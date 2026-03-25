+++
title = "URP 深度扩展 05｜RenderDoc 调试 URP 自定义 Pass"
slug = "urp-ext-05-renderdoc"
date = 2026-03-25
description = "用 RenderDoc 调试 URP 自定义 Renderer Feature 的完整流程：Unity 接入 RenderDoc、捕获帧、在 Event Browser 里定位自定义 Pass、查看 RT 内容与 G-Buffer、追踪 Blit 链、在 Shader Debugger 里断点调试 HLSL。"
[taxonomies]
tags = ["Unity", "URP", "RenderDoc", "调试", "渲染管线", "性能分析"]
series = ["URP 深度"]
[extra]
weight = 1630
+++

写完 Renderer Feature 之后，最常见的问题是：效果没出来，但也没有报错。这时候需要一个能看到 GPU 实际执行了什么的工具——**RenderDoc**。

这篇讲如何用 RenderDoc 调试 URP 自定义 Pass 的典型问题，包含几个高频场景的排查流程。

---

## 接入 RenderDoc

### 方式一：Unity 内置接入（推荐）

1. Unity 编辑器菜单：**Window → Analysis → RenderDoc**
2. 点击 **Load RenderDoc**（自动检测本地安装）
3. Game 视图右上角出现 RenderDoc 图标，表示接入成功
4. 点击图标或按 `F12` 触发帧捕获

> 注意：RenderDoc 接入后 Unity 会切换到 DX11/Vulkan 渲染，某些特性行为可能略有差异。捕获的是 **当前帧**，不是录制视频。

### 方式二：RenderDoc 启动 Unity

1. RenderDoc → File → Launch Application
2. 选择 Unity 编辑器可执行文件（`Unity.exe`）
3. 传入项目路径作为命令行参数
4. 在 RenderDoc 里按 `F12` 捕获

方式一对调试编辑器内场景更方便，方式二在需要调试 Play 模式下的特定帧时更稳定。

---

## Event Browser：找到你的 Pass

捕获帧之后，核心界面是左侧的 **Event Browser**。

URP 的一帧在 Event Browser 里大致是这样的结构：

```
▼ Frame 1234
  ▼ [CommandBuffer] BeginSample: URP
    ▼ [CommandBuffer] BeginSample: RenderShadowMap
        DrawIndexed ...
    ▼ [CommandBuffer] BeginSample: CopyDepth
        DrawIndexed ...
    ▼ [CommandBuffer] BeginSample: RenderOpaqueForward
        DrawIndexed × N（场景物体）
    ▼ [CommandBuffer] BeginSample: [你的 Feature 名]    ← ★ 就在这里
        DrawIndexed ...
        Blit ...
    ▼ [CommandBuffer] BeginSample: PostProcessing
        ...
```

自定义 Pass 会以 `ProfilingSampler` 的名字出现在 Event Browser 里。这就是为什么在 Pass 里必须设置 `profilingSampler`：

```csharp
public MyPass()
{
    // 这个名字就是在 Event Browser 里看到的标签
    profilingSampler = new ProfilingSampler("MyFeature");
}
```

没有设置 ProfilingSampler 的 Pass 的命令会散落在 URP 的大括号里，很难找。

---

## 查看 RT 内容

在 Event Browser 里点击某个 Draw Call，右侧切换到 **Texture Viewer**：

- **Output** 标签页：当前 Draw Call 的渲染目标（颜色 + 深度）
- 右上角下拉菜单：切换查看哪张 RT（如果有 MRT）
- **Overlays**：可以叠加深度、法线、stencil 可视化

**典型操作：确认 Blit 是否正确**

1. 找到 Blit 命令（通常是一个全屏四边形 DrawIndexed）
2. 点击 Blit 之前的 DrawCall，查看 Input RT 内容
3. 点击 Blit 本身，查看 Output RT 内容
4. 对比两者，确认 Shader 处理是否符合预期

---

## 追踪 Blit 链的常见问题

### 问题：效果没有出现在最终画面

**排查步骤**：

1. 在 Event Browser 找到你的 Pass 范围
2. 查看 Pass 最后一个 Draw Call 的 Output RT
3. 往后找 URP 后续 Pass 的第一个 Draw Call，查看它的 Input RT
4. 如果 Output 有效果但 Input 没有，说明中间有 RT 被覆盖

**常见原因**：RT 引用错误。例如 `renderPassEvent` 选在了 `BeforeRenderingOpaques`，但 URP 在不透明 Pass 开始时会 Clear 颜色 RT，你写进去的内容被清掉了。

### 问题：UV 翻转（画面上下颠倒）

在 Texture Viewer 里查看 Blit 的 Output RT，如果内容是倒的：

- 使用了 `cmd.Blit` 而不是 `Blitter.BlitCameraTexture`
- Metal / Vulkan 后端的纹理坐标系和 DX 相反
- 改用 `Blitter.BlitCameraTexture` 解决

### 问题：Blit 输出是黑色

1. 查看 Blit 对应 Draw Call 的 Input 纹理——如果 Input 本身是黑的，问题在 RT 内容，不是 Blit
2. 查看使用的 Shader——点击 Draw Call，Pipeline State 标签下有当前绑定的 Vertex/Fragment Shader
3. 进入 Shader Debugger 断点确认采样是否正确

---

## G-Buffer 解析（Deferred 路径）

如果用 Deferred 渲染路径，G-Buffer 是重要的调试对象。URP 的 G-Buffer 布局（URP 14）：

```
GBuffer0 (RGBA8)  ：Albedo (RGB) + MaterialFlags (A)
GBuffer1 (RGB10A2)：SpecularColor (RGB) + Occlusion (A)
GBuffer2 (RGBA8)  ：NormalWS (RGB, Packed) + SmoothnessPacked (A)
Depth             ：场景深度（单独 RT）
```

在 Texture Viewer 里查看 G-Buffer 时：

- GBuffer2 的法线是 Packed 格式（不是直接的 -1~1 法线向量），看起来会偏蓝/绿
- 点击某个像素，**Pixel History** 标签会显示该像素在所有 Draw Call 中的历史值

---

## Shader Debugger：断点调试 HLSL

RenderDoc 支持在 HLSL Shader 里设置断点，查看某个像素的 Shader 执行过程。

**使用步骤**：

1. 在 Texture Viewer 里右键某个像素，选择 **Debug this pixel**
2. 弹出 Shader Debugger 窗口，显示该像素对应的 Fragment Shader
3. 可以单步执行，查看每一行的寄存器值
4. 如果效果在某个像素异常，在这里可以直接看到 `uv`、采样结果、计算中间值

**注意**：Unity 的 Shader 会被编译器优化，部分变量可能被内联，在 Debugger 里看不到。开启 Debug 编译可以保留更多信息：

```hlsl
// Shader 里加 #pragma enable_d3d11_debug_symbols（仅开发时）
#pragma enable_d3d11_debug_symbols
```

---

## Frame Debugger vs RenderDoc

Unity 内置的 **Frame Debugger**（Window → Analysis → Frame Debugger）和 RenderDoc 是互补的工具：

| | Frame Debugger | RenderDoc |
|---|---|---|
| 使用门槛 | 低，直接在 Unity 里用 | 需要安装，有学习成本 |
| RT 查看 | 每个 Pass 的输出 RT | 完整的纹理内容、格式、Mip |
| Shader 调试 | 不支持 | 支持像素级断点 |
| Draw Call 过滤 | 按 Pass 分组 | 完整 Event 树，可过滤搜索 |
| 性能数据 | 无 | GPU 时间戳（部分平台）|
| 移动端 | 不支持 | 支持（需要设备连接）|

**工作流建议**：先用 Frame Debugger 快速定位问题在哪个 Pass，确认问题后切换到 RenderDoc 做深度分析。

---

## 移动端：Android RenderDoc 调试

手游最终跑在移动端，PC RenderDoc 的捕获结果不代表真机行为。Android 上的 RenderDoc 调试：

1. **Android 设备安装 RenderDoc for Android**（Google Play 或官网 APK）
2. PC 端 RenderDoc → Tools → Android → 连接设备
3. Launch 目标 APK（Unity 编译时需要勾选 **Development Build** + **Wait for Managed Debugger** 可关闭）
4. 在 PC RenderDoc 界面触发捕获，数据传回 PC 分析

移动端常见额外问题：
- **Tile Memory 可见性**：TBDR 设备上，Tile 内的中间计算在 RenderDoc 里可能显示为空——这是正常的，Tile Memory 不在系统内存里
- **格式差异**：Android 上部分 RT 格式（如 R11G11B10）在低端设备上不支持，RenderDoc 会捕获到 Fallback 格式

---

## 小结

- `ProfilingSampler` 命名是在 Event Browser 里定位 Pass 的关键，一定要设置
- Texture Viewer：查看 RT 内容，确认 Blit 输入输出是否符合预期
- Blit 链追踪：如果效果消失，检查 RT 引用和 `renderPassEvent` 时机
- Shader Debugger：像素级断点，查看 HLSL 执行中间值
- Frame Debugger 快速定位，RenderDoc 深度分析，两者配合使用
- 移动端最终需要在真机上用 Android RenderDoc 验证，PC 结果仅供参考

下一篇：URP扩展-06，2022.3 → Unity 6 迁移指南——Breaking Change 清单与逐步迁移策略。
