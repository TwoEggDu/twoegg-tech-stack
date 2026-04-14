---
title: "横切对比｜类型系统实现：MethodTable vs Il2CppClass vs RtClass"
date: "2026-04-14"
description: "同一个 ECMA-335 TypeDef，在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里各自长什么样。从字段数、内存占用、vtable 布局、泛型膨胀到接口分派，逐维度横切对比五种类型系统实现的设计 trade-off。"
weight: 81
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "LeanCLR"
  - "TypeSystem"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> 五个 runtime 读同一份 DLL，解析同一条 TypeDef——但最终在内存里构建出来的类型描述符，字段数从十几个到上百个不等，内存占用相差数十倍，vtable 有内联的也有外置的。这些差异不是随意的，每一项都反映了各自的核心设计约束。

这是 .NET Runtime 生态全景系列的横切对比篇第 2 篇。

CROSS-G1 对比的是 metadata 解析——五个 runtime 怎么从 PE 文件里读出 TypeDef、MethodDef、FieldDef 这些原始记录。但 metadata 只是静态的描述信息。一个类型要在运行时被实例化、被调用、被 GC 扫描，runtime 必须把这些 metadata 记录转化为一套运行时数据结构——这就是类型系统实现。

同一条 ECMA-335 TypeDef 记录（Partition II 22.37），在五个 runtime 里会被转化为五种完全不同的运行时结构。这篇就是要把这五种结构摊开来看。

## 同一个 TypeDef，五个运行时表示

考虑一个简单的 C# 类型：

```csharp
class Player : MonoBehaviour, ISerializable
{
    public int hp;
    public string name;
    public virtual void TakeDamage(int amount) { ... }
}
```

这个 `Player` 类型编译后在 DLL 的 metadata 中是一条 TypeDef 记录：类名、命名空间、基类 token、字段列表起始行、方法列表起始行、实现的接口列表。所有 runtime 看到的都是同一条记录。

但 runtime 需要回答的问题远多于 metadata 提供的信息：

- 这个类型的实例在堆上占多少字节
- 字段 `hp` 的偏移是多少、`name` 的偏移是多少
- 虚方法 `TakeDamage` 在 vtable 的第几个槽位
- 接口 `ISerializable` 的方法怎么分派到具体实现
- 如果有泛型参数，共享还是特化

每个 runtime 用自己的数据结构来存储这些答案。下面逐个拆解。

## CoreCLR：MethodTable + EEClass

CoreCLR 的类型系统实现是所有 .NET runtime 中最复杂的，也是性能优化最激进的。它的核心设计是 **hot/cold 分离**——把频繁访问的数据和低频访问的数据拆到两个结构里。

### MethodTable：热数据

`MethodTable` 是 CoreCLR 中使用频率最高的类型描述符。每个托管对象的对象头里存的类型指针就是 `MethodTable*`。

```
MethodTable 核心字段（简化）：
┌────────────────────────────────────────┐
│ m_dwFlags           (4 bytes)          │  ← 类型标记：值/引用、数组、泛型等
│ m_BaseSize          (4 bytes)          │  ← 实例基础大小（含对象头）
│ m_pEEClass          (8 bytes)          │  ← 指向 EEClass 冷数据
│ m_pParentMethodTable(8 bytes)          │  ← 父类 MethodTable
│ m_wNumInterfaces    (2 bytes)          │  ← 实现的接口数量
│ m_wNumVirtuals      (2 bytes)          │  ← 虚方法数量
│ ... 更多标记位和缓存 ...               │
├────────────────────────────────────────┤
│ VTable slots (inline)                  │  ← 虚方法表，直接内联在结构体末尾
│  slot[0]: methodPtr                    │
│  slot[1]: methodPtr                    │
│  ...                                   │
├────────────────────────────────────────┤
│ Interface map                          │  ← 接口映射表
└────────────────────────────────────────┘
```

关键设计：

**vtable 内联。** 虚方法槽位直接排列在 MethodTable 结构体的尾部，不需要额外的指针解引用。虚方法调用时，runtime 从对象头读出 MethodTable 指针，加上固定偏移就能拿到目标方法的 native code 地址。一次指针解引用 + 一次偏移计算，对缓存非常友好。

**GC 信息可达。** `m_BaseSize` 直接告诉 GC 这个对象需要扫描多大的范围。引用字段的位置通过 GC descriptor 编码，也存储在 MethodTable 附近。GC 在标记阶段不需要访问 EEClass。

**接口映射表。** 接口分派通过 interface map 完成。map 里存的是每个接口对应的 vtable 起始槽位偏移。调用接口方法时，先从 interface map 找到对应接口的槽位起始，再加上方法在接口中的序号。

### EEClass：冷数据

`EEClass` 存储所有不在热路径上需要的信息：

```
EEClass 核心字段（简化）：
┌────────────────────────────────────────┐
│ m_pFieldDescList    (8 bytes)          │  ← 字段描述符数组
│ m_pMethodDescList   (8 bytes)          │  ← 方法描述符数组
│ m_wNumInstanceFields(2 bytes)          │  ← 实例字段数
│ m_wNumStaticFields  (2 bytes)          │  ← 静态字段数
│ m_dwAttrClass       (4 bytes)          │  ← 类型属性
│ ... 反射信息、泛型参数、嵌套类型 ...    │
└────────────────────────────────────────┘
```

这些数据只在反射、调试、类型加载时才需要。把它们隔离到 EEClass 意味着 MethodTable 可以保持紧凑，GC 扫描和虚方法调用的热路径上不会被冷数据污染缓存行。

### 设计代价

hot/cold 分离的代价是双重间接。当需要访问字段列表时，要先从 MethodTable 读 `m_pEEClass` 指针，再从 EEClass 读 `m_pFieldDescList`。对于反射密集的场景，这个间接层是额外成本。CoreCLR 认为这个 trade-off 是值得的，因为反射不在稳态热路径上。

一个 CoreCLR MethodTable 的典型大小在 200-500 字节之间（取决于虚方法数量和接口数量），EEClass 在 100-300 字节之间。两者合计，一个类型的运行时表示通常占 300-800 字节。

## Mono：MonoClass

Mono 的类型描述符是一个单一的大结构 `MonoClass`。没有 hot/cold 分离。

```
MonoClass 核心字段（简化）：
┌────────────────────────────────────────┐
│ name              (char*)              │  ← 类型名
│ name_space        (char*)              │  ← 命名空间
│ parent            (MonoClass*)         │  ← 父类
│ nested_in         (MonoClass*)         │  ← 外层类型
│ image             (MonoImage*)         │  ← 所属程序集
│ type_token        (uint32_t)           │  ← metadata token
│ instance_size     (int)                │  ← 实例大小
│ vtable_size       (int)                │  ← vtable 大小
│ interface_count   (uint16_t)           │  ← 接口数量
│ flags             (uint32_t)           │  ← TypeAttributes
│ fields            (MonoClassField*)    │  ← 字段数组
│ methods           (MonoMethod**)       │  ← 方法数组
│ interfaces        (MonoClass**)        │  ← 接口列表
│ vtable            (MonoMethod**)       │  ← 虚方法表（外置指针）
│ gc_descr          (void*)              │  ← GC 描述符
│ sizes             (union)              │  ← element_size / class_size
│ ... 各种位域和状态标记 ...              │
└────────────────────────────────────────┘
```

### 单结构设计的特点

**所有信息一站式。** 无论是热路径上需要的 vtable 大小和实例大小，还是反射需要的字段列表和方法列表，都在同一个结构里。访问任何信息都只需要一次指针解引用。

**vtable 外置。** 和 CoreCLR 不同，Mono 的 vtable 不内联在 MonoClass 结构体中，而是一个独立分配的数组，通过 `vtable` 指针访问。虚方法调用多了一次间接：先从对象读 MonoClass 指针，再从 MonoClass 读 vtable 指针，再从 vtable 读方法指针。

**惰性初始化。** MonoClass 的很多字段在首次使用时才填充。`fields`、`methods`、`interfaces` 这些数组只有在被访问时才从 metadata 中解析和分配。这减少了类型加载的前置成本，但运行时需要检查初始化状态。

### 与 CoreCLR 的差异根源

Mono 最初的设计目标是可嵌入性和跨平台兼容性，不是极致的稳态性能。单结构设计让代码更简单、更容易维护，缺点是在 GC 扫描和虚方法调用的热路径上，MonoClass 占据更多缓存行，其中大部分字段对热路径没有用。

一个 MonoClass 实例的典型大小在 200-400 字节之间。看起来和 CoreCLR 的 MethodTable 差不多，但 CoreCLR 在热路径上只碰 MethodTable 那 200 字节，不会把 EEClass 的冷数据拉进缓存。

## IL2CPP：Il2CppClass

IL2CPP 的类型描述符 `Il2CppClass` 有一个独特的属性：它的大部分内容是**构建时生成**的。

```
Il2CppClass 核心字段（简化）：
┌────────────────────────────────────────┐
│ image             (Il2CppImage*)       │  ← 所属程序集映像
│ name              (const char*)        │  ← 类型名
│ namespaze         (const char*)        │  ← 命名空间（拼写来自源码）
│ parent            (Il2CppClass*)       │  ← 父类
│ fields            (FieldInfo*)         │  ← 字段数组
│ methods           (MethodInfo**)       │  ← 方法数组
│ nestedTypes       (Il2CppClass**)      │  ← 嵌套类型
│ implementedInterfaces(Il2CppClass**)   │  ← 接口列表
│ vtable           (VirtualInvokeData*) │  ← vtable（方法指针+类型对）
│ static_fields    (void*)              │  ← 静态字段存储区
│ rgctx_data       (void**)             │  ← 运行时泛型上下文
│ instance_size    (uint32_t)           │  ← 实例大小
│ actualSize       (uint32_t)           │  ← 对齐后实际大小
│ native_size      (int32_t)            │  ← P/Invoke 大小
│ token            (uint32_t)           │  ← metadata token
│ method_count     (uint16_t)           │  ← 方法数
│ field_count      (uint16_t)           │  ← 字段数
│ interfaces_count (uint16_t)           │  ← 接口数
│ vtable_count     (uint16_t)           │  ← vtable 槽位数
│ ... 位域标记 ...                       │
│ typeHierarchyDepth(uint8_t)           │  ← 继承深度
│ typeHierarchy    (Il2CppClass**)      │  ← 继承链数组
│ gc_desc          (uint32_t)           │  ← GC 描述
│ cctor_started    (int32_t)            │  ← 静态构造函数状态
│ cctor_finished   (uint32_t)           │  ← 静态构造完成标记
│ initialized      (uint8_t)           │  ← 初始化状态
│ size_inited      (uint8_t)           │  ← 大小初始化状态
│ ... 更多运行时状态 ...                 │
└────────────────────────────────────────┘
```

### 构建时生成的含义

il2cpp.exe 在构建期间遍历所有 metadata，为每个 TypeDef 生成对应的 C++ 初始化代码。生成的代码类似：

```cpp
// 这是构建工具生成的，不是手写的
Il2CppClass Player_TypeInfo = {
    .image = &g_Assembly_CSharp_Image,
    .name = "Player",
    .namespaze = "",
    .parent = &MonoBehaviour_TypeInfo,
    .instance_size = sizeof(Player_o),
    .vtable_count = 7,
    // ...
};
```

这意味着 IL2CPP 不需要在运行时解析 metadata 来构建类型描述符。大部分字段在 native 代码加载时就已经是确定值。但有些字段（如 `static_fields` 的内存分配、`initialized` 状态）仍然需要运行时填充。

### vtable 结构

IL2CPP 的 vtable 使用 `VirtualInvokeData` 结构：

```cpp
struct VirtualInvokeData {
    Il2CppMethodPointer methodPtr;  // 方法的 native 函数指针
    const MethodInfo* method;        // 方法描述信息
};
```

每个 vtable 槽位不仅存了函数指针，还存了方法描述。这比 CoreCLR 纯存函数指针的 vtable 多了一倍空间，但在调试和反射时能直接拿到方法信息。

### 接口分派

IL2CPP 使用 interface offsets 数组来做接口分派。对于每个类型实现的接口，记录该接口的方法在 vtable 中的起始偏移。分派时先通过接口 ID 查到偏移，再加上方法在接口中的序号。

一个典型的 Il2CppClass 在 200-600 字节之间，加上 vtable 和各种外置数组，总内存占用可以较大。但由于构建时已经确定大部分值，运行时不需要从 metadata 反复解析。

## HybridCLR：复用 Il2CppClass + InterpreterImage 扩展

HybridCLR 的类型系统实现有一个前提：它运行在 IL2CPP 内部。

这意味着它不能、也不需要重新发明类型描述符。IL2CPP 的 `Il2CppClass` 就是它的类型描述符。问题在于：热更新加载的新程序集里的类型，怎么变成 `Il2CppClass`。

### 注册路径

HybridCLR 通过 `InterpreterImage` 把热更 DLL 的 metadata 注入到 IL2CPP 的类型系统中：

1. 热更 DLL 加载后，HybridCLR 创建 `InterpreterImage` 来管理这份 metadata
2. 每个 TypeDef 被转化为 `Il2CppTypeDefinition`，注册到全局 `MetadataCache`
3. 当 IL2CPP 需要构建某个热更类型的 `Il2CppClass` 时，走正常的 class loading 路径
4. 区别在于：AOT 类型的方法指向 native code，热更类型的方法指向解释器入口（`Interpreter::Execute`）

从 IL2CPP 类型系统的视角看，一个热更类型和一个 AOT 类型的 `Il2CppClass` 结构完全相同。差异只在方法指针的目标——一个指向预编译的 native 函数，另一个指向解释器。

### 扩展部分

HybridCLR 在 `Il2CppClass` 之外维护了一些额外信息：

- `InterpreterImage` 持有热更 DLL 的完整 metadata，用于解释器在 transform 阶段查询方法体、字段签名等
- 每个被 transform 过的方法有独立的 `InterpMethodInfo`，存储 transform 后的 HiOpcode 指令序列
- 泛型实例化信息需要在运行时动态构建（AOT 类型的泛型实例化在构建期已完成）

这种设计的优势是：热更类型可以无缝参与 IL2CPP 原有的 vtable 分派、接口分派、类型检查等机制，不需要在每个类型操作的地方加分支判断。

## LeanCLR：RtClass

LeanCLR 的类型描述符 `RtClass` 是五个 runtime 中最精简的。

```
RtClass 核心字段（简化）：
┌────────────────────────────────────────┐
│ parent            (RtClass*)           │  ← 父类
│ name              (const char*)        │  ← 类型名
│ name_space        (const char*)        │  ← 命名空间
│ module            (RtModuleDef*)       │  ← 所属模块
│ fields            (RtFieldInfo*)       │  ← 字段数组
│ methods           (RtMethodInfo*)      │  ← 方法数组
│ vtable            (RtMethodInfo**)     │  ← 虚方法表
│ interfaces        (RtClass**)          │  ← 接口列表
│ instance_size_without_header (uint32_t)│  ← 实例字段大小（不含对象头）
│ vtable_count      (uint16_t)           │  ← vtable 槽位数
│ field_count       (uint16_t)           │  ← 字段数
│ method_count      (uint16_t)           │  ← 方法数
│ interface_count   (uint16_t)           │  ← 接口数
│ flags             (uint32_t)           │  ← TypeAttributes
│ token             (uint32_t)           │  ← metadata token
└────────────────────────────────────────┘
```

### 极简设计的原因

LeanCLR 的目标是 600KB 总体积、资源受限平台。每一个字段的存在都需要有理由。

**没有 GC 描述符。** 当前 GC 是 stub 实现（Universal 版），内存管理委托给宿主。不需要在类型描述符里编码引用字段位置。

**没有 static_fields 指针。** 静态字段的存储由独立的分配器管理，不挂在 RtClass 上。

**没有 rgctx_data。** LeanCLR 当前不支持 CoreCLR 风格的运行时泛型上下文缓存。泛型每次都走完整的 inflate 路径。

**instance_size_without_header。** 存的是不含对象头的净实例字段大小。分配对象时加上对象头大小即可。这个命名比 CoreCLR 的 `m_BaseSize`（含对象头）和 IL2CPP 的 `instance_size`（含对象头）更直接地反映含义。

### vtable

LeanCLR 的 vtable 是一个外置的 `RtMethodInfo*` 数组。每个槽位存的是指向方法描述符的指针，而不是 native code 地址——因为 LeanCLR 是纯解释器，不存在 native 方法指针。调用虚方法时，从 vtable 拿到 `RtMethodInfo`，再通过 `RtMethodInfo` 找到 transform 后的 LL-IL 指令序列。

一个典型的 RtClass 实例在 80-150 字节之间。这是五个 runtime 中最小的。

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 核心结构 | MethodTable + EEClass | MonoClass | Il2CppClass | Il2CppClass（复用） | RtClass |
| 字段数（核心） | ~30 + ~20 | ~40 | ~50 | ~50（同 IL2CPP） | ~15 |
| 典型内存占用 | 300-800B | 200-400B | 200-600B | 200-600B | 80-150B |
| vtable 布局 | 内联在结构体尾部 | 外置指针数组 | 外置 VirtualInvokeData 数组 | 同 IL2CPP | 外置 RtMethodInfo* 数组 |
| vtable 槽位内容 | native code 地址 | MonoMethod 指针 | 方法指针 + MethodInfo 对 | 同 IL2CPP（热更方法指向解释器） | RtMethodInfo 指针 |
| 泛型膨胀策略 | 引用类型共享 + 值类型特化 | 全类型共享（JIT）/ 特化（AOT） | 构建时全量特化 | AOT 类型同 IL2CPP，热更类型运行时 inflate | 运行时逐次 inflate |
| 接口分派 | interface map 查偏移 | interface offsets | interface offsets | 同 IL2CPP | vtable 线性搜索 / 接口槽位 |
| hot/cold 分离 | 有（MethodTable / EEClass） | 无 | 无 | 无 | 无 |
| 构建时 vs 运行时 | 运行时构建 | 运行时构建 | 主要构建时生成 | AOT 构建时 + 热更运行时 | 运行时构建 |
| GC 信息位置 | MethodTable 附近 | MonoClass 内 gc_descr | Il2CppClass 内 gc_desc | 同 IL2CPP | 无（GC stub） |

## 设计 trade-off 分析

五种实现背后是三种截然不同的设计哲学，每一种都有清晰的约束来源。

### hot/cold 分离（CoreCLR）vs 单结构（Mono / LeanCLR）

CoreCLR 选择把类型数据拆成 MethodTable 和 EEClass，根本原因是：服务端 .NET 应用的稳态性能至关重要。一个 ASP.NET 服务每秒处理上千次请求，每次请求涉及大量虚方法调用和 GC 扫描。如果这些热路径上每次都要碰到一个包含反射信息和字段列表的大结构，缓存利用率会显著下降。

Mono 和 LeanCLR 没有做 hot/cold 分离。Mono 最初面向的是嵌入式 CLR 和跨平台兼容，代码简单性比极致缓存优化更重要。LeanCLR 面向的是 600KB 体积约束，RtClass 本身已经只有 80-150 字节，再拆分反而增加复杂度而收益不大。

结论：hot/cold 分离在数据量大、访问模式固定的场景下有显著收益；在整个类型描述符已经很小的场景下，拆分带来的代码复杂度和间接访问成本不值得。

### 构建时生成（IL2CPP）vs 运行时构建（CoreCLR / Mono / LeanCLR）

IL2CPP 在构建期就确定了大部分 Il2CppClass 的内容。这是 AOT 策略的自然延伸——既然所有代码都在构建时转成了 native code，类型信息也没有理由推迟到运行时才构建。

好处是运行时没有类型加载开销——应用启动后所有类型"已经存在"。代价是没有运行时动态加载新类型的能力，也无法在运行时修改类型结构。这就是 HybridCLR 存在的意义：它在构建时生成的类型系统上叠加了运行时注册能力。

CoreCLR 和 Mono 在运行时按需构建类型描述符。首次使用某个类型时触发 class loading，从 metadata 解析字段布局、计算 instance_size、分配 vtable。这个过程有首次成本，但提供了完整的动态能力。

LeanCLR 也是运行时构建，但因为是纯解释器，不存在 JIT 编译环节。RtClass 构建完成后，方法不需要编译成 native code，只需要在首次调用时做 HL-IL / LL-IL transform。

### vtable 内联（CoreCLR）vs vtable 外置（其他）

CoreCLR 把 vtable 直接排列在 MethodTable 结构体的尾部。这意味着从对象拿到 MethodTable 指针后，vtable 和 MethodTable 在同一块连续内存里，很可能在同一个或相邻的缓存行。虚方法调用的整个路径只需要一次指针解引用。

其他四个 runtime 的 vtable 都是外置的——MonoClass 有个 vtable 指针指向独立分配的数组，Il2CppClass 也是如此，RtClass 同理。这意味着虚方法调用需要两次指针解引用：先拿类型描述符，再拿 vtable。

CoreCLR 能这样做的前提是：MethodTable 的大小在类型加载时就完全确定（虚方法数量固定），可以一次性分配 MethodTable + vtable 的连续内存。这种设计在 MethodTable 不会频繁重分配的前提下是最优的。

对于 LeanCLR，vtable 外置还有另一个原因：vtable 槽位存的是 RtMethodInfo 指针而不是 native code 地址。解释器调用虚方法时，拿到 RtMethodInfo 后还要经过一步查找才能拿到可执行的指令序列，vtable 是否内联对整体延迟的影响相对较小。

### 泛型膨胀策略的分化

五个 runtime 在泛型处理上的差异是最大的：

- **CoreCLR** 对引用类型共享一份代码（因为所有引用类型大小相同——都是一个指针），对值类型为每种类型参数特化一份
- **IL2CPP** 在构建时为所有用到的封闭泛型类型生成独立的 C++ 代码。没有用到的组合不会生成，这就是 AOT 泛型缺失问题的根源
- **HybridCLR** 对 AOT 类型继承 IL2CPP 的策略；对热更类型，支持全泛型共享（FGS），运行时通过 rgctx 传递类型信息
- **LeanCLR** 每次泛型调用都走完整的 inflate 路径，不做共享缓存。这在性能上不是最优的，但实现最简单，且对于解释器执行来说，inflate 开销相对于解释执行本身不是瓶颈

## 收束

同一条 ECMA-335 TypeDef 记录，五个 runtime 构建出来的运行时结构在字段数、内存占用、vtable 设计上差异巨大。但每一个差异都不是随意的设计选择，而是各自核心约束的直接体现：

- CoreCLR 追求稳态性能，所以做 hot/cold 分离和 vtable 内联
- Mono 追求简单性和可嵌入性，所以用单一大结构
- IL2CPP 是全量 AOT，所以把类型构建推到编译期
- HybridCLR 在 IL2CPP 上补热更能力，所以复用 Il2CppClass 并扩展注册机制
- LeanCLR 追求最小体积，所以用最精简的 RtClass

理解了这些约束，在看到某个 runtime 的特定设计时，就不会问"为什么不像 CoreCLR 那样做 hot/cold 分离"这种脱离上下文的问题。

## 系列位置

这是横切对比篇第 2 篇（CROSS-G2）。

上一篇 CROSS-G1 对比了 metadata 解析——五个 runtime 怎么从 PE 文件里读出 TypeDef、MethodDef 等记录。这篇从 TypeDef 往前走了一步：runtime 怎么把这些记录变成可操作的运行时类型描述符。

下一篇 CROSS-G3 继续往前：有了类型系统之后，方法怎么执行——JIT、AOT、解释器、混合执行四种路线的横切对比。
