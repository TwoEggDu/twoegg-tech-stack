---
title: "Unity 性能优化"
description: "沿瓶颈定位、内存、动画、物理、UGUI、加载和渲染七个模块，选择已有文章，识别仍需补齐的诊断与实验。"
url: "/reading/performance/unity/"
reading_kind: "engine"
topic_key: "performance"
performance_engine: "unity"
---

这条路线适合已有 Unity 开发经验、希望系统排查性能问题的读者。先读第一模块建立测量方法，再按当前问题进入其他模块；优化之后，回到[性能验证与回归]({{< relref "performance/rendering-tier-design-06-validation-loop.md" >}})检查结果。

七个模块参考 [UWA 的 Unity 游戏性能优化路线](https://community.uwa4d.com/objective)。这里重新组织本站已有文章；UWA 页面可能需要登录，具体版本、参数和操作方式以各篇说明及目标工程验证为准。

**阅读状态**：“已有阅读”表示已有相关正文；“待补内容”说明本路线尚未接齐的专篇、版本核对或实验，不表示该模块已经完整覆盖。

## 01 · 定位性能瓶颈

先固定设备、场景与帧率目标，再区分 CPU、GPU、内存与等待。耗时、内存及渲染预算需要对应具体设备与内容规模，不能只拿一个推荐值判断所有项目。

**已有阅读**

- [Unity 中性能问题怎样显形]({{< relref "performance/game-performance-unity-symptoms.md" >}})：从主线程、首次使用和资源行为认识常见现场。
- [Unity Profiler 真机连接]({{< relref "performance/mobile-tool-01-unity-profiler-device.md" >}})：建立真机采样入口。
- [Profiler CPU 深度分析]({{< relref "performance/cpu-opt-04-profiler-cpu-deep.md" >}})：沿时间线、调用栈与等待寻找耗时归属。
- [性能诊断工具选择指南]({{< relref "performance/mobile-tool-09-performance-diagnosis-tool-selection.md" >}})：按问题选择工具；GPU 可继续看 [Mali 分析与离线 Shader 编译]({{< relref "performance/mobile-tool-03-mali-debugger.md" >}})、[Xcode GPU Frame Capture]({{< relref "performance/mobile-tool-05-xcode-gpu-capture.md" >}})。
- [性能预算管理]({{< relref "performance/game-budget-management-series-index.md" >}})：把帧时间、内存和画面成本接到目标设备与内容约束。

> **待补内容**
>
> - 同一场景从 Profiler 到 Frame Debugger、GPU 捕获的对照实验：说明各工具观察的是哪一段成本。
> - UWA GOT Online 报告与本地采样的对应读法；目前尚未形成这条工具路线。

## 02 · 内存占用

从内存归属、对象持有与加载策略开始，分别检查资源重复、运行时创建、缓存常驻和托管分配；再进入纹理、网格、Shader、RenderTexture、动画、音频、字体与粒子资源。不同工具的统计分类需要先对齐。

**已有阅读**

- [内存专题索引]({{< relref "performance/memory-topic-index.md" >}})：区分工作集、资源内存、运行时与系统限制。
- [Unity Memory Profiler]({{< relref "performance/mobile-tool-08-memory-profiler.md" >}})：从快照增量和引用追踪进入对象持有，含 RenderTexture 排查。
- [GC 压力与分配]({{< relref "performance/cpu-opt-01-gc-pressure.md" >}})：理解托管分配、回收和可减少的临时对象。
- [加载时的内存控制]({{< relref "engine-toolchain/unity-loading-memory-control-cache-pool-refcount-unload.md" >}})：检查缓存、引用计数、卸载和场景重叠峰值。
- [AssetBundle 粒度与资源冗余]({{< relref "engine-toolchain/unity-assetbundle-pack-granularity-coarse-vs-fine-dependencies-redundancy.md" >}})：把构建期的依赖重复与运行时持有分开。

> **待补内容**
>
> - 按资源类型建立统一的内存核算路线：动画、音频、字体、粒子等目前缺少本路线可直接串读的诊断专篇。
> - 运行时创建纹理、材质、网格后的持有和释放实验，以及同一资源重复加载的快照证据。
> - UWA 分类与目标 Unity / Memory Profiler 版本统计项的对应说明，避免直接套用旧分类与预算。

## 03 · 动画模块

把动画状态更新、姿态求值、骨骼变换和蒙皮分开观察，再判断活跃 Animator 数量、可见性、Root Motion、对象激活及初始化分别影响哪一段。

**已有阅读 · 目前以原理铺垫为主**

- [骨骼蒙皮与 Blend Shape]({{< relref "rendering/unity-rendering-03-skeletal-animation.md" >}})：认识骨骼、蒙皮、CPU / GPU 分工及其成本。
- [Update 调度与更新频率]({{< relref "performance/cpu-opt-03-update-scheduling.md" >}})：作为更新工作如何分布的通用背景，不能替代 Animator 专项诊断。

> **待补内容**
>
> - Mecanim 的采样链与常见 Marker：活跃 Animator、Culling Mode、Optimize Game Objects、Root Motion 与初始化尖峰。
> - 不同版本、平台上的蒙皮路径对比实验，说明测量条件与取舍。
> - Legacy Animation 的采样与 `Animation.Sample` 排查；现有蒙皮文章不能覆盖两种动画系统的运行时诊断。

## 04 · 物理模块

先看一帧做了多少次物理更新，再看每次模拟中的碰撞体、接触、回调和查询成本。降低 CPU 耗时与减少托管分配是两项需要分别测量的目标。

**已有阅读**

- [Unity 物理系统移动端优化]({{< relref "performance/cpu-opt-06-physics-optimization.md" >}})：已有固定时间步、碰撞矩阵、碰撞体、休眠、Contacts 与 Profiler 的讨论。
- [GC 压力与分配]({{< relref "performance/cpu-opt-01-gc-pressure.md" >}})：含 NonAlloc 物理查询示例，可作为分配控制的补充阅读。

> **待补内容**
>
> - 手动模拟与 Transform 同步的职责边界，以及不同版本下的实际采样。
> - RaycastCommand 批量查询的调度、完成等待与结果处理实验。
> - Trigger、碰撞回调与自定义几何检测的对比；补上 NonAlloc 查询缓冲区容量和结果截断的检查。

## 05 · UGUI

将 UI 属性更新、布局与网格重建、Canvas 合批、输入射线和透明叠层分开。CPU 的重建成本与 GPU 的像素成本需要用各自的证据判断。

**已有阅读**

- [UI 渲染：Canvas、Rebuild、Overdraw 与 Atlas]({{< relref "rendering/unity-rendering-supp-d-ui-rendering.md" >}})：已有 Canvas 合批、图集、更新重建与动静拆分主线。
- [UI 首开、切页和叠层问题]({{< relref "performance/game-performance-ui-first-open-layering.md" >}})：从图集、层级、动效和模糊背景理解交互时刻的成本。

> **待补内容**
>
> - 将 SendWillRenderCanvases、BuildBatch、EmitWorldScreenspaceCameraGeometry、SyncTransform 的采样对应到可复现的小场景。
> - EventSystem 与 GraphicRaycaster 的输入查询诊断专篇。
> - 图集、重叠顺序、Z 坐标与合批中断的 Frame Debugger 对照实验；现有说明尚未组成逐项验证路线。

## 06 · 加载模块

把取得文件、展开依赖、恢复资产、实例化、激活和首次渲染接成一条链。再分别检查 Shader 首次准备、异步加载优先级和卸载回收发生的时机。

**已有阅读**

- [AssetBundle 运行时加载链]({{< relref "engine-toolchain/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md" >}})：区分文件、缓存、依赖、资产和实例，含卸载边界。
- [场景加载优化与分帧实例化]({{< relref "engine-toolchain/unity-scene-loading-optimization-loading-screen-frame-slicing-instantiate.md" >}})：已有 Loading Screen、实例化调度、Shader 预热与首帧可见的讨论。
- [加载时的内存控制]({{< relref "engine-toolchain/unity-loading-memory-control-cache-pool-refcount-unload.md" >}})：连接缓存池、持有和卸载时机。
- [Shader 变体生命周期]({{< relref "rendering/unity-shader-variant-full-lifecycle-overview.md" >}})：把构建、加载、运行时命中和首次使用成本放回完整链路。
- [启动与加载阅读路径]({{< relref "reading/startup-loading.md" >}})：需要跨模块梳理时，继续沿可用边界、依赖和生命周期阅读。

> **待补内容**
>
> - Loading.UpdatePreloading 与主线程收尾的版本化采样说明，以及异步优先级的对照实验。
> - Resources.UnloadUnusedAssets 的耗时、持有边界与调用时机专篇。
> - 对象池、Instantiate / Destroy、SetActive 与组件启停的同条件对比，分开记录加载时延和关键帧尖峰。

## 07 · 渲染模块压力定位

先判断是 CPU 的可见性处理与提交，还是 GPU 的几何、像素、带宽和各个 Pass 成本，再选择合批、剔除、内容裁剪或画质调整。

**已有阅读**

- [Draw Call 与合批]({{< relref "rendering/unity-rendering-01b-draw-call-and-batching.md" >}})：认识静态 / 动态合批、Instancing 和 SRP Batcher；深入可看 [GPU Instancing 与 SRP Batcher]({{< relref "rendering/unity-rendering-01b2-gpu-instancing-srp-batcher.md" >}})。
- [LOD 与 Culling]({{< relref "rendering/unity-rendering-supp-c-lod-culling.md" >}})：按可见性与几何细节进入渲染前置成本。
- [Draw Call 与 Overdraw]({{< relref "performance/gpu-opt-01-drawcall-overdraw.md" >}})：将 CPU 提交与透明像素开销分开。
- [Shader 优化]({{< relref "performance/gpu-opt-03-shader.md" >}})、[移动端后处理]({{< relref "performance/gpu-opt-05-postprocess-mobile.md" >}})：继续定位 Shader 和全屏效果成本。
- [GPU Counter 读法]({{< relref "performance/mobile-tool-06-read-gpu-counter.md" >}})：按工具与硬件解释统计量。

> **待补内容**
>
> - 同一场景分别调整可见对象、面数、分辨率、透明叠层和 Shader 的归因实验。
> - CullingGroup 的使用边界与开销，以及 CPU / GPU 剔除路径的版本核对。
> - 将合批、Overdraw 和 Shader 离线分析结果对应到真机帧时间；已有机制文章不等同于这组实验已完成。
