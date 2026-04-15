---
title: "横切对比索引｜同一个问题在 5 个 runtime 里的不同答案"
slug: "runtime-cross-series-index"
date: "2026-04-15"
weight: 79
series: "dotnet-runtime-ecosystem"
series_id: "cross-index"
tags: [Comparison, Architecture, Index]
---

这 9 篇文章不深入某个 runtime，而是横切对比同一个 ECMA-335 概念在 5 个 runtime 里的不同实现决策。适合做技术选型或想建立全局判断框架的读者。

## 文章列表

| # | 主题 | 对比维度 |
|---|------|----------|
| G1 | [Metadata 解析：5 个 runtime 怎么读同一份 .NET DLL]({{< relref "engine-toolchain/runtime-cross-metadata-parsing-five-runtimes.md" >}}) | 解析策略、缓存策略、惰性加载 |
| G2 | [类型系统实现：MethodTable vs Il2CppClass vs RtClass]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}}) | 内存布局、VTable 设计、泛型膨胀 |
| G3 | [方法执行：JIT vs AOT vs Interpreter vs 混合执行]({{< relref "engine-toolchain/runtime-cross-method-execution-jit-aot-interpreter-hybrid.md" >}}) | 编译/解释策略的 trade-off |
| G4 | [GC 实现：分代精确 vs 保守式 vs 协作式 vs stub]({{< relref "engine-toolchain/runtime-cross-gc-implementation-generational-conservative-cooperative.md" >}}) | 4 种 GC 策略的工程取舍 |
| G5 | [泛型实现：共享 vs 特化 vs Full Generic Sharing]({{< relref "engine-toolchain/runtime-cross-generic-implementation-sharing-specialization-fgs.md" >}}) | 代码膨胀 vs 运行时性能 |
| G6 | [异常处理：两遍扫描 vs setjmp/longjmp vs 解释器展开]({{< relref "engine-toolchain/runtime-cross-exception-handling-seh-setjmp-interpreter.md" >}}) | 异常模型与平台约束 |
| G7 | [程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载]({{< relref "engine-toolchain/runtime-cross-assembly-loading-hot-update-comparison.md" >}}) | 热更新能力与隔离机制 |
| G8 | [体积与嵌入性：从 50MB CoreCLR 到 300KB LeanCLR]({{< relref "engine-toolchain/runtime-cross-size-embedding-50mb-to-300kb.md" >}}) | 裁剪策略与嵌入成本 |
| G9 | [P/Invoke 与 native interop：5 个 runtime 的原生互操作策略]({{< relref "engine-toolchain/runtime-cross-pinvoke-native-interop-comparison.md" >}}) | 绑定时机、动态加载、callback 机制 |

---

## 前置建议

横切对比需要对规范层和至少一个 runtime 有基本了解。建议至少读过 [ECMA-335 基础层]({{< relref "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" >}}) 的 A1~A3，以及任意一个 runtime 模块（IL2CPP / HybridCLR / LeanCLR / CoreCLR / Mono）的第一篇总览。
