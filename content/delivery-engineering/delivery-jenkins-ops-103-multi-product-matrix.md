---
date: "2026-04-27"
title: "多产品矩阵：一套模板支撑 N 个产品线"
description: '多产品流水线不是从第一天就该上参数化。从硬编码到参数化到模板组合的三阶段演进，每个阶段都有适用规模和切换信号——以及"过度抽象"在游戏团队的真实代价。'
slug: "delivery-jenkins-ops-103-multi-product-matrix"
weight: 1574
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Pipeline"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 40
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **多产品矩阵的真实面貌** —— 不是"一套模板搞定一切"，而是有结构的差分管理
- **阶段 1：硬编码** —— 一产品一模板，1-3 个产品时是对的
- **阶段 2：参数化** —— 一模板 N 产品，4-10 个产品的最佳折中
- **阶段 3：模板组合** —— 基础 + 差分，10+ 产品才需要
- **过度抽象的真实代价** —— 一个团队跳级的故事
- **切换信号** —— 什么数据点告诉你"该升级到下一阶段了"

---

## 多产品矩阵的真实面貌

游戏团队的"多产品"通常长这样：

- **同公司多个游戏**（TopHero、SGI、Zuma 同一团队维护）—— 引擎版本可能不同、构建流程相似
- **同一游戏多个 SKU**（国际版 / 国内版 / 渠道版）—— 引擎一致、配置和资源不同
- **同一 SKU 多个分支**（main / release / hotfix）—— 完全相同流水线，输入不同
- **多产品 + 多 SKU + 多分支**（典型大团队）—— 笛卡尔积可达上百条流水线

"一套模板支撑 N 个产品线"说起来是终极目标，但**直接上来就追求这个会过度抽象**。多产品矩阵的真相是：**先建立差分，再决定哪些差分值得统一**。

三阶段演进：

```
阶段 1：硬编码（1-3 个产品）
    → 每个产品独立 Jenkinsfile，互不干扰

阶段 2：参数化（4-10 个产品）
    → 共享 Jenkinsfile 模板，差异通过参数注入

阶段 3：模板组合（10+ 个产品）
    → 基础流水线 Library + 产品级差分文件
```

每个阶段都有适用规模、切换信号、和过度抽象的代价。

---

## 阶段 1：硬编码（1-3 个产品）

每个产品有自己独立的 `Jenkinsfile`，互相 copy-paste，没有 Shared Library。

```
projectA/Jenkinsfile         # 200 行，自给自足
projectB/Jenkinsfile         # 200 行，从 A 复制改
projectC/Jenkinsfile         # 200 行，从 B 复制改
```

### 适用规模

1-3 个产品。**这阶段不要急着抽象**——抽象需要"多个实例的共性"作为依据，3 个以下样本不足以辨认共性。

### 这阶段的"对的事"

- **不开 Shared Library**，让每个 Jenkinsfile 自给自足
- **重复是允许的**，重复 3 次后再考虑抽象
- **每个产品的特化逻辑直接写在 Jenkinsfile 里**（比如 productA 要在打包前清 PlayerPrefs），不要预先抽象

### 这阶段的常见错误

- 为了"以后可能用得上"提前抽象 → 抽象错方向，6 个月后还是要重写
- 把所有产品的特化都搬到一个"超级模板"里 → 业务方读不懂
- 引入 Shared Library 但只有 1-2 个函数 → 维护成本高于收益

### 切换信号

满足**任意 2 条**就该升级到阶段 2：

- 产品数已达 4 个，且未来 6 个月会再加 2 个
- 修改一个构建参数（比如 Unity 版本号）需要改 ≥3 个 Jenkinsfile
- 看到工程师在不同 Jenkinsfile 之间频繁 copy-paste 同一段代码
- 出现"A 产品的 fix 忘记同步到 B 产品"的事故

---

## 阶段 2：参数化（4-10 个产品）

共享一套 Jenkinsfile 模板，产品差异通过参数注入。

### 形态 A：共享 Jenkinsfile + 产品配置文件

每个产品仓库根目录放一个 `build-config.json`：

```json
{
  "productId": "tophero",
  "platforms": ["iOS", "Android"],
  "unityVersion": "2022.3.21f1",
  "channels": ["AppStore", "GooglePlay", "TapTap"],
  "preBuildHooks": ["clear_player_prefs"]
}
```

Jenkinsfile 是统一模板（可以从 Shared Library 拉，也可以让每个产品都引用同一个 `template.jenkins`）：

```groovy
@Library('game-pipeline-lib@v1.4.0') _

pipeline {
    agent { label 'unity-builder' }
    stages {
        stage('Load Config') {
            steps {
                script {
                    ctx = loadProductConfig()
                }
            }
        }
        stage('Build') {
            steps { gameBuild(ctx) }
        }
        stage('Archive') {
            steps { gameArchive(ctx) }
        }
    }
    post {
        always { gameCleanup(ctx) }
    }
}
```

### 形态 B：参数化 Jenkinsfile（不推荐用作主线）

通过 `parameters {}` 让人工触发时填参数。

```groovy
parameters {
    choice(name: 'PRODUCT_ID', choices: ['tophero', 'sgi', 'zuma'])
    choice(name: 'PLATFORM', choices: ['iOS', 'Android'])
}
```

**问题**：这种方式适合"偶尔手动跑"的特殊任务，**不适合主线**——因为每次构建依赖人填对参数，无法自动触发。

### 适用规模

4-10 个产品。**这是大多数游戏团队的稳定状态**，不一定要继续往阶段 3 走。

### 这阶段的常见错误

#### 错误 1：参数列表无限膨胀

随着产品多样化，参数列表从 5 个涨到 30 个：

```json
{
  "productId": "...",
  "platforms": [...],
  "unityVersion": "...",
  "il2cppVersion": "...",
  "isDebug": false,
  "enableLogging": false,
  "enableProfiler": false,
  "customDefines": [...],
  "preBuildHooks": [...],
  "postBuildHooks": [...],
  // ... 再 20 个
}
```

业务方加新产品要填 30 个字段——大半填错。**信号：参数表超过 15 个，该考虑拆分配置文件了**（比如把 hooks 拆成独立 `hooks.json`）。

#### 错误 2：参数语义和实际行为不一致

参数 `enableLogging: true` 在 productA 里是"打详细日志"，在 productB 里是"打 verbose 日志（更详细）"——同一个参数名两种含义。**根因**：阶段 1 硬编码时各产品自己定义的语义，阶段 2 抽象时没统一。

**修法**：进入阶段 2 时做一次"参数语义对齐"——所有产品的 `build-config.json` 字段必须有统一定义。

#### 错误 3：Shared Library 函数过度承担产品逻辑

`gameBuild(ctx)` 内部包含 `if (ctx.productId == 'tophero') { 特化逻辑 }`——把业务逻辑下沉到 Shared Library。

**问题**：6 个月后 Shared Library 里塞了几十个 `if-else`，新产品接入要改 Shared Library。
**修法**：业务特化通过 `hooks` 机制让产品仓库自己定义，Shared Library 只调度，不写产品逻辑。

```groovy
// Shared Library 的 gameBuild 内部
def hooks = ctx.preBuildHooks
hooks.each { hookName ->
    sh "scripts/hooks/${hookName}.sh"  // 产品仓库提供脚本
}
// Shared Library 不知道也不关心 hookName 是什么
```

### 切换信号

升级到阶段 3 的信号（满足 ≥2 条）：

- 产品数突破 10 个
- 参数文件已经拆成 3+ 个独立 JSON / YAML
- 出现"Shared Library 的某个函数有 5+ 产品特化分支"
- 团队希望某些产品独立演进（比如新引擎试点），但又不想完全脱离统一流水线

---

## 阶段 3：模板组合（10+ 产品）

基础流水线作为 Shared Library 的"骨架"，产品级差分作为"插件"组合进来。

### 设计形态：Pipeline Template + Product Plugin

```
shared-library/
├── vars/
│   ├── gamePipeline.groovy        # 顶层模板入口
│   └── gameStage_*.groovy          # 各 stage 标准实现
├── src/com/yourteam/
│   ├── PipelineTemplate.groovy
│   └── plugins/                    # 各种可选插件
│       ├── ChinaChannelPlugin.groovy
│       ├── ConsolePlatformPlugin.groovy
│       └── ...
```

每个产品的 Jenkinsfile：

```groovy
@Library('game-pipeline-lib@v2.0.0') _

gamePipeline {
    productId = 'tophero'
    plugins = ['ChinaChannel', 'Console']  // 该产品启用的能力
    customStages = [
        afterBuild: ['runUnitTests', 'runSmokeTests']
    ]
}
```

### 这阶段的关键设计

#### 1. 明确"骨架不变 + 插件注入"

骨架（基础流水线）的 stage 顺序是固定的：

```
Setup → Build → Validate → Archive → Cleanup
```

产品级特化通过插件注入到这些 stage 的扩展点：

```
Setup
  ├─ [pre-setup hooks]
  ├─ standard setup
  └─ [post-setup hooks]
Build
  ├─ [pre-build hooks]
  └─ ...
```

业务方不能改骨架的 stage 顺序——这是治理边界。能改的是 hooks 注入点。

#### 2. 插件 vs hooks 的边界

- **Hooks**：产品仓库的 shell 脚本，调度由 Shared Library 完成，逻辑在产品仓库
- **Plugins**：Shared Library 提供的"打包好的能力"，产品选择启用

经验法则：**业务逻辑用 hooks，工程能力用 plugins**。

#### 3. 测试矩阵管理

10+ 产品 × 多平台 × 多分支 = 几十到上百条流水线。这阶段需要明确：

- 哪些是"主流水线"（每次提交都跑）
- 哪些是"次流水线"（夜间跑）
- 哪些是"按需流水线"（人工触发）

否则 build farm 容量被"所有流水线全部跑"耗尽。

### 这阶段的代价

- **Shared Library 自己变成产品**——需要专人维护、版本管理（[详见 102]({{< relref "delivery-engineering/delivery-jenkins-ops-102-shared-library.md" >}})）
- **新人上手时间从 1 天涨到 1 周**——要看懂插件机制
- **错误信息更难定位**——出错可能在骨架、插件、hooks 任一层

### 适用规模

10+ 产品，且团队有 ≥1 个全职构建工程师维护 Shared Library。**没有专人维护就不要走到阶段 3**——会变成无人能改的黑箱。

---

## 过度抽象的真实代价

一个团队跳级的真实故事：4 个产品的团队，工程负责人觉得"以后会有 20 个产品，先把架构搭好"——直接上阶段 3。

### 半年后发生了什么

- Shared Library 写了 800 行 Groovy + 4 个 plugin
- 4 个产品 Jenkinsfile 都在 30 行以内（很漂亮）
- **但只有架构师本人能改 Shared Library**
- 业务方加新产品要等架构师有空（排期 2 周起）
- 出错时业务方 debug 要先理解骨架 + plugin 机制，调试时长从 30 分钟涨到 4 小时

### 一年后被迫降级

业务方失去耐心，开始绕开 Shared Library 自己写 Jenkinsfile——回到了阶段 1，但 Shared Library 还在那里没人删。**最终两条线并行：新产品走 Jenkinsfile 直写，老产品在过度抽象的 Shared Library 里挣扎。**

### 教训

- **复杂度要被业务推着走，不是被工程师拉着走**
- 阶段 2 的稳定状态可以维持很久——不是必须升级到阶段 3
- 升级阶段必须满足"团队规模能维护"——不是"我能写出来"

---

## 切换信号汇总

| 当前阶段 | 切换到下一阶段的信号（≥2 条） |
|---------|---------------------------|
| 阶段 1 | 4+ 产品；改一处要改 ≥3 个 Jenkinsfile；出现遗漏同步事故 |
| 阶段 2 | 10+ 产品；参数文件超过 15 字段；Shared Library 内部 if-else 超过 5 个产品特化 |

**反向信号（阶段 3 → 阶段 2）**：

- Shared Library 维护人离职 / 调岗
- 业务方开始绕过 Shared Library 直接改 Jenkinsfile
- 新人上手时间超过 2 周

满足任一条 → 主动降级，不要硬撑。

---

## 文末导读

下一步进 104 多分支流水线：Dev / QA / Release 自动化策略——本篇讲多产品维度，104 讲多分支维度，两者结合是真正的"流水线矩阵"。

L3 面试官线读者：本篇核心是"什么时候不该抽象"——大部分多产品流水线的故障来自过度抽象，不是来自抽象不足。
