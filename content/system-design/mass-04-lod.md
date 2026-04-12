---
title: "Unreal Mass 04｜Mass LOD：Fragment 分级激活、距离驱动的精度切换，DOTS 没有内置对应物"
slug: "mass-04-lod"
date: "2026-03-28"
description: "Mass LOD 是 Mass 比 DOTS 内置得更完整的地方——它在框架层面提供了基于距离的仿真精度切换，让高密度 NPC 在远处降频更新，靠近时恢复完整仿真。本篇讲清楚 LOD Fragment、Viewer 机制和距离配置。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "LOD"
  - "FMassLODFragment"
  - "仿真精度"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 4
weight: 2040
---

## 为什么仿真层也需要 LOD

渲染层的 LOD 人尽皆知：远处模型换低面数 Mesh，GPU 开销下降。但在 ECS 架构里，**仿真层同样需要 LOD**，而且往往比渲染更迫切。

考虑一个场景：地图上有 10000 个 NPC。如果每帧对所有 NPC 执行完整 AI 决策（路径查询、感知检测、状态机跳转），按每个 NPC 0.1ms 计算，总开销就是 1000ms——直接打爆帧预算，游戏根本跑不起来。

问题的本质是：**玩家感知不到 400 米外 NPC 的 AI 精度**。一个在地图边缘游荡的 NPC，即使每 4 帧才更新一次路径，玩家也不会察觉。CPU 省下来的算力，可以用于更近的 NPC 或其他系统。

Unity DOTS 在框架层面没有内置这套仿真 LOD 机制。如果你在 DOTS 里想做同等效果，需要从零开始手写：距离计算、Component 启停、System 跳帧逻辑。Unreal Mass 框架则把这套机制作为一等公民内置进来，开箱即用。

---

## MassLOD 的三个核心概念

### FMassLODFragment

这是挂在每个 Entity 上的 LOD 状态数据，定义如下（简化版）：

```cpp
USTRUCT()
struct MASSLODSYSTEM_API FMassLODFragment : public FMassFragment
{
    GENERATED_BODY()

    // 当前 LOD 级别：High(0) / Medium(1) / Low(2) / Off(3)
    int8 LOD = MAX_int8;

    // 上一帧的 LOD 级别，用于检测是否发生切换
    int8 PrevLOD = MAX_int8;
};
```

LOD 级别枚举对应四种仿真精度：
- `High`（LOD 0）：完整仿真，每帧执行所有 Processor
- `Medium`（LOD 1）：中等精度，降频更新
- `Low`（LOD 2）：低精度，极低频更新或简化逻辑
- `Off`（LOD 3）：完全跳过，Processor 遍历时直接忽略

### FMassViewerInfoFragment

```cpp
USTRUCT()
struct MASSLODSYSTEM_API FMassViewerInfoFragment : public FMassFragment
{
    GENERATED_BODY()

    // 该 Entity 到最近 Viewer 的距离平方（避免开根号）
    float ClosestViewerDistanceSq = FLT_MAX;
};
```

这个 Fragment 记录 Entity 距离所有 Viewer 中最近一个的距离。LOD Processor 每帧更新这个值，下游 Processor 读取它。

### Viewer

Viewer 代表"观察点"，通常是玩家的 Pawn。Mass LOD 系统支持多个 Viewer——在多人游戏里，每个在线玩家都是一个 Viewer。系统会对每个 Entity 找到距离最近的 Viewer，以该距离决定 LOD 级别。

Viewer 通过 `UMassLODSubsystem` 注册，框架会在 PlayerController Possess Pawn 时自动处理这部分逻辑。

---

## LOD 参数配置

`FMassLODParameters` 是在 Trait 中配置各级别距离阈值和更新频率的核心结构：

```cpp
// 在你的 Trait 头文件中
UPROPERTY(EditAnywhere, Category = "Mass|LOD")
FMassLODParameters LODParams;
```

在 Trait 的 `BuildTemplate` 函数里把参数写入 Entity 配置：

```cpp
void UMyNPCLODTrait::BuildTemplate(
    FMassEntityTemplateBuildContext& BuildContext,
    const UWorld& World) const
{
    BuildContext.AddFragment<FMassLODFragment>();
    BuildContext.AddFragment<FMassViewerInfoFragment>();

    FMassLODParameters& Params =
        BuildContext.AddFragment_GetRef<FMassLODParameters>();

    // LOD 0: 0 ~ 50m，每帧更新
    Params.LODDistance[0] = 50.f * 100.f;     // UE 单位：厘米
    Params.LODMaxCountPerViewer[0] = 100;

    // LOD 1: 50 ~ 150m，每 2 帧更新
    Params.LODDistance[1] = 150.f * 100.f;
    Params.LODMaxCountPerViewer[1] = 200;

    // LOD 2: 150 ~ 400m，每 4 帧更新
    Params.LODDistance[2] = 400.f * 100.f;
    Params.LODMaxCountPerViewer[2] = 500;

    // LOD Off: 400m 以外，跳过遍历
    // LODDistance[3] 不设置，超过 LODDistance[2] 即为 Off
}
```

`LODMaxCountPerViewer` 是另一个重要参数：它限制每个 Viewer 周围各 LOD 级别的最大 Entity 数量。当区域内 NPC 密集时，超出数量的 Entity 会被强制降级到更低 LOD。

---

## LOD Processor 的工作流程

`UMassLODProcessor` 是框架内置的核心 Processor，每帧按以下顺序工作：

1. 从 `UMassLODSubsystem` 获取当前所有 Viewer 的世界坐标
2. 遍历所有持有 `FMassViewerInfoFragment` 的 Entity，计算每个 Entity 到最近 Viewer 的距离平方，写入 Fragment
3. 根据距离和 `FMassLODParameters` 的阈值，更新每个 Entity 的 `FMassLODFragment`（设置 `LOD` 和 `PrevLOD`）

下游的 AI Processor 读取 LOD 状态决定执行策略：

```cpp
void UMyAIProcessor::Execute(
    FMassEntityManager& EntityManager,
    FMassExecutionContext& Context)
{
    // 获取当前帧计数，用于跳帧
    const int32 FrameCount = GFrameCounter;

    EntityManager.ForEachEntityChunk(
        EntityQuery, Context,
        [FrameCount](FMassExecutionContext& Ctx)
    {
        const TConstArrayView<FMassLODFragment> LODList =
            Ctx.GetFragmentView<FMassLODFragment>();
        TArrayView<FAIStateFragment> AIList =
            Ctx.GetMutableFragmentView<FAIStateFragment>();

        for (int32 i = 0; i < Ctx.GetNumEntities(); ++i)
        {
            const int8 LOD = LODList[i].LOD;

            // Off：完全跳过
            if (LOD >= EMassLOD::Off) { continue; }

            // Low：每 4 帧执行一次
            if (LOD == EMassLOD::Low && (FrameCount % 4 != 0)) { continue; }

            // Medium：每 2 帧执行一次
            if (LOD == EMassLOD::Medium && (FrameCount % 2 != 0)) { continue; }

            // High：每帧执行完整 AI 逻辑
            RunFullAI(AIList[i]);
        }
    });
}
```

---

## Fragment 分级激活（LOD Collector）

仅降频更新还不够极致。当 Entity 进入 Low 或 Off 级别时，那些只有 High 级别才用到的 Fragment（比如详细感知数据、路径缓存）其实根本不需要存在于内存里。

`FMassLODCollector` Trait 提供了基于 LOD 切换自动添加/删除 Fragment 的能力：

```cpp
// LOD 切换时的回调，在 Processor 里处理
void UMyLODCollectorProcessor::Execute(
    FMassEntityManager& EntityManager,
    FMassExecutionContext& Context)
{
    EntityManager.ForEachEntityChunk(
        EntityQuery, Context,
        [&EntityManager](FMassExecutionContext& Ctx)
    {
        const TConstArrayView<FMassLODFragment> LODList =
            Ctx.GetFragmentView<FMassLODFragment>();

        for (int32 i = 0; i < Ctx.GetNumEntities(); ++i)
        {
            const FMassEntityHandle Entity = Ctx.GetEntity(i);
            const int8 CurLOD  = LODList[i].LOD;
            const int8 PrevLOD = LODList[i].PrevLOD;

            // LOD 没有发生变化，跳过
            if (CurLOD == PrevLOD) { continue; }

            // 从 High 降级到 Low：删除详细 AI Fragment，节省内存
            if (PrevLOD == EMassLOD::High && CurLOD >= EMassLOD::Low)
            {
                EntityManager.RemoveFragmentFromEntity(
                    Entity, FDetailedAIFragment::StaticStruct());
            }

            // 从 Low 升回 High：重新添加详细 AI Fragment
            if (PrevLOD >= EMassLOD::Low && CurLOD == EMassLOD::High)
            {
                EntityManager.AddFragmentToEntity(
                    Entity, FDetailedAIFragment::StaticStruct());
            }
        }
    });
}
```

Fragment 的增删会触发 Entity 的 Archetype 迁移，这有一定开销。实践中建议把切换频率较高的 Fragment 改用 `FMassTag` 配合 Enableable 机制处理，仅对真正大块数据（路径点数组、感知历史缓冲）做实际迁移。

---

## DOTS 里的等价实现

Unity DOTS 没有内置仿真 LOD 系统。如果要在 DOTS 里复现 Mass LOD 的效果，需要自己实现三个部分：

**第一步：距离计算 System**

每帧遍历所有 Entity，计算与主相机的距离，写进 `LODDistanceComponent`。多个 Camera（多人游戏）还要取最小值，逻辑要自己写。

**第二步：LOD 状态更新 System**

读取距离值，对比阈值，更新 `LODLevelComponent`（值：0/1/2/3）。如果要支持 `LODMaxCount` 类似的数量上限逻辑，还需要对当前帧所有 Entity 做排序或计数，复杂度更高。

**第三步：仿真精度控制**

使用 DOTS 的 `IEnableableComponent` 来启停高精度 Component，或通过 Archetype 切换实现 Fragment 级别的增删。System 跳帧则需要手动维护帧计数器并在每个 System 入口处检查。

完整实现下来，DOTS 侧通常需要编写 50~100 行的基础设施代码，而 Mass 侧只需要在 Trait 里填写约 20 行参数配置，其余由框架负责。这不是语法繁简的差距，而是框架层有没有把这个问题当作通用问题来解决。

---

## 实际性能收益

Epic 在 City Sample（《黑客帝国：觉醒》技术 Demo）中公开的数字可以作为参考基准：场景中约 10000 个 NPC，启用 Mass LOD 后 CPU 仿真开销相比全量 High LOD 降低约 60%~80%。

收益来自两个层面：

- **跳帧**：Medium/Low 级别的 Entity 每次 Processor 执行时大量跳过，减少了 AI 逻辑、物理查询等开销
- **完全跳过遍历**：Off 级别的 Entity 通过 Mass 的 Chunk 过滤机制完全不进入 Processor 的遍历循环，不消耗任何 CPU——这是最大的收益来源

合理配置 Off 距离（通常是玩家实际可见范围的 1.5~2 倍）可以让绝大多数 Entity 在大多数时刻处于 Off 状态，而玩家几乎感知不到仿真空洞。

---

## 小结

Mass LOD 体系的设计思路是：**把"什么都模拟"变成"按需模拟"**。`FMassLODFragment` 是状态载体，`FMassViewerInfoFragment` 是距离数据，`FMassLODParameters` 是策略配置，`UMassLODProcessor` 是每帧驱动引擎。四个角色分工明确，下游 Processor 只需要读一个 `int8` 就能决定自己该不该跑。

DOTS 开发者如果遇到大规模 Entity 的性能问题，通常会意识到需要做这件事，但框架不帮你做，所以往往在每个项目里重新发明轮子。Mass 把这个轮子造好内置进来，是框架层的务实选择。

下一篇 [Mass 05｜Mass Signals：事件驱动的 Entity 通信，替代 Processor 轮询的正确姿势](../mass-05-signals) 会讲解 Mass 里如何用信号机制替代轮询，让 Entity 之间的通信从"每帧检查有没有事件"变成"有事件才触发"。
