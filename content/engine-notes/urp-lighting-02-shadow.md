+++
title = "URP 深度光照 02｜URP Shadow 深度：Cascade 机制、Shadow Atlas、Bias 调参"
slug = "urp-lighting-02-shadow"
date = 2026-03-25
description = "URP 阴影系统的完整工作原理：Cascade Shadow Map 的分割算法、Shadow Atlas 的布局与分辨率分配、Bias 参数的数学含义与调参步骤、Soft Shadow 的 PCF 实现、移动端阴影代价的量化分析。"
[taxonomies]
tags = ["Unity", "URP", "Shadow", "Cascade", "Shadow Map", "渲染管线"]
series = ["URP 深度"]
[extra]
weight = 1570
+++

URP 的阴影是移动端性能开销最大的单项功能之一。一个 4 级联、1024 分辨率的 Shadow Map，在 60Hz 下每秒需要执行 4 次完整场景渲染（仅阴影），约占总渲染时间的 20–40%。这篇把阴影系统的工作原理讲清楚，让每个参数的调整有据可依。

---

## Shadow Map 的基本原理

Shadow Map 是一个两阶段算法：

**阶段一（Shadow Pass）：从光源视角渲染深度**

以主光源为视点，用正交投影渲染场景，把每个表面的深度值写入 Shadow Map（一张深度纹理）。Shadow Map 记录的是"从光源看过去，最近的表面在哪里"。

**阶段二（Lighting Pass）：采样 Shadow Map 判断遮挡**

渲染正常场景时，对每个像素：
1. 把世界坐标转换到光源的 NDC 空间（光源 VP 矩阵变换）
2. 把 NDC 的 XY 作为 UV，采样 Shadow Map
3. 比较当前像素的深度（距光源的距离）和 Shadow Map 里的值

```
当前像素深度 > Shadow Map 值：说明有物体比当前像素更靠近光源
  → 当前像素在阴影里
当前像素深度 ≤ Shadow Map 值：当前像素就是最近的物体
  → 当前像素在光照里
```

---

## Cascade Shadow Map：为什么需要多张

### 单张 Shadow Map 的精度问题

Shadow Map 的像素数有限（比如 1024×1024）。当摄像机视野很大时，Shadow Map 的每个像素需要覆盖大片地面，导致阴影边缘粗糙（Shadow Map 分辨率不足）。

```
摄像机视野 100m × 100m，Shadow Map 1024×1024：
  每个 Shadow Map 像素 ≈ 10cm × 10cm 世界空间
  → 近处阴影勉强够用
  → 但如果视野是 500m × 500m：每个像素 ≈ 50cm，近处阴影锯齿严重
```

### Cascade 的解法

把摄像机视锥按距离分成 N 段，每段用一张独立的 Shadow Map：

```
Cascade 1：0m  – 10m（近处），Shadow Map 覆盖小范围 → 高精度
Cascade 2：10m – 30m，Shadow Map 覆盖中等范围 → 中精度
Cascade 3：30m – 70m，Shadow Map 覆盖较大范围 → 低精度
Cascade 4：70m – Max（远处），Shadow Map 覆盖最大范围 → 最低精度
```

渲染每个像素时，根据该像素与摄像机的距离，选择对应的 Cascade 采样：

```hlsl
// 根据距离选 Cascade（URP 内部逻辑）
half cascadeIndex = ComputeCascadeIndex(positionWS);
float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));
half shadow = SampleShadowmap(shadowCoord, cascadeIndex);
```

### Cascade 分割比例

URP 的 Cascade 分割比例在 Pipeline Asset 里配置（当 Cascade Count > 1 时，会出现一个滑动条）。

分割比例决定每个 Cascade 覆盖多少距离。以 4 Cascade、Max Distance = 50m 为例：

| 比例 | Cascade 1 | Cascade 2 | Cascade 3 | Cascade 4 |
|---|---|---|---|---|
| 均匀（0.25/0.25/0.25/0.25）| 0–12.5m | 12.5–25m | 25–37.5m | 37.5–50m |
| 常用（0.067/0.2/0.467/0.267）| 0–3.3m | 3.3–10m | 10–23m | 23–50m |

近处的 Cascade 覆盖范围小，Shadow Map 精度高。调参原则：把 Cascade 1 的比例调小，让近处获得更高精度阴影；把 Max Distance 也尽量缩短，避免 Shadow Map 浪费在玩家看不到或不重要的远处。

---

## Shadow Atlas：所有 Shadow Map 在一张纹理里

URP 把所有 Shadow Map（主光各 Cascade + 附加光各自的 Shadow Map）打包到一张 **Shadow Atlas** 里，避免多次 RT 切换。

```
Shadow Atlas（默认 2048×2048）
┌────────┬────────┐
│ Casc 0 │ Casc 1 │  主光 Cascade 0 / 1
├────────┼────────┤
│ Casc 2 │ Casc 3 │  主光 Cascade 2 / 3
└────────┴────────┘

附加光 Shadow Atlas（独立的 Atlas，默认 1024×1024）
┌──┬──┬──┬──┐
│L1│L2│L3│L4│  各有阴影的附加光（Point / Spot）
└──┴──┴──┴──┘
```

**Atlas 分辨率的影响**：

主光 Atlas 默认 2048×2048，4 个 Cascade 各占 1024×1024——也就是说，一个 Cascade 的实际分辨率 = Atlas 大小 / sqrt(Cascade 数量)。

如果 Atlas = 1024×1024 且 Cascade = 4，每个 Cascade 只有 512×512，精度会明显下降。

**调参建议**：
- 移动端：Atlas 1024、Cascade 2，每个 Cascade 512×512 — 平衡精度与带宽
- PC 中高端：Atlas 2048、Cascade 4，每个 Cascade 1024×1024 — 高质量阴影

---

## Shadow Bias：解决 Shadow Acne 和 Peter Pan

### Shadow Acne 是什么

表面在自己的 Shadow Map 里采样时，浮点精度误差会导致"当前深度稍微大于 Shadow Map 值"，判定自己在阴影里——即使没有任何物体遮挡。表现为表面上出现不规则的条纹状暗斑。

### Depth Bias（深度偏移）

渲染 Shadow Map 时，在深度值上加一个 Offset，让 Shadow Map 里的值"虚报"得稍微更大一点：

```
正常：Shadow Map 深度 = 真实深度
有 Depth Bias：Shadow Map 深度 = 真实深度 + bias

采样时：
  当前深度（真实）< Shadow Map 深度（真实 + bias）
  → 判定为不在阴影 → 消除了 Acne
```

**副作用（Peter Pan）**：bias 太大，阴影整体向光源方向偏移，物体底部的阴影和物体本身出现明显缝隙，物体"浮空"。

### Normal Bias（法线偏移）

渲染 Shadow Map 时，把每个顶点沿自身法线方向偏移一个距离，再渲染深度：

```
顶点偏移后的位置 = 原始位置 + normal × normalBias
```

这样 Shadow Map 里的几何体比实际几何体"略微膨胀"，采样时不会出现"深度刚好等于 Shadow Map 值"的浮点误差。Normal Bias 比 Depth Bias 更精确，不容易产生 Peter Pan。

### 调参步骤

1. 先把 `Depth Bias` 调到刚好消除 Acne（从 0 开始往上加）
2. 观察是否有 Peter Pan（阴影浮空）
3. 如果有 Peter Pan，减小 Depth Bias，增加 Normal Bias 补偿
4. 两者配合，找到 Acne 消失 + Peter Pan 最小的平衡点

典型值参考：
- 一般室外场景：Depth Bias = 1.0, Normal Bias = 1.0
- 地板等大平面（Acne 严重）：Depth Bias = 1.5, Normal Bias = 0.5
- 细薄物体（Peter Pan 敏感）：Depth Bias = 0.5, Normal Bias = 2.0

URP 的 Bias 值是归一化的（不是世界单位），实际偏移 = Bias × `_ShadowBias.x`（由 Pipeline Asset 和光源距离共同决定）。

---

## Soft Shadow：PCF 的实现与代价

URP 的 Soft Shadow 用 **PCF（Percentage Closer Filtering）**：不是采样一次 Shadow Map，而是在周围采样 N×N 个点，求平均，得到软边缘。

```hlsl
// 简化的 PCF 伪代码
float shadow = 0;
for (int x = -radius; x <= radius; x++)
for (int y = -radius; y <= radius; y++)
{
    float2 offset = float2(x, y) * texelSize;
    shadow += SampleShadowmapCompare(shadowCoord.xy + offset, shadowCoord.z);
}
shadow /= (2*radius+1) * (2*radius+1);
```

URP 的 Soft Shadow Quality 档位对应的采样次数：

| 档位 | 采样方式 | 采样次数 | 适用场景 |
|---|---|---|---|
| Off | 单点采样 | 1 | 移动端低端 |
| Low | 3×3 PCF（近似）| ~4（Poisson Disk）| 移动端高端 |
| Medium | 5×5 PCF（近似）| ~9 | PC / 主机 |
| High | 自适应 PCF + 随机旋转 | ~16 | PC 高质量 |

**移动端建议**：Soft Shadow = Low 或 Off。Medium 在中端手机上会消耗约 2–5ms，High 不适合移动端。

---

## 附加光的 Shadow Map

附加光（Point / Spot）也可以产生阴影，但代价更高：

- **Spot Light 阴影**：1 张 Shadow Map（单面投影）
- **Point Light 阴影**：6 张 Shadow Map（Cubemap 6 个面，每面各渲染一次）

每个有阴影的附加光 = 额外的 Shadow Pass 次数。

URP 里，Pipeline Asset 的 `Additional Lights → Cast Shadows` 开关控制附加光是否允许产生阴影。即使开启了，每盏附加光的 Light 组件上也要单独勾选 `Cast Shadows`。

**移动端强烈建议**：关闭附加光阴影，或限制最多 1 盏。每盏有阴影的 Point Light 相当于额外渲染 6 次 Shadow Pass。

---

## 阴影的性能代价量化

以一个中等复杂度场景（1000 个 Shadow Caster）为例，移动端（Snapdragon 888）的大致开销：

| 配置 | Shadow Pass 耗时 | 备注 |
|---|---|---|
| 无阴影 | 0ms | |
| 主光 1 Cascade 512 | ~1.5ms | |
| 主光 2 Cascade 512 | ~3ms | |
| 主光 4 Cascade 1024 | ~8–12ms | 移动端上限 |
| + 1 Spot Light 阴影 | +1.5ms | |
| + 1 Point Light 阴影 | +5–8ms | 6 面 × Shadow Pass |

60fps 的帧时间预算 = 16.7ms。主光 4 Cascade 就占掉了约一半预算。

---

## 小结

- **Cascade**：把视锥按距离分 N 段，每段一张 Shadow Map。近处精度高，远处精度低。分割比例调参的原则：让近处 Cascade 占更小的距离范围
- **Shadow Atlas**：所有 Shadow Map 打包到一张纹理，避免 RT 切换。Atlas 大小 / Cascade 数 = 每个 Cascade 的实际分辨率
- **Depth Bias / Normal Bias**：解决 Shadow Acne，Normal Bias 比 Depth Bias 更准确，两者配合调参
- **Soft Shadow**：PCF 多次采样，移动端用 Low 档，PC 用 Medium~High
- **附加光阴影**：代价极高，移动端避免使用；Point Light 最贵（6 × Shadow Pass）

下一篇（URP光照-03）讲 Ambient Occlusion：SSAO 在 URP 中的具体实现方式、SSAO / GTAO 的参数含义，以及移动端的性能代价与降质策略。
