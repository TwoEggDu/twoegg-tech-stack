---
date: "2026-03-24"
title: "URP 的 Shader Variant 管理：Prefiltering、Strip 设置和多 Pipeline Asset 对变体集合的影响"
description: "把 URP 特有的变体管理机制拆成 Shader Prefiltering、Strip Unused Shader Variants、多 Pipeline Asset 与质量档，讲清 URP 项目里变体问题和通用机制的不同之处。"
slug: "unity-urp-shader-variant-prefiltering-strip-settings"
weight: 110
featured: false
tags:
  - "Unity"
  - "Shader"
  - "URP"
  - "Variant"
  - "Build"
series: "Unity Shader Variant 治理"
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
前面几篇把变体的通用机制都讲清楚了：keyword 设计、构建期枚举、stripping、SVC、Always Included、运行时命中。

但如果项目用的是 URP，还有一层很容易忽略的东西：

`URP 在通用机制之上，还有自己的一套变体过滤逻辑。`

这套逻辑不在 `IPreprocessShaders` 里，不在 `Always Included` 里，也不在你自己写的 stripping 规则里——它更早，发生在 URP 自己的构建处理阶段。

如果不了解它，就会遇到很典型的困惑：

- 构建日志里某个 URP shader 的变体数量远少于理论值，但自己根本没写 stripping 规则
- 某个 Renderer Feature 一开一关，构建出来的变体数量差距很大
- 多个画质档配置下，变体集合变化难以预测
- 某条在编辑器里正常的路径，真机上效果不对但材质不粉

这篇的目标就是把 URP 特有的变体管理机制讲清楚。

## 先给一句总判断

`URP 在通用构建流程的枚举阶段之前，会根据当前 URP Pipeline Asset 的功能配置，提前把不可能被用到的变体路径剪掉；理解这套机制，是 URP 项目做变体治理的前提。`

## 一、URP Shader Prefiltering：比 usedKeywords 更早的一刀

通用流程里，变体是否进入枚举，取决于 `usedKeywords`——也就是材质贡献的 keyword 组合。

URP 在这之前还有一步：**Shader Prefiltering**。

### 1. 它做的事

URP 构建时会分析当前 Pipeline Asset 里开启了哪些功能，把不可能被触发的 shader 路径直接标记为无效，从而在枚举阶段就不生成这些变体。

例如：

- 如果 URP Asset 关闭了 HDR，HDR 相关的变体路径会被直接排除
- 如果关闭了 Depth Texture，依赖深度纹理的路径会被排除
- 如果关闭了 Additional Lights，额外光源相关的变体会被排除

### 2. 它为什么会让变体数量"意外地少"

很多项目发现构建日志里 URP Lit 的变体数量远低于预期，以为是 stripping 在工作，其实往往是 Prefiltering 提前剔掉了大量路径。

这是好事——它让 URP 项目在配置层就能自然减少变体数量。但它也带来一个问题：

`如果 URP Asset 配置不够完整，Prefiltering 可能剔掉运行时真正需要的变体。`

### 3. 常见的 Prefiltering 陷阱

**多 URP Asset 不一致**

如果项目有多个 URP Asset（不同画质档），构建时只会基于当前 Quality Settings 里生效的那个 Asset 做 Prefiltering。

如果高画质 Asset 开了某个功能，低画质 Asset 没开，而构建时用的是低画质 Asset，那高画质需要的变体会被 Prefiltering 剔掉，高画质档运行时就会出问题。

**解决方式**：构建时需要把所有可能被使用的 URP Asset 都传给构建系统，让 Prefiltering 基于所有 Asset 的并集来做，而不是只用一个。这可以通过 `GraphicsSettings.renderPipelineAsset` 的配置或通过 SVC 显式保留关键路径来处理。

## 二、Strip Unused Shader Variants 设置

URP Asset 的 General 设置里有一个 `Strip Unused Shader Variants` 开关。

### 1. 它做的事

开启后，URP 会根据当前 Pipeline Asset 的配置，在构建期剔除当前配置下"不可能被用到"的变体。

和 Prefiltering 不同，这个开关更接近 URP 自定义的 `IPreprocessShaders` 实现——它发生在枚举之后、最终编译之前。

### 2. 它不做的事

这个开关只基于 URP Asset 的功能配置来判断，不会基于场景里材质的实际 keyword 使用情况做精细剔除。

换句话说，即使开启了这个设置，如果某个 URP 功能在 Pipeline Asset 里是开启的，对应的变体就会被保留，不管项目里是否真的有材质用到了这条路径。

### 3. 和自定义 stripping 的关系

URP 的这套 stripping 和自定义 `IPreprocessShaders` 是叠加关系，不是替代关系。

构建日志里：

```
After built-in stripping: N    ← 包含了 URP 内置 stripping
After scriptable stripping: M  ← SRP + 自定义 stripping
```

URP 内置 stripping 的结果会在 `After built-in stripping` 里体现，和 Unity 的全局 feature stripping（雾效、光照贴图等）在同一个数字里。

## 三、Renderer Features 对变体的影响

URP Renderer 上挂载的 Renderer Feature 会直接影响变体集合，这是很多项目没想到的。

### 1. 每个 Renderer Feature 可能引入新的变体路径

例如：

- **Screen Space Ambient Occlusion (SSAO)**：开启后会引入 SSAO 相关的 keyword，产生对应变体
- **Decal Renderer Feature**：引入 Decal 相关路径
- **Screen Space Shadows**：引入阴影相关变体路径

这些变体不是你的自定义 shader 产生的，而是 URP 内置处理逻辑要求的。

### 2. 问题在于"开了但没用"

很多项目的 Renderer 上挂了很多 Feature，但实际上只用了其中几个。其余的 Feature 即使没有材质使用它们，也会因为 Renderer 上存在而影响构建期的变体生成。

### 3. 排查方式

如果发现某个 URP shader 的变体数量突然增加，检查：

- 最近是否有人在 Renderer 上添加了新的 Feature
- 哪些已有 Feature 是否真的在使用

清理掉不用的 Renderer Feature，是减少 URP 变体数量最直接的方式之一。

## 四、多 Pipeline Asset 与多质量档的变体问题

这是 URP 项目最容易踩的坑之一。

### 1. 问题的结构

典型场景：

- 低端机使用低画质 URP Asset：关闭阴影、关闭 HDR、关闭 SSAO
- 高端机使用高画质 URP Asset：开启阴影、开启 HDR、开启 SSAO

构建时如果只基于一个 Asset 做 Prefiltering，另一个 Asset 需要的变体可能就被剔掉了。

### 2. 正确处理方式

**方式一：在 Graphics Settings 里设置所有 Pipeline Asset**

确保所有质量档的 URP Asset 都被 Graphics Settings 引用。Unity 构建时会把所有引用的 Pipeline Asset 都纳入 Prefiltering 的考量范围。

**方式二：用 SVC 显式保护跨 Asset 的关键路径**

对于某些 Asset 下特有的变体，用 SVC 显式登记确保它们不被 Prefiltering 误伤。

**方式三：在 CI 里针对每个质量档分别做构建验证**

不要只验一个档，每个质量档的关键入口都应该有对应的构建回归。

## 五、Forward vs Deferred 对变体集合的影响

URP 在 Forward 和 Deferred 渲染路径下，shader 的变体集合是完全不同的。

- Forward Rendering Path：光照计算在 shader 里做，需要额外光源、阴影等相关变体
- Deferred Rendering Path：G-Buffer pass、Lighting pass 是独立的，需要不同的 shader 变体集合

如果项目里两种渲染路径都可能用到（例如某些平台用 Forward，某些用 Deferred），那这两套变体都需要包含在构建里。

如果只用一种，确认 URP Asset 里没有多余的渲染路径开启，是减少变体的低成本操作。

## 六、URP 变体问题排查的额外步骤

在通用的三层排查（枚举阶段 / stripping 阶段 / 运行时命中）之外，URP 项目还需要额外检查：

| 检查项 | 目的 |
|--------|------|
| 构建时生效的是哪个 URP Asset | 确认 Prefiltering 基准是否完整 |
| 所有质量档的 URP Asset 是否都被 Graphics Settings 引用 | 防止跨档变体漏掉 |
| Renderer 上是否有未使用的 Renderer Feature | 排查变体数量异常增加的来源 |
| Strip Unused Shader Variants 是否开启 | 确认 URP 内置 stripping 是否生效 |
| 出问题的路径是 Forward 还是 Deferred | 确认渲染路径配置是否匹配 |

## 最后收成一句话

`URP 的变体管理在通用机制之上多了一层：Prefiltering 基于 Pipeline Asset 配置提前剪掉不可能的路径，Strip Unused Shader Variants 在枚举后做 URP 配置级的剔除；多画质档项目的核心风险是构建时只用了部分 Asset 做 Prefiltering，导致其他档位需要的变体被漏掉。`
