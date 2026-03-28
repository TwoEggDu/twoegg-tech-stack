---
title: "Unity DOTS E13｜Burst 编译规则全景：什么代码能过、限制来自哪里、常见报错原因"
slug: "dots-e13-burst-compiler"
date: "2026-03-28"
description: "Burst 不是魔法，它是受约束的 LLVM。理解 Burst 的限制来自 SIMD 向量化和 Native 内存模型的物理前提，才能写出能过 Burst 编译的代码，而不是靠试错绕过报错。"
tags:
  - "Unity"
  - "DOTS"
  - "Burst"
  - "LLVM"
  - "SIMD"
  - "性能优化"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 13
weight: 1930
---

## Burst 是什么

很多人第一次接触 Burst 时的直觉是"给 Job 加个 `[BurstCompile]` 就快了"。这个直觉并没有错，但它掩盖了一个关键事实：Burst 不是一个神秘的加速开关，而是一个**有明确约束的编译器前端**。

Burst 本质上是 Unity 封装的 LLVM 工具链。它的工作流程是：读入你的 C# IL（中间语言），将其翻译成 LLVM IR，再由 LLVM 后端生成目标平台的 Native 机器码。整个过程在**编译期或 Editor 里的异步编译阶段**完成，而不是在运行时——这一点与 Mono JIT 和 IL2CPP 都不同。

正因为是静态编译，Burst 才能做到两件 JIT 做不到的事：

1. **SIMD 自动向量化**：LLVM 可以静态分析数据依赖关系，将标量循环重写成 AVX2/NEON 向量指令（`vaddps`、`vmulps` 等），单条指令处理 4 或 8 个 float。
2. **无 GC 开销**：生成的代码直接操作 Native 内存，没有 GC 分配，没有 write barrier，没有 stop-the-world。

但这两件事是有前提的。Burst 的所有限制，本质上都是这两个前提的衍生结果。

---

## 限制的本质来源

理解 Burst 限制，先要理解两条物理约束：

**约束一：Native 内存与 GC 的隔离**

Burst 编译的代码运行在 Native 内存空间。GC 只能追踪 Managed Heap 上的对象引用——它对 Native 内存里的指针一无所知。如果 Burst 代码持有一个指向 Managed 对象的引用，GC 可能在任意时刻移动或回收那个对象，而 Burst 代码不会收到任何通知。结果是野指针。

因此，所有**GC 引用类型**（class 实例、delegate、string 等）都不能出现在 Burst 代码路径中。

**约束二：SIMD 向量化要求静态可分析的数据流**

LLVM 的向量化 Pass 需要对循环的数据依赖做静态分析。虚函数调用和接口调用会在运行时 dispatch 到未知目标，LLVM 无法追踪其副作用，无法确认是否存在内存别名，向量化分析就此中断。

因此，**动态分发（virtual dispatch、interface dispatch）** 在 Burst 中受到严格限制。

Burst 允许的 C# 子集，就是这两条约束的交集：**没有 GC 引用、没有动态分发、没有依赖 Managed 运行时的任何机制**。

---

## 不能用的东西（以及为什么）

### class 实例

```csharp
// 错误：Job struct 里不能有 class 字段
public struct MyJob : IJob {
    public MyClass data; // Managed type found in job struct
}
```

`class` 对象活在 GC Heap，Job 被调度到工作线程时，整个 struct 会被复制到 Native 内存。GC 无法追踪这个复制出去的引用，导致悬空指针风险。修复方向：将数据改成 `NativeArray` 或 `BlobAssetReference<T>`。

### 装箱（boxing）

```csharp
int x = 42;
object obj = x; // 装箱：在 GC Heap 分配一个 object
```

装箱在 GC Heap 上分配内存，产生 GC 引用。Burst 无法容忍这一点。常见触发场景：将值类型传给接受 `object` 参数的方法（如旧式 `Dictionary` 或未用泛型约束的工具函数）。

### 虚函数和接口方法调用

```csharp
public interface IProcessor { void Process(); }
public struct MyJob : IJob {
    public IProcessor processor; // 接口字段 → 装箱 + 动态分发
    public void Execute() { processor.Process(); } // 不确定能否过 Burst
}
```

接口字段本身就是装箱引用。即便某些情况下 Burst 能内联已知的 struct 实现，这依赖编译器的静态分析能力，不保证在所有调用路径上成立，不应作为可靠模式。正确做法是用泛型约束 + `where T : struct, IProcessor`，让 Burst 在编译期确定具体类型。

### try/catch / throw

异常机制依赖 Managed 运行时的 stack unwinding 和异常对象分配，这套机制在 Native 代码中根本不存在。Burst 遇到 `throw` 语句会直接报错：`Not supported: Throw`。错误处理应改为返回错误码或使用 `Assert`（仅 Debug 模式有效）。

### string

`string` 是 Managed 引用类型，内部是 GC Heap 上的 char 数组。替代方案是 `Unity.Collections` 提供的 `FixedString32Bytes`、`FixedString128Bytes` 等，它们是值类型，存储在栈或 Native 内存中。

### static 可变字段

```csharp
// 错误
public static int counter = 0; // 多线程写入，无同步
```

Burst Job 可能在多个工作线程上并行执行。可变静态字段没有线程安全保障，Burst 拒绝编译。常量（`static readonly` + 编译期可确定的值）在部分情况下可以通过，但需要谨慎验证。

### 托管 API（DateTime.Now、File.ReadAllText 等）

这类 API 内部依赖 Managed 运行时服务，Burst 无法调用。需要在 Job 外部准备好数据，通过 NativeArray 或 BlobAsset 传入。

---

## 可以用的东西

**Unity.Mathematics 数学库**：`float3`、`float4x4`、`math.sin()` 等函数专为 Burst 设计，LLVM 能直接将其向量化为 SIMD 指令。优先使用它，而不是 `UnityEngine.Mathf`（后者在 Burst 中可用但向量化效果差）。

**NativeContainer 系列**：`NativeArray<T>`、`NativeList<T>`、`NativeHashMap<TKey, TValue>`、`NativeQueue<T>` 等，底层是 Native 内存，GC 不管理其内容，完全兼容 Burst。

**FixedString 系列**：值类型字符串，支持 UTF-8，可用于日志和调试场景。

**unsafe 指针操作**：Burst 完全支持 `unsafe` 块和指针算术，这是性能敏感路径的重要工具。

**BlobAssetReference\<T\>**：用于只读共享数据（技能表、配置表等），Native 内存，零运行时分配。

---

## 常见报错速查

| 报错信息 | 原因 | 修复方向 |
|---|---|---|
| `Managed type 'XXX' found in job struct` | Job struct 有 class 字段 | 改用 NativeArray / BlobAssetReference |
| `Not supported: Throw` | 代码路径包含 throw 或 try/catch | 改为错误码返回 |
| `Static readonly fields are not supported` | 非编译期常量的静态字段 | 改为 `const` 或通过参数传入 |
| `An interface method call has been found` | 接口动态分发 | 改用泛型约束 `where T : struct, IInterface` |
| `Boxing a non-primitive value type` | 值类型被装箱 | 移除 object 参数，改用泛型 |
| `The type 'System.String' is not blittable` | 使用了 string | 改用 FixedString128Bytes |

---

## Burst Inspector 的使用

Burst Inspector 是确认向量化是否真正发生的唯一可靠方式。路径：**Window → Burst → Open Inspector**。

打开后，左侧列表会显示所有带 `[BurstCompile]` 的 Job 和函数指针。选中目标后，右侧会显示生成的汇编代码。

**如何确认 SIMD 向量化**：在汇编中搜索以 `v` 开头的 AVX/SSE 指令，例如：

- `vaddps`：4 个 float 并行加法（SSE）或 8 个（AVX2）
- `vmulps`：并行乘法
- `vfmadd`：融合乘加（FMA）

如果只看到 `addss`（标量），说明向量化没有发生，需要检查循环中的数据依赖是否阻止了向量化（常见原因：循环迭代间存在写后读依赖）。

**性能对比**：Inspector 顶部可以切换 Burst Enable/Disable，配合 Unity Profiler 的 Timeline 视图，可以直观看到同一段代码 Burst 开关前后的耗时差异。

---

## `[BurstCompile]` 的调优选项

```csharp
[BurstCompile(
    FloatMode = FloatMode.Fast,
    FloatPrecision = FloatPrecision.Standard,
    OptimizeFor = OptimizeFor.Performance
)]
public struct MyJob : IJob { ... }
```

**FloatMode**

- `FloatMode.Strict`（默认）：严格遵守 IEEE 754，结果可重现，但 LLVM 无法重排 FP 运算。
- `FloatMode.Fast`：允许 LLVM 重排浮点运算顺序（如 `a + b + c` 可被重写为 `(a + c) + b`），解锁 FMA 融合和更激进的向量化，通常带来 10-30% 的提升。代价是浮点结果可能与 Strict 模式有微小差异，`NaN` 传播行为也可能改变。物理模拟等对精度敏感的模块慎用。

**FloatPrecision**

- `FloatPrecision.Standard`：`sin`/`cos` 等超越函数使用全精度实现。
- `FloatPrecision.Low`：使用近似多项式实现，误差约 1e-3 量级，速度更快。粒子、特效等视觉类计算可以考虑。

**OptimizeFor**

- `OptimizeFor.Performance`（默认）：LLVM 以最大性能为目标，允许代码膨胀（循环展开、函数内联）。
- `OptimizeFor.Size`：限制展开和内联，减小生成代码体积。移动平台 icache 压力大时可以考虑，但通常有 5-15% 的性能代价。

---

## 小结

Burst 的限制不是任意设定的禁令，而是 Native 内存模型和 SIMD 静态分析两条硬约束的必然结果。每一条报错背后都有明确的物理原因：

- 凡是 GC 引用 → 不能进 Native 内存 → 必须替换
- 凡是动态分发 → 阻断 LLVM 分析 → 必须静态化
- 凡是 Managed 运行时服务 → Native 代码没有这层运行时 → 必须在 Job 外准备

能过 Burst 的代码，就是把这两条约束内化之后写出来的代码。这也是为什么 DOTS 的整个数据模型（ECS 组件是 struct、NativeContainer 管内存、BlobAsset 做只读共享）都是围绕这两条约束设计的——它们是配套的，不是偶然。

下一篇 E14 将进入 **NativeCollection 选型**：`NativeArray`、`NativeList`、`NativeHashMap` 在什么场景下各有优势，并发读写的安全边界在哪里，以及如何用 `Allocator` 类型控制内存生命周期。
