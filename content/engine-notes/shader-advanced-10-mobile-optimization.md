---
title: "Shader 进阶技法 10｜移动端 Shader 完整优化检查表"
slug: "shader-advanced-10-mobile-optimization"
date: "2026-03-26"
description: "移动端 GPU 的架构（TBDR）、带宽限制、ALU 吞吐量与 PC 差异很大。这篇整理一份可操作的 Shader 优化检查表：数据类型、纹理采样、透明代价、变体控制、CBUFFER、后处理取舍。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "移动端"
  - "优化"
  - "性能"
series: "Shader 手写技法"
weight: 4380
---
移动端 GPU（Mali、Adreno、Apple GPU）普遍采用 **TBDR（Tile-Based Deferred Rendering）** 架构，与 PC 的 IMR 差异很大。理解这些差异，才能写出真正高效的移动端 Shader。

---

## 一、数据精度：优先用 half

移动端 GPU 的 half（16位）ALU 吞吐量通常是 float（32位）的 **2 倍**。

### 规则

| 数据类型 | 用 float | 用 half |
|---------|---------|--------|
| 顶点位置、坐标变换 | ✅ 必须 | ❌ 精度不足 |
| 法线、切线（世界空间） | ✅ 推荐 | 可用，局限较小误差 |
| UV 坐标 | ✅ | ❌ 远离原点时精度丢失 |
| `_Time.y` | ✅ 必须 | ❌ 运行时超过 half 最大值 65504 |
| 颜色（漫反射、高光） | half ✅ | — |
| 纹理采样结果 | half ✅ | — |
| lerp/saturate 插值系数 | half ✅ | — |
| 深度值（LinearEyeDepth 等） | float ✅ | ❌ 精度不足 |

### 实操

```hlsl
// ✅ 好
half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
half  NdotL  = saturate(dot(normalWS, lightDir));
half3 color  = albedo * _BaseColor.rgb * NdotL;

// ❌ 不必要的 float
float3 albedo = SAMPLE_TEXTURE2D(...).rgb;  // 采样结果用 float 无收益
```

---

## 二、纹理采样：减少次数，合理打包

纹理采样是移动端带宽消耗的主要来源。

### 规则

**打包相关数据到同一张贴图的不同通道：**

```
金属度/粗糙度贴图（MaskMap）：
    R = Metallic
    G = Occlusion
    B = Detail Mask
    A = Smoothness

皮肤透射 + 自发光打包：
    R = Thickness（厚度）
    G = Emission Mask
```

**能合并的采样合并：**
```hlsl
// ❌ 两次采样
half metallic   = SAMPLE_TEXTURE2D(_MetallicMap, ..., uv).r;
half smoothness = SAMPLE_TEXTURE2D(_SmoothnessMap, ..., uv).r;

// ✅ 一次采样，两个通道
half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
half metallic   = mask.r;
half smoothness = mask.a;
```

**按需开关纹理采样（用变体）：**
```hlsl
shader_feature_local _NORMALMAP
shader_feature_local _EMISSIONMAP

#ifdef _NORMALMAP
    half3 n = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, ...));
#else
    half3 n = half3(0, 0, 1);
#endif
```

### 采样次数参考上限

| 档位 | 推荐采样上限（Fragment Shader） |
|------|-------------------------------|
| 高档 | 8 次以内 |
| 中档 | 5 次以内 |
| 低档 | 3 次以内 |

---

## 三、透明与混合代价

TBDR 架构的 Tile 内存操作对透明物体特别敏感。

**透明层数限制（Overdraw）：**

每个屏幕像素被多层透明覆盖时，每层都需要读取 + 混合颜色。

| 档位 | 透明层数上限 | 备注 |
|------|------------|------|
| 高档 | 4 层 | |
| 中档 | 2 层 | |
| 低档 | 1 层 | 尽量用 Alpha Test 替代 Alpha Blend |

**Alpha Test vs Alpha Blend：**

| 方式 | 代价 | 说明 |
|------|------|------|
| Alpha Test（clip） | 中 | 丢弃像素，破坏 TBDR 提前深度测试（Early-Z），中档以下慎用大面积 clip |
| Alpha Blend | 高（带宽） | 需要读写颜色 RT，透明层多时带宽成倍增加 |
| Dithered Alpha | 低（仅 Opaque） | 用 Bayer 噪声伪装透明，走 Opaque 管线 |

```hlsl
// Dithered Alpha（移动端中低档推荐）
float2 pos = input.hcs.xy % 4;          // 4×4 Bayer 矩阵位置
float  bayer = _BayerMatrix[pos.y][pos.x];  // 预存的 Bayer 阈值 0~1
clip(albedo.a - bayer);                 // 走 Opaque，无混合代价
```

---

## 四、CBUFFER 规范：避免 uniform 溢出

移动端 GPU 的 Constant Buffer 寄存器有限（通常 64~128 个 float4）。超出限制时 uniform 溢出到内存，访问代价大幅上升。

**规则：**
- 所有材质属性放入 `CBUFFER_START(UnityPerMaterial) ... CBUFFER_END`
- 不要声明用不到的属性（声明即占槽位）
- float4 对齐：单个 float/half 参数声明后会填充到 float4，尽量把同类参数打包

```hlsl
// ✅ 好：打包排列，减少填充浪费
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;          // 16 bytes
    float4 _EmissionColor;      // 16 bytes
    float  _Metallic;           // ┐
    float  _Smoothness;         //  ├ 打包成一个 float4 的前 2 个分量
    float  _OcclusionStrength;  //  │
    float  _Cutoff;             // ┘ → 刚好 16 bytes
CBUFFER_END

// ❌ 不好：随意声明 float，每个 float 独占一个 float4 槽位
float _Metallic;    // 占 float4 = 16 bytes，浪费 12 bytes
float _Smoothness;  // 同上
```

---

## 五、Shader 变体控制

变体爆炸直接导致构建时间增加、包体变大、加载时 Shader 编译卡顿。

**规则：**

| 规则 | 说明 |
|------|------|
| 材质级开关用 `shader_feature_local` | 不使用的变体不打包 |
| 全局级开关用 `shader_feature`（不加 `_local`） | 整个项目可开关 |
| 避免 `multi_compile` 打无用变体 | 宁可手写几个 include，不用 multi_compile 组合爆炸 |
| 限制自定义 `multi_compile` 总数 | 每加一个 keyword，变体数翻倍 |

**变体数量估算：**
```
总变体数 = 2^(multi_compile_keyword数) × 其他组合
例：3个 multi_compile keywords → 8 个变体
    5个 → 32 个
```

**变体裁剪（Shader Stripping）：**
在 Graphics Settings → Shader Stripping 里关闭不用的内置 keyword（如 Lightmap Modes、Fog Modes），或实现 `IPreprocessShaders` 接口自动剔除。

---

## 六、分支与计算

**避免像素值驱动的分支：**

```hlsl
// ❌ Warp Divergence：每个像素的 albedo.a 不同，分支无法合并
if (albedo.a > 0.5)
    color = ...;
else
    color = ...;

// ✅ 用 step/lerp 消除分支
float mask = step(0.5, albedo.a);
color = lerp(colorA, colorB, mask);
```

**pow 的代价：**
`pow(x, n)` 在移动端代价较高，尽量用以下替代：
```hlsl
pow(x, 2)  → x * x
pow(x, 4)  → x*x * x*x（mul 代替 pow）
pow(x, 3)  → x*x * x
```

**normalize 的代价：**
插值后的法线才需要 normalize。如果法线来自纹理解包，`UnpackNormal` 已经归一化了。

---

## 七、后处理取舍

后处理每个效果至少 1 次全屏 Blit，对移动端带宽消耗很大。

| 效果 | 代价 | 移动端建议 |
|------|------|----------|
| URP 内置 Bloom | 高（多次降采样/上采样） | 仅高档使用，或用自定义低质量版 |
| Color Grading（LUT） | 低（单次查表） | 全档位可用 |
| Depth of Field | 高 | 仅高档或过场景 |
| Motion Blur | 高（多次采样） | 移动端关闭 |
| SSAO | 高 | 仅高档 |
| SSR | 很高 | 移动端关闭，Fallback 反射探针 |
| 简单色差/扫描线 | 低 | 可用 |
| MSAA | 低（TBDR 原生支持） | 优于 TAA，优先 MSAA 2x |

---

## 八、TBDR 架构特有优化

移动端 TBDR GPU 特性：

**Early-Z 和 HSR（Hidden Surface Removal）：**
- 从近到远绘制不透明物体，让 TBDR 尽早丢弃遮挡像素
- 避免在 Fragment Shader 里写 `clip()`（破坏 Early-Z）或修改深度
- `Cull Off` 双面渲染会让 TBDR 无法做正确的背面剔除

**Framebuffer Fetch：**
- TBDR 的 Tile 内存读写几乎免费，`Blend` 操作代价远低于 IMR
- 避免频繁切换 Render Target（每次切换都要 flush Tile 内存到主内存）

**带宽：**
- 移动端内存带宽通常只有 PC 的 1/10
- 减少 RT 数量（G-Buffer 层数）、使用 R11G11B10 或 RGBA16F 而非 RGBA32F

---

## 九、完整检查清单

写完一个 Shader 后，逐项过一遍：

```
数据精度
  □ 颜色/光照计算用 half
  □ 坐标变换/UV/深度用 float
  □ _Time.y 用 float

纹理采样
  □ 相关数据打包到同一贴图不同通道
  □ 没有重复采样同一贴图
  □ 用 shader_feature_local 控制可选纹理

透明
  □ 透明物体尽量少层叠加
  □ 考虑 Dithered Alpha 代替 Alpha Blend

CBUFFER
  □ 所有参数在 CBUFFER_START/END 内
  □ float 参数尽量打包（4 个一组）
  □ 没有未使用的属性声明

变体
  □ 材质级开关用 shader_feature_local
  □ 没有多余的 multi_compile
  □ 检查总变体数是否合理

计算
  □ 没有像素值驱动的 if 分支
  □ pow(x,2/3/4) 改用乘法
  □ 不必要的 normalize 已移除

后处理
  □ 高代价效果（Bloom/DOF/SSR）仅高档开启
  □ 全档位效果（LUT/简单色差）已确认代价低
```

---

## 小结

移动端 Shader 优化没有"一个技巧搞定所有问题"——需要从精度、采样、透明、变体、CBUFFER 多个维度同时把控。建议每个项目维护一套分档策略（高/中/低），并在真实设备上用 GPU 性能分析工具（Adreno Profiler、Mali Graphics Debugger、Xcode GPU Frame Capture）验证数据。

这篇是 Shader 手写技法进阶层的最后一篇。掌握这些进阶技法后，可以进入**项目实战层**：把理论运用到完整的卡通角色、写实武器、水面、草地等项目级 Shader 中。
