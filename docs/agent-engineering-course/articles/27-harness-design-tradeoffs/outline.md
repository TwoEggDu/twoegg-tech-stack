# Article 27 Outline｜Harness 的设计取舍：可替换性、复杂度、Bloat 与演化

Gate: `OUTLINE`

Author role: `AUTHOR`

Article type: `PRINCIPLE`

Course weight: `M`

Required Lab: `NONE`

Experiment Count: `0`

Runtime Observation: `ABSENT`

Evidence posture: `PASS / 11 CLAIMS / 11 CARDS / 1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

BuildPilot status: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`

## 0. Outline Gate Summary

This outline converts the Article 27 card and evidence into a medium-weight principle article. It closes Part V by asking whether a Harness is worth building after Articles 24-26 have already established why the shared boundary appears, how Runtime/Harness/Host responsibilities split, and what a minimum capability model can look like.

The article must not re-teach the full minimum model from Article 26. Article 27 should use that model as prior context and move the reader into adoption judgment:

```text
Article 24: why shared governance pressure appears
Article 25: who owns execution, governance, host and business responsibility
Article 26: what minimum Harness capabilities can protect shared invariants
Article 27: when building that Harness is worth the new cost, owner and risk surface
```

One-sentence thesis:

> Harness 不是成熟团队的徽章，而是一笔治理债务和工程投资；只有当重复的权限、证据、Trace、恢复和审查漂移已经比新建共享控制面更昂贵时，才值得从局部工作流逐步长出来。

## 1. Safe Front Matter Plan

Target published path: `content/ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md`

Planned front matter:

```yaml
---
title: "Harness 的设计取舍：可替换性、复杂度、Bloat 与演化"
slug: "agent-engineering-27-harness-design-tradeoffs"
date: "2026-08-30T00:00:00+08:00"
description: "从收益、成本、风险和退出条件判断什么时候值得建设 Harness，什么时候应该停在局部工作流，并用 BuildPilot V1 说明克制采用路径。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 280
weight: 3280
---
```

Navigation plan:

- Previous: Article 26, `ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md`.
- Course index: `ai-empowerment/agent-engineering-series-index.md`.
- No next-article link in this article unless Article 28 is separately authorized and published later. Article 28 / Part VI must not be started or preview-written here.

## 2. Required Opening Shape

Opening should be direct and grounded in the reader's project problem:

```text
如果这篇只记一句话：
Harness 的问题从来不是能不能设计出来，而是它什么时候比局部 Prompt、Tool wrapper、Workflow、CI 和 Review 更值得拥有。
```

Opening paragraphs:

1. Recall the previous three articles in one compact paragraph only.
2. State the new counter-question: `可以设计` does not equal `值得建设`.
3. Name the central tension:
   - unified governance reduces drift;
   - central layers add cost, bottlenecks, privacy surface, approval queues, migration burden and false safety.
4. Freeze evidence boundaries early:
   - `11 / 11` claims covered;
   - `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`;
   - Required Lab `NONE`;
   - Experiment Count `0`;
   - Runtime Observation `ABSENT`;
   - BuildPilot `NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.

Do not open with MCP, SDK names, gateway patterns or a module list. Source names belong in the evidence boundary and references, not the first teaching move.

## 3. Teaching Spine

Detailed teaching progression:

```text
1. The minimum model can be coherent and still not worth building.
2. Adoption judgment needs a two-sided model: benefit, cost and risk together.
3. Centralization solves duplicated governance drift only under enough repeated pressure.
4. Replaceability is not plugin enthusiasm; it requires real variation pressure.
5. Governance can create false safety through stale policy, stale knowledge, broad approval and trusted-looking hints.
6. No-build is a legitimate design outcome.
7. Adoption stages are reversible operating choices, not a maturity destiny.
8. BuildPilot V1 should start restrained: read-only, suggestion-first, evidence-backed, owner-reviewed.
9. The article closes Part V by teaching judgment, not by launching Article 28 or implementing BuildPilot.
```

## 4. Detailed Article Outline

### 4.1 Section 1｜先问值不值得，而不是先问能不能做

Purpose: move the reader from Article 26's capability model into Article 27's adoption question.

Key points:

- A Harness can have a coherent responsibility model and still be a bad investment for a team.
- The burden of proof belongs to the new shared layer.
- Local Prompt, Tool wrapper, Workflow, CI and Review can be correct at Stage 0 or Stage 1.
- The question is not `How complete can the Harness become?`
- The better question is `Which shared governance drift is already expensive enough to justify a new owner, new state, new policy surface and new failure modes?`

Mini figure:

```text
coherent model
   |
   v
adoption judgment
   |
   +--> repeated governance drift? yes/no
   +--> risk and scale high enough? yes/no
   +--> owner capacity exists? yes/no
   +--> rollback path exists? yes/no
```

Boundary sentence:

> 本篇不会再展开 Capability、Policy、Session、Trace 与 Recovery 的完整最小模型；那是 Article 26 的任务。本篇只讨论它们什么时候值得进入团队的真实工程表面。

Claim coverage: `27-C01`, `27-C06`, `27-C07`, `27-C09`.

### 4.2 Section 2｜收益、成本、风险要放在同一张账上

Purpose: introduce the two-sided model required by the brief.

Core model:

| Side | Questions | Examples |
|---|---|---|
| Benefit | What drift or inconsistency does shared governance reduce? | permission language, evidence acceptance, trace identity, review state, recovery semantics |
| Cost | What new work must be carried every run? | token/context cost, storage/retention, latency, operator attention, migration work |
| Risk | What new failure modes can the central layer create? | bottleneck, SPOF, policy drift, false safety, privacy exposure, approval fatigue |

Important wording:

- Use `can reduce`, `can introduce`, `must be balanced`.
- Do not claim measured ROI, latency savings, cost savings or defect reduction.
- Include the idea that architecture sources support both consolidation and caution; they do not prove agent-Harness-specific rates.

Practical action box:

```text
Before building a Harness, list:
1. repeated governance words that already disagree across workflows;
2. decisions that currently cannot be audited after the run;
3. failures that cannot be safely retried or resumed;
4. reviews that go stale but are still being reused;
5. costs the team can actually own after centralization.
```

Claim coverage: `27-C01`, `27-C02`, `27-C10`, `27-C11`.

### 4.3 Section 3｜什么时候统一治理真的值得

Purpose: answer RQ1 while preserving the central bottleneck risk.

Entry signals for centralization:

- Multiple agents or workflows use the same permission words with different actual scope.
- Evidence acceptance differs by tool, team or workflow.
- Review decisions need frozen scope, stale invalidation and trace linkage.
- Recovery depends on committed state, unknown state and side-effect uncertainty across tools.
- Capability exposure changes by user, project, workspace or risk level.
- Future knowledge intake depends on evidence provenance and freshness.

Counter-signals:

- Single low-risk assistant.
- One-off script or local checklist.
- Fixed deterministic workflow with existing CI/review gates.
- Team lacks owner capacity for policy, trace, privacy and migration.
- Central layer would become a queue for small local decisions.

Table:

| Pressure | Local symptom | Harness helps only if | New cost |
|---|---|---|---|
| Permission drift | same action allowed in one workflow and denied in another | shared authority gate can be owned and audited | policy maintenance and denial UX |
| Evidence drift | `PASS` means different evidence levels | claim/evidence/status language can be shared | artifact storage and reviewer discipline |
| Trace/recovery drift | logs exist but resume is unsafe | trace and recovery state can be joined | retention, privacy and replay limits |
| Review drift | old approval reused after scope changes | approval scope and stale rules can be enforced | approval fatigue and queueing |
| Capability drift | tool visible means tool used | visibility/authority/execution/evidence are separated | registry/version/trust upkeep |

Claim coverage: `27-C01`, `27-C02`, `27-C05`, `27-C11`.

### 4.4 Section 4｜可替换性不是提前做插件平台

Purpose: make replaceability concrete and restrained.

Core distinction:

```text
imagined future replacement
  -> not enough

real variation pressure
  -> possible reason to introduce a contract
```

Real variation pressure examples:

- second model provider;
- second host;
- second workflow;
- second evidence sink;
- second policy consumer;
- second capability implementation;
- migration requirement;
- independently versioned owner lifecycle.

Anti-patterns:

- Building a plugin marketplace before a second consumer exists.
- Treating every vendor abstraction as future-proof architecture.
- Adding adapters for imagined providers while the first local workflow is still unstable.
- Hiding lock-in inside trace format, approval record, knowledge schema or capability descriptor.

Replaceability checklist:

| Question | If answer is no |
|---|---|
| Which record must survive a Runtime/framework replacement? | keep implementation local |
| Which two consumers need the same contract today? | do not extract a platform interface |
| Which migration has a named owner and deadline? | mark replacement as proposal, not requirement |
| Which fields are stable enough for old traces/reviews to remain readable? | avoid broad versioned surface |

Claim coverage: `27-C03`, `27-C06`, `27-C08`.

### 4.5 Section 5｜Bloat 的来源：功能越多不等于治理越强

Purpose: discuss complexity and bloat without turning into generic platform complaints.

Bloat sources to name:

- capability registry growing into an unreviewed tool marketplace;
- every trace retained as if future replay is guaranteed;
- approval inserted at low-risk steps until reviewers click through;
- policy config precedence becoming impossible to explain;
- knowledge graph absorbing stale findings;
- eval/regression hooks added before stable dataset/oracle/verdict policy;
- recovery UI suggesting safety even when side effects are unknown;
- platform team becoming the bottleneck for ordinary local workflow changes.

Table:

| Bloat pattern | Why it feels attractive | Why it is dangerous | Better move |
|---|---|---|---|
| pluginize everything | looks replaceable | increases lifecycle/version/config burden | wait for real second implementation |
| trace everything | looks auditable | creates privacy/retention/replay confusion | minimize, redact and bind to claims |
| approve everything | looks safe | creates fatigue and stale decisions | route approval only at risk boundaries |
| centralize every rule | looks consistent | creates bottleneck and coupling | centralize shared invariants only |
| keep every memory | looks smart | stale knowledge becomes false fact | require provenance, freshness and retirement |

Claim coverage: `27-C02`, `27-C04`, `27-C05`, `27-C10`, `27-C11`.

### 4.6 Section 6｜虚假安全感比没有 Harness 更危险

Purpose: satisfy required false-safety failure modes.

Required failure modes:

- privacy/observability conflict;
- approval fatigue;
- policy/knowledge drift;
- migration/lock-in;
- recovery complexity;
- centralized bottleneck;
- trusted-looking annotations or hints;
- stale approval;
- redacted trace treated as complete replay evidence;
- memory treated as current fact;
- requirement guess treated as owner intent.

Suggested table:

| False safety | Looks like | Actually needs |
|---|---|---|
| `tool visible = authorized` | tool schema appears in context | use-time authority, scope and sandbox |
| `trace exists = replayable` | full logs or trace ref | environment/version/input/state and side-effect boundary |
| `redacted = safe` | sensitive values hidden in one view | minimization, retention, access and review |
| `approved once = approved forever` | human clicked or commented | actor/action/resource/scope/expiry/stale rules |
| `memory says so = true now` | prior run or KB has a note | provenance, freshness and applicability check |
| `eval passed = production safe` | fixture or regression is green | release gates, monitoring and owner acceptance |

Required language:

```text
UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW
```

Use these as explicit exits in Stage 1-4 and BuildPilot V1. Do not upgrade them to `CONFIRMED` unless evidence supports it.

Claim coverage: `27-C04`, `27-C05`, `27-C10`, `27-C11`.

### 4.7 Section 7｜No-build 不是失败，是设计判断

Purpose: make explicit no-build decision central, not an afterthought.

No-build cases:

- single low-risk assistant;
- one-off document helper;
- short-lived prototype;
- fixed deterministic script;
- stable CI/review workflow that already owns evidence and authority better;
- team lacks owner capacity for policy, privacy, trace and migration;
- domain requires human judgment and existing process gates rather than agent governance;
- evidence and authority can be handled by existing tools with less coupling.

Decision table:

| Situation | Better choice | Why |
|---|---|---|
| one-off low-risk task | Stage 0 | platform overhead exceeds reuse |
| repeated local task with weak evidence notes | Stage 1 | discipline helps before architecture |
| two workflows share evidence and permission language | Stage 2 candidate | local drift becomes review cost |
| multiple hosts/providers/evidence sinks | Stage 3 candidate | real variation pressure exists |
| multi-team shared infrastructure | Stage 4 candidate | governance owner may justify platform |
| no owner for privacy/policy/recovery | no-build or defer | unmanaged Harness is risk theater |

Required sentence:

> Defer 是设计决策，不是失败；停在 Stage 0、1 或 2 可以是正确架构。

Claim coverage: `27-C06`, `27-C07`, `27-C09`.

### 4.8 Section 8｜Stage 0-4：阶段顺序不是成熟命运

Purpose: present the graduated Stage 0-4 proposal with all required fields.

Required disclaimer:

> 这套 Stage 0-4 是课程采用模型，不是外部标准，也不是团队成熟度排名。阶段可以停留、回退、拆分或拒绝；向上移动必须来自观察到的压力，而不是架构野心。

Stage table:

| Stage | Entry signals | Build | Benefits | Costs and risks | Exit / rollback | Explicit ability not to build |
|---|---|---|---|---|---|---|
| Stage 0｜No Harness | single user, single low-risk workflow, no external side effects, no cross-run evidence promise | prompt, script, checklist or existing process | fastest path, no new owner, minimal overhead | manual discipline, weak audit, little reuse | move up only after repeated evidence/permission/trace drift appears | correct default for one-off helpers and throwaway prototypes |
| Stage 1｜Local disciplined workflow | repeated task in one team; read-only evidence or bounded approval needed; stable host/tool set | structured output, evidence notes, simple approval checklist, local conventions | reduces ambiguity without platform cost | local drift, reviewer discipline, manual stale checks | roll back if task stops repeating or review cost exceeds value | do not create registry, plugin system or session store yet |
| Stage 2｜Modular monolith Harness slice | two or more workflows share permission/evidence/trace/budget/review semantics | shared policy/evidence/session/trace contracts in one codebase; narrow extension points | same governance words mean the same thing; easier review and recovery | central bottleneck, config precedence bugs, local coupling, migration burden | simplify if one owner becomes a queue; delete unused extension points | do not make everything a plugin; keep write automation out by default |
| Stage 3｜Governed extension architecture | multiple hosts/providers/capabilities need independent lifecycle or second implementations | versioned capability registry, effective config dump, provider adapters, owner-routed review, retention/redaction policy | real replaceability, safer migration, better auditability | state growth, compatibility work, policy drift, approval fatigue, latency/storage cost | freeze adapters, retire unused versions, collapse single-consumer extension points | do not expand without second real consumer or migration pressure |
| Stage 4｜Platform / ecosystem Harness | multiple teams depend on shared agent governance infrastructure | rollout/sunset process, change-controlled policy/versioning, eval/regression hooks, operational ownership, reporting | shared infrastructure for high-risk multi-team agent work | platform bottleneck, false safety, governance theater, high migration/privacy burden | sunset capabilities, demote to Stage 2/3, move domain logic out | do not use Stage 4 to prove maturity; avoid if operations/governance cannot be staffed |

Follow-up paragraph:

- Stages are operating choices.
- The same organization may use Stage 0 for document helpers, Stage 1 for local diagnostics, Stage 2 for shared evidence workflow and Stage 4 for a high-risk platform.
- Higher stage increases required proof, not prestige.

Claim coverage: `27-C06`, `27-C07`, `27-C08`, `27-C09`.

### 4.9 Section 9｜BuildPilot V1：从 Stage 1/2 开始，而不是假装平台

Purpose: land the adoption model into BuildPilot while preserving design-only status.

Freeze status in-text:

```text
BuildPilot in Article 27:
  COURSE PROPOSAL
  DESIGN CASE
  NOT IMPLEMENTED
  NOT RUN
  READ-ONLY
  SUGGESTION-FIRST
```

Recommendation:

- BuildPilot V1 should start around Stage 1/2.
- It should not jump to Stage 3/4 extension architecture.
- It should not claim runtime, Unity/Jenkins scans, PR creation, autonomous modification, deployment, cost reduction, latency improvement, defect reduction or production safety.

BuildPilot V1 matrix:

| Decision | V1 treatment | Why |
|---|---|---|
| `ADOPT` | restricted read checks, Evidence package, Trace reference, Change Request, Human Review, unknown/stale labels, re-verification plan | these keep suggestion-first work auditable without stealing owner action |
| `SIMPLIFY` | budget as step/time/tool-call caps and stop reasons; capability list as fixed read-only list with source/version/trust notes | avoids building a cost platform or open capability marketplace too early |
| `DEFER` | multi-project knowledge graph, semantic/multi-trial eval, governed capability evolution, full durable replay, autonomous code modification, PR creation, production deployment | these require real pressure, runtime evidence and stronger authority |
| `REJECT` | claims that BuildPilot already runs, scans Unity/Jenkins, modifies code, creates PRs, improves cost/latency, lowers defects or proves production safety | no evidence exists in Article 27 scope |

Suggested figure:

```text
Owner request
   |
   v
Read-only intake + scope
   |
   v
Restricted checks
   |
   v
Evidence-backed finding
   |
   v
Change Request proposal
   |
   v
Human Review
   |
   v
Owner implements outside BuildPilot
   |
   v
Read-only re-verification plan
```

Boundary:

- This is not a BuildPilot implementation architecture.
- Article 27 should only recommend restrained adoption.
- Later BuildPilot design articles may expand, but Article 27 cannot pre-write Article 38-44.

Claim coverage: `27-C08`, `27-C09`, `27-C10`, `27-C11`.

### 4.10 Section 10｜采用前可以做的实际检查

Purpose: give reader value and practical action without inventing metrics.

Checklist A: governance drift audit.

```text
For each repeated workflow, write down:
- What does PASS mean?
- What does APPROVED mean?
- What does RETRY mean?
- What does EVIDENCE mean?
- What does TRACE prove and not prove?
- What makes old approval stale?
- Who owns privacy, retention and redaction?
- Who can reject a capability expansion?
```

Checklist B: adoption decision record.

| Field | Answer format |
|---|---|
| Problem pressure | concrete repeated drift or risk |
| Current local owner | prompt / wrapper / workflow / CI / review |
| Proposed shared owner | team/person/system that will maintain it |
| Expected benefit | qualitative only unless measured |
| New cost | token/storage/latency/reviewer/privacy/migration/operations |
| Failure mode | bottleneck, false safety, stale policy, lock-in, recovery confusion |
| No-build option | explicit Stage 0/1/local alternative |
| Exit / rollback | condition that demotes or removes the Harness slice |

Checklist C: stop-before-building questions.

- Can existing CI/review/process gates solve this with less coupling?
- Is there a second real consumer or only imagined future variation?
- Does the team know what not to collect in trace?
- Can approval be routed to the right owner without creating click-through fatigue?
- If policy or knowledge is stale, will the system say `STALE` or silently proceed?
- If the central layer is down, does the local workflow have a safe fallback?

Claim coverage: all 11, especially `27-C06`, `27-C07`, `27-C10`, `27-C11`.

### 4.11 Section 11｜本篇能建立什么，不能证明什么

Purpose: preserve evidence ceilings and reviewer traceability.

Can establish:

- shared governance can reduce duplicated drift under repeated pressure;
- shared governance also introduces bottleneck, SPOF, coupling, latency, privacy, approval and migration risks;
- token/context, storage/retention, direct cost reconciliation, user-visible latency and reviewer attention are cost classes to consider;
- replaceability should be driven by real variation pressure;
- no-build and remain-low-stage outcomes are valid course design decisions;
- Stage 0-4 is a proposal model, not an external maturity standard;
- BuildPilot V1 should remain read-only, suggestion-first and Stage 1/2-like.

Cannot prove:

- every team needs a Harness;
- the Stage model is industry standard;
- centralization improves cost, latency, quality, safety or defect rate;
- BuildPilot exists, runs, scans Unity/Jenkins, creates PRs, modifies code or deploys;
- any runtime observation, lab result, ROI metric or production validation exists for Article 27;
- Article 28 / Part VI is ready to start.

Required status close:

```text
Coverage: 11 / 11
Status mix: 1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED
Required Lab: NONE
Experiment Count: 0
Runtime Observation: ABSENT
BuildPilot: COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST
```

Claim coverage: all 11.

### 4.12 Section 12｜最短结论

Candidate final paragraph:

```text
Harness 的取舍，不是把 Agent 系统一步步升级成更大的平台，而是持续判断哪一部分治理漂移已经值得被共享承载，哪一部分仍应该留在本地流程、现有工具或人工判断里。真正稳的 Harness，不是阶段最高，而是能在收益、成本、风险、退出条件和不建设选项之间保持诚实。
```

Short final sentence:

> Harness 的成熟不是做得更大，而是知道什么时候建、建到哪里、什么时候停，以及什么时候不建。

## 5. Claim-to-Section Traceability

| Claim | Status | Outline sections | Wording ceiling |
|---|---|---|---|
| `27-C01` Shared governance benefit and central bottleneck risk | `PARTIAL` | 4.1, 4.2, 4.3 | Say `can reduce` and `can introduce`; no universal safety/cost claim |
| `27-C02` Context/trace/evidence/policy/approval costs | `PARTIAL` | 4.2, 4.3, 4.5 | Name cost classes; no numbers or measured savings |
| `27-C03` Replaceability requires real variation pressure | `PARTIAL` | 4.4 | Contract-based replaceability; no default plugin platform |
| `27-C04` Drift and stale/confused signals create false safety | `PARTIAL` | 4.5, 4.6 | Keep `UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW` states |
| `27-C05` HITL/recovery are stateful and costly | `PARTIAL` | 4.3, 4.5, 4.6 | No measured fatigue rate; route approvals carefully |
| `27-C06` Stage 0-4 adoption model | `PROPOSAL` | 4.1, 4.7, 4.8, 4.10 | Course proposal only; not maturity standard |
| `27-C07` No-build / remain-low-stage cases | `PROPOSAL` | 4.1, 4.7, 4.8, 4.10 | Defer/no-build is valid; no team-specific prescription |
| `27-C08` Restrained BuildPilot V1 | `PROPOSAL` | 4.4, 4.8, 4.9 | Design recommendation only; read-only/suggestion-first |
| `27-C09` Article reality and forbidden claims | `CONFIRMED` | opening, 4.1, 4.7, 4.8, 4.9, 4.11 | Required Lab NONE, experiment 0, runtime absent, BuildPilot not run |
| `27-C10` Observability vs privacy/secrets | `PARTIAL` | 4.2, 4.5, 4.6, 4.9, 4.10 | Trace is useful but also risk surface |
| `27-C11` Eval/regression/capability evolution need scoped pressure | `PARTIAL` | 4.2, 4.5, 4.6, 4.9, 4.10 | No single trace/eval/proposal proves production quality |

Coverage result: `11 / 11`.

## 6. Figures and Tables Plan

Required figures/tables in draft:

1. Adoption judgment flow: coherent model -> pressure -> owner capacity -> rollback path.
2. Benefit/cost/risk table.
3. Centralization pressure table.
4. Replaceability pressure checklist.
5. Bloat pattern table.
6. False-safety table.
7. No-build decision table.
8. Stage 0-4 adoption table with entry signals, build, benefits, costs, exit/rollback and not-to-build ability.
9. BuildPilot `ADOPT / SIMPLIFY / DEFER / REJECT` matrix.
10. Claim traceability table.

No generated image asset required. Use Markdown tables and compact ASCII diagrams only.

## 7. Learning Check

Draft should include a short `Learning Check` with questions like:

1. 为什么 Article 26 的最小能力模型成立以后，Article 27 仍然要问是否值得建？
2. 统一治理的收益和集中式瓶颈为什么必须一起讨论？
3. 哪些信号说明团队应该停在 Stage 0 或 Stage 1？
4. 为什么阶段顺序不是成熟度命运？
5. 可替换性为什么需要真实 variation pressure，而不是提前做插件平台？
6. `trace exists` 为什么不能直接推出 `replayable` 或 `evidence accepted`？
7. Approval fatigue 怎样让更多人工确认反而降低安全性？
8. No-build 决策应该记录哪些理由？
9. BuildPilot V1 为什么应该保持 read-only / suggestion-first？
10. 哪些 BuildPilot 能力必须 `DEFER` 或 `REJECT`？
11. Article 27 为什么不能给出 ROI、延迟、成本或缺陷下降数字？

## 8. Practical Actions for Readers

The draft should give the reader three immediately usable actions:

1. Run a governance-word audit:
   - collect `PASS / APPROVED / RETRY / EVIDENCE / TRACE / DONE` from several workflows;
   - compare owner, scope, expiry, evidence and recovery semantics.
2. Write a one-page adoption decision record:
   - problem pressure;
   - local alternative;
   - shared owner;
   - cost/risk;
   - no-build option;
   - rollback condition.
3. Build the smallest useful slice:
   - start with read-only checks and evidence/status language;
   - do not add provider adapters, plugin marketplaces, broad traces or write automation until real pressure exists.

## 9. Job Competency Coverage

This article should signal senior engineering judgment without overt self-promotion:

| Competency | How the article demonstrates it |
|---|---|
| Architecture trade-off judgment | weighs shared governance against bottleneck, coupling and migration |
| Reliability engineering | separates trace, evidence, recovery, eval and production safety |
| Security and privacy awareness | treats observability, secrets, retention and approval as risk surfaces |
| Platform restraint | makes no-build and rollback first-class decisions |
| Technical leadership | routes approval and ownership instead of turning all risk into tooling |
| Migration thinking | asks what survives provider/runtime/host/workflow replacement |
| Evidence discipline | preserves `PARTIAL / PROPOSAL / CONFIRMED` ceilings and forbids runtime claims |

## 10. Draft Guardrails

Hard prohibitions for the next gate:

- Do not repeat the full Article 26 minimum model.
- Do not start Article 28, Part VI or DeepSeek Harness source reading.
- Do not mention DSH as current source evidence for Article 27.
- Do not claim BuildPilot exists, runs, scans Unity/Jenkins, creates PRs, modifies code, deploys or verifies anything.
- Do not invent ROI, cost, token, latency, storage, reviewer-throughput or defect-reduction numbers.
- Do not claim centralization always improves safety, quality, speed or cost.
- Do not imply Stage 4 is more mature or more correct than Stage 0-2.
- Do not write `no-build` as reluctance or failure; write it as valid design judgment.
- Do not let `Trace`, `Eval`, `Approval` or `Knowledge` replace evidence acceptance and owner decisions.

## 11. Outline Gate Checklist

- [x] Article type fixed as `PRINCIPLE`.
- [x] Medium-weight scope preserved.
- [x] Problem starts from `worth building?`, not API/tool names.
- [x] Benefit/cost/risk model included.
- [x] Replaceability and anti-plugin-bloat boundary included.
- [x] False-safety failure modes included.
- [x] Explicit no-build decision included.
- [x] Stage 0-4 proposal includes entry signals, benefits, costs, exit/rollback and ability not to build.
- [x] Stage order explicitly not maturity destiny.
- [x] BuildPilot V1 remains restrained, read-only and suggestion-first.
- [x] Required Lab `NONE`, Experiment Count `0`, Runtime Observation `ABSENT`.
- [x] No metrics, no Article 28, no BuildPilot runtime claim.
- [x] Figures/tables, checks, practical actions, job competency and traceability included.
- [x] Claim coverage preserved: `11 / 11`.
- [x] Evidence mix preserved: `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
