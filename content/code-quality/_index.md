---
title: "Code Quality"
description: "讨论代码质量如何影响协作效率、演进速度和线上稳定性，而不是把整洁当成抽象审美。"
hero_title: "代码质量不是风格偏好，而是协作成本、演进速度和稳定性的约束。"
---

这里不再只放一篇“观点文”，而是已经开始把代码质量一路接到工程质量。

如果把这条线压成一句话，我更愿意这样说：

`代码质量决定团队还能不能持续、安全、低成本地修改系统；工程质量则负责把这种能力接进测试、CI、发布和线上观察闭环。`

## 推荐先读

- [代码质量不是洁癖，而是交付能力]({{< relref "code-quality/code-quality-is-delivery-capability.md" >}})
- [软件工程基础与 SOLID 原则系列索引｜先看代码为什么腐化，再看设计原则怎样落回重构判断]({{< relref "engine-notes/software-engineering-solid-series-index.md" >}})

如果你想先把“代码为什么会越来越难改”这条主线看完整，最稳的顺序是：

1. 入口文：代码质量到底在保护什么
2. `SW-01` 到 `SW-02`：代码腐化、耦合与内聚
3. `SW-03` 到 `SW-09`：SOLID 五条原则和完整重构案例
4. `SW-10` 到 `SW-13`：辅助原则、代码气味和测试保护下的重构

## 第一批桥梁文

这三篇是把“代码怎么更好改”往“团队怎么更稳交付”接过去的第一批桥梁文：

- [代码评审真正该拦什么，不该拦什么]({{< relref "code-quality/code-review-what-to-block.md" >}})
- [什么问题必须做成自动检查，不能靠人盯]({{< relref "code-quality/what-must-be-automated-checks.md" >}})
- [测试保护下的最小重构路径]({{< relref "code-quality/minimal-refactoring-path-with-test-protection.md" >}})

如果你现在最关心的是“团队流程里到底哪里该靠人，哪里该靠工程护栏”，建议按上面这个顺序读。

## 质量护栏主线

这 6 篇把“自动化测试、AI review、职责切分和 Quality Gate”接成了一条连续主线：

- [自动化测试不是写几个单测，而是质量门禁]({{< relref "code-quality/automated-testing-is-quality-gate.md" >}})
- [哪些问题该靠自动化测试拦，哪些问题不该]({{< relref "code-quality/what-should-be-caught-by-automated-tests.md" >}})
- [游戏 / 客户端项目最值得先自动化的 5 类验证]({{< relref "code-quality/top-5-validations-for-game-client-projects.md" >}})
- [AI code review 应该抓什么，不应该抓什么]({{< relref "code-quality/ai-code-review-what-to-catch.md" >}})
- [AI review、人类 review、CI 怎样分工]({{< relref "code-quality/ai-review-human-review-and-ci-roles.md" >}})
- [Quality Gate：怎样把自动化测试和 AI 评审接进发布流程]({{< relref "code-quality/quality-gate-how-to-connect-automation-and-ai-review.md" >}})

如果你现在关心的是“怎样把质量从口头共识变成流程护栏”，最稳的顺序就是按上面 6 篇往下读：先立边界，再定优先级，再讲 AI 位置，最后把职责和流程收口。

## 如果你更关心工程质量怎么落地

下面这些文章更接近“怎样把质量变成工程护栏”：

- [Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI]({{< relref "code-quality/baseline-budgets-in-ci.md" >}})
- [灰度发布：分批放量策略（1% → 10% → 全量）、关键指标监控窗口、自动回滚触发条件]({{< relref "code-quality/gradual-rollout-and-automatic-rollback.md" >}})
- [线上质量监控：崩溃率 / ANR / 卡顿的实时监控告警、版本健康看板、问题响应 SLA]({{< relref "code-quality/online-quality-monitoring-and-version-health-dashboard.md" >}})
- [热修复流程：紧急修复的快速验证通道、Cherry-Pick 策略、回滚预案与演练]({{< relref "code-quality/hotfix-flow-fast-verification-cherry-pick-and-rollback.md" >}})
- [Bug 管理与 Playtest：P0/P1/P2 严重级别定义、Bug 生命周期、结构化测试用例设计]({{< relref "code-quality/bug-management-and-playtest.md" >}})
- [兼容性测试策略：设备矩阵划定优先级、Device Farm（AWS / Firebase Test Lab）、驱动差异检测]({{< relref "code-quality/compatibility-testing-device-matrix-and-device-farm.md" >}})
- [视觉质量测试：感知差异（Perceptual Diff）、渲染 Artifact 自动检测、多平台视觉一致性]({{< relref "code-quality/visual-quality-testing-layered-diff-rules-and-ai-review.md" >}})
- [代码与资产质量工具：静态分析（Roslyn Analyzer / Clang-Tidy）、资产导入时自动规范校验]({{< relref "code-quality/static-analysis-and-asset-quality-tools.md" >}})
- [单元测试基础：游戏代码的可测试性设计，如何解耦才能写测试]({{< relref "code-quality/testability-design-for-game-code.md" >}})
- [游戏项目自动化构建：Jenkins / GitHub Actions 打包流水线]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}})
- [从规范到护栏：命名、目录边界、配置契约、脚手架分别解决什么]({{< relref "code-quality/from-conventions-to-guardrails.md" >}})
- [变体、资源、热更链路怎样接进 CI]({{< relref "code-quality/variants-assets-and-hot-update-in-ci.md" >}})
- [烟测与回归：从构建产物验证到关键路径回归的最小链路]({{< relref "code-quality/smoke-tests-and-regression-minimal-chain.md" >}})
- [Unity 资源交付工程实践：分组、命名、版本、缓存、回滚和烟测基线]({{< relref "engine-notes/unity-resource-delivery-engineering-practices-baseline.md" >}})
- [Unity 资源系统怎么做烟测和回归：从构建校验、入口实例化到 Shader 首载]({{< relref "engine-notes/unity-resource-system-smoketests-and-regression.md" >}})
- [Shader Variant 数量监控与 CI 集成：怎么把变体治理接入构建流程]({{< relref "engine-notes/unity-shader-variant-ci-monitoring.md" >}})
- [HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景]({{< relref "engine-notes/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})
- [CrashAnalysis 系列索引｜先立概念地图，再按平台和 Unity + IL2CPP 回查]({{< relref "engine-notes/crash-analysis-series-index.md" >}})
- [特效性能检查器案例]({{< relref "projects/vfx-checker-case.md" >}})

这些文章有些还散在 `engine-notes` 和 `projects` 里，但它们其实都服务同一条更大的工程质量主线。

## 这条线已经补到哪里

这一栏现在已经从“代码怎么更好改”走到了四层：

1. 先把代码质量和重构边界立住
2. 再把自动化测试、AI review 和 CI 的职责切开
3. 再把 Quality Gate、Baseline、静态分析和构建流水线接进团队流程
4. 再把灰度发布、线上质量监控、热修复、Bug 闭环、兼容性策略和视觉质量判断接到版本健康闭环
5. 再把规范、资源链路和烟测 / 回归收成长期可执行的工程护栏

也就是说，当前已经不只是“代码写得好不好”，而是在回答“团队怎样更稳地合并、构建、发版、回滚、热修、验证、判图和盯版本健康”。

## 接下来更适合补什么

下一批更值得往下补的是：

- 包体大小监控：资产分析、Bundle 大小告警、贡献度追踪
- 包体大小优化：IL2CPP Managed Stripping、Split APK/AAB、iOS On-Demand Resources、资产精简策略
- Crash 上报与分析：Firebase Crashlytics / Bugly 接入，崩溃归因

也就是说，这一栏已经从“代码怎么更好改”走到了“哪些判断该变成护栏、这些护栏怎么进流程、预算怎样进 CI、版本怎样在线上保持健康”；下一步更适合继续补“包体与构建成本怎样长期可视化、跨平台分发优化怎样形成策略、Crash 上报和归因怎样真正接回发布与回溯闭环”。
