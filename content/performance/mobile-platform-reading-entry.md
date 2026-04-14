---
date: "2026-03-29"
title: "移动平台阅读入口｜客户端程序先建立硬件、渲染和平台差异地图"
description: "给客户端程序的移动平台入口页：先建立 SoC、TBDR、设备档次、持续性能和平台碎片化的概念地图，再进入 Android / iOS 的真实工程决策。"
slug: "mobile-platform-reading-entry"
weight: 2011
featured: false
tags:
  - "Mobile"
  - "Client"
  - "Platform"
  - "Index"
  - "Unity"
series: "移动端硬件与优化"
primary_series: "mobile-hardware-and-optimization"
series_role: "appendix"
---
> 这页主要面向客户端程序。它不是想把你立刻推进一条更深的"移动端优化黑洞"，而是先帮你建立一张足够稳的地图：以后看到手机上的掉帧、发热、花屏、后台重启和机型差异时，至少知道它们大概分别属于哪一层。

如果你已经确认自己要先做性能定位，可以先回 [游戏性能判断入口｜客户端程序先看定位、证据链和引擎显形]({{< relref "performance/game-performance-entry-client-programmer.md" >}})。如果你这次更想回答的是"为什么同一个客户端问题，到了移动平台会像换了一种病"，这页更适合作为起点。

## 为什么客户端程序要单独补这条线

很多客户端程序第一次进入移动端，最容易带着两种错觉：

- 手机只是"性能更低的 PC"。
- 移动端优化只是把参数继续往下砍。

这两种直觉都会让后面的判断越来越偏。

因为移动平台真正麻烦的地方，并不是简单的"硬件更弱"，而是：

- `CPU / GPU / 内存 / 存储` 在争同一份共享预算。
- 渲染代价很多时候更像带宽问题，而不只是算力问题。
- 设备池不是平滑变化，而是分层离散的。
- 峰值性能不等于持续性能，冷机结论也不等于热机结论。
- 平台规则、厂商定制和驱动碎片化会直接变成项目问题。

所以这条阅读线最想帮你先立住的，不是某几个 API 或技巧，而是五个概念开关：

1. 共享预算。
2. 带宽优先。
3. 设备分层。
4. 持续性能。
5. 平台碎片化。

## 第一轮先读这 7 篇

1. [手机和 PC 为什么要用不同的性能直觉]({{< relref "performance/game-performance-mobile-vs-pc-intuition.md" >}})
   先把最容易误导人的 PC 直觉拆掉，知道为什么同样叫"卡"，手机和 PC 往往不是同一种病。
2. [移动端硬件 00｜入门：为什么做移动端优化前，要先懂 SoC、TBDR、带宽和热]({{< relref "performance/mobile-hardware-00-getting-started.md" >}})
   这是这条线最稳的总起点，先把 SoC、TBDR、设备档次、持续性能和平台碎片化收成一张最小地图。
3. [移动端硬件 01｜SoC 总览：CPU / GPU / 内存 / 闪存共享一块芯片意味着什么]({{< relref "performance/mobile-hardware-01-soc-overview.md" >}})
   这一篇会把"共享预算"这件事立住。客户端程序需要先理解，很多移动端问题最后都不是单点热点，而是资源争抢。
4. [移动端硬件 02｜TBDR 架构详解：Tile、On-Chip Buffer、HSR 如何改变渲染逻辑]({{< relref "rendering/hardware-02-tbdr.md" >}})
   这一篇负责把"为什么移动端特别怕带宽浪费"讲清。你后面再看后处理、透明、阴影和 Render Target，判断会稳很多。
5. [移动端硬件 02｜设备档次：旗舰、高端、主流、低端的硬件差距在哪里]({{< relref "performance/mobile-hardware-02-device-tiers.md" >}})
   这一篇负责建立设备分层概念。客户端程序要先接受一个现实：设备池不是连续光谱，而是一组差异很大的平台桶。
6. [移动端硬件 02b｜为什么高端机的游戏体验更持久：散热设计、持续性能与内存稳定性]({{< relref "performance/mobile-hardware-02b-sustained-performance.md" >}})
   这一篇负责把"为什么高端机不只是更快，而是更耐玩"讲清。它会把你的注意力从峰值拉到持续性能上。
7. [移动端硬件 03｜功耗与发热：降频模型、帧率稳定性与热管控策略]({{< relref "performance/mobile-hardware-03-power-thermal.md" >}})
   这一篇把热机掉帧这件事真正接回项目现场。读到这里，你对"前十分钟没问题，二十分钟后全变样"会开始有稳定解释。

如果你第一轮只想先收一个大概概念，不想一口气读完整条线，这 7 篇已经够建立骨架。

## 第二轮再把概念接回项目现场

第一轮建立的是世界观。第二轮要做的，是把这套世界观重新接回客户端程序每天会遇到的工程现实。

建议按这条顺序继续：

1. [移动端硬件 04｜移动端 vs PC / 主机：带宽、内存层级与驱动差异]({{< relref "performance/mobile-hardware-04-mobile-vs-pc.md" >}})
   把前面的硬件直觉重新翻成客户端程序更熟悉的项目语言。
2. [Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化]({{< relref "performance/mobile-unity-android.md" >}})
   把 Android 上最常见的 API 选择、包体、性能策略和坑点接到 Unity 现场。
3. [Unity on Mobile｜iOS 专项：Metal 渲染行为、内存警告机制与 Instruments 联用]({{< relref "performance/mobile-unity-ios.md" >}})
   建立 iOS 的另一套现实：统一得多，但约束也更明确。
4. [Android 厂商定制｜调度器、内存回收与省电策略如何影响游戏]({{< relref "performance/android-oem-customization.md" >}})
   这一篇负责解释为什么"同一颗 SoC 在不同品牌手机上长得不像同一台机器"。
5. [Android Vulkan 驱动架构｜碎片化的根源、Shader 花屏的成因与规避方法]({{< relref "performance/android-vulkan-driver.md" >}})
   这一篇负责把驱动碎片化拉回真实图形问题，而不是只停留在"安卓就是玄学"。
6. [性能分析工具 01｜Unity Profiler 真机连接：USB 接入、GPU Profiler 与 Memory Profiler]({{< relref "performance/mobile-tool-01-unity-profiler-device.md" >}})
   到这里再补工具最合适，因为你已经知道自己为什么必须看真机，而不是只在 Editor 里下结论。

## 按你现在最困惑的问题进入

- 你最困惑的是"为什么 PC 上没事，手机上一开效果就炸"：
  先看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "performance/game-performance-mobile-vs-pc-intuition.md" >}})、[SoC 总览]({{< relref "performance/mobile-hardware-01-soc-overview.md" >}}) 和 [TBDR 架构详解]({{< relref "rendering/hardware-02-tbdr.md" >}})。
- 你最困惑的是"为什么同一场景在不同手机上差这么多"：
  先看 [设备档次]({{< relref "performance/mobile-hardware-02-device-tiers.md" >}})、[持续性能]({{< relref "performance/mobile-hardware-02b-sustained-performance.md" >}}) 和 [Android 厂商定制]({{< relref "performance/android-oem-customization.md" >}})。
- 你最困惑的是"为什么冷机没问题，热机全变样"：
  先看 [持续性能]({{< relref "performance/mobile-hardware-02b-sustained-performance.md" >}}) 和 [功耗与发热]({{< relref "performance/mobile-hardware-03-power-thermal.md" >}})。
- 你最困惑的是"为什么 Android 平台特别难稳"：
  先看 [Android 专项]({{< relref "performance/mobile-unity-android.md" >}})、[Android 厂商定制]({{< relref "performance/android-oem-customization.md" >}}) 和 [Android Vulkan 驱动架构]({{< relref "performance/android-vulkan-driver.md" >}})。
- 你最困惑的是"为什么 Editor 里看起来还行，真机一测全不是一回事"：
  先看 [移动端硬件 04｜移动端 vs PC / 主机]({{< relref "performance/mobile-hardware-04-mobile-vs-pc.md" >}})，再看 [Unity Profiler 真机连接]({{< relref "performance/mobile-tool-01-unity-profiler-device.md" >}})。

## 如果你只想先读最小 6 篇版

如果你最近时间很碎，只想先把客户端程序最需要的移动平台骨架立起来，我建议先读这 6 篇：

1. [手机和 PC 为什么要用不同的性能直觉]({{< relref "performance/game-performance-mobile-vs-pc-intuition.md" >}})
2. [移动端硬件 00｜入门]({{< relref "performance/mobile-hardware-00-getting-started.md" >}})
3. [移动端硬件 01｜SoC 总览]({{< relref "performance/mobile-hardware-01-soc-overview.md" >}})
4. [移动端硬件 02｜TBDR 架构详解]({{< relref "rendering/hardware-02-tbdr.md" >}})
5. [移动端硬件 02｜设备档次]({{< relref "performance/mobile-hardware-02-device-tiers.md" >}})
6. [移动端硬件 03｜功耗与发热]({{< relref "performance/mobile-hardware-03-power-thermal.md" >}})

读完这 6 篇之后，你对移动平台至少应该先有三层稳定判断：

- 它不是低配 PC，而是一套完全不同的预算结构。
- 它的渲染和性能判断常常先被带宽、热和设备分层改写。
- 它的很多问题最后会从"优化问题"演化成"平台治理问题"。

## 这页之后往哪走

- 你要回到更通用的客户端排障路线：
  去 [客户端程序阅读入口｜先按问题类型，再按系统层级进入]({{< relref "system-design/client-programmer-reading-entry.md" >}})。
- 你想继续深入移动端整条主线：
  去 [移动端硬件与优化系列索引｜从 SoC、TBDR 和热约束，到 GPU / CPU / 工具链 / 平台碎片化]({{< relref "performance/mobile-hardware-and-optimization-series-index.md" >}})。
- 你已经开始想理解"平台抽象"为什么会变成引擎问题：
  去 [引擎开发阅读入口｜先立分层地图，再按图形、运行时和平台抽象进入]({{< relref "system-design/engine-programmer-reading-entry.md" >}})。
- 你当前更焦虑的是内存、LMK、jetsam 和运行时工作集：
  去 [内存专题索引｜先分清运行时内存分布，再看预算、RT、流送和系统强杀]({{< relref "performance/memory-topic-index.md" >}})。

## 这页刻意不替你做什么

- 它不把移动平台简化成"多几个低配选项"。
- 它不默认你一开始就要去学最底层 API 细节。
- 它不试图在一页里讲完 Android 和 iOS 的全部工程实践。

这页真正想做的，只是先把移动平台从"很多零散坑"收成一张可重复使用的概念地图。
