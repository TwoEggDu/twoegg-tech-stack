+++
date = 2026-03-23
title = "HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT"
description = "不做空泛 benchmark，而是把 HybridCLR 的成本拆成几层：首调 transform、长期解释执行、跨 ABI 调用，以及 PreJit 和运行时选项到底该怎么用。"
weight = 38
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "Performance", "Runtime"]
series = "HybridCLR"
+++

> HybridCLR 的性能问题，最容易被一句“解释器比 AOT 慢”带过去；但真正能指导工程决策的，不是这句大而化之的判断，而是先分清你现在付出的成本，到底来自首调 transform、长期解释执行，还是跨 ABI 边界。

这是 HybridCLR 系列第 9 篇。  
前面几篇已经把主链、工具链、AOT 泛型、资源挂载、best practice 和故障诊断都立住了；这一篇不再补新的系统边界，而是回到一个更工程的问题：

`HybridCLR 在项目里怎么用，才能既保住热更灵活性，又不把性能代价放大到不可控？`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. HybridCLR 的性能成本，真正来自哪几层，而不是笼统地说“解释器慢”。
2. `PreJitMethod / PreJitClass` 到底在预热什么，它为什么不等于 native JIT。
3. 哪些逻辑适合长期留在 interpreter，哪些更应该前移或回到 AOT。
4. 高频跨 interpreter / AOT / native 边界时，什么才是真正该担心的成本。
5. `RuntimeOptionId` 这类运行时选项该怎么理解，哪些是调优旋钮，哪些不是“性能魔法开关”。

## 先给一句总判断

如果先把这篇压成一句话，我的判断是：

`HybridCLR 的性能治理，不是想办法把所有热更逻辑都“优化成 AOT”，而是先把成本拆层：该前移的前移，该避免跨边界的避免跨边界，该留在解释器的留在解释器。`

这件事一旦按层理解，很多策略都会自然很多。

![HybridCLR 性能分层图](../../images/hybridclr/performance-layers.svg)

*图：如果不先把成本拆成“首调、稳态、跨边界、启动期聚集”这几层，后面的优化动作很容易打错位置。*

## 不要把性能问题压扁成一句“解释器慢”

如果只用一句“解释器比 AOT 慢”来讲 HybridCLR，工程上几乎没有指导价值。  
因为项目里真实会感觉到的成本，至少来自 4 层。

### 1. 首次调用成本

也就是第一次真正执行某个解释器方法时，`GetInterpMethodInfo -> HiTransform::Transform` 这条链要不要现场发生。

这一层的特点是：

- 更像尖峰成本
- 容易集中在启动期、首开 UI、首次进入玩法时暴露
- 不是长期每帧都重复付出

### 2. 长期解释执行成本

也就是方法已经拿到 `InterpMethodInfo` 之后，长期跑在 `Interpreter::Execute` 里的成本。

这一层的特点是：

- 更像常驻成本
- 会和调用频率、方法复杂度、数据路径相关
- 不会因为做一次预热就消失

### 3. 跨 ABI 边界成本

这一层常被低估。  
很多时候热点并不在解释器方法体本身，而在：

- interpreter 调 AOT
- interpreter 调 native
- delegate / reverse PInvoke
- function pointer / `calli`

这些路径除了正确性，还会带来桥接和参数搬运成本。

### 4. 启动期集中爆发成本

这一层也很常见。  
即使单个方法的 transform 或解释执行不夸张，但如果你把：

- 补充 metadata
- 热更程序集加载
- 首批关键方法的首次执行
- 资源世界大规模启动

都堆在同一帧，那用户感知到的就不是“解释器慢”，而是明显的启动抖动。

所以这篇后面所有建议，都会围着这 4 层展开。

## `PreJitMethod / PreJitClass` 真正预热的不是 native 代码，而是解释器方法信息

这件事必须先说死，不然后面所有“预热”讨论都会歪。

先看 `Packages/HybridCLR/Runtime/RuntimeApi.cs`：

```csharp
/// prejit method to avoid the jit cost of first time running
public static extern bool PreJitMethod(MethodInfo method);

/// prejit all methods of class to avoid the jit cost of first time running
public static extern bool PreJitClass(Type type);
```

它自己把这件事叫 `PreJit`。  
但如果你继续看 `HybridCLRData/LocalIl2CppData-WindowsEditor/il2cpp/libil2cpp/hybridclr/RuntimeApi.cpp`，就会发现它真正做的不是把 IL JIT 成 native 代码：

```cpp
int32_t PreJitMethod0(const MethodInfo* methodInfo)
{
    if (!methodInfo->isInterpterImpl)
    {
        return false;
    }
    ...
    return interpreter::InterpreterModule::GetInterpMethodInfo(methodInfo) != nullptr;
}
```

再往下看 `HybridCLRData/LocalIl2CppData-WindowsEditor/il2cpp/libil2cpp/hybridclr/interpreter/InterpreterModule.cpp`：

```cpp
InterpMethodInfo* InterpreterModule::GetInterpMethodInfo(const MethodInfo* methodInfo)
{
    if (methodInfo->interpData)
    {
        return (InterpMethodInfo*)methodInfo->interpData;
    }
    ...
    InterpMethodInfo* imi = transform::HiTransform::Transform(methodInfo);
    const_cast<MethodInfo*>(methodInfo)->interpData = imi;
    return imi;
}
```

这就把语义钉死了：

`PreJitMethod / PreJitClass 预热的是解释器方法链，也就是提前触发 transform 并把 InterpMethodInfo 缓存到 method->interpData。`

所以它的价值是：

- 避免首调时现场做 transform
- 把一部分首次执行成本前移到你可控的时机

它没有做的事是：

- 不会把热更方法变回 AOT native 代码
- 不会消灭长期解释执行成本
- 不会绕开泛型实例是否存在、bridge 是否匹配这些更深层约束

## `PreJitClass` 和 `PreJitMethod` 不是想调就都能调

这一点源码里也写得很清楚。

`PreJitMethod0` 里会提前拦掉几类情况：

- 不是解释器方法
- 泛型类型本身还没实例化好
- 泛型方法本身不满足条件
- inflated generic context 里还有未实例化类型

也就是说：

`PreJit` 不是“给任何方法加速”的万能按钮，而是“对满足条件的解释器方法，提前完成 transform 和缓存”。`

这也是为什么我不建议把它宣传成“性能修复总开关”。

## 什么时候该用 `PreJit`

我觉得最稳的判断很简单：

`只对“马上就会被用户感觉到的首次调用路径”做预热。`

### 适合 `PreJit` 的场景

- 启动后第一批必进的热更入口
- 首屏 UI 打开时一定会触发的一组方法
- 进入主玩法第一帧就会跑的热更逻辑
- 明确知道首次执行尖峰比长期成本更值得治理的路径

这类场景的共同点是：

- 方法数有限
- 首调时机可预测
- 用户对首调抖动敏感

### 不适合 `PreJit` 的场景

- 想对整个大程序集一股脑“全预热”
- 大量并不一定会执行的方法
- 泛型和边界复杂、成功率本来就不稳定的整类路径
- 你其实根本没有首调问题，只是长期每帧热点在解释器里

因为这类做法只是把成本提前堆到启动期，并不会真的减少总成本。

## 真正该前移的，通常不是“所有解释器方法”，而是“首调尖峰”

这是我对 `PreJit` 最核心的工程判断。

很多项目会本能地把“预热”理解成“先做越多越好”。  
但对 HybridCLR，这个判断通常不成立。

因为如果你把几十上百个方法的 transform 全堆到启动期：

- 首帧可能更稳了
- 启动整体却变重了
- 资源、程序集、metadata 也在同一时段竞争

用户最后感知到的，未必更好。

所以更稳的策略是：

`只把用户最容易感知到的那一小段首次调用路径前移，而不是把整个解释器世界都提前初始化。`

## 哪些逻辑适合长期留在 interpreter

这一点其实不需要玄学 benchmark，更多是工程分层判断。

我会优先把下面这些逻辑留在 interpreter：

- 变化频繁的业务逻辑
- 配置驱动、活动驱动、剧情驱动流程
- UI 组织和中低频交互
- 首次调用可控、长期热点不高的功能代码
- 热修收益明显大于原生性能收益的模块

这些逻辑留在 interpreter，通常是合理交易。  
因为你买到的是：

- 更强的迭代灵活性
- 更低的发版摩擦
- 更自然的 Unity/C# 工作流延续

## 哪些逻辑更应该前移或回到 AOT

我更警惕下面这些场景：

- 高频数值核心循环
- 长期每帧热点逻辑
- 大量重型集合运算或复杂算法核心段
- 高频跨 interpreter / AOT / native 边界调用
- 强依赖原生性能的底层系统

这类逻辑的问题往往不是“首调慢一下”，而是长期路径一直在付费。  
这时候最有效的策略通常不是多做 `PreJit`，而是重新放置边界：

- 把稳定核心段留回 AOT
- 让热更层更多做 orchestration，而不是做底层重活
- 减少解释器层和 native/AOT 层来回穿梭

## 高频跨边界调用，往往比单纯解释执行更值得先处理

这点很重要。

很多人一提 HybridCLR 性能，就只盯 `Interpreter::Execute`。  
但真实项目里，先爆掉的未必是方法体本身，而是这种路径：

- 热更逻辑每帧大量调 AOT 方法
- 热更 delegate 高频回调
- native 插件频繁回调 managed
- 复杂值类型参数在 bridge 上来回搬运

这时候即使方法体本身不大，跨边界也会把成本放大。

所以如果你已经确认热点在这类路径上，我更建议先做两件事：

1. 减少来回穿梭次数  
   尽量把一段逻辑聚在同一侧做完，而不是每步都跨边界。

2. 把边界收粗  
   一个粗一点的调用，通常比很多碎调用更好治理。

## `RuntimeOptionId` 是调优旋钮，不是性能魔法开关

前面几篇其实很少碰这个话题，但这一篇必须提一下。

在 `Packages/HybridCLR/Runtime/RuntimeOptionId.cs` 和 `HybridCLRData/LocalIl2CppData-WindowsEditor/il2cpp/libil2cpp/hybridclr/RuntimeConfig.cpp` 里，HybridCLR 暴露了一些运行时选项，比如：

- `InterpreterThreadObjectStackSize`
- `InterpreterThreadFrameStackSize`
- `MaxMethodBodyCacheSize`
- `MaxMethodInlineDepth`
- `MaxInlineableMethodBodySize`

这些东西确实能影响运行时行为。  
但我不建议把它们当成第一选择。

更稳的理解方式是：

- 先解决边界设计问题
- 再解决首调时机问题
- 最后才考虑这些旋钮是否值得调

也就是说：

`RuntimeOptionId 更像二阶调优工具，而不是一阶性能策略。`

如果边界本来就放错了，单纯加大缓存、栈大小或 inline 深度，通常只会把问题藏得更深。

## `PreJit` 还有一个容易忽略的工程含义：它会把成本搬到你调用它的地方

这一点说起来很朴素，但很关键。

因为一旦你调用 `PreJitMethod / PreJitClass`，你其实是在做一个很明确的选择：

`把方法第一次真正执行时要付出的 transform 成本，搬到当前时刻付。`

这意味着你最好把它放在这些位置：

- 加载完成后的空档期
- 进入关键界面前的可控节点
- 你能接受一次性初始化成本的阶段

而不是：

- 最繁忙的主线程高峰点
- 和大资源反序列化同时竞争的时刻
- 你其实并不确定用户会不会走到的路径

这不是代码层复杂问题，只是工程调度问题。  
但它对最终手感影响很大。

## 我最推荐的 3 条性能策略，不是调参数，而是调结构

如果只保留最有工程价值的三条，我会写成：

1. 先分清是首调尖峰，还是长期热点  
   首调问题优先考虑 `PreJit` 和调用时机；长期热点优先考虑边界重构。

2. 尽量减少高频跨 ABI 边界  
   很多时候桥接成本比单纯解释执行更该先处理。

3. 让 AOT 承担稳定底座，让 HotUpdate 承担变化层  
   真正高频、稳定、强性能敏感的逻辑，不要默认留在 interpreter。

这三条通常比“盲调运行时选项”更有效。

## 最后压一句话

如果只允许我用一句话收这篇文章，我会写成：

`HybridCLR 的性能治理，核心不是把解释器“调得像 AOT 一样快”，而是先把成本拆层，再决定哪些首调该前移、哪些热点该回到 AOT、哪些跨边界调用该收缩。`

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-troubleshooting-diagnose-by-layer.md" >}}">HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了</a>
- 下一篇：<a href="{{< relref "engine-notes/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}}">HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版</a>
