# Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery

如果这篇只记一句话，可以先记这一句：

> 最小 Harness 不是功能最少的菜单，而是能持续回答谁在行动、什么能力可见且获权、哪些上下文可用、什么能被当作证据、失败后从哪里恢复、何时停止或问人，以及知识和回归声明是否仍然有效的最小责任闭环。

上一篇把 Runtime、Harness、Host 和业务 Agent 的边界切开：Runtime 负责推进一次 Run，Harness 负责让跨 Run、跨 Tool、跨 Workflow 的身份、权限、证据、预算、Trace、审批、恢复和能力暴露保持同一种可审计语义。

切完边界后，真正难的问题才出现。

如果团队决定“我们确实需要 Harness”，下一步不能直接列功能清单。因为一列功能很容易越列越长：Session、Memory、Tool Registry、Policy Engine、Sandbox、Approval、Trace、Replay、Eval、Budget、Workflow、Knowledge Base、Dashboard、Cost Center、Admin Console……每一个看起来都重要，每一个都能找到合理场景。

但“重要”不等于“首版最小核心”。最小模型要回答的是：缺了哪些能力，系统就不能维持上一篇说的共享治理语义？哪些能力虽然有价值，但只在特定风险、规模或承诺下进入核心？哪些能力应该保留接口或 hook，却不该在第一版里被写成 mandatory platform？

本文做的就是这件事。

先声明证据姿态，免得后面语气跑飞。Article 26 是一篇原则模型文章，Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。本文沿用 `11 / 11` Claims 与 `11 / 11` Evidence Cards，证据状态上限为 `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。BuildPilot 只作为 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST` 出现；它不是已经实现、已经运行、已经扫描 Unity、已经调用 Jenkins、已经创建 PR、已经修改代码，或已经产生生产证据的系统。

换句话说：本文会给出一个课程模型，而不是发布一个行业标准；会用公开资料支持责任区域的合理性，但不会把这些资料包装成“所有产品都必须长这样”的结论；会用 BuildPilot 做设计镜子，但不会把设计镜子说成落地系统。

## 1. 最小 Harness 不是功能菜单

很多架构讨论会从这个问题开始：

> 一个 Harness 需要哪些模块？

这个问法很顺手，也很危险。

它会诱导我们把最小模型写成采购清单：有没有 Session？有没有 Approval？有没有 Sandbox？有没有 Trace？有没有 Eval？有没有 Memory？有没有 Replay？最后得到一张长表，看起来完整，实际上没有说清任何工程边界。

更麻烦的是，同一个词在不同系统里可能代表不同层级。一个 SDK 里的 `Session` 可能主要是历史记录对象；课程里的 Session 还要承担可追踪、可恢复、可治理的交互与执行边界。一个框架里的 `Capability` 可能是沙箱能力或工具能力；课程里的 Capability 是 Agent 可被授予和治理的一类能力契约，不等同某一个具体 Tool。一个平台里的 `Trace` 可能只是事件记录；课程里的 Evidence 还要说明某个结论被什么接受、没证明什么。

所以最小 Harness 的起点不是“有哪些功能”，而是“哪些不变量必须长期成立”。

功能菜单会问：

| 菜单式问题 | 容易得到的答案 | 为什么不够 |
|---|---|---|
| 要不要 Session？ | 要，先存 history。 | 存了 history 不等于知道 owner、scope、continuation boundary 和可恢复状态。 |
| 要不要 Tool Registry？ | 要，把工具 schema 放进去。 | 看见工具不等于有权使用；schema 存在不等于可信、适用或版本兼容。 |
| 要不要 Approval？ | 要，弹窗让用户点。 | 用户点过不等于本次 actor、action、resource、scope、expiry 都有效。 |
| 要不要 Trace？ | 要，打日志。 | 发生过不等于可接受为证据；日志也不自动支持 replay 或 regression。 |
| 要不要 Eval？ | 要，做 benchmark。 | 没有稳定 dataset、oracle、manifest 和 verdict policy，Eval 只是另一种 demo。 |

不变量式问题会更硬一点：

| 不变量式问题 | 它逼你定义什么 |
|---|---|
| 谁在行动？ | Actor、owner、session、workspace、任务边界和责任归属。 |
| 什么能力可见，什么能力获权？ | Registry、version、trust、policy、approval 与 sandbox 的分层。 |
| 当前步骤可以看到哪些上下文？ | Context policy、source provenance、freshness、隔离、预算和复用规则。 |
| 什么能成为证据？ | Observation、Trace、Evidence、claim、failure layer 与 unknown 的区别。 |
| 中断后从哪里继续？ | 已提交状态、in-flight action、副作用不确定性、预算和恢复决策。 |
| 何时问人、何时停止？ | HITL、Change Request、approval expiry、budget stop 和 policy deny。 |

这就是本文的判断顺序：

```text
不是功能菜单
  -> 先写必须长期成立的不变量
  -> 再推导最少责任闭环
  -> 对每项能力定义合同、失败与证据
  -> 区分最小核心和环境相关扩展
  -> 用 BuildPilot 映射只读 suggestion-first 设计案例
```

如果一个能力不能保护任何不变量，它就不该因为名字高级而进入最小核心。如果一个能力保护的不变量一旦失效就会让权限、证据、恢复或归属全部失真，它就算实现得很薄，也必须进入最小 Harness 的责任面。

## 2. 十条不变量：先问什么必须长期成立

本篇采用十条课程不变量来推导最小能力。它们不是行业标准条款，而是把前文 Evidence、Permission、Budget、Trace、Recovery、Eval、Session、Knowledge 等内容压缩成一组可审查的问题。

| ID | 不变量 | 如果失效，会发生什么 |
|---|---|---|
| I1 | Actor、Session 与 Ownership 必须稳定可归属。 | 后续权限、证据、恢复和责任记录都不知道归谁。 |
| I2 | Capability visibility 不等于 capability authority。 | “模型看见工具”会被误写成“系统允许使用工具”。 |
| I3 | Context provenance、隔离和复用规则必须跨压缩、恢复和会话延续保持可审查。 | 旧上下文、污染上下文或跨项目记忆会被当成当前事实。 |
| I4 | Authority 必须在使用时检查，而不是只在发现时检查。 | 曾经可见、曾经批准或曾经安全的动作会在条件变化后继续执行。 |
| I5 | Observation、Trace 和 accepted Evidence 必须分开。 | “发生过”“看见过”“工具成功过”会被误写成“结论成立”。 |
| I6 | Recovery 必须从 known / unknown 分离开始。 | 中断后直接重试，可能重复副作用或覆盖未确认状态。 |
| I7 | 稀缺资源必须有停止语义。 | 长任务、付费调用、限流资源或用户可见延迟会无限消耗。 |
| I8 | Human decision 必须成为状态转换，而不是聊天旁白。 | 人说过的话无法约束 scope、expiry、owner、diff 或复验责任。 |
| I9 | Knowledge 必须带来源、时效和进入策略。 | memory、RAG、历史结论会自动冒充当前事实。 |
| I10 | Regression 声明必须独立于一次成功运行。 | “这次过了”会被误写成“以后不会坏”。 |

这十条里，I1 到 I6 基本决定了最小 Harness 的骨架。没有身份和会话，就没有归属；没有能力注册与权限边界，就无法解释模型为什么能或不能行动；没有上下文策略，就无法知道模型到底看了什么；没有 Trace/Evidence/Failure，就无法审计结论；没有 Recovery 边界，就无法从失败里安全继续。

I7 到 I10 则更像风险触发器。它们很重要，但不是所有最小场景都要把完整预算平台、人工审批平台、知识图谱和 Eval 平台一次性做完。真正可靠的最小模型，应该承认这些能力的进入条件，而不是用“都必须有”制造一个巨大的 Harness。

## 3. 十类候选能力：先分级，再讨论实现

按照这些不变量，候选能力可以分成三种位置：

- Minimum Core：缺了它，共享治理语义就断。
- Conditional Core：在特定风险、场景或承诺下必须进入核心；低风险首版可简化或延后。
- Environment-specific Extension：有价值，但依赖组织规模、评测要求、产品形态或后续阶段，不应写成所有 Harness 的首版必要条件。

本文给出的分级如下：

| 候选能力区 | 本篇分类 | 判断理由 |
|---|---|---|
| Identity / Session / Ownership | `MINIMUM CORE` | 没有稳定 actor、session、task 与 owner 边界，权限、trace、evidence、recovery 都无法归属。 |
| Context Assembly and Isolation | `MINIMUM CORE for policy and isolation; Runtime owns concrete assembly` | Harness 至少要定义什么可暴露、保留、压缩、引用和复用；具体 token packing 可由 Runtime 执行。 |
| Tool / Skill Capability Registry and Version | `MINIMUM CORE` | 能力发现、schema、版本、来源信任和适用范围必须可见，否则错误能力会进入执行。 |
| Permission / Approval / Sandbox / Policy Enforcement | `MINIMUM CORE` | 任何能触达外部动作的 Harness 都需要拒绝优先、使用时检查的 authority gate。 |
| Execution Control / State / Checkpoint / Recovery | `MINIMUM CORE as boundary contract; durable checkpoint engine is CONDITIONAL CORE` | Harness 不一定拥有每个 step loop，但必须定义 stop / resume / retry / recover 语义。 |
| Trace / Evidence / Replay / Failure Taxonomy | `MINIMUM CORE for Trace/Evidence/Failure layer; Replay is CONDITIONAL CORE` | Trace、证据状态、失败层级是审计最低要求；完整 replay 依赖确定性、环境和副作用约束。 |
| Budget / Step / Cost / Latency Control | `CONDITIONAL CORE` | 长任务、付费资源、限流资源、风险动作或用户可见延迟时进入核心；短小本地一次性 assistant 可简化。 |
| Human-in-the-loop, Change Request and Intent Confirmation | `CONDITIONAL CORE; MINIMUM CORE for BuildPilot` | suggestion-first 的生产建议必须走 owner review；需求目标、范围或验收口径不清时必须先澄清，对应 `26-C09`。 |
| Knowledge provenance and freshness | `CONDITIONAL CORE` | memory、RAG 或项目知识影响 action / claim 时必须有来源和时效；不替代 BuildPilot 的 intent confirmation。 |
| Evaluation / Golden Cases / Regression Hook | `ENVIRONMENT-SPECIFIC EXTENSION, often DEFERRED from first Harness slice` | 一旦承诺可重复质量就需要 hook；但 Article 26 不强制首版做完整 Eval 平台。 |

这张表的重点不是“某个能力永远不重要”。恰好相反，预算、HITL、Knowledge 和 Eval 都非常容易在真实系统里变重要。重点是：最小模型要把触发条件写出来。

比如一个只回答本地文档问题的小 assistant，可能不需要复杂 cost center，也不需要完整 owner review 流程。但如果它要在生产仓库里给修改建议、影响团队知识库、触发复验，HITL、知识来源、时效和证据接收就不再是“以后再说”的 UX 装饰，而是最小闭环的一部分。

## 4. 能力不是模块名，而是责任合同

接下来讨论核心能力时，本文不按“模块怎么命名”展开，而按“责任合同”展开。

一个 Harness capability 至少应该能回答八个问题：

| 合同字段 | 要回答的问题 |
|---|---|
| Problem protected | 它保护哪条不变量？避免哪类治理漂移？ |
| Input | 它接收哪些事实、请求、状态或策略？ |
| Output | 它产出什么可被其他层消费的记录、决策或边界？ |
| Dependencies | 它依赖 Host、Runtime、Workflow、Tool、Policy、KB 或业务 Agent 的哪些输入？ |
| Trust boundary | 哪些输入只能当 hint，不能当 authority？ |
| Failure / degradation | 信息缺失、版本不明、权限不足、上下文过期或副作用不确定时，应该 deny、ask、degrade、stop 还是 mark unknown？ |
| Observable evidence | 后续审计者能看到什么 artifact，证明它确实做过边界判断？ |
| Interfaces | Runtime、业务 Agent、Tool、Workflow、Policy、KB/RAG 分别怎样消费它？ |

这套合同有一个很朴素的好处：它逼我们把“我觉得需要这个功能”改写成“系统必须在这个位置留下这种可审查状态”。如果一个能力只能说出 UI、类名或产品截图，却说不出失败时怎么降级、什么能被审计，它就还不是 Harness 最小能力。

下面的 A 到 F，是本文承认的最小核心责任合同。G 到 J 是条件核心或延后扩展，会在后面单独说明进入条件。

## 5. Core A：Identity + Session + Ownership Ledger

第一项核心能力是 Identity、Session 与 Ownership Ledger。

它解决的问题非常原始：这次执行是谁发起的，谁拥有目标，在哪个 workspace 或 host 里发生，延续到哪里算同一条任务边界，失败以后该向谁问，证据最后归属到哪里。

如果这些问题没有稳定答案，后面的 policy、approval、trace、evidence、recovery 都会变成空中楼阁。审批不知道谁批的，Trace 不知道属于哪个任务，知识沉淀不知道进哪个项目，恢复不知道是否还在同一上下文里继续。

| 合同字段 | Core A 的最小回答 |
|---|---|
| Problem protected | 保护 I1：actor、session、owner 和 task boundary 必须可归属。 |
| Input | 用户身份或 owner 标识、任务请求、session id、host / workspace id、时间、前序会话引用。 |
| Output | Session envelope、ownership record、actor binding、continuation boundary。 |
| Dependencies | Host 提供交互与工作区事实；Runtime 绑定 run / session；Workflow 绑定任务状态。 |
| Trust boundary | Host UI 的当前可见状态不是完整 authority；SDK session object 也不自动等同课程里的 Session。 |
| Failure / degradation | actor 缺失则 fail closed；owner 模糊则 ask；session 过期或跨范围复用则建立新边界或请求确认。 |
| Observable evidence | session record、owner decision、trace correlation id、continuation boundary。 |
| Interfaces | Runtime 消费 run/session id；业务 Agent 读取 owner goal；Tool 接收 scoped actor；Workflow 保存任务边界；Policy 检查 actor/scope；KB/RAG 标注来源范围。 |

这里最容易写错的是把 Session 降级成“聊天历史”。

History 是素材，Session 是边界。一个系统可以保存很完整的聊天记录，却仍然不知道这段记录是否还能授权新动作、是否属于同一个 owner、是否可以跨项目复用、是否允许进入知识库。反过来，一个极简系统即使不保存完整 history，也应该至少保存足以归属和恢复的 session envelope。

所以 Core A 的关键不是“存得多”，而是“归属稳定”。谁问的、谁负责、在哪个范围内继续、哪些记录可被引用，这些答案一旦漂移，最小 Harness 就已经破了。

## 6. Core B：Capability Registry + Version + Trust Filter

第二项核心能力是 Capability Registry、Version 与 Trust Filter。

课程术语表里，Capability 是“Agent 可被授予和治理的一类能力契约”，不等同某一个具体 Tool。这句话很重要。因为在 Agent 系统里，模型“看见”一个工具 schema，只是很靠前的一步；真正执行前至少还有好几层问题。

```text
Capability exists
  -> visible to this actor/session
  -> relevant to this task
  -> trusted enough for this source
  -> version-compatible
  -> authorized for this action/resource
  -> executable in current sandbox/runtime
  -> result admissible as observation/evidence
```

把这些问题压成一个“tools 列表”，是 Harness 最常见的早期偷懒。

| 合同字段 | Core B 的最小回答 |
|---|---|
| Problem protected | 保护 I2：能力可见性不等于能力授权；能力描述也不自动可信。 |
| Input | capability descriptor、tool / skill / MCP schema、version、source trust、environment、actor、task scope、risk label。 |
| Output | allowed view、hidden / denied list、selected capability id/version、freshness 或 compatibility note。 |
| Dependencies | Host / Registry 提供能力清单；Policy 提供可见性与风险规则；Runtime 执行选择后的调用。 |
| Trust boundary | 来自外部 server 或未受信 registry 的描述、annotation、title、hint 都只能当提示，不能当授权事实。 |
| Failure / degradation | unknown version/source 则隐藏或请求 review；schema mismatch 则 block；缺能力则报告 gap，而不是伪造能力。 |
| Observable evidence | registry snapshot、capability id/version、trust decision、denied reason。 |
| Interfaces | Runtime 只调 allowed capability；业务 Agent 只在可见能力内做计划；Tool Runtime 校验 schema；Workflow 记录能力版本；Policy 决定 allow/deny；KB/RAG 不得从能力描述推导事实真值。 |

这里有一个实用判断：只要系统允许 Agent 调外部能力，就不能只有“工具能不能调用”这一问。

例如一个工具可能技术上存在，但版本和当前 project contract 不兼容；一个 MCP server 可能暴露了漂亮的 annotations，但 annotations 本身不是可信 authority；一个 skill 可能适合某类文章，却不适合当前证据门禁；一个文件编辑能力可能对普通草稿安全，对生产发布路径就需要更强 approval。

Core B 不是要求首版做一个复杂管理后台。最小形态可以很薄：一张能力表、版本、来源、信任级别、当前 actor/session 的可见范围、deny reason。关键是它必须把“存在、可见、相关、可信、获权、可执行、可采信”拆开。

## 7. Core C：Context Policy Envelope

第三项核心能力是 Context Policy Envelope。

注意，这里说的是 Context Policy，不是所有具体 Context Assembly 细节。Runtime 可以负责 token packing、压缩、排序、截断和传给模型的实际上下文；Harness 至少要定义哪些上下文可以暴露、哪些不能复用、哪些需要引用来源、哪些已经过期、哪些必须排除。

如果没有这层 policy，系统会很快陷入“模型看过什么没人说得清”的状态。尤其是在长任务、跨会话、memory/RAG、项目知识和压缩摘要参与时，上下文污染会比工具错误更隐蔽。

| 合同字段 | Core C 的最小回答 |
|---|---|
| Problem protected | 保护 I3：上下文来源、隔离、时效、压缩和复用规则必须可审查。 |
| Input | task scope、candidate context、source references、sensitivity label、freshness、token budget、reuse policy、citation / receipt requirement。 |
| Output | context plan、included / excluded source list、context receipt、compaction / reuse limit、unknown list。 |
| Dependencies | Host 提供 workspace / file / UI 范围；Runtime 执行 assembly；KB/RAG 提供候选知识；Policy 定义敏感和隔离规则。 |
| Trust boundary | 检索到、记住了、上次说过、摘要里出现过，都不是当前事实；必须经过来源和时效策略。 |
| Failure / degradation | 缺 provenance 则 unknown 或 exclude；超预算则降级并留下 receipt；敏感或越界内容则 redact / block。 |
| Observable evidence | context receipt、source list、exclusion reason、freshness note、compaction boundary。 |
| Interfaces | Runtime 按 policy 打包；业务 Agent 引用 receipt；Tool 不接收越界 context；Workflow 记录 context boundary；Policy 执行敏感规则；KB/RAG 只提供带来源候选。 |

这层能力最容易被低估，因为它不像 Sandbox 那样“看起来危险”。但很多 Agent 事故不是因为工具太强，而是因为上下文被悄悄污染：把旧项目决策当当前规则，把上一个用户的偏好带到新任务，把检索结果当权威事实，把压缩摘要当完整证据。

最小 Harness 不需要掌握每个 token 的内部注意力。它需要的是工程侧可控的上下文合同：本步允许看什么、为什么允许、什么被排除、哪些材料过期、哪些结论只能标 unknown。

## 8. Core D：Authority Gate = Permission + Approval + Sandbox + Policy

第四项核心能力是 Authority Gate。

这也是最容易被口号化的一项。很多系统会说“我们有权限控制”“我们有 approval”“我们跑在 sandbox 里”。但在 Harness 视角，这几个词不能互相替代。

- Permission 说明 actor 在某个范围内能做什么。
- Approval 说明某个具体请求在某个证据与范围下被人或规则批准。
- Sandbox 说明动作运行在哪个隔离边界里。
- Policy Enforcement 说明实际使用时如何拒绝、降级或要求额外确认。

它们一起构成拒绝优先的 authority gate。

| 合同字段 | Core D 的最小回答 |
|---|---|
| Problem protected | 保护 I4：authority 必须在使用时检查，并绑定 actor、action、resource、scope 和当前请求。 |
| Input | actor、capability、action、resource、frozen request digest、params、risk、approval record、sandbox envelope、policy rules。 |
| Output | allow / deny / approval-required、scoped approval、sandbox decision、denied reason、policy decision id。 |
| Dependencies | Host 承载确认 UI；Policy 定义规则；Runtime 在调用前检查；Tool Runtime 执行沙箱和参数限制；Workflow 处理 pause/resume。 |
| Trust boundary | “用户在聊天里说可以”不是无限授权；approval 不能越过变更后的 request、resource、scope、risk 或 expiry。 |
| Failure / degradation | 默认 deny；approval stale 则 ask；sandbox 不匹配则 block 或 downgrade 到 read-only；risk 升级则重新审批。 |
| Observable evidence | decision id、approval record、request digest、sandbox manifest、denied reason。 |
| Interfaces | Runtime 每次外部动作前查询；业务 Agent 解释为什么要 approval；Tool 接收已裁剪参数；Workflow 暂停或恢复；Policy 存储决策；KB/RAG 不能授予 authority。 |

这层能力的底线是：不要把发现时检查当使用时授权。

一个工具在任务开始时可见，不代表十分钟后仍然适合调用；一个用户批准了“读取日志”，不代表批准“修改配置”；一个 sandbox 允许写临时目录，不代表允许改生产仓库；一个先前任务的 owner 决策，不代表当前 session 可以继承。

对 BuildPilot 这种只读建议链尤其如此。本文里的 BuildPilot 不获得写权限，不创建 PR，不调用 Jenkins，不部署。它可以提出 Change Request，可以要求 owner review，可以设计复验步骤；真实修改由 owner 在 BuildPilot 之外完成。Authority Gate 要保护的，正是这条边界不被“顺手自动化”吞掉。

## 9. Core E：Trace + Evidence + Failure Layer

第五项核心能力是 Trace、Evidence 与 Failure Layer。

前文已经反复强调过：Trace 不是 Evidence，Evidence 也不是“日志很多”。在最小 Harness 里，Trace 的价值是让发生历史可关联；Evidence 的价值是让 claim 的证明范围可审查；Failure Layer 的价值是让失败不要被压成一句“工具报错”。

| 记录类型 | 它记录什么 | 它不自动证明什么 |
|---|---|---|
| Raw log | 某个组件输出过什么文本 | 业务事实成立、证据被接受、根因找到。 |
| Tool result | 工具执行返回的结果 | 结果可信、满足目标、可作为 claim evidence。 |
| Observation | 经过关联和正规化后可供 Agent 使用的观察 | 已进入权威 State 或 Evidence Contract。 |
| Trace | 跨 step、tool、provider、state change 的结构化记录 | 可 replay、可回归、可授权。 |
| Evidence | 支撑某个 claim 的来源、观测、实验或设计提案 | 证明范围之外的结论。 |
| Failure taxonomy | 失败所属层级、候选原因、未知项 | 已经得到 root cause 或修复成功。 |

最小 Harness 要做的是把这些记录分开，而不是把它们都叫“日志”。

| 合同字段 | Core E 的最小回答 |
|---|---|
| Problem protected | 保护 I5：发生、观察、证据接受和失败归因必须分开。 |
| Input | run / step / tool events、observations、claim ids、evidence rules、failure taxonomy、source references。 |
| Output | trace events / spans、observation refs、evidence status、claim register、failure classification、unknown list。 |
| Dependencies | Runtime 产出事件；Tool Runtime 产出结果；业务 Agent 提出 claim；Evidence contract 定义接收标准；Workflow 记录状态变化。 |
| Trust boundary | Trace/log presence 不是 proof；tool success 不是 business acceptance；一次观察不是 regression verdict。 |
| Failure / degradation | trace 不完整则降低证据状态；未接受观察保持 observation；失败层级不清则标 unknown，而不是强行归因。 |
| Observable evidence | trace/span/event id、evidence card、claim register、failure layer、unknown list。 |
| Interfaces | Runtime emits events；业务 Agent 只能引用 accepted evidence；Tool 返回 observation；Workflow 记录 transition；Policy 可要求 evidence；KB/RAG 只吸收被接受记录。 |

这层能力特别适合拿来检查一个 Harness 是否“看起来很强，实际没证据纪律”。

如果系统展示一条很漂亮的执行 timeline，但每个结论都没有 claim id 和 evidence status，它只是可视化好看。若系统能 replay 某些步骤，却没有说明外部副作用、环境版本和输入确定性，replay 也不能升级为“同一次执行可复现”。若系统说“修复已验证”，但只有一次工具返回成功，没有 golden case、oracle、manifest 和 verdict policy，那就只是一次成功运行，不是回归保证。

Article 26 的自身证据姿态也遵守这条规则：这里有 `11 / 11` claim/evidence 对齐，但状态仍是 `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。没有 lab，就不写成 lab result；没有 runtime observation，就不写成 runtime proof。

## 10. Core F：Checkpoint + Recovery Decision Boundary

第六项核心能力是 Checkpoint 与 Recovery Decision Boundary。

在 Agent 系统里，“恢复”很容易被误写成“再跑一次”。这在无副作用、低成本、短任务里也许只是浪费；在有外部系统、写操作、审批、预算或人类协作时，盲目重试会把问题扩大。

最小 Harness 不一定要拥有完整 durable workflow engine，也不一定要支持全量 deterministic replay。但它必须定义：中断时哪些状态已经提交，哪些动作正在进行，哪些副作用不确定，哪些 approval 仍有效，预算还剩多少，能力版本是否漂移，下一步应该 resume、retry、reconcile、compensate、ask 还是 stop。

```text
Interruption / failure
        |
        v
Separate known / unknown
        |
        +--> no side-effect uncertainty -> resume or retry if policy/budget allow
        |
        +--> side-effect uncertain -> reconcile before retry
        |
        +--> authority/context/version stale -> ask or rebuild boundary
        |
        +--> unsafe or over budget -> stop
```

| 合同字段 | Core F 的最小回答 |
|---|---|
| Problem protected | 保护 I6：Recovery 必须从 known / unknown 分离开始，不能等同盲目重试。 |
| Input | committed state、in-flight action、last known evidence、approval state、budget state、capability versions、continuation reason。 |
| Output | resume / retry / reconcile / compensate / ask / stop 决策、recovery preconditions、checkpoint pointer、unknown list。 |
| Dependencies | Runtime 暂停/恢复执行；Workflow 提供状态机位置；Tool Runtime 提供副作用和 idempotency 线索；Policy 检查 authority；Evidence layer 提供 last accepted facts。 |
| Trust boundary | checkpoint file 不是安全 replay 证明；同一 action intent、确定输入、环境版本和副作用边界都要单独判断。 |
| Failure / degradation | in-flight identity 缺失则 stop/ask；副作用不确定则先 reconcile；版本漂移则 review；预算不足则 stop 或请求扩展。 |
| Observable evidence | checkpoint record、recovery decision、unknown list、replay eligibility note。 |
| Interfaces | Runtime 执行恢复动作；业务 Agent 解释 unknown；Tool 支持查询/幂等线索；Workflow 保持状态位置；Policy 决定能否继续；KB/RAG 不把中断状态当已完成事实。 |

这就是为什么本文把“Recovery boundary”列为 minimum core，却把“完整 durable checkpoint engine / full replay”列为 conditional core。

一个最小 Harness 至少要知道自己能不能安全继续；但它不一定要在首版实现 Temporal 级别的 durable execution，也不一定要重演所有外部调用。前者是边界合同，后者是实现深度。把这两者混在一起，要么会过度建设，要么会把不安全的恢复伪装成可靠性能力。

## 11. 条件核心：Budget、HITL、Knowledge 与 Eval 的进入条件

A 到 F 构成最小 Harness 的主骨架。但真实系统通常还会遇到 G 到 J：预算控制、人工决策、知识来源与回归评估。

它们不能被轻率删掉，也不能被一律塞进首版。正确做法是把进入条件写清。

### G. Budget / Step / Cost / Latency Control

Budget 能力保护 I7：稀缺资源必须有停止语义。

它在这些场景下进入 conditional core：

- 任务可能长时间运行；
- 调用会产生显著费用；
- 外部系统限流或配额紧张；
- 动作有风险，不能无限尝试；
- 用户可见延迟会影响体验或业务承诺。

最小形态不一定是完整成本平台。对于 BuildPilot V1 这样的只读建议链，可以先简化成 step cap、time cap、tool-call cap 和明确 stop reason。重要的是系统要能说清：为什么停止，是否可继续，继续需要什么授权或预算，而不是让 Agent “再试试”直到耗尽资源。

### H. HITL + Change Request + Intent Confirmation

HITL 能力保护 I8：人的决定必须成为状态转换，而不是聊天旁白。

一般系统里，HITL 是 conditional core。一个低风险、只读、信息型 assistant 可以先不做完整 review routing。但在 BuildPilot 的设定里，HITL、Change Request 和 Intent Confirmation 是 minimum core。

原因很简单：BuildPilot 的价值主张是 read-only / suggestion-first。它不替 owner 直接改生产系统，而是把需求、观察、风险、建议和复验计划组织成 Change Request，让 owner 在 BuildPilot 外部实施真实修改。既然 owner implementation 是边界本身，human review 就不是“加一个确认按钮”，而是闭环的核心状态。

| 合同字段 | H 的最小回答 |
|---|---|
| Problem protected | 保护 I8：人的澄清、review 和 change decision 必须成为 scoped state transition，避免 suggestion-first 边界被写成自动实施。 |
| Input | ambiguity、finding、current evidence、proposed change request、options、owner identity、expiry / review policy。 |
| Output | clarification request、approval / rejection、change request、owner decision、review trail、re-verification request。 |
| Dependencies | Host 承载澄清与 review UI；业务 Agent 准备 finding / CR；Authority Gate 绑定 scope 与 expiry；Workflow 处理 pause / resume。 |
| Trust boundary | 人类自然语言不能授权无关 action；approval 不能跨 scope、diff、evidence 或时间自动延续；BuildPilot owner implementation remains outside BuildPilot。 |
| Failure / degradation | intent 模糊则 ask before action；rejection 则 stop 或 revise suggestion；无响应则 wait / expire。 |
| Observable evidence | change request record、review decision、re-verification request、decision scope。 |
| Interfaces | Runtime 在等待 owner decision 时暂停/恢复；业务 Agent 产出 clarification / CR；Tool 保持 read-only 除非另行授权；Workflow 路由 review；Policy 保存 scoped decision；KB/RAG 只吸收带 provenance 的 accepted decision。 |

这也是后面 BuildPilot 设计案例里必须保留的边界：BuildPilot 可以把“建议怎样改”写清楚，但不能把“建议已被实施并验证”写成自己的成果。

### I. Knowledge Provenance / Freshness / Intake Control

Knowledge 能力保护 I9：知识必须有来源、时效和进入策略。

只要 Harness 让 memory、RAG、项目知识、历史决策或团队经验影响 action 或 claim，它就必须回答三件事：

- 这条知识来自哪里？
- 它在当前任务里是否仍然新鲜、适用、未被覆盖？
- 它是否允许进入长期知识层，还是只能作为本次 observation / note？

这不是要首版实现一个庞大的知识图谱。最小形态可以只是 provenance、freshness、scope、intake/rejection reason。尤其对 BuildPilot，accepted finding 与 owner decision 若要沉淀，就必须带来源和时效；被 rejected 或证据不足的建议，则不能悄悄变成“项目经验”。

### J. Eval / Golden / Regression Hook

Eval 与 Regression 能力保护 I10：回归声明必须独立于一次成功运行。

本文把完整 Eval / Golden / Regression 平台列为 environment-specific extension，常常可以从第一版 Harness 延后。但“hook”应该在设计上留下位置：当系统开始承诺稳定质量、自动修改、跨版本行为一致、或 production-facing suggestion quality 时，就要能接入 dataset、oracle、manifest、runner、metric、baseline 和 verdict policy。

在 Article 26 里，不能声称 BuildPilot 已经有 regression coverage。也不能因为一个设计案例看起来闭环，就写成质量已经被 Eval 证明。Eval 的入口条件很明确：有固定任务、有可复验判据、有运行记录、有 verdict policy，然后才谈 regression。

## 12. 把这些能力接成一个最小闭环

现在可以把 A 到 F，加上按条件触发的 G 到 J，串成最小 Harness 责任闭环。

```text
Owner Request
   |
   v
[A] Identity / Session / Ownership
   |
   v
[C] Context Policy Envelope
   |
   v
[B] Capability Registry + Trust Filter
   |
   v
[D] Authority Gate
   |
   v
Runtime executes allowed step
   |
   v
[E] Trace / Observation / Evidence / Failure
   |
   v
[F] Checkpoint / Recovery Decision
   |
   +--> continue
   +--> ask human [H]
   +--> stop by budget [G]
   +--> intake knowledge [I]
   +--> regression hook [J]
```

这张图有一个隐藏重点：Runtime 仍然在推进 step，业务 Agent 仍然在做领域判断，Tool 仍然执行外部能力，Workflow 仍然组织业务步骤。Harness 不是把它们吞掉，而是在每一步外侧维持共享治理语义。

对应到最小闭环，Harness 必须持续回答这些问题：

| 闭环问题 | 最小能力 |
|---|---|
| 这是谁的任务？ | A：Identity / Session / Ownership |
| 本步可以看什么？ | C：Context Policy |
| 本步有哪些能力可见且可信？ | B：Capability Registry / Version / Trust |
| 本次动作是否获权？ | D：Authority Gate |
| 发生了什么，能证明什么？ | E：Trace / Evidence / Failure |
| 失败后怎样继续或停止？ | F：Checkpoint / Recovery |
| 资源是否还能继续消耗？ | G：Budget trigger |
| 是否需要人类确认或 owner 决策？ | H：HITL / CR / Intent trigger |
| 知识能否被引用或沉淀？ | I：Knowledge trigger |
| 能否声明回归稳定？ | J：Eval / Regression trigger |

如果一个系统能把这些问题稳定回答出来，即使实现很薄，也具备了最小 Harness 的形状。如果一个系统拥有很多模块，却无法回答这些问题，它只是功能多，不一定是闭环。

## 13. Harness 不吞掉邻居：接口边界

为了避免把 Harness 写成 God Object，还要把它和周围角色再切一遍。

| 角色 | 它负责什么 | 它不应该被 Harness 抢走什么 |
|---|---|---|
| Host | 应用、UI、workspace、外部系统入口、用户交互承载。 | 不自动拥有 approval 语义；UI state 不等于 authority。 |
| Business Agent | 理解业务目标，提出领域判断、finding、建议和解释。 | 不拥有最终执行 authority，不把 claim 当 proof。 |
| Agent Runtime | 推进模型调用、工具分派、循环、状态、等待和停止。 | 不在每条执行链里复制一套彼此漂移的治理语义。 |
| Tool Runtime | Validate、Policy hook、Execute、Result、局部 Trace。 | 不独自决定业务证据是否成立或长期知识能否吸收。 |
| Workflow | 组织业务步骤、分支、暂停、恢复和状态转换。 | 不把局部流程 approval、budget、evidence 变成孤岛规则。 |
| Policy | 定义 permission、approval、sandbox、deny-first 规则。 | 不替业务 Agent 判断领域真相，不替 Evidence 证明 claim。 |
| KB / RAG | 提供带来源的知识候选、检索和沉淀入口。 | 不自动提供当前事实，不授予 action authority。 |
| Evidence Layer | 管理 claim、card、status、证明范围和未知项。 | 不执行工具，不替 Eval 声明 regression。 |

这张表看起来像重复上一篇，其实目的不同。上一篇回答“Runtime 与 Harness 怎么切”；本篇回答“最小 Harness 的 capability contract 怎样不越界”。同样一个能力，放错 owner，就会变味。

例如 Context Assembly 可以由 Runtime 做，但 Context Policy 必须能被 Harness 统一；Tool schema 可以来自 MCP server，但 Trust Filter 不能完全相信外部 annotation；human review 可以由 Host UI 承载，但 decision scope、expiry 和 stale 判断必须进入治理记录；Eval runner 可以是外部 CI，但 regression verdict 不能由一次普通工具成功替代。

好 Harness 不是什么都管。好 Harness 管的是那些一旦分散就会漂移的语义。

## 14. BuildPilot：只读建议链里的最小闭环

现在把模型落到 BuildPilot。

先再次冻结边界：

```text
BuildPilot in Article 26:
  COURSE PROPOSAL
  DESIGN CASE
  NOT IMPLEMENTED
  NOT RUN
  READ-ONLY
  SUGGESTION-FIRST
  Owner implements real change outside BuildPilot
```

这意味着，本文不会写 BuildPilot 已经有代码、已经跑过、已经接入 Unity、已经扫描 Jenkins、已经创建 PR、已经部署、已经减少缺陷。BuildPilot 在这里是一面设计镜子，用来检查最小 Harness 能不能支撑一条生产建议闭环。

设想一个只读的需求变更分析场景：owner 让 BuildPilot 查看某个变更需求与现有项目材料，输出 finding 和 Change Request。它不直接改代码，只给出证据支持的建议，并等待 owner 在外部实施后再设计复验。

这条最小闭环可以拆成九步：

| 步骤 | BuildPilot 设计动作 | 对应 Harness 能力 | 证据边界 |
|---|---|---|---|
| 1. Requirement intake | Host 记录 owner request、workspace、session；Harness 标记 `READ-ONLY / SUGGESTION-FIRST`；业务 Agent 解析意图。 | A、C、H | 只是需求进入，不是修改授权。 |
| 2. Intent confirmation | 需求目标、文件范围、验收口径或风险不清时，先向 owner 澄清。 | A、C、H | 澄清是状态转换；不是执行结果。 |
| 3. Capability discovery | 只暴露相关的只读 source / config / build-report / log 能力；未知或写能力隐藏或另需授权。 | B、D | 可见能力不等于写权限。 |
| 4. Restricted checks | Runtime 执行获准的只读检查；Tool Runtime 返回 observation；Context Policy 记录来源、时效和排除项。 | C、D、E、G | 观察不自动成为 accepted Evidence。 |
| 5. Finding | 业务 Agent 把 observation 组织成候选 finding，标注 OBSERVED / INFERRED / UNKNOWN / NOT_PROVEN。 | E、I | finding 不是修复，也不是 owner 决策。 |
| 6. Change Request | BuildPilot 生成带证据、影响、风险、owner action 和 re-verification plan 的建议。 | H、E、F | CR 是 suggestion-first artifact，不改代码、不建 PR、不跑 Jenkins、不部署。 |
| 7. Human Review | owner 接受、拒绝或修改建议；Harness 保存 decision scope、expiry 和复验要求。 | H、D、A | 人的决定必须绑定 scope，不授权无关动作。 |
| 8. Re-verification | owner 在 BuildPilot 外实施后，Runtime 在允许范围内重新执行只读检查。 | D、E、F、G | 本文未运行复验；这里只是设计步骤。 |
| 9. Evidence / Knowledge intake | accepted finding / owner decision 进入知识层候选；rejected、uncertain 或过期材料保留 unknown / exclusion。 | E、I、J hook | 知识沉淀需要 provenance/freshness；不产生 regression claim。 |

这九步说明一件事：BuildPilot 的最小闭环不是“替人改代码”，而是把需求、上下文、能力、权限、观察、证据、建议、人工决策、复验和知识进入规则串成可审计状态。

用流程图看，它大概是这样：

```text
Owner request
   |
   v
Intake + session + read-only boundary
   |
   v
Intent clear?
   | no
   +--> ask owner -> update decision state
   |
  yes
   v
Read-only capability discovery
   |
   v
Restricted checks
   |
   v
Finding with evidence status
   |
   v
Change Request proposal
   |
   v
Human review
   |
   +--> reject / revise -> stop or update CR
   |
   +--> accept -> owner implements outside BuildPilot
                      |
                      v
              design-only re-verification path
                      |
                      v
              evidence / knowledge intake candidate
```

注意最后两步的措辞。Owner 在 BuildPilot 外部实施真实修改；re-verification 在本文中仍是设计路径，不是已运行证据。只有未来真的存在运行记录、检查输出和 evidence acceptance，才能把某个 finding 从 proposal / partial 的语言升级为更强状态。

## 15. BuildPilot V1 可以延后的能力

因为 BuildPilot 在本文中只是只读建议案例，它不需要在 Article 26 里吞掉所有未来能力。

| 能力 | Article 26 处理方式 | 为什么可以延后 | 不能丢掉的边界 |
|---|---|---|---|
| Autonomous code modification | Out of scope | BuildPilot 是 read-only / suggestion-first。 | Change Request 与 owner external implementation。 |
| Branch / PR creation | Out of scope | 本文不授予写权限，也不产生仓库副作用。 | 如果未来加入，必须重新进入 Authority Gate 和 Evidence Gate。 |
| Production deployment | Out of scope | 没有运行、CI、部署或生产证据。 | 不能把建议写成上线结果。 |
| Full eval platform | Deferred | Article 26 没有 Eval lab 或 golden dataset。 | 保留 Eval / Regression hook，不声明 regression。 |
| Governed capability evolution | Deferred | 首版只需记录 capability id/version/trust；复杂治理留给后续。 | 版本漂移必须能 block / ask / review。 |
| Multi-project knowledge graph | Deferred | V1 可先做 provenance/freshness/intake。 | 知识进入必须可追溯、可拒绝、可标 stale。 |
| Cost optimization strategy | Deferred | V1 可先用 step/time/tool-call cap。 | 稀缺资源必须有 stop reason。 |
| Article 27 adoption staging | Future article | 本篇不讨论采纳阶段、复杂度、Bloat、替换性和什么时候不值得建。 | 不提前写完 Article 27。 |

这张表其实是在保护最小模型。一个 Harness 如果第一天就把所有高级能力都拉进来，很容易变成又大又脆的治理平台；一个 Harness 如果完全不留这些能力的位置，又会在系统变大时重写边界。

比较好的做法是：首版薄实现，强合同。能力可以薄，边界不能糊。

## 16. 最小 Harness 常见的坏写法

判断一个 Harness 是否“最小但闭环”，不只看它有什么，也看它怎样写坏。

| 坏写法 | 破坏的不变量 | 更稳的写法 |
|---|---|---|
| 把所有工具 schema 直接暴露给模型 | I2、I4 | Capability Registry 先过滤 actor、scope、version、trust，再暴露 allowed view。 |
| 用聊天里的“可以”当永久 approval | I1、I4、I8 | approval 绑定 actor、action、resource、scope、expiry 和 request digest。 |
| 把上下文摘要当事实来源 | I3、I9 | 摘要要有 source receipt；缺 provenance 就标 unknown 或 exclude。 |
| Tool 返回成功就写 evidence confirmed | I5 | Tool result 先变 observation，再按 Evidence Contract 接受或拒绝。 |
| Checkpoint 等于保存整个状态对象 | I6 | checkpoint 要记录 committed state、in-flight action、unknown、副作用和恢复前提。 |
| 失败后默认 retry | I6、I7 | 先判断 same intent、budget、authority、side-effect uncertainty，再决定 retry/reconcile/ask/stop。 |
| Budget 只统计 token，不定义停止 | I7 | 至少要有 step/time/tool-call cap 和 stop reason。 |
| Human review 只是评论区留言 | I8 | review decision 必须成为 scoped state transition。 |
| Knowledge Base 自动吸收所有结论 | I9 | 只有 accepted finding / decision 带 provenance/freshness 后才能 intake。 |
| 一次复验通过就说 regression solved | I10 | regression 需要 golden cases、oracle、manifest、verdict policy 和运行证据。 |
| “BuildPilot suggestion = fix completed” | I5、I8、I10 | BuildPilot 只给 suggestion/CR；owner 外部实施；复验未运行前不得升级。 |

这些坏写法的共同点是偷换边界：把可见写成可用，把观察写成证据，把建议写成修复，把人话写成授权，把历史写成当前，把一次成功写成回归。Harness 的最小能力模型，本质上就是防止这些偷换长期扩散。

## 17. 本篇建立了什么，没有证明什么

到这里，本文可以建立的结论是：

- 最小 Harness 应该从跨 Run、跨 Tool、跨 Workflow 的不变量推导，而不是从厂商功能菜单推导。
- Identity / Session / Ownership 是最小核心，因为所有后续权限、证据、恢复和责任记录都需要归属。
- Capability Registry / Version / Trust Filter 是最小核心，因为能力存在、可见、可信、获权、可执行、可采信是不同问题。
- Context Policy 是最小核心，即使具体 Context Assembly 可由 Runtime 执行。
- Permission / Approval / Sandbox / Policy 应形成拒绝优先、使用时检查的 Authority Gate。
- Trace / Evidence / Failure 是最小核心；Replay 是条件核心，不应被 Trace 自动推出。
- Checkpoint / Recovery Decision Boundary 是最小核心；完整 durable checkpoint engine 是条件实现深度。
- Budget、HITL、Knowledge、Eval/Regression 都需要明确进入条件；BuildPilot 让 HITL、Intent Confirmation 和 Change Request 成为它的核心边界。
- BuildPilot 可以被映射为一个从 intake 到 evidence/knowledge intake candidate 的设计闭环，但它仍然是 `NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。

本文没有证明的是：

- 没有证明存在一个统一行业标准叫 Harness，且所有产品都按本文切分。
- 没有证明某个 SDK、框架或产品已经完整实现本文的最小模型。
- 没有证明 BuildPilot 已经存在、运行、扫描 Unity、调用 Jenkins、创建 PR、修改代码、部署或降低缺陷。
- 没有证明 Trace 自动支持 Replay，也没有证明一次成功运行等于 Regression。
- 没有给出 Article 27 的采纳阶段、复杂度、Bloat、替换性或“不值得建设”的判断。
- 没有进入 Part VI 的 DeepSeek Harness pinned source / runtime evidence。
- 没有设计 Part VII 的 BuildPilot Runtime 或完整实现架构。

这些限制不是削弱文章，而是让模型有边界。Agent Engineering 里最危险的不是少说一点，而是把 proposal 写成 runtime proof。

## Claim Traceability（11 / 11）

| Claim | Status | Evidence | 本文覆盖位置 | 边界说明 |
|---|---|---|---|---|
| `26-C01` | `PROPOSAL` | `26-E01` | 1、2、3、17 | minimum model 是课程方法，不是外部标准。 |
| `26-C02` | `PARTIAL` | `26-E02` | 2、3、5、12、14 | attribution ledger 是课程综合，不等同某个 SDK Session。 |
| `26-C03` | `PARTIAL` | `26-E03` | 3、6、12、14、16 | discovery/schema/trust split 有来源支撑；版本治理是课程综合。 |
| `26-C04` | `PARTIAL` | `26-E04` | 3、7、12、14 | Context Policy 是 minimum；具体 assembly 可由 Runtime 承担。 |
| `26-C05` | `PARTIAL` | `26-E05` | 3、8、12、14、16 | deny-first authority gate，不是完整 IAM 或安全证明。 |
| `26-C06` | `PARTIAL` | `26-E06` | 9、12、14、16 | Trace/Evidence/Failure 是 minimum；full replay conditional。 |
| `26-C07` | `PARTIAL` | `26-E07` | 10、12、14、16 | Recovery boundary minimum；durable workflow/replay engine conditional。 |
| `26-C08` | `PARTIAL` | `26-E08` | 3、11、12、14、15 | Budget 对长、贵、限流或延迟敏感任务是 conditional core。 |
| `26-C09` | `PARTIAL` | `26-E09` | 3、11、14、16 | HITL 一般 conditional；BuildPilot 因 suggestion-first 而 core。 |
| `26-C10` | `PROPOSAL` | `26-E10` | 3、11、15、16 | Knowledge/Eval 分类是设计提案；无 runtime/eval evidence。 |
| `26-C11` | `PROPOSAL` | `26-E11` | 14、15、17 | BuildPilot loop 是 design-only，`NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。 |

这张表也提醒读者：`11 / 11` 是 traceability，不是 `11 / 11 CONFIRMED`。本篇的证据状态仍是 `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。

## Learning Check

读完后，可以用下面这些问题自测。

1. 为什么最小 Harness 不能从功能菜单开始？

   参考思路：功能菜单只能说明“哪些东西看起来有用”，不能说明哪些不变量缺失后治理语义会断。最小模型应该从跨 Run、跨 Tool、跨 Workflow 必须长期成立的边界推导。

2. Capability 为什么不等于 Tool？

   参考思路：Tool 是具体外部能力或动作接口；Capability 是可被授予和治理的一类能力契约。模型看见 Tool schema，只是 capability visibility，不是 authority。

3. Session 为什么不只是 history？

   参考思路：history 是素材；Session 是可追踪、可恢复、可治理的交互与执行边界，需要绑定 actor、owner、workspace、task scope 和 continuation boundary。

4. Context Policy 与 Context Assembly 有什么区别？

   参考思路：Assembly 可以由 Runtime 负责具体打包；Policy 要定义哪些来源可用、哪些被排除、是否过期、能否压缩复用、是否需要 receipt。

5. Approval 为什么必须绑定 request digest、scope 和 expiry？

   参考思路：否则一次聊天里的同意会被误用到不同动作、不同资源、不同风险或过期上下文里，破坏 use-time authority。

6. Trace 为什么不是 Evidence？

   参考思路：Trace 记录发生历史；Evidence 支撑某个 claim，并声明证明范围与不证明范围。发生过不等于结论被接受。

7. Recovery 为什么不能等同 retry？

   参考思路：恢复要先分离 known / unknown，判断 in-flight action、副作用、预算、authority、版本和 evidence，再决定 resume、retry、reconcile、compensate、ask 或 stop。

8. Budget 为什么是 conditional core？

   参考思路：长任务、付费、限流、风险动作或用户可见延迟下，预算必须进入核心；低风险短任务可以用更薄的 stop semantics。

9. HITL 为什么对 BuildPilot 是 core？

   参考思路：BuildPilot 的边界是 read-only / suggestion-first，真实修改由 owner 在外部实施。因此 intent confirmation、Change Request 和 owner review 是闭环本体，不是附加 UI。

10. Knowledge intake 为什么不能自动吸收所有 finding？

    参考思路：finding 必须有 provenance、freshness、scope 和 acceptance；rejected、uncertain、stale 或证据不足的内容不能进入长期知识层当事实。

11. Eval / Regression 为什么可以延后，但不能没有 hook？

    参考思路：首版不一定要完整 Eval 平台；但一旦系统承诺可重复质量、自动修改或稳定 suggestion quality，就需要接入 dataset、oracle、manifest、metric 和 verdict policy。

12. BuildPilot minimum loop 的九步是什么？

    参考思路：Requirement intake、Intent confirmation、Capability discovery、Restricted checks、Finding、Change Request、Human Review、Re-verification、Evidence / Knowledge intake。

13. 本篇为什么反复写 `NOT IMPLEMENTED / NOT RUN`？

    参考思路：因为 BuildPilot 在 Article 26 只是设计案例，没有 runtime、lab、PR、CI、部署或生产证据。保持状态标签是防止 proposal 被写成 runtime proof。

14. 本篇把哪些内容留给 Article 27？

    参考思路：采纳阶段、复杂度、Bloat、可替换性、演化成本，以及哪些场景不值得建设 Harness。

15. 最小 Harness 的一句话判断标准是什么？

    参考思路：它能否持续回答谁在行动、什么能力可见且获权、上下文从哪来、什么能当证据、失败后怎样恢复、何时问人或停止、知识和回归声明是否有效。

## 参考资料 / 证据边界

本文依据本课程已有 Research / Evidence source manifest：Microsoft Agent Framework Harness、Tool Approval 与 Workflow HITL，OpenAI Agents SDK Sessions、Guardrails 与 Sandbox，MCP `2025-06-18` Tools / Schema，OpenTelemetry `1.60.0` Trace / Baggage，Temporal durable execution / retry 文档，GitHub CODEOWNERS / branch protection，以及本课程已发布的 Articles 06、07、10、11、18、19、20、21、22、24、25。

这些资料支持本文讨论 identity、capability、context、authority、trace、recovery、HITL、knowledge 与 regression hook 等责任区域；不证明 Article 26 的“最小 Harness”分类是行业标准，也不证明所有产品都应采用同一最小集合。本文 taxonomy 是课程模型；BuildPilot 仍然只是 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`，没有 runtime、lab、PR、CI、Jenkins、Unity 或生产验证证据。

## 最短结论

Harness 的最小能力模型，不是“把所有 Agent 平台功能做一遍”。它是一组围绕共享治理不变量的责任合同。

Identity / Session / Ownership 让任务有归属；Capability Registry 让能力可见性、版本和信任可审查；Context Policy 让模型看到什么有边界；Authority Gate 让外部动作拒绝优先；Trace / Evidence / Failure 让发生历史与证明范围分开；Checkpoint / Recovery 让中断后的继续不等于盲目重试。Budget、HITL、Knowledge 和 Eval 则按风险、承诺和场景进入核心或延后。

用 BuildPilot 来看，这个模型的意义不是让 Agent 自动接管生产变更，而是让只读建议链也能有清楚的 owner、scope、capability、evidence、review 和 re-verification 边界。Owner 在 BuildPilot 外实施真实修改；BuildPilot 在本文中仍然只是设计案例。

下一篇才适合继续问：既然这些能力构成了最小 Harness，什么时候值得建？什么时候会变成 bloat？怎样保证可替换性和演化成本不把团队拖住？那是 Article 27 的问题。本文只先把底座钉住：能力可以薄，边界不能糊；实现可以晚，证据不能乱。
