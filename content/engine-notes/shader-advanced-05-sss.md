+++
title = "Shader 进阶技法 05｜皮肤次表面散射：SSS 与透光感"
slug = "shader-advanced-05-sss"
date = 2026-03-26
description = "皮肤不是完全不透明的——光线穿透进去散射后再出来，让皮肤显得通透有血色。理解次表面散射的物理原理，实现 Wrap Lighting 和透射（Transmission）两种近似方案。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "进阶", "SSS", "皮肤", "次表面散射"]
series = ["Shader 手写技法"]
[extra]
weight = 4330
+++

真实皮肤、蜡烛、玉石等半透明材质有一个共同特征：光线不是在表面直接反射，而是穿透进材质内部散射后再出来。这叫**次表面散射（Subsurface Scattering，SSS）**。它让皮肤看起来有血色、通透，而不是塑料感的纯反射。

---

## 物理原理

皮肤由多层组成（角质层、表皮、真皮、皮下脂肪），光线穿透各层时被血红蛋白等染色剂吸收和散射。最终出射的光：

- 出射点和入射点**不在同一位置**（散射使光横向扩散）
- 出射颜色偏红（血红蛋白在红光散射更多）
- 背光时薄处（耳朵、手指）有透光感

完整 SSS 需要屏幕空间模糊（SSSS 技术），代价很高。游戏里通常用两种近似：

1. **Wrap Lighting**：把 NdotL 的负值区域扭曲，让背光面也有漫射
2. **Transmission（透射）**：模拟背光透过薄处的效果

---

## 方案一：Wrap Lighting

标准 Lambert 漫反射在 `NdotL < 0` 时直接截断为 0（背面全黑）。Wrap Lighting 把这个截断推到更小的值，让光"绕"过表面：

```hlsl
// 标准 Lambert
float NdotL = saturate(dot(normal, lightDir));

// Wrap Lighting（w = 包裹系数，0.5 意味着光绕半圈）
float w     = _WrapFactor;   // 通常 0.3~0.5
float NdotL_wrap = saturate((dot(normal, lightDir) + w) / (1.0 + w));
```

当 `dot(N, L) = -w` 时，`NdotL_wrap = 0`（完全暗）；
当 `dot(N, L) = 0` 时，`NdotL_wrap = w / (1+w)`（有一定亮度，而不是 0）。

效果：侧光面和背光面之间的过渡更柔和，皮肤的球状感更强。

**加上散射颜色：**

皮肤散射颜色偏红（血液颜色），在暗部加入红色调：

```hlsl
// 用 NdotL_wrap 在亮部颜色和散射颜色之间插值
half3 scatterColor = half3(0.8, 0.2, 0.1);   // 红色散射
half3 diffuse = lerp(albedo.rgb * scatterColor, albedo.rgb * lightColor, NdotL_wrap);
```

---

## 方案二：透射（Transmission）

耳朵、手指在背光时会透光——光从背面穿过来。透射模拟这种效果：

**原理**：计算从光源到表面再到观察者的路径，被遮挡越多（厚度越大），透射越少。

**厚度贴图（Thickness Map）**：存储模型各部分的"厚度"（薄处白色，厚处黑色）。耳朵、鼻翼、手指是薄处，躯干是厚处。

```hlsl
// 采样厚度贴图（白色=薄，透光；黑色=厚，不透光）
half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;

// 透射方向：从光源穿过表面到达观察者
float3 transmitDir = normalize(lightDir + normalWS * _TransmitDistortion);
float  VdotL_trans = saturate(dot(viewDir, -transmitDir));

// 透射强度：厚度越薄越强，视线越对齐越强
half   transmit    = pow(VdotL_trans, _TransmitPower) * thickness * _TransmitStrength;
half3  transColor  = mainLight.color * _TransmitColor.rgb * transmit;
```

`_TransmitDistortion`：法线扭曲透射方向，模拟散射的方向偏移（通常 0.1~0.5）。

---

## 完整皮肤 Shader 片段

```hlsl
Properties
{
    _BaseMap         ("Albedo",         2D)           = "white" {}
    _NormalMap       ("Normal Map",     2D)           = "bump" {}
    _ThicknessMap    ("Thickness Map",  2D)           = "white" {}
    _WrapFactor      ("Wrap Factor",    Range(0,1))   = 0.4
    _ScatterColor    ("Scatter Color",  Color)        = (0.8,0.2,0.1,1)
    _TransmitStrength("Transmit Strength", Range(0,2)) = 1.0
    _TransmitPower   ("Transmit Power", Float)        = 3.0
    _TransmitDistortion ("Transmit Distortion", Range(0,1)) = 0.2
    [HDR] _TransmitColor ("Transmit Color", Color)   = (0.8,0.2,0.1,1)
}

// Fragment Shader：
half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;

// ── Wrap Lighting ──────────────────────────────────────────
float w         = _WrapFactor;
float NdotL     = dot(normalWS, mainLight.direction);
float NdotL_w   = saturate((NdotL + w) / (1.0 + w));

half3 diffuse   = albedo.rgb * lerp(_ScatterColor.rgb, mainLight.color, NdotL_w)
                  * mainLight.shadowAttenuation;

// ── 透射（背面透光）────────────────────────────────────────
float3 transmitDir = normalize(mainLight.direction + normalWS * _TransmitDistortion);
float  VdotT       = saturate(dot(viewDir, -transmitDir));
half   transmit    = pow(VdotT, _TransmitPower) * thickness * _TransmitStrength;
half3  transmission = mainLight.color * _TransmitColor.rgb * transmit;

// ── 高光（Blinn-Phong，皮肤高光较宽较弱）──────────────────
float3 halfDir = normalize(mainLight.direction + viewDir);
half   NdotH   = saturate(dot(normalWS, halfDir));
half3  specular = pow(NdotH, 16.0) * 0.3 * mainLight.color;

half3 finalColor = diffuse + transmission + specular;
return half4(finalColor, 1.0);
```

---

## 厚度贴图的制作

厚度贴图在 3D 软件里烘焙：

1. **Blender**：Bake → Thickness（需插件或手动设置）
2. **Substance Painter**：Bake → Thickness
3. **手绘近似**：参考模型，薄处（耳朵、鼻翼）手绘白色，厚处黑色

另一种运行时近似：用深度差自动估算厚度（类似软粒子的思路，但从多个方向采样）。

---

## 移动端适配

| 效果 | 代价 | 移动端建议 |
|------|------|---------|
| Wrap Lighting | 极低（一个公式替换） | 全档位可用 |
| 透射 | 低（一次贴图采样 + 计算） | 高档可用，中低档关闭 |
| 完整 SSSS 模糊 | 极高（多次全屏 Blur） | 不适合移动端 |

---

## 小结

| 技术 | 原理 | 效果 |
|------|------|------|
| Wrap Lighting | 扭曲 NdotL 截断，背面有光 | 柔和的明暗过渡 |
| 散射颜色 | 暗部混入红色调 | 皮肤的血色感 |
| 透射 | 背面光透过厚度贴图 | 耳朵、手指的透光感 |
| 厚度贴图 | 白=薄（透光），黑=厚 | 控制透射强弱分布 |

下一篇：布料 Shader——各向异性高光，天鹅绒的逆光毛边效果，丝绸的方向性反射。
