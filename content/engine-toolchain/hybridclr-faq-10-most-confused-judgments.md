---
date: "2026-03-23"
title: "HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断"
description: "不再补新原理，而是把 HybridCLR 系列里最容易混掉的 10 个判断重新拉直：哪些是装载问题，哪些是 metadata 问题，哪些是 AOT 泛型、MethodBridge、MonoBehaviour、PreJit、Full Generic Sharing、DHE 的边界问题。"
weight: 42
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "FAQ"
  - "Architecture"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

这个 FAQ 用来快速找回机制边界，不再重复主线的整段论证。看到一句过于确定的结论时，先找它缺少了哪一项前提。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 13 篇，按问题跳转即可。

## 十个判断与对应入口

| 常见判断 | 更准确的边界 | 继续读 |
|---|---|---|
| HybridCLR 就是一个解释器 | 还包含装载、metadata、工具链和跨边界设施 | [总论]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}}) |
| 补 metadata 就是在加载热更 DLL | 两种入口不同；补充针对已有 AOT 程序集 | [泛型篇]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) |
| 没有 native 泛型实例就只能重发主包 | 补充原始方法体可提供解释实现；需要 native 性能时再保实例 | [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) |
| AOTGenericReferences 生成完就自动安全 | 它是需求线索；还要核对定义、实现和 Player 路径 | [工作流衔接]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) |
| MethodBridge 只是性能优化 | 它首先承担跨调用布局的正确性 | [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) |
| AddComponent 成功就证明资源挂载成功 | 资源反序列化还有身份登记与版本条件 | [资源篇]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}) |
| Assembly.Load 之后方法都已准备好 | 装载与转换分开；模块初始化也可能提前触发部分方法 | [调用链]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}}) |
| PreJit 把热更方法编成 native | 它准备符合条件的方法的解释表示 | [性能篇]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}}) |
| 出现 FullySharedGenericAny 就代表商业 FGS 或泛型故障 | 这是生成调用形态的线索，不能证明授权能力、兼容范围或根因 | [FGS 边界]({{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}}) |
| DHE 只是解释器更快 | 它讨论已修改模块的函数执行分流，需满足自身版本与支持条件 | [DHE]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}}) |

## 两个容易被旧结论带偏的现场

第一，`AOT generic method not instantiated` 不表示 metadata 一定无关。先查定义所在程序集的补充情况和方法体，再区分 native 与解释路径；[综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})提供了同一热更 DLL 的对照。

第二，`SIGSEGV`、重复 native 帧和 `Il2CppFullySharedGenericAny` 的组合仍然不是唯一根因。历史 async 案例只保留当时能支持的观察，未知环节见 [案例续篇]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})。

## 系列位置

回到 [系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})选路线；需要看历史定位过程时继续 [案例诊断]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}})。

**先辨认问题与证据属于哪一层，再决定下一步检查什么。**
