---
title: "版本管理 02｜Git 的工作模型：工作区、暂存区、提交与远端"
slug: "version-management-02-git"
date: "2026-10-29"
description: "用暂存后继续修改同一文件的命令行实验，观察工作区、暂存区、提交和远端，解释提交后仍有修改、推送后对方文件未变的原因。"
draft: false
content_stage: "article"
weight: 3962
tags: ["版本管理", "Git"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 2
reading_title: "Git 的工作模型"
---

一次提交已经成功，同一个文件却仍然显示有修改；一次推送已经成功，另一个人的文件也没有变化。理解这两个现象，需要先问：**命令操作的究竟是哪份内容？**

[第一篇]({{< relref "delivery-engineering/version-management-01-model.md" >}})把选择修改、记录版本和共享内容分开。本篇用一个文本文件实际走过这些动作：先修改伤害并暂存，再继续修改蓄力开关，最后只把已经暂存的内容交给接收方。

> 实测范围：2026-10-29，Git `2.53.0.windows.2`、Windows PowerShell `5.1.19041.6456`；一个本地裸仓库、两个独立工作区，23 项检查通过。文件是教学数据，没有运行游戏，也没有验证网络传输或托管平台审批。[实验脚本与完整记录](https://github.com/TwoEggDu/twoegg-tech-stack/tree/main/examples/version-management/git-work-model)

## 先把一个文件拆成三份内容

在这个例子里，同一个路径 `skill.rules` 会对应三份需要分别查看的内容：

- **工作区**：磁盘上正在编辑的文件。
- **暂存区，也称 index**：为下一次提交准备的内容。
- **当前提交**：本次从历史中选来比较的版本；下面使用 `HEAD` 指向的提交。

`git add` 将指定文件当时的内容加入暂存区。之后继续编辑，暂存内容不会因此自动更新。本文使用普通的 `git commit -m ...`，它记录已经暂存的内容；带 `-a` 或路径等其他提交方式有不同规则，不在这次实验中使用。[git-add 官方说明](https://git-scm.com/docs/git-add)

初始文件只有两行：

```text
damage=100
charge=off
```

我们把这份内容记录为基线 V0，并让开发方与接收方都从 V0 开始。仓库与目录关系如下：

```text
shared.git     本地裸仓库，充当共享远端
developer      开发方独立仓库与工作区
receiver       接收方独立仓库与工作区
```

裸仓库在这里负责保存 Git 对象和引用，没有供人日常编辑的工作区。实验没有连接 GitHub 或 GitLab。

<details>
<summary>环境准备：在一个新的临时目录中建立基线</summary>

下面是交互执行的准备命令，从本仓库根目录开始。`$labRoot` 必须指向不存在的新目录；每一步确认成功后再继续。需要一次执行并自动检查结果时，使用配套 `run-lab.ps1`，它会检查 Git 退出码、隔离配置并保存证据。

```powershell
$labRoot = Join-Path (Get-Location) '.tmp/git-work-model-manual'
if (Test-Path -LiteralPath $labRoot) { throw '请换一个新的实验目录' }
New-Item -ItemType Directory -Path $labRoot | Out-Null

git init --bare --initial-branch=main "$labRoot/shared.git"
git init --initial-branch=main "$labRoot/developer"
Set-Location "$labRoot/developer"
git config --local user.name 'Version Management Lab'
git config --local user.email 'lab@example.invalid'
git config --local core.autocrlf false
git config --local core.eol lf
git config --local commit.gpgsign false

$utf8 = New-Object System.Text.UTF8Encoding($false)
$rulesPath = Join-Path (Get-Location) 'skill.rules'
[IO.File]::WriteAllText($rulesPath, "damage=100`ncharge=off`n", $utf8)
git add -- skill.rules
git commit -m 'Baseline'
git remote add origin "$labRoot/shared.git"
git push -u origin main
git clone --no-hardlinks "$labRoot/shared.git" "$labRoot/receiver"
```

这些身份、换行和签名设置只写入新建的实验仓库。文件明确使用 UTF-8 无 BOM 与 LF，减少 PowerShell 默认写入方式和换行转换对内容比较的影响。完整脚本采用空初始化模板和进程级配置隔离；本折叠段用于理解和手动操作，不代表你的现有全局配置已经被隔离。

</details>

## 暂存之后再改一次，看两种差异

接下来都在 `developer` 中操作。先把伤害从 100 改成 80，再暂存这份内容：

```powershell
[IO.File]::WriteAllText($rulesPath, "damage=80`ncharge=off`n", $utf8)
git add -- skill.rules
```

然后继续编辑同一个文件，将蓄力开关打开。这一次不执行暂存：

```powershell
[IO.File]::WriteAllText($rulesPath, "damage=80`ncharge=on`n", $utf8)
git status --short
```

本次实际输出为：

```text
MM skill.rules
```

这里有两个状态列。对本实验这种已经跟踪、没有合并冲突的文件，第一列说明暂存区相对 `HEAD` 的状态，第二列说明工作区相对暂存区的状态。两个 `M` 表示这两次比较都发现了修改，并不是文件被重复记录。[git-status 的短格式规则](https://git-scm.com/docs/git-status#_short_format)

先看**工作区相对暂存区**的差异：

```powershell
git diff -- skill.rules
```

实际差异如下，省略文件头，仅保留内容块：

```diff
@@ -1,2 +1,2 @@
 damage=80
-charge=off
+charge=on
```

伤害 80 已经存在于这两份内容里，因此这里仅展示后来打开的蓄力开关。

再看**暂存区相对当前提交**的差异：

```powershell
git diff --cached -- skill.rules
```

```diff
@@ -1,2 +1,2 @@
-damage=100
+damage=80
 charge=off
```

这次看到的才是准备进入下一次提交的伤害修改。普通 `diff` 与 `diff --cached` 的比较对象不同；前者为空，也不能单独证明没有准备提交的修改。[git-diff 官方说明](https://git-scm.com/docs/git-diff)

还可以直接查看三份完整内容：

```powershell
Get-Content -LiteralPath $rulesPath
git show :skill.rules
git show HEAD:skill.rules
```

`:<path>` 在这里定位暂存区里的文件内容，`HEAD:<path>` 定位当前提交中的文件内容。它们查询的都是 Git 记录，不会用磁盘上的最新文件替代结果。[Git 的对象定位语法](https://git-scm.com/docs/gitrevisions)

| 此时查看的对象 | damage | charge |
| --- | --- | --- |
| 工作区 | 80 | on |
| 暂存区 | 80 | off |
| `HEAD` 对应的提交 V0 | 100 | off |

这三行正是理解本次操作的中心：**一个路径可以同时有正在编辑的内容、准备提交的内容和已经提交的内容。**

## 提交记录了什么，为什么文件仍有修改

保持刚才的状态，执行普通提交：

```powershell
git commit -m 'Reduce damage to 80'
git show HEAD:skill.rules
git status --short
```

新提交里的实际内容是：

```text
damage=80
charge=off
```

状态输出则是下面这一行，`M` 前面的空格属于状态本身：

```text
 M skill.rules
```

提交以后，暂存区与新 `HEAD` 中的文件内容一致，第一列变为空格；工作区仍是 `charge=on`，相对暂存区还有修改，所以第二列保留 `M`。提交成功和工作区仍有修改可以同时成立。

没有再次暂存的编辑留在本地，这正是本次想保留的结果。如果现在又对整个文件执行 `git add`，蓄力开关也会被更新进暂存区；所以“这个文件以前暂存过”不能替代提交前的内容核对。

新提交记为 V1。此次本地历史只有一条简单的父子关系：

```text
V0 —— V1
       ↑
     main ← HEAD
```

在当前没有分离 HEAD 的开发仓库中，`main` 是指向提交的本地分支引用，`HEAD` 指向正在使用的分支。提交把 `main` 推进到 V1，V1 仍能关联父提交 V0。分支名称负责指向历史位置，不等于再复制一套工作目录。

这里讨论的是提交和引用的最小模型。Git 怎样压缩对象、多个分支怎样合并，以及如何从已经混在一起的修改中拆任务，分别留到后续篇目。

## 推送和取得内容，分别改变了哪里

V1 已经记录在开发方仓库，但测试远端可能仍指向 V0。可以先核对：

```powershell
git rev-parse HEAD
git rev-parse origin/main
git ls-remote --heads origin refs/heads/main
```

三个命令分别查询当前本地提交、本地保存的远端跟踪引用，以及远端当前公布的 `main` 引用。**`origin/main` 保存在本地，不是每次读取时都会向服务器查询。** 在本次提交后、推送前，第一项为 V1，后两项仍为 V0。

执行推送，再查询远端：

```powershell
git push origin main
git ls-remote --heads origin refs/heads/main
```

推送会传送所需对象并更新远端引用。实测远端 `main` 已指向 V1；它保存的文件仍是 `charge=off`，因为推送以仓库中的提交为对象，不把开发方尚未暂存的编辑一起打包。[git-push 官方说明](https://git-scm.com/docs/git-push)

此时切到接收方，核对其状态：

```powershell
Set-Location "$labRoot/receiver"
git rev-parse HEAD
git rev-parse origin/main
Get-Content -LiteralPath skill.rules
git fetch origin
git rev-parse origin/main
git rev-parse HEAD
Get-Content -LiteralPath skill.rules
```

实际观察到三个阶段：

| 接收方阶段 | 本地 `origin/main` | `HEAD` | 磁盘内容 |
| --- | --- | --- | --- |
| 开发方已推送，接收方尚未 fetch | V0 | V0 | 100 / off |
| 接收方执行 `fetch origin` 后 | V1 | V0 | 100 / off |
| 接收方明确切到 V1 后 | V1 | V1 | 80 / off |

`fetch origin` 在本次克隆的默认引用映射下取得对象并更新远端跟踪引用，但没有改变接收方当前选中的提交和工作文件。它让接收方知道共享仓库更新到了哪里；怎样把这些内容用于当前工作，需要后续操作。[git-fetch 官方说明](https://git-scm.com/docs/git-fetch)

实验最后在**干净的接收目录**中选定刚取得的提交：

```powershell
$candidate = git rev-parse origin/main
git switch --detach $candidate
git show HEAD:skill.rules
Get-Content -LiteralPath skill.rules
git status --short
```

这次 Git 记录与磁盘内容都为 `damage=80`、`charge=off`，状态为空。`--detach` 使 `HEAD` 直接指向指定提交；本实验只用它固定查看对象，没有推进接收方的本地 `main`，也不在这里继续开发。日常分支整合怎样选择，留到分支与合并篇。

本次基线 V0 的完整标识是 `516755fb6f3e47f72f8be0680011b193a6be0971`，V1 是 `4948ff5d011831377760eda670e4b8b8a794b377`。复跑时提交时间等元数据可能不同，不要求得到同一 SHA；要核对的是每次实验内部的内容与引用关系。

## 放回游戏工程：检查准备提交的内容

将 `skill.rules` 换成脚本或文本配置，首先仍要分清这三份内容：开发者可能暂存了一次修复，随后又为调试或下一项需求修改了同一个文件。如果只看磁盘上的文件，就容易把当前运行所用的内容误认为下一次提交的内容。

提交前可以先用 `git status --short` 看状态，再用 `git diff --cached` 检查已准备的变化，用 `git diff` 检查还留在工作区的变化。二者都需要放在明确的比较对象下理解。新文件、忽略规则与资源依赖还需要另外检查；本文的单文件实验没有覆盖整个工程的完整性。

实验也没有证明已混合的任务总能自动拆开。这里特意采用“先暂存修复，再继续编辑”的顺序；如果两项工作已经交织，仍需在[任务提交篇]({{< relref "delivery-engineering/version-management-05-task-submission.md" >}})讨论选择片段、相互依赖及本地修改保护。

最后，推送成功只说明对应远端接受了这次引用更新。团队审查、正式合入、引擎导入、构建和功能测试，需要各自的条件与证据。当前接收方内容一致的结果，只支持这个 Git 文本文件场景。

**提交前分清工作区、暂存区和当前提交，交接时再核对远端与接收方实际指向的版本。**
