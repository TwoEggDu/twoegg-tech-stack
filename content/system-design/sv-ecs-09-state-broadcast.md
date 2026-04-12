---
title: "服务端 ECS 09｜状态广播：Component 变化同步、Delta 压缩与 AOI 的 ECS 实现"
slug: "sv-ecs-09-state-broadcast"
date: "2026-04-04"
description: "ECS World 里有几千个 Entity，每个都有几十个 Component，哪些变化需要告诉哪些客户端？全发太贵，不发又不对——这是状态广播的选择性问题，而不是序列化问题。"
tags:
  - "服务端"
  - "ECS"
  - "状态同步"
  - "游戏服务器"
  - "AOI"
  - "Delta压缩"
  - "带宽优化"
  - "广播"
series: "高性能游戏服务端 ECS"
primary_series: "server-ecs"
series_role: "article"
series_order: 9
weight: 2309
---

上一篇讲了 ECS 和异步 I/O 的接缝——MPSC Queue 边界模式，保证了网络包安全地进入 ECS World。这一篇讲另一个方向：ECS World 里发生的变化，如何安全地、高效地从服务端出去，传给客户端。

这个问题看起来是"序列化"问题，实际上核心是**选择性问题**：哪些变化要发？发给谁？多频繁发一次？序列化只是最后一步。

---

## 一、状态广播的三个核心问题

先把问题空间建清楚。

### 问题一：发什么？

理想的状态是：只广播真正发生变化的数据。如果一个 NPC 这帧没有移动、没有受到伤害、状态也没变，就什么都不发。把这个约束换一个角度：**每帧结束时，我怎么知道哪些 Component 的值和上一帧不一样了？**

朴素方案是每帧全量快照然后对比，代价极高。正确方案是 **Dirty Flag**：在 Component 被修改时打上标记，广播 System 只处理带标记的实体。

### 问题二：发给谁？

一个有 3000 个实体的 MMO 场景里，大多数实体的变化和大多数玩家无关——玩家只关心自己附近的实体。把所有变化发给所有玩家，带宽会爆炸。

需要 **AOI（Area of Interest，兴趣区域）**：每个玩家有一个感知半径，只广播感知半径内的实体变化给这个玩家。这把每次广播的实体数量从"全房间"压缩到"附近几十个"。

### 问题三：发什么格式，多频繁发？

即使已经用了 Dirty Flag + AOI，每帧广播的数据量仍然可能很大。进一步优化有两个方向：

**Delta 压缩**：不发完整状态，只发和上次发送时的差值。比如位置从 `(100, 50)` 变成 `(101, 50)`，可以只发 `dx=1, dy=0`。

**优先级/频率分层**：近处实体每帧广播，远处实体每 3 帧广播一次。不是所有变化都值得 20Hz 广播。

---

## 二、Dirty Flag：只广播变化过的 Component

Dirty Flag 是最核心的优化，决定了广播系统的数量级。

### 方案一：Tag 标记（推荐）

在 Flecs 里，用 Tag 做 Dirty 标记是最自然的方式：

```c
ECS_TAG(world, DirtyPosition);
ECS_TAG(world, DirtyHealth);
ECS_TAG(world, DirtyCombatState);

// MoveSystem：移动后打上 DirtyPosition
void MoveSystem(ecs_iter_t *it) {
    Position *pos = ecs_field(it, Position, 0);
    Velocity *vel = ecs_field(it, Velocity, 1);
    for (int i = 0; i < it->count; i++) {
        if (vel[i].x != 0 || vel[i].y != 0) {
            pos[i].x += vel[i].x * it->delta_time;
            pos[i].y += vel[i].y * it->delta_time;
            // 打标记：本帧位置发生了变化
            ecs_add(it->world, it->entities[i], DirtyPosition);
        }
    }
}

// BroadcastSystem：只处理有 DirtyPosition 的实体
void BroadcastPositionSystem(ecs_iter_t *it) {
    Position *pos = ecs_field(it, Position, 0);
    for (int i = 0; i < it->count; i++) {
        // 只广播这个实体给 AOI 范围内的玩家
        broadcast_position(it->world, it->entities[i], pos[i]);
        // 广播完毕后清除标记
        ecs_remove(it->world, it->entities[i], DirtyPosition);
    }
}

// BroadcastSystem 的 Query：只处理有 DirtyPosition 标记的实体
ECS_SYSTEM(world, BroadcastPositionSystem, EcsOnStore, Position, DirtyPosition);
```

Tag 方案的优点：利用 ECS 自身的 Query 机制，`DirtyPosition` 直接成为 Query 的过滤条件，框架底层只遍历满足条件的 Archetype，没有额外的逐实体 if 判断。

### 方案二：Observer 触发（事件驱动）

如果不想在每个 System 里手动打标记，可以用 Observer 自动响应 Component 修改：

```c
// Observer：Health 被修改时自动打 DirtyHealth 标记
ecs_observer_init(world, &(ecs_observer_desc_t){
    .filter.terms = {{ecs_id(Health)}},
    .events = {EcsOnSet},
    .callback = OnHealthSet
});

void OnHealthSet(ecs_iter_t *it) {
    for (int i = 0; i < it->count; i++) {
        ecs_add(it->world, it->entities[i], DirtyHealth);
    }
}
```

Observer 方案的代价：每次 `ecs_set(world, entity, Health, ...)` 都会触发 Observer，增加了一次 `ecs_add(DirtyHealth)` 的开销。在 Health 更新非常频繁的场景（每帧大量战斗计算）下，Observer 的触发次数可能很高。

实践建议：对修改频率高的 Component（`Position`、`Velocity`）用 System 内手动打标记，对修改频率低的 Component（`Inventory`、`Quest`）用 Observer 自动打标记。

### 帧末清除标记

所有 Dirty Tag 必须在广播完成后清除，否则下一帧没有发生变化的实体仍然会被广播。清除的时机是 BroadcastSystem 执行完之后，通常在 `EcsOnStore` Phase 末尾：

```c
void CleanDirtyFlagsSystem(ecs_iter_t *it) {
    // 批量删除所有 DirtyPosition 实体上的 Tag
    // 注意：这里不是逐实体删除，而是利用 ecs_delete_with 批量清理
}

// 更高效的做法：在 BroadcastSystem 内部广播完就立刻清除
// 利用 ecs_defer_begin/end 批量处理删除操作
```

---

## 三、AOI：ECS 里的兴趣区域实现

AOI 解决的是"发给谁"的问题。核心思路：每个玩家有一个感知半径 `R`，只接收距离自己 `R` 以内的实体的状态变化。

### 空间哈希（Spatial Hash）

最轻量的 AOI 实现，适合实体密度较均匀的场景：

```c
// 把地图划分成格子，每个格子维护一个实体列表
typedef struct {
    uint32_t grid_w;     // 格子列数
    uint32_t grid_h;     // 格子行数
    float cell_size;     // 每个格子的边长
    // 每个格子的实体列表（动态数组或链表）
    EntityList *cells;
} SpatialHash;

// MoveSystem 里更新格子归属
void MoveSystem(ecs_iter_t *it) {
    Position *pos = ecs_field(it, Position, 0);
    GridCell *cell = ecs_field(it, GridCell, 1);  // 当前格子坐标
    SpatialHash *grid = ecs_get_ctx(it->world);

    for (int i = 0; i < it->count; i++) {
        // 更新位置
        pos[i].x += vel[i].x * it->delta_time;
        pos[i].y += vel[i].y * it->delta_time;

        // 检查是否跨格子
        int new_cx = (int)(pos[i].x / grid->cell_size);
        int new_cy = (int)(pos[i].y / grid->cell_size);
        if (new_cx != cell[i].x || new_cy != cell[i].y) {
            // 从旧格子移出，加入新格子
            spatial_hash_move(grid, it->entities[i], cell[i].x, cell[i].y, new_cx, new_cy);
            cell[i].x = new_cx;
            cell[i].y = new_cy;
        }
    }
}

// 查询某玩家 AOI 范围内的实体
void get_aoi_entities(SpatialHash *grid, Position player_pos, float radius,
                      EntityList *out_entities) {
    int min_cx = (int)((player_pos.x - radius) / grid->cell_size);
    int max_cx = (int)((player_pos.x + radius) / grid->cell_size);
    int min_cy = (int)((player_pos.y - radius) / grid->cell_size);
    int max_cy = (int)((player_pos.y + radius) / grid->cell_size);

    for (int cx = min_cx; cx <= max_cx; cx++) {
        for (int cy = min_cy; cy <= max_cy; cy++) {
            EntityList *cell = spatial_hash_get(grid, cx, cy);
            entity_list_merge(out_entities, cell);
        }
    }
}
```

格子大小的选择经验：格子边长 = 1.5 × AOI 半径。太小会导致跨格子太频繁，太大会导致 AOI 查询返回太多格子。

### AOI 与 ECS Query 的结合

BroadcastSystem 不是用 ECS Query 直接遍历所有实体，而是先查空间哈希得到候选实体，再序列化这些实体的 Component：

```c
void BroadcastSystem(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);

    // 遍历所有在线玩家
    ecs_iter_t player_it = ecs_query_iter(it->world, ctx->player_query);
    while (ecs_query_next(&player_it)) {
        Position *player_pos = ecs_field(&player_it, Position, 0);
        PlayerSession *session = ecs_field(&player_it, PlayerSession, 1);

        for (int p = 0; p < player_it.count; p++) {
            // 查 AOI：获取这个玩家附近的实体列表
            EntityList nearby;
            entity_list_init(&nearby);
            get_aoi_entities(ctx->spatial_hash, player_pos[p], AOI_RADIUS, &nearby);

            // 序列化 Dirty 实体（AOI 内 + 有 Dirty 标记的）
            StateUpdatePacket packet;
            packet_init(&packet);

            for (int j = 0; j < nearby.count; j++) {
                ecs_entity_t entity = nearby.entities[j];
                if (ecs_has(it->world, entity, DirtyPosition)) {
                    const Position *pos = ecs_get(it->world, entity, Position);
                    packet_add_position(&packet, entity, pos);
                }
                if (ecs_has(it->world, entity, DirtyHealth)) {
                    const Health *hp = ecs_get(it->world, entity, Health);
                    packet_add_health(&packet, entity, hp);
                }
            }

            // 发送给这个玩家
            if (packet.count > 0) {
                io_send(session[p].connection, &packet);
            }
            entity_list_clear(&nearby);
        }
    }
}
```

---

## 四、Delta 压缩：只发变化量

即使有了 Dirty Flag + AOI，每次广播的数据量仍然可以优化。Delta 压缩的思路是：客户端已经知道实体上次发送时的状态，服务端只需要发送"和上次的差值"。

### 位置 Delta

玩家移动速度有上限，每帧位移量通常在 [-127, 127] 范围内（假设地图单位是像素，20Hz Tick，最大速度每秒 2000 单位）。可以用 int8 编码 delta：

```c
typedef struct {
    int8_t dx;   // 1 字节，范围 [-128, 127]
    int8_t dy;   // 1 字节
} PositionDelta;

// 服务端维护"每个玩家上次已发送的实体状态"
typedef struct {
    Position last_sent_pos;
    Health   last_sent_hp;
    uint32_t last_sent_frame;
} SentState;

void serialize_position_delta(const Position *current, const SentState *last,
                               PositionDelta *out) {
    float dx = current->x - last->last_sent_pos.x;
    float dy = current->y - last->last_sent_pos.y;
    // 超出 int8 范围时降级成全量发送（并加上标志位）
    out->dx = (int8_t)clampf(dx, -128, 127);
    out->dy = (int8_t)clampf(dy, -128, 127);
}
```

对于全量 `Position`（float × 2 = 8 字节），Delta 版本只需要 2 字节，节省 75%。

### 带宽预算估算

估算一个具体的带宽数字，帮助判断优化是否足够：

```
场景参数：
  - 每个房间 50 名玩家，500 个 NPC，50 颗子弹
  - 每名玩家的 AOI 内平均有 20 个其他玩家 + 30 个 NPC + 10 颗子弹 = 60 个实体
  - 广播频率：20Hz
  - 每个 Dirty 实体广播的平均数据量：Position(4B delta) + Health(2B delta) = 6B

每帧单玩家广播量 = 60 实体 × 假设 30% 的实体本帧有变化 = 18 实体
每帧单玩家数据量 = 18 × 6B + 包头(10B) = 118B
每玩家带宽 = 118B × 20Hz = 2360 B/s ≈ 2.3 KB/s（上行，服务端到客户端）

50 名玩家的房间总出口带宽 = 50 × 2.3 KB/s = 115 KB/s ≈ 0.9 Mbps
```

这个估算表明，一个 50 人房间在合理的 Dirty Flag + Delta 优化下，出口带宽在 1 Mbps 以内，是完全可接受的。如果不做这些优化（全量广播给所有玩家），带宽会是：600 实体 × 8B × 20Hz × 50 玩家 = 48 MB/s，相差 50 倍。

---

## 五、优先级广播：近处高频，远处低频

不是所有 AOI 内的实体都需要 20Hz 的广播频率。典型的分层策略：

```c
typedef enum {
    BROADCAST_PRIORITY_HIGH   = 0,  // 每帧广播（距离 < 近距离阈值）
    BROADCAST_PRIORITY_MEDIUM = 1,  // 每 2 帧广播
    BROADCAST_PRIORITY_LOW    = 2,  // 每 5 帧广播
} BroadcastPriority;

BroadcastPriority get_broadcast_priority(float distance) {
    if (distance < 50.0f)  return BROADCAST_PRIORITY_HIGH;
    if (distance < 150.0f) return BROADCAST_PRIORITY_MEDIUM;
    return BROADCAST_PRIORITY_LOW;
}

// BroadcastSystem 里加入帧计数过滤
void BroadcastSystem(ecs_iter_t *it) {
    uint64_t current_frame = ecs_get_world_info(it->world)->frame_count;
    // ...
    for (int j = 0; j < nearby.count; j++) {
        float dist = distance(player_pos[p], entity_pos);
        BroadcastPriority priority = get_broadcast_priority(dist);

        bool should_broadcast = false;
        switch (priority) {
            case BROADCAST_PRIORITY_HIGH:   should_broadcast = true; break;
            case BROADCAST_PRIORITY_MEDIUM: should_broadcast = (current_frame % 2 == 0); break;
            case BROADCAST_PRIORITY_LOW:    should_broadcast = (current_frame % 5 == 0); break;
        }

        if (should_broadcast && has_dirty_components(it->world, entity)) {
            serialize_and_enqueue(packet, it->world, entity);
        }
    }
}
```

优先级分层和 Dirty Flag 叠加使用：远处实体即使有 Dirty 标记，也可能这帧不广播（等到下次轮到它的周期再发）。Dirty Flag 在等待期间不清除，直到真正广播时才清除。这会引入一个问题：实体在等待期间又发生了多次变化，最终广播的是**最新状态**，而不是每次变化——这对位置、血量等连续状态是可接受的（客户端只需要最新值），对"死亡事件"等离散事件不适用（不能等到下次周期才告诉客户端实体已经死了）。

处理方案：区分**状态型 Component**（可以跳帧广播最新值）和**事件型 Component**（必须可靠、及时广播）。事件型用独立的可靠事件队列传输，不走 Dirty Flag + 分层广播。

---

## 六、Flecs Observer + 自定义序列化的组合

把上面的模式在 Flecs 里完整串起来：

```c
// 广播管道：Flecs Observer 驱动，AOI 过滤，Delta 序列化
void setup_broadcast_pipeline(ecs_world_t *world) {
    // 1. Observer：自动给 Dirty 实体打标记
    ecs_observer_init(world, &(ecs_observer_desc_t){
        .filter.terms = {{ecs_id(Position)}},
        .events = {EcsOnSet},
        .callback = OnPositionSet     // 添加 DirtyPosition Tag
    });

    ecs_observer_init(world, &(ecs_observer_desc_t){
        .filter.terms = {{ecs_id(Health)}},
        .events = {EcsOnSet},
        .callback = OnHealthSet       // 添加 DirtyHealth Tag
    });

    // 2. BroadcastSystem：AOI 查询 + 序列化 + 清理 Dirty
    ecs_system_init(world, &(ecs_system_desc_t){
        .entity = ecs_entity_init(world, &(ecs_entity_desc_t){
            .name = "BroadcastSystem",
            .add = {ecs_dependson(EcsOnStore)}
        }),
        .callback = BroadcastSystem,
        // 不需要 Query（内部用空间哈希查 AOI）
    });
}
```

整个数据流：

```
MoveSystem 修改 Position
    → ecs_set 触发 OnSet Observer
        → ecs_add(DirtyPosition) 打标记
    
[帧内其他 System 执行]

BroadcastSystem（EcsOnStore Phase）
    → 遍历所有在线玩家
        → get_aoi_entities 查空间哈希
            → 过滤 ecs_has(DirtyPosition) 的实体
                → serialize_delta 计算 delta
                    → io_send 推入 OutboundQueue
                → ecs_remove(DirtyPosition) 清标记
```

---

## 小结

状态广播不是序列化问题，是选择性问题。把问题分解成三层：
- **发什么**：Dirty Flag 只广播本帧发生变化的 Component，消灭无效广播
- **发给谁**：AOI 把广播范围从全房间压缩到附近几十个实体，减少 10-50 倍数据量
- **发什么格式/多频繁**：Delta 压缩减少每条广播的字节数，优先级分层进一步降低远距实体的广播频率

三层叠加，典型场景下带宽从"不优化"的 50MB/s 级别降到 1MB/s 以内，相差 50 倍。这不是理论数字，是可以通过前文的公式估算出来的工程边界。

事件型数据（死亡、复活、技能释放）不适合走 Dirty Flag + 分层广播，需要单独的可靠事件通道——这个边界要在设计时明确，否则会在测试中发现"死亡事件偶尔丢失"的 Bug。

下一篇 SV-ECS-10 讲持久化边界：ECS World 的状态如何安全地映射到数据库，冷热数据分离，以及宕机恢复策略。
