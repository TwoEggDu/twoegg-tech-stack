---
title: "赛季与活动系统：时间窗口内容、Battle Pass 数据模型、活动期服务器压力"
slug: "game-backend-gaas-02-season-and-events"
date: "2026-04-04"
description: "Battle Pass 的奖励链怎么设计数据模型？活动开服的流量洪峰怎么用缓存预热应对？从数据结构到容量规划的系统性分析。"
tags:
  - "游戏后端"
  - "GaaS"
  - "赛季系统"
  - "Battle Pass"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 35
weight: 3035
---

## 时间窗口内容的本质

Live Service 游戏保持玩家持续活跃的核心手段，是在时间维度上设置内容节奏：每周有新任务，每月有赛季奖励，特定时期有限时活动。这种设计在商业上被称为 **内容节奏（Content Cadence）**。

从工程角度看，时间窗口内容制造了三个特殊的技术挑战：

**挑战 1：内容的有效性是时间依赖的。** 同一个任务在活动期间有效，在活动结束后无效。服务端必须管理内容的生命周期（开始时间、结束时间），并在边界时刻正确处理状态切换。

**挑战 2：边界时刻产生流量洪峰。** 活动开始的瞬间，大量玩家涌入查看新内容、领取初始奖励。赛季重置时，所有活跃玩家都需要初始化新赛季数据。这些操作在短时间内密集发生，是平时流量的数倍乃至数十倍。

**挑战 3：历史数据的归档与迁移。** 赛季结束后，旧赛季的玩家进度需要归档（可能用于排行榜历史、成就记录），新赛季需要重置进度但保留某些跨赛季的永久资产。

理解这三个挑战，是设计赛季与活动系统的起点。

---

## 赛季系统的数据模型

### 赛季主表

```sql
CREATE TABLE seasons (
    season_id       INT PRIMARY KEY,
    season_name     VARCHAR(100) NOT NULL,
    start_time      TIMESTAMP NOT NULL,
    end_time        TIMESTAMP NOT NULL,
    -- 赛季主题、图标等展示信息
    display_config  JSONB,
    -- 赛季状态：draft / active / ended
    status          VARCHAR(20) DEFAULT 'draft',
    created_at      TIMESTAMP DEFAULT NOW()
);
```

**关键设计决策：** `status` 字段不依赖实时对比当前时间和 `start_time/end_time` 来判断，而是由后台任务在时间窗口到来时主动写入。这样可以避免大量并发查询都去做时间计算，也让"手动提前开始/延迟结束"赛季的操作成为可能（只需改 `status`，不需要改时间）。

### 玩家赛季进度表

```sql
CREATE TABLE player_season_progress (
    player_id       BIGINT NOT NULL,
    season_id       INT NOT NULL,
    -- 赛季经验值（用于确定 Battle Pass 等级）
    season_exp      INT DEFAULT 0,
    -- 已领取的 Battle Pass 等级（领过的不重复领）
    claimed_tiers   INT[] DEFAULT '{}',
    -- 是否购买了付费 Battle Pass
    has_premium     BOOLEAN DEFAULT FALSE,
    -- 赛季积分排名相关
    rank_points     INT DEFAULT 0,
    last_updated    TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (player_id, season_id)
);
```

索引策略：`(player_id, season_id)` 是联合主键，查询特定玩家当前赛季的进度，走主键索引效率极高。历史赛季的数据可以分区存储（PostgreSQL 的 PARTITION BY LIST 或分表），避免活跃赛季查询时扫描历史数据。

---

## Battle Pass 的奖励链设计

Battle Pass 的核心是一条线性奖励链：玩家每达到一个 tier（等级），可以领取对应奖励。通常有免费奖励和付费奖励两种。

### Tier 奖励配置表

```sql
CREATE TABLE battle_pass_tiers (
    season_id           INT NOT NULL,
    tier_level          INT NOT NULL,       -- 1, 2, 3, ..., 100
    required_exp        INT NOT NULL,       -- 到达该 tier 需要的累计经验值
    -- 免费奖励（所有玩家可领）
    free_reward         JSONB,
    -- 付费奖励（仅购买 Battle Pass 的玩家可领）
    premium_reward      JSONB,
    PRIMARY KEY (season_id, tier_level)
);
```

示例数据：

```json
// tier_level = 10 的 free_reward
{
  "type": "currency",
  "currency_id": "gold",
  "amount": 500
}

// tier_level = 10 的 premium_reward  
{
  "type": "cosmetic",
  "item_id": "skin_warrior_gold",
  "display_name": "黄金战士皮肤"
}
```

### 奖励领取的服务端逻辑

```python
def claim_battle_pass_tier(player_id: int, season_id: int, tier_level: int):
    # 1. 查询玩家进度
    progress = db.query(PlayerSeasonProgress, player_id, season_id)
    
    # 2. 服务端验证：该 tier 是否已解锁（经验值是否足够）
    tier_config = db.query(BattlePassTier, season_id, tier_level)
    if progress.season_exp < tier_config.required_exp:
        raise Error("TIER_NOT_UNLOCKED")
    
    # 3. 服务端验证：该 tier 是否已领取
    if tier_level in progress.claimed_tiers:
        raise Error("TIER_ALREADY_CLAIMED")
    
    # 4. 确定可领取的奖励（免费 + 付费如果有的话）
    rewards = [tier_config.free_reward]
    if progress.has_premium and tier_config.premium_reward:
        rewards.append(tier_config.premium_reward)
    
    # 5. 发放奖励（具体实现依赖道具/货币系统）
    for reward in rewards:
        grant_reward(player_id, reward)
    
    # 6. 更新已领取记录
    progress.claimed_tiers.append(tier_level)
    db.update(progress)
    
    return rewards
```

**关键点：** 第 2 步和第 3 步的验证完全基于服务端数据，不接受客户端传入的"我已经达到 X 级了，给我奖励"。客户端只传 `tier_level`（想领第几层），服务端自己查经验值判断是否可以领取。

---

## 时间窗口任务的重置机制

每日任务和每周任务需要定期重置。常见的错误做法是"每次查询任务时动态计算是否过期"——这在高并发下会产生大量不必要的计算，也难以处理重置时机的并发问题（多个请求同时认为"需要重置"）。

### 推荐方案：定时任务 + 版本号

```sql
CREATE TABLE player_daily_tasks (
    player_id       BIGINT NOT NULL,
    task_date       DATE NOT NULL,        -- 属于哪一天的任务（服务器时区）
    task_id         INT NOT NULL,
    completed       BOOLEAN DEFAULT FALSE,
    completed_at    TIMESTAMP,
    PRIMARY KEY (player_id, task_date, task_id)
);
```

用 `task_date` 作为天级别的标识符，而不是存一条记录然后修改"重置时间"。每天的新任务就是 `task_date = today` 的新记录，历史任务自然归档在历史日期的行中，不需要物理删除或重置。

查询今日任务：`WHERE player_id = ? AND task_date = CURRENT_DATE`

这个设计的优点：
- 无需重置操作，每天的任务天然分开
- 历史任务完成记录自然保留（用于成就、活动统计）
- 不存在重置时并发写冲突的问题

---

## 活动开始时的流量洪峰

### 流量预估

活动开始的流量洪峰通常来自以下几类请求：

1. **活动信息查询：** 所有在线玩家同时查询"新活动是什么"
2. **活动任务初始化：** 服务端为每个玩家创建活动任务记录
3. **活动奖励配置拉取：** 客户端获取活动的奖励配置（道具图标、文本描述）

粗略估算：假设 DAU（日活跃用户）10 万，在活动开始后 5 分钟内 30% 的用户上线并触发活动相关请求，则 5 分钟内有 3 万次查询，折合约 100 QPS。

实际上，流量不会均匀分布在 5 分钟内，而是在活动开始的前 30 秒密集爆发（如果有倒计时提示），峰值 QPS 可能是平均值的 5-10 倍，即 500-1000 QPS。

**这是否会打垮服务器？** 取决于服务器的基线容量（通常日常运营时只用 20-30% 容量），以及请求是否命中了数据库还是缓存。命中数据库的请求，即使 500 QPS 也可能是问题；命中缓存的请求，5000 QPS 也没什么压力。

### 缓存预热策略

活动开始前，主动将高频读取的数据预加载到缓存（Redis），而不是等到第一次请求时才从数据库加载（冷启动问题）。

需要预热的典型数据：
- 活动配置信息（奖励列表、任务列表、时间信息）
- Battle Pass 的 tier 奖励配置
- 赛季排行榜的初始状态

```python
def preheat_season_cache(season_id: int):
    # 在赛季开始前 30 分钟触发
    
    # 1. 预热赛季配置
    season_config = db.query_season(season_id)
    redis.set(f"season:{season_id}:config", 
              json.dumps(season_config), 
              ex=3600)
    
    # 2. 预热所有 tier 奖励配置
    tiers = db.query_all_tiers(season_id)
    for tier in tiers:
        redis.set(f"season:{season_id}:tier:{tier.level}", 
                  json.dumps(tier), 
                  ex=3600)
    
    # 3. 预热排行榜结构
    redis.delete(f"season:{season_id}:leaderboard")
    # 此时排行榜为空，第一批玩家进入时会触发 ZADD
    
    logger.info(f"Season {season_id} cache preheated")
```

**预热触发方式：** 不要依赖手动触发，而是在赛季配置写入时自动调度（在 `start_time - 30 minutes` 触发预热任务）。

### 容量规划

活动期的容量规划原则：**按峰值的 2 倍来规划，而不是按平均值**。

原因：流量洪峰本身很难精确预测，留出 2 倍余量可以应对估算偏差。如果使用云服务，可以配置自动扩容策略（Auto Scaling），在 CPU 使用率超过 70% 时自动增加实例，活动结束后自动缩容。

---

## 赛季切换时的数据迁移

赛季结束时，需要处理两类数据：

**需要归档（保留，但不在活跃表中）：**
- 玩家的赛季最终排名（用于历史荣誉展示）
- 赛季总获得经验（用于成就统计）
- Battle Pass 领取记录（防止纠纷时查证）

**需要重置（新赛季初始化为零）：**
- 赛季经验值
- 赛季任务进度
- 排行榜积分

**迁移方案：** 不做物理删除和重置，而是在新赛季开始时使用新的 `season_id`。旧赛季数据自然保留在表中（可设置 TTL 或定期归档到冷存储），新赛季的数据从空白开始。

这是前文"用 `task_date` 作为标识符"思路的赛季级版本——**用新的标识符隔离新数据，而不是清除旧数据**。

---

## 工程边界

- **时区问题：** 赛季开始/结束时间必须明确是哪个时区。面向全球用户的游戏，通常统一用 UTC，在客户端展示时转换为本地时间。如果服务端时间与客户端时区不一致，每日任务的重置时间会让玩家困惑（"凌晨 4 点重置是什么鬼"）。
- **赛季切换不是瞬间完成的：** 在高 DAU 游戏中，赛季切换时为大量玩家初始化新赛季数据（INSERT）是一个重型操作。通常选择在流量低谷（如凌晨 4 点服务器维护窗口）切换，而不是在黄金时段。

---

## 最短结论

赛季与活动系统的数据模型核心是**用时间或版本标识符隔离不同时间窗口的数据**，而不是重置和覆盖。Battle Pass 的奖励链是"配置驱动 + 服务端验证"的标准模式：奖励条件服务端来判断，奖励结果服务端来发放。活动期的流量洪峰用缓存预热提前吸收，用容量规划保证服务不崩。
