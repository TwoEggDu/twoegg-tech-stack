---
title: "Unreal Mass 01｜Mass Framework 架构全景：Fragment、Tag、Trait、EntityHandle 与 DOTS 概念对照"
slug: "mass-01-framework-overview"
date: "2026-03-28"
description: "Mass Framework 和 DOTS 解决的是同一个问题——大量同构对象的高频状态更新——但命名约定和设计粒度都不同。本篇建立 Mass 的完整概念地图，并和 DOTS 做逐一对照，让已经了解 DOTS 的读者能快速建立 Mass 心智模型。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "ECS"
  - "Fragment"
  - "MassEntity"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 1
weight: 2010
---

## 为什么需要 Mass

Unreal Engine 的 Actor 系统设计目标是「通用对象」。每个 Actor 携带完整的生命周期管理、蓝图支持、网络复制钩子，代价是内存分散、虚函数开销高、缓存不友好。当场景里需要同时存在数千个士兵或数万颗子弹时，Actor 的开销就变得不可接受。

Mass Framework（插件 `MassEntity` + `MassGameplay`）是 Epic 对这一问题的回答——借鉴 Unity DOTS 的数据导向思路，在 UE 体系内构建一套面向大量同构对象的 ECS 运行时。两套系统解决的是同一个问题，但命名约定和工程集成方式都不同。本篇的目标是建立 Mass 的完整概念地图，并和 DOTS 做逐一对照。

---

## 概念对照总表

| Mass 概念 | DOTS 对应 | 关键差异 |
|---|---|---|
| `FMassFragment` | `IComponentData` | Mass Fragment 必须继承基类；DOTS 只需实现接口 |
| `FMassTag` | 零大小 `IComponentData` | 用法几乎相同，内存开销相同 |
| `FMassSharedFragment` | `ISharedComponentData` | 语义相同，Chunk 分组机制相同 |
| Trait / `UMassEntityConfigAsset` | Baker / SubScene | Trait 是编辑器配置层；Baker 是代码转换层 |
| `FMassEntityHandle` | `Entity` | 本质相同：index + serial number 轻量句柄 |
| `FMassEntityManager` | `EntityManager` | 名字相同，功能类似，API 风格不同 |
| `UMassProcessor` | `ISystem` / `SystemBase` | Mass Processor 有更强的自动依赖声明 |
| Mass Archetype（内部） | Archetype | 相同概念，相同内存布局语义 |

带着这张表继续往下读，遇到陌生 API 时可以回来对照。

---

## Fragment：Mass 的 Component

Fragment 是 Mass 里存储 Entity 状态的最小单元，对应 DOTS 的 `IComponentData`。

**定义方式**

```cpp
// Mass 做法：继承 FMassFragment（一个空 struct，只提供类型标识）
USTRUCT()
struct FMassVelocityFragment : public FMassFragment
{
    GENERATED_BODY()
    FVector Value = FVector::ZeroVector;
};

USTRUCT()
struct FMassTransformFragment : public FMassFragment
{
    GENERATED_BODY()
    FTransform Transform;
};
```

```csharp
// DOTS 做法：实现 IComponentData 接口，无需继承具体基类
public struct Velocity : IComponentData
{
    public float3 Value;
}
```

**两者的关键差异**

1. **继承 vs 接口**：Mass Fragment 必须继承 `FMassFragment`，这使得 UE 的反射系统（`USTRUCT` + `GENERATED_BODY`）能自动登记类型，无需额外注册代码。DOTS 的 `IComponentData` 是 C# interface，编译时由 Burst/IL2CPP 做进一步处理。

2. **没有 Baker 机制**：DOTS 在进入 PlayMode 之前，由 Baker 把 MonoBehaviour 数据转换成 Component。Mass 没有这一步——Fragment 在运行时由代码或 Trait 直接挂到 Entity 上，不存在"烘焙阶段"。

3. **内存布局**：两者都把相同 Archetype 的 Fragment 连续排列在 Chunk 里，保证遍历时的缓存局部性。

---

## Tag：零大小标记

Tag 用于标记 Entity 的状态或类别，本身不携带数据，只参与 Query 过滤。

```cpp
USTRUCT()
struct FMassEnemyTag : public FMassTag
{
    GENERATED_BODY()
};

USTRUCT()
struct FMassDeadTag : public FMassTag
{
    GENERATED_BODY()
};
```

在 Processor 里的用法：

```cpp
// 只处理带 FMassEnemyTag、且不带 FMassDeadTag 的 Entity
EntityQuery.AddTagRequirement<FMassEnemyTag>(EMassFragmentPresence::All);
EntityQuery.AddTagRequirement<FMassDeadTag>(EMassFragmentPresence::None);
```

**和 DOTS Tag Component 的对比**

DOTS 里习惯用零大小的 `IComponentData` 充当 Tag（`struct Dead : IComponentData {}`），行为和 Mass Tag 几乎完全一致：不占 Chunk 数据空间，只影响 Archetype 分类。语义上两者等价，Mass 只是通过继承 `FMassTag` 基类让意图更明确。

---

## Shared Fragment：跨 Entity 共享数据

当多个 Entity 共用同一份配置（例如同一种武器的伤害参数），重复存储会浪费内存。Mass 用 Shared Fragment 解决这个问题：

```cpp
USTRUCT()
struct FMassWeaponSharedFragment : public FMassSharedFragment
{
    GENERATED_BODY()
    float Damage = 10.f;
    float FireRate = 0.5f;
};
```

Shared Fragment 的值相同的 Entity 会被归入同一个 Chunk，这和 DOTS `ISharedComponentData` 的 Chunk 分组语义完全一致。修改某个 Entity 的 Shared Fragment 值，会导致它在 Archetype 内部移动到另一个 Chunk——这一点两个系统的代价模型相同，需要注意高频修改的开销。

---

## Trait：编辑器配置层

DOTS 通过 Baker + SubScene 在编辑器里组织 Entity 的初始状态。Mass 提供了一套更面向策划友好的配置层：**Trait**。

`UMassEntityConfigAsset` 是一个数据资产，可以在编辑器里拖拽组合多个 Trait。每个 Trait 负责向 Entity 添加一组 Fragment、Tag 和 Shared Fragment：

```cpp
UCLASS()
class UMassMovementTrait : public UMassEntityTraitBase
{
    GENERATED_BODY()
public:
    virtual void BuildTemplate(FMassEntityTemplateBuildContext& BuildContext,
                                const UWorld& World) const override
    {
        // 这个 Trait 被激活时，自动为 Entity 添加以下 Fragment
        BuildContext.AddFragment<FMassVelocityFragment>();
        BuildContext.AddFragment<FMassTransformFragment>();
        BuildContext.AddTag<FMassMovingTag>();

        // 绑定 Shared Fragment 的初始值
        FMassMovementParameters& Params =
            BuildContext.SetSharedFragment<FMassMovementSharedFragment>();
        Params.MaxSpeed = MaxSpeed;
    }

    UPROPERTY(EditAnywhere)
    float MaxSpeed = 600.f;
};
```

**和 DOTS Baker 的比较**

- DOTS Baker 是代码层：开发者写 C# 代码把 GameObject 上的组件转换成 ECS Component，灵活但需要编写胶水代码。
- Mass Trait 是配置层：策划可以直接在编辑器里拼装 Trait，Trait 内部的 `BuildTemplate` 由程序员实现一次，之后复用。

两种做法各有侧重。Mass 的模式更适合「程序提供积木，策划搭积木」的协作流程。

---

## EntityHandle 与 EntityManager

### FMassEntityHandle

`FMassEntityHandle` 是对 Entity 的轻量引用：

```cpp
// 引擎源码（简化）
struct FMassEntityHandle
{
    int32 Index   = 0;   // Archetype 内部的槽位索引
    int32 SerialNumber = 0;  // 用于检测悬空句柄（Entity 已销毁）
};
```

结构和 DOTS `Entity`（index + version）完全相同。句柄本身不持有数据，不能直接读写 Fragment——必须通过 `FMassEntityManager` 进行。

### FMassEntityManager

`FMassEntityManager` 是 Mass 的核心运行时，负责：

- Archetype 的注册与内存管理
- Entity 的创建、销毁、Fragment 的添加/移除
- Chunk 内数据的直接读写

在 Processor 外部访问 EntityManager 的方式：

```cpp
// 在任意 UObject 或 Actor 内
UMassEntitySubsystem* EntitySubsystem =
    GetWorld()->GetSubsystem<UMassEntitySubsystem>();

FMassEntityManager& EntityManager = EntitySubsystem->GetMutableEntityManager();

// 创建一个 Entity
FMassEntityHandle NewEntity = EntityManager.CreateEntity(ArchetypeHandle);

// 读取 Fragment
FMassVelocityFragment& Vel =
    EntityManager.GetFragmentDataChecked<FMassVelocityFragment>(NewEntity);
Vel.Value = FVector(100.f, 0.f, 0.f);
```

DOTS 的 `EntityManager` API 功能类似，但 Mass 的版本更直接——没有 `EntityCommandBuffer` 的概念，Processor 执行期间可以选择直接写入（在 `Defer()` 保护下操作结构变更）。

---

## Mass 在 Unreal 里的位置

### 插件结构

Mass 以两个插件的形式集成进 UE 5.4/5.5：

- **MassEntity**：核心 ECS 运行时（Fragment、Tag、Archetype、EntityManager、Processor）
- **MassGameplay**：上层功能模块（移动、感知、表现层 LOD、寻路集成）

在 `ProjectName.uproject` 里激活：

```json
{
  "Plugins": [
    { "Name": "MassEntity",   "Enabled": true },
    { "Name": "MassGameplay", "Enabled": true },
    { "Name": "MassAI",       "Enabled": true }
  ]
}
```

### 和 Actor 世界的关系

Mass Entity **不是** Actor，没有 Transform Component、没有碰撞、没有蓝图。它只是 Archetype Chunk 里的一行数据。

但 Mass 提供了 **Representation** 机制（`MassRepresentation` 模块）：在近距离用 Actor / Skeletal Mesh 表示，在远距离降级为 Static Mesh Instance 或完全隐藏。这让 Mass Entity 在逻辑上保持轻量，同时在视觉上能接入 UE 渲染管线。

DOTS 的 `Hybrid` 模式（Managed Component + ECS）解决同一问题，思路类似，但 Mass 的 LOD 分层更内置、开箱即用。

---

## 小结

Mass Framework 的概念体系可以用一句话概括：**Fragment 是数据，Tag 是标记，Trait 是模板，EntityHandle 是指针，EntityManager 是仓库，Processor 是系统。** 和 DOTS 相比，最大的不同不在于内存模型（两者几乎相同），而在于工程集成方式——Mass 依托 UE 的反射系统和插件机制，把配置层和代码层分得更清楚。

下一篇「Mass-02｜UMassProcessor 执行模型：Query 构建、依赖声明与并行调度」将深入 Processor 的生命周期，讲清楚 `ConfigureQueries`、`Execute`、`ExecutionOrder` 三者的关系，以及 Mass 如何在不写一行 Job 代码的情况下实现自动并行。
