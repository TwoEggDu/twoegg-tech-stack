# Working Memory 与 Investigation State：当前任务正在想什么

假设上一篇要求的检查都已经通过：当前 Unity Console 的 **CS0103** 片段确实进入了 application-visible Context Snapshot；Goal 与 State revision 相容；History 里也保留着前几步提出的两个候选解释，以及一条反证。没有已知的 Assembly、Packing、Compression 或 Truncation 故障。

新的 Step 仍可能答不出四个问题：

1. 哪个候选只是模型说过，哪个已经由 Host 接受为仍需检验的 hypothesis？
2. “条件编译排除了声明”是否已被反证，还是下一步还要重复检查？
3. CS0103 这条 observation 最多能支持到哪里，是否已被越写成 root cause？
4. 当前还缺什么，下一项 action 又需要哪些前置条件与权限？

材料没有丢，Context 也未必装错。真正缺少的是一份**当前工作承诺**：哪些内容已经接受，哪些解释仍然活跃，哪些已经退出，哪些问题仍是未知，下一步为什么仍然合法。

本篇把这份当前投影称为 Working Memory，并用 Investigation State 作为它在调查任务中的具体形状。先声明证据级别：这是 **COURSE PROPOSAL / NOT INDUSTRY STANDARD**，不是对某个产品术语的改名，也不是跨生态统一定义。

这里的 Working Memory 是由 Host 管理、可建模、可版本化、可审计的**外部应用状态**，不是也不要求复制或暴露模型私有的 hidden reasoning / chain-of-thought。模型只提交可审查的 claim、Evidence ref、next test 与 mutation candidate。

如果这篇只记一句话，我建议记住：

> 上下文决定这一 Step 看见什么；Working Memory 以任务级、带版本的工作投影保存“现在按什么继续”，但只有 Host 才能批准它如何变化。

> 证据范围：产品行为依据 2026-08-22 检索的 LangGraph、Google ADK、OpenAI Agents SDK 与 Temporal 当前 hosted docs，相关 package / Server 版本并未全部锁定；结构化 task ledger 的设计先例来自 2024 年 Magentic-One 原始论文，provenance 边界参考 W3C PROV。本文的 Working Memory 定义、Investigation State schema、认知 taxonomy、语义接受管线与保存策略都是课程提案。后文 CS0103 案例是 **SYNTHETIC / ILLUSTRATIVE / NOT A LAB / NO RUNTIME CLAIM**。

## 材料都还在，为什么仍然不知道下一步

[Article 12]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})回答“一个 Step 应该看到什么”，[Article 13]({{< relref "ai-empowerment/agent-engineering-13-context-debugging.md" >}})回答“这个 view 在哪里失真”。两篇都把视线放在一次模型调用的 application-visible Snapshot 上。

但 Snapshot 是当前 Step 的 view，不自动拥有跨 Step 的 committed projection；History 记录按时间发生过什么，也不会在没有 projector、reducer 或 acceptance rule 时自动告诉系统“现在该相信哪一条”。

~~~text
History（时间序列仍完整）
  observation -> suggestion A -> suggestion B -> counter-evidence
                                      |
                                      | 没有 typed projection / accepted revision
                                      v
下一 Step 面对一组都“说过”的句子：
  哪个 accepted？哪个 active？哪个 rejected？哪个仍 unknown？
  completion gap 是什么？哪个 action 仍被允许？

Working Memory @ revision N
  accepted facts + active hypotheses + rejected reasons
  + unresolved + evidence refs + pending actions + completion gaps
                                      -> 下一 Step 获得有边界的起点
~~~

这张图不是说 History 无法产生 State。Temporal 的 Event History 可以服务于应用状态恢复；LangGraph 的 state 也可以包含 messages。逻辑边界不要求物理分库：History 可以重放或归纳出 current state，也可以与它共同存储。关键是，**时间序列本身不等于当前承诺**。

因此，继续把更多聊天、日志和旧计划塞回 Context，最多让材料再次可见；它不能自动补上 claim status、mutation authority、revision conflict 或 completion gap。要补的是状态合同，不是另一段更长的 Prompt。

## 七个对象分别回答什么

Working Memory 很容易变成一个“大字典”：Context、History、Checkpoint、Evidence 和长期记忆都往里塞。更稳的做法是先按职责与 authority 切边界，再承认具体实现可以共用容器。

| Object | 它回答的问题 | 默认 authority / 生命周期 | 与 Working Memory 的合法重叠 | 它不负责什么 |
|---|---|---|---|---|
| Context Snapshot | 这一个 Step 被应用选择给模型看的是什么？ | application assembly；Step-scoped view | 可包含某个 Working Memory revision 的有界投影 | 不自动成为 durable state、完整 History 或 truth |
| History | 按时间发生过什么？ | message / event / transition record；可 append-only | 可用于重放、归纳或审计 current projection；state 也可含 messages | 不自动裁决哪个 statement 仍有效 |
| Working Memory | 当前未完成任务现在按什么继续？ | task-scoped、versioned projection；**COURSE PROPOSAL** | 引用 History、Evidence、Workflow State；可被 Snapshot 选入 | 不拥有 workflow gate、permission 或事实自证权 |
| authoritative Workflow State | 系统处于哪个已提交阶段，哪些 transition / guard 合法？ | Host / Workflow authority；committed control state | Working Memory 可引用它的 revision 或 allowed action；Checkpoint 可共同保存二者 | 不保存全部调查思路，也不因模型笔记自动迁移 |
| Checkpoint | 中断后靠什么恢复到可判定边界？ | durability / recovery artifact；按风险触发 | 可绑定一版 Working Memory、Workflow State、in-flight identity 与 continuation | 不是 Memory 分类，也不等于每次 mutation |
| Long-term Memory | 哪些信息跨 session / thread 保留和复用？ | 跨任务生命周期；留给 Article 15 | task state 可以 durable，但 scope 仍只属于当前任务 | 不因“存进数据库”就自动成立；本篇不设计其治理 |
| Evidence | 哪个来源、观测、实验或工件支持 / 反驳 claim？ | 独立 provenance / artifact boundary | Working Memory 保存 ref、受控摘要和 claim status | ref 或 provenance 不自动等于 evidence quality、truth 或 acceptance |

这张表是 **PARTIAL / COURSE-SYNTHESIS**。公开产品资料能直接支持其中若干局部差异，但不能证明这套七分法是行业标准，也不能推出七个对象必须各自使用一套数据库。

它们之间更接近下面的职责关系：

~~~text
History --------------------┐
Evidence bodies + refs -----+--> projector / reducer --> Working Memory @ revision N
Workflow State @ revision W ┘                              |
                                                           +--> next Context Snapshot
                                                           |
Checkpoint boundary <--------------------------------------+

Long-term / Project Memory：Article 14 范围之外
~~~

这里的 projector 只应消费引用和受控摘要，不应把每条 History 与整份 Evidence body 复制成第二事实源。**logical role != physical database**：实现可以重叠，责任不能因此混掉。

## 抽象模型：一份 task-scoped、versioned working projection

先看为什么不能直接选一家产品的对象当统一答案。

截至 2026-08-22，[LangGraph 当前文档](https://docs.langchain.com/oss/python/langgraph/add-memory)把 short-term memory 放在 thread-scoped graph state，并可由 checkpoint 保存；[Google ADK 当前文档](https://adk.dev/sessions/state/)把 session.state 描述为 session 的 scratchpad，同时把 events 作为历史；[OpenAI Agents SDK 当前文档](https://openai.github.io/openai-agents-python/context/)区分 local application context 与 LLM-visible conversation context，另外还有 Session history 和可序列化 RunState；[Magentic-One 原始论文](https://arxiv.org/abs/2411.04468)则用 Task Ledger 与 Progress Ledger 保存任务期的 verified facts、待查 facts、educated guesses、计划与进度。

这些名称有交集，但边界不相同。它们支持“任务、thread 或 session 范围内可以存在可更新 state / ledger”这个 product-scoped 判断，不支持把本篇字段、taxonomy 或 acceptance pipeline 写成通用规范。

因此，本课程采用下面的操作定义：

> **COURSE PROPOSAL**：Working Memory 是当前未完成任务的 task-scoped、可更新、带版本 working projection。它保留继续判断下一步所需的 goal、accepted facts、active / rejected hypotheses、unresolved questions、evidence refs、pending actions 与 completion gaps；它既不是完整 History，也不拥有 Workflow transition authority。

这个定义包含六条设计不变量。

1. **Task-scoped**：scope 由当前未完成任务决定，不由进程寿命或存储介质决定。
2. **Projection, not transcript**：只保留继续判断所需的 current view；完整经过仍由 History 与 Evidence 承担。
3. **Versioned**：每次受控改变产生可比较 revision；候选必须声明 base_revision，旧写入不能静默覆盖新状态。
4. **Epistemically typed**：Observation、Inference、Hypothesis、Unknown 不靠措辞互相升级；Rejected 必须保留反证与适用 scope。
5. **Reference-preserving**：状态保存 Evidence ref、locator、version 与必要摘要，不复制或替代 Evidence body。
6. **No control authority**：Working Memory 可以提出下一动作、引用 guard，但不能推进 authoritative Workflow State、授权工具或自封 claim 为真。

这里的“短期”也不等于“只存在 RAM 里”。LangGraph checkpointer 与 ADK 的持久化 SessionService 都说明 task / thread-scoped state 可以跨进程保存；相应产品又把 cross-thread / cross-session memory 另列。保存了多久与属于哪个 scope，是两个问题。一个只服务于当前调查的 state，即使在数据库里存了很久，也不会仅凭 duration 自动变成 Long-term Memory。

## Investigation State：最小 schema 与认知两轴

定义要落到工程里，必须先回答：哪些字段让下一步可判定？哪些字段只是把自然语言换个地方存？下面的 schema 是 **COURSE PROPOSAL / NOT INDUSTRY STANDARD**。Magentic-One、W3C PROV 与产品 state / checkpoint 机制只提供设计支点，不提供这份精确格式。

~~~yaml
schema: investigation-state-course-v2
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
task:
  task_id: string
  goal: string
  completion_criteria: [string]
revision: integer
entries:
  - entry_id: string
    kind: OBSERVATION|INFERENCE|HYPOTHESIS|UNKNOWN
    disposition: PROPOSED|ACTIVE|ACCEPTED|REJECTED|UNRESOLVED
    statement: string
    scope: string
    evidence_refs: [string]
    counter_evidence_refs: [string]
    derivation_rule: string|NOT_APPLICABLE
    next_test: string|NOT_APPLICABLE
    falsifier: string|NOT_APPLICABLE
    missing_refs_or_inputs: [string]
    acceptance_rule: string|NOT_APPLICABLE
    accepted_by: host_policy|human|NOT_APPLICABLE
    accepted_at_revision: integer|NOT_APPLICABLE
    rejection_reason: string|NOT_APPLICABLE
    rejected_at_revision: integer|NOT_APPLICABLE
evidence_refs:
  - ref_id: string
    source: string
    locator: string
    source_version: string|UNKNOWN
    retrieved_at: date|UNKNOWN
    classification: EVIDENCE|SYNTHETIC_SCENARIO_RECORD_NOT_RUNTIME_EVIDENCE
pending_actions:
  - action_id: string
    purpose: string
    prerequisites: [string]
    authority_required: string
    status: PROPOSED|ELIGIBLE|BLOCKED
completion_gaps: [string]
~~~

字段多不等于边界清楚。这里用统一 `entries` 保存 epistemic kind 与 disposition，避免某种类型只能存在于自然语言、进入 accepted view 后又丢失来源类型。每一组字段承担的是不同责任：

- **task + completion_criteria** 固定调查目标，但不复制或替代 Workflow gate。
- **revision** 支持 stale-write / conflict detection；具体版本格式由 Host 决定。
- **entries** 是 typed source of truth；`accepted_facts` 只是 `disposition=ACCEPTED` 的 Host-owned projection。进入该 projection 后仍保留原 `kind`、`acceptance_rule`、`accepted_by` 与 `accepted_at_revision`，模型不能把自己写成 accepter。
- `INFERENCE` 必须填写 `derivation_rule`；`HYPOTHESIS / ACTIVE` 必须填写 `next_test` 与 `falsifier`；`HYPOTHESIS / REJECTED` 必须保留 counter-evidence、reason 与 revision；`UNKNOWN / UNRESOLVED` 必须列出缺失输入。其余不适用字段显式写 `NOT_APPLICABLE` 或空列表，便于 validator 做条件校验。
- **evidence_refs** 至少保存 source、locator、version 与 retrieved time；digest 或 provenance 仍不是真值证明。
- **pending_actions** 记录目的、前置与 authority。它不是 Tool invocation，也不授予执行权限。
- **completion_gaps** 说明距离调查目标还缺什么，不等于 Workflow guard 已经通过。

confidence 没有进入最小 schema。本篇来源没有提供跨任务可比、已经校准的数值语义。没有校准方案时，**status + evidence refs + counter-evidence + next test** 比一个看似精确的 0.83 更可审查。

### REJECTED 为什么不应和 OBSERVED 放在同一个轴

五个常用读者标签的精确定义同样是 **COURSE PROPOSAL**。更稳的存储方式是拆成两个轴：

- **Axis A — epistemic kind**：OBSERVATION / INFERENCE / HYPOTHESIS / UNKNOWN
- **Axis B — disposition**：PROPOSED / ACTIVE / ACCEPTED / REJECTED / UNRESOLVED

REJECTED 描述某个 hypothesis 在特定 scope / revision 下的处置，不是一种 observation；`accepted_facts` 是 Host acceptance policy 从 typed entries 得到的 view，也不是模型新造的一种 kind。acceptance 只改变 disposition，不删除或改写原 kind。

| Reader label | Storage mapping | Minimum entry condition | 明确不代表 |
|---|---|---|---|
| OBSERVED | kind=OBSERVATION, disposition=PROPOSED 或 ACCEPTED | 有可定位 raw output / event / file / command result，source / locator / version 已校验；ACCEPTED 还需 Host acceptance metadata | 根因确定、跨环境成立；仅写 OBSERVED 也不表示已接受 |
| INFERRED | kind=INFERENCE, disposition=PROPOSED 或 ACCEPTED | 列出依赖的 observations、推理规则与 refs；ACCEPTED 还需 Host acceptance metadata | 直接看到、因果经实验确认 |
| HYPOTHESIS | kind=HYPOTHESIS, disposition=ACTIVE | 可检验候选，带 next_test 或 falsifier | accepted fact、可越过风险 gate |
| REJECTED | kind=HYPOTHESIS, disposition=REJECTED | 预测失败、反证或 scope 冲突，保留 reason、refs 与 revision | 永久、全版本“绝对错误” |
| UNKNOWN | kind=UNKNOWN, disposition=UNRESOLVED | 任务要求回答，但材料缺失、冲突或不足 | false、空值或某个未经写出的 hypothesis |

~~~text
raw artifact
  -> OBSERVATION / PROPOSED committed with a resolvable ref
  -> acceptance policy decision
  -> host-owned mutation -> OBSERVATION / ACCEPTED (kind retained)
       ├─ explicit derivation + refs -> INFERENCE / PROPOSED
       └─ testable explanation       -> HYPOTHESIS / ACTIVE

HYPOTHESIS / ACTIVE
  ├─ counter-evidence / failed prediction -> HYPOTHESIS / REJECTED
  ├─ required checks + acceptance policy  -> HYPOTHESIS / ACCEPTED
  └─ insufficient / conflicting evidence  -> remains ACTIVE + UNKNOWN gap
~~~

每条箭头都需要 Host rule、Evidence refs 与新 revision。尤其是 acceptance / rejection：policy 先做决定，随后仍要由 host-owned mutation 写入下一 revision；模型只是换了一种更肯定的说法，不构成状态迁移。

还要再切一层：Evidence ref、Evidence body 与 truth guarantee 不是同一个东西。[W3C PROV](https://www.w3.org/TR/prov-overview/)可以描述 entity、activity、agent、derivation、revision、invalidation 与 attribution，却不会替应用判断被引用内容是否可信，也不会宣布某个 claim 已通过课程 Evidence Gate。Working Memory 保存的是可回查边界，不是事实证明本身。

## 具体机制：从 suggestion 到 accepted mutation

产品事实与课程设计必须分两层说。

当前 LangGraph 文档支持 node 返回 state update、按 key 使用 reducer，并让 update_state 形成新 checkpoint；Google ADK 当前文档支持 Runner 把更新封装为 Event、由 SessionService 在 append event 时应用 state delta，并明确警告直接修改取回的 session.state 可能绕过 event history、持久化、线程安全和时间戳维护。

这些资料支持一个受管更新路径：

~~~text
node / agent output
  -> runtime-managed update
  -> reducer or event / session service
  -> new state and, in some products, a new checkpoint
~~~

它们没有承诺 reducer 或 SessionService 会按本篇的 Evidence policy 判断 claim 为真。ADK 的 output_key 甚至可以把模型最终文本经受管 Runtime 写进 state；这个反例说明：**managed commit 与 semantic acceptance 必须分开**。

本课程因此提出下面的 authority pipeline：

~~~text
COURSE PROPOSAL

model / tool / operator proposes MutationCandidate
  -> host validates schema + task identity + base_revision + allowed operation
  -> host validates refs + scope + guard + conflicts
  -> deterministic reducer applies the valid delta
  -> runtime commits revision + mutation event
  -> host acceptance policy evaluates claim disposition
~~~

一个 synthetic mutation candidate 可以长这样：

~~~yaml
mutation_candidate:
  task_id: SYNTH-CS0103-INVESTIGATION
  base_revision: 2
  actor: model
  operation: add_hypothesis
  value:
    hypothesis_id: H-DEFINE
    statement: "the declaration may be excluded because its conditional-compilation expression evaluates false under the effective symbol set"
    evidence_refs: [SYNTH-OBS-CONSOLE-001]
    next_test: "1) locate the declaration source and its conditional-compilation expression; 2) capture the effective symbols and evaluate that expression"
    falsifier: "the declaration source participates in the target compilation and is either unguarded or its conditional-compilation expression evaluates true under the effective symbols"
~~~

SYNTH-* 只是教学 placeholder。Host 收到 candidate 后，至少要检查六件事：

1. **Schema**：operation、required fields、enum 与大小边界是否合法。
2. **Identity**：task_id 是否指向当前 investigation；actor 是否有 propose authority。
3. **Revision**：base_revision 是否仍是 current；否则显式返回 conflict。
4. **Allowed fields / guard**：当前 Workflow Stage 是否允许新增 hypothesis 或改变 completion gap。
5. **Evidence refs**：ref 是否可解析，scope / version 是否匹配；ref 存在仍不等于内容为真。
6. **Conflict / idempotency**：同一 hypothesis 是否重复、冲突或已有 `REJECTED` disposition；merge rule 是否确定。

| Stage | 负责 | 不负责 |
|---|---|---|
| Host validate | 结构、身份、revision、field authority、refs、guard、冲突 | 开放式生成新解释；替 Evidence body 背书 |
| Reducer | 按确定规则合并 valid delta，给出重复 / 冲突结果 | 判定 claim 的语义真值 |
| Commit revision | 写入新 revision、mutation event，必要时 checkpoint | 宣告 root cause 或 Workflow gate 已通过 |
| Acceptance policy | 按 Evidence threshold、scope、risk，必要时 human review，决定 active / accepted / rejected / remain unknown | 绕过相同 mutation / revision path 原地改 state |

如果 acceptance policy 决定把 typed entry 的 disposition 改为 ACCEPTED 或 REJECTED，这个处置本身也要提交新的 host-owned mutation 与 revision，不能在 commit 后悄悄原地改写；原 `kind` 与 acceptance / rejection metadata 必须保留。

~~~text
candidate = model_suggestion(base_revision=N)
validated = host_validate(candidate, current_revision=N, policy=P)
delta     = reducer(current_state, validated)
stateN1   = store_compare_and_commit(expected=N, delta)
decision  = acceptance_policy(stateN1.entry, evidence_store, risk_policy)

if decision changes projection:
    acceptance_candidate = host_owned_mutation(base_revision=N+1, decision)
    acceptance_delta     = reducer(stateN1, host_validate(acceptance_candidate))
    stateN2              = store_compare_and_commit(expected=N+1, acceptance_delta)
else:
    keep the committed entry as PROPOSED / ACTIVE / UNRESOLVED
~~~

这条链有三道不能合并的等号：

~~~text
model suggestion != committed state
committed state != accepted fact
accepted fact != authoritative Workflow transition
~~~

## Synthetic Unity / BuildPilot 案例：CS0103 从 rev1 到 rev7

> **SYNTHETIC / ILLUSTRATIVE / NOT A LAB / NO RUNTIME CLAIM**
>
> 本节没有真实 BuildPilot Runtime、Unity 项目、compiler invocation、asmdef graph、effective define set、rerun receipt 或 terminal build receipt。所有 artifact ID 与 revision 都是教学构造；Required Lab = NONE。

证据上限必须先于案例叙事。[Microsoft 当前 CS0103 文档](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103)只支持“名称在当前 class / namespace / scope / context 中不存在”，并列出多个检查方向；[Unity 2022.3 Console 文档](https://docs.unity3d.com/2022.3/Documentation/Manual/Console.html)只支持 Console 展示脚本编译错误。两者都不证明一定缺少 using、asmdef reference、define 或生成代码，也不证明 BuildPilot 真实运行、根因成立、修复有效或整个 build 的 terminal outcome。

在这个天花板下，可以构造一条受控状态演进：

| Revision | Proposed / observed delta | Host-owned committed projection | 为什么下一步不同 | 明确仍未证明 |
|---:|---|---|---|---|
| rev1 | 提交 OBSERVATION / PROPOSED：synthetic Console scenario record 报告 CS0103，identifier=BuildReceiptWriter，带 file / line locator | Host 验证 ref 后只提交 typed observation candidate；kind 与 PROPOSED disposition 都保留 | acceptance policy 只能在 rev1 commit 后评估 | root cause、accepted fact、完整 build outcome、真实项目存在 |
| rev2 | acceptance policy 对 rev1 observation 作 bounded acceptance decision | host-owned mutation 以 base_revision=1 提交 OBSERVATION / ACCEPTED，并写 acceptance_rule / accepted_by / accepted_at_revision | accepted view 现在可以安全引用“该 synthetic record 报告名称不可用”，同时仍保留 kind=OBSERVATION | root cause、跨环境成立、完整 build outcome |
| rev3 | model 提议 H-DEFINE：conditional-compilation expression 可能在 effective symbols 下排除声明 | Host 验证 candidate，reducer 提交 H-DEFINE / ACTIVE | 下一步先定位 declaration source 与 conditional expression，再取得 effective symbols 并计算表达式 | define 确实是根因 |
| rev4 | model / operator 再提议 H-ASMDEF：required assembly reference 可能缺失 | Host 保留并行 hypothesis，不覆盖第一个 | 两个候选可并存，但 action 仍受证据与权限约束 | asmdef 缺失、两个假设互斥或已有优先级 |
| rev5 | synthetic scenario record 表示 declaration source 参与目标编译，且 conditional expression 在 effective symbols 下为 true；新增 counter-evidence ref | reducer 提交 ref；H-DEFINE 在 rev5 commit 中仍为 ACTIVE，随后 acceptance policy 才评估 disposition | 给 rejection decision 提供输入，但不做 commit 后原地改写 | H-DEFINE 已被提交为 REJECTED；H-ASMDEF 为真 |
| rev6 | acceptance policy 对 rev5 entry 作 REJECTED decision | host-owned mutation 以 base_revision=5 提交 H-DEFINE / REJECTED，保留 reason、counter-evidence ref、scope 与 rejected_at_revision；H-ASMDEF 仍 active | 不再重复 define 检查，转向尚未完成的 assembly graph 检查 | H-DEFINE 在所有版本永久错误；H-ASMDEF 因此为真 |
| rev7 | UNKNOWN / UNRESOLVED：assembly graph 尚未取得 | entry=U-ASM-GRAPH；pending action 与 completion gap 同步保留；H-ASMDEF 仍 active | 下一步收窄成“先取图与编译 profile，再讨论根因 / rerun” | assembly reference 缺失、修复、rerun 结果、build terminal success |

完整 History 会保留七次演进；Working Memory@rev7 只保存当前仍影响决策的集合：

~~~text
accepted view : O-CONSOLE-001, kind=OBSERVATION + acceptance metadata
active        : H-ASMDEF + next_test
rejected      : H-DEFINE + reason + counter-evidence + scope
unresolved    : U-ASM-GRAPH + missing inputs
pending       : capture asmdef graph + compiler invocation/profile
completion_gap: graph + profile + rerun receipt
~~~

下面用一个 wrapper 同时给出 synthetic source records 与精确的 `investigation-state-course-v2` 实例。wrapper 的 `sample_support` 不是 Working Memory；它只让 state 内的 locator 在这个教学样例中可解析。

~~~yaml
sample_support:
  classification: SYNTHETIC_SCENARIO_NOT_RUNTIME_EVIDENCE
  runtime_executed: false
  scenario_records:
    SR-CONSOLE-001:
      record_kind: SYNTHETIC_SCENARIO_RECORD
      statement: "constructed Console record reports CS0103 for BuildReceiptWriter at Assets/Editor/BuildMenu.cs:42"
      scope: "synthetic Unity 2022.3 illustration"
    SR-DEFINE-CHECK-001:
      record_kind: SYNTHETIC_SCENARIO_RECORD
      statement: "constructed check locates the declaration in the target compilation and evaluates its conditional-compilation expression to true under the effective symbols"
      scope: "this synthetic revision only"

investigation_state:
  schema: investigation-state-course-v2
  classification: SYNTHETIC_ILLUSTRATIVE_NOT_EXECUTED
  task:
    task_id: SYNTH-CS0103-INVESTIGATION
    goal: "determine the narrow cause of the reported CS0103"
    completion_criteria:
      - "root-cause claim has sufficient scoped evidence"
      - "rerun receipt confirms the bounded outcome"
  revision: 7
  entries:
    - entry_id: O-CONSOLE-001
      kind: OBSERVATION
      disposition: ACCEPTED
      statement: "the synthetic Console record reports CS0103 for BuildReceiptWriter"
      scope: "synthetic Unity 2022.3 illustration"
      evidence_refs: [SYNTH-OBS-CONSOLE-001]
      counter_evidence_refs: []
      derivation_rule: NOT_APPLICABLE
      next_test: NOT_APPLICABLE
      falsifier: NOT_APPLICABLE
      missing_refs_or_inputs: []
      acceptance_rule: "bounded-artifact-observation-v1"
      accepted_by: host_policy
      accepted_at_revision: 2
      rejection_reason: NOT_APPLICABLE
      rejected_at_revision: NOT_APPLICABLE
    - entry_id: H-ASMDEF
      kind: HYPOTHESIS
      disposition: ACTIVE
      statement: "the required assembly reference may be missing"
      scope: "this synthetic Unity 2022.3 illustration"
      evidence_refs: [SYNTH-OBS-CONSOLE-001]
      counter_evidence_refs: []
      derivation_rule: NOT_APPLICABLE
      next_test: "capture the asmdef dependency graph and compiler profile for the same target"
      falsifier: "the captured graph and compiler profile show the required reference is present for that target"
      missing_refs_or_inputs: []
      acceptance_rule: NOT_APPLICABLE
      accepted_by: NOT_APPLICABLE
      accepted_at_revision: NOT_APPLICABLE
      rejection_reason: NOT_APPLICABLE
      rejected_at_revision: NOT_APPLICABLE
    - entry_id: H-DEFINE
      kind: HYPOTHESIS
      disposition: REJECTED
      statement: "the declaration may be excluded because its conditional-compilation expression evaluates false under the effective symbol set"
      scope: "this synthetic revision only"
      evidence_refs: [SYNTH-OBS-CONSOLE-001]
      counter_evidence_refs: [SYNTH-DEFINE-CHECK-001]
      derivation_rule: NOT_APPLICABLE
      next_test: "1) locate the declaration source and expression; 2) evaluate the expression under effective symbols"
      falsifier: "the declaration source participates in the target compilation and is either unguarded or its conditional-compilation expression evaluates true under the effective symbols"
      missing_refs_or_inputs: []
      acceptance_rule: NOT_APPLICABLE
      accepted_by: NOT_APPLICABLE
      accepted_at_revision: NOT_APPLICABLE
      rejection_reason: "the constructed source-and-symbol check matched the falsifier"
      rejected_at_revision: 6
    - entry_id: U-ASM-GRAPH
      kind: UNKNOWN
      disposition: UNRESOLVED
      statement: "does the assembly graph include the required reference?"
      scope: "this synthetic Unity 2022.3 illustration"
      evidence_refs: []
      counter_evidence_refs: []
      derivation_rule: NOT_APPLICABLE
      next_test: NOT_APPLICABLE
      falsifier: NOT_APPLICABLE
      missing_refs_or_inputs: [ASMDEF_GRAPH, COMPILER_PROFILE]
      acceptance_rule: NOT_APPLICABLE
      accepted_by: NOT_APPLICABLE
      accepted_at_revision: NOT_APPLICABLE
      rejection_reason: NOT_APPLICABLE
      rejected_at_revision: NOT_APPLICABLE
  evidence_refs:
    - ref_id: SYNTH-OBS-CONSOLE-001
      source: "inline synthetic scenario record"
      locator: "#/sample_support/scenario_records/SR-CONSOLE-001"
      source_version: synthetic-scenario-v1
      retrieved_at: UNKNOWN
      classification: SYNTHETIC_SCENARIO_RECORD_NOT_RUNTIME_EVIDENCE
    - ref_id: SYNTH-DEFINE-CHECK-001
      source: "inline synthetic scenario record"
      locator: "#/sample_support/scenario_records/SR-DEFINE-CHECK-001"
      source_version: synthetic-scenario-v1
      retrieved_at: UNKNOWN
      classification: SYNTHETIC_SCENARIO_RECORD_NOT_RUNTIME_EVIDENCE
  pending_actions:
    - action_id: A-CAPTURE-GRAPH
      purpose: "capture asmdef graph and compiler invocation/profile"
      prerequisites: [READ_ONLY_ACCESS]
      authority_required: HOST_POLICY
      status: PROPOSED
  completion_gaps: [ASMDEF_GRAPH, COMPILER_PROFILE, RERUN_RECEIPT]
~~~

再次强调：`SYNTH-*` 是只在这个 YAML 内使用的 synthetic ref_id，不是真实 Evidence artifact 或 runtime receipt。每个 ref_id 都先解析到 `investigation_state.evidence_refs`，再由 locator 指向 `sample_support.scenario_records` 中明确标为 synthetic、`runtime_executed=false` 的记录；这只让样例可做内部 reference-integrity 校验，不把构造记录升级为 Runtime Evidence，也不证明诊断正确率。

## Discard、Persist、Checkpoint：按恢复风险选

rev7 足够让调查继续，不表示每段聊天、每份日志和每版中间计划都要永久留在 active Working Memory。Working Memory 是 projection，不是第二份 archive。

下面的选择仍是 **COURSE PROPOSAL**：

| 默认退出 active Working Memory | 应 task-durable 持久化 | 触发 checkpoint 的条件 |
|---|---|---|
| 重复聊天原文；纯排版 scratch；可廉价重算中间变换；已被替代的详细计划；已外置的大 Evidence body | task / goal / completion criteria；revision；accepted entries + original kind + acceptance metadata + refs；active hypotheses + next tests；unresolved；rejected reason + refs + scope；pending actions + authority；completion gaps | context reset / process restart / handoff；恢复代价高；需要审计；并发 writer 有 stale revision 风险；下一动作有外部副作用或 retry 风险 |

两条边界必须同时成立。

第一，退出 active state 不等于删除。History 与 Evidence 的 retention 由各自系统负责；Working Memory 只是不再重复携带那些可回查的大对象。

第二，task-durable 描述作用域，不描述介质。一份 thread state 即使跨进程长期存在数据库中，也可能仍只是当前任务的恢复状态，而不是 Article 15 将讨论的 Long-term / Project Memory。

可以用一个很短的判断树决定是否建 checkpoint：

~~~text
Current projection 能否廉价且安全地重算？
  ├─ yes，且无 handoff / side effect / audit need
  |    -> 保持 ephemeral，或只持久化最小 refs
  └─ no / uncertain
       -> 持久化 task-durable Investigation State
            |
            +-- context reset / process restart / handoff? -> checkpoint
            +-- concurrent writer / stale revision risk?    -> checkpoint
            +-- next action 有外部副作用?                  -> action 前后 checkpoint + receipt
            +-- recovery 昂贵 / 需要审计?                   -> checkpoint
~~~

这只承接 [Article 11]({{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}}) 的最小边界：副作用前后保存 stable action identity、revision、effect receipt 与 continuation point。本篇不重讲 Retry eligibility、Cancellation、Resume、Reconcile、Compensate 或 exactly-once。

短、无副作用、可重算、无需 handoff 的调查可以不建 durable checkpoint。反过来，可能重复提交、发布或调用外部系统的 pending action，需要更强的 checkpoint / receipt 边界。大日志留在 Evidence store，Working Memory 保存 stable locator、digest / version 与受控摘要；rejected hypothesis 只保留最小 reason、refs 和 scope，详细讨论过程可以退出 active projection。

## 坏实现通常怎样坏

Working Memory 的问题通常不是少了一个字段，而是 projection、authority 与 archive 又被揉回一个可随意覆盖的字典。

| Bad implementation | 会怎样坏 | 最小护栏 |
|---|---|---|
| 把完整 transcript 当 Working Memory | 旧假设、反证和当前结论并存，下一步靠模型临时猜 | 建 typed current projection；History 保持时间序列 |
| 让模型直接覆盖 state JSON | suggestion、acceptance 与 authority 混在一起 | MutationCandidate + Host validation + revisioned commit |
| 把 reducer 当 truth judge | merge 成功被误写为 claim 已证实 | reducer 只合并；acceptance policy 单独评估 refs / scope / risk |
| 用单轴 enum 混写五标签 | REJECTED 与 OBSERVED 看似同类，UNKNOWN 容易变成 false | kind 与 disposition 分轴；accepted view 从 typed entries 派生并保留 kind / acceptance metadata |
| rejection 后直接删除 hypothesis | 后续 Step 重复检查，也无法解释它为何退出 | 保存最小 reason、counter-evidence refs、scope、revision |
| 把 Evidence body 全复制进 state | projection 膨胀、版本漂移、产生第二事实源 | 保存 stable ref / locator / version / bounded summary |
| 没有 revision / compare-and-commit | stale writer 覆盖当前调查 | 每个 mutation 声明 base_revision；冲突显式返回 |
| 强制数值 confidence | 无校准概率制造虚假精确性 | 默认使用 status + refs + counter-evidence + next_test |
| 每个微小变化都 checkpoint / 从不 checkpoint | 写放大与噪声，或中断后重复高风险动作 | 按 handoff、恢复成本、并发与副作用触发 |

stale mutation 应 fail closed，而不是 last-write-wins：

~~~text
current revision = 7
candidate.base_revision = 3

result = REVISION_CONFLICT
committed_state = unchanged
next = reload rev7 -> re-evaluate candidate -> submit a new mutation or discard
~~~

这是课程最小 contract，不宣称任何框架使用相同错误码或 compare-and-swap API。真正的工程判断在于：系统能否拒绝一个已经失去前提的写入，而不是能否让每次模型输出都顺利落盘。

## Evidence、版本与前后文章边界

到这里，需要把能确认的产品事实、课程综合与合成示例重新分层。

**按当前来源可以确认，但必须保留 product / version scope：**

- 被检查的 LangGraph、Google ADK、OpenAI Agents SDK 使用不同的 state / context / session / memory 构造；这不代表已经穷尽行业。
- History 与 current state 在若干系统中可以分角色；state 也可含 messages，History 也可恢复 state。
- runtime-managed reducer / event / session-service update path 存在；direct mutation 可能绕过部分框架保证。
- task / thread-scoped state 可以 durable；storage duration 不能单独决定 memory category。
- CS0103 的最窄含义，以及 Unity 2022.3 Console 的可观察边界。

**必须保留 COURSE PROPOSAL：** Working Memory 定义、Investigation State 精确 schema、五标签两轴、semantic authority pipeline，以及 discard / persist / checkpoint 清单。

**不得声称：** reducer、checkpoint、provenance 或 Evidence ref 自动判真；SYNTH-* 是真实 artifact；rev1..rev7 在 BuildPilot / Unity 项目中运行过；已经取得真实 asmdef graph、define set、compiler profile、rerun receipt 或 terminal build outcome。

产品资料也有明确时间边界：LangGraph、ADK、OpenAI Agents SDK 与 Temporal 采用 2026-08-22 的 current hosted docs，未锁定的 package / Server 版本不能写成永久兼容合同；Magentic-One 是 2024 年研究设计先例，不是生产可靠性证明；W3C PROV 是 provenance 标准，不是 truth judge；Unity 示例只限定 Unity 2022.3 的 Console 观察面，不绑定 Unity 内部编译器实现。

相邻文章的责任同样不能被 L 权重吞掉：

| Article / concept | 本篇只承接什么 | 明确不重复 / 不展开什么 |
|---|---|---|
| Article 11 Checkpoint / Recovery | 说明何时值得把一版 Working Memory 放进 checkpoint | Retry、Cancellation、Resume / Reconcile / Compensate、exactly-once |
| Article 12 Context Engineering | 下一 Step 的 Snapshot 可以选择 Working Memory projection | contributor taxonomy、Select / Order / Scope / Fit Budget、Receipt schema |
| Article 13 Context Debugging | 假设 Snapshot / Receipt 已完成 application-visible 检查 | packing chain、八标签、Reconstruction Ladder、Lab 05 |
| Article 15 Session / Long-term / Project Memory | 只留边界：task state durable 不等于 cross-session memory | Session identity / continuity、consolidation、retention / deletion、Project Memory truth policy |
| Article 16 Knowledge Base / RAG | Working Memory 只保存 task-scoped Evidence refs | embedding、vector DB、chunking、retrieve / filter / rerank / inject / cite / retrieval eval |
| Article 18 Evidence Contract | 只使用最小 acceptance boundary：refs、scope、counter-evidence、risk | 完整 Evidence quality、claim gate 与审计数据模型 |

本篇不会创建或预写 Article 15 / 16，也不会把 evidence lookup 扩写成 retrieval system。Working Memory 负责保存当前任务怎样继续，不负责把外部知识怎样检索进来。

## Claim-to-section traceability

| Claim | Evidence status | 正文落点 | Wording boundary |
|---|---|---|---|
| 14-C01 | CONFIRMED | 抽象模型、Evidence 边界 | 只说被检查来源不同，不穷尽行业 |
| 14-C02 | PROPOSAL | 抽象模型 | 定义与 schema 均标 COURSE PROPOSAL |
| 14-C03 | PARTIAL | 问题空间、七对象、保存边界 | 角色边界不等于物理隔离 |
| 14-C04 | CONFIRMED | 问题空间、七对象、synthetic case | History 可恢复 State，state 也可含 messages |
| 14-C05 | CONFIRMED | 七对象、抽象模型、保存边界 | scope 与 storage duration 分开 |
| 14-C06 | CONFIRMED | mutation mechanism、坏实现 | managed update path 不等于 semantic validation |
| 14-C07 | PROPOSAL | authority pipeline、synthetic case | model 只 propose；post-commit acceptance 改变 projection 时必须另提 host-owned revision |
| 14-C08 | PROPOSAL | schema、synthetic case | 统一 typed entries 可表示四种 kind；accepted view 保留 kind 与 acceptance metadata；confidence 可选 |
| 14-C09 | PROPOSAL | 认知两轴、synthetic case | kind 与 disposition 分轴；REJECTED / ACCEPTED 不覆盖 epistemic kind；taxonomy 非产品 / W3C 标准 |
| 14-C10 | PARTIAL | 七对象、schema、Evidence 边界 | provenance / ref 不自动证明 claim 为真 |
| 14-C11 | PROPOSAL | discard / persist / checkpoint、坏实现 | 保存清单是风险导向课程 policy |
| 14-C12 | CONFIRMED / NARROW | 开场、synthetic case、Evidence 边界 | CS0103 / Console 不证明具体根因、修复或完整 build |

Coverage：**12 / 12**。BLOCKED = 0。正文没有把 PARTIAL 或 PROPOSAL 升格为 CONFIRMED。

## Learning Check

1. Snapshot 含当前 CS0103，History 也含两个 hypothesis，为什么下一 Step 仍可能不知道先做什么？最少还缺哪些 committed fields？
2. Console artifact 报 CS0103 后，模型写“一定缺 asmdef reference”。这条记录应先是什么 kind / disposition？若以后进入 `accepted_facts` view，哪些 kind 与 acceptance metadata 必须保留，又要经过什么 authority path？
3. reducer 成功提交 rev3，能否写“root cause confirmed”？为什么？
4. task state 在数据库里保存 90 天，它会自动成为 Long-term / Project Memory 吗？判断应看 duration 还是 scope？
5. 下一动作可能发布制品，哪些 Working Memory / Checkpoint 字段应在动作前后保留，哪些大对象只需 ref？

### 参考思路

1. 缺 current typed projection：active / rejected / unresolved、Evidence refs、pending action、completion gap 与 current revision；不是先增加更多聊天。
2. 先是 HYPOTHESIS / ACTIVE；Host 校验、受控提交，并按 Evidence、scope 与 risk 作 post-commit decision 后，仍须用 host-owned mutation 写入下一 revision。进入 `accepted_facts` view 时保留 `kind=HYPOTHESIS`、acceptance rule、accepter 与 accepted revision。
3. 不能。commit 只证明 update 被 Runtime 接受，reducer 不裁决 semantic truth。
4. 看 task / thread / session scope；durability 与 Long-term Memory category 分开。
5. 保存 task / action identity、revision、accepted / unresolved、Evidence refs、authority、effect receipt、continuation / completion gap；raw log 与 artifact body 留在 Evidence store。

这组问题对应五类可观察的工程能力：

| Competency | 本篇可观察产物 | 不应夸大的地方 |
|---|---|---|
| State modeling | schema、revision、typed claim / hypothesis / unknown | 设计稿不等于 production implementation |
| Authority design | candidate / validator / reducer / store / acceptance responsibilities | 不宣称框架自动提供 semantic policy |
| Diagnostic reasoning | CS0103 从 observation 到 hypothesis / rejection / unknown 的窄演进 | synthetic case 不证明诊断准确率 |
| Reliability / recovery | discard / persist / checkpoint trigger 与 stale revision fail-closed | 不重讲或证明 distributed recovery |
| Evidence discipline | ref、scope、counter-evidence、does-not-prove 与 version boundary | provenance 不等于 truth |

这张表只说明读者可以从哪些产物审查设计判断。schema 或合成案例只能证明设计是否可审查，不能被夸大成 production implementation、真实诊断准确率或 distributed recovery。

真正可迁移的最低合同，是让每条可审查 entry 同时保留 epistemic kind、disposition 与必要 acceptance / rejection metadata，并让每次处置变化都经过 Host 批准的新 revision。

## 最短结论

> 上下文决定这一 Step 看见什么；Working Memory 以任务级、带版本的工作投影保存“现在按什么继续”，但只有 Host 才能批准它如何变化。

## 参考资料

- [LangGraph：Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)
- [LangGraph：Graph API](https://docs.langchain.com/oss/python/langgraph/graph-api)
- [LangGraph：Memory](https://docs.langchain.com/oss/python/langgraph/add-memory)
- [Google ADK：Session State](https://adk.dev/sessions/state/)
- [Google ADK：Session](https://adk.dev/sessions/session/)
- [Google ADK：Memory](https://adk.dev/sessions/memory/)
- [OpenAI Agents SDK：Context management](https://openai.github.io/openai-agents-python/context/)
- [OpenAI Agents SDK：RunState](https://openai.github.io/openai-agents-python/ref/run_state/)
- [OpenAI Agents SDK：Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/)
- [Temporal：Event History](https://docs.temporal.io/workflow-execution/event)
- [Magentic-One：A Generalist Multi-Agent System for Solving Complex Tasks](https://arxiv.org/abs/2411.04468)
- [W3C PROV Overview](https://www.w3.org/TR/prov-overview/)
- [Microsoft：CS0103](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103)
- [Unity 2022.3：Console](https://docs.unity3d.com/2022.3/Documentation/Manual/Console.html)
