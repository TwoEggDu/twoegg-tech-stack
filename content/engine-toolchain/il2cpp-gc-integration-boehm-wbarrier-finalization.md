---
title: "IL2CPP 实现分析｜GC 集成：BoehmGC 的接入层、write barrier 与 finalization"
slug: "il2cpp-gc-integration-boehm-wbarrier-finalization"
date: "2026-04-14"
description: "拆解 IL2CPP 的 GC 集成层：为什么 C++ codegen 无法提供精确栈布局导致只能用保守式 BoehmGC、gc::GarbageCollector 包装层的抽象设计、Object::New 的对象分配路径、Unity 2021+ Incremental GC 引入 write barrier 的原因、GC_REGISTER_FINALIZER 的 finalization 机制、HybridCLR 解释器的 GC 集成策略，以及与 CoreCLR GC / Mono SGen / LeanCLR 的横向对比。"
weight: 55
featured: false
tags:
  - IL2CPP
  - Unity
  - GC
  - BoehmGC
  - Memory
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> IL2CPP 使用 BoehmGC 不是因为保守式 GC 够好，而是因为 IL → C++ → native 的转换链路无法产出精确的 GC 栈映射。这个架构约束决定了 IL2CPP 的整个内存管理策略。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 6 篇。

D3 在拆解 libil2cpp runtime 时，简要介绍了 BoehmGC 的集成——保守式 GC、不精确区分指针和整数、无法做对象搬移。这篇把 GC 集成作为独立主题展开：IL2CPP 为什么只能用保守式 GC、包装层的抽象设计、对象分配和回收的完整路径、write barrier 和 finalization 的实现、HybridCLR 解释器如何与这套 GC 集成层协作。

## IL2CPP 为什么用 BoehmGC

### 精确式 GC 的前提

精确式 GC 需要在收集时精确知道：栈上哪些位置存放着托管对象引用、寄存器中哪些值是引用、每个对象内部哪些字段是引用类型。

在 CoreCLR 中，RyuJIT 在编译每个方法时生成 GC Info——一份描述每个安全点上的根集信息的数据结构。GC 收集时读取 GC Info，精确枚举所有栈根。MONO-C4 分析过，Mono 的 SGen 也依赖 Mini JIT 生成的 GC Map 来实现精确式收集。

两者的共同前提是：**由 CLR 自己的 JIT 编译器生成 native code，JIT 编译器在生成代码的同时记录栈帧中每个位置的类型信息。**

### IL2CPP 的架构约束

IL2CPP 的转换链路是 IL → C++ → native code。Native code 不是由 CLR 自己的编译器生成的，而是由平台 C++ 编译器（Clang、MSVC、GCC）生成的。

C++ 编译器不知道什么是"托管引用"。对 Clang 来说，`Il2CppObject*` 就是一个普通的 C++ 指针——它不会为这个指针生成 GC 栈映射信息。C++ 编译器在做优化时，可能把一个对象引用从栈上移到寄存器中、可能把它溢出到栈上的某个位置、可能把它合并到一个临时变量中——这些变换对 GC 来说是不可见的。

要在 IL2CPP 中使用精确式 GC，需要满足以下条件之一：

**方案 A：在 C++ 层面维护引用信息。** il2cpp.exe 在生成 C++ 代码时，为每个可能被 GC 安全点打断的区域生成引用注册和注销代码。这需要在所有方法调用点、所有可能触发 GC 的分配点周围插入显式的引用注册逻辑，代码量和运行时开销都很大。

**方案 B：定制 C++ 编译器。** 修改 Clang 使其支持 GC 栈映射的输出，类似于 LLVM 的 GC 插件机制（Statepoint / GCRoot）。这需要深度集成 LLVM 的 GC 基础设施，并且每个平台的 C++ 编译器都需要适配。

**方案 C：使用不需要精确栈信息的 GC。** 保守式 GC 不依赖编译器提供的类型信息，通过扫描栈上所有看起来像堆地址的值来识别根。不需要 C++ 编译器的配合。

IL2CPP 选择了方案 C——使用 BoehmGC。这不是最优选择，而是在"IL → C++ → native"这条转换链路约束下的务实妥协。

### BoehmGC 的保守扫描

Boehm-Demers-Weiser GC（BoehmGC）的工作原理：

1. **栈扫描。** 遍历每个线程的栈空间，把所有对齐到指针大小的值检查一遍——如果某个值落在 GC 管理的堆地址范围内，就认为它可能是一个对象引用
2. **堆遍历。** 从根集（栈扫描结果 + 全局变量 + GC handle）出发，遍历对象图，标记所有可达对象
3. **回收。** 未标记的对象被认为不可达，回收其占用的内存

保守扫描的核心问题是 false positive（误保留）：栈上的一个整数值碰巧等于某个堆对象的地址，GC 就会把那个对象当作可达的，即使没有任何真实的引用指向它。这导致两个后果：

- **无法搬移对象。** 不能确定某个值是不是真的引用，就不能安全地修改它（更新为新地址）。所以 BoehmGC 不做堆压缩，所有对象分配后原地不动。
- **潜在的内存泄漏。** 误保留的对象无法被回收，长期运行后可能积累。在 32 位平台上（堆地址空间占整数空间的比例更高）这个问题更严重，在 64 位平台上概率较低但仍然存在。

## gc::GarbageCollector 包装层

IL2CPP 没有直接在代码中调用 BoehmGC 的 API，而是封装了一层抽象。

### 抽象设计

libil2cpp 的 GC 集成在 `il2cpp/gc/` 目录下，核心类是 `il2cpp::gc::GarbageCollector`。它提供了一组与 GC 实现无关的接口：

| 方法 | 职责 |
|------|------|
| `GarbageCollector::AllocateFixed` | 分配 GC 管理的内存（固定大小） |
| `GarbageCollector::Allocate` | 分配可被 GC 跟踪的内存 |
| `GarbageCollector::SetWriteBarrier` | 设置 write barrier |
| `GarbageCollector::RegisterFinalizer` | 为对象注册 finalization 回调 |
| `GarbageCollector::Collect` | 触发 GC 收集 |
| `GarbageCollector::GetUsedHeapSize` | 查询已用堆大小 |
| `GarbageCollector::RegisterThread` | 注册线程（让 GC 能扫描该线程的栈） |
| `GarbageCollector::UnregisterThread` | 注销线程 |
| `GarbageCollector::AddMemoryPressure` | 通知 GC 有不可见的内存压力 |

在 BoehmGC 实现下，这些方法的内部映射：

```
GarbageCollector::AllocateFixed  → GC_MALLOC / GC_MALLOC_UNCOLLECTABLE
GarbageCollector::Allocate       → GC_MALLOC
GarbageCollector::RegisterFinalizer → GC_REGISTER_FINALIZER_NO_ORDER
GarbageCollector::Collect        → GC_gcollect
GarbageCollector::RegisterThread → GC_register_my_thread
```

### 为什么做抽象

抽象层的设计意图是让 GC 实现可替换。虽然 IL2CPP 从诞生至今一直使用 BoehmGC，但 libil2cpp 的架构在接口层面保留了更换 GC 实现的可能性——如果未来有更好的保守式 GC，或者找到了在 IL2CPP 中使用精确式 GC 的工程方案，GC 的替换只需要在 `gc/` 目录下提供新的实现，不需要修改上层的 `vm/` 和转换后的 C++ 代码。

这种设计在实际中的另一个作用是隔离 BoehmGC 的 API 变化。BoehmGC 的不同版本之间 API 可能有变更，包装层把这些变更封装在一个位置。

## 对象分配路径

### 从 C# `new` 到 native 分配

当 C# 代码执行 `var obj = new MyClass()` 时，il2cpp.exe 把它转换为对 `il2cpp_codegen_object_new` 的调用：

```cpp
// il2cpp.exe 转换后的 C++ 代码
MyClass_t* V_0 = (MyClass_t*)il2cpp_codegen_object_new(MyClass_il2cpp_TypeInfo);
MyClass__ctor(V_0, /*method*/NULL);
```

`il2cpp_codegen_object_new` 是一个薄包装，最终落到 `il2cpp::vm::Object::New`。

### Object::New 的内部流程

`Object::New` 的核心步骤：

1. **获取类型信息。** 从传入的 `Il2CppClass*` 中读取实例大小（`instance_size`）。这个大小在类型初始化阶段已经计算好，包含对象头 + 所有实例字段
2. **GC 分配。** 调用 `GarbageCollector::AllocateFixed` 或 `GarbageCollector::Allocate`，传入实例大小。BoehmGC 从其管理的堆中分配一块对应大小的内存
3. **初始化对象头。** 在分配到的内存起始位置填入 `Il2CppObject` 结构——`klass` 指针指向 `Il2CppClass`，`monitor` 指针初始化为 NULL
4. **返回对象指针。** 返回类型化的指针，后续代码通过这个指针访问对象的字段和方法

```
内存布局：

┌─────────────────────────────────┐
│ Il2CppObject（对象头）            │
│   klass*  → Il2CppClass         │  ← Object::New 填入
│   monitor* → NULL               │
├─────────────────────────────────┤
│ 实例字段区域                      │
│   field_0                       │  ← 构造函数初始化
│   field_1                       │
│   ...                           │
└─────────────────────────────────┘
```

### 值类型的分配

值类型（struct）在 IL2CPP 中通常不经过 GC 分配——它们存储在栈上或作为其他对象的内联字段。只有在 boxing（装箱）时，值类型才需要 GC 分配：

```cpp
// int 装箱 → GC 分配一个 boxed int 对象
Il2CppObject* boxed = il2cpp_codegen_box(Int32_il2cpp_TypeInfo, &value);
```

Boxing 的分配路径与引用类型相同——`Object::New` 分配对象头 + 值类型大小的内存，然后把值拷贝到字段区域。

### 数组分配

数组分配通过 `il2cpp::vm::Array::New` 完成，与普通对象分配的差异在于：

- 内存大小 = 数组头（`Il2CppArray`） + 元素数量 x 元素大小
- 数组头包含额外的 `max_length` 字段记录数组长度
- 大数组可能触发 BoehmGC 的大对象分配路径（直接从操作系统分配大块内存）

## Write Barrier

### 为什么 IL2CPP 也需要 Write Barrier

BoehmGC 是非分代的保守式 GC——每次收集扫描整个堆。在这种模式下不需要 write barrier，因为不存在"只扫描年轻代"的局部收集。

但 Unity 2021+ 引入了 Incremental GC（增量式 GC），改变了这个前提。

### Incremental GC 的引入

Unity 的 Incremental GC 基于 BoehmGC 的增量标记模式。核心思路是把 GC 的标记阶段拆分为多个小步骤，分散到多个帧中执行，避免单帧内出现长时间的 STW（Stop-The-World）暂停。

增量标记的流程：

```
帧 1: 标记一部分对象 → 继续执行游戏逻辑
帧 2: 标记一部分对象 → 继续执行游戏逻辑
帧 3: 标记一部分对象 → 继续执行游戏逻辑
...
帧 N: 标记完成 → 短暂 STW → 清扫回收 → 恢复执行
```

问题在于：标记阶段跨越了多个帧，在标记过程中应用代码仍然在修改对象引用。如果在"对象 A 已被标记为可达"之后，应用代码把"对象 B（未标记）的引用"赋值给了"对象 A 的某个字段"，那么对象 B 可能在后续的标记步骤中被错误地认为不可达——因为扫描器已经过了对象 A，不会重新检查它的字段。

这就是增量/并发标记中经典的"丢失更新"问题。Write barrier 是标准的解决方案——在每次引用赋值时通知 GC，让 GC 知道有新的引用关系需要处理。

### il2cpp_gc_wbarrier_set_field

IL2CPP 的 write barrier 通过 `il2cpp_gc_wbarrier_set_field` 函数实现：

```cpp
void il2cpp_gc_wbarrier_set_field(
    Il2CppObject* obj,
    void** targetAddress,
    void* value)
{
    // 1. 执行实际的引用赋值
    *targetAddress = value;

    // 2. 通知 GC 这个位置的引用被修改了
    GC_dirty(targetAddress);
}
```

il2cpp.exe 在转换引用类型字段赋值时，生成对 `il2cpp_gc_wbarrier_set_field` 的调用。D5 分析过的泛型共享代码中也能看到这个调用：

```cpp
void Holder_1_Set_mSHARED(
    Holder_1_tSHARED* __this,
    RuntimeObject* ___value,
    const RuntimeMethod* method)
{
    il2cpp_gc_wbarrier_set_field(
        __this, (void**)&__this->____item, ___value);
}
```

Write barrier 的开销是每次引用赋值额外执行几条指令。对于非增量模式（Unity 的 Incremental GC 关闭时），write barrier 的 `GC_dirty` 调用可能是空操作或被条件跳过。

### 与精确式 GC 的 Write Barrier 对比

Mono SGen 和 CoreCLR 的 write barrier 服务于分代收集——追踪老年代到新生代的跨代引用。IL2CPP 的 write barrier 服务于增量标记——通知 GC 在标记过程中有引用被修改。

| 维度 | IL2CPP Write Barrier | Mono SGen / CoreCLR Write Barrier |
|------|---------------------|----------------------------------|
| **目的** | 增量标记的安全性 | 分代收集的跨代引用追踪 |
| **触发条件** | 引用类型字段赋值 | 引用类型字段赋值 |
| **记录机制** | `GC_dirty` 标记脏位 | card table 标记脏 card |
| **GC 使用方式** | 重新标记脏对象 | nursery GC 时扫描 dirty card |
| **非增量/非分代时** | 可跳过 | 仍然需要（分代是常态） |

表面上机制相似——都是在引用赋值时做额外记录——但服务的 GC 策略不同。SGen 和 CoreCLR 的 write barrier 是分代 GC 的必需品（没有它就不能做局部收集），IL2CPP 的 write barrier 只在 Incremental GC 模式下有意义。

## Finalization

### .NET 的 Finalization 语义

.NET 的 finalization（析构/终结）机制允许对象在被 GC 回收前执行清理逻辑。C# 中通过析构函数（`~ClassName()`）或实现 `IDisposable` 接口来使用。

析构函数在 IL 层面编译为 `Finalize()` 方法的重写。当 GC 发现一个可终结对象不再可达时，它不会立即回收该对象，而是把它放入 finalization queue。一个专门的 finalizer 线程从队列中取出对象并调用其 `Finalize()` 方法。`Finalize()` 执行完毕后，对象在下一次 GC 中才被真正回收。

这意味着可终结对象至少需要两次 GC 才能被回收——第一次 GC 发现它不可达并放入 finalization queue，第二次 GC 在 `Finalize()` 执行后回收它。这是 finalization 的固有开销。

### GC_REGISTER_FINALIZER

IL2CPP 通过 BoehmGC 的 `GC_REGISTER_FINALIZER_NO_ORDER` 实现 finalization 注册：

```cpp
// Object::New 中，如果类型有 Finalize 方法
if (klass->has_finalize)
{
    GarbageCollector::RegisterFinalizer(obj);
}
```

`GarbageCollector::RegisterFinalizer` 内部调用 BoehmGC 的 `GC_REGISTER_FINALIZER_NO_ORDER`，传入对象指针和一个 finalization 回调函数。当 BoehmGC 检测到对象不可达时，调用注册的回调。

回调函数的实现：

```cpp
static void invoke_invoke_invoke(void* obj, void* data)
{
    Il2CppObject* o = (Il2CppObject*)obj;
    // 通过 vtable 找到 Finalize 方法并调用
    il2cpp_invoke_finalize(o);
}
```

### Finalization Queue

BoehmGC 内部维护了一个 finalization queue。当 GC 标记阶段发现一个注册了 finalizer 的对象不可达时：

1. 不立即回收该对象
2. 把对象从"不可达"状态标记为"pending finalization"
3. 把对象放入 finalization queue
4. 对象在 queue 中被认为是可达的（不会被意外回收）

BoehmGC 的 finalization 处理有两种模式：

**同步模式。** GC 在收集完成后立即调用 pending finalizer。这可能导致 GC 暂停时间不可预测——如果 `Finalize()` 方法执行时间长，暂停时间就会很长。

**异步模式。** GC 收集完成后，由一个独立的 finalizer 线程异步调用 pending finalizer。这是 Unity 的默认行为——GC 暂停时间不包含 `Finalize()` 的执行时间。

### 与 CoreCLR Finalization 的对比

| 维度 | IL2CPP（BoehmGC） | CoreCLR |
|------|-------------------|---------|
| **注册** | `GC_REGISTER_FINALIZER_NO_ORDER` | 对象创建时自动标记 FinalizationReachable |
| **发现** | GC 标记阶段检测不可达 | GC 标记阶段检测不可达 |
| **队列** | BoehmGC 内部 finalization queue | CLR 的 FinalizationQueue |
| **执行** | finalizer 线程异步调用 | Finalizer Thread 异步调用 |
| **复活** | 对象在 queue 中保持可达 | 对象在 f-reachable queue 中保持可达 |
| **回收** | 下一次 GC 后回收 | 下一次 GC 后回收（至少两次 GC） |
| **Suppress** | `GC.SuppressFinalize` → 取消注册 | `GC.SuppressFinalize` → 清除标记 |

两者的 finalization 语义一致——都是"不可达 → 进入队列 → 执行 Finalize → 下次 GC 回收"的两阶段模型。这是 ECMA-335 规范定义的语义，不同 runtime 的实现路径不同但结果一致。

`GC.SuppressFinalize` 在 IL2CPP 中通过 `GC_REGISTER_FINALIZER(obj, NULL, ...)` 实现——把 finalizer 回调设为 NULL，等价于取消注册。这对于实现了 `IDisposable` 的类型很重要：`Dispose()` 方法执行清理逻辑后调用 `GC.SuppressFinalize(this)`，避免对象在已经被显式释放后还被 finalizer 线程再次处理。

## HybridCLR 解释器的 GC 集成

HybridCLR 在 IL2CPP 的 runtime 中注入了一个 IL 解释器。解释器执行的代码同样需要与 BoehmGC 协作——解释器栈上的托管引用必须对 GC 可见，否则 GC 可能错误地回收正在被解释器使用的对象。

### MachineState 动态根注册

HybridCLR 的解释器使用 `MachineState` 结构管理执行状态。`MachineState` 包含解释器的操作数栈和局部变量区——这些区域可能存放托管对象引用。

问题在于 BoehmGC 的保守扫描只覆盖线程的 native 栈。解释器的操作数栈是在堆上分配的独立内存区域，不在 native 栈范围内——BoehmGC 默认不会扫描它。

HybridCLR 通过 BoehmGC 的根注册 API 解决这个问题：

```cpp
// 概念上的根注册
GC_add_roots(machineState->operandStack,
             machineState->operandStack + stackSize);
```

把解释器的操作数栈范围注册为 GC 的额外根区域。GC 收集时除了扫描 native 栈，还会扫描这些注册的额外根区域。当解释器帧退出后，取消注册对应的根区域。

### HYBRIDCLR_SET_WRITE_BARRIER

HybridCLR 的解释器在执行引用类型字段赋值时，同样需要调用 write barrier。AOT 编译的代码由 il2cpp.exe 在转换时插入 write barrier 调用，但解释器执行的是 IL 字节码——没有经过 il2cpp.exe 的转换过程。

解释器在处理 `stfld`（字段存储）、`stelem.ref`（数组元素存储引用）等 IL 指令时，手动调用 write barrier：

```cpp
// 解释器执行 stfld 引用类型字段时
case HiOpcodeEnum::StfieldVarVar_ref:
{
    Il2CppObject* obj = /* ... */;
    Il2CppObject* value = /* ... */;
    HYBRIDCLR_SET_WRITE_BARRIER(
        (void**)((uint8_t*)obj + fieldOffset), value);
    break;
}
```

`HYBRIDCLR_SET_WRITE_BARRIER` 最终调用的是 IL2CPP 的 `il2cpp_gc_wbarrier_set_field` 或直接调用 BoehmGC 的 `GC_dirty`——与 AOT 代码中的 write barrier 走的是同一条路径。

### GC 安全性保证

HybridCLR 解释器的 GC 集成需要保证两个不变量：

**不变量 1：解释器栈上的引用对 GC 可见。** 通过 `GC_add_roots` 注册操作数栈实现。如果这个注册遗漏了，解释器正在使用的对象可能被 GC 错误回收。

**不变量 2：引用赋值通过 write barrier 通知 GC。** 通过在每条引用存储指令中插入 write barrier 调用实现。如果遗漏了 write barrier，Incremental GC 模式下可能丢失引用更新，导致存活对象被错误回收。

这两个不变量是 HybridCLR 正确性的基础约束之一。解释器的 bug 如果违反了这些不变量，会导致极难调试的内存错误——对象被意外回收，后续访问产生 use-after-free。

## 与 CoreCLR GC / Mono SGen / LeanCLR 的对比

| 维度 | IL2CPP（BoehmGC） | CoreCLR GC | Mono SGen | LeanCLR |
|------|-------------------|-----------|-----------|---------|
| **精确性** | 保守式 | 精确式 | 精确式 | 无 GC（宿主管理） |
| **选择原因** | C++ codegen 无法提供栈映射 | JIT 生成 GC Info | JIT 生成 GC Map | 极小体积，委托宿主 |
| **分代** | 无分代 | 三代（Gen0/1/2） | 两代（nursery + major） | N/A |
| **对象搬移** | 不支持 | 支持 | 支持 | N/A |
| **堆压缩** | 不支持 | 支持 | nursery + major 可选 | N/A |
| **碎片化** | 长期运行后不可避免 | 压缩消除 | nursery 零碎片，major 可选压缩 | N/A |
| **误保留** | 可能（保守扫描的 false positive） | 不会 | 不会 | N/A |
| **并发/增量** | 增量标记（Unity 2021+） | Background GC | 并发 major 标记 | N/A |
| **Write Barrier** | 增量标记需要 | 分代收集需要 | 分代收集需要 | N/A |
| **Finalization** | GC_REGISTER_FINALIZER | FinalizationQueue | GC_REGISTER_FINALIZER | N/A |
| **分配速度** | 中（BoehmGC 空闲链表） | 快（bump pointer + TLAB） | 快（bump pointer + TLAB） | N/A |

### 分配速度的差异

CoreCLR 和 Mono SGen 的年轻代使用 bump pointer 分配——只需一次指针加法和一次边界检查。MONO-C4 分析过，这是所有内存分配算法中最快的，O(1) 时间复杂度。

BoehmGC 的分配使用空闲链表（free list）——从大小匹配的空闲块链表中取出第一个可用块。这比 bump pointer 慢——需要遍历链表、匹配大小、可能产生碎片。

对于分配密集的游戏代码（每帧大量临时对象），分配速度的差异会体现在帧耗时中。这是 IL2CPP 使用 BoehmGC 的另一个性能代价。

### 碎片化的累积效应

BoehmGC 不做堆压缩——对象一旦分配就不会被搬移。长期运行的游戏中，大量对象的分配和回收会在堆上留下大小不一的空洞。虽然 BoehmGC 通过空闲链表管理这些空洞，但碎片化的累积效应是不可逆的：

- 总空闲空间可能很大，但没有足够大的连续块满足大对象的分配请求
- 实际内存占用高于存活对象的总大小——碎片空间无法利用也无法归还操作系统
- 在内存受限的移动端设备上，碎片化可能导致 OOM（Out of Memory），即使理论上还有足够的空闲空间

CoreCLR 和 Mono SGen 通过堆压缩解决碎片化——定期把存活对象搬移到堆的一端，消除所有空洞。IL2CPP 无法做到这一点——保守式 GC 的根本限制。

### LeanCLR 的策略

LeanCLR 当前的 GC 是 stub 实现，不是"委托给宿主"。作为约 600KB 的纯 C++17 解释器运行时，开源版本（Universal 单线程版）通过 `GeneralAllocation::alloc` 走系统 `malloc` 分配托管对象，不做回收——对象一旦分配，进程退出前不会被释放。

架构层面，LeanCLR 已经预留了 `write_barrier` 接口和精确扫描的设计意图。**设计目标**是 Universal 版做精确协作式 GC、Standard 版做保守式 GC，但当前实现尚未达成。区分"当前实现"与"设计目标"是阅读 LeanCLR 的关键——把路线图写成现状会误导读者。

## 收束

IL2CPP 的 GC 集成可以从三个层次理解。

**保守式 GC 是架构约束的结果，不是设计选择。** IL → C++ → native 的转换链路决定了 native code 由 C++ 编译器生成，C++ 编译器不提供 GC 栈映射。没有精确的栈信息，就只能用保守式 GC。这个约束是 IL2CPP 选择 BoehmGC 的根本原因。

**gc::GarbageCollector 包装层提供了实现隔离。** 上层代码通过统一接口与 GC 交互，不直接依赖 BoehmGC 的 API。这个设计在实际中隔离了 BoehmGC 的版本变化，在理论上保留了更换 GC 实现的可能。

**Incremental GC 引入了 write barrier，但不改变保守式的本质。** Unity 2021+ 的增量标记通过把标记阶段分散到多帧来减少 STW 暂停，write barrier 确保增量标记期间引用更新不丢失。这是在保守式 GC 框架内的延迟优化，不是精度升级——BoehmGC 仍然不能搬移对象、不能做分代收集、仍然存在误保留问题。

HybridCLR 的解释器通过动态根注册和 write barrier 调用与 IL2CPP 的 GC 集成层协作，确保解释器执行的代码遵循 BoehmGC 的收集语义。这是 HybridCLR 在 IL2CPP runtime 中正确运行的内存安全基础。

## 系列位置

- 上一篇：[IL2CPP-D5 泛型代码生成：共享、特化与 Full Generic Sharing]({{< ref "il2cpp-generic-code-generation-sharing-instantiation" >}})
- 下一篇（跨模块）：[MONO-C6 Mono 在 Unity 中的角色：为什么最终转向了 IL2CPP]({{< ref "mono-unity-role-why-il2cpp-replaced" >}})
