---
title: "内购与支付系统深度：IAP 收据验证、礼包设计原则、防刷单与补单机制"
slug: "game-backend-economy-04-iap-payment"
date: "2026-04-05"
description: "Apple IAP 和 Google Play 收据验证的完整流程、礼包定价的锚定效应、刷单防护技术，以及支付失败未发货的补单流程设计。"
tags:
  - "游戏后端"
  - "游戏经济"
  - "内购"
  - "支付系统"
  - "IAP"
  - "防刷单"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 54
weight: 3054
---

## 问题空间

某款手游上线第一周，客服收到大量投诉：付了钱但没收到钻石。与此同时，风控部门发现
有人用修改器绕过客户端，批量伪造购买成功回调，刷走了价值数十万的游戏道具。

两件事的根源是同一个：**没有做服务端收据验证**。开发者相信了客户端传来的"支付成功"，
而客户端是可以被任意篡改的。

内购系统的工程核心是：**永远不相信客户端，永远以平台验证结果为准**。

---

## 抽象模型

### IAP 支付的完整链路

```
用户点击购买
    ↓
客户端发起 StoreKit（iOS）/ Google Play Billing（Android）请求
    ↓
平台处理扣款
    ↓
平台返回收据（Receipt / Purchase Token）给客户端
    ↓
客户端将收据上报给游戏服务端           ← 不可信区域
    ↓
游戏服务端向平台 API 验证收据          ← 安全区域
    ↓
验证通过 → 发放道具 + 写订单记录
验证失败 → 拒绝，记录日志
```

关键洞察：步骤 4-5 之间的边界是安全边界。收据在客户端手中，
可以被复制、伪造、重放——**服务端必须独立向 Apple / Google 确认这笔支付真实存在**。

---

## 具体实现

### Apple IAP 收据验证（StoreKit 2 之前）

```python
import httpx

APPLE_VERIFY_URL_PROD    = "https://buy.itunes.apple.com/verifyReceipt"
APPLE_VERIFY_URL_SANDBOX = "https://sandbox.itunes.apple.com/verifyReceipt"

async def verify_apple_receipt(receipt_data: str, shared_secret: str) -> dict:
    payload = {
        "receipt-data": receipt_data,
        "password": shared_secret,
        "exclude-old-transactions": True
    }
    # 先打生产环境，若返回 21007 则是沙盒收据，切到沙盒验证
    async with httpx.AsyncClient() as client:
        resp = await client.post(APPLE_VERIFY_URL_PROD, json=payload, timeout=10)
        data = resp.json()

    if data["status"] == 21007:
        async with httpx.AsyncClient() as client:
            resp = await client.post(APPLE_VERIFY_URL_SANDBOX, json=payload, timeout=10)
            data = resp.json()

    return data  # status=0 表示有效

# StoreKit 2（iOS 15+）改用 App Store Server API，用 JWT 签名验证，更现代
```

验证通过后，从响应中提取关键字段：
- `transaction_id`：唯一交易ID，作为幂等键
- `product_id`：购买的商品ID，与服务端商品表对照
- `purchase_date_ms`：购买时间，检查是否过期（重放攻击防护）
- `bundle_id`：确认是本游戏的收据，防止跨应用收据复用

### Google Play 收据验证

Google Play 返回的是 `purchaseToken`，需要调用 Google Play Developer API：

```python
from google.oauth2 import service_account
from googleapiclient.discovery import build

def verify_google_purchase(package_name: str, product_id: str, purchase_token: str):
    credentials = service_account.Credentials.from_service_account_file(
        'service_account.json',
        scopes=['https://www.googleapis.com/auth/androidpublisher']
    )
    service = build('androidpublisher', 'v3', credentials=credentials)

    result = service.purchases().products().get(
        packageName=package_name,
        productId=product_id,
        token=purchase_token
    ).execute()

    # purchaseState: 0=已购买, 1=已取消, 2=待定
    # consumptionState: 0=未消耗, 1=已消耗
    # orderId: 唯一订单ID，作为幂等键
    return result
```

验证后立即调用 `consume`（消耗型商品）或标记已确认，
否则 Google 会在 3 天内自动退款。

### 服务端订单表设计

```sql
CREATE TABLE iap_order (
    id               BIGSERIAL    PRIMARY KEY,
    player_id        BIGINT       NOT NULL,
    platform         VARCHAR(8)   NOT NULL,  -- 'apple' | 'google' | 'steam'
    product_id       VARCHAR(64)  NOT NULL,
    platform_order_id VARCHAR(128) NOT NULL, -- transaction_id / orderId
    receipt_data     TEXT,                   -- 原始收据，用于二次核查
    amount_cents     INT          NOT NULL,  -- 法币金额（分），来自商品定价表
    currency         VARCHAR(3)   NOT NULL,  -- 'USD' | 'CNY'
    verify_status    VARCHAR(16)  NOT NULL DEFAULT 'pending',
    -- 'pending' | 'verified' | 'failed' | 'refunded'
    grant_status     VARCHAR(16)  NOT NULL DEFAULT 'pending',
    -- 'pending' | 'granted' | 'failed'
    verified_at      TIMESTAMPTZ,
    granted_at       TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (platform, platform_order_id)  -- 幂等：同一笔平台订单只处理一次
);
```

`platform_order_id` 的唯一约束是防刷的第一道门：
同一个 `transaction_id` 第二次上报会被 UNIQUE 冲突拦住。

---

## 礼包设计原则：锚定效应

为什么 30 元的礼包旁边总有一个 98 元的？

**锚定效应（Anchoring Effect）**：玩家对价值的判断受到第一眼看到的参照物影响。
当 98 元礼包是"锚"时，30 元礼包显得极具性价比，转化率显著提升。

```
定价结构示例：
  ┌─────────────────────────────────────────┐
  │ 豪华礼包  ¥98   ← 锚定价（高价参照）   │
  │  ├── 钻石 × 3000                        │
  │  ├── 史诗皮肤 × 1                       │
  │  └── 专属头像框                         │
  │                                         │
  │ 超值礼包  ¥30   ← 目标转化礼包         │
  │  ├── 钻石 × 1200（"单价更低！"）        │
  │  └── 稀有材料 × 10                      │
  │                                         │
  │ 新手礼包  ¥6    ← 低门槛首充引导        │
  │  └── 钻石 × 300 + 体验道具              │
  └─────────────────────────────────────────┘
```

其他常用心理学原理：
- **损失厌恶**：限时礼包倒计时，强调"即将消失"而非"马上拥有"
- **首充福利**：首次充值额外奖励 × 3，降低首付壁垒
- **月卡订阅**：低日均成本感知（"每天只要 X 分钱"），提高 LTV

---

## 刷单防护

### 常见刷单手段

| 手段 | 原理 |
|------|------|
| 收据重放 | 同一张有效收据提交多次 |
| 沙盒收据滥用 | 提交沙盒环境收据到生产服务器 |
| 越狱修改器 | 拦截本地 IAP 流程，伪造成功回调 |
| 退款循环 | 付款 → 收到道具 → 向 Apple/Google 申请退款 |

### 防护措施

**1. 服务端独立验证（已述）**：客户端传的任何"支付结果"都不可信。

**2. 沙盒隔离**：生产服务器只接受生产收据。如果收到沙盒收据（`status=21007`），
记录日志但不发货（可能是测试人员误操作，也可能是攻击）。

**3. 时效性检查**：收据发行时间与上报时间差 > 30 分钟的，升级审核。
正常用户买完会立刻上报；攻击者可能囤积收据批量提交。

**4. 设备与账号绑定**：同一笔订单只能被同一设备-账号组合核销。
换设备要重新验证，且有次数限制。

**5. 退款监控**：Apple/Google 提供服务端退款通知（Server Notifications）。
收到退款通知后，立即回收已发放道具或封禁账号。

```python
# Apple Server Notifications v2（JWS 格式）
# 需要在 App Store Connect 配置回调 URL
def handle_apple_notification(jws_payload: str):
    # 解码并验证签名
    decoded = decode_jws(jws_payload, apple_root_certs)
    notification_type = decoded["notificationType"]

    if notification_type == "REFUND":
        transaction_id = decoded["data"]["signedTransactionInfo"]["transactionId"]
        revoke_grant(transaction_id)  # 回收道具或封号
    elif notification_type == "CONSUMPTION_REQUEST":
        # 消耗型商品投诉，可选择退款
        pass
```

---

## 补单机制：支付成功但未发货

这是客服投诉最多的场景。原因通常是：

1. 服务端验证成功，但发货服务崩溃
2. 发货服务超时，客户端提前断开
3. 数据库写入失败（事务回滚），但平台已扣款

补单流程设计：

```
iap_order.verify_status = 'verified'
iap_order.grant_status  = 'pending'    ← 这批订单需要补发

定时任务（每 5 分钟）：
  SELECT * FROM iap_order
  WHERE verify_status = 'verified'
    AND grant_status  = 'pending'
    AND created_at    < NOW() - INTERVAL '2 minutes'  -- 给首次发货留出时间
  LIMIT 100;

  FOR each order:
    try:
      grant_items(order)               -- 发货（幂等：以 platform_order_id 为key）
      UPDATE iap_order SET grant_status='granted' WHERE id=order.id
    except:
      UPDATE iap_order SET grant_status='failed'  -- 告警人工介入
```

补单任务的幂等性：发货操作使用 `platform_order_id` 作为幂等键，
无论重试多少次，玩家只会收到一份货。

**玩家主动申请补单**：客服后台提供"核查订单"功能，输入订单号，
系统自动拉取平台验证结果，若已验证未发货则立即补发，全程留审计日志。

---

## 工程边界

- Apple / Google 的验证接口有 QPS 限制，批量补单时必须加限速（令牌桶）
- 不要在客户端存储共享密钥（`shared_secret`），这是服务端专属
- 订单表的 `receipt_data` 字段加密存储（含用户支付信息，合规要求）
- 退款回调要配置白名单IP，防止伪造退款通知触发批量回收
- 上架商品时服务端要维护商品定价表，验证收据中的 `product_id` 是否合法，
  防止玩家提交旧版本的廉价商品收据购买新版本高价值内容

---

## 最短结论

IAP 系统的安全底线是服务端验证收据，工程可靠性底线是补单任务——
前者防黑产，后者保玩家，缺一个都会让你在苹果税之外再额外损失一笔。
