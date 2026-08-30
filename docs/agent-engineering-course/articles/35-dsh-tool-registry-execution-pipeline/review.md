# Article 35 Review｜Cycle 1

> Role：`REVIEWER / FRESH CONTEXT`
> Gate：`REVIEW`
> Review Date：`2026-08-30 (Asia/Shanghai)`
> Decision：`FAIL / REVISION REQUIRED`

## Review scope and independence

- Reviewer：`/root/a35_reviewer_cycle1`。本轮独立读取 canonical、Course Factory / Article workflow / eight-role contracts、review checklist、Evidence Card contract、Glossary、Article 35 全 workspace、原始 Trace、相关 earlier articles 与 pinned DSH source。
- 本轮遵循 TwoEgg 原理篇方法，按“问题空间 -> 抽象模型 -> pinned 实现 -> 工程边界”审查；没有读取 Author 的隐藏推理或 self-score。
- 唯一写入为本 `review.md`。Draft、Research、Evidence、Source Map、raw artifacts、Factory state、Published Content、Article 36+ 与 external fixture 均未修改。

## Artifact identity and direct checks

- Draft：`38652 bytes / 737 physical lines / SHA-256 A76E323AC7357227A5D529BEBACC5B2E450FA3A8FF83200FE3553271F9C15008`。
- Outline：`15864 bytes / 333 physical lines / SHA-256 7E1350F8D617E9BF59625DE3B8E81F552BDD2771210A3D6B38868D16065C661E`。
- H1 精确等于 canonical title：`Tool Registry 与 Tool Execution Pipeline`。
- Draft Claim coverage：`12 / 12`；required cases：`35-X01—X05`。
- Recovery raw integrity：manifest 列出的 `9 / 9` 非 manifest artifact bytes / SHA-256 fresh recomputation 一致。
- Recovery JSONL：`13` records；case distribution=`3 / 3 / 2 / 2 / 3`；`13 / 13` callId 唯一；required top-level fields、Session `1/1`、next-request `1`、derived-history `1` 与 content hash correlation 均通过。
- Accepted capture：`1 file / 5 tests / exit 0`。Cycle 0 保持 `22 passed / 0 failed / NOT_ACCEPTED`；Recovery Attempt 1 保持 `exit 0 / 0 of 5 selected / NOT_ACCEPTED`。
- External fixture fresh read：`HEAD=cd5ef8148158c3a752a658978873241fdf8e2bbc`，status empty，`git diff --check` 无输出。
- Link gate：Draft 的 `4` 个 `relref` occurrence 只指向已存在的 Article 34 与课程索引；shortcode 使用 ASCII quotes。全部 pinned-source GitHub blob path 在本地 fixed fixture 中存在。
- Repository `git diff --check` 没有 whitespace error；仅报告现有 LF/CRLF warning。

## What passed

### Technical and evidence substance

- Registry / model view 与 call / execution 两条链分开；`Registry != Permission`、`Canonical != Authorized`、`Provider != Tool`、`UI Presentation != Model Content`、`schema validation != side-effect safety` 五条核心反等同均进入正文。
- `ToolRuntime.register/view/get/restrict/wireSchemas`、`parseArguments/createExecution`、typed `defineTool`、pre/post waterfall、ApprovalService、timeout wrapper、scheduler、Session append、client projection 与 spill/prune owner path 与 pinned source一致。
- typed `defineTool` validation 没有推广到 raw registration；pre-policy 被准确写成 composition-ordered waterfall，而不是 Article 06 / Lab 02 的 `Deny > Ask > Allow` vote merge。
- timeout/caller cancellation 被限定为 cooperative signal + drain + terminal classification；没有 hard-kill、rollback、remote quiescence、billing stop 或 run-level recovery 承诺。
- canonical value、model content、presentation meta、durable result 与 additional context 分账；actual client screen 与 real Provider delivery保持未执行。
- 大结果行为准确限定为 opt-in spill、bounded preview/locator 与 exact inline fallback；`semanticSummary:false` 没有被包装成 universal semantic summary。
- blocked Corepack preflight 被诚实披露：accepted experiment / Provider / tool-body network=`ZERO`，whole executor turn network attempt 不为零。
- BuildPilot `ToolExecutionReceipt` 明示为 `COURSE_PROPOSAL / DEFER`；没有启动 Article 37 决策矩阵、Part VII Architecture、ADR、Design v1 或 Runtime。

### Teaching, continuity, and scope

- 开篇先建立“为什么 schema 可见仍不足”的真实问题，再给两条链 / 五本账，最后落到固定 DSH owner path、五类负例和工程合同，符合原理篇方法，没有退化为 API 清单。
- 与 Article 05/06 的 Tool Call / Tool Runtime 抽象、Article 19 的 authority 分层、Article 33 的 scheduler dependency、Article 34 的 Session / Projection 边界总体衔接清楚。
- Article 36 的 run-level Cost / Compaction / Trace / Cancellation / Recovery 和 Article 37 的最终 extension decision matrix均保持 future boundary。

## Findings

### A35-R1-F01

- Finding ID：`A35-R1-F01`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`EVIDENCE`
- Location：`draft.md:54`
- Problem：正文写“运行证据只来自 production services、repo-owned MockAdapter……”，直接声称 production services 属于本轮运行证据；同句下一半及 Evidence Boundary 又明确没有 production Tool / side effect / safety evidence。这既是内部矛盾，也超过 raw fixture 能证明的环境层级。
- Supporting Evidence：`evidence.md:9`、`evidence.md:146-149` 与 `draft.md:665-677` 都把运行上限固定为 pinned temporary source-owned test instrumentation、MockAdapter 和 in-memory fixtures；Recovery command 没有生产服务、真实 Provider 或外部副作用。
- Why It Matters：该句位于开篇证据摘要，读者会据此错误升级整篇的 runtime tier；后文 limitation 不能自动撤销开篇的直接 overclaim。
- Required Disposition：只改 `draft.md:54`，把来源精确写成“临时 source-owned test instrumentation 中组合的 pinned DSH runtime components、repo-owned MockAdapter 与 in-memory Tool / approval / spill fixtures”，并明确 production service/deployment=`NOT RUN`。不得改 raw evidence 或提升 Claim。

### A35-R1-F02

- Finding ID：`A35-R1-F02`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`EVIDENCE`
- Location：`evidence.md:22-142, 151-163`
- Problem：文档把 `35-E02—E11` 的旧卡明确保留为 historical preliminary cards，却只用一张四列表格给出最终 disposition。最终 source Evidence Cards 本身没有逐卡记录统一合同要求的 repository/tag/full commit、file、symbol、call path、runtime command/exit、fixture/instrumentation、Trace path、counter-evidence/falsifier、Proves / Does Not Prove、limitation 与 BuildPilot implication。Source Map / Call Path 虽然另文完整，但不能替代“每张 Evidence Card 至少记录”的 closed card contract。
- Supporting Evidence：例如 `35-E02` 在 `evidence.md:34-42` 仍是 `BLOCKED_SOURCE_MAP` candidate，最终表 `evidence.md:154` 只说 owner/call path 已闭合，却不把最终 file/symbol/call path 写回卡；`35-E11` 在 `evidence.md:126-132` 仍称 Recovery `NOT_EXECUTED`，最终表 `evidence.md:162` 才说实验通过，但没有卡内 exact command、exit 与 raw Trace path。
- Why It Matters：当前 Draft 的事实大体可由散布的 artifact 复原，但 Part VI Audit 无法从一张 final card deterministic 地验证 source/runtime/proposal ceiling；这正是课程 Evidence Contract 要避免的“结论在一处、证据身份在另一处”。
- Required Disposition：只追加 final Evidence Card section，不改写 historical snapshots。至少为所有 `PINNED_SOURCE` 卡 `35-E02—E10` 写入完整最终字段；`35-E04—E11` 还要指向 exact command、exit、fixture/instrumentation 与 raw Trace。允许引用 `repository-map.md` / `call-path.md` 的稳定 anchor，但每张卡必须保留自己的 exact file/symbol/call path、Proves、Does Not Prove、counter-evidence 与 limitation。`35-E12` 继续是 `COURSE_PROPOSAL / DEFER`。

### A35-R1-F03

- Finding ID：`A35-R1-F03`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`COURSE`
- Location：`article-card.md:11-20`
- Problem：Article Card 仍是 `WORKSPACE_INIT` skeleton，明确说 problem framing、dependencies、claims、experiment、evidence status、teaching structure 全部 unfilled，且 Draft 未授权；但当前已进入 Review。Article Card Gate 要求的位置、依赖、reader change、non-goal、相邻文章边界与 Evidence / experiment type没有形成 approved durable input。
- Supporting Evidence：`production-workflow.md` Gate 0 要求“位置、依赖、读者变化和非目标”“相邻文章边界”“Evidence 与 Lab 类型”；Author contract要求读取 approved Article Card。当前这些内容只散落在 `research.md` / `outline.md`，未形成 current Article Card。
- Why It Matters：Review 无法把“读者变化与 Article Card 一致”作为 deterministic check；Part Audit也无法区分 canonical identity、研究后 scope 与 Author 自己形成的 teaching thesis。
- Required Disposition：由 Master 基于已经批准的 canonical / Research / Evidence 做 mechanical current-card reconciliation，只补 Problem Space、Required Questions、dependencies、reader change、non-goals、Source Mode / required experiment、fixed baseline 与 current evidence boundary；不得新增 Claim、改 canonical 或预写 Article 36/37。

### A35-R1-F04

- Finding ID：`A35-R1-F04`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`COURSE`
- Location：`subagent-trace.md` after `wr-a35-evidence-gate`；`README.md:8-25`；`course-run-state.md:13-46, 76`
- Problem：durable transaction surfaces仍停在 `WORKSPACE_INIT / RESEARCH`，Article README还断言 Draft、experiment、Source Map、Claim 与 Evidence Card不存在；run-state仍把下一动作写成 dispatch Research。Subagent Trace则缺少 initial Research disposition、accepted Research handoff、`OUTLINE` 与 `AUTHOR_DRAFT` worker envelopes / Master artifact validation。当前 Review 因此没有与磁盘一致的 closed continuation chain。
- Supporting Evidence：现有 `draft.md`、`outline.md`、final Evidence Merge 与 raw Recovery artifacts均已存在；`subagent-trace.md` 的最后记录却是 Evidence Gate，`next_allowed_gate=OUTLINE`。Course Factory要求 worker result validation -> artifact validation -> state transition -> next dispatch，且 repository artifact而非聊天上下文才是 durable handoff。
- Why It Matters：Article 30—34 continuation audit刚刚修复的正是 deterministic record / current-state parity问题；若 Article 35 在 commit 前留下同类缺口，Review/PUBLISH authority只能从隐藏上下文推断，不能由 repository独立恢复。
- Required Disposition：Master只使用真实收到且 schema-valid 的 envelope补齐缺失 handoff与 deterministic artifact-validation/state-transition records；任何没有有效 envelope 的 execution必须保留为 `MISSING / INTERRUPTED / NOT_PROVABLE` truthful disposition，不得 retrospective fake PASS。随后把 Article README、run-state、status/course README（如 canonical state contract要求）收敛到同一 current Review/Revision boundary。不得让 Revision Worker伪造 Master或旧 worker结果。

### A35-R1-F05

- Finding ID：`A35-R1-F05`
- Severity：`MINOR`
- Status：`OPEN`
- Category：`TECHNICAL`
- Location：`draft.md:183-189`
- Problem：“Provider 只站在模型调用边界……不拥有……外部副作用”缺少 fixed DSH native client-tool scope，容易被读成全局定义；课程 Glossary 和 Article 05 已明确 Provider也可能提供 built-in/server-executed tools 或基础设施能力。
- Supporting Evidence：Glossary 的 Provider 定义包含“提供模型、工具或基础设施服务”；Article 05 明确 Provider-managed / server-executed tools反驳“所有 Tool都由本地应用执行”。本篇 pinned source只闭合 DSH native client-tool path，且没有 real Provider run。
- Why It Matters：文章主线要求“不把 Provider 当 Tool”，但不能通过反向绝对化把所有 Provider-owned tool execution排除出课程术语；否则会造成跨篇定义漂移。
- Required Disposition：仅收窄该段语域为“在本文闭合的 DSH native client-tool path 中，Provider边界不拥有本地 callback”，并补一句这不外推到 Provider-managed built-in/server-executed tools。无需扩展 Provider 教程。

## Finding summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `4` | `A35-R1-F01`—`A35-R1-F04` |
| MINOR | `1` | `A35-R1-F05` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`5`** | **REVISION REQUIRED** |

这些 Finding 都可使用当前 pinned source、现有 raw evidence 与当前 transaction context做 bounded repair；不需要新 Research、Provider、网络、实验、Lab 07、baseline migration或 human decision。它们不是暂停理由。

## Five-dimensional score

| Dimension | Score | Review basis |
|---|---:|---|
| Technical Accuracy | `17 / 20` | 核心 source/call path准确；production-services overclaim 与 Provider语域需修。 |
| Evidence Discipline | `13 / 20` | raw Trace与失败历史很强，但 final card contract 和 durable transaction chain未闭合。 |
| Teaching Quality | `19 / 20` | problem -> model -> implementation -> boundary主线完整，五类负例服务于工程判断。 |
| Engineering Transfer | `18 / 20` | registry/execution/projection contracts可迁移，并保持 Proposal；需修正卡片与 owner receipts。 |
| Readability & Compression | `18 / 20` | 长文仍能靠两条链、五本账与最短结论收束；开篇证据句存在关键歧义。 |
| **Total** | **`85 / 100`** | **内容主体可修，但当前不满足 Final Gate。** |

## Gate decision

- Review Decision：`FAIL / REVISION REQUIRED`。
- Open Findings：`5`；`0 BLOCKER / 4 MAJOR / 1 MINOR`。
- Next allowed gate：`REVISION`，随后必须由 fresh Reviewer执行 `REVIEW_RECHECK`。
- 不返回新实验或 baseline migration，不授权 Publisher，不触碰 Article 36+。

## Master reconciliation disposition｜Cycle 1

- `A35-R1-F03`：`READY_FOR_RECHECK`。Master 基于已批准的 canonical、Research、Evidence Gate 与 accepted trace，机械补齐 `article-card.md` 的 Problem Space、Required Questions、dependencies、reader change、non-goals、Source Mode、固定 baseline、required experiment 与 current evidence boundary；没有新增 Claim 或预写 Article 36/37。
- `A35-R1-F04`：`READY_FOR_RECHECK`。Master 在 `subagent-trace.md` 中保存真实收到的 Research retry、Author、Review 与 Revision envelope，并为首次中断且无有效 envelope 的 Research attempt 保留 `INTERRUPTED / ENVELOPE MISSING / AUTHORITY NOT_PROVABLE`，没有 retrospective PASS。Article README、run-state、status 与 course README 已收敛到 `REVIEW_RECHECK / active NONE / 5 findings READY_FOR_RECHECK`。
- Master 未关闭任何 Finding。`A35-R1-F01—F05` 全部等待 fresh Reviewer 独立复核。

## Revision Cycle 1 disposition｜Revision Worker

> Role：`REVISION_WORKER / FRESH CONTEXT`
>
> Scope：仅 `A35-R1-F01`、`A35-R1-F02`、`A35-R1-F05`；`A35-R1-F03/F04` 保持 Master-owned。
>
> Gate result：`REVISION PASS / READY_FOR_RECHECK FOR F01, F02, F05 ONLY`

| Finding | Disposition | Exact changed location | Recheck boundary |
|---|---|---|---|
| `A35-R1-F01` | `READY_FOR_RECHECK` | `draft.md` 开篇 Evidence 摘要（原 review location `draft.md:54`） | 改为“临时 source-owned test instrumentation 中组合的 pinned DSH runtime components + repo-owned MockAdapter + in-memory fixtures”，并明记 production service / deployment=`NOT RUN`；未提升证据等级。 |
| `A35-R1-F02` | `READY_FOR_RECHECK` | `evidence.md` 新增 `Final Evidence Cards｜post-Recovery deterministic record`，其中 `Final Evidence 35-E02—E12` | 历史 preliminary cards 原样保留；final cards 逐卡补齐 fixed identity、source/symbol/call path、experiment command/exit（E04—E11）、fixture/raw paths、falsifier、Proves/Does Not Prove、status 与 bounded BuildPilot implication；E12 仍为 `COURSE_PROPOSAL / DEFER`。 |
| `A35-R1-F05` | `READY_FOR_RECHECK` | `draft.md` §5 Provider 边界段（原 review location `draft.md:183-189`） | 只对本文闭合的 DSH native client-tool path 作结论，显式不外推至 Provider-managed built-in / server-executed tools。 |
| `A35-R1-F03` | `OPEN / MASTER PENDING` | `article-card.md` | Revision Worker 未修改、未处置；等待 Master mechanical reconciliation。 |
| `A35-R1-F04` | `OPEN / MASTER PENDING` | `subagent-trace.md`、Article/course current-state surfaces | Revision Worker 未修改、未处置；等待 Master truthful deterministic record / state reconciliation。 |

本轮没有修改 Source Map、Call Path、Research、raw evidence、experiment design/observation、Outline、Article Card、README/Factory state、Published Content、Article 36+、Labs 或 DSH baseline；也没有新增 Research 或 runtime Claim。三个 bounded dispositions 需由 fresh Reviewer 复核，不代表 F03/F04 或 Final Gate 通过。

## Review Recheck｜Cycle 1

> Role：`REVIEWER / FRESH CONTEXT`
>
> Gate：`REVIEW_RECHECK`
>
> Recheck Date：`2026-08-30 (Asia/Shanghai)`
>
> Decision：`FAIL / ONE MAJOR REMAINS`

### Independence and fresh verification

- Reviewer：`/root/a35_reviewer_recheck_cycle1`。本轮只读取 repository artifacts、canonical contracts、pinned source 与 raw evidence；没有读取 Author / Revision Worker 的隐藏推理或 self-score。
- 唯一写入是本 `review.md` 的 Cycle 1 Recheck。Draft、Evidence、Article Card、Article README、Subagent Trace、global state、raw evidence、Published Content、external fixture 与 Article 36+ 均未修改。
- Fresh identities：Draft=`38999 bytes / 737 lines / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`；Evidence=`44911 bytes / 330 lines / SHA-256 1614DF93CF2BA79FCB79CAAA1F64F3AFA19ADC7CC88B3F628E557C03DC9F475C`；Article Card=`4201 bytes / 58 lines / SHA-256 698167FB26B976E319C8C002C399FE89CBE11DDEDB847377748F22D00D9CAD32`；Subagent Trace=`16213 bytes / 346 lines / SHA-256 3B830CF206E4F2E11539F2E034F508E379B1EE4B6082B5FEAE81FD393A7EC651`。
- Recovery integrity fresh check：manifest `9 / 9` entries bytes/hash match；JSONL=`13` records，distribution=`3 / 3 / 2 / 2 / 3`，callId=`13 / 13 unique`，required top-level/nested fields与每条 Session/next-history correlation=`PASS`。Cycle 0 与 Recovery Attempt 1 仍分别为 `NOT_ACCEPTED`，没有被最终 capture 覆盖。
- Pinned fixture fresh check：`HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；status、staged/unstaged `diff --check` 均无输出；temporary recovery test 不存在。
- Link / future-asset boundary：Draft `4 / 4 relref` 指向现有 Article 34 / series index，shortcode 使用 ASCII quotes；Published Article 35=`ABSENT`，Part VI Audit=`ABSENT`，Article 36—44 workspace/content/static hits=`0`。
- Closed-key parser：`subagent-trace.md` 中 `14 / 14` YAML `worker_result` blocks 都具备 exact 11 fields，missing/unknown fields=`0`。这只证明 key-level shape，不替代 role enum、result mapping、Gate sequence 与 Master validation record。

### Finding recheck

#### A35-R1-F01 — CLOSED

- Direct evidence：`draft.md:54` 已把 runtime tier 收窄为 temporary source-owned instrumentation 中组合的 pinned DSH components、repo-owned `MockAdapter` 与 in-memory fixtures，并明确 production service / deployment=`NOT RUN`。
- Regression check：同句及 Evidence Boundary 继续排除 real Provider、production Tool / side effect、actual client UI 与 production safety；Claim strength 未升级。

#### A35-R1-F02 — CLOSED

- Direct evidence：`evidence.md:178-326` 新增并逐卡保存 `Final Evidence 35-E02—E12`。E02—E10 均保留 repository/tag/full commit、exact file/symbol/anchor、call path、observation、falsifier、Proves / Does Not Prove、limitations 与 bounded BuildPilot implication；E04—E11 另有 exact accepted command/exit、fixture/instrumentation 与 raw Trace paths。
- Regression check：historical preliminary cards 原样保留旧 `BLOCKED_SOURCE_MAP / PARTIAL / NOT_EXECUTED` 时间语义；E12 仍为 `COURSE_PROPOSAL / DEFER`，没有变成 DSH 内置事实或 Part VII design。

#### A35-R1-F03 — CLOSED

- Direct evidence：`article-card.md` 已包含 canonical identity、Problem Space、7 个 Required Questions、dependencies / reader change、non-goals、Source Mode / required experiment、fixed repository/tag/full commit、accepted/failure receipts、teaching structure与 current transaction boundary。
- Regression check：canonical title / Part / weight / required identity一致；没有新增 Article 36/37 Claim、Part VII Architecture 或 BuildPilot Runtime。

#### A35-R1-F04 — OPEN / MAJOR

- Direct evidence passed：Article README、`course-run-state.md`、`status.md` 与 course README 已统一到 `Article35 / REVIEW_RECHECK / active_worker NONE / five findings READY_FOR_RECHECK`；Article 35 authorization仍只到 `END_ARTICLE`；Article 36—37=`NOT_STARTED / ZERO ASSETS`，Article 38—44=`FORBIDDEN / ZERO ASSETS`。首次 Research attempt也明确保留为 `MISSING / INTERRUPTED / NOT_PROVABLE`，fresh retry envelope具备 exact 11 fields。
- Remaining problem 1：原 Finding 明确要求补齐 `OUTLINE` 与 `AUTHOR_DRAFT` handoff。当前 Trace 只有一个 `gate: AUTHOR_DRAFT` envelope，且其中把 `outline.md` 列为 modified；`gate: OUTLINE` envelope count=`0`，也没有把缺失的 OUTLINE execution truthful disposition 为 `MISSING / INVALID / NOT_PROVABLE`。文件存在或后续 Author envelope不能反向证明独立 Outline Gate 已发生。
- Remaining problem 2：`subagent-contracts.md` 要求每条 durable record 同时保存 execution/task ID、bounded task brief snapshot、raw envelope、Master validation result 与 validation time。新补的 Research retry、Author、Review、Revision 与 Master reconciliation sections只有标题和 raw envelope；没有对应 deterministic artifact/Allowed-Writes/Gate validation record，因此当前 continuation chain 仍需从 envelope 自报推断。
- Remaining problem 3：key-level parser不会检查 enum / transition。canonical closed role enum不包含现有 `role: SOURCE_INVESTIGATOR`，且现有 `REVIEW -> REVISION` record使用 `status: FAIL / blocker: NONE`，与 common result mapping冻结的 `PASS / true / REVISION / NONE` 不一致。若本次 human prompt意图扩展 role或覆盖 mapping，必须先形成明确、可审计的 contract interpretation；当前 repository里没有该 alias / override。
- Why it remains MAJOR：该缺口位于当前尚未提交的 Article 35 transaction，本轮如果直接关闭，就会再次把 artifact existence 或 hidden orchestration context当 continuation authority，违反 F04 的原始 Required Disposition。
- Required Disposition：Master不得改写或补全旧 raw payload。把无有效 authority 的 event 明确登记为 `MISSING / INVALID / NOT_PROVABLE`，补齐 contract-required record metadata；对缺失的 OUTLINE Gate 使用新的 contract-valid execution重新建立 forward authority，或在 canonical schema与显式 Source Investigator role / Review mapping无法安全裁决时返回准确 `STATE_CONFLICT`。完成后再次 fresh recheck。不得借修复启动 Publisher、Article 36 或修改 raw evidence。

#### A35-R1-F05 — CLOSED

- Direct evidence：`draft.md:187` 已把结论限定为本文闭合的 DSH native client-tool path，并明确不外推 Provider-managed built-in / server-executed tools。
- Regression check：该表述与 Glossary 的 Provider 边界、Article 05 的 provider-managed execution反例一致；没有真实 Provider runtime Claim。

### Recheck summary

| Finding | Severity | Recheck status |
|---|---|---|
| `A35-R1-F01` | MAJOR | `CLOSED` |
| `A35-R1-F02` | MAJOR | `CLOSED` |
| `A35-R1-F03` | MAJOR | `CLOSED` |
| `A35-R1-F04` | MAJOR | `OPEN` |
| `A35-R1-F05` | MINOR | `CLOSED` |

Open Findings：`1`；`0 BLOCKER / 1 MAJOR / 0 MINOR / 0 EDITORIAL`。

### Five-dimensional score

| Dimension | Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | F01/F05已收窄，pinned source与五类 Trace未见技术回归。 |
| Evidence Discipline | `14 / 20` | final cards与raw integrity已闭合；durable Gate authority仍缺 deterministic record。 |
| Teaching Quality | `19 / 20` | 问题空间、抽象模型、固定实现、负例与边界主线保持完整。 |
| Engineering Transfer | `19 / 20` | Registry / Execution / Projection contract可迁移且Proposal受控。 |
| Readability & Compression | `19 / 20` | 证据层级与Provider语域已消歧，长文仍可由两条链/五本账收束。 |
| **Total** | **`91 / 100`** | **知识内容可进入Final候选，但F04的transaction authority仍阻断Final Gate。** |

### Recheck Gate decision

- Decision：`FAIL / REVISION REQUIRED`。
- Next allowed gate：`REVISION`。
- 只允许修复 `A35-R1-F04` 的 durable record / contract interpretation；F01/F02/F03/F05保持关闭，禁止借机改正文主线、Evidence strength、raw observations或 future Articles。

## Master A35-R1-F04 Revision disposition｜Cycle 2

- `A35-R1-F04`：`READY_FOR_RECHECK`，不是自关闭。
- Master 在 `subagent-trace.md` 追加 deterministic validation registry：首次 Research attempt继续为`MISSING / INTERRUPTED / NOT_PROVABLE`；execution identity缺失的Research retry不再作为current authority；历史`SOURCE_INVESTIGATOR` envelope登记`INVALID_ROLE_ENUM`；两份Reviewer `status: FAIL -> REVISION / blocker NONE`记录登记`INVALID_RESULT_MAPPING`；原Author记录不再被当作独立OUTLINE authority。
- Fresh current-time read-only executions重新建立`RESEARCH -> SOURCE_MAP -> OUTLINE -> AUTHOR_DRAFT` authority。Source investigation task的envelope role按closed enum归一为`RESEARCHER`；OUTLINE与AUTHOR_DRAFT分别返回独立合规结果。每条新记录均保存execution ID、bounded task、allowed writes、raw envelope、Master validation和validation time。
- 原始Finding内容、Cycle0/Attempt1失败、raw实验、Draft、Evidence strength与future boundary均未修改。下一步仅允许fresh `REVIEW_RECHECK`关闭或保留F04。

## Master FINAL_GATE repair disposition｜A35-FG-F01

- `A35-FG-F01`：`READY_FOR_FINAL_GATE_RECHECK`，不是自关闭。
- Master 只把course README的“当前 Article 35”与`content/`资产边界两处 stale `REVIEW_RECHECK candidate`改为当前真实的`FINAL_GATE candidate`；没有修改Draft、Evidence、raw Trace、Article Card、Publisher目标、navigation或future Article。
- 当前所有状态表面继续指向`Article35 / FINAL_GATE / active NONE`。下一步只允许fresh FINAL_GATE recheck。

## Master FINAL_GATE metadata repair disposition｜A35-FG-F02

- `A35-FG-F01`：`CLOSED`，保持不变。
- `A35-FG-F02`：`READY_FOR_FINAL_GATE_RECHECK`，不是自关闭。
- Master 为`wr-a35-final-gate-revision1`补齐execution ID、bounded task、allowed writes、Master validation result与validation time；随后建立自始完整的`wr-a35-final-gate-revision2`并让run-state引用它。
- 本轮只改A35 transaction metadata/current projection，没有修改Draft、Evidence、raw observations、Article Card、navigation、Published target或future Article资产。下一步只允许fresh FINAL_GATE recheck。

```yaml
worker_result:
  role: REVIEWER
  article: "35"
  gate: REVIEW_RECHECK
  execution_type: REAL_SUBAGENT
  status: FAIL
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/review.md
  gate_completed: true
  next_allowed_gate: REVISION
  blocker: NONE
  notes:
    - "A35-R1-F01, F02, F03, and F05 are CLOSED by direct evidence; A35-R1-F04 remains OPEN / MAJOR."
    - "Raw recovery integrity, pinned fixture cleanliness, relrefs, current-state parity, and Article 36-44 zero-assets checks pass."
    - "The remaining scope is the missing OUTLINE authority and deterministic trace-record validation/closed-schema interpretation; no content, evidence, experiment, or future-Article rewrite is authorized."
```

## Review Recheck｜Cycle 2

> Role：`REVIEWER / FRESH CONTEXT`
>
> Gate：`REVIEW_RECHECK`
>
> Recheck Date：`2026-08-30 (Asia/Shanghai)`
>
> Decision：`PASS / A35-R1-F04 CLOSED`

### Independence and bounded scope

- Reviewer：`/root/a35_reviewer_recheck_cycle2`。本轮重新读取 canonical、Course Factory / Article workflow / eight-role contracts、review checklist 与 Article 35 全部 current artifacts；只复核 `A35-R1-F04` 和 content/raw/future regressions，没有读取 Author、Revision Worker 或 Master 的隐藏推理与 self-score。
- 唯一写入是本 `review.md` 的 Cycle 2 Recheck。Draft、Evidence、Article Card、README、Subagent Trace、global state、raw evidence、Published Content、external fixture 与 Article 36+ 均未修改。
- 本轮使用两层 deterministic check：先解析 closed 11-field shape，再独立检查 role / status / execution-type enum、common result mapping 与 Gate transition；key-level PASS 不被当作 semantic PASS。

### Fresh deterministic verification

- `subagent-trace.md` 中 `19 / 19` YAML `worker_result` blocks 都具有 exact 11 fields；missing / unknown / duplicate fields=`0`。
- 历史无效记录继续原样可见且不提供 current authority：首次 Research attempt=`MISSING / INTERRUPTED / NOT_PROVABLE`；`wr-a35-source-map-retry1`=`INVALID_ROLE_ENUM`，因为 `SOURCE_INVESTIGATOR` 不在 closed role enum；`wr-a35-review-cycle1` 与 `review.md` 内 Cycle 1 Recheck 的 `status: FAIL -> REVISION / blocker: NONE` 均为 `INVALID_RESULT_MAPPING`。没有把这些 payload 改写成 retrospective PASS。
- Fresh `wr-a35-research-current-revalidation-cycle2` 完整保存 execution ID、bounded task、allowed writes、raw envelope、Master validation 与 validation time；语义映射为 `RESEARCHER / RESEARCH / PASS / true / SOURCE_MAP / NONE`，合法。
- Fresh `wr-a35-source-map-current-revalidation-cycle2` 同样保存全部 record metadata；envelope 使用 canonical `RESEARCHER` role，语义映射为 `SOURCE_MAP / PASS / true / EXPERIMENT_DESIGN / NONE`，合法。该记录只建立 current source authority，不声称历史 `SOURCE_INVESTIGATOR` payload 合法。
- Fresh `wr-a35-outline-current-revalidation-cycle2` 与 `wr-a35-author-draft-current-revalidation-cycle2` 是两个独立 Gate record，分别保存完整 metadata 与 raw envelope；语义映射依次为 `AUTHOR / OUTLINE / PASS / true / AUTHOR_DRAFT / NONE` 和 `AUTHOR / AUTHOR_DRAFT / PASS / true / REVIEW / NONE`。current Outline authority不再由后续 Draft 文件存在性反推。
- Cycle 0 BLOCKED receipt、Recovery Design、Recovery Execute、Evidence Merge、Evidence Gate与Revision receipt继续保持各自原始 status、scope和Master validation；fresh revalidation没有覆盖失败历史，也没有提升 Evidence strength。

### Current-state and regression checks

- Article README、`course-run-state.md`、`status.md` 与 course README一致指向 `RUNNING / Article 35 / REVIEW_RECHECK / Cycle 2 / active_worker NONE`；Article authorization只到`END_ARTICLE`，blocker=`NONE`，next action=`DISPATCH_ARTICLE_35_REVIEW_RECHECK`。
- Draft=`38999 bytes / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`；Evidence=`44911 bytes / SHA-256 1614DF93CF2BA79FCB79CAAA1F64F3AFA19ADC7CC88B3F628E557C03DC9F475C`；Outline=`15864 bytes / SHA-256 7E1350F8D617E9BF59625DE3B8E81F552BDD2771210A3D6B38868D16065C661E`。它们与 Cycle 1 recheck identities一致；F01/F02/F03/F05 closure没有被 Cycle 2 authority repair回退。
- Cycle 0 manifest fresh recomputation=`13 / 13 PASS`；Recovery manifest=`9 / 9 PASS`。Recovery JSONL=`13` records / `13` unique callIds / `0` parse errors / required top-level fields完整，case distribution=`3 / 3 / 2 / 2 / 3`，Session与next-history correlation failures=`0`。
- External fixture fresh read：`HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；status、staged/unstaged `diff --check`均无输出。Cycle 0与Recovery Attempt 1仍为`NOT_ACCEPTED`，没有被最终 capture覆盖。
- Repository `git diff --check`无 whitespace error；只报告既有 LF/CRLF warning。Article 35 Published Content=`ABSENT`，Part VI Audit=`ABSENT`，Article 36—44 workspace/content/static hits=`0`。

### A35-R1-F04 — CLOSED

- 原问题一已关闭：缺失的 historical OUTLINE authority没有被伪造；current `OUTLINE` 与 `AUTHOR_DRAFT` 由分立、schema-valid、semantically valid 的 fresh records建立。
- 原问题二已关闭：fresh Research、Source Map、Outline与Author Draft records均具有 execution/task identity、bounded brief、Allowed Writes、raw envelope、Master artifact/write/Gate validation与validation time，不再依赖 envelope自报或 hidden context。
- 原问题三已关闭：旧非法 role与非法 Review mapping仍明确为invalid/non-authoritative；current source investigation使用canonical `RESEARCHER` envelope role，current normal forward results均符合 common mapping。旧 missing/invalid disposition没有被覆盖成 retrospective PASS。
- Regression result：current-state parity、Evidence/Trace integrity、closed F01/F02/F03/F05、pinned baseline、no Published Content与Article 36—44 zero-assets guard全部保持成立。

### Finding and score summary

| Finding | Severity | Cycle 2 status |
|---|---|---|
| `A35-R1-F01` | MAJOR | `CLOSED / NO REGRESSION` |
| `A35-R1-F02` | MAJOR | `CLOSED / NO REGRESSION` |
| `A35-R1-F03` | MAJOR | `CLOSED / NO REGRESSION` |
| `A35-R1-F04` | MAJOR | `CLOSED` |
| `A35-R1-F05` | MINOR | `CLOSED / NO REGRESSION` |

Open Findings：`0`；`0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`。

| Dimension | Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | pinned source、typed/raw、Provider语域与timeout/cancel边界无回归。 |
| Evidence Discipline | `19 / 20` | raw Trace、final cards、失败历史与current durable authority均可独立复核。 |
| Teaching Quality | `19 / 20` | 问题空间、抽象模型、pinned实现、负例与边界主线保持完整。 |
| Engineering Transfer | `19 / 20` | Registry / Execution / Projection contracts可迁移，Proposal仍受控。 |
| Readability & Compression | `19 / 20` | 双链、五本账和Evidence Boundary继续提供稳定压缩。 |
| **Total** | **`96 / 100`** | **满足现行课程Review质量基线。** |

### Recheck Gate decision

- Decision：`PASS`。
- `A35-R1-F04 CLOSED`；全部 Findings=`0 OPEN`。
- Next allowed gate：`FINAL_GATE`。
- 本结果不等于 Final Gate、Publish、Build、completion commit、push、remote verification或`END_ARTICLE`。

```yaml
worker_result:
  role: REVIEWER
  article: "35"
  gate: REVIEW_RECHECK
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/review.md
  gate_completed: true
  next_allowed_gate: FINAL_GATE
  blocker: NONE
  notes:
    - "A35-R1-F04 is CLOSED; all five Cycle 1 findings are closed with zero open findings."
    - "Fresh Research, Source Map, OUTLINE, and AUTHOR_DRAFT records pass exact-key, metadata, role-enum, result-mapping, and transition checks; historical missing/invalid records remain visible and non-authoritative."
    - "Draft/Evidence/Outline identities, both raw manifests, 13-record trace correlation, pinned-fixture cleanliness, current-state parity, and Article 36-44 zero-assets guards pass fresh regression checks."
```

## Final Gate｜Cycle 2 Attempt 1

> Role：`REVIEWER / FRESH READ-ONLY FINAL GATE`
>
> Gate：`FINAL_GATE`
>
> Gate Date：`2026-08-30 (Asia/Shanghai)`
>
> Decision：`REVISION REQUIRED / NOT YET ELIGIBLE FOR PUBLISH`

### Fresh final checks

- Canonical / identity：canonical Article `35` title、Part VI、weight `L`、required Source Mode 与 Article Card一致；Draft H1精确为`Tool Registry 与 Tool Execution Pipeline`。
- Draft identity：`38999 bytes / 737 physical lines / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`，与 Review Recheck冻结身份一致。
- Evidence：`35-C01—C12 = 12 / 12`；`35-E01`保持`DOC_CONFIRMED`，post-Recovery final section含`35-E02—E12 = 11 / 11`，final status中`BLOCKED=0`；source / runtime / experiment / inference / proposal继续分账。
- Required traces：Cycle 0 manifest fresh hash=`13 / 13 PASS`；Recovery manifest=`9 / 9 PASS`；JSONL=`13 records / 13 unique callIds / 0 schema missing / 0 Session-next correlation failure / 3-3-2-2-3 distribution`。
- Preserved failures：Cycle 0=`22 passed / 0 failed / NOT_ACCEPTED`，Recovery Attempt 1=`exit 0 / selected 0 of 5 / NOT_ACCEPTED`；首次 Research missing、历史非法 role与非法 Reviewer result mapping仍明确不可作为authority。
- Source / safety boundary：Repository Map与Call Path保留exact owner、symbol、call path、counter-evidence和`NOT_PROVABLE`；Draft继续明示Developer Preview、未安全审计、production service/deployment=`NOT RUN`，不声称real Provider、production Tool/side effect、actual UI、hard kill/rollback、universal summary或BuildPilot Runtime。
- Links / pinned source：Draft `4 / 4 relref`目标存在、curly-quote shortcode=`0`；`8 / 8` pinned-commit GitHub blob paths在fixed fixture中存在。
- Fixture / future guard：external fixture `HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`，status与staged/unstaged diff checks为空；Article 35 Published Content=`ABSENT`，Part VI Audit=`ABSENT`，Article 36—44 workspace/content/static hits=`0`。
- Publisher mechanical surface otherwise ready：target content path应为`content/ai-empowerment/agent-engineering-35-dsh-tool-registry-execution-pipeline.md`且当前不存在；Article 34 Published Content存在；series index恰有一个Article 35 planned row和零个published row。Final Gate通过后，Publisher allowlist只能包含新建该Published Content、对Article 34追加Article 35 next-navigation、把series index的Article 35单行机械改为published relref，以及写Article 35 README publication evidence；不得修改Draft/Evidence/raw、global state、canonical或future assets。

### A35-FG-F01

- Finding ID：`A35-FG-F01`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`COURSE`
- Location：`docs/agent-engineering-course/README.md:99,159`
- Problem：Master已把 run-state、status、Article README、Article Card及course README顶部/Factory摘要推进到`FINAL_GATE candidate`，但同一course README的“当前 Article 35”与content资产边界仍写`REVIEW_RECHECK candidate`。这是两个current-only projection与当前权威Gate的直接冲突，不是历史记录。
- Supporting Evidence：`course-run-state.md`当前为`current_gate: FINAL_GATE / next_action: DISPATCH_ARTICLE_35_FINAL_GATE`；`status.md`顶部与Article 35行、Article README和Article Card都已记录Cycle 2 PASS / F01—F05 CLOSED / FINAL_GATE。course README:99与:159仍使用旧current wording。
- Why It Matters：Final Gate若在同一current surface自相矛盾时授权Publisher，会重新引入Article 35已经专门修复过的continuation-state drift；发布后恢复者无法只依赖durable repository判断真实边界。
- Required Disposition：Master只机械修改`docs/agent-engineering-course/README.md`这两处current-only wording，使其与`FINAL_GATE / Cycle 2 PASS / 0 OPEN / Published Content absent`一致；不得改历史记录、Draft、Evidence、raw、Article 36+、canonical或Publisher assets。修复后执行fresh Final Gate recheck。

### Final Gate decision

- Prior Review Findings：`A35-R1-F01—F05 CLOSED / 0 OPEN`，没有内容或Evidence回归。
- New Final Gate Finding：`A35-FG-F01 MAJOR / OPEN / mechanically repairable`。
- Publisher authorization：`NOT GRANTED`；next route=`REVISION -> FINAL_GATE recheck`。
- 这不是Evidence、实验、baseline、publication或human-decision blocker；按common result mapping，本次Reviewer execution完整返回Finding，因此worker envelope为`PASS / true / REVISION / NONE`，不把可修复Finding伪装成`FAIL / BLOCKED`。

```yaml
worker_result:
  role: REVIEWER
  article: "35"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/review.md
  gate_completed: true
  next_allowed_gate: REVISION
  blocker: NONE
  notes:
    - "A35-R1-F01 through F05 remain CLOSED; draft identity, 12 final dispositions, five accepted traces, preserved failures/invalid history, Developer Preview limits, links, fixture, and future-asset guards pass."
    - "A35-FG-F01 is OPEN / MAJOR because course README lines 99 and 159 still say REVIEW_RECHECK candidate while all authoritative current surfaces say FINAL_GATE candidate."
    - "Only a two-location Master mechanical current-state repair is required; Publisher is not authorized until a fresh FINAL_GATE recheck."
```

## Final Gate Recheck｜Cycle 2 Attempt 2

> Role：`REVIEWER / FRESH READ-ONLY FINAL GATE RECHECK`
>
> Gate：`FINAL_GATE`
>
> Recheck Date：`2026-08-30 (Asia/Shanghai)`
>
> Decision：`REVISION REQUIRED / NOT YET ELIGIBLE FOR PUBLISH`

### Independence and exact scope

- Reviewer：`/root/a35_final_gate_recheck`。本轮重新读取 repository instructions、canonical、Course Factory / Article workflow / eight-role contracts、review checklist 与 Article 35 current artifacts；只复核 `A35-FG-F01` 直接闭合和 Final Gate 全量回归，没有读取 Author、Revision Worker 或 Master 的隐藏推理与 self-score。
- 唯一写入是本 `review.md` 的 Final Gate Recheck。Draft、Evidence、raw Trace、Article Card、Article/course README、Subagent Trace、global state、Published Content、external fixture 与 Article 36+ 均未修改。
- 本轮把 closed 11-field envelope shape 与 role enum、result mapping、Gate transition、record metadata 分开检查；exact-key PASS 不替代 semantic / authority PASS。

### A35-FG-F01 — CLOSED

- course README“当前 Article 35”位置现为 `FINAL_GATE candidate`，`content/` 资产边界也现为 `FINAL_GATE candidate / Published Content absent`；原 Finding 指定的两处 stale `REVIEW_RECHECK candidate` 已消失。
- `course-run-state.md`、`status.md`、Article README、Article Card 与 course README 当前投影一致指向 `RUNNING / Article 35 / FINAL_GATE / active NONE / blocker NONE`；Review Cycle 2=`PASS / A35-R1-F01—F05 CLOSED / 0 OPEN`。
- Master disposition仍把 `A35-FG-F01` 标为 `READY_FOR_FINAL_GATE_RECHECK` 而非自关闭；本次 fresh Reviewer 以 current repository truth 独立关闭该 Finding。

### Fresh content, evidence and experiment regression checks

- Draft=`38999 bytes / 737 physical lines / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`；Evidence=`44911 bytes / 330 lines / SHA-256 1614DF93CF2BA79FCB79CAAA1F64F3AFA19ADC7CC88B3F628E557C03DC9F475C`；Outline=`15864 bytes / 333 lines / SHA-256 7E1350F8D617E9BF59625DE3B8E81F552BDD2771210A3D6B38868D16065C661E`；这些身份与 Cycle 2 Review Recheck / Final Gate Attempt 1 一致。当前 Subagent Trace（本轮未修改）=`29149 bytes / 578 lines / SHA-256 978A4B82798ABC67279AC329F0510778D3A13C3160495BE0C8698A92D503A01F`。
- Draft Claim table=`35-C01—C12 / 12 unique`；Evidence final cards=`35-E01—E12 / 12 unique`，final `BLOCKED=0`；Official Doc、Pinned Source、Runtime Observation、Experiment、Inference 与 Course Proposal 继续分账。
- Cycle 0 manifest fresh recomputation=`13 / 13 bytes+SHA PASS`；Recovery manifest=`9 / 9 bytes+SHA PASS`。Recovery JSONL=`13 records / 13 unique callIds / 0 parse error / 0 required-field miss / 0 Session-next correlation failure`，case distribution=`3 / 3 / 2 / 2 / 3`。
- Cycle 0=`22 passed / 0 failed / NOT_ACCEPTED`、Recovery Attempt 1=`exit 0 / selected 0 of 5 / NOT_ACCEPTED`继续可见；首次 Research=`MISSING / INTERRUPTED / NOT_PROVABLE`、历史非法 `SOURCE_INVESTIGATOR` role 和非法 Reviewer `FAIL -> REVISION / blocker NONE` mapping 继续明确为 non-authoritative，没有 retrospective PASS。
- External fixture fresh read：`HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；status、staged / unstaged `diff --check`均无输出。Draft `4 / 4 relref`目标存在、curly-quote shortcode=`0`；`8 / 8` pinned-commit GitHub blob paths在fixed fixture中存在。
- `A35-R1-F01—F05`保持`CLOSED / 0 OPEN`；Developer Preview / 未安全审计、typed-vs-raw、Provider边界、cooperative timeout/cancel、no rollback、optional spill/no universal summary、actual UI/real Provider/production side effect未运行等边界均无回归。

### Closed-schema and transition verification

- `subagent-trace.md` 当前 `22 / 22` `worker_result` blocks具有 exact 11 fields；未知、缺失或重复 top-level fields=`0`。
- `20 / 22` records同时通过 role enum与common result mapping。余下两条是此前已登记且保持 non-authoritative 的历史失效记录：`wr-a35-source-map-retry1=INVALID_ROLE_ENUM`；`wr-a35-review-cycle1=INVALID_RESULT_MAPPING`。fresh Research / Source Map / Outline / Author Draft / Review Recheck / Final Gate Attempt 1 与本次 Master Revision envelope 的 role、status、gate_completed、next Gate和blocker组合均合法。
- `wr-a35-final-gate-revision1` 的 raw envelope本身是 exact 11-field、`MASTER_ORCHESTRATOR / REVISION / MASTER_DETERMINISTIC / PASS / true / FINAL_GATE / NONE`，其 transition shape合法；但 envelope shape不等于完整 durable record authority，见下方新 Finding。
- Repository `git diff --check` fresh exit=`0`，仅有 Git 的 LF/CRLF working-copy warning；Article 35 Published Content=`ABSENT`，Part VI Audit=`ABSENT`，Article 36—44 workspace/content/static assets=`0`。

### Publisher mechanical readiness check

- Target Published Content=`content/ai-empowerment/agent-engineering-35-dsh-tool-registry-execution-pipeline.md / ABSENT`；Article 34 Published Content存在；series index中Article 35=`1 planned row / 0 published relref`。
- 在 Final Gate 真正通过后，Publisher机械 allowlist仍只能包含：新建Article 35 Published Content；为Article 34追加Article 35 next-navigation；把series index唯一Article 35 planned row改为published relref；在Article 35 README写publication evidence。Publisher不得修改Draft、Evidence、raw、global state、canonical或future assets。
- 上述机械面本身可发布，但当前仍有一条 durable authority Finding，所以本轮不授权Publisher。

### A35-FG-F02

- Finding ID：`A35-FG-F02`
- Severity：`MAJOR`
- Status：`OPEN`
- Category：`COURSE`
- Location：`docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/subagent-trace.md:556-578`；`docs/agent-engineering-course/course-run-state.md:19-29`
- Problem：当前 `last_worker_result.result_ref` 指向 `#wr-a35-final-gate-revision1`，但该 durable record只有 raw 11-field envelope，没有合同要求的 execution / task ID、bounded task brief、Allowed Writes、Master validation result 与验证时间。`course-run-state.md`投影中的`execution_id: MASTER / artifact_verified: true / validation_status: PASS`不能替代 canonical raw record 的验证 metadata。
- Supporting Evidence：`subagent-contracts.md:111`要求 checkpoint 前每条 record同时保存 stable ID、execution/task ID、bounded brief、raw envelope、Master validation result与验证时间；`:115`要求 Master deterministic execution 也先序列化 envelope并用 repository truth、actual diff、Gate contract和state machine验证。当前 trace 的前一条`wr-a35-final-gate-attempt1`完整保存了这些 metadata，而`:556-578`的新 Master Revision record从heading直接进入YAML并在fence后结束。
- Why It Matters：这条 record是当前 Final Gate continuation authority 的直接 source。缺少 task/write/validation receipt时，fresh resolver只能知道Master自报了合法 envelope，不能审计“两处 current-only repair + review/trace/state disposition”是否真的经过 artifact、write-scope与Gate validation；这会回归已关闭`A35-R1-F04`要求的“envelope不能自证authority”边界。
- Required Disposition：Master只为`wr-a35-final-gate-revision1`补齐真实的 execution/task ID、bounded task brief、Allowed Writes、Master artifact/write/Gate/transition validation result和实际 validation time；不得改写其raw envelope、虚构历史时点或扩大修复范围。随后同步current-only disposition并执行fresh Final Gate recheck。不得修改Draft、Evidence、raw experiment、canonical、Publisher assets或Article 36+。

### Finding and score summary

| Finding | Severity | Attempt 2 status |
|---|---|---|
| `A35-R1-F01—F05` | MAJOR / MINOR | `CLOSED / NO REGRESSION` |
| `A35-FG-F01` | MAJOR | `CLOSED` |
| `A35-FG-F02` | MAJOR | `OPEN / MECHANICALLY REPAIRABLE` |

Open Findings：`1 MAJOR`；`0 BLOCKER / 1 MAJOR / 0 MINOR / 0 EDITORIAL`。

| Dimension | Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | pinned source、运行边界与五类负例无回归。 |
| Evidence Discipline | `18 / 20` | 内容/实验完整，但当前Master Revision authority record缺验证metadata。 |
| Teaching Quality | `19 / 20` | 问题空间、抽象双链/五账、具体source path与边界完整。 |
| Engineering Transfer | `19 / 20` | Registry / Execution / Projection合同与Proposal边界稳定。 |
| Readability & Compression | `19 / 20` | 结构和术语压缩无回归。 |
| **Total** | **`95 / 100`** | **知识内容已冻结，但当前不满足Final Gate的durable authority条件。** |

### Final Gate Recheck decision

- `A35-FG-F01 CLOSED`；所有Article内容、Evidence、Trace payload、fixture、link、Publisher mechanical surface与future guard回归通过。
- `A35-FG-F02 MAJOR / OPEN / mechanically repairable`；Publisher authorization=`NOT GRANTED`。
- Next route：`REVISION -> fresh FINAL_GATE recheck`。这不是Evidence、baseline、publication或human-decision blocker；按common result mapping，本次Reviewer完整返回Finding，因此envelope为`PASS / true / REVISION / NONE`。

```yaml
worker_result:
  role: REVIEWER
  article: "35"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/review.md
  gate_completed: true
  next_allowed_gate: REVISION
  blocker: NONE
  notes:
    - "A35-FG-F01 is CLOSED; draft/evidence identities, 12 claims/cards, five accepted traces, preserved failures/invalid history, pinned fixture, links, Publisher mechanical surface, and Article 36-44 zero-assets guards pass."
    - "A35-FG-F02 is OPEN / MAJOR because the current wr-a35-final-gate-revision1 record lacks execution/task, bounded brief, allowed-writes, Master validation, and validation-time metadata required for current durable authority."
    - "The repair is bounded to truthful record metadata and current-only disposition; Publisher remains unauthorized until a fresh FINAL_GATE recheck."
```

## Final Gate Recheck｜Cycle 2 Attempt 3

> Role：`REVIEWER / FRESH READ-ONLY FINAL GATE RECHECK`
>
> Gate：`FINAL_GATE`
>
> Recheck Date：`2026-08-30 (Asia/Shanghai)`
>
> Decision：`PASS / ELIGIBLE_FOR_PUBLISH`

### Independence and exact scope

- Reviewer：`/root/a35_final_gate_recheck2`。本轮只复核 `A35-FG-F02` 的直接闭合并重跑 Final Gate 全量回归；没有读取 Master 的隐藏推理或 self-score。
- 唯一写入是本 `review.md` 的 Attempt 3 记录。Draft、Evidence、raw observation、Article Card、Article/course README、Subagent Trace、global state、Published Content、external fixture 与 Article 36+ 均未修改。
- 本轮继续把 raw exact-11 envelope、record metadata、Master validation receipt 与 current projection 分开验证；任何单一层都不能替代其余层。

### A35-FG-F02 — CLOSED

- `wr-a35-final-gate-revision1` 现同时保存 stable record ID、`Execution ID: MASTER`、bounded task brief、4 项 exact Allowed Writes、raw exact-11 envelope、Master validation result=`PASS` 与 validation time=`2026-08-30T21:15:00+08:00`。其 raw envelope保持 `MASTER_ORCHESTRATOR / REVISION / MASTER_DETERMINISTIC / PASS / true / FINAL_GATE / NONE`，没有被改写成 retrospective worker result。
- `wr-a35-final-gate-revision2` 从创建时即保存 stable record ID、`Execution ID: MASTER`、bounded metadata-only brief、4 项 exact Allowed Writes、raw exact-11 envelope、Master validation result=`PASS` 与 validation time=`2026-08-30T21:26:00+08:00`；raw envelope同样是合法的 `REVISION -> FINAL_GATE` deterministic result。
- `course-run-state.md:last_worker_result.result_ref` 精确指向 `subagent-trace.md#wr-a35-final-gate-revision2`，并投影 `execution_id: MASTER / artifact_verified: true / validation_status: PASS / next_allowed_gate: FINAL_GATE`。它没有继续引用 Revision 1，也没有把 Reviewer Finding 自关闭记录当作 continuation authority。
- Master disposition只把 `A35-FG-F02` 标成 `READY_FOR_FINAL_GATE_RECHECK`；本次 fresh Reviewer依据当前 repository truth独立关闭该 Finding。`A35-FG-F01`保持`CLOSED`，没有回归。

### Fresh full regression results

- Canonical / identity：Article `35`、标题`Tool Registry 与 Tool Execution Pipeline`、Part VI、weight `L`、required Source Mode 与 Article Card一致；Draft H1精确匹配。Draft=`38999 bytes / 737 lines / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`，Evidence=`44911 bytes / 330 lines / SHA-256 1614DF93CF2BA79FCB79CAAA1F64F3AFA19ADC7CC88B3F628E557C03DC9F475C`，Outline=`15864 bytes / 333 lines / SHA-256 7E1350F8D617E9BF59625DE3B8E81F552BDD2771210A3D6B38868D16065C661E`；内容身份与上一轮冻结值一致。
- Evidence / review：Draft `35-C01—C12 = 12 unique`，Evidence `35-E01—E12 = 12 unique / final BLOCKED=0`；`A35-R1-F01—F05`保持`CLOSED / 0 OPEN`，`A35-FG-F01`保持`CLOSED`。Developer Preview / 未安全审计、typed-vs-raw、Provider边界、cooperative timeout/cancel、no rollback、optional spill/no universal summary、real Provider / production side effect / actual UI=`NOT RUN`等限制无回归。
- Trace integrity：Cycle 0 raw=`7 / 7`、selected fixture sources=`6 / 6`，合计 manifest=`13 / 13 bytes+SHA PASS`；Recovery manifest=`9 / 9 PASS`。Recovery JSONL=`13 records / 13 unique callIds / 0 parse error / 0 schema miss-or-extra / 0 Session-next correlation failure`，case distribution=`3 / 3 / 2 / 2 / 3`；accepted capture=`1 file / 5 tests / exit 0`。Cycle 0 `22 passed / NOT_ACCEPTED`与 Recovery Attempt 1 `selected 0/5 / NOT_ACCEPTED`仍可见，未被覆盖。
- Envelope / authority：`subagent-trace.md` fresh parser=`24 / 24 exact 11-field`。其中`22 / 24`通过 role enum与 common result mapping；既有 `SOURCE_INVESTIGATOR` 非法 role和 Reviewer `FAIL / true / REVISION / NONE`非法 mapping继续明确为历史 non-authoritative records，没有 retrospective PASS。两条 Final Gate Revision record均通过 exact-key、role、execution type、mapping与 `REVISION -> FINAL_GATE` transition验证。
- Source / link / fixture：Draft `4 / 4 relref`目标存在、curly-quote shortcode=`0`，`8 / 8` pinned-commit GitHub blob paths在固定 fixture存在。fixture `HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`，status、staged / unstaged `diff --check`均为空。
- State / publication guard：run-state、status、course README、Article README与Article Card一致保持 `RUNNING / Article35 / FINAL_GATE / active NONE / blocker NONE`；Article 35 Published Content=`ABSENT`，Part VI Audit=`ABSENT`，Article 36—44 workspace/content/static assets=`0`。series index为 `1` 个Article 35 planned row、`0` published relref；Article 34 Published Content存在。
- Build / repository hygiene：fresh `hugo --renderToMemory --minify` exit=`0`，Hugo `0.157.0`，`1262 Pages / 44 Static / 1 Alias`；repository `git diff --check` exit=`0`，仅有既有 LF/CRLF working-copy warning。

### Finding and score summary

| Finding | Severity | Attempt 3 status |
|---|---|---|
| `A35-R1-F01—F05` | MAJOR / MINOR | `CLOSED / NO REGRESSION` |
| `A35-FG-F01` | MAJOR | `CLOSED / NO REGRESSION` |
| `A35-FG-F02` | MAJOR | `CLOSED` |

Open Findings：`0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`。

| Dimension | Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | pinned source、边界与五类负例无回归。 |
| Evidence Discipline | `20 / 20` | 两条current Revision authority records与run-state引用完整闭合。 |
| Teaching Quality | `19 / 20` | 问题空间、抽象双链/五账与具体source path完整。 |
| Engineering Transfer | `19 / 20` | Registry / Execution / Projection合同与Proposal边界稳定。 |
| Readability & Compression | `19 / 20` | 结构、术语与发布映射清楚。 |
| **Total** | **`97 / 100`** | **Final Gate满足Publisher eligibility；不等于Published、Build Verify或END_ARTICLE。** |

### Final Gate Recheck decision

- `A35-FG-F02 CLOSED`；全部Article 35 Review / Final Gate Findings=`0 OPEN`。
- Decision=`PASS / ELIGIBLE_FOR_PUBLISH`。Publisher机械 allowlist仍仅限新建Article 35 Published Content、为Article 34追加Article 35 next-navigation、把series index唯一Article 35 planned row改为published relref，以及回写Article 35 README publication evidence。
- `PUBLISH`是下一允许Gate；本结论不声称Article 35已经Published、Hugo production Build Verify已完成、Git checkpoint已创建或`END_ARTICLE_35`已经成立。

```yaml
worker_result:
  role: REVIEWER
  article: "35"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/35-dsh-tool-registry-execution-pipeline/review.md
  gate_completed: true
  next_allowed_gate: PUBLISH
  blocker: NONE
  notes:
    - "A35-FG-F02 is CLOSED: both Final Gate Revision records have execution/task IDs, bounded briefs, exact allowed writes, raw exact-11 envelopes, Master validation results, and validation times; run-state references Revision 2."
    - "A35-FG-F01 and A35-R1-F01 through F05 remain closed; content identities, claims/cards, manifests, five accepted traces, preserved failures/invalid history, pinned fixture, links, state parity, and future-asset guards pass fresh regression."
    - "Article 35 has zero open findings and is ELIGIBLE_FOR_PUBLISH; Published Content remains absent and Article 36-44 assets remain zero."
```
