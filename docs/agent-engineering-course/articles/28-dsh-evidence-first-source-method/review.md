# Article 28 Review

Status: `FINAL REVIEW PASS / ZERO OPEN FINDINGS`

Reviewer: `fresh REVIEWER / independent first pass`

Reviewed at: `2026-08-30 / Asia/Shanghai`

Rechecked at: `2026-08-30 / Asia/Shanghai`

## 1. Gate decision

- Decision: `PASS`
- Open findings: `0 MAJOR / 0 MINOR / 0 BLOCKER`
- Closed findings: `A28-RV-001`
- Next allowed gate: `FINAL_GATE`
- Publishing eligibility: `ELIGIBLE FOR FINAL_GATE`；实际 publication 与 Hugo build 仍由后续 Publisher / Build Gate 独立验证。
- Revised score: `98 / 100`
- Rechecked Draft identity: `SHA-256 78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE / 38066 bytes`

Independent recheck 已确认唯一 Finding 完成最小修订：本次 baseline 的现状统一写成 direct structured observations with sanitized excerpts，并明确未保留完整 stdout/stderr stream；理想 raw Trace 教学标准与本轮实际 durable form 已分开。修订没有改变 16 个 Claim 主线、12 张 Evidence Card、DSH baseline 或任何 direct probe outcome。

## 2. Independent reads and source spot-check

本轮完整读取并交叉核对：

- `draft.md`、`outline.md`、`research.md`、`evidence.md`、`source-map.md`、`baseline-manifest.md`、`experiments/baseline-probes.md`、`article-card.md`、`README.md`。
- 仓库 `AGENTS.md`、`docs/article-writing-method.md`、`docs/article-outline-template.md`、`docs/series-planning-method.md`、`docs/article-production-workflow.md` 与 `twoegg-article-method`。
- DSH 固定 fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`，只读抽查。

Fixture spot-check result：

| Check | Direct result | Review conclusion |
|---|---|---|
| origin | `https://github.com/deepseek-ai/deepseek-harness.git` | official repo identity matches |
| HEAD | `cd5ef8148158c3a752a658978873241fdf8e2bbc` | exact pinned revision matches |
| local tag target | same full SHA | tag identity matches |
| status | `0` rows | fixture remained clean at review time |
| root commands | `packageManager=pnpm@11.7.0`; `build=tsx scripts/build.ts`; `test=vitest run`; `dsh=node --import tsx/esm apps/cli/src/bin.ts`; `postinstall=node scripts/install-lefthook.mjs` | article command entry claims match source |
| CLI/profile/boot | `parseDshArgs -> runProfile -> composeProfile/prepareProfile -> boot -> mountRootInclude -> loader.await -> assertEntriesActivated` anchors exist | settled-boot static baseline is supported; runner-to-Agent remains correctly deferred |
| headless composition | `headless-startup` and `headless-runner` rows exist | config-row existence only; draft correctly refuses activation inference |
| future route anchors | `AgentLoop`, `ReactLoopAgent`, `PromptContext`, `PromptAssembly`, `SystemPrompt`, `renderPrompt`, `executeToolCalls`, `SessionEventMap`, `PersistenceCoordinator`, `JsonlSessionPersistence`, `SessionProjectionRegistry`, `ToolRuntime`, `ToolDefinition`, `ToolExecutionResult`, `defineTool`, `validateArgs`, `TOOL_TIMEOUT`, `CompactionEngine` exist in named owning files | appropriate as Article 29—37 seeds, not completed paths |
| official posture | pinned `README.md` says Developer Preview and compatibility-breaking changes; pinned `SAFETY.md` says no security audit, not secure/production-ready, and requires least privilege | version/safety wording is accurate |
| license | root `package.json` declares `MIT`; `LICENSE` exists | baseline manifest wording is accurate |

No DSH source or fixture artifact was modified during review.

## 3. Finding register

### A28-RV-001 — Stored trace is summarized/excerpted, but several artifacts overstated its durable form

- Severity: `MAJOR`
- Status: `CLOSED / REVIEW_RECHECK PASS`
- Location:
  - `research.md` §1 / §2 / §5 / §8
  - `evidence.md` Evidence Posture / Gate recommendation and `28-E04: Trace / Limitations / Runtime Trace`
  - `baseline-manifest.md` opening status and durable-evidence note
  - `baseline-probes.md` opening status/note, §2 heading and gate-result heading
  - `draft.md` opening evidence ceiling, §4 / §5 / §12
- Evidence:
  - The Article 28 workspace contains no separate full stdout/stderr log artifact or hash-addressed raw-log path.
  - `baseline-probes.md` durably preserves exact commands, environment conditions, exit codes, terminal summaries, failure classes and selected sanitized stderr excerpts. For the full unit test and build it does not preserve the complete raw terminal stream.
  - The evidence-card template defines `Trace` as a raw trace/log path or `N/A`; before revision, `28-E04` pointed to summarized Markdown sections without disclosing that the full terminal stream was not retained.
- Why this matters: 本篇主张 Evidence-first 与可重建证据链。把“direct, structured, sanitized observation record”写成“完整 raw log”会让 Evidence 层级本身出现过度确认；这不推翻 `32 files / 129 tests failed`、isolated `27/27`、host-access caveat 或 `MISSING_CREDENTIAL` 等窄观察，但会削弱其可审计性说明。
- Required minimal fix:
  1. 把所有针对**本次 baseline artifact 现状**的过强措辞改为准确表述：`direct structured observations with sanitized terminal excerpts; complete stdout/stderr stream not retained`。
  2. 在 `28-E04` 的 `Trace` / `Limitations` 与 `baseline-probes.md` 开头明确：durable artifact 保存的是命令、exit、summary、failure classification 与脱敏关键行；未保存完整 stdout/stderr stream。
  3. `draft.md` 保留“理想方法应保存 raw Trace”的教学要求，同时加一句本次 baseline 的实际证据上限，避免把方法目标反投影为已经拥有的 artifact。
  4. 不需要重跑 5 分钟 full suite，也不要改写 direct `32 / 129`、isolated `27/27`、build caveat 或 keyless boundary；除非 Revision Worker 能提供真实、可核验且脱敏的完整日志路径，否则只能收窄措辞，不能补写一个并不存在的 raw log。

Revision Worker exact edits for recheck:

- `research.md`：将 baseline 现状收窄为 direct structured observations，明示保留 commands、environment、exit codes、terminal summaries、failure classification 与 sanitized excerpts，且未保留完整 stdout/stderr stream；理想 dynamic claim 标准仍保留 raw stdout/stderr。
- `evidence.md`：在 Evidence Posture、`28-E04` Trace/Limitations/Runtime Trace 和 Gate recommendation 中写明 structured/sanitized durable form 及其上限。
- `baseline-manifest.md`：状态改为 `STRUCTURED OBSERVATIONS COMPLETE`，并在开头声明持久化形式与 full-stream limitation。
- `experiments/baseline-probes.md`：状态与章节改为 structured observations，开头明示命令/环境/exit/摘要/分类/脱敏摘录为持久化形式，完整 stdout/stderr stream 未保留。
- `draft.md`：保留“理想方法应保存 raw Trace”的教学标准，并在开篇、Minimal run 和研究单处明示本次 baseline 实际只有 structured/sanitized durable record。
- 保持 `32 files / 129 tests failed`、isolated `27/27`、`PASS_WITH_HOST_ACCESS_CAVEAT`、`MISSING_CREDENTIAL`、16 Claims / 12 Cards、baseline SHA、Article 29—37 routes 和 Part VII stop line 不变；本轮未重跑测试。
- Independent recheck disposition:
  - `research.md` 不再把本次 baseline 称为 raw-complete；§2 与 §5 明确区分理想协议和实际 structured/sanitized evidence ceiling。
  - `evidence.md` 的 Evidence Posture、`28-E04 Trace / Limitations / Runtime Trace` 与 Gate recommendation 均明确 durable form 和 full-stream limitation。
  - `baseline-manifest.md` 与 `experiments/baseline-probes.md` 的状态、开头说明和观察标题已统一为 structured observations。
  - `draft.md` 在开篇、Minimal run、source-boundary 与 reusable research sheet 处明确区分理想 raw Trace 标准和本次实际保存层级。
  - Article 28 中其余 `raw Trace` 表述均指向理想方法或 Article 29—37 的未来 evidence work；没有再把本次 baseline 的 excerpt/summary 冒充完整 raw stream。
  - Finding `A28-RV-001 = CLOSED`。

## 4. Claim and Evidence Card audit

### Claim coverage

- Coverage: `16 / 16`。
- `28-C01—C06` 的正文措辞与已合并 Evidence 一致，固定为版本身份、安全姿态、课程合同、当前 host 的 bounded probes 与 credential boundary。
- `28-C07—C16` 全部保持 `PROPOSAL`；正文没有把 Article 29—37 的 source seeds 写成 completed call path 或 runtime fact。
- Status account matches: `6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED`。

### Evidence Card coverage

- Coverage: `12 / 12`。
- Card account matches: `4 CONFIRMED / 0 PARTIAL / 8 PROPOSAL / 0 BLOCKED`。
- Evidence Class、DSH Verification / confirmation level 与 Claim Status 被作为独立轴叙述，没有把 `PINNED_SOURCE` 自动升级成 `RUNTIME_CONFIRMED`。
- `28-E04` 对 install、build、full test、isolated test、CLI help、config dump 与 keyless run 的证据上限分账正确；Finding `A28-RV-001` 只要求修正 durable trace 的保存形式，不要求更改这些 bounded outcomes。

## 5. Required boundary checks

| Review axis | Result | Notes |
|---|---|---|
| Developer Preview / safety / license / version | `PASS` | 无 production-ready、安全审计或跨版本稳定性过度承诺 |
| source / generated / runtime boundary | `PASS` | `lib/dist` 只作为 producing-command artifact；latest main/live docs 不补 pinned gap |
| direct Lab full test | `PASS` | 正文保留 `32 failed files / 129 failed tests` 与多类失败，不归并成单一 Windows 原因 |
| isolated notices suite | `PASS` | `27/27` 只分类一个 timing-sensitive case，不升级 full suite |
| host-access caveat | `PASS` | sandbox `exit 1` 与 unchanged host retry `exit 0` 同时保留 |
| config dump | `PASS` | 只写 effective resolution，不写 activation |
| keyless run | `PASS` | 只确认 `MISSING_CREDENTIAL` boundary；不宣称 Agent Turn、provider/model response、token 或 cost |
| DSH five-layer identity | `PASS` | Model Wrapper / Runtime / Harness / Host / Product 仅作为问题矩阵，没有确认归属图 |
| Article 29—37 routing | `PASS` | 九篇均为 `PARTIAL / DEFER`，没有创建或预写 future article conclusion |
| BuildPilot decision | `PASS` | `ADOPT` Evidence-first 方法与安全约束；具体架构均 `DEFER` |
| Part VII stop line | `PASS` | Article 38 仅被描述为 Audit 后的候选；`Part VII: NOT STARTED` 明确 |

## 6. Writing-method and readability audit

- Problem space first: `PASS`。首屏从“名字被误写成行为证据”的真实问题切入，没有从 API 或 package catalog 开场。
- Abstract model: `PASS`。Six Evidence Classes、DOC/SOURCE/RUNTIME confirmation、Claim Status 与 evidence-upgrade ladder 构成清晰中心模型。
- Concrete implementation: `PASS`。Baseline Manifest、source boundary 与 install/build/test/config/keyless probes 把抽象落回真实工程。
- Engineering boundaries: `PASS`。版本、平台、权限、credential、network、cost、generated residue 与 production posture 都被显式切开。
- Article type: `PASS`。整体是 S 级 `STAGE_NAVIGATION / SOURCE_METHOD`，不是 DSH 模块百科。
- Repetition: `PASS_WITH_NOTE`。Section 13—14 重复部分 evidence ceiling，但承担 S 级导航篇的审计索引职责，未达到需要重构正文的程度。
- Class-name stacking: `PASS_WITH_NOTE`。类名集中在 Article 29—37 路由和 static baseline，而不是驱动开篇；每组名字均绑定 future evidence question。
- Closing: `PASS`。结尾压缩回“知道何时成立、成立到哪里、何时停下”，没有 Part VII 宣传性抢跑。

## 7. Hugo and publication-surface audit

- Draft 中两个 `relref` shortcode 均使用 ASCII 双引号，语法形态正确。
- Draft 尚未含 front matter，符合当前 Draft Gate；Publisher 应按 approved outline 写入 target content front matter，并由真实 Hugo build 验证。
- 未发现中文引号嵌入 YAML、broken shortcode、未来 Article 29 relref 或 Article 38 asset。
- 本轮不执行 Publisher 的 Hugo build，也不把 review 结果冒充 publication/build PASS。

## 8. Review Recheck result

| Acceptance check | Result |
|---|---|
| 本次 baseline 不再被称为完整 raw log | `PASS` |
| `28-E04` 明示 durable trace form 与 limitation | `PASS` |
| 理想 raw Trace 标准与实际 structured/sanitized record 分离 | `PASS` |
| Direct full-test result | `UNCHANGED / 32 failed files / 129 failed tests` |
| Isolated notices result | `UNCHANGED / 27/27 PASS / full suite remains FAIL` |
| Build boundary | `UNCHANGED / PASS_WITH_HOST_ACCESS_CAVEAT` |
| Keyless boundary | `UNCHANGED / MISSING_CREDENTIAL / no completed Agent Run` |
| Claim / Card account | `UNCHANGED / 16 Claims / 12 Cards` |
| Fixed DSH baseline | `UNCHANGED / cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| Article 29—37 routes | `UNCHANGED / PARTIAL / DEFER` |
| BuildPilot / Part VII | `UNCHANGED / ADOPT METHOD / DEFER ARCHITECTURE / Part VII NOT STARTED` |
| Draft identity | `SHA-256 78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE / 38066 bytes` |
| `git diff --check` | `PASS` |

Final Reviewer decision: `PASS / ZERO OPEN FINDINGS / CONTINUE TO FINAL_GATE`。

## 9. Independent FINAL_GATE

Final Reviewer: `fresh REVIEWER / independent final gate`

Final-gate reviewed at: `2026-08-30 / Asia/Shanghai`

### 9.1 Frozen input identity

- Draft SHA-256: `78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE`
- Draft bytes: `38066`
- Prior Review / Revision / Recheck: `1 MAJOR CLOSED / 0 OPEN`
- Claim register: `16 / 16`，顺序精确覆盖 `28-C01—28-C16`
- Evidence Cards: `12 / 12`，顺序精确覆盖 `28-E01—28-E12`
- Claim status: `6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED`
- Card status: `4 CONFIRMED / 0 PARTIAL / 8 PROPOSAL / 0 BLOCKED`

FINAL_GATE 复算得到的 Draft identity 与 Review Recheck 冻结值完全一致；未发现 Review 之后的正文漂移，也未发现重新打开、遗漏或新增 Finding。

### 9.2 Evidence and boundary audit

| Final-gate axis | Result | Independent conclusion |
|---|---|---|
| DSH pinned identity | `PASS` | fixture `origin` 仍为 official repository；`HEAD == local tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；status `0` rows |
| source / runtime / durable-record separation | `PASS` | pinned source、generated artifact、runtime observation 与 structured/sanitized durable record 的上限保持分离；没有把未保存的完整 stdout/stderr stream 写成现有证据 |
| install / build | `PASS` | 只保留 populated-offline-store install 与 `PASS_WITH_HOST_ACCESS_CAVEAT`；sandbox `exit 1` 没有被成功重试覆盖 |
| full test / isolated counterexample | `PASS` | direct full suite 保持 `32 failed files / 129 failed tests / exit 1`；isolated `27/27` 只用于分类 timing-sensitive case，不升级 full suite |
| config / keyless runtime | `PASS` | config dump 只确认 effective resolution；keyless child 只确认 `MISSING_CREDENTIAL` boundary，没有 Agent Turn、provider/model response、token 或 cost Claim |
| Article 29—37 | `PASS` | 正文只给 investigation route、falsifier 与 `PARTIAL / DEFER` ceiling；没有把 future seed 升级为 completed call path 或 runtime fact |
| five-layer model | `PASS` | Model Wrapper / Runtime / Harness / Host / Product 只以 question matrix 出现，没有确认归属图 |
| BuildPilot / Part VII | `PASS` | 仅 `ADOPT` Evidence-first 方法与安全约束；架构继续 `DEFER`；Article 38 / Part VII 保持 `NOT STARTED` |

### 9.3 Publication preflight

- Planned target: `content/ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md`；当前不存在，符合尚未进入 `PUBLISH` Gate 的状态。
- Planned title、slug、series、`primary_series`、`series_order = 290` 与 `weight = 3290` 符合 canonical 和 `(ID + 1) × 10` / `3000 + series_order` 规则。
- Draft 仅含 Article 27 与 Course Index 两个 `relref`；目标文件均存在，shortcode 参数使用 ASCII 双引号。
- 没有 Article 29 future `relref`，也没有 Article 29—44 workspace / published-content asset。
- Publisher 仍须把 approved front matter 写入 published content、更新 Course Index / canonical publication surface，并执行真实 Hugo Build；本 FINAL_GATE 不冒充 `PUBLISH` 或 `BUILD_VERIFY` PASS。

### 9.4 Final decision

- FINAL_GATE: `PASS`
- Quality score: `98 / 100`
- Open findings: `0 BLOCKER / 0 MAJOR / 0 MINOR`
- Publication eligibility: `ELIGIBLE_FOR_PUBLISH_GATE`
- Next allowed gate: `PUBLISH`

Final-gate decision: `PASS / ZERO OPEN FINDINGS / ELIGIBLE_FOR_PUBLISH_GATE`。
