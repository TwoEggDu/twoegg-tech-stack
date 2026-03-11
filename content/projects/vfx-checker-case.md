+++
title = "特效性能检查器案例"
description = "用特效性能检查器案例展示资源门禁、规则化检查和发布前质量控制。"
weight = 10
featured = true
tags = ["VFX", "Quality Gate", "Projects"]
+++

## 一句话总结

这条经历最好的包装不是“做过特效工具”，而是：

`参与特效性能检查器与特效资源门禁建设，围绕特效 Layer、粒子 Mesh 丢失、最大粒子数、粒子系统依赖 FBX 可读写等规则，把高风险特效问题前置到发布前检查流程。`

## 这条经历为什么值钱

- 它不是孤立的美术工具，而是发布前质量门禁。
- 它不是单点渲染优化，而是把特效问题规则化、可检查化、部分可修复化。
- 它同时证明你理解美术资源、粒子系统、渲染成本和交付流程。

## 当前能证明它的直接证据

- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResProcess\ResourceReport.VFX.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResProcess\ResourceReport.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\Prefab_MaxParticlesCheck.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\Prefab_ParticleMeshMissing.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\PrefabHelper.cs`
- `E:\HT\docs\reference\PX项目-资源检查系统分析.md`

这些文件能支撑的点包括：

- 特效 Layer 统一设置和 TransparentFX 规范检查
- 粒子 Mesh 丢失检查
- 最大粒子数检查
- 粒子系统依赖的 FBX 可读写检查
- 检查结果被串进统一资源检查主流程
- 部分规则具备自动修复入口

## 简历怎么写

推荐版本：

`参与特效性能检查器与特效资源门禁建设，围绕特效 Layer 规范、粒子 Mesh 丢失、最大粒子数、粒子系统依赖 FBX 可读写等规则，把高风险特效问题前置到发布前资源检查流程，并支持部分自动修复。`

更偏负责人视角的版本：

`推动特效资源从人工经验治理走向规则化门禁，降低高成本特效问题在联调、提测和上线阶段暴露的概率。`

## 面试怎么讲

推荐讲法：

`我做的不是一个给特效同学点按钮的小工具，而是把特效资源里最容易导致性能、渲染和发布问题的几个点做成了检查规则，让它们在发布前就被发现。这样特效问题不会等到联调、提测或者线上才暴露。`

## 最值得补的量化数据

- 检查覆盖了多少特效 Prefab
- 平均一轮能拦下多少问题
- 自动修复覆盖了哪些规则
- 这套检查减少了多少返工或提测前问题

## 和 GitHub 开源的关系

如果你要做公开仓库，这条经历最适合抽成一个脱敏后的通用样例，而不是直接公开公司代码。

更完整的开源路线见：

- [docs/vfx-checker-open-source-plan.md](vfx-checker-open-source-plan.md)