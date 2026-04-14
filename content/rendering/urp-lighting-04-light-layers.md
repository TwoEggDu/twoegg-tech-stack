---
title: "URP 深度光照 04｜Light Layers：逐光源过滤的配置、场景与代价"
slug: "urp-lighting-04-light-layers"
date: "2026-04-14"
description: "Light Layers 不是 Layer，不是 Culling Mask，是独立的逐光源过滤机制。本篇讲清楚它的本质、三个典型应用场景、配置步骤、性能代价，以及与 Camera Stack 的交互。"
tags:
  - "Unity"
  - "URP"
  - "Light Layers"
  - "光照"
  - "渲染管线"
series: "URP 深度"
weight: 1582
---
> **读这篇之前**：本篇建立在 URP 光照系统基础上。如果不熟悉，建议先看：
> - [URP 深度光照 01｜URP 光照系统]({{< relref "rendering/urp-lighting-01-lighting-system.md" >}})

在项目里遇到过这种需求吗：一盏补光只照角色，不照地面；或者室内室外共用同一个场景，但室内灯不能穿墙照到室外物体。直觉上会去改 GameObject 的 Layer 配合 Culling Mask，但很快会发现：Culling Mask 控制的是"Camera 看不看得到这个物体"，根本不是"灯照不照这个物体"。

Light Layers 就是为了解决这个问题。

---

## 什么是 Light Layers

Light Layers 是 URP 12+（Unity 2021.2+）引入的一套独立于 GameObject Layer 的渲染层掩码系统。它的核心概念是 **Rendering Layer Mask**——每个 Renderer 和每盏 Light 各自持有一个 32 位掩码，只有两者按位与结果不为零时，这盏灯才会照亮该 Renderer。

几个关键事实：

- **Rendering Layer Mask 不是 GameObject Layer**。GameObject Layer 是 Unity 引擎级别的概念，用于 Physics、Culling、Tag 等系统，总共只有 32 层且全局共享。Rendering Layer Mask 是 SRP 引入的独立掩码，存在 Renderer 组件上，与 Physics Layer 互不干扰。
- **过滤发生在 GPU 着色阶段**，不是 CPU 裁剪阶段。物体照常进入渲染流程、照常写入深度，只是在 Fragment Shader 计算光照时，掩码不匹配的光源被跳过。
- **默认关闭**。必须在 Pipeline Asset 里显式开启，否则 Light 和 Renderer Inspector 上不会出现 Rendering Layer Mask 选项。

---

## 与 Layer / Culling Mask 的区别

这三个概念名字相似，但工作阶段和控制粒度完全不同：

| | GameObject Layer | Culling Mask | Light Layers |
|---|---|---|---|
| **作用阶段** | Culling（CPU 裁剪） | Camera 决定渲染哪些 Layer | Lighting（GPU 着色） |
| **控制粒度** | 物体级别 | Camera 级别 | Light x Renderer 级别 |
| **影响** | 物体是否参与渲染流程 | Camera 是否看到该 Layer 的物体 | 光源是否照亮该 Renderer |
| **设置位置** | GameObject Inspector 顶部 | Camera 组件 | Light 组件 + Renderer 组件 |
| **层数** | 32 层，全局共享 | 复用 GameObject Layer | 32 位独立掩码 |

一句话总结：Culling Mask 是"看不看得到"，Light Layers 是"照不照得到"。一个物体可以被 Camera 看到，但特定灯不照它——这正是 Light Layers 的用途。

---

## 典型应用场景

### 场景一：角色专属补光

开放世界里，主光（太阳）照亮整个场景。但角色在复杂环境里经常"脸黑"——背光面缺乏补光。常见做法是加一盏跟随角色的 Directional Light 或 Point Light 专门补亮角色。

问题是：这盏补光如果照亮地面和建筑，会破坏场景的整体光感，甚至让地面出现不合理的亮斑。

用 Light Layers 的做法：
- 角色 Renderer 设 Rendering Layer 0 + Layer 1
- 环境 Renderer 设 Rendering Layer 0
- 太阳设 Layer 0（照所有物体）
- 角色补光设 Layer 1（只照角色）

补光只影响角色，环境完全不受影响，视觉干净。

### 场景二：室内外光源分离

同一个场景中，室内有吊灯、壁灯，室外有太阳和路灯。物理上墙壁应该挡住光线，但实时光没有真实遮挡——Point Light 会穿墙。

Light Layers 把室内和室外的光源放在不同 Layer，室内物体只接收室内灯光，室外物体只接收室外灯光。不需要复杂的遮挡体方案，配置层掩码即可。

### 场景三：过场动画专用光

过场（Cutscene）期间需要特殊的戏剧性布光——Key Light、Rim Light、Fill Light 精心摆放。这些灯只在过场时启用，且只照过场演出的角色和道具，不应该影响背景里的普通场景物体。

给过场道具和角色额外加一个 Rendering Layer，过场灯只照这个 Layer。过场结束后禁用这些灯即可，场景光照不会受到任何污染。

---

## 配置步骤

### 第一步：开启 Light Layers

在 Pipeline Asset 里找到 Lighting 区域，勾选 **Light Layers**（URP 12/13 的命名）或 **Rendering Layers**（URP 14+ 的命名）：

```
Universal Render Pipeline Asset
  └─ Lighting
       └─ Light Layers: ✓ Enable
```

勾选后，Light 和 MeshRenderer 的 Inspector 上才会出现 Rendering Layer Mask 下拉框。

### 第二步：设置 Light 的 Rendering Layer Mask

在每盏 Light 的 Inspector 面板中，找到 **Rendering Layer Mask** 字段，选择这盏灯应该照亮哪些 Layer。默认是 Layer 0（即"Everything"的默认状态）。

### 第三步：设置 Renderer 的 Rendering Layer Mask

在 MeshRenderer（或 SkinnedMeshRenderer）的 Inspector 面板中，找到 **Rendering Layer Mask**，选择该物体属于哪些 Layer。

规则：Light 的 Mask 和 Renderer 的 Mask 按位与不为零 → 灯照亮物体。

### 第四步：代码控制

运行时可以动态修改掩码：

```csharp
// 修改 Renderer 的 Rendering Layer Mask
var renderer = GetComponent<MeshRenderer>();
renderer.renderingLayerMask = (1u << 0) | (1u << 1);  // Layer 0 + Layer 1

// 修改 Light 的 Rendering Layer Mask
var light = GetComponent<Light>();
light.renderingLayerMask = (1u << 1);  // 只照 Layer 1
```

过场系统启动时给过场灯和角色设上对应 Layer，结束后还原——这比在 Inspector 里手动配更灵活。

---

## 性能代价

Light Layers 不是免费的。开启后的代价主要在两个层面：

### Shader Variant 增加

开启 Light Layers 后，URP 会激活 `_LIGHT_LAYERS` 这个 Shader 关键字。这意味着所有受 URP 光照影响的 Shader 都会多编译一套 Variant（包含 Light Layers 采样逻辑的版本）。

Variant 增加的直接后果：
- **构建时间变长**：每个 Shader 的 Variant 组合数翻倍
- **包体变大**：多一套编译后的 Shader 字节码
- **运行时首次加载可能卡顿**：Shader Variant 首次使用时触发编译（如果没有预热）

### 逐像素掩码比较

在 Fragment Shader 里，每个光源的循环中增加一次位运算比较（Light Mask & Renderer Mask），判断是否跳过。这个操作本身极其轻量——一条 AND 指令加一条分支——逐像素代价可以忽略。

### 实际建议

**真正值得关注的是 Variant，不是逐像素运算。** 如果项目在移动端做了严格的 Shader Variant 预算管控，开启 Light Layers 等于给预算增加一个维度。

推荐做法：利用 Quality Tier 分级控制。在 High Quality 的 Pipeline Asset 上开启 Light Layers，Low Quality 的 Pipeline Asset 上关闭。这样高端机享受精细光控，低端机不承担 Variant 代价。

```
Quality Tier: High  →  Pipeline Asset A  →  Light Layers: ON
Quality Tier: Low   →  Pipeline Asset B  →  Light Layers: OFF
```

---

## 与 Camera Stack / Renderer Feature 的交互

### Camera Stack

URP 的 Camera Stack 由一个 Base Camera 和若干 Overlay Camera 组成。Light Layers 的设置挂在 Light 和 Renderer 上，不挂在 Camera 上——所以 Overlay Camera 渲染时，场景里的灯仍然按 Light 自身的 Rendering Layer Mask 生效。

换句话说：切换到 Overlay Camera 不会改变 Light Layers 的行为。两台 Camera 看到的同一个物体，受到的光照一致（前提是它们渲染的 Renderer 和 Light 相同）。

### 自定义 Render Pass / Renderer Feature

如果你写了自定义的 `ScriptableRenderPass`，Light Layers 的过滤已经内置在 URP 的光照计算函数里（`GetMainLight()`、`GetAdditionalLight()`）。只要你在 Shader 里调用这些标准接口，Light Layers 就自动生效，不需要手动处理。

但有一个容易踩的坑：在自定义 Renderer Feature 里使用 `DrawRenderers` 时，`FilteringSettings` 的 `renderingLayerMask` 字段控制的是**哪些 Renderer 参与绘制**（类似 Culling），而不是 Light Layers。Light Layers 的过滤始终发生在 Shader 内部，`FilteringSettings` 管不到它。不要把两者混淆。

---

## 常见问题

**Q：开了 Light Layers 但某个光不生效？**

检查两端的 Mask 是否匹配。最常见的错误：Light 设了 Layer 1，但 Renderer 只有 Layer 0——按位与结果为零，灯当然不照。在 Inspector 里确认两者的 Rendering Layer Mask 有交集。

**Q：开启 Light Layers 后帧率明显下降？**

不要只看帧率——先看 Shader Variant 数量。打开 `Project Settings → Graphics → Shader Stripping` 的日志，观察 Variant 总数是否暴增。如果项目有大量自定义 Shader 且 Keyword 组合本来就多，加上 `_LIGHT_LAYERS` 可能导致 Variant 爆炸。排查方向是 Variant 编译和加载，而非光照计算本身。

**Q：Light Layers 在 Shader Graph 里不生效？**

确认 Shader Graph 的 Target 设置为 Universal（不是 Built-in）。URP 的 Shader Graph 生成的代码会自动包含 `_LIGHT_LAYERS` 分支，但前提是 Graph 目标正确。如果你手动修改了生成的 Shader 代码或用了自定义 Master Node，需要确保 `_LIGHT_LAYERS` 关键字的 `#pragma multi_compile` 没有被剥离。

**Q：Light Layers 影响阴影吗？**

影响。如果 Light 和 Renderer 的 Rendering Layer Mask 没有交集，该 Renderer 既不会被这盏灯照亮，也不会向这盏灯的 Shadow Map 写入深度——即不投影、不接收阴影。这是符合预期的：灯都不照你，自然没有阴影关系。

---

## 导读

- [URP 深度光照 01｜URP 光照系统]({{< relref "rendering/urp-lighting-01-lighting-system.md" >}})
- [URP 深度光照 02｜URP Shadow 深度：Cascade 机制、Shadow Atlas、Bias 调参]({{< relref "rendering/urp-lighting-02-shadow.md" >}})
- [URP 深度配置 01｜Pipeline Asset 全字段解析]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})
