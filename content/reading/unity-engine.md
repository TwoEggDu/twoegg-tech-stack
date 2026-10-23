---
title: "Unity"
description: "从资产导入与对象恢复，到脚本执行、渲染、资源加载和构建工具，理解 Unity 各条系统链路怎样协作，以及它们对项目开发的约束。"
url: "/reading/engine/unity/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "unity"
---

按正在研究的系统或遇到的问题选择专题。各篇涉及的 Unity、Package 与工具版本，以正文说明为准。

## 资产系统与序列化

理解源文件怎样经过导入成为资产，引用关系怎样保存，以及 Scene、Prefab 和序列化数据怎样恢复为运行时对象。

**[进入资产系统与序列化专题]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})**

- [GUID、fileID 与 PPtr 在引用什么]({{< relref "engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md" >}})：理解资产、子对象与跨文件引用的身份关系。
- [Importer 做了什么]({{< relref "engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md" >}})：理解源文件、导入设置与导入产物之间的关系。
- [序列化资产怎样恢复为运行时对象]({{< relref "engine-toolchain/unity-serialized-assets-how-they-restore-to-runtime-objects.md" >}})：连接序列化数据、原生对象与托管绑定。

## 资源构建、加载与管理

沿资产到构建产物，再到下载、缓存、依赖满足、对象创建和释放，理解资源系统每一层承担的职责。

### 构建与运行时链路

- [资源构建管线分层]({{< relref "engine-toolchain/unity-buildpipeline-sbp-addressables-build-script-layering.md" >}})：分清 BuildPipeline、SBP 与框架构建脚本。
- [AssetBundle 打包粒度与依赖]({{< relref "engine-toolchain/unity-assetbundle-pack-granularity-coarse-vs-fine-dependencies-redundancy.md" >}})：理解粗细粒度、共享依赖、资源冗余与加载边界。
- [AssetBundle 运行时加载链]({{< relref "engine-toolchain/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md" >}})：区分下载、缓存、依赖、LoadAsset、实例化与卸载。
- [场景加载与分帧实例化]({{< relref "engine-toolchain/unity-scene-loading-optimization-loading-screen-frame-slicing-instantiate.md" >}})：连接资源准备、对象创建、激活与首帧可见。
- [加载时的内存控制]({{< relref "engine-toolchain/unity-loading-memory-control-cache-pool-refcount-unload.md" >}})：检查缓存、引用持有、卸载时机与场景重叠峰值。
- [AssetBundle 产物分析]({{< relref "engine-toolchain/unity-assetbundle-post-build-binary-analysis.md" >}})：从实际文件反查内容、依赖线索与交付问题。

### 资源管理框架

- [Addressables]({{< relref "engine-toolchain/unity-addressables-series-index.md" >}})：沿加载、Catalog、引用计数和资源构建，理解框架的管理机制。
- [YooAsset]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})：沿架构、文件系统、清单、下载缓存、生命周期与构建深入实现。
- [Addressables 与 YooAsset 对比及选型]({{< relref "engine-toolchain/unity-addressables-yooasset-comparison-series-index.md" >}})：按运行时调度、构建产物和交付治理比较两套系统。

## 脚本编译与执行运行时

理解 C# 从源文件到执行的过程，以及异步调度、热更新、泛型和裁剪怎样受到编译链与运行时的共同约束。

- [脚本编译管线]({{< relref "engine-toolchain/unity-script-compilation-pipeline-series-index.md" >}})：追踪 asmdef、编译、ILPP、域重载与 Player 构建之间的联系。
- [Task／UniTask 与 Unity 异步运行时]({{< relref "engine-toolchain/unity-async-runtime-series-index.md" >}})：理解线程、执行阶段、调度、取消、生命周期与异常处理。
- [Mono]({{< relref "engine-toolchain/mono-series-index.md" >}})：研究类型加载、解释执行、JIT、AOT 与 GC。
- [IL2CPP]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})：连接 IL 转换、生成代码、元数据与 libil2cpp 运行时。
- [HybridCLR]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：理解解释执行与 IL2CPP 的协作，以及 AOT 泛型、补充元数据和程序集边界。
- [Unity 裁剪]({{< relref "engine-toolchain/unity-stripping-series-index.md" >}})：区分托管代码与引擎代码裁剪，理解动态依赖、反射和保留策略。

## 渲染系统与 Shader

从 Mesh、Material、Texture、空间与光照出发，理解画面怎样经过渲染管线生成，以及管线扩展和 Shader 工程怎样组织。

- [Unity 渲染系统]({{< relref "rendering/unity-rendering-series-index.md" >}})：沿基础资产、Draw Call、光照和调试，进入 Built-in、SRP 与 URP。
- [URP 深度]({{< relref "rendering/urp-deep-dive-series-index.md" >}})：按前置基础、管线配置、光照阴影和扩展开发深入阅读。
- [Shader 手写技法]({{< relref "rendering/shader-handwriting-series-index.md" >}})：从基础语法和第一个 Shader，进入光照、效果与项目应用。
- [Shader Variant 治理]({{< relref "rendering/unity-shader-variants-series-index.md" >}})：连接变体来源、收集、保留、剔除、交付与运行时使用。
- [CachedShadows 阴影缓存]({{< relref "rendering/cachedshadows-series-index.md" >}})：通过一个具体的 URP 扩展，理解接管链路、资源交付、效果验证与问题排查。

## DOTS 与数据导向开发

理解 Entity、Component 与 System 怎样组织数据和行为，以及 Baking、Jobs、Burst、渲染接入和传统对象体系怎样协作。

**[进入 Unity DOTS 工程实践]({{< relref "system-design/dots-engineering-index.md" >}})**

从 ECS 核心概念与第一个程序开始，再按问题进入查询、组件存储、Baking、SubScene、Jobs、Burst、NativeCollection 和混合架构边界。

> **后续专题 · 草稿，暂不开放阅读入口**
>
> 以下内容已有本地草稿，尚未列入正式阅读主线：
>
> - **DOTS Physics（草稿）**：物理世界、查询、事件、Baking、角色控制与调试。
> - **DOTS NetCode（草稿）**：Client／Server World、Ghost、快照、预测、回滚与排障。
> - **DOTS 项目落地与迁移（草稿）**：选型、混合边界、分阶段迁移、收益验证与工程接入。

## 构建配置、平台与工具开发

理解构建参数怎样影响代码、运行时和平台产物，再看项目工具如何组织这些能力。

- [Player Settings 总览]({{< relref "engine-toolchain/unity-player-settings-build-runtime-overview.md" >}})：分清脚本后端、API 兼容级别、IL2CPP 代码生成、C++ 编译配置和增量 GC。
- [Android AAB／PAD]({{< relref "engine-toolchain/unity-android-aab-pad-series-index.md" >}})：理解 base module、asset packs、Unity 打包设置与包体超限排查。
- [构建系统的设计模式选择]({{< relref "engine-toolchain/build-system-pattern-reading-guide.md" >}})：从两个项目共享构建系统的需求出发，讨论复用、扩展与配置边界。

## 性能诊断与调试

遇到耗时、卡顿、内存异常或崩溃时，先确定观察对象，再选择工具并追踪成本与故障的来源。

- [Unity 性能优化]({{< relref "reading/unity-performance.md" >}})：按瓶颈、内存、动画、物理、UGUI、加载和渲染查找文章；各模块同时标明待补内容。
- [RenderDoc 阅读入口]({{< relref "rendering/renderdoc-reading-entry.md" >}})：从捕获与基础读图，进入 Shader 调试和 URP 实战。
- [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})：理解符号、断点及 Unity 调试开关。
- [崩溃分析]({{< relref "engine-toolchain/crash-analysis-series-index.md" >}})：连接平台异常、符号化与 Unity／IL2CPP 运行时排障。

## Unity 6 与版本专题

集中查找版本变化与升级决策。阅读时请核对各篇注明的 Unity、URP 和 Package 版本。

- [Unity 6 升级决策]({{< relref "engine-toolchain/unity6-upgrade-01-cost-benefit.md" >}})：结合项目阶段、收益、迁移成本与维护风险判断升级时机。
- [Awaitable、UniTask 与 Coroutine]({{< relref "engine-toolchain/unity6-runtime-01-awaitable-vs-unitask-vs-coroutine.md" >}})：比较异步方案的执行模型、能力范围与使用边界。
- [GPU Resident Drawer]({{< relref "rendering/unity6-rendering-01-gpu-resident-drawer.md" >}})：理解渲染数据组织和 CPU 到 GPU 提交链路的变化。

### Unity 6.6 Content Directories

- [01｜内容产物、引用模型与工作流]({{< relref "engine-toolchain/unity-content-directories-01-model-and-workflow.md" >}})：理解内容构建、引用、部署、加载与释放。
- [02｜与 AssetBundle 的对比]({{< relref "engine-toolchain/unity-content-directories-02-vs-assetbundle.md" >}})：比较依赖、内存、生命周期与迁移取舍。
- [03｜构建、加载与内存分析]({{< relref "engine-toolchain/unity-content-directories-03-why-faster.md" >}})：理解逐资产构建、缓存、稳定 ID 与加载流水线，以及性能判断的边界。

---

**继续阅读**

- [引擎研发]({{< relref "reading/engine.md" >}})：查找引擎架构、图形、语言运行时与计算基础等公共专题。
- [启动与加载]({{< relref "reading/startup-loading.md" >}})：沿可用边界、依赖、对象准备与生命周期串联阅读。
- [工程与交付]({{< relref "reading/delivery.md" >}})：继续研究构建、分发、发布与协作。
- [Unity 游戏更新与发布（提纲）]({{< relref "delivery-engineering/unity-game-update-release-series-index.md" >}})：梳理版本绑定、兼容、构建与回退；**七篇章节当前均为提纲，正文待补齐。**
