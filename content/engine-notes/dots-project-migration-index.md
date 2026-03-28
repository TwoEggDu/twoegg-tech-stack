---
title: "Unity DOTS 项目落地与迁移｜系列索引"
slug: "dots-project-migration-index"
date: "2026-03-28"
draft: true
description: "什么时候该上 DOTS，Hybrid 边界怎么切，第一阶段先迁哪层，怎样验证收益、接进构建链并控制升级风险。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Architecture"
  - "Engineering"
  - "Index"
series: "Unity DOTS 项目落地与迁移"
series_id: "unity-dots-project-migration"
series_role: "index"
series_order: 0
series_nav_order: 54
series_title: "Unity DOTS 项目落地与迁移"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "技术负责人"
  - "主程 / 架构"
series_level: "进阶"
series_best_for: "当你想判断项目里到底该不该上 DOTS，以及如果要上，第一刀应该切哪层"
series_summary: "把 DOTS 选型、Hybrid 边界、第一阶段迁移、验证链、工程化和升级风险放回一条项目落地主线。"
series_intro: "这组文章不讲“DOTS API 怎么用”，而是讲项目里到底该不该引入 DOTS，以及引入之后怎样长期活下去。它先建立选型判断和 Hybrid 边界，再进入第一阶段迁移、验证链、构建与 CI、版本升级这些真正决定成败的工程问题。"
series_reading_hint: "第一次读建议先看 M01（该不该上）和 M03（Hybrid 边界），再看 M02（第一阶段怎么迁）。M05 和 M06 负责把迁移从观点收束到证据链与工程链，M04 最后处理升级维护期的高风险问题。"
weight: 2200
---

这组文章面向的是“已经知道 DOTS 是什么，但还没决定要不要在项目里用它”的读者。

它真正要处理的不是 API，而是工程判断：什么问题空间值得上 DOTS，哪些系统绝对不该先迁，MonoBehaviour 和 ECS 的边界要怎么切，怎样证明这次引入真的有收益。

建议按这条路径进入：

1. `M01` 先建立选型判断，不让整组文章默认站在“必须上 DOTS”的立场上。
2. `M03` 和 `M02` 再把 Hybrid 边界和第一阶段迁移路径接起来。
3. `M05`、`M06`、`M04` 最后收回到验证、工程链和升级风险。

{{< series-directory >}}
