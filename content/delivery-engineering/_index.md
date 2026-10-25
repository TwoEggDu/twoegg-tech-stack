---
date: "2026-04-14"
title: "交付工程"
description: "从内容冻结到三端上线的完整闭环：资源管线、多端构建、质量门禁、发布运营与组织治理。"
hero_title: "交付不是发布。从代码到产品稳定运行在用户设备上，中间每一环都是工程问题。"
---

这一栏把游戏项目从内容冻结到稳定运行在用户设备上的完整交付链路拆开讲清楚。

不只讲"怎么打包"，而是讲整条链路上每一环的工程设计——出了什么问题会怎样，应该怎样预防和恢复。

## 选择阅读入口

- **[从美术制作与客户端协作开始]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})**：沿原画、角色、绑定、动画、场景资产、地编、特效和 TA，认识制作、接入与联合验收。第 01—05 篇已有正文，其余 7 篇为提纲。

- **[从一次游戏更新开始]({{< relref "delivery-engineering/unity-game-update-release-series-index.md" >}})**：用首次整包、线上修复、新英雄与活动串联版本绑定、构建范围、强更、热更与发布回退。首页与七篇章节提纲已建立，正文待写。

- **[从生产管线实践开始]({{< relref "engineering-journey/production-pipeline-overview.md" >}})**：先读第 0 节《从解决问题到形成生产飞轮》，看资源、配置与代码检查怎样连接跨工种协作，并逐步积累可复用的生产能力。
- **[按交付工程体系深入](#delivery-system-guide)**：从下方设计原则进入，按原有 19 卷结构与五条阅读线，系统查阅原理、实现和平台专题。

### 按具体问题继续阅读

- **资源与配置**：[资源完整性案例]({{< relref "engineering-journey/production-resource-completeness.md" >}}) → [配置与双端一致性]({{< relref "engineering-journey/production-config-consistency.md" >}})。
- **提交后的开发检查**：[客户端编译检查]({{< relref "engineering-journey/production-client-compile.md" >}})、[服务端编译检查]({{< relref "engineering-journey/production-server-compile.md" >}})、[配置表格式检查]({{< relref "engineering-journey/production-config-table-format-checks.md" >}})。

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
