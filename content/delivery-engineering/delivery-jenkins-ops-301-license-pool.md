---
date: "2026-04-27"
title: "Unity License 池管理：稀缺资源的治理"
description: 'Unity License 是激活式（不是 floating），泄漏后 seat 不释放。游戏团队 Jenkins 最早撞墙的地方就是 License 池——本篇拆解三类故障（不够用 / 泄漏 / 占用不释放）和池化治理方案。'
slug: "delivery-jenkins-ops-301-license-pool"
weight: 1582
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
  - "License"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 120
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
leader_pick: true
---

## 在本篇你会读到

- **License 不是 token，是激活** —— 这一个区别决定一切
- **三类故障** —— 不够用 / 泄漏 / 占用不释放
- **License 池架构** —— 中央 server vs Agent 池的取舍
- **自动化激活与释放** —— 流水线必须 finally 释放
- **监控与告警** —— License 占用是核心指标
- **真实事故** —— "半夜全队卡死"的还原

---

## License 不是 token，是激活

理解 Unity License 的关键认知：**它和 SaaS 软件的"floating license"完全不同**。

### Floating License（典型软件）

- 中央 license server 维护 `N` 个 token
- 客户端启动时**借用**一个 token，结束时**归还**
- 网络断了 → 客户端自动归还（server 看 timeout）
- token 数 = 真实并发数

### Unity License（激活式）

- 每个 Unity 安装做"激活"动作，绑定一个 seat
- 激活之后 Unity 在该机器上可以**离线**运行
- 退出 Unity 时**不自动释放** seat
- 必须显式做"返回激活"（return activation）才释放

```bash
# 激活
unity -batchmode -quit -nographics -username xxx -password xxx -serial xxx

# 返回激活
unity -batchmode -quit -returnlicense
```

### 为什么这一点是关键

通用 CI 经验假设的是 floating license——build 进程崩溃 → 自动释放。

Unity 不行：

- build 进程崩溃 → seat 仍然占着
- Agent 强杀 → seat 仍然占着
- Agent 重启 → seat 仍然占着
- 几天后 seat 池被"幽灵激活"占满

这就是游戏团队 Jenkins 第一个撞墙的地方。

---

## 三类故障

### 故障 1：License 不够用

**现象**：build 排队，理由是 "no license available"。

**真实原因可能有几种**：

- **真不够用**：团队规模长大，原来 10 个 seat 不够支撑 20 个工程师
- **未优化分配**：local 工程师机和 build farm 共用 seat 池，工程师机器占用过多
- **泄漏导致看起来不够用**（最常见）

### 故障 2：License 泄漏

**现象**：license server 显示 100% seat 占用，但 build farm 只有 30% Agent 在跑 build。

**根因**：build 异常退出（OOM / 强杀 / Agent 离线）没释放 license。这是 Unity license 的"原罪"——它没有 timeout 自动释放机制。

### 故障 3：占用不释放（Stuck Activation）

**现象**：某 Agent 上的激活记录一直在，但 Agent 实际已经停机或迁移。

**触发**：

- Agent 整机迁移（换硬件、换云实例），新机器无法激活，因为旧激活还挂着
- Agent 装的 Unity 被重装，激活记录冗余
- License server 网络抖动期间，激活状态不一致

### 三类故障的关系

```
[License 不够用] ← 大多数情况是表象
    ↓
[License 泄漏] ← 真正根因（70%）
[占用不释放] ← 真正根因（20%）
[真不够用] ← 真正根因（10%）
```

**先排除泄漏和占用不释放，再考虑加 seat**——盲目买 seat 是浪费。

---

## License 池架构

### 模式 A：每 Agent 单独激活（最简单）

每台 Agent 装 Unity 后单独激活，绑定一个 seat。

```
Agent 1 → License Seat 1
Agent 2 → License Seat 2
...
Agent N → License Seat N
```

**优点**：简单，Agent 离线不会影响其他 Agent。
**缺点**：seat 数 = Agent 数，浪费严重（Agent 不跑 build 时 seat 也占着）。

### 模式 B：Floating License Server（Unity 提供）

Unity 提供企业版 floating license server，能动态分配 seat：

```
[License Server]
    ↓ 动态分配
Agent 1 → Seat #?
Agent 2 → Seat #?
...
```

**优点**：seat 数 = 实际并发数（不是 Agent 数）。
**缺点**：需要 Unity Pro 企业级订阅，成本高；server 自身要高可用。

### 模式 C：自建 License 池调度（推荐中型团队）

自建一个 license 调度器，根据当前 build 队列决定哪些 Agent 激活、哪些休眠：

```
[Pool Manager]
    ↓ 分配
Active Agents → 激活状态（有 seat）
    ↑ 释放后
Standby Agents → 休眠（无 seat）
```

实现：在 Jenkins Shared Library 里写一个 `requestLicense()` / `releaseLicense()` 包装：

```groovy
// vars/withUnityLicense.groovy
def call(Closure body) {
    def licenseAcquired = false
    try {
        // 阻塞获取 seat（来自池）
        licenseAcquired = poolManager.acquire(env.NODE_NAME, timeout: 30)
        if (!licenseAcquired) {
            error("Cannot acquire Unity license, pool exhausted")
        }
        body()
    } finally {
        if (licenseAcquired) {
            poolManager.release(env.NODE_NAME)
        }
    }
}
```

**Pool Manager** 是一个独立进程（Redis / 文件锁 / 简单 HTTP 服务都行），管理"哪些 Agent 当前持有激活、哪些是空闲池"。

---

## 自动化激活与释放

### 关键模板：finally 释放

每个 Pipeline 的 Unity 调用必须包在 `try-finally` 里：

```groovy
pipeline {
    agent { label 'unity-builder' }
    stages {
        stage('Build') {
            steps {
                script {
                    try {
                        sh '''
                            unity -batchmode -quit -nographics \
                                -username "$UNITY_USER" \
                                -password "$UNITY_PASS" \
                                -serial "$UNITY_SERIAL" \
                                -executeMethod Build.iOS
                        '''
                    } finally {
                        sh 'unity -batchmode -quit -nographics -returnlicense || true'
                    }
                }
            }
        }
    }
    post {
        always {
            // 双保险：post 块再尝试释放
            sh 'unity -batchmode -quit -nographics -returnlicense || true'
        }
    }
}
```

**关键点**：

- `finally` 块（stage 内）和 `post { always {} }`（pipeline 级）双保险
- `|| true` 防止释放失败导致 stage 标记 FAILURE（释放可能因为 Unity 进程已死而失败）
- 即使 build 成功也要释放——Unity 不会因为正常退出而自动释放 seat

### Shared Library 封装

每个 Jenkinsfile 都写这段重复代码不可持续。封装到 Shared Library：

```groovy
// Jenkinsfile
@Library('game-pipeline-lib@v1.4.0') _

pipeline {
    agent { label 'unity-builder' }
    stages {
        stage('Build') {
            steps {
                withUnityLicense {
                    sh 'unity -batchmode -executeMethod Build.iOS'
                }
            }
        }
    }
}
```

`withUnityLicense` 内部处理激活、释放、错误处理。所有业务方流水线**自动**遵守规范。

---

## 监控与告警

### 核心指标

```
unity_license_total                 # 总 seat 数
unity_license_used                  # 当前占用
unity_license_active_agents         # 当前激活 Agent 列表
unity_license_acquire_duration_ms   # 获取 license 等待时长
```

### 告警阈值

| 信号 | 阈值 | 级别 |
|------|------|------|
| License 占用率 > 90% | 持续 10 分钟 | P1 |
| License 占用率 > 99% | 持续 1 分钟 | P0 |
| 等待 license 超时（30 秒） | 1 次 | P1 |
| License 占用 Agent 数 > 实际跑 build Agent 数 + 20% | 持续 30 分钟 | P1（疑似泄漏） |

最后一条最关键——**"占用 Agent 数远大于活跃 Agent 数"是泄漏的最强信号**。

### 自动检测泄漏

Pool Manager 定期扫描：

```python
# 伪代码
for agent in active_agents:
    if not agent.is_running_build():
        if agent.last_build_finished_at < (now - 10 minutes):
            # Agent 没在跑 build 但仍持有 license
            log.warning(f"License leak suspected on {agent.name}")
            agent.force_release_license()
```

强制释放可能让"刚结束 build 的"Agent 误伤——但在游戏团队的实践中，**误伤代价远小于让 license 池一直堵着**。

---

## 真实事故："半夜全队卡死"还原

### T 时刻

周三晚上 22:00，业务方所有夜间 build 触发。

### T+30 分钟

某个夜间 build 因为 OOM 挂了（资源烘焙阶段内存不够，[详见 305]({{< relref "delivery-engineering/delivery-jenkins-ops-305-il2cpp-build.md" >}})）。Agent 强杀进程。**license 没释放。**

### T+1 小时

队列里 5 个其他 Pipeline 排队，等不到空闲 license seat。

### T+3 小时（半夜 1 点）

队列堆到 30+ Pipeline，license 池 100% 占用。但实际只有 2 个 build 在跑——其他 28 个都是泄漏。

### T+8 小时（早上 6 点）

业务方早班拿 build，发现一个都没出来。研发负责人被电话叫醒。

### T+9 小时

排查链路：Jenkins UI 看 Agent 状态正常 → 看 Unity license server 显示"License pool exhausted" → 登 license server 看占用，发现 28 个"激活记录"对应的 Agent 实际不在跑 build。

### T+10 小时

紧急救火：所有 Agent 跑 `unity -returnlicense`（部分能释放，部分因为 Unity 进程已死失败）→ 重启所有 Unity Agent（强制释放 license seat）→ 队列开始消化。

### 上午全员开复盘会

事故损失：

- 2 个产品的 dev / qa build 全部延迟一晚
- 2 个发版 release 流程被迫延后
- 业务方对 Jenkins 信任度下降一档

### 改进项

- ✅ 所有 Pipeline 强制 `try-finally` + `post-always` 双重释放
- ✅ Pool Manager 加入"泄漏检测 + 强制释放"
- ✅ License 占用监控告警接入（之前只监控 Master 内存）
- ✅ Agent 配置 OOM-killer 时主动释放 license（trap 信号）

---

## 文末导读

下一步进 302 大仓库在 Jenkins 下的 Workspace 策略——License 解决之后，下一个游戏团队特化问题是几十 GB 仓库怎么 checkout。

L3 面试官线读者：本篇核心是"激活式 vs floating"那一节——一个产品设计决策（Unity 选了激活式 license），决定了所有团队必须自建池化治理。这是"产品约束反向影响工程实践"的典型例子。
