---
title: "Shader 核心光照 06｜环境光与 IBL：球谐、反射探针与间接光照"
slug: "shader-lighting-06-ibl"
date: "2026-03-26"
description: "没有环境光的 PBR 材质在背光面完全黑——这不真实。理解球谐光照（SH）提供漫反射环境光，反射探针（Reflection Probe）提供镜面环境光，以及如何在自定义 Shader 里手动接入这两部分。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "光照"
  - "IBL"
  - "环境光"
  - "球谐"
  - "反射探针"
series: "Shader 手写技法"
weight: 4160
---
真实世界里，物体的背光面不是全黑的——天空、地面、周围物体的反射光都在照亮它。这种来自环境的光叫**间接光照（Indirect Lighting）**，由两部分组成：**漫反射间接光（Indirect Diffuse）** 和 **镜面间接光（Indirect Specular）**。

---

## 漫反射间接光：球谐光照（SH）

**球谐（Spherical Harmonics，SH）** 是一种把低频光照信息压缩成少量系数的技术。Unity 把场景的环境光（天空盒、烘焙光照）编码成 9 个球谐系数，每个物体通过 Light Probe 插值得到周围的环境光颜色。

SH 给出的是"来自各个方向的漫反射光"——低频、平滑、不含高频细节。适合表现天光、漫射光。

在 URP Shader 里采样球谐光：

```hlsl
// 需要顶点阶段计算，传给片元
// Varyings 里用宏声明（兼容 Lightmap 和 SH）：
DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);

// Vertex Shader 里：
OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
OUTPUT_SH(output.normalWS, output.vertexSH);   // 按法线方向预采样 SH

// Fragment Shader 里，通过 SAMPLE_GI 统一接口获取：
half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
// 如果物体在光照贴图上：使用 Lightmap
// 如果没有：使用 Light Probe（SH）
```

**效果**：背光面不再全黑，有天空的蓝色/地面的暖色环境色。

---

## 镜面间接光：反射探针（Reflection Probe）

光滑金属、玻璃、水面的环境反射来自**反射探针**——Unity 在场景里预烘焙一个 Cubemap，光滑表面用反射向量采样它，得到镜面环境光。

反射探针通过 **IBL（Image-Based Lighting）** 方式工作：

1. 场景里放 Reflection Probe，烘焙周围环境为 Cubemap
2. Shader 里用反射向量采样 Cubemap，得到环境反射颜色
3. 根据粗糙度（roughness）用不同 Mip 层级——粗糙表面反射模糊，光滑表面反射清晰

在 URP 里，反射探针通过内置函数采样：

```hlsl
// 反射方向
float3 reflectDir = reflect(-viewDir, normalWS);

// 用粗糙度选择 Mip 级别，粗糙 → 高 Mip → 模糊
half perceptualRoughness = 1.0 - smoothness;
half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

// 采样反射探针（自动选最近的探针）
half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir, mip);
half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
```

---

## 菲涅耳效应（Fresnel）

环境反射受**菲涅耳效应**影响：掠射角（视线几乎平行表面）时反射更强，正视时反射更弱。

```hlsl
// Schlick 近似
half  NdotV   = saturate(dot(normalWS, viewDir));
half3 F0      = lerp(half3(0.04, 0.04, 0.04), albedo, metallic);  // 非金属 F0=0.04
half3 fresnel = F0 + (1.0 - F0) * pow(1.0 - NdotV, 5.0);

half3 specularIBL = irradiance * fresnel;
```

非金属材质的 F0（正视反射率）约为 0.04（4%），金属材质的 F0 等于 albedo 颜色。

---

## 完整间接光照代码（手动实现版）

如果不用 `UniversalFragmentPBR`，手动组合所有间接光：

```hlsl
// ── 间接漫反射（SH / Lightmap）───────────────────────────
half3 indirectDiffuse = bakedGI;   // 已由 SAMPLE_GI 获取
half3 diffuseColor    = albedo * (1.0 - metallic);   // 金属没有漫反射
half3 diffuse         = indirectDiffuse * diffuseColor * ao;

// ── 间接镜面（反射探针）──────────────────────────────────
float3 reflectDir = reflect(-viewDir, normalWS);
half   perceptualRoughness = 1.0 - smoothness;
half   mip        = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

half4  envSample  = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,
                        samplerunity_SpecCube0, reflectDir, mip);
half3  envColor   = DecodeHDREnvironment(envSample, unity_SpecCube0_HDR);

// Fresnel + 镜面颜色
half  NdotV   = saturate(dot(normalWS, viewDir));
half3 F0      = lerp(half3(0.04, 0.04, 0.04), albedo, metallic);
half3 fresnel = F0 + (1.0 - F0) * pow(saturate(1.0 - NdotV), 5.0);

// BRDF LUT（URP 预计算的高光积分表，处理 roughness 对镜面的影响）
half2 brdfLUT = SAMPLE_TEXTURE2D(_GlossyEnvironmentColor, sampler_GlossyEnvironmentColor,
                    half2(NdotV, perceptualRoughness)).rg;
half3 specular = envColor * (fresnel * brdfLUT.x + brdfLUT.y) * ao;

// ── 合并 ──────────────────────────────────────────────────
half3 indirect = diffuse + specular;
half3 color    = directLighting + indirect;
```

实际项目中**推荐直接用 `UniversalFragmentPBR`**，它已经包含了所有这些计算，并且处理了各种边界情况。手动实现主要用于学习原理，或需要高度定制化的场景。

---

## 两种间接光对比

| | 间接漫反射（SH） | 间接镜面（反射探针） |
|--|--------------|-----------------|
| 数据来源 | Light Probe / Lightmap | Reflection Probe Cubemap |
| 分辨率 | 极低（9 个系数） | 中等（Cubemap，默认 128px） |
| 适合 | 漫射表面、低频光照 | 光滑金属、镜面 |
| 动态物体 | Light Probe（自动插值） | 最近探针 |
| 静态物体 | Lightmap | 探针或实时捕获 |
| 移动端开销 | 极低（顶点预计算） | 低（一次 Cubemap 采样） |

---

## 移动端注意

反射探针在移动端基本免费（一次 Cubemap 采样），建议保留。

SH 在 Vertex Shader 里计算，片元只读插值结果——开销极低，必须保留。

如果完全不需要环境反射（全哑光材质），可以通过 `shader_feature` 关闭探针采样，减少一次纹理读取。

---

## 常见问题

**Q：背光面还是全黑，SH 没效果**

场景没有设置 Skybox 或者没有生成 Light Probe Grid。检查 `Window → Rendering → Lighting` 的 Environment Lighting 设置。

**Q：反射探针的反射位置不对**

Cubemap 是球面投影，位置偏差属正常。开启探针的 `Box Projection`（勾选 Box Projection，填入探针大小），可以修正室内等有规律空间的反射偏差。

**Q：金属材质没有反射**

检查：1）场景里有 Reflection Probe；2）Probe 已烘焙；3）物体在 Probe 的 Culling Mask 范围内。

**Q：想实时环境反射（如水面）**

使用 `ReflectionProbe` 组件设置 `Type = Realtime`，或者在 URP Renderer Feature 里使用 Planar Reflection 专用方案。

---

## 小结

| 概念 | 要点 |
|------|------|
| 间接漫反射 | 球谐（SH）/ Lightmap，`SAMPLE_GI` 统一接口 |
| 间接镜面 | 反射探针 Cubemap，`SAMPLE_TEXTURECUBE_LOD` |
| 粗糙度影响 | 粗糙 → 高 Mip → 模糊反射 |
| 菲涅耳 | 掠射角反射更强，Schlick 近似 |
| 推荐方式 | 用 `UniversalFragmentPBR`，自动处理所有间接光 |

核心光照层到这里结束。下一层进入**核心技法**——UV 动画、溶解、卡通渲染、描边、折射等 12 个实用效果。
