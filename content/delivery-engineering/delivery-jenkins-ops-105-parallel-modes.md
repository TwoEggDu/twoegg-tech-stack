---
date: "2026-04-27"
title: "Jenkins 下的并行模式：Parallel / Matrix / Triggers 实战"
description: '扇入扇出在 Jenkins 里有三种实现：parallel 块、matrix 轴、build job 触发——能力和代价完全不同。游戏团队最常踩的坑是"假并发"和"失败传播失控"。'
slug: "delivery-jenkins-ops-105-parallel-modes"
weight: 1576
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Parallel"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 60
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **三种并行的语义层次** —— 同进程 / 跨 Agent / 跨流水线
- **parallel 块** —— stage 级并行的能力与陷阱
- **matrix 轴** —— 参数笛卡尔积的适用边界
- **build job 触发** —— 跨流水线扇入扇出
- **失败隔离** —— failFast / catchError / propagate 的真值表
- **真实场景的组合方案** —— 一个三层并行的游戏团队示例

---

## 三种并行的语义层次

Jenkins 提供三种并行机制，对应三个不同的语义层次：

| 机制 | 并行单位 | 是否跨 Agent | 失败隔离能力 | 适用场景 |
|------|---------|------------|------------|---------|
| `parallel { }` | stage | 可以（每个 stage 单独 agent） | 中（failFast / catchError） | 同流水线内多任务并行 |
| `matrix { }` | axis 笛卡尔积 | 可以 | 弱（matrix 内部失败语义粗） | 平台 × 配置的笛卡尔积 |
| `build job:` + `parallel` | 整条子流水线 | 必然跨 | 强（每条子流水线独立） | 多产品 / 跨流水线 |

**关键认知**：选哪种不是凭"哪个语法更短"，是凭"失败隔离粒度"和"是否需要跨 Agent"。

---

## parallel 块：stage 级并行

最常见的并行机制，把 stages 包在 `parallel { }` 里：

```groovy
stage('Build All Platforms') {
    parallel {
        stage('iOS') {
            agent { label 'macos' }
            steps { sh 'build_ios.sh' }
        }
        stage('Android') {
            agent { label 'linux' }
            steps { sh 'build_android.sh' }
        }
        stage('WebGL') {
            agent { label 'linux' }
            steps { sh 'build_webgl.sh' }
        }
    }
}
```

### parallel 的能力

- **每个 stage 可以指定独立 agent**——iOS 在 macOS、Android 在 Linux
- **stage 之间真正并行执行**（不是协程，是真线程）
- **每个 stage 独立日志和耗时统计**
- **失败可以选择 failFast 还是收集所有失败**

### 游戏团队最常踩的坑：假并发

```groovy
// 看起来"并行"，实际上踩坑
parallel {
    stage('iOS Build') {
        agent { label 'unity-builder' }   // ← 关键问题
        steps { sh 'build_ios.sh' }
    }
    stage('Android Build') {
        agent { label 'unity-builder' }   // ← 同一个标签
        steps { sh 'build_android.sh' }
    }
}
```

`unity-builder` 标签下只有 2 台 Agent，但同一台 Agent 上的两个 build 会**共用 workspace**——iOS / Android build 的 `Library/` 内容不同，互相 reimport，**总时长比串行还慢**。

**正解：** 平台特化的 Agent 标签 + workspace 隔离：

```groovy
parallel {
    stage('iOS') {
        agent { label 'macos-unity' }
        options { skipDefaultCheckout() }
        steps {
            ws("${env.WORKSPACE}-ios") {     // ← 显式独立 workspace
                checkout scm
                sh 'build_ios.sh'
            }
        }
    }
    stage('Android') {
        agent { label 'linux-unity' }
        options { skipDefaultCheckout() }
        steps {
            ws("${env.WORKSPACE}-android") {
                checkout scm
                sh 'build_android.sh'
            }
        }
    }
}
```

### parallel 的失败隔离

默认行为：**任一 stage 失败 → 整个 parallel 块标记失败 → 后续 stage 不跑**。

但失败是否立即终止其他 stages？两种模式：

```groovy
parallel(failFast: true) { ... }   // 任一失败立刻杀掉其他
parallel(failFast: false) { ... }  // 失败也让其他跑完（默认）
```

游戏团队的**典型选择**：

- 验证类并行（unit test / lint）：`failFast: true`，节省时间
- 构建类并行（多平台 build）：`failFast: false`，让所有平台都跑完，一起拿到失败报告

---

## matrix 轴：参数笛卡尔积

Declarative Pipeline 的 `matrix` 块表达"轴的笛卡尔积"：

```groovy
matrix {
    axes {
        axis {
            name 'PLATFORM'
            values 'iOS', 'Android', 'WebGL'
        }
        axis {
            name 'CONFIG'
            values 'Debug', 'Release'
        }
    }
    stages {
        stage('Build') {
            steps {
                sh "build.sh ${PLATFORM} ${CONFIG}"
            }
        }
    }
}
```

这个 matrix 会展开成 3 × 2 = 6 条并行任务：iOS-Debug / iOS-Release / Android-Debug / ...

### matrix 的适用边界

#### 适用场景

- 轴是**静态已知**的（写在 Jenkinsfile 里就不变）
- 所有组合执行**完全相同的 stages**（仅环境变量不同）
- 失败粒度可以接受到"某个组合失败，其他组合不影响"

#### 不适用场景

- 轴的值需要**运行时决定**（读 JSON 等外部数据）→ matrix 撑不住，要 Scripted
- 不同组合执行**不同的步骤**（iOS 要签名、Android 要对齐）→ matrix 内部分支会很丑，不如直接 parallel
- 笛卡尔积维度过多导致组合数爆炸（4 平台 × 3 配置 × 2 渠道 = 24 条并行）

### matrix 的隐藏陷阱

#### 陷阱 1：excludes 写不出复杂排除

`matrix` 支持 `excludes {}` 排除某些组合，但表达力有限：

```groovy
matrix {
    axes { ... }
    excludes {
        exclude {
            axis { name 'PLATFORM'; values 'WebGL' }
            axis { name 'CONFIG'; values 'Release' }
        }
    }
    // ...
}
```

只能"完全不要这一组合"，不能"WebGL 在 macOS Agent 上不跑、在 Linux Agent 上跑"——这种条件 excludes 写不出。

#### 陷阱 2：当一个组合失败，其他组合的状态不直观

matrix 失败的 UI 显示是"matrix 整体失败"——你要点进去才能看到具体哪个组合失败。Blue Ocean 视图也对 matrix 支持有限。

---

## build job 触发：跨流水线扇入扇出

最高级别的并行，跨 Pipeline 调度：

```groovy
stage('Trigger Sub-Pipelines') {
    steps {
        script {
            def builds = [:]
            builds['ios-pipeline'] = {
                build job: 'tophero-ios-build', wait: true, propagate: false
            }
            builds['android-pipeline'] = {
                build job: 'tophero-android-build', wait: true, propagate: false
            }
            parallel builds
        }
    }
}
```

### 跨流水线的优势

- **每条子流水线独立 history、独立产物归档、独立通知**
- **可以单独 retry 某条子流水线**而不影响其他
- **业务方可以单独触发 / 查看**某条子流水线
- **失败传播完全可控**（`propagate: false` 让父流水线决定怎么处理）

### 跨流水线的代价

- **总耗时增加**——每条子流水线有 setup 开销（checkout、setup、cleanup）
- **工件传递麻烦**——子流水线产物要回传给父流水线（通过 `copyArtifacts` 或外部存储）
- **依赖关系复杂**——多层嵌套时调试困难

### `wait: true` vs `wait: false`

```groovy
build job: 'X', wait: true   // 阻塞，等 X 跑完
build job: 'X', wait: false  // 异步触发，立刻返回
```

游戏团队**推荐 `wait: true`**——异步触发会让父流水线"看不到"子流水线的失败，调度可见性差。

### `propagate: true` vs `propagate: false`

```groovy
build job: 'X', wait: true, propagate: true   // 子失败 → 父也失败
build job: 'X', wait: true, propagate: false  // 子失败不传播，父决定怎么处理
```

`propagate: false` 的典型场景：父流水线触发 5 个子流水线，希望"任意 3 个成功就算整体成功"——父流水线收集所有结果再决策。

---

## 失败隔离的真值表

把上面三种机制的失败行为汇总：

| 场景 | 默认行为 | 怎么改 |
|------|---------|--------|
| `parallel` 内 stage 失败 | 整个 parallel 失败，其他 stage 继续跑完 | `failFast: true` 立刻终止其他 |
| `matrix` 某组合失败 | 整个 matrix 失败 | excludes 提前裁剪不可行的组合 |
| `build job` 子流水线失败 | 父流水线失败 | `propagate: false` 让父决定 |
| stage 内 step 失败 | stage 失败传播 | `catchError(buildResult: 'UNSTABLE') { ... }` |

### catchError 的妙用

游戏团队常见需求："iOS 构建失败，但 Android 成功——希望整体标记 unstable 而不是 failure"：

```groovy
parallel {
    stage('iOS') {
        steps {
            catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                sh 'build_ios.sh'
            }
        }
    }
    stage('Android') {
        steps { sh 'build_android.sh' }
    }
}
```

iOS 失败 → 该 stage 标记 FAILURE，但整体只是 UNSTABLE，业务方可以下载 Android 产物继续测试。

---

## 真实场景的组合方案：三层并行

游戏团队 production 流水线的典型形态——三层并行嵌套：

### 第 1 层：跨流水线（按产品）

主流水线触发各产品的子流水线：

```groovy
parallel {
    stage('TopHero') { build job: 'tophero-build', propagate: false }
    stage('SGI') { build job: 'sgi-build', propagate: false }
}
```

### 第 2 层：跨 Agent（按平台）

每个产品的子流水线内部，多平台并行：

```groovy
parallel {
    stage('iOS') {
        agent { label 'macos-unity' }
        steps { /* ... */ }
    }
    stage('Android') {
        agent { label 'linux-unity' }
        steps { /* ... */ }
    }
}
```

### 第 3 层：matrix（按配置）

每个平台内部，多配置笛卡尔积（Debug / Release × 多渠道）：

```groovy
matrix {
    axes {
        axis { name 'CONFIG'; values 'Debug', 'Release' }
        axis { name 'CHANNEL'; values 'AppStore', 'TapTap' }
    }
    stages {
        stage('Build') {
            steps { sh "build.sh ${CONFIG} ${CHANNEL}" }
        }
    }
}
```

### 总时长 vs 资源占用的权衡

三层并行后：

- 1 个产品 × 2 平台 × 4 配置 = 8 个并行任务 / 产品
- 5 个产品 = 40 并行任务

如果 build farm 只有 10 个 Agent，这 40 个任务会排队——**总时长可能比顺序构建还长**（因为 setup 开销 + 调度延迟）。

**关键决策点**：并行度和 Agent 容量必须匹配，盲目加并行会让 setup 开销吞掉所有收益。

---

## 文末导读

下一步进 201 Master 的三类瓶颈：连接、内存、磁盘 I/O——并行度上去之后，第一个崩的是 Master。

L3 面试官线读者：本篇核心是"三种并行机制的失败隔离粒度"——选并行机制不是选语法，是选治理边界。
