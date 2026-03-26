+++
title = "Shader 语法基础 04｜控制流与分支代价：GPU 里的 if 为什么危险"
slug = "shader-basic-04-control-flow"
date = 2026-03-26
description = "GPU 的 SIMD 架构决定了分支有独特的代价模型。理解 Warp Divergence，知道什么时候 if 是安全的，什么时候要用 step/lerp 替代，以及 UNITY_BRANCH 和 flatten 的区别。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "语法基础", "控制流", "分支", "性能"]
series = ["Shader 手写技法"]
[extra]
weight = 4080
+++

写 Shader 时，`if` 并不是完全禁止的——但它有独特的代价模型，必须理解才能用对。

---

## GPU 的执行模型：SIMD

CPU 一次执行一条指令，处理一个数据。GPU 的架构不同——它用 **SIMD（Single Instruction, Multiple Data）** 模式运行：**一批线程同时执行同一条指令，但各自处理不同的数据**。

这一批线程叫做 **Warp**（NVIDIA）或 **Wavefront**（AMD/Apple），通常是 32 或 64 个线程。

Fragment Shader 里，每个线程对应屏幕上的一个像素。32 个相邻像素组成一个 Warp，同时执行同一条指令。

---

## Warp Divergence：分支的真实代价

当 Warp 里的线程碰到 `if` 时：

```hlsl
if (someCondition)
{
    // 路径 A
}
else
{
    // 路径 B
}
```

如果同一个 Warp 里，部分线程走路径 A，部分走路径 B——它们无法"分开执行"，GPU 必须**串行执行两条路径**：

1. 先执行路径 A，需要路径 B 的线程**等待**（被 Mask 掉）
2. 再执行路径 B，需要路径 A 的线程**等待**

结果：同一个 Warp 里出现了分叉（Divergence），两条路径都要执行，总时间 = A + B，而不是 max(A, B)。

**Warp 里所有线程走同一路径时，没有代价损失**。问题只出现在同一 Warp 里有分叉时。

---

## 什么时候 if 是安全的

**条件是编译期常量**：编译器直接选一条路径，另一条消失。

```hlsl
#if defined(_FEATURE_ENABLED)
    // 编译进去
#else
    // 编译掉
#endif
```

**条件对整个 Draw Call 统一**（Uniform Branch）：所有线程都走同一路径，没有 Divergence。

```hlsl
if (_LightCount > 0)   // _LightCount 是材质属性，整个 Draw Call 的值相同
{
    // 所有像素都走这里，或都不走
}
```

GPU 驱动能识别这种 Uniform Branch，通常处理得很好。

**条件基于像素的值**（Non-Uniform Branch）：同一 Warp 里不同像素可能走不同路径——这是问题所在。

```hlsl
if (NdotL > 0.5)   // 每个像素 NdotL 不同，同一 Warp 里会分叉
{
    // ...
}
```

---

## step/lerp 替代分支

用数学函数把分支变成连续计算，消除 Divergence：

```hlsl
// ❌ 有分支（可能 Divergence）
half3 color;
if (NdotL > 0.5)
    color = litColor;
else
    color = shadowColor;

// ✅ 无分支，同等效果
float mask = step(0.5, NdotL);              // NdotL >= 0.5 时为 1
half3 color = lerp(shadowColor, litColor, mask);
```

两者的计算结果完全相同，但后者无条件执行全部计算，没有 Divergence。

**代价分析**：

- 有分支版本：可能串行执行两次（Divergence 时），可能执行一次（Uniform 时）
- 无分支版本：每次都执行两次路径的计算，但无等待开销

对于轻量计算（两次 `lerp`），无分支版本更快。对于重量计算（两段复杂代码），要具体分析。

---

## smoothstep 替代硬边分支

硬边效果（如卡通渲染的色阶）：

```hlsl
// ❌ if 版本
if (NdotL > threshold)
    color = litColor;
else
    color = shadowColor;

// ✅ step 版本（硬边）
float mask = step(threshold, NdotL);
half3 color = lerp(shadowColor, litColor, mask);

// ✅ smoothstep 版本（软边，更自然）
float mask = smoothstep(threshold - 0.05, threshold + 0.05, NdotL);
half3 color = lerp(shadowColor, litColor, mask);
```

---

## UNITY_BRANCH 和 flatten

HLSL 提供了两个 Shader 提示，影响编译器如何处理 `if`：

```hlsl
[branch]    // 告诉编译器：生成真正的分支指令
if (condition) { ... }

[flatten]   // 告诉编译器：把两条路径都展开执行，结果用 select 选择
if (condition) { ... }
```

URP 提供了对应的宏：

```hlsl
UNITY_BRANCH
if (condition) { ... }

// flatten 在 URP 里没有专用宏，但大多数情况下编译器默认就会 flatten
```

**什么时候用 `UNITY_BRANCH`**：条件是 Uniform 的（整个 Draw Call 相同），且被跳过的路径计算量较大时，显式 branch 让 GPU 可以跳过整个块。

```hlsl
UNITY_BRANCH
if (_AdditionalLightsCount > 0)
{
    // 附加光照计算（较重）
    // 条件对整个 DrawCall 统一，branch 可以让无灯光时完全跳过
}
```

**什么时候用 flatten（默认）**：条件基于像素值，路径计算轻量时，让编译器展开效率更高。

---

## for 循环的分支问题

循环本身不一定有 Divergence，但循环次数如果因像素而异就会有问题：

```hlsl
// ❌ 循环次数由像素决定——不同像素可能循环不同次，Divergence
int count = (int)SAMPLE_TEXTURE2D(_CountTex, sampler, uv).r * 10;
for (int i = 0; i < count; i++) { ... }

// ✅ 循环次数固定或由 Uniform 决定——安全
for (int i = 0; i < _LightCount; i++) { ... }
```

---

## 循环展开：unroll

当循环次数在编译期已知时，可以提示编译器展开：

```hlsl
[unroll]
for (int i = 0; i < 4; i++)
{
    result += SampleLight(i);
}
```

展开后消除循环控制指令，也消除了动态跳转的 Divergence。适合小固定次数的循环。

---

## 实际建议

| 场景 | 推荐做法 |
|------|---------|
| 条件基于像素值，两条路径都轻量 | `step`/`lerp` 替代，无分支 |
| 条件基于像素值，两条路径都很重 | 拆成两个 Shader 变体（`multi_compile`） |
| 条件是 Uniform（材质属性、宏） | 直接用 `if` 或 `UNITY_BRANCH`，安全 |
| 编译期已知条件 | `#if` / `#ifdef` |
| 小固定循环 | `[unroll]` |

---

## 小结

| 概念 | 要点 |
|------|------|
| SIMD | 32/64 线程同时执行同一指令 |
| Warp Divergence | 同一 Warp 里走不同路径，串行执行，时间加倍 |
| 安全的 if | 条件是 Uniform 或编译期常量 |
| 替代方案 | `step`/`lerp`/`smoothstep` 把分支变成连续计算 |
| `UNITY_BRANCH` | 条件 Uniform 且跳过块计算量大时使用 |
| `[unroll]` | 固定次数小循环展开 |

下一篇：宏体系与 Shader 变体——`#pragma multi_compile` vs `shader_feature`，变体爆炸的根源和控制方法。
