---
title: "DLSS 进化论 04｜FSR、XeSS、PSSR、MetalFX、TSR、DirectSR：谁在走哪条路"
description: "把竞品和相关路线拆成厂商方案、引擎方案、平台接口三个层级，解释它们各自解决的到底是不是同一个问题。"
slug: "dlss-evolution-04-competitors-and-routes"
weight: 80
featured: false
tags:
  - DLSS
  - FSR
  - XeSS
  - MetalFX
  - DirectSR
series: "DLSS 进化论"
---

## 这篇要回答什么

“市面上有没有类似 DLSS 的技术？”

有，而且不止一种。但这件事最容易写错的地方，是把不同层级的技术硬拉到同一张横向对比表里。严格说来，今天围绕 DLSS 的相关路线至少分成三层：

- 厂商级方案：DLSS、FSR、XeSS、PSSR、MetalFX
- 引擎级方案：Unreal TSR
- 平台级接口：DirectSR

如果不先分层，文章就会变成“谁更强”的混战，而不是“谁在用什么方式解决什么问题”的分析。

## 先给一个总表

| 路线 | 所在层级 | 当前公开能力重点 | 强项 | 局限 |
| --- | --- | --- | --- | --- |
| NVIDIA DLSS | 厂商级套件 | SR、FG、Ray Reconstruction、MFG、Low Latency 协同 | 软硬件闭环最深 | 依赖 NVIDIA 生态和专有硬件能力 |
| AMD FSR | 厂商级套件 | Temporal Upscaling、FG、ML upscaling、Ray Regeneration | 覆盖广、开放度高 | 闭环协同和专有硬件控制弱一些 |
| Intel XeSS | 厂商级套件 | SR、FG、MFG、Low Latency | Intel 硬件优化 + 跨厂商 fallback | 生态体量仍在追赶 |
| Sony PSSR | 平台级厂商方案 | 主机端 AI upscaling，持续升级 | 封闭主机平台、可深度定制 | 主要面向 PlayStation 生态 |
| Apple MetalFX | 平台级厂商方案 | Spatial/Temporal upscaling、Metal 4 denoising、frame interpolation | Apple 平台深度整合 | 平台范围窄，不是 PC 通用方案 |
| Unreal TSR | 引擎级方案 | 平台无关的 temporal upscaler | 接入统一、跨平台强 | 不绑定专有 AI 硬件能力 |
| DirectSR | 平台接口 | 单一输入输出接口接入多家 SR | 降低接入成本，推进标准化 | 当前覆盖核心是 SR，不是完整神经渲染套件 |

## AMD FSR：从开放超分，到开放神经渲染栈

AMD 的路线特别适合拿来观察产业演进，因为它几乎走完了“从简单到复杂”的完整路径。

`2021-06-22` 的 FSR 1 还是一套空间超分方案，AMD 当时强调的重点是开放、跨平台和易接入。[AMD FSR 1，2021-06-22](https://www.amd.com/en/newsroom/press-releases/2021-6-22-with-amd-fidelityfx-super-resolution-amd-brings-h.html)

到了 `2022-03-17` 的 FSR 2，AMD 官方开始明确写出 uses temporal data，这说明 FSR 正式进入时域超分路线。[AMD FSR 2.0，2022-03-17](https://www.amd.com/en/newsroom/press-releases/2022-3-17-introducing-amd-software-adrenalin-edition-2022-r.html)

再到 `2023-08-25` 的 FSR 3，AMD 引入了 Frame Generation，使用 Fluid Motion Frames 与 game motion vector data 提升显示帧率。[AMD FSR 3，2023-08-25](https://www.amd.com/en/newsroom/press-releases/2023-8-25-new-amd-radeon-rx-7800-xt-and-radeon-rx-7700-xt-gr.html)

截至 `2026-03-17`，AMD 官方技术页已经把 FSR 组织成更大的技术栈：FSR Upscaling、FSR Frame Generation、FSR Ray Regeneration、FSR 4，以及 FSR “Redstone” ML-powered features，其中还包括 Neural Radiance Caching 等项目。[AMD FSR Technologies](https://www.amd.com/en/products/graphics/technologies/fidelityfx/super-resolution.html)

这说明 AMD 也在走向“神经渲染套件”，只是它的风格始终更偏开放生态和广覆盖，而不是 NVIDIA 式的深闭环。

## Intel XeSS：专有硬件优化和跨厂商 fallback 并存

Intel 的 XeSS 代表的是一条折中路线。Intel 希望在自家 Arc / XMX 硬件上拿到更好的 AI 加速效果，同时也保留 DP4a 这类跨硬件 fallback 路线。这样做的意思是：既不放弃自家硬件优势，也不愿意把生态完全锁死在自家卡上。

Intel 当前的 XeSS 3 开发者页面，已经把整套能力写得很明确：XeSS Super Resolution、XeSS Frame Generation、XeSS Multi Frame Generation 和 Xe Low Latency 都被纳入同一技术集合。[Intel XeSS 3](https://www.intel.com/content/www/us/en/developer/topic-technology/gamedev/xess.html)

Intel 自己的官方回顾文章又明确提到，XeSS 最早是在 `2022` 年随 Arc A-Series 推出的。[Intel Gaming Access 对 XeSS 的回顾，2025-05-06](https://game.intel.com/us/stories/xess-2-now-available-in-10-more-games-get-up-to-4x-boost-in-fps/)

所以 XeSS 的位置很清楚：它不是 AMD 那种尽量开放的通用方案，也不是 NVIDIA 那种最强闭环，而是在专有优化和生态扩张之间找平衡。

## Sony PSSR：主机生态也在走 AI upscaling

PSSR 特别值得写，因为它意味着这条路线已经不只是 PC 显卡厂商在推进，而是连主机平台也在把 AI upscaling 当作基础能力。

`2026-02-27`，Sony 在 PlayStation Blog 上公布升级版 PSSR 时，明确表示新的算法和神经网络来自与 AMD 的 Project Amethyst 合作，而且 PC 玩家已经能通过 AMD FSR 4 看到合作成果的一部分。[PlayStation PSSR 更新，2026-02-27](https://blog.playstation.com/2026/02/27/upgraded-pssr-upscaler-is-coming-to-ps5-pro/)

这件事的意义不在于“索尼也做了一个和 DLSS 类似的功能”，而在于：主机这种过去更强调固定硬件、定制优化、长周期打磨的平台，也已经接受“让模型参与最终图像生成”将是未来图形栈的一部分。

## Apple MetalFX：苹果路线的价值在于平台一体化

很多 PC 圈讨论 DLSS 时会忽略 MetalFX，但它其实是很有代表性的一条路线。Apple 在 `2022` 年 WWDC 的《Boost performance with MetalFX Upscaling》里，把 MetalFX 作为 Metal 3 的一部分推出，提供 spatial scaler 和 temporal scaler，目标同样是用更低内部渲染分辨率换取更高显示分辨率和更稳的性能。[Apple WWDC22: Boost performance with MetalFX Upscaling](https://developer.apple.com/videos/play/wwdc2022/10103/)

这说明苹果很早就接受了一个事实：在功耗受限、平台整合度极高的设备上，纯粹坚持高分辨率原生渲染并不划算。

更有意思的是，Apple 当前的《What’s new in Metal》页面已经把 Metal 4 的重点描述扩展到了 frame interpolation 和 denoising，说明 MetalFX 也在从“upscaling 工具”继续向更完整的图形辅助栈延伸。[Apple What’s new in Metal](https://developer.apple.com/metal/whats-new/)

因此，MetalFX 虽然不直接参与 PC 显卡大战，但它说明另一件事：当平台垂直整合足够强时，神经渲染同样会成为系统级特性，而不是单独的显卡卖点。

## Unreal TSR：这不是竞品，但必须放进来

严格说，TSR 不是 DLSS 的“厂商竞品”，因为它是引擎级 temporal upscaler，而不是某个 GPU 厂商的专有套件。但如果你真的想理解这个领域，TSR 反而是必须讨论的一条线。

原因很简单。TSR 说明：哪怕不依赖某家硬件厂商的专门神经网络路线，时域重建也已经足够重要，值得直接写进引擎内核。Epic 的官方文档明确把 TSR 描述为 platform-agnostic temporal upscaler，并强调它能在较低内部渲染分辨率下逼近 4K 输出，同时保持较好的几何细节与稳定性。[Unreal Engine TSR 文档](https://dev.epicgames.com/documentation/unreal-engine/temporal-super-resolution-in-unreal-engine)

TSR 的意义不在于“它是不是比 DLSS 更强”，而在于它证明了时间维度重建已经成为现代引擎的默认思路之一。

## DirectSR：它不是算法，而是标准化接口

`2024-05-29`，微软推出 DirectSR Preview。微软的表述非常明确：DirectSR 提供一组通用输入输出，让开发者可以用 single code path 同时对接 DLSS Super Resolution、FSR 和 XeSS，并用 “implement once and ship SR” 来概括其价值。[Microsoft DirectSR，2024-05-29](https://devblogs.microsoft.com/directx/directsr-preview/)

这件事的重要性在于，它说明超分能力正在从厂商私有 SDK 走向平台级基础设施。哪怕现在 DirectSR 的重心还主要在 SR，而不是完整覆盖补帧、光线重建和低延迟，但它已经透露出未来竞争的另一个维度：

- 一边是专有模型和硬件协同继续深化。
- 另一边是公共接口和接入标准继续抽象。

未来几年，这两股力量很可能会同时存在。

## 竞品篇里最容易写错的地方

### 第一，把“同层对比”和“跨层关系”混为一谈

DLSS、FSR、XeSS 更像是同层方案；TSR 是引擎层方案；DirectSR 是接口层方案。MetalFX 与 PSSR 则是强平台特征路线。如果把它们粗暴排成“谁更好”，文章一定会失真。

### 第二，只写画质和帧率，不写接入成本和生态位置

开发者真实关心的，从来不只是截图里哪张更锐，而是：接入难不难，维护重不重，跨平台怎么办，升级成本高不高，调试是否可控。DirectSR 和 TSR 之所以重要，就是因为它们分别在接口层和引擎层降低了这部分摩擦。

### 第三，把“开放”误写成“技术一定更弱”

开放和闭环不是强弱关系，而是工程取舍。闭环方案常常能把软硬件协同做得更深；开放方案则更容易扩大覆盖面，降低碎片化。

## 我的结论

如果一定要用一句话概括这篇文章，我会这样写：

> 今天围绕 DLSS 的竞争，不是单一算法之间的竞争，而是厂商套件、引擎方案、平台接口和整机生态之间的多层竞争。

DLSS 当然是这场竞赛里最有代表性的名字，但它并不是孤例。真正发生的事情是：越来越多公司都在承认，现代游戏图形必须学会把“真实渲染”和“模型重建”混合起来。

## 参考资料

- [AMD FSR 1，2021-06-22](https://www.amd.com/en/newsroom/press-releases/2021-6-22-with-amd-fidelityfx-super-resolution-amd-brings-h.html)
- [AMD FSR 2.0，2022-03-17](https://www.amd.com/en/newsroom/press-releases/2022-3-17-introducing-amd-software-adrenalin-edition-2022-r.html)
- [AMD FSR 3，2023-08-25](https://www.amd.com/en/newsroom/press-releases/2023-8-25-new-amd-radeon-rx-7800-xt-and-radeon-rx-7700-xt-gr.html)
- [AMD FSR Technologies](https://www.amd.com/en/products/graphics/technologies/fidelityfx/super-resolution.html)
- [Intel XeSS 3](https://www.intel.com/content/www/us/en/developer/topic-technology/gamedev/xess.html)
- [Intel Gaming Access 对 XeSS 的回顾，2025-05-06](https://game.intel.com/us/stories/xess-2-now-available-in-10-more-games-get-up-to-4x-boost-in-fps/)
- [PlayStation PSSR 更新，2026-02-27](https://blog.playstation.com/2026/02/27/upgraded-pssr-upscaler-is-coming-to-ps5-pro/)
- [Apple WWDC22: Boost performance with MetalFX Upscaling](https://developer.apple.com/videos/play/wwdc2022/10103/)
- [Apple What’s new in Metal](https://developer.apple.com/metal/whats-new/)
- [Unreal Engine Temporal Super Resolution](https://dev.epicgames.com/documentation/unreal-engine/temporal-super-resolution-in-unreal-engine)
- [Microsoft DirectSR，2024-05-29](https://devblogs.microsoft.com/directx/directsr-preview/)
