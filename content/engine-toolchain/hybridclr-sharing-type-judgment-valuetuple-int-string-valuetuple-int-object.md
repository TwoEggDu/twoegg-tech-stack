---
title: "HybridCLR 共享类型判断｜为什么报错是 ValueTuple<int,string>，你写的却可能是 ValueTuple<int,object>"
date: "2026-03-31"
description: "把 AOT 泛型修法里最容易误判的一层单独拆开：日志里的具体闭包类型和 DisStripCode 里该写的共享类型为什么经常不是同一个，以及该按什么规则判断。"
weight: 49
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "Generics"
  - "DisStripCode"
  - "SharingType"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

日志里出现 `ValueTuple<int,string>`，保留代码却使用 `ValueTuple<int,object>`，这不一定是写错。它可能是在为一组兼容实例准备共享实现。但判断对象必须落到**目标泛型方法**，只替换几个类型名还不够。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

本文按 [官方 AOT 泛型规则](https://www.hybridclr.cn/docs/basic/aotgeneric) 解释普通泛型共享。商业完全泛型共享是另一项能力，不把它的适用范围混进这里。

## 先区分参数的共享类型与方法的共享实例

`List<string>` 本身是一个引用类型。当它作为另一个泛型方法的类型参数时，可以参与引用类型的共享。

但我们要调用 `List<string>.Add` 时，目标仍然是 `List<T>` 的成员。应考虑 `List<object>.Add` 的共享实现，不能把目标改成 `object.Add`。

这是两次不同的运算：

| 问题 | 示例 | 结果含义 |
|---|---|---|
| 这个类型作为泛型参数时如何共享 | `Container<List<string>>` 中的 `List<string>` | 该参数是引用类型 |
| 这个泛型方法由哪个共享实例承载 | `List<string>.Add` | 保留 `List<T>` 方法身份，归一化其泛型参数 |

类的泛型参数和方法自己的泛型参数都要检查。例如 `Container<T>.Convert<U>` 同时包含 `T` 与 `U`，不能只看方法名后的那一组参数。

## 从参数向外递归计算

普通共享可以按下面的顺序理解：

- 引用类型参数通常归入引用共享形态；`object` 是常见的代表。
- 非泛型、非枚举值类型不能仅凭大小相同就互相替代。
- 枚举按相同底层类型的枚举共享，不能据此把所有枚举与任意整数实例都当成同一组。
- 泛型结构体保留自身泛型定义，再递归处理各参数。
- 泛型类作为**外层调用的类型参数**时，仍是引用类型。

因此 `ValueTuple<int,string>` 与 `ValueTuple<int,object>` 可对应到同一共享形态，而 `ValueTuple<int,int>` 仍保留第二个 `int`。不能因为二者都叫 ValueTuple，或恰好占用相同字节数，就认为 native 方法通用。

还要检查泛型约束。若定义要求某个接口、基类或 `new()`，机械把参数改成 `object` 可能连 C# 都无法编译。此时选择满足约束的代表，或直接评估补充元数据；不能绕过约束制造一个“看起来像共享”的调用。

## 把规则应用到具体成员

| 实际调用 | 要核对的共享方法形态 | 容易漏掉的点 |
|---|---|---|
| `List<string>.Add` | `List<object>.Add` | 只保留构造函数不等于保留 Add |
| `List<int>.Add` | `List<int>.Add` | `List<object>` 不能替代值类型参数 |
| `Dictionary<(int,string),HotClass>.set_Item` | `Dictionary<(int,object),object>.set_Item` | 前提是 HotClass 为引用类型 |
| `Dictionary<(int,int),HotClass>.TryGetValue` | `Dictionary<(int,int),object>.TryGetValue` | 必须检查 TryGetValue，不能拿 set_Item 代替 |
| `Outer<List<HotClass>>.Method` | 外层参数按引用形态判断 | 这不会自动覆盖内部所有 List 成员 |

表中是候选共享形态，不是跨版本的自动验收结果。显式引用一个可由编译器生成共享实现的原始 AOT 类型实例也可能有效，并非必须把源码写成 `object` 才有效。

对于 `ValueTuple`，还应观察实际 IL：默认初始化可能使用 `initobj`，显式构造、比较和字典查找会触发不同成员。不能只看到 `new ValueTuple<...>` 或一个局部变量，就宣称相关成员已经全部生成。

## 热更 struct 为什么需要另一种判断

如果类型参数是热更 DLL 里新增的结构体，主包构建时无法直接引用这个尚未存在的类型。创建一个同样大小的 AOT struct 也不等于创建了相同类型身份、约束、布局和调用语义。

这正是补充元数据有价值的场景：为相关 AOT 泛型定义补回方法体，让它能按真实类型参数解释执行。若已经有官方支持的其他共享能力，应按该能力的版本契约判断，不要自行用 dummy struct 模拟。

## 让计算结果落到证据上

计算共享形态后，记录“程序集 + 声明类型 + 方法 + 类参数 + 方法参数”。再查看裁剪后 IL 中的实际调用、生成代码或注册信息，并在 Player 执行目标成员。只确认类型能被反射找到，仍未证明方法实现可调用。

[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 已覆盖固定样本的值类型参数、补充 metadata 和显式 AOT 对照；上表的全部嵌套容器组合尚未逐项实测。需要手写候选引用时，继续 [DisStripCode 可验证模板]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})。
