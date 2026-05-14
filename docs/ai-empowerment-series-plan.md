# AI 赋能游戏开发系列计划

## 这份计划解决什么

这份计划是 `AI 赋能游戏开发` 系列的 canonical 篇级目录。

它负责回答：

1. 这个系列到底在解决什么问题
2. 已有 01-07 各自承担什么职责
3. 08 `AI Coding Harness Engineering` 应该放在哪
4. 新增 09-11 AI 工程化实践三切面（SDD / Skill 自动沉淀 / kb/ vs LLM Wiki）各自的职责与触发条件
5. 跟独立的 `Harness Engineering` 系列怎么分工
6. 系列索引页和正文更新时应该回写哪里

根 `doc-plan.md` 只保留系列级路由，不维护本系列篇级清单。

## 一句话定位

`AI 赋能不是把工具装进团队，而是把知识、上下文、领域规则、开发流程、验证门禁和反馈沉淀接成可复用的工程闭环。`

## 目标读者

- 技术负责人 / 主程
- 有 AI 工具使用经验的游戏开发者
- 负责团队知识管理、开发流程和工程效率的人
- 想把个人 AI 使用经验沉淀成团队能力的人

## 系列边界

### 属于这条线的内容

- 团队知识管理闭环
- 内网 AI 问答层与知识回流
- LKB / Wiki 等知识生产和沉淀链路
- 项目上下文、领域 Skill、跨层开发工作流
- AI Coding Harness 的流程化、验证和跨 Agent 交接
- AI 辅助开发的最小指标、复盘和反馈沉淀

### 不属于这条线的内容

- AI 模型横评和工具购买建议
- Prompt 技巧教程
- 与游戏工程无关的泛 AI 生产力文章
- 只讲某个 SaaS 产品配置、不沉淀团队工作流的教程
- 需要私仓源码才能成立、无法公开描述的实现细节

## 核心模型

本系列目前形成两条线，08 开始把两条线接成 Harness。

```text
知识管理线：
Dify 问答入口
-> 知识缺口
-> LKB / Claude Code 生产知识
-> Hugo Wiki 沉淀
-> Dify 重新索引

AI 开发工作流线：
项目上下文
-> 领域 Skill
-> 跨层开发流程
-> Harness 状态机
-> 验证门禁
-> 反馈沉淀
```

## 文章目录

### A. 知识管理线

#### AI 赋能 01｜团队知识管理的闭环：从"反复被问"到"AI 能答"

- 正文：`content/ai-empowerment/ai-empowerment-01-team-knowledge-closed-loop.md`
- 状态：`已写`
- 职责：建立 Dify -> 知识缺口 -> LKB -> Wiki -> Dify 的整体闭环。

#### AI 赋能 02｜Dify + Ollama：20 人团队的内网 AI 问答层

- 正文：`content/ai-empowerment/ai-empowerment-02-dify-team-qa.md`
- 状态：`已写`
- 职责：说明团队唯一问答入口怎样落地到内网环境。

#### AI 赋能 03｜LKB：用 AI 消化原始资料生成结构化知识

- 正文：`content/ai-empowerment/ai-empowerment-03-lkb-knowledge-engine.md`
- 状态：`已写`
- 职责：说明原始资料怎样被 AI 消化成可维护的知识文档。

#### AI 赋能 04｜知识缺口发现与回流：让系统越用越好

- 正文：`content/ai-empowerment/ai-empowerment-04-knowledge-gap-feedback.md`
- 状态：`已写`
- 职责：说明 Dify 答不了的问题怎样回流成下次能答的知识。

### B. AI 上下文线

#### AI 赋能 05｜CLAUDE.md：让 AI 理解你的项目

- 正文：`content/ai-empowerment/ai-empowerment-05-claude-md-project-context.md`
- 状态：`已写`
- 职责：说明项目级上下文应该怎样固化，避免每次对话重新解释项目。

#### AI 赋能 06｜Skill 系统：给 AI 注入领域规则

- 正文：`content/ai-empowerment/ai-empowerment-06-skill-domain-knowledge.md`
- 状态：`已写`
- 职责：说明特定领域规则怎样按需注入，避免把所有信息塞进全局上下文。

### C. 开发工作流线

#### AI 赋能 07｜AI 辅助开发工作流：从协议到 UI 的跨层联动

- 正文：`content/ai-empowerment/ai-empowerment-07-dev-workflow-integration.md`
- 状态：`已写`
- 职责：说明 AI 在 Data -> Protocol -> Server -> Client -> Unity UI 五层开发中的角色边界。

#### AI 赋能 08｜我如何搭建自己的 AI Coding Harness Engineering

- 正文：`content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md`
- 状态：`已写`
- 建议 `series_order`：`80`
- 建议 `weight`：`180`
- 职责：把 05 的项目上下文、06 的领域 Skill、07 的跨层开发流程，上升为一套可复用、可验证、可跨 Agent 交接的 AI Coding Harness。

本篇必须回答：

> 怎样让 AI 稳定接手工程任务？

本篇不展开：

- CLAUDE.md 应该写什么，由 05 覆盖
- Skill 的定义、触发与维护，由 06 覆盖
- 具体五层开发案例，由 07 覆盖
- Prompt 技巧、模型横评、工具购买建议

推荐结构：

1. 为什么 AI Coding 不能只靠更强模型
2. Harness 的最小模型
3. 我会如何搭建 v0
4. 一次任务在 Harness 里的完整流转
5. 怎么验证 Harness 真的有效
6. 什么时候扩展，什么时候停下来

### D. AI 工程化实践三切面（09-11）

> 这组三篇是 2026-05-13 新增的实践层文章，源于阅读 3 篇外部参考文章（SDD / Hermes 自进化 / LLM Wiki）后的延展。它们不接续 08 的 "Harness 演化" 主轴，而是从 3 个独立切面讲 AI 工程化的具体落地。

> "原计划 09 `AI Coding 指标与复盘`" 已经由独立的 `Harness Engineering` 系列承载（见 [docs/harness-engineering-series-plan.md](./harness-engineering-series-plan.md) 的 08 篇）。本系列 09 重新启用为下面的 SDD 实践篇。

#### AI 赋能 09｜把 SDD 套进内容生产：Hugo 站点的 spec / plan / tasks 实录

- 计划正文：`content/ai-empowerment/ai-empowerment-09-sdd-in-content-production.md`
- 状态：`已写完整正文（2026-05-13）`——以 2026-05-12/13 启动 Harness Engineering 系列的真实过程为案例
- 建议 `series_order`：`90`
- 建议 `weight`：`190`
- 职责：以 TechStackShow 自己的内容生产为案例，演示 Spec-Driven Development 的四阶段（Specify → Plan → Implement → Validate）如何落到一个 Hugo 站点的写作流程上。
- 必须回答：现有 col-* 系列 skill（col-editor / col-drilldown / col-verify / col-draft / col-risk / col-bridge / col-consistency）跟 SDD 的 spec.md / plan.md / tasks.md 三文件体系如何映射；DAY 0 写 spec 的时间成本到底值不值。
- 不展开：
  - SDD 在企业 Java / Web 后端项目里的实践（外部已占）
  - 代码生成 Agent 实操（属于 ai-empowerment-07 / Harness Engineering 范畴）
- 关键 TODO：`EXPERIENCE-TODO` 需要至少 1 次真实写作任务的完整 DAY 0~N 复盘记录

#### AI 赋能 10｜Skill 自动沉淀：不靠 RL 的"自进化"在 Claude Code 里怎么实现

- 计划正文：`content/ai-empowerment/ai-empowerment-10-skill-auto-distillation.md`
- 状态：`已写骨架（2026-05-13），大量 DATA-TODO / EXPERIENCE-TODO 待 2 周真实运行后回填`
- 建议 `series_order`：`100`
- 建议 `weight`：`200`
- 职责：演示个人工程师怎样用 Claude Code 的 hook + memory + skill 三件套，搭出 Hermes Agent 那条"动态 Skill 沉淀"路径的轻量版——不碰 RL 训练那条路。
- 必须回答：Stop hook / SubagentStop hook 怎么配；后台复盘怎么跑（不是另起一个 Agent，而是用一个独立 session）；什么时机沉淀 / 什么时机不沉淀（避免 Bloat）。
- 不展开：
  - Hermes 的 RL / GRPO / 奖励函数（不适用、不写）
  - Skill 设计模式（外部"Agent Skill 规范"已占）
- 关键 TODO：`DATA-TODO` 需要至少 5 次"AI 犯错 → 沉淀回 memory/skill"的真实记录

#### AI 赋能 11｜kb/ 与 LLM Wiki 范式：把 Karpathy 的 idea 适配到内容站

- 计划正文：`content/ai-empowerment/ai-empowerment-11-kb-and-llm-wiki-pattern.md`
- 状态：`已写骨架（2026-05-13），首段已明示"参考 LLM Wiki + 内容站适配"立场，大量 DATA-TODO / EXPERIENCE-TODO 待 kb/wiki/ 长到 50+ 页后回填`
- 建议 `series_order`：`110`
- 建议 `weight`：`210`
- 职责：诚实记录本站 kb/raw + kb/wiki 二层架构跟 Karpathy LLM Wiki 范式的对照关系；讲清楚 Ingest / Query / Lint 三操作在内容站（非通用知识库）场景下要怎么调整。
- 立场标注：**作者之前看过 Karpathy 的 LLM Wiki 思想，kb/ 的二层架构属于"有意无意参考 + 内容站场景适配"**，不是独立沉淀。文章首段必须明示这一点，避免被读者误以为是抄而作者没意识到。
- 必须回答：内容站场景下 raw 层放什么（不是论文截图、不是 Slack 日志，是源码笔记 / 决策记录 / 外部参考摘要）；Schema 怎么跟 Hugo 站点的 docs/ 区分（一个是前置规划、一个是回顾沉淀）；为什么没采用 GBrain 的向量混合检索（规模、ROI、维护成本）。
- 不展开：
  - 通用知识管理理论（Memex / RAG 范式）
  - GBrain 的图谱关系抽取实现（不适用本站）
  - Obsidian-Wiki 的多 Agent 框架（跟单作者写作场景错位）
- 关键 TODO：`EXPERIENCE-TODO` 需要一段"我什么时候看的 LLM Wiki / 当时怎么消化 / kb/ 二层架构具体在哪一步落定"的真实回忆

是否写 09 / 10 / 11 的判断条件：

- 09 SDD 实录：至少有 1 次完整的 spec.md → plan.md → tasks.md → 完成文章的真实任务记录，再写
- 10 Skill 沉淀：至少跑过 2 周的 hook + memory 配置，能拿出几个真实沉淀样本，再写
- 11 kb/ vs LLM Wiki：kb/wiki/ 下至少有 20 篇 concept / entity 页面，能展示真实结构，再写

## 推荐阅读顺序

### 技术负责人

1. 01：先理解团队知识闭环
2. 02：看问答入口怎样落地
3. 04：看知识缺口怎样回流
4. 08：看 AI Coding Harness 怎样把流程固化
5. 想看演化 / 诊断 / 复盘 → 切到独立的 `Harness Engineering` 系列

### 一线开发者

1. 05：先理解项目上下文
2. 06：再理解领域 Skill
3. 07：看日常开发任务中 AI 该做什么
4. 08：把个人工作流沉淀成 Harness
5. 想立刻动手实践 → 看 09 / 10 / 11（按当前关心的切面挑）

### 知识管理负责人

1. 01 -> 02 -> 03 -> 04
2. 再读 05 / 06，理解知识如何服务开发任务
3. 读 08，判断是否把内容生产、代码开发和验证流程接成统一 Harness
4. 想把知识管理推到"自进化"层 → 看 11（kb/ 与 LLM Wiki 范式）

### 想"最近就要实践"的人

1. 09：先按 SDD 套一次任务，体会"先 spec 后实现"的成本与收益
2. 10：把 Skill 自动沉淀配起来，让单次任务能学到东西
3. 11：把多次沉淀组织进 kb/，让长期知识不散

## 与其他系列的关系

- `代码质量到工程质量`：负责质量判断、review、CI、自动化测试和 Quality Gate。本系列只讨论 AI 如何进入这些流程，不替代工程质量主线。
- `质量护栏 / Quality Guardrails`：负责验证门禁分层。本系列 08 的验证门禁可以引用它，但不重讲测试体系。
- `交付工程旗舰专栏`：负责多端交付和发布闭环。本系列只覆盖 AI 对知识、开发和验证流程的赋能。
- `长线运营工程专栏`：负责运营期场景。本系列只在 AI 知识问答和工作流层面交叉，不展开运营 AI 场景。

## 当前状态

- 01-04 知识管理线已写完
- 05-06 AI 上下文线已写完
- 07 开发工作流线已写完
- 08 `AI Coding Harness Engineering` 已写，正文保留 `DATA-TODO` / `EXPERIENCE-TODO` 等真实数据补充点
- 09 / 10 / 11 AI 工程化实践三切面（SDD / Skill 自动沉淀 / kb/ vs LLM Wiki）已写（2026-05-13）：09 完整正文（基于真实 Harness Engineering 系列搭建过程的案例），10 / 11 骨架（大量 TODO 待真实运行后回填）
- 原 "09 指标与复盘" 已让位给独立的 `Harness Engineering` 系列承载（见 [docs/harness-engineering-series-plan.md](./harness-engineering-series-plan.md) 的 08 篇）
- 08 与 `Harness Engineering` 系列的分工：本系列 08 仍是通用 v0 实施篇；`Harness Engineering` 系列从 v0 之后接续，主战场是游戏引擎客户端的演化、诊断、瘦身、跨仓库、场景联动
- 09-11 与 `Harness Engineering` 系列的分工：09-11 是"AI 工程化的 3 个切面实践"，跟 Harness 演化主轴正交——任务输入侧（SDD）、任务反馈侧（Skill 沉淀）、长期知识侧（kb / LLM Wiki）

## 维护规则

1. 本文件是 `AI 赋能游戏开发` 的 canonical 篇级计划。
2. 根 `doc-plan.md` 只维护本系列路由，不回填篇级清单。
3. `content/ai-empowerment/ai-empowerment-series-index.md` 是发布侧索引页，应与本文件保持方向一致，但不替代本文件。
4. 每新增或完成一篇文章，优先回写本文件，再按需更新系列索引页。
5. 08 写作时必须使用 `.agents/harness/content-production.md` 的 Day 1 产物，避免退化成泛 AI Coding 方法论。
