---
title: "DLSS 进化论 06｜硬件与驱动：DLSS 到底依赖显卡的哪些变化"
description: "把 DLSS 放回 GPU 硬件与驱动运行时的真实栈里，解释从 Turing 到 Blackwell 到底新增了哪些能力，以及驱动层究竟能更新什么、不能更新什么。"
slug: "dlss-evolution-06-hardware-and-driver-layer"
weight: 100
featured: false
tags:
  - DLSS
  - GPU Architecture
  - Driver
  - NGX
  - Blackwell
series: "DLSS 进化论"
---

## 这篇要回答什么

如果只把 DLSS 写成“一个算法”，那整件事会被写浅。因为 DLSS 从来都不是纯算法故事，它一直是一个 **硬件能力、驱动运行时、模型分发和引擎接入协议** 共同成立的系统。

换句话说，DLSS 每一代真正能做成什么，不只取决于神经网络本身，还取决于：

- 显卡上有没有足够合适的 AI 推理硬件。
- 有没有专门的光流、显示时序、光追辅助硬件。
- 驱动里是否带着可升级的运行时。
- 游戏有没有把运动矢量、深度、曝光、HUD 分层这些关键信号按契约喂给 DLSS。

这篇文章就专门回答两个硬核问题：

1. 从 `Turing -> Ampere -> Ada -> Blackwell`，显卡硬件到底变了什么，为什么这些变化会直接改变 DLSS 的上限。  
2. 驱动层和运行时到底能更新什么，又不能凭空替你补什么。

## 先看一个最重要的结构图

把 DLSS 放回真实栈里，大概是这样：

```text
Game Engine / Renderer
  ├─ color / depth / motion vectors / exposure / HUD policy
  ├─ Streamline or NGX API integration
  v
NGX / DLSS Runtime Contract
  ├─ feature discovery
  ├─ parameter marshaling
  ├─ model selection
  v
NVIDIA Graphics Driver
  ├─ NGX Core Runtime
  ├─ NGX Update Module
  ├─ scheduling / profiles / compatibility logic
  v
GPU Hardware
  ├─ Tensor Cores
  ├─ Optical Flow / vision blocks
  ├─ RT Cores
  ├─ display engine / flip metering
  v
Displayed Frame
```

这里最重要的一点是：**DLSS 从来不是只有一层。**

- 引擎负责提供正确输入。  
- 运行时负责发现能力、加载模型、调度推理。  
- 驱动负责分发和升级运行时。  
- GPU 硬件负责把这些模型和时序逻辑真正跑起来。

如果少了其中任何一层，DLSS 都会退化，甚至直接不可用。

## 一、如果只看硬件，DLSS 每一代到底踩到了哪些“新砖块”

### 1. Turing：第一次把 Tensor Core 和 RT Core 真正带进消费级实时图形

DLSS 的硬件起点不是“AI 突然很强了”，而是 `2018` 年的 Turing 把两类此前不属于传统游戏显卡主舞台的硬件，正式拉进了实时图形：

- RT Cores：负责加速光线追踪相关工作。
- Tensor Cores：负责 AI 训练与推理中的张量运算，在 GeForce 语境下最关键的是推理。

NVIDIA 在 `2018-08-13` 的 Turing 发布里写得很清楚：Turing 首次把实时光追、AI、模拟和光栅化融合成 hybrid rendering；同时推出了 NGX SDK，用预训练网络给应用提供 DLAA、denoising、resolution scaling 这类能力。[NVIDIA Turing 发布，2018-08-13](https://nvidianews.nvidia.com/news/nvidia-reinvents-computer-graphics-with-turing-architecture)

这一步的历史意义，不是“显卡多了一个 AI 单元”，而是：

- **AI 推理第一次被官方定义为实时图形流水线的一部分。**
- DLSS 这类特性从一开始就不是纯 shader 技巧，而是 Tensor Core 驱动的专门能力。
- NVIDIA 同时准备了 NGX 这套软件栈，说明它从一开始就打算把模型更新和能力分发做成运行时机制，而不是把权重硬编码死在游戏里。

你也可以把 Turing 的意义理解成：**DLSS 在 Turing 上成立，不是因为 Turing 能“更快渲染”，而是因为 Turing 首次让 GPU 具备了“边渲染边推理”的现实条件。**

### 2. Ampere：它不是 DLSS 版本分界线，但它大幅抬高了 AI 图形的吞吐上限

很多文章讲 DLSS 时会跳过 Ampere，因为大家更容易记住 `DLSS 2` 和 `DLSS 3` 这两个软件版本节点。但如果从硬件看，Ampere 很重要。

GeForce RTX 30 系列官方页面把它写得很直白：Ampere 是第 2 代 RTX 架构，带来 dedicated 2nd gen RT Cores 和 3rd gen Tensor Cores；在 3080 页面上，NVIDIA 还直接给出“2nd gen RT Cores, 2X throughput”“3rd gen Tensor Cores, up to 2X throughput”的表述。[RTX 3080 页面](https://www.nvidia.com/en-us/geforce/graphics-cards/30-series/rtx-3080/)

Ampere 架构页还强调了另一点：3rd-gen Tensor Cores 加入了 structural sparsity 的硬件支持，可以把推理吞吐再翻倍，并明确把 DLSS 列为 Tensor Cores 把 AI 带入图形的能力之一。[NVIDIA Ampere Architecture](https://www.nvidia.com/en-us/technologies/ampere-architecture/)

这对 DLSS 的意义是什么？

- 它没有创造一个像 Frame Generation 这样全新的模式边界。
- 但它显著提高了 DLSS 2 这类时域超分在高分辨率、高画质模式下的可承受性。
- 也就是说，Ampere 更像是 **把 DLSS 从“能跑”推向“更大规模、更高分辨率、更稳地跑”** 的那一代。

这点很容易被忽略。很多时候，一项技术的上限不是被“有没有新功能”决定的，而是被“每帧能在多少毫秒内把已有模型跑完”决定的。Ampere 在这里的贡献，就是把这个预算抬高了。

### 3. Ada：DLSS 3 真正依赖的不是一句“AI 更强”，而是新的 Optical Flow Accelerator

如果说 Ampere 是吞吐扩张，那么 Ada 才是真正的 **模式跃迁**。

NVIDIA 对 Ada 和 DLSS 3 的官方表述非常明确：DLSS 3 由 Ada 的 fourth-generation Tensor Cores 和新的 Optical Flow Accelerator 驱动。[NVIDIA Ada Architecture](https://www.nvidia.com/en-us/technologies/ada-architecture/)；[Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)

这件事必须讲细，因为这里恰好解释了为什么 DLSS 3 的 Frame Generation 不是简单的软件开关。

DLSS 3 的生成帧模型官方有四类输入：

- 当前帧和前一帧图像
- Ada Optical Flow Accelerator 生成的 optical flow field
- 来自游戏引擎的 motion vectors
- 深度等游戏数据

其中最关键的新增砖块，是 **Optical Flow Accelerator**。NVIDIA 官方解释得很清楚：OFA 能捕捉粒子、反射、阴影、光照等像素级运动信息，而这些信息往往不在游戏引擎自身的 motion vectors 里。[Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)

这就带来一个很硬核但很重要的结论：

> DLSS 3 的 Frame Generation 不是“DLSS 2 再跑快一点”，而是建立在新的硬件观测信号之上的新模式。

Ada 还有两项跟 DLSS 经常被一起提到、但经常没被讲清楚的变化：

- 4th-generation Tensor Cores 加入 FP8 等能力，官方称其推理性能相较上一代可到 4X。它们不仅服务 DLSS 3，也提高了更复杂 AI 模型进入实时图形的可行性。[NVIDIA Ada Architecture](https://www.nvidia.com/en-us/technologies/ada-architecture/)
- 3rd-generation RT Cores、Opacity Micromap、Displaced Micro-Mesh、SER，则主要降低复杂光追内容本身的成本。这些能力不直接“等于 DLSS”，但会显著改善 DLSS 经常搭配使用的重光追场景的整体预算。[NVIDIA Ada Architecture](https://www.nvidia.com/en-eu/geforce/ada-lovelace-architecture/)

所以 Ada 的真正意义是：**它不只是让 DLSS 更快，而是让“生成整帧”第一次在消费级游戏里变成了可交付的能力。**

### 4. Blackwell：Multi Frame Generation 真正依赖的是 Tensor、显示引擎和帧 pacing 硬件一起升级

Blackwell 是另一个容易被写浅的节点。很多人会把 DLSS 4 / 4.5 理解成“更多帧、更强模型”，但官方材料已经清楚表明，它背后是硬件和软件一起改了架构。

NVIDIA 在 `2025-01-06` 的 RTX 50 发布里把话说得很直接：Blackwell GeForce RTX 50 系列由 5th-generation Tensor Cores 和 4th-generation RT Cores 驱动，面向 neural rendering、neural shaders 和 DLSS 4。[NVIDIA Blackwell GeForce RTX 50 发布，2025-01-06](https://nvidianews.nvidia.com/news/nvidia-blackwell-geforce-rtx-50-series-opens-new-world-of-ai-computer-graphics)

但更关键的是同一天的 DLSS 4 文章。NVIDIA 在那篇文里说明了 Multi Frame Generation 为什么必须绑定 Blackwell：

- 每个传统渲染帧都要在几毫秒内跑完 Super Resolution、Ray Reconstruction 和 Multi Frame Generation 相关模型。
- 为了避免“生成越多反而越慢”，RTX 50 系列加入了 5th-generation Tensor Cores，官方说 AI processing performance 最高可到前代的 2.5X。
- DLSS 3 的 Frame Generation 使用 CPU-based pacing；而为了处理多帧生成的复杂时序，Blackwell 把 Flip Metering 硬件化，直接下沉到 display engine。
- Blackwell display engine 还把 pixel processing capability 提高到 2 倍，以支持更高分辨率和更高刷新率下的 hardware Flip Metering。[NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)

这组信息特别值得细讲，因为它说明：

- DLSS 4 的关键不只是“模型更聪明”。
- 它还需要 **AI 吞吐、显示节奏控制、显示引擎能力** 一起升级。
- 从 DLSS 3 的“一次插一帧”，走到 DLSS 4/4.5 的“多帧生成和动态倍率”，真正的难点不只是推理本身，还包括 **怎么把这些生成帧以稳定节奏交给显示器**。

这也是为什么 Blackwell 的升级不只是 Tensor Core 代际更迭，而是连 display engine 都被拉进了 DLSS 的讨论范围。

## 二、把四代硬件压成一张表

| 世代 | 官方硬件变化 | 对 DLSS 的真实影响 |
| --- | --- | --- |
| Turing / RTX 20 | 首次把 Tensor Cores + RT Cores + NGX 带进消费级实时图形 | DLSS 得以作为独立运行时能力成立 |
| Ampere / RTX 30 | 2nd-gen RT Cores、3rd-gen Tensor Cores、稀疏推理支持、吞吐提升 | 抬高 DLSS 2 类时域超分在更高分辨率下的预算上限 |
| Ada / RTX 40 | 4th-gen Tensor Cores、新 Optical Flow Accelerator、3rd-gen RT Cores、SER | 让 DLSS 3 Frame Generation 成为新模式，而不只是更快的 SR |
| Blackwell / RTX 50 | 5th-gen Tensor Cores、4th-gen RT Cores、hardware Flip Metering、增强 display engine | 让 DLSS 4/4.5 的 Multi Frame Generation 与动态倍率真正可落地 |

如果只记一句话，那就是：

> DLSS 每一代“能做什么”，本质上是由 GPU 愿意把哪些专门硬件借给神经渲染来决定的。

## 三、驱动层和运行时，到底能更新什么

很多玩家对驱动的理解，还停留在“修 bug、加 profile、提一点性能”。对 DLSS 来说，这个理解不够。因为 NVIDIA 从一开始就把 DLSS 放在 **NGX 运行时** 里，而不是完全静态链接进游戏。

NVIDIA 的 NGX Programming Guide 写得非常明确：

- NGX SDK：应用接入 AI features 的 API。  
- NGX Core Runtime：运行时模块，随支持 RTX 的 NVIDIA Graphics Driver 一起提供。  
- NGX Update Module：负责让集成了 NGX 的客户端持续使用最新特性版本。  

更关键的是，官方文档直接说：当某个 NGX feature 被更新时，NGX infrastructure 会把更新推给所有使用该 feature 的客户端。[NVIDIA NGX Programming Guide](https://docs.nvidia.com/ngx/programming-guide/index.html)

这意味着，DLSS 从设计上就不是“游戏发售那天集成了什么，以后永远只能那样”。它天然支持一部分 **运行时升级**。

### 驱动和运行时能更新的，主要是四类东西

#### 1. 运行时模块本身

因为 NGX Core Runtime 是跟着图形驱动走的，所以驱动可以更新：

- feature discovery 逻辑
- 参数校验和兼容层
- 某些调度与运行时行为
- 对新模型和新 feature ID 的支持

这也是为什么很多 DLSS 能力，官方都会把“新驱动”和“新 NVIDIA app 更新”一起发布。

#### 2. 模型权重与模型预设

这是最容易被玩家感知到的一层。NVIDIA 官方已经明确公开了通过 NVIDIA app 做 DLSS Override 的路径：

- 可以对 Super Resolution、Ray Reconstruction、DLAA 选择新的模型 preset。
- 可以对 Frame Generation 选择更新模型。
- 可以全局或按游戏覆盖。
- 这些覆盖建立在“游戏已经启用相关 DLSS feature”的前提上。[NVIDIA App Update Adds DLSS 4 Overrides，2025-01-30 附更新说明](https://www.nvidia.com/en-us/geforce/news/nvidia-app-update-dlss-overrides-and-more.html)；[NVIDIA DLSS 4.5 Game Ready Driver Update，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-geforce-game-ready-driver/)

`2026-01-06` 的 DLSS 4.5 官方文章又更进一步说明：

- 所有 GeForce RTX 用户都可以通过 NVIDIA app 把 400 多个游戏和应用升级到新的 DLSS 4.5 Super Resolution 模型。
- Dynamic Multi Frame Generation、6X Multi Frame Generation 和 2nd-gen transformer Super Resolution 都被设计成对 existing DLSS integrations backwards compatible。[NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)

这说明驱动/运行时确实可以在 **不要求每个游戏重做一轮深集成** 的情况下，升级一部分模型能力。

#### 3. 某些兼容模式和覆盖行为

NVIDIA 在 DLSS 4 发布时公开了三类 DLSS Override：

- Frame Generation override：在已支持 Frame Generation 的游戏里，为 RTX 50 开启 Multi Frame Generation。
- Model Presets override：为 RTX 40/50 和全体 RTX 用户切换最新 FG / SR / RR 模型。
- Super Resolution override：调整内部渲染分辨率策略，甚至切到 DLAA 或 Ultra Performance。  

这类能力的本质，是 **在稳定 API 契约之上替换实现**，而不是要求每款游戏都重新写一遍整合逻辑。[NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)

#### 4. 帧 pacing 与显示相关逻辑的一部分协同

这里要更谨慎一点。严格说，帧 pacing 不全是驱动决定的，它牵涉游戏、OS、显示链路和硬件显示引擎。但从 DLSS 4 的官方表述看，Blackwell 已经把 Flip Metering 的关键逻辑下沉到 display engine，而驱动/运行时需要参与调度这些硬件路径。[同上](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)

所以，一个更精确的说法是：

> 驱动不能凭空发明帧 pacing 硬件，但它可以更新和调度如何使用这些硬件。

## 四、驱动层不能更新什么

这一部分如果不写，文章就会变成厂商宣传稿。

### 1. 驱动不能凭空给没集成 DLSS 的游戏“造出”完整 DLSS

NVIDIA app 的官方操作说明里有个非常关键的前提：你要升级的 feature 必须已经在程序里激活，才能被 override。也就是说，override 的前提是 **程序已经有相应 DLSS feature hook**。[NVIDIA App Update Adds DLSS 4 Overrides](https://www.nvidia.com/en-us/geforce/news/nvidia-app-update-dlss-overrides-and-more.html)

所以驱动层能做的是：

- 替换模型
- 覆盖 preset
- 调整某些兼容路径

但它不能在完全没有 NGX / Streamline / DLSS 集成契约的情况下，凭空把整套功能塞进任意游戏。

### 2. 驱动不能凭空发明高质量的引擎输入

这点需要结合前面几篇一起看。

- DLSS 2.0 明确依赖 motion vectors 和 temporal feedback。[NVIDIA DLSS 2.0，2020-03-23](https://www.nvidia.com/en-us/geforce/news/nvidia-dlss-2-0-a-big-leap-in-ai-rendering/)
- DLSS 3 的 Frame Generation 依赖 optical flow、game motion vectors、depth 等输入。[Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)
- NGX 编程指南也明确说明，evaluate feature 时要把 color、albedo、normals、depth 等缓冲和参数传进去；传得越完整，feature 往往越受益。[NVIDIA NGX Programming Guide](https://docs.nvidia.com/ngx/programming-guide/index.html)

因此可以做出一个非常稳妥的工程判断：

> 驱动可以升级模型，但不能神奇地替游戏补出高质量 motion vectors、正确的深度、合理的曝光策略，或者正确的 HUD 分层策略。

如果游戏这些输入本来就脏，换更新的模型只会在一定范围内缓解问题，不可能无限兜底。

### 3. 驱动不能把不支持的硬件变成支持的硬件

这点看似废话，实际上很重要。

- DLSS 3 的 Frame Generation 与 Ada 的 Optical Flow Accelerator 硬绑定。  
- DLSS 4 / 4.5 的 Multi Frame Generation 与 Blackwell 的 5th-gen Tensor Cores、hardware Flip Metering、display engine 增强能力绑定。  

也就是说，驱动可以把某些 **已经兼容的 integration** 提升到更新模型，但它不能把“硬件上根本没有 OFA / 没有对应 display engine 能力”的 GPU 变成支持相应模式的设备。

## 五、一个更工程化的判断：哪些能力更像“驱动可升级”，哪些更像“必须重做接入”

你可以把 DLSS 能力大致分成两类。

### 更像“驱动 / 运行时可升级”的能力

- 同一 feature 下的新模型权重和 preset
- 同一 API 合约下的兼容行为改进
- 某些 per-game override 和 profile
- 对已有 DLSS integration 的向后兼容升级

### 更像“必须有引擎或游戏配合重做”的能力

- 新增 feature hook
- 新的 motion vector / depth / exposure 提供方式
- HUD / post-process / transparency 的分层策略
- 某些必须重新安排 render graph 或 frame boundary 的能力

这也是为什么有些游戏可以通过 NVIDIA app 很快吃到新 SR 模型，而另一些涉及更深链路变化的能力，则依然需要原生 in-game support。

## 六、为什么这件事决定了文章该怎么写

如果把 DLSS 写成“算法越来越聪明”，读者会漏掉一半真相。更完整的写法应该是：

- `Turing` 让 DLSS 成为可能。  
- `Ampere` 让 DLSS 在更大吞吐预算下更好跑。  
- `Ada` 让 DLSS 从重建像素跨进生成整帧。  
- `Blackwell` 则把 AI 推理、显示节奏控制和多帧生成一起拉进同一个硬件设计目标里。  

与此同时，驱动和 NGX 运行时又保证了另一件事：DLSS 不是一次性静态集成，而是一套能持续升级模型、runtime 和兼容逻辑的系统。但这个系统依然受硬件边界和引擎输入边界约束，它不是魔法。

## 我的结论

如果让我把这一篇压成一句话，我会这样写：

> DLSS 的进化，从来不是“一个算法越来越强”，而是 GPU 硬件、驱动运行时和游戏引擎之间的边界在被重新划分。到 DLSS 5，这种边界已经开始触及视觉真实感本身，而不只是性能与重建质量。

这也解释了为什么 DLSS 会从 1.0 走到今天。真正变化的，不只是模型，而是 **显卡开始愿意为神经渲染提供专门硬件，驱动开始愿意把模型更新做成运行时能力，而游戏开始愿意把更多图像生成职责交给推理链路。**

## 参考资料

- [NVIDIA Turing 发布，2018-08-13](https://nvidianews.nvidia.com/news/nvidia-reinvents-computer-graphics-with-turing-architecture)
- [NVIDIA Turing Whitepaper 入口，2018-09-14](https://www.nvidia.com/en-us/geforce/news/geforce-rtx-20-series-turing-architecture-whitepaper/)
- [NVIDIA Ampere Architecture](https://www.nvidia.com/en-us/technologies/ampere-architecture/)
- [RTX 3080 页面](https://www.nvidia.com/en-us/geforce/graphics-cards/30-series/rtx-3080/)
- [NVIDIA Ada Architecture](https://www.nvidia.com/en-us/technologies/ada-architecture/)
- [Introducing NVIDIA DLSS 3](https://www.nvidia.com/en-my/geforce/news/dlss3-ai-powered-neural-graphics-innovations/)
- [NVIDIA Blackwell GeForce RTX 50 发布，2025-01-06](https://nvidianews.nvidia.com/news/nvidia-blackwell-geforce-rtx-50-series-opens-new-world-of-ai-computer-graphics)
- [NVIDIA DLSS 4 技术文章，2025-01-06](https://www.nvidia.com/en-us/geforce/news/gfecnt/20251/dlss4-multi-frame-generation-ai-innovations/)
- [NVIDIA App Update Adds DLSS 4 Overrides](https://www.nvidia.com/en-us/geforce/news/nvidia-app-update-dlss-overrides-and-more.html)
- [NVIDIA DLSS 4.5，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-dynamic-multi-frame-gen-6x-2nd-gen-transformer-super-res/)
- [NVIDIA DLSS 5，2026-03-16](https://www.nvidia.com/en-us/geforce/news/dlss5-breakthrough-in-visual-fidelity-for-games/)
- [NVIDIA DLSS 4.5 Game Ready Driver Update，2026-01-06](https://www.nvidia.com/en-us/geforce/news/dlss-4-5-geforce-game-ready-driver/)
- [NVIDIA GDC 2026 更新，2026-03-10](https://www.nvidia.com/en-us/geforce/news/gdc-2026-nvidia-geforce-rtx-announcements/)
- [NVIDIA NGX Programming Guide](https://docs.nvidia.com/ngx/programming-guide/index.html)
- [NVIDIA DLSS 2.0，2020-03-23](https://www.nvidia.com/en-us/geforce/news/nvidia-dlss-2-0-a-big-leap-in-ai-rendering/)


