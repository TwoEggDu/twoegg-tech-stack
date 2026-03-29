---
date: "2026-03-29"
title: "存储设备与 IO 基础系列索引｜先立住存储硬件、文件系统和 OS I/O，再回到游戏加载链"
description: "给存储设备与 IO 基础补一个稳定入口：先把 HDD / SSD / UFS、文件系统、页缓存、预读和异步 IO 这些底层机制看清，再回到游戏里的小文件、加载链、首载抖动和资源交付问题。"
slug: "storage-io-series-index"
weight: 171
featured: false
tags:
  - "Storage"
  - "IO"
  - "OS"
  - "Index"
series: "存储设备与 IO 基础"
series_id: "storage-io-foundations"
series_role: "index"
series_order: 0
series_nav_order: 18
series_title: "存储设备与 IO 基础"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "资源 / 加载链路"
series_level: "入门到进阶"
series_best_for: "当你想把 HDD / SSD / UFS、文件系统、页缓存和游戏加载链收回同一张结构图"
series_summary: "把存储硬件、文件系统和 OS I/O 机制接到游戏里的小文件访问、加载链和资源交付问题。"
series_intro: "这组文章关心的不是“背几个 IO 术语”，而是先把一张更基础的地图立住：底层存储设备在怕什么，文件系统和 OS I/O 在帮你做什么，又为什么这些现实最后会变成游戏里的首载卡顿、streaming 抖动、碎文件访问和资源链路不稳。只要先把这张地图立住，后面再看合包、AssetBundle、首载卡顿和读盘不等于可用，判断会稳很多。"
series_reading_hint: "第一次系统读，建议先从桥接文进入，再按大整包 vs 碎文件、读盘不等于可用这条主线往下读；如果你已经在项目里碰到首载和 streaming 问题，也可以先从第二篇开始，再回头补底层机制。"
---
> 这页是“存储设备与 IO 基础”的专门入口。它不想把你带进一个纯操作系统教材，而是想先把存储硬件、文件系统、OS I/O 和游戏加载链接成同一条线。

## 这组文章主要在解决什么

如果把这个系列压成一句话，我会这样描述：

`很多项目里看起来像“加载慢”的问题，根子并不只在磁盘速度，而在于存储硬件、文件系统、OS I/O 机制和游戏加载链怎样在错误时机一起结账。`

所以这组文章最适合回答的是：

- 为什么很多小碎文件会比大整包更抖
- 为什么 SSD / UFS 很快，项目还是会卡
- 为什么“异步读完了”还不等于资源已经能安全使用
- 为什么 AssetBundle / Addressables / streaming 最后会回到 I/O 与加载链判断

## 最短阅读路径

1. [存储与 IO 01｜为什么碎文件更慢：从 HDD / SSD / UFS、文件系统和 OS I/O 机制，到游戏加载链]({{< relref "engine-notes/storage-io-01-why-fragmented-files-are-slower.md" >}})
   先把底层硬件、文件系统和 OS I/O 机制怎么一起放大小文件问题讲透。
2. [为什么一个大整文件，往往比很多小散文件更稳]({{< relref "engine-notes/game-performance-big-files-vs-small-files.md" >}})
   再把这张底图映射回游戏里的真实访问模式、首次命中和关键时刻。
3. [读盘完成，为什么还是不等于资源可用]({{< relref "engine-notes/game-performance-read-does-not-mean-ready.md" >}})
   最后把视线从存储层继续推到加载链后半段。

## 如果你是从具体问题进来

- 你想先搞清楚“小文件为什么慢”，从 [为什么碎文件更慢]({{< relref "engine-notes/storage-io-01-why-fragmented-files-are-slower.md" >}}) 开始。
- 你想直接看游戏运行时为什么更怕碎访问，看 [为什么一个大整文件，往往比很多小散文件更稳]({{< relref "engine-notes/game-performance-big-files-vs-small-files.md" >}})。
- 你已经确定不是单纯读盘，而是加载后半段更重，看 [读盘完成，为什么还是不等于资源可用]({{< relref "engine-notes/game-performance-read-does-not-mean-ready.md" >}})。
- 你已经在 AssetBundle / 资源交付现场里排问题，看 [AssetBundle 的性能与内存代价：LZMA/LZ4、首次加载卡顿、内存峰值、解压与 I/O]({{< relref "engine-notes/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md" >}})。

## 交叉阅读

这组文章的主线故意收得比较聚焦。  
如果你还想把问题继续往平台和设备现实扩出去，最适合接着看的通常是：

- [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})
- [移动端硬件 02｜设备档次：旗舰、高端、主流、低端的硬件差距在哪里]({{< relref "engine-notes/mobile-hardware-02-device-tiers.md" >}})
- [移动端硬件 04｜移动端 vs PC / 主机：带宽、内存层级与驱动差异]({{< relref "engine-notes/mobile-hardware-04-mobile-vs-pc.md" >}})
- [Unity 资产系统与序列化系列索引：从资产通识到 Scene、Prefab、Shader 与 AssetBundle]({{< relref "engine-notes/unity-asset-system-and-serialization-series-index.md" >}})

## 后续最值得继续补什么

如果这组文章后面继续扩张，我更建议下一步补这些题目：

- 存储设备类型：HDD / SSD / NVMe / eMMC / UFS 的读写特性与延迟差异
- 文件系统基础：路径、目录项、元数据和页缓存为什么会影响游戏加载
- OS I/O 机制：同步 / 异步 I/O、DMA、mmap、Read-Ahead、队列深度
- 移动端存储特性：eMMC vs UFS 随机读写差异，碎访问为什么在手机上也照样危险

{{< series-directory >}}
