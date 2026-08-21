# Article 10 Detailed Outline｜State Machine 与 Workflow

> Outline Gate candidate：`PASS_RECOMMENDED`。本文件是 Author 的 Detailed Outline 候选，不是正文、Formal Review、Final Gate 或发布批准。

## 0. Article decision

### Article type

- 类型：`原理 / 机制桥接篇（Major Core Lesson / NORMAL_ARTICLE）`。
- 选择理由：本篇承接 Article 08 的 Agent Loop 与 Article 09 的 Planning，把“模型可以提出下一步”收束到“程序只允许合法 transition 被提交”。重点是控制责任、状态边界与工程判断，不适合写成 AWS Step Functions / LangGraph / Microsoft / OpenAI 的 API 对照，也不做 BPM 或 SCXML 教程。
- 结构：`问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界`。

### 最短 thesis

`State Machine / Workflow 的价值不是替 Agent 思考，而是把合法状态、边、Guard、Invariant 与 Terminal 交给确定性程序；Agent 只在多个合法候选仍需上下文判断时，输出受 Schema 约束的 suggestion，最终能否推进仍由 runtime 验证并提交。`

### Reader Change

读前，读者容易把 Plan、Workflow Definition、Runtime State 与 Trace 混成一份“流程记录”，把模型建议当成合法 transition，或把 Workflow / Agent 当成二选一的产品类型。读后，读者应能：

1. 区分 Plan、Workflow Definition、Runtime State 与 Trace 各自的 producer、consumer 与证明力；
2. 只在课程 taxonomy 范围内比较 Agent Loop、State Machine 与 Workflow，保留 `10-C02 PARTIAL`；
3. 定义 State、Transition、Guard、Terminal，并把 Stage、Step、Invariant 标为本文工作定义 / Proposal；
4. 画出 `Agent suggestion -> deterministic validation -> State commit`，明确 model suggestion 不等于 legal transition；
5. 比较 Workflow 调 Agent、Agent 调受控 Workflow Tool 与 code orchestration 三种控制形态，但不把它们写成互斥标准；
6. 从 AL-04 中区分 raw observed repeat / no-progress / fake-success rejection 与 State Machine overlay `PROPOSAL / NOT EXECUTED`；
7. 说明当前 State 与 Checkpoint / Retry / Cancellation / Resume / Recovery 的 stop line。

### Teaching Spine

1. **Problem space**：自由 Loop 和候选 Plan 仍会重复、漏阶段或提前自证完成；流程需要一个不由模型随意改写的确定性骨架。
2. **Abstract model**：Plan、Workflow Definition、Runtime State 与 Trace 是四类对象；Agent Loop、State Machine 与 Workflow 只在“根据当前信息推进下一步”上有共同骨架，decision owner 与 scope 不同。
3. **Concrete mechanism**：State Machine 用 State / Transition / Guard / Terminal 收窄合法推进；Workflow 负责应用级阶段、任务组合与 Agent Decision Point 的放置；Agent 只提交 schema-bounded candidate。
4. **Engineering judgment**：改变 authoritative State 或进入 terminal 的 legal transition 必须由程序验证；Guard / invariant / authorization / evidence 不能交给 prompt 自证。
5. **Verification boundary**：AL-04 只证明 fixed fixture 中 repeat、no progress、missing requirements 与 fake-success rejection；`INTAKE -> LOG_READY -> SOURCE_READY -> VERIFIED -> SUCCEEDED` 是分析 overlay，未被 runtime 执行。

---

## 1. 为什么自由 Loop 和 Plan 还不够

- **Reader Question**：Article 08 已经能安全推进一个 Step，Article 09 已经有 Plan candidate，为什么还需要 State Machine / Workflow？
- **Section Goal**：先立问题空间：自由 Loop 与候选 Plan 能表达下一步，但不能自己保证阶段顺序、合法边、Guard、Invariant 或 terminal contract。
- **Core Claim**：Plan 只表达剩余候选，Agent Loop 只定义当前 State 下怎样推进一步；当任务有固定阶段、必经证据与不可违反约束时，需要由确定性骨架持有合法推进关系。
- **Claim IDs / Evidence IDs**：`10-C01`、`10-C02`、`10-C05`、`10-C08`；`10-E01`、`10-E02`、`10-E05`、`10-E08`；dependency Article 08 / 09。
- **Wording strength**：`C01 CONFIRMED` 可写成对象边界事实；`C02 PARTIAL` 只能写成课程比较轴；`C05 PROPOSAL` 用“本文采用 / 更稳的设计是”；`C08 CONFIRMED` 只限 fixed fixture raw facts。
- **Teaching Duty**：从构建失败调查类任务切入：日志、源码、验证结果必须按顺序进入状态，不能因为模型“计划了”或“请求成功”就跳过。
- **Example / Figure Duty**：
  - F10-01：`自由 Loop / Plan candidate` 与 `确定性骨架` 的缺口图。
  - 小例子：Goal 要求 log 与 matching source Evidence；两次读取无关文件会让 history 增长，却不能推进到 `SOURCE_READY`。
- **Guardrail / Counter-evidence**：
  - AWS 会把 state machine 直接称为 workflow；正文不能写成“Workflow 一定在 State Machine 之上”。
  - LangGraph / Microsoft / OpenAI 都展示混合控制；不能把“需要确定性骨架”写成“模型不能参与任何 route 选择”。
- **Boundary / Stop Line**：不展开 Article 11 的 Retry / Checkpoint / Resume；这里只讲“为什么本次 transition 可不可以提交”。
- **Bridge**：读者接受“需要骨架”之后，下一节先把四类经常混在一起的对象切开。

---

## 2. 第一层对象边界：Plan、Definition、Runtime State 与 Trace

- **Reader Question**：一个流程系统里，计划、定义、当前状态和历史记录到底分别能证明什么？
- **Section Goal**：建立全文第一张对象合同表，阻断“Plan 写了 / Trace 有记录 / State 有字段 / Definition 存在”互相冒充。
- **Core Claim**：Plan、Workflow Definition、Runtime State 与 Trace 拥有不同 producer、consumer 与证明力，不能互相替代；产品可以共置这些对象，但共置不改变语义边界。
- **Claim IDs / Evidence IDs**：`10-C01`；`10-E01`。
- **Wording strength**：`CONFIRMED / PRODUCT + REPOSITORY-SCOPED`。可以写成“在引用产品与课程 fixture 中可分别识别”；不能写成所有实现必须拆成四份文件。
- **Required table duty**：

  | Object | 最小工作定义 | 能证明 | 不能证明 |
  |---|---|---|---|
  | Plan | Goal 与 Current Evidence 下的剩余行动候选 | 准备考虑什么 | 已执行、已授权、合法 transition、当前 State |
  | Workflow Definition | 预定义 stage / state、edge、condition、terminal 与 task composition | 被配置的候选合法路径 | 某次 execution 已走到哪里或成功 |
  | Runtime State | 当前已提交的控制位置与权威数据 | 当前接受了哪些事实、哪些 state active | 完整历史、持久化成功、可恢复性 |
  | Trace | step、transition、tool、state revision 与 terminal event 的结构化记录 | 记录中可追到的已发生事件 | Definition 本身、authoritative current state、recovery guarantee |

- **Example / Figure Duty**：
  - F10-02：四对象分层图，箭头标 producer / consumer。
  - 例子：ASL definition 存在不等于 execution 成功；Article 08 Trace 有 AL-04 两次读文件，不等于 goal-state 进展。
- **Guardrail / Counter-evidence**：产品 UI 或 SDK object 可能同时暴露 definition、state、history；正文要写“同容器不同 authority”。
- **Boundary / Stop Line**：不把 Trace / Replay 的完整系统设计提前写入 Article 21；Trace 在本篇只作为对象边界。
- **Bridge**：对象分清后，再比较三种推进结构：Agent Loop、State Machine 与 Workflow。

---

## 3. Agent Loop、State Machine 与 Workflow：只作为课程比较轴

- **Reader Question**：Agent Loop、State Machine 与 Workflow 是三种互斥架构吗，还是同一类东西的不同名字？
- **Section Goal**：用 `10-C02 PARTIAL` 的正确强度建立比较轴：共同点是“从当前信息推进下一步并走向 terminal”，差异在 decision owner、合法候选集合位置与运行对象 scope。
- **Core Claim**：三者共享最小推进骨架，但产品术语高度重叠；本文只按课程责任面比较，不宣称行业统一 taxonomy。
- **Claim IDs / Evidence IDs**：`10-C02`、`10-C10`；`10-E02`、`10-E10`。
- **Wording strength**：`10-C02 PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED`；必须显式保留“本课程比较轴”。`10-C10 CONFIRMED` 只用于反驳唯一架构。
- **Required comparison table duty**：

  | Object | 本篇最小边界 | 下一步主要由谁决定 | 不自动等于 |
  |---|---|---|---|
  | Agent Loop | Article 08 的有界反馈循环 | model / decision source 给 candidate，Host gate 与 reducer 提交 | Workflow Definition、legal transition relation、checkpoint |
  | State Machine | current state configuration、enabled transition、guard 与 terminal 的执行语义 | transition rules + deterministic program | 整个业务 Workflow、Plan、Trace |
  | Workflow | 较预定义步骤、分支与决策点组成的应用骨架 | definition + runtime；局部可调用 code / rule / Agent | 必然是某种状态机规范、必然包含 Agent、必然可恢复 |

- **Example / Figure Duty**：
  - F10-03：三圆不是“互斥集合”，而是三个控制责任镜头；图注标 `COURSE TAXONOMY / PARTIAL`。
  - 反例组：AWS 命名合并、LangGraph 同 runtime 组合 workflow / agent、Microsoft 双向 composition、OpenAI code / LLM 混合。
- **Guardrail / Counter-evidence**：
  - 不写“Workflow 一定比 Agent 更可靠”。
  - 不写“Agent Loop 没有状态机语义就不是 Agent”。
  - 不用产品名直接替代 control-owner 分析。
- **Boundary / Stop Line**：不进入 Multi-Agent topology；OpenAI Agents SDK source 用于 control-owner fact，不展开 handoff / multi-agent 章节。
- **Bridge**：比较轴建立后，需要给 State Machine 本身的核心词一个可审稿的窄定义。

---

## 4. 状态机最小词汇：State、Transition、Guard、Terminal

- **Reader Question**：哪些词有规范或产品锚点，哪些只是课程工作定义？
- **Section Goal**：为正文核心图准备术语地基，明确强弱：State / Transition / Guard / Terminal 可由 SCXML / AWS 窄化；Stage / Step / Invariant 要保持 Proposal。
- **Core Claim**：State configuration、Transition、Guard 与 Terminal 有规范 / 产品锚点，可映射到课程 runtime；Stage、Step、Invariant 的粒度是本文 source-informed 工作定义。
- **Claim IDs / Evidence IDs**：`10-C03`、`10-C04`；`10-E03`、`10-E04`。
- **Wording strength**：`10-C03 CONFIRMED / SPEC + PRODUCT-SCOPED`；`10-C04 PROPOSAL / SOURCE-INFORMED COURSE DEFINITION`。
- **Required term table duty**：

  | Term | 本课程工作定义 | Evidence status | 边界 |
  |---|---|---|---|
  | State | 当前已提交的控制位置与相关权威数据 | `CONFIRMED / SCXML-SCOPED + COURSE MAPPING` | 不是 history、Plan 或 checkpoint |
  | Transition | source 到 target 的一次合法状态变化 | `CONFIRMED / SCXML-SCOPED` | 不是 model suggestion、tool call 或 Plan item |
  | Guard | transition 是否 enabled 的布尔前置条件 | `CONFIRMED / SCXML cond MAPPING` | 不生成开放式候选，也不证明副作用完成 |
  | Terminal State | 当前 machine / workflow execution 不再推进的合法结束状态 | `CONFIRMED / SCXML + AWS-SCOPED` | terminal 不自动等于 success |
  | Stage | Workflow 中治理 / 可视化 / 责任分组 | `PROPOSAL` | 不是所有引擎标准对象 |
  | Step | Article 08 的 committed loop iteration 或本地可审计执行单元 | `PROPOSAL / REPOSITORY-LOCAL` | AWS 的 step 可指 state，不能跨产品换算 |
  | Invariant | 所有 reachable State 必须成立的 predicate | `PROPOSAL / SOURCE-INFORMED` | 不等于单条 edge guard |

- **Example / Figure Duty**：
  - F10-04：小状态图标出 state、edge、guard、terminal；图注说明不是 SCXML 完整教程。
  - 例子：`End: true` / Succeed / Fail 与 Article 08 terminal outcome 分开。
- **Guardrail / Counter-evidence**：SCXML 层级 / 并行语义不代表所有业务 workflow；AWS terminal form 不等于成功含义统一。
- **Boundary / Stop Line**：不教 SCXML 语法，不做 UML / BPM 教程，不展开 compensation。
- **Bridge**：术语落定后，进入本文中心工程判断：Agent 建议如何经过 deterministic validation 才能提交。

---

## 5. 中心机制：Model suggestion 不是 Legal transition

- **Reader Question**：模型说“下一步应该进入 VERIFIED / SUCCEEDED”，谁来判定它是否真的能跳？
- **Section Goal**：建立全文核心机制图：Agent 只给 suggestion；runtime 重新验证 source revision、definition edge、guard / authorization / Evidence、post-state invariant 与 terminal contract，最后才 commit State。
- **Core Claim**：改变 authoritative State 的 legal transition 由程序验证；Agent 只能提交 schema-bounded candidate suggestion。Agent Decision Point 只适合放在多个合法候选仍需语境判断的位置。
- **Claim IDs / Evidence IDs**：`10-C05`、`10-C07`、`10-C03`、`10-C01`；`10-E05`、`10-E07`、`10-E03`、`10-E01`。
- **Wording strength**：`10-C05 PROPOSAL / SOURCE-INFORMED CONTROL DESIGN`，`10-C07 PROPOSAL / COURSE INTERFACE DESIGN`。正文使用“本文建议 / 更稳的 commit protocol”，不得写成 official product guarantee。
- **Concrete mechanism duty**：

  ```text
  Agent Decision Point
      input: allowed state view + evidence refs + optional plan
      output: schema-bounded transition suggestion
           |
           v
  Deterministic validation
      current source / revision?
      edge exists in definition?
      guard / policy / authorization / evidence satisfied?
      applicable invariant still holds after commit?
      terminal reason / outcome derived from state contract?
           |
           v
  State commit or rejection
  ```

- **Required rejection examples**：
  1. source revision 已变化：拒绝 stale suggestion；
  2. target 不在 definition allowed edge：拒绝 illegal transition；
  3. required Evidence 缺失：拒绝 success transition；
  4. invariant 失败：拒绝 post-state commit；
  5. terminal output 结构正确但 success contract 不满足：派生 failure / incomplete，而非复制模型自报。
- **Example / Figure Duty**：
  - F10-05：三段图 `suggest -> validate -> commit`，必须阻断 `suggest -> State` 直连箭头。
  - 小伪代码不超过 15 行，表达验证顺序，不实现 SDK。
- **Guardrail / Counter-evidence**：
  - 来源支持 transition condition 与 code / LLM 控制分工，但没有规定本文五项 protocol。
  - 某些 runtime 允许模型选择 route 或 tool；本文不否认，只要求 authoritative commit 前验证。
- **Boundary / Stop Line**：不设计 retry、backoff、resume、side-effect idempotency；失败后如何继续留给 Article 11。
- **Bridge**：有了中心机制，再比较 Agent 放在 Workflow 里、Workflow 包成 Tool、或 code 全权编排三种落法。

---

## 6. 三种控制形态：Workflow 调 Agent、Agent 调受控 Workflow Tool、Code orchestration

- **Reader Question**：到底是 Workflow 包 Agent，还是 Agent 包 Workflow，还是全部由代码编排？
- **Section Goal**：用 official product facts 说明三种形态都可构造，真正要检查的是 control owner、validation boundary 与 State commit authority。
- **Core Claim**：Workflow 调 Agent、Agent 调受控 Workflow Tool 与 code orchestration 在引用的 current official products 中均可构造且可组合；它们不是互斥产品类型，也不天然形成可靠性排序。
- **Claim IDs / Evidence IDs**：`10-C06`、`10-C10`、`10-C05`；`10-E06`、`10-E10`、`10-E05`。
- **Wording strength**：`10-C06 CONFIRMED / CITED-PRODUCTS-SCOPED`，只能写引用产品范围内可构造；`10-C10 CONFIRMED` 用作 counter-evidence。
- **Required control-shape table duty**：

  | Shape | Control owner | Agent freedom | Deterministic boundary | Evidence scope |
  |---|---|---|---|---|
  | Workflow -> Agent | Workflow 决定何时进入 Agent node / function | Agent 在 bounded input 内动态判断 | entry / exit schema、allowed next edge、postcondition | Microsoft Functional Workflow docs |
  | Agent -> controlled Workflow Tool | Agent 选择是否请求窄入口 | 选择是否调用与合约参数 | tool schema、policy、内部 guards / invariants | Microsoft workflow-as-agent、OpenAI FunctionTool |
  | Code orchestration | Application code 持有 sequence / branch / loop | Agent 只在被调用点产生输出或候选 | code 决定 flow 并检查 structured output | OpenAI code orchestration docs |

- **Example / Figure Duty**：
  - F10-06：三种控制形态的同题对照，不画产品类图。
  - 例子：构建诊断可由 workflow 固定 `Intake -> Evidence -> Review`，其中某个 Evidence 分支调用 Agent；也可让 Agent 调用一个窄的 `run_evidence_workflow` tool；还可由代码显式 sequence。
- **Guardrail / Counter-evidence**：
  - Microsoft Functional Workflow API 有 experimental scope；正文要保留。
  - OpenAI FunctionTool pipeline 不证明任意 workflow 自动获得内部 guard / invariant。
  - 不比较哪个框架成熟、不推荐产品。
- **Boundary / Stop Line**：不展开 Multi-Agent，即使 OpenAI 页面使用 multi-agent 示例；本篇只取 orchestration control-owner fact。
- **Bridge**：三种形态都绕不开同一个接口问题：什么时候值得让 Agent 做 Decision Point。

---

## 7. Agent Decision Point：只放在合法候选仍需语境判断的位置

- **Reader Question**：什么判断适合交给 Agent，什么判断不应该每次让模型重想？
- **Section Goal**：给出 Agent Decision Point 的窄接口，避免把 guard、权限、枚举判断或完整性校验交给 prompt。
- **Core Claim**：Agent Decision Point 只在确定性过滤后仍有多个 legal candidate，且选择依赖非结构化、多源或语境化 Evidence 时使用；输入 / 输出受 schema 与 guard 约束，最终仍由 runtime 验证。
- **Claim IDs / Evidence IDs**：`10-C07`、`10-C05`；`10-E07`、`10-E05`。
- **Wording strength**：`PROPOSAL / COURSE INTERFACE DESIGN`。必须写“本文的接口设计”，不能写成官方标准或运行时事实。
- **Interface duty**：

  ```yaml
  # COURSE PROPOSAL / NOT EXECUTED
  allowed_state_view:
    current_state: "SOURCE_READY"
    legal_targets: ["VERIFIED", "FAILED"]
  evidence_refs:
    - "EV-LOG-001"
    - "EV-FILE-001"
  optional_plan_ref: "plan-v2"
  output_schema:
    suggested_transition: "VERIFIED | FAILED"
    rationale_ref: "evidence ids only, not hidden CoT"
  runtime_result:
    status: "COMMITTED | REJECTED"
  ```

- **Example / Figure Duty**：
  - T10-04：适合 Agent / 不适合 Agent 的判断对照。
  - 适合：两个合法诊断分支都满足 guard，需要阅读多源 Evidence 选择下一调查方向。
  - 不适合：source state 是否匹配、edge 是否存在、权限是否通过、required field 是否非空、Evidence ID 是否 allowlisted。
- **Guardrail / Counter-evidence**：
  - 产品可以让 Agent 选择工具或 route，不代表所有选择都先枚举完整候选。
  - Schema 约束不让输出天然合法；仍需 guard / policy / invariant 验证。
- **Boundary / Stop Line**：不讨论完整 Context Engineering、Working Memory 或 RAG；Decision Point 的 input 只作为后续 Article 12+ 的桥。
- **Bridge**：抽象接口需要一条坏 trace 来说明它解决什么，但 AL-04 只能作为 bounded raw facts + proposal overlay。

---

## 8. AL-04 双层案例：Observed raw facts 与 State Machine overlay 分开

- **Reader Question**：怎样用 AL-04 说明自由 Loop 的重复和伪完成风险，同时不伪造“Workflow runtime 已拒绝非法 transition”？
- **Section Goal**：严格建立双层表：下层只写 Lab 03 raw observed facts；上层是 Article 10 的 proposed State Machine overlay，标 `PROPOSAL / NOT EXECUTED`。
- **Core Claim**：AL-04 直接证明 fixed fixture 中发生了语义重复、no progress、required Evidence 缺失和 fake success 被拒绝；illegal-transition、stage skip 与 guard rejection 只属于分析 overlay。
- **Claim IDs / Evidence IDs**：`10-C08`、`10-C05`、`10-C07`；`10-E08`、`10-E05`、`10-E07`。
- **Wording strength**：Raw facts 为 `CONFIRMED / FIXTURE-SCOPED`；overlay 为 `PROPOSAL / NOT EXECUTED`。两者不可合并。
- **Required observed table duty**：

  | Order | Observed action / state | Classification |
  |---|---|---|
  | 0 | `REQ_LOG / REQ_SOURCE` unresolved；accepted Goal Evidence 为空 | `OBSERVED` |
  | 1 | 读取 `Unrelated.cs`，Tool success 但 `goal_relevant=false / NO_PROGRESS` | `OBSERVED` |
  | 2 | 同一 action fingerprint 再次读取同一文件；goal-state digest 不变 | `OBSERVED` |
  | 3 | 请求 `SUCCEEDED` 并引用 `EV-FAKE` | `OBSERVED` |
  | 4 | completion validation 返回 `STOP_CONTRACT_FAILED / FAILED`；requirements 仍 unresolved | `OBSERVED` |

- **Required overlay table duty**：

  | Proposed edge | Proposed deterministic guard | AL-04 mapping |
  |---|---|---|
  | `INTAKE -> LOG_READY` | 已接受 Goal 相关 log Evidence | 两次 unrelated read 均不能越过 |
  | `LOG_READY -> SOURCE_READY` | 已接受与 log 关联的 source Evidence | 未到达 |
  | `SOURCE_READY -> VERIFIED` | required Evidence 已接受且无 unresolved failure | 未到达 |
  | `VERIFIED -> SUCCEEDED` | output / Evidence / success completion contract 满足 | `EV-FAKE` 请求应被拒绝 |
  | `any -> FAILED` | deterministic terminal rule 触发并保存 failure reason | raw fixture 实际 failed，但未执行 overlay edge |

- **Example / Figure Duty**：
  - F10-07：上下双泳道，上层 `PROPOSAL / NOT EXECUTED State Machine overlay`，下层 `OBSERVED AL-04 trace`。
  - 不贴大段 JSONL；只摘 action fingerprint 相同、goal-state digest 不变、`EV-FAKE` rejected 等必要字段。
- **Guardrail / Counter-evidence**：
  - AL-04 使用 scripted decisions，不代表真实模型统计行为。
  - raw artifacts 没有 Workflow Definition、State Machine Runtime 或 transition event。
  - 不证明 State Machine 自动修复 planning quality。
- **Boundary / Stop Line**：不创建新 Lab，不修改 Lab 03；Article 10 Required Lab 为 `NONE`。
- **Bridge**：案例暴露坏法后，下一节转化为设计审查清单。

---

## 9. 一个坏 State Machine / Workflow + Agent 实现通常怎么坏

- **Reader Question**：团队已经画了状态图、写了 workflow，还可能怎样让模型越权或让状态失真？
- **Section Goal**：把前文 Claim 转成 review heuristics，不新增 failure taxonomy。
- **Core Claim**：坏实现通常不是“没有状态机”，而是把 candidate、legal edge、current state、trace、checkpoint 与 success 混在一起，导致模型建议绕过 deterministic commit boundary。
- **Claim IDs / Evidence IDs**：`10-C01`—`10-C08`、`10-C10`；`10-E01`—`10-E08`、`10-E10`。
- **Wording strength**：这是由既有 Claim 直接教学转写的 design-review heuristic，不宣称穷举所有 failure mode。
- **Bad implementation examples**：
  1. 把 Plan item、Workflow Definition、Runtime State 与 Trace 混成一个 `flow` 字段；
  2. 模型输出 `next_state=SUCCEEDED` 后直接写 State；
  3. Guard 用 prompt 描述，不在提交点重新执行；
  4. Tool success 就推进 workflow，未检查 goal Evidence；
  5. history 增长就判有进展，不看 goal-state；
  6. terminal 只用 `done=true`，不分 stopped / failed / incomplete / succeeded；
  7. Workflow-as-tool 暴露太宽，让 Agent 自由跳过内部 stage；
  8. 把 current state 序列化后就宣称支持 checkpoint / recovery。
- **Example / Figure Duty**：
  - T10-05：每个坏法绑定对应 Claim 与最小反问，例如“谁拥有 legal edge？”“谁提交 State？”“哪个 guard 在 commit 前执行？”
  - AL-04 只用于坏法 4、5、6 的 bounded evidence，不覆盖所有坏法。
- **Guardrail / Counter-evidence**：现实产品可以把多个职责放在同 runtime；坏法检查职责，不检查类名或部署拓扑。
- **Boundary / Stop Line**：不扩展到 Article 21 failure taxonomy、Article 22 Eval / Regression 或 production reliability 结论。
- **Bridge**：坏法说明“确定性骨架”不是要消灭 Agent，而是让可变判断和稳定约束各在正确层。

---

## 10. 工程边界：让 Agent 处理不确定，让程序守住不变量

- **Reader Question**：一套工程系统里，哪些部分应该稳定，哪些部分可以交给 Agent 动态判断？
- **Section Goal**：收束核心工程判断，并把本篇自然桥接到 Article 11。
- **Core Claim**：可变的是 candidate selection 与 evidence-sensitive judgment；稳定的是 legal transition、guard、policy、authorization、invariant、terminal completion contract 与 authoritative State commit。
- **Claim IDs / Evidence IDs**：`10-C05`、`10-C07`、`10-C10`、`10-C09`；`10-E05`、`10-E07`、`10-E10`、`10-E09`。
- **Wording strength**：`C05 / C07` 保持 Proposal；`C10` confirmed counter-evidence；`C09` confirmed product-scoped stop line。
- **Engineering judgment duty**：
  - **Agent适合**：对多个合法候选做上下文 / Evidence 判断，给出 schema-bounded suggestion；
  - **Program / Workflow适合**：维护 definition、edge、guard、state revision、authorization、invariant、terminal；
  - **Trace适合**：记录发生过的 decision / transition / tool / state revision；
  - **Checkpoint另算**：持久化 identity、next、metadata、parent / tasks 与 resume boundary。
- **Article 11 bridge duty**：
  - 只保留一句：`State 描述当前位置；Checkpoint 把可恢复位置、持久化边界与 continuation metadata 绑定起来。`
  - 不解释 retry、cancellation、resume、replay、副作用去重 / compensation 或 durability tradeoff。
- **Example / Figure Duty**：
  - F10-08：`dynamic candidate layer` 覆盖在 `deterministic commit skeleton` 之上，再以虚线指向 Article 11 `checkpoint boundary`。
  - 反例：把 current state JSON 存盘，不能直接宣称 recovery solved。
- **Guardrail / Counter-evidence**：
  - LangGraph checkpoint 字段不是行业统一 schema；只用于反驳 current State = Checkpoint。
  - 不把 BuildPilot Design 写成已存在 Runtime。
- **Boundary / Stop Line**：Explicit non-scope 全量保留；不提前写 Article 11、20、21、22、23 或 DSH / BuildPilot。
- **Closing takeaway**：`好的 Agent Workflow 不是让模型拥有更多跳转自由，而是把自由收窄到真正需要判断的 Decision Point，并让每一次状态推进都能被程序证明合法。`

---

## 11. Figures, tables and examples plan

| ID | 位置 | 形式 | 教学职责 | Claim / Evidence binding | 禁止表达 |
|---|---|---|---|---|---|
| F10-01 | Section 1 | gap diagram | 展示自由 Loop / Plan 与确定性骨架缺口 | C01, C02, C05, C08 / E01, E02, E05, E08 | Planning 自动可靠或 Workflow 已运行 |
| T10-01 | Section 2 | object contract table | 分开 Plan / Definition / Runtime State / Trace | C01 / E01 | 四对象必须四文件存储 |
| F10-02 | Section 2 | producer-consumer diagram | 标出对象 authority 与证明力 | C01 / E01 | 同容器即同语义 |
| T10-02 | Section 3 | comparison table | 比较 Agent Loop / State Machine / Workflow 的课程责任面 | C02, C10 / E02, E10 | 行业统一 taxonomy |
| T10-03 | Section 4 | term table | 区分 confirmed terms 与 proposal terms | C03, C04 / E03, E04 | Stage / Step / Invariant 标准化 |
| F10-05 | Section 5 | validation pipeline | 阻断 model suggestion 直写 State | C05, C07 / E05, E07 | product guarantee 或已执行 runtime |
| T10-04 | Section 7 | Agent Decision Point suitability table | 说明什么适合 Agent、什么必须确定性校验 | C05, C07 / E05, E07 | prompt 替代 guard |
| F10-07 | Section 8 | observed/proposal dual-lane trace | 分开 AL-04 raw facts 与 overlay | C08 / E08 | illegal transition observed |
| T10-05 | Section 9 | bad implementation review table | 把 Claim 转成设计审查问题 | C01-C08, C10 | failure taxonomy 穷举 |
| F10-08 | Section 10 | layered boundary diagram | 动态候选层、确定性提交层、Article 11 checkpoint 边界 | C05, C07, C09, C10 | recovery 已解决 |

### Example responsibilities

1. **构建失败调查例子**只负责说明阶段、Evidence 与 success contract 为什么不能由模型自证。
2. **产品 composition 例子**只负责说明三种 control owner 都存在，不做产品选型。
3. **AL-04**是唯一 bounded bad trace；只展示 repeat/no-progress/fake-success rejection 与 overlay，不新增 Lab。
4. **Agent Decision Point YAML**只是课程接口 Proposal，不作为 runtime artifact。
5. **Current State vs Checkpoint**只保留 bridge，不展开 Article 11。

图表实施规则：优先使用 Mermaid / Markdown table；所有涉及课程 taxonomy、Agent Decision Point、AL-04 overlay 的 caption 必须标 `PROPOSAL` 或 `PROPOSAL / NOT EXECUTED`；所有 raw Lab 字段必须标 `OBSERVED / FIXTURE-SCOPED`。当前 Outline 不创建 `assets/`。

---

## 12. Learning Check

### Check 1｜对象证明力

- **题目**：某系统有一份 Workflow Definition、一份 Plan 和一段 Trace。它们分别能证明什么，为什么仍不能直接证明任务成功？
- **参考思路**：Definition 证明配置的候选合法路径，Plan 证明准备考虑什么，Trace 证明记录中发生过什么；成功还需要 current State、Evidence 与 completion contract。
- **Claims**：C01。

### Check 2｜课程 taxonomy

- **题目**：看到 AWS 把 state machine 称为 workflow，是否推翻本文“Agent Loop / State Machine / Workflow 分开讨论”的必要性？
- **参考思路**：不推翻，但要求本文只按课程责任面比较，不写成行业统一分类。`10-C02`保持 PARTIAL。
- **Claims**：C02, C10。

### Check 3｜Legal transition

- **题目**：模型输出 `target_state=SUCCEEDED`，还需要哪些 deterministic checks 才能提交？
- **参考思路**：检查 source / revision、definition edge、guard、policy / authorization、required Evidence、post-state invariant、terminal completion contract；模型输出只是 suggestion。
- **Claims**：C03, C05, C07。

### Check 4｜Guard 与 Invariant

- **题目**：Guard 和 Invariant 为什么不能混成一个“校验规则”？
- **参考思路**：Guard 决定某条 transition 本次是否 enabled；Invariant 是所有 reachable State 都应保持的 predicate。Invariant 的 commit 检查是本文 Proposal，不是产品统一 hook。
- **Claims**：C03, C04。

### Check 5｜AL-04 双层判读

- **题目**：AL-04 两次读取无关文件、goal-state digest 不变、`EV-FAKE` 被拒绝。能否说“Workflow runtime 拒绝了 `VERIFIED -> SUCCEEDED` transition”？
- **参考思路**：不能。repeat、no progress、fake-success rejection 是 OBSERVED；State Machine table 是 PROPOSAL / NOT EXECUTED overlay，没有 Workflow runtime 或 transition event。
- **Claims**：C08, C05。

### Check 6｜State vs Checkpoint

- **题目**：把 current State 序列化到磁盘，为什么还不能直接宣称支持 Resume / Recovery？
- **参考思路**：Checkpoint 还需要 durable identity、continuation / next、metadata、parent / tasks 和 resume boundary；retry、cancellation、replay、副作用语义留给 Article 11。
- **Claims**：C09。

---

## 13. Claim-to-section coverage

| Claim | Final status inherited from Evidence | Primary section | Supporting sections | Coverage duty | Result |
|---|---|---|---|---|---|
| `10-C01` | `CONFIRMED / PRODUCT + REPOSITORY-SCOPED` | 2 | 1, 9, 12 | Plan、Definition、Runtime State、Trace 的 producer / consumer / proof boundary | COVERED |
| `10-C02` | `PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED` | 3 | 1, 12 | Agent Loop、State Machine、Workflow 只作课程比较轴；不升级行业 taxonomy | COVERED |
| `10-C03` | `CONFIRMED / SPEC + PRODUCT-SCOPED` | 4 | 5, 12 | State / Transition / Guard / Terminal 的规范或产品锚点 | COVERED |
| `10-C04` | `PROPOSAL / SOURCE-INFORMED COURSE DEFINITION` | 4 | 9, 12 | Stage / Step / Invariant 保留工作定义；不跨产品等同 | COVERED |
| `10-C05` | `PROPOSAL / SOURCE-INFORMED CONTROL DESIGN` | 5 | 1, 6, 8, 9, 10, 12 | legal transition 由程序验证，Agent 只提交 suggestion | COVERED |
| `10-C06` | `CONFIRMED / CITED-PRODUCTS-SCOPED` | 6 | 10 | 三种 control-owner 形态均可构造且可组合；不做可靠性排序 | COVERED |
| `10-C07` | `PROPOSAL / COURSE INTERFACE DESIGN` | 7 | 5, 8, 10, 12 | Agent Decision Point 输入 / 输出 / guard boundary；不写成标准接口 | COVERED |
| `10-C08` | `CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY` | 8 | 1, 9, 12 | AL-04 raw facts 与 overlay 分层；illegal transition 不写成 observed | COVERED |
| `10-C09` | `CONFIRMED / LANGGRAPH-CURRENT-DOCS-SCOPED` | 10 | 12 | Current State 不等于 Checkpoint；Article 11 stop line | COVERED |
| `10-C10` | `CONFIRMED / COUNTER-EVIDENCE PRODUCT-SCOPED` | 3 | 6, 10 | 产品组合反驳唯一正确架构；只检查 control responsibility | COVERED |

Coverage result：`10 / 10 COVERED`。`10-C02`保持`PARTIAL`；`10-C04 / 10-C05 / 10-C07`保持`PROPOSAL`；`10-C08`明确分离`OBSERVED`与`PROPOSAL / NOT EXECUTED`。没有新增 Claim ID，没有把 Research / Evidence 的 Claim 状态升级。

---

## 14. Job Competency mapping

| Competency | 文章中的可观察产出 | 对应章节 |
|---|---|---|
| 架构分层与边界判断 | 分开 Plan、Definition、Runtime State、Trace、Agent suggestion 与 legal transition | 2, 5 |
| Runtime / control-plane 设计 | 定义 deterministic validation 与 State commit protocol | 5, 7 |
| 状态与流程建模 | 区分 State、Transition、Guard、Invariant、Terminal、Stage、Step | 4, 9 |
| 可靠性与 fail-closed 思维 | 不让模型自报 success；缺 Evidence、guard 失败或 invariant 失败时拒绝 commit | 5, 8, 9 |
| 证据纪律与验证边界 | 对 AL-04 采用 `OBSERVED / PROPOSAL / NOT EXECUTED` 双层，不把 overlay 写成 runtime fact | 8 |
| 技术选型与术语治理 | 用产品 counter-evidence 反驳唯一架构，按 control owner 而非类名比较 | 3, 6, 10 |
| 技术沟通 / Tech Lead 能力 | 用对象合同表、term table、validation pipeline 与 bad-implementation checklist 压缩跨团队歧义 | 2, 4, 5, 9 |

表达要求：职业能力通过模型、边界、例子与验证纪律隐式呈现；正文不出现求职自夸或职位宣言。

---

## 15. Source and publication link plan

### External primary / official sources

| Source | 用途 | 放置位置 | Scope label |
|---|---|---|---|
| W3C SCXML 1.0 Recommendation | State configuration、enabled transition、`cond`、top-level final | Sections 4, 5 | normative spec；不等于所有 business workflow |
| Lamport TLA+ inductive invariant note | invariant 与 reachable state predicate | Section 4 | formal note；不规定本文 runtime API |
| AWS Step Functions state machine concepts / ASL structure / GetExecutionHistory | definition / execution / state / history 分离；AWS state / step / workflow 术语反例 | Sections 2, 3, 4 | product-scoped；Standard / Express history limitation 保留 |
| LangGraph Workflows and agents | predetermined workflow 与 dynamic Agent 对照 | Sections 3, 6, 7 | current hosted docs；未绑定 package run |
| Microsoft Agent Framework Functional Workflow / Workflows as Agents | workflow 调 Agent、workflow-as-agent / agent tool | Section 6 | current docs；Functional Python API experimental scope 保留 |
| OpenAI Agents SDK orchestration / tools | LLM / code orchestration 可混合；FunctionTool runtime pipeline | Sections 5, 6, 7 | current hosted docs；不证明任意 workflow guard |
| LangGraph Checkpointers | StateSnapshot 中 values 之外的 identity / next / metadata / parent / tasks 等 checkpoint boundary | Section 10 | product-scoped；不定义通用 checkpoint schema |

### Internal dependency and fixture links

- Article 08 published content：承接 `Decision candidate -> Act -> Tool Outcome -> Observation -> State -> terminal`，尤其 AL-04 raw facts；不重讲完整 Agent Loop。
- Article 09 published content：承接 Plan 是 remaining candidate，不等于 Execution、Verified State、Authorization 或 Workflow；不重讲 Planning pattern。
- Lab 03 README / execution log / run-a raw artifacts：Section 8 只摘 AL-04 claim-relevant facts；不复制大段 JSONL。
- Article 11：Published Content 阶段只做下一篇 forward link（若发布路径存在时使用 `relref`）；当前 Outline 只写 stop line，不制造未发布 shortcode。
- Article 12+ / 20 / 21 / 22 / 23 / DSH / BuildPilot：只在 explicit non-scope 中按课程路线提示，不作证据来源。

Link implementation note：Published Content 阶段才写 Hugo `relref`，shortcode 使用 ASCII 双引号；当前 Outline 只保存 source title / URL 与 repository path 计划。外部链接严格复用 Research / Evidence 已冻结 URL，不新增检索来源。

---

## 16. Length budget

目标正文：`5,200—6,600` 中文字（不含 frontmatter、链接注释与图表 caption）。

| Section | Budget | 压缩策略 |
|---|---:|---|
| Opening + Section 1 | 500—650 | 用 Article 08 / 09 缺口立问题，不复述两篇正文 |
| Section 2 | 600—750 | 对象表为主，不展开 AWS API 细节 |
| Section 3 | 550—700 | 课程比较轴 + counter-evidence，不做产品综述 |
| Section 4 | 650—850 | term table 承担定义；SCXML / TLA+ 只作锚点 |
| Section 5 | 850—1,050 | 全文中心；保留 validation pipeline 与 rejection examples |
| Section 6 | 600—750 | 三形态对照，不做框架选型 |
| Section 7 | 550—700 | Agent Decision Point 接口 + 适用 / 不适用对照 |
| Section 8 | 850—1,050 | AL-04 双层表为核心；不复制 raw JSONL |
| Section 9 | 400—550 | 反模式绑定 Claim，不扩 failure taxonomy |
| Section 10 + closing | 350—500 | 用 Article 11 stop line 收口，不展开 recovery |

若超长，优先删减产品例子、反例解释与 term 背景，不删 `model suggestion != legal transition`、`10-C02 PARTIAL`、`10-C04 / C05 / C07 PROPOSAL`、AL-04 双层标签、Article 11 stop line 或 explicit non-scope。

---

## 17. Explicit non-scope

- 不做 BPM、UML、SCXML 或 AWS Step Functions 教程。
- 不把课程 State / Stage / Step / Workflow 命名写成行业标准。
- 不把 Agent Loop、State Machine、Workflow 写成三种互斥产品类型或唯一正确架构。
- 不把 `10-C02 PARTIAL` 升级为 confirmed industry taxonomy。
- 不把 `10-C04 / 10-C05 / 10-C07` 的 Proposal 写成 observed runtime、official standard 或 product guarantee。
- 不声称 model suggestion 本身就是 legal transition、authoritative State update 或 terminal success。
- 不声称 AL-04 观察了 Workflow runtime、illegal transition、stage skip、guard rejection、automatic repair、planning quality 或 production reliability。
- 不启动 Lab 04，不创建或引用 Article 11 workspace，不执行新实验。
- 不展开 Checkpoint storage、Retry、Cancellation、Resume、Replay、Recovery、side-effect idempotency、compensation 或 durability tradeoff。
- 不引入 Multi-Agent topology、handoff governance 或 shared state。
- 不展开 Context Engineering、Working Memory、Session、RAG、Skill、Budget、Trace / Replay / Eval / Regression。
- 不读取 DSH 源码，不实现或预演 BuildPilot Runtime。
- 不对 Microsoft experimental API、OpenAI / LangGraph current hosted docs补写未核验 package-version 保证。

---

## 18. New Core Facts Audit

| Proposed outline statement | Basis | Audit result |
|---|---|---|
| Plan / Definition / Runtime State / Trace 分离 | C01 / E01 | existing confirmed fact；不要求四文件实现 |
| Agent Loop / State Machine / Workflow 的比较轴 | C02 / E02, E10 | existing PARTIAL；保留课程 taxonomy 标签 |
| State / Transition / Guard / Terminal 定义 | C03 / E03 | existing spec + product scoped fact |
| Stage / Step / Invariant 工作定义 | C04 / E04 | existing Proposal；不升级标准对象 |
| legal transition 由 deterministic program 验证 | C05 / E05 | existing control design Proposal；用“应 / 本文采用” |
| 三种 control-owner 形态可构造 | C06 / E06 | existing cited-products-scoped fact；不做可靠性排序 |
| Agent Decision Point 窄接口 | C07 / E07 | existing course interface Proposal；不声称执行过 |
| AL-04 raw facts + overlay | C08 / E08 | raw facts confirmed；overlay保持`PROPOSAL / NOT EXECUTED` |
| Current State 不等于 Checkpoint | C09 / E09 | existing LangGraph-current-docs-scoped fact；不定义 recovery |
| 产品组合反驳唯一架构 | C10 / E10 | existing counter-evidence；只检查职责 |

Audit result：`NO NEW CORE FACT REQUIRED`。不存在需要退回 Research 的新核心 Claim；Draft 不得新增 Workflow runtime behavior、State Machine execution result、Article 11 recovery semantics、product version guarantee、production reliability、DSH source fact 或 BuildPilot Runtime fact。

---

## 19. Outline Gate checklist

- [x] Article type 已明确为原理 / 机制桥接篇，Mode=`NORMAL_ARTICLE`。
- [x] Teaching Spine 完整覆盖 `problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary`。
- [x] 每个正文 section 都包含 Reader Question、Section Goal、Claim / Evidence binding、wording strength、guardrail / counter-evidence、boundary / stop line 与 bridge。
- [x] Claim-to-section coverage 为 `10 / 10`。
- [x] `10-C02` 保持 `PARTIAL`；`10-C04 / 10-C05 / 10-C07` 保持 `PROPOSAL`。
- [x] `model suggestion != legal transition` 已作为中心机制显式建立。
- [x] AL-04 明确分为 `OBSERVED` raw facts 与 `PROPOSAL / NOT EXECUTED` State Machine overlay。
- [x] Current State 与 Checkpoint 边界保留 Article 11 stop line。
- [x] Figures / Tables / Examples 职责已定义；当前未创建 `assets/`。
- [x] Learning Check、Job Competency mapping、Source / publication link plan、Length budget 与 Explicit non-scope 已定义。
- [x] New Core Facts Audit=`NO NEW CORE FACT REQUIRED`。
- [x] 未创建 Draft、Review、README、Published Content、Lab、Article 11 workspace 或 global state。
- [x] 未执行 Git branch / commit / push。

Gate candidate：`PASS_RECOMMENDED`。最终 Outline Gate 结论由 Master 验证；Author 不修改 global durable state，也不自批 `FINAL`。
