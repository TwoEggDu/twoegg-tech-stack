---
title: "IL2CPP 实现分析索引｜Unity AOT runtime 的 8 篇管线拆解"
slug: "il2cpp-series-index"
date: "2026-04-15"
weight: 49
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp-index"
tags: [IL2CPP, Unity, AOT, Index]
---

IL2CPP 是 Unity 当前的主力 runtime。这 8 篇从完整管线、转换器、runtime 内部、metadata 格式、泛型代码生成、GC 集成、ECMA-335 覆盖度到代码裁剪，覆盖 IL2CPP 的核心机制。

## 文章列表

| # | 主题 | 一句话 |
|---|------|--------|
| D1 | [架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) | il2cpp.exe + libil2cpp + global-metadata 三件套的协作关系 |
| D2 | [il2cpp.exe 转换器：IL → C++ 代码生成策略]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}}) | 转换器怎么把 CIL 指令映射到 C++ 函数调用 |
| D3 | [libil2cpp runtime：MetadataCache、Class、Runtime 三层结构]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}}) | 运行时的类型初始化和方法分派入口 |
| D4 | [global-metadata.dat：格式、加载与 runtime 的绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) | metadata 文件的二进制布局和运行时查找路径 |
| D5 | [泛型代码生成：共享、特化与 Full Generic Sharing]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) | 引用类型共享 + 值类型特化的代码膨胀控制策略 |
| D6 | [GC 集成：BoehmGC 的接入层、write barrier 与 finalization]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 保守式 GC 在 AOT 环境下的集成方式 |
| D7 | [ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}}) | Reflection.Emit、动态程序集等缺失特性的技术原因 |
| D8 | [Managed Code Stripping：裁剪策略与 link.xml]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) | UnityLinker 的裁剪级别和保留规则 |

---

## 前置建议

这组文章假设读者已经了解 ECMA-335 的基本概念。如果对 CLI Metadata、CIL 指令集或 CLI Type System 还不熟悉，建议先读 [ECMA-335 基础层]({{< relref "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" >}}) 的 A1~A3。

## 延伸阅读

理解 IL2CPP 的限制后，可以接着看 HybridCLR 怎么在 IL2CPP 之上补回解释执行能力：

- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) — 37 篇，从 build-time 到 runtime 的完整链路
