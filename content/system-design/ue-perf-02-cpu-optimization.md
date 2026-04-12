---
title: "Unreal 性能 02｜CPU 优化：Game Thread 瓶颈、Tick 调度与 TaskGraph 并行"
slug: "ue-perf-02-cpu-optimization"
date: "2026-03-28"
description: "Game Thread 超预算是 Unreal 项目最常见的 CPU 瓶颈。本篇深入 Tick 调度机制、碰撞与物理开销、AI 与寻路开销，以及如何用 TaskGraph 把计算推到 Worker Thread。"
tags:
  - "Unreal"
  - "性能优化"
  - "CPU"
  - "TaskGraph"
series: "Unreal Engine 架构与系统"
weight: 6240
---

`stat unit` 里 Game 时间超过预算，是 Unreal 项目最常见的 CPU 问题。本篇从 Tick 机制深入，到 TaskGraph 并行，建立完整的 Game Thread 优化认知。

---

## Game Thread 超预算的典型症状

```
stat unit 输出：
  Frame: 22.4ms
  Game:  18.1ms   ← 超出 16.7ms 预算
  Draw:   8.3ms
  GPU:   14.2ms

Unreal Insights 展开 GameThread：
  UWorld::Tick (17.8ms)
    ├─ AActor::TickActor (9.2ms)
    │    ├─ AMyEnemy::Tick × 200 (6.1ms)   ← 200 个敌人每帧 Tick
    │    └─ AMyProjectile::Tick × 80 (3.1ms)
    ├─ FPhysScene::Tick (5.4ms)             ← 物理
    └─ UNavigationSystem::Tick (3.2ms)      ← NavMesh
```

**定位步骤**：
1. `stat game` → 找到各子系统的耗时
2. Unreal Insights → 展开具体调用栈
3. 找到 Self Time 最高的函数

---

## Tick 机制深度

### FTickFunction 的注册与调度

每个需要 Tick 的 Component / Actor 都注册一个 `FTickFunction`：

```cpp
// 在 AMyActor 构造函数中
AMyActor::AMyActor()
{
    // 开启 Actor Tick（默认 true）
    PrimaryActorTick.bCanEverTick = true;

    // 设置 Tick 组（决定在 World Tick 中的执行时机）
    PrimaryActorTick.TickGroup = TG_PrePhysics;

    // 设置 Tick 间隔（不需要每帧 Tick 时）
    PrimaryActorTick.TickInterval = 0.1f; // 每 100ms Tick 一次
}
```

### TickGroup 的执行顺序

```
每帧 World Tick 的顺序：

TG_PrePhysics      → Actor/Component Tick（物理模拟前）
    ↓ 物理模拟（PhysX/Chaos）
TG_DuringPhysics   → 可以在物理运行时并行执行的 Tick
    ↓ 等待物理完成
TG_PostPhysics     → 物理模拟后（可读取物理结果）
TG_PostUpdateWork  → 所有其他 Tick 完成后（相机、网络同步等）
```

**重要**：`TG_DuringPhysics` 中的 Tick 与物理模拟并行执行，适合没有物理依赖的逻辑。

### Tick 的实际开销来源

```
即使是空 Tick 函数，每个 Actor 的 Tick 调用也有开销：
  - 函数调用（C++ 虚函数 dispatch）：约 3-5ns
  - Blueprint Tick（通过 VM 执行）：约 50-100ns（比 C++ 慢 10-20x）

1000 个 Actor 的空 Tick：
  C++：约 0.03ms
  Blueprint：约 0.05-0.1ms

1000 个 Actor 的有逻辑 Tick（AI、动画更新）：
  通常是 1-10ms，取决于逻辑复杂度
```

---

## 减少 Tick 的策略

### 1. 关闭不需要 Tick 的 Actor

```cpp
// 静态场景物体、装饰性 Actor 完全不需要 Tick
AMyDecorationActor::AMyDecorationActor()
{
    PrimaryActorTick.bCanEverTick = false; // 彻底关闭
}

// 运行时动态开关
SetActorTickEnabled(false);
MyComponent->SetComponentTickEnabled(false);
```

### 2. 使用 TickInterval 降低频率

```cpp
// 不需要每帧更新的逻辑
AMyEnemy::AMyEnemy()
{
    // AI 决策每 200ms 更新一次就够了
    PrimaryActorTick.TickInterval = 0.2f;

    // 但动画必须每帧更新
    // GetMesh()->PrimaryComponentTick.TickInterval = 0; // 保持每帧
}
```

### 3. 事件驱动替代轮询

```cpp
// ❌ 每帧轮询检测玩家是否在范围内
void AMyEnemy::Tick(float DeltaTime)
{
    APlayerCharacter* Player = FindNearbyPlayer(); // 每帧 Overlap 查询
    if (Player)
    {
        AttackPlayer(Player);
    }
}

// ✅ 用 Overlap 事件驱动
void AMyEnemy::BeginPlay()
{
    // 在 DetectionSphere 上注册事件
    DetectionSphere->OnComponentBeginOverlap.AddDynamic(
        this, &AMyEnemy::OnPlayerEnterRange);
    DetectionSphere->OnComponentEndOverlap.AddDynamic(
        this, &AMyEnemy::OnPlayerExitRange);

    // 关闭 Tick
    SetActorTickEnabled(false);
}

void AMyEnemy::OnPlayerEnterRange(...)
{
    SetActorTickEnabled(true); // 玩家进入才开启 Tick
}
```

### 4. LOD Tick（根据距离降低更新频率）

```cpp
void AMyEnemy::Tick(float DeltaTime)
{
    float DistToPlayer = GetDistanceToPlayer();

    // 近距离：每帧全逻辑
    if (DistToPlayer < 1000.f)
    {
        UpdateAI(DeltaTime);
        UpdateAnimation(DeltaTime);
        return;
    }

    // 中距离：降频 AI，简化动画
    _aiTimer += DeltaTime;
    if (_aiTimer >= 0.5f)
    {
        _aiTimer = 0;
        UpdateAI_Simple(DeltaTime);
    }

    // 远距离：停止 Tick
    if (DistToPlayer > 5000.f)
    {
        SetActorTickEnabled(false);
    }
}
```

---

## 物理与碰撞开销

### 物理 Tick 的构成

```
FPhysScene::Tick 耗时来源：
  PhysX/Chaos 模拟步进：通常 1-3ms（取决于 Rigid Body 数量）
  碰撞查询（Overlap、Raycast）：每次查询 0.01-0.1ms
  布娃娃/衣物模拟：0.5-5ms（如果开启）
```

### 碰撞优化

```cpp
// 碰撞 Channel 影响查询的对象数量
// ❌ 过于宽泛的碰撞查询
FHitResult Hit;
GetWorld()->LineTraceSingleByChannel(
    Hit,
    Start, End,
    ECC_Visibility  // 查询所有可见对象
);

// ✅ 自定义 Channel，只查询需要的对象
GetWorld()->LineTraceSingleByChannel(
    Hit,
    Start, End,
    ECC_GameTraceChannel1  // 只查询"可攻击"对象
);

// ✅ 用 ObjectType 代替 Channel（更精细的控制）
FCollisionObjectQueryParams ObjectParams;
ObjectParams.AddObjectTypesToQuery(ECC_Pawn);
GetWorld()->LineTraceMultiByObjectType(Hit, Start, End, ObjectParams);
```

### 异步物理（Chaos Async Physics）

```cpp
// UE5 的 Chaos 支持异步物理步进（不阻塞 GameThread）
// 在 Project Settings → Physics → Substepping 中开启
// 物理以独立频率运行，GameThread 不等待物理完成

// 注意：异步物理中不能直接修改 Actor Transform
// 需要通过物理 API 施加力
RigidBody->AddForce(ForceVector);
```

---

## AI 与导航开销

### NavMesh 重建开销

```cpp
// NavMesh 重建是 GameThread 杀手
// 触发条件：动态障碍物移动、地形变化

// ❌ 动态障碍物频繁移动触发重建
void AMyMovingObstacle::Tick(float DeltaTime)
{
    SetActorLocation(NewLocation); // 每帧移动 → 每帧触发 NavMesh 更新
}

// ✅ 使用 NavModifierComponent 代替重建
// 或者关闭动态障碍物的 NavMesh 影响
AMyMovingObstacle::AMyMovingObstacle()
{
    // 不影响 NavMesh（AI 会绕过它的方式改为其他）
    bNavigationRelevant = false;
}

// ✅ 如果必须动态更新，限制更新频率
GetWorld()->GetNavigationSystem()->UpdateActorInNavOctree(*this);
// 不要每帧调用，只在停止移动后调用一次
```

### AI Perception 优化

```cpp
// AIPerceptionComponent 的感知更新有开销
// 缩短感知半径 + 减小更新频率
UAIPerceptionComponent* Perception = GetAIPerceptionComponent();

// ✅ 限制感知范围
UAISenseConfig_Sight* SightConfig = CreateDefaultSubobject<UAISenseConfig_Sight>();
SightConfig->SightRadius = 1200.f;          // 不要设太大
SightConfig->MaxAge = 5.f;                  // 感知信息保留 5 秒
Perception->ConfigureSense(*SightConfig);

// ✅ 远离玩家的 AI 降低更新频率
void AMyAIController::SetLODLevel(int32 Level)
{
    if (Level == 0) // 近距离
    {
        GetBrainComponent()->SetLooping(true);
        // 行为树正常运行
    }
    else // 远距离
    {
        GetBrainComponent()->PauseLogic("Far away"); // 暂停行为树
        SetActorTickInterval(1.0f); // 每秒检查一次是否需要激活
    }
}
```

---

## TaskGraph 并行计算

### 将独立逻辑推到 Worker Thread

```cpp
// GameThread 上的独立计算 → 推到 Worker Thread
void AMyManager::Tick(float DeltaTime)
{
    // ❌ 在 GameThread 串行计算所有 NPC 路径
    for (AMyNPC* NPC : AllNPCs)
    {
        NPC->CalculateNextWaypoint(); // 假设每个耗时 0.5ms，100 个 = 50ms
    }
}

// ✅ 并行计算
void AMyManager::Tick(float DeltaTime)
{
    // 收集计算任务（必须是线程安全的数据访问）
    TArray<TSharedRef<FPathCalculationTask>> Tasks;
    for (AMyNPC* NPC : AllNPCs)
    {
        Tasks.Add(MakeShared<FPathCalculationTask>(NPC->GetPosition()));
    }

    // 分发到 Worker Thread
    TArray<TFuture<FVector>> Futures;
    for (auto& Task : Tasks)
    {
        Futures.Add(Async(EAsyncExecution::TaskGraph, [Task]()
        {
            return Task->Calculate(); // 在 Worker Thread 执行
        }));
    }

    // GameThread 继续执行其他逻辑...

    // 在需要结果时收集（此时 Worker 可能已完成）
    for (int32 i = 0; i < AllNPCs.Num(); i++)
    {
        FVector NewWaypoint = Futures[i].Get(); // 等待完成
        AllNPCs[i]->SetNextWaypoint(NewWaypoint);
    }
}
```

### 线程安全注意事项

```cpp
// ❌ 在 Worker Thread 中直接操作 UObject（不安全）
Async(EAsyncExecution::TaskGraph, [this]()
{
    SetActorLocation(NewLocation); // 崩溃！UObject 不是线程安全的
});

// ✅ 在 Worker Thread 计算，结果回到 GameThread 应用
Async(EAsyncExecution::TaskGraph, [this]()
{
    FVector Result = ExpensiveCalculation();

    // 回到 GameThread 执行 UObject 操作
    AsyncTask(ENamedThreads::GameThread, [this, Result]()
    {
        SetActorLocation(Result); // 安全
    });
});
```

---

## Blueprint vs C++ 性能边界

```
Blueprint VM 执行代价（相对 C++）：
  简单属性访问：  5-10x 慢
  函数调用：      10-20x 慢
  循环（for each）：10-50x 慢（取决于循环体）

实测（每帧调用）：
  C++ 空函数：      0.001ms
  Blueprint 空函数：0.01ms（10x 慢）

  C++ 100 次循环数学运算：0.005ms
  Blueprint 同等逻辑：    0.08ms（16x 慢）
```

**什么时候 Blueprint 性能不可接受**：

```cpp
// ❌ 这些操作放在 Blueprint 里每帧调用代价极高：

// 1. GetAllActorsOfClass（遍历所有 Actor）
TArray<AActor*> Actors;
UGameplayStatics::GetAllActorsOfClass(GetWorld(), AMyActor::StaticClass(), Actors);
// 时间复杂度 O(n)，场景大时可能 1-5ms

// 2. 每帧大量 Cast
AMyCharacter* Char = Cast<AMyCharacter>(OtherActor);
// 单次不贵，但在 Blueprint 里每帧对 100 个对象 Cast = 明显开销

// 3. 复杂的字符串操作
FString Name = Actor->GetName() + "_" + FString::FromInt(Index);
// 字符串拼接每次堆分配
```

**迁移到 C++ 的判断标准**：单帧耗时 > 0.5ms 的 Blueprint 逻辑，考虑迁移到 C++。
