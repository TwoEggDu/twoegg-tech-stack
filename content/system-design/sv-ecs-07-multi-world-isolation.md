---
title: "服务端 ECS 07｜Multi-World 隔离：每个房间一个 World，生命周期、资源共享与跨 World 边界"
slug: "sv-ecs-07-multi-world-isolation"
date: "2026-04-04"
description: "一个进程跑几十上百个游戏房间，用一个大 World 还是每个房间独立 World？这不是性能问题，是数据隔离和生命周期管理的架构选择——单 World 的问题比你想象的更深。"
tags:
  - "服务端"
  - "ECS"
  - "Multi-World"
  - "游戏服务器"
  - "高性能"
  - "架构设计"
  - "生命周期"
series: "高性能游戏服务端 ECS"
primary_series: "server-ecs"
series_role: "article"
series_order: 7
weight: 2307
---

服务端 ECS 面对的一个几乎所有项目都会遇到的架构问题，往往在早期不被重视，等到出问题的时候已经很难重构：**一个进程要同时跑几十上百个游戏房间，ECS World 该怎么组织？**

表面上看这是个性能问题——单 World 还是多 World，哪个快？但实际上，这是一个数据隔离、生命周期管理、资源共享的架构问题。性能是选择的结果，不是出发点。

---

## 一、单 World 方案的问题

最自然的出发点是：用一个全局 World 跑所有房间的实体。

```c
// 错误示范：全局单 World
ecs_world_t *g_world = ecs_init();

// 房间创建：在全局 World 里创建实体
void room_create(int room_id) {
    // 为每个实体打上 RoomTag 区分房间
    ecs_entity_t room_tag = ecs_new_entity(g_world, NULL);
    ecs_entity_t npc1 = ecs_new(g_world, 0);
    ecs_add_pair(g_world, npc1, BelongsToRoom, room_tag);
    // ...
}

// System 遍历时用 Filter 过滤
void ai_system(ecs_iter_t *it) {
    // 每次都要额外过滤 room_tag，遍历全部 NPC
    // ...
}
```

这个方案在几十个房间、每个房间几百个实体时可以工作，但会遭遇三类根本性问题：

### 问题一：Entity ID 污染与碎片化

ECS 的 Entity ID 是连续分配的整数。单 World 里，所有房间的实体共享同一个 ID 空间。房间 A 创建了 5000 个实体，房间 B 再创建 5000 个，全局 ID 已经到了 10000。

房间 A 结束，5000 个实体被销毁。这 5000 个 ID 被标记为可复用，但 Archetype 表内的内存槽不会立刻整理（Flecs 和大多数 ECS 框架都是延迟整理的）。随着房间不断创建销毁，Entity ID 的分布变得极度碎片化。

后果是 Query 迭代时的 Archetype 碎片化——原本连续的实体数据被"洞"打散，遍历一万个 NPC 实际上要跳过很多空槽，破坏了 ECS 连续内存布局的缓存优势。

### 问题二：System 全量遍历，无法按房间限定范围

单 World 下，一个 AISystem 的 Query 会遍历**所有房间的 NPC**，然后在逻辑里靠 Room Tag 过滤。这意味着即使某个房间只有 3 个玩家，它的 NPC 也要参与全局遍历。

更严重的是，System 内部无法利用 ECS 的 Archetype 连续内存优势做房间级的批量处理——不同房间的 NPC 混杂在同一批 Archetype Table 里，一次遍历的"工作集"太大，L3 缓存溢出，实际性能比理论差很多。

### 问题三：房间间数据隔离困难，生命周期难以管理

单 World 里，一个 System 的 Bug 可能意外修改了另一个房间的实体（比如 Query 范围写错，过滤条件遗漏）。这类 Bug 极难复现，因为它依赖特定的实体共存状态。

房间销毁时，你需要保证所有属于这个房间的实体都被正确清理，包括它们持有的外部资源引用（数据库连接、玩家 Session 引用）。这需要精心设计的清理 System，而且一旦出错，残留的"孤儿实体"会悄悄消耗内存，直到服务端内存溢出。

---

## 二、Multi-World 方案：强隔离的代价与收益

每个游戏房间创建一个独立的 ECS World，是服务端 ECS 的标准解法。

```c
// 正确做法：每个房间一个 World
typedef struct {
    int room_id;
    ecs_world_t *world;
    IoChannels *io;
    pthread_t tick_thread;
} RoomState;
```

### 收益

**完全的内存隔离**：不同房间的 Entity ID 从 0 开始独立计数。房间 A 的实体 42 和房间 B 的实体 42 是完全不同的内存地址，没有任何关联。这让 Archetype 布局保持紧凑，遍历始终在"干净"的连续内存上进行。

**独立 Tick，可以并行**：每个 World 的 `ecs_progress()` 是独立的，可以分发到线程池的不同工作线程上并行执行。线程边界就是 World 边界，不需要任何锁：

```c
// 线程池并行 Tick：每个工作线程处理一批 World
void* tick_worker(void *arg) {
    WorldBatch *batch = (WorldBatch *)arg;
    for (int i = 0; i < batch->count; i++) {
        ecs_progress(batch->worlds[i], batch->delta_time);
    }
    return NULL;
}
```

**生命周期清晰**：房间创建 = `ecs_init()`，房间销毁 = `ecs_fini()`。World 内所有实体的生命周期都随 World 一起结束，不可能有"孤儿实体"残留。

### 代价

**共享资源需要特殊处理**：配置表、数据库连接池、日志系统——这些不该每个 World 各自持有一份。下一节详细讲。

**跨 World 通信不能直接操作 Component**：如果需要发送跨房间消息（比如组队系统，玩家从房间 A 进入房间 B），不能直接在 System 内操作另一个 World 的 Entity，必须通过消息层中转。下一节详细讲。

**内存总开销更高**：每个 World 有自己的 Archetype 注册表、Query 缓存、System 列表。100 个房间 = 100 套这些元数据。对于小型服务，这可能是不必要的开销。

---

## 三、World 生命周期管理

World 的生命周期管理是 Multi-World 架构里最容易出错的地方，顺序错误会导致数据丢失或内存泄漏。

### 创建时机

World 应该在所有共享资源初始化完成之后创建，并在创建时立刻把共享资源的访问接口绑定到 World Context：

```c
RoomState* room_create(int room_id, SharedServices *services) {
    RoomState *room = calloc(1, sizeof(RoomState));
    room->room_id = room_id;

    // 1. 创建 World
    room->world = ecs_init();

    // 2. 注册 Component 和 Tag
    ECS_COMPONENT_DEFINE(room->world, Position);
    ECS_COMPONENT_DEFINE(room->world, Health);
    ECS_COMPONENT_DEFINE(room->world, Velocity);
    ECS_TAG_DEFINE(room->world, Dead);
    ECS_TAG_DEFINE(room->world, Alive);

    // 3. 注册 System（按 Phase 顺序）
    ECS_SYSTEM(room->world, DrainInputSystem, EcsOnLoad);
    ECS_SYSTEM(room->world, MoveSystem, EcsOnUpdate, Position, Velocity);
    ECS_SYSTEM(room->world, AISystem, EcsOnUpdate, AIState, Position);
    ECS_SYSTEM(room->world, CombatSystem, EcsOnUpdate, Health);
    ECS_SYSTEM(room->world, BroadcastSystem, EcsOnStore, Position, Health);
    ECS_SYSTEM(room->world, PersistSystem, EcsOnStore);

    // 4. 把共享服务和 I/O 通道绑定到 World Context
    RoomCtx *ctx = malloc(sizeof(RoomCtx));
    ctx->services = services;    // 共享服务引用
    ctx->io = io_channels_create(room_id);
    ecs_set_ctx(room->world, ctx, free_room_ctx);

    // 5. 加载地图初始实体
    map_load_entities(room->world, room_id, services->map_db);

    return room;
}
```

**关键点**：Component 和 Tag 的 Define 顺序在同一个 World 里必须一致，否则 ID 会错乱。建议封装成统一的 `register_schema(world)` 函数。

### 销毁时机与顺序

销毁顺序至关重要，错误顺序会导致数据丢失：

```c
void room_destroy(RoomState *room) {
    // 第一步：停止接受新的玩家输入（关闭 I/O 入口）
    io_channels_close_inbound(room->ctx->io);

    // 第二步：执行最后一次 Tick，确保处理完队列里剩余的消息
    ecs_progress(room->world, 0);

    // 第三步：触发持久化快照（这步必须在 ecs_fini 之前完成）
    persist_snapshot_sync(room->world, room->room_id);
    // 等待异步持久化完成（如果持久化是异步的）
    persist_wait_drain(room->room_id);

    // 第四步：通知所有在线玩家房间即将关闭
    broadcast_room_close(room->world);

    // 第五步：销毁 World（释放所有 ECS 内存）
    // ecs_fini 会调用所有注册了 EcsOnRemove Observer 的回调
    ecs_fini(room->world);
    room->world = NULL;

    // 第六步：释放 I/O 通道（在 World 销毁之后，避免 Observer 回调访问已释放的通道）
    io_channels_destroy(room->ctx->io);

    free(room);
}
```

**最常见的错误**：在 `ecs_fini` 之后才触发持久化，此时 Component 数据已经被释放，读取到的是垃圾数据或直接崩溃。

### Flecs 的 OnRemove Observer 陷阱

`ecs_fini` 在销毁 World 时，会为每个还存活的实体触发其 Component 的 `EcsOnRemove` Observer。如果你在 `EcsOnRemove` 的回调里访问 World Context（比如往 I/O 通道写数据），**必须保证 I/O 通道在 `ecs_fini` 之后才销毁**。

```c
// 在 OnRemove Observer 里访问 IoChannels：
void OnPlayerRemoved(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);
    // 此时 ctx->io 必须还有效！
    // 所以 io_channels_destroy 必须在 ecs_fini 之后调用
    broadcast_player_left(ctx->io, it->entities[0]);
}
```

---

## 四、共享资源的处理模式

哪些资源应该每个 World 独立持有，哪些应该共享？

### 只读共享数据：单例模式

技能配置表、怪物配置表、地图静态数据——这些数据只读，不会被任何 World 修改，所有 World 可以共享同一份内存副本：

```c
// 共享服务：进程启动时初始化，只读访问
typedef struct {
    SkillDatabase     *skill_db;      // 技能配置表（只读）
    MonsterDatabase   *monster_db;    // 怪物配置表（只读）
    MapDatabase       *map_db;        // 地图数据（只读）
    DbConnectionPool  *db_pool;       // 数据库连接池（并发读写，内部加锁）
    Logger            *logger;        // 日志器（线程安全）
} SharedServices;

// System 通过 World Context 访问共享服务
void AISystem(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);
    const SkillConfig *skill = skill_db_lookup(ctx->services->skill_db, skill_id);
    // ...
}
```

**绝对不要**把只读数据复制进 ECS Component 里（比如把整个技能配置表作为 Component 挂在每个 NPC 上）——这是最常见的内存浪费，也会破坏 Archetype 的紧凑性。Component 里只存 ID（`uint32_t skill_id`），通过 ID 查共享表。

### 可变共享资源：不要共享，要消息化

如果某个资源需要被多个 World 写入，**不要共享这个资源**，要通过消息队列把写操作序列化：

```c
// 错误：全局排行榜让多个 World 直接写入
// void CombatSystem(ecs_iter_t *it) {
//     g_leaderboard->add_kill(player_id);  // 数据竞争！
// }

// 正确：通过消息队列
void CombatSystem(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);
    // 只写队列，不直接访问共享状态
    LeaderboardEvent ev = {player_id, KILL_EVENT, kill_count};
    mpsc_enqueue(ctx->services->leaderboard_queue, &ev);
}

// 独立的排行榜服务线程消费队列，无竞争
void leaderboard_service_thread(SharedServices *services) {
    while (running) {
        LeaderboardEvent ev;
        while (mpsc_dequeue(services->leaderboard_queue, &ev)) {
            leaderboard_update(services->leaderboard, &ev);
        }
        usleep(10000); // 10ms 批量处理
    }
}
```

### 数据库连接池：并发访问，内部加锁

数据库连接池是共享的，但连接池内部本身就是线程安全的（每次 `db_acquire` 从池中取一个空闲连接，用完 `db_release` 归还）。多个 World 的持久化线程可以并发使用同一个连接池，不需要额外的锁。

---

## 五、跨 World 通信的设计原则

**核心原则：不在 System 内直接操作另一个 World。**

System 执行时，不应该持有对其他 World 的引用，更不应该调用 `ecs_progress` 或 `ecs_set` 在另一个 World 上。原因：
1. 破坏了 World 之间的隔离，使得 System 的行为依赖外部状态
2. 如果目标 World 同时在另一个线程上 Tick，会产生数据竞争
3. 让 System 的测试变得不可能（单独 Tick 一个 World 会有副作用）

正确的模式是**共享消息队列**：

```c
// 跨 World 事件通道
typedef struct {
    mpsc_queue_t *cross_world_events;  // 多 World 写入，事件路由服务消费
} CrossWorldBus;

// World A 的 System：玩家发起跨房间组队请求
void HandleTeamRequest(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);
    TeamRequestEvent ev = {
        .from_room = ctx->room_id,
        .from_player = player_entity_id,
        .to_room = target_room_id,
        .to_player = target_player_global_id,
    };
    // 只发事件，不操作目标 World
    mpsc_enqueue(ctx->services->cross_world_bus->cross_world_events, &ev);
}

// 独立的事件路由服务（不是 ECS System）
void event_router_thread(CrossWorldBus *bus, RoomRegistry *rooms) {
    while (running) {
        CrossWorldEvent ev;
        while (mpsc_dequeue(bus->cross_world_events, &ev)) {
            RoomState *target = room_registry_find(rooms, ev.to_room);
            if (target) {
                // 把事件推入目标 World 的 InboundQueue
                // 目标 World 的 DrainSystem 下一帧处理
                mpsc_enqueue(target->io->inbound, &ev);
            }
        }
        usleep(1000);
    }
}
```

这个设计的关键：事件路由是独立的服务线程，不是 ECS System，它的职责只是把事件从发送方的出口队列路由到接收方的入口队列。World 之间没有直接引用，只通过消息传递。

---

## 六、World 池化：避免频繁 alloc/free

在高并发场景下（比如 5 分钟一局的休闲游戏，每分钟新增 20 个房间），World 的频繁创建和销毁会带来两个问题：

1. **内存分配开销**：`ecs_init()` 内部会分配大量内部数据结构（Archetype 哈希表、Query 缓存等），频繁 alloc/free 会产生内存碎片，拖慢分配器
2. **初始化开销**：Component 注册、System 注册、Observer 注册——这些操作在每个新 World 上都要重复执行

解决方案是 **World 池化**：

```c
// World 池
typedef struct {
    ecs_world_t **worlds;       // 预分配的 World 数组
    bool *in_use;               // 使用状态
    int pool_size;
    pthread_mutex_t lock;
} WorldPool;

WorldPool* world_pool_create(int size, RegisterFn register_fn) {
    WorldPool *pool = malloc(sizeof(WorldPool));
    pool->pool_size = size;
    pool->worlds = malloc(size * sizeof(ecs_world_t *));
    pool->in_use = calloc(size, sizeof(bool));
    pthread_mutex_init(&pool->lock, NULL);

    // 预先初始化所有 World，注册 Schema 和 System
    for (int i = 0; i < size; i++) {
        pool->worlds[i] = ecs_init();
        register_fn(pool->worlds[i]);  // 注册 Component、Tag、System
        // 关键：调用 ecs_reset 而不是 ecs_fini/ecs_init 来"清空"World
    }
    return pool;
}

ecs_world_t* world_pool_acquire(WorldPool *pool) {
    pthread_mutex_lock(&pool->lock);
    for (int i = 0; i < pool->pool_size; i++) {
        if (!pool->in_use[i]) {
            pool->in_use[i] = true;
            pthread_mutex_unlock(&pool->lock);
            return pool->worlds[i];
        }
    }
    pthread_mutex_unlock(&pool->lock);
    return NULL; // 池已满，需要扩容或等待
}

void world_pool_release(WorldPool *pool, ecs_world_t *world) {
    // 清空实体数据（Flecs 支持批量删除所有实体但保留 Schema）
    ecs_delete_with(world, EcsAny);   // 删除所有实体

    pthread_mutex_lock(&pool->lock);
    for (int i = 0; i < pool->pool_size; i++) {
        if (pool->worlds[i] == world) {
            pool->in_use[i] = false;
            break;
        }
    }
    pthread_mutex_unlock(&pool->lock);
}
```

**World 池化的性能收益**：在压测中，World 复用比每次 `ecs_init()/ecs_fini()` 快 3-5 倍（主要节省了 Schema 注册和内部哈希表初始化的开销）。对于高频创建/销毁房间的场景，这个差距很显著。

**注意**：`ecs_delete_with(world, EcsAny)` 会删除所有用户创建的实体，但 Component 定义、System 注册、Observer 注册会保留——这正是我们想要的。复用时只需要重新加载地图实体，不需要重新注册 Schema。

---

## 小结

一个进程多个游戏房间，应该选 Multi-World 方案，而不是单 World + Room Tag。单 World 方案的问题不只是性能，更深层是数据隔离失效和生命周期管理混乱——这两个问题随着房间数量增长会非线性恶化。

Multi-World 的关键工程点：
- World 生命周期的正确顺序：先关 I/O 入口，再执行最后一次 Tick，再持久化，再 `ecs_fini()`
- 共享资源按只读/可变分类：只读数据直接共享引用，可变数据必须消息化
- 跨 World 通信不允许 System 内直接操作另一个 World，只通过共享消息队列中转
- 高频场景考虑 World 池化，避免频繁 `ecs_init()/ecs_fini()` 的分配开销

下一篇 SV-ECS-08 讲同步仿真世界和异步网络世界之间的接缝——MPSC Queue 边界模式，以及在 Flecs、EnTT、Bevy ECS 里的具体实现。
