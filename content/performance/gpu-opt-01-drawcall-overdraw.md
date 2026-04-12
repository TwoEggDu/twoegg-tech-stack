---
title: "GPU 渲染优化 01｜Draw Call 与 Overdraw：移动端的合批策略与 Alpha 排序"
slug: "gpu-opt-01-drawcall-overdraw"
date: "2026-03-25"
description: "Draw Call 和 Overdraw 是移动端 GPU 性能的两个常见瓶颈，但它们的优化方向截然不同。本篇从移动端视角讲清楚合批的三种策略和各自的适用条件，以及 Alpha 排序为什么在 TBDR 上格外重要、怎么用工具量化 Overdraw。"
tags:
  - "移动端"
  - "GPU"
  - "Draw Call"
  - "Overdraw"
  - "合批"
  - "性能优化"
  - "URP"
series: "移动端硬件与优化"
weight: 2210
---
Draw Call 和 Overdraw 经常被并列提到，但它们是两个完全不同的问题：Draw Call 是 CPU 向 GPU 提交指令的开销，Overdraw 是 GPU 重复渲染同一像素的开销。优化方向不同，工具不同，在移动端的权重也不同。

---

## Draw Call 的代价在哪里

每次 Draw Call，CPU 需要：
1. 设置渲染状态（材质参数、Shader、混合模式）
2. 提交顶点缓冲、索引缓冲的绑定
3. 发出绘制指令，通知 GPU 开始处理

**代价的主体在 CPU 侧**，不在 GPU。Draw Call 多意味着 CPU 每帧花在提交指令上的时间多，GPU 等待指令的间隙多（Pipeline stall）。

在移动端，这个代价比 PC 更明显：
- 移动端 CPU 单核性能弱，处理每次 Draw Call 的固定开销更高
- GPU 驱动层也更轻量，状态切换的 overhead 更大

**但 Draw Call 不是越少越好**：减少 Draw Call 的代价是合批——要么合并 Mesh（增加顶点数），要么用 GPU Instancing（增加 Draw 参数）。合批本身有 CPU 构建代价和内存代价，需要权衡。

---

## 三种合批策略

### 1. Static Batching（静态合批）

**适用条件**：不移动的物体，勾选 `Static` → `Batching Static`。

**原理**：Build 时或运行时把多个静态 Mesh 合并成一个大 Mesh，存在内存里。运行时一次 Draw Call 绘制整个合并 Mesh。

**优点**：运行时零 CPU 合并代价，Draw Call 减少效果最好。

**代价**：
- 内存增加：每个静态物体的 Mesh 数据被复制一份到合并 Mesh 里。场景里 100 个相同的石头 → 内存里存 100 份顶点数据（不是共享）
- 合并后的 Mesh 做视锥裁剪时是整体裁剪，局部在视野外也不会剔除单个子 Mesh

**移动端注意**：静态合批适合顶点数少的小物件（石头、路灯、草丛装饰），不适合顶点数多的大型静态物体（建筑），否则内存代价过高。

---

### 2. Dynamic Batching（动态合批）

**适用条件**：顶点数 ≤ 300（URP 默认限制），使用相同材质，不能有不同 Scale。

**原理**：每帧 CPU 把符合条件的多个小 Mesh 合并成一个临时 Mesh，一次 Draw Call 提交。

**优点**：无需预处理，动态物体也能合批。

**代价**：
- 每帧 CPU 合并有运行时代价，物体多时 CPU 消耗明显
- 顶点数限制严格（300 顶点），只适合非常简单的 Mesh
- 合并时要做坐标变换（变换到世界空间），CPU 有额外计算

**移动端实际价值有限**：URP 默认关闭 Dynamic Batching（推荐用 SRP Batcher 替代）。对于粒子、UI 之外的 3D 物体，动态合批的条件苛刻，实际能合上的场景不多。

---

### 3. GPU Instancing

**适用条件**：多个物体使用**相同 Mesh + 相同 Material**，但 Transform 或颜色不同。

**原理**：一次 Draw Call 绘制多个实例，每个实例的 Transform / 颜色等差异通过 Instance Buffer 传给 GPU。

**优点**：
- 对 CPU 最友好——一次 Draw Call 绘制几百个实例
- 每个实例可以有不同的 Transform、颜色、自定义属性
- 不受顶点数限制

**代价**：
- 需要 Shader 支持 `#pragma multi_compile_instancing` 和 `UNITY_INSTANCING_BUFFER`
- 不同材质实例无法合批（即使同一 Mesh）
- 与 SRP Batcher 不能同时工作（二选一）

**SRP Batcher vs GPU Instancing 的选择**：
- 同 Shader、不同材质参数 → 用 **SRP Batcher**（减少 CPU 设置参数的开销，不合并 Draw Call 但减少状态切换）
- 同 Mesh + 同 Material，大量重复实例 → 用 **GPU Instancing**（真正合并 Draw Call）

---

## Overdraw：移动端比 PC 更值得关注

Overdraw 是指同一个像素被多个 Draw Call 的 Fragment Shader 写入多次。最终只有最顶层的结果留下，之前的计算全部浪费。

**为什么移动端 Overdraw 代价更高**：

PC 上桌面 GPU 是 IMR 架构，Fragment Shader 执行后写入显存，Overdraw 的代价是 Fragment Shader 时间 + 显存带宽。显存带宽充裕，代价主要来自 Fragment Shader。

移动端 TBDR 对**不透明物体**有 HSR/FPK 机制（见 [硬件-02](../hardware-02-tbdr/)），能自动剔除被遮挡的像素，不透明 Overdraw 在 TBDR 上代价很低。

**但透明物体和 Alpha Test 物体不适用 HSR**——这两类是移动端 Overdraw 的真正问题所在。

---

## Alpha Test 的 Overdraw

使用 `clip()` 或 `discard` 的 Alpha Test Shader 会让 TBDR 的 HSR 完全失效（原因见 [硬件-02](../hardware-02-tbdr/)）。大量 Alpha Test 物体叠加时，每一层都要执行完整的 Fragment Shader。

**植被是最常见的场景**：草地上多层草片叠加，每片草都用 Alpha Test 做镂空。俯视角或摄像机低角度时，Overdraw 倍数可以达到 6~8 倍。

**移动端处理 Alpha Test 植被的策略**：

```
① 限制视角：摄像机角度不要太低，减少大量草片叠加
② 减少草的密度：用更大的 Mesh 代替大量小片
③ Alpha to Coverage（MSAA 开启时）：利用 MSAA 采样做软边缘，比 clip() 更友好
④ 把 Alpha Test 物体的 RenderQueue 放在不透明物体之后：
   让 Depth Buffer 先被不透明物体填好，Alpha Test 物体至少能被深度裁掉一部分
```

---

## 半透明物体的排序

半透明物体（Blend 混合，Queue = Transparent）无法写入深度缓冲，必须从后往前渲染（Painter's Algorithm）才能保证混合结果正确。

**为什么排序很重要**：

如果半透明物体渲染顺序错误（前面的先画），混合结果在视觉上会出现错误（颜色穿透、层次混乱）。Unity 默认按到摄像机距离排序，但这个排序是**物体级别**的（按 Bounds 中心点），不是像素级别的。

**移动端排序的额外代价**：半透明物体每帧都需要排序，物体数量多时 CPU 排序本身有开销。并且排序无法解决**物体互相穿插**的情况（A 物体一部分在 B 前面，一部分在 B 后面），这种情况只能通过拆分 Mesh 解决。

**减少半透明物体数量是最有效的手段**：
- 粒子系统尽量用 Alpha Test 替代 Alpha Blend（视觉上能接受时）
- UI 层的全屏半透明叠加代价极高，尽量避免多层半透明 UI 同时显示
- 水面、玻璃等大面积半透明，优先考虑 Dithering / Screen Door Transparency 方案

---

## 用工具量化

### Frame Debugger 看 Draw Call 分布

**Window → Analysis → Frame Debugger**

展开每个 Pass，查看：
- 合批是否生效（SRP Batcher 合批后会显示 `SRP Batch` 标签）
- 哪些物体打断了合批（材质不同、Shader 不同）
- Draw Call 总数分布在哪些 Pass

---

### Scene View 的 Overdraw 模式

**Scene View → Draw Mode → Overdraw**

越白的区域 Overdraw 越高，可以直观看到半透明叠加和 Alpha Test 密集区域。这个模式是近似可视化，不是精确数值，但足够定位问题区域。

---

### RenderDoc 精确统计

Frame Capture 后，在 **Pipeline State → Fragment Shader** 里查看 Invocation Count（Fragment Shader 执行次数）。拿这个数字除以分辨率像素数，就是平均 Overdraw 倍数。

移动端目标：不透明物体 Overdraw < 1.5，透明物体 Overdraw < 2.5（超出这个范围需要排查）。

---

## 小结

- Draw Call 代价在 CPU 侧，合批策略按场景选择：静态合批适合不动的小物件，GPU Instancing 适合大量相同 Mesh 实例
- SRP Batcher 和 GPU Instancing 二选一，同 Shader 不同 Material 用 SRP Batcher，同 Mesh 同 Material 用 GPU Instancing
- TBDR 的 HSR 消灭了不透明物体的 Overdraw，移动端真正的 Overdraw 问题来自 Alpha Test 和半透明
- Alpha Test 植被是最常见的高 Overdraw 来源，策略是限制密度和视角，或改用 Alpha to Coverage
- 半透明排序是物体级别的，无法解决像素级穿插；减少半透明物体数量是最根本的手段
- 量化工具：Frame Debugger 看合批，Scene View Overdraw 模式定位问题区域，RenderDoc 统计精确 Invocation Count
