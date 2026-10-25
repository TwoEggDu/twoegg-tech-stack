---
date: "2026-03-23"
title: "HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事"
description: "把 HybridCLR 系列里的几条主线重新收成一张边界图：动态装载、补充 metadata、AOT 泛型、MethodBridge、资源挂载、Full Generic Sharing 与 DHE 分别在解决什么，又分别带来什么代价。"
weight: 35
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Architecture"
  - "Tradeoff"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

热更新成功运行一段业务代码之后，团队仍要回答：新程序集怎么进入 runtime，泛型调用从哪里获得实现，资源脚本能否恢复，以及这些能力带来多少构建和运行成本。把这些问题都叫作“支持热更”，会掩盖真正的工程边界。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 6 篇，负责把主线收成选型地图；具体失败处理见 [诊断篇]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}})。

## 按问题拆能力

| 问题 | 相关机制 | 不应混成的另一件事 |
|---|---|---|
| 新 DLL 如何登记到 runtime | Assembly.Load、InterpreterImage | 所有方法已经转换 |
| AOT 泛型缺少可调用实现怎么办 | native 共享或补充 IL 后解释 | 必须生成新的 native 代码才能运行 |
| 方法如何执行 | Transform、InterpMethodInfo、Execute | JIT 成 native |
| 解释器与 native 如何传参 | bridge、invoker、wrapper | 单纯性能优化 |
| 资源脚本如何恢复 | 构建登记、placeholder、真实 DLL | 仅 AddComponent 成功 |
| 如何减少特定执行成本 | 测量、预热、边界调整及适用的高级能力 | 所有路径都能统一提速 |

这些能力互相依赖，但各有检查入口。源码定位统一使用 [总论]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})、[泛型篇]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})与 [资源篇]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}})的固定版本路径，避免另维护一份容易失效的函数名清单。

## 功能可用与 native 性能要分别判断

补充 metadata 可以给缺少合适 native 实现的 AOT 泛型提供解释执行所需的 IL。它可以是正式方案，不必等线上报错才使用。若性能目标已满足，不能仅因执行路径是解释器，就把功能标为未修好。

显式 AOT seeds 的意义是让适合提前确定的共享实例进入 native 产物。它有主包发布和维护成本，也不能用任意 dummy struct 覆盖热更 struct。是否添加，应该由具体路径与测量决定。

普通泛型共享、Unity 生成的完全共享形态、HybridCLR 商业 FGS 是不同层次。高级能力的适用条件见 [能力选型]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}})，不能从一个 `Il2CppFullySharedGenericAny` 符号推定授权能力或支持范围。

## 构建链的代价是配套关系

Editor、package、runtime、il2cpp_plus、平台、宏、development、裁剪与场景，都可能影响产物。生成操作成功后，还需要证明本次 Player 消费的是这批配套输入。

主包上线后，其 native 产物和 AOT 定义形成一条版本边界。下发热更 DLL 不会同时替换桥接 C++、新增主包定义或改掉旧包布局；补充 metadata 也不能绕过这条边界。需要变更这些能力时，应评估发主包或适用的其他机制。

## 内存与首调成本来自不同对象

运行时额外成本包括 DLL 解析数据、同源 image、解释指令、线程解释栈、桥接表等。`MethodBodyCache` 的原始方法体缓存与 `interpData` 的转换结果具有不同生命周期，不能用一个缓存参数代表全部开销。

社区版普通装载不能假设有通用程序集卸载。托管对象被 GC 回收，也不等于 native metadata 被释放。具体生命周期见 [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})。

首次加载、类型初始化、首次 Transform、资源反序列化，可能在启动期叠加。预热可以迁移其中一部分工作发生的时机，但增加了提前执行的工作与内存占用。

## 性能判断需要实际路径

不保留没有设备、构建条件和 workload 的通用倍数。把性能拆成首调、稳态执行、跨边界与启动期集中工作，再测量各自的成本。

适合留在热更层的通常是变化频繁、频率可控的业务组织；稳定且已测得昂贵的核心循环，可以评估留在 AOT、批处理接口或减少跨边界次数。是否划算仍以本项目数据为准，见 [性能篇]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}})。

## 当前证据到哪里

固定 v9 的公开实验已覆盖简单调用、泛型对照及若干 GC/线程路径；各篇明确限定样本和环境。这些结果没有自动覆盖资源身份链、移动端 ABI、全部 async 行为或商业能力。

因此当前工作的重点是补齐关键场景验证和维护既有结论，不是用更多概念标题替代实验。

## 系列位置

执行细节见 [调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})，工程组织继续 [最佳实践]({{< relref "engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md" >}})。

**能力、成本与验证范围分别列清楚，才有依据决定边界放在哪里。**
