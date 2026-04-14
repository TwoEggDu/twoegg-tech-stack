---
title: ".NET Runtime 生态全景系列索引｜从 ECMA-335 到 5 个 CLR 实现的完整知识体系"
date: "2026-04-14"
description: "从 ECMA-335 规范出发，系统拆解 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五大 CLR 实现的架构决策与工程 trade-off。86 篇文章，4 条阅读线，覆盖类型系统、执行模型、GC、泛型、热更新全链路。"
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

这是 .NET Runtime 生态全景系列的总入口。86 篇文章，覆盖 ECMA-335 规范层 + 5 个 CLR 实现 + 8 篇横切对比。

```
                    ECMA-335 规范层
                         │
           ┌─────────────┼─────────────┐
           │             │             │
      Type System    Metadata     Execution
           │             │             │
     ┌─────┴─────┐ ┌─────┴─────┐ ┌─────┴─────┐
     │           │ │           │ │           │
  CoreCLR    Mono  IL2CPP  HybridCLR  LeanCLR
     │           │ │           │ │           │
     └─────┬─────┘ └─────┬─────┘ └─────┬─────┘
           │             │             │
        JIT 路线     AOT 路线     Interpreter 路线
```

## 四条阅读线

不需要从头到尾顺读。根据你的角色选一条线进入：

| 线 | 适合谁 | 路径 |
|----|--------|------|
| **线 1：Unity 工程师线** | 做热更新的人 | ECMA-335 → IL2CPP → HybridCLR |
| **线 2：Runtime 工程师线** | 想理解 CLR 实现的人 | ECMA-335 → CoreCLR → Mono → LeanCLR |
| **线 3：H5/小游戏线** | WebAssembly/小游戏开发者 | ECMA-335 → LeanCLR（尤其 F9 WASM） |
| **线 4：架构对比线** | 做技术选型的人 | ECMA-335 → 横切对比 G1~G8 |

所有线共享同一个 ECMA-335 基础层。

---

## 模块 A：ECMA-335 基础层（7 篇）

> 所有阅读线的公共入口。不讲任何 runtime 实现，只讲规范本身。

| # | 主题 |
|---|------|
| A1 | [CLI Metadata 基础：TypeDef、MethodDef、Token、Stream]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) |
| A2 | [CIL 指令集与栈机模型：ldloc、add、call]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) |
| A3 | [CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) |
| A4 | [CLI Execution Model：方法调用约定、虚分派、异常处理模型]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}}) |
| A5 | [CLI Assembly Model：程序集身份、版本策略与加载模型]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}}) |
| A6 | [CLI Memory Model：对象布局、GC 契约与 finalization 语义]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) |
| A7 | [IL2CPP 泛型共享规则：引用类型共享 object，值类型为什么不能]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) |

---

## 模块 B：CoreCLR 实现分析（10 篇）

> 最主流的 .NET runtime，JIT 路线的标杆实现。

| # | 主题 |
|---|------|
| B1 | [架构总览：从 dotnet run 到 JIT 执行]({{< relref "engine-toolchain/coreclr-architecture-overview-dotnet-run-to-jit.md" >}}) |
| B2 | [程序集加载：AssemblyLoadContext、Binder 与卸载支持]({{< relref "engine-toolchain/coreclr-assembly-loading-assemblyloadcontext-binder.md" >}}) |
| B3 | [类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}}) |
| B4 | [RyuJIT：从 IL → IR → native code 的编译管线]({{< relref "engine-toolchain/coreclr-ryujit-il-to-ir-to-native-code.md" >}}) |
| B5 | [GC：分代精确 GC、Workstation vs Server、POH]({{< relref "engine-toolchain/coreclr-gc-generational-precise-workstation-server.md" >}}) |
| B6 | [异常处理：两遍扫描模型与 SEH 集成]({{< relref "engine-toolchain/coreclr-exception-handling-two-pass-seh-integration.md" >}}) |
| B7 | [泛型实现：代码共享、特化与 System.__Canon]({{< relref "engine-toolchain/coreclr-generics-sharing-specialization-canon.md" >}}) |
| B8 | [线程与同步：Thread、Monitor、ThreadPool]({{< relref "engine-toolchain/coreclr-threading-synchronization-thread-monitor-threadpool.md" >}}) |
| B9 | [Reflection 与 Emit：运行时类型查询与动态代码生成]({{< relref "engine-toolchain/coreclr-reflection-emit-dynamic-code-generation.md" >}}) |
| B10 | [Tiered Compilation：多级 JIT、动态降级与 PGO]({{< relref "engine-toolchain/coreclr-tiered-compilation-tier0-tier1-pgo.md" >}}) |

---

## 模块 C：Mono 实现分析（6 篇）

> Unity 的第一代 runtime，跨平台先驱。

| # | 主题 |
|---|------|
| C1 | [架构总览：从嵌入式 runtime 到 Unity 集成]({{< relref "engine-toolchain/mono-architecture-overview-embedded-runtime-unity.md" >}}) |
| C2 | [解释器（mint/interp）：与 LeanCLR 双解释器的对比]({{< relref "engine-toolchain/mono-interpreter-mint-interp-vs-leanclr.md" >}}) |
| C3 | [Mini JIT：IL → SSA → native 的编译管线]({{< relref "engine-toolchain/mono-mini-jit-il-to-ssa-to-native.md" >}}) |
| C4 | [SGen GC：精确式分代 GC 与 nursery 设计]({{< relref "engine-toolchain/mono-sgen-gc-precise-generational-nursery.md" >}}) |
| C5 | [AOT：Full AOT 与 LLVM 后端]({{< relref "engine-toolchain/mono-aot-full-aot-llvm-backend.md" >}}) |
| C6 | [Mono 在 Unity 中的角色：为什么最终转向了 IL2CPP]({{< relref "engine-toolchain/mono-unity-role-why-il2cpp-replaced.md" >}}) |

---

## 模块 D：IL2CPP 实现分析（8 篇）

> Unity 当前的 AOT runtime。

| # | 主题 |
|---|------|
| D1 | [架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) |
| D2 | [il2cpp.exe 转换器：IL → C++ 代码生成策略]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}}) |
| D3 | [libil2cpp runtime：MetadataCache、Class、Runtime 三层结构]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}}) |
| D4 | [global-metadata.dat：格式、加载与 runtime 的绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) |
| D5 | [泛型代码生成：共享、特化与 Full Generic Sharing]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) |
| D6 | [GC 集成：BoehmGC 的接入层、write barrier 与 finalization]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) |
| D7 | [ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}}) |
| D8 | [Managed Code Stripping：裁剪策略与 link.xml]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) |

---

## 模块 E：HybridCLR 系列（37 篇）

> IL2CPP 的热更补丁方案。这是系列中最完整的模块。

→ [HybridCLR 系列独立索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})

包含：前置篇(2) + 桥接篇(4) + 主线(25) + 商业功能(6) = 37 篇

---

## 模块 F：LeanCLR 实现分析（10 篇）

> 轻量级独立 CLR，零依赖嵌入式方案。

| # | 主题 |
|---|------|
| F1 | [调研报告：架构总览与源码地图]({{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}) |
| F2 | [Metadata 解析：CliImage、RtModuleDef 与 ECMA-335 表]({{< relref "engine-toolchain/leanclr-metadata-parsing-cli-image-module-def.md" >}}) |
| F3 | [双解释器架构：HL-IL → LL-IL 的三级 transform 管线]({{< relref "engine-toolchain/leanclr-dual-interpreter-hl-ll-transform-pipeline.md" >}}) |
| F4 | [对象模型：RtObject、RtClass、VTable 与单指针头设计]({{< relref "engine-toolchain/leanclr-object-model-rtobject-rtclass-vtable.md" >}}) |
| F5 | [类型系统：泛型膨胀、接口分派与值类型判断]({{< relref "engine-toolchain/leanclr-type-system-generic-inflation-interface-dispatch.md" >}}) |
| F6 | [方法调用链：从 Assembly.Load 到 Interpreter::execute]({{< relref "engine-toolchain/leanclr-method-invocation-chain-assembly-load-to-execute.md" >}}) |
| F7 | [内存管理：MemPool arena、GC 接口设计与精确协作式 GC]({{< relref "engine-toolchain/leanclr-memory-management-mempool-gc-interface.md" >}}) |
| F8 | [Internal Calls 与 Intrinsics：61 个 icall 和 BCL 适配策略]({{< relref "engine-toolchain/leanclr-internal-calls-intrinsics-bcl-adaptation.md" >}}) |
| F9 | [WebAssembly 构建与 H5 小游戏嵌入]({{< relref "engine-toolchain/leanclr-webassembly-build-h5-minigame-embedding.md" >}}) |
| F10 | [LeanCLR vs HybridCLR：同一团队的两条技术路线]({{< relref "engine-toolchain/leanclr-vs-hybridclr-two-routes-same-team.md" >}}) |

---

## 模块 G：横切对比（8 篇）

> 同一个 ECMA-335 概念，在 5 个 runtime 里的不同实现决策。

| # | 主题 |
|---|------|
| G1 | [Metadata 解析：5 个 runtime 怎么读同一份 .NET DLL]({{< relref "engine-toolchain/runtime-cross-metadata-parsing-five-runtimes.md" >}}) |
| G2 | [类型系统实现：MethodTable vs Il2CppClass vs RtClass]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}}) |
| G3 | [方法执行：JIT vs AOT vs Interpreter vs 混合执行]({{< relref "engine-toolchain/runtime-cross-method-execution-jit-aot-interpreter-hybrid.md" >}}) |
| G4 | [GC 实现：分代精确 vs 保守式 vs 协作式 vs stub]({{< relref "engine-toolchain/runtime-cross-gc-implementation-generational-conservative-cooperative.md" >}}) |
| G5 | [泛型实现：共享 vs 特化 vs Full Generic Sharing]({{< relref "engine-toolchain/runtime-cross-generic-implementation-sharing-specialization-fgs.md" >}}) |
| G6 | [异常处理：两遍扫描 vs setjmp/longjmp vs 解释器展开]({{< relref "engine-toolchain/runtime-cross-exception-handling-seh-setjmp-interpreter.md" >}}) |
| G7 | [程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载]({{< relref "engine-toolchain/runtime-cross-assembly-loading-hot-update-comparison.md" >}}) |
| G8 | [体积与嵌入性：从 50MB CoreCLR 到 300KB LeanCLR]({{< relref "engine-toolchain/runtime-cross-size-embedding-50mb-to-300kb.md" >}}) |

---

## 数字一览

| 指标 | 数值 |
|------|------|
| 总篇数 | 86 |
| 覆盖的 runtime | 5（CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR） |
| ECMA-335 基础层 | 7 篇 |
| 横切对比 | 8 篇 |
| 阅读线 | 4 条 |
| SVG 图表 | 16 张 |

## 收束

这个系列不是要把所有 runtime 的源码都讲一遍。它的目标是一件事：

`同一份 ECMA-335 规范，5 个 runtime 做了 5 套不同的实现决策——把这些决策拆开，读者就能在面对任何 CLR 相关的技术问题时，知道问题属于哪一层、各个 runtime 在这一层做了什么选择、每个选择的 trade-off 是什么。`
