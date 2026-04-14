---
title: "性能分析工具 02｜RenderDoc 完整指南：帧捕获、Pipeline State、资源查看、Shader 调试"
slug: "mobile-tool-02-renderdoc-complete-guide"
date: "2026-04-01"
description: "RenderDoc 不是另一个 Frame Debugger，而是一把直接看 GPU 真实数据的手术刀。本篇把帧捕获、Texture Viewer、Mesh Viewer、Pipeline State、Pixel History 和 Shader 调试串成一条完整工作流。"
tags:
  - "Unity"
  - "RenderDoc"
  - "GPU"
  - "Debugging"
  - "工具"
series: "移动端硬件与优化"
weight: 2070
---

RenderDoc 最容易被误解的地方，不是"难用"，而是"被拿来解决不属于它的问题"。

它很擅长回答：

- GPU 这一帧到底收到了什么数据
- 当前 Draw Call 实际绑定了哪些资源
- 这张 RT 里到底写进去了什么
- 这个像素为什么最终是这个颜色

但它不擅长回答：

- 为什么 CPU 这一帧这么慢
- 为什么 GC 正好在这个时机触发
- 为什么某台手机在 20 分钟后开始热降频

所以如果先把 RenderDoc 的职责立住，后面的面板和按钮才不会看散。

> 如果你第一次接触 `Pass`、`Draw Call`、`RT` 这些词，建议先补：
> - [Unity 渲染系统 01c5｜调试视角补桥：为什么工具里总在看 Draw Call、Pass 和 Render Target]({{< relref "rendering/unity-rendering-01c5-debugging-bridge-drawcall-pass-render-target.md" >}})
> - [Unity 渲染系统 01d｜Frame Debugger 使用指南：逐 Draw Call 分析一帧画面]({{< relref "rendering/unity-rendering-01d-frame-debugger.md" >}})

---

## 先给一句总判断

如果只用一句话概括 RenderDoc 在整条诊断链里的位置，我会这样说：

`Frame Debugger 先告诉你"哪一个 Pass / Draw Call 可疑"，RenderDoc 再告诉你"GPU 真实看到了什么、算出了什么、写出了什么"。`

这也是最稳的工作流：

```
先用 Frame Debugger 缩小范围
  → 再用 RenderDoc 看 GPU 真实数据
  → 如果问题已经变成平台瓶颈，再切去厂商工具
```

如果这一步顺序反过来，最常见的后果就是一上来就扎进几百上千个 Draw Call 里迷路。

---

## RenderDoc 真正解决什么问题

| 问题 | RenderDoc 是否适合 | 更合适的工具 |
|---|---|---|
| 某个 Pass 有没有执行 | 一般不先用它 | Frame Debugger |
| 某张 RT 里到底写了什么 | 非常适合 | RenderDoc |
| 贴图采样的是哪个 mip | 非常适合 | RenderDoc |
| 顶点 / UV / Tangent 数据是否正确 | 非常适合 | RenderDoc |
| Blend / Depth / Stencil 状态是否正确 | 非常适合 | RenderDoc |
| 某个像素为什么是这个颜色 | 非常适合 | RenderDoc |
| CPU Timeline / GC 尖峰 / Loading 卡顿 | 不适合 | Unity Profiler |
| GPU 是带宽瓶颈还是 ALU 瓶颈 | 只能辅助 | Mali / Snapdragon / Xcode |
| iOS Metal 帧分析 | 不适合 | Xcode GPU Frame Capture |

所以你可以把它理解成一把"数据正确性排查工具"，不是一把"通用性能分析工具"。

---

## 在开始抓帧前，先做两件事

### 1. 先在 Unity 里把范围缩小

如果你还不知道问题大概发生在：

- 哪个 Camera
- 哪个 Pass
- 哪类 Draw Call
- 哪个材质 / Shader

那先去用：

- [Unity 渲染系统 01d｜Frame Debugger 使用指南：逐 Draw Call 分析一帧画面]({{< relref "rendering/unity-rendering-01d-frame-debugger.md" >}})

Frame Debugger 负责回答"问题大概在哪一段"；RenderDoc 负责回答"这一段里数据到底怎么了"。

### 2. 确认平台是否适合 RenderDoc

```
适合：
  - Windows：DX11 / DX12 / Vulkan
  - Android：Vulkan / OpenGL ES（实际更推荐 Vulkan）
  - Linux：Vulkan / OpenGL

不适合：
  - iOS / macOS：Metal
```

如果是 iOS / macOS，直接转：

- [性能分析工具 05｜Xcode GPU Frame Capture：iOS Metal 性能分析完整指南]({{< relref "performance/mobile-tool-05-xcode-gpu-capture.md" >}})

---

## 建立一次可用的捕获会话

### 方式一：从 Unity 编辑器附加

如果你只是先在 PC / 编辑器里验证一帧，这个方式最省事：

```
Window → Analysis → RenderDoc
点击 Load RenderDoc
进入 Play Mode
按 F12 或点击 Capture
```

优点：

- 操作简单
- 和编辑器调试流程靠得很近
- 适合先确认工具链是否跑通

缺点：

- 你看到的是编辑器里的那一帧，不是真机环境
- 如果问题只在设备上出现，这一步不够

### 方式二：从 RenderDoc 启动 Unity

如果你需要更稳定的捕获环境，或者要确保注入成功，直接从 RenderDoc 启动 Unity 更稳。

```
Launch Application
  Executable Path:
    Unity.exe
  Working Directory:
    项目根目录
  Arguments:
    -projectPath "你的项目路径"
```

进入 Play Mode 后再抓帧。

### 方式三：Android 真机捕获

Android 能抓，但你要先判断目的：

- 目的是"画面不对、RT 不对、Shader 不对"
  可以抓 RenderDoc
- 目的是"带宽、ALU、Early-Z、热和功耗"
  先抓厂商工具更值

这一点如果混掉，后面会白忙。

---

## 抓下一帧后，先看哪四个面板

RenderDoc 功能很多，但日常排障真正高频的只有四类。

### 1. Event Browser：这一帧到底做了哪些事

这里列的是 GPU 事件，不是 Unity 场景树。

你要重点找的是：

- 当前 Render Pass 在哪里
- 哪个 `DrawIndexed` 或 `DrawIndexedInstanced` 对应目标 Draw Call
- 哪些事件是 Clear / Blit / Resolve / BeginRenderPass / EndRenderPass

一个最小经验是：

```
先找可疑 Draw Call
  → 再围绕它看前后几个事件
  → 不要一上来从帧头顺着硬读
```

### 2. Texture Viewer：当前输入和输出到底长什么样

这是 RenderDoc 里最常用的面板。

你要在这里做的事通常是：

- 看 Outputs：当前 Draw Call 写到哪张 RT
- 看 Inputs：当前 Shader 采样了哪些纹理
- 看通道：R / G / B / A / Depth / Stencil
- 看 mip：当前资源的不同 mip 层
- 看像素值：Pick 一个像素拿精确 RGBA
- 调 Range：把 HDR 或很暗的内容看清楚

很多"看起来像 Shader 算错"的问题，最后其实只是：

- 看错了 RT
- 看错了通道
- 看的不是当前 Draw Call 的输出
- 看的不是当前采样到的那张贴图

### 3. Mesh Viewer：GPU 真正收到的顶点数据是什么

只要问题跟下面这些词有关，就应该想到 Mesh Viewer：

- 顶点位置不对
- UV 拉伸 / 翻转
- 法线不对
- Tangent 不对
- 蒙皮后形变异常

RenderDoc 的价值不在于"告诉你 Inspector 里配置了什么"，而在于直接看：

```
VS Input
  - 输入顶点缓冲里的原始数据

VS Output
  - Vertex Shader 处理后的结果
```

这能帮你把问题切开：

- 原始数据就错了
- 原始数据对，但 Vertex Shader 变坏了

### 4. Pipeline State：这一跳的 GPU 状态是不是对的

这是另一个高频面板。

你重点看四块：

- VS：顶点输入、常量缓冲、绑定 Shader
- PS：贴图、采样器、常量缓冲、绑定 Shader
- OM：Color Attachment、Depth Attachment、Blend / Depth / Stencil
- Rasterizer：Cull、Fill、Viewport、Scissor

很多"结果不对"的问题，本质上根本不是贴图或代码问题，而是状态问题：

- Blend 开错
- ZTest / ZWrite 配错
- Cull 模式反了
- 绑定到了错误的 Color Attachment

---

## 五类最常见检查任务，分别怎么做

### 1. 查"当前到底写到哪张 RT"

最稳的动作是：

1. 在 Event Browser 选中可疑 Draw Call
2. 打开 Pipeline State
3. 看 `OM / Output Merger`
4. 确认当前 Color / Depth Attachment
5. 再到 Texture Viewer 打开对应输出

这个顺序很重要，因为很多人会直接在 Texture Viewer 里切来切去，但没先确认"当前 Draw Call 绑定的是哪张 RT"。

### 2. 查"Shader 采样的输入资源是不是对的"

最常见的动作是：

1. 在 Pipeline State 的 PS 阶段看绑定的纹理槽位
2. 打开对应资源预览
3. 检查：
   - 资源是不是那张图
   - mip 是否合理
   - 通道内容是否合理
   - 尺寸 / 格式是否合理

这类问题很适合排查：

- 后处理链拿错 source RT
- 法线贴图采样错资源
- LUT / Mask / Noise 图绑定错
- 动态 RT 没有按预期更新

### 3. 查"顶点 / UV / Tangent 到底对不对"

这类问题建议先看 Mesh Viewer，再决定是否看 Shader Debugger。

一个典型例子是法线贴图方向不对：

1. 先确认 `_NormalMap` 本身内容正常
2. 再看 Mesh Viewer 里的 `TANGENT`
3. 看 `w` 分量或方向是否异常
4. 再决定要不要进 Pixel / Shader 级别调试

这样效率比一上来就追 Fragment Shader 高得多。

### 4. 查"状态对不对，而不是资源对不对"

常见信号：

- 物体像被空气墙挡住，实际是 ZTest 问题
- 透明像素发灰，实际是 Blend 配置问题
- 某个面永远不显示，实际是 Cull 问题
- 效果只写深度不写颜色，实际是 Color Write Mask 问题

这类问题不要先猜贴图或关键词，直接先看 Pipeline State。

### 5. 查"这个像素到底为什么是这个结果"

这时再进像素级工具。

RenderDoc 里有两个层次：

**Pixel History**

- 看某个像素在这一帧里被哪些 Draw Call 改写过
- 看每次写入是通过了还是没通过深度 / 模板 / Blend
- 适合回答"谁最后覆盖了我"

**Shader Debugger**

- 看单个像素在 Shader 里的执行过程
- 适合回答"它为什么算成这个值"

经验顺序应该是：

```
先 Pixel History
  → 再 Shader Debugger
```

因为很多问题在 Pixel History 这一层就已经能结束：

- 根本不是当前 Draw Call 写的
- 写了但被后面的 Draw Call 覆盖
- 压根没通过深度 / 模板测试

---

## 一个最小但可复用的工作流

假设你碰到的问题是：

`某个全屏特效执行了，但结果全黑。`

最小排查流程可以这样走：

### 第一步：先在 Unity 里确认它真的执行了

用 Frame Debugger 看：

- 这个 Blit / Pass 是否存在
- 它在什么阶段执行
- 它前后的 RT 切换是否合理

### 第二步：RenderDoc 抓同一帧

抓到后，不要先看 Shader 代码，先做这三件事：

1. 看当前 Draw Call 的输出 RT
2. 看当前 Shader 的输入资源
3. 看当前状态

### 第三步：确认 source 和 destination

很多"全黑"问题，本质上是：

- destination RT 对了，但 source RT 本身就是空的
- source 对了，但 destination 指到了另一张临时 RT
- 当前 Pass 根本没有写入 Color，只写了 Depth

### 第四步：Pick 像素，再决定是否下钻

如果 RT 表面看起来全黑，先 Pick 几个像素：

- 真的是全 0？
- 只是 HDR 值很低，显示上像黑？
- Alpha 里有值，Color 没值？

只有这一步做完以后，才值得进 Pixel History 或 Shader Debugger。

### 第五步：用 Pixel History 判断是不是被覆盖

如果当前 Draw Call 看起来写对了，但结果最终还是不对，先看它是不是被后面的 Pass 覆盖了。

这一步尤其适合：

- 后处理链
- 透明叠加
- 多次 Blit
- UI / Scene 混写

---

## RenderDoc 最常见的四种误用

### 1. 用它查 CPU 尖峰

RenderDoc 不回答这个问题。

遇到：

- GC.Alloc
- Load 峰值
- 脚本 Update 尖峰
- Main Thread 卡顿

直接去：

- [性能分析工具 01｜Unity Profiler 真机连接：USB 接入、GPU Profiler 与 Memory Profiler]({{< relref "performance/mobile-tool-01-unity-profiler-device.md" >}})

### 2. 用它直接判断 GPU 瓶颈类型

RenderDoc 能看到"画了什么"，但不擅长告诉你"为什么这台 GPU 这么忙"。

如果你已经确认是 GPU 瓶颈，接下来更该去：

- [性能分析工具 03｜Mali GPU Debugger：Counter 系统与带宽分析]({{< relref "performance/mobile-tool-03-mali-debugger.md" >}})
- [性能分析工具 04｜Snapdragon Profiler：Adreno Counter 与 GPU 帧分析]({{< relref "performance/mobile-tool-04-snapdragon-profiler.md" >}})
- [性能分析工具 06｜跨厂商 GPU Counter 对照：读懂 Adreno / Mali / Apple GPU 数据]({{< relref "performance/mobile-tool-06-read-gpu-counter.md" >}})

### 3. 还没缩小范围就直接抓整帧

这是最浪费时间的用法。

正确顺序应该是：

```
症状
  → Frame Debugger 缩小范围
  → RenderDoc 看数据
  → 必要时再上厂商工具
```

### 4. 把"看见资源"误当成"理解问题"

RenderDoc 最大的诱惑在于它什么都能给你看见，但"看见"不等于"读懂"。

你每次都要强迫自己回答：

- 我现在是在验证输入、输出，还是状态？
- 我是在看"当前 Draw Call"，还是"最终整帧"？
- 我要回答的是"谁写的"，还是"为什么算成这样"？

---

## 平台边界：什么时候该离开 RenderDoc

可以用下面这张分流图记住：

```
画面顺序 / Pass 是否执行
  → Frame Debugger

RT / 输入资源 / 顶点 / 状态 / 像素为什么错
  → RenderDoc

CPU / GC / 加载 / 主线程
  → Unity Profiler

GPU 已经确定是瓶颈，想知道是带宽、ALU、采样还是早Z
  → Mali / Snapdragon / Xcode

iOS / macOS Metal
  → Xcode GPU Frame Capture
```

如果你能一直保持这个边界感，RenderDoc 就会非常好用。

---

## 文末建议

如果你想先知道站里这条线该怎么顺着读，看：

- [RenderDoc 阅读入口｜先读哪篇，遇到什么问题该回看哪篇]({{< relref "rendering/renderdoc-reading-entry.md" >}})

如果你已经进入 URP 自定义 Pass、RT 链和 Blit 链排障，看：

- [URP 深度扩展 05｜RenderDoc 调试 URP 自定义 Pass]({{< relref "rendering/urp-ext-05-renderdoc.md" >}})

如果你真正关心的是"手上这个症状先该开哪个工具"，接着看：

- [性能分析工具 09｜性能诊断工具选择指南：什么问题用 Frame Debugger / RenderDoc / Unity Profiler / Mali / Snapdragon]({{< relref "performance/mobile-tool-09-performance-diagnosis-tool-selection.md" >}})
