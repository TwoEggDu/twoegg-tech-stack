# Article Card 00｜Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host

> 来源基线：`Agent Engineering 课程规划 v3.1（入口与结构校正版评审稿）`。本卡保留其 16 项课程设计；M1 已完成取证，具体陈述强度以 [Evidence Register](evidence.md) 与 [Definition Matrix](definition-matrix.md) 为准。

## 1. 本篇定位

课程导论与导航篇。回答“我打开 Claude Code、Codex、DeepSeek Harness 或普通 LLM App 时，应该从哪些系统层次理解它”，只建立地图，不提前穷举定义。

## 2. 为什么现在学它

目标读者有软件工程基础，却可能同时听过 Agent、Agentic、Copilot、Runtime、Harness、Tool、Skill、RAG 和 Memory。如果没有地图，课程从 Model API 开始虽正确，却容易让读者不知道这些底层能力最终长成什么。

## 3. 学完以后应该能回答的问题

- LLM / Model 与使用它的 AI Application 有什么区别？
- Copilot 为什么是产品术语，而不是统一架构标准或 Agent 的固定前置阶段？
- Agent 与 Agentic 分别描述什么？
- Agent Runtime、Harness、Host / Product 大概位于哪一层？
- Prompt、Context、Tool、Skill、Workflow、Memory、RAG 大概放在哪里？
- 后续课程为什么仍要从 Model 开始重新学习？

## 4. 前置知识

不要求 Agent 前置知识；只需具备普通应用、API 和状态的基本概念。

## 5. 核心概念

LLM / Model、AI Application、AI Feature、Copilot、Agent、Agentic、Agent Runtime、Harness、Host / Product、Prompt、Context、Tool、Skill、Workflow、Memory、RAG。

## 6. 核心心智模型

以下是 M1 确立的课程导航提案，不是产品实现断言，也不是行业统一部署拓扑：

```text
User Goal
   ↓
Host / Product：CLI / Web / IDE / Unity Editor / CI
   ↓
Harness：Runtime 周围的可复用工程控制与约束（课程工作定义）
   ↓
Agent Runtime：Model Call / Tool Dispatch / Loop / State / Stop（课程工作定义）
   ↓
Model + Tool + State
   ↓
External World
```

## 7. 正文详细框架

这是 v3.1 的范围基线，不代表 Detailed Outline 已完成：

1. 从一个普通 LLM App 开始：对照普通问答功能与 Tool-using Application。
2. Copilot、Agent 与 Agentic：建立产品定位、执行系统与描述性术语的初步边界。
3. Runtime、Harness 与 Host：建立执行内核、横切工程能力和产品入口的初步位置关系。
4. 横向能力放在哪里：只定位 Prompt、Context、Tool、Skill、Workflow、Memory 与 RAG。
5. 地图不是术语百科：不靠产品名称反推统一架构，并桥接到 Model API。

## 8. Engineering Lab / 示例

不设 Lab。只用普通模型调用、Claude Code、Codex 与 DeepSeek Harness 的公开事实帮助定位；不根据产品入口或名称猜内部实现。

## 9. 与 DeepSeek Harness 的关系

本篇只引用 DSH 官方“open-source agent harness / developer preview”等公开定位，不把它强行映射为课程的 Runtime / Harness / Product 分层；28—37 再根据 pinned commit、源码位置与运行证据判断。

## 10. 与 BuildPilot 的关系

BuildPilot 在本篇只作为未来的专用工程 Agent 例子，不介绍架构，不启动实现。

## 11. Evidence 要求

地图级通用概念使用一手资料；课程 working definition 明确标 `PROPOSAL`；具体产品只陈述公开事实与版本边界，不凭名称断言内部层次。

## 12. 最容易混淆的概念

需持续保留的边界清单：Model / Application、Copilot / Agent、Agentic / 架构类型、Runtime / Harness、Harness / Host、RAG / Memory。

## 13. 本篇明确不讲什么

不讲 Agent Loop 细节，不列 Harness 能力全集，不定义各类 Memory 生命周期，不做产品横评。

## 14. 学习检查

- 一个调用模型做摘要的按钮为什么未必是 Agent？
- Agentic Workflow 是否一定包含一个独立 Agent Runtime？
- CLI 与 Agent Runtime 是同一层吗？
- 为什么看完地图后仍需从 Model API 开始？

## 15. 篇幅等级 / 课程权重

`S（Bridge / Overview）`。只负责降低术语进入门槛并提供全课导航，不穷举机制。

## 16. 概念成熟度

`Introduction`。Agent、Agentic、Copilot、Runtime、Harness、Host 以及横向能力均只建立位置感，正式机制留给后续文章。
