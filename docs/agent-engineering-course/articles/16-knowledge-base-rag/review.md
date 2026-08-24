# Review｜Article 16 Knowledge Base 与 RAG

## Review Identity

- Reviewer: fresh Reviewer `/root/article16_reviewer_cycle0`
- Review date: `2026-08-24`（Asia/Shanghai）
- Gate: `REVIEW`
- Cycle: `0`（initial Findings；尚未发生 Revision / Recheck）
- Mode: `NORMAL_ARTICLE`
- Required Lab: `NONE`
- Context isolation: 仅依据 durable repository artifacts 与 claim-relevant primary / official sources审查；未读取 Author hidden reasoning、confidence / self-score，也未读取 Article 16 `subagent-trace.md`。
- Write scope: 本轮只修改 `docs/agent-engineering-course/articles/16-knowledge-base-rag/review.md`；未修改 Draft 或其他 Article artifact。

## Review Status

`REVIEW_CYCLE_0_COMPLETE / PASS_WITH_NOTES / REVISION_REQUIRED`

Draft遵循 problem space -> abstract model -> concrete engineering -> engineering judgment -> verification boundary。Knowledge Base、RAG、Memory、Evidence保持职责分账；`Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify`始终标成`COURSE PROPOSAL / review model`，没有写成行业统一 taxonomy、强制拓扑或普遍最优物理顺序。

`16-C01 / C02 / C04 / C06`保持`PROPOSAL`；`16-C03 / C05`只在来源直接支持的窄范围内使用`CONFIRMED`语态。正文没有新增无 Evidence Card 的核心 Claim，也没有把 score、filter survivor、citation presence 或 trace字段升级为 truth、authority、Evidence acceptance 或 Verification。

本轮发现一个发布前必须处置的`MINOR / PUBLICATION`问题：Draft参考资料中的课程依赖和生产 artifact 使用 repo-relative链接，其中三项没有Hugo公开路由；直接机械发布会产生不可用链接。技术、Evidence与教学主线本身通过，但存在actionable Finding，因此路由`REVISION`，不能进入`FINAL_GATE`。

## Technical Review

- [x] Glossary四对象边界一致：Knowledge Base负责带来源边界的可检索集合；RAG负责request / step scoped检索与装配模式；Memory负责跨step / Session保留、召回与治理；Evidence负责claim-scoped可审计支持。
- [x] Article 12的application-visible Context Snapshot / Receipt上限被保留：Inject / Receipt不证明Provider内部使用、模型关注或答案正确。
- [x] Article 15的`Stored -> Retrieved -> Eligible -> Injected`、promotion / freshness / conflict边界被承接而未重写；历史记录没有冒充Current Reality。
- [x] Query drift、retrieval miss、wrong-scope survivor、over-filter、rerank demotion、qualifier truncation、wrong / incomplete citation与unsupported acceptance等失败路径均有可解释落点。
- [x] Keyword / lexical、Vector / dense、Hybrid只被描述为候选生成 / 融合策略；DPR数字没有外推到Unity / Jenkins或16-EXP01。
- [x] Filter与Rerank只作逻辑责任分账；正文明确允许Provider融合物理阶段，也没有规定pre-filter / post-filter的通用最优解。
- [x] permission metadata、identity、同步与enforcement没有被一个字符串filter吞掉；完整Permission / Approval / Sandbox仍留给Article 19。
- [x] Citation presence、support correctness / completeness、applicability / authority、Evidence acceptance与Verification逐层分开。

Outcome: `PASS`

## Evidence Review

| Claim | Evidence status | Draft disposition | Reviewer result |
|---|---|---|---|
| `16-C01` | `PROPOSAL` | 四对象分账明确写成课程工作定义；允许共用设施 | `TRACEABLE / WITHIN_CEILING` |
| `16-C02` | `PROPOSAL` | 全链明确写成review model；物理阶段可融合 | `TRACEABLE / WITHIN_CEILING` |
| `16-C03` | `CONFIRMED` | 只确认strategy / fusion差异及workload / metric依赖 | `TRACEABLE / WITHIN_CEILING` |
| `16-C04` | `PROPOSAL` | eligibility与relevance逻辑分账；不规定filter placement或权限闭环 | `TRACEABLE / WITHIN_CEILING` |
| `16-C05` | `CONFIRMED` | 只确认citation presence与support correctness / completeness等判断可分 | `TRACEABLE / WITHIN_CEILING` |
| `16-C06` | `PROPOSAL` | 只写实验合同与claim ceiling；没有observed effect | `TRACEABLE / WITHIN_CEILING` |

Traceability result: `6 / 6`；`CONFIRMED=2 / PARTIAL=0 / PROPOSAL=4 / BLOCKED=0`；Draft不存在依赖`BLOCKED` Evidence的行为性结论。

Primary-source spot-check：Lewis et al.只直接建立特定retriever + generator RAG模型，没有定义本文完整审查链；DPR的dense优势绑定open-domain QA dataset、Lucene-BM25 baseline与top-20 passage retrieval accuracy；BEIR覆盖18个异构dataset与多种retrieval family，支持workload / metric约束；ALCE分别评价correctness与citation quality，并报告citation support可不完整。OpenAI Vector Store Search当前参考将query、attributes filter、ranking options、chunks与similarity score分字段暴露；Elastic当前Hybrid文档与明确标版的8.19 Re-ranking文档分别支持full-text + vector fusion及candidate generation / reranking责任分离。Azure Hybrid Search页面（last updated `2026-07-21`）明确filter可在query processing开头或结尾应用且应按实际query测试；Document-Level Access Control页面（last updated `2026-08-12`）保留security filter、preview能力、identity / indexed permission metadata与同步时滞边界。上述spot-check与Draft措辞强度一致，没有形成新Research Claim。

Outcome: `PASS`

## Course / Reader Value / Engineering Review

- [x] Article type=`PRINCIPLE`；开篇先立“搜到不等于可相信并使用”的工程问题，再建立四对象与阶段责任，最后落到Unity / Jenkins synthetic illustration和实验合同。
- [x] M-weight控制总体合格：没有扩写embedding / chunking / vector database参数大全、企业知识治理平台或production RAG实现。
- [x] Article 17只作为Skill后续桥接，Article 18只保留Evidence acceptance接口，Article 19只保留检索侧permission seam；没有提前写完后续课程。
- [x] 没有BuildPilot Runtime、permission closure、enterprise taxonomy或production certification表述。
- [x] Reader可据此区分candidate、eligibility、relevance、assembly、citation support与Claim acceptance；Learning Check覆盖六个Claim的关键判断。
- [x] Job competency落点明确：retrieval design review、事故知识复用、auditability、实验设计与Evidence discipline均带适用边界。
- [x] 重复的proposal / not-run标记服务Evidence纪律；表格与failure semantics使当前篇幅仍符合M-weight，不要求为压缩删除关键边界。
- [ ] 发布参考链接尚未闭合；见`16-RV-C0-001`。

Outcome: `PASS_WITH_NOTES`

## Claim Traceability

| Claim | Draft locations | Evidence anchor | Review ceiling |
|---|---|---|---|
| `16-C01` | Sections 1、2、7、9 | `16-E01` | `PROPOSAL`；课程职责分账，不是行业taxonomy或物理分库要求 |
| `16-C02` | Sections 1、3、6、7、9 | `16-E02` | `PROPOSAL`；review model，不是强制组件或最优物理顺序 |
| `16-C03` | Sections 4、7 | `16-E03` | `CONFIRMED`仅限strategy / fusion与workload / metric范围；无赢家 |
| `16-C04` | Sections 5、7 | `16-E04` | `PROPOSAL`；eligibility / relevance seam，不是permission closure |
| `16-C05` | Sections 6、7 | `16-E05` | `CONFIRMED`仅限citation判断可分；不自动成立applicability、acceptance或Verification |
| `16-C06` | Section 8 | `16-E06` | `PROPOSAL`；只定义未来实验合同，不得写observed effect |

Coverage: `6 / 6`；new core Claim=`NONE`。

## Experiment Boundary

| Field | Reviewed state | Result |
|---|---|---|
| Experiment | `16-EXP01` | `TRACEABLE` |
| Status | `PROPOSAL / NOT_RUN` | `PRESERVED` |
| Observed Result | `ABSENT` | `PRESERVED` |
| Raw Artifact | `NONE` | `PRESERVED` |
| Fixture | `10 synthetic Markdown incidents / NOT_CREATED` | `PRESERVED` |
| Required Lab | `NONE` | `PRESERVED` |
| Concrete Article 16 effect | recall / precision / ranking / accuracy / latency / cost / answer utility / quality improvement / winner | `ABSENT` |

Expected Observable、future metric、failure criteria与Observed Result保持分离；没有fixture、command、exit code、stage output或raw observation可被误读成实验完成。Synthetic illustration明确标`NOT EXECUTED / NO RUNTIME CLAIM`。Outcome: `PASS`。

## Findings

### Finding ID

`16-RV-C0-001`

Severity: `MINOR`

Category: `PUBLICATION`

Location

`draft.md:303-311`，尤其课程依赖列表中的`../../glossary.md`、两条`../../../../content/...`链接，以及`research.md` / `evidence.md`链接。

Problem

这些链接只在当前`docs/.../16-knowledge-base-rag/`工作区相对位置下成立。Article 12 / 15链接若原样映射到Hugo content路径会改变解析基准；Glossary、Article 16 Research与Evidence不是已确认的Hugo公开页面。当前Draft因此还不是publish-safe的冻结链接集合，且Hugo未必会把普通Markdown相对链接报告为`REF_NOT_FOUND`。

Supporting Evidence

Repository `AGENTS.md`规定站内文章使用ASCII双引号的`relref` shortcode；Article 15的published reference section只保留公开可达引用。Article 16的目标发布路径是`content/ai-empowerment/agent-engineering-16-knowledge-base-rag.md`，而`docs/agent-engineering-course/glossary.md`、当前`research.md`与`evidence.md`不在Hugo content tree中。

Why It Matters

读者可能获得404或错误相对路径，同时Build Gate可能因这些不是`relref`而无法提前失败。Reviewer若忽略该点，会把一个可预见的publication defect推迟到发布后人工发现。

Required Disposition

在Draft冻结前明确处理五条内部引用：Article 12 / 15改为符合repository规则的publish-safe `relref`；Glossary、Research、Evidence要么移除链接、改为不承诺公开路由的文字说明，要么替换为已验证的公开目标。Revision后逐项确认目标存在；不得借此修改正文技术主线或扩写新Claim。

## Five-Dimension Score

| Dimension | Score | Artifact basis |
|---|---:|---|
| Technical Accuracy（brief: Technical Quality） | `19 / 20` | 四对象、阶段责任、failure semantics与Article 12 / 15边界一致；无固定拓扑或权限闭环误写。 |
| Evidence Discipline（brief: Evidence Traceability） | `19 / 20` | `6 / 6`可追踪；四项proposal、两项narrow confirmed与NOT_RUN ceiling均保持。 |
| Teaching Quality（brief: Teaching Accuracy） | `18 / 20` | problem-first、抽象模型、Unity / Jenkins示例、Learning Check与最短结论形成完整教学路径。 |
| Engineering Transfer（brief: Engineering Depth） | `18 / 20` | eligibility / relevance、Receipt / citation、stage trace与实验失败条件可迁移；不冒充Runtime验证。 |
| Readability & Compression | `18 / 20` | M-weight内重复边界有审计用途，表格压缩有效；仅publication links需处置。 |
| **Total** | **`92 / 100`** | **分数达到质量线，但不关闭`16-RV-C0-001`。** |

Threshold check：Total `92 >= 88`、Technical `19 >= 18`、Evidence `19 >= 18`、Teaching `18 >= 17`、Engineering `18 >= 17`。Result=`ALL SCORE THRESHOLDS MET`。

## Unclosed Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `0` | `NONE` |
| MINOR | `1` | `16-RV-C0-001` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`1`** | **`16-RV-C0-001`** |

## Gate Decision

`PASS_WITH_NOTES`

- Review execution: `COMPLETE`
- Review Gate decision: `PASS_WITH_NOTES`
- Recommended next Gate: `REVISION`
- Blocker: `NONE`
- Rationale: Technical、Evidence、Teaching与Engineering阈值均通过，Claim traceability=`6 / 6`，实验与后续课程边界完整；但存在`1`个actionable `MINOR / PUBLICATION` Finding，按Gate规则不得进入`FINAL_GATE`。
- Scope guard: 本结论不是Master Gate、Final Gate、Publish或Build PASS；只有Reviewer可在recheck中关闭Finding。

## Revision Disposition｜Cycle 1

- Finding ID: `16-RV-C0-001`
- Files Changed: `draft.md`、`review.md`
- What Changed: Article 12 / 15 references now use publish-safe ASCII-quoted Hugo `relref`; Glossary / Research / Evidence are plain repository production references without public-route promise.
- Evidence Impact: `NONE`；no technical, Claim, Evidence, experiment or future-Article boundary changed.
- Proposed Status: `READY_FOR_RECHECK`
- Closure Authority: Revision Worker did not mark the Finding CLOSED; only Reviewer may close it.

## Review Recheck｜Cycle 1

- Reviewer: `Codex / fresh real REVIEWER (article16_reviewer_recheck_cycle1)`
- Date: `2026-08-24`
- Finding: `16-RV-C0-001` — **CLOSED**
  - The former five workspace-relative references are now publish-safe: Article 12 and Article 15 use Hugo `relref` with ASCII double quotes, respectively targeting `content/ai-empowerment/agent-engineering-12-context-engineering.md` and `content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md`; both target files exist.
  - Glossary, Article 16 Research, and Article 16 Evidence are plain repository-production-reference text, each explicitly states that it has no public Hugo route. They make no public-route promise.
  - This is the exact disposition requested by Cycle 1. No external URL, technical claim, evidence/experiment wording, or future-Article wording was changed in the scoped fix.
- New-breakage check: **PASS**. The two public internal references comply with the repository `relref` rule (ASCII `"` parameters), non-public references are not links, and `git diff --check` is `PASS`.
- `review_cycle`: `1 / 3`
- Unclosed Findings: `0` (none).
- Score: **92 / 100 retained** — Technical 19, Evidence 19, Teaching 18, Engineering 18, Readability 18. The scoped link-safety correction changes no scored technical/evidence/teaching content; all threshold conditions remain met.
- Gate decision: **PASS**. With `16-RV-C0-001` closed by the Reviewer, zero open Findings, and thresholds retained, the next allowed Gate is **FINAL_GATE**.

## Independent Final Gate

### Identity and context isolation

- Role / gate: fresh independent `REVIEWER` / `FINAL_GATE`.
- Execution: `REAL_SUBAGENT`; decision made independently from the relayed repository facts, not from a Master verdict.
- Isolation: Author hidden reasoning, confidence, self-score, and subagent trace were not read. Draft was treated as frozen and was not modified.
- Scope: final Review/Finding state, Claim/Evidence and experiment boundaries, concept and future-article boundaries, title/references/links/static publication preflight, score thresholds, and next-gate eligibility. Publish and Hugo Build were not run or claimed.

### Frozen Draft identity

- File: `docs/agent-engineering-course/articles/16-knowledge-base-rag/draft.md`
- SHA-256: `1FF54604DD48CADFD0FDBA33FCB3217854F9EE3B84E24A10D703B8633979FB4C`
- Bytes: `26021`
- Physical lines: `329`
- Title: `Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite`
- Frontmatter: absent, as expected before Publisher.
- Placeholder hits: `0`; `relref` hits: `2`.

### Review and Finding revalidation

- Durable Cycle 0 remains valid: Technical `PASS`, Evidence `PASS`, Course `PASS_WITH_NOTES`; score `92 / 100` with dimensions `19 / 19 / 18 / 18 / 18`.
- The sole Cycle 0 Finding, `16-RV-C0-001` (`MINOR / PUBLICATION`), is `CLOSED` after the five-item reference-only revision.
- Cycle 1 recheck remains valid: both `relref` targets exist; three nonpublic repository references remain plain text; no technical, Evidence, experiment, or future-Article wording changed; new-breakage check `PASS`; open Findings `0`; review cycle `1 / 3`.
- No blocking item is open or removed from the audit trail. All three review classes have no `BLOCKED` result.

### Claim and Evidence boundary revalidation

- Claim coverage is `6 / 6`; no unsupported core Claim was found.
- `C01`, `C02`, `C04`, and `C06` remain `PROPOSAL`.
- `C03` and `C05` remain narrow `CONFIRMED`: lexical/dense/hybrid retrieval effectiveness is workload- and metric-dependent with no universal winner; citation presence is distinct from correctness, completeness, applicability, and Verification, and a citation mapping does not accept a Claim.
- The Evidence Gate remains `PASS / 2 CONFIRMED / 0 PARTIAL / 4 PROPOSAL / 0 BLOCKED`.
- The Draft consistently marks the four-object split and the `Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify` chain as a course review model. It does not present them as a unified industry taxonomy, mandatory physical components, or a universally optimal physical order.

### Experiment and conclusion boundary revalidation

- `16-EXP01` remains `PROPOSAL / NOT_RUN`.
- Observed Result: `ABSENT`; Raw Artifact: `NONE`; fixture: `NOT_CREATED`.
- The experiment section contains design, metrics, and failure criteria only; it reports no observed values.
- The Draft makes no concrete recall, precision, ranking, accuracy, latency, cost, answer-utility, quality-improvement, winner, or production conclusion.

### Concept and course boundary revalidation

- Knowledge Base, RAG, Memory, and Evidence remain distinct responsibilities.
- Retrieve, Filter, Rerank, Inject, and Cite remain distinct review responsibilities even where an implementation may merge physical stages.
- Citation/support/applicability/acceptance/Verification remain separated; evidence mapping does not imply acceptance.
- No Article 17 Skill workspace/content is prewritten. Article 17/18/19 topics appear only as forward boundaries.
- No BuildPilot Runtime, permission closure, enterprise taxonomy, or production certification is claimed.

### Final title, links, and static publication preflight

- Canonical title matches exactly: `Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite`.
- Placeholder hits: `0`.
- The only two `relref` shortcodes use ASCII quotes and target existing Article 12 and Article 15 public content.
- Three nonpublic repository production references are plain text rather than public links.
- Article 17 assets are absent; Article 16 Published Content is absent as expected before Publisher.
- `git diff --check`: `PASS`.
- No Publish or Hugo Build was run or claimed by this gate.

### Score and unclosed summary

- Score retained: `92 / 100` (`19 / 19 / 18 / 18 / 18`).
- Thresholds: Total `>= 88`, Technical `>= 18`, Evidence `>= 18`, Teaching `>= 17`, Engineering `>= 17`; all met.
- Open Findings: `0`.
- Blocking Claims / Evidence items: `0`.
- Unclosed publication blockers: `NONE`.

### Final Gate decision

`PASS — next allowed gate: PUBLISH.`
