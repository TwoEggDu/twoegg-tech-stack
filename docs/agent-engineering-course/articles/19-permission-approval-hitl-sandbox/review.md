# Review｜Article 19 Permission、Approval、Human-in-the-loop 与 Sandbox

## Review Identity

- Reviewer: fresh Reviewer `/root/article19_reviewer_cycle0`
- Review date: `2026-08-25`（Asia/Shanghai）
- Gate: `REVIEW`
- Cycle: `0`（initial independent review）
- Mode: `NORMAL_ARTICLE`
- Course Weight: `L / Deep Core Lesson`
- Required Lab: `NONE`
- Context isolation: 仅依据 durable repository artifacts、canonical、glossary、已发布 Articles 06 / 10 / 11 / 18 与可复核 primary / official sources 审查；未读取或依赖 Author hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只完整替换 `review.md`，并向 Article 19 `subagent-trace.md` 追加一个 canonical raw Worker Result；未修改 Draft、Outline、Research、Evidence、Card、README、global/canonical/published/future-Article artifacts。

## Frozen Review Input

- Draft: `docs/agent-engineering-course/articles/19-permission-approval-hitl-sandbox/draft.md`
- SHA-256: `A35E30D16E9356BCCD5732B9BBAEE6B569096729837F89C3FB936D68249E970C`
- Bytes: `43098`
- Physical lines: `577`
- Canonical title: `Permission、Approval、Human-in-the-loop 与 Sandbox`
- Claim register: `10` Claims，`12` Evidence Cards，core `BLOCKED=0`
- Registered evidence mix: `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL`
- Runtime state: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`

## Review Status

`REVIEW_CYCLE_0_COMPLETE / REVISION_REQUIRED`

Draft 的主线符合 problem space -> abstract model -> concrete BuildPilot design -> engineering / verification boundary，且绝大多数 Proposal、runtime、security 与 cross-Article boundaries 均保持克制。但 `19-C09` 的 Draft 矩阵把 Evidence Card `19-E09` 明确列入 Limitations 的 filesystem 与 secret-broker mechanism 写进了 `CONFIRMED MECHANISM BOUNDARIES`；同时，`19-E04 / 19-E08` 声明固定到 NIST SP 800-53 Rev. `5.1.1`，locator 却只指向会漂移的 publication landing page，无法重放指定版本。

因此本轮打开 `1 MAJOR + 1 MINOR` Finding。Finding 均未关闭；Review execution 完整，但 Review Gate 不通过，按合同路由 `REVIEW -> REVISION`，不构成 Factory blocker 或 human stop。

## Technical Accuracy

- [x] Evidence acceptance、Permission、Authorization、Approval、HITL 与 Sandbox 的责任面没有混成同一个布尔 Gate。
- [x] Capability ceiling、本次 action、resource、constraints、policy decision、approval binding 与 use-time enforcement 的分层内部一致。
- [x] Approval decision idempotency 与 action side-effect exactly-once 明确分开，并正确承接 Articles 06 / 11。
- [x] Resume revalidation、expiry、revocation propagation window 与 TOCTOU 均保留 mitigation 而非绝对保证语态。
- [x] HITL state machine、R0—R3 route、Approval records 与 `ActionAuthorityEnvelope` 明确标为 course proposal / synthetic / not run。
- [ ] Sandbox matrix 的四个 mechanism family 并非都由 `19-E09` 支持；见 `A19-R0-F01`。

Outcome: `FAIL / REVISION_REQUIRED`

## Evidence Discipline

### Claim audit（10 / 10）

| Claim | Registered status | Draft disposition | Reviewer result |
|---|---|---|---|
| `19-C01` | `CONFIRMED` | accepted Evidence 与 action authority 分离，不产生真实 credential / approval / execution authority | `TRACEABLE / WITHIN_CEILING` |
| `19-C02` | `PARTIAL` | 五概念只称课程 working model，产品交互不外推 | `TRACEABLE / PARTIAL_CEILING_PRESERVED` |
| `19-C03` | `PROPOSAL` | authority chain 明确为 auditable design / not implemented | `TRACEABLE / WITHIN_CEILING` |
| `19-C04` | `PARTIAL` | 五时点 workflow 明确为 standard-supported separation + course design | `TRACEABLE / PARTIAL_CEILING_PRESERVED` |
| `19-C05` | `PROPOSAL` | R0—R3 明确为 organization-specific course proposal | `TRACEABLE / WITHIN_CEILING` |
| `19-C06` | `PROPOSAL` | ApprovalRequest / DecisionRecord 明确非 product schema | `TRACEABLE / WITHIN_CEILING` |
| `19-C07` | `PROPOSAL` | HITL state machine 未伪装成 OpenAI SDK 实测或通用协议 | `TRACEABLE / WITHIN_CEILING` |
| `19-C08` | `CONFIRMED` | least privilege / scope / expiry / revocation / TOCTOU 保留窄事实与非零风险边界 | `TRACEABLE / WITHIN_CEILING` |
| `19-C09` | `CONFIRMED` | Linux namespace / network namespace / seccomp 部分在 ceiling 内，但 filesystem / secret-broker 两行越过唯一 Card 的覆盖范围 | `OVER_CEILING / A19-R0-F01` |
| `19-C10` | `PROPOSAL` | BuildPilot 仅为 design candidate，ownership 保持 repository-local | `TRACEABLE / WITHIN_CEILING` |

Coverage=`10 / 10`；registered mix=`3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED`；new core Claim=`NONE`。状态计数本身正确，但 `19-C09` 的正文证据使用不完整，因此不能整体通过 Evidence Review。

### Evidence Card audit（12 / 12）

| Card | Source / version / locator | Proves / Does Not Prove / Limitations / falsifier | Reviewer result |
|---|---|---|---|
| `19-E01` | Published Article 18 snapshot + NIST SP 800-162 §§2.1–2.3 | 四类边界齐全；只支撑 Evidence acceptance 与 access authority 分离 | `COMPLETE / WITHIN_SCOPE` |
| `19-E02` | NIST SP 800-162，2014 + 2019 updates，§§2.1–2.3 / Figure 2 | ABAC attributes 与 decision/enforcement 分工有界；不证明五分法 taxonomy | `COMPLETE / WITHIN_SCOPE` |
| `19-E03` | NIST SP 800-207，2020，§§2.1 / 3.2 / 3.3 | zero-trust session / PE-PA-PEP 范围清楚；不外推所有 Agent Tool | `COMPLETE / WITHIN_SCOPE` |
| `19-E04` | NIST SP 800-53 claimed Rev. `5.1.1`，AC-2 / AC-3 / AC-3(2)/(8) / AC-6 / AC-24 | 控制映射的 Proves / Does Not Prove / Limitations / falsifier 完整，但 locator 未固定 claimed release | `CONTENT_PLAUSIBLE / LOCATOR_INCOMPLETE / A19-R0-F02` |
| `19-E05` | RFC 6749 §§3.3 / 4.2.2 + RFC 7009 §§2–3 | scope、expiry、revocation 与 propagation window 被限定在 OAuth | `COMPLETE / WITHIN_SCOPE` |
| `19-E06` | MITRE CWE-367，CWE 4.20，accessed 2026-08-25 | check/use race 与 mitigation direction 有界；不声称消除 race | `COMPLETE / MOVING_SOURCE_BOUNDARY_PRESENT` |
| `19-E07` | RFC 9110 §9.2.2 + Published Articles 06 / 11 | HTTP retry语义与 course side-effect ownership 分开；不推出 exactly-once | `COMPLETE / WITHIN_SCOPE` |
| `19-E08` | NIST SP 800-53 claimed Rev. `5.1.1`，AU-3 | audit content元素映射有界；完整 Approval schema 不归因于 NIST，但 locator 未固定 claimed release | `CONTENT_PLAUSIBLE / LOCATOR_INCOMPLETE / A19-R0-F02` |
| `19-E09` | current Linux seccomp docs + man-pages 6.18 namespaces / network_namespaces | 只证明 namespace、network namespace 与 seccomp 边界；Card 自己明确未覆盖 filesystem / secret broker configurations | `CARD_COMPLETE / DRAFT_USAGE_OVERREACH / A19-R0-F01` |
| `19-E10` | current OpenAI Agents SDK HITL guide，accessed 2026-08-25 | interruption、approve/reject、serialized RunState / resume 是 product-scoped example；未外推安全或 revalidation | `COMPLETE / MOVING_SOURCE_BOUNDARY_PRESENT` |
| `19-E11` | course design synthesis，2026-08-25 | 只证明 proposed model 的内部 traceability；不证明运行、安全、收益或性能 | `COMPLETE / PROPOSAL_CEILING_PRESERVED` |
| `19-E12` | Published Articles 06 / 10 / 11 / 18 + Article 19 workspace snapshot | course ownership 可复核；不证明未来文章或 Runtime | `COMPLETE / REPOSITORY_SCOPE_PRESERVED` |

所有 12 Cards 均有 `Source`、版本或 access boundary、`Locator`、`Proves`、`Does Not Prove`、`Limitations` 与 falsifier。问题不在字段缺失，而在一个 Draft usage 超出 Card limitation，以及一个 claimed pinned release 的 locator 不能重放指定版本。

Outcome: `FAIL / REVISION_REQUIRED`

## Teaching Quality

- [x] 第一屏从“accepted diagnosis 是否可直接发布”立问题，没有 API-first 退化。
- [x] 五概念分账、五时点、authority chain、risk route、records、HITL、TOCTOU、Sandbox 与 BuildPilot 形成逐层认知路径。
- [x] BuildPilot fail-closed walk-through 保留 `REQUIRED / UNKNOWN / NOT_RUN`，展示缺口而非伪造完整样例。
- [x] 10 题 Learning Check 均有参考答案，答案未引入新核心事实；涵盖 authority inputs、五概念、时点、risk、records、idempotency、revalidation、sandbox 与 Article 20/21 bridge。
- [ ] Sandbox matrix 的“CONFIRMED”总标签会让读者把未取证的 filesystem / secret-broker 行与已取证 Linux机制读成同一证据强度；见 `A19-R0-F01`。

Outcome: `FAIL / REVISION_REQUIRED`

## Engineering Transfer

- [x] Principal / Action / Resource / Constraints / Policy / Approval / Enforcement 可转化为设计审查清单。
- [x] ApprovalRequest 与 DecisionRecord 分离，能暴露 digest、scope、expiry、revocation 与 approver binding。
- [x] `WAITING_APPROVAL`、reject/cancel/expired terminal、resume revalidation 与 use-time enforcement 给出可迁移的 control seams。
- [x] Risk route 明确 unknown handling 与 hard deny，且没有抢占 Article 20 的预算 owner。
- [x] Job Competency 映射落到 reader-visible design outputs，并逐项写清 proposal / runtime ceiling。

Outcome: `PASS_WITH_FINDING_DEPENDENCY`

## Readability & Compression

- [x] L-weight篇幅与深核主题基本匹配；标题、段落、表格、构造图、YAML、Learning Check 与参考资料顺序清楚。
- [x] 构造 relationship、错误等式、五概念表、五时点表、authority chain、risk table、record table/YAML、HITL state machine、idempotency table、TOCTOU sequence、Sandbox matrix、BuildPilot flow/YAML、ownership、anti-pattern、Claim traceability 与 competency table 均有就地或紧邻标签。
- [x] 所有 BuildPilot 与课程自定义结构保持 `DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN`；没有虚构命令、Trace、metric、approval、credential、runtime、security 或 benefit evidence。
- [x] Draft 无 frontmatter / Hugo shortcode 属于 Publisher 前 workspace 状态；fence 成对、无 placeholder、DATA/EXPERIENCE TODO 或 future-route link。
- [x] 重复的 boundary label 偏多，但主要承担 ceiling 防误读职责，Cycle 0 不单独开风格 Finding。

Outcome: `PASS`

## Course Continuity and Ownership Boundaries

| Owner | Article 19 usage | Explicit non-scope | Reviewer result |
|---|---|---|---|
| Article 06 Tool Runtime | enforcement 落到 tool execution/result seam；沿用 Policy terminal 与 invocation/effect boundary | 不重讲 Validate/Result/Trace，不把 Policy v1 当 approval system | `PASS` |
| Article 10 State Machine / Workflow | approval-required、pause/resume 成为 deterministic control points | 不重讲通用 Workflow / Agent Decision Point | `PASS` |
| Article 11 Long-running Agent | Resume revalidation、side-effect/idempotency seam | 不设计 Retry/Recovery/compensation/exactly-once | `PASS` |
| Article 18 Evidence Contract | accepted Claim 只是 authority input | 不把 acceptance 当 Permission / Approval | `PASS` |
| Article 20 Budget | 只保留 constraint owner 接口 | 不定义 Token/Step/Cost/Latency数值、计账、耗尽或调度 | `PASS` |
| Article 21 Trace/Replay/Failure Taxonomy | future records 可引用 request/decision identity | 不设计 cross-step Trace schema、Replay、re-execution或taxonomy | `PASS` |

Article 19 也没有提前进入 Article 22 Eval 或 Articles 24—27 Harness control-plane实现。Course-local ownership 没有外推为行业统一系统架构。

Outcome: `PASS`

## Runtime, Lab, Safety and BuildPilot Boundary

| Field | Reviewed state | Result |
|---|---|---|
| Required Lab | `NONE` | `PRESERVED` |
| Experiment Count | `0` | `PRESERVED` |
| Runtime Observation | `ABSENT` | `PRESERVED` |
| BuildPilot implementation | `NOT IMPLEMENTED` | `PRESERVED` |
| BuildPilot execution | `NOT RUN` | `PRESERVED` |
| BuildPilot posture | `DESIGN / SYNTHETIC` | `PRESERVED` |
| real Approval / credential / Sandbox / enforcement | `ABSENT` | `PRESERVED` |
| security / success / reliability / cost / latency / benefit guarantee | `ABSENT` | `PRESERVED` |

Approval、Sandbox、least privilege 与 revalidation 均未被写成绝对安全或收益保证。唯一安全相关缺口是 `A19-R0-F01` 的证据覆盖，不是 Draft 声称了 observed security outcome。

## Mechanical Publication Preflight

- Frozen SHA-256 recheck: `PASS / A35E30D16E9356BCCD5732B9BBAEE6B569096729837F89C3FB936D68249E970C`
- Physical structure: `577` lines，nine fenced code blocks paired（18 fence lines），tables/blockquote nesting可机械映射。
- Draft-stage wrapper: no frontmatter / relref is expected before Publisher；Publisher仍须独立添加 canonical metadata、previous/next navigation与Hugo route。
- Placeholder scan: no `DATA-TODO`、`EXPERIENCE-TODO`、`NOT_ASSIGNED`、`NOT_REACHED`、`NOT_EVALUATED` or future unpublished relref。
- Build status: `NOT RUN AT REVIEW`；Reviewer PASS不替代 Publisher mapping或Hugo Build Gate。
- Publication readiness: `NOT ELIGIBLE UNTIL FINDINGS CLOSED AND FINAL_GATE PASS`。

## Findings

### A19-R0-F01

- ID: `A19-R0-F01`
- Severity: `MAJOR`
- Category: `EVIDENCE`
- Location: `draft.md` Sandbox mechanism / limit matrix and the paragraphs immediately following it；`research.md` answer `19-C09`；`evidence.md` `19-E09` Limitations / Claim `19-C09` usage。
- Problem: Draft 将 filesystem view/mount/allow-write policy 与 secret broker/mount/environment policy 放进标为 `CONFIRMED MECHANISM BOUNDARIES` 的矩阵，并在 `19-C09` 的 confirmed conclusion 中与 namespace/seccomp 并列。但 `19-C09` 唯一 Evidence Card `19-E09` 的 `Proves` 只覆盖 Linux namespaces、network namespace 与 seccomp，且 `Limitations` 明确写明“未覆盖……filesystem、secret broker configurations”。正文因此越过了 Card 自己冻结的证明范围。
- Supporting Evidence: `19-E09 Proves` 仅列 namespaces/network namespace/seccomp；`19-E09 Limitations` 明确排除 filesystem 与 secret broker configurations；Draft matrix 却把四个 mechanism family 整体标为 confirmed，并在后文把 read-only filesystem 与 secret broker 作为机制事实使用。Evidence Contract 要求 `Proves / Does Not Prove / Limitations` 控制正文措辞，不能由一个总标签覆盖 Card 排除项。
- Why It Matters: 这是核心 Sandbox 教学矩阵；读者会把未取证的两行误读为与 Linux primary-source事实同强度。它同时破坏 `19-C09 CONFIRMED` 的 ceiling、12-Card traceability与“机制存在不升级为保证”的中心教学规则。
- Required Disposition: 在 Revision 中选择最小闭环之一：要么把 `19-C09` 及 Draft 的 confirmed matrix 收窄到 `19-E09` 真正证明的 namespace/network namespace/seccomp 范围，并把 filesystem/secret-broker 内容明确降为未取证的 course design/example；要么返回 Research，为两类机制补充版本化 primary-source Cards、完整 `Proves / Does Not Prove / Limitations / falsifier`，再重新映射 Claim与正文。不得仅修改矩阵标签而保留同样的 confirmed conclusion。

### A19-R0-F02

- ID: `A19-R0-F02`
- Severity: `MINOR`
- Category: `EVIDENCE`
- Location: `evidence.md` `19-E04` and `19-E08` Source / Locator；`research.md` Source and drift register；`draft.md` NIST SP 800-53 reference entry。
- Problem: 两张 Card 声明固定使用 NIST SP 800-53 Rev. `5.1.1`，但 locator 只指向 `https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final` 的 publication landing page。该页面是会更新 supplemental release信息的入口，不能唯一定位到 claimed `5.1.1` control text；因此 source identity与version虽写出，locator无法重放指定版本。
- Supporting Evidence: `19-E04` 与 `19-E08` 都写 `Source: ... Rev. 5.1.1`，但共享非版本固定 landing-page locator；Evidence Card合同要求 source、version与locator共同让 cited control可复核。Draft也以 Rev. `5.1.1` 精确版本展示给读者。
- Why It Matters: AC-2 / AC-3 / AC-3(2)/(8) / AC-6 / AC-24 / AU-3 支撑 `19-C04 / C05 / C06 / C08`。若 locator不能复现声明版本，后续 Reviewer无法区分 control text drift、landing-page更新与作者实际核对的版本。
- Required Disposition: 将 `19-E04 / E08` 及 Draft reference 改为同一份可重放、版本固定的 NIST `5.1.1` artifact locator（例如明确的 versioned OSCAL/PDF release identity），并逐项复核所列 control locator；或者统一改为另一明确版本并重新确认 Proves边界。保留 generic landing page可作为 publication入口，但不能替代 pinned locator。

本 Cycle 未对上述 Finding 标 `CLOSED`，也未预写 Revision Disposition。

## Five-Dimension Score

| Dimension | Score | Artifact basis |
|---|---:|---|
| Technical Accuracy | `18 / 20` | authority、HITL、revocation、TOCTOU与sandbox非保证边界总体准确；Sandbox confirmed矩阵存在一处证据覆盖错误。 |
| Evidence Discipline | `16 / 20` | 10/10 Claims与12/12 Cards形式完整，但C09越过Card limitation，且两张fixed-version Card缺可重放locator。 |
| Teaching Quality | `18 / 20` | problem-first、抽象模型、fail-closed BuildPilot与Learning Check完整；核心Sandbox表的强度混标会误导读者。 |
| Engineering Transfer | `18 / 20` | authority envelope、records、state machine、revalidation与ownership seam可迁移。 |
| Readability & Compression | `18 / 20` | L-weight结构清楚，构造物标签齐全；高密度重复仍在可接受范围。 |
| **Total** | **`88 / 100`** | **Total达到基线，但 Evidence Discipline `16 < 18`，且存在open MAJOR/MINOR Findings。** |

Threshold check: Total `88 >= 88`、Technical `18 >= 18`、Evidence `16 < 18`、Teaching `18 >= 17`、Engineering `18 >= 17`。Result=`THRESHOLDS NOT ALL MET`。

## Open Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `1` | `A19-R0-F01` |
| MINOR | `1` | `A19-R0-F02` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`2`** | **`A19-R0-F01`, `A19-R0-F02`** |

## Gate Decision

`FAIL / REVISION_REQUIRED`

- Review execution: `COMPLETE`
- Review Gate Decision: `FAIL`
- Outcome: `REVISION_REQUIRED`
- Open Findings: `2`（BLOCKER 0 / MAJOR 1 / MINOR 1 / EDITORIAL 0）
- Score: `88 / 100`（Evidence Discipline threshold not met）
- Next Allowed Gate: `REVISION`
- Blocker: `NONE`
- Gate completed: `true`
- Exact route: `REVIEW -> REVISION -> REVIEW_RECHECK`
- Final Gate: `NOT_REACHED`
- Scope guard: Findings are repairable within the frozen Review cycle contract；they do not authorize Draft/Research/Evidence edits by Reviewer, do not require human intervention, and do not justify changing canonical/course ownership or future Articles。

## Revision Disposition Candidates｜Cycle 0

### A19-R0-F01

- Finding ID: `A19-R0-F01`
- Files Changed: `research.md`, `evidence.md`, `outline.md`, `draft.md`
- What Changed: 全链收窄 `19-C09` 的 confirmed conclusion，只保留 Linux namespaces、network namespace 与 seccomp；Sandbox matrix 改为逐行 mixed evidence posture，filesystem view/mount/allow-write 与 secret broker/mount/environment 两行及正文、提纲、Learning Check 上下文均显式标为 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`。
- Evidence Impact: 不新增 Claim 或 Evidence Card；`19-E09` 的 `Proves / Limitations` 继续作为 ceiling，`19-C09` 仍为 CONFIRMED，但仅限该 Card 直接覆盖的 Linux mechanism scope。
- Proposed Status: `READY_FOR_RECHECK`

### A19-R0-F02

- Finding ID: `A19-R0-F02`
- Files Changed: `research.md`, `evidence.md`, `outline.md`, `draft.md`
- What Changed: `19-E04 / 19-E08` 与 Draft reference 改用 official `usnistgov/oscal-content` release/tag `v1.2.0` 和 tag-pinned OSCAL JSON；记录 annotated tag、resolved commit、catalog metadata `5.1.1+u2`、HTTP/readback identity 与 SHA-256，并把 generic CSRC page 限定为 publication entry。逐项核对 `ac-2`, `ac-3`, `ac-3.2`, `ac-3.8`, `ac-6`, `ac-24`, `au-3` 的存在与窄语义。
- Evidence Impact: 不新增 Claim 或 Card，不改变既有 status mix；固定 artifact 使 `19-C04 / C05 / C06 / C08` 的原有 NIST control mappings 可重放，同时明确 course workflow、risk route 与 record schema 不是 NIST 规定。
- Proposed Status: `READY_FOR_RECHECK`

## Cycle 1 Recheck｜A19-R0-F01 / A19-R0-F02

### Recheck Identity

- Reviewer: fresh Reviewer `/root/article19_reviewer_recheck1`
- Review date: `2026-08-26`（Asia/Shanghai）
- Gate: `REVIEW_RECHECK`
- Cycle: `1 / 3`
- Execution: `REAL_SUBAGENT / FRESH REVIEWER CONTEXT`
- Context isolation: 仅读取原始 Findings、Revision Disposition、修订后的 Research / Evidence / Outline / Draft、Reviewer/recheck contracts、trace dispatch、canonical / glossary / Article Card 与必要 primary sources；未读取 Revision Worker hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只向 `review.md` 追加本 Recheck，并向 `subagent-trace.md` 追加一个 canonical raw Worker Result；未修改 Research、Evidence、Outline、Draft、README、content、global/canonical、Lab、assets、Git 或未来 Article。

### A19-R0-F01 Recheck

- Finding ID: `A19-R0-F01`
- Original Severity: `MAJOR`
- Decision: `CLOSED`
- Artifact Evidence:
  - `research.md` 的 Terminology ledger、`19-C09` 与 Evidence Gate summary 均把 confirmed conclusion 限定为 Linux namespaces、network namespace 与 seccomp；filesystem view/mount/allow-write 及 secret broker/mount/environment 均标为 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`。
  - `evidence.md` Claim Register、`19-E09` Limitations / Course usage 与 Gate wording discipline 使用同一 ceiling；`19-E09` 没有被扩写为 filesystem 或 secret-broker primary evidence。
  - `outline.md` 的五概念表、mixed-evidence matrix、composition boundary、Learning Check 与 Claim coverage 均逐行区分 confirmed Linux mechanisms 与 unconfirmed course examples。
  - `draft.md` 不再存在全表 `CONFIRMED MECHANISM BOUNDARIES` 总标签；matrix 使用 `MIXED EVIDENCE POSTURE`，前三行为 `CONFIRMED / 19-E09`，后两行为 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`。正文、反例、Claim Traceability、Learning Check 与参考答案保持同一强度。
- Primary-source Recheck: Linux `namespaces(7)` 支持 namespace 所管辖资源视图的隔离；`network_namespaces(7)` 支持 network devices、protocol stacks、routing tables 与 firewall rules 等网络资源隔离；Linux kernel seccomp 文档只支持 syscall/kernel-surface filtering，并明确其本身不是完整 sandbox。三者都不为本文的 filesystem allow-write 或 secret-broker configuration 提供额外 confirmed 结论。
- Regression Scan: 四份修订 artifact 中未发现仍把 filesystem / secret broker 与前三类 Linux mechanism 并列为 confirmed 的宽表述，也未发现 mechanism presence 被升级为安全、无逃逸、业务授权或收益保证。
- Closure Basis: Revision 满足原 Finding 的第一条最小闭环路径：全链收窄 confirmed scope，并把两个未取证 family 显式降为 course examples；没有仅改标签而保留宽 conclusion。

### A19-R0-F02 Recheck

- Finding ID: `A19-R0-F02`
- Original Severity: `MINOR`
- Decision: `CLOSED`
- Version / Identity Recheck:
  - official repository release/tag: `usnistgov/oscal-content` `v1.2.0`；release notes 明确说明与 NIST SP 800-53 v5.1.1 CPRT release 对齐。
  - live `git ls-remote`：annotated tag object=`686109e7516295dce79e6db806e721492586da74`，peeled commit=`1763607deb4ffb3d67c59dc669a7c6404a6f93a6`。
  - tag-pinned JSON returned successfully from `https://raw.githubusercontent.com/usnistgov/oscal-content/v1.2.0/nist.gov/SP800-53/rev5/json/NIST_SP-800-53_rev5_catalog.json`；bytes=`10192497`；SHA-256=`ADC257A7A9019BED1EE11E108E62D74B531BA9C33EA89F1B9AD49FD9149ACA23`。
  - catalog metadata title=`Electronic Version of NIST SP 800-53 Rev 5.1.1 Controls and SP 800-53A Rev 5.1.1 Assessment Procedures`；version=`5.1.1+u2`；last-modified=`2023-12-05T01:16:10`。
- Control Recheck:
  - `ac-2` exists and covers account management, account-creation approval and valid access authorization；
  - `ac-3` exists and enforces approved authorizations under applicable access-control policy；
  - `ac-3.2` exists and requires dual authorization / two-person control for organization-selected actions；
  - `ac-3.8` exists and covers revocation after subject/object security-attribute changes while acknowledging differing effective times；
  - `ac-6` exists and states least privilege for authorized access necessary to assigned tasks；
  - `ac-24` exists and places access-control decision application before enforcement while allowing decision and enforcement to be separate entities；
  - `au-3` exists and requires event type、time、location、source、outcome and associated identity/entity content in audit records。
- Locator / Wording Evidence: `research.md`、`19-E04`、`19-E08` 与 Draft reference all use the same tag-pinned JSON and official release identity. The generic CSRC page is explicitly labeled publication entry only and no longer substitutes for the fixed locator. Existing `19-C04 / C05 / C06 / C08` mappings remain narrow；course workflow、R0–R3 route 与 record schema are not attributed to NIST.
- Closure Basis: source identity、version、artifact bytes/hash、control existence 与 semantic locators are replayable and mutually consistent；the original landing-page-only reproducibility defect is removed without adding a Claim or Card。

### Claim, Boundary and Mechanical Regression Audit

- Claim register: `19-C01`—`19-C10` = `10 / 10` unique Claims；Evidence Cards=`12`；registered mix remains `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。
- Runtime / Lab: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；no Lab or runtime artifact was introduced。
- BuildPilot: `DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN` remains explicit；no real approval、credential、sandbox、enforcement、publish、security、benefit or production evidence is claimed。
- Course boundary: Article 20 retains Token/Step/Cost/Latency budget ownership；Article 21 retains cross-step Trace/Replay/re-execution/Failure Taxonomy ownership。Article 19 only keeps constraint/record identity seams and does not pre-complete either Article。
- Revised Draft identity: SHA-256=`5B64D6B16CF0CC57E9D56F6B9DCE7A568DDE86A8E9079535E443B0F8C99FADD4`；bytes=`44803`；physical lines=`580`。
- Mechanical checks: nine fenced blocks are paired (`18` fence lines)；trailing-whitespace lines=`0`；placeholder hits=`0`；Hugo shortcode hits=`0`；Draft has no frontmatter as expected before Publisher。Publisher still owns frontmatter、navigation、semantic mapping and Hugo Build Verify。

### Cycle 1 Five-Dimension Score

| Dimension | Score | Threshold | Result | Recheck basis |
|---|---:|---:|---|---|
| Technical Accuracy | `19 / 20` | `>= 18` | `PASS` | Sandbox mechanism scope、authority/HITL、revocation/TOCTOU 与 non-guarantee boundaries are internally and source-consistent。 |
| Evidence Discipline | `19 / 20` | `>= 18` | `PASS` | `10 / 10` Claims、`12 / 12` Cards、zero BLOCKED；F01 wording ceiling and F02 pinned locator/control semantics independently replay。 |
| Teaching Quality | `19 / 20` | `>= 17` | `PASS` | Mixed-evidence matrix now teaches evidence strength per row；problem-first progression、Learning Check and fail-closed walk-through remain coherent。 |
| Engineering Transfer | `18 / 20` | `>= 17` | `PASS` | Authority envelope、records、state machine、revalidation and ownership seams remain actionable without claiming Runtime proof。 |
| Readability & Compression | `18 / 20` | `N/A` | `PASS` | L-weight structure is mechanically clean；explicit boundary labels are dense but serve the security/evidence ceiling。 |
| **Total** | **`93 / 100`** | **`>= 88`** | **`PASS`** | **All total and hard component thresholds pass with zero unclosed Finding。** |

Threshold check: Total `93 >= 88`、Technical `19 >= 18`、Evidence `19 >= 18`、Teaching `19 >= 17`、Engineering `18 >= 17`。Result=`ALL REQUIRED SCORE THRESHOLDS MET`。

### Cycle 1 Open Finding Summary

| Severity | OPEN | ESCALATED | CLOSED this cycle | Finding IDs |
|---|---:|---:|---:|---|
| BLOCKER | `0` | `0` | `0` | `NONE` |
| MAJOR | `0` | `0` | `1` | `A19-R0-F01` |
| MINOR | `0` | `0` | `1` | `A19-R0-F02` |
| EDITORIAL | `0` | `0` | `0` | `NONE` |
| **Total actionable unclosed** | **`0`** | **`0`** | **`2`** | **`NONE`** |

### Cycle 1 Gate Decision

`PASS / ELIGIBLE_FOR_FINAL_GATE`

- REVIEW_RECHECK execution: `COMPLETE`
- Review cycle: `1 / 3`
- Finding decisions: `A19-R0-F01 CLOSED`；`A19-R0-F02 CLOSED`
- Open Findings: `0`
- Escalated Findings: `0`
- Score: `93 / 100`
- Thresholds: `ALL MET`
- Gate completed: `true`
- Next Allowed Gate: `FINAL_GATE`
- Blocker: `NONE`
- Exact route: `REVIEW_RECHECK -> FINAL_GATE`
- Publication boundary: this decision does not execute FINAL_GATE、PUBLISH、Hugo Build、global-state mutation、Git operations or any Article 20/21 work；it only establishes eligibility for an independent FINAL_GATE execution。

## Final Gate Decision

### Final Gate Identity

- Reviewer: fresh Reviewer `/root/article19_final_reviewer`
- Review date: `2026-08-26`（Asia/Shanghai）
- Gate: `FINAL_GATE`
- Execution: `REAL_SUBAGENT / FRESH INDEPENDENT REVIEWER`
- Context isolation: 独立读取 repository instructions、canonical Factory contracts、global run state / status / course README 与 Article 19 全部 durable artifacts；未读取 Author、Revision Worker 或前序 Reviewer 的 hidden reasoning、confidence 或 self-score。
- Write scope: 本轮只向 `review.md` 追加本 Final Gate Decision，并向 `subagent-trace.md` 追加一个 canonical raw Worker Result record；未修改 Draft、Research、Evidence、Outline、README、global/canonical、Published Content、Git 或未来 Article。

### Frozen Input and Review Closure

- Frozen Draft SHA-256: `5B64D6B16CF0CC57E9D56F6B9DCE7A568DDE86A8E9079535E443B0F8C99FADD4` — independently recomputed `PASS`.
- Frozen Draft identity: `44803` bytes；`580` physical lines.
- Cycle 0 Findings: `A19-R0-F01 MAJOR` and `A19-R0-F02 MINOR`.
- Cycle 1 decisions: `A19-R0-F01 CLOSED`；`A19-R0-F02 CLOSED`.
- Current Finding state: `0 OPEN / 0 ESCALATED / 2 CLOSED`；no new Final Gate Finding opened.
- Review cycle: `1 / 3`；review-cycle exhaustion not reached.

### Independent Final Gate Audit

| Gate requirement | Independent result | Basis |
|---|---|---|
| Claim integrity | `PASS` | `19-C01`—`19-C10` = `10 / 10` unique Claims；new core Claim=`NONE`。 |
| Evidence integrity | `PASS` | `19-E01`—`19-E12` = `12 / 12` Evidence Cards；core `BLOCKED=0`；mix remains `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL`。 |
| Fixed NIST locator | `PASS` | `19-E04 / E08`、Research、Outline and Draft consistently use official `usnistgov/oscal-content` release/tag `v1.2.0` plus the tag-pinned OSCAL JSON path；catalog version=`5.1.1+u2`、recorded artifact SHA-256=`ADC257A7A9019BED1EE11E108E62D74B531BA9C33EA89F1B9AD49FD9149ACA23`；the generic CSRC page is publication entry only。 |
| NIST control mapping | `PASS` | Existing mappings remain limited to `ac-2`、`ac-3`、`ac-3.2`、`ac-3.8`、`ac-6`、`ac-24`、`au-3`；R0–R3、approval record fields and the course workflow are not attributed to NIST。 |
| Sandbox evidence posture | `PASS` | Matrix global posture is `MIXED EVIDENCE`；Linux namespaces、network namespace and seccomp are `CONFIRMED / 19-E09`；filesystem and secret-broker rows remain `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`。 |
| Sandbox guarantee boundary | `PASS` | seccomp is not promoted to a complete sandbox；mechanism presence is not promoted to authorization、safety、no-escape、success or benefit。 |
| Runtime / Lab boundary | `PASS` | Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；no Lab or runtime artifact exists。 |
| BuildPilot boundary | `PASS` | `DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN`；no real approval、credential、sandbox、enforcement、publish、security、benefit or production evidence is claimed。 |
| Course ownership | `PASS` | Article 19 owns action authority / HITL / sandbox seams only；Article 20 retains Token/Step/Cost/Latency budget，Article 21 retains cross-step Trace/Replay/re-execution/Failure Taxonomy。 |
| Article method | `PASS` | Draft follows problem space -> abstract model -> concrete BuildPilot design -> engineering / verification boundary and ends with a compressed takeaway / forward bridge。 |
| Mechanical publication preflight | `PASS` | Nine fenced blocks are paired (`18` fence lines)；trailing-whitespace lines=`0`；placeholder hits=`0`；Hugo shortcode hits=`0`；Published Content remains absent before PUBLISH。 |

### Final Score Threshold Check

| Dimension | Score | Threshold | Result |
|---|---:|---:|---|
| Technical Accuracy | `19 / 20` | `>= 18` | `PASS` |
| Evidence Discipline | `19 / 20` | `>= 18` | `PASS` |
| Teaching Quality | `19 / 20` | `>= 17` | `PASS` |
| Engineering Transfer | `18 / 20` | `>= 17` | `PASS` |
| Readability & Compression | `18 / 20` | `N/A` | `PASS` |
| **Total** | **`93 / 100`** | **`>= 88`** | **`PASS`** |

Threshold result: `ALL REQUIRED SCORE THRESHOLDS MET`.

### Publication Mechanics and Routing

- FINAL_GATE validates the frozen knowledge artifact only；it does not add Hugo frontmatter、navigation、series metadata or Published Content.
- Publisher must mechanically map the exact frozen Draft into `content/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md`，preserve semantic identity，apply repository YAML / ASCII-shortcode rules，and independently execute the required publication and Hugo Build checks.
- Publisher / Build PASS still does not equal `PUBLISHED` or `END_ARTICLE`；Master must later complete global reconciliation、the one-Article completion commit、single `main` push、remote verification and read-only post-commit reconciliation.
- Article 20 remains outside this worker execution；the only legal immediate route from this decision is `FINAL_GATE -> PUBLISH`.

### Decision

`PASS / ELIGIBLE_FOR_PUBLISH`

- FINAL_GATE execution: `COMPLETE`
- Gate decision: `PASS`
- Open Findings: `0`
- Escalated Findings: `0`
- Score: `93 / 100`
- Thresholds: `ALL MET`
- Frozen Draft: `5B64D6B16CF0CC57E9D56F6B9DCE7A568DDE86A8E9079535E443B0F8C99FADD4`
- Gate completed: `true`
- Next Allowed Gate: `PUBLISH`
- Blocker: `NONE`
- Exact route: `FINAL_GATE -> PUBLISH`
- Lifecycle implication: Article 19 is eligible to enter `FINAL` and be handed to Publisher；this decision does not itself publish, build, mutate global state, commit, push, resolve `END_ARTICLE`, or authorize Article 20 work.
