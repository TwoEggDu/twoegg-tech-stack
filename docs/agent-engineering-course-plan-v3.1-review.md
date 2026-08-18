# Agent Engineering 课程规划 v3.1（入口与结构校正版评审稿）

> 课程名：Agent Engineering｜从 LLM、Agent 到 Harness 的系统课程
>
> 状态：非 canonical 评审稿
>
> 日期：2026-08-18
>
> 输入：Agent Engineering 课程规划 v3（能力生长版评审稿）及 v3.1 评审意见

---

## 0. 文档状态

本稿是 v3 的小版本结构校正，不重新设计课程。v3 的能力生长主线、Context 边界、Reliable Agent 闭环、Harness 四篇与 BuildPilot 推导顺序保持稳定。

本稿新建，不覆盖：

- docs/game-agent-engineering-series-outline.md
- docs/agent-engineering-course-outline-v2.md
- docs/agent-engineering-course-plan-v3-review.md
- docs/ai-empowerment-series-plan.md
- docs/harness-engineering-series-plan.md
- 根 doc-plan.md

当前只做课程设计，不写正文，不建立 content/，不实现 BuildPilot Runtime。

## 1. 一句话课程定位

先用一篇世界地图帮助熟悉软件工程但 Agent 术语不系统的读者辨认系统层次，再从 Model API 与 Structured Output 开始，亲手理解 Tool Runtime、Agent Loop、Planning、State、Context、Memory、RAG、Reliable Agent 与 Harness 为什么依次出现；再用锁定版本的 DeepSeek Harness 验证这些抽象，最终独立完成 BuildPilot Design v1。

## 2. 课程解决什么问题

这门课不解决“哪个 Agent 产品最好用”，而解决三个断层：

1. 使用断层：会用 Codex / Claude Code，却不能解释一次 Agent Run 怎样推进。
2. 架构断层：知道 Tool、RAG、Memory 等术语，却不知道它们为什么出现、由谁承载。
3. 迁移断层：能看懂某个框架的类名，却不能判断设计的收益、代价及是否适合自己的专用 Agent。

课程的答案不是术语表，而是一条能力生长链：

~~~text
Model 能生成
→ 输出可被程序消费
→ 模型能表达行动意图
→ 宿主安全执行行动
→ 决策、行动、观察形成循环
→ 长任务需要状态、流程与检查点
→ 多 Step 让 Context 和记忆成为问题
→ 真实环境让权限和沙箱成为问题
→ 非确定性让 Trace、Eval 与回归成为问题
→ 横切能力需要统一承载
→ Harness 自然出现
~~~

## 3. 目标读者

- 熟悉 C# / Unity、接口、状态机、异步和工程工具链
- 可能深度使用过 Coding Agent
- 没有系统学习 Agent Runtime / Harness
- 想设计游戏研发、构建、交付或诊断 Agent

## 4. 前置知识

默认具备：

- HTTP、JSON、异步调用、错误处理
- C# 接口、依赖注入、状态机基本思想
- Unity Build、Jenkins、日志、CI/CD 基本经验

不要求：

- 不要求先会 Python Agent 框架
- 不要求训练模型或部署推理服务
- 不要求使用向量数据库
- 不要求已经开发 BuildPilot

## 5. 最终学习目标

课程结束后，读者能够：

1. 解释从 Model API 到 Agent System 的能力演进。
2. 区分 Function Calling 与 Tool Runtime、Workflow 与 Agent Loop、Runtime 与 Harness。
3. 设计 Working Memory、Session、Long-term Memory、KB、RAG 与 Context 的边界。
4. 用 Evidence / Hypothesis / Diagnosis / Verification 构造可审计结果。
5. 建立 Permission、Approval、Sandbox、Budget、Trace、Replay、Eval 与 Regression 闭环。
6. Evidence-first 地阅读 DeepSeek Harness，而不是复述 API。
7. 判断哪些游戏研发问题继续使用 Rule / Script / Workflow。
8. 完成 BuildPilot Design v1，并诚实区分 Design、Pilot 与 Runtime。

## 6. 核心教学原则

1. 新概念必须由前一能力暴露的问题引出。
2. 先讲问题空间，再讲抽象，再落工程结构。
3. 不把单一生态实现写成通用标准，尤其是 Skill、Harness 与 Memory。
4. 每组知识后安排一个独立小 Lab；Lab 验证概念，不偷偷开发 BuildPilot。
5. DeepSeek Harness 只承担源码教材角色。
6. BuildPilot 在前半程只是轻量参照，从 Part VII 才成为毕业设计主体。
7. 任何行为性结论必须注明文档、源码、实验或 Trace 证据等级。
8. Agent 不替代已有确定性工具；它连接事实源并处理调查与决策缝隙。
9. 00 只提供导航级定义；08 建立 Agent 运行机制；24-27 建立 Harness 架构认知，避免重复从零定义。

## 7. Agent Engineering 总体心智模型

~~~text
Host / Application
        ↓
Harness：能力组合、边界、预算、审计、恢复
        ↓
Agent Runtime
├── Model Adapter
├── Structured I/O
├── Tool Runtime
├── Agent Loop
└── State / Workflow
        ↓
Context System
├── Working Memory
├── Session
├── Long-term Memory
├── Knowledge / RAG
└── Skill
        ↓
External Systems：Files / Jenkins / Unity / Metrics / KB
~~~

这里的边界是课程抽象，不声称所有框架使用相同类名。

00 篇先给这张图的地图级版本；后续课程再从 Model 开始逐层把它重新“长出来”。

## 8. Knowledge Dependency Graph

~~~text
00 Agent Engineering World Map
  ↓
01 Model / API / Messages / Token
  ↓
02 Prompt / Roles / Context Window
  ↓
03 Structured Output / Schema
  ↓
04 Streaming / Error / Retry / Model Adapter
  ↓
05 Function Calling / Tool Use
  ↓
06 Tool Runtime
  ↓
07 MCP / External Capability
  ↓
08 Agent Loop / Turn / Step / Observation
  ↓
09 Planning / Re-planning
  ↓
10 State Machine / Workflow / Agent Decision Point
  ↓
11 Checkpoint / Long-running / Cancellation
  ↓
12 Context Engineering
  ↓
13 Context Debugging / Compression / Pollution
  ↓
14 Working Memory / Investigation State
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
20 Token / Step / Cost / Latency Budget
  ↓
21 Trace / Replay / Failure Taxonomy
  ↓
22 Eval / Golden Dataset / Regression
  ↓
23 Multi-Agent（Advanced / Optional，可旁路）
  ↓
24-27 Harness Engineering
  ↓
28 DSH Evidence-first Reading
  ↓
29 DSH Architecture Overview
  ↓
30-37 DeepSeek Harness Subsystems
  ↓
38-44 BuildPilot Design v1
~~~

主线旁路：完成 22 后可以直接进入 24；23 Multi-Agent 不构成 Harness 的必修前置。

关键回收点：

| 通用问题 | 文章 | DSH 回收 | BuildPilot 回收 |
|---|---|---|---|
| Model I/O | 01-04 | 32、33 | 41-44 |
| Tool Runtime | 05-07 | 35 | 39、42 |
| Loop / Planning / State / Workflow | 08-11 | 33-34、37 | 40、42 |
| Context / Memory / Knowledge | 12-17 | 32、34、37 | 40、42 |
| Reliable Agent | 18-23 | 34-36 | 39-44 |
| Harness | 24-27 | 28-37 | 41-44 |

## 9. Concept Progressive Definition

同一个概念在课程中经历“知道它在哪里 → 理解机制 → 看源码 → 自己设计”的认知升级。00 只负责 Introduction，不抢后续文章的正式定义。

| 概念 | 第一次出现 | 正式建立 | 源码验证 | BuildPilot 回收 |
|---|---|---|---|---|
| Agent | 00 世界地图 | 08 Agent Loop | 29 总图、33 Loop | 38-40 Investigation |
| Agentic | 00 世界地图 | 08-10 作为系统特征落到机制 | 29、33 验证实际自主边界 | 38 任务分类 |
| Copilot | 00 世界地图 | 不设统一架构定义，后续只作产品交互语境 | 29 判断 DSH 是否跨 Product 层 | 38 判断辅助式入口 |
| Agent Runtime | 00 世界地图 | 08-11 Loop / State / Long-running | 29、33-36 | 41 Architecture |
| Harness | 00 世界地图 | 24-27 Harness Engineering | 28-37 | 43 Governance |
| Host | 00 世界地图 | 25 Runtime vs Harness | 29 Architecture Overview、37 Mapping | 41 CLI Host |
| Context | 00 世界地图 | 12-13 Context Engineering / Debugging | 32 PromptContext | 42 Context Plane |
| Tool | 00 世界地图 | 05-06 Function Calling / Tool Runtime | 35 Tool Pipeline | 39、42 Capability |
| Workflow | 00 世界地图 | 10 State Machine / Workflow | 37 Extension Mapping | 42 Workflow Gate |
| Skill | 00 世界地图 | 17 Skill Engineering | 37 Extension Mapping | 42 Domain Pack |
| Memory | 00 世界地图 | 14-16 Working / Session / Knowledge | 34 Session、37 Mapping | 40、42 Memory Design |
| RAG | 00 世界地图 | 16 Knowledge Base / RAG | 37 Extension Mapping | 42 Knowledge Source |
| Trace / Replay / Eval | 00 只定位为可靠性能力 | 21-22 | 34、36 | 43 Governance |

## 10. 最终课程骨架

### 00｜课程导论

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 00 | Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host | S | 否 |

### Part I｜从 LLM 到可编程模型

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 01 | 模型调用到底发生了什么：LLM、Model API、Messages 与 Token | M | 否 |
| 02 | Prompt Engineering：任务合同、角色、示例与边界 | M | 否 |
| 03 | Structured Output：让模型输出成为机器可消费的合同 | L | 否 |
| 04 | Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异 | M | 否 |

### Part II｜从模型到 Agent

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 05 | Function Calling 与 Tool Use：模型如何表达行动意图 | M | 否 |
| 06 | Tool Runtime：Validate、Policy、Execute、Result 与 Trace | L | 否 |
| 07 | MCP 与外部能力边界：协议解决什么，宿主仍需解决什么 | M | 否 |
| 08 | Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop | L | 否 |
| 09 | Planning：Agent 为什么需要计划，又为什么不能迷信计划 | M | 否 |
| 10 | State Machine 与 Workflow：确定性骨架和 Agent Decision Point | L | 否 |
| 11 | Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery | M | 否 |

### Part III｜Agent 的信息、状态与知识

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 12 | Context Engineering：每一个 Step 到底应该看到什么 | L | 否 |
| 13 | Context Debugging：Packing、Compression、Pollution 与可重建性 | L | 否 |
| 14 | Working Memory 与 Investigation State：当前任务正在想什么 | L | 否 |
| 15 | Session、Long-term Memory 与 Project Memory：事实、经验和作用域 | M | 否 |
| 16 | Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite | M | 否 |
| 17 | Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt | M | 否 |

### Part IV｜Reliable Agent Engineering

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 18 | Evidence Contract：把自然语言推断变成可审计工程数据 | L | 否 |
| 19 | Permission、Approval、Human-in-the-loop 与 Sandbox | L | 否 |
| 20 | Budget Engineering：Token、Step、Cost 与 Latency | M | 否 |
| 21 | Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层 | L | 否 |
| 22 | Eval、Golden Dataset 与 Regression：修复以后还会不会再坏 | L | 否 |
| 23 | Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent | M | **Advanced / Optional** |

### Part V｜Harness Engineering

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 24 | 为什么最终需要 Harness：横切能力由谁承载 | L | 否 |
| 25 | Agent Runtime vs Harness：执行内核与工程控制面 | L | 否 |
| 26 | Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery | L | 否 |
| 27 | Harness 的设计取舍：可替换性、复杂度、Bloat 与演化 | M | 否 |

### Part VI｜DeepSeek Harness

| 编号 | 标题 | 权重 | Optional |
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

| 编号 | 标题 | 权重 | Optional |
|---|---|---|---|
| 38 | 游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent | M | 否 |
| 39 | 案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent | L | 否 |
| 40 | 案例 B：启动性能调查——设计一个长链路、多假设 Agent | L | 否 |
| 41 | 从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块 | L | 否 |
| 42 | BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位 | L | 否 |
| 43 | BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval | L | 否 |
| 44 | BuildPilot Design v1：设计评审、里程碑与退出条件 | S | 否 |

总计 45 篇，其中 1 篇 Advanced / Optional。S / M / L 用于控制写作深度，不代表主题重要性排序。

## 11. Part I｜从 LLM 到可编程模型（01-04）

核心问题：模型如何从“生成文字”变成程序可以稳定调用、解析和替换的工程组件。

## 12. Part II｜从模型到 Agent（05-11）

核心问题：模型怎样表达行动，宿主怎样安全执行，Decide → Act → Observe 怎样形成 Loop，长任务怎样获得 State、Workflow 与 Checkpoint。

## 13. Part III｜Agent 的信息、状态与知识（12-17）

核心问题：Agent 每个 Step 看什么；当前任务状态、会话事实、长期经验、权威知识和按需方法分别放在哪里。

## 14. Part IV｜Reliable Agent Engineering（18-23）

核心问题：怎样把自然语言推断变成可审计数据，并为真实环境建立权限、成本、Trace、Eval 与回归。

## 15. Part V｜Harness Engineering（24-27）

核心问题：当 Model、Tool、Context、State、Policy、Trace、Eval 和 Recovery 同时存在时，由谁统一承载与组织。

## 16. Part VI｜DeepSeek Harness 源码教材（28-37）

核心问题：一个真实 Harness 怎样把前五部分的抽象实体化；每篇都从已有问题进入，不做目录考古。

## 17. Part VII｜BuildPilot Design（38-44）

核心问题：从真实游戏生产管线推导一个专用只读诊断 Agent，最终交付 Design v1，不在课程内完成 Runtime。

## 18. Engineering Labs 路线

| Lab | 插入位置 | 独立输入 | 观察点 | 预期失败 |
|---|---|---|---|---|
| Lab 01 Structured Output | 03 后 | 三条自然语言订单 / 日志摘要 | Parse、Schema、业务校验 | 合法 JSON 但语义非法 |
| Lab 02 Tool Runtime | 06 后 | Calculator + ReadOnly File Tool | Validate、Policy、Timeout、Result | 越界路径、大结果、取消 |
| Lab 03 Minimal Agent Loop | 08 后 | Mock Build Log + 两只 Tool | Turn、Step、Observation、Stop | 无限循环、伪完成 |
| Lab 04 State Machine + Checkpoint | 11 后 | Fake Long-running Investigation | 状态转移、Resume、取消 | 重试副作用、丢失检查点 |
| Lab 05 Context Debugging | 13 后 | 正确 / 过期 / 冲突资料包 | Context Snapshot、污染、截断 | 正确信息被淹没 |
| Lab 06 Trace + Eval | 22 后 | Simple Investigation Golden Set | Trace、Failure Layer、Regression | 答案正确但证据伪造 |

所有 Lab：

- 使用独立小项目或 Fixture
- 不连接真实 Unity / Jenkins
- 不写入 BuildPilot 仓库
- 有可观察输出和失败样本
- 结论只证明当前机制，不证明产品价值

## 19. 完整 Article Cards

### 00｜Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host

#### 1. 本篇定位

课程导论与导航篇。回答“我打开 Claude Code、Codex、DeepSeek Harness 或普通 LLM App 时，应该从哪些系统层次理解它”，只建立地图，不提前穷举定义。

#### 2. 为什么现在学它

目标读者有软件工程基础，却可能同时听过 Agent、Agentic、Copilot、Runtime、Harness、Tool、Skill、RAG 和 Memory。如果没有地图，课程从 Model API 开始虽正确，却容易让读者不知道这些底层能力最终长成什么。

#### 3. 学完以后应该能回答的问题

- LLM / Model 与使用它的 AI Application 有什么区别？
- Copilot 为什么是产品交互定位，而不是统一架构标准？
- Agent 与 Agentic 分别描述什么？
- Agent Runtime、Harness、Host / Product 大概位于哪一层？
- Prompt、Context、Tool、Skill、Workflow、Memory、RAG 大概放在哪里？
- 后续课程为什么仍要从 Model 开始重新学习？

#### 4. 前置知识

不要求 Agent 前置知识；只需具备普通应用、API 和状态的基本概念。

#### 5. 核心概念

LLM / Model、AI Application、AI Feature、Copilot、Agent、Agentic、Agent Runtime、Harness、Host / Product、Prompt、Context、Tool、Skill、Workflow、Memory、RAG。

#### 6. 核心心智模型

~~~text
User Goal
   ↓
Host / Product：CLI / Web / IDE / Unity Editor / CI
   ↓
Harness：Context / Policy / Session / Budget / Trace / Recovery
   ↓
Agent Runtime：Loop / Model Call / Tool Dispatch / State Update
   ↓
Model + Tool + State
   ↓
External World
~~~

#### 7. 正文详细框架

1. 从一个普通 LLM App 开始  
   1.1 Model 生成输出，Application 仍可使用完全确定的控制流。  
   1.2 材料：普通问答功能与 Tool-using Application 对照。
2. Copilot、Agent 与 Agentic  
   2.1 Copilot 强调辅助人完成任务，不是严格架构层级。  
   2.2 Agent 是在目标、状态、行动与反馈之间循环决定下一步动作的执行系统。  
   2.3 Agentic 是形容词，表示具有一定自主判断、规划、行动、反馈或适应特征；Agentic Workflow / Coding / RAG 不自动构成统一产品类型。
3. Runtime、Harness 与 Host  
   3.1 Runtime 执行 Loop、Model Call、Tool Dispatch 与 State Update。  
   3.2 Harness 提供能力组合和运行约束；Host / Product 提供 CLI、Web、IDE、Unity Editor 或 CI 入口。  
   3.3 材料：同一 Runtime 被两个 Host 使用的结构图。
4. 横向能力放在哪里  
   4.1 Prompt / Context 是模型当前看到的信息组织。  
   4.2 Tool 是可执行能力，Skill 是按需领域方法，Workflow 是受约束流程。  
   4.3 Memory 与 RAG 分别涉及跨时信息保存和按需检索；这里只定位，不在本篇正式定界。
5. 地图不是术语百科  
   5.1 不用产品名称反推统一架构，不把所有含 LLM 的应用都叫 Agent。  
   5.2 桥接句：接下来整门课会从最底层 Model 开始，一层层重新把这张图长出来。

#### 8. Engineering Lab / 示例

不设 Lab。选择一个普通 LLM Chat、一个 Coding Agent 和一个 CI Agent 概念方案，只标注可确认的层，不根据产品宣传猜内部实现。

#### 9. 与 DeepSeek Harness 的关系

本篇只把 DSH 放在待验证的 Runtime / Harness / Product 多层候选位置；29 将根据 pinned commit 和调用路径正式判断。

#### 10. 与 BuildPilot 的关系

BuildPilot 在本篇只作为未来的专用工程 Agent 例子，不介绍架构，不启动实现。

#### 11. Evidence 要求

地图级通用概念参考权威资料；涉及具体产品层次时必须注明公开事实或待验证，不凭名称断言。

#### 12. 最容易混淆的概念

Model ≠ Application；Copilot ≠ Agent 的严格上下级；Agentic ≠ 独立产品类型；Runtime ≠ Harness；Harness ≠ Host；RAG ≠ Memory。

#### 13. 本篇明确不讲什么

不讲 Agent Loop 细节，不列 Harness 能力全集，不定义各类 Memory 生命周期，不做产品横评。

#### 14. 学习检查

- 一个调用模型做摘要的按钮为什么未必是 Agent？
- Agentic Workflow 是否一定包含一个独立 Agent Runtime？
- CLI 与 Agent Runtime 是同一层吗？
- 为什么看完地图后仍需从 Model API 开始？

#### 15. 篇幅等级 / 课程权重

**S（Bridge / Overview）**。只负责降低术语进入门槛并提供全课导航，不穷举机制。

#### 16. 概念成熟度

**Introduction**。Agent、Agentic、Copilot、Runtime、Harness、Host 以及横向能力均只建立位置感，正式机制留给后续文章。

## Part I｜从 LLM 到可编程模型

### 01｜模型调用到底发生了什么：LLM、Model API、Messages 与 Token

#### 1. 本篇定位

基础篇。建立课程最底层的调用模型，读者先看清一次请求，再讨论 Agent。

#### 2. 为什么现在学它

课程入口尚无前置。必须先知道模型只根据当前请求生成输出，才能理解后续状态、Tool 和 Harness 都来自宿主工程。

#### 3. 学完以后应该能回答的问题

- LLM、Model 与 Model API 分别是什么？
- System / User / Assistant Message 在请求中承担什么？
- Token、Context Window 和输出上限如何影响调用？
- Temperature 等参数改变什么，不改变什么？
- 为什么模型调用本身没有任务状态？

#### 4. 前置知识

HTTP、JSON、异步调用。

#### 5. 核心概念

LLM、Model、Inference、Message、Role、Token、Context Window、Sampling Parameter、Usage。

#### 6. 核心心智模型

~~~text
Messages + Parameters
→ Model Request
→ Generated Response + Usage
~~~

#### 7. 正文详细框架

1. 从聊天界面拆回 API  
   1.1 回答：产品 UI 隐藏了哪些请求字段；论点：模型只看到被提交的输入。  
   1.2 材料：一张 UI → HTTP → Model 时序图，一份去敏请求 JSON。
2. Message 与 Role  
   2.1 回答：不同 Role 如何组织指令和内容；论点：Role 是协议语义，不等于不可违反的权限。  
   2.2 材料：三组消息顺序对照，不需要代码。
3. Token 与 Context Window  
   3.1 回答：输入、输出、工具定义为何共享预算；论点：窗口大不代表相关信息一定被正确使用。  
   3.2 材料：Token 估算表和超限失败示例。
4. Parameters 与非确定性  
   4.1 回答：采样参数怎样改变输出分布；论点：它们不能补回缺失事实。  
   4.2 材料：固定输入多次调用的小实验，标注模型版本。
5. 本篇收束  
   5.1 建立问题：程序怎样可靠消费自然语言输出？  
   5.2 引向 02 的 Prompt / Message Contract 和 03 的 Structured Output。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab。示例是同一请求改变消息顺序和输出上限，观察响应与 Usage。

#### 9. 与 DeepSeek Harness 的关系

32、33 会重新遇到 Model Adapter、请求装配和 Step 调用。

#### 10. 与 BuildPilot 的关系

未来 BuildPilot 必须记录模型、参数、Usage 和请求版本，但此处不设计其 Runtime。

#### 11. Evidence 要求

模型 API 官方文档、Message / Usage Schema、固定日期的 Context Window 说明。涉及参数行为时需小实验。

#### 12. 最容易混淆的概念

LLM ≠ Agent；Message Role ≠ Permission；Context Window ≠ Memory；Token ≠ 汉字数。

#### 13. 本篇明确不讲什么

不讲训练、GPU 推理部署、模型横评和 Agent Loop。

#### 14. 学习检查

- 请求中没有项目版本，模型能否凭“更大窗口”知道当前版本？
- System Message 是否构成不可绕过的安全边界？
- 同一请求输出不同，哪些变量需要先检查？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。建立后续模型调用的共同底座，但不承担复杂实验。

#### 16. 概念成熟度

**Foundation**。正式建立 Model API、Message、Token 与 Context Window。

### 02｜Prompt Engineering 的边界：System Prompt、指令与请求合同

#### 1. 本篇定位

基础原理篇。讲清 Prompt 怎样表达任务，同时明确 Prompt 不是 Agent Engineering 的全部。

#### 2. 为什么现在学它

01 已知道模型只看请求。新问题是：怎样把目标、约束、输入和失败语义表达得足够明确，供后续 Schema 与 Tool 使用。

#### 3. 学完以后应该能回答的问题

- Prompt Engineering 真正解决什么？
- System Prompt 与 User Input 怎样分工？
- Few-shot 示例为什么既能帮助也能污染？
- Prompt 为什么不能代替权限、状态和事实校验？
- Prompt 版本怎样进入 Eval？

#### 4. 前置知识

01 的 Messages、Role、Token。

#### 5. 核心概念

Instruction、Goal、Constraint、Input Delimiter、Few-shot、Failure Semantics、Prompt Version、Injection。

#### 6. 核心心智模型

~~~text
Goal + Constraints + Inputs + Examples + Output Contract + Failure Semantics
→ Prompt
~~~

#### 7. 正文详细框架

1. 从模糊请求到任务合同  
   1.1 回答：模型缺少哪些任务信息；论点：先定义成功和失败，再润色措辞。  
   1.2 材料：一条 Unity 日志摘要 Prompt 的前后对照。
2. System / User / Dynamic Input  
   2.1 回答：稳定规则、用户目标和运行事实分别放哪；论点：不要把动态事实固化进长期 Prompt。  
   2.2 材料：分层请求图。
3. 示例与输出要求  
   3.1 回答：Few-shot 怎样改变格式和决策；论点：示例也占 Context，并可能携带旧假设。  
   3.2 材料：有 / 无示例的对照输出。
4. Prompt 解决不了什么  
   4.1 权限、Tool 执行、状态持久化、当前事实和 Eval 均需宿主机制。  
   4.2 材料：反模式表，不需要代码。
5. 版本与测试  
   5.1 Prompt ID、模板变量、变更原因和固定 Fixture。  
   5.2 引出 03：自然语言合同仍然难被程序可靠解析。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；使用固定输入做 Prompt A/B，只观察格式稳定性，不宣称准确率提升。

#### 9. 与 DeepSeek Harness 的关系

32 会把本篇映射到 System Prompt Assembly 与多来源 Section。

#### 10. 与 BuildPilot 的关系

影响 Harness Identity、Deployment Persona 和任务 Prompt 的分离。

#### 11. Evidence 要求

模型官方 Prompt / Message 指南、Prompt Injection 权威资料、A/B Fixture。

#### 12. 最容易混淆的概念

Prompt ≠ Context；System Prompt ≠ Policy；Few-shot ≠ Knowledge Base；措辞优化 ≠ 事实更新。

#### 13. 本篇明确不讲什么

不做提示词技巧大全，不讲 RAG、Memory 和 Context 生命周期。

#### 14. 学习检查

- “不要删除文件”写进 Prompt，是否已经安全？
- 项目分支每天变化，应该固化进 System Prompt 吗？
- Prompt A 输出更整齐，是否已经证明诊断更正确？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 03｜Structured Output：让模型输出成为机器可消费的合同

#### 1. 本篇定位

基础核心篇。把自然语言结果升级为 Schema 可解析、可校验、可评测的数据。

#### 2. 为什么现在学它

02 已能表达任务，但程序仍只能读取一段文本。Tool Call、Action、Workflow State、Evidence 和 Eval 都要求明确字段。

#### 3. 学完以后应该能回答的问题

- Structured Output 比“请输出 JSON”多了什么？
- JSON Schema 能保证什么，不能保证什么？
- Parse、Schema Validation 与 Domain Validation 怎样分层？
- Refusal、截断和修复重试怎样处理？
- 为什么它是 Tool Call 与 Evidence Contract 的前置？

#### 4. 前置知识

01 Model Response；02 Output Contract。

#### 5. 核心概念

Structured Output、JSON Schema、Typed DTO、Parse、Schema Validation、Domain Validation、Refusal、Repair。

#### 6. 核心心智模型

~~~text
Candidate Output
→ Parse
→ Schema Validate
→ Domain Validate
→ Accept / Repair / Fail
~~~

#### 7. 正文详细框架

1. 自然语言为何阻断工程链  
   1.1 回答：程序怎样判断字段、状态和引用；论点：可读不等于可消费。  
   1.2 材料：自由文本诊断与 DTO 对照图。
2. Schema 的边界  
   2.1 回答：类型、必填、枚举、嵌套怎样约束；论点：Schema 不验证事实真实性。  
   2.2 材料：一份简化 DiagnosisCandidate Schema 和错误样本。
3. 三层 Validation  
   3.1 Parse：语法；Schema：结构；Domain：文件、ID、状态等业务不变量。  
   3.2 材料：C# 伪代码与失败分类表。
4. Refusal、Truncation 与 Repair  
   4.1 回答：哪些输出可修复重试，哪些应停止；论点：修 JSON 不能修证据不足。  
   4.2 材料：四条失败 Fixture。
5. Structured Output 的下游  
   5.1 Tool Call、Action、Workflow State、Evidence 与 Eval。  
   5.2 引出 Lab 01 和第 05 篇 Function Calling。

#### 8. Engineering Lab / 示例

Lab 01 Structured Output：

- 目的：区分 Parse / Schema / Domain Validation。
- 输入：三条订单或日志摘要。
- 观察：模型原始输出、解析结果、Validation 错误。
- 预期失败：合法 JSON 引用不存在的 ID。
- 结论：结构正确不是语义正确。

#### 9. 与 DeepSeek Harness 的关系

33、35 会重新遇到模型 Step 输出、Tool 参数和 Result Validation。

#### 10. 与 BuildPilot 的关系

为 Evidence、Hypothesis、DiagnosisResult 提供机器合同基础。

#### 11. Evidence 要求

Structured Output 官方 API、JSON Schema Specification、Lab 01 Fixture。**BLOCKED：完成 Lab 01 后才能写行为结论。**

#### 12. 最容易混淆的概念

JSON 文本 ≠ Structured Output；Schema Valid ≠ Domain Valid；Structured Result ≠ Verified Result。

#### 13. 本篇明确不讲什么

不讲 Tool 执行、Agent Loop 和 BuildPilot 最终字段。

#### 14. 学习检查

- 输出合法但 Evidence ID 不存在，应该在哪层失败？
- 模型拒绝回答时，是否应强制修复成空 JSON？
- 为什么 Enum 可以减少歧义，却不能证明状态真实？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 04｜Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异

#### 1. 本篇定位

基础工程篇。把单一 API 调用包装成可替换、可观察的 Model Capability。

#### 2. 为什么现在学它

03 已有结构化合同，但真实 Provider 在消息格式、Streaming、错误和 Usage 上不同。后续 Agent Loop 不应绑定一家 SDK。

#### 3. 学完以后应该能回答的问题

- Model Adapter 应封装什么，不能吞掉什么？
- Streaming 与最终 Structured Result 怎样共存？
- 哪些模型错误可以 Retry？
- LLM Gateway 与 Model Adapter 有什么差别？
- Provider 切换为何不能只替换 URL？

#### 4. 前置知识

01-03。

#### 5. 核心概念

Model Adapter、Gateway、Streaming Event、Finish Reason、Rate Limit、Transient Error、Retry Policy、Capability Negotiation。

#### 6. 核心心智模型

~~~text
Domain Request
→ Model Adapter
→ Provider API / Stream
→ Normalized Events + Final Result + Usage
~~~

#### 7. 正文详细框架

1. Provider 差异为何会上溢  
   1.1 消息、Tool、Schema、Usage 与错误码差异。  
   1.2 材料：两家官方 API 能力矩阵。
2. Adapter 边界  
   2.1 规范化请求、响应、错误和 Usage；领域 DTO 留在上层。  
   2.2 材料：接口图和少量 C# 伪代码。
3. Streaming  
   3.1 回答：增量文本、Tool 参数片段、Usage 和完成事件如何处理。  
   3.2 材料：事件时间线；说明与取消的后续关系。
4. Error / Retry  
   4.1 限流、网络、超时、拒绝、Schema 失败的分类。  
   4.2 论点：业务重试与传输重试分层；材料：决策表。
5. Gateway  
   5.1 集中路由、凭证、限流、审计；不等于 Agent Runtime。  
   5.2 引出 05：模型接下来怎样表达“我要行动”。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；可用 Fake Provider 模拟增量事件、429 和截断。

#### 9. 与 DeepSeek Harness 的关系

31、33 会研究 Provider / Capability Seam 和 Agent Step 如何调用 Model。

#### 10. 与 BuildPilot 的关系

影响 IModelAdapter、Usage 归一化和未来模型路由，不要求现在实现 Gateway。

#### 11. Evidence 要求

至少两家官方 API、Streaming Event Schema、错误码和 Retry 指南。涉及具体能力必须标日期。

#### 12. 最容易混淆的概念

Adapter ≠ Gateway；Streaming ≠ Agent Event；Retry ≠ Recovery；Provider Capability ≠ 领域合同。

#### 13. 本篇明确不讲什么

不搭建 API 网关服务，不讲负载均衡和模型部署。

#### 14. 学习检查

- Provider 只返回流式 Tool 参数片段，哪层负责拼装？
- 429 与 Domain Validation 失败应使用同一 Retry 吗？
- 领域 DiagnosisResult 为什么不应直接继承 SDK Response？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

## Part II｜从模型到 Agent

### 05｜Function Calling 与 Tool Use：模型如何表达行动意图

#### 1. 本篇定位

Agent 能力起点篇。区分“模型建议调用”与“宿主实际执行”。

#### 2. 为什么现在学它

03 让输出可被程序解析，04 让模型可替换。新问题是模型如何从生成答案转向选择外部能力。

#### 3. 学完以后应该能回答的问题

- Function Calling 是协议还是执行系统？
- Tool Schema 怎样影响选择和参数？
- Tool Call、Tool Result 与普通消息有什么关系？
- 模型可以请求不存在或不允许的 Tool 吗？
- Tool Use 为什么还不是完整 Agent？

#### 4. 前置知识

03 Structured Output；04 Adapter。

#### 5. 核心概念

Function Calling、Tool Use、Tool Schema、Tool Choice、Tool Call ID、Arguments、Tool Result。

#### 6. 核心心智模型

~~~text
Available Tool Schemas
→ Model emits Tool Call Intent
→ Host decides whether / how to execute
→ Tool Result returns to model
~~~

#### 7. 正文详细框架

1. 从结构化结果到行动意图  
   1.1 回答：Tool Call 与业务 Structured Result 的差别。  
   1.2 材料：消息时序图。
2. Tool Schema 与选择  
   2.1 名称、描述、参数和枚举如何改变模型选择。  
   2.2 材料：两版 Calculator Schema 对照实验。
3. Host 的决定权  
   3.1 模型只能提出 intent；注册、权限、执行均在宿主。  
   3.2 材料：伪造 deleteFile Call 的负例。
4. Tool Result 回注  
   4.1 Call ID、错误、结果内容与下一次请求。  
   4.2 论点：原始 Result 仍需 Runtime 处理。
5. 为什么还不是 Agent  
   5.1 一次 Tool Use 可以是普通应用；持续 Loop 与状态尚未出现。  
   5.2 引出 06 Tool Runtime。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；Calculator 示例只验证 Tool Call intent，不直接执行副作用。

#### 9. 与 DeepSeek Harness 的关系

35 会回收到 Tool Registry 与 Pipeline 的模型侧入口。

#### 10. 与 BuildPilot 的关系

决定模型只能请求 parse/read/search，Harness 保留执行权。

#### 11. Evidence 要求

Function Calling 官方文档、Tool Schema Fixture、消息 Trace。

#### 12. 最容易混淆的概念

Function Calling ≠ Tool Runtime；Tool Call ≠ 已执行；Tool Result ≠ Evidence；Tool Use ≠ Agent Loop。

#### 13. 本篇明确不讲什么

不讲权限、超时、MCP Transport 和多 Step。

#### 14. 学习检查

- 模型生成 deleteFile Tool Call，文件是否已删除？
- Tool 参数符合 Schema，是否意味着允许执行？
- 一次查天气并回答的应用一定是 Agent 吗？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 06｜Tool Runtime：Validate、Policy、Execute、Result 与 Trace

#### 1. 本篇定位

核心工程篇。建立模型意图到真实执行之间不可省略的运行管线。

#### 2. 为什么现在学它

05 暴露了 Tool Call 只是候选意图。真实环境要求参数规范化、权限、超时、取消、输出校验和审计。

#### 3. 学完以后应该能回答的问题

- Tool 为什么不是普通函数包装？
- Model-visible Schema 与 Host Metadata 为什么分开？
- Tool Runtime 怎样处理副作用、幂等和取消？
- Result 怎样分别供模型、UI 和 Trace 使用？
- Policy 冲突为什么必须 fail closed？

#### 4. 前置知识

03 Validation；05 Function Calling。

#### 5. 核心概念

ToolDefinition、Registry、Canonical Arguments、Policy、Permission、Timeout、Cancellation、Idempotency、Result Validation、Render、Trace。

#### 6. 核心心智模型

~~~text
Call → Canonicalize → Validate → Policy
→ Execute → Validate Result → Render / Spill → Trace
~~~

#### 7. 正文详细框架

1. 概率性调用者带来的风险  
   1.1 参数合法但危险、时机错误、重复副作用。  
   1.2 材料：普通函数调用与 Tool Runtime 对照图。
2. Definition 与 Registry  
   2.1 模型视图和 Host-only 风险元数据。  
   2.2 材料：ReadOnlyFileTool 定义伪代码。
3. Execute 前  
   3.1 Canonicalize、Schema / Domain Validate、Tool / Resource Policy。  
   3.2 材料：路径 traversal 和 junction 负例。
4. Execute 中  
   4.1 Timeout、Cancellation、并发、幂等与错误分类。  
   4.2 材料：CancellationToken 伪代码。
5. Execute 后  
   5.1 Result Schema、敏感信息、裁剪、Spill、Model / UI / Trace 视图。  
   5.2 材料：大文件结果处理图。
6. Policy 合并  
   6.1 Allow / Deny / Ask；Deny 不可被后续覆盖。  
   6.2 引出 Lab 02。

#### 8. Engineering Lab / 示例

Lab 02 Tool Runtime：

- 目的：验证模型意图与宿主执行边界。
- 输入：Calculator、ReadOnly File Tool。
- 观察：参数、Policy Decision、Timeout、Result、Trace。
- 预期失败：越界路径、大结果、取消、重复调用。
- 结论：Tool Runtime 是独立工程系统。

#### 9. 与 DeepSeek Harness 的关系

35 对应 Tool Registry、Policy 与 Execution Pipeline。

#### 10. 与 BuildPilot 的关系

未来三只只读 Tool 共享同一 Pipeline，但此 Lab 不进入 BuildPilot。

#### 11. Evidence 要求

Tool Calling / MCP 规范、Lab 02 代码与 Trace。**BLOCKED：必须完成 Lab 02 后写行为章节。**

#### 12. 最容易混淆的概念

ToolDefinition ≠ 函数；Schema Valid ≠ Policy Allowed；Result ≠ Evidence；Sandbox ≠ Permission。

#### 13. 本篇明确不讲什么

不开放 Shell、写文件或生产凭证；不讲 Agent Loop。

#### 14. 学习检查

- 白名单路径经过 junction 指向外部，哪一步拒绝？
- Tool 执行成功但输出 Schema 错误，应怎样记录？
- UI 需要全文、模型只需摘要，为什么不能共用一个字符串？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 07｜MCP 与外部能力边界：协议解决什么，宿主仍需解决什么

#### 1. 本篇定位

协议映射篇。把 MCP 放在 Tool 发现与远程调用层，不把它写成 Agent 或安全系统。

#### 2. 为什么现在学它

06 已有本地 Tool Runtime。新问题是 Tool 来自独立服务或第三方系统时，怎样标准化发现、调用和结果。

#### 3. 学完以后应该能回答的问题

- MCP Tool 与本地 ToolDefinition 有何关系？
- MCP Server 是否决定 Agent 权限？
- Transport、Capability Discovery 和 Tool Runtime 怎样分层？
- 远程错误和取消怎样映射？
- 为什么连接 MCP 不等于完成 Agent？

#### 4. 前置知识

05-06。

#### 5. 核心概念

MCP Client、Server、Transport、Capability Discovery、Tool Schema、Remote Result、Trust Boundary。

#### 6. 核心心智模型

~~~text
Agent Host Tool Runtime
→ MCP Client / Transport
→ MCP Server Tool
→ External System
~~~

#### 7. 正文详细框架

1. 远程能力问题  
   1.1 回答：为什么需要标准发现和调用。  
   1.2 材料：本地 Tool 与 MCP Tool 架构图。
2. MCP 负责什么  
   2.1 Capability、Schema、Call、Result 与协议错误。  
   2.2 材料：Specification 摘要，不做 API 教程。
3. MCP 不负责什么  
   3.1 业务授权、Agent Policy、领域 Validation 和最终审计。  
   3.2 材料：危险 Tool 暴露负例。
4. 两层 Tool Runtime  
   4.1 本地宿主 Policy + 远端 Server Policy。  
   4.2 论点：两边都需 fail closed。
5. 工程边界  
   5.1 网络、取消、超时、身份和版本。  
   5.2 引出 08：有 Tool 之后，怎样连续决定下一步。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；可将 Lab 02 Calculator 用 Fake MCP Transport 替换，验证协议不改变本地 Policy。

#### 9. 与 DeepSeek Harness 的关系

31、35、37 会检查 Capability / Provider 与 Tool Registry 的连接点。

#### 10. 与 BuildPilot 的关系

未来 Jenkins / Metrics 可以通过 API 或 MCP 接入，但先按现有系统能力选择，不为使用协议而使用协议。

#### 11. Evidence 要求

MCP 官方 Specification、版本日期、最小消息 Trace。

#### 12. 最容易混淆的概念

MCP ≠ Agent；MCP Tool ≠ 已授权能力；Server Policy ≠ Host Policy；协议标准 ≠ 业务标准。

#### 13. 本篇明确不讲什么

不搭建生产 MCP Server，不讲所有 Transport 和资源类型。

#### 14. 学习检查

- MCP Server 暴露 deploy，客户端是否应直接展示给模型？
- 远端已做权限，宿主是否还需 Tool Allowlist？
- 把本地函数改成 MCP Tool，会自动获得 Trace 与 Eval 吗？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 08｜Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop

#### 1. 本篇定位

核心原理篇。第一次让应用从单次 Tool Use 生长为 Agent。

#### 2. 为什么现在学它

07 已经能安全调用外部能力。现在模型需要根据 Tool Result 再决定下一步，Decide → Act → Observe 开始循环。

#### 3. 学完以后应该能回答的问题

- Run、Turn 和 Step 怎样区分？
- Tool Result 何时成为 Observation？
- State 由谁更新？
- Continue / Stop 由哪些条件共同决定？
- 怎样避免无限循环和伪完成？

#### 4. 前置知识

03 Structured Output；05-07 Tool。

#### 5. 核心概念

Run、Turn、Step、Agent Loop、Action、Observation、State、Wakeup、Stop Condition、Max Step。

#### 6. 核心心智模型

~~~text
Goal → Decide → Act → Observe → Update
         ↑                 ↓
         └── Continue / Stop
~~~

#### 7. 正文详细框架

1. 一次 Tool Use 为什么不够  
   1.1 新结果会改变下一步；材料：两步日志调查时序。  
   1.2 论点：循环的关键是反馈，不是调用次数。
2. Run / Turn / Step  
   2.1 外部目标、唤醒周期和模型决策的层级。  
   2.2 材料：无 Tool、单 Tool、多 Tool 三条 Trace。
3. Observation 与 State  
   3.1 原始 Result、规范化 Observation、任务状态。  
   3.2 材料：状态快照图。
4. Stop Conditions  
   4.1 目标满足、模型完成、输出合同、预算、Policy、取消和错误。  
   4.2 论点：模型没有唯一停止权。
5. 最小 Loop  
   5.1 C# 伪代码；只包含 assemble、call、dispatch、append、stop。  
   5.2 引出 Lab 03、09 Planning 与 10 State / Workflow。

#### 8. Engineering Lab / 示例

Lab 03 Minimal Agent Loop：

- 目的：观察 Turn / Step / Observation / Stop。
- 输入：Mock Build Log、parseMockLog、readMockFile。
- 观察：Step Trace 和 State Snapshot。
- 预期失败：重复调用同一 Tool、没有证据却 Stop。
- 结论：Loop 需要外部停止和状态合同。

#### 9. 与 DeepSeek Harness 的关系

33 对应 Inbox、Turn、Step 与 Agent Loop。

#### 10. 与 BuildPilot 的关系

未来 Compile Case 使用短 Loop，Startup Case 使用多步调查；当前 Lab 独立。

#### 11. Evidence 要求

Agent SDK 生命周期资料、Lab 03 Trace。**BLOCKED：必须完成四条最小轨迹。**

#### 12. 最容易混淆的概念

Turn ≠ Step；Tool Result ≠ Observation；Stop ≠ Success；Chain ≠ Agent Loop。

#### 13. 本篇明确不讲什么

不深入 Context、Memory、Workflow 和多 Agent。

#### 14. 学习检查

- 模型说“完成”，但 Schema 未满足，谁阻止 Stop？
- 一次 Turn 可以产生零个 Step 吗？
- Tool 返回空结果后，系统还缺哪些信息才能决定下一步？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 09｜Planning：Agent 为什么需要计划，又为什么不能迷信计划

#### 1. 本篇定位

计划机制篇。解释 Agent 为什么需要形成候选步骤，以及 Plan 为什么既不是执行结果，也不能凌驾于确定性约束之上。

#### 2. 为什么现在学它

08 已建立逐 Step 决策的 Agent Loop。任务一旦超过一两步，系统需要显式表达目标分解和下一步意图；但如果把 Plan 当作真相，又会把猜测伪装成已完成状态。

#### 3. 学完以后应该能回答的问题

- Implicit Plan 与 Explicit Plan 有什么差别？
- ReAct、Plan-and-Execute、Planner / Executor 各强调什么控制方式？
- Plan Revision 与 Re-planning 在何时发生？
- 为什么 Plan 不等于 Execution、Workflow 或 Verified State？
- 哪些机制有权拒绝模型计划？

#### 4. 前置知识

08 Agent Loop；基本任务分解经验。

#### 5. 核心概念

Implicit Plan、Explicit Plan、Task Decomposition、ReAct、Plan-and-Execute、Planner / Executor、Plan Revision、Re-planning、Candidate Intent。

#### 6. 核心心智模型

~~~text
Goal + Current Evidence
→ Candidate Plan
→ Policy / Workflow / Evidence Gate
→ Execute one safe step
→ Observe
→ Keep / Revise / Replace Plan
~~~

#### 7. 正文详细框架

1. Planning 为什么出现  
   1.1 多步目标需要表达依赖、顺序与未知；材料：两步与十步任务对照。  
   1.2 论点：Planning 降低盲目局部决策，但不会自动提高事实正确性。
2. 隐式与显式 Plan  
   2.1 模型内部倾向、可见任务列表、结构化 Plan。  
   2.2 材料：同一调查任务的三种表示。
3. 三种常见思维模式  
   3.1 ReAct、Plan-and-Execute、Planner / Executor。  
   3.2 边界：轻量理解控制权，不扩展为 Planning 算法或论文大全。
4. Revision / Re-planning  
   4.1 新 Observation、工具失败、前提证伪和目标变化。  
   4.2 材料：计划版本与变更理由。
5. Plan 的权力边界  
   5.1 Tool Policy、Workflow Guard、Evidence Gate 与 Budget 都可以拒绝或缩小计划。  
   5.2 论点：Plan 是候选执行意图，不是授权，也不是 Verified State。
6. 引出确定性骨架  
   6.1 哪些顺序和不变量不应交给模型每次重想。  
   6.2 转到 10 State Machine / Workflow。

#### 8. Engineering Lab / 示例

不设独立 Lab；给 Lab 03 的同一目标生成 Initial Plan，并在 Tool 返回反证后记录一次 Revision，观察 Plan 与已完成 State 是否分离。

#### 9. 与 DeepSeek Harness 的关系

29、33 将验证 DSH 的 Loop 是否存在显式 Planning 一级对象，或由 Host、插件与模型行为共同形成。

#### 10. 与 BuildPilot 的关系

Startup Investigation 可维护候选调查计划，但每一步仍受只读 Tool、证据合同和预算约束。

#### 11. Evidence 要求

Agent Loop Trace、至少一条计划被反证后修订的 Fixture，以及所选 Agent 模式的权威资料。

#### 12. 最容易混淆的概念

Plan ≠ Execution；Plan Item ≠ Completed State；Re-planning ≠ Retry；Planner ≠ 必须是另一个 Agent；模型计划 ≠ 授权。

#### 13. 本篇明确不讲什么

不做 Planning 论文综述，不比较复杂搜索算法，不把 Chain-of-Thought 当作必须持久化的 Plan。

#### 14. 学习检查

- 模型列出“已验证配置”但 Tool 尚未执行，属于 Plan 还是 State？
- 新证据推翻前提时为什么不应只 Retry 原步骤？
- Plan 要删除文件时，哪个层有权拒绝？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。Planning 是 Loop 到 Workflow 的必要桥梁，但本课程不把它扩成独立算法专题。

#### 16. 概念成熟度

**Mechanism**。把 00 的 Agentic 规划直觉落为可观察 Plan、Revision 与约束关系。

### 10｜State Machine 与 Workflow：确定性骨架和 Agent Decision Point

#### 1. 本篇定位

核心边界篇。回答“流程可以写代码，为什么还需要 Agent”。

#### 2. 为什么现在学它

08 的自由 Loop 能执行，09 也能形成候选计划，但长任务仍会漏步骤、重复和违反不变量。需要把确定状态与转换写进程序，只保留需要上下文和证据判断的决策点。

#### 3. 学完以后应该能回答的问题

- Plan、Workflow Definition 与当前 State 为什么是三种对象？
- Agent Loop 与 State Machine 有什么差别？
- 哪些状态转移必须确定性验证？
- Workflow 何时调用 Agent，Agent 何时调用 Workflow Tool？
- Checkpoint 与普通 State 有何区别？

#### 4. 前置知识

08 Agent Loop；09 Planning；传统状态机知识。

#### 5. 核心概念

State、State Machine、Workflow、Stage、Transition、Guard、Invariant、Agent Decision Point、Terminal State。

#### 6. 核心心智模型

~~~text
确定状态 / 转换 → Program / Workflow
需要上下文判断的转移 → Agent Decision Point
~~~

#### 7. 正文详细框架

1. 自由 Loop 的失败  
   1.1 漏步骤、重复 Tool、不可恢复；材料：坏 Trace。  
   1.2 论点：自主性应放在真正不确定的位置。
2. State Machine  
   2.1 State、Transition、Guard、Invariant、Terminal。  
   2.2 材料：简单调查状态图。
3. Workflow + Agent  
   3.1 Workflow 调 Agent、Agent 调受控 Workflow Tool、Code Orchestration。  
   3.2 材料：三种控制权时序图。
4. Agent Decision Point  
   4.1 输入允许的 State、Evidence 和候选 Plan；输出受 Schema 和 Guard 约束的选择。  
   4.2 论点：只把真正依赖上下文判断的转移交给 Agent。
5. 从 State 到 Checkpoint  
   5.1 当前状态可表达流程位置，但跨中断恢复还需要持久化与副作用语义。  
   5.2 引出 11 的长任务、取消和 Resume。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；Lab 04 在 11 后同时验证 State Machine、Checkpoint 和 Resume。

#### 9. 与 DeepSeek Harness 的关系

33-34 会观察 Loop 与 Session State；37 判断 Workflow 是核心事实还是扩展映射。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot 固定 Intake / Evidence / Diagnosis / Review 阶段，把调查路径留给 Agent。

#### 11. Evidence 要求

状态机资料、三种编排模式示例、坏 Trace 与状态图。

#### 12. 最容易混淆的概念

Workflow ≠ Agent Loop；Plan ≠ Workflow State；Stage ≠ Step；模型建议 ≠ 合法转移。

#### 13. 本篇明确不讲什么

不做 BPM 教程，不引入 Multi-Agent，不处理分布式事务。

#### 14. 学习检查

- Unity Build 固定阶段应由模型逐步决定吗？
- 模型计划跳过 Evidence 阶段，谁拒绝？
- 状态固定但分支条件需读多源证据，哪部分适合 Agent？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。这是“确定性流程为何仍需要 Agent”的主轴边界，后续 Long-running、Harness 与 BuildPilot 都依赖它。

#### 16. 概念成熟度

**Engineering**。把 00 的 Workflow 位置感和 08-09 的动态决策落为可执行状态、Guard 与受控 Decision Point。

### 11｜Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery

#### 1. 本篇定位

长任务工程篇。把 Agent 从短 Loop 扩展到可中断、可恢复且不会盲目重试的运行。

#### 2. 为什么现在学它

10 已有 State Machine / Workflow，但真实 Tool 和外部系统会超时、取消或跨时段返回。没有 Checkpoint 与恢复语义，Workflow 只能从头重跑。

#### 3. 学完以后应该能回答的问题

- Timeout、Cancellation、Retry、Resume 和 Recovery 怎样区分？
- 哪些状态应进入 Checkpoint？
- 幂等性为什么决定 Retry？
- 外部副作用发生后还能恢复什么？
- Long-running Task 怎样诚实输出 partial result？

#### 4. 前置知识

06 Tool Runtime；08 Loop；09 Planning；10 State / Workflow。

#### 5. 核心概念

Long-running Task、Checkpoint、Retry Budget、Idempotency、Timeout、Cancellation、Resume、Compensation、Recovery Boundary。

#### 6. 核心心智模型

~~~text
Failure / Cancel
→ Classify
→ Retry / Checkpoint / Compensate / Ask / Stop
→ Resume within explicit boundary
~~~

#### 7. 正文详细框架

1. 长任务新增的失败面  
   1.1 模型、Tool、网络、异步系统、用户取消。  
   1.2 材料：跨分钟调查时序。
2. Checkpoint  
   2.1 保存 State、已完成 Action、Evidence 引用、Budget；不保存什么。  
   2.2 材料：Checkpoint Schema。
3. Retry  
   3.1 Transient / Permanent、幂等键、Retry Budget。  
   3.2 材料：决策表和重复副作用负例。
4. Cancellation  
   4.1 信号传播到模型、Tool 和 Workflow。  
   4.2 材料：CancellationToken 时序。
5. Recovery / Resume  
   5.1 从安全点继续、补偿或人工接棒。  
   5.2 论点：无法回滚的外部系统必须诚实报告。
6. Partial Result  
   6.1 已知、未知、未验证和下一安全动作。  
   6.2 引出 Lab 04。

#### 8. Engineering Lab / 示例

Lab 04 State Machine + Checkpoint：

- 目的：验证取消后恢复和幂等 Retry。
- 输入：Fake Long-running Investigation。
- 观察：State、Checkpoint、Retry Count、Resume Trace。
- 预期失败：重复副作用、检查点缺少未完成动作。
- 结论：Recovery 是显式合同，不是“再跑一次”。

#### 9. 与 DeepSeek Harness 的关系

34、36 对应 Session Event、Cancellation、Compaction 与 Recovery。

#### 10. 与 BuildPilot 的关系

影响 Startup 调查的 Resume；Compile Fixture 可简单重启，不提前复杂化。

#### 11. Evidence 要求

Lab 04、取消 Trace、幂等性测试。**BLOCKED：必须先完成 Lab 04。**

#### 12. 最容易混淆的概念

Retry ≠ Recovery；Timeout ≠ Cancellation；Resume ≠ Replay；Checkpoint ≠ Memory。

#### 13. 本篇明确不讲什么

不设计分布式事务，不承诺回滚所有外部副作用。

#### 14. 学习检查

- Tool 已创建远端资源后超时，能否直接 Retry？
- 用户取消时为什么仍需落最后安全状态？
- 哪些 Compile Case 状态无需 Checkpoint？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

## Part III｜Agent 的信息、状态与知识

### 12｜Context Engineering：每一个 Step 到底应该看到什么

#### 1. 本篇定位

信息主线篇。在读者已经理解 Step 之后，正式研究一次模型请求的完整装配。

#### 2. 为什么现在学它

08-11 让 Agent 连续运行，随之产生计划、历史、Tool Result、State 和 Checkpoint。新问题不是 Prompt 怎么写，而是下一 Step 应看到哪些信息。

#### 3. 学完以后应该能回答的问题

- Prompt 与 Context 的边界是什么？
- 一个 Step 的 Context 有哪些来源？
- Tool Schema、Tool Result 和 Workflow State 为什么都属于 Context？
- Context Selection、Priority 和 Budget 如何协作？
- 怎样记录模型这一 Step 实际看到了什么？

#### 4. 前置知识

02 Prompt；06 Tool Result；08 Step；09 Planning；10-11 State / Checkpoint。

#### 5. 核心概念

Context Assembly、Contributor、Selection、Ordering、Priority、Scope、Snapshot、Context Receipt、Stable / Dynamic Context。

#### 6. 核心心智模型

~~~text
Prompt + State + History + Tool Schema / Result + Environment
→ Select → Order → Fit Budget → Snapshot
→ Model Step
~~~

#### 7. 正文详细框架

1. Context 是构建产物  
   1.1 回答：为什么 Message List 不是全部；论点：每个 Step 都应重新装配。  
   1.2 材料：Context 来源图。
2. 六类来源  
   2.1 指令、当前目标、Working State、历史、能力、外部事实。  
   2.2 材料：同一调查 Step 的 Request Breakdown。
3. Select / Order / Scope  
   3.1 什么应常驻，什么按 Stage / Project / Agent 加载。  
   3.2 材料：Contributor Priority 表。
4. Context Budget  
   4.1 输入、输出、Tool Schema 和结果共享窗口。  
   4.2 论点：预算是质量约束，不只是成本。
5. Snapshot / Receipt  
   5.1 记录来源、版本、冲突、裁剪与未知。  
   5.2 材料：Context Receipt Schema。
6. 引出调试问题  
   6.1 当答案错时先问“这个 Step 看到了什么”。  
   6.2 转到 13 Context Debugging。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；为 Lab 05 先生成三个 Context Snapshot。

#### 9. 与 DeepSeek Harness 的关系

32 对应 System Prompt / PromptContext，33-34 对应 Step History 与 Session。

#### 10. 与 BuildPilot 的关系

影响 Project、Runtime、Investigation 与 Historical Context 的分层。

#### 11. Evidence 要求

模型请求 Trace、Context Window 官方资料、至少一份 Context Receipt 样例。

#### 12. 最容易混淆的概念

Context ≠ Prompt；Context ≠ Session；Tool Result ≠ 永久历史；Snapshot ≠ Memory。

#### 13. 本篇明确不讲什么

不讲向量检索、长期 Memory 和具体 Compaction 算法。

#### 14. 学习检查

- 当前 Workflow State 未进入请求，属于 Prompt 问题还是 Context 问题？
- Tool Schema 常驻 80 个，会影响哪些预算？
- 只保存最终 Prompt 文本，能否追踪每段信息来源？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Foundation**。正式建立本篇核心概念的定义、作用域与边界，后续不再从零解释。

### 13｜Context Debugging：Packing、Compression、Pollution 与可重建性

#### 1. 本篇定位

信息调试核心篇。建立“模型这一步看到了什么”的系统化故障分析方法。

#### 2. 为什么现在学它

12 已能装配 Context，但正确来源也可能因顺序、截断、过期、冲突和历史污染而失效。Agent 错误不能默认归因于模型。

#### 3. 学完以后应该能回答的问题

- Context Pollution 怎样产生？
- 重要信息被淹没与根本没注入怎样区分？
- Compression 可以丢什么，不能丢什么？
- Tool Result 截断怎样保持可追溯？
- 怎样从 Trace 重建某一步的 Context？

#### 4. 前置知识

12 Context Assembly；11 Checkpoint。

#### 5. 核心概念

Packing、Prioritization、Pollution、Stale Context、Conflict、Truncation、Compression、Compaction、Spill、Rehydration。

#### 6. 核心心智模型

~~~text
Expected Context
vs Actual Snapshot
→ Missing / Stale / Conflicting / Buried / Truncated
→ Repair source or assembly
~~~

#### 7. 正文详细框架

1. Context Failure Taxonomy  
   1.1 Missing、Wrong Version、Conflict、Buried、Oversized、Truncated。  
   1.2 材料：错误分类矩阵。
2. Packing 与 Priority  
   2.1 不是简单拼接；信息价值随 Stage 变化。  
   2.2 材料：同一资料不同排序对照。
3. Compression / Compaction  
   3.1 历史摘要、Evidence 引用、大结果 Spill。  
   3.2 论点：不可删除未验证标记、来源和待办状态。
4. Pollution  
   4.1 过期示例、错误 RAG、无关 Skill、重复规则、模型自我总结。  
   4.2 材料：污染链路图。
5. Debugging Procedure  
   5.1 导出 Snapshot → 对照 Expected → 检查来源 / 顺序 / 变换。  
   5.2 材料：Context Debug Checklist。
6. A/B 与可重建性  
   6.1 全量、简单截断、阶段化 Context 三组。  
   6.2 引出 Lab 05。

#### 8. Engineering Lab / 示例

Lab 05 Context Debugging：

- 目的：定位 Context 层错误而不是调 Prompt。
- 输入：正确、过期、冲突和大 Tool Result 资料包。
- 观察：Snapshot、排序、裁剪、模型引用。
- 预期失败：正确信息存在但被旧记录淹没。
- 结论：Context Debugging 需要来源和变换记录。

#### 9. 与 DeepSeek Harness 的关系

32、36 对应 Prompt Assembly、History、Compaction 与 Trace。

#### 10. 与 BuildPilot 的关系

决定每次调查都要有 Context Receipt，并保留原始 Evidence 引用。

#### 11. Evidence 要求

Lab 05、三组 A/B Trace。**BLOCKED：不能凭概念写“阶段化一定更好”。**

#### 12. 最容易混淆的概念

Compression ≠ Memory；摘要 ≠ Evidence；Context 大 ≠ Context 全；排序 ≠ 权威性。

#### 13. 本篇明确不讲什么

不讲服务端 KV Cache，不做通用 Prompt 优化课。

#### 14. 学习检查

- 正确版本在 Context 中但排在大量旧记录后，应修 RAG 还是 Packing？
- Compression 删除 unverifiedScope 会造成什么？
- 模型没引用某条信息，如何判断是没看到还是没采纳？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 14｜Working Memory 与 Investigation State：当前任务正在想什么

#### 1. 本篇定位

状态映射篇。补上上一版缺失的 Working Memory，并把它与 Session、Context 分开。

#### 2. 为什么现在学它

12-13 让我们看到 Context 每 Step 重建且可能被污染，但 Agent 仍需保存当前目标、已知证据、假设、待办和失败尝试。它们不是长期 Memory，而是当前任务工作状态。

#### 3. 学完以后应该能回答的问题

- Working Memory 与 Context 有什么差别？
- Investigation State 应包含哪些字段？
- 模型隐含推理能否作为 Working Memory？
- Working Memory 何时更新、何时清理？
- Checkpoint 与 Working Memory 怎样连接？

#### 4. 前置知识

08 Loop；11 Checkpoint；12-13 Context。

#### 5. 核心概念

Working Memory、Investigation State、Task Goal、Known Facts、Hypothesis Set、Open Questions、Attempt History、Next Action Candidate。

#### 6. 核心心智模型

~~~text
Working Memory = 当前任务的显式工作状态
→ 每 Step 选择其中一部分进入 Context
→ 由新 Observation 更新
~~~

#### 7. 正文详细框架

1. 为什么不能依赖模型“自己记得”  
   1.1 隐含推理不可审计、跨 Step 不稳定。  
   1.2 材料：丢失目标的坏 Trace。
2. Working Memory 最小结构  
   2.1 Goal、Facts、Hypotheses、Open Questions、Attempts、Next Candidates。  
   2.2 材料：InvestigationState DTO。
3. 更新规则  
   3.1 Observation 怎样加入、假设怎样降级或淘汰。  
   3.2 论点：模型建议更新，Runtime 验证不变量。
4. 与 Context / Checkpoint  
   4.1 Working Memory 是源状态，Context 是本次视图。  
   4.2 Checkpoint 持久化安全子集。
5. 生命周期  
   5.1 Run 内创建、阶段结束压缩、任务结束归档或丢弃。  
   5.2 引出 15 Session 与长期 Memory。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；扩展 Lab 04，观察 Working Memory 在 Resume 前后是否一致。

#### 9. 与 DeepSeek Harness 的关系

33-34 检查 DSH 的 Step / Session 能否映射 Working State；不预设同名一级对象。

#### 10. 与 BuildPilot 的关系

未来 Startup 调查的 Hypothesis Tree 和 Open Questions 属于 Investigation State。

#### 11. Evidence 要求

状态 Schema、坏 Trace、Checkpoint 对照。不需要 DSH 源码。

#### 12. 最容易混淆的概念

Working Memory ≠ Context；Working Memory ≠ Long-term Memory；隐含推理 ≠ 可审计状态；Checkpoint ≠ 全部 Session。

#### 13. 本篇明确不讲什么

不保存 Chain-of-Thought，不讲跨任务学习和知识库。

#### 14. 学习检查

- 当前假设列表应直接拼进每个 Step 的完整 Context 吗？
- 模型说“我已经排除了 CDN”，但无 Evidence，Working State 应怎样更新？
- 任务结束后哪些 Working Memory 值得长期保留？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 15｜Session、Long-term Memory 与 Project Memory：事实、经验和作用域

#### 1. 本篇定位

信息边界篇。区分一次运行的持久事实、跨任务经验和项目级长期上下文。

#### 2. 为什么现在学它

14 只有当前任务工作状态。任务需要中断恢复、跨会话复用经验，并且不同项目不能共享全部信息，因此需要分生命周期和作用域。

#### 3. 学完以后应该能回答的问题

- Session 保存什么，为什么不等于 Transcript？
- Long-term Memory 是事实、经验还是偏好？
- Project Memory 怎样避免跨项目污染？
- Memory 写入为什么需要筛选和退场？
- 当前源码事实与 Memory 冲突时相信谁？

#### 4. 前置知识

11 Checkpoint；14 Working Memory。

#### 5. 核心概念

Session Event、Transcript、Durable State、Long-term Memory、Project Memory、Scope、Write Gate、Freshness、Retirement。

#### 6. 核心心智模型

~~~text
Session = 一次运行的持久事实流
Long-term Memory = 跨任务复用的候选经验
Project Memory = 受项目作用域约束的长期上下文
~~~

#### 7. 正文详细框架

1. Session 不只是聊天记录  
   1.1 Tool、Policy、State、取消和结果都需事件。  
   1.2 材料：Session Event 与 Transcript 对照。
2. Long-term Memory  
   2.1 偏好、经验、失败模式；不是自动真相。  
   2.2 材料：候选写入与审核流程。
3. Project Memory  
   3.1 项目版本、架构决策、领域约定与作用域。  
   3.2 材料：User / Org / Project / Session Scope 图。
4. 写入与读取 Gate  
   4.1 来源、Owner、置信、有效期和冲突。  
   4.2 论点：任务总结只能成为 Candidate。
5. 权威性  
   5.1 Source / CI / Build / Runtime 保存当前事实；Memory 路由经验。  
   5.2 引出 16 Knowledge Base / RAG。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；使用三条候选 Memory 做 Scope / Freshness 分类练习。

#### 9. 与 DeepSeek Harness 的关系

34 对应 append-only Session Event；37 讨论 Memory / Skill / RAG 的扩展映射。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot Session Store、Project Context 和经验候选分离。

#### 11. Evidence 要求

Session / Memory 框架官方资料、作用域案例、当前事实权威映射。

#### 12. 最容易混淆的概念

Session ≠ Transcript；Memory ≠ Knowledge Base；Project Memory ≠ 当前源码；模型总结 ≠ 事实。

#### 13. 本篇明确不讲什么

不完整讲团队知识飞轮，不自动跨项目共享经验。

#### 14. 学习检查

- 上次任务的错误总结可以直接成为 Project Memory 吗？
- Session 只保存 Message，恢复 Tool 执行会缺什么？
- 当前 Job 配置与历史 Memory 冲突，应怎样报告？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Foundation**。正式建立本篇核心概念的定义、作用域与边界，后续不再从零解释。

### 16｜Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite

#### 1. 本篇定位

知识获取篇。讲清 Knowledge Base 是来源，RAG 是当前任务的检索与注入链。

#### 2. 为什么现在学它

15 能保存会话和经验，但 Agent 还需要从大量外部文档、事故和规则中按需取得相关信息。

#### 3. 学完以后应该能回答的问题

- Knowledge Base 与 RAG 有什么区别？
- Keyword、Vector、Hybrid 在什么环节不同？
- Filter、Rerank 和 Freshness 为什么不能省？
- 检索结果何时能成为 Evidence？
- Citation 与 Permission 怎样进入链路？

#### 4. 前置知识

12 Context；15 Memory / Authority。

#### 5. 核心概念

Knowledge Base、Retriever、Query、Chunk、Filter、Rerank、Top-K、Inject、Citation、Freshness、Permission、Retrieval Eval。

#### 6. 核心心智模型

~~~text
Query → Retrieve → Filter → Rerank
→ Inject with Citation
→ Use / Reject / Verify
~~~

#### 7. 正文详细框架

1. KB 与 RAG 分层  
   1.1 来源管理与运行检索不是同一件事。  
   1.2 材料：Storage / Retrieval / Context 三层图。
2. Query / Retrieve  
   2.1 Keyword、Vector、Hybrid 的稳定抽象。  
   2.2 材料：同一故障三种召回结果，不做数据库教程。
3. Filter / Rerank  
   3.1 项目、版本、权限、新鲜度和业务相关性。  
   3.2 论点：语义相似不等于可用。
4. Inject / Cite  
   4.1 摘要、原文引用、来源 ID、版本和 Context Budget。  
   4.2 材料：RetrievedEvidence 结构。
5. Retrieval Eval  
   5.1 Recall、Precision、Citation Correctness、Answer Utility。  
   5.2 材料：小型黄金 Query Set。
6. 与知识飞轮  
   6.1 本篇只讲消费；生产、准入、记账、保鲜和退场归 AI 赋能知识飞轮。  
   6.2 引向 17 Skill：方法知识与事实知识如何分开。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；可用 10 篇 Markdown 事故记录比较 Keyword 与 Hybrid。

#### 9. 与 DeepSeek Harness 的关系

37 判断 Retrieval Tool 与 Context Provider 两种接法，并严格区分扩展设计与 DSH 内置事实。

#### 10. 与 BuildPilot 的关系

未来检索 Unity / Jenkins 历史事故和构建合同，必须带项目、版本和引用。

#### 11. Evidence 要求

RAG 原始论文 / 权威资料、检索 Fixture、黄金 Query Set。具体效果需实验。

#### 12. 最容易混淆的概念

RAG ≠ KB；召回 ≠ 证据；相似 ≠ 当前适用；Citation ≠ Verification。

#### 13. 本篇明确不讲什么

不做向量数据库大全，不设计企业知识治理平台。

#### 14. 学习检查

- 历史事故语义相似但 Unity 版本不同，应在哪层处理？
- 检索到文档后直接整篇注入，会出现什么问题？
- RAG 返回结果是否自动成为 Evidence？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 17｜Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt

#### 1. 本篇定位

知识与方法边界篇。把 Skill 定义为生态相关的可发现方法包，不冒充全行业统一对象。

#### 2. 为什么现在学它

16 解决“找什么事实”，但 Agent 还需要知道“这类任务通常怎样做”。长期把所有方法放进 Prompt 会造成 Context Bloat。

#### 3. 学完以后应该能回答的问题

- Skill 与 Prompt、Tool、Workflow、KB 有什么差别？
- Discovery Metadata 与完整 Instructions 为什么分层？
- Skill 何时触发、怎样避免误触发？
- Skill 怎样版本化、测试、过期和瘦身？
- 自动沉淀为什么只能生成候选 Skill？

#### 4. 前置知识

12-13 Context；16 Knowledge。

#### 5. 核心概念

Skill、Discovery Metadata、Trigger、Progressive Disclosure、Instructions、References、Scripts、Assets、Version、Drift、Bloat。

#### 6. 核心心智模型

~~~text
Lightweight Discovery
→ Trigger
→ Load Instructions
→ Read References / Use Scripts as needed
→ Eval / Maintain / Retire
~~~

#### 7. 正文详细框架

1. Skill 解决什么  
   1.1 方法复用、按需加载、团队维护。  
   1.2 材料：Prompt / KB / Skill / Workflow 对照表。
2. 最小结构  
   2.1 Metadata、Instructions、References、Scripts、Assets。  
   2.2 论点：不同产品实现不同，不宣称统一 Runtime。
3. Trigger / Progressive Loading  
   3.1 显式、规则和语义触发。  
   3.2 材料：未加载、正确加载、误加载三组 Context。
4. 边界  
   4.1 执行能力变 Tool；固定状态转移变 Workflow；事实进入 KB。  
   4.2 材料：十条内容分类练习。
5. 生命周期  
   5.1 Owner、版本、适用范围、测试、Drift、Retire。  
   5.2 自动总结只进候选区。
6. Skill Eval  
   6.1 规则遵守、步骤选择、Token、误触发率。  
   6.2 转入可靠性 Part IV。

#### 8. Engineering Lab / 示例

本篇不设置独立 Lab；设计一个 Generic Log Investigation Skill，并做三组触发 Fixture，不进入 BuildPilot。

#### 9. 与 DeepSeek Harness 的关系

37 检查 Skill 是否为 DSH 一级能力，以及如何通过 Plugin / Context / Tool 映射。

#### 10. 与 BuildPilot 的关系

未来 Compile / Startup Skill 分离方法与 Tool；此处只确定设计原则。

#### 11. Evidence 要求

Agent Skills Specification、至少两个产品 Skill 实现、三组触发 Fixture。

#### 12. 最容易混淆的概念

Skill ≠ Tool；Skill ≠ KB；Skill ≠ Workflow；自动生成 ≠ 自动可信。

#### 13. 本篇明确不讲什么

不做 RL 自进化，不把任何单一 Skill 格式写成行业标准。

#### 14. 学习检查

- “读取文件”应该是 Skill 还是 Tool？
- Skill 含十个状态和恢复逻辑，是否已变成 Workflow？
- Unity 2022.3 Skill 怎样避免污染 Unity 6 任务？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

## Part IV｜Reliable Agent Engineering

### 18｜Evidence Contract：把自然语言推断变成可审计工程数据

#### 1. 本篇定位

可靠性领域合同篇。保留并强化 Evidence → Hypothesis → Diagnosis → Action → Verification。

#### 2. 为什么现在学它

Agent 已能多步调查并获取知识，但自然语言回答会混淆事实、推测和已验证状态。真实工程需要可追溯数据合同。

#### 3. 学完以后应该能回答的问题

- Tool Result 何时成为 Evidence？
- Hypothesis 怎样引用支持与反证？
- Confidence 表达什么、不表达什么？
- ProposedAction 与 VerificationResult 为什么分开？
- Unverified Scope 怎样进入最终结果？

#### 4. 前置知识

03 Structured Output；08 Loop；14 Working State；16 Citation。

#### 5. 核心概念

Evidence ID、Source、Observation、Hypothesis、Contradicting Evidence、DiagnosisResult、Confidence、ProposedAction、VerificationResult、Unverified Scope。

#### 6. 核心心智模型

~~~text
Source → Evidence
Evidence ↔ Hypothesis / Contradiction
→ DiagnosisResult
→ ProposedAction
→ VerificationResult
~~~

#### 7. 正文详细框架

1. 自然语言结果的审计缺口  
   1.1 事实与猜测混写；材料：一段错误诊断拆解。  
   1.2 论点：正确答案也可能来自错误路径。
2. Evidence  
   2.1 来源、时间、作用域、原始引用与不可变 ID。  
   2.2 材料：Evidence Schema 与 Tool Result → Evidence Gate。
3. Hypothesis  
   3.1 支持、反证、状态、置信和下一验证。  
   3.2 材料：Hypothesis Tree。
4. Diagnosis / Action  
   4.1 当前最佳解释不等于修复完成。  
   4.2 Proposed、Approved、Executed 分状态。
5. Verification  
   5.1 Offline、静态、Unity、Jenkins、设备分别标渠道。  
   5.2 材料：VerificationResult 状态机。
6. Schema 负例  
   6.1 无证据结论、伪造已验证、忽略反证。  
   6.2 为 21-22 Trace / Eval 提供目标。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；使用 Generic Config Failure Fixture，不使用 BuildPilot 参考答案。

#### 9. 与 DeepSeek Harness 的关系

34、36 研究 Session / Trace 承载领域事件的方式；不假设 DSH 内置本合同。

#### 10. 与 BuildPilot 的关系

这是 BuildPilot Design v1 的核心领域合同。

#### 11. Evidence 要求

Schema、负例、至少一条完整 Trace。**BLOCKED：必须先完成 Evidence Contract evidence card。**

#### 12. 最容易混淆的概念

Tool Result ≠ Evidence；Evidence ≠ Root Cause；Diagnosis ≠ Verification；Confidence ≠ 准确率。

#### 13. 本篇明确不讲什么

不保存 Chain-of-Thought，不自动执行 Patch，不用参考答案冒充独立推理。

#### 14. 学习检查

- 模型未读配置就说“很可能缺引用”，属于什么？
- Offline Fixture 通过能否标 Unity Verified？
- Contradicting Evidence 应删除还是进入合同？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 19｜Permission、Approval、Human-in-the-loop 与 Sandbox

#### 1. 本篇定位

安全控制篇。把真实环境中的授权边界移出 Prompt。

#### 2. 为什么现在学它

18 让 Agent 可以提出 Action。下一问题是哪些 Action 有权执行、何时需要人批准、即使出错能影响多大范围。

#### 3. 学完以后应该能回答的问题

- Permission、Approval、Authentication 和 Sandbox 分别控制什么？
- Human-in-the-loop 应放在哪些决策点？
- Tool / Resource Policy 怎样组合？
- 凭证为什么不能直接进模型 Context？
- Sandbox 为什么不能替代最小权限？

#### 4. 前置知识

06 Tool Runtime；10 Workflow Gate；18 ProposedAction。

#### 5. 核心概念

Least Privilege、Allow / Deny / Ask、Capability Boundary、Approval Record、Human Gate、Sandbox、Credential Boundary、Path Allowlist。

#### 6. 核心心智模型

~~~text
Action Intent
→ Permission
→ Approval when required
→ Sandbox / Capability Boundary
→ Execute or Deny
~~~

#### 7. 正文详细框架

1. Prompt 不是安全边界  
   1.1 模型误判、注入和规则冲突。  
   1.2 材料：“不要删除”Prompt 的失败分析。
2. Permission  
   2.1 用户、Agent、Tool、资源、作用域和时限。  
   2.2 材料：Capability Matrix。
3. Approval / HITL  
   3.1 风险摘要、一次性批准、范围和过期。  
   3.2 材料：Ask → Approve / Reject 时序。
4. Sandbox  
   4.1 文件、进程、网络和凭证隔离。  
   4.2 论点：Sandbox 降低影响，不授予权限。
5. Policy Composition  
   5.1 Deny 优先，Ask 不可被静默升级。  
   5.2 材料：冲突 Policy 负例。
6. 只读并非零风险  
   6.1 敏感数据、成本和资源消耗。  
   6.2 引出 20 Budget。

#### 8. Engineering Lab / 示例

不设新 Lab；复用 Lab 02 加入 Ask / Reject 和敏感文件规则。

#### 9. 与 DeepSeek Harness 的关系

35-36 检查 Tool Policy、Sandbox、Approval 与取消的真实接点。

#### 10. 与 BuildPilot 的关系

第一阶段坚持路径 / Tool 白名单、无 Shell、无写入、无自动触发 Unity / Jenkins。

#### 11. Evidence 要求

Sandbox / Policy 官方资料、越权 Fixture、Approval Trace。

#### 12. 最容易混淆的概念

Permission ≠ Approval；Approval ≠ Authentication；Sandbox ≠ Policy；只读 ≠ 无风险。

#### 13. 本篇明确不讲什么

不设计生产 IAM，不提供“绝对安全”承诺。

#### 14. 学习检查

- 批准读取目录是否批准执行其中脚本？
- Sandbox 内允许删除是否还需 Approval？
- Tool 只读但能读取密钥，风险在哪里？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 20｜Budget Engineering：Token、Step、Cost 与 Latency

#### 1. 本篇定位

资源控制篇。建立 Agent Run 的多维预算，不只计算第一轮 Prompt。

#### 2. 为什么现在学它

Agent 已能长时间运行、检索和调用 Tool。即使权限安全，也可能因 Step、Context、Retry 和外部调用失控。

#### 3. 学完以后应该能回答的问题

- Agent 总成本从哪里产生？
- Token、Step、Wall-clock、Tool 和现金预算怎样协作？
- Context Budget 与质量是什么关系？
- Prefix Cache、Spill、Routing 能优化什么？
- Budget Exhausted 应怎样返回 partial result？

#### 4. 前置知识

11 Long-running；12-13 Context；16 RAG；19 Policy。

#### 5. 核心概念

Usage Ledger、Input / Output / Reasoning Token、Step Budget、Context Budget、Tool Budget、Latency Budget、Model Routing、Budget Guard。

#### 6. 核心心智模型

~~~text
Run Cost =
Σ Model + Tool + Retrieval + Retry + Subtask
under Token / Step / Time / Money Policies
~~~

#### 7. 正文详细框架

1. 聊天成本模型为何不够  
   1.1 多 Step 重复 Prompt、Schema、History。  
   1.2 材料：Run Cost Breakdown。
2. Usage Ledger  
   2.1 Run / Turn / Step / Model / Tool 五层。  
   2.2 材料：Usage Event Schema。
3. Budget Types  
   3.1 Token、Step、Time、Tool、Money、Risk Action。  
   3.2 论点：Budget 是运行 Policy。
4. Optimization Levers  
   4.1 Context Selection、Stable Prefix、Cache、Spill、Routing。  
   4.2 材料：优化杠杆与风险表。
5. 联合评价  
   5.1 正确率、引用完整、成本、延迟和返工。  
   5.2 论点：删 Evidence 换成本不是成功。
6. Exhaustion  
   6.1 partial result、已知 / 未知和下次建议。  
   6.2 转向 21 Trace。

#### 8. Engineering Lab / 示例

本篇不设独立 Lab；给 Lab 03 增加 Step / Token 模拟预算。

#### 9. 与 DeepSeek Harness 的关系

36 对应 Cost、Compaction 和 Budget Control。

#### 10. 与 BuildPilot 的关系

未来 Compile / Startup 使用不同 Budget Profile。

#### 11. Evidence 要求

模型 Usage / 价格官方资料并标日期、固定 Trace、成本表。具体结论需实验。

#### 12. 最容易混淆的概念

Token 少 ≠ 便宜；Cache Hit ≠ 正确；Budget ≠ 报表；Reasoning Token ≠ 可审计推理。

#### 13. 本篇明确不讲什么

不讲 GPU 推理优化，不把当前价格写成长期常量。

#### 14. 学习检查

- Tool Schema 从 3 个增到 80 个，哪些成本上升？
- 成本下降但引用丢失，是否优化成功？
- Budget 耗尽后为什么不应丢弃已有 Evidence？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 21｜Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层

#### 1. 本篇定位

可观察性核心篇。把“发生了什么”和“错在哪层”变成工程数据。

#### 2. 为什么现在学它

Agent 已有多个非确定组件：Prompt、Context、Retrieval、Planning、Tool、Policy、Workflow、Model。只有最终回答无法定位失败。

#### 3. 学完以后应该能回答的问题

- Trace 至少记录哪些事件和关联 ID？
- Transcript 与 Trace 有什么差别？
- Replay 是重建状态还是重新执行？
- Failure Taxonomy 怎样防止所有错误归因模型？
- Context Snapshot 与 Tool Trace 怎样关联？

#### 4. 前置知识

08-20，尤其是 Context、Tool、Policy 和 Evidence。

#### 5. 核心概念

Trace ID、Event / Span、Request Snapshot、Decision Record、Replay、Projection、Failure Taxonomy、Root Layer。

#### 6. 核心心智模型

~~~text
Trace = What happened
Replay = Rebuild / Re-execute
Failure Taxonomy = Where it failed
~~~

#### 7. 正文详细框架

1. 最终答案为何不够  
   1.1 正确答案可能来自猜测；错误答案可能因 Context。  
   1.2 材料：同结果不同路径 Trace。
2. Trace Model  
   2.1 Run、Turn、Step、Context、Model、Tool、Policy、State、Usage。  
   2.2 材料：关联图。
3. Replay 两层  
   3.1 事件重建 Projection。  
   3.2 冻结输入重新执行非确定系统。
4. Failure Taxonomy  
   4.1 Prompt、Context、Retrieval、Planning、Tool Selection / Args / Execution、Permission、Workflow、Loop、Model、Citation、Eval。  
   4.2 材料：分类决策树。
5. Debugging Procedure  
   5.1 定位最早偏离点，而不是只看最终错误。  
   5.2 材料：一次 Context Pollution 归因案例。
6. 向 Eval 过渡  
   6.1 Trace 说明发生什么，但不判断好坏。  
   6.2 引到 22。

#### 8. Engineering Lab / 示例

Lab 06 前半：为 Simple Investigation 生成完整 Trace，并对三种失败分类。

#### 9. 与 DeepSeek Harness 的关系

34、36 对应 Session Event、Trace、Cancellation 与 Recovery。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot Demo 必须可解释失败层，而非只展示答案。

#### 11. Evidence 要求

Trace Schema、至少四条失败轨迹。**BLOCKED：无 Trace 不写归因结论。**

#### 12. 最容易混淆的概念

Trace ≠ Transcript；Replay ≠ 相同输出；Failure Layer ≠ 最终症状；Session ≠ Trace。

#### 13. 本篇明确不讲什么

不做通用 Observability 平台，不记录隐藏 Chain-of-Thought。

#### 14. 学习检查

- 最终答案错误，但检索就返回了旧文档，首个失败层是什么？
- 事件可重建状态但重新推理不同，Replay 是否失败？
- Eval 参考答案错误，应归到哪一类？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 22｜Eval、Golden Dataset 与 Regression：修复以后还会不会再坏

#### 1. 本篇定位

质量闭环篇。把 Trace、Replay、Failure Taxonomy 接成可重复评测与回归。

#### 2. 为什么现在学它

21 能解释失败，但还不能判断系统整体是否更好。需要 Golden Dataset、Evaluator、回归和真实证据渠道。

#### 3. 学完以后应该能回答的问题

- Eval 应评最终答案还是调查路径？
- Golden Dataset 怎样避免答案泄露？
- Offline Eval 与真实 Runtime 验收怎样分层？
- 修复 Context 问题后怎样建立 Regression？
- Eval 本身怎样被验证？

#### 4. 前置知识

18 Evidence；20 Budget；21 Trace / Taxonomy。

#### 5. 核心概念

Eval Case、Golden Dataset、Fixture、Evaluator、Rubric、Path Quality、Regression Suite、Shadow Run、Acceptance Channel。

#### 6. 核心心智模型

~~~text
Trace → Eval → Failure Layer
→ Fix → Regression Case
→ Re-run across versions / models
~~~

#### 7. 正文详细框架

1. Eval 对象  
   1.1 Schema、Policy、Evidence、路径、结论、成本和人工返工。  
   1.2 材料：多维评分表。
2. Golden Dataset  
   2.1 输入、参考事实、允许变体、隐藏答案。  
   2.2 材料：Case 包结构。
3. Evaluator  
   3.1 确定性检查、规则、模型 Judge、人工 Review。  
   3.2 论点：Judge 也需要校准。
4. 证据层级  
   4.1 Unit / Fixture / Offline / Shadow / Unity / Jenkins / Device / Production。  
   4.2 材料：Acceptance Matrix。
5. Regression  
   5.1 每次真实失败沉淀最小复现。  
   5.2 Prompt / Context / Tool / Workflow 修复分别验证。
6. 公平比较  
   6.1 同模型通用 vs 专用；跨模型需单独报告。  
   6.2 接入 Lab 06。

#### 8. Engineering Lab / 示例

Lab 06 Trace + Eval：

- 目的：把一次失败变成 Regression Case。
- 输入：Simple Investigation Golden Set。
- 观察：Trace、Failure Layer、Score、成本。
- 预期失败：答案正确但引用伪造；Evaluator 漏判。
- 结论：Eval 是多层合同，不是单一分数。

#### 9. 与 DeepSeek Harness 的关系

36 研究 Trace / Recovery 接点；DSH 本身不自动提供 BuildPilot 领域 Eval。

#### 10. 与 BuildPilot 的关系

未来 Compile Golden Fixture 与 Startup Investigation Dataset 都由本篇方法设计。

#### 11. Evidence 要求

Lab 06、Golden Set、Evaluator 校准。**BLOCKED：必须先完成至少一条失败→修复→回归闭环。**

#### 12. 最容易混淆的概念

Eval Pass ≠ Production Ready；Golden Answer ≠ Agent Evidence；Judge Score ≠ 真值；Fixture ≠ Unity 验收。

#### 13. 本篇明确不讲什么

不做通用 LLM Benchmark，不用单一准确率概括系统。

#### 14. 学习检查

- 根因正确但越权读取，Case 是否通过？
- Reference Answer 被放进 Context，会怎样污染 Eval？
- 修复 Prompt 后为什么还要跑 Tool 与 Policy Regression？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 23｜Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent

#### 1. 本篇定位

后置架构篇。只有在单 Agent 可观测、可评测后才讨论拆分。

> 课程属性：**Advanced / Optional**。完成 22 后可直接进入 24，不影响 Harness 主线。

#### 2. 为什么现在学它

22 已能测出单 Agent 的 Context、权限、成本和失败瓶颈。现在才有证据判断职责拆分是否带来净收益。

#### 3. 学完以后应该能回答的问题

- Subagent 与普通 Tool 有什么差别？
- Agent as Tool 和 Handoff 谁保留控制权？
- Multi-Agent 解决职责、Context 还是权限隔离？
- 输出合同、失败传播和冲突怎样处理？
- 怎样证明拆分收益大于协调成本？

#### 4. 前置知识

06 Tool Runtime；10 Workflow；12 Context；19 Policy；21-22 Trace / Eval。

#### 5. 核心概念

Single Agent、Subagent、Agent as Tool、Handoff、Manager、Delegation、Context Isolation、Result Contract、Conflict Resolution、Coordination Cost。

#### 6. 核心心智模型

~~~text
先单 Agent
→ 发现可测边界
→ 选择 Agent as Tool / Handoff / Code Orchestration
→ Eval 拆分净收益
~~~

#### 7. 正文详细框架

1. 为什么 Multi-Agent 后置  
   1.1 拆分会复制 Context、Tool、Policy 和 Trace 问题。  
   1.2 材料：单 Agent 与双 Agent 成本图。
2. 四种模式  
   2.1 Single + Tools、Manager / Agent as Tool、Handoff、Code Orchestration。  
   2.2 材料：控制权时序。
3. 隔离维度  
   3.1 职责、Context、Tool、Permission、Budget 和 Session。  
   3.2 论点：角色 Prompt 不自动构成独立 Agent。
4. 失败传播与聚合  
   4.1 子结果 Schema、partial failure、冲突和重试。  
   4.2 材料：两专家结论冲突案例。
5. Eval  
   5.1 准确率、Context 节省、权限隔离 vs Token、延迟、交接遗漏。  
   5.2 结论：默认单 Agent。
6. 引向 Harness  
   6.1 多 Agent 放大统一能力、Policy、Session 和 Trace 的需求。  
   6.2 Optional 读者回到主线并转入 24。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；将确定性日志解析分别实现为 Tool 和 Subagent，做纸面 / Trace 成本比较。

#### 9. 与 DeepSeek Harness 的关系

31、37 检查 Subagent Provider、Profile 与 Host 编排边界。

#### 10. 与 BuildPilot 的关系

Design v1 默认 Single Agent；没有 Eval 证据不建立 Agent Swarm。

#### 11. Evidence 要求

主流 SDK 官方编排文档、单 / 多 Agent 对照 Trace。

#### 12. 最容易混淆的概念

Subagent ≠ Tool；Handoff ≠ Agent as Tool；并发 ≠ Multi-Agent；角色 ≠ 独立 Runtime。

#### 13. 本篇明确不讲什么

不讲大规模 Swarm，不以 Agent 数量证明架构能力。

#### 14. 学习检查

- 日志解析为何通常更适合 Tool 而非 Subagent？
- Handoff 后原 Agent 还拥有下一步控制权吗？
- 两个 Agent 共用同一长 Context，隔离收益在哪里？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

## Part V｜Harness Engineering

### 24｜为什么最终需要 Harness：横切能力由谁承载

#### 1. 本篇定位

Harness 入口篇。不是先背定义，而是汇总前四部分已长出的横切问题。

#### 2. 为什么现在学它

当前系统已有 Model Adapter、Tool Runtime、Loop、State、Context、Memory、Workflow、Policy、Budget、Trace、Eval 和 Recovery。若它们散落在 Agent、Host 与 Tool 内部，会重复、冲突并无法统一作用域。

#### 3. 学完以后应该能回答的问题

- 哪些问题迫使 Harness 出现？
- Harness 为什么不是 Model Wrapper？
- Harness 为什么不是 Agent Loop？
- 横切 Policy、Session 和 Trace 为什么需要统一 Scope？
- 什么时候轻量 Runtime 已经足够？

#### 4. 前置知识

01-22；23 为 Advanced / Optional，不是本篇前置。

#### 5. 核心概念

Harness、Cross-cutting Concern、Control Plane、Capability Composition、Scope、Lifecycle、Host Integration。

#### 6. 核心心智模型

~~~text
Runtime 负责继续执行
Harness 负责以什么能力、身份、边界、预算和证据执行
~~~

#### 7. 正文详细框架

1. 横切机制清单  
   1.1 Context、Tool、State、Policy、Budget、Trace、Recovery 分散会怎样。  
   1.2 材料：重复责任热力图。
2. 统一作用域  
   2.1 User / Host / Agent / Run / Session / Tool。  
   2.2 论点：同一动作的权限、预算与 Trace 必须关联。
3. Harness 最早的形态  
   3.1 可能只是组合与控制层，不要求插件平台。  
   3.2 材料：Runtime v0 → Harness v0 演化图。
4. 不是什么  
   4.1 不是模型 SDK、Prompt 文件、Loop 或 Workflow。  
   4.2 也不是行业统一类名。
5. 何时不需要  
   5.1 单次、低风险、固定 Tool 的小应用。  
   5.2 引出 25 Runtime vs Harness。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；把 Lab 03 的 Policy / Trace 从 Loop 类中外移，做架构重构练习。

#### 9. 与 DeepSeek Harness 的关系

28 先用本篇问题建立 DSH Evidence-first 阅读地图。

#### 10. 与 BuildPilot 的关系

只有能回指真实风险的机制才进入 BuildPilot Harness。

#### 11. Evidence 要求

课程前四部分问题矩阵、至少两个 Agent Runtime / Harness 架构对照。

#### 12. 最容易混淆的概念

Harness ≠ Agent；Harness ≠ Workflow；横切层 ≠ 必须独立进程；能力多 ≠ Harness 成熟。

#### 13. 本篇明确不讲什么

不介绍 DSH 类名，不画 BuildPilot 最终架构。

#### 14. 学习检查

- Tool Policy 在 CLI 与 Web 各复制一份，会产生什么风险？
- 三只只读 Tool 的一次性 Demo 是否必须插件化？
- Harness 新增一层抽象时应证明什么收益？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Foundation**。正式建立本篇核心概念的定义、作用域与边界，后续不再从零解释。

### 25｜Agent Runtime vs Harness：执行内核与工程控制面

#### 1. 本篇定位

边界映射篇。建立可用但不绝对化的 Runtime / Harness 责任分配。

#### 2. 为什么现在学它

24 说明横切能力需要承载者。新问题是哪些仍属于执行内核，哪些应由外层组合和约束；不同框架可能切法不同。

#### 3. 学完以后应该能回答的问题

- Runtime 的最小闭环是什么？
- Harness 最小控制面是什么？
- Session、Tool Registry 和 Context 归谁是否有唯一答案？
- Host 与 Harness 怎样分工？
- 边界为什么应按替换频率、作用域和风险判断？

#### 4. 前置知识

24；06-11 Runtime 机制。

#### 5. 核心概念

Execution Kernel、Control Plane、Data Plane、Host、Ownership、Scope、Replacement Boundary、Interface Seam。

#### 6. 核心心智模型

~~~text
Runtime：Request → Decide → Act → Observe → Stop
Harness：Compose → Constrain → Record → Recover
Host：Receive / Present / Lifecycle
~~~

#### 7. 正文详细框架

1. Runtime 最小闭环  
   1.1 Adapter、Structured I/O、Loop、Tool Dispatch、State。  
   1.2 材料：最小组件图。
2. Harness 控制面  
   2.1 Profile、Capability、Policy、Session、Budget、Trace、Recovery。  
   2.2 论点：是否物理分层取决于规模。
3. 灰色地带  
   3.1 Context、Registry、Session 可以由不同实现承载。  
   3.2 材料：三种框架切法矩阵。
4. Host 边界  
   4.1 CLI、Web、IDE、CI 的输入、呈现、身份和生命周期。  
   4.2 Host 不复制执行内核。
5. 设计判断法  
   5.1 作用域、风险、替换频率、复用消费者。  
   5.2 引出 26 最小能力模型。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；为同一个 Minimal Loop 画“单模块”和“Runtime + Harness”两种部署图。

#### 9. 与 DeepSeek Harness 的关系

28-31 会检验 DSH 的实际切层，不把本篇定义强加给源码。

#### 10. 与 BuildPilot 的关系

BuildPilot 先模块化单体；接口分层不等于微服务。

#### 11. Evidence 要求

多个官方框架架构资料、责任矩阵；不需要行为实验。

#### 12. 最容易混淆的概念

Runtime ≠ Host；Harness ≠ UI；逻辑边界 ≠ 进程边界；灰色地带 ≠ 定义错误。

#### 13. 本篇明确不讲什么

不宣称唯一标准，不讨论部署平台。

#### 14. 学习检查

- Session 由 Runtime 持有是否一定错误？
- Web Host 是否应该实现自己的 Tool Policy？
- 只有一个进程时，为什么仍值得区分责任？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Mechanism**。正式解释本篇核心对象如何运行、交互和产生可观察结果。

### 26｜Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery

#### 1. 本篇定位

Harness 核心模型篇。把前面横切机制压缩成最小可评审能力集合。

#### 2. 为什么现在学它

25 已切开执行与控制，但还缺一个不会无限膨胀的 Harness 最小模型。

#### 3. 学完以后应该能回答的问题

- Harness 最小需要哪些能力？
- Capability 与 Tool / Provider 有什么差别？
- Profile 怎样收窄专用 Agent？
- Policy、Session、Trace 与 Budget 怎样关联？
- Recovery 边界怎样进入设计？

#### 4. 前置知识

18-25。

#### 5. 核心概念

Capability、Provider、Profile、Policy Set、Session Store、Trace Store、Budget Guard、Recovery Boundary、Effective Configuration。

#### 6. 核心心智模型

~~~text
Profile chooses Capabilities
→ Policy constrains use
→ Session / Trace records
→ Budget stops
→ Recovery resumes within boundary
~~~

#### 7. 正文详细框架

1. Capability Composition  
   1.1 Model、FS、Tool、Retriever、Sandbox 等能力。  
   1.2 材料：Capability / Provider / Consumer 图。
2. Profile / Effective Config  
   2.1 同一 Runtime 形成不同能力集和权限。  
   2.2 材料：Read-only / Write-enabled Profile 对照。
3. Policy / Budget  
   3.1 动作、资源、时间和成本控制。  
   3.2 论点：配置必须可 dump、可审计。
4. Session / Trace  
   4.1 Durable Fact 与 Observability 的关联和区别。  
   4.2 材料：Run ID 关联图。
5. Recovery  
   5.1 Checkpoint、Resume、外部副作用边界。  
   5.2 材料：Recovery Contract。
6. 最小验收问题  
   6.1 能否重建、收窄、拒绝、取消、恢复、评测。  
   6.2 引到 27 取舍。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；用 Lab 03 / 06 的产物做一份 Effective Profile 与 Run Receipt。

#### 9. 与 DeepSeek Harness 的关系

30-36 分别对应 Plugin、Profile、Context、Loop、Session、Tool 与运行控制。

#### 10. 与 BuildPilot 的关系

提供候选能力表，但 BuildPilot 只采用 Case 真正需要的部分。

#### 11. Evidence 要求

能力矩阵、有效配置示例、课程 Lab Trace。

#### 12. 最容易混淆的概念

Capability ≠ Tool；Provider ≠ Agent；Profile ≠ Persona Prompt；Session ≠ Trace。

#### 13. 本篇明确不讲什么

不规定插件系统，不设计所有 Host。

#### 14. 学习检查

- Diagnostic Profile 与 Write Profile 只换 Prompt 是否足够？
- Trace 和 Session 为什么不应完全合并？
- 一个 Capability 没有第二实现时，是否必须抽 Provider？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

### 27｜Harness 的设计取舍：可替换性、复杂度、Bloat 与演化

#### 1. 本篇定位

Harness 收束篇。建立“何时抽象、何时停止”的工程判断，再进入真实源码。

#### 2. 为什么现在学它

26 给出最小能力模型，但把每项都插件化会产生隐藏依赖、配置漂移和调试成本。需要用真实变化压力决定抽象。

#### 3. 学完以后应该能回答的问题

- 何时用固定核心 + 扩展点，何时用完整插件系统？
- 可替换性怎样用真实消费者证明？
- Harness Bloat 和 Drift 有哪些信号？
- 配置叠层怎样破坏可复现性？
- 什么时候应 Simplify、Reject 或 Defer？

#### 4. 前置知识

24-26；现有 Harness Engineering 系列作为延伸。

#### 5. 核心概念

Plugin Architecture、Fixed Core、Extension Seam、Bloat、Drift、Configuration Precedence、Lifecycle、Adopt / Simplify / Reject / Defer。

#### 6. 核心心智模型

~~~text
Problem pressure
→ choose smallest seam
→ observe change / reuse
→ expand only with evidence
→ slim or retire when cost exceeds value
~~~

#### 7. 正文详细框架

1. 抽象的真实驱动力  
   1.1 多 Provider、多 Host、多 Profile、独立生命周期。  
   1.2 材料：需求→抽象对照。
2. 三种架构  
   2.1 固定核心、模块化单体、Everything is a Plugin。  
   2.2 材料：收益 / 代价矩阵。
3. Bloat / Drift  
   3.1 规则、Context、插件、配置和兼容层膨胀。  
   3.2 材料：诊断指标，详细治理转 Harness Engineering 系列。
4. 可复现配置  
   4.1 Effective Config、版本、Profile 和本地 Overlay。  
   4.2 论点：不可 dump 的组合不可审计。
5. 决策记录  
   5.1 Adopt / Simplify / Reject / Defer。  
   5.2 材料：ADR 模板。
6. 进入源码教材  
   6.1 用取舍问题阅读 DSH，而不是寻找“最佳实践”。  
   6.2 引向 28。

#### 8. Engineering Lab / 示例

本篇不需要独立 Lab；把 Lab Runtime 设计成固定核心和插件化两版，做纸面复杂度评审。

#### 9. 与 DeepSeek Harness 的关系

29-37 每篇都必须报告收益、代价与替代方案。

#### 10. 与 BuildPilot 的关系

默认 Simplify；无变化压力的通用能力一律 Defer。

#### 11. Evidence 要求

现有 Harness Engineering plan、两个真实架构案例、ADR。

#### 12. 最容易混淆的概念

可插拔 ≠ 低耦合；配置化 ≠ 可复现；复杂 ≠ 成熟；Defer ≠ 永不实现。

#### 13. 本篇明确不讲什么

不重写 Harness 生命周期专栏，不提前评价 DSH。

#### 14. 学习检查

- 只有一种 Provider，为何可能不值得插件化？
- 本地 Overlay 无法导出，会破坏什么？
- 哪些指标能证明 Harness 应该瘦身？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Engineering**。把已经出现的概念推进到合同、控制、失败处理与可验证工程实践。

## Part VI｜DeepSeek Harness 源码教材

### 28｜怎样把 DeepSeek Harness 当作 Evidence-first 源码教材

#### 1. 本篇定位

源码阶段索引篇。锁定快照、事实标签和验证路线。

#### 2. 为什么现在学它

前五部分已经提出 Harness 工程问题。现在才有共同语言评判 DSH，而不是被类名牵着走。

#### 3. 学完以后应该能回答的问题

- 为什么必须锁定 tag / commit？
- 官方事实、源码事实、运行观察和推断怎样区分？
- DSH 覆盖 Runtime、Harness、Host 哪些层？
- 每篇源码课如何建立可观察入口？
- Developer Preview 的结论如何限版本？

#### 4. 前置知识

01-27。

#### 5. 核心概念

Pinned Commit、Official Fact、Source Fact、Runtime Observation、Inference、Architecture Mapping、Evidence Card。

#### 6. 核心心智模型

~~~text
前置工程问题
→ pinned docs / source
→ lifecycle / call path
→ runtime observation
→ tradeoff
→ BuildPilot decision
~~~

#### 7. 正文详细框架

1. DSH 为什么只是教材  
   1.1 通用抽象不依赖框架；材料：课程问题→DSH 子系统表。  
   1.2 不做 API 教程。
2. 锁定快照  
   2.1 commit、日期、依赖、构建、测试、运行状态。  
   2.2 材料：Baseline Manifest。
3. 四类事实标签  
   3.1 Official / Source / Observation / Inference。  
   3.2 材料：容易越界的句子改写。
4. 验证层级  
   4.1 符号→调用路径→测试→最小运行→Trace。  
   4.2 无法运行时标 Source Confirmed。
5. 阅读地图  
   5.1 Plugin、Profile、Prompt / Context、Loop、Session、Tool、Control、Extension。  
   5.2 为 29-37 建 evidence card 路由。

#### 8. Engineering Lab / 示例

本篇不设 Lab；产出 DSH Evidence Baseline 和一张 Source Map。

#### 9. 与 DeepSeek Harness 的关系

本篇定义后续所有 DSH 正文的证据合同。

#### 10. 与 BuildPilot 的关系

防止 BuildPilot 复制版本敏感或未验证机制。

#### 11. Evidence 要求

**BLOCKED：必须锁定 commit，并记录 build / test / run 状态后才能写 29-37。**

#### 12. 最容易混淆的概念

官方文档 ≠ 当前源码；源码存在 ≠ 生命周期成立；可扩展 ≠ 已内置；Source Confirmed ≠ Runtime Verified。

#### 13. 本篇明确不讲什么

不详细解释子系统，不推断官方未说明的意图。

#### 14. 学习检查

- 官方文档与 pinned source 冲突时怎样写？
- 找到类但没找到调用，能否声明功能已工作？
- 插件能实现 RAG，能否称 DSH 内置 RAG？

#### 15. 篇幅等级 / 课程权重

**S（Bridge / Overview）**。本篇承担阶段导航或课程收束，不新增需要穷举的机制。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 29｜DeepSeek Harness 总图：从 Host 启动到一次 Agent Run

#### 1. 本篇定位

DSH Architecture Overview。先建立 pinned commit 下的仓库、包、启动入口和一次 Agent Run 总图，再进入 Plugin、Profile、Context、Loop、Session 与 Tool 子系统。

#### 2. 为什么现在学它

28 已建立 Evidence-first 阅读规则，但若下一篇直接进入 Plugin，读者仍不知道 Plugin 位于哪条系统链、DSH 跨越 Runtime、Harness 还是 Product 哪些层。

#### 3. 学完以后应该能回答的问题

- pinned commit 下的 DeepSeek Harness 到底是什么：Model Wrapper、Coding Agent、Agent Runtime、Harness、完整 Product，还是跨多个层？
- Host / Entry、Profile / Bundle、Plugin Core、Agent Runtime、Session、Runtime Controls 与 Web / Headless 如何关联？
- 从 Host 启动到一次 Agent Run 的已确认调用路径是什么？
- 哪些关系只有目录或类型证据，哪些已有 Runtime Trace？
- 后续 30-37 各自位于总图哪里？

#### 4. 前置知识

24-28；能够阅读 Repository Map、入口代码和最小调用链。

#### 5. 核心概念

Repository Map、Host / Entry、Core Package Relationship、Bootstrap、Profile Resolution、Plugin Installation、Capability Availability、Agent / Session Creation、Turn / Step Lifecycle、Architecture Status。

#### 6. 核心心智模型

~~~text
Host starts
→ Profile / Bundle resolves
→ Plugins installed
→ Capabilities available
→ Agent / Session created
→ User / Inbox event
→ Turn → Step
→ Model → Tool → Session Event
→ Next Step / Final
~~~

这是一条待 pinned source 验证的调查假设，不是预设 DSH 必须完全按此实现。

#### 7. 正文详细框架

1. DSH 身份判断  
   1.1 官方定位、可运行 Host、核心包和暴露能力。  
   1.2 材料：Model Wrapper / Agent Runtime / Harness / Product 分层判定表。
2. Repository Map  
   2.1 Host / Entry、Profile / Bundle、Plugin Core、Runtime、Session、Control、Web / Headless 的实际路径。  
   2.2 材料：只画 source-confirmed 包关系，不按理想图补空白。
3. Startup Path  
   3.1 入口参数、Profile 解析、Bundle / Plugin 安装和 Capability 建立。  
   3.2 材料：带文件、符号和行号的调用链。
4. One Agent Run  
   4.1 Agent / Session 创建、Inbox / User Event、Turn、Step、Model、Tool、Session Event、Final。  
   4.2 材料：至少一条 Host → Agent Run 路径；能运行时补最小 Trace。
5. 事实等级  
   5.1 Source Confirmed、Runtime Confirmed、Partial、Proposal。  
   5.2 论点：文件夹名只能证明组织线索，不能独自证明生命周期。
6. 后续导航  
   6.1 30 Plugin → 31 Profile / Provider → 32 Context → 33 Loop → 34 Session → 35 Tool → 36 Controls → 37 Extensions。  
   6.2 材料：从总图到专题的阅读路径。

#### 8. Engineering Lab / 示例

不设独立 Lab。生成 Repository Map、Startup Call Path 与 One Run Trace 三件套；若环境不能运行，只交 Source-confirmed 路径并列出 Runtime 验证缺口。

#### 9. 与 DeepSeek Harness 的关系

本篇是 Part VI 的总导航；所有结构与生命周期结论都绑定 pinned commit，不将课程抽象硬套成 DSH 类名。

#### 10. 与 BuildPilot 的关系

提供一张可比较的真实 Harness 总图，但 BuildPilot 只在 41 以后按案例责任选择 Adopt / Simplify / Reject / Defer。

#### 11. Evidence 要求

**BLOCKED：必须取得 Pinned Commit、Repository Map、Startup Entry、Core Package Relationship，以及至少一条 Host → Agent Run 调用路径。**不能运行时可标 Source Confirmed，但不得凭目录名推断完整生命周期。

#### 12. 最容易混淆的概念

Repository Layout ≠ Runtime Architecture；Package Dependency ≠ Lifecycle Order；Source Confirmed ≠ Runtime Confirmed；Host ≠ Harness；Plugin Core ≠ 全部 Agent Runtime。

#### 13. 本篇明确不讲什么

不逐类解释 Plugin，不提前深挖 PromptContext、Session Event 或 Tool Pipeline，不评判 BuildPilot 应复制多少模块。

#### 14. 学习检查

- DSH 有 CLI 是否足以称为完整 Product？
- 看见 session 目录是否已经证明 append-only 事件模型？
- 如果源码调用链存在但环境无法启动，应怎样标记结论？
- 30-37 的每篇能否回指总图中的一个明确节点？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。它是源码阶段的导航桥，但必须包含真实 Repository Map 和至少一条调用路径，因此重于普通 S 导读。

#### 16. 概念成熟度

**Source Verification**。把 00 的地图级 Runtime / Harness / Host 与 24-27 的架构模型映射到 pinned DSH 事实。

### 30｜Everything is a Plugin：插件内核如何承载 Capability 与生命周期

#### 1. 本篇定位

源码原理篇。用插件内核验证 26-27 的 Capability Composition 与取舍。

#### 2. 为什么现在学它

28-29 已固定证据并建立总图。第一个子系统问题是 DSH 怎样组合众多 Harness 能力，并让它们拥有 Scope 和可释放生命周期。

#### 3. 学完以后应该能回答的问题

- 插件内核解决哪些真实组合问题？
- Context、Service、Event、Effect 怎样协作？
- Scope 与 Dispose 为什么重要？
- 插件化的调试和初始化代价是什么？
- 普通 DI 能否替代？

#### 4. 前置知识

26-29。

#### 5. 核心概念

Plugin、Cordis Context、Service、Event、Effect、Scope、Lifecycle、Dispose、Dependency。

#### 6. 核心心智模型

~~~text
Install Plugin
→ register Service / Event / Effect
→ operate in Scope
→ dispose reversible effects
~~~

#### 7. 正文详细框架

1. 课程问题回收  
   1.1 多 Capability / Profile / Host 为什么需要组合。  
   1.2 材料：26 能力模型映射。
2. 锁定源码对象  
   2.1 关键类型、安装入口和调用路径。  
   2.2 材料：Source Map，具体符号待 evidence card。
3. Service / Event / Effect  
   3.1 分别承担什么，怎样进入生命周期。  
   3.2 材料：一只插件追踪时序。
4. Scope / Dispose  
   4.1 Agent、Session、Host 作用域与资源释放。  
   4.2 材料：卸载测试或最小观察。
5. 收益 / 代价 / 替代  
   5.1 组合、替换 vs 隐式依赖、初始化顺序。  
   5.2 DI、模块化单体对照。
6. BuildPilot 取舍  
   6.1 默认 Simplify 为显式接口。  
   6.2 没有多个实现 / 生命周期前不照搬。

#### 8. Engineering Lab / 示例

不设课程 Lab；源码验证是追踪一个插件 install → use → dispose。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Plugin Core；所有符号和文件由 evidence card 填写。

#### 10. 与 BuildPilot 的关系

只借鉴必要 seam，不预设 Everything is a Plugin。

#### 11. Evidence 要求

**BLOCKED：Source Map、调用路径、生命周期测试 / Trace。**

#### 12. 最容易混淆的概念

Plugin Context ≠ Model Context；Plugin Event ≠ Session Event；Plugin ≠ Tool；可替换 ≠ 低复杂度。

#### 13. 本篇明确不讲什么

不遍历所有插件，不写 Cordis 教程。

#### 14. 学习检查

- 插件 dispose 后监听器仍活着，破坏什么？
- 只有一个 FS Provider 是否需要完整插件模型？
- DI 已能替换服务时，插件还需证明什么价值？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 31｜Profile、Bundle、Provider 与 Capability Seam

#### 1. 本篇定位

源码架构篇。研究 DSH 怎样从通用 Harness 组合出不同 Host 和能力集。

#### 2. 为什么现在学它

30 有插件生命周期，但还不知道如何选择插件、叠加配置和替换 Provider。

#### 3. 学完以后应该能回答的问题

- Profile、Bundle、Patch / Overlay 各解决什么？
- Effective Configuration 怎样形成？
- Provider / Consumer 怎样通过 Capability Seam 解耦？
- Web / Headless 怎样共享核心？
- 配置漂移如何暴露？

#### 4. 前置知识

25-30。

#### 5. 核心概念

Profile、Bundle、Patch、Overlay、Provider、Consumer、Capability Seam、Effective Config、Host。

#### 6. 核心心智模型

~~~text
Base Bundle + Profile + Patches / Overlay
→ Effective Plugin / Provider Set
→ Host starts scoped Agent
~~~

#### 7. 正文详细框架

1. 多 Host / 多能力集问题  
   1.1 为什么不 fork Runtime；材料：Web / Headless 对照。  
2. Profile / Bundle  
   2.1 pinned source 中的配置对象、加载顺序和冲突。  
   2.2 材料：配置叠层图。
3. Provider / Capability  
   3.1 Service Definition、Provider、Consumer 调用路径。  
   3.2 材料：Model / FS / Sandbox 一个实例。
4. Effective Config  
   4.1 Dump、版本和本地 Overlay。  
   4.2 材料：两次启动差异。
5. Host  
   5.1 生命周期、输入、呈现、身份和权限差异。  
   5.2 不复制 Loop。
6. BuildPilot 取舍  
   6.1 Read-only Profile；CLI first。  
   6.2 多 Host 与复杂叠层 Defer。

#### 8. Engineering Lab / 示例

不设课程 Lab；源码实验是 dump 两个 Profile 的 Effective Config。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Profile / Bundle / Provider / Host 模块。

#### 10. 与 BuildPilot 的关系

采用能力集思想，简化配置叠层。

#### 11. Evidence 要求

**BLOCKED：配置 Schema、加载路径、Effective Config dump。**

#### 12. 最容易混淆的概念

Profile ≠ Persona；Bundle ≠ 部署包；Provider ≠ Tool；Host ≠ Runtime。

#### 13. 本篇明确不讲什么

不做全部配置字段手册，不遍历 Provider。

#### 14. 学习检查

- 本地 Overlay 改权限但未记录，破坏什么？
- Web 与 Headless 应各有 Loop 吗？
- Read-only / Write Profile 只差 Prompt 是否足够？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 32｜System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成

#### 1. 本篇定位

源码 Context 篇。回收 02、12、13 的 Prompt / Context / Debugging 主线。

#### 2. 为什么现在学它

31 安装了不同能力，多个插件会同时贡献身份、变量、Tool Guidance、动态状态和历史，需要有序装配与冲突语义。

#### 3. 学完以后应该能回答的问题

- Prompt Section 怎样排序、覆盖和作用域化？
- PromptContext 与 Model Context 是什么关系？
- Stable / Dynamic Context 怎样分离？
- Compaction 后怎样重注入不变量？
- 如何追踪每段输入来源？

#### 4. 前置知识

02、12-13；29-31。

#### 5. 核心概念

Prompt Section、PromptContext、Variable、Provider、Ordered Assembly、Scope、Shadow / Complete（若 pinned source 存在）、Snapshot。

#### 6. 核心心智模型

~~~text
Installed Contributors
→ resolve scope / variables / order
→ assemble prompt + dynamic context + history + tools
→ request snapshot
~~~

#### 7. 正文详细框架

1. 多来源装配问题  
   1.1 身份、宿主、任务、Tool、历史由不同插件贡献。  
   1.2 材料：来源图。
2. pinned 数据结构  
   2.1 Section / Context / Provider 的真实字段和调用。  
   2.2 材料：Source Map。
3. 排序 / 冲突 / Scope  
   3.1 重复、覆盖、变量缺失和终结语义。  
   3.2 材料：负例测试。
4. Dynamic Context / History  
   4.1 变化检测、Agent / Session Scope、Tool Schema。  
   4.2 材料：两 Step Request diff。
5. Debugging / Reconstruction  
   5.1 Effective Assembly、来源与变换。  
   5.2 回看 Lab 05 Failure Taxonomy。
6. BuildPilot 取舍  
   6.1 IContextContributor + Receipt。  
   6.2 不复制无需求的完整语法。

#### 8. Engineering Lab / 示例

不设新 Lab；源码验证包括重复 Section、错误变量和 Context Snapshot。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned System Prompt subsystem、PromptContext 与 History Assembly。

#### 10. 与 BuildPilot 的关系

借鉴来源可追踪的 Contributor，不照搬所有插件语义。

#### 11. Evidence 要求

**BLOCKED：装配顺序、冲突行为和 Request Trace 必须验证。**

#### 12. 最容易混淆的概念

PromptContext 名称 ≠ 通用 Context 定义；Section ≠ Message；顺序 ≠ 权威性；稳定前缀 ≠ 稳定事实。

#### 13. 本篇明确不讲什么

不做 Prompt 文案教程，不假设未验证关键字行为。

#### 14. 学习检查

- 两个插件贡献冲突安全规则，静默覆盖是否合理？
- 稳定前缀含当前分支名会怎样？
- 只保存最终文本为何仍难调试？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 33｜Inbox、Turn、Step 与 Agent Loop

#### 1. 本篇定位

源码生命周期篇。把 08 的 Loop 映射到 DSH 的实际消息入口和 Step 执行。

#### 2. 为什么现在学它

32 已知道每个 Step 的请求怎样装配，现在追踪外部事件怎样唤醒 Turn、模型怎样调用 Tool、何时继续或停止。

#### 3. 学完以后应该能回答的问题

- Inbox 与用户 Message 有何关系？
- 一次 Turn 为何可产生零到多个 Step？
- Tool Batch 怎样调度？
- Continue / Stop 由谁决定？
- Cancellation 怎样穿过 Loop？

#### 4. 前置知识

08 Loop；29、31-32。

#### 5. 核心概念

Inbox、Wakeup、Turn、Step、Request、Tool Batch、Continuation、Stop、Cancellation。

#### 6. 核心心智模型

~~~text
Inbox Event → Turn
→ Step Request → Model
→ Tool Batch / Final
→ Events
→ Next Step / Stop
~~~

#### 7. 正文详细框架

1. Inbox → Turn  
   1.1 Host 写入什么、Runtime 监听什么。  
   1.2 材料：入口调用路径。
2. Step Lifecycle  
   2.1 Assembly、call、parse、event。  
   2.2 材料：Step State 图。
3. Tool Batch  
   3.1 并发 / 顺序规则和结果汇总。  
   3.2 材料：单 / 多 Tool Trace。
4. Stop / Continue  
   4.1 完成、Policy、Budget、Error、Cancel。  
   4.2 论点：模型不是唯一停止权。
5. 四条轨迹  
   5.1 无 Tool、单 Tool、多 Tool、取消。  
   5.2 与 08 Lab 03 对照。
6. BuildPilot 取舍  
   6.1 显式 Turn / Step。  
   6.2 不提前复制多 Tool 并发。

#### 8. Engineering Lab / 示例

不设新 Lab；运行 / 测试四条 DSH Trace。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Agent Lifecycle 与 Inbox / Turn / Step 调用链。

#### 10. 与 BuildPilot 的关系

采用可观察状态，不照搬无需求的并发行为。

#### 11. Evidence 要求

**BLOCKED：关键符号、调用路径、四条运行或测试 Trace。**

#### 12. 最容易混淆的概念

Inbox ≠ Chat UI；Turn ≠ Step；Tool Batch ≠ Multi-Agent；Stop ≠ Success。

#### 13. 本篇明确不讲什么

不展开 Session 持久化和 Tool Policy。

#### 14. 学习检查

- Policy 在模型前拒绝，Turn 是否留事件？
- 多 Tool 结果顺序会影响下一 Step 吗？
- Cancel 发生在 Tool 中，哪些状态要落盘？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 34｜Append-only Session Event：Replay、Resume、Fork 与 Projection

#### 1. 本篇定位

源码状态篇。回收 11、14、15、21 的 Checkpoint、Session、Working State 和 Replay。

#### 2. 为什么现在学它

33 持续产生模型、Tool、Policy 和状态事件。List<Message> 无法完整恢复、审计或构建不同视图。

#### 3. 学完以后应该能回答的问题

- Durable Event 与 Live Event 怎样区分？
- Transcript、Model History、Domain State 和 Trace 为何是不同 Projection？
- Replay 与重新推理有何差别？
- Resume / Fork 继承什么？
- Compaction 怎样保留来源和未完成状态？

#### 4. 前置知识

11、14-15、21；33。

#### 5. 核心概念

Append-only Event、Durable / Live Event、Projection、Replay、Resume、Fork、Transcript、Model History、Compaction Event。

#### 6. 核心心智模型

~~~text
Session Events
├→ Model History
├→ UI Transcript
├→ Domain State
├→ Trace
└→ Resume / Fork
~~~

#### 7. 正文详细框架

1. List<Message> 的不足  
   1.1 无 Policy、Tool 生命周期、取消、预算。  
   1.2 材料：信息缺口表。
2. pinned Event Model  
   2.1 类型、序号、Run / Turn / Step 关联。  
   2.2 材料：Source Schema。
3. Projection  
   3.1 History、Transcript、State、Telemetry。  
   3.2 材料：同一事件流四个视图。
4. Replay / Resume / Fork  
   4.1 重建、继续、分支的语义和外部状态边界。  
   4.2 材料：时序。
5. Compaction  
   5.1 新事件 vs 改写历史，Evidence / unverified 保留。  
   5.2 材料：长 Session 测试。
6. BuildPilot 取舍  
   6.1 简化 JSONL Event Store。  
   6.2 只保留领域需要事件。

#### 8. Engineering Lab / 示例

不设新 Lab；从 pinned Event Stream 重建 History，并 Fork 一条分支。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Session subsystem。

#### 10. 与 BuildPilot 的关系

借鉴 append-only 与 Projection；事件类型重新按 BuildPilot 领域设计。

#### 11. Evidence 要求

**BLOCKED：事件类型表、写读路径、Replay / Resume / Fork 测试。**

#### 12. 最容易混淆的概念

Session Event ≠ Plugin Event；Replay ≠ 重新推理；Transcript ≠ Model History；Fork ≠ 复制外部世界。

#### 13. 本篇明确不讲什么

不构建通用 Event Sourcing 平台。

#### 14. 学习检查

- 删除旧 Result 写摘要会破坏什么？
- Replay 能保证相同模型输出吗？
- Fork 后哪些 Budget / Permission 应继承？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 35｜Tool Registry 与 Tool Execution Pipeline

#### 1. 本篇定位

源码执行篇。验证 05-07、19 的 Tool Runtime 与 Policy 抽象。

#### 2. 为什么现在学它

33-34 已有 Loop 与事件流，现在追踪一次 Tool Call 从 Registry 到 Policy、Execute、Result 和 Session 的全过程。

#### 3. 学完以后应该能回答的问题

- Registry 怎样生成模型视图？
- Canonical Arguments 和 Host Metadata 怎样分开？
- Allow / Deny / Ask 怎样合并？
- Timeout、Cancellation 和并发在哪里？
- Model Content、UI Presentation、Persisted Result 怎样分离？

#### 4. 前置知识

05-07、19；33-34。

#### 5. 核心概念

Tool Registry、Provider、Canonical Value、Policy Decision、Pre / Post Hook、Execution Result、Presentation、Persist。

#### 6. 核心心智模型

~~~text
Registry → Model View
Call → Canonicalize → Validate → Policy
→ Execute → Normalize → Persist
→ Model / UI views
~~~

#### 7. 正文详细框架

1. Registry / Discovery  
   1.1 作用域、去重、Provider 和模型列表。  
   1.2 材料：Source Map。
2. Input Pipeline  
   2.1 Schema、Canonical、Host Metadata。  
   2.2 材料：坏参数测试。
3. Policy Chain  
   3.1 Pre-execute、Deny / Ask / Allow 合并。  
   3.2 材料：冲突 Policy 负例。
4. Execute  
   4.1 Timeout、Cancellation、Concurrency、Error。  
   4.2 材料：取消 Trace。
5. Output Pipeline  
   5.1 Schema、Spill、Model、UI、Persist。  
   5.2 材料：大 Result。
6. BuildPilot 取舍  
   6.1 保留不可绕过 Pipeline。  
   6.2 UI Presentation 与通用复杂度 Defer。

#### 8. Engineering Lab / 示例

不设新 Lab；以 pinned DSH Tool 跑坏参数、Deny、Timeout、Cancel、大 Result 五条轨迹。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Tool subsystem / execution pipeline。

#### 10. 与 BuildPilot 的关系

三只只读 Tool 采用简化 Pipeline。

#### 11. Evidence 要求

**BLOCKED：Source Map + 五类负例 Trace。**

#### 12. 最容易混淆的概念

Registry ≠ Permission；Provider ≠ Tool；Canonical ≠ Authorized；Presentation ≠ Model Content。

#### 13. 本篇明确不讲什么

不遍历全部 Tool，不实现 BuildPilot Tool。

#### 14. 学习检查

- A Deny、B Allow，最终怎样？
- UI 要全文、模型要摘要，应分离什么？
- Execute 成功但 Result Schema 失败，Session 记什么？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 36｜Cost、Compaction、Trace、Cancellation 与 Recovery

#### 1. 本篇定位

源码运行控制篇。回收 11、13、20-22 的长任务与可靠性机制。

#### 2. 为什么现在学它

32-35 已连通 Context、Loop、Session 和 Tool。长 Run 需要 Budget、Compaction、Trace、Cancel 与 Recovery 在真实接点协作。

#### 3. 学完以后应该能回答的问题

- Usage 在哪层累计？
- Compaction 何时触发、怎样写入 Session？
- Cancellation 如何传播？
- Trace 怎样关联 Context、Model、Tool 和 Policy？
- Recovery 能保证什么，不能保证什么？

#### 4. 前置知识

11、13、20-22；32-35。

#### 5. 核心概念

Usage Ledger、Budget Policy、Compaction Trigger、Rehydration、Trace Correlation、Cancellation Propagation、Recovery Boundary。

#### 6. 核心心智模型

~~~text
Run Controls observe every Step
→ budget / compact / trace / cancel
→ durable event
→ resume within explicit boundary
~~~

#### 7. 正文详细框架

1. 横切接点  
   1.1 Request 前、Model 后、Tool 前后、Session 写入。  
   1.2 材料：Hook Map。
2. Usage / Budget  
   2.1 字段、累计与 Stop。  
   2.2 材料：长 Run Usage Trace。
3. Compaction  
   3.1 触发、摘要、History / Projection 与重注入。  
   3.2 材料：前后 Request diff。
4. Trace  
   4.1 Context、Model、Tool、Policy、Error、Usage。  
   4.2 材料：Correlation Graph。
5. Cancel / Recovery  
   5.1 信号、落盘、Resume、外部副作用边界。  
   5.2 材料：Cancel + Resume 实验。
6. BuildPilot 取舍  
   6.1 Per-run Budget / Cancel / Trace 先做。  
   6.2 Compaction / Resume 仅长调查采用。

#### 8. Engineering Lab / 示例

不设课程 Lab；源码实验是长 Session → Compaction → Cancel → Resume。

#### 9. 与 DeepSeek Harness 的关系

对应 pinned Cost / Context Lifecycle / Trace / Recovery mechanisms。

#### 10. 与 BuildPilot 的关系

采用最小运行控制接口，不宣称具备 DSH 全部恢复。

#### 11. Evidence 要求

**BLOCKED：Usage 验证、长 Session、Cancel / Resume Trace。**

#### 12. 最容易混淆的概念

Compaction ≠ Memory；Trace ≠ Session；Cancel ≠ Rollback；Resume ≠ 外部恢复。

#### 13. 本篇明确不讲什么

不重讲成本公式，不声称恢复所有副作用。

#### 14. 学习检查

- Budget 在 Step 后检查，有什么超支窗口？
- Compaction 丢来源后还能调试吗？
- Tool 改外部系统后 Resume，Harness 能恢复什么？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

### 37｜RAG、Skill、Workflow、Subagent 与 Web / Headless：核心事实和扩展映射

#### 1. 本篇定位

DSH 综合映射篇。严格区分已有一级对象、扩展点和我们的架构提案。

#### 2. 为什么现在学它

29-36 已掌握 DSH 总图与核心子系统。课程中的 RAG、Skill、Workflow、Subagent 和多 Host 未必都有同名一级实现，必须从 seam 映射而非硬找类名。

#### 3. 学完以后应该能回答的问题

- DSH 当前原生定义了哪些能力？
- Retrieval Tool 与主动 Context Provider 有何差别？
- Skill Discovery 应放在哪个 Scope？
- Workflow 由 Host、Tool 还是插件状态机持有？
- Web / Headless 与 Subagent 怎样复用核心？

#### 4. 前置知识

16-17、23；28-36。

#### 5. 核心概念

Core Fact、Documented Extension、Source-only Mechanism、Architecture Mapping、Retrieval Provider、Skill Loader、Workflow Host、Subagent Provider、Multiple Hosts。

#### 6. 核心心智模型

~~~text
Need
→ check pinned DSH core fact
→ if absent choose Plugin / Context / Tool / Host seam
→ label as our mapping
→ evaluate tradeoff
~~~

#### 7. 正文详细框架

1. Fact Matrix  
   1.1 Core / Documented Extension / Source-only / Proposal。  
   1.2 材料：带证据引用的矩阵。
2. RAG  
   2.1 Retrieval Tool vs Context Provider；权限、引用、新鲜度。  
   2.2 材料：两种控制权时序。
3. Skill  
   3.1 Discovery、Instructions、Assets 与 Agent Scope。  
   3.2 材料：插件 / Context 映射图。
4. Workflow  
   4.1 Host State Machine、Workflow Tool、Code Orchestration。  
   4.2 论点：Loop 不等于业务 Workflow。
5. Subagent / Hosts  
   5.1 Provider / Profile / Web / Headless 的实际边界。  
   5.2 材料：共享核心图。
6. DSH 设计成绩单  
   6.1 Adopt / Simplify / Reject / Defer。  
   6.2 引入 BuildPilot ADR 输入。

#### 8. Engineering Lab / 示例

不设课程 Lab；为“历史故障检索”画 Tool 与 Context Provider 两版设计。

#### 9. 与 DeepSeek Harness 的关系

本篇是 DSH 阶段综合结论，绝不把 Mapping 写成内置事实。

#### 10. 与 BuildPilot 的关系

形成 BuildPilot Adopt / Simplify / Reject / Defer 矩阵。

#### 11. Evidence 要求

**BLOCKED：汇总 28-36 evidence card 与官方扩展文档。**

#### 12. 最容易混淆的概念

Mapping ≠ Source Fact；可扩展 ≠ 已内置；Workflow ≠ Loop；Host ≠ Agent。

#### 13. 本篇明确不讲什么

不实现插件，不给 DSH 做功能宣传。

#### 14. 学习检查

- 无 Skill 类是否说明不能加载领域方法？
- 主动注入与模型按需检索，谁掌控时机？
- CLI-only BuildPilot 为什么不该复制多 Host？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Source Verification**。在 pinned DeepSeek Harness 版本中验证前面建立的抽象，不从类名反推通用标准。

## Part VII｜BuildPilot Design：从两个案例反推领域 Agent

### 38｜游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent

#### 1. 本篇定位

BuildPilot 的入口篇。先识别游戏研发与交付中的问题类型，再决定是否需要 Agent。

#### 2. 为什么现在学它

前六个 Part 已建立通用能力和 Harness 判断标准。现在必须回到业务问题，避免拿着 Agent 反找场景。

#### 3. 学完以后应该能回答的问题

- 游戏生产链上有哪些信息断点与调查断点？
- 确定性任务为什么优先 Script 或 Rule？
- 什么样的 Investigation Gap 值得引入 Agent？
- BuildPilot 第一阶段为何应只读、可审计？

#### 4. 前置知识

01-37，尤其 10、18-22、24-27。

#### 5. 核心概念

Problem Space、Production Pipeline、Deterministic Task、Investigation Gap、Script、Rule、Workflow、Agent、Read-only First。

#### 6. 核心心智模型

~~~text
需求 → 配置 → 代码 / 资源 → 构建 → 制品 → 发布 → 运行 → 反馈
        │          │          │        │        │
        └──── 信息、证据与责任边界会在这里断裂 ────┘

固定输入固定输出       → Script
显式条件与约束         → Rule
步骤稳定、分支可枚举   → Workflow
目标明确、路径需调查   → Agent
~~~

#### 7. 正文详细框架

1. 游戏生产不是一个“写代码”问题  
   1.1 Unity、资源、CI、制品、设备与线上证据的边界。  
   1.2 材料：端到端问题空间图。
2. 四种自动化形态  
   2.1 Script / Rule / Workflow / Agent 的输入、控制权与失败模式。  
   2.2 材料：决策矩阵与反例。
3. Agent-worthy 问题  
   3.1 需要跨源取证、动态选择下一步、保留调查状态。  
   3.2 论点：不确定性在路径，不在验收标准。
4. BuildPilot 初始约束  
   4.1 C#/.NET、Unity/Jenkins、CLI、只读诊断、审批后写入。  
   4.2 材料：范围盒与非目标。
5. 两个课程案例  
   5.1 Unity 编译诊断：窄而可验证。  
   5.2 启动性能调查：长链路且多假设。

#### 8. Engineering Lab / 示例

拿 12 个真实或虚构的研发任务做四分类，并为两个 Agent 候选写“目标—未知路径—证据—停止条件”。

#### 9. 与 DeepSeek Harness 的关系

用 DSH 的 Host / Harness / Capability 分离帮助判断共性基础设施，但不复制其产品形态。

#### 10. 与 BuildPilot 的关系

确立 BuildPilot 的问题空间、首阶段安全边界与两个设计案例。

#### 11. Evidence 要求

使用仓库现有交付、构建、诊断材料形成场景清单；未有运行证据的案例明确标为 Design Scenario。

#### 12. 最容易混淆的概念

自动化 ≠ Agent；复杂 Workflow ≠ Agent；能调用 LLM ≠ 值得调用 LLM；只读 ≠ 无风险。

#### 13. 本篇明确不讲什么

不实现 BuildPilot，不承诺覆盖所有游戏研发环节，不把写操作纳入第一阶段。

#### 14. 学习检查

- 一个固定 Jenkins 发布流程为什么通常不是 Agent？
- “编译失败”中哪部分是确定规则，哪部分是调查？
- 若验收标准也无法定义，适合立即自动化吗？

#### 15. 篇幅等级 / 课程权重

**M（Standard Core Lesson）**。本篇围绕一个完整问题建立标准知识单元，控制边界，不扩展为专题大全。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 39｜案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent

#### 1. 本篇定位

用小而硬的 Unity 编译诊断案例，完整走一遍 Agent 与 Harness 的设计闭环。

#### 2. 为什么现在学它

直接从宏大架构开始会掩盖接口问题。Golden Fixture 能让输入、答案、证据和回归都可控。

#### 3. 学完以后应该能回答的问题

- 怎样把编译失败定义成 Agent Task？
- Reference Answer 为什么不能直接塞给 Agent？
- 如何区分源码事实、编辑器验证与推断？
- 如何判断诊断结果可回归？

#### 4. 前置知识

18-22、24-27、38。

#### 5. 核心概念

Golden Fixture、Reference Answer、Observation、Hypothesis、Evidence Link、Diagnosis Contract、Read-only Tool、Regression Case。

#### 6. 核心心智模型

~~~text
Broken fixture
→ inspect project / asmdef / compiler output
→ build hypotheses
→ gather discriminating evidence
→ diagnosis + confidence + evidence
→ compare with hidden reference answer
~~~

#### 7. 正文详细框架

1. Fixture 契约  
   1.1 固定版本、故障注入、允许工具、禁止写入、停止条件。  
   1.2 材料：Fixture Manifest。
2. 任务与答案分离  
   2.1 Agent 看到什么，Evaluator 看到什么。  
   2.2 论点：Golden Answer 是评价资产，不是上下文提示。
3. 最小 Tool Set  
   3.1 文件、项目结构、编译日志与可选 Unity 验证。  
   3.2 材料：工具输入输出 Schema。
4. 调查 Loop  
   4.1 Observation → Hypothesis → Next Action → Evidence。  
   4.2 材料：一次成功与一次误判 Trace。
5. Evidence Contract  
   5.1 source / config / log / editor-run / inference 分级。  
   5.2 论点：未运行 Unity 时不得声称编译已恢复。
6. Eval  
   6.1 根因、证据定位、越权、成本和停止条件。  
   6.2 材料：最小评分表。

#### 8. Engineering Lab / 示例

这是 BuildPilot 设计案例，不是前置 Lab。产出 Fixture Manifest、Tool Contract、示例 Trace 与 Eval Rubric，不实现 Runtime。

#### 9. 与 DeepSeek Harness 的关系

借鉴 Tool Pipeline、Session Event 与 Trace 分层；验证哪些概念对单 Host 诊断仍然必要。

#### 10. 与 BuildPilot 的关系

形成 BuildPilot 的第一条 Reference Task，并反推最小接口。

#### 11. Evidence 要求

若没有可公开复现的 Unity Fixture、版本信息和真实编译输出，本篇实验结果标为 **BLOCKED**；设计契约仍可先评审。

#### 12. 最容易混淆的概念

Fixture ≠ 生产项目；Reference Answer ≠ Prompt；找到可疑文件 ≠ 证明根因；source evidence ≠ runtime verification。

#### 13. 本篇明确不讲什么

不自动修改项目，不提交修复，不把一次命中当成通用能力。

#### 14. 学习检查

- 为什么答案文件必须与 Agent Context 隔离？
- 只有源码引用、没有 Unity 运行时，结论应标什么状态？
- 如何构造“看起来相似但根因不同”的回归样本？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 40｜案例 B：启动性能调查——设计一个长链路、多假设 Agent

#### 1. 本篇定位

用启动耗时回退案例，检验 Agent 在跨阶段调查、Working Memory 与动态计划上的价值。

#### 2. 为什么现在学它

编译诊断路径较短。性能回退需要先定义指标，再跨代码、资源、构建和运行数据逐步缩小范围。

#### 3. 学完以后应该能回答的问题

- “启动从 15 秒变 23 秒”为什么还不是可执行任务？
- 如何构造并更新假设树？
- Working Memory 保存哪些调查状态？
- 何时继续取证，何时停止并请求人工？

#### 4. 前置知识

12-16、18-22、38-39。

#### 5. 核心概念

Metric Contract、Stage Timeline、Baseline、Hypothesis Tree、Information Gain、Working Memory、Context Debugging、Stop Rule。

#### 6. 核心心智模型

~~~text
metric definition + baseline
→ stage decomposition
→ hypothesis tree
→ choose highest-information observation
→ update working memory
→ evidence-backed conclusion or escalation
~~~

#### 7. 正文详细框架

1. 先定义“启动”  
   1.1 起点、终点、设备、版本、冷暖启动、统计口径。  
   1.2 材料：Metric Contract 与错误口径反例。
2. 阶段化时间线  
   2.1 进程、引擎、资源、热更、登录、首屏。  
   2.2 材料：Timeline 图。
3. 假设树  
   3.1 变更相关、平台相关、数据相关、环境相关。  
   3.2 论点：按信息增益选下一步，不按直觉堆工具。
4. Working Memory  
   4.1 已知事实、未决假设、排除项、来源、下一步。  
   4.2 材料：Investigation State Schema。
5. Context Debugging  
   5.1 指标定义丢失、日志混批、旧结论污染与来源断裂。  
   5.2 材料：污染前后 Trace 对照。
6. 终止与评价  
   6.1 找到主导阶段、证据不足、预算耗尽、需设备复测。  
   6.2 材料：停止条件和 Eval 路径。

#### 8. Engineering Lab / 示例

这是 BuildPilot 设计案例。用合成的三阶段 Trace 推演两轮调查，重点检查 Working Memory 更新，不声称得到真实项目结论。

#### 9. 与 DeepSeek Harness 的关系

检验 Session Event、Compaction、Budget 和 Cancellation 在长调查中的实际作用。

#### 10. 与 BuildPilot 的关系

提供第二条 Reference Task，补足 Compile 案例无法暴露的长时状态与上下文问题。

#### 11. Evidence 要求

真实数字、设备、版本和阶段事件若未提供，必须用合成数据并标记；真实案例结论为 **BLOCKED**。

#### 12. 最容易混淆的概念

总耗时 ≠ 单阶段问题；相关变更 ≠ 根因；Working Memory ≠ 原始日志仓库；压缩摘要 ≠ 证据。

#### 13. 本篇明确不讲什么

不做通用性能优化教程，不伪造实测收益，不自动执行设备或生产环境操作。

#### 14. 学习检查

- 起点不同的两组“启动耗时”能直接比较吗？
- 为什么排除过的假设仍需保留来源？
- 预算将尽时应输出什么，而不是硬给根因？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 41｜从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块

#### 1. 本篇定位

架构推导篇。用 39-40 暴露的共性与差异反推 BuildPilot Design v1，而非先画框架图。

#### 2. 为什么现在学它

只有经过两个不同难度案例，才能判断哪些能力属于 Harness，哪些属于领域 Capability。

#### 3. 学完以后应该能回答的问题

- 两个案例共享什么，变化什么？
- Host、Harness、Agent Runtime、Context 与 Capability 如何分层？
- 为什么 v1 适合模块化单体？
- 哪些接口现在必须稳定，哪些应延迟？

#### 4. 前置知识

24-27、37-40。

#### 5. 核心概念

Change Axis、Stable Core、Modular Monolith、Host、Harness、Agent Runtime、Context Plane、Capability Plane、Domain Pack、ADR。

#### 6. 核心心智模型

~~~text
CLI Host
  → Harness: policy / session / trace / budget / recovery
    → Agent Runtime: loop / state / planning
      → Context Plane + Capability Plane
        → Unity Compile Pack / Startup Investigation Pack
~~~

#### 7. 正文详细框架

1. 案例差异表  
   1.1 输入、时长、工具、状态、证据、评价。  
   1.2 材料：Common / Variable Matrix。
2. 稳定核心  
   2.1 Turn/Step、Tool Call、Session Event、Policy、Trace。  
   2.2 论点：共性来自控制责任，不来自类名相似。
3. 领域变化轴  
   3.1 Tool、Skill、Knowledge、Workflow、Evaluator。  
   3.2 材料：两个 Domain Pack 对照。
4. 模块边界  
   4.1 Host / Harness / Runtime / Context / Capability / Domain。  
   4.2 材料：依赖方向图和禁止依赖。
5. 部署选择  
   5.1 v1 模块化单体；何时才拆服务。  
   5.2 论点：先稳定协议与证据，不为未来流量预付复杂度。
6. ADR 入口  
   6.1 Adopt / Simplify / Reject / Defer DSH 设计。  
   6.2 材料：关键 ADR 清单。

#### 8. Engineering Lab / 示例

把两个案例卡片贴到架构模块上，删除没有任何案例责任的模块，形成最小 Design v1 图。

#### 9. 与 DeepSeek Harness 的关系

把 Part VI 的事实转为 ADR 输入；名称相同不代表职责照搬。

#### 10. 与 BuildPilot 的关系

给出 BuildPilot Design v1 的一级模块和依赖方向。

#### 11. Evidence 要求

每个一级模块至少回指一个案例责任和一个失败模式；否则标记 Deferred，不进入 v1。

#### 12. 最容易混淆的概念

架构图 ≠ 运行证明；模块 ≠ 微服务；Domain Pack ≠ Provider 万能抽象；稳定核心 ≠ 永不变化。

#### 13. 本篇明确不讲什么

不写项目脚手架，不选数据库和消息队列，不设计 Web UI。

#### 14. 学习检查

- 一个模块若只因 DSH 存在而存在，足够吗？
- Tool Schema 应依赖 Unity Pack 还是反过来？
- 什么时候拆服务才有证据基础？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 42｜BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位

#### 1. 本篇定位

细化 BuildPilot 的信息面与能力面，明确 Working Memory、知识库、RAG、Skill、Tool 和 Workflow 的职责。

#### 2. 为什么现在学它

架构分层之后，最容易重新混在一起的是“给 Agent 的东西”：资料、状态、方法、动作和流程。

#### 3. 学完以后应该能回答的问题

- 当前调查状态放哪里？
- 团队文档怎样进入 Context，又如何保留引用？
- Skill 何时加载，Tool 何时调用？
- 稳定流程如何与 Agent 决策结合？

#### 4. 前置知识

12-17、31-37、39-41。

#### 5. 核心概念

Context Source、Working Memory Store、Knowledge Source、Retrieval Contract、Skill Catalog、Tool Catalog、Workflow Definition、Provenance、Freshness。

#### 6. 核心心智模型

~~~text
Knowledge Source --retrieve--> cited context
Skill Catalog ----load-------> method instructions
Working Memory --------------> current investigation state
Tool Catalog -----execute----> observations / side effects
Workflow --------------------> constrained stage transitions
~~~

#### 7. 正文详细框架

1. Context Source Registry  
   1.1 用户输入、项目事实、Session、检索结果、工具观察。  
   1.2 材料：来源优先级与生命周期表。
2. Working Memory  
   2.1 Task State / Investigation State Schema。  
   2.2 论点：结构化状态与对话历史双轨保存。
3. Knowledge / RAG  
   3.1 索引、检索、权限、新鲜度、引用与拒答。  
   3.2 材料：知识飞轮入口与污染防护图。
4. Skill  
   4.1 Discovery、适用条件、版本、依赖与渐进加载。  
   4.2 材料：Compile / Startup 两个 Skill Card。
5. Tool  
   5.1 Schema、权限、超时、幂等、结果规范化。  
   5.2 材料：Read-only Tool Catalog。
6. Workflow  
   6.1 固定 Gate + Agent 调查节点。  
   6.2 论点：流程约束 Agent，不吞掉 Agent。
7. 知识飞轮边界  
   7.1 Trace → Review → Curated Knowledge / Skill / Eval。  
   7.2 材料：候选知识进入正式库的审核门。

#### 8. Engineering Lab / 示例

为 Compile 与 Startup 各画一张 Context Bill of Materials，标注来源、生命周期、权限和 Token 策略。

#### 9. 与 DeepSeek Harness 的关系

参考 Provider / Context / Tool seam，但 BuildPilot 明确增加领域级 Working Memory 和知识治理契约。

#### 10. 与 BuildPilot 的关系

定义 BuildPilot Design v1 的 Context Plane 与 Capability Plane。

#### 11. Evidence 要求

知识飞轮文章只可使用已审查案例说明机制；若尚无真实 Trace 到知识条目的闭环，成效结论标为 **BLOCKED**。

#### 12. 最容易混淆的概念

Knowledge Base ≠ Working Memory；RAG ≠ Context Engineering；Skill ≠ Tool；Workflow ≠ Prompt Template；Trace ≠ 可直接入库的知识。

#### 13. 本篇明确不讲什么

不选择向量数据库，不实现索引管线，不宣称团队知识已形成飞轮。

#### 14. 学习检查

- 一条刚产生的工具输出应直接进长期知识库吗？
- Skill 为什么需要版本和适用条件？
- Workflow 的固定 Gate 与 Agent 的动态决策怎样共存？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 43｜BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval

#### 1. 本篇定位

可靠性设计篇。把前面分散的控制能力组织为 BuildPilot 的一条可运行、可诊断、可回归的治理闭环。

#### 2. 为什么现在学它

Capability 决定能做什么，治理面决定能否放心使用。没有闭环的 Harness 只是工具调用容器。

#### 3. 学完以后应该能回答的问题

- 一次任务如何从准入到结束被完整治理？
- Evidence、Trace 与 Eval 如何衔接？
- Budget、Cancellation、Checkpoint 和 Recovery 各控制什么？
- 失败怎样进入回归集而不是只留在日志里？

#### 4. 前置知识

18-22、24-27、34-36、39-42。

#### 5. 核心概念

Admission Policy、Evidence Gate、Session Event、Trace Span、Budget Ledger、Checkpoint、Cancellation、Recovery Contract、Failure Taxonomy、Eval Gate。

#### 6. 核心心智模型

~~~text
admit task
→ enforce policy and budget
→ append session events + emit trace
→ collect evidence
→ checkpoint / cancel / recover
→ classify outcome
→ eval and regression
→ reviewed learning candidate
~~~

#### 7. 正文详细框架

1. Task Admission  
   1.1 Scope、权限、数据源、预算与可停止性。  
   1.2 材料：Admission Checklist。
2. Evidence + Policy  
   2.1 Claim 级证据门；只读、审批与拒绝。  
   2.2 材料：Policy Decision Event。
3. Session + Trace  
   3.1 恢复所需状态与诊断所需遥测分离。  
   3.2 材料：关联 ID 与事件示例。
4. Budget + Cancellation  
   4.1 Token、工具次数、时间、失败次数。  
   4.2 论点：预算检查点必须覆盖模型与工具边界。
5. Checkpoint + Recovery  
   5.1 内部状态恢复、外部事实复核、不可逆副作用。  
   5.2 材料：Recovery Matrix。
6. Failure → Eval → Regression  
   6.1 Failure Taxonomy、Golden Case、指标、基线与发布 Gate。  
   6.2 材料：闭环图与一个失败样本的迁移路径。

#### 8. Engineering Lab / 示例

将 Compile 案例的一次错误诊断转成 Failure Record、Eval Case 与 Regression Gate，说明每一步的审核责任。

#### 9. 与 DeepSeek Harness 的关系

对照 DSH 的 Session、Trace、Budget 与恢复机制，指出 BuildPilot 为工程诊断补充的 Evidence Gate 和 Eval 闭环。

#### 10. 与 BuildPilot 的关系

形成 BuildPilot Harness 的治理规格，不涉及具体 Runtime 代码。

#### 11. Evidence 要求

闭环图中的每条自动迁移必须有输入、输出、Owner 和失败路径；没有实际回归运行时，效果结论标为 **BLOCKED**。

#### 12. 最容易混淆的概念

日志 ≠ Trace；Trace ≠ Session；Retry ≠ Recovery；Cancel ≠ Rollback；Eval 分数 ≠ 生产安全。

#### 13. 本篇明确不讲什么

不设计生产发布权限，不自动修复项目，不把人工审核从知识入库和高风险动作中移除。

#### 14. 学习检查

- 一次 Tool Call 超时后，Retry 前要检查什么？
- 失败记录何时可以进入 Golden Dataset？
- Session 可恢复为什么仍不代表外部副作用可回滚？

#### 15. 篇幅等级 / 课程权重

**L（Major Core Lesson）**。本篇是后续多篇依赖的课程主轴，需要重点实验、Trace 或源码证据。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

### 44｜BuildPilot Design v1：设计评审、里程碑与退出条件

#### 1. 本篇定位

课程收束篇。汇总问题空间、案例、架构、治理与证据，形成一份可评审的 BuildPilot Design v1。

#### 2. 为什么现在学它

设计不是图纸集合，而是一组可追溯决策。最后需要回答为何做、做什么、不做什么，以及何时允许进入实现。

#### 3. 学完以后应该能回答的问题

- Design v1 的最小交付物是什么？
- 哪些结论是 Current、Target、Pilot、Unverified？
- 如何用两个案例评审架构完整性？
- 进入 Runtime 实现前必须满足哪些退出条件？

#### 4. 前置知识

38-43，以及全课程关键概念。

#### 5. 核心概念

Design Review、Decision Traceability、Current / Target / Pilot / Unverified、Milestone、Exit Criteria、Risk Register、Rejected / Deferred。

#### 6. 核心心智模型

~~~text
problem → case → responsibility → module → contract → evidence → decision
                                ↓
                      milestone + exit criteria
~~~

#### 7. 正文详细框架

1. Design v1 包  
   1.1 Problem Statement、Scope、Context Diagram、Module View、Contracts、ADRs、Risk Register。  
   1.2 材料：交付物目录。
2. 状态标注  
   2.1 Current / Target / Pilot / Unverified / Rejected / Deferred。  
   2.2 论点：目标设计不能伪装成当前能力。
3. 案例追溯  
   3.1 Compile 与 Startup 如何覆盖模块、失败模式和评价。  
   3.2 材料：Case-to-Architecture Traceability Matrix。
4. 关键决策  
   4.1 模块化单体、CLI、只读优先、C#/.NET、证据优先。  
   4.2 材料：ADR 摘要。
5. 里程碑  
   5.1 M0 Contract Spike；M1 Compile Pilot；M2 Reliability Loop；M3 Startup Pilot。  
   5.2 每阶段独立入口与退出条件。
6. Runtime 前退出条件  
   6.1 Fixture、Evidence Contract、Tool Schema、Policy、Trace、Eval 与风险接受。  
   6.2 评审问题：哪些未知会推翻架构？

#### 8. Engineering Lab / 示例

进行纸面 Design Review：让评审者从任一模块反查案例责任、证据与 ADR；断链即退回设计。

#### 9. 与 DeepSeek Harness 的关系

附最终 Adopt / Simplify / Reject / Defer 表，并为每项提供 pinned-source 证据或明确的设计理由。

#### 10. 与 BuildPilot 的关系

本篇就是 BuildPilot Design v1 的课程交付终点；Runtime 实现属于后续独立阶段。

#### 11. Evidence 要求

Design v1 可完成，但所有尚未运行的实验、Unity 验证和性能结果必须保持 **BLOCKED** 或 Unverified，不用推测填空。

#### 12. 最容易混淆的概念

Design Complete ≠ Runtime Complete；Pilot ≠ Production；Target ≠ Current；路线图 ≠ 承诺日期。

#### 13. 本篇明确不讲什么

不创建 BuildPilot Runtime，不提交 Unity 修复，不给出未经估算的生产上线计划。

#### 14. 学习检查

- 能否从每个模块追溯到案例责任？
- 哪些未知一旦证伪会要求重画架构？
- Runtime 开工前，哪五项证据必须到位？

#### 15. 篇幅等级 / 课程权重

**S（Bridge / Overview）**。本篇承担阶段导航或课程收束，不新增需要穷举的机制。

#### 16. 概念成熟度

**Design Application**。把前面建立并验证的概念用于 BuildPilot Design v1，不把目标设计写成当前 Runtime。

## 20. DeepSeek Harness 证据策略

Part VI 不是二手概念介绍，而是一组源码阅读任务。写作时执行以下约束：

1. **固定版本**：在系列计划中记录仓库 URL、commit SHA、读取日期、文档版本；文章之间不漂移。
2. **证据分层**：区分 Official Docs、Pinned Source、Runnable Experiment、Architecture Mapping、Our Proposal。
3. **逐篇 Evidence Card**：至少记录 Claim、证据路径、符号、调用链、实验、状态与反证。
4. **先事实后解释**：先说明代码当前做什么，再解释为什么可能这样设计，最后才谈 BuildPilot 是否采纳。
5. **显式阻塞**：无法取得仓库、commit、依赖或运行环境时写 **BLOCKED**，不以类名猜实现。
6. **动态路径验证**：涉及生命周期、并发、取消、恢复和 Pipeline 顺序时，源码静态证据之外还要设计可运行实验。
7. **版本变化隔离**：后续版本差异另做附录或勘误，不悄悄改写原文章的证据基线。

建议 Evidence Card 模板：

~~~text
Claim:
Evidence class:
Repository / commit:
File / symbol / call path:
Observed behavior:
Runnable check:
Counter-evidence searched:
Status: CONFIRMED | PARTIAL | BLOCKED | PROPOSAL
BuildPilot implication:
~~~

## 21. BuildPilot Design v1 交付物

课程只要求设计交付，不要求 Runtime：

1. Problem Space 与 Script / Rule / Workflow / Agent 决策矩阵。
2. Unity Compile Golden Fixture 设计包。
3. Startup Investigation Scenario 设计包。
4. Context Diagram、一级模块图、依赖方向与关键时序。
5. Host、Harness、Runtime、Context、Capability、Domain Pack 职责表。
6. Tool、Skill、Workflow、Context Source、Working Memory 契约草案。
7. Evidence、Policy、Session、Trace、Budget、Recovery、Eval 治理闭环。
8. DSH Adopt / Simplify / Reject / Defer 矩阵。
9. ADR、风险登记、开放问题与 **BLOCKED** 证据表。
10. M0-M3 路线图及每阶段退出条件。

## 22. 与现有栏目的边界

| 内容 | 本课程负责 | 其他栏目负责 |
|---|---|---|
| Prompt / Context / Tool / Skill 基础 | 建立 Agent Engineering 依赖链 | AI 工具使用技巧可留在 AI 赋能栏目 |
| Harness | 从 Agent 可靠运行推导控制底座 | 独立 Harness 专题可继续追踪产品和版本生态 |
| DeepSeek Harness | pinned commit 源码教材与设计评判 | 新闻、发布说明和横向产品测评不放本课程 |
| Unity / Jenkins | 作为 Agent 案例和证据源 | 具体构建交付方法仍归交付工程 |
| 启动性能 | 作为长链路调查案例 | 指标采集和优化技术细节仍归性能栏目 |
| 团队知识飞轮 | 讲 Trace 如何经过审核变成 Knowledge / Skill / Eval | 组织制度、知识运营和平台选型可单独扩展专题 |
| BuildPilot | Design v1 与可追溯决策 | Runtime、产品化与真实试点另立执行计划 |

**编辑判断**：知识飞轮先作为本课程 Part III 与文章 42-43 的一条主线，不急着另开专栏。等积累出真实的“Trace → Review → Knowledge / Skill / Eval → Regression”案例后，再拆成独立系列；否则会过早滑向泛知识管理。

## 23. 推荐阅读顺序

首次学习按 00 → 44；23 Multi-Agent 可选：

1. 00 先建立导航地图，不要求掌握全部定义。
2. 01-11 建立从 Model API、Agent Loop、Planning 到有状态长任务的可运行链。
3. 12-17 学会组织 Context、Working Memory、Session、知识与领域方法。
4. 18-22 建立 Evidence、Policy、Budget、Trace、Eval 与 Regression 闭环。
5. 23 是 Advanced / Optional；可以完成 22 后直接进入 24。
6. 24-27 推导 Harness，避免把 Harness 学成名词清单。
7. 28-37 先建立 DSH 阅读方法和总图，再校准各子系统的源码级理解。
8. 38-44 将知识收束为 BuildPilot Design v1。

已有 Agent 实践经验的读者仍建议先读 00 和每 Part 导读；可以跳过 23，但不建议跳过 12-22 后直接进入 Harness。

## 24. 推荐写作顺序

写作顺序不必等同阅读顺序：

1. 先冻结 28 的 pinned commit 与 Evidence Card，并完成 29 所需 Repository Map / Startup Path 可得性检查。
2. 完成 38-40 的问题空间和两个案例设计，保证课程终点真实可追溯。
3. 写 01-11 和 Lab 01-04，建立最小可运行教学链。
4. 写 12-17 和 Lab 05，补齐 Working Memory、Context Debugging 与知识能力。
5. 写 18-22 和 Lab 06，形成 Trace / Failure / Eval / Regression 闭环。
6. 写 24-27，再依据证据完成 28-37；23 Multi-Agent 最后按 Optional 穿插。
7. 写 41-44，汇总为 Design v1；最后回写 00 和各 Part 导读。

## 25. 当前需要实验或证据、不得靠推测完成的文章

| 文章 | 必要证据 / 实验 | 未满足时状态 |
|---|---|---|
| 03-04 | 至少一个真实 Provider 的 Schema、流式和错误行为 | **BLOCKED** |
| 05-06 | Function Calling 往返与 Tool Runtime 失败注入 | **BLOCKED** |
| 11 | Checkpoint、取消、恢复最小实验 | **BLOCKED** |
| 12-16 | Context 装配、污染、压缩、Working Memory、RAG 引用实验 | **BLOCKED** |
| 18-22 | Evidence、Trace、Replay、Failure、Eval、Regression 串联实验 | **BLOCKED** |
| 28-37 | DSH pinned commit、Repository Map、Startup / Run 调用链与必要运行实验 | **BLOCKED** |
| 39 | 可公开 Unity Golden Fixture、版本与编译输出 | **BLOCKED** |
| 40 | 指标口径、设备、版本和启动阶段事件；否则仅用合成数据 | **BLOCKED** |
| 42-43 | 真实 Trace 经审核进入 Knowledge / Skill / Eval 的闭环 | **BLOCKED** |

**写作规则**：文章结构可以先完成，事实性结论不能用“合理推测”替代证据。阻塞解除后更新 Evidence Card，再进入初稿。

## 26. 仍需作者决策的事项

1. DSH 的目标仓库、官方文档入口与 pinned commit SHA。
2. 课程示例使用哪个模型 Provider；是否准备第二 Provider 做 Adapter 对照。
3. Lab 的统一语言：建议 C#/.NET，以贴近 BuildPilot；是否允许少量 Python 仅作对照。
4. Unity Golden Fixture 的版本、许可边界、公开范围与故障注入方式。
5. Startup Scenario 使用合成数据、脱敏历史数据，还是新采集数据。
6. 课程中的“DeepSeek Harness”最终采用官方项目名、仓库名还是编辑别名。
7. 知识飞轮是否在出现第一个真实闭环后升级为独立系列。

## 27. Terminology First-use Audit

审计规则：00 允许导航级解释；正式文章第一次使用时建立机制或工程定义；DSH 只验证 pinned source 中的实际对象，不把课程术语强加给源码。

| 术语 | 首次导航 | 正式解释 / 边界 | 审计结论 |
|---|---|---|---|
| Agent | 00 | 08 Agent Loop | 00 只给执行系统直觉，08 才建立运行机制 |
| Agentic | 00 | 08-10 通过 Loop / Planning / Decision Point 落地 | 不写成统一产品类型 |
| Copilot | 00 | 不设后续统一架构定义 | 明确是常见产品定位，不是 Agent 严格上下级 |
| Agent Runtime | 00 | 08-11、25 | 先看执行机制，再与 Harness 切层 |
| Harness | 00 | 24-27 | 00 不提前列完整能力模型 |
| Host | 00 | 25、29 | CLI / Web / IDE / Unity Editor / CI 先定位，DSH 再源码验证 |
| Capability | 00 只在图中定位 | 26、30-31 | 不与 Tool 或 Provider 混同 |
| Provider | 04 先作为模型适配语境 | 31 DSH 组合语境 | 明确同词可能承担不同生态责任 |
| Tool | 00 | 05-06 | Function Calling 与 Tool Runtime 分开解释 |
| Skill | 00 | 17 | 与 Prompt、Tool、Workflow、KB 分界 |
| Workflow | 00 | 10 | 与 Plan、Agent Loop、Verified State 分界 |
| Context | 00 | 12-13 | 与 Prompt、Session、Memory 分界 |
| Memory | 00 | 14-16 | Working Memory 不回塞普通 Memory 篇 |
| Session | 00 只定位为 Harness 能力 | 15、34 | 通用作用域先于 DSH 事件实现 |
| RAG | 00 | 16 | 与 KB、Memory、Evidence 分界 |
| Trace / Replay | 00 只定位为可靠性能力 | 21 | 与 Session、Retry、Eval 分界 |
| Eval | 00 只定位为可靠性能力 | 22 | 与测试、Judge 分数、生产安全分界 |

审计结论：核心术语不存在“正文大量使用后才首次解释”的断点；00 没有承担后续机制和源码细节。

## 28. 提纲自检

- [x] 00 能让 Agent 术语不系统的工程师先获得地图。
- [x] 00 只负责 Introduction，没有抢 Agent Loop、Harness 和 Memory 正文。
- [x] Agent / Agentic / Copilot / Runtime / Harness / Host 已完成导航级定位。
- [x] 七个 Part 与 v3 能力生长顺序一致。
- [x] Model API、Structured Output、Function Calling、Tool Runtime 先于 Agent。
- [x] Agent Loop → Planning → State Machine / Workflow → Long-running 的粒度已拆开。
- [x] Context Debugging 与 Working Memory 为独立文章。
- [x] Knowledge Base / RAG、Skill、Workflow、Agent 边界明确。
- [x] Trace、Replay、Failure Taxonomy、Eval、Regression 形成闭环。
- [x] Harness 从控制需求推导，不作为开场黑盒。
- [x] DSH 在 Evidence-first 后先建立 Architecture Overview，再进入 Plugin。
- [x] DSH 总图明确 **BLOCKED** 于 pinned commit、Repository Map、Startup Entry 和 Host → Agent Run 路径。
- [x] Multi-Agent 位于可靠性基础之后，并标记 Advanced / Optional。
- [x] 完成 Eval 后可以旁路 Multi-Agent 直接进入 Harness。
- [x] 6 个 Engineering Lab 均可独立完成，不依赖 BuildPilot Runtime。
- [x] 45 篇文章均使用完整 16 字段 Article Card。
- [x] 每篇均分配 S / M / L 并说明理由。
- [x] 每篇均标记概念成熟度，避免重复从零定义。
- [x] 编号、前置依赖、Lab、DSH 回收点和 BuildPilot 回收点已同步。
- [x] Terminology First-use Audit 已完成。
- [x] BuildPilot 只交付 Design v1，不进入 Runtime 实现。
- [x] Compile → Startup → Architecture 的案例推导顺序没有倒置。
- [x] 知识飞轮保留在课程主线，并设置独立成系列的触发条件。
- [x] 总篇数控制在 45，没有借 v3.1 扩成 50+ 专题。

## 29. 最短结论

v3.1 没有重做 v3：它用 00 降低入口门槛，用 Planning 拆分修正中段粒度，用 DSH Architecture Overview 补上源码阶段总图，再以 S / M / L 和概念成熟度约束实际写作。课程终点仍是一份由 Compile 与 Startup 两个案例反推、能够进入工程评审的 BuildPilot Design v1，而不是 BuildPilot Runtime v1。
