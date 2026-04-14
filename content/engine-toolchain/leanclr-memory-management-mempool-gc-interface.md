---
slug: "leanclr-memory-management-mempool-gc-interface"
date: "2026-04-14"
title: "LeanCLR 源码分析｜内存管理：MemPool arena、GC 接口设计与精确协作式 GC 的架构意图"
description: "拆解 LeanCLR 的内存管理实现：MemPool arena 分配器的 Region 链表与整体释放、GeneralAllocation 的 malloc 包装层、GeneralAllocator STL 适配器、GarbageCollector 的接口语义与 stub 实现，以及精确协作式 GC 的设计意图和当前 malloc-only 策略的适用场景。"
weight: 76
featured: false
tags:
  - LeanCLR
  - CLR
  - Memory
  - GC
  - SourceCode
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> ECMA-335 规定了 GC 的语义契约——托管对象在堆上分配、运行时负责回收——但不规定 GC 用什么算法、分配器怎么实现。每个 CLR 实现自行决定内存管理的具体策略。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 7 篇。

## 从 ECMA-A6 接续

ECMA-335 基础层第 3 篇（ECMA-A6）讲清了 CLI 类型系统的内存语义：引用类型在托管堆上分配，生命周期由运行时管理；值类型内联存储，赋值即复制。规范要求运行时提供自动内存管理（Partition I 8.4），但不约束具体实现——可以是分代式、标记-清扫、引用计数，甚至完全不回收。

LeanCLR 在这个问题上的回答分两个层面：编译期的内部内存管理已经完整实现（MemPool arena + GeneralAllocation），运行期的托管堆 GC 只定义了接口、尚未实现回收逻辑。这个分层本身就是一个设计决策，值得仔细拆解。

## MemPool：transform 专用 arena 分配器

**源码位置：** `src/alloc/MemPool.h`、`src/alloc/MemPool.cpp`

MemPool 是 LeanCLR 内部使用最频繁的分配器，但它不服务于托管对象——它服务于 IL transform 管线。

### 核心结构

```cpp
struct Region {
    Region* next;
    size_t capacity;
    size_t used;
    char data[];  // 柔性数组
};

class MemPool {
    Region* head;
    Region* current;
    size_t default_capacity;
};
```

Region 是一个固定大小的内存页，通过 `next` 指针串成单向链表。`data[]` 是柔性数组成员，Region 本身和数据区在同一次 `malloc` 中分配，减少一次间接寻址。

### 分配流程

分配逻辑是典型的 arena 模式：

```
1. 检查 current Region 剩余空间 (capacity - used)
2. 够用 → 返回 current->data + used，推进 used
3. 不够 → 分配新 Region，挂到链表尾，切换 current
```

新 Region 的容量取 `max(default_capacity, requested_size)`——如果单次请求超出默认页大小，直接分配一个刚好够用的大页。这个策略避免了对"合理的最大分配大小"做假设。

### 容量猜测

LEAN-F1 分析过 MemPool 的使用场景：每次方法 transform 都会创建一个独立的 MemPool，初始容量用 `methodBody.code_size * 32` 来估算。

这个 32 倍系数是经验值。一条 MSIL 指令平均 2-3 字节，transform 后会生成 LL-IL 指令、操作数数组、基本块结构、类型信息缓存等中间数据。32 倍使得大多数方法的 transform 可以在一个 Region 内完成，避免链表增长带来的间接跳转。

### 整体释放

MemPool 的析构函数遍历整个 Region 链表，逐个 `free`。没有逐对象释放的能力——这正是 arena 分配器的核心特征：分配快（指针推进）、释放快（整体销毁）、但不支持局部回收。

这和 transform 管线的生命周期模型完全匹配：transform 一个方法时产生大量临时数据，方法 transform 结束后这些数据全部失效，整体释放是最优策略。

## GeneralAllocation：通用 malloc 包装

**源码位置：** `src/alloc/GeneralAllocation.h`、`src/alloc/GeneralAllocation.cpp`

GeneralAllocation 是对系统 `malloc` / `free` 的薄包装层。

```cpp
class GeneralAllocation {
    static void* allocate(size_t size);
    static void* reallocate(void* ptr, size_t new_size);
    static void free(void* ptr);
};
```

功能上就是转发到 `malloc` / `realloc` / `free`。但包一层的价值在于建立统一的拦截点。

三个实际用途：

**统计**。所有走 GeneralAllocation 的分配都可以在这一层做计数、累计字节数，不需要修改调用方。

**替换**。要切换到 jemalloc、mimalloc 或者平台特定的分配器时，只改这一个文件。对嵌入式平台（LeanCLR 的目标场景之一），系统 `malloc` 的实现质量差异很大，统一入口使得适配成本可控。

**调试**。Debug 构建下可以加 guard bytes、记录调用栈、检测 double-free，所有改动集中在包装层。

这种模式在游戏引擎中很常见。Unity 的 UnsafeUtility.Malloc、Unreal 的 FMemory::Malloc 都是同样的思路——不是因为系统 malloc 不能直接用，而是因为一个足够大的项目迟早需要在分配层做全局策略调整。

## GeneralAllocator<T>：STL 容器适配

**源码位置：** `src/alloc/GeneralAllocation.h`

```cpp
template<typename T>
class GeneralAllocator {
    using value_type = T;
    T* allocate(size_t n) {
        return static_cast<T*>(
            GeneralAllocation::allocate(n * sizeof(T)));
    }
    void deallocate(T* p, size_t) {
        GeneralAllocation::free(p);
    }
};
```

这是标准的 C++ Allocator 概念实现，让 `std::vector<T, GeneralAllocator<T>>` 等 STL 容器的内存分配走 GeneralAllocation 而不是默认的 `std::allocator`。

LeanCLR 在运行时内部大量使用 `std::vector` 和 `std::unordered_map`。通过 GeneralAllocator，这些容器的内存分配也被纳入统一管控——和直接使用裸 `new` / `delete` 的容器相比，切换分配策略时不需要逐个修改容器声明。

## GC 接口设计：已定义但未实现

**源码位置：** `src/gc/GarbageCollector.h`、`src/gc/GarbageCollector.cpp`

gc 目录只有 76 行代码，但接口定义完整：

```cpp
class GarbageCollector {
    static void initialize();
    static void* allocate_fixed(size_t size);
    static RtObject* allocate_object(RtClass* klass, size_t size);
    static RtObject* allocate_array(RtClass* arrClass, size_t totalBytes);
    static void write_barrier(RtObject** obj_ref_location, RtObject* new_obj) {
        *obj_ref_location = new_obj; // TODO: implement write barrier
    }
};
```

每个方法都有明确的语义：

`initialize` 负责 GC 子系统的初始化——分配托管堆、设置分代参数、注册 root 扫描回调。

`allocate_fixed` 分配不受 GC 管理的固定内存块。用途是运行时内部需要长期存活、不应被回收的数据结构，比如类型元数据、interned 字符串表。

`allocate_object` 和 `allocate_array` 是托管对象的分配入口。接收 RtClass 指针和大小，返回初始化好 header 的对象指针。分开两个方法是因为数组对象在 header 之后还有 bounds/length 字段，布局不同。

`write_barrier` 是分代 GC 的核心组件。当一个对象引用字段被修改时——比如 `obj.field = newObj`——运行时必须通知 GC 这次写入。如果 `obj` 在老年代而 `newObj` 在年轻代，不记录这次写入会导致年轻代回收时漏扫 `newObj`，造成悬空引用。

当前实现是直接赋值 `*obj_ref_location = new_obj`，旁边标了 `TODO: implement write barrier`。

这组接口定义了一个清晰的 GC 抽象层：分配侧区分固定 / 对象 / 数组三种语义，写入侧预留了 barrier 插桩点。任何 GC 实现——不管是分代式、标记-清扫还是引用计数——都可以在不改变调用方代码的前提下接入。

## 精确协作式 GC 的架构意图

LEAN-F1 调研中提到，LeanCLR 的设计目标是精确协作式 GC（Universal 版），标准版降级为保守式。这个设计选择需要展开说明。

### 精确式 vs 保守式

保守式 GC（以 BoehmGC 为代表）的工作方式是：扫描栈和寄存器上所有看起来像指针的数值，如果一个值恰好落在堆的合法地址范围内，就把它指向的对象标记为存活。

这种方式不需要运行时提供额外的类型信息（哪些栈槽是引用、哪些是整数），实现简单。但有两个结构性缺陷：

**假阳性**。一个整数值碰巧和堆地址重合时会被误判为引用，导致该对象无法回收。在长期运行的应用中，这种泄漏会累积。

**无法移动对象**。保守式 GC 不能确定某个值到底是不是指针，所以不能移动对象（移动后需要更新所有指向该对象的引用，但如果"引用"实际上是整数，更新它会破坏数据）。不能移动意味着不能做压缩（Compaction），堆碎片化无法消除。

精确式 GC 要求运行时准确知道每个栈帧中哪些位置存放引用类型。扫描时只检查这些位置，不存在假阳性，也可以安全地移动对象。

LeanCLR 的解释器天然适合精确式 GC：LL-IL 指令集中每个操作数都有类型信息（F3 分析过），eval stack 的每个槽位类型在 transform 阶段已经确定。运行时在任意时刻都能精确枚举当前帧中哪些槽位持有对象引用。对比 JIT 编译的 CoreCLR，它需要额外生成 GC Info 表来记录每个代码点的引用分布——解释器免费获得了这个信息。

### 协作式的含义

协作式（cooperative）是指 GC 只在安全点（safe point）触发，而不是随时中断线程。

安全点的典型位置是：方法调用边界、循环回边、分配操作。在这些点上，所有线程的状态是确定的——栈帧结构完整、eval stack 内容已知——GC 可以安全地扫描和（如果是移动式 GC）更新引用。

对 LeanCLR 的解释器来说，安全点插入几乎没有成本：在 dispatch loop 的分配指令（newobj、newarr）处检查 GC 触发标志，如果需要回收就暂停执行、进入 GC 流程，然后恢复。每个解释器指令的边界天然就是安全点候选。

对比抢占式 GC（preemptive），它需要在任意指令处中断线程并扫描——这对 JIT 生成的原生代码可行（通过信号机制挂起线程），但对解释器来说是过度设计。

## 当前的 stub 实现意味着什么

gc 目录 76 行代码，没有任何回收逻辑。所有 `allocate_object` 和 `allocate_array` 调用最终都走向 `malloc`，分配出去的内存永远不会被回收。

这不是一个 bug，而是一个有意的阶段性决策。

在 LeanCLR 的目标场景——H5 小游戏、微信小游戏——中，一局游戏就是一个进程生命周期。游戏开始时创建运行时实例，游戏结束时整个进程销毁。在这种模式下，GC 回收的收益有限：进程存活时间短（几分钟到几十分钟），分配的对象总量可控，进程退出时操作系统会回收所有内存。

类比的例子是编译器。GCC、Clang 在编译单个翻译单元时也是 malloc-only——每次编译是一个短生命周期的进程，结束后操作系统回收一切。对这类工作负载，实现一个精细的 GC 的投入产出比不高。

但对长期运行的场景（比如服务端 CLR 宿主、持续运行的游戏逻辑），没有 GC 意味着内存只增不减，最终会耗尽。这就是 Universal 版精确式 GC 存在于设计目标中的原因——当 LeanCLR 需要支持长生命周期宿主时，接口层已经就绪，只需填充实现。

## 与 IL2CPP BoehmGC / CoreCLR GC 的对比

| 维度 | LeanCLR (当前) | IL2CPP (BoehmGC) | CoreCLR (Server/Workstation GC) |
|------|---------------|-----------------|-------------------------------|
| **GC 类型** | 无 (malloc-only) | 保守式标记-清扫 | 精确式分代压缩 |
| **对象移动** | 不涉及 | 不移动 | 移动 + 压缩 |
| **Write barrier** | stub (直接赋值) | 无 (非分代) | Card table / region-based |
| **栈扫描** | 未实现 | 保守扫描 | 精确 (GC Info 表) |
| **分配器** | malloc | BoehmGC allocator | bump pointer (分代) |
| **finalization** | 未实现 | BoehmGC finalizer 队列 | Finalizer 线程 |
| **回收触发** | 不触发 | 分配阈值 | 分配阈值 / 手动 |
| **设计目标** | 精确协作式 | N/A (第三方库) | 精确抢占式 |

IL2CPP 选择 BoehmGC 是工程妥协：IL2CPP 把 C# 编译成 C++ 后，栈上的引用混在 C++ 局部变量里，没有类型信息来区分引用和整数，只能用保守式扫描。这是 AOT 翻译路线的结构性限制——除非在生成的 C++ 代码中显式记录每个栈帧的引用位图，否则精确式不可行。

CoreCLR 的 GC 是工业级实现，分代压缩、并发标记、pinned 对象处理、大对象堆、Region-based 内存管理，复杂度远超 LeanCLR 和 IL2CPP 的需求。但它的设计前提和 LeanCLR 类似——JIT 编译器在生成代码时同步产出 GC Info 表，运行时可以精确知道每个指令点的引用分布。

LeanCLR 的解释器比 JIT 更天然地适合精确式 GC：eval stack 的类型信息在 transform 阶段已经固化到 LL-IL 指令中，不需要额外的 GC Info 生成步骤。如果未来实现精确式 GC，成本会比 JIT 运行时低得多。

## 收束

LeanCLR 的内存管理用 402 行 alloc 代码 + 76 行 gc 代码搭建了两层架构。

内层是运行时自身的内存管理。MemPool 用 Region 链表实现 arena 分配，服务于 transform 管线的短生命周期临时数据——分配快（指针推进）、释放快（整体销毁）、初始容量用 `code_size * 32` 估算减少扩展。GeneralAllocation 包装 malloc 建立统一拦截点，GeneralAllocator 让 STL 容器融入同一套分配策略。

外层是托管对象的 GC 接口。allocate_object / allocate_array / write_barrier 三个方法定义了分配和写入通知的抽象层，当前用 malloc + 直接赋值做 stub 实现。设计目标是精确协作式 GC——解释器的 eval stack 类型信息天然支持精确扫描，dispatch loop 的指令边界天然就是安全点。

当前的 malloc-only 策略不是技术负债，而是对目标场景（短生命周期嵌入式宿主）的合理选择。接口层已经就绪，回收算法的实现是增量添加、不改动调用方的工作。

## 系列位置

- 上一篇：[LEAN-F6 方法调用链]({{< ref "leanclr-method-invocation-chain-assembly-load-to-execute" >}})
- 下一篇：[LEAN-F8 Internal Calls 与 Intrinsics]({{< ref "leanclr-internal-calls-intrinsics-bcl-adaptation" >}})
