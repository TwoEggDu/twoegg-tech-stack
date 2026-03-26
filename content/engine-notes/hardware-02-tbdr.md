---
title: "移动端硬件 02｜TBDR 架构详解：Tile、On-Chip Buffer、HSR 如何改变渲染逻辑"
slug: "hardware-02-tbdr"
date: "2026-03-25"
description: "TBDR 是移动端 GPU 和桌面 GPU 最根本的架构差异。本篇从硬件层面讲清楚 Tile 怎么分、On-Chip Buffer 是什么、HSR 如何剔除被遮挡的像素、Bandwidth 代价模型，以及这套机制对渲染工程决策的实际影响。"
tags:
  - "移动端"
  - "GPU"
  - "TBDR"
  - "TBR"
  - "硬件架构"
  - "渲染管线"
  - "性能优化"
series: "移动端硬件与优化"
weight: 2020
---
移动端 GPU 几乎全都是 TBDR（Tile-Based Deferred Rendering）或 TBR（Tile-Based Rendering）架构，桌面 GPU 则普遍是 IMR（Immediate Mode Rendering）。这不是设计风格的差异，而是由物理约束决定的——移动端没有独立显存，没有风扇，功耗预算只有桌面的 1/10。这些约束导致了完全不同的渲染执行方式，也决定了移动端优化的底层逻辑。

---

## IMR：桌面 GPU 的直接执行方式

理解 TBDR 之前，先看 IMR 怎么工作。

IMR 的思路非常直接：每个 Draw Call 提交后立即执行，像素结果直接写入显存里的 Framebuffer。

```
Draw Call 1 → 光栅化 → 深度测试 → 写显存 Framebuffer
Draw Call 2 → 光栅化 → 深度测试 → 写显存 Framebuffer
Draw Call 3 → 光栅化 → 深度测试 → 写显存 Framebuffer
...
```

**优点**：逻辑简单，Draw Call 之间相互独立，GPU 随时可以开始执行下一个 Draw Call。

**代价**：每个像素可能被多个 Draw Call 写入多次（OverDraw），最终只有最上层的结果留下来，但前面写入的带宽全部浪费。深度测试也需要频繁读写显存里的深度缓冲。

桌面 GPU 有宽裕的显存带宽（几百 GB/s）和独立散热，这个代价可以接受。

---

## 为什么移动端不能用 IMR

移动端 GPU 和 CPU 共享系统内存（UMA，Unified Memory Architecture），没有独立的高速显存。系统内存带宽通常只有 25~50 GB/s，是桌面独显的 1/10 甚至更少。

如果直接用 IMR，每帧所有的像素写入、深度读写都要走这条窄带宽，很快成为瓶颈。更严重的是：每次读写系统内存都要把数据在内存和 GPU 核心之间搬运，消耗大量功耗，产生热量。

移动端 GPU 的解决方案：**把一帧的渲染工作分成小块（Tile），每个小块在 GPU 片上的高速缓冲（On-Chip Buffer）里完整地计算完，最后才把结果写回系统内存一次。**

这就是 TBR 的核心思想。

---

## TBR 的两阶段工作流程

### 第一阶段：Geometry（几何处理）

所有 Draw Call 的顶点处理先统一执行完，每个三角形被变换到屏幕空间后，记录它覆盖了哪些 Tile，生成一张"Tile 分配表"。

```
顶点变换（全场景）→ 裁剪 → 记录三角形覆盖的 Tile → 生成 Tile 分配表
```

这一阶段结果存在系统内存里（只是索引，数据量小）。

### 第二阶段：Rasterization（光栅化，按 Tile 执行）

GPU 逐个处理每个 Tile：

```
取出这个 Tile 对应的三角形列表
→ 在 On-Chip Buffer 里光栅化 + 深度测试 + 着色
→ 结果写回系统内存（一次 Store）
→ 处理下一个 Tile
```

**整个 Tile 的中间计算结果（颜色、深度、模板）全程在 On-Chip Buffer 里**，不需要读写系统内存，直到 Tile 完全计算完毕才执行一次 Store。

---

## On-Chip Buffer：片上缓冲的物理意义

On-Chip Buffer 是集成在 GPU 核心内部的高速 SRAM，延迟极低、带宽极高（走片上总线，不经过系统内存控制器），但容量很小（通常几百 KB 到几 MB）。

正因为容量小，才需要把屏幕分成 Tile——1080P 的深度缓冲需要约 8MB（32bit × 1920 × 1080），On-Chip Buffer 放不下；但一个 32×32 的 Tile 只需要 16KB，完全可以放进去。

**Tile 大小的权衡**：
- Tile 越小 → On-Chip Buffer 利用效率越高，但 Tile 切换次数更多（每次切换有固定开销）
- Tile 越大 → 切换少，但 On-Chip Buffer 可能放不下，导致溢出到系统内存

主流 Tile 大小：
- **Apple GPU**：16×16 像素
- **ARM Mali（Valhall）**：16×16 像素
- **Qualcomm Adreno**：具体实现未完全公开，行为类似

---

## TBDR 的核心：Deferred 深度测试与 HSR

TBR 是基础形态，TBDR 在其基础上增加了一个关键步骤：**在 Fragment Shader 执行之前，先做一次全 Tile 的可见性判断，把被遮挡的像素提前剔除。**

这个机制在不同厂商有不同名字：
- **Apple / Imagination PowerVR**：HSR（Hidden Surface Removal）
- **ARM Mali（Valhall）**：FPK（Forward Pixel Kill）
- **Qualcomm Adreno**：LRZ（Low Resolution Z）

### HSR 的工作原理

在光栅化阶段，GPU 对 Tile 内所有三角形先做深度排序，找出每个像素位置真正可见的那个三角形，**只对可见像素执行 Fragment Shader**：

```
光栅化所有三角形 → On-Chip 深度测试
→ 标记每个像素的可见三角形
→ 只执行可见像素的 Fragment Shader
→ 被遮挡的像素直接丢弃（零 Fragment Shader 代价）
```

**实际收益**：如果场景有 4 层不透明物体叠加（OverDraw = 4），IMR 执行 4 次 Fragment Shader；TBDR 只执行 1 次。Fragment Shader 通常是 GPU 最重的部分，这个优化在复杂场景里收益非常显著。

---

## Bandwidth 代价模型

### Load 和 Store

每个 Tile 开始处理时，需要从系统内存读取 RT 的当前内容（**Load**）。处理完成后结果写回系统内存（**Store**）。

```
系统内存 → On-Chip Buffer  （Load）
               ↓
           渲染计算（全程在 On-Chip Buffer）
               ↓
系统内存 ← On-Chip Buffer  （Store）
```

**Load/Store 是移动端带宽消耗的主要来源**，不是每个像素写入都走带宽，而是集中在 Tile 的切换边界。

### RT 切换的代价

每次切换渲染目标，GPU 必须：
1. **Store**：把当前 Tile 的 On-Chip Buffer 内容写回系统内存
2. **Load**：把新 RT 对应的内容从系统内存读进 On-Chip Buffer

一帧里如果有 10 次 RT 切换，每次都有 Load + Store，带宽压力显著增加。

这正是 URP **Native RenderPass** 在移动端价值所在——把相邻 Pass 合并成一个 Native RenderPass，中间的 Store + Load 直接省掉，数据全程留在 On-Chip Buffer。

### LoadAction 和 StoreAction

| 设置 | 含义 | 带宽影响 |
|------|------|---------|
| `LoadAction.Load` | 从系统内存读取 RT 旧内容 | 有 Load 带宽代价 |
| `LoadAction.Clear` | 直接在 On-Chip Buffer 里填清除色，不读系统内存 | **省掉 Load** |
| `LoadAction.DontCare` | 不关心旧内容（内容未定义）| **省掉 Load** |
| `StoreAction.Store` | 把结果写回系统内存 | 有 Store 带宽代价 |
| `StoreAction.DontCare` | 结果不保留（丢弃）| **省掉 Store** |

**实际应用**：Shadow Map 每帧重新绘制，不需要上一帧的深度内容 → 用 `LoadAction.Clear` 省掉 Load；深度缓冲在最终输出后不再需要 → 用 `StoreAction.DontCare` 省掉 Store。

---

## HSR 的失效条件

### discard / clip 打断 HSR

```hlsl
// 这行代码让 GPU 无法在 Fragment Shader 之前判断可见性
clip(alpha - 0.5);
```

HSR 的前提是在 Fragment Shader 执行**之前**就能确定可见性。`discard` / `clip` 使可见性取决于 Fragment Shader 的执行结果，GPU 只能执行完再决定是否丢弃，HSR 对这些像素完全失效。

植被、栅栏、头发大量使用 Alpha Test，这是移动端植被渲染代价偏高的根本原因之一。

### 透明物体

透明物体需要按从后往前的顺序混合，无法提前判断可见性，HSR 不适用。透明物体的 OverDraw 在 TBDR 和 IMR 上代价相同。

### Fragment Shader 修改深度

如果 Fragment Shader 输出 `SV_Depth` 修改深度值，GPU 同样无法提前判断，HSR 失效。

---

## TBR vs TBDR 的区别

| | TBR | TBDR |
|--|-----|------|
| 代表厂商 | 部分早期移动 GPU | Apple GPU、ARM Mali Valhall、PowerVR、现代 Adreno |
| Tile 处理 | 有 | 有 |
| Fragment Shader 前可见性判断 | 无或有限 | 有（HSR/FPK/LRZ）|
| 不透明 OverDraw 代价 | 较高 | 低（几乎零）|

现代 Qualcomm Adreno 的 LRZ 本质上也是延迟深度测试，行为接近 TBDR，通常一并归入 TBDR 阵营讨论。

---

## 对渲染工程决策的实际影响

理解 TBDR 架构后，移动端常见"最佳实践"都有了原理依据：

| 实践 | TBDR 角度的原因 |
|------|---------------|
| 开启 Native RenderPass | 减少 RT 切换，中间结果留在 On-Chip Buffer，省 Store/Load |
| 不透明物体从前往后排序 | 提前写入深度，帮助 HSR 尽早剔除后面的物体 |
| 避免 `discard/clip`（能不用就不用）| 保持 HSR 有效，避免不必要的 Fragment Shader 执行 |
| Shadow Map 用 `DontCare` Store | 深度结果只用于当帧，不需要写回系统内存 |
| 减少不必要的 MRT | 每张额外 RT 都占 On-Chip Buffer 空间，超出容量溢出到系统内存 |
| MSAA 在移动端代价低于 PC | MSAA 样本存在 On-Chip Buffer，Resolve 不走系统内存 |
| Framebuffer Fetch（iOS Metal）| 直接读当前 Tile 的 On-Chip 颜色值，完全不走系统内存 |

---

## 小结

- TBDR 是由移动端物理约束（低带宽、UMA、低功耗）驱动的架构选择
- 两阶段流程：全场景几何处理 → 按 Tile 光栅化，中间结果在 On-Chip Buffer
- On-Chip Buffer：片上高速 SRAM，容量小但速度极快，是 TBDR 省带宽的关键
- HSR/FPK/LRZ：Fragment Shader 前剔除不可见像素，消灭不透明物体 OverDraw
- 带宽代价集中在 Load/Store：减少 RT 切换是移动端带宽优化的核心手段
- `LoadAction.Clear` 和 `StoreAction.DontCare` 是最简单的带宽优化手段
- HSR 失效条件：`discard/clip`、透明物体、Fragment Shader 修改深度

下一篇：移动端硬件-03，移动端功耗与发热——为什么帧率稳定比峰值帧率更重要，热降频的机制与工程应对策略。
