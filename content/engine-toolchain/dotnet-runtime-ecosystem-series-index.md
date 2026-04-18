---
title: ".NET Runtime 生态全景系列｜从 ECMA-335 到 5 个 CLR 实现"
slug: "dotnet-runtime-ecosystem-series-index"
date: "2026-04-15"
description: "94 篇正文 + 8 个索引页（共 102 个文件），8 条阅读线，覆盖 ECMA-335 + CoreCLR + Mono + IL2CPP + HybridCLR + LeanCLR + 9 篇横切对比。"
weight: 1
featured: true
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "Mono"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Architecture"
series: "dotnet-runtime-ecosystem"
series_id: "index"
---
> 同一份 ECMA-335 规范，5 个 runtime 做了 5 套不同的实现决策。把这些决策拆开对比，就是这个系列的全部。

![四条阅读线](../../images/runtime-ecosystem/reading-lines-overview.svg)

---

## 按角色选入口

| 你是谁 | 推荐路径 |
|--------|---------|
| **Unity 工程师**（做热更新） | ECMA-335 → [IL2CPP]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) → [HybridCLR]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) |
| **Runtime 工程师**（想理解 CLR 实现） | ECMA-335 → [CoreCLR]({{< relref "engine-toolchain/coreclr-series-index.md" >}}) → [Mono]({{< relref "engine-toolchain/mono-series-index.md" >}}) → [LeanCLR]({{< relref "engine-toolchain/leanclr-series-index.md" >}}) |
| **H5/小游戏开发者** | ECMA-335 → [LeanCLR F1]({{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}) → [F9 WASM]({{< relref "engine-toolchain/leanclr-webassembly-build-h5-minigame-embedding.md" >}}) → [F10 选型]({{< relref "engine-toolchain/leanclr-vs-hybridclr-two-routes-same-team.md" >}}) → [G8 体积]({{< relref "engine-toolchain/runtime-cross-size-embedding-50mb-to-300kb.md" >}}) |
| **做技术选型的人** | ECMA-335 → [横切对比 G1~G9]({{< relref "engine-toolchain/runtime-cross-series-index.md" >}}) |

---

## 按专题深入

| 专题 | 路径 | 篇数 |
|------|------|------|
| **GC 专题** | [A6 Memory Model]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) → [B5 CoreCLR GC]({{< relref "engine-toolchain/coreclr-gc-generational-precise-workstation-server.md" >}}) → [C4 SGen]({{< relref "engine-toolchain/mono-sgen-gc-precise-generational-nursery.md" >}}) → [D6 BoehmGC]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) → [Bridge-F GC 模型]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}}) → [G4 GC 对比]({{< relref "engine-toolchain/runtime-cross-gc-implementation-generational-conservative-cooperative.md" >}}) | 6 |
| **泛型专题** | [A3 Type System]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) → [A7 共享规则]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) → [B7 CoreCLR 泛型]({{< relref "engine-toolchain/coreclr-generics-sharing-specialization-canon.md" >}}) → [D5 IL2CPP 泛型]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) → [G5 泛型对比]({{< relref "engine-toolchain/runtime-cross-generic-implementation-sharing-specialization-fgs.md" >}}) | 5 |
| **编译器与执行引擎** | [A2 CIL 栈机]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) → [Bridge-E 解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}}) → [B4 RyuJIT]({{< relref "engine-toolchain/coreclr-ryujit-il-to-ir-to-native-code.md" >}}) → [C3 Mini JIT]({{< relref "engine-toolchain/mono-mini-jit-il-to-ssa-to-native.md" >}}) → [F3 双解释器]({{< relref "engine-toolchain/leanclr-dual-interpreter-hl-ll-transform-pipeline.md" >}}) → [G3 执行对比]({{< relref "engine-toolchain/runtime-cross-method-execution-jit-aot-interpreter-hybrid.md" >}}) | 6 |
| **热更新专题** | [A5 Assembly]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}}) → [B2 ALC]({{< relref "engine-toolchain/coreclr-assembly-loading-assemblyloadcontext-binder.md" >}}) → [HybridCLR 37 篇]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) → [G7 加载对比]({{< relref "engine-toolchain/runtime-cross-assembly-loading-hot-update-comparison.md" >}}) | 40+ |

---

## 各模块入口

| 模块 | 篇数 | 定位 |
|------|------|------|
| [**ECMA-335 基础层**]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) | 14 | 所有阅读线的公共地基。术语、metadata、CIL、类型系统、执行模型、程序集、内存模型、泛型共享、verification、custom attributes、P/Invoke、security、threading、PE 文件格式 |
| [**CoreCLR**]({{< relref "engine-toolchain/coreclr-series-index.md" >}}) | 10 | .NET 主线 runtime。架构、ALC、MethodTable、RyuJIT、GC、异常、泛型、线程、Reflection、Tiered Compilation |
| [**Mono**]({{< relref "engine-toolchain/mono-series-index.md" >}}) | 8 | Unity 第一代 runtime。架构、解释器、Mini JIT、SGen GC、类型加载、方法编译、AOT、Unity 转型 |
| [**IL2CPP**]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) | 8 | Unity AOT runtime。管线、转换器、libil2cpp、metadata.dat、泛型、GC、ECMA 覆盖度、裁剪 |
| [**HybridCLR**]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) | 37 | IL2CPP 热更补丁。原理、工具链、AOT 泛型、案例、商业功能、DHE、加密 |
| [**LeanCLR**]({{< relref "engine-toolchain/leanclr-series-index.md" >}}) | 10 | 600KB 独立 CLR。调研、metadata、双解释器、对象模型、类型系统、方法链、内存、icall、WASM、对比 |
| [**横切对比**]({{< relref "engine-toolchain/runtime-cross-series-index.md" >}}) | 9 | 5 runtime 同维度对比。metadata、类型系统、执行、GC、泛型、异常、加载、体积、P/Invoke |

---

**94 篇正文 + 8 个索引页 · 29 张图 · 5 个 runtime · 8 条阅读线**
