---
title: "ECMA-335 基础｜Threading 内存模型：volatile、原子操作与内存屏障的规范定义"
slug: "ecma335-threading-memory-model-volatile-barriers"
date: "2026-04-15"
description: "从 ECMA-335 规范出发，拆解 CLI 内存模型的三层契约：volatile 语义、Interlocked 原子操作、Memory Barrier 三类 fence；说明 .NET 内存模型相对 C++ 与 Java 的强弱位置，以及 CoreCLR/Mono/IL2CPP/HybridCLR/LeanCLR 在不同硬件架构上的实现差异。"
weight: 20
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Threading"
  - "MemoryModel"
  - "Volatile"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> volatile、Interlocked、Thread.MemoryBarrier 不是 BCL 的便利函数——它们对应 ECMA-335 规范层就定义好的内存屏障语义。理解规范，才能理解各 runtime 的内存模型承诺和 C# 代码的真实行为。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 13 篇。

前 12 篇覆盖了 metadata、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享、verification、custom attribute、P/Invoke、security。这些层次描述了 CLI 在单线程视角下代码的组织、执行、加载与权限。这一篇补上并发视角的那块拼图：多线程环境下，CLI 规范对内存可见性做出什么承诺、volatile 与 Interlocked 背后的规范定义是什么、各 runtime 如何在 x86 与 ARM 上兑现这份承诺。

> **本文明确不展开的内容：**
> - Thread / Task / async 的 BCL API（属于 BCL 不在 ECMA 规范层）
> - 硬件内存模型（x86 TSO / ARM weak / RISC-V RVWMO 的指令级细节）
> - lock-free 数据结构设计模式

## 为什么内存模型要在规范层定义

ECMA-335 Partition I §12.6.4 定义了 CLI 的 memory ordering。这个章节回答了一个具体的问题：一个线程对内存的写入，另一个线程什么时候能看到？

托管代码不能假设"顺序执行"。现代 CPU 和编译器都会为了性能做指令重排：

- **编译器重排** — C# 编译器和 JIT 编译器都会按优化策略重排 IL / 机器指令。只要单线程语义不变（as-if-serial），重排是合法的
- **CPU 重排** — x86 / ARM / RISC-V 的执行单元会乱序发射指令、store buffer 会延迟写入、cache line 在各核心之间通过 coherence 协议传播也有延迟
- **Cache 可见性** — 一个核心写入 L1 cache 的值，另一个核心可能在几十到几百周期后才看到

如果规范不做任何承诺，那么即使两个线程之间"写了再读"，读的一方也可能看到旧值，甚至看到"一半写入"的破损状态——这对任何跨线程协作的代码都是灾难。

ECMA-335 选择在规范层把这份承诺固定下来：runtime 必须保证某些操作的原子性，必须保证某些操作的可见性顺序，必须在 volatile / Interlocked 等机制上兑现具体的 fence 语义。这样 C# 代码才能有一份跨平台一致的并发契约——在 x86 上写的代码能在 ARM 上正确运行，在 CoreCLR 上写的代码能在 Mono / IL2CPP / HybridCLR 上得到相同的语义。

ECMA-335 的内存模型是一个中间强度的模型：

- 比 C++ 的 `memory_order_relaxed` 强——CLI 默认提供 acquire / release 级别的保证
- 比 Java 的 JMM 弱——Java 的 volatile write 是 sequentially consistent，CLI 的 volatile write 只是 release

这个"中间强度"定位是 CLI 设计上的一个明确取舍：在 x86（TSO 模型本身很强）上不需要额外开销，在 ARM（weak 模型）上通过显式 fence 指令补上保证。

## CLI 内存模型的核心承诺

Partition I §12.6.2 与 §12.6.4 定义了四项核心承诺。理解这四项承诺，就能理解 C# 并发代码背后到底发生了什么。

### Atomic reads / writes

规范保证"某些类型的读写是原子的"——也就是说，不会出现"读到一半、另一个线程写入导致读到破损数据"的情形。

原子读写的范围：

- **引用类型的读写** — 任意引用类型字段（object、string、T[]、class 实例）的赋值和读取都是原子的。背后原因是引用本质上是一个 native-size 指针（32 位平台 4 字节，64 位平台 8 字节），规范要求引用类型字段在内存中自然对齐
- **对齐的 primitive** — 4 字节及以下的 primitive（byte、short、int、float、char、bool）如果按自身大小对齐，读写是原子的。8 字节的 primitive（long、double）在 64 位平台对齐时原子；在 32 位平台上规范**不保证** long / double 的原子性

这意味着一个关键的工程结论：如果你在 32 位平台上对一个普通 `long` 字段做无锁的跨线程读写，可能读到"一半高位、一半低位"的破损 64 位值。要避免这种情形，必须用 `Interlocked.Read` / `Interlocked.Exchange` 之类的原子 API。

非对齐字段的读写规范不保证原子性。`[StructLayout(LayoutKind.Explicit)]` 强制把一个 int 字段放在非 4 字节对齐的偏移量上，runtime 不保证它在多线程下的原子性。

### Happens-before

规范定义了"happens-before"关系——如果操作 A happens-before 操作 B，那么 A 的所有副作用（写入、分配、状态修改）对 B 都是可见的。

- **同一线程内** — 按程序源码顺序，前面的语句 happens-before 后面的语句。这是 as-if-serial 语义的基础：单线程视角下一切看起来是顺序的
- **跨线程** — 没有显式同步，跨线程不建立 happens-before 关系。线程 A 的写入对线程 B 不保证可见

这是最容易被误解的地方——"我写完了，对方应该能看到"在 CLI 规范下不成立。必须通过显式同步原语（volatile 读写、lock / Monitor、Interlocked 操作、Task / Thread 的 join / wait）建立 happens-before。

### volatile 语义

volatile read 的语义是 **acquire** — 这个读之后的所有读写不会被重排到这个读之前。

volatile write 的语义是 **release** — 这个写之前的所有读写不会被重排到这个写之后。

组合起来的工程效果：一个线程按顺序做了"普通写 → volatile 写"，另一个线程按顺序做了"volatile 读 → 普通读"。如果第二个线程的 volatile 读看到了第一个线程的 volatile 写，那么第一个线程的普通写对第二个线程也可见。这是 acquire / release 配对建立跨线程 happens-before 的标准模式。

### locks

进入 lock 的语义是 **acquire**，退出 lock 的语义是 **release**。

C# 的 `lock (obj) { ... }` 在 IL 层面展开为 `Monitor.Enter(obj)` / `Monitor.Exit(obj)`。规范要求 `Monitor.Enter` 的语义是 acquire fence，`Monitor.Exit` 的语义是 release fence——这意味着 lock 块内的代码不会被重排到块外，且 lock 块之间通过共享锁对象建立跨线程 happens-before。

这四项承诺加起来就是 CLI 并发的契约基线。任何跨线程共享状态的代码必须基于这份契约设计，否则就是 undefined behavior。

## volatile 关键字的规范定义

Partition I §12.6.7 "Volatile reads and writes" 与 CIL 的 `volatile.` 前缀共同定义了 volatile 语义。这里要分清三件事：C# 的 `volatile` 关键字、CIL 的 `volatile.` 前缀、`Volatile.Read` / `Volatile.Write` 静态方法。

### CIL 层的 volatile 前缀

CIL 定义了一个 `volatile.` 前缀指令。它不是独立指令，而是修饰紧随其后的内存访问指令：

- `volatile. ldfld` / `volatile. ldsfld` — 带 acquire 语义的字段读取
- `volatile. stfld` / `volatile. stsfld` — 带 release 语义的字段写入
- `volatile. ldind.*` / `volatile. stind.*` — 带 fence 语义的间接读写

runtime（JIT / 解释器）看到 `volatile.` 前缀时，必须在生成的目标代码中插入对应的 fence 指令。在 x86 上 fence 通常是 no-op（TSO 模型已保证 acquire / release），在 ARM 上 fence 是 `dmb ish` 或等价指令。

### C# volatile 关键字

C# 的 `volatile` 字段修饰符是 CIL `volatile.` 前缀的语法糖。C# 编译器对一个 volatile 字段的每次读取生成 `volatile. ldfld` / `volatile. ldsfld`，每次写入生成 `volatile. stfld` / `volatile. stsfld`。

volatile 的重要限制：

- 不能用于 `long`、`double`、`decimal`、结构体等大于 native word 的类型（因为 primitive 原子性规范只对 native-size 以下有保证）
- 只能作为字段修饰符，不能用于局部变量或方法参数
- 不能保证复合操作原子性——比如 `volatile int count; count++;` 中 `++` 是"读 → 加 → 写"三个操作，每一步单独是 volatile 的，但整体不是原子的

### 常见误解：volatile 不是"禁用所有优化"

很多人把 volatile 理解成"禁用编译器优化、每次都回内存读写"。这种理解在 Java 和一部分 C++ 实现上勉强成立，在 .NET 上完全错误。

ECMA-335 的 volatile 只是 acquire / release 屏障：

- volatile read 之后的读写不会被重排到读之前（acquire）
- volatile write 之前的读写不会被重排到写之后（release）

它**不保证**：

- 两个线程看到 volatile field 的值是同一份（在 x86 上通常是，在 ARM 上需要配对的 acquire / release 才建立可见性）
- 每次读取都穿透 cache 直达 RAM（cache coherence 是 CPU 自动做的，与 volatile 无关）
- 多个 volatile 字段之间的更新顺序对其他线程可见（只对同一个字段的 read / write 配对建立顺序）

### Unit 参考类里的 volatile 用法

一个常见的工程例子：战斗系统的 `Unit` 类需要跨线程标记"已死亡"。

```csharp
public class Unit : IHittable {
    private int hp;
    private volatile bool isDead;   // 跨线程读 = acquire

    public virtual void TakeDamage(int amount) {
        hp -= amount;
        if (hp <= 0) {
            isDead = true;           // 跨线程写 = release
        }
    }

    public bool IsAlive() {
        return !isDead;              // 跨线程读 = acquire
    }
}
```

这个 pattern 的语义：

- 战斗线程调用 `TakeDamage` 把 `isDead` 写为 true，这是 release 写——之前的 `hp` 修改对其他线程也可见
- UI 线程或 AI 线程调用 `IsAlive()` 读 `isDead`，这是 acquire 读——如果读到 true，那么在战斗线程把 `isDead` 写为 true 之前的所有修改（包括 `hp`）对读方也可见

这里 `hp` 没有加 volatile，但通过 `isDead` 的 acquire / release 配对，`hp` 的值也建立了跨线程可见性——这就是 acquire / release 的威力。

要注意 volatile 不能解决"多字段一致性"。如果 Unit 同时要跨线程更新 `hp` 和 `isDead`，volatile 只保证读方看到 `isDead` 时也能看到 `hp`，但不保证"在 `isDead` 为 false 时看到的 `hp` 是哪一个版本"。后者必须用 lock 或 Interlocked 才能保证。

## Interlocked 的规范基础

Partition I §12.6.6 "Atomic operations" 定义了一组必须原子执行的复合操作。这组操作在 BCL 中以 `System.Threading.Interlocked` 类的静态方法暴露出来。

### 规范要求的原子复合操作

| Interlocked 方法 | 语义 | 硬件映射（典型） |
|------------------|------|----------------|
| `CompareExchange(ref T loc, T value, T comparand)` | 若 `loc == comparand`，则写入 `value`，返回原值 | x86 `LOCK CMPXCHG` / ARM `LDREX+STREX` |
| `Exchange(ref T loc, T value)` | 写入 `value`，返回原值 | x86 `XCHG` / ARM `LDREX+STREX` |
| `Increment(ref int loc)` | `loc++` 并返回新值 | x86 `LOCK XADD` / ARM `LDREX+ADD+STREX` |
| `Decrement(ref int loc)` | `loc--` 并返回新值 | x86 `LOCK XADD` / ARM `LDREX+SUB+STREX` |
| `Add(ref int loc, int value)` | `loc += value` 并返回新值 | x86 `LOCK XADD` / ARM `LDREX+ADD+STREX` |

这些操作的共同点是"读 → 修改 → 写"在硬件层面不可分割——不可能被中断、不可能被其他线程观察到中间状态。

### Interlocked 操作的 fence 语义

ECMA-335 规定：所有 Interlocked 操作同时提供 acquire + release 语义，相当于 full fence。

这意味着：

- Interlocked 操作之前的所有读写不会被重排到它之后（release）
- Interlocked 操作之后的所有读写不会被重排到它之前（acquire）

这个 full fence 语义比 volatile 更强——volatile 只有单向 fence。实际工程中，Interlocked 操作可以作为 lock-free 算法的同步点，担任 volatile + 原子性的双重角色。

### CompareExchange 的核心地位

在所有 Interlocked 操作中，`CompareExchange`（CAS，Compare-And-Swap）是最基础的原语——其他所有原子操作都可以用 CAS 循环实现。

```csharp
// 模拟 Interlocked.Increment 的 CAS 实现
public static int AtomicIncrement(ref int location) {
    int current, next;
    do {
        current = location;
        next = current + 1;
    } while (Interlocked.CompareExchange(ref location, next, current) != current);
    return next;
}
```

这个 CAS 循环（也叫 CAS spin）是 lock-free 数据结构的骨架。BCL 内部的 `ConcurrentQueue<T>`、`ConcurrentDictionary<T,K>`、`Lazy<T>` 等类型都大量使用 CAS 循环。

### 硬件实现差异

x86 的 `LOCK CMPXCHG` 是一条指令完成 CAS，语义清晰且性能可预测。ARM 的 `LDREX / STREX` 是 load-linked / store-conditional 模型——`LDREX` 读取并标记 cache line 为"exclusive"，后续对该 line 的任何其他访问都会让 `STREX` 失败。这种模型下 CAS 需要在循环中重试直到 `STREX` 成功，硬件实现更复杂但功耗更低。

runtime 负责把 `Interlocked.CompareExchange` 翻译成目标平台正确的指令序列。开发者不需要关心这个差异——规范保证语义一致。

## Memory Barrier 的三个层级

Partition I §12.6.4 定义了 memory barrier（fence）的规范行为。BCL 在这一基础上提供了三类 fence API，各自覆盖不同的语义需求。

### Thread.MemoryBarrier — full fence

`Thread.MemoryBarrier()` 是 full fence。它同时提供 acquire + release 语义，阻止所有重排：

- 调用之前的读写不会被重排到调用之后
- 调用之后的读写不会被重排到调用之前

在 x86 上通常映射为 `MFENCE` 或 `LOCK`-prefixed 指令；在 ARM 上映射为 `dmb ish` 全屏障指令。这是最重的 fence，也是语义最强的 fence。

典型用法：double-check locking 的初始化路径、lock-free 数据结构的发布点。

### Volatile.Read / Volatile.Write — 单向 fence

`Volatile.Read(ref T)` 与 `Volatile.Write(ref T, T)` 提供单向 fence：

- `Volatile.Read` 是 acquire fence — 等价于 `volatile.` 前缀的 load
- `Volatile.Write` 是 release fence — 等价于 `volatile.` 前缀的 store

它们相对 full fence 的优势是更轻——在 x86 上通常是纯 no-op（TSO 模型本身就保证 acquire / release），在 ARM 上也只需要单向 fence 指令。

为什么需要 `Volatile.Read` / `Volatile.Write` 而不是直接用 `volatile` 字段？两个场景：

- 字段类型本身不能加 `volatile`（比如 `long`、`double`、结构体）
- 同一个字段有时需要 volatile 访问，有时需要普通访问（`volatile` 字段修饰符是全局的，不能按调用点切换）

### Interlocked.MemoryBarrier — explicit full fence

.NET 5+ 引入了 `Interlocked.MemoryBarrier()`——语义与 `Thread.MemoryBarrier()` 相同，只是 API 命名上归类到 `Interlocked` 静态类里。这个新 API 的引入没有改变 CLI 规范，只是重新整理了 BCL 的命名空间组织。

### CIL 层面的 fence

ECMA-335 CIL 指令集里**没有**独立的 fence 指令。内存屏障语义完全通过以下两种方式表达：

- `volatile.` 前缀修饰具体的读写指令（`volatile. ldfld` 等）
- 调用 BCL 方法（`Thread.MemoryBarrier`、`Interlocked.*`），由 runtime 在方法内部插入 fence

这种设计让 CIL 指令集保持紧凑——fence 是内在于内存访问语义的一部分，而不是独立的操作。runtime 负责把这些高层语义翻译成目标平台的具体 fence 指令。

## 与 C++ / Java 内存模型的对比

三种主流托管 / 系统语言的内存模型各有不同。理解它们的相对位置有助于在跨语言协作时避免语义错配。

### C++

C++11 引入了显式的 `std::memory_order`：

- `memory_order_relaxed` — 只保证原子性，不提供任何重排约束
- `memory_order_acquire` — acquire 语义
- `memory_order_release` — release 语义
- `memory_order_acq_rel` — 同时 acquire + release
- `memory_order_seq_cst` — sequentially consistent（最强，默认）

C++ 的默认是 `seq_cst`（比 CLI 强），但可以显式降级到 `relaxed`（比 CLI 弱）。工程师需要自己选择合适的 memory_order。

### Java

Java Memory Model（JMM，JSR-133）定义的语义比 CLI 更强：

- volatile write 的语义是 sequentially consistent（不只是 release）
- volatile read 的语义也是 sequentially consistent（不只是 acquire）
- 所有 volatile 访问之间建立全局偏序

这意味着 Java 的 volatile 开销比 CLI 更大——在 ARM 上 Java 的 volatile store 必须用 `dmb ish` + 原子 store，而 CLI 只需要 `stlr`（release store）即可。

### .NET 的中间位置

.NET 的默认 volatile 是 acquire / release，比 C++ default 弱但比 C++ relaxed 强，比 Java 弱。这是一个工程取舍：

- 在 x86（TSO）上，acquire / release 天然成立，普通 load / store 即可，零开销
- 在 ARM（weak）上，acquire / release 只需要单向 fence（`ldar` / `stlr`），比 full fence 便宜
- 跨线程强可见性如果真的需要，再用 `Thread.MemoryBarrier` 或 `Interlocked` 显式请求

实际差异的直接后果：**.NET 的普通字段写不保证跨线程可见**。必须 volatile、lock、Interlocked 三者之一才能建立跨线程 happens-before。这与 Java 里"随便写个字段、大概率能被其他线程看到（虽然无保证）"的经验完全不同。

从 Java 迁到 .NET 的工程师最容易踩的坑就是这个——以为 .NET 的 volatile 和 Java 的 volatile 语义相同，实际上 .NET 的 volatile 更弱。

## 各 runtime 的内存模型实现差异

规范只定义了契约，具体实现由各 runtime 负责。不同 runtime 在不同硬件上的实现策略差异直接影响工程行为。

### CoreCLR

CoreCLR 完全遵循 ECMA-335 规范，在 x86 / x64 / ARM64 上都有正确的内存模型实现。

- JIT 编译器识别 `volatile.` 前缀，在 ARM64 上生成 `ldar` / `stlr`（acquire load / release store）指令
- Interlocked 操作映射到 `LOCK CMPXCHG`（x86）或 `LDXR / STXR`（ARM64）
- `Thread.MemoryBarrier` 在 ARM64 上是 `dmb ish`

从 .NET 5 起 CoreCLR 正式对 ARM64 Apple Silicon 做了完整支持。ARM64 弱内存模型下的 volatile 正确性是经过工程验证的——BCL 本身大量使用 volatile 字段、Interlocked 操作，任何语义错配都会被 CoreCLR 自己的测试覆盖出来。

### Mono

Mono 的内存模型历史上偏弱。早期版本在 ARM 上对 volatile 的实现不够完整——`volatile.` 前缀有时只生成普通 load / store，没有插入 fence。这导致一些 .NET 代码在 Mono ARM 上出现数据竞争 bug，但在 CoreCLR 上正常。

.NET 6 之后 Mono runtime 与 CoreCLR 合并到同一份代码库（`dotnet/runtime`），内存模型实现统一对齐到 ECMA-335。现在的 Mono 在 ARM 上也能生成正确的 fence 指令。

Unity 的 Mono Scripting Backend 基于较老的 Mono 分支，但 Unity 2022 LTS 以后的版本已经修复了大部分内存模型问题。跨平台 Unity 项目要特别注意：如果你在 Unity 编辑器（x86 Mono）测试通过的并发代码，需要在 ARM 真机（iOS / Android）上验证——x86 的 TSO 模型兜底可能掩盖数据竞争 bug。

### IL2CPP

IL2CPP 把 IL 转换为 C++ 代码，由 C++ 编译器编译成原生二进制。内存模型的实现路径：

- il2cpp.exe 识别 `volatile.` 前缀，在生成的 C++ 代码中使用 `std::atomic<T>::load(std::memory_order_acquire)` / `std::atomic<T>::store(std::memory_order_release)`
- Interlocked 操作翻译成 `std::atomic<T>::compare_exchange_*` 系列
- `Thread.MemoryBarrier` 翻译成 `std::atomic_thread_fence(std::memory_order_seq_cst)`

IL2CPP 实际上把内存模型的正确性委托给了 C++ 标准库 `std::atomic`。只要 C++ 编译器和标准库在目标平台的实现正确，IL2CPP 生成的二进制就有正确的内存模型行为。iOS 和 Android 的 Clang / libc++ 在 ARM 上对 `std::atomic` 的实现是经过 C++ 标准委员会和 LLVM 社区验证的，质量有保证。

### HybridCLR

HybridCLR 的解释器执行 IL 时，必须在 `volatile.` 前缀对应的字节码位置显式插入 fence 指令。这块实现在 HCLR-9（interpreter 基础）中有展开——解释器识别 volatile 前缀后，在执行字段读写前/后调用 `std::atomic_thread_fence` 或内联 ARM `dmb ish` 汇编。

对工程的影响：解释器的 fence 插入比 AOT 代码更贵。AOT 代码里 volatile 读可能是一条 ARM `ldar` 指令，解释器里的 volatile 读是"dispatch 到 volatile_ldfld handler → 执行 runtime call → 执行 fence → 执行 load"几十条指令。高频并发路径（比如每帧对大量 Unit 的 `isDead` 字段做 volatile 读）在 HybridCLR 上会比 IL2CPP 慢。

这是热更代码的一个性能陷阱：看起来只是加了 volatile 关键字，运行时成本在解释器路径上被放大几十倍。实际工程中，热更侧的 lock-free 代码通常要比 AOT 侧设计得更保守。

### LeanCLR

LeanCLR 是嵌入式 CLR（本系列后续专门展开），分 Universal 和 Standard 两个版本：

- **Universal 版** — 单线程执行模型，所有代码在宿主主线程上跑，不需要内存屏障。volatile 关键字在 Universal 版中是 no-op——因为没有并发，acquire / release 不会发挥作用。这是嵌入式场景的一个合理简化：MCU 上多数应用根本没有多核，强行保留 fence 只增加开销
- **Standard 版** — 规划中会支持多线程（FreeRTOS 或 bare-metal 多核场景）。这时内存屏障必须完整实现——ARM Cortex-M 是 ARMv7-M / ARMv8-M 架构，虽然单核下弱序影响有限，但多核 Cortex-A / R 需要完整的 `dmb` 指令。LeanCLR Standard 版对 ECMA-335 内存模型的完整兼容是多线程安全的硬性要求

LeanCLR 的设计让开发者能够在"不需要并发"的场景下关闭整套内存屏障机制，换取更小的二进制体积和更快的 VM 热路径。这与 CoreCLR / Mono / IL2CPP 的"始终按规范实现"策略是相反的工程取舍。

## 工程影响

内存模型在工程上的影响最容易被低估，因为 bug 常常以"偶现"形式出现——测试环境不复现、生产环境偶尔崩溃。下面是几个具体的工程陷阱。

### x86 开发陷阱

x86 的 TSO（Total Store Order）模型本身就保证了大部分 acquire / release 语义：

- 所有 store 按程序顺序对其他核心可见
- load 不会被重排到其他 load 之前（只有 load → store 这一种重排被硬件允许）

这意味着在 x86 上，即使你**不加** volatile、不加 lock，很多并发代码"看起来也能正常工作"。开发者会被这个现象误导：代码在本地 x86 Windows 开发机上跑通，在 ARM 的 iPhone / Android / Apple Silicon Mac 上就数据竞争、就崩溃。

这种 bug 的根因不是 ARM "有问题"——而是 x86 的强模型掩盖了代码的语义错误。跨平台项目必须建立"ARM 验证才是内存模型正确性测试"的意识。

### Unity 项目

Unity 项目在编辑器使用 Mono、构建 Player 使用 IL2CPP。两者的内存模型实现路径不同（Mono 的 JIT 生成 native code，IL2CPP 生成 C++ → native code），但对 ECMA-335 内存模型的承诺一致。

但实际工程中仍然有几个具体的坑：

- **Editor 跑得通不等于 Player 跑得通** — 编辑器是 x86 Mono，Player 可能是 ARM IL2CPP。x86 TSO 的兜底在 Player 上消失
- **Unity 自己的 Job System** — Unity 的 Job System 在 C# 侧跨线程传递数据，内部依赖 Burst 编译器的内存模型假设。如果 Burst 代码和普通 C# 代码通过共享 `NativeArray` 协作，必须理解 Burst 的内存模型语义（与 ECMA-335 有细微差异）
- **跨平台测试必要性** — 任何跨线程的战斗系统、AI 系统、网络代码，必须在 ARM 真机（iOS 低端机、Android 低端机）做压力测试

### HybridCLR 热更代码的 fence 成本

HybridCLR 解释器对 volatile 的 fence 插入成本显著高于 AOT 代码。工程建议：

- 热更代码避免在高频路径上用 volatile 字段（比如每帧几千次的 Unit 状态读取）
- 如果必须跨线程共享状态，优先考虑"每帧同步一次、本帧内本地副本"的模式——把 volatile 读降频到每帧一次
- 复杂的 lock-free 数据结构尽量保留在 AOT 侧（BCL 或主工程 DLL），热更只做业务逻辑

### double-check locking pattern

double-check locking 是经典的延迟初始化模式，在弱内存模型下必须正确使用 volatile：

```csharp
public class SingletonUnit {
    private static volatile SingletonUnit instance;
    private static readonly object gate = new object();

    public static SingletonUnit Instance {
        get {
            if (instance == null) {                 // 第一次 check（volatile read = acquire）
                lock (gate) {
                    if (instance == null) {         // 第二次 check
                        instance = new SingletonUnit();  // volatile write = release
                    }
                }
            }
            return instance;
        }
    }
}
```

关键点：`instance` 字段必须是 volatile。如果不是 volatile，在 ARM 上会出现"一个线程看到 instance 不为 null 但 `SingletonUnit` 对象的构造还没完成"的破损状态——另一个线程调用到一个字段还未初始化完毕的对象。

.NET 9 及之前版本的 BCL 推荐替代方案是 `Lazy<T>`。`Lazy<T>` 内部已经正确实现了 double-check locking 的内存屏障细节，C# 代码只需要调用 `lazy.Value` 就能得到正确的延迟初始化语义。手写 double-check locking 是易错代码——只要不是性能极限场景，都应该用 `Lazy<T>` 替代。

## 收束

内存模型是 ECMA-335 最容易被忽略的部分。大部分 .NET 代码在 x86 上"看起来正常"是因为 TSO 模型兜底——加没加 volatile、加没加 lock，大部分时候都不会出 bug。但跨到 ARM / Apple Silicon / 真实多核场景，或者跨到 HybridCLR 解释器的 fence 插入路径上，任何语义错误都会暴露。

理解规范层的 volatile / Interlocked / barrier 三层定义，才能写出真正可移植的并发代码：

- volatile 是 acquire / release 单向 fence，不是"禁用所有优化"
- Interlocked 是原子操作 + full fence 的组合，比 volatile 语义更强
- `Thread.MemoryBarrier` / `Volatile.Read` / `Volatile.Write` / `Interlocked.MemoryBarrier` 是对规范层 fence 语义的 BCL 封装，按需选择最轻的一种

.NET 的内存模型比 C++ 强（默认 acquire / release），比 Java 弱（volatile 不是 seq_cst）。这个中间位置既允许在 x86 上零开销，又要求开发者在跨平台时对弱内存模型有明确认知。跨平台项目没有捷径——本地 x86 通过不代表 ARM 通过，必须在目标硬件上验证。

至此 ECMA-335 基础层的 13 篇文章覆盖了 metadata、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享、verification、custom attribute、P/Invoke、security、threading memory model 这一整套规范契约。下一篇展开 CLI File Format——PE 文件与 CLI Header 的物理结构，把前面所有章节的规范语义与具体二进制字段对应起来。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/ecma335-cli-security-strong-name-cas.md" >}}">A11 CLI Security 模型</a>
- 下一篇：<a href="{{< relref "engine-toolchain/ecma335-cli-file-format-pe-cli-header.md" >}}">A13 CLI File Format</a>
