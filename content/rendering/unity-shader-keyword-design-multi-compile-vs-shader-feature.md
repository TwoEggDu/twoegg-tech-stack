---
date: "2026-03-24"
lastmod: "2026-10-26"
title: "Unity Shader Keyword 设计：multi_compile、shader_feature 和 _local 变体的选择与误用"
description: "把 multi_compile、shader_feature、multi_compile_local、shader_feature_local 拆成构建行为、适用场景和决策依据，讲清 keyword 设计错误是怎样直接导致变体爆炸的。"
slug: "unity-shader-keyword-design-multi-compile-vs-shader-feature"
weight: 20
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Keyword"
  - "Build"
  - "Variant"
series: "Unity Shader Variant 治理"
---
Shader variant 数量增长的一个源头，是：

`keyword 设计错了。`

keyword 的声明方式会影响候选组合数；后续裁剪、构建配置和交付边界还会继续决定结果。本篇先处理声明设计，不用它替代后续阶段的检查。

> 版本与证据范围：本文依据 Unity 6000.0 文档区分声明、作用范围与裁剪；代码是教学片段，没有本项目的 Shader 编译、构建回执或 Player 验证。下文的组合数是指定声明的理论推导，不是最终产物数量。

所以这篇要解决一个比"怎么治理变体"更早的问题：

`keyword 该怎么设计，才不会在源头就把变体数量做大。`

## 先给一句总判断

如果把整件事压成一句话：

`multi_compile 的候选不依赖与 shader_feature 相同的材质使用过滤；shader_feature 通常依据构建使用证据缩小候选。最终保留还受后续裁剪影响，_local 控制作用范围，不能直接减少同一组声明的组合数。`

这句话里最关键的是两个判断：

- 声明怎样影响候选，以及后续怎样决定保留
- 谁决定"keyword 空间是否被共享"

只要这两个问题分清楚，后面的选择就不会乱。

## 一、先把四种声明方式的核心差异说清楚

### 1. multi_compile

```glsl
#pragma multi_compile __ _FOG_LINEAR _FOG_EXP _FOG_EXP2
```

**构建行为**：这类声明的候选不依赖材质是否保存了对应使用状态，但仍可能经过内置、管线或项目自定义裁剪，不能说所有排列最终都进包。

这里区别的是候选来源，不是跳过整个过滤流程。实际还要结合平台、Shader 阶段和声明条件看候选，再看本次构建的后续处理，见[裁剪层次]({{< relref "rendering/unity-shader-variant-stripping-stages-prefiltering-builtin-custom.md" >}})。

**适合的场景**：

- 运行时会被动态切换、且无法在构建期通过材质状态确定哪些组合会被用到
- 不适合只由材质静态状态证明需求，必须另外核对保留与交付

雾效、光照等管线功能可能采用这类声明；具体关键字、状态来源与过滤规则要看所用管线及版本，不能据功能名称保证每种组合都会保留。

### 2. shader_feature

```glsl
#pragma shader_feature __ _EMISSION _NORMALMAP
```

**构建行为**：通常依据构建中材质等使用证据保留所需组合，未被使用的候选可以被过滤；显式集合或其他保留配置也需要一并核对。

如果构建里没有对应使用证据，又没有其他保留路径，就不能指望运行时突然开启 `_EMISSION` 后仍有精确变体可用。声明机制见 [Unity 条件编译说明](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-conditionals-choose-a-type.html)。

**适合的场景**：

- 由材质静态开关控制的功能
- 大多数材质不会同时启用所有 keyword 的情况
- 功能是否开启在构建期已经可以通过材质状态确定

材质中的贴图、法线和自发光功能适合据此判断候选需求；具体 Shader 的声明仍需核对。项目若允许脚本在运行时改变这些状态，也必须把相应组合纳入保留与测试，不能只按功能类别假定它永远静态。

### 3. multi_compile_local 和 shader_feature_local

```glsl
#pragma multi_compile_local __ _CUSTOM_FEATURE
#pragma shader_feature_local __ _MATERIAL_DETAIL
```

**构建行为**：仍分别沿用 `multi_compile` 与 `shader_feature` 的候选/使用过滤思路；增加 `_local` 本身不会减少这组声明的状态数，也不保证候选最终保留。

`_local` 后缀解决的是**不同的问题**：它使声明具有局部作用范围，不受同名全局关键字状态覆盖，应通过材质相关 API 控制。

Unity 6000.0 的文档把可受全局状态覆盖的关键字列为 `Overridable`，局部范围列为 `Not Overridable`。C# 中的 `LocalKeyword` 表示 Shader 的关键字，其名称中的 Local 不等于该声明一定具有局部作用范围，见[脚本控制与范围说明](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-keywords-scripts.html)。

关键字数量限制需要按选定版本、计数对象与作用范围查证，不能把历史数字当作跨版本预算。本文依据 6000.0 文档解释作用范围，不列通用槽位值。

## 二、变体爆炸最常见的几种源头

### 1. 用 multi_compile 做材质功能开关

这是最常见的误用。

例如：

```glsl
// 错误示例
#pragma multi_compile __ _DETAIL_MAP _DETAIL_MAP_OVERLAY
#pragma multi_compile __ _SPECULAR_COLOR
#pragma multi_compile __ _REFLECTION_PROBE_BLENDING
```

本例三组状态数分别为 3、2、2，仅按这三条独立声明计算，理论组合数是 `3×2×2=12`。这是候选规模，不含其他声明和阶段因素，也不是最终构建保留数。

如果这些功能由构建中可确定的材质静态状态控制，可以评估 `shader_feature` 的使用过滤；是否足够还要确认动态路径、显式保留和后续裁剪。

**原则**：功能是否启用由材质决定且不会在运行时动态切换 → 用 `shader_feature`。

### 2. 多个布尔 keyword 没有合成枚举

```glsl
// 仅这三组声明的理论组合数：2 × 2 × 2 = 8
#pragma shader_feature __ _QUALITY_LOW
#pragma shader_feature __ _QUALITY_MEDIUM
#pragma shader_feature __ _QUALITY_HIGH
```

这三个 keyword 如果是互斥的，应该合成一个枚举：

```glsl
// 仅这一组声明的理论组合数：4（未启用 + 三个值）
#pragma shader_feature __ _QUALITY_LOW _QUALITY_MEDIUM _QUALITY_HIGH
```

同一条 `multi_compile` / `shader_feature` 声明里的多个值是互斥的，只会选其一，变体数量是线性的。跨行的多条声明才是乘积关系。

**原则**：多个互斥选项 → 写在同一行，而不是多行布尔。

### 3. 把运行时参数做成了 keyword

并不是所有"需要不同结果"的地方都应该用 keyword。

如果某个效果的差异可以通过 shader 里的 uniform 参数（`float`、`vector`、`texture`）控制，就没必要拆成 keyword 变体。

keyword 变体适合的是**代码路径分叉**的情况，比如"有法线贴图时执行一段不同的计算逻辑"。

如果只是"这个材质用了不同的颜色值"，直接用材质属性就行，完全不需要新建变体。

**原则**：代码路径不同 → keyword；只是参数值不同 → uniform。

### 4. 声明了但从未有材质使用的 shader_feature

有时候 shader 里会留下历史遗留的 `shader_feature` 声明，但项目里已经没有材质启用它了。

即使当前材质没有启用它，也要查显式集合、构建保留和动态使用需求；不能只凭一次材质扫描就删除。若采用 `multi_compile` 声明，则还会影响相应候选规模。

定期审查声明与实际消费者，确认不再需要后再移除，避免把动态所需状态当作历史遗留。

## 三、决策表

| 场景 | 推荐声明 | 原因 |
|------|---------|------|
| 全局渲染功能，运行时动态切换（雾效、阴影质量档） | 评估 `multi_compile` | 不只依赖材质静态状态，仍需检查后续保留 |
| 材质功能开关，静态确定（法线贴图、自发光） | 评估 `shader_feature` | 通常可依据构建使用证据缩小候选 |
| 材质功能不应被同名全局状态覆盖 | 对应的 `_local` 声明 | 控制作用范围，不是候选数量开关 |
| 运行时脚本会动态 Enable/Disable keyword | 按需求评估声明、保留和作用范围 | 声明选择不能单独保证动态组合在产物中 |
| 互斥的多档选项 | 同一行多值声明 | 避免乘积式变体增长 |
| 只影响参数值，不影响代码路径 | 不用 keyword，用 uniform | 变体是代码路径分叉，不是参数差异 |

## 四、运行时动态切换的特殊情况

`shader_feature` 有一个经常被忽略的陷阱：

如果在运行时通过 `material.EnableKeyword()` 或 `Shader.EnableKeyword()` 动态切换 keyword，但这个 keyword 是用 `shader_feature` 声明的，那么：

- 如果没有构建使用证据或显式保留路径，对应组合可能被过滤
- 运行时缺少精确组合时，可能选择相近变体，或在严格匹配检查中暴露缺失；不能只凭没有粉色画面判定正确

Editor 的动态准备与 Player 的构建结果不同，Editor 运行正常不能证明目标 Player 具备所需组合；具体诊断按[缺失排查流程]({{< relref "rendering/unity-shader-variant-missing-diagnosis-flow.md" >}})进行。

**处理方式**：

1. 明确运行时需要的组合与作用范围，评估 `multi_compile` 等声明；这一步不保证后续不会裁剪。
2. 若使用 SVC 等显式集合，确认它实际参与目标构建及相应保留配置，不能只证明文件中有记录。
3. `Always Included` 可以作为有代价的保留手段或对照条件，但不代表跳过所有管线过滤；继续核对候选、构建与交付证据。

<!-- DATA-TODO: 固定 Unity/管线/API 与 Player/Bundle 版本，记录静态、动态及显式保留候选，检查各层裁剪及目标 Player 实际命中。未取得回执和运行记录前，不把声明、SVC 或 Always Included 写成最终保留保证。 -->

## 五、keyword 数量的健康边界

没有通用的“超过几条声明就失控”标准，应先分清理论候选与实际保留：

- 新增独立、具有两个状态的声明，会将相应理论组合数乘以 2；不能把所有声明都默认当成二选一
- `shader_feature` 的使用过滤可能缩小实际候选，后续处理还会继续影响结果
- 引擎数量限制、声明作用范围、理论组合与最终报告是不同指标，应按同一版本和统计阶段记录

判断增长是否异常，要结合每组状态数、Shader/pass/阶段、API、使用需求和前次可比构建，而不是套统一的声明条数阈值。

更健康的做法是：

- 将管线内置与自定义声明一起纳入候选分析，不按旧版槽位预算推算
- 使用需求适合时选 `shader_feature`，保留动态路径的明确证据
- 不需要全局状态覆盖时评估 `_local`，避免作用范围混用
- 清理前核对材质、脚本、显式集合与热更内容的消费者

## 最后收成一句话

`声明决定候选与使用过滤的起点，作用范围决定状态如何控制；最终保留与运行可用，还要沿构建、裁剪和交付继续确认。`
