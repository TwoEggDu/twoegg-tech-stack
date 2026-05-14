---
title: "AI 赋能 09｜把 SDD 套进内容生产：Hugo 站点的 spec / plan / tasks 实录"
slug: "ai-empowerment-09-sdd-in-content-production"
date: "2026-05-13"
description: "外部讲 Spec-Driven Development 的文章都是 Java / Web 后端场景。这篇用 TechStackShow 站点新开 Harness Engineering 系列的真实过程，演示同一套 SDD 范式怎样落到内容生产上——以及哪里水土不服。"
tags:
  - "AI Engineering"
  - "Spec-Driven Development"
  - "Content Production"
  - "Developer Productivity"
series: "AI 赋能游戏开发"
primary_series: "ai-empowerment"
series_role: "article"
series_order: 90
weight: 190
---

> **读这篇之前**：本篇假设你已经读过 [05｜CLAUDE.md：让 AI 理解你的项目]({{< relref "ai-empowerment/ai-empowerment-05-claude-md-project-context.md" >}}) 和 [06｜Skill 系统：给 AI 注入领域规则]({{< relref "ai-empowerment/ai-empowerment-06-skill-domain-knowledge.md" >}})。这篇不重讲这些，讲的是把 Spec-Driven Development（SDD）的四阶段套进一个 Hugo 站点的写作流程之后，会发生什么。

## 这篇解决什么问题

2026 年 5 月有一篇阿里云的文章在工程圈传得很广——《5 人 7 天干完 20 人数周的活：Spec-Driven Development 如何重新定义 AI 编程》。它讲了一个 SDD 范式：DAY 0 不写代码、只写 Spec，DAY 1-6 让 AI 按 Spec 执行。

读完之后，我做了一件可能很多读这篇文章的人都做过的事——**反问自己：这套范式套到我自己的场景里，能不能直接用？**

我的场景跟那篇文章描述的不一样。我不是带一个 5 人团队做企业 Java 系统，我是一个工程师在维护一个 Hugo 中文技术专栏站点。任务不是"开发一个新产品"，是"新开一个 8 篇的文章系列"。但**任务的形态高度一致**：

- 都需要前置定义（系列定位 vs MVP 边界）
- 都需要拆解（篇级 plan vs 模块拆分）
- 都需要按部就班执行（写文章 vs 写代码）
- 都需要验证（hugo 构建 vs 单测）

更巧的是，我之前就有一组写作辅助 skill：`col-editor / col-drilldown / col-verify / col-draft / col-risk / col-bridge / col-consistency`。我搭这些 skill 的时候没听过 SDD——但搭出来的形态，跟 SDD 的 Specify → Plan → Implement → Validate 四阶段**几乎一一对应**。

这篇文章要回答两件事：

1. 一个真实的内容生产任务跑完 SDD 全流程，到底长什么样、各阶段花多少时间
2. SDD 范式从 Java 后端搬到 Hugo 内容站，**哪些地方原样能用、哪些地方水土不服**

我用的案例是 2026-05-12 启动、2026-05-13 进入正文阶段的"Harness Engineering" 系列搭建。这是一次真实的 SDD 走查——不是事后补的范例，是过程中我和 AI 实际怎么走的。

## SDD 的四阶段与 col-* skill 的对应

先把对应关系画出来。SDD 原始的四阶段（出自阿里那篇）：

```
Specify（规格定义）-> Plan（方案规划）-> Implement（代码实现）-> Validate（验证确认）
```

我的 col-* skill 组：

```
col-editor（编辑定位）
col-drilldown（七层深挖）
col-verify（验证设计）
col-draft（初稿骨架）
col-risk（风险审稿）
col-bridge（解释桥接）
col-consistency（系列一致性）
col-rewrite（最小改写）
```

对应起来：

| SDD 阶段 | 内容生产对应物 | 用到的 skill / 文件 |
|---------|--------------|-------------------|
| Specify | 系列立项备忘 / 编辑工作单 | col-editor（输出工作单）/ docs/*-positioning.md |
| Plan | 系列篇级 plan / 七层深挖 | col-drilldown（深挖分析）/ docs/*-series-plan.md |
| Implement | 正文起草 / 骨架成文 | col-draft（写骨架）/ content/*.md |
| Validate | 风险审稿 / 系列一致性 / hugo 构建 | col-risk / col-consistency / hugo |

这个对应关系不是事后凑的，是真实的工作流。我之前一直**没给它起名字**，看到阿里那篇之后才意识到这就是 SDD。

## 一次真实任务的全程记录

下面是 2026-05-12 到 2026-05-13 这次"启动 Harness Engineering 系列"的完整 SDD 走查。

### Phase 1（Specify）：从 0 到一份定位备忘

#### 输入

一句话："我想在这个工程里边落地 harness engineering"

这一句话有多模糊，做过内容工作的人都懂——它不是"我想写一篇文章"，是"我想做一件事"。落到 Hugo 站点上要回答的问题至少有：

- 写多少篇
- 放在哪个栏目
- 跟现有 `ai-empowerment-08` 的 Harness 那篇怎么处理
- 跟其他系列（code-quality / delivery / live-ops）怎么划界
- 这个系列卖什么差异化角度

如果直接让 AI 写大纲，它会给一份"看起来很顺"的大纲——但很可能跟外面那 4 篇 Harness 文章撞角度、跟 ai-empowerment 系列重复、跟自己的 wiki 工作笔记内容相同。

#### 我和 AI 做了什么

按 SDD 的原则，第一步不写大纲，先做盘点和定位。具体是 3 个动作：

**动作 1：素材盘点**

派 3 个 Explore subagent 并行读：

- Agent A：wiki ai-collaboration 系列 7 篇正文
- Agent B：wiki harness-engineering 系列 4 篇正文 + TechStackShow 已发布的 ai-empowerment-08
- Agent C：E:/harness/ 4 篇外部参考文章

每个 agent 输出统一格式：文件路径 / 核心论点 / 关键例子 / 留下的空白 / 独到角度。**这一步不写大纲、不挑卖点、只盘点**。

**动作 2：差异化分析**

3 份报告回来后，我做横向交叉：

- 哪些角度外部已经占住了（不写）
- 哪些角度自己手上独家（卖点候选）
- 哪些角度跟自己已发布的内容重复（不写或合并）

得到 3 个候选定位（纯 A 游戏引擎客户端 / 纯 B 演化诊断 / 纯 C 跨仓库），并标了每个的优缺点。

**动作 3：定位备忘落盘**

最终选 A+B 混合，把定位备忘写进 `docs/harness-engineering-series-positioning.md`。这份文件**不是大纲**——它只回答"为什么是这个立意、卖点清单、不写什么清单、跟既有素材的关系"。

#### 这阶段的产出

一份约 200 行的 `docs/harness-engineering-series-positioning.md`，明确钉死：

- 一句话定位
- A+B 混合的取舍逻辑
- 6 条"不写什么"
- 6 条差异化卖点
- 跟 ai-empowerment-08 / wiki 两个系列 / E:/harness 4 篇外部文章 各自的处理方式

#### 时间成本

约 1.5 小时。其中 3 个 Explore subagent 并行盘点占了 30 分钟，后面是横向分析和定位决策。

#### 关键对照：跟 SDD 原始 Specify 阶段的差异

阿里那篇里 Specify 的核心是写 `spec.md`：Problem Statement / Success Metrics / User Stories / Acceptance Criteria / Non-Goals / Constraints。

我的"定位备忘"包含其中大部分（一句话定位 ≈ Problem Statement，差异化卖点 ≈ User Stories，不写什么 ≈ Non-Goals，跟既有素材的关系 ≈ Constraints），但**缺一项最关键的——Success Metrics**。

内容生产的 Success Metrics 不像后端那样可量化（"P95 < 50ms"）。它的等价物是什么？我现在的回答是：**审稿轮次、构建通过率、是否需要回头改立意**。这些都是事后才能算的，前置不出来。这是 SDD 原始范式在内容生产场景的第一个水土不服点。

### Phase 2（Plan）：从备忘到 canonical 篇级 plan

#### 输入

上一阶段的定位备忘 + 你给的 3 个拍板：C 视角进首轮、新建 `content/harness-engineering/` 栏目、首轮 8+ 篇全面。

#### 我和 AI 做了什么

写 `docs/harness-engineering-series-plan.md`。这份文件按 `docs/series-planning-method.md` 规定的格式：

- 系列定位
- 目标读者
- 系列边界（属于 / 不属于）
- 与其他系列的关系
- 核心模型
- 栏目与目录结构（weight 区间、子组规约）
- 文章目录（8 篇各自的职责、weight、状态、必须回答 / 不展开 / 关键 TODO）
- 推荐阅读顺序
- 当前状态
- 维护规则

#### 这阶段的产出

一份约 180 行的 series-plan。**它不是大纲也不是 outline**，是篇级目录——每篇用一段标准格式说"这篇必须回答什么、不展开什么、有哪些关键 TODO"。

#### 时间成本

约 30 分钟。这一步快是因为 Phase 1 把立意钉死了——只剩翻译成篇级清单。

#### 关键对照：跟 SDD 原始 Plan 阶段的差异

SDD 的 plan.md 是技术方案：Architecture Decision / Module Breakdown / Interface Contracts / Risk Assessment。

我的 series-plan 跟它**结构上极相似**，但有一处大不同：

- SDD 的 plan 给"模块" + "接口契约"（模块之间怎么调用）
- 我的 series-plan 给"篇" + "**篇间引用**"（哪些篇通过 relref 互相指向）

内容站的"接口契约"是 relref——每篇结尾的"下一篇 → "和正文里的 `{{</* relref */>}}` 链接。这件事在 SDD 原始范式里没有对应物，但**在内容生产里非常关键**：relref 错了会导致 hugo 构建失败，相当于代码里的"调用了不存在的函数"。

### Phase 3（Implement）：从 plan 到 8 篇正文骨架 + 1 篇完整正文

#### 输入

series-plan + 你给的拍板（"混合：骨架 + 首篇完整正文"、"宁可多留 TODO 不编造"）。

#### 我和 AI 做了什么

按 plan 列的 8 篇职责依次写：

- 栏目入口 `content/harness-engineering/_index.md`
- 系列索引页 `content/harness-engineering/harness-engineering-series-index.md`
- 01 首篇完整正文（"为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness"）
- 02-08 七篇骨架，每篇含完整 frontmatter + 章节论点骨架 + 跟前后篇的 relref + 标好 `DATA-TODO` / `EXPERIENCE-TODO`

#### 这阶段的产出

10 个新文件 + 2 处旧文件更新（`doc-plan.md` 路由表、`ai-empowerment-series-plan.md` 接续标注）。

#### 时间成本

约 2 小时（含 8 篇 + 2 个索引页）。

#### 关键对照：跟 SDD 原始 Implement 阶段的差异

SDD 的 Implement 阶段是 AI 主导写代码——人 review 合并 PR。我这次实际操作是 AI 主导写正文骨架——人（你）明天 review 填 TODO。

但有一处水土不服：**正文里的"事实陈述"很难像代码那样有可执行验证**。

代码写错了，单测会爆。文章里把 Unity 某 API 的行为讲错了，hugo 构建一样能过——只有读者（甚至几个月后的作者自己）翻到才会发现。

我的应对方式是：**所有需要真实经验或数据的位置都标 `DATA-TODO` / `EXPERIENCE-TODO`**，不让 AI 编造代填。这是 SDD 在内容场景的一个被动适配——把"AI 不能保证事实正确"承认下来，把验证责任挪到 Phase 4。

### Phase 4（Validate）：从骨架到能发布

#### 输入

10 个新文件 + 2 个旧文件更新。

#### 我和 AI 做了什么

跑 `hugo`。1221 页全部构建成功、6.8 秒、零 ERROR。然后 `ls public/harness-engineering/` 确认 10 个产物全部正确渲染。

#### 这阶段的产出

一份验证报告（构建通过 + 产物清单）+ "明天打开就能逐篇填 TODO" 的交付状态。

#### 时间成本

不到 1 分钟。

#### 关键对照：跟 SDD 原始 Validate 阶段的差异

SDD 的 Validate 是自动化测试 + 人 review。内容站对应的两层：

- **机器能验证的**：hugo 构建（frontmatter 语法 / shortcode 引号 / relref 指向 / weight 冲突）
- **机器不能验证的**：事实正确 / 表达通顺 / 论点跟其他文章一致 / 风险句没越界

机器那一层是确定性的——错了就构建挂掉，跟单测一样。人那一层完全无法自动化——这是内容生产场景特有的"必须人接"。

我目前的应对是：

- col-risk skill：跑"风险审稿"——找高风险句和未核查事实
- col-consistency skill：跑"系列一致性"——查跟其他文章的术语 / 立场冲突
- col-bridge skill：跑"解释桥接"——找读者会卡住的位置
- 人最终 review

这三层串起来是内容站版的 Validate——**比 SDD 原始的"单测 + review"更重，因为机器能验证的部分占比小**。

## 总时间账

这次任务从你说"落地 harness engineering"到我交付"明天打开就能用"：

| 阶段 | 时间 |
|------|------|
| Specify | 1.5 小时 |
| Plan | 0.5 小时 |
| Implement | 2 小时 |
| Validate | < 1 分钟 |
| **合计** | **约 4 小时** |

中间隔了一晚上（5-12 → 5-13）。

如果跳过 Specify 直接让 AI 写大纲再写骨架，省的是 1.5 小时——但大概率写出来跟外部 4 篇撞角度、跟 wiki 系列重复，明天你看到的是一堆要回头改立意的草稿。

**这 1.5 小时是 SDD 范式在内容生产里的核心收益。**

## 内容生产 SDD 跟代码 SDD 的差异总结

把上面四个阶段的"水土不服"汇总：

| 维度 | 代码 SDD | 内容生产 SDD |
|------|---------|------------|
| Specify 的 Success Metrics | 可量化（P95 / 错误率） | **难前置量化**（审稿轮次、是否回头改立意） |
| Plan 的"接口契约" | API / 函数签名 | **篇间 relref** |
| Implement 的"事实正确性" | 单测能爆 | **机器无法验证** |
| Validate 的"必须人接"比例 | 小（自动化覆盖大头） | **大（事实 / 表达 / 一致性 / 风险都要人）** |
| 失败回滚成本 | 高（要回去改代码 + 重跑构建） | **低（改文章直接重构建）** |
| 任务粒度 | 单 feature / 单 PR | **单篇 / 多篇** |

内容生产 SDD 比代码 SDD **前置成本低、回滚成本也低**——这意味着可以更激进地启动 SDD 流程，不需要像代码那样担心"DAY 0 写多了 Spec 后面改不动"。但**机器能验证的部分小得多**——必须接受"hugo 构建过 ≠ 内容质量好"。

## col-* skill 反过来看 SDD 的启示

读完 SDD 那篇之后再回看自己的 col-* skill，发现两件事：

### 启示 1：之前缺了一个 "constitution.md"

SDD 引入了一个项目级"宪法" `constitution.md`——所有 Spec 都必须遵守的不可变约束。

我的内容站对应物是 `CLAUDE.md` / `AGENTS.md`——但它们目前的形态偏"项目说明书"，不是"每篇文章的 spec 都必须遵守的约束"。

差异在哪？看一个具体例子：

- "frontmatter YAML 引号规则"（包含中文引号必须外层单引号）——这是项目说明书，AI 读了大概率会遵守
- "**任何一篇文章在写完后必须跑 col-risk 和 col-consistency**"——这是 constitution，目前 CLAUDE.md 里没明确列

可改进的方向：把 col-* skill 的触发节奏明确写进 CLAUDE.md，让它从"工具说明书"升级为"流程宪法"。

### 启示 2：之前缺了一个跨 skill 的 tasks.md

SDD 的 tasks.md 把 plan 拆成"原子任务清单"，每个任务对应可独立验证的交付物。

我目前的 col-* skill 是**单点工具**——col-editor 出工作单、col-drilldown 出深挖稿、col-draft 出骨架。它们之间靠人手工串。

可改进的方向：在 `docs/*-series-plan.md` 之下再加一层 `docs/*-tasks.md`——每篇文章一个原子任务条目，列"输入是什么、用哪个 skill、产出是什么、怎么验证"。这是当前 col-* 体系缺的最后一块拼图。

<!-- EXPERIENCE-TODO: 真把上面两个改进做了之后，写一段"改完之前 vs 改完之后"的对比。当前阶段先不动，等下次启动新系列时试一次再回填。 -->

## 收束

我不建议你读完阿里那篇 SDD 文章之后**立刻把 4 阶段 / 三文件 / constitution / tasks 整套搬进自己的项目**。那是企业级团队的标准流程——重、严密、有团队磨合成本。

但**SDD 的核心 idea 可以立刻用**——`DAY 0 不动笔，先做一个东西（定位备忘 / spec / 你叫它什么都行），把"为什么做、不做什么、怎么算完"钉死`。

钉的过程会让你发现**很多模糊都是真模糊**：

- 你以为想清楚了立意，写出来才发现跟另一篇撞角度
- 你以为知道要写 8 篇，列篇级 plan 才发现 3 篇内容重复
- 你以为知道目标读者，定位备忘里才发现"技术负责人"和"一线开发"在这个系列里要不同写法

DAY 0 不动笔不是仪式。**是把这些模糊在动笔前就抓出来，而不是动笔后翻车了再回头改**。

下一篇 [10｜Skill 自动沉淀]({{< relref "ai-empowerment/ai-empowerment-10-skill-auto-distillation.md" >}}) 切到另一条线——单次任务做完之后，怎样让 AI 自己学到东西。下下篇 [11｜kb/ 与 LLM Wiki 范式]({{< relref "ai-empowerment/ai-empowerment-11-kb-and-llm-wiki-pattern.md" >}}) 收尾——多次沉淀长期怎么组织。

<!-- DATA-TODO: 等下次启动新系列时，做一次完整的 SDD 走查、记录每阶段的实际时间和踩坑，回来补一份"第二次走 SDD 的差异点"。一次实践不构成趋势，至少要 2 次。 -->
