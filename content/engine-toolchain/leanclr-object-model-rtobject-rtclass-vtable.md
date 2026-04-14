---
title: "LeanCLR 源码分析｜对象模型：RtObject、RtClass、VTable 与单指针头设计"
date: "2026-04-14"
description: "拆解 LeanCLR 对象模型的完整实现：RtObject 的 16 字节双指针头与 8 字节单指针头裁剪、RtClass 类型描述符的字段布局、VTable 虚分派机制、RtArray/RtString 的内存排列、Boxing/Unboxing 路径，以及反射对象的嵌入式 header 设计。"
weight: 73
featured: false
tags:
  - LeanCLR
  - CLR
  - ObjectModel
  - Memory
  - SourceCode
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> ECMA-335 定义了对象布局的语义——引用类型在堆上分配、值类型内联存储——但不规定具体的内存格式。每个 CLR 实现自行决定对象头长什么样、类型指针放哪里、VTable 怎么组织。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 4 篇。

## 从 ECMA-A6 接续

ECMA-335 基础层第 3 篇（ECMA-A6）讲清了 CLI 类型系统的核心模型：值类型与引用类型的本质区别在于内存语义——值类型内联存储、赋值即复制，引用类型在堆上分配、变量持有的是引用。

但规范只定义了语义，不定义格式。一个引用类型对象在堆上到底是什么样的二进制布局？类型信息存在哪里？VTable 怎么索引虚方法？这些问题的答案在每个 CLR 实现中都不同。

LEAN-F2 拆解了 metadata 解析——从 PE 文件到 CliImage 到 RtModuleDef，构建出 RtClass、RtMethodInfo、RtFieldInfo 等运行时结构。LEAN-F3 拆解了双解释器——从 MSIL 到 HL-IL 到 LL-IL 的三级 transform 管线。

这篇进入对象模型：这些运行时结构在 metadata 解析之后，怎么组织成解释器实际操作的托管对象。

## RtObject：托管对象基础结构

**源码位置：** `src/runtime/vm/rt_managed_types.h`

RtObject 是所有托管对象的基础结构：

```cpp
struct RtObject {
    metadata::RtClass* klass;
    void* __sync_block;
};
```

两个字段，16 字节（x64 环境下）。

`klass` 指向 RtClass 类型描述符。运行时需要知道一个对象"是什么类型"时——类型检查、虚方法分派、GC 扫描——都通过这个指针找到类型信息。

`__sync_block` 是同步块指针，用于 C# 的 `lock` 语句和 `Monitor` 操作。每个对象都有这个字段，即使绝大多数对象一辈子不会被 lock。

一个托管对象在堆上的内存布局是：

```
┌──────────────────────────────────────┐
│ RtObject header (16 bytes)           │
│  ┌──────────┐ ┌──────────────────┐   │
│  │ klass*   │ │ __sync_block*    │   │
│  │ (8 bytes)│ │ (8 bytes)        │   │
│  └──────────┘ └──────────────────┘   │
├──────────────────────────────────────┤
│ Instance fields                      │
│  (size = instance_size_without_header│
│   from RtClass)                      │
└──────────────────────────────────────┘
```

### 单指针头：8 字节版本

LeanCLR Universal 版是单线程的。单线程意味着不存在多线程竞争，`lock` 语句在语义上退化为空操作，`__sync_block` 字段完全没有用途。

这时候可以把 `__sync_block` 省掉，对象头从 16 字节缩减到 8 字节——只剩一个 `klass*`。

8 字节对象头意味着什么？

一个空的引用类型对象（没有任何实例字段），在 CoreCLR 上至少占 24 字节（8 字节 MethodTable 指针 + 4 字节 SyncBlock index + 填充对齐），在 IL2CPP 上至少占 16 字节（`Il2CppClass*` + `monitor*`），在 LeanCLR Universal 版上只占 8 字节。

对于 H5 和微信小游戏这种内存敏感的目标平台，每个对象省 8 字节的效果是显著的。一个场景里几万个小对象，对象头的总开销差距可以达到几百 KB。

### 与 IL2CPP Il2CppObject 的对比

IL2CPP 的对象头定义在 `il2cpp-object-internals.h` 中：

```cpp
// IL2CPP (参考)
struct Il2CppObject {
    Il2CppClass* klass;
    MonitorData* monitor;
};
```

结构上和 LeanCLR 的 RtObject 几乎一样：一个类型指针 + 一个同步块指针，16 字节。

区别在于 LeanCLR 的单线程版可以裁掉 `__sync_block`，而 IL2CPP 做不到——IL2CPP 始终运行在多线程环境中（Unity 的 Job System、async/await 都依赖多线程），`monitor` 字段不能省略。

CoreCLR 的做法又不同。CoreCLR 不在对象头里内联 SyncBlock 指针，而是在对象前面放一个 `ObjHeader`（存 SyncBlock index 或 thin lock），对象指针本身指向 MethodTable 指针的位置。这种负偏移设计让对象指针直接就是类型指针，但代价是地址计算更复杂。

## RtClass：类型描述符

**源码位置：** `src/runtime/metadata/class.h`

RtClass 是 LeanCLR 的类型描述符，描述一个 .NET 类型的完整运行时信息：

```cpp
struct RtClass {
    RtModuleDef* image;
    RtClass* parent;
    const char* namespaze;
    const char* name;
    const RtFieldInfo* fields;
    const RtMethodInfo** methods;
    const RtTypeSig* by_val;
    uint32_t instance_size_without_header;
    uint16_t vtable_count;
    uint16_t field_count, method_count, property_count, event_count;
};
```

逐字段拆解：

**`image`** —— 指向所属 RtModuleDef。一个 RtClass 属于哪个程序集，就通过这个指针回溯。跨 assembly 类型解析时需要用到。

**`parent`** —— 父类的 RtClass 指针。构成继承链。`System.Object` 的 parent 为 null，是继承链的终点。类型检查（`is`/`as`）沿这个链往上遍历。

**`namespaze` + `name`** —— 类型的命名空间和名称，从 metadata 的 `#Strings` stream 中读取。注意字段名是 `namespaze`（拼写不同于 C++ 关键字 `namespace`），这是一个刻意的命名规避。

**`fields`** —— 字段描述符数组，指向 RtFieldInfo 结构体。每个 RtFieldInfo 描述一个实例字段或静态字段的名称、类型和偏移量。

**`methods`** —— 方法描述符指针数组。注意这里是 `const RtMethodInfo**`（指针的指针），不是直接的数组——因为方法可能来自不同的 assembly（接口方法、继承方法）。

**`by_val`** —— 值类型签名。对于值类型，这个字段指向一个 RtTypeSig，描述这个类型作为值内联使用时的签名。引用类型这个字段通常为 null。

**`instance_size_without_header`** —— 实例字段占用的总大小（不包含 RtObject 头部）。分配对象时，实际分配的大小是 `sizeof(RtObject) + instance_size_without_header`。这个值在 RtModuleDef 构建阶段根据字段布局计算出来。

**`vtable_count`** —— VTable 中虚方法槽位的数量。

**`field_count / method_count / property_count / event_count`** —— 各种成员的计数。解释器遍历类型成员时用这些值确定数组边界。

### 与 IL2CPP Il2CppClass 的对比

IL2CPP 的 Il2CppClass 包含的信息量远超 RtClass。Il2CppClass 有 50+ 个字段，涵盖了 static 字段存储、generic 实例化信息、interface offset table、cctor 状态、类型初始化标志等。

LeanCLR 把这些信息分散到了不同的结构中（泛型信息在 RtGenericClass、接口信息在单独的查找逻辑中），RtClass 本身保持精简。这是两种不同的设计取向：IL2CPP 把所有信息集中在一个大结构体里追求访问效率，LeanCLR 拆分成多个小结构体追求概念清晰。

### CorLibTypes：核心类型缓存

**源码位置：** `src/runtime/metadata/class.h`

某些 .NET 类型在运行时被反复使用。每次通过名字查找 `System.Int32` 或 `System.String` 的 RtClass 代价太高，所以 LeanCLR 用 CorLibTypes 结构体做了一层缓存：

```cpp
struct CorLibTypes {
    metadata::RtClass* cls_void;
    metadata::RtClass* cls_boolean;
    metadata::RtClass* cls_int32;
    metadata::RtClass* cls_string;
    metadata::RtClass* cls_object;
    metadata::RtClass* cls_valuetype;
    metadata::RtClass* cls_array;
    metadata::RtClass* cls_delegate;
    metadata::RtClass* cls_nullable;
    metadata::RtClass* cls_exception;
    // ... 70+ cached corlib types
};
```

这个缓存在 corlib 加载时一次性填充，之后所有对基础类型的引用直接走指针访问，不需要哈希查找。70+ 个缓存项覆盖了所有基础值类型、字符串、数组、委托、Nullable、常见异常类型、反射类型和线程类型。

IL2CPP 有完全对等的机制——`il2cpp_defaults` 全局结构体缓存了同样的一组类型指针。CoreCLR 也有类似的 `CoreLibBinder`。这是所有 CLR 实现的共识做法：核心类型使用频率太高，必须做直接指针缓存。

## VTable 设计

**源码位置：** `src/runtime/metadata/class.h`

### VTable 结构

虚方法分派需要一张表，让运行时能够根据对象的实际类型找到正确的方法实现。LeanCLR 的 VTable 由 RtVirtualInvokeData 数组构成：

```cpp
struct RtVirtualInvokeData {
    RtManagedMethodPointer method_pointer;
    const RtMethodInfo* method;
};
```

每个槽位包含两个值：

**`method_pointer`** —— 方法的实际执行入口。对于解释执行的方法，这个指针指向解释器的 dispatch 入口；对于 intrinsic 或 internal call，指向对应的 C++ 函数。

**`method`** —— 指向 RtMethodInfo，保留完整的方法元数据。运行时在某些场景下不仅需要调用方法，还需要知道方法的签名、名称、所属类型等信息（比如反射调用、异常栈追踪）。

RtClass 中的 `vtable_count` 字段记录了 VTable 的大小。VTable 的槽位按方法声明顺序排列：父类的虚方法占据低位槽，子类覆写的方法替换对应槽位，子类新声明的虚方法追加到末尾。

### 虚方法分派流程

当解释器执行 `callvirt` 指令时，分派流程是：

```
1. 从操作数栈获取对象引用 obj
2. 从 obj->klass 获取 RtClass
3. 用方法的 vtable slot index 索引 VTable
4. 取出 RtVirtualInvokeData.method_pointer
5. 跳转执行
```

这是一个 O(1) 的查表操作。slot index 在 LL-IL transform 阶段就已经烘焙到指令的操作数中（LEAN-F3 中 LLTransformer 的类型特化步骤），运行时不需要再做方法名匹配。

### 接口分派

接口方法的分派比普通虚方法多一步。一个类可以实现多个接口，每个接口的方法在类的 VTable 中的位置不固定。运行时需要先查找接口到 VTable 的偏移映射，再用偏移加接口方法的 slot index 定位到实际的 VTable 槽位。

这和 CoreCLR 的接口分派思路一致。CoreCLR 在 MethodTable 中维护了一个 InterfaceMap，记录每个接口的起始 slot 偏移。LeanCLR 的实现更简化，但核心逻辑相同。

### 与 CoreCLR MethodTable 的对比

CoreCLR 把类型描述符和 VTable 合并成了一个叫 MethodTable 的结构。MethodTable 既包含类型元数据（类名、父类、接口列表、字段布局），也包含虚方法槽位数组——VTable 直接内联在 MethodTable 的尾部。

LeanCLR 的 RtClass 和 VTable 在概念上是分离的。RtClass 存储类型元数据，VTable 是一个单独的 RtVirtualInvokeData 数组。这种分离让 RtClass 结构更紧凑，但虚方法分派时多了一次间接寻址。

在解释器场景下，这个额外间接寻址的开销可以忽略——解释器本身的 dispatch loop 开销远大于一次指针跳转。

## RtArray：数组对象

**源码位置：** `src/runtime/vm/rt_managed_types.h`

RtArray 继承自 RtObject，增加了数组特有的字段：

```cpp
struct RtArray : public RtObject {
    const ArrayBounds* bounds;
    int32_t length;
    uint64_t first_data;
};

struct ArrayBounds {
    int32_t length;
    int32_t lower_bound;
};
```

内存布局：

```
┌──────────────────────────────────────┐
│ RtObject header                      │
│  klass* │ __sync_block*              │
├──────────────────────────────────────┤
│ bounds*        (8 bytes)             │
│ length         (4 bytes)             │
│ first_data ←── 元素数据起始位置       │
│ ...            (length * elem_size)  │
└──────────────────────────────────────┘
```

**`bounds`** —— 指向 ArrayBounds 结构。对于一维零基数组（C# 中最常见的 `T[]`），bounds 为 null。只有多维数组或非零基数组才会分配 ArrayBounds，记录每一维的长度和下界。

**`length`** —— 数组元素总数。一维数组就是元素个数，多维数组是所有维度长度的乘积。

**`first_data`** —— 这不是一个真正的 `uint64_t` 字段。它的地址标记了元素数据的起始位置。运行时通过 `&first_data` 获取数据区的首地址，然后按元素大小做偏移访问。这是 C/C++ 中常见的"柔性数组成员"技巧——在结构体末尾放一个占位字段，实际的数组数据紧跟其后。

这个设计和 IL2CPP 的 Il2CppArray 几乎完全一致。IL2CPP 也是在 Il2CppArraySize 结构体末尾用一个 `alignas(8) char values[1]` 占位字段标记数据起始位置。

## RtString：字符串对象

**源码位置：** `src/runtime/vm/rt_managed_types.h`

```cpp
struct RtString : RtObject {
    int32_t length;
    Utf16Char first_char;
};
```

内存布局：

```
┌──────────────────────────────────────┐
│ RtObject header                      │
│  klass* │ __sync_block*              │
├──────────────────────────────────────┤
│ length         (4 bytes)             │
│ first_char ←── UTF-16 字符序列起始    │
│ ...            (length * 2 bytes)    │
│ \0             (null terminator)     │
└──────────────────────────────────────┘
```

RtString 继承自 RtObject，增加了 `length`（字符数，不是字节数）和 `first_char`（与 RtArray 的 `first_data` 相同的占位技巧，标记 UTF-16 字符序列的起始位置）。

.NET 字符串在所有 CLR 实现中都是 UTF-16 编码，这是 ECMA-335 规定的。CoreCLR、Mono、IL2CPP、LeanCLR 在字符串的内部表示上没有分歧。

## Boxing 与 Unboxing

**源码位置：** `src/runtime/vm/object.h`

值类型和引用类型的转换是 CLR 对象模型中最关键的操作之一。

```cpp
class Object {
    static RtResult<RtObject*> box_object(
        metadata::RtClass* klass, const void* value);
    static RtResultVoid unbox_any(
        const RtObject* obj, metadata::RtClass* klass,
        void* dst, bool extend_to_stack);
    static const void* get_box_value_type_data_ptr(
        const RtObject* obj);
};
```

### Box 路径

`box_object` 接收值类型的 RtClass 和值数据的指针，执行以下操作：

```
1. 计算分配大小 = sizeof(RtObject) + instance_size_without_header
2. 分配堆内存
3. 设置 header：klass = 值类型的 RtClass
4. 将 value 数据复制到 header 之后的内存区域
5. 返回 RtObject 指针
```

装箱后的对象和普通引用类型对象在内存布局上没有区别——都是 RtObject 头 + 字段数据。区别在语义上：这个对象的 klass 指向的是一个值类型的 RtClass。

### Unbox 路径

`unbox_any` 做反向操作：给定一个装箱对象和目标值类型，将值数据从堆对象中提取出来。

`get_box_value_type_data_ptr` 是一个辅助函数——它返回装箱对象中值数据的起始地址，本质上就是 `(char*)obj + sizeof(RtObject)`。unbox 操作可以先调用这个函数拿到数据指针，再按 `instance_size_without_header` 的大小复制到目标位置。

`extend_to_stack` 参数处理的是一个细节：某些小于 4 字节的值类型（`bool`、`byte`、`short`）在 evaluation stack 上需要扩展到 4 字节（32 位），这是 ECMA-335 Partition III 规定的栈行为。

### 类型检查

```cpp
class Object {
    static const RtObject* is_inst(
        const RtObject* obj, metadata::RtClass* klass);
    static const RtObject* cast_class(
        const RtObject* obj, metadata::RtClass* klass);
};
```

`is_inst` 对应 C# 的 `is` 和 `as` 操作符。它沿着 `obj->klass->parent` 继承链向上遍历，检查是否有一个 RtClass 与目标 klass 匹配。匹配成功返回对象指针，失败返回 null。

`cast_class` 对应 C# 的强制类型转换 `(T)obj`。逻辑和 `is_inst` 相同，但类型不匹配时抛出 `InvalidCastException` 而不是返回 null。

`clone` 方法做浅拷贝——分配一个新对象，复制 header 和所有字段数据。

## 反射对象

**源码位置：** `src/runtime/vm/rt_managed_types.h`

LeanCLR 的反射类型对象有一个统一的设计模式：每个反射对象都内嵌一个 RtObject header，然后紧跟该反射类型特有的元数据指针。

### RtReflectionType

```cpp
struct RtReflectionType {
    RtObject header;
    const metadata::RtTypeSig* type_handle;
};
```

对应 C# 的 `System.Type`。`header` 使它成为一个合法的托管对象（可以被 GC 管理、可以参与类型检查），`type_handle` 指向类型签名，是运行时获取类型信息的入口。

### RtReflectionMethod

```cpp
struct RtReflectionMethod {
    RtObject header;
    const metadata::RtMethodInfo* method;
    RtString* name;
    RtReflectionType* ref_type;
};
```

对应 `System.Reflection.MethodInfo`。除了 header，它持有三个引用：`method` 指向方法的运行时描述符，`name` 是方法名的字符串对象，`ref_type` 是方法所属类型的反射对象。

### RtReflectionField

```cpp
struct RtReflectionField {
    RtObject header;
    metadata::RtClass* klass;
    const metadata::RtFieldInfo* field;
    RtString* name;
    RtReflectionType* type_;
    uint32_t attrs;
};
```

对应 `System.Reflection.FieldInfo`。字段最多：klass（所属类型）、field（字段描述符）、name（字段名）、type\_（字段类型的反射对象）、attrs（字段属性标志）。

### 设计模式

三个反射结构体的共同点是用 `RtObject header`（而不是继承 `RtObject`）嵌入对象头。这意味着反射对象在类型系统层面和普通托管对象有相同的内存布局起始结构，运行时可以统一处理。

`header.klass` 分别指向 `System.Type`、`System.Reflection.MethodInfo`、`System.Reflection.FieldInfo` 的 RtClass——这些 RtClass 就是 CorLibTypes 缓存的反射类型。

## 对比总览

把 LeanCLR、IL2CPP、CoreCLR 三个实现的对象模型关键设计放在一起：

| 维度 | LeanCLR | IL2CPP | CoreCLR |
|------|---------|--------|---------|
| **对象头结构** | RtObject: klass* + sync_block* | Il2CppObject: klass* + monitor* | ObjHeader(负偏移) + MethodTable* |
| **对象头大小** | 16B / 8B(单线程) | 16B | 16B (含 ObjHeader) |
| **类型描述符** | RtClass (~10 字段) | Il2CppClass (~50+ 字段) | MethodTable (内联 VTable) |
| **VTable 存储** | 独立 RtVirtualInvokeData[] | 内联在 Il2CppClass | 内联在 MethodTable 尾部 |
| **VTable 槽结构** | method_pointer + RtMethodInfo* | MethodInfo* | 函数指针 |
| **数组对象** | RtArray: bounds + length + first_data | Il2CppArray: bounds + max_length + values[] | Array: length + 内联数据 |
| **字符串编码** | UTF-16 (RtString) | UTF-16 (Il2CppString) | UTF-16 (System.String) |
| **Boxing** | Object::box_object 显式分配 | il2cpp::vm::Object::Box | JIT 内联或 helper call |
| **CoreLib 缓存** | CorLibTypes (70+ 类型) | il2cpp_defaults | CoreLibBinder |
| **反射对象** | 嵌入 RtObject header 成员 | 嵌入 Il2CppObject header 成员 | 继承 System.Object |

几个值得注意的差异：

CoreCLR 把 VTable 直接内联在 MethodTable 尾部，对象的 MethodTable 指针同时就是 VTable 的入口，虚方法分派只需要一次间接寻址。LeanCLR 和 IL2CPP 都是先从对象到类型描述符，再从类型描述符到 VTable，多一次间接跳转。

LeanCLR 的 RtClass 字段数量只有 IL2CPP Il2CppClass 的五分之一左右。这不是因为 LeanCLR 缺少功能，而是因为它把附加信息放在了别的结构里——泛型信息在 RtGenericClass，接口映射在运行时查找逻辑中。IL2CPP 选择把一切打平到一个大结构体，减少运行时的间接访问。

## 收束

LeanCLR 的对象模型用 6 个核心结构体搭建了完整的托管对象系统。

RtObject 提供 16 字节的双指针头，单线程版可裁剪到 8 字节。RtClass 用 10 个字段描述一个类型的运行时信息，配合 CorLibTypes 的 70+ 缓存项加速核心类型访问。VTable 采用独立的 RtVirtualInvokeData 数组，每个槽位同时保存执行入口和方法元数据。RtArray 和 RtString 在 RtObject 基础上用占位字段技巧实现变长数据区。Boxing/Unboxing 通过 Object 类的静态方法完成值类型与引用类型的转换。反射对象通过嵌入 RtObject header 成员融入统一的对象体系。

这套设计在概念上和 IL2CPP 高度相似——毕竟来自同一团队，面对同一份规范。但实现策略的差异反映了不同的约束条件：IL2CPP 在 AOT 环境下追求访问效率，把信息集中在大结构体里；LeanCLR 在解释执行环境下追求结构清晰和体积可控，把信息分散到多个精简结构中。

## 系列位置

- 上一篇：[LEAN-F3 双解释器架构]({{< ref "leanclr-dual-interpreter-hl-ll-transform-pipeline" >}})
- 下一篇：LEAN-F5 类型系统（计划中）
