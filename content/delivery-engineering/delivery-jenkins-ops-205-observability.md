---
date: "2026-04-27"
title: "Jenkins 自身的可观测性：监控与告警"
description: '"Jenkins 自己挂了谁来发现"——这个问题在 99% 的团队里答案是"业务方报警"。本篇讲怎么从被动救火变成主动监控：三层观测、关键指标、告警分级、Prometheus 落地。'
slug: "delivery-jenkins-ops-205-observability"
weight: 1581
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Observability"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 110
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 在本篇你会读到

- **谁来监控监控者** —— Jenkins 不能自己监控自己
- **三层观测：Master / Agent / Pipeline** —— 每层关注不同信号
- **关键指标体系** —— 不是越多越好，要有所聚焦
- **告警分级** —— P0/P1/P2 的判定标准
- **Prometheus + Grafana 落地** —— 完整 stack 与配置示例
- **真实告警示例** —— 三类典型告警的处理路径

---

## 谁来监控监控者

游戏团队 Jenkins 监控最常见的反模式：

> 在 Jenkins 上配一个"监控 Pipeline"，定时跑，挂了发钉钉。

**这是错的。** Jenkins 自己挂了，监控 Pipeline 也跑不起来——根本不会发钉钉。

**正确认知**：监控 Jenkins 必须由**外部独立系统**完成，不能依赖 Jenkins 本身。

外部监控系统的最低要求：

- 独立部署（不和 Jenkins 同机）
- 主动拉取（不是 Jenkins 推送，因为 Jenkins 挂了就不推了）
- 健康检查的"反向逻辑"：超过 N 分钟没收到数据就告警，而不是收到失败信号才告警

游戏团队推荐组合：**Prometheus（数据采集）+ Grafana（可视化）+ AlertManager（告警分发）**——开源、成熟、和 Jenkins 集成好。

---

## 三层观测：Master / Agent / Pipeline

### 层 1：Master 健康

最关键的层。Master 挂了 = 全队停工。

关注信号：

- **进程存活**（HTTP 200 from `/login`）
- **系统资源**（CPU / 内存 / 磁盘 / 文件描述符）
- **JVM 内部**（堆使用率、GC 频率、活跃线程数）
- **业务功能**（队列长度、Agent 在线数、近 1 小时构建数）

### 层 2：Agent 健康

Agent 挂了影响吞吐，但不致命（其他 Agent 能接活）。

关注信号：

- **在线状态**（每个 Agent 的 online/offline）
- **资源使用**（CPU / 内存 / 磁盘 / 网络）
- **Workspace 占用**
- **License 占用**（Unity Agent 特有，[详见 301]({{< relref "delivery-engineering/delivery-jenkins-ops-301-license-pool.md" >}})）

### 层 3：Pipeline 健康

业务可见层。

关注信号：

- **构建成功率**（按产品 / 按分支）
- **构建时长**（趋势）
- **队列等待时间**
- **失败原因分布**（编译失败 / 超时 / OOM / 其他）

每层数据来源不同，告警阈值也不同。下面分别展开。

---

## 关键指标体系

不要"什么都监控"——指标越多 noise 越大。聚焦**这套核心指标**：

### Master 指标（10 个）

```
# 进程
jenkins_master_up                          # 0/1
jenkins_master_response_time_ms            # /login 响应时间

# 资源
jenkins_master_cpu_usage_percent
jenkins_master_memory_used_bytes
jenkins_master_disk_usage_percent{path="/var/jenkins"}
jenkins_master_open_fds                    # 文件描述符

# JVM
jenkins_jvm_heap_used_bytes
jenkins_jvm_gc_pause_seconds_total         # GC 累计暂停时长

# 业务
jenkins_queue_size
jenkins_active_builds
```

### Agent 指标（按 Agent 维度）

```
jenkins_agent_online{agent="agent-1"}      # 0/1
jenkins_agent_cpu_usage_percent
jenkins_agent_memory_used_bytes
jenkins_agent_disk_usage_percent
jenkins_agent_workspace_size_bytes
jenkins_agent_license_used                 # Unity Agent
jenkins_agent_license_total
```

### Pipeline 指标（按 Job 维度）

```
jenkins_build_total{job="...",result="..."}      # 累计 build 数
jenkins_build_duration_seconds{job="..."}        # 时长直方图
jenkins_build_queue_wait_seconds{job="..."}     # 队列等待
jenkins_build_failure_reason{job="...",reason="..."}  # 失败分类
```

---

## 告警分级

告警必须分级——所有告警都打到一个频道，最严重的会被淹没。

### P0：必须立刻响应（电话 / 短信）

| 信号 | 阈值 | 含义 |
|------|------|------|
| Master 进程挂了 | `up == 0` 持续 1 分钟 | 全队停工 |
| Master 磁盘满 | `disk_usage > 95%` | 即将停工 |
| Master OOM | JVM heap > 90% 持续 5 分钟 | 即将挂 |
| 全部 Agent 离线 | `sum(agent_online) == 0` | 无法跑 build |

P0 必须**白天 5 分钟、夜间 15 分钟内**有人响应。

### P1：当日响应（钉钉 / 企微 + 邮件）

| 信号 | 阈值 | 含义 |
|------|------|------|
| Master 内存压力 | heap > 75% 持续 30 分钟 | 即将 OOM |
| Master 磁盘告急 | `disk_usage > 85%` | 即将满 |
| 关键 Agent 离线 | macOS Agent 离线 > 30 分钟 | iOS 构建受阻 |
| License 池耗尽 | `license_used / license_total > 95%` | 新 build 排队 |
| 队列堆积 | `queue_size > 50` 持续 30 分钟 | 业务方等待 |

### P2：日常关注（仅邮件 / 周报）

| 信号 | 阈值 | 含义 |
|------|------|------|
| 失败率上升 | 某 Job 失败率 > 上周 + 20% | 可能引入新问题 |
| 构建时长上升 | 某 Job 中位时长 > 上周 + 20% | 可能性能退化 |
| Agent workspace 膨胀 | 某 Agent workspace > 200 GB | 即将需要清理 |

---

## Prometheus + Grafana 落地

### 数据源：Jenkins Prometheus Plugin

[Prometheus Metrics Plugin](https://plugins.jenkins.io/prometheus/) 暴露 `/prometheus` endpoint：

```
GET http://jenkins-master:8080/prometheus/
```

返回 Prometheus 格式的指标。Prometheus 配置抓取：

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'jenkins'
    metrics_path: '/prometheus/'
    static_configs:
      - targets: ['jenkins-master:8080']
```

### Master 系统指标：node_exporter

Jenkins Plugin 不能采集 OS 层指标（CPU / 磁盘 / 文件描述符），需要在 Master 上跑 `node_exporter`：

```
GET http://jenkins-master:9100/metrics
```

### Agent 指标：每个 Agent 跑 node_exporter

游戏团队的 Agent 通常 5-50 台，每台跑一个 node_exporter。Prometheus 配 service discovery 自动发现。

### Pipeline 指标：自定义采集

部分 Pipeline 指标需要在 Jenkinsfile 里手动写入：

```groovy
post {
    always {
        script {
            def duration = currentBuild.durationString
            def result = currentBuild.result ?: 'SUCCESS'
            // 写入 Pushgateway
            sh """
                echo 'jenkins_build_duration_seconds{job="${env.JOB_NAME}"} ${currentBuild.duration / 1000}' \
                    | curl --data-binary @- http://pushgateway:9091/metrics/job/jenkins
            """
        }
    }
}
```

或者用更优雅的方式：Shared Library 封装 `recordBuildMetrics()` 函数（[详见 102]({{< relref "delivery-engineering/delivery-jenkins-ops-102-shared-library.md" >}})）。

### Grafana Dashboard 模板

社区有现成的 [Jenkins Dashboard](https://grafana.com/grafana/dashboards/9964/)，但游戏团队需要定制：

- 加 Unity License 占用面板
- 加按产品的构建成功率
- 加按平台的构建时长

### AlertManager 配置

```yaml
groups:
  - name: jenkins-master
    rules:
      - alert: JenkinsMasterDown
        expr: up{job="jenkins"} == 0
        for: 1m
        labels:
          severity: P0
        annotations:
          summary: "Jenkins Master 不可达"
          
      - alert: JenkinsMasterDiskFull
        expr: |
          (1 - node_filesystem_avail_bytes{mountpoint="/var/jenkins"} 
               / node_filesystem_size_bytes{mountpoint="/var/jenkins"}) > 0.95
        for: 5m
        labels:
          severity: P0
        annotations:
          summary: "Jenkins 磁盘 > 95%"
```

---

## 三类典型告警的处理路径

### 案例 1：Master 内存告警（P1）

**触发**：JVM heap > 75% 持续 30 分钟

**处理**：

1. 看 Grafana 面板：是不是有"长跑 Pipeline"卡了内存？
2. 检查最近是否升级了 Jenkins 或大插件（可能引入 leak）
3. 看 GC 日志：Mixed GC 频率
4. 短期方案：重启 Master（清空堆 + 元空间）
5. 长期方案：分析 heap dump，定位泄漏

### 案例 2：License 池耗尽（P1）

**触发**：`license_used / license_total > 95%`

**处理**：

1. 看 Grafana：是真的全在用，还是泄漏？
2. 用 license server 命令行检查：

   ```bash
   /opt/Unity/Editor/Unity --returnlicense  # 单机释放
   ```

3. 如果是泄漏（占用未释放的 ghost 激活）：
   - 重启对应 Agent（强制释放 license）
   - 长期方案见 301
4. 如果真的不够用：**临时减少并发** 或 **申请增加 seat**

### 案例 3：构建时长趋势异常（P2）

**触发**：某 Job 中位时长 > 上周 + 20%

**处理**：

1. 看 Grafana 面板：是哪个 stage 慢了？
2. 对比最近代码 / 资源变更
3. 看 Agent 资源占用：是不是 Agent 性能下降（磁盘满、内存压力）
4. 如果是 Library 缓存失效：[见 001 总论"主角反转"]({{< relref "delivery-engineering/delivery-jenkins-ops-001-why-different.md" >}})
5. 如果是资源膨胀：找美术 / 程序确认是否预期

---

## 监控的"反模式"

### 反模式 1：监控所有指标

100 个 Dashboard，没人看。**精简到 10-20 个核心面板**。

### 反模式 2：所有告警同优先级

所有告警都打到 #ops 频道，1 周后大家屏蔽这个频道。**严格分级**。

### 反模式 3：告警阈值太敏感

`P1` 告警每天 100 条 → 没人响应。**把"持续时长"加进阈值**（例如"持续 30 分钟"而不是"瞬时 > 75%"）。

### 反模式 4：监控不演练

配了告警从没真的触发过——真出事时才发现告警没生效。**每季度做一次 chaos drill**：故意让 Master 高内存、断网、磁盘填满，看告警是否准确触发。

---

## 文末导读

下一步进 301 Unity License 池管理：稀缺资源的治理——监控之外，License 是游戏团队 Jenkins 最特殊的运维对象。

L3 面试官线读者：本篇核心是"P0/P1/P2 告警分级 + 反模式"——可观测性不是"指标越多越好"，是"信号 / 噪声比"工程。
