---
title: "ECMA-335 基础层索引｜CLI 规范的 8 个核心主题"
slug: "ecma335-series-index"
date: "2026-04-15"
weight: 8
series: "dotnet-runtime-ecosystem"
series_id: "ecma335-index"
tags: [ECMA-335, CLR, Index]
---

这 8 篇文章不讲任何 runtime 实现，只讲 ECMA-335 规范本身。它们是所有 runtime 模块的公共地基。

## 推荐阅读顺序

| # | 文章 | 一句话定位 |
|---|------|-----------|
| A0 | [术语约定：runtime、toolchain、metadata、execution engine 的边界]({{< relref "engine-toolchain/ecma335-terminology-conventions.md" >}}) | 统一 86 篇文章的核心术语边界，消除歧义 |
| A1 | [CLI Metadata 基础：TypeDef、MethodDef、Token、Stream]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) | metadata 表和 token 的编码规则，所有 runtime 解析 DLL 的起点 |
| A2 | [CIL 指令集与栈机模型：ldloc、add、call]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) | 规范定义的指令集和栈机语义，JIT/AOT/解释器的共同输入 |
| A3 | [CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) | 类型系统的规范定义，理解各 runtime 类型加载差异的基准 |
| A4 | [CLI Execution Model：方法调用约定、虚分派、异常处理模型]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}}) | 调用约定和异常模型的规范层定义 |
| A5 | [CLI Assembly Model：程序集身份、版本策略与加载模型]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}}) | 程序集的身份标识与版本绑定规则 |
| A6 | [CLI Memory Model：对象布局、GC 契约与 finalization 语义]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) | 对象布局和 GC 契约的规范约束，各 runtime GC 实现的共同约束 |
| A7 | [IL2CPP 泛型共享规则：引用类型共享 object，值类型为什么不能]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) | 泛型共享的规范基础，对比各 runtime 的共享策略差异 |

## 读完后可以进入

- [CoreCLR 实现分析索引]({{< relref "engine-toolchain/coreclr-series-index.md" >}}) — .NET 主线 runtime，JIT 路线的标杆实现
- [Mono 实现分析索引]({{< relref "engine-toolchain/mono-series-index.md" >}}) — Unity 第一代 runtime，理解 IL2CPP 转型的必经坐标
- [IL2CPP 实现分析]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) — Unity 当前 AOT runtime，从 D1 架构总览开始
- [LeanCLR 调研报告]({{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}) — 轻量级独立 CLR，从 F1 调研报告开始
- [横切对比 G1~G8]({{< relref "engine-toolchain/runtime-cross-metadata-parsing-five-runtimes.md" >}}) — 同一概念在 5 个 runtime 中的不同实现决策
