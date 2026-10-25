---
date: "2026-03-23"
title: "HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT"
description: "不做空泛 benchmark，而是把 HybridCLR 的成本拆成几层：首调 transform、长期解释执行、跨 ABI 调用，以及 PreJit 和运行时选项到底该怎么用。"
weight: 38
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Performance"
  - "Runtime"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

首开界面卡顿与每帧执行缓慢，可能来自完全不同的成本。前者可能集中在装载、类型初始化与首次转换，后者可能来自解释执行、边界调用或业务算法。先拆成本，才能判断预热是否有用。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 9 篇，提供测量方法与取舍。现有调用链实验验证了转换结果复用，**尚未提供这套环境的性能基准**；本文不保留没有来源的通用慢速倍数。

## 分开四类成本

| 成本 | 观察方式 | 优化方向 |
|---|---|---|
| 首次方法准备 | 新进程中区分查找、类型初始化、Transform 与执行 | 有选择地预热 |
| 稳态方法执行 | 同一输入与调用次数下重复采样 | 算法、数据组织、执行位置 |
| 跨边界 | 固定工作量比较集中调用与频繁往返 | 批处理、减少调用次数 |
| 启动期叠加 | 分段记录 DLL、metadata、资源及业务启动 | 调整时机和并发度 |

预热只能前移部分准备工作，不能消除后续解释执行，也不能代替缺失 metadata 或不匹配桥接的修复。

## PreJit 真正准备什么

固定 [RuntimeApi.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/RuntimeApi.cpp) 的 PreJit 路径会检查方法形态，再通过解释器方法准备链获取内部表示。它不生成 native 机器码。

[InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp) 中的 `GetInterpMethodInfo` 与 `interpData` 是关键观察点。有缓存不等于方法已完成业务调用；同一方法再次执行仍要处理参数、建立现场和返回结果。

不能给任意类型或开放泛型直接套用预热。按目标方法实际形态检查返回结果、异常及泛型上下文。类型初始化可能引入额外工作，也应与 Transform 分开记录。

## 哪些路径值得预热

优先考虑即将出现、范围有限、首调尖峰已经影响体验的入口。对整个程序集无差别预热，可能把大量不会使用的方法转换到启动期，同时增加内存。

若使用工作线程预热，还需核对该入口允许的线程条件、初始化副作用与 metadata 锁竞争。不要仅因 API 看似纯计算就假定可以任意并发；默认先用可控制的顺序预热建立基线。

## 缓存与运行时选项分别管什么

package 的 [RuntimeOptionId.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Runtime/RuntimeOptionId.cs) 与 runtime 的 [RuntimeConfig.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/RuntimeConfig.cpp) 可用于核对当前版本选项。

- 对象槽与帧栈大小影响解释线程的容量，不等于托管 GC 堆大小。
- `MaxMethodBodyCacheSize` 控制的是**原始 IL MethodBody 缓存**的相应规模阈值，具体淘汰见 [MethodBodyCache.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/MethodBodyCache.cpp)；它不是全部 `InterpMethodInfo` 转换结果的回收开关。
- 内联深度与可内联方法体限制影响转换策略，不能仅靠增加数值推定更快。

修改选项后要在新进程和相同输入下比较，并记录内存和延迟；不要把默认值或单个版本的含义推广到全部版本。

## 怎样设计一组可解释的测量

固定 Editor、源码提交、平台、架构、Development/Release、优化等级、Profiler设置、GC模式、输入规模和调用次数。

1. **冷路径**：新进程分别记录装载、首次查找、预热与首次业务调用。
2. **稳态**：先确认目标表示已经准备好，再采样固定工作量。
3. **边界**：同样数据分别由一次批量调用和多次小调用完成，避免同时改变算法。
4. **内存**：比较预热前后保留量，并区分托管对象与 native metadata/指令数据。

记录分布和异常样本，而不是只挑一次最快值。原生断点验证路径会扰动时间，因此断点运行与性能采样应分开。

## profiler 中看见 Execute 能说明什么

原生栈中出现 `Interpreter::Execute` 说明采样链经过解释器，但其上方仍可能正在执行 AOT callee。反过来，没看到该帧也可能来自采样遗漏、符号、内联或展开限制，不能据此断言目标方法一定是 AOT。

把目标方法身份、已知调用链、生成物和采样位置结合起来。需要确定泛型是否走 native 时，可参考 [泛型实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})的断点归因，再单独做无调试器测量。

## 什么时候改变执行边界

若已测到稳定高频循环或大量跨边界开销，考虑把稳定核心放到 AOT、扩大批量接口、减少来回传递。变化频繁且性能满足预算的业务继续解释执行即可。

补充 metadata 是有效的常规方案；只有确有性能或内存需求，才进一步保留 native 实例或评估适用的其他能力。

<!-- DATA-TODO: 增加同环境的冷调用、预热后调用、稳态与跨边界批处理测量，记录构建条件、样本分布、内存与输入规模。 -->

## 系列位置

正确性排查先看 [诊断篇]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}})，高级方案边界见 [能力选型]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}})。

**预热解决发生时机，性能优化还需要实际工作量与执行路径的测量。**
