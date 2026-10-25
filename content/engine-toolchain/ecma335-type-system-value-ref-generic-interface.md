---
title: "ECMA-335 基础｜类型系统：值、引用、泛型与接口"
slug: "ecma335-type-system-value-ref-generic-interface"
date: "2026-04-14"
lastmod: "2026-10-23"
description: "区分值复制、对象引用、托管指针和泛型上下文，为 IL2CPP 泛型共享与 HybridCLR 调用边界建立语义基础。"
weight: 12
featured: false
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "TypeSystem", "Generics"]
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
primary_series: "ecma335"
series_order: 12
---

把一个 class 改成 struct，可能同时改变赋值结果、装箱行为、泛型代码生成和跨边界传参。原因不是某个 runtime 对 struct “支持较差”，而是程序要求的语义已经改变。

这篇先区分三件事：**变量存什么、类型参数绑定成什么、某个方法怎样获得可执行实现。** 前两件由 CLI 类型模型描述，第三件需要具体 runtime 作出选择。

## 值复制与引用复制

```csharp
public struct Point
{
    public int X;
}

public sealed class Unit
{
    public int Hp;
}
```

复制 Point 变量，会复制它的值；复制 Unit 变量，会复制指向对象的引用：

```csharp
Point p1 = new Point { X = 1 };
Point p2 = p1;
p2.X = 2;                  // p1.X 仍为 1

Unit u1 = new Unit { Hp = 10 };
Unit u2 = u1;
u2.Hp = 20;                // u1.Hp 也为 20
```

值类型内部如果包含引用字段，复制的是那个引用，不会递归复制它指向的对象。因此 struct 的复制不等于对象图的深拷贝。

“值类型在栈上，引用类型在堆上”不能替代这个模型。值类型可内嵌在堆对象或数组中，也可以装箱；局部变量如何放入寄存器或栈，是实现选择。真正稳定的是赋值、访问和对象身份的语义。

## 对象引用与托管指针

| 形状 | 描述什么 | 常见来源 |
|---|---|---|
| 对象引用 O | 指向一个托管对象，也可为 null | class 变量、数组引用 |
| 托管指针 & | 指向某个存储位置 | ref 参数、局部变量地址、对象内部字段地址 |
| 非托管指针 * | 原始地址及相关操作 | unsafe 和原生互操作 |

`ref Unit` 指向的是“保存 Unit 引用的那个位置”，不等于普通 Unit 引用。被调用者可以替换该变量指向的对象。

托管指针也不天然意味着地址被永久固定。跨 native 边界时，地址是否可移动、需要 pin 多久、何时失效，要按具体运行时与互操作契约处理。

## 装箱改变存储形态

```csharp
Point p = new Point { X = 1 };
object boxed = p;
p.X = 3;
Point copy = (Point)boxed;   // copy.X 为 1
```

本例把 Point 的值复制到装箱对象中。随后修改 p 不会修改那个副本。CIL 中 `box`、`unbox` 与 `unbox.any` 的职责不同：unbox 取得指向装箱值的托管指针，unbox.any 对普通值类型取出相应的值。

因此“struct 没有对象头”只适用于未装箱的值存储，不能概括装箱后的形态。`Nullable<T>` 的装箱另有专门规则，不能用这一个 Point 例子推导所有细节。

## 泛型：定义、开放类型与封闭类型

`List<T>` 定义了一组可按类型参数构造的类型。`List<int>` 和 `List<string>` 是不同的封闭类型；`Dictionary<string, T>` 仍含未绑定参数，属于开放类型。

“不能创建开放类型的对象”不等于“runtime 不能表示开放类型”。反射可以拿到泛型定义；在泛型代码中写 `new List<T>()` 也是合法的，因为真正执行时会带上调用者的类型实参。

更需要分开的三个层次是：

| 层次 | 例子 | 应确认的事实 |
|---|---|---|
| 定义 | `List<T>`、`Box<T>.Echo<U>` | 类型／方法参数及成员定义 |
| 构造后的类型或方法身份 | `List<int>`、`Box<int>.Echo<string>` | 类型实参与方法实参、约束、布局 |
| 可执行实现 | 某个具体调用使用的 native 代码或解释路径 | AOT 产物、共享策略或可解释的方法体是否可用 |

不能写成“IL2CPP 运行时只能查表，所以任何新类型信息都不能构造”。也不能写成“只要反射能获得封闭类型，它的方法就一定可以调用”。HybridCLR 官方的 [AOT 泛型说明](https://www.hybridclr.cn/docs/basic/aotgeneric)专门区分了类型元数据与方法实现的可用性。

## 泛型在 metadata 中如何表达

GenericParam 记录类型或方法的参数；GenericParamConstraint 记录基类／接口约束；TypeSpec 和 MethodSpec 用签名表达相应的类型形状和方法实例化。

例如，一个泛型类型中的泛型方法有两组上下文：

```text
Box<int>.Echo<string>
    └ int：类型参数上下文
                └ string：方法参数上下文
```

IL 中用 `!0` 与 `!!0` 区分这两组参数。它们怎样连接到 MemberRef 和调用目标，下一篇 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}}) 用一个完整例子说明。

泛型代码共享是实现策略，不会抹掉封闭类型的身份。即使两种实例化共用方法代码，它们仍可能有各自的静态字段、反射身份和初始化状态，详见 [类型初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})。

## 约束提供保证，不替代存储和执行分析

```csharp
public static T Create<T>() where T : new() => new T();

public static void Reset<T>(ref T value) where T : IResettable
    => value.Reset();

public interface IResettable
{
    void Reset();
}
```

约束限制合法实参，并让编译器和运行时知道允许哪些操作。但它不自动承诺以下事情：

- `where T : struct` 不保证对象绝不分配在堆上，也不保证整个方法永不装箱。
- `where T : class` 不直接规定必须使用哪一种 native 代码共享策略。
- 通过反射构造泛型时，不能绕过约束；具体异常取决于 API 和失败阶段，不宜统一写成 TypeLoadException。

Reset 的接口调用是否需要装箱，还要看接收者表示和 `constrained.` 等 IL 语义，见 [调用与异常处理]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}})。

## 接口是契约，分派是执行机制

当变量类型是 IResettable 时，它表达“目标支持 Reset”，没有直接指定某个最终方法入口。运行时必须依据实际类型确定实现。

vtable、接口映射表、缓存或编译期去虚化，都是完成这项工作的方法。CLI 定义可观察的覆盖与调用语义，并不要求每个 runtime 使用同一张物理表、同一个 slot 编号或固定数量的间接查找。

这也是为什么不能直接从“接口调用”推导固定性能倍数。接口调用的语义、编译后是否还能去虚化、跨 AOT／解释边界的方式，需要分层确认。

## 读到 IL2CPP 与 HybridCLR 时怎么使用这些知识

遇到 `List<HotStruct>` 问题，先确认 HotStruct 是值类型、具体调用的成员及参数组合，再检查方法实现路径；遇到 struct 修改未生效，先排除复制或装箱；遇到接口调用问题，先确定实际接收者和分派语义。

共享规则、补充元数据、FGS 等机制放在 [泛型实现桥接]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) 继续讨论。它们需要满足类型语义，但并非类型规范本身。

规范依据：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I 的类型系统、Partition II 的泛型，以及 Partition III 的装箱与调用指令。

**先确定值、引用和泛型身份，再判断存储、分派与执行路径，才能把 AOT 问题定位到正确的一层。**

## 系列位置

- 上一篇：[DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})
- 下一篇：[签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
