# Article 09 Detailed Outline｜Planning

> Outline Gate candidate：`PASS_RECOMMENDED`。本文件是 Author 的 Detailed Outline 候选，不是正文、Formal Review、Final Gate 或发布批准。

## 0. Article decision

### Article type

- 类型：`原理 / 机制桥接篇（Standard Core Lesson / NORMAL_ARTICLE）`。
- 选择理由：本篇承接 Article 08 已建立的逐 Step Agent Loop，补上跨多个 Step 的候选步骤表示与修订机制，再把不应由 Plan 决定的确定性约束交给 Article 10；重点是抽象边界与工程判断，不适合写成 SDK API 对照、Planning 论文综述或单一事故案例。
- 结构：`问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界`。

### 最短 thesis

`Plan 只是 Goal 与 Current Evidence 条件下的剩余行动候选；它必须随 Observation 修订，并接受 Policy、Workflow、Evidence 与 Authorization 的拒绝，不能冒充执行事实或完成状态。`

### Reader Change

读前，读者容易把“模型列出了步骤”当成任务已经按顺序执行，把显式 Planner 当成 Planning 的必要条件，或在 Tool 失败后机械 Retry 原步骤。读后，读者应能：

1. 用课程工作定义解释 Plan candidate，并区分 implicit、visible 与 structured 三种可观察形态；
2. 只在 pattern scope 内比较 ReAct、Plan-and-Solve、Plan-and-Execute 与 Planner / Executor；
3. 根据新 Observation 判断剩余 Plan 应 `KEEP / REVISE / REPLACE / STOP`，并说明这是课程 Proposal；
4. 分开 Plan、Execution、Observation、Verified State、Authorization 与 Workflow；
5. 设计一个可审计但不要求保存完整 Chain-of-Thought 的 Plan artifact；
6. 从 AL-02 中区分 raw observed failure 与 Article 09 proposed planning overlay。

### Teaching Spine

1. **Problem space**：多步目标需要表达依赖、顺序与执行前未知量，但步骤列表不会自动提高事实正确性。
2. **Abstract model**：Plan 是 `Goal + Current Evidence` 条件下对剩余候选行动的表示；可隐式、可见或结构化，但始终只是 candidate intent。
3. **Concrete mechanism**：不同 Planning pattern 采用不同控制节奏；每次执行后以新 Observation / State 重新判断 `KEEP / REVISE / REPLACE / STOP`。
4. **Engineering judgment**：Plan 不拥有执行、事实提交、授权、Workflow routing 或完成裁决权；可审计 Plan 只保存必要的候选与变更依据。
5. **Verification boundary**：AL-02 只观测到 typed Tool failure、normalized Observation、State revision 与 failed terminal；Plan v1 / v2 和 `REPLACE` 是分析 Proposal，不是 runtime observation。

---

## 1. Planning 为什么出现，又为什么最容易制造“纸面完成”

- **Reader Question**：Article 08 已经能让 Agent Loop 每次安全推进一步，为什么多步任务还需要 Planning？
- **Core Claim**：当目标包含依赖、顺序与执行前未知量时，系统需要表达跨 Step 的剩余候选行动；Planning 可以降低失忆式局部决策，却不能让候选事实自动变真，也不能证明步骤已执行或目标已完成。
- **Claim IDs / Evidence IDs**：`09-C01`、`09-C09`；`09-E01`、`09-E10`；dependency `09-S08`。
- **Teaching Duty**：从真实工程问题切入，而不是先列 Planner API；把“十步任务需要方向感”和“计划文本不具备运行时权威”同时立住。
- **Example / Figure Duty**：
  - 对照一个只需一步读取文件的任务与一个需要“解析日志 -> 定位源码 -> 验证 Goal Evidence”的调查任务。
  - F09-01 用两条路径对比：`逐步失忆式选择` 与 `有剩余候选表示`；二者都必须经过执行与观测才能更新事实。
  - 反例：Plan item 写着“已验证配置”，但没有 execution record、Observation 或 completion evidence；只能判为候选描述，不能判完成。
- **Boundary / Must-not-imply**：
  - `09-C01` 始终标作课程 `PROPOSAL`，不写成行业统一 Plan 定义。
  - 不声称 Planning 自动提升模型正确率、可靠性或成功率。
  - 不提前展开 tree search、MCTS、beam search、planner benchmark 或模型质量比较。
- **Transition**：既然 Planning 的价值来自“表达剩余意图”，下一步要先定义这个对象最小是什么、又可以以哪些形态出现。

---

## 2. 抽象模型：Plan 是剩余行动候选，不是另一份 State

- **Reader Question**：Plan 最少要表达什么？没有独立 Plan object 时，还能否说系统存在 Planning？
- **Core Claim**：本文把 Plan 定义为 `Goal + Current Evidence` 条件下对剩余候选行动的表示；在 cited products 中，它既可以通过逐步 feedback loop 形成，也可以保存为显式结构，因此独立 Planner / Plan class 不是共同必要条件。
- **Claim IDs / Evidence IDs**：`09-C01`、`09-C02`；`09-E01`、`09-E02`。
- **Teaching Duty**：建立与 SDK 解耦的最小模型，再按“能否被人或机器检查”区分实现形态，不按“模型脑中有没有想过”分类。
- **Abstract Model**：

  ```text
  Goal + Current Evidence
            |
            v
     Remaining Candidate Intent
            |
            +--> implicit in loop / history
            +--> visible plan list
            +--> structured plan artifact
  ```

- **Required table duty**：

  | 形态 | 本课程工作定义 | 可审计面 | 不自动拥有的能力 |
  |---|---|---|---|
  | Implicit Plan | 无独立持久化 Plan object；下一意图从 loop / history 逐步形成 | Decision、Tool call、result sequence | 完整长期步骤、revision diff |
  | Visible Plan | 面向人或 Trace 的剩余步骤列表 | plan version、item、reason | 机器可校验 schema、执行权、授权 |
  | Structured Plan | 有 schema 的步骤、依赖、状态或 version artifact | parser、validator、diff、consumer | 执行、Verified State、Workflow invariant |

- **Example / Figure Duty**：
  - Semantic Kernel current page只用于展示 function-call / result feedback loop 形态；LangGraph.js notebook只用于展示显式 `plan` 与 `pastSteps` 分离。
  - 不做两个产品的能力、质量或生产成熟度比较。
- **Boundary / Must-not-imply**：
  - 三分法是课程 taxonomy，不是来源中的标准分类。
  - 同一个 state container 可以存放多个对象，但“同容器”不等于“同语义、同 producer、同 authority”。
  - LangGraph.js notebook 未做本轮 compatibility run；Semantic Kernel page未绑定本轮具体 package build。
- **Transition**：Plan 的载体不是唯一标准，真正值得比较的是它在执行链中何时产生、何时被重访，以及谁负责生成和执行。

---

## 3. 四种常见说法只比较控制节奏，不做算法排名

- **Reader Question**：ReAct、Plan-and-Solve、Plan-and-Execute 与 Planner / Executor 到底差在哪里？
- **Core Claim**：这些说法来自不同论文与产品语境，只能用于说明 reasoning、planning、execution 与 observation 的相对节奏或责任分离；它们不是互斥分类，也不能做 API 一一映射。
- **Claim IDs / Evidence IDs**：`09-C02`、`09-C03`；`09-E02`、`09-E03`。
- **Teaching Duty**：给读者一张足够辨向的轻量地图，避免把本篇膨胀成 Planning 历史、算法综述或框架选型报告。
- **Required comparison table duty**：

  | Pattern | 只解释的控制重点 | Evidence 允许的最小表述 | 禁止推出 |
  |---|---|---|---|
  | ReAct | reasoning、action、observation 交错 | 原论文中可借 external observation追踪或更新高层 action plan | 所有实现都保存 structured plan或必须公开 CoT |
  | Plan-and-Solve | 先分解，再按步骤求解 | 原论文是多步 reasoning task 的 prompting strategy | 等同工具型 Plan-and-Execute runtime architecture |
  | Plan-and-Execute | 先生成多步 Plan，再逐项执行并重访剩余 Plan | cited LangGraph.js example分开 `plan`、`pastSteps`，执行首项后 replan | 是唯一推荐架构，或已证明生产可靠性 |
  | Planner / Executor | 分开生成 / 修订候选与执行已选步骤的责任 | cited example中由不同 runnable / node承担 | 必须使用另一个 Agent、模型或进程 |

- **Example / Figure Duty**：
  - F09-02 只画四种 pattern 的节奏线，不画产品类图或 API 调用细节。
  - 明确指出 cited Plan-and-Execute example 的 executor 可使用 ReAct agent，说明 pattern 可以嵌套而非互斥。
- **Boundary / Must-not-imply**：
  - `09-C03` 全程保持 `PARTIAL / PATTERN-SCOPED`。
  - 不比较效果优劣、成本、延迟或 benchmark。
  - 不把 Plan-and-Solve 论文结论扩写成 Tool Runtime、Authorization 或 Workflow 证据。
- **Transition**：无论采用哪种节奏，行动一旦产生新结果，系统都要重新判断原先的剩余候选是否仍然成立。

---

## 4. Revision / Re-planning：新 Observation 改变的是剩余候选，不是过去事实

- **Reader Question**：什么时候保留计划，什么时候局部修改、整段替换或停止？这和 Retry 有什么区别？
- **Core Claim**：source-scoped evidence 支持把已执行结果或 external Observation作为更新剩余 Plan 的输入；本文进一步用 `KEEP / REVISE / REPLACE / STOP` 作为课程 disposition taxonomy，使变更原因可检查，但不保证新计划正确。
- **Claim IDs / Evidence IDs**：`09-C04`、`09-C05`；`09-E04`、`09-E05`、`09-E06`。
- **Teaching Duty**：把“模型又想了一遍”改写成可审查的变更判定：哪项前提被接受或推翻、哪些剩余项失效、为什么改变版本。
- **Concrete mechanism duty**：

  ```text
  Goal + State(n) + Plan(v1)
       -> execute one allowed step
       -> Outcome -> Observation -> State(n+1)
       -> inspect remaining assumptions
       -> KEEP | REVISE | REPLACE | STOP / ESCALATE
       -> Plan(v1 or v2 candidate)
  ```

- **Required decision table duty**：

  | Disposition | 课程条件 | 最小审计记录 |
  |---|---|---|
  | `KEEP` | 新 Observation 未否定剩余步骤前提 | plan version、accepted observation reference |
  | `REVISE` | Goal 与主路径仍成立，局部步骤、顺序、参数或前提需变更 | from/to version、change reason、evidence reference |
  | `REPLACE` | 关键前提、Goal 或允许路径失效，剩余路径需废弃 | invalidated assumption、replacement candidate、authority check |
  | `STOP / ESCALATE` | 没有安全候选路径，或需要授权 / 人类输入 | blocker、stop reason、required authority |

- **Example / Figure Duty**：先用非产品化的三行任务说明 `KEEP` 与 `REVISE` 的差异；`REPLACE` 的完整示例只使用 Section 7 的 AL-02 overlay，不另造 runtime case。
- **Boundary / Must-not-imply**：
  - `09-C05` 与整张 disposition table 始终标 `PROPOSAL`，不是来源中的标准 enum。
  - Re-planning 改变后续候选路径；Retry 是在既定 retry policy 下再次尝试相同意图，具体 retry / backoff / recovery 留给 Article 11。
  - Observation 不必每次都触发 revision；同一事实在不同 policy、budget或authorization下可以导向不同 disposition。
- **Transition**：决定“改不改 Plan”仍不等于获得“能不能执行”的权力；接下来必须把候选意图与运行时事实、授权和确定性骨架分开。

---

## 5. Plan 的权力边界：候选步骤不能批准、执行或自证完成

- **Reader Question**：Planner 把某项列入 Plan、标为 done 或生成 Tool call 后，哪些层仍有权拒绝它？
- **Core Claim**：在 cited implementation、Article 08 contract与fixed fixture中，Plan、Execution / past step、Observation、Verified State 与 Workflow routing是可分对象；2026-08-20 retrieved current official OpenAI Agents SDK docs显示，model-emitted tool call仍可被approval或tool guardrail暂停、拒绝或阻断，因此 Plan / call 不等于 Authorization，更不能证明完成。该guardrail行为限定于custom `function_tool` pipeline，不覆盖hosted / built-in tools或handoff；HITL受tool-type支持范围约束。
- **Claim IDs / Evidence IDs**：`09-C06`、`09-C07`、`09-C09`；`09-E07`、`09-E08`、`09-E10`。
- **Teaching Duty**：把“谁提议、谁执行、谁接受事实、谁允许、谁规定路径、谁裁决完成”拆成不同 authority，形成可用于 design review 的边界表。
- **Required ownership table duty**：

  | Object | 最小含义 | Evidence / authority source | Plan 能否替代 |
  |---|---|---|---|
  | Plan candidate | 接下来可能做什么 | planner role / model / code | — |
  | Execution | action 是否真实发出并由 runtime处理 | executor / Tool Runtime trace | 否 |
  | Observation | outcome 经关联与正规化后观察到什么 | Article 08 Host boundary | 否 |
  | Verified State | accepted Observation / Evidence怎样更新权威任务事实 | reducer / verifier | 否 |
  | Authorization | action是否被当前 policy / approval允许 | policy / guardrail / human approval | 否 |
  | Workflow | 允许的 stage、edge、guard与invariant | 程序 / orchestration definition | 否 |

- **Authority path duty**：

  ```text
  Plan item / model tool call
       -> capability / policy / approval / workflow gate
       -> permitted execution
       -> Outcome -> Observation -> Verified State
       -> completion evidence
  ```

- **Example / Figure Duty**：
  - 用“Plan 要删除文件”说明：候选存在不授予删除权；approval / policy可以暂停或拒绝。
  - 用2026-08-20 retrieved current official OpenAI Agents SDK docs作产品范围例子：tool guardrail只限于custom `function_tool` pipeline，不覆盖hosted / built-in tools或handoff；HITL保留tool-type支持范围。`0.22.0`只作当日PyPI / tag version anchor，并明示docs-current与tag未逐项source mapping。
  - 反例：Planner自行把 item 标为 `done`，但 reducer / verifier没有接受对应 Observation，仍不能升级为 Verified State。
- **Boundary / Must-not-imply**：
  - `Plan != Execution != Observation != Verified State != Authorization != Workflow` 必须显式写出。
  - Approval只回答授权，不自动证明action已执行或事实正确。
  - 不展开 Workflow transition / invariant / compensation；只建立边界，细节留给 Article 10。
  - Budget只作为可拒绝继续动作的边界词出现，不解释 token、step、cost或latency budget，留给 Article 20。
- **Transition**：既然 Plan 没有上述 authority，审计重点不应是保存更多“思考文本”，而应是保存候选版本与它为何改变。

---

## 6. 一个可审计 Plan artifact 应该保存什么，又不必保存什么

- **Reader Question**：为了让计划变更可回看，是否必须持久化模型的完整 Chain-of-Thought？
- **Core Claim**：本文建议持久化剩余候选步骤、plan version、change reason 与 evidence reference；这是课程 artifact design Proposal，不要求或暗示保存完整 Chain-of-Thought，也不把字段存在当成事实真实性证明。
- **Claim IDs / Evidence IDs**：`09-C01`、`09-C08`、`09-C09`；`09-E01`、`09-E09`、`09-E10`。
- **Teaching Duty**：把可审计性从“泄露全部推理过程”转成“记录可检查的候选与变更依据”，同时提醒字段本身仍需权威 producer与引用证据。
- **Proposed artifact duty**：只给最小示意，不引入新行业 schema：

  ```yaml
  plan_version: 2
  remaining_candidate_steps:
    - "先解除 parse failure，再决定是否读取匹配源码"
  change_reason: "AL-02/step-01 未产生 diagnostic locator"
  evidence_references:
    - "AL-02/step-01 observation"
  ```

  图注必须标 `COURSE PROPOSAL / ANALYSIS OVERLAY`；示意中的内容不声称来自 Lab raw Plan field。
- **Example / Figure Duty**：T09-04 对比“可审计字段”与“不能由字段推出的结论”：有version不等于plan正确；有evidence reference不等于引用已被验证；有status字段不等于item已执行。
- **Boundary / Must-not-imply**：
  - `09-C08` 始终为 `PROPOSAL`，四个字段不是跨行业标准。
  - 不讨论模型内部推理机制、隐私实现或存储策略结论。
  - ReAct研究中的verbal reasoning trajectory不等于生产系统必须公开或持久化完整CoT。
- **Transition**：这套最小审计面要在一条真实轨迹上接受检验；AL-02正好展示“运行事实”和“Planning解释”必须分轨。

---

## 7. AL-02 双轨案例：Observed failure 与 Proposed replanning overlay

- **Reader Question**：怎样证明新 Observation 已经推翻原计划前提，同时不伪造 Runtime 自动 re-planning？
- **Core Claim**：AL-02 raw artifacts确认 step 1发生typed parse failure、形成failure Observation、State仍缺失source locator与Goal Evidence，最终为failed terminal；因此可以提出 `REPLACE` 候选，但Plan v1 / v2、disposition与v2执行均未被Runtime观测。
- **Claim IDs / Evidence IDs**：`09-C04`、`09-C05`、`09-C06`、`09-C09`；`09-E05`、`09-E06`、`09-E07`、`09-E10`。
- **Teaching Duty**：用同一张双轨图训练读者区分 source/runtime fact与article analysis；这也是全文 verification boundary 的核心。
- **Required dual-track table duty**：

  | Sequence | Layer | Classification | 只允许写入正文的结论 |
  |---|---|---|---|
  | 1 | Initial Plan v1 | `PROPOSAL` | 候选顺序为 parse log -> read matched source -> verify Goal Evidence |
  | 2 | Execution | `OBSERVED` | step 1调用 `parse_mock_log` |
  | 3 | Tool Outcome | `OBSERVED` | `FAILED / MOCK_PARSE_FAILED / FI_PARSE_TYPED_FAILURE` |
  | 4 | Observation | `OBSERVED` | `TOOL_FAILURE` normalization=`PASS`，无accepted Evidence ID |
  | 5 | Verified State | `OBSERVED` | `REQ_LOG / REQ_SOURCE` unresolved，accepted Goal Evidence为空 |
  | 6 | Disposition | `PROPOSAL / REPLACE` | v1的locator前提不成立；候选v2先解除parse failure，否则stop / escalate |
  | 7 | Runtime re-plan / v2 execution | `NOT OBSERVED` | 不得声称发生 |

- **Example / Figure Duty**：
  - F09-04 用上下两条泳道：上层 `PROPOSAL Plan overlay`，下层 `OBSERVED AL-02 trace`；只允许Observation / State箭头反驳上层前提，不画“Runtime执行v2”的箭头。
  - 保留 observed terminal `UNRESOLVED_TOOL_FAILURE / FAILED`，防止后续某个successful request倒推Goal完成。
- **Boundary / Must-not-imply**：
  - Lab 03没有Planner；未观察模型生成v1/v2、automatic revision、v2 execution或成功恢复。
  - `REPLACE`是课程建议，不证明Retry必然失败；Retry / Recovery的条件和实现留给Article 11。
  - fixed fixture不证明真实模型planning quality、Provider行为或生产可靠性。
- **Transition**：双轨案例暴露了常见坏法：不是没有计划，而是让计划越权覆盖了失败事实与控制边界。

---

## 8. 一个坏 Planning 实现通常怎么坏

- **Reader Question**：有了显式 Plan、replanner 和状态字段后，系统还可能在哪些地方制造伪进展？
- **Core Claim**：坏实现通常混淆candidate、authority与fact：把计划文本当执行历史、把item status当Verified State、把每个失败都机械Retry、让Planner绕过policy，或用完整CoT替代可审计变更记录。
- **Claim IDs / Evidence IDs**：`09-C01`、`09-C03`、`09-C04`、`09-C05`、`09-C06`、`09-C07`、`09-C08`、`09-C09`；`09-E01`、`09-E03`—`09-E10`。
- **Teaching Duty**：把前述模型转化为设计审查清单，不新增failure taxonomy或生产结论。
- **Bad implementation examples**：
  1. 把“列出了步骤”写成“已完成分解与验证”，让Plan文字替代runtime record。
  2. Planner自行把item标`done`，没有correlated Outcome、accepted Observation或authoritative State update。
  3. 把ReAct、Plan-and-Solve、Plan-and-Execute写成互斥技术选型，并做API逐项对照。
  4. 任何Tool failure都Retry同一步，不检查原路径前提是否已失效，也不记录change reason。
  5. Plan中出现敏感action就直接执行，绕过capability、policy、approval或workflow guard。
  6. 为了“可解释”持久化完整CoT，却没有plan version、remaining candidates与evidence reference。
  7. 把`STOP`、`REPLACE`或“无安全路径”写成失败掩盖项，硬凑一个看似完整的新Plan。
- **Example / Figure Duty**：每个反模式只绑定前文已有表格或AL-02，不发明新SDK行为、Lab结果或算法结论。
- **Boundary / Must-not-imply**：
  - 这是由9条Claim直接转写的design-review heuristic，不宣称穷举所有Planning failure mode。
  - 不进入Article 21的完整failure taxonomy或Article 22的Eval / regression设计。
- **Transition**：最后收口本篇责任：Planning负责表达与修订候选方向，确定性顺序、不变量和长生命周期恢复应由后续层承担。

---

## 9. 工程边界：把不应每次重想的约束交给确定性骨架

- **Reader Question**：哪些东西适合留在可修订Plan里，哪些不应该每次交给模型重新决定？
- **Core Claim**：Planning负责在当前证据下表达和更新候选方向；authorization、不可违反的invariant、允许的stage / edge、completion evidence与预算边界可以拒绝或收窄Plan。本文只建立这条权力分界，不展开确定性Workflow实现。
- **Claim IDs / Evidence IDs**：`09-C01`、`09-C05`、`09-C06`、`09-C07`、`09-C08`、`09-C09`；`09-E01`、`09-E06`—`09-E10`。
- **Teaching Duty**：让读者获得一条可迁移的架构判断，并自然桥接Article 10，而不是把Workflow提前讲完。
- **Engineering judgment**：
  - **Planning负责**：在Goal与Current Evidence下表达remaining candidates；保留或修订步骤、顺序、参数与前提；记录版本与变更依据。
  - **Planning不拥有**：真实执行、Observation normalization、Verified State提交、授权、固定routing / invariant、完成裁决。
  - **交给后续层**：Article 10展开State Machine / Workflow与Agent Decision Point；Article 11展开Checkpoint / Retry / Cancellation / Resume / Recovery；Article 20展开token / step / cost / latency Budget Engineering。
- **Example / Figure Duty**：F09-05 画“可变候选层”覆盖在“确定性控制骨架”之上；箭头只能是Plan提出candidate、guards接受/拒绝/收窄，不画Plan改写invariant。
- **Boundary / Must-not-imply**：
  - 不定义Article 10的state、edge、guard、compensation或workflow engine实现。
  - 不定义Article 11的retry/backoff/checkpoint/recovery语义。
  - 不把Budget一句话扩写为Article 20的成本与延迟工程。
  - 不做Chain-of-Thought持久化方案，不做Planning算法或论文综述。
  - 不读取DeepSeek Harness源码，不实现或预演BuildPilot Runtime。
- **Closing takeaway**：`Plan 应该告诉系统“接下来可以考虑什么”，不能替系统宣称“已经发生了什么”。`
- **Transition to Article 10**：本篇回答“候选方向怎样形成、修订并被约束”；Article 10继续回答“哪些stage、edge和invariant必须由确定性State Machine / Workflow持有”。

---

## 10. Figures, tables and examples plan

| ID | 位置 | 形式 | 教学职责 | Evidence / Claim binding | 禁止表达 |
|---|---|---|---|---|---|
| F09-01 | Section 1 | two-path gap diagram | 对比失忆式逐步选择与有剩余候选表示，同时阻断“有Plan=完成” | C01, C09 / E01, E10 | Planning自动提高正确率 |
| F09-02 | Sections 2—3 | abstract model + rhythm lines | 展示Plan三种形态与四种pattern的控制节奏 | C01-C03 / E01-E03 | industry taxonomy、API mapping、优劣排名 |
| T09-01 | Section 2 | plan-shape table | 对比implicit / visible / structured的审计面 | C01, C02 / E01, E02 | structured plan天然更可靠 |
| T09-02 | Section 3 | pattern comparison | 轻量区分ReAct、Plan-and-Solve、Plan-and-Execute、Planner / Executor | C03 / E03 | 互斥分类或统一实现 |
| T09-03 | Section 4 | disposition decision table | 让Keep / Revise / Replace / Stop可审查 | C04, C05 / E04-E06 | 标准enum或replan必然成功 |
| F09-03 | Section 5 | authority pipeline | 阻断Plan直接跳到Execution、Authorization、State或Success | C06, C07, C09 / E07, E08, E10 | approval等于执行或验证 |
| T09-04 | Section 6 | artifact field / non-proof table | 说明最小审计字段与其不证明范围 | C08, C09 / E09, E10 | 保存完整CoT是必要条件 |
| F09-04 | Section 7 | observed/proposal dual-lane trace | 严格区分AL-02 raw事实与planning overlay | C04-C06, C09 / E05-E07, E10 | Runtime已自动replan或恢复 |
| F09-05 | Section 9 | layered boundary diagram | 区分可变Plan与确定性control skeleton | C06, C07 / E07, E08 | 提前讲完Article 10 |

### Example responsibilities

1. **多步调查对照**只负责说明Planning为什么出现，不证明质量提升。
2. **产品形态对照**只负责说明显式Plan不是唯一形态，不做Semantic Kernel与LangGraph.js能力比较。
3. **AL-02**是唯一bounded fixture；只负责展示Observation反证前提与proposed Replace，不创建新Lab或新runtime结论。
4. **删除文件候选**只负责说明Plan / tool call不等于Authorization，不声称所有SDK使用同一gate顺序。
5. **最小YAML artifact**只负责展示课程Proposal字段，不作为runtime output或行业schema。

图表实施规则：优先使用 Mermaid / Markdown table；所有涉及课程taxonomy或AL-02 overlay的标题 / caption必须带`PROPOSAL`，所有raw字段必须带`OBSERVED / FIXTURE-SCOPED`。当前Outline不创建`assets/`。

---

## 11. Learning Check

### Check 1｜Plan 与 State

- **题目**：模型列出“检查配置并确认已生效”，但Tool尚未执行。它属于Plan、Execution还是Verified State？
- **参考思路**：只能算candidate Plan item；没有execution record、Observation、authoritative State update与completion Evidence，不能写成已执行或已验证。
- **Claims**：C01, C06, C09。

### Check 2｜Planning形态

- **题目**：一个runtime没有独立`Planner` class，只根据上一步result形成下一次decision，能否直接判定“没有Planning”？
- **参考思路**：不能。cited products显示Planning可通过feedback loop形成，也可显式保存；是否建立可观察Plan artifact是另一项工程选择。三分法本身是课程taxonomy。
- **Claims**：C01, C02。

### Check 3｜Pattern边界

- **题目**：为什么不能把ReAct、Plan-and-Solve、Plan-and-Execute与Planner / Executor做成一张API一一映射表？
- **参考思路**：来源语境与抽象层不同；它们强调不同控制节奏或责任面，还可以嵌套。`09-C03`只允许pattern-scoped比较。
- **Claims**：C03。

### Check 4｜AL-02 disposition

- **题目**：AL-02 step 1得到typed parse failure，State仍没有diagnostic locator。为何不应直接宣称“Runtime已replan并恢复”？
- **参考思路**：Outcome / Observation / State / failed terminal是observed；Plan v1/v2与`REPLACE`只是analysis overlay，v2 execution和恢复都未观察。是否Retry还受Article 11尚未展开的policy与recovery条件约束。
- **Claims**：C04, C05, C09。

### Check 5｜Authorization

- **题目**：Plan包含“删除旧文件”，甚至模型已发出tool call，哪个层仍可拒绝？拒绝后能否写成“删除已验证完成”？
- **参考思路**：capability、policy / guardrail、human approval或workflow guard都可拒绝 / 收窄；2026-08-20 retrieved current official OpenAI docs中的custom `function_tool` guardrail与受tool-type范围约束的HITL提供产品例证。这不扩展到hosted / built-in tools或handoff，也不把当日`0.22.0` version anchor当成已完成逐项source mapping的contract。拒绝不等于执行，更不等于Verified State或完成。
- **Claims**：C06, C07, C09。

### Check 6｜可审计artifact

- **题目**：为什么plan version、change reason与evidence reference比保存完整Chain-of-Thought更符合本篇审计目标？
- **参考思路**：它们让remaining candidates与变更依据可检查；这是课程Proposal，不保证Plan正确，也不要求暴露完整私有推理。
- **Claims**：C08, C09。

---

## 12. Claim-to-section coverage

| Claim | Final status inherited from Evidence | Primary section | Supporting sections | Coverage duty | Result |
|---|---|---|---|---|---|
| `09-C01` | `PROPOSAL / COURSE TAXONOMY` | 2 | 1, 6, 8, 9 | Plan candidate最小定义与三种形态；始终保留Proposal标签 | COVERED |
| `09-C02` | `CONFIRMED / CITED-PRODUCTS-SCOPED` | 2 | 3 | cited products中feedback-loop形态与显式plan形态并存；不比较能力或质量 | COVERED |
| `09-C03` | `PARTIAL / PATTERN-SCOPED` | 3 | 8 | 四种说法只比较控制节奏 / 责任面；不做API映射、互斥分类或排名 | COVERED |
| `09-C04` | `CONFIRMED / SOURCE + FIXTURE-SCOPED` | 4 | 7, 8 | 新Observation可更新remaining Plan；AL-02只证明v1前提被反驳 | COVERED |
| `09-C05` | `PROPOSAL / COURSE TAXONOMY` | 4 | 7, 8, 9 | Keep / Revise / Replace / Stop decision table；不升级为标准enum | COVERED |
| `09-C06` | `CONFIRMED / CITED-IMPLEMENTATION + FIXTURE-SCOPED` | 5 | 7, 9 | Plan、past execution、Observation、State与Workflow routing的对象 / authority分离 | COVERED |
| `09-C07` | `CONFIRMED / OPENAI-CURRENT-OFFICIAL-DOCS-RETRIEVED-2026-08-20` | 5 | 8, 9 | current docs中tool call可被approval或custom `function_tool` guardrail阻断；保留hosted / built-in tools、handoff、HITL tool-type与docs-current / tag未逐项mapping边界 | COVERED |
| `09-C08` | `PROPOSAL / COURSE ARTIFACT DESIGN` | 6 | 8, 9 | 持久化candidate、version、reason与evidence reference，不要求完整CoT | COVERED |
| `09-C09` | `CONFIRMED / OBJECT-CONTRACT + FIXTURE-SCOPED` | 5 | 1, 6, 7, 8, 9 | Plan item不能证明执行、验证或成功；AL-02保持failed terminal | COVERED |

Coverage result：`9 / 9 COVERED`；`09-C03`保持`PARTIAL`，`09-C01 / C05 / C08`保持`PROPOSAL`。没有新增Claim ID，没有把AL-02 overlay写成runtime observation。

---

## 13. Job Competency mapping

| Competency | 文章中的可观察产出 | 对应章节 |
|---|---|---|
| 架构分层与边界判断 | 分开Plan、Execution、Observation、Verified State、Authorization与Workflow | 2, 5, 9 |
| Runtime / control-plane设计 | 把candidate、gate、execution、state commit与completion evidence串成可审查authority path | 4, 5 |
| 状态与变更建模 | 用plan version、remaining candidates、change reason与evidence reference表达revision | 4, 6 |
| 可靠性与fail-closed思维 | 允许Policy、Approval、Workflow与Evidence拒绝Plan；拒绝item自证done | 5, 8, 9 |
| 证据纪律与验证边界 | 对AL-02采用`OBSERVED / PROPOSAL / NOT OBSERVED`双轨，不把失败涂成恢复 | 7 |
| 技术选型与术语治理 | 只按pattern scope比较四种Planning说法，识别术语漂移与嵌套关系 | 3 |
| 技术沟通 / Tech Lead能力 | 用decision table、ownership table与non-scope压缩跨团队歧义 | 4, 5, 8, 9 |

表达要求：能力通过模型、边界、例子与验证纪律隐式呈现；正文不出现求职自夸或职位宣言。

---

## 14. Source and link plan

### External primary sources

| Source | 用途 | 放置位置 | Scope label |
|---|---|---|---|
| ReAct original paper，arXiv `2210.03629v3` | interleaved reasoning / action / observation；external observation可参与plan update与exception handling | Sections 1, 3, 4 | research setup；不要求structured Plan或生产公开CoT |
| Plan-and-Solve Prompting，arXiv `2305.04091v3` | “先分解、再求解”的历史pattern定义 | Section 3 | prompting method；不是Tool Runtime / Agent architecture证据 |
| LangGraph.js Plan-and-Execute official notebook，retrieved 2026-08-20 | `plan`、`pastSteps`、planner / executor / replanner与graph routing示例 | Sections 2—5 | official example / unpinned `main`；未做compatibility run或生产验收 |
| Semantic Kernel Planning，last updated 2025-06-11 | function-calling feedback loop与legacy planner removal | Sections 2—3 | product-scoped；未绑定本轮package build |
| OpenAI Agents SDK Guardrails，current official docs retrieved 2026-08-20 | custom `function_tool` input guardrail在执行前skip / replace / tripwire的产品例证 | Sections 5, 9 | 不覆盖hosted / built-in tools或handoff；docs-current未与tag逐项source mapping |
| OpenAI Agents SDK Human-in-the-loop，current official docs retrieved 2026-08-20 | pending tool approval可暂停并由人批准 / 拒绝的产品例证 | Sections 5, 9 | SDK-specific且受tool-type支持范围约束；approval不等于执行或验证 |
| PyPI `openai-agents 0.22.0` / tag `v0.22.0` | 2026-08-20当日version anchor only | Section 5 source note | registry / tag metadata不单独证明guardrail / HITL行为；docs-current与tag未逐项source mapping |

### Internal dependency and fixture links

- Article 08 published content：链接`Decision candidate -> Outcome -> Observation -> State -> terminal`边界与AL-02小节；只承接已发布课程合同，不重讲完整Agent Loop。
- Lab 03 run-a：链接`trace.jsonl`、`tool-outcomes.jsonl`、`observations.jsonl`与`states.jsonl`；Section 7只摘claim-relevant字段，不复制大段JSONL。
- Article 10：Published Content阶段只在结尾建立forward link（若目标页尚不存在则不制造会阻断Hugo的`relref`）；正文只预告确定性骨架，不引用未发布内容作Evidence。
- Article 11 / 20：只在non-scope中按课程ID路由Retry / Recovery与Budget，不制造未发布页链接或预写结论。

Link implementation note：Published Content阶段才写Hugo `relref`，shortcode使用ASCII双引号；当前Outline只保存source title / URL与repository path计划。外部链接严格复用Research / Evidence已冻结URL，不新增检索来源。

---

## 15. Length budget

目标正文：`4,600—5,800` 中文字（不含frontmatter、链接注释与图表caption）。

| Section | Budget | 压缩策略 |
|---|---:|---|
| Opening + Section 1 | 450—600 | 用一步 / 多步对照立问题，不重述Article 08 |
| Section 2 | 650—800 | 一张三形态表承担定义，不展开SDK实现 |
| Section 3 | 500—650 | pattern表控制在一屏内，不写论文史或优劣 |
| Section 4 | 700—900 | decision table为中心；Retry只保留边界句 |
| Section 5 | 800—1,000 | 全文工程判断中心，ownership table代替多段重复解释 |
| Section 6 | 450—600 | YAML只给最小字段，不展开CoT / 存储策略 |
| Section 7 | 750—950 | 双轨表 + 一段解释；不复制raw JSONL |
| Section 8 | 400—550 | 反模式逐项绑定已有Claim，不扩failure taxonomy |
| Section 9 + closing | 300—450 | 用后续篇路由收口，不提前讲Workflow |

若超长，优先删减产品pattern例子与反模式解释，不删Plan authority boundary、AL-02双轨标签、Proposal / Partial scope或explicit non-scope。

---

## 16. New Core Facts Audit

| Proposed outline statement | Basis | Audit result |
|---|---|---|
| Plan是Goal + Current Evidence下的remaining candidate | C01 / E01 | existing course Proposal；标签保留 |
| implicit / visible / structured形态与显式Plan非必要 | C01, C02 / E01, E02 | existing taxonomy + cited-products-scoped fact |
| 四种Planning说法的轻量节奏比较 | C03 / E03 | existing PARTIAL / pattern-scoped；不扩写 |
| Observation / past result可触发Plan更新 | C04 / E04, E05 | existing source + fixture-scoped fact |
| Keep / Revise / Replace / Stop taxonomy | C05 / E06 | existing course Proposal；不升级enum |
| Plan与execution / observation / state / workflow分离 | C06 / E07 | existing cited implementation + fixture fact |
| 2026-08-20 retrieved current official OpenAI docs中tool call可被guardrail / approval阻断 | C07 / E08 | existing current-docs-scoped fact；`0.22.0`仅为当日version anchor，保留`function_tool`、hosted / built-in tools、handoff、HITL tool-type与未逐项source mapping边界 |
| 最小可审计Plan artifact，不要求完整CoT | C08 / E09 | existing course Proposal；不新增存储结论 |
| Plan item不能证明execution / verification / success | C09 / E10 | existing object contract + fixture fact |
| AL-02 Plan v1 / v2与Replace | C04, C05 / E05, E06 | existing Proposal overlay；不是observed runtime |

Audit result：`NO NEW CORE FACT REQUIRED`。不存在需要退回Research的新核心Claim；Draft不得新增Planning性能、模型质量、生产可靠性、自动re-planning、统一SDK gate顺序、Workflow实现、Retry / Recovery或Budget工程结论。

---

## 17. Outline Gate checklist

- [x] Article type已明确为原理 / 机制桥接篇，Mode=`NORMAL_ARTICLE`。
- [x] Teaching Spine完整覆盖`problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary`。
- [x] 每个正文section都有Reader Question、Core Claim、Claim / Evidence binding、Example / Figure Duty、Boundary与Transition。
- [x] Claim-to-section coverage为`9 / 9`。
- [x] `09-C03`保持`PARTIAL / PATTERN-SCOPED`；`09-C01 / C05 / C08`保持`PROPOSAL`。
- [x] AL-02明确分为`OBSERVED`事实、`PROPOSAL`overlay与`NOT OBSERVED`runtime行为。
- [x] `Plan != Execution != Observation != Verified State != Authorization != Workflow`已显式建立。
- [x] ReAct、Plan-and-Solve、Plan-and-Execute、Planner / Executor只做轻量pattern比较。
- [x] Figures / Tables / Examples职责已定义；当前未创建assets。
- [x] Learning Check与参考思路、Job Competency mapping、最短结论与Source / Link plan已定义。
- [x] Explicit Non-scope覆盖Article 10、11、20、Chain-of-Thought persistence、Planning算法 / 论文综述、DSH与BuildPilot Runtime。
- [x] New Core Facts Audit=`NO NEW CORE FACT REQUIRED`。
- [x] 未修改README、Article Card、Research、Evidence、Review、global state或trace。
- [x] 未创建Draft、Published Content、Article 10 workspace；未执行Git branch / commit / push。

Gate candidate：`PASS_RECOMMENDED`。最终Outline Gate结论由Master验证；Author不修改global durable state，也不自批`FINAL`。
