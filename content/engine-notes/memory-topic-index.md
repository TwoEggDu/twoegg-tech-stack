---
date: "2026-03-29"
title: "内存专题索引｜先分清运行时内存分布，再看预算、RT、流送、工作集和系统强杀"
description: "给内存专题补一个稳定入口：先把运行时内存桶、预算线、工作集、RenderTexture、Shader Variant、资源生命周期，以及 Windows 的工作集/显存问题和移动端的 LMK / jetsam 看清，再按 Unity / 平台问题进入正文。"
slug: "memory-topic-index"
weight: 172
featured: false
tags:
  - "Memory"
  - "Unity"
  - "Mobile"
  - "Runtime"
  - "Index"
---
> 这页不是某一条单独系列的目录，而是一张“跨系列内存地图”。
> 因为项目里的内存问题，从来不会只站在一层：它会同时牵到 GC、对象池、纹理、RenderTexture、Shader Variant、AssetBundle 生命周期，以及 Android / iOS 的系统终止机制。

如果你这次只想沿着移动端这条线读，也可以先回 [移动端硬件与优化系列索引｜从 SoC、TBDR 和热约束，到 GPU / CPU / 工具链 / 平台碎片化]({{< relref "engine-notes/mobile-hardware-and-optimization-series-index.md" >}})。

如果你这次更关心 Windows / 桌面平台，最适合先看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})、[CPU 性能优化 05｜内存预算管理：按系统分配上限、Texture Streaming 与 OOM/LMK 防护]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}}) 和 [Unity 渲染系统 01c｜Render Target 与帧缓冲区：GPU 把渲染结果写到哪里]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}})。

## 这页主要在解决什么

如果把这个专题压成一句话，我会这样描述：

`内存问题真正难的，不是“背几个优化技巧”，而是先分清谁在常驻、谁在做峰值、谁在系统压力下会把进程送进 LMK / jetsam 区间。`

所以这页索引最适合回答的是：

- 运行时内存到底分成哪些桶
- Texture 和 RenderTexture 为什么要分开算
- GC、对象池和“常驻对象”之间到底是什么关系
- AssetBundle / 下载缓存 / 解压 / Unload 为什么总会把内存峰值抬高
- Shader Variant 为什么不一定最大，却仍然会把启动、驱动和 Native 压力一起抬高
- Android 的 LMK、iOS 的 jetsam 为什么经常表现成“直接闪退回桌面”
- Windows 上为什么更常先表现成工作集抖动、分页、显存驻留抖动或 TDR，而不是像手机那样先被系统强杀

## 最短阅读路径

如果你第一次系统读，我建议先按这条路径走：

1. [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})
   先把“移动端更怕系统强杀，桌面更怕工作集、分页和显存驻留慢慢把你拖垮”这条平台差异立住。
2. [CPU 性能优化 05｜内存预算管理：按系统分配上限、Texture Streaming 与 OOM/LMK 防护]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})
   再把 Unity / 移动端里的运行时内存桶、Texture / RT 分桶、LMK / jetsam 区别收进真实项目语境。
3. [CPU 性能优化 01｜C# GC 压力：堆分配来源、零分配写法与对象池]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}})
   再把托管堆、GC 抖动和代码侧分配来源看清。
4. [游戏编程设计模式 04｜Object Pool：对象池化原理与实践]({{< relref "engine-notes/pattern-04-object-pool.md" >}})
   然后把“对象池省的是 GC，不是白送内存”这件事补稳。
5. [Unity 渲染系统 01c｜Render Target 与帧缓冲区：GPU 把渲染结果写到哪里]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}}) 和 [URP 深度前置 02｜RenderTexture 与 RTHandle：临时 RT、RTHandle 体系]({{< relref "engine-notes/urp-pre-02-rthandle.md" >}})
   接着把 RT、深度缓冲、临时 RT 和 RT 生命周期收回来，不然后面很难解释为什么 HDR / Bloom / Shadow 一开就多几十 MB。
6. [AssetBundle 的性能与内存代价：LZMA/LZ4、首次加载卡顿、内存峰值、解压与 I/O]({{< relref "engine-notes/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md" >}}) 和 [AssetBundle 运行时加载链：下载、缓存、依赖、反序列化、Instantiate、Unload 怎么接起来]({{< relref "engine-notes/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md" >}})
   最后把“为什么切场景和热更时总会突然爆内存峰值”这条链补齐。

## Windows / 桌面平台需要关心什么

Windows 当然也要关心内存，只是**关心的重点和移动端不一样**。

移动端最该警惕的是：系统在高压下直接把前台进程送进 `LMK / jetsam`。  
Windows 更常见的现实则是：**系统不一定先杀你，但会先让你变慢、抖、换页、显存回退，最后甚至走到 GPU 超时或分配失败。**

在 Windows / 桌面平台，最值得单独盯住的通常是这几条线：

- **Working Set（工作集）**：
  当前真正驻留在物理内存里的那部分页。它抖得厉害时，往往意味着资源进进出出过于频繁，或者切场景、首开 UI、热更恢复把热页集冲散了。
- **Commit（提交量）**：
  不是“现在实际驻留了多少”，而是系统承诺要为这些虚拟内存分配提供后备空间。大块分配、资源双驻留、解压缓冲和插件常驻，经常会先把这条线顶高。
- **Page Fault / 分页**：
  这通常是 Windows 上最容易先把体验拖坏的东西。它更常表现成切场景突然巨卡、磁盘活动升高、帧时间抖得很厉害，而不是立刻回桌面。
- **VRAM / Shared GPU Memory / Driver Residency**：
  桌面项目的 Texture、RT、Shadow Map、G-Buffer、驱动程序对象不只是“GPU 自己的事”。当显存预算不稳时，Windows 常见的表现是资源驻留抖动、复制增多、共享内存回退和帧时间突然上升。
- **TDR（Timeout Detection and Recovery）风险**：
  这不等于“纯粹的内存问题”，但过重的 RT 链、显存驻留抖动、异常长的 GPU 工作负载，会把问题一路放大到 `DXGI_ERROR_DEVICE_REMOVED` 这类桌面 GPU 故障形态。

如果只用一句话概括 Windows 的内存直觉，我会这样说：

`移动端更怕系统直接杀你，Windows 更怕系统先不杀你，但通过工作集、分页和显存驻留把你慢慢拖垮。`

所以在 Windows 上，最该问的通常不是“为什么没收到 lowMemory”，而是：

- 工作集是不是在关键时刻抖得太厉害
- Commit 是不是被双驻留、解压和缓存顶高了
- VRAM 和 Shared GPU Memory 是不是已经开始互相挤压
- 切场景、首进、回前台和热更这些峰值时刻，是否在把 CPU 内存峰值和 GPU 驻留峰值叠在一起结账

## 按你现在遇到的问题进入

- 不知道现在到底是谁在吃内存：
  先看 [CPU 性能优化 05｜内存预算管理]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})。这篇现在已经把 `代码 / Runtime / Texture / RT / Mesh / Shader Variant / Objects / Cache` 分桶拆开了。
- 你看到的是 GC 抖动，不是系统强杀：
  先看 [CPU 性能优化 01｜C# GC 压力]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}})，再看 [Object Pool：对象池化原理与实践]({{< relref "engine-notes/pattern-04-object-pool.md" >}})。
- 一开 HDR、Bloom、阴影、多相机，内存和帧时间一起上去：
  先看 [Render Target 与帧缓冲区]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}})、[RenderTexture 与 RTHandle]({{< relref "engine-notes/urp-pre-02-rthandle.md" >}})、[GPU 渲染优化 05｜后处理在移动端的取舍与降质策略]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})、[GPU 优化 04｜移动端阴影：Shadow Map 代价、CSM 配置与软阴影替代方案]({{< relref "engine-notes/gpu-opt-04-shadow-mobile.md" >}})。
- Windows 上不是直接闪退，而是切场景巨卡、磁盘突然很忙、越玩越抖：
  先看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})、[AssetBundle 的性能与内存代价]({{< relref "engine-notes/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md" >}})、[读盘完成，为什么还是不等于资源可用]({{< relref "engine-notes/game-performance-read-does-not-mean-ready.md" >}})。
- Windows 上更像显存 / 驱动驻留问题，而不是托管堆问题：
  先看 [Render Target 与帧缓冲区]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}})、[RenderTexture 与 RTHandle]({{< relref "engine-notes/urp-pre-02-rthandle.md" >}})、[手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})。
- 切场景、热更、下载解压、首进场景时特别容易出峰值：
  先看 [AssetBundle 的性能与内存代价]({{< relref "engine-notes/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md" >}})、[AssetBundle 运行时加载链]({{< relref "engine-notes/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md" >}})、[读盘完成，为什么还是不等于资源可用]({{< relref "engine-notes/game-performance-read-does-not-mean-ready.md" >}})。
- 线上表现是“闪退回桌面”，Crash SDK 还不一定有栈：
  先看 [CPU 性能优化 05｜内存预算管理]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})、[Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化]({{< relref "engine-notes/mobile-unity-android.md" >}})、[Unity on Mobile｜iOS 专项：Metal 渲染行为、内存警告机制与 Instruments 联用]({{< relref "engine-notes/mobile-unity-ios.md" >}})。如果你已经在做线上排障，也可以回 [CrashAnalysis 系列索引｜先立概念地图，再按平台和 Unity + IL2CPP 回查]({{< relref "engine-notes/crash-analysis-series-index.md" >}})。
- 你怀疑不是 Texture，而是 Shader Variant / WarmUp / 驱动程序对象在抬高内存和启动成本：
  先看 [Unity Shader Variant 是什么：GPU 程序的编译模型]({{< relref "engine-notes/unity-shader-variant-what-is-a-variant-gpu-compilation-model.md" >}})、[Unity Shader Variant 全流程总览：从生产、保留、剔除到运行时使用]({{< relref "engine-notes/unity-shader-variant-full-lifecycle-overview.md" >}})、[Unity Shader Variant 从资产到 GPU 消费：资源定义、构建产物和运行时命中链路]({{< relref "engine-notes/unity-shader-variant-runtime-hit-mechanism.md" >}}) 和 [Unity Shader Variant 实操：怎么知道项目用了哪些、运行时缺了哪些、以及怎么剔除不需要的]({{< relref "engine-notes/unity-shader-variants-how-to-find-missing-and-strip.md" >}})。
- 你需要 Unreal 对照视角：
  直接看 [Unreal 性能 04｜内存与流送：资产预算、Texture Streaming、PSO Cache 与 GC 调优]({{< relref "engine-notes/ue-perf-04-memory-streaming.md" >}})。

## 按“内存桶”进入

- Managed Heap / GC / 脚本实例：
  看 [CPU 性能优化 01｜C# GC 压力]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}}) 和 [Object Pool：对象池化原理与实践]({{< relref "engine-notes/pattern-04-object-pool.md" >}})。
- Texture / Lightmap / Cubemap / RenderTexture：
  看 [CPU 性能优化 05｜内存预算管理]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})、[Render Target 与帧缓冲区]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}})、[RenderTexture 与 RTHandle]({{< relref "engine-notes/urp-pre-02-rthandle.md" >}})。
- Bundle / Cache / Temp / 解压缓冲：
  看 [AssetBundle 的性能与内存代价]({{< relref "engine-notes/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md" >}})、[AssetBundle 运行时加载链]({{< relref "engine-notes/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md" >}})。
- Shader / Variant / Driver Program：
  看 [Unity Shader Variant 是什么：GPU 程序的编译模型]({{< relref "engine-notes/unity-shader-variant-what-is-a-variant-gpu-compilation-model.md" >}})、[Unity Shader Variant 全流程总览：从生产、保留、剔除到运行时使用]({{< relref "engine-notes/unity-shader-variant-full-lifecycle-overview.md" >}})、[Unity Shader Variant 从资产到 GPU 消费：资源定义、构建产物和运行时命中链路]({{< relref "engine-notes/unity-shader-variant-runtime-hit-mechanism.md" >}}) 和 [Unity Shader Variant 实操：怎么知道项目用了哪些、运行时缺了哪些、以及怎么剔除不需要的]({{< relref "engine-notes/unity-shader-variants-how-to-find-missing-and-strip.md" >}})。
- 系统终止机制：
  看 [Unity on Mobile｜Android 专项]({{< relref "engine-notes/mobile-unity-android.md" >}})、[Unity on Mobile｜iOS 专项]({{< relref "engine-notes/mobile-unity-ios.md" >}}) 和 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})。
- Working Set / Commit / VRAM / Driver Residency：
  看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})、[Render Target 与帧缓冲区]({{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}})、[RenderTexture 与 RTHandle]({{< relref "engine-notes/urp-pre-02-rthandle.md" >}})。

## 交叉阅读

如果你读完这页，还想把内存问题放回更大的系统上下文，最适合接着看的通常是：

- [移动端硬件与优化系列索引｜从 SoC、TBDR 和热约束，到 GPU / CPU / 工具链 / 平台碎片化]({{< relref "engine-notes/mobile-hardware-and-optimization-series-index.md" >}})
- [游戏性能判断系列索引｜先看判断框架，再看症状、证据和排查工作流]({{< relref "engine-notes/game-performance-judgment-series-index.md" >}})
- [Unity 资产系统与序列化系列索引：从资产通识到 Scene、Prefab、Shader 与 AssetBundle]({{< relref "engine-notes/unity-asset-system-and-serialization-series-index.md" >}})
- [Unity Shader Variant 治理系列索引｜先拆问题边界，再看收集、剔除和交付]({{< relref "engine-notes/unity-shader-variants-series-index.md" >}})
- [存储设备与 IO 基础系列索引｜先立住存储硬件、文件系统和 OS I/O，再回到游戏加载链]({{< relref "engine-notes/storage-io-series-index.md" >}})
