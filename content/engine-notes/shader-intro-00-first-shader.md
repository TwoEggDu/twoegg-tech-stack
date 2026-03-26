+++
title = "Shader 手写入门 00｜我的第一个 Shader：让物体显示纯色"
slug = "shader-intro-00-first-shader"
date = 2026-03-26
description = "从一个空文件开始，写出第一个能在 Unity URP 里跑的 Shader。不讲渲染管线理论，只讲每一行代码是什么、为什么要写它，让物体显示一个你指定的颜色。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "入门", "Unity"]
series = ["Shader 手写技法"]
[extra]
weight = 4000
+++

写第一个 Shader 之前，先把一件事说清楚：Shader 不是魔法，它就是一段跑在 GPU 上的程序。你写的代码决定了每个像素最终显示什么颜色。

这篇只有一个目标：写出一个能用的 Shader，让材质球上的物体显示你指定的颜色。其他一切都在后面慢慢展开。

---

## 新建一个 Shader 文件

在 Project 窗口里右键 → **Create → Shader → Unlit Shader**。

Unity 会生成一个模板文件。把里面所有内容删掉，从空白开始写——模板里有很多现在不需要理解的东西，反而会干扰学习。

---

## 完整代码

```hlsl
Shader "Custom/SolidColor"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(_Color.rgb, _Color.a);
            }

            ENDHLSL
        }
    }
}
```

新建一个材质球，把这个 Shader 赋给它，拖到场景里的物体上，Inspector 里出现 Color 属性——改颜色，物体颜色跟着变。这就是一个可以用的 Shader 了。

---

## 逐行解释

### Shader "Custom/SolidColor"

Shader 的名字，决定它在 Unity 的材质 Shader 下拉列表里出现在哪个路径。斜杠表示分组，`Custom/SolidColor` 会出现在 Custom 分组下。

---

### Properties

```hlsl
Properties
{
    _Color ("Color", Color) = (1, 0, 0, 1)
}
```

Properties 块声明了可以在 Inspector 里调整的参数。格式是：

```
变量名 ("显示名称", 类型) = 默认值
```

`Color` 类型在 Inspector 里显示为颜色拾取器，默认值是 RGBA = (1, 0, 0, 1)，即不透明红色。

**注意**：Properties 块里声明的变量，还需要在 HLSL 代码里再声明一次才能用（见下面的 CBUFFER）。

---

### Tags

```hlsl
Tags
{
    "RenderType" = "Opaque"
    "RenderPipeline" = "UniversalPipeline"
}
```

告诉 URP 这个 SubShader 是给 URP 用的。如果不写 `"RenderPipeline" = "UniversalPipeline"`，URP 会跳过这个 SubShader，材质显示洋红色。

Pass 里的 `"LightMode" = "UniversalForward"` 告诉 URP 这个 Pass 是主渲染 Pass，在 Forward 渲染时执行。

---

### HLSLPROGRAM / ENDHLSL

这对标记之间的内容是真正的 GPU 代码（HLSL 语言）。所有顶点变换和颜色计算都在这里。

---

### #pragma

```hlsl
#pragma vertex vert
#pragma fragment frag
```

告诉编译器：名叫 `vert` 的函数是顶点着色器，名叫 `frag` 的函数是片元着色器。名字可以随便起，只要 `#pragma` 里和函数名一致就行。

---

### #include

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
```

引入 URP 的核心工具库。`TransformObjectToHClip` 就来自这里——它把顶点从物体本地坐标变换到裁剪空间（屏幕坐标的前一步）。

---

### CBUFFER

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _Color;
CBUFFER_END
```

Properties 里声明的 `_Color` 要在这里再声明一次，GPU 才能读到。

`CBUFFER_START(UnityPerMaterial)` 是 URP 的要求：材质属性必须放在名为 `UnityPerMaterial` 的 Constant Buffer 里，否则 SRP Batcher（一种减少 CPU 开销的优化）不会对这个 Shader 生效。

---

### Attributes 和 Varyings

```hlsl
struct Attributes
{
    float4 positionOS : POSITION;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
};
```

**Attributes**：顶点着色器的输入，从 Mesh 里读数据。`: POSITION` 是语义（Semantic），告诉 GPU 这个字段对应 Mesh 的顶点位置。

**Varyings**：顶点着色器的输出，同时也是片元着色器的输入。`: SV_POSITION` 是系统语义，表示裁剪空间坐标，GPU 用它来做光栅化。

---

### vert（顶点着色器）

```hlsl
Varyings vert(Attributes v)
{
    Varyings o;
    o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
    return o;
}
```

每个顶点执行一次。输入是 Mesh 上的一个顶点（物体本地坐标），输出是这个顶点在屏幕上的位置（裁剪空间坐标）。

`TransformObjectToHClip`：把顶点从物体空间变换到裁剪空间，内部做了 Model × View × Projection 三个矩阵变换。

---

### frag（片元着色器）

```hlsl
half4 frag(Varyings i) : SV_Target
{
    return half4(_Color.rgb, _Color.a);
}
```

每个像素执行一次。返回值就是这个像素最终的颜色，`: SV_Target` 表示输出到渲染目标（屏幕或 RT）。

`half4` 是 16bit 精度的 4 维向量，比 `float4` 省一半寄存器，在移动端 GPU 上更快。颜色值范围是 0~1，`half` 的精度完全够用。

---

## 试着改改

**改成随机颜色**（用物体坐标做颜色）：

```hlsl
half4 frag(Varyings i) : SV_Target
{
    // positionHCS.xy 是屏幕像素坐标，除以屏幕分辨率得到 0~1 的 UV
    float2 screenUV = i.positionHCS.xy / _ScreenParams.xy;
    return half4(screenUV.x, screenUV.y, 0, 1);
}
```

赋给物体，你会看到左下角是黑色，右上角是黄色的渐变。这就是用屏幕坐标驱动颜色的效果——改代码，GPU 立刻响应。

---

## 小结

- Shader 是跑在 GPU 上的程序：Vertex Shader 决定顶点位置，Fragment Shader 决定像素颜色
- Properties 声明 Inspector 参数，CBUFFER 里再声明一次 GPU 才能读到
- URP Shader 必须有 `"RenderPipeline" = "UniversalPipeline"` 和 `"LightMode" = "UniversalForward"` 两个 Tag
- `TransformObjectToHClip`：物体空间 → 裁剪空间，每个顶点着色器都需要

下一篇：用 `_Time` 让颜色随时间变化，理解 Shader 的执行时机。
