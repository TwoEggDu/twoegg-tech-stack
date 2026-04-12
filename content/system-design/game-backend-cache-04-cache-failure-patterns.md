---
title: "缓存穿透、击穿、雪崩：游戏后端的三类缓存故障原理与防护"
slug: "game-backend-cache-04-cache-failure-patterns"
date: "2026-04-04"
description: "游戏开服和活动期间缓存为什么经常崩？穿透、击穿、雪崩三类故障各有不同的触发机制和防护手段。本文从故障原理出发，讲清楚每类故障在游戏场景中的具体表现和对应的工程防护策略。"
tags:
  - "游戏后端"
  - "Redis"
  - "缓存"
  - "高可用"
  - "故障防护"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 9
weight: 3009
---

## 问题空间：游戏开服为什么总在缓存上栽跟头

游戏开服或大型活动上线，是后端最容易出故障的时刻。流量在几分钟内从零冲到峰值，远超平时的一个量级；玩家操作高度同质化（大家都在做同样的活动）；缓存是为稳态流量设计的，往往没有充分预热。这三个因素叠加，缓存系统承受的压力远超设计预期。

事故发生后复盘，几乎所有缓存相关的故障都可以归到三类模式之一：**穿透**（查的 key 根本不存在）、**击穿**（热 key 过期瞬间大量请求打穿）、**雪崩**（大量 key 同时失效，或 Redis 整体不可用）。

这三类故障的触发机制完全不同，防护手段也不同，混淆它们会导致用错药——用雪崩的方案去防击穿，或者把穿透误判为雪崩，都会浪费工程投入。理解三类故障的本质是后端设计健壮缓存系统的前提。

---

## 第一类：缓存穿透

### 故障原理

缓存穿透的定义是：请求的 key **在缓存和数据库中都不存在**，每次请求都会穿透缓存直接打到数据库，且数据库也返回空（NULL），缓存无法建立，每次同样的请求都会重复打库。

正常的 Cache-Aside 流程在"缓存未命中"时会读库、然后写缓存。但如果数据库也没有这条记录，就没有值可以写入缓存，下一次同样的请求会再次走完"缓存未命中 → 读库 → 库也没有 → 不写缓存"的整条链路。

**游戏场景的具体表现**：
- 攻击者脚本轮询不存在的玩家 UID（比如 uid=99999999、uid=88888888）
- 活动期间玩家访问已删除或未开放的道具 ID
- 排行榜查询已注销账号的历史数据
- 爬虫或外挂扫描不存在的 NPC 信息

开服时这类请求量可能非常大，特别是有人恶意刷接口的情况下。

### 防护手段 1：空值缓存（Null Cache）

对于查询结果为空的 key，在缓存里显式存储一个"空"标记，并设置较短的 TTL（通常 1-5 分钟）：

```python
def get_player_info(uid: str) -> Optional[dict]:
    cache_key = f"player:info:{uid}"
    cached = redis.get(cache_key)
    
    if cached is not None:
        # 缓存命中
        if cached == "__NULL__":
            return None  # 空值标记，直接返回 None，不打数据库
        return json.loads(cached)
    
    # 缓存未命中，读数据库
    player = db.query("SELECT * FROM player WHERE uid = ?", uid)
    
    if player is None:
        # 数据库也没有，缓存空值，TTL 设短一些（5 分钟）
        redis.set(cache_key, "__NULL__", ex=300)
        return None
    
    # 正常缓存，TTL 可以更长
    redis.set(cache_key, json.dumps(player), ex=3600)
    return player
```

**空值缓存的局限**：如果攻击者不断换不同的 key（枚举大量不存在的 UID），每个 key 只会被查一次数据库，之后被缓存为 NULL。但这会导致 Redis 里堆积大量的 NULL 缓存 key，消耗内存。短 TTL 可以缓解这个问题，但不能根治。

### 防护手段 2：布隆过滤器（Bloom Filter）

布隆过滤器可以在 O(1) 时间内以极低的内存消耗判断"某个 key 是否可能存在"。它的特性是：**判断不存在时 100% 准确，判断存在时有小概率误判**（即把不存在的 key 误判为存在，实际上不存在，这叫 false positive）。

对于缓存穿透防护，这个特性完全够用：在查缓存之前，先用布隆过滤器检查 key 是否可能存在，如果过滤器说"不存在"，直接返回空，完全不打缓存和数据库。

```python
# Redis 模块：redis-bloom，提供 BF.ADD / BF.EXISTS 命令
# 或者用 Python 的 pybloom_live 在内存里维护布隆过滤器

# 初始化：把所有合法的玩家 UID 加入布隆过滤器
def init_bloom_filter():
    all_uids = db.query("SELECT uid FROM player")
    for uid in all_uids:
        redis.execute_command("BF.ADD", "bloom:player:uid", uid)

def get_player_info(uid: str) -> Optional[dict]:
    # 先查布隆过滤器
    if not redis.execute_command("BF.EXISTS", "bloom:player:uid", uid):
        return None  # 过滤器确定不存在，直接返回
    
    # 可能存在，走正常缓存流程
    # ...
```

**布隆过滤器的维护成本**：新用户注册时要同步向过滤器添加；布隆过滤器不支持删除（删除已注销账号的 UID），需要使用 Counting Bloom Filter（计数布隆过滤器）或定期重建。游戏后端通常把布隆过滤器当"前置防护"使用，允许小概率 false positive（漏过去的不存在 key 会走空值缓存兜底）。

---

## 第二类：缓存击穿

### 故障原理

缓存击穿的定义是：**一个极高热度的 key（热 key）在某一时刻过期**，恰好在这个瞬间有大量并发请求到来，这些请求同时发现缓存未命中，同时去查数据库，数据库瞬间承受远超平时的查询压力。

和穿透的区别：穿透是 key 根本不存在，击穿是 key 曾经存在但刚刚过期。击穿通常只针对特定的热 key，而不是所有 key。

**游戏场景的具体表现**：
- 开服时全服玩家同时查询活动配置（`activity:config:2026newyear`），这个 key 设置了 1 小时 TTL，过期后大量请求同时打库
- 热门玩家的战斗数据（被大量玩家关注的主播账号）
- 全服公告缓存过期

击穿的典型特征是：在正常运行时完全没有问题，在热 key 过期那一刻出现数据库 QPS 尖刺。

### 防护手段 1：互斥锁（Mutex Lock）

缓存未命中时，不是所有请求都去查数据库，而是用分布式锁保证只有一个请求去重建缓存，其余请求等待：

```python
def get_activity_config(activity_id: str) -> Optional[dict]:
    cache_key = f"activity:config:{activity_id}"
    cached = redis.get(cache_key)
    if cached:
        return json.loads(cached)
    
    # 缓存未命中，尝试获取重建锁
    lock_key = f"lock:rebuild:{cache_key}"
    got_lock = redis.set(lock_key, "1", nx=True, ex=10)  # NX + 10秒超时防死锁
    
    if got_lock:
        try:
            # 获得锁，去数据库重建缓存
            data = db.query("SELECT * FROM activity WHERE id = ?", activity_id)
            redis.set(cache_key, json.dumps(data), ex=3600)
            return data
        finally:
            redis.delete(lock_key)
    else:
        # 没获得锁，短暂等待后重试（自旋等待）
        time.sleep(0.05)  # 50ms
        return get_activity_config(activity_id)  # 递归重试（注意递归深度限制）
```

**互斥锁的代价**：在高并发场景下，大量请求在 `time.sleep(0.05)` 上等待，请求响应时间延长。如果重建缓存的数据库查询本身耗时较长（比如复杂聚合查询），等待时间叠加后可能导致大量请求超时。

### 防护手段 2：逻辑过期（Logical Expiry）

逻辑过期的思路是：**不设置 Redis key 的物理 TTL**，而是在 value 里存储一个"逻辑过期时间"字段。缓存命中后检查逻辑过期时间，如果已过期，**立刻返回旧数据**，同时异步启动一个后台任务去重建缓存。

```python
import threading

def get_activity_config_logical_expire(activity_id: str) -> Optional[dict]:
    cache_key = f"activity:config:{activity_id}"
    cached_raw = redis.get(cache_key)
    
    if not cached_raw:
        # 冷启动：key 完全不存在，同步重建（只在首次访问时发生）
        data = db.query("SELECT * FROM activity WHERE id = ?", activity_id)
        wrapped = {"data": data, "expire_at": time.time() + 3600}
        redis.set(cache_key, json.dumps(wrapped))  # 不设 TTL
        return data
    
    wrapped = json.loads(cached_raw)
    
    if time.time() < wrapped['expire_at']:
        # 未过期，直接返回
        return wrapped['data']
    
    # 逻辑过期，立即返回旧数据（保证响应不受影响）
    # 同时异步重建缓存
    lock_key = f"lock:rebuild:{cache_key}"
    got_lock = redis.set(lock_key, "1", nx=True, ex=10)
    if got_lock:
        # 异步重建，不阻塞当前请求
        threading.Thread(target=rebuild_cache, args=(cache_key, activity_id, lock_key)).start()
    
    return wrapped['data']  # 返回旧数据

def rebuild_cache(cache_key: str, activity_id: str, lock_key: str):
    try:
        data = db.query("SELECT * FROM activity WHERE id = ?", activity_id)
        wrapped = {"data": data, "expire_at": time.time() + 3600}
        redis.set(cache_key, json.dumps(wrapped))
    finally:
        redis.delete(lock_key)
```

**逻辑过期的适用场景**：高一致性要求的场景不适合（会返回旧数据），但对于活动配置、公告、游戏平衡数据这类"短暂旧值不影响体验"的场景非常适合。逻辑过期彻底消除了击穿问题，代价是接受缓存更新有短暂延迟。

---

## 第三类：缓存雪崩

### 故障原理

缓存雪崩是规模最大、影响最严重的一类缓存故障，有两种触发方式：

**触发方式 1：大量 key 同时过期**。如果开服时批量写入大量缓存数据，而且这些 key 的 TTL 完全相同（比如都设置了 3600 秒），那么 3600 秒后这些 key 会同时过期，同一时刻所有查询都打到数据库，数据库在瞬间承受平时数十倍的压力。

**触发方式 2：Redis 整体不可用**。Redis 宕机、网络分区、主从切换期间服务中断，所有请求在这段时间内完全没有缓存保护，全部直接查数据库。

雪崩和击穿的区别：击穿是某一个热 key 的问题，雪崩是大量 key 同时失效（或缓存层整体失效）的问题。

**游戏场景的具体表现**：
- 开服后 1 小时，之前预热的缓存统一过期，数据库 QPS 突然飙升
- Redis 主节点宕机，主从切换耗时 30 秒，这 30 秒所有请求全部打库
- 活动结束后大量缓存在同一时刻 DEL，紧接着活动结束界面的查询打来

### 防护手段 1：随机过期时间

最简单也最有效的防护是在 TTL 上加随机抖动，把同时过期的 key 分散到不同时间点：

```python
import random

BASE_TTL = 3600  # 基础 TTL 1小时
JITTER = 600     # 抖动范围 ±10分钟

def cache_set_with_jitter(key: str, value: str, base_ttl: int = BASE_TTL):
    jitter = random.randint(-JITTER, JITTER)
    actual_ttl = base_ttl + jitter
    redis.set(key, value, ex=max(actual_ttl, 60))  # 最少 60 秒
```

这样原本同时到期的 key 会在 `BASE_TTL ± JITTER` 的范围内分散过期，数据库压力从"瞬间峰值"变成"平滑上升"。

### 防护手段 2：熔断与降级

当 Redis 不可用时，不能无限制地让所有请求打数据库。应该在缓存层实现熔断（Circuit Breaker）：检测到 Redis 连续失败超过阈值时，暂时切换到降级模式。

降级模式的选项（按保守程度从高到低）：
1. **直接数据库**：Redis 不可用时所有请求直接读库，只在数据库也能扛住的前提下适用
2. **本地内存缓存**：在业务服务进程本地维护一个小型 LRU 缓存，Redis 不可用时使用本地缓存
3. **返回默认值**：对于非关键数据（活动配置、公告），Redis 不可用时返回一个硬编码的默认值或最后一次成功的值
4. **拒绝请求**：对于无法降级的关键操作（如充值、道具交易），返回错误码要求客户端重试，保护数据库

```python
class CacheService:
    def __init__(self):
        self.circuit_open = False
        self.failure_count = 0
        self.failure_threshold = 10
        self.recovery_time = 30  # 30秒后尝试恢复
        self.last_failure_time = 0
        self.local_cache = {}  # 本地降级缓存

    def get(self, key: str) -> Optional[str]:
        # 检查熔断状态
        if self.circuit_open:
            if time.time() - self.last_failure_time > self.recovery_time:
                self.circuit_open = False  # 尝试半开状态
            else:
                return self.local_cache.get(key)  # 熔断中，用本地缓存
        
        try:
            value = redis.get(key)
            self.failure_count = 0  # 成功，重置计数
            if value:
                self.local_cache[key] = value  # 更新本地缓存作为备份
            return value
        except Exception:
            self.failure_count += 1
            self.last_failure_time = time.time()
            if self.failure_count >= self.failure_threshold:
                self.circuit_open = True
                logger.error("Redis circuit breaker opened")
            return self.local_cache.get(key)  # Redis 故障，降级到本地缓存
```

### 防护手段 3：多级缓存

多级缓存是雪崩最彻底的防护方案。在 Redis 之上增加一层本地内存缓存（L1），Redis 作为 L2：

```
请求 → L1 本地缓存（进程内，毫秒级，容量小）
         ↓ 未命中
       L2 Redis（分布式，毫秒级，容量大）
         ↓ 未命中
       L3 数据库（持久化，百毫秒级，全量数据）
```

L1 的 TTL 设置为较短（30-60 秒），L2 的 TTL 较长（几分钟到几小时）。Redis 雪崩时，L1 仍然能命中大部分热数据，显著减少打到数据库的请求量。

多级缓存的代价是数据一致性更难维护，以及 L1 缓存在多进程/多节点部署时各自独立，没有统一失效机制。游戏后端通常把 L1 用于"极低变化频率"的数据（游戏配置、数值表），这类数据只有在版本更新时才变，L1 一致性问题可以接受。

---

## 游戏开服/活动的缓存预热策略

以上三类防护都是被动防御。主动的预热策略可以从源头降低风险。

**什么是缓存预热**：在大流量到来之前，提前把热数据加载到缓存，避免流量冲击时大量缓存未命中。

**预热的两个核心问题**：
1. **预热什么数据**：根据历史流量分析，识别开服时最频繁访问的数据——通常是活动配置、开始界面显示的排行榜、签到奖励配置、商城道具列表。这些数据在开服前几分钟批量写入 Redis。
2. **预热的速度控制**：如果一次性写入 10 万条记录，会给 Redis 带来瞬时压力，也可能占满带宽。应该分批次、限速写入（比如每秒最多写入 1000 条）。

**预热脚本的基本结构**：
```python
def warmup_cache(batch_size: int = 500, delay_ms: int = 100):
    """开服前执行，分批预热热数据"""
    hot_player_uids = get_top_active_players(limit=10000)
    
    for i in range(0, len(hot_player_uids), batch_size):
        batch = hot_player_uids[i:i + batch_size]
        pipeline = redis.pipeline()
        
        for uid in batch:
            player_data = db.query("SELECT * FROM player WHERE uid = ?", uid)
            if player_data:
                pipeline.set(
                    f"player:info:{uid}",
                    json.dumps(player_data),
                    ex=BASE_TTL + random.randint(-600, 600)  # 带抖动的 TTL
                )
        
        pipeline.execute()
        time.sleep(delay_ms / 1000.0)  # 批次间等待，控制速度
    
    logger.info(f"Cache warmup complete: {len(hot_player_uids)} players loaded")
```

**预热的局限**：预热只能覆盖可预测的热数据。真正的开服流量往往有大量难以预测的长尾请求（玩家互相查看对方信息、访问从未被缓存的旧角色等），这些只能靠运行时的缓存机制自然填充。

---

## 什么时候应该主动接受缓存不可用

这是一个常被忽视但非常重要的工程决策：**不是所有情况下都应该无限重试缓存，有些时候主动接受缓存失败是正确的选择**。

以下情况应该主动降级而不是重试：

1. **Redis 连续失败超过阈值**：无限重试会占用线程资源，导致请求队列堆积，最终整个服务不可用。熔断后返回降级数据或错误码，给 Redis 时间恢复。

2. **缓存穿透攻击已确认**：如果监控发现大量特定 IP 在请求不存在的 key，应该在网关层封禁这些 IP，而不是让请求继续走到缓存层。继续处理这些请求只会消耗资源。

3. **数据库本身已经过载**：Redis 雪崩导致数据库 QPS 超过阈值时，不应该让更多请求进来"帮助数据库恢复"——而是应该用限流（Rate Limiting）把请求挡在入口，保护数据库有足够的资源处理已有请求。

4. **非关键数据的缓存**：排行榜、活动公告、好友在线状态等数据，缓存不可用时完全可以返回"暂时不可用，请稍后重试"，而不是冒着压垮数据库的风险强行读库。

**关键原则**：缓存的首要职能是保护数据库，而不是保证自己永远可用。当缓存和数据库之间需要做取舍时，优先保护数据库——数据库恢复后，缓存可以重建；数据库崩溃后，缓存重建也没有意义。

---

## 三类故障对比速查

| 故障类型 | 触发条件 | 数据库表现 | 主要防护 |
|----------|----------|------------|----------|
| 穿透 | 请求不存在的 key | QPS 持续升高，查询量大 | 布隆过滤器 + 空值缓存 |
| 击穿 | 热 key 突然过期 | 瞬间 QPS 尖刺 | 互斥锁 / 逻辑过期 |
| 雪崩 | 大量 key 同时过期 / Redis 宕机 | QPS 大面积升高或直接崩溃 | 随机 TTL + 熔断降级 + 多级缓存 |

---

## 最短结论

穿透、击穿、雪崩三类故障的触发机制完全不同，识别故障类型是选择正确防护手段的前提；而所有防护策略的底层逻辑只有一条：缓存是数据库的保护盾，防护设计的终极目标是让数据库永远不被流量打垮。
