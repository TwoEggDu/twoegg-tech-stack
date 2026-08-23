# Article 14 Detailed Outline｜Working Memory 与 Investigation State

## 0. Outline contract

- **Article type**：原理篇（信息 / 状态边界主线篇；不写成框架 API 对照或 Memory 产品清单）。
- **Canonical title**：`Working Memory 与 Investigation State：当前任务正在想什么`。
- **Course position**：Article 12 已回答“每个 Step 应看到什么”，Article 13 已回答“这个 view 在哪里失真”；本篇转向“即使 Context Snapshot 与 History 都还在，系统凭什么知道当前调查走到哪里、下一步还缺什么”。
- **Shortest thesis**：`上下文决定这一 Step 看见什么；Working Memory 以任务级、带版本的工作投影保存“现在按什么继续”，但只有 Host 才能批准它如何变化。`
- **Course working definition (`COURSE PROPOSAL / NOT INDUSTRY STANDARD`)**：`Working Memory 是当前未完成任务的 task-scoped、可更新、带版本 working projection；它保留继续判断下一步所需的目标、已接受事实、仍在检验的解释、未决问题、证据引用、待执行动作与完成缺口。`
- **Reader change**：读者从“把历史和日志都塞回 Context，Agent 就会记得下一步”转为能够设计一个有 epistemic type、mutation authority、revision、Evidence ref 与 persistence boundary 的 Investigation State。
- **Concrete anchor**：明确标为 `SYNTHETIC / ILLUSTRATIVE / NOT A LAB / NO BUILDPILOT RUNTIME` 的 Unity 2022.3 `CS0103` 调查，以 `rev1..rev5` 展示观察、候选解释、否决、未知与下一动作怎样受控演化。
- **Evidence gate input**：`PASS`；`12 claims = 5 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED`。正文不得把 `PARTIAL` 或 `PROPOSAL` 升格为 `CONFIRMED`。
- **Required Lab**：`NONE`。本篇不能把 schema 示例、伪代码或 revision 演进写成已执行 Fixture、Runtime Observation 或生产验证。

### Teaching Spine

```text
Problem Space
  Context Snapshot 和 History 都还在，系统却不知道哪个判断已接受、哪个假设已否决、还缺什么。
        ↓
Abstract Model
  Working Memory = task-scoped + versioned + typed working projection（COURSE PROPOSAL）；
  它从 History / Evidence 归纳当前视图，引用 authoritative Workflow State，但不取代它们。
        ↓
Concrete Mechanism
  Investigation State schema + epistemic kind / disposition 两轴
  + model suggestion -> host validate -> reducer -> commit revision -> acceptance policy。
        ↓
Engineering Application
  用 synthetic Unity / BuildPilot CS0103 rev1..rev5 展示 current projection 怎样让下一步可判定。
        ↓
Engineering Boundary
  按恢复、交接与副作用风险决定 discard / persist / checkpoint；
  不扩写 Session / Long-term / Project Memory 或 Knowledge Base / RAG。
        ↓
Verification Boundary
  managed commit != semantic truth；Evidence ref != Evidence 本体；synthetic state evolution != Runtime / Lab。
```

### Scope guardrails

- 第一屏必须先解决：**“上下文还在，但 Agent 为什么仍不知道下一步？”** 不从 LangGraph、ADK、OpenAI Agents SDK 的 API 或字段名起笔。
- `Working Memory`、`Investigation State` 精确定义、schema、taxonomy、semantic acceptance 与 discard / persist policy 都必须在第一次出现时标为 `COURSE PROPOSAL`。
- 产品资料只承担局部事实：被检查生态的术语不同；state 可受管更新和持久化；History / state / checkpoint / long-term memory 在部分产品中职责可区分。不得把课程综合写成统一行业标准。
- 贯穿全文的权限边界：`model suggestion != committed state`；`committed state != accepted fact`；`accepted fact != authoritative Workflow transition`。
- 贯穿全文的证据边界：`Evidence ref != Evidence body != truth guarantee`；`OBSERVED != root cause confirmed`；`UNKNOWN != false`；`REJECTED != permanently false across all revisions`。
- 不重讲 Article 11 的 Retry / Cancellation / Recovery，不重讲 Article 12 的 Context Assembly / Receipt，不重讲 Article 13 的 Packing chain / failure taxonomy / Lab 05。
- Article 15、16 保持 `NOT_STARTED`：不设计 Session identity / continuity、Long-term / Project Memory、retention / deletion、Memory DB、Embedding、Vector Store、Chunking、Retrieve / Filter / Rerank / Inject / Cite。

## 1. 问题空间：材料都还在，为什么仍不知道下一步

- **目标 / Reader question**：让读者先看到“有 Context、有 History”与“有 current working commitment”之间的缺口；回答为什么继续堆消息不能自动恢复下一步。
- **Claims / Evidence Cards**：`14-C03 (PARTIAL) / EC-14-03`；`14-C04 (CONFIRMED) / EC-14-04`；辅助 `14-C12 / EC-14-12`。
- **关键桥接**：从 Article 13 的“先冻结 application-visible Snapshot”推进到本篇的“即使 Snapshot 合理，还需要一份当前任务投影告诉下一 Step：什么已接受、什么仍活跃、什么已经退出、还缺什么”。
- **图表 / 代码框草案**：Figure 14-1 `Same History, Missing Current Projection`；对照“时间序列仍完整”与“rev? 的 current projection 缺失”，标出无法回答的四个问题。
- **禁写点**：不把回答错误直接归因于模型遗忘；不再展开 Missing / Stale / Pollution / Compression / Truncation；不把构造场景写成真实 BuildPilot incident。

### 1.1 开场构造场景（第一屏）

假设 Article 13 要求检查的前提都已满足：当前 Unity Console `CS0103` 片段在 Snapshot 中；前几步提出过“条件编译排除了声明”和“asmdef 引用缺失”两个候选；History 仍保留提问、工具输出、模型解释与一次反证；Snapshot / Receipt 没有已知 Assembly 或 Packing failure。

但新的 Step 仍答不出：

1. 哪个候选只是模型说过，哪个已经由 Host 接受为 active hypothesis？
2. 条件编译候选是否已被反证退出，还是仍要重复检查？
3. `CS0103` observation 能安全支持到哪一层，是否已经越写成 root cause？
4. 当前 completion gap 是什么，下一项合法 action 要满足哪些前置与权限？

**本节最短判断**：`历史保留“说过什么”，不自动保留“当前按什么继续”；Context Snapshot 是本 Step 的 view，也不自动拥有跨 Step 的 committed projection。`

### 1.2 Figure 14-1 draft

```text
History (chronological, still present)
  rev1 message -> rev2 suggestion -> tool result -> rev3 suggestion -> counter-evidence
                                   |
                                   | no typed projection / no accepted revision
                                   v
Next Step sees many statements but cannot decide:
  accepted? active? rejected? unknown? completion gap? allowed next action?

With Working Memory @ rev5:
  accepted facts + active hypotheses + rejected reasons
  + unresolved + evidence refs + pending actions + completion gaps
                                  -> next Step has a bounded starting point
```

图注必须注明：这不是说 History 无法重建 State；而是说没有 reducer / projection rule 时，完整时间序列不会自动等于当前承诺。History 可以产生 current state，也可以与 state 共存。

### 1.3 到下一节的桥

先不要急着定义 schema。必须先问这些对象分别回答什么问题、谁有 authority；否则 Working Memory 很容易吞掉 Context、Workflow State、Checkpoint、Evidence 和长期记忆。

## 2. 边界地图：七个对象分别回答什么

- **目标 / Reader question**：把 Working Memory 与 Context Snapshot、History、authoritative Workflow State、Checkpoint、Long-term Memory、Evidence 分开，同时承认实现容器可能重叠。
- **Claims / Evidence Cards**：`14-C03 (PARTIAL) / EC-14-03`；`14-C04 (CONFIRMED) / EC-14-04`；`14-C05 (CONFIRMED) / EC-14-05`；`14-C10 (PARTIAL) / EC-14-10`。
- **关键桥接**：从“缺 current projection”转到“先按职责和 authority 切对象，再定义 projection”；强调逻辑职责不要求物理分库。
- **图表 / 代码框草案**：Table 14-1 七对象边界矩阵；Figure 14-2 `History / Evidence / Workflow State -> Working Projection -> Next Context Snapshot`，Checkpoint 作为外侧保存边界。
- **禁写点**：不说 History 与 State 必须物理隔离；不说 Checkpoint 与 Working Memory 从不包含彼此；不因 task state 落盘很久就称为 Long-term Memory；不说 Evidence ref 自动使 claim 为真。

### 2.1 Table 14-1｜Boundary matrix

| Object | 它回答的问题 | 默认 authority / 生命周期 | 与 Working Memory 的合法重叠 | 它不负责什么 |
|---|---|---|---|---|
| Context Snapshot | 这一个 Step 实际被应用选择给模型看的是什么？ | application assembly；Step-scoped view | 可包含某个 Working Memory revision 的有界投影 | 不自动成为 durable state、完整 history 或 truth |
| History | 按时间发生过什么？ | message / event / transition record；可 append-only | 可用于重放、归纳或审计 current projection；state 也可含 messages | 不自动裁决哪个 statement 仍有效 |
| Working Memory | 当前未完成任务现在按什么继续？ | task-scoped、versioned projection；`COURSE PROPOSAL` | 引用 History、Evidence、Workflow State；可被 Snapshot 选入 | 不拥有 workflow gate、permission 或事实自证权 |
| authoritative Workflow State | 系统现在处于哪个已提交阶段，哪些 transition / guard 合法？ | Host / Workflow authority；committed control state | Working Memory 可引用其 revision / allowed action，Checkpoint 可共同保存二者 | 不保存全部诊断思路，也不因模型笔记自动迁移 |
| Checkpoint | 中断后靠什么恢复到可判定边界？ | durability / recovery artifact；触发式保存 | 可绑定一版 Working Memory、Workflow State、in-flight identity 与 continuation | 不是 Memory 分类，也不等于每次 state mutation |
| Long-term Memory | 哪些信息跨 session / thread 保留和被重新使用？ | 跨任务生命周期；Article 15 | task state 可以 durable，但 scope 仍只属于本任务 | 不因“存进数据库”就自动成立；本篇不设计其治理 |
| Evidence | 哪个来源、观测、实验或工件支持 / 反驳 claim？ | 独立 provenance / artifact boundary | Working Memory 保存 ref、受控摘要、claim status | ref 或 provenance 不自动等于 evidence quality、truth 或 acceptance |

### 2.2 Figure 14-2｜职责关系，不是统一存储拓扑

```text
History --------------------┐
Evidence bodies + refs -----+--> projector / reducer --> Working Memory @ revision N
Workflow State @ revision W ┘                              |
                                                           +--> selected projection in next Context Snapshot
                                                           |
Checkpoint boundary <--------------------------------------+

Long-term / Project Memory: outside Article 14 scope
```

图中必须标注 `logical role != physical database`，并说明 projection 只消费 refs，不复制每条 History 或 Evidence body。

### 2.3 到下一节的桥

边界切开后才有资格给 Working Memory 一个课程定义：它不是新建一个“大容器”，而是把当前任务所需的承诺压成一版可更新视图。

## 3. 抽象模型：task-scoped、versioned working projection

- **目标 / Reader question**：明确 Working Memory 是什么、为什么必须标 `COURSE PROPOSAL`，并给出不依赖具体框架的六条设计不变量。
- **Claims / Evidence Cards**：`14-C01 (CONFIRMED) / EC-14-01`；`14-C02 (PROPOSAL) / EC-14-02`；`14-C05 (CONFIRMED) / EC-14-05`。
- **关键桥接**：从七对象的职责差异收束出“task scope + current projection + version + typed uncertainty + reference boundary + no transition authority”。
- **图表 / 代码框草案**：Evidence-basis side box（只列 product-scoped roles，不列 API）；Figure 14-3 `Working Projection Invariants` 六栏卡片。
- **禁写点**：不宣布跨生态统一定义；不把 Magentic-One ledger 写成行业标准；不把保存时长当 scope；不把 Working Memory 写成模型隐藏思维或 chain-of-thought。

### 3.1 为什么是课程提案，而不是产品术语归一化

正文只用一个短证据框交代：当前 LangGraph 文档把 short-term memory 放在 thread-scoped graph state，并可由 checkpoint 保存；当前 Google ADK 文档把 `session.state` 作为 scratchpad，并把 events 作为 history；当前 OpenAI Agents SDK 文档区分 local application context、LLM-visible context、Session history 与可序列化 RunState；Magentic-One 的 Task / Progress Ledger 是任务期结构化台账的研究设计先例。

这些事实只支持“存在若干 task / thread / session-scoped 可更新 state 与 ledger 设计”，不支持本篇字段名、taxonomy 或 acceptance pipeline 是通用标准。所有产品描述保留 `retrieved 2026-08-22 / hosted docs / package version not locked` 边界。

### 3.2 六条课程不变量

1. **Task-scoped**：scope 由当前未完成任务决定，不由进程寿命或存储介质决定。
2. **Projection, not transcript**：只保留继续决策所需的 current view；完整经过仍由 History / Evidence 承担。
3. **Versioned**：每次受控改变产生可比较 revision；候选必须声明 `base_revision`，旧写入不能静默覆盖新状态。
4. **Epistemically typed**：Observation、Inference、Hypothesis、Unknown 不靠措辞互相升级；Rejected 有反证与 scope。
5. **Reference-preserving**：状态保存 evidence refs、locator、version 与必要摘要，不复制或替代 Evidence body。
6. **No control authority**：Working Memory 能提出下一动作、引用 guard，但不能推进 authoritative Workflow State、授权工具或自封 claim 为真。

### 3.3 正文第一次定义

> **COURSE PROPOSAL**：Working Memory 是当前未完成任务的 task-scoped、可更新、带版本 working projection。它保留继续判断下一步所需的 goal、accepted facts、active / rejected hypotheses、unresolved questions、evidence refs、pending actions 与 completion gaps；它既不是完整 History，也不拥有 Workflow transition authority。

### 3.4 到下一节的桥

定义只有在状态可序列化、认知类型不混叠时才有工程价值。下一节给出最小 Investigation State schema，但必须把字段名继续限定为课程提案。
## 4. Investigation State：最小 schema 与认知两轴

- **目标 / Reader question**：给出能支持继续调查、交接、审计和冲突检测的最小状态形状；解释为什么 `REJECTED` 不是与 `OBSERVED` 同层的 evidence kind。
- **Claims / Evidence Cards**：`14-C08 (PROPOSAL) / EC-14-08`；`14-C09 (PROPOSAL) / EC-14-09`；`14-C10 (PARTIAL) / EC-14-10`。
- **关键桥接**：从六条不变量落到数据模型；先给字段职责，再给两轴映射，避免把自然语言段落直接当 state。
- **图表 / 代码框草案**：Code Box 14-A `investigation-state-course-v1` schema；Table 14-2 五个读者标签到 `kind + disposition` 的映射；Figure 14-4 受控 epistemic transitions。
- **禁写点**：不把 schema 说成 W3C、Magentic-One 或任一框架格式；不强制数字 confidence；不把 `UNKNOWN` 写成 false；不把 `REJECTED` 写成永久跨版本真理；不让 evidence ref 替代 acceptance rule。

### 4.1 Code Box 14-A｜Minimum schema draft

```yaml
schema: investigation-state-course-v1
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
task:
  task_id: string
  goal: string
  completion_criteria: [string]
revision: integer
accepted_facts:
  - fact_id: string
    statement: string
    scope: string
    evidence_refs: [string]
    acceptance_rule: string
    accepted_by: host_policy|human
    accepted_at_revision: integer
current_hypotheses:
  - hypothesis_id: string
    kind: HYPOTHESIS
    disposition: ACTIVE
    statement: string
    evidence_refs: [string]
    counter_evidence_refs: [string]
    next_test: string
    falsifier: string
rejected:
  - hypothesis_id: string
    kind: HYPOTHESIS
    disposition: REJECTED
    rejection_reason: string
    counter_evidence_refs: [string]
    rejected_at_revision: integer
    scope: string
unresolved:
  - question_id: string
    kind: UNKNOWN
    question: string
    missing_refs_or_inputs: [string]
evidence_refs:
  - ref_id: string
    source: string
    locator: string
    source_version: string|UNKNOWN
    retrieved_at: date|UNKNOWN
pending_actions:
  - action_id: string
    purpose: string
    prerequisites: [string]
    authority_required: string
    status: PROPOSED|ELIGIBLE|BLOCKED
completion_gaps: [string]
```

字段教学职责：

- `task + completion_criteria` 固定调查目标，而不复制 Workflow gate。
- `revision` 支持 stale-write / conflict detection；具体格式由 Host 决定。
- `accepted_facts` 只保存 Host acceptance 结果；模型不能直接写 `accepted_by=model`。
- `current_hypotheses / rejected / unresolved` 分别保留可检验候选、退出理由和仍缺信息。
- `evidence_refs` 至少保存 source / locator / version / retrieved time；digest 也不是真值证明。
- `pending_actions` 记录目的、前置与 authority，不是 Tool invocation，也不授予权限。
- `completion_gaps` 说明距离目标还缺什么，不等于 Workflow guard 已通过。
- `confidence` 默认不进最小 schema；只有存在可校准定义和更新规则时才作为扩展字段。

### 4.2 Table 14-2｜两个轴，不让标签靠措辞升级

**Axis A — epistemic kind**：`OBSERVATION | INFERENCE | HYPOTHESIS | UNKNOWN`。

**Axis B — candidate disposition**：`ACTIVE | REJECTED`。`REJECTED` 是 hypothesis 的处置，不是另一种 observation；进入 `accepted_facts` 是 Host acceptance policy 的结果，不是模型提供的新 kind。

| Reader label | Storage mapping | Minimum entry condition | 明确不代表 |
|---|---|---|---|
| `OBSERVED` | `kind=OBSERVATION` | 有可定位 raw output / event / file / command result，source / locator / version 已校验 | 根因确定、跨环境成立 |
| `INFERRED` | `kind=INFERENCE` | 列出所依赖 observations、推理规则与 refs | 直接看到、因果经实验确认 |
| `HYPOTHESIS` | `kind=HYPOTHESIS, disposition=ACTIVE` | 可检验候选，带 `next_test` 或 `falsifier` | accepted fact、可越过风险 gate |
| `REJECTED` | `kind=HYPOTHESIS, disposition=REJECTED` | 预测失败、反证或 scope 冲突，保留 reason + refs + revision | 永久、全版本“绝对错误” |
| `UNKNOWN` | `kind=UNKNOWN` | 任务要求回答，但材料缺失、冲突或不足 | false、空值、未写出的 hypothesis |

### 4.3 Figure 14-4｜Epistemic transition draft

```text
raw artifact
  -> provenance validated
  -> OBSERVED
       ├─ explicit derivation + refs -> INFERRED
       └─ testable explanation       -> HYPOTHESIS / ACTIVE

HYPOTHESIS / ACTIVE
  ├─ counter-evidence or failed prediction -> HYPOTHESIS / REJECTED
  ├─ required checks + acceptance policy   -> accepted_facts
  └─ insufficient / conflicting evidence   -> remains ACTIVE and/or UNKNOWN gap
```

每条箭头都需要 Host rule、refs 与 revision；模型“换一种更肯定的说法”不构成 transition。

### 4.4 到下一节的桥

有 schema 仍不等于有安全 mutation。下一节把“谁可以提议、谁可以合并、谁可以接受为事实”拆成不同 authority。

## 5. 具体机制：suggestion、validation、reducer、revision 与 acceptance

- **目标 / Reader question**：解释模型输出如何成为受控 working-state mutation，以及为什么 runtime commit 成功仍不等于 claim 为真。
- **Claims / Evidence Cards**：`14-C06 (CONFIRMED) / EC-14-06`；`14-C07 (PROPOSAL) / EC-14-07`；辅助 `14-C10 / EC-14-10`。
- **关键桥接**：从 typed schema 落到 authority chain；先分“运行时如何提交 update”的产品事实，再提出“业务语义如何接受”的课程 pipeline。
- **图表 / 代码框草案**：Figure 14-5 sequence `model suggestion -> host validate -> reducer -> commit revision -> acceptance policy`；Code Box 14-B `MutationCandidate`；Code Box 14-C 伪代码 / conflict path。
- **禁写点**：不说 reducer 自动判断真伪；不说 ADK / LangGraph 已实现本课程 semantic validator；不让 model 直接改 accepted facts；不把 commit success 当 Evidence Gate；不展开 reducer algebra、CRDT 或 distributed transaction。

### 5.1 先分事实层与课程层

**可确认的产品事实层**：

```text
node / agent output
  -> runtime-managed update
  -> reducer or event / session service
  -> new state and, in some products, a new checkpoint
```

LangGraph 当前文档支持 node 返回 update、reducer 应用 key 更新、`update_state` 形成新 checkpoint；Google ADK 当前文档支持 Runner / Event / SessionService 应用 state delta，并警告 direct mutation 可能绕过 event history、persistence、thread safety 与 timestamp maintenance。这些资料没有承诺 reducer / service 会按本篇 Evidence policy 判断 claim 为真。

**课程 authority pipeline (`COURSE PROPOSAL`)**：

```text
model / tool / operator proposes MutationCandidate
  -> host validates schema + task identity + base_revision + allowed operation
  -> host validates refs + scope + guard + conflicts
  -> deterministic reducer applies the valid delta
  -> runtime commits revision + mutation event
  -> host acceptance policy evaluates claim disposition
```

若 acceptance policy 要把 active hypothesis 移入 `accepted_facts` 或 `rejected`，该处置本身也必须再次走受控 mutation 并产生后续 revision；禁止在 commit 后进行不可见 in-place rewrite。

### 5.2 Code Box 14-B｜Mutation candidate draft

```yaml
mutation_candidate:
  task_id: SYNTH-CS0103-INVESTIGATION
  base_revision: 1
  actor: model
  operation: add_hypothesis
  value:
    hypothesis_id: H-DEFINE
    statement: "symbol declaration may be excluded by the active define set"
    evidence_refs: [SYNTH-OBS-CONSOLE-001]
    next_test: "capture the effective define set and declaration path"
    falsifier: "the effective define set includes the declaration path"
```

样例值必须注明 synthetic；它只示范 envelope 形状，不代表真实仓库、define set 或 BuildPilot output。

### 5.3 Host validation checklist

1. **Schema**：operation、required fields、enum 和大小边界是否合法。
2. **Identity**：`task_id` 是否指向当前 investigation；actor 是否有 propose authority。
3. **Revision**：`base_revision` 是否仍是 current；否则返回 conflict，不静默 last-write-wins。
4. **Allowed fields / guard**：当前 Workflow Stage 是否允许新增 hypothesis、移除 pending action 或修改 completion gap。
5. **Evidence refs**：ref 是否可解析、scope / version 是否匹配；ref 存在仍不等于内容为真。
6. **Conflict / idempotency**：同一 hypothesis 是否重复、相互冲突或已在 rejected set；reducer 的 deterministic merge rule 是什么。

### 5.4 Reducer、commit 与 acceptance 分工

| Stage | 负责 | 不负责 |
|---|---|---|
| Host validate | 结构、身份、revision、field authority、refs、guard、冲突 | 开放式生成新解释；替 Evidence body 背书 |
| Reducer | 按确定规则合并 valid delta、处理重复 / 冲突结果 | 判定一个 claim 的语义真值 |
| Commit revision | 写入新 revision、mutation event，必要时 checkpoint | 宣告 root cause 或 Workflow gate 已通过 |
| Acceptance policy | 按 evidence threshold、scope、risk，必要时 human review，决定 active / accepted / rejected / remain unknown | 绕过相同 mutation / revision path 原地改 state |

### 5.5 Code Box 14-C｜Conflict-aware pseudo flow

```text
candidate = model_suggestion(base_revision=N)
validated = host_validate(candidate, current_revision=N, policy=P)
delta     = reducer(current_state, validated)
stateN1   = store_compare_and_commit(expected=N, delta)
decision  = acceptance_policy(stateN1.entry, evidence_store, risk_policy)

if decision changes projection:
    submit a new host-owned mutation against base_revision=N+1
else:
    keep the committed entry as ACTIVE / UNKNOWN
```

**核心反例**：受管流程可以把模型文本写进 state；这证明 managed commit 与 semantic acceptance 必须分开，而不是证明模型文本已经成为事实。

### 5.6 到下一节的桥

抽象 pipeline 最容易显得像多余手续。用一个不宣称真实运行的 `CS0103 rev1..rev5`，展示每个 revision 怎样消除重复检查并留下下一步。

## 6. Synthetic Unity / BuildPilot 案例：CS0103 rev1..rev5

- **目标 / Reader question**：让读者看到 History 与 current projection 的实际差异，并把 schema、两轴、mutation authority 和 revision 串成一条可迁移的诊断链。
- **Claims / Evidence Cards**：`14-C12 (CONFIRMED / NARROW) / EC-14-12`；`14-C08 (PROPOSAL) / EC-14-08`；`14-C09 (PROPOSAL) / EC-14-09`；`14-C07 (PROPOSAL) / EC-14-07`。
- **关键桥接**：从“pipeline 负责谁能改”落到“每次只提交一个有证据边界的 delta”；随后比较 History 全量与 `Working Memory@rev5` 的当前视图。
- **图表 / 代码框草案**：Table 14-3 revision ledger；Figure 14-6 `rev1 -> rev5` timeline；Code Box 14-D filled `Working Memory@rev5`。
- **禁写点**：必须写 `SYNTHETIC / ILLUSTRATIVE / NOT A LAB / NO RUNTIME CLAIM`；不声称真实 BuildPilot、Unity project、compiler invocation、asmdef graph、define set、rerun receipt 或 terminal build receipt 存在；不说 CS0103 证明缺 `using`、asmdef、define 或生成代码中的任何单一根因。

### 6.1 Evidence ceiling before the example

可确认到的最窄事实只有：Microsoft 当前 CS0103 文档支持“名称在当前 class / namespace / scope / context 中不存在”，且可能有多个检查方向；Unity 2022.3 文档支持 Console 展示脚本编译错误，并可通过相关 API 判断日志中是否有 compilation error。两者都不证明 BuildPilot 真实运行、具体根因、修复有效或整个 build 的 terminal outcome。

因此下表所有 artifact ID、define / asmdef 检查结果与 revision 都是教学构造；真正可引用的外部 Evidence 只用于限制 CS0103 语义。

### 6.2 Table 14-3｜Revision ledger

| Revision | Proposed / observed delta | Host-owned committed projection | 为什么下一步不同 | 明确仍未证明 |
|---:|---|---|---|---|
| `rev1` | `OBSERVED`：synthetic Console artifact 报告 `CS0103`，identifier=`BuildReceiptWriter`，带 file / line locator | accepted fact 只写“该 artifact 报告当前 context 中名称不可用”；保存 `SYNTH-OBS-CONSOLE-001` ref | 可以提出多个可检验原因，不能直接修 `using` | root cause、完整 build outcome、真实项目存在 |
| `rev2` | model 提议 `H-DEFINE`：active define 可能排除了声明；附 next test / falsifier | Host 验证 candidate，reducer 提交 `H-DEFINE / ACTIVE` | 下一步是取得 effective define set 与 declaration path，不是把 hypothesis 写成 fact | define 确实是根因 |
| `rev3` | model / operator 再提议 `H-ASMDEF`：required assembly reference 可能缺失 | Host 保留并行 hypothesis；不因第二个解释出现就覆盖第一个 | 调查可并行维护两个候选，但 action 仍受证据与权限约束 | asmdef 缺失、两个假设互斥或已排序 |
| `rev4` | synthetic counter-evidence 表示 define 检查不符合 `H-DEFINE` 的预测 | acceptance policy 提交 `H-DEFINE / REJECTED`，保留 reason、counter-evidence ref、scope 与 revision；`H-ASMDEF` 仍 active | 不再重复 define 检查；转向尚未完成的 assembly graph 检查 | `H-DEFINE` 在所有版本永久错误；`H-ASMDEF` 因此为真 |
| `rev5` | `UNKNOWN`：assembly graph 尚未取得 | `unresolved=ASSEMBLY_GRAPH_NOT_CAPTURED`；pending action=`capture asmdef graph`；completion gap 同步保留；`H-ASMDEF` 仍 active | 下一步可判定为“先获取图与编译 profile，再讨论根因 / rerun” | assembly reference 缺失、修复、rerun 结果、build terminal success |

### 6.3 Figure 14-6｜History vs Working Memory@rev5

```text
History:
  rev1 observation
  rev2 define suggestion
  rev3 asmdef suggestion
  rev4 counter-evidence + rejection
  rev5 missing assembly graph

Working Memory @ rev5:
  accepted_fact : artifact reports CS0103 for BuildReceiptWriter
  active        : H-ASMDEF + next_test
  rejected      : H-DEFINE + reason + counter-evidence + scope
  unresolved    : assembly graph not captured
  pending       : capture asmdef graph + compiler invocation/profile
  completion_gap: graph + profile + rerun receipt
```

教学点：History 保留完整演进；current projection 只保留继续判断所需的当前集合。二者职责不同，但可以从同一 event log / checkpoint 重建或共同存储。

### 6.4 Code Box 14-D｜Filled rev5 projection draft

```yaml
schema: investigation-state-course-v1
classification: SYNTHETIC_ILLUSTRATIVE_NOT_EXECUTED
task:
  task_id: SYNTH-CS0103-INVESTIGATION
  goal: "determine the narrow cause of the reported CS0103"
  completion_criteria:
    - "root-cause claim has sufficient scoped evidence"
    - "rerun receipt confirms the bounded outcome"
revision: 5
accepted_facts:
  - fact_id: F-CONSOLE-001
    statement: "the synthetic Console artifact reports CS0103 for BuildReceiptWriter"
    scope: "synthetic Unity 2022.3 illustration"
    evidence_refs: [SYNTH-OBS-CONSOLE-001]
current_hypotheses:
  - hypothesis_id: H-ASMDEF
    kind: HYPOTHESIS
    disposition: ACTIVE
    statement: "the required assembly reference may be missing"
    next_test: "capture and inspect the asmdef dependency graph"
rejected:
  - hypothesis_id: H-DEFINE
    kind: HYPOTHESIS
    disposition: REJECTED
    rejection_reason: "the synthetic define check did not match the prediction"
    counter_evidence_refs: [SYNTH-DEFINE-CHECK-001]
    rejected_at_revision: 4
    scope: "this synthetic revision only"
unresolved:
  - question_id: U-ASM-GRAPH
    kind: UNKNOWN
    question: "does the assembly graph include the required reference?"
    missing_refs_or_inputs: [ASMDEF_GRAPH, COMPILER_PROFILE]
pending_actions:
  - action_id: A-CAPTURE-GRAPH
    purpose: "capture asmdef graph and compiler invocation/profile"
    prerequisites: [READ_ONLY_ACCESS]
    authority_required: HOST_POLICY
    status: PROPOSED
completion_gaps: [ASMDEF_GRAPH, COMPILER_PROFILE, RERUN_RECEIPT]
```

正文要在代码框后立即说明：`SYNTH-*` 是教学 placeholder，不是 Evidence locator；`Required Lab: NONE`；该样例展示的是状态合同，不是诊断正确率。

### 6.5 到下一节的桥

rev5 已经足以继续，但不意味着每段聊天、每份日志、每个中间计划都要永久留在 active state。接下来用恢复成本与副作用风险决定什么丢、什么任务级持久化、何时建 checkpoint。
## 7. Discard、Persist、Checkpoint：按恢复风险选，不按“短期 / 长期”字面选

- **目标 / Reader question**：给出最小、风险导向的保存策略；说明 task-scoped state 可以 durable，而 active-state discard 不等于删除 History / Evidence。
- **Claims / Evidence Cards**：`14-C05 (CONFIRMED) / EC-14-05`；`14-C11 (PROPOSAL) / EC-14-11`；辅助 `14-C03 / EC-14-03`、`14-C10 / EC-14-10`。
- **关键桥接**：从 rev5 的 current projection 转到生命周期：只保存恢复 / handoff / 防重复副作用需要的状态，不把 Working Memory 变成第二份 archive。
- **图表 / 代码框草案**：Table 14-4 三栏 `discard from active / task-durable / checkpoint trigger`；Figure 14-7 checkpoint decision tree；Side Box `scope != storage duration`。
- **禁写点**：不说 discard 等于 delete；不设计 Article 15 的 retention / deletion / consolidation；不重讲 Article 11 的完整 Recovery、Retry、Cancellation；不要求每次 field update 都 checkpoint；不说数据库存储自动成为 Long-term Memory。

### 7.1 Table 14-4｜三种处置

| 默认退出 active Working Memory | 应 task-durable 持久化 | 触发 checkpoint 的条件 |
|---|---|---|
| 重复聊天原文；纯排版 scratch；可廉价重算中间变换；已被替代的详细计划；已外置大 Evidence body | task / goal / completion criteria；revision；accepted facts + refs；active hypotheses + next tests；unresolved；rejected reason + refs + scope；pending actions + authority；completion gaps | context reset / process restart / handoff；恢复代价高；需要审计；并发 writer 需要 revision conflict detection；下一动作有外部副作用或 retry 风险 |

两条必须紧跟表格：

1. “退出 active state”只是不再重复塞进 current projection；History / Evidence 的 retention 由各自系统负责，不能被本篇顺手删除。
2. “任务级持久化”描述 scope，不描述保存介质；一个 thread state 即使跨进程长期存于数据库，也可能仍然只是该 task 的恢复状态。

### 7.2 Figure 14-7｜Checkpoint decision tree draft

```text
Can the current projection be recomputed cheaply and safely?
  ├─ yes, no handoff / no side effect / no audit need
  |    -> keep ephemeral or persist only minimal refs
  └─ no / uncertain
       -> persist task-durable Investigation State
            |
            +-- context reset / process restart / handoff? -> checkpoint
            +-- concurrent writer / stale revision risk?    -> checkpoint
            +-- next action has external side effect?       -> checkpoint before and after receipt
            +-- recovery expensive / audit required?        -> checkpoint
```

“副作用前后”最多承接 Article 11 的边界：保存 stable action identity、revision、effect receipt 与 continuation point；不在本篇展开 retry eligibility、reconcile、compensate 或 exactly-once。

### 7.3 小型工程判断

- 短、无副作用、可重算、无需 handoff 的调查可以不建 durable checkpoint。
- 可能重复提交、发布或调用外部系统的 pending action 需要更强的 checkpoint / receipt 边界。
- 大日志留在 Evidence store；Working Memory 保存 stable locator、digest / version 与受控摘要。
- rejected hypothesis 应保留最小 reason + refs，避免下一 Step 重走已失败路径；详细讨论过程可以退出 active projection。

### 7.4 到下一节的桥

保存策略常见的失败，不是“字段少一个”，而是 authority、projection 与 archive 又被揉回一个可随意改写的字典。下一节集中给出坏实现与最小护栏。

## 8. 工程判断：坏实现通常怎样坏

- **目标 / Reader question**：把抽象模型转成 review checklist，识别 transcript-as-memory、direct mutation、truth-by-reducer、no-revision 等坏味道。
- **Claims / Evidence Cards**：`14-C06 / EC-14-06`；`14-C07 / EC-14-07`；`14-C08 / EC-14-08`；`14-C09 / EC-14-09`；`14-C10 / EC-14-10`；`14-C11 / EC-14-11`。
- **关键桥接**：从“何时保存”推进到“保存后怎样不越权、不丢 uncertainty、不制造第二事实源”。
- **图表 / 代码框草案**：Table 14-5 bad implementation / failure / minimum guard；Code Box 14-E stale revision rejection；不新增框架 API。
- **禁写点**：不写生产级分布式一致性方案；不引出 CRDT、event sourcing 教程或完整 Evidence Contract；不把所有 mutable dict 都判为错误；不把 confidence 当排序捷径。

### 8.1 Table 14-5｜Bad implementation review

| Bad implementation | 会怎样坏 | 最小护栏 |
|---|---|---|
| 把完整 transcript 当 Working Memory | 旧假设、反证和当前结论共存，下一步靠模型临时猜 | 建 typed current projection；History 保持时间序列 |
| 让模型直接覆盖 state JSON | suggestion、acceptance 与 authority 混在一起 | `MutationCandidate + Host validation + revisioned commit` |
| 把 reducer 当 truth judge | merge 成功被误写为 claim 已证实 | reducer 只合并；acceptance policy 单独评估 refs / scope / risk |
| 用单轴 enum 混写五标签 | `REJECTED` 与 `OBSERVED` 看似同类，Unknown 容易变 false | `kind` 与 `disposition` 分轴；accepted facts 由 Host-owned collection 表示 |
| rejection 后直接删除 hypothesis | 后续 Step 重复检查；无法解释为何退出 | 保存最小 reason、counter-evidence refs、scope、revision |
| 把 Evidence body 全复制进 state | projection 膨胀、版本漂移、产生第二事实源 | 保存 stable ref / locator / version / bounded summary |
| 没有 revision / compare-and-commit | stale writer 覆盖 current investigation | 每个 mutation 声明 `base_revision`；冲突显式返回 |
| 强制数值 confidence | 无校准概率制造虚假精确性 | 默认 `status + refs + counter-evidence + next_test` |
| 每个微小变化都 checkpoint / 从不 checkpoint | 写放大与噪声，或中断后重复高风险动作 | 按 handoff、恢复成本、并发与副作用触发 |

### 8.2 Code Box 14-E｜Stale mutation fail-closed

```text
current revision = 5
candidate.base_revision = 3

result = REVISION_CONFLICT
committed_state = unchanged
next = reload rev5 -> re-evaluate candidate -> submit a new mutation or discard
```

只把它写成课程最小 contract；不宣称任何框架使用相同错误码或 compare-and-swap API。

### 8.3 到下一节的桥

工程护栏定义了“应该怎样落”，但正文还要诚实说明哪些结论来自产品资料、哪些只是课程设计，以及本篇必须在哪里停。

## 9. Evidence / version boundary 与 Article 15、16 non-scope

- **目标 / Reader question**：让读者分辨 product-scoped fact、course synthesis、synthetic illustration 与未来主题；防止正文因 L 权重吞掉后续文章。
- **Claims / Evidence Cards**：`14-C01..14-C12 / EC-14-01..EC-14-12`；重点 `14-C01`、`14-C03`、`14-C05`、`14-C10`、`14-C12`。
- **关键桥接**：从 implementation checklist 收束到 evidence ceiling；先写能安全说什么，再列 non-scope，而不是 disclaimer-first。
- **图表 / 代码框草案**：Table 14-6 version / source plan；Table 14-7 dependency / forward boundary。
- **禁写点**：不创建、链接或预写 Article 15 / 16 资产；不声称 BuildPilot Runtime、Lab 或 production behavior；不把 hosted docs 写成固定包版本；不引用私有 Unity C++ 源码。

### 9.1 安全陈述层级

**可按当前来源确认（仍保留 product / version scope）**：

- 被检查的 LangGraph、Google ADK、OpenAI Agents SDK 使用不同的 state / context / session / memory 构造；不代表穷尽行业。
- History 与 current state 在若干系统中可分角色；state 也可含 messages，History 也可恢复 state。
- runtime-managed reducer / event / session-service update path 存在；direct mutation 可能绕过若干框架保证。
- task / thread-scoped state 可以 durable；duration 不能单独决定 memory category。
- CS0103 的最窄含义与 Unity 2022.3 Console 的可观察边界。

**必须标 `COURSE PROPOSAL`**：Working Memory 定义；Investigation State 精确 schema；五标签两轴；semantic authority pipeline；discard / persist / checkpoint 清单。

**不得声称**：跨生态统一 ontology / schema；reducer、checkpoint、provenance 或 ref 自动判真；`SYNTH-*` 是真实 artifact；rev1..rev5 在 BuildPilot / Unity project 运行过；已取得真实 asmdef graph、define set、compiler profile、rerun receipt 或 terminal build outcome。

### 9.2 Table 14-6｜Version / source plan

| Area | Scope in draft | Maximum use | Stop line |
|---|---|---|---|
| LangGraph | `2026-08-22` hosted Python OSS docs；package version not locked | state update / reducer / checkpoint / thread-memory examples | 不写统一 schema 或 semantic truth validator |
| Google ADK | `2026-08-22` hosted docs；页面列出的 language/version floor；安装版本未锁定 | session state / events / delta / direct-mutation warning | 不跨 SDK 推广 prefix、scope 或 acceptance semantics |
| OpenAI Agents SDK | `2026-08-22` hosted Python SDK docs；package version not locked | local vs LLM context、Session history、serializable RunState 的反例 | 不把 RunState 当通用 Working Memory schema |
| Temporal | current hosted docs，server / SDK version not locked | append-only Event History 可用于恢复 current state | 不推出 History / State 必须分库 |
| Magentic-One | 2024 original paper / arXiv `2411.04468` | task / progress ledger 是设计先例 | 不称行业标准、生产可靠性或本篇字段来源 |
| W3C PROV | 2013 Recommendations | provenance / derivation / revision / invalidation / attribution | 不判 claim true 或 Evidence Gate passed |
| C# / Unity | current Microsoft CS0103 docs + Unity 2022.3 docs | 最窄 diagnostic / Console observation | 不绑定 Unity Roslyn 实现或推出具体根因 |

### 9.3 Table 14-7｜Dependency and forward boundary

| Article / concept | 本篇只承接什么 | 明确不重复 / 不展开什么 |
|---|---|---|
| Article 11 Checkpoint / Recovery | 说明何时值得把一版 Working Memory 放进 checkpoint | 不重讲 Retry、Cancellation、Resume / Reconcile / Compensate、exactly-once |
| Article 12 Context Engineering | 下一 Step 的 Snapshot 可以选择 Working Memory projection | 不重讲 contributor taxonomy、Select / Order / Scope / Fit Budget、Receipt schema |
| Article 13 Context Debugging | 假设 Snapshot / Receipt 已完成 application-visible 检查 | 不重讲 packing chain、八标签、Reconstruction Ladder、Lab 05 |
| Article 15 Session / Long-term / Project Memory | 只留边界：task state durable 不等于 cross-session memory | 不设计 Session identity / continuity、consolidation、retention / deletion、Project Memory truth policy |
| Article 16 Knowledge Base / RAG | Working Memory 只保存 task-scoped evidence refs | 不设计 embedding、vector DB、chunking、retrieve / filter / rerank / inject / cite / retrieval eval |
| Article 18 Evidence Contract | 只需要最小 acceptance boundary：refs、scope、counter-evidence、risk | 不在本篇展开完整 Evidence quality、claim gate 与审计数据模型 |

### 9.4 到下一节的桥

边界明确后，用 Learning Check 验证读者是否真的能回答“为什么上下文还在仍不知道下一步”，而不是只记住一组字段名。

## 10. Learning Check、competency mapping 与最小结论

- **目标 / Reader question**：验证读者能否从工程场景区分 view、history、projection、authority、persistence 与 evidence ceiling，并把主张压回一句话。
- **Claims / Evidence Cards**：综合 `14-C01..14-C12 / EC-14-01..EC-14-12`，不引入新核心事实。
- **关键桥接**：从证据边界回到开场四个问题；每个答案都落到 schema / revision / authority / refs，而不是“给模型更多上下文”。
- **图表 / 代码框草案**：Table 14-8 Job Competency mapping；5 道 Learning Check + reference criteria；末尾一行最短结论。
- **禁写点**：不在结尾新增产品、来源、案例或未来实现；不写成求职自述；不把 design competency 等同已实现生产系统。

### 10.1 Learning outcomes

读完应能：

1. 解释 Context Snapshot、History 与 Working Memory 分别回答“本步看见什么 / 发生过什么 / 现在按什么继续”。
2. 设计 task-scoped、versioned Investigation State，并让 accepted fact、active / rejected hypothesis、unknown、pending action 与 completion gap 不混叠。
3. 把 `REJECTED` 建模为 hypothesis disposition，而不是 evidence kind；知道 `UNKNOWN != false`。
4. 设计 `model suggestion -> host validate -> reducer -> commit revision -> acceptance policy`，并解释 commit success 为什么不是 truth guarantee。
5. 用 base revision、Evidence refs、counter-evidence、next test 与 authority 解释 `rev1..rev5` 的下一步。
6. 按恢复、handoff、并发与副作用风险决定 discard / persist / checkpoint，同时不吞掉 Article 15 / 16。

### 10.2 Learning Check

1. Snapshot 含当前 `CS0103`、History 也含两次 hypothesis，为什么下一 Step 仍可能不知道先做什么？最少还缺哪些 committed fields？
2. Console artifact 报 `CS0103` 后，模型写“一定缺 asmdef reference”。这条记录应先是什么 kind / disposition？要进入 `accepted_facts` 还需经过什么 authority path？
3. reducer 成功提交 `rev3`，能否写“root cause confirmed”？为什么？
4. task state 在数据库里保存 90 天，它就自动成为 Long-term / Project Memory 吗？判断应看 duration 还是 scope？
5. 下一动作可能发布制品，哪些 Working Memory / Checkpoint 字段必须在动作前后保留，哪些大对象只需 ref？

### 10.3 Reference criteria（给 Draft / Reviewer，不写成唯一实现答案）

1. 缺 current typed projection：active / rejected / unresolved、refs、pending action、completion gap 与 current revision；不是先加更多聊天。
2. 先是 `HYPOTHESIS / ACTIVE`；Host 校验、受控提交、Evidence / risk acceptance 后才可能进入 accepted facts，且处置变化仍需新 revision。
3. 不能；commit 只证明 update 被运行时接受，reducer 不裁决 semantic truth。
4. 看 task / thread / session scope；durability 与 Long-term Memory category 分开。
5. 保存 task / action identity、revision、accepted / unresolved、Evidence refs、authority、effect receipt、continuation / completion gap；raw log / artifact body 留在 Evidence store。

### 10.4 Table 14-8｜Job Competency mapping

| Competency | 本篇可观察产物 | 不应夸大的地方 |
|---|---|---|
| State modeling | schema、revision、typed claim / hypothesis / unknown | 设计稿不等于 production implementation |
| Authority design | candidate / validator / reducer / store / acceptance responsibilities | 不宣称框架自动提供 semantic policy |
| Diagnostic reasoning | CS0103 从 observation 到 hypothesis / rejection / unknown 的窄演进 | synthetic case 不证明诊断准确率 |
| Reliability / recovery | discard / persist / checkpoint trigger 与 stale revision fail-closed | 不重讲或证明 distributed recovery |
| Evidence discipline | ref、scope、counter-evidence、does-not-prove 与 version boundary | provenance 不等于 truth |

### 10.5 最短结论

`上下文决定这一 Step 看见什么；Working Memory 以任务级、带版本的工作投影保存“现在按什么继续”，但只有 Host 才能批准它如何变化。`

## 11. Claim-to-section coverage（12 / 12）

| Claim | Status | Evidence Card | Primary sections | Exact wording guard |
|---|---|---|---|---|
| `14-C01` | `CONFIRMED` | `EC-14-01` | 3、9 | 只说“被检查来源的术语 / 构造不同”，不穷尽行业 |
| `14-C02` | `PROPOSAL` | `EC-14-02` | 3、10 | 第一次定义和 schema 旁都标 `COURSE PROPOSAL` |
| `14-C03` | `PARTIAL` | `EC-14-03` | 1、2、7、9 | 角色边界不等于物理隔离 |
| `14-C04` | `CONFIRMED` | `EC-14-04` | 1、2、6 | History 可恢复 / 产生 State，state 也可含 messages |
| `14-C05` | `CONFIRMED` | `EC-14-05` | 2、3、7、9 | scope 与 storage duration 分开 |
| `14-C06` | `CONFIRMED` | `EC-14-06` | 5、8、9 | managed update path 不等于 semantic validation |
| `14-C07` | `PROPOSAL` | `EC-14-07` | 5、6、8 | model 只 propose；精确 validator / acceptance 为课程设计 |
| `14-C08` | `PROPOSAL` | `EC-14-08` | 4、6、8 | 精确字段 / 必填性非行业标准；confidence 可选 |
| `14-C09` | `PROPOSAL` | `EC-14-09` | 4、6、8 | `REJECTED` 是 disposition；taxonomy 非 W3C / 产品标准 |
| `14-C10` | `PARTIAL` | `EC-14-10` | 2、4、5、7、9 | provenance / ref 不自动证明 claim 为真 |
| `14-C11` | `PROPOSAL` | `EC-14-11` | 7、8、9 | discard / persist 清单是风险导向课程 policy |
| `14-C12` | `CONFIRMED / NARROW` | `EC-14-12` | 1、6、9 | synthetic only；CS0103 / Console 不证明具体根因、修复或完整 build |

## 12. Outline gate self-check

- [x] 第一屏先回答“上下文还在但 Agent 为何仍不知道下一步”，没有从 API / 框架名开篇。
- [x] Teaching Spine 遵循 `problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary -> minimal conclusion`。
- [x] Working Memory 第一次定义即标 `COURSE PROPOSAL`，并明确 `task-scoped + versioned + working projection`。
- [x] Context Snapshot / History / authoritative Workflow State / Checkpoint / Long-term Memory / Evidence 与 Working Memory 已逐项拆开。
- [x] 最小 Investigation State schema 包含 goal / completion criteria、revision、accepted facts、active hypotheses、rejected、unresolved、evidence refs、pending actions 与 completion gaps。
- [x] `OBSERVED / INFERRED / HYPOTHESIS / REJECTED / UNKNOWN` 已按 `kind + disposition` 两轴解释；`UNKNOWN != false`，`REJECTED` 非永久真理。
- [x] `model suggestion -> host validate -> reducer -> commit revision -> acceptance policy` 已拆 authority；commit success 不等于 semantic truth。
- [x] synthetic Unity / BuildPilot `CS0103 rev1..rev5` 只展示 state evolution，并显式声明 `NOT A LAB / NO RUNTIME CLAIM`。
- [x] discard / task-durable persist / checkpoint trigger 已说明，且不把 durable task state 升格为 Long-term Memory。
- [x] Article 11—13 重复内容已设 stop line；Article 15 / 16 non-scope 已显式冻结。
- [x] 每一正文 section 都标注 Claim IDs / Evidence Cards、目标、关键桥接、图表 / 代码框草案与禁写点。
- [x] Claim coverage=`12 / 12`；未引入需要返回 Research 的新核心事实。
- [x] Outline 只规划正文；未创建 Draft、Published Content、global state、future Article 或 asset。

**Outline Gate candidate**：`PASS -> AUTHOR_DRAFT`。该候选只供 Master 验证，不自行推进 authoritative Workflow State。