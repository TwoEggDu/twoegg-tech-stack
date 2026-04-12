---
title: "Dedicated Server 性能优化：Tick 率、物理精简、AI 精简、渲染关闭"
slug: "game-backend-ded-srv-04-performance"
date: "2026-04-04"
description: "DS 不渲染画面，CPU 为什么还是跑满？优化方向和客户端完全不同，从 Tick 密度到 GC 压力，一篇讲清楚。"
tags:
  - "游戏后端"
  - "Dedicated Server"
  - "性能优化"
  - "Tick 率"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 24
weight: 3024
---

# Dedicated Server 性能优化：Tick 率、物理精简、AI 精简、渲染关闭

## 问题空间：DS 不渲染，为什么 CPU 还是跑满了

你把游戏部署到云服务器，监控显示 DS 进程的 CPU 占用率持续在 80-95%。但这个进程根本没有 GPU，没有渲染调用——CPU 在忙什么？

一台云服务器跑 4 个游戏房间实例，服务器费用远超预期。你需要优化到每台机器跑 8 个实例，但不知道从哪里下手。

更诡异的是：当房间里只有 2 个玩家时，DS 的 CPU 占用和 8 个玩家时几乎一样高——玩家数量对 CPU 负载的影响比预想的小得多。

这三个问题指向的是 DS 的真实瓶颈：**不是渲染，而是 Tick 密度、物理模拟、AI 计算和 GC 压力**。这些负载和玩家数量的关系不是线性的，和渲染毫无关系，也和客户端性能优化的方向几乎完全不同。

## 抽象模型：DS 的 CPU 负载来源分解

从高到低，DS 的主要 CPU 负载来源通常是：

```
DS CPU 负载来源（典型分布）
├── Physics Simulation         ~20-35%
│   ├── Collision Detection
│   ├── Rigidbody Integration
│   └── Raycast Queries
├── Game Logic Tick            ~15-25%
│   ├── Character/Entity Update
│   ├── Ability System Tick
│   └── State Machine Update
├── AI Processing              ~10-20%
│   ├── Pathfinding
│   ├── Behavior Tree Tick
│   └── Perception System
├── Network Processing         ~10-15%
│   ├── State Replication
│   ├── Packet Serialization
│   └── Relevancy Calculation
├── GC / Memory                ~5-15%
│   ├── Object Allocation
│   └── GC Collection Pauses
└── Misc (I/O, Logging, etc)   ~5-10%
```

这个分布告诉你：即使彻底关闭物理，DS 的 AI 和游戏逻辑 Tick 仍然占据大量 CPU；即使把 AI 全部关掉，物理和网络处理还在。**DS 性能优化是多个子系统的综合工程**，不存在一个"杀手锏"优化。

更重要的洞察：**这些负载和帧率（Tick 率）直接成正比**。如果你把 Tick 率从 60Hz 降到 30Hz，理论上所有 Tick 驱动的逻辑负载都减半——这是 DS 优化里回报最高的单一动作。

## Tick 率：最重要的优化旋钮

### 不同游戏类型的 Tick 率选择

Tick 率不是越高越好，也不是越低越好，它由游戏类型的交互精度需求决定：

| 游戏类型 | 推荐服务端 Tick 率 | 理由 |
|---------|-----------------|------|
| 竞技 FPS（CS:GO 类） | 64-128 Hz | 子弹判定需要高精度时间分辨率 |
| 竞技 FPS（团队战术类） | 30-64 Hz | 可以接受轻微的判定延迟 |
| MOBA / 格斗 | 30 Hz | 技能判定有容错窗口 |
| 合作射击 / ARPG | 20-30 Hz | 玩家对精确判定要求不高 |
| 回合策略 | 10-20 Hz | 甚至可以按事件驱动，不用固定 Tick |
| MMO 区域服务器 | 10-20 Hz | 单区域玩家数百，Tick 率必须低 |

**CS:GO 的 128Hz 服务器为什么贵**：在 128Hz 下，每台服务器能同时托管的游戏房间数量大约是 64Hz 服务器的一半，因为所有 Tick 驱动的逻辑计算量翻倍了。这是硬件成本和竞技体验之间的直接权衡。

### Unity 中设置服务端 Tick 率

```csharp
void ConfigureServerTickRate()
{
    // 关闭 VSync（DS 上必须关闭）
    QualitySettings.vSyncCount = 0;

    // 设置目标帧率（即 Tick 率）
    Application.targetFrameRate = 30;

    // 固定物理步长（与逻辑帧解耦）
    // 注意：物理步长不一定要和逻辑帧率相同
    Time.fixedDeltaTime = 1f / 60f;  // 物理可以跑更高频率
}
```

一个常见的误解：`Time.fixedDeltaTime` 控制物理步长，`Application.targetFrameRate` 控制逻辑 Tick 频率，两者可以独立设置。逻辑 Tick 30Hz，物理 Tick 60Hz，是完全合法的配置。

### Unreal 中设置服务端 Tick 率

在 `DefaultEngine.ini` 里：

```ini
[/Script/Engine.Engine]
; 固定服务端帧率为 30Hz
bUseFixedFrameRate=true
FixedFrameRate=30.0
```

或者在代码里动态控制：

```cpp
// GameMode::InitGame 里
if (GetNetMode() == NM_DedicatedServer)
{
    GEngine->SetMaxFPS(30.0f);
}
```

## 物理精简

物理系统是 DS 上最值得精简的子系统，因为 DS 需要"权威物理"，但不需要"表现物理"。

### 关闭客户端表现物理

**布料模拟（Cloth Simulation）**：布料抖动是纯视觉效果，DS 上关闭。

Unity：
```csharp
#if UNITY_SERVER
    var cloth = GetComponent<Cloth>();
    if (cloth != null) Destroy(cloth);
#endif
```

Unreal：在 Server Target 的 Build.cs 里可以禁用 Chaos Cloth 模块；或者在 Actor 的 Skeletal Mesh 设置里关闭 `Enable Clothing Simulation`，通过 `bUseServerClothSimulation = false` 这样的配置控制。

**粒子物理**：Niagara / Shuriken 粒子系统的物理碰撞（Particle Collision）在 DS 上没有任何游戏逻辑价值，应该完全禁用。

**非必要 Rigidbody**：场景里的装饰性物理对象（可以被踢飞的垃圾桶、飘散的纸张）——如果它们的位置不影响游戏逻辑，在 DS 上不应该有 Rigidbody，甚至不需要这些 Actor。

### 精简碰撞形状

一个很少被提到的优化：**复杂碰撞形状的代价远大于简单形状**。

DS 上的角色碰撞通常只需要胶囊体（Capsule Collider）用于移动和命中判定，不需要精确的 Mesh 碰撞（它在客户端上用于近身格挡动画反馈之类的表现）。

对于 AI 敌人尤其重要：如果 AI 角色在客户端有 10 个 Collider 用于细节碰撞，在 DS 上精简到 2-3 个（胶囊体 + 必要的攻击判定区域），每帧的碰撞检测开销会显著下降。

### Physics Query 的批量化

DS 上的 Raycast 和 SphereCast 查询往往分散在每个角色的 `Update`/`Tick` 里，每帧产生大量独立的 Physics Query。

优化方向：**Physics Query 批量化**——把同一帧内多个角色的射线检测收集到一个统一的 Job/Task 里，利用 Unity Physics（DOTS Physics）或 Unreal 的异步 LineTrace 批量执行：

```csharp
// Unity DOTS Physics 示例：批量射线检测
// 比每个角色单独 Raycast 性能提升 2-5x
var raycastCommands = new NativeArray<RaycastCommand>(enemies.Count, Allocator.TempJob);
var raycastHits = new NativeArray<RaycastHit>(enemies.Count, Allocator.TempJob);

for (int i = 0; i < enemies.Count; i++)
{
    raycastCommands[i] = new RaycastCommand(
        enemies[i].position, enemies[i].forward, 
        new QueryParameters(), attackRange
    );
}

var handle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 32);
handle.Complete();
```

## AI 优化

DS 上的 AI 优化思路和客户端不同：**客户端 AI 优化关注表现质量（动画、对话），DS 的 AI 优化关注决策频率（多久更新一次行为树）**。

### 服务端 AI 和客户端 AI 的职责分离

DS 上的 AI 只需要做**决策**：
- 寻路（Pathfinding）：去哪里
- 目标选择（Target Selection）：攻击谁
- 行为树决策（Behavior Tree）：现在做什么动作

DS 上的 AI **不需要做**：
- 动画状态混合（客户端做）
- 视觉反馈（表情、动作细节）（客户端做）
- 声音触发（客户端做）

一个典型的职责划分：

```cpp
// Unreal AI Controller 里的职责区分
void AMyAIController::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    // 只在服务端运行决策逻辑
    if (GetNetMode() == NM_DedicatedServer || GetNetMode() == NM_ListenServer)
    {
        UpdateTargetSelection();
        UpdatePathing();
        // 行为树由引擎自动 Tick，不需要在这里手动调用
    }

    // 客户端表现（永远不在 DS 上运行）
#if !UE_SERVER
    UpdateAnimationParameters();
    UpdateVoiceLines();
#endif
}
```

### AI LOD（Level of Detail for AI）

距离玩家越远的 AI，更新频率应该越低。这个原则在客户端 AI 里也适用，但在 DS 上更关键——DS 上可能同时运行数十个 AI，如果所有 AI 都以 30Hz Tick，CPU 负载会直接线性叠加。

一个简单但有效的 AI LOD 实现：

```csharp
// Unity 示例：AI Tick 频率按距离分层
public class AILODController : MonoBehaviour
{
    private float _tickInterval;
    private float _timeSinceLastTick;

    void UpdateTickInterval()
    {
        float distToNearestPlayer = GetDistanceToNearestPlayer();

        if (distToNearestPlayer < 30f)
            _tickInterval = 1f / 30f;   // 30Hz：近距离全精度
        else if (distToNearestPlayer < 80f)
            _tickInterval = 1f / 10f;   // 10Hz：中距离
        else
            _tickInterval = 1f / 3f;    // 3Hz：远距离最低精度
    }

    void Update()
    {
        _timeSinceLastTick += Time.deltaTime;
        if (_timeSinceLastTick >= _tickInterval)
        {
            _timeSinceLastTick = 0;
            ExecuteAITick();
        }
    }
}
```

这个策略可以在 AI 数量多的场景里把 AI 相关的 CPU 负载降低 40-60%，同时玩家几乎感知不到远距离 AI 决策频率的变化（因为网络延迟本身就会掩盖这种差异）。

## 网络发送频率优化

DS 的网络部分有两个独立的优化维度：**每帧发送多少数据**（带宽）和**多高频率发送**（帧率对齐）。

### 状态广播批量化

每帧给所有连接的客户端发送所有 Actor 的状态更新，是 DS 网络处理的最大开销来源之一。

**按优先级分层发送**：

- 高优先级（每逻辑帧发送）：玩家角色位置、血量、技能状态
- 中优先级（每 2-4 帧发送一次）：NPC 位置、环境对象状态
- 低优先级（每 8-16 帧发送一次）：环境装饰、非交互 Actor

Unity NGO 通过 `NetworkVariable` 的 `SendTickRate` 可以控制发送频率：

```csharp
// 只在每个逻辑帧发送
public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

// 低频更新，适合非关键状态
public NetworkVariable<int> Score = new NetworkVariable<int>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
) { SendTickRate = 5 };  // 每秒最多更新 5 次
```

### Relevancy 裁剪

并不是所有客户端都需要收到所有 Actor 的状态更新——距离太远的 Actor 对客户端不相关（Not Relevant），可以完全跳过网络复制。

Unreal 的 `IsNetRelevantFor()` 方法控制相关性；Unity NGO 可以自定义 `NetworkObject` 的相关性逻辑。合理设置相关性半径可以将 DS 的网络出站流量减少 30-50%。

## GC / 内存优化

DS 上的内存优化有两个目标：**减少资产内存占用**（让每台机器能跑更多实例）和**减少 GC 压力**（避免 GC 停顿影响帧率稳定性）。

### 不加载贴图和音频资产

DS 不需要渲染贴图，也不需要播放音频。但如果你的关卡 Blueprint 里有对 Texture 或 Sound 资产的引用，Unity/Unreal 仍然可能把这些资产加载到内存里。

服务器资产剥离需要主动处理：
- 在 Cook 配置里标记服务器平台不需要这些资产类型
- 在代码里用条件编译包裹所有对贴图/音频资产的引用，确保服务器代码路径不会触发这些加载

一个典型效果：一个 2GB 内存占用的客户端进程，对应的 DS 实例内存占用通常在 400-800MB——因为贴图（通常占客户端内存的 50-70%）在 DS 上完全不需要。

### 减少 GC 分配

在高 Tick 率的 DS 上，每帧的对象分配会快速触发 GC。针对 DS 的 GC 优化原则：

1. **避免在 Tick/Update 里 new 对象**：用对象池（Object Pool）替代频繁的 Instantiate/Destroy
2. **用 struct 替代 class 存储网络状态快照**：减少堆分配
3. **PreAllocate 网络数据包缓冲区**：避免每帧 new byte[]

```csharp
// 错误做法：每帧分配新的列表
void SendUpdates()
{
    var updates = new List<PlayerState>();  // 每帧分配
    // ...
}

// 正确做法：复用缓冲区
private readonly List<PlayerState> _updateBuffer = new List<PlayerState>(64);
void SendUpdates()
{
    _updateBuffer.Clear();  // 不分配新对象
    // ...
}
```

## DS 的 Profiling 方法

DS 没有交互式界面，传统的 Profiler 使用方式不适用。几种可行的非交互式 Profiling 方案：

### Unity Profiler 远程连接

Unity Profiler 支持通过网络连接到运行中的 DS 进程（即使是无头进程）：

```bash
# DS 启动时开启 Profiler 监听
./GameServer -batchmode -nographics -profiler-enable -profiler-log-file /tmp/profile.raw
```

然后在 Unity Editor 里通过 `Window > Analysis > Profiler`，选择 `Remote` 连接到服务器 IP。

### 自定义性能日志

对于生产环境，推荐在 DS 代码里内置轻量的性能日志：

```csharp
// 每 30 秒输出一次关键指标
void LogPerformanceMetrics()
{
    Debug.Log($"[Perf] FPS: {1f/Time.deltaTime:F1} | " +
              $"Players: {_connectedPlayers} | " +
              $"ActiveAI: {_activeAICount} | " +
              $"PhysicsObjects: {Physics.GetOverlappingCount()} | " +
              $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
}
```

这些日志可以被 ELK Stack / Grafana 等监控系统收集，形成随时间变化的性能趋势图，比快照式的 Profiler 更适合排查生产环境的性能退化。

## 工程边界

**优化要有 Profiling 数据支撑**：在没有数据的情况下猜测"物理是瓶颈"或"AI 是瓶颈"，很可能把时间花在错误的方向上。先跑 Profiler，找出实际的 top hotspot，再优化。

**Tick 率降低可能影响游戏体验**：20Hz 的 Tick 率对于 FPS 游戏会导致明显的命中感延迟（每帧 50ms），玩家能够感知。优化 Tick 率之前，确认你的游戏类型和玩家预期可以接受。

**DS 的单实例性能上限是硬件的 1/N**：如果目标是每台机器跑 N 个实例，那么每个实例的资源上限是总资源的 1/N。这个目标要在游戏设计阶段就确定，而不是在运维阶段"压榨"出来。

## 最短结论

DS 的 CPU 瓶颈是 **Tick 驱动的逻辑密度**，不是渲染。

最高回报的优化路径按顺序是：

1. **固定 Tick 率**（降低 Tick 率是最高效的整体优化，副作用最大，需要先确认游戏类型能接受）
2. **物理精简**（关闭表现物理，精简碰撞形状，批量化 Query）
3. **AI LOD**（按距离降低 AI 更新频率）
4. **网络发送分层**（Relevancy 裁剪 + 按优先级控制发送频率）
5. **GC 优化**（对象池 + 避免热路径分配）

每个优化都要先用 Profiling 数据确认是实际瓶颈，再下手。
