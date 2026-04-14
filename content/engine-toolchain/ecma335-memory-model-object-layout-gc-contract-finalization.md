---
title: "ECMA-335 基础｜CLI Memory Model：对象布局、GC 契约与 finalization 语义"
date: "2026-04-14"
description: "从 ECMA-335 规范出发，拆解 CLI 内存模型的三大核心：对象在内存中的布局规则、GC 的规范契约与各 runtime 的实现策略、finalization 的语义边界与工程陷阱。基础层最后一篇，为进入各 runtime 实现分析做收束。"
weight: 15
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Memory"
  - "GC"
  - "ObjectLayout"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> 规范不关心你用几代 GC、用 mark-sweep 还是 copying collector——它只定义一份契约：不可达对象可能被回收，Finalizer 可能被调用，但不保证时机，不保证顺序。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 6 篇，也是基础层的最后一篇。

前 5 篇覆盖了 CLI 的五个层次：metadata 的物理结构、CIL 指令集与栈机、类型系统的分类规则、执行模型的运行时行为、程序集的身份与加载。这些层次回答了"代码怎么描述、怎么跑、怎么组织"的问题。但还有最后一块拼图：对象在内存里长什么样、GC 的规范契约是什么、finalization 的语义边界在哪。

这一篇补上这最后一块，然后基础层收束，进入各 runtime 的实现分析。

## 对象布局规范

ECMA-335 Partition I 12.6.2 定义了对象在内存中的基本结构，但定义得很克制——规范只规定了语义约束，不规定具体的内存格式。

### 引用类型对象

引用类型的实例是堆上分配的对象。规范要求每个对象必须携带足够的信息让 runtime 在运行时确定对象的类型——但没有规定这个信息的具体格式。

实践中所有主流 runtime 都采用了相同的基本结构：

```
引用类型对象 = object header + instance fields
```

object header 包含类型标识（指向类型描述结构的指针）和同步信息（用于 Monitor.Enter/Exit）。但 header 的具体大小、字段排列、附加信息完全由各 runtime 自行决定。

### 字段布局

ECMA-335 Partition II 10.1 定义了三种字段布局方式，通过 `StructLayoutAttribute` 或 metadata 中的 layout 标记指定：

**auto layout** — 默认值。runtime 自行决定字段的排列顺序和对齐方式。这意味着同一个类型在不同 runtime 中的字段偏移可能完全不同。C# class 默认使用 auto layout，runtime 可以为了对齐或缓存友好而重排字段。

**sequential layout** — 字段按声明顺序排列。用于 P/Invoke 场景下与 native 代码的内存布局对齐。C# struct 默认使用 sequential layout。

**explicit layout** — 每个字段通过 `FieldOffset` 属性显式指定偏移量。用于实现 union 语义（多个字段共享同一段内存）。

```csharp
// sequential layout（默认 struct 行为）
[StructLayout(LayoutKind.Sequential)]
struct Vector3 { public float x, y, z; }
// 字段顺序：x @ offset 0, y @ offset 4, z @ offset 8

// explicit layout（union 语义）
[StructLayout(LayoutKind.Explicit)]
struct IntOrFloat
{
    [FieldOffset(0)] public int intValue;
    [FieldOffset(0)] public float floatValue;  // 与 intValue 共享同一位置
}
```

对齐规则由 `Pack` 属性控制。`Pack = 1` 表示不做对齐填充，`Pack = 0`（默认）表示使用平台默认对齐。对齐直接影响对象大小——一个只有 `byte + int` 两个字段的 struct，在 `Pack = 0` 时因为对齐填充可能占 8 字节，而 `Pack = 1` 时只占 5 字节。

### 值类型的内存模型

值类型没有 object header。值类型的实例直接嵌入容器——可能在栈上（作为局部变量）、在另一个对象的字段区域中、在数组的元素区域中。

```
struct Point { int x; int y; }    // 8 字节，无 header

// 作为字段嵌入引用类型对象
class Line {
    Point start;    // 直接嵌入，offset = header_size
    Point end;      // 直接嵌入，offset = header_size + 8
}
```

这是值类型和引用类型在内存成本上的根本差异：一个包含 1000 个 `Point` 值类型字段的数组只需要一份 object header + 8000 字节的连续数据；如果 `Point` 是引用类型，需要 1001 份 object header + 8000 字节指针 + 8000 字节数据，且数据分散在堆上。

## 各 runtime 的对象头实现

规范不定义 object header 的格式，各 runtime 自行设计。但它们的设计呈现出很高的收敛性——都在 16 字节（64 位平台）左右。

| Runtime | 对象头结构 | 大小（64-bit） | 说明 |
|---------|-----------|---------------|------|
| **CoreCLR** | ObjHeader (4/8 bytes) + MethodTable* (8 bytes) | 12~16 bytes | ObjHeader 包含 SyncBlock index 和哈希码缓存，位于对象指针之前 |
| **IL2CPP** | Il2CppObject = klass* (8) + monitor* (8) | 16 bytes | klass 指向 Il2CppClass，monitor 用于线程同步 |
| **LeanCLR** | RtObject = klass* (8) + sync_block* (8) | 16 bytes | 单线程版可优化为只保留 klass 指针（8 bytes） |
| **Mono** | MonoObject = vtable* (8) + synchronisation* (8) | 16 bytes | vtable 指向 MonoVTable，包含类型信息和接口分派表 |

几个细节值得注意：

**CoreCLR 的 ObjHeader 位于对象指针之前。** 当代码持有一个对象引用时，引用指向的是 MethodTable* 的位置，而不是 ObjHeader 的位置。ObjHeader 在对象起始地址的负偏移处。这是 CoreCLR 独有的设计——其他三个 runtime 的对象引用都指向对象的第一个字段（klass/vtable 指针）。

**SyncBlock 的设计差异。** CoreCLR 使用一个全局 SyncBlock 表，ObjHeader 中存的是表的索引（thin lock 模式下直接存线程 ID + 重入计数，膨胀后才指向 SyncBlock 表）。IL2CPP 和 Mono 在对象头中直接存 monitor 指针。LeanCLR 的设计目标是嵌入式场景，单线程版本可以完全省略 sync_block 字段。

**类型指针的归宿。** 所有 runtime 都在对象头中保留一个指向类型描述结构的指针——CoreCLR 的 MethodTable*、IL2CPP 的 klass*、Mono 的 vtable*。GC 扫描对象时通过这个指针找到类型描述，从中获取字段布局信息，判断哪些字段是引用类型（需要递归扫描）、哪些是值类型（可以跳过）。

## 数组和字符串的特殊布局

数组和字符串是 CLI 中两种有特殊内存布局的内建类型。

### 数组

一维零基数组（SZArray，最常用的 `T[]` 形式）的布局：

```
SZArray = object header + length (4/8 bytes) + elements[0..length-1]

int[] arr = new int[4];

┌──────────────────────┐
│  object header       │  16 bytes (klass* + sync*)
├──────────────────────┤
│  length = 4          │  platform-dependent (通常 8 bytes, 含 padding)
├──────────────────────┤
│  elements[0] = 0     │  4 bytes (int)
│  elements[1] = 0     │  4 bytes
│  elements[2] = 0     │  4 bytes
│  elements[3] = 0     │  4 bytes
└──────────────────────┘
```

多维数组（如 `int[,]`）的布局更复杂：

```
多维数组 = object header + rank 相关信息 + bounds/sizes + elements

int[,] matrix = new int[3, 4];

┌──────────────────────┐
│  object header       │
├──────────────────────┤
│  总 length = 12      │
│  dim0_size = 3       │
│  dim1_size = 4       │
│  dim0_lower_bound = 0│
│  dim1_lower_bound = 0│
├──────────────────────┤
│  elements[0..11]     │  12 * 4 = 48 bytes
└──────────────────────┘
```

多维数组的访问比一维数组慢：`matrix[i, j]` 需要计算 `i * dim1_size + j` 再加上元素区域的起始偏移。一维数组只需要 `offset + i * element_size`。这就是为什么性能敏感的代码倾向于用 jagged array（`int[][]`，数组的数组）替代多维数组——jagged array 的内层是 SZArray，访问时只需一维索引。

### 字符串

`System.String` 在所有 runtime 中都有专用的内存布局：

```
String = object header + length (4 bytes) + chars[0..length-1] (UTF-16) + null terminator

string s = "Hello";

┌──────────────────────┐
│  object header       │  16 bytes
├──────────────────────┤
│  length = 5          │  4 bytes
├──────────────────────┤
│  'H' 'e' 'l' 'l' 'o'│  10 bytes (5 * 2, UTF-16)
│  '\0'                │  2 bytes (null terminator)
└──────────────────────┘
```

字符串使用 UTF-16 编码（每个 char 2 字节），末尾有一个 null terminator（方便 P/Invoke 传给 native API）。字符串对象创建后内容不可变（immutable）——`String.Concat` 会创建新的字符串对象，而不是修改现有对象。

这个不可变特性允许 runtime 做字符串驻留（string interning）：相同内容的字符串字面量可以共享同一个堆对象。`string.Intern` 方法显式触发驻留，编译时的字符串字面量默认驻留。

## GC 契约

ECMA-335 Partition I 12.6 定义了 GC 的规范契约。这份契约的特点是极度宽松——规范只定义语义保证，不规定实现算法。

### 规范定义的语义

**可达性判定。** GC 维护一组根（roots）。从根出发沿引用链可达的对象被认为存活，不可达的对象可能被回收。规范用"可能"而不是"一定"——runtime 可以选择延迟回收或永不回收某些对象。

**根的定义。** 根包括：
- 静态字段（static fields）中的引用
- 栈上的局部变量和参数中的引用
- CPU 寄存器中的引用（JIT 编译后）
- GC handles（`GCHandle.Alloc` 创建的显式根）

**不保证回收时机。** 规范没有规定 GC 何时运行。`GC.Collect()` 可以请求一次回收，但即使调用了也不保证所有不可达对象立即被回收。

**不保证 finalization 顺序。** 多个对象同时变得不可达时，它们的 Finalizer 的调用顺序未定义。

**弱引用。** `WeakReference` 和 `WeakReference<T>` 允许持有一个对象的引用但不阻止 GC 回收该对象。如果对象被回收，弱引用的 `Target` 属性返回 null。这让缓存等场景可以在内存压力下自动释放条目。

### 精确式 vs 保守式

规范没有要求 GC 是精确式（exact/precise）还是保守式（conservative）的。但这个选择对工程行为影响巨大：

**精确式 GC** 知道每个内存位置存的是引用还是值类型数据。扫描时只跟踪引用字段，不会把一个整数值误当成指针。CoreCLR、Mono SGen 都是精确式的——JIT/AOT 编译器为每个方法生成 GC info，记录每个 safe point 处栈上和寄存器中的引用分布。

**保守式 GC** 把所有看起来像指针的值都当作潜在的引用。这意味着一个恰好等于某个堆地址的整数可能阻止那个地址上的对象被回收（false retention）。BoehmGC 是保守式的——不需要类型信息就能扫描，实现简单，但可能产生内存泄漏。

## 各 runtime 的 GC 实现策略

| Runtime | GC 实现 | 类型 | 分代 | 核心特点 |
|---------|---------|------|------|---------|
| **CoreCLR** | 自研 GC | 精确式 | 3 代 + LOH + POH | Workstation/Server 两种模式，并发 GC |
| **IL2CPP** | BoehmGC | 保守式 | 不分代 | mark-sweep，简单但存在 false retention |
| **LeanCLR** | 当前为 stub | 设计目标精确协作式 | 待实现 | 嵌入式场景优先，初期可用引用计数 |
| **Mono** | SGen | 精确式 | 2 代（nursery + major） | copying nursery + mark-sweep major |

### CoreCLR GC

CoreCLR 的 GC 是工程复杂度最高的实现。关键设计点：

**三代分代。** Gen 0（新生代，最频繁回收）、Gen 1（中间代，缓冲区）、Gen 2（老年代，低频回收）。新分配的对象进入 Gen 0，经过一次 GC 存活后提升到 Gen 1，再存活提升到 Gen 2。分代策略基于一个统计观察：大多数对象的生命周期很短，只回收年轻代就能释放大量内存。

**LOH 和 POH。** Large Object Heap 存放 85KB 以上的大对象，避免大对象在代际提升时的复制开销。Pinned Object Heap（.NET 5+ 引入）专门存放被 pinned 的对象，减少 pin 对 GC 压缩的干扰。

**Workstation vs Server。** Workstation GC 在一个线程上执行回收，适合客户端应用。Server GC 为每个 CPU 核心分配一个 GC 堆和一个 GC 线程，适合高吞吐服务器场景。

### IL2CPP + BoehmGC

IL2CPP 选择 BoehmGC 有历史和工程原因。BoehmGC 是一个成熟的保守式 GC 库，集成简单——不需要 JIT/AOT 编译器生成精确的 GC info，扫描时直接把栈和对象中所有对齐的指针大小的值当作潜在引用。

代价是：

**False retention。** 一个碰巧等于堆地址的整数值会阻止对应对象被回收。在长时间运行的游戏中，这可能导致内存使用量缓慢增长。

**不分代。** 每次 GC 扫描整个堆。在堆较大时 GC 暂停时间可能明显——Unity 开发者对 GC spike 的抱怨很大一部分来自于此。

**Incremental GC。** Unity 2019+ 引入了增量式 BoehmGC，把一次 GC 的工作分散到多帧执行，减少单帧的暂停时间。但总工作量没有减少。

### Mono SGen

SGen 是 Mono 的精确式分代 GC。两代设计：nursery（新生代，使用 copying collector）和 major heap（老年代，使用 mark-sweep 或 mark-sweep-compact）。

SGen 对 nursery 使用复制式回收——把存活对象复制到另一块空间，然后整体释放原空间。这在新生代对象存活率低时非常高效（只需复制少量存活对象，不需要遍历整个空间标记可回收对象）。

## Finalization 语义

ECMA-335 Partition I 12.6.6.1 定义了 finalization 的语义。这是 CLI 内存模型中最容易误用的部分。

### 从析构函数到 Finalizer

C# 的 `~ClassName()` 析构函数语法在编译后变成 `Finalize()` 方法的 override：

```csharp
// C# 源代码
class ResourceHolder
{
    ~ResourceHolder()
    {
        ReleaseNativeResource();
    }
}

// 编译后的 IL 等价于
class ResourceHolder
{
    protected override void Finalize()
    {
        try
        {
            ReleaseNativeResource();
        }
        finally
        {
            base.Finalize();
        }
    }
}
```

编译器自动添加了 try/finally 包裹，确保基类的 Finalizer 也会被调用。

### Finalization 的执行流程

当 GC 发现一个不可达的对象且该对象有 Finalizer（override 了 `Finalize` 方法）时，不会立即回收它。完整流程：

1. GC 扫描发现对象 A 不可达
2. 检查 A 是否有 Finalizer
3. 如果有 → 把 A 放入 finalization queue，A 暂时被当作可达（不回收）
4. 独立的 Finalizer 线程从队列中取出 A，调用 A.Finalize()
5. Finalize() 执行完毕后，A 不再受 finalization queue 保护
6. 下一次 GC 如果发现 A 仍然不可达 → 真正回收

这意味着有 Finalizer 的对象至少需要两次 GC 才能被回收。在分代 GC 中，第一次 GC 后对象通常已经提升到 Gen 1 或 Gen 2，导致回收进一步延迟（因为老年代的回收频率更低）。

### 复活问题

Finalizer 在执行时拥有 this 引用。如果 Finalizer 把 this 赋给一个静态字段或其他可达对象的字段，对象就重新变得可达——这叫"复活"（resurrection）。

```csharp
class Zombie
{
    static Zombie alive;

    ~Zombie()
    {
        alive = this;  // 复活：对象重新可达
    }
}
```

复活后的对象不会再次进入 finalization queue（Finalizer 只调用一次，除非显式调用 `GC.ReRegisterForFinalize`）。这意味着复活的对象如果再次变得不可达，会被 GC 直接回收，不再调用 Finalizer。

复活是一个合法但危险的特性。实际工程中几乎不应该使用它——对象的状态在 Finalization 后可能不一致（其他被 finalize 的对象可能已经释放了资源），复活的对象处于未定义的状态。

### 规范不保证的事情

ECMA-335 对 finalization 做了几个明确的"不保证"：

**不保证 Finalizer 一定执行。** 进程退出时，runtime 可以选择不执行未处理的 Finalizer。CoreCLR 给 Finalizer 线程一个有限的超时时间（默认 2 秒），超时后直接终止进程。

**不保证执行顺序。** 两个对象 A 和 B 同时变得不可达，即使 A 引用 B，A 的 Finalizer 也不保证在 B 之前执行。这意味着 Finalizer 中不能安全地访问其他可 finalize 对象的状态——它们可能已经被 finalize 了。

**不保证执行线程。** 规范没有规定 Finalizer 在哪个线程执行。CoreCLR 使用专用的 Finalizer 线程，但这是实现细节而非规范要求。

### Dispose 模式

正因为 Finalizer 有这些不确定性，工程实践中推荐使用 Dispose 模式做确定性资源释放：

```csharp
class ManagedResource : IDisposable
{
    private IntPtr nativeHandle;
    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);  // 告诉 GC 不需要再调用 Finalizer
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }
            // 释放非托管资源
            ReleaseHandle(nativeHandle);
            disposed = true;
        }
    }

    ~ManagedResource()
    {
        Dispose(false);  // Finalizer 作为安全网
    }
}
```

`GC.SuppressFinalize(this)` 把对象从 finalization queue 中移除。调用 Dispose 后对象不再需要 Finalizer——资源已经释放了。这避免了 Finalizer 带来的两轮 GC 延迟和不确定性。

Finalizer 只作为"安全网"：如果调用方忘了 Dispose，Finalizer 确保非托管资源最终被释放（虽然时机不确定）。

## 内存模型与解释器的关系

解释器（LeanCLR、HybridCLR 的解释器部分）在内存模型上面临两个特殊问题。

### 解释器栈上的引用如何被 GC 看到

JIT 编译的代码使用硬件栈，JIT 为每个 safe point 生成 GC info，告诉 GC 栈上哪些位置存着对象引用。GC 扫描时读取这些信息，精确地追踪栈上的根。

解释器的栈帧是软件模拟的（LeanCLR 的 `InterpFrame`、HybridCLR 的 `MachineState`）。GC 不知道这些结构的存在——它们只是堆上或 native 栈上的普通 C++ 对象。解释器需要主动把自己管理的引用注册为 GC 根。

常见的做法：

- 解释器维护一个当前活跃的 InterpFrame 链表
- GC 扫描根时，遍历这个链表，扫描每个帧的局部变量和求值栈中的引用
- 解释器在入栈/出栈对象引用时更新帧的引用记录

如果这一步做错——比如某个临时引用没有被注册为根——GC 可能回收一个正在被解释器使用的对象，导致悬挂指针。这是解释器开发中最隐蔽的 bug 来源之一。

### Write Barrier

分代 GC 和增量 GC 都需要 write barrier。当代码把一个引用写入一个对象的字段时，runtime 需要记录这次写操作——可能是老年代对象引用了新生代对象（跨代引用），也可能是并发 GC 期间对象图被修改。

JIT 编译的代码中，编译器在每个引用类型字段赋值处自动插入 write barrier 调用。解释器也需要做同样的事情——在执行 `stfld`（存储字段）或 `stelem.ref`（存储数组引用元素）等指令时，如果目标字段是引用类型，解释器必须调用 runtime 的 write barrier API。

IL2CPP 的增量 BoehmGC 依赖 write barrier 追踪对象图的修改。如果 HybridCLR 的解释器在执行引用赋值时遗漏了 write barrier，增量 GC 可能错过某些引用更新，导致活对象被误回收。这在 Bridge-F（IL2CPP GC 模型分析）中会进一步展开。

## 收束

CLI Memory Model 的核心归纳为三层：

**布局层。** 引用类型对象 = object header + instance fields，值类型没有 header 直接嵌入容器。字段布局有 auto / sequential / explicit 三种模式。数组和字符串有专用的内存格式。规范不定义 object header 的具体结构——但四个主流 runtime 都收敛到 16 字节左右，包含一个类型指针和一个同步信息字段。

**GC 层。** 规范只定义语义契约：不可达对象可能被回收，根包括静态字段、栈变量、GC handles。不规定 GC 算法。CoreCLR 用三代精确式 GC，IL2CPP 用 BoehmGC（保守式、不分代），Mono 用 SGen（精确式、两代），LeanCLR 的 GC 尚在设计阶段。GC 类型的选择——精确式 vs 保守式、分代 vs 不分代——是各 runtime 最大的工程差异之一。

**Finalization 层。** 有 Finalizer 的对象需要至少两轮 GC 才能回收。Finalizer 的执行时机、顺序、线程都不由规范保证。复活是合法但危险的特性。工程实践用 Dispose 模式做确定性释放，Finalizer 只作为安全网。

这三层加上前 5 篇的 metadata、指令集、类型系统、执行模型、程序集模型，构成了 ECMA-335 基础层的完整拼图。从下一篇开始，进入各 runtime 的具体实现分析——CoreCLR 的 MethodTable 怎么编码类型信息、IL2CPP 的 MetadataCache 怎么初始化、LeanCLR 的解释器怎么管理对象生命周期——都是在这同一套规范之上做出的不同工程选择。

## 系列位置

- 上一篇：ECMA-A5 CLI Assembly Model：程序集身份、版本策略、加载模型
- 下一篇：ECMA-335 基础层完结，进入各 runtime 实现分析
