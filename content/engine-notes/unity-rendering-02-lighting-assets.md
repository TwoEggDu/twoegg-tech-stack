+++
date = 2026-03-24
title = "Unity 渲染系统 02｜光照资产：实时光、Lightmap、Light Probe、Reflection Probe"
description = "把 Unity 的四条光照路径拆开讲清楚：实时光提供直接光、Lightmap 存储烘焙间接光、Light Probe 给动态物体提供间接光、Reflection Probe 提供环境反射——以及这四条路径怎么在 Fragment Shader 里合并成最终颜色。"
slug = "unity-rendering-02-lighting-assets"
weight = 500
featured = false
tags = ["Unity", "Rendering", "Lighting", "Lightmap", "LightProbe", "ReflectionProbe", "GI", "PBR"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：Unity 的光照不是一个系统，而是四条并行的路径——实时光、Lightmap、Light Probe、Reflection Probe——各自解决不同类型的光照贡献，最终在 Fragment Shader 里汇合成一个颜色。

01 篇在 PBR 计算那一节留了一个坑：

```
间接光漫反射  = albedo × (1 - metallic) × irradiance（来自 Light Probe 或 Lightmap）
间接光高光    = reflectionColor（来自 Reflection Probe）
```

这篇把这个坑填上：**这些间接光数据是什么，怎么产生的，Fragment Shader 怎么拿到它们。**

---

## 为什么光照要拆成四条路径

光照计算可以分成两类：

**直接光**：光源直接照射到表面，计算相对简单（光源方向 × 法线 × 材质参数），可以每帧实时计算。

**间接光**：光线在场景中多次弹射后最终照射到表面的光（天花板反射的地面光、走廊里弹来弹去的环境光）。精确计算间接光需要路径追踪，实时做不起，必须预计算或近似。

Unity 的四条路径就是这个分工：

```
实时光          → 直接光（每帧计算，可动态变化）
Lightmap        → 间接光 + 烘焙直接阴影（预计算，只适合静态物体）
Light Probe     → 间接光（低精度，适合动态物体）
Reflection Probe → 环境镜面反射（预计算或实时，适合高光反射）
```

---

## 第一条路径：实时光（直接光）

### 实时光提供什么

实时光（Directional Light、Point Light、Spot Light）的参数在每帧由引擎计算后传入 Shader：

```
_MainLightPosition:    (方向光的方向向量)
_MainLightColor:       (颜色 × 强度)
_AdditionalLightsCount: N
_AdditionalLightsPosition[N]: (额外光源位置数组)
_AdditionalLightsColor[N]:    (额外光源颜色数组)
```

Fragment Shader 用这些参数，配合表面法线和材质参数，计算直接光的漫反射和高光：

```
直接光漫反射 = albedo × max(dot(N, L), 0) × lightColor × (1 - metallic)
直接光高光   = F(V, H) × D(roughness) × G(roughness) × lightColor / (4 × dot(N,V) × dot(N,L))
```

### 实时光的代价

实时光是"每帧重新算"的，代价随光源数量线性增长。URP 的 Forward 渲染下，每个额外光源对每个被照射的物体都要执行一次额外的光照计算——这就是"控制实时光数量"这条性能建议的根本原因。

**阴影**同样是实时光的一部分：方向光从光源视角渲染一张深度贴图（Shadow Map），Fragment Shader 用当前像素的世界坐标在 Shadow Map 里采样，判断这个点是否被遮挡。Shadow Map 的分辨率和级联数（Cascade）直接影响阴影质量和性能。

---

## 第二条路径：Lightmap（烘焙间接光）

### Lightmap 是什么

Lightmap 是一张预先烘焙好的贴图，存储的是场景里静态物体表面接收到的**间接光强度**（以及烘焙的直接阴影）。

烘焙的本质是路径追踪：对场景里每个静态物体的每个像素，模拟大量光线从光源出发、在场景中多次弹射的过程，统计最终到达该像素的光能，存入贴图。这个过程可能需要几分钟到几小时，但结果保存下来之后，运行时只需要采样这张贴图，零实时计算开销。

### Lightmap UV（UV1）

Lightmap 用的是一套专用的 UV 坐标，叫 **Lightmap UV**（通常存在 UV1 通道）。

这套 UV 和表面贴图用的 UV0 是分开的，原因是它们有不同的要求：

- **UV0**：可以有重叠（同一张贴图可以贴在不同的面上）、可以平铺（Tiling）
- **UV1**：必须唯一（Lightmap 上每块区域对应场景中确定的一块表面）、不能重叠、不能平铺

Unity 的 Mesh Import Settings 里有 **"Generate Lightmap UVs"** 选项，会自动展开一套唯一的 Lightmap UV。也可以在建模软件里手动展开后导入。

### Fragment Shader 怎么采样 Lightmap

```hlsl
// Lightmap 的 UV 坐标从顶点的 UV1 传入
float2 lightmapUV = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;

// 采样 Lightmap，得到间接光强度
float3 bakedGI = SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
                                      lightmapUV, ...);

// 叠加到漫反射
float3 indirectDiffuse = albedo * (1 - metallic) * bakedGI;
```

`unity_LightmapST` 是一个变换参数，处理 Lightmap 图集中每个物体的偏移和缩放（多个物体的 Lightmap 通常打包在一张图集里）。

### Lightmap 的限制

- **只适用于标记为 Static 的物体**：动态物体（角色、可移动的道具）无法使用 Lightmap——它们的位置每帧都在变，烘焙好的光照贴图无法跟着更新
- **不响应动态变化**：白天/夜晚切换、灯光被打碎——所有改变都要重新烘焙
- **额外内存占用**：每张 Lightmap 贴图占用 VRAM，大场景可能有几十张 Lightmap

---

## 第三条路径：Light Probe（动态物体的间接光）

### Light Probe 解决什么问题

角色、怪物、可移动的道具——这些动态物体无法使用 Lightmap。但如果它们完全不受间接光影响，走进一个被暖黄色间接光包围的房间时，角色身上的光照依然是全局统一的冷色，和环境格格不入。

Light Probe 就是为了让动态物体也能感受到周围环境的间接光。

### Light Probe 存什么

一个 Light Probe 是空间中的一个采样点，存储该点周围所有方向的光照强度——用**球谐函数（Spherical Harmonics，SH）**编码。

直观理解：想象站在一个点上，向所有方向看去，把每个方向的光照强度记录下来。这是一个"球面上的函数"。球谐函数是这种球面函数的低频近似——用 27 个浮点数（L0/L1/L2 三阶，9 个系数 × 3 个颜色通道）来表示。

低频近似意味着：**Light Probe 只能表示柔和的环境光分布，不能表示锐利的阴影或高频光照细节**。这是设计上的取舍——低精度但极其高效。

### 运行时怎么使用

运行时，Unity 为每个动态物体找到它周围最近的几个 Light Probe，用物体包围盒中心点位置在这些 Probe 之间进行空间插值（四面体插值），得到一组 SH 系数，传入 Shader：

```hlsl
// Fragment Shader 里，用世界空间法线对 SH 求值
float3 SHEval(float3 normalWS) {
    return max(0, SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb)
                + SHEvalLinearL2(normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC));
}

float3 indirectDiffuse = albedo * (1 - metallic) * SHEval(normalWS);
```

`unity_SHAr/g/b` 等就是 CPU 插值好之后传进来的 SH 系数。

### SH 求值的展开版本

把 `SHEval` 展开，能看清楚 27 个系数分别在做什么：

```hlsl
// 输入：世界空间单位法线 n，以及 CPU 上传的 SH 系数
// unity_SHAr/g/b：L0+L1 系数，float4（xyz = L1 系数，w = L0 系数）
// unity_SHBr/g/b：L2 前 4 个系数，float4
// unity_SHC：     L2 第 5 个系数，float3（RGB 各一个值）

float3 SHEvalExpanded(float3 n) {
    float3 result = 0;

    // ─── L0（常数项，1 个基函数）─────────────────────────────────────────
    // Y_0 = 0.282（归一化常数），对所有方向相同
    // 物理意义：Light Probe 位置处的平均环境亮度，是"全方向均匀光"
    result.r += unity_SHAr.w;
    result.g += unity_SHAg.w;
    result.b += unity_SHAb.w;

    // ─── L1（线性项，3 个基函数：x / y / z）──────────────────────────────
    // Y_1 ∝ (x, y, z)，描述从某方向来的主光方向偏移
    // 物理意义：近似一盏主方向光的漫反射贡献
    result.r += dot(unity_SHAr.xyz, n);
    result.g += dot(unity_SHAg.xyz, n);
    result.b += dot(unity_SHAb.xyz, n);

    // ─── L2（二次项，5 个基函数）─────────────────────────────────────────
    // 描述更复杂的方向变化：xy/yz/zz/xz 的乘积项，以及 x²-y²
    // 物理意义：近似两盏以上光源的叠加、以及有一定方向性的间接光细节
    float4 b = float4(n.x * n.y,                    // xy 项
                      n.y * n.z,                    // yz 项
                      n.z * n.z - 1.0 / 3.0,        // zz - 常数（减去 L0 的泄漏）
                      n.z * n.x);                   // zx 项
    result.r += dot(unity_SHBr, b);
    result.g += dot(unity_SHBg, b);
    result.b += dot(unity_SHBb, b);

    float c = n.x * n.x - n.y * n.y;               // x²-y² 项（第 5 个 L2 基函数）
    result.r += unity_SHC.r * c;
    result.g += unity_SHC.g * c;
    result.b += unity_SHC.b * c;

    return max(0, result);  // 负值无物理意义，截断到 0
}
```

这 9 个基函数（1 + 3 + 5）× 3 通道 = 27 个浮点数，就是一个 Light Probe 存储的全部数据。

L0 和 L1 合在一起可以近似表示一盏主方向光 + 环境底色；L2 加入后能捕捉到"两侧亮、顶部暗"或"多方向来光"这类更复杂的分布。但高频细节（锐利的点光源、尖锐阴影边界）L2 完全无法表达——这是 Light Probe 只适合表示柔和环境光的根本原因。

### Light Probe 的设置建议

- **在光照变化明显的地方密集放置**：门口、窗边、明暗交界处
- **在开阔平坦区域稀疏放置**：大空地上放太多 Probe 意义不大
- **确保覆盖动态物体的所有活动区域**：角色走出 Probe 覆盖范围时会退回到场景的默认 Ambient 光照

---

## 第四条路径：Reflection Probe（环境镜面反射）

### Reflection Probe 解决什么问题

实时光的高光只能表示来自点光源或方向光的反射亮斑。但现实中，金属表面、光滑石材、水面反射的是整个环境——天空、建筑、附近的物体。这部分反射来自"间接镜面光"，Lightmap 和 Light Probe 解决的是漫反射部分，没有解决这个。

Reflection Probe 就是专门解决**环境镜面反射**的。

### Reflection Probe 存什么

Reflection Probe 在空间中某个位置，向六个方向各渲染一次场景，得到一张 **Cubemap**（六面体贴图）。这张 Cubemap 记录了从这个位置看出去的环境全景。

Fragment Shader 用反射方向向量去采样这张 Cubemap：

```hlsl
// 计算反射方向
float3 reflectDir = reflect(-viewDir, normalWS);

// 用粗糙度选择 mip 层级（粗糙度越高，用越模糊的 mip）
float mipLevel = roughness * UNITY_SPECCUBE_LOD_STEPS;

// 采样 Reflection Probe Cubemap
float4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0,
                                                   reflectDir, mipLevel);
float3 indirectSpecular = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
```

**粗糙度和 mip 的关系**：Reflection Probe 的 Cubemap 在导入时会预先生成多个模糊程度不同的 mip 层级。粗糙度低（光滑表面）→ 采样清晰的 mip 0，高光反射清晰；粗糙度高（粗糙表面）→ 采样模糊的高 mip，高光反射模糊扩散。

### Box Projection（盒体投影）

默认情况下，Reflection Probe 把 Cubemap 当作无限远的环境球来采样，适合室外天空反射。但室内场景里，反射方向应该命中有限距离的墙壁、地板，而不是"无限远的反射"——这会导致反射位置明显偏移。

开启 **Box Projection** 后，Unity 会根据物体相对于 Probe 盒体边界的位置修正反射方向，使室内物体的反射更准确。

### Baked vs Realtime

- **Baked（烘焙）**：捕获一次，存成 Cubemap 资产，运行时直接采样。适合静态室内、不变的天空
- **Realtime（实时）**：每帧（或按间隔）重新渲染 Cubemap，能反映动态变化。代价是每帧额外的渲染开销（相当于从 6 个方向各多渲染一次场景）

---

## 四条路径在 Fragment Shader 里的合并

把前面所有路径的贡献加在一起，就是 URP/Lit 的最终颜色公式：

```
最终颜色 =
    直接光漫反射（实时光 × albedo × (1-metallic)）
  + 直接光高光（实时光 × BRDF）
  + 间接光漫反射（(Lightmap 或 Light Probe SH) × albedo × (1-metallic)）
  + 间接光高光（Reflection Probe Cubemap × Fresnel × BRDF_Specular）
  + 自发光（Emissive）
```

这五项都在 Fragment Shader 的一次执行里算完，输出一个像素的最终 HDR 颜色值。

**metallic 参数的作用在这里体现得很明显**：
- `metallic = 0`（非金属）：漫反射项满权重，高光是白色
- `metallic = 1`（纯金属）：漫反射项几乎为零，高光颜色继承 albedo 颜色，间接镜面反射贡献最大

### 完整的 Fragment Shader 框架

把四条路径在代码里组装到一起，就是 URP Lit Shader 的核心结构：

```hlsl
// ── 输入（由 Vertex Shader 插值传来）──────────────────────────────────────
float3 positionWS;   // 世界空间位置
float3 normalWS;     // 世界空间法线（已归一化）
float2 uv0;          // 表面贴图 UV
float2 uv1;          // Lightmap UV（静态物体）

// ── 材质参数（从 Material Properties 读取）─────────────────────────────────
float3 albedo     = tex2D(_BaseMap, uv0).rgb * _BaseColor.rgb;
float  metallic   = tex2D(_MetallicMap, uv0).r * _Metallic;
float  roughness  = 1.0 - tex2D(_SmoothnessMap, uv0).r * _Smoothness;
float3 normalTS   = UnpackNormal(tex2D(_NormalMap, uv0));  // 切线空间法线
float3 normalWS   = TangentToWorldNormal(normalTS, ...);   // 转到世界空间
float3 emissive   = tex2D(_EmissionMap, uv0).rgb * _EmissionColor.rgb;

float3 viewDirWS  = normalize(_WorldSpaceCameraPos - positionWS);

// ═══════════════════════════════════════════════════════════════════════════
// 路径一：直接光（实时光源）
// ═══════════════════════════════════════════════════════════════════════════
float3 lightDir   = normalize(_MainLightPosition.xyz);
float3 halfDir    = normalize(lightDir + viewDirWS);
float  NdotL      = max(0, dot(normalWS, lightDir));
float  NdotH      = max(0, dot(normalWS, halfDir));
float  NdotV      = max(0, dot(normalWS, viewDirWS));

// 阴影系数（从 Shadow Map 采样，0 = 在阴影里，1 = 被照亮）
float  shadowAtten = SampleShadowMap(positionWS);

// 直接光漫反射（Lambert）
float3 directDiffuse  = albedo * (1 - metallic) * NdotL * _MainLightColor.rgb * shadowAtten;

// 直接光高光（Cook-Torrance BRDF 的简化版）
float  D = GGX_D(NdotH, roughness);    // 法线分布函数
float  G = SmithG(NdotV, NdotL, roughness); // 几何遮蔽函数
float3 F = Fresnel_Schlick(NdotV, lerp(float3(0.04, 0.04, 0.04), albedo, metallic)); // 菲涅尔
float3 directSpecular = D * G * F / (4 * NdotV * NdotL + 0.001)
                      * NdotL * _MainLightColor.rgb * shadowAtten;

// ═══════════════════════════════════════════════════════════════════════════
// 路径二 + 三：间接光漫反射（Lightmap 或 Light Probe，二选一）
// ═══════════════════════════════════════════════════════════════════════════
#if defined(LIGHTMAP_ON)
    // 静态物体：从 Lightmap 贴图采样（UV1 坐标）
    float2 lightmapUV   = uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    float3 bakedGI      = SampleLightmap(lightmapUV);
#else
    // 动态物体：用 CPU 插值好的 SH 系数对法线方向求值
    float3 bakedGI      = SHEval(normalWS);   // 展开版本见上文
#endif

float3 indirectDiffuse = albedo * (1 - metallic) * bakedGI;

// ═══════════════════════════════════════════════════════════════════════════
// 路径四：间接光高光（Reflection Probe Cubemap）
// ═══════════════════════════════════════════════════════════════════════════
float3 reflectDir     = reflect(-viewDirWS, normalWS);
float  mipLevel       = roughness * UNITY_SPECCUBE_LOD_STEPS;  // 粗糙度 → mip 层级
float3 envSample      = DecodeHDR(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,
                                   samplerunity_SpecCube0, reflectDir, mipLevel));

// Fresnel 控制反射强度（掠射角时反射最强）
float3 envFresnel     = Fresnel_Schlick(NdotV, lerp(float3(0.04,0.04,0.04), albedo, metallic));
float3 indirectSpecular = envSample * envFresnel;

// ═══════════════════════════════════════════════════════════════════════════
// 最终合并
// ═══════════════════════════════════════════════════════════════════════════
float3 finalColor = directDiffuse
                  + directSpecular
                  + indirectDiffuse
                  + indirectSpecular
                  + emissive;

// 输出 HDR 颜色（后处理阶段再做 Tonemapping 压到 0-1）
return float4(finalColor, 1.0);
```

这段代码就是 URP Lit Shader 的骨架。URP 实际实现里多了很多工程细节（多光源循环、Lightmap 解码格式、能量守恒修正、混合模式），但结构和这段伪代码完全一致。

---

## 常见光照问题定位

**问题：静态物体在烘焙后依然没有间接光效果（全黑或和直接光一样）**

检查顺序：
1. 物体是否标记为 Static（Contribute GI）
2. 是否有 Lightmap UV（UV1）——查 Mesh 的 Import Settings，确认勾选了 Generate Lightmap UVs
3. 烘焙是否完成——Window → Rendering → Lighting，查看烘焙状态
4. Material 的 Shader 是否支持 Lightmap——URP/Lit 支持，自定义 Shader 需要添加 meta pass

**问题：动态物体（角色）在不同区域光照差异很大，走进室内突然变很亮或很暗**

检查顺序：
1. 室内是否有 Light Probe——Light Probe Group 是否覆盖了角色的活动范围
2. 内外过渡区域的 Probe 是否足够密集
3. 用 Frame Debugger 查看角色 Draw Call 里的 SH 系数值（Properties 里的 `unity_SHAr` 等），确认是否插值到了正确的 Probe

**问题：金属物体反射的环境不对（反射到了不存在的东西，或者方向感觉偏移了）**

检查顺序：
1. Reflection Probe 的位置是否合理——Probe 应该放在能"看见"它周围真实环境的位置
2. 室内场景是否开启了 Box Projection
3. 烘焙 Reflection Probe 后，物体上的材质是否重新捕获了（运行时 Probe 未刷新）

**问题：用 RenderDoc 查看光照结果**

1. 捕获帧，找到目标物体的 Draw Call
2. Pipeline State → PS → CBs，找到 `UnityPerDraw` 常量缓冲区，查看 `unity_SHAr/b/g` 等 SH 系数
3. Texture Viewer → Inputs，找 `unity_Lightmap`（如果有），查看 Lightmap 贴图内容
4. 找 `unity_SpecCube0`（Reflection Probe Cubemap），用 Cube 视图查看六面内容是否正确

---

## 和下一篇的关系

这篇讲的四条光照路径都作用于**静止的表面**。但 Unity 里大量物体是动态的——角色在奔跑、衣物在飘动、面部在说话。这些形状变化在渲染层面是怎么实现的，下一篇讲骨骼动画和 Blend Shape。
