# Review｜Article 20 Budget Engineering：Token、Step、Cost 与 Latency

## Review Identity

- Reviewer: fresh Reviewer `/root/article20_reviewer_cycle0`
- Review date: `2026-08-26`（Asia/Shanghai）
- Gate: `REVIEW`
- Cycle: `0`（initial independent review）
- Mode: `NORMAL_ARTICLE`
- Course Weight: `M / Standard Core Lesson`
- Required Lab: `NONE`
- Context isolation: 仅依据 durable repository artifacts、canonical、glossary、published Article 19、Article 21/22 detailed plans 与可复核的 current primary / official sources审查；未读取或依赖 Author hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只创建 `review.md`，并在当前 Article `subagent-trace.md` 的既有 REVIEW dispatch 下追加一个 raw Worker Result；未修改 Card、README、Research、Evidence、Outline、Draft、Published Content、global/canonical、Git 或 future-Article artifact。

## Frozen Review Input

- Draft: `docs/agent-engineering-course/articles/20-budget-engineering-token-step-cost-latency/draft.md`
- SHA-256: `064407F1841DD55AF6B1DDCC7903969AC2106EC775CEEF9F3B6454E4EE1EEFA1`
- Bytes: `37187`
- Physical lines: `444`
- Claim register: `9` Claims
- Evidence Cards: `11`
- Registered evidence mix: `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`
- Runtime state: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`

## Review Status

`REVIEW_CYCLE_0_COMPLETE / REVISION_REQUIRED`

Draft 的问题空间、四维分账、Provider-native boundary、Article 19 authority bridge、Article 21/22 ownership stop line 与 BuildPilot design-only标签均成立。9个Claims和11张Cards也全部可追踪，没有新增核心Claim。

但核心工程模型仍有两处不能直接交给读者实现的内部缺口：`BudgetVector` 的 Cost 维没有 `remaining`，且 Step/Cost 的 reserve、consume、commit 与 pending actual 转移可能重复计账或提前释放；Latency 又要求跨 checkpoint/resume 继续扣除 monotonic elapsed，却没有冻结 clock domain / reboot / host migration 后的可比较规则。此外，两项声称在 `2026-08-26` 检索到的 current release identity 已被同日官方 release index 直接推翻。

因此本轮打开 `2 MAJOR + 1 MINOR` Findings。三项均保持 `OPEN`，Review execution 完整，但 Review Gate 不通过；按合同路由 `REVIEW -> REVISION -> REVIEW_RECHECK`，不构成 Factory blocker 或 human stop。

## Technical Accuracy

- [x] Token、Step、Cost、Latency 被定义为相关但不可互换的四个合同面，没有压成一个 scalar score。
- [x] Context Window、preflight count/estimate、Provider-native response usage 与 Run Token Budget 四分账成立；没有固化窗口或跨 Provider字段语义。
- [x] OpenAI Agents SDK turn、LangGraph super-step 与课程 committed Step 保持产品/课程限定，没有声称可换算。
- [x] Estimate、reservation 与 source-qualified cost actual 在概念上分离；FOCUS Billed/List/Effective Cost 没有被写成 Agent reservation标准。
- [x] Deadline、timeout、application-visible queue/service time 与 visible-DAG critical path 的证据天花板总体克制。
- [ ] Step/Cost 的状态字段与扣减/提交时点缺少单值不变量，现有表述可重复扣减或错误释放；见 `A20-R0-F01`。
- [ ] checkpoint/resume后的 latency remaining 缺少跨 clock-domain 规则；见 `A20-R0-F02`。

Outcome: `FAIL / REVISION_REQUIRED`

## Evidence Discipline

### Claim audit（9 / 9）

| Claim | Registered status | Draft disposition | Reviewer result |
|---|---|---|---|
| `20-C01` | `PARTIAL` | 四维只称 source-supported course taxonomy，不称行业统一模型 | `TRACEABLE / PARTIAL_CEILING_PRESERVED` |
| `20-C02` | `CONFIRMED` | capacity、preflight、native usage与application policy分账，保持Provider scope | `TRACEABLE / WITHIN_CEILING` |
| `20-C03` | `PARTIAL` | 产品单位与课程Step不互换，到顶不等于完成/质量/安全 | `TRACEABLE / MODEL_TRANSITION_GAP / A20-R0-F01` |
| `20-C04` | `PARTIAL` | cost basis有来源，reservation明确为课程设计，actual允许UNKNOWN | `TRACEABLE / MODEL_TRANSITION_GAP / A20-R0-F01` |
| `20-C05` | `PARTIAL` | gRPC只支撑deadline/timeout窄语义，ledger/critical path标为课程设计 | `TRACEABLE / RESUME_CLOCK_GAP / A20-R0-F02` |
| `20-C06` | `PROPOSAL` | lifecycle与enforcement matrix标为未实现设计 | `TRACEABLE / INTERNAL_INVARIANT_INCOMPLETE / A20-R0-F01,F02` |
| `20-C07` | `PROPOSAL` | STOP/DEGRADE/REQUEST_APPROVAL/PARTIAL保持policy route，不越过authority/quality | `TRACEABLE / WITHIN_CEILING` |
| `20-C08` | `PROPOSAL` | record允许range/UNKNOWN，且明确不是invoice或Trace schema | `TRACEABLE / REMAINING_BASIS_INCOMPLETE / A20-R0-F01,F02` |
| `20-C09` | `PROPOSAL` | BuildPilot envelope全程DESIGN/NOT IMPLEMENTED/NOT RUN | `TRACEABLE / INHERITS_MODEL_GAPS / A20-R0-F01,F02` |

Coverage=`9 / 9`；registered mix=`1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。Evidence ceiling和语态整体正确；问题是Proposal本身必须内部一致，而不是只要标了`NOT IMPLEMENTED`就可以保留互相冲突的扣账或时钟语义。

### Evidence Card audit（11 / 11）

| Card | Source / locator | Proves / Does Not Prove / limitations | Reviewer result |
|---|---|---|---|
| `20-E01` | OpenAI Responses `POST /responses` | output cap、usage、truncation/incomplete限定于该surface | `COMPLETE / CURRENT CONTRACT VERIFIED` |
| `20-E02` | OpenAI `POST /responses/input_tokens` | preflight count operation与response usage分离 | `COMPLETE / CURRENT CONTRACT VERIFIED` |
| `20-E03` | Anthropic Token Counting + Messages | count为estimate，`max_tokens`为output cap，usage为Anthropic-native | `COMPLETE / CURRENT CONTRACT VERIFIED` |
| `20-E04` | OpenAI Agents SDK Running agents | `max_turns`限定agent-loop turns/LLM calls及`MaxTurnsExceeded` | `SEMANTICS VERIFIED / RELEASE LABEL STALE / A20-R0-F03` |
| `20-E05` | LangGraph Graph API | recursion limit限定super-step，parallel nodes可同一super-step | `SEMANTICS VERIFIED / RELEASE LABEL STALE_OR_UNBOUND / A20-R0-F03` |
| `20-E06` | gRPC Deadlines | deadline/timeout、elapsed deduction与cooperative cancellation边界 | `COMPLETE / COURSE RESUME SYNTHESIS INCOMPLETE / A20-R0-F02` |
| `20-E07` | OpenAI Organization Costs | historical organization cost surface、amount/currency/time/attribution | `COMPLETE / CURRENT CONTRACT VERIFIED` |
| `20-E08` | Anthropic Usage and Cost API | historical usage/cost Admin reconciliation与availability边界 | `COMPLETE / CURRENT CONTRACT VERIFIED` |
| `20-E09` | FOCUS 1.4 §§3.1.7/35/40 | Billed/List/Effective basis与invoiced-not-estimated边界 | `COMPLETE / VERSION-FIXED` |
| `20-E10` | course synthesis | lifecycle、routing、record与BuildPilot proposal | `CARD COMPLETE / PROPOSAL INTERNAL GAPS / A20-R0-F01,F02` |
| `20-E11` | canonical + published dependencies | Article 20/21/22 ownership与BuildPilot ceiling | `COMPLETE / REPOSITORY_SCOPE_PRESERVED` |

Current primary verification used only to adjudicate cited facts:

- OpenAI Responses仍把 `max_output_tokens`定义为包含visible output与reasoning tokens的上限，并分别暴露usage、truncation与incomplete details；input-token count仍是独立operation。
- OpenAI Agents SDK当前Running agents文档把`max_turns`描述为agent-loop turns / LLM calls，并以`MaxTurnsExceeded`表示未在上限内完成。
- Anthropic当前Token Counting明确说明count是estimate；Messages的`max_tokens`是output maximum且可能更早停止。
- LangGraph当前Graph API仍把recursion limit绑定super-steps，并说明parallel nodes可处于同一super-step。
- gRPC当前guide仍区分absolute deadline与timeout duration，并在传播时扣除已流逝时间。
- FOCUS 1.4仍要求Billed Cost反映invoice issuer的invoiced amount，而不是estimated/inferred value。

这些核验支持Draft的窄产品语义，但官方GitHub release index在本次Review显示OpenAI Agents SDK latest=`v0.22.0`（2026-08-19 release）和LangGraph latest=`1.2.11`（2026-08-11 release），与Research/Evidence写在`2026-08-26`检索边界中的`v0.17.3`/`1.2.9`不一致。

Outcome: `FAIL / REVISION_REQUIRED`

## Teaching Quality and M-Weight Pedagogy

- [x] 第一屏从“有动作authority仍不等于有资源资格”立题，没有API-first退化。
- [x] Problem space -> four-dimension model -> enforcement -> exhaustion -> audit record -> BuildPilot design -> counterexamples的主线完整。
- [x] 12个编号单元虽然偏密，但表格承担了四维比较、成本三态、路由和ownership压缩，没有形成第二条互相竞争的主线。
- [x] 10题Learning Check与答案覆盖核心区分，没有引入新Claim或运行事实。
- [x] Article 21/22的Trace/Eval桥接清楚，未预写event schema、replay algorithm、failure classes、dataset、metric或threshold。
- [ ] Step/Cost表的“consume/commit”双重语义会让读者无法从表格还原唯一扣账流程；见`A20-R0-F01`。
- [ ] “monotonic elapsed + resume”缺少clock-domain前提，Learning Check也没有测试这个高风险边界；见`A20-R0-F02`。

Draft为444行，明显高于近期M-weight文章，但四维主题本身具有必要密度，且多数重复标签承担Evidence/Runtime防误读职责。本Cycle不为篇幅单独开风格Finding；Revision应局部修复不变量，不扩成L-weight实现教程。

Outcome: `PASS_WITH_FINDING_DEPENDENCY`

## Engineering Transfer

- [x] 四维表、counting-rule checklist、cost三态、latency ledger、enforcement matrix与route table均可转化为设计审查材料。
- [x] Provider receipt保持native identity；不存在“一个通用usage schema”伪装。
- [x] Budget PASS没有替代Article 19 action authority、Article 10 legal transition或Article 11 Retry/effect eligibility。
- [x] UNKNOWN/PENDING与source freshness被保留，没有用estimate填actual。
- [ ] 缺少per-dimension single-accounting invariant时，`remaining`不能成为可执行决定依据；见`A20-R0-F01`。
- [ ] 缺少resume clock compatibility时，deadline remaining不能在long-running/checkpoint场景可靠重建；见`A20-R0-F02`。

Outcome: `FAIL / REVISION_REQUIRED`

## Readability & Compression

- [x] 标题、表格、code fences、blockquote nesting、Learning Check、references与最短结论顺序清楚。
- [x] 14条fence marker成对；Draft-stage无frontmatter/shortcode符合Publisher前边界。
- [x] 无固定price/window/service-tier/timeout值，无DATA/EXPERIENCE TODO或伪runtime输出。
- [x] BuildPilot每次具体出现均保留DESIGN/NOT IMPLEMENTED/NOT RUN或等价标签。
- [x] Claim Traceability与Job Competency表属于课程审查型附录，虽然增加篇幅，仍可机械压缩且没有破坏正文主线。

Outcome: `PASS`

## Course Continuity and Ownership Boundaries

| Owner | Article 20 usage | Explicit non-scope | Reviewer result |
|---|---|---|---|
| Article 01 | Token/Context capacity与usage基础 | 不重讲tokenization/API入门 | `PASS` |
| Article 10 | committed Step与legal transition seam | 不重定义State/Guard/commit authority | `PASS` |
| Article 11 | checkpoint/resume、Retry/effect eligibility | 不用余额批准Retry，不设计exactly-once/compensation | `PASS_WITH_CLOCK_GAP / A20-R0-F02` |
| Article 12 | Context fit/receipt与request revision seam | 不重讲assembly/pollution | `PASS` |
| Article 19 | `authority_ref`与budget-change approval seam | Budget不授予action authority或越过hard deny | `PASS` |
| Article 21 plan | 仅留下`trace_ref`和budget-local reason | 不定义cross-step Trace/Replay/Failure Taxonomy | `PASS` |
| Article 22 plan | 只留下degrade后的future quality question | 不定义Eval/Golden Dataset/Regression或质量结论 | `PASS` |

Canonical明确Article 20为M-weight Budget core，Article 21/22各为L-weight Trace/Eval owners；Draft没有越界。Glossary中的Context、State、Checkpoint、Retry、Trace、Replay与Eval定义均未被改写。Budget在本文继续被明确标为course-level model，而非行业统一taxonomy。

## Runtime, Lab and BuildPilot Boundary

| Field | Reviewed state | Result |
|---|---|---|
| Required Lab | `NONE` | `PRESERVED` |
| Experiment Count | `0` | `PRESERVED` |
| Runtime Observation | `ABSENT` | `PRESERVED` |
| BuildPilot implementation | `NOT IMPLEMENTED` | `PRESERVED` |
| BuildPilot execution | `NOT RUN` | `PRESERVED` |
| real token/cost/latency receipt or billing read | `ABSENT` | `PRESERVED` |
| reservation atomicity / race freedom | `NOT PROVEN` | `PRESERVED` |
| cost/latency/quality/safety/benefit outcome | `ABSENT` | `PRESERVED` |

本轮Findings针对design contract内部一致性与source identity，不要求Lab或Runtime，也不把未运行设计判成运行失败。

## Mechanical Publication Preflight

- Frozen SHA-256 recheck: `PASS / 064407F1841DD55AF6B1DDCC7903969AC2106EC775CEEF9F3B6454E4EE1EEFA1`
- Bytes / physical lines: `37187 / 444`
- Fences: `14` marker lines / paired
- Frontmatter / shortcode: `0 / 0`，符合Draft-stage contract
- Fixed price/window/service-tier values: `0`
- `git diff --check` for Article 20 Research/Evidence/Outline/Draft: `PASS`
- Build status: `NOT RUN AT REVIEW`；Reviewer结果不替代Publisher mapping或Hugo Build Gate
- Publication readiness: `NOT ELIGIBLE UNTIL FINDINGS CLOSED AND FINAL_GATE PASS`

## Findings

### A20-R0-F01

- ID: `A20-R0-F01`
- Status: `OPEN`
- Severity: `MAJOR`
- Category: `TECHNICAL`
- Location: `draft.md` §6 `BudgetVector`（lines 156—181）、§7 lifecycle/enforcement matrix（183—215）、§9 record（236—265）与§10 BuildPilot envelope/walk-through（267—321）；对应`research.md` Abstract model/enforcement matrix、`outline.md` units 6—10、`evidence.md` `20-E10`。
- Problem: 核心accounting model没有冻结每维唯一扣账不变量。`BudgetVector.cost`缺少`remaining`，但正文紧接着声称每维都能回答remaining；Step在“Before Step commit”被写成`admit/consume next unit`，在“After response/result”又`commit actual unit`，没有说明前者是reserve还是已经增加`used`；通用lifecycle又写`COMMIT ACTUAL or RELEASE RESERVATION`，而Cost段同时规定actual可能要等历史billing record并保持pending。当前shape因此无法判断同一消耗是被算一次还是两次，也无法判断billing pending时reservation应保留、部分commit还是release。
- Supporting Evidence: Draft lines 163—168的四维shape只有Cost没有remaining；lines 171—179声称每维都能回答remaining；lines 195、206、208同时使用reserve/consume/commit actual；lines 257—260允许actual pending与remaining range，却未给出outstanding reservation/pending charge如何进入remaining。Article 10把课程Step定义为committed loop iteration或本地可审计单元，因此“admitted counts”与“after result commit”不能同时保持未解释。`20-E10`本身也把atomicity/race freedom列为未证明，不能用实现细节替代当前设计不变量。
- Why It Matters: 本篇的中心承诺是把usage report升级为可执行Budget contract。如果同一Step/Cost可能双扣、漏扣或在账单未到时释放hold，admission、exhaustion与audit record都会给出不同决定；读者无法从文章安全迁移这套模型，`20-C03/C04/C06/C08/C09`的Proposal内部一致性不成立。
- Required Disposition: 在不新增核心Claim或Lab的前提下，统一修订Research/Evidence/Outline/Draft：为每维冻结最小state transition与single-accounting invariant；给Cost加入`remaining: value_or_range`及其basis；明确pre-action是check/reserve还是consume，何时唯一增加Step `used`；response后如何把reservation转换为measured/incurred pending amount、释放未使用部分，并在source-qualified cost actual到达前保守计算remaining且避免double count。同步修正BuildPilot envelope、enforcement matrix、record与Learning Check。不得仅补一个`remaining`字段而保留冲突时点。

### A20-R0-F02

- ID: `A20-R0-F02`
- Status: `OPEN`
- Severity: `MAJOR`
- Category: `TECHNICAL`
- Location: `draft.md` §5 latency ledger/clock boundary（128—154）、§6 latency vector（156—181）、§7 queue dequeue/resume（183—215）与§10 walk-through step 5（307—319）；对应`research.md` `20-C05/C06/C08`、`outline.md` units 5—10、`evidence.md` `20-E06/E10`。
- Problem: Draft要求elapsed使用monotonic clock，并要求checkpoint/resume加载deadline identity后扣除elapsed重算remaining，但没有限定monotonic timestamp的clock domain。进程重启、机器迁移、系统reboot或不同host上的monotonic origin不可直接比较；wall-clock deadline又可能受clock skew/adjustment影响。`clock_basis`一个字段名不足以说明哪些值可相减、跨哪个resume boundary仍有效，以及无法比较时应如何fail closed。
- Supporting Evidence: Draft line 150只给出“elapsed用monotonic、wall-clock承载deadline identity”；lines 205/211/313要求resume后deduct elapsed/recompute remaining，却没有boot/host/clock-domain identity、checkpoint segment或uncertainty route。gRPC官方Deadlines guide只确认deadline/timeout区分，并说明跨server传播时转换为已经扣除elapsed的timeout以避免clock skew；它不证明本文的持久化Agent resume clock protocol。Article 11的checkpoint/resume边界使该缺口成为本篇必须显式处理的工程条件。
- Why It Matters: Latency Budget的hard limit依赖remaining。如果resume后remaining可能因不可比较的时钟被放大、缩小或重置，系统既可能超时继续工作，也可能错误耗尽；queue/service/critical-path审计也无法重放。这直接影响`20-C05/C06/C08/C09`与Long-running engineering transfer。
- Required Disposition: 把clock contract收窄并写进Research/Outline/Draft/record/envelope：至少声明clock-domain/host/boot identity与checkpoint segment；same-domain内用monotonic delta，跨进程/host/reboot时使用明确的persisted absolute deadline + current clock/uncertainty policy或保守fail-closed route，并记录`observed_at/clock_basis/uncertainty`。若课程不准备设计跨domain算法，就明确只保证same-clock-domain，并把其他resume标`UNKNOWN/BLOCKED/STOP`。不得把gRPC传播语义外推为已解决的Agent resume机制。

### A20-R0-F03

- ID: `A20-R0-F03`
- Status: `OPEN`
- Severity: `MINOR`
- Category: `EVIDENCE`
- Location: `research.md` Source and drift register lines 28/31；`evidence.md` `20-E04` Source identity与`20-E05` Source identity/Does not prove；Draft references依赖这些Cards的current-source标签。
- Problem: Source package声明在`2026-08-26`检索时OpenAI Agents SDK current release为`v0.17.3`，并用LangGraph `1.2.9`作为同日repository release observation。官方GitHub release index在本次同日Review显示OpenAI Agents SDK latest=`v0.22.0`（released 2026-08-19）和LangGraph latest=`1.2.11`（released 2026-08-11）。两者都早于声明的retrieval date；因此current/release identity标签不可复核。Hosted docs语义本身仍成立，但不能靠错误或任意旧release号提供版本上下文。
- Supporting Evidence: [OpenAI Agents SDK releases](https://github.com/openai/openai-agents-python/releases)列出`v0.22.0`为Latest；[LangGraph releases](https://github.com/langchain-ai/langgraph/releases)列出`langgraph==1.2.11`，并显示后续于`1.2.9`的`1.2.10/1.2.11`。Current [OpenAI Running agents](https://openai.github.io/openai-agents-python/running_agents/)和[LangGraph Graph API](https://docs.langchain.com/oss/python/langgraph/graph-api#recursion-limit)仍支持Draft使用的窄语义，但package已经明确承认hosted docs不证明来自所观察tag。
- Why It Matters: Course Evidence contract要求moving product fact的source identity、retrieval date与drift boundary可重放。错误的same-day “current release”标签会让后续Reviewer误判文档版本，也削弱Draft在references中对current-source freshness的承诺。
- Required Disposition: 重新读取官方release index并选择一种最小闭环：若正文只依赖current hosted docs，删除不提供证明力的release号，只保留retrieval date与moving-doc boundary；若要保留版本，则更新为真实current release并提供可重放的tag/source locator，逐项确认cited semantics属于该版本。同步Research/Evidence及必要的Draft source note；不得只改一个数字而继续暗示hosted docs已与该tag绑定。

本Cycle未对任何Finding标`CLOSED`，也未预写Revision Disposition。

## Five-Dimension Score

| Dimension | Score | Artifact basis |
|---|---:|---|
| Technical Accuracy | `16 / 20` | 四维语义边界总体准确，但accounting transition与resume clock contract各有一项核心MAJOR缺口。 |
| Evidence Discipline | `17 / 20` | 9/9 Claims、11/11 Cards与ceiling完整；两项same-day moving-source release identity不可复核。 |
| Teaching Quality | `17 / 20` | Problem-first、model、BuildPilot和Learning Check完整；两项核心表尚不能给出唯一工程解释。 |
| Engineering Transfer | `16 / 20` | Review matrices可迁移，但remaining/accounting与cross-resume clock仍可能产生错误实现。 |
| Readability & Compression | `17 / 20` | M-weight偏长但结构清楚、表格压缩有效；重复boundary仍在可接受范围。 |
| **Total** | **`83 / 100`** | **Total与Technical/Evidence/Engineering Transfer均未达到课程阈值。** |

Threshold check: Total `83 < 88`、Technical `16 < 18`、Evidence `17 < 18`、Teaching `17 >= 17`、Engineering `16 < 17`。Result=`THRESHOLDS NOT ALL MET`。

## Open Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `2` | `A20-R0-F01`, `A20-R0-F02` |
| MINOR | `1` | `A20-R0-F03` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`3`** | **`A20-R0-F01`—`A20-R0-F03`** |

## Gate Decision

`FAIL / REVISION_REQUIRED`

- Review execution: `COMPLETE`
- Review Gate Decision: `FAIL`
- Outcome: `REVISION_REQUIRED`
- Open Findings: `3`（BLOCKER 0 / MAJOR 2 / MINOR 1 / EDITORIAL 0）
- Score: `83 / 100`（Total、Technical Accuracy、Evidence Discipline与Engineering Transfer thresholds未满足）
- Next Allowed Gate: `REVISION`
- Blocker: `NONE`
- Gate completed: `true`
- Exact route: `REVIEW -> REVISION -> REVIEW_RECHECK`
- Final Gate: `NOT_REACHED`
- Scope guard: Findings可在当前Article的frozen claim scope内修复；它们不授权Reviewer改正文，不要求Lab/Runtime或future Article，不构成human stop。

## Recheck Contract

Fresh `REVIEW_RECHECK`必须逐项只依据原Finding、Revision Disposition、变更后artifact与必要current evidence返回`OPEN / CLOSED / ESCALATED`：

1. `A20-R0-F01`: 验证Cost `remaining`、Step/Cost reserve/consume/commit/pending transitions与single-accounting invariant在Research/Evidence/Outline/Draft/BuildPilot/record中一致。
2. `A20-R0-F02`: 验证clock domain、checkpoint segment、cross-resume uncertainty/fail-closed规则存在，且未把gRPC窄语义外推为Runtime证明。
3. `A20-R0-F03`: 验证moving-source release identity已删除或更新为可重放的真实边界，并与Draft reference posture一致。
4. 重新计算五维score；只有`0 actionable Findings`且所有threshold通过，才可路由`FINAL_GATE`。

## Revision Disposition Candidates｜Cycle 0

### A20-R0-F01

- Finding ID: `A20-R0-F01`
- Files Changed: `research.md`, `evidence.md`, `outline.md`, `draft.md`
- What Changed: 在 `research.md` Abstract model / `20-C03` / `20-C04` / lifecycle / record / BuildPilot / enforcement matrix，`evidence.md` Claim Register 与 `20-E10`，`outline.md` units 3/4/6/7/9/10、counterexamples、Claim coverage 与 Learning Check，以及 `draft.md` §§3/4/6/7/9/10、counterexamples、Claim Traceability 与 Learning Check 冻结单值分账。一个 `consumption_id` 同时只占一个 bucket；course Step pre-admission 只 reserve，`remaining_to_admit = limit - used - in_flight_reserved`，只在 successful Step commit 以同一 `step_attempt_id` 唯一 `used + 1`，abort-before-commit release。Cost 以同一 `charge_id` 在 reservation -> measured/incurred-pending -> source-qualified actual/released 间 replace；`remaining = limit - settled_actual - conservative(outstanding)`，hard admission 使用 upper bound，缺 finite bound 即 `UNKNOWN/STOP`；只 release unused/aborted/proven-absent amount。
- Evidence Impact: 不新增或升级 Claim/Card；`20-C03/C04/C06/C08/C09` status 不变，设计 refinement 仍由 `20-E10 PROPOSAL` 承担；Provider/FOCUS narrow facts 与 BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN` ceiling 不变。
- Proposed Status: `READY_FOR_RECHECK`

### A20-R0-F02

- Finding ID: `A20-R0-F02`
- Files Changed: `research.md`, `evidence.md`, `outline.md`, `draft.md`
- What Changed: 在 `research.md` `20-C05` / BudgetVector / record / BuildPilot / enforcement matrix，`evidence.md` `20-C05` wording、`20-E06 Does not prove` 与 `20-E10`，`outline.md` units 5/6/7/9/10、counterexamples、Claim coverage 与 Learning Check，以及 `draft.md` §§5/6/7/9/10、counterexamples、Claim Traceability 与 Learning Check 冻结 `clock_domain_id / host_id / boot_id / checkpoint_segment_id`。只允许 compatible same-domain monotonic delta；process boundary 重验 identity。跨不兼容 process/host/reboot 使用 persisted absolute deadline、current trusted wall clock 与 uncertainty policy，按 `safe_remaining = max(0, absolute_deadline - current_wall_clock - uncertainty_bound)` 保守计算；trust/uncertainty 无法界定则 `remaining=UNKNOWN`，hard latency policy `BLOCKED/STOP`。phase receipt 不重复扣 end-to-end remaining。
- Evidence Impact: 不新增或升级 Claim/Card；`20-C05/C06/C08/C09` ceiling 不变。`20-E06` 只继续证明 gRPC deadline/timeout 与 elapsed propagation 的窄语义；persisted Agent resume clock contract 仍明确为 `20-E10 COURSE PROPOSAL / NOT IMPLEMENTED`。
- Proposed Status: `READY_FOR_RECHECK`

### A20-R0-F03

- Finding ID: `A20-R0-F03`
- Files Changed: `research.md`, `evidence.md`, `outline.md`, `draft.md`
- What Changed: `research.md` Source and drift register、Evidence Gate provider discipline，`evidence.md` `20-E04/E05` Source identity/URLs/limitations 与 Evidence Gate source discipline，`outline.md` Source plan，以及 `draft.md` References 现保存可重放的 official release/tag snapshots：OpenAI Agents SDK [`v0.22.0`](https://github.com/openai/openai-agents-python/releases/tag/v0.22.0)，released `2026-08-19`；LangGraph [`1.2.11`](https://github.com/langchain-ai/langgraph/releases/tag/1.2.11)，released `2026-08-11`。两处均以 `2026-08-26` retrieval 为边界，并明确 exact release identity 不证明 hosted docs 由对应 tag 构建。
- Evidence Impact: `20-E04/E05` 的 product semantics、PARTIAL contribution、Does Not Prove 与 no-runtime boundary 不变；仅修复 moving-source release identity 和 replayability，未把 hosted docs 绑定到 tag，未新增 Evidence Card。
- Proposed Status: `READY_FOR_RECHECK`

## Cycle 1 Recheck｜2026-08-26

### Recheck Identity and Frozen Input

- Reviewer: fresh Reviewer `/root/article20_reviewer_recheck1`
- Gate: `REVIEW_RECHECK`
- Cycle: `1 / 3`
- Context isolation: 只读取原始 Findings、持久化 Revision Disposition、变更后的 Research/Evidence/Outline/Draft、dispatch contract、canonical/boundary artifacts 与必要 current primary evidence；未读取或依赖 Revision hidden reasoning、confidence 或 self-score。
- Revised Draft SHA-256: `031B873C7C027D22E0D7EB9649D96CFE222AAACBF9EE19CE89B3C7C9F4759E49`
- Revised Draft identity: `44197` bytes / `475` physical lines
- Frozen evidence shape: Claims=`9`；Evidence Cards=`11`；new core Claim/Card=`NONE`
- Frozen runtime boundary: Required Lab=`NONE`；Experiments=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`

### Finding Decisions

| Finding | Cycle 1 decision | Independent recheck basis |
|---|---|---|
| `A20-R0-F01` | `CLOSED` | Research、Evidence `20-E10`、Outline与Draft均冻结一个 identity 同时只占一个 accounting bucket 的 replace invariant。Step pre-admission 只按 `step_attempt_id` reserve，`remaining_to_admit = limit - used - in_flight_reserved`，只在 successful Article 10 Step commit 唯一 `used + 1`，abort-before-commit release。Cost 已有 `remaining` 与 `limit - settled_actual - conservative(outstanding)` basis；同一 `charge_id` 只在 reservation、incurred-pending、source-qualified actual 或 released 中一个 bucket，response/result 后 replace 为 pending、仅 release unused/aborted/proven-absent amount，hard admission 使用 outstanding upper bound，缺 finite bound 则 `UNKNOWN/STOP`。Enforcement matrix、minimum record、BuildPilot envelope/walk-through、counterexample与 Learning Check 全部保持同一转换语义。 |
| `A20-R0-F02` | `CLOSED` | 四份 artifact 均记录 `clock_domain_id / host_id / boot_id / checkpoint_segment_id`；只有 compatible same-domain monotonic stamps 可相减，process boundary 必须重验 identity。跨不兼容 process/host/reboot 使用 persisted absolute deadline、current trusted wall clock 与 bounded uncertainty，按 `safe_remaining = max(0, absolute_deadline - current_wall_clock - uncertainty_bound)` 保守计算；trust/uncertainty 无法界定时 `remaining=UNKNOWN` 且 hard latency policy `BLOCKED/STOP`。phase receipt 不重复扣 end-to-end remaining。`20-E06` 和正文明确 gRPC 只支撑 deadline/timeout 与 elapsed propagation 的窄语义；persisted Agent resume contract 仍是 `20-E10 COURSE PROPOSAL / NOT IMPLEMENTED`。 |
| `A20-R0-F03` | `CLOSED` | 2026-08-26 对 official GitHub release API/tag 的独立核验确认 OpenAI Agents SDK [`v0.22.0`](https://github.com/openai/openai-agents-python/releases/tag/v0.22.0) published `2026-08-19`，且为当时 latest release；LangGraph [`1.2.11`](https://github.com/langchain-ai/langgraph/releases/tag/1.2.11) published `2026-08-11`，且为当时 newest listed `langgraph` package release。Research、Evidence、Outline与Draft使用相同 exact tag URLs/dates，并明确 release snapshot 只提供 replayable identity，不证明 current hosted docs 由该 tag 构建。 |

Decision detail:

- `A20-R0-F01`: `CLOSED / REQUIRED DISPOSITION SATISFIED`。没有新增 Claim/Card，也没有把 Proposal 升级为 Runtime 证明；atomic reservation 与 race freedom继续明确为未证明。
- `A20-R0-F02`: `CLOSED / REQUIRED DISPOSITION SATISFIED`。same-domain 与 cross-domain 两条规则均可从 record/envelope 恢复；无法形成有限 uncertainty bound 时没有伪造 remaining。
- `A20-R0-F03`: `CLOSED / REQUIRED DISPOSITION SATISFIED`。tag identity、URL、发布日期和 hosted-doc non-binding posture 可重放且四份 artifact 一致。
- New or escalated Finding: `NONE`。

### Recheck Coverage and Boundaries

- Claims: `20-C01`—`20-C09`，coverage=`9 / 9`；status mix仍为 `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`。
- Evidence Cards: `20-E01`—`20-E11`，count=`11`；new core Claim/Card=`NONE`。
- Article method: M-weight Principle结构继续按 problem space -> abstract model -> concrete BuildPilot design -> engineering/verification boundary推进；没有 API-first 开场，也没有停在抽象层。
- Article 21 boundary: 只保留 `trace_ref` 与 budget-local decision seam；未定义 cross-step event schema、correlation、reconstruction/re-execution algorithm或 Failure Taxonomy。
- Article 22 boundary: 只留下未来 quality/regression question；未定义 Eval dataset、metric、threshold、grader或 regression verdict。
- BuildPilot: 所有 concrete design均保持 `DESIGN / NOT IMPLEMENTED / NOT RUN` 或同义强标签；没有 Provider call、billing read、queue simulation、deadline test、Runtime receipt、收益或 production Claim。
- Mechanical recheck: Draft hash/bytes/lines=`PASS`；fence markers=`16 / paired`；frontmatter=`0`；shortcode=`0`；Draft-stage publication mapping与 Hugo Build仍属于后续 Publisher Gate。

### Five-Dimension Score｜Cycle 1

| Dimension | Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `19 / 20` | Step/Cost single-accounting与Latency跨域fail-closed合同现已单值、跨artifact一致；保留未实现、并发原子性与不可见阶段边界。 |
| Evidence Discipline | `19 / 20` | 9/9 Claims、11/11 Cards、status ceilings与Does Not Prove完整；两个moving release snapshot已由official tag/API独立复核且不绑定hosted docs。 |
| Teaching Quality | `18 / 20` | 问题空间、四维抽象、enforcement、BuildPilot walk-through与10题Learning Check完整覆盖新不变量；M-weight密度较高但主线唯一。 |
| Engineering Transfer | `18 / 20` | matrix、record、envelope、remaining公式、identity transition与clock fail-closed route足以迁移到设计审查；实现/atomicity仍诚实留待后续。 |
| Readability & Compression | `17 / 20` | 475行偏长且术语密集，但表格、code sketch、反例、ownership与最短结论层次清楚，没有形成第二主线。 |
| **Total** | **`91 / 100`** | **全部现行课程阈值通过。** |

Threshold check: Total `91 >= 88`；Technical `19 >= 18`；Evidence `19 >= 18`；Teaching `18 >= 17`；Engineering `18 >= 17`。Result=`ALL THRESHOLDS MET`。

### Open Finding Summary｜After Cycle 1

| Severity | Open / escalated count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `0` | `NONE` |
| MINOR | `0` | `NONE` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`0`** | **`NONE`** |

### Recheck Gate Decision

`PASS / READY_FOR_FINAL_GATE`

- Recheck execution: `COMPLETE`
- Recheck Gate Decision: `PASS`
- Finding decisions: `A20-R0-F01 CLOSED`；`A20-R0-F02 CLOSED`；`A20-R0-F03 CLOSED`
- Open / escalated Findings: `0`
- Score: `91 / 100`；all thresholds met
- Gate completed: `true`
- Next Allowed Gate: `FINAL_GATE`
- Blocker: `NONE`
- Exact route: `REVIEW_RECHECK -> FINAL_GATE`
- Publication/Build status: `NOT YET RUN`；本决议不是 Publisher、Hugo Build、commit、push或remote verification结果。

## Final Gate Decision

### Final Gate Identity

- Reviewer: fresh Reviewer `/root/article20_final_reviewer`
- Review date: `2026-08-26`（Asia/Shanghai）
- Gate: `FINAL_GATE`
- Execution: `REAL_SUBAGENT / FRESH INDEPENDENT REVIEWER`
- Context isolation: 独立读取 repository instructions、Factory / Reviewer contracts、canonical、glossary、Article 20 全部 durable artifacts、Cycle 0 Findings、Revision Dispositions、Cycle 1 closure、FINAL_GATE dispatch 与必要 current official sources；未读取或依赖 Author、Revision Worker 或前序 Reviewer 的 hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只向 `review.md` 追加本 Final Gate Decision，并在已有 `FINAL_GATE` dispatch 下向 `subagent-trace.md` 追加一个 canonical raw Reviewer Result；未修改 Draft、Research、Evidence、Outline、README、Published Content、global/canonical、Git 或 future Article。

### Frozen Input and Review Closure

- Frozen Draft SHA-256: `031B873C7C027D22E0D7EB9649D96CFE222AAACBF9EE19CE89B3C7C9F4759E49` — independently recomputed `PASS`.
- Frozen Draft identity: `44197` bytes；`475` physical lines.
- Cycle 0 Findings: `A20-R0-F01 MAJOR`、`A20-R0-F02 MAJOR`、`A20-R0-F03 MINOR`.
- Cycle 1 decisions: `A20-R0-F01 CLOSED`；`A20-R0-F02 CLOSED`；`A20-R0-F03 CLOSED`.
- Current Finding state: `0 OPEN / 0 ESCALATED / 3 CLOSED`；no new Final Gate Finding opened.
- Review cycle: `1 / 3`；review-cycle exhaustion not reached.

### Independent Final Gate Audit

| Gate requirement | Independent result | Basis |
|---|---|---|
| Claim integrity | `PASS` | `20-C01`—`20-C09` = `9 / 9` unique Claims；status mix remains `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`；new core Claim=`NONE`。 |
| Evidence integrity | `PASS` | `20-E01`—`20-E11` = `11 / 11` Evidence Cards；Proves / Does Not Prove / limitations 与 Draft wording ceiling对齐；new Evidence Card=`NONE`。 |
| `A20-R0-F01` single accounting | `PASS` | 同一 `consumption_id` / `charge_id` 仅占一个 bucket；Step pre-admission 只 reserve，只在 successful committed Step 以同一 `step_attempt_id` 唯一 `used + 1`；Cost 在 reservation -> incurred-pending -> source-qualified actual / released 间 replace，`remaining = limit - settled_actual - conservative(outstanding)`，无 finite upper bound 则 `UNKNOWN/STOP`。 |
| `A20-R0-F02` latency clock contract | `PASS` | compatible same-domain monotonic stamps 才可相减；process boundary 重验 `clock_domain_id / host_id / boot_id / checkpoint_segment_id`；跨不兼容域使用 persisted absolute deadline + trusted current wall clock + bounded uncertainty，不可界定则 `UNKNOWN/BLOCKED/STOP`；phase receipt 不重复扣 end-to-end remaining。 |
| `A20-R0-F03` moving-source posture | `PASS` | Official GitHub release API/tag 重新核验 OpenAI Agents SDK `v0.22.0` published `2026-08-19`，且当前 latest release 仍为该 tag；LangGraph `1.2.11` published `2026-08-11`，仍是当前 release list 中最新的 `langgraph` package release。Artifacts 正确保留 hosted-doc-to-tag binding `NOT PROVEN`。 |
| Fixed-number and Provider boundary | `PASS` | Draft 没有当前价格、Context Window、service-tier、timeout 或 deadline 数值；OpenAI、Anthropic、OpenAI Agents SDK、LangGraph 与 FOCUS 语义保持 Provider/product/spec native scope，没有伪造 universal usage/billing/Step schema。 |
| Runtime / Lab boundary | `PASS` | Required Lab=`NONE`；Experiments=`0`；Runtime Observation=`ABSENT`；no Provider call、billing read、queue simulation、deadline test 或 runtime receipt 被声称。 |
| BuildPilot boundary | `PASS` | `DESIGN / NOT IMPLEMENTED / NOT RUN`；没有 implementation、atomicity/race-freedom、cost/latency/quality/safety/benefit 或 production evidence claim。 |
| Course ownership | `PASS` | Article 20 只拥有资源 admission、accounting、exhaustion 与 reconciliation；Article 21 保留 cross-step Trace/Replay/Failure Taxonomy，Article 22 保留 Eval/Golden Dataset/Regression；只消费 `trace_ref` 和 future quality question seam。 |
| Article method | `PASS` | Draft 遵守 problem space -> abstract model -> concrete BuildPilot design -> engineering/verification boundary，非 API-first，且用最短结论收口。 |
| Mechanical publication preflight | `PASS` | Draft hash/bytes/lines与冻结值相同；`16` fence markers paired；trailing-whitespace lines=`0`；frontmatter=`0`；shortcode=`0`；placeholder/TODO hits=`0`；Published Content 在 PUBLISH 前仍不存在。 |

### Final Score Threshold Check

| Dimension | Score | Threshold | Result |
|---|---:|---:|---|
| Technical Accuracy | `19 / 20` | `>= 18` | `PASS` |
| Evidence Discipline | `19 / 20` | `>= 18` | `PASS` |
| Teaching Quality | `18 / 20` | `>= 17` | `PASS` |
| Engineering Transfer | `18 / 20` | `>= 17` | `PASS` |
| Readability & Compression | `17 / 20` | `N/A` | `PASS` |
| **Total** | **`91 / 100`** | **`>= 88`** | **`PASS`** |

Threshold result: `ALL REQUIRED SCORE THRESHOLDS MET`.

### Publication Mechanics and Routing

- FINAL_GATE 只验证 frozen knowledge artifact；不添加 Hugo frontmatter、navigation、series metadata 或 Published Content，也不代替 Publisher / Build Gate。
- Publisher 只可将精确冻结 Draft 机械映射到 `content/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md`，保持语义同一性，按仓库 YAML / ASCII-shortcode 规则添加发布载体，并独立执行 publication / Hugo Build 验证。
- Publisher / Build PASS 仍不等于 `PUBLISHED` 或 `END_ARTICLE`；Master 仍需完成 global reconciliation、one-Article completion commit、single `main` push、remote verification 与 read-only post-commit reconciliation。
- Article 21 / 22 均在本 worker execution 之外；本决议唯一合法即时路由为 `FINAL_GATE -> PUBLISH`。

### Decision

`PASS / ELIGIBLE_FOR_PUBLISH`

- FINAL_GATE execution: `COMPLETE`
- Gate decision: `PASS`
- Open Findings: `0`
- Escalated Findings: `0`
- Score: `91 / 100`
- Thresholds: `ALL MET`
- Frozen Draft: `031B873C7C027D22E0D7EB9649D96CFE222AAACBF9EE19CE89B3C7C9F4759E49`
- Gate completed: `true`
- Next Allowed Gate: `PUBLISH`
- Blocker: `NONE`
- Exact route: `FINAL_GATE -> PUBLISH`
- Lifecycle implication: Article 20 is eligible to enter `FINAL` and be handed to Publisher；this decision does not itself publish、build、mutate global state、commit、push、resolve `END_ARTICLE`，or authorize Article 21/22 work.
