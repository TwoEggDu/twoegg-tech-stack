---
title: "GPU 渲染优化 03｜Shader 优化：精度、分支与采样次数"
slug: "gpu-opt-03-shader"
date: "2026-03-25"
description: "移动端 Shader 优化的三个核心维度：精度选择（half vs float 对 ALU 和寄存器的影响）、分支对 GPU 流水线的代价、采样次数的控制策略。每个维度都从硬件执行原理出发，给出判断依据而不只是规则。"
tags:
  - "移动端"
  - "GPU"
  - "Shader"
  - "性能优化"
  - "HLSL"
  - "half"
  - "float"
series: "移动端硬件与优化"
weight: 2230
---
Shader 优化是移动端性能调优里最直接、效果最可量化的部分。改一行 `float` 为 `half`、去掉一次纹理采样，用 Profiler 立刻能看到帧时间变化。但要做对，需要理解这些改动背后的硬件原理——不然容易改出精度问题，或者优化了不影响性能的地方。

---

## 精度：half vs float

### 移动端 GPU 的精度支持

HLSL / GLSL 里有三种浮点精度：
- `float`：32bit，全精度
- `half`：16bit，半精度（范围约 ±65504，精度约 3 位小数）
- `fixed`（GLSL `lowp`）：10~11bit，极低精度，现代 GPU 通常映射到 half

在移动端 GPU 上，**16bit（half）运算通常比 32bit（float）快 1~2 倍**，原因是：

**① ALU 吞吐量**：Qualcomm Adreno、ARM Mali Valhall 等现代移动端 GPU 都有专门的 16bit 计算单元，16bit 的 ALU 吞吐量是 32bit 的 2 倍（两个 16bit 打包在一个 32bit 运算槽里执行，即 `FP16×2` packing）。

**② 寄存器压力**：half 占用一半寄存器，更多变量可以同时保存在寄存器里，减少寄存器溢出（register spilling）到内存的情况。

**③ 带宽**：varying（顶点到片元的插值数据）如果用 half，在 Rasterization 阶段搬运的数据量减半。

---

### 什么时候用 half，什么时候必须用 float

**适合用 half 的**：

```hlsl
// 颜色值（0~1范围，精度够用）
half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
half3 normalWS = normalize(i.normalWS); // 法线方向

// 光照中间计算（方向向量、光照系数）
half NdotL = saturate(dot(normalWS, lightDirWS));
half3 diffuse = NdotL * _LightColor.rgb * albedo.rgb;

// UV（通常在 0~1 或略超出，half 精度够）
half2 uv = i.uv;
```

**必须用 float 的**：

```hlsl
// 世界空间坐标（数值大，half 精度不够，会有抖动）
float3 positionWS = i.positionWS;

// 矩阵变换（特别是 VP 矩阵乘法）
float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

// 深度值（范围大，half 精度不足，会有深度冲突）
float depth = i.positionCS.z / i.positionCS.w;

// 高精度 UV 动画（大数值 _Time 参与的计算）
float scrollU = _Time.y * _ScrollSpeed; // _Time.y 是秒数，超出 half 范围
```

**容易踩坑的地方**：

```hlsl
// ❌ _Time 是 float，传入 half 会截断
half t = _Time.y * 10.0; // 几秒后 _Time.y 超出 half 范围（±65504），出现闪烁

// ✅ 计算用 float，最后结果截断到 half
half t = (half)frac(_Time.y * _CycleSpeed); // frac 后值在 0~1，half 精度够
```

---

### 实际操作建议

**不要全局替换 float → half**，而是：
1. Fragment Shader 里的颜色、光照中间量、法线方向 → 改 half
2. 世界坐标、矩阵变换、深度相关 → 保持 float
3. 改完在目标真机上运行，检查有没有视觉闪烁或精度异常

验证方法：修改前后用 RenderDoc 截帧，对比同一像素的颜色值差异是否在可接受范围内。

---

## 分支：GPU 上的分支代价

### GPU 的 SIMD 执行模型

GPU 的 Fragment Shader 不是一个像素一个像素串行执行的，而是以 **Warp**（NVIDIA 叫法）或 **Wavefront**（AMD 叫法）或 **Quad**（通用叫法）为单位，把多个像素打包在一起**同时执行同一条指令**（SIMD，Single Instruction Multiple Data）。

移动端 GPU 通常以 4×4 = 16 个像素为一个执行单元。

**分支的问题**：当一个执行单元里的 16 个像素走了不同的分支路径时（比如 8 个走 `if`，8 个走 `else`），GPU 必须**两条路径都执行**，然后根据条件掩码保留各自的结果。

```
// 假设一个 4×4 Quad 里，有些像素 condition=true，有些 false
if (condition) {
    result = expensive_branch_A(); // 所有像素都执行，false 的像素结果被丢弃
} else {
    result = expensive_branch_B(); // 所有像素都执行，true 的像素结果被丢弃
}
// 总代价 = A + B，不是 max(A, B)
```

这叫 **Divergence（分叉）**，是 GPU 分支代价高的根本原因。

---

### 哪种分支代价低

**编译期常量分支（零代价）**：

```hlsl
// Shader 关键字控制的分支，编译时确定，不同变体编译成不同代码
#ifdef _ENABLE_DETAIL_MAP
    half3 detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uv).rgb;
    albedo.rgb = albedo.rgb * detail * 2.0;
#endif
```

这是最推荐的方式，编译出两个变体，运行时根据 Material 关键字选择，分支在编译期消除。

**Uniform 分支（低代价）**：

```hlsl
// _QualityLevel 是每帧固定的 uniform，Warp 内所有像素条件相同
if (_QualityLevel > 1.0) {
    // 所有像素同时走这里，无 Divergence
}
```

如果分支条件对 Warp 内所有像素都相同（比如来自 `_Time`、`_QualityLevel` 等 uniform），GPU 可以整体跳过另一分支，代价接近零。

**逐像素分支（高代价）**：

```hlsl
// 依赖 UV 或贴图采样结果的分支，不同像素条件不同，产生 Divergence
if (tex.a > 0.5) {
    // 高代价：Warp 内像素条件不同
}
```

---

### 用 step / lerp 替代分支

对于简单的逐像素条件，`step` 和 `lerp` 通常比 `if` 更高效：

```hlsl
// ❌ 逐像素 if，产生 Divergence
half3 color;
if (mask > 0.5) color = colorA;
else            color = colorB;

// ✅ 用 lerp + step，无分支，编译成条件选择指令
half3 color = lerp(colorB, colorA, step(0.5, mask));
```

但注意：`lerp` 替代方案**两个分支都会计算**，只是用数学方式混合结果。如果一个分支计算代价很高（比如包含纹理采样），用 `lerp` 不一定比 `if` 好——Divergence 只是避免了，但两次纹理采样都执行了。

---

### Shader 关键字替代运行时分支

如果分支条件来自材质设置（不同物体用不同效果），优先用 `shader_feature` 或 `multi_compile` 做编译期分支：

```hlsl
#pragma shader_feature_local _DETAIL_MAP_ON

// 运行时材质开关，编译成两个变体，无运行时分支
#ifdef _DETAIL_MAP_ON
    half3 detail = SampleDetailMap(uv);
    albedo = BlendDetail(albedo, detail);
#endif
```

代价：每增加一个关键字，变体数量翻倍。关键字管理是独立话题（见 Shader 变体系列）。

---

## 采样次数：控制纹理采样的代价

### 纹理采样为什么慢

纹理采样（`SAMPLE_TEXTURE2D`）是 Fragment Shader 里最常见的高代价操作：
1. 需要计算 MIP Level（LOD）
2. 从缓存或内存读取 texel 数据（可能触发 Cache Miss）
3. 执行双线性或三线性过滤插值

在移动端，如果 Cache Miss 频繁（纹理尺寸大、随机 UV 访问），纹理采样的延迟可以高达数百个 ALU 指令的等效时间。

---

### 减少采样次数的实用策略

**① 合并贴图通道（Texture Packing）**

把多张单通道 Mask 合并成一张 RGBA 贴图：

```hlsl
// 原来：4 次采样，4 张贴图
half roughness  = SAMPLE_TEXTURE2D(_RoughnessTex, s, uv).r;
half metallic   = SAMPLE_TEXTURE2D(_MetallicTex, s, uv).r;
half ao         = SAMPLE_TEXTURE2D(_AOTex, s, uv).r;
half emission   = SAMPLE_TEXTURE2D(_EmissionMask, s, uv).r;

// 优化：1 次采样，打包到一张贴图
half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, s, uv);
half roughness = maskMap.r;
half metallic  = maskMap.g;
half ao        = maskMap.b;
half emission  = maskMap.a;
```

4 次采样变 1 次，Cache 命中率更高（同一贴图的相邻 texel 通常在同一 Cache Line 里）。

**② 避免在 Fragment Shader 里重复采样同一贴图**

```hlsl
// ❌ 两次采样同一贴图
half alpha = SAMPLE_TEXTURE2D(_MainTex, s, uv).a;
half3 albedo = SAMPLE_TEXTURE2D(_MainTex, s, uv).rgb;

// ✅ 一次采样，拆分通道
half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, s, uv);
half3 albedo = mainTex.rgb;
half alpha = mainTex.a;
```

**③ 低频信息用顶点插值代替片元采样**

渐变色、简单光照可以在 Vertex Shader 里计算，结果通过 varying 插值到 Fragment Shader，避免额外的纹理采样：

```hlsl
// Vertex Shader：计算环境光遮蔽（简化版）
output.ambientOcclusion = ComputeSimpleAO(positionWS, normalWS);

// Fragment Shader：直接用插值结果，无采样
half ao = input.ambientOcclusion;
```

**④ 控制 MIP Bias，避免不必要的高分辨率 MIP 采样**

远景物体自动采样低分辨率 MIP，Cache 友好；近景物体采样高分辨率 MIP，可能 Cache Miss。对于远景专用的 LOD Shader，可以手动指定 MIP Level：

```hlsl
// 强制采样 MIP 2，适合远景 LOD Shader
half4 col = SAMPLE_TEXTURE2D_LOD(_MainTex, s, uv, 2.0);
```

---

## 综合示例：一个优化前后对比

**优化前**：

```hlsl
float4 frag(Varyings i) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
    float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, i.uv).r;
    float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, i.uv).r;
    float ao = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, i.uv).r;

    float3 normalWS = UnpackNormal(normal);
    float NdotL = saturate(dot(normalWS, _MainLightDir));

    float3 diffuse = NdotL * albedo.rgb * (1.0 - metallic);
    return float4(diffuse * ao, albedo.a);
}
```

**优化后**：

```hlsl
half4 frag(Varyings i) : SV_Target
{
    // 1 次采样替代 3 次（Texture Packing）
    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
    half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv); // roughness/metallic/ao packed

    // half 精度（方向向量、光照系数）
    half3 normalWS = normalize(mul((half3x3)i.TBN, normalTS));
    half NdotL = saturate(dot(normalWS, (half3)_MainLightDir));

    half roughness = maskMap.r;
    half metallic  = maskMap.g;
    half ao        = maskMap.b;

    half3 diffuse = NdotL * albedo.rgb * (1.0h - metallic);
    return half4(diffuse * ao, albedo.a);
}
```

改动：
- 采样次数：5 次 → 3 次（3 张 Mask 合并为 1 张 MaskMap）
- 精度：Fragment 内中间量全部改为 half
- 世界坐标变换移到 Vertex Shader（略）

---

## 小结

- **精度**：颜色、法线方向、光照系数用 `half`；世界坐标、矩阵变换、深度值保持 `float`；`_Time` 参与计算时注意 half 范围限制
- **分支**：编译期关键字分支零代价；Uniform 分支（条件对 Warp 内所有像素相同）低代价；逐像素分支产生 Divergence，代价最高
- **采样**：合并贴图通道（Texture Packing）是最直接的减采样手段；避免在同一 Shader 里重复采样同一贴图；低频信息移到 Vertex Shader 插值
- 优化验证：改完在真机上用 Snapdragon Profiler / Xcode 对比 `SP Active` 和 `Texture Active` 的比例变化
