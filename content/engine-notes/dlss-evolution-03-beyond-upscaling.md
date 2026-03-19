---
title: "DLSS 进化论 03｜DLSS 为什么不再只是超分"
description: "从 DLSS 3、3.5、4 到 4.5，解释 DLSS 为什么已经越过了单纯的超分辨率，进入神经渲染套件阶段。"
slug: "dlss-evolution-03-beyond-upscaling"
weight: 70
featured: false
tags:
  - DLSS
  - Frame Generation
  - Ray Tracing
  - Transformer
series: "DLSS 进化论"
---

## 这篇要回答什么

如果说 DLSS 2.0 是“把超分这件事做对了”，那么 DLSS 3 之后的阶段，就是它开始越过“超分”本身。

很多人今天仍然习惯把 DLSS 理解成一个 upscaler，但只要把 `2022` 年之后的时间线顺一遍，这个定义就明显不够用了。因为 DLSS 现在不仅在补像素，还在补帧、补光线信息，并且开始把模型深度嵌进图像形成链路里。

## DLSS 3：从重建一帧，到生成一帧

`2022-09-20`，NVIDIA 发布 DLSS 3。官方最关键的一句表述是：Optical Multi Frame Generation can generate entirely new frames, rather than just pixels。这里的分量非常重，因为它意味着 DLSS 从“帮助重建一帧”正式走到了“帮助创造一帧”。[NVIDIA DLSS 3，2022-09-20](https://nvidianews.nvidia.com/news/nvidia-introduces-dlss-3-with-breakthrough-ai-powered-frame-generation-for-up-to-4x-performance)

在 DLSS 2.0 时代，至少每一帧基础画面仍然是游戏引擎真实渲染出来的；DLSS 负责把它重建得更接近目标分辨率。到了 DLSS 3，系统开始在两帧“真帧”之间插入一帧“生成帧”。

这件事改变了什么？

- 它不再只缓解像素着色开销，也开始改善 CPU 限制场景下的显示流畅度。
- 它不再只关心图像细节，也要关心帧 pacing 和运动连续性。
- 它把光流估计、运动矢量、显示链路的协同带进了讨论中心。

而且这里必须再往下拆一层硬件。DLSS 3 之所以不是“DLSS 2 跑快一点”，关键不在营销命名，而在 Ada 架构新增了新的 Optical Flow Accelerator。NVIDIA 对 Ada 和 DLSS 3 的官方表述非常明确：DLSS 3 由 fourth-generation Tensor Cores 和新的 Optical Flow Accelerator 驱动。后者的作用，是捕捉粒子、反射、阴影、光照等像素级运动信息，而这些信息并不总是存在于游戏引擎自己的 motion vectors 里。也就是说，DLSS 3 的 Frame Generation 并不是纯软件外插，而是建立在 **新的硬件观测信号** 之上的新模式。[NVIDIA Ada Architecture](https://www.nvidia.com/en-us/technologies/ada-architecture/)；[Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)

## 帧率更高，不等于交互延迟自动按比例更低

这里必须把一个常见误解掰开。Frame Generation 改善的是显示出来的流畅度，但它并没有让游戏逻辑更新频率按同样倍率同步增长。也就是说，生成帧可以让观感更顺，却不会神奇地把用户输入采样次数同步翻倍。

这就是为什么 NVIDIA 要把 Reflex 和 DLSS 3 一起推。Reflex 的任务是缩短渲染队列与系统延迟，尽量避免“看起来更丝滑，但摸起来更迟钝”的问题。[NVIDIA Reflex](https://developer.nvidia.com/reflex)

所以，DLSS 3 真正改写的不是“显卡多算了几帧”，而是“系统如何利用已有帧信息与光流信息，生成更连续的显示体验”。

## DLSS 3.5：AI 开始进入光线追踪重建链路

`2023-09-21`，DLSS 3.5 发布，重点是 Ray Reconstruction。表面上看，它像是一种“让光追画面更稳定”的改良；本质上，它意味着 AI 不再只是末端的分辨率重建器，而开始进入光线追踪链路本身。

传统实时光追因为采样数有限，经常需要多套手工调出来的 denoiser 去平滑反射、阴影和间接光照。NVIDIA 在 DLSS 3.5 里提出的做法，是用 AI 模型取代这些分散的传统 denoiser 逻辑，改善反射细节、光照稳定性和动态场景表现。[NVIDIA DLSS 3.5，2023-09-21](https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-phantom-liberty-dlss-3-5-ray-reconstruction-game-ready-driver/)

到了 `2025-01-06` 的 DLSS 4 介绍里，NVIDIA 已经更明确地把 Ray Reconstruction 描述为“replaces multiple hand-tuned denoisers with a single AI model”。这说明它的定位不再是附加增强，而是开始尝试统一一部分光照重建工作。[NVIDIA DLSS 4，2025-01-06](https://www.nvidia.com/en-us/geforce/news/dlss4-multi-frame-generation-ray-tracing-rtx-games/)

## DLSS 4：它开始像一套神经渲染组件

`2025-01-06` 的 DLSS 4 有两个信号非常明确。

第一，Multi Frame Generation 出现了。也就是说，系统已经不满足于在两帧之间补一帧，而是开始朝着更高倍率、更动态的多帧生成推进。  
第二，NVIDIA 开始反复强调 transformer 模型在 Super Resolution、Ray Reconstruction 和 DLAA 上的作用。

这两个变化组合起来，意味着 DLSS 的中心议题已经不是“超分能不能做”，而是“模型能否更稳定地理解图像形成过程，并在多个环节协同工作”。

如果从硬件层继续往下看，Blackwell 的意义也不能只写成“Tensor Core 更强”。NVIDIA 在 DLSS 4 的技术文章里给出的重点，除了 5th-generation Tensor Cores 之外，还有一个容易被忽略的改动：为了支撑 Multi Frame Generation 更复杂的节奏控制，Flip Metering 被从 CPU-based pacing 下沉到了 display engine，Blackwell 还提高了显示引擎的 pixel processing capability，以支持更高分辨率和刷新率下的硬件节奏控制。换句话说，DLSS 4/4.5 的多帧生成，不只是模型升级，还涉及 **AI 推理吞吐 + 显示时序硬件 + 帧 pacing 调度** 一起升级。[NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)；[NVIDIA Blackwell GeForce RTX 50 发布，2025-01-06](https://nvidianews.nvidia.com/news/nvidia-blackwell-geforce-rtx-50-series-opens-new-world-of-ai-computer-graphics)

## 截至 2026 年 3 月，DLSS 5 把路线又往前推了一步

截至 `2026-03-19`，NVIDIA 官方最新公开节点已经是 DLSS 5。但要理解它，仍然要先从 DLSS 4.5 的三条主线往前看：

- second-generation transformer Super Resolution
- Dynamic Multi Frame Generation
- 6X Multi Frame Generation

而且 NVIDIA 在后续 `2026-03-10` 的 GDC 更新中，又确认 Dynamic MFG 和 6X MFG 将在 `2026-03-31` 通过 NVIDIA app opt-in beta 面向 RTX 50 系列开放。[NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)；[NVIDIA GDC 2026 更新，2026-03-10](https://www.nvidia.com/en-us/geforce/news/gdc-2026-nvidia-geforce-rtx-announcements/)

这组信息说明，DLSS 的演化重点正在从“是否支持某个功能”转向“如何让整条 AI 渲染管线更稳定、更动态、更能适配不同刷新率和性能状态”。

这里还有一层常被忽略的软件现实。DLSS 4.5 之所以能被 NVIDIA 反复强调“向后兼容 existing integrations”，原因并不只是模型更强，而是 DLSS 从一开始就建立在 NGX / 驱动运行时可以持续更新模型和特性实现的机制之上。也就是说，今天很多玩家感知到的“驱动一更新，DLSS 模型又变了”，并不是偶然福利，而是这套系统从设计上就给自己留出的演进接口。[NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)

而 `2026-03-16` 公布的 DLSS 5 又把这条路线往前推了一层。NVIDIA 官方对它的定义已经不是单纯的 super resolution 或 frame generation，而是 real-time neural rendering model：它以每帧的 color 和 motion vectors 为输入，为场景注入 photoreal lighting and materials，并强调输出必须 deterministic、temporally stable，而且 anchored to source 3D content。这说明 DLSS 的演进已经从“重建更高分辨率的像素”走向“在保持游戏可控性的前提下，直接提升实时画面的真实感”。[NVIDIA DLSS 5，2026-03-16](https://www.nvidia.com/en-us/geforce/news/dlss5-breakthrough-in-visual-fidelity-for-games/)

## 用伪代码看今天的 DLSS

```cpp
// simplified neural rendering pipeline
BaseFrame = DLSSSuperResolution(
    lowResFrame,
    history,
    motionVectors,
    depth
);

Lighting = RayReconstruction(
    noisyRayTracedBuffers,
    history,
    motionVectors
);

if (frameGenerationEnabled) {
    GeneratedFrame = FrameGeneration(
        previousDisplayedFrame,
        BaseFrame,
        opticalFlow,
        motionVectors
    );
}

Present(BestAvailableFrame(BaseFrame, GeneratedFrame, Lighting));
```

这段伪代码真正想说明的是：DLSS 今天已经不再只是末端滤镜。它处在渲染链的中层，甚至部分进入了光照重建和显示调度层。它不只是“把已经算完的东西变好看”，而是在帮助系统决定“哪些东西值得原生算，哪些东西可以交给模型补”。

## 常见误解

### 误解一：DLSS 3 之后只是“多了个补帧开关”

不是。补帧会迫使整个系统重新考虑光流、运动矢量、延迟和显示节奏；它不是在老图像上再套一层滤镜，而是改变了显示链路的组织方式。

### 误解二：Ray Reconstruction 只是更聪明的降噪

这个理解不算全错，但还是太窄。它真正重要的地方，在于 AI 开始进入光线追踪结果的中间重建过程，而不是只在结果图像的末端做修饰。

### 误解三：DLSS 4/4.5 的重点只是倍率更高

倍率只是结果层的表述。更重要的是，DLSS 4/4.5 把模型路线、时域稳定性、多帧生成策略和图像形成链路的协同一起推向了新阶段。

## 我的结论

DLSS 3 之后，DLSS 已经很难再被准确地叫做“超分技术”。更接近事实的说法是：它正在变成一套神经渲染组件库，负责重建分辨率、生成显示帧、重建光线信息，并协同管理最终输出体验。

> 从 DLSS 3 到 5，真正发生的不是“超分升级”，而是图形流水线里越来越多环节开始被模型接管，目标也从性能逐步扩展到视觉真实感本身。

## 参考资料

- [NVIDIA DLSS 3，2022-09-20](https://nvidianews.nvidia.com/news/nvidia-introduces-dlss-3-with-breakthrough-ai-powered-frame-generation-for-up-to-4x-performance)
- [Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)
- [NVIDIA Ada Architecture](https://www.nvidia.com/en-us/technologies/ada-architecture/)
- [NVIDIA DLSS 3.5，2023-09-21](https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-phantom-liberty-dlss-3-5-ray-reconstruction-game-ready-driver/)
- [NVIDIA DLSS 4，2025-01-06](https://www.nvidia.com/en-us/geforce/news/dlss4-multi-frame-generation-ray-tracing-rtx-games/)
- [NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)
- [NVIDIA Blackwell GeForce RTX 50 发布，2025-01-06](https://nvidianews.nvidia.com/news/nvidia-blackwell-geforce-rtx-50-series-opens-new-world-of-ai-computer-graphics)
- [NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)
- [NVIDIA GDC 2026 更新，2026-03-10](https://www.nvidia.com/en-us/geforce/news/gdc-2026-nvidia-geforce-rtx-announcements/)
- [NVIDIA DLSS 5，2026-03-16](https://www.nvidia.com/en-us/geforce/news/dlss5-breakthrough-in-visual-fidelity-for-games/)
- [NVIDIA Reflex](https://developer.nvidia.com/reflex)



