---
title: "服务端 ECS 06｜C# 服务端 ECS：Arch、Leopotam 定位，与 Unity DOTS Headless Server 的对比"
slug: "sv-ecs-06-csharp-server-ecs"
date: "2026-03-28"
description: "对 Unity 技术栈的团队，服务端 ECS 有三条 C# 路线：独立的 Arch/Leopotam、DOTS Headless Server、或混合方案。选择依据是能否和客户端代码共享，以及是否愿意绑定 Unity 运行时。"
tags:
  - "服务端"
  - "ECS"
  - "C#"
  - "DOTS"
  - "Arch"
  - "游戏服务器"
  - "高性能"
  - "框架选型"
series: "高性能游戏服务端 ECS"
primary_series: "server-ecs"
series_role: "article"
series_order: 6
weight: 2360
---

上一篇讲完了 Flecs 和 EnTT 的 C++ 侧选型逻辑。这一篇转向 C# 阵营——对 Unity 技术栈的团队来说，服务端 ECS 有三条明确的 C# 路线可以选择，但它们的定位和适用场景差异很大，混淆了容易踩坑。

## 三条路线的基本轮廓

**路线一：Arch**。纯 .NET 实现的 Archetype ECS 框架，高性能，不依赖 Unity 运行时，可以直接在 .NET 服务端使用。

**路线二：Leopotam ECS-lite**。极简 ECS 框架，API 设计接近 OOP 习惯，学习曲线低，适合没有 ECS 背景的团队快速上手。

**路线三：Unity DOTS Headless Server**。把 Unity + Entities 1.x 以无渲染模式跑在服务端，最大优势是可以完整复用客户端的 ECS 代码。

这三条路线并不是性能排名，而是三种不同的"绑定程度"选择：是否要绑定 Unity 运行时，以及是否需要和客户端共享同一套 ECS 代码。后面逐一展开。

---

## 一、Arch：.NET 原生高性能 ECS

[Arch](https://github.com/genaray/Arch) 是目前 .NET 生态里最接近 Unity DOTS 设计理念的独立 ECS 框架。它使用 Archetype/Chunk 存储结构（SoA 布局），与 DOTS Entities 的内存模型高度相似，但完全不依赖 Unity 运行时。

基础用法如下：

```csharp
using Arch.Core;
using Arch.Core.Extensions;

var world = World.Create();

// 创建实体——Archetype 根据组件类型自动推断
world.Create(new Position { X = 0, Y = 0 }, new Velocity { X = 1, Y = 0 });

// 定义查询
var query = new QueryDescription()
    .WithAll<Position, Velocity>();

// 高性能遍历——通过 ref 参数直接操作内存
world.Query(in query, (ref Position pos, ref Velocity vel) => {
    pos.X += vel.X;
    pos.Y += vel.Y;
});

// 实体和 World 的销毁
world.Destroy(entity);
World.Destroy(world);
```

几个值得关注的性能细节：

- 组件数据按 Archetype 分 Chunk 存储，同一 Archetype 的实体在内存里连续排布，Query 遍历时 CPU Cache 命中率高。
- 没有 Burst Compiler 支持——这是它和 DOTS 最核心的区别。在 JIT 环境下（.NET 服务端默认是 JIT），Arch 的单核吞吐量约为 DOTS + Burst 的 60~80%，但相比其他 Sparse Set ECS 框架仍有明显优势。
- 支持多线程 Query（通过 `ParallelQuery`），可以利用服务器多核优势。

**适合场景**：需要高性能 ECS，但不想引入 Unity 运行时依赖；或者客户端用 Unity DOTS，但服务端希望保持纯 .NET 部署（容器化更简单，许可证更清晰）。

---

## 二、Leopotam ECS-lite

[Leopotam ECS-lite](https://github.com/Leopotam/ecslite) 的设计哲学和 Arch 完全不同。它不追求极致的内存布局性能，而是追求 API 的清晰和可读性。

```csharp
class VelocitySystem : IEcsRunSystem {
    EcsPool<Position> _positions;
    EcsPool<Velocity> _velocities;
    EcsFilter _filter;

    public void Init(IEcsSystems systems) {
        var world = systems.GetWorld();
        _positions = world.GetPool<Position>();
        _velocities = world.GetPool<Velocity>();
        // 显式声明 Filter 条件：有 Position 且有 Velocity
        _filter = world.Filter<Position>().Inc<Velocity>().End();
    }

    public void Run(IEcsSystems systems) {
        foreach (var entity in _filter) {
            ref var pos = ref _positions.Get(entity);
            ref var vel = ref _velocities.Get(entity);
            pos.X += vel.X;
        }
    }
}

// 组装和运行
var world = new EcsWorld();
var systems = new EcsSystems(world);
systems
    .Add(new VelocitySystem())
    .Init();

// 游戏循环
while (isRunning) {
    systems.Run();
}

systems.Destroy();
world.Destroy();
```

从上面的代码可以看出，Leopotam 的写法和传统的 OOP 服务分层写法非常相似——Pool 像是 Repository，System 像是 Service。对于没有 ECS 背景的 C# 服务端开发者来说，这套 API 的上手成本很低。

**存储结构**：Leopotam 使用 Sparse Set 存储，而不是 Archetype Chunk。这意味着：
- 实体的组件添加/删除更快（不需要迁移 Archetype）
- 但 Query 遍历时内存访问的局部性不如 Arch，在实体规模大时缓存命中率更低

在实际服务端场景里，Leopotam 的性能对于单房间 10 万以内实体的游戏逻辑通常是足够的。它的瓶颈往往不会出现在 ECS 遍历本身，而是在 I/O 和网络处理上。

**适合场景**：团队 ECS 经验不足，优先保证快速上线；或者项目规模中等，不需要极致优化。

---

## 三、Unity DOTS Headless Server

这条路线的核心价值只有一条：**完整复用客户端的 ECS 代码**。

在帧同步或服务端权威架构下，客户端和服务端往往跑的是同一套逻辑——移动、技能、碰撞。如果客户端用 DOTS，那么把同样的 System 搬到服务端是最理想的方案。DOTS Headless Server 就是为这个场景设计的。

```csharp
// 服务端入口（不依赖 MonoBehaviour）
using Unity.Entities;

var world = new World("ServerWorld");
var initGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
var simGroup  = world.GetOrCreateSystemManaged<SimulationSystemGroup>();

// 自定义 Tick 循环，不依赖 UnityEngine.Update
while (isRunning) {
    world.Update();
    Thread.Sleep(tickIntervalMs);
}

world.Dispose();
```

客户端的 System 几乎不需要改动就能在这套框架里运行——条件是这些 System 不依赖 `UnityEngine.Transform`、`Renderer`、`Camera` 等渲染相关类型。纯逻辑 System（物理积分、技能状态机、碰撞检测）通常可以直接复用。

**关于 Burst Compiler**：Burst 在服务端同样可以工作。服务端构建（Unity Server Build）会把 Burst 编译结果打进构建产物，JIT 阶段依然会使用 SIMD 优化后的原生代码。这是 DOTS Headless 性能高于 Arch 的根本原因——有 Burst 加持时，热路径的性能接近手写 C++ SIMD 代码。

**但这条路线的代价也很明显**：

1. **Unity 运行时依赖**：服务端需要跑 Unity 运行时（IL2CPP 编译产物或 Mono），不是标准的 .NET 应用。Docker 容器里跑 Unity Server Build 是可以的，但构建流程比纯 .NET 镜像复杂得多。
2. **许可证**：Unity 服务端构建需要有效的 Unity 许可证。服务端按节点收费的场景需要确认许可证条款。
3. **复杂度**：DOTS 本身的上手成本相当高——Blob Asset、Baking 管线、Job System 的使用约束、Safety Handle 系统……服务端开发者如果没有 Unity 客户端背景，学习曲线会非常陡峭。

---

## 四、三条路线横向对比

| 维度 | Arch | Leopotam | DOTS Headless |
|------|------|----------|---------------|
| 性能峰值 | 高 | 中 | 最高（Burst SIMD） |
| 客户端代码复用 | 不可 | 不可 | 可（同一套 ECS） |
| Unity 运行时依赖 | 无 | 无 | 必须 |
| 部署复杂度 | 低（纯 .NET） | 低（纯 .NET） | 高（Unity Server Build） |
| 上手难度 | 中 | 低 | 高（DOTS 全套） |
| Structural Change 性能 | 中 | 高 | 中 |
| 适合实体规模 | 中大型 | 中小型 | 中大型 |
| 许可证约束 | MIT | MIT | Unity 许可证 |

性能这一列需要额外说明：DOTS Headless 的"最高"是有条件的——要在 Unity Server Build 环境下，Burst 编译生效，才能发挥 SIMD 优势。如果只是在 .NET JIT 下跑 Entities 包，实际性能未必比 Arch 高。

---

## 五、选型决策树

**如果客户端用 Unity DOTS，且服务端逻辑和客户端高度重叠**（帧同步验证服务器、服务端权威的物理/技能逻辑），优先考虑 DOTS Headless。复用客户端代码的工程价值，通常远大于部署复杂度的代价。

**如果团队是 C# 背景，但不想引入 Unity 运行时**（纯 .NET 微服务架构、Kubernetes 部署），选 Arch。性能够用，部署简单，没有许可证烦恼。

**如果团队 ECS 经验不足，或者项目周期紧**，选 Leopotam。API 接近 OOP 习惯，两天就能跑起来，性能对大多数中小型游戏服务端足够。

---

## 六、什么情况下不该用 C# 服务端 ECS

**对性能要求极高，且团队有 C++ 能力**：Burst 能弥补很大一部分 C# 和 C++ 的性能差距，但如果本身有 C++ 背景，Flecs 或 EnTT 在单核热路径上仍然有优势，且不需要 Unity 运行时。

**跨语言后端**（部分服务 Go，部分 C++）：如果服务端本身已经是多语言混合架构，单独引入 C# ECS 会造成技术孤岛。不如用统一的协议层通信，各服务内部选最适合自己的技术。

**服务端逻辑极简**：如果服务端只是做输入验证和状态广播，实体数量少、逻辑简单，ECS 的组件化分层反而是过度设计。用 Actor 模型或简单的 Dictionary + 状态机通常更直接。

---

下一篇：SV-ECS-07 讨论 Multi-World 隔离——服务端最典型的 ECS 部署形态，每个游戏房间是一个独立的 ECS World，以及 World 生命周期管理和跨 World 边界的正确处理方式。
