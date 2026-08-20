# Article 08 Evidence Register｜Agent Loop

- Evidence Phase：`EVIDENCE_MERGE COMPLETE`
- Evidence Status：`READY / NO BLOCKED CLAIM`
- Evidence Gate Recommendation：`PASS`
- Evidence Gate Closure：`MASTER DECISION PENDING`
- Claim Count：`8`
- Claim Summary：`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Evidence Card Count：`12`
- Required Lab：`Lab 03 Minimal Agent Loop`
- Lab Dependency：`REQUIRED / EXECUTED / OBSERVED / MERGED`
- Runtime Evidence：`AVAILABLE / DETERMINISTIC FIXTURE`
- Retrieval Date：`2026-08-20`（Asia/Shanghai）
- Preliminary Evidence Decision：`PROCEED_TO_LAB_EXECUTION`
- Evidence Merge Date：`2026-08-20`（Asia/Shanghai）

> 本 Register 已按 `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 完成 Lab 03 Evidence Merge。pre-Lab Expected 仍仅作为验收基线；Claim 升级依据是 raw Observation、execution log、implementation/spec inspection 与 Researcher interpretation。

## 1. Claim Register

| Claim ID | Claim | Type | Status | Evidence | Lab dependency | Boundary |
|---|---|---|---|---|---|---|
| 08-C01 | OpenAI Agents SDK Python current loop 会在 final output、handoff、tool execution + result append 之间推进；该 SDK 的 `max_turns` 以 AI invocation 为计数单位 | product contract | `CONFIRMED` | E-08-01, E-08-02 | none | 限 Python SDK current docs / release identity；不外推成 universal loop |
| 08-C02 | cited products 的 `turn` / `step` 计数单位不同，不能把 OpenAI model-invocation turn、logical chat turn、LangGraph super-step 直接等同 | comparative contract | `CONFIRMED` | E-08-01, E-08-02, E-08-05 | none | 只证明 cited products 间不等同，不声称穷举行业术语 |
| 08-C03 | 本文以 goal-bounded Run、external grouping Turn、committed loop Step 作为课程工作定义 | teaching abstraction | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | 08-C02, E-08-08, E-08-11 | satisfied for local conformance | 不是 glossary 全局事实；不是 SDK API 定义 |
| 08-C04 | 在 cited products 中，tool result 成为后续 model-visible item 与 authoritative state update 可以是不同操作 | product contract | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | E-08-01, E-08-03, E-08-06, E-08-07, E-08-10, E-08-11 | satisfied | 只新增 fixed implementation conformance，不把课程 Observation 写成 universal API |
| 08-C05 | Lab 03 的 Decision 只是 candidate，authoritative state 仅由 deterministic Host reducer 提交 | lab design | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | E-08-08, E-08-10, E-08-11 | satisfied for local conformance | 这是课程安全设计，不是 framework universal requirement |
| 08-C06 | stop 可以由 runtime/config/limit 产生而不只由模型产生；bounded termination 不能自动标成 success | product contract | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | E-08-01, E-08-02, E-08-03, E-08-05, E-08-06, E-08-10 | satisfied | Lab `max_steps` 不等同 OpenAI turn、LangGraph super-step 或成本预算 |
| 08-C07 | Lab 03 fixed Host completion contract 能拒绝 unresolved tool failure 与缺少 required evidence 的 pseudo-success | behavior | `CONFIRMED / FIXED-HOST-FIXTURE-SCOPED` | E-08-09, E-08-10, E-08-11, E-08-12 | satisfied：AL-01, AL-02, AL-04 | 不证明任意 runtime/model 采用相同 contract |
| 08-C08 | 四条冻结轨迹能在 Lab 03 deterministic fixture 中可重复地区分 success、tool failure、max-step stop、duplicate/no-progress + pseudo-completion | behavior | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` | E-08-09, E-08-10, E-08-11, E-08-12 | satisfied：AL-01～AL-04；two fresh processes | 不证明 Provider/model determinism、planning quality 或 production reliability |

### Claim status rules

- `CONFIRMED` 只用于 cited product contract，并保留产品与版本 scope。
- `PROPOSAL` 不会因为 Lab 跑通而升级成“行业事实”；Lab 只能证明实现 conformance。
- 行为 Claim 只有在 raw trace、state snapshots、process/build logs、implementation/spec inspection 与 Researcher interpretation 全部对齐后才升级；C07/C08 已在明确 Lab scope 内满足。
- Tool Result、Observation、Evidence 三者不可互相替换。

## 2. Source Manifest

| Source ID | Source | Authority | Version / immutability | Retrieved | Used by | Limitation |
|---|---|---|---|---|---|---|
| S-01 | [OpenAI Agents SDK Python — Running agents](https://openai.github.io/openai-agents-python/running_agents/) | official product docs | current hosted docs；not immutable | 2026-08-20 | C01, C02, C04, C06 | 页面可能随 main 更新；behavior scope 以页面当日为准 |
| S-02 | [OpenAI Agents SDK Python — Run reference](https://openai.github.io/openai-agents-python/ref/run/) | official API reference | current hosted docs；not immutable | 2026-08-20 | C01, C02, C06 | 不代表其他语言 SDK |
| S-03 | [OpenAI Agents SDK Python — Agents](https://openai.github.io/openai-agents-python/agents/) | official product docs | current hosted docs；not immutable | 2026-08-20 | C04, C06 | tool-use behavior 是 product configuration，不是 universal policy |
| S-04 | [PyPI — openai-agents](https://pypi.org/project/openai-agents/) | official package registry record | `0.22.0` uploaded 2026-08-19；verified source commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024` | 2026-08-20 | C01 scope | release metadata 不单独证明 runtime behavior；hosted docs 与 tag 未做逐行同构审计 |
| S-05 | [LangGraph — Graph API overview](https://docs.langchain.com/oss/python/langgraph/graph-api) | official product docs | current hosted docs；package unpinned | 2026-08-20 | C02, C06 | `super-step` 是 LangGraph 术语；不得映射到 OpenAI turn |
| S-06 | [LangChain — Tools](https://docs.langchain.com/oss/python/langchain/tools) | official product docs | current hosted docs；package unpinned | 2026-08-20 | C04, C06 | 示例/contract 只覆盖 LangChain/LangGraph surface |
| S-07 | [LangGraph — Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents) | official product docs | current hosted docs；package unpinned | 2026-08-20 | C04 | 示例不证明课程 Lab behavior |
| R-01 | Published Article 03 + evidence | repository durable published dependency | repository checkpoint before Article 08 | 2026-08-20 | C07 | shape validation 不等于 truth/success |
| R-02 | Published Article 05 + evidence | repository durable published dependency | repository checkpoint before Article 08 | 2026-08-20 | C04, C07 | one Tool Use 不证明 Agent Loop |
| R-03 | Published Article 06 + evidence | repository durable published dependency | repository checkpoint before Article 08 | 2026-08-20 | C04, C07 | Tool Runtime closure 不证明本 Loop；cancellation 不回滚副作用 |
| R-04 | Published Article 07 + evidence | repository durable published dependency | published commit `f3de0f2a7b1e06c530900627183bd364ca0b4314` | 2026-08-20 | boundary only | 只证明 MCP protocol boundary；**不证明已经安全调用外部能力** |
| R-05 | series plan / glossary / factory contracts | repository canonical governance | current workspace at Research | 2026-08-20 | C03, all stop lines | Article workspace 不得回写全局 glossary |
| L-01 | `labs/lab-03-minimal-agent-loop/observations/execution-log.md` | Lab Engineer execution ledger | frozen environment；formal run 2026-08-20 | C07, C08 | 命令/环境/失败 ledger；不是 Claim interpretation |
| L-02 | `observations/run-a/*` six raw artifacts | fresh-process runtime evidence | schemas `lab03-*-v1`；fixture SHA pinned | C04, C05, C06, C07, C08 | deterministic fixture only |
| L-03 | `observations/run-b/*` six raw artifacts | independent fresh-process runtime evidence | byte-equal to L-02 | C08 | 相同 binary/input；不证明模型随机性 |
| L-04 | `src/MinimalAgentLoop/*` + `tests/MinimalAgentLoop.Tests/Program.cs` | implementation + independent BCL spec | `net10.0` / BCL-only | C03, C04, C05, C06, C07, C08 | 证明当前实现/断言 surface；不是 industry contract |

## 3. Evidence Cards

### E-08-01｜OpenAI Runner loop contract

- **Evidence Type**：official product documentation
- **Source**：[Running agents](https://openai.github.io/openai-agents-python/running_agents/)
- **Product / Version Scope**：OpenAI Agents SDK Python；current hosted docs retrieved 2026-08-20；release identity cross-check `openai-agents 0.22.0`
- **Supports**：08-C01, 08-C02, 08-C04, 08-C06
- **Primary observation / paraphrase**：Runner 调用 LLM；若产生 final output 则结束，handoff 则以新 Agent 重跑，tool call 则执行工具、追加结果再调用模型。一次 run 可包含一个或多个 Agent 和一个或多个 LLM call，而同页还称这次 run 为 single logical chat turn。`max_turns` 限制 loop turns。
- **Proves**：该 SDK current Runner 的 documented control flow；`run` 与内部多次 LLM call 可以共存；存在外部 turn limit。
- **Does not prove**：不证明课程 Step；不证明工具结果真实；不证明 final output 满足业务目标；不证明其他 SDK 的 turn 含义。
- **Counter-evidence**：同一页面把完整 run 称 logical chat turn，而 API 的 max-turn counter 又面向 model invocation，禁止写成单一 universal Turn。
- **Limitations**：hosted docs 非 immutable；未执行 Python SDK。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-02｜OpenAI max_turn unit

- **Evidence Type**：official API reference + package release metadata
- **Source**：[Run reference](https://openai.github.io/openai-agents-python/ref/run/), [PyPI release](https://pypi.org/project/openai-agents/)
- **Product / Version Scope**：OpenAI Agents SDK Python current docs；PyPI `0.22.0`（2026-08-19），verified source commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024`
- **Supports**：08-C01, 08-C02, 08-C06
- **Primary observation / paraphrase**：reference 把 `max_turns` 的一次 turn 定义为一次 AI invocation，并说明 final output 会结束 loop；超限是 bounded termination / error surface，而不是 success contract。
- **Proves**：该 SDK 的计数单位不是“工具调用次数”；limit termination 与 final success 是不同路径。
- **Does not prove**：不证明默认值适合本课程；不证明成本/延迟预算；不证明已发生外部副作用可撤销。
- **Counter-evidence**：logical chat turn 与 max-turn invocation 不是同一粒度。
- **Limitations**：PyPI 元数据只为版本 scope，不单独证明 behavior；docs/tag 未逐行 diff。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-03｜Tool-use behavior changes stopping

- **Evidence Type**：official product documentation
- **Source**：[Agents — tool use behavior](https://openai.github.io/openai-agents-python/agents/)
- **Product / Version Scope**：OpenAI Agents SDK Python current hosted docs retrieved 2026-08-20
- **Supports**：08-C04, 08-C06
- **Primary observation / paraphrase**：default behavior 把工具结果送回 LLM；`stop_on_first_tool`、`StopAtTools` 与 custom `ToolsToFinalOutputFunction` 可改变是否继续调用模型、是否直接采用工具结果。
- **Proves**：stop/continue 并非只能由模型 final signal 控制；runtime/configuration 可以改变 loop。
- **Does not prove**：不证明 direct tool output 是事实正确的业务成功；不证明这些选项是其他 runtime 的标准。
- **Counter-evidence**：相同 tool result 在不同配置下可能继续 loop 或直接返回，因此“看到 result 就一定 observe 再 decide”不是 universal contract。
- **Limitations**：未执行真实 Provider/runtime。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-04｜Current package identity

- **Evidence Type**：official package registry metadata
- **Source**：[PyPI — openai-agents](https://pypi.org/project/openai-agents/)
- **Product / Version Scope**：`openai-agents 0.22.0`；uploaded 2026-08-19；verified source commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024`
- **Supports**：source date/version scope for 08-C01
- **Primary observation**：registry 页面给出当前 release identity、upload date 与 verified source commit。
- **Proves**：本次检索所记录的 package release identity。
- **Does not prove**：不证明 hosted docs 与该 commit 完全同构；不证明任何 loop case 已运行。
- **Counter-evidence**：current-main docs 可在 release 后改变，所以行为证据仍引用具体 docs 页面与检索日。
- **Limitations**：没有把 registry metadata 当 runtime observation。
- **Disposition**：`ACCEPTED / SCOPE ONLY`

### E-08-05｜LangGraph super-step and state reducer

- **Evidence Type**：official product documentation
- **Source**：[LangGraph Graph API overview](https://docs.langchain.com/oss/python/langgraph/graph-api)
- **Product / Version Scope**：LangGraph current hosted docs retrieved 2026-08-20；local package unpinned
- **Supports**：08-C02, 08-C06
- **Primary observation / paraphrase**：State 是 nodes 共享的 snapshot；node 返回 update，reducer 应用 update；`super-step` 是 graph iteration，同一 super-step 可有多个并行 node。recursion limit 以 super-steps 限制 graph。
- **Proves**：LangGraph 的 step 与 limit unit 具有 graph-specific semantics；state update 有明确 reducer contract。
- **Does not prove**：不证明 OpenAI turn 可映射为 super-step；不证明课程 Host reducer 已实现；不证明 parallel workflow（Article 10）。
- **Counter-evidence**：一个 super-step 可含多个 nodes，直接画成“一个 step = 一次模型调用/工具调用”会错误。
- **Limitations**：未固定或执行 local LangGraph package；只作 comparative contract evidence。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-06｜ToolMessage is not automatic graph-state update

- **Evidence Type**：official product documentation
- **Source**：[LangChain Tools](https://docs.langchain.com/oss/python/langchain/tools)
- **Product / Version Scope**：LangChain / LangGraph current hosted docs retrieved 2026-08-20；local package unpinned
- **Supports**：08-C04, 08-C06
- **Primary observation / paraphrase**：string tool result 会变成 `ToolMessage` 供模型处理；工具若要更新 graph state 可返回 `Command`，更新再按 reducer 应用；`return_direct` 可直接结束 loop。
- **Proves**：在该 product contract 中，model-visible tool result、state update 与 loop short-circuit 是可区分的操作。
- **Does not prove**：不证明课程 Observation schema；不证明任意工具结果是 Evidence；不证明 Host 应采用唯一 reducer 设计。
- **Counter-evidence**：tool result 并非总是进入同一条“observe then re-decide”路径，也不是自动任意改 state。
- **Limitations**：documentation-level only。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-07｜Agent feedback-loop example

- **Evidence Type**：official product example/documentation
- **Source**：[LangGraph Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)
- **Product / Version Scope**：LangGraph current hosted docs retrieved 2026-08-20；local package unpinned
- **Supports**：08-C04
- **Primary observation / paraphrase**：示例让 LLM 选择 action、tool node 执行，再通过 correlated ToolMessage / conditional edge 继续或结束，并把 tool result 称作 observation。
- **Proves**：该示例中 Decide、tool execution、result correlation 与 conditional continuation 的具体组织方式。
- **Does not prove**：`observation` 是跨 SDK 标准实体；课程四轨迹、completion contract 或 state digest 已运行。
- **Counter-evidence**：示例术语与本文教学抽象形似但 scope 不同，文章必须显式标注。
- **Limitations**：example-level evidence，不能替代 Lab raw artifacts。
- **Disposition**：`ACCEPTED / PRODUCT-SCOPED`

### E-08-08｜Published dependency boundary card

- **Evidence Type**：repository durable published content + evidence registers
- **Source**：Published Articles 03, 05, 06, 07；their evidence registers；canonical glossary / series plan
- **Version Scope**：repository at Article 07 published checkpoint `f3de0f2a7b1e06c530900627183bd364ca0b4314`，read 2026-08-20
- **Supports**：08-C03, 08-C05, 08-C07 design basis
- **Primary observation / paraphrase**：Article 03 冻结 shape/truth boundary；05 冻结 tool intent/result/evidence boundary；06 冻结 Tool Runtime、normalized result、trace、cooperative cancellation；07 冻结 MCP protocol boundary。
- **Proves**：Article 08 可以复用这些术语边界，且必须把 Tool Result、Observation、state、Evidence 和 success 分开。
- **Does not prove**：Article 07 **没有**证明 Agent 已经安全调用外部能力；任何已发布前篇都没有执行 Lab 03，也没有证明本篇 four-case loop。
- **Counter-evidence**：把“protocol success”或“一次 tool use”升级成 Agent Loop 会违反已发布 evidence stop line。
- **Limitations**：local dependency evidence，不替代 current upstream product docs。
- **Disposition**：`ACCEPTED / LOCAL DEPENDENCY ONLY`

### E-08-09｜Lab execution and environment ledger

- **Evidence Type**：local runtime execution ledger
- **Source**：`labs/lab-03-minimal-agent-loop/observations/execution-log.md`
- **Environment Scope**：Windows `10.0.19045` / `win-x64`；.NET SDK `10.0.301`；Host `10.0.9`；`net10.0`；BCL-only；2026-08-20
- **Supports**：08-C07, 08-C08
- **Experiment**：locked restore、Release build、BCL spec、formal run-a、formal run-b、independent verify-only；Provider/network/credentials 均未使用。
- **Observation**：最终命令全部 exit `0`；build `0 warnings / 0 errors`；formal runs 与 verifier PASS。
- **Evidence Interpretation**：final green chain 是 frozen local environment 下的有效实验，不是文档模拟。
- **Does not prove**：Provider/model、network、MCP、production reliability、cancellation 或 external side effects。
- **Counter-evidence / failures**：ledger 保留 CIM denied、compile collision、fixture EOF、testhost unavailable、live-reference digest mismatch 与两次交付阶段 interruption；最终复核覆盖修正后的实现，但失败事实没有删除。
- **Disposition**：`ACCEPTED / FIXED-ENVIRONMENT`

### E-08-10｜Lab raw runtime artifacts

- **Evidence Type**：raw JSONL + artifact manifests
- **Source**：`observations/run-a/`、`observations/run-b/`
- **Artifact Scope**：each run 6 files / 47,772 bytes；corresponding files pairwise byte-identical
- **Supports**：08-C04, 08-C05 local conformance, 08-C06 local conformance, 08-C07, 08-C08
- **Experiment**：two fresh-process executions of the same frozen binary/input。
- **Observation**：each run has `10 STEP / 4 TERMINAL / 10 states / 7 Tool Outcomes / 7 Observations / 10 decisions / 1 SUCCEEDED`；six shared SHA-256 values recorded in manifests and execution log。
- **Case observation**：AL-01 `GOAL_SATISFIED/SUCCEEDED`；AL-02 `UNRESOLVED_TOOL_FAILURE/FAILED`；AL-03 `MAX_STEPS_EXHAUSTED/INCOMPLETE` with unconsumed third decision；AL-04 `STOP_CONTRACT_FAILED/FAILED` with repeat/no-progress and rejected `EV-FAKE`。
- **Evidence Interpretation**：四条 frozen trajectory 在 fixed Host/fixture 中可判定且可复现；STOPPED 与 SUCCEEDED 分离。
- **Does not prove**：真实 Model/Provider determinism、planning、unbounded workflows、unseen inputs 或生产可靠性。
- **Disposition**：`ACCEPTED / RAW BEHAVIOR EVIDENCE`

### E-08-11｜Implementation and independent spec conformance

- **Evidence Type**：source inspection + independent BCL spec
- **Source**：`src/MinimalAgentLoop/LabRunner.cs`、`Models.cs`、`Canonical.cs`、`fixtures/cases.json`、`tests/MinimalAgentLoop.Tests/Program.cs`
- **Version Scope**：current Lab 03 implementation executed 2026-08-20
- **Supports**：08-C03 implementation mapping, 08-C04 local separation, 08-C05 conformance, 08-C06 local limit, 08-C07, 08-C08
- **Experiment**：Researcher inspected input validation、pre-decision guard、Tool Outcome normalization、Host reducer、completion derivation、digest generation and verifier assertions。
- **Observation**：`cases.json` has no expected-answer fields；anti-self-fulfilling validation rejects them；Decision cursor is read before action but max-step guard runs first；`ExecuteStop` derives outcome from output/provenance/facts/exact Evidence/unresolved failures；spec independently recomputes digests and cross-references raw Result/Observation/state rows。
- **Evidence Interpretation**：raw outcome 并非 runner 从 expected answer 复制；current fixed implementation conforms to the frozen control-plane proposal。
- **Does not prove**：Host-only reducer 是行业强制；scripted candidate 等于真实模型 decision；tests 覆盖所有未知错误。
- **Disposition**：`ACCEPTED / IMPLEMENTATION CONFORMANCE`

### E-08-12｜Failure ledger and counter-evidence disposition

- **Evidence Type**：preserved failed-attempt / interruption ledger
- **Source**：`observations/execution-log.md` sections `Preserved failed attempts` and continuation verification
- **Supports**：Evidence hygiene for 08-C07, 08-C08
- **Observation**：五类实现/验证失败被记录并修正；两次 Master interruption 均发生在 formal commands 已结束后的 Markdown delivery，第一次后 restore/build/test/verifier 全部重新 PASS，第二次后只检查已有 log/artifacts。
- **Evidence Interpretation**：这些记录证明生成过程并非一次性“全绿”；它们没有反驳 final raw case outcomes，但限制成功结论只落在最终 artifacts/current source。
- **Does not prove**：失败路径已成为生产 recovery 能力；interruption 等于 cancellation trajectory；旧失败中间产物完整保留为独立 run。
- **Limitation**：失败事实由 execution ledger 保存，没有把它们包装成正式 normalized case artifacts。
- **Disposition**：`ACCEPTED / LIMITATION PRESERVED`

## 4. Preliminary Evidence decision（pre-Lab record）

> 本节保留执行前决定，不代表当前 Evidence Gate 状态；current merge 见第 8 节。

### Decision

`PROCEED_TO_LAB_EXECUTION`

### Why design may be frozen

1. S-01～S-07 已给出足够的 current product contract，能界定 loop、result return、state reducer、stop 与 counter 的 scope。
2. C03/C05 被明确标成课程 Proposal，不需要伪装成 upstream fact。
3. C07/C08 的 behavior gap 可被四条 deterministic trajectories 直接证伪。
4. Provider/network/credential 均不需要；fixture 与 fault seam 可以固定。

### Why Evidence Gate remained closed before execution

- Lab 03 尚未 build / run。
- `observations/`、raw trace、state snapshots、process log 均不存在。
- 没有 observed counts、digests、terminal records 或 fresh-process comparison。
- Lab Design 中的所有 case outcome 都只是 Expected。

## 5. Lab 03 Evidence Mapping（frozen pre-Lab mapping）

| Case | Claim | Required raw artifacts | Frozen Expected（historical, not itself Evidence） | Falsifier |
|---|---|---|---|---|
| AL-01 success | C05, C07, C08 | 3 step rows；3 state snapshots；terminal；output；process log | `GOAL_SATISFIED / SUCCEEDED`，required log + source Evidence present | output 成功但缺 required evidence；非 Host 派生 outcome；count/digest 不符 |
| AL-02 tool failure + requested success | C04, C05, C07, C08 | failed Tool Outcome；failure Observation；2 step rows；state；terminal | `UNRESOLVED_TOOL_FAILURE / FAILED`；failed-result digest 被 Observation 引用 | failure 被丢失；REQUEST_STOP 把 outcome 涂成 success；runner crash |
| AL-03 max steps | C06, C08 | 2 step rows；state；terminal；decision/tool call counters；unconsumed scripted decision proof | `MAX_STEPS_EXHAUSTED / INCOMPLETE`；third decision/tool `NOT_RUN` | 第三次 decision/tool 已调用；off-by-one；limit 被写成 success |
| AL-04 repeat + pseudo-complete | C05, C07, C08 | 3 step rows；same action fingerprints + distinct invocation IDs；full/goal digests；terminal | repeat step `NO_PROGRESS`；`STOP_CONTRACT_FAILED / FAILED` | fingerprint 含 invocation ID；只看 full-state history；fake evidence 被接受；pseudo-stop 成功 |

### Merge rule

执行后 Researcher 必须逐 case 记录：

1. **Experiment**：实际命令、环境、fixture digest、exit code。
2. **Observation**：raw artifact path、row/count/digest、terminal facts；不得把 Expected 抄成 Observed。
3. **Evidence Interpretation**：该 Observation 支持或反驳哪个 Claim，以及为何。
4. **Claim Status**：只在 acceptance 全部满足时升级；失败或缺件保留 `PARTIAL` / 标 `BLOCKED`。

## 6. Freshness, limitations, and counter-evidence

- 检索日是 2026-08-20；current hosted docs 可能变化。后续引用必须保留日期。
- OpenAI behavior claims 限 Python SDK current docs；没有把 JS、其他语言或 provider 实现混入。
- LangGraph sources 用来展示术语与 state contract 差异，不把其 super-step 当课程 Step。
- deterministic substitute 不证明模型选择正确工具、会恢复、会自发停止或 Provider lifecycle。
- Lab 使用 read-only local fixtures，不证明外部服务、权限、网络、side-effect rollback。
- cancellation 只保留设计边界；本 Lab 不生成 cancellation Observation。外部副作用不会因为 cancellation 自动撤销。
- Article 09 Planning、10 Workflow/State Machine、11 long-running recovery、12+ context/memory、20 budget engineering 均保持 stop line。

## 7. Evidence Gate Stop Line（pre-Lab conditions）

> 以下条件是执行前 hard stop。第 8 节记录它们已经由 raw artifacts 与 Researcher Merge 满足；本段不再是 current status。

在四条冻结轨迹完成真实 build / run / named fault injection、两次 fresh-process normalized artifact comparison，并由 Researcher完成 Evidence Merge 前：

- Evidence Gate 必须保持 `NOT_READY`；
- 08-C07 / 08-C08 必须保持 `PARTIAL`；
- 08-C05 必须保持 `PROPOSAL`，最多新增 implementation-conformance evidence；
- 不得创建 Outline / Draft；
- 不得把 Lab Expected 写成 Observed；
- 不得声明 Article 08 可以发布。

## 8. Researcher Evidence Merge

### 8.1 Artifact integrity

| Check | Observed | Disposition |
|---|---|---|
| Frozen Design prefix | first `30312` bytes SHA-256 `242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86` | PASS |
| run-a/run-b six files | all six corresponding files byte-identical | PASS |
| `artifact-manifest.json` | `6B1E3148DF5812B92A155BCEB29783B540CF9D4E8576D9012388A6B73ACD00E6` | PASS |
| `case-results.jsonl` | `90F2256AA18E401C6DDCEFFFB0837AB25105C58A80A3D24D3A87ADFD907D157D` | PASS |
| `observations.jsonl` | `5A446F0327571D33AECFFB2B642C71A3CC9D28ADBE9E5341BAF8FD5D21809586` | PASS |
| `states.jsonl` | `88F3E541C1A17FD44AA924ACB912C62B9C387F0669EF0F071979AD94A750E729` | PASS |
| `tool-outcomes.jsonl` | `128FE933B0CFF633949B0EDABEF6B4294379D119C4174D2F08EBF420B54A1332` | PASS |
| `trace.jsonl` | `3B816B5B7E2E370EED38268F02E83B045EAEDC6EAB9CEC801266ADD76D4D6427` | PASS |

### 8.2 Per-Claim merge

| Claim | Experiment | Observation | Evidence Interpretation | Final Status |
|---|---|---|---|---|
| 08-C01 | current official OpenAI Python SDK docs + `0.22.0` release identity | documented final/handoff/tool-result loop and AI-invocation max-turn unit | product contract remains current-date/version scoped；Lab neither broadens nor contradicts it | `CONFIRMED / PRODUCT-SCOPED` |
| 08-C02 | compare OpenAI `turn` scopes and LangGraph `super-step` | units differ；Lab uses one external turn with multiple committed Steps | cited-product non-equivalence stands；no universal hierarchy inferred | `CONFIRMED / CITED-PRODUCTS-SCOPED` |
| 08-C03 | inspect Lab trace/state use of Run/turn_index/step_index | four runs keep `turn_index=1` while committing 10 Steps | fixed implementation conforms to course vocabulary, but vocabulary remains a teaching choice | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` |
| 08-C04 | correlate 7 Tool Outcomes, 7 Observations, trace refs and state snapshots | every ACT resolves Outcome -> Observation -> state；AL-02 FAILED result becomes PASS-normalized TOOL_FAILURE | supports upstream separation and fixed Lab chain；does not create universal Observation API | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` |
| 08-C05 | inspect `cases.json`、`LabRunner.ExecuteAct/ExecuteStop` and state revisions | input has no expected answer；each Step commits exactly one revision；Host derives authoritative outcome | current implementation conforms, while Host ownership remains course design | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` |
| 08-C06 | execute/inspect AL-03 max_steps=2 | two decisions/tools consumed；third Decision remains；terminal INCOMPLETE before next Decide | fixed external limit is not success；unit remains Lab Step, not SDK turn/super-step/cost budget | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` |
| 08-C07 | execute AL-01/02/04 and inspect completion derivation | valid completion succeeds；unresolved failure and fake/missing Evidence fail despite requested success | fixed Host contract distinguishes terminal success from pseudo-completion | `CONFIRMED / FIXED-HOST-FIXTURE-SCOPED` |
| 08-C08 | two fresh-process four-case suites + independent verifier | exact counts/outcomes/digests；all six artifact pairs equal | frozen deterministic trajectories are reproducible for current binary/fixture | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` |

### 8.3 Counter-evidence and limitations retained

- Five pre-green implementation/verification failures remain recorded. They limit the claim to current source/final artifacts; they do not become runtime recovery evidence.
- Two orchestration interruptions occurred after formal commands ended while Markdown delivery was being organized. No Lab command ran during either interruption, so they are not case failures or cancellation observations.
- Deterministic Decision source proves no model/provider quality, autonomy, recovery or stochastic behavior.
- No cancellation trajectory、external side effect、network、MCP、permission or production load was observed。
- No conclusion is made for Planning、Workflow/State Machine、long-running recovery、Context/Memory or token/cost/latency budgets。

### 8.4 Evidence Gate recommendation

- Lab Status Candidate：`VERIFIED / EVIDENCE_MERGED`
- Claim Summary：`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Evidence Gate Recommendation：`PASS`
- Evidence Gate Closure：`MASTER DECISION PENDING`
- Blocker：`NONE`
- Exact next action：Master 独立复核并关闭 Evidence Gate；随后分派真实 Outliner。Researcher 不创建 Outline / Draft。
