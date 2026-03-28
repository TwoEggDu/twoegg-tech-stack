---
title: "游戏常用效果｜树木渲染：LOD、Billboard 与 SpeedTree 原理"
slug: "shader-scene-02-tree"
date: "2026-03-28"
description: "从顶点色编码风力权重到 Cross Billboard LOD，拆解树木渲染的三层风力动画、叶片 alpha clip 双面材质，以及 SpeedTree 的核心技术思路。"
tags: ["Shader", "HLSL", "URP", "场景", "树木渲染", "Billboard", "LOD", "SpeedTree"]
series: "Shader 手写技法"
weight: 4570
---

树木是开放世界场景里密度最高、性能压力最大的渲染对象之一。一棵树可能有数千个叶片三角形，都带 alpha 通道；几千棵树同时在视野内，排序、批次、风力动画全部叠加在一起。SpeedTree 能把这些问题处理得相当好，但它的核心思路并不神秘——理解顶点色风力编码、三层动画分解和 Billboard LOD 的原理，就能手写出接近商业品质的树木 Shader。

---

## 叶片渲染：双面 + Alpha Clip

树叶通常是一张四边形（quad），正反两面都需要可见。在 Pass 级别加 `Cull Off` 即可，不需要在 Shader 里手动处理背面法线——URP 在光照计算前会通过内置的 `FRONT_FACE_SEMANTIC` 翻转背面法线方向。

透明度处理用 alpha clip 而非半透明混合。半透明需要排序，几千个叶片四边形之间的排序开销在移动端几乎不可接受；alpha clip 只需一条 `clip()` 指令，没有排序代价，代价是边缘会有锯齿（用 MSAA 或 TAA 缓解）。

```hlsl
// 叶片 Fragment Shader 核心
half4 LeafFragment(Varyings input) : SV_Target
{
    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

    // Alpha Clip：边缘锯齿比排序 overdraw 更划算
    clip(col.a - _Cutoff);

    InputData lightingInput = (InputData)0;
    lightingInput.normalWS        = normalize(input.normalWS);
    lightingInput.positionWS      = input.positionWS;
    lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surface = (SurfaceData)0;
    surface.albedo     = col.rgb;
    surface.alpha      = 1.0;
    surface.smoothness = 0.1; // 叶片表面粗糙，不光滑

    return UniversalFragmentPBR(lightingInput, surface);
}
```

叶片 Pass 设置：

```hlsl
Pass
{
    Name "UniversalForward"
    Tags { "LightMode" = "UniversalForward" }
    Cull Off          // 双面渲染
    AlphaToMask On    // 配合 MSAA 改善 alpha clip 边缘
    ZWrite On
}
```

---

## SpeedTree 核心：顶点色编码风力权重

SpeedTree 最聪明的设计是用顶点色存储风力响应系数，而不是在运行时靠几何结构推算：

- **R 通道**：主干弯曲权重（0 = 根部固定，1 = 梢部最大弯曲）
- **G 通道**：枝条边缘颤动权重（叶片所在枝条的摆动幅度）
- **B 通道**：叶片自身抖动权重（单片叶子的随机翻转）
- **A 通道**（可选）：AO 遮蔽或其他自定义数据

这套编码在美术侧的 SpeedTree 编辑器里绘制，导出时烘焙进 FBX 顶点色。自定义树木资产如果没有这套数据，可以在 DCC 软件里手绘，或者用高度（Y 坐标相对于根部的归一化值）粗略替代主干权重，G/B 通道手动赋值。

---

## 三层风力动画

SpeedTree 把风力分解为三层，分别作用于不同频率和幅度：

**第一层：主干整体弯曲（Primary Bend）**

整棵树沿风向低频摆动，模拟树冠受风整体倾斜。使用低频正弦波，幅度较大：

```hlsl
// 主干弯曲：低频大幅摆动，世界坐标引入相位差避免所有树同步
float primaryBend = sin(_Time.y * _WindPrimaryFreq + worldPos.x * 0.3)
                  * _WindStrength * bendWeight;
worldPos += _WindDirection.xyz * primaryBend;
```

**第二层：枝条边缘颤动（Edge Flutter）**

枝条末端的中频摆动，频率是主干的 2～4 倍，方向偏向风向两侧，产生拂动感：

```hlsl
// 枝条颤动：中频，与风向垂直的侧向摆动
float3 sideDir = normalize(cross(_WindDirection.xyz, float3(0, 1, 0)));
float  flutter = sin(_Time.y * _WindFlutterFreq + worldPos.z * 1.2 + worldPos.x * 0.7)
               * _WindFlutterStrength * flutterWeight;
worldPos += sideDir * flutter;
```

**第三层：叶片抖动（Leaf Tremble）**

单片叶子的高频随机抖动，模拟叶面受局部气流翻转。用本地坐标引入相位差，避免所有叶片同步：

```hlsl
// 叶片抖动：高频随机翻转，每片叶子相位不同
float phase   = dot(input.positionOS.xyz, float3(1.7, 3.1, 2.3));
float tremble = sin(_Time.y * _WindTrembleFreq + phase)
              * _WindTrembleStrength * trembleWeight;
worldPos.y += tremble;
```

---

## 完整顶点风力 Shader

```hlsl
Varyings TreeVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    // 顶点色：R = 主干弯曲权重, G = 枝条颤动, B = 叶片抖动
    float bendWeight    = input.color.r;
    float flutterWeight = input.color.g;
    float trembleWeight = input.color.b;

    float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

    // --- 第一层：主干弯曲 ---
    float primaryBend = sin(_Time.y * _WindPrimaryFreq + worldPos.x * 0.3)
                      * _WindStrength * bendWeight;
    worldPos += _WindDirection.xyz * primaryBend;

    // --- 第二层：枝条颤动 ---
    float3 sideDir = normalize(cross(_WindDirection.xyz, float3(0, 1, 0)));
    float  flutter = sin(_Time.y * _WindFlutterFreq + worldPos.z * 1.2 + worldPos.x * 0.7)
                   * _WindFlutterStrength * flutterWeight;
    worldPos += sideDir * flutter;

    // --- 第三层：叶片抖动 ---
    float phase   = dot(input.positionOS.xyz, float3(1.7, 3.1, 2.3));
    float tremble = sin(_Time.y * _WindTrembleFreq + phase)
                  * _WindTrembleStrength * trembleWeight;
    worldPos.y += tremble;

    output.positionCS = TransformWorldToHClip(worldPos);
    output.positionWS = worldPos;
    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
    output.uv         = input.uv;
    return output;
}
```

三层动画的参数建议范围：

| 参数 | 建议值 | 说明 |
|------|--------|------|
| `_WindPrimaryFreq` | 0.5～1.0 | 主干低频，约 0.5～1 次/秒 |
| `_WindStrength` | 0.3～0.8 | 主干最大位移（米） |
| `_WindFlutterFreq` | 2.0～4.0 | 枝条中频 |
| `_WindFlutterStrength` | 0.05～0.15 | 枝条侧向摆动 |
| `_WindTrembleFreq` | 6.0～12.0 | 叶片高频抖动 |
| `_WindTrembleStrength` | 0.02～0.06 | 叶片翻转幅度 |

---

## Billboard LOD

远距离的树用完整 Mesh 渲染是浪费。**Billboard** 用一张始终朝向相机的四边形替代，贴上从多角度预渲染的树木图片。

普通 Billboard 的问题：相机绕树转动时，贴图会发生旋转跳变（因为只有有限的几张角度图）。在开放世界里，这种跳变非常明显。

**Cross Billboard** 是更稳定的方案：用两张正交放置的四边形交叉构成"十字"，各贴同一张纹理。任何角度看过去都至少有一张四边形正对相机，视觉上不会完全退化为一条线，也没有旋转跳变。Cross Billboard 不需要朝向相机的旋转矩阵，直接走正常 MVP 变换即可：

```hlsl
// Cross Billboard：两张正交四边形，模型空间已正交放置
// 不需要旋转逻辑，走标准变换
Varyings CrossBillboardVert(Attributes input)
{
    Varyings output;
    float3 worldPos   = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(worldPos);
    output.uv         = input.uv;
    return output;
}

half4 CrossBillboardFrag(Varyings input) : SV_Target
{
    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    clip(col.a - _Cutoff);
    // 极远距离通常不做完整 PBR，用简单漫反射
    return col * _Color;
}
```

如果需要真正朝向相机的单张 Billboard（适合 200m 以上极远距离），在顶点着色器里从 View 矩阵提取相机方向构造四边形：

```hlsl
// 相机对齐 Billboard：从 View 矩阵提取相机右/上方向
float3 BillboardWorldPos(float2 localXY, float3 pivotWorld)
{
    // UNITY_MATRIX_V 的列向量是视空间基向量（世界空间）
    float3 camRight = float3(UNITY_MATRIX_V[0][0], UNITY_MATRIX_V[1][0], UNITY_MATRIX_V[2][0]);
    float3 camUp    = float3(UNITY_MATRIX_V[0][1], UNITY_MATRIX_V[1][1], UNITY_MATRIX_V[2][1]);
    return pivotWorld + camRight * localXY.x + camUp * localXY.y;
}
```

---

## LOD 切换策略与 GPU Instancing

一棵树的完整 LOD 链通常是：

1. **LOD 0**（0～30m）：完整 Mesh，三层风力动画，全质量 PBR 光照
2. **LOD 1**（30～80m）：简化 Mesh（叶片数减半），只保留主干弯曲动画
3. **LOD 2**（80～150m）：最简 Mesh 或 Cross Billboard，无顶点动画
4. **Billboard**（150m+）：单张朝向相机 Billboard，alpha clip，极低面数

LOD 切换时的"蹦级"感可以用 Dither LOD Crossfade 缓解。URP 内置 `LODDitheringTransition` 宏，在过渡带用屏幕空间抖动混合两个 LOD 级别：

```hlsl
// Shader 变体声明
#pragma multi_compile _ LOD_FADE_CROSSFADE

// Fragment Shader 开头加入淡出支持
#ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(input.positionCS.xy, unity_LODFade.x);
#endif
```

GPU Instancing 对树木非常关键——同一种树 Mesh 的所有实例合并到一个 DrawCall，配合 LOD 切换，几千棵树的批次数可以控制在几十个以内。确保材质开启 `Enable GPU Instancing`，Shader 里正确声明 per-instance 缓冲区：

```hlsl
#pragma multi_compile_instancing

UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(Props)
```

使用 GPU Instancing 时避免在运行时修改单棵树的材质属性（比如修改某棵树的颜色），否则会破坏批次合并，直接把 DrawCall 数打回到逐实例。需要单棵差异时，用 `UNITY_ACCESS_INSTANCED_PROP` 在 per-instance 缓冲区里传参，而不是修改材质本身。
