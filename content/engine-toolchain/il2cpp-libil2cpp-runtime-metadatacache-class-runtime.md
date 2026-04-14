---
title: "IL2CPP 实现分析｜libil2cpp runtime：MetadataCache、Class、Runtime 三层结构"
slug: "il2cpp-libil2cpp-runtime-metadatacache-class-runtime"
date: "2026-04-14"
description: "拆解 libil2cpp 运行时的内部三层结构：MetadataCache 全局类型注册表、Il2CppClass 惰性初始化、Runtime 初始化序列。覆盖 Il2CppObject 对象头设计、BoehmGC 集成、icall 注册机制、线程模型，以及 HybridCLR 的嵌入点。"
weight: 52
featured: false
tags:
  - IL2CPP
  - Unity
  - Runtime
  - MetadataCache
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> il2cpp.exe 做完转换就退场了，真正在运行时撑起整个 .NET 语义的是 libil2cpp——它负责类型注册、方法调用、GC 集成和所有运行时服务。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 3 篇，承接 D2 il2cpp.exe 转换器的展开。

D2 拆解了 il2cpp.exe 如何把 IL 翻译成 C++ 代码。但翻译后的代码无法独立运行——它依赖一整套运行时基础设施来完成类型查找、对象分配、GC 管理和方法调度。这套基础设施就是 libil2cpp。

## libil2cpp 在管线中的位置

回顾 D1 的五阶段管线：

```
Stage 1        Stage 2          Stage 3           Stage 4         Stage 5
C# 源码  →  IL 程序集  →  Stripped IL  →  C++ 源码  →  native binary  →  最终包体
(Roslyn)    (DLL)       (UnityLinker)   (il2cpp.exe)  (Clang/MSVC)
```

libil2cpp 处于 Stage 5 的 runtime 层。il2cpp.exe 产出的 C++ 代码在 Stage 4 编译时，会和 libil2cpp 的 C++ 源码一起编译、链接，最终合并到同一个 native binary 中——Windows 上是 `GameAssembly.dll`，Android 上是 `libil2cpp.so`。

il2cpp.exe 是构建时工具，构建完就退场。libil2cpp 是运行时常驻库，从进程启动到退出一直在工作。转换后的代码里到处充满了对 libil2cpp API 的调用：`il2cpp_codegen_object_new` 分配对象、`VirtFuncInvoker` 做虚分派、`NullCheck` 做空指针校验——这些都落到 libil2cpp 的实现上。

## 目录结构概览

libil2cpp 的源码位于 Unity 安装目录下 `Editor/Data/il2cpp/libil2cpp/`，核心子目录的分工如下：

| 目录 | 职责 |
|------|------|
| `il2cpp/vm/` | 虚拟机核心：Runtime、Class、MetadataCache、Object、Type、Thread 等 |
| `il2cpp/metadata/` | metadata 结构定义和二进制格式解析 |
| `il2cpp/gc/` | GC 集成层（BoehmGC 包装） |
| `il2cpp/os/` | 操作系统抽象层：线程、文件、内存、同步原语 |
| `il2cpp/icalls/` | internal call 实现：System.Array、System.String、System.Type 等 BCL 方法的 native 实现 |
| `il2cpp/utils/` | 工具函数：字符串处理、哈希、路径操作 |

这几个目录的依赖关系是单向的：`vm/` 调用 `metadata/`、`gc/`、`os/`、`icalls/`，反过来不成立。`vm/` 是 libil2cpp 的核心控制层，其他目录是它的服务提供方。

理解 libil2cpp 的核心，就是理解 `vm/` 目录下三个关键模块的分工：MetadataCache 管数据、Class 管类型、Runtime 管生命周期。

## MetadataCache：全局类型/方法注册表

**源码位置：** `il2cpp/vm/MetadataCache.cpp`

MetadataCache 是 libil2cpp 的全局注册中心。所有类型定义、方法定义、程序集信息在运行时的查找，最终都落到这里。

### 核心静态数组

MetadataCache 内部维护了一组静态数组，每个数组对应 metadata 的一个维度：

```cpp
// 按 TypeDefinitionIndex 索引的类型信息表
static Il2CppClass** s_TypeInfoTable;
// 按 TypeDefinitionIndex 索引的类型定义映像表
static Il2CppClass** s_TypeInfoDefinitionTable;
// 所有已加载程序集的列表
static Il2CppAssembly** s_AssembliesTable;
// 泛型类实例缓存
static Il2CppMetadataGenericClassMap s_GenericClassTable;
// 方法指针表——il2cpp.exe 生成的所有方法指针
static const Il2CppMethodPointer* s_MethodPointers;
```

这些数组在 `MetadataCache::Initialize()` 时从 `global-metadata.dat` 加载并建立索引。之后的所有查询操作——按 token 查类型、按名字查方法、按索引查程序集——都是直接数组索引或哈希表查找，不需要再碰 metadata 文件。

### 按 token 查找

ECMA-335 规范中每个类型和方法都有唯一的 metadata token。libil2cpp 沿用了这套编址体系。转换后的 C++ 代码中，类型引用和方法引用最终落到 token 上，MetadataCache 根据 token 从静态数组中取出对应的 `Il2CppClass*` 或 `MethodInfo*`。

```cpp
// 按 token 查找类型（简化示意）
Il2CppClass* MetadataCache::GetTypeInfoFromTypeDefinitionIndex(
    TypeDefinitionIndex index)
{
    // 先检查缓存
    if (s_TypeInfoTable[index])
        return s_TypeInfoTable[index];
    // 首次访问：从 metadata 构建 Il2CppClass 并缓存
    Il2CppClass* klass = FromTypeDefinition(index);
    s_TypeInfoTable[index] = klass;
    return klass;
}
```

首次查找时构建 `Il2CppClass`，后续直接返回缓存。这就是惰性初始化（lazy initialization）的入口。

### 只增不减的设计

MetadataCache 的静态数组在整个进程生命周期中只增不减。一旦一个 `Il2CppClass` 被构建并写入 `s_TypeInfoTable`，它就永远不会被释放或替换。

这个设计对原生 IL2CPP 来说是合理的——AOT 环境下所有类型在构建时已经确定，运行时不会产生新类型，自然不需要清理旧类型。

但对 HybridCLR 的热重载（assembly hot reload）而言，这成了一个结构性障碍。热重载意味着旧版本的程序集需要卸载、新版本需要重新加载，对应的 `Il2CppClass` 需要被替换。MetadataCache 的只增不减设计不支持这种操作。HybridCLR 的 DHE（Differential Hybrid Execution）模块在这一层做了额外的适配——但这属于 HybridCLR 的架构问题，不在本篇展开。

## Il2CppClass：运行时类型描述符

**源码位置：** `il2cpp/vm/Class.cpp`，结构体定义在 `il2cpp/metadata/il2cpp-metadata.h`

每一个托管类型在 libil2cpp 中都对应一个 `Il2CppClass` 实例。它是运行时类型系统的核心数据结构，承载了类型的所有运行时信息。

### 关键字段

```cpp
typedef struct Il2CppClass {
    const char* name;           // 类型名
    const char* namespaze;      // 命名空间
    Il2CppClass* parent;        // 基类指针
    
    FieldInfo* fields;          // 字段数组
    uint16_t field_count;       // 字段数量
    
    const MethodInfo** methods; // 方法数组
    uint16_t method_count;      // 方法数量
    
    VirtualInvokeData* vtable;  // 虚方法表
    uint16_t vtable_count;      // vtable 槽位数量
    
    Il2CppClass** interfaces;   // 实现的接口列表
    uint16_t interfaces_count;
    
    void* static_fields;        // 静态字段内存块的指针
    uint32_t instance_size;     // 实例对象的大小（含对象头）
    uint32_t actualSize;        // 实际分配大小（含对齐填充）
    
    uint8_t initialized;        // 是否已完成初始化
    // ... 更多字段
} Il2CppClass;
```

几个值得注意的设计：

`vtable` 是一个 `VirtualInvokeData` 数组，每个槽位包含方法指针和 `MethodInfo*`。虚方法调用时，D2 篇中 `VirtFuncInvoker` 的 `Invoke(slot, obj)` 最终就是从 `obj->klass->vtable[slot].methodPtr` 取出函数指针并调用。

`static_fields` 指向一块独立分配的内存，存放该类型所有静态字段的值。静态字段不属于任何对象实例，而是属于类型本身，所以由 `Il2CppClass` 统一持有。

`instance_size` 记录了该类型对象实例的总大小（包含对象头的 `Il2CppObject` 部分）。运行时分配对象时，`il2cpp_codegen_object_new` 直接用这个值向 GC 申请内存。

### Class::Init 惰性初始化

`Il2CppClass` 不是在 runtime 启动时全部初始化的，而是在首次被使用时才完成初始化。这就是 `Class::Init` 的核心逻辑。

```cpp
// 简化示意
bool Class::Init(Il2CppClass* klass)
{
    if (klass->initialized)
        return true;  // 已经初始化过，直接返回
    
    // 1. 初始化基类（递归）
    if (klass->parent)
        Class::Init(klass->parent);
    
    // 2. 解析字段布局，计算 instance_size
    SetupFields(klass);
    
    // 3. 构建 vtable
    SetupVTable(klass);
    
    // 4. 初始化接口实现
    SetupInterfaces(klass);
    
    // 5. 分配静态字段内存
    if (klass->static_fields_size > 0)
        klass->static_fields = GC_MALLOC(klass->static_fields_size);
    
    // 6. 标记完成
    klass->initialized = true;
    return true;
}
```

初始化是递归的——初始化一个类之前，先确保基类已经初始化完毕。这保证了 vtable 的 slot 继承和字段偏移计算的正确性。

惰性初始化的意义在于避免启动时一次性加载所有类型信息。一个项目可能有上万个类型，但一次运行只会用到其中一部分。按需初始化让启动速度和内存占用都保持在合理范围。

## Runtime：初始化序列

**源码位置：** `il2cpp/vm/Runtime.cpp`

`il2cpp::vm::Runtime` 是整个 libil2cpp 的生命周期管理器。引擎启动时调用 `il2cpp_init()`，最终走到 `Runtime::Init()`，按确定的顺序拉起所有子系统。

### 初始化链路

```
il2cpp_init()
  └─ Runtime::Init()
       ├─ os::Initialize()                    // 操作系统抽象层
       ├─ MetadataCache::Initialize()          // 加载 global-metadata.dat，建索引
       ├─ gc::GarbageCollector::Initialize()   // 启动 BoehmGC
       ├─ Thread::Initialize()                 // 线程子系统
       ├─ InternalCallManager::Initialize()    // 注册 icall 映射表
       └─ 注册 il2cpp.exe 生成的类型和方法表
```

顺序不是随意的：MetadataCache 必须在 GC 之前初始化，因为后续的类型构建需要分配 GC 管理的内存；GC 必须在 Thread 之前初始化，因为 GC 内部需要线程本地存储来跟踪分配状态。

### Runtime::Invoke

`Runtime::Invoke` 是 libil2cpp 暴露的通用方法调用入口。它接收一个 `MethodInfo*`、一个目标对象和参数数组，完成一次方法调用。

```cpp
Il2CppObject* Runtime::Invoke(
    const MethodInfo* method,
    void* obj,
    void** params,
    Il2CppException** exc)
{
    // 1. 确保所属类型已初始化
    Class::Init(method->klass);
    // 2. 通过 invoker 桥接参数格式，调用目标函数
    return method->invoker_method(method->methodPointer, method, obj, params);
}
```

`invoker_method` 是 il2cpp.exe 在构建时为每个方法签名生成的参数封装函数。它的作用是把 `void** params` 形式的通用参数列表拆解成目标函数需要的具体参数类型。反射调用（`MethodInfo.Invoke`）最终就是走这条路径。

## Il2CppObject：对象头

**源码位置：** `il2cpp/metadata/il2cpp-metadata.h`

每一个引用类型的托管对象在内存中以 `Il2CppObject` 开头：

```cpp
typedef struct Il2CppObject {
    Il2CppClass* klass;   // 类型指针
    MonitorData* monitor; // 同步块（lock/Monitor）
} Il2CppObject;
```

两个指针，在 64 位平台上占 16 字节。这就是所谓的"对象头"——每个堆上的引用类型对象都以这 16 字节开头，后面紧跟实例字段。

```
堆上的 Player 对象：
┌──────────────────────────────────┐
│ Il2CppObject header (16 bytes)   │
│  ├─ klass*  → Il2CppClass(Player)│
│  └─ monitor* → (通常为 null)      │
├──────────────────────────────────┤
│ int hp       (4 bytes)           │
│ float speed  (4 bytes)           │
│ String* name (8 bytes)           │
└──────────────────────────────────┘
```

`klass` 是运行时类型标识。`obj->klass == typeof(Player)` 的判断、vtable 查找、GC 扫描时判断对象类型——所有这些操作都通过 `klass` 指针完成。

`monitor` 用于 `lock` 语句和 `Monitor.Enter/Exit`。绝大多数对象一辈子不会被 lock，这个指针通常为 null。

### 与 LeanCLR RtObject 的对比

LeanCLR 的 `RtObject`（`src/runtime/vm/rt_managed_types.h`）结构几乎相同：

```cpp
// LeanCLR
struct RtObject {
    metadata::RtClass* klass;
    void* __sync_block;
};
```

两者都是双指针头，16 字节，一个指向类型描述符，一个用于同步。这不是巧合——ECMA-335 规范要求每个引用类型对象支持类型标识和 monitor 操作，双指针头是最直接的实现方式。

差异在于上层：IL2CPP 的 `Il2CppClass` 包含 vtable、方法指针、静态字段等完整的 AOT 运行时信息；LeanCLR 的 `RtClass` 更精简，偏向解释器执行的需求。

## GC 集成

**源码位置：** `il2cpp/gc/GarbageCollector.cpp`，`il2cpp/gc/BoehmGC.cpp`

IL2CPP 使用 BoehmGC 作为垃圾收集器。libil2cpp 通过 `gc::GarbageCollector` 类封装了 BoehmGC 的 API，提供统一的 GC 接口。

### 核心操作

```cpp
// 分配 GC 托管内存
void* gc::GarbageCollector::Allocate(size_t size)
{
    return GC_MALLOC(size);  // BoehmGC 的分配函数
}

// 注册 finalizer（析构回调）
void gc::GarbageCollector::RegisterFinalizer(Il2CppObject* obj)
{
    GC_REGISTER_FINALIZER(obj, RunFinalizer, NULL, NULL, NULL);
}
```

`GC_MALLOC` 从 BoehmGC 管理的堆中分配内存，分配出的内存会被 GC 跟踪。当对象不可达时，BoehmGC 在后台回收这块内存。如果对象注册了 finalizer（C# 中的 `~ClassName()` 析构函数），GC 会在回收前调用 `RunFinalizer`。

### write barrier

当一个引用类型字段被赋值时，GC 需要知道引用关系发生了变化。这通过 write barrier 实现：

```cpp
void il2cpp_gc_wbarrier_set_field(
    Il2CppObject* obj,
    void** targetAddress,
    void* value)
{
    *targetAddress = value;
    GC_END_STUBBORN_CHANGE(obj);  // 通知 BoehmGC 对象的引用关系已变化
}
```

il2cpp.exe 在转换代码时，每一处引用类型字段赋值都会被替换为对 `il2cpp_gc_wbarrier_set_field` 的调用。这确保了 GC 的引用图始终是最新的。

BoehmGC 作为保守式 GC，不精确区分栈上的值是指针还是整数。这意味着某些整数值恰好和堆地址重合时，会产生虚假引用（false positive），导致本应回收的对象被保留。D1 篇已经讨论过这个 trade-off——对于游戏运行时，这种不精确性在实践中影响有限。

## icalls：Internal Call 注册

**源码位置：** `il2cpp/icalls/`，注册逻辑在 `il2cpp/vm/InternalCallManager.cpp`

.NET BCL 中大量方法标记了 `[MethodImpl(MethodImplOptions.InternalCall)]`，这些方法没有 IL 实现，由运行时提供 native 代码。libil2cpp 通过 `InternalCallManager` 维护一张注册表，把方法的全限定名映射到 C++ 函数指针。

### 注册机制

```cpp
// 注册一个 icall 映射
void InternalCallManager::Add(
    const char* name,       // 如 "System.Array::GetLength"
    Il2CppMethodPointer func) // 对应的 C++ 实现
{
    s_InternalCallMap[name] = func;
}

// 查找 icall 实现
Il2CppMethodPointer InternalCallManager::Resolve(const char* name)
{
    auto it = s_InternalCallMap.find(name);
    if (it != s_InternalCallMap.end())
        return it->second;
    return NULL;  // 找不到时返回 null，运行时会抛 MissingMethodException
}
```

`il2cpp/icalls/` 目录下按命名空间组织了所有 icall 的实现。例如 `System.Array` 的 icall 在 `icalls/mscorlib/System/Array.cpp`，`System.String` 的在 `icalls/mscorlib/System/String.cpp`。

### 与 LeanCLR InternalCallRegistry 的对比

LeanCLR 的 icall 注册机制（`src/runtime/icalls/InternalCallRegistry`）在设计思路上和 libil2cpp 一致——都是全限定名到函数指针的映射表。核心差异在规模：libil2cpp 作为生产级 runtime，icall 覆盖面接近完整的 BCL surface；LeanCLR 作为精简运行时，只实现了 61 个核心 icall，覆盖 Array、String、Type、Thread、Math 等关键路径。

两者的 fallback 策略也不同。libil2cpp 找不到 icall 实现时直接报错。LeanCLR 在 icall 之前还有一级 intrinsic 拦截层，共 18 个 intrinsic——在方法分派进入解释器之前就完成处理。

## 线程模型

**源码位置：** `il2cpp/os/Thread.cpp`，`il2cpp/vm/Thread.cpp`

libil2cpp 的线程支持分两层：`os::Thread` 提供平台无关的线程原语封装，`vm::Thread` 在其上构建托管线程语义。

### os::Thread

`os::Thread` 封装了不同平台的原生线程 API：

| 平台 | 底层 API |
|------|---------|
| Windows | `CreateThread` / `WaitForSingleObject` |
| POSIX (Android/iOS/Linux) | `pthread_create` / `pthread_join` |
| WebGL | 不支持多线程 |

它提供了统一的 `Create`、`Join`、`Sleep`、`GetCurrentThread` 接口，上层代码不需要直接处理平台差异。

### Monitor 与 Interlocked

`il2cpp::vm::Monitor` 实现了 C# 的 `Monitor.Enter` / `Monitor.Exit`（即 `lock` 语句的底层机制）。每个 `Il2CppObject` 对象头中的 `monitor` 字段在首次 lock 时被初始化为一个 `MonitorData` 结构，包含互斥锁和条件变量。

`il2cpp::os::Interlocked` 封装了原子操作——`CompareExchange`、`Increment`、`Decrement`、`Exchange`。这些映射到平台特定的原子指令（x86 的 `lock cmpxchg`、ARM 的 `ldrex/strex`）。

C# 中 `System.Threading.Thread`、`System.Threading.Monitor`、`System.Threading.Interlocked` 的方法调用，最终通过 icall 机制落到 libil2cpp 的这些实现上。

## HybridCLR 怎么嵌入 libil2cpp

HybridCLR 不是一个独立的运行时，而是嵌入到 libil2cpp 内部的一组扩展模块。它的嵌入点集中在 `Runtime::Init` 的初始化序列中。

### 初始化嵌入

在安装了 HybridCLR 的项目中，`Runtime::Init` 的初始化链路变成：

```
Runtime::Init()
  ├─ os::Initialize()
  ├─ MetadataCache::Initialize()
  ├─ gc::GarbageCollector::Initialize()
  ├─ Thread::Initialize()
  ├─ InternalCallManager::Initialize()
  ├─ hybridclr::Runtime::Initialize()       // ← HybridCLR 嵌入点
  │    ├─ 注册解释器模块
  │    ├─ 初始化 metadata 扩展接口
  │    └─ 注册 supplementary metadata 加载回调
  └─ 注册 il2cpp.exe 生成的类型和方法表
```

`hybridclr::Runtime::Initialize()` 在 libil2cpp 自身的子系统初始化完毕后调用。它做三件事：把解释器模块（`hybridclr::interpreter`）挂进方法分派链路，使得标记为"需要解释执行"的方法走解释器而不是 AOT 代码；扩展 MetadataCache 的查找逻辑，使其能处理热更程序集注册的新类型；注册 supplementary metadata 的加载接口，补充 AOT 泛型的缺口。

### 方法分派的变化

原生 IL2CPP 中，每个 `MethodInfo` 的 `methodPointer` 字段在构建时就已经绑定到 AOT 编译出的函数指针上。调用一个方法就是直接跳转到那个地址。

HybridCLR 嵌入后，热更方法的 `methodPointer` 被设置为一个桥接函数。这个桥接函数把调用转发给 `Interpreter::Execute`，由解释器从 metadata 中取出方法的 IL 字节码，走解释执行路径。

这是 HybridCLR 系列 D1 篇详细拆解的内容，这里只点出嵌入位置和机制。核心认知是：HybridCLR 没有替换 libil2cpp，而是在 libil2cpp 已有的框架内注入了解释执行路径。

## 收束

libil2cpp 的内部结构可以归纳为三个核心层次：

**MetadataCache——数据层。** 从 `global-metadata.dat` 加载类型和方法信息，建立以 token 为索引的静态数组。所有运行时类型查询的最终落点。只增不减的设计让 AOT 场景下查询效率极高，但也为热重载带来了结构性障碍。

**Il2CppClass——类型层。** 每个托管类型在运行时的完整描述符，包含字段布局、vtable、接口列表、静态字段内存。通过 `Class::Init` 惰性初始化——首次使用时递归构建，避免启动时一次性加载全部类型。

**Runtime——控制层。** 管理整个运行时的生命周期。`Runtime::Init` 按确定顺序拉起 MetadataCache → GC → Thread → icall 等子系统。`Runtime::Invoke` 提供通用的方法调用入口。

围绕这三层，Il2CppObject 定义了对象头的内存格式（双指针：klass + monitor），BoehmGC 通过 `gc::GarbageCollector` 包装层完成内存管理和 write barrier，icall 注册表把 BCL 方法连接到 native 实现，线程模型封装了平台差异。

这些模块之间的边界是清晰的，各自可以独立分析。后续 D4 篇将深入 `global-metadata.dat` 的二进制格式——MetadataCache::Initialize 到底从那个文件里读了什么、怎么读的、版本校验机制如何工作。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}}">IL2CPP 实现分析｜il2cpp.exe 转换器</a>
- 下一篇：<a href="{{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}">IL2CPP 实现分析｜global-metadata.dat</a>
