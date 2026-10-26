---
date: "2026-03-26"
title: "HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑"
description: "从 AOT 泛型缺口、补充 metadata、Consistent/SuperSet 映射，到 AOTGenericReference 和 MethodBridge，拆解 HybridCLR 在泛型问题上到底补了什么、没补什么。"
weight: 31
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Generics"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-26"
---

热更代码可以编译，运行时也能找到泛型类型，却在调用方法时报错。真正缺的可能是方法实现，也可能是解释该方法所需的 IL；只问“这个类型存在吗”还不够。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 2 篇。本文解释机制；选修法看 [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})，算共享形态看 [共享类型判断]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})。

## 类型存在与方法能执行是两个问题

泛型类型定义、封闭类型、泛型方法定义和封闭方法要分开。例如 `Foo<int>.Bar<string>` 同时带有类型参数与方法参数。metadata 描述这些身份，runtime 还要找到可调用实现。

IL2CPP 能根据现有定义构造很多封闭泛型的运行时信息，但没有原始 IL 时，不能仅靠另一份 native 实例还原任意泛型方法体。HybridCLR 补充 metadata 的关键，就是重新提供泛型函数的 IL，让解释器转换并执行。

| 检查项 | 缺失时的方向 |
|---|---|
| 类型或成员定义是否保留 | 裁剪、程序集版本与引用 |
| 是否有适用的 native 泛型实现 | 共享规则和实际 AOT 产物 |
| 是否有解释所需的泛型方法体 | 补充程序集、加载结果与模式 |

补充 metadata **不生成 native 代码，但能解决符合条件的 AOT 泛型执行缺口**。不能把“不是 native”写成“仍不能调用”。[官方 AOT 泛型说明](https://www.hybridclr.cn/docs/basic/aotgeneric)也将它作为常规方案。

## 原始方法体来自哪个程序集

`LoadMetadataForAOTAssembly` 给已有 AOT 程序集登记同源 image。以 `List<HotStruct>.Add` 为例，需要补的是定义 `List<T>.Add` 的程序集；不能因为实参来自热更 DLL，就把热更 DLL 当作 AOT 补充文件。

在 [Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/Assembly.cpp) 中，可沿创建同源 image、加载输入、匹配 AOT assembly、初始化与登记阅读。输入 DLL 来源、模式、返回结果都要记录；“调用过 API”不等于成功。

补充应在首次使用相关 AOT 泛型前完成，可以放在热更 DLL 装载之后、业务执行之前。若模块初始化已经触发该泛型，就要更早完成。“始终必须早于 Assembly.Load”不是通用机制要求。

## Consistent 与 SuperSet 的边界

| 模式 | 输入前提 | 不能据此推定 |
|---|---|---|
| Consistent | 与目标 Player 对应的裁剪后 DLL 精确一致 | 同名即可 |
| SuperSet | 输入是目标裁剪后 DLL 的适配超集，可使用同源原始 DLL | 任意旧版或新版兼容 |

[官方模式说明](https://www.hybridclr.cn/docs/basic/aotgeneric#元数据模式-homologousimagemode)给出输入要求。实现分别见 [ConsistentAOTHomologousImage.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/ConsistentAOTHomologousImage.cpp)、[SuperSetAOTHomologousImage.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/SuperSetAOTHomologousImage.cpp)。

SuperSet 会处理额外定义，不意味着能复活 Player 中已裁掉的所有类型、字段和非泛型方法。上线后应按主包版本保存和选择补充 DLL，不能每次热更都用新构建产物覆盖旧主包基线。

## native 与解释实现怎样分流

下面是检查模型，不是适用于所有入口的固定调用栈：

1. 确认类型和方法身份可解析。
2. 有适用 native 共享实现时，核对实际调用是否使用它。
3. 缺少合适 native 实现时，检查同源 image 是否提供解释所需的方法体。
4. 按调用方向核对 invoker、桥接与参数布局。
5. 两种实现都不可用，或边界条件不满足，再根据具体异常定位。

当前 v9.0.1 的 [Il2CppCompatibleDef.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/Il2CppCompatibleDef.cpp) 也提供直接证据：`InitAndGetInterpreterDirectlyCallMethodPointerSlow` 会根据 `MetadataModule::IsImplementedByInterpreter` 设置解释器指针；后者涉及同源 image 查找。**所以报 `AOT generic method not instantiated`，不能据此排除补充 metadata 的作用。**

记录报错函数、类型/方法参数与 DLL，不能只按异常类名定因。

## 清单与显式 AOT 保留分别做什么

`AOTGenericReferences.cs` 主要以注释列出泛型需求，也包含建议补充程序集的清单。它不证明 native 实例已生成，更不要求把每条注释都手工填成代码。

若测量确认某个可共享实例值得保留 native 实现，在 AOT 中写出真实调用并保护 helper，再检查生成物与 Player 路径。仅声明 `AsyncUniTaskMethodBuilder<int>` 不会自动保住任意 `Start<TStateMachine>` 或 `AwaitUnsafeOnCompleted<TAwaiter,TStateMachine>`。

热更 struct 不能直接在旧 AOT 源码中引用，也不能随意用 dummy struct 替代。无法提前覆盖时可以继续使用匹配的补充 metadata，不必把它归为临时止血。

## 普通共享与历史 async 崩溃

普通泛型共享、Unity 生成物中的完全共享调用形态、HybridCLR 商业版 FGS 支持，相关但不能互相替代。

`Il2CppFullySharedGenericAny` 符号只说明进入某种完全共享生成形态，不证明已启用商业 FGS、缺少 metadata，或必然无限递归。历史重复栈要结合方法身份、状态机状态和实际调用目标解释。旧文的确定因果已收紧，见 [async 案例续篇]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})。

## 用四组 Player 对照检查机制

[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 固定同一热更 DLL，改变显式 AOT seeds 与补充 metadata 两个变量，并用原生断点区分路径。

无 seeds、无补充 metadata 的 int/结构体样本出现方法缺失；补充后相应样本能执行。这证明没有对应 native 实例与无法运行并不等价。具体方法、配置与断点归因以实验正文为准，不扩展成所有泛型和平台已验证。

## 同一热更结构体：按值与 ref 的实现选择

[constrained.／装箱 Player 实测]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md#constrained-boxing-player" >}})补充了一组更具体的对照。同一 Player／hot DLL 中，GenericCalls.ByValue<HotCounter> 在 metadata off 时三次报告 MissingMethod；on 组补充与最终主包匹配的 Probe.AOT.dll 后，三次从其 IL 准备／转换并进入解释帧，返回 11、调用方原值仍 10。

同一组 ref T 调用的 on/off 则都命中已有 native ByRef 共享入口，随后进入 hot Counter.Increment，原值改为 11。AOT Cases.Run 没有提前调用这两个目标的具体种子；cross/box 对照本身不经过泛型 helper。

这是“身份、签名、可用实现与执行位置要一起检查”的固定样本，不是 ref T 普遍免补 metadata 的新规则。带 FullySharedGenericAny 名称的函数存在，也不证明当前实参／签名必然会选中它。on/ref 现场在准备阶段读取过 ByValue 的补充 body，实际执行却仍是 native ByRef；不能只凭 metadata 已加载或 body 被读取就宣布目标被解释执行。

本轮沿用自定义 Editor/v9/Windows x64，记录在实验仓库 evidence/2026-10-26-constrained-boxing；本轮固定提交为 [5638131](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/563813195813cc2d618bdf301e12ab1af0556d79)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/results.json)。

## 系列位置

下一步读 [工具链拆解]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})；带着报错时看 [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})。

**先确认方法能从哪条路径获得实现，再决定是否需要保留到 native。**
