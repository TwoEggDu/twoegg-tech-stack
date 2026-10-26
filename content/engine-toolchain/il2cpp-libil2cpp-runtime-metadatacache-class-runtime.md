---
title: "IL2CPP 实现分析｜libil2cpp runtime：MetadataCache、Class、Runtime 三层结构"
slug: "il2cpp-libil2cpp-runtime-metadatacache-class-runtime"
date: "2026-04-14"
lastmod: "2026-10-26"
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

MetadataCache 及相关 metadata 模块负责注册、解析和缓存类型、方法与程序集信息。内部职责会随 Unity 版本调整；这里先看输入数据、代码注册和运行时缓存之间的关系，不把某个版本的数组名当成稳定接口。

### 描述、代码注册与运行时缓存

| 来源 | 提供什么 | 不能直接等同于什么 |
|---|---|---|
| global-metadata.dat | 转换后的类型、成员、字符串等描述 | 原始 CLI 表流或全部 native 函数地址 |
| 生成代码及其注册数据 | 构建产物中的方法实现与相应关联信息 | 某个 DLL 的完整 IL 方法体集合 |
| 运行时解析与缓存 | 已准备的类型／方法描述、泛型实例等 | 构建时已完整创建的全部运行时对象 |

因此，不能说所有方法指针都从 global-metadata.dat 读出，也不能由“存在查找表”推出每次查询都只是一次固定数组访问。解析过程可能需要泛型上下文与惰性准备；成功后可以复用缓存。

### 原始 token、转换后索引与运行时对象

CLI 表 token 只在所属模块内定位一条记录。它不是跨 DLL 的全局编号，也不能单独表示一个封闭泛型方法的完整身份。Microsoft 的 [MetadataToken 说明](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.memberinfo.metadatatoken)同样要求结合 Module 理解它。

| 层次 | 例子 | 使用时需要什么上下文 |
|---|---|---|
| 原 DLL 的 CLI token | 0x02000003、0x06000001 | 所属模块、对应构建产物，必要时带泛型实参 |
| IL2CPP 转换后的索引 | TypeDefinitionIndex 等 | 对应 Unity 版本、metadata 格式及该次构建的注册关系 |
| 运行时描述对象 | Il2CppClass、MethodInfo | 解析结果、泛型上下文、初始化与执行状态 |

例如 `GetTypeInfoFromTypeDefinitionIndex` 的参数名说明它接收类型定义索引，不能把原 DLL 的 `0x02000003` 原样传入。某些记录可以保留原始 token，用于关联或诊断；这也不意味着 token 与所有内部索引采用同一种编码。

```text
构建输入：模块 + CLI 引用／定义 + 泛型上下文
  → 裁剪与转换：生成该构建自己的 metadata 和代码关联
  → 运行时：按相应引用解析、必要时准备类型／方法并缓存
```

这张图说明身份转换，不代表每次调用都要完整重走一次链路。直接 native 调用可能已经由构建期绑定；反射、泛型和接口分派需要的信息也不同。CLI 一侧的完整例子见 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

### 缓存寿命与程序集卸载

普通 IL2CPP 项目不能把“再加载一份同名 DLL”当作替换类型和回收旧缓存的通用办法。不过，AOT 也不意味着运行时绝不能构造泛型类型描述；类型信息可用和方法实现可用仍需分开判断。

程序集卸载需要同时处理旧对象、引用、类型信息和执行入口。HybridCLR 的相关产品能力见 [程序集卸载专题]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}})，不能把它与 DHE 差分混合执行直接视为同一个机制。

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

这里讨论的是类型描述的准备，不等同于执行用户的 `.cctor`。静态字段存储已经分配，也不证明静态初始化表达式已经执行；触发条件与失败语义见 [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})。

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

## Player 对照：类型准备与 .cctor 不能互相代证 {#type-initialization-runtime}

上面的 Class 类型信息准备、Runtime 整体启动，与 CLI 的用户 .cctor 是不同职责。不能只看到 Class::Init 的名字，就认定用户静态构造已经执行；也不能把 Runtime::Init 与某个类型的 Runtime::ClassInit 混为一谈。

2026-10-26 的独立 `projects/type-initialization` 使用本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3／runtime v9.0.1 和 Windows x64 Development IL2CPP Debug。build_id `5b177115-e8ae-4e58-8808-2a0bc54b092a`。AOT／hot 场景仅命名空间不同，最终 DLL 的标志与自有 CIL 已读取核对；HotUpdate 未进入 AOT 代码注册。

[完整矩阵见类型初始化篇]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#type-initialization-player" >}})。显式 .cctor 和两个封闭泛型的静态状态在两侧符合样本预期；beforefieldinit 在本例都早于 Ping 方法体完成初始化，这是一种允许的顺序。30 个普通进程中 27 个完成、3 个热更内部捕获路径异常逃逸，不能称为全部行为一致。共用 AOT 外层捕获的额外路径在两侧均确认第二次访问没有重跑失败构造体。

两份独立 native 记录进一步把阶段拆开：Ping 转换在前，.cctor 经 Runtime::ClassInit 执行在后；初始化成功后，strict 才记录到两次 Ping 方法体事件。failed 虽没有方法体事件，结束时相同 Ping MethodInfo 的 interpData 仍非空。由此可见：

| 已观察状态 | 仍需另证的事实 |
|---|---|
| 类型身份和方法可解析 | 用户静态状态是否已建立 |
| 方法已转换、缓存可用 | 类型初始化是否成功、方法体是否真正执行 |
| 一次方法返回成功 | 其他封闭泛型的静态状态、业务依赖和其他异常路径是否就绪 |

本轮保留热更 catch 差异，不修改运行时或猜测根因。最终两个 DLL 都把失败 call 放在 catch 保护区间内，因此后续应围绕类型初始化异常与调用边界设计对照。复现工具为 `tools/init_lab.py`，本轮固定提交为 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/46ac71c74cd068bc177d653dd143b7bb9dc482bd)，见[实验工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/46ac71c74cd068bc177d653dd143b7bb9dc482bd/projects/type-initialization)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/results.json)；前轮 `74da063` 不是本轮版本。此结果不包含初始化锁并发、循环依赖、设备性能或跨平台结论。

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
| WebGL | 依版本与构建选项区分托管线程和原生 C/C++ 线程，不能一概而论 |

它提供了统一的 `Create`、`Join`、`Sleep`、`GetCurrentThread` 接口，上层代码不需要直接处理平台差异。

### Monitor 与 Interlocked

`il2cpp::vm::Monitor` 实现了 C# 的 `Monitor.Enter` / `Monitor.Exit`（即 `lock` 语句的底层机制）。每个 `Il2CppObject` 对象头中的 `monitor` 字段在首次 lock 时被初始化为一个 `MonitorData` 结构，包含互斥锁和条件变量。

`il2cpp::os::Interlocked` 封装了原子操作——`CompareExchange`、`Increment`、`Decrement`、`Exchange`。这些由平台原子原语实现；具体指令序列受架构、编译器和目标选项影响，不应固定为某一组 ARM 或 x86 指令。

C# 中 `System.Threading.Thread`、`System.Threading.Monitor`、`System.Threading.Interlocked` 的方法调用，最终通过 icall 机制落到 libil2cpp 的这些实现上。

### 接入与平台边界

托管线程、SDK 创建的原生线程和 Unity 主线程约束需要分开。runtime 登记解决线程身份及 GC 接入，不能让任意 Unity API 自动变成后台线程安全。实际 wrapper 是否已登记、谁负责退出以及解释器 TLS 怎样清理，见 [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

[Unity 2023.2 WebGL 限制](https://docs.unity3d.com/2023.2/Documentation/Manual/webgl-technical-overview.html)明确区分 C# 多线程限制与有条件的 C/C++ 多线程支持。不要把原生线程开关当成托管 Thread 可用的证明；其他版本和小游戏宿主须单独核对。

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

已有 AOT 实现可以通过相应方法入口执行；但不能从“拿到了 MethodInfo”推断所有泛型实例化都已有可调用的 native 方法。直接调用、反射调用和虚分派的入口组织也不完全相同。

HybridCLR 为需要解释执行的方法接入相应调用路径。原始 CIL 方法体先转换为内部寄存器指令，再由解释器执行；已准备的转换结果可以复用。跨 AOT／解释边界时还需要匹配参数和返回值表示，不能把所有调用都概括为某一个字段的固定跳转。转换与执行的职责见 [HybridCLR 官方模块说明](https://www.hybridclr.cn/docs/basic/sourceinspect)。

这是 HybridCLR 系列 D1 篇详细拆解的内容，这里只点出嵌入位置和机制。核心认知是：HybridCLR 没有替换 libil2cpp，而是在 libil2cpp 已有的框架内注入了解释执行路径。

<a id="icpp-2"></a>

## ICPP-2：它描述谁，指向谁，又由谁管理

沿[D2同一ReadingProbe程序]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md#icpp-1" >}})继续。Counter类型、某个counter实例、Value字段描述、Value的当前值和保存它的存储是五项不同的观察内容，不能因为调试器都能显示一个地址就混成一项。

| 读到的东西 | 它描述/指向谁 | 管理与观察边界 |
|---|---|---|
| Counter的CLI TypeDef及runtime类型描述 | 一种类型；后者还承担当前runtime实现所需信息 | 描述符不是Counter实例，不按实例析构处理 |
| Step的方法描述/MethodDef身份 | 声明类型、签名和方法 | 找到描述不保证已有独立native函数或代码已准备 |
| token、转换索引、GCHandle值 | 各自表/命名空间中的定位信息 | 不能当可直接解引用的对象地址；须经对应解析协议 |
| counter引用 | 一个运行实例 | 与另一次new的Counter不同；类描述可相同 |
| Value字段描述/本次字段访问 | 字段定义及counter内当前值 | 原始CLI信息与runtime实际布局分层；偏移随版本/平台/类型变化 |
| ReadingProbe.Held静态字段 | 一条托管强持有关系 | 清空仅解除这条路径，不删除类型描述，也不承诺当场GC |
| native局部/包装 | 一条C++访问或资源责任 | 析构native包装不等于销毁托管目标，是否登记GC根另查 |

桌面自测的实际变化是：Counter(7)建立实例；Step(4)读Value并写成11；Held赋值后ReferenceEquals为真、读到同一字段11；再把Held清空。这条**字段读写与持有解除**路径已在.NET桌面执行。它没有测量该对象在Unity中的地址、runtime描述符或GC归因。

### 把三段证据摆开

1. **新样本的实际CIL**：Step有ldfld/stfld，Run有stsfld Held和置空。操作数解析为字段身份而非凭相邻字节猜偏移；输出和hash随完整输入维护。
2. **已有Player的保留/解除实验**：固定实验仓[a387d28记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)，build_id70021d1d-8d18-4bdf-8066-446dd9898b80。held三组为0/16/16，解除静态数组后0/0/16，再解除事件后0/0/0；它回答该独立样本的托管持有路径，不是本节Counter的回收记录。
3. **已有根区间与对象快照**：固定[4861bed记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)有活跃根区间及Context→Slots的对象图；ManagedStacks条目为0，没有给出解释器动态根的完整唯一归因。不可用这份快照推定任何native局部都不会保留目标。

这三段具有独立构建/进程身份。复用已取得的Player证据避免重做相同GC实验，但不会把新程序也标为Unity通过。新Counter的IL2CPP字段访问、类型/方法描述与实际实例地址对应仍待同源构建采证。

语言接缝回看[CXX-02拥有]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})、[CXX-06布局/字节]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})、[CXX-09对象/存储]({{< relref "engine-toolchain/cpp-foundations-09-storage-object-lifetime-ub.md" >}})；托管存储与根协议继续见[GC专题]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})和[共享线程专题]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。不复写某版结构体，更不把双指针对象头大小当通用托管格式。

方法描述如何接入调用和注册，接[D5的同样本分派表]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md#icpp-3" >}})。


## 收束

libil2cpp 的内部结构可以归纳为三个核心层次：

**MetadataCache 及相关模块——数据层。** 连接转换后的 metadata、代码注册数据与运行时缓存。原始 CLI token、转换后的索引和运行时对象是不同层次；泛型解析与缓存寿命需要另外确认。

**Il2CppClass——类型层。** 每个托管类型在运行时的完整描述符，包含字段布局、vtable、接口列表、静态字段内存。通过 `Class::Init` 惰性初始化——首次使用时递归构建，避免启动时一次性加载全部类型。

**Runtime——控制层。** 管理整个运行时的生命周期。`Runtime::Init` 按确定顺序拉起 MetadataCache → GC → Thread → icall 等子系统。`Runtime::Invoke` 提供通用的方法调用入口。

围绕这三层，Il2CppObject 定义了对象头的内存格式（双指针：klass + monitor），BoehmGC 通过 `gc::GarbageCollector` 包装层完成内存管理和 write barrier，icall 注册表把 BCL 方法连接到 native 实现，线程模型封装了平台差异。

这些模块之间的边界是清晰的，各自可以独立分析。后续 D4 篇将深入 `global-metadata.dat` 的二进制格式——MetadataCache::Initialize 到底从那个文件里读了什么、怎么读的、版本校验机制如何工作。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}}">IL2CPP 实现分析｜il2cpp.exe 转换器</a>
- 下一篇：<a href="{{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}">IL2CPP 实现分析｜global-metadata.dat</a>
