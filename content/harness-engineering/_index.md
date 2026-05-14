---
date: "2026-05-13"
title: "Harness Engineering"
description: "v0 之后——当 AI Coding Harness 在游戏引擎客户端这种带历史包袱的场景下跑起来，怎样让它活下来、长出来、瘦下去。"
hero_title: "Harness 不该越堆越厚。它该长肌肉，也该会瘦身。"
---

这一栏不讲怎么搭一个 AI Coding Harness 的 v0——那是 [AI 赋能 08｜我如何搭建自己的 AI Coding Harness Engineering]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 的事。

这一栏讲的是**v0 跑起来之后**：

- 在游戏引擎客户端这种带历史包袱的场景下，Harness 长什么样
- 怎样判断它现在处于 Bootstrap / Growth / Bloat / Drift / Sunset 哪个阶段
- 什么时候该停下来不再扩展、什么时候该主动瘦身
- 同一份 SDK vendor 到多个项目时，Harness 信息归谁
- 跟交付工程、长线运营怎样接

## 核心模型

```text
v0 实施（见 ai-empowerment 08）
        │
        ▼
游戏引擎客户端的独有约束
        │
        ▼
五阶段生命周期：Bootstrap → Growth → Bloat → Drift → Sunset
        │
        ├── Bloat 反模式与瘦身
        ├── Drift 与文档腐烂
        ├── 跨仓库作用域（SDK vendor）
        ├── 与交付工程联动
        ├── 与长线运营联动
        └── 复盘与指标
```

## 三条阅读线

- **领域差异化线**（01）：为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness
- **演化与诊断主轴**（02-04）：五阶段生命周期、Bloat 反模式、Drift 与文档腐烂
- **场景联动与复盘**（05-08）：跨仓库 SDK、交付工程、长线运营、复盘与指标

## 与其他栏目的关系

[AI 赋能 · 08]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 是通用 v0 实施指南——五层模型、状态机、五指标。本栏目假设你已经读过它。

[代码质量到工程质量]({{< relref "code-quality" >}}) 讲的是质量判断、CI、Quality Gate；本栏目只在 Harness 跟这些机制的接口处讨论它。

[交付工程]({{< relref "delivery-engineering" >}}) 讲多端交付闭环；本栏目 06 篇专门讨论 Harness 在交付场景的位置。

[长线运营工程]({{< relref "live-ops-engineering" >}}) 讲长线运营场景；本栏目 07 篇专门讨论 Harness 在运营场景的位置。
