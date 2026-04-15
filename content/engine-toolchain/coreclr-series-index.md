---
title: "CoreCLR 实现分析索引｜.NET 主线 runtime 的 10 个核心模块"
slug: "coreclr-series-index"
date: "2026-04-15"
weight: 39
series: "dotnet-runtime-ecosystem"
series_id: "coreclr-index"
tags: [CoreCLR, CLR, Index]
---

CoreCLR 是 .NET 的参考实现。这 10 篇从架构总览、类型系统、JIT、GC、异常处理到 Tiered Compilation，覆盖 CoreCLR 的核心子系统。

## 前置建议

先完成 [ECMA-335 基础层]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。CoreCLR 的每个模块都对应规范层的一个概念，读过规范再看实现会顺畅很多。

## 推荐阅读顺序

| # | 文章 | 一句话定位 |
|---|------|-----------|
| B1 | [架构总览：从 dotnet run 到 JIT 执行]({{< relref "engine-toolchain/coreclr-architecture-overview-dotnet-run-to-jit.md" >}}) | 启动链路全景，理解 host → runtime → JIT 的分层 |
| B2 | [程序集加载：AssemblyLoadContext、Binder 与卸载支持]({{< relref "engine-toolchain/coreclr-assembly-loading-assemblyloadcontext-binder.md" >}}) | 程序集的发现、绑定与隔离机制 |
| B3 | [类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}}) | 类型在内存中的三层表示结构 |
| B4 | [RyuJIT：从 IL → IR → native code 的编译管线]({{< relref "engine-toolchain/coreclr-ryujit-il-to-ir-to-native-code.md" >}}) | JIT 编译器的核心管线和优化 pass |
| B5 | [GC：分代精确 GC、Workstation vs Server、POH]({{< relref "engine-toolchain/coreclr-gc-generational-precise-workstation-server.md" >}}) | 分代回收、两种模式与 Pinned Object Heap |
| B6 | [异常处理：两遍扫描模型与 SEH 集成]({{< relref "engine-toolchain/coreclr-exception-handling-two-pass-seh-integration.md" >}}) | managed 异常与 OS 结构化异常的集成方式 |
| B7 | [泛型实现：代码共享、特化与 System.__Canon]({{< relref "engine-toolchain/coreclr-generics-sharing-specialization-canon.md" >}}) | 引用类型共享 + 值类型特化的实现策略 |
| B8 | [线程与同步：Thread、Monitor、ThreadPool]({{< relref "engine-toolchain/coreclr-threading-synchronization-thread-monitor-threadpool.md" >}}) | managed 线程模型与同步原语实现 |
| B9 | [Reflection 与 Emit：运行时类型查询与动态代码生成]({{< relref "engine-toolchain/coreclr-reflection-emit-dynamic-code-generation.md" >}}) | 反射元数据查询与动态 IL 生成 |
| B10 | [Tiered Compilation：多级 JIT、动态降级与 PGO]({{< relref "engine-toolchain/coreclr-tiered-compilation-tier0-tier1-pgo.md" >}}) | Tier0 快速编译 → Tier1 优化编译 → PGO 反馈的渐进策略 |

## 对比阅读

CoreCLR 的每个子系统在其他 runtime 中都有对应实现。读完某个模块后，可以跳到横切对比系列查看差异：

- [G1 Metadata 解析对比]({{< relref "engine-toolchain/runtime-cross-metadata-parsing-five-runtimes.md" >}}) — 对应 B2
- [G2 类型系统对比]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}}) — 对应 B3
- [G3 方法执行对比]({{< relref "engine-toolchain/runtime-cross-method-execution-jit-aot-interpreter-hybrid.md" >}}) — 对应 B4
- [G4 GC 实现对比]({{< relref "engine-toolchain/runtime-cross-gc-implementation-generational-conservative-cooperative.md" >}}) — 对应 B5
- [G5 泛型实现对比]({{< relref "engine-toolchain/runtime-cross-generic-implementation-sharing-specialization-fgs.md" >}}) — 对应 B7
- [G6 异常处理对比]({{< relref "engine-toolchain/runtime-cross-exception-handling-seh-setjmp-interpreter.md" >}}) — 对应 B6
