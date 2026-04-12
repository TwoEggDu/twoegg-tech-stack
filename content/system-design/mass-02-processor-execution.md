---
title: "Unreal Mass 02｜UMassProcessor 执行模型：Query、依赖声明、Pipeline 与 DOTS System 的对比"
slug: "mass-02-processor-execution"
date: "2026-03-28"
description: "UMassProcessor 的调度比 DOTS ISystem 更「自动」——通过 Fragment 读写声明自动推导依赖关系，不需要手动指定 UpdateBefore/UpdateAfter。本篇讲清楚这套自动调度的工作原理，以及它的代价和限制。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "UMassProcessor"
  - "ECS"
  - "调度"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 2
weight: 2020
---

## 从「手动排序」到「自动推导」

DOTS 开发者对 `[UpdateBefore(typeof(SomeSystem))]` 一定不陌生。每当系统之间有顺序依赖，就要在属性标签里手写一遍，漏写了就是隐性 bug，多写了又容易形成循环依赖。Mass 的设计者选择了另一条路：**让 Processor 自己声明数据读写意图，由调度器自动推导执行顺序**。

代价是：推导结果有时过于保守。理解这套机制的边界，是写出高性能 Mass 代码的前提。

---

## 1. UMassProcessor 基本结构

Mass 的逻辑单元是 `UMassProcessor`，对应 DOTS 里的 `ISystem` / `SystemBase`。每个 Processor 必须重写两个函数：

```cpp
UCLASS()
class UMoveProcessor : public UMassProcessor
{
    GENERATED_BODY()

public:
    UMoveProcessor();

protected:
    virtual void ConfigureQueries() override;
    virtual void Execute(FMassEntityManager& EntityManager,
                         FMassExecutionContext& Context) override;

private:
    FMassEntityQuery EntityQuery;
};
```

- **`ConfigureQueries()`**：在此声明 Query，告诉调度器「我读哪些 Fragment，写哪些 Fragment」。
- **`Execute()`**：实际逻辑，每帧被调度器调用。
- **`FMassEntityQuery`**：Query 对象，可以挂多个，每个独立声明访问权限。

### 一个完整的移动 Processor

```cpp
// 构造函数：声明执行阶段和 Net Mode
UMoveProcessor::UMoveProcessor()
{
    ExecutionFlags = static_cast<int32>(EProcessorExecutionFlags::AllNetModes);
    ProcessingPhase = EMassProcessingPhase::PrePhysics;
    bAutoRegisterWithProcessingPhases = true;
}

void UMoveProcessor::ConfigureQueries()
{
    // 读写速度，只读变换（写变换需要单独的 TransformProcessor）
    EntityQuery.AddRequirement<FMassVelocityFragment>(EMassFragmentAccess::ReadWrite);
    EntityQuery.AddRequirement<FTransformFragment>(EMassFragmentAccess::ReadOnly);

    // 必须有移动能力 Tag，排除已死亡
    EntityQuery.AddTagRequirement<FMassMovableTag>(EMassFragmentPresence::All);
    EntityQuery.AddTagRequirement<FMassDeadTag>(EMassFragmentPresence::None);

    // 将 Query 注册到 Processor，调度器从这里读取依赖信息
    EntityQuery.RegisterWithProcessor(*this);
}

void UMoveProcessor::Execute(FMassEntityManager& EntityManager,
                              FMassExecutionContext& Context)
{
    EntityQuery.ForEachEntityChunk(
        EntityManager, Context,
        [](FMassExecutionContext& Ctx)
        {
            const float DeltaTime = Ctx.GetDeltaTimeSeconds();
            TArrayView<FMassVelocityFragment> Velocities =
                Ctx.GetMutableFragmentView<FMassVelocityFragment>();
            TConstArrayView<FTransformFragment> Transforms =
                Ctx.GetFragmentView<FTransformFragment>();

            for (int32 i = 0; i < Ctx.GetNumEntities(); ++i)
            {
                // 示例：简单速度衰减
                Velocities[i].Value *= (1.0f - DeltaTime * 2.0f);
            }
        });
}
```

`ForEachEntityChunk` 以 **Chunk** 为粒度遍历，这与 DOTS 的 `IJobChunk` 是同一层抽象——同一 Archetype 的连续内存块，缓存友好。

---

## 2. 自动依赖推导：工作原理与代价

调度器在 Pipeline 构建阶段（不是每帧，而是启动时）遍历所有注册 Processor，收集每个 Query 的 Fragment 读写声明，然后按以下规则生成依赖图：

| 情况 | 结论 |
|------|------|
| A 写 Fragment X，B 也写 Fragment X | A、B 必须串行 |
| A 写 Fragment X，B 读 Fragment X | A、B 必须串行（先写后读或先读后写，取决于其他依赖） |
| A 读 Fragment X，B 读 Fragment X | A、B 可以并行 |

这套规则等价于经典的读写锁语义，**实现上 Mass 使用有向无环图（DAG）来表达依赖，拓扑排序后生成最终执行序列**。

### 与 DOTS 的对比

DOTS 里你必须写：

```csharp
[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct MySystem : ISystem { ... }
```

忘写 `UpdateAfter` 不会报错，只会在特定帧产生一帧延迟的竞态 bug，排查极难。

Mass 的自动推导消除了这个问题，但引入了另一个：**推导粒度是「Fragment 类型」，不是「具体字段」**。两个 Processor 只要都声明了对同一 Fragment 的写权限，调度器就会串行化它们——即使它们实际上修改的是完全不同的 Entity 集合（因为 Tag 过滤不同）。调度器目前不分析 Tag 过滤的交集，只看 Fragment 类型。

> **实践建议**：拆分 Fragment 粒度。把频繁写入的数据分散到多个 Fragment（如 `FMassVelocityFragment`、`FMassAccelerationFragment`），而不是塞进一个大 `FMassMovementFragment`，可以让更多 Processor 并行。

---

## 3. ExecutionFlags：控制执行环境

构造函数里有三个关键配置：

### `ExecutionFlags`

```cpp
// 在所有 NetMode 下执行（Client、Server、Standalone）
ExecutionFlags = static_cast<int32>(EProcessorExecutionFlags::AllNetModes);

// 仅在 GameClient（含 ListenServer）上执行——适合纯表现层 Processor
ExecutionFlags = static_cast<int32>(EProcessorExecutionFlags::GameClient);

// 仅在 DedicatedServer 上执行——适合纯逻辑 Processor
ExecutionFlags = static_cast<int32>(EProcessorExecutionFlags::GameServer);
```

这让同一个 Processor 类可以做到「逻辑/表现分离」，不用在代码里写 `if (GetNetMode() == NM_DedicatedServer)`。

### `bAutoRegisterWithProcessingPhases`

默认为 `true`，Processor 实例化后自动注册到 `ProcessingPhase` 对应的 Pipeline。设为 `false` 时需要手动调用 `RegisterWithProcessingPhases()`，适合动态启停某类逻辑。

### `bRequiresGameThreadExecution`

```cpp
bRequiresGameThreadExecution = true; // 默认 false
```

Mass 的 Processor 默认在任意线程执行。**访问 `UObject`、`AActor`、调用蓝图、读写 `GWorld` 时必须设为 `true`**，否则会触发线程安全断言。代价是该 Processor 无法和其他 Processor 并行，成为 Pipeline 里的串行瓶颈。

---

## 4. Pipeline：执行阶段与物理 Tick 的绑定

Mass 的执行阶段与 UE 的物理 Tick 深度绑定：

```cpp
enum class EMassProcessingPhase : uint8
{
    PrePhysics,    // 物理模拟前（逻辑更新、AI 决策）
    StartPhysics,  // 物理模拟开始时
    EndPhysics,    // 物理模拟结束后（读取物理结果）
    PostPhysics,   // 物理完成后（动画、表现同步）
    FrameEnd,      // 帧末尾（清理、状态提交）
    NumPhases
};
```

对比 DOTS 的 SystemGroup：

| DOTS SystemGroup | 典型用途 | 对应 Mass Phase |
|-----------------|---------|----------------|
| `InitializationSystemGroup` | 实体创建、数据初始化 | — （Mass 用 Initializer） |
| `SimulationSystemGroup` | 逻辑模拟 | `PrePhysics` / `PostPhysics` |
| `PresentationSystemGroup` | 渲染同步 | `FrameEnd` |

两套体系的核心差异在于：DOTS 的 SystemGroup 是纯逻辑概念，与引擎 Tick 的绑定由 `[WorldSystemFilter]` 控制，相对松散。**Mass 的 Phase 直接映射到 `FTickFunction` 的不同 `TickGroup`**，调度时序与物理引擎的关系更确定，也更容易推理「我的 Processor 执行时物理状态是什么」。

---

## 5. Query 高级写法

### Tag 过滤

```cpp
// 必须同时具有两个 Tag
EntityQuery.AddTagRequirement<FMassMovableTag>(EMassFragmentPresence::All);
EntityQuery.AddTagRequirement<FMassSimulatedTag>(EMassFragmentPresence::All);

// 不能有 DeadTag（已死亡的实体跳过）
EntityQuery.AddTagRequirement<FMassDeadTag>(EMassFragmentPresence::None);

// 可选存在（Optional——有就读，没有也不报错）
EntityQuery.AddTagRequirement<FMassStunnedTag>(EMassFragmentPresence::Optional);
```

Tag 只影响哪些 Entity 进入遍历，**不影响调度器的依赖推导**。这正是前面提到的保守推导问题的根源。

### 多 Query 的 Processor

一个 Processor 可以声明多个 `FMassEntityQuery`，每个 Query 独立注册：

```cpp
void UCombatProcessor::ConfigureQueries()
{
    AttackQuery.AddRequirement<FMassAttackFragment>(EMassFragmentAccess::ReadWrite);
    AttackQuery.AddTagRequirement<FMassDeadTag>(EMassFragmentPresence::None);
    AttackQuery.RegisterWithProcessor(*this);

    DefendQuery.AddRequirement<FMassHealthFragment>(EMassFragmentAccess::ReadWrite);
    DefendQuery.RegisterWithProcessor(*this);
}
```

调度器会把所有 Query 的读写声明合并后计算该 Processor 的依赖。

---

## 6. DOTS vs Mass 调度对比表

| 维度 | DOTS ISystem / SystemBase | UMassProcessor |
|------|--------------------------|----------------|
| **依赖声明** | 手动 `[UpdateBefore]` / `[UpdateAfter]` | 自动从 Fragment 读写声明推导 |
| **推导保守性** | 精确（你怎么写就怎么排） | 偏保守（Tag 过滤不参与推导） |
| **执行粒度** | System | Processor |
| **主线程强制** | `[BurstCompile]` 默认多线程，需显式用 `EntityCommandBuffer` 延迟结构变更 | `bRequiresGameThreadExecution = true` |
| **阶段划分** | SystemGroup（逻辑分组） | `EMassProcessingPhase`（与物理 Tick 绑定） |
| **并行单位** | Chunk（`ScheduleParallel`） | Chunk（`ForEachEntityChunk` + TaskGraph） |
| **Net Mode 过滤** | `[WorldSystemFilter]` | `ExecutionFlags` |
| **自动注册** | 默认注册到 World | `bAutoRegisterWithProcessingPhases` |

---

## 小结

UMassProcessor 的执行模型可以用一句话概括：**「你告诉我读什么写什么，我来决定先后顺序」**。这套模型降低了大规模 Processor 协作的心智负担，但要获得真正的并行效益，需要有意识地拆分 Fragment 粒度、用 Tag 隔离逻辑域，以及谨慎标记 `bRequiresGameThreadExecution`。

Pipeline Phase 与物理 Tick 的绑定是 Mass 区别于 DOTS 的一个实用设计——在 AI 模拟、物理驱动的移动这类场景中，能更直觉地推理执行时序。

---

**下一篇 [Mass-03：Structural Change——Fragment 的添加、删除与实体的 Archetype 迁移](../mass-03-structural-change/)** 将深入 Mass 最复杂的操作：运行时改变实体的 Fragment 组成。这对应 DOTS 里的 `EntityCommandBuffer.AddComponent`，但 Mass 的实现路径和限制有所不同。
