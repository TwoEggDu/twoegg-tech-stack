---
title: "IL2CPP 实现分析索引｜运行时地图、核心机制与产物排查"
slug: "il2cpp-series-index"
type: "reading-guide"
layout: "engine-directory"
reading_kind: "runtime-index"
topic_key: "engine"
reading_title: "IL2CPP 实现分析"
description: "从 C# 到 native，沿转换器、元数据与运行时理解 Unity AOT 的执行链路。"
date: "2026-04-15"
lastmod: "2026-10-25"
weight: 49
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp-index"
tags: [IL2CPP, Unity, AOT, Index]
---

本系列按“产物地图 → 八篇核心机制 → 产物排查”组织，共 10 篇正文。先建立 metadata、native code 与 runtime 的分工，再进入 IL2CPP 的构建与执行链路，最后衔接 HybridCLR。

## 前置知识

建议先完成 [ECMA-335 前置主线]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。读 D3 前区分 [类型信息准备与静态初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})，读 D5 前确认 [签名与泛型上下文]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

## 推荐阅读顺序

先读 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})，再按 D1～D8 阅读。遇到启动失败或文件混用时，跳到后面的产物排查专题。

## 核心机制

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

## 按问题查阅

| 问题 | 阅读入口 |
|---|---|
| 分不清 metadata、GameAssembly 与 runtime | [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) |
| 要理解 metadata 文件结构与绑定 | [D4：格式、加载与绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) |
| 启动崩溃，怀疑文件混用或符号不匹配 | [产物一致性排查]({{< relref "engine-toolchain/il2cpp-global-metadata-deep-dive.md" >}}) |
| 泛型方法不能执行 | [D5：泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) → [HybridCLR AOT 泛型专题]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) |
| 反射目标被裁剪 | [D8：裁剪与保留规则]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) |

## 延伸阅读

理解 IL2CPP 的限制后，可以接着看 HybridCLR 怎么在 IL2CPP 之上补回解释执行能力：

- [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})：准备版本、调试工程和第一条调用链。
- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：从基础机制进入案例与工程防线。

## 接下来补强什么

现有内容覆盖核心机制；贯穿三个专栏的泛型调用实验、固定构建样本与性能测量仍待补充。源码阅读起步已提供操作路线，其 Player 断点结果仍需单独验证。
