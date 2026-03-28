---
title: "游戏常用效果｜序列帧与 Flipbook：合图优化与 UV 计算"
slug: shader-fx-03-flipbook
date: "2026-03-28"
description: "讲解序列帧动画的 UV 计算原理、帧间插值消除跳帧感、与 Unity 粒子系统的集成方式，以及 mip 渗色、padding 等实用优化技巧，附完整 Flipbook Fragment Shader。"
tags: ["Shader", "HLSL", "URP", "特效", "序列帧", "Flipbook", "粒子"]
series: "Shader 手写技法"
weight: 4620
---

序列帧动画是特效制作里最常见的技术之一。爆炸、火焰、魔法粒子、液体飞溅——很多视觉效果用 3D Shader 做成本太高，预先渲染成序列帧然后在游戏里播放反而更实际。这篇文章讲的不是怎么制作序列帧素材，而是 Shader 里那些容易被忽视的细节：UV 怎么算、帧间怎么插值、粒子系统怎么配合，以及几个影响画质的小技巧。

---

## 序列帧的核心原理

Flipbook（序列帧合图）把一段动画的所有帧排列在一张贴图里，常见布局是 N 列 M 行的网格，总帧数 N×M。Shader 在每个时刻计算出当前应该显示哪一帧，算出该帧在贴图里的 UV 范围，只采样那一小块区域。

以 4 列 4 行共 16 帧的贴图为例，每帧的 UV 大小是 `(1/4, 1/4) = (0.25, 0.25)`。第 0 帧在左上角，第 5 帧在第 1 行第 1 列（0-indexed）。

---

## UV 计算公式

```hlsl
// 根据帧索引计算该帧在贴图中的 UV
// baseUV：网格内的局部 UV（0~1）
// frameIndex：当前帧序号（整数）
// cols/rows：列数/行数
float2 GetFrameUV(float2 baseUV, float frameIndex, float cols, float rows)
{
    // 帧在网格中的列、行位置
    float col = fmod(frameIndex, cols);          // 列：0 ~ cols-1
    float row = floor(frameIndex / cols);        // 行：0 ~ rows-1

    // 每帧的 UV 尺寸
    float2 frameSize = float2(1.0 / cols, 1.0 / rows);

    // Unity 贴图 UV 原点在左下角，序列帧从左上角开始排列，需要翻转行号
    float2 frameOffset = float2(col, rows - 1.0 - row) * frameSize;

    // 将 baseUV 压缩到一帧范围内，再加上帧的起始偏移
    return baseUV * frameSize + frameOffset;
}
```

帧索引的计算：

```hlsl
// floor(_Time.y * FPS) 取整得到整数帧，mod TotalFrames 实现循环
float totalFrames = _Cols * _Rows;
float frameIndex  = fmod(floor(_Time.y * _FPS), totalFrames);
```

`_Time.y` 是 Unity 内置时间变量，单位秒。`_FPS` 是播放帧率，12~24 适合大多数粒子特效，动作游戏 UI 特效可以用到 30。

---

## Flipbook 混合：帧间插值消除跳帧感

直接用 `floor` 取整帧，播放时会有明显的跳帧感，尤其在低 FPS（12 帧以下）时突出。Flipbook Blending 同时采样当前帧和下一帧，用 `frac()` 做 lerp：

```hlsl
float4 SampleFlipbookBlend(float2 baseUV, float cols, float rows, float fps)
{
    float totalFrames = cols * rows;
    float timeFrame   = _Time.y * fps;

    // 当前帧和下一帧
    float frameA = fmod(floor(timeFrame), totalFrames);
    float frameB = fmod(frameA + 1.0, totalFrames);

    // frac() 取小数部分：当前帧到下一帧的进度（0~1）
    float blend = frac(timeFrame);

    float2 uvA = GetFrameUV(baseUV, frameA, cols, rows);
    float2 uvB = GetFrameUV(baseUV, frameB, cols, rows);

    // 强制 mip 0，避免跨帧边界的 mipmap 采样渗色
    float4 colorA = SAMPLE_TEXTURE2D_LOD(_FlipbookTex, sampler_FlipbookTex, uvA, 0);
    float4 colorB = SAMPLE_TEXTURE2D_LOD(_FlipbookTex, sampler_FlipbookTex, uvB, 0);

    return lerp(colorA, colorB, blend);
}
```

帧间插值让动画平滑很多，代价是每帧多一次贴图采样。对于粒子特效，这个开销通常可以接受。

---

## 完整带混合插值的 Flipbook Fragment Shader

```hlsl
Shader "Custom/FlipbookParticle"
{
    Properties
    {
        _FlipbookTex  ("Flipbook Texture", 2D)          = "white" {}
        _Cols         ("Columns",          Float)       = 4
        _Rows         ("Rows",             Float)       = 4
        _FPS          ("Playback FPS",     Float)       = 24
        _BlendFrames  ("Blend Frames (0=off 1=on)", Float) = 1
        _Color        ("Tint Color",       Color)       = (1,1,1,1)
        _Softness     ("Soft Particle Softness", Range(0,3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FlipbookForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // 开启粒子系统 GPU Instancing 支持
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
            #include "UnityParticleInstancing.cginc"
            #endif

            TEXTURE2D(_FlipbookTex); SAMPLER(sampler_FlipbookTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FlipbookTex_ST;
                float  _Cols;
                float  _Rows;
                float  _FPS;
                float  _BlendFrames;
                float4 _Color;
                float  _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float4 screenPos  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float2 GetFrameUV(float2 baseUV, float frameIdx, float cols, float rows)
            {
                float  col    = fmod(frameIdx, cols);
                float  row    = floor(frameIdx / cols);
                float2 sz     = float2(1.0 / cols, 1.0 / rows);
                float2 offset = float2(col, rows - 1.0 - row) * sz;
                return baseUV * sz + offset;
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
                ParticleInstancingSetup();
                #endif

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _FlipbookTex);
                OUT.color      = IN.color * _Color;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float totalFrames = _Cols * _Rows;
                float timeFrame   = _Time.y * _FPS;
                float4 finalColor;

                if (_BlendFrames > 0.5)
                {
                    // 插值模式：同时采样当前帧和下一帧
                    float frameA = fmod(floor(timeFrame), totalFrames);
                    float frameB = fmod(frameA + 1.0, totalFrames);
                    float blend  = frac(timeFrame);

                    float2 uvA = GetFrameUV(IN.uv, frameA, _Cols, _Rows);
                    float2 uvB = GetFrameUV(IN.uv, frameB, _Cols, _Rows);

                    // 强制 mip 0 避免边缘渗色
                    float4 cA = SAMPLE_TEXTURE2D_LOD(_FlipbookTex, sampler_FlipbookTex, uvA, 0);
                    float4 cB = SAMPLE_TEXTURE2D_LOD(_FlipbookTex, sampler_FlipbookTex, uvB, 0);
                    finalColor = lerp(cA, cB, blend);
                }
                else
                {
                    // 跳帧模式
                    float frameIdx = fmod(floor(timeFrame), totalFrames);
                    float2 uv      = GetFrameUV(IN.uv, frameIdx, _Cols, _Rows);
                    finalColor     = SAMPLE_TEXTURE2D_LOD(_FlipbookTex, sampler_FlipbookTex, uv, 0);
                }

                finalColor *= IN.color;

                // Soft Particle：靠近不透明表面时淡出，避免粒子和几何体穿插的硬边
                float2 screenUV   = IN.screenPos.xy / IN.screenPos.w;
                float  sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float  partDepth  = IN.screenPos.w;
                float  fade       = saturate((sceneDepth - partDepth) / _Softness);
                finalColor.a     *= fade;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
```

---

## 为什么用 Flipbook 而不是 Unity Animator

Unity Animator 在单个 GameObject 上运行很流畅，但放到粒子系统里就出问题了。粒子系统开启 **GPU Instancing** 后，每个粒子实例共享同一个 Shader 状态，Animator 无法为每个粒子维护独立的播放时间。

Flipbook 把播放时间编码进 `_Time.y`，每个粒子实例在 Shader 内自行计算帧索引，和 GPU Instancing 完全兼容。Unity 粒子系统的 **Texture Sheet Animation** 模块本质上就是 Flipbook——它在 CPU 侧计算帧信息，传给 Shader。如果手写 Flipbook Shader，需要处理 `UNITY_PARTICLE_INSTANCING_ENABLED` 宏，否则所有粒子实例会同步显示同一帧。

---

## 避免边缘渗色的优化技巧

**边缘渗色（Bleeding）**是 Flipbook 贴图常见问题：在帧边界处，硬件 mipmap 采样会把相邻帧的颜色混进来，产生明显的彩色边框。

三个解决方案：

1. **强制 mip 0**：使用 `SAMPLE_TEXTURE2D_LOD(..., 0)` 禁用 mipmap，彻底避免跨帧采样。代价是远处粒子可能出现锯齿，通常可以接受。

2. **增加 padding**：在每帧的边界处留 2~4 像素的空白（或重复边缘像素），即使 mipmap 轻微溢出，采样到的也是同一帧的边缘颜色而非相邻帧的内容。

3. **Wrap Mode 设为 Clamp**：虽然不能解决帧间渗色，但能避免 UV 接近 0 或 1 时因 Repeat 模式采样到贴图另一侧的颜色，减少一类错误。

实际项目中 1 + 2 搭配使用是最稳的方案：强制 mip 0 兜底，padding 作为保险。对于高速运动的近距离粒子，也可以在 `SAMPLE_TEXTURE2D_LOD` 第四个参数里传入较小的 mip 等级（如 1~2），在质量和渗色之间取平衡。
