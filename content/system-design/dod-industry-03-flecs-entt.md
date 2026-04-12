---
title: "DOD 行业案例 03｜Flecs 与 EnTT：独立 ECS 框架的设计哲学，Minecraft Bedrock 为什么选 EnTT"
slug: "dod-industry-03-flecs-entt"
date: "2026-03-28"
description: "在 Unity DOTS 和 Unreal Mass 之外，C++ 独立 ECS 框架是服务端和跨平台项目的另一条路。Flecs 和 EnTT 代表两种截然不同的设计哲学：前者功能完整、内置工具链，后者极简、header-only、控制权在开发者手里。"
tags:
  - "Flecs"
  - "EnTT"
  - "ECS"
  - "C++"
  - "Minecraft"
  - "服务端"
  - "数据导向"
series: "数据导向行业横向对比"
primary_series: "dod-industry"
series_role: "article"
series_order: 3
weight: 2130
---

## 为什么需要独立 ECS 框架

Unity DOTS 和 Unreal Mass 解决了各自引擎内部的 ECS 需求，但它们有一个共同的硬限制：**不可脱离宿主引擎运行**。DOTS 依赖 Unity Job System 和 Burst Compiler，Mass 依赖 Unreal 的 Module 系统和 UObject 基础设施。一旦离开这两个引擎，这些框架就无从谈起。

这对以下三类项目构成障碍：

- **服务端仿真**：MMO 或竞技游戏的权威服务器通常跑在 Linux 裸机上，不可能部署一个 Unity Editor 环境。
- **自研引擎**：大型工作室有时从零构建引擎，需要 ECS 层但不接受任何引擎绑定。
- **跨平台嵌入**：目标平台跨越 PC、主机、移动端和服务器，需要一套 C++ 实现在所有平台统一编译。

C++ 社区里，两个框架长期占据这个位置：**Flecs** 和 **EnTT**。它们在设计哲学上走向了两个极端，选择哪一个取决于项目对工具链和框架约定的接受程度。

---

## Flecs：功能完整的企业级 ECS

Flecs 由 Sander Mertens 开发，当前稳定版本为 4.x，同时提供 C 和 C++ API。它的定位是"一个完整的 ECS 运行时"，不仅管理数据，还负责调度、观察和工具链。

### 核心特性

**内置 Pipeline 和 Phase 调度系统**

Flecs 把 System 按 Phase 分组（PreUpdate、OnUpdate、PostUpdate 等），Pipeline 负责按顺序执行这些 Phase，并自动推导 System 之间的读写依赖关系，实现并行化。开发者只需声明 System 在哪个 Phase 运行，框架自动排布执行顺序。

```cpp
flecs::world world;

world.system<Position, Velocity>("Move")
    .kind(flecs::OnUpdate)
    .each([](Position& p, const Velocity& v) {
        p.x += v.x;
        p.y += v.y;
    });

world.progress(); // 执行一帧
```

**Observer：响应 Component 变化**

Observer 是 Flecs 最有特色的功能之一，等价于 Unreal Mass 的 Signal 系统。它可以监听 Component 被添加、删除或修改时触发回调，适合构建状态机和事件驱动逻辑：

```cpp
world.observer<Position>("OnPositionAdded")
    .event(flecs::OnAdd)
    .each([](flecs::entity e, Position& p) {
        // Entity 获得 Position 时触发
    });
```

**关系（Relationships）**

Flecs 4.x 引入了一等公民的关系系统。关系不只是 parent-child 层级，而是任意命名的 Entity 对关系。例如 `(Eats, Apples)` 可以表达"这个 Entity 吃苹果"，查询时可以直接过滤关系：

```cpp
flecs::entity Alice = world.entity("Alice");
flecs::entity Bob = world.entity("Bob");
Alice.add<Likes>(Bob); // Alice likes Bob

// 查询所有喜欢 Bob 的 Entity
world.query<>().with<Likes>(Bob).each([](flecs::entity e) { ... });
```

这个能力在 Unity DOTS 和 Mass 里都没有原生对应物，通常需要用额外的 Component 手动模拟。

**内置 REST API 和 Web Explorer**

Flecs 自带一个 HTTP 服务器，运行时可以通过浏览器访问 `https://www.flecs.dev/explorer`，连接到本地进程，实时查看所有 Entity、Component 和 System 的状态。这对服务端调试尤为重要，不需要接一个 UI 框架就能获得可视化。

### 性能基准

根据 Flecs 官方仓库 `flecs/perf` 中的 benchmark 数据（在 x86-64 Linux，单线程条件下）：

- 遍历 100 万个 Entity（每个持有 2 个 Component，Position + Velocity）：约 **1~2 ms**
- 遍历 100 万个 Entity（每个持有 5 个 Component）：约 **3~4 ms**
- Component 增删操作：约 **100~200 ns / op**（涉及 Archetype 迁移）

整体性能与 Unity DOTS 在纯遍历场景下处于同一数量级，差距主要来自 Burst 的 SIMD 自动向量化，后者在 Flecs 中需要手动实现。

### 适合场景

Flecs 适合需要"开箱即用"体验的项目：服务端多 World 仿真、需要 Observer 驱动逻辑、希望内置工具链替代外部监控。

---

## EnTT：极简的 header-only ECS

EnTT 由 Michele Caini（skypjack）开发，当前稳定版本为 3.x，要求 C++17，**整个库只有头文件，无需编译步骤**。它的设计哲学是"框架不替你做决定"。

### 核心特性

**没有内置调度器**

EnTT 只管数据，不管执行顺序。没有 Pipeline、没有 Phase，也没有 System 注册机制。你自己写循环，自己决定谁先谁后：

```cpp
entt::registry registry;

// 创建 Entity
auto entity = registry.create();
registry.emplace<Position>(entity, 0.0f, 0.0f);
registry.emplace<Velocity>(entity, 1.0f, 0.0f);

// 遍历（你自己决定在哪里调用）
auto view = registry.view<Position, Velocity>();
view.each([](Position& p, const Velocity& v) {
    p.x += v.x;
    p.y += v.y;
});
```

这看起来是限制，实际上是将完全控制权交给开发者。在 Tick 逻辑复杂、执行顺序依赖游戏状态的项目里，这种自由度是刚需。

**View 和 Group：接近裸内存访问速度**

EnTT 提供两种查询接口：

- `view`：过滤 Component 组合，内部用稀疏集（Sparse Set）实现，随机访问 O(1)
- `group`：将多个 Component 的存储对齐，遍历时内存访问模式等价于 SoA（Structure of Arrays），性能接近裸数组遍历

Group 是 EnTT 的核心性能武器。一旦建立 Group，遍历时 Component 数据在内存中连续排列，Cache miss 率极低。

**Signal：轻量级 Component 回调**

EnTT 的 Signal 比 Flecs Observer 更轻量，直接绑定到 Registry 的生命周期事件：

```cpp
registry.on_construct<Position>().connect<&onPositionAdded>();
registry.on_destroy<Position>().connect<&onPositionRemoved>();
```

没有查询语法，没有 Phase 依赖，回调就是纯函数指针或 Lambda，开销极低。

### 性能基准

根据 `skypjack/entt` 仓库的 benchmark 数据：

- 遍历 100 万个 Entity（Group 模式，2 个 Component）：约 **0.5~1.5 ms**（比 Flecs View 略快，因为框架开销更小）
- 遍历 100 万个 Entity（View 模式）：约 **1~3 ms**
- Component 增删：约 **50~100 ns / op**（Sparse Set 不需要 Archetype 迁移）

EnTT 没有 Archetype 模型，添加/删除 Component 的代价比 Flecs 低，因为不涉及 Entity 在 Archetype 之间的迁移。代价是遍历时 Component 数据的局部性依赖 Group 的建立，如果不用 Group，遍历性能会有损失。

### 适合场景

EnTT 适合需要极简集成、不接受框架约定的项目：自研引擎的 ECS 层、跨平台嵌入场景、需要自己掌控 Tick 顺序的复杂游戏逻辑。

---

## Minecraft Bedrock 选 EnTT 的理由

Mojang 开发者在 EnTT GitHub 仓库的 issue 中明确确认 Minecraft Bedrock Edition 使用了 EnTT 作为其 ECS 实现。这是 EnTT 迄今最广为人知的工业级使用案例。

为什么是 EnTT 而不是 Flecs？基于公开信息，可以推导出以下几点：

**1. 跨平台是硬约束**

Bedrock 需要在 Windows、Xbox、PlayStation、Nintendo Switch、iOS、Android 和专用服务器上统一运行。任何依赖特定平台工具链或运行时的框架都不可行。EnTT 的 header-only 特性意味着只要编译器支持 C++17，就能集成，没有外部依赖。

**2. 没有调度器意味着完全控制 Tick**

Minecraft 的 Tick 逻辑极为复杂：区块加载、红石电路、生物 AI、物理模拟，这些系统的执行顺序不能简单地用 Phase 描述，里面有大量的条件分支和状态依赖。让框架管理调度顺序反而是负担。EnTT 让 Mojang 自己写 Tick 循环，每帧的执行顺序完全由游戏逻辑决定。

**3. 集成成本极低**

Bedrock 的代码库在引入 EnTT 之前已有大量遗留代码。header-only 的库可以逐步引入，不需要重构构建系统，也不需要全量迁移，可以只在新增的子系统里使用 ECS，与旧代码共存。

**4. Group 性能满足批量遍历需求**

生物 AI 的批量 Tick（同屏数百个实体同时更新位置、状态）是 Bedrock 的性能热点之一。EnTT 的 Group 在这类批量遍历场景下性能接近 DOTS 的 IJobEntity，在没有 Burst 的情况下已经足够。

---

## Flecs vs EnTT 对比

| 维度 | Flecs | EnTT |
|------|-------|------|
| 语言 | C / C++ | C++17 |
| 分发方式 | 单文件或 CMake | header-only |
| 内置调度器 | 有（Pipeline + Phase）| 无 |
| 内置工具链 | REST API + Web Explorer | 无 |
| Observer / Signal | Observer（功能强，支持关系过滤）| Signal（轻量，函数指针级开销）|
| 关系（Relationship）| 内置一等公民 | 无 |
| Component 增删开销 | 中等（涉及 Archetype 迁移）| 低（Sparse Set，无迁移）|
| 遍历性能（100 万 Entity）| 1~3 ms | 0.5~3 ms |
| 框架开销 | 中等 | 极低 |
| 适合场景 | 需要工具链、服务端多 World、Observer 驱动 | 极简控制权、跨平台嵌入、自定义 Tick |
| 知名使用案例 | 多个独立游戏和服务端项目 | Minecraft Bedrock Edition |

---

## 和 DOTS / Mass 的对比

**最核心的差异是：不绑定引擎。** Flecs 和 EnTT 可以在任何能跑 C++ 的地方运行，包括 Linux 服务器、嵌入式平台、浏览器（通过 Emscripten）。

**性能提升的来源不同。** DOTS 的性能有相当一部分来自 Burst Compiler 的自动 SIMD 向量化和 NativeContainer 的内存安全检查消除。Flecs 和 EnTT 的性能完全来自数据布局（Archetype / Sparse Set + Group），没有编译器加持。如果热路径需要 SIMD，开发者需要手动使用 SSE/AVX intrinsic 或 ISPC。

**API 稳定性优于 DOTS。** DOTS 在 0.x 阶段经历了多次大规模 API 重构，EnTT 3.x 和 Flecs 4.x 的 API 已相对稳定，向后兼容性更有保障。对于需要长期维护的商业项目，这一点不可忽视。

**社区和生态相对较小。** DOTS 有 Unity 背书，有大量第三方插件和教程；Flecs 和 EnTT 的社区主要集中在 GitHub 和少数论坛。技术问题通常需要直接阅读源码或提 issue，文档深度因模块而异。

---

## 小结

Flecs 和 EnTT 代表了独立 ECS 框架的两个方向：**Flecs 给你一个完整的运行时**，内置调度、工具链和高级语义（关系、Observer）；**EnTT 给你一个高性能的数据容器**，其余都由你自己决定。Minecraft Bedrock 选择 EnTT，本质上是在说"我们不需要框架告诉我们怎么跑游戏，我们只需要一个可靠的 Entity-Component 存储"。

选择哪个取决于项目的控制欲：如果你希望框架做更多，Flecs 是更好的起点；如果你需要把框架嵌入一个已有的复杂系统并保持完全控制，EnTT 的极简性是优势而非缺陷。

下一篇 [DOD 行业案例 04｜选型决策地图](/engine-notes/dod-industry-04-decision-map) 会把 Unity DOTS、Unreal Mass、Flecs、EnTT 放在同一张坐标系上，给出基于项目类型的选型建议。
