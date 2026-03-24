+++
title = "Unity 渲染系统 01f｜RenderDoc 进阶：顶点数据、贴图采样、Pipeline State 调试"
description = "深入讲解 RenderDoc 的 Mesh Viewer、Texture Viewer 高级功能和 Pipeline State 各项参数，覆盖顶点缓冲验证、mip 采样检查、Blend/Depth/Stencil State 读法，以及 Shader Debugger 逐像素追踪的基本用法。"
slug = "unity-rendering-01f-renderdoc-advanced"
weight = 450
featured = false
tags = ["Unity", "Rendering", "RenderDoc", "Debugging", "GPU", "Shader", "MeshViewer"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：当 Frame Debugger 告诉你"哪里出问题了"之后，RenderDoc 告诉你"GPU 收到的原始数据到底是什么"——Mesh Viewer 看顶点，Texture Viewer 看贴图采样，Pipeline State 看渲染状态，Shader Debugger 追踪单个像素的计算过程。

上一篇把 RenderDoc 的界面和基本捕获流程讲清了。这篇进入实际排查问题时最常用的几个功能。

---

## Mesh Viewer：验证顶点数据

**打开方式**：选中一个 `DrawIndexed` 事件后，菜单 Window → Mesh Viewer，或点击 Pipeline State 面板里 VS 阶段的 Input 链接。

### 界面结构

Mesh Viewer 分成两个区域：

**左侧表格（顶点数据）**：逐顶点列出每个属性的值：

```
Vertex | POSITION          | TEXCOORD0    | NORMAL           | TANGENT
-------|-------------------|--------------|------------------|------------------
0      | (0.5, 0.0, -0.5)  | (1.0, 0.0)   | (0.0, 1.0, 0.0)  | (1.0, 0.0, 0.0, 1.0)
1      | (-0.5, 0.0, -0.5) | (0.0, 0.0)   | (0.0, 1.0, 0.0)  | (1.0, 0.0, 0.0, 1.0)
2      | (-0.5, 0.0, 0.5)  | (0.0, 1.0)   | (0.0, 1.0, 0.0)  | (1.0, 0.0, 0.0, 1.0)
...
```

**右侧 3D 预览**：把顶点数据可视化成网格，可以旋转查看。有两个视图可以切换：
- **VS Input**：Vertex Shader 执行前的原始顶点数据（模型空间）
- **VS Output**：Vertex Shader 执行后的数据（裁剪空间/NDC），通常是变换后的位置

### 用 Mesh Viewer 能排查什么

**顶点位置错误**：模型在屏幕上位置不对，但 Transform 看起来正确——有时是 Mesh 本身的 Pivot 偏移，或者骨骼蒙皮数据错误。在 VS Input 里查看原始 Position 值能直接确认。

**UV 错误**：贴图拉伸、接缝、位置偏移——在表格里找 TEXCOORD0 列，检查 UV 值是否在合理范围（通常 0～1），以及分布是否符合预期。

**法线错误**：光照计算异常（某个面莫名地偏暗或偏亮）——检查 NORMAL 列，法线向量应该是单位向量（长度为 1），方向是否朝外（法线方向和面朝向相反时会出现异常光照）。

**Tangent w 分量**：法线贴图方向不对（表面凹凸感反转）——Tangent 的 w 分量（第 4 个值）应该是 +1 或 -1，决定了副切线的方向。如果模型导入时手性处理不对，w 分量会有问题，导致法线贴图在某些面上方向反转。

### VS Input vs VS Output 的对比用法

同时看 VS Input 和 VS Output，可以验证 Vertex Shader 的变换是否正确：

```
VS Input  POSITION: (0.5, 0.0, -0.5)   ← 模型空间，应该是 Mesh 的原始坐标
VS Output SV_Position: (0.32, 0.15, 0.87, 1.0)  ← 裁剪空间，经过 MVP 变换后
```

如果 VS Output 的 SV_Position 位置明显不对（比如 z 值超出 0～1 范围，或者 xy 超出 -1～1 范围太多），说明 MVP 矩阵计算有问题。

---

## Texture Viewer 进阶：mip、通道、像素精确值

### 查看特定 mip 层级

贴图导入后，Unity 会生成 mip 链（从原始分辨率逐步缩小的多个版本）。GPU 根据物体到相机的距离选择合适的 mip 层级采样。

如果远处物体出现闪烁（mip 层级切换太生硬）或模糊不该模糊的细节（mip 生成算法有问题），可以在 Texture Viewer 右侧的 **Mip** 下拉里逐级查看每个 mip 的内容。

### 调整 Range 查看 HDR 内容

HDR 渲染时，Color Buffer 里存的是超过 1.0 的浮点值（比如 Bloom 处理前的高亮区域值可能是 3.5、8.0 等）。默认显示范围是 0～1，超出的部分显示为白色。

右侧 **Range** 工具可以手动设置显示范围，比如设成 0～10，能看到 HDR 内容的真实分布。这在调试 Bloom 阈值（为什么某块区域没有 Bloom——可能是值没超过阈值）时很有用。

### Pick 像素获取精确值

Texture Viewer 左上角的 **Pick** 按钮（或直接右键点击）：点击任意像素，右下角 **Pixel Context** 面板会显示这个像素的精确 RGBA 值和坐标。

结合 Shader Debugger（见下文），可以在这里找到一个感兴趣的像素，然后"进入"它的 Fragment Shader 执行过程。

### 查看深度缓冲

在 Texture Viewer 的 Outputs 下拉里选 Depth，可以查看深度缓冲的内容。默认显示是非线性深度（近处集中在 0，远处接近 1），用 Range 工具可以调整对比度。

深度缓冲异常（比如 Z-fighting——两个面的深度值非常接近，渲染结果在两者之间闪烁）可以通过查看深度缓冲的精确值来定位。

---

## Pipeline State：读懂渲染状态

选中一个 DrawIndexed 事件后，Pipeline State 面板显示完整的 GPU 流水线状态。重点关注几个阶段：

### VS / PS 阶段：Shader 和资源绑定

**VS（Vertex Stage）**：
- Shader：当前绑定的 Vertex Shader 程序（可以点击查看编译后的字节码）
- VB（Vertex Buffers）：绑定的顶点缓冲，能看到 stride（每个顶点的字节数）和 offset

**PS（Pixel Stage）**：
- Shader：当前绑定的 Fragment Shader 程序
- CBs（Constant Buffers）：绑定的常量缓冲（材质参数就在这里）——点击可以展开查看每个参数的实际值
- Textures：绑定的贴图列表，能看到每个采样器槽位绑定的是哪张贴图

**常量缓冲的值和 Frame Debugger 里的 Properties 对应**，但 RenderDoc 里看到的是 GPU 实际收到的二进制值，转换成浮点数后和 Unity 侧的值应该一致——如果不一致，说明数据在 CPU→GPU 传输途中有问题。

### OM（Output Merger）阶段：关键的渲染状态

OM 阶段显示输出相关的所有状态：

**Render Targets**：当前绑定的输出 RT，以及每个 RT 的格式（RGBA8、RGBA16F 等）。如果某个 Pass 应该写入某张 RT 但没有绑定，这里会直接看出来。

**Depth/Stencil State**：

```
DepthEnable:     true        ← 深度测试是否开启
DepthWriteMask:  All         ← 是否写入深度缓冲（透明物体通常是 Zero/不写入）
DepthFunc:       Less        ← 深度比较函数（Less = 更近的通过）
StencilEnable:   false       ← 模板测试是否开启
```

常见问题：透明物体渲染顺序不对——检查 `DepthWriteMask`，透明物体通常应该是不写入深度（`Zero`），如果错误地写入了深度，会挡住后面本该可见的其他透明物体。

**Blend State**：

```
BlendEnable:    true
SrcBlend:       SrcAlpha           ← 源像素的混合因子
DestBlend:      InvSrcAlpha        ← 目标像素的混合因子
BlendOp:        Add                ← 混合运算（Add/Subtract/Min/Max）
```

混合公式：`输出颜色 = 源颜色 × SrcBlend + 目标颜色 × DestBlend`

常见 Blend 组合：
- 标准透明：`SrcAlpha + InvSrcAlpha`（叠加透明效果）
- 加法混合（粒子发光）：`One + One`（颜色相加，越叠越亮）
- 无混合（不透明）：`BlendEnable = false`

如果粒子特效颜色不对（应该发光但变暗了，或者应该透明但完全不透明），检查 Blend State 是首选。

### Rasterizer State：几何处理

```
CullMode:    Back       ← 剔除背面（Back/Front/None）
FillMode:    Solid      ← 填充模式（Solid/Wireframe）
ScissorRect: ...        ← 裁剪矩形
```

如果模型有某些面消失（本该看到但看不到），`CullMode` 是第一个要检查的——法线朝向反了会导致背面被剔除，把 CullMode 临时改成 None 能确认是否是这个问题。

---

## Shader Debugger：逐像素追踪

**打开方式**：在 Texture Viewer 里用 Pick 选中一个感兴趣的像素，然后右键 → **Debug this pixel**（需要 Shader 以 Debug 模式编译）。

Shader Debugger 会展示这个像素在 Fragment Shader 里的完整执行过程：

```
行号  指令                           寄存器状态
----  ------------------------------ --------------------------------
12    sample r0, t0, s0              r0 = (0.52, 0.48, 0.41, 1.0)  ← BaseColor 采样结果
18    mul r1, r0, cb0[_BaseColor]    r1 = (0.52, 0.48, 0.41, 1.0)  ← 乘以 _BaseColor 参数
24    sample r2, t1, s1              r2 = (0.50, 0.51, 1.0)        ← NormalMap 采样结果
...
```

能逐步执行，观察每条指令后寄存器的变化。

**适合排查**：

- 某个像素颜色完全不对，但材质参数看起来正确——逐步追踪，找到第一条结果异常的指令
- 怀疑某个贴图采样返回了错误的值——在采样指令处观察返回值
- 数学计算溢出或 NaN（某些像素出现黑色或纯白色）——追踪找到产生 NaN 的位置

**注意**：Shader Debugger 需要 Shader 以调试模式编译（`#pragma enable_d3d11_debug_symbols` 或在 Unity Player Settings 里开启），默认的 Release 编译不支持逐步调试，只能看到汇编级指令。

---

## 一个完整的排查示例

**问题：角色面部法线贴图效果不对，光照方向感觉反了**

**第一步：Frame Debugger 定位**
找到角色面部的 Draw Call，确认 `_NormalMap` 贴图引用正确，Keywords 里有 `_NORMALMAP`。

**第二步：RenderDoc 捕获帧**
在 Texture Viewer 的 Inputs 里找 `_NormalMap`，查看贴图内容——检查颜色是否正常（法线贴图通常是偏蓝绿色的）。

**第三步：Mesh Viewer 检查 Tangent**
切到 Mesh Viewer，查看顶点数据里 TANGENT 的 w 分量——如果 w = -1（手性反转），会导致 TBN 矩阵方向错误，从而使法线贴图方向反转。

**第四步：确认**
如果 w 分量确实是 -1，问题出在模型导出设置（FBX 导出时切线方向设置不对）或 Unity Import Settings 里的 Tangent 计算方式。

---

## 调试工具的选用总结

至此，调试工具组的五篇文章已经覆盖了从基础到深入的完整工具链：

```
遇到渲染问题
  ↓
先用 Scene View Debug Mode 快速目视确认（Wireframe / Overdraw / Lighting Only）
  ↓
用 Frame Debugger 定位是哪个 Pass / Draw Call 出问题
  → 检查材质参数是否正确
  → 检查 Keyword 组合是否符合预期
  → 检查批处理是否如预期合批
  ↓
需要更深层排查时，用 RenderDoc
  → Mesh Viewer：验证顶点数据（Position / UV / Normal / Tangent）
  → Texture Viewer：验证贴图内容和采样结果
  → Pipeline State：验证 Blend / Depth / Stencil State
  → Shader Debugger：逐像素追踪 Fragment Shader 执行过程
```

下一篇进入光照资产——Lightmap、Light Probe、Reflection Probe 这三类资产是怎么产生的，以及它们怎么在 Fragment Shader 里和直接光一起合并成最终颜色。
