# Article 26 Outline｜Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery

## Outline contract

- Article Type: `PRINCIPLE`
- Course Weight: `L / Major Core Lesson`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Teaching Spine: Article 24 established why shared governance pressure appears -> Article 25 split Runtime / Harness / Host / Business responsibilities -> Article 26 derives minimum Harness capability from invariants, not vendor feature menus -> classify ten candidate areas into minimum core / conditional core / environment-specific extension -> define admitted core capability contracts by problem, input, output, dependency, trust boundary, failure/degradation, observable evidence and interfaces -> map a read-only BuildPilot closed loop -> preserve Article 27 trade-off/adoption and Part VI implementation as non-scope
- Core Claim Scope: `26-C01`-`26-C11` only；不新增 core Claim / Evidence Card
- Evidence Posture: `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Claim Coverage: `11 / 11`
- Evidence Cards: `11 / 11`
- Proposal Discipline: Article 26 的 minimum model 是课程模型；不能写成行业标准、厂商标准、唯一架构或 DeepSeek Harness 源码事实。
- Minimum Discipline: "最小核心"只允许从不变量推导；不得把十个候选区全部判定为 Mandatory，也不得把条件核心和延后扩展伪装成首版必做平台。
- BuildPilot Discipline: BuildPilot 只作为 bounded design case；必须持续保留 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。它不修改代码、不创建 PR、不调用 Unity / Jenkins / CI、不部署、不产生运行或生产证据。
- Future Boundary: Article 27 才讨论收益、代价、Bloat、可替换性、采纳阶段和不值得建设的情形。Part VI 才进入 DeepSeek Harness pinned source/runtime evidence。Part VII 才展开 BuildPilot Design v1。
- Draft fact boundary: Draft 只能重组 `research.md`、`evidence.md`、Article 26 card/README、series plan、glossary、Published Articles 24/25 和必要前文边界。若需要新的外部事实、实现事实、运行观测、BuildPilot 证据或完整 product API schema，必须 `RETURN_TO_RESEARCH`。

> 如果这篇只记一句话：`最小 Harness 不是功能最少的菜单，而是能持续回答谁在行动、什么能力可见且获权、哪些上下文可用、什么能被当作证据、失败后从哪里恢复、何时停止或问人，以及知识和回归声明是否仍然有效的最小责任闭环。`

## Reader transformation

读者开始时可能会把 Harness minimum 理解成一张功能清单：Session、Tools、Policy、Trace、Budget、Replay、Eval、Knowledge 全都写上去就像完整了。文章结束时，读者应能：

1. 解释为什么 Harness minimum 必须从跨 run、跨 tool、跨 workflow 的不变量推导，而不是从厂商功能菜单拼装。
2. 区分 `MINIMUM CORE`、`CONDITIONAL CORE`、`ENVIRONMENT-SPECIFIC EXTENSION / DEFERRED` 三类能力。
3. 说清 Identity / Session / Ownership 为什么是后续 permission、trace、evidence、recovery 和 owner review 的归属前提。
4. 区分 capability existence、visibility、relevance、authority、execution 和 evidence acceptance，不把 tool list 当授权。
5. 说明 Context Assembly 可以由 Runtime 执行，但 Context Policy 必须定义可见性、隔离、来源、保留、裁剪、复用和 freshness 边界。
6. 把 Permission、Approval、Sandbox、Policy Enforcement 写成 use-time authority gate，而不是一句 prompt rule 或一次 UI click。
7. 分清 Trace、Evidence、Failure Taxonomy、Replay 的关系：trace 记录发生，evidence 接受主张，failure taxonomy 分类，full replay 条件更强。
8. 解释 Checkpoint / Recovery minimum 是决策边界，不等于完整 durable workflow engine 或 safe replay engine。
9. 判断 Budget、HITL、Knowledge、Eval 为什么不能一律强制进入首版，但在特定风险条件和 BuildPilot 场景下会变成 core surface。
10. 用 BuildPilot 的只读 suggestion-first 链路，把 intake、intent confirmation、capability discovery、restricted checks、finding、change request、human review、re-verification、evidence / knowledge intake 串成闭环。
11. 保持 Evidence posture：`0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`，不把 proposal 或 partial 写成 confirmed。

## Teaching Spine

```text
Article 24 says cross-cutting governance needs a shared carrying boundary
  -> Article 25 says Runtime runs, Harness governs shared semantics
  -> Article 26 asks what the smallest closed Harness capability model must keep
  -> start from invariants, not from feature names
  -> classify ten candidate areas by whether they protect non-negotiable invariants
  -> admit a small mandatory core: attribution, capability governance,
     context policy, authority gate, trace/evidence/failure, recovery boundary
  -> treat budget, HITL, knowledge and eval as conditional or deferred by risk
  -> define every admitted capability as a contract, not an implementation module
  -> map contracts to Runtime, Business Agent, Tool, Workflow, Policy and KB interfaces
  -> use BuildPilot only as read-only suggestion-first design case
  -> close by handing Article 27 the adoption and bloat question
```

### Spine checkpoints

| Stage | Reader transformation | Required article artifact | Failure if omitted |
|---|---|---|---|
| Problem and invariants | 从"要哪些功能"转向"哪些不变量不能破" | invariant table + bad feature-menu opening | 文章变成 SDK checklist |
| Classification | 能把十个候选区分成 minimum / conditional / deferred | candidate classification table | 把所有能力都写成 mandatory |
| Core capability contracts | 能看到每个核心能力保护什么、输入输出是什么、失败怎么降级 | A-F contract sheets + BuildPilot-specific H/G/I note | 抽象模型缺少工程落点 |
| Interfaces | 能说清 Harness 不吞 Runtime、Business Agent、Tool、Workflow、Policy、KB | interface matrix | Harness 滑向 God Object |
| BuildPilot mapping | 能把模型落到一个只读建议闭环 | nine-step BuildPilot loop table | BuildPilot 被误写成实现事实 |
| Evidence boundary | 能保持 claim status、lab/runtime absence 和 future-article non-scope | claim traceability + no-new-fact boundary | proposal 被升级、Article 27 被提前写完 |

## Opening bridge｜Article 24 / 25 留下的问题：最小 Harness 到底少不了什么

- Reader Question: 如果已经知道 Harness 为什么出现，也知道 Runtime 和 Harness 怎样分工，为什么还要一篇 minimum model？
- Core Questions: `26-C01`、`26-C02`、`26-C03`、`26-C05`、`26-C11`。
- Claims / Evidence: `26-C01 PROPOSAL / 26-E01`，`26-C02 PARTIAL / 26-E02`，`26-C03 PARTIAL / 26-E03`，`26-C05 PARTIAL / 26-E05`，`26-C11 PROPOSAL / 26-E11`，辅助 Published Articles 24/25。
- Planned teaching move:
  - 用 Article 24 的终点开场：共享边界已经出现，因为治理词会跨 prompt / tool / workflow 漂移。
  - 用 Article 25 的终点接住：Runtime 负责推进一次 run，Harness 负责共享治理语义。
  - 提出本文的新问题：当团队要真正做一个最小 Harness 时，不能把所有听起来重要的东西都塞进来；要先问哪些不变量一旦破掉，系统就无法归属、授权、审计、恢复或停止。
  - 立刻声明证据上限：本文是 `11 / 11` Claims / Cards 的课程模型，状态 `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；Required Lab=`NONE`；Runtime Observation=`ABSENT`。
- Boundary / Non-goal:
  - 不重讲 Article 24 的 Why，也不重画 Article 25 的 Runtime / Harness / Host / Business 总表。
  - 不提前写 Article 27 的 stage、cost、bloat 和 adoption framework。
  - 不声称 BuildPilot 已实现或运行。
- Transition purpose: 从"责任边界已经切清"推进到"最小能力怎样由不变量推导"。
- Learning check: 为什么不能直接把厂商 Harness / Agent Framework 的功能列表拿来当本文 minimum？期望答案：产品打包方式不是课程责任边界；minimum 必须由跨 run / tool / workflow 仍需成立的不变量推导。
- Section takeaway: **Article 26 不问 Harness 能有多少功能，而问没有哪些责任就无法形成闭环。**

## Part A｜问题空间：最小不是少功能，而是不变量少不了

### 1. 功能菜单会把"重要"误写成"必选"

- Reader Question: 为什么直接列 Session、Tools、Policy、Trace、Budget、Eval、Knowledge 会写歪？
- Core Questions: `26-C01`、`26-C08`、`26-C10`。
- Claims / Evidence: `26-C01 PROPOSAL / 26-E01`，`26-C08 PARTIAL / 26-E08`，`26-C10 PROPOSAL / 26-E10`。
- Planned teaching move:
  - 用一个坏起点开场：团队看到公开框架有 planning、todo、history、approval、sandbox、sessions、guardrails、tracing、eval，于是把这些全列成 "Harness v1 required"。
  - 说明问题不在这些能力不重要，而在它们的进入条件不同：有些保护 attribution / authority / evidence / recovery 的底线，有些只在长任务、付费调用、RAG、回归承诺或组织 review 出现后才变成 core。
  - 把 "minimum" 改写成 "minimum closed responsibility loop"：能回答 who / what / context / authority / observation / evidence / failure / recovery / stop / human / knowledge 的最小闭环。
- Required table `T26-01`:

  | Feature-menu shortcut | Why it is too coarse | Minimum-model question |
  |---|---|---|
  | 看到 framework 有 session，就说 session store 必做 | SDK session object 可能只是 history 容器 | 谁在行动、为谁行动、哪个任务边界可恢复？ |
  | 看到 tool list，就说 capability registry 已解决 | tool schema / annotation 不是 authority | 能力存在、可见、相关、获权、执行和证据是否分开？ |
  | 看到 trace，就说 audit/evidence 完整 | trace 不接受 claim | 哪些 observation 被接受为 evidence，哪些仍 unknown？ |
  | 看到 checkpoint，就说 recovery 完整 | checkpoint 不保证 safe replay | effect、approval、budget、version 和 unknown 是否足够恢复？ |
  | 看到 eval，就说可靠性闭环完成 | 一次 eval 或未运行 hook 不等于 regression gate | 是否已有 golden、oracle、metric、baseline、manifest 和 verdict policy？ |

- Boundary / Non-goal:
  - 不否定公开框架能力；只说明不能按功能名决定本课程 minimum。
  - 不给所有团队统一建设路线；Article 27 处理 adoption。
- Transition purpose: 既然功能名不够，需要先写不变量。
- Practical action: 评审 Harness 设计时先把每个功能背后的 invariant 写出来；写不出 invariant 的功能先不要进 minimum core。
- Section takeaway: **最小模型不是删到只剩两个模块，而是只保留那些没有它就无法闭环的不变量承载。**

### 2. 十条不变量：从什么必须长期成立开始

- Reader Question: Harness minimum 要保护的底层事实是什么？
- Core Questions: `26-C01`-`26-C10`。
- Claims / Evidence: `26-E01`-`26-E10`。
- Required figure/table `T26-02`:

  | Invariant | If broken | Capability pressure |
  |---|---|---|
  | `I1 Stable actor / session / ownership` | 后续 run 无法解释谁在行动、为谁行动、在哪个任务边界内行动 | Identity、Session、Ownership Ledger |
  | `I2 Capability visibility is not capability authority` | tool 或 skill 被看见就被误当安全、相关且授权 | Capability Registry、Version、Trust Filter |
  | `I3 Context provenance and isolation survive compaction/reuse` | 旧材料、敏感材料或越界材料被静默复用 | Context Policy Envelope |
  | `I4 Authority is checked at use time` | 旧 approval、宽权限或 UI 事件变成空白支票 | Permission、Approval、Sandbox、Policy Enforcement |
  | `I5 Observation is not accepted evidence` | 日志、HTTP 200 或 tool success 被写成结论 | Evidence Contract、Trace Linkage |
  | `I6 Recovery starts from known / unknown separation` | resume / retry / replay 复制副作用或掩盖缺失状态 | Checkpoint / Recovery Boundary |
  | `I7 Scarce resources have stop semantics` | token、时间、cost、tool call 在长链路里无共享停止规则 | Budget / Step / Cost / Latency Control |
  | `I8 Human decision is a state transition` | review、reject、clarify、adopt 变成聊天旁白，无法审计 | HITL、Change Request、Intent Confirmation |
  | `I9 Knowledge has source, freshness and intake policy` | memory / RAG 把 stale 或 untrusted 材料变成当前事实 | Knowledge Provenance / Freshness Controls |
  | `I10 Regression is separate from one successful run` | 修复跑通一次后未来回归不可见 | Eval / Golden / Regression Hook |

- Planned teaching move:
  - 每条 invariant 不写成长定义，只写"破了会怎样"。
  - 明确它们不是十个必做系统，而是十个 pressure source。
  - 后文所有 capability classification 都回指这些 invariant。
- Boundary / Non-goal:
  - 不把 I7-I10 一律拉成首版 mandatory。
  - 不声称这些 invariant 是外部标准文本；它们是研究证据支持下的课程设计模型。
- Transition purpose: 有了 invariant，才能把十个候选能力分级。
- Learning check: `Tool returns success` 能保护哪条 invariant？不能保护哪条？期望答案：只能提供 observation；不能替代 I4 authority、I5 evidence acceptance 或 I6 recovery safety。
- Section takeaway: **不变量是 Harness minimum 的入口；功能名只是后面的实现候选。**

### 3. 十个候选区的分级：不是每个都 Mandatory

- Reader Question: 哪些候选能力进入最小核心，哪些按场景进入，哪些延后？
- Core Questions: `26-C01`、`26-C07`、`26-C08`、`26-C09`、`26-C10`。
- Claims / Evidence: `26-C01 PROPOSAL / 26-E01`，`26-C07 PARTIAL / 26-E07`，`26-C08 PARTIAL / 26-E08`，`26-C09 PARTIAL / 26-E09`，`26-C10 PROPOSAL / 26-E10`。
- Required table `T26-03`:

  | Candidate area | Article 26 classification | Why / wording ceiling |
  |---|---|---|
  | Identity / Session / Ownership | `MINIMUM CORE` | 没有归属边界，后续 authority、trace、evidence 和 recovery 都不可归属；不等同任一 SDK session object。 |
  | Context Assembly and isolation | `MINIMUM CORE for policy and isolation; Runtime owns concrete assembly` | Harness 定义可暴露、保留、裁剪、引用、复用和隔离规则；Runtime 可以执行具体 packing。 |
  | Tool/Skill Capability Registry and version | `MINIMUM CORE` | capability discovery / schema 是显式事实，但 annotation 和 visibility 不是 authority。 |
  | Permission, Approval, Sandbox and Policy Enforcement | `MINIMUM CORE` | 暴露外部动作的 Harness 必须有 deny-first use-time authority path；不要求首版完整 IAM。 |
  | Execution Control, State, Checkpoint and Recovery | `MINIMUM CORE as boundary contract; durable checkpoint engine is CONDITIONAL CORE` | Harness 必须定义 stop/resume/retry/recover 语义；完整 durable engine 视风险和长任务决定。 |
  | Trace, Evidence, Replay and Failure Taxonomy | `MINIMUM CORE for Trace/Evidence/Failure; Replay is CONDITIONAL CORE` | audit 需要 trace/evidence/failure 标签；full replay 依赖确定性、环境和副作用边界。 |
  | Budget / Step / Cost / Latency Control | `CONDITIONAL CORE` | 长任务、付费、限流、风险高或用户可见延迟时 core；低风险 one-shot 可简化。 |
  | Human-in-the-loop and Change Request | `CONDITIONAL CORE; MINIMUM CORE for BuildPilot` | suggestion-first 生产建议必须 owner review；纯信息型低风险 assistant 可延后。 |
  | Evaluation, Golden Cases and Regression hook | `ENVIRONMENT-SPECIFIC EXTENSION, often DEFERRED from first Harness slice` | 行为被依赖后需要 hook；首版不等于 full eval platform。 |
  | Knowledge provenance, freshness and Intent confirmation | `CONDITIONAL CORE` | 使用 memory/RAG/project knowledge 影响 action 或 claim 时必须有 provenance/freshness；BuildPilot 的 intent confirmation 核心。 |

- Required framing:
  - "Minimum core" means any serious Harness around external actions should answer this question from day one.
  - "Conditional core" means the feature becomes core once the triggering risk exists; it is not optional decoration after that.
  - "Deferred / extension" means the article can name the hook and evidence boundary, but must not promise an implemented platform.
- Boundary / Non-goal:
  - 不写 Article 27 的分阶段采用路线。
  - 不把 `CONDITIONAL CORE` 说成 "可以永远不管"。
- Transition purpose: 分级之后，进入核心能力合同。
- Section takeaway: **最小模型的第一步，是敢于说哪些能力现在必须有、哪些必须等条件出现才有、哪些只保留接口和证据边界。**

## Part B｜抽象模型：能力不是模块名，而是责任合同

### 4. Capability contract template：每个核心能力都要能被审查

- Reader Question: 一项能力怎样才算进入 Harness model，而不只是好听的模块名？
- Core Questions: `26-C02`-`26-C07`。
- Claims / Evidence: `26-E02`-`26-E07`。
- Required figure `F26-01`:

  ```text
  Capability contract
    -> problem / invariant protected
    -> inputs
    -> outputs
    -> dependencies
    -> trust boundary
    -> failure / degradation
    -> observable evidence
    -> interfaces
  ```

- Required table `T26-04`:

  | Contract field | It prevents | Draft instruction |
  |---|---|---|
  | Problem / invariant | 功能为了存在而存在 | 每个能力都回指 I1-I10 |
  | Inputs | 自然语言猜 state | 写清 actor、scope、request、tool schema、source、budget 等输入 |
  | Outputs | 模糊状态词 | 写成 session envelope、allowed view、decision id、evidence status、recovery decision |
  | Dependencies | God Object | 说明依赖 Host、Runtime、Tool、Policy、KB 哪些输入 |
  | Trust boundary | UI / annotation / memory 冒充 authority | 明确哪些东西只是 hint、observation 或 stale candidate |
  | Failure / degradation | 隐式继续 | 写 deny、ask、stop、degrade、unknown、not-run |
  | Observable evidence | 完成声明无证据 | 指向 registry snapshot、trace id、approval record、evidence card 等 |
  | Interfaces | Harness 吞业务 | 明确 Runtime / Business Agent / Tool / Workflow / Policy / KB 各自交互 |

- Planned teaching move:
  - 告诉读者：本文不会设计类图、数据库 schema 或 product API，而是给出 capability contract。
  - 每个 contract 都用同一组字段，让 Reviewer 能检查是否有遗漏。
  - 后续 Draft 每个核心能力都用短段落 + 小表呈现，避免堆成大一张无法阅读的百科表。
- Boundary / Non-goal:
  - 不引入新核心事实或伪实现。
  - 不把 contract 字段说成唯一 schema。
- Transition purpose: 用统一模板逐个展开 admitted core。
- Practical action: 评审现有 Agent 系统时，挑一项 concern 填这八格；空格越多，越说明它只是命名而不是能力。
- Section takeaway: **进入 Harness minimum 的能力，必须能说明它守住什么、不信任什么、失败时怎样停止，以及把什么证据留给后续审查。**

### 5. Core A｜Identity + Session + Ownership Ledger

- Classification: `MINIMUM CORE`
- Reader Question: 为什么身份、会话和 owner 不是元数据，而是所有治理记录的归属边界？
- Core Questions: `26-C02`。
- Claims / Evidence: `26-C02 PARTIAL / 26-E02`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I1`。后续 permission、approval、trace、evidence、budget、recovery、human review 都要知道谁在行动、为谁行动、在哪个 session / task / workspace 边界内行动。 |
  | Inputs | user / owner identity、actor role、task id、session id、host/workspace id、timestamp、previous-session reference、requested scope。 |
  | Outputs | session envelope、ownership record、actor binding、continuation boundary、trace correlation id seed。 |
  | Dependencies | Host identity/session source、Runtime run id、Policy store、Workflow task boundary。 |
  | Trust boundary | Host 当前 UI 状态不是长期 authority；SDK session object 不自动等于课程 Session；聊天历史不是 owner decision。 |
  | Failure / degradation | actor/session missing -> fail closed；owner ambiguous -> ask；session stale -> open new boundary or require confirmation；workspace mismatch -> stop or relabel. |
  | Observable evidence | session record、owner decision record、trace correlation id、task/workspace binding。 |
  | Interfaces | Runtime consumes run/session id；Business Agent reads owner goal；Tool receives scoped actor；Workflow stores task boundary；Policy checks actor/scope；KB tags source scope。 |

- Draft move:
  - 用 "没有归属，就没有治理" 做核心句。
  - 避免把 OpenAI Sessions、Microsoft AgentSession 或 GitHub CODEOWNERS 写成同一个东西；它们只支撑 attribution / ownership 这类工程压力。
  - 给一个轻量伪 envelope：

    ```yaml
    session_boundary:
      actor: CURRENT_USER_OR_SERVICE
      owner: REQUIREMENT_OWNER
      task: TASK_ID
      workspace: WORKSPACE_ID
      scope: READ_ONLY_OR_APPROVED_SCOPE
      continuation: NEW_OR_RESUMED
    ```

- Boundary / Non-goal:
  - 不设计完整 identity provider。
  - 不声称所有 SDK session 都包含 owner / policy / recovery。
- Section takeaway: **Identity / Session / Ownership 不是为了好看地打标签，而是让后续每一次授权、证据、恢复和人工决策都有可归属边界。**

### 6. Core B｜Capability Registry + Version + Trust Filter

- Classification: `MINIMUM CORE`
- Reader Question: 为什么 tool / skill 已注册仍然不等于 Agent 可以看见、选择、调用或相信它？
- Core Questions: `26-C03`。
- Claims / Evidence: `26-C03 PARTIAL / 26-E03`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I2`。能力存在、可见、相关、获权、执行、证据接受是六个不同问题。 |
  | Inputs | tool / skill / MCP descriptors、schema、version、source trust、environment、actor、task scope、risk profile、policy version。 |
  | Outputs | allowed capability view、hidden/denied list、selected capability id/version、freshness or trust warning、schema mismatch decision。 |
  | Dependencies | Host registry、MCP/tool servers、Policy engine、version metadata、Runtime dispatch path。 |
  | Trust boundary | MCP tool annotations and descriptors from untrusted servers are hints, not authority；tool schema 不是 permission；model relevance judgment 不是 approval。 |
  | Failure / degradation | unknown source/version -> hide or require review；schema mismatch -> block call；missing capability -> report gap；risky write-capable ability -> separate approval. |
  | Observable evidence | registry snapshot、selected capability id/version、allowed/denied reason、policy filter result。 |
  | Interfaces | Runtime dispatches only allowed view；Business Agent chooses relevance；Tool provides schema/result；Workflow references capability id；Policy filters；KB records source/version。 |

- Required figure `F26-02`:

  ```text
  capability exists
    -> trusted/versioned?
    -> visible to actor/session?
    -> relevant to task?
    -> authorized for action/resource?
    -> dispatched by runtime?
    -> observation accepted as evidence?
  ```

- Draft move:
  - 这一节承接 Article 25 的 registry/discovery split，但往前收缩成 minimum core。
  - 用 MCP spec 的 untrusted annotations 作为 wording guard：能写"支持拆分"，不能写"有 MCP 就有治理"。
- Boundary / Non-goal:
  - 不设计 governed capability evolution；能力缺口如何演进留给 Article 27 / Part VII。
  - 不声称 version semantics 来自 MCP basic tool schema；version governance 是课程 synthesis。
- Section takeaway: **Capability Registry 的最低价值，不是让模型知道更多工具，而是让模型只看到当前可解释、可信且可治理的能力面。**

### 7. Core C｜Context Policy Envelope

- Classification: `MINIMUM CORE for policy and isolation; Runtime owns concrete assembly`
- Reader Question: Runtime 已经会拼 prompt / messages / observations，为什么 Harness 还需要 Context Policy？
- Core Questions: `26-C04`。
- Claims / Evidence: `26-C04 PARTIAL / 26-E04`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I3`。上下文跨 step、compaction、resume、memory/RAG reuse 时不能丢失 source、scope、sensitivity、freshness、retention 和 isolation。 |
  | Inputs | task scope、candidate context items、source refs、sensitivity labels、freshness timestamps、token budget、reuse policy、exclusion rules。 |
  | Outputs | model-visible context plan、excluded items、citation/receipt requirements、compaction/reuse limits、unknown/stale labels。 |
  | Dependencies | Host file/session inputs、Runtime context assembler、KB/RAG retriever、Budget control、Evidence contract。 |
  | Trust boundary | Retrieved or remembered material is not current truth by default；old trace/evidence is not live project state；previous run context is not automatically in scope。 |
  | Failure / degradation | missing provenance -> label unknown or exclude；over budget -> omit lower-priority context with receipt；sensitive/out-of-scope -> redact/block；conflict -> preserve conflict instead of resolving by guess。 |
  | Observable evidence | context receipt、source list、exclusion record、freshness marker、budget trim record。 |
  | Interfaces | Runtime packs current step；Business Agent sets business priority；Tool/RAG supplies observations；Workflow binds step need；Policy enforces exposure；KB supplies provenance/freshness。 |

- Required figure `F26-03`:

  ```text
  source candidates
    -> policy filter: scope / sensitivity / freshness / reuse
    -> budget fit: required / optional / omitted
    -> runtime assembly
    -> model-visible context
    -> receipt and unknowns
  ```

- Draft move:
  - 明确 "Context Assembly" 和 "Context Policy" 分开：Runtime 负责当前 step 怎么装，Harness 负责什么可装、可复用、可引用、可保留。
  - 连接 Articles 12/13，但不重讲 context debugging lab。
- Boundary / Non-goal:
  - 不设计完整 context store。
  - 不把 memory/RAG 检索结果写成 current fact。
- Section takeaway: **Context Policy 不是更会塞材料，而是规定哪些材料有资格进入当前执行、怎样留下来源、何时必须被排除或降级。**

### 8. Core D｜Authority Gate: Permission + Approval + Sandbox + Policy

- Classification: `MINIMUM CORE`
- Reader Question: 为什么 approval 不能只是用户说"可以"，policy 也不能只是 prompt 里一句"谨慎操作"？
- Core Questions: `26-C05`。
- Claims / Evidence: `26-C05 PARTIAL / 26-E05`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I4`。每个外部动作都要在 use-time 检查 actor、capability/action、resource、scope、risk、approval、sandbox 和 policy。 |
  | Inputs | actor、capability/action、resource、frozen request digest、parameters、risk class、approval state、sandbox limits、policy version。 |
  | Outputs | allow / deny / approval-required decision、scoped approval request/response、sandbox execution envelope、denied reason。 |
  | Dependencies | Identity ledger、Capability registry、Host UI、Policy engine、Sandbox client/runtime、Workflow pause/resume。 |
  | Trust boundary | UI click is not authority unless bound to actor/action/resource/scope/expiry/request digest；approval cannot expand policy；sandbox presence does not prove business safety。 |
  | Failure / degradation | missing policy/approval -> deny by default；stale approval -> ask again；sandbox mismatch -> block or downgrade to read-only；policy conflict -> stop and surface conflict。 |
  | Observable evidence | policy decision id、approval record、sandbox manifest/scope、denied reason、trace event。 |
  | Interfaces | Runtime asks before dispatch；Business Agent receives decision and adjusts plan；Tool executes only scoped call；Workflow pauses/resumes；Policy owns decision；KB/RAG cannot grant authority。 |

- Required figure `F26-04`:

  ```text
  requested action
    -> actor/session/scope
    -> capability registry view
    -> policy decision
    -> approval if required
    -> sandbox envelope
    -> runtime dispatch or deny/ask/stop
  ```

- Draft move:
  - 用 "use-time authority" 做关键词：旧许可、宽许可、UI 状态、tool annotation 都不能越过当前动作检查。
  - 连接 Article 19，但只取 minimum gate，不重讲 permission taxonomy。
- Boundary / Non-goal:
  - 不承诺完整 IAM、sandbox escape resistance 或 production security。
  - 不把 BuildPilot 从 read-only 升级到 write authority。
- Section takeaway: **Authority Gate 的最低要求，是每一次真实动作都能被当前身份、范围、策略、审批和沙箱重新检查，而不是复用一句旧的同意。**

### 9. Core E｜Trace + Evidence + Failure Layer

- Classification: `MINIMUM CORE for Trace / Evidence / Failure; Replay is CONDITIONAL CORE`
- Reader Question: 为什么有 trace 仍然不能说结论已经被证明？为什么 replay 不能默认进入最低要求？
- Core Questions: `26-C06`。
- Claims / Evidence: `26-C06 PARTIAL / 26-E06`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I5`，并为 `I6 / I10` 留接口。occurrence、observation、accepted evidence、failure classification、replay eligibility 必须分开。 |
  | Inputs | run/step/tool events、observations、claim ids、evidence rules、failure-layer taxonomy、source refs、trace correlation ids。 |
  | Outputs | trace events/spans、observation refs、evidence status、failure classification、unknown list、replay eligibility flag。 |
  | Dependencies | Runtime events、Tool results、OpenTelemetry-like trace model、Evidence contract、Failure taxonomy、optional replay/eval hooks。 |
  | Trust boundary | Trace/log presence is not proof；tool success is not business acceptance；failure candidate is not root cause；replay requires stronger deterministic/environment/effect evidence。 |
  | Failure / degradation | incomplete trace -> lower evidence status；unaccepted observation -> keep as observation；unclear failure -> classify unknown；missing replay manifest -> no replay claim。 |
  | Observable evidence | trace/span/event ids、evidence card、claim register、failure layer、unknown list、not-run/replay-ineligible marker。 |
  | Interfaces | Runtime emits events；Business Agent cites accepted evidence only；Tool returns observation；Workflow records state transition；Policy may require evidence；KB ingests accepted records only。 |

- Required table `T26-05`:

  | Record type | Answers | Cannot prove alone |
  |---|---|---|
  | Trace | What happened and in what causal/order context? | Claim truth、authority、business acceptance、replay safety |
  | Observation | What did a tool/runtime/source return? | Evidence acceptance or root cause |
  | Evidence | Which claim may rely on which observation/source and under what status? | Full trace, authorization or production validation |
  | Failure layer | Where did it fail or remain unknown? | Exact root cause without supporting evidence |
  | Replay | Can a slice be reconstructed or rerun under constraints? | Original correctness, exactly-once side effects, production safety |

- Draft move:
  - 这一节应成为 Article 18/21/22 的收束桥：它不重讲各篇，但强调 Harness minimum 必须把它们的语义连接起来。
  - Replay 只作为 conditional core：只有有 deterministic inputs、environment、version、side-effect rules 时才谈 full replay。
- Boundary / Non-goal:
  - 不声称 OpenTelemetry 或任何 tracing product 提供 claim acceptance。
  - 不写 Article 22 的 eval runner 或 golden corpus。
- Section takeaway: **Trace 告诉我们发生了什么；Evidence 告诉我们能据此相信什么；Failure layer 告诉我们哪里还不能相信。**

### 10. Core F｜Checkpoint + Recovery Decision Boundary

- Classification: `MINIMUM CORE as boundary contract; durable checkpoint engine is CONDITIONAL CORE`
- Reader Question: 为什么 minimum recovery 不是"失败就重试"，也不是"存个 checkpoint 文件就能安全恢复"？
- Core Questions: `26-C07`。
- Claims / Evidence: `26-C07 PARTIAL / 26-E07`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I6`。恢复必须先分清 known / unknown、committed / in-flight、safe retry / unsafe effect、same intent / changed intent。 |
  | Inputs | committed state、in-flight action、last known evidence、approvals、budget、capability versions、context receipt、continuation reason、failure layer。 |
  | Outputs | resume / retry / reconcile / compensate / ask / stop decision、recovery preconditions、checkpoint pointer、replay eligibility marker。 |
  | Dependencies | Runtime/workflow state、Identity ledger、Authority Gate、Trace/Evidence layer、optional durable workflow engine。 |
  | Trust boundary | Checkpoint file is not safe replay by itself；retry is not recovery correctness；approval/budget/version/context may be stale on resume。 |
  | Failure / degradation | missing in-flight identity -> stop/ask；side-effect uncertain -> reconcile before retry；version drift -> require review；budget exhausted -> stop/degrade/ask。 |
  | Observable evidence | checkpoint record、recovery decision record、in-flight/effect state、stale approval marker、not-replayable marker。 |
  | Interfaces | Runtime performs resume/retry；Business Agent restructures partial report；Tool side effects are reconciled；Workflow stores checkpoint；Policy decides retry authority；KB receives final accepted lesson。 |

- Required figure `F26-05`:

  ```text
  failure / interruption
    -> read last committed state
    -> inspect in-flight action and effect uncertainty
    -> re-check authority, budget, capability version and context freshness
    -> choose resume / retry / reconcile / compensate / ask / stop
    -> record decision before continuing
  ```

- Draft move:
  - Use the phrase "recovery decision boundary" consistently.
  - Full durable execution and Temporal-like replay remain conditional examples, not minimum requirement.
  - Tie back to Article 11 without repeating Lab 04 details.
- Boundary / Non-goal:
  - 不承诺 exactly-once side effects。
  - 不把 checkpoint / replay / recovery 合并成一个万能 "state store"。
- Section takeaway: **Recovery 的最低能力不是继续跑，而是在继续前知道自己从哪里来、什么未知、哪些副作用不能重放、谁有权允许下一步。**

## Part C｜条件核心与延后扩展：进入条件必须写清

### 11. Conditional G｜Budget / Step / Cost / Latency Control

- Classification: `CONDITIONAL CORE`
- Reader Question: 如果 budget 不一定是所有 Harness 的第一天 mandatory，什么时候它变成 core？
- Core Questions: `26-C08`。
- Claims / Evidence: `26-C08 PARTIAL / 26-E08`。
- Capability contract summary:

  | Field | Plan |
  |---|---|
  | Trigger | Long-running、paid、rate-limited、high-risk、latency-visible、multi-tool 或会自动 retry 的 run。 |
  | Inputs | estimate、actual usage、step/time/tool-call cap、latency deadline、risk tier、user budget、retry policy。 |
  | Outputs | budget envelope、reservation、actual ledger、stop/degrade/ask decision、usage delta。 |
  | Dependencies | Runtime usage reporting、provider/tool cost signal、Trace events、Policy thresholds、owner budget preference。 |
  | Trust boundary | Budget grant is not permission, evidence or business acceptance；usage report after the fact is not admission control。 |
  | Failure / degradation | unknown estimate -> conservative cap；exhausted budget -> stop/degrade/ask；retry consumes budget unless policy says otherwise。 |
  | Observable evidence | budget ledger、stop reason、usage deltas、not-run marker、degrade/ask decision。 |
  | Interfaces | Runtime checks before/after steps；Tool usage is counted；Workflow gates long branches；Policy owns threshold；Business Agent reports partial scope；KB/RAG retrieval can be curtailed by freshness/value。 |

- BuildPilot V1 usage:
  - Use simplified step/time/tool-call caps and explicit stop reasons.
  - Do not build or promise full cost platform.
- Boundary / Non-goal:
  - 不给成本优化策略、定价表、ROI 或 adoption threshold；Article 27 处理代价。
- Section takeaway: **Budget 不是每个 one-shot assistant 的首版必需品，但一旦资源消耗会影响信任、恢复或用户体验，它就必须成为共享停止语义。**

### 12. Conditional H｜HITL + Change Request + Intent Confirmation

- Classification: `CONDITIONAL CORE generally; MINIMUM CORE for BuildPilot`
- Reader Question: 为什么 human review 不是一句聊天回复，而是有 scope 的状态迁移？
- Core Questions: `26-C09`、`26-C11`。
- Claims / Evidence: `26-C09 PARTIAL / 26-E09`，`26-C11 PROPOSAL / 26-E11`。
- Capability contract:

  | Field | Plan |
  |---|---|
  | Problem / invariant | 保护 `I8`。clarification、approval、rejection、change request 和 review 必须绑定 owner、scope、evidence、expiry 和 next action。 |
  | Inputs | ambiguity/finding、current evidence、proposed change request、options、owner identity、expiry/review policy、request digest。 |
  | Outputs | clarification request、approval/rejection、change request、owner decision、review trail、re-verification request。 |
  | Dependencies | Host UI、Business Agent report、Authority Gate、Workflow pause/resume、Evidence layer。 |
  | Trust boundary | Human text does not authorize unrelated actions；approval cannot outlive changed request/evidence/diff；BuildPilot owner implementation remains outside BuildPilot。 |
  | Failure / degradation | ambiguous intent -> ask before action；rejection -> stop or revise suggestion；no response -> wait/expire；scope change -> re-confirm。 |
  | Observable evidence | change request record、review decision、owner scope、stale condition、re-verification request。 |
  | Interfaces | Runtime pauses/resumes；Business Agent prepares CR；Tool stays read-only unless separately authorized；Workflow routes review；Policy stores approval；KB ingests accepted decision with provenance。 |

- Draft move:
  - General assistant can defer full HITL if it is informational and low-risk.
  - BuildPilot cannot defer it, because its value proposition is suggestion-first owner-routed production judgment.
- Boundary / Non-goal:
  - 不设计 PR system 或 review UI。
  - 不写真实 owner review occurred。
- Section takeaway: **HITL 只有在变成有归属、有范围、有过期条件、有复验路径的状态迁移时，才是真正的治理能力。**

### 13. Conditional I｜Knowledge Provenance / Freshness / Intake Control

- Classification: `CONDITIONAL CORE`
- Reader Question: Harness 如果用 memory、RAG 或 project knowledge，最低要管什么？
- Core Questions: `26-C10`、`26-C11`。
- Claims / Evidence: `26-C10 PROPOSAL / 26-E10`，`26-C11 PROPOSAL / 26-E11`。
- Capability contract summary:

  | Field | Plan |
  |---|---|
  | Trigger | memory/RAG/project knowledge influences action、claim、capability choice、intent interpretation or future run behavior。 |
  | Inputs | source uri、retrieval time、authority/trust、freshness rule、intent link、acceptance status、scope。 |
  | Outputs | cited knowledge item、stale/unknown marker、intake/update candidate、rejection reason。 |
  | Dependencies | KB/RAG retriever、Context Policy、Evidence Contract、owner review for accepted decisions、Policy access rules。 |
  | Trust boundary | Memory, RAG and previous run summaries are not current truth or authority by default；accepted lesson scope can expire。 |
  | Failure / degradation | stale source -> label or refresh；low trust -> no authority use；conflict -> preserve conflict；missing source -> exclude or caveat。 |
  | Observable evidence | source manifest、freshness stamp、accepted/rejected intake record、conflict/unknown marker。 |
  | Interfaces | KB owns storage/freshness；Runtime retrieves；Business Agent uses with caveat；Tool/RAG supplies content；Workflow may gate intake；Policy limits access；Evidence decides acceptance。 |

- BuildPilot V1 usage:
  - Accepted findings and owner decisions may become knowledge intake candidates.
  - Rejected or uncertain claims remain excluded or marked unknown.
- Boundary / Non-goal:
  - 不设计 multi-project knowledge graph。
  - 不把 memory 里的历史经验写成 current project proof。
- Section takeaway: **Knowledge controls 不是为了让系统记更多，而是防止旧经验、检索结果和未经接受的结论在未来 run 里冒充事实。**

### 14. Deferred J｜Eval / Golden / Regression Hook

- Classification: `ENVIRONMENT-SPECIFIC EXTENSION, often DEFERRED from first Harness slice`
- Reader Question: 为什么最小 Harness 可以先保留 eval hook，而不是首版建设完整 eval platform？
- Core Questions: `26-C10`。
- Claims / Evidence: `26-C10 PROPOSAL / 26-E10`。
- Capability contract summary:

  | Field | Plan |
  |---|---|
  | Trigger | Harness behavior becomes relied upon, promises repeatability, or gates production-impacting suggestions/actions。 |
  | Inputs | scenario id、golden input/output/rubric、trace/evidence reference、environment/version、scorer policy。 |
  | Outputs | regression hook、eval result or not-run marker、drift finding、incomparable/unknown result。 |
  | Dependencies | Eval framework or CI hook、trace/evidence corpus、fixed manifest、scorer/verdict policy、environment/version record。 |
  | Trust boundary | One demo, one green run, or a declared hook is not regression evidence；eval result is scoped to its fixture, metric and manifest。 |
  | Failure / degradation | no golden case -> no regression claim；flaky eval -> downgrade；environment mismatch -> incomparable；not run -> label not run。 |
  | Observable evidence | eval manifest/result、skipped/not-run marker、failing case、incomparable/unknown verdict。 |
  | Interfaces | Runtime may execute；Workflow schedules；Tool provides check outputs；Policy may require gate；Evidence accepts result only under scope；KB stores lessons。 |

- Draft move:
  - Connect to Article 22: one green run is not regression evidence.
  - State clearly that Article 26 has no new eval/lab/runtime evidence.
- Boundary / Non-goal:
  - 不实现 eval runner。
  - 不 claim BuildPilot regression coverage。
- Section takeaway: **Eval hook 可以是设计边界；回归能力只有在 golden、oracle、metric、manifest 和真实结果存在时才是证据。**

## Part D｜把核心能力拼成最小闭环

### 15. Minimum Harness responsibility loop

- Reader Question: 这些能力怎样连成闭环，而不是孤立的治理抽屉？
- Core Questions: `26-C02`-`26-C07`。
- Claims / Evidence: `26-E02`-`26-E07`。
- Required figure `F26-06`:

  ```text
  Identity / Session / Ownership
    -> Context Policy Envelope
    -> Capability Registry / Trust Filter
    -> Authority Gate
    -> Runtime Dispatch under allowed scope
    -> Trace / Observation
    -> Evidence / Failure classification
    -> Checkpoint / Recovery decision
    -> Budget / HITL / Knowledge / Eval hooks when triggered
  ```

- Required table `T26-06`:

  | Minimum question | Primary capability | Supporting capability | Wrong shortcut |
  |---|---|---|---|
  | Who is acting? | A Identity/Session/Ownership | E Trace correlation | "current chat user" as implicit identity |
  | What can be seen? | B Capability Registry, C Context Policy | D Authority Gate | raw tool list / raw files as visible context |
  | What can be done? | D Authority Gate | A/B/C/G | tool schema or approval text as action authority |
  | What happened? | E Trace/Observation | A/F | logs without correlation |
  | What can be claimed? | E Evidence | C/I/J | trace or tool success as accepted claim |
  | Where can we recover? | F Checkpoint/Recovery | A/D/E/G | retry from latest prompt |
  | When should we stop or ask? | F Recovery, G Budget, H HITL | D Policy | infinite loop / blind retry |
  | What can future runs learn? | I Knowledge Intake | E Evidence, H Review | memory capture without source/freshness |

- Planned teaching move:
  - The loop is not a deployment diagram. It is the order of questions a Harness must make answerable.
  - Mark Budget/HITL/Knowledge/Eval as triggered hooks rather than default heavyweight platforms.
- Boundary / Non-goal:
  - 不写 a final API or class diagram。
  - 不 claim vendor product implements exactly this loop。
- Transition purpose: 把抽象能力带入 BuildPilot 案例。
- Section takeaway: **最小闭环不是所有能力同时全量上线，而是每一步都知道自己的身份、权限、证据、失败和恢复边界。**

### 16. Interface matrix：Harness 不吞掉 Runtime、业务 Agent、Tool、Workflow、Policy、KB

- Reader Question: 如果 Harness 承载这么多治理语义，怎样避免变成 God Object？
- Core Questions: `26-C02`-`26-C10`。
- Claims / Evidence: `26-E02`-`26-E10`。
- Required table `T26-07`:

  | Neighbor | Harness asks/provides | Neighbor still owns | Boundary guard |
  |---|---|---|---|
  | Runtime | allowed context/capability/action, policy decision, evidence/failure/recovery rules | model calls、step loop、tool dispatch、wait/resume mechanics | Harness governs; Runtime executes |
  | Business Agent / Workflow | evidence labels、owner scope、change-request state、allowed capability view | domain goal、risk interpretation、finding wording、suggested business action | Harness does not decide domain truth alone |
  | Tool / Tool Runtime | scoped action envelope、schema/version/trust, trace/evidence requirement | validation、execution、raw result、local error/timeout | tool success is observation only |
  | Workflow Engine | policy guard、HITL pause/resume state、checkpoint semantics | deterministic transition model、workflow instance state | workflow step is not global governance |
  | Policy Engine | actor/action/resource/context facts, request digest, risk class | allow/deny/approval-required decision rules | policy decision cannot be inferred from prompt |
  | Knowledge Base / RAG | source/freshness/scope rules, intake acceptance | storage、retrieval、ranking、citation payloads | retrieval is not current proof |
  | Host | workspace/user/environment inputs, approval UI carrier | UI, filesystem/app integration, session surface | Host UI event is not full authority |

- Draft move:
  - Keep this section concise; it prevents "Harness owns everything" misunderstanding before BuildPilot mapping.
  - Reuse Article 25 language lightly, but do not duplicate full responsibility table.
- Boundary / Non-goal:
  - 不改 Article 25 的 layer model。
  - 不 design org chart or service topology。
- Section takeaway: **Harness 的成熟不是拥有更多业务逻辑，而是让邻居在交互时使用同一套治理语义。**

## Part E｜BuildPilot design case：只读建议链怎样映射最小模型

### 17. BuildPilot frozen boundary before the loop

- Reader Question: BuildPilot 在本文中到底是什么，不是什么？
- Core Questions: `26-C11`。
- Claims / Evidence: `26-C11 PROPOSAL / 26-E11`。
- Required boundary block:

  ```text
  BuildPilot in Article 26:
    COURSE PROPOSAL / DESIGN CASE
    NOT IMPLEMENTED / NOT RUN
    READ-ONLY / SUGGESTION-FIRST
    Owner implements real change outside BuildPilot
  ```

- Planned teaching move:
  - Before any BuildPilot flow, repeat the frozen mode once.
  - Say the case exists to test capability allocation, not to demonstrate product behavior.
- Must explicitly not claim:
  - BuildPilot code exists.
  - Unity / Jenkins / CI / Addressables / device test ran.
  - Any PR, patch, build report, trace, approval record or production result exists.
- Transition purpose: With frozen boundary set, map the loop safely.
- Section takeaway: **BuildPilot 在本文中是一面设计镜子，不是一套已经跑起来的系统。**

### 18. BuildPilot minimum closed loop

- Reader Question: 一个 read-only requirement-change assistant 怎样用 minimum Harness 闭环？
- Core Questions: `26-C11` plus `26-C02`-`26-C10` as supporting capability claims。
- Claims / Evidence: `26-C11 PROPOSAL / 26-E11`，support from `26-E02`-`26-E10`。
- Required table `T26-08`:

  | Step | Harness capability used | Primary output | Evidence ceiling | Not allowed in Article 26 |
  |---|---|---|---|---|
  | 1. Requirement intake | A Identity/Session/Ownership, C Context Policy | owner request envelope, workspace/task boundary, read-only mode | design case | Treating user text as modification authority |
  | 2. Intent confirmation | H HITL/Intent Confirmation, C Context Policy | clarification or frozen requirement candidate | proposal | Inferring missing platform/scope/acceptance silently |
  | 3. Capability discovery | B Registry/Trust Filter, D Policy | read-only allowed capability view | partial/proposal | Exposing write tools or untrusted capabilities by default |
  | 4. Restricted checks | D Authority Gate, C Context Policy, G simplified budget | permitted source/config/log/build-report observations | proposal/not run | Running Unity/Jenkins/CI or modifying files |
  | 5. Finding | E Trace/Evidence/Failure | findings with `OBSERVED / INFERRED / UNKNOWN / NOT_PROVEN` labels | proposal | Turning observation into confirmed production conclusion |
  | 6. Change Request | H HITL/Change Request, E Evidence | evidence-backed suggestion, impact, risk, owner action, re-verification plan | proposal | Applying patch, opening PR, assigning owner as fact |
  | 7. Human Review | H HITL, D Authority Gate | accept/reject/revise decision scope | proposal | Treating review as global future approval |
  | 8. Re-verification | F Recovery, E Evidence, G caps | allowed read-only re-check result or unknown | proposal/not run | Claiming full production verification |
  | 9. Evidence and knowledge intake | I Knowledge Control, E Evidence | accepted decision/lesson intake candidate | proposal | Ingesting rejected/uncertain claims as truth |

- Required figure `F26-07`:

  ```text
  Intake
    -> Intent Confirmation
    -> Allowed Capability View
    -> Restricted Read-only Checks
    -> Finding with Evidence Status
    -> Change Request
    -> Human Review
    -> Owner Implements Outside BuildPilot
    -> Read-only Re-verification
    -> Evidence / Knowledge Intake Candidate
  ```

- Draft move:
  - Keep language concrete enough for a reader to imagine a Unity requirement-change review, but never state a real project was scanned.
  - Use "candidate", "proposal", "not run", "read-only" repeatedly at decision points.
  - For source/config/build-report examples, present them as possible evidence categories only, not observed artifacts.
- Boundary / Non-goal:
  - No exact schema for Requirement Contract or Change Request beyond outline-level fields.
  - No BuildPilot V1 architecture implementation.
- Section takeaway: **BuildPilot 的最小闭环不是替人改代码，而是把需求、证据、建议、人工决策和复验边界串成可审计状态。**

### 19. What BuildPilot can defer without breaking Article 26

- Reader Question: 哪些高级能力可以延后，同时不破坏本文的最小闭环？
- Core Questions: `26-C08`、`26-C10`、`26-C11`。
- Claims / Evidence: `26-E08`、`26-E10`、`26-E11`。
- Required table `T26-09`:

  | Advanced ability | Article 26 treatment | Why it can defer | Guard that must remain |
  |---|---|---|---|
  | Autonomous code modification | Out of scope | BuildPilot is read-only/suggestion-first | Change Request and owner implementation boundary |
  | PR creation / merge automation | Out of scope | Owner/external process implements | Review and re-verification request |
  | Production deployment | Out of scope | No deployment authority/evidence | `NOT IMPLEMENTED / NOT RUN` label |
  | Full cost optimization platform | Deferred | simplified caps enough for design case | explicit stop/degrade/ask semantics |
  | Full replay engine | Conditional/deferred | read-only analysis can preserve recovery decision without deterministic replay | checkpoint/recovery eligibility marker |
  | Full eval platform | Deferred extension | Article 26 has no lab/eval run | not-run marker and future hook only |
  | Governed capability evolution | Deferred | capability gap can be reported before registry expansion | no silent tool install or permission expansion |
  | Multi-project knowledge graph | Deferred | accepted local finding intake can start simpler | provenance/freshness/source scope |

- Boundary / Non-goal:
  - This is not Article 27 adoption staging. Do not add stages, cost thresholds or "when to scale" framework.
  - Do not imply deferred means unnecessary forever.
- Section takeaway: **能延后的不是治理语义，而是重型平台实现；底线是 unknown、not-run、owner boundary 和 no silent authority expansion 必须保留。**

## Part F｜坏实现与工程判断

### 20. A minimum Harness usually writes badly in these ways

- Reader Question: 哪些写法看起来完整，实际破坏 minimum model？
- Core Questions: all `26-C01`-`26-C11` as review synthesis。
- Claims / Evidence: `26-E01`-`26-E11`。
- Required table `T26-10`:

  | Bad design | Broken invariant | Minimal correction |
  |---|---|---|
  | "Longer prompt = Harness" | `I4 / I5 / I6` | Prompt states behavior; Harness stores/enforces authority, evidence and recovery facts |
  | "SDK Session = full Session boundary" | `I1` | Add owner/task/workspace/scope/continuation semantics |
  | "Tool schema visible = authorized" | `I2 / I4` | Filter by trust/version/scope and use-time policy |
  | "Context packing = context policy" | `I3` | Separate assembly from exposure/provenance/freshness/reuse policy |
  | "Trace = evidence" | `I5` | Link trace to claim acceptance status |
  | "Checkpoint = safe replay" | `I6` | Add effect, version, approval, budget and deterministic eligibility checks |
  | "Budget report after run = budget control" | `I7` | Add admission, cap, stop/degrade/ask semantics |
  | "Human said OK = all future authority" | `I8 / I4` | Bind approval to request digest, scope, expiry and stale conditions |
  | "Memory retrieved = current fact" | `I9` | Add provenance, freshness and acceptance policy |
  | "One green eval = no regression" | `I10` | Require golden/oracle/manifest/verdict policy and real run before regression claim |
  | "BuildPilot suggestion = fix completed" | `I5 / I8 / I10` | Owner implements externally; re-verification remains not-run until actually run |

- Draft move:
  - This section is a review checklist, not a new model.
  - Keep it after BuildPilot so readers can see how mistakes would corrupt the design case.
- Boundary / Non-goal:
  - Do not add Article 27's bloat/adoption analysis.
- Section takeaway: **大多数 Harness 失败不是少一个模块，而是让一种记录冒充了另一种权威或证据。**

### 21. What this article establishes and what it does not prove

- Reader Question: 这篇文章最后能安全留下哪些结论？
- Core Questions: `26-C01`-`26-C11`。
- Claims / Evidence: `26-E01`-`26-E11`。
- Planned "establishes" list:
  - Article 26 can safely define an invariant-first course model for Harness minimum.
  - Identity/session/ownership, capability governance, context policy, authority gate, trace/evidence/failure layer and recovery decision boundary are minimum core in this model.
  - Budget, HITL, knowledge and eval/regression have explicit triggers; BuildPilot makes HITL/intent confirmation core for its read-only suggestion-first loop.
  - BuildPilot can be mapped as a design-only closed loop from intake to evidence/knowledge intake candidate.
  - All claim strength remains `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
- Planned "does not prove" list:
  - Not an industry-standard Harness checklist.
  - Not a vendor taxonomy or product API guide.
  - Not proof that BuildPilot exists, runs, scans Unity, calls Jenkins, creates PRs, modifies code or reduces defects.
  - Not proof of full replay, eval/regression coverage, production safety, cost saving or adoption readiness.
  - Not Article 27 trade-off/adoption framework and not Part VI DeepSeek Harness source/runtime evidence.
- Closing bridge:
  - Article 26 leaves readers with a minimum model.
  - Article 27 will ask when the model is worth implementing, when it becomes too heavy, and how to avoid Bloat.
- Mandatory final boundary sentence:
  - `最小 Harness 的价值不是把所有治理能力一次性做大，而是让每一次能力暴露、授权、证据接受、恢复、人工决策和知识吸收都有同一套可审查边界。`

## Claim Traceability（11 / 11）

| Claim | Status | Evidence | Primary outline sections | Wording boundary |
|---|---|---|---|---|
| `26-C01` | `PROPOSAL` | `26-E01` | Opening, 1, 2, 3, 21 | invariant-first is course method, not external standard |
| `26-C02` | `PARTIAL` | `26-E02` | 2, 3, 5, 15, 18 | attribution ledger is course synthesis; not any single SDK Session |
| `26-C03` | `PARTIAL` | `26-E03` | 3, 6, 15, 18, 20 | MCP supports discovery/schema/untrusted annotation split; version governance is course synthesis |
| `26-C04` | `PARTIAL` | `26-E04` | 3, 7, 15, 18 | context policy is minimum; concrete assembly may remain Runtime-owned |
| `26-C05` | `PARTIAL` | `26-E05` | 3, 8, 15, 18, 20 | deny-first authority gate, not full IAM or security proof |
| `26-C06` | `PARTIAL` | `26-E06` | 9, 15, 18, 20 | trace/evidence/failure minimum; full replay conditional |
| `26-C07` | `PARTIAL` | `26-E07` | 10, 15, 18, 20 | recovery boundary minimum; durable workflow/replay engine conditional |
| `26-C08` | `PARTIAL` | `26-E08` | 3, 11, 15, 18, 19 | budget conditional for long/paid/rate-limited/latency-sensitive runs; no universal mandatory claim |
| `26-C09` | `PARTIAL` | `26-E09` | 3, 12, 18, 20 | conditional generally, BuildPilot-core because suggestion-first requires owner decision |
| `26-C10` | `PROPOSAL` | `26-E10` | 3, 13, 14, 19, 20 | knowledge/eval classification is design proposal; no runtime/eval evidence |
| `26-C11` | `PROPOSAL` | `26-E11` | 17, 18, 19, 21 | BuildPilot loop is design-only, `NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST` |

Coverage=`11 / 11`；Evidence Cards=`26-E01`-`26-E11`；Status mix=`0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Core Question coverage（8 / 8）

| Core Question | Primary sections | Claims / Evidence | Required boundary |
|---|---|---|---|
| Q1 "最小但闭环"要维持哪些跨步骤、跨工具、跨会话不变量 | 1, 2, 15 | `26-C01`, all supporting | Invariants first; no feature checklist |
| Q2 Identity、Session与Ownership怎样建立可归属执行边界 | 5, 15, 18 | `26-C02/E02` | not equal to any single SDK session object |
| Q3 Context Assembly、隔离、Capability Registry和版本怎样避免错误能力进入执行 | 6, 7, 15, 18 | `26-C03/E03`, `26-C04/E04` | assembly vs policy; visibility vs authority |
| Q4 Permission、Approval、Sandbox与Policy Enforcement怎样形成拒绝优先信任边界 | 8, 15, 18, 20 | `26-C05/E05` | use-time deny-first contract, not full IAM |
| Q5 Execution Control、State、Checkpoint与Recovery怎样避免盲目重试 | 10, 15, 18, 20 | `26-C07/E07` | recovery decision boundary; full durable engine conditional |
| Q6 Trace、Evidence、Replay与Failure Taxonomy怎样形成可审计记录链 | 9, 15, 18, 20 | `26-C06/E06` | trace is not evidence; replay conditional |
| Q7 Budget、HITL、Evaluation与Knowledge controls哪些最小核心、哪些可延后 | 3, 11, 12, 13, 14, 19 | `26-C08/E08`, `26-C09/E09`, `26-C10/E10` | conditional/deferred triggers explicit |
| Q8 BuildPilot最小闭环怎样从需求读取走到Finding、CR、Review、re-verification与知识沉淀 | 17, 18, 19 | `26-C11/E11` | design-only, read-only, suggestion-first |

## Figures, tables and examples plan

| ID | Form | Teaching responsibility | Evidence source | Mandatory label / restraint |
|---|---|---|---|---|
| `T26-01` | feature-menu shortcut table | 解释为什么不能从 vendor feature menu 起笔 | `26-E01/E08/E10` | COURSE MODEL |
| `T26-02` | invariant table | 用 I1-I10 建立 capability pressure | `26-E01-E10` | not external standard |
| `T26-03` | candidate classification table | 覆盖十个候选区，区分 minimum / conditional / deferred | `26-E01-E10` | do not make all mandatory |
| `F26-01` | capability contract template | 统一每项核心能力的审查字段 | `26-E02-E07` | contract, not implementation schema |
| `T26-04` | contract-field table | 说明 problem/input/output/trust/failure/evidence/interface 各自防什么 | `26-E02-E07` | no class/API design |
| `F26-02` | capability lifecycle chain | 拆 existence -> visibility -> relevance -> authority -> execution -> evidence | `26-E03` | annotations not authority |
| `F26-03` | context policy flow | 拆 source candidates -> policy -> budget fit -> assembly -> receipt | `26-E04` | no full context store |
| `F26-04` | authority gate flow | 展示 actor/action/resource/scope/policy/approval/sandbox/use-time check | `26-E05` | no full IAM/security proof |
| `T26-05` | trace/evidence/failure/replay ledger | 分离记录、观察、证据、失败层和 replay | `26-E06` | trace is not evidence |
| `F26-05` | recovery decision flow | 展示 known/unknown、in-flight、effect、authority、budget、version 复核 | `26-E07` | no safe replay claim |
| `F26-06` | minimum responsibility loop | 串起 A-F 与 triggered hooks | `26-E02-E10` | not deployment diagram |
| `T26-07` | interface matrix | 防止 Harness 吞 Runtime / Business / Tool / Workflow / Policy / KB | `26-E02-E10` | non-God-Object boundary |
| `T26-08` | BuildPilot loop table | 将九步 read-only suggestion-first 链路映射到 capability model | `26-E11` | NOT IMPLEMENTED / NOT RUN |
| `F26-07` | BuildPilot sequence | 从 intake 到 knowledge intake candidate 的闭环 | `26-E11` | Owner implements externally |
| `T26-09` | deferred advanced abilities table | 说明哪些高级能力可以延后但保留 guard | `26-E08/E10/E11` | not Article 27 staging |
| `T26-10` | anti-pattern table | 汇总 minimum model 的常见坏写法 | `26-E01-E11` | review heuristic |

Asset policy: Outline/Draft 优先使用 Markdown 表和 ASCII 图；本 Gate 不创建 `assets/`，不生成截图，不伪造 UI、Trace、BuildPilot、Unity、Jenkins、runtime 或 production artifact。若后续 Publisher 需要图像资产，必须从 Draft/Review/Publisher Gate 另行确认并保留 `COURSE PROPOSAL / PARTIAL / NOT RUN` 标签。

## Learning Check（题目 + answer expectations）

1. 为什么 Harness minimum 不能从厂商功能菜单开始？
   - Expected: 产品打包方式和课程责任边界不同；minimum 必须由跨 run/tool/workflow 仍需成立的不变量推导。
2. `MINIMUM CORE`、`CONDITIONAL CORE`、`DEFERRED` 的区别是什么？
   - Expected: minimum core 是没有它无法闭环；conditional core 是触发风险出现后必须有；deferred 是可先保留 hook/边界但不承诺重型实现。
3. Identity / Session / Ownership 为什么是 minimum core？
   - Expected: authority、trace、evidence、budget、recovery、review 都要归属到 actor、owner、task、workspace 和 continuation boundary。
4. Tool schema 被模型看见，为什么还不能直接调用？
   - Expected: existence、visibility、relevance、authority、execution、evidence acceptance 分离；untrusted annotations 不能授权。
5. Context Assembly 和 Context Policy 怎样区分？
   - Expected: Runtime 可负责 packing；Harness 负责暴露、隔离、来源、freshness、保留、复用、裁剪和 receipt 规则。
6. Use-time authority gate 最少要检查什么？
   - Expected: actor、action/capability、resource、scope、request digest、risk、policy version、approval state、sandbox limit。
7. Trace 和 Evidence 有什么区别？
   - Expected: Trace 记录发生和关联；Evidence 判断 observation/source 能否支持 claim 以及状态。
8. 为什么 checkpoint 不等于 safe replay？
   - Expected: replay 还要确定性输入、环境、版本、side-effect boundary、approval/budget/context freshness 和 manifest。
9. Budget 为什么不是所有 assistant 的首版 mandatory？
   - Expected: low-risk one-shot 可以简化；长任务、付费、限流、自动 retry 或延迟可见时变成 conditional core。
10. BuildPilot 为什么必须把 HITL / Change Request 当 core？
    - Expected: 它是 read-only/suggestion-first 生产建议链；owner review、scope、expiry、re-verification 是价值边界。
11. Knowledge provenance 什么时候变成 core？
    - Expected: 当 memory/RAG/project knowledge 影响 action、claim、intent 或 future run 行为时。
12. Eval / Regression Hook 在 Article 26 中能说到什么程度？
    - Expected: 可作为设计 hook；没有真实 golden/oracle/manifest/verdict run 时不能 claim regression coverage。
13. BuildPilot minimum loop 的九步是什么？
    - Expected: intake -> intent confirmation -> discovery -> restricted checks -> finding -> change request -> human review -> re-verification -> evidence/knowledge intake candidate。
14. 哪些 BuildPilot 高级能力明确延后？
    - Expected: autonomous code modification、PR/merge、deployment、full cost platform、full replay、full eval platform、governed capability evolution、multi-project knowledge graph。
15. Article 27 将接住什么问题？
    - Expected: 何时值得实现、收益/代价、Bloat、可替换性、演化和不适用条件；Article 26 不提前展开。

## Practical reader actions

| Action | Minimum artifact | Review question | Evidence ceiling |
|---|---|---|---|
| Derive from invariants | I1-I10 invariant list | 每项能力到底保护哪条不变量？ | COURSE PROPOSAL |
| Classify capability candidates | minimum / conditional / deferred table | 进入首版的理由是底线，还是只是完整性焦虑？ | mixed PARTIAL / PROPOSAL |
| Fill capability contract | problem/input/output/dependency/trust/failure/evidence/interface sheet | 是否有空格被自然语言猜过去？ | contract, not schema |
| Separate capability visibility and authority | existence -> visibility -> relevance -> authority -> execution -> evidence chain | raw tool list 是否被当成可调用能力？ | MCP-supported PARTIAL |
| Audit context policy | source/freshness/scope/sensitivity/reuse/receipt list | Runtime packing 前是否已有 policy filter？ | Article 12/13 continuity |
| Add deny-first authority path | action authority record | 缺 actor/scope/policy/approval 时是否 stop/ask/deny？ | Article 19 continuity |
| Split trace and evidence | trace/evidence/failure ledger | 发生记录是否被误写成 accepted claim？ | Article 18/21/22 continuity |
| Define recovery decision | resume/retry/reconcile/compensate/ask/stop matrix | effect unknown 时是否 blind retry？ | Article 11 continuity |
| Trigger conditional hooks | budget/HITL/knowledge/eval trigger list | 当前风险是否已经让条件能力变成 core？ | no universal mandatory claim |
| Map BuildPilot safely | read-only suggestion-first loop | 哪一步产生 evidence，哪一步只是 proposal？ | DESIGN CASE / NOT RUN |

## Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Architecture minimalism | invariant-first classification | 能拒绝 feature checklist，把能力按不变量和触发条件分级 | course model, not standard |
| Harness governance design | A-F capability contracts | 能为 core capability 写 problem/input/output/trust/failure/evidence/interface | no implementation schema |
| Capability governance | registry/trust/version chain | 能区分 tool existence、visibility、authority、execution、evidence | no governed evolution design |
| Context engineering discipline | Context Policy Envelope | 能让 context 来源、freshness、scope、retention、compaction 可审计 | no full context store |
| Permission and approval rigor | deny-first authority gate | 能把 approval 绑定 actor/action/resource/scope/expiry/request digest | no production security proof |
| Evidence literacy | trace/evidence/failure split | 能阻止 trace/tool success 冒充 accepted claim | no full replay/eval claim |
| Recovery reasoning | checkpoint/recovery decision boundary | 能区分 resume/retry/reconcile/compensate/ask/stop | no exactly-once guarantee |
| Conditional-core judgment | budget/HITL/knowledge/eval trigger matrix | 能判断什么时候能力从 optional decoration 变成 required surface | no Article 27 adoption framework |
| BuildPilot design-case thinking | read-only closed loop table | 能用 Harness model 组织 suggestion-first analysis without implementation claims | BuildPilot not implemented/run |
| Evidence communication | claim traceability + posture labels | 能保持 `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED` 可见 | no status upgrade |

## Frontmatter and publication plan

```yaml
---
title: "Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery"
slug: "agent-engineering-26-harness-minimum-capability-model"
date: "2026-08-30T00:00:00+08:00"
description: "从跨 Run、跨 Tool、跨 Workflow 的不变量出发，推导 Harness 的最小能力模型，并用 BuildPilot 的只读建议链说明 Capability、Policy、Session、Trace 与 Recovery 怎样形成闭环。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 270
weight: 3270
---
```

- Published Path Candidate: `content/ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md`
- Previous link: Published Article 25 exact path `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`
- Course index link: existing Agent Engineering series index
- Next link: Article 27 planned title only if Publisher later confirms path exists；Draft 阶段可用 prose bridge，不创建 broken `relref`。
- Metadata rationale: `series_order=(26+1)*10=270`，`weight=3000+270=3270`，follows Article 25 `series_order=260 / weight=3260`。
- YAML quote rule: current title/description contain no ASCII quote characters；double-quoted scalars are valid. Any later edit adding ASCII quotation marks must switch outer YAML quoting safely per repository rule。
- OUTLINE Gate note: This is a plan only; no actual Published Content, frontmatter body or Hugo page is created in this Gate.

## Exact no-new-fact boundary for Draft

Draft may:

- paraphrase and reorganize only `26-C01`-`26-C11` and `26-E01`-`26-E11`;
- state the invariant-first minimum model as a course method, not an external standard;
- use Published Article 24 only as bridge: shared governance pressure appears when local prompt/tool/workflow semantics drift;
- use Published Article 25 only as bridge: Runtime advances execution, Harness carries shared governance semantics, Host/Business/Tool/Workflow/Policy/KB retain separate ownership;
- use prior local Articles 06、07、10、11、12、13、18、19、20、21、22 only for continuity already preserved in Evidence Cards;
- preserve the ten candidate area classification without making all candidates mandatory;
- define A-F as the main admitted minimum-core capability contracts, with H as BuildPilot-core and G/I/J as triggered conditional/deferred hooks;
- use BuildPilot requirement-change loop only as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`;
- state Owner implements real changes outside BuildPilot and any re-verification is design-only/not-run until future evidence exists;
- preserve Required Lab=`NONE`, Experiment Count=`0`, Runtime Observation=`ABSENT`;
- preserve `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`, Claim Coverage `11 / 11`, Evidence Cards `11 / 11`.

Draft must not:

- introduce a core Claim/Evidence Card beyond `26-C01`-`26-C11`;
- claim the Article 26 model is an industry standard, vendor standard, DeepSeek Harness source fact or universal architecture;
- upgrade any `PARTIAL` or `PROPOSAL` claim to `CONFIRMED`;
- copy vendor product feature lists as the organizing structure;
- define Article 27 adoption stages, cost thresholds, bloat framework, migration strategy or when-not-to-build decision tree;
- describe BuildPilot as implemented, run, integrated, deployed, benchmarked or capable of direct automated fixes;
- claim any Unity Editor, Jenkins, CI, Addressables Analyze, package build, runtime profile, device test, project scan, PR or production deployment was executed;
- fabricate metrics, screenshots, logs, traces, approval records, BuildReports, registry output, owner reviews, runtime observations or production results;
- turn suggestion-first, Human Review, sandbox, approval, eval or Harness into a guarantee of safety, correctness, compliance, no regression or future prevention;
- silently install tools, expand permissions, mutate capability registry or propose production writes as completed behavior;
- create Review/content/assets/Lab/runtime/global/canonical/Git/future-Article artifacts during AUTHOR_DRAFT.

Trigger: if Draft requires any fact outside this boundary, return `RETURN_TO_RESEARCH` with the exact missing Claim/Evidence need; do not fill it with memory, inference or common practice.

## Explicit non-scope

- 不写成 OpenAI Agents SDK、Microsoft Agent Framework、MCP、Temporal、OpenTelemetry、GitHub、Unity 或任何厂商产品教程。
- 不实现或设计完整 Harness API、Capability Registry product、Policy Engine、Session Store、Trace Store、Evidence Store、Replay Engine、Eval Runner、Knowledge Store、Tool Registry、Workflow Engine 或 BuildPilot Runtime。
- 不运行 Lab、外部网络、Provider/model call、Unity Editor、Jenkins、CI、package build、device test、Addressables Analyze、真实项目扫描或任何 runtime experiment。
- 不创建或修改 Article 27—28 workspace/content/assets，不启动 Article 27，不触碰 Article 28。
- 不修改 `research.md`、`evidence.md`、README、article-card、review、subagent-trace、series plan、status tracker、course-run-state、published content 或任何全局/canonical 文件。
- 不创建 branch/worktree/commit/push。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。

## Closing bridge

- Closing sentence: `最小 Harness 的价值不是把所有治理能力一次性做大，而是让每一次能力暴露、授权、证据接受、恢复、人工决策和知识吸收都有同一套可审查边界。`
- Bridge to Article 27:
  - Article 26 建立 minimum capability model。
  - Article 27 才回答这套模型何时值得实现、何时过重、怎样避免 Bloat、怎样处理可替换性和演化。
  - Part VI 才用 DeepSeek Harness pinned source / runtime evidence 验证真实实现。
- Mandatory final boundary sentence: **只有先从不变量推导最小能力，后续讨论收益、代价和演化时才不会把 Harness 写成万能平台或功能清单。**

## OUTLINE Gate checklist

- [x] Article Type fixed as `PRINCIPLE`；L-weight structure follows problem space -> invariants -> abstract minimum model -> capability contracts -> BuildPilot design case -> engineering boundaries。
- [x] Teaching Spine begins from Article 24 Why and Article 25 Runtime/Harness boundary, then answers Article 26 Minimum Model。
- [x] `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED` preserved exactly。
- [x] Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。
- [x] BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。
- [x] All ten candidate areas covered and classified without making every feature mandatory。
- [x] All admitted core capability contracts include problem/invariant, inputs, outputs, dependencies, trust boundary, failure/degradation, observable evidence and interfaces。
- [x] A-F minimum core capability sheets complete；G/H/I/J conditional/deferred triggers and BuildPilot treatment explicit。
- [x] Runtime、Business Agent、Tool、Workflow、Policy、Knowledge Base and Host interfaces are separated to prevent Harness-as-God-Object。
- [x] BuildPilot loop covers intake、intent confirmation、capability discovery、restricted checks、Finding、Change Request、Human Review、re-verification、Evidence/Knowledge intake。
- [x] Article 27 trade-off/adoption, Part VI DeepSeek Harness source/runtime evidence and Part VII BuildPilot implementation remain non-scope。
- [x] Claim coverage=`11 / 11`；Evidence Cards=`26-E01`-`26-E11` only；new core Claim/Card=`NONE`。
- [x] Figures/Tables、Learning Checks、Practical Actions、Job Competency、Frontmatter plan and Draft no-new-fact boundary are complete。
- [x] No Draft/Review/content/assets/Lab/runtime/global/canonical/Git/future-Article artifact belongs to this Author result。
- [x] OUTLINE Gate recommendation: `PASS`；next allowed gate candidate: `AUTHOR_DRAFT`；Master validation remains outside this artifact。
