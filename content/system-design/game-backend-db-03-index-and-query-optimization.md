---
title: "索引与查询优化：游戏后端为什么慢，怎么看执行计划"
slug: "game-backend-db-index-and-query-optimization"
date: "2026-04-04"
description: "游戏后端慢查询从哪里来？索引加了不用或用错，比没加还危险。从执行计划的读法到游戏场景典型索引反模式，帮你系统定位并解决数据库性能问题。"
tags:
  - "游戏后端"
  - "数据库"
  - "索引优化"
  - "MySQL"
  - "查询优化"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 3
weight: 3003
---

## 这篇文章在解决什么问题

游戏上线后，第一个数据库性能问题通常来得很突然：压测没问题，真实玩家一多就开始报超时。DBA 或后端同学排查一圈，最后发现是某个查询"忘了加索引"或者"加了索引没生效"。

这是大多数人第一次接触慢查询排查的经历。但这个经历不会给你带来系统化的能力——下次遇到新的慢查询，还是靠猜。

慢查询排查需要一套可重复的方法论：

1. 知道慢查询从哪里找（慢查询日志 / 监控告警）
2. 知道怎么读执行计划（EXPLAIN 输出）
3. 知道常见的索引反模式（加了等于没加）
4. 知道游戏场景特有的慢查询来源

这四件事都有了，才叫"会优化"。

---

## 游戏场景典型慢查询来源

在正式讲执行计划之前，先建立对游戏数据库慢查询的感性认识。游戏后端最常见的慢查询来源有三类：

### 全表扫描背包

```sql
-- 查玩家持有的特定物品（典型的全背包扫描）
SELECT * FROM item_instance
WHERE item_template_id = 2001;  -- 忘了加 user_id 条件
```

这个查询如果只在 `item_template_id` 上有索引，它会找出所有持有该物品的玩家，这通常不是业务意图。但更常见的问题是连 `item_template_id` 的索引都没有，直接全表扫描百万行。

正确的查询应该是：
```sql
SELECT * FROM item_instance
WHERE user_id = ? AND item_template_id = 2001;
```
并且在 `(user_id, item_template_id)` 上建联合索引。

### 多条件过滤排行

```sql
-- 查某服务器、某赛季的前 100 名（看起来合理，实际是陷阱）
SELECT user_id, score
FROM player_rank
WHERE server_id = 3 AND season_id = 12
ORDER BY score DESC
LIMIT 100;
```

这个查询有三个条件（server_id、season_id、score），如果索引设计不对，MySQL 可能只用其中一个条件过滤，剩下的在内存里做 filesort，性能很差。

### 日志范围查询

```sql
-- 查过去 7 天的充值记录
SELECT * FROM payment_log
WHERE user_id = ? AND created_at >= '2026-03-28';
```

如果 `created_at` 没有索引，或者索引顺序是 `(created_at, user_id)` 而不是 `(user_id, created_at)`，这个查询的性能会差很多。联合索引的列顺序是关键知识点，后面详细说。

---

## 执行计划：EXPLAIN 输出怎么读

### 基本用法

```sql
EXPLAIN SELECT * FROM item_instance
WHERE user_id = 10001 AND item_template_id = 2001;
```

EXPLAIN 输出是一行或多行（多表 JOIN 时有多行），每行代表查询计划中的一个步骤。核心关注的列：

### type 列：访问类型（最重要）

从好到坏排序：

| type 值 | 含义 | 可接受性 |
|--------|------|---------|
| `system` | 单行系统表 | 最优 |
| `const` | 主键或唯一索引等值查询 | 最优 |
| `eq_ref` | JOIN 时每行对应唯一匹配 | 很好 |
| `ref` | 非唯一索引等值匹配 | 可接受 |
| `range` | 索引范围扫描 | 可接受 |
| `index` | 全索引扫描 | 较差，比 ALL 好有限 |
| `ALL` | 全表扫描 | 危险信号 |

游戏后端慢查询大多数情况下 type 是 `ALL` 或 `index`，需要立刻处理。

### rows 列：预估扫描行数

这是 MySQL 优化器的估算值，不是精确数字，但量级是准的。如果一个查询的 `rows` 是几十万，而表总行数也是几十万，就是全表扫描的信号。

### Extra 列：关键附加信息

- `Using index`：覆盖索引，查询只读索引不读表数据，很好
- `Using where`：在存储引擎层用 WHERE 过滤，通常表示有索引但有部分回表
- `Using filesort`：在内存（或磁盘）里排序，说明 ORDER BY 没走索引，通常需要优化
- `Using temporary`：用了临时表，通常在 GROUP BY / DISTINCT 场景，较差
- `Using index condition`：Index Condition Pushdown（ICP），MySQL 5.6+ 的优化

看到 `Using filesort` 或 `Using temporary`，就需要重新审查 ORDER BY / GROUP BY 的字段是否在索引里。

### EXPLAIN ANALYZE（MySQL 8.0+）

```sql
EXPLAIN ANALYZE SELECT ...;
```

这个命令会实际执行查询并返回真实的执行时间和行数，不是优化器估算，对排查准确性更高。代价是会真实执行，在生产环境慎用大查询。

---

## 索引类型与联合索引原则

### B-Tree 索引的本质

MySQL InnoDB 的普通索引是 B-Tree 结构，支持**等值查询**和**范围查询**，但对以下操作无能为力：

- 以通配符开头的 LIKE：`LIKE '%sword'`（前缀通配无法用索引）
- 对列做函数运算：`WHERE YEAR(created_at) = 2026`（函数破坏索引）
- 隐式类型转换：`WHERE user_id = '10001'`（user_id 是 INT，用字符串比较会转换，索引失效）

这三个场景是游戏后端索引失效最常见的原因。

### 联合索引的最左前缀原则

```sql
-- 假设索引是 (user_id, item_template_id, quality)
INDEX idx_item (user_id, item_template_id, quality)
```

这个联合索引可以用于：
- `WHERE user_id = ?`  （用了最左列）
- `WHERE user_id = ? AND item_template_id = ?`（用了最左两列）
- `WHERE user_id = ? AND item_template_id = ? AND quality = ?`（用了全部三列）

但不能用于：
- `WHERE item_template_id = ?`（跳过了最左列 user_id）
- `WHERE user_id = ? AND quality = ?`（中间跳过了 item_template_id）

**"最左前缀原则"**：联合索引按照从左到右的顺序使用，中间不能断。

### 索引列顺序的设计原则

上面提到的排行榜查询：
```sql
WHERE server_id = 3 AND season_id = 12 ORDER BY score DESC
```

索引应该设计为 `(server_id, season_id, score)`，让 WHERE 条件里的等值过滤列在前，ORDER BY 的排序列在后。这样 MySQL 可以用索引做排序，避免 filesort。

**等值条件列在前，范围/排序列在后**，是联合索引列顺序的核心原则。

### 覆盖索引

```sql
-- 查询只需要 user_id 和 score，不需要其他列
SELECT user_id, score FROM player_rank
WHERE server_id = 3 AND season_id = 12
ORDER BY score DESC LIMIT 100;
```

如果索引是 `(server_id, season_id, score, user_id)`，MySQL 可以只读索引，不需要回表（Back to Table）读取行数据。EXPLAIN 的 Extra 列会显示 `Using index`，这就是覆盖索引（Covering Index）。

对于高频查询，把查询返回的列全部加进索引（覆盖索引），是一个有效的优化手段。

---

## 游戏常见的索引反模式

### 反模式 1：低基数字段加索引

```sql
-- status 字段只有 0/1 两种值，不适合加索引
CREATE INDEX idx_status ON players (status);

SELECT * FROM players WHERE status = 0;  -- 封禁玩家，可能是全表的 1%
```

当查询结果占表行数的比例超过约 20-30% 时，MySQL 优化器可能放弃索引，直接全表扫描（因为回表的成本比全表扫描还高）。`status`、`gender`、`is_deleted` 这类字段单独加索引，通常没有实际效果。

正确的做法：把这些低基数字段作为**联合索引的次要列**，和高基数字段（如 `user_id`）组合使用。

### 反模式 2：过多索引导致写性能下降

每个索引都是一棵独立的 B-Tree，写入（INSERT / UPDATE / DELETE）时，所有索引都需要同步更新。一张表有 10 个索引，每次写入要更新 10 棵 B-Tree。

游戏后端的 `item_instance` 表是高频写的表（道具获取、消费、转移），如果在上面建了 7、8 个索引，写入性能会显著下降。

原则：**高频写的表，索引数量控制在 3-5 个以内**，删除没有被查询用到的冗余索引。

### 反模式 3：对 JSON 字段查询没有虚拟列索引

```sql
-- 查 ext_json 里 enchant_type = 'fire' 的物品
SELECT * FROM item_instance
WHERE ext_json->>'$.enchant_type' = 'fire';  -- 全表扫描！
```

JSON 字段里的属性默认无法索引。如果需要按 JSON 内部字段查询，应该用 Generated Column（虚拟列）加索引：

```sql
ALTER TABLE item_instance
ADD COLUMN enchant_type VARCHAR(32) GENERATED ALWAYS AS (ext_json->>'$.enchant_type') VIRTUAL;

CREATE INDEX idx_enchant_type ON item_instance (enchant_type);
```

---

## N+1 查询：应用层最常见的慢查询来源

N+1 查询不是数据库的问题，而是**应用层的查询模式问题**，但它制造的数据库压力是真实的。

典型场景：查询一个公会的所有成员信息

```python
# 错误的 N+1 查询
guild = db.query("SELECT * FROM guild WHERE guild_id = ?", guild_id)
members = db.query("SELECT user_id FROM guild_member WHERE guild_id = ?", guild_id)

# 对每个成员单独查询 profile，N 次查询
for member in members:
    profile = db.query("SELECT * FROM player_profile WHERE user_id = ?", member.user_id)
```

如果公会有 100 个成员，这段代码会发出 102 次数据库查询（1 次查公会 + 1 次查成员列表 + 100 次查 profile）。

**正确做法**：用 IN 查询批量拉取，或用 JOIN 一次性获取：

```sql
-- 批量查询（In 列表）
SELECT * FROM player_profile
WHERE user_id IN (?, ?, ?, ...);  -- 一次查询，N 个参数

-- 或者用 JOIN
SELECT pp.*
FROM guild_member gm
JOIN player_profile pp ON gm.user_id = pp.user_id
WHERE gm.guild_id = ?;
```

N+1 查询在使用 ORM 框架时尤其容易出现，因为关联关系通常是懒加载（Lazy Loading）的，每次访问关联对象都会触发一次查询。游戏后端如果用 ORM，要对懒加载保持警惕，热路径上关闭懒加载，改用预加载（Eager Loading）。

---

## 慢查询日志的开启与使用

生产环境排查慢查询，靠的不是猜，靠的是**慢查询日志**：

```sql
-- MySQL 开启慢查询日志
SET GLOBAL slow_query_log = 'ON';
SET GLOBAL long_query_time = 1;  -- 超过 1 秒的查询记录
SET GLOBAL slow_query_log_file = '/var/log/mysql/slow.log';
```

分析慢查询日志用 `pt-query-digest`（Percona Toolkit）：

```bash
pt-query-digest /var/log/mysql/slow.log > slow_report.txt
```

报告里会按查询模式聚合，显示每种查询的执行次数、总耗时、最大耗时，帮你找到"出现最频繁 × 每次最慢"的查询组合，这才是值得优先优化的目标。

---

## 工程边界

索引优化不该承担的职责：

**索引不能替代缓存**。一个查询即便有完美的索引，也有磁盘 I/O 的下限。对于高频热点数据（当前在线玩家的基础信息），应该用 Redis 缓存，而不是靠索引让数据库扛住。

**索引不能替代合理的 Schema 设计**。在一个错误的数据模型上加再多索引，也无法解决根本的结构问题。

---

## 最短结论

索引能解决的问题是"数据怎么找"，解决不了"数据模型是否合理"——慢查询排查要从执行计划入手，而不是直觉地加索引。
