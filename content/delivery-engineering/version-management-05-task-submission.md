---
title: "版本管理 05｜一次任务怎样组成一次提交：选文件、依赖与本地修改保护"
slug: "version-management-05-task-submission"
date: "2026-10-29"
description: "用 Git 命令行闭环只交付任务 A、保留任务 B，验证遗漏依赖、同文件混合修改、提交拒绝和模拟回执丢失，再补 SVN/P4 的文件分组边界。"
draft: false
content_stage: "article"
weight: 3965
tags: ["版本管理", "任务提交", "资源依赖"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 5
reading_title: "一次任务怎样组成一次提交"
---

现在只允许交付任务 A，开发目录里却已经有 A 和未完成的 B。它们还改了同一个脚本。只选脚本可能夹带 B，只挑脚本里的几行又可能漏掉 A 新增的配置。

一次任务提交需要同时回答三个问题：**候选是否只含本次任务、依赖是否齐全、未交付的工作是否仍在原处。** 提交命令返回成功之后，还要核对历史记录与接收方实际取得的内容。

前面的 [Git]({{< relref "delivery-engineering/version-management-02-git.md" >}})、[SVN]({{< relref "delivery-engineering/version-management-03-svn.md" >}})、[P4]({{< relref "delivery-engineering/version-management-04-p4.md" >}})篇分别建立工具模型。本篇沿 Git 命令行跑完一次交付，再补 SVN/P4 的关键差异。

> 实测范围：2026-10-30，Git `2.53.0.windows.2`、Python `3.12.4`；同机本地裸仓库、开发仓库、暂存树导出目录与干净接收仓库，33 项检查通过。包含两个预期失败和一次包装器模拟回执丢失；没有引擎、真实网络故障或 SVN/P4 同任务端到端验证。[实验脚本、补丁与完整记录](https://github.com/TwoEggDu/twoegg-tech-stack/tree/main/examples/version-management/task-submission)

## 先写清任务边界，再看文件列表

教学程序只有两个行为：返回伤害值，以及返回蓄力是否启用。基线是 `100 / false`，本次任务如下：

| 文件或动作 | 开发目录中的工作 | 本次候选要什么 |
| --- | --- | --- |
| `skill.py` 的伤害函数 | A：改为读取新配置，伤害为 80 | 纳入 |
| 同文件的蓄力函数 | B：从 false 改为 true，尚未交付 | 保留在开发目录 |
| 新增 `damage.json` | A：提供伤害 80 | 必须纳入 |
| `notes.txt`、合成 `texture.bin` 修改 | B 的说明与二进制占位数据 | 保留在开发目录 |
| 删除 `obsolete.txt`、新增未跟踪 `scratch.txt` | B 的整理与草稿 | 保留在开发目录 |

我们希望候选运行结果为 `80 / false`，开发目录仍保持 `80 / true`。任务判断来自这份约定，工具不会根据文件名自动理解 A 和 B。

同一文件中的两处改动也不是因为能分开选，就必然能独立交付。这里刻意让它们位于两个独立函数；若 A 调用了 B 新增的逻辑，就要先解除依赖或调整任务边界。二进制文件本次只检查整份内容的保留，没有演示片段拆分。

<details>
<summary>复跑入口与目录约定</summary>

需要 Python 3.11+ 与 Git，Windows 使用能运行标准 shell hook 的 Git for Windows。从本仓库根目录执行，输出目录都必须不存在：

```powershell
python examples/version-management/task-submission/run-lab.py `
  --lab-root E:/workspace/TechStackShow/.tmp/task-submission-new `
  --evidence-directory E:/workspace/TechStackShow/.tmp/task-submission-new-evidence
```

将绝对路径换成自己的位置。脚本在新根中建立 `shared.git`、`developer`、两个候选导出目录和 `receiver`，隔离 Git 系统/全局配置，不使用公司仓库或已有 hook。

下列命令按实验执行阶段摘录，`$dev` 指向新建的 `developer`，`$labRoot` 是该实验根，`$patch` 指向实际的 `task-a.patch`，`$labHooks` 是脚本创建的独立 hook 目录。一次完整复跑使用上面的入口；手工操作时逐步确认命令退出码再继续。

</details>

## 候选内容可以不同于磁盘内容

先看修改，再决定选择什么：

```powershell
git -C $dev status --short
git -C $dev diff -- skill.py
```

`skill.py` 包含两处实质变化：

```diff
 def damage():
-    return 100
+    return json.loads(Path(__file__).with_name("damage.json").read_text())["damage"]

 def charge_enabled():
-    return False
+    return True
```

上面压缩展示了两处改动，不是用于应用的完整补丁。手工选择可以从 `git add -p -- skill.py` 进入：逐块检查，只选择伤害变化；若显示在同一块中，需要继续拆块或编辑候选，再核对完整差异。[git add 的交互与 patch 模式](https://git-scm.com/docs/git-add)

自动实验使用已经明确写出的 A-only 目标生成补丁，只改伤害函数，然后执行：

```powershell
git -C $dev apply --cached --check $patch
git -C $dev apply --cached $patch
git -C $dev show :skill.py
git -C $dev diff --cached -- skill.py
git -C $dev diff -- skill.py
```

`--cached` 只应用到 index，`--check` 只预检；这些命令不会创建提交。[git apply 官方说明](https://git-scm.com/docs/git-apply)

实测 index 中伤害函数已读取 JSON，蓄力仍为 false；磁盘仍是 A+B。这次确实执行的是补丁应用，没有自动驱动或验证 `add -p` 的交互菜单。补丁也只是任务选择的表达：在真实项目中，仍要由人判断它是否完整、是否夹带其他工作。

## 本地能运行，为什么候选却失败

现在开发目录已经有新 JSON，运行成功：

```text
{"charge": true, "damage": 80}
```

但目前 index 只选择了 A 的脚本改动，还没有新 JSON。把 **index** 导出到不存在的候选目录，直接运行：

```powershell
$missing = Join-Path $labRoot 'candidate-missing-dependency'
New-Item -ItemType Directory -Path $missing | Out-Null
$missingPrefix = $missing.Replace('\','/') + '/'
git -C $dev checkout-index --all "--prefix=$missingPrefix"
python -B "$missing/skill.py"
```

实测退出 1，`FileNotFoundError` 指向缺少的 `damage.json`。它不能借用开发目录那份配置，因为程序明确从自身脚本目录读取文件。候选完整性的问题因此暴露出来。

这里导出的是暂存内容，直接复制开发目录会把 B 与未提交输入也带过去。`checkout-index` 的 `--prefix` 用于将 index 内容写到指定位置，末尾 `/` 有实际含义；本文只在新建目录使用。[暂存树导出说明](https://git-scm.com/docs/git-checkout-index)

接着只补齐 A 的依赖，再导出到另一个新目录：

```powershell
git -C $dev add -- damage.json
git -C $dev diff --cached --name-only
git -C $dev write-tree

$complete = Join-Path $labRoot 'candidate-complete'
New-Item -ItemType Directory -Path $complete | Out-Null
$completePrefix = $complete.Replace('\','/') + '/'
git -C $dev checkout-index --all "--prefix=$completePrefix"
python -B "$complete/skill.py"
```

本次变更清单恰好是 `damage.json`、`skill.py`。脚本的 index 内容没有变化，新候选运行退出 0：

```text
{"charge": false, "damage": 80}
```

候选仍包含基线已有的其他文件；“只变更两个文件”不代表“候选目录只有两个文件”。脚本还核对合成二进制是基线字节、旧文件仍存在、B 的草稿未进入候选。

## 明确拒绝后，检查候选与本地修改是否保留

下一步故意让一次本地 commit 失败。实验仅在自己的新仓库设置 `core.hooksPath`，放入固定拒绝器：

```sh
#!/bin/sh
printf "%s\n" "VM-LAB: intentional rejection" >&2
exit 1
```

```powershell
git -C $dev config --local core.hooksPath $labHooks
git -C $dev commit -m 'Task A'

git -C $dev rev-parse HEAD
git -C $dev write-tree
git -C $dev status --short
```

实测 commit 退出 1，输出实验拒绝标记；HEAD 仍为基线，index tree 仍为已验证的完整候选，远端也仍为基线。Git 的 `pre-commit` 在创建提交前运行，非零退出可中止 commit。[hook 调用时机](https://git-scm.com/docs/githooks)

但不能只因命令失败就假定磁盘没有变化：hook 本身可能执行任意处理。本次脚本使用自己已知的拒绝器，并在失败后重新计算开发目录文件清单与 SHA256，确认与选择前相同，B 的删除也保持不变。

接着只禁用本实验的固定拒绝器，再正常提交。这是教学注入的解除步骤，不是让实际项目跳过失败检查；没有使用 `--no-verify` 或修改用户 hook。

```powershell
git -C $dev commit -m 'Task A: damage configuration only'
git -C $dev rev-parse HEAD
git -C $dev rev-parse 'HEAD^{tree}'
git -C $dev diff --cached
```

实测生成的教学提交为 `4bfd9a5…`，父提交是基线，提交 tree 等于已经验收的 index tree。开发目录仍保留 B，此时远端还没有前进。

## 回执不明时，先查询实际完成到哪一层

本次用明确 SHA 推送到本地裸仓库：

```powershell
$candidate = git -C $dev rev-parse HEAD
git -C $dev push origin "${candidate}:refs/heads/main"
```

实验控制端确认真实 push 退出 0 后，包装步骤故意不给模拟调用方返回成功，只给出 `unknown`。这用于模拟“无法根据当前回执判断结果”，没有切断真实网络，也没有验证超时或服务崩溃。

模拟调用方随后独立查询：

```powershell
git -C $dev ls-remote --heads origin refs/heads/main
```

查询返回的 SHA 与候选完全相同，说明该远端 ref 此时已经指向预期提交。[git ls-remote](https://git-scm.com/docs/git-ls-remote)

本次因此没有再次 push；裸仓库相对基线只增加一次任务提交。若查询失败，应继续记为未知；若 SHA 不符，还要调查目标 ref、权限与并发更新，不能直接把本次结果套上去。

异常可以按实际状态区分：

| 观察到什么 | 本次应核对什么 |
| --- | --- |
| commit 明确被本实验 hook 拒绝 | HEAD 是否前进、index 和磁盘是否被改写 |
| 本地已有候选提交，远端仍为基线 | 内容是否尚待共享，以及下一次 push 的目标 |
| push 回执未知 | 远端精确 ref 当前指向什么，是否能确认预期提交已交付 |

对 SVN/P4，提交阶段和可查询的记录身份不同，需要使用它们的仓库记录核对；Git 这次本地实验没有证明其他工具的异常行为。

## 让接收方取得精确版本，再核对原目录

最后从裸仓库建立新的接收仓库，明确取得候选 SHA：

```powershell
git clone --no-hardlinks --no-checkout "$labRoot/shared.git" "$labRoot/receiver"
git -C "$labRoot/receiver" switch --detach $candidate
python -B "$labRoot/receiver/skill.py"
git -C "$labRoot/receiver" status --short
```

实测输出为 `80 / false`，接收仓库干净，逐文件散列与完整 index 导出目录一致。开发目录仍为 `80 / true`，备注、合成二进制、删除和未跟踪草稿都保留；在选择后、补依赖后、拒绝后、commit 后、push 与查询后，五个检查点的开发目录文件清单与字节散列均未变化。

这证明本次交付保留了磁盘内容，不代表编辑器未保存缓冲区、文件权限或全部时间戳也经过验证。需要恢复保障时，应另行保存可恢复副本并验证恢复过程；本次没有执行覆盖后恢复实验。

## 换成 SVN/P4，要保留哪些区别

下面是机制对照与流程建议，没有在本篇新增 SVN/P4 同任务实测。

| 工具 | 组织不同文件的入口 | 同一文件混有 A、B 时 |
| --- | --- | --- |
| Git | 路径选择、index，必要时分块选择 | 本文用 A 补丁构造 index，并导出验收 |
| SVN | 明确路径与本地 changelist | 文件标签不能表达同文件内两套候选内容；可在独立工作副本准备 A，再核对依赖及差异 |
| P4 | pending Changelist，`reopen -c` 调整文件归属 | 移动打开文件没有自动拆开 A、B；可在独立 Workspace 准备并验证 A |

SVN Book 1.7 将 changelist 解释为工作副本文件的标签，本地分组不是另一份已保存的文件内容；这里引用其历史版本基础语义，不把该文档当成本次 1.14.1 实测。[SVN changelist](https://svnbook.red-bean.com/en/1.7/svn.advanced.changelists.html)

P4 2026.1 的 `reopen -c` 将打开文件移到指定 pending Changelist；它组织的是文件归属。[p4 reopen](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_reopen.html)若要交接内容，还要检查 Shelf 或最终提交，特别是[搁置后继续编辑]({{< relref "delivery-engineering/version-management-04-p4.md" >}})时的差别。

独立工作副本也需要明确基线、选入内容与验收对象。它减少对原目录的干扰，但不会替人识别依赖或自动生成正确候选。

## 放到游戏工程，依赖检查还要继续往下走

本次新增 JSON 说明一个最小事实：漏掉输入时，当前开发目录可能仍能运行，干净候选却不能。真实引擎工程需要继续检查代码、配置、资源引用和必要元数据。例如 Unity 的资源与 `.meta`、Unreal 的资产引用及外部制作文件，都需要各自的身份与依赖验证，不能仅用文件清单或本次 Python 程序通过来证明完整。

提交前看候选内容，提交后核对实际记录，交接时让接收方明确取得指定身份，再看剩余工作是否保留。审查是否覆盖最终内容进入[第 08 篇]({{< relref "delivery-engineering/version-management-08-review.md" >}})，工具链和完整构建输入进入[第 09 篇]({{< relref "delivery-engineering/version-management-09-build-baseline.md" >}})。

**一次任务提交，要让候选带齐本次依赖，让接收方取得这份内容，也让未交付的工作完整留下来。**
