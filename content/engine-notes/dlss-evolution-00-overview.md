---
title: "DLSS 进化论 00｜总论：DLSS 到底改变了什么"
description: "从传统原生渲染的性能瓶颈出发，解释 DLSS 为什么代表了游戏图形向神经渲染的转向。"
slug: "dlss-evolution-00-overview"
weight: 40
featured: false
tags:
  - DLSS
  - Rendering
  - GPU
  - AI
series: "DLSS 进化论"
---

> 这篇文章先立一个总判断：DLSS 不是“更高级的放大工具”，而是游戏图形从传统渲染走向神经渲染的入口。

## 这篇要回答什么

如果把今天的大型 3D 游戏拆开看，你会发现开发者想同时拿到的东西越来越多：4K，120Hz，复杂几何，实时光照，路径追踪，电影级后处理，稳定帧时间，以及尽量低的输入延迟。问题是，这些目标并不会自动兼容。显示分辨率越高，每帧需要处理的像素越多；刷新率越高，每帧可支配时间越少；光照模型越复杂，单位像素的计算成本越高。到了这个阶段，“每个像素都老老实实原生渲染出来”开始变成一条越来越昂贵、而且收益递减的路。

DLSS 改变的，不只是显卡设置里多了一个能提帧的选项。它改变的是图形工业默认的提问方式。过去的问题是：怎样在有限的时间里，把整帧尽可能完整地算出来。DLSS 之后的问题越来越像：哪些信息必须由引擎真实渲染，哪些信息可以由跨帧积累、AI 重建、AI 补帧、AI 光线重建来完成。

## 为什么这个问题在当下重要

NVIDIA 在 `2018-08-20` 发布 RTX 平台时，把实时光追和 AI 能力一起作为新一代图形平台的标志能力。这一点很关键，因为它说明 Tensor Core 从一开始就不是孤立的 AI 计算硬件，而是被设计成可以直接进入实时图形流水线的基础设施。[NVIDIA RTX 平台，2018-08-20](https://nvidianews.nvidia.com/news/nvidia-rtx-platform-brings-real-time-ray-tracing-and-ai-to-barrage-of-blockbuster-games)

此后几年里，DLSS 的演化速度也说明了它不是一个单点功能，而是一条不断向渲染核心深入的路线：

- `2020-03-23`，DLSS 2.0 把早期路线重构成通用时域超分网络。[NVIDIA DLSS 2.0](https://www.nvidia.com/en-us/geforce/news/nvidia-dlss-2-0-a-big-leap-in-ai-rendering/)
- `2022-09-20`，DLSS 3 引入 Frame Generation，开始“生成新帧”。[NVIDIA DLSS 3](https://nvidianews.nvidia.com/news/nvidia-introduces-dlss-3-with-breakthrough-ai-powered-frame-generation-for-up-to-4x-performance)
- `2023-09-21`，DLSS 3.5 引入 Ray Reconstruction，把 AI 拉进光追重建链路。[NVIDIA DLSS 3.5](https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-phantom-liberty-dlss-3-5-ray-reconstruction-game-ready-driver/)
- `2025-01-06`，DLSS 4 把 Multi Frame Generation 和 transformer 模型路线推上台前。[NVIDIA DLSS 4](https://www.nvidia.com/en-us/geforce/news/dlss4-multi-frame-generation-ray-tracing-rtx-games/)
- `2026-01-06`，DLSS 4.5 继续推进 second-generation transformer Super Resolution、Dynamic Multi Frame Generation 和 6X Multi Frame Generation；`2026-03-10` NVIDIA 又确认其 Dynamic MFG 与 6X 模式将在 `2026-03-31` 面向 RTX 50 系列开放测试。[NVIDIA DLSS 4.5](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)；[NVIDIA GDC 2026 更新](https://www.nvidia.com/en-us/geforce/news/gdc-2026-nvidia-geforce-rtx-announcements/)

这条时间线足够说明一件事：DLSS 现在已经不是“把低分辨率放大到高分辨率”的单一方案，而是一个不断扩张的神经渲染套件。

## DLSS 到底改了哪三件事

### 第一，图形的性能预算被重新分配了

DLSS 最直接的作用，是允许开发者不必在目标输出分辨率上把所有像素都完整算出来，而是先渲染一个更低内部成本、但信息尽量完整的基础帧，再通过跨帧积累和模型重建恢复目标画面。NVIDIA 在 `2019-08-30` 的《Control》相关文章里，实际上已经把这件事说透了：下一代游戏对每像素的计算需求会持续升高，于是需要“rendering fewer, richer pixels”，也就是先渲染更少但更有价值的像素，再把它们重建成高分辨率画面。[NVIDIA DLSS: Control and Beyond](https://www.nvidia.com/en-us/geforce/news/dlss-control-and-beyond/)

### 第二，图形流水线不再只相信“这一帧”

现代超分技术的核心不是单帧放大，而是跨帧重建。也就是说，最终输出帧不仅由当前帧决定，还由历史帧、运动矢量、深度、抖动采样等信息共同决定。这个变化的含义非常大：它让“时间”成为图像质量的一部分。你今天看到的画面，不再只是当前 8.33ms 或 16.67ms 内完成的结果，而是多个帧历史共同收敛的结果。

### 第三，AI 开始进入图像形成的中层

当 DLSS 只做 Super Resolution 时，我们还能把它勉强理解成“输出阶段的重建器”。但 DLSS 3 的 Frame Generation、DLSS 3.5 的 Ray Reconstruction、DLSS 4/4.5 的 Multi Frame Generation 和 transformer 路线，意味着模型正在逐渐深入到“如何形成一帧图像”的中层逻辑。它不再只是末端滤镜，而是在参与像素、帧、光照信息的生成与调度。

## 这不只是 NVIDIA 一家的故事

如果市场上只有 DLSS，那么我们很容易把它写成品牌神话。但现实恰恰相反。

AMD 已经把 FSR 从空间超分推进到时域超分、补帧，再推进到当前包含 ML upscaling、Frame Generation、Ray Regeneration 和 Radiance Caching 的 FSR “Redstone” 套件。[AMD FSR Technologies](https://www.amd.com/en/products/graphics/technologies/fidelityfx/super-resolution.html)

Intel 则把 XeSS 扩展成包含 Super Resolution、Frame Generation、Multi Frame Generation 和 Low Latency 的整套 XeSS 3 能力，既强调 Intel Arc 上的最佳效果，也保留了跨厂商 fallback 路线。[Intel XeSS 3](https://www.intel.com/content/www/us/en/developer/topic-technology/gamedev/xess.html)

Sony 在主机端推进 PSSR，并在 `2026-02-27` 公布升级版 PSSR 时明确表示，新算法和神经网络来自与 AMD 的 Project Amethyst 合作。[PlayStation PSSR 更新](https://blog.playstation.com/2026/02/27/upgraded-pssr-upscaler-is-coming-to-ps5-pro/)

微软则在 `2024-05-29` 推出 DirectSR，试图把 DLSS Super Resolution、FSR、XeSS 收进同一组输入输出接口，让开发者用单一代码路径接入多家 SR 技术。[Microsoft DirectSR](https://devblogs.microsoft.com/directx/directsr-preview/)

把这些东西放在一起，你会看到更清晰的图景：神经渲染不是某家公司的功能点，而是在变成行业基础设施。

## 常见误解

### 误解一：DLSS 只是“更聪明的分辨率缩放”

这只对了一小半。DLSS 2.0 之后，它本质上已经是时域重建；DLSS 3 之后，它还开始生成新帧；DLSS 3.5 之后，它甚至进入了光线追踪重建链路。把它继续叫“放大”并不算错，但明显不够用了。

### 误解二：DLSS 的意义只是提升 FPS

如果只是为了提升 FPS，那么它和过去各种简单分辨率缩放器的差别就不会这么大。DLSS 真正重要的是，它改变了开发者如何规划性能预算，改变了玩家如何理解“高画质”，也改变了媒体和社区如何讨论“原生渲染”的地位。

### 误解三：原生分辨率会立刻失去意义

不会。原生渲染仍然是重要基线，是训练和评估模型的参照，是技术对比的标准答案。但它在真实消费体验中的地位，很可能会继续退居成一种基准测试语言，而不是默认体验语言。

## 我的结论

如果必须用一句话概括 DLSS 到底改变了什么，我会这样说：

> DLSS 的意义，不是把低分辨率图像“拉高”，而是让游戏图形从“每个像素都必须真算出来”，走向“部分真实渲染 + 时域积累 + AI 重建 + AI 生成”的神经渲染时代。

后面几篇文章会把这句话拆开讲清楚：它为什么会诞生，为什么 1.0 到 2.0 是断代变化，为什么 3.0 之后它已经不再只是超分，以及它和 FSR、XeSS、PSSR、TSR、MetalFX、DirectSR 这些路线到底是什么关系。

## 参考资料

- [NVIDIA RTX 平台，2018-08-20](https://nvidianews.nvidia.com/news/nvidia-rtx-platform-brings-real-time-ray-tracing-and-ai-to-barrage-of-blockbuster-games)
- [NVIDIA DLSS: Control and Beyond，2019-08-30](https://www.nvidia.com/en-us/geforce/news/dlss-control-and-beyond/)
- [NVIDIA DLSS 2.0，2020-03-23](https://www.nvidia.com/en-us/geforce/news/nvidia-dlss-2-0-a-big-leap-in-ai-rendering/)
- [NVIDIA DLSS 3，2022-09-20](https://nvidianews.nvidia.com/news/nvidia-introduces-dlss-3-with-breakthrough-ai-powered-frame-generation-for-up-to-4x-performance)
- [NVIDIA DLSS 3.5，2023-09-21](https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-phantom-liberty-dlss-3-5-ray-reconstruction-game-ready-driver/)
- [NVIDIA DLSS 4，2025-01-06](https://www.nvidia.com/en-us/geforce/news/dlss4-multi-frame-generation-ray-tracing-rtx-games/)
- [NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)
- [NVIDIA GDC 2026 更新，2026-03-10](https://www.nvidia.com/en-us/geforce/news/gdc-2026-nvidia-geforce-rtx-announcements/)
- [AMD FSR Technologies](https://www.amd.com/en/products/graphics/technologies/fidelityfx/super-resolution.html)
- [Intel XeSS 3](https://www.intel.com/content/www/us/en/developer/topic-technology/gamedev/xess.html)
- [PlayStation PSSR 更新，2026-02-27](https://blog.playstation.com/2026/02/27/upgraded-pssr-upscaler-is-coming-to-ps5-pro/)
- [Microsoft DirectSR，2024-05-29](https://devblogs.microsoft.com/directx/directsr-preview/)
