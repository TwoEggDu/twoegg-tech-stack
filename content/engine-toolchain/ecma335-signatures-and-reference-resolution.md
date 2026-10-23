---
title: "ECMA-335 基础｜签名与引用解析：一个泛型调用如何找到目标方法"
slug: "ecma335-signatures-and-reference-resolution"
date: "2026-10-23"
description: "用一个跨 DLL 泛型调用串起模块作用域、MemberRef、TypeSpec、MethodSpec、签名与泛型上下文，区分解析成功和拥有可执行实现。"
weight: 22
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "Metadata", "Generics"]
---

> **读这篇之前**：先读 [DLL 与 metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) 和 [类型系统]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})。本篇的 IL 只用于观察引用关系，逐条执行留到下一篇。

热更代码调用一个泛型方法，报错信息里同时出现方法名、泛型参数和程序集名。只按方法名搜索，经常会发现“它明明存在”。问题在于，运行时需要的目标远比一个名字精确：声明类型、方法签名、类型实参、方法实参，以及它们所属的模块，都参与这次调用。

这篇先建立一个模型：**token 定位记录，签名描述形状，上下文绑定参数；解析出目标之后，才轮到执行实现。**

规范基线为 [ECMA-335 第六版](https://ecma-international.org/publications-and-standards/standards/ecma-335/)，重点对应 Partition II 的 metadata 表与签名编码。下面的表项行号、程序集版本和 IL 偏移不作为固定值。

## 一个名字为什么不够

把示例拆成两个项目。共享库 `Shared.dll` 中放：

```csharp
namespace Shared
{
    public sealed class Box<T>
    {
        public U Echo<U>(T key, U value) => value;
    }
}
```

引用它的 `HotEntry.dll` 中放：

```csharp
using Shared;

public static class Entry
{
    public static string Run()
        => new Box<int>().Echo<string>(7, "ok");
}
```

这里有两个独立的绑定：`Box<T>` 的 `T = int`，以及 `Echo<U>` 的 `U = string`。只记录 `Echo<string>` 仍然不够，因为它可能属于 `Box<int>`，也可能属于 `Box<long>`。

源代码中没有重载也必须保留签名。以后给 `Box<T>` 增加同名重载，已有调用不能因为名字相同就绑定到另一个方法。

## 把“定义、引用、构造后的类型”分开

| 记录 | 在本例中承担的职责 |
|---|---|
| TypeDef、MethodDef | 在 Shared 模块中定义 Box 和 Echo；MethodDef 关联方法签名与方法体 |
| AssemblyRef、TypeRef | 在 HotEntry 中描述对 Shared 程序集及 Box 类型的引用 |
| TypeSpec | 用类型签名表达 Box<int> 这样的构造类型 |
| MemberRef | 在 Box<int> 这个声明类型上下文中引用 Echo，并记录成员签名 |
| MethodSpec | 在所引用的泛型方法上绑定方法实参 string |

这不是六张表的固定串行查询算法，而是解释本例所需的信息关系。不同编译器可以选择合法的不同编码，运行时也可以提前解析或缓存。

两个常见简化需要修正。**MemberRef 不只用于跨程序集成员**：引用构造泛型类型上的成员等情况也会用到它。**TypeSpec 不只表示泛型实例**：数组、指针等需要类型签名表达的类型形状也属于它的用途。

## 签名怎样表示两个不同的泛型参数

在 IL 文本中，类型参数按位置写作 `!0`，方法参数按位置写作 `!!0`。它们并不根据源代码名字 T、U 进行绑定。

| C# 中的形状 | IL 表示 | 签名中的关键编码 |
|---|---|---|
| int | int32 | I4，0x08 |
| string | string | STRING，0x0E |
| 类型的第一个参数 T | !0 | VAR，0x13，后接位置 0 |
| 方法的第一个参数 U | !!0 | MVAR，0x1E，后接位置 0 |
| ref int | int32& | BYREF，0x10，再接 I4 |
| int[] | int32[] | SZARRAY，0x1D，再接 I4 |

这里的 `VAR` 与 C# 局部变量的 `var` 没有关系；`SZARRAY` 表示从零开始的一维向量，不代表所有数组形状。

本例 Echo 的方法签名可以读成：

```text
实例方法；泛型参数数量 1；显式参数数量 2
返回类型：!!0
参数 0：!0
参数 1：!!0
```

对应方法签名的 payload 是：

```text
30 01 02 1E 00 13 00 1E 00
│  │  │  └──┘ └──┘ └──┘
│  │  │   !!0   !0   !!0
│  │  └─ 两个显式参数
│  └─ 一个方法泛型参数
└─ HASTHIS | GENERIC | DEFAULT
```

隐式 this 由 HASTHIS 表达，不计入这里的两个显式参数。字节串是签名本身，不包含 #Blob heap 用来包住整个 blob 的长度前缀。字段签名、局部变量签名和方法签名各有自己的语法，不能拿同一种头部去解所有 blob。

## 跟着 call 操作数走一次

对上面的调用，IL 工具会显示形如下面的调用目标。这里省略偏移和程序集限定名，只保留关系：

```il
newobj instance void class Shared.Box`1<int32>::.ctor()
ldc.i4.7
ldstr "ok"
call instance !!0 class Shared.Box`1<int32>::Echo<string>(!0, !!0)
ret
```

本例在 .NET SDK 10.0.402 的 Release 编译中使用 call：接收者刚由 newobj 产生，编译器可以采用直接实例调用。其他写法可能生成 callvirt；调用指令的选择不改变本篇要观察的引用关系。最后这次调用的操作数指向 HotEntry 模块的一个 MethodSpec：

```text
HotEntry 模块中的调用操作数
  → MethodSpec
      Method → MemberRef：Echo
                 Parent → TypeSpec：Box<int>
                 Signature → 实例泛型方法，返回 !!0，参数 !0、!!0
      Instantiation → 方法实参 [string]

类型上下文 [int] + 方法上下文 [string]
  → 这次调用的参数为 int、string，返回 string
```

本例 MethodSpec 的实例化签名 payload 是 `0A 01 0E`：GENERICINST 方法实例化标记、一个方法实参、STRING。不要把这里的 0x0A 与 metadata 表号混淆，它们出现在不同编码位置。

这里不是“把 T 的源码替换成 int，再编译一次”。我们是在解释 metadata 描述的类型参数绑定。后面选择共享 native 代码、专门生成的代码，还是解释执行，是另一层问题。

## 为什么同一个 token 能指向不同的方法

表 token 的高 8 位是表号，低 24 位是行号。它只在所属模块中有意义。两个不同 DLL 都可以有 `0x06000001`，且指向完全不同的方法；同一个项目重新编译后，行号也可能变化。

因此排障至少保留“哪个构建产物、哪个模块、哪个 token”，必要时再带泛型上下文。Unity 常见的单模块程序集容易让人把 module 与 assembly 混说，但 token 的作用域仍然是模块。

metadata 表内还使用 **coded index**：例如 MemberRef 的 Parent 可以引用多种表，用低位 tag 区分目标表，再编码行号。coded index 不是原样存下来的 32 位 token；#Blob 中的类型引用还有压缩编码。手工解析时不能统一套用 `token >> 24`。

## 约束在哪里

`where U : class, new()` 等约束不是简单附在 Echo 上面那段参数／返回值签名末尾。

- 泛型参数的归属、位置以及 class／struct／默认构造等特殊约束由 GenericParam 的相关列与标志表达。
- 基类或接口约束通过 GenericParamConstraint 引用相应类型。
- 方法签名负责表示泛型参数数量及类型形状；使用 VAR／MVAR 时引用参数位置。

运行时需要综合这些记录判断实例化是否合法。一个 blob 能被成功解码，不代表所给实参满足约束，也不代表成员可访问。

## 动手验证：看真实记录，不固定 token

1. 建立 Shared 类库与 HotEntry 类库，将上述代码分别放入，给 HotEntry 添加对 Shared 的项目引用。
2. 用同一套 SDK 编译两个项目；打开 HotEntry.dll，在 ILSpy／ildasm 中查看 Entry.Run 的 IL 和调用目标。
3. 用支持 metadata 表视图的工具检查该操作数：它属于哪个模块，是否为 MethodSpec，Method 列指向哪里，MemberRef 的 Parent 是否为 TypeSpec。
4. 对照 Echo 的签名 blob 与 MethodSpec 的实例化 blob。也可用 .NET 的 `PEReader`、`MetadataReader`、`GetMethodSpecification`、`GetMemberReference` 和 `GetBlobBytes` 读取原始数据。
5. 把调用改成 `Box<long>.Echo<string>`，再改成 `Box<int>.Echo<int>`，观察类型实参与方法实参分别改变哪一处记录。

本机验证：2026-10-23，.NET SDK 10.0.402，Release，两个类库。Echo 的签名 payload 为 `3001021E0013001E00`，MethodSpec payload 为 `0A010E`，对应 MemberRef 的 Parent 为 TypeSpec。行号随编译产物变化。此实验检查编译产物的描述关系，不证明对应调用已经通过 IL2CPP 或 HybridCLR 真机运行。

## 放到 IL2CPP 与 HybridCLR 中看

IL2CPP 在构建时消费托管程序集，再生成自己的 metadata 和 native 产物。运行时产物中的索引不能直接当作原 DLL 的表 token 使用。HybridCLR 处理热更 DLL 时仍需理解这些 CLI 引用和签名，并把解析结果接到现有类型和方法体系上。

可以按三步定位泛型调用问题：

| 阶段 | 应确认的事实 |
|---|---|
| 描述与解析 | 引用的程序集、声明类型、成员签名和两组泛型实参是否正确 |
| 语义成立 | 类型约束、访问权限、参数形状是否允许这次调用 |
| 执行可用 | 相应 native 实现或解释执行所需信息是否存在，跨边界调用是否受支持 |

HybridCLR 官方 [AOT 泛型文档](https://www.hybridclr.cn/docs/basic/aotgeneric)说明了类型元数据可以构造出来、相应方法实现却缺失的情形，以及补充原始泛型方法体信息的作用。因此“解析到了 MethodInfo”不能直接推出“所有实例化都能执行”。具体处理继续看 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。

**一次泛型调用的身份由模块、声明类型、方法签名和泛型上下文共同确定；它的执行能力还要另行确认。**

## 系列位置

- 上一篇：[类型系统]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- 下一篇：[CIL 与最小验证规则]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
