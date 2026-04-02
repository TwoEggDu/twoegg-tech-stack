---
title: "HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排"
date: "2026-03-31"
description: "不再按单个报错拆案例，而是把 HybridCLR AOT 泛型问题按坑型归类：async/UniTask、LINQ、Dictionary/ValueTuple、委托回调、自定义泛型容器各自的典型信号、根因和处理方式。"
weight: 52
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "Generics"
  - "UniTask"
  - "LINQ"
series: "HybridCLR"
---
> 项目里真正拖慢排障的，往往不是“没有思路”，而是每次都把同一类 AOT 泛型坑当成第一次见的新事故。

这是 HybridCLR 系列第 23 篇。

前几篇已经把判断链搭起来了：

- [HCLR-19]({{< relref "engine-notes/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) 讲决策入口
- [HCLR-20]({{< relref "engine-notes/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 讲共享类型判断
- [HCLR-21]({{< relref "engine-notes/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) 讲 DisStripCode 写法
- [HCLR-22]({{< relref "engine-notes/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) 讲协作关系

这一篇再往前走一步，不按单个案例拆，而是直接按**坑型**归类。

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 真实项目里最常见的 AOT 泛型坑，通常长成哪几类。
2. 每一类最典型的信号是什么。
3. 每一类的优先判断顺序是什么。
4. 每一类到底更偏向补 metadata、补 DisStripCode，还是查裁剪。

## 先给一句总判断

如果先把整件事压成一句话，我的判断是：

`AOT 泛型问题最稳的排法，不是盯着单条日志猜，而是先判断它属于哪种坑型；坑型一旦分对，后面的共享类型判断、DisStripCode 写法和 metadata 取舍才会稳定。`

## 第一类：async / UniTask 型

这一类最典型的特征，是你表面上没写什么泛型容器，但运行时却掉进了非常像“隐式泛型”的路径。

常见信号：

- `AsyncUniTaskMethodBuilder<T>`
- `AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>`
- `IlCppFullySharedGenericAny`
- 解释器 / builder / state machine 反复递归

这一类难点在于：

`你源码里没直接写出那个 AOT 泛型实例，但编译器替你写了。`

所以判断顺序要反过来：

1. 先看是不是 async / builder / state machine 路径
2. 再看 `AOTGenericReferences` 里有没有对应 builder / source / awaiter 需求
3. 再决定是补 metadata 恢复运行，还是补 DisStripCode / builder 引用回到 native

这类坑的关键词不是 `Dictionary`、`List`，而是：

`隐藏泛型`

## 第二类：Dictionary / List / ValueTuple 容器型

这是最容易被肉眼识别出来的一类。

常见信号：

- `Dictionary<K,V>::TryGetValue`
- `List<T>.ctor`
- `ValueTuple<...>.ctor`
- 错误日志里直接出现容器和闭包类型

这一类的难点不是“有没有泛型”，而是：

`你看到的闭包类型，和共享规则下该写进 DisStripCode 的类型，往往不是同一个。`

所以这类坑最稳的排法是：

1. 先判断 `K`、`V`、`T` 哪些位置是值类型
2. 再翻共享类型
3. 再决定补的是类型实例化还是方法实例化

这类坑最容易犯的错，是直接照抄日志里的泛型签名。

## 第三类：LINQ / 扩展方法型

这一类最容易被低估，因为源码看起来只是：

- `ToList`
- `Select`
- `OrderBy`
- `FirstOrDefault`

但这些调用背后经常会形成：

- 泛型方法实例
- 迭代器状态机
- delegate / closure

于是你表面上看到的是一行平平无奇的 LINQ，真正坏掉的可能却是：

- `Enumerable.ToList<T>`
- `Func<T, TResult>`
- 某个迭代器状态机的 AOT 泛型路径

这一类更稳的排法是：

1. 不要只看调用行长什么样
2. 先看最终缺的是容器、delegate 还是扩展方法本身
3. 再决定是补共享类型、delegate 签名，还是先回 builder / iterator 路径

这类坑常见于“代码很短，但链条很长”。

## 第四类：委托 / 泛型回调型

这类问题最典型的外观是：

- `Action<T>`
- `Func<T1,T2>`
- 泛型事件总线
- 泛型接口回调

它的难点是：

`表面上你觉得自己只是在传一个回调，但 AOT 世界真正需要的是那组具体签名的实例。`

这类坑最稳的排法是：

1. 先确认缺的是 delegate 签名还是被调方法
2. 再判断泛型参数哪些落到 `object`，哪些保值类型外壳
3. 如果跨 ABI 或 native callback，再顺手查 MethodBridge 那一层

它最容易和“普通方法没实例化”混在一起。

## 第五类：自定义泛型容器 / 第三方库型

这是最难一眼看透的一类。

最常见的外观不是 BCL，而是你项目里自己的：

- `MessageBus<T>`
- `Pool<T>`
- `Cache<TKey, TValue>`
- 第三方库的 strongly typed API

这一类的难点在于：

`它没有标准答案，也没有现成案例，必须把它拆回“这个泛型类 / 泛型方法在共享规则下到底落成什么”。`

所以排法反而要更老实：

1. 先确定缺的是类型还是方法
2. 再按 `HCLR-20` 那套共享类型判断翻译
3. 再按 `HCLR-21` 的模板写出最小可见引用
4. 最后回头看 `AOTGenericReferences` 和 metadata 该怎么配合

自定义泛型最忌讳的，不是“不会写”，而是：

`以为它跟 List/Dictionary 没什么区别，结果把共享规则套错层。`

## 给每一类都压一个排障入口

如果只想快速判断，我建议先问下面 5 句：

1. 这是不是 async / builder / state machine 路径？
2. 这是容器型闭包类型，还是隐藏泛型？
3. 错在类型实例，还是方法实例？
4. 这里缺的是 native 实现，还是先缺 metadata？
5. 这是标准库坑，还是项目自定义泛型坑？

只要这 5 句先问完，你基本就不会再把所有问题都塞进“再补一个 object 看看”这条路里。

## 最后给一张压缩表

| 坑型 | 最典型信号 | 第一优先动作 | 最常见误判 |
|---|---|---|---|
| async / UniTask | builder、state machine、`IlCppFullySharedGenericAny` | 先认隐藏泛型路径 | 以为只是 metadata 问题 |
| 容器 / ValueTuple | `Dictionary`、`List`、`ValueTuple` | 先翻共享类型 | 直接照抄日志闭包类型 |
| LINQ / 扩展方法 | `ToList`、`Select`、`OrderBy` | 先拆到真实泛型方法 / delegate | 以为 LINQ 本身不算泛型坑 |
| 委托 / 回调 | `Action<T>`、`Func<T>`、泛型事件 | 先认签名实例 | 只保了被调方法，没保签名 |
| 自定义泛型容器 | 项目自写 `Pool<T>`、`Cache<K,V>` | 先拆类型 / 方法层 | 套用标准库经验太早 |

## 把这件事压成一句话

> AOT 泛型问题真正高效的排法，不是每次都从一条日志重新猜，而是先判断它属于哪种坑型；坑型分对之后，共享类型、DisStripCode 和 metadata 的处理顺序才会稳定。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}">HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合</a>
- 下一篇：<a href="{{< relref "engine-notes/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}}">HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里</a>
- 相关前文：<a href="{{< relref "engine-notes/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}}">HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法</a>
- 相关前文：<a href="{{< relref "engine-notes/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}">HybridCLR 案例｜Dictionary&lt;ValueTuple, 热更类型&gt; 的 MissingMethodException 与 object 替代法</a>
