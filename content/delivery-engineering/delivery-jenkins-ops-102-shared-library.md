---
date: "2026-04-27"
title: "Shared Library 设计：抽象出可复用的游戏构建原语"
description: 'Shared Library 不是放代码的杂物间。游戏团队的 Shared Library 设计成败，关键是抽象层级——什么算"原语"、什么不算，以及一个真实演进案例：从 0 个共享库到 3 层共享库的 18 个月。'
slug: "delivery-jenkins-ops-102-shared-library"
weight: 1573
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Shared Library"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 30
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
leader_pick: true
---

## 在本篇你会读到

- **Shared Library 是什么、不是什么** —— 边界先划清，避免变成"杂物间"
- **抽象层级的取舍** —— 太抽象 vs 太具体，两个真实反例
- **哪些原语值得下沉** —— License / 参数化 / 归档 / 平台检测
- **哪些原语不值得下沉** —— 业务逻辑、特化 SDK、临时脚本
- **演进路径** —— 从 0 个库到 3 层库的 18 个月真实时间线
- **维护成本与版本管理** —— Shared Library 自己怎么发版、怎么灰度

---

## Shared Library 是什么、不是什么

Jenkins Shared Library 是**一个 Git 仓库**，里面放可复用的 Pipeline 代码（Groovy 函数 + 类）。Jenkinsfile 里通过 `@Library('xxx')` 引用，调用其中的函数。

```
shared-library-repo/
├── vars/
│   ├── gameBuild.groovy        # 全局函数 gameBuild()
│   ├── gameSetup.groovy
│   └── gameCleanup.groovy
├── src/
│   └── com/yourteam/
│       ├── BuildContext.groovy # 类，给 vars/ 函数使用
│       └── PlatformDetector.groovy
└── resources/
    └── templates/
        └── build-script.sh.tpl  # 资源文件
```

**它不是什么：**

- 不是"通用 Jenkins 教程的 copy-paste 仓库"——里面的代码必须对你的团队有针对性
- 不是"杂物间"——所有不知道放哪的脚本不能塞进来
- 不是"业务流水线"——业务流水线在 Jenkinsfile 里，Shared Library 是底层能力
- 不是"替代 Jenkins 插件"——能用插件解决的不要自己写

划清这条边界，是后面所有设计的前提。

---

## 抽象层级的取舍：两个反例

设计 Shared Library 最容易踩的坑是**抽象层级失衡**。两个真实反例：

### 反例 1：抽象过度（"上帝函数"）

某团队抽出一个 `gameUniversalPipeline()` 函数，参数列表 27 个：

```groovy
gameUniversalPipeline(
    platform: 'iOS',
    config: 'Release',
    buildScript: 'build.sh',
    archivePattern: '*.ipa',
    notifyDingtalk: true,
    runTests: false,
    timeout: 120,
    retryCount: 0,
    license: 'pro',
    // ...再 17 个参数
)
```

调用方看不懂参数语义，没人改得动 Shared Library，最后所有团队又复制了一份"Jenkinsfile 实际版本"绕开它。**抽象失败的标志：调用方读不懂、维护方不敢改。**

### 反例 2：抽象不足（"瑞士军刀小钉子"）

另一个团队的 Shared Library 有 50 个小函数：`getUnityPath()`、`isWindowsAgent()`、`computeTimestamp()`、`echoStartTime()` ……每个函数 3-5 行。

```groovy
// 调用方 Jenkinsfile
def unityPath = getUnityPath()
def now = computeTimestamp()
echoStartTime(now)
def isWin = isWindowsAgent()
if (isWin) {
    runWindowsScript(unityPath, now)
} else {
    runUnixScript(unityPath, now)
}
```

调用方代码看起来"用了 Shared Library"，但其实没简化什么——50 个函数的语义负担让新人读 Jenkinsfile 反而更累。**抽象失败的另一个标志：函数粒度不构成有意义的"语言"。**

### 正确的抽象层级

中间状态：**抽出"游戏团队的构建动词"**——每个函数对应一个有业务语义的动作：

```groovy
gameSetup(productId: 'tophero')        // 解析产品配置 + 准备 workspace
gameBuild(platforms: ['iOS','Android']) // 多平台并行构建
gameArchive(includeSymbols: true)       // 归档产物 + 符号表
gameCleanup()                            // 释放 license + 清理中间产物
```

调用方读 Jenkinsfile 像读"故事大纲"——`setup → build → archive → cleanup`。每个函数的内部复杂度由 Shared Library 维护者承担，业务方不需要懂。

**判断抽象是否合适的标尺**：调用方代码读起来像不像"业务说人话"？是的就是合适的层级。

---

## 哪些原语值得下沉

游戏团队 Shared Library 里**值得下沉**的能力，按高 ROI 排序：

### 1. Unity License 管理

License 是稀缺资源 + 泄漏风险高 + 处理细节碎（详见 301）。这是 Shared Library 最高 ROI 的下沉对象——

```groovy
// vars/withUnityLicense.groovy
def call(Closure body) {
    try {
        unityActivate()
        body()
    } finally {
        unityReturnLicense()  // 关键：finally 保证释放
    }
}

// Jenkinsfile 用法
withUnityLicense {
    sh 'unity -batchmode -quit -executeMethod Build.iOS'
}
```

封装在 Shared Library 后，所有业务方流水线**自动**遵守"激活-释放"规范，不再依赖每个 Jenkinsfile 自己记得加 `post { always { } }`。

### 2. 参数化的真值表

游戏团队多产品的常见复杂度：每个产品有自己的 `build-config.json`，里面定义"哪些平台 / 哪些 SKU / 是否走特定渠道"。这部分逻辑下沉成 `loadProductConfig(productId)`：

```groovy
// vars/loadProductConfig.groovy
def call(String productId) {
    def configFile = "configs/${productId}.json"
    def raw = readJSON(file: configFile)
    return new BuildContext(
        productId: productId,
        platforms: raw.platforms,
        skuList: raw.sku,
        channels: raw.channels
    )
}
```

调用方拿到 `BuildContext` 对象后只关心业务，不关心配置文件格式（未来从 JSON 换 YAML，业务方零感知）。

### 3. 产物归档的标准化

包括"哪些目录 / 是否带符号表 / 命名规则 / 上传到哪"——这些是工程规约，不是业务关心的事：

```groovy
gameArchive(
    productId: ctx.productId,
    platform: 'iOS',
    includeSymbols: true,  // 默认 true，强制带符号表
    uploadToOSS: true
)
// 内部实现：archiveArtifacts + dSYM 命名规范化 + 上传 + 元数据写入版本表
```

详见 303。

### 4. 平台 Agent 检测与切换

哪些操作必须在 macOS Agent、哪些在 Linux、Windows 在哪——把这些规则写成 `runOnPlatform(platform) { body }` 而不是每个 Jenkinsfile 自己判断标签：

```groovy
runOnPlatform('iOS') {
    // 自动调度到 macOS agent + 检查 Xcode 版本
    sh 'xcodebuild ...'
}
```

### 5. 通用错误处理与告警

钉钉 / 飞书 / 邮件通知格式、失败等级判定、是否触发告警——这些治理逻辑下沉一次，不要每个 Jenkinsfile 都写一遍。

---

## 哪些原语**不**值得下沉

划清这条线和"值得下沉"同样重要，否则 Shared Library 会膨胀成"瑞士军刀小钉子"。

### 1. 业务逻辑

某产品要在打 iOS 包前清空一个特定 PlayerPrefs 键——这不是工程能力，是**业务约束**。下沉到 Shared Library 会污染所有产品；留在该产品的 Jenkinsfile 里就好。

### 2. 临时性的 hotfix 脚本

"修复 Unity 2022.3.10 一个 bug 的临时脚本"这种东西放进 Shared Library，6 个月后没人敢删（"会不会还有谁在用？"）。临时脚本就放在产品仓库的 `tools/` 下，过期就删。

### 3. 强平台特化的 SDK 集成

某 SDK 接入只有一两个产品用——下沉到 Shared Library 是负担，不下沉是正解。**判断标准**：是否被 ≥3 个产品用到？是的话再考虑。

### 4. 一次性数据迁移逻辑

"从旧 CDN 迁移到新 CDN" 这种一次性脚本，绝对不要进 Shared Library。

### 5. 业务方期望"自己改的"参数

如果某个参数业务方频繁要调（比如某产品的"是否启用 logging"），**它应该在产品 Jenkinsfile 里**，不应该在 Shared Library 里——下沉了反而让业务方每次要改都要找 Shared Library 维护者。

---

## 演进路径：从 0 个库到 3 层库的 18 个月

一个真实游戏团队的演进时间线：

### 0-3 个月：每个产品一份 Jenkinsfile

3 个产品都有自己的 Jenkinsfile，互相 copy-paste。每改一次构建逻辑要改 3 处。**没有 Shared Library 是对的——还没有足够多的重复来证明值得抽象。**

### 3-6 个月：第一层 `pipeline-utils-lib`

发现 80% 的 Jenkinsfile 在做相同的事（License 激活、归档、通知）。抽出第一层 Shared Library，包含 5 个函数（`gameSetup`、`gameBuild`、`gameArchive`、`gameCleanup`、`notifyDingtalk`）。3 个 Jenkinsfile 长度从 200 行降到 30 行。

### 6-12 个月：第二层 `platform-toolkit-lib`

新增 Console 平台支持，发现 iOS / Android / Console 的差异不该污染顶层抽象。拆出第二层"平台工具包"——专门处理平台特化（Xcode 版本检测、Android Gradle 配置、Console SDK 注入）。第一层调用第二层。

### 12-18 个月：第三层 `monitoring-lib`

监控系统接入复杂化（钉钉 + 飞书 + 邮件 + 内部告警平台 + Crashlytics）。拆出第三层专门处理"通知与监控"，第一层调用第三层。

### 三层结构的最终形态

```
gameBuild()  (vars/, 第一层)
  ├─ withUnityLicense() (vars/, 第一层)
  ├─ runOnPlatform('iOS') { ... } (vars/, 第二层)
  │     └─ xcodeBuild(...) (src/, 第二层)
  └─ notifyOnFailure() (vars/, 第三层)
        └─ DingtalkClient (src/, 第三层)
```

**关键观察：抽象不是一开始设计好的，是被真实的复杂度压出来的**。前期不要预设三层结构——会过度设计。让重复出现 3 次以上再下沉。

---

## 维护成本与版本管理

Shared Library 自己也是代码，也需要工程化对待。

### 版本固定：一律带版本号

Jenkinsfile 引用 Shared Library 时**必须固定版本**：

```groovy
@Library('game-pipeline-lib@v1.4.2') _   // ✅ 固定版本
@Library('game-pipeline-lib@main') _     // ❌ 永远跟 main，会被无声破坏
```

否则某个产品的 Jenkinsfile 跑了半年没改一行代码，结果突然挂了——因为 Shared Library 的 `main` 分支昨天合了一个改动。

### 灰度发版

Shared Library 自己也要有灰度策略：

1. 在 Shared Library 仓库里打 `v1.5.0-rc1` 标签
2. 一个非关键流水线（比如内部工具流水线）改用 `v1.5.0-rc1`
3. 跑 1-2 周，确认无问题
4. 打正式 `v1.5.0` 标签
5. 业务流水线分批升级（不要全员同时升级）

### 测试是难题

Shared Library 的代码本身怎么测？三种渐进方案：

- **方案 1（最低成本）**：开个"sandbox" Jenkins job，每次 Shared Library 改完跑一遍 sandbox 流水线
- **方案 2（中等）**：用 [JenkinsPipelineUnit](https://github.com/jenkinsci/JenkinsPipelineUnit) 做单元测试
- **方案 3（高成本）**：搭一个"集成测试 Jenkins 实例"专门跑 Shared Library 的回归

游戏团队规模通常方案 1 够用；方案 2 在 Shared Library 复杂度上来后值得引入；方案 3 只有 50+ 流水线规模才必要。

### 维护人员配置

Shared Library 是构建工程师的"第二份产品"——它需要：

- 一个 owner（决定下沉策略、决定版本节奏）
- 至少 1 个 reviewer（保证 PR 不是单点）
- 文档（README + 函数级别注释，业务方读得懂）

**没有 owner 的 Shared Library 会迅速腐烂**——这是真实观察。

---

## 文末导读

下一步进 [103 多产品矩阵：一套模板支撑 N 个产品线]——本篇讲的"参数化原语"在多产品场景下的具体应用。

L3 面试官线读者：本篇核心是"抽象层级"那一节——好的工程抽象不是越多越好，而是层级合适、动词清晰、调用方读得懂。
