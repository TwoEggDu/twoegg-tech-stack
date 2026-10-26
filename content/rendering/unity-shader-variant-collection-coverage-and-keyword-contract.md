---
date: "2026-03-24"
lastmod: "2026-10-26"
title: "Shader Variant 收集的覆盖边界：静态扫描看不到什么，以及 Keyword 使用契约"
description: "区分资产静态扫描、Editor 使用追踪与动态 Keyword 需求声明，说明如何把覆盖契约接到构建保留、交付和目标 Player 验证。"
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
series: "Unity Shader Variant 治理"
---
> 如果只用一句话概括这篇，我会这样说：只扫描保存资产的收集器，不能推导所有未被记录的动态状态；把动态需求显式登记，可以让它们可审查，但构建保留、交付与目标 Player 命中仍需各自证据。

> 版本与证据范围：工具定位参考 Unity 6000.0 文档，代码与数据库记录为教学示意。没有实际收集器、保留规则执行、构建回执或目标 Player 结果，以下建议不代表已经证明全覆盖。

前几篇把收集的工具和方法讲清了。这篇要回答一个更根本的问题：

**不管用什么收集器，有没有一类变体是系统性扫不到的？如果有，怎么处理？**

---

## 静态收集器在做什么

Unity 项目里，收集变体的常见方式是扫描资产文件：扫 `.mat` 文件、扫场景依赖、扫 Prefab 依赖。这类方式统称**静态收集**。

静态收集器的工作原理用一句话说：

```
读取材质资产 → 取得保存的 keyword 状态 → 记录这个 shader + 这组 keyword
```

这里读的是**材质资产当前保存的 keyword 状态**。美术在编辑器里通过相应材质界面改变功能并存盘后，收集器可以读取保存结果；具体存储字段和访问接口按版本核对，不把示意当作固定序列化布局。

静态收集覆盖的是：**在资产文件里有明确记录的 keyword 组合。**

---

## 静态收集看不到什么

有些动态需求没有保存在扫描范围内的 `.mat` 文件里，而是由运行时代码产生。

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

假设 `terrainMat` 对应资产保存的是默认状态，比如“无关键字”。运行时天气切到下雨，就会请求 `_WET_SURFACE` 对应状态。

如果扫描范围内只保存默认状态，又没有其他声明或分析输入，单凭这些资产就不能推导 `_WET_SURFACE` 的动态需求。若已有雨天材质或其他显式记录，它也可能被静态收集；关键在于输入范围，而非“动态组合必然不在任何文件里”。

### 情况二：运行时 new 出来的材质

```csharp
// 动态创建的材质，没有对应的 .mat 文件
Material mat = new Material(Shader.Find("Custom/Surface"));
mat.EnableKeyword("_DISSOLVE_EFFECT");
mat.SetFloat("_DissolveThreshold", progress);
```

这段代码创建的材质没有对应 `.mat` 文件；只扫描保存资产且没有其他输入的收集器，不能仅靠反复扫描推导这条代码可能产生的状态。

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

全局状态只会影响使用相关、可被覆盖关键字的 Shader，不能说影响所有材质。若项目支持上述两个档位，应将两种动态需求登记，并确认它们在对应交付配置中可用；只读材质默认状态无法证明这一点。

### 情况四：热更内容的材质

热更材质可能仍在同一项目的 `Assets/` 目录，但未被纳入本次 Player 依赖或收集范围；另一个工程的远端内容则可能根本不在当前资产数据库。应确认收集器是否显式包含相关 Bundle 输入，不能把“热更”直接等同于 `GetDependencies` 一定找不到。

---

## 运行时追踪：另一条路

Unity Editor 可以追踪运行中使用的变体，再从 Graphics Settings 的 **Tracked shader variants** 创建 SVC；可先清理旧记录，再运行所需路线并创建资产。具体界面见 [Graphics Settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-GraphicsSettings.html#shader-loading)与 [SVC 创建说明](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variant-collections.html)。这不是对目标设备每一次底层编译事件的完整追踪保证。

Editor 使用追踪可以补充资产扫描遗漏的组合，但仍要检查追踪得到的实际记录及其表达范围；代码路径被执行，不等于已经证明所有 API、质量档和交付组合都可用。

```
计划运行指定路线与状态
  → 检查 Editor 当前追踪的使用记录
  → 创建并审查 SVC 中的 Shader / PassType / keywords
  → 另行确认构建保留、交付与目标 Player 使用结果
```

这类追踪有一个根本限制：**覆盖质量取决于运行路线与记录范围**。没有跑到的路径——比如只在节日活动中触发的效果——可能没有对应证据；即使 Editor 路线通过，也不能替代目标 Player 和 Bundle 组合的验证。

---

## 两种方式的对比

| | 静态收集 | 运行时追踪 |
|---|---|---|
| **覆盖内容** | 扫描范围中保存的 keyword 状态与显式输入 | 本次 Editor 路线实际追踪到的组合 |
| **盲区** | 没有静态证据的动态需求与范围外内容 | 未运行状态、记录范围外或不同交付条件 |
| **可重复性** | 固定输入、配置与工具版本后可比较 | 还需固定路线、状态和追踪起点 |
| **适合场景** | 大多数"写死在材质上"的变体 | 需要补充动态路径 |

两者不是替代关系，而是互补：静态收集打底，运行时追踪补充动态路径。

---

## Keyword 使用契约：在设计阶段登记动态需求

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

**需求可追溯**：即使某条路径尚未跑到，也可以先登记它为什么需要这个组合。登记本身不保证保留；未登记的组合也可能因其他构建输入而存在，不能用数据库有无记录直接判断最终产物。

声明需接入项目的保留与裁剪规则，再检查构建回执、Player/Bundle 交付及目标 API 的实际使用。SVC 可以承载一部分显式组合，但不能绕过所有过滤，见[裁剪层次]({{< relref "rendering/unity-shader-variant-stripping-stages-prefiltering-builtin-custom.md" >}})。

**可审查性**：每条动态路径都有文字说明——谁写的、为什么要有。新人接手代码时能立刻理解这个组合的来源。

**进 git**：项目若以资产或配置文件保存数据库，变更可以被审查。应要求新增动态开关时同步提交记录，reviewer 看到的是新增需求；哪些实际保留还要由构建证据确认。

### 契约的成本

**依赖纪律与接入**：如果没有自动检查，就依赖团队同步登记；漏登记会让需求失去审查依据。即使登记完整，保留规则或交付接入缺失仍可能造成不可用，必须分别检查。

**组合数量**：两个独立声明各有 3 个状态，理论组合为 `3×3=9`；实际登记应覆盖项目需要的合法状态，不把理论组合数当成最终产物数。状态多时，手动维护的工作量也会增加。

---

## 减少组合爆炸：`shader_feature` 的作用

`multi_compile` 的候选不依赖与 `shader_feature` 相同的材质使用过滤；`shader_feature` 通常可据构建使用证据缩小候选。两者的最终保留仍受后续构建处理影响。

这个区别对 Keyword 使用契约很重要：

- **适合用 `multi_compile` 的**：运行时会被全局或动态开关、无法在构建期确定哪些组合会被用到的 keyword（天气、画质档位、设备能力）
- **适合用 `shader_feature` 的**：每个材质独立决定是否开启的功能（法线贴图、自发光、透明度混合模式）

保存的材质状态可以为 `shader_feature` 提供使用证据，但不自动证明所有动态状态或最终产物。运行时若改变状态，也应登记需求并确认保留，不能把声明类型当作没有覆盖风险的证明。

**契约针对缺少充分静态证据的动态需求，不只针对一种声明**。若静态输入已覆盖所需状态，可复用其证据；若保留了动态切换，就继续检查声明、范围、保留与目标使用。`shader_feature` 并非 API 上不能动态切换，而是缺少精确组合时可能出现不正确匹配。

---

## 官方文档参考

- [Unity 6000.0：关键字类型与候选](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-conditionals-choose-a-type.html)
- [Unity 6000.0：ShaderVariantCollection](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ShaderVariantCollection.html)
- [Unity 6000.0：关键字作用范围](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-keywords-scripts.html)

## 实际操作建议

**在代码里写动态开关时，同步更新数据库**：

把"写 `EnableKeyword` / `DisableKeyword`"和"在 ShaderVariantDatabase 里添加对应记录"作为同一个任务的两个步骤，不是两件独立的事。

**PR review 时检查数据库变更**：

凡是 diff 里有 `EnableKeyword` 或 `DisableKeyword` 的 PR，review checklist 里加一项：确认 ShaderVariantDatabase 里有对应的 manual 记录。

同时检查需求是否接到当前构建规则，Player/Bundle 与 API 配置是否对应，保留和实际使用是否有结果；缺少结果时标记待验证，不以记录已提交代替通过。

**新增动态开关时估算组合数量**：

如果新 keyword 会和已有的多个 `multi_compile` 产生交叉，先算一下组合数量。`shader_feature` 能解决问题的，优先用 `shader_feature`，避免不必要的笛卡尔积膨胀。

<!-- DATA-TODO: 实现并审查需求登记到保留/裁剪规则的接入，固定 Unity/管线/API、质量状态和 Player/Bundle 版本，分别保留收集、构建与目标运行记录。当前没有执行这些验证，也没有“声明即全覆盖”的结果。 -->
