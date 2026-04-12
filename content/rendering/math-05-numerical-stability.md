---
title: "图形数学 05｜数值稳定性：浮点误差、深度精度问题、Kahan 求和"
slug: "math-05-numerical-stability"
date: "2026-03-26"
description: "浮点数不是实数——0.1 + 0.2 ≠ 0.3，大世界坐标精度丢失，深度 Z 非线性分布。这篇把图形开发中最容易踩到的数值问题讲清楚：IEEE 754 浮点格式、深度缓冲精度分布、大世界坐标解决方案、以及 Kahan 求和等稳定化技巧。"
weight: 640
tags:
  - "数学"
  - "图形学"
  - "浮点精度"
  - "深度缓冲"
  - "数值稳定性"
  - "大世界"
series: "图形数学"
---
## IEEE 754 float32 的结构

`float`（单精度）的 32 位按如下方式分配：

```
符号  指数（biased）  尾数（mantissa）
 1 位     8 位           23 位
```

实际值 = `(-1)^符号 × 2^(指数-127) × (1 + 尾数/2^23)`

精度由尾数决定：23 位尾数能精确表示约 `2^23 = 8388608`（约 840 万）个等间距值。在同一个"指数段"内（比如 `[1.0, 2.0)` 范围内），相邻 float 的间距是 `2^-23 ≈ 1.19 × 10^-7`，即约 7 位十进制有效数字。

**关键节点**：当整数部分超过 `2^23 ≈ 838万` 时，float 无法精确表示连续整数。更早的节点是 `2^14 = 16384`：坐标超过 16384 米后，精度降到 0.002 米以下；超过 `2^17 ≈ 131072` 米后，精度降到 0.016 米，顶点抖动在视觉上开始明显。

```cpp
float x = 100000.0f;        // 10万米处
float x2 = x + 0.001f;     // 想加 1 毫米
printf("%.10f\n", x2 - x); // 实际输出：0.0156250000（精度已退化到 ~1.56cm）
```

---

## 图形中的浮点误差

### Z-Fighting

两个几何体的深度值非常接近时，浮点误差导致深度大小关系在不同像素之间随机翻转，表面像素相互"争夺"深度测试，表现为闪烁的摩尔纹。

常见场景：在 Terrain 上叠放一层薄地板、共面的 decal 与底面没有偏移量、阴影 ShadowMap 的 acne 问题（自阴影误差）。

解决方案：

```hlsl
// 方案1：Polygon Offset（深度偏移）
// 在 Unity ShaderLab 里
Offset -1, -1  // factor=-1, units=-1，把近处的面"拉近"一点

// 方案2：分层渲染，用 Camera 的 Near/Far 分段（适合大范围场景）
// 方案3：手动深度偏移（Shader 里修改 SV_Depth）
output.depth = input.pos.z / input.pos.w - 0.0001;
```

ShadowMap 的 self-shadowing acne 也是 Z-Fighting 的变体，标准做法是添加 Shadow Bias（Normal Bias + Depth Bias）。

---

## 深度缓冲的精度分布

标准透视投影把世界空间深度 `z_view`（负数，沿 -Z 方向）映射到 NDC 深度 `z_ndc`（范围 [0, 1] 或 [-1, 1]，因 API 而异）：

```
z_ndc = (f * (z_view - n)) / (z_view * (f - n))
      ≈ f / z_view   （当 f >> n 时的近似）
```

这个映射是**非线性的（1/z 分布）**：`z_view` 在近平面附近时，`z_ndc` 的变化率很大（精度高）；在远平面附近，`z_ndc` 的变化率极小（精度极低）。

具体来说，如果 `Near = 0.1m`、`Far = 1000m`：
- 前 1m 范围（0.1m → 1m）占用了深度缓冲约 99% 的精度
- 后 999m 范围（1m → 1000m）只剩约 1% 的精度

这就是大场景里远处物体容易 Z-Fighting 的根本原因。

### Reversed-Z

现代游戏引擎的标准做法是翻转深度缓冲：**Near = 1，Far = 0**（或 NDC 里 Near = 1，Far → 0）。

这样做的好处是利用了 float 的精度分布特性：float 在小值附近精度更高（尾数位相同，但指数更小），把精度较高的区域分配给了远处，大幅减少远处 Z-Fighting。

```hlsl
// Unity URP/HDRP 默认开启 Reversed-Z
// 检查宏：
#if UNITY_REVERSED_Z
    // 深度值：近=1，远≈0
    // 深度比较：LESS → GREATER（或 LESS_EQUAL → GREATER_EQUAL）
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler, uv).r;
    // depth 接近 1 表示近处，接近 0 表示远处
#else
    // 传统深度：近=0，远=1
#endif
```

Reversed-Z 在 OpenGL 下需要通过 `glClipControl(GL_LOWER_LEFT, GL_ZERO_TO_ONE)` 开启（OpenGL 4.5+）；在 DirectX 11/12 和 Vulkan 下是标准行为。

---

## 大世界坐标精度：Camera-Relative Rendering

大地图游戏（开放世界、飞行模拟）里，玩家走到 `(100000, 0, 0)` 坐标时，float32 精度只剩约 0.01 米，顶点在 0.01m 的格子上跳动，动画和物理积分都出问题。

解决方案是 **Camera-Relative Rendering**：在 CPU 侧把所有顶点坐标减去摄像机位置，再传入 GPU。GPU 始终只处理相对摄像机的小坐标（几百米范围内），精度没有问题。

```csharp
// C# 侧（每帧更新）
Vector3 cameraPos = Camera.main.transform.position;

// 传给 Shader 的矩阵需要加入摄像机平移的补偿
// 方法：用 camera-relative 的 VP 矩阵
Matrix4x4 viewRelative = view;  // View 矩阵本身已经包含了 -cameraPos
// 或者直接在 CPU 侧处理所有 Object 的 Model 矩阵：
Matrix4x4 modelRelative = Matrix4x4.TRS(
    objectPos - cameraPos,   // 相对坐标
    objectRot,
    objectScale
);
```

```hlsl
// Shader 侧直接收到 camera-relative 的顶点坐标，不需要特殊处理
float4 clipPos = mul(UNITY_MATRIX_VP, float4(posRelativeCamera, 1.0));
```

Unity HDRP 已内置 Camera-Relative Rendering（`ShaderVariablesGlobal` 里有 `_WorldSpaceCameraPos` 的处理），HLSL 里用 `GetCameraRelativePositionWS()` 等接口。URP 目前没有内置，大世界项目需要手动实现。

Unreal Engine 5 则引入了 **Large World Coordinates（LWC）**，用 double 精度在 CPU 侧存储坐标，传给 GPU 时仍然是 float（通过摄像机相对坐标转换）。

---

## 法线重新归一化

`normalize()` 之后的向量 `length` 理论上是 1.0，但：

- 插值（Vertex → Fragment 插值）会破坏归一化：硬件对两个单位向量做线性插值，结果长度 < 1
- `half` 精度下 `normalize` 结果偏差可达 0.01，累积后影响法线贴图计算

正确做法：

```hlsl
// Vertex Shader 输出归一化法线
output.normalWS = normalize(TransformObjectToWorldNormal(normalOS));

// Fragment Shader 里再次归一化（因为插值破坏了长度）
float3 N = normalize(input.normalWS);

// 法线贴图采样后也要归一化
float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler, uv));
float3 normalWS = normalize(mul(normalTS, float3x3(T, B, N)));  // TBN 变换后重新归一化
```

---

## Kahan 补偿求和

对大量浮点数累加时，舍入误差会随项数增长而积累。标准累加的误差是 O(n·ε)，其中 ε 是机器精度（`float` 约 `1e-7`）。

Kahan 补偿求和通过额外记录被截断的小量，把误差压到 O(ε)（与 n 无关）：

```cpp
// 标准累加：误差 O(n * epsilon)
float sum = 0.0f;
for (float x : values) sum += x;

// Kahan 补偿求和：误差 O(epsilon)，与 n 无关
float KahanSum(const std::vector<float>& values)
{
    float sum  = 0.0f;
    float comp = 0.0f;  // 补偿量（上次被截断的小量）

    for (float x : values)
    {
        float y = x - comp;       // 加上上次截断的补偿
        float t = sum + y;        // 暂时求和（高位数字占主导，低位被截断）
        comp    = (t - sum) - y;  // 计算本次截断的量
        sum     = t;
    }
    return sum;
}
```

图形引擎里适合用 Kahan 求和的场景：
- 粒子系统位置积分（每帧累加速度 × dt，长时间运行后标准积分漂移）
- 物理引擎冲量累积
- 蒙特卡罗积分的样本累加（光线追踪降噪）

---

## 快速诊断数值问题

```hlsl
// 用颜色可视化中间值（在 Fragment Shader 里临时输出）
return float4(frac(worldPos * 0.01), 1.0);  // 可视化坐标密度

// 检查 NaN 和 Inf
float value = /* 某个计算结果 */;
if (isnan(value) || isinf(value))
    return float4(1, 0, 1, 1);  // 品红色标记异常

// 可视化深度精度（Reversed-Z 下）
float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler, uv);
return float4(depth, depth, depth, 1);  // 越亮越近

// 检查法线长度是否偏离 1
float nLen = length(normal);
return float4(abs(nLen - 1.0) * 10.0, 0, 0, 1);  // 红色表示法线未归一化
```

CPU 侧常用 `std::isinf`、`std::isnan`（`<cmath>`）；Release 构建里浮点异常通常被静默处理，调试时可以开启浮点异常捕获（MSVC：`_controlfp_s`；GCC：`feenableexcept`）。

---

## 小结

- float32 约 7 位有效数字，坐标超过 131072 米时精度退化到厘米级，产生顶点抖动。
- Z-Fighting 用 Polygon Offset 或分层渲染缓解；Reversed-Z（Near=1, Far=0）从根本上改善远处深度精度。
- 大世界用 Camera-Relative Rendering：CPU 侧减去摄像机坐标，GPU 始终处理小坐标。
- Fragment Shader 里对插值法线和 TBN 变换后的法线重新 `normalize`。
- Kahan 求和消除长时间浮点累加的漂移误差。
- `isnan`/`isinf` + 颜色可视化是 Shader 数值问题的快速诊断手段。
