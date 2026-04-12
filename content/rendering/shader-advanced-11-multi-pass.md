---
title: "Shader 进阶技法 11｜Multi-Pass Shader：描边 Pass 与主体 Pass 的组合"
slug: "shader-advanced-11-multi-pass"
date: "2026-03-28"
description: "在 URP 中手写 Multi-Pass 描边 Shader，理解外扩法线描边的顶点偏移原理、Clip Space 与 World Space 宽度一致性问题，以及 URP 对多 Pass 的支持方式。"
tags: ["Shader", "HLSL", "URP", "进阶", "Multi-Pass", "描边"]
series: "Shader 手写技法"
weight: 4390
---

描边是游戏中最常见的视觉强调手段，卡通渲染、UI 物件高亮、选中状态提示都会用到。最经典的实现方式是 Multi-Pass：第一个 Pass 把物体沿法线方向膨胀并翻转面剔除，渲染出一圈轮廓；第二个 Pass 正常渲染主体盖在上面。这个思路在 Built-in 管线里几乎零门槛，但在 URP 里有几处需要特别处理的地方。

---

## URP 对 Multi-Pass 的限制

Built-in 管线里，一个 SubShader 可以堆任意多个 Pass，渲染管线会按顺序全部执行。URP 的情况不同：URP 的 Forward Renderer 在处理每个渲染对象时，默认**只执行 LightMode 匹配的那一个 Pass**，不会自动执行同一 SubShader 里的所有 Pass。

具体来说：

- LightMode 为 `UniversalForward` 的 Pass：用于主体渲染，每个物体执行一次。
- LightMode 为 `ShadowCaster` 的 Pass：阴影深度图渲染时执行。
- 没有 LightMode tag 或 LightMode 为空字符串的 Pass：URP 不会自动执行。

因此，描边 Pass 需要赋予一个 URP 能识别的 LightMode。常用做法是给描边 Pass 也标记为 `UniversalForward`，URP 会在同一帧对同一对象执行所有标记为 `UniversalForward` 的 Pass。这是目前最直接的方式，无需 Renderer Feature。

---

## 外扩法线描边原理

描边 Pass 的顶点着色器里，把每个顶点沿法线方向移动一段距离，让整个网格"膨胀"一圈。同时设置 `Cull Front`：只渲染背面。这样主体的正面会把膨胀的正面挡住，漏出来的只有背面那一圈——就是描边。

```
原始顶点 P，法线 N，偏移量 d
膨胀后顶点 = P + N * d
```

---

## Clip Space 偏移 vs World Space 偏移

直接在 Object Space 或 World Space 里按固定距离偏移有一个问题：物体离相机越远，描边在屏幕上越细，近处又太粗。更一致的做法是在 Clip Space 里沿法线的屏幕投影方向偏移，这样描边的屏幕像素宽度与距离无关。

```hlsl
// Clip Space 偏移写法
float4 posCS = TransformObjectToHClip(positionOS);
float3 normalCS = TransformObjectToHClipDir(normalOS); // 法线变换到 clip space

// 沿屏幕空间法线方向偏移，除以 w 保持透视正确
float2 screenNormal = normalize(normalCS.xy);
posCS.xy += screenNormal * (_OutlineWidth * posCS.w * 0.01);
```

`posCS.w` 的乘法是关键：posCS.xy 在 NDC 之前要除以 w，所以这里提前乘回去，让偏移量在 NDC 中保持固定大小，从而使描边宽度在屏幕上一致。

---

## 完整 Multi-Pass 描边 Shader

```hlsl
Shader "Custom/OutlineMultiPass"
{
    Properties
    {
        _MainTex      ("Texture", 2D)             = "white" {}
        _BaseColor    ("Base Color", Color)        = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color)     = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // ── Pass 1：描边 ──────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front  // 只渲染背面，形成描边轮廓

            HLSLPROGRAM
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings outlineVert(Attributes IN)
            {
                Varyings OUT;

                // 转换到 Clip Space
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);

                // 法线变换到 Clip Space（只取 xy 做屏幕方向）
                // 注意：用 UNITY_MATRIX_MVP 的法线变换，需要逆转置
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 normalCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0.0));

                float2 screenNormal = normalize(normalCS.xy);

                // 在 Clip Space 偏移，乘以 posCS.w 保持屏幕宽度一致
                posCS.xy += screenNormal * (_OutlineWidth * 0.01 * posCS.w);

                OUT.positionHCS = posCS;
                return OUT;
            }

            half4 outlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ── Pass 2：主体 ──────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float2 uv          : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = texColor * _BaseColor;

                // 简单 Lambert 光照
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                col.rgb *= mainLight.color * ndotl + 0.2; // 0.2 环境光补偿

                return col;
            }
            ENDHLSL
        }
    }
}
```

---

## 描边宽度一致性的进一步处理

上面的 Clip Space 偏移已经保证了屏幕像素宽度在不同距离下一致，但还有一个问题：**物体自身缩放会影响法线长度**，导致描边宽度不一致。解决方法是在 Object Space 归一化法线后再变换，或者在世界空间中直接做归一化：

```hlsl
// 如果物体有非均匀缩放，法线需要用逆转置矩阵变换
// TransformObjectToWorldNormal 内部已经处理了这个问题
float3 normalWS = TransformObjectToWorldNormal(IN.normalOS); // 已归一化
```

另一个常见问题是**硬边（Hard Edge）模型**描边断裂：法线不连续的地方，相邻面膨胀方向不同，会在边缘留下缝隙。解决方案是烘焙一套平滑法线（Smooth Normal）到顶点色或 UV 里，描边 Pass 读平滑法线而不是原始法线。

---

## 局限性与替代方案

外扩法线描边有几个固有缺陷：凹形区域（如字母 O 的内侧）描边会错误填充；复杂拓扑的描边宽度不均匀；半透明物体无法用这个思路。

大型项目更常见的方案是**后处理描边**：在 Renderer Feature 里对深度图或法线图做边缘检测（Sobel、Roberts Cross 等算子），生成描边图像后叠加到最终画面。后处理描边不依赖网格拓扑，能处理任意物体，但无法按物体单独控制描边颜色，且对法线变化剧烈的地方容易误判。两种方案各有适用场景，实际项目里常常混用。
