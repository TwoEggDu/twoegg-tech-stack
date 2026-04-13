---
date: "2026-03-24"
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
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
Shader variant 数量为什么会失控，最终能追溯到源头的问题通常只有一个：

`keyword 设计错了。`

不是 stripping 太弱，不是 SVC 没建好，而是在更前面的地方，keyword 的声明方式就已经把变体数量推高了。

所以这篇要解决一个比"怎么治理变体"更早的问题：

`keyword 该怎么设计，才不会在源头就把变体数量做大。`

## 先给一句总判断

如果把整件事压成一句话：

`multi_compile 产生的变体无论材质是否使用都会被编进构建，shader_feature 产生的变体只在材质实际启用时才会被保留；_local 后缀解决的是全局 keyword 空间污染问题，而不是变体数量问题。`

这句话里最关键的是两个判断：

- 谁决定"变体要不要生成"
- 谁决定"keyword 空间是否被共享"

只要这两个问题分清楚，后面的选择就不会乱。

## 一、先把四种声明方式的核心差异说清楚

### 1. multi_compile

```glsl
#pragma multi_compile __ _FOG_LINEAR _FOG_EXP _FOG_EXP2
```

**构建行为**：无论项目里有没有材质使用这些 keyword 组合，所有排列都会被编进构建。

`multi_compile` 不关心 `usedKeywords` 收集结果。它的每一条排列都会参与枚举，不依赖材质证据，也不受 `shader_feature` 那套材质驱动的剔除逻辑影响。

**适合的场景**：

- 运行时会被动态切换、且无法在构建期通过材质状态确定哪些组合会被用到
- 必须在所有情况下都确保存在，不能依赖材质来保留

Unity 内置的雾效、光照贴图、GPU Instancing 等全局功能都用 `multi_compile`，原因正是这类功能在运行时由全局状态控制，不是由单个材质决定的。

### 2. shader_feature

```glsl
#pragma shader_feature __ _EMISSION _NORMALMAP
```

**构建行为**：只有在 `usedKeywords` 里出现的 keyword 组合才会被编进构建。

换句话说，如果项目里没有任何材质启用 `_EMISSION`，这条路径就会被剔掉，不会出现在最终构建结果里。

**适合的场景**：

- 由材质静态开关控制的功能
- 大多数材质不会同时启用所有 keyword 的情况
- 功能是否开启在构建期已经可以通过材质状态确定

这也是为什么 URP Lit 的大部分功能开关用的是 `shader_feature`——贴图、法线、自发光这些特性，是否启用由每个材质自己决定，不会在运行时被全局切换。

### 3. multi_compile_local 和 shader_feature_local

```glsl
#pragma multi_compile_local __ _CUSTOM_FEATURE
#pragma shader_feature_local __ _MATERIAL_DETAIL
```

**构建行为**：和不带 `_local` 的版本完全相同——`multi_compile_local` 总是编，`shader_feature_local` 只在材质使用时编。

`_local` 后缀解决的是**不同的问题**：它让这个 keyword 成为"本地 keyword"，不占用全局 keyword 槽位。

Unity 对每个 shader 程序的全局 keyword 数量有上限（在不同平台和 Unity 版本上约为 256 个）。如果大量 shader 都声明全局 keyword，整个项目会迅速逼近这个上限，触发"关键字空间耗尽"错误。

`_local` 把 keyword 限定在当前 shader 的私有空间里，不影响全局计数。

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

每一行 `multi_compile` 乘进来，变体数量就翻一倍。三行就是 2×2×2 = 8 倍。

而这些功能如果是材质静态开关，用 `shader_feature` 就完全够了。构建期只会保留材质真正用到的组合。

**原则**：功能是否启用由材质决定且不会在运行时动态切换 → 用 `shader_feature`。

### 2. 多个布尔 keyword 没有合成枚举

```glsl
// 变体数量：2 × 2 × 2 = 8
#pragma shader_feature __ _QUALITY_LOW
#pragma shader_feature __ _QUALITY_MEDIUM
#pragma shader_feature __ _QUALITY_HIGH
```

这三个 keyword 如果是互斥的，应该合成一个枚举：

```glsl
// 变体数量：4（null + 三个值）
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

如果这条 keyword 同时被某个 SVC 显式登记，或者被 `multi_compile` 误声明，它就会持续产生变体。

定期审查 shader 声明，删掉没人使用的 keyword，是维持变体数量健康的基本操作。

## 三、决策表

| 场景 | 推荐声明 | 原因 |
|------|---------|------|
| 全局渲染功能，运行时动态切换（雾效、阴影质量档） | `multi_compile` | 必须总是存在，不依赖材质 |
| 材质功能开关，静态确定（法线贴图、自发光） | `shader_feature` | 只保留材质真正用到的组合 |
| 材质功能开关，但 keyword 数量多怕冲突 | `shader_feature_local` | 不占全局 keyword 槽位 |
| 运行时脚本会动态 Enable/Disable keyword | `multi_compile` 或 `multi_compile_local` | shader_feature 的组合在构建期可能被剔 |
| 互斥的多档选项 | 同一行多值声明 | 避免乘积式变体增长 |
| 只影响参数值，不影响代码路径 | 不用 keyword，用 uniform | 变体是代码路径分叉，不是参数差异 |

## 四、运行时动态切换的特殊情况

`shader_feature` 有一个经常被忽略的陷阱：

如果在运行时通过 `material.EnableKeyword()` 或 `Shader.EnableKeyword()` 动态切换 keyword，但这个 keyword 是用 `shader_feature` 声明的，那么：

- 如果构建期没有材质使用这个 keyword 组合，对应的 variant 就不会被编进包
- 运行时切换后，对应 variant 不存在，效果就会出错

这类问题在编辑器里不会暴露（编辑器里 shader 是动态编译的），但在真机构建里会失效。

**处理方式**：

1. 如果这个 keyword 必须在运行时动态切换，改用 `multi_compile`
2. 或者用 SVC 显式登记这个 keyword 组合，让它进入构建的 `usedKeywords`
3. 或者用 `Always Included` 让整个 shader 绕过 `usedKeywords` 枚举

## 五、keyword 数量的健康边界

没有一个精确的"每个 shader 最多几个 keyword"的标准，但有几个参考值：

- 每新增一个 **boolean** `multi_compile`，变体数量**翻倍**
- 每新增一个 **boolean** `shader_feature`，变体数量**最多**翻倍（实际取决于材质使用面）
- 全局 keyword 总数通常有硬限制（因平台而异，常见为 256）

实际工程中，一个 shader 如果有超过 5-6 条 `multi_compile` 声明，变体数量通常就已经开始失控了。

更健康的做法是：

- 把 URP 或 HDRP 的 built-in keyword 计入总量，自定义 keyword 要从这个额度里挤
- 优先用 `shader_feature` 而不是 `multi_compile`
- 优先用 `_local` 版本降低全局 keyword 压力
- 定期审查，把已经没有材质使用的 `shader_feature` 声明清理掉

## 最后收成一句话

`multi_compile 总是生成变体，shader_feature 只在材质使用时生成变体；keyword 设计错误是变体爆炸的根源，stripping 只能减轻后果，不能从根上解决问题。`
