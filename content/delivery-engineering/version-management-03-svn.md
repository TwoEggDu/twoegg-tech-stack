---
title: "版本管理 03｜SVN 的工作模型：工作副本、仓库修订与更新"
slug: "version-management-03-svn"
date: "2026-10-29"
description: "用两个工作副本交错提交和更新的命令行实验，区分 SVN 仓库全局修订、路径工作修订、最后修改修订与本地修改。"
draft: false
content_stage: "article"
weight: 3963
tags: ["版本管理", "SVN"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 3
reading_title: "SVN 的工作模型"
---

“我已经提交到 r2，整个目录就是 r2 了吧？”“日志里有 r3，我这边也没有本地修改，为什么还缺别人的配置？”

这两个问题都需要分开查看仓库与工作副本。**仓库有一个全局修订序列，工作副本却可以让不同路径停在不同的工作修订上。** 提交成功、目录干净、已经取得最新内容，分别回答不同的问题。

[共同模型]({{< relref "delivery-engineering/version-management-01-model.md" >}})把记录版本与取得版本分开。本篇用两个文件、两个工作副本走完提交、单文件更新、完整更新和本地再修改，观察编号与内容如何变化。

> 实测范围：2026-10-30，SVN / svnadmin `1.14.1`、Windows PowerShell `5.1.19041.6456`；一个本地 `file:///` 仓库、两个协作副本和一个干净对照副本，19 项检查通过。未验证网络服务、权限、冲突或游戏行为。[实验脚本与完整记录](https://github.com/TwoEggDu/twoegg-tech-stack/tree/main/examples/version-management/svn-work-model)

## 先认清三个修订字段

设仓库中有两个教学文件：

```text
damage.rules    damage=100
charge.rules    charge=off
```

每次成功提交在这个仓库中产生一个新的全局修订，表示提交后的仓库树。r3 可以包含“r2 改过的伤害文件”和“r3 才改过的蓄力文件”；每个文件不需要都在 r3 被改写。

查看本地路径时，还要区分两个字段：

| 对象或字段 | 回答什么 | 本文如何查看 |
| --- | --- | --- |
| 仓库最新修订 | 这个仓库最近成功记录到哪里？ | 实验管理员用 `svnlook youngest` |
| 工作修订，`Revision` | 本地这个路径以哪个修订为基准？ | `svn info`、`svn status -v` |
| 最后修改修订，`Last Changed Rev` | 这个路径最后在哪次提交中变化？ | `svn info`、`svn status -v` |

工作副本还允许保留尚未提交的修改。此时工作修订是比较基准，磁盘内容可以已经不同于基准。[SVN Book 的工作副本与混合修订说明](https://svnbook.red-bean.com/en/1.7/svn.basic.in-action.html)

上面的参考来自 SVN Book 1.7 稳定版，用于解释基础模型；下文具体输出来自本次 1.14.1 客户端。`svnlook` 直接访问仓库数据库，普通团队成员通常没有这样的服务器磁盘入口，本实验因为自己创建本地仓库才使用它。

<details>
<summary>准备：创建本地仓库与两个 r1 工作副本</summary>

下面从本仓库根目录执行。需要 PATH 中有 `svn`、`svnadmin`、`svnlook` 和 `svnversion`。每条命令确认成功后再继续；自动检查退出码、隔离配置和保存证据请运行配套脚本。

```powershell
$labRoot = Join-Path (Get-Location) '.tmp/svn-work-model-manual'
if (Test-Path -LiteralPath $labRoot) { throw '请换一个新的实验目录' }
New-Item -ItemType Directory -Path "$labRoot/seed" | Out-Null
$repository = Join-Path $labRoot 'repository'
$url = ([Uri]($repository.Replace('\','/') + '/')).AbsoluteUri.TrimEnd('/')
$wcA = Join-Path $labRoot 'wc-a'
$wcB = Join-Path $labRoot 'wc-b'
$wcR3 = Join-Path $labRoot 'wc-r3'
$utf8 = New-Object System.Text.UTF8Encoding($false)

svnadmin create $repository
[IO.File]::WriteAllText("$labRoot/seed/damage.rules", "damage=100`n", $utf8)
[IO.File]::WriteAllText("$labRoot/seed/charge.rules", "charge=off`n", $utf8)
svn import "$labRoot/seed" $url -m 'Baseline' --username vm-lab
svn checkout -r 1 $url $wcA
svn checkout -r 1 $url $wcB
```

初始导入产生 r1。两个副本都取得这份仓库树；没有网络服务，也没有使用公司仓库。折叠段用于手动理解，完整脚本另为每条 SVN 命令传入独立 `--config-dir`、`--non-interactive` 与 `--no-auth-cache`。

</details>

## A 提交一个文件，为什么出现 1:2

A 先把伤害改成 80，只提交这个文件：

```powershell
[IO.File]::WriteAllText("$wcA/damage.rules", "damage=80`n", $utf8)
svn diff "$wcA/damage.rules"
svn commit "$wcA/damage.rules" -m 'Reduce damage'
svn status $wcA
svn status -v $wcA
svnversion -n $wcA
```

`diff` 是供手动提交前检查的命令；本次自动脚本保存文件内容并对状态断言。提交结果为 r2，普通 `status` 没有输出，`svnversion` 却为 `1:2`。

这不是提交失败。该次提交让 `damage.rules` 的工作修订前进到 2，未顺便把其他路径更新到 r2。本次 `status -v` 的关键字段如下，路径简化展示：

```text
工作修订  最后修改修订  作者    路径
1         1             vm-lab  wc-a
1         1             vm-lab  wc-a/charge.rules
2         2             vm-lab  wc-a/damage.rules
```

混合修订描述的是各路径基准不同；本地是否有待提交修改则由另一组状态表达。这个副本既是混合修订，又没有本地修改。`svnversion` 的区间压缩表达工作修订的范围，不是说每个路径都“属于 1 和 2 两个版本”。[svnversion 说明](https://svnbook.red-bean.com/en/1.7/svn.ref.svnversion.re.html)

## B 提交 r3，不会更新 A 的文件

B 仍从初始副本出发，只修改另一个文件：

```powershell
[IO.File]::WriteAllText("$wcB/charge.rules", "charge=on`n", $utf8)
svn commit "$wcB/charge.rules" -m 'Enable charge'
svnlook youngest $repository
Get-Content "$wcA/charge.rules"
svnversion -n $wcA
```

本次两个提交修改不同文件，没有发生冲突或过期拒绝。仓库现在最新是 3；A 的蓄力文件仍为 `charge=off`，`svnversion` 仍为 `1:2`。

这里能直接看出：**A 的 `svn status` 为空，只说明没有它会报告的本地改动，不说明 A 已取得仓库最新内容。** 普通 `status` 默认不联网询问更新状态；别人提交后，自己的磁盘也不会自动改变。

## 只更新一个文件，内容齐了，修订仍可能混合

A 先只取回 B 的蓄力改动，再检查整个副本：

```powershell
svn update "$wcA/charge.rules"
svn info --xml -R $wcA
svn status -v $wcA
svnversion -n $wcA
```

这次更新只针对 `charge.rules`。仓库没有新提交，目标取得 r3。此时 A 的两个文件内容已经与仓库 r3 相同，但路径工作修订分别是根目录 1、伤害文件 2、蓄力文件 3，`svnversion` 返回 `1:3`。

把这一轮状态并排看，会比单看“更新到修订 3”更清楚：

| A 所处阶段 | 仓库最新 | 根 / damage / charge 工作修订 | svnversion | A 内容 damage / charge |
| --- | --- | --- | --- | --- |
| 刚取得基线 | 1 | 1 / 1 / 1 | `1` | 100 / off |
| A 提交伤害后 | 2 | 1 / 2 / 1 | `1:2` | 80 / off |
| B 提交蓄力后 | 3 | 1 / 2 / 1 | `1:2` | 80 / off |
| A 只更新蓄力文件 | 3 | 1 / 2 / 3 | `1:3` | 80 / on |
| A 完整更新 | 3 | 3 / 3 / 3 | `3` | 80 / on |

只有这两个文件时，可以逐份比较并确认内容相同。换成真实工程，根目录一个编号、某条命令的最后一句输出，都不足以说明其他路径实际取得了什么。

## 完整更新后，工作修订 3 与最后修改 2 可以同时成立

接着更新 A 的整个副本：

```powershell
svn update $wcA
svn info "$wcA/damage.rules"
svn status -v $wcA
svnversion -n $wcA
```

本次副本没有稀疏路径、switch 或 externals，完整更新后所有实验路径工作修订均为 3，`svnversion` 为 `3`。伤害文件的关键字段为：

```text
Revision: 3
Last Changed Rev: 2
```

伤害内容仍为 80。工作修订前进表示这个路径的基准已经对应仓库 r3；r3 没改它，所以最后修改修订仍为 2。把第二个字段当成“尚未更新到 r3”的证据，就会错误地要求一个没有变化的文件再次产生提交。[svn info 字段说明](https://svnbook.red-bean.com/en/1.7/svn.ref.svn.c.info.html)

## 明确取得 r3，再加一个本地修改作对照

新建第三个副本，明确选择 r3：

```powershell
svn checkout -r 3 $url $wcR3
Get-Content "$wcR3/damage.rules"
Get-Content "$wcR3/charge.rules"
svnversion -n $wcR3
```

实测内容为 `damage=80`、`charge=on`，与 A 完整更新后相同，`svnversion` 为 `3`。这是一次指定仓库状态的干净取得，没有依赖 A 之前做过哪些局部更新。

现在只在 A 的磁盘上把伤害改为 70：

```powershell
[IO.File]::WriteAllText("$wcA/damage.rules", "damage=70`n", $utf8)
svn status $wcA
svnversion -n $wcA
svn cat -r 3 "$url/damage.rules"
```

结果分别是 `M` 状态、`3M` 和仓库中的 `damage=80`。工作修订仍是 3，仓库也没有因此产生 r4。`M` 表示本地修改，不能被省略成一句“我在 r3”。

## 放回游戏工程，先固定范围，再解释编号

在 Unity 或 Unreal 项目里，代码、配置和资源可能由不同的人提交。只更新一个脚本，未必取得对应的配置或资源；仅看到某个目录更新到新编号，也不能替代其他路径的状态检查。

这次实验给出的检查顺序是：先确认工作副本对应的仓库 URL 与路径范围，再查看本地修改、各路径工作修订及所需内容。需要干净对照时，在新目录明确取得目标修订，避免用当前混合副本替代基线。

真实项目还可能有稀疏检出、切换路径、externals 和未版本化生成物。它们会改变“整个工程”的实际范围，本文没有实验验证；完整构建输入的固定留到[第 09 篇]({{< relref "delivery-engineering/version-management-09-build-baseline.md" >}})。分支传播、冲突与合并也不由本次两个独立文件提交证明，后续进入[分支与合并]({{< relref "delivery-engineering/version-management-06-branching-and-merging.md" >}})。

**仓库修订描述一棵历史树；判断本地工程，还要看路径基准与磁盘修改。**
