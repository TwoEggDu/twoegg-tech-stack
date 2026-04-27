---
date: "2026-04-27"
title: "Workspace 与产物的磁盘治理"
description: '几十 GB 的游戏产物 + 几十个分支 + 几十个产品 = TB 级磁盘压力。Jenkins 的磁盘治理不是"加大磁盘"能解决的——必须按 workspace / 产物归档 / Pipeline state 三层分别治理。'
slug: "delivery-jenkins-ops-203-disk-governance"
weight: 1579
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Storage"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 90
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

- **磁盘占用的真实分布** —— 三层结构与每层量级
- **Workspace 治理** —— Library 是大头，清理策略要分层
- **产物归档策略** —— 按分支类型、按用途分级保留
- **Pipeline state 持久化清理** —— 隐藏的"workflow/" 目录
- **报警与自动清理** —— 不要等磁盘满了才知道
- **真实事故案例** —— Master 磁盘满了之后会发生什么

---

## 磁盘占用的真实分布

游戏团队 Jenkins 磁盘占用大致分布：

| 位置 | 内容 | 典型占比 |
|------|------|---------|
| Agent 上的 `workspace/` | 源码 + Library + 中间产物 | 60-70% |
| Master 上的 `JENKINS_HOME/jobs/*/builds/` | build 元数据 + 日志 + 归档产物 | 20-30% |
| Master 上的 `JENKINS_HOME/workflow/` | Pipeline state 持久化 | 5-15% |

Agent 上的 workspace 是真正的大头——单个 Unity 项目的 Library 缓存就 5-50 GB，多分支 × 多产品 × 多 Agent 让总量轻松到 TB 级。

### 真实数字（典型中型团队）

- 5 个产品 × 平均 20 个分支 × 每分支 30 GB = 3 TB （workspace）
- Master 归档 200 build × 平均 10 GB = 2 TB （归档）
- workflow/ 历史累积 ~ 50 GB（Pipeline state）

总计：5+ TB 级磁盘消耗。

---

## Workspace 治理：Library 是大头

### Workspace 占用拆解

单个 Unity 项目的 workspace 大致：

| 子目录 | 内容 | 大小 |
|-------|------|------|
| `Assets/` | 源资源 | 2-20 GB |
| `Library/` | Unity 缓存（含 import 中间产物） | 5-50 GB |
| `Temp/` | 构建中间产物 | 1-10 GB |
| `Build/` | 输出产物 | 1-10 GB |
| `Logs/` | Unity 日志 | 几十 MB |
| `obj/` | C# 编译中间产物 | 100MB-1GB |

**Library 占总 workspace 的 50-70%**——治理它就治理了大头。

### 三层 workspace 策略

#### 策略 1：完全独立 workspace（最简单，最贵）

每个 Pipeline 的每次 build 用新的 workspace：

```groovy
agent {
    node {
        label 'unity-builder'
        // 默认行为：每次新 workspace
    }
}
```

每次都从零 reimport Library——完整 build 慢得离谱（多 1-2 小时）。**不推荐**，除非是发版关键 build（要保证完全干净）。

#### 策略 2：固定 workspace 复用（推荐）

同名 Pipeline 的多次 build 共用同一个 workspace：

```groovy
agent {
    node {
        label 'unity-builder'
        customWorkspace "/data/workspaces/${env.JOB_NAME}-${env.BRANCH_NAME}"
    }
}
```

Library 增量复用，build 速度快。**风险**：workspace 状态污染（上次 build 留下的临时文件影响下次）。

#### 策略 3：分层 workspace（高级）

把 workspace 分成"持久层"和"临时层"：

- 持久层：`Library/` + `obj/`（增量复用）
- 临时层：`Temp/` + `Build/`（每次 build 清理）

```groovy
stages {
    stage('Cleanup Temp') {
        steps {
            sh 'rm -rf Temp/ Build/'
        }
    }
    stage('Build') {
        steps {
            sh 'unity -batchmode ...'
        }
    }
}
```

Library 不动，Build 干净——速度和卫生平衡。

### Workspace 容量限制

Agent 必须**给每个 workspace 设大小上限**，否则一个 Pipeline 的 workspace 漏出去能撑爆整台 Agent。

- **方案 A**：定时清理脚本（cron 每天凌晨清理超过 7 天未访问的 workspace）
- **方案 B**：Jenkins WS Cleanup 插件（`cleanWs()` step），按需清理
- **方案 C**：每个 workspace 用独立 Linux quota（最严格，配置麻烦）

游戏团队**推荐方案 A + B 组合**：日常用 B 在 Pipeline 里清理临时层；周期任务用 A 清理 stale workspace。

---

## 产物归档策略：按分支 / 按用途分级

Master 上 `jobs/<name>/builds/` 目录里堆积的归档产物是第二大头。

### 按分支类型保留

复用 104 多分支策略的真值表：

| 分支 | 保留次数 | 保留时间 | 含符号表 |
|------|---------|---------|---------|
| feature/* | 5 | 7 天 | 否 |
| dev | 30 | 30 天 | 否 |
| qa | 30 | 60 天 | 是 |
| release/* | 不限 | 永久 | 是 |
| hotfix/* | 不限 | 永久 | 是 |

Jenkins MBP 配置 → Properties → Discard old items，按分支类型设。

### 按用途分级归档

不是所有产物都归档到 Master 上，按用途分流：

| 产物类型 | 归档位置 | 理由 |
|---------|---------|------|
| 用户可下载的 build（IPA / APK） | Master 归档（短期）+ OSS / S3（长期） | 业务方频繁下载，Master 便利；长期保留靠对象存储 |
| 符号表（dSYM / mapping.txt） | OSS / S3（永久） | Master 不适合 GB 级长期归档 |
| 中间产物（Temp/ / Library/） | **不归档** | 没价值，不要进 Master |
| 日志 | Master 归档 | 调试用 |
| 测试报告 | Master 归档 + 监控系统 | 业务方查看 |

### 归档清理脚本

定期（每周）清理逻辑：

```bash
#!/bin/bash
# 清理超过保留期的 build artifacts
find $JENKINS_HOME/jobs/*/builds/ -maxdepth 2 -type d -mtime +60 \
    -exec rm -rf {}/archive/ \;
# 注意：保留 build.xml 和 log.gz，只删 archive/ 子目录
```

**不要直接删整个 build 目录**——这会让 Jenkins build history 出现"空洞"，UI 显示混乱。只删 `archive/` 子目录，保留元数据。

---

## Pipeline state 持久化清理

`JENKINS_HOME/workflow/` 是 Pipeline 引擎的状态持久化目录，每条 Pipeline 一个子目录。

### 目录结构

```
$JENKINS_HOME/jobs/<job-name>/builds/<build-#>/workflow/
├── 1.xml      # Pipeline 启动状态
├── 2.xml      # 第一个 step 完成
├── 3.xml      # ...
├── ...
└── 124.xml    # 每个 step 一个文件
```

一条复杂 Pipeline 可能产生几百个 XML 文件——单独不大，但累积起来很可观。

### 清理策略

**默认行为**：Jenkins 在 build 完成（成功 / 失败）后**不自动删除** workflow 状态文件。

清理时机：

- 跟随 build 历史清理（discard old builds 时连带删除）
- 手动清理 stale Pipeline state（用 Groovy 脚本扫描）

### 一个救火脚本

```groovy
import jenkins.model.Jenkins
import org.jenkinsci.plugins.workflow.job.WorkflowJob

Jenkins.instance.getAllItems(WorkflowJob).each { job ->
    job.getBuilds().each { build ->
        if (build.number < (job.getLastBuild().number - 50)) {
            // 删 50 次之前的 build 的 workflow 目录
            def workflowDir = new File(build.getRootDir(), "workflow")
            if (workflowDir.exists()) {
                workflowDir.deleteDir()
            }
        }
    }
}
```

跑一次能释放几十 GB，但风险：删了之后这些 build 的 Pipeline graph 就不能再 replay。

---

## 报警与自动清理

绝对不要"磁盘满了才发现"——那时候 Master 已经卡死，救火困难。

### 三级报警阈值

| 阈值 | 行为 |
|------|------|
| 70% 占用 | 告警邮件（运维 Owner） |
| 85% 占用 | 触发自动清理（删 stale workspace） |
| 95% 占用 | 紧急告警（电话） + 暂停接受新 build |

### 自动清理触发器

Jenkins 上的 [Disk Usage Plugin](https://plugins.jenkins.io/disk-usage/) 提供 API：

```groovy
// 在 Pipeline 里检查磁盘
def disk = Jenkins.instance.getNode('master').getDiskUsage()
if (disk.usedPercent > 85) {
    // 触发清理 Job
    build job: 'disk-cleanup-job', wait: false
}
```

清理 Job 的逻辑：

1. 删除超过 30 天的 archived artifacts（保留元数据）
2. 删除超过 50 次以前的 workflow state
3. 清理 stale workspace（7 天未访问）
4. 重新检查磁盘占用，写入监控

### 监控指标（接入 Prometheus）

```
jenkins_master_disk_usage_bytes{path="/var/jenkins"}
jenkins_master_disk_total_bytes{path="/var/jenkins"}
jenkins_agent_disk_usage_bytes{agent="agent-1"}
jenkins_workflow_dir_size_bytes
```

详见 205 Jenkins 自身的可观测性。

---

## 真实事故案例：Master 磁盘满了之后

### 时间线

- T-30 天：开始累积，无报警
- T-7 天：磁盘 80%，告警被忽略（"还能撑"）
- T-3 天：磁盘 92%，运维计划周末清理
- **T 时刻**：磁盘满
- T+5 分钟：Pipeline 无法启动新 build（无法写 build.xml）
- T+10 分钟：UI 卡顿（用户登录请求写 session 文件失败）
- T+20 分钟：所有进行中的 build 状态卡住
- T+30 分钟：业务方反馈"Jenkins 挂了"

### 救火过程

1. SSH 到 Master，发现磁盘 100%
2. 紧急 `du -sh /var/jenkins/jobs/*` 找最大占用 Job
3. `rm -rf /var/jenkins/jobs/<huge-job>/builds/*/archive/`（先释放空间）
4. 重启 Jenkins（很多卡住的状态需要重启清理）
5. **Pipeline state 损坏**：进行中的 build 全部失败，需要重跑
6. 配置 build retention：每个 Job 限制最多保留 50 个 build

### 成本

- 救火时长：4 小时
- 业务影响：所有产品流水线停 4 小时
- 数据损失：当时进行中的 build 全部失败重跑（多耗 2 小时）

### 教训

- **磁盘报警阈值不要超过 80%**——95% 已经太晚
- **自动清理必须早于人工介入**——指望"运维周末清理"不可靠
- **Master 磁盘必须独立**（不和系统盘共用）

---

## 文末导读

下一步进 204 Jenkins 升级踩坑：JVM / 插件 / 迁移——磁盘治理稳定后，下一个不稳定源是升级。

L3 面试官线读者：本篇核心是"按用途分流归档"那一节——把"长期归档"和"短期访问"分开，是大产物场景的工程必经之路。
