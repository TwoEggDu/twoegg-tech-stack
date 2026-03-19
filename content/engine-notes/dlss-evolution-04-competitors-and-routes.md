---
title: "DLSS 进化论 04｜FSR、XeSS、PSSR、MetalFX、TSR、DirectSR：谁在走哪条路"
description: "把竞品和相关路线拆成厂商方案、引擎方案、平台接口三个层级，并从硬件依赖、运行时分发和接入契约角度解释它们的真实差异。"
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

但如果只分层，也还是不够硬核。因为开发者真正关心的不只是名字，而是这四件事：

1. 它到底依赖什么硬件。  
2. 它能不能靠驱动和运行时持续升级。  
3. 它和引擎之间的接入契约有多深。  
4. 它在生态里的位置，是厂商闭环、引擎内建，还是平台公共接口。  

## 先给一个结构表

| 路线 | 所在层级 | 硬件依赖 | 运行时/分发 | 接入契约深度 | 生态位置 |
| --- | --- | --- | --- | --- | --- |
| NVIDIA DLSS | 厂商级套件 | Tensor / OFA / display engine 等专用硬件最深 | NGX + 驱动 + NVIDIA app 持续升级 | 深，吃 motion vectors、历史信息、FG/RR 等多条契约 | 最强闭环 |
| AMD FSR | 厂商级套件 | 尽量弱化专用硬件依赖，强调广覆盖 | 以游戏集成和 SDK 版本为主，驱动协同较弱 | 中到深，取决于 FSR 2/3/4 与 FMF 路线 | 开放生态 |
| Intel XeSS | 厂商级套件 | XMX 最优，DP4a fallback | 既有专有优化，又保留跨厂商路径 | 中到深，SR/FG/MFG 与低延迟并存 | 折中路线 |
| Sony PSSR | 平台级厂商方案 | 主机固定硬件平台，深定制 | 随主机系统与游戏版本推进 | 深，但在封闭平台里可控 | 主机闭环 |
| Apple MetalFX | 平台级厂商方案 | Apple GPU + Metal 能力栈 | OS / Metal 框架级分发 | 中，平台 API 统一 | 平台一体化 |
| Unreal TSR | 引擎级方案 | 平台无关，不依赖专有 AI 硬件 | 随引擎版本演进 | 深入引擎，但不深入厂商驱动 | 引擎公共能力 |
| DirectSR | 平台接口 | 不定义专有硬件 | 由系统/平台接口抽象 | 浅到中，统一 I/O 合约 | 标准化接口 |

## 一、NVIDIA DLSS：最深的软硬件闭环

DLSS 的强项不只是模型质量，而是它把四层东西绑在了一起：

- GPU 专用硬件  
- 驱动与 NGX runtime  
- Streamline / NGX 接入框架  
- 模型分发与 per-game override  

这也是为什么 DLSS 很难只被叫做一个 upscaler。它从 DLSS 2 到 DLSS 5 的路线，实际上对应着 NVIDIA 逐步把更多专用硬件借给神经渲染：

- `Turing`：Tensor Cores + NGX 让 DLSS 得以作为运行时能力成立。  
- `Ada`：新的 Optical Flow Accelerator 让 DLSS 3 Frame Generation 成为新模式。  
- `Blackwell`：5th-gen Tensor Cores、hardware Flip Metering 和增强 display engine 让 MFG 与更复杂的调度真正落地。  
  [NVIDIA Turing 发布，2018-08-13](https://nvidianews.nvidia.com/news/nvidia-reinvents-computer-graphics-with-turing-architecture)；[Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)；[NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)

从分发机制看，DLSS 也明显和其他方案不同。它建立在 NGX Core Runtime、NGX Update Module、驱动分发和 NVIDIA app override 能力之上，这意味着模型、preset、兼容行为和部分 runtime 逻辑可以持续升级，而不是只能跟着游戏首发版本走。[NVIDIA NGX Programming Guide](https://docs.nvidia.com/ngx/programming-guide/index.html)；[NVIDIA App Update Adds DLSS 4 Overrides](https://www.nvidia.com/en-us/geforce/news/nvidia-app-update-dlss-overrides-and-more.html)

但它的代价也很清楚：

- 生态闭环很深。  
- 功能边界高度受 NVIDIA 硬件代际能力影响。  
- 最先进的 FG / MFG / 视觉增强能力，并不天然能跨到所有硬件。  

也就是说，DLSS 的本质不是“更聪明的算法”，而是 **最深的一套消费级神经渲染闭环。**

## 二、AMD FSR：尽量不把未来押在专有硬件上

AMD 的路线特别适合做对照，因为它从一开始就尽量弱化“必须依赖某家专用 AI 硬件”这件事。

- `FSR 1` 是空间超分，优势是简单、开放、覆盖广。[AMD FSR 1，2021-06-22](https://www.amd.com/en/newsroom/press-releases/2021-6-22-with-amd-fidelityfx-super-resolution-amd-brings-h.html)  
- `FSR 2` 转向 temporal data，正式进入时域重建路线。[AMD FSR 2.0，2022-03-17](https://www.amd.com/en/newsroom/press-releases/2022-3-17-introducing-amd-software-adrenalin-edition-2022-r.html)  
- `FSR 3` 加入 Frame Generation，但其叙事依然更强调 game motion vectors 与广兼容，而不是某个专用硬件块。[AMD FSR 3，2023-08-25](https://www.amd.com/en/newsroom/press-releases/2023-8-25-new-amd-radeon-rx-7800-xt-and-radeon-rx-7700-xt-gr.html)  
- 当前官方页已把 FSR 扩展成 `FSR Upscaling + FSR Frame Generation + FSR Ray Regeneration + FSR 4 + Redstone` 的更大技术集合。[AMD FSR Technologies](https://www.amd.com/en/products/graphics/technologies/fidelityfx/super-resolution.html)

这条路线的工程含义是：

- AMD 不想把未来完全锁死在某个单一卡种专有能力上。  
- 它愿意牺牲一部分极致闭环，换来更广的生态覆盖和更低的碎片化。  
- 它的升级更多依赖开发者采用新的 SDK / 新版本集成，而不是像 DLSS 那样强依赖独立 runtime 和 app override 机制。  

这也解释了为什么 FSR 的讨论经常和“开放”绑在一起。这里的开放，不只是源码或品牌姿态，而是它在架构上就尽量避免把最核心能力写成“没有这块专用硬件就完全无法进入”的模式。

## 三、Intel XeSS：专有硬件最优，fallback 保底

XeSS 的位置更像一条折中路线。Intel 希望在自家 Arc / XMX 上获得最好的 AI 推理表现，但又保留 DP4a fallback，让别家硬件也能跑起来。

Intel 当前 XeSS 3 开发者页面，已经把 XeSS 定义成一个包含：

- XeSS Super Resolution  
- XeSS Frame Generation  
- XeSS Multi Frame Generation  
- Xe Low Latency  

的完整技术集合。[Intel XeSS 3](https://www.intel.com/content/www/us/en/developer/topic-technology/gamedev/xess.html)

Intel 官方回顾还明确提到，XeSS 最早在 `2022` 年随 Arc A-Series 上线。[Intel Gaming Access 对 XeSS 的回顾，2025-05-06](https://game.intel.com/us/stories/xess-2-now-available-in-10-more-games-get-up-to-4x-boost-in-fps/)

XeSS 的价值，在于它把“专用硬件最优”和“跨硬件可运行”这两件事硬凑在了一起。它不像 DLSS 那样把生态锁得很紧，也不像 FSR 那样尽量抹平专有硬件差异，而是更接近：

- 有 XMX，就吃更完整的 AI 路线。  
- 没有 XMX，就退回 fallback 路径。  

这种设计的优点是更容易兼顾性能和生态，缺点则是产品叙事会更复杂，因为用户实际体验不完全由“XeSS”这个名字决定，还高度取决于跑在哪种硬件路径上。

## 四、Sony PSSR：主机平台的深闭环验证

PSSR 的价值，不是因为它和 DLSS 名字相像，而是因为它证明了 **固定硬件主机平台也已经接受神经渲染会成为基础能力**。

`2026-02-27`，Sony 公布升级版 PSSR 时明确提到，新算法和神经网络来自与 AMD 的 Project Amethyst 合作，而且 PC 玩家已经能在 AMD FSR 4 中看到合作成果的一部分。[PlayStation PSSR 更新，2026-02-27](https://blog.playstation.com/2026/02/27/upgraded-pssr-upscaler-is-coming-to-ps5-pro/)

主机路线和 PC 最大的不同，不在算法本身，而在工程环境：

- 硬件固定。  
- 系统版本可控。  
- 开发者目标平台稳定。  
- 调优可以更深入地围绕单一 SoC 与显示目标展开。  

这意味着 PSSR 这类方案未必需要像 PC 一样为极多驱动版本和硬件组合兜底。相反，它更像在验证另一种可能：**当硬件平台足够固定时，神经渲染可以更深地写进平台默认图形工作流。**

## 五、Apple MetalFX：系统框架级的一体化路线

MetalFX 往往被 PC 讨论忽略，但从架构视角看，它很典型。Apple 在 `2022` 年 WWDC 的《Boost performance with MetalFX Upscaling》中把 MetalFX 作为 Metal 3 的组成部分推出，提供 spatial scaler 与 temporal scaler。[Apple WWDC22: Boost performance with MetalFX Upscaling](https://developer.apple.com/videos/play/wwdc2022/10103/)

当前 Apple《What’s new in Metal》页面又把 Metal 4 的能力延展到 frame interpolation 和 denoising。[Apple What’s new in Metal](https://developer.apple.com/metal/whats-new/)

Apple 路线的关键不在于“和 DLSS 正面对打”，而在于它说明当平台、OS、API、GPU 和工具链完全归一时，神经渲染会自然演变成 **框架能力**，而不是单独显卡品牌能力。

也就是说，MetalFX 的生态位置更像：

- 不强调独立 runtime 品牌。  
- 不强调跨厂商扩张。  
- 而是把神经图形能力吸收到系统图形框架里。  

这和 NVIDIA 的品牌闭环、AMD 的开放生态、Intel 的双路径设计都不一样。

## 六、Unreal TSR：它不是厂商竞品，但它定义了现代引擎的默认思路

严格说，TSR 不是厂商竞品，因为它不是围绕某家 GPU 硬件打造的专有神经渲染栈，而是引擎级 temporal upscaler。

Epic 官方把 TSR 定义为 platform-agnostic temporal upscaler，并强调它能在较低内部渲染分辨率下逼近 4K 输出，同时保持几何细节和稳定性。[Unreal Engine Temporal Super Resolution](https://dev.epicgames.com/documentation/unreal-engine/temporal-super-resolution-in-unreal-engine)

TSR 最重要的意义，不是“它和 DLSS 比谁更清楚”，而是它证明：

- 时间维度重建已经足够重要，值得直接写进引擎内核。  
- 哪怕不借助某家专用 AI 硬件，引擎也会主动围绕低内部渲染分辨率 + 时域重建来组织现代图形预算。  
- 这让厂商方案必须回答一个新问题：除了更强模型和专有硬件，你还能比引擎内建方案多给开发者什么。  

从接入契约看，TSR 的优势在于它直接活在 render graph 内部；它的限制也在于它天然缺少 DLSS 那种跨驱动 runtime 和专用硬件协同深度。

## 七、DirectSR：公共接口层的抽象开始出现

`2024-05-29`，微软推出 DirectSR Preview。它的目标不是再发明一个超分算法，而是提供 single code path 去对接 DLSS Super Resolution、FSR 和 XeSS，并用 “implement once and ship SR” 概括价值。[Microsoft DirectSR，2024-05-29](https://devblogs.microsoft.com/directx/directsr-preview/)

DirectSR 的技术意义，是把“厂商特性接入”抽象成“平台公共接口”。

但要注意它解决的问题边界：

- 它主要统一的是 SR 层输入输出。  
- 它并不天然等于统一 FG、MFG、RR、低延迟或 DLSS 5 这类更深能力。  
- 它更像是在接口层减少碎片化，而不是抹平所有厂商能力差异。  

也就是说，未来竞争很可能会形成一个双层结构：

- 底层公共接口越来越统一。  
- 顶层专有能力越来越深入。  

这也是为什么 DirectSR 很重要，但又不能被误写成“微软做了一个 DLSS 替代品”。

## 八、如果按‘硬件依赖、运行时分发、接入契约’重新排一次，差别会更清楚

### 1. 硬件依赖

- `DLSS`：依赖专用 Tensor / OFA / display engine 路线最深。  
- `FSR`：尽量弱化专用硬件依赖。  
- `XeSS`：XMX 最优，DP4a 保底。  
- `PSSR / MetalFX`：绑定固定平台硬件，但因为平台固定，优化可以更深。  
- `TSR / DirectSR`：不以某家专有 AI 硬件为前提。  

### 2. 运行时分发

- `DLSS`：最像“驱动 + runtime + app + per-game override”共同维护的系统。  
- `FSR / XeSS`：更依赖 SDK 集成和游戏版本升级；运行时独立分发色彩弱一些。  
- `PSSR / MetalFX`：更像系统/平台能力，跟随平台软件栈演进。  
- `TSR`：跟随引擎版本演进。  
- `DirectSR`：跟随平台接口演进。  

### 3. 接入契约

- `DLSS / FSR 2+ / XeSS / TSR`：都需要运动矢量、历史帧、分辨率策略等时域重建契约。  
- `FG / MFG / RR / DLSS 5` 这类更深能力：会继续要求更复杂的缓冲、遮罩、时域一致性和 HUD 策略。  
- `DirectSR`：尽量把最基础的 SR I/O 统一，但不会天然吃掉更深层契约复杂度。  

## 竞品篇里最容易写错的地方

### 第一，把同层对比和跨层关系混在一起

DLSS、FSR、XeSS 更像同层方案；TSR 是引擎层方案；DirectSR 是接口层方案；PSSR 和 MetalFX 则带有极强平台特征。如果把这些名字排成一个简单分数榜，文章一定会失真。

### 第二，只写画质和帧率，不写分发与维护成本

真实工程世界里，开发者关心的不只是截图，而是：

- 这个功能要接多深。  
- 后续能不能通过 runtime 升级。  
- 要不要为不同 GPU 厂商维护多套特殊路径。  
- UI、透明物体、后处理、低延迟是否会带来新的维护成本。  

### 第三，把开放误写成技术一定更弱，把闭环误写成天然更先进

开放和闭环不是强弱关系，而是工程取舍。闭环能做更深协同，开放能做更广覆盖。很多时候，两者只是目标函数不同。

## 我的结论

如果一定要用一句话概括这篇文章，我会这样写：

> 今天围绕 DLSS 的竞争，不是单一算法之间的竞争，而是专用硬件、运行时分发、引擎契约和平台接口四个层面的多层竞争。

DLSS 当然是这场竞赛里最有代表性的名字，但它并不是孤例。真正发生的事情是：越来越多公司都在承认，现代游戏图形必须学会把“真实渲染”和“模型重建”混合起来；区别只在于，每家公司把这条边界画在了不同位置。

## 参考资料

- [NVIDIA Turing 发布，2018-08-13](https://nvidianews.nvidia.com/news/nvidia-reinvents-computer-graphics-with-turing-architecture)
- [Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)
- [NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)
- [NVIDIA NGX Programming Guide](https://docs.nvidia.com/ngx/programming-guide/index.html)
- [NVIDIA App Update Adds DLSS 4 Overrides](https://www.nvidia.com/en-us/geforce/news/nvidia-app-update-dlss-overrides-and-more.html)
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
