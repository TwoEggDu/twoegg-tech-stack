---
title: "Unreal Mass 05｜Mass Signals：跨 Entity 异步事件机制，解决纯 DOD 里的突发事件问题"
slug: "mass-05-signals"
date: "2026-03-28"
description: "纯数据导向的 ECS 擅长批量状态更新，但不擅长处理「某个 Entity 死了」这类突发事件。Mass Signals 是 Mass 框架对这个问题的有意识补充——DOTS 没有直接对应物，这一点值得深入理解。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "Mass Signals"
  - "事件"
  - "ECS"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 5
weight: 2050
---

## ECS 擅长的和不擅长的

ECS 架构的核心优势是**批量状态更新**：每帧遍历所有拥有 `HealthFragment` 的 Entity，统一扣血、统一更新位置、统一衰减 Buff 时长。数据紧密排列，Cache 命中率高，SIMD 友好，这是 DOD 设计的本意。

但这个模型在面对**突发事件**时会遇到麻烦。

考虑这样一个场景：这一帧，第 3472 号 Entity 的 HP 刚好被扣到 0。ECS 里没有"事件回调"这个概念，Processor 只是在遍历数据，它不知道这个 Entity 比上一帧多发生了什么。要处理死亡逻辑，通常有两种传统做法：

**方案 A：每帧检查**——在伤害 Processor 之后，再跑一个死亡检测 Processor，遍历所有 Entity 判断 `HP <= 0`。这个 Processor 大多数情况下什么都不做，却每帧都在遍历。

**方案 B：维护死亡列表**——伤害 Processor 写一个线程局部列表，遍历结束后合并，再传给死亡 Processor。这能减少遍历，但需要额外的同步逻辑，而且"列表在哪里存、谁来清空"很快会变成架构负担。

Mass Signals 是 Mass 框架对这个问题给出的有意识的答案。

---

## Signal 的本质

Mass Signal 是一个**从 Processor 发出的、定向到特定 Entity 的异步通知**。

"定向"是关键词。普通的 Processor 查询的是满足条件的所有 Entity；Signal 驱动的 Processor 只会处理**收到了该 Signal 的 Entity**。没收到 Signal 的 Entity，即使满足 Query 的其他条件，也不会被这次执行触及。

"异步"也是关键词。Signal 不是立即触发，而是进入 `UMassSignalSubsystem` 的队列，在**下一帧**（或当帧末，取决于调度配置）由框架统一派发给订阅了该 Signal 的 Processor。

这两点合在一起，决定了 Mass Signals 特别适合**稀疏、突发**的事件：大多数帧大多数 Entity 什么都不发，只有少数 Entity 在少数帧触发，处理成本与触发数量正比，而不是与 Entity 总量正比。

---

## 发送 Signal

在任意 Processor 的 `Execute` 里，拿到 `UMassSignalSubsystem` 的引用后即可发送：

```cpp
// 在 Processor 的 Execute 或任意能拿到 World 的地方
UMassSignalSubsystem* SignalSubsystem =
    World->GetSubsystem<UMassSignalSubsystem>();

// 向单个 Entity 发信号
SignalSubsystem->SignalEntity(
    UE::Mass::Signals::OnDeath,
    EntityHandle
);

// 向多个 Entity 批量发信号（更高效）
TArray<FMassEntityHandle> DyingEntities;
// ... 填充列表 ...
SignalSubsystem->SignalEntities(
    UE::Mass::Signals::OnDeath,
    DyingEntities
);
```

`UE::Mass::Signals::OnDeath` 是一个 `FName`，你可以在项目里定义任意自定义 Signal 名：

```cpp
namespace MyGame::Signals
{
    const FName OnEnterAlert  = TEXT("OnEnterAlert");
    const FName OnPickupItem  = TEXT("OnPickupItem");
    const FName OnStateChange = TEXT("OnStateChange");
}
```

---

## 接收 Signal：订阅与 Processor 实现

接收方是一个普通的 `UMassProcessor` 子类，只需要在 `Initialize` 里声明订阅关系：

```cpp
// DeathProcessor.h
UCLASS()
class UDeathProcessor : public UMassProcessor
{
    GENERATED_BODY()
public:
    UDeathProcessor();
protected:
    virtual void Initialize(UObject& Owner) override;
    virtual void ConfigureQueries() override;
    virtual void Execute(FMassEntityManager& EntityManager,
                         FMassExecutionContext& Context) override;
private:
    FMassEntityQuery DeathQuery;
};
```

```cpp
// DeathProcessor.cpp
UDeathProcessor::UDeathProcessor()
{
    // 让这个 Processor 在伤害 Processor 之后运行
    ExecutionOrder.ExecuteAfter.Add(UE::Mass::ProcessorGroupNames::Behavior);
}

void UDeathProcessor::Initialize(UObject& Owner)
{
    Super::Initialize(Owner);

    // 订阅 Signal：当 OnDeath Signal 到达时，框架会驱动这个 Processor
    if (UMassSignalSubsystem* SignalSubsystem =
            GetWorld()->GetSubsystem<UMassSignalSubsystem>())
    {
        // RegisterSignalWithNames 将 Signal 名与本 Processor 绑定
        SubscribeToSignal(*SignalSubsystem, UE::Mass::Signals::OnDeath);
    }
}

void UDeathProcessor::ConfigureQueries()
{
    // Query 正常声明所需的 Fragment
    // 框架会自动把查询范围限制为「收到该 Signal 的 Entity」
    DeathQuery.AddRequirement<FHealthFragment>(EMassFragmentAccess::ReadOnly);
    DeathQuery.AddRequirement<FTransformFragment>(EMassFragmentAccess::ReadOnly);
    DeathQuery.RegisterWithProcessor(*this);
}

void UDeathProcessor::Execute(FMassEntityManager& EntityManager,
                              FMassExecutionContext& Context)
{
    DeathQuery.ForEachEntityChunk(EntityManager, Context,
        [](FMassExecutionContext& Ctx)
        {
            const auto& HealthList    = Ctx.GetFragmentView<FHealthFragment>();
            const auto& TransformList = Ctx.GetFragmentView<FTransformFragment>();

            for (int32 i = 0; i < Ctx.GetNumEntities(); ++i)
            {
                // 这里只会执行到「收到 OnDeath Signal 的 Entity」
                // 可以安全地播放死亡特效、标记销毁、通知 Actor 世界等
                const FVector DeathPos =
                    TransformList[i].GetTransform().GetLocation();
                // SpawnDeathEffect(DeathPos);
                Ctx.Defer().DestroyEntity(Ctx.GetEntity(i));
            }
        });
}
```

`SubscribeToSignal` 内部调用的正是 `RegisterSignalWithNames`，它将 Signal 名与 Processor 实例绑定到 `UMassSignalSubsystem` 的订阅表里。框架在派发 Signal 时会查这张表，找到对应 Processor，然后只向它传递"有信号"的 Entity 集合。

---

## 工作流程全貌

```
当帧（伤害 Processor 执行）
  └─ 某 Entity HP 降至 0
       └─ SignalSubsystem->SignalEntity(OnDeath, EntityHandle)
            └─ Signal 进入内部队列

帧末 / 下帧开始
  └─ SignalSubsystem 派发队列
       └─ 找到订阅了 OnDeath 的 Processor（UDeathProcessor）
            └─ 将收到 Signal 的 Entity 列表注入 DeathQuery
                 └─ UDeathProcessor::Execute 只迭代这些 Entity
```

关键细节：Signal 有**一帧延迟**。这意味着你无法在同帧内"发送并立即响应"。对于大多数突发事件（死亡、状态转换、拾取），一帧延迟不是问题；但如果你需要同帧内的因果链（比如某个 Processor 的输出必须在同帧内被另一个 Processor 消费），Signal 不适合，应该用 Processor 的执行顺序依赖（`ExecuteAfter`）来解决。

---

## DOTS 的等价方案

Unity DOTS 没有内置的 Signal 机制。面对同样的问题，常见的替代方案有三种：

**替代方案 1：Enableable Component**
定义一个 `IEnableableComponent` 标记组件，事件发生时 Enable 它，专用 System 每帧检查有该组件且处于 Enable 状态的 Entity，处理完后 Disable。DOTS 对 Enableable Component 用 bit mask 做了优化，跳过 Disabled Entity 的成本较低，但仍然是全量扫描结构，不是"只处理有信号的 Entity"。

**替代方案 2：NativeQueue 收集 + 独立 System**
事件发生时向一个 `NativeQueue<Entity>` 写入，下帧用独立 System 消费这个队列。逻辑清晰，但需要手动管理队列的分配、清空和线程安全。多个 System 需要共享队列时，还需要额外的 Singleton Component 或 SystemState 来传递引用。

**替代方案 3：ECB + Tag**
用 `EntityCommandBuffer` 在事件发生时 `AddComponent<DeathTag>`，下帧专用 System 用 `WithAll<DeathTag>` 查询并处理，处理完后移除 Tag。这是 DOTS 里最惯用的模式，语义清晰，但每次事件都会触发 Archetype 变更（Entity 在 Chunk 间移动），对于高频事件有额外开销。

**横向对比：**

| 维度 | Mass Signals | DOTS Enableable | DOTS ECB+Tag |
|---|---|---|---|
| 内置支持 | 是 | 是 | 是 |
| 稀疏事件效率 | 高（只处理有信号的 Entity） | 中（全量扫描但跳过 Disabled） | 中（Archetype 变更有开销） |
| 实现复杂度 | 低（订阅即可） | 中（需管理 Enable/Disable 生命周期） | 低（ECB 模式成熟） |
| 同帧处理 | 不支持（一帧延迟） | 支持 | 不支持（ECB 下帧执行） |
| 自定义事件类型 | FName，任意扩展 | 需要不同组件类型 | 需要不同 Tag 类型 |

Mass Signals 对"稀疏、突发"事件更优化，代价是框架绑定（只能在 Mass 体系内使用）和一帧延迟。DOTS 的替代方案更通用，但需要自己搭建事件分发的基础设施。

---

## 适合与不适合的场景

**适合用 Signal 的场景：**
- **死亡事件**：HP 降至 0，触发死亡动画、掉落道具、通知 AI 系统
- **状态机转换**：AI 从 Patrol 进入 Alert，需要初始化警戒状态的 Fragment 数据
- **碰撞/交互事件**：Entity 进入触发区，需要执行一次性逻辑
- **任何"某帧某个 Entity 发生了某件事"的模式**，且触发频率远低于 Entity 总量

**不适合用 Signal 的场景：**
- **高频全量触发**：如果每帧有 80% 的 Entity 都会发出某个 Signal，那和全量遍历没有区别，反而多了队列和派发的开销
- **需要同帧响应**：Signal 有一帧延迟，不能用于同帧因果链
- **跨系统广播**：Signal 是 Mass 内部机制，无法直接通知 Actor、GameplaySystem 等 Mass 体系外的对象（这个问题在下一篇讨论）

---

## 小结

Mass Signals 填补了纯 ECS 在突发事件处理上的空白：它不是用"全量遍历 + 条件检查"来发现事件，而是用"显式发送 + 定向派发"来响应事件。对于稀疏触发的场景，这是质的效率差异。

DOTS 在这个问题上没有内置的对等机制，但通过 Enableable Component、NativeQueue 或 ECB+Tag 可以达到相似的效果，代价是需要自己管理事件分发的基础设施。理解这个差异，有助于在 Mass 和 DOTS 之间做出更有根据的架构决策。

下一篇，我们来到一个更现实的问题：**Mass Entity 和 Actor 世界如何共存**。Mass 擅长处理大量无 Actor 的轻量 Entity，但游戏里总有一些对象必须是 Actor（有物理、有动画、有 Gameplay 能力）。Mass 提供了 `UMassAgentComponent` 和 Fragment 同步机制来打通这两个世界，但边界在哪里、同步的代价是什么，是需要仔细权衡的设计问题。

→ 下一篇：[Mass 06｜Mass 与 Actor 世界的边界：UMassAgentComponent 与双向同步](../mass-06-actor-bridge)
