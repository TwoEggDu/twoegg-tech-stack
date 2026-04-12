---
title: "游戏内交易与拍卖行：P2P 交易设计、手续费模型、RMT 防控"
slug: "game-backend-economy-03-trading-auction"
date: "2026-04-05"
description: "拍卖行数据模型、手续费作为货币回收机制、现实货币交易的识别与打击，以及交易系统的反洗钱风控思路。"
tags:
  - "游戏后端"
  - "游戏经济"
  - "拍卖行"
  - "RMT防控"
  - "风控"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 53
weight: 3053
---

## 问题空间

某 MMORPG 上线拍卖行后，官方货币购买力下跌 40%。调查发现：工作室每天批量刷材料
在拍卖行低价倾销，同时将收到的游戏金币通过"账号转让中间商"换成现金（RMT）。
受害者是正常玩家——既买不到合理价格的材料，投入的付费资源也被通胀吃掉。

P2P 交易是游戏经济中最难控制的变量：你给了玩家自由，就同时给了黑产入口。
**拍卖行能否健康运转，取决于设计时是否把风控当成一等公民。**

---

## 抽象模型

### 交易模式分类

```
直接交易（Direct Trade）
  └── 两名玩家面对面交换道具 / 货币
      问题：无审计轨迹，洗钱最简单

拍卖行（Auction House）
  ├── 限时竞拍（Auction）：出价最高者得
  ├── 即时购买（Buyout）：按标价直接成交
  └── 委托出售（Listing）：挂单等待买家

P2P 信箱交易（Mail Trade）
  └── 发送道具，收到回邮货币
      问题：异步，难以实时监控
```

拍卖行相比直接交易的优势在于**所有交易经过中间方（服务器）撮合**，
留下完整记录，可以做事后分析和实时风控。

---

## 具体实现

### 拍卖行数据模型

```sql
-- 挂单表
CREATE TABLE ah_listing (
    id           BIGSERIAL    PRIMARY KEY,
    seller_id    BIGINT       NOT NULL,
    item_id      INT          NOT NULL,
    item_uuid    UUID         NOT NULL,   -- 具体道具实例ID
    count        INT          NOT NULL DEFAULT 1,
    listing_type VARCHAR(16)  NOT NULL,   -- 'auction' | 'buyout' | 'both'
    start_price  BIGINT       NOT NULL,   -- 起拍价（最小货币单位）
    buyout_price BIGINT,                  -- 一口价，NULL表示仅竞拍
    current_bid  BIGINT,                  -- 当前最高出价
    top_bidder   BIGINT,                  -- 当前最高出价玩家
    expires_at   TIMESTAMPTZ NOT NULL,
    status       VARCHAR(16)  NOT NULL DEFAULT 'active',
    -- 'active' | 'sold' | 'expired' | 'cancelled'
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 出价记录表（竞拍历史）
CREATE TABLE ah_bid (
    id         BIGSERIAL   PRIMARY KEY,
    listing_id BIGINT      NOT NULL REFERENCES ah_listing(id),
    bidder_id  BIGINT      NOT NULL,
    amount     BIGINT      NOT NULL,
    bid_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 成交记录表
CREATE TABLE ah_transaction (
    id            BIGSERIAL   PRIMARY KEY,
    listing_id    BIGINT      NOT NULL,
    seller_id     BIGINT      NOT NULL,
    buyer_id      BIGINT      NOT NULL,
    item_id       INT         NOT NULL,
    item_uuid     UUID        NOT NULL,
    sale_price    BIGINT      NOT NULL,
    fee_amount    BIGINT      NOT NULL,   -- 手续费
    seller_net    BIGINT      NOT NULL,   -- seller实收 = sale_price - fee_amount
    completed_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 手续费模型

手续费是拍卖行最重要的 **Sink 机制**。货币从买家流向卖家时，
一部分被系统销毁（不归任何玩家所有），等效于通缩操作。

```
常见手续费模型：
  固定费率：成交价 × 5%（简单，但高价值道具 Sink 量大）
  挂单押金：挂单时预扣，流拍仍收（惩罚无效挂单，减少垃圾刷屏）
  成功方收取：仅成交时向卖家收（最常见）
  双向收取：买卖双方各付 2.5%（对高频刷单打击更强）
```

设计原则：**手续费率要让黑产工作室的利润空间压缩到接近零**。
若工作室刷材料的利润率是 30%，手续费至少要 20% 以上才有抑制效果。

```sql
-- 成交时计算手续费并原子更新
BEGIN;

-- 扣除买家货币
UPDATE player_wallet SET gold = gold - :sale_price WHERE player_id = :buyer_id;

-- 给卖家结算（扣除手续费）
UPDATE player_wallet SET gold = gold + :seller_net WHERE player_id = :seller_id;
-- 手续费直接销毁，不写入任何玩家账户

-- 转移道具所有权
UPDATE player_item SET owner_id = :buyer_id WHERE uuid = :item_uuid;

-- 记录成交流水
INSERT INTO ah_transaction (...) VALUES (...);

-- 更新挂单状态
UPDATE ah_listing SET status = 'sold' WHERE id = :listing_id;

COMMIT;
```

---

## RMT 防控

### RMT 的运作链条

```
黑产工作室（刷材料）
    ↓
拍卖行低价出售
    ↓
收集大量游戏金币
    ↓
将金币账号卖给买家（场外交易）
    ↓
买家用金币购买游戏内高价值道具
```

识别 RMT 的关键不是单笔交易，而是**交易网络的异常模式**。

### 检测指标

| 指标 | 正常范围 | 异常信号 |
|------|----------|----------|
| 单账号 24h 成交笔数 | < 50 | > 500 |
| 单账号买入金额占比 | < 5% 全服 | > 20% |
| 账号间资金流向 | 随机分布 | 明显的"1→N"或"N→1"星型 |
| 新账号挂单速度 | 注册 > 7 天才活跃 | 注册当天即大量挂单 |
| IP / 设备聚集 | 同IP < 3 账号 | 同IP > 20 账号 |
| 成交价格偏差 | 在市场均价 ±30% 内 | 远低于市场价的"友善出售" |

### 反制手段

**交易冷却期（Trade Cool-down）**
新账号注册后 N 天内不能使用拍卖行，封堵批量注册即用的工作室。

**交易总量限制（Rate Limiting）**
每账号每日最多挂单 X 件，成交总金额不超过 Y，超限自动暂停并人工审核。

**图谱分析（Transaction Graph）**
将所有交易关系构建成有向图，用社区发现算法（Louvain、Label Propagation）
找出资金集中流向的异常节点。同一社区内的账号若多为工作室特征，批量处理。

```python
# 伪代码：构建交易图，找异常节点
import networkx as nx

G = nx.DiGraph()
for tx in transactions_last_7d:
    G.add_edge(tx.seller_id, tx.buyer_id, weight=tx.sale_price)

# 检查入度异常高的节点（大量货币流入）
for node, in_degree in G.in_degree():
    if in_degree > THRESHOLD:
        flag_for_review(node)
```

**异常价格报警**
成交价低于市场中位价 50% 的交易，自动挂起等待审核（可能是定向转移财富给买家小号）。

---

## 交易系统反洗钱思路

游戏内洗钱路径：真实货币 → 游戏充值 → 购买道具 → 拍卖行出售 → 游戏货币 →
转给其他账号 → 通过其他途径换回法币。

防控要点：

1. **大额充值 KYC**：单日充值超过一定金额触发身份核验（各地区合规要求不同）
2. **充值后锁定期**：充值后 24-48h 内限制大额交易，打断快速洗钱链路
3. **账号关联分析**：共用 IP、设备、银行卡的账号群视为同一风险实体
4. **异常充值退款模式**：充值后快速退款再充值是常见洗钱信号，需标记

---

## 工程边界

- 竞拍出价必须原子操作：出价时先冻结买家货币，流拍后解冻，
  不能先出价再扣钱（超卖风险）
- 拍卖行搜索必须走只读副本或 Elasticsearch，不能直接打主库
- 手续费销毁记录要写到独立的 `currency_burn_log` 表，方便经济审计
- 拍卖行下架（Cancel）也要收挂单押金，否则玩家会用"挂单不卖"刷版扰乱市场

---

## 最短结论

拍卖行的本质是受控的货币回收器：手续费是 Sink，RMT 检测是护城河，
二者缺一都会让玩家经济变成黑产的提款机。
