---
title: "底层硬件 F03｜SIMD 指令集：一条指令处理 8 个 float，Burst 背后在做什么"
slug: "hardware-cpu-f03-simd"
date: "2026-03-28"
description: "从 SSE2 到 AVX-512 再到 ARM NEON，解释 SIMD 的硬件原理和向量宽度演进，以及 Unity Burst 编译器如何通过自动向量化让同样的 C# 代码快 4~8 倍。"
tags:
  - "CPU"
  - "SIMD"
  - "AVX"
  - "NEON"
  - "Burst"
  - "向量化"
  - "性能基础"
  - "底层基础"
series: "底层硬件 · CPU 与内存体系"
weight: 1330
---
一条 `VADDPS ymm0, ymm1, ymm2` 指令，让 8 个 float 同时完成加法，耗时和一次普通 `ADDSS`（单精度标量加法）完全相同。这不是编译器的魔法，而是硬件里真实存在的并行执行单元。

理解这件事之后，你才能真正理解 Unity Burst 编译器的约束为什么存在——那些限制不是 Burst 团队的偏好，而是向量化执行的物理前提。

---

## 标量 vs 向量：硬件层的根本差异

**标量运算**是 CPU 执行的默认形态：一条指令，一个操作数，产生一个结果。

```asm
; 标量：一条指令处理 1 个 float
ADDSS xmm0, xmm1        ; xmm0[0] = xmm0[0] + xmm1[0]，其余位不变
```

**向量运算**是 SIMD 的形态：一条指令，多个操作数打包在一起，同时产生多个结果。

```asm
; 向量：一条指令处理 8 个 float（AVX2，256-bit）
VADDPS ymm0, ymm1, ymm2
; ymm0[0..7] = ymm1[0..7] + ymm2[0..7]
; 8 个加法，1 个时钟周期
```

SIMD 的全称是 **Single Instruction, Multiple Data**（单指令多数据）。硬件能做到这件事，原因很直接：**物理上有多个 ALU 并排工作**。一个 256-bit 的向量 ALU，内部实际上是 8 个 32-bit 的浮点 ALU，它们共享同一套控制逻辑，在同一个时钟周期内同时完成各自的计算。

这和多核并行不同。多核是多套完整的 CPU 流水线，各自独立调度。SIMD 是单个核心内部的数据级并行——同一条指令的解码、取操作数、写回，只发生一次，但数据处理宽度是标量的 N 倍。

---

## 向量宽度演进史

x86 的 SIMD 扩展从 1999 年开始积累，每隔几年增加一次宽度或指令集：

| 指令集 | 年份 | 寄存器宽度 | float32 并行数 | 关键新增 |
|--------|------|-----------|--------------|---------|
| SSE | 1999 | 128-bit | 4x | 首个浮点 SIMD，4 个 XMM 寄存器 |
| SSE2 | 2001 | 128-bit | 4x | 整数 SIMD，double 支持，16 个 XMM |
| SSE4.1/4.2 | 2007 | 128-bit | 4x | 点积、字符串指令、blendvps |
| AVX | 2011 | 256-bit | 8x | YMM 寄存器（256-bit），3 操作数格式 |
| AVX2 | 2013 | 256-bit | 8x | **FMA**（融合乘加），整数 256-bit 支持 |
| AVX-512 | 2017 | 512-bit | 16x | ZMM 寄存器，mask 寄存器，主要服务器/高端桌面 |

ARM 侧的 SIMD 走了另一条路：

| 指令集 | 宽度 | float32 并行数 | 备注 |
|--------|------|--------------|------|
| NEON (AArch32/64) | 128-bit | 4x | 所有现代 ARM Cortex-A 和 Apple Silicon 均支持 |
| SVE / SVE2 | 128~2048-bit（可变） | 4~64x | Armv8.2+，宽度由硬件实现决定 |
| Apple AMX | 不公开（推测 512-bit+） | — | Apple Silicon 专用矩阵扩展，加速 BLAS/ML 推理 |

AVX2 是今天 PC 游戏开发最重要的基准线：Intel Haswell（2013）和 AMD Ryzen（2017）之后的 CPU 均支持，Unity Burst 的默认 PC 目标就是 AVX2。

**FMA（Fused Multiply-Add）**值得单独说一句。`a * b + c` 这个操作在 FMA 下变成一条指令（`VFMADD`），不仅节省了一条指令，还**消除了中间舍入误差**——乘法结果保持全精度再和 c 相加。神经网络推理、物理模拟、蒙皮计算中大量出现乘加操作，FMA 使 AVX2 的实际吞吐量接近理论值的两倍。

---

## 理论峰值：为什么实际加速比不到 8x

纯计算密集型循环（所有数据在寄存器或 L1 缓存里）：

- 标量 ADDSS：1 float/cycle
- SSE ADDPS（128-bit）：4 float/cycle，**4x 理论上限**
- AVX2 VADDPS（256-bit）：8 float/cycle，**8x 理论上限**
- AVX2 VFMADD（FMA）：16 float/cycle（乘和加合并），**16x 理论上限**

实际场景中达不到理论值，主要有三个原因：

**1. 内存带宽限制（Memory-Bound）**

SIMD 每个时钟周期消耗的数据量是标量的 8 倍。如果数据在 L2/L3 缓存或主存，读取速度赶不上计算速度，ALU 就会等待。这就是为什么 cache-friendly 的数据布局和向量化必须配合使用——SIMD 扩大了对带宽的需求，不优化内存访问模式的向量化是残缺的向量化。

**2. 循环开销（Loop Overhead）**

向量化通常需要对循环做 peeling（处理头部不对齐的元素）和 remainder（处理尾部不足一个向量宽度的元素），这些额外代码会在数据量小时稀释收益。

**3. 依赖链（Dependency Chain）**

如果向量指令之间存在数据依赖（上一条的输出是下一条的输入），指令级并行度下降，后端执行单元空转。

---

## 什么样的代码能被向量化

自动向量化的编译器（包括 Burst 的 LLVM 后端）在分析循环时，需要满足一套条件才敢放心生成 SIMD 指令。

**可以被向量化的典型形态：**

```c
// 连续内存、相同操作、无循环间依赖
for (int i = 0; i < N; i++) {
    result[i] = a[i] * b[i] + c[i];
}
```

满足：无依赖（`result[i]` 只依赖同下标的输入）、连续内存、相同操作（乘加）。编译器可以每次读 8 个 float，做 8 次乘加，写 8 个结果。

**无法被向量化：循环携带依赖（Loop-Carried Dependency）**

```c
// result[i] 依赖 result[i-1]，必须串行
for (int i = 1; i < N; i++) {
    result[i] = result[i-1] + a[i];
}
```

第 i 次迭代必须等第 i-1 次完成才能开始，无法并行。这是**前缀和（Prefix Sum）**问题，有专用的并行算法，但普通自动向量化无法处理。

**条件分支：部分可向量化，部分不能**

```c
// 简单的双路分支：SIMD 可用 blend/mask 指令处理
for (int i = 0; i < N; i++) {
    result[i] = (a[i] > 0) ? a[i] * 2.0f : -a[i];
}
```

这里的分支可以用 SIMD 的 **mask（掩码）** 操作处理：同时计算两条路径的结果，再用比较结果作为掩码做 blend（混合）选择。但前提是两条路径的操作都没有副作用。如果分支内有函数调用、写外部状态或路径代价差异极大，向量化就会退化。

```c
// 无法向量化：分支内有复杂控制流或副作用
for (int i = 0; i < N; i++) {
    if (a[i] > threshold) {
        result[i] = expf(a[i]);   // 超越函数，无法并行化
        sideEffectBuffer[count++] = i;  // 写共享状态，有副作用
    }
}
```

---

## Burst 编译器：不是 JIT，是 AOT + LLVM

Unity 的 Mono/IL2CPP 编译器是通用 C# 编译器，不为 SIMD 做专门优化。**Burst 是独立的 AOT 编译器**，使用 LLVM 后端，在构建时将 `[BurstCompile]` 标记的 Job 编译成目标平台的原生代码。

核心流程：

```
C# IL → Burst 前端分析 → LLVM IR → LLVM 优化 Pass → 目标平台机器码
                                        ↑
                              自动向量化在这里发生
                              （Loop Vectorization Pass）
```

LLVM 的向量化 Pass 会分析 IR 中的循环结构：检查依赖关系、内存对齐、步长是否连续，然后决定是否生成 `ymm` 或 `xmm` 向量指令。Burst 在此基础上增加了额外的分析（例如针对 `NativeArray` 的对齐保证），让向量化可以更激进。

### Burst 的限制从哪里来

Burst Job 有一套众所周知的约束：不能用 managed 对象、不能访问 static 字段、不能用 try/catch、不能用普通 C# 数组。这些约束通常被解释成"Burst 的规定"，但背后的原因是**向量化和并行执行的物理前提**。

**不能用 managed 对象（class 实例、数组引用）**

向量化需要**连续的内存布局**。managed 对象由 GC 堆管理，地址不保证连续，对象头（object header）、GC 标记位散布其中。Burst 无法对 `new float[N]`（managed 数组）应用 SIMD 读取，因为它不知道内存是否连续对齐。`NativeArray<float>` 是固定地址的连续内存块，Burst 可以安全地生成宽度为 256-bit 的向量读取。

**不能用 static 字段**

Burst Job 设计为可以在 Worker Thread 上并行运行多个实例。static 字段是共享状态，多个向量化的 Job 同时写同一个 static 变量会产生数据竞争（Data Race），向量化引入的批量写入甚至可能在错误的时序上部分提交结果。

**不能用 try/catch**

向量化循环要求循环体的控制流是**规则的**（没有任意跳出点）。try/catch 引入了隐式的控制流跳转（异常表），LLVM 的向量化 Pass 无法分析这种结构，遇到就会放弃向量化。

这些约束不是 Burst 故意刁难开发者，而是向量化执行的物理前提的直接映射。

---

## 一个完整的 Burst Job 示例

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MultiplyAddJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> A;
    [ReadOnly] public NativeArray<float> B;
    [ReadOnly] public NativeArray<float> C;
    [WriteOnly] public NativeArray<float> Result;

    public void Execute(int i)
    {
        // a[i] * b[i] + c[i]
        // Burst 会把这个循环体向量化为 VFMADD256
        Result[i] = math.mad(A[i], B[i], C[i]);
    }
}
```

Burst 看到 `IJobParallelFor` 时，知道每个 `Execute(i)` 调用之间没有依赖。结合 `NativeArray` 提供的连续内存保证，它会把多个连续的 `Execute` 调用合并成向量指令：一次处理 8 个 i（AVX2）或 4 个 i（NEON/SSE），而不是每个 i 单独执行一次。

你可以在 Burst Inspector（Window > Burst Inspector）里看到实际生成的汇编，确认 `ymm` 寄存器是否出现在输出里。

---

## 移动端 NEON：更窄，但够用

ARM NEON 固定 128-bit，一次处理 4 个 float32 或 8 个 float16。相比 AVX2 的 256-bit，向量宽度只有一半，但移动端的数据集通常也更小，并且 NEON 的延迟和吞吐比 AVX2 更稳定（移动端 CPU 的热设计余量决定了不会有 AVX-512 那种降频风险）。

**float16 在移动端的重要性正在上升。** Qualcomm Adreno 和 Apple Neural Engine 的 ML 推理大量使用 float16，NEON 的 `FADD v0.8h, v1.8h, v2.8h`（8x float16，128-bit）在推理任务上的实际吞吐不输 PC 的 float32 路径。

Burst 会根据构建目标自动选择后端：
- PC（x64）：AVX2（默认），可降级到 SSE4 或 SSE2
- iOS/Android（ARM64）：NEON
- Apple Silicon Mac：NEON + AMX（通过 `Unity.Mathematics` 的 matrix 路径间接利用）

开发者不需要手动选择，但了解目标平台的向量宽度，有助于判断某段代码的理论加速上限。

---

## SoA 布局：SIMD 的天然搭档

SIMD 需要三样东西：**连续内存、相同类型、相同操作**。

ECS 的 Component 存储正好满足这三个条件。以位置更新为例：

**AoS（Array of Structures）——SIMD 不友好：**

```
内存布局：[x0 y0 z0 | x1 y1 z1 | x2 y2 z2 | ...]
取 8 个 x：需要跳跃读取，步长 = sizeof(float3) = 12 bytes
```

SIMD 的 gather 指令（分散读取）可以处理，但延迟比连续读取高 4~5 倍。

**SoA（Structure of Arrays）——SIMD 友好：**

```
内存布局：[x0 x1 x2 x3 x4 x5 x6 x7 | y0 y1 y2 ... | z0 z1 z2 ...]
取 8 个 x：一条 VMOVUPS ymm0, [ptr]，连续 256-bit 读取
```

ECS 的 Archetype Chunk 本质上就是 SoA：每种 Component 类型的所有实例连续存储在一起。当你写一个遍历 `Translation` 的 Burst Job，Burst 读取的是连续的 float3 数组，向量化读取每次可以装入 8 个 x 分量，然后 8 个 y，然后 8 个 z，计算完再写回。

这是 **DOTS + Burst 组合能有如此显著性能提升的根本原因**：不是单独某一个技术，而是 SoA 满足了 SIMD 的内存前提，Burst 把这个前提转化为实际的向量指令。

---

## 实际加速数据参考

Unity 官方在 DOTS 相关博客和 GDC 演讲中给出的数据（2019~2021 年，基于不同测试场景）：

- Burst 编译的代码 vs Mono：**典型范围 10x~100x**（包含 GC 消除、缓存优化、指令优化的综合效果）
- 单纯向量化的理论贡献：**AVX2 下 float32 最多 8x，FMA 再翻倍到 16x**
- 实测中向量化单独的贡献：通常 **3x~6x**（受内存带宽和循环开销影响）

这些数字适合作为量级参考，具体场景差异很大。真正有说服力的是用 Burst Inspector 验证汇编里确实有 `ymm` 指令，以及 Profiler 里的实际帧时间对比。

---

## 写 Burst Job 时，什么样的代码能触发向量化

给一个直接可用的检查清单：

**会触发向量化：**
- `IJobParallelFor` 的 `Execute(int i)` 体内，用 `NativeArray` 做连续读写
- 循环内操作是加、减、乘、除、FMA、min/max、比较
- `math.` 前缀的函数（`math.mad`, `math.dot`, `math.sqrt` 等）——这些有 Burst intrinsic 对应
- 循环体内没有函数调用，或调用的函数也是 `[BurstCompile]` 且是内联候选

**会阻止向量化：**
- 读写同一数组（`[ReadOnly]` 缺失，Burst 无法证明无别名）
- 循环内有分支，且分支路径有副作用（写外部状态）
- 使用 `NativeArray` 以外的数据容器（managed 数组、List）
- 调用了非 Burst 编译的方法（包含 virtual 调用的接口方法）
- 步长不为 1 的访问模式（`a[i * stride]`，Burst 有时能处理，但需要 stride 是编译时常量）

验证方式只有一个：打开 Burst Inspector，看汇编里是否出现 `ymm`（AVX2）或 `xmm`（SSE）寄存器操作序列，而不是一堆标量 `xmm0[0]` 操作。

---

SIMD 是 CPU 里最被低估的性能杠杆。相比多线程，它不涉及同步开销；相比换算法，它几乎是"免费"的——只要数据布局对，只要代码结构合规，编译器会替你做完向量化。Burst 的价值不是它能做什么神奇的事，而是它把"合规代码自动变成向量指令"这件事做得足够可靠，让你可以信任它，然后专注于写出让向量化有发挥空间的数据结构和算法。
