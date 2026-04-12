---
date: "2026-03-23"
title: "HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版"
description: "把 Full Generic Sharing 放回正确位置：它解决的不是 metadata 可见性，而是 generic 代码共享与调用模型。解释为什么它能减少补充 metadata、缩小包体和内存，又为什么会引入泛型函数性能代价和 Unity 版本前提。"
weight: 39
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Generics"
  - "Runtime"
series: "HybridCLR"
---
> 这篇不写“商业版内幕揭秘”。能确定的部分，只来自公开文档、当前系列已经拆出来的 runtime 边界，以及 `libil2cpp` 公开字段名暴露出的方向；凡是涉及具体内部实现的地方，我都会明确按“高可信推断”来写，而不是把猜测写成事实。

这是 HybridCLR 系列第 10 篇，也是第一篇继续往高级能力边界下探的文章。  
前面那篇边界文已经把一句话先钉死了：

`Full Generic Sharing 不是补充 metadata 的升级版。`

这句话如果不单独展开，后面一遇到 AOT 泛型、补充 metadata、包体大小、泛型函数性能这些问题，读者还是会把它们重新混回一层。

所以这一篇只做一件事：

`把 Full Generic Sharing 放回 IL2CPP generic runtime 自己那一层。`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 为什么 `Full Generic Sharing` 不是“补 metadata 的更强版本”。
2. 旧的 generic sharing 为什么总会在一部分 AOT 泛型场景里露出上限。
3. 补充 metadata 能解决什么，为什么它又解决不了 `Full Generic Sharing` 想解决的那件事。
4. 如果只按公开资料和现有 runtime 线索推断，`Full Generic Sharing` 大概率改的是哪一层。
5. 它在项目里真正换来了什么，又额外付出了什么代价。

## 先给一句总判断

如果先把这篇压成一句话，我的判断是：

`Full Generic Sharing` 想解决的，不是“让 runtime 看见更多 metadata”，而是“让更多原本需要单独 AOT 实例化的 generic 调用，收敛到可共享的 native generic 执行路径上”。

这两个问题看起来都和 “AOT 泛型跑不起来” 有关，但层级完全不同：

- 补充 metadata 解决的是“看不看得见、解释不解释得动”。
- `Full Generic Sharing` 解决的是“能不能不再强依赖每个具体泛型实例都提前 AOT 出一份独立 native 实现”。

只要这条边界不先立住，后面所有判断都会偏。

## 先把真正的问题立住：AOT 泛型的上限，不只是一份 metadata 文件

在前面的 [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) 里，我们已经拆过一层：

`AOT generic method not instantiated in aot`

这类错误，根本上不是“metadata 没带够”，而是：

`这个具体泛型实例在 native 侧没有现成实现。`

为什么这件事在 IL2CPP 下特别容易成为上限？  
因为 IL2CPP 是 AOT runtime，它不能像 JIT 那样在运行时临时给你补一份新泛型实例的机器码。  
于是你会天然遇到两个压力：

- 你得尽量在打包前把可能用到的泛型实例都覆盖到。
- 但一旦泛型参数来自热更新代码，或者泛型组合太多，这件事很快就不现实。

也就是说，真正难的不是“泛型这个概念”，而是：

`AOT runtime 如何在不现场 JIT 的前提下，尽可能覆盖更多实际会发生的 generic 调用。`

这就是 `Full Generic Sharing` 要碰的问题空间。

## 旧 generic sharing 为什么总会在一部分场景里露出上限

先把一个背景说清楚。  
`IL2CPP` 本来就不是完全不会做 generic sharing。  
问题在于，旧的 sharing 有边界，它并不能把所有泛型实例都收敛到同一类共享代码路径里。

从公开文档和既有行为看，最容易撞上边界的通常是这两类场景：

### 1. 值类型泛型实例

像 `List<int>`、`Dictionary<int, Foo>`、`Func<MyStruct, int>` 这类场景，往往比引用类型更难共享。

原因不难理解。  
值类型会牵涉到：

- 实际数据布局
- 拷贝语义
- 装箱拆箱
- 参数和返回值在调用约定里的传递方式

这些东西一旦不同，runtime 就更难把它们都压成“同一份共享方法体 + 很少量额外上下文”的形式。

### 2. 泛型参数来自热更新类型

这类场景更直接。  
如果你在热更新代码里新定义了一个 `struct` 或某个新的泛型组合，AOT 主包在构建时根本不可能预先知道它。

这时你很容易进入一个尴尬状态：

- 语义上我明明只是用了一个普通的泛型类或泛型函数。
- 但运行时真正卡住的，是这个具体实例没有 native instantiation。

于是“多写点预实例化代码”这条路，理论上能解一部分，工程上却很难成为长期方案。

## 补充 metadata 为什么能救场，但为什么它不是同一层解法

HybridCLR 的补充 metadata 技术，价值非常大，但它的层级必须摆对。

它做的事情，本质上是：

- 让 runtime 重新拿回某些 AOT assembly 的 metadata 可见性
- 让解释器能继续解析 method body、泛型定义和相关签名信息
- 把一部分原本“native 侧缺实例”的场景，转成“解释器还能继续执行”

你会发现，这条路的核心是：

`把问题接回 interpreter 能消费的 metadata 和 method body。`

这条路当然有效，所以社区版才能把大量 AOT 泛型问题从“完全无解”拉回“可以跑”。  
但它也天然保留了自己的边界：

- 你通常还得携带或下载补充 metadata dll
- 运行时仍然要显式加载这些 metadata
- 包体、内存、加载链会更重
- 泛型调用如果最终走的是解释器路径，性能边界也还在那里

所以它更像：

`用 metadata 可见性 + interpreter 执行能力，把 AOT 泛型缺口托住。`

而不是：

`从 native generic runtime 模型本身，重新抬高泛型覆盖率上限。`

这就是为什么官方文档会把“补充 metadata”和“`Full Generic Sharing`”明确当成两条不同路线。

## `Full Generic Sharing` 真正想解决的是什么

如果把官方公开表述和我们当前系列已经拆出来的边界放在一起看，`Full Generic Sharing` 想要达成的目标其实很清楚：

- 不再依赖补充 metadata dll 这条工作流
- 在更多 AOT 泛型场景下直接获得可运行能力
- 同时把包体和内存成本继续压下去

把这几条放在一起，你就能得到一个很稳的判断：

`Full Generic Sharing` 追求的不是“让解释器拿到更多信息”，而是“尽量让更多 generic 调用本身就不必再退回补 metadata + interpreter 这条兜底路线”。

换句话说，它更像是在做这件事：

`把更多原本要求“每个具体实例各有一份 native 实现”的 generic 调用，改造成“共享方法体 + 额外运行时上下文”也能成立。`

只要这个方向成立，后面很多官方行为就都讲得通了：

- 为什么它能简化 workflow
- 为什么它能减少 metadata dll 带来的包体和内存负担
- 为什么它会直接影响 generic function 的运行时成本

因为它已经不再是“多带一份信息”，而是“换了一种 generic 执行模型”。

## 如果只按公开线索推断，它大概率会碰到哪几层

下面这部分，我刻意按“能确定什么”和“高可信推断什么”拆开写。

### 能确定的部分

从官方公开文档里，至少有这几件事是可以确定的：

- `Full Generic Sharing` 是商业版能力，不是社区版主线的一部分。
- 它和补充 metadata 是两条不同路线。
- 它的目标包括简化 workflow、减少补充 metadata dll 依赖、降低包体和内存。
- 它依赖 Unity 较新的 `IL2CPP` generic sharing 能力，`Unity 2020` 不支持，`Unity 2021` 需要依赖 `Code Generation = Faster (smaller) builds` 这条能力线，`Unity 2022` 则已经默认开启。

到这里，其实已经足够支撑“它不只是 metadata 技术”这个结论了。

### 高可信推断一：它一定碰 generic 调用签名本身

边界篇里提到的 `MethodInfo.has_full_generic_sharing_signature` 已经把方向暴露得很明显了。

只要一个能力开始影响 “method signature 是否属于 full generic sharing 形态”，那它就已经不再是 metadata 存不存在的问题，而是：

- 调用时怎么解释这个方法签名
- 调用方和被调方如何约定参数布局
- 共享方法体到底拿什么上下文去区分具体实例

也就是说，它首先碰的是“调用模型”，不是“描述文件”。

### 高可信推断二：它会连带影响 `methodPointer / invoker_method / virtualMethodPointer`

只要 generic 调用不再强依赖“每个闭包实例一份专属 native 实现”，runtime 就必须重新保证几类入口的一致性：

- 反射调用看到的 `invoker_method`
- 普通直接调用最终落到的 `methodPointer`
- 虚调用路径依赖的 `virtualMethodPointer`

否则你会出现一种很难接受的裂缝：

- 直调能跑
- 反射不能跑
- 虚调用和委托调用又是另一套行为

所以从运行时结构上看，它几乎不可能不碰这几条分发路径。

### 中可信推断三：它会更深地依赖 generic context，而不是每次都依赖具体实例化代码

这条推断的根据也很直接。  
如果目标真的是“让更多具体实例共用更少的 native 方法体”，那 runtime 就必须把“实例差异”更多地收敛到上下文里，而不是全部收敛到“不同机器码”里。

你可以把它粗略理解成：

- 旧路径更依赖“这个具体实例有没有单独 AOT 出来”
- 新路径更依赖“这份共享代码能不能带着足够的 generic context 正确解释当前实例”

这也是为什么它天然更像 runtime generic model 的升级，而不是 metadata 补丁。

### 中可信推断四：某些 delegate / marshaling 限制会跟着变化

边界篇里也已经提过，`Full Generic Sharing` 会触到一部分 generic delegate marshaling 限制。

这不奇怪。  
因为 delegate、reverse P/Invoke、native/managed 边界，本来就是对调用约定最敏感的地方。  
只要你开始改变 shared generic method 的签名形态，边界层的 wrapper 和适配逻辑就一定会被牵连。

所以这类能力很难是“只改一个开关，别的都不动”。

## 它换来的到底是什么

如果把收益和代价都摆到工程视角里，我会这样总结。

### 收益一：工作流更短

这是最直观的一条。  
少一套补充 metadata dll 的携带、分发、加载链，意味着：

- 包体组织更简单
- 热更下载链更短
- 启动时少一段显式 metadata 装载流程

这对包体治理和运行时初始化顺序，都是实打实的减负。

### 收益二：包体和内存压力更小

既然目标就是不再依赖一批额外 metadata dll，包体和内存占用自然会下降。  
这也是为什么官方会把它和“更小包体、更低内存”绑定着讲。

对小游戏、WebGL、iOS 主包大小敏感项目来说，这一点尤其有吸引力。

### 收益三：把一部分泛型场景从 interpreter 兜底，抬回更高性能路径

这条最容易被误读成“它一定全面更快”。  
更准确的说法应该是：

`它让更多 generic 调用不必先依赖补 metadata + interpreter 兜底，因此有机会停留在更高性能的 native shared path 上。`

这和“所有泛型都没有额外代价”不是一回事。  
它只是把上限往上抬了。

## 它额外付出的代价是什么

如果只讲收益，这篇就会重新变成宣传文。  
真正工程化的判断，必须把代价也一起说清楚。

### 代价一：它不是免费午餐，generic function 会有性能折价

官方公开文档已经明确提醒过：  
`Full Generic Sharing` 会带来 generic function 性能下降。

这其实完全符合前面的判断。  
既然你不再要求“每个具体实例一份最直接的专属 native 实现”，那运行时就要在：

- 参数适配
- 上下文传递
- shared body 分发

这些地方多付出一些成本。

所以它的真实语义不是“又全又快还不要钱”，而是：

`用更高的 generic 覆盖率、更轻的 workflow，换取一部分 generic function 的运行时折价。`

### 代价二：它强依赖 Unity / IL2CPP 版本能力线

这条也很现实。  
它不是一项完全自洽的独立 runtime 黑科技，而是建立在 Unity 新版 `IL2CPP` generic sharing 基础之上的扩展。

这意味着：

- `Unity 2020` 这条线不用想
- `Unity 2021` 需要结合代码生成模式看
- `Unity 2022` 才进入更自然的可用区间

所以它不是一个“任何项目都能随手加”的能力，而是和引擎版本绑定得很紧。

### 代价三：它解决的是 generic 覆盖率上限，不是所有泛型问题的总开关

就算用了 `Full Generic Sharing`，也不等于你可以把下面这些判断全部删掉：

- 泛型是否真的适合长期留在热更层
- 高频 generic 调用是不是热点
- native/managed 边界是不是被放大了
- 某些 delegate / marshaling 场景是不是仍然敏感

也就是说，它解决的是一层很重要的上限问题，但不会替你消灭工程判断。

## 项目里什么时候该认真考虑它

如果你的项目已经出现下面这些信号，那 `Full Generic Sharing` 就值得认真进入方案表了：

- 你已经被 AOT 泛型覆盖率和补充 metadata workflow 反复绊住
- 热更里有不少泛型值类型、复杂泛型组合，靠 AOT 预实例化越来越难兜住
- 包体、内存或热更下载链对 metadata dll 很敏感
- 你想尽量减少“能跑是能跑，但最后还是要靠 interpreter 兜底”的比例

反过来，如果你现在的项目特征是这样：

- 业务热更为主，泛型压力没那么高
- 现有补充 metadata 工作流已经稳定
- Unity 版本还不在合适区间
- generic function 本身又是明确性能热点

那它就不一定是你最先该上的能力。

## 最后压一句话

如果只允许我用一句话收这篇文章，我会写成：

`Full Generic Sharing` 真正抬高的，不是 metadata 可见性，而是 IL2CPP generic runtime 的共享上限；它把一部分原本只能靠“补 metadata + interpreter”兜底的泛型场景，重新拉回 shared native path，但代价是更复杂的 generic 调用模型、对 Unity 版本更强的依赖，以及一部分泛型函数性能折价。`

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}}">HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}}">HybridCLR DHE｜为什么它不是普通解释执行更快一点</a>
