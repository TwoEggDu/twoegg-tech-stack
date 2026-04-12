---
title: "排行榜、Session、匹配队列：Redis 在游戏后端的三个核心用法"
slug: "game-backend-cache-03-redis-game-patterns"
date: "2026-04-04"
description: "排行榜、Session 管理、匹配队列是游戏后端三类最典型的 Redis 使用场景。本文讲清楚为什么这三类功能天然适合 Redis、对应的数据结构选型、容量估算，以及 Redis Cluster 对这三种模式的影响。"
tags:
  - "游戏后端"
  - "Redis"
  - "排行榜"
  - "匹配系统"
  - "Session"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 8
weight: 3008
---

## 问题空间：数据库为什么做不好这三件事

排行榜、Session 存储、匹配队列——这三个功能在游戏里几乎是标配，但如果用关系型数据库来承载，都会遇到严重的性能问题。

**排行榜**：每次玩家战斗结束要更新分数并实时刷新排名。`SELECT rank FROM (SELECT uid, RANK() OVER (ORDER BY score DESC) as rank FROM leaderboard) t WHERE uid = ?` 这类查询在百万行时还能接受，但每秒几千次并发更新 + 读取排名，数据库会被压垮。关键在于，排行榜的每次更新都会改变其他玩家的相对排名，这是全表级别的操作，数据库无法优化。

**Session 存储**：玩家登录后的 Token 验证发生在每一个需要鉴权的 API 请求里，一个在线 100 万的游戏每秒可能有几十万次 Token 验证请求。这些请求不需要关系查询，只需要一个 key-value 查找，数据库的磁盘 I/O 和行锁对于这类高频点查询是严重的过度设计。

**匹配队列**：玩家进入匹配时入队，匹配成功时出队，整个过程需要原子性和低延迟。数据库的行级锁和事务机制在高并发出入队时产生大量锁等待，而且数据库不提供"阻塞等待直到有新数据"这种原语，需要轮询，浪费资源。

这三类场景有一个共同特征：**操作模式高度固定，不需要关系查询，对延迟极度敏感**。Redis 的数据结构和单线程原子语义恰好为这三类场景提供了最优解。

---

## ZSet 排行榜：原理与工程实践

### 基本原理

Redis ZSet（Sorted Set）的内部是跳表（skiplist）+ 哈希表的组合。跳表保证了按 score 排序的 O(log N) 查询和插入，哈希表保证了按 member 查找的 O(1) 查询。这两种查询恰好对应排行榜的两个核心操作：查某玩家的排名（按 member 找），查 Top N（按 score 范围找）。

```bash
# 更新玩家分数
ZADD leaderboard:pvp 9500 "uid:10001"

# 查询玩家排名（降序，返回 0-based index，加 1 得到名次）
ZREVRANK leaderboard:pvp "uid:10001"

# 查询 Top 100（降序，带分数）
ZREVRANGE leaderboard:pvp 0 99 WITHSCORES

# 查询分数在 8000-10000 之间的玩家数量
ZCOUNT leaderboard:pvp 8000 10000

# 查询某玩家的当前分数
ZSCORE leaderboard:pvp "uid:10001"
```

所有这些命令的时间复杂度：`ZADD` 是 O(log N)，`ZREVRANK` 是 O(log N)，`ZREVRANGE` 是 O(log N + M)（M 是返回的元素数量）。N 是排行榜总人数，M 是请求返回的条数。即使全服 500 万玩家，log₂(5,000,000) ≈ 22，每次操作的比较次数非常少。

### 同分排名问题的工程解法

当两个玩家分数相同时，ZSet 按 member 字典序排列，这不符合游戏需求（通常要求先达到分数的玩家排名靠前）。

工程解法：把时间信息编码进 score，让 score 同时携带分数和时间信息。

```python
import time

MAX_TS = 9999999999  # 比当前 Unix 时间戳大的一个固定值

def encode_score(game_score: int, timestamp: float) -> float:
    """
    高位：游戏分数（乘以大倍数占据高位）
    低位：时间偏移（越早达到，低位值越大，同分时排名越靠前）
    """
    time_component = MAX_TS - int(timestamp)
    return game_score * 1e10 + time_component

# 玩家 A 在 T=100 时达到 9500 分
score_a = encode_score(9500, 100)  # 95000009999999900

# 玩家 B 在 T=200 时达到 9500 分
score_b = encode_score(9500, 200)  # 95000009999999800

# score_a > score_b，所以 A 排在 B 前面（ZSet 降序时 A 先出现）
```

这个方案的前提是游戏分数和时间戳的数值范围不会溢出 float64（约 15-16 位有效数字）。设计时要根据游戏实际的分数上限调整倍率。

### 分段排行榜与跨 key 聚合

当排行榜需要按服务器、赛季、段位分组时，不同分组用不同的 key（`leaderboard:pvp:server1`、`leaderboard:pvp:season:2026Q1`）。需要查全服排名时，用 `ZUNIONSTORE` 合并多个 ZSet——但注意 `ZUNIONSTORE` 是 O(N) + O(M log M)，N 是所有参与合并的元素总数，M 是结果集大小，在全服合并时可能是个大操作，不应在请求链路里同步调用，而是离线定时合并。

### 容量估算

一个 ZSet member 的内存占用约 40-80 字节（包括 member 字符串、score、跳表指针、哈希表节点）。500 万玩家的全服排行榜：

```
5,000,000 × 80 bytes ≈ 400 MB
```

400 MB 对于一台 Redis 实例是完全可接受的。排行榜本身的内存开销不大，真正需要估算的是更新频率带来的 CPU 压力——500 万玩家同时在线、每场战斗结束后更新一次，每秒并发更新量要根据同时在线人数和平均战斗时长来估算。

### 过期策略

排行榜通常以赛季为周期，赛季结束后整个 key 可以直接 `DEL` 或使用 `EXPIREAT` 在赛季结束时间点自动删除。**不要给排行榜 key 设置短 TTL**——如果 TTL 到期时排行榜还在使用中，会导致数据丢失，玩家分数全部清零。

---

## Session 存储：分布式鉴权的 Redis 实现

### Session 的本质需求

Session 存储的核心需求是：给定一个 Token 字符串，在 O(1) 时间内查到对应的玩家信息，并且这个查找结果要在所有游戏服务器节点上保持一致。

这天然映射到 Redis 的 key-value 查找（String 或 Hash）。

### Token → 玩家信息映射

两种方案：

**方案 A：String 存 Token，值为序列化的玩家信息**
```bash
SET session:tok:abc123def456 '{"uid":"10001","server":"s1","exp":1712345678}' EX 86400
```

**方案 B：Hash 存 Token，字段为玩家信息**
```bash
HSET session:tok:abc123def456 uid 10001 server s1 login_time 1712345678
EXPIRE session:tok:abc123def456 86400
```

方案 A 的问题是每次都要序列化/反序列化整个 JSON；方案 B 可以单独读取某个字段（`HGET session:tok:abc123def456 uid`），更灵活，在 Session 信息字段不多（<10 个）时推荐使用方案 B。

### TTL 管理：滑动过期

游戏 Session 通常要求"活跃玩家不会被踢下线"，即在玩家活跃期间持续续期。实现方式是在每次成功鉴权后刷新 TTL：

```python
def verify_token(token: str) -> Optional[dict]:
    key = f"session:tok:{token}"
    session_data = redis.hgetall(key)
    if not session_data:
        return None
    
    # 每次验证成功后重置 TTL（滑动过期）
    redis.expire(key, 86400)  # 重置为 24 小时
    return session_data
```

这个模式的代价是每次鉴权都有一次 `EXPIRE` 调用（额外一次 Redis 操作）。在极高频场景下（每秒数十万次鉴权），可以用"每 5 分钟才续期一次"的策略减少 Redis 写操作：

```python
if int(session_data['last_refresh']) < time.time() - 300:
    redis.expire(key, 86400)
    redis.hset(key, 'last_refresh', int(time.time()))
```

### 分布式 Session 的一致性

在多节点 Redis（主从或 Cluster）下，Session 写操作（登录、踢出）要发到主节点，读操作可以路由到从节点。由于主从复制有延迟，刚登录的玩家 Token 写入主节点后，如果立刻有请求打到从节点，可能读不到刚写入的 Session，出现"刚登录就显示未登录"的情况。

解法：登录成功后的首次请求强制读主节点（通过业务层路由，或通过 sticky session 将该玩家的请求粘滞到同一后端节点）。

### 强制下线（踢号）

踢出某个玩家的 Session：直接 `DEL session:tok:{token}`。但如果玩家有多个 Token（多端同时登录），需要维护一个反向索引：

```bash
# 记录某玩家的所有活跃 Token
SADD session:uid:10001:tokens tok:abc123 tok:xyz789

# 踢出某玩家时，取出所有 Token 逐一删除
tokens = redis.smembers("session:uid:10001:tokens")
for token in tokens:
    redis.delete(f"session:{token}")
redis.delete("session:uid:10001:tokens")
```

---

## 匹配队列：List 与 ZSet 的组合

### 简单匹配队列：List RPUSH/BLPOP

最简单的 1v1 匹配队列用 List 实现：

```python
# 玩家加入匹配队列
def join_queue(uid: str):
    redis.rpush("queue:pvp:1v1", uid)

# 匹配服务取出两个玩家配对
def match_players():
    # BLPOP 阻塞等待，最多等 30 秒
    result1 = redis.blpop("queue:pvp:1v1", timeout=30)
    result2 = redis.blpop("queue:pvp:1v1", timeout=30)
    if result1 and result2:
        create_game(result1[1], result2[1])
```

`BLPOP` 的优势是阻塞等待直到队列有数据，不需要轮询，CPU 友好。但这个方案有一个严重问题：玩家 A 在前，玩家 B 在后，如果玩家 A 取消匹配，他的 uid 已经在 List 里，无法直接删除（List 的删除是 O(N)）。

**处理取消匹配**：有两种思路。第一种是"软删除"——取消匹配时只是在另一个 Set 里记录"已取消的玩家"，匹配服务 pop 出玩家后检查是否在取消集合里，如果是则跳过：

```python
def cancel_queue(uid: str):
    redis.sadd("queue:pvp:1v1:cancelled", uid)
    redis.expire("queue:pvp:1v1:cancelled", 300)

def match_players():
    while True:
        result = redis.blpop("queue:pvp:1v1", timeout=30)
        if not result:
            break
        uid = result[1]
        # 检查玩家是否已取消
        if redis.sismember("queue:pvp:1v1:cancelled", uid):
            redis.srem("queue:pvp:1v1:cancelled", uid)  # 清理取消记录
            continue
        # 继续匹配逻辑...
```

### 优先级匹配：ZSet 队列

当匹配需要考虑段位或等待时长时，List 无法按优先级出队，需要换成 ZSet：

```python
import time

def join_queue_with_priority(uid: str, mmr: int):
    # score 用等待开始时间（Unix 时间戳取负值，让等待越久的玩家 score 越小，优先出队）
    # 实际上通常用 mmr 段位作为第一维，等待时间作为第二维
    wait_start = time.time()
    redis.zadd("queue:pvp:ranked", {uid: wait_start})
    redis.hset(f"queue:pvp:ranked:meta:{uid}", "mmr", mmr, "joined_at", wait_start)

def find_match(uid: str):
    meta = redis.hgetall(f"queue:pvp:ranked:meta:{uid}")
    my_mmr = int(meta['mmr'])
    wait_secs = time.time() - float(meta['joined_at'])
    
    # 随着等待时间增加，扩大 MMR 匹配范围（避免等太久匹配不到）
    tolerance = min(50 + wait_secs * 2, 300)
    
    # 找 MMR 在范围内且等待最久的玩家
    # 这里需要在应用层过滤，或者用 Lua 脚本原子化
    candidates = redis.zrange("queue:pvp:ranked", 0, -1, withscores=True)
    for candidate_uid, score in candidates:
        if candidate_uid == uid:
            continue
        candidate_meta = redis.hgetall(f"queue:pvp:ranked:meta:{candidate_uid}")
        if abs(int(candidate_meta['mmr']) - my_mmr) <= tolerance:
            # 找到匹配对手，原子移除两人并创建对局
            return candidate_uid
    return None
```

注意 `zrange` 返回所有元素然后在应用层过滤，在队列很大时效率低。更高效的方案是用 Lua 脚本在 Redis 内原子化完成"查找 + 移除"，避免多次网络往返。

### 超时处理

玩家进入匹配队列后，如果超过一定时间没有匹配成功，需要自动移除。可以给玩家的 meta key 设置 TTL（比如 5 分钟），匹配服务定期扫描 ZSet，移除 meta key 已过期（已超时）的玩家：

```python
def cleanup_expired_players():
    all_in_queue = redis.zrange("queue:pvp:ranked", 0, -1)
    for uid in all_in_queue:
        if not redis.exists(f"queue:pvp:ranked:meta:{uid}"):
            redis.zrem("queue:pvp:ranked", uid)
            # 通知玩家匹配超时
```

这个清理任务应该定期运行（每 30 秒），而不是在每次匹配请求里运行，避免阻塞匹配主流程。

---

## Redis Cluster 对三种模式的影响

当 Redis 从单实例扩展到 Cluster 时，这三种模式都需要额外关注 key 的分布问题。

**排行榜**：如果有多个分区排行榜需要合并（`ZUNIONSTORE`），它们必须在同一 slot。可以用 hash tag 强制多个 key 落在同一 slot：`{leaderboard:pvp}:server1`、`{leaderboard:pvp}:server2` 都会被分配到 `leaderboard:pvp` 对应的 slot。

**Session**：Session key 通常是 `session:tok:{random_token}`，每个 key 独立，天然分布在不同 slot 上，Cluster 对 Session 几乎没有影响。唯一需要注意的是，玩家的 Token 列表（`session:uid:{uid}:tokens`）和 Token 对应的 Session（`session:tok:{token}`）可能在不同 slot，无法用事务保证原子性。这种情况下要么接受最终一致性，要么用 Lua 脚本（Lua 脚本在 Cluster 下要求所有涉及的 key 在同一 slot）。

**匹配队列**：队列 key 通常只有一个（`queue:pvp:1v1`），它落在某一个固定 slot 上，单个 slot 的处理能力是否够用是关键问题。如果匹配流量极高（每秒数千次入队/出队），单 slot 可能成为瓶颈。解法是按服务器、按段位分组，把一个大队列拆成多个小队列，分散到不同 slot。

---

## 工程边界：Redis 不能替代匹配服务的业务逻辑

这里有一个重要边界需要点明：Redis 提供的是数据结构和原子操作，而匹配系统的核心是匹配算法（MMR 计算、段位范围扩展、组队匹配、延迟补偿）。这些业务逻辑不应该写在 Redis Lua 脚本里，而应该在应用服务层处理。

Redis 在匹配系统里的角色是：提供原子的入队/出队操作，维护队列状态，保证并发安全。匹配算法由独立的 Match Service 进程运行，从 Redis 拿到候选玩家后在内存里做算法决策。把匹配算法塞进 Lua 脚本是错误的方向——不利于迭代，也会阻塞 Redis 的其他操作。

---

## 最短结论

排行榜、Session、匹配队列选 Redis 的本质原因是一样的：这三类操作的模式高度固定，不需要关系查询，对延迟敏感，而 Redis 把"高效数据结构"直接暴露在网络层，省掉了数据库的所有额外开销。
