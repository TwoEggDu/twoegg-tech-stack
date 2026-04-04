---
title: "游戏数据库表结构设计：玩家数据、背包、排行榜的实战方案"
slug: "game-backend-db-schema-design"
date: "2026-04-04"
description: "背包要不要单独一张表？排行榜为什么不能只靠 SQL ORDER BY？从玩家主表、物品实例、排行榜到时间序列日志，给出游戏数据库 Schema 设计的实战取舍依据。"
tags:
  - "游戏后端"
  - "数据库"
  - "Schema设计"
  - "MySQL"
  - "Redis"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 2
weight: 3002
---

## 这篇文章在解决什么问题

Schema 设计是游戏后端最容易"欠债"的环节。上线初期赶进度，随手设计的表结构往往在版本迭代中变成维护噩梦：

- 背包物品直接用一个 JSON 大字段存，查单件物品要把整个背包读出来再在应用层过滤
- 排行榜用 `ORDER BY score DESC`，日活过万之后每次查前 100 都要全表扫描
- 玩家主表不断加列，三年后变成 200 多列的"大宽表"，ALTER TABLE 要锁几分钟

这些问题的根源不是技术选型错了，而是在**设计阶段没有想清楚数据的访问模式**。表结构设计和业务逻辑是双向约束的——结构影响查询效率，访问模式决定结构取舍。

---

## 玩家主表：大宽表 vs 拆多表

### 大宽表的诱惑

把玩家所有数据放在一张表里，一个 `SELECT * FROM players WHERE user_id = ?` 搞定所有数据读取，代码简单，JOIN 为零。

这个方案在小规模项目里没有明显问题。问题在于它的**扩展代价随列数线性增长**：

- 加新功能需要加列，ALTER TABLE 有锁风险（即便是 Online DDL，也有元数据锁）
- 不同模块的数据（战斗属性、社交关系、VIP 信息）耦合在一张表里，读取时必须加载无关字段
- 200+ 列的表，哪些字段属于哪个业务模块，靠命名前缀来区分，维护成本极高

### 拆多表的正确方式

核心原则：**按访问频率和业务模块拆分，而不是按"感觉应该拆"**。

推荐的玩家数据表组织：

```sql
-- 核心账号表：每次登录都读，字段极少改动
CREATE TABLE player_account (
    user_id     BIGINT UNSIGNED PRIMARY KEY,
    username    VARCHAR(64) NOT NULL UNIQUE,
    created_at  DATETIME NOT NULL,
    last_login  DATETIME,
    status      TINYINT NOT NULL DEFAULT 1  -- 1=正常, 0=封禁
);

-- 游戏角色表：进入游戏时读取，迭代频率中等
CREATE TABLE player_profile (
    user_id     BIGINT UNSIGNED PRIMARY KEY,
    nickname    VARCHAR(64) NOT NULL,
    level       INT NOT NULL DEFAULT 1,
    exp         BIGINT NOT NULL DEFAULT 0,
    class_id    INT NOT NULL,            -- 职业
    vip_level   INT NOT NULL DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES player_account(user_id)
);

-- 资源货币表：高频读写，强一致性需求
CREATE TABLE player_currency (
    user_id     BIGINT UNSIGNED PRIMARY KEY,
    gold        BIGINT NOT NULL DEFAULT 0,
    diamond     BIGINT NOT NULL DEFAULT 0,
    energy      INT NOT NULL DEFAULT 0,
    updated_at  DATETIME NOT NULL,
    version     INT NOT NULL DEFAULT 0,  -- 乐观锁版本号
    FOREIGN KEY (user_id) REFERENCES player_account(user_id)
);
```

`player_currency` 单独拆出来，有一个重要原因：货币字段是**高频写且需要行级锁**的数据。如果放在 `player_profile` 里，每次扣金币都要锁整个玩家行，影响其他字段的并发写。

### 扩展属性的处理

游戏里经常有"临时属性"——比如某个节日活动的专属积分、某个版本新增的战斗统计字段。这类字段的生命周期短，加在主表里是污染。

常见的处理方式有两种：

**方案 A：JSON 扩展列**
```sql
ALTER TABLE player_profile ADD COLUMN ext_json JSON;
```
适合存少量临时属性，查询时用 `JSON_EXTRACT(ext_json, '$.activity_score')` 取值。MySQL 5.7+ 支持 JSON 类型，且可以在 JSON 字段的特定 Key 上建虚拟列索引（Generated Column）。

**方案 B：EAV 扩展表**
```sql
CREATE TABLE player_attribute (
    user_id     BIGINT UNSIGNED NOT NULL,
    attr_key    VARCHAR(64) NOT NULL,
    attr_value  VARCHAR(256),
    PRIMARY KEY (user_id, attr_key)
);
```
EAV 模式灵活，但查询效率差，不适合热路径。只在属性种类无法预估且属性数量稀疏时使用。

---

## 背包设计：物品实例模型

### 最常见的错误：JSON 大字段

```sql
-- 这个设计几乎在每个快速迭代的项目里都会出现
CREATE TABLE player_bag (
    user_id   BIGINT UNSIGNED PRIMARY KEY,
    items     JSON  -- [{id:1001, count:5, level:3}, ...]
);
```

这个设计的问题：
1. 查单件物品要读整个 JSON，在应用层遍历过滤
2. 更新单件物品需要 READ-MODIFY-WRITE，有并发安全问题
3. 无法用 SQL 查询"所有持有 ID=1001 物品的玩家"（需要全表扫）
4. 物品数量没有约束，一个玩家理论上可以持有无限物品而数据库不报错

### 正确方案：item_instance 表

```sql
-- 物品实例表：每行代表一件物品
CREATE TABLE item_instance (
    instance_id     BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    user_id         BIGINT UNSIGNED NOT NULL,
    item_template_id INT NOT NULL,          -- 物品模板ID（策划配置表）
    stack_count     INT NOT NULL DEFAULT 1, -- 可叠加物品的数量
    quality         TINYINT NOT NULL,       -- 品质（白/绿/蓝/紫/橙）
    level           INT NOT NULL DEFAULT 0, -- 强化等级
    bind_type       TINYINT NOT NULL,       -- 0=未绑定, 1=绑定
    acquire_time    DATETIME NOT NULL,
    expire_time     DATETIME,               -- NULL表示永久，限时道具有截止时间
    ext_json        JSON,                   -- 物品特殊属性（附魔、词条等）
    INDEX idx_user_id (user_id),
    INDEX idx_user_template (user_id, item_template_id)
);
```

**物品特殊属性用 `ext_json` 存**，因为不同物品的特殊属性结构差异大（武器有攻击词条，防具有防御词条，消耗品可能什么都没有），放 JSON 是合理的。但物品的核心属性（品质、等级、绑定状态）必须是独立列，方便索引和查询。

### 装备格子 vs 背包格子

很多游戏的背包有"格子数量"限制。这个限制**不应该在数据库层面用行数来控制**，因为格子扩展（购买背包格）是常见的付费点，频繁更改限制会很麻烦。

正确做法：格子上限存在 `player_profile` 里（`bag_capacity` 列），在应用层做格子数量校验，数据库层面只保证数据完整性。

---

## 排行榜：为什么不能只靠 SQL

### ORDER BY 的性能天花板

```sql
-- 这个查询在小规模时没问题，但随着用户增长会变成定时炸弹
SELECT user_id, nickname, score
FROM player_rank
ORDER BY score DESC
LIMIT 100;
```

即便在 `score` 上建了索引，这个查询的代价也会随数据量增长。更关键的问题是**"查询某玩家排名"**：

```sql
-- 查玩家自己的排名，这个查询更危险
SELECT COUNT(*) + 1 AS rank
FROM player_rank
WHERE score > (SELECT score FROM player_rank WHERE user_id = ?);
```

百万级数据下，这个查询即便有索引，也需要扫描大量行来计数。每个在线玩家打完一局都查一次排名，并发下数据库直接崩。

### Redis ZSet 是正确答案

Redis 的有序集合（Sorted Set）专门为排名场景设计，核心操作都是 O(log N)：

```
# 更新分数（战斗结算时调用）
ZADD leaderboard 9500 "user:10001"

# 查全服前 100（分页友好）
ZREVRANGE leaderboard 0 99 WITHSCORES

# 查某玩家排名
ZREVRANK leaderboard "user:10001"

# 查某段分数区间的玩家（赛季段位分布统计）
ZRANGEBYSCORE leaderboard 8000 9000
```

**数据库的角色是持久化**，Redis 的角色是实时排名计算。排行榜数据流：战斗结算 → 写入 DB（持久化） → 更新 Redis ZSet（实时排名）。如果 Redis 宕机，从 DB 重建 ZSet 即可（一次全量 ZADD 操作）。

### 排行榜分区

全服排行榜、区服排行榜、公会排行榜是不同的 ZSet Key，用命名规范区分：

```
leaderboard:global:season_12
leaderboard:server:s03:season_12
leaderboard:guild:12345:weekly
```

---

## 时间序列数据：日志与行为记录

### 别把日志存进 MySQL

玩家行为日志（登录、充值、战斗结果、道具使用）的特点：**高频写入、低频查询、不需要实时一致性**。

用 MySQL 存日志的问题：
- 写入量远大于读取量，B-Tree 索引的维护开销极大
- 日志表不需要频繁 UPDATE / DELETE，但 InnoDB 的多版本并发控制（MVCC）仍有开销
- 日志数据量增长快，单表行数容易破亿，每次查询都慢

推荐方案：

**方案 A：MongoDB**
写入吞吐高，JSON 文档天然适合结构不一致的日志。适合日活百万以下的游戏。

```javascript
db.player_logs.insertOne({
    user_id: 10001,
    event_type: "battle_end",
    timestamp: ISODate("2026-04-04T10:00:00Z"),
    data: {
        battle_id: "b_20260404_001",
        result: "win",
        duration_sec: 180,
        rewards: [{item_id: 2001, count: 3}]
    }
});
```

**方案 B：Kafka + ClickHouse**
Kafka 做消息缓冲，ClickHouse（列式存储）做日志落地和分析。ClickHouse 对时间范围查询和聚合统计有极高性能，适合需要 BI 分析的大型游戏。

---

## Schema 设计和业务扩展性的权衡

Schema 设计没有完美方案，只有**当前阶段代价最低的方案**。一些判断原则：

1. **核心数据严格建模**：账号、货币、核心道具，用清晰的列结构，不用 JSON 偷懒
2. **扩展属性允许 JSON**：活动属性、临时数据、配置扩展，放 `ext_json` 是合理的折中
3. **查询是结构的镜子**：先想清楚查询怎么写，再决定字段怎么放。"这张表怎么查"比"这张表存什么"更重要
4. **不要过早范式化**：三年后拆表比现在设计完美结构更容易——业务还没稳定就设计第三范式，是过早优化
5. **版本号列是标配**：高频写的表加 `version INT`，为乐观锁预留，后面文章会详细说

---

## 工程边界

Schema 设计不该承担的职责：

**应用层的业务校验不能依赖数据库约束**。`CHECK` 约束和触发器（Trigger）在业务层面能做的事情，不要放进数据库——这会让应用层和数据库强耦合，也让分库分表变得更复杂。

**计算结果不要存在数据库里**。"玩家总战斗力"是所有装备属性的计算结果，在 `player_profile` 加一列 `total_power` 并实时维护它，是错误的。计算结果应该在缓存层（Redis）维护，数据库只存原始数据。

---

## 最短结论

好的 Schema 设计来自对查询模式的理解，而不是对范式理论的套用——先想清楚数据怎么被读写，再决定表怎么建。
