+++
title = "HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断"
description = "不再补新原理，而是把 HybridCLR 系列里最容易混掉的 10 个判断重新拉直：哪些是装载问题，哪些是 metadata 问题，哪些是 AOT 泛型、MethodBridge、MonoBehaviour、PreJit、Full Generic Sharing、DHE 的边界问题。"
weight = 42
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "FAQ", "Architecture"]
series = "HybridCLR"
+++

> 这篇不再讲新的源码细节。它只做一件事：把前面几篇里最容易混掉、但又最影响项目判断的 10 句话重新拉直。因为很多时候，团队不是“不会用 HybridCLR”，而是脑子里默认站着几句错判断。

这是 HybridCLR 系列第 13 篇。  
如果说前面的主链、AOT 泛型、工具链、资源挂载、`Full Generic Sharing`、`DHE` 和选型篇都在补地图，那这一篇更像是把地图边上最容易误导人的几句路标重新写对。

## 这篇要回答什么

这篇主要回答 3 个问题：

1. HybridCLR 系列里最容易混掉的 10 个判断到底是什么。
2. 每个判断为什么不对，它真正混掉的是哪两层。
3. 如果项目现场又出现类似说法，第一反应应该改成什么。

## 先给一句总判断

如果先把这篇压成一句话，我的判断是：

`HybridCLR 最常见的误解，不是某个 API 名字记错了，而是把“装载、metadata、AOT 泛型实例、MethodBridge、资源身份链、generic 共享、函数级分流”这些本来就不在一层的东西，误当成同一种能力。`

只要先把层分对，很多误解其实会自动消失。

## FAQ 1：HybridCLR 本质上只是一个解释器

不对。

解释器当然是 HybridCLR 的核心组成之一，但把它压成“只是一个解释器”，会直接抹掉前后两大块真正决定项目行为的东西：

- 前面：动态程序集装载、AOT metadata 补充、MethodBridge、资源挂载身份链
- 后面：`PreJit`、`Full Generic Sharing`、`DHE` 这些继续往外扩的能力

更准确的说法应该是：

`HybridCLR` 是一套把 `IL2CPP` 热更补成可工程化方案的 runtime 体系，解释器只是其中负责“热更方法怎么执行”的那一层。

如果项目里有人说“它不就是个解释器”，你最好立刻追问：

`你现在说的是方法执行层，还是把整条装载到执行的链都压扁了？`

## FAQ 2：`LoadMetadataForAOTAssembly` 就是在加载热更 DLL

不对。

这两件事在运行时入口上就不是同一条链：

- `Assembly.Load(byte[])` 才是在把热更程序集真正接进 runtime
- `LoadMetadataForAOTAssembly` 是给已有 AOT assembly 补一份同源 metadata 视图

它解决的是：

`runtime 看不看得见那份 AOT metadata`

它不在做的是：

`把一个新的热更程序集装进来`

如果把这两个动作混成一件事，你后面就会同时看错两类问题：

- 热更 DLL 根本没装进来
- AOT metadata 补错了 DLL 或顺序不对

更准确的说法应该是：

`一个负责把新程序集接进来，一个负责让 AOT 世界在运行时重新变得可查询。`

## FAQ 3：补充 metadata 以后，泛型问题就都解决了

不对。

补充 metadata 解决的是：

`AOT metadata 在运行时能不能被看见、被解释器继续消费`

它不等于：

`所有原本没有被 AOT 出来的泛型 native 实现，都会自动出现`

这就是为什么现场会同时出现两类完全不同的问题：

- 一类是 metadata 不可见，解释器拿不到 method body、泛型签名或上下文
- 另一类是 metadata 看懂了，但真正要调的那个具体泛型实例根本没有 native implementation

最典型的边界提醒，就是：

`AOT generic method not instantiated in aot`

看到这句时，第一反应就不该再问“metadata 补了没”，而该问：

`这个具体实例到底有没有被 AOT 出来。`

更准确的说法应该是：

`补 metadata 能让更多 AOT 泛型场景退回 interpreter 兜底，但它不是 generic native instantiation 的总开关。`

## FAQ 4：`AOTGenericReference` 一生成，运行时就自动安全了

也不对。

`AOTGenericReference` 的价值很大，但它不是自动修复器。  
它更像是：

`帮你把一部分需要显式暴露的 AOT 泛型实例，提前放到构建视野里。`

它解决不了三类事情：

- 你没覆盖到的实例
- 真正发生在业务动态组合里的新实例
- 和 `MethodBridge`、metadata 顺序、资源身份链有关的其他问题

所以它最多只能说明：

`你显式声明过一部分风险`

不能说明：

`运行时已经天然安全`

更准确的说法应该是：

`AOTGenericReference` 是显式暴露和前移泛型风险的工具，不是对泛型问题的运行时担保。`

## FAQ 5：MethodBridge 只是性能优化

不对。

这是系列里最容易把人带偏的一句。

如果某个调用场景真的缺桥接，现场通常不是“慢一点”，而是直接报错：

- `GetReversePInvokeWrapper fail...`
- `NotSupportNative2Managed`
- `NotSupportAdjustorThunk`

这说明 `MethodBridge` 首先解决的是：

`interpreter / AOT / native 之间的 ABI 正确性`

性能只是在“桥已经正确存在”的前提下，才有资格继续讨论。

更准确的说法应该是：

`MethodBridge` 是跨边界调用契约的一部分，先是正确性能力，其次才是性能问题。`

## FAQ 6：支持 MonoBehaviour，就等于支持 `AddComponent`

不对。

代码里 `AddComponent(type)` 能成立，只说明：

`这个类型对象在运行时能被拿到，并沿普通代码路径实例化`

而资源上的热更 `MonoBehaviour` 能成立，真正难的是另一条链：

`Prefab / Scene / AssetBundle 反序列化出来的脚本引用，能不能沿程序集身份链重新接回真实热更程序集`

所以这两件事不是一回事：

- 一个偏代码实例化路径
- 一个偏资源反序列化和程序集身份链

更准确的说法应该是：

`HybridCLR` 对资源挂载 `MonoBehaviour` 的支持，本质上是脚本身份链问题，不是“这个类能不能继承 MonoBehaviour”这么简单。`

## FAQ 7：`Assembly.Load(byte[])` 之后，方法就已经“编译好了”

不对。

`Assembly.Load(byte[])` 之后，你最多只能说：

`程序集对象已经进了 runtime`

但这离“方法已经准备好执行”还差了好几步：

- 元数据结构要先接好
- 某些方法第一次执行时还会现场走 transform
- 真正调用时还要看 `MethodInfo`、`invoker_method`、桥接路径和 interpreter 主链

所以如果有人把 `Assembly.Load` 理解成“相当于已经编译完成”，那通常会立刻看错首调尖峰、transform 成本和实际调用路径。

更准确的说法应该是：

`Assembly.Load` 是装载入口，不是“方法已经 ready”的终点。`

## FAQ 8：`PreJitMethod` 真的是“提前 JIT 成 native 代码”

不对。

这句误解通常来自名字，而不是行为。

`PreJitMethod / PreJitClass` 真正预热的，不是新的 native 机器码，而是：

- 解释器方法信息
- 首次 transform 成本
- 一部分首次调用前置准备

它当然能减少首调尖峰，但它没有把解释器方法“变成 JIT native code”。

所以如果把 `PreJit` 理解成：

`把热更代码提前编成原生代码`

那后面你一定会高估它的能力边界。

更准确的说法应该是：

`PreJit` 是把解释器首调成本前移，不是给 IL2CPP 补一个真正的 runtime native JIT。`

## FAQ 9：`Full Generic Sharing` 是补充 metadata 的升级版

不对。

这两条路线都和 AOT 泛型有关，但层级完全不同：

- 补充 metadata 解决的是 metadata 可见性和 interpreter 兜底
- `Full Generic Sharing` 解决的是 generic runtime 共享上限

它们最大的区别不是“谁更高级”，而是：

- 一个在回答“runtime 能不能继续看懂并解释”
- 一个在回答“更多 generic 调用能不能不再退回补 metadata + interpreter”

这也是为什么 `Full Generic Sharing` 会直接触到：

- generic method 签名形态
- `MethodInfo` 相关字段
- `methodPointer / invoker_method / virtualMethodPointer`

这些都已经不是 metadata 文件层能单独解释的事了。

更准确的说法应该是：

`Full Generic Sharing` 改的是 generic 调用模型，不是“metadata 更多一点”的升级版。`

## FAQ 10：`DHE` 就是普通解释执行更快一点

还是不对。

普通热更主线更像是：

`新热更程序集进来后，里面的方法进入解释执行主链`

而 `DHE` 的核心语义是另一件事：

`对已经打进包体的 AOT 程序集做任意增删改后，运行时按函数是否变化，分别走原 AOT 路径或最新解释执行路径`

这就是为什么 `DHE` 必须引入：

- 离线差分得到的 `dhao`
- 更严格的加载顺序
- 对资源挂载和 `extern` 的更保守边界

所以它真正改的不是“解释器速度曲线”，而是：

`AOT 与 interpreter 在同一程序集里的函数级分流策略`

更准确的说法应该是：

`DHE` 不是普通解释器的加速补丁，而是“修改现有 AOT 模块”这件事的函数级混合执行方案。`

## 最后压一句话

如果只允许我用一句话收这篇 FAQ，我会写成：

`HybridCLR` 最容易混掉的 10 个判断，几乎都指向同一个根因：把不在一层的能力误当成同一种东西；真正稳的做法，不是多记几个结论，而是先问自己现在讨论的到底是装载、metadata、AOT 泛型实例、MethodBridge、资源身份链、generic 共享，还是函数级分流。`

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}}">HybridCLR 高级能力选型｜社区版主线、补 metadata、Full Generic Sharing、DHE 分别该在什么时候上</a>
- 下一篇：无。到这里，这个系列已经同时有主链、边界、高级能力和 FAQ 入口了。
