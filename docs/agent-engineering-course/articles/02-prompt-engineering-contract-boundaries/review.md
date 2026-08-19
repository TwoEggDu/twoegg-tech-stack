# Article 02 Review Record

- Lifecycle Status：`FINAL`
- Current Review Scope：`REVIEW_RECHECK / FINAL_GATE`
- Formal Review Status：`PASS`
- Evidence Review Status：`PASS`
- Course Review Status：`PASS`
- Review Cycle：`1 / 3`
- Review Date：`2026-08-19（Asia/Shanghai）`
- Reviewer Context：`FRESH / REPOSITORY_ARTIFACTS_ONLY`

## Gate History

### PRECHECK

- Outcome：`PASS`
- Disposition：Article 01 dependency、canonical entry、Normal Article mode、workspace absence、published path absence 与 clean transaction boundary 已核对。

### ARTICLE_KICKOFF / WORKSPACE_INIT

- Outcome：`PASS`
- Disposition：只创建 `PLANNED` skeleton；未写 Research Answer、Evidence Conclusion、Outline、Draft 或 Review Finding。

### REVIEW

- Outcome：`FAIL`
- Disposition：首轮独立 Review 记录两个 `MAJOR`（`02-F01`、`02-F02`），转入定向 Revision；未进入 FINAL 或 Publish。

### REVIEW_RECHECK

- Outcome：`PASS`
- Disposition：Reviewer 基于 commit `798443c1d41f03960253b1190fcbc91425d4f285` 的实际 diff、Article 01 hotfix review / build evidence、Article 02 Revision Disposition 与当前 README，关闭两个 Finding；Final Gate 进入 `FINAL`，允许 Publisher。

## First-pass Primary-source Re-verification

- OpenAI 当前 [Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering) 仍支持 developer 高于 user、developer / user 类比函数定义与参数、Prompt 存入应用代码、typed arguments、representative fixtures / tests，以及 reusable prompt objects 自 `2026-06-03` 弱化创建入口、`/v1/prompts` 计划于 `2026-11-30` 关闭的表述。
- OpenAI 当前 [Model guidance](https://developers.openai.com/api/docs/guides/latest-model) 支持 GPT-5.6 guidance 中的 goal、relevant context、constraints、required evidence、success criteria 与 output format；正文没有把它外推为跨 Provider 统一模板。
- OpenAI 当前 [Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices) 支持 objective、dataset、metrics、run / compare、continuous evaluation、typical / edge / adversarial cases 与反对 vibe-based eval；同页支持托管 Evals 于 `2026-10-31` 转只读、`2026-11-30` 关闭的时间线。
- OpenAI 当前 [Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs) 支持 `JSON mode != schema adherence`，并明确 Structured Outputs 仍可能包含内容错误；Article 02 已停在自然语言 Output Requirement，没有提前展开 Article 03 的 parse / validate / repair。
- OpenAI 当前 [Safety in building agents](https://developers.openai.com/api/docs/guides/agent-builder-safety) 与 [OWASP Prompt Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html) 共同支持 approvals、input / output validation、least privilege、isolation 与 residual injection risk；正文没有把 delimiter 或 Prompt 写成 authorization boundary。
- Anthropic 当前 [Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices) 支持 relevant / diverse / structured examples 与 XML tags 的有限作用；`02-C04` 在 Draft 中保持了 `PARTIAL` 的收窄措辞。
- Anthropic 当前 [Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages) 逐字列出 `Claude Fable 5`、`Claude Mythos 5`、`Claude Opus 4.8`、`Claude Opus 5`、`Claude Sonnet 5` 的支持范围，并支持 later system message precedence、mid-conversation system 相对 top-level system 的位置规则、application-observed state 与 untrusted-content 限制。Evidence 中没有不存在的模型名；但它也使已发布 Article 01 的 Anthropic role 总结成为当前跨篇矛盾，见 `02-F01`。

## First-pass Findings

### 02-F01

- Finding ID：`02-F01`
- Severity：`MAJOR`
- Category：`COURSE`
- Location：`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md:147-155`；`docs/agent-engineering-course/articles/02-prompt-engineering-contract-boundaries/draft.md:71-81`；`evidence.md:73-84`
- Problem：Article 02 的当前 Provider 边界是正确的：Anthropic 通用起始 system instruction 使用顶层 `system`，部分当前模型又允许 messages 数组中的 mid-conversation `role: system`。但作为直接前置的已发布 Article 01 仍无条件写成“Anthropic input messages 使用 user / assistant；system 是顶层参数，不是 system role”。两篇都标注 `2026-08-19` 核对时间，却向读者给出互不相容的当前 contract。
- Supporting Evidence：Anthropic 当前 [Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages) 明确给出 `{"role":"system"}` message、当前支持模型、placement rules，以及 later system messages 的 precedence；Article 02 `02-E03` 已正确记录该例外，而 Article 01 发布表格没有 model / feature scope 限定。
- Why It Matters：Article 01 是 Article 02 的必读前置，课程核心能力又包括“不得把 Provider-specific role 写成统一 enum / hierarchy”。若直接发布 Article 02，读者会在相邻两篇看到同日相反事实，Part I 的 provider-contract reading 训练失去可信度；这不是单篇措辞偏好，而是可验证的跨篇技术矛盾。
- Required Disposition：由 Master 按 `PUBLISHED + POST_PUBLICATION_HOTFIX` 路由一个范围明确的 Article 01 修复：把其 Anthropic role 表述限定为 generic / top-level baseline，并补上当前部分模型的 mid-conversation `system` 例外与核对日期；同步 Article 01 的 Research / Evidence / Draft / Published Content，并重新通过 Hugo Build。Article 02 只需保留当前准确的 model / placement / version 限定，不得把修复扩大成统一 hierarchy 或完整 Prompt priority 教程。完成后交回 Reviewer recheck；只有 Reviewer 可将本 Finding 标为 `CLOSED`。

### 02-F02

- Finding ID：`02-F02`
- Severity：`MAJOR`
- Category：`PUBLICATION`
- Location：`docs/agent-engineering-course/articles/02-prompt-engineering-contract-boundaries/README.md:24-31`、`43-45`
- Problem：README 顶部已经声明 `Lifecycle Status = REVIEW`、`Current Gate = REVIEW`、下一动作是 independent review，但“生产资产”仍把 Research 写成 `NOT_STARTED`、Evidence 写成核心 Claim 全部 `BLOCKED`、Review 写成 `NOT_STARTED`，并继续声称当前只完成 `ARTICLE_KICKOFF / WORKSPACE_INIT`、不得创建 Draft。它与同一 README 顶部、实际已存在的 Research / Evidence / Outline / Draft，以及 global status / run-state 相冲突。
- Supporting Evidence：`README.md:9-15` 与 `status.md` / `course-run-state.md` 均指向 Article 02 `REVIEW`；workspace 已存在完整 `research.md`、`evidence.md`、`outline.md`、`draft.md`。Course Factory invariant 明确 `Repository State > Agent Context`，README 又是当前 Article workspace 的 durable lifecycle artifact。
- Why It Matters：这会使 resume、revision handoff 和 publication precheck 读取到互斥状态。即使正文技术内容正确，也不能在 Article transaction 的 durable artifact 自相矛盾时进入 FINAL 或 Publish。
- Required Disposition：只更新 Article 02 README 中已过期的生产资产说明与 Stop Line，使其准确描述当前 `REVIEW`、Evidence summary、已存在 artifacts、未发布状态与下一允许动作；保留 PRECHECK / KICKOFF 历史，不写成 `FINAL / PUBLISHED`，不修改 global durable state。完成后交回 Reviewer recheck；只有 Reviewer 可将本 Finding 标为 `CLOSED`。

## Revision Disposition

### 02-F01 Revision Disposition

- Finding ID：`02-F01`
- Changed Scope：Article 01 post-publication hotfix，独立 commit `798443c1d41f03960253b1190fcbc91425d4f285`。
- Repair Summary：Article 01 的 Anthropic role contract 已收窄为 generic / conversation-start baseline，并补入当前受支持模型中 placement-constrained mid-conversation `role: system` 的例外；Research、Evidence、Outline、Draft、Published Content 与 Article 01 durable review state 已同步。
- Independent Recheck Evidence：fresh Reviewer 已对 Article 01 hotfix 给出 `PASS`。
- Build Evidence：Hugo `0.157.0`；`1230 Pages`；`0 ERROR`；`0 WARNING`；process `exit 0`。
- Article 02 Impact：Article 02 继续保留当前 model / placement / version 限定，无需改写其 Research、Evidence、Outline 或 Draft；相邻必读文章的 provider-contract 表述现已对齐。
- Proposed Status：`READY_FOR_RECHECK`。
- Authority Boundary：本处只提交修订证据与处置建议；Finding 是否关闭仍由 Reviewer recheck 决定。

### 02-F02 Revision Disposition

- Finding ID：`02-F02`
- Changed Scope：仅 `docs/agent-engineering-course/articles/02-prompt-engineering-contract-boundaries/README.md`。
- README Changes：
  - 将 Current Gate 更新为 `REVISION`、Next Allowed Action 更新为 `REVIEW_RECHECK`，并显式列出 `02-F01`、`02-F02`。
  - 将 Research 更新为 `COMPLETE`，将 Evidence 更新为 `7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
  - 补列已存在的 `outline.md`、`draft.md`，并记录首轮独立 Review 为 `FAIL`。
  - 将 Stop Line 更新为当前真实阶段：尚未发布，Reviewer recheck 前不得进入 `FINAL / PUBLISHED`、不得创建 Published Content、不得启动 Article 03。
  - 保留 `PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT` 的完成历史。
- Evidence Impact：`NONE`；未修改 Research、Evidence、Outline 或 Draft。
- Proposed Status：`READY_FOR_RECHECK`。
- Authority Boundary：本处不关闭 Finding，也不声明 Final Gate PASS；结论交由 Reviewer recheck。

## Review Recheck｜Cycle 1

### 02-F01 Recheck

- Finding ID：`02-F01`
- Prior Severity：`MAJOR`
- Reviewer Decision：`CLOSED`
- Recheck Evidence：Reviewer 已读取 commit `798443c1d41f03960253b1190fcbc91425d4f285` 的实际 `show / diff`。该 commit 只修改 Article 01 Published Content、README、Research、Evidence、Outline、Draft、Review 七个文件：generic / conversation-start baseline 继续使用顶层 `system`；当前受支持模型的 mid-conversation `role: system` 例外同时带有 model、placement、version 限定。Research `RQ-04`、Claim `01-C04 / 01-E04`、Outline、Draft 与 Published Content 对齐，没有扩写成跨 Provider 统一 hierarchy 或完整 Prompt priority 教程。
- Hotfix Review / Build Evidence：Article 01 `review.md` 已记录 fresh independent recheck 为 `PASS`、`01-IR-F02` 为 `CLOSED`；同一 durable record 记录 Hugo `0.157.0`、`1230 Pages`、`0 ERROR`、`0 WARNING`、process `exit 0`。这些仓库 artifacts 足以证明 required disposition 中的跨篇同步、独立复核与构建门已完成。
- Remaining Issue：`NONE`。
- Closure Rationale：Article 01 与 Article 02 现在共同表达“通用起始 contract + 当前受支持模型的受限例外”，首审指出的相邻必读文章事实矛盾已消失；由本 Reviewer 在本轮正式关闭。

### 02-F02 Recheck

- Finding ID：`02-F02`
- Prior Severity：`MAJOR`
- Reviewer Decision：`CLOSED`
- Recheck Evidence：当前 Article 02 README 已将 Current Gate 记录为 `REVISION`、Next Allowed Action 记录为 `REVIEW_RECHECK`，列出 `02-F01 / 02-F02`；Research 为 `COMPLETE`，Evidence 为 `7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`，Outline、Draft 与首轮 Review 均已列入生产资产。Stop Line 明确当前尚未发布，Reviewer recheck 前不得进入 `FINAL / PUBLISHED`、不得创建 Published Content、不得启动 Article 03，同时保留 PRECHECK / KICKOFF 历史。
- Remaining Issue：`NONE`。
- Closure Rationale：README 已与实际 workspace artifacts 和首审阶段一致，不再出现 `NOT_STARTED / BLOCKED / 仅完成 WORKSPACE_INIT` 的陈旧声明；由本 Reviewer 在本轮正式关闭。

## Finding Counts

- `BLOCKER`：`0`
- `MAJOR`：`0`
- `MINOR`：`0`
- `EDITORIAL`：`0`
- Historical Findings Closed：`02-F01`、`02-F02`（均为首审 `MAJOR`）
- Unclosed Findings：`NONE`

## Review Coverage

### Technical / Evidence

- `02-C04`：Draft 只保留 few-shot 的模式表达、relevant / diverse / structured 建议和 workload-specific verification；没有通用准确率、token、staleness 或 bias 量化外推。
- `02-C09`：`SYNTHETIC_COURSE_PROPOSAL / NOT_EXECUTED`、`Runtime Observation = NONE`、`Claim Status = PROPOSAL` 在 Outline 与 Draft 中一致；没有把 expected 写成 observed，也没有 accuracy / production claim。
- `Prompt != Policy / Permission / Current Fact / Structured Output / Eval`：正文均以“不替代 / 不能由此证明”切开，且保留 Prompt 作为表达或 defense-in-depth 一层的有限价值。
- Provider roles：Draft 没有建立跨 Provider 固定 enum 或 hierarchy；OpenAI 与 Anthropic 的 current contract、model / placement / version 限定准确。Article 01 已通过 `798443c1d41f03960253b1190fcbc91425d4f285` 收窄并完成独立复核，跨篇矛盾 `02-F01` 已关闭。

### Course / Reader Value / Job Competency / Publication

- Article 03 stop line：只引入 Output Requirement 与 schema / domain correctness 区分，没有展开 JSON Schema、parse、validate、repair。
- Article 12—17 stop line：只说明 dynamic facts、Context / KB / Memory / Skill 不由 Prompt 自动建立，没有吞掉 assembly、debugging、state lifecycle、RAG 或 Skill packaging。
- Article 19 stop line：只建立 authorization、least privilege、approval、sandbox 与 Prompt 的责任边界，没有设计 permission runtime。
- Article 22 stop line：只建立 version + fixture + criteria + raw output / judgment 的最低变更接口，没有展开 dataset、grader calibration、metrics、continuous eval 或 regression system。
- Reader Value / Job Competency：六项 review canvas、Provider contract reading questions、boundary matrix、Prompt change record 与七道 Learning Check 均可迁移到真实工程；正文保持 M 级密度，没有退化为技巧清单。
- Publication：Draft 尚未写入 Hugo content tree，符合 review transaction boundary；`02-F01`、`02-F02` 均已由 Reviewer 关闭，Article 02 可交由 Publisher 执行后续发布事务。

## Formal Review Score｜First Pass

| Dimension | Score | Rationale |
|---|---:|---|
| Technical Accuracy | `17/20` | Article 02 自身 Provider、Prompt、Structured Output 与安全边界准确；直接前置 Article 01 仍存在同日可验证的 Anthropic role 矛盾。 |
| Evidence Discipline | `19/20` | 9 个 Claim 可追踪；`02-C04` 与 `02-C09` 强度匹配；模型名、OpenAI Prompt / Evals 生命周期日期及安全边界均由 current primary sources 支持。 |
| Teaching Quality | `17/20` | 问题空间、六项抽象、具体 mapping 与验证边界完整；相邻必读文章的冲突会破坏课程递进，需先闭合。 |
| Engineering Transfer | `17/20` | Review canvas、Provider questions 与 change record 可直接迁移；当前跨篇 role 冲突会让读者在实际 Provider mapping 上得到两套答案。 |
| Readability & Compression | `18/20` | 正文约 3.6k CJK 字符，表格与示例承担压缩，主线清楚；没有用重复篇幅掩盖 Evidence 缺口。 |
| **Total** | **`88/100`** | 总分达到数值线，但 Technical `17 < 18`，且存在两个未关闭 `MAJOR`，不得进入 FINAL。 |

## First-pass Gate Decision

- Decision：`FAIL`
- Final Threshold Check：Total `88 >= 88`；Technical `17 < 18`；Evidence `19 >= 18`；Teaching `17 >= 17`；Engineering Transfer `17 >= 17`
- Gate Reason：`02-F01`、`02-F02` 均为未关闭 `MAJOR`；首轮 Review 不允许假设未来修复或自行关闭 Finding。
- Recommended Next Action：Master 路由 `REVISION`：先执行 `02-F02` 的 Article 02 README 定向修订，并按 post-publication hotfix contract 处理 `02-F01` 的 Article 01 跨篇事实矛盾；Revision Worker 逐 Finding 写 Revision Disposition 后，返回 fresh `REVIEW_RECHECK`。
- Blockers：`NONE`。当前证据足以完成定向修订，无需新 Lab、无需执行 `02-FX01`、无需修改 canonical。

## Final Gate

### Formal Review Score｜Final

| Dimension | Score | Rationale |
|---|---:|---|
| Technical Accuracy | `19/20` | Article 02 自身的 Provider contract、Prompt 边界、Structured Output 与安全边界准确；Article 01 热修复已消除 Anthropic role 的相邻跨篇矛盾，并保留 model / placement / version 限定。 |
| Evidence Discipline | `19/20` | 9 个 Claim 可追踪，`02-C04` 保持 `PARTIAL` 收窄，`02-C09` 保持 `NOT_EXECUTED / PROPOSAL`；当前模型名、Prompt / Evals 生命周期日期与安全边界均有首审 primary-source 核对记录。 |
| Teaching Quality | `18/20` | 问题空间、六项 contract 抽象、具体 mapping、反例与 Learning Check 形成完整递进；直接前置 Article 01 已与本篇的 Provider 边界一致。 |
| Engineering Transfer | `18/20` | Review canvas、Provider contract questions、boundary matrix 与 Prompt change record 可直接迁移到工程 review / change workflow，且未把 Prompt 当作 Policy、Permission、Current Fact、Structured Output 或 Eval。 |
| Readability & Compression | `18/20` | 正文约 3.6k CJK 字符，表格与示例承担压缩，M 级篇幅内主线清楚；各相邻主题均在 stop line 前止步。 |
| **Total** | **`92/100`** | 五维均达到课程阈值，且没有未关闭 `BLOCKER / MAJOR`。 |

- Decision：`PASS`
- Final Threshold Check：Total `92 >= 88`；Technical `19 >= 18`；Evidence `19 >= 18`；Teaching `18 >= 17`；Engineering Transfer `18 >= 17`
- Finding Threshold Check：未关闭 `BLOCKER = 0`；未关闭 `MAJOR = 0`
- Lifecycle Decision：`FINAL`
- Publisher Authorization：`ALLOWED`。Publisher 可在其独立事务中创建 Published Content、执行构建并推进 durable publication state；本 Review 不代替 Publisher 的发布与构建责任。
- Recommended Next Action：Master 路由 `PUBLISH`；不得在本 Review recheck 中直接修改 content / canonical、全局状态或启动 Article 03。
- Blockers：`NONE`。
