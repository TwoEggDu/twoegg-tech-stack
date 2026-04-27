---
date: "2026-04-27"
title: "Agent 调度与标签体系"
description: '标签不是给 Agent 贴名字，是把"调度意图"工程化。游戏团队的 Agent 标签设计有三个维度：能力、角色、容量——任何一维设计失败都会让调度变成排队地狱。'
slug: "delivery-jenkins-ops-202-agent-scheduling"
weight: 1578
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Agent"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 80
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **标签不是名字** —— 标签是调度策略的输入
- **三个维度：能力 / 角色 / 容量** —— 缺一不可
- **标签设计反例** —— 一个团队的"标签爆炸"故事
- **调度策略** —— 排他、亲和、优先级
- **离线监控与自动恢复** —— Agent 掉线该怎么处理

---

## 标签不是给 Agent 贴名字

Jenkins Agent 的标签（label）是字符串，但**它的语义是"调度过滤条件"**，不是"Agent 的别名"。

错误用法：

```
agent { label 'mac-mini-3' }   # 用机器名做标签
```

这种用法等于"我要 mac-mini-3 这一台机器"，调度灵活性归零——这台机器忙了 build 就排队，即使 mac-mini-1 / mac-mini-2 空着也用不上。

正确用法：把标签理解为**Pipeline 对 Agent 的能力要求**：

```
agent { label 'macos && unity-2022.3 && xcode-15' }
```

这告诉调度器：找一台**满足这些能力**的 Agent 来执行——具体哪台机器调度器决定。

---

## 三个维度：能力 / 角色 / 容量

游戏团队的 Agent 标签必须覆盖三个维度，缺一不可：

### 维度 1：能力（Capability）

Agent 上**装了什么、能做什么**：

- 操作系统：`linux` / `macos` / `windows`
- Unity 版本：`unity-2022.3` / `unity-6.0`
- 平台 SDK：`xcode-15` / `android-sdk-34` / `ndk-r25`
- 特殊工具：`hybridclr` / `gradle-8` / `node-18`

```
labels: linux unity-2022.3 android-sdk-34 ndk-r25 gradle-8
```

### 维度 2：角色（Role）

Agent 在 build farm 中**扮演的功能角色**：

- `unity-builder`：跑 Unity 构建任务的主力
- `test-runner`：跑自动化测试
- `archive-uploader`：归档与产物上传
- `monitoring`：监控类任务

为什么角色和能力分开？同一个 Agent 可能既能跑 build 也能跑测试，但你**不希望测试任务和长 build 任务争资源**——靠角色标签做调度隔离。

```
labels: linux unity-2022.3 unity-builder
```

### 维度 3：容量（Capacity）

Agent 的**资源规格分级**：

- `mem-32g` / `mem-64g`：内存等级
- `cpu-8` / `cpu-16`：CPU 等级
- `disk-large`：超大磁盘（专门跑大产物 build）

游戏团队 IL2CPP 构建机至少要 `mem-32g`——见 305。配 IL2CPP 任务时调度到资源不足的 Agent 会 OOM。

```
labels: linux unity-2022.3 unity-builder mem-64g cpu-16
```

### 完整标签示例

一台 Agent 的完整标签集：

```
linux                     # OS
unity-2022.3 unity-6.0    # Unity 版本（共存安装）
android-sdk-34            # Android SDK
ndk-r25
gradle-8
hybridclr                 # HybridCLR 已配置
unity-builder             # 角色
mem-64g cpu-16 disk-large # 容量
```

Pipeline 申请 Agent：

```groovy
agent { label 'linux && unity-2022.3 && android-sdk-34 && unity-builder && mem-64g' }
```

---

## 标签设计反例：一个团队的"标签爆炸"故事

某游戏团队 18 个月演进的真实路径：

### 初期（5 台 Agent）：每台一个标签

```
agent-1 → mac-1
agent-2 → mac-2
agent-3 → linux-1
agent-4 → linux-2
agent-5 → win-1
```

Pipeline 写死 `agent { label 'mac-1' }`。问题立刻显现：mac-1 忙时 mac-2 空着也用不上。

### 中期（15 台 Agent）：按能力打标签

```
agent-1 → macos unity
agent-2 → macos unity
...
agent-10 → linux unity
...
```

调度灵活了，但出现"测试任务和构建任务挤一起"问题——测试 30 分钟跑不完是因为构建占了 8 个 Agent。

### 中后期（25 台 Agent）：加角色标签

```
agent-1 → macos unity unity-builder
agent-12 → linux unity test-runner
agent-15 → linux unity archive-uploader
...
```

测试和构建隔离了。但 Unity 升级后又出现新问题——某些 Pipeline 要 Unity 2021、某些要 Unity 2022.3，无法调度到对的 Agent。

### 现在（40 台 Agent）：加版本标签

```
agent-X → macos unity-2021.3 unity-builder
agent-Y → macos unity-2022.3 unity-builder
agent-Z → macos unity-2022.3 unity-6.0 unity-builder  # 双版本机
```

### 教训

- **标签设计要随团队规模演进**，但每次演进都要**统一所有 Agent 的标签格式**——不能新加的 Agent 用新规范，老 Agent 留旧标签
- **不要用机器名做标签**——一开始就要按"能力 / 角色 / 容量"打
- **每个维度独立**——能力和角色不要混（不要 `mac-builder` 这种合体标签）

---

## 调度策略

### 策略 1：排他（exclusive）

某些任务**必须独占 Agent 整机**——比如 IL2CPP 构建（[详见 305]({{< relref "delivery-engineering/delivery-jenkins-ops-305-il2cpp-build.md" >}})），同 Agent 跑两个 IL2CPP 会 OOM。

Jenkins Pipeline 配置：

```groovy
pipeline {
    agent { label 'unity-builder && mem-64g' }
    options {
        // 该 Agent 在该 build 期间不接受其他 build
        // 通过减少 Agent 的 executors 数到 1 实现
    }
}
```

实际做法：把 IL2CPP 类 Agent 的 executors 配为 1（每台 Agent 同一时间只跑一个任务）。

### 策略 2：亲和（affinity）

希望同一个产品的 build **尽量调度到同一台 Agent**——为了利用 workspace 缓存。

Jenkins 不直接支持"亲和性调度"，但可以**通过 workspace 路径管理近似实现**：

```groovy
agent {
    node {
        label 'unity-builder'
        customWorkspace "/data/jenkins-workspaces/${env.JOB_NAME}"
    }
}
```

`customWorkspace` 让多次同名 build 用同一个工作目录——只要调度到同一台 Agent，Library 缓存就能复用。

### 策略 3：优先级

发版分支的 build 优先级高于 dev 分支。Jenkins 的优先级插件：

- [Priority Sorter Plugin](https://plugins.jenkins.io/PrioritySorter/)
- 配置：每个 Job 设置 priority（数字越小越高）
- release/* 设 1，dev 设 5，feature/* 设 10

队列里 release build 永远排前面。

### 策略 4：限流（throttle）

防止某类任务占满 Agent 池：

- 全局：feature 分支 build 同时最多 5 个
- 单产品：TopHero 的 build 同时最多 3 个
- 单平台：iOS build 同时最多 2 个（macOS Agent 稀缺）

通过 [Throttle Concurrent Builds Plugin](https://plugins.jenkins.io/throttle-concurrents/) 实现。

---

## 离线监控与自动恢复

Agent 掉线是常态——网络抖动、Agent 重启、Master 重启都会导致 Agent 短暂离线。

### 离线信号

Master 把 Agent 标记为 offline 的几种情况：

- **JNLP 通道断开** → 几秒内重连成功 → 不影响 build
- **JNLP 通道断开** → 长时间未重连 → Agent 标记 offline → 进行中的 build 失败
- **Agent 心跳超时** → 标记 offline → 同上

### 自动恢复

Agent 配置层面：

```bash
# JNLP Agent 启动参数
-jnlpUrl http://master:8080/computer/agent-name/slave-agent.jnlp
-secret xxx
-workDir /home/jenkins
-disableHttpsCertValidation        # 自签证书时
-jvmargs -XX:+UseG1GC
```

启动脚本封装为 systemd / launchd 服务，挂掉自动重启：

```ini
# /etc/systemd/system/jenkins-agent.service
[Unit]
Description=Jenkins Agent
After=network.target

[Service]
ExecStart=/path/to/agent.sh
Restart=always
RestartSec=10
User=jenkins
```

### 监控告警

Jenkins 自身提供 Agent 离线状态 API：

```
GET /computer/api/json
```

外部监控（Prometheus / Grafana）拉这个 API，对 offline 状态发告警。

### 进行中 build 的丢失风险

Agent 长时间离线时，正在跑的 build 会失败。处理策略：

- **关键 build（release / hotfix）**：用专用稳定 Agent，不调度到不稳定 Agent
- **其他 build**：失败后自动 retry（在 Jenkinsfile 里加 retry 逻辑，但只对 transient 故障 retry）

**绝对不要做**：在 Jenkinsfile 全局 `retry(3)`——这会让 license 占用 / OOM 之类的非 transient 故障被无意义重试，浪费 Agent 时间（[详见 001 总论的"失败重试盲目化"]({{< relref "delivery-engineering/delivery-jenkins-ops-001-why-different.md" >}})）。

---

## 文末导读

下一步进 203 Workspace 与产物的磁盘治理——Agent 调度对了，但 Agent 上的磁盘还是会被产物撑爆。

L3 面试官线读者：本篇核心是"三维度标签"那一节——调度策略的工程化是治理边界的工程化。
