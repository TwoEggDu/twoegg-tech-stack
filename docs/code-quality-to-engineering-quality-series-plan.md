# 代码质量到工程质量系列计划

## 这份计划解决什么

这条线现在已经不再是一个单独的“代码质量观点栏目”。
它正在承担一条更完整的主线：

- 先解释代码质量到底在保护什么
- 再把 review、重构、自动化护栏这些桥梁文接起来
- 再把 Quality Gate、Baseline、发布质量和线上质量收成工程闭环

这份计划专门回答四件事：

1. 这条线到底在解决什么问题
2. 现在已经写到哪里了
3. 还缺哪些关键环节
4. 接下来最该补哪几篇

## 一句话定位

`代码质量不是风格洁癖，而是团队持续、安全、低成本修改系统的能力；工程质量则是把这种能力接进测试、CI、发布和线上观察闭环。`

## 目标读者

- 客户端 / 工具链 / TA / Tech Lead
- 已经写过一段时间业务代码，开始碰到多人协作、重构、回归、发布质量问题的工程师

## 系列边界

### 属于这条线的内容

- 设计边界、依赖方向、代码可接手性
- Code Review、重构、可测试性、自动化护栏
- Quality Gate、Smoke Test、Regression、Baseline
- Crash / ANR / 卡顿监控、灰度发布、回滚、版本健康

### 不属于这条线的内容

- 纯算法、纯渲染、纯引擎机制讲解，除非它直接服务质量判断
- 只停留在工具 API 使用说明、没有沉淀成方法论的零散教程
- 完全项目私有、无法抽象复用的事故复盘细节

## 现有内容资产

### 已写的入口与基础判断

- `content/code-quality/code-quality-is-delivery-capability.md`
- `content/engine-notes/software-engineering-solid-series-index.md`
- `content/engine-notes/solid-sw-01-why-game-code-rots.md` 到 `content/engine-notes/solid-sw-13-refactoring.md`

### 已写的桥梁文与质量护栏

- `content/code-quality/code-review-what-to-block.md`
- `content/code-quality/what-must-be-automated-checks.md`
- `content/code-quality/minimal-refactoring-path-with-test-protection.md`
- `content/code-quality/automated-testing-is-quality-gate.md`
- `content/code-quality/what-should-be-caught-by-automated-tests.md`
- `content/code-quality/top-5-validations-for-game-client-projects.md`
- `content/code-quality/ai-code-review-what-to-catch.md`
- `content/code-quality/ai-review-human-review-and-ci-roles.md`
- `content/code-quality/quality-gate-how-to-connect-automation-and-ai-review.md`
- `content/code-quality/baseline-budgets-in-ci.md`
- `content/code-quality/gradual-rollout-and-automatic-rollback.md`
- `content/code-quality/online-quality-monitoring-and-version-health-dashboard.md`
- `content/code-quality/static-analysis-and-asset-quality-tools.md`
- `content/code-quality/testability-design-for-game-code.md`
- `content/code-quality/game-project-build-pipeline-jenkins-github-actions.md`

### 已写的工程质量实践

- `content/engine-notes/unity-resource-delivery-engineering-practices-baseline.md`
- `content/engine-notes/unity-resource-system-smoketests-and-regression.md`
- `content/engine-notes/unity-shader-variant-ci-monitoring.md`
- `content/engine-notes/hybridclr-ci-pipeline-generate-all-and-development-flag.md`
- `content/engine-notes/crash-analysis-series-index.md`
- `content/projects/vfx-checker-case.md`

## 当前的结构性判断

这条线现在已经站住了四层：

- “代码怎么更好改”已经有入口和桥梁文
- “哪些判断该变成护栏”已经有 `QG-01 ~ QG-06`
- “预算怎样进 CI、构建怎样进流水线、规则怎样前移”已经有对应文章
- “版本怎样放量、怎样盯线上健康”已经有灰度和线上监控文章

现在真正还缺的，主要不是测试和 gate 本身，而是：

- 从规范到护栏这类更偏团队机制的收束文
- 变体、资源、热更链路怎样接进 CI 这类跨系列工程实践的总收束文
- 烟测与回归这类已经有局部实践、但还没系统收束的工程链路文

## 目录结构

### A. 代码质量判断层

1. 代码质量不是洁癖，而是交付能力 `已写`
2. 游戏代码为什么容易腐化 `已写，复用 SW-01`
3. 耦合与内聚 `已写，复用 SW-02`
4. SOLID 主线 `已写，复用 SW-03 ~ SW-09`
5. DRY / KISS / YAGNI `已写，复用 SW-10`
6. Clean Code / Code Smell / 重构 `已写，复用 SW-11 ~ SW-13`

### B. 变更与协作层

1. 代码评审真正该拦什么，不该拦什么 `已写`
2. 什么问题必须做成自动检查，不能靠人盯 `已写`
3. 测试保护下的最小重构路径 `已写`
4. AI review、人类 review、CI 怎样分工 `已写`
5. 可测试性设计：怎样让系统能验证、能接手、能重构 `已写`
6. 从规范到护栏：命名、目录边界、配置契约、脚手架分别解决什么 `待写`

### C. 交付与门禁层

1. 自动化构建流水线：Jenkins / GitHub Actions 打包流水线 `已写`
2. Quality Gate：发布前必须通过的检查清单与阻断点 `已写`
3. 烟测与回归：从构建产物验证到关键路径回归的最小链路 `部分已写`
4. Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI `已写`
5. 静态分析与资产规则：哪些问题该在编译期、导入期、构建期发现 `已写`
6. 变体、资源、热更链路怎样接进 CI `已写局部实践，待收束`

### D. 发布与线上质量层

1. 灰度发布与自动回滚：观察窗口、触发条件、回滚策略 `已写`
2. 热修复流程：紧急修复的快速验证通道、Cherry-Pick 策略、回滚预案与演练 `已写`
3. Crash / ANR / 卡顿监控：版本健康看板怎样立 `已写，Crash 系列可复用`
4. Bug 管理与 Playtest：严重级别、生命周期、验证闭环 `已写`
5. 兼容性测试策略：设备矩阵划定、优先级和 Device Farm 边界 `已写`
6. 视觉质量测试：感知差异（Perceptual Diff）、渲染 Artifact 自动检测、多平台视觉一致性 `已写`

## 当前完成度

### 已完成

- 入口判断文 1 篇
- 软件工程基础主线 13 篇
- 变更与协作层已补 5 篇桥梁文 / 护栏文
- `质量护栏` 主线 6 / 6 已写
- 交付与门禁层已补 `自动化构建流水线`、`Quality Gate`、`Baseline`、`静态分析 / 资产规则`
- 发布与线上质量层 5 / 5 已补齐：`灰度发布`、`热修复流程`、`线上质量监控`、`Bug 管理与 Playtest`、`兼容性测试策略`
- 视觉质量判断层已补 `视觉质量测试`
- 工程质量实践若干，覆盖资源交付、烟测回归、Shader Variant CI、HybridCLR CI、Crash 分析、项目案例

### 当前最短结论

这条线现在已经足以回答：

- 代码质量为什么会直接影响交付能力
- 哪些问题该交给 AI、review、自动化测试和 CI
- Quality Gate、Baseline、自动化构建和静态分析怎样真正接进流程
- 版本怎样灰度放量、怎样盯线上健康、什么时候该回滚、出了问题以后怎样热修、怎样把问题重新接回验证闭环、兼容性风险该怎样被提前看见，以及“功能没坏但画面已经错了”该怎样被稳定识别

还没补上的，主要是从规范到护栏、变体 / 资源 / 热更链路怎样接进 CI，以及烟测 / 回归怎样系统收束这类更强的工程收束文。

## 推荐后续写作顺序

1. 先补 `从规范到护栏：命名、目录边界、配置契约、脚手架分别解决什么`
原因：现在缺的已经不是发布与线上响应，而是怎样把已经写出来的规则收束成团队能长期执行的机制。

2. 再补 `变体、资源、热更链路怎样接进 CI`
原因：这条线在资源交付、Shader Variant、HybridCLR CI 里已经有很多局部实践，现在值得收成一篇总收束文。

3. 再补 `烟测与回归：从构建产物验证到关键路径回归的最小链路`
原因：这块现在是“部分已写”，适合在上面几篇收住之后做一次工程链路层面的系统归并。

## 维护规则

- `content/code-quality/_index.md` 继续作为这条线的稳定入口页
- 每补一篇新文，优先回写这份计划和相关子计划
- 不单独再开一个松散的 “QA 栏目”，优先继续把现有 `code-quality` 收束成更完整的工程质量主线
- 与资源交付、Crash、CI、Shader Variant、HybridCLR 相关的工程实践，优先作为这条主线的案例和证据来复用
