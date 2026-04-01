---
title: "渲染系统分档设计 02｜怎么评价一套渲染系统：GPU Time 之外的五维健康度"
slug: "rendering-tier-design-02-health-model"
date: "2026-04-01"
description: "把渲染系统从单点优化拉回到可量化的健康度面板：用 GPU、带宽、可见性剔除、Shader、几何五维建立通用指标和项目基线。"
tags:
  - "Rendering"
  - "Performance"
  - "GPU"
  - "Mobile"
  - "TBDR"
  - "Unity"
series: "渲染系统分档设计"
primary_series: "device-tiering"
series_order: 2
weight: 1910
---
只看 `GPU Time`，只能回答一件事：

`这一帧有没有超预算。`

但系统设计真正要回答的是另一件事：

`为什么超预算，以及是系统哪一层在耗。`

如果你只盯着帧时间，最后很容易出现这种局面：

- 看起来 GPU 没满，但画面一开特效就掉
- 看起来 Shader 不复杂，但一到大场景就不稳
- 看起来分辨率没变，带宽却一直在涨
- 看起来渲染链没改多少，实际却多了一堆中间 RT

所以我更建议把渲染系统的健康度拆成五维：

1. `GPU 总压力`
2. `外部带宽 / RT 压力`
3. `可见性剔除效率`
4. `Shader / Fragment 压力`
5. `几何 / Tiler 压力`

这五维加在一起，才构成一套可讨论、可回归、可比较的健康模型。

## 为什么不能只看 GPU Time

`GPU Time` 是结果，不是原因。

同样的 16.6ms，背后可能是完全不同的病因：

- 有的是带宽满了
- 有的是 Shader 太重
- 有的是 overdraw 太高
- 有的是几何和 tiler 先堵了
- 有的是 RT 链和 Resolve 太频繁

所以系统设计不能只问“快不快”，而要继续追问：

- 是哪一层快不起来
- 是哪类场景最容易触发
- 是哪一个档位最容易失控
- 是不是一加特性就失衡

这也是为什么我更愿意把它叫做“健康度模型”，而不是“性能分数”。

## 第一维：GPU 总压力

这一维回答的是最基本的问题：

`GPU 有没有接近满载。`

### 常用指标

- `GPU Time`
- `GPU Busy`
- `GPU_ACTIVE`
- 帧时间与目标帧预算的比值

### 怎么看

这一维的通用判断最简单：

- `GPU Time > 目标帧预算`，就是超了
- `GPU Busy` 长时间接近上限，说明已经进入饱和区
- 只在少数重场景爆掉，和全场景持续爆掉，不是一个问题

### 什么时候报警

最实用的做法，不是强行给全项目统一一个绝对值，而是先设一个经验范围：

- `80%` 以内，通常还保留一定余量
- `80% - 90%`，开始接近上限，要观察是否持续
- `90%` 以上并且持续出现在关键场景，基本就应该按 GPU Bound 处理

真正判断是否健康，必须结合项目自己的目标帧率和设备档位。

## 第二维：外部带宽 / RT 压力

这一维回答的是：

`是不是在搬数据，而不是在算画面。`

对于移动端和 TBDR 架构来说，这一维尤其关键，因为很多看起来“只是换了一个 RT”的操作，背后其实是一次完整的写回和读回。

### 常用指标

- `GPU Read Bytes`
- `GPU Write Bytes`
- `Tile Memory Load/Store`
- `External Bandwidth`
- 全屏 `Blit`、`CopyColor`、`CopyDepth`、`FinalBlit` 数量
- RT 分辨率、RT 格式、MSAA 情况

### 怎么看

这一维最好的判读方式，不是绝对带宽本身，而是**同场景对比**：

- 同一套内容，开启某个特性后带宽是否明显上升
- 同一条渲染链，是否突然多出中间 RT
- 同一档位，是否因为格式升级导致带宽翻倍

### 典型危险信号

- 多了一层全屏中间 RT，但画面收益不明显
- 开了 HDR，却没有真正利用 HDR 带来的视觉收益
- 为了某个局部效果，默认引入了整帧 `Blit`
- `Store/Load` 次数增多，但没有换来可见质量提升

这一维的细节可以继续回看 {{< relref "engine-notes/gpu-opt-02-bandwidth.md" >}} 和 {{< relref "engine-notes/unity-rendering-01c-render-target-and-framebuffer.md" >}}。

## 第三维：可见性剔除效率

这一维回答的是：

`本来可以不画的像素，有多少被真正挡掉了。`

这也是 TBDR 或任何依赖 Early-Z / HSR 的系统里最容易被忽略的一层。

### 常用指标

- `HSR` / `Early-Z` / `LRZ` 相关命中率
- 不透明物体 overdraw
- 透明物体 overdraw
- `discard` / `clip` / alpha test 的使用密度
- 需要前置深度的比例

### 怎么看

如果这一维差，通常意味着两种情况：

1. 画面本来就重，且大量像素没被提前挡掉
2. 设计上把原本能剔除的像素又重新暴露给 fragment 了

最常见的破坏因素包括：

- 透明层过多
- 排序不好
- `discard` 太多
- 过多的半透明粒子
- 过于复杂的 Alpha 测试材质

### 经验判断

这一维不适合只看一个数字，更适合看趋势：

- `overdraw` 持续上升
- 关键场景里透明层级越来越多
- 加了特性以后，剔除效率明显下降

更详细的 overdraw 和排序问题，可以回看 {{< relref "engine-notes/gpu-opt-01-drawcall-overdraw.md" >}}。

## 第四维：Shader / Fragment 压力

这一维回答的是：

`留下来真正要跑的 fragment，到底贵不贵。`

### 常用指标

- `Fragment Invocations`
- `Shader Cycles`
- `Total Cycles`
- `ALU Limiter`
- `Texture Sample Limiter`
- 寄存器压力

### 怎么看

这一维分两种问题：

1. `单次贵不贵`
2. `被调用了多少次`

真正的系统成本，往往是两者相乘的结果。

### 经验区间

静态编译分析和运行时指标要一起看：

- `Total Cycles < 1.0`，通常可以视为轻量
- `1.0 - 2.0`，通常还能接受
- `> 3.0`，就应该认真拆原因

但这不是最终结论。一个“中等复杂”的 shader，如果全屏执行、再叠 overdraw，实际代价一样会非常高。

### 典型危险信号

- 全屏后处理 shader 持续占用较高 cycles
- 采样次数不高，但 ALU 很重
- 采样重和 ALU 重同时出现
- 只在高屏占比材质上爆发

这一维的判读，可以继续看 {{< relref "engine-notes/gpu-opt-03-shader.md" >}} 和 {{< relref "engine-notes/mobile-tool-03-mali-debugger.md" >}}。

## 第五维：几何 / Tiler 压力

这一维回答的是：

`是不是在送太多几何，或者 tile 分发本身已经很重。`

### 常用指标

- `Input Primitives`
- `Vertex Invocations`
- `Tiler load`
- `DrawCall` 数量
- 粒子、草、碎 mesh、UI 分层复杂度

### 怎么看

很多项目一开始只看 fragment，忽略了几何侧，结果会出现：

- 场景还没进入重后处理，GPU 就已经在几何侧吃紧
- 草海、UI、碎片、群体角色一多，帧率先掉
- 后端 shader 还没成为主因，tiler 先满了

所以这一维的判断，不能只看某一个 mesh，而要看整条场景构造：

- 有没有过碎的几何
- 有没有过多的同屏对象
- 有没有不必要的多相机叠加
- 有没有把场景组织成了 tile 不友好的形态

更具体的几何和 draw call 关系，可以回看 {{< relref "engine-notes/unity-rendering-01b-draw-call-and-batching.md" >}} 和 {{< relref "engine-notes/gpu-opt-07-instancing-deep.md" >}}。

## 三层口径：通用阈值、经验区间、项目 baseline

如果你想把这套模型真正落地，不能只给一个结论，必须分三层写：

### 1. 通用阈值

适合直接用来报警的条件。

典型例子：

- `GPU Time > 目标帧预算`
- `GPU Busy` 持续接近上限
- 某个关键 shader 的 `Total Cycles > 3`
- 原本不该出现的整帧 `Blit` 突然出现

### 2. 经验区间

适合做健康预警，而不是一票否决。

典型例子：

- `GPU Busy` 长时间落在 `80% - 90%`
- `Total Cycles` 落在 `1 - 2` 之间，通常可接受
- 某个特性开启后，带宽和 RT 数量明显抬升

### 3. 项目 baseline

这是最重要的一层。

因为不同项目、不同档位、不同设备，绝对值都不一样。真正有意义的判断，不是“这个数好不好看”，而是：

- 同一个场景，版本 A 和版本 B 差多少
- 同一台设备，热机前和热机后差多少
- 同一档位，开某个特性前后差多少

所以最稳的做法，是给每个档位、每类黄金场景都建立自己的 baseline。

这一步通常不是靠口头经验完成的，而是要和一套固定的检测链一起走，参考 {{< relref "engine-notes/mobile-tool-06-read-gpu-counter.md" >}} 和 {{< relref "code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md" >}}。前者负责把 counter 读对，后者负责把 baseline 和回归边界钉住。

## 一张健康度表应该怎么读

你可以把健康度面板做成这样的结构：

| 维度 | 看什么 | 过线时先怀疑什么 |
|---|---|---|
| GPU 总压力 | GPU Time / Busy / Active | 是否已经整体满载 |
| 带宽 / RT 压力 | Load/Store / Read/Write Bytes | 是否多了中间 RT 或大格式 RT |
| 剔除效率 | HSR / Early-Z / overdraw | 是否透明层、discard、排序破坏了剔除 |
| Shader 压力 | Cycles / ALU / Texture Limiter | 是否 shader 本体太重或调用次数太多 |
| 几何 / Tiler | Primitives / Vertex / Tiler load | 是否几何过碎或场景组织不友好 |

这张表的价值在于，它能让团队把问题先分类，再进入具体优化。

## 不同 GPU 厂商工具里具体看什么

上面这张健康度表解决的是“该按什么维度看”，还差最后一步：**到了不同 GPU 厂商的工具里，分别该点开哪些 counter。**

更稳的做法不是强行找“完全同名”的指标，而是先按五维找“语义等价”的替代项。可以先用下面这张对照表落地：

| 五维 | Apple（Xcode GPU Capture） | Mali（Streamline / Mali Graphics Debugger） | Adreno（Snapdragon Profiler） |
|---|---|---|---|
| GPU 总压力 | `GPU Time / Frame` | `GPU_ACTIVE` | `GPU Busy %` |
| 带宽 / RT 压力 | `GPU Read Bytes` / `GPU Write Bytes` | `EXTERNAL_READ_BYTES` / `EXTERNAL_WRITE_BYTES` | `AXI Read Bytes` / `AXI Write Bytes` |
| 剔除效率 | `HSR Efficiency` | `FRAG_QUADS_EZS_KILLED` | `LRZ Kill Rate` |
| Shader / Fragment 压力 | `ALU Limiter` / `Texture Sample Limiter` / `Fragment Invocations` | `FRAG_SHADER_ALU_UTIL` / `FRAG_SHADER_LS_UTIL` / `FRAG_QUADS_RAST` | `Shader ALU %` / `Texture Filter` / `Fragment Invocations` |
| 几何 / Tiler 压力 | `Vertex Invocations` | `PRIM_RAST` | `Vertex Primitives` |

如果你在某个平台上拿不到完全等价的项，优先保持这套判断顺序不变：

- 先确认 GPU 是否整体满载
- 再确认是不是 RT / 带宽在涨
- 再看 Early-Z / HSR / LRZ 是否失效
- 再区分是 Shader 本体重，还是 Fragment 调用次数太高
- 最后再看几何和 tiler 是否成了独立瓶颈

更细的 counter 读法可以回看 {{< relref "engine-notes/mobile-tool-06-read-gpu-counter.md" >}}。如果你已经知道自己当前只看某一类 GPU，也可以分别继续看 {{< relref "engine-notes/mobile-tool-05-xcode-gpu-capture.md" >}}、{{< relref "engine-notes/mobile-tool-03-mali-debugger.md" >}} 和 {{< relref "engine-notes/mobile-tool-04-snapdragon-profiler.md" >}}。

## 这套模型怎么接回分档设计

健康度模型不是为了多一个 dashboard，而是为了让分档设计有依据。

它至少会直接影响四件事：

- `01` 里写的体验合同是否真的可达
- `03` 里渲染链是否多了不必要的 RT 和 Pass
- `04` 里哪些特性应该留给高档
- `05` 里哪些 shader 和变体该被门禁拦下

换句话说，健康度模型是分档系统的测量层，合同是目标层，渲染链和特性矩阵是实现层。

这三层要对齐，系统才算设计完成。

## 下一步该做什么

这篇文章解决的是“怎么看”的问题。

下一篇要解决的是“怎么组织”的问题：

- Camera 应该怎么排
- Pass 应该怎么拆
- RT 和中间结果应该怎么放
- 哪些中间层保留，哪些应当丢掉

那是渲染链设计本身的工作。

## 系列内导航

- 上一篇：[渲染系统分档设计 01｜先定体验合同和预算合同：低档保什么，高档加什么]({{< relref "engine-notes/rendering-tier-design-01-contracts.md" >}})
- 下一篇：[渲染系统分档设计 03｜渲染链怎么设计：Camera、Pass、RT 与中间结果的组织原则]({{< relref "engine-notes/rendering-tier-design-03-pipeline-structure.md" >}})
- 回到入口：[机型分档专题入口｜先定分档依据，再接配置、内容、线上治理与验证]({{< relref "engine-notes/device-tiering-series-index.md" >}})
