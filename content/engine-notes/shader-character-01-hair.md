---
title: "游戏常用效果｜头发渲染：Kajiya-Kay 各向异性高光与 Alpha 排序"
slug: "shader-character-01-hair"
date: "2026-03-28"
description: "深入剖析头发渲染的两个核心挑战：Kajiya-Kay 各向异性高光模型的实现原理，以及半透明 Alpha 排序问题的工程解法。"
tags: ["Shader", "HLSL", "URP", "角色渲染", "头发", "各向异性", "Kajiya-Kay"]
series: "Shader 手写技法"
weight: 4500
---

头发渲染是角色渲染中公认的难点，原因不是技术门槛高，而是要同时解决两个性质截然不同的问题：**光照模型**和**渲染顺序**。前者需要一套专门为发丝设计的各向异性高光公式，后者是半透明几何体的老问题——画错顺序就会闪。

---

## 为什么法线高光不适合头发

标准 Blinn-Phong 或 GGX 的高光是基于表面法线 N 的，默认光照沿表面法线方向对称。但头发不是平面，每根发丝是圆柱体，高光沿发丝切线方向拉伸成一条光带。如果用法线高光，头发看起来像塑料块，完全没有丝绸感。

Kajiya-Kay 模型（1989）针对这一点做了修正：**用发丝切线 T 替代法线 N 参与高光计算**。其核心公式：

```hlsl
// sinTH = sin(angle between T and H)
// T: 发丝切线方向（沿发丝延伸方向）
// H: 半程向量
float sinTH = sqrt(1.0 - pow(dot(T, H), 2));
float specular = pow(max(0, sinTH), _SpecularPower);
```

`sin(T, H)` 替代了传统的 `dot(N, H)`，使高光沿切线方向形成条带状光泽。

## 各向异性 Shift Map

单纯的 Kajiya-Kay 高光是均匀的光带，真实头发由于发丝粗细不均、倾斜角度各异，高光位置会有微小偏移，呈现出不规则的闪亮感。解决方案是引入 **shift map**：一张存储切线偏移量的灰度贴图，在发冠附近偏移值接近 0，发梢或分叉处偏移更大。

```hlsl
// 从 shift map 采样偏移量，范围映射到 [-1, 1]
float shiftVal = tex2D(_ShiftMap, uv).r * 2.0 - 1.0;

// 将切线沿法线方向偏移
float3 ShiftTangent(float3 T, float3 N, float shift)
{
    return normalize(T + shift * N);
}
```

偏移后的切线带入 Kajiya-Kay 公式，高光就有了自然的不均匀感。

## 双层高光结构

游戏中常见的头发高光分为两层：

- **Primary specular**：白色，尖锐，位置偏高（靠近发根），代表头发表面的直接反射。
- **Secondary specular**：带发色染色，柔和，位置偏低（靠近发梢），代表光线穿透发丝后的二次散射。

两层各自有独立的 shift 偏移参数和 specular power。

```hlsl
float StrandSpecular(float3 T, float3 V, float3 L, float exponent)
{
    float3 H = normalize(L + V);
    float sinTH = sqrt(1.0 - pow(saturate(dot(T, H)), 2));
    return pow(sinTH, exponent);
}

// Fragment Shader 中双层叠加
float3 HairSpecular(float3 T, float3 N, float3 V, float3 L, float2 uv)
{
    float shiftVal = tex2D(_ShiftMap, uv).r * 2.0 - 1.0;

    // Primary: 白色，偏移量较小
    float3 T1 = ShiftTangent(T, N, _PrimaryShift + shiftVal * _ShiftStrength);
    float3 spec1 = StrandSpecular(T1, V, L, _PrimaryPower) * _PrimaryColor.rgb;

    // Secondary: 发色染色，偏移量较大
    float3 T2 = ShiftTangent(T, N, _SecondaryShift + shiftVal * _ShiftStrength);
    float spec2Intensity = StrandSpecular(T2, V, L, _SecondaryPower);
    // 用 Fresnel 衰减遮住逆光区域的二次高光
    float fresnel = saturate(1.0 - dot(N, V));
    float3 spec2 = spec2Intensity * fresnel * _SecondaryColor.rgb;

    return spec1 + spec2;
}
```

`_PrimaryColor` 通常接近白色，`_SecondaryColor` 从发色贴图采样后乘上一个倍增系数，视觉上形成金属丝光感。

## Alpha 排序问题

头发几乎不可能用纯不透明网格表现发梢和发缝。发丝末端需要 Alpha 渐隐，必须走 Transparent 渲染队列。Transparent 队列按物体包围盒中心做粗排序，但头发几何体内部的三角面之间没有排序，导致近处发丝被远处发丝遮挡，出现典型的**闪烁和穿透**。

工程上有三种常用策略：

**1. Alpha Test（Alpha Cutoff）**：丢弃 Alpha 低于阈值的片元，走不透明队列，完全没有排序问题。代价是发梢边缘有锯齿感。适合中低端平台。

**2. Alpha to Coverage（MSAA）**：利用 MSAA 的多采样缓冲，将 Alpha 值转换为亚像素覆盖率，渲染时写深度。效果接近半透明但无需排序，边缘平滑。需要开启 MSAA，不适合移动端。

```hlsl
// 在 URP 中开启 Alpha to Coverage
Pass
{
    AlphaToMask On
    // ...
}
```

**3. Dithered Alpha（Ordered Dithering）**：在片元着色器中用 Bayer 矩阵或蓝噪声将 Alpha 转换为二值 discard，在时域上抖动。配合 TAA 或 SSAA 积累样本，可以得到平滑边缘，同时保持深度写入。

```hlsl
// 4x4 Bayer 矩阵 dither
float Dither4x4(float2 screenPos, float alpha)
{
    const float bayer[16] = {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };
    int2 pos = (int2)(screenPos % 4);
    float threshold = (bayer[pos.x + pos.y * 4] + 1.0) / 17.0;
    clip(alpha - threshold);
    return alpha;
}
```

## 完整头发 Fragment Shader

```hlsl
// URP 头发 Shader 关键 Fragment 阶段

Varyings hairVert = ...; // 省略 Vertex Shader

half4 HairFragment(Varyings input) : SV_Target
{
    // 基础纹理
    float4 baseColor = tex2D(_BaseMap, input.uv) * _BaseColor;
    float alpha = baseColor.a;

    // Dithered Alpha（以 TAA 为前提）
    float2 screenPos = input.positionCS.xy;
    Dither4x4(screenPos, alpha);

    // 光照向量
    Light mainLight = GetMainLight();
    float3 L = mainLight.direction;
    float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
    float3 N = normalize(input.normalWS);
    float3 T = normalize(input.tangentWS);

    // 漫反射：用 N·L 做基础，略微夸张以避免背光区域过暗
    float NdotL = saturate(dot(N, L));
    float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * 0.8 + 0.2);

    // 双层各向异性高光
    float3 specular = HairSpecular(T, N, V, L, input.uv);
    specular *= mainLight.color * _SpecularIntensity;

    // 环境光（SH）
    float3 ambient = SampleSH(N) * baseColor.rgb * _AmbientStrength;

    return half4(diffuse + specular + ambient, 1.0);
}
```

`input.tangentWS` 需要在 Vertex Shader 中从 mesh 的 tangent 属性变换到世界空间。由于发丝网格建模时切线方向沿发丝走向，不需要额外手动指定。

## Alpha 策略的实际选择

| 平台        | 推荐策略                  | 原因                         |
|-------------|---------------------------|------------------------------|
| PC / 主机   | Alpha to Coverage + MSAA  | 效果最干净，硬件支持         |
| 移动端高端  | Dithered Alpha + TAA      | 无 MSAA 压力，TAA 消抖       |
| 移动端低端  | Alpha Test (Cutoff 0.5)   | 兼容性最好，性能开销最低     |

三种策略可以通过 Shader Keyword 在同一个 Shader 文件中切换，不需要维护多个 Shader。发丝网格的绘制顺序建议手动分为头皮底层（先绘制）和外层飘发（后绘制），减少背面对前面的干扰。
