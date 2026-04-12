---
title: "集群运维基础：健康检查、滚动更新、灰度发布、多机房部署"
slug: "game-backend-micro-07-cluster-operations"
date: "2026-04-04"
description: "游戏版本更新时，如何在不踢掉在线玩家的前提下完成服务器升级？深度解析健康检查、滚动更新、灰度发布与多机房部署的工程实践。"
tags:
  - "游戏后端"
  - "运维"
  - "Kubernetes"
  - "滚动更新"
  - "灰度发布"
  - "多机房"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 20
weight: 3020
---

## 问题的本质：有状态的升级

Web 服务的无状态特性让它的升级变得简单：旧实例下线，新实例上线，中间的请求由其他实例处理，用户感知不到切换。

游戏后端的麻烦在于**有状态**。玩家正在进行的战斗、正在处理的交易、维持的 WebSocket 长连接——这些状态和会话都绑定在特定的服务实例上。你不能简单地关掉一个进程，里面的 500 个在线玩家会直接被踢下线，回到登录界面，这在玩家体验上是灾难性的。

这就是游戏后端运维的核心难题：**如何在不中断在线玩家体验的前提下，完成服务器升级？**

理解这个问题，需要从健康检查开始说起。

---

## 健康检查的三个层次

很多团队把健康检查等同于"进程还在跑"，这是一个危险的简化。游戏后端的健康检查应该分三个层次，每个层次回答不同的问题。

### 层次一：进程存活（Liveness）

**问题**：进程还在跑吗？

进程存活检查的目的是发现"僵死状态"——进程还在，但已经陷入死锁或无限循环，无法处理任何请求。这种情况下，重启进程是正确的响应。

在 K8s 里，Liveness Probe 失败会触发 Pod 重启。

```yaml
livenessProbe:
  httpGet:
    path: /healthz/live
    port: 8080
  initialDelaySeconds: 30   # 给服务足够的启动时间
  periodSeconds: 10
  failureThreshold: 3       # 连续 3 次失败才重启，避免抖动
```

**注意**：Liveness Probe 的检查逻辑必须极其简单，不能依赖外部服务（数据库、缓存）。如果 Liveness Probe 因为 Redis 暂时不可达而失败，会触发大批 Pod 重启，反而雪崩。

### 层次二：服务就绪（Readiness）

**问题**：服务已经准备好接受流量了吗？

就绪检查解决两个问题：

1. **启动阶段**：服务刚启动，正在加载配置、预热缓存、建立数据库连接池，这时候不应该接收用户流量。
2. **运行阶段**：服务因为某些原因（如下游服务不可用导致请求积压）暂时不能正常处理请求，应该从负载均衡中暂时摘除。

K8s 里，Readiness Probe 失败不会重启 Pod，而是将 Pod 从 Service 的 Endpoints 列表中移除，流量停止转发过去，等恢复后重新加入。

```yaml
readinessProbe:
  httpGet:
    path: /healthz/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
  failureThreshold: 2
```

`/healthz/ready` 的检查逻辑可以包含下游依赖的状态：数据库连接是否健康、消息队列是否可达、内部队列积压是否超阈值。

### 层次三：业务健康（Business Health）

**问题**：服务在正确地处理游戏业务吗？

这是最容易被忽视的层次。进程活着、依赖都通、但业务逻辑出了 Bug，所有战斗结算返回错误——这种情况在前两个层次的检查中都是"健康"的。

业务健康检查通常不接入 K8s 的探针机制，而是通过以下方式实现：
- **业务指标监控**：监控成功率、P99 延迟、错误率，设置告警阈值
- **Canary 指标**：灰度发布时，对比新旧版本的业务指标差异
- **端到端心跳**：定时执行一个完整的模拟业务流程（如模拟登录 → 查询背包 → 退出），验证整个链路是否正常

---

## 滚动更新：逐步替换，而不是全量重启

滚动更新（Rolling Update）的思路是：**不要同时关掉所有旧实例，而是逐步用新实例替换旧实例，确保在任何时刻都有足够数量的实例在提供服务。**

### K8s Deployment 滚动更新机制

```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  replicas: 10
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 2        # 最多允许超出期望副本数 2 个（更新期间最多 12 个 Pod）
      maxUnavailable: 1  # 最多允许 1 个 Pod 不可用（更新期间最少 9 个健康 Pod）
```

更新流程：
1. 启动 2 个新版本 Pod（maxSurge=2），等待其 Readiness Probe 通过
2. 终止 1 个旧版本 Pod（maxUnavailable=1）
3. 重复，直到所有旧版本 Pod 被替换

**在终止旧 Pod 之前，K8s 会先将其从 Endpoints 中摘除，等待 `terminationGracePeriodSeconds` 之后再发送 SIGTERM。** 这个间隔给了负载均衡层足够的时间将新请求路由到其他实例。

### 有状态服务滚动更新的难点

无状态服务滚动更新是"随时可以关"，有状态服务则需要考虑**连接迁移**和**状态排空**。

**WebSocket 长连接**：网关层收到 SIGTERM 后，不能立刻关闭所有 WebSocket 连接。正确做法是：
1. 停止接受新连接
2. 向所有当前连接的客户端发送"即将维护，请重连到其他节点"的信令
3. 等待客户端自行重连（或超时后强制关闭）
4. 待所有连接都迁移完成后，进程退出

```go
func (s *GatewayServer) GracefulShutdown(ctx context.Context) {
    // 停止接受新连接
    s.listener.Close()
    
    // 通知所有在线客户端迁移
    s.broadcastMaintenance()
    
    // 等待连接自然排空，或超时
    select {
    case <-s.allConnectionsDrained():
        log.Info("all connections drained gracefully")
    case <-ctx.Done():
        log.Warn("shutdown timeout, forcing close of remaining connections", 
            "remaining", s.connectionCount())
    }
}
```

**战斗服务**：正在进行的战局绑定在特定实例上。滚动更新前，需要确保该实例不再接受新战局分配（将实例标记为 Draining 状态），等待现有战局自然结束后再重启。这通常需要在业务层实现，而不是依赖 K8s 的默认机制。

---

## PodDisruptionBudget：保障升级期间的最低服务能力

K8s 的 `PodDisruptionBudget（PDB）`是保障滚动更新期间不影响在线玩家的关键机制。

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: battle-service-pdb
spec:
  minAvailable: 8    # 任何时候至少保证 8 个健康 Pod
  selector:
    matchLabels:
      app: battle-service
```

PDB 告诉 K8s：**在进行任何"自愿中断"（如节点维护、滚动更新）时，必须保证至少 8 个 Pod 处于可用状态，不能低于这个数量。** K8s 的 Eviction API 和滚动更新控制器都会遵守 PDB 约束。

对于游戏后端，PDB 的 `minAvailable` 应该根据峰值负载计算：
- 当前副本数 = 10
- 峰值时每个 Pod 处理约 200 个并发连接
- 服务器 SSO 活动峰值预计 1500 个并发连接
- 最低需要 8 个 Pod，设置 `minAvailable: 8`

---

## 灰度发布：金丝雀先飞

全量滚动更新有一个风险：如果新版本有 Bug，等你发现时可能已经影响了大量玩家。**灰度发布（Canary Release）** 的思路是先让一小部分流量打到新版本，验证无误后再全量推进。

### 流量切分策略

**基于比例的随机切分**：10% 流量到新版本，90% 流量到旧版本。简单，但无法控制哪些玩家进入灰度。

**基于用户属性的切分**：只让特定玩家（如内部测试账号、某个区服的玩家、VIP 等级低的玩家）使用新版本。需要在网关层或流量染色层实现。

```yaml
# Istio VirtualService 示例：5% 流量打到金丝雀版本
apiVersion: networking.istio.io/v1alpha3
kind: VirtualService
spec:
  http:
  - match:
    - headers:
        x-canary-user:
          exact: "true"
    route:
    - destination:
        host: battle-service
        subset: canary
  - route:
    - destination:
        host: battle-service
        subset: stable
      weight: 95
    - destination:
        host: battle-service
        subset: canary
      weight: 5
```

### 灰度期间的观测指标

灰度不只是切流量，关键是**观测**。需要对比新旧版本的：
- 错误率（5xx 比例）
- P99/P999 延迟
- 业务成功率（如战斗结算成功率、购买成功率）
- 内存/CPU 使用趋势

这些指标如果新版本显著差于旧版本，应立即回滚。工具可以是 Prometheus + Grafana，关键是要在发布之前就设定好"回滚阈值"，而不是靠人眼判断。

---

## 多机房部署：数据一致性是最大挑战

为了应对单机房故障和降低玩家延迟，游戏后端通常会部署在多个机房（或多个云区域）。这带来了全新的复杂度。

### 跨机房读写策略

**就近读写**：玩家的请求路由到最近的机房，读写都在本机房完成。延迟最低，但机房之间的数据不是实时同步的。

这对于游戏的大多数数据是可以接受的：玩家 A 在北京机房的购买记录，不需要实时同步到上海机房——因为玩家 A 不会同时登录两个机房。

**写主读从（Read-Your-Writes 保障）**：所有写操作发送到主机房（或该玩家的 Home 机房），读操作可以在就近机房，但保证读到自己刚写入的数据。适合货币、背包等对一致性敏感的数据。

### 机房间延迟的影响

跨机房网络延迟通常在 10-100ms，同机房内部通常在 1ms 以下。这意味着：

- 跨机房的同步调用会显著增加请求延迟，应尽量避免在关键路径上跨机房同步调用
- 数据库主从复制是异步的，从库可能落后主库几百毫秒到几秒
- 分布式锁如果跨机房，代价极高

**实践建议**：将玩家的会话（Session）强绑定到一个机房（Home 机房），玩家的关键数据写操作只在 Home 机房进行，排行榜、公告等全局数据通过异步同步在机房间传播。

### 机房故障切换

当某个机房出现故障时，需要将该机房的玩家流量切换到其他机房。游戏场景的切换比普通 Web 服务更复杂：

1. **会话迁移**：玩家需要重新登录（接受），但背包数据和货币数据必须正确（不可接受丢失）
2. **数据一致性**：切换前需要确认数据已同步到备用机房，或接受短暂的数据不一致
3. **切换时机**：在玩家体验最低峰（如凌晨维护时间）切换，避免高峰期切换带来连锁故障

---

## 游戏开服与活动期间的运维预案

开服和大型活动是游戏后端最脆弱的时刻，流量往往在短时间内数倍于日常峰值。

### 预热与预扩容

在活动开始前 30 分钟，提前将副本数扩展到预期峰值所需的数量。不要依赖 HPA（Horizontal Pod Autoscaler）的自动扩容：HPA 的反应速度（通常 1-2 分钟一个周期）在活动开始的瞬时流量冲击下太慢。

### 限流与熔断的分级配置

活动期间，某些非核心服务（如排行榜刷新、成就统计）应主动降级或限流，把计算资源集中给核心链路（登录、战斗、道具）。在服务启动配置中预设"活动模式"的参数，一键切换。

### 只读模式

极端情况下（如数据库主节点故障），可以切换到只读模式：玩家可以查看背包、查看排行榜，但无法进行消耗货币的操作。这比全服宕机的影响小得多，也给后端争取了恢复时间。

---

## 最短结论

游戏服务器升级不踢人的关键在于**三层健康检查 + 优雅退出 + PDB**：Readiness Probe 保证新版本就绪后才接流量，SIGTERM 信号触发服务主动排空连接，PodDisruptionBudget 约束 K8s 不能同时终止过多实例。灰度发布是新版本上线的保险绳，多机房部署是单点故障的最后防线。

运维能力是游戏后端工程能力的天花板。代码写得再好，上线一次宕机就能把玩家口碑打垮。从第一天起就把健康检查、优雅退出、回滚流程设计进系统，比事后救火便宜得多。
