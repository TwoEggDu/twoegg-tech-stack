---
title: "底层硬件 F03｜SIMD 指令集：向量宽度、自动向量化与 Burst 的能力边界"
slug: "hardware-cpu-f03-simd"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "从 SSE、AVX 到 ARM NEON，区分向量宽度、指令吞吐和实际加速比，解释 Burst 自动向量化的条件、语言支持边界与验证方法。"
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
一条 `VADDPS ymm0, ymm1, ymm2` 指令可以表达 8 个 float 的逐元素加法。但这不代表它在所有 CPU 上都只用一个周期，也不代表程序就能快 8 倍。指令处理多少数据、执行单元多久能接收下一条指令、整个循环跑多快，是三个不同的问题。

理解这层区别，才能判断数据布局是否给了 SIMD 发挥空间，以及 Burst 实际利用了多少。

> 版本与证据：Burst 部分按 1.8 文档核对；该文档入口本次显示为 1.8.31。具体目标选项需以项目安装版本为准。下面的代码用于说明机制，尚未在 Unity 中完成编译、汇编对照和设备性能测试。

---

## 标量 vs 向量：硬件层的根本差异

**标量运算**以单个数据元素为单位。例如标量浮点加法读取两个输入，产生一个浮点结果。

```asm
; 标量：一条指令处理 1 个 float
ADDSS xmm0, xmm1        ; xmm0[0] = xmm0[0] + xmm1[0]，其余位不变
```

**向量运算**是 SIMD 的形态：一条指令，多个操作数打包在一起，同时产生多个结果。

```asm
; 向量：一条指令处理 8 个 float（AVX，256-bit）
VADDPS ymm0, ymm1, ymm2
; ymm0[0..7] = ymm1[0..7] + ymm2[0..7]
; 表达 8 个加法；周期数取决于具体 CPU 和依赖关系
```

SIMD 的全称是 **Single Instruction, Multiple Data**（单指令多数据）。指令集规定了多个数据通道（lane）的运算语义，具体硬件可以用宽执行单元实现，也可以把一条宽指令拆成较窄的内部操作。不能由 256-bit 寄存器直接推断出 8 个独立浮点单元及其周期数。

这和多核并行不同。多核是多套完整的 CPU 流水线，各自独立调度。SIMD 是单个核心内部的数据级并行——一条架构指令描述多个元素的同类运算；内部解码、执行和访存可能仍涉及多个微操作。

---

## 向量宽度演进史

x86 的 SIMD 扩展既增加过宽度，也扩充过同一宽度下的运算能力：

| 指令集 | 寄存器宽度 | float32 通道数 | 关键区别 |
|--------|-----------|--------------|---------|
| SSE | 128-bit | 4 | 提供打包单精度浮点运算；IA-32 下有 8 个 XMM，64 位模式下有 16 个 |
| SSE2 | 128-bit | 4 | 扩充 XMM 整数运算并增加双精度支持；XMM 数量增加不是 SSE2 本身的特性 |
| SSE4.1 / SSE4.2 | 128-bit | 4 | 分别增加点积、混合选择及字符串比较等指令，不增加向量宽度 |
| AVX | 256-bit | 8 | YMM 寄存器和三操作数形式，已支持 256-bit 浮点加法 |
| AVX2 | 256-bit | 8 | 扩展 256-bit 整数等操作；FMA 是另一个独立的指令集特性 |
| AVX-512 | 512-bit | 16 | ZMM、掩码及多个功能子集；需要核对具体 CPU 和所用子集 |

这些是指令能力，不能直接当成加速倍数。指令语义和特性检测可查 [Intel 架构手册及优化手册入口](https://www.intel.com/content/www/us/en/developer/articles/technical/intel-sdm.html)。

ARM 侧也需要区分固定宽度和可伸缩向量：

| 指令集 | 向量宽度 | float32 通道数 | 备注 |
|--------|---------|--------------|------|
| NEON / Advanced SIMD | 128-bit | 4 | 本文讨论 128-bit 浮点向量形式；FP16 算术还需对应扩展 |
| SVE / SVE2 | 128～2048-bit，按 128-bit 递增 | 4～64 | 可用向量长度依硬件与执行环境而定；SVE2 是进一步扩展，不能由 Armv8.2 版本号推定已支持 |

宽度关系见 [Arm 对 Neon、SVE 和 SME 的说明](https://developer.arm.com/community/arm-community-blogs/b/architectures-and-processors-blog/posts/matrix-matrix-multiplication-neon-sve-and-sme-compared)。

**FMA（Fused Multiply-Add）**把 `a * b + c` 按融合乘加语义计算，中间乘积不单独舍入，最终结果舍入一次。因此它可能与分开乘、加的结果不同。按通常 FLOP 计数，每个通道的 FMA 计两次浮点运算；这不等于程序吞吐自动翻倍，标量也可以使用 FMA。

Burst 的 x64 构建目标有多个选项，不能统一称为“默认 AVX2”。还要区分 **ISA 特性**与**编译器目标名称**：Burst 1.8.31 名为 AVX2 的目标组合包含 FMA 等额外特性，不表示 FMA 属于 AVX2 指令集。最终代码取决于构建配置、运行时分派及 CPU / OS 支持，见 [Burst 平台和目标说明](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/building-projects.html)。

---

## 理论峰值：为什么向量宽度不等于加速比

先只数一条指令表达的浮点工作量：

| 运算形式 | 元素数 | FLOP / 指令 |
|---------|-------|-------------|
| 标量加法 | 1 | 1 |
| 128-bit float32 加法 | 4 | 4 |
| 256-bit float32 加法 | 8 | 8 |
| 256-bit float32 FMA | 8 | 16 |

要从这个表推算每秒算力，还需要指令吞吐、执行单元数量和实际频率。**延迟**表示一条指令的结果多久可被依赖者使用；**吞吐**表示独立指令能以多快的速度持续执行，两者不能混用。比较加速比时，标量与向量版本也必须完成同样的工作。

实际循环还受三类条件约束：

**1. 内存带宽限制（Memory-Bound）**

向量化不必增加完成同一任务的总数据量，但会提高计算单元消费数据的速率。如果缓存或主存供数跟不上，执行单元仍会等待。因此应结合访问局部性和 [F04 的带宽模型]({{< relref "performance/hardware-cpu-f04-memory-bandwidth.md" >}}) 分析。

**2. 循环开销（Loop Overhead）**

向量化可能增加别名检查、对齐处理和尾部处理；具体目标也可能用非对齐访问或掩码完成。数据量小时，这些开销会稀释收益。

**3. 依赖链（Dependency Chain）**

如果向量指令之间存在数据依赖（上一条的输出是下一条的输入），能同时推进的独立工作减少，宽执行单元也未必能跑满。

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

这里还隐含一个条件：输出与输入之间没有造成跨迭代依赖的内存重叠。只看下标相同并不足以证明这一点，因为不同指针可能指向同一块内存。条件成立且成本合适时，编译器才可能批量加载、计算和写回；宽度由目标与优化决策决定。

**需要额外分析：循环携带依赖（Loop-Carried Dependency）**

```c
// 直接写法中，result[i] 依赖前一轮结果
for (int i = 1; i < N; i++) {
    result[i] = result[i-1] + a[i];
}
```

直接按这个计算顺序执行时，第 i 次迭代依赖第 i-1 次。这是**前缀和（Prefix Sum）**问题，可以改用并行扫描算法，但不能把普通逐元素向量化直接套上去；浮点版本还要检查改写计算顺序是否改变结果。也不能据此认为所有循环携带依赖都无法向量化，例如归约有专门的处理方式。

**条件分支：部分可向量化，部分不能**

```c
// 简单的双路分支：SIMD 可用 blend/mask 指令处理
for (int i = 0; i < N; i++) {
    result[i] = (a[i] > 0) ? a[i] * 2.0f : -a[i];
}
```

这里可能用比较加 blend（混合选择），或目标支持的掩码执行处理。前者可能计算两条路径再选择，后者不必等价于两路全部执行。编译器需要保留副作用与异常语义，并判断转换是否划算；存在分支或函数调用本身并不等于不能向量化。

```c
// 需要分别分析数学函数支持和输出下标依赖
for (int i = 0; i < N; i++) {
    if (a[i] > threshold) {
        result[i] = expf(a[i]);   // 是否有可用的向量实现、是否依赖 errno 等语义
        sideEffectBuffer[count++] = i;  // count 使输出位置依赖此前命中的元素数
    }
}
```

这里更难的是可变输出位置，可能需要压缩写入或算法改写。`expf` 并非物理上不能并行，是否能自动向量化取决于编译器、数学库和浮点选项。以上条件可对照 [LLVM 自动向量化文档](https://llvm.org/docs/Vectorizers.html)；LLVM 的通用能力不等于某个 Burst 版本一定会采用相同变换。

---

## Burst 编译器：Editor 即时编译与 Player AOT

Burst 使用 LLVM 后端编译受支持的 C# 子集：在 Editor 中按需即时编译，在 Player 构建时进行 AOT 编译。Editor 默认异步编译时，等待期间可能先执行托管版本，因此首次运行不能直接用来代表 Burst 性能。见 [Burst Play mode 编译说明](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-synchronous.html)。

也不能把 Mono / IL2CPP 概括为“绝不生成 SIMD”：最终指令受运行时、原生编译后端、代码与选项影响。Burst 的支持范围和优化信息更有针对性，但收益仍需对照生成代码与测量。

核心流程：

```
C# IL → Burst 前端分析 → LLVM IR → LLVM 优化 Pass → 目标平台机器码
                                        ↑
                              自动向量化在这里发生
                              （循环向量化 / SLP 向量化等）
```

循环向量化分析多次迭代能否一起处理；SLP 向量化则尝试合并同一代码区域中的相似标量操作。依赖、别名、访问方式和目标成本都会影响决策。最终可能生成 x86 或 ARM 向量指令，也可能保留标量代码。

### Burst 的限制从哪里来

Burst 的限制首先是**编译器支持的语言与运行时边界**，不能全部归因于 SIMD 的物理要求。

**托管对象与数组**

Burst 一般不支持在编译代码中使用托管对象和普通托管数组，但原因不是 `float[]` 元素不连续：其元素本身是连续存储的。对象生命周期、GC 和运行时功能支持是另一层问题。`NativeArray<float>` 提供非托管连续存储及访问约定，利于分析，却不保证向量化。Burst 还有限制性地支持静态只读托管数组，见 [类型支持说明](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-type-support.html)。

**静态字段**

“不能用任何 static”也不准确。[静态只读字段](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-static-read-only-support.html)在规定条件下受支持；共享可变数据有专门的 [SharedStatic](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-shared-static.html) 机制。但可编译不代表并发安全，共享写入仍需要正确的同步。

**异常处理**

不能在 Burst 中依赖普通托管 `try/catch` 恢复流程。Burst 支持 `try/finally` 等部分结构，但异常路径的行为与 .NET 不同。这是支持范围和运行时语义的约束，不能推导成“所有含异常结构的代码都不可能向量化”。具体边界见 [HPC# 支持说明](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-hpc-overview.html)。

---

## 一个 Burst Job 定义示例

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
        // 是否向量化、是否融合成 FMA，需检查目标代码和浮点设置
        Result[i] = math.mad(A[i], B[i], C[i]);
    }
}
```

这个定义假设四个数组覆盖相同的有效下标范围，且输出与输入没有产生依赖的重叠。调用方还需要负责分配、初始化、`Schedule`、`Complete` 和释放。

`IJobParallelFor` 要求开发者保证迭代之间独立；接口不能证明任意用户代码都满足这一点。此示例给了编译器批量处理的机会，但不是“每次必定合并 8 个 Execute”的保证，`math.mad` 也不代表所有配置下都融合为 FMA。

可以在 [Burst Inspector](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/editor-burst-inspector.html) 选择目标并看热点循环：确认是打包运算、如何加载和处理尾部，而不只是搜索 `ymm`。`xmm` 同时用于标量与向量指令，寄存器名本身不能证明整个循环已向量化。

---

## 移动端 NEON：宽度之外还要看什么

128-bit NEON 一次可表达 4 个 float32 运算。若处理器具备相应 FP16 算术扩展，才可使用 8 个 float16 通道的算术指令；“支持 NEON”本身不足以推出这一点。FP16 存储、转换与算术能力也应分别核对，见 [Arm 的 Neon / FP16 扩展说明](https://developer.arm.com/documentation/101028/0009/Advanced-SIMD--Neon--intrinsics)。

不能从向量更窄推导出移动端性能更稳定、数据集一定更小或不会热降频。持续性能还取决于频率、供数能力、整机负载和温控。GPU、NPU 的 FP16 吞吐也不能用来证明 CPU NEON 的性能。

Burst 支持的 ARM 目标会随版本扩展，应检查安装版本的平台表与构建设置，并在目标设备验证。不能仅凭使用 `Unity.Mathematics` 的矩阵 API 就断言调用了 Apple AMX，也不能用指令集名称替代生成代码证据。

---

## SoA 布局：SIMD 的天然搭档

连续存储和相同操作常让向量化更划算，但 SIMD 也能通过 gather、重排等方式处理部分非连续访问。以跨实体读取位置的 x 分量为例：

**AoS（Array of Structures）——跨实体的同一分量不连续：**

```
内存布局：[x0 y0 z0 | x1 y1 z1 | x2 y2 z2 | ...]
取 8 个 x：需要跳跃读取，步长 = sizeof(float3) = 12 bytes
```

目标支持时可以考虑 gather，也可能连续加载后再重排；代价取决于访问分布、缓存和指令实现，没有通用的 4～5 倍结论。如果计算同时使用 x、y、z，AoS 也可能适合向量化。

**SoA（Structure of Arrays）——SIMD 友好：**

```
内存布局：[x0 x1 x2 x3 x4 x5 x6 x7 | y0 y1 y2 ... | z0 z1 z2 ...]
取 8 个 x：一条 VMOVUPS ymm0, [ptr]，连续 256-bit 读取
```

ECS Chunk 按组件类型组织数组，但**组件数组连续，不等于组件内部每个字段分别连续**。若组件存的是紧凑 `float3`，其 x 分量仍然跨 12 字节分布，不能直接当成上面的独立 x 数组。

组件级拆分、字段级拆分与 AoSoA 是不同的布局选择，详见 [F06：数据布局]({{< relref "performance/hardware-cpu-f06-data-layout.md" >}})。布局能减少无用读取或重排，Burst 再把可利用的规律编译成代码；是否更快取决于实际访问的字段和工作量。

---

## 加速比如何验证

没有对应的设备、版本、代码与计时方法，就不把“Burst 快 10～100 倍”或“单独向量化快 3～6 倍”当成通用参考。应拆开三个问题：

| 对照 | 能回答什么 | 不能直接归因为什么 |
|------|-----------|------------------|
| 同一任务，Burst 关闭 / 开启 | 该配置下编译路径改变后的整体收益 | 纯 SIMD 收益 |
| 同一后端、单线程下的标量 / 向量版本 | 在控制算法、数据与浮点语义后的向量化收益 | 多核扩展收益 |
| 相同计算内核，不同 Worker / 批次配置 | 并行调度和负载分配的效果 | 单条指令变快 |

先检查结果正确性，再排除编译、分配和预热时间；记录数据量、缓存工作集、CPU、构建目标与安全检查设置。短循环计时与整帧收益分别报告。若没有可控的标量基线，就只报告整体优化收益，不把它拆成未经验证的 SIMD 倍数。

<!-- DATA-TODO: 补 MultiplyAddJob 在固定 Unity/Burst 版本和 x64/ARM64 设备上的生成代码、浮点设置、输入规模、结果误差和计时记录；当前未完成 Unity 运行验证，不填写性能倍数。 -->

---

## 写 Burst Job 时，如何检查向量化机会

**有利条件：**

- 迭代独立，热点数据连续或访问模式规则，工作量足以摊薄准备和尾部开销。
- 编译器能判断别名关系，读写声明与真实访问一致。
- 运算与被调用方法处于 Burst 支持范围，目标有合适的向量实现。

**需要核对的边界：**

- 原地更新同一数组不必然阻止向量化；关键是是否有跨迭代依赖，而不是单看 `[ReadOnly]` 是否存在。
- `NativeArray` 不是唯一可用容器，指针和其他受支持的非托管结构也可能向量化。
- 被调用方法不必逐一标记 `[BurstCompile]` 才能被内联；应检查入口及其可达代码是否受支持。
- 分支、非单位步长或数学函数调用是分析条件，不是统一的否决项。

验证应同时看**热点循环的生成指令、结果正确性和实际耗时**。汇编说明编译器做了什么，计时说明它在该设备、该工作量下是否值得。

---

SIMD 提供的是数据级并行机会。数据布局、算法依赖和编译配置决定能否利用它，供数和执行资源决定实际收益。Burst 可以自动完成许多变换，但工程判断仍要落在具体生成代码与可复现测量上。
