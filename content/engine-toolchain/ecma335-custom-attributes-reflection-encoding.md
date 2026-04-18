---
title: "ECMA-335 基础｜Custom Attributes 与 Reflection 元数据编码"
slug: "ecma335-custom-attributes-reflection-encoding"
date: "2026-04-15"
description: "从 ECMA-335 规范出发，拆解 Custom Attribute 在 metadata 中的存储位置、blob 的二进制编码格式、Reflection API 读取 attribute 的运行时路径，以及 CoreCLR/Mono/IL2CPP/HybridCLR 四种 runtime 的处理差异。ECMA-335 基础层第 10 篇，全系列收束篇。"
weight: 17
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "CustomAttribute"
  - "Reflection"
  - "Metadata"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> `[Serializable]`、`[Obsolete]`、`[DllImport]` 这些 attribute 不是 C# 语法糖——它们在 metadata 里有精确的二进制编码，运行时反射能读到的所有信息都来自这套编码。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 10 篇（系列收束篇）。

前 9 篇覆盖了术语约定、metadata 物理结构、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享、CLI Verification——这些层次回答了"代码怎么描述、怎么跑、怎么验证"的问题。但 ECMA-335 还有最后一块拼图：metadata 体系的可扩展机制。Custom Attribute 是这套机制的载体。所有不属于核心语法但又要附加到类型/方法/字段上的语义信息（序列化标记、过时警告、P/Invoke 配置、AOT hint、依赖注入标识），都通过这一套编码挂在 metadata 上。

> **本文明确不展开的内容：**
> - Reflection API 的完整使用方法（这是 BCL 用法，不在规范层）
> - Source Generator（编译时元编程，不属于运行时反射）
> - 各 runtime 的 attribute 性能优化细节（属于实现层，在 B/C/D/F 模块各 runtime 章节展开）

## Custom Attribute 在 metadata 中的存储位置

ECMA-335 Partition II §22.10 定义了 `CustomAttribute` 表的物理结构。这张表在 metadata stream 中占据一个固定的 table id（0x0C），是 metadata 体系中数量最多的表之一——一个中等规模的 BCL assembly 里 `CustomAttribute` 表可能有几千行。

每行有 3 列：

| 列 | 类型 | 含义 |
|----|------|------|
| Parent | HasCustomAttribute coded index | 指向被附加 attribute 的 metadata 元素 |
| Type | CustomAttributeType coded index | 指向 attribute 类型的构造函数（MethodDef 或 MemberRef） |
| Value | #Blob heap offset | 指向 attribute 实例的二进制编码数据 |

**Parent 列的 coded index 设计是关键。** ECMA-335 Partition II §24.2.6 列出了 HasCustomAttribute 编码下能指向的 22 种目标表——TypeDef、MethodDef、Field、Param、Property、Event、Module、Assembly 等几乎所有可见的 metadata 元素都能携带 attribute。这意味着 attribute 不是只能挂在类型和方法上，连 assembly 本身、单个方法参数、甚至泛型参数都能挂。

**一个目标可以携带 N 个 attribute。** 如果 `Unit` 类同时带了 `[Serializable]` 和 `[Obsolete("Use Player")]`，`CustomAttribute` 表里就有两行 Parent 指向 `Unit` 的 TypeDef token。表本身没有"分组"概念——attribute 之间的关系完全靠 Parent 列的相同值来体现。

**Type 列指向的是构造函数，不是类型本身。** 这是一个常被误解的点。`CustomAttribute` 表的 Type 列指向 `MethodDef` 或 `MemberRef`，对应的是 attribute 类型的某个构造函数（`.ctor`）。runtime 通过这个构造函数 token 反查到声明类型，再决定怎么实例化 attribute 对象。一个 attribute 类型如果有多个构造函数重载，metadata 会精确记录用的是哪一个。

**Value 列指向 #Blob heap 的偏移量。** Blob heap 是 metadata 中专门存放二进制数据的区域（与 #Strings、#US、#GUID 等 heap 并列）。Custom attribute 的实例数据——构造函数参数值、命名参数值——都按一套固定格式编码后塞进 #Blob heap，Value 列只存 4 字节偏移量。

## Attribute 实例的二进制编码

ECMA-335 Partition II §23.3 "Custom attributes blob" 定义了 #Blob 中 attribute 数据的完整格式：

```
CustomAttribute = Prolog FixedArg* NumNamed NamedArg*
```

四个组成部分：

**Prolog** — 固定 2 字节，值为 `0x0001`。这是 attribute blob 的魔数，runtime 解析时先校验这两个字节。如果不是 `0x0001`，说明 blob 损坏或不是 custom attribute 数据。

**FixedArg\*** — 按 attribute 构造函数声明的参数顺序排列的固定参数值。每个参数的编码方式由参数类型决定（下一节展开）。FixedArg 的数量不在 blob 里显式存储——runtime 通过解析 Type 列指向的构造函数签名，得知有几个参数、每个是什么类型，按顺序往下读。

**NumNamed** — 2 字节，命名参数的数量。即使没有命名参数，这 2 字节也必须存在（值为 `0x0000`）。

**NamedArg\*** — 命名参数序列。每个 NamedArg 由 4 部分组成：

```
NamedArg = FieldOrPropTag (1 byte) + FieldOrPropType + Name (SerString) + Value
```

`FieldOrPropTag` 取值 `0x53` 表示字段、`0x54` 表示属性。`FieldOrPropType` 是参数类型的编码（与 FixedArg 的类型编码同源）。Name 是命名参数的名字（用 SerString 格式）。Value 是参数值（编码方式与 FixedArg 一致）。

用 `Unit` 参考类的两个 attribute 对比 blob 的差异：

```csharp
[Serializable]                   // 无构造参数，blob = prolog + NumNamed=0
[Obsolete("Use Player")]         // 1 个 string 参数，blob 包含字符串编码
public class Unit : IHittable { ... }
```

`[Serializable]` 的 blob 是最小可能形态——只有 4 字节：

```
01 00          ← Prolog (0x0001)
00 00          ← NumNamed = 0
```

`[Obsolete("Use Player")]` 的 blob 多出一个字符串参数。`Obsolete(string)` 构造函数有 1 个 string 参数，FixedArg 部分要编码 `"Use Player"`：

```
01 00                       ← Prolog
0A                          ← SerString length prefix (PackedLen, "Use Player" 长度 10)
55 73 65 20 50 6C 61 79 65 72   ← UTF-8 编码 "Use Player"
00 00                       ← NumNamed = 0
```

如果是 `[Obsolete("Use Player", true)]`（指定 IsError=true），FixedArg 部分会再追加 1 字节的 bool 值（`0x01`）。

**Blob 的紧凑性是 metadata 体积的关键。** 一个 BCL assembly 可能有上万个 attribute 实例，blob 编码在每个字节上都做了压缩——SerString 的长度前缀用 PackedLen（1~4 字节变长编码），bool 用 1 字节，原生类型用最小宽度的 little-endian 编码。这是 metadata 设计取舍的典型例子：解析速度让位于体积。

## FixedArg 的类型编码规则

ECMA-335 Partition II §23.3 规定了 FixedArg 内每种参数类型的编码方式。理解这套规则才能手工解析或生成 blob。

**Primitive types** — 直接二进制，按类型对应的字节宽度，little-endian 编码。`int32` 占 4 字节，`int64` 占 8 字节，`float32` 占 4 字节，`bool` 占 1 字节。`char` 占 2 字节（UTF-16 code unit）。

**String** — 用 SerString 格式：1~4 字节的 PackedLen 长度前缀 + UTF-8 编码的字节序列。空字符串编码为单字节 `0x00`，null 字符串编码为单字节 `0xFF`（这是 PackedLen 的特殊值，runtime 解析时要区分"长度 0"和"null"）。

**Type** — 用 SerString 编码 type 的 assembly-qualified name。例如 `typeof(Unit)` 在 attribute 里会编码为 `"GameLib.Unit, GameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"` 这种字符串。runtime 在反序列化 attribute 时会调用 `Type.GetType(string)` 把这个字符串解析回 `Type` 对象——这也是为什么 attribute 里持有的 `Type` 在程序集重命名后会失效。

**Array** — 4 字节的元素数量前缀 + 元素序列。如果数量是 `0xFFFFFFFF`，表示数组本身是 null（区别于"空数组"，空数组的数量是 `0x00000000`）。元素按数组的元素类型编码。

**Enum** — 编码为底层 primitive type。`enum E : int { A, B }` 类型的参数 `E.B`，blob 里就是 `01 00 00 00`（int32 little-endian），runtime 在反序列化时通过签名得知这是 enum，再做一次类型转换。

**Boxed object（Object 类型参数）** — 比较特殊。如果 attribute 构造函数声明了 `object` 类型的参数，blob 里要先编码 1 字节的类型标记（FieldOrPropType），再编码值本身。这给运行时一个动态类型信息：当签名是 `object` 时，没办法静态确定参数的真实类型，必须在 blob 里显式标注。

举一个综合例子。假设有 attribute：

```csharp
public sealed class TestCaseAttribute : Attribute
{
    public TestCaseAttribute(int id, string name, Type[] inputs) { ... }
}

[TestCase(42, "ShouldHit", new[] { typeof(Unit), typeof(Player) })]
public void HitTest() { ... }
```

它的 blob 大致编码为：

```
01 00                                   ← Prolog
2A 00 00 00                             ← FixedArg[0] int32 = 42
09 53 68 6F 75 6C 64 48 69 74           ← FixedArg[1] SerString "ShouldHit" (len=9)
02 00 00 00                             ← FixedArg[2] Array length = 2
[SerString "GameLib.Unit, GameLib..."]   ← Array[0] Type
[SerString "GameLib.Player, GameLib..."] ← Array[1] Type
00 00                                   ← NumNamed = 0
```

这个例子展示了 blob 的递归性——Array 的元素如果是 String/Type，会嵌入 SerString；如果是另一个 enum，会嵌入 enum 的 underlying type 编码。

## Reflection 读取路径

ECMA-335 Partition II §22.10 定义了 metadata 表，Partition I §11.2 描述了 Reflection 的语义契约。两者结合起来，`typeof(Unit).GetCustomAttributes()` 的运行时执行路径如下：

**步骤 1 — 拿到目标的 metadata token。** `typeof(Unit)` 返回的 `Type` 对象内部持有 `Unit` 类型的 TypeDef token（4 字节，table id `0x02` + row index）。

**步骤 2 — 在 CustomAttribute 表中扫描。** runtime 遍历 CustomAttribute 表，找出所有 Parent 列等于 `Unit` 的 TypeDef token 的行。CoreCLR 和 Mono 在这一步通常做过预排序——CustomAttribute 表按 Parent 排序后，可以用二分查找快速定位匹配区间，避免每次都全表扫描。

**步骤 3 — 对每行解析 Type 列和 Value blob。**
- Type 列指向构造函数 token，runtime 通过这个 token 找到 attribute 的声明类型（比如 `System.SerializableAttribute`）。
- Value 列指向 #Blob 偏移，runtime 按 §23.3 的格式解析 blob：先验 Prolog 的 `0x0001`，再按构造函数签名顺序解 FixedArg，最后读 NumNamed 和 NamedArg。
- 解析得到的所有参数值传给构造函数 → 调用 `Activator.CreateInstance` 类似的逻辑实例化 attribute 对象。
- 如果有命名参数，再对每个 NamedArg 通过反射设置对应的字段或属性。

**步骤 4 — 返回 attribute 数组。** 所有匹配行处理完成后，组装成 `Attribute[]` 返回给调用方。

**关键观察 — 每次 `GetCustomAttributes` 都重新实例化 attribute 对象，不是缓存的单例。** 这是规范层面的语义要求：attribute 是数据快照，调用方可能修改返回对象的状态（虽然几乎没人这么做），所以每次调用都返回独立的实例。也是反射性能问题的核心来源——一个 hot path 上的 `GetCustomAttribute<T>()` 调用，每次都要走 metadata 解析 + 对象分配 + 构造函数调用 + 命名参数赋值的完整链路。

`CustomAttributeData` API 提供了一条不实例化的替代路径——它直接返回 blob 的解析结果（构造函数引用 + 参数值列表），不调用构造函数。这条路径在 AOT 场景下尤其重要，因为 attribute 类型本身可能已经被 stripping 移除，但 blob 还在 metadata 里。

## 各 runtime 的 attribute 处理

四种主流 runtime 在 attribute 处理上的实现策略差异显著。

**CoreCLR** — `CustomAttributeData` 直读 metadata blob，按需实例化。`MethodInfo.GetCustomAttributes` 内部走 `RuntimeMethodInfo.InvokeCustomAttributeCtor`，每次调用都解析 blob + 调用构造函数。CoreCLR 在 metadata 加载时不预解析 attribute，只在反射查询时按需解。运行时维护一个 attribute 类型的缓存（避免重复查找声明类型），但 attribute 实例本身不缓存。

**Mono** — `MonoCustomAttrInfo` 结构缓存解析结果。Mono 在第一次查询某个目标的 attribute 时，会把整套 attribute 信息（类型 + 构造参数 + 命名参数）解析到 `MonoCustomAttrInfo` 并挂在目标的 metadata 缓存上。后续查询同一目标时跳过 blob 解析，直接从缓存重建对象。这个设计对 Unity 这种反射重度使用的场景做了针对性优化。

**IL2CPP** — 构建期 `il2cpp.exe` 把所有 attribute 元数据预生成为 C++ 数组。具体落在 `Il2CppCustomAttributeTypeRange` 数组中，每个 entry 记录 `{token, start, count}`：token 是被附加 attribute 的目标，start 和 count 指向 `Il2CppCustomAttributeDataReader` 的范围。运行时 `il2cpp_custom_attrs_get_attrs` 直接二分查找索引数组，无需解析 blob——blob 在构建期已经被反序列化为 C++ 字面量并编译进二进制。这是 IL2CPP "用编译期换运行时"的典型设计。

**HybridCLR** — 热更 DLL 中的 attribute 走 metadata 解析路径。`InterpreterImage` 在加载热更程序集时把 CustomAttribute 表注册到 `MetadataCache`，反射查询热更类型的 attribute 时，从 `MetadataCache` 取出 blob 偏移，按 ECMA-335 §23.3 解析。这条路径与 IL2CPP 主包预生成的路径完全独立——主包类型的 attribute 走 IL2CPP 的 C++ 数组，热更类型的 attribute 走 HybridCLR 的解释器路径。区别于 AOT 类型已经预处理的快路径，热更 attribute 每次访问都走完整解析流程。

四种 runtime 的核心差异对照：

| Runtime | 解析时机 | 缓存策略 | 实例化策略 |
|---------|---------|---------|-----------|
| CoreCLR | 反射查询时 | 仅缓存类型查找结果 | 每次重新实例化 |
| Mono | 首次查询时 | 缓存完整 `MonoCustomAttrInfo` | 从缓存重建 |
| IL2CPP | 构建时（C++ 数组） | C++ 数组本身就是缓存 | 运行时按索引取出 |
| HybridCLR | 热更 DLL 加载时注册，反射时解析 blob | `MetadataCache` 缓存元数据，不缓存实例 | 每次重新实例化 |

## 工程影响

理解 attribute 的编码和解析机制，会直接影响三类工程决策。

**反射开销与缓存模式。** CoreCLR 和 Mono 每次 `GetCustomAttributes` 都解析 blob + 实例化对象，是运行时热点。生产代码里常见的缓存模式：

```csharp
// 静态缓存所有 MemberInfo 的 attribute 数组
private static readonly ConcurrentDictionary<MemberInfo, Attribute[]> _cache = new();

public static Attribute[] GetAttributesCached(MemberInfo member)
{
    return _cache.GetOrAdd(member, m => m.GetCustomAttributes(true).Cast<Attribute>().ToArray());
}
```

序列化框架（Newtonsoft.Json、System.Text.Json）、IoC 容器（Autofac、MS.DI）、ORM（EF Core）几乎都内置了类似的 attribute 缓存。在性能敏感场景下，避免每次调用 `GetCustomAttribute<T>()` 是基本前提。

**AOT 裁剪对 attribute 的影响。** IL2CPP 和 NativeAOT 都会对未引用的类型做 stripping。如果某个 attribute 类型被裁剪掉（比如 BCL 里的某个内部 attribute 没有被任何反射代码显式引用），运行时反射 `GetCustomAttributes()` 会拿不到这个 attribute——因为没有 attribute 类型可以实例化。但 `CustomAttributeData` API 仍然能读到原始 blob 数据（构造函数 token 仍在 metadata 里，参数值 blob 仍在 #Blob heap 里）。这是 IL2CPP/CoreCLR 在 AOT 场景下的常见 fallback 路径——序列化框架检测到 `GetCustomAttribute<T>()` 返回 null 时，会退回去用 `CustomAttributeData` 读 blob，至少能拿到 attribute 类型名和参数值，恢复部分语义。

**HybridCLR 热更类型的 attribute 可见性。** 热更 DLL 里如果新增了 attribute 类型（AOT 主包从未见过），反射查询时需要确保该 attribute 类型已经被 HybridCLR 加载，否则 attribute 实例化时会抛 `TypeLoadException`。常见踩坑场景：热更代码里给新类标注 `[NewAttribute]`，但 `NewAttribute` 也定义在热更 DLL 里。如果热更 DLL 加载顺序错了（先加载使用方再加载定义方），第一次反射查询会失败。规避方式是热更 DLL 之间显式声明依赖，或者把 attribute 类型放在最先加载的核心热更包里。

## 收束

Custom attribute 是 ECMA-335 metadata 体系最被低估的扩展点。它不是 C# 语法糖，而是一套精确的二进制编码规范——`CustomAttribute` 表的三列结构、blob 的 Prolog + FixedArg + NumNamed + NamedArg 四段、SerString 和 PackedLen 的紧凑编码——共同构成了一套独立于具体语言的元数据扩展协议。

理解这套机制，是理解所有 .NET 序列化框架（Newtonsoft.Json、System.Text.Json、Protobuf-net）、所有 IoC 容器（Autofac、MS.DI）、所有 ORM（EF Core）的运行时行为的前提。这些框架的核心工作模式都是：扫描类型/方法/字段上的 attribute → 根据 attribute 配置决定运行时行为。它们之所以能跨 runtime 工作，是因为底层依赖的就是 ECMA-335 这一层规范。

**收束本系列。** A0~A9 这 10 篇覆盖了 ECMA-335 规范的核心主干：

- A0 术语约定 — 统一全系列的核心边界
- A1 Metadata 物理结构 — TypeDef、MethodDef、Token、Stream
- A2 CIL 指令集与栈机模型 — JIT/AOT/解释器的共同输入
- A3 类型系统 — 值类型、引用类型、泛型、接口
- A4 执行模型 — 调用约定、虚分派、异常处理
- A5 程序集模型 — 身份、版本、加载
- A6 内存模型 — 对象布局、GC 契约、finalization
- A7 泛型共享规则 — 引用类型共享、值类型独立
- A8 CLI Verification — IL 类型安全的运行时校验
- A9 Custom Attributes — metadata 扩展协议

后续 runtime 实现模块（B/C/D/E/F）和横切对比模块（G）都会反复回引这套规范层定义。读 CoreCLR 的 MethodTable 设计、IL2CPP 的 MetadataCache 初始化、LeanCLR 的 metadata 解析、HybridCLR 的热更类型注册——所有这些实现层的工程决策，都是在这同一套 ECMA-335 规范之上做出的不同取舍。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/ecma335-cli-verification-il-type-safety.md" >}}">A8 CLI Verification</a>
- 下一篇：进入 runtime 实现模块。推荐入口：
  - <a href="{{< relref "engine-toolchain/coreclr-architecture-overview-dotnet-run-to-jit.md" >}}">CoreCLR 架构总览</a>
  - <a href="{{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}">IL2CPP 架构总览</a>
  - <a href="{{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}">LeanCLR 调研报告</a>
