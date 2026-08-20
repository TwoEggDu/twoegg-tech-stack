# Article 08 Detailed Outline｜Agent Loop

> Outline Gate candidate：`PASS_RECOMMENDED`。本文件是 Author 的 Detailed Outline 候选，不是正文、Formal Review、Final Gate 或发布批准。

## 0. Article decision

### Article type

- 类型：`原理篇（Major Core Lesson / LAB_ARTICLE）`。
- 选择理由：本篇要先纠正“一次 Tool Use 就是 Agent”的问题定义，再建立不依赖某个 SDK 的课程抽象，最后用 Lab 03 的 fixed Host 轨迹落地；不适合写成 API 对照或单次故障案例。
- 结构：`问题空间 -> 抽象模型 -> 具体机制 -> Lab 验证 -> 工程判断与边界`。

### 最短 thesis

`Agent Loop 不是“模型调一次工具”，而是 Host 在有界 Run 中反复提交 Step：把 Decision candidate 经 Act、Tool Outcome、Observation 和 State 更新变成下一次 Decide 的输入，并独立判定 Continue / Stop 与 Success。`

### Reader Change

读前，读者容易把 `turn`、模型调用、工具调用和一次任务混成同一粒度，把 Tool Result 当成 Observation / State / Evidence，并把“模型说完成”或“循环停了”当成成功。读后，读者应能：

1. 在使用前先声明 `Run / Turn / Step` 的产品或课程 scope；
2. 画出并解释 `Decide -> Act -> Tool Outcome -> Observation -> State -> Continue / Stop`；
3. 指出 Decision、工具结果与权威状态各由谁提出、执行和提交；
4. 设计有界停止条件，并把 `STOPPED` 与 `SUCCEEDED` 分字段判断；
5. 从 Lab 轨迹中识别真实完成、未解决工具失败、步数耗尽和重复无进展伪完成。

### Teaching Spine

1. **Problem space**：一次 Tool Use 只闭合一次调用意图与结果返回，没有自动形成反馈循环或完成合同。
2. **Abstract model**：先限定 Run / Turn / Step，再分开 Decision candidate、Action、Tool Outcome、Observation 与 authoritative State。
3. **Concrete mechanism**：Host 在每个 committed Step 执行 gate、correlation / normalization、reducer 与 terminal derivation。
4. **Engineering judgment**：停止来源是组合判定；`STOPPED` 只描述生命周期，不能替代成功语义。
5. **Verification boundary**：Lab 03 的四条 deterministic 轨迹只证明 fixed environment / fixture / Host 的实现符合设计，不证明真实模型、Provider 或生产可靠性。

---

## 1. 为什么一次 Tool Use 还不是 Agent Loop

- **Reader Question**：Article 05—07 已经讲了 Tool Call、Tool Runtime 和 MCP，为什么还需要 Agent Loop？
- **Core Claim**：一次 Tool Use 可以表达调用意图、执行并返回相关结果，但只有当应用把结果纳入下一次判断、提交新状态，并由外部控制面继续或停止时，反馈循环才成立。
- **Claim IDs / Evidence IDs**：`08-C01`、`08-C04`；`E-08-01`、`E-08-03`、`E-08-06`、`E-08-07`、`E-08-08`；local dependencies `R-02`、`R-03`、`R-04`。
- **Teaching Duty**：接住前三篇而不重复教程；把读者的关注点从“能不能调用工具”转到“谁把一次结果变成下一步输入，谁判定任务结束”。
- **Example / Figure Duty**：
  - 先画“单次 Tool Use”：`Call -> Runtime -> Result`。
  - 再在右侧补上缺口：`Result -?-> next Decide`、`Result -?-> State`、`final -?-> Success`。
  - 用同一个 mock build-log 任务说明：解析工具成功返回，不等于已经收集到完成任务所需的全部 Evidence。
- **Boundary / Must-not-imply**：
  - 不写成 Article 07 已证明“安全调用外部能力”；它只闭合协议 envelope 与 capability boundary。
  - 不把 Tool Result 写成 Evidence，也不把协议 success 写成任务 success。
  - 不展开 Tool Runtime 的 validate / policy / timeout 细节，那是 Article 06 的职责。
- **Transition**：既然“是否形成循环”取决于结果如何推进下一次判断，就必须先冻结循环的计数与分组单位。

---

## 2. 先限定词义：Run、Turn 与 Step 不是天然同一个计数器

- **Reader Question**：一轮用户对话、一次模型调用、一次图迭代和一次工具动作，哪个才叫 Turn 或 Step？
- **Core Claim**：cited products 的计数语义不同，不能直接等同；本文只为 Article 08 / Lab 03 提供课程工作定义，而不是建立行业标准。
- **Claim IDs / Evidence IDs**：`08-C01`、`08-C02`、`08-C03`；`E-08-01`、`E-08-02`、`E-08-05`、`E-08-08`、`E-08-11`。
- **Teaching Duty**：先用反例消除术语幻觉，再提供后文可执行、可追踪的局部词汇。
- **Abstract Model**：
  - **Run（课程工作定义 / PROPOSAL）**：从一个冻结目标和初始 State 开始，直到 Host 写出 terminal record 的一次 goal-bounded invocation。
  - **Turn（课程工作定义 / PROPOSAL）**：外部交互分组；Lab 03 中一个 `turn_id` 绑定一个输入目标和一个 Run，不承担 loop counter 语义。
  - **Step（课程工作定义 / PROPOSAL）**：一次 committed loop iteration；读取 step-before State 和 Decision candidate，提交 ACT 路径或 REQUEST_STOP 校验路径，并留下 before / after state version。
- **Comparison table duty**：

  | 术语 | 本文只允许的表述 | 禁止偷换 |
  |---|---|---|
  | OpenAI `max_turn` | cited Python SDK current docs 中的一次 AI invocation | 用户对话轮、Lab Step、工具调用数 |
  | logical chat turn | cited guide 对整个 run 的外部描述 | `max_turn` counter |
  | LangGraph `super-step` | graph iteration，可包含并行 nodes | 模型调用或 Lab Step |
  | Lab 03 `Step` | 本文定义的 committed iteration | universal SDK unit |

- **Example / Figure Duty**：用 `Run R1 / Turn T1 / Step S1..S3` 的嵌套图展示“一个外部分组内可以提交多个 Step”；不画成所有 SDK 的统一层级。
- **Boundary / Must-not-imply**：
  - 每次出现产品术语都带产品 / counter scope。
  - `08-C03` 始终标作 `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED`，Lab 跑通也不升级成 glossary 全局事实。
  - 不进入 Article 09 的 plan step、replanning 或 search node。
- **Transition**：计数单位冻结后，下一步才可以定义一个 committed Step 到底提交哪些信息。

---

## 3. 一个 Step 提交什么：Decide、Act、Tool Outcome、Observation 与 State

- **Reader Question**：Tool Result 何时才算 Observation？模型、工具和 Host 谁能修改任务状态？
- **Core Claim**：在 cited products 中，model-visible tool result 与 authoritative state update 可以是不同操作；本文进一步采用 Host-owned reducer 的课程设计，把 Decision 当候选，把 Tool Outcome 正规化为 Observation，再由 Host 提交 authoritative State。
- **Claim IDs / Evidence IDs**：`08-C04`、`08-C05`；`E-08-01`、`E-08-03`、`E-08-06`、`E-08-07`、`E-08-08`、`E-08-10`、`E-08-11`。
- **Teaching Duty**：把“模型决定了”“工具返回了”“状态更新了”拆成三个所有权不同的工程动作；这是全文中心抽象。
- **Abstract Model**：

  ```text
  State(n)
     |
     v
  Decide -> Decision candidate
     |
     v
  Host action gate -> Act -> Tool Outcome
                            |
                            v
                  correlate / normalize
                            |
                            v
                       Observation
                            |
                            v
                   Host reducer commits
                            |
                            v
                        State(n+1)
  ```

- **Concept duties**：
  - **Decide**：产生 `ACT` 或 `REQUEST_STOP` 等候选，不直接写权威结论。
  - **Act**：Host 在工具名、参数、policy、重复动作等 gate 之后允许的执行路径。
  - **Tool Outcome**：Tool Runtime 的 correlated execution record，保留 status、code、data / error。
  - **Observation（课程抽象）**：Host 对 Tool Outcome 做 correlation check、normalization 与安全裁剪后，允许进入下一次 Decide 输入的记录。
  - **State**：Host reducer 根据旧 State、Decision 与 Observation 提交的新 authoritative snapshot；模型或工具不直接覆盖。
- **Example / Figure Duty**：
  - 正例：`parseMockLog` 返回成功 Tool Outcome，经 normalizer 形成 Observation，reducer 增加 Evidence 与 state revision。
  - 失败例：`MOCK_PARSE_FAILED` 仍被正规化为 `TOOL_FAILURE` Observation；“Observation 流转成功”不把工具失败改成成功。
  - 表格对比 Tool Outcome / model-visible item / Observation / State / Evidence 的来源、消费者、是否权威。
- **Boundary / Must-not-imply**：
  - Host-only reducer 是 `08-C05` 的课程安全设计，不是 OpenAI、LangGraph 或所有框架的强制实现。
  - 不把 `Observation` 写成 universal SDK API 名称。
  - Observation 是任务推进输入，不等于 Evidence；Evidence 仍要求可复核来源与解释链。
  - 不深入 Context packing、Memory 或 RAG。
- **Transition**：有了新 State，循环还必须回答两个独立问题：是否继续运行，以及停下时是否真的成功。

---

## 4. Continue / Stop 是 Host 的组合判定，Stopped 不等于 Succeeded

- **Reader Question**：模型给出 final、工具返回成功、达到上限或发生错误时，Run 应怎样结束？
- **Core Claim**：stop 可以来自 model signal、runtime/config、limit、policy、错误或取消；bounded termination 只保证“不会无限继续”，不保证目标成功。Host 应把 lifecycle、termination reason 与 outcome 分开派生。
- **Claim IDs / Evidence IDs**：`08-C01`、`08-C06`、`08-C07`；`E-08-01`、`E-08-02`、`E-08-03`、`E-08-05`、`E-08-06`、`E-08-09`、`E-08-10`、`E-08-11`、`E-08-12`。
- **Teaching Duty**：把“停止信号”降级为候选输入，把成功提升为需要验证的 completion contract；解释 boundedness 与 correctness 是两种性质。
- **Abstract Model**：
  - **Continue inputs**：新 Observation / State、剩余允许动作、未满足目标、runtime policy、limit 尚未触发。
  - **Stop inputs**：`REQUEST_STOP` candidate、output contract、goal invariant、required Evidence、unresolved failure、max-step guard、policy / error / cancellation。
  - **Terminal fields**：`lifecycle`、`termination_reason`、`outcome` 分开记录。
- **Required table duty**：

  | Termination reason | Lifecycle | Outcome | 本篇解释 |
  |---|---|---|---|
  | `GOAL_SATISFIED` | `STOPPED` | `SUCCEEDED` | output、goal、Evidence、unresolved-failure contract 全通过 |
  | `STOP_CONTRACT_FAILED` | `STOPPED` | `FAILED` | candidate 声称成功，但事实 / Evidence 不足 |
  | `UNRESOLVED_TOOL_FAILURE` | `STOPPED` | `FAILED` | 仍有未解决的 normalized tool failure |
  | `MAX_STEPS_EXHAUSTED` | `STOPPED` | `INCOMPLETE` | 外部 Step counter 到限，不冒充完成 |
  | `CANCELLED` | `STOPPED` | `INCOMPLETE` | 只作设计边界；Lab 03 未实测该轨迹 |
  | `HOST_FAILURE` | `STOPPED` | `FAILED` | runtime 自身失败；Lab 03 四轨迹不覆盖 |

- **Example / Figure Duty**：画两阶段判定：`Should stop?` 与 `What outcome?`，用红色阻断“Stop -> Success”的直接箭头。
- **Boundary / Must-not-imply**：
  - 模型的 `REQUEST_STOP` 是 Decision candidate，不是成功裁决。
  - Lab `max_steps` 不等于 OpenAI `max_turns`、LangGraph recursion limit、token / cost / latency budget。
  - cancellation 是 cooperative boundary，不承诺回滚外部副作用；checkpoint / resume / recovery 留给 Article 11。
- **Transition**：抽象判定需要一个最小 Host 执行骨架，才能从概念落到可追踪的实现责任。

---

## 5. 最小 Host Loop 骨架：每个 Step 只提交一次

- **Reader Question**：如果不依赖特定 Agent SDK，一个可审计的最小循环需要哪些控制点？
- **Core Claim**：本文的最小实现把 Decision source、Tool Runtime、Observation normalization、Host reducer 与 completion validator 分开；每个 Step 产生一个可关联的 trace / state transition，外部 guard 可在下一次 Decide 前终止 Run。
- **Claim IDs / Evidence IDs**：`08-C03`、`08-C04`、`08-C05`、`08-C06`、`08-C07`；`E-08-08`、`E-08-10`、`E-08-11`。
- **Teaching Duty**：让读者能把抽象模型落实为实现分工，但避免写成 Lab 源码逐行讲解或框架 API 教程。
- **Concrete mechanism duty**：用不超过 20 行的伪代码表达以下顺序：
  1. 建立 goal-bounded Run 与初始 State；
  2. 在请求新 Decision 前检查 cancellation / policy / `max_steps`；
  3. 获取 Decision candidate；
  4. `ACT`：action gate -> tool execute -> correlate / normalize -> Observation -> Host reducer；
  5. `REQUEST_STOP`：completion validator 基于 output、goal、Evidence 与 unresolved failures 派生 terminal；
  6. 写 Step trace、state version 或 terminal record；
  7. 继续或退出。
- **Example / Figure Duty**：
  - Figure：Host-owned control-plane swimlane，参与者为 Decision Source / Host / Tool Runtime / State Store / Trace。
  - Example：AL-03 的 max-step guard 必须在读取第三个 Decision 前生效，展示 off-by-one 风险。
- **Boundary / Must-not-imply**：
  - 伪代码只表达 Article 08 / Lab 03 设计，不声称是 production-ready runtime。
  - 不加入 planning queue、workflow nodes、checkpoint persistence、parallel branch、human approval wait。
  - `Trace` 在这里仅承担本 Step 可关联记录；Trace / Replay / Eval 的系统设计留给 Article 21—22。
- **Transition**：接下来不再增加抽象层，而用四条 frozen trajectory 检查这套骨架能否区分关键结果。

---

## 6. Lab 03：四条轨迹各自教什么

- **Reader Question**：怎样用最小、可复现的实验反驳“停了就是成功”和“失败结果会自然被修好”？
- **Core Claim**：四条 frozen 轨迹在 deterministic fixture 中可重复地区分 success、tool failure、max-step stop、duplicate / no-progress + pseudo-completion；fixed Host completion contract 能拒绝未解决失败与缺少所需 Evidence 的伪成功。
- **Claim IDs / Evidence IDs**：`08-C04`、`08-C05`、`08-C06`、`08-C07`、`08-C08`；`E-08-09`、`E-08-10`、`E-08-11`、`E-08-12`。
- **Teaching Duty**：Lab 不是展示“happy path 会跑”，而是通过一条成功轨迹与三条反例，把 Observation / State / Stop / Outcome 的边界压实。
- **Fixed-scope wording（正文必须原样保持语义）**：
  - `以下结果只适用于 2026-08-20 冻结的 Windows 10.0.19045 / .NET SDK 10.0.301 / Host 10.0.9 / net10.0 环境、固定 fixture、ScriptedDecisionSource v1 与当前 fixed Host 实现。两次 fresh-process 运行的六个 normalized artifacts 逐文件 byte-identical；这证明当前 deterministic fixture 可复现，不证明真实模型或 Provider 的 determinism、planning quality 或 production reliability。`

### Four-trajectory teaching matrix

| Case | Observed terminal | 教学职责 | 正文选取的最小证据 | 必须保留的失败 / 限制 |
|---|---|---|---|---|
| `AL-01` success | `GOAL_SATISFIED / SUCCEEDED` | 给出唯一正例：成功必须同时通过 Goal、Output、Evidence 与 unresolved-failure contract | 三个 Step 的职责摘要、最终 state / terminal 字段；不复制 raw rows | 只是 fixed fixture 的成功，不证明未知输入或模型能力 |
| `AL-02` tool failure + requested success | `UNRESOLVED_TOOL_FAILURE / FAILED` | 证明 failed Tool Outcome 可以成为 failure Observation，但后续 stop candidate 不能把失败涂绿 | `MOCK_PARSE_FAILED` 与 `TOOL_FAILURE` Observation 的 digest correlation；terminal 摘要 | Observation normalization PASS 不等于工具成功；不声称 recovery 已实现 |
| `AL-03` max steps | `MAX_STEPS_EXHAUSTED / INCOMPLETE` | 证明外部有界停止与任务成功分离，并暴露 guard 的 off-by-one 位置 | `steps=2 / decisions=2 / tools=2`，第三个 scripted Decision 未消费 | `max_steps` 只是 Lab Step counter，不是成本预算或 SDK turn |
| `AL-04` duplicate + pseudo-complete | `STOP_CONTRACT_FAILED / FAILED` | 证明不同 invocation ID 仍可识别语义重复；history 变化不等于目标有进展；fake Evidence 不能通过 completion | action fingerprint 相同；semantic payload digest 相同；goal-state digest 不变；`EV-FAKE` 被拒绝 | 不证明通用重复检测算法；fixed fingerprint / contract 只属于当前 Host |

- **Aggregate observation duty**：正文只给一行总览：每次 run 为 `4 cases / 10 STEP / 4 TERMINAL / 10 state snapshots / 7 Tool Outcomes / 7 Observations / 7 tool calls / 10 decision calls / 1 SUCCEEDED`；随后明确这些 count 只用于 artifact integrity，不包装成性能指标。
- **Failure-ledger duty**：用一小段保留 CIM access denied、compile-name collision、fixture EOF blank line、NuGet testhost 不可用、live-reference digest mismatch；说明最终 green chain 覆盖修正后的实现，但失败事实不是 production recovery evidence。两次 Markdown 交付阶段 interruption 不是 runtime case / cancellation observation。
- **Example / Figure Duty**：
  - Table：上面的 four-trajectory matrix 是主体，不贴大段 JSONL。
  - Figure：四条 terminal 分叉图，颜色区分 `SUCCEEDED / FAILED / INCOMPLETE`。
  - Artifact links：指向 execution log 和 run-a 的六个 raw artifacts，供读者复核；正文只摘字段，不复制整行。
- **Boundary / Must-not-imply**：
  - Provider、network、credentials、MCP、权限、外部副作用与 production load 均未观察。
  - cancellation trajectory 未执行。
  - deterministic substitute 验证控制面和状态迁移，不验证真实 LLM 会选对 action、会恢复或会自行停止。
- **Transition**：四条轨迹说明“可停止、可记录”仍不足以成为可靠 Agent；还需要明确本篇不负责的更大系统边界。

---

## 7. 一个坏 Loop 通常怎么坏

- **Reader Question**：如果团队只照着 `while` 循环把模型和工具串起来，最容易出现哪些伪闭环？
- **Core Claim**：坏实现通常不是少一个循环语句，而是混淆计数单位、所有权、状态进展和成功条件；这些错误会让系统无限重复或提前“涂绿”。
- **Claim IDs / Evidence IDs**：`08-C02`、`08-C04`、`08-C05`、`08-C06`、`08-C07`、`08-C08`；`E-08-05`、`E-08-06`、`E-08-10`、`E-08-11`。
- **Teaching Duty**：把抽象模型转成 code review / design review 可使用的反模式清单。
- **Bad implementation examples**：
  1. 把“模型调用次数、工具调用数、graph tick”都叫 `step`，limit 和 trace 无法解释。
  2. 把 raw Tool Result 直接写进 authoritative State，绕过 correlation、normalization 与 reducer。
  3. 只看 trace/history 是否变化，不看 goal-state 是否进展，导致重复动作伪装成工作。
  4. 接受任意 Evidence ID 或模型自报完成，形成 pseudo-success。
  5. 在请求下一次 Decision 后才检查 limit，产生 off-by-one 与额外副作用。
  6. 用一个布尔 `done=true` 同时表达 STOPPED、SUCCEEDED、FAILED 和 INCOMPLETE。
- **Example / Figure Duty**：以 AL-02～AL-04 各映射两个坏实现，不新增新 case 或实验结果。
- **Boundary / Must-not-imply**：
  - 这些是由已确认边界和 fixed Lab 轨迹支持的 review heuristics，不声称穷举所有 Agent failure modes。
  - 不把反模式扩写为 planning quality、workflow deadlock、long-running recovery 或 context pollution。
- **Transition**：最后把 Article 08 的判断收口，并把 Planning 和更长生命周期明确交给后续篇章。

---

## 8. 工程边界：Agent Loop 应负责什么，不应吞掉什么

- **Reader Question**：最小 Loop 到哪里为止，哪些能力必须留给后续系统层？
- **Core Claim**：Article 08 只建立一次有界 Run 内的 committed Step、Observation / State transition 与 terminal semantics；它不等于 Planning、Workflow、long-running runtime、Context / Memory、Multi-Agent 或生产 Harness。
- **Claim IDs / Evidence IDs**：`08-C03`、`08-C05`、`08-C06`、`08-C07`、`08-C08`；`E-08-08`、`E-08-09`、`E-08-10`、`E-08-11`、`E-08-12`。
- **Teaching Duty**：防止最小循环吞掉整个 Agent System；让读者知道当前可做的设计评审与尚未获得的能力证明。
- **Engineering judgment**：
  - **本篇负责**：局部术语 scope、Step commit、Result -> Observation -> State、Continue / Stop、terminal reason / outcome、max-step boundedness、伪完成拒绝。
  - **本篇不负责**：计划生成与质量、确定性 workflow 编排、持久 checkpoint / resume、context packing、memory、subagent coordination、permission / sandbox、预算系统、完整 Trace / Eval、真实 Provider lifecycle。
- **Example / Figure Duty**：用“最小 Loop 位于 Model/Tool Runtime 之上、Planning/Workflow/Long-running 之下”的课程分层图；不画成产品 architecture mandate。
- **Boundary / Must-not-imply**：Explicit Non-scope 全量保留：
  - **Planning**：Article 09 的 plan decomposition、plan quality、replanning、search strategy。
  - **Workflow / State Machine**：Article 10 的 deterministic orchestration、branch、compensation。
  - **Long-running**：Article 11 的 checkpoint、retry、resume、recovery、human approval wait。
  - **Context / Memory**：Article 12+ 的 packing、compaction、working memory、session / long-term memory、RAG。
  - **Multi-Agent**：不讨论 delegation、handoff topology 或共享状态治理。
  - **DSH**：不读取或借用 DeepSeek Harness 源码来证明通用模型。
  - **BuildPilot**：不实现或预演 Part VII Runtime / Pilot。
  - **Budget**：不把 `max_steps` 扩写为 token / cost / latency budget engineering。
- **Closing takeaway**：`一个 Agent Loop 是否可靠，不看它有没有 while，而看每个 Step 是否可提交、每个 Observation 是否可追溯、每次停止是否与成功分开判定。`
- **Transition to Article 09**：本篇只回答“当前 State 下怎样安全推进一步并决定是否停止”；Article 09 才回答“怎样形成和修订跨多步的计划”。

---

## 9. Figures and tables plan

| ID | 位置 | 形式 | 教学职责 | Evidence / Claim binding | 禁止表达 |
|---|---|---|---|---|---|
| F08-01 | Section 1 | before/after gap diagram | 对比 single Tool Use 与反馈循环缺口 | C01, C04 / E-08-01, E-08-08 | Tool Result 自动等于 Observation / Evidence |
| F08-02 | Section 2 | nested scope diagram | 展示本课程 Run / Turn / Step 的局部关系 | C02, C03 / E-08-01, E-08-02, E-08-05, E-08-11 | universal hierarchy |
| F08-03 | Section 3 | state transition flow | 展示 Decision candidate 到 Host reducer commit | C04, C05 / E-08-06, E-08-10, E-08-11 | SDK 通用 API 形状 |
| F08-04 | Section 4 | two-stage stop/outcome decision | 阻断 `STOPPED -> SUCCEEDED` 的错误推导 | C06, C07 / E-08-02, E-08-10 | 所有 runtime 共用同一 terminal enum |
| F08-05 | Section 5 | swimlane | 明确 Decision Source / Host / Tool Runtime / State / Trace 责任 | C03-C07 / E-08-11 | production reference architecture |
| T08-01 | Section 2 | terminology comparison | 对照 OpenAI turn、logical turn、super-step、Lab Step | C01-C03 | 跨产品单位换算 |
| T08-02 | Section 3 | entity ownership table | 分离 Outcome、model-visible item、Observation、State、Evidence | C04, C05 | Observation 即 Evidence |
| T08-03 | Section 4 | terminal semantics table | 对照 lifecycle / reason / outcome | C06, C07 | Lab 未测路径写成 Observed |
| T08-04 | Section 6 | four-trajectory matrix | 让一条正例与三条反例各负担独立教学职责 | C07, C08 / E-08-09～12 | 复制大段 raw JSONL 或泛化到生产 |

图表实施规则：优先用 Mermaid / Markdown table；若后续 Draft 需要截图，只能从已保存 artifact 生成非证据性示意，不改变 raw evidence。所有图题必须带 scope / proposal 标签。

---

## 10. Learning Check

### Check 1｜术语 scope

- **题目**：某 SDK 的 `max_turns=8` 能否直接写成“允许 8 个 Tool Step”？为什么？
- **期望能力**：指出产品 counter unit 必须查合同；OpenAI cited Python SDK 的 max turn 是 AI invocation，不能等同 Lab Step 或工具调用数。
- **Claims**：C01, C02, C03。

### Check 2｜状态所有权

- **题目**：工具返回 `{status: ok}` 后，列出它成为下一次 Decide 输入前至少要跨过哪些层，并指出谁提交 authoritative State。
- **期望能力**：区分 Tool Outcome、correlation / normalization、Observation、Host reducer、State；说明这是本文课程设计而非 universal API。
- **Claims**：C04, C05。

### Check 3｜停止判定

- **题目**：`lifecycle=STOPPED` 至少还需要哪些信息才能判断是否完成目标？
- **期望能力**：检查 termination reason、outcome、output contract、goal / Evidence invariant 与 unresolved failure。
- **Claims**：C06, C07。

### Check 4｜轨迹判读

- **题目**：一个 case 两次执行相同语义动作但 invocation ID 不同，history digest 变化、goal-state digest 不变，随后模型请求成功，应怎样分类？
- **期望能力**：识别 duplicate / no progress 与 pseudo-completion；在 fixed Lab contract 中应 STOPPED / FAILED，而不是因 trace 变化判进展。
- **Claims**：C07, C08。

### Check 5｜边界判断

- **题目**：把 retry、checkpoint、replanning 和 token budget 全塞进本篇 Loop 伪代码会造成什么课程边界问题？
- **期望能力**：分别路由 Article 09、11、20，不用“完整性”提前讲完后续文章。
- **Claims**：C03, C06。

---

## 11. Claim-to-section coverage

| Claim | Final status inherited from Evidence | Primary section | Supporting sections | Coverage duty | Result |
|---|---|---|---|---|---|
| `08-C01` | CONFIRMED / PRODUCT-SCOPED | 1 | 2, 4 | cited OpenAI loop 与 max-turn unit；保留 current Python docs scope | COVERED |
| `08-C02` | CONFIRMED / CITED-PRODUCTS-SCOPED | 2 | 7 | 不等同 model turn、logical turn、super-step | COVERED |
| `08-C03` | PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED | 2 | 5, 8 | Article 08 / Lab 03 的 Run / Turn / Step 工作定义 | COVERED |
| `08-C04` | CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE | 3 | 1, 5, 6 | model-visible result 与 state update 分离；Lab chain conformance | COVERED |
| `08-C05` | PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED | 3 | 5, 6, 8 | Decision candidate 与 Host-owned authoritative reducer | COVERED |
| `08-C06` | CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE | 4 | 5, 6, 8 | stop 来源多元；bounded termination 不等于 success | COVERED |
| `08-C07` | CONFIRMED / FIXED-HOST-FIXTURE-SCOPED | 4 | 5, 6, 7, 8 | completion contract 拒绝 unresolved failure / pseudo-success | COVERED |
| `08-C08` | CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED | 6 | 7, 8 | four-case / two-process reproducibility 与限制 | COVERED |

Coverage result：`8 / 8 COVERED`；没有新增 Claim ID，没有把 Proposal 升格为通用事实，没有遗漏 blocked Claim（Evidence 中为 0）。

---

## 12. Job Competency mapping

| Competency | 文章中的可观察产出 | 对应章节 |
|---|---|---|
| 架构分层与边界判断 | 分开 Tool Runtime、Loop、Planning、Workflow、Long-running；明确系统不该吞掉什么 | 1, 8 |
| Runtime / control-plane 设计 | 定义 Decision candidate、Host gate、Observation normalization、reducer、terminal derivation | 3, 4, 5 |
| 状态与生命周期建模 | 分开 Run / Turn / Step、state version、lifecycle / reason / outcome | 2, 4 |
| 可靠性与 fail-closed 思维 | 拒绝 unresolved failure、fake Evidence 和 pseudo-completion；有界停止不涂绿 | 4, 6, 7 |
| 可观测性与证据意识 | 用 correlation、state snapshot、terminal record 和 raw artifacts 支撑判断 | 3, 5, 6 |
| 实验设计与结论约束 | 解释 deterministic fixture 的可复现性与外推限制，保留失败 ledger | 6 |
| 技术沟通 / Tech Lead 能力 | 用局部术语合同和 review heuristics 消除跨团队歧义 | 2, 7, 8 |

表达要求：能力通过结构、判断与验证隐式呈现；正文不出现求职自夸或职位宣言。

---

## 13. Source and link plan

### External primary sources

| Source | 用途 | 放置位置 | Scope label |
|---|---|---|---|
| OpenAI Agents SDK Python — Running agents | current Runner final / handoff / tool-result loop；run 与 turn 的措辞差异 | Sections 1, 2, 4 | current hosted docs, retrieved 2026-08-20；Python SDK |
| OpenAI Agents SDK Python — Run reference | `max_turns` 的 AI invocation unit | Sections 2, 4 | current docs；不代表其他 SDK |
| OpenAI Agents SDK Python — Agents | configurable tool-use stop behavior | Sections 1, 4 | product-specific configuration |
| PyPI `openai-agents 0.22.0` | release identity / date scope | first source note only | metadata 不单独证明 runtime behavior |
| LangGraph Graph API overview | super-step 与 reducer contract 的对照 | Section 2 | current docs；package unpinned |
| LangChain Tools | ToolMessage / Command / return_direct 的分层反例 | Sections 3, 4 | current docs；product-scoped |
| LangGraph Workflows and agents | feedback-loop example | Section 3 | example-level，不作 universal contract |

### Internal published dependency links

- Article 03：只链接 Structured Output 的 abstract model 与 engineering boundary，承接“shape valid 不等于 truth / success”。
- Article 05：只链接 Host fail-closed、Tool Result、one Tool Use is not loop 的相关小节。
- Article 06：只链接 pipeline、Result、Trace 与 engineering boundary；不重复 Tool Runtime 教程。
- Article 07：只链接 capability-chain、Call + Result 与 protocol-success limits；显式声明 protocol closure 不等于 safe execution / Agent Loop。

### Lab links

- Lab 03 README：链接 course vocabulary、state / observation、stop / cases、observations、limitations、Researcher Evidence Merge。
- execution log：支持环境、命令结果和 preserved failures。
- run-a：链接 `case-results.jsonl`、`observations.jsonl`、`states.jsonl`、`tool-outcomes.jsonl`、`trace.jsonl`、`artifact-manifest.json`。
- run-b：正文只说明 Evidence 已证明六文件逐 byte 相同，不重复列六个链接。

Link implementation note：Published Content 阶段才写 Hugo `relref`；当前 Outline 使用仓库路径 / source title，避免预先制造未验证 shortcode。外部链接直接引用 Evidence Register 已固定的官方 URL，不新增检索来源。

---

## 14. Length budget

目标正文：`5,800—7,200` 中文字（不含 frontmatter、链接注释与图表 caption）。

| Section | Budget | 压缩策略 |
|---|---:|---|
| Opening + Section 1 | 550—700 | 不复述 Article 05—07，只立缺口 |
| Section 2 | 700—850 | 用一张表替代产品术语长篇介绍 |
| Section 3 | 1,050—1,300 | 全文中心，保留对象 / 所有权 / 链路 |
| Section 4 | 850—1,050 | 终止表为主，不展开后续预算 / recovery |
| Section 5 | 650—850 | 伪代码不超过 20 行，不逐行讲 Lab source |
| Section 6 | 1,200—1,500 | 四轨迹各一段，不复制 raw JSONL |
| Section 7 | 450—600 | 反模式与 Lab 反例一一绑定 |
| Section 8 + closing | 350—500 | non-scope 与 Article 09 转场收口 |

若超长，优先删减 SDK 例子和 aggregate counts 的解释，不删抽象模型、Stopped vs Succeeded、Lab failure/limitation 或 explicit non-scope。

---

## 15. New Core Facts Audit

| Proposed outline statement | Basis | Audit result |
|---|---|---|
| cited OpenAI loop 与 max-turn unit | C01 / E-08-01, E-08-02 | existing confirmed fact |
| cited products 的 turn / step 不等同 | C02 / E-08-01, E-08-02, E-08-05 | existing confirmed fact |
| course Run / Turn / Step definitions | C03 / E-08-08, E-08-11 | existing Proposal；标签保留 |
| Outcome -> Observation -> Host State | C04, C05 / E-08-06, E-08-10, E-08-11 | product-scoped fact + course Proposal conformance |
| stop 来源多元、bounded 不等于 success | C06 / upstream + AL-03 | existing confirmed fact |
| fixed completion contract 拒绝伪完成 | C07 / AL-01, AL-02, AL-04 | fixed-host-fixture-scoped |
| four trajectories / two-process equality | C08 / E-08-09～12 | deterministic-fixture-scoped |
| bad-loop review heuristics | 由 C02、C04—C08 直接教学转写 | no new core fact；不得写成行业穷举 |

Audit result：`NO NEW CORE FACT REQUIRED`。不存在需要退回 Research 的新核心 Claim；Draft 不得扩写真实 Provider、cancellation、permission、network、production load、planning quality 或 recovery 事实。

---

## 16. Outline Gate checklist

- [x] Article type 已明确为原理篇 / LAB_ARTICLE。
- [x] 最短 thesis、Reader Change、Teaching Spine 已定义。
- [x] 每个正文 section 都有 Reader Question、Core Claim、Claim / Evidence binding、Teaching Duty、Example / Figure Duty、Boundary 与 transition。
- [x] Progressive Definition：先限定产品词义，再提出课程 Run / Turn / Step；Proposal 标签未移除。
- [x] 抽象模型显式包含 Run / Turn / Step、Decide / Act / Tool Outcome / Observation / State、Continue / Stop、Stopped vs Succeeded。
- [x] 具体机制落到 Host gate、normalization、reducer、completion validator 与 trace / state commit。
- [x] Lab 03 四轨迹各有独立教学职责，failure ledger 与 fixed-scope wording 保留；没有复制大段 raw JSONL。
- [x] Figures / Tables plan 与 Learning Check 已定义。
- [x] Claim-to-section coverage 为 `8 / 8`。
- [x] Job Competency mapping 已定义且不露骨自我推销。
- [x] Explicit Non-scope 覆盖 Planning、Workflow、Long-running、Context、Memory、Multi-Agent、DSH、BuildPilot，并额外保留 Budget 边界。
- [x] Source / link plan 与长度预算已定义。
- [x] New Core Facts Audit 结果为 `NO NEW CORE FACT REQUIRED`。
- [x] 未创建 Draft、未做 Formal Review、未批准 Final / Publish、未启动 Article 09。

Gate candidate：`PASS_RECOMMENDED`。最终 Outline Gate 结论由 Master / 后续 Reviewer 决定；Author 不自批 `FINAL`。
