---
title: "项目实战 06｜UI 特效 Shader：扫光、溶解边框、全息干扰"
slug: "shader-project-06-ui-effects"
date: "2026-03-26"
description: "UI 的视觉冲击力经常来自 Shader 特效：装备扫光（高亮从左到右扫过）、技能冷却溶解（圆形进度裁剪）、科幻全息界面（扫描线 + 色差 + 闪烁）。这篇给出可直接挂在 UI Image 上的 Shader 实现。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "项目实战"
  - "UI"
  - "特效"
  - "全息"
  - "扫光"
series: "Shader 手写技法"
weight: 4450
---
UI Shader 与场景 Shader 有几个关键差异：渲染管线走 Unity UI 的 Canvas（`UI/Default` 基础）、需要保持 UV 归一化（`0~1` 范围）、通常不需要光照（纯颜色操作）。这些特效可以直接赋给 `Image` 组件的 Material。

---

## Unity UI Shader 基础结构

UI Shader 需要继承 Unity 内置 UI 的基本设置：

```hlsl
Shader "Custom/UI/FX"
{
    Properties { ... }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True"
               "RenderType" = "Transparent" "PreviewType" = "Plane"
               "CanUseSpriteAtlas" = "True" }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]    // 遵循 Canvas ZTest 设置
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGBA

        Pass
        {
            HLSLPROGRAM
            // ...
            ENDHLSL
        }
    }
}
```

---

## 效果一：装备扫光（Shine Sweep）

高亮从左到右扫过图片，常用于装备获得、强化成功等反馈：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

    // 扫光位置：_ShinePosX 由 C# 控制（-0.2 ~ 1.2，超出屏幕范围不显示）
    float shineX    = input.uv.x - _ShinePosX;
    float shineMask = smoothstep(_ShineWidth, 0, abs(shineX));    // 中心亮，两侧渐暗

    // 斜向扫光：加入 uv.y 偏移
    float shineAngle = shineX - (input.uv.y - 0.5) * _ShineTilt;
    shineMask = smoothstep(_ShineWidth, 0, abs(shineAngle));

    // 只在图片不透明区域显示扫光
    shineMask *= base.a;

    half3 shineColor = lerp(base.rgb, _ShineColor.rgb, shineMask * _ShineIntensity);
    return half4(shineColor, base.a);
}
```

**C# 控制动画：**
```csharp
IEnumerator PlayShine()
{
    float t = 0;
    while (t < 1)
    {
        t += Time.deltaTime / shineDuration;
        // 从 -0.2 扫到 1.2（完整覆盖图片宽度）
        mat.SetFloat("_ShinePosX", Mathf.Lerp(-0.2f, 1.2f, t));
        yield return null;
    }
}
```

---

## 效果二：圆形进度裁剪（技能冷却）

顺时针从上方开始裁剪，常用于技能冷却遮罩：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

    // UV 转极坐标
    float2 centered = input.uv - 0.5;           // 以中心为原点
    float  angle    = atan2(centered.x, centered.y);  // [-π, π]，从上方开始顺时针
    angle = angle / (2.0 * PI) + 0.5;           // 归一化到 [0, 1]

    // _FillAmount: 0=空，1=满
    float  cutoff   = step(angle, _FillAmount);

    // 可选：在截断边缘加柔化（smoothstep）
    // float cutoff = smoothstep(_FillAmount - _SoftEdge, _FillAmount + _SoftEdge, angle);
    // cutoff = 1.0 - cutoff;  // 注意翻转

    return half4(base.rgb, base.a * cutoff);
}
```

---

## 效果三：溶解边框（技能解锁/卡牌溶解）

溶解时在边缘产生发光边框：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    half4 base  = SAMPLE_TEXTURE2D(_MainTex,    sampler_MainTex,    input.uv) * input.color;
    half  noise = SAMPLE_TEXTURE2D(_NoiseTex,   sampler_NoiseTex,   input.uv + _Time.y * 0.1).r;

    // _Dissolve: 0=完整，1=完全消失
    float threshold = _Dissolve;
    float edge      = threshold + _EdgeWidth;

    // 发光边缘
    float edgeFactor = smoothstep(threshold, edge, noise);
    half3 edgeColor  = lerp(_EdgeColor1.rgb, _EdgeColor2.rgb, edgeFactor) * _EdgeIntensity;

    // 最终裁剪
    float visible    = step(threshold, noise);   // 噪声 > 阈值才可见

    half3 finalColor = base.rgb + edgeColor * (1.0 - visible) * step(noise, edge);
    return half4(finalColor, base.a * visible);
}
```

---

## 效果四：科幻全息界面

扫描线 + 颜色闪烁 + 色差，模拟 CRT/全息投影效果：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    float2 uv = input.uv;

    // ── 色差（RGB 分离）─────────────────────────────────
    float  aberration = _AberrationStrength * (1.0 + sin(_Time.y * 2.0) * 0.5);
    half   r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( aberration, 0)).r;
    half   g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
    half   b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-aberration, 0)).b;
    half4  base = half4(r, g, b, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a);
    base *= input.color;

    // ── 扫描线 ───────────────────────────────────────────
    float  scanline = sin(uv.y * _ScanlineCount + _Time.y * _ScanlineSpeed) * 0.5 + 0.5;
    scanline        = lerp(1.0, scanline, _ScanlineStrength);
    base.rgb       *= scanline;

    // ── 随机闪烁干扰（模拟信号故障）──────────────────────
    float glitchNoise = frac(sin(floor(uv.y * 20) + _Time.y * 30) * 43758.5453);
    float glitch      = step(1.0 - _GlitchProbability, glitchNoise);
    // UV 水平偏移（字符跳行）
    float2 glitchUV   = float2(uv.x + (frac(glitchNoise * 100) - 0.5) * glitch * _GlitchStrength, uv.y);
    half4  glitchSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, glitchUV) * input.color;
    base = lerp(base, glitchSample, glitch * 0.5);

    // ── 全息颜色叠加（纯色 × 原贴图亮度）──────────────────
    half  luminance   = dot(base.rgb, half3(0.299, 0.587, 0.114));
    base.rgb          = lerp(base.rgb, _HoloColor.rgb * luminance, _HoloBlend);

    // ── 边缘淡出（模拟投影衰减）──────────────────────────
    float2 edgeFade = min(uv, 1.0 - uv) * _EdgeFadeSharpness;
    float  vignette = saturate(edgeFade.x) * saturate(edgeFade.y);
    base.a *= vignette;

    return base;
}
```

---

## 完整 UI 特效 Shader（四效合一，变体开关）

```hlsl
Shader "Custom/UI/FX"
{
    Properties
    {
        _MainTex          ("Texture",          2D)         = "white" {}
        [Enum(Shine,0,CircleFill,1,Dissolve,2,Hologram,3)]
        _EffectMode       ("Effect Mode",      Float)      = 0

        [Header(Shine)]
        _ShinePosX        ("Shine Pos X",      Range(-0.2,1.2)) = -0.2
        _ShineWidth       ("Shine Width",      Range(0.01,0.5)) = 0.1
        _ShineTilt        ("Shine Tilt",       Range(-1,1))    = 0.3
        [HDR]_ShineColor  ("Shine Color",      Color)      = (2,2,2,1)
        _ShineIntensity   ("Shine Intensity",  Range(0,1))  = 0.8

        [Header(Circle Fill)]
        _FillAmount       ("Fill Amount",      Range(0,1))  = 1.0

        [Header(Dissolve)]
        _NoiseTex         ("Noise Texture",    2D)         = "white" {}
        _Dissolve         ("Dissolve",         Range(0,1)) = 0.0
        _EdgeWidth        ("Edge Width",       Range(0,0.3))= 0.05
        [HDR]_EdgeColor1  ("Edge Color 1",     Color)      = (1,0.5,0,1)
        [HDR]_EdgeColor2  ("Edge Color 2",     Color)      = (1,1,0,1)
        _EdgeIntensity    ("Edge Intensity",   Range(1,5))  = 3.0

        [Header(Hologram)]
        _AberrationStrength("Aberration",     Range(0,0.02))= 0.005
        _ScanlineCount    ("Scanline Count",   Float)      = 200
        _ScanlineSpeed    ("Scanline Speed",   Float)      = 3.0
        _ScanlineStrength ("Scanline Strength",Range(0,1)) = 0.3
        _GlitchProbability("Glitch Prob",     Range(0,1))  = 0.05
        _GlitchStrength   ("Glitch Strength", Range(0,0.1))= 0.02
        [HDR]_HoloColor   ("Holo Color",       Color)      = (0,1,0.8,1)
        _HoloBlend        ("Holo Blend",       Range(0,1)) = 0.5
        _EdgeFadeSharpness("Edge Fade",        Range(1,20)) = 8.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True"
               "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local EFFECT_SHINE EFFECT_CIRCLE EFFECT_DISSOLVE EFFECT_HOLO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _ShinePosX; float _ShineWidth; float _ShineTilt; float _ShineIntensity;
                float4 _ShineColor;
                float  _FillAmount;
                float  _Dissolve; float _EdgeWidth; float _EdgeIntensity;
                float4 _EdgeColor1; float4 _EdgeColor2;
                float  _AberrationStrength; float _ScanlineCount; float _ScanlineSpeed;
                float  _ScanlineStrength; float _GlitchProbability; float _GlitchStrength;
                float4 _HoloColor; float _HoloBlend; float _EdgeFadeSharpness;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings   { float4 hcs:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            Varyings vert(Attributes i) {
                Varyings o;
                o.hcs   = TransformObjectToHClip(i.pos.xyz);
                o.uv    = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                #if defined(EFFECT_SHINE)
                    float shineAngle = (input.uv.x - _ShinePosX) - (input.uv.y - 0.5) * _ShineTilt;
                    float shineMask  = smoothstep(_ShineWidth, 0, abs(shineAngle)) * base.a;
                    base.rgb = lerp(base.rgb, _ShineColor.rgb, shineMask * _ShineIntensity);

                #elif defined(EFFECT_CIRCLE)
                    float2 c  = input.uv - 0.5;
                    float  a  = atan2(c.x, c.y) / (2.0 * PI) + 0.5;
                    base.a   *= step(a, _FillAmount);

                #elif defined(EFFECT_DISSOLVE)
                    half  noise  = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv + _Time.y * 0.1).r;
                    float edge   = _Dissolve + _EdgeWidth;
                    float vis    = step(_Dissolve, noise);
                    half3 ec     = lerp(_EdgeColor1.rgb, _EdgeColor2.rgb, smoothstep(_Dissolve, edge, noise));
                    base.rgb    += ec * (1.0 - vis) * step(noise, edge) * _EdgeIntensity;
                    base.a      *= vis;

                #elif defined(EFFECT_HOLO)
                    float ab = _AberrationStrength;
                    half r   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(ab,0)).r;
                    half b   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(ab,0)).b;
                    base.r   = r * input.color.r;
                    base.b   = b * input.color.b;
                    float scan = sin(input.uv.y * _ScanlineCount + _Time.y * _ScanlineSpeed) * 0.5 + 0.5;
                    base.rgb *= lerp(1.0, scan, _ScanlineStrength);
                    half lum = dot(base.rgb, half3(0.299,0.587,0.114));
                    base.rgb = lerp(base.rgb, _HoloColor.rgb * lum, _HoloBlend);
                    float2 ef = min(input.uv, 1.0 - input.uv) * _EdgeFadeSharpness;
                    base.a  *= saturate(ef.x) * saturate(ef.y);
                #endif

                return base;
            }
            ENDHLSL
        }
    }
}
```

---

## 小结

| 效果 | 核心技术 | C# 控制参数 |
|------|---------|-----------|
| 装备扫光 | UV.x 偏移 + smoothstep 宽度 | `_ShinePosX`：-0.2 → 1.2 |
| 圆形进度 | atan2 极坐标 + step 裁剪 | `_FillAmount`：0 → 1 |
| 溶解边框 | 噪声 + smoothstep 边缘发光 | `_Dissolve`：0 → 1 |
| 全息干扰 | 色差 + 扫描线 + 随机 glitch | 自动动画（_Time.y 驱动）|

下一篇：后处理特效组合——夜视仪、受伤血屏、扫描波效果，用 Renderer Feature 实现游戏中常见的屏幕空间特效。
