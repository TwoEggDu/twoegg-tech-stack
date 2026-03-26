+++
date = 2026-03-24
title = "Unity 渲染系统 01b｜Draw Call 是什么：CPU 每次向 GPU 发出什么请求"
description = "把 Draw Call 拆成它真正包含的内容，讲清楚 CPU 和 GPU 的协作模型、批处理的三种方式和各自的条件限制，以及 Frame Debugger 里看到的每一行对应什么。"
slug = "unity-rendering-01b-draw-call-and-batching"
weight = 250
featured = false
tags = ["Unity", "Rendering", "DrawCall", "Batching", "GPU", "Performance"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：Draw Call 不是"让 GPU 画一个东西"的神秘指令，而是 CPU 把一批顶点数据、一套材质状态、一组变换矩阵打包好，交给 GPU 驱动排队执行的一次工作请求。

上一篇追踪了 Mesh、Material、Texture 在管线里的数据路径。但那条路是从 GPU 视角看的——数据已经在 GPU 里了。这篇往前一步：

**这些数据是怎么从 CPU 送到 GPU 的，每次"送"是什么，以及为什么这个"送"的次数会直接影响性能。**

---

## CPU 和 GPU 是两个独立的处理器

先建立一个基础认知：CPU 和 GPU 不是同一个芯片，它们之间有一条总线（PCIe 或移动端的统一内存架构），数据传输有延迟，更重要的是——**它们是异步工作的**。

CPU 每帧在做：逻辑更新、物理模拟、动画、AI、准备渲染数据……

GPU 每帧在做：处理 CPU 提交的渲染命令队列，执行 Shader，光栅化，输出像素……

这两件事在不同的处理器上同时进行。CPU 把渲染命令写进一个**命令缓冲区（Command Buffer）**，GPU 驱动从另一端读取并执行。

---

## Draw Call 的内容

每次 Draw Call，CPU 告诉 GPU 驱动：

```
Draw Call = {
    顶点缓冲（Vertex Buffer）  → 从哪个 Mesh 取顶点数据
    索引缓冲（Index Buffer）   → 画哪些三角形
    Shader 程序               → 用哪个 Vertex Shader + Fragment Shader
    材质参数（Constant Buffer）→ _BaseColor、_Roughness 等浮点值
    贴图绑定（Texture Slots）  → _BaseMap、_NormalMap 等贴图资源
    渲染状态（Render State）   → 深度写入开关、混合模式、剔除方向
    变换矩阵（Transform）      → Model Matrix（物体的 TRS）
}
```

GPU 驱动收到这些之后，把 Shader 程序和状态设置好（这一步叫 **SetPass**），然后发出绘制指令，GPU 才开始真正执行顶点处理和光栅化。

---

## 为什么 Draw Call 数量影响性能

Draw Call 本身消耗的 GPU 时间很少——真正的瓶颈在 **CPU 侧的准备开销**和 **SetPass 的状态切换开销**。

**CPU 侧开销**：每次 Draw Call，CPU 要检查物体是否可见、收集渲染数据、验证 Shader 状态、提交命令……这些工作在 CPU 上串行执行。场景里有 2000 个物体就要做 2000 次，CPU 时间被大量消耗在"准备工作"上，而不是实际渲染逻辑。

**SetPass 开销**：如果相邻两个 Draw Call 使用不同的 Material，GPU 驱动需要重新设置 Shader 程序和渲染状态。这个切换操作在驱动层有不可忽视的开销，尤其在旧图形 API（OpenGL ES）上更明显。

**实际经验值**：移动端项目通常把每帧 Draw Call 数量控制在 100～200 以内；PC 端宽松一些，但也不是无限的。

---

## 批处理：把多个 Draw Call 合并成一个

批处理（Batching）的本质是：**把多个物体的数据合并，用一次 Draw Call 画完**。

Unity 提供三种批处理方式，条件和代价各不相同。

### 静态合批（Static Batching）

**条件**：物体标记为 Static，且使用相同的 Material。

**原理**：在构建时（或场景加载时），把所有符合条件的物体的顶点缓冲合并成一个大的顶点缓冲。运行时用一次 Draw Call 画完整个合并缓冲。

```
物体A（100个顶点）+ 物体B（200个顶点）+ 物体C（150个顶点）
    → 合并成一个 450 个顶点的大缓冲
    → 1 次 Draw Call
```

**代价**：合并后的顶点数据在内存里额外存一份（原始数据还在）。物体不能移动——标记为 Static 意味着 Transform 固定。场景里静态物体很多时，合并缓冲可能非常大。

### 动态合批（Dynamic Batching）

**条件**：物体使用相同 Material，顶点数少于 900（Unity 的限制），没有使用不兼容的 Shader 特性。

**原理**：每帧在 CPU 上把多个小物体的顶点数据临时合并，用一次 Draw Call 提交。

**代价**：每帧都要在 CPU 上做合并，有 CPU 开销。顶点数限制严格，复杂模型基本无法使用。在 URP 下默认关闭（因为 SRP Batcher 更高效）。

### GPU Instancing

**条件**：多个物体使用完全相同的 Mesh 和 Material（Shader 需要支持 Instancing）。

**原理**：只提交一次 Mesh 数据，但同时传入多个实例的 Transform 矩阵（和可选的每实例参数）。GPU 用一次 Draw Call 画出所有实例，每个实例用自己的矩阵做变换。

```
同一棵树模型 × 500 棵 → 1 次 Draw Call（传入 500 个 Transform 矩阵）
```

**代价**：所有实例必须用完全相同的 Mesh 和 Material。如果每棵树颜色略有不同，需要通过 `MaterialPropertyBlock` 传入每实例的颜色参数，而不是创建多个 Material（多个 Material 会破坏合批）。

### SRP Batcher（URP / HDRP 专属）

**条件**：Shader 声明了 `CBUFFER_START(UnityPerMaterial)` 块，将材质属性放进统一的常量缓冲区。

**原理**：SRP Batcher 不减少 Draw Call 数量，而是**减少 SetPass 开销**。它把每个 Material 的常量缓冲区缓存在 GPU 里，相邻 Draw Call 切换材质时，不需要重新上传所有参数，只需要切换常量缓冲区的绑定指针。

对于使用同一 Shader 的不同 Material，SRP Batcher 能把它们组成一个"兼容批次"，大幅减少状态切换开销。

**这是 URP 项目最主要的批处理优化手段**，比动态合批更适合现代项目。

---

## Frame Debugger 里的 Draw Call

打开 Frame Debugger（Window → Analysis → Frame Debugger → Enable），你会看到这一帧所有的渲染事件列表。

每一行代表一个事件：

```
▼ Camera.Render
  ▼ RenderLoop.Draw
    ▶ Draw Mesh（MeshRenderer）   ← 一次普通 Draw Call
    ▶ Draw Mesh（MeshRenderer）
    ▶ Draw Mesh Instanced          ← GPU Instancing 的 Draw Call
  ▼ SRP Batch
    ▶ RenderLoop.Draw [8]          ← SRP Batcher 合并的 8 个 Draw Call
```

点击某一行，右侧会显示这次 Draw Call 的详细信息：

- **Shader**：用的是哪个 Shader、哪个 Pass
- **Keywords**：当前激活的 Keyword 组合（对应哪个 Shader Variant）
- **Properties**：Material 的所有参数值（`_BaseColor`、`_Roughness` 等）
- **Why this draw call is not batched**：如果这个 Draw Call 本来可以合批但没有，Unity 会在这里说明原因

最后一项特别有用——常见原因包括：
- 和前一个物体使用了不同 Material
- 物体有 `MaterialPropertyBlock`（会打断 SRP Batcher）
- Mesh 有不同的顶点格式
- 物体超出了动态合批的顶点限制

---

## SetPass Call 和 Draw Call 的区别

Frame Debugger 里有时会看到 "SetPass Calls" 和 "Draw Calls" 两个计数，它们不一样：

**Draw Call**：每次让 GPU 绘制一批三角形的指令，数量等于场景里独立提交的绘制次数。

**SetPass Call**：每次切换 Shader 程序或渲染状态时发出的指令，数量等于材质状态发生切换的次数。

如果 100 个 Draw Call 都用同一个 Material，SetPass Call 可能只有 1 次——SRP Batcher 就是在优化这个。如果 100 个 Draw Call 每次都换 Material，SetPass Call 就是 100 次。

**性能优化时，SetPass Call 的数量通常比 Draw Call 更值得关注。**

---

## 和下一篇的关系

Frame Debugger 里每次 Draw Call 都有一个对应的"渲染目标（Render Target）"——这次绘制的结果写到哪里。大多数时候是屏幕缓冲区，但有时是中间缓冲区（比如深度贴图、G-Buffer）。

下一篇讲 Render Target 和帧缓冲区的结构——搞清楚这些之后，Frame Debugger 里的 RT 切换就能完全看懂了。
