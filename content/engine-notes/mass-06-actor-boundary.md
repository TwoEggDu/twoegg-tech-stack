---
title: "Unreal Mass 06｜Mass 与 Actor 世界的边界：Representation Fragment、ISM、Niagara 与 DOTS 边界的对比"
slug: "mass-06-actor-boundary"
date: "2026-03-28"
description: "Mass Entity 没有视觉表示，它只是数据。要让玩家看到 NPC，需要通过 Representation Fragment 把 Mass Entity 和 ISM（Instanced Static Mesh）或 Actor 关联起来。本篇讲清楚这套关联机制，以及为什么 Mass 比 DOTS 更容易和现有 Unreal 项目混用。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "ISM"
  - "Actor"
  - "Representation"
  - "Niagara"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 6
weight: 2060
---

## Entity 本身是不可见的

Mass Entity 是一组 Fragment 的集合：位置、速度、AI 状态……但它本身不渲染任何东西。这不是缺陷，而是设计选择——渲染是一个独立关切，不应该污染数据模型。

```cpp
// Entity 只有数据 Fragment，没有任何渲染状态
FMassEntityHandle Entity = EntityManager->CreateEntity(
    FMassArchetypeHandle,
    FTransformFragment{},
    FMassMoveTargetFragment{},
    FMyAIStateFragment{}
    // 注意：没有 MeshComponent，没有 Actor 引用
);
```

要让玩家看到这个 Entity，需要一套独立机制把 Entity 的 Transform 同步给渲染层。这套机制就是 **Representation**。

---

## 三种表示方式

根据距离和精度需求，Mass 提供三个层级的视觉表示：

### ISM（Instanced Static Mesh）：轻量化大量渲染

ISM 是 GPU Instancing 的封装。一次 Draw Call 渲染数千个实例，每个实例只有一个 Transform 矩阵的差异。适合远处没有骨骼动画的 NPC——士兵群体、人群背景、飞行的鸟群。

代价：无逐实例动画，无复杂材质交互。

### Actor（带 SkeletalMesh）：高精度近处表示

靠近玩家时，ISM 的静态外观会穿帮。此时 Mass 会 Spawn 一个真实的 Actor，挂载 SkeletalMesh，执行 AnimBlueprint。这和普通 Unreal NPC 没有区别——开销当然也一样。

适合：LOD0 范围内的交互 NPC，需要骨骼动画、布料模拟、IK 的角色。

### Niagara Particle：极大量的群体效果

当群体数量进入十万级别，即便是 ISM 也会面临 CPU 侧的 Transform 更新瓶颈。此时可以把 Entity 的位置数据写入 Niagara 的 Buffer，由 GPU 完成粒子渲染。每个"NPC"退化为一个粒子，但你仍然保有 Mass 侧的逻辑模拟。

适合：蚂蚁群、鱼群、观众席背景人群——视觉上存在，但不参与精细交互。

### 三者配合 LOD 使用

实际项目中，同一个 NPC 在不同距离使用不同表示：

```
距离 > 100m   →  Niagara Particle（纯视觉）
距离 30-100m  →  ISM（Transform 同步，静态 Mesh）
距离 < 30m    →  Actor + SkeletalMesh（完整动画）
```

切换逻辑由 `UMassRepresentationProcessor` 自动驱动。

---

## Representation Fragment 的工作机制

### FMassRepresentationFragment

每个需要视觉表示的 Entity 携带这个 Fragment：

```cpp
// MassRepresentationTypes.h（引擎源码简化版）
USTRUCT()
struct FMassRepresentationFragment : public FMassFragment
{
    GENERATED_BODY()

    // 当前使用的表示类型
    EMassRepresentationType CurrentRepresentation = EMassRepresentationType::None;

    // 上一帧的表示类型（用于检测切换）
    EMassRepresentationType PrevRepresentation = EMassRepresentationType::None;

    // ISM 表示时，对应的实例索引
    int32 ISMInstanceIndex = INDEX_NONE;

    // Actor 表示时，对应的 Actor 引用
    TObjectPtr<AActor> RepresentedActor = nullptr;

    // 高精度 Actor 表示时的 Handle
    FMassEntityHandle HighResEntityHandle;
};
```

`CurrentRepresentation` 的值在每帧由 Processor 根据 LOD 结果更新。

### UMassRepresentationProcessor

这个 Processor 的职责是：读取 LOD Fragment，决定每个 Entity 应该用哪种表示，然后执行切换。

```cpp
void UMassRepresentationProcessor::Execute(
    FMassEntityManager& EntityManager,
    FMassExecutionContext& Context)
{
    Context.GetMutableFragmentView<FMassRepresentationFragment>().ForEach(
        [&](FMassRepresentationFragment& RepresentationFragment,
            const FMassLODFragment& LODFragment,
            const FTransformFragment& TransformFragment)
        {
            const EMassLOD::Type CurrentLOD = LODFragment.LOD;

            // LOD2 及以上：切到 ISM
            if (CurrentLOD >= EMassLOD::Medium)
            {
                SwitchToISM(RepresentationFragment, TransformFragment);
            }
            // LOD0：切到 Actor
            else if (CurrentLOD == EMassLOD::High)
            {
                SwitchToActor(RepresentationFragment, TransformFragment);
            }
        }
    );
}
```

切换发生时，旧表示会被清理（Actor Despawn 归池，ISM 实例释放），新表示被初始化。整个过程对 Entity 数据透明——Entity 的 Fragment 不关心自己现在是 ISM 还是 Actor。

---

## ISM 集成的完整流程

ISM 的核心是一个 Shared Fragment，多个同类型 Entity 共用同一个 `UInstancedStaticMeshComponent`：

```cpp
// 注册 ISM Shared Fragment
USTRUCT()
struct FMassISMSharedComponent : public FMassSharedFragment
{
    GENERATED_BODY()

    UPROPERTY()
    TObjectPtr<UInstancedStaticMeshComponent> ISMComponent = nullptr;
};
```

Processor 在每帧把 Entity 的 `FTransformFragment` 同步给 ISM 实例：

```cpp
// 自定义 NPC ISM 同步 Processor
void UNPCISMSyncProcessor::Execute(
    FMassEntityManager& EntityManager,
    FMassExecutionContext& Context)
{
    // 只处理当前使用 ISM 表示的 Entity
    auto ISMQuery = Context.FilterQuery(
        FMassRepresentationFragment::StaticStruct(),
        FTransformFragment::StaticStruct(),
        FMassISMSharedComponent::StaticStruct()
    );

    Context.ForEachEntityChunk(ISMQuery, [&](FMassExecutionContext& ChunkContext)
    {
        auto RepFragments  = ChunkContext.GetMutableFragmentView<FMassRepresentationFragment>();
        auto TransFragments = ChunkContext.GetFragmentView<FTransformFragment>();
        auto& ISMShared    = ChunkContext.GetMutableSharedFragment<FMassISMSharedComponent>();

        UInstancedStaticMeshComponent* ISMComp = ISMShared.ISMComponent;
        if (!ISMComp) return;

        for (int32 i = 0; i < ChunkContext.GetNumEntities(); ++i)
        {
            const int32 InstanceIndex = RepFragments[i].ISMInstanceIndex;
            if (InstanceIndex == INDEX_NONE) continue;

            // 直接把 Entity Transform 写入 ISM 实例
            ISMComp->UpdateInstanceTransform(
                InstanceIndex,
                TransFragments[i].GetTransform(),
                /*bWorldSpace=*/true,
                /*bMarkRenderStateDirty=*/false, // 批量更新，最后统一标脏
                /*bTeleport=*/false
            );
        }

        // 批量更新完成后一次性标脏，避免每帧多次重建渲染数据
        ISMComp->MarkRenderStateDirty();
    });
}
```

关键点：`bMarkRenderStateDirty=false` 配合最后一次 `MarkRenderStateDirty()` 是 ISM 批量更新的标准写法，避免 O(N) 次渲染状态重建。

---

## Actor 集成：Pool 与双向同步

### Actor Pool

频繁 Spawn/Despawn Actor 会触发大量 GC 压力。Mass 内置 Actor Pool 机制：

```cpp
// 在 MassRepresentationSubsystem 中配置 Actor Pool 大小
RepresentationSubsystem->SetActorPoolSize(ANPCHighResActor::StaticClass(), 50);
```

当 Entity 进入 LOD0 时，从池中取出一个 Actor 并激活；退出 LOD0 时，Actor 被重置并归还到池中，而不是销毁。

### 双向同步

Entity → Actor 方向（每帧）：

```cpp
// Entity 的 Transform 驱动 Actor 位置
if (AActor* Rep = RepFragment.RepresentedActor)
{
    Rep->SetActorTransform(TransformFragment.GetTransform());
}
```

Actor → Entity 方向（输入事件）：

```cpp
// 玩家与高精度 Actor 交互后，通过 Signal 通知 Entity
UMassSignalSubsystem* SignalSubsystem = GetWorld()->GetSubsystem<UMassSignalSubsystem>();
SignalSubsystem->SignalEntity(
    UMassStateTreeSignals::OnInteracted,
    EntityHandle
);
```

这个设计保证了即便 Actor 逻辑写在 Blueprint 里，事件结果仍然能反馈回 Mass 的数据层。

---

## 与 DOTS Entities.Graphics 的对比

DOTS（Unity ECS）通过 `Entities.Graphics` 包实现 Entity 的渲染，机制和 Mass Representation 有明显差异：

| 维度 | DOTS Entities.Graphics | Mass Representation |
|------|----------------------|---------------------|
| 渲染机制 | GPU Instancing（自动批处理）| ISM（手动管理实例）|
| 骨骼动画 | 需要 `Entities.Graphics` 动画扩展，成熟度有限 | Actor SkeletalMesh，完整 AnimBlueprint 支持 |
| 和引擎集成 | 依赖 URP/HDRP，Built-in 管线不支持 | 原生 Unreal 渲染管线，无额外依赖 |
| LOD 联动 | 需要手动实现 LOD 切换逻辑 | 内置 Representation + LOD Processor 联动 |
| 混用 GameObject | 需要 `IConvertGameObjectToEntity` 或手动桥接 | Actor 直接接收 Mass Signal，无需转换 |
| 上手成本 | 需要完全转换思维，旧 MonoBehaviour 无法直接用 | 旧 Actor 逻辑可以保留，只有需要性能的部分迁移到 Mass |

### 为什么 Mass 更容易混用

DOTS 的核心问题不是技术，而是边界：一旦进入 ECS 世界，你的 GameObject/MonoBehaviour 就必须转换或放弃。项目早期很容易低估这个代价。

Mass 的 Representation 机制天然处理了"有些 Entity 是 Actor、有些是 ISM"的混合状态。在同一个场景里，你可以：

- 主角和 Boss：普通 Actor，完整 Blueprint 逻辑，不动
- 中距离 NPC：Mass Entity + Actor 表示，只有 AI 状态在 Mass 里
- 远距离人群：Mass Entity + ISM，纯数据驱动

三种存在形式共存，共用同一套 Mass Signal 通信机制，没有强制的架构割裂。

另一个现实因素：Mass 是 Unreal 原生组件，随引擎更新维护；DOTS 在 Unreal 里根本不存在。选择 Unreal 的团队不需要面对"要不要把引擎换成 Unity"的决策，Mass 就是你在 Unreal 里的选项。

---

## 小结

Mass Entity 是纯数据，视觉表示是独立关切：

- **ISM** 处理大量远距离静态 NPC，一次 Draw Call，Transform 批量同步
- **Actor** 处理近距离高精度 NPC，完整骨骼动画，通过对象池控制开销
- **Niagara** 处理极大量视觉群体，GPU 侧渲染，Mass 侧逻辑
- **Representation Fragment** 记录当前状态，**Representation Processor** 根据 LOD 自动切换

对比 DOTS，Mass 的优势不在于渲染性能本身，而在于**混用成本**：现有 Actor 逻辑不需要重写，只有需要规模化的部分才迁移进 Mass。

下一篇 **Mass-07「Mass 实战案例拆解」** 会把这些机制放进一个完整的项目场景：一个有 5000 个 NPC 的城镇，从 EntityConfig 设计、Processor 分层，到 Representation 切换阈值的调优，逐步拆解一个可落地的架构方案。
