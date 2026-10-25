---
title: "HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用"
date: "2026-03-31"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
tags:
  - "IL2CPP"
  - "AOT"
  - "DisStripCode"
  - "link.xml"
  - "LoadMetadataForAOTAssembly"
  - "MissingMethodException"
weight: 48
lastmod: "2026-10-25"
---

同一条 `MissingMethodException`，可能指向定义被裁剪，也可能指向一个 AOT 泛型方法没有可用实现。修法必须跟着缺失的东西走，不能只按异常名字选择 `link.xml` 或补充元数据。

本文接着 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) 做修法决策；具体共享规则和保留代码写法留给后两篇。历史 async 案例尚未完成根因闭环，不把它作为某种修法必然有效的依据。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

## 先沿构建时序定位缺口

主包构建先对托管程序集做裁剪，再由 IL2CPP 转换保留下来的 IL，生成、编译并链接 native 代码。运行时则解析类型与方法身份，选择已有 native 实现，或在满足条件时解释相应方法体。

这个时序很关键：“托管裁剪删除了定义”和“该泛型实例没有生成 native 实现”是不同的问题。不能描述成“native 代码先生成，再被托管裁剪器删掉”。

| 核对对象 | 要拿到的证据 | 可能的修法 |
|---|---|---|
| AOT 类型、方法定义 | 最终裁剪 DLL 中是否存在目标定义和必要成员 | 调整静态引用、`link.xml` 或 `Preserve`，重建主包 |
| 目标泛型方法的 native 实现 | 本包生成代码、注册信息与实际调用路径 | 在 AOT 构建输入中显式引用所需实例，或使用可用的共享实例 |
| AOT 泛型方法的解释执行条件 | 对应 AOT assembly 的同源补充 DLL、加载返回值、目标方法体 | 补充匹配元数据，使缺少 native 实现的方法可以解释执行 |
| 名字、token、签名或程序集身份 | 热更 DLL、主包和补充 DLL 是否来自兼容输入 | 修正发布组合；保留代码不能修复版本身份错误 |

这里列的是检查方向。单条异常没有提供上表全部证据，必须先保留完整签名和原始堆栈。

## link.xml：保留定义和成员

当代码只通过反射、序列化或热更路径引用 AOT 成员时，主包裁剪分析可能看不到它。`link.xml`、`Preserve` 或可达的静态引用用于告诉裁剪器保留相应定义。

但“保留了泛型定义”不等于“为所有未来类型参数生成了 native 实例”。同样，显式 AOT 调用也会参与裁剪可达性分析，所以不能把保留代码与 `link.xml` 的作用说成完全互斥。它们在保留定义这一层可能重叠，在表达具体泛型调用这一层则不同。

如果已发布主包根本不包含所需 AOT 定义，不能假定补充 metadata 会把整套已裁掉的 AOT API 自动恢复。应核对支持范围，并在需要时发布新主包。新增热更程序集里的类型属于另一条装载路径。

## 显式 AOT 引用：让构建看见具体方法

项目常把这类代码命名为 `DisStripCode`，但这不是特殊 runtime API。有效性来自保留下来的 IL 中有目标调用，例如某个闭包上的 `Dictionary<TKey,TValue>.set_Item`，而不是文件名叫得正确。

只声明变量、写 `typeof`，或构造一个 builder 默认值，均不足以证明所需方法被生成。应先按 [共享规则]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 确定候选实例，再按 [可验证模板]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) 核对裁剪后 IL、生成物和 Player。

适合优先考虑显式实例化的场景，是调用频繁、参数集合稳定，并且性能测量支持把目标路径保留为 native。新增 AOT 引用要重新构建主包，无法仅靠下发一个文本文件改变已发布 native 代码。

## 补充元数据：正常的功能方案

`LoadMetadataForAOTAssembly` 补回对应 AOT assembly 的方法体信息，使本来缺少 native 实现的泛型方法有机会走解释执行。这是社区版支持 AOT 泛型的正常方案，不应一概称作“临时止血”。

对 `List<HotStruct>.Add`，应检查 `List<T>.Add` 所属 AOT 程序集的补充数据，而不是把热更参数类型所在的 DLL 当成补充对象。可用 native 共享实现已经存在时，runtime 会优先使用它；补 metadata 不意味着整套 AOT 程序集都改成解释执行。

补充 DLL 必须匹配已发布主包。`Consistent` 要求对应裁剪产物；`SuperSet` 接受符合要求的同源超集，并不允许随便拿另一版 framework DLL 代替。元数据应在首次需要它的方法执行前加载成功，工程上常放在热更入口启动前。

## 用一个失败路径做最小决策

假设报错目标是 `Dictionary<ValueTuple<int,int>,HotClass>.set_Item`，`HotClass` 是热更引用类型：

1. 核对这次 Player 的版本、完整签名和输入 DLL，先排除“读的是另一包日志”。
2. 检查主包的泛型定义是否存在，再查该方法是否有可用共享 native 实现。
3. 若没有 native 实现，检查定义所在程序集的补充元数据是否匹配并已加载。它可能直接提供正确的解释路径。
4. 若需要 native 性能，再评估共享形态的显式调用，并重新构建验证；不是看到报错就自动要求重发主包。

[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 已用固定环境对照缺失、补充 metadata 与显式 AOT 实例。这个证据支持上述分层模型，不能代替对所有容器、平台和性能的验收。

## 修复后的交付标准

修复记录至少包含：缺的是哪一层、改了哪个构建或加载输入、对应主包身份、失败路径如何重新执行，以及最终走 native 还是解释路径。功能通过后再决定性能优化，避免把“能执行”和“必须 native”混成一个标准。

下一篇进入 [共享类型判断]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})；完整发布协作放在 [AOT 泛型工作流]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})。
