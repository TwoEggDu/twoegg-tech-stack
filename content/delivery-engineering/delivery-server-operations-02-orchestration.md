---
title: "服务端部署与运维 02｜容器编排——Docker Compose、K8s 与游戏服务的特殊需求"
slug: "delivery-server-operations-02-orchestration"
date: "2026-04-14"
description: "容器编排解决的是'怎么管理一堆容器'的问题。游戏服务端的有状态、长连接、低延迟特性让编排选型比 Web 后端复杂得多。"
tags:
  - "Delivery Engineering"
  - "Server"
  - "Kubernetes"
  - "Docker"
  - "DevOps"
series: "服务端部署与运维"
primary_series: "delivery-server-operations"
series_role: "article"
series_order: 20
weight: 1020
delivery_layer: "principle"
delivery_volume: "V11"
delivery_reading_lines:
  - "L1"
  - "L3"
---

## 这篇解决什么问题

上一篇讲了部署策略。但一台一台手动部署效率太低——需要编排工具来管理容器的启动、调度、健康维护和自动恢复。本篇对比三种编排方案，重点讲游戏服务端在 K8s 上的特殊需求。

## 三种编排方案对比

| 维度 | Docker Compose | Kubernetes | 自建编排 |
|------|---------------|------------|---------|
| 复杂度 | 低 | 高 | 视实现而定 |
| 适用规模 | 单机 / 小团队 | 中大规模集群 | 有特殊需求的大团队 |
| 学习成本 | 低 | 高 | 高 |
| 自动扩缩容 | 不支持 | 原生支持 | 需自建 |
| 服务发现 | Docker DNS | CoreDNS + Service | 需自建 |
| 有状态支持 | 手动管理 | StatefulSet | 可定制 |
| 社区生态 | 小 | 极大 | 无 |

### Docker Compose：小规模首选

适用场景：10 台以下服务器、开发/测试环境、小团队。

优点是配置简单——一个 `docker-compose.yml` 描述所有服务的依赖和启动顺序。缺点是不支持跨机器调度和自动扩缩容。

### Kubernetes：生产级选择

K8s 是容器编排的事实标准。它解决的核心问题是：**声明式地描述你想要的状态，系统自动维护这个状态**。

但 K8s 的复杂度不低。如果团队没有专职运维，且服务器数量不多，K8s 可能是过度工程化。

### 自建编排：特殊需求

部分大型游戏公司会自建编排系统——原因通常是 K8s 的抽象不完全匹配游戏服务端的需求（如自定义的房间分配逻辑、跨区服调度）。除非有明确的技术需求，否则不推荐。

## 游戏服务端在 K8s 上的特殊需求

### StatefulSet vs Deployment

| 维度 | Deployment | StatefulSet |
|------|-----------|-------------|
| Pod 标识 | 随机名称 | 稳定的有序名称（pod-0, pod-1） |
| 存储 | 共享 / 无状态 | 每个 Pod 独立 PVC |
| 启停顺序 | 无保证 | 有序启动、逆序停止 |
| 网络标识 | 不稳定 | 稳定的 DNS 名称 |
| 适合 | 无状态服务（登录、匹配） | 有状态服务（游戏世界服务器） |

游戏世界服务器通常需要 StatefulSet——每个实例有固定标识，便于玩家重连和运维定位。

### 网络需求

游戏服务端对网络延迟敏感。K8s 默认的网络模型（Pod → Service → Pod）会引入额外延迟：

| 网络模式 | 延迟 | 适用 |
|---------|------|------|
| ClusterIP Service | 有额外跳转 | 内部微服务通信 |
| NodePort | 较低 | 需要外部访问的服务 |
| Host Network | 最低 | 对延迟极其敏感的游戏服务器 |
| Headless Service | 直连 Pod | StatefulSet 配合使用 |

对实时性要求高的游戏服务器（如 FPS/MOBA 的战斗服务器），通常使用 Host Network 直接暴露宿主机端口，避免网络层的额外开销。

### UDP 支持

K8s Service 默认面向 TCP。游戏服务端常用 UDP（如 KCP/ENET）。需要注意：

- Service 需要显式声明 `protocol: UDP`
- 负载均衡器需要支持 UDP（不是所有云厂商的 LB 都支持）
- 健康检查不能用 TCP 探针——需要自定义 HTTP 健康检查端点

## 何时 K8s 是过度工程化

满足以下条件时，Docker Compose 或简单的脚本部署可能更合适：

- 服务器数量 < 10 台
- 团队没有 K8s 运维经验
- 游戏处于开发早期，架构频繁变动
- 不需要自动扩缩容（如买断制游戏的固定服务器）

**务实建议**：先用 Docker Compose 跑通，当规模增长到需要跨机器调度和自动扩缩容时再迁移到 K8s。过早引入 K8s 的团队往往花大量时间在运维工具上，而不是游戏本身。

## 健康检查设计

K8s 提供三种探针，游戏服务端都需要：

| 探针 | 回答的问题 | 游戏服务端示例 |
|------|-----------|---------------|
| Liveness | 进程是否还活着？ | 主线程是否在正常 tick |
| Readiness | 是否可以接受新玩家？ | 初始化是否完成、是否已加载地图数据 |
| Startup | 首次启动是否完成？ | 游戏服务器启动可能需要 30 秒以上加载数据 |

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  periodSeconds: 5

startupProbe:
  httpGet:
    path: /health/startup
    port: 8080
  failureThreshold: 30
  periodSeconds: 2
```

Startup Probe 给游戏服务器足够的启动时间（上例最多等 60 秒）。启动完成前，Liveness 和 Readiness 不会生效——避免启动慢的服务器被误杀。

## 资源限制

```yaml
resources:
  requests:
    cpu: "2"
    memory: "4Gi"
  limits:
    cpu: "4"
    memory: "8Gi"
```

| 配置 | 含义 | 建议 |
|------|------|------|
| requests | 调度保证的最低资源 | 根据平均负载设置 |
| limits | 资源上限（超出会被限流或 OOM Kill） | 根据峰值负载设置，留余量 |

游戏服务器的内存使用通常随在线玩家数线性增长——limits 需要按满载人数设置，否则高峰期会被 OOM Kill。

## 小结

- [ ] 是否根据团队规模和服务器数量选择了合适的编排方案
- [ ] 有状态游戏服务器是否使用 StatefulSet（而非 Deployment）
- [ ] 对延迟敏感的服务是否评估了 Host Network
- [ ] 三种健康检查探针是否都已配置
- [ ] 资源 limits 是否按满载人数设置

---

**下一篇**：[监控体系]({{< relref "delivery-engineering/delivery-server-operations-03-monitoring.md" >}}) — 日志、指标、追踪与告警
