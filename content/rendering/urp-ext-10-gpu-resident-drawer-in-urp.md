---
title: "URP 深度扩展 10｜GPU Resident Drawer 在 URP 中的落地：启用条件、Shader 适配与迁移决策"
slug: "urp-ext-10-gpu-resident-drawer-in-urp"
date: "2026-04-14"
description: "GPU Resident Drawer 原理见 Unity 6 渲染管线升级系列。本篇专讲 URP 项目里怎么启用、对自定义 Shader 的要求、对 Renderer Feature 的影响、与 SRP Batcher 的关系，以及什么时候值得开。"
tags:
  - "Unity"
  - "URP"
  - "GPU Resident Drawer"
  - "Unity 6"
  - "SRP Batcher"
  - "性能优化"
series: "URP 深度"
weight: 1648
---
> **读这篇之前**：GPU Resident Drawer 的原理（BatchRendererGroup、StructuredBuffer、DOTS Instancing）见：
> - [Unity 6 渲染管线升级 01｜GPU Resident Drawer 原理]({{< relref "rendering/unity6-rendering-01-gpu-resident-drawer.md" >}})
>
> URP Renderer Feature 基础见：
> - [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})

原理篇讲了 GPU Resident Drawer 在 CPU→GPU 提交链路上改了什么。这篇不重复那些内容，只聚焦一件事：在 URP 项目里，怎么把它开起来、踩哪些坑、什么时候值得开。

---

## 1 在 URP 中启用

### 1.1 开关位置

打开你的 **Universal Renderer Data** 资产（不是 Pipeline Asset），Inspector 顶部找到 **GPU Resident Drawer** 下拉，从 Disabled 改为 **Instanced Drawing**。

改完后不需要额外操作——URP 会在满足条件的物体上自动启用 GPU Resident Drawer 路径。

### 1.2 与 SRP Batcher 的关系

一句话：**GPU Resident Drawer 是 SRP Batcher 的超集**。

开启 GPU Resident Drawer 后，SRP Batcher 的 CBuffer 持久化仍然生效。GPU Resident Drawer 在此基础上额外做了一件事：通过 BatchRendererGroup 把同 Shader variant 的物体自动合并成 instanced indirect draw call，减少的不是 SetPass，是 draw submission 本身。

所以你不需要同时关心两个开关——开了 GPU Resident Drawer，SRP Batcher 的行为就隐含在里面了。

### 1.3 图形 API 要求

GPU Resident Drawer 依赖 indirect draw 和 StructuredBuffer，需要现代图形后端：

| 图形 API | 是否支持 |
|---|---|
| **Vulkan** | 支持 |
| **Metal** | 支持 |
| **DX12** | 支持 |
| **DX11** | 不支持——自动回退到 SRP Batcher |

如果项目的目标平台包含 DX11（例如需要兼容 Windows 7 的老设备），开了 GPU Resident Drawer 不会报错，这些设备上会静默回退到 SRP Batcher 路径。但你得知道这件事，否则 Profiler 数据在不同后端上会出现差异。

---

## 2 对 Shader 的要求

### 2.1 内置 Shader 无需改动

URP 自带的 Lit、SimpleLit、Unlit 等 Shader 已经内置了 DOTS Instancing 支持，开启后直接生效。

### 2.2 自定义 Shader 必须适配 DOTS Instancing

如果你有手写的 URP Shader，需要做三件事，否则这些 Shader 的物体会回退到 SRP Batcher（不会崩溃，但享受不到 GPU Resident Drawer 的合批收益）。

**第一步**：添加 variant 关键字声明：

```hlsl
#pragma multi_compile _ DOTS_INSTANCING_ON
```

**第二步**：把原来 CBUFFER 里的 per-instance 属性改成 StructuredBuffer 形式。用 Unity 提供的宏包裹：

```hlsl
// 改之前（SRP Batcher 模式）：
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float  _Smoothness;
CBUFFER_END

// 改之后（DOTS Instancing 模式）：
#ifdef DOTS_INSTANCING_ON
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float,  _Smoothness)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    #define _BaseColor    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
    #define _Smoothness   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Smoothness)
#endif
```

**第三步**：保留原来的 CBUFFER 声明，让非 DOTS 路径（DX11 回退、Editor 预览等）仍然能正常工作。两组声明用 `#ifdef DOTS_INSTANCING_ON` 隔开即可。

### 2.3 回退行为

不支持 DOTS Instancing 的 Shader 不会导致渲染错误。Unity 的处理方式是**逐物体回退**：该物体走 SRP Batcher 路径，其他支持 DOTS Instancing 的物体仍然走 GPU Resident Drawer。所以你可以渐进式迁移，不需要一次改完所有 Shader。

---

## 3 对 Renderer Feature 的影响

### 3.1 DrawRenderers 类 Feature

基于 `ScriptableRenderPass.DrawRenderers` 的自定义 Feature 仍然能工作，但要注意：GPU Resident Drawer 管理的物体在底层走的是 BatchRendererGroup 的 instanced indirect 路径，和你在 Feature 里手动构造的 DrawingSettings 可能产生交互。具体表现是——这些物体的绘制可能不经过你 Feature 里的 override material 或 override shader tag。

如果你的 Feature 需要对所有物体生效（例如自定义的 Outline Pass），需要实际测试确认 GPU Resident Drawer 管理的物体是否被正确覆盖。

### 3.2 MaterialPropertyBlock 的限制

GPU Resident Drawer 的数据路径是 StructuredBuffer，不是传统的 per-draw Constant Buffer。因此，`MaterialPropertyBlock` 的支持是有限的：

- 如果 MaterialPropertyBlock 设置的属性能被映射到 DOTS Instancing 的 StructuredBuffer，物体仍然走 GPU Resident Drawer。
- 如果不能映射（例如属性没有在 DOTS Instancing 宏里声明），该物体会回退到 SRP Batcher。

### 3.3 自动回退的场景

以下情况物体会自动从 GPU Resident Drawer 回退到 SRP Batcher：

- Shader 不支持 DOTS Instancing
- 使用了无法映射的 MaterialPropertyBlock
- 启用了 Dynamic Batching 的物体（Dynamic Batching 和 GPU Resident Drawer 互斥）
- SkinMesh 和粒子等非静态几何体（Unity 6.0 初期版本的限制，后续版本可能扩展支持）

---

## 4 性能对比

下表提供了一个对比框架，**实际数据需要你在目标设备上用 Profiler 和 Frame Debugger 实测填入**。

<!-- DATA-TODO: 用 Profiler 在目标设备上实测以下场景，填入 Batch 数和 CPU 渲染耗时 -->

| 场景 | SRP Batcher | GPU Resident Drawer | 差异 |
|---|---|---|---|
| 1000 静态物体（同 Material） | ___ Batches / ___ ms | ___ Batches / ___ ms | |
| 5000 静态物体（同 Material） | ___ Batches / ___ ms | ___ Batches / ___ ms | |
| 1000 物体 + MaterialPropertyBlock | ___ Batches / ___ ms | ___ Batches / ___ ms | |

**测试建议**：用 `Profiler → CPU → Rendering` 模块看 `RenderLoop.Draw` 耗时变化，用 `Frame Debugger` 看 Batch 数变化。物体数量低于几百的场景可能看不出差异，建议用 1000+ 物体的场景做压测。

---

## 5 迁移决策

### 5.1 适合开启的场景

- **高物体数量**：场景中超过 1000 个使用相同 Material 的 Renderer（城市、环境、植被）
- **静态为主**：大量静态物体的 Outdoor 场景，GPU Resident Drawer 的合批效果最明显
- **已在 Unity 6 + 现代后端**：项目已经跑在 Vulkan / Metal / DX12 上，不需要兼容 DX11

### 5.2 不适合开启的场景

- **大量 MaterialPropertyBlock 驱动的逐物体动画**：溶解效果、受击闪白等用 MPB 做的效果会导致频繁回退
- **必须支持 DX11**：DX11 上 GPU Resident Drawer 完全不生效，开了也是 SRP Batcher
- **小场景**：物体数量少于几百，SRP Batcher 已经够用，开启 GPU Resident Drawer 的收益可忽略

### 5.3 决策流程

```
物体数量 > 1000？
  ├─ 是 → 自定义 Shader 都支持 DOTS Instancing？
  │         ├─ 是 → 开启 GPU Resident Drawer
  │         └─ 否 → 先适配 Shader，再开启
  └─ 否 → SRP Batcher 够用，不需要开
```

适配 Shader 的工作量不大（参考第 2 节的代码改动），但需要逐个 Shader 排查。建议先在 Frame Debugger 里确认哪些 Shader 没走 GPU Resident Drawer 路径，再针对性修改。

---

## 相关文章

- [Unity 6 渲染管线升级 01｜GPU Resident Drawer 原理]({{< relref "rendering/unity6-rendering-01-gpu-resident-drawer.md" >}}) — 本篇的前置原理
- [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}}) — Renderer Feature 基础
- [URP 配置 02｜Renderer 设置]({{< relref "rendering/urp-config-02-renderer-settings.md" >}}) — GPU Resident Drawer 开关所在的 Renderer Data 资产详解
