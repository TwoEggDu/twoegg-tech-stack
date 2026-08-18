# 游戏 Agent 工程系列提纲（评审稿）

> 角色：新系列立项前的完整提纲 / GPT 审核输入
> 状态：评审稿，非 canonical plan
> 日期：2026-08-17
> 暂定系列名：`游戏 Agent 工程｜从 Prompt、Context 到 DeepSeek Harness`
> 暂定副标题：`以 DeepSeek Harness 为源码样本，用 C# 构建一个 Unity / Jenkins 专用诊断 Agent`

---

## 0. 这份提纲负责什么

这份文档先把系列的定位、概念边界、篇级候选目录、源码证据要求和实作路线完整展开，供外部 GPT 做结构审核。

当前阶段只做提纲评审，不做以下动作：

- 不更新根 `doc-plan.md`
- 不替换现有 `docs/harness-engineering-series-plan.md`
- 不建立 `content/` 正文目录
- 不把本文件声明为 canonical series plan
- 不直接开写正文
- 不承诺 34 篇全部进入首期

审核完成后，再决定：

1. 是否正式独立成系列
2. 是否拆成基础篇、DeepSeek Harness 源码篇和 BuildPilot 实作篇三期
3. 哪些文章合并，哪些文章保留为独立主线
4. 最终 canonical plan 的文件名、栏目目录和 weight 区间

---

## 1. 一句话定位

`这不是一套 AI 工具使用教程，而是一条 Agent 构建者的学习与实作路线：从 Prompt、Context、RAG、Tool、Skill、Workflow 等基础对象出发，读懂 DeepSeek Harness 为什么这样设计，再用 C# 做出一个能解决 Unity / Jenkins 真实生产问题的专用 Agent。`

## 2. 这个系列解决什么问题

当前公开内容常见两种断层。

第一种只讲 AI 编程工具怎么用：

- 怎样写 Prompt
- 怎样配置 Skill
- 怎样连接 MCP
- 怎样让 Coding Agent 修改代码

读完可以使用工具，但无法回答：Agent 内部怎样运行、Tool 为什么需要执行管线、Context 为什么要分层、Session 为什么要可重放、Harness 为什么必须独立于模型。

第二种直接讲 Agent 框架或 Harness 源码：

- Agent Loop
- Plugin System
- Event Bus
- Tool Registry
- Sandbox
- Multi-Agent

读者能看到类和接口，却没有先建立 Prompt Engineering、Context Engineering、RAG、Token Cost、Workflow 等抽象，因此很难判断这些设计在解决什么问题，也很难迁移到自己的 Agent。

本系列要补上中间这条完整链路：

```text
LLM / API
→ Prompt Engineering
→ Context Engineering
→ RAG / Session / Memory
→ Tool / Skill / Workflow
→ Agent Runtime
→ Harness Engineering
→ Token / Cost / Trace / Eval
→ Unity / Jenkins 专用 Agent
→ 现有游戏开发工具链集成
```

## 3. 学完后应该具备什么能力

### 3.1 能解释

- LLM、AI 应用、Agent、Harness 和 AI Native 产品的区别
- Tool、Skill、Workflow、Agent、Harness 的职责边界
- Prompt Engineering、Context Engineering 和 RAG 的包含与协作关系
- Session、Memory、Knowledge Base 和 RAG 的区别
- 通用 Agent 与专用 Agent 的差别
- 确定性 Workflow 与模型自主决策怎样配合
- Token、推理成本、延迟、正确率为什么必须一起优化

### 3.2 能读懂

- DeepSeek Harness 的插件树和启动组合
- Profile、Bundle、Patch 的配置叠层
- System Prompt Assembly
- Turn、Step、Inbox 和 Agent Loop
- Append-only Session Event Log
- Tool Registry 与执行管线
- Capability Seam 与 Provider 替换
- Approval、Sandbox、Cancellation、Recovery、Trace 等工程机制

### 3.3 能实现

- 用 C#/.NET 接入一个支持 Tool Calls 的模型 API
- 实现最小 Agent Loop
- 定义结构化 Tool 与 Tool Result
- 实现项目 Context、Skill、Workflow 与 RAG
- 实现路径白名单、工具白名单、人工审批和预算控制
- 用 Fixture、Trace 和 Eval 验证一个专用 Agent
- 把 Agent 逐步接入 Unity / Jenkins 现有工具链

### 3.4 能判断

- 哪些任务值得 Agent 化，哪些只需要确定性脚本
- 哪些信息应该进入 Prompt、Context、RAG、Memory 或项目文档
- 哪些 Tool 可以自动执行，哪些必须审批
- 什么时候应该做单 Agent，什么时候才需要多 Agent
- 什么时候应该扩展 Harness，什么时候继续加规则只会制造 Bloat

---

## 4. 目标读者

- 有 Unity / C# 工程经验，开始系统学习 Agent Engineering 的游戏开发者
- 深度使用 Claude Code、Codex、Cursor、DeepSeek Harness 等工具，但还没有自己实现过 Agent Runtime 的工程师
- 想为游戏研发、构建、交付、运营或排障开发专用 Agent 的客户端 / 工具链工程师
- 希望把 AI 开发能力转化为可验证作品，而不是停留在“会用聊天工具”的资深工程师

本系列不要求读者一开始熟悉 Agent 框架，但默认具备基本的软件工程、HTTP API、JSON、异步编程和 C# 项目经验。

---

## 5. 核心概念图

```text
User Goal
   │
   ▼
Application / Product
   │
   ▼
Harness
├── Context / Session / Memory
├── Permission / Sandbox / Approval
├── Budget / Trace / Eval / Recovery
│
└── Agent Runtime
    ├── Model Adapter
    ├── Agent Loop
    ├── Tool Registry
    ├── Skill Loader
    └── Workflow State
         │
         └── RAG / Project Knowledge / External Systems
```

### 5.1 六个核心对象的最短定义

| 对象 | 最短定义 | 主要回答的问题 |
|---|---|---|
| Model | 根据当前输入生成下一步输出 | 下一步建议做什么 |
| Tool | Agent 可以调用的外部能力 | 能读取或改变什么 |
| Skill | 可按需加载的领域知识与操作方法 | 这类任务通常应该怎样做 |
| Workflow | 阶段、顺序、停止点与状态转移 | 任务按什么路径推进 |
| Agent | 使用模型在循环中选择下一步行动的执行者 | 当前应该做什么 |
| Harness | 承载并约束 Agent 运行的工程系统 | 在什么边界内运行、怎样记录和验证 |

### 5.2 Prompt、Context、RAG、Memory 的关系

```text
Prompt Engineering
└── 设计当前指令怎样表达

Context Engineering
├── 选择模型本轮看什么
├── 决定顺序、角色、作用域和生命周期
├── 控制历史、Tool Schema、Tool Result 与任务状态
└── 接收 RAG / Memory / Project Context 的结果

RAG
└── 从外部知识中检索当前需要的证据或背景，再注入 Context

Memory
└── 跨 Turn 或跨 Session 保留可复用状态与经验
```

RAG 是 Context Engineering 的一种信息获取机制，不等于 Context Engineering，也不等于 Memory。

---

## 6. 系列边界

### 6.1 属于本系列

- Agent Engineering 的基本对象与运行链路
- Prompt、Context、RAG、Session、Memory 的工程边界
- Tool、Skill、Workflow、Agent Runtime 与 Harness 的设计
- Token、推理成本、延迟和正确率的联合优化
- DeepSeek Harness 的公开源码与官方架构文档分析
- C#/.NET 专用 Agent 的最小实现
- Unity / Jenkins 只读诊断场景
- Trace、Eval、Approval、Sandbox、Recovery 等生产化机制
- Agent 与现有 Unity、Jenkins、CLI、Web、CI 工具链的连接点

### 6.2 不属于本系列

- 从零教学 C#、C++、HTTP、JSON 或软件设计模式
- 完整讲解 LangChain、AutoGen、OpenAI Agents SDK 等每个框架的全部 API
- 大模型训练、SFT、RLHF、推理引擎和 GPU 部署全栈
- 完整向量数据库选型教程
- 通用搜索引擎、数据库或分布式系统教程
- 把岗位截图中的每条通用能力都扩写成独立基础课
- 在没有真实数据前宣称效率提升百分比
- 在第一阶段开放自动提交、自动部署或生产写权限

### 6.3 只覆盖交叉点

- C# / C++：只覆盖 Agent 实现和 Unity 工具链接入所需部分
- 前后端：只覆盖 Agent Service、Web UI、Jenkins API 和 SaaS 接口的连接点
- 设计模式：只在 Plugin、Adapter、Provider、State Machine、Event Sourcing 等真实设计中解释
- RAG：讲清完整最小链路，但不扩成一套独立搜索工程系列
- 团队知识飞轮：本系列讲知识怎样被 Agent 检索、注入和消费；完整的生产、准入、保鲜、退场与组织运营仍由 `AI 赋能游戏开发` 系列承担
- 多 Agent：讲职责拆分和编排边界，不以 Agent 数量作为复杂度目标

---

## 7. 与现有系列的关系

### 7.1 `AI 赋能游戏开发`

现有系列站在使用者和实践者角度，讲 CLAUDE.md、Skill、跨层工作流和 Harness v0。

本系列站在构建者角度，深入：

- Agent Runtime 怎样实现
- Skill 怎样进入 Context
- Tool 怎样经过执行管线
- Session 怎样持久化和重放
- Harness 怎样管理权限、预算、调试和恢复

处理原则：已有文章作为实践入口和交叉引用，不在本系列整篇复述。

#### 知识飞轮深化篇的归属建议

现有 01-04 已经建立了第一版知识闭环：

```text
提问暴露缺口
→ 生产结构化知识
→ Wiki 沉淀
→ RAG 重新可答
```

这条链解决了“知识怎样从无到有”，但还没有完整回答 Agent 成为主要消费者之后的第二阶段问题：

- 什么候选知识可以进入正式引用池
- 怎样把相关知识主动送到正确的 Agent，而不是等模型碰运气搜索
- 每次注入和消费怎样记账，怎样知道知识是否真正有用
- 过期、冲突、低价值知识怎样退场，误删后怎样恢复
- 人、Agent、源码、CI、构建产物和运行系统之间，谁才是当前事实的权威来源

建议把它作为 `AI 赋能游戏开发` 的一个独立深化篇，而不是塞进本系列 07 RAG、08 Memory 或 12 Skill：

**候选标题：AI 赋能 12｜Agent 知识飞轮：从“知识库”到会供给、会记账、会代谢的系统**

- 类型：原理 + 工程闭环篇
- 核心问题：为什么 Agent 时代需要的不是“存得更多”的知识仓库，而是可持续供给、可追踪消费、可治理生命周期的知识系统
- 抽象主链：`Task / Source → Candidate → Curate / Gate → Publish → Route / Retrieve → Inject → Consume / Trace → Feedback → Refresh / Retire / Revive`
- 飞轮短链：`生产 → 提炼 → 注入 → 消费 → 反馈 → 保鲜`
- 三个核心合同：
  - `Knowledge Card`：适用范围、结论、证据、来源、Owner、可信度、有效期
  - `Knowledge Package`：按项目、任务、Agent 和权限组合可消费知识
  - `Context Receipt / Usage Ledger`：本次注入了什么、为什么注入、被谁使用、是否采纳、结果怎样
- 必须切清的边界：
  - RAG 负责找，Context Engineering 负责选、排、装配和压缩
  - Memory 保存跨运行经验或状态，不自动成为团队事实
  - Skill 保存“这类任务怎样做”，Knowledge Base 保存可追溯的长期事实与决策
  - Harness 负责路由、门禁、注入、Trace 和预算，不应把未经验证的任务输出直接提升为权威知识
  - 源码、CI、构建产物和运行系统保存当前事实；Wiki / KB 保存经过审核的持久知识
- 治理机制：客观准入信号、隔离区、人工复核、可信度分级、有效期、新鲜度检查、冲突报告、退场与恢复
- 观测指标：检索命中与误召、注入采纳率、引用正确率、缺口关闭率、重复失败下降率、过期冲突率、单次有效知识供给成本
- BuildPilot 验证：把一次已确认的 Unity / Jenkins 故障沉淀成候选卡；验证后转正；在新版本使它过期时触发冲突或退场；用 Trace 证明它在下一次相似诊断中被正确注入和引用
- 与现有文章的分工：
  - 01 负责建立团队知识闭环总图
  - 04 负责知识缺口发现与回流
  - 10 负责单次任务怎样沉淀为 Skill / Memory 候选
  - 11 负责长期知识的目录与 Schema
  - 本候选篇负责知识从候选到消费再到代谢的完整生命周期
- 证据边界：外部案例中的团队规模、效率数字、默认有效期和自动化比例只能作为待验证参考，不能改写成本站实践数据

### 7.2 `Harness Engineering`

现有 `Harness Engineering` 从 v0 之后开始，主线是 Growth、Bloat、Drift、Sunset、跨仓库和交付 / 运营联动。

本系列是它的上游：

```text
本系列：Agent / Harness 是什么、怎样设计、怎样实现
    ↓
现有 Harness Engineering：系统跑起来后怎样演化和治理
```

处理原则：现有 canonical plan 保留，不把基础概念硬塞进去。

### 7.3 `交付工程` 与 `长线运营工程`

这两个系列提供 Agent 要进入的真实问题空间。本系列只写 Agent 与它们的接口，不重讲完整交付或运营体系。

---

## 8. 贯穿全系列的实作项目

暂定项目名：`BuildPilot`。

目标不是做另一个通用 Coding Agent，而是做一个 Unity / Jenkins 只读诊断 Agent。

### 8.1 第一阶段黄金路径

```text
输入：一份 Unity C# 编译失败日志 + 隔离的项目 Fixture
→ 解析失败阶段和编译错误
→ 读取白名单范围内的相关文件
→ 搜索字面引用
→ 形成 Evidence
→ 提出 Hypothesis
→ 输出 DiagnosisResult
→ 明确 confidence 与 unverifiedScope
```

### 8.2 第一批 Tool

- `parseUnityLog`
- `readFile`
- `searchLiteral`

### 8.3 第一阶段硬边界

- 不开放 Shell
- 不写源文件
- 不访问白名单外路径
- 不跟随 reparse point / junction 越界
- 不自动触发 Unity 或 Jenkins
- 不把参考答案 Fixture 冒充模型独立诊断
- 不把“建议验证”写成“已经验证”

### 8.4 核心数据合同

```text
Evidence
→ Hypothesis
→ DiagnosisResult
→ ProposedAction
→ VerificationResult
```

其中：

- Evidence 是不可变事实
- Hypothesis 必须引用 Evidence ID
- DiagnosisResult 必须标注 confidence
- VerificationResult 必须标注实际运行渠道和未验证范围

---

## 9. 每篇文章的统一写法

除索引篇外，每篇默认回答七个问题：

1. 它在解决什么工程问题
2. 常见理解为什么不够
3. 最小抽象模型是什么
4. DeepSeek Harness 或其他框架怎样实现
5. 为什么选择这种设计，替代方案是什么
6. 这种设计的收益、代价和适用边界是什么
7. BuildPilot 怎样实现一个最小切片并验证

DeepSeek Harness 源码篇额外要求：

- 固定 tag 或 commit，不以漂移中的 `master` 作为无版本事实
- 区分官方文档事实、源码事实、本文推断和我们的目标设计
- 每篇至少提供一条可执行或可观察的验证路径
- 不把插件可实现的能力写成 DSH 已内置的核心能力
- 不把 Developer Preview 的当前设计写成稳定标准

---

## 10. 候选文章目录

> 当前为 34 篇全景候选。GPT 审核时应重点判断粒度、重复和首期范围，不默认全部同时开写。

### 第一部分：AI 编程系统的基本对象（01-04）

#### 01｜LLM、AI 应用、Agent 与 Harness 到底差在哪

- 类型：原理篇
- 核心问题：一次模型调用在什么条件下才变成 Agent，Agent 又在什么条件下需要 Harness
- 抽象模型：Model Request → Tool-using Application → Agent Loop → Harnessed Agent
- 必须回答：目标、状态、行动、反馈、停止条件分别由谁负责
- BuildPilot 落地：对比普通日志问答、一次 Tool Call 和多 Step 诊断
- 不展开：具体框架 API

#### 02｜Tool、Skill、Workflow、Agent、Harness 的关系

- 类型：原理篇
- 核心问题：五个高频术语为什么总被混用
- 抽象模型：能力、知识、路径、决策者、运行环境
- 必须回答：Tool 与 Skill、Workflow 与 Agent、Agent 与 Harness 的边界
- BuildPilot 落地：用同一个 Unity 编译失败任务映射五个对象
- 不展开：Tool Pipeline 和 Skill 格式细节

#### 03｜通用 Agent 与专用 Agent：更窄为什么可能更可靠

- 类型：映射篇
- 核心问题：专用 Agent 是否只是换了一段 System Prompt
- 抽象模型：任务空间、工具面、数据合同、权限、Eval 五维专用化
- 必须回答：通用 Agent 和专用 Agent 的收益、代价、适用场景
- BuildPilot 落地：通用 Coding Agent 与只读诊断 Agent 对照
- 不展开：多 Agent 编排

#### 04｜AI 功能、AI Copilot、AI Agent 与 AI Native 产品

- 类型：原理篇
- 核心问题：给现有工具增加聊天框是否就是 AI Native
- 抽象模型：Feature → Assistant → Agent → AI-native Loop
- 必须回答：AI 怎样进入产品主循环、状态和反馈闭环
- BuildPilot 落地：CLI、桌面端、Jenkins 后台和 Unity Editor 入口的职责
- 不展开：具体 UI 设计

### 第二部分：模型输入与知识工程（05-09）

#### 05｜Prompt Engineering：一次模型调用应该怎样设计

- 类型：原理篇
- 核心问题：Prompt Engineering 解决什么，又解决不了什么
- 抽象模型：目标、约束、输入、示例、输出合同、失败语义
- 必须回答：System/User/Tool 消息、Few-shot、Structured Output、Prompt Injection、Prompt 版本管理
- BuildPilot 落地：诊断输出 Prompt v0 与结构化输出对照
- 验证：固定 Fixture 上做 Prompt A/B
- 不展开：完整 Context 生命周期

#### 06｜Context Engineering：模型每一步到底应该看到什么

- 类型：原理篇
- 核心问题：Context 为什么不是“把所有文件塞进去”
- 抽象模型：选择、排序、作用域、生命周期、压缩、注入
- 必须回答：Prompt、历史、Tool Schema、Tool Result、任务状态、动态环境怎样组成一次 Model Request
- BuildPilot 落地：为不同诊断阶段生成不同 Context View
- 验证：比较全量上下文与按需上下文的准确率、Token 和延迟
- 不展开：RAG 检索算法细节

#### 07｜RAG：检索不是把文档塞进向量库

- 类型：原理篇
- 核心问题：RAG 在 Agent 中真正解决哪类知识缺口
- 抽象模型：Query → Retrieve → Filter / Rerank → Inject → Cite
- 必须回答：Keyword、Vector、Hybrid；Chunk；Top-K；新鲜度；权限；引用
- BuildPilot 落地：检索 Unity/Jenkins 历史事故和构建合同
- 验证：无 RAG、Keyword、Hybrid 三组 Fixture 对照
- 不展开：向量数据库运维全栈

#### 08｜Session、Memory、RAG 与 Knowledge Base 不要混在一起

- 类型：映射篇
- 核心问题：四种“记忆”概念分别保存什么
- 抽象模型：运行事实、跨运行经验、即时检索、长期事实源
- 必须回答：写入时机、读取时机、权威性、过期与冲突处理
- BuildPilot 落地：Session Trace、事故库、项目规则和 RAG 索引分层
- 交叉引用：知识的完整准入、消费记账与代谢机制转到 `AI 赋能 12｜Agent 知识飞轮`
- 不展开：企业知识治理全体系

#### 09｜Token、推理成本与延迟：Agent 为什么比聊天更容易失控

- 类型：原理篇
- 核心问题：Agent 成本为什么不能只看一次 Prompt
- 抽象模型：Input + History + Tool Schema + Tool Result + Output + Reasoning + Retry + Steps + Subagents - Cache
- 必须回答：Token Budget、Context Budget、Step Budget、模型路由、Reasoning Effort、Prefix Cache、压缩、结果溢出
- BuildPilot 落地：记录每次诊断的 Token、现金成本、延迟和 Tool 次数
- 验证：成本下降不能以证据缺失和准确率下降为代价
- 不展开：模型服务端推理优化

### 第三部分：Agent 的执行系统（10-15）

#### 10｜Agent Loop：模型怎样从回答问题变成执行任务

- 类型：原理篇
- 核心问题：Agent 每一步如何选择、行动、观察和停止
- 抽象模型：Receive → Assemble → Decide → Act → Observe → Continue / Stop
- 必须回答：Turn、Step、Tool Call、Tool Result、Stop Condition
- BuildPilot 落地：实现最小多 Step Loop
- 不展开：具体框架封装

#### 11｜Tool Engineering：Tool 不是普通函数包装

- 类型：原理篇
- 核心问题：一个函数暴露给模型后新增了哪些工程风险
- 抽象模型：Schema → Policy → Execute → Validate → Render → Record
- 必须回答：只读、破坏性、幂等、超时、取消、并发、错误、结果裁剪、审批
- BuildPilot 落地：`parseUnityLog`、`readFile`、`searchLiteral`
- 验证：坏参数、越界路径、大结果、取消和超时 Fixture

#### 12｜Skill Engineering：领域知识怎样按需进入 Agent

- 类型：原理篇
- 核心问题：Skill 与 Prompt、Tool、文档和 Workflow 有什么差别
- 抽象模型：Discovery Metadata → Trigger → Instructions → References / Scripts / Assets
- 必须回答：触发、渐进加载、复用、版本、验证、过期和 Bloat
- BuildPilot 落地：Unity C# 编译失败诊断 Skill
- 验证：Skill 未加载、正确加载、错误触发三组对照

#### 13｜Workflow Engineering：确定性骨架与模型决策怎样结合

- 类型：原理篇
- 核心问题：为什么生产 Agent 不能把所有流程都交给模型临场规划
- 抽象模型：Deterministic State Machine + Agent Decision Points
- 必须回答：阶段、停止点、人工接棒、重试、补偿、恢复、同步与异步验证
- BuildPilot 落地：Intake → Evidence → Diagnosis → Review → Verification Proposal
- 不展开：完整 BPM 系统

#### 14｜单 Agent、多 Agent、Agent as Tool 与 Handoff

- 类型：映射篇
- 核心问题：什么时候应该拆 Agent，什么时候拆分只会增加成本和丢失上下文
- 抽象模型：Manager、Agent as Tool、Handoff、Code Orchestration
- 必须回答：控制权、上下文、输出合同、失败传播和成本
- BuildPilot 落地：先保留单 Agent；仅用实验比较“日志解析专员”是否值得拆出
- 不展开：大规模 Agent Swarm

#### 15｜专用 Agent 的数据合同：Evidence 不能和 Hypothesis 混在一起

- 类型：原理篇
- 核心问题：为什么自然语言诊断难以审计和验证
- 抽象模型：Evidence → Hypothesis → Diagnosis → Action → Verification
- 必须回答：证据 ID、来源、置信度、反证、未验证范围
- BuildPilot 落地：定义结构化 DiagnosisResult
- 验证：结论必须能追溯到证据

### 第四部分：DeepSeek Harness 源码主线（16-28）

#### 16｜DeepSeek Harness 总图：它到底解决什么问题

- 类型：索引 / 原理篇
- 核心问题：DSH 是模型 Wrapper、Coding Agent、Agent Runtime，还是完整产品底座
- 抽象模型：Application → Profile / Bundle → Plugin Tree → Agent / Session / Tool / LLM / Policy
- 必须回答：核心包、宿主、Web、Headless、开发者预览状态
- 源码要求：固定 tag 或 commit，记录仓库状态和官方文档入口
- BuildPilot 落地：画出自己的最小对应图

#### 17｜为什么 DeepSeek Harness 选择 Everything is a Plugin

- 类型：原理篇
- 核心问题：为什么 Model Adapter、Tool Registry、Session Log、Agent Loop 都做成插件
- 抽象模型：Cordis Context + Service + Event + Reversible Effect
- 必须回答：可替换、可组合、作用域、卸载，以及调试复杂度和过度设计风险
- 对照方案：普通依赖注入、模块化单体、固定核心 + 扩展点
- BuildPilot 落地：只提炼 Adapter / Provider 边界，不照搬完整 Cordis

#### 18｜Profile、Bundle 与配置叠层：通用 Harness 怎样组合成专用 Agent

- 类型：原理篇
- 核心问题：同一运行时怎样形成 Web、Headless 和不同专用能力集
- 抽象模型：Base Bundle → Profile Bundles → Profile Patch → Home Patch → Runtime Overlay
- 必须回答：配置覆盖、能力组合、作用域隔离、可复现启动
- BuildPilot 落地：定义 Diagnostic Profile 与未来 Write-enabled Profile
- 验证：dump 出实际生效配置

#### 19｜System Prompt Assembly：Prompt Engineering 怎样进入运行时

- 类型：源码原理篇
- 核心问题：多个插件同时贡献 Prompt 时怎样避免顺序、覆盖和冲突失控
- 抽象模型：PromptSection + PromptContext + Tool Provider + Variables + Ordered Assembly
- 必须回答：顺序、Scope、Shadow、`complete`、冲突失败、动态 Provider
- BuildPilot 落地：拆分 Harness Identity、Deployment Persona、Tool Guidance
- 验证：重复 section、错误变量、多个 complete section 必须显式失败

#### 20｜DeepSeek Harness 的 Context Engineering

- 类型：源码原理篇
- 核心问题：静态 Prompt、动态 Context、历史和 Tool Schema 怎样形成每一步输入
- 抽象模型：Stable Prompt Prefix + Cache-safe Dynamic Context + Derived History
- 必须回答：PromptContext 快照、变化检测、Compaction 后重注入、Agent Scope、模型可见信息可重建
- BuildPilot 落地：ContextContributor 接口与阶段化 Context View
- 验证：每次模型请求必须能从 Trace 重建

#### 21｜Turn、Step、Inbox 与 Agent Loop

- 类型：源码原理篇
- 核心问题：一条用户消息如何驱动零到多个 Model Step 和 Tool 执行
- 抽象模型：Inbox → Turn → Step → Request → Tool Batch → Next Step / Stop
- 必须回答：唤醒消息、注入 Context、并发 Tool、继续条件、停止条件
- BuildPilot 落地：实现最小 Turn / Step 状态
- 验证：无 Tool、一个 Tool、多 Tool、取消四条轨迹

#### 22｜Session Event：为什么聊天记录要变成可重放事实流

- 类型：源码原理篇
- 核心问题：普通 `List<Message>` 为什么不足以支持恢复、分叉、UI 和审计
- 抽象模型：Append-only Events → Projections
- 必须回答：Durable Event、Live Event、Replay、Resume、Fork、Transcript、Telemetry、Compaction
- BuildPilot 落地：JSONL Session Event Store
- 验证：从事件流重建模型历史和诊断结果

#### 23｜Tool Registry 与执行管线

- 类型：源码原理篇
- 核心问题：模型发出 Tool Call 后，为什么不能直接调用函数
- 抽象模型：Validate → Pre-execute → Allow / Deny / Ask → Execute → Validate Output → Post-execute → Persist
- 必须回答：模型 Schema、Host-only Metadata、Canonical Value、Model Content、UI Presentation、Timeout、Cancellation、Concurrency
- BuildPilot 落地：三只只读 Tool 的统一执行管线
- 验证：策略拒绝不能被后续插件重新放行

#### 24｜Capability Seam：模型、文件系统、Shell、Sandbox 为什么都能替换

- 类型：源码原理篇
- 核心问题：怎样更换 Provider 而不让所有 Consumer 分叉
- 抽象模型：Service Definition → Provider → Consumer
- 必须回答：LLM Adapter、FS Provider、Subprocess、Sandbox、Subagent Provider
- BuildPilot 落地：IModelAdapter、IFileEvidenceProvider、未来 IJenkinsProvider
- 不展开：全部 DSH Provider 实现细节

#### 25｜DeepSeek Harness 的 Token 与推理成本控制

- 类型：源码 / 实验篇
- 核心问题：Harness 怎样同时控制输入、输出、Reasoning、步骤数、Tool Result 和 Cache
- 抽象模型：Cost Ledger + Budget Policies + Context Lifecycle
- 必须回答：稳定前缀、动态 Context、Prefix Cache、Tool Schema、Result Spill、Compaction、Reasoning Effort、Model Routing、Subagent Cost
- BuildPilot 落地：Per-run Usage 与 Budget Guard
- 验证：优化前后同时比较成本、延迟和诊断正确率

#### 26｜RAG、Skill 与 Workflow 应该怎样接进 DeepSeek Harness

- 类型：设计映射篇
- 核心问题：这些能力是否是 DSH 核心一级对象，如果不是，应落在哪个扩展点
- 抽象模型：Retrieval / Skill / Workflow Provider → Context / Tool / Agent Scope
- 必须回答：Retrieval Tool 与 PromptContext Provider 两种 RAG 接法；Skill Discovery 与能力注册；Workflow Tool 与状态机
- 证据边界：明确区分官方已有机制与本文提出的插件映射
- BuildPilot 落地：实现一个最小历史故障检索 Provider

#### 27｜Tracing、调试、取消、恢复与错误闭环

- 类型：源码 / 案例篇
- 核心问题：Agent 答错时怎样知道错在 Prompt、Context、Model、Tool、Policy 还是 Workflow
- 抽象模型：Run → Turn → Step → Model Request → Tool Execution → Result → Error Classification
- 必须回答：Trace、错误分层、取消、超时、重试、恢复、可复现输入
- BuildPilot 落地：统一 Trace ID 和错误分类
- 验证：同一失败可以从 Trace 重放和解释

#### 28｜Web、Headless 与 AI Native 产品接入

- 类型：架构映射篇
- 核心问题：同一个 Harness 怎样服务交互式产品和后台自动化
- 抽象模型：Shared Harness Core + Multiple Hosts
- 必须回答：Web UI、CLI、CI、IDE、Unity Editor、Jenkins 后台入口的状态与权限差异
- BuildPilot 落地：CLI first，桌面 / Unity / Jenkins 作为后续 Host
- 不展开：完整前端框架教程

### 第五部分：实现自己的游戏开发专用 Agent（29-34）

#### 29｜用 C#/.NET 实现最小 Agent Runtime

- 类型：实作篇
- 核心问题：不用大框架，最小 Agent Loop 至少需要哪些对象
- 实现：Model Adapter、Messages、Tool Schema、Tool Dispatch、Turn / Step、Structured Output
- 模型：DeepSeek 作为第一个 Adapter，但不把 Runtime 锁死在一个 Provider
- 验证：无 Tool 与单 Tool 两条最小轨迹

#### 30｜实现 Unity / Jenkins 只读诊断 Agent

- 类型：实作 / 案例篇
- 核心问题：怎样把通用 Agent Runtime 收窄成可验证的专用 Agent
- 实现：`parseUnityLog`、`readFile`、`searchLiteral`、DiagnosisResult
- 边界：隔离 Fixture、路径白名单、无 Shell、无写入
- 验证：标准 C# 编译失败黄金案例

#### 31｜给诊断 Agent 加 Context、Skill 与 RAG

- 类型：实作篇
- 核心问题：项目事实、领域方法和历史知识怎样分别进入 Agent
- 实现：Project Context、Diagnostic Skill、Incident Retrieval、Source Citation
- 验证：错误版本知识、冲突知识、无检索结果和过期记录

#### 32｜给诊断 Agent 加 Harness Policy

- 类型：实作篇
- 核心问题：怎样把“请不要越界”从 Prompt 变成运行时约束
- 实现：Path Allowlist、Tool Allowlist、Approval、Token / Step Budget、Cancellation、Trace
- 验证：越界读取、未授权 Tool、预算耗尽、人工拒绝

#### 33｜Eval：怎样证明专用 Agent 比通用 Agent更适合这个任务

- 类型：实验 / 映射篇
- 核心问题：Agent 好不好怎样从主观印象变成可重复评测
- 对照：无 Harness、通用 Agent、专用 Agent；同模型与跨模型
- 指标：根因准确率、Evidence 引用正确率、越权率、Token、现金成本、延迟、Tool 次数、人工返工点
- 边界：Eval Fixture 不等于真实 Unity / Jenkins 运行验收

#### 34｜把 Agent 接进真实游戏开发工具链

- 类型：工程落地篇
- 核心问题：怎样从 Demo 安全走向现有工具管线
- 路径：Fixture → 本地日志 → Jenkins 只读 API → Unity 静态检查 → 人工批准的补丁建议 → 后续验证
- 必须回答：权限升级门槛、部署形态、凭证、审计、回滚和人工责任
- 验证边界：Jenkins 读取、代码补丁、Unity 编译、设备 / 生产验收分别报告

---

## 11. 岗位能力覆盖矩阵

| 岗位职责 / 要求 | 主要对应文章 | 产出证据 |
|---|---|---|
| AI 开发流程构建 | 10-15、29-34 | 可运行 Agent Loop 与 Workflow |
| Agent 工作流编排 | 13-14、21 | 状态机、Turn / Step Trace |
| Harness 工程 | 16-28、32 | 源码分析、Policy、Session、Tool Pipeline |
| Context Engineering | 06、20、31 | Context View 与 Token / 准确率对照 |
| Agent Skill 与 Tool 设计 | 11-12、23、26、30-31 | Skill、Tool Schema、执行测试 |
| Token Optimization | 09、25、33 | Usage Ledger、预算和 A/B 报告 |
| 调试链路优化 | 22、27、33 | 可重放 Trace 与错误分类 |
| LLM API 与 Agent 编排框架 | 01、10、16、29 | Model Adapter 与跨框架映射 |
| Prompt Engineering | 05、19 | Prompt A/B 与 Assembly 分析 |
| RAG | 07-08、26、31 | 检索实验、引用和新鲜度处理 |
| AI Native 产品经验 | 04、28、34 | 多 Host 架构与集成路径 |
| 接入游戏研发工具管线 | 28、30、34 | Unity / Jenkins 渐进式集成 |
| C#、C++、前后端基础 | 29-34 中使用 | 实际工程代码，不重教语言语法 |
| 软件设计与计算机基础 | 13、17、22-24 | 状态机、事件溯源、插件、Adapter / Provider |

---

## 12. 推荐阅读顺序与建议写作顺序

### 12.1 读者阅读顺序

默认按 01 → 34 顺序阅读。

按目标跳读：

- 先补 Agent 基础：01-15
- 重点读 DeepSeek Harness：01-02、05-13、16-28
- 直接构建专用 Agent：01-03、05-15、29-34
- 关注成本与 Context：05-09、19-20、25-27、31-33
- 关注游戏工具链落地：03-04、13、15、28-34

### 12.2 作者建议写作顺序

目录顺序不等于写作顺序。建议先写能立住整套抽象和实作骨架的文章：

```text
01 总边界
→ 02 对象关系
→ 06 Context Engineering
→ 10 Agent Loop
→ 11 Tool Engineering
→ 13 Workflow Engineering
→ 15 Evidence Contract
→ 16 DSH 总图
→ 19 Prompt Assembly
→ 21 Agent Loop
→ 23 Tool Pipeline
→ 29 最小 Runtime
→ 30 诊断 Agent
→ 33 Eval
```

以上骨架站住后，再补 Prompt、RAG、Memory、Cost、Capability、产品接入和其他源码篇。

---

## 13. 证据与版本策略

### 13.1 DeepSeek Harness

- 官方仓库当前是 Developer Preview，存在破坏性变化风险
- 正式开写源码篇前必须锁定 tag 或 commit
- 每篇记录文件路径、关键符号和验证日期
- 官方架构文档与当前源码冲突时，以锁定 commit 的源码为准，并记录差异
- 不根据社区二手文章推断官方内部意图

### 13.2 Prompt、Context、RAG、Tool、Skill

- 概念定义优先引用官方规范、官方 SDK 或原始论文
- Skill 需区分通用 Agent Skills 规范与具体产品实现
- Tool 需区分 Function Calling、MCP Tool 和 Harness 内部 ToolDefinition
- RAG 需区分稳定抽象与具体向量数据库 API

### 13.3 Token 与价格

- 模型价格、上下文窗口、缓存折扣和 Reasoning 行为都属于时效性事实
- 正文必须标版本和核验日期
- 优先写计算模型和实验方法，不把当前价格写成长期常量

### 13.4 BuildPilot

- Fixture 证明离线诊断行为，不等于真实 Unity 编译、Jenkins 运行或生产接受
- 静态检查、离线 Eval、Unity BatchMode、Jenkins Build、设备运行分别作为独立证据渠道
- 每项“已实现”都必须带实际测试或 Trace

---

## 14. 首期与二期的候选切法

### 方案 A：完整大系列

- 01-34 全部保留
- 优点：知识体系完整，能覆盖岗位能力图
- 代价：周期长，早期容易出现大量骨架和未完成文章

### 方案 B：三期发布

第一期：Agent 基础与最小 Runtime

- 01-15
- 29-30

第二期：DeepSeek Harness 源码

- 16-28

第三期：BuildPilot 工程化

- 31-34

优点：每一期都有独立闭环。当前倾向此方案。

### 方案 C：主线 + 专题

- 主线保留 01-15、29-34
- DeepSeek Harness 16-28 独立成源码专题

优点：框架版本变化不会拖动基础主线。
风险：读者在基础系列和源码专题之间来回跳转。

---

## 15. 请 GPT 重点审核的问题

1. 这个系列的问题主线是否清晰，还是被岗位能力清单牵着走
2. 34 篇是否过多；哪些应该合并，哪些绝不能合并
3. 01-15 的概念粒度是否一致
4. Prompt、Context、RAG、Session、Memory 的边界是否准确
5. Tool、Skill、Workflow、Agent、Harness 的关系是否存在概念错误
6. DeepSeek Harness 16-28 是否足以讲清架构，又是否出现源码细节过深的问题
7. 第 26 篇把 RAG / Skill / Workflow 映射为插件能力是否合理，是否误写成 DSH 现有事实
8. Token 与推理成本是否应该保留 09 和 25 两篇：一篇通用模型，一篇 DSH 实现
9. BuildPilot 是否适合作为贯穿案例，还是应该换成更小的 Agent
10. 三期发布是否比完整大系列更可执行
11. 与现有 `AI 赋能游戏开发`、`Harness Engineering` 是否仍有明显重复
12. 岗位截图中的能力是否有遗漏，或有哪些其实不应该由本系列承担
13. 哪 8-12 篇最适合作为首批写作闭环
14. 哪些篇必须先做实验或 evidence card，不能只凭概念起草
15. `Agent 知识飞轮` 应留在 `AI 赋能游戏开发` 做深化篇，还是应进入本系列；怎样避免与 01、04、07、08、10、11 重复

---

## 16. 可直接交给 GPT 的审核提示词

```text
下面是一份中文技术专栏的系列提纲评审稿。请不要直接重写全文，也不要因为标题多就机械压缩。

请从“系列编辑 + Agent/Harness 架构师 + 目标读者”三个视角独立审核，并输出：

1. 一句话判断：这个系列的真正主线是什么，目前是否站得住
2. 概念正确性：指出 Prompt、Context、RAG、Memory、Tool、Skill、Workflow、Agent、Harness 之间可能不准确或混淆的地方
3. DeepSeek Harness 覆盖度：16-28 是否足以解释其设计、理由、收益和代价；哪些关键机制遗漏
4. 粒度审查：逐项列出“保留 / 合并 / 拆分 / 移到二期”的文章编号和理由
5. 重复审查：检查基础篇、DSH 源码篇和 BuildPilot 实作篇之间是否重复
6. 学习路径：判断前置依赖和阅读顺序是否合理
7. 工程闭环：判断 BuildPilot 是否能真正验证这些知识，而不只是做演示
8. 证据风险：指出哪些文章必须有源码、实验、Trace 或真实项目证据才能写
9. 给出一个你建议的首期最小目录，控制在 8-12 篇，但不能丢失完整系列的长期地图
10. 最后列出 5 个需要作者本人拍板的问题
11. 单独判断“Agent 知识飞轮”候选篇的归属、必要性和最小不可合并内容，检查它是否和既有知识闭环、RAG、Memory、Skill 自动沉淀重复

审核时请保留这份提纲的目标：它是作者自己的系统学习路线，也是面向游戏 Agent / AI 工程岗位的作品集；不应退化成工具使用教程、术语百科或 DeepSeek Harness API 手册。
```

---

## 17. 当前参考入口

### 仓库内方法与相邻系列

- `docs/article-writing-method.md`
- `docs/article-outline-template.md`
- `docs/series-planning-method.md`
- `docs/article-production-workflow.md`
- `docs/ai-empowerment-series-plan.md`
- `docs/harness-engineering-series-plan.md`
- 外部案例素材：`D:\DownLoad\Agent的上限，可能不在模型，而在团队知识.html`（只作论点与机制参考，不继承其中的实践数据）

### DeepSeek Harness 官方资料

- <https://github.com/deepseek-ai/deepseek-harness>
- <https://github.com/deepseek-ai/deepseek-harness/blob/master/docs/architecture.md>
- <https://github.com/deepseek-ai/deepseek-harness/blob/master/docs/subsystems/system-prompt.md>
- <https://github.com/deepseek-ai/deepseek-harness/blob/master/docs/subsystems/tools.md>
- <https://github.com/deepseek-ai/deepseek-harness/blob/master/docs/agent-lifecycle.md>
- <https://github.com/deepseek-ai/deepseek-harness/blob/master/docs/tool-execution-pipeline.md>

### 通用规范入口

- Agent Skills：<https://agentskills.io/specification>
- MCP Tools：<https://modelcontextprotocol.io/specification/2025-06-18/server/tools>
- OpenAI Agents SDK：<https://openai.github.io/openai-agents-python/>
- DeepSeek Tool Calls：<https://api-docs.deepseek.com/guides/tool_calls>

---

## 18. 最短结论

`先用基础抽象建立 Agent Engineering 的共同语言，再用 DeepSeek Harness 源码解释这些机制为什么存在，最后用 BuildPilot 的真实证据链证明自己不只是“读懂了”，而是真的能设计、实现和验证一个专用 Agent。`
