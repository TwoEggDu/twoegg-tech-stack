---
title: "固定渲染管线：Built-in 的渲染流程与限制"
slug: "unity-rendering-06-builtin-pipeline"
date: "2025-01-26"
description: "Built-in 渲染管线是怎么组织一帧画面的，Camera 排序、Culling、Forward/Deferred 路径、以及它为什么越来越难适应现代项目需求。"
tags:
  - "Unity"
  - "渲染管线"
  - "Built-in"
  - "Forward"
  - "Deferred"
series: "Unity 渲染系统"
weight: 900
---
如果只用一句话概括这篇：Built-in 渲染管线用一套固定的"Camera → Culling → Pass 顺序"把一帧画面组织出来，这套流程在简单场景里工作良好，但它对 Shader 模型、光照模型和多 RT 输出都做了写死的假设，这就是后来需要 SRP 的根本原因。

---

## 从上一篇出发

上一篇（05：后处理）描述了后处理 Pass 怎么把帧缓冲区最终变成屏幕上的图像。但我们一直没有讨论一个更基础的问题：谁来决定这些 Pass 该按什么顺序执行？谁来决定每一帧先渲染哪些物体、用什么光照模型？

Built-in 渲染管线是 Unity 最早内置的答案。理解它是理解"为什么需要 SRP"的前提。

---

## 一帧的入口：Camera

Built-in 管线里，一帧的执行入口是场景里所有启用的 `Camera` 组件。Unity 按照 Camera 的 `Depth` 字段从小到大依次渲染每一个 Camera，Depth 值高的 Camera 结果叠加在 Depth 值低的 Camera 上面（通过 `Clear Flags` 和 `Viewport Rect` 控制覆盖方式）。

```
场景里启用的 Camera 列表（按 Depth 排序）
  Camera A (Depth=-1) → 渲染主场景
  Camera B (Depth=0)  → 渲染 UI 覆盖层
  Camera C (Depth=1)  → 渲染小地图
```

每个 Camera 单独走一遍完整的 Culling → 渲染流程。

---

## Culling：丢弃不可见物体

每帧渲染开始时，Unity 首先对当前 Camera 做视锥体剔除（Frustum Culling）：

```
Camera 的 ViewProjection 矩阵 → 计算视锥体六个平面
  ↓
遍历场景里所有 Renderer（MeshRenderer / SkinnedMeshRenderer / ...）
  ↓
对每个 Renderer 的包围盒（AABB）做平面测试
  ↓
完全在视锥体外 → 剔除（不提交 Draw Call）
完全或部分在内 → 进入可见列表
```

Unity 还支持 Occlusion Culling（遮挡剔除）：在烘焙阶段预计算物体间的遮挡关系，运行时跳过被完全遮挡的物体，进一步减少 Draw Call。

Culling 之后，Built-in 管线把可见的 Renderer 分为几类队列：

- **Opaque 队列**（RenderQueue 0–2500）：不透明物体，从前到后排序（近处优先，减少 Overdraw）
- **Transparent 队列**（RenderQueue 2501–5000）：半透明物体，从后到前排序（正确 Alpha 混合）
- **Overlay 队列**（RenderQueue > 5000）：始终最后渲染（UI、调试线框等）

---

## Forward 渲染路径：逐光源 Pass

Built-in 的 Forward 渲染路径（默认模式）对每个 Opaque 物体的处理逻辑如下：

```
对于场景里每个不透明物体：
  Base Pass（一次 Draw Call）
    → 处理：最重要的一盏实时光（通常是 Directional Light）
             + 所有 Lightmap 采样
             + 环境光（Ambient）

  Additional Pass（每盏额外实时灯一次 Draw Call）
    → 处理：每一盏影响该物体的额外实时光
    → 与 Base Pass 结果做加法混合
```

关键约束：

- **一个物体受 N 盏实时光影响 → N-1 次 Additional Pass → (N-1) 个额外 Draw Call**
- Additional Pass 默认上限是 4 盏（可以调 Quality Settings 里的 Pixel Light Count）
- 每次 Additional Pass 都重新读取 Depth Buffer 并写结果到 Color Buffer

这就是为什么场景里灯光增多后性能迅速下降：不是因为光照计算本身，而是因为 Draw Call 数量随灯光数线性增长。

---

## Deferred 渲染路径：几何和光照分离

Built-in 也支持 Deferred Shading，解决多光源 Forward 的 Draw Call 爆炸问题。

```
第一阶段：Geometry Pass（G-Buffer 填充）
  遍历所有 Opaque 物体，每个物体一次 Draw Call
  输出到四张 G-Buffer RT（同时写，用 MRT）：
    RT0: Albedo (RGB) + Occlusion (A)
    RT1: Specular (RGB) + Roughness (A)
    RT2: World Normal (RGB)
    RT3: Emission + Lightmap (RGB)

第二阶段：Lighting Pass（光照计算）
  对每盏光源单独做一次全屏 Pass（或光照体积 Pass）
  从 G-Buffer 读取所有几何信息
  计算该光源的贡献，加法累积到 Light Accumulation Buffer
```

Deferred 的优势：

- 物体 Draw Call 数量与光源数量解耦，N 盏灯只增加 N 次 Lighting Pass，不影响 Geometry Pass
- 每个像素只做一次 Shading 计算（已经过 Depth Test）

Deferred 的代价：

- 需要 4 张 G-Buffer，显存带宽压力大
- **不支持半透明物体**（G-Buffer 阶段只写最近片元的数据）：半透明物体仍然需要在 Deferred 之后走 Forward Pass
- **不支持 MSAA**（G-Buffer 下 MSAA 成本极高），抗锯齿只能用后处理方案（FXAA/TAA）
- 移动平台 TBDR 架构的 GPU（Apple、Mali、Adreno）处理多 RT 切换成本高，Deferred 在移动端表现差

---

## Shader 模型：写死的 Surface Shader

Built-in 提供了一套名为 **Surface Shader** 的抽象机制。开发者只需要写"表面属性计算"部分，Unity 自动生成 Base Pass 和 Additional Pass 的完整 Shader 代码：

```glsl
// Surface Shader 示意
CGPROGRAM
#pragma surface surf Standard fullforwardshadows

struct Input {
    float2 uv_MainTex;
};

void surf(Input IN, inout SurfaceOutputStandard o) {
    o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
    o.Metallic = _Metallic;
    o.Smoothness = _Smoothness;
}
ENDCG
```

Surface Shader 的限制：

- 光照模型选择只有 `Standard`、`Lambert`、`BlinnPhong` 几个内置选项，要用自定义光照模型需要绕过整个 Surface Shader 框架
- 生成的 Pass 代码是黑箱，开发者难以精确控制 Pass 数量、合批条件、Render State
- 不同平台的 Shader 变体（PC/Mobile/WebGL）差异由 Built-in 内部处理，开发者无法插手

---

## 有限的扩展点

Built-in 提供了几个运行时钩子，允许有限度地插入自定义逻辑：

| 扩展点 | 时机 | 能做什么 |
|---|---|---|
| `Camera.onPreRender` | Camera Culling 前 | 修改 Camera 参数 |
| `Camera.onPreCull` | Culling 前 | 修改 Renderer 可见性 |
| `CommandBuffer` + `AddCommandBuffer` | 特定渲染阶段前后 | 插入自定义 Draw Call、Blit |
| `OnRenderImage` | 后处理阶段 | 对 RT 做全屏处理 |

`CommandBuffer` 是 Built-in 里最灵活的扩展机制，但它的插入点仍然是固定的，只能在预定的"槽位"插入，无法重新定义整个管线的执行顺序。

例如，你可以用 `CommandBuffer` 在阴影渲染之后插入一个自定义 Pass，但无法改变"先渲染阴影图，再渲染不透明物体"这个顺序本身。

---

## Built-in 的根本限制

总结一下，Built-in 渲染管线的架构性问题：

**1. Shader 模型被管线绑定**

Surface Shader 假设光照模型是 Lambert/BlinnPhong/Standard，要完全自定义光照（比如卡通渲染的 Cel Shading、皮肤的 SSS）需要绕开 Surface Shader 系统，手写低级 Shader，同时失去所有 Pass 的自动生成能力。

**2. Pass 数量由管线决定，不由开发者决定**

Forward 路径的 Additional Pass 机制意味着"场景里有多少灯就有多少额外 Pass"，开发者无法控制这个行为，也无法换成不同的光照累积策略。

**3. 多平台差异硬编码**

Built-in 内部有大量针对 PC/Mobile/Console/WebGL 的条件编译，但这些差异处理对开发者不透明，移植时常常出现意外。

**4. 无法控制 RT 布局**

G-Buffer 的四张 RT 是 Built-in Deferred 的固定格式，开发者无法改变存储什么、用几张 RT，也无法在其中加入自己需要的数据（比如额外的屏幕空间信息）。

---

## 诊断：怎么确认当前用的是哪条路径

在 Frame Debugger 里可以清楚看到区别：

**Forward 路径特征：**
```
Camera.RenderForward
  → RenderForwardOpaque.Render
      Batch #1 "Base Pass"   ← 含 Directional Light
      Batch #2 "Add. Pass"   ← 额外 Point Light
      Batch #3 "Add. Pass"   ← 另一盏 Point Light
  → RenderForwardTransparent.Render
```

**Deferred 路径特征：**
```
Camera.RenderDeferred
  → GBuffer.Render       ← 所有不透明物体，每个一次 Draw Call
  → Lighting.DirLight    ← Directional Light Lighting Pass
  → Lighting.PointLight  ← Point Light 1 Lighting Pass（球形光照体积）
  → Lighting.PointLight  ← Point Light 2 Lighting Pass
  → ForwardPlus.Render   ← 半透明物体（仍然走 Forward）
```

---

## 小结

Built-in 渲染管线的设计是"给大多数场景提供开箱可用的渲染结果，隐藏所有复杂性"。这在 2010 年代初期是合理的。但随着：

- 移动平台需要更激进的性能优化（Forward 改版、Tile-based Light）
- 高端项目需要完全自定义的光照和后处理模型
- 多人团队需要对 Shader 变体、RT 布局有精确控制

Built-in 的固定管线架构变成了瓶颈。

下一篇将具体分析"Built-in 的限制在工程上具体造成了什么问题"，以及 SRP 是如何从架构层面解决这些问题的。
