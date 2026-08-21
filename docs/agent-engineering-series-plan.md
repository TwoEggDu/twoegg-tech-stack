# Agent Engineering 课程系列计划

> 状态：canonical
>
> 结构基线：`docs/agent-engineering-course-plan-v3.1-review.md`
>
> 生产入口：`docs/agent-engineering-course/README.md`
>
> 状态追踪：`docs/agent-engineering-course/status.md`

## 这份计划负责什么

这是 Agent Engineering 课程唯一的 canonical 篇级目录。它负责课程定位、知识依赖、00-44 目录、课程权重、实验路线、证据边界、阅读顺序和写作顺序。

以下文件保留为历史评审材料，不再承担生产期结构权威：

- `docs/game-agent-engineering-series-outline.md`
- `docs/agent-engineering-course-outline-v2.md`
- `docs/agent-engineering-course-plan-v3-review.md`
- `docs/agent-engineering-course-plan-v3.1-review.md`

文章生产状态不回填到历史 review 稿，统一维护在 `docs/agent-engineering-course/status.md`。

## 一句话定位

帮助熟悉 C# / Unity 和传统软件工程、但 Agent 知识不系统的工程师，从可编程模型逐层理解 Agent Runtime、Reliable Agent 与 Harness，再用固定版本的 DeepSeek Harness 验证抽象，最终完成 BuildPilot Design v1。

## 目标读者

- 熟悉 C# / Unity、接口、状态机、异步和工程工具链
- 可能使用过 Coding Agent，但没有系统学习 Agent Runtime / Harness
- 想设计游戏研发、构建、交付或诊断类专用 Agent

默认具备 HTTP、JSON、异步调用、错误处理、C# 接口、依赖注入和状态机基础。不要求会 Python Agent 框架、训练模型或实现 BuildPilot。

## 学习目标

完成课程后，读者能够：

1. 解释从 Model API 到 Agent System 的能力演进。
2. 区分 Function Calling 与 Tool Runtime、Workflow 与 Agent Loop、Runtime 与 Harness。
3. 设计 Context、Working Memory、Session、Long-term Memory、KB、RAG 与 Skill 的边界。
4. 用 Evidence / Hypothesis / Diagnosis / Verification 构造可审计结果。
5. 建立 Permission、Budget、Trace、Replay、Eval 与 Regression 闭环。
6. Evidence-first 地阅读固定版本的 DeepSeek Harness。
7. 判断何时使用 Script / Rule / Workflow，何时才值得引入 Agent。
8. 完成 BuildPilot Design v1，并区分 Design、Pilot 与 Runtime。

## 冻结的知识主链

```text
00 Agent Engineering World Map
  ↓
Model API → Prompt → Structured Output → Model Adapter
  ↓
Function Calling → Tool Runtime → MCP
  ↓
Agent Loop → Planning → State Machine / Workflow → Long-running
  ↓
Context → Context Debugging → Working Memory → Session / Memory → RAG → Skill
  ↓
Evidence → Permission / Sandbox → Budget → Trace / Replay → Eval / Regression
  ↓
Harness Engineering
  ↓
DeepSeek Harness Evidence-first Source Reading
  ↓
BuildPilot Design v1
```

23 Multi-Agent 是 Advanced / Optional。完成 22 后可以直接进入 24，不影响 Harness 主线。

## Knowledge Dependency Graph

```text
00 World Map
  ↓
01 Model / API / Message / Token
  ↓
02 Prompt
  ↓
03 Structured Output
  ↓
04 Model Adapter / Gateway
  ↓
05 Function Calling
  ↓
06 Tool Runtime
  ↓
07 MCP
  ↓
08 Agent Loop
  ↓
09 Planning
  ↓
10 State Machine / Workflow
  ↓
11 Long-running / Checkpoint
  ↓
12 Context Engineering
  ↓
13 Context Debugging
  ↓
14 Working Memory
  ↓
15 Session / Long-term / Project Memory
  ↓
16 Knowledge Base / RAG
  ↓
17 Skill Engineering
  ↓
18 Evidence Contract
  ↓
19 Permission / Approval / Sandbox
  ↓
20 Budget
  ↓
21 Trace / Replay / Failure Taxonomy
  ↓
22 Eval / Golden Dataset / Regression
  ├──→ 23 Multi-Agent（Advanced / Optional）
  ↓
24-27 Harness Engineering
  ↓
28 DSH Evidence-first Reading
  ↓
29 DSH Architecture Overview
  ↓
30-37 DSH Subsystems
  ↓
38-44 BuildPilot Design v1
```

## Concept Progressive Definition

| 概念 | 导航级出现 | 正式建立 | 源码验证 | 设计回收 |
|---|---|---|---|---|
| Agent | 00 | 08 Agent Loop | 29、33 | 38-40 |
| Agentic | 00 | 08-10 | 29、33 | 38 |
| Agent Runtime | 00 | 08-11、25 | 29、33-36 | 41 |
| Harness | 00 | 24-27 | 28-37 | 43 |
| Host | 00 | 25 | 29、37 | 41 |
| Context | 00 | 12-13 | 32 | 42 |
| Tool | 00 | 05-06 | 35 | 39、42 |
| Workflow | 00 | 10 | 37 | 42 |
| Skill | 00 | 17 | 37 | 42 |
| Memory | 00 | 14-16 | 34、37 | 40、42 |
| RAG | 00 | 16 | 37 | 42 |
| Trace / Replay / Eval | 00 | 21-22 | 34、36 | 43 |

同一概念遵循 Introduction → Foundation / Mechanism → Engineering → Source Verification → Design Application，不在每篇重复从零定义。

## 最终目录

### 00｜课程导论

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 00 | [Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host](../content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md) | S | 否 |

### Part I｜从 LLM 到可编程模型

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 01 | [模型调用到底发生了什么：LLM、Model API、Messages 与 Token](../content/ai-empowerment/agent-engineering-01-model-api-messages-token.md) | M | 否 |
| 02 | [Prompt Engineering：任务合同、角色、示例与边界](../content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md) | M | 否 |
| 03 | [Structured Output：让模型输出成为机器可消费的合同](../content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md) | L | 否 |
| 04 | [Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异](../content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md) | M | 否 |

### Part II｜从模型到 Agent

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 05 | [Function Calling 与 Tool Use：模型如何表达行动意图](../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md) | M | 否 |
| 06 | [Tool Runtime：Validate、Policy、Execute、Result 与 Trace](../content/ai-empowerment/agent-engineering-06-tool-runtime.md) | L | 否 |
| 07 | [MCP 与外部能力边界：协议解决什么，宿主仍需解决什么](../content/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md) | M | 否 |
| 08 | [Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop](../content/ai-empowerment/agent-engineering-08-agent-loop.md) | L | 否 |
| 09 | [Planning：Agent 为什么需要计划，又为什么不能迷信计划](../content/ai-empowerment/agent-engineering-09-planning.md) | M | 否 |
| 10 | [State Machine 与 Workflow：确定性骨架和 Agent Decision Point](../content/ai-empowerment/agent-engineering-10-state-machine-workflow.md) | L | 否 |
| 11 | [Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery](../content/ai-empowerment/agent-engineering-11-long-running-agent.md) | M | 否 |

### Part III｜Agent 的信息、状态与知识

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 12 | [Context Engineering：每一个 Step 到底应该看到什么](../content/ai-empowerment/agent-engineering-12-context-engineering.md) | L | 否 |
| 13 | Context Debugging：Packing、Compression、Pollution 与可重建性 | L | 否 |
| 14 | Working Memory 与 Investigation State：当前任务正在想什么 | L | 否 |
| 15 | Session、Long-term Memory 与 Project Memory：事实、经验和作用域 | M | 否 |
| 16 | Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite | M | 否 |
| 17 | Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt | M | 否 |

### Part IV｜Reliable Agent Engineering

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 18 | Evidence Contract：把自然语言推断变成可审计工程数据 | L | 否 |
| 19 | Permission、Approval、Human-in-the-loop 与 Sandbox | L | 否 |
| 20 | Budget Engineering：Token、Step、Cost 与 Latency | M | 否 |
| 21 | Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层 | L | 否 |
| 22 | Eval、Golden Dataset 与 Regression：修复以后还会不会再坏 | L | 否 |
| 23 | Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent | M | Advanced / Optional |

### Part V｜Harness Engineering

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 24 | 为什么最终需要 Harness：横切能力由谁承载 | L | 否 |
| 25 | Agent Runtime vs Harness：执行内核与工程控制面 | L | 否 |
| 26 | Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery | L | 否 |
| 27 | Harness 的设计取舍：可替换性、复杂度、Bloat 与演化 | M | 否 |

### Part VI｜DeepSeek Harness

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 28 | 怎样把 DeepSeek Harness 当作 Evidence-first 源码教材 | S | 否 |
| 29 | DeepSeek Harness 总图：从 Host 启动到一次 Agent Run | M | 否 |
| 30 | Everything is a Plugin：插件内核如何承载 Capability 与生命周期 | M | 否 |
| 31 | Profile、Bundle、Provider 与 Capability Seam | M | 否 |
| 32 | System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成 | L | 否 |
| 33 | Inbox、Turn、Step 与 Agent Loop | L | 否 |
| 34 | Append-only Session Event：Replay、Resume、Fork 与 Projection | L | 否 |
| 35 | Tool Registry 与 Tool Execution Pipeline | L | 否 |
| 36 | Cost、Compaction、Trace、Cancellation 与 Recovery | L | 否 |
| 37 | RAG、Skill、Workflow、Subagent 与 Web / Headless：核心事实和扩展映射 | M | 否 |

### Part VII｜BuildPilot Design

| ID | 标题 | 权重 | Optional |
|---|---|---|---|
| 38 | 游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent | M | 否 |
| 39 | 案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent | L | 否 |
| 40 | 案例 B：启动性能调查——设计一个长链路、多假设 Agent | L | 否 |
| 41 | 从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块 | L | 否 |
| 42 | BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位 | L | 否 |
| 43 | BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval | L | 否 |
| 44 | BuildPilot Design v1：设计评审、里程碑与退出条件 | S | 否 |

S 是 Bridge / Overview，M 是 Standard Core Lesson，L 是 Major Core Lesson。权重控制课程篇幅和证据投入，不是 Hugo front matter 的 `weight`。

## Engineering Labs 路线

| Lab | 插入位置 | 默认技术栈 | 核心观察 | 实现状态 |
|---|---|---|---|---|
| Lab 01 Structured Output | 03 后 | C# / .NET | Parse、Schema、Domain Validation | 未实现 |
| Lab 02 Tool Runtime | 06 后 | C# / .NET | Validate、Policy、Timeout、Result | 未实现 |
| Lab 03 Minimal Agent Loop | 08 后 | C# / .NET | Turn、Step、Observation、Stop | 未实现 |
| Lab 04 State Machine + Checkpoint | 11 后 | C# / .NET | State、Checkpoint、Resume、Cancellation | 未实现 |
| Lab 05 Context Debugging | 13 后 | C# / .NET | Context Snapshot、Pollution、Truncation | 未实现 |
| Lab 06 Trace + Eval | 22 后 | C# / .NET | Trace、Failure Layer、Regression | 未实现 |

Lab 结论只证明当前 Fixture 和实验，不自动升级为通用事实。Lab 生产入口见 `docs/agent-engineering-course/labs/README.md`。

## DeepSeek Harness Evidence Strategy

Part VI 必须执行：

1. 固定 Repository、commit SHA、读取日期和运行环境。
2. 区分 Official Docs、Pinned Source、Runtime Observation、Experiment、Inference 和 Design Proposal。
3. 每项源码 Claim 记录文件、符号、调用路径、反证搜索和版本限制。
4. 29 Architecture Overview 在取得 Repository Map、Startup Entry、Core Package Relationship 和至少一条 Host → Agent Run 调用路径之前保持 `BLOCKED`。
5. 静态调用链只可标 `SOURCE_CONFIRMED`；实际运行 Trace 才可标 `RUNTIME_CONFIRMED`。
6. 先写源码事实，再写架构解释，最后写 BuildPilot 的 ADOPT / SIMPLIFY / REJECT / DEFER。

DSH 扩展字段见 `docs/agent-engineering-course/templates/evidence-card-template.md`。

## BuildPilot Design v1 交付物

课程只交付设计，不交付 Runtime：

1. Problem Space 与 Script / Rule / Workflow / Agent 决策矩阵。
2. Unity Compile Golden Fixture 设计包。
3. Startup Investigation Scenario 设计包。
4. Context Diagram、模块图、依赖方向与关键时序。
5. Host、Harness、Runtime、Context、Capability、Domain Pack 职责表。
6. Tool、Skill、Workflow、Context Source、Working Memory 契约草案。
7. Evidence、Policy、Session、Trace、Budget、Recovery、Eval 治理闭环。
8. DSH ADOPT / SIMPLIFY / REJECT / DEFER 矩阵。
9. ADR、风险登记、开放问题和阻塞证据表。
10. M0-M3 路线图与退出条件。

BuildPilot 第一阶段保持只读、隔离 Fixture、显式审批后写入；不得把 Fixture 结果写成真实 Unity / Jenkins / 生产验证。

## BLOCKED 原则

- 缺少支撑关键行为结论的 Evidence 时，Article Status 必须进入 `BLOCKED`。
- `BLOCKED` 可以继续整理研究问题和 Outline Skeleton，但不允许进入 `DRAFTING` 并写成确定性行为结论。
- 解除阻塞必须补 Evidence Card、实验或明确缩小 Claim。
- 未验证内容只能标 `PARTIAL`、`INFERENCE` 或 `PROPOSAL`，不能写成 `CONFIRMED`。
- DSH 未绑定 pinned commit 时，Part VI 的源码行为结论全部 `BLOCKED`。

具体 Gate 见 `docs/agent-engineering-course/production-workflow.md`。

## 推荐阅读顺序

1. 00 建地图。
2. 01-11 建立 Model → Agent 的执行主链。
3. 12-17 建立 Context、状态和知识边界。
4. 18-22 建立 Reliable Agent 闭环。
5. 23 可选；24-27 推导 Harness。
6. 28-37 进行 Evidence-first DSH 源码学习。
7. 38-44 完成 BuildPilot Design v1。

## 推荐写作顺序

1. 先用 00 完成一次完整生产流程试点。
2. 冻结 28 的 pinned source 规则，并确认 29 的证据可得性。
3. 预先设计 38-40 的问题空间与两个案例，保证课程终点可追溯。
4. 按 01-11、12-17、18-22、24-27 推进主线和对应 Lab。
5. 在证据就绪后完成 28-37；23 最后按 Optional 插入。
6. 用 41-44 汇总 BuildPilot Design v1。

## 与相邻系列的边界

| 系列 | 该系列负责 | 本课程负责 |
|---|---|---|
| AI 赋能游戏开发 | 团队知识闭环、上下文实践、Skill、AI Coding 工作流和 Harness v0 | 从 Model 到 Agent / Harness 的系统建造课程 |
| Harness Engineering | v0 之后的 Growth、Bloat、Drift、Sunset、跨仓库与交付治理 | Harness 为什么出现、怎样分层，以及真实源码验证 |
| 交付工程 | 构建、制品、发布和生产证据闭环 | 把它们作为 Agent Tool / Evidence 来源，不重讲交付体系 |
| 性能工程 | 指标定义、采集、分析和优化 | 把启动性能作为多步调查案例，不重讲性能教程 |
| kb/ 知识沉淀 | 写后概念、实体、来源和发布覆盖地图 | 写前 Research / Evidence / Outline 生产资产 |

知识飞轮只覆盖 Agent 消费知识以及 Trace → Review → Knowledge / Skill / Eval 的接口；完整团队知识治理仍归 AI 赋能系列。

## 发布与编号约定

M0 不创建发布正文。未来进入 `PUBLISHED` 时默认：

- 系列入口：`content/ai-empowerment/agent-engineering-series-index.md`
- 正文：`content/ai-empowerment/agent-engineering-<id>-<slug>.md`
- `series`：`Agent Engineering`
- `primary_series`：`agent-engineering`
- `series_order`：`(ID + 1) × 10`，00 为 10，44 为 450
- Hugo `weight`：`3000 + series_order`，与课程 S / M / L 权重分离
- 发布图片：`static/images/agent-engineering/<id>-<slug>/`
- 工作中资产：文章 workspace 下的 `assets/`，首次产生资产时才创建

发布前仍以仓库 front matter、YAML 引号和 `relref` 规则为准。

## 维护规则

1. 本文件是课程结构唯一真相源。
2. `docs/agent-engineering-course/status.md` 是生产状态唯一真相源。
3. 通用写法仍以 `docs/article-writing-method.md` 为准，本课程不复制第二套写作方法。
4. 每篇开始时按需实例化一个 workspace，不批量创建空目录。
5. 正文发布后，先更新状态 tracker 和本计划的必要链接，再按需更新根 `doc-plan.md` 与发布侧索引。
6. 课程结构变化必须先经过新的 review 稿，不直接在生产过程中修改 canonical 目录。
