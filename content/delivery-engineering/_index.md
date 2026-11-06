---
date: "2026-04-14"
title: "交付工程"
description: "从内容制作与功能研发，到运行验证和上线反馈：生产协作、资源管线、质量检查、多端构建与发布运营。"
hero_title: "把上线要求带进日常生产，让资源、配置与功能经过交接、修正和运行验证。"
---

这一栏沿内容制作、功能研发、跨工种交接、运行验证与上线反馈，讨论每一步的产出应满足什么要求，以及遇到问题怎样定位、修正和复查。

能够可靠提前发现的运行时问题，尽量在制作、开发、导出和提交时暴露。每项检查都有自己的输入与覆盖范围，完整版本仍需结合功能、内容表现、目标设备和部署环境验收；运行反馈再用于改进生产约定与检查。

## 选择阅读入口

- **[游戏研发 FDE：从技术机制到产品交付]({{< relref "delivery-engineering/fde-methods-series-index.md" >}})**：从 Missing、AOT 两个真实案例进入现场判断、工程落地与团队采用，再按问题阅读核心模块、引擎、操作系统与硬件。两篇基础与方法正文保持独立，案例与技术文章沿用原归属。

- **[版本管理：从 Git、SVN、P4 到团队协作]({{< relref "delivery-engineering/version-management-series-index.md" >}})**：从工作区、修改与历史进入任务提交、分支合并、资源协作、审查和构建基线。前六篇已有正文，其余 4 篇为提纲；三种工具模型、Git 任务提交及分支合并已实测，引擎待验证。

- **[从提交检查到修正复检]({{< relref "delivery-engineering/game-delivery-agent-series-index.md" >}})**：以 Missing 为例，研究现有工具、固定流程、人和模型的分工。第 01～05 篇正文已写，其余 3 篇为提纲；新增服务与适配器未实现，实验未执行。

- **[从美术制作与客户端协作开始]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})**：沿原画、角色、绑定、动画、场景资产、地编、特效和 TA，认识制作、接入与联合验收。12 篇正文已成稿，软件实操与素材待补。

- **[从一次游戏更新开始]({{< relref "delivery-engineering/unity-game-update-release-series-index.md" >}})**：用首次整包、线上修复、新英雄与活动串联版本绑定、构建范围、强更、热更与发布回退。首页与七篇章节提纲已建立，正文待写。

- **[游戏客户端生产体系]({{< relref "engineering-journey/production-pipeline-overview.md" >}})**：FDE 的第一组实践，从资源、配置与代码检查走向构建、并行版本和验收；五篇精选正文与一篇性能提纲，另有开发期检查和技术深读。
- **[按交付工程体系深入](#delivery-system-guide)**：从下方设计原则进入，按原有 19 卷结构与五条阅读线，系统查阅原理、实现和平台专题。

### 按具体问题继续阅读

- **资源与配置**：[资源完整性案例]({{< relref "engineering-journey/production-resource-completeness.md" >}}) → [配置与双端一致性]({{< relref "engineering-journey/production-config-consistency.md" >}})。
- **提交前与提交后的开发检查**：[Jenkins 客户端编译检查]({{< relref "engineering-journey/production-client-compile.md" >}})、[服务端编译检查]({{< relref "engineering-journey/production-server-compile.md" >}})、[鲁班与本地 hook]({{< relref "engineering-journey/production-config-table-format-checks.md" >}})。

两种入口共用已有文章：从具体问题进入实践，也可以按需回到专题理解机制与取舍。

<a id="delivery-system-guide"></a>

## 设计原则

这套专栏按三层模型组织：

- **原理层**（长青）：引擎无关，任何技术栈都适用
- **实践层**（当前技术栈）：用 Unity / C# 服务端做具体示例
- **平台层**（平台专项）：iOS / Android / 微信小游戏各自的差异

原理不绑引擎，实践用案例展示，平台规则变了就更新平台层。

## 19 卷结构

```
Part I   认知篇        V01 交付总论
Part II  供给篇        V02 内容生产与配置管线  ·  V03 工程基建
Part III 版本篇        V04 版本与分支管理
Part IV  资源篇        V05 资源管线  ·  V06 包体管理与分发
Part V   客户端交付篇   V07 多端构建  ·  V08 脚本热更新  ·  V09 平台发布
Part VI  服务端交付篇   V10 服务端架构与构建  ·  V11 服务端部署与运维  ·  V12 服务端版本与热更新
Part VII 质量篇        V13 验证与测试  ·  V14 性能与稳定性工程  ·  V15 缺陷闭环
Part VIII 运营篇       V16 CI/CD 管线  ·  V17 灰度上线与线上运营
Part IX  组织篇        V18 组织治理  ·  V19 案例与模板
```

## 五条阅读线

不同角色可以沿不同路径穿过这套专栏：

- **L1 交付工程原理线**：只读原理层，适合使用任何引擎的技术负责人
- **L2 Unity 客户端交付线**：原理 + Unity 实践 + 三端平台，主力路径
- **L3 服务端交付线**：原理 + C# 服务端实践，覆盖部署、运维、版本协调
- **L4 质量与治理线**：质量门 + 验证 + 缺陷闭环 + 组织治理，适合 QA 和技术管理者
- **L5 平台发布专项线**：三端发布 + 审核 + 合规，适合平台对接工程师

## 从哪里开始

如果你是第一次读，建议从 **V01 交付总论** 开始——它定义了整套专栏的术语、模型和阅读地图。
