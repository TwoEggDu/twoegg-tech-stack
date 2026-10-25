---
date: "2026-03-20"
title: "HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了"
description: "不再重讲主链，而是把 HybridCLR 常见报错和现场按层拆开：装载、补充 metadata、AOT 泛型实例、MethodBridge/ReversePInvoke、资源挂载身份链分别怎么判断。"
weight: 37
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Troubleshooting"
  - "Runtime"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

同一句 MissingMethodException，可能来自裁剪后的定义缺失，也可能来自泛型实现或解释方法体不可用。排障的第一步是固定失败位置和输入，再缩小根因范围。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 8 篇，提供定位入口。表里的信号只帮助选择第一轮检查，**不能仅靠异常名或 native 信号唯一归因**。

## 先留住现场

记录主包版本、target、进程、实际 native 模块身份、热更/补充 DLL hash、加载顺序与返回结果。原生崩溃需要匹配同一构建的符号；错误符号可能产生看似合理的栈，随后所有判断都会偏离。

先区分正常托管异常、进程退出与 native fault。缺失日志时保留原始 crash/tombstone，不把“没看到托管异常”当作证明异常没发生。

## 五层检查地图

| 首个已确认的失败点 | 先核对 | 进一步证据 |
|---|---|---|
| DLL 装载或补充 API 失败 | 文件身份、输入模式、依赖与返回码 | 实际装载入口和目标 image |
| 类型/成员解析失败 | 定义是否保留、程序集是否正确 | stripped DLL、类型签名、解析目标 |
| 泛型方法无法调用 | native 共享、补充 IL、调用方向 | 目标方法参数及实际方法体来源 |
| bridge / wrapper 失败 | 方向、签名、生成物与 wrapper 限额 | 对应查表或失败分支 |
| 资源脚本恢复失败 | 资源版本、程序集登记、装载时机 | 组件类型与生命周期实际调用 |

## 装载层：两种入口别混在一起

`Assembly.Load(byte[])` 装载热更程序集；`LoadMetadataForAOTAssembly` 给已有 AOT 程序集补同源 image。分别记录它们的调用结果。

补充模式决定输入要求：Consistent 使用目标构建精确对应的裁剪 DLL；SuperSet 允许适配的超集，不限于裁剪文件。不要把“使用了原始 DLL”直接判为错误，也不要只凭程序集同名判为匹配。

固定 runtime 的 [Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/Assembly.cpp) 是两个方向的重要入口。重复装载、错误 image、错误依赖，都应以具体分支为证据。

## 解析与泛型：先看定义，再看实现

出现 `TypeLoadException`、`MissingMethodException` 时，先确认目标定义是否存在于实际使用的程序集。若定义被主包裁剪，补充 metadata 不能作为恢复所有定义的通用办法。

定义存在而某个封闭泛型无法执行时，继续查：

1. 目标类型参数和方法参数各是什么。
2. 是否有适用 native 共享实现。
3. 是否已为**方法定义所在程序集**加载正确的补充 metadata。
4. 原始 IL 方法体能否取得、转换是否成功。
5. 这次调用方向是否还要求特定桥接。

`AOT generic method not instantiated` 不能排除补充 metadata 的作用。相关机制见 [泛型篇]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})，具体选择见 [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})。

v9 的方法体读取可从 [Transform.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/Transform.cpp)、[MethodBodyCache.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/MethodBodyCache.cpp) 追踪。v6.1.0 入口不同，应按 [调用链差异表]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})处理。

## bridge 层：按方向与实际失败分支排查

固定 [InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp) 中有 `GetManaged2NativeMethodPointer`、`GetMethodPointer`、`NotSupportNative2Managed` 等入口。先确定是解释器调 native、native 调解释器、委托调用还是 reverse P/Invoke。

`NotSupportNative2Managed` 等失败分支会抛出相应异常；未捕获异常、平台错误和其他损坏又可能影响最终进程表现。因此“缺桥 = SIGABRT/SIGILL”不是通用规律。

检查 Player 编译进去的桥接产物，不能只检查开发机最近生成的文件。原生回调还要核对 delegate 存活、线程接入及停机顺序，见 [线程接入专题]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

## 资源层：检查程序集身份是否闭合

AddComponent 成功只能证明那条代码路径；Prefab/AssetBundle 还依赖构建登记、资源身份和实际 DLL 装载时机。

从资源记录的类型出发，核对脚本程序集、generated manifest、placeholder 与真实 image。不要看到 Missing Script 就只重跑 metadata 补充，详见 [资源挂载篇]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}})。

## native crash 里哪些只是线索

`SIGSEGV`、重复栈帧与 `Il2CppFullySharedGenericAny` 都不足以唯一确定 AOT 泛型缺失。重复栈可能涉及业务递归、异常回调或展开失真，需要方法身份、状态变化和实际 fault 信息支持。

“解释器深层出错”也不是 HybridCLR bug 的证明。先缩小可复现样本，核对版本、调用参数、对象生命周期和线程条件，再比较修复版本或提交上游 issue。

工具步骤与历史证据边界见 [崩溃定位专题]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}})。

## 系列位置

工程预防见 [最佳实践]({{< relref "engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md" >}})，功能稳定后再进入 [性能与预热]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}})。

**异常给出调查入口；输入、实际分支和复现结果才支持根因。**
