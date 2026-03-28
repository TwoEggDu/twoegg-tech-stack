---
title: "Shader 语法基础 07｜ShaderLab 结构：Properties、SubShader、Pass 与 Tags"
slug: "shader-basic-07-shaderlab-structure"
date: "2026-03-28"
description: "系统梳理 Unity ShaderLab 的 DSL 结构，包括 Properties 声明语法、SubShader 降级逻辑、Tags 各字段含义、Pass 渲染状态，以及一个可直接使用的最小 URP Unlit Shader 示例。"
tags: ["Shader", "ShaderLab", "URP", "Unity"]
series: "Shader 手写技法"
weight: 4110
---

很多人第一次打开 `.shader` 文件，看到 `SubShader`、`Tags`、`Pass` 这些关键字时，会以为自己在看 HLSL。其实这些都属于 **ShaderLab**——Unity 自己定义的一套 DSL（领域特定语言），它是 HLSL 代码的"外壳"。理解 ShaderLab 的分层结构，是读懂任何 Unity Shader 的前提。

---

## ShaderLab 不是 HLSL

HLSL 是微软的着色器编程语言，负责描述 GPU 如何计算顶点位置和像素颜色。ShaderLab 则是 Unity 在 HLSL 之上再包一层的声明式结构，负责描述"这个 Shader 有哪些属性、在什么硬件上用哪个实现、渲染状态怎么设置"。

真正的 HLSL 代码写在 `HLSLPROGRAM ... ENDHLSL` 块里，而这个块本身是被 ShaderLab 的 Pass 包裹的。两者是嵌套关系，不是同一层面的东西。

---

## Properties 块

Properties 块定义了在 Inspector 面板中可见的材质属性，以及 HLSL 代码可以读取的 uniform 变量。每条声明的格式是：

```hlsl
_PropertyName ("Display Name", Type) = DefaultValue
```

三段含义：
- `_PropertyName`：HLSL 中引用时用的变量名，下划线开头是约定俗成
- `"Display Name"`：Inspector 面板里显示的标签
- `Type = DefaultValue`：属性类型和默认值

常用类型一览：

```hlsl
Properties
{
    _MainTex ("Main Texture", 2D) = "white" {}
    _Color   ("Tint Color", Color) = (1, 1, 1, 1)
    _Glossiness ("Smoothness", Range(0.0, 1.0)) = 0.5
    _Metallic   ("Metallic", Range(0.0, 1.0)) = 0.0
    _Cutoff  ("Alpha Cutoff", Float) = 0.5
    _Offset  ("UV Offset", Vector) = (0, 0, 0, 0)
}
```

其中 `2D` 类型的默认值是一个内置纹理名（`"white"`、`"black"`、`"bump"`），后面必须跟空的花括号 `{}`。这是历史遗留语法，不能省略。

`_MainTex_ST` 是 Unity 自动派生的配套变量，存储 tiling（xy）和 offset（zw），使用 `TRANSFORM_TEX` 宏时会用到它——这个细节在 Vertex Shader 篇会详细展开。

---

## SubShader 与 LOD 降级逻辑

一个 Shader 可以包含多个 SubShader，Unity 会从上到下依次检测，选择第一个当前硬件能运行的版本：

```hlsl
Shader "Custom/MyShader"
{
    SubShader
    {
        // 高端实现（需要支持 SM 4.5）
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 300
        Pass { ... }
    }

    SubShader
    {
        // 低端回退
        LOD 100
        Pass { ... }
    }

    FallBack "Hidden/InternalErrorShader"
}
```

`LOD` 是一个整数，配合 `Shader.globalMaximumLOD` 或 `Material.shader.maximumLOD` 可以在运行时强制降级。`FallBack` 指定了所有 SubShader 都不满足时的最终回退目标。

---

## Tags：渲染管线的调度信息

Tags 是键值对，告诉渲染管线如何调度这个 Shader。SubShader 级别的 Tags 影响整个子着色器，Pass 级别的 Tags 只影响单个 Pass。

**SubShader 级别 Tags：**

```hlsl
Tags
{
    "RenderType"     = "Opaque"
    "Queue"          = "Geometry"
    "RenderPipeline" = "UniversalPipeline"
}
```

- `RenderType`：用于摄像机替换效果（`Camera.SetReplacementShader`），常见值：`Opaque`、`Transparent`、`TransparentCutout`
- `Queue`：渲染顺序，数字形式或命名形式均可：
  - `Background` = 1000
  - `Geometry` = 2000（不透明物体默认）
  - `AlphaTest` = 2450
  - `Transparent` = 3000（透明物体，从后往前排序）
  - `Overlay` = 4000
  - 也可以用偏移：`"Queue" = "Geometry+1"`
- `RenderPipeline`：值为 `"UniversalPipeline"` 时此 SubShader 只在 URP 下生效，留空则 BRP/URP 通用

---

## Pass 块：Name、LightMode 与渲染状态

每个 Pass 是一次完整的绘制调用。Pass 的 Tags 里最重要的是 `LightMode`，它决定这个 Pass 参与哪个渲染阶段：

```hlsl
Pass
{
    Name "ForwardLit"
    Tags { "LightMode" = "UniversalForward" }

    Cull Back
    ZWrite On
    ZTest LEqual
    Blend Off

    HLSLPROGRAM
    // ...
    ENDHLSL
}
```

常用 `LightMode` 值：
- `UniversalForward`：主前向渲染 Pass，处理光照
- `ShadowCaster`：投射阴影，在灯光视角下渲染深度
- `DepthOnly`：只写深度，用于预深度 Pass
- `Meta`：烘焙 GI 时使用

渲染状态关键字：
- `Cull`：`Off` / `Front` / `Back`（默认 Back，剔除背面）
- `ZWrite`：`On` / `Off`，透明物体通常关闭
- `ZTest`：`LEqual`（默认）/ `Less` / `Always` 等
- `Blend`：`Off`（不透明）或 `SrcAlpha OneMinusSrcAlpha`（标准透明）

---

## 完整示例：最小 URP Unlit Shader

下面这个 Shader 包含 ShaderLab 的全部核心结构，可以直接在 URP 项目里使用：

```hlsl
Shader "Custom/MinimalURPUnlit"
{
    Properties
    {
        _BaseMap  ("Base Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
```

这个示例已经包含了：Properties 声明、SubShader Tags 三件套、Pass 的 LightMode、四个渲染状态开关、CBUFFER 包裹的材质属性、以及最基础的 Vert/Frag 函数。后续文章会在这个框架上逐步展开每个细节。
