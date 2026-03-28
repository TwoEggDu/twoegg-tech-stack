---
title: "Unity DOTS E16｜Entities.Graphics：Hybrid Renderer、MaterialMeshInfo、GPU Instancing 与 Mesh 替换"
slug: "dots-e16-entities-graphics"
date: "2026-03-28"
description: "Entities.Graphics（原 Hybrid Renderer）是 ECS 与 Unity URP/HDRP 渲染管线的桥接层。本篇讲清楚 RenderMeshArray、MaterialMeshInfo 的工作原理、GPU Instancing 是如何触发的，以及运行时动态替换 Mesh/Material 的正确方式。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Entities.Graphics"
  - "GPU Instancing"
  - "渲染"
  - "URP"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 16
weight: 1960
---

## 为什么不能直接用 MeshRenderer

`MeshRenderer` 是 `MonoBehaviour` 的子类，它的整个生命周期都绑定在 `GameObject` 上。ECS 的 `Entity` 不是 `GameObject`——它只是一个 `int` 句柄加一组 `ComponentData`，根本没有 Transform 层级，也没有引擎为它自动调用 `OnEnable` / `Update`。

把 `MeshRenderer` 挂到 Entity 上不可行，但渲染管线（URP / HDRP）又需要一个入口来收集"这帧要画什么"。**Entities.Graphics**（Entities 包在 1.x 版本中的名称，前身是 Hybrid Renderer V2）就是这个桥接层：它在每帧通过 `IJobChunk` 遍历所有携带渲染组件的 Chunk，把批次信息直接提交给 SRP Batch 渲染器，绕开了传统 GameObject 路径。

---

## 核心组件一览

Entities.Graphics 的最小渲染单元由以下几个组件共同构成：

```
┌──────────────────────────────────────────────────────────┐
│  Chunk（共享同一 Archetype 的 Entity 集合）               │
│                                                          │
│  [SharedComponent]  RenderMeshArray                      │
│  ┌──────────────────────────────────────┐                │
│  │  meshes[]     = [Mesh_A, Mesh_B, …]  │                │
│  │  materials[]  = [Mat_0, Mat_1, …]    │                │
│  └──────────────────────────────────────┘                │
│           ▲               ▲                              │
│           │ meshIndex     │ materialIndex                │
│  [Component]  MaterialMeshInfo  (per-Entity)             │
│                                                          │
│  [Component]  RenderBounds      (per-Entity AABB)        │
│  [Component]  LocalTransform    (per-Entity 位置/旋转)   │
└──────────────────────────────────────────────────────────┘
```

- **`RenderMeshArray`**（`ISharedComponentData`）：存储一个 Mesh 数组和一个 Material 数组。同一 Chunk 中的所有 Entity 共享同一份 `RenderMeshArray`，避免重复上传资源到 GPU。
- **`MaterialMeshInfo`**（`IComponentData`）：每个 Entity 独立持有，记录它用哪个 `meshIndex` 和 `materialIndex`（在 `RenderMeshArray` 的数组中的下标）。这是 per-Entity 选择外观的关键。
- **`RenderBounds`**：Entity 的轴对齐包围盒（AABB），供视锥剔除和遮挡剔除使用。
- **`LocalTransform` / `WorldTransform`**：Entities.Graphics 读取这两个组件确定渲染位置，不依赖 GameObject Transform。

---

## 在 Baking 中设置渲染

Baker 是 Authoring → Entity 的转换入口。`RenderMeshUtility.AddComponents()` 是 Entities.Graphics 提供的工具方法，一次性把上面所有必需组件都加到 Entity 上。

```csharp
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

// Authoring Component（挂在 GameObject 上）
public class EnemyAuthoring : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
}

// Baker
public class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // 构造 RenderMeshDescription：指定渲染层、阴影模式等
        var desc = new RenderMeshDescription(
            shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.On,
            receiveShadows: true);

        // 构造 RenderMeshArray（可以传多个 Mesh/Material）
        var renderMeshArray = new RenderMeshArray(
            new[] { authoring.material },
            new[] { authoring.mesh });

        // AddComponents 负责添加 RenderMeshArray、MaterialMeshInfo、RenderBounds 等
        RenderMeshUtility.AddComponents(
            entity,
            this,           // IBaker
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
    }
}
```

`MaterialMeshInfo.FromRenderMeshArrayIndices(meshIndex, materialIndex)` 明确告诉运行时：这个 Entity 使用 `RenderMeshArray.meshes[0]` 和 `RenderMeshArray.materials[0]`。

---

## GPU Instancing 的工作原理

Entities.Graphics 不需要你手动调用 `Graphics.DrawMeshInstanced`。它的批次合并逻辑是：

1. 每一帧，系统遍历所有拥有渲染组件的 Chunk。
2. 同一 Chunk 内的 Entity 天然共享同一 `RenderMeshArray`（SharedComponent 相同 → 同 Chunk）。
3. 对于相同 `meshIndex + materialIndex` 的 Entity，系统将它们的 `LocalToWorld` 矩阵打包成一个 GPU Instancing 批次提交。
4. 单批次上限通常为 500～1023 个 Instance（取决于平台和 Buffer 大小），超出时自动拆分成多批次。

**合批的核心条件：`RenderMeshArray` 的 SharedComponent 值必须相同。** 如果两组 Entity 使用了内容完全一样但引用不同的 `RenderMeshArray` 实例，它们会被分到不同的 Chunk，无法合批。在 Baking 阶段复用同一个 `RenderMeshArray` 对象即可保证合批。

---

## 运行时替换 Mesh / Material

### 方式一：只修改 MaterialMeshInfo（轻量）

当 Mesh 和 Material 都已经在 `RenderMeshArray` 中，只需改下标，不触发 Archetype 迁移：

```csharp
// 在 SystemBase / ISystem 中
public partial struct SwitchLodSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (info, transform) in
            SystemAPI.Query<RefRW<MaterialMeshInfo>, RefRO<LocalTransform>>())
        {
            // 根据距离摄像机的距离选择 LOD Mesh 下标
            float dist = math.distance(transform.ValueRO.Position, CameraPosition);
            int meshIdx = dist > 50f ? 1 : 0;   // meshes[0]=高模, meshes[1]=低模
            info.ValueRW = MaterialMeshInfo.FromRenderMeshArrayIndices(meshIdx, 0);
        }
    }
}
```

### 方式二：修改 RenderMeshArray（SharedComponent 替换）

当需要更换整组 Entity 的外观（例如所有"精英敌人"换皮肤），修改 SharedComponent 会触发 Chunk 迁移——Entity 从旧 Chunk 移动到新 Chunk。这个操作比修改 `MaterialMeshInfo` 代价更高，适合低频调用：

```csharp
// EntityManager 操作，不能在 Job 中直接调用
EntityManager.SetSharedComponentManaged(entity, new RenderMeshArray(
    new[] { eliteMaterial },
    new[] { eliteMesh }));
```

---

## 材质属性覆盖：Per-Entity Properties

传统 `MaterialPropertyBlock` 在 ECS 中没有等价 API，但 Entities.Graphics 提供了一套基于 Component 字段的机制：用 `[MaterialProperty("_PropertyName")]` 标记一个 `IComponentData`，系统会自动把每个 Entity 的字段值上传到 GPU 的 per-instance buffer。

```csharp
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

// 每个 Entity 独立的颜色属性
// "_BaseColor" 对应 URP Lit Shader 的同名属性
[MaterialProperty("_BaseColor")]
public struct EnemyColor : IComponentData
{
    public float4 Value;   // RGBA，float4 对应 shader 中的 float4
}
```

在 Baker 中添加这个 Component：

```csharp
AddComponent(entity, new EnemyColor { Value = new float4(1, 0, 0, 1) }); // 红色
```

在 System 中随时修改：

```csharp
foreach (var color in SystemAPI.Query<RefRW<EnemyColor>>())
{
    color.ValueRW.Value = new float4(0, 1, 0, 1); // 改为绿色
}
```

Entities.Graphics 会在每帧把所有 `EnemyColor` 的值打包进 GPU Buffer，Shader 端无需任何修改，per-instance 属性会自动生效。注意 Shader 必须开启 GPU Instancing（在 Inspector 中勾选，或在 ShaderGraph 中开启 Enable GPU Instancing）。

---

## LOD 与 Entities.Graphics

Entities.Graphics 内置了基于 `LODGroupMask` 的 LOD 剔除。Baking 时如果 GameObject 上有 `LODGroup`，Unity 会自动将每个 LOD 层级 Bake 成携带不同 `LODGroupMask` 的 Entity，运行时由 Entities.Graphics 根据摄像机距离选择显示哪一层级。

这和"Mass LOD"（在 Simulation 层面根据距离降低 AI 更新频率）是两个不同层面的概念：

| 维度 | Entities.Graphics LOD | Mass LOD（仿真层） |
|------|-----------------------|-------------------|
| 作用层 | 渲染层，控制画哪个 Mesh | 仿真层，控制跑哪些 System |
| 触发条件 | 摄像机到 Entity 的屏幕占比 | 距离/优先级策略 |
| 开销 | GPU Draw Call 数 | CPU System 更新开销 |
| 实现方式 | LODGroupMask + Entities.Graphics | 自定义 System 分组 |

实际项目中两者通常配合使用：远处 Entity 切换到低多边形 Mesh（渲染层 LOD），同时降低 AI 更新频率（仿真层 LOD）。

---

## 小结

- Entities.Graphics 是 ECS Entity 接入 URP/HDRP 的唯一官方桥接层，核心是 `RenderMeshArray`（SharedComponent）+ `MaterialMeshInfo`（per-Entity 下标）。
- Baking 阶段用 `RenderMeshUtility.AddComponents()` 一次性完成所有渲染组件的注册。
- GPU Instancing 不需要手动触发，相同 `RenderMeshArray` + 相同 Mesh/Material 下标的 Entity 自动合批。
- 轻量替换走 `MaterialMeshInfo` 下标，重量替换（换整组皮肤）走 `RenderMeshArray` SharedComponent 替换。
- Per-Entity 材质属性用 `[MaterialProperty]` attribute 标记 Component，系统自动同步到 GPU per-instance buffer。

下一篇 **E17「MonoBehaviour ↔ ECS 边界」** 将讨论：当 DOTS 世界需要和传统 MonoBehaviour 代码互通时，如何正确地跨越这条边界——包括从 MonoBehaviour 查询 Entity、从 System 触发 Unity 事件，以及二者共存时的陷阱。
