---
date: "2026-03-24"
title: "Unity 渲染系统 01e｜RenderDoc 入门：捕获第一帧并读懂它"
description: "讲清楚 RenderDoc 和 Frame Debugger 的定位差异，如何从 RenderDoc 启动 Unity 并捕获一帧，以及 Event Browser、Texture Viewer 和 Pipeline State 面板的基本用法。"
slug: "unity-rendering-01e-renderdoc-basics"
weight: 400
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "RenderDoc"
  - "Debugging"
  - "GPU"
series: "Unity 渲染系统"
---
> 如果只用一句话概括这篇，我会这样说：RenderDoc 是一个外部 GPU 帧捕获工具，它能让你看到 GPU 真正收到了什么数据——而不是 Unity 以为自己发出了什么——这个区别在排查顶点数据错误、贴图采样问题和 GPU 状态问题时至关重要。

上一篇用 Frame Debugger 把一帧画面的 Draw Call 顺序和材质参数看清楚了。但 Frame Debugger 有它看不到的地方——顶点数据、实际 mip 采样、逐像素的着色过程。

这篇进入 RenderDoc：一个能直接看 GPU 原始数据的工具。

---

## RenderDoc 和 Frame Debugger 的定位差异

先搞清楚两个工具各自解决什么问题：

| | Frame Debugger | RenderDoc |
|---|---|---|
| **层次** | Unity 引擎层（Draw Call 和 Pass） | GPU API 层（真实的 GPU 指令和数据） |
| **看材质参数** | 能，Unity 侧的值 | 能，GPU 收到的实际值 |
| **看顶点数据** | 不能 | 能（原始字节） |
| **看贴图 mip** | 不能 | 能（每个 mip 层级） |
| **看 Pipeline State** | 部分 | 完整（Blend/Depth/Stencil/Rasterizer） |
| **逐像素调试** | 不能 | 能（Shader Debugger） |
| **启动方式** | Unity 内置，直接启用 | 需要从 RenderDoc 启动 Unity |
| **适合场景** | 快速定位 Pass 顺序和材质问题 | 深入排查 GPU 数据正确性 |

两个工具是互补的：Frame Debugger 定位"哪里出问题"，RenderDoc 确认"数据层面出了什么问题"。

---

## 安装 RenderDoc

从 [renderdoc.org](https://renderdoc.org) 下载安装，支持 Windows、Linux、Android。

Unity 2019.3 及以上版本内置了 RenderDoc 集成——不需要额外配置，直接从 RenderDoc 启动 Unity 即可识别。

---

## 从 RenderDoc 启动 Unity

RenderDoc 的捕获原理是**注入目标进程**：它在进程启动时注入一个 hook，拦截所有图形 API 调用（DX11/DX12/Vulkan/OpenGL）。要让 RenderDoc 能捕获 Unity 的帧，必须由 RenderDoc 启动 Unity，而不是先打开 Unity 再附加。

**启动步骤：**

1. 打开 RenderDoc，切到 **Launch Application** 选项卡
2. Executable Path 填 Unity Editor 的路径：
   ```
   C:\Program Files\Unity\Hub\Editor\<版本>\Editor\Unity.exe
   ```
3. Working Directory 填项目根目录
4. Command-line Arguments 填：
   ```
   -projectPath "E:\你的项目路径"
   ```
5. 点击 **Launch** 启动 Unity

Unity 正常打开后，进入 Play Mode，在 RenderDoc 里点击 **Capture Frame**（或按 F12）捕获当前帧。

**Unity 编辑器里的快捷方式：**

Unity 菜单栏里有 **RenderDoc → Capture Frame** 选项（需要 RenderDoc 已启动且注入成功）。如果看不到这个菜单，说明 Unity 没有从 RenderDoc 启动。

---

## 加载捕获文件

捕获成功后，RenderDoc 左侧 **Captures** 列表里会出现一个 `.rdc` 文件。双击打开，进入帧分析界面。

首次打开某个 Unity 项目的帧时，RenderDoc 需要重新编译 Shader（因为 Unity 的 Shader 是在运行时编译的，RenderDoc 要重建调试信息）。这个过程可能需要几分钟。

---

## 界面结构

RenderDoc 打开捕获文件后，主要有几个面板：

**Event Browser（左侧）**

列出这一帧里所有的 GPU 事件，结构和 Frame Debugger 类似，但层级更深——每个 Unity Pass 下面能看到具体的 API 调用：

```
▼ Render Pass（对应 URP 的 Opaque Forward）
  ▶ vkCmdBeginRenderPass
  ▶ vkCmdBindPipeline       ← 绑定 Shader 程序
  ▶ vkCmdBindDescriptorSets ← 绑定贴图和常量缓冲
  ▶ vkCmdDrawIndexed        ← 实际绘制指令（对应一次 Draw Call）
  ▶ vkCmdDrawIndexed
  ...
  ▶ vkCmdEndRenderPass
```

点击任意事件，其他面板会同步更新显示该事件的相关数据。

**Texture Viewer（中央）**

显示当前选中事件输入或输出的纹理内容。上方有下拉菜单可以切换显示：
- Outputs：这次 Draw Call 写入的 RT（Color Buffer / Depth Buffer）
- Inputs：绑定的输入贴图（_BaseMap、_NormalMap 等）

右侧工具栏可以：
- 切换 R/G/B/A/Depth 通道单独查看
- 选择 mip 层级（验证 mip 生成是否正确）
- 调整显示范围（Range），方便查看 HDR 值或非常暗的区域
- 用 Pick 工具点击某个像素，查看精确的像素值

**Pipeline State（右侧）**

显示当前 Draw Call 完整的 GPU Pipeline State，分成几个阶段：

- **VS（Vertex Shader）**：绑定的 Shader 程序、输入的顶点缓冲
- **PS（Pixel/Fragment Shader）**：绑定的 Shader 程序、绑定的贴图资源
- **OM（Output Merger）**：输出的 RT、Blend State、Depth State、Stencil State

---

## 读懂 Event Browser 里的内容

Unity 使用的是哪个图形 API，Event Browser 里的指令名字就不同：

- **Vulkan**：`vkCmdDrawIndexed`、`vkCmdBeginRenderPass`
- **DirectX 11**：`DrawIndexed`、`OMSetRenderTargets`
- **DirectX 12**：`DrawIndexedInstanced`、`BeginRenderPass`
- **OpenGL**：`glDrawElements`

不同 API 的名字不同，但含义是一样的。Unity 在不同平台默认使用不同 API：
- Windows：DX11（默认）或 DX12
- Android：Vulkan（推荐）或 OpenGL ES
- macOS/iOS：Metal（RenderDoc 不支持 Metal，iOS 需要用 Xcode 的 Metal Debugger）

---

## 第一次用 RenderDoc 的建议流程

**目标：验证某个物体的顶点数据和贴图是否正确**

1. 捕获帧，打开捕获文件
2. 在 Event Browser 里找到这个物体对应的 `DrawIndexed` 指令（可以用 Find 搜索，或者通过 Frame Debugger 先定位 Draw Call 序号，再在 RenderDoc 里找对应位置）
3. 点击这条指令，切到 **Mesh Viewer**（下一篇详细讲），验证顶点数据
4. 切到 **Texture Viewer**，在 Inputs 下拉里找 `_BaseMap`，查看贴图内容和当前 mip
5. 切到 **Pipeline State → OM**，确认输出 RT 的格式和 Blend State

**目标：检查 RT 内容**

1. 在 Event Browser 里点击某个 `BeginRenderPass` 之后的任意 Draw Call
2. Texture Viewer 的 Outputs 下拉里选 Color 或 Depth
3. 用 Range 工具调整对比度，更容易看清内容
4. 用 Pick 工具点击某个像素，右下角会显示精确的像素值

---

## RenderDoc 在 Unity 项目里的常见局限

**Metal 不支持**：iOS 和 macOS 上无法用 RenderDoc，需要用 Xcode 的 Metal Frame Debugger，界面不同但概念类似。

**Android 需要额外配置**：在 Android 设备上使用 RenderDoc 需要 root 权限或使用 RenderDoc 专用的 APK 包装工具，或者使用 Unity 的 Android Logcat 配合。

**Shader 调试需要 Debug 编译**：Unity 默认的 Shader 编译是 Release 模式，逐像素调试（Shader Debugger）需要用 Debug 模式重新编译 Shader，在 Unity Player Settings 或 Shader 导入设置里开启。

**性能数据有限**：RenderDoc 擅长数据正确性排查，不擅长性能分析。GPU 性能数据（各 Pass 耗时、带宽等）需要用平台专用工具（NVIDIA NSight、ARM Mali GPU Tool、高通 Snapdragon Profiler 等）。

---

## 和下一篇的关系

这篇建立了 RenderDoc 的基本使用流程。下一篇进入 RenderDoc 的深度用法：Mesh Viewer 读顶点缓冲的原始数据、Texture Viewer 的高级功能、Pipeline State 各项参数的含义——这些是用 RenderDoc 真正解决问题的核心技能。
