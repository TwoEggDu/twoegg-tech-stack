---
date: "2026-04-27"
title: "Declarative vs Scripted Pipeline：游戏团队的选型取舍"
description: 'Declarative 限制能力换可读性，Scripted 给你完整 Groovy 但维护成本高。游戏团队不是二选一，而是分层选——从一个真实迁移案例看清两种语法各自的天花板，以及"必须切换"的四个信号。'
slug: "delivery-jenkins-ops-101-declarative-vs-scripted"
weight: 1572
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Pipeline"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 20
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **选型这件事到底在选什么** —— 不是"哪个语法好"，是"哪种约束你愿意接受"
- **Declarative 的能力边界** —— 撑住什么、撑不住什么
- **Scripted 的代价与回报** —— Groovy 全集换来的灵活性和负担
- **游戏团队的"必须切换"信号** —— 四个让 Declarative 失效的真实场景
- **分层选型决策** —— 顶层 Declarative + 底层 Scripted 的折中方案
- **迁移成本与陷阱** —— 一个真实迁移案例的时间线

---

## 选型这件事到底在选什么

Jenkins 的两种 Pipeline 语法选型，网上文章 90% 在比"语法漂亮"——Declarative 像 YAML 那样有结构，Scripted 像 Groovy 那样灵活。这层比较是表面的，对游戏团队没用。

真正在选的是一对**约束 vs 能力**的取舍：

| 维度 | Declarative | Scripted |
|------|------------|----------|
| 表达能力 | 受限 DSL | 完整 Groovy（含 JVM 库） |
| 静态可分析性 | 高（structure 在 parse 时已知） | 低（运行时才知道结构） |
| Blue Ocean / 可视化 | 完全支持 | 部分支持 |
| 错误信息 | 结构性错误前置 | 运行时才报 |
| Restart from Stage | 原生支持 | 需要手写状态保存 |
| 学习曲线 | 1 天 | 1-2 周（要会 Groovy + Jenkins API） |
| 维护人门槛 | 普通构建工程师 | 需要 Jenkins 老司机 |

游戏团队的关键信号是**最后一行**：Declarative 让普通构建工程师能维护；Scripted 让你必须养一个"Jenkinsfile 老司机"。

不是哪个更好，是你的团队现在能负担哪种维护成本。

---

## Declarative：能撑住什么、撑不住什么

Declarative 的设计哲学是"用结构化 DSL 换静态可分析性"。能撑住的场景：

**线性流水线 + 简单分支**

```groovy
pipeline {
    agent { label 'unity-builder' }
    stages {
        stage('Checkout') { steps { checkout scm } }
        stage('Build') {
            steps {
                sh 'unity -batchmode -quit -executeMethod Build.iOS'
            }
        }
        stage('Archive') {
            steps { archiveArtifacts 'build/**' }
        }
    }
}
```

这种"checkout → build → archive"的线性结构，Declarative 表达力完全够。游戏团队 80% 的"单产品 + 单平台"流水线属于这一类。

**简单矩阵（matrix axis）**

Declarative 在 2.x 后期加了 `matrix` 块，能表达"平台 × 配置"这种笛卡尔积：

```groovy
matrix {
    axes {
        axis { name 'PLATFORM'; values 'iOS', 'Android' }
        axis { name 'CONFIG'; values 'Debug', 'Release' }
    }
    stages {
        stage('Build') { steps { sh "build.sh $PLATFORM $CONFIG" } }
    }
}
```

但 matrix 的能力是**有限的笛卡尔积**——不能根据某个轴的值动态裁剪 stages，不能跨 axis 做依赖。

### Declarative 撑不住的四类场景

**1. 动态生成 stage 列表**

游戏团队常见需求：根据产品配置文件读取要构建的平台列表，再动态生成对应的 stages。Declarative 的 `stages {}` 块在 parse 时就要确定，做不到运行时动态生成。

**2. 复杂条件分支**

`when {}` 表达式只支持简单条件（branch / environment / expression）。一旦你需要"如果上一个 stage 的某个产物存在，则跳过下一个"——Declarative 表达不出来。

**3. 跨流水线状态**

游戏团队常见的"主流水线触发子流水线，子流水线结果回传给主流水线"——Declarative 能 `build job:` 触发，但拿回结果做条件判断很笨重。

**4. 自定义错误处理**

Declarative 的 `post { failure { } }` 适合发钉钉通知，但做不到"失败时自动从某个状态点 restart 而不是重头跑"。

---

## Scripted：代价与回报

Scripted 给你完整 Groovy，包括能 `import` JVM 库、写 closure、操作 Jenkins 内部 API。

```groovy
node('unity-builder') {
    def platforms = readJSON(file: 'build-config.json').platforms
    def parallelStages = [:]
    platforms.each { platform ->
        parallelStages["build-${platform}"] = {
            stage("Build ${platform}") {
                buildPlatform(platform)
            }
        }
    }
    parallel parallelStages
}
```

这段代码在 Declarative 里写不出来——它需要运行时根据 JSON 内容生成 `parallel` 的 map。

### Scripted 的真实代价

**代价 1：错误信息的"假漂亮真无用"**

Scripted 报错经常是 Groovy 栈帧，不是 Jenkins 的结构性错误：

```
java.io.NotSerializableException: java.util.Random
    at WorkflowScript.run(WorkflowScript:42)
```

这个错通常是 CPS（Continuation Passing Style）相关——Pipeline 用 CPS 实现可恢复执行，所以非 Serializable 对象不能跨 step 传递。Declarative 在这一点上把陷阱挡在 parse 阶段，Scripted 让你撞上了再调一周。

**代价 2：Replay 是双刃剑**

Scripted 的 `Replay` 功能让你能在 Jenkins UI 直接修改 Jenkinsfile 重跑，调试很爽。但**线上事故的根因经常是"某人 Replay 改了一个 stage，没回写到 git，第二天构建用错版本"**。这种治理风险 Declarative 同样存在但发生率低。

**代价 3：Sandbox 限制经常踩坑**

Jenkins 默认 Pipeline 跑在 Groovy Sandbox 里，能用的 API 是受限白名单。Scripted 写得越深越容易撞 `RejectedAccessException`。然后你需要管理员去 "In-process Script Approval" 里逐个批准——不是路径上的事故，是治理上的麻烦。

### Scripted 的真实回报

- 表达力天花板高（动态生成、复杂条件、跨流水线状态都能写）
- Shared Library 配合 Scripted 才能发挥全部能力（详见 102）
- 复杂场景下代码量反而比 Declarative 少（Declarative 写复杂逻辑要绕多次）

---

## 游戏团队的"必须切换"信号

什么时候 Declarative 撑不住，必须上 Scripted？四个具体信号：

### 信号 1：流水线开始出现 `script {}` 块超过 3 个

Declarative 提供了 `script {}` 转义口——可以在 Declarative 内嵌一段 Scripted 代码。一开始用一两个 `script {}` 处理特殊场景没问题；超过 3 个就是味道——本质是 Declarative 已经撑不住，但你还在硬装。

```groovy
// Declarative 里的 script 块越多，越说明该切了
stage('Build') {
    steps {
        script {  // 第 1 个 script
            // ...动态决定参数
        }
        sh 'build.sh ${COMPUTED_ARG}'
        script {  // 第 2 个
            // ...判断结果
        }
        script {  // 第 3 个 → 信号了
            // ...
        }
    }
}
```

### 信号 2：要根据外部数据源（JSON / YAML / 数据库）生成 stages

只要你需要读外部配置文件、然后**生成数量不确定的 stages**——Declarative 的 `matrix` 撑不住（matrix 的轴是静态的）。这是游戏团队"多产品矩阵"的典型场景，详见 103。

### 信号 3：流水线之间有复杂依赖（不只是 `build job:`）

比如：主流水线触发 5 个子流水线，要等到其中**任意 3 个**完成就继续，剩下 2 个失败也允许。Declarative 表达不出"任意 K-of-N"逻辑，必须 Scripted。

### 信号 4：要做高级错误恢复

游戏团队的真实场景：iOS 构建挂了，但 Android 构建成功——希望"标记 iOS 失败，但流水线继续走 Android 后续流程，最后整体标记为 unstable 而非 failed"。Declarative 的 `post {}` 不够灵活，必须 Scripted 的 `try-catch-finally`。

---

## 分层选型：顶层 Declarative + 底层 Scripted

游戏团队最常见的折中方案：**Jenkinsfile 用 Declarative 当骨架，复杂逻辑下沉到 Shared Library 用 Scripted 写**。

```groovy
@Library('game-pipeline-lib') _

pipeline {
    agent { label 'unity-builder' }
    stages {
        stage('Setup') {
            steps {
                gameSetup()  // ← Shared Library 函数，内部 Scripted
            }
        }
        stage('Build') {
            steps {
                gameBuild(platforms: readPlatforms())
                // gameBuild 是 Scripted，能动态生成 parallel
            }
        }
    }
    post {
        always { gameCleanup() }
    }
}
```

读 Jenkinsfile 的人看到的是干净的 Declarative 结构（业务方易读、可视化好用）；复杂能力藏在 Shared Library 里，由专人维护。

**这是绝大多数游戏团队应该走的路径**——业务流水线是 Declarative，平台能力是 Scripted。Shared Library 设计详见 102。

---

## 选型决策表 + 迁移成本

### 决策表

| 你的场景 | 推荐 |
|---------|-----|
| 单产品、单平台、线性构建 | Declarative |
| 单产品、多平台（≤3 个，固定） | Declarative + matrix |
| 多产品（>3 个），平台静态 | Declarative + 共享 Jenkinsfile 模板 |
| 多产品 + 平台动态 + 配置外置 | Declarative 骨架 + Shared Library（Scripted 函数） |
| 流水线之间复杂依赖（K-of-N、状态机） | Scripted |
| 高级错误恢复、resume from arbitrary point | Scripted |
| 需要可视化 Blue Ocean 给老板看 | 优先 Declarative，复杂逻辑下沉 |

### 迁移成本（一个真实案例的时间线）

某团队从纯 Scripted（200 行 Jenkinsfile）迁移到"Declarative 骨架 + Shared Library"的真实时长：

| 阶段 | 时间 | 内容 |
|------|------|------|
| 评估 + 设计 | 1 周 | 决定哪些下沉到 Shared Library，哪些留在 Jenkinsfile |
| Shared Library 实现 | 2 周 | 抽象 5 个核心函数（setup / build / test / archive / cleanup） |
| 第一条流水线迁移 | 3 天 | 试点产品的 Jenkinsfile 改写 + 对照测试 |
| 全量迁移（10 条流水线） | 1 周 | 业务方协助验证 |
| 稳定期（修补隐藏 bug） | 2 周 | 边界场景陆续暴露 |
| **总计** | **6 周** | 1 个工程师全职投入 |

### 迁移陷阱

- **环境变量行为差异**：Declarative 的 `environment {}` 块和 Scripted 的 `withEnv {}` 在变量作用域上有微妙差异，迁移后要逐个验证
- **Post 块语义差异**：Declarative 的 `post { always {} }` 和 Scripted 的 `try { } finally { }` 不完全等价，特别是异常传播
- **Replay 配置丢失**：迁移时如果有人在原 Scripted 里 Replay 改过参数没回写，迁移后这些"隐式配置"会丢

---

## 文末导读

下一步进 [102 Shared Library 设计：抽象出可复用的游戏构建原语]——分层选型方案里"Shared Library"那一层具体怎么设计。

L3 面试官线读者：本篇决策表那一节是核心——它体现的是"在团队当前能力 vs 业务复杂度的张力下做出取舍"，不是哪个语法更高级的问题。
