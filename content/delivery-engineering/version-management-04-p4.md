---
title: "版本管理 04｜P4 的工作模型：Depot、Workspace、Changelist 与 Shelf"
slug: "version-management-04-p4"
date: "2026-10-29"
description: "用双工作区命令行实验，观察搁置后继续编辑时 Workspace、Shelf 与 Depot 的不同内容，理解 Changelist、普通提交、同步和 reconcile。"
draft: false
content_stage: "article"
weight: 3964
tags: ["版本管理", "P4", "Perforce"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 4
reading_title: "P4 的工作模型"
---

把修改放进一个 Changelist，再上传 Shelf，接着继续编辑。此时同一个文件，自己看到什么，接收方取得什么，Depot 正式版本又是什么？

理解 P4，需要把**磁盘内容、打开状态、变更组织、搁置内容与已提交历史**分开。pending Changelist 组织了一组准备提交的文件；要知道交接的是哪份内容，还要查看 Shelf 或最终提交。

本篇沿用[共同模型]({{< relref "delivery-engineering/version-management-01-model.md" >}})中的伤害与蓄力示例，实际执行 `edit → shelve → 继续编辑 → unshelve → submit → sync`。最后用一次磁盘直接改写，观察 `reconcile` 怎样补登记打开状态。

> 实测范围：2026-10-30，P4 CLI / P4D `2026.1/3089746`（NTX64）、Windows PowerShell `5.1.19041.6456`；一个仅监听回环地址的 personal server、一个合成用户、两个普通映射工作区、一个文本文件，19 项检查通过，测试进程已停止。未验证生产权限、TLS、P4V、审查平台、锁或引擎行为。[实验脚本与完整记录](https://github.com/TwoEggDu/twoegg-tech-stack/tree/main/examples/version-management/p4-work-model)

## 先把路径与内容分开

同一份文件在实验中有三种路径写法：

```text
//depot/skill.rules                  Depot 路径
//vm-a/skill.rules                   客户端路径
[LAB]/workspace-a/skill.rules        A 的本地路径
```

Depot 保存受版本管理的文件及其历史；Workspace 对应一份 client specification，定义根目录、映射和工作区选项，并关联服务器记录的工作区状态。`View` 决定哪些 Depot 路径映射到这个客户端，`Root` 决定本地落点。[p4 client 说明](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_client.html)

本次 A、B 的 View 都覆盖 `//depot/...`，但各自映射到 `//vm-a/...`、`//vm-b/...`，Root 也不同。它们可以取得相同版本，同时保留不同的本地编辑与打开状态。

| 需要查看的对象 | 关键问题 | 本文使用的命令 |
| --- | --- | --- |
| Workspace 磁盘文件 | 编辑器现在看到什么？ | `Get-Content` |
| 打开状态 | 哪些文件登记为 add、edit 等动作？ | `p4 opened`、`p4 fstat` |
| pending Changelist | 哪些打开的文件被组织为一组？ | `p4 change -o` |
| Shelf | 上次搁置保存了什么内容？ | `p4 print` 加 `@=编号` |
| Depot 已提交版本 | 团队正式历史中有什么？ | `p4 print` |
| have list | 服务器记录该工作区已取得哪个文件修订？ | `p4 have` |

have list 也不能替代磁盘内容检查：同步之后仍可以继续编辑文件。下面的对照会同时记录两者。

<details>
<summary>实验环境与命令约定</summary>

配套 `run-lab.ps1` 新建实验目录与独立服务。2026.1 默认启用认证，本次先通过官方 `p4 init -C1 -n` 创建 personal server，再在服务停止时为合成用户设置随机口令，最后启动显式回环端口并登录。没有安装 Windows 服务。[p4 init](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_init.html)、[2026.1 默认安全配置](https://help.perforce.com/helix-core/server-apps/p4sag/current/Content/P4SAG/secure-by-default-overview.html)

实际配置记录为 `security=4 (default)`、`dm.user.noautocreate:1 (configure)`、`serverServices local`；这是 personal server 的初始化结果，不是团队服务安装范例。初始化自带的 Stream 不参与下面的实验，业务文件另放在普通 local Depot 中。

脚本接收 `-LabRoot`、`-EvidenceDirectory`、`-P4dPath`，两个输出目录必须不存在；运行方式、官方服务端 SHA256 与初始化细节见实验 README。随机口令与 ticket 不进入公开证据。

下文摘录脚本中的业务步骤，假设该隔离服务已完成登录、client view 建立及初始提交。`$endpoint` 是本次随机选择的 `127.0.0.1:端口`，`$rootA`、`$rootB` 是不同根目录；服务会在脚本退出时停止，因此不要把记录中的旧端口用于新实验。

```powershell
$a = @('-p', $endpoint, '-u', 'vm-lab', '-c', 'vm-a')
$b = @('-p', $endpoint, '-u', 'vm-lab', '-c', 'vm-b')
$utf8 = New-Object System.Text.UTF8Encoding($false)
p4 @a where //depot/skill.rules
```

客户端选项为 `noallwrite`、`noclobber`，`LineEnd: unix`，服务使用非 Unicode 模式且所有教学文本为 ASCII。两工作区共用一个合成用户，只用于分开验证工作区状态，不证明不同人员的权限边界。

</details>

## 打开文件并组织到 pending Changelist

初始提交后，Depot 中的 `skill.rules#1`、A、B 都是：

```text
damage=100
charge=off
```

A 创建编号 Changelist：

```powershell
@"
Change: new
Client: vm-a
User: vm-lab
Description:
    Damage change for shelf exercise
"@ | p4 @a change -i
```

本次返回 `Change 2 created.`。下文 `$change` 表示实际返回的编号；自动脚本从结果解析它，不假设永远为 2。

```powershell
$change = '2'  # 本次运行的返回值，复跑时使用自己的编号
p4 @a edit -c $change "$rootA/skill.rules"
[IO.File]::WriteAllText("$rootA/skill.rules", "damage=80`ncharge=off`n", $utf8)
p4 @a opened -c $change
p4 @a change -o $change
```

实测文件已登记 `edit change 2`，Changelist 状态为 `pending`。在本次 `noallwrite` 设置下，`edit` 还会让已同步的只读文件可写。

**打开文件与保存一份固定内容是两个动作。** `edit -c` 登记准备修改哪个文件以及归属哪个 pending Changelist，之后仍会继续改写磁盘。Changelist 编号本身不意味着这些编辑已经上传或固定下来。[Changelist 工作方式](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/P4Guide/basic-tasks.recurring-other.changelist.html)

## 搁置之后再改一次，同一路径出现三份内容

先搁置伤害改为 80 的版本，再在本地启用蓄力：

```powershell
p4 @a shelve -c $change
[IO.File]::WriteAllText("$rootA/skill.rules", "damage=80`ncharge=on`n", $utf8)

Get-Content "$rootA/skill.rules"
p4 @a print -q "//depot/skill.rules@=$change"
p4 @a print -q //depot/skill.rules
p4 @a change -o $change
```

本次观察：

| 来源 | damage | charge |
| --- | --- | --- |
| A 当前磁盘 | 80 | on |
| Changelist 2 的 Shelf | 80 | off |
| Depot 最新已提交内容 | 100 | off |

`shelve` 把当时打开文件的内容保存到服务端，供后续恢复或交接，但没有把 pending Changelist 变成 submitted。之后继续编辑，没有再次执行更新 Shelf 的命令，已经保存的 Shelf 不会自动跟着磁盘变化。[p4 shelve](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_shelve.html)

这里 `@=$change` 用来定位该 Changelist 的搁置文件内容；本例该编号此时仍为 pending。`print` 不带修订限定时查看这个 Depot 路径的最新已提交内容。[p4 print](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_print.html)

与 [Git 篇]({{< relref "delivery-engineering/version-management-02-git.md" >}})对照时，要保留这种语义差别：Git 普通提交记录暂存内容；P4 的 pending Changelist 组织打开的文件，Shelf 另存搁置时的内容。不能把 Changelist 或 Shelf 直接当成 Git 暂存区来套用后续提交规则。

## B unshelve，拿到的是 Shelf

B 当前文件干净且仍位于基线 #1，执行：

```powershell
p4 @b unshelve -s $change
Get-Content "$rootB/skill.rules"
p4 @b opened
p4 @b print -q //depot/skill.rules
```

结果是 B 磁盘出现 `damage=80`、`charge=off`，文件打开为默认 Changelist 中的 edit；Depot 仍为 `100 / off`。

B 取得的是 Shelf 保存的内容，既没有取得 A 搁置后继续写下的 `charge=on`，也没有产生正式提交。`unshelve` 会涉及接收工作区的基线和打开状态；其他基线或本地修改可能引出 resolve 等要求，本次干净基线对照未触发这些情况。[p4 unshelve](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_unshelve.html)

## 普通 submit 取得当前打开文件，B 仍需 sync

为了让 B 回到原来的干净基线，先撤销它**仅为本实验取得的修改**。随后删除 A 的 Shelf，再走普通工作区提交：

```powershell
p4 @b revert "$rootB/skill.rules"
p4 @a shelve -d -c $change
p4 @a submit -c $change

p4 @a change -o $change
p4 @a print -q //depot/skill.rules
Get-Content "$rootB/skill.rules"
p4 @b have //depot/skill.rules
```

`revert` 会丢弃目标文件的本地改动，这里只对自己的教学副本使用。本次 submit 成功后，Changelist 为 `submitted`，Depot 新文件修订为 #2，内容是 **80 / on**，包含 A 搁置后继续写入的蓄力开关。B 已撤销回基线，磁盘仍为 **100 / off**，have list 也仍为 #1。

本篇实验明确使用 `submit -c` 从工作区提交，并在提交前删除 Shelf；没有测试从 Shelf 直接提交的 `submit -e` 路径。[p4 submit](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_submit.html)

接着 B 明确同步：

```powershell
p4 @b sync //depot/skill.rules
p4 @b have //depot/skill.rules
Get-Content "$rootB/skill.rules"
```

B 才取得 `80 / on`，have list 前进到 #2。**提交更新服务器历史，同步更新接收工作区。** 本次 Changelist 2 与文件 #2 恰好同号：前者标识一次变更记录，后者是这个文件的修订号，不能据此混为一类编号。

## 磁盘改了，为什么 opened 仍没有 edit

最后模拟一次绕过 P4 打开流程的磁盘改写：先手动清除这个教学文件的只读属性，再把伤害改为 70。

```powershell
(Get-Item -LiteralPath "$rootB/skill.rules").IsReadOnly = $false
[IO.File]::WriteAllText("$rootB/skill.rules", "damage=70`ncharge=on`n", $utf8)
p4 @b -ztag fstat -T haveRev,headRev,action //depot/skill.rules
```

实测 `headRev` 与 `haveRev` 都为 2，没有 `action` 字段。磁盘变化没有自动登记打开状态。接着先预览，再登记：

```powershell
p4 @b reconcile -n -e "$rootB/skill.rules"
p4 @b reconcile -e "$rootB/skill.rules"
p4 @b opened
p4 @b print -q //depot/skill.rules
```

`-n` 只预览，脚本在预览前后都用 fstat 核对没有 action；真正执行后才出现 edit。Depot 仍为 `80 / on`，说明 reconcile 没有替代 submit。`-e` 将本次范围限定为已修改文件，不扫描其他新增或删除动作。[p4 reconcile](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_reconcile.html)

本次脚本还处理了一个实测细节：快速复跑时，同步后同一秒内的等长改写曾被 reconcile 预览漏检；只改变文件修改时间后即可识别。因此配套脚本对这个教学改写显式增加 2 秒修改时间，并记录该动作，再检查上述结果。这里的通过结论包含这一前提，不推广为所有文件系统的检测保证；调试过程见实验 README。

## 放回资源协作，交接时明确指向哪份内容

在游戏项目中，一次任务可能同时修改脚本、配置、贴图和引擎资产。pending Changelist 可以组织打开的文件，但任务是否完整，仍需检查实际文件与依赖；Workspace 映射只决定本地能取得哪些路径，不会替团队补齐遗漏依赖。

用 Shelf 交接时，应核对接收方实际取得的搁置内容。若搁置后继续编辑，之前交给别人检查的内容可能已经不同于后来普通提交的内容；审查与最终提交怎样对应，留到[第 08 篇]({{< relref "delivery-engineering/version-management-08-review.md" >}})。本次没有接入任何审查系统。

资源能否并行编辑还取决于文件类型、锁和团队流程；本实验的普通文本 edit 没有证明排他锁语义。Streams 与集成进入[第 06 篇]({{< relref "delivery-engineering/version-management-06-branching-and-merging.md" >}})，资源锁与依赖进入[第 07 篇]({{< relref "delivery-engineering/version-management-07-assets.md" >}})，不以一次双工作区成功替代它们的验证。

**Changelist 组织修改，Shelf 保存搁置内容，submit 记录正式版本，sync 把版本带到另一个工作区。**
