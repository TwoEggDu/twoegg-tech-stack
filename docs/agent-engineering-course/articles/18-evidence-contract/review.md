# Review｜Article 18 Evidence Contract

## Review Identity

- Reviewer: fresh Reviewer `/root/article18_reviewer_cycle0`
- Review date: `2026-08-25`（Asia/Shanghai）
- Gate: `REVIEW`
- Cycle: `0`（initial independent review）
- Mode: `NORMAL_ARTICLE`
- Course Weight: `L / Deep Core Lesson`
- Required Lab: `NONE`
- Context isolation: 仅依据 durable repository artifacts、已发布依赖文章与当前 primary / official sources 审查；未读取或依赖 Author hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只修改 `review.md` 并向 Article 18 `subagent-trace.md` 追加一个 canonical Worker Result；未修改 Draft、Outline、Research、Evidence、Card、README、global/canonical/published/future-Article artifacts。

## Frozen Review Input

- Draft: `docs/agent-engineering-course/articles/18-evidence-contract/draft.md`
- SHA-256: `F6CD06C0CC98D310A5617CADC2E2FEDFE1F1657CC30790EF3A63D8BFD2924646`
- Bytes: `31943`
- Physical lines: `386`
- Canonical title: `Evidence Contract：把自然语言推断变成可审计工程数据`
- Claim register: `10` Claims，`8` Evidence Cards，core `BLOCKED=0`
- Runtime state: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`

## Review Status

`REVIEW_CYCLE_0_COMPLETE / PASS / FINAL_GATE_ELIGIBLE`

Draft遵循 problem space -> abstract model -> concrete engineering design -> boundary / verification -> engineering judgment。它将结构合法性、来源/Provenance、support mapping、qualitative Confidence、policy Acceptance 与 action authority 分开，并始终把课程自定义 record、semantic gates、lifecycle/conflict policy 与 BuildPilot package 保持为 Proposal / Design。

独立复核未发现新的 unsupported core Claim、证据强度升级、伪造运行事实、benefit/production叙事、跨篇 ownership 越界或 publish-blocking Hugo风险。全部五维分数达到阈值，open Findings=`0`，因此本轮直接路由 `FINAL_GATE`。

## Technical Accuracy

- [x] Parse、Schema、source resolution、Provenance、applicability、support mapping、counter-evidence、limitations/falsifier 与 policy decision 是不同审查问题；Draft没有把 schema-valid object 写成 truth 或 accepted Claim。
- [x] Claim、Evidence、Observation、Inference、Proposal、Unknown 六种角色明确标为 `18-C02 = PROPOSAL`，没有冒充跨行业统一 ontology。
- [x] Citation、Provenance、qualitative Confidence 与 Acceptance 保持分账；Confidence明确是未校准课程 scheme，不是概率、真实性或批准。
- [x] Semantic Acceptance Chain 与 append-only revision/conflict policy均是 course design；W3C PROV、SLSA与NIST audit controls只作窄范围 precedent，不被写成完整 agent Evidence Store 标准。
- [x] `BLOCKED / Unknown`用于缺失或冲突输入的 fail-closed disposition，不被写成已完成诊断。
- [x] BuildPilot package是 synthetic design shape；sources/observations为空时 walkthrough停止，没有虚构root cause、Runtime、Trace、accuracy、latency、cost、benefit或production result。

Outcome: `PASS`

## Evidence Discipline

### Claim mapping and wording ceilings

| Claim | Registered status | Draft disposition | Reviewer result |
|---|---|---|---|
| `18-C01` | `CONFIRMED` | 只确认fluency / parse / schema validity不足以独自建立Evidence support或policy acceptance | `TRACEABLE / WITHIN_CEILING` |
| `18-C02` | `PROPOSAL` | 六种陈述角色明确称课程工作定义 | `TRACEABLE / WITHIN_CEILING` |
| `18-C03` | `PROPOSAL` | 七组字段明确称course proposal、非标准强制schema、未实现 | `TRACEABLE / WITHIN_CEILING` |
| `18-C04` | `PARTIAL` | identity、version/time、scope有直接precedent；limitations/falsifier明确为课程扩展 | `TRACEABLE / PARTIAL_CEILING_PRESERVED` |
| `18-C05` | `PARTIAL` | Provenance/Acceptance分离有来源；四分法与Confidence scheme不称标准或校准事实 | `TRACEABLE / PARTIAL_CEILING_PRESERVED` |
| `18-C06` | `PROPOSAL` | conflict/stale/partial retain + re-review明确为未实现政策 | `TRACEABLE / WITHIN_CEILING` |
| `18-C07` | `PROPOSAL` | semantic gates、顺序与fail-closed disposition明确为course design | `TRACEABLE / WITHIN_CEILING` |
| `18-C08` | `PROPOSAL` | append/supersede/invalidate/review/current projection不称标准状态机或已实现基础设施 | `TRACEABLE / WITHIN_CEILING` |
| `18-C09` | `PROPOSAL` | BuildPilot package保持`DESIGN / NOT IMPLEMENTED / NOT RUN` | `TRACEABLE / WITHIN_CEILING` |
| `18-C10` | `CONFIRMED` | 只确认course-local ownership，不外推行业命名 | `TRACEABLE / WITHIN_CEILING` |

Coverage: `10 / 10`；`CONFIRMED=2 / PARTIAL=2 / PROPOSAL=6 / BLOCKED=0`；new core Claim=`NONE`。

### Evidence Card audit

| Card | Source identity / version / locator | Proves boundary | Does Not Prove / Limitations boundary | Reviewer result |
|---|---|---|---|---|
| `18-E01` | JSON Schema Core + Validation, fixed Draft `2020-12`, Validation §3 / Overview | structural validity与Evidence acceptance可分 | 不标准化Article 18 gate order；允许custom vocabulary但不验证内容真值 | `COMPLETE / LOCATOR_VERIFIED` |
| `18-E02` | W3C PROV-DM Recommendation `2013-04-30`, §§2–3、§5.1.8、§5.2.2、§5.2.4、§5.4.1 | provenance、revision/invalidation、bundle及downstream assessment可分 | 不提供universal schema、confidence scale、acceptance policy或source truth | `COMPLETE / LOCATORS_VERIFIED` |
| `18-E03` | `in-toto/attestation` tag `v1.0`, Statement + Resource Descriptor schema/fields/parsing | subject、predicate type、digest、URI/content是不同audit dimensions | well-formed Statement或URI不自动证明predicate truth；agent-domain映射是类比 | `COMPLETE / PIN_VERIFIED` |
| `18-E04` | SLSA Specification `v1.2`, Provenance、Verifying Artifacts、VSA fields | subject matching、trust/expectations、verifier/policy/result分账 | 不把binary VSA结果复制为agent reasoning标准；不证明任意事实Claim | `COMPLETE / VERSION_AND_FIELDS_VERIFIED` |
| `18-E05` | OpenTelemetry Specification snapshot `1.60.0`, Trace API Span/SpanContext/Links/Events/Status | execution correlation与Claim acceptance是不同责任 | 不证明trace完整、真实、可重放或足以重建失败；moving-source限制明确 | `COMPLETE / SNAPSHOT_VERIFIED` |
| `18-E06` | NIST AI RMF `1.0`, NIST AI 100-1, §5.3 MEASURE / TEVV | documented、contextual、repeatable measurement比单个Claim acceptance更广 | 不提供Article 22 dataset/grader/regression或BuildPilot metrics | `COMPLETE / PARTIAL_CEILING_VERIFIED` |
| `18-E07` | NIST SP 800-53 Rev. `5.1` OSCAL-derived PDF, AU-3/AU-8/AU-9/AU-11 | audit content、time、protection、retention是独立controls | 不要求所有系统实现NIST controls、immutable storage或课程schema | `COMPLETE / CONTROL_LOCATORS_VERIFIED` |
| `18-E08` | TechStackShow baseline `272ff0e24450ead78ff959dd019da202593a518d` + current master-owned Article 18 card | course ownership、upstream continuity、Required Lab与BuildPilot posture | course-local authority不证明行业taxonomy或Runtime/Evidence Store存在 | `COMPLETE / REPOSITORY_SCOPE_VERIFIED` |

每张Card都具备`Proves`、`Does Not Prove`、`Limitations`、Falsifier、source scope及retrieval/version信息；没有Card把source presence升级成truth或approval。Spot-check使用的是固定/标版primary source，moving OpenTelemetry source保留访问快照与漂移限制。

Outcome: `PASS`

## Teaching Quality

- [x] 开篇用明确标注的synthetic BuildPilot诊断揭示“有JSON/引用/Trace仍无法复核”的问题，再引出抽象模型，而非从字段清单起讲。
- [x] 六种陈述角色、七组record fields、semantic gates与lifecycle分别回答“是什么、为什么、如何审、如何演化”，认知层次清楚。
- [x] `Citation != Provenance != Confidence != Acceptance`、`Claim != Evidence != Observation != Inference`两组不等式形成可复述的审查框架。
- [x] 具体BuildPilot package walk-through在缺少exact build identity时停止，给读者展示fail-closed judgment，而不是只给理想模板。
- [x] Learning Check及参考答案覆盖10项Claim的关键区分；答案没有引入新事实或运行结论。
- [x] 重复的Proposal / PARTIAL / NOT RUN标记服务本篇Evidence纪律；在L-weight范围内未形成需要Revision的冗余主线。

Outcome: `PASS`

## Engineering Transfer

- [x] 读者可以将source manifest、Observation、Inference、alternatives、limitations/falsifier、policy review event拆成可独立检查的数据责任。
- [x] 语义Gate为实现者提供明确停止条件，同时承认物理pipeline和字段命名仍需具体系统设计。
- [x] stale/conflict/partial不静默覆盖、current view由保留记录投影等规则可迁移到诊断、发布、审计和incident workflows。
- [x] BuildPilot package只提供minimum design contract，不虚构已实现平台；从课程模型到工程实现的缺口被显式保留。
- [x] Job Competency映射落到schema/design review、evidence audit、diagnostic reasoning、governance与cross-system architecture，并逐项写清Evidence ceiling。

Outcome: `PASS`

## Readability & Compression

- [x] L-weight篇幅与深核主题匹配；问题、模型、字段、Gate、lifecycle、BuildPilot、边界、Learning Check依次推进。
- [x] 表格用于字段、Gate、boundary、traceability和competency等高密度映射，避免把同类约束散写成大段说明。
- [x] 三个关键构造物均有就地标签：开篇例子、semantic关系图、lifecycle图；BuildPilot section有全局classification，YAML片段另有局部构造标签。
- [x] 核心边界在开头、具体示例与结论处重复，但每次分别承担scope freeze、walk-through guard和takeaway，不构成actionable repetition。
- [x] Draft没有frontmatter或Hugo shortcode，符合Publisher前workspace状态；外部references使用ASCII URL，未发现引号、未闭合fence、占位符或内部route承诺风险。

Outcome: `PASS`

## Course Continuity and Ownership Boundaries

| Boundary | Draft responsibility | Explicit exclusion | Reviewer result |
|---|---|---|---|
| Article 03 Structured Output | schema-valid record作为semantic Gate入口 | 不重讲parser/DTO/Domain Validation；schema validity不等于truth | `PASS` |
| Article 06 Tool Runtime | Result/terminal Trace可作为candidate Evidence source | Trace presence不等于Claim acceptance；不重讲Runtime pipeline | `PASS` |
| Article 19 Permission/Approval/Sandbox | 仅保留Evidence acceptance不授予action authority的接口 | 不定义principal、approval flow、credential scope或enforcement | `PASS` |
| Article 21 Trace/Replay/Failure Taxonomy | 要求future Trace可被Evidence Record引用 | 不设计correlation、replay/re-execution或完整failure taxonomy | `PASS` |
| Article 22 Eval/Golden Dataset/Regression | 说明accepted Claim不等于系统质量 | 不设计dataset、grader、metrics、regression或Lab 06 | `PASS` |

Published Articles 12–17的Context receipt、Memory promotion/freshness/conflict、Knowledge Base/RAG citation边界与Skill Evidence boundary均被承接为上游约束，没有被Article 18重写。Course-local ownership始终未外推为行业统一taxonomy。

Outcome: `PASS`

## Reader Value, Job Competency and Publication Readiness

- Reader Value: `PASS` — 读者获得能用于“拒绝过度结论”的最小Evidence审查框架、停止条件和可迁移记录模型，而非只得到术语表。
- Job Competency: `PASS` — 展示的能力是工程审查、source/version discipline、diagnostic uncertainty management、lifecycle/governance seams与跨系统责任分层；没有露骨自我推销。
- Publication Readiness: `PASS_AT_REVIEW_GATE` — title与canonical一致，references可达目标均为公开primary sources；无frontmatter/relref属于Publisher尚未执行的正常状态，不是Draft缺陷。Publisher仍须独立完成frontmatter、公开路由映射与Hugo Build Gate。

## Runtime, Lab and Constructed-Example Boundary

| Field | Reviewed state | Result |
|---|---|---|
| Required Lab | `NONE` | `PRESERVED` |
| Experiment Count | `0` | `PRESERVED` |
| Runtime Observation | `ABSENT` | `PRESERVED` |
| BuildPilot implementation | `NOT IMPLEMENTED` | `PRESERVED` |
| BuildPilot execution | `NOT RUN` | `PRESERVED` |
| BuildPilot posture | `DESIGN / SYNTHETIC SHAPE` | `PRESERVED` |
| accuracy / cost / latency / benefit / production evidence | `ABSENT` | `PRESERVED` |
| constructed examples / diagrams | all locally or section-globally labeled | `PASS` |

No command、fixture、exit code、Trace、metric、raw artifact或observation被写成已执行结果。Outcome: `PASS`。

## Findings

`NONE`。

No `BLOCKER`、`MAJOR`、`MINOR` or `EDITORIAL` Finding was opened. The frozen Finding schema (`ID`, `Severity`, `Category`, `Location`, `Problem`, `Supporting Evidence`, `Why It Matters`, `Required Disposition`) therefore has no instance in Cycle 0; no future Revision disposition is pre-closed.

## Five-Dimension Score

| Dimension | Score | Artifact basis |
|---|---:|---|
| Technical Accuracy | `19 / 20` | 对象、Gate、lifecycle与authority边界一致；外部precedent未被升级为universal implementation。 |
| Evidence Discipline | `20 / 20` | `10 / 10` Claim映射；8/8 Card边界完整；2项PARTIAL与6项PROPOSAL全程保留。 |
| Teaching Quality | `19 / 20` | problem-first、抽象模型、fail-closed BuildPilot walk-through、Learning Check形成完整教学闭环。 |
| Engineering Transfer | `19 / 20` | record、semantic gates、conflict/lifecycle与review event可直接转化为设计审查责任。 |
| Readability & Compression | `18 / 20` | L-weight结构清晰；重复边界均有审计用途，仍可由Publisher做非语义性版式压缩。 |
| **Total** | **`95 / 100`** | **达到全部质量阈值。** |

Threshold check: Total `95 >= 88`、Technical `19 >= 18`、Evidence `20 >= 18`、Teaching `19 >= 17`、Engineering `19 >= 17`。Result=`ALL SCORE THRESHOLDS MET`。

## Open Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `0` | `NONE` |
| MINOR | `0` | `NONE` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`0`** | **`NONE`** |

## Gate Decision

`PASS`

- Review execution: `COMPLETE`
- Review Gate Decision: `PASS`
- Open Findings: `0`（BLOCKER 0 / MAJOR 0 / MINOR 0 / EDITORIAL 0）
- Score: `95 / 100`（all thresholds met）
- Next Allowed Gate: `FINAL_GATE`
- Blocker: `NONE`
- Exact route: `REVIEW -> FINAL_GATE`
- Scope guard: 本结论不是Master Validation、Final Gate、Publish或Hugo Build PASS；本Reviewer未执行发布、构建、Git stage/commit/push/branch动作。

## Final Gate Decision

- Reviewer: fresh Reviewer `/root/article18_final_reviewer`
- Review date: `2026-08-25`（Asia/Shanghai）
- Gate: `FINAL_GATE`
- Decision: `PASS`
- Publisher eligibility: `ELIGIBLE / NEXT_ALLOWED_GATE=PUBLISH`
- Open Findings: `0`（BLOCKER 0 / MAJOR 0 / MINOR 0 / EDITORIAL 0）
- Score preservation: `95 / 100`；Technical `19 >= 18`、Evidence `20 >= 18`、Teaching `19 >= 17`、Engineering `19 >= 17`、Total `95 >= 88`。

Independent Final Gate recheck confirms that the zero-open-Finding statement remains truthful. The frozen Draft SHA-256 is still `F6CD06C0CC98D310A5617CADC2E2FEDFE1F1657CC30790EF3A63D8BFD2924646`（`31943` bytes / `386` physical lines），so the reviewed body has not changed since Cycle 0.

Evidence integrity remains intact: all `10 / 10` Claims map to the same eight Evidence Cards；`18-C04 / 18-C05` retain their `PARTIAL` ceilings；`18-C02 / C03 / C06 / C07 / C08 / C09` retain Proposal language；every Card still preserves source identity/version/locator, `Proves`, `Does Not Prove`, `Limitations` and falsifier boundaries. Current primary-source rechecks found no contradiction requiring new Research or semantic revision.

The runtime and scope ceiling is unchanged: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`；accuracy/cost/latency/benefit/production evidence=`ABSENT`. All constructed snippets and diagrams remain locally or section-globally labeled. Article 03 / 06 / 19 / 21 / 22 ownership remains explicit, and no permission model, replay/failure taxonomy, Eval system, BuildPilot Runtime, unsupported core fact or invented experience/benefit has entered the Draft.

Mechanical publication preflight=`PASS`: the Draft is a complete body with paired fences, no placeholder, trailing whitespace, premature frontmatter, Hugo shortcode or future-route dependency. Publisher can add the standard Article 18 Hugo wrapper (`series_order=190`, `weight=3190`), previous navigation to published Article 17, publication metadata and series-index mapping without changing semantic content；Article 19 remains unpublished and must not be linked as an existing next article. No required semantic rewrite exists. Publish mapping and Hugo Build remain independent downstream Gates and were not executed here.

Exact Final Gate result: `PASS / PUBLISHER_ELIGIBLE / BLOCKER=NONE`.
