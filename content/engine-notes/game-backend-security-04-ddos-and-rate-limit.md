---
title: "DDoS 防御与限流：Rate Limiting、IP 封禁、CDN 防护层设计"
slug: "game-backend-security-04-ddos-and-rate-limit"
date: "2026-04-04"
description: "游戏开服被打到宕机是真实发生的工程事故，防御体系应该从流量清洗、限流算法到降级响应多层构建，本文梳理完整的防御层次。"
tags:
  - "游戏后端"
  - "安全"
  - "DDoS"
  - "限流"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 33
weight: 3033
---

## 为什么游戏服务特别容易被打

DDoS（Distributed Denial of Service，分布式拒绝服务攻击）的目标是通过海量请求耗尽目标服务器的资源，使其无法响应合法请求。游戏服务相比普通 Web 服务，在 DDoS 防御上面临几个额外挑战：

**实时性要求高：** 游戏服务（特别是对战服务）对延迟极度敏感。普通 Web 服务可以在攻击期间降速服务，对用户只是"慢"。游戏服务一旦延迟超过阈值，玩家体验就完全崩溃，等同于不可用。

**攻击动机多样：** 除了常规的勒索攻击，游戏还面临竞争对手的恶意打压、被打败的玩家报复性攻击高排名对手，甚至职业代练团队通过攻击服务来制造混乱牟利。

**UDP 协议的特殊风险：** 大量游戏（特别是对战服务）使用 UDP 协议。UDP 无连接、无状态，每个包都是独立的，没有 TCP 三次握手作为天然的连接成本。这使得 UDP 反射攻击和 UDP 洪水攻击的成本极低——攻击者可以伪造源 IP，用极小的带宽消耗产生极大的攻击流量（放大系数）。

**开服流量峰值：** 游戏开服时，真实玩家流量本身就会产生巨大的合法峰值。防御系统需要能区分合法的流量洪峰和攻击流量，避免误伤真实玩家。

---

## DDoS 的两种主要类型

理解攻击类型，才能针对性地设计防御层次。

### 流量型攻击（Volumetric Attack）

目标是用海量流量塞满目标服务器的网络带宽，使合法流量无法到达。

典型手段：
- **UDP 洪水（UDP Flood）：** 向目标端口发送大量伪造的 UDP 数据包
- **DNS/NTP 反射放大（Amplification）：** 向第三方 DNS/NTP 服务器发送伪造源 IP（伪造为目标 IP）的请求，第三方服务器将大量响应数据发向目标，实现流量放大（NTP 的 monlist 命令放大系数可达 556 倍）

**防御重点：** 需要在靠近攻击源的位置（ISP 层、CDN 清洗中心）过滤流量，单靠应用服务器层无法处理，因为带宽在到达服务器之前就已经耗尽。

### 应用层攻击（Application Layer Attack / L7 Attack）

目标是模拟合法请求，耗尽服务器的计算资源（CPU、内存、数据库连接）。

典型手段：
- **HTTP 洪水（HTTP Flood）：** 大量合法格式的 HTTP 请求，每个请求消耗少量服务器资源，累积起来可以耗尽服务器处理能力
- **慢速攻击（Slowloris）：** 发起大量 TCP 连接但每次只发送极少量数据，长期占用服务器连接池
- **连接耗尽攻击：** 针对游戏大厅服务，建立大量 WebSocket 连接不断开，耗尽连接数限制

**防御重点：** 需要在应用层识别异常行为模式，结合频率限制、行为特征分析等手段过滤恶意请求。

---

## Rate Limiting：限流算法的选择

限流（Rate Limiting）是应对应用层攻击和接口滥用的核心手段。主要有两种算法：

### 令牌桶（Token Bucket）

**模型：** 有一个桶，以固定速率往桶里投放令牌。每次请求消耗一个令牌。桶满了新令牌溢出丢弃。桶为空时请求被拒绝（或排队等待）。

**特点：** 允许突发流量（桶内积累的令牌允许短时间内超过平均速率），但长期平均速率不超过令牌投放速率。适合游戏场景中的"正常玩家操作可能有瞬间高频（连击、快速翻背包），但不能持续高频"的需求。

```python
import time
import redis

def is_allowed_token_bucket(user_id: str, capacity: int, refill_rate: float) -> bool:
    """
    capacity: 桶的容量（最大令牌数）
    refill_rate: 每秒投放令牌数
    """
    r = redis.Redis()
    key = f"ratelimit:tb:{user_id}"
    now = time.time()
    
    pipe = r.pipeline()
    pipe.hgetall(key)
    result = pipe.execute()[0]
    
    if result:
        tokens = float(result[b'tokens'])
        last_refill = float(result[b'last_refill'])
        # 计算自上次以来应该补充的令牌数
        elapsed = now - last_refill
        tokens = min(capacity, tokens + elapsed * refill_rate)
    else:
        tokens = capacity
    
    last_refill = now
    
    if tokens >= 1:
        tokens -= 1
        r.hmset(key, {'tokens': tokens, 'last_refill': last_refill})
        r.expire(key, int(capacity / refill_rate) + 10)
        return True  # 允许
    
    r.hmset(key, {'tokens': tokens, 'last_refill': last_refill})
    return False  # 拒绝
```

### 漏桶（Leaky Bucket）

**模型：** 请求进入队列（桶），以固定速率从队列出去（漏出）处理。桶满时新请求被丢弃。

**特点：** 输出速率严格均匀，不允许任何突发。适合对下游系统（数据库、外部 API）保护的场景，保证下游不会被突发流量冲垮。

### Redis 滑动窗口计数器（简单高效）

对于大多数游戏场景，滑动窗口计数器是在工程复杂度和效果之间最好的平衡：

```python
def is_allowed_sliding_window(user_id: str, endpoint: str, 
                               limit: int, window_secs: int) -> bool:
    r = redis.Redis()
    key = f"ratelimit:{endpoint}:{user_id}"
    now = int(time.time() * 1000)  # 毫秒时间戳
    window_start = now - window_secs * 1000
    
    pipe = r.pipeline()
    pipe.zremrangebyscore(key, 0, window_start)  # 清除窗口外的记录
    pipe.zcard(key)  # 计算窗口内请求数
    pipe.zadd(key, {str(now): now})  # 记录本次请求
    pipe.expire(key, window_secs + 1)
    results = pipe.execute()
    
    current_count = results[1]
    return current_count < limit  # 未超限则允许
```

---

## 限流的维度设计

限流不是一个全局开关，需要按不同维度设计：

| 维度 | 说明 | 示例参数 |
|------|------|----------|
| IP 级别 | 单 IP 的请求频率 | /login 接口：每分钟 10 次 |
| 账号级别 | 单账号的接口调用频率 | /reward/claim：每小时 5 次 |
| 接口级别 | 单个接口的全局调用上限 | /matchmaking：全局每秒 1000 次 |
| 功能级别 | 特定业务操作的频率 | 发送聊天消息：每秒 2 条 |

**不同维度的优先级：** IP 限流是第一道防线，可以在不需要鉴权的情况下快速过滤（适用于登录接口被爆破的场景）。账号限流是业务层防线，需要鉴权通过后才能识别账号。

**参数设计原则：**
- 限流参数应该比正常玩家的最高合理操作频率宽松 3-5 倍（留出真实玩家的突发空间）
- 通过监控统计真实玩家的接口调用分布（P99），而不是拍脑袋定参数
- 限流参数应支持运行时热更新（通过远程配置），方便快速调整

---

## IP 封禁的自动化策略

单纯的限流对于持续性攻击来说还不够——即使每次都被限速，大量被限速的连接本身就会消耗服务器资源（解析请求、验证签名、返回 429 等）。对于已识别为恶意的 IP，需要在更早的层次（网络层）直接封禁。

**自动化封禁流程：**

```
触发条件（任一）：
  - 单 IP 在 1 分钟内触发限流超过 N 次
  - 单 IP 的登录失败率超过 80%（撞库特征）
  - 单 IP 在短时间内请求超过 M 个不同账号的数据

       ↓ 自动触发

临时封禁（自动，不需人工）：
  - 将 IP 加入 Redis 黑名单，TTL = 1 小时
  - 在 Nginx/网关层生效，直接返回 403 不进入业务层

       ↓ 上报告警

人工审核：
  - 安全人员审核封禁原因
  - 确认恶意攻击：延长为永久封禁 + 同段 IP 扩展封禁
  - 发现误封（如：某企业 NAT 出口 IP）：解封并记录白名单
```

**注意事项：** 自动封禁有误封风险，特别是在共享 IP 环境（企业 NAT、运营商 CGNAT）下。封禁时长不宜过长（初始自动封禁建议 1 小时，给误封的合法用户留出投诉机会），持续攻击的 IP 再升级为长期封禁。

---

## CDN 防护层：Anycast + 流量清洗

对于大规模流量型 DDoS，CDN 防护层是最有效的防御手段，因为它在流量到达源服务器之前就在骨干网层面过滤。

### Anycast 路由

现代 CDN 服务商（Cloudflare、AWS Shield、阿里云 DDoS 高防）使用 Anycast 路由：同一个 IP 地址在全球多个数据中心同时宣告。攻击流量被路由到离攻击源最近的清洗节点，不会汇聚到同一个点。

好处：即使一个清洗节点饱和，其他节点仍然正常工作。防御带宽是全球清洗节点的总和（通常达到数 Tbps），远超任何单一服务的能力。

### 流量清洗的工作原理

```
外网流量
    ↓
CDN 清洗中心（识别并过滤）
  ├── IP 信誉库过滤（已知攻击 IP）
  ├── 协议合规检查（畸形包过滤）
  ├── 流量特征识别（放大攻击特征、Bot 特征）
  └── Rate Limiting（第一层粗粒度限流）
    ↓
清洁流量 → 源服务器
```

**游戏 UDP 服务的挑战：** CDN 天然适合 HTTP/HTTPS 流量。对于游戏 UDP 对战服务，需要选择支持 UDP 流量清洗的 DDoS 防护服务（如阿里云游戏盾、腾讯云 GameShield），或者在架构上隔离 UDP 游戏服务，单独为其配置 IP 防护（而不是与 API 服务共享 IP）。

---

## 降级策略：限流后不要返回 500

这是一个常被忽视的工程细节：**限流触发后，返回什么状态码和响应体，直接影响客户端的行为。**

| 响应方式 | 客户端行为 | 影响 |
|----------|------------|------|
| 返回 500 | 客户端认为服务器崩溃，可能立即重试 | 加剧服务器压力 |
| 返回 503 | 客户端认为服务暂时不可用，可能立即重试 | 加剧服务器压力 |
| 返回 429（正确） | 客户端知道是限流，应该等待后重试 | 减少无效重试 |

标准的 429 响应应该包含 `Retry-After` 头，告诉客户端应该等待多久再重试：

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json

{
  "error": "rate_limit_exceeded",
  "message": "请求过于频繁，请 60 秒后重试",
  "retry_after": 60
}
```

**客户端的配合：** 游戏客户端在收到 429 后，应该使用指数退避（Exponential Backoff）策略重试，而不是立即重试或固定间隔重试，避免在限流解除后瞬间涌入大量重试请求再次触发限流。

---

## 工程边界

- **CDN 防护不是万能的：** CDN 通常只能防护 L3/L4 的流量型攻击和部分 L7 攻击。精心模拟合法业务请求的应用层攻击（如慢速 HTTP 洪水、精准模拟游戏操作的 Bot）难以用 CDN 过滤，需要在应用层结合业务逻辑识别。
- **限流参数是猜测和调整的过程：** 初期参数往往不准确，需要通过监控数据持续调整。关键是要有快速修改限流参数的能力（热配置）。
- **防御有成本：** DDoS 防护服务（特别是高防 IP）价格不低，需要根据游戏的商业价值和攻击风险评估是否投入，以及投入多大规模。

---

## 最短结论

游戏服务的 DDoS 防御是分层体系：CDN 清洗处理流量型攻击，限流算法处理应用层滥用，IP 封禁减少恶意连接消耗，降级响应（429 + Retry-After）让系统在攻击期间能优雅降级而不是崩溃。

**单层防御都有绕过的可能，多层防御的目标是让攻击成本高于收益**——当攻击者发现你的服务有 CDN 防护 + 限流 + 自动封禁，攻击成本急剧上升，他们通常会转向更容易的目标。
