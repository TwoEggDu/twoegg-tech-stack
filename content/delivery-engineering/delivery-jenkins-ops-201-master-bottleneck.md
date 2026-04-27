---
date: "2026-04-27"
title: "Master 的三类瓶颈：连接、内存、磁盘 I/O"
description: 'Jenkins Master 在游戏团队这种"大作业 + 大产物 + 多分支"场景下容易崩，根因不是单点机器配置，是三类瓶颈的耦合：连接数、堆内存、磁盘 I/O。本篇拆解每一类的诊断信号、容量基线和扩容信号。'
slug: "delivery-jenkins-ops-201-master-bottleneck"
weight: 1577
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Master"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 70
delivery_layer: "principle"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
leader_pick: true
---

## 在本篇你会读到

- **Master 是什么、不是什么** —— 它实际承担的工作和常见误解
- **瓶颈 1：连接数** —— Agent 通道 + Web 请求的双重压力
- **瓶颈 2：内存** —— 堆 + 元空间 + Pipeline state 的耦合
- **瓶颈 3：磁盘 I/O** —— build.xml 写入是隐藏杀手
- **三类瓶颈的耦合反馈环** —— 单维度优化为什么不够
- **容量规划基线** —— 给定团队规模的 Master 配置参考
- **扩容信号** —— 什么时候必须分布式 Master 或迁 Master 上更大机器

---

## Master 是什么、不是什么

### 它实际做什么

Jenkins 架构里，Master 不是"带头干活的 Agent"，而是**调度中心 + 状态机 + Web 服务**的组合：

- 接收 webhook / API 触发
- 解析 Jenkinsfile，生成 Pipeline state
- 把 stage 分发到 Agent 执行
- 收集 Agent 返回的日志和产物
- 把结果写入 build history（磁盘）
- 渲染 Web UI 给所有用户

**Master 自己默认不跑 build job**——配置上可以，但生产环境绝对不要这么做。

### 常见误解

| 误解 | 实际 |
|-----|------|
| "Master 挂了大不了重启" | Master 重启会丢正在跑的 Pipeline 状态（除非用 Durable Task） |
| "加 CPU 就能解决性能问题" | Master 的瓶颈通常是 I/O 和内存，不是 CPU |
| "Master 不跑 build 就没什么压力" | 调度本身的压力被严重低估，特别是大量并发 Pipeline |
| "用云服务托管的 Jenkins 就没事" | 托管服务内部仍然是 Master 架构，瓶颈一样存在 |

### 为什么游戏团队的 Master 容易崩

通用 CI 团队的 Master 压力小，因为：

- 单 build 时长短（分钟级）→ Pipeline state 在 Master 上停留时间短
- 产物小 → Master 处理 artifact metadata 压力低
- 分支少 → MBP 扫描和 webhook 处理压力低

游戏团队全部反过来：

- 单 build 时长 1-4 小时 → Pipeline state 在 Master 上**长期占用**
- 产物 GB 级 → Master 处理 artifact metadata、index 压力高
- 多产品 × 多分支 → 几百条 Pipeline 同时存在

这是为什么"通用 Jenkins 经验在 Master 容量规划上不适用"。

---

## 瓶颈 1：连接数

Master 维护两类连接：

- **Agent 通道**（JNLP / SSH / WebSocket）
- **Web 请求**（用户浏览 + API 调用 + webhook）

每条都有上限，达到上限后表现是"Master 看起来正常但响应卡顿"。

### Agent 通道

每个在线 Agent 占用一条到 Master 的持久连接。Master 需要：

- 保持心跳（每几秒一次）
- 发送命令（启动 build、传输 step）
- 接收日志（实时流式回传）

**容量参考**：典型配置下，单 Master 能稳定撑住 50-100 个 Agent。超过 200 就要警觉，超过 500 必须分布式。

### Web 请求

更隐蔽。每个用户浏览 Jenkins UI 都在持续请求：

- 实时构建状态（轮询）
- Build history 列表
- Pipeline graph 渲染

游戏团队多产品 + 多分支场景下，业务方一打开 Jenkins 首页，加载几百条 Pipeline 状态——单页可能触发数百次 API 调用。

### 诊断信号

**信号 1：UI 卡顿但 Agent 正常**
打开首页要 30 秒以上加载，但 Agent 上的 build 跑得正常。这是 Web 请求瓶颈。

**信号 2：Agent 频繁掉线再上线**
日志里 "Agent X disconnected" / "Agent X reconnected" 高频出现。这是 Agent 通道压力。

**信号 3：Webhook 延迟**
Git push 后 10 分钟才触发构建（不是 MBP 扫描问题）。这是 webhook 处理排队。

### 失效模式

**典型故障：连接数耗尽**
- Master 的 file descriptor 上限被 hit（Linux 默认 1024）
- 新 Agent 无法连接，已连的 Agent 断线后无法重连
- 救火方法：临时调大 ulimit，长期方案是减少 Agent 数 + 升级 Master 机器

**真实场景**：
> 某团队为了"加快构建"加了 30 台 Agent，加到第 50 台时 Master 开始反复掉线。诊断后发现是 file descriptor 限制，调大后又遇到内存上限——根因是 Agent 通道占用大量堆内存（见瓶颈 2）。

---

## 瓶颈 2：内存（JVM 堆 + 元空间）

Jenkins 是 Java 进程，内存压力来自三个层次：

### 层次 1：JVM 堆（Heap）

存放：

- 当前正在跑的 Pipeline 状态（每条 Pipeline 占几 MB-几十 MB）
- Build history 缓存
- 用户会话和权限信息

**单条游戏团队 Pipeline 的堆占用**：通常 50-200 MB（含 Groovy CPS state 和工件 metadata）。同时跑 50 条 Pipeline = 5-10 GB 堆压力。

**典型配置基线**：

- 小团队（<10 Agent）：4 GB 堆
- 中团队（10-50 Agent）：8-16 GB 堆
- 大团队（50+ Agent）：32 GB+ 堆

### 层次 2：JVM 元空间（Metaspace）

存放类元数据。Jenkins 的特殊性：

- 每条 Pipeline 都会编译 Groovy 脚本生成新类
- 长期不重启 Master 会让元空间膨胀
- 元空间默认无上限（OOM 来源之一）

**常见错误**：只配 `-Xmx16g`（堆上限）但忘了 `-XX:MaxMetaspaceSize`，导致元空间无限增长，最终 RSS 爆。

**正确配置**：

```bash
-Xmx16g
-XX:MaxMetaspaceSize=2g
-XX:+UseG1GC
```

### 层次 3：Pipeline state 持久化

Jenkins 把每条 Pipeline 的状态序列化到磁盘（Durable Task），但**反序列化回内存**时压力可能很大。一条跑了 4 小时的 Pipeline 重启 Jenkins 后恢复，可能要分配几百 MB 临时内存。

### 诊断信号

**信号 1：长期不重启后内存持续上涨**
监控 Master JVM 的 RSS，30 天内涨了 3 GB——是元空间或 leak。

**信号 2：GC 频率上升**
G1 Mixed GC 每 10 秒触发一次（应该是分钟级）。堆压力。

**信号 3：随机 OOM 杀进程**
没有具体错误日志，进程突然消失。看 dmesg 里 "Out of memory" 信息。

### 失效模式

**典型故障：Master 在凌晨 4 点 OOM**
- 凌晨是夜间 build 高峰（多产品定时 build）
- 同时跑的 Pipeline state 占用堆
- 元空间随每条 Pipeline 涨一点
- 最终一条新 Pipeline 触发 OOM
- 整个 Jenkins 挂掉，运行中的 build 全部失败

**真实场景**：
> 我们的 Master 周三早上发现挂了，看监控是凌晨 4:23 OOM。问题不在那一刻——问题在于 30 天没重启过，元空间从启动时 200 MB 涨到 1.8 GB。配置 `MaxMetaspaceSize` 后改善。

---

## 瓶颈 3：磁盘 I/O

Master 持续向磁盘写：

- `build.xml`：每个 build 的元数据
- `log` 文件：build 日志（来自 Agent 实时回传）
- `workflow/`：Pipeline state 的持久化快照
- `nodes/`：Agent 状态
- `users/`：用户配置

### 隐藏杀手：`build.xml` 写入

Jenkins 每个 build 一个目录，里面有 `build.xml`（元数据）。**每次 build state 变化都会重写这个文件**——开始、结束、stage 切换、工件归档……一次 build 期间可能写几十次。

游戏团队多 build 并发场景：50 条 Pipeline 同时变状态 → 50 个 `build.xml` 写入 → IO 队列积压。

### 容量参考

| 团队规模 | 推荐磁盘类型 | 推荐 IOPS |
|---------|------------|----------|
| 小（<10 Agent） | SSD | 1000+ |
| 中（10-50 Agent） | NVMe SSD | 5000+ |
| 大（50+ Agent） | NVMe SSD（独立卷） | 20000+ |

**关键：JENKINS_HOME 千万不要放在网络存储（NFS / SMB）上**——延迟敏感操作（`build.xml` 重写）会让 Jenkins 整体卡死。即使云环境也要用本地 SSD（云盘 IOPS 配额可能成为瓶颈）。

### 诊断信号

**信号 1：iowait 高**
`top` 看 CPU 的 `wa%` 持续 >20%。

**信号 2：build 状态切换延迟**
build 在 UI 显示"Running"但实际已经结束 3 分钟。`build.xml` 写入延迟。

**信号 3：磁盘空间警告**
即使做了清理策略，几个月后磁盘仍然涨满——通常是 `workflow/` 目录（Pipeline state 历史）没清理。

### 失效模式

**典型故障：磁盘满 → Master 卡死**
- `JENKINS_HOME` 磁盘满
- `build.xml` 写入失败
- Pipeline state 无法序列化
- 新 build 无法启动
- UI 卡死（无法读 history）
- 整个 Jenkins 不响应

**救火方法（按紧急度）**：

1. 先释放空间：`rm -rf /var/jenkins/jobs/*/builds/old-*`（按时间）
2. 重启 Jenkins
3. 配置 build 历史保留策略（详见 203）

---

## 三类瓶颈的耦合反馈环

三类瓶颈不是孤立的，存在恶性耦合：

```
[连接数压力]
    ↓ Agent 通道堆栈占用堆
[堆内存压力]
    ↓ 频繁 GC + Pipeline state 持久化
[磁盘 I/O 压力]
    ↓ 写入延迟导致 Pipeline state 卡住
[连接数压力放大]
    ↑ Pipeline 卡住释放慢，连接占用变长
```

### 为什么单维度优化不够

- **只加内存**：堆够了，但 Master 写 `build.xml` 的速度不变 → 高峰期仍然卡
- **只换 SSD**：I/O 快了，但 Agent 通道占用的堆仍然没变 → 50+ Agent 仍然 OOM
- **只减 Agent**：Master 压力降了，但 build farm 吞吐降了 → 业务方排队等

### 正确的扩容路径

容量规划要**三个维度同时考虑**：

| 团队规模 | Agent 数 | Master CPU | 堆 | Metaspace | 磁盘 | IOPS |
|---------|---------|----------|-----|-----------|------|------|
| 小 | <10 | 4 核 | 4 GB | 1 GB | 200 GB SSD | 1000 |
| 中 | 10-50 | 8 核 | 16 GB | 2 GB | 500 GB NVMe | 5000 |
| 大 | 50-200 | 16 核 | 32 GB | 4 GB | 1 TB NVMe | 20000 |
| 超大 | 200+ | **必须分布式** | — | — | — | — |

---

## 扩容信号

什么时候不能继续靠"加配置"，必须做架构调整？

### 信号 1：垂直扩容到顶

Master 已经是 32 核 / 64 GB / NVMe，但仍然 80% CPU / 70% 内存。再加配置 ROI 急剧降低。

### 信号 2：单 Master 不再满足"故障恢复时间"

Master 重启 = 全队等。如果团队规模让"重启 5 分钟全队 100 人停工" = 5 × 100 = 500 人分钟成本，每周一次就是 30 小时人月成本。这时候考虑高可用方案（Active/Standby 或 CloudBees CI）。

### 信号 3：Agent 数突破 200

JNLP/WebSocket 通道在单 Master 上已经吃力。考虑：

- **方案 A**：拆成多 Master，按业务线分（每个产品组一个独立 Master）
- **方案 B**：迁到 CloudBees CI 等企业版（提供 Operations Center 做多 Master 联邦）

### 信号 4：跨地域 Agent

Master 在 IDC，海外 Agent 通过公网连——延迟高 + 通道不稳。这时候要么部署边缘 Master，要么 Operations Center 多 Master 联邦。

---

## 文末导读

下一步进 202 Agent 调度与标签体系——Master 的压力很大一部分来自 Agent 调度策略不合理。

L3 面试官线读者：本篇核心是"三类瓶颈耦合"那一节——基础设施容量规划不是单维度问题，是反馈环问题。
