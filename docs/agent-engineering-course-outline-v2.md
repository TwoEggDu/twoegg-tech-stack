# Agent Engineering 课程提纲 v2

> 暂定课程名：Agent Engineering｜从基础概念、运行原理到 Harness 架构
>
> 状态：课程设计评审稿，非 canonical plan
>
> 日期：2026-08-17
>
> 上一版输入：docs/game-agent-engineering-series-outline.md

---

## 0. 文档状态

这是一份重新设计的课程规划稿，不覆盖任何 canonical plan。

当前阶段只完成：

- 课程定位与边界
- 四阶段知识依赖
- 35 篇候选课程的完整 Article Card
- DeepSeek Harness 源码 evidence 策略
- BuildPilot Design v1 毕业设计要求

当前阶段明确不做：

- 不修改根 doc-plan.md
- 不修改 docs/ai-empowerment-series-plan.md
- 不修改 docs/harness-engineering-series-plan.md
- 不建立 content/ 正文目录
- 不开始 BuildPilot Runtime 实现
- 不把漂移中的 DeepSeek Harness master 当成稳定事实

本稿通过人工审核后，才决定正式系列名、canonical plan、栏目目录、weight 和首批写作任务。

## 1. 课程一句话定位

这是一门面向 C# / Unity 工程师的 Agent Engineering 系统课程：先建立 Agent 世界的共同语言，再理解 Agent 的运行与约束机制，用锁定版本的 DeepSeek Harness 源码验证这些抽象，最后独立推导出 BuildPilot Design v1。

## 2. 为什么要学这门课

会使用 Claude Code、Codex 或 Cursor，不等于理解 Agent Engineering。

工具使用者通常知道怎样提问、授权和接入 MCP，却不一定能回答：

- 一次模型请求在什么时候变成 Agent
- Agent Runtime 与 Harness 为什么要分开
- Tool、Skill、Workflow 分别解决什么
- Context 为什么会随 Step 膨胀
- Session、Memory、Knowledge Base 与 RAG 谁是事实源
- 为什么生产 Agent 必须有 Policy、Trace、Budget、Eval 和 Recovery
- 哪些游戏研发问题根本不该 Agent 化

本课程要补的是从“会用 Agent 产品”到“能解释、评审并设计 Agent 系统”的中间层。

## 3. 课程最终学习目标

完成课程后，读者应该能够：

1. 用一致的术语解释 Model、Agent、Runtime、Harness、Tool、Skill、Workflow、Context、Memory 等对象。
2. 画出一次 Agent Run 从 Goal 到 Stop 的完整数据与控制链。
3. 判断确定性 Script、Workflow 与 Agent 的适用边界。
4. 读懂一个真实 Harness 的关键数据结构、生命周期和扩展点。
5. 区分官方文档事实、锁定源码事实、本文推断和自己的目标设计。
6. 为专用 Agent 设计 Context、Tool、Skill、Workflow、Policy、Trace、Budget 与 Eval。
7. 把 Evidence、Hypothesis、Diagnosis 和 Verification 分成可审计的数据合同。
8. 完成 BuildPilot Design v1，而不把离线 Fixture 或设计稿冒充已经运行的产品。

## 4. 目标读者与前置知识

目标读者：

- 有 C# / Unity 或游戏工具链经验，但 Agent Engineering 知识不系统的工程师
- 深度使用 Coding Agent，准备从使用者转向构建者的人
- 想把 Agent / Harness 能力形成可解释作品集的客户端、构建或工具链工程师

默认前置：

- 基本 HTTP、JSON、异步编程和接口设计经验
- 能读 C#，能理解状态机、事件和依赖注入的基本思想
- 知道 Unity Build、Jenkins、日志和 CI/CD 是什么

不要求：

- 不要求先学过 Agent 框架
- 不要求掌握向量数据库
- 不要求会训练或部署大模型
- 不要求已经实现过 BuildPilot

## 5. 整门课的核心心智模型

~~~text
User Goal
   ↓
Application / Host
   ↓
Harness：边界、资源、审计、恢复
   ↓
Agent Runtime：装配、决策、行动、状态更新
   ↓
Model Request ↔ Tool / External System
   ↓
Evidence / Result
   ↓
Stop、Handoff 或下一 Step
~~~

第二条主线是知识怎样进入每次模型请求：

~~~text
Prompt + Session State + Tool Schema + Tool Result
       + Project Context + Retrieved Knowledge
                         ↓
                  Context Assembly
                         ↓
                    Model Request
~~~

第三条主线是课程怎样完成迁移：

~~~text
通用抽象
  ↓
DeepSeek Harness 锁定源码中的工程实体
  ↓
收益、代价和替代方案
  ↓
BuildPilot 是否采用及如何收窄
~~~

## 6. 课程四阶段结构

### Part I｜认识 Agent 世界（01-06）

建立术语、对象关系和系统边界。此阶段不读 DeepSeek Harness 源码，不实现 BuildPilot。

### Part II｜理解 Agent 怎样运行（07-19）

从结构化模型 I/O 进入 Agent Loop，再逐层补上 Tool、Context、Skill、Workflow、Evidence、Policy、Recovery、Cost、Trace、Eval 与 Multi-Agent，最后推导 Harness。

### Part III｜用 DeepSeek Harness 验证抽象（20-28）

先锁定版本与证据，再按前面学过的问题读取 Plugin、Profile、Context、Loop、Session、Tool Pipeline 和运行控制。源码是教材，不是课程主题本身。

### Part IV｜完成 BuildPilot Design v1（29-35）

先研究游戏研发问题空间，再用 Compile Diagnosis 与 Startup Performance 两个案例校准 Agent 价值，最终交付架构、数据合同、治理、Eval 和实施 Roadmap。此阶段仍不正式开发 Runtime。

### 6.1 几个热门主题的放置结论

- Prompt Engineering：并入 03 的 Prompt / Context 边界，不独立扩成技巧教程；23 再从 DSH 运行时回收。
- Context Engineering：作为 03、10、23、33 四次递进的纵向主线。
- AI Native：不在前半段独立成篇；01 只建立产品形态概念，35 再讨论未来 Host / 产品闭环是否真的 AI-native。
- Multi-Agent：后移到 18；只有单 Agent 的 Tool、Workflow、Context、Trace 和 Eval 成立后才讨论拆分。
- Token / Cost：16 讲通用成本模型，27 讲 DSH 的具体运行控制。
- Evidence / Hypothesis / Diagnosis：保留为 13 的独立领域合同，并在 30-35 持续回收。
- Agent 知识飞轮：完整治理留在 AI 赋能系列；04、33 只覆盖 Agent 的知识消费接口。

## 7. 完整课程依赖图

~~~text
01 Agent 世界
 ├─→ 02 Tool / Skill / Workflow
 ├─→ 03 Prompt / Context
 └─→ 06 Application / Runtime / Harness

03 → 04 RAG / KB / Session / Memory
02 + 04 → 05 通用 / 专用 / 非 Agent 边界
01-05 → 06 系统总图

06 → 07 结构化 Model I/O
07 → 08 Agent Loop
02 + 08 → 09 Tool Engineering
03 + 08 + 09 → 10 Context Lifecycle
02 + 10 → 11 Skill Engineering
05 + 08 → 12 Workflow Engineering
08-12 → 13 Evidence Contract
09 + 13 → 14 Permission / Approval / Sandbox
08 + 09 + 14 → 15 Retry / Cancellation / Recovery
10 + 15 → 16 Token / Cost / Budget
08 + 13-16 → 17 Trace / Eval
12 + 17 → 18 Multi-Agent
07-18 → 19 Harness

19 → 20 DSH 阅读地图
20 → 21 Plugin Kernel
21 → 22 Profile / Bundle / Provider
03 + 10 + 22 → 23 DSH Prompt / Context
08 + 22 → 24 DSH Loop
04 + 17 + 24 → 25 DSH Session Event
09 + 14 + 15 + 24 → 26 DSH Tool Pipeline
16 + 17 + 23-26 → 27 DSH 运行控制
11 + 12 + 18 + 21-27 → 28 DSH 扩展与取舍

01-19 + 28 → 29 BuildPilot 问题空间
13 + 17 + 29 → 30 Compile Golden Fixture
08 + 10 + 13 + 29 → 31 Startup Investigation
30 + 31 → 32 BuildPilot Architecture
04 + 09-12 + 32 → 33 Capability Design
14-17 + 32 → 34 Harness / Eval Design
29-34 → 35 BuildPilot Design v1
~~~

## 8. 完整文章目录

| Part | 篇 | 主问题 |
|---|---:|---|
| I | 01-06 | Agent 世界有哪些对象，它们的边界是什么 |
| II | 07-19 | Agent 怎样运行，为什么最终需要 Harness |
| III | 20-28 | 一个真实 Harness 怎样把抽象变成工程结构 |
| IV | 29-35 | 怎样独立设计游戏研发专用 Agent |

### Part I｜认识 Agent 世界

01. 从一次模型调用到 Agent：LLM、Copilot、Agentic、Runtime 与 Harness
02. Tool、Function Calling、MCP Tool、Skill 与 Workflow
03. Prompt、System Prompt、Context 与 Context Engineering
04. RAG、Knowledge Base、Session 与 Memory
05. 通用 Agent、专用 Agent，以及哪些问题根本不该 Agent 化
06. Agent 系统总图：Application、Host、Runtime 与 Harness

### Part II｜理解 Agent 怎样运行

07. 从自然语言到机器合同：Model Request 与 Structured Output
08. Agent Loop：Turn、Step、Observation、State 与 Stop Condition
09. Tool Engineering：Schema 之后还有一整条执行管线
10. Context Engineering：选择、装配、压缩与生命周期
11. Skill Engineering：把领域方法按需带进任务
12. Workflow Engineering：确定性骨架与模型决策点
13. Evidence Contract：Evidence、Hypothesis、Diagnosis 与 Result
14. Permission、Approval 与 Sandbox：把边界移出 Prompt
15. Retry、Timeout、Cancellation 与 Recovery
16. Token、推理成本、延迟与 Budget
17. Trace、Replay 与 Eval：怎样知道 Agent 为什么成功或失败
18. Single Agent、Agent as Tool、Handoff、Subagent 与 Multi-Agent
19. 为什么 Agent 最终需要 Harness

### Part III｜DeepSeek Harness 源码教材

20. 怎样把 DeepSeek Harness 当作教材，而不是 API 手册
21. Everything is a Plugin：插件内核解决了什么
22. Profile、Bundle、Provider 与 Capability Seam
23. System Prompt 与 Context Assembly
24. Turn、Step、Inbox 与 Agent Loop
25. Session Event、Replay、Resume 与 Fork
26. Tool Registry、Policy 与 Execution Pipeline
27. Cost、Compaction、Trace、Cancellation 与 Recovery
28. RAG、Skill、Workflow 与 Host 应该怎样映射：事实、扩展与取舍

### Part IV｜BuildPilot Design

29. 游戏研发生产管线里，什么问题值得 Agent 化
30. Case A：Unity Compile Diagnosis 作为 Golden Fixture
31. Case B：Startup Performance Diagnosis 作为多步调查
32. 从问题空间推导 BuildPilot Architecture
33. BuildPilot 的 Context、Knowledge、Tool、Skill 与 Workflow
34. BuildPilot 的 Policy、Session、Trace、Budget、Recovery 与 Eval
35. BuildPilot Design v1：毕业设计评审与未来实现 Roadmap

---

## 9. 每篇 Article Card

## Part I｜认识 Agent 世界

### 01｜从一次模型调用到 Agent：LLM、Copilot、Agentic、Runtime 与 Harness

#### 1. 本篇定位

基础总论篇。建立整门课的第一组对象边界，避免把任何调用大模型的产品都称为 Agent。

#### 2. 为什么现在学它

- 上一节：无，这是课程入口。
- 问题：读者可能会使用 Coding Agent，却没有统一标准判断“什么是 Agent”。
- 本节：先用目标、状态、行动、反馈和停止条件建立最小判定模型。

#### 3. 学完以后应该能回答什么

- 普通 Chat Completion 为什么通常不是 Agent？
- Copilot 与 Agent 的控制权差在哪里？
- Agentic 是形容词、行为特征，还是一种固定产品类型？
- Agent Runtime 与 Harness 分别承载什么？
- Claude Code 或 Codex 满足哪些 Agent 特征？

#### 4. 前置知识

基本的大模型 API 使用体验；不要求知道 Agent 框架。

#### 5. 核心概念

LLM、Model、Inference、AI Application、Feature、Copilot、Agentic、Agent、Agent Runtime、Harness、AI-native Loop。

#### 6. 核心心智模型

~~~text
Model Request
  → AI Feature
  → Copilot（人主导步骤）
  → Agent（系统循环选择步骤）
  → Harnessed Agent（受工程边界约束）
~~~

#### 7. 正文框架

1. 为什么“用了大模型”不是有效分类  
   1.1 对比问答、补全、建议和持续执行；用同一条 Unity 日志展示四种产品形态。  
   1.2 提出五个判定问题：目标、状态、行动、反馈、停止条件由谁负责。
2. Model、Application、Copilot 与 Agent  
   2.1 Model 只生成当前输出，不天然持有任务。  
   2.2 Copilot 把下一步选择留给人；Agent 把部分选择放进循环。  
   2.3 Agentic 只描述自主性程度，不保证存在统一架构。  
   2.4 AI Native 关注产品主循环、状态和反馈是否围绕 AI 重构，不等于增加聊天入口。
3. Runtime 与 Harness 为什么不是同义词  
   3.1 Runtime 负责请求、Tool 调度和状态推进。  
   3.2 Harness 负责能力组合、权限、预算、Trace、恢复和宿主接入。  
   3.3 用两层架构图说明可以有轻量 Runtime，也可以逐步长出 Harness。
4. 用真实产品做分类练习  
   4.1 只根据可观察行为分析 Claude Code、Codex 和普通 Chat API。  
   4.2 明确产品实现会变化，分类结论必须附观察日期。

#### 8. 贯穿案例

“给一份 Unity 编译错误日志”：一次回答、一次 Tool Call、连续调查三种形态对照。

#### 9. 与 DeepSeek Harness 的关系

20 会回收本篇：先判断 DSH 覆盖 Application、Runtime、Harness 中的哪些层，再进入源码；35 再讨论 BuildPilot 是否有理由走向 AI-native 产品闭环。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot 不能从“加一个聊天框”开始，而要先说明它承担了哪些循环决策。

#### 11. 需要的证据

概念资料、主流 SDK 官方文档、产品当前可观察行为。产品分类涉及版本时必须核验。

#### 12. 容易混淆的概念

LLM ≠ Agent；Agentic ≠ Multi-Agent；Agent Runtime ≠ Harness；AI Feature ≠ AI Native。

#### 13. 本篇明确不讲什么

不讲 Prompt 技巧、Tool Pipeline、具体框架 API 和 Multi-Agent。

#### 14. 学习检查

- 一个只把用户问题转发给模型的 Web 页是不是 Agent？为什么？
- 模型一次返回三个建议，但由人选择下一步，控制权在哪里？
- 一个循环执行 Tool 却没有停止条件的系统，能否称为可用 Agent？

### 02｜Tool、Function Calling、MCP Tool、Skill 与 Workflow

#### 1. 本篇定位

基础映射篇。建立“能力、协议、方法和路径”的边界，并明确 Skill 不是全行业统一的数据类型。

#### 2. 为什么现在学它

- 上一节：已经知道 Agent 需要行动能力和运行环境。
- 问题：工程讨论常把 Tool、MCP、Skill、Workflow 当成可互换名词。
- 本节：先给对象地图，后续 09、11、12 再分别深入工程化。

#### 3. 学完以后应该能回答什么

- Tool 与普通函数的差别从哪里开始？
- Function Calling 是 Tool 本身还是模型输出协议？
- MCP Tool 与 Harness 内部 ToolDefinition 是什么关系？
- Skill 为什么更接近可加载的方法包？
- Workflow 与 Agent 谁决定下一步？

#### 4. 前置知识

01 的 Agent 最小判定模型；基本函数和 API 概念。

#### 5. 核心概念

Function Calling、Tool Schema、Tool Call、Tool Result、MCP Tool、Skill、Instructions、Workflow、State Transition。

#### 6. 核心心智模型

~~~text
Tool = 能做什么
Skill = 这类事通常怎样做
Workflow = 按什么阶段推进
Agent = 此刻选择哪一步
Harness = 允许在什么边界内做
~~~

#### 7. 正文框架

1. 从普通函数到模型可调用能力  
   1.1 函数有编译期调用者，Tool 面对概率性调用者。  
   1.2 Schema、描述和错误语义为什么会改变模型行为。
2. Function Calling、MCP 与 ToolDefinition  
   2.1 Function Calling 约束模型如何表达调用意图。  
   2.2 MCP 规范化客户端与外部服务之间的能力发现和调用。  
   2.3 Harness 仍需在协议之上加入 Policy、执行和呈现元数据。
3. Skill 的生态差异  
   3.1 把 Skill 定义为“按需发现和加载的方法、规则与资产包”。  
   3.2 对比 Agent Skills、产品内 Skill 和团队自定义指令包；不宣称唯一标准。
4. Workflow 与 Agent  
   4.1 Workflow 固定阶段和转移；Agent处理阶段内部或边界上的判断。  
   4.2 用 Jenkins Build 与故障调查对比确定性路径和开放性决策。
5. 五对象映射练习  
   5.1 把 Unity 编译诊断拆成 Tool、Skill、Workflow、Agent、Harness。  
   5.2 找出把所有逻辑都塞进 Prompt 的反模式。

#### 8. 贯穿案例

parseUnityLog、Unity 编译诊断方法卡、诊断阶段状态机三者对照。

#### 9. 与 DeepSeek Harness 的关系

21、22、26、28 会检验 DSH 把哪些对象做成核心插件，哪些只能通过扩展映射。

#### 10. 与 BuildPilot 的关系

为未来 Tool 清单、Diagnostic Skill 与 Investigation Workflow 提供分类规则。

#### 11. 需要的证据

Function Calling 官方文档、MCP Tools Specification、Agent Skills Specification，以及至少两个产品实现对照。

#### 12. 容易混淆的概念

Function Calling ≠ Tool 执行；MCP ≠ Agent；Skill ≠ Tool；Workflow ≠ Agent Loop。

#### 13. 本篇明确不讲什么

不讲 Tool 安全管线、Skill 文件格式细节、BPM 和多 Agent 编排。

#### 14. 学习检查

- “每天三点执行 Unity Build”应该是 Scheduler、Workflow 还是 Agent？
- 一份故障排查清单应该做 Tool 还是 Skill？为什么？
- MCP Server 暴露了 deleteBuild，是否意味着 Agent 已获得删除权限？

### 03｜Prompt、System Prompt、Context 与 Context Engineering

#### 1. 本篇定位

基础原理篇。建立模型每次真正看到的输入结构，并把 Prompt Engineering 放回 Agent Engineering 的正确位置。

#### 2. 为什么现在学它

- 上一节：知道 Agent 有能力和方法，但还不知道模型依据什么做选择。
- 问题：很多方案把 Prompt 当成模型看到的全部内容。
- 本节：区分指令表达与完整请求装配。

#### 3. 学完以后应该能回答什么

- Prompt 与 Context 的边界是什么？
- System Prompt 为什么不等于全部长期规则？
- Tool Schema 和 Tool Result 为什么也属于 Context？
- Context Engineering 比 Prompt Engineering 多解决了哪些生命周期问题？

#### 4. 前置知识

01 的 Model Request；02 的 Tool Schema 与 Tool Result。

#### 5. 核心概念

System Message、User Message、Developer Instruction、Prompt Template、Context Window、Context Assembly、Scope、Ordering、Lifecycle。

#### 6. 核心心智模型

~~~text
Prompt：怎样表达目标与约束
Context：本次请求实际可见的一切
Context Engineering：选择、排序、作用域、更新、压缩和追踪
~~~

#### 7. 正文框架

1. 模型请求里到底有什么  
   1.1 展开消息、历史、Tool Schema、Tool Result、状态和外部知识。  
   1.2 用请求快照证明“Prompt 只是 Context 的一部分”。
2. Prompt Engineering 的职责  
   2.1 目标、约束、示例、输出合同和失败语义。  
   2.2 为什么更精巧的措辞修不好错误事实和过期上下文。
3. Context Engineering 的六个动作  
   3.1 Select、Order、Scope、Refresh、Compress、Trace。  
   3.2 静态前缀与动态上下文分别承担什么。
4. 常见失败模式  
   4.1 全量塞入、重复规则、冲突来源、旧状态和大 Tool Result。  
   4.2 为后续 10 的生命周期管理留下问题。

#### 8. 贯穿案例

同一份 Unity 日志分别使用“长 System Prompt”和“阶段化 Context View”进行概念对照。

#### 9. 与 DeepSeek Harness 的关系

23 会回看 System Prompt Assembly、动态 Context、历史和 Tool Schema 如何进入真实请求。

#### 10. 与 BuildPilot 的关系

决定 Project Context、Runtime Context、Investigation Context 与 Historical Context 不能混成一个长提示词。

#### 11. 需要的证据

模型 API 消息规范、Context Window 官方说明、一次真实请求 Trace。后续性能结论必须实验。

#### 12. 容易混淆的概念

Prompt ≠ Context；System Prompt ≠ Memory；长上下文 ≠ 有效上下文；Context Engineering ≠ RAG。

#### 13. 本篇明确不讲什么

不展开 RAG 算法、Compaction 实现、缓存价格和 Prompt 技巧清单。

#### 14. 学习检查

- Tool 返回的 20 万字符日志属于 Prompt 问题还是 Context 问题？
- 项目版本从 2022.3 升级后，旧规则继续注入，应该改措辞还是改来源生命周期？
- 为什么“全部文件都放进 Context”不等于模型获得了完整理解？

### 04｜RAG、Knowledge Base、Session 与 Memory

#### 1. 本篇定位

基础映射篇。建立四种长期或外部信息机制的写入、读取、权威性与过期边界。

#### 2. 为什么现在学它

- 上一节：知道 Context 需要选择信息。
- 问题：信息不可能全在当前请求里，但“保存”和“检索”常被统称为记忆。
- 本节：按来源、生命周期和权威性拆开四类对象。

#### 3. 学完以后应该能回答什么

- Session 保存什么，结束后是否仍然有效？
- Memory 为什么不能自动成为团队事实？
- Knowledge Base 与 RAG 是存储和检索的哪两层？
- 当前源码事实与历史事故知识冲突时相信谁？
- RAG 为什么只是 Context Engineering 的一种获取机制？

#### 4. 前置知识

03 的 Context Assembly；02 的 Tool。

#### 5. 核心概念

Session State、Transcript、Event、Memory、Knowledge Base、Retriever、Chunk、Rerank、Citation、Freshness、Authority。

#### 6. 核心心智模型

~~~text
Session = 本次运行事实
Memory = 跨运行保留的经验或偏好
Knowledge Base = 持久知识来源
RAG = 针对当前问题检索并注入
Source / CI / Runtime = 当前事实权威
~~~

#### 7. 正文框架

1. 为什么“记住”至少有四种含义  
   1.1 用同一故障的对话历史、经验规则、事故文档和检索结果做分类。  
   1.2 引入写入时机、读取时机、权威性和有效期四维表。
2. Session 与 Memory  
   2.1 Session 保存 Run 内的事件和状态。  
   2.2 Memory 需要筛选、作用域和淘汰，不能把模型总结直接升格为事实。
3. Knowledge Base 与 RAG  
   3.1 KB 是来源集合，RAG 是 Query 到 Cite 的运行链。  
   3.2 Keyword、Vector、Hybrid 只作为检索策略概览。
4. 冲突与新鲜度  
   4.1 Source、CI、构建产物、运行指标与 Wiki 的权威层次。  
   4.2 通过 Context Receipt 记录来源、冲突、未知和验证路径。
5. 与团队知识飞轮的边界  
   5.1 本篇讲消费接口，不重讲知识生产、准入、记账和代谢。  
   5.2 交叉引用 AI 赋能 12 候选知识飞轮篇。

#### 8. 贯穿案例

检索一次 Jenkins 历史故障，同时读取当前 Job 配置并报告冲突。

#### 9. 与 DeepSeek Harness 的关系

25 研究 Session Event；28 研究 RAG / Memory 如何借扩展点接入而不冒充 DSH 一级能力。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot 的 Session Store、Incident KB、Project Facts 与 Retrieval Result 分层。

#### 11. 需要的证据

RAG 原始论文或权威综述、Session / Memory 框架文档、检索 Fixture。涉及知识飞轮时引用现有 AI 赋能系列。

#### 12. 容易混淆的概念

RAG ≠ Knowledge Base；Session ≠ Memory；Memory ≠ 权威事实；召回结果 ≠ 已验证证据。

#### 13. 本篇明确不讲什么

不做向量数据库选型，不讲企业知识治理全体系，不承诺自动沉淀正确。

#### 14. 学习检查

- 模型上次误判后生成的总结应该直接写进 Knowledge Base 吗？
- 当前 Job 配置与三个月前复盘冲突时，诊断应怎样表达？
- 只建立向量库但从不把结果注入请求，算不算完成 RAG？

### 05｜通用 Agent、专用 Agent，以及哪些问题根本不该 Agent 化

#### 1. 本篇定位

基础判断篇。建立任务不确定性与系统确定性之间的选择框架。

#### 2. 为什么现在学它

- 上一节：已经认识 Agent 所依赖的能力和信息。
- 问题：如果不先判断问题类型，课程很容易把所有工具自动化都包装成 Agent。
- 本节：在进入运行机制前，先建立“什么时候不用 Agent”的纪律。

#### 3. 学完以后应该能回答什么

- 专用 Agent 是否只是更长的 System Prompt？
- 哪些维度让 Agent 真正专用化？
- Script、Rule、Workflow 和 Agent 应怎样选择？
- 为什么确定性工具仍然是 Agent 的地基？
- 通用 Agent 与专用 Agent怎样做公平 Eval？

#### 4. 前置知识

01-04 的 Agent、Tool、Workflow、Context 和知识边界。

#### 5. 核心概念

Task Space、Uncertainty、Determinism、Tool Surface、Data Contract、Policy、Evaluation Distribution、Specialization。

#### 6. 核心心智模型

~~~text
规则明确 + 输入稳定 → Script / Rule
阶段固定 + 局部判断 → Workflow + Decision Point
需要调查 + 上下文理解 + 下一步选择 → Agent
任务域收窄 + 工具/权限/合同/Eval 收窄 → 专用 Agent
~~~

#### 7. 正文框架

1. 不要从“我们想做 Agent”开始  
   1.1 从任务的不确定来源出发：缺信息、需解释、需探索还是仅需执行。  
   1.2 给出错误 Agent 化的三个案例。
2. 四种自动化形态  
   2.1 Script / Rule、Scheduler、Workflow、Agent 的控制权对比。  
   2.2 用 Unity Build、资产检查和故障调查映射。
3. 专用化的五个维度  
   3.1 任务空间、工具面、上下文源、输出合同、Policy / Eval。  
   3.2 说明换 Prompt 只是其中很小一部分。
4. 通用与专用的收益和代价  
   4.1 通用 Agent 覆盖广但测试空间大。  
   4.2 专用 Agent 更可评测，但维护领域边界有成本。
5. 为 BuildPilot 建立候选标准  
   5.1 编译错误是教学 Fixture，不自动证明 Agent 价值。  
   5.2 启动性能调查更接近多源证据和下一步决策问题。

#### 8. 贯穿案例

资产命名检查、每日构建、编译诊断、启动性能回归四类任务对照。

#### 9. 与 DeepSeek Harness 的关系

22 会观察 Profile / Bundle 如何收窄能力集；28 判断通用 Harness 设计哪些适合专用 Agent。

#### 10. 与 BuildPilot 的关系

这是 29 的前置，防止 BuildPilot 替代 Jenkins、脚本和已有检查器。

#### 11. 需要的证据

真实游戏研发流程清单、现有确定性工具能力、至少四个任务分类案例。

#### 12. 容易混淆的概念

专用 Agent ≠ 小模型；Workflow ≠ Agent；自动化 ≠ Agent 化；复杂任务 ≠ 必须 Multi-Agent。

#### 13. 本篇明确不讲什么

不设计 BuildPilot 架构，不做框架横评，不给自动化率目标。

#### 14. 学习检查

- 已有稳定资产检查器时，再让模型判断同一规则有什么代价？
- “分析最近十次构建失败并决定下一条调查路径”为什么比“运行构建”更适合 Agent？
- 一个只允许三只 Tool 的 Agent是否仍然可以有高自主性？

### 06｜Agent 系统总图：Application、Host、Runtime 与 Harness

#### 1. 本篇定位

Part I 收束篇。把前五篇对象装回一张系统图，为 Part II 的运行机制建立容器。

#### 2. 为什么现在学它

- 上一节：已经知道对象和任务边界。
- 问题：这些对象如果没有分层，后续 Tool、Context、Policy 会被随意塞进 Agent 类。
- 本节：建立 Application / Host、Runtime、Harness、External System 四层。

#### 3. 学完以后应该能回答什么

- Host 与 Agent Runtime 的状态分别是什么？
- Harness 是 Runtime 的一部分还是外层控制面？
- Tool、Skill、Workflow、Session 应挂在哪一层？
- 同一个 Runtime 怎样服务 CLI、Web、IDE 或 Jenkins？
- 为什么 Harness 可以逐步长出，而不必一次做全？

#### 4. 前置知识

01-05 全部 Part I 概念。

#### 5. 核心概念

Application、Host、Runtime、Control Plane、Capability、Provider、Policy、External System、Deployment Boundary。

#### 6. 核心心智模型

~~~text
Host：接收用户或系统事件
Harness：组合能力并施加边界
Runtime：执行 Agent Loop
Provider / Tool：连接模型与外部系统
~~~

#### 7. 正文框架

1. 为什么需要系统层次  
   1.1 展示“一个 AgentService 类包办所有事情”的膨胀路径。  
   1.2 用控制权、状态归属和替换频率切层。
2. Host / Application  
   2.1 CLI、Web、Unity Editor、Jenkins 后台的输入与交互差异。  
   2.2 Host 不应复制 Runtime 核心逻辑。
3. Agent Runtime  
   3.1 Model Adapter、Loop、Tool Dispatch、Structured Output、State。  
   3.2 Runtime 的最小闭环和非职责。
4. Harness  
   4.1 配置组合、Policy、Permission、Budget、Trace、Recovery。  
   4.2 说明 Harness 是工程责任集合，不要求所有框架使用同一类名。
5. External Capability  
   5.1 Tool、Retriever、File、Jenkins、Metrics Provider。  
   5.2 用依赖反转保持专用化而不锁死宿主。
6. Part II 路线预告  
   6.1 从 Model I/O 开始逐层构造 Runtime。  
   6.2 最终在 19 回答这些机制为何聚合为 Harness。

#### 8. 贯穿案例

同一个只读诊断核心分别从 CLI 和 Jenkins 事件启动。

#### 9. 与 DeepSeek Harness 的关系

20-22 会用锁定源码检验 DSH 对 Application、Profile、Plugin、Provider 的实际切法。

#### 10. 与 BuildPilot 的关系

为 32 的架构推导提供初始分层，但不预先强迫 BuildPilot照搬 DSH。

#### 11. 需要的证据

架构概念资料、两个 Agent SDK / 产品的模块图、当前仓库已有 Harness 文章作为对照。

#### 12. 容易混淆的概念

Host ≠ Harness；Runtime ≠ 产品 UI；Provider ≠ Tool；架构分层 ≠ 必须拆成微服务。

#### 13. 本篇明确不讲什么

不讲具体 Loop、Plugin 实现、部署拓扑和 BuildPilot 类图。

#### 14. 学习检查

- Unity Editor 窗口关闭后，Session 是否必须消失？这取决于哪一层？
- CLI 与 Web 都复制 Tool Policy 会产生什么问题？
- 只有单进程实现时，是否仍然值得区分 Host、Runtime 与 Harness？

## Part II｜理解 Agent 怎样运行

### 07｜从自然语言到机器合同：Model Request 与 Structured Output

#### 1. 本篇定位

运行原理入口篇。先把单次模型调用变成可解析、可失败、可验证的工程接口。

#### 2. 为什么现在学它

- 上一节：已经有系统分层，但 Runtime 内部还是黑盒。
- 问题：如果模型输出只能靠人读，后续 Loop、Tool 和 Eval 都无法稳定建立。
- 本节：从 Message 和 Schema 建立 Agent Runtime 的最小 I/O。

#### 3. 学完以后应该能回答什么

- Model Request 除 Prompt 外还包含什么？
- Structured Output 与 Tool Call 分别表达什么意图？
- Schema 为什么会影响模型行为和失败类型？
- Parse Success 为什么不等于语义正确？

#### 4. 前置知识

03 的 Context；06 的 Runtime。

#### 5. 核心概念

Message、Role、Request、Response、Structured Output、JSON Schema、Validation、Refusal、Finish Reason、Adapter。

#### 6. 核心心智模型

~~~text
Typed Input → Model Request → Candidate Output
→ Parse → Validate → Accept / Repair / Fail
~~~

#### 7. 正文框架

1. 单次调用的工程边界  
   1.1 请求参数、消息、工具定义和模型选项。  
   1.2 响应内容、停止原因、Usage 和错误。
2. 为什么要结构化  
   2.1 自然语言可读但难以驱动状态机。  
   2.2 JSON Schema / typed DTO 带来的可校验边界。
3. Structured Output 与 Tool Call  
   3.1 前者表达业务结果，后者表达行动意图。  
   3.2 两者可以组合，但不能把 Tool 执行结果伪造成模型结论。
4. 失败语义  
   4.1 解析失败、Schema 失败、业务约束失败、拒绝和截断。  
   4.2 哪些可以重试，哪些必须停止或交人。
5. Adapter 边界  
   5.1 Provider 差异封装在 Adapter。  
   5.2 领域数据合同不能被某一家 API 类型绑死。

#### 8. 贯穿案例

把“请分析编译错误”改成 DiagnosisCandidate DTO，但暂时不运行多 Step。

#### 9. 与 DeepSeek Harness 的关系

24、26 会检验 Request、Step、Tool Call 和 Result 如何进入生命周期。

#### 10. 与 BuildPilot 的关系

为 Evidence / Hypothesis / Diagnosis DTO 和未来 Model Adapter 定义边界。

#### 11. 需要的证据

至少两个模型 API 的 Structured Output / Tool Calling 官方文档；固定 Schema Fixture。

#### 12. 容易混淆的概念

JSON 合法 ≠ 结论正确；Structured Output ≠ Tool；Response ≠ VerificationResult。

#### 13. 本篇明确不讲什么

不讲 Agent Loop、Prompt 优化、Provider 价格和完整 Tool 执行。

#### 14. 学习检查

- 模型返回合法 DiagnosisResult，但引用了不存在的文件，哪层应拒绝？
- Tool Call 参数符合 Schema，是否可以直接执行删除？
- 为什么业务 DTO 不应直接复用某个模型 SDK 的 Response 类型？

### 08｜Agent Loop：Turn、Step、Observation、State 与 Stop Condition

#### 1. 本篇定位

核心原理篇。解释 Agent 怎样从一次调用变成持续执行任务的状态推进器。

#### 2. 为什么现在学它

- 上一节：已经能进行一次结构化模型调用。
- 问题：真实任务需要行动、观察和下一步选择，单次调用无法闭环。
- 本节：建立 Turn / Step / State 与 Continue / Stop。

#### 3. 学完以后应该能回答什么

- Turn 与 Step 为什么不能混用？
- Tool Result 何时变成 Observation？
- 谁更新 State，谁决定继续？
- Stop Condition 应该由模型、Runtime 还是 Policy 决定？
- 一个 Run 如何避免无限循环？

#### 4. 前置知识

07 Model I/O；02 Tool 基础。

#### 5. 核心概念

Run、Turn、Step、Agent Loop、Action、Observation、State、Continuation、Stop Condition、Wakeup。

#### 6. 核心心智模型

~~~text
Goal → Assemble → Decide → Act → Observe → Update State
                 ↑                         ↓
                 └──── Continue / Stop ───┘
~~~

#### 7. 正文框架

1. 为什么一次调用不够  
   1.1 缺信息、需调用 Tool、需根据结果改变计划。  
   1.2 对比 Chain 与真正可观察的 Loop。
2. Run、Turn 与 Step  
   2.1 Run 是任务实例，Turn 是外部驱动周期，Step 是一次模型决策。  
   2.2 给出无 Tool、单 Tool、多 Tool 三条时序。
3. State 与 Observation  
   3.1 原始 Tool Result、规范化 Observation 和领域状态的区别。  
   3.2 State 更新必须可追踪，不能只存在模型隐含推理中。
4. Continue / Stop  
   4.1 模型完成、目标满足、预算耗尽、Policy 拒绝、取消和错误。  
   4.2 Runtime 与 Harness 都有停止权。
5. 最小伪代码  
   5.1 只展示 loop、dispatch、append、budget 和 stop。  
   5.2 标出后续章节要替换的临时实现。

#### 8. 贯穿案例

读取 Unity 日志后决定是否读相关文件，再形成诊断。

#### 9. 与 DeepSeek Harness 的关系

24 专门映射 Inbox、Turn、Step 和 Agent Loop。

#### 10. 与 BuildPilot 的关系

决定 Compile Case 是两三步教学轨迹，而 Startup Case 需要多轮假设驱动调查。

#### 11. 需要的证据

Agent SDK 生命周期文档、最小 Loop 伪代码、四条可重放 Trace Fixture。

#### 12. 容易混淆的概念

Turn ≠ Step；Tool Result ≠ 已解释 Observation；Stop ≠ 成功；Chain ≠ Agent Loop。

#### 13. 本篇明确不讲什么

不展开 Tool Policy、Context Compaction、Retry 和 Multi-Agent。

#### 14. 学习检查

- 一个 Turn 可以没有 Model Step 吗？什么情况下？
- 模型说“完成了”，但没有满足输出合同，谁应该阻止 Stop？
- Tool 返回空结果后，系统应重试、换 Tool 还是停止？还缺哪些信息？

### 09｜Tool Engineering：Schema 之后还有一整条执行管线

#### 1. 本篇定位

核心原理篇。把 Tool 从“函数包装”提升为面对概率性调用者的受控执行协议。

#### 2. 为什么现在学它

- 上一节：Agent Loop 已经能产生 Tool Call。
- 问题：模型生成调用意图不等于调用安全、合法或值得执行。
- 本节：建立 Schema → Policy → Execute → Validate → Render → Record。

#### 3. 学完以后应该能回答什么

- Tool Schema 为什么只是入口？
- 模型可见字段和 Host-only Metadata 为什么要分开？
- 只读、有副作用和破坏性 Tool 的管线有什么不同？
- Tool Result 为什么要裁剪、溢出和保留来源？
- 并发、超时和取消应放在哪一层？

#### 4. 前置知识

02 Tool 基础；07 Schema；08 Agent Loop。

#### 5. 核心概念

ToolDefinition、Canonical Arguments、Metadata、Policy Hook、Idempotency、Timeout、Cancellation、Result Normalization、Presentation、Spill。

#### 6. 核心心智模型

~~~text
Discover → Select → Validate Input → Policy
→ Execute → Validate Output → Normalize / Spill
→ Present to Model / UI → Persist Trace
~~~

#### 7. 正文框架

1. 概率性调用者改变了什么  
   1.1 参数可能合法但危险，调用时机可能错误。  
   1.2 Tool 描述同时是模型控制面和文档。
2. Tool Definition 的两张脸  
   2.1 模型可见 Schema、名称和描述。  
   2.2 Host-only 风险级别、权限、超时、审计标签。
3. 执行前管线  
   3.1 规范化参数、路径解析、权限检查、Approval。  
   3.2 多 Policy 冲突必须 fail closed。
4. 执行与失败  
   4.1 幂等、重入、并发、取消、超时和外部系统错误。  
   4.2 错误应结构化返回还是终止 Run。
5. 执行后管线  
   5.1 输出校验、敏感信息处理、裁剪和大结果溢出。  
   5.2 Model Content、UI Presentation 与持久 Trace 分离。
6. 三只只读 Tool 的设计练习  
   6.1 parseUnityLog、readFile、searchLiteral。  
   6.2 每只 Tool 的风险、输入合同和证据来源。

#### 8. 贯穿案例

readFile 的路径白名单、junction 越界、大文件和取消测试。

#### 9. 与 DeepSeek Harness 的关系

26 对照 Tool Registry 与 Execution Pipeline 的锁定源码事实。

#### 10. 与 BuildPilot 的关系

定义 BuildPilot Tool 不是任意 C# 方法，而是经过 Policy 和 Evidence 归一化的能力。

#### 11. 需要的证据

Function Calling / MCP 规范、Tool Pipeline 源码 evidence card、坏参数与越权 Fixture。

#### 12. 容易混淆的概念

Tool Call ≠ Tool 执行；Schema Valid ≠ Policy Allowed；Tool Result ≠ Evidence；UI 输出 ≠ Model 输入。

#### 13. 本篇明确不讲什么

不讲 Shell 全能力 Tool，不实现写文件，不展开 MCP Transport。

#### 14. 学习检查

- readFile 参数在白名单内，但路径经过 junction 指向外部，应该在哪一步拒绝？
- 两个 Tool 可以并发，是否意味着应该并发？
- 完整日志太大时，只截前 4K 字符会损失什么？怎样保留可追溯性？

### 10｜Context Engineering：选择、装配、压缩与生命周期

#### 1. 本篇定位

Part II 主线篇。研究 Agent Loop 多 Step 运行后，Context 怎样保持相关、可重建并受预算约束。

#### 2. 为什么现在学它

- 上一节：Tool 不断产生新的结果和 Observation。
- 问题：历史、Schema、状态和证据会持续增长，不能无限放入下一次请求。
- 本节：把 03 的基础概念扩展成运行时生命周期。

#### 3. 学完以后应该能回答什么

- 每个 Step 的 Context 应由哪些来源装配？
- Stable Prefix 与 Dynamic Context 为什么要分开？
- Compaction 应压缩什么，不能丢什么？
- Tool Result 何时应留摘要、引用或外部 Spill？
- 怎样从 Trace 重建模型当时看到的内容？

#### 4. 前置知识

03 Prompt / Context；08 Loop；09 Tool Result。

#### 5. 核心概念

Context Contributor、Priority、Scope、Stable Prefix、Dynamic Context、Snapshot、Compaction、Spill、Rehydration、Context Receipt。

#### 6. 核心心智模型

~~~text
Sources → Select → Order → Fit Budget → Snapshot
→ Model → New Observation → Refresh / Compact → Next Step
~~~

#### 7. 正文框架

1. Context 是每个 Step 的构建产物  
   1.1 不把 Mutable Message List 当成唯一真相。  
   1.2 列出 Prompt、State、History、Tool、Knowledge、Environment 六类来源。
2. 选择与作用域  
   2.1 Run、Agent、Project、User、Global 的优先级。  
   2.2 冲突、重复和过期来源的处理。
3. 排序与稳定前缀  
   3.1 指令、能力说明、动态事实和历史的顺序。  
   3.2 Cache 友好设计不能牺牲事实新鲜度。
4. Compaction 与 Spill  
   4.1 摘要历史、保留关键 Evidence ID、外置大结果。  
   4.2 Compaction 后重新注入不变量和未完成状态。
5. 可重建性  
   5.1 Context Snapshot / Receipt 记录来源和变换。  
   5.2 为什么只保存最终 Prompt 不足以解释来源冲突。
6. A/B 实验设计  
   6.1 全量、简单截断、阶段化 Context 三组。  
   6.2 同时比较准确率、引用正确率、Token 和延迟。

#### 8. 贯穿案例

Startup 调查每一步只加载当前异常阶段的指标、变更和历史事故。

#### 9. 与 DeepSeek Harness 的关系

23、27 分别研究 Context Assembly 与 Compaction / Cost 控制。

#### 10. 与 BuildPilot 的关系

直接决定四类 Context View 和 Context Receipt 合同。

#### 11. 需要的证据

模型 Context 官方资料、锁定请求 Trace、三组 A/B Fixture。性能结论必须实验后写。

#### 12. 容易混淆的概念

Context ≠ Transcript；Compaction ≠ 随意总结；Cache 命中 ≠ Context 正确；摘要 ≠ Evidence。

#### 13. 本篇明确不讲什么

不深入向量检索算法，不讨论服务端 KV Cache 实现，不声称长上下文必然更差。

#### 14. 学习检查

- Compaction 后删掉“尚未验证”的标记会造成什么语义错误？
- 一条稳定规则与当前源码事实冲突，排序能否解决权威性问题？
- 只保存最终请求文本，为什么仍可能无法审计 Context 来源？

### 11｜Skill Engineering：把领域方法按需带进任务

#### 1. 本篇定位

核心原理篇。把 Skill 定义为生态相关但可抽象比较的方法包，并建立发现、触发、加载和维护生命周期。

#### 2. 为什么现在学它

- 上一节：Context 不能永久携带所有领域方法。
- 问题：全局 Prompt 会 Bloat，而 Tool 又不能表达完整排查方法。
- 本节：研究怎样按任务加载“如何做”的知识。

#### 3. 学完以后应该能回答什么

- Skill 与 Prompt、Tool、Workflow、Knowledge Base 有什么差别？
- Discovery Metadata 与完整 Instructions 为什么分层？
- Skill 触发错误会怎样污染 Context？
- Skill 怎样版本化、验证、过期和瘦身？

#### 4. 前置知识

02 对象地图；04 Knowledge；10 Context Lifecycle。

#### 5. 核心概念

Discovery、Trigger、Progressive Disclosure、Instructions、References、Scripts、Assets、Version、Validation、Bloat、Drift。

#### 6. 核心心智模型

~~~text
轻量 Metadata 常驻
  → 判断相关性
  → 加载 Instructions
  → 按需读取 References / Scripts / Assets
  → 记录效果并维护
~~~

#### 7. 正文框架

1. Skill 解决的不是“再写一段 Prompt”  
   1.1 领域方法的复用、发现和按需加载。  
   1.2 不同生态没有完全统一的 Skill Runtime。
2. Skill 的最小结构  
   2.1 Metadata、Instructions、References、Scripts、Assets。  
   2.2 哪些内容应变 Tool、Workflow 或 KB。
3. 触发与渐进加载  
   3.1 显式调用、语义匹配和规则路由。  
   3.2 误触发、漏触发和多个 Skill 冲突。
4. 生命周期  
   4.1 版本、Owner、适用范围、测试和弃用。  
   4.2 自动沉淀只生成 Candidate，不能绕过 review。
5. Skill Eval  
   5.1 未加载、正确加载、错误加载三组对照。  
   5.2 指标包括步骤选择、规则遵守、Token 与失败模式。

#### 8. 贯穿案例

Unity C# 编译失败诊断 Skill，只包含排查方法，不包含 readFile 能力。

#### 9. 与 DeepSeek Harness 的关系

28 判断 DSH 是否存在一级 Skill，以及怎样通过 Plugin / Context / Tool 组合映射。

#### 10. 与 BuildPilot 的关系

决定 Diagnostic Skill 与 Startup Investigation Skill 的粒度、触发和版本边界。

#### 11. 需要的证据

Agent Skills Specification、具体产品 Skill 文档、三组触发 Eval。

#### 12. 容易混淆的概念

Skill ≠ Tool；Skill ≠ Memory；Skill ≠ Workflow；自动生成 Skill ≠ 自动获得真知识。

#### 13. 本篇明确不讲什么

不把任何单一 Skill 规范写成行业唯一标准，不讲 RL 自进化。

#### 14. 学习检查

- “读取文件”为什么不是 Skill？
- 一条仅适用于 Unity 2022.3 的 Skill 应怎样避免污染 Unity 6 任务？
- Skill 内包含十个阶段和恢复逻辑时，是否已经更像 Workflow？

### 12｜Workflow Engineering：确定性骨架与模型决策点

#### 1. 本篇定位

核心原理篇。说明生产 Agent 为什么通常不是完全自由 Loop，而是确定性流程与开放决策的组合。

#### 2. 为什么现在学它

- 上一节：Agent 已有 Tool 和领域方法。
- 问题：把所有阶段都交给模型临场规划，会放大遗漏、漂移和不可恢复性。
- 本节：固定必须发生的阶段，只把不确定选择留给 Agent。

#### 3. 学完以后应该能回答什么

- Workflow 与 Agent Loop 的控制权差别是什么？
- 哪些步骤应该确定性执行？
- Decision Point、Human Gate 和 Stop Point 怎样设计？
- Retry、Compensation 和 Recovery 为什么属于流程语义？

#### 4. 前置知识

05 自动化边界；08 Agent Loop；11 Skill。

#### 5. 核心概念

State Machine、Stage、Transition、Invariant、Decision Point、Gate、Compensation、Resume、Workflow Tool。

#### 6. 核心心智模型

~~~text
Deterministic Stage
  → Agent Decision Point
  → Deterministic Validation
  → Human Gate / Next Stage / Stop
~~~

#### 7. 正文框架

1. 完全自由规划为什么难生产化  
   1.1 漏步骤、不可审计、重复调用和恢复困难。  
   1.2 确定性不等于没有智能。
2. Workflow 的最小对象  
   2.1 Stage、State、Transition、Invariant、Terminal State。  
   2.2 Agent 可以在阶段内选择 Tool，也可以建议转移。
3. 决策点设计  
   3.1 信息是否充分、下一证据源、是否需要人批准。  
   3.2 不把审批写成模型礼貌询问。
4. 失败与恢复  
   4.1 Retry、Compensation、Checkpoint、Resume。  
   4.2 同步验证与异步 Jenkins / Unity 验证的区别。
5. 组合模式  
   5.1 Workflow 调 Agent、Agent 调 Workflow Tool、Code Orchestration。  
   5.2 选择依据是控制权，不是框架流行度。

#### 8. 贯穿案例

Intake → Evidence → Diagnosis → Review → Verification Proposal；其中调查步骤由 Agent 决定。

#### 9. 与 DeepSeek Harness 的关系

28 研究 Workflow 是否为 DSH 一级对象，以及用 Tool / Plugin / Host State Machine 接入的替代方案。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot 不替代 Jenkins Pipeline，而是在调查和决策点介入。

#### 11. 需要的证据

状态机资料、至少两种编排模式示例、失败恢复时序图。

#### 12. 容易混淆的概念

Workflow ≠ Agent Loop；Stage ≠ Step；Retry ≠ Restart；Human Gate ≠ Chat 确认句。

#### 13. 本篇明确不讲什么

不做企业 BPM 教程，不实现调度平台，不引入 Multi-Agent。

#### 14. 学习检查

- Unity Build 的固定阶段是否应该由模型逐步决定？
- Agent 建议进入发布阶段，谁验证前置条件？
- 异步验证结果第二天返回，Workflow 需要保存哪些状态才能 Resume？

### 13｜Evidence Contract：Evidence、Hypothesis、Diagnosis 与 Result

#### 1. 本篇定位

领域数据合同篇。为可审计诊断建立事实、推断、结论、行动和验证的分离。

#### 2. 为什么现在学它

- 上一节：Agent 已能在 Workflow 中调查。
- 问题：如果所有内容都混在自然语言回答里，无法判断结论来自哪里、是否已验证。
- 本节：建立 BuildPilot 最重要且可跨领域复用的数据合同。

#### 3. 学完以后应该能回答什么

- Tool Result 何时可以成为 Evidence？
- Hypothesis 必须引用什么？
- Diagnosis 的 confidence 应表达什么？
- ProposedAction 与 VerificationResult 为什么不能合并？
- 反证和 unverifiedScope 怎样保存？

#### 4. 前置知识

07 Structured Output；08 State；09 Tool Result；12 Workflow。

#### 5. 核心概念

Evidence ID、Source、Observation、Hypothesis、Counter-evidence、Diagnosis、Confidence、ProposedAction、VerificationResult、Unverified Scope。

#### 6. 核心心智模型

~~~text
Source → Evidence
Evidence ↔ Hypothesis / Counter-evidence
→ Diagnosis
→ ProposedAction
→ VerificationResult
~~~

#### 7. 正文框架

1. 自然语言诊断为什么不够  
   1.1 事实和推测混写、引用断裂、验证状态漂移。  
   1.2 用一段“看起来合理”的错误诊断拆解问题。
2. Evidence  
   2.1 来源、采集时间、作用域、内容摘要和原始引用。  
   2.2 不可变与派生 Observation 的边界。
3. Hypothesis 与 Diagnosis  
   3.1 Hypothesis 引用 Evidence ID 并允许反证。  
   3.2 Diagnosis 是当前最佳解释，不等于已修复。
4. Action 与 Verification  
   4.1 建议、已批准、已执行、已验证四种状态。  
   4.2 Offline Fixture、静态检查、Unity BatchMode、Jenkins 和设备是不同证据渠道。
5. 数据合同草图  
   5.1 给出字段、状态和不变量。  
   5.2 设计 Schema 负例：无证据结论、伪造验证、过度 confidence。

#### 8. 贯穿案例

缺失 asmdef reference 的编译失败：日志、文件内容、引用关系、假设和未运行 Unity 的明确标记。

#### 9. 与 DeepSeek Harness 的关系

25、27 研究 Session Event 与 Trace 怎样保存这些领域事件，但不假设 DSH 内置本合同。

#### 10. 与 BuildPilot 的关系

这是 BuildPilot Design v1 的核心领域模型。

#### 11. 需要的证据

真实 Fixture、Schema、Trace、反例测试。必须先做 evidence card 再写正文。

#### 12. 容易混淆的概念

Tool Result ≠ Evidence；Evidence ≠ Root Cause；Diagnosis ≠ Verification；confidence ≠ 准确率。

#### 13. 本篇明确不讲什么

不解释模型隐藏推理，不用 Chain-of-Thought 充当证据，不自动应用 Patch。

#### 14. 学习检查

- 模型说“很可能缺引用”，但没读 asmdef，这是什么对象？
- Fixture 内参考答案能否作为 Agent 独立发现的 Evidence？
- Unity 没有实际运行时，结果字段应该怎样诚实表达？

### 14｜Permission、Approval 与 Sandbox：把边界移出 Prompt

#### 1. 本篇定位

生产控制篇。把“请不要越界”从自然语言约束升级为运行时不可绕过的 Policy。

#### 2. 为什么现在学它

- 上一节：诊断已能提出行动。
- 问题：模型遵守 Prompt 不是权限证明，Tool 参数合法也不代表有权执行。
- 本节：建立授权、审批和隔离执行三层边界。

#### 3. 学完以后应该能回答什么

- Permission、Approval 与 Sandbox 分别控制什么？
- Tool 白名单和路径白名单为什么都需要？
- 哪些动作可以自动，哪些必须 Ask？
- 多个 Policy 冲突时怎样合并？
- 凭证应该对模型可见吗？

#### 4. 前置知识

09 Tool Pipeline；13 Action / Verification。

#### 5. 核心概念

Allow、Deny、Ask、Least Privilege、Capability Token、Path Allowlist、Tool Allowlist、Approval Record、Sandbox、Credential Boundary。

#### 6. 核心心智模型

~~~text
Model Intent
  → Permission（有没有权）
  → Approval（这一次是否同意）
  → Sandbox（即使出错能影响多大）
  → Execute / Deny
~~~

#### 7. 正文框架

1. Prompt 约束为什么不构成安全边界  
   1.1 指令冲突、Prompt Injection 和模型误判。  
   1.2 权限必须由模型之外的确定性系统执行。
2. Permission  
   2.1 用户、任务、Agent、Tool、资源和作用域。  
   2.2 默认拒绝、最小能力和临时授权。
3. Approval  
   3.1 Ask 的触发条件、风险摘要和批准范围。  
   3.2 防止一次批准被扩大复用。
4. Sandbox  
   4.1 文件、进程、网络和凭证隔离。  
   4.2 Sandbox 不是 Permission 的替代品。
5. Policy 合并  
   5.1 Deny 优先、Ask 不能被后续插件升级为 Allow。  
   5.2 记录最终决策链。
6. BuildPilot M1 边界练习  
   6.1 只读 Fixture、无 Shell、无写入、无 reparse point。  
   6.2 提案和实际执行分离。

#### 8. 贯穿案例

Agent 请求读取白名单外源码并建议修改 asmdef，系统分别处理读和写。

#### 9. 与 DeepSeek Harness 的关系

26、27 检查 DSH Tool Policy、Sandbox、取消和审计入口。

#### 10. 与 BuildPilot 的关系

决定第一阶段只做读取与建议，任何 Patch 应在未来单独批准和验证。

#### 11. 需要的证据

Sandbox / Tool Policy 官方资料、越权 Fixture、审批 Trace。安全结论不能只靠概念。

#### 12. 容易混淆的概念

Permission ≠ Approval；Approval ≠ Authentication；Sandbox ≠ 完全安全；Prompt Rule ≠ Policy。

#### 13. 本篇明确不讲什么

不设计生产密钥系统，不开放 Shell，不给出“绝对安全”承诺。

#### 14. 学习检查

- 用户批准“读取这个目录”，是否同时批准执行目录中的脚本？
- Tool Allowlist 只有 readFile，为什么仍需要路径 Policy？
- Sandbox 中允许删除文件，是否就不需要 Approval？

### 15｜Retry、Timeout、Cancellation 与 Recovery

#### 1. 本篇定位

韧性原理篇。建立失败分类与恢复语义，避免把所有失败都处理成“再问模型一次”。

#### 2. 为什么现在学它

- 上一节：Agent 已受权限控制，但运行仍会遇到模型、Tool、外部系统和用户取消。
- 问题：无差别 Retry 会重复副作用、烧掉预算并掩盖根因。
- 本节：让失败成为显式状态。

#### 3. 学完以后应该能回答什么

- 哪些错误可重试，哪些必须停止？
- Timeout 与 Cancellation 有什么区别？
- 幂等性为什么决定 Retry 安全？
- Resume 需要哪些 Durable State？
- Recovery 是继续、补偿还是重新开始？

#### 4. 前置知识

08 Loop；09 Tool；12 Workflow；14 Policy。

#### 5. 核心概念

Transient Error、Permanent Error、Timeout、Cancellation Token、Idempotency、Retry Budget、Checkpoint、Resume、Compensation、Recovery。

#### 6. 核心心智模型

~~~text
Failure → Classify
  ├─ Retry safely
  ├─ Compensate
  ├─ Resume from checkpoint
  ├─ Ask human
  └─ Stop honestly
~~~

#### 7. 正文框架

1. Agent 的失败面  
   1.1 模型 API、Schema、Tool、Policy、外部系统、预算和用户取消。  
   1.2 错误分类必须早于 Retry。
2. Timeout 与 Cancellation  
   2.1 Deadline、局部超时和全 Run 取消传播。  
   2.2 Tool 必须合作响应取消。
3. Retry  
   3.1 指数退避、Retry Budget、幂等键。  
   3.2 模型格式修复与业务重新调查不是同一种重试。
4. Recovery  
   4.1 Checkpoint、Durable Event、Resume。  
   4.2 副作用任务的补偿与人工接棒。
5. 失败输出  
   5.1 partial result、unverified scope、next safe action。  
   5.2 不把取消或超时写成诊断完成。

#### 8. 贯穿案例

Jenkins API 超时、用户中途取消、readFile 成功后模型请求失败三种恢复路径。

#### 9. 与 DeepSeek Harness 的关系

24-27 检查 Loop、Session、Tool 和运行控制怎样协作处理取消与恢复。

#### 10. 与 BuildPilot 的关系

为长时间 Startup 调查设计 checkpoint、异步数据等待和安全 Resume。

#### 11. 需要的证据

取消 Trace、超时 Fixture、幂等性测试、Session 恢复实验。必须实验后写具体行为。

#### 12. 容易混淆的概念

Retry ≠ Recovery；Timeout ≠ Cancellation；Resume ≠ Replay；部分结果 ≠ 成功。

#### 13. 本篇明确不讲什么

不设计分布式事务，不承诺所有 Tool 可恢复，不自动重试破坏性动作。

#### 14. 学习检查

- Tool 已创建远程资源后响应超时，能否直接重试？
- 用户取消后，为什么还要持久化最后一个安全状态？
- 模型 JSON 格式错误与证据不足应该使用同一种 Retry 吗？

### 16｜Token、推理成本、延迟与 Budget

#### 1. 本篇定位

成本原理篇。建立 Agent Run 的全链成本账本，而不是只计算首轮 Prompt。

#### 2. 为什么现在学它

- 上一节：Loop 可能 Retry、Resume 并调用多个 Tool。
- 问题：Agent 成本由多 Step、历史、Schema、Tool Result、Reasoning 和子 Agent 共同累积。
- 本节：定义 Cost Ledger 与多维 Budget。

#### 3. 学完以后应该能回答什么

- Agent 总 Token 从哪里产生？
- Context、Step、Tool Result 与 Reasoning Budget 怎样互相影响？
- Prefix Cache 能优化什么，不能修正什么？
- 成本、延迟和正确率怎样联合评估？
- Budget Exhausted 应产生什么结果？

#### 4. 前置知识

08 Loop；10 Context；15 Retry。

#### 5. 核心概念

Input Token、Output Token、Reasoning Token、Cached Input、Step Budget、Context Budget、Cost Ledger、Model Routing、Result Spill、Budget Guard。

#### 6. 核心心智模型

~~~text
Run Cost =
Σ(Model Input + Output + Reasoning - Cache Benefit)
+ Tool / Retrieval / Retry / Subagent Cost
~~~

#### 7. 正文框架

1. 为什么聊天成本模型不够  
   1.1 多 Step 重复前缀、历史和 Tool Schema。  
   1.2 Tool 与外部系统也有时间和金钱成本。
2. Usage Ledger  
   2.1 Run、Turn、Step、Model、Tool 五层记账。  
   2.2 价格是时效数据，Token 和 Usage 是运行事实。
3. Budget 类型  
   3.1 Token、Step、Wall-clock、Tool Call、现金和高风险动作预算。  
   3.2 Budget 是 Policy，不只是报表。
4. 优化杠杆  
   4.1 稳定前缀、按需 Tool、Context 选择、Spill、Compaction、模型路由。  
   4.2 不用删 Evidence 换取漂亮成本。
5. 联合 Eval  
   5.1 成本、延迟、正确率、引用完整率和人工返工。  
   5.2 建立 Pareto 而不是单指标排名。

#### 8. 贯穿案例

Compile Case 的三种 Context 策略成本对照；Startup Case 的 Step Budget 设计。

#### 9. 与 DeepSeek Harness 的关系

27 研究 DSH 如何在具体 Context Lifecycle 和运行控制中记录与限制成本。

#### 10. 与 BuildPilot 的关系

定义 Per-run Usage、Step Guard、Tool Result Spill 和不同调查场景预算。

#### 11. 需要的证据

模型官方价格与 Usage 规范、固定日期；A/B Trace 与成本计算表。具体结论必须实验。

#### 12. 容易混淆的概念

Token 少 ≠ 便宜；Cache 命中 ≠ 请求正确；Budget ≠ 事后统计；Reasoning Effort ≠ 智能等级。

#### 13. 本篇明确不讲什么

不讲服务端推理引擎优化，不把当前价格写成长期常量。

#### 14. 学习检查

- Tool Schema 从 3 个增到 80 个，哪部分成本首先上升？
- 压缩后成本下降但证据引用丢失，这算优化成功吗？
- Run 达到 Step Budget 时，系统应该直接丢弃已有调查结果吗？

### 17｜Trace、Replay 与 Eval：怎样知道 Agent 为什么成功或失败

#### 1. 本篇定位

可观测与评测主线篇。把“感觉效果不错”变成可重放、可归因、可比较的工程证据。

#### 2. 为什么现在学它

- 上一节：Agent 已有成本和停止边界。
- 问题：只有最终回答，无法判断错误来自 Prompt、Context、Model、Tool、Policy 还是 Workflow。
- 本节：建立 Trace 数据模型和 Eval 分层。

#### 3. 学完以后应该能回答什么

- Trace 至少要覆盖哪些事件？
- Replay 是重放事件还是重新调用模型？
- Fixture Eval 与真实 Unity / Jenkins 验证有什么差别？
- 怎样比较通用 Agent 与专用 Agent？
- 哪些指标能揭示人工返工点？

#### 4. 前置知识

08-16，尤其是 State、Evidence、Policy 和 Cost。

#### 5. 核心概念

Trace ID、Span / Event、Request Snapshot、Decision Record、Replay、Deterministic Projection、Fixture、Benchmark、Evaluator、Regression、Human Review。

#### 6. 核心心智模型

~~~text
Run Trace
→ Reconstruct what happened
→ Classify failure layer
→ Evaluate against Fixture / Policy / Human rubric
→ Feed regression suite
~~~

#### 7. 正文框架

1. 最终答案为什么不是足够证据  
   1.1 同样结论可能来自正确证据或猜测。  
   1.2 成功率不能解释越权、成本和不可复现。
2. Trace 数据模型  
   2.1 Run、Turn、Step、Context Snapshot、Model Request、Tool、Policy、Result。  
   2.2 敏感信息、原始内容和摘要的保存边界。
3. Replay 的两种含义  
   3.1 从事件重建 Projection。  
   3.2 用冻结输入重新执行并比较非确定性输出。
4. Eval 分层  
   4.1 Schema / policy unit eval、golden fixture、offline benchmark、shadow run、真实渠道验收。  
   4.2 不能用 Fixture 代替 Unity BatchMode 或 Jenkins。
5. 指标设计  
   5.1 根因准确、Evidence 引用、越权、Token、延迟、Tool 次数、返工点。  
   5.2 同模型与跨模型对照的公平性。
6. 反馈闭环  
   6.1 失败进入 Prompt、Context、Tool、Skill、Workflow 或 Eval 修复队列。  
   6.2 不把所有失败都归咎于模型。

#### 8. 贯穿案例

Compile Golden Fixture 的完整 Trace；隐藏参考答案与模型独立调查严格分离。

#### 9. 与 DeepSeek Harness 的关系

25、27 研究事件、Trace、恢复和可重建性。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot 是否能作为岗位 Demo 被解释和审计，而不仅是现场演示。

#### 11. 需要的证据

Trace Schema、至少四条黄金轨迹、Eval Harness、一次失败归因实例。必须先有 Trace 再写结论。

#### 12. 容易混淆的概念

Trace ≠ Transcript；Replay ≠ 必然同输出；Fixture ≠ Runtime Acceptance；Evaluator Score ≠ 用户价值。

#### 13. 本篇明确不讲什么

不做通用 LLM Benchmark，不隐藏测试答案进入 Context，不宣称离线结果等于生产。

#### 14. 学习检查

- 最终根因正确但引用证据不存在，应判成功吗？
- 重新调用模型得到不同路径，是否说明事件 Projection 不可重放？
- 专用 Agent Tool 更少，比较时怎样避免给它不公平优势或劣势？

### 18｜Single Agent、Agent as Tool、Handoff、Subagent 与 Multi-Agent

#### 1. 本篇定位

后置映射篇。只在单 Agent、Workflow、Context 和 Eval 已建立后讨论职责拆分。

#### 2. 为什么现在学它

- 上一节：单 Agent 已具备完整运行和评测机制。
- 问题：只有在能测出瓶颈时，才知道拆 Agent 是否改善系统。
- 本节：按控制权、上下文和失败传播选择组合模式。

#### 3. 学完以后应该能回答什么

- Agent as Tool 与普通 Tool 有什么差别？
- Handoff 后谁拥有后续控制权？
- Subagent 是否必须有独立 Session？
- Multi-Agent 什么时候改善隔离，什么时候制造上下文损失？
- 为什么“角色更多”不是架构进步？

#### 4. 前置知识

08 Loop；12 Workflow；16 Cost；17 Trace / Eval。

#### 5. 核心概念

Single Agent、Manager、Agent as Tool、Handoff、Subagent、Delegation、Context Boundary、Result Contract、Failure Propagation。

#### 6. 核心心智模型

~~~text
先单 Agent
→ 发现可测的职责 / 权限 / Context 边界
→ 选择 Agent as Tool、Handoff 或 Code Orchestration
→ 用 Eval 证明拆分收益大于协调成本
~~~

#### 7. 正文框架

1. 为什么 Multi-Agent 必须后学  
   1.1 单 Agent 问题尚未解决时，拆分只会复制问题。  
   1.2 流行叙事与实际协调成本。
2. 四种组合模式  
   2.1 Single Agent + Tools。  
   2.2 Manager 把专员当 Tool。  
   2.3 Handoff 转移对话与责任。  
   2.4 Code Orchestration 固定调用多个 Agent。
3. 选择维度  
   3.1 控制权、Context、权限、输出合同、并发和失败传播。  
   3.2 Session 与 Cost 怎样变化。
4. Eval 拆分收益  
   4.1 专员准确率、上下文节省、权限隔离。  
   4.2 交接遗漏、重复 Token、延迟和不可归因。
5. BuildPilot 的保守结论  
   5.1 先保留单 Agent。  
   5.2 只有 Startup Case 的明确子调查在数据支持后才考虑拆分。

#### 8. 贯穿案例

把日志解析“专员”拆成 Subagent 与保留确定性 parseUnityLog Tool 的对照。

#### 9. 与 DeepSeek Harness 的关系

22、28 检查 Subagent Provider、Profile 和 Host 编排的实际扩展边界。

#### 10. 与 BuildPilot 的关系

Design v1 默认单 Agent，不为展示复杂度建立 Agent Swarm。

#### 11. 需要的证据

主流 SDK 官方编排文档、单 / 多 Agent 对照 Trace、协调成本数据。

#### 12. 容易混淆的概念

Subagent ≠ Tool；Handoff ≠ Agent as Tool；并发 ≠ 多 Agent；角色 Prompt ≠ 独立 Agent。

#### 13. 本篇明确不讲什么

不讲大规模 Swarm，不以 Agent 数量作为岗位能力证明。

#### 14. 学习检查

- 日志解析是确定性任务，拆成 Agent 有什么额外成本？
- Handoff 后原 Agent 还能直接决定下一步吗？
- 两个 Agent 使用同一长 Context，是否真的获得了隔离收益？

### 19｜为什么 Agent 最终需要 Harness

#### 1. 本篇定位

Part II 收束篇。用前十二篇机制推导 Harness，而不是先给框架定义。

#### 2. 为什么现在学它

- 上一节：Runtime 已逐步拥有 Tool、Context、Workflow、Policy、Recovery、Cost 与 Eval。
- 问题：这些横切机制分散在各处会冲突、重复并难以替换。
- 本节：推导 Harness 作为能力组合与控制面的必要性。

#### 3. 学完以后应该能回答什么

- Harness 解决了 Runtime 的哪些横切问题？
- Harness 是产品、框架、控制面还是责任集合？
- 为什么 Permission、Budget 和 Trace 需要统一作用域？
- 什么时候轻量 Runtime 已经够用？
- Harness 怎样避免增长成 Bloat？

#### 4. 前置知识

07-18 全部 Part II。

#### 5. 核心概念

Harness、Control Plane、Capability Composition、Scope、Policy Chain、Observability、Recovery Boundary、Profile、Provider、Lifecycle。

#### 6. 核心心智模型

~~~text
Agent Runtime 负责“怎样继续执行”
Harness 负责“以哪些能力、身份、边界、预算和证据执行”
~~~

#### 7. 正文框架

1. 从横切问题反推 Harness  
   1.1 Tool Policy、Context、Session、Budget、Trace 各自局部实现的冲突。  
   1.2 需要统一 Run / Agent / Host Scope。
2. Harness 的最小责任  
   2.1 能力注册与组合。  
   2.2 Policy / Permission。  
   2.3 Session / Trace / Budget / Recovery。  
   2.4 Host 与 Provider 接口。
3. Harness 不是什么  
   3.1 不等于 Model Wrapper、Agent Loop 或工作流产品。  
   3.2 不要求所有实现叫 Harness。
4. 演化路径  
   4.1 Runtime v0 → 局部 Policy → 可组合能力 → 可观测恢复。  
   4.2 什么时候不要继续抽象。
5. 评价 Harness 的问题清单  
   5.1 能否重建、收窄、拒绝、取消、恢复和评测。  
   5.2 复杂度是否与任务风险匹配。
6. 进入源码教材  
   6.1 把上述问题变成 DSH 阅读问题。  
   6.2 不以目录结构替代工程问题。

#### 8. 贯穿案例

比较三个阶段：单文件 Loop、带 Tool Policy 的诊断 Runtime、可配置的 BuildPilot Harness 设计。

#### 9. 与 DeepSeek Harness 的关系

这是 20 的直接前置；Part III 每篇都要回答 DSH 怎样实现本篇的一项 Harness 责任。

#### 10. 与 BuildPilot 的关系

决定 BuildPilot Harness 只吸收风险和可观察性真正需要的机制。

#### 11. 需要的证据

多框架架构资料、现有 AI 赋能 08 与 Harness Engineering 边界对照、最小责任矩阵。

#### 12. 容易混淆的概念

Harness ≠ Agent；Harness ≠ Workflow；插件多 ≠ Harness 成熟；抽象多 ≠ 可维护。

#### 13. 本篇明确不讲什么

不介绍 DSH 源码细节，不设计 BuildPilot 类图，不讨论 Harness 生命周期治理细节。

#### 14. 学习检查

- 一个只有固定 Loop 和三只 Tool 的 Demo 是否必须做插件系统？
- Tool Policy 分散在 CLI 和 Web Host 会产生什么风险？
- Harness 新增一层抽象时，应该要求什么可验证收益？

## Part III｜DeepSeek Harness 源码教材

### 20｜怎样把 DeepSeek Harness 当作教材，而不是 API 手册

#### 1. 本篇定位

源码阶段索引篇。锁定研究对象、证据等级和问题地图，阻止 Part III 退化为目录导览。

#### 2. 为什么现在学它

- 上一节：已经从通用机制推导出 Harness 的责任。
- 问题：直接从仓库目录开始，容易把当前实现误写成通用定义。
- 本节：先固定 commit，再把 19 的问题映射为源码阅读任务。

#### 3. 学完以后应该能回答什么

- 为什么必须锁定 tag 或 commit？
- 官方文档、源码、运行观察和本文推断如何分级？
- DSH 覆盖 Application、Runtime、Harness 中的哪些部分？
- 哪些结论只能描述 Developer Preview 的当前版本？
- 怎样为每篇源码文章建立验证入口？

#### 4. 前置知识

01-19，尤其是 06 与 19。

#### 5. 核心概念

Evidence Baseline、Pinned Commit、Architecture Map、Subsystem、Lifecycle、Observation、Inference、Design Mapping。

#### 6. 核心心智模型

~~~text
课程抽象问题
→ 锁定文档 / 源码证据
→ 数据结构与生命周期
→ 可观察验证
→ 收益 / 代价 / 替代方案
→ BuildPilot 取舍
~~~

#### 7. 正文框架

1. 为什么 DSH 只是教材  
   1.1 通用抽象不依赖单一框架。  
   1.2 源码价值在于展示工程取舍。
2. 锁定研究快照  
   2.1 commit、日期、文档版本、构建命令和环境。  
   2.2 master 漂移与链接失效风险。
3. 四级事实标签  
   3.1 Official Document Fact。  
   3.2 Pinned Source Fact。  
   3.3 Runtime Observation。  
   3.4 Our Inference / BuildPilot Mapping。
4. 阅读地图  
   4.1 Plugin Kernel、Profile、Prompt / Context、Loop、Session、Tool、Controls。  
   4.2 每个子系统对应 Part II 哪个问题。
5. 验证策略  
   5.1 静态符号与调用路径。  
   5.2 最小启动、配置 dump、Trace 或测试入口。  
   5.3 无法运行时怎样诚实标记。

#### 8. 贯穿案例

建立一张 DSH Evidence Matrix，而不是运行 BuildPilot。

#### 9. 与 DeepSeek Harness 的关系

本篇定义后续所有 DSH 文章的证据合同。

#### 10. 与 BuildPilot 的关系

防止 BuildPilot 直接复制未验证、版本敏感或只适合通用平台的机制。

#### 11. 需要的证据

官方仓库、官方文档、锁定 commit、依赖与运行说明。必须先完成源码 evidence card。

#### 12. 容易混淆的概念

官方意图 ≠ 本文推测；可扩展 ≠ 已内置；当前实现 ≠ 行业标准；静态源码 ≠ Runtime Verified。

#### 13. 本篇明确不讲什么

不详细解释任何子系统，不比较模型效果，不声称已运行尚未执行的示例。

#### 14. 学习检查

- 官方文档与锁定源码冲突时，文章应怎样写？
- 一个插件理论上能实现 RAG，能否称 DSH 内置 RAG？
- 只看到类定义但没追踪调用，能够证明生命周期吗？

### 21｜Everything is a Plugin：插件内核解决了什么

#### 1. 本篇定位

源码原理篇。用 DSH 插件内核验证 Capability Composition、Scope 与 Lifecycle 的工程实体。

#### 2. 为什么现在学它

- 上一节：已锁定源码并建立阅读问题。
- 问题：Harness 需要组合多种横切能力，固定核心容易形成分支和耦合。
- 本节：研究 DSH 为什么把大量能力放进统一插件模型。

#### 3. 学完以后应该能回答什么

- “Everything is a Plugin”解决了哪些组合问题？
- Context、Service、Event 和 Effect 如何协作？
- 插件作用域与卸载为什么重要？
- 插件化带来了哪些调试和认知成本？
- 普通 DI 或固定核心 + 扩展点能否替代？

#### 4. 前置知识

19 Harness；20 Evidence Baseline。

#### 5. 核心概念

Plugin、Cordis Context、Service、Event、Effect、Scope、Dispose、Dependency、Composition。

#### 6. 核心心智模型

~~~text
Plugin installs
→ registers Service / Event / Effect
→ participates within Scope
→ produces reversible side effects
→ disposes cleanly
~~~

#### 7. 正文框架

1. 从 Harness 横切能力回到组合问题  
   1.1 模型、Tool、Session、Policy 为什么需要共同生命周期。  
   1.2 固定核心与条件分支怎样增长。
2. 锁定源码里的插件最小模型  
   2.1 关键接口、安装入口和 Context。  
   2.2 Service、Event、Effect 的实际调用路径。
3. Scope 与可逆副作用  
   3.1 Agent / Session / Host 作用域。  
   3.2 卸载和资源回收怎样验证。
4. 收益  
   4.1 能力替换、组合、测试和宿主差异。  
   4.2 Profile / Bundle 的基础。
5. 代价与替代方案  
   5.1 隐式注册、事件跳转、初始化顺序和调试复杂度。  
   5.2 普通 DI、模块化单体、固定核心 + 少量扩展点对照。
6. BuildPilot 取舍  
   6.1 只借鉴 Provider / Adapter seam。  
   6.2 没有两个真实实现前，不复制完整插件内核。

#### 8. 贯穿案例

追踪一个锁定插件从注册到释放的完整生命周期。

#### 9. 与 DeepSeek Harness 的关系

对应 Plugin Core 的关键接口、Context、Service、Event 与 Effect；具体符号必须在 evidence card 中填写。

#### 10. 与 BuildPilot 的关系

Design v1 可以定义扩展 seam，但默认模块化单体，不预设 Everything is a Plugin。

#### 11. 需要的证据

锁定源码符号、调用路径、生命周期测试或最小运行 Trace。必须先做源码 evidence card。

#### 12. 容易混淆的概念

Plugin ≠ Tool；Context（插件容器）≠ Model Context；Event ≠ Session Event；可插拔 ≠ 低复杂度。

#### 13. 本篇明确不讲什么

不遍历全部插件，不把 Cordis 设计写成 Agent 必需标准。

#### 14. 学习检查

- 只有一个文件系统实现时，为什么完整插件系统可能过度？
- 插件卸载后监听器仍存活，破坏了什么合同？
- 普通 DI 已能替换 Provider 时，还需要哪些证据才能引入插件内核？

### 22｜Profile、Bundle、Provider 与 Capability Seam

#### 1. 本篇定位

源码架构篇。研究通用 Harness 怎样组合出不同 Agent / Host 能力集。

#### 2. 为什么现在学它

- 上一节：已经理解插件怎样安装能力。
- 问题：真实产品需要 Web、Headless、只读和不同模型配置，不能靠复制 Runtime。
- 本节：研究配置组合、Provider 替换和可复现启动。

#### 3. 学完以后应该能回答什么

- Profile 与 Bundle 分别解决什么？
- 配置叠层怎样影响最终能力集？
- Provider 与 Consumer 如何通过 Capability Seam 解耦？
- 同一 Harness 怎样服务多个 Host？
- 配置覆盖怎样被 dump 和复现？

#### 4. 前置知识

06 系统总图；21 Plugin Kernel。

#### 5. 核心概念

Profile、Bundle、Patch、Overlay、Provider、Consumer、Capability、Service Definition、Host、Configuration Precedence。

#### 6. 核心心智模型

~~~text
Base Bundle
→ Profile Bundles / Patch
→ Home / Runtime Overlay
→ Effective Plugin + Provider Set
→ Host starts Agent
~~~

#### 7. 正文框架

1. 通用 Harness 的组合问题  
   1.1 Web、Headless、不同模型和权限为什么不应 fork Runtime。  
   1.2 能力集与用户身份、部署环境的关系。
2. Profile / Bundle / Patch  
   2.1 锁定源码中的配置对象和加载顺序。  
   2.2 同名配置、覆盖和冲突行为。
3. Provider / Consumer  
   3.1 Service Definition 与实现注册。  
   3.2 LLM、FS、Subprocess、Sandbox 等 Capability 实例。
4. 可复现性  
   4.1 输出 Effective Config / Plugin Tree。  
   4.2 配置漂移和本地 Home Patch 风险。
5. 多 Host  
   5.1 Host 只适配输入、呈现和生命周期。  
   5.2 权限与 Session 所有权的差异。
6. BuildPilot 取舍  
   6.1 Diagnostic Profile 与未来 Write-enabled Profile 分离。  
   6.2 CLI first，不提前开发所有 Host。

#### 8. 贯穿案例

对比 DSH Web 与 Headless 的 Effective Capability Set。

#### 9. 与 DeepSeek Harness 的关系

对应 Profile、Bundle、Patch、Provider 和 Host 的锁定源码结构。

#### 10. 与 BuildPilot 的关系

借鉴能力集与权限配置，不照搬复杂叠层；Design v1 明确只读 Profile。

#### 11. 需要的证据

配置 Schema、加载调用路径、Effective Config dump、两个 Host 的启动 Trace。

#### 12. 容易混淆的概念

Profile ≠ Agent 人格；Bundle ≠ 部署包；Provider ≠ Tool；Host ≠ Runtime。

#### 13. 本篇明确不讲什么

不做完整配置参考手册，不讨论每个 Provider 的实现。

#### 14. 学习检查

- 本地 Overlay 改变 Tool 权限却未记录，会破坏什么？
- Web 与 Headless 是否应该各自实现 Agent Loop？
- Diagnostic Profile 与 Write Profile 只差 Prompt 是否足够？

### 23｜System Prompt 与 Context Assembly

#### 1. 本篇定位

源码原理篇。用锁定源码验证 Prompt Section、动态 Context 和历史怎样装配成 Step Request。

#### 2. 为什么现在学它

- 上一节：知道不同 Profile 会安装不同能力。
- 问题：多个插件都可能贡献指令、变量和 Context，顺序与冲突会失控。
- 本节：回收 03 与 10 的 Prompt / Context 主线。

#### 3. 学完以后应该能回答什么

- 多插件 Prompt Section 怎样排序？
- Scope、Shadow 或 complete 之类机制解决什么？
- 动态 Context 怎样与稳定前缀分离？
- Compaction 后哪些不变量必须重注入？
- 怎样重建一次模型请求的来源？

#### 4. 前置知识

03 Prompt / Context；10 Context Lifecycle；21-22 插件与 Profile。

#### 5. 核心概念

Prompt Section、Prompt Context、Provider、Variable、Ordered Assembly、Scope、Snapshot、Stable Prefix、Dynamic Context。

#### 6. 核心心智模型

~~~text
Installed Contributors
→ Resolve scope / variables / order
→ Assemble stable + dynamic sections
→ attach history / Tool schema
→ Request Snapshot
~~~

#### 7. 正文框架

1. 多来源装配问题  
   1.1 身份、宿主、Tool Guidance、任务状态和历史由不同插件贡献。  
   1.2 拼字符串为什么不可维护。
2. 锁定源码对象  
   2.1 Prompt Section / Context / Provider 的字段和注册路径。  
   2.2 排序、覆盖、Shadow、complete 的真实行为。
3. 动态 Context  
   3.1 变化检测、Agent / Session Scope 和快照。  
   3.2 Tool Schema 与历史如何汇合。
4. 冲突与失败  
   4.1 重复 Section、未知变量、多个终结 Section。  
   4.2 应显式失败还是静默覆盖。
5. Cache 与可重建性  
   5.1 稳定前缀的收益。  
   5.2 Trace 保存 Effective Assembly 与来源。
6. BuildPilot 取舍  
   6.1 Identity、Deployment Persona、Tool Guidance、Context View 分开。  
   6.2 小系统先用有序 Contributor，不复制全部语法。

#### 8. 贯穿案例

为同一 Step 导出 Effective Prompt / Context Assembly，并追溯每个 Section 来源。

#### 9. 与 DeepSeek Harness 的关系

对应 System Prompt subsystem 与动态 Context 的锁定源码生命周期。

#### 10. 与 BuildPilot 的关系

形成 IContextContributor 与 Context Receipt 的设计依据。

#### 11. 需要的证据

源码 evidence card、重复 / 缺变量负例、请求 Trace。必须先验证实际装配顺序。

#### 12. 容易混淆的概念

Plugin Context ≠ Model Context；Prompt Section ≠ Message；覆盖成功 ≠ 冲突正确；稳定前缀 ≠ 静态事实。

#### 13. 本篇明确不讲什么

不做 Prompt 文案教程，不提前展开 Token 优化，不声称未验证的关键字语义。

#### 14. 学习检查

- 两个插件贡献互相冲突的安全规则，静默后者覆盖是否合理？
- 稳定前缀中包含当前分支名，会发生什么？
- 只保存最终文本而不保存 Section 来源，调试时缺什么？

### 24｜Turn、Step、Inbox 与 Agent Loop

#### 1. 本篇定位

源码生命周期篇。把 08 的通用 Loop 映射为 DSH 的事件入口、Turn 和 Step。

#### 2. 为什么现在学它

- 上一节：已经知道每次 Step 的请求怎样装配。
- 问题：还不知道外部消息怎样唤醒 Runtime，以及 Tool Batch 后怎样继续。
- 本节：追踪一条消息从 Inbox 到 Stop 的调用链。

#### 3. 学完以后应该能回答什么

- Inbox 与用户 Message 是什么关系？
- 一次 Turn 为什么可能产生零到多个 Step？
- 多 Tool Call 怎样调度和汇总？
- 谁判断 Continue / Stop？
- Cancellation 怎样穿过 Loop？

#### 4. 前置知识

08 Agent Loop；22 Profile；23 Context Assembly。

#### 5. 核心概念

Inbox、Wakeup、Turn、Step、Model Request、Tool Batch、Continuation、Stop、Cancellation。

#### 6. 核心心智模型

~~~text
Inbox Event → Turn
→ Assemble Step Request
→ Model Output
→ Tool Batch / Final Output
→ New Events
→ Next Step / Stop
~~~

#### 7. 正文框架

1. 从外部事件到 Turn  
   1.1 Host 写入什么，Runtime 监听什么。  
   1.2 无需模型调用的 Turn 是否存在。
2. Step 生命周期  
   2.1 请求装配、调用、响应解析和事件记录。  
   2.2 Step ID、父级与状态。
3. Tool Batch  
   3.1 多调用并发或顺序的实际规则。  
   3.2 结果如何形成下一 Step Observation。
4. Continue / Stop  
   4.1 完成、无 Tool、Policy 拒绝、错误、预算与取消。  
   4.2 模型建议和 Runtime 终止权。
5. 四条验证轨迹  
   5.1 无 Tool。  
   5.2 单 Tool。  
   5.3 多 Tool。  
   5.4 中途取消。
6. BuildPilot 取舍  
   6.1 保留显式 Turn / Step。  
   6.2 不复制与当前用例无关的并发复杂度。

#### 8. 贯穿案例

锁定 DSH 示例中的一条最短 Tool 调用 Trace。

#### 9. 与 DeepSeek Harness 的关系

对应 Agent Lifecycle、Inbox、Turn、Step 和 Tool 批处理调用链。

#### 10. 与 BuildPilot 的关系

为 Compile / Startup 两类 Run 定义可观察状态机。

#### 11. 需要的证据

关键符号、调用路径、四条运行或测试 Trace。必须先做源码 evidence card。

#### 12. 容易混淆的概念

Inbox ≠ Chat UI；Turn ≠ Step；Tool Batch ≠ Multi-Agent；Stop ≠ Success。

#### 13. 本篇明确不讲什么

不讲 Session 持久化细节，不完整展开 Tool Policy。

#### 14. 学习检查

- Policy 在模型调用前就拒绝任务，Turn 是否仍应留事件？
- 两只 Tool 并发返回，结果顺序怎样影响下一 Step？
- 用户取消发生在 Tool 执行中，哪些状态必须落盘？

### 25｜Session Event、Replay、Resume 与 Fork

#### 1. 本篇定位

源码状态篇。研究 DSH 为什么用事件而不是单纯 Message List 表示可恢复会话。

#### 2. 为什么现在学它

- 上一节：Loop 会持续产生请求、Tool 和状态变化。
- 问题：只保存聊天文本无法恢复执行、审计 Policy 或构建不同 Projection。
- 本节：把 Session 作为 append-only 事实流研究。

#### 3. 学完以后应该能回答什么

- Durable Event 与 Live Event 有什么区别？
- Replay 重建哪些 Projection？
- Resume 需要哪些运行状态？
- Fork 怎样定义父事件和新分支？
- Transcript、Model History 与 Telemetry 为什么不同？

#### 4. 前置知识

04 Session / Memory；17 Trace / Replay；24 Loop。

#### 5. 核心概念

Append-only Event、Projection、Durable Event、Live Event、Replay、Resume、Fork、Transcript、Model History、Compaction Event。

#### 6. 核心心智模型

~~~text
Append-only Session Events
 ├─→ Model History Projection
 ├─→ UI Transcript
 ├─→ Domain State
 ├─→ Telemetry
 └─→ Resume / Fork
~~~

#### 7. 正文框架

1. Message List 的不足  
   1.1 无法表示 Policy、取消、Tool 生命周期和派生状态。  
   1.2 修改历史会破坏审计。
2. 锁定事件模型  
   2.1 事件类型、序号、时间、Run / Turn / Step 关联。  
   2.2 Durable 与仅实时通知事件。
3. Projection  
   3.1 Model History、Transcript、UI、Trace。  
   3.2 Projection 可以重建，不反写历史。
4. Resume / Fork  
   4.1 恢复点、未完成 Tool、预算与 Context。  
   4.2 Fork 继承什么、隔离什么。
5. Compaction  
   5.1 摘要是新事件还是改写旧事件。  
   5.2 如何保留 Evidence 与未完成状态。
6. BuildPilot 取舍  
   6.1 JSONL Event Store 作为设计原型。  
   6.2 只定义需要的事件，不复制所有 UI 事件。

#### 8. 贯穿案例

从事件流重建一次中断的诊断，并 Fork 出另一条假设调查。

#### 9. 与 DeepSeek Harness 的关系

对应 Session subsystem 的事件、Projection、Resume 和 Fork 事实。

#### 10. 与 BuildPilot 的关系

决定 Session Event Store 与领域 Evidence Store 的关联方式。

#### 11. 需要的证据

事件类型表、写入 / 读取调用路径、Replay / Resume 测试。必须先做源码 evidence card。

#### 12. 容易混淆的概念

Session Event ≠ Plugin Event；Replay ≠ 重新推理；Transcript ≠ Model History；Fork ≠ Copy 全部外部状态。

#### 13. 本篇明确不讲什么

不设计通用 Event Sourcing 平台，不把 Memory 写入 Session 细节混入。

#### 14. 学习检查

- 删除旧 Tool Result 再写摘要，会破坏哪种审计能力？
- Replay 能否保证模型再次生成相同输出？
- Fork 后原 Session 的预算消耗是否应继承？为什么？

### 26｜Tool Registry、Policy 与 Execution Pipeline

#### 1. 本篇定位

源码执行篇。用 DSH 具体管线验证 09、14、15 的 Tool Engineering 抽象。

#### 2. 为什么现在学它

- 上一节：Session 已能保存 Loop 事实。
- 问题：模型 Tool Call 必须经过注册、策略、执行、输出和持久化链。
- 本节：追踪一次 Tool Call 的完整调用路径。

#### 3. 学完以后应该能回答什么

- Registry 怎样向模型和 Host 提供不同视图？
- 参数怎样 canonicalize 和 validate？
- Allow / Deny / Ask 怎样合并？
- Timeout、Cancellation 和并发在哪里实现？
- Model Content、UI Presentation 和持久结果怎样分离？

#### 4. 前置知识

09 Tool Engineering；14 Policy；15 Cancellation；24 Loop；25 Events。

#### 5. 核心概念

Tool Registry、Definition、Provider、Canonical Value、Policy Decision、Pre / Post Hook、Execution Result、Model Content、Presentation。

#### 6. 核心心智模型

~~~text
Registry → Model Tool View
Tool Call → Canonicalize → Validate → Policy
→ Execute → Normalize → Persist
→ Model Content + UI Presentation
~~~

#### 7. 正文框架

1. Registry 的责任  
   1.1 发现、去重、作用域和模型可见列表。  
   1.2 Tool Provider 与具体实现。
2. 输入处理  
   2.1 Schema 验证、Canonical Value、Host-only Metadata。  
   2.2 参数错误的事件与模型反馈。
3. Policy Chain  
   3.1 Pre-execute hook 和决策合并。  
   3.2 Deny / Ask 不能被后续扩展静默升级。
4. Execute  
   4.1 Timeout、Cancellation、Concurrency 和错误。  
   4.2 Side-effect Tool 的额外责任。
5. 输出处理  
   5.1 输出 Schema、裁剪 / Spill、模型内容和 UI。  
   5.2 写入 Session / Trace。
6. 负例验证  
   6.1 坏参数、策略拒绝、超时、取消、大结果。  
   6.2 锁定版本的真实行为与文档差异。
7. BuildPilot 取舍  
   7.1 三只只读 Tool 的简化 Pipeline。  
   7.2 保留不可绕过 Policy，不复制无关 UI 层。

#### 8. 贯穿案例

选择一只锁定 DSH Tool，记录从注册到结果进入下一 Step 的全过程。

#### 9. 与 DeepSeek Harness 的关系

对应 Tool subsystem 与 Tool Execution Pipeline 的核心源码。

#### 10. 与 BuildPilot 的关系

为 BuildPilot ToolDefinition、Policy Hook 和 Evidence Normalizer 提供参考。

#### 11. 需要的证据

源码 evidence card、五类负例测试、真实 Trace。必须先验证 Pipeline 顺序。

#### 12. 容易混淆的概念

Registry ≠ Permission；Provider ≠ Tool；Canonical Value ≠ 已授权参数；Presentation ≠ Model Content。

#### 13. 本篇明确不讲什么

不列举全部 Tool，不实现 BuildPilot Tool，不把当前 Pipeline 宣称为标准。

#### 14. 学习检查

- Policy 插件 A 返回 Deny、B 返回 Allow，最终应怎样合并？
- UI 需要完整输出但模型只需摘要，应该共享哪个对象、分离哪个对象？
- Tool 执行成功但输出不符合 Schema，Session 应记录什么？

### 27｜Cost、Compaction、Trace、Cancellation 与 Recovery

#### 1. 本篇定位

源码运行控制篇。研究 DSH 怎样把多个横切机制接进同一 Run，而不是重新讲通用原理。

#### 2. 为什么现在学它

- 上一节：Loop、Session 和 Tool Pipeline 已经连通。
- 问题：长 Run 需要预算、压缩、调试、取消和恢复共同工作。
- 本节：检验 DSH 的具体接点、作用域和限制。

#### 3. 学完以后应该能回答什么

- Usage 在 Run / Turn / Step 哪层记录？
- Compaction 何时触发，产物怎样进入 Session？
- Cancellation 怎样传播到模型和 Tool？
- Trace 怎样关联 Context、Model 和 Tool？
- Recovery 能恢复到哪一级，哪些外部状态无法保证？

#### 4. 前置知识

10、15-17；23-26。

#### 5. 核心概念

Usage Ledger、Budget Policy、Compaction Trigger、Context Rehydration、Trace Correlation、Cancellation Propagation、Checkpoint、Recovery Boundary。

#### 6. 核心心智模型

~~~text
Run Controls observe every Step
→ budget / trace / cancellation decisions
→ compaction or stop
→ durable events
→ resume within explicit recovery boundary
~~~

#### 7. 正文框架

1. 横切机制接在哪里  
   1.1 Request 前、模型后、Tool 前后和 Session 写入点。  
   1.2 统一关联 ID 与 Scope。
2. Cost / Budget  
   2.1 Usage 字段与累计方式。  
   2.2 Step、Token、时间和 Provider 路由限制。
3. Compaction  
   3.1 触发条件、摘要产物、历史替换或 Projection。  
   3.2 不变量重注入和 Trace 可重建性。
4. Trace  
   4.1 请求、Context、Tool、Policy、错误和 Usage。  
   4.2 哪些内容因敏感性只能保留引用。
5. Cancellation / Recovery  
   5.1 信号传播和状态落盘。  
   5.2 Resume 的保证与外部副作用边界。
6. 综合故障演练  
   6.1 大 Tool Result 触发压缩。  
   6.2 随后用户取消并从安全点 Resume。
7. BuildPilot 取舍  
   7.1 先做 Per-run Budget 和取消 Trace。  
   7.2 Compaction 与 Resume 只在 Startup 长调查阶段需要。

#### 8. 贯穿案例

锁定 DSH 长 Session 的 Context / Usage / Cancellation Trace。

#### 9. 与 DeepSeek Harness 的关系

对应成本、Context 生命周期、Trace、取消和恢复相关插件 / 服务；具体边界以 pinned evidence 为准。

#### 10. 与 BuildPilot 的关系

决定 Design v1 的运行控制接口和分阶段实施顺序。

#### 11. 需要的证据

源码 evidence card、长 Session Trace、取消与 Resume 实验、Usage 计算验证。必须实验。

#### 12. 容易混淆的概念

Compaction ≠ Memory；Trace ≠ Session；Cancel ≠ Rollback；Resume ≠ 外部系统恢复。

#### 13. 本篇明确不讲什么

不重讲 16 的成本公式，不声称 DSH 能恢复所有副作用。

#### 14. 学习检查

- Tool 已改变外部系统后 Session Resume，Harness 能自动恢复什么、不能恢复什么？
- Compaction 结果未记录来源，Trace 还能解释模型输入吗？
- Budget 在 Step 结束后才检查，会有什么超支窗口？

### 28｜RAG、Skill、Workflow 与 Host 应该怎样映射：事实、扩展与取舍

#### 1. 本篇定位

Part III 综合设计映射篇。明确 DSH 已有一级机制、可用扩展点和我们自己的架构提案。

#### 2. 为什么现在学它

- 上一节：已经掌握 DSH 的核心运行结构。
- 问题：课程中的 RAG、Skill、Workflow 未必都是 DSH 一级对象，不能硬找同名类。
- 本节：用 Capability / Plugin / Context / Tool / Host 映射，并总结不可照搬项。

#### 3. 学完以后应该能回答什么

- DSH 当前是否原生定义 RAG、Skill、Workflow？
- Retrieval Tool 与 Context Provider 两种接法有何差别？
- Skill Discovery 应进入哪种 Scope？
- Workflow 应由 Host、Tool 还是插件状态机持有？
- 哪些 DSH 设计值得 BuildPilot 采用，哪些不值得？

#### 4. 前置知识

11、12、18；20-27 全部 DSH 文章。

#### 5. 核心概念

Core Fact、Extension Point、Architecture Mapping、Retrieval Provider、Skill Loader、Workflow Host、Subagent Provider、Shared Harness Core、Multiple Hosts。

#### 6. 核心心智模型

~~~text
通用能力需求
→ 检查 DSH 是否已有一级对象
→ 若无：选择 Plugin / Context / Tool / Host seam
→ 标记为“我们的映射”
→ 验证收益与代价
~~~

#### 7. 正文框架

1. 先做事实矩阵  
   1.1 Core Object、Documented Extension、Source-only Mechanism、Our Proposal。  
   1.2 禁止把“可实现”改写成“已内置”。
2. RAG 映射  
   2.1 Retrieval Tool：由模型决定何时检索。  
   2.2 PromptContext / Context Provider：平台主动注入。  
   2.3 权限、新鲜度、引用和 Usage Receipt。
3. Skill 映射  
   3.1 Metadata Discovery、Instructions 注入、Script / Asset 能力。  
   3.2 与插件生命周期和 Agent Scope 的关系。
4. Workflow 映射  
   4.1 Host State Machine、Workflow Tool、Code Orchestration。  
   4.2 不把 Agent Loop 本身称为业务 Workflow。
5. Subagent 与 Host  
   5.1 Provider / Profile 怎样隔离能力。  
   5.2 Web、Headless、CLI 的输入和权限差异。
6. DSH 设计成绩单  
   6.1 值得借鉴：显式作用域、可替换 seam、事件和 Policy。  
   6.2 谨慎借鉴：完整插件内核、复杂配置叠层、通用多 Host。  
   6.3 不照搬：没有 BuildPilot 需求证据的能力。
7. 进入毕业设计  
   7.1 把课程抽象和 DSH 证据转成 BuildPilot 决策表。  
   7.2 每项采用都写 Problem、Alternative、Decision、Consequence。

#### 8. 贯穿案例

为“历史故障检索”分别设计 Tool 和主动 Context Provider，并比较控制权。

#### 9. 与 DeepSeek Harness 的关系

这是 DSH 阶段的综合判断，不新增未证实的一级能力。

#### 10. 与 BuildPilot 的关系

形成 BuildPilot Architecture Decision Record 的输入。

#### 11. 需要的证据

前八篇 DSH evidence card、官方扩展文档、最小插件原型或设计验证。必须明确事实标签。

#### 12. 容易混淆的概念

Mapping ≠ Source Fact；Plugin 可实现 ≠ Core 内置；Workflow ≠ Loop；Host ≠ Agent。

#### 13. 本篇明确不讲什么

不实现完整 RAG / Skill / Workflow 插件，不给 DSH 做功能宣传。

#### 14. 学习检查

- 找不到 Skill 类，是否说明 DSH 不能加载领域方法？
- 平台主动注入历史事故与模型按需检索，谁拥有检索控制权？
- BuildPilot 只有 CLI Host 时，复制 DSH 多 Host 架构有什么代价？

## Part IV｜BuildPilot Design

### 29｜游戏研发生产管线里，什么问题值得 Agent 化

#### 1. 本篇定位

毕业设计问题空间篇。先研究现有生产管线和确定性工具，再决定 BuildPilot 是否存在以及站在哪里。

#### 2. 为什么现在学它

- 上一节：已经掌握 Agent / Harness 抽象和一个真实工程案例。
- 问题：如果从“我要做 Unity / Jenkins Agent”开始，架构会替一个预设答案找问题。
- 本节：从需求到线上反馈逐层找出真正需要上下文调查和下一步判断的缝隙。

#### 3. 学完以后应该能回答什么

- BuildPilot 为什么存在？
- 哪些问题继续用 Script、Rule 或 Workflow？
- Agent 应连接哪些确定性系统，而不是替代它们？
- Compile 与 Startup 两类场景分别证明什么？
- 第一阶段为什么必须只读？

#### 4. 前置知识

05 自动化边界；19 Harness；28 DSH 取舍。

#### 5. 核心概念

Production Pipeline、System of Record、Deterministic Tool、Investigation Gap、Decision Point、Agent-worthy Task、Read-only Pilot、Escalation Ladder。

#### 6. 核心心智模型

~~~text
现有确定性系统产生事实与执行能力
           ↓
跨系统出现理解、调查和下一步选择
           ↓
Agent 连接证据、形成假设、提出安全动作
~~~

#### 7. 正文框架

1. 游戏研发生产管线地图  
   1.1 需求、配置、程序、资产、构建、平台包、发布、运行、反馈。  
   1.2 每层的 Owner、事实源、工具和已有门禁。
2. 现有确定性能力  
   2.1 Jenkins、Python、Unity BuildPipeline、检查器、日志、监控和 CI/CD。  
   2.2 为什么 Agent 不应重写稳定能力。
3. 寻找 Investigation Gap  
   3.1 多源事实需要关联。  
   3.2 根因未预先枚举。  
   3.3 下一步 Tool 取决于新 Evidence。  
   3.4 需要解释未知和未验证范围。
4. 候选场景评分  
   4.1 价值、不确定性、证据可得、风险、可评测性和接入成本。  
   4.2 编译、构建、启动性能、资源、发布和线上事故对照。
5. BuildPilot v1 使命  
   5.1 游戏研发只读调查与诊断助手。  
   5.2 不执行构建、不改代码、不发布、不操作生产。
6. 渐进授权  
   6.1 Fixture → 本地只读 → Jenkins 只读 → Unity 静态 / BatchMode 验证建议。  
   6.2 每次升级需要独立证据门槛。

#### 8. 贯穿案例

用矩阵比较资产命名检查、Unity 编译错误和启动性能回归。

#### 9. 与 DeepSeek Harness 的关系

使用 28 的取舍表，不从 DSH 现有模块倒推需求。

#### 10. 与 BuildPilot 的关系

正式定义 BuildPilot 的 Problem Statement、Non-goals 和第一阶段授权边界。

#### 11. 需要的证据

真实生产管线、现有工具清单、2-3 个历史问题案例。不能只靠概念写。

#### 12. 容易混淆的概念

Agent-worthy ≠ 自动化价值高；只读 ≠ 无风险；能调用 Jenkins ≠ 应替代 Pipeline。

#### 13. 本篇明确不讲什么

不画类图，不选模型，不承诺效率数据，不设计自动修复。

#### 14. 学习检查

- 资产命名规则已明确且稳定，为什么继续写检查器更合适？
- 启动性能回归涉及多阶段和历史变更，Agent 价值可能在哪里？
- 哪些证据出现前，不应让 BuildPilot 获得写权限？

### 30｜Case A：Unity Compile Diagnosis 作为 Golden Fixture

#### 1. 本篇定位

设计案例篇。用小而可控的编译失败校准数据合同、Tool、Trace 与 Eval，不把它包装成 Agent 最大价值。

#### 2. 为什么现在学它

- 上一节：已经选定只读诊断方向。
- 问题：直接从复杂 Startup Case 设计，会同时引入太多变量。
- 本节：先用 Golden Fixture 验证最小合同和边界。

#### 3. 学完以后应该能回答什么

- Compile Case 为什么适合 Hello World？
- 哪些步骤可确定性完成，哪些留给模型？
- Reference Fixture 怎样避免泄露答案？
- Evidence / Hypothesis / Diagnosis 怎样落字段？
- Offline Eval 与真实 Unity 编译怎样分开？

#### 4. 前置知识

09 Tool；13 Evidence；14 Policy；17 Eval；29 问题空间。

#### 5. 核心概念

Golden Fixture、Isolated Workspace、Reference Answer、Candidate Source、Evidence Channel、DiagnosisResult、Verification Marker。

#### 6. 核心心智模型

~~~text
Compile Log
→ deterministic parse
→ bounded file / reference evidence
→ model hypothesis
→ structured diagnosis
→ offline eval
→ Unity verification: separate channel
~~~

#### 7. 正文框架

1. 为什么选择编译错误  
   1.1 输入和根因可冻结，工具面可收窄。  
   1.2 许多错误可由规则解决，所以它只是教学 Fixture。
2. Golden Case 定义  
   2.1 缺失 asmdef reference 的输入、项目树和预期根因。  
   2.2 Reference Answer 只给 Evaluator，不进入 Agent Context。
3. 最小 Tool 面  
   3.1 parseUnityLog、readFile、searchLiteral。  
   3.2 无 Shell、无写入、无越界和 reparse point。
4. 数据合同  
   4.1 Evidence、Hypothesis、DiagnosisResult、PatchProposal。  
   4.2 candidateSource、confidence、unverifiedScope、unityVerification。
5. Trace 与 Eval  
   5.1 证据引用、Tool 轨迹、越权和预算。  
   5.2 根因正确但路径不诚实的失败例。
6. 验证渠道  
   6.1 Offline Fixture。  
   6.2 静态检查。  
   6.3 Unity BatchMode 或真实编辑器编译。  
   6.4 三者不互相冒充。
7. 对架构的有限启示  
   7.1 验证数据合同和 Policy。  
   7.2 不据此证明复杂多步调查价值。

#### 8. 贯穿案例

BuildPilot M1A 的缺失 BuildPilot.Demo.Shared reference Fixture。

#### 9. 与 DeepSeek Harness 的关系

借鉴 Tool Pipeline、Session Trace 与 Policy seam；不复制通用插件树。

#### 10. 与 BuildPilot 的关系

成为 Design v1 的第一个 Golden Fixture 和未来最小实现入口。

#### 11. 需要的证据

真实隔离 Fixture、参考答案、负例、Trace Schema。必须先完成 evidence card / Fixture 设计。

#### 12. 容易混淆的概念

Reference Answer ≠ Agent Evidence；Offline Pass ≠ Unity Verified；Patch Proposal ≠ Patch Applied。

#### 13. 本篇明确不讲什么

不正式实现 Runtime，不自动修改 asmdef，不把单案例成功率当产品指标。

#### 14. 学习检查

- Evaluator 读参考答案是否会污染 Agent？
- parseUnityLog 已能直接定位某类错误，还需要模型做什么？
- Unity 未运行时，怎样避免标题或结果暗示已经修复？

### 31｜Case B：Startup Performance Diagnosis 作为多步调查

#### 1. 本篇定位

复杂设计案例篇。用跨系统、动态证据和多个假设验证 Agent 的真正调查价值。

#### 2. 为什么现在学它

- 上一节：Compile Case 验证了最小合同，但问题空间过于确定。
- 问题：还没有证明 Agent 是否能根据新 Evidence 决定下一步。
- 本节：设计 15s → 23s 启动回归的多阶段调查。

#### 3. 学完以后应该能回答什么

- Startup Case 为什么更 Agent-worthy？
- 怎样分阶段建立 Hypothesis Tree？
- 指标、构建、Git Diff 和历史事故怎样进入 Context？
- Workflow 固定什么，Agent 决定什么？
- 怎样为开放性调查建立 Eval？

#### 4. 前置知识

08 Loop；10 Context；12 Workflow；13 Evidence；17 Eval；29。

#### 5. 核心概念

Startup Stage、Metric Baseline、Regression Window、Hypothesis Tree、Investigation Plan、Historical Context、Evidence Coverage、Root Cause Candidate。

#### 6. 核心心智模型

~~~text
Regression Signal
→ locate abnormal stage
→ create competing hypotheses
→ choose next evidence source
→ update / eliminate hypotheses
→ diagnosis + verification proposal
~~~

#### 7. 正文框架

1. 定义问题而不是只给“启动慢”  
   1.1 指标定义、版本、设备、网络和时间基线。  
   1.2 15s 与 23s 是否可比。
2. 阶段模型  
   2.1 game.js 下载、WASM、引擎、AssemblyLoad、资源、热更、登录、平台 API。  
   2.2 阶段可能重叠，不能把图表顺序当严格因果。
3. 数据源和 Tool 候选  
   3.1 Metrics、构建清单、Git Diff、CDN、日志、历史事故。  
   3.2 权限、新鲜度和 Source Citation。
4. Hypothesis-driven Loop  
   4.1 先定位异常阶段。  
   4.2 建立多个可反驳假设。  
   4.3 选择信息增益最大的下一步。  
   4.4 更新 Evidence 与停止条件。
5. Workflow 边界  
   5.1 Intake、Baseline Check、Investigation、Diagnosis、Verification Proposal 固定。  
   5.2 阶段内 Tool Selection 与 Hypothesis 更新开放。
6. Context 策略  
   6.1 当前阶段 Context View。  
   6.2 历史事故 RAG 与当前事实冲突处理。  
   6.3 长 Run Compaction。
7. Eval 设计  
   7.1 多根因 Fixture、缺证据、误导历史、过期知识。  
   7.2 比较调查路径质量，而不只看最终答案。
8. 风险与非目标  
   8.1 不自动触发构建、发布或线上操作。  
   8.2 诊断建议必须附验证路径。

#### 8. 贯穿案例

某小游戏版本启动从 15s 回归到 23s 的冻结数据包。

#### 9. 与 DeepSeek Harness 的关系

综合使用 Context、Loop、Session、Tool、Cost 和 Recovery 机制，但只选择必要切片。

#### 10. 与 BuildPilot 的关系

这是判断 BuildPilot 是否值得存在的价值案例，也是未来 M2 设计输入。

#### 11. 需要的证据

匿名化 Startup Trace、构建差异、指标定义、历史事故与反例 Fixture。必须先完成实验 / evidence card。

#### 12. 容易混淆的概念

相关性 ≠ 根因；阶段耗时和 ≠ 总启动时间必然相加；历史相似 ≠ 当前同因；Diagnosis ≠ Verification。

#### 13. 本篇明确不讲什么

不写完整性能优化教程，不宣称已解决真实线上回归，不接生产凭证。

#### 14. 学习检查

- 资源阶段多 8 秒，是否已经证明资源是根因？
- 历史事故与当前版本相似，但构建合同已变化，应怎样处理？
- 最终根因未知但成功排除四个假设，这次 Run 是否有价值？如何评测？

### 32｜从问题空间推导 BuildPilot Architecture

#### 1. 本篇定位

毕业设计架构篇。以 29-31 的需求和案例为输入，推导 BuildPilot 分层，不从 DSH 类图复制。

#### 2. 为什么现在学它

- 上一节：已有一个小 Fixture 和一个复杂调查案例。
- 问题：现在才具备足够证据判断哪些模块真的需要。
- 本节：形成 BuildPilot Architecture v1 与 Decision Records。

#### 3. 学完以后应该能回答什么

- BuildPilot 的 Host、Harness、Runtime 和 Capability 怎样切层？
- 哪些状态属于 Session，哪些属于领域调查？
- 为什么设计 v1 默认模块化单体？
- 哪些 seam 现在就需要，哪些延后？
- DSH 哪些设计采用、简化或拒绝？

#### 4. 前置知识

06 系统总图；19 Harness；28 DSH 取舍；29-31。

#### 5. 核心概念

Architecture Driver、Module Boundary、Host、Harness、Runtime、Context Service、Capability Provider、Domain Model、ADR、Deferred Decision。

#### 6. 核心心智模型

~~~text
Problem / Risk / Evidence
→ Architecture Driver
→ Module + Contract
→ Alternative
→ Decision + Consequence
~~~

#### 7. 正文框架

1. 架构驱动因素  
   1.1 只读调查、多源事实、长 Run、可审计、渐进接入。  
   1.2 非目标：通用 Coding Agent、自动修复、多租户平台。
2. 顶层分层  
   2.1 CLI / future Hosts。  
   2.2 Harness：Policy、Session、Budget、Trace、Recovery。  
   2.3 Runtime：Adapter、Loop、Tool Registry、Structured Output。  
   2.4 Context / Capability / Domain。
3. 关键数据流  
   3.1 Goal 到 Context Request。  
   3.2 Tool Result 到 Evidence。  
   3.3 Hypothesis 到 Diagnosis 与 Verification Proposal。
4. 关键控制流  
   4.1 Policy / Approval / Cancellation / Budget。  
   4.2 Workflow Stage 与 Agent Decision Point。
5. DSH 取舍表  
   5.1 采用：显式 seam、事件、Policy、Trace。  
   5.2 简化：Profile / Provider。  
   5.3 延后：通用 Plugin Kernel、多 Host、Subagent。
6. 部署与信任边界  
   6.1 本地 CLI、只读 API、凭证不进入模型。  
   6.2 Source、CI、Runtime 当前事实与 KB。
7. 架构风险  
   7.1 过度抽象、Context Bloat、证据污染和 Fixture 过拟合。  
   7.2 用 ADR 记录 Deferred Decision。

#### 8. 贯穿案例

让 Compile 与 Startup 两个 Case 分别穿过同一架构，找出共享与专用模块。

#### 9. 与 DeepSeek Harness 的关系

逐项引用 21-28 的设计证据，同时明确没有采用的原因。

#### 10. 与 BuildPilot 的关系

本篇产出 BuildPilot Architecture v1 总图、模块责任和 ADR 清单。

#### 11. 需要的证据

Problem Matrix、两个 Case 数据流、DSH 证据矩阵、架构评审。设计篇也必须有可追溯输入。

#### 12. 容易混淆的概念

模块边界 ≠ 进程边界；Provider seam ≠ 必须插件化；Session State ≠ Domain Truth。

#### 13. 本篇明确不讲什么

不生成项目脚手架，不决定全部类名，不承诺未来多 Agent。

#### 14. 学习检查

- 只有一个 CLI Host 时，为什么仍可能保留 IModelAdapter，但不需要 Host 插件系统？
- Evidence Store 应由 UI 持有吗？
- 哪个真实需求能证明现在就需要 Subagent Provider？

### 33｜BuildPilot 的 Context、Knowledge、Tool、Skill 与 Workflow

#### 1. 本篇定位

毕业设计能力篇。完整设计信息进入、行动能力和调查路径，并把团队知识飞轮留在相邻系列。

#### 2. 为什么现在学它

- 上一节：架构层次已经确定。
- 问题：还需要为两类 Case 定义可执行的 Context、Capability 和 Workflow 合同。
- 本节：把 04、09-12 的通用机制回收到 BuildPilot。

#### 3. 学完以后应该能回答什么

- 四类 Context 分别来自哪里、何时刷新？
- Tool、Skill、Workflow 各有哪些首批对象？
- 历史知识怎样检索、引用和处理过期冲突？
- Knowledge Candidate 怎样进入团队飞轮而不自动成为事实？
- Compile 与 Startup 为什么使用不同 Capability Set？

#### 4. 前置知识

04、09-12；31-32。

#### 5. 核心概念

Project Context、Runtime Context、Investigation Context、Historical Context、Context Receipt、Knowledge Card、Capability Set、Diagnostic Skill、Investigation Workflow。

#### 6. 核心心智模型

~~~text
Current Facts + Task State + Domain Method + Retrieved History
→ scoped Context View
→ Tool / Workflow action
→ Evidence
→ Candidate feedback（进入审核，不自动升格）
~~~

#### 7. 正文框架

1. Context Sources  
   1.1 Project：版本、仓库、构建合同。  
   1.2 Runtime：当前 Job、指标、环境。  
   1.3 Investigation：Evidence、Hypothesis、未完成动作。  
   1.4 Historical：事故与决策，带新鲜度和引用。
2. Context Assembly  
   2.1 按 Stage 构建 View。  
   2.2 Context Receipt 记录来源、冲突、未知和预算。
3. Tool Catalog  
   3.1 Compile v1：parseUnityLog、readFile、searchLiteral。  
   3.2 Startup design：queryMetrics、readBuildManifest、getGitDiff、searchIncident。  
   3.3 每只 Tool 的 Schema、Policy、Evidence Normalizer。
4. Skill Catalog  
   4.1 Unity Compile Diagnosis。  
   4.2 Startup Stage Investigation。  
   4.3 触发、版本、测试和过期。
5. Workflow  
   5.1 Compile 的短路径。  
   5.2 Startup 的 Hypothesis-driven 路径。  
   5.3 Human Gate 与异步验证。
6. Knowledge 接口  
   6.1 RAG 检索与主动注入两种模式。  
   6.2 任务输出只生成 Candidate Knowledge Card。  
   6.3 准入、消费记账、保鲜和退场交给 AI 赋能 12 知识飞轮。
7. Capability Profiles  
   7.1 Compile Fixture Profile。  
   7.2 Startup Read-only Profile。  
   7.3 Write-enabled Profile 明确 Deferred。

#### 8. 贯穿案例

同一条历史“AssemblyLoad 变慢”事故在旧版本和当前版本中的冲突处理。

#### 9. 与 DeepSeek Harness 的关系

借鉴 Context Contributor、Provider、Profile 与 Tool Pipeline；RAG / Skill / Workflow 明确是 BuildPilot 设计，不冒充 DSH 内置事实。

#### 10. 与 BuildPilot 的关系

产出 Context Contract、Tool Catalog、Skill Catalog、Workflow State 和 Knowledge Interface。

#### 11. 需要的证据

Tool Schema、Context Fixture、检索实验、Skill 触发 Eval、Workflow 时序图。Startup Tool 只能先做设计 evidence。

#### 12. 容易混淆的概念

Historical Context ≠ Current Fact；Skill ≠ KB；Tool Result ≠ Evidence；Candidate Knowledge ≠ Published Knowledge。

#### 13. 本篇明确不讲什么

不实现企业知识平台，不自动发布 Skill / KB，不开放写 Tool。

#### 14. 学习检查

- 当前构建清单与历史事故冲突时，Context View 应怎样排序和标记？
- Startup Skill 包含指标查询代码，应拆成什么？
- 一次成功诊断是否自动进入 Knowledge Base？还缺哪些门禁？

### 34｜BuildPilot 的 Policy、Session、Trace、Budget、Recovery 与 Eval

#### 1. 本篇定位

毕业设计治理篇。把 BuildPilot 从“会调查”变成边界明确、可恢复、可评测的设计。

#### 2. 为什么现在学它

- 上一节：能力与 Context 已经完整。
- 问题：没有统一控制面，专用 Agent 仍会越权、失控、不可解释。
- 本节：完成 Harness Policy 和质量证明设计。

#### 3. 学完以后应该能回答什么

- BuildPilot 的最小 Policy 集是什么？
- Session Event 与 Domain Event 怎样关联？
- Cancel / Resume 的恢复边界在哪里？
- Budget 怎样随 Case 和 Profile 变化？
- Eval 怎样区分 Fixture、Shadow、Unity / Jenkins 与生产接受？

#### 4. 前置知识

14-17；25-27；32-33。

#### 5. 核心概念

Policy Set、Approval Record、Session Event、Trace Correlation、Budget Profile、Recovery Boundary、Eval Suite、Shadow Run、Acceptance Gate。

#### 6. 核心心智模型

~~~text
Capability Request
→ Policy / Approval / Budget
→ Traced Execution
→ Durable Session + Domain Evidence
→ Recover or Stop
→ Eval at the correct evidence level
~~~

#### 7. 正文框架

1. Policy Set  
   1.1 Tool、路径、网络、凭证、风险动作。  
   1.2 Read-only 默认和未来升级门槛。
2. Session / Trace  
   2.1 Run、Turn、Step、Tool、Policy、Evidence ID 关联。  
   2.2 JSONL 设计与敏感数据处理。
3. Budget  
   3.1 Compile 与 Startup 的 Step / Token / 时间 / Tool Budget。  
   3.2 Budget Exhausted 的 partial result。
4. Cancellation / Recovery  
   4.1 取消传播、最后安全点、未完成 Tool。  
   4.2 Compile 可以重启，Startup 需要 checkpoint / Resume 的原因。
5. Eval Suite  
   5.1 Schema / Policy tests。  
   5.2 Compile Golden Fixture。  
   5.3 Startup multi-hypothesis benchmark。  
   5.4 Shadow read-only run。  
   5.5 Unity / Jenkins 独立验证渠道。
6. 指标  
   6.1 根因准确、Evidence 引用、越权、成本、延迟、Tool 次数、返工点。  
   6.2 调查路径质量和未知表达。
7. 权限升级 Gate  
   7.1 什么证据支持从 Fixture 到 Jenkins 只读。  
   7.2 什么证据仍不足以开放写入和部署。

#### 8. 贯穿案例

Compile Case 越界 Fixture 与 Startup Case 预算耗尽 / Resume Trace。

#### 9. 与 DeepSeek Harness 的关系

借鉴 Session、Policy、Usage、Trace 和取消 seam；不声称达到 DSH 全部恢复能力。

#### 10. 与 BuildPilot 的关系

产出 Harness Policy v1、Session / Trace Schema、Budget Profiles、Eval Plan 和权限升级表。

#### 11. 需要的证据

负例 Fixture、预算 Trace、取消 / Resume 实验设计、Eval 数据集规范。必须先设计可验证样本。

#### 12. 容易混淆的概念

Eval Pass ≠ Production Ready；Trace ≠ Verification；Cancel ≠ Rollback；只读 ≠ 可无审批访问所有数据。

#### 13. 本篇明确不讲什么

不接真实生产写权限，不宣称 Recovery 已实现，不承诺准确率阈值。

#### 14. 学习检查

- Offline Fixture 全通过，是否足以接 Jenkins 凭证？
- Startup Run 预算耗尽但已定位异常阶段，结果应该怎样表达？
- Trace 含完整源码和 Token，审计价值与数据风险怎样权衡？

### 35｜BuildPilot Design v1：毕业设计评审与未来实现 Roadmap

#### 1. 本篇定位

课程毕业设计篇。汇总所有设计产物，进行反向追踪、自检和分阶段实现规划；仍不把设计称为 Runtime v1。

#### 2. 为什么现在学它

- 上一节：Problem、Architecture、Capability、Policy 与 Eval 已分别设计。
- 问题：这些文档必须组成一个内部一致、可评审、可执行的 Design Baseline。
- 本节：完成课程闭环，并定义课程结束后才开始的实现阶段。

#### 3. 学完以后应该能回答什么

- BuildPilot Design v1 应包含哪些交付物？
- 每个模块能追溯到哪个问题和案例吗？
- 哪些设计已验证、哪些只是 Target？
- 最小实现顺序怎样避免先搭大框架？
- 何时应该停止或缩减 BuildPilot？

#### 4. 前置知识

全课程，尤其是 29-34。

#### 5. 核心概念

Design Baseline、Requirement Traceability、Current / Target / Pilot / Unverified、ADR、Milestone、Exit Criteria、Implementation Roadmap。

#### 6. 核心心智模型

~~~text
Problem → Requirement → Architecture / Contract
→ Evidence / Eval Plan → Milestone
→ Exit Criteria → Implement later
~~~

#### 7. 正文框架

1. Design v1 交付清单  
   1.1 Problem Statement、Audience、Non-goals。  
   1.2 Architecture、Data Contracts、Context、Tool、Skill、Workflow。  
   1.3 Policy、Session、Trace、Budget、Recovery、Eval。  
   1.4 ADR、Risk、Evidence Matrix 和 Roadmap。
2. 需求追踪  
   2.1 每个模块回指 Compile / Startup Case 和风险。  
   2.2 删除“没有问题来源”的架构部件。
3. 状态标注  
   3.1 Current：已有外部事实。  
   3.2 Target：目标设计。  
   3.3 Pilot：受限验证。  
   3.4 Unverified：尚无运行证据。  
   3.5 Rejected / Deferred：明确不做或后做。
4. 设计评审  
   4.1 术语一致性、责任边界、证据诚实性。  
   4.2 安全、成本、恢复和可评测性。  
   4.3 DSH 借鉴项是否有必要性。
5. 实现 Roadmap  
   5.1 M0：Schema / Fixture / deterministic parser。  
   5.2 M1：只读 Compile Agent Loop。  
   5.3 M2：Context / Incident Retrieval / Startup design slice。  
   5.4 M3：Jenkins 只读 Shadow。  
   5.5 任何写权限都是后续独立决策。
6. Exit Criteria  
   6.1 Design v1 完整不等于必须实施全部。  
   6.2 如果确定性方案更优、数据不可得或 Eval 不成立，允许停止。
7. 课程收束  
   7.1 回看 Agent、Harness、Context 和 Evidence 四条主线。  
   7.2 正式开发从下一阶段开始，不在本文伪装完成。

#### 8. 贯穿案例

用 Compile / Startup 双案例执行一次完整设计评审。

#### 9. 与 DeepSeek Harness 的关系

附 DSH Adopt / Simplify / Reject / Defer 矩阵，每项带 pinned evidence。

#### 10. 与 BuildPilot 的关系

本篇就是 BuildPilot Design v1 的验收说明；课程在这里结束。

#### 11. 需要的证据

前序全部 Article Card 产物、ADR、Fixture / Trace 计划、独立架构评审。不能把设计状态写成实现状态。

#### 12. 容易混淆的概念

Design v1 ≠ Runtime v1；Roadmap ≠ 已承诺功能；Pilot ≠ Production；课程完成 ≠ 产品完成。

#### 13. 本篇明确不讲什么

不创建代码仓库，不生成脚手架，不执行 Unity / Jenkins，不发布产品。

#### 14. 学习检查

- 一个模块无法回指任何问题、风险或 Eval，应保留吗？
- Design v1 里哪些结论必须标 Unverified？
- 如果 Compile Case 规则化方案明显更好，BuildPilot 应怎样调整？

---

## 10. DeepSeek Harness 源码 evidence 策略

### 10.1 开写前的快照

必须记录：

- 仓库 URL
- tag / commit
- checkout 日期
- 官方文档入口
- 依赖与运行环境
- 能否构建、能否运行、能否执行测试
- 当前限制与未验证项

### 10.2 每篇 evidence card 的最小字段

| 字段 | 含义 |
|---|---|
| Question | 本篇从课程带入的工程问题 |
| Official Fact | 官方文档明确写了什么 |
| Source Fact | pinned commit 的文件、符号、数据结构与调用路径 |
| Runtime Observation | 实际运行、测试或 Trace 看到了什么 |
| Inference | 根据事实做出的解释 |
| Alternative | 可替代设计 |
| Benefit / Cost | 当前设计的收益与代价 |
| BuildPilot Decision | Adopt / Simplify / Reject / Defer |
| Unverified | 还没有证据的部分 |

### 10.3 事实书写规则

- 只用官方文档描述官方目标。
- 用文件路径、关键符号和 commit 描述源码事实。
- “为什么这样设计”如果官方未说明，必须标为本文推断。
- RAG、Skill、Workflow 等能力如果只是可由插件实现，必须标为扩展映射。
- Developer Preview 的实现必须标版本，不写成行业标准。
- 能静态看到但不能运行的机制只能称 Source Confirmed，不能称 Runtime Verified。

### 10.4 验证层级

~~~text
文档存在
→ 源码符号存在
→ 调用路径成立
→ 测试 / 最小运行观察
→ Trace 可重建
→ 文章结论
~~~

## 11. BuildPilot 在课程中的角色

BuildPilot 只承担三种角色：

1. 前半课程的轻量设计参照：帮助解释对象，但不持续编码。
2. Part IV 的两个设计案例：Compile 是 Golden Fixture，Startup 是 Agent 价值测试。
3. 课程毕业设计：交付 BuildPilot Design v1。

BuildPilot 不承担：

- 不作为前 28 篇的编码主线
- 不要求每学一个概念就实现一个模块
- 不在课程中完成 Runtime v1
- 不用来证明所有游戏开发任务都适合 Agent

## 12. BuildPilot Design v1 最终应交付什么

1. Problem Statement 与 Non-goals
2. 游戏研发任务 Agent 化判断矩阵
3. Compile / Startup 两个 Case Pack
4. Architecture Context、Container / Module 与数据流图
5. Evidence / Hypothesis / Diagnosis / Action / Verification 合同
6. Context Source 与 Context Receipt 合同
7. Tool Catalog 与 Tool Policy
8. Skill Catalog 与版本 / Eval 规则
9. Workflow State Machine
10. Session Event 与 Trace Schema
11. Permission / Approval / Sandbox 设计
12. Token / Step / Tool / Time Budget Profile
13. Cancellation / Recovery 边界
14. Eval Suite 与证据渠道矩阵
15. DSH Adopt / Simplify / Reject / Defer 矩阵
16. ADR、风险登记和实现 Roadmap

## 13. 与现有系列的边界

### 13.1 AI 赋能游戏开发

它负责使用者和团队实践视角：

- 团队知识闭环
- CLAUDE.md 与领域 Skill
- 跨层开发工作流
- Harness v0 实践
- Skill 自动沉淀与 kb/ / LLM Wiki

本课程只在建立 Agent Engineering 抽象时交叉引用，不整篇复述。

知识飞轮深化篇仍建议归入 AI 赋能系列。课程 04 / 33 只讲 Agent 的知识消费接口：

~~~text
KB / RAG / Context / Usage Receipt
~~~

完整的生产、准入、消费记账、保鲜、退场和组织运营不由本课程吞并。

### 13.2 Harness Engineering

它负责 v0 之后的演化治理：

- Bootstrap / Growth / Bloat / Drift / Sunset
- 游戏客户端约束
- 跨仓库作用域
- 交付 / 运营联动
- 真实复盘与指标

本课程负责 Harness 为什么存在、核心机制怎样设计，以及怎样借 DSH 源码验证。课程不重写生命周期系列。

### 13.3 交付工程、长线运营和问题解决

这些系列提供 BuildPilot 的问题空间和真实案例。本课程只讨论 Agent 与既有工具链的接口，不重讲它们的主线。

## 14. 推荐写作顺序

阅读顺序必须按 01 → 35；写作顺序可以先立骨架和证据：

~~~text
第一批基础骨架：
01 → 02 → 03 → 04 → 05 → 06

第二批运行主链：
07 → 08 → 09 → 10 → 12 → 13 → 14 → 17 → 19

第三批补齐机制：
11 → 15 → 16 → 18

第四批源码教材：
先做 20 evidence baseline
再写 21 → 23 → 24 → 25 → 26
最后写 22 → 27 → 28

第五批毕业设计：
29 → 30 → 31 → 32 → 33 → 34 → 35
~~~

## 15. 首批最适合开始写的文章

首批建议 10 篇，形成“共同语言 → 运行主链 → Harness”的最小学习闭环：

1. 01 从一次模型调用到 Agent
2. 02 Tool、Skill、Workflow
3. 03 Prompt 与 Context
4. 04 RAG、KB、Session、Memory
5. 05 哪些问题不该 Agent 化
6. 07 Model I/O 与 Structured Output
7. 08 Agent Loop
8. 09 Tool Engineering
9. 10 Context Engineering
10. 19 为什么需要 Harness

说明：

- 这 10 篇可以先建立课程主体。
- 19 初稿可以先写抽象版，但发布前应补齐 11-18 的反向引用。
- DSH 源码篇必须等 20 的 pinned evidence baseline。
- BuildPilot 设计篇必须等 29 的真实问题空间资料。

## 16. 哪些文章必须先做实验 / Evidence Card

| 篇 | 前置证据 |
|---|---|
| 07 | Structured Output / Tool Calling Schema Fixture |
| 09 | Tool 越权、超时、取消、大结果 Fixture |
| 10 | 全量 / 截断 / 阶段化 Context A/B |
| 11 | Skill 未加载 / 正确加载 / 错误触发 Eval |
| 13 | Evidence Contract Schema 与负例 |
| 14 | 路径、Tool、Approval、Sandbox 负例 |
| 15 | Retry / Timeout / Cancellation Trace |
| 16 | Usage Ledger 与固定日期价格 |
| 17 | 至少四条可重放 Agent Trace |
| 18 | 单 / 多 Agent 对照 Trace |
| 20-28 | 每篇 pinned DSH source evidence card；涉及行为时加运行验证 |
| 29 | 真实生产管线与现有工具矩阵 |
| 30 | 隔离 Compile Golden Fixture |
| 31 | 匿名化 Startup Case Pack |
| 32-35 | 前序 Case、ADR 与 Eval 设计输入 |

## 17. 当前仍需要作者拍板的问题

1. 正式系列名使用“Agent Engineering”，还是保留“游戏 Agent 工程”作为副标题？
2. 35 篇作为完整课程地图，首期是否只发布 01-10 中的最小闭环？
3. DeepSeek Harness 锁定哪个 tag / commit，是否能够在本地完成最小运行？
4. DSH 文章放在同一系列 Part III，还是发布层做子系列但保留统一课程编号？
5. BuildPilot 的 Startup Case 能公开哪些真实指标、日志和构建差异？
6. AI 赋能 12 知识飞轮何时进入 canonical plan，课程只保留交叉引用是否确认？
7. BuildPilot Design v1 的目标是求职 Demo 设计、团队内部原型，还是两者兼顾？两者证据标准不同。
8. 第一阶段是否坚持完全无 Shell、无写入、无自动 Unity / Jenkins 触发？

## 18. 完整课程路线图

| 学习层 | 文章 | DeepSeek Harness 回收 | BuildPilot 回收 |
|---|---|---|---|
| Agent 基本对象 | 01-02 | 20-22、26、28 | 29、32 |
| Prompt / Context | 03、10 | 23、27 | 33 |
| RAG / KB / Session / Memory | 04 | 25、28 | 33-34 |
| 专用化与非 Agent 边界 | 05 | 22、28 | 29-31 |
| Runtime / Harness 分层 | 06、19 | 20-22 | 32 |
| Structured I/O / Loop | 07-08 | 24 | 30-32 |
| Tool / Skill / Workflow | 09、11-12 | 26、28 | 33 |
| Evidence Contract | 13 | 25、27 | 30-35 |
| Policy / Recovery | 14-15 | 26-27 | 34 |
| Cost / Trace / Eval | 16-17 | 27 | 34-35 |
| Multi-Agent | 18 | 22、28 | 默认 Defer |
| DSH 综合取舍 | 20-28 | 本阶段 | 32-35 |
| Problem / Cases | 29-31 | 只引用已验证设计 | 32-35 |
| Architecture / Governance | 32-34 | Adopt / Simplify / Reject | 35 |
| Design v1 | 35 | 证据矩阵 | 课程最终交付 |

总路线：

~~~text
基础术语 01-06
  ↓
Model I/O / Loop / Tool / Context 07-10
  ↓
Skill / Workflow / Evidence 11-13
  ↓
Policy / Recovery / Cost / Trace / Multi-Agent 14-18
  ↓
Harness 19
  ↓
DeepSeek Harness 20-28
  ↓
BuildPilot Problem / Cases 29-31
  ↓
BuildPilot Architecture / Capability / Governance 32-34
  ↓
BuildPilot Design v1 35
  ↓
课程结束，未来实现阶段另行立项
~~~

## 19. 课程设计自检

1. 零散使用过 Agent 产品的 C# / Unity 工程师可以从 01 顺序进入。
2. Function Calling、Tool、Prompt、Context 等概念都在运行机制使用前建立。
3. DeepSeek Harness 从 20 开始，严格依赖 01-19 的抽象。
4. DSH 每篇都从课程问题进入，不按目录做源码考古。
5. BuildPilot 在 01-28 只作轻量参照，从 29 才成为毕业设计主体。
6. BuildPilot Design v1 覆盖问题、架构、合同、治理、Eval 和 Roadmap。
7. 05 与 29 两次明确回答哪些问题不用 Agent。
8. Tool、Skill、Workflow、Agent、Harness 的职责在四阶段保持一致。
9. Prompt、Context、RAG、Session、Memory 始终按来源与生命周期分开。
10. 每篇 Article Card 都包含正文框架、证据要求、Scope 和学习检查。

## 20. 最短结论

先学会用统一抽象解释 Agent 怎样运行，再用锁定源码检验抽象，最后从真实游戏研发问题反推 BuildPilot；课程交付的是独立设计能力和 Design v1，不是假装已经完成的 Agent 产品。
