---
date: "2026-04-12"
title: "变体实践：怎样确保项目的 Shader 变体被正确保留"
description: "按项目设置的顺序，把确保 Shader 变体正确保留的完整流程串成一条可执行的检查链：从 keyword 审计、管线配置、Always Included 边界、SVC 分组、裁剪日志、WarmUp 配置到构建验证，每一步给出具体动作。"
slug: "unity-shader-variant-retention-practice-project-setup"
weight: 160
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Build"
  - "SVC"
  - "Practice"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
前面的文章已经把 Shader Variant 的生命周期、保留机制、裁剪层级和运行时命中都拆清楚了。但有一个问题一直没有被单独回答：

`我现在要确保项目的 Shader 变体被正确保留，从第一步到最后一步，到底该怎么做？`

这篇文章不讲原理（前面讲过了），只按项目设置的顺序，给出一条可执行的检查链。

## 第一步：审计 Shader 的 keyword 声明

在做任何保留策略之前，先搞清楚你的项目里到底声明了什么。

### 要做的事

- 列出项目里所有自定义 Shader（不含 Unity 内置和 URP 包内的）
- 对每个 Shader，统计它用了多少 `multi_compile` 和 `shader_feature`，每组有几个 keyword
- 计算理论变体数量（每组 keyword 数相乘）

### 判断标准

- `multi_compile` 声明的 keyword：所有组合都会被编译，无论有没有材质在用。如果组数多、每组选项多，变体数量会爆炸
- `shader_feature` 声明的 keyword：只编译被材质或 SVC 引用的组合。如果某条路径没有任何材质引用也没有 SVC 登记，它不会被编译进来
- `shader_feature_local`：和 `shader_feature` 类似但作用域是局部的，不受全局 keyword 影响

### 常见问题

- 把本应用 `shader_feature` 的路径写成了 `multi_compile`，导致大量永远不会走到的变体被编译进来
- 把本应用 `multi_compile` 的全局功能开关（比如质量档切换）写成了 `shader_feature`，导致运行时切换后找不到对应变体

### 动作

如果发现 keyword 声明不合理，先修正声明再做后续步骤。这是变体治理的源头。

## 第二步：检查 URP Asset 和管线配置

URP 的 Prefiltering 会在所有 stripping 之前，根据当前管线配置提前过滤掉"不可能发生"的变体路径。如果管线配置不覆盖项目实际需要的功能，变体会在这一层就被干掉。

### 要做的事

- 打开 `Graphics Settings`，检查 `Scriptable Render Pipeline Settings` 引用的是哪个 URP Asset
- 如果项目有多个质量档（Quality Levels），检查每个质量档是否关联了对应的 URP Asset
- 打开每个 URP Asset，检查以下功能开关是否和项目实际需求一致：
  - HDR
  - MSAA
  - Depth Texture / Opaque Texture
  - Shadow 类型和级联数
  - Renderer Features（Decal、SSAO、Screen Space Shadows 等）
  - Additional Lights / Per Object Limit

### 判断标准

构建时，Unity 会收集所有被 Quality Level 引用的 URP Asset，合并它们的功能开关。如果某个功能只在高画质档开启，但高画质档的 URP Asset 没有被任何 Quality Level 引用，那个功能对应的变体就不会被保留。

### 常见问题

- 项目有 3 个质量档，但 `Quality Settings` 里只有 2 个关联了 URP Asset，第 3 个是空的——导致该档的功能开关丢失
- 开发时用的 URP Asset 开了 Decal Layers，但实际构建用的 URP Asset 没开——开发时正常，构建后 Decal 效果消失

### 动作

确保所有目标质量档都关联了正确的 URP Asset，且每个 URP Asset 的功能开关覆盖了该档实际需要的渲染特性。

## 第三步：识别需要进 Always Included 的 Shader

不是所有 Shader 都需要 Always Included。它的正确使用场景是：需要由 Player 全局兜底、不依赖具体 bundle 或场景的 Shader。

### 要做的事

- 列出项目里哪些 Shader 属于全局基础设施：
  - 所有 bundle 都可能引用的公共 Shader（URP/Lit、URP/SimpleLit、自定义全局 Shader）
  - 后处理 Shader
  - UI Shader
  - 粒子/特效的基础 Shader
  - Fallback Shader
- 把这些 Shader 加入 `Graphics Settings → Always Included Shaders`

### 判断标准

- 如果一个 Shader 被多个 AssetBundle 引用，但没有进 Always Included，每个 bundle 会各自携带一份——浪费包体，且可能出现版本不一致
- 如果一个 Shader 只被一两个 bundle 使用，没有全局兜底的必要——它应该跟着 bundle 走，不需要 Always Included

### 注意

Always Included 的 Shader 会使用 `kShaderStripGlobalOnly` 级别的裁剪——只根据全局设置（雾、光照贴图模式等）裁剪，不根据逐材质 keyword 使用情况裁剪。这意味着它保留的变体数量可能比普通 Shader 多。不要把所有 Shader 都塞进 Always Included。

## 第四步：按入口分组建 SVC

ShaderVariantCollection 的职责是：显式登记项目关心的关键变体路径，用于构建保留和运行时预热。

### 要做的事

- 按游戏入口分组创建 SVC：
  - `SVC_Login`：登录和首屏需要的 Shader 变体
  - `SVC_MainUI`：主界面需要的变体
  - `SVC_Combat`：战斗场景的关键变体
  - `SVC_Common`：全局公共变体（如果没进 Always Included 的关键路径）

- 收集变体的方法：
  - 在编辑器中打开目标场景，使用 `Shader Variant Collection` 的录制功能捕获使用到的变体
  - 通过 `IPreprocessShaders` 回调记录构建期实际编译的变体，和 SVC 做交叉对比
  - 对于动态创建的材质或运行时切换的 keyword，需要手动添加对应变体到 SVC

### 判断标准

- SVC 不是越全越好——它登记的每条路径都会参与构建保留和 WarmUp，太多会增加构建时间和首次预热时间
- SVC 也不是什么都不登记——如果某条关键路径没有被任何场景直接引用（比如动态生成的材质用到的变体），不登记就不会被保留

### 动作

建好 SVC 后，把它们放到 `Graphics Settings → Preloaded Shaders` 或者作为构建资产加入 Addressables/YooAsset 的构建输入。

## 第五步：配置 IPreprocessShaders 裁剪日志

在做任何自定义裁剪之前，先建观测能力。

### 要做的事

创建一个 `IPreprocessShaders` 的实现，不做任何裁剪，只记录日志：

- 记录每个 Shader 的名称、Pass 名称、keyword 列表
- 统计每次构建的变体总数
- 把日志输出到文件，方便对比不同构建之间的变化

### 判断标准

- 如果你还没有这个日志，你对"构建里到底保留了多少变体"是盲的
- 对比两次构建的日志，可以发现哪些变体新增了、哪些消失了

### 动作

先跑一次 Development Build 和一次正式 Build，对比日志。确认关键 Shader 的变体数量在预期范围内，再考虑是否需要添加自定义裁剪规则。

## 第六步：配置 WarmUp 时机

变体保留进了构建产物后，运行时还需要在正确的时机预热，避免首帧卡顿。

### 要做的事

- 在 Loading Screen 期间调用 SVC 的 `WarmUp()` 或 `WarmUpProgressively(int variantCount)`
- `WarmUp()` 一次性预热所有变体，适合变体数量较少的 SVC
- `WarmUpProgressively(N)` 每次预热 N 个变体，返回 `true` 表示全部完成。适合变体数量多的 SVC，可以分散到多帧

### 判断标准

- WarmUp 应该在 Loading Screen 关闭之前完成
- 如果 WarmUp 耗时太长，考虑拆分 SVC（只预热当前场景需要的），或使用 `WarmUpProgressively` 分帧执行
- WarmUp 只能预热已经存在于构建产物中的变体——如果变体在构建期就被裁掉了，WarmUp 不会把它变出来

### 动作

在场景切换的 Loading 流程中加入 SVC WarmUp 步骤，确保在 Loading Screen 关闭前完成。

## 第七步：用测试构建验证

所有配置做完后，必须用实际构建验证。

### 要做的事

- 打一个目标平台的 Development Build
- 开启 `Player Settings → Other Settings → Strict Shader Variant Matching`（Unity 2022.1+）。它会把运行时的模糊退化变成明确的错误日志，帮你发现缺失的变体
- 开启 `Player Settings → Other Settings → Log Shader Compilation`，看运行时是否有未预热的变体在首用时编译
- 跑一遍关键场景流程：登录 → 主界面 → 第一场战斗 → 场景切换
- 检查：
  - 有没有粉材质
  - 有没有效果不对的地方（退化命中）
  - 有没有首帧卡顿（Log Shader Compilation 会报）
  - Console 或 logcat 里有没有 Shader 相关的 warning/error

### 如果发现问题

- 粉材质 → 回到第三步检查 Always Included 和第四步检查 SVC 覆盖
- 效果不对 → 开启 Strict Shader Variant Matching 获取精确的缺失信息，回到对应层排查
- 首帧卡顿 → 回到第六步检查 WarmUp 是否覆盖了对应变体
- AB 加载后才出问题 → 检查 Shader 的交付责任归属（是跟 Player 走还是跟 bundle 走），回到第三步

## 官方文档参考

- [Graphics Settings](https://docs.unity3d.com/Manual/class-GraphicsSettings.html)
- [ShaderVariantCollection](https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html)
- [IPreprocessShaders](https://docs.unity3d.com/ScriptReference/Build.IPreprocessShaders.html)
- [Shader loading](https://docs.unity3d.com/Manual/shader-loading.html)

## 总结检查表

| 步骤 | 核心动作 | 确认标准 |
|------|---------|---------|
| 1 审计 keyword | 列出所有 `multi_compile` / `shader_feature`，修正不合理的声明 | 没有不必要的 `multi_compile` |
| 2 管线配置 | 检查所有质量档的 URP Asset 功能开关 | 所有功能开关覆盖实际需求 |
| 3 Always Included | 把全局公共 Shader 加入 Always Included | 全局基础 Shader 不依赖 bundle |
| 4 SVC 分组 | 按入口创建 SVC，登记关键路径 | 关键路径有显式保留依据 |
| 5 裁剪日志 | 配置 IPreprocessShaders 日志 | 能观测每次构建的变体统计 |
| 6 WarmUp | 在 Loading Screen 期间预热 SVC | Loading 关闭前预热完成 |
| 7 构建验证 | 用 Strict Matching + Log Compilation 跑关键流程 | 无粉材质、无退化、无首帧卡顿 |
