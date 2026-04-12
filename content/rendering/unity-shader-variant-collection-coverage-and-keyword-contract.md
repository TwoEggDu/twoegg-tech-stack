---
date: "2026-03-24"
title: "Shader Variant 收集的覆盖边界：静态扫描看不到什么，以及 Keyword 使用契约"
description: "讲清楚静态收集器（材质/场景/Prefab 扫描）和运行时追踪的本质区别，哪些动态路径是静态扫描覆盖不到的，以及怎么用 Keyword 使用契约在设计阶段把这个问题消灭掉。"
slug: "unity-shader-variant-collection-coverage-and-keyword-contract"
weight: 55
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Collection"
  - "Keyword"
  - "Workflow"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
> 如果只用一句话概括这篇，我会这样说：静态收集器只能看到写在资产文件里的 keyword 状态，它永远看不到运行时代码动态开关的那部分变体；消灭这个盲区最可靠的方法不是更强的扫描器，而是在项目里约定"凡是动态开关 keyword 的代码，必须在数据库里提前声明"。

前几篇把收集的工具和方法讲清了。这篇要回答一个更根本的问题：

**不管用什么收集器，有没有一类变体是系统性扫不到的？如果有，怎么处理？**

---

## 静态收集器在做什么

Unity 项目里，收集变体的常见方式是扫描资产文件：扫 `.mat` 文件、扫场景依赖、扫 Prefab 依赖。这类方式统称**静态收集**。

静态收集器的工作原理用一句话说：

```
打开 .mat 文件 → 读取 material.shaderKeywords 字段 → 记录这个 shader + 这组 keyword
```

这个字段里存的是**这个材质资产文件当前保存的 keyword 状态**。美术在编辑器里把材质的"法线贴图开关"打开，存盘，这个状态就被写进了 `.mat` 文件。下次收集时，静态扫描器就能读到。

静态收集覆盖的是：**在资产文件里有明确记录的 keyword 组合。**

---

## 静态收集看不到什么

有一类 keyword 组合，从来不会出现在任何 `.mat` 文件里——它们只存在于运行时代码的逻辑里。

### 情况一：代码根据条件动态开启关键字

```csharp
// 天气系统
void OnWeatherChanged(WeatherType weather) {
    if (weather == WeatherType.Rain)
        terrainMat.EnableKeyword("_WET_SURFACE");
    else
        terrainMat.DisableKeyword("_WET_SURFACE");
}
```

`terrainMat` 对应的 `.mat` 文件里，`shaderKeywords` 字段保存的是设计时的默认状态，比如"无关键字"。但运行时，只要天气切到下雨，材质就需要 `_WET_SURFACE` 变体。

静态收集器扫到的是"无关键字"这个变体，`_WET_SURFACE` 那个变体从来不在任何文件里——除非美术专门存了一个"雨天版本"的材质文件。

### 情况二：运行时 new 出来的材质

```csharp
// 动态创建的材质，没有对应的 .mat 文件
Material mat = new Material(Shader.Find("Custom/Surface"));
mat.EnableKeyword("_DISSOLVE_EFFECT");
mat.SetFloat("_DissolveThreshold", progress);
```

这个材质在项目里没有任何对应文件。静态扫描器无论扫多少遍，都看不到 `_DISSOLVE_EFFECT` 这个组合。

### 情况三：全局关键字由设备档位或设置驱动

```csharp
// 画质设置初始化
void ApplyQualitySettings(int tier) {
    if (tier >= QualityTier.High)
        Shader.EnableKeyword("_HIGH_QUALITY_SHADOWS");
    else
        Shader.DisableKeyword("_HIGH_QUALITY_SHADOWS");
}
```

全局关键字影响所有材质。低画质档位的设备上这个关键字是关的，高画质档位是开的。静态收集器扫到的是材质文件里存的默认状态——但这两个档位需要的变体都必须进包，否则某个档位的用户就会看到错误效果。

### 情况四：热更内容的材质

热更包里的材质在 Player 构建时不存在于 `Assets/` 目录，`AssetDatabase.GetDependencies` 找不到它们。它们的 keyword 组合同样是静态扫描的盲区。

---

## 运行时追踪：另一条路

Unity 原生提供了运行时追踪机制：在 `Graphics Settings → Shader Preloading` 里开启录制，进入 Play Mode 跑一遍游戏，Unity 会拦截每一次 variant 编译事件，把命中的 `(shader, passType, keywords)` 记录进 ShaderVariantCollection 文件。

这套机制能覆盖静态扫描的盲区——只要游戏真实跑过那条代码路径，对应的变体就会被记录。

```
Play Mode 运行 → 触发 EnableKeyword("_WET_SURFACE") → Unity 编译 _WET_SURFACE 变体
                                                            ↓
                                                  追踪器记录这次命中
                                                            ↓
                                                  写入 SVC 文件
```

但运行时追踪有一个根本限制：**覆盖质量取决于测试覆盖率**。你在 Play Mode 里跑了哪些路径，就能收集到哪些变体。没跑到的路径——比如某个只在特定节日活动里才触发的效果——追踪结果里就没有，漏掉了也不会有任何提示。

---

## 两种方式的对比

| | 静态收集 | 运行时追踪 |
|---|---|---|
| **覆盖内容** | 资产文件里存的 keyword 状态 | 实际运行过程中命中的变体 |
| **盲区** | 所有运行时动态路径 | 没有被测试跑到的路径 |
| **可重复性** | 高——每次扫描结果一致 | 低——取决于测试时走了哪些路径 |
| **适合场景** | 大多数"写死在材质上"的变体 | 需要补充动态路径 |

两者不是替代关系，而是互补：静态收集打底，运行时追踪补充动态路径。

---

## Keyword 使用契约：在设计阶段消灭盲区

追踪式补丁是被动的——变体漏了，跑一遍测试，发现了再补。更可靠的方式是**主动声明**：在写动态切换代码的同时，把这段代码可能产生的所有变体组合显式记录进数据库。

这就是 **Keyword 使用契约**。

### 契约的核心规则

```
写了 EnableKeyword("X") 的代码
  → 必须在 ShaderVariantDatabase 里有对应记录
  → 记录里说明：这个组合从哪来、为什么要保留
```

> 注意：`ShaderVariantDatabase` 不是 Unity 的内置类型。它是项目自定义的治理工具——可以是一个 `ScriptableObject`、一份 JSON 配置、或者一张 Google Sheet，具体形式由团队决定。核心是有一个明确的地方登记"哪些动态 keyword 组合是项目显式关心的"。

对应上面的天气系统例子，开发者在数据库里手动添加这些记录：

```
Shader:   Terrain/Lit
Pass:     ScriptableRenderPipeline

keyword 组合: （空）          source: manual  reason: 默认天气，无效果叠加
keyword 组合: _WET_SURFACE    source: manual  reason: 雨天，由 WeatherSystem.OnWeatherChanged 触发
keyword 组合: _SNOW_SURFACE   source: manual  reason: 雪天，由 WeatherSystem.OnWeatherChanged 触发
```

如果同时有画质档位切换：

```
keyword 组合: _HIGH_QUALITY_SHADOWS                    source: manual  reason: 高画质档位
keyword 组合: _WET_SURFACE + _HIGH_QUALITY_SHADOWS     source: manual  reason: 雨天×高画质组合
keyword 组合: _SNOW_SURFACE + _HIGH_QUALITY_SHADOWS    source: manual  reason: 雪天×高画质组合
```

### 契约的好处

**确定性**：不依赖测试覆盖率，不依赖跑过哪些路径。声明了就会进包，没声明就不进包，行为完全可预测。

**可审查性**：每条动态路径都有文字说明——谁写的、为什么要有。新人接手代码时能立刻理解这个组合的来源。

**进 git**：数据库是资产文件，变更有历史记录。新增一个动态 keyword 开关的 PR，diff 里必然包含数据库的变更——reviewer 能在 PR 里看到"这次改动新增了哪些变体"。

### 契约的成本

**依赖纪律**：没有工具层面的强制，只有团队规范。有人加了 `EnableKeyword` 忘记更新数据库，变体就会漏掉。

**组合数量**：两个独立的 `multi_compile` 各 3 个选项 = 9 条记录。选项多的时候，手动维护的工作量会增加。

---

## 减少组合爆炸：`shader_feature` 的作用

`multi_compile` 无论有没有材质使用，所有组合都进包。`shader_feature` 只有材质实际用到的组合才进包。

这个区别对 Keyword 使用契约很重要：

- **适合用 `multi_compile` 的**：运行时会被全局或动态开关、无法在构建期确定哪些组合会被用到的 keyword（天气、画质档位、设备能力）
- **适合用 `shader_feature` 的**：每个材质独立决定是否开启的功能（法线贴图、自发光、透明度混合模式）

`shader_feature` 的变体是材质驱动的——材质文件里开了这个功能，就有对应的变体；没开就不生成。这类变体静态收集器天然能扫到，不需要手动声明，也不会出现"运行时动态开关"的问题。

**Keyword 使用契约主要针对 `multi_compile` 类型的动态开关**。设计阶段如果能把某个功能开关设计成 `shader_feature`，就不需要在契约里手动声明它——代价是这个开关就不能在运行时动态改了（或者说，运行时开关后如果材质文件里没有对应状态，变体可能不在包里）。

---

## 实际操作建议

**在代码里写动态开关时，同步更新数据库**：

把"写 `EnableKeyword` / `DisableKeyword`"和"在 ShaderVariantDatabase 里添加对应记录"作为同一个任务的两个步骤，不是两件独立的事。

**PR review 时检查数据库变更**：

凡是 diff 里有 `EnableKeyword` 或 `DisableKeyword` 的 PR，review checklist 里加一项：确认 ShaderVariantDatabase 里有对应的 manual 记录。

**新增动态开关时估算组合数量**：

如果新 keyword 会和已有的多个 `multi_compile` 产生交叉，先算一下组合数量。`shader_feature` 能解决问题的，优先用 `shader_feature`，避免不必要的笛卡尔积膨胀。
