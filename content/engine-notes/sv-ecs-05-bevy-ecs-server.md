---
title: "服务端 ECS 05｜Bevy ECS on Server：Rust 内存安全 + ECS 性能，无渲染环境下的用法"
slug: "sv-ecs-05-bevy-ecs-server"
date: "2026-03-28"
description: "Bevy 的 ECS 内核可以独立使用，Rust 的所有权模型天然防止数据竞争——这篇讲在无渲染服务端环境下怎样用 Bevy ECS，以及它的 Schedule、System、Resource 和多 World 在服务端的实际意义。"
tags:
  - "服务端"
  - "ECS"
  - "Bevy"
  - "Rust"
  - "游戏服务器"
  - "高性能"
  - "框架选型"
series: "高性能游戏服务端 ECS"
primary_series: "server-ecs"
series_role: "article"
series_order: 5
weight: 2350
---

前面讲的 Flecs 和 EnTT 都是 C++ 方案。这篇换语言——用 Rust 的 Bevy ECS 做服务端，以及 Rust 的所有权模型为什么能在语言层面解决 ECS 并发安全问题。

## 定位：引擎的 ECS 内核，可以单独拆出来用

Bevy 是 Rust 游戏引擎，完整版包含渲染、音频、输入、场景等大量模块。但它的架构是高度模块化的——ECS 内核是独立的 `bevy_ecs` crate，不依赖渲染层，可以在任何 Rust 项目里单独使用。

这对服务端的意义是：你不需要接受 Bevy 的整套 App 框架，也不需要引入 wgpu 渲染器，只需要 ECS 数据层 + 调度器，就可以获得 Bevy ECS 的全部核心能力。

Bevy ECS 属于"Archetype ECS"，和 Unity DOTS 的 Chunk 设计类似：具有相同组件集合的实体存储在同一 Archetype 里，内存连续，遍历时缓存友好。和 Flecs 的 Archetype 方案在性能量级上相当。

---

## 一、独立使用 bevy_ecs（不引入渲染）

在 `Cargo.toml` 里只引入 `bevy_ecs`，完全不需要 Bevy 的完整 App 系统：

```toml
# Cargo.toml
[dependencies]
bevy_ecs = "0.14"  # 不引入 bevy 全家桶
```

最小可运行的 ECS 循环：

```rust
use bevy_ecs::prelude::*;

#[derive(Component)]
struct Position { x: f32, y: f32 }

#[derive(Component)]
struct Velocity { x: f32, y: f32 }

fn update_position(mut query: Query<(&mut Position, &Velocity)>) {
    for (mut pos, vel) in query.iter_mut() {
        pos.x += vel.x;
        pos.y += vel.y;
    }
}

fn main() {
    let mut world = World::new();
    let mut schedule = Schedule::default();

    world.spawn((Position { x: 0.0, y: 0.0 }, Velocity { x: 1.0, y: 0.0 }));

    schedule.add_systems(update_position);
    schedule.run(&mut world);
}
```

不需要 `App`，不需要 `Plugin`，不需要 `DefaultPlugins`——这些是 Bevy 完整引擎的概念，服务端用不到。直接 `World` + `Schedule` 就是完整的 ECS 运行时。

---

## 二、Rust 所有权模型 = 编译期 ECS 安全

这是 Bevy ECS 和 C++ 方案的本质差异，也是很多人低估的优势。

Flecs 和 EnTT 的并发安全依赖**约定**：开发者声明 System 访问哪些组件（read/write），框架在运行时推断依赖关系，然后决定哪些 System 可以并行、哪些需要串行。这套机制的前提是开发者声明正确——如果声明写错了，运行时才会报错，或者直接产生数据竞争（C++ 是未定义行为）。

Bevy ECS 的安全靠 **Rust 编译器的借用检查**：

```rust
// 编译器会拒绝这段代码
fn bad_system(
    mut pos_query: Query<&mut Position>,  // mutable borrow
    pos_query2: Query<&Position>,         // immutable borrow——同一类型同时借用
) { ... }
// error: cannot borrow `Position` as immutable because it is also borrowed as mutable
```

这个报错在**编译时**产生，不是运行时。如果两个 System 同时访问同一组件，Bevy 的调度器会在构建 Schedule 时分析依赖关系：

- 无访问冲突的 System → 调度器自动并行执行
- 有冲突的 System → 调度器自动串行化，不需要手工标注

更重要的是：如果你在同一个 System 函数签名里写出了冲突的访问，编译器直接拒绝，不存在"忘记声明"导致运行时数据竞争的路径。

对服务端的实际意义：**不需要在运行时用互斥锁保护 Component 数据，编译器在构建时就杜绝了数据竞争**。这在长期维护的服务端代码库里是真实的工程收益——C++ 的数据竞争 bug 通常只在高负载下才触发，定位成本极高。

---

## 三、Schedule：灵活的系统调度

Bevy ECS 的 `Schedule` 支持 `SystemSet` 和顺序约束，这对服务端的 tick 结构非常实用：

```rust
use bevy_ecs::prelude::*;

// 定义执行阶段
#[derive(SystemSet, Debug, Hash, PartialEq, Eq, Clone)]
enum ServerPhase {
    ReceiveInput,
    Simulate,
    BroadcastState,
}

fn build_schedule() -> Schedule {
    let mut schedule = Schedule::default();

    schedule
        .configure_sets((
            ServerPhase::ReceiveInput,
            ServerPhase::Simulate,
            ServerPhase::BroadcastState,
        ).chain())  // 三个阶段顺序执行
        .add_systems(drain_input_queue.in_set(ServerPhase::ReceiveInput))
        .add_systems(update_physics.in_set(ServerPhase::Simulate))
        .add_systems(update_ai.in_set(ServerPhase::Simulate))       // Simulate 内两个 System 自动并行
        .add_systems(send_state_updates.in_set(ServerPhase::BroadcastState));

    schedule
}
```

`.chain()` 保证三个 Set 按顺序执行（ReceiveInput → Simulate → BroadcastState）。同一 Set 内的多个 System 如果没有访问冲突，调度器会自动并行执行（`update_physics` 和 `update_ai` 如果访问不同组件，就会并行）。

这套机制和服务端 tick 的典型结构（接收→模拟→广播）天然匹配，不需要手工管理线程，调度器会根据 System 的组件访问声明自动推导最优并行度。

---

## 四、Resource：全局服务端状态

`Resource` 相当于 Unity DOTS 的 Singleton Component，用于存储 World 级别的全局状态——服务端常见的用例是存放 IO 通道：

```rust
#[derive(Resource)]
struct InboundQueue(Arc<Mutex<VecDeque<ClientMessage>>>);

#[derive(Resource)]
struct OutboundQueue(Arc<Mutex<Vec<ServerMessage>>>);

fn drain_input_queue(
    inbound: Res<InboundQueue>,
    mut commands: Commands,
) {
    let mut queue = inbound.0.lock().unwrap();
    while let Some(msg) = queue.pop_front() {
        // 将网络消息转化为 ECS 操作
        commands.spawn(PlayerInput { data: msg });
    }
}

fn send_state_updates(
    outbound: Res<OutboundQueue>,
    query: Query<(&PlayerId, &Position, &Health)>,
) {
    let mut queue = outbound.0.lock().unwrap();
    for (id, pos, hp) in query.iter() {
        queue.push(ServerMessage::StateUpdate {
            player_id: id.0,
            position: (pos.x, pos.y),
            health: hp.current,
        });
    }
}
```

`Arc<Mutex<...>>` 是 Resource 内部持有跨线程通道的标准模式。ECS 的 tick 线程和网络 IO 线程通过这个队列解耦——网络层把消息推入 `InboundQueue`，ECS tick 读取并处理，处理结果写入 `OutboundQueue`，网络层再异步发送。

---

## 五、多 World 在 Rust 中的自然表达

多 World 是服务端 ECS 的常见架构——每个游戏房间或副本是一个独立的 World，World 之间完全隔离，互不干扰。

在 Rust 里，这个模式表达得非常自然：

```rust
// 每个游戏房间是一个独立 World
struct GameRoom {
    world: World,
    schedule: Schedule,
    room_id: u64,
}

// 运行单个房间的一帧
fn tick_room(room: &mut GameRoom) {
    room.schedule.run(&mut room.world);
}

// 多线程并行运行所有房间——使用 rayon
use rayon::prelude::*;

fn tick_all_rooms(rooms: &mut Vec<GameRoom>) {
    rooms.par_iter_mut().for_each(|room| {
        tick_room(room);
    });
}
```

这里有一个关键点：`par_iter_mut` 要求 `GameRoom` 实现 `Send` trait（可以安全地跨线程传递）。如果 `GameRoom` 里包含了非 `Send` 的数据（比如原始指针、`Rc<T>`），编译器会**拒绝** `par_iter_mut` 调用，不需要程序员去检查。

这意味着 Rust 的类型系统在编译期就保证了：**只有当 World 的所有数据都是线程安全的，才允许把它放到多线程环境里运行**。C++ 的 EnTT 和 Flecs 做不到这一点——它们把这个责任交给了开发者的自律。

---

## 六、与 Bevy App 框架的关系

如果你接受 Bevy 的完整 App 框架，可以获得更多开箱即用的能力：

```rust
use bevy::prelude::*;

fn main() {
    App::new()
        .add_plugins(MinimalPlugins)  // 只加载最小插件集（不含渲染）
        .add_systems(Update, update_position)
        .run();
}
```

`MinimalPlugins` 包含：时间管理、任务调度器、基础 ECS 支持，但不包含任何渲染或窗口相关的内容。对于服务端来说，这是一个在"裸 World + Schedule"和"完整 Bevy App"之间的中间选项。

不过对于需要精确控制 tick 频率的游戏服务器（比如固定 20Hz 或 64Hz），通常还是选择裸 `World + Schedule` + 自己管理 tick 循环，而不是接受 `App::run()` 的控制权。

---

## 七、局限性

**API 迭代速度快**：Bevy 目前每半年左右发一个大版本，0.13 → 0.14 → 0.15 之间经常有 breaking change（System 参数、Schedule API 等都改过）。服务端需要锁定版本，升级时需要专门分配时间处理 API 变更。

**生态比 Flecs/EnTT 小**：Bevy ECS 的服务端生产案例相对少，社区的踩坑记录也比 C++ 方案少，遇到边缘问题时自行排查的成本较高。

**Rust 学习曲线**：所有权、借用检查、生命周期对没有 Rust 背景的团队是真实负担。"编译器会保护你"这件事，前提是你能过得了编译。Rust 的编译期安全是收益，但学习成本是前期的真实投入。

**与 C++ 引擎的互操作**：如果服务端需要调用 C++ 物理库（Bullet、PhysX）或其他 C++ SDK，Rust FFI 有额外的 unsafe 代码和生命周期管理负担。纯 Rust 项目里 Bevy ECS 是很好的选择，但"Rust ECS + C++ 物理"的混合架构复杂度不低。

**内存分配模型**：Bevy ECS 的 Archetype 在组件集合变化时会进行数据迁移（和 Unity DOTS 的 Chunk Move 类似），高频 AddComponent/RemoveComponent 的场景下会有性能压力。

---

## 八、性能量级参考

Bevy ECS 属于 Archetype 方案，和 Flecs 的量级相当：

| 场景 | 量级 |
|---|---|
| 纯遍历（位置更新，1M 实体）| < 10ms |
| Archetype 内迭代（无组件变更）| 缓存命中率高，接近裸内存遍历 |
| 跨 Archetype 迁移（频繁 AddComponent）| 有重排开销，应避免高频操作 |
| 多 World 并行（rayon）| 线性扩展，无共享状态 |

多 World 并行是 Bevy ECS 在服务端最有优势的场景——每个 World 完全独立，rayon 的并行效率接近线性，100 个游戏房间在 100 个核心上可以接近 100x 的吞吐量提升。

---

## 什么情况下选 Bevy ECS

| 需求 | 评估 |
|---|---|
| 团队熟悉 Rust | 强推荐 |
| 对编译期安全有高要求（防外挂计算、金融级游戏服务）| 推荐 |
| 全新 Rust 项目，无历史 C++ 包袱 | 推荐 |
| 需要最大多 World 并行效率 | 推荐 |
| 团队 Rust 经验为零 | 不推荐，学习曲线是真实成本 |
| 需要与 C++ 引擎深度集成 | 谨慎，FFI 复杂度高 |
| 需要稳定 API（生产服务不想频繁迁移）| 谨慎，Bevy API 迭代快 |

---

## 小结

Bevy ECS 把 Rust 的语言特性和 ECS 的并发需求结合得很自然——借用检查器本来就是为了防止数据竞争而设计的，而 ECS 的 System 调度本质上就是一个数据竞争分析问题。两者的匹配不是偶然的。

对于新建的 Rust 服务端项目，或者对并发安全有高要求的场景，Bevy ECS 是目前最成熟的 Rust ECS 选项，比 hecs、specs 等其他 Rust ECS 方案有更活跃的社区和更完整的文档。

下一篇：SV-ECS-06 C# 服务端 ECS——Arch 和 Leopotam 的定位，以及 Unity 技术栈团队在 DOTS Headless Server 和独立 C# ECS 之间怎样选。
