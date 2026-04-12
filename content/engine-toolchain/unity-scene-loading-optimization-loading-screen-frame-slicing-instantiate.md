---
date: "2026-04-12"
title: "场景加载优化：Loading Screen 设计、帧时间分摊、分帧实例化"
description: "把场景加载从'等进度条走完'拆成资源准备、对象实例化和首帧可见三个阶段，讲清 Loading Screen 真正该遮住什么、分帧实例化怎么控制帧时间、以及从加载完成到用户看到画面之间还有多少工程问题。"
slug: "unity-scene-loading-optimization-loading-screen-frame-slicing-instantiate"
weight: 72
featured: false
tags:
  - "Unity"
  - "Loading"
  - "Scene"
  - "Instantiate"
  - "Performance"
  - "Loading Screen"
series: "Unity 资产系统与序列化"
---
前面三篇把运行时加载链从异步管线、优先级调度拆到了内存控制。走到这一步，资源怎么异步加载、先加载谁、内存怎么管都有了框架。但实际项目中，用户感知最强的加载场景是一个非常具体的时刻：

`场景切换。`

用户从一个场景进入另一个场景，中间看到 Loading Screen，等它走完，新场景出现。这个过程如果做得不好，体验会非常差——要么等太久，要么进去以后卡一下，要么画面闪烁。

这篇要讲的就是这段路的工程问题。

## 先给一句总判断

`场景加载优化的核心不是"怎么让加载更快"，而是"怎么让用户从看到 Loading 到看到完整画面之间，不感知到任何卡顿和异常"。这需要管理三个阶段：资源准备、对象实例化、首帧可见。Loading Screen 不是在等加载完成，而是在遮住这三个阶段的过渡。`

## 一、场景切换到底经历了什么

一次典型的场景切换，从用户点"进入关卡"到看到新场景画面，中间至少会经过：

- 卸载旧场景：Destroy 所有对象、Unload bundle、释放内存
- 可选的 `Resources.UnloadUnusedAssets` 和 `GC.Collect`：清理孤儿资源和托管堆
- 加载新场景的 bundle 和依赖 bundle
- 解压和反序列化
- Scene 文件中的对象重建（GameObject 树、组件绑定）
- Instantiate 额外的动态对象（Prefab 实例化）
- Shader 首次编译（如果变体未预热）
- 贴图和 Mesh 上传 GPU
- Awake / OnEnable / Start 回调
- 首帧渲染

这里面任何一个环节如果耗时过长且没有被 Loading Screen 遮住，用户就会感知到卡顿。

## 二、Loading Screen 真正该遮住什么

### 1. Loading Screen 不是装饰，是工程遮罩

很多人把 Loading Screen 当成"等进度条走完"的视觉装饰。但从工程角度看，Loading Screen 的职责是：

`在所有可能导致帧率波动的操作完成之前，给用户一个稳定流畅的画面，避免看到半成品的场景状态。`

### 2. Loading Screen 应该在什么时候关闭

不是在 `AsyncOperation.progress == 1.0` 的时候。而是在以下条件全部满足时：

- 场景资源加载完成
- 关键 Prefab 实例化完成
- Shader 预热完成（ShaderVariantCollection.WarmUp 或首帧预渲染）
- GPU 资源上传完成（或者至少关键资源已上传）
- 首帧能正常渲染不卡顿

很多项目的做法是：`SceneManager.LoadSceneAsync` 加载完成后，不立即关闭 Loading Screen，而是额外等 1–2 帧，确保首帧渲染的全部工作完成后再关闭。

### 3. allowSceneActivation 的用法

`AsyncOperation.allowSceneActivation = false` 可以让场景加载到 90% 后暂停，不自动激活。这给你一个机会在激活前做额外的准备：

- 等待依赖资源加载完毕
- 执行 Shader 预热
- 预创建对象池
- 设置相机和光照

准备完毕后，设置 `allowSceneActivation = true` 激活场景，然后再关闭 Loading Screen。

需要注意的是，当 `allowSceneActivation` 设为 `false` 时，`AsyncOperation.progress` 会停在约 `0.9`（精确值是 `0.8999...f`），而不是 `1.0`。最后的 10% 代表的是场景激活阶段——调用所有场景对象的 `Awake()`、`OnEnable()` 以及运行初始化逻辑。这个 0.9 阈值是常见的困惑来源：开发者经常在 Loading Screen 逻辑中检查 `progress >= 1.0f`，结果发现永远不会触发。正确的做法是检查 `progress >= 0.9f` 来判断"资源已就绪，可以激活"。两阶段模式是：先让加载跑到 0.9（资源加载完毕，对象尚未激活），然后在 Loading Screen 过渡动画准备好后设置 `allowSceneActivation = true`，触发最终的激活阶段。

## 三、分帧实例化：为什么不能一帧内创建所有对象

### 1. 一帧内大量 Instantiate 是最常见的卡顿来源

场景加载完成后，业务逻辑通常需要创建大量动态对象：NPC、道具、特效、UI 元素。如果这些 `Instantiate` 调用集中在同一帧，主线程会被长时间占用，导致明显的掉帧。

一个 Prefab 的 `Instantiate` 要做的事情不少：

- 克隆对象图（所有 GameObject 和 Component）
- 执行 Awake 回调
- 如果是 Active 的，立即执行 OnEnable
- 如果当帧需要渲染，还要参与 Culling 和 DrawCall 收集

50 个有组件的 Prefab 在同一帧 Instantiate，主线程开销可能达到几十毫秒。

### 2. 分帧实例化的基本思路

核心思想是把 `Instantiate` 分散到多帧执行，每帧只创建一定数量的对象，保证帧时间不超过预算。

一个简单的实现：

- 维护一个待实例化的 Prefab 队列
- 每帧开始时检查帧时间预算（比如 16ms 目标帧时间，留 4ms 给实例化）
- 从队列中取出 Prefab，Instantiate，检查已用时间
- 如果已用时间接近预算，剩余的留到下一帧

### 3. 帧时间预算怎么定

- 目标帧率 60fps → 帧时间预算 16.67ms
- 渲染和逻辑通常已经占了 10–12ms
- 留给实例化的时间大约 2–4ms
- 每帧能 Instantiate 的对象数量取决于 Prefab 的复杂度

可以用 `Time.realtimeSinceStartup` 在每帧的实例化循环中检查已用时间，作为动态调节的依据。不需要精确到微秒，只要保证不超预算就行。

从 Unity 2022.3 LTS 开始，`Object.InstantiateAsync` 提供了内置的异步实例化能力，自带自动时间分片。它返回一个 `AsyncInstantiateOperation`，引擎会自动把实例化工作分散到多帧执行，不需要手动写协程控制。对于 2022.3 及以上版本的项目，这是比手动分帧更推荐的方案——引擎侧的工作分配比用户侧的 yield 逻辑能优化得更好。上面描述的手动协程方案对老版本 Unity 仍然有效。

### 4. 对象池和分帧实例化的关系

对象池（Object Pool）是分帧实例化的进阶方案：

- 在 Loading Screen 期间，预先 Instantiate 一批常用对象放入对象池
- 游戏运行时，从对象池取出已创建的对象（SetActive(true)），而不是 Instantiate 新的
- 不再使用时，放回对象池（SetActive(false)），而不是 Destroy

这样运行时几乎没有 Instantiate 开销，代价是对象池占用额外内存。

## 四、Shader 预热：首帧卡顿的隐形杀手

### 1. 为什么 Shader 会导致首帧卡顿

Unity 的 Shader 在首次渲染某个变体组合时，需要在 GPU 上编译该变体。这个编译过程发生在主线程，通常耗时几毫秒到几十毫秒。如果首帧有大量 Shader 变体首次可见，编译时间叠加后会导致明显的卡顿。

严格来说，运行时发生的并不是 Shader 编译（把着色器源码翻译成 GPU ISA 的过程在构建期已经完成）。运行时真正发生的是 **GPU 程序对象 / Pipeline State Object (PSO) 的创建**：在 Vulkan 上是创建 `VkPipeline`，Metal 上是创建 `MTLRenderPipelineState`，OpenGL ES 上则是 Shader Program 的链接。这个过程每个变体可能耗时 5–50ms。D3D11 上这个开销很小，因为 Shader 字节码可以直接使用；D3D12 上 PSO 创建同样可能造成卡顿；主机平台使用预编译 Shader，这个问题基本不存在。区分这一点对平台特定优化很重要：Shader 预热在移动端（Vulkan / Metal / GLES）是关键优化项，但在主机上可能完全不需要。

### 2. ShaderVariantCollection 预热

`ShaderVariantCollection` 可以在 Loading Screen 期间提前编译需要的 Shader 变体：

- 在编辑器中收集场景会用到的 Shader 变体组合
- 在场景激活前调用 `ShaderVariantCollection.WarmUp()`
- WarmUp 会触发这些变体的 GPU 编译，避免首帧编译卡顿

但 WarmUp 本身也有开销，变体太多时 WarmUp 可能需要几百毫秒甚至更长。所以也需要控制预热的变体数量，只预热首帧确定会用到的。

### 3. 预渲染方式

另一种预热方式是在 Loading Screen 期间，用一个不可见的相机渲染一帧包含关键材质的场景。这会触发所有相关 Shader 变体的编译，但不会被用户看到。渲染完成后，关闭预渲染相机，打开正式相机，首帧就不会再有编译卡顿。

## 五、从加载完成到首帧可见：最后一段路

即使资源全部加载完成、Shader 预热完毕、对象池准备就绪，从"Loading Screen 关闭"到"用户看到正常画面"之间仍然有一些需要处理的事情：

### 1. 首帧渲染抖动

Loading Screen 关闭后的第一帧，渲染管线会突然从渲染简单的 Loading UI 切换到渲染完整的 3D 场景。如果场景复杂度高，这一帧可能特别长。

一种缓解方式是：Loading Screen 关闭前先渲染一帧场景但不显示（用相机渲染到 RenderTexture），让渲染管线"热起来"，然后再关闭 Loading Screen。

### 2. 异步上传时间片

Unity 有 `QualitySettings.asyncUploadTimeSlice` 和 `asyncUploadBufferSize` 控制贴图和 Mesh 的异步 GPU 上传速度。默认值可能偏保守，导致大贴图需要好几帧才能上传完毕。在 Loading Screen 期间，可以临时调大这些值加速上传，Loading 结束后再改回来。

### 3. 音频和动画的初始化

加载完成后，BGM 播放、环境音启动、角色 Idle 动画开始播放——这些也会占用第一帧的时间。如果可能，在 Loading Screen 关闭前就开始播放音频和动画的第一帧。

## 六、最小检查表

- Loading Screen 是否在所有初始化完成后才关闭（而不是 progress == 1.0 就关）？
- 是否使用了 `allowSceneActivation = false` 来控制激活时机？
- 动态对象是否分帧实例化？每帧的实例化数量是否有时间预算控制？
- 常用对象是否有对象池？对象池是否在 Loading 期间预填充？
- Shader 是否预热？预热是在 Loading Screen 期间完成的吗？
- 场景切换前是否清理了旧场景的内存（过渡场景或显式 Unload + UnloadUnusedAssets）？
- `asyncUploadTimeSlice` 是否在 Loading 期间临时调大？
- 首帧渲染是否有预渲染缓解措施？

## 结语

场景加载优化看起来是"让加载更快"，实际上更多是"让加载过程中的每一段卡顿都被 Loading Screen 遮住"。资源准备、对象实例化、Shader 预热、GPU 上传——任何一段露出来，用户都会感知到卡顿。Loading Screen 的关闭时机不是进度条决定的，而是这些初始化全部完成后才决定的。

下一篇会进入流式加载领域：当场景大到不可能一次全部加载时，Level Streaming 是怎么让用户在不感知 Loading 的情况下无缝体验大世界的。
