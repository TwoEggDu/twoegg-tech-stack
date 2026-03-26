+++
title = "Shader 语法基础 05｜宏体系与 Shader 变体：multi_compile vs shader_feature"
slug = "shader-basic-05-macros-variants"
date = 2026-03-26
description = "Shader 变体是移动端包体膨胀和运行时卡顿的常见根源。理解 #pragma multi_compile 和 shader_feature 的区别，变体是怎么生成的，以及如何控制变体数量。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "语法基础", "宏", "Shader变体", "性能"]
series = ["Shader 手写技法"]
[extra]
weight = 4090
+++

Shader 里的 `#pragma multi_compile` 和 `shader_feature` 是两个看起来相似、行为截然不同的指令。理解它们的区别，是控制包体大小和避免运行时卡顿的前提。

---

## 什么是 Shader 变体

一个 Shader 源码经过编译，可能产生**多个变体（Variant）**——每个变体是一份独立的 GPU 字节码，对应不同的关键字组合。

```hlsl
#pragma multi_compile _ _FEATURE_A
#pragma multi_compile _ _FEATURE_B
```

这两行产生 2×2 = **4 个变体**：

| 变体 | _FEATURE_A | _FEATURE_B |
|------|-----------|-----------|
| 变体 0 | 关闭 | 关闭 |
| 变体 1 | 开启 | 关闭 |
| 变体 2 | 关闭 | 开启 |
| 变体 3 | 开启 | 开启 |

每个变体是单独的 GPU 程序。关键字数量增加时，变体数量指数级增长——这就是**变体爆炸**的来源。

---

## multi_compile vs shader_feature

| | `multi_compile` | `shader_feature` |
|--|-----------------|-----------------|
| 打包行为 | **全部变体都打进包** | **只打包用到的变体** |
| 运行时切换 | 支持（随时用 `EnableKeyword`） | 支持，但只能切换到已打包的变体 |
| 适合场景 | 运行时动态启用的功能 | 材质勾选的功能（静态） |
| 变体裁剪 | 无，全部保留 | 构建时分析材质，未用到的变体剔除 |

**关键区别**：

```hlsl
// 这两个关键字，如果没有任何材质勾选 _EMISSION，打包时会被裁掉
#pragma shader_feature _EMISSION

// 这个关键字，无论材质有没有用，都会打进包
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
```

---

## 什么时候用哪个

**用 `shader_feature`**：

- 材质 Inspector 里的勾选功能（比如"启用自发光"）
- 功能是否启用在导入时就能确定
- 希望打包时自动裁剪未使用的变体

```hlsl
#pragma shader_feature _EMISSION         // 自发光开关
#pragma shader_feature _NORMALMAP        // 法线贴图开关
#pragma shader_feature _ALPHATEST_ON     // Alpha Test 开关
```

**用 `multi_compile`**：

- 运行时动态切换的功能（比如全局光照质量档位）
- URP 自身的关键字（阴影、光照模式等）——这些你控制不了，URP 用的就是 multi_compile

```hlsl
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _SHADOWS_SOFT
#pragma multi_compile_fog                // 雾效
```

---

## 关键字命名约定

按照 Unity 惯例：

- 全大写加下划线前缀：`_FEATURE_NAME`
- `_` 表示"关键字关闭"（空字符串变体）

```hlsl
#pragma shader_feature _ _EMISSION    // _ 是关键字关闭的变体名
// 等价于：
#pragma shader_feature_local _ _EMISSION
```

---

## _local 后缀：避免全局污染

```hlsl
#pragma shader_feature_local _EMISSION
#pragma multi_compile_local _ _FEATURE_A
```

加 `_local` 后缀的关键字是**材质级别**的，不同材质的关键字互不干扰。不加则是**全局关键字**，会被所有 Shader 共享。

全局关键字上限是 256 个（Unity 限制），局部关键字没有这个限制。**推荐自定义关键字都用 `_local`**。

---

## 在材质 Inspector 里控制关键字

用 `[Toggle]` 和 `[KeywordEnum]` 让材质 Inspector 自动切换关键字：

```hlsl
Properties
{
    [Toggle(_EMISSION)]     _EmissionEnabled ("Emission", Float) = 0
    [Toggle(_NORMALMAP)]    _NormalMapEnabled ("Normal Map", Float) = 0

    [KeywordEnum(Off, Low, High)] _Quality ("Quality", Float) = 0
    // 产生关键字：_QUALITY_OFF、_QUALITY_LOW、_QUALITY_HIGH
}
```

Inspector 里勾选/取消时，Unity 自动调用 `material.EnableKeyword` 或 `DisableKeyword`。

---

## 运行时切换关键字

代码里控制关键字：

```hlsl
// 材质级别
material.EnableKeyword("_EMISSION");
material.DisableKeyword("_EMISSION");

// 全局级别（影响所有 Shader）
Shader.EnableKeyword("_QUALITY_HIGH");
Shader.DisableKeyword("_QUALITY_LOW");
```

**注意**：运行时切换到一个没打进包的变体，会触发**运行时编译**，在移动端可能卡顿 100ms ~ 几秒。这就是为什么 `shader_feature` 必须确保用到的变体都被材质引用过。

---

## 变体数量控制实践

假设 Shader 有以下关键字：

```hlsl
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE   // 3 个
#pragma multi_compile _ _SHADOWS_SOFT                                       // 2 个
#pragma shader_feature_local _ _EMISSION                                    // 2 个
#pragma shader_feature_local _ _NORMALMAP                                   // 2 个
```

理论变体数：3 × 2 × 2 × 2 = **24 个**

URP 自带的关键字（阴影、光照模式、雾效等）加起来容易超过 100+ 变体。控制方法：

1. **删掉用不到的 multi_compile**：确认项目不用某功能时，删掉对应的 `#pragma`
2. **用 `shader_feature` 替代 `multi_compile`**：让构建期裁剪
3. **URP 全局剥离设置**：`Project Settings → Graphics → Shader Stripping`，可以关闭雾效、光照贴图等全局变体
4. **自定义 `IPreprocessShaders`**：在构建管线里写代码，按规则裁剪特定变体

---

## 条件编译：#if / #ifdef

`#pragma` 声明关键字后，在代码里用 `#ifdef` 判断关键字是否启用：

```hlsl
#ifdef _EMISSION
    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
    color.rgb += emission * _EmissionColor.rgb;
#endif

// 或者
#if defined(_NORMALMAP)
    float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
    normalWS = TransformTangentToWorld(normalTS, TBN);
#else
    float3 normalWS = normalize(input.normalWS);
#endif
```

这类分支是**编译期分支**，不同变体编译出的字节码完全不同，不存在 Warp Divergence 问题。

---

## Pass 级别的关键字

关键字默认对 Shader 的所有 Pass 生效。如果只想影响某个 Pass：

```hlsl
Pass
{
    HLSLPROGRAM
    #pragma shader_feature_local_fragment _EMISSION
    // _fragment 后缀：只在 Fragment Shader 里生效，减少变体数
    ENDHLSL
}
```

`_vertex`、`_fragment`、`_geometry` 后缀可以限制关键字作用域。

---

## 小结

| 概念 | 要点 |
|------|------|
| 变体 | 每种关键字组合对应一份独立 GPU 字节码 |
| `multi_compile` | 全部打包，支持运行时任意切换 |
| `shader_feature` | 只打包用到的，构建期自动裁剪 |
| `_local` | 材质级关键字，避免全局污染，推荐默认用 |
| `#ifdef` | 编译期分支，不同变体独立编译，无 Divergence |
| 变体控制 | 删无用 pragma、用 shader_feature、URP Stripping 设置 |

下一篇：Shader 调试技巧——颜色可视化法线/UV/光照数据，Frame Debugger 看 Pass 执行顺序，RenderDoc 定位像素级问题。
