---
title: "Shader 语法基础 02｜内置数学函数：saturate、lerp、step、smoothstep、frac"
slug: "shader-basic-02-math-functions"
date: "2026-03-26"
description: "HLSL 内置的数学函数是 Shader 的基础词汇。掌握 saturate/lerp/step/smoothstep/frac/pow/abs/floor/ceil，理解它们的图形含义和常见组合用法。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "语法基础"
  - "数学函数"
series: "Shader 手写技法"
weight: 4060
---
写 Shader 和写普通代码不同——没有 `if-else` 的思维习惯，而是用数学函数把条件变成连续值。这些函数是 Shader 的核心词汇，熟悉它们比背语法更重要。

---

## 夹值：saturate

```hlsl
saturate(x)   // 等价于 clamp(x, 0.0, 1.0)
```

把值限制在 [0, 1]。最常用的函数之一，因为颜色、光照系数都要求在这个范围内：

```hlsl
float NdotL = dot(normal, lightDir);
float lit = saturate(NdotL);   // 去掉负值（背面）
```

对向量同样适用，逐分量处理：

```hlsl
half3 color = saturate(rawColor);  // 每个通道都夹到 0~1
```

---

## 线性插值：lerp

```hlsl
lerp(a, b, t)   // 返回 a + (b - a) * t
```

`t = 0` 时返回 `a`，`t = 1` 时返回 `b`，中间值线性过渡。

```hlsl
// 两种颜色之间混合
half3 color = lerp(colorA, colorB, blendFactor);

// 配合 sin 做颜色脉冲
float t = sin(_Time.y) * 0.5 + 0.5;   // 0~1 循环
half3 pulse = lerp(darkColor, brightColor, t);
```

`t` 不限制在 [0, 1]，超出范围会外插（`t = 2` 时返回 `2b - a`）。需要限制时配合 `saturate`：

```hlsl
lerp(a, b, saturate(t))   // 安全插值
```

---

## 阶跃函数：step

```hlsl
step(edge, x)   // x >= edge 时返回 1，否则返回 0
```

数学上是一个跳变：小于阈值为 0，大于等于阈值为 1。

```hlsl
// 替代 if (NdotL > 0.5)：
float lit = step(0.5, NdotL);   // NdotL >= 0.5 时为 1，否则为 0

// 做二值化（卡通渲染的色阶）：
float cel = step(0.5, NdotL);
half3 color = lerp(shadowColor, litColor, cel);
```

注意参数顺序：**edge 在前，x 在后**，和直觉相反，容易记混。

---

## 平滑阶跃：smoothstep

```hlsl
smoothstep(edge0, edge1, x)
```

在 `[edge0, edge1]` 范围内从 0 平滑过渡到 1，两端切线为 0（S 形曲线）。

```hlsl
// 硬边：step
float hard = step(0.5, NdotL);

// 软边：smoothstep（在 0.4~0.6 之间平滑过渡）
float soft = smoothstep(0.4, 0.6, NdotL);
```

`smoothstep` 在边缘检测、渐变遮罩、软阴影边缘、溶解效果里大量使用。

数学形式：`t = clamp((x-e0)/(e1-e0), 0, 1); return t*t*(3 - 2*t)`。

---

## 小数部分：frac

```hlsl
frac(x)   // 返回 x 的小数部分，等价于 x - floor(x)
```

常见于两类场景：

**UV 平铺**：手动控制贴图重复：

```hlsl
float2 tiledUV = frac(input.uv * _TileCount);
```

**周期性动画**：`frac(_Time.y * speed)` 产生 0→1→0→1 的锯齿波，适合单次闪光、逐帧效果：

```hlsl
float t = frac(_Time.y * _Speed);   // 0~1 循环，锯齿波
// 配合 smoothstep 做冲击波扩散
float wave = smoothstep(t - 0.1, t, dist);
```

---

## 幂函数：pow

```hlsl
pow(base, exp)
```

常用于调整高光曲线，值越大，高光越集中越亮：

```hlsl
float spec = pow(saturate(dot(H, N)), _Shininess);
```

**注意**：`pow(x, y)` 在 `x < 0` 时行为未定义。如果 base 可能为负，先 `saturate` 或 `abs`：

```hlsl
float spec = pow(max(0, dot(H, N)), _Shininess);
```

---

## 取整系列：floor、ceil、round

```hlsl
floor(x)   // 向下取整：1.7 → 1.0
ceil(x)    // 向上取整：1.2 → 2.0
round(x)   // 四舍五入：1.5 → 2.0
```

`floor` 在做色阶、网格、棋盘格时常用：

```hlsl
// 棋盘格
float2 grid = floor(input.uv * _GridSize);
float checker = frac((grid.x + grid.y) * 0.5) * 2.0;  // 0 或 1 交替
```

---

## 绝对值与符号：abs、sign

```hlsl
abs(x)    // |x|
sign(x)   // x > 0 时 1，x < 0 时 -1，x == 0 时 0
```

---

## 三角函数：sin、cos、atan2

```hlsl
sin(x)         // 正弦，输入弧度
cos(x)         // 余弦
atan2(y, x)    // 反正切，返回向量(x,y)的角度，范围 (-π, π)
```

`atan2` 常用于极坐标转换，做旋转效果或角度遮罩：

```hlsl
// 以 UV 中心为原点的角度
float2 centered = input.uv - 0.5;
float angle = atan2(centered.y, centered.x);   // -π ~ π
```

---

## 长度与距离：length、distance、dot、cross

```hlsl
length(v)          // 向量长度 √(x²+y²+z²)
distance(a, b)     // 两点距离，等价于 length(a - b)
dot(a, b)          // 点积：|a||b|cos(θ)
cross(a, b)        // 叉积：垂直于 a 和 b 的向量（float3 only）
normalize(v)       // 归一化，返回同方向长度为 1 的向量
```

---

## 函数组合示例：溶解效果

溶解（Dissolve）是这些函数组合的典型案例——用噪声贴图和阈值控制溶解进度：

```hlsl
half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r;

// 硬溶解（硬边）
clip(noise - _Threshold);   // noise < threshold 时 discard

// 带发光边缘的溶解
float edge = smoothstep(_Threshold, _Threshold + _EdgeWidth, noise);
half3 edgeColor = lerp(_EdgeColor.rgb, baseColor.rgb, edge);
clip(noise - _Threshold);
```

`clip` 是另一个常用函数——参数 < 0 时丢弃该像素（discard）。

---

## 速查表

| 函数 | 作用 | 典型用途 |
|------|------|---------|
| `saturate(x)` | 夹到 [0,1] | 光照系数、颜色 |
| `lerp(a,b,t)` | 线性插值 | 混合、过渡 |
| `step(edge,x)` | 阶跃 0/1 | 卡通色阶、阈值判断 |
| `smoothstep(e0,e1,x)` | S 形平滑 | 软边、渐变遮罩 |
| `frac(x)` | 小数部分 | UV 平铺、锯齿波动画 |
| `pow(x,n)` | 幂 | 高光曲线 |
| `floor/ceil/round` | 取整 | 棋盘格、色阶 |
| `abs(x)` | 绝对值 | 镜像、折叠 UV |
| `sin/cos(x)` | 三角函数 | 周期动画 |
| `atan2(y,x)` | 反正切 | 极坐标、角度遮罩 |
| `normalize(v)` | 归一化 | 法线、方向向量 |
| `dot(a,b)` | 点积 | 光照、投影 |
| `cross(a,b)` | 叉积 | 法线计算、切线空间 |
| `clip(x)` | 丢弃像素 | 溶解、Alpha Test |

下一篇：向量与矩阵运算——空间变换、坐标系、切线空间的几何含义。
