---
title: "无尽对决"
description: "以两人合作副本为起点，把玩法、系统、内容制作、设计模式和工程交付连成完整实践。"
date: "2026-10-25"
url: "/practice/cooperative-dungeon/"
type: "reading-guide"
reading_kind: "project"
cooperative_project: true
project_hub: true
implementation_routes:
  - name: "Unity"
    role: "完整项目主线"
    description: "先在 Editor 跑通制作与游戏流程，再逐步推进完整副本、版本迭代、多平台与运营准备。"
    focus: "ET · Luban · YooAsset · HybridCLR · UGUI"
  - name: "Unreal"
    role: "关键系统对照"
    description: "围绕共同规则与验收场景，研究对象组织、技能、内容制作和生命周期，再逐步扩展。"
    focus: "技术选型与实现正文待补充"
  - name: "Piccolo"
    role: "机制研究与最小实现"
    description: "先核对已有机制与能力缺口，再选择关键系统完成最小实验，记录扩展与验证边界。"
    focus: "能力盘点与实现正文待补充"
---

<a id="workflow"></a>

## 策划、美术与程序怎样共同制作

项目同时研究游戏怎样运行、内容怎样进入游戏。即使早期由同一人兼任角色，也按真实交接完成制作和验收。

| 角色 | 计划提供的独立操作 | 共同验收 |
| --- | --- | --- |
| 策划 | 配置技能、怪物、关卡与奖励，检查并进入指定内容预览 | 规则、数据版本、两端结果与异常路径 |
| 美术 | 在兼容约定内替换模型、动画、特效与 UI | 资源依赖、时序、绑定、视觉反馈与生命周期 |
| 程序与技术美术 | 提供行为能力、数据和资源约定、预览与检查工具 | 新能力可配置，错误可定位，制作流程可重复 |
| 测试与版本负责人 | 组织双人流程、候选版本与回归 | 版本、环境、实际结果和验证边界可追溯 |

这些是制作能力目标，具体工具和运行结果尚待实现与验证。

<a id="patterns"></a>

## 在同一个项目里学习设计模式

从直接方案开始，引入一次需求变化或失败，再比较候选方案，解释为什么采用或不用某个模式，最后通过实现、验证和迁移练习检验判断。

首个候选案例是技能目标规则的演进；之后结合怪物状态、事件通知、UI、对象创建、资源适配与生产流程展开。模式是设计选项，项目不以使用数量作为完成目标。

[进入设计模式教科书]({{< relref "system-design/design-patterns-textbook-series-index.md" >}}) · [阅读设计总览中的教学安排]({{< relref "practice/cooperative-dungeon/design-overview.md#9-让设计模式教学沿真实变化展开" >}})

<a id="related"></a>

## 沿着共同案例进入各个专栏

- [GamePlay]({{< relref "reading/gameplay.md" >}})：合作体验、战斗、敌人、关卡与奖励。
- [全栈框架]({{< relref "reading/frameworks.md" >}})：客户端、服务端、配置、同步与数据。
- [工程与交付]({{< relref "reading/delivery.md" >}})：工种协作、构建、版本、更新与恢复。
- [引擎研发]({{< relref "reading/engine.md" >}})：进入 Unity、Unreal、Piccolo 的机制与实现研究。
- [性能优化]({{< relref "reading/performance.md" >}})：从运行负载与目标设备建立预算、测量和取舍。

设计和独立教学材料公开，Demo 工程保持私有。设计初稿、实现进度与运行验证分别记录；已有机制文章不能替代本项目的实际验收。
