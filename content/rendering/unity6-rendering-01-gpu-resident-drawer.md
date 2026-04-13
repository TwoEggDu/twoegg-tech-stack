---
title: "Unity 6 渲染管线升级 01｜GPU Resident Drawer 原理：从 SRP Batcher 到自动 Instancing"
slug: "unity6-rendering-01-gpu-resident-drawer"
date: "2026-04-13"
description: "GPU Resident Drawer 在 CPU→GPU 提交链路上改了什么：per-instance 数据从 Constant Buffer 搬到 StructuredBuffer，通过 BatchRendererGroup 自动将同 Shader variant 的物体合并为 instanced indirect draw call，减的不是 SetPass，是 draw submission 本身。"
tags:
  - "Unity"
  - "Unity 6"
  - "GPU Resident Drawer"
  - "BatchRendererGroup"
  - "SRP Batcher"
  - "GPU Instancing"
  - "渲染管线"
  - "URP"
series: "Unity 6 渲染管线升级实战"
series_id: "unity6-rendering-upgrade"
weight: 2201
unity_version: "6000.0+"
---

SRP Batcher 让 500 个物体用同一 Shader 时，SetPass 只做一次。但 CPU 仍然发了 500 次 DrawCommand 提交。

在几千到几万物体的场景里，draw submission 本身成了 CPU RenderThread 最大的开销来源——SRP Batcher 的 CBuffer 持久化解决不了这个问题。

GPU Resident Drawer 改变的就是这一层。本篇讲清它的调度模型，以及它与 SRP Batcher、传统 GPU Instancing 到底是什么关系。

---

## 从 SRP Batcher 的上限说起

[01b-2]({{< relref "rendering/unity-rendering-01b2-gpu-instancing-srp-batcher.md" >}}) 建立了两个关键结论：

- **SRP Batcher** 持久化的是 Constant Buffer——消除了逐物体的 CBuffer 上传，但 Draw Call 数量不变，CPU 仍然逐物体发 `DrawIndexedPrimitive` 调用。
- **GPU Instancing** 能把 Draw Call 合成 1 次，但要求同 Mesh + 同 Material 实例，且受 Constant Buffer 大小限制（DX11 下单个 CBuffer 最大 64KB），Unity 默认每批最多数百到约一千个实例（实际值取决于 per-instance 属性数量和平台 CBuffer 上限）。

把这两个结论画到一张 CPU→GPU 提交流程图里：

```
SRP Batcher 模式（Unity 2022）：

  每帧提交：
  ┌─────────────────────────────────────────────────┐
  │  CPU RenderThread                               │
  │                                                 │
  │  for each object:                               │
  │    if CBuffer 没变 → 跳过上传     ← SRP Batcher │
  │    else → 只上传变化的 CBuffer                  │
  │    DrawIndexedPrimitive(...)      ← 每物体一次  │
  │                                                 │
  │  5000 个物体 = 5000 次 draw submission          │
  └─────────────────────────────────────────────────┘
                    ↓ 5000 次
              ┌───────────┐
              │    GPU    │
              └───────────┘
```

SRP Batcher 减掉的是"上传"那一行的开销。但 `DrawIndexedPrimitive` 这一行——CPU 向图形 API 提交绘制命令的开销——是逐物体的，5000 个物体就是 5000 次。

传统 GPU Instancing 能合并 draw submission，但条件太严：同 Mesh + 同 Material 实例 + 上限 1023。场景里 500 棵树用了 5 种颜色的 Material（虽然都是同一 Shader），就得分 5 批。

GPU Resident Drawer 要解决的核心矛盾：**怎样在更宽松的条件下，把数千次 draw submission 合并到少量 instanced draw call 里。**

---

## GPU Resident Drawer 的调度模型

GPU Resident Drawer 把提交流程改成了这样：

```
GPU Resident Drawer 模式（Unity 6）：

  场景初始化 / 变更时：
  ┌─────────────────────────────────────────────────┐
  │  引擎为每个兼容 MeshRenderer 在 GPU 端分配      │
  │  StructuredBuffer 槽位                          │
  │  写入 per-instance 数据：                       │
  │    objectToWorld、materialProperties 等          │
  │  数据持久驻留 GPU 端，仅变化时增量更新           │
  └─────────────────────────────────────────────────┘

  每帧提交：
  ┌─────────────────────────────────────────────────┐
  │  CPU RenderThread                               │
  │                                                 │
  │  1. Culling                                     │
  │     Frustum Culling（CPU）                      │
  │     + 可选 GPU Occlusion Culling（HZB）         │
  │                                                 │
  │  2. DrawCommand 组装                            │
  │     同 Shader variant + 同 Mesh → 合成一个      │
  │     DrawCommand（不要求同 Material）             │
  │                                                 │
  │  3. 提交少量 DrawCommand                        │
  │     DrawIndexedInstancedIndirect(...)            │
  │     5000 个物体 → DrawCommand 数取决于             │
  │     Mesh × Shader variant 的去重组合数             │
  └─────────────────────────────────────────────────┘
                    ↓ 几十次
              ┌───────────┐
              │    GPU    │
              │  通过 SV_InstanceID 索引             │
              │  StructuredBuffer 读取               │
              │  per-instance 数据                   │
              └───────────┘
```

三个关键变化：

### 数据存储：Constant Buffer → StructuredBuffer

SRP Batcher 把 per-object 数据存在 Constant Buffer（`UnityPerDraw`、`UnityPerMaterial`）里，每个物体一份 CBuffer，持久化在 GPU 显存中。

GPU Resident Drawer 把 per-instance 数据存在 **StructuredBuffer** 里——一个大型结构体数组，所有实例的 Transform 和材质参数都在同一个 buffer 里，通过 index 访问。

```
Constant Buffer（SRP Batcher）：
  CBuffer[ObjectA]: { objectToWorld_A, baseColor_A, metallic_A, ... }
  CBuffer[ObjectB]: { objectToWorld_B, baseColor_B, metallic_B, ... }
  CBuffer[ObjectC]: { objectToWorld_C, baseColor_C, metallic_C, ... }
  ↑ 每个物体一个独立 CBuffer，大小受 64KB 限制

StructuredBuffer（GPU Resident Drawer）：
  Buffer[0]: { objectToWorld_A, baseColor_A, metallic_A, ... }
  Buffer[1]: { objectToWorld_B, baseColor_B, metallic_B, ... }
  Buffer[2]: { objectToWorld_C, baseColor_C, metallic_C, ... }
  ↑ 所有实例在同一个 buffer，通过 SV_InstanceID 索引，容量受 GPU 显存限制
```

Constant Buffer 的大小上限决定了传统 GPU Instancing 的单批实例数量天花板。StructuredBuffer 的容量不受 CBuffer 上限约束，改为受 GPU 可用显存限制——在桌面 GPU 上可容纳数十万实例，移动端上限取决于设备显存和 per-instance 数据大小。

### 合批条件：同 Material → 同 Shader Variant

传统 GPU Instancing 要求同 Mesh + 同 Material 实例才能合批。

GPU Resident Drawer 的合批条件更宽松：**同 Mesh + 同 Shader variant**。不同 Material（参数值不同）可以合批——因为材质参数不再存在共享 CBuffer 里，而是存在 StructuredBuffer 里按 `SV_InstanceID` 索引。

具体场景：500 棵树用了 5 种颜色的 Material，都用 URP Lit Shader。

```
传统 GPU Instancing：
  Material_Red    → DrawMeshInstanced(100 棵)   ← 第 1 批
  Material_Green  → DrawMeshInstanced(100 棵)   ← 第 2 批
  Material_Blue   → DrawMeshInstanced(100 棵)   ← 第 3 批
  Material_Yellow → DrawMeshInstanced(100 棵)   ← 第 4 批
  Material_Brown  → DrawMeshInstanced(100 棵)   ← 第 5 批
  合计：5 次 Draw Call

GPU Resident Drawer：
  Shader=URP/Lit + Mesh=Tree → 1 个 DrawCommand（500 instances）
  合计：1 次 Draw Call
  每个实例的 baseColor 从 StructuredBuffer[instanceID] 读取
```

### Shader 侧：UNITY_INSTANCING_BUFFER → UNITY_DOTS_INSTANCING

传统 GPU Instancing 在 Shader 里用 `UNITY_INSTANCING_BUFFER` 声明 per-instance 属性，数据存在 Constant Buffer 里：

```hlsl
// 传统 GPU Instancing（Unity 2022）
UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// 读取
float4 color = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor);
```

GPU Resident Drawer 在 Shader 里用 `UNITY_DOTS_INSTANCING` 宏，数据存在 StructuredBuffer 里：

```hlsl
// DOTS Instancing（Unity 6 GPU Resident Drawer）
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

// 读取
float4 color = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
```

虽然宏名称里有"DOTS"，但这只是历史命名——这套宏最初为 DOTS Hybrid Renderer 设计，后来被 GPU Resident Drawer 复用。**使用这些宏不需要引入 Entities 包，不需要 ECS 架构。**

---

## 从手动 BRG 到自动 GPU Resident Drawer

GPU Resident Drawer 底层用的是 `BatchRendererGroup`（BRG）API。这个 API 并不是 Unity 6 新增的——早在 Unity 2022 就存在，是 DOTS Hybrid Renderer 的底层渲染接口。

手动使用 BRG 需要开发者自己完成全部流程：

```csharp
// 手动 BRG（shader-advanced-19 已讲的路线）
// 1. 创建 BRG 实例
var brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);

// 2. 分配 GPU buffer，写入 per-instance 数据
var gpuBuffer = new GraphicsBuffer(...);
// 写入 objectToWorld、materialProperties...

// 3. 注册 batch
var batchID = brg.AddBatch(batchMetadata, gpuBuffer.bufferHandle);

// 4. 实现 Culling 回调，每帧填写 DrawCommand
JobHandle OnPerformCulling(
    BatchRendererGroup rendererGroup,
    BatchCullingContext cullingContext,
    BatchCullingOutput cullingOutput,
    IntPtr userContext)
{
    // 决定哪些实例可见，填写 DrawCommand
}
```

GPU Resident Drawer 在引擎内部自动执行了上述全部步骤：

1. 扫描场景中所有 MeshRenderer → 检查兼容性
2. 为每个兼容物体自动注册 BRG 实例
3. 自动管理 StructuredBuffer（分配、写入、增量更新）
4. 自动执行 Culling 和 DrawCommand 组装

开发者不需要写一行 BRG 代码。在 Frame Debugger 里，GPU Resident Drawer 生成的 draw 显示为 **"Hybrid Batch Group"**——这个名字也是 BRG / Hybrid Renderer 的历史遗留。

**手动 BRG 仍然有存在的意义：** 如果需要自定义 Culling 逻辑（比如自己做 GPU Driven Culling）、或者需要渲染 procedural 生成的大量物体（不挂 MeshRenderer），仍然要走手动 BRG 路线。GPU Resident Drawer 只接管"场景中已有 MeshRenderer 的 GameObject"。

---

## 开启条件与硬件要求

### 配置步骤

开启 GPU Resident Drawer 需要三处配置：

| 位置 | 设置项 | 值 |
|------|-------|----|
| Project Settings > Graphics | BatchRendererGroup Variants | Keep All |
| URP Asset | SRP Batcher | 启用 |
| URP Asset | GPU Resident Drawer | Instanced Drawing |
| Universal Renderer | Rendering Path | **Forward+** |

**Forward+ 是硬性前置条件（Unity 6.0 / URP 17.0）。** 在 6.0 中，使用 Forward（非 Forward+）或 Deferred 渲染路径时，GPU Resident Drawer 不生效。Deferred 路径的支持在 6.1 中通过 Deferred+ 开始引入，具体版本可用性请查阅对应版本的 Release Notes。这是最容易踩的坑——许多 Unity 2022 项目用的是 Forward 路径，升级到 Unity 6 后开了 GPU Resident Drawer 的选项，但没改渲染路径，结果 Frame Debugger 里看不到任何 Hybrid Batch Group，配置看起来开了但实际没工作。

### Shader 兼容性

Shader 必须支持 DOTS Instancing（包含 `UNITY_DOTS_INSTANCING_START` 宏声明）。

当前支持状态：

| Shader | DOTS Instancing 支持 |
|--------|---------------------|
| URP Lit | ✅ 内建支持 |
| URP Unlit | ✅ 内建支持 |
| URP SimpleLit | ✅ 内建支持 |
| URP BakedLit | ✅ 内建支持 |
| 自定义 Shader（手写） | ❌ 需要手动添加 DOTS Instancing 宏 |
| Asset Store Shader | ⚠ 多数尚未适配（截至 2026-04 的观察），需逐一确认是否包含 DOTS Instancing 宏 |
| Shader Graph 生成 | ✅ Unity 6 的 Shader Graph 自动生成 DOTS Instancing 兼容代码 |

Shader 不支持 DOTS Instancing 的物体不会报错——它们自动回退到 SRP Batcher 路径渲染，只是享受不到 GPU Resident Drawer 的合批收益。

### 物体级兼容性

以下条件会导致单个物体回退到普通渲染路径：

| 不兼容条件 | 回退行为 |
|-----------|---------|
| 使用 `MaterialPropertyBlock`（`renderer.SetPropertyBlock()`） | 该物体回退 SRP Batcher |
| Light Probes 设为 Use Proxy Volume | 回退 |
| 使用实时 GI（非静态光照贴图） | 回退 |
| 有 `OnRenderObject` 等 per-instance 回调脚本 | 回退 |
| SkinnedMeshRenderer / Animator 层级下的物体 | 回退 |
| 在两个相机渲染之间改变位置的物体 | 回退 |

如果需要显式排除某个物体，可以添加 `Disallow GPU Driven Rendering` 组件，支持递归应用到子物体。

### 已知限制

- **LOD Animated Cross-Fade 不支持**：GPU Resident Drawer 开启时，LOD 切换变为瞬间跳变（distance-based），没有 dithering 过渡。根据 Unity 论坛信息，animated cross-fade 的 LOD Group 绕过 GPU Resident Drawer 的改动计划 backport 到 6.0 / 6.2 / 6.3，具体版本是否已包含此修改请查阅对应版本 Release Notes。
- **OpenGL ES 不支持**：需要 Compute Shader 支持，OpenGL ES 不满足，完全回退。
- **WebGL 不支持**：完全回退。
- **构建时间增长**：Unity 会编译所有 BRG shader variant 进包，增加构建时间。

---

## 与 SRP Batcher / GPU Instancing / Static Batching 的关系

GPU Resident Drawer 不是独立的第五种合批方式——它开启后会接管和改变其他合批方式的行为。

```
GPU Resident Drawer 关闭时（Unity 2022 / Unity 6 默认）：
┌──────────────────────────────────────────────────────┐
│  同 Mesh + 同 Material + 大量重复？                  │
│    → YES → GPU Instancing（N→1 Draw Call）           │
│    → NO                                              │
│  同 Shader，CBuffer 结构规范？                       │
│    → YES → SRP Batcher（N→N，减 SetPass）            │
│    → NO                                              │
│  静态物体？                                          │
│    → YES → Static Batching（合并 Mesh）              │
│    → NO → 普通渲染                                   │
└──────────────────────────────────────────────────────┘

GPU Resident Drawer 开启时（Unity 6 + Forward+）：
┌──────────────────────────────────────────────────────┐
│  物体兼容 GPU Resident Drawer？                      │
│  （Shader 支持 DOTS Instancing、无 MPB、非 Skinned） │
│    → YES → Hybrid Batch Group                        │
│            同 Shader variant + 同 Mesh → 1 个        │
│            instanced indirect draw call              │
│    → NO  → 回退 SRP Batcher                          │
│                                                      │
│  Static Batching 开着？                              │
│    → 被静态合批的物体优先走 Static Batching           │
│      不进入 GPU Resident Drawer                       │
│    → 官方建议：开 GPU Resident Drawer 时关闭          │
│      Static Batching（Player Settings）              │
└──────────────────────────────────────────────────────┘
```

几个关键事实：

**两套路径同一帧共存。** GPU Resident Drawer 开启后，兼容物体走 Hybrid Batch Group 路径，不兼容物体走 SRP Batcher 路径。Frame Debugger 里会同时看到 "Hybrid Batch Group" 和 "SRP Batch" 两种条目。

**传统手动 GPU Instancing 被接管。** 不再需要手动调用 `DrawMeshInstanced`——引擎通过 BRG 自动做了更宽松条件的 instancing（不要求同 Material）。

**Static Batching 优先级更高。** 如果物体被 Static Batching 合并了，GPU Resident Drawer 不会再接管它。两者同时开启时，Static Batching 会先把物体合并成大 Mesh，GPU Resident Drawer 对这些合并后的 Mesh 不再做 instancing。所以官方建议二选一——如果开 GPU Resident Drawer，就在 Player Settings > Other Settings 里关掉 Static Batching。

**MaterialPropertyBlock 是最大的不兼容源。** 很多项目用 `renderer.SetPropertyBlock()` 做 per-instance 差异化（角色染色、受击闪白、动态高亮）。使用 MPB 的物体不走 GPU Resident Drawer。如果项目大量依赖 MPB，GPU Resident Drawer 的覆盖率会很低。替代方案是改用独立的 Material 实例或 DOTS Instancing 属性。

---

## 在 Frame Debugger 和 Profiler 里确认它在工作

开启 GPU Resident Drawer 后，最直接的验证方式是看 Frame Debugger。

### Frame Debugger 对比

```
SRP Batcher 模式（GPU Resident Drawer 关闭）：
  ▸ SRP Batch (150 objects)
    Draw Mesh "Rock_A"
    Draw Mesh "Rock_B"
    Draw Mesh "Tree_LOD0"
    ... × 150 行
    ↑ 150 个独立 Draw Call，SetPass 只做 1 次

GPU Resident Drawer 模式（开启）：
  ▸ Hybrid Batch Group
    Draw Mesh "Rock_A" (Instances: 80)
    Draw Mesh "Tree_LOD0" (Instances: 200)
    ... × 十几行
    ↑ 每行是一个 instanced draw call，Instances 显示合批数量
```

**SRP Batch** 展开后是 N 行独立 Draw Call（Draw Call 数量没变，减少的是 SetPass）。
**Hybrid Batch Group** 展开后每行是 instanced draw，单行但 instance count 可能是数十到数百。

如果 Frame Debugger 里**只看到** SRP Batch 而没有 Hybrid Batch Group：

1. 检查渲染路径是否为 Forward+
2. 检查 URP Asset 中 GPU Resident Drawer 是否设为 Instanced Drawing
3. 检查 Project Settings > Graphics > BatchRendererGroup Variants 是否为 Keep All
4. 检查场景中的 Shader 是否支持 DOTS Instancing

### Profiler 观察

开启前：CPU RenderThread 的时间分散在大量 Draw Call 提交中，Profiler 里看到密集的小段标记。

开启后：RenderThread 时间集中在 BRG 的 batch 组装和少量 instanced draw 提交上，整体 RenderThread 耗时下降。

### 覆盖率判断

在 Frame Debugger 里数一下 Hybrid Batch Group 的 draw 条目占 draw 总数的比例。这个比例反映 GPU Resident Drawer 的实际覆盖率。

如果 Hybrid Batch Group 占比明显偏低（例如不足一半），说明大量物体因 Shader 不兼容、MaterialPropertyBlock、SkinnedMesh 等原因回退了 SRP Batcher 路径。此时应先排查回退原因，再根据 Profiler 中 CPU RenderThread 的实际节省幅度决定是否保留 GPU Resident Drawer，还是暂时关闭回到纯 SRP Batcher 路径。

---

## 小结

GPU Resident Drawer 改变的不是 SRP Batcher 已经解决的 SetPass 开销，而是 SRP Batcher 没有触及的 **draw submission 本身**。

| | SRP Batcher | GPU Resident Drawer |
|---|---|---|
| per-instance 数据存储 | Constant Buffer（每物体独立） | StructuredBuffer（所有实例共享） |
| 合批条件 | 同 Shader（CBuffer 结构规范） | 同 Shader variant + 同 Mesh |
| 是否要求同 Material | 不涉及（每物体独立 CBuffer） | 不要求（参数按 instanceID 索引） |
| Draw Call 变化 | N → N（不变） | N → 少量 instanced draw |
| CPU 减少的开销 | CBuffer 上传 | draw submission |
| Shader 要求 | UnityPerDraw / UnityPerMaterial CBuffer | DOTS Instancing 宏 |
| 渲染路径要求 | 无特殊要求 | Forward+（URP） |
| 与 MPB 兼容 | 兼容 | 不兼容，回退 SRP Batcher |

本篇建立了 GPU Resident Drawer 的调度模型。但"知道它怎么工作"不等于"知道该不该开"——在什么场景下它比 SRP Batcher 快、什么场景下反而慢、GPU 端的代价怎么评估，这些问题留给下一篇。

---

**下一步应读：** [GPU Resident Drawer vs SRP Batcher：性能模型对比与切换时机]({{< relref "rendering/unity6-rendering-02-gpu-resident-drawer-vs-srp-batcher.md" >}}) — 用实测数据回答"该不该开"

**扩展阅读：** [GPU Instancing 与 SRP Batcher：两种减少 CPU 开销的机制]({{< relref "rendering/unity-rendering-01b2-gpu-instancing-srp-batcher.md" >}}) — 如果对 SRP Batcher / GPU Instancing 的基础机制不够清楚，回去补这篇
