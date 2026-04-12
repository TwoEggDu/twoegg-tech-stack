---
title: "游戏虚拟货币设计：硬币 / 钻石双货币体系、通货膨胀防控机制"
slug: "game-backend-economy-01-currency-design"
date: "2026-04-05"
description: "软硬货币的设计逻辑、发行与回收漏斗、通货膨胀检测指标，以及货币数据库设计中不能用 float 的原因。"
tags:
  - "游戏后端"
  - "游戏经济"
  - "货币系统"
  - "经济设计"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 51
weight: 3051
---

## 问题空间

一款上线三个月的 MMORPG，金币平均持有量从 50 万涨到了 800 万。玩家抱怨"什么都买不起"，
新玩家进场即劝退。这不是关卡数值的问题——这是货币系统崩了。

虚拟货币区别于真实货币的核心点：**发行方（游戏公司）同时控制发行量和消耗场景**。
这既是优势，也是陷阱。优势在于可以精密调控；陷阱在于一旦失控，修复成本极高（玩家已经囤了货）。

---

## 抽象模型

### 双货币体系

几乎所有商业成功的 F2P 游戏都采用双货币（或多货币）体系，核心逻辑如下：

```
软货币（Soft Currency）
  ├── 来源：日常任务、关卡掉落、活动奖励
  ├── 用途：强化、合成、低端消耗
  └── 特点：可大量获取，购买力有限

硬货币（Hard Currency / Premium Currency）
  ├── 来源：付费购买、极少量免费任务
  ├── 用途：抽卡、加速、高价值道具
  └── 特点：稀缺，驱动付费转化
```

两套货币分开的根本原因：**隔离付费玩家和免费玩家的经济轨道**，避免免费通货膨胀
污染付费资产的购买力。

### 货币的生命周期

```
发行（Faucet）→ 流通 → 回收（Sink）
```

| 阶段 | 典型操作 |
|------|----------|
| 发行 | 任务奖励、掉落、签到、活动、充值 |
| 流通 | 玩家钱包持有、交易转移 |
| 回收 | 强化消耗、抽卡、交易税、道具合成、限时活动 |

**Sink（漏斗）设计是防通胀的核心。** 发行端容易被低估——每新增一个任务奖励，
就必须同步评估对应的 Sink 容量是否够用。

---

## 具体实现

### 数据库设计：绝对不用 float

```sql
-- 错误做法
CREATE TABLE player_wallet (
    player_id BIGINT,
    gold      FLOAT,   -- 永远不要这样
    diamond   FLOAT
);

-- 正确做法：以"分"或最小单位存整数
CREATE TABLE player_wallet (
    player_id  BIGINT      NOT NULL,
    gold       BIGINT      NOT NULL DEFAULT 0,  -- 单位：枚
    diamond    INT         NOT NULL DEFAULT 0,  -- 单位：个
    version    BIGINT      NOT NULL DEFAULT 0,  -- 乐观锁版本号
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (player_id)
);
```

FLOAT 的问题：IEEE 754 浮点数在十进制运算中存在精度损失。
`0.1 + 0.2 = 0.30000000000000004` 在货币场景会导致账务差错，
规模越大差错越明显，且无法审计。

货币变更必须走**流水表**，不能直接 UPDATE wallet：

```sql
CREATE TABLE currency_ledger (
    id          BIGSERIAL   PRIMARY KEY,
    player_id   BIGINT      NOT NULL,
    currency    VARCHAR(16) NOT NULL,  -- 'gold' | 'diamond'
    delta       BIGINT      NOT NULL,  -- 正数为增加，负数为减少
    balance     BIGINT      NOT NULL,  -- 变更后余额（冗余存储，方便审计）
    reason      VARCHAR(64) NOT NULL,  -- 业务原因枚举值
    ref_id      VARCHAR(64),           -- 关联业务 ID（任务ID、订单ID等）
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

流水表的 `ref_id` 同时是**幂等键**：同一笔业务对应的 `ref_id` 只能写入一次，
防止网络重试导致的重复发放。

### 乐观锁更新余额

```sql
UPDATE player_wallet
SET    gold    = gold + :delta,
       version = version + 1,
       updated_at = NOW()
WHERE  player_id = :pid
  AND  version   = :expected_version
  AND  (gold + :delta) >= 0;  -- 防止余额为负

-- 检查 affected rows，若为 0 则表示并发冲突，重试或报错
```

---

## 通货膨胀检测指标

| 指标 | 含义 | 告警阈值（参考） |
|------|------|-----------------|
| 全服软货币总量 | Faucet - Sink 的净增量 | 7日增长 > 15% |
| 人均软货币持有量 | 按活跃玩家分层统计 | 中位数连续升 14 天 |
| 软货币消耗率 | Sink 总量 / Faucet 总量 | < 0.85 连续 3 天 |
| 高价值道具成交价 | 玩家交易行的实际成交 | 基准价上浮 > 30% |
| 新玩家留存与经济相关度 | 新玩家 7 日留存 vs 货币购买力 | 留存下降 + 货币膨胀同步 |

监控数据建议写入时序数据库（InfluxDB / Prometheus），每小时聚合，超阈值自动告警。

### 防控手段

1. **调节 Sink 强度**：临时活动增加消耗场景（限时合成、特殊强化材料）
2. **减少 Faucet 输出**：降低掉落率或任务奖励金额（需配合公告，否则玩家反感）
3. **货币兑换汇率调整**：软货币 → 硬货币的兑换比例（一般单向，不允许硬货币换软）
4. **引入新消耗层**：推出只接受软货币的高价值内容（坐骑、皮肤、仓库扩容）

---

## 工程边界

**不要做的事：**

- 不要允许软货币直接兑换硬货币（会破坏付费价值感）
- 不要在同一个 UPDATE 里修改多个货币类型（原子性难以保证，应拆事务或用 Lua + Redis）
- 不要把货币余额缓存在客户端作为权威数据（客户端只做展示，以服务端为准）
- 不要在没有流水表的情况下上线货币系统（出了问题无法排查）

**Redis 用于高频读，数据库用于权威存储：**

```
玩家打开背包 → 读 Redis 缓存余额（毫秒级）
玩家消耗货币 → 写数据库 + 失效 Redis 缓存（Write-through 或 Write-behind）
```

货币操作的幂等性保证：数据库唯一约束 `(player_id, ref_id, currency)` + 应用层重试逻辑。

---

## 最短结论

货币系统的本质是受控的经济模型：用整数流水表保证精度和幂等，用 Sink/Faucet 比率监控
通胀，软硬货币分离保护付费价值——任何一环失控都会让玩家用脚投票。
