---
title: "URP 深度扩展 05｜RenderDoc 调试 URP 自定义 Pass"
slug: "urp-ext-05-renderdoc"
date: "2026-03-25"
description: "RenderDoc 是调试 URP 自定义 Pass 最有效的工具。本篇讲 Unity 连接 RenderDoc 的正确姿势、帧捕获后的 Pass 定位方法、RT 内容查看、Blit 链追踪、Shader 调试，以及几个常见问题的定位流程。"
tags:
  - "Unity"
  - "URP"
  - "RenderDoc"
  - "调试"
  - "渲染管线"
  - "性能分析"
series: "URP 深度"
weight: 1570
---
自定义 Renderer Feature 出问题时，Debug.Log 和 Frame Debugger 能帮你缩小范围，但要真正看清楚"某张 RT 里存了什么"、"这个 Draw Call 用的是哪个 Shader 变体"，需要 RenderDoc。

> 如果你第一次接触 `Pass`、`Draw Call`、`RT` 这些词，建议先补：
> - [Unity 渲染系统 01c5｜调试视角补桥：为什么工具里总在看 Draw Call、Pass 和 Render Target]({{< relref "engine-notes/unity-rendering-01c5-debugging-bridge-drawcall-pass-render-target.md" >}})
> - [Unity 渲染系统 01d｜Frame Debugger 使用指南：逐 Draw Call 分析一帧画面]({{< relref "engine-notes/unity-rendering-01d-frame-debugger.md" >}})

---

## 连接方式：两条路

### 方式一：从 Unity 编辑器启动（推荐）

1. 安装 RenderDoc（[renderdoc.org](https://renderdoc.org)）
2. Unity 编辑器菜单：**Window → Analysis → RenderDoc**
3. 点击 `Load RenderDoc`，Unity 编辑器会附加 RenderDoc
4. 进入 Play 模式，点击编辑器里的相机图标（或按 F12）捕获一帧

**优点**：可以直接调试编辑器里的渲染，不需要打包，迭代最快。

### 方式二：从 RenderDoc 启动 Unity

1. 打开 RenderDoc，`File → Launch Application`
2. 选择 Unity 编辑器可执行文件，设置工作目录和命令行参数
3. RenderDoc 启动 Unity 后自动附加

这种方式适合调试特定启动参数或不想从编辑器操作的情况，但比方式一麻烦。

---

## 捕获一帧后的基本导航

捕获成功后，RenderDoc 主界面分三个区域：

```
左侧 Event Browser  →  中间 Texture Viewer / Mesh Output  →  右侧 Pipeline State
```

**Event Browser**：列出当前帧所有 Draw Call 和 API 调用，是导航的起点。

**Texture Viewer**：查看任意 RT 的内容，支持按通道分离（R/G/B/A）、HDR 值查看、像素 pick。

**Pipeline State**：查看选中 Draw Call 的完整渲染状态——绑定了哪些 RT、使用了哪个 Shader、深度/模板/混合状态是什么。

---

## 定位自定义 Pass

URP 的 Pass 在 Event Browser 里以 `ProfilerMarker` 名字展示。如果你在 Pass 里设置了 `profilingSampler`，可以直接搜索：

```csharp
// 在 Pass 里设置名字
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get("MyCustomEffect"); // 这个名字会出现在 Event Browser 里
    // ...
}
```

在 Event Browser 的搜索框输入 `MyCustomEffect`，直接跳转到对应的 Draw Call 组。

**如果看不到自己的 Pass**：
- 检查 `AddRenderPasses` 里是否正确 Enqueue
- 检查 `Execute` 里是否有 `context.ExecuteCommandBuffer(cmd)` 调用
- URP 的 Pass 有时会被合并到同一个 `CommandBuffer` 里，看父节点而不是单独的 Draw Call

---

## 查看 RT 内容

选中一个 Draw Call 后，在 Pipeline State 面板的 `OM（Output Merger）` 区域可以看到当前绑定的 RT：

- **Color Attachment 0**：当前的颜色写入目标
- **Depth Stencil**：当前的深度/模板缓冲

点击 RT 缩略图，在 Texture Viewer 里打开：

**常用操作**：
- **R/G/B/A 按钮**：单独查看某个通道，排查法线 RT、深度 RT 的内容
- **HDR Range 调节**：如果 RT 是 HDR 格式，调节 Range 滑块让值域可见
- **像素坐标 Pick**：点击 Texture Viewer 里的像素，右下角显示该像素的精确值（RGBA 浮点）

**调试法线 RT 的常见误区**：法线贴图的值域是 [-1, 1]，存在 RT 里时通常是 [0, 1]（编码后）。如果看到法线 RT 全黑，先调节 Range，再判断是采样问题还是写入问题。

---

## 追踪 Blit 链

自定义后处理效果通常是"采样 A → 写入 B → 采样 B → 写回 A"的链条。如果最终效果不对，需要逐步确认每个环节的 RT 内容。

**定位方法**：

1. 在 Event Browser 找到你的 Pass 组
2. 展开，找到 `DrawCall` 节点，点击
3. Pipeline State → `PS（Pixel Shader）` 查看绑定的贴图：`t0 / t1 / t2...` 对应 Shader 里的 `Texture2D _SourceTex` 等
4. 点击贴图预览，在 Texture Viewer 里确认采样的是正确的 RT

**Blit 链断了的常见原因**：
- `Blitter.BlitCameraTexture` 的 source 和 destination 用了同一个 RTHandle（读写同一张 RT 是未定义行为）
- Intermediate Texture 被触发，实际 `activeColorTexture` 不是你以为的那张
- RenderPassEvent 顺序不对，采样的 RT 还没被上一个 Pass 写入

---

## Shader 调试

### 查看正在执行的 Shader 和变体

选中 Draw Call → Pipeline State → VS（Vertex Shader）或 PS（Pixel Shader）

点击 Shader 名字旁的 `Edit` 按钮，进入 Shader Viewer：

- **Source**：如果 Shader 包含调试信息，可以看到 HLSL 源码
- **Disassembly**：查看反汇编的 GPU 指令，用于极致的 Shader 性能分析
- **Constants**：查看当前 Draw Call 的 Constant Buffer 值（对应 `cbuffer`），确认 `_Color`、`_Intensity` 等参数是否被正确传入

### 确认 Unity 打了调试信息

RenderDoc 能看到 Shader 源码的前提是 Unity 打包时包含调试信息：

- **编辑器模式**：默认包含，直接可以看
- **Development Build**：`Player Settings → Development Build` + `Allow 'unsafe' Code`，Shader 调试信息会包含
- **Release Build**：默认不包含 Shader 调试信息，只能看反汇编

---

## 几个常见问题的定位流程

### 自定义 Pass 执行了但 RT 内容全黑

```
1. Texture Viewer 打开目标 RT → 查看 HDR Range 和通道
2. 确认写入的 RT 和查看的 RT 是同一张（Pipeline State → OM 里的 Color Attachment）
3. 检查 Shader：PS Constants 里参数值是否正确传入
4. 检查 Material 的 Blend State：是否意外开了 Zero 混合
```

### 效果出现但位置/UV 不对

```
1. Texture Viewer → 查看 Blit 的 source RT，确认内容正确
2. Pipeline State → VS → Constants 里查看 _BlitScaleBias 值
   （Blitter 用这个参数控制 UV 偏移，值不对会导致采样错位）
3. 检查是否有 RT 翻转问题（Metal/OpenGL 的 UV Y 轴方向不同）
```

### 自定义 Pass 根本没执行

```
1. Event Browser 搜索 Pass 名字，确认是否出现
2. 如果没有：
   - AddRenderPasses 里加 Debug.Log，确认 EnqueuePass 被调用
   - 检查 renderPassEvent 时机是否被 URP 内部 Pass 覆盖
3. 如果 Unity 6：打开 RenderGraph Viewer 确认 Pass 是否被裁剪
```

---

## Frame Debugger vs RenderDoc 的选择

两个工具各有侧重，不是替代关系：

| 场景 | 推荐工具 |
|------|---------|
| 快速确认 Pass 是否执行、执行顺序 | Frame Debugger |
| 查看具体 RT 的像素内容 | RenderDoc |
| 确认 Shader 参数是否传入 | RenderDoc |
| 追踪 Native RenderPass 合并情况 | Frame Debugger |
| Shader 反汇编和指令分析 | RenderDoc |
| 移动端调试（iOS Metal） | Xcode GPU Frame Capture |
| 移动端调试（Android Adreno） | Snapdragon Profiler |

**调试顺序建议**：先用 Frame Debugger 确认 Pass 执行顺序和 RT 绑定，缩小范围后再用 RenderDoc 看具体内容。

---

## 小结

- Unity 编辑器 Window → Analysis → RenderDoc 直接附加，Play 模式下 F12 捕获
- Event Browser 按 Pass 名字（CommandBuffer.name）快速定位
- Texture Viewer：按通道查看、调节 HDR Range、像素 Pick 获取精确值
- Pipeline State → OM 确认写入的 RT，PS → Constants 确认 Shader 参数
- 效果全黑先查 Range 和 Blend State，UV 错位查 `_BlitScaleBias`，Pass 没执行先查 Unity 6 的 RenderGraph Viewer 裁剪

下一篇：URP扩展-06，2022.3 → Unity 6 迁移指南——Breaking Change 清单与迁移策略。
