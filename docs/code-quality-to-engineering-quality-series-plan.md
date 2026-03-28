# 代码质量到工程质量系列计划

## 这份计划解决什么

现在仓库里和“代码质量”相关的内容有一个明显结构问题：

- `content/code-quality/` 里只有一篇入口文，栏目判断是对的，但入口太薄
- 软件工程基础主线其实已经写在 `content/engine-notes/solid-sw-*`
- 工程质量实践已经散在资源交付、CI、Crash、项目案例里，但还没有被统一收束

这份计划专门回答四件事：

1. 这条线到底在解决什么问题
2. 哪些内容已经有，哪些是空白
3. 接下来应该按什么顺序补
4. “代码质量”怎样自然过渡到“工程质量”

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

### 已写的工程质量实践

- `content/engine-notes/unity-resource-delivery-engineering-practices-baseline.md`
- `content/engine-notes/unity-resource-system-smoketests-and-regression.md`
- `content/engine-notes/unity-shader-variant-ci-monitoring.md`
- `content/engine-notes/hybridclr-ci-pipeline-generate-all-and-development-flag.md`
- `content/engine-notes/crash-analysis-series-index.md`
- `content/projects/vfx-checker-case.md`

### 当前的结构性问题

- `code-quality` 栏目没有把已有内容接成阅读路径
- “设计原则 -> 工程护栏 -> 发布质量”之间缺桥梁文
- 工程质量内容已有局部实践，但缺总入口、缺写作顺序、缺状态维护

## 目录结构

### A. 代码质量判断层

1. 代码质量不是洁癖，而是交付能力 `已写`
2. 游戏代码为什么容易腐化 `已写，复用 SW-01`
3. 耦合与内聚 `已写，复用 SW-02`
4. SOLID 主线 `已写，复用 SW-03 ~ SW-09`
5. DRY / KISS / YAGNI `已写，复用 SW-10`
6. Clean Code / Code Smell / 重构 `已写，复用 SW-11 ~ SW-13`

### B. 变更与协作层

1. 代码评审真正该拦什么，不该拦什么 `待写`
2. 什么问题必须做成自动检查，不能靠人盯 `待写`
3. 测试保护下的最小重构路径 `待写`
4. 可测试性设计：怎样让系统能验证、能接手、能重构 `待写`
5. 从规范到护栏：命名、目录边界、配置契约、脚手架分别解决什么 `待写`

### C. 交付与门禁层

1. Quality Gate：发布前必须通过的检查清单与阻断点 `待写`
2. 烟测与回归：从构建产物验证到关键路径回归的最小链路 `部分已写`
3. Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI `待写`
4. 静态分析与资产规则：哪些问题该在编译期、导入期、构建期发现 `待写`
5. 变体、资源、热更链路怎样接进 CI `已写局部实践，待收束`

### D. 发布与线上质量层

1. 灰度发布与自动回滚：观察窗口、触发条件、回滚策略 `待写`
2. Crash / ANR / 卡顿监控：版本健康看板怎样立 `待写，Crash 系列可复用`
3. Bug 管理与 Playtest：严重级别、生命周期、验证闭环 `待写`

## 推荐写作顺序

1. 先补 `代码评审真正该拦什么，不该拦什么`
原因：它正好站在“代码质量”与“工程质量”中间，是最好的桥。

2. 再补 `什么问题必须做成自动检查，不能靠人盯`
原因：把 review 的边界继续推进到工程护栏。

3. 再补 `测试保护下的最小重构路径`
原因：把已有的“重构手法”推进到团队可执行路径。

4. 再补 `Quality Gate`
原因：把个人工程判断变成团队合并 / 发布门禁。

5. 再补 `Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI`
原因：把“感觉不稳”改成“超阈值就告警或阻断”。

6. 最后补 `灰度 / 回滚 / 线上质量监控`
原因：到这一步才形成完整闭环。

## 暂不优先的内容

- 具体某个 CI 平台的配置细节
- 某个监控服务的接入步骤罗列
- 纯工具对比文

这些东西不是不写，而是不应该先写。前面的方法和边界没立住，后面很容易又退回成工具清单。

## 立即动作

- 已拆出 `docs/quality-guardrails-series-plan.md`，专门推进“自动化测试 + AI review + CI 门禁”这条子主线
- 先更新 `content/code-quality/_index.md`，把已写内容接成稳定入口
- 后续每补一篇桥梁文，同时回写这份计划和 `doc-plan.md` 的状态
- 不单独再开一个松散的“QA 栏目”，优先先把现有“代码质量”收束成更完整的工程质量主线

## 当前状态

### 已完成

- 入口判断文 1 篇
- 软件工程基础主线 13 篇
- 工程质量实践若干篇，覆盖资源交付、烟测回归、Shader Variant CI、HybridCLR CI、Crash 分析、项目案例

### 下一步优先级

1. 入口收束
2. 桥梁文 3 篇
3. 门禁 / 基线 2 篇
4. 发布 / 线上质量 2 篇
