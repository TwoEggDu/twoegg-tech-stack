---
title: "服务端 ECS 10｜持久化边界：ECS World 快照映射到数据库，冷热分离与宕机恢复"
slug: "sv-ecs-10-persistence-boundary"
date: "2026-04-04"
description: "ECS 是内存中的对象模型，数据库是持久化存储，两者数据格式完全不同。把 World 状态安全地写进数据库，需要设计三件事：映射策略、冷热分离、宕机恢复。"
tags:
  - "服务端"
  - "ECS"
  - "持久化"
  - "游戏服务器"
  - "数据库"
  - "快照"
  - "宕机恢复"
  - "冷热分离"
series: "高性能游戏服务端 ECS"
primary_series: "server-ecs"
series_role: "article"
series_order: 10
weight: 2310
---

ECS 架构在内存里是 SoA（Structure of Arrays）布局，同类 Component 的数据连续排列，为批量遍历优化。数据库是行式存储，每行是一个对象的完整属性，为随机查找优化。这两种模型的差距，比"序列化格式不同"更深——它们对"什么是一条数据"的理解就不一样。

把 ECS World 状态安全地写进数据库，不是简单地"把 Component 序列化成 JSON 存进去"。需要回答三个问题：**存什么（冷热分离）、什么时候存（持久化时机）、宕机后怎么恢复**。

---

## 一、问题空间：为什么不能直接序列化整个 World

最直接的想法是：每隔一段时间，把整个 ECS World 序列化成 JSON 或二进制，存进 Redis 或数据库。这个方案的问题：

**数据量太大，绝大多数数据无持久化价值**

ECS World 里存着大量运行时状态：NPC 的当前 AI 决策目标（`AITargetRef`）、子弹的飞行路径（`BulletPath`）、临时 Buff 的倒计时（`BuffTimer`）、当前帧的物理加速度（`Acceleration`）。这些数据在游戏会话内有意义，但在服务重启之后完全可以重新计算或丢弃——NPC 可以重新找目标，子弹可以重新生成。

如果全量序列化，这些数据会占据持久化内容的大多数，白白浪费 I/O 和存储。

**持久化频率和 Tick 频率的不匹配**

一个 20Hz Tick 的服务端每 50ms 更新一次 World 状态。如果每帧都全量持久化，50ms 内需要完成"序列化整个 World + 写入数据库"，而数据库写入通常需要几毫秒到几十毫秒。这会直接撑爆 Tick 预算。

**ECS 布局和关系型数据库的阻抗不匹配**

ECS 里，一个实体的数据分散在多个 Archetype Table 里（Position 在一张表，Health 在另一张表，Inventory 在第三张表）。关系型数据库通常希望一个对象对应一行或几行，而不是分散在 N 张表的 N 个位置。

直接映射的结果是要么写入大量 JOIN 查询，要么每帧 N × M 次数据库操作（N 个实体 × M 个 Component），性能极差。

---

## 二、冷热分离：哪些 Component 需要持久化

解决"存什么"的问题，核心原则是**冷热 Component 分离**。

### 热 Component（不持久化）

这类 Component 每帧都可能变化，但在服务重启后可以被重新初始化或直接丢弃：

```c
// 热 Component 示例：纯运行时状态
typedef struct { float x, y; } Velocity;           // 速度（可从输入重算）
typedef struct { ecs_entity_t target; } AITarget;  // AI 当前目标（重新寻敌）
typedef struct { float remaining; } BuffTimer;     // Buff 倒计时（服务重启后 Buff 失效是可接受的设计）
typedef struct { float vx, vy; } Acceleration;    // 物理加速度（每帧重算）
typedef struct { int frame_count; } TempFlag;      // 临时帧标记
```

### 冷 Component（需要持久化）

这类 Component 的值代表了"游戏进度"或"玩家资产"，服务重启后必须能恢复：

```c
// 冷 Component 示例：跨会话有价值的数据
typedef struct { float x, y; } Position;          // 玩家最后位置（断线重连恢复）
typedef struct { int current, max; } Health;       // 血量（不丢失玩家状态）
typedef struct { Item items[MAX_ITEMS]; } Inventory; // 背包（资产，绝对不能丢）
typedef struct { uint64_t exp; int level; } PlayerProfile; // 等级经验
typedef struct { QuestRecord records[MAX_QUESTS]; } QuestLog; // 任务记录
```

### 用 Tag 标记冷 Component

在 Flecs 里，可以用 Tag 来标记哪些实体的哪些 Component 需要持久化：

```c
// 定义 Persistent Tag：带这个 Tag 的实体会被持久化 System 处理
ECS_TAG(world, Persistent);

// 创建玩家时打上 Persistent 标记
ecs_entity_t player = ecs_new(world, 0);
ecs_add(world, player, Persistent);
ecs_set(world, player, Position, {100.0f, 200.0f});
ecs_set(world, player, Health, {100, 100});
ecs_set(world, player, PlayerProfile, {0, 1});

// NPC 不需要持久化，不打标记
ecs_entity_t npc = ecs_new(world, 0);
// ecs_add(world, npc, Persistent);  // 不打标记，持久化 System 不处理它
```

持久化 System 只处理有 `Persistent` Tag 的实体，NPC、子弹、场景对象全部跳过，数据量立刻降低几十倍。

---

## 三、Component 到数据库的映射策略

确定了"存什么"之后，要解决"怎么存"——ECS Component 如何映射到数据库。

### 策略一：每种 Component 一张表

最结构化的方案。每种需要持久化的 Component 对应一张数据库表：

```sql
-- Position 表
CREATE TABLE entity_position (
    entity_global_id BIGINT PRIMARY KEY,
    room_id INT NOT NULL,
    x FLOAT NOT NULL,
    y FLOAT NOT NULL,
    updated_at BIGINT NOT NULL  -- Unix timestamp in milliseconds
);

-- Health 表
CREATE TABLE entity_health (
    entity_global_id BIGINT PRIMARY KEY,
    room_id INT NOT NULL,
    current_hp INT NOT NULL,
    max_hp INT NOT NULL,
    updated_at BIGINT NOT NULL
);

-- Inventory 表（背包比较复杂，可能需要单独设计）
CREATE TABLE entity_inventory (
    entity_global_id BIGINT NOT NULL,
    slot_index INT NOT NULL,
    item_id INT NOT NULL,
    quantity INT NOT NULL,
    PRIMARY KEY (entity_global_id, slot_index)
);
```

**优点**：可以按 Component 类型独立查询和分析，Schema 清晰，易于 DBA 维护，也支持按列建索引。

**缺点**：恢复一个玩家需要 N 张表的 JOIN 或多次查询（N = 持久化的 Component 种类数）。如果有 10 种冷 Component，恢复一个玩家 = 10 次 SELECT。批量恢复 100 个玩家 = 最少 10 次批量 SELECT，加上 JOIN 逻辑，代码复杂度较高。

适合**背包、任务、角色属性**等更新不频繁、需要精细查询的 Component。

### 策略二：JSON 全量存一张表

把一个实体的所有冷 Component 序列化成 JSON，存入一张宽表：

```sql
CREATE TABLE entity_snapshot (
    entity_global_id BIGINT PRIMARY KEY,
    room_id INT NOT NULL,
    snapshot_json JSONB NOT NULL,    -- PostgreSQL JSONB，支持 JSON 字段索引
    created_at BIGINT NOT NULL,
    updated_at BIGINT NOT NULL
);
```

```c
// 序列化：把实体的所有冷 Component 打包成 JSON
cJSON *snapshot = cJSON_CreateObject();
const Position *pos = ecs_get(world, entity, Position);
const Health *hp = ecs_get(world, entity, Health);
const PlayerProfile *profile = ecs_get(world, entity, PlayerProfile);

cJSON_AddNumberToObject(snapshot, "pos_x", pos->x);
cJSON_AddNumberToObject(snapshot, "pos_y", pos->y);
cJSON_AddNumberToObject(snapshot, "hp_current", hp->current);
cJSON_AddNumberToObject(snapshot, "hp_max", hp->max);
cJSON_AddNumberToObject(snapshot, "exp", profile->exp);
cJSON_AddNumberToObject(snapshot, "level", profile->level);

char *json_str = cJSON_Print(snapshot);
db_upsert(db, "entity_snapshot", entity_global_id, json_str);
```

**优点**：Schema 灵活，新增 Component 不需要改数据库表结构；恢复一个实体只需要一次 SELECT；批量恢复 100 个实体只需要一次 `WHERE entity_global_id IN (...)` 查询。

**缺点**：JSON 序列化/反序列化有开销；JSON 字段无法直接建普通索引（PostgreSQL 的 JSONB 支持 GIN 索引，但查询语法比较繁琐）；字段重命名或类型变更需要迁移脚本。

适合**整体快照、简单实体数据**（没有复杂子对象，不需要按单字段查询）。

### 实践建议：混合使用两种策略

- **背包、技能树**这类有复杂子结构的 Component → 单独的规范化表
- **玩家基础属性**（位置、血量、等级）→ JSON 宽表（减少查询次数）
- **任务记录、成就**（需要按条件查询，比如"查询所有完成了任务 X 的玩家"）→ 单独表 + 索引

---

## 四、持久化时机的三种模式

### 模式一：游戏结束时批量写

最简单的策略：玩家离开、房间关闭时，批量持久化所有冷 Component。

```c
// 房间销毁流程里的持久化
void persist_room_snapshot(ecs_world_t *world, int room_id, DbConnection *db) {
    // 遍历所有 Persistent 实体
    ecs_query_t *persist_query = ecs_query(world, {
        .terms = {
            {ecs_id(Persistent)},
            {ecs_id(Position), ECS_IN},
            {ecs_id(Health), ECS_IN},
            {ecs_id(PlayerProfile), ECS_IN},
        }
    });

    // 开启数据库事务，批量写入
    db_begin_transaction(db);

    ecs_iter_t it = ecs_query_iter(world, persist_query);
    while (ecs_query_next(&it)) {
        Position *pos = ecs_field(&it, Position, 1);
        Health *hp = ecs_field(&it, Health, 2);
        PlayerProfile *profile = ecs_field(&it, PlayerProfile, 3);

        for (int i = 0; i < it.count; i++) {
            uint64_t global_id = get_global_id(it.entities[i]);
            db_upsert_player(db, global_id, pos[i], hp[i], profile[i]);
        }
    }

    db_commit(transaction);
    ecs_query_fini(persist_query);
}
```

**优点**：实现最简单，对 Tick 性能零影响（持久化在 Tick 结束后做）。

**缺点**：风险窗口长——如果游戏中途宕机，自上次持久化以来的所有变化丢失。对于"每局 30 分钟"的游戏，宕机可能丢失 30 分钟的进度，不可接受。

适合：每局时间短（< 5 分钟），玩家接受局内进度丢失的设计（比如回合制对战游戏，对战结果在对战结束时一次性写入）。

### 模式二：定期快照（Checkpoint）

每隔固定时间（30秒、60秒）执行一次快照，不依赖玩家离线事件：

```c
// 持久化 System：每 N 帧触发一次
void CheckpointSystem(ecs_iter_t *it) {
    RoomCtx *ctx = ecs_get_ctx(it->world);
    uint64_t frame = ecs_get_world_info(it->world)->frame_count;

    // 每 600 帧（20Hz 下约 30 秒）执行一次
    if (frame % 600 != 0) return;

    // 不能在 Tick 线程内同步写数据库，提交到异步持久化线程
    PersistJob job = collect_persist_data(it->world);
    thread_pool_submit(ctx->services->persist_pool, &job);
}
```

**关键：持久化必须异步，不能阻塞 Tick**。`collect_persist_data` 在 Tick 线程里快速收集需要持久化的数据（从 Component 读出值，打包成持久化请求），然后提交给独立的持久化线程去执行实际的数据库写入。

收集数据的时间应该控制在 1-2ms 以内，实际写库操作由异步线程承担，不占用 Tick 预算。

### 模式三：关键事件触发写入（事件日志）

对于高价值、低频的关键事件（物品购买、任务完成、等级提升），在事件发生时立刻写入，不等快照：

```c
// Observer：任务完成时立刻写入
ecs_observer_init(world, &(ecs_observer_desc_t){
    .filter.terms = {{ecs_id(QuestComplete)}},
    .events = {EcsOnAdd},
    .callback = OnQuestComplete
});

void OnQuestComplete(ecs_iter_t *it) {
    QuestComplete *quest = ecs_field(it, QuestComplete, 0);
    RoomCtx *ctx = ecs_get_ctx(it->world);

    for (int i = 0; i < it->count; i++) {
        // 推入事件日志队列（异步写入）
        EventLogEntry entry = {
            .entity_global_id = get_global_id(it->entities[i]),
            .event_type = EVENT_QUEST_COMPLETE,
            .quest_id = quest[i].quest_id,
            .timestamp = get_current_time_ms(),
        };
        mpsc_enqueue(ctx->services->event_log_queue, &entry);
    }
}
```

事件日志的设计借鉴数据库的 WAL（Write-Ahead Log）概念：关键操作的"发生"先于其效果被记录，即使服务在效果写入数据库之前崩溃，事件日志也保留了证据，可以在恢复时重放。

---

## 五、快照格式设计：二进制 vs JSON

快照格式的选择影响序列化速度、可读性和版本演进难度。

### 轻量二进制（生产推荐）

直接把 Component 的内存布局写入文件，速度最快：

```c
typedef struct {
    uint32_t magic;          // 格式标识符 0xECS10001
    uint32_t version;        // 快照版本（用于兼容性检查）
    uint64_t frame_count;    // 快照时的帧号
    uint64_t timestamp;      // Unix 时间戳
    uint32_t entity_count;   // 实体数量
} SnapshotHeader;

typedef struct {
    uint64_t global_id;      // 全局实体 ID
    Position pos;
    Health   hp;
    PlayerProfile profile;
    // 注意：可变长度数据（Inventory）需要单独存储
} PlayerSnapshot;
```

**优点**：序列化/反序列化极快（接近 memcpy），文件小。

**缺点**：字段顺序、类型改变时，旧快照无法直接加载（需要迁移工具）；不可读，调试困难。

**处理版本演进**：在 Header 里保存版本号，加载时检查版本，对旧版本快照运行迁移函数：

```c
void* load_snapshot(const char *path) {
    SnapshotHeader header;
    read_header(path, &header);
    if (header.version < CURRENT_VERSION) {
        migrate_snapshot(path, header.version, CURRENT_VERSION);
    }
    return load_snapshot_v_current(path);
}
```

### 可读 JSON（开发/调试阶段）

Flecs 内建了 World 的 JSON 序列化支持：

```c
// Flecs 内建序列化
ecs_world_to_json_desc_t desc = {
    .serialize_entities = true,
    .serialize_components = true,
    .serialize_type_info = true,   // 包含类型信息，便于反序列化时验证
};
char *json = ecs_world_to_json(world, &desc);
// 写入文件或 Redis
write_file("snapshot_debug.json", json);
ecs_os_free(json);

// 反序列化恢复
char *json_data = read_file("snapshot_debug.json");
ecs_world_from_json(world, json_data, NULL);
free(json_data);
```

**优点**：调试时可以直接用文本编辑器查看快照内容；Flecs Explorer 可以直接加载 JSON 快照；字段改名时只需要更新反序列化时的字段映射，不需要二进制迁移工具。

**缺点**：序列化/反序列化开销比二进制高 5-10 倍；文件大 3-5 倍。

**实践建议**：开发和测试阶段用 JSON 快照（便于调试），生产环境用二进制快照（低延迟），两套序列化器并存，通过配置切换。

---

## 六、宕机恢复策略：快照 + 事件日志重放

单靠快照无法做到零丢失：快照有时间间隔（比如 30 秒一次），宕机时最多丢失 30 秒的数据。结合事件日志可以把丢失窗口压到极低。

### 恢复流程

```
服务宕机后重启：

1. 从数据库加载最后一次成功的快照
   → 恢复所有玩家的基础状态（位置、血量、等级等）

2. 读取快照时间戳之后的事件日志
   → 重放关键事件（任务完成、物品获得、等级提升等）
   → 不重放瞬态事件（战斗过程中的血量变化）

3. 对无法重放的状态（如宕机时的战斗结果），做补偿处理
   → 常见补偿：战斗结果未知，双方血量恢复到战斗前快照值
   → 向玩家展示"服务器已恢复，部分战斗进度已回滚"的提示
```

```c
// 宕机恢复的 ECS 实现
void restore_from_snapshot(ecs_world_t *world, DbConnection *db,
                            const char *snapshot_path) {
    // 第一步：加载基础快照
    char *snapshot_data = read_file(snapshot_path);
    ecs_world_from_json(world, snapshot_data, NULL);
    free(snapshot_data);

    // 第二步：查询快照时间戳之后的事件日志
    SnapshotHeader header = read_snapshot_header(snapshot_path);
    EventLogEntry *events;
    int event_count = db_query_events_after(db, header.timestamp, &events);

    // 第三步：按时间顺序重放事件
    for (int i = 0; i < event_count; i++) {
        replay_event(world, &events[i]);
    }

    free(events);
}

void replay_event(ecs_world_t *world, const EventLogEntry *event) {
    ecs_entity_t entity = find_entity_by_global_id(world, event->entity_global_id);
    if (entity == 0) return;  // 实体可能在快照里不存在（已离线的玩家）

    switch (event->event_type) {
        case EVENT_QUEST_COMPLETE: {
            QuestLog *log = ecs_get_mut(world, entity, QuestLog);
            quest_log_mark_complete(log, event->quest_id);
            ecs_modified(world, entity, QuestLog);
            break;
        }
        case EVENT_LEVEL_UP: {
            PlayerProfile *profile = ecs_get_mut(world, entity, PlayerProfile);
            profile->level = event->new_level;
            profile->exp = event->new_exp;
            ecs_modified(world, entity, PlayerProfile);
            break;
        }
        // ... 其他关键事件类型
    }
}
```

### 持久化对 ECS 性能的影响分析

```
Tick 帧预算（50ms，20Hz）：
├── DrainInput System: ~0.5ms
├── MoveSystem: ~2ms
├── AISystem: ~15ms
├── CombatSystem: ~10ms
├── BroadcastSystem: ~5ms
├── collect_persist_data（同步，每 30 秒触发一次）: ~1ms
└── Sleep: ~16.5ms

异步持久化线程（独立，不占 Tick 预算）：
└── db_write_players: 5-50ms（取决于玩家数量和网络延迟）
```

关键约束：`collect_persist_data`（在 Tick 线程里运行）只做数据拷贝，不做数据库操作。实际数据库写入在独立线程里完成，即使数据库响应慢（50ms），也不会影响 Tick。

如果需要保证持久化完成（比如房间销毁时），需要等待异步持久化线程的 ack：

```c
void room_destroy_safe(RoomState *room) {
    // 触发持久化
    persist_async_submit(room->world, room->persist_pool);

    // 等待持久化完成（有超时，防止数据库故障卡死）
    bool success = persist_wait_completion(room->persist_pool, 5000 /* ms */);
    if (!success) {
        log_error("Persistence timeout for room %d", room->room_id);
        // 记录告警，人工处理
    }

    // 无论持久化是否成功，都继续销毁 World（不能因为数据库问题阻塞资源回收）
    ecs_fini(room->world);
    // ...
}
```

---

## 七、工程边界：持久化不是 ECS 的责任

最后一个重要的认知边界：**持久化逻辑不应该深入到 ECS 的 System 内部**。

常见的错误模式是让 AISystem、CombatSystem 在执行过程中直接调用数据库。这让 System 有了外部副作用，破坏了 System 的可测试性（单元测试需要 mock 数据库），也使 System 的执行时间不可预测（数据库延迟会传导进 Tick 预算）。

正确的分层：

```
ECS System 层：纯内存操作，修改 Component
    ↓ （Dirty Flag 或 Observer 触发）
持久化 System 层：收集变化，打包成持久化请求
    ↓ （推入队列）
异步持久化层：独立线程，负责实际数据库读写
```

ECS System 只关心 Component 数据的计算，不知道数据库的存在。持久化 System 是一个专门的 ECS System，它的职责是"发现变化并收集"，不做实际 I/O。实际 I/O 在 ECS 生命周期之外的异步线程里完成。

这个分层保证了：
1. 所有 ECS System 都是纯内存操作，执行时间可预测
2. 数据库故障不会影响游戏逻辑的运行（最多丢失部分持久化数据，但服务不崩溃）
3. System 可以独立单元测试（不依赖数据库 mock）

---

## 小结

ECS 持久化的核心是三件事：

**冷热分离**：明确哪些 Component 需要持久化。热 Component（运行时状态）不存，冷 Component（游戏进度和资产）按策略存。这一步通常能把需要持久化的数据量减少 80-90%。

**映射策略**：复杂子结构用规范化表，简单属性用 JSON 宽表。不要试图把整个 ECS Archetype 结构映射进数据库，会在 JOIN 和查询复杂度上付出很高代价。

**宕机恢复**：定期快照 + 关键事件日志是最成熟的组合。快照提供基础状态，事件日志补齐快照间隔内的关键变化。这个组合能把数据丢失风险压到秒级以内，同时不影响 Tick 性能。

持久化不是 ECS 的内置功能，是 ECS 外层的工程问题。ECS System 保持纯内存操作，持久化通过异步队列和独立线程在 ECS 生命周期之外完成——这是保证 Tick 预算稳定的根本前提。

本篇是服务端 ECS 系列的第 10 篇，到这里核心技术栈已经覆盖完整：从为什么需要 ECS（01）、五个约束（02），到框架选型（03-06），再到多 World 管理（07）、I/O 边界（08）、状态广播（09）、持久化（10）。整个链路的每个环节都有对应的设计模式和工程权衡。
