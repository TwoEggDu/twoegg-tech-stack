---
title: "LeanCLR 实现分析索引｜600KB CLR 的 10 篇源码拆解"
slug: "leanclr-series-index"
date: "2026-04-15"
weight: 69
series: "dotnet-runtime-ecosystem"
series_id: "leanclr-index"
tags: [LeanCLR, CLR, WebAssembly, Index]
---

LeanCLR 是 HybridCLR 同一团队从零构建的轻量级 CLR。这 10 篇从调研报告、metadata 解析、双解释器、对象模型、类型系统、方法调用链、内存管理、internal calls、WebAssembly 构建到 HybridCLR 对比，覆盖完整源码。

## 文章列表

| # | 主题 | 一句话 |
|---|------|--------|
| F1 | [调研报告：架构总览与源码地图]({{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}) | 73K LOC 的全景地图和模块划分 |
| F2 | [Metadata 解析：CliImage、RtModuleDef 与 ECMA-335 表]({{< relref "engine-toolchain/leanclr-metadata-parsing-cli-image-module-def.md" >}}) | 从 PE 文件到内存中的类型定义 |
| F3 | [双解释器架构：HL-IL → LL-IL 的三级 transform 管线]({{< relref "engine-toolchain/leanclr-dual-interpreter-hl-ll-transform-pipeline.md" >}}) | 182 条 HL-IL 到 298 条 LL-IL 的降级策略 |
| F4 | [对象模型：RtObject、RtClass、VTable 与单指针头设计]({{< relref "engine-toolchain/leanclr-object-model-rtobject-rtclass-vtable.md" >}}) | 对象头只有一个 class 指针的极简方案 |
| F5 | [类型系统：泛型膨胀、接口分派与值类型判断]({{< relref "engine-toolchain/leanclr-type-system-generic-inflation-interface-dispatch.md" >}}) | 泛型实例化和接口方法查找的实现路径 |
| F6 | [方法调用链：从 Assembly.Load 到 Interpreter::execute]({{< relref "engine-toolchain/leanclr-method-invocation-chain-assembly-load-to-execute.md" >}}) | 一次方法调用经过的完整代码路径 |
| F7 | [内存管理：MemPool arena、GC 接口设计与精确协作式 GC]({{< relref "engine-toolchain/leanclr-memory-management-mempool-gc-interface.md" >}}) | arena 分配 + GC stub 的架构意图 |
| F8 | [Internal Calls 与 Intrinsics：61 个 icall 和 BCL 适配策略]({{< relref "engine-toolchain/leanclr-internal-calls-intrinsics-bcl-adaptation.md" >}}) | 最小 BCL 的 icall 注册和调用机制 |
| F9 | [WebAssembly 构建与 H5 小游戏嵌入]({{< relref "engine-toolchain/leanclr-webassembly-build-h5-minigame-embedding.md" >}}) | 从 C++ 源码到浏览器运行的完整链路 |
| F10 | [LeanCLR vs HybridCLR：同一团队的两条技术路线]({{< relref "engine-toolchain/leanclr-vs-hybridclr-two-routes-same-team.md" >}}) | 独立 CLR vs IL2CPP 补丁的架构对比 |

---

## 前置建议

这组文章假设读者已经了解 ECMA-335 的基本概念。建议先读 [ECMA-335 基础层]({{< relref "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" >}}) 的 A1~A3。

## H5/小游戏快速路径

如果只关心 LeanCLR 在 H5/小游戏场景的落地，建议只读三篇：F1（全景）→ F9（WASM 构建）→ F10（选型对比）。

## 对比阅读

- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) — 同一团队的另一条路线
- [体积与嵌入性：从 50MB CoreCLR 到 300KB LeanCLR]({{< relref "engine-toolchain/runtime-cross-size-embedding-50mb-to-300kb.md" >}}) — G8 横切对比
