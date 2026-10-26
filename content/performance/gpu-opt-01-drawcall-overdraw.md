---
title: "GPU 渲染优化 01｜Draw Call 与 Overdraw：移动端的合批策略与 Alpha 排序"
slug: "gpu-opt-01-drawcall-overdraw"
date: "2026-03-25"
lastmod: "2026-10-26"
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
> 版本与证据范围：本轮按 Unity 6（6000.0）公开资料修订批处理条件；GPU 隐藏面与深度优化受架构和绘制状态影响。本文未提供真机测量结果，以下工具步骤为待执行检查。

Draw Call 和 Overdraw 经常被并列提到，但它们是两个完全不同的问题：Draw Call 是 CPU 向 GPU 提交指令的开销，Overdraw 是 GPU 重复渲染同一像素的开销。优化方向不同，工具不同，在移动端的权重也不同。

---

## Draw Call 的代价在哪里

每次 Draw Call，CPU 需要：
1. 设置渲染状态（材质参数、Shader、混合模式）
2. 提交顶点缓冲、索引缓冲的绑定
3. 发出绘制指令，通知 GPU 开始处理

**这里首先关注 CPU 准备和提交绘制的代价**。Draw Call 多可能加重提交负担，但不能仅凭数量断言 GPU 正在等待；GPU 也要处理状态、几何和后续着色，需要用时间线判断瓶颈。

在移动端，这个代价比 PC 更明显：
- CPU 可用预算受设备、调度与热状态影响
- API、驱动和渲染状态的组织会改变提交成本，不能仅凭“驱动更轻量”推出开销更大

**Draw Call 数量需要和成本一起看**：批处理可能带来 CPU 构建或额外几何副本；手动合并还会改变剔除粒度。SRP Batcher 则主要降低状态准备成本，不要求把多个 Draw 合成一个。

---

## 三种合批策略

### 1. Static Batching（静态合批）

**适用条件**：不移动的物体，勾选 `Static` → `Batching Static`。

**原理**：构建时或调用静态批处理 API 时，将兼容对象的几何组织到共享缓冲。原对象仍保留并可分别剔除；实际 Draw 数量受可见对象、材质、pass 和批次限制影响，不能保证整个批次永远只有一次 Draw。

**优点**：预先组织几何后，可以减少部分运行时提交与状态开销；运行时创建批次本身仍有构建成本。

**代价**：
- 内存增加：每个静态物体的 Mesh 数据被复制一份到合并 Mesh 里。场景里 100 个相同的石头 → 内存里存 100 份顶点数据（不是共享）
- 原对象仍能独立剔除。这与在 DCC 或通过 `Mesh.CombineMeshes` 手动合成单个 Renderer 不同，后者按合并后的边界参与剔除

**移动端注意**：比较提交收益和几何副本成本。大量相同网格是否改用实例化、大网格是否分块，都要结合实际规模，不能仅按“小物件/建筑”分类。独立剔除行为见 [StaticBatchingUtility](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/StaticBatchingUtility.html)。

---

### 2. Dynamic Batching（动态合批）

**适用条件**：网格与 pass 需要满足动态批处理条件。Unity 文档列出 300 顶点和 900 顶点属性的限制，使用更多属性会降低可合批的顶点数；还要检查材质、光照与镜像缩放等条件，不能简化成“不同 Scale 一律不行”。

**原理**：每帧 CPU 把符合条件的多个小 Mesh 合并成一个临时 Mesh，一次 Draw Call 提交。

**优点**：无需预处理，动态物体也能合批。

**代价**：
- 每帧 CPU 合并有运行时代价，物体多时 CPU 消耗明显
- 顶点数限制严格（300 顶点），只适合非常简单的 Mesh
- 合并时要做坐标变换（变换到世界空间），CPU 有额外计算

**是否启用要测量**：CPU 顶点变换成本可能抵消节省的提交时间。检查当前 URP 设置和实际批次，不预设所有项目的默认值。具体条件见 [Unity 批处理说明](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html)。

---

### 3. GPU Instancing

**适用条件**：多个物体使用**相同 Mesh + 相同 Material**，但 Transform 或颜色不同。

**原理**：一次 Draw Call 绘制多个实例，每个实例的 Transform / 颜色等差异通过 Instance Buffer 传给 GPU。

**优点**：
- 在兼容的重复物体上减少逐物体提交；每次可用实例数量还受 API 与数据限制
- 每个实例可以有不同的 Transform、颜色、自定义属性
- 不使用动态批处理的 300 顶点门槛，但顶点处理与几何数据仍有成本

**代价**：
- 需要 Shader 支持 `#pragma multi_compile_instancing` 和 `UNITY_INSTANCING_BUFFER`
- 不同材质实例无法合批（即使同一 Mesh）
- 同一个 Renderer 的传统自动实例化与 SRP Batcher 有路径选择关系；不同对象可以在同一场景使用不同路径，显式实例绘制、BRG/GPU Resident Drawer 则需另按对应机制判断

**SRP Batcher vs GPU Instancing 的选择**：
- 同 Shader 变体、不同材质参数 → 可比较 **SRP Batcher**（减少 CPU 设置参数的开销，不合并 Draw Call）
- 同 Mesh + 同 Material，大量重复实例 → 用 **GPU Instancing**（真正合并 Draw Call）

---

## Overdraw：移动端比 PC 更值得关注

Overdraw 描述同一屏幕区域被重复处理的情况，具体统计可能按几何覆盖、片元执行或输出计数。透明混合中的多层贡献可能都是目标画面所需，不能把前面的计算一概视为浪费。

**为什么移动端 Overdraw 代价更高**：

重复覆盖可能增加片元着色、深度与混合工作。桌面与移动 GPU 的缓存、隐藏面和深度优化各有差异，不能概括成桌面总在着色后直接写显存、且带宽总是充裕。

部分移动 GPU 可以在条件允许时提前消除被遮挡片元，但 HSR、FPK 和 Early-Z 不是跨架构完全相同的机制；它们也不消除全部几何与提交成本。背景见 [TBDR 架构]({{< relref "rendering/hardware-02-tbdr.md" >}})。

**混合与 Alpha Test 会改变这些优化的适用条件**。是否能提前做深度测试或消除着色，要看 GPU、Shader 副作用、深度写入与绘制顺序。

---

## Alpha Test 的 Overdraw

使用 `clip()` / `discard` 时，是否保留该片元要等 Alpha 条件确定，这可能限制部分提前深度写入或隐藏面优化。不能据此断言所有 GPU 的 HSR 完全失效，或每一层都必然执行全部片元工作。

**植被是典型观察对象**：多层草片叠加时，覆盖面积、视角、密度和 Alpha 遮罩都会改变工作量。具体倍数需要说明计数口径并实际测量。

**移动端处理 Alpha Test 植被的策略**：

```
① 限制视角：摄像机角度不要太低，减少大量草片叠加
② 比较密度、草片形状和空白区域；单纯放大面片可能增加覆盖
③ Alpha to Coverage：在支持的 MSAA 路径中比较边缘质量和性能，不预设必然优于 clip()
④ 把 Alpha Test 物体的 RenderQueue 放在不透明物体之后：
   让 Depth Buffer 先被不透明物体填好，Alpha Test 物体至少能被深度裁掉一部分
```

---

## 半透明物体的排序

常规 Alpha Blend 透明材质通常关闭深度写入，并按从后往前组织绘制；这是常见渲染配置，透明 Shader 仍可配置 `ZWrite`。开启深度写入也不能自动解决透明层次。加法等其他混合方式的排序需求可能不同。

**为什么排序很重要**：

常规 Alpha Blend 的顺序错误会改变颜色与层次。Unity 的透明排序受队列、排序层、相机排序模式等影响，通常以 Renderer 的排序位置组织，不是逐像素解决几何关系。

**排序也有成本和表达边界**：对象级排序不能完整解决互相穿插或单个 Mesh 内部的透明顺序。拆分、调整表现结构或采用专用透明算法都有不同代价，应按目标效果选择。

**减少半透明物体数量是最有效的手段**：
- 按视觉需求比较 Alpha Test 与 Alpha Blend，不把前者当成必然更快的替代
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

### Capture 与硬件计数

用 RenderDoc 检查 pass、绘制范围、深度/混合状态；管线统计与硬件计数是否可用取决于 API、GPU、驱动和工具版本。若使用片元 invocation 与屏幕像素的比值，应明确统计区间、渲染分辨率、MSAA 与 helper invocation 等口径，不能把它直接叫做所有表面的精确覆盖倍数。

没有统一适用于所有项目的 1.5/2.5 阈值。固定画面条件，对照覆盖区域、GPU 时间与可用计数，判断改动是否减少了不必要的工作。

---

## 小结

- 先测 CPU 提交与 GPU 工作，再比较批处理；静态批处理保留逐对象剔除，手动合并需要另看边界。
- SRP Batcher 降低状态准备成本；传统自动实例化与其有优先级关系，不能概括成整个项目二选一。
- 不透明、Alpha Test 与混合路径的提前剔除条件因架构和状态而异。
- Alpha Test 植被是最常见的高 Overdraw 来源，策略是限制密度和视角，或改用 Alpha to Coverage
- 半透明排序是物体级别的，无法解决像素级穿插；减少半透明物体数量是最根本的手段
- Frame Debugger 看绘制组织，Scene View 定位覆盖区域；capture 与硬件计数按平台能力使用，性能结果需要重复采样。
