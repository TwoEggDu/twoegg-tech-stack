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

本系列按“产物地图 → 八篇核心机制 → 产物排查”组织，共 10 篇正文，另与 HybridCLR 共用一篇线程接入专题。先建立 metadata、native code 与 runtime 的分工，再进入 IL2CPP 的构建与执行链路，最后衔接 HybridCLR。

## 前置知识

先按 [C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}}) 补值与借用、对象/所有权、函数指针、布局/存储与编译链接。公共目录中尚未成稿的单元明确列为待补，不能把几个语法示例当成完整基础。

建议先完成 [ECMA-335 前置主线]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。读 D3 前区分 [类型信息准备与静态初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})，读 D5 前确认 [签名与泛型上下文]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

## 公共基础之后，补 IL2CPP 的约定

已有运行时地图与 D2～D6 可作为专项材料。接下来按以下顺序补跟读，均不计入当前 10 篇正文：

1. **C#、IL 与生成代码对应阅读（待整理）**：固定一个构建样本，逐句解释参数、调用入口与返回值；接公共函数指针和构建课。
2. **metadata、对象与存储边界（待补）**：接公共布局/对象寿命课，分清描述、运行时对象、代码入口与 GC 协作，避免把 C++ 的析构规则直接套到托管对象。
3. **方法分派与泛型接线（待补）**：接公共模板课和 CLI 调用/泛型知识，对照生成调用与 runtime 表；C++ 模板实例化不能替代托管泛型语义。

先建立产物地图，再在读到对应 D 篇前补专项；不用等待所有高级内容完成才开始第一轮阅读。具体生成代码和布局随版本与平台核对，公开正文遵守源码许可边界。

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
| D6 | [GC 集成：BoehmGC 的接入层、write barrier 与 finalization]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 根、写入协议、终结与托管／native 内存增长的区分 |
| D7 | [ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}}) | Reflection.Emit、动态程序集等缺失特性的技术原因 |
| D8 | [Managed Code Stripping：裁剪策略与 link.xml]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) | UnityLinker 的裁剪级别和保留规则 |

---

## 共享运行时专题

D3 与 D6 之后可读 [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：串起托管线程、原生回调 wrapper、解释器 TLS、GC 根与退出清理。正文归 HybridCLR，只维护一份；其中 Player 与原生插件实验仍待实测。

## 按问题查阅

| 问题 | 阅读入口 |
|---|---|
| 分不清 metadata、GameAssembly 与 runtime | [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) |
| 要理解 metadata 文件结构与绑定 | [D4：格式、加载与绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) |
| 启动崩溃，怀疑文件混用或符号不匹配 | [产物一致性排查]({{< relref "engine-toolchain/il2cpp-global-metadata-deep-dive.md" >}}) |
| GC 后内存不下降，或退出后对象仍存活 | [D6：内存分类与生命周期对照]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) → [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}}) |
| SDK 线程回调热更方法、线程退出后状态未释放 | [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) |
| 泛型方法不能执行 | [D5：泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) → [HybridCLR AOT 泛型专题]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) |
| 反射目标被裁剪 | [D8：裁剪与保留规则]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) |

## 延伸阅读

理解 IL2CPP 的限制后，可以接着看 HybridCLR 怎么在 IL2CPP 之上补回解释执行能力：

- [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})：准备版本、调试工程和第一条调用链。
- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：从基础机制进入案例与工程防线。

## 接下来补强什么

现有内容覆盖核心机制；贯穿三个专栏的泛型调用实验、固定构建样本与性能测量仍待补充。源码阅读起步已提供操作路线，其 Player 断点结果仍需单独验证。
