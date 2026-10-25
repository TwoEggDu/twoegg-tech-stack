---
slug: "hybridclr-案例续篇async-崩溃的真正根因与两种修法"
title: "HybridCLR 案例续篇｜async 崩溃的证据边界与两条验证路线"
date: "2026-03-26"
series: "HybridCLR"
hybridclr_version: "历史案例版本未完整固定；当前机制另按 v9 配套源码核对"
tags:
  - "async"
  - "UniTask"
  - "IL2CPP"
  - "crash"
  - "AOT"
weight: 46
lastmod: "2026-10-25"
---

历史 async 崩溃记录出现了重复的 builder 与解释器栈。旧文先把空 RefMethods 当根因，后来又把 Development 不一致写成必然导致 metadata 失效的完整链。两种说法都超过了现存证据。

> **证据状态**：这是系列第 17 篇的历史案例复核。原 Android Release 记录没有完整固定源码、补充 DLL hash/返回码、状态机对照和修复后回归材料。本篇保留观察、撤回确定根因，并给出下一轮可执行的验证设计；没有新增 async Player 实测。

## 目前能确认与不能确认的事

| 原记录 | 支持的判断 | 尚不能推出 |
|---|---|---|
| 符号化后出现 Start、AwaitUnsafeOnCompleted 与 Execute | 调用经过 async 与解释器相关路径 | 泛型缺失是唯一根因 |
| 若干 native 帧重复 | 需要检查递归、重入及展开可靠性 | 必然是同一状态机无出口循环 |
| RefMethods 只有注释 | 生成文件没有手写调用 | 补充 metadata 路径无效 |
| 怀疑生成与最终 Build 的 Development 不同 | 需要检查输入一致性 | 一定失配、一定 SIGSEGV |

旧的“确定性因果链”图已移除，避免读者把待验证假设继续当作事实。

## 为什么空 RefMethods 不是失败条件

自动生成的 AOTGenericReferences 主要是一份需求清单。正确补充方法定义所在 AOT 程序集的 IL 后，相应泛型可以走解释实现，不要求把清单填满。

即使要保留 native 实例，声明 `AsyncUniTaskMethodBuilder<int>` 也没有说明 `TStateMachine` 和 `TAwaiter` 是什么。为一个 dummy struct 生成 `Start<DummyStateMachine>`，不能作为覆盖其他热更 struct 状态机的保证。

[UniTask 2.5.10 的公开 builder 实现](https://github.com/Cysharp/UniTask/blob/2.5.10/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/AsyncUniTaskMethodBuilder.cs)显示，Start 调用 MoveNext，AwaitUnsafeOnCompleted 设置相应状态机并注册 continuation。这一正常机制不包含“每次 await 必然重新 Start 同一状态机”的结论。历史项目仍需补录当时实际 UniTask 版本，不能用该参考版本替代现场身份。

## 路线一：验证输入与补充方法体

固定 Player、package/runtime、UniTask 版本及一个能正常完成的最小 async 方法。先建立一组匹配输入的正向基线，记录：

- 热更、定义 builder 的 AOT DLL、补充 DLL 的 hash 与模式。
- LoadMetadataForAOTAssembly 返回结果和首次使用时机。
- 实际生成的状态机是 class 还是 struct。
- 目标 Start / Await 方法的泛型参数、方法体来源及执行路径。
- 状态推进、完成结果与进程退出状态。

再一次只改变一个变量，例如 development、补充文件或加载时机；每组使用新进程，避免已登记 metadata/缓存遮住差异。不要同时换主包和热更 DLL 后，把恢复运行归因于其中一个 flag。

## 路线二：验证递归或重入

若仍出现重复栈，记录每次进入的具体 MethodInfo、状态机身份和状态值，区分同一状态机重复进入、不同 async 方法相互调用、同步 continuation 重入以及 unwind 失真。

同时保存 fault address、寄存器、栈界和完整线程信息，判断是否真正耗尽原生栈。`Il2CppFullySharedGenericAny` 只是一条调用形态线索，不证明缺方法体，也不证明已启用商业 FGS。

## 修法怎样跟证据对应

若已证实补充 DLL 选错，修复其主包绑定、模式与加载门槛，并复跑同样样本。若已证实 bridge/调用布局问题，核对匹配的运行时与生成物；若是业务递归或 continuation 重入，修业务路径。

只有在性能需求明确、共享条件成立时，才添加具体 native seeds。它可以优化目标调用，不应被当作绕开未知 runtime 故障的通用补丁。

构建参数统一有长期价值，见 [打包工程化]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})，但流程变整齐本身不是历史根因已确认的证据。

<!-- DATA-TODO: 补充最小 UniTask/async Player 实验，固定库版本、class/struct 状态机、metadata加载结果及continuation状态。保留单变量失败与恢复记录后再定因。 -->

## 系列位置

原始观察见 [历史案例]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}})，已有普通泛型对照见 [综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})，容器泛型案例继续 [Dictionary 与热更类型]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}})。

**修法要对应已证实的失败条件，未知根因应继续保持未知。**
