---
title: "项目实战 04｜草地系统 Shader：风吹动画 + 交互弯曲 + LOD 策略"
slug: "shader-project-04-grass-system"
date: "2026-03-26"
description: "游戏中的草地需要：风吹摇摆（顶点动画）、玩家踩踏时弯曲（运行时交互）、远处简化（LOD 淡出）。这篇用顶点色存储弯曲权重，结合 C# 传入的交互数据，实现完整的草地系统 Shader。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "项目实战"
  - "草地"
  - "顶点动画"
  - "交互"
  - "LOD"
series: "Shader 手写技法"
weight: 4430
---
草地 Shader 需要解决三个问题：**风吹动画**（让草自然摇摆）、**交互弯曲**（玩家经过时压倒草）、**远距离 LOD 淡出**（节省性能）。这三个问题分别在 Shader 的顶点阶段、顶点阶段和 Fragment 阶段处理。

---

## 整体思路

```
草地顶点位移 =
    风吹基础摆动（sin 波 × 高度权重）
    + 全局风噪声（Wind Noise 贴图，低频漩涡）
    + 交互弯曲（C# 传入接触点，距离衰减）

草地片元 =
    双面光照（Cull Off）
    + 顶点色 Translucency（透光感）
    + 距离 Alpha 淡出（LOD 渐隐）
```

---

## 模块一：顶点色权重

草地模型的顶点色决定每个顶点的弯曲程度：

```
顶点色 R = 风吹权重（草根 = 0，草尖 = 1）
顶点色 G = 弯曲影响权重（同上，或单独控制）
顶点色 B = 草地丰茂程度（可用于 LOD 随机）
顶点色 A = 透光强度
```

草根（Y = 0）权重 = 0（固定不动），草尖权重 = 1（最大摆动）。这保证草从根部弯曲，而不是整体平移。

```hlsl
// 顶点色 R 通道作为弯曲权重
float windWeight = input.color.r;   // 0=根部，1=草尖
```

---

## 模块二：风吹动画

两层风叠加，产生自然的随机感：

```hlsl
// 全局风方向和强度
float3 windDir   = normalize(float3(_WindDirX, 0, _WindDirZ));
float  windSpeed = _WindSpeed;
float  windStrength = _WindStrength;

// 低频大波（整体摆动方向）
float2 windNoiseUV  = posWS.xz * _WindNoiseScale + _Time.y * windSpeed * 0.5;
float  windNoise    = SAMPLE_TEXTURE2D_LOD(_WindNoiseTex, sampler_WindNoiseTex, windNoiseUV, 0).r;
windNoise = windNoise * 2.0 - 1.0;   // [0,1] → [-1,1]

// 高频小抖动（叶片颤动）
float localJitter = sin(_Time.y * 8.0 + posWS.x * 4.0 + posWS.z * 3.7) * 0.15;

// 合并风力
float windForce = (windNoise + localJitter) * windStrength;

// 沿风向在 XZ 平面位移（乘以高度权重保证根部固定）
float3 windDisplacement = windDir * windForce * windWeight;
posWS.xz += windDisplacement.xz;

// 高度补偿：XZ 位移会导致草变长，用勾股定理近似保持长度
// （简化：忽略 Y 变化，对于小振幅效果足够）
```

---

## 模块三：交互弯曲

玩家/NPC 进入草地时，C# 将接触点位置传入 Shader，草地向外弯曲：

**C# 端：**
```csharp
// 把接触点世界坐标传给 Shader（最多 4 个同时接触点）
Shader.SetGlobalVector("_InteractPos0", new Vector4(pos.x, pos.y, pos.z, radius));
Shader.SetGlobalVector("_InteractPos1", ...);
```

**Shader 端：**
```hlsl
float4 _InteractPos0;  // xyz=位置，w=影响半径

// 计算草顶点到接触点的距离
float3 interactDelta = posWS - _InteractPos0.xyz;
interactDelta.y = 0;  // 只考虑水平距离
float  dist        = length(interactDelta);
float  radius      = _InteractPos0.w;

// 距离越近弯曲越大，超出半径无影响
float  interactFade = saturate(1.0 - dist / radius);
interactFade = smoothstep(0, 1, interactFade);

// 向远离接触点的方向弯曲
float3 pushDir = normalize(interactDelta + float3(0.001, 0, 0.001));
float3 interactDisp = pushDir * interactFade * _InteractStrength * windWeight;

posWS += interactDisp;
```

---

## 模块四：Fragment — 双面光照与透光

草是双面渲染（`Cull Off`），背面光照需要翻转法线：

```hlsl
// SV_IsFrontFace：1=正面，0=背面
half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
{
    float3 normalWS = normalize(input.normalWS);

    // 背面翻转法线
    if (!isFrontFace) normalWS = -normalWS;

    float NdotL = saturate(dot(normalWS, light.direction));

    // 顶点色 A 通道控制透光感：背光时叠加一点透过光的暖色
    half translucency = input.color.a;
    float VdotL       = saturate(dot(viewDir, -light.direction));
    half3 transLight  = pow(VdotL, 3.0) * translucency * light.color * _TranslucentColor.rgb;

    half3 albedo  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
    half3 diffuse = albedo * (light.color * NdotL + transLight + SampleSH(normalWS) * 0.5);

    // ── 距离淡出（LOD Alpha）───────────────────────────────
    float dist     = length(input.posWS - GetWorldSpaceViewDir(input.posWS) * 1e6);  // 到摄像机距离
    float fadeNear = _LODFadeDistance * 0.8;
    float fadeFar  = _LODFadeDistance;
    float alpha    = saturate((fadeFar - length(input.posWS - _WorldSpaceCameraPos)) / (fadeFar - fadeNear));

    clip(alpha - 0.01);  // 完全透明时丢弃

    return half4(diffuse, alpha);
}
```

---

## 完整草地 Shader

```hlsl
Shader "Custom/GrassSystem"
{
    Properties
    {
        _BaseMap         ("Albedo",            2D)          = "white" {}
        _BaseColor       ("Base Color",        Color)       = (0.3,0.6,0.1,1)
        _WindNoiseTex    ("Wind Noise",         2D)          = "gray"  {}
        _WindNoiseScale  ("Wind Noise Scale",  Float)       = 0.1
        _WindStrength    ("Wind Strength",     Range(0,1))  = 0.3
        _WindSpeed       ("Wind Speed",        Float)       = 1.0
        _WindDirX        ("Wind Dir X",        Float)       = 1.0
        _WindDirZ        ("Wind Dir Z",        Float)       = 0.5
        _InteractStrength("Interact Strength", Range(0,2))  = 0.8
        _TranslucentColor("Translucent Color", Color)       = (0.5,0.9,0.3,1)
        _LODFadeDistance ("LOD Fade Distance", Float)       = 50.0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline"
               "Queue" = "AlphaTest" }
        Pass
        {
            Name "GrassForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float  _WindNoiseScale; float _WindStrength; float _WindSpeed;
                float  _WindDirX; float _WindDirZ;
                float  _InteractStrength;
                float4 _TranslucentColor;
                float  _LODFadeDistance;
            CBUFFER_END

            // 交互点（由 C# 通过 Shader.SetGlobalVector 传入）
            float4 _InteractPos0;
            float4 _InteractPos1;

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_WindNoiseTex); SAMPLER(sampler_WindNoiseTex);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings   { float4 hcs:SV_POSITION; float2 uv:TEXCOORD0;
                                float3 normalWS:TEXCOORD1; float3 posWS:TEXCOORD2;
                                float4 shadowCoord:TEXCOORD3; float4 color:COLOR; };

            Varyings vert(Attributes i) {
                Varyings o;
                float windWeight = i.color.r;   // 根部=0，草尖=1

                float3 posWS = TransformObjectToWorld(i.pos.xyz);

                // ── 风吹 ─────────────────────────────────────
                float3 windDir = normalize(float3(_WindDirX, 0, _WindDirZ));
                float2 windUV  = posWS.xz * _WindNoiseScale + _Time.y * _WindSpeed * 0.5;
                float  wNoise  = SAMPLE_TEXTURE2D_LOD(_WindNoiseTex, sampler_WindNoiseTex, windUV, 0).r * 2.0 - 1.0;
                float  jitter  = sin(_Time.y * 8.0 + posWS.x * 4.0 + posWS.z * 3.7) * 0.15;
                float3 windDisp = windDir * (wNoise + jitter) * _WindStrength * windWeight;
                posWS += windDisp;

                // ── 交互弯曲 ──────────────────────────────────
                [unroll]
                for (int idx = 0; idx < 2; idx++) {
                    float4 ip = (idx == 0) ? _InteractPos0 : _InteractPos1;
                    float3 delta = posWS - ip.xyz; delta.y = 0;
                    float  d     = length(delta);
                    float  fade  = smoothstep(ip.w, 0, d);
                    posWS += normalize(delta + 0.0001) * fade * _InteractStrength * windWeight;
                }

                float4 clipPos = TransformWorldToHClip(posWS);
                VertexPositionInputs pi; pi.positionCS = clipPos; pi.positionWS = posWS;
                pi.positionVS = mul(UNITY_MATRIX_V, float4(posWS, 1));
                pi.positionNDC = clipPos;

                o.hcs        = clipPos;
                o.posWS      = posWS;
                o.normalWS   = TransformObjectToWorldNormal(i.n);
                o.shadowCoord = ComputeShadowCoord(clipPos);
                o.uv         = TRANSFORM_TEX(i.uv, _BaseMap);
                o.color      = i.color;
                return o;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                if (!isFrontFace) N = -N;

                float3 V = normalize(GetWorldSpaceViewDir(input.posWS));
                Light  light = GetMainLight(input.shadowCoord);

                half4  albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(albedoTex.a - 0.1);   // Alpha cutout（草的形状）

                half3 albedo  = albedoTex.rgb * _BaseColor.rgb;
                float NdotL   = saturate(dot(N, light.direction));

                // 透光感
                float VdotL    = saturate(dot(V, -light.direction));
                half3 transLight = pow(VdotL, 3.0) * input.color.a * _TranslucentColor.rgb * light.color;

                half3 diffuse = albedo * (light.color * NdotL * light.shadowAttenuation
                                         + transLight + SampleSH(N) * 0.5);

                // LOD 距离淡出
                float camDist = length(input.posWS - _WorldSpaceCameraPos.xyz);
                float fadeA   = saturate((_LODFadeDistance - camDist) / (_LODFadeDistance * 0.2));

                return half4(diffuse, fadeA);
            }
            ENDHLSL
        }
    }
}
```

---

## 草地实例化与密度

大量草地需要 GPU Instancing：
- 使用 `#pragma instancing_options assumeuniformscaling`
- 通过 `Graphics.DrawMeshInstanced` 或 Terrain Detail 系统批量绘制
- 每个实例传入随机旋转/缩放（通过 `UNITY_INSTANCING_BUFFER`）

---

## 小结

草地系统 Shader = 顶点色权重（根部锁定）+ 风噪声贴图（自然漩涡）+ 高频抖动叠加 + C# 传入交互点（压弯效果）+ 双面法线翻转 + 透光感 + 距离 Alpha 淡出。这套方案可直接用于地形草地、灌木丛等植被效果。

下一篇：角色皮肤完整方案——把 SSS、Wrap Lighting、法线贴图、毛孔细节、汗水湿润整合到一套可用于生产的皮肤 Shader。
