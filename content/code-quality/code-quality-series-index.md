---
date: "2026-04-13"
title: "代码质量系列索引｜从 Code Review 到 CI 门禁到线上监控的完整质量链"
description: "给代码质量系列补一个总入口：Code Review 策略、自动化测试、CI/CD 管线、崩溃监控、灰度发布，以及质量意识如何融入日常开发流程。"
slug: "code-quality-series-index"
weight: 1
featured: false
tags:
  - "Code Quality"
  - "CI/CD"
  - "Testing"
  - "Index"
series: "Code Quality"
series_id: "code-quality"
series_role: "index"
series_order: 0
series_nav_order: 50
series_title: "Code Quality"
series_audience:
  - "客户端 / 服务端程序"
  - "技术主管"
series_level: "进阶"
series_best_for: "当你想把代码质量从个人习惯升级成团队可执行的工程链路"
series_summary: "把代码质量从 Code Review、自动化测试、CI 门禁、崩溃监控到灰度发布串成一条可落地的质量链"
series_intro: "这组文章不是在讲代码风格指南，而是在回答：怎样让质量成为工程流水线的一部分，而不是靠人盯。从 Code Review 该拦什么不该拦什么，到自动化测试怎么设计门禁，到崩溃监控和灰度发布怎么闭环。"
series_reading_hint: "如果你是技术主管，建议从质量门禁和 CI 相关文章开始；如果你是开发者，从 Code Review 和可测试性设计开始。"
---
> 代码质量真正难的地方，不是某个人不愿意写好代码，而是整条链路里没有一个位置在自动拦截。

这是代码质量系列第 0 篇。
它不讲新的技术细节，只做一件事：

`给这组文章补一个稳定入口，让读者知道先看哪篇、遇到什么问题该回看哪篇。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 这组文章把代码质量拆成了哪些可落地的工程环节。
2. 如果按主题分组看，各组解决的是什么层次的问题。
3. 如果不是系统读，而是项目里遇到了具体痛点，该先看哪篇。
4. 整条质量链从左到右是怎样串起来的。

## 先给一句总判断

如果先把整个系列压成一句话，我会这样描述：

`代码质量不是一种审美，而是一条从提交前到线上运行的工程链路；这组文章的任务，就是把这条链按阶段拆开，再在项目层重新收回来。`

所以这组文章不是按知识点平铺，而是按阶段拆：

- 提交前：Code Review 和 AI Review 各自该做什么
- 代码层：规范、护栏、可测试性设计
- 验证层：自动化测试、烟测、回归、视觉质量
- 构建层：CI 管线、门禁、Baseline 预算
- 发布层：灰度、热修复、平台认证
- 线上层：崩溃监控、质量看板、数据分析
- 工具层：静态分析、调试工具、包体监控

---

### 一、质量意识与工程起点

这组文章回答的是最前置的问题：代码质量到底在解决什么，以及什么东西该靠人、什么该靠机器。

- [代码质量不是洁癖，而是交付能力]({{< relref "code-quality/code-quality-is-delivery-capability.md" >}})
  把代码质量放回协作、演进和交付语境里看。

- [什么问题必须做成自动检查，不能靠人盯]({{< relref "code-quality/what-must-be-automated-checks.md" >}})
  划清人和机器的边界：重复的、确定的、可枚举的检查，不要靠 review 和口头约定。

- [从规范到护栏：命名、目录边界、配置契约、脚手架分别解决什么]({{< relref "code-quality/from-conventions-to-guardrails.md" >}})
  规范文档不够，要把高频协作错误前移成默认路径和自动约束。

### 二、Code Review 与 AI Review

这组文章拆清 Code Review 该拦什么不该拦什么，以及 AI 在这条链里应该扮演什么角色。

- [代码评审真正该拦什么，不该拦什么]({{< relref "code-quality/code-review-what-to-block.md" >}})
  优先拦结构问题和回归风险，不要把时间耗在细碎风格上。

- [AI code review 应该抓什么，不应该抓什么]({{< relref "code-quality/ai-code-review-what-to-catch.md" >}})
  AI 做第一轮风险扫描，不做最终裁决。

- [AI review、人类 review、CI 怎样分工]({{< relref "code-quality/ai-review-human-review-and-ci-roles.md" >}})
  三者分开：AI 扫风险，人裁决上下文，CI 硬拦已确定规则。

- [Quality Gate：怎样把自动化测试和 AI 评审接进发布流程]({{< relref "code-quality/quality-gate-how-to-connect-automation-and-ai-review.md" >}})
  Quality Gate 不是 checklist，而是一组阻断点。

### 三、自动化测试与可测试性

这组文章回答的是测试到底该测什么、怎样分层、怎样让代码本身变得可测。

- [自动化测试不是写几个单测，而是质量门禁]({{< relref "code-quality/automated-testing-is-quality-gate.md" >}})
  把编译、lint、契约校验、smoke、regression 和 baseline 变成门禁链。

- [哪些问题该靠自动化测试拦，哪些问题不该]({{< relref "code-quality/what-should-be-caught-by-automated-tests.md" >}})
  优先拦最值钱、最可重复、最容易回归的那部分。

- [游戏 / 客户端项目最值得先自动化的 5 类验证]({{< relref "code-quality/top-5-validations-for-game-client-projects.md" >}})
  资源有限时，优先把最贵的 5 类验证做成门禁。

- [单元测试基础：游戏代码的可测试性设计，如何解耦才能写测试]({{< relref "code-quality/testability-design-for-game-code.md" >}})
  可测试性不是为了单测而抽象，而是把决策逻辑和副作用边界拆开。

- [自动化测试：Unity Test Framework，Play Mode 测试，性能回归测试]({{< relref "code-quality/automated-testing-unity-test-framework-playmode-and-performance-regression.md" >}})
  EditMode、Play Mode 和性能回归分别放回它们最值钱的位置。

- [测试保护下的最小重构路径]({{< relref "code-quality/minimal-refactoring-path-with-test-protection.md" >}})
  先记录行为，再缩小改动面，再让验证跟着每一步走。

- [烟测与回归：从构建产物验证到关键路径回归的最小链路]({{< relref "code-quality/smoke-tests-and-regression-minimal-chain.md" >}})
  把构建产物验证、关键入口烟测和发布前回归收成最小验证链。

### 四、CI/CD 管线与构建门禁

这组文章回答的是构建流程怎样把质量规则硬接进去，而不是留成口头约定。

- [游戏项目自动化构建：Jenkins / GitHub Actions 打包流水线]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}})
  真正的流水线把输入、环境、生成步骤、产物和失败责任都固定下来。

- [Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI]({{< relref "code-quality/baseline-budgets-in-ci.md" >}})
  Baseline 不是感觉，而是固定样本、明确阈值和超线动作。

- [变体、资源、热更链路怎样接进 CI]({{< relref "code-quality/variants-assets-and-hot-update-in-ci.md" >}})
  把 shader variant、资源快照和热更产物变成构建里的显式门禁。

- [代码与资产质量工具：静态分析（Roslyn Analyzer / Clang-Tidy）、资产导入时自动规范校验]({{< relref "code-quality/static-analysis-and-asset-quality-tools.md" >}})
  把代码规则和资产契约前移到编译期、导入期和构建期。

### 五、发布与灰度

这组文章回答的是版本怎样安全地到达用户手里，以及出问题了怎样最快收回来。

- [灰度发布：分批放量策略（1% → 10% → 全量）、关键指标监控窗口、自动回滚触发条件]({{< relref "code-quality/gradual-rollout-and-automatic-rollback.md" >}})
  灰度不是"慢一点发版"，而是受控放量加明确回滚触发条件。

- [热修复流程：紧急修复的快速验证通道、Cherry-Pick 策略、回滚预案与演练]({{< relref "code-quality/hotfix-flow-fast-verification-cherry-pick-and-rollback.md" >}})
  缩短验证路径，同时不放弃版本边界和回写主干的工程流程。

- [主机平台认证流程：TRC / XR 认证要求概览，常见不通过原因]({{< relref "code-quality/platform-certification-trc-xr-overview.md" >}})
  把 suspend/resume、用户切换、存档等高风险要求前移成发布前的固定验证链。

### 六、线上监控与质量闭环

这组文章回答的是版本上线之后，怎样持续看到质量状态并闭环问题。

- [Crash 上报与分析：Firebase Crashlytics / Bugly 接入，崩溃归因]({{< relref "code-quality/crash-reporting-crashlytics-bugly-and-symbolication.md" >}})
  把崩溃采集、符号化、版本映射、聚类归因和告警响应接成事故处理链。

- [线上质量监控：崩溃率 / ANR / 卡顿的实时监控告警、版本健康看板、问题响应 SLA]({{< relref "code-quality/online-quality-monitoring-and-version-health-dashboard.md" >}})
  把崩溃率、ANR、卡顿和版本健康做成持续可见、可分诊、可响应的机制。

- [游戏分析接入：埋点 SDK 集成、事件上报规范、隐私合规（GDPR/COPPA）]({{< relref "code-quality/game-analytics-integration-event-schema-and-privacy.md" >}})
  先把业务问题、事件契约、版本上下文和隐私边界固定下来。

### 七、设备兼容与视觉质量

这组文章回答的是怎样在多设备、多平台的现实条件下保证视觉和兼容性质量。

- [兼容性测试策略：设备矩阵划定优先级、Device Farm（AWS / Firebase Test Lab）、驱动差异检测]({{< relref "code-quality/compatibility-testing-device-matrix-and-device-farm.md" >}})
  按风险和收益划定设备矩阵，再把 Device Farm 和真机各自放回合适的位置。

- [机型分档怎样验证：设备矩阵、Baseline、截图回归与错配排查]({{< relref "code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md" >}})
  把设备矩阵、固定 baseline、截图回归和错配排查收成分档验证链。

- [视觉质量测试：感知差异（Perceptual Diff）、渲染 Artifact 自动检测、多平台视觉一致性]({{< relref "code-quality/visual-quality-testing-layered-diff-rules-and-ai-review.md" >}})
  把规则检测、图像差异、AI 语义复核和人工裁决分层放置。

### 八、包体治理与工程工具

这组文章回答的是包体怎样持续监控，以及调试和 Bug 管理怎样成为工程流程。

- [包体大小优化：IL2CPP Managed Stripping、Split APK/AAB、iOS On-Demand Resources、资产精简策略]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}})
  先分清代码、原生库、资源和分发层的成本结构。

- [包体大小监控：资产分析、Bundle 大小告警、贡献度追踪]({{< relref "code-quality/package-size-monitoring-bundle-alerts-and-contribution-tracking.md" >}})
  把异常增长、关键 bundle 变化和模块贡献度持续接进 CI。

- [Bug 管理与 Playtest：P0/P1/P2 严重级别定义、Bug 生命周期、结构化测试用例设计]({{< relref "code-quality/bug-management-and-playtest.md" >}})
  把严重级别、生命周期、复现信息和验证责任固定下来。

- [游戏内调试工具系统：控制台命令（Console System）、Debug Overlay、开发者菜单的规范实现]({{< relref "code-quality/in-game-debug-tools-console-overlay-and-developer-menu.md" >}})
  把版本、资源、热更、性能和关键状态可见化、可控制、可复现。

---

## 如果你不是系统读，而是带着问题来查

如果你已经在项目里遇到具体痛点，那比起从头读，更稳的是按问题回看。

### 1. 你想知道 Code Review 到底该拦什么，每次 review 不知道重点在哪

先看：

- [代码评审真正该拦什么，不该拦什么]({{< relref "code-quality/code-review-what-to-block.md" >}})

再看：

- [AI review、人类 review、CI 怎样分工]({{< relref "code-quality/ai-review-human-review-and-ci-roles.md" >}})

### 2. 你想搭自动化测试但不知道该测什么，或者代码太耦合根本写不了测试

先看：

- [哪些问题该靠自动化测试拦，哪些问题不该]({{< relref "code-quality/what-should-be-caught-by-automated-tests.md" >}})

再看：

- [单元测试基础：游戏代码的可测试性设计，如何解耦才能写测试]({{< relref "code-quality/testability-design-for-game-code.md" >}})

### 3. 你想把 CI 从只跑编译升级成真正的质量门禁

先看：

- [Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI]({{< relref "code-quality/baseline-budgets-in-ci.md" >}})

再看：

- [游戏项目自动化构建：Jenkins / GitHub Actions 打包流水线]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}})

### 4. 你的版本上线后崩溃率升高，但不知道从哪里开始排查

先看：

- [Crash 上报与分析：Firebase Crashlytics / Bugly 接入，崩溃归因]({{< relref "code-quality/crash-reporting-crashlytics-bugly-and-symbolication.md" >}})

再看：

- [线上质量监控：崩溃率 / ANR / 卡顿的实时监控告警、版本健康看板、问题响应 SLA]({{< relref "code-quality/online-quality-monitoring-and-version-health-dashboard.md" >}})

### 5. 你想做灰度发布但不知道该观察什么指标、什么时候该回滚

先看：

- [灰度发布：分批放量策略（1% → 10% → 全量）、关键指标监控窗口、自动回滚触发条件]({{< relref "code-quality/gradual-rollout-and-automatic-rollback.md" >}})

再看：

- [热修复流程：紧急修复的快速验证通道、Cherry-Pick 策略、回滚预案与演练]({{< relref "code-quality/hotfix-flow-fast-verification-cherry-pick-and-rollback.md" >}})

### 6. 你的包体越来越大，但不知道是谁贡献的，也没有门禁在拦

先看：

- [包体大小监控：资产分析、Bundle 大小告警、贡献度追踪]({{< relref "code-quality/package-size-monitoring-bundle-alerts-and-contribution-tracking.md" >}})

再看：

- [包体大小优化：IL2CPP Managed Stripping、Split APK/AAB、iOS On-Demand Resources、资产精简策略]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}})

### 7. 你想把质量从个人习惯升级成团队工程链路，但不知道该从哪开始

先看：

- [代码质量不是洁癖，而是交付能力]({{< relref "code-quality/code-quality-is-delivery-capability.md" >}})

再看：

- [什么问题必须做成自动检查，不能靠人盯]({{< relref "code-quality/what-must-be-automated-checks.md" >}})

---

## 收束

这组文章最重要的价值，不是把工具和方法论列齐，而是帮你看清一件事：

代码质量从来不是某个人的洁癖，也不是某个阶段的一次性投入。它是一条从提交前到线上运行的连续链路。Code Review 拦不住的，交给自动化测试；测试拦不住的，交给 CI 门禁；门禁拦不住的，交给灰度和监控。

如果链条断在某一层，所有压力都会堆到下一层。把每一层该拦的东西放回原位，就是这组文章在做的事。
