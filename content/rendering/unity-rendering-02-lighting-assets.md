---
date: "2026-03-24"
lastmod: "2026-10-26"
title: "Unity 渲染系统 02｜光照资产：实时光、Lightmap、Light Probe、Reflection Probe"
description: "分清实时光、Lightmap、Light Probe 和 Reflection Probe 的数据与职责，按光照模式理解烘焙内容、动态对象和运行时采样成本。"
slug: "unity-rendering-02-lighting-assets"
weight: 500
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "Lighting"
  - "Lightmap"
  - "LightProbe"
  - "ReflectionProbe"
  - "GI"
  - "PBR"
series: "Unity 渲染系统"
---
> 版本与证据范围：本轮按 Unity 6（6000.0）/ URP 17 公开文档校正模式与数据语义，传统 Light Probe 与 APV 分开理解。代码用于解释数据关系，未在 Unity 中编译、烘焙或测量。

> 如果只用一句话概括这篇，我会这样说：Unity 的光照不是一个系统，而是四条并行的路径——实时光、Lightmap、Light Probe、Reflection Probe——各自解决不同类型的光照贡献，最终在 Fragment Shader 里汇合成一个颜色。

01 篇在 PBR 计算那一节留了一个坑：

```
间接光漫反射  = albedo × (1 - metallic) × irradiance（来自 Light Probe 或 Lightmap）
间接光高光    = reflectionColor（来自 Reflection Probe）
```

这篇把这个坑填上：**这些预计算光照与环境反射数据是什么，怎么产生的，Shader 怎么使用它们。** Lightmap 和探针的内容取决于光照模式，不能一律称为间接光。

---

## 为什么光照要拆成四条路径

光照计算可以分成两类：

**直接光**：光源直接照射到表面，计算相对简单（光源方向 × 法线 × 材质参数），可以每帧实时计算。

**间接光**：光线经过其他表面反射后到达当前表面的贡献，例如天花板反射的地面光。求解光线传输成本很高，项目可以采用预计算或实时近似，不能一概说只能离线计算。

Unity 的四条路径就是这个分工：

```
实时光          → 直接光（每帧计算，可动态变化）
Lightmap        → 表面的预计算照明；内容取决于光源与场景光照模式
Light Probe     → 空间中的低频预计算照明，可供动态物体使用
Reflection Probe → 环境镜面反射（预计算或实时，适合高光反射）
```

---

例如 Baked 光源的直接与间接光均可进入 lightmaps 和 Light Probes；Mixed 的实时/烘焙分工还取决于场景 Lighting Mode。应用烘焙结果仍有采样和数据成本。见 [Unity Light Modes](https://docs.unity3d.com/6000.0/Documentation/Manual/LightModes-introduction.html)。

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

实时光增加运行时着色工作。URP Forward 的额外光计算还受逐顶点/逐像素设置、影响范围、覆盖与剔除结果影响；额外光计算不等于额外 Draw Pass，成本不能只按光源总数线性外推。

**阴影**同样是实时光的一部分：方向光从光源视角渲染一张深度贴图（Shadow Map），Fragment Shader 用当前像素的世界坐标在 Shadow Map 里采样，判断这个点是否被遮挡。Shadow Map 的分辨率和级联数（Cascade）直接影响阴影质量和性能。

---

## 第二条路径：Lightmap（预计算表面光照）

### Lightmap 是什么

Lightmap 存储物体表面的预计算照明。Baked 光源可以烘焙直接与间接光，Mixed 则需结合 Baked Indirect、Shadowmask 或 Subtractive 等实际模式理解。方向性和遮挡数据也可能使用额外资源，不能统一定义成一张“间接光强度图”。

使用 Progressive Lightmapper 等烘焙器时，会离线求解光照并保存结果。运行时免去了相应逐帧求解，但仍要驻留数据、传递 UV、采样与解码。烘焙迭代时间与运行时内存/采样是不同成本。

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

// 采样并解码预计算光照；实际接口取决于版本与方向性设置
float3 bakedGI = SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
                                      lightmapUV, ...);

// 叠加到漫反射
float3 indirectDiffuse = albedo * (1 - metallic) * bakedGI;
```

`unity_LightmapST` 是一个变换参数，处理 Lightmap 图集中每个物体的偏移和缩放（多个物体的 Lightmap 通常打包在一张图集里）。

### Lightmap 的限制

- **表面与环境关系被固定**：通常供参与烘焙、以 Lightmaps 接收 GI 的固定表面使用；移动后结果不会自动反映新位置。勾选任意 Static 标志不保证配置正确
- **不自动更新烘焙结果**：实时光可以另外变化；烘焙贡献若需更新，要选择重烘焙、预制多套数据或其他方案并处理切换
- **额外内存占用**：每张 Lightmap 贴图占用 VRAM，大场景可能有几十张 Lightmap

---

## 第三条路径：Light Probe（动态物体的预计算照明）

### Light Probe 解决什么问题

移动对象无法仅靠固定表面的 Lightmap 正确响应沿途照明。若只用统一环境值，进入暖色房间时可能与场景脱节。

传统 Light Probe 用空间采样的预计算照明缓解这类问题；它可以包含 Baked 光源的直接光与间接光，不能自动提供动态遮挡或自阴影。

### Light Probe 存什么

一个 Light Probe 是空间中的一个采样点，存储该点周围所有方向的光照强度——用**球谐函数（Spherical Harmonics，SH）**编码。

直观理解：对空间位置处的方向性照明作低频近似。传统探针常用到二阶、共三个 band（L0/L1/L2）的 SH，每个颜色 9 个系数，共 27 个数；“三个 band”与“最高二阶”表示同一范围。

低频近似意味着：**Light Probe 只能表示柔和的环境光分布，不能表示锐利的阴影或高频光照细节**。这是设计上的取舍——低精度但极其高效。

### 运行时怎么使用

传统逐 Renderer 探针路径在四面体化后的布局中按采样位置插值，再传给 Shader。位置与探针锚点等设置有关，不固定为“最近几个”或永远取包围盒中心。APV 等路径不套用这里的逐对象模型。

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

下面展开 Unity 已打包的 Shader 常量形式，`unity_SH*` 不能直接当作原始数学系数；常量项包含打包调整。公式对照公开 Package 的 [SphericalHarmonics.hlsl](https://github.com/Unity-Technologies/Graphics/blob/6000.0/staging/Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl)，这是该分支的静态实现依据。

```hlsl
// 输入：世界空间单位法线 n，以及 CPU 上传的 SH 系数
// unity_SHAr/g/b：打包的线性与常量部分，w 不能直接视为原始 L0
// unity_SHBr/g/b：L2 前 4 个系数，float4
// unity_SHC：     L2 第 5 个系数，float3（RGB 各一个值）

float3 SHEvalExpanded(float3 n) {
    float3 result = 0;

    // ─── 打包后的常量部分 ────────────────────────────────────────────────
    // 已打包常量项，不再额外减去原始基函数常数
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
    // 描述二次方向变化，band 数不等于能表达的光源数量
    float4 b = float4(n.x * n.y,                    // xy 项
                      n.y * n.z,                    // yz 项
                      n.z * n.z,                    // Unity 已打包系数对应 zz
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
- **检查动态物体的活动区域与边界**：不要把超出探针凸包后的插值/外推行为一概当作自动切回 Ambient；沿角色路线观察实际照明与锚点

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

**粗糙度和 mip 的关系**：Reflection Probe 数据在生成/预过滤时准备不同粗糙度对应的层级。较光滑的表面通常使用较清晰层级，较粗糙的表面使用较模糊层级；上面的线性换算只是示意，实际映射按管线函数处理。

### Box Projection（盒体投影）

默认情况下，Reflection Probe 把 Cubemap 当作无限远的环境球来采样，适合室外天空反射。但室内场景里，反射方向应该命中有限距离的墙壁、地板，而不是"无限远的反射"——这会导致反射位置明显偏移。

开启 **Box Projection** 后，Unity 会根据物体相对于 Probe 盒体边界的位置修正反射方向，使室内物体的反射更准确。

### Baked vs Realtime

- **Baked（烘焙）**：捕获一次，存成 Cubemap 资产，运行时直接采样。适合静态室内、不变的天空
- **Realtime（实时）**：每帧（或按间隔）重新渲染 Cubemap，能反映动态变化。代价是每帧额外的渲染开销（相当于从 6 个方向各多渲染一次场景）

---

## 四条路径在 Fragment Shader 里的合并

把前面几类贡献组织起来，可以得到下面的教学模型。它以“Mixed 光源的实时直接光与烘焙间接光分开”为例；Baked、Shadowmask、Subtractive 等实际路径还要处理对应分工与混合，不能把预计算直接光再叠加一次：

```
最终颜色 =
    直接光漫反射（实时光 × albedo × (1-metallic)）
  + 直接光高光（实时光 × BRDF）
  + 间接光漫反射（(Lightmap 或 Light Probe SH) × albedo × (1-metallic)）
  + 间接光高光（Reflection Probe Cubemap × Fresnel × BRDF_Specular）
  + 自发光（Emissive）
```

这是贡献分类，不保证全部计算都发生在一次片元调用中；逐顶点计算、多个 pass、变体和渲染路径都会改变实现位置。

**metallic 参数的作用在这里体现得很明显**：
- `metallic = 0`（非金属）：漫反射项满权重，高光是白色
- `metallic = 1`（纯金属）：漫反射项几乎为零，高光颜色继承 albedo 颜色，间接镜面反射贡献最大

### Fragment Shader 的教学框架

下面只示意主光、传统探针/非方向性 Lightmap 和环境反射的关系。省略了实际接口和多种光照模式，不能直接复制为可编译的 URP Lit 实现：

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

这段伪代码帮助定位数据来源；实际 URP 还包含方向性 Lightmap、混合光遮挡、探针选择、多光源循环和 BRDF 等处理，不能由此宣称与真实实现完全一致。

---

## 常见光照问题定位

**问题：静态物体在烘焙后依然没有间接光效果（全黑或和直接光一样）**

检查顺序：
1. 物体是否标记为 Static（Contribute GI）
2. 是否有有效 Lightmap UV（UV1）——可以手工制作或由 Generate Lightmap UVs 生成，还需检查边距与重叠
3. 烘焙是否完成——Window → Rendering → Lighting，查看烘焙状态
4. Shader 是否解码并应用 Lightmap；Meta Pass 提供烘焙所需表面信息，不能代替运行时采样逻辑

**问题：动态物体（角色）在不同区域光照差异很大，走进室内突然变很亮或很暗**

检查顺序：
1. 室内是否有 Light Probe——Light Probe Group 是否覆盖了角色的活动范围
2. 内外过渡区域的 Probe 是否足够密集
3. 用 Frame Debugger 查看角色 Draw Call 里的 SH 系数值（Properties 里的 `unity_SHAr` 等），确认是否插值到了正确的 Probe

**问题：金属物体反射的环境不对（反射到了不存在的东西，或者方向感觉偏移了）**

检查顺序：
1. Reflection Probe 的位置是否合理——Probe 应该放在能"看见"它周围真实环境的位置
2. 室内场景是否开启了 Box Projection
3. Renderer 实际选择了哪份探针数据，Probe 是否按配置刷新；捕获场景的是 Probe，不是材质自行重新捕获

**问题：用 RenderDoc 查看光照结果**

1. 捕获帧，找到目标物体的 Draw Call
2. Pipeline State → PS → CBs，找到 `UnityPerDraw` 常量缓冲区，查看 `unity_SHAr/b/g` 等 SH 系数
3. Texture Viewer → Inputs，找 `unity_Lightmap`（如果有），查看 Lightmap 贴图内容
4. 找 `unity_SpecCube0`（Reflection Probe Cubemap），用 Cube 视图查看六面内容是否正确

---

## 和下一篇的关系

这些光照路径也服务于移动或变形的表面，其中固定表面 Lightmap 与动态对象探针的条件不同。角色奔跑、衣物和面部变形还需要更新几何；下一篇讲骨骼动画和 Blend Shape。
