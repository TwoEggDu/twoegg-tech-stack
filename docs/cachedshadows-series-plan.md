# CachedShadows 阴影缓存专题计划

## 定位

这不是一组新的 URP 基础文章，也不是把 `Shadow Map`、`Renderer Feature`、`SVC` 这些现有内容再讲一遍。

它要解决的是一个更具体的问题：

`让刚入行、但已经开始接触项目代码的人，能够顺着 TopHeroUnity 里的真实链路，看懂 CachedShadows 是怎么替代 URP 原生主光阴影生成、为什么低配 Android 还能出影子，以及效果不对时应该怎么排查。`

这组文章的重点不是“概念更多”，而是“把已经存在的基础文，接到项目里的真实系统”。

## 为什么值得单独补

当前站内已经有这些前置文章：

- `content/engine-notes/unity-rendering-02b-shadow-map.md`
- `content/engine-notes/unity-rendering-09-urp-architecture.md`
- `content/engine-notes/urp-config-01-pipeline-asset.md`
- `content/engine-notes/urp-config-02-renderer-settings.md`
- `content/engine-notes/urp-ext-01-renderer-feature.md`
- `content/engine-notes/urp-ext-04-draw-renderers.md`
- `content/engine-notes/urp-platform-02-quality.md`
- `content/engine-notes/unity-svc-always-included-stripping-when-to-use-which.md`
- `content/engine-notes/unity-shader-variant-full-lifecycle-overview.md`
- `content/problem-solving/urp-shader-prefiltering-assetbundle.md`

这些文章已经足够讲清：

- 阴影是什么
- URP 的层级结构是什么
- Pipeline Asset / Renderer Asset / Feature 各在管什么
- Shader Variant 为什么会丢
- Frame Debugger / RenderDoc 能看什么

但它们还没有回答下面这些项目实战问题：

- CachedShadows 到底替代了 URP 阴影链路里的哪一段
- 为什么 `m_MainLightShadowsSupported = 0` 以后仍然可能有阴影
- 什么叫“缓存阴影”，缓存的到底是什么
- 静态阴影和动态阴影是怎么拼起来的
- 为什么相机移动会触发刷新
- 为什么 Editor 有、Android 没有时要先查 Quality / Feature / Hidden Shader / 预加载链路

这就是专题要补的“桥接层”。

## 与现有文章的分工

### 这组文章不重讲的内容

- 不重讲 Shadow Map 的数学原理，见 `unity-rendering-02b-shadow-map.md`
- 不重讲 URP 的四层结构，见 `unity-rendering-09-urp-architecture.md`
- 不重讲 `ScriptableRendererFeature` 的 API 入门，见 `urp-ext-01-renderer-feature.md`
- 不重讲 Quality 系统的通用机制，见 `urp-platform-02-quality.md`
- 不重讲 `SVC / Always Included / Stripping` 的通用边界，见 `unity-svc-always-included-stripping-when-to-use-which.md`

### 这组文章要补的内容

- 用 TopHeroUnity 的真实配置和代码，把“基础概念”接到“项目系统”
- 给新人一条稳定的阅读路径：先看懂系统，再学会排查
- 给排查留证据链：不是凭感觉判断“像是有阴影”，而是能证明当前到底走了哪条链路

## 专题范围

本专题第一期只聚焦 TopHeroUnity 当前最相关、也最值得新人先看懂的主线：

- `AndroidPipelineAssetLow` 的低配主光阴影方案
- `ForwardRendererAndroid` 上的 `CachedShadowsRenderFeature`
- `CachedShadowsRenderPass` 的缓存、重绘、动态叠加逻辑
- `CachedShadowsCameraManualUpdateFromMovement` 的刷新触发
- `Hidden/CopyShadowMap`、`StaticShaders.prefab`、`BundleSVC.shadervariants` 这条资源交付链
- 运行时验证与排查方法

### 第一阶段先不展开的内容

- `CachedAdditionalShadowsRenderFeature` 的完整附加光缓存专题
- Demo 资源里的通用玩法
- 脱离 TopHeroUnity 的泛化插件介绍

这些可以作为第二期扩展，不应该干扰当前主线。

## 推荐系列名称

建议用：

`CachedShadows 阴影缓存专题`

原因：

- 和现有 `URP 深度`、`Unity Shader Variant 治理` 这些系列命名风格相容
- 名称足够聚焦，读者一眼就知道是在讲一个具体系统
- 后续如果补 `CachedAdditionalShadows`，仍然可以挂在同一组专题下

## 计划篇目

建议做成 `1 篇索引 + 8 篇正文`。

### 00｜索引

- 建议标题：`CachedShadows 阴影缓存专题索引｜先看懂工作原理，再学会验证与排查`
- 建议文件名：`content/engine-notes/cachedshadows-series-index.md`
- 唯一职责：给出阅读地图、适用边界、专题范围和推荐顺序
- 必答问题：
  - 这组文章在解决什么
  - 已覆盖什么
  - 遇到什么问题先跳哪篇
  - 哪些内容故意不讲
- 建议章节：
  - 这组文章在解决什么问题
  - 先给一句总判断
  - 推荐阅读顺序
  - 按主题分组去读
  - 如果你是带着问题来查
  - 这组文章暂时没覆盖什么
- 推荐放在系列最前，并在末尾反链到现有前置文章

### 01｜工作原理 01

- 建议标题：`CachedShadows 工作原理 01｜它替代了 URP 主光阴影链路里的哪一段`
- 建议文件名：`content/engine-notes/cachedshadows-01-overview.md`
- 唯一职责：建立总认知，回答“它到底是什么”
- 必答问题：
  - CachedShadows 不是另一个光照系统，那它到底替代了什么
  - 为什么 URP 原生主光阴影关掉以后仍然可能有影子
  - 它和普通 URP 实时阴影最根本的区别是什么
- 建议章节：
  - 为什么低配 Android 需要它
  - 把 URP 原生主光阴影链路先缩成最小模型
  - CachedShadows 插进来的位置在哪
  - 它没有替代什么，它只替代了什么
  - 为什么 receiver 侧看起来还像在用 URP 阴影
  - 先给出一张总流程图
- 推荐前置：
  - `unity-rendering-02b-shadow-map.md`
  - `unity-rendering-09-urp-architecture.md`
  - `urp-ext-01-renderer-feature.md`
- 项目证据锚点：
  - `Assets/Settings/AndroidPipelineAssetLow.asset`
  - `Assets/Settings/ForwardRendererAndroid.asset`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderFeature.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderPass.cs`

### 02｜工作原理 02

- 建议标题：`CachedShadows 工作原理 02｜一帧里到底发生了什么：静态缓存、动态叠加、手动刷新`
- 建议文件名：`content/engine-notes/cachedshadows-02-frame-flow.md`
- 唯一职责：把一帧内部的渲染步骤讲清
- 必答问题：
  - “缓存”缓存的到底是什么
  - 静态阴影和动态阴影是怎么拼起来的
  - `Manual / EverySecond / EveryFrame` 真正改变的是什么
  - 为什么空场景、无主光、无投影体时会退回空图
- 建议章节：
  - 先区分静态 caster、动态 caster、receiver
  - `Setup()` 先判断哪些前提
  - 缓存图什么时候复用，什么时候重画
  - `Hidden/CopyShadowMap` 在动态叠加里扮演什么角色
  - `PostShadowPassConfigureKeywords()` 为什么是这套方案成立的关键
  - 为什么 Editor 非 Play Mode 的观察很容易误导
- 推荐前置：
  - `urp-ext-01-renderer-feature.md`
  - `urp-ext-04-draw-renderers.md`
  - `urp-lighting-02-shadow.md`
- 项目证据锚点：
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderPass.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/Shaders/CopyShadowMap.shader`

### 03｜生效链路 01

- 建议标题：`CachedShadows 生效链路 01｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的`
- 建议文件名：`content/engine-notes/cachedshadows-03-activation-chain.md`
- 唯一职责：回答“项目里为什么这套东西真的会跑起来”
- 必答问题：
  - 当前到底是哪一档 Quality 在生效
  - 它绑定的是哪个 Pipeline Asset 和 Renderer
  - Renderer 上的 Feature 是怎么进到当前 Camera 的
  - 为什么 `featureReferences` 为空时仍然可能正常刷新
- 建议章节：
  - 先把生效链路压成一句话
  - Quality Settings 如何决定当前 URP Asset
  - Pipeline Asset 如何决定默认 Renderer
  - RendererFeature 如何成为当前 Camera 的一部分
  - `CachedShadowsCameraManualUpdateFromMovement` 如何触发刷新
  - 为什么 Editor 里“看到了”不等于 Android 上“真的会有”
- 推荐前置：
  - `urp-config-01-pipeline-asset.md`
  - `urp-platform-02-quality.md`
  - `urp-config-03-camera-stack.md`
- 项目证据锚点：
  - `ProjectSettings/QualitySettings.asset`
  - `Assets/Settings/AndroidPipelineAssetLow.asset`
  - `Assets/Settings/ForwardRendererAndroid.asset`
  - `Assets/ArtTools/CommonArtTools/Camera/DP/GameMainCamera.prefab`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/Components/CachedShadowsCameraManualUpdateFromMovement.cs`

### 04｜Shader 链路 01

- 建议标题：`CachedShadows Shader 链路 01｜为什么编辑器有阴影、打包后可能没了：SVC、Hidden Shader、StaticShaders 各管什么`
- 建议文件名：`content/engine-notes/cachedshadows-04-shader-delivery.md`
- 唯一职责：把 Shader 本体、Variant、Hidden Shader、预加载链路的边界拆开
- 必答问题：
  - `_MAIN_LIGHT_SHADOWS` 系列变体由谁保
  - `Hidden/CopyShadowMap` 为什么不是靠 SVC 兜底
  - `StaticShaders.prefab` 为什么在这个项目里关键
  - 为什么“代码没问题”仍然可能在真机失效
- 建议章节：
  - 先把“Shader 存在”和“Variant 不被裁”分开
  - `BundleSVC.shadervariants` 负责什么，不负责什么
  - `StaticShaders.prefab` 在这条链路里的职责
  - `ProcedurePreload` 为什么是运行时前提
  - AssetBundle / Player Build / Hidden Shader 三条边界最容易怎么混
  - 最后收成一张职责表
- 推荐前置：
  - `unity-svc-always-included-stripping-when-to-use-which.md`
  - `unity-shader-variant-full-lifecycle-overview.md`
  - `problem-solving/urp-shader-prefiltering-assetbundle.md`
- 项目证据锚点：
  - `Assets/ArtWork/Generate/Shared/BundleSVC.shadervariants`
  - `Assets/ArtWork/Generate/Shared/StaticShaders.prefab`
  - `Assets/GameScripts/AOT/Procedure/ProcedurePreload.cs`
  - `Assets/GameScripts/AOT/Res/StaticShaders.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/Shaders/CopyShadowMap.shader`

### 05｜排查 01

- 建议标题：`CachedShadows 排查 01｜症状总表：没影子、不刷新、只有 Editor 有、Android 没有时先查什么`
- 建议文件名：`content/engine-notes/cachedshadows-05-troubleshooting-symptoms.md`
- 唯一职责：做成新人可直接照着走的速查手册
- 必答问题：
  - 完全没影子先看哪一层
  - Editor 有、Android 没有先看哪一层
  - 静态有、动态没有或动态有、静态没有各意味着什么
  - 相机动一下才有影子说明什么
- 建议章节：
  - 先给症状 -> 检查入口总表
  - 完全没影子：先查主光、caster、Quality、Feature
  - 只有 Editor 有：先查 Quality 档、PlayMode、打包链路
  - 有但不刷新：先查 `UpdateMode`、触发器、阈值
  - 只有静态或只有动态：先查 `DynamicShadow`、拷贝链路、静态重绘
  - 最后给最小排查顺序
- 推荐前置：
  - `unity-rendering-01d-frame-debugger.md`
  - `urp-platform-02-quality.md`
  - `cachedshadows-03-activation-chain.md`（写完后反链）
- 项目证据锚点：
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/Components/CachedShadowsCameraManualUpdateFromMovement.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderFeature.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderPass.cs`

### 06｜排查 02

- 建议标题：`CachedShadows 排查 02｜怎么证明当前阴影来自哪条链路：Frame Debugger、RenderDoc、日志各看什么`
- 建议文件名：`content/engine-notes/cachedshadows-06-validation-and-proof.md`
- 唯一职责：建立证据链，而不是让读者凭画面猜
- 必答问题：
  - 这一帧到底有没有执行 CachedShadows 的 Pass
  - 当前 shadow map 是谁生成的
  - receiver 侧有没有拿到正确的全局纹理和 keyword
  - 如何证明当前不是 URP 原生阴影在生效
- 建议章节：
  - 先给一张“证据链”图
  - Frame Debugger 里该看什么顺序
  - RenderDoc 里该抓哪些 RT 和 draw
  - 运行时日志最值得打哪几个点
  - 哪些现象能排除 URP 原生阴影
  - 如何把验证收成回归 checklist
- 推荐前置：
  - `unity-rendering-01d-frame-debugger.md`
  - `unity-rendering-01e-renderdoc-basics.md`
  - `unity-rendering-01f-renderdoc-advanced.md`
  - `urp-ext-05-renderdoc.md`
- 项目证据锚点：
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderPass.cs`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderFeature.cs`

### 07｜排查 03

- 建议标题：`CachedShadows 排查 03｜阴影画质问题怎么查：锯齿、漂浮、漏光、抖动，到底该调哪一层`
- 建议文件名：`content/engine-notes/cachedshadows-07-visual-quality-debug.md`
- 唯一职责：处理“有阴影但效果不对”的问题
- 必答问题：
  - 哪些问题属于 Shadow Map 原理层
  - 哪些问题属于 CachedShadows 刷新/范围/拼接层
  - 该先调 Bias、Distance、Cascade，还是先查缓存触发条件
- 建议章节：
  - 先把“有没有阴影”和“阴影好不好”分开
  - Shadow Acne / Peter Panning / 漏光 各属于哪一层
  - 抖动和突然跳变为什么常常和刷新边界有关
  - Camera Override / Shadow Distance / 分辨率对画质的影响
  - 动态叠加场景下常见的错位和断层
  - 最后给调参顺序
- 推荐前置：
  - `unity-rendering-02b-shadow-map.md`
  - `urp-lighting-02-shadow.md`
  - `urp-config-02-renderer-settings.md`
- 项目证据锚点：
  - `Assets/Settings/AndroidPipelineAssetLow.asset`
  - `Assets/Settings/ForwardRendererAndroid.asset`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/Components/CachedShadowsMainLightCameraOverride.cs`

### 08｜取舍 01

- 建议标题：`CachedShadows 取舍 01｜为什么低端 Android 选缓存阴影，而不是一直全量实时阴影`
- 建议文件名：`content/engine-notes/cachedshadows-08-tradeoffs-and-tiering.md`
- 唯一职责：把方案选择的工程原因讲清楚
- 必答问题：
  - 它真正省掉的是哪部分成本
  - 它牺牲了什么实时性和维护复杂度
  - 什么场景适合 `Manual`，什么场景不该用缓存阴影
  - 为什么它特别适合静态场景多、主光稳定的低配移动端
- 建议章节：
  - 先比较“原生实时阴影”和“缓存阴影”的成本结构
  - TopHeroUnity 为什么适合这条路
  - `Manual / EverySecond / EveryFrame` 在工程上怎么选
  - 动态阴影打开与关闭的代价差异
  - 为什么它不是银弹
  - 低配 Android 的降级策略应该怎么收口
- 推荐前置：
  - `urp-platform-02-quality.md`
  - `urp-platform-01-mobile.md`
  - `cachedshadows-02-frame-flow.md`（写完后反链）
- 项目证据锚点：
  - `Assets/Settings/AndroidPipelineAssetLow.asset`
  - `Assets/Settings/ForwardRendererAndroid.asset`
  - `Assets/ArtTools/CorgiCachedShadows/Scripts/RenderFeatures/CachedShadowsRenderFeature.cs`

## 推荐发布顺序

### 第一阶段：先建立系统认知

1. `cachedshadows-01-overview.md`
2. `cachedshadows-02-frame-flow.md`
3. `cachedshadows-03-activation-chain.md`
4. `cachedshadows-05-troubleshooting-symptoms.md`

这四篇先发，读者就已经能：

- 看懂系统的大体结构
- 跟着真实链路找到关键配置
- 知道效果不对时先查哪里

### 第二阶段：补运行时和验证

5. `cachedshadows-04-shader-delivery.md`
6. `cachedshadows-06-validation-and-proof.md`
7. `cachedshadows-07-visual-quality-debug.md`

这三篇发完，读者就能从“知道怎么排查”升级到“能证明问题在哪一层”。

### 第三阶段：补工程判断和总入口

8. `cachedshadows-08-tradeoffs-and-tiering.md`
9. `cachedshadows-series-index.md`

索引建议最后写，因为它应该反映专题的真实完成状态，而不是计划状态。

## 推荐的 front matter 约定

建议统一使用：

- `series: "CachedShadows 阴影缓存"`
- `series_id: "cachedshadows"`
- `series_role: "entry" / "index"`
- `series_order:` 按 `0, 10, 20, 30...` 预留插入空间
- `tags:` 至少带上 `Unity`、`URP`、`Shadow`、`Rendering`

### 文件命名建议

统一用：

- `cachedshadows-series-index.md`
- `cachedshadows-01-overview.md`
- `cachedshadows-02-frame-flow.md`
- `cachedshadows-03-activation-chain.md`
- `cachedshadows-04-shader-delivery.md`
- `cachedshadows-05-troubleshooting-symptoms.md`
- `cachedshadows-06-validation-and-proof.md`
- `cachedshadows-07-visual-quality-debug.md`
- `cachedshadows-08-tradeoffs-and-tiering.md`

原因：

- 和 `urp-config-01-*`、`urp-ext-01-*` 这些现有命名风格一致
- 看文件名就知道当前在专题里的位置
- 后续如果加 `09`、`10` 也不会破坏顺序

## 每篇建议的固定模板

为了减少写作风格飘散，建议每篇都尽量用同一套骨架：

1. 这篇在解决什么问题
2. 为什么这个问题在项目里容易被误判
3. 先把问题拆成几层
4. 放到 TopHeroUnity 里对应什么配置 / 代码 / 运行时现象
5. 如果效果不对，最先应该查哪里
6. 最后收成一句最短结论
7. 下一篇推荐阅读

## 建议配图

至少准备下面五类图，不然新人很难建立稳定心智模型：

1. `URP 原生主光阴影链路 vs CachedShadows 链路` 对照图
2. `静态缓存 + 动态叠加 + 手动刷新` 的一帧时序图
3. `Quality -> Pipeline Asset -> Renderer -> Feature -> Camera` 生效链路图
4. `症状 -> 第一个检查点` 的排查决策图
5. `Editor / PlayMode / Build / 真机` 的验证边界图

## 当前执行建议

如果现在只投入一轮写作资源，优先写下面四篇：

1. `cachedshadows-01-overview.md`
2. `cachedshadows-02-frame-flow.md`
3. `cachedshadows-03-activation-chain.md`
4. `cachedshadows-05-troubleshooting-symptoms.md`

原因不是这四篇最容易写，而是它们能最快把专题从“有知识点”变成“新人能开始用”。

## 后续扩展位

等第一期稳定后，再考虑补下面这些第二期内容：

- `CachedAdditionalShadows` 的附加光缓存专题
- 一个真实案例复盘：`Editor 有、Android 没有` 的逐步定位过程
- 一个运行时工具篇：在项目里加最小日志 / 面板怎么做

如果只看第一期，这些都不是必需品。
