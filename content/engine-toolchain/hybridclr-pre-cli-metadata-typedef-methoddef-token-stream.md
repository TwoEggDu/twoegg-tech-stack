---
title: "HybridCLR 前置篇｜CLI Metadata 基础：TypeDef、MethodDef、Token、Stream 到底是什么"
date: "2026-04-13"
description: "从 PE 文件结构、5 个 metadata stream、核心 metadata 表、token 编码，到 method body 定位，给 Unity 工程师补一份 CLI metadata 的最小必要知识，为后续 HybridCLR 源码阅读建立语义基础。"
weight: 10
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "ECMA-335"
  - "Metadata"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
series_order: -2
---
> "metadata"这个词在 HybridCLR 系列里出现了 200 多次，但从来没有一篇文章正式定义过它到底包含什么。这篇补上。

这是 HybridCLR 系列的前置篇 A，位于正式系列第 1 篇之前。

它不讲 HybridCLR 源码，只做一件事：把 CLI metadata 里那些最常被源码引用的结构讲清楚，让后面的文章不再只是在看符号。

## 为什么要先讲 metadata

如果你直接打开 HybridCLR 的源码，会发现几乎每条核心链路都绑在 metadata 上：

- `Assembly.cpp` 装载热更 DLL 时，第一步就是解析 metadata
- `AOTHomologousImage` 补充的就是 AOT 程序集的 metadata 快照
- `MethodBodyCache` 用 (image, token) 做缓存 key，这里的 token 就是 metadata token
- `HiTransform::Transform` 的输入就是从 metadata 里取出来的 method body

如果不知道 metadata 里到底装了什么，这些结构看起来就只是一堆变量名。你能看到代码在操作 `typeDefIndex`、`methodDefIndex`、`token`，但不知道它们指向的是什么结构、记录的是什么信息。

所以这篇先把 metadata 本身讲清楚，后面的源码阅读才是在看语义而不是看符号。

## 一个 .NET DLL 里到底装了什么

Unity 工程师最常打交道的 .NET 产物是 DLL 文件。在 HybridCLR 场景下，热更 DLL 和 AOT 补充 DLL 都是标准的 .NET 程序集。

一个 .NET DLL 的物理结构大致可以分成四层：

1. **PE header** — 标准的 Windows PE 格式头。IL2CPP 和 HybridCLR 在运行时并不直接依赖 PE 加载器，但 DLL 文件本身仍然遵循这个格式。
2. **CLI header** — 也叫 CLR header，记录 metadata 的起始位置、EntryPoint token、runtime 版本等基本信息。
3. **Metadata** — 类型定义、方法定义、字段定义、签名、字符串、跨程序集引用，全都在这里。
4. **IL code** — 方法体的 CIL 字节码。metadata 里的 MethodDef 表通过 RVA 字段指向这些字节码的物理位置。

这里最关键的认知是：metadata 不是"附加信息"，不是"调试用的注释层"。它是运行时的核心数据结构。

没有 metadata，runtime 就不知道一个类有哪些方法、一个方法的参数签名是什么、一个字段的类型是什么。IL 代码本身只有操作指令，操作对象的描述全在 metadata 里。

用一个类比：如果 IL code 是菜谱里的操作步骤，metadata 就是食材清单、工具清单和所有名词的定义。没有定义，步骤就是一串无法执行的动词。

## 5 个 metadata stream

metadata 在物理上被组织成 5 个 stream（堆/流）。每个 stream 存一类数据：

| Stream | 存什么 | 一句话说明 |
|--------|--------|-----------|
| **#Strings** | 标识符字符串 | 类型名、方法名、命名空间、字段名。UTF-8 编码，以 null 结尾 |
| **#US** | 用户字符串 | 代码里 `ldstr` 加载的字面量字符串。UTF-16 编码 |
| **#Blob** | 二进制数据 | 方法签名、字段签名、自定义属性值、常量值。变长编码 |
| **#GUID** | GUID | 模块标识。每个条目固定 16 字节 |
| **#~** | metadata 表 | 所有结构化的 metadata 记录。这是最核心的 stream |

前四个 stream 本质上都是"堆"——一块连续的字节区域，表里的记录通过偏移量索引到这些堆里去取具体值。

比如 TypeDef 表里某一行的 TypeName 字段，存的不是字符串本身，而是一个指向 #Strings 堆的偏移量。runtime 拿到偏移量后去 #Strings 堆里读出 null 结尾的 UTF-8 字符串，才能知道这个类型叫什么名字。

#~ stream 才是 metadata 的主体。它包含了所有结构化的 metadata 表，下一节展开。

## 核心 metadata 表

#~ stream 里定义了几十张表（ECMA-335 规范定义了 45 张），但对理解 HybridCLR 来说，只需要先记住下面这些：

**TypeDef（表编号 0x02）** — 类型定义表。每一行描述当前程序集里定义的一个类型：类名、命名空间、基类、字段列表起始行、方法列表起始行。当 runtime 需要加载一个类型时，第一步就是查 TypeDef。

**MethodDef（表编号 0x06）** — 方法定义表。每一行描述一个方法：方法名、签名（指向 #Blob 堆）、IL 方法体的 RVA、实现标志。HybridCLR 的 MethodBodyCache 要做的事情，就是根据 MethodDef 的 RVA 找到方法体并解析。

**FieldDef（表编号 0x04）** — 字段定义表。每一行描述一个字段：字段名、签名、访问标志。它和 TypeDef 之间的关系是：TypeDef 表记录了"从哪一行 FieldDef 开始是这个类型的字段"。

**TypeRef（表编号 0x01）** — 类型引用表。当代码引用了其他程序集里定义的类型时，引用信息就记在这张表里。它不存完整定义，只存名字和来源，runtime 需要到目标程序集的 TypeDef 表里去解析。

**MemberRef（表编号 0x0A）** — 成员引用表。跨程序集调用方法或访问字段时，引用信息记在这里。类似于 TypeRef 之于 TypeDef 的关系。

**TypeSpec（表编号 0x1B）** — 泛型类型实例化表。`List<int>`、`Dictionary<string, int>` 这类具体泛型实例的签名存在这里。HybridCLR 在处理 AOT 泛型时频繁查这张表。

**MethodSpec（表编号 0x2B）** — 泛型方法实例化表。`Foo<int>()`、`Bar<string, int>()` 这类具体泛型方法实例的签名存在这里。和 TypeSpec 配合，覆盖了泛型的两个维度。

这些表之间的关系可以这样理解：TypeDef 和 MethodDef 描述的是"当前程序集定义了什么"；TypeRef 和 MemberRef 描述的是"当前程序集引用了别人的什么"；TypeSpec 和 MethodSpec 描述的是"泛型定义被实例化成了什么"。

## metadata token 的结构

metadata 表里的每一行都可以用一个 32 位的 token 来唯一标识。token 的编码规则非常简单：

- 高 8 bit = 表编号
- 低 24 bit = 行号（从 1 开始）

举一个具体例子：

```
token = 0x06000042
```

拆开看：

- `0x06` = MethodDef 表（表编号 6）
- `0x000042` = 第 66 行（0x42 = 十进制 66）

所以 `0x06000042` 的含义是：MethodDef 表的第 66 行。

再看几个常见的表编号：

| 表编号 | 表名 | 含义 |
|--------|------|------|
| 0x01 | TypeRef | 类型引用 |
| 0x02 | TypeDef | 类型定义 |
| 0x04 | FieldDef | 字段定义 |
| 0x06 | MethodDef | 方法定义 |
| 0x0A | MemberRef | 成员引用 |
| 0x1B | TypeSpec | 泛型类型实例 |
| 0x2B | MethodSpec | 泛型方法实例 |

这个编码规则解释了 HybridCLR 里一个非常常见的模式：用 `(image, token)` 做缓存 key。

image 标识"哪个程序集"，token 标识"这个程序集的 metadata 表里的哪一行"。这两个值组合起来就能唯一定位一个 metadata 记录。在 `MethodBodyCache` 里，key 就是 `(image, methodToken)`，value 是解析和 transform 后的方法体。

理解了 token 的结构，再看源码里那些 `token >> 24`、`token & 0x00FFFFFF` 的位操作就不再是魔法数字了——前者取表编号，后者取行号。

## method body 在哪

MethodDef 表的每一行有一个 RVA（Relative Virtual Address）字段，它指向 IL method body 在文件中的物理偏移。

一个完整的 method body 包含四个部分：

1. **Method header** — 分 tiny format 和 fat format 两种。tiny format 只有 1 字节，用于方法体小于 64 字节且没有局部变量和异常处理的简单方法。fat format 有 12 字节，记录最大栈深度、代码长度、局部变量签名 token 等信息。
2. **IL bytes** — CIL 指令字节码。这是方法体的核心内容。
3. **Exception handling table** — try-catch-finally 的区间信息。只在 fat format 且设置了 `CorILMethod_MoreSects` 标志时存在。
4. **Local variable signature** — 局部变量的类型签名，存在 #Blob 堆里，通过 fat header 里的 LocalVarSigTok 字段引用。

HybridCLR 的 `MethodBodyCache` 缓存的就是解析后的这个结构。当一个热更方法第一次被调用时，runtime 通过 MethodDef 的 RVA 定位到 method body，解析出 IL bytes 和其他信息，然后交给 `HiTransform::Transform` 转换成内部 IR。转换后的结果缓存起来，后续再次调用同一个方法时直接命中缓存。

所以 method body 的位置链路是：

```
MethodDef 行 → RVA 字段 → 文件偏移 → method header → IL bytes
```

## 贯穿全系列的参考类

后续文章会反复用到同一组类型来展示不同 runtime 的实现差异。为了降低读者的上下文切换成本，这里先定义这组参考类：

```csharp
public interface IHittable
{
    void TakeDamage(int amount);
}

public class Unit : IHittable
{
    public int hp;
    public virtual void TakeDamage(int amount) { hp -= amount; }
}

public class Player : Unit
{
    public List<Item> inventory = new();
    public override void TakeDamage(int amount) { /* 带护甲计算 */ }
    public T GetItem<T>(int index) where T : Item { return (T)inventory[index]; }
}

public class Item
{
    public string name;
    public int weight;
}
```

这组类型覆盖了 CLI 类型系统的核心场景：

- **接口**（`IHittable`）→ 接口分派
- **继承**（`Unit → Player`）→ 虚方法、vtable
- **泛型方法**（`GetItem<T>`）→ 泛型实例化、约束
- **值类型字段**（`hp: int`、`weight: int`）→ 内存布局
- **引用类型字段**（`inventory: List<Item>`、`name: string`）→ GC 引用追踪
- **异常处理**（可在 `TakeDamage` 中加 try/catch 示例）→ 异常模型

后续各 runtime 模块的文章中，会尽量用这组类型来展示：metadata 表里 `Unit` 的 TypeDef 行、CIL 里 `TakeDamage` 的 IL 字节码、CoreCLR 里 `Unit` 的 MethodTable、IL2CPP 里 `TakeDamage` 的 C++ 翻译、LeanCLR 里 `Unit` 的 RtClass。

## 用 ildasm 看一个最小示例

下面用一个最简单的 C# 类来展示 metadata 表里实际存了什么：

```csharp
public class MathHelper
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
}
```

如果用 ildasm 打开编译后的 DLL，可以看到这些信息：

**TypeDef 表中的一行：**

```
TypeDef #2
  TypDefName: MathHelper
  Flags:      [Public] [AutoLayout] [Class] [AnsiClass] [BeforeFieldInit]
  Extends:    [TypeRef] System.Object
  Method #1:  Add
  Method #2:  .ctor
```

这里记录了类型名是 MathHelper，基类是 System.Object（通过 TypeRef 引用），包含两个方法——Add 和编译器自动生成的构造函数 .ctor。

**MethodDef 表中 Add 方法的行：**

```
Method #1
  MethodName: Add
  Flags:      [Public] [Static] [HideBySig] [ReuseSlot]
  RVA:        0x00002050
  ImplFlags:  [IL] [Managed]
  CallConv:   [DEFAULT]
  ReturnType: I4
  2 Arguments
    Argument #1: I4
    Argument #2: I4
```

RVA 为 0x00002050，指向 method body 在文件中的位置。签名显示两个 int 参数、返回 int。

**method body 的 IL 字节码：**

```
.method public hidebysig static int32 Add(int32 a, int32 b) cil managed
{
  .maxstack 2
  IL_0000: ldarg.0      // 把参数 a 压栈
  IL_0001: ldarg.1      // 把参数 b 压栈
  IL_0002: add          // 弹出两个值相加，结果压栈
  IL_0003: ret          // 返回栈顶值
}
```

这是一个 tiny format 的 method body——只有 4 字节 IL code，没有局部变量，没有异常处理。

把这三块信息串起来：TypeDef 表告诉 runtime "有一个叫 MathHelper 的类"；MethodDef 表告诉 runtime "这个类有一个叫 Add 的静态方法，签名是 (int, int) -> int，方法体在 RVA 0x00002050"；从 RVA 处读到的 method body 告诉 runtime "这个方法的 IL 指令是 ldarg.0 ldarg.1 add ret"。

## 这些概念在 HybridCLR 里对应什么

前面几节讲的是 ECMA-335 规范层面的概念。回到 HybridCLR 源码，这些概念有明确的对应关系：

| CLI metadata 概念 | HybridCLR 中的对应 |
|-------------------|-------------------|
| 5 个 metadata stream | `RawImageBase` 解析后持有的 5 个 stream 数据 |
| TypeDef 表 | `Il2CppTypeDefinition` 结构体 |
| MethodDef 表 | `Il2CppMethodDefinition` 结构体 |
| metadata token | `MethodBodyCache` 的缓存 key（与 image 组合） |
| method body | `HiTransform::Transform` 的输入 |
| TypeRef / MemberRef | 跨程序集解析时走 `MetadataModule` 的 resolve 链路 |
| TypeSpec / MethodSpec | AOT 泛型实例化解析和补充 metadata 的基础 |

当你在源码里看到 `GetTypeDefinition(typeDefIndex)` 时，它做的事情就是从 TypeDef 表里按行号取一行记录。当你看到 `GetMethodBody(token)` 时，它做的事情就是从 MethodDef 行里取 RVA，然后去对应偏移解析 method body。

所有的 `index`、`token`、`RVA` 都不是抽象概念，而是直接映射到 metadata 表的物理行和文件偏移。

## 收束

这篇文章只做了一件事：把 CLI metadata 的物理结构和核心概念交代清楚。

如果要压到最短，记住三条就够了：

1. metadata 是 .NET 程序集的核心数据结构，不是附加信息。它包含所有类型、方法、字段的定义和引用关系。
2. metadata token 是一个 32 位值，高 8 bit 是表编号，低 24 bit 是行号。HybridCLR 用 (image, token) 唯一定位一条 metadata 记录。
3. method body 通过 MethodDef 表的 RVA 字段定位。HybridCLR 的 MethodBodyCache 缓存的就是解析后的 method body，Transform 的输入也是它。

有了这些概念基础，后面系列里出现的 `typeDefIndex`、`methodDefIndex`、`token`、`RawImage`、`MethodBodyCache` 就不再是无根的变量名了。

## 系列位置

- 上一篇：无（前置层首篇）
- 下一篇：Pre-B CIL 指令集与栈机模型
