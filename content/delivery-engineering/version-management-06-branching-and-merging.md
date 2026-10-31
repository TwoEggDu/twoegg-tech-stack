---
title: "版本管理 06｜分支与合并：并行开发之后，修改怎样汇合"
slug: "version-management-06-branching-and-merging"
date: "2026-10-29"
description: "用 Git 命令行对照快进、三方合并与文本冲突，再验证两条分支单独通过、自动合并后伤害超限的语义冲突，分开核对历史关系和最终行为。"
draft: false
content_stage: "article"
weight: 3966
tags: ["版本管理", "分支", "合并"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 6
reading_title: "分支与合并"
---

两条分支各自验证通过，合并没有冲突，为什么合到一起后反而出错？

设一条分支把基础伤害从 100 改为 120，另一条把倍率从 100% 改为 140%。它们修改不同文件，单独结果分别是 120、140，都没有超过约定上限 150；合并后却得到 168。文件能够自动汇合，组合行为仍可能违反共同规则。

分支与合并需要同时核对两件事：**修改从哪份历史进入目标，以及目标最终表现是否正确。** [上一篇]({{< relref "delivery-engineering/version-management-05-task-submission.md" >}})检查一次任务怎样组成候选，本篇继续检查不同工作线的候选怎样汇合。

> 实测范围：2026-10-31，Git `2.53.0.windows.2`、Python `3.12.4`；四个独立本地教学仓库和一个干净接收副本，41 项检查通过。两个预期非零退出分别为文本冲突与合并后的业务检查失败。没有网络、引擎或 SVN/P4 同场景实测。[实验脚本与完整记录](https://github.com/TwoEggDu/twoegg-tech-stack/tree/main/examples/version-management/branching-and-merging)

## 合并之前，先认清基线、两侧和结果

只复制两份工程目录，能保存文件，却未必保留足够的来源关系。判断一次合并，至少需要四个对象：

| 对象 | 需要明确什么 | 本篇称呼 |
| --- | --- | --- |
| 共同基线 | 两边从什么历史出发？ | base |
| 目标侧 | 本次在哪条分支上接收修改？ | main，当前 HEAD |
| 来源侧 | 要纳入哪条工作线的内容？ | feature |
| 合并结果 | 历史如何连接，最终文件和行为是什么？ | 合并提交与验收输出 |

三方合并利用共同基线与两侧变化形成结果。这里的 base、main、feature 是本次教学关系，不代表固定组织岗位。换个方向执行 merge，当前侧与来源侧也会改变。

先用命令核对关系，再判断怎样合入：

```powershell
git -C $repo status --short
git -C $repo log --graph --oneline --decorate --all
git -C $repo merge-base main feature
```

本次每个场景都从干净副本开始，只有一个明确的共同基线。复杂历史可能存在多个最佳共同祖先，不能把这份小实验当成所有历史的简化保证。[git merge-base](https://git-scm.com/docs/git-merge-base)

<details>
<summary>复跑入口与实验控制条件</summary>

需要 Python 3.11+ 与 Git。从本仓库根目录运行，两个输出目录必须是互不包含的新目录：

```powershell
python examples/version-management/branching-and-merging/run-lab.py `
  --lab-root E:/workspace/TechStackShow/.tmp/branch-merge-new `
  --evidence-directory E:/workspace/TechStackShow/.tmp/branch-merge-new-evidence
```

替换自己的绝对路径。脚本分别创建 `fast-forward`、`divergent`、`text-conflict`、`semantic` 仓库，隔离 Git 系统/全局配置，明确 `merge.ff=true`、`rerere.enabled=false`，使用空模板与空 hook 目录。不修改本项目分支、暂存区或远端。

下文按实际执行阶段摘录命令，`$repo` 指向该阶段对应的独立仓库；四个场景不在同一个目录串行制造。完整复跑使用上述脚本，它会保存每条命令的退出码与输出。

</details>

## 快进：main 可以直接指向已有提交

第一个仓库只在 feature 上提交一次伤害变化，main 仍停在基线。回到 main 后执行：

```powershell
git -C $repo switch main
git -C $repo merge --ff-only feature
git -C $repo rev-parse main
git -C $repo rev-parse feature
git -C $repo log --graph --oneline --decorate --all
```

本次结果为：

```text
* f8a65ef (HEAD -> main, feature) Feature damage
* 1e2a7e9 Baseline
```

main 与 feature 指向同一个已经存在的提交，仓库没有多出一次合并提交。`--ff-only` 明确要求能够快进，否则拒绝；它不是“所有合并都自动成功”的开关。[Git 的快进与合并选项](https://git-scm.com/docs/git-merge)

因此，“merge 成功”本身不能说明一定产生了一个新的双父提交，还要查看实际图和提交身份。

## 两侧都前进：普通合并记录两个父提交

第二个仓库从共同基线分开：main 把 `damage.rules` 改成 80，feature 把另一个 `charge.rules` 改成 on。两边各有独立提交后，从 main 执行普通 merge：

```powershell
git -C $repo switch main
git -C $repo merge --no-edit feature
git -C $repo show -s --format=%P HEAD
git -C $repo log --graph --oneline --decorate --all
Get-Content "$repo/damage.rules"
Get-Content "$repo/charge.rules"
```

本次形成的关系如下，图中省略了完整 SHA：

```text
        M  main：新的合并提交
       / \
      A   B  feature
       \ /
        O  共同基线
```

M 的第一父提交是合并前 main 的 A，第二父提交是来源 feature 的 B。最终文件同时包含 80 与 on，工作区干净，feature 仍指向原来的 B。操作改变了目标分支，没有顺便移动来源分支。

再检查来源提交是否已经可从目标历史到达：

```powershell
git -C $repo merge-base --is-ancestor feature main
```

实测退出 0。但可达性表达历史关系，不会替后面的业务检查作答。

## 文本冲突：把三个输入与解决结果分开看

第三个仓库让两边修改同一个文件、同一行。基线为 `damage=100`，main 改为 80，feature 改为 90。从 main 合并 feature：

```powershell
git -C $repo merge --no-edit feature
git -C $repo status --short
git -C $repo rev-parse HEAD
git -C $repo rev-parse MERGE_HEAD
git -C $repo ls-files -u
git -C $repo show :1:damage.rules
git -C $repo show :2:damage.rules
git -C $repo show :3:damage.rules
```

merge 按预期退出 1，HEAD 仍是合并前 main，MERGE_HEAD 指向 feature。工作文件有冲突标记，index 保存三个输入：

| index stage | 本次角色 | 实际内容 |
| --- | --- | --- |
| 1 | 共同基线 | `damage=100` |
| 2 | 当前 HEAD，即 main | `damage=80` |
| 3 | 来源 MERGE_HEAD，即 feature | `damage=90` |

这些角色对应本次普通 merge，不将同样的 ours/theirs 口头称呼直接套到其他操作。stage 内容可以用来核对输入，不只靠冲突标记附近的几行判断。[Git 冲突状态与 index stages](https://git-scm.com/docs/git-merge#_true_merge)

本场景在实验计划里已经约定最终值为 80。按约定写入结果，再显式暂存、提交：

```powershell
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText("$repo/damage.rules", "damage=80`n", $utf8)
git -C $repo add -- damage.rules
git -C $repo commit -m 'Resolve according to teaching spec: damage 80'
git -C $repo ls-files -u
git -C $repo show -s --format=%P HEAD
```

实测未合并条目清空，结果为 80，形成两父提交，来源分支保持不变。消除标记和完成 add 表示解决结果已经被登记；这个值为何应该是 80，仍来自业务约定。

这个例子也说明：feature 的 90 已经参与合并历史，最终文件却保留 80。**“来源提交可达”不能等同于“来源每一处内容都原样保留”。**

## 无文本冲突，为什么最终伤害仍然错误

主案例换成两个配置和一个小程序：

```text
base.json          {"damage": 100}
charge.json        {"percent": 100}
damage.py          raw = base * percent // 100；初始 final = raw
check_damage.py    固定验收器
```

实验开始前先写明规则：最终伤害在 0～150；最终值应为原始伤害按 150 封顶。集成后保留基础伤害 120 和倍率 140%，原始值仍需可观察。这是教学规则，不是对实际游戏平衡的建议。

基线与两侧分别运行同一个检查器：

```powershell
python -B "$repo/check_damage.py"
```

| 运行对象 | 基础伤害 | 倍率 | 原始 / 最终伤害 | 检查退出码 |
| --- | --- | --- | --- | --- |
| 共同基线 | 100 | 100% | 100 / 100 | 0 |
| main 单独修改基础伤害 | 120 | 100% | 120 / 120 | 0 |
| feature 单独修改倍率 | 100 | 140% | 140 / 140 | 0 |

两边都满足同一约束，也都经过了行为检查。随后从 main 合并 feature：

```powershell
git -C $repo switch main
git -C $repo merge --no-edit feature
git -C $repo ls-files -u
git -C $repo status --short
git -C $repo merge-base --is-ancestor feature main
python -B "$repo/check_damage.py"
```

本次 merge 退出 0，未合并条目为空，工作区干净，feature 也是 main 的祖先。两个配置文件都已进入结果，程序却输出：

```json
{"base": 120, "cap": 150, "final": 168, "percent": 140, "raw": 168}
```

检查器退出 1，报告 `FINAL_DAMAGE_OUT_OF_RANGE` 及最终值未按约定封顶。工具没有需要人解决的文本冲突，但程序已经违反规则。

这里的语义冲突来自两个变化的组合：基础伤害与倍率分别合理，组合后跨过边界。它不会因为文件不同、各自分支通过，或者提交图完整而自动消失。

## 适配合并结果，保留输入与失败证据

按预先写明的规则，这次需要保留两边配置，并在计算程序中显式处理最终上限：

```python
raw = base * percent // 100
final = min(raw, 150)
```

自动脚本只改 `damage.py`，没有改两个 JSON，也没有改 `check_damage.py`。适配作为单独提交记录，再运行原检查器：

```powershell
git -C $repo diff -- damage.py
git -C $repo add -- damage.py
git -C $repo commit -m 'Apply predeclared cap while preserving both configurations'
python -B "$repo/check_damage.py"
```

最终输出：

```json
{"base": 120, "cap": 150, "final": 150, "percent": 140, "raw": 168}
```

检查退出 0，验收器的字节 SHA256 与基线相同。本次通过来自计算逻辑按约定适配，没有放宽上限或删除失败检查。

教学记录保留自动合并 `11343ed…` 和其后的适配提交 `1b0dbf3…`，使失败与修正都可追查。它们只存在于本次临时仓库；没有把坏版本推送到团队分支。实际流程可以先建立合并候选，验收后再允许进入共同工作线，记录版本和团队接受是不同动作。

最后从本地目标仓库 clone 新目录，明确取得适配 SHA，再运行同一检查器。实测原始值仍为 168、最终值为 150，副本干净，所有文件散列与目标相同；feature 仍停在原来的来源提交。这补上了目标目录之外的独立取得与验收证据。

## SVN/P4 怎样对应这些问题

本篇 Git 实验不能直接认证 SVN mergeinfo 或 P4 的集成行为。下面只解释官方机制，具体操作仍应对照采用版本和真实路径范围核对。

| 工具 | 先明确什么 | 还要核对什么 |
| --- | --- | --- |
| Git | 目标、来源、共同祖先、合并策略 | 提交图、解决后的内容与目标行为 |
| SVN | 来源/目标仓库路径及要传播的修订范围 | 工作副本差异、mergeinfo、实际提交与目标行为 |
| P4 | 来源/目标文件或 Stream 关系、集成范围 | integrate/resolve 的结果、submit 后记录与目标行为 |

SVN 的分支可以通过带历史的目录复制建立；合并把变化应用到目标工作副本，再核对和提交。[SVN Book：Using Branches](https://svnbook.red-bean.com/en/1.7/svn.branchmerge.using.html)其 `svn:mergeinfo` 用版本化属性记录合并跟踪信息，需要留意路径与子树范围，不能只用一个修订号代替所有路径的核对。[Basic Merging](https://svnbook.red-bean.com/en/1.7/svn.branchmerge.basicmerging.html)两份资料都是 1.7 历史版本说明；本篇没有把它们标为当前客户端实测。

P4 的 `integrate` 发起来源到目标的变化传播，已有目标可能需要 resolve，正式提交继续经过 submit。[P4 2026.1 integrate](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_integrate.html)Streams 还定义工作线的视图和变化流动关系，不能把一个 Stream 简化为 Git 分支引用的别名。[p4 stream](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_stream.html)

Git 的 rebase、cherry-pick 以及其他合并策略不在本次运行范围；它们需要另行核对历史身份与变化传播，不能仅复制本篇普通 merge 的祖先判定作为全部验收。

## 回到工程：先核对传播，再验证组合行为

游戏工程中的语义冲突可能表现为配置组合越界、接口与调用方约定不一致，或者两项资源变化组合后超出预算。文本没有冲突，只减少了一类人工处理，不会替目标版本完成这些验收。

检查一个修复是否进入目标时，要沿来源、目标、解决时适配和最终行为逐层核对。本次既保存两父提交和来源可达性，也验证修正后的程序，并从新目录重新取得精确 SHA。删除、移动、二进制资源、复杂祖先和引擎依赖尚未覆盖，不能外推。

长期保留多少工作线、修复先进入哪里、冻结期怎样接受变化，继续阅读现有[交付分支策略]({{< relref "delivery-engineering/delivery-version-management-02-branching.md" >}})。本篇负责合并机制与证据边界，不重新定义发布节奏。

**合并连接历史，目标版本是否正确，还要由解决后的内容与组合行为共同证明。**
