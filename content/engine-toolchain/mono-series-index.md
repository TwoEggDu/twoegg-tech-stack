---
title: "Mono 实现分析索引｜Unity 第一代 runtime 的 8 篇深度拆解"
slug: "mono-series-index"
date: "2026-04-15"
weight: 59
series: "dotnet-runtime-ecosystem"
series_id: "mono-index"
tags: [Mono, CLR, Unity, Index]
---

Mono 是 Unity 的第一代 runtime，也是理解 IL2CPP 转型的必经坐标。这 8 篇覆盖架构、JIT、解释器、GC、AOT、类型加载、方法编译，以及 Unity 最终转向 IL2CPP 的决策分析。

## 前置建议

先完成 [ECMA-335 基础层]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。Mono 的类型加载、JIT、GC 都是对规范层概念的具体实现，读过规范再看实现会更清楚每个模块在解决什么问题。

## 推荐阅读顺序

| # | 文章 | 一句话定位 |
|---|------|-----------|
| C1 | [架构总览：从嵌入式 runtime 到 Unity 集成]({{< relref "engine-toolchain/mono-architecture-overview-embedded-runtime-unity.md" >}}) | Mono 的嵌入式设计与 Unity 的集成方式 |
| C2 | [解释器（mint/interp）：与 LeanCLR 双解释器的对比]({{< relref "engine-toolchain/mono-interpreter-mint-interp-vs-leanclr.md" >}}) | 两代解释器的演进与跨 runtime 对比 |
| C3 | [Mini JIT：IL → SSA → native 的编译管线]({{< relref "engine-toolchain/mono-mini-jit-il-to-ssa-to-native.md" >}}) | Mono JIT 的核心管线：前端 → SSA → 寄存器分配 → 后端 |
| C3.5 | [类型加载：MonoClass 的初始化链路与 metadata 解析]({{< relref "engine-toolchain/mono-type-loading-monoclass-initialization.md" >}}) | 从 metadata token 到 MonoClass 的完整加载过程 |
| C4 | [SGen GC：精确式分代 GC 与 nursery 设计]({{< relref "engine-toolchain/mono-sgen-gc-precise-generational-nursery.md" >}}) | 分代精确 GC 的 nursery + major 两代设计 |
| C4.5 | [方法编译与执行：从 MonoMethod 到 native code 或解释器]({{< relref "engine-toolchain/mono-method-compilation-execution-dispatch.md" >}}) | 方法的分派路径：JIT 编译 vs 解释器执行 |
| C5 | [AOT：Full AOT 与 LLVM 后端]({{< relref "engine-toolchain/mono-aot-full-aot-llvm-backend.md" >}}) | 提前编译模式与 LLVM 后端的集成 |
| C6 | [Mono 在 Unity 中的角色：为什么最终转向了 IL2CPP]({{< relref "engine-toolchain/mono-unity-role-why-il2cpp-replaced.md" >}}) | Mono 的局限与 Unity 转向 IL2CPP 的技术决策 |

## 延伸阅读

读完 C6 之后，自然的下一步是看 IL2CPP 怎么解决 Mono 的限制：

- [IL2CPP 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) — D1，IL2CPP 的整体架构
- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) — IL2CPP 之上的热更方案
- [CoreCLR 实现分析索引]({{< relref "engine-toolchain/coreclr-series-index.md" >}}) — 对比 Mono 与 CoreCLR 在同一层面的不同实现决策
