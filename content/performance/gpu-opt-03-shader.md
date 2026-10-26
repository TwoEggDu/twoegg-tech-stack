---
title: "GPU 渲染优化 03｜Shader 优化：精度、分支与采样次数"
slug: "gpu-opt-03-shader"
date: "2026-03-25"
lastmod: "2026-10-26"
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
Shader 的精度、分支与采样是可以比较的优化方向，但改一行代码不保证能测出帧时间变化：编译器可能已消除相关工作，瓶颈也可能在别处。需要把源码、编译结果、画质与真机工作负载一起看。

> 版本与证据范围：精度说明参考 Unity 6（6000.0）公开文档。下文代码为教学片段，未在完整 Shader 中编译或进行设备性能对照，不能当作优化成功的记录。

---

## 精度：half vs float

### 移动端 GPU 的精度支持

Unity 着色代码中的精度声明需要结合编译目标理解：

- `float` 通常用于 32-bit 运算。
- `half` 用于表达较低精度需求；实际是否使用 16-bit 运算受平台、编译器和 Shader Precision Model 影响。IEEE binary16 的有限范围约为 ±65504，约 3 位十进制有效数字，不是固定保留 3 位小数。
- 旧代码的 `fixed` 与 GLSL `lowp` 不能当作所有后端完全相同的数据格式，迁移时应检查实际映射。

较低精度可能改善下面几类成本，但不承诺固定提速倍数：

**① ALU 吞吐量**：部分架构/指令支持更高的低精度吞吐；是否被编译器采用、该 Shader 是否受 ALU 限制，要检查产物和测量。

**② 寄存器压力**：较低精度有机会减少寄存器占用；打包、活跃区间与分配规则影响结果，不能由类型名直接推出寄存器减半。

**③ 数据传递**：varying 和缓冲布局需要分别检查，`half` 声明不保证所有传递数据都减半。Unity 对 CPU 可见缓冲中的布局还可能使用 32-bit 大小与对齐，见 [16-bit precision](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-Use16BitPrecisionInShaders.html)。

---

### 哪些计算可以尝试 half，哪些先保留 float

**适合用 half 的**：

```hlsl
// 颜色值（0~1范围，精度够用）
half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
half3 normalWS = normalize(i.normalWS); // 法线方向

// 光照中间计算（方向向量、光照系数）
half NdotL = saturate(dot(normalWS, lightDirWS));
half3 diffuse = NdotL * _LightColor.rgb * albedo.rgb;

// UV：即使在 0~1，大贴图的单 texel 精度也可能不足，先保留 float
float2 uv = i.uv;
```

**通常优先保留 float 的**：

```hlsl
// 世界空间坐标（数值大，half 精度不够，会有抖动）
float3 positionWS = i.positionWS;

// 矩阵变换（特别是 VP 矩阵乘法）
float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

// 深度值（范围大，half 精度不足，会有深度冲突）
float depth = i.positionCS.z / i.positionCS.w;

// 高精度 UV 动画（大数值 _Time 参与的计算）
float scrollU = _Time.y * _ScrollSpeed; // 长时间运行先关注精度，再检查范围
```

**容易踩坑的地方**：

```hlsl
// 转成 binary16 会量化：数值增大时可能先丢失小时间增量
half t = _Time.y * 10.0; // 理论溢出边界约 6550 秒（109 分钟），不是几秒

// 对周期效果可先用 float 计算相位，再按误差预算降精度
half phase = (half)frac(_Time.y * _CycleSpeed); // 长时间 float 精度和相位跳变仍需检查
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

GPU 会按一组执行通道组织 Shader 工作。Warp/Wave 等描述的执行组，不能与用于片元导数等操作的 2×2 quad 混为一谈；具体组宽和调度因 GPU 架构而异。

这里按组内通道是否一致来讨论分支；quad 与 wave 的术语区别可参照 [Microsoft HLSL Shader Model 6](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/hlsl-shader-model-6-0-features-for-direct3d-12)，不以此规定移动 GPU 的组宽。

**分支的问题**：当同一执行组的通道走不同路径时，硬件可能分阶段执行这些路径，并屏蔽当前不活跃的通道；编译器也可能使用选择/谓词等方式。这会降低有效并行度，但不是所有分支都固定付出两条路径的简单相加成本。

```
// 示意：同组通道的 condition 不一致，实际编译方式需检查
if (condition) {
    result = expensive_branch_A(); // 这一路径的活跃通道参与
} else {
    result = expensive_branch_B(); // 另一路径的活跃通道参与
}
// 不能只按源码分支数推算实际耗时
```

这叫 **Divergence（分叉）**，是 GPU 分支代价高的根本原因。

---

### 哪种分支代价低

**编译期常量选择**：

```hlsl
// Shader 关键字控制的分支，编译时确定，不同变体编译成不同代码
#ifdef _ENABLE_DETAIL_MAP
    half3 detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uv).rgb;
    albedo.rgb = albedo.rgb * detail * 2.0;
#endif
```

这种独立二值开关可产生两个候选变体，分支选择在编译期确定；未使用变体是否被剔除另看构建。运行时没有该动态分支，不等于变体管理、程序切换和保留路径零成本。

**Uniform 分支（组内条件一致）**：

```hlsl
// _QualityLevel 是每帧固定的 uniform，Warp 内所有像素条件相同
if (_QualityLevel > 1.0) {
    // 所有像素同时走这里，无 Divergence
}
```

同组通道条件一致通常能避免分歧，GPU/编译器可能跳过另一条路径；仍需看编译结果，不能统一宣布代价接近零。

**逐像素分支（可能产生分歧）**：

```hlsl
// 依赖 UV 或贴图采样结果的分支，不同像素条件不同，产生 Divergence
if (tex.a > 0.5) {
    // 条件在同组内不同时，可能产生分歧
}
```

---

### 用 step / lerp 替代分支

对于简单条件，可以比较 `step` / `lerp` 与 `if` 的编译结果，而不是预设哪一种更快：

```hlsl
// 写法 A：可能被编译为分支或条件选择
half3 color;
if (mask > 0.5) color = colorA;
else            color = colorB;

// 写法 B：表达为数学选择，编译结果仍由目标决定
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

代价：新增一个独立二值关键字组会使理论组合数翻倍；多值组、约束与剔除会改变实际数量。关键字治理见相关变体专题。

---

## 采样次数：控制纹理采样的代价

### 纹理采样为什么慢

纹理采样（`SAMPLE_TEXTURE2D`）是 Fragment Shader 里最常见的高代价操作：
1. 需要计算 MIP Level（LOD）
2. 从缓存或内存读取 texel 数据（可能触发 Cache Miss）
3. 执行双线性或三线性过滤插值

缓存未命中、过滤方式、纹理格式和并发工作都会影响代价；一次采样不能换算成跨设备固定数量的 ALU 指令。纹理单元忙也不单独证明外部带宽饱和。

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

源码中四个读取可写成一次取 RGBA，但应确认这些数据适合共用尺寸、UV、过滤与加载范围，且 Shader 确实一起使用它们。Mask 属于数值数据，通常应关闭 sRGB 颜色解释；各通道的含义必须与 Shader 一致。压缩后字节数、误差、缓存命中和实际指令需要另查，不能仅凭贴图数量推断收益。

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

相同资源、采样器、坐标与 LOD 的重复读取也可能由编译器合并；写成一次读取更明确，但不保证编译产物一定减少一条采样。

**③ 低频信息用顶点插值代替片元采样**

渐变等低频信息可尝试在顶点阶段计算后插值；要同时检查顶点数量、varying 成本和插值误差，非线性计算通常不与逐片元计算等价：

```hlsl
// Vertex Shader：计算环境光遮蔽（简化版）
output.ambientOcclusion = ComputeSimpleAO(positionWS, normalWS);

// Fragment Shader：直接用插值结果，无采样
half ao = input.ambientOcclusion;
```

**④ 控制 MIP Bias，避免不必要的高分辨率 MIP 采样**

远景物体自动采样低分辨率 MIP，Cache 友好；近景物体采样高分辨率 MIP，可能 Cache Miss。对于远景专用的 LOD Shader，可以手动指定 MIP Level：

```hlsl
// 显式采样 MIP 2：只是实验对照，不随投影自动选层
half4 col = SAMPLE_TEXTURE2D_LOD(_MainTex, s, uv, 2.0);
```

---

## 综合示例：两种候选写法

下列片段省略完整输入结构与 Shader 包装，只比较源码表达；未提供编译指令数或耗时结果。

**分散纹理的写法**：

```hlsl
float4 frag(Varyings i) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    float4 normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
    float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, i.uv).r;
    float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, i.uv).r;
    float ao = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, i.uv).r;

    float3 normalTS = UnpackNormal(normal);
    float3 normalWS = normalize(mul((float3x3)i.TBN, normalTS));
    float NdotL = saturate(dot(normalWS, _MainLightDir));

    float3 diffuse = NdotL * albedo.rgb * (1.0 - metallic);
    return float4(diffuse * ao, albedo.a);
}
```

**打包纹理的候选写法**：

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

源码差别：
- 读取表达式：5 处 → 3 处，但此简化公式没有使用 roughness，编译器可能删除相关读取，不能把源码计数当实际指令计数。
- 候选版本降低部分中间量精度，两个版本均将切线空间法线转换到世界空间，避免用不同坐标系制造虚假的画质/成本对照。
- `i.TBN` 等输入需要完整顶点阶段提供；这里只给片段，不声明脚本/Shader 已编译或两版速度不同。

---

## 小结

- **精度**：颜色、法线方向、光照系数用 `half`；世界坐标、矩阵变换、深度值保持 `float`；`_Time` 参与计算时注意 half 范围限制
- **分支**：区分编译期选择、组内一致与分歧路径；不能由 if/lerp 或分支位置直接给耗时排序。
- **采样**：合并贴图通道（Texture Packing）是最直接的减采样手段；避免在同一 Shader 里重复采样同一贴图；低频信息移到 Vertex Shader 插值
- 优化验证：固定版本、变体、画面和设备条件；编译报告解释工作，重复采样确认 GPU 时间与误差，计数器名称按实际工具文档读取。
