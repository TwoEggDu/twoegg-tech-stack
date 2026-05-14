---
title: "Harness Engineering 06｜Harness 在交付工程里的位置"
slug: "harness-engineering-06-in-delivery-engineering"
date: "2026-05-13"
description: "在多端交付闭环里，Harness 跟构建脚本、产物校验、发布门禁怎么衔接。交付工程视角下，AI 能接手哪些可重复任务、哪些必须人接。"
tags:
  - "Harness Engineering"
  - "Delivery Engineering"
  - "CI/CD"
  - "AI Engineering"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 60
weight: 2160
---

> **读这篇之前**：本篇假设你已经读过 [01]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}})。如果你想了解交付工程主线，先看 [交付工程旗舰专栏]({{< relref "delivery-engineering" >}})——本篇只讲 Harness 跟交付工程的接口。

## 这篇解决什么问题

交付工程关心的事情是：怎样把代码、资源、配置稳定地变成可以发布的产物，然后稳定地发到各个平台。

Harness 关心的事情是：怎样让 AI 在明确边界内稳定接活。

两者交集是：**交付工程里有大量可重复、规则稳定、失败模式明确的任务，理论上是 AI 接活的好场景，但它们有一组独有约束**——产物大、构建慢、平台多、验证滞后。

外面讲 Harness 的文章基本都假设任务是"写一段代码、跑一次单测"。交付工程的任务是"改一段配置、等夜构、第二天看烟测结果"——根本不是同一种节奏。

这篇要回答的是：

- 交付工程视角下哪些任务适合 Harness 化
- 哪些必须人接、为什么
- Verify 在异步场景下怎么设计
- Harness 跟构建系统（Jenkins / 各自家构建工具）怎么接口

## 交付工程的任务谱

交付工程的任务大致分四类：

### 类型一：高频低风险

- 例：改一个 BuildConfig、加一个新平台的资源压缩参数
- 适合 Harness 化吗：适合
- 怎么 Harness 化：规则化"改这类参数前要扫一遍 affected platforms"

### 类型二：高频高风险

- 例：改 Addressables 分组、改 Shader stripping 配置
- 适合 Harness 化吗：部分
- 怎么 Harness 化：AI 可以提出改动建议，但执行 / 应用必须有人 review

### 类型三：低频低风险

- 例：升级一个不影响构建的 dev 工具
- 适合 Harness 化吗：不太值得
- 怎么 Harness 化：可以但 ROI 低，先把高频的做了

### 类型四：低频高风险

- 例：升级 Unity 大版本、迁移构建系统、换 CDN
- 适合 Harness 化吗：不适合
- 怎么 Harness 化：这类任务必须人主导，AI 只做辅助查询和起草

<!-- EXPERIENCE-TODO: 每个类型补一两个真实例子。Zhulong 跨项目构建调度框架里应该有大量类型一和二的例子。 -->

## Verify 在异步场景下的设计

通用 Harness 的 Verify 是"跑测试、看结果、闭环"。交付工程的 Verify 经常不能闭环：

- 改了构建参数 → 要等下一次构建（半小时到几小时）
- 改了资源加载策略 → 要等真机测试（半天到一天）
- 改了 CDN 配置 → 要等灰度上线（一天到一周）

这种情况 Harness 状态机要扩一下：

```text
Intake → Context → Plan → Execute → Verify(Sync) → Async-Verify-Queued → Handoff
                                          │
                                          └─ 如果同步 verify 失败，回 Execute
                                          
                                  Async-Verify-Queued → Async-Verify-Result → Learn
                                  （等待外部反馈，可能数小时到数天后才到）
```

异步 verify 的关键设计：

### 设计一：把 Async-Verify 显式建模

- 不是"任务完成"，而是"任务进入等待"
- Harness 必须知道"这次任务什么时候算真的结束"

### 设计二：失败时能定位回当时的修改

- 异步 verify 失败可能是几天前的修改导致的
- 必须把"哪次修改 → 哪次构建 → 哪次失败"链条保留

### 设计三：失败回流要专门设计

- 同步失败回到 Execute 容易；异步失败回去时项目可能已经走远了
- 需要在 Memory 层沉淀"这类延迟反馈的修改要更小心"

<!-- DATA-TODO: 补一个真实的异步 verify 失败案例——改了什么、等了多久、怎么定位回去的。 -->

## Harness 跟构建系统的接口

Harness 不应该自己跑构建，应该跟现有构建系统接口。三个常见接口点：

### 接口一：构建触发

- AI 改了构建相关代码后，自动触发一次"快速 sanity build"
- 不跑全平台、不跑全资源，只验证基础构建链路没断
- 失败 → 立即回 Execute 阶段；成功 → 进入 Async-Verify-Queued

### 接口二：构建产物校验

- 构建完成后跑一组校验：包大小是否异常、关键资源是否存在、签名是否正确
- 这些都可以是机械化的 CI check
- Harness 只需要在"AI 改了关键路径"时确保这些 check 被触发

### 接口三：失败日志回流

- 构建失败的日志要回流到 Harness Memory
- 不是直接喂给 AI——而是被人审一遍后沉淀成规则
- 例：本月已经第 3 次因为 X 配置错误导致 iOS 构建失败 → 写一条规则

<!-- EXPERIENCE-TODO: 用 Zhulong 的真实接口设计举例——能公开化的部分。重点是"AI 改完代码后 Zhulong 怎么自动决定要不要触发 sanity build"。 -->

## 哪些必须人接

不是所有交付任务都能 Harness 化。下面这些必须人接：

### 必须人接 1：发版决策

- 这一版发不发、灰度比例、回滚预案
- 这是商业决策，不是工程决策

### 必须人接 2：跨团队协调

- 美术资源 / 策划配置 / 运维部署的对接
- 涉及人和人之间的协商

### 必须人接 3：第一次走某条新路径

- 第一次接一个新平台、第一次用一种新 SDK
- 走通后再考虑 Harness 化

### 必须人接 4：紧急修复

- 线上事故的快速止血
- Harness 流程会拖慢响应

<!-- EXPERIENCE-TODO: 补一段"我自己怎么判断 Harness 化 vs 人接"的实际心得。最好是某次本想 Harness 化但及时撤回的例子。 -->

## 跟交付工程其他文章的联动

本篇是 Harness 视角看交付工程。如果你想从交付工程视角理解全貌：

- [交付总论 01｜发布 vs 交付——为什么"包能打出来"不等于"产品能上线"]({{< relref "delivery-engineering" >}})
- 后续根据 delivery-engineering 系列发布情况补完整 relref

<!-- DATA-TODO: 等 delivery-engineering 系列的具体篇目稳定后，补 2-3 处具体 relref。 -->

## 收束

交付工程是 Harness 落地的高价值场景——任务多、规则稳、失败模式明确。但它有自己独特的节奏（异步、平台多、反馈滞后），通用 Harness 状态机不够用。

最短结论是：

**交付工程里的 Harness，重点不是让 AI 写得快，而是让 AI 知道什么时候自己的工作"还没真的完成"。**

下一篇 [07｜Harness 在长线运营里的位置]({{< relref "harness-engineering/harness-engineering-07-in-live-ops.md" >}}) 切到运营场景。

<!-- DATA-TODO: 等真实跑过 2-3 次"AI 改交付配置 + 异步 verify"任务后，把这篇所有 TODO 都填实。 -->
