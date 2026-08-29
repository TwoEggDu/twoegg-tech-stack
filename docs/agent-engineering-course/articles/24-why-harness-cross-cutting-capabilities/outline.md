# Article 24 Outline｜为什么最终需要 Harness：横切能力由谁承载

## Outline contract

- Article Type: `PRINCIPLE`
- Course Weight: `L / Major Core Lesson`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`
- Teaching Spine: Part IV reliable-agent controls already exist as separate contracts -> problem space（同一套身份、权限、证据、预算、Trace、审批、恢复、知识和能力发现散落在 prompt/tool/workflow 中）-> abstract model（横切能力与 shared carrying boundary）-> concrete design case（BuildPilot Unity requirement-change suggestion-first chain）-> engineering boundary（Harness 不是更长 Prompt、不是 Tool wrapper、不是业务 Workflow、不是 God Object、不是行业统一标准名）-> Article 25 bridge
- Core Claim Scope: `24-C01`—`24-C12` only；不新增 core Claim / Evidence Card
- Evidence Posture: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Proposal Discipline: Harness 是本课程工作定义；初步职责集只是 pressure map / shared-control proposal，不是 Article 26 的完整 Capability model，也不是任何厂商或行业标准
- BuildPilot Discipline: BuildPilot requirement-change scenario 只作为 bounded design case；不得写成已实现、已运行、已修改 Unity 项目、已查询 Jenkins/Unity、已创建 PR、已验证生产行为
- Future Boundary: Article 25 才正式回答 Runtime vs Harness；Article 26 才展开 Capability / Policy / Session / Trace / Recovery 最小能力模型；Article 27 才讨论成本、Bloat、演化和什么时候不该做 Harness
- Draft fact boundary: Draft 只能重组 `research.md`、`evidence.md`、Article 24 card/README、series plan、glossary 与 Published Articles 21/22 的衔接事实；若需要新的外部事实、实现事实、运行观测或 BuildPilot 证据，必须 `RETURN_TO_RESEARCH`

> 如果这篇只记一句话：`Harness 不是把 Agent 变大的新盒子，而是当身份、权限、证据、预算、Trace、审批、上下文、恢复、知识和能力发现开始跨多个局部链路漂移时，用来承载共享工程不变量的边界。`

## Reader transformation

读者开始时可能觉得“把规则写进 System Prompt”“把校验放到 Tool wrapper”“在 Workflow 里多加几步”已经足够。文章结束时，读者应能：

1. 识别哪些 concern 是局部任务能力，哪些已经变成跨 Agent / Tool / Workflow 的横切能力。
2. 解释为什么 Prompt、Tool 和业务 Workflow 都能承载一部分治理，但都不自然拥有完整 identity、permission、evidence、budget、trace、approval、context、recovery、knowledge、capability discovery 语义。
3. 说明重复实现 permission、evidence、budget、trace、approval 和 recovery 会怎样造成 policy、failure semantics、auditability 与 replay/review 语义漂移。
4. 使用一个轻量测试判断某个 concern 是否应该上移到共享边界。
5. 给出本篇最小 Harness 定义，同时准确声明它是课程术语、proposal 和 pressure map，不是行业标准或产品复刻。
6. 区分 Harness 与更长 Prompt、Tool wrapper、业务 God Object、Knowledge Base、RAG、Runtime 和 Host 的初步边界。
7. 用 BuildPilot Unity 需求变更设计案例走通 suggestion-first + Human Review 的治理链，同时保留 `NOT IMPLEMENTED / NOT RUN` 的证据上限。
8. 说清 Article 24 给 Article 25 的问题：既然共享边界需要存在，下一篇才讨论它与 Runtime 执行内核的相对位置。

## Teaching Spine

```text
Article 21/22 leave behind trace/eval/review-grade control concerns
  -> local prompt/tool/workflow can solve local work, but not shared invariants
  -> duplicated governance logic starts to drift
  -> define cross-cutting capability by consistency and audit ownership
  -> show why prompt, tool wrapper and business workflow each fail as sole owner
  -> introduce Harness as a course-defined shared execution/governance boundary
  -> immediately narrow it: not industry standard, not God Object, not full Runtime model
  -> map the first responsibility set as a pressure map, not a final interface
  -> walk BuildPilot's Unity requirement-change design case as read-only, suggestion-first
  -> show Human Review still needs executable governance state
  -> capture common bad designs and minimal corrections
  -> close with Article 25: Runtime executes; Harness control-plane split comes next
```

### Spine checkpoints

| Stage | Reader transformation | Required article artifact | Failure if omitted |
|---|---|---|---|
| Problem pressure | 从“再写一段规则”转向“共享不变量无人统一承载” | 散落治理链路 + 漂移例子表 | 文章退化成 Harness 名词解释 |
| Abstract model | 能定义横切能力并判断 concern 是否应该上移 | cross-cutting capability test + local surface responsibility table | 只剩“平台层很重要”的泛泛判断 |
| Harness introduction | 能说清 Harness 为什么出现以及不是什么 | minimum definition + non-God-Object boundary table | Harness 被误读成更长 Prompt 或全能 Runtime |
| Concrete design case | 能把模型落到 Unity BuildPilot requirement-change chain | bounded design sequence + evidence/claim labels | BuildPilot 被误写成已实现产品或真实运行 |
| Engineering boundary | 能识别坏设计和未来文章边界 | anti-pattern table + Article 25/26/27 bridge | 本篇提前吞掉后续三篇 |

## Opening bridge｜Part IV 留下了控制面，但还没回答“谁来承载”

- Reader Question: Evidence、Permission、Budget、Trace 和 Eval 都已经有了各自合同，为什么还需要再引入 Harness？
- Core Questions: `24-RQ01`、`24-RQ02`、`24-RQ07`。
- Claims / Evidence: `24-C04 PARTIAL / 24-E04`，`24-C07 PROPOSAL / 24-E07`，辅助 Published Articles 21/22。
- Planned teaching move:
  - 接住 Article 21：Trace slice 可以保存 identity、causality、unknown 和 failure candidate，但它不拥有 approval、budget、evidence acceptance 或 eval verdict。
  - 接住 Article 22：Eval Contract 可以决定样本、oracle、metric、baseline 和 regression verdict，但它不拥有运行时 identity、tool permission、context assembly 或 recovery。
  - 立刻给出新的压力：当这些控制面同时出现在多个 Agent、Tool、Workflow 和长任务里，问题不再是某一个合同设计得够不够好，而是这些合同的身份、状态、审批和证据如何保持一致。
- Boundary / Non-goal:
  - 不重讲 Article 18—22 的完整模型；只引用它们作为 Part IV 到 Part V 的接缝。
  - 不把 Trace、Eval、Permission、Budget 合并成一个万能系统；它们仍保留各自 owner。
  - 不预设 Article 25 的 Runtime/Harness 精确拆分。
- Transition purpose: 从“已有可靠性合同”过渡到“这些合同跨局部链路复用时需要共享承载边界”。
- Learning check: 如果一个 run 有 trace、eval 和 approval 记录，为什么仍可能缺 Harness 层问题？期望答案：记录本身不保证跨 workflow 的身份、权限、证据接受、预算、恢复和知识复用语义一致。
- Section takeaway: **Part IV 建立了可靠性控制的零件，Article 24 要回答的是这些零件跨系统时由谁承载。**

## Part A｜问题空间：局部链路可以工作，横切治理会散落

### 1. 一个真实团队里的散落形态：Prompt、Tool、Workflow、CI、Review checklist 和团队约定各管一段

- Reader Question: 为什么一个“安全的 AI 工程师”原型在单条链路里看起来可用，一扩展就开始变脆？
- Core Questions: `24-RQ01`、`24-RQ02`、`24-RQ03`、`24-RQ04`。
- Claims / Evidence: `24-C01 CONFIRMED / 24-E01`，`24-C02 CONFIRMED / 24-E02`，`24-C03 PARTIAL / 24-E03`，`24-C05 PARTIAL / 24-E05`。
- Planned scenario:
  - Prompt 里写“只读优先、不要修改生产文件”。
  - Tool wrapper 里写参数校验、超时、日志和局部 error。
  - Workflow 里写先扫描、再分析、再建议、再请求 review。
  - CI 里写状态检查，PR 里写 CODEOWNERS，review checklist 里写证据要求。
  - 团队约定里补“高风险操作要问人”“未知不能硬猜”。
- Required table `T24-01`:

  | Local surface | Naturally owns | Starts leaking when scaled |
  |---|---|---|
  | Prompt | 任务框定、角色、局部 instruction、示例和偏好 | 不持久化 approval，不限制实际 tool surface，不统一 evidence acceptance、trace identity、owner routing |
  | Tool wrapper | schema、validate、execute、local result、局部 error | 不自然决定当前请求是否有权、结果是否可信、预算是否允许、后续知识是否可吸收 |
  | Business Workflow | domain sequence、业务状态、确定性步骤和 Agent decision point | 不自然拥有跨 workflow 的 permission、approval、evidence、budget、trace、context、recovery |
  | CI / Review gate | 构建、检查、owner review、merge gate | 不理解 Agent 内部 step/context/unknown，不替 Agent 记录 run semantics |
  | Team convention | 人的判断、例外处理和非正式经验 | 难审计、难 replay、难判断 stale approval 或 policy drift |

- Evidence wording:
  - MCP / OpenAI / Microsoft / GitHub / Azure / NIST 来源共同支持“能力和控制机制在公开系统中被分层表达”。
  - 这不能写成“这些来源都提出了课程 Harness”，只能写成“散落和分层的责任面确实存在”。
- Boundary / Non-goal:
  - 不否定 prompt/tool/workflow；它们是必要局部 surface。
  - 不把小型单 Agent 原型也强行要求上 Harness；Article 27 才讨论成本与不适用条件。
- Transition purpose: 先承认局部 surface 有价值，再指出它们各自只能解决局部问题。
- Practical action: 让读者列出当前团队里 permission、approval、evidence、trace、budget 和 recovery 分别写在哪些地方；若同一规则出现三次以上，标记为 Harness pressure candidate。
- Section takeaway: **局部链路能把一次任务跑通，但它们不会自动把跨任务的不变量管成同一种语义。**

### 2. 漂移怎样发生：同一条规则在不同位置变成不同失败语义

- Reader Question: 重复实现横切逻辑，为什么坏处不只是“代码重复”？
- Core Questions: `24-RQ02`、`24-RQ09`。
- Claims / Evidence: `24-C05 PARTIAL / 24-E05`，`24-C03 PARTIAL / 24-E03`，`24-C08 PARTIAL / 24-E08`。
- Required drift examples:
  - Permission drift: Prompt 写“只读”，Tool wrapper 只拦截写文件，却没有拦截外部发布 API；CI 只看到结果，不知道请求是否越权。
  - Evidence drift: 一个 Workflow 接受 HTTP 200，另一个要求 artifact digest，第三个要求 device/runtime observation；最终同名 `PASS` 含义不同。
  - Trace drift: Tool log 有 invocation ID，Runtime 有 step ID，Review comment 只有文件路径；后续无法判断一个 finding 是否对应同一次 run。
  - Budget drift: 一个 Agent 按 token 停，另一个按 wall time 停，第三个在 approval pending 时继续花预算；成本记录无法比较。
  - Approval drift: Review 通过后 diff 变化，局部 workflow 没有 stale invalidation；Human Review 变成一次性文本，而不是可执行 gate。
  - Recovery drift: Retry、Resume、Rerun 都被叫成“再试一次”，effect unknown 时没有 reconcile。
- Required table `T24-02`:

  | Drift type | Looks harmless as local code | Cross-system failure |
  |---|---|---|
  | Policy drift | 每个 wrapper 都有一点权限判断 | 同一动作在不同入口得到不同 allow/deny |
  | Evidence drift | 每条 workflow 自己定义完成条件 | `PASS` 无法审计，不同证据层被混用 |
  | Identity drift | 每个系统都有自己的 ID | trace、approval、budget 和 review 无法 join |
  | Failure semantics drift | error code 各写各的 | recovery / retry / eval 无法判断同一类失败 |
  | Review drift | approval 保存在评论或 checklist | diff 变化、scope 变化、owner 变化后不知是否过期 |
  | Knowledge drift | 经验被塞进 prompt 或文档 | 不知道哪些经验来自证据，哪些只是猜测 |

- Evidence wording: Azure operations/microservices 与 NIST governance 支撑 cross-cutting concern 重复会带来维护、日志、治理和审计问题；这是 architecture analogy，因此保持 `PARTIAL`。
- Boundary / Non-goal:
  - 不给出漂移发生率、成本数字或 agent-specific 统计。
  - 不说中心化必然更便宜；只说当共享不变量增多时，局部复制会产生漂移压力。
- Transition purpose: 漂移不是抽象坏味道，而是迫使我们定义“什么算横切能力”的信号。
- Learning check: 两个 workflow 都输出 `PASS`，为什么可能无法比较？期望答案：它们可能使用不同证据层、不同 authority、不同 trace identity 和不同 failure semantics。
- Section takeaway: **重复实现的真正风险，是同一治理词在不同链路里悄悄变成不同合同。**

## Part B｜抽象模型：什么是横切能力，为什么需要共享承载者

### 3. Cross-cutting capability test：什么时候 concern 应该上移

- Reader Question: 怎样判断一个 concern 是局部实现细节，还是已经需要共享边界？
- Core Questions: `24-RQ01`、`24-RQ04`。
- Claims / Evidence: `24-C07 PROPOSAL / 24-E07`，辅助 `24-C04 PARTIAL / 24-E04`。
- Proposed test `F24-01`:

  ```text
  concern appears in local agent/tool/workflow
    -> needed by 3+ surfaces?
    -> different implementations create inconsistent failure semantics?
    -> run-after audit/replay/review depends on it?
    -> later steps depend on its prior state?
    -> owner routing or external review is involved?
    -> pause/resume/retry/replay must preserve it?
    -> it changes which capabilities the model may discover/call?
    -> it creates organizational responsibility, not only local computation?
        yes to several -> Harness pressure candidate
        no -> keep local until pressure appears
  ```

- Required checklist:
  - 多个 agent/tool/workflow 需要同一规则。
  - 不同实现会导致 failure semantics 不一致。
  - 结果需要在 run 后审计。
  - 后续步骤依赖前序 approval/evidence/budget/trace state。
  - 需要 owner routing 或外部 review。
  - 需要跨 pause/resume/retry/replay 保存语义。
  - 会改变模型可发现或可调用的 capability 集合。
  - 创建组织责任，而不只是局部计算。
- Boundary / Non-goal:
  - 这是课程 Proposal，不是外部标准。
  - 不是“满足一项就必须抽平台”；Article 27 才讨论 adoption threshold。
  - 不提前定义 Article 26 的 Capability schema。
- Transition purpose: 有了识别测试，下一节才能给出本篇最小 Harness 定义。
- Practical action: 对任一团队规则写下它出现在哪些 surface，哪些后续流程依赖它，是否有 stale/replay/review 问题。
- Section takeaway: **横切能力不是“重要能力”，而是必须跨多个局部链路保持同一语义的不变量。**

### 4. 本篇的最小 Harness 定义：shared carrying boundary，而不是最终 API

- Reader Question: 如果 Harness 不是某个行业标准组件，本课程为什么仍要给它一个名字？
- Core Questions: `24-RQ06`、`24-RQ07`。
- Claims / Evidence: `24-C01 CONFIRMED / 24-E01`，`24-C06 PROPOSAL / 24-E06`，`24-C07 PROPOSAL / 24-E07`。
- Minimum definition:
  - `Harness` 是本课程对 Runtime 周围可复用工程控制与约束层的称呼。
  - Article 24 只使用更窄定义：Harness 是承载横切控制与记录的共享边界；这些控制与记录必须在多个 Agent、Tool、Workflow 执行时保持一致。
- Required wording:
  - “本课程把这个边界称为 Harness。”
  - “这个名称不是行业统一标准。”
  - “本文只解释它为什么需要出现；完整 Runtime/Harness 责任拆分留给 Article 25。”
- Responsibility pressure map `T24-03`:

  | Concern | Why local ownership is insufficient | Article 24 wording ceiling |
  |---|---|---|
  | Identity | trace、approval、budget、review、knowledge 都需要 join 到同一 run/step/action | pressure map, not final identity model |
  | Permission | tool discovery/call 不等于当前请求获权 | grounded by MCP/tool security, not full policy engine |
  | Context | prompt 只是 context 的一部分，多来源 packing/receipt 会跨 step | do not define Article 25/26 context runtime |
  | Evidence | evidence acceptance 不能被 HTTP 200 或 tool success 冒充 | bridge from Article 18, not re-teach |
  | Budget | budget 是 admission/stopping contract，不是使用报告 | bridge from Article 20 |
  | Trace | trace 支持诊断和 replay lineage，不自动接受 evidence/eval | bridge from Article 21 |
  | Approval | human review 需要 scope、expiry、stale invalidation 和 resume state | keep as executable control, not prompt rule |
  | Recovery | retry/resume/rerun/reconcile 要共享 effect knowledge | bridge from Article 11/21, no engine |
  | Knowledge | 经验进入 future runs 需要来源、作用域和可信度 | bridge from Article 15/16/17 |
  | Capability discovery | 模型能看见/调用什么需要治理，不只是工具列表 | defer full Capability model to Article 26 |

- Boundary / Non-goal:
  - 不给出接口矩阵、插件系统、registry schema、session event storage 或 DSH 源码结论。
  - 不说所有 concern 都必须在一个进程、一个服务或一个代码库里。
- Transition purpose: 定义 Harness 后，必须立刻清理它不是什么，防止读者把它想成 God Object。
- Learning check: 为什么“课程术语”仍有价值？期望答案：它让散落的横切控制有一个讨论边界，但不假装外部生态都采用同一名字。
- Section takeaway: **给 Harness 命名，不是为了造一个大词，而是为了让共享不变量有一个可审查的承载边界。**

### 5. 三个局部 surface 为什么都不能独占 Harness：Prompt、Tool、Workflow 的责任上限

- Reader Question: 为什么不把这些规则分别塞回 System Prompt、Tool wrapper 或业务 Workflow？
- Core Questions: `24-RQ02`、`24-RQ03`、`24-RQ04`、`24-RQ06`。
- Claims / Evidence: `24-C02 CONFIRMED / 24-E02`，`24-C03 PARTIAL / 24-E03`，`24-C05 PARTIAL / 24-E05`。
- Required comparison `T24-04`:

  | Candidate owner | What it can do | What it cannot own alone |
  |---|---|---|
  | Longer System Prompt | 描述规则、解释偏好、要求输出格式、提醒未知边界 | enforce authorization、persist approval、join trace identity、verify evidence、route owner、pause/resume |
  | Tool wrapper | validate inputs、execute capability、return result/error、record local timeout | decide user/request authority globally、accept evidence、manage budget, control future capability exposure |
  | Business Workflow | arrange domain steps、encode deterministic gates、define local decision points | keep policy/evidence/trace/recovery semantics consistent across unrelated workflows |
  | CI / Review platform | enforce repo gate、owner review、status checks | understand every Agent step/context, preserve model/tool/runtime state |

- Evidence placement:
  - MCP Tools supports tool discovery/schema/call versus security/human-confirmation separation。
  - OpenAI / Microsoft approval and guardrail examples support executable placement/state controls。
  - GitHub review/CODEOWNERS supports owner routing and stale review, but remains source-control specific。
- Boundary / Non-goal:
  - 不说 Prompt 无价值；Prompt 仍是局部 behavior contract。
  - 不说 Tool Runtime 不重要；Tool Runtime 是 Article 06 已建立的必要执行管线。
  - 不说 Workflow 不需要 governance；Workflow 仍承载业务顺序。
- Transition purpose: 局部 surface 都有上限，因此 Harness 作为共享边界出现；但共享边界不能反过来吞掉业务意图。
- Practical action: 对现有工具 wrapper 的每个 `if (approved)` 或 `if (evidencePass)` 提问：这个判断是否依赖本工具之外的 actor、trace、policy、budget 或 owner state。
- Section takeaway: **Prompt 写得再长，也不能替代需要状态、权限、证据和审批生命周期的可执行控制。**

### 6. Harness 不是 God Object：共享控制面与业务意图必须分开

- Reader Question: 如果 Harness 承载这么多横切 concern，怎样避免它变成新的大泥球？
- Core Questions: `24-RQ06`、`24-RQ07`。
- Claims / Evidence: `24-C06 PROPOSAL / 24-E06`，`24-C07 PROPOSAL / 24-E07`。
- Boundary model `F24-02`:

  ```text
  Business Agent / Workflow
    owns: domain goal, interpretation, planning, suggested change, owner conversation
      |
      v uses / reports through
  Harness
    owns: shared identity, permission, evidence labels, budget, trace,
          approval state, recovery policy, knowledge intake, capability exposure
      |
      v delegates execution to
  Runtime / Tool Runtime / Host surfaces
    owns: model calls, loop execution, tool validation/execution, IO adapters
  ```

- Required anti-God-Object rules:
  - Harness 不替业务 Agent 决定需求价值。
  - Harness 不替 owner 实施生产修改。
  - Harness 不把 Knowledge Base、RAG、Skill、Tool、Runtime、CI 全吞成一个模块。
  - Harness 可以保存和检查控制事实，但不把所有业务数据复制进默认可见记录。
  - Harness 可以提出 Capability Evolution candidate，但不能静默提权、安装工具或扩展写权限。
- Evidence wording: Azure microservices/API gateway/security offload 只是类比：共享控制可以让业务服务保持聚焦；不证明 agent Harness 必须长成同一架构。
- Boundary / Non-goal:
  - 不展开 Article 27 的 bloat / sunset / maturity-stage 讨论，只提示风险存在。
  - 不把 Article 24 的 diagram 写成最终部署图。
- Transition purpose: 边界清楚后，才能把模型落到 BuildPilot，而不让 BuildPilot 变成自动改一切的系统。
- Learning check: Harness 为什么既要集中控制语义，又不能集中业务决策？期望答案：共享不变量需要一致，领域意图和责任 owner 需要留在业务链路。
- Section takeaway: **Harness 承载不变量，不接管领域意图。**

## Part C｜具体设计案例：BuildPilot 如何把 suggestion-first 做成可治理链路

### 7. 场景设定：Unity 策划需求变更先变成 Requirement Contract candidate

- Reader Question: 一个 Unity 需求变更为什么不能直接进入“帮我改代码”的 Agent 链路？
- Core Questions: `24-RQ08`、`24-RQ09`。
- Claims / Evidence: `24-C09 PARTIAL / 24-E09`，`24-C11 PROPOSAL / 24-E11`，`24-C12 CONFIRMED / 24-E12`。
- Mandatory label: `BUILDPILOT COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Scenario setup:
  - 策划提出：“这个功能现在要在低内存移动场景中工作，并且不能让包体回退。”
  - BuildPilot 不直接改 C#、配置表、资源、Importer/Meta、Addressables Group、Policy 或 Capability Registry。
  - 首先生成 `Requirement Contract candidate`，把自然语言拆成平台、约束、目标对象、验证信号和未知条件。
- Required contract fields:
  - `requester / owner candidate`
  - `target_platforms`
  - `memory / package-size constraints`
  - `affected scenes / features / assets / config tables`
  - `expected validation signals`
  - `unknowns`
  - `ambiguous clauses`
  - `contradictory constraints`
  - `evidence_needed`
- Required outputs when conditions are insufficient:
  - `AMBIGUOUS_REQUIREMENT`
  - `CONTRADICTORY_REQUIREMENT`
  - `MISSING_PREREQUISITE`
- Boundary / Non-goal:
  - 需求合同是 candidate，不是 owner-approved spec。
  - 不声称 ISO 29148 或 ADR 定义了 BuildPilot schema；只说 requirement precision、traceability、decision record 是可参考的工程实践。
- Transition purpose: 需求被结构化之后，下一步不是修改，而是受治理的只读证据收集。
- Practical action: 让读者对任一真实需求先写 `constraints / affected scope / validation signal / unknowns`，缺一项就不要进入自动修改。
- Section takeaway: **Suggestion-first 的第一步不是给方案，而是把需求缺口暴露出来。**

### 8. 只读证据收集：C#、跨表配置、资源规范和构建证据分别回答什么

- Reader Question: BuildPilot 可以读很多工程面，但每类 evidence 到底能证明什么、不能证明什么？
- Core Questions: `24-RQ08`。
- Claims / Evidence: `24-C10 PARTIAL / 24-E10`，`24-C11 PROPOSAL / 24-E11`。
- Required evidence categories:
  - C# reference scan: 识别受影响调用、入口、条件分支和潜在 owner；不证明 runtime path 一定执行。
  - Cross-table config relationship scan: 识别配置表引用、ID 关系、缺失行、冲突条件；不证明线上数据已同步。
  - Asset / import / dependency rule scan: 识别资源格式、依赖、bundle/group/layout 风险；不证明 device memory 或 package delta 已发生。
  - BuildReport or equivalent build evidence: 识别构建输入、输出、platform、packed assets、steps；不证明运行性能或用户体验。
  - Addressables Analyze or equivalent layout check: 识别 duplicate dependency、explicit/implicit asset、layout risk；不授权 auto-fix。
- Required finding states:
  - `CONFIRMED`
  - `VIOLATION`
  - `INSUFFICIENT_EVIDENCE`
  - `PERMISSION_BLOCKED`
  - `TOOL_GAP`
  - `INTENT_DRIFT`
- Evidence wording:
  - Unity BuildReport / AssetDatabase / Addressables docs 只支持 read-only evidence surfaces 存在；不证明 BuildPilot adapter 存在或已运行。
  - `Intent Drift` 必须有 evidence 支撑；仅由代码推断只能叫 `CANDIDATE_INTENT`。
- Boundary / Non-goal:
  - 不运行 Unity、不读取真实项目、不生成指标、不声明包体/内存数值。
  - 不创建 Lab、截图、runtime observation 或 experiment output。
- Transition purpose: 证据状态出来后，需要把 finding 转成带 owner routing 的 Change Request，而不是让 Agent 自己实施。
- Learning check: AssetDatabase 能证明什么？期望答案：能支撑源文件、导入表示、依赖和 metadata 类 evidence；不能单独证明 runtime memory、build delta 或线上行为。
- Section takeaway: **只读证据的价值在于缩小判断范围，不在于替 owner 执行修改。**

### 9. Evidence-backed Change Request：Human Review 是 gate，不是治理语义的存放处

- Reader Question: 如果最终仍然要人审，为什么还需要 Harness 记录 approval、stale、owner routing 和 re-verification？
- Core Questions: `24-RQ08`、`24-RQ09`。
- Claims / Evidence: `24-C03 PARTIAL / 24-E03`，`24-C08 PARTIAL / 24-E08`，`24-C09 PARTIAL / 24-E09`，`24-C11 PROPOSAL / 24-E11`。
- Required design chain `F24-03`:

  ```text
  Requirement Contract candidate
    -> missing / ambiguous / contradictory conditions
    -> read-only evidence collection
    -> finding + evidence refs + unknowns
    -> owner-routed Change Request
    -> Human Review / approve / reject / request-more-evidence
    -> owner implements outside BuildPilot
    -> BuildPilot re-verifies declared evidence
    -> Intent Ledger + Knowledge Store update candidate
    -> repeated pattern -> Rule / Test / Gate candidate
  ```

- Change Request minimum content:
  - requirement contract ref；
  - affected files/assets/config refs；
  - evidence refs and status labels；
  - proposed change intent, not patch；
  - owner routing candidate and review scope；
  - unknowns and blocked evidence；
  - stale conditions: diff changed, requirement changed, evidence expired, owner changed, capability gap；
  - re-verification plan；
  - knowledge/rule/test/gate candidate status。
- Human Review boundary:
  - Human Review 决定是否接受建议和谁实施。
  - Harness 保存 approval scope、decision state、expiry/stale conditions、trace linkage 和 re-verification requirement。
  - Review comment 不能替代可执行状态；approval 文本也不自动证明证据充分。
- Boundary / Non-goal:
  - 不声称 BuildPilot 打开 PR、发起 CODEOWNERS review、运行 status check 或写入知识库。
  - GitHub protected branch / CODEOWNERS 只是 review gate / owner routing 的工程例子，不等于 BuildPilot integration。
- Transition purpose: Change Request 链说明 Harness 的必要性；下一节要把这条链映射回前面的横切 concern。
- Practical action: 设计一个 CR 模板，要求每条建议显式列 `evidence_refs / unknowns / owner / stale_conditions / reverify_plan`。
- Section takeaway: **Human Review 是关键门禁，但门禁本身仍需要身份、范围、过期和复验语义。**

### 10. BuildPilot 场景到底证明什么，不证明什么

- Reader Question: 这个设计案例能支持 Harness 必要性到什么程度，哪些结论必须保持未证明？
- Core Questions: `24-RQ08`、`24-RQ09`。
- Claims / Evidence: `24-C11 PROPOSAL / 24-E11`，`24-C12 CONFIRMED / 24-E12`。
- What this scenario can prove as article argument:
  - suggestion-first 仍需要共享治理：系统必须一致回答谁提出需求、读过什么、证据是否足够、谁有权批准、approval 是否 stale、哪些 unknown 被保留、哪些经验进入未来知识。
  - Unity 工程语境中，C#、配置、资源、构建证据来自不同 surface；它们需要统一 identity、evidence label 和 review path 才能形成一条审计链。
  - 需求变更不是一个单点 tool call，而是一条跨 context、evidence、permission、review、recovery、knowledge 的控制链。
- What this scenario must not claim:
  - BuildPilot 已存在、已运行、已修改 Unity 项目、已创建 PR、已调用 Unity/CI/Jenkins、已验证 device/runtime behavior。
  - Requirement Contract、Intent Ledger、Knowledge Store、Rule/Test/Gate candidate 已有 stable schema。
  - Intent Drift 已在真实项目中确认。
  - Unity read-only evidence 一定足以覆盖所有项目。
  - suggestion-first 能保证不会出错、不会回归或永不复发。
- Required wording block:

  ```text
  本节是 COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN。
  它只说明一条受治理建议链为什么需要共享承载边界，
  不说明 BuildPilot 已经具备这些能力。
  ```

- Transition purpose: 案例完成后，回到架构判断：哪些坏设计会把 Harness 写歪。
- Learning check: 如果 BuildPilot 输出了 Change Request，为什么仍不能说“修复已经完成”？期望答案：owner 尚未实施，re-verification 未运行，runtime/device/build evidence 未取得，CR 只是建议与治理记录。
- Section takeaway: **设计案例的价值，是把 Harness 压力落到可审计链路，而不是伪造实现事实。**

## Part D｜工程边界：Harness 应该收敛什么，不应该吞掉什么

### 11. 一套 Harness 设计通常怎样写坏

- Reader Question: 引入 Harness 后，最容易把哪些边界写歪？
- Claims / Evidence: 不新增 Claim；使用 `24-C01`—`24-C12` 作为 review heuristics。
- Required anti-pattern table `T24-05`:

  | Shortcut | Responsibility swallowed | Minimum correction |
  |---|---|---|
  | `longer prompt = governance` | executable state, approval lifecycle, evidence acceptance | prompt describes behavior; Harness stores/enforces control facts |
  | `tool registered = safe to use` | permission, trust, budget, audit | separate discovery, authorization, approval, execution and evidence |
  | `workflow has steps = shared control solved` | cross-workflow identity and policy semantics | workflow owns domain sequence; shared controls keep common meanings |
  | `trace exists = evidence accepted` | claim/evidence acceptance | trace provides lineage; evidence contract decides acceptance |
  | `eval pass = release safe` | production risk owner and post-release monitoring | eval is release input with scope and limitations |
  | `human approved = always valid` | scope, expiry, stale invalidation | bind approval to diff/requirement/evidence/version and revalidate on change |
  | `knowledge captured = true forever` | source scope, staleness, confidence | store provenance, applicability, confidence and retirement conditions |
  | `capability gap = install more tools` | permission and capability governance | propose Governed Capability Evolution; no silent privilege/tool expansion |
  | `Harness owns everything` | domain intent and business ownership | keep business planning, owner decision and implementation outside shared control |
  | `Harness is an industry standard` | course terminology boundary | say “本课程把这个边界称为 Harness” |

- Boundary / Non-goal:
  - 反模式是 review checklist，不是 industry taxonomy 或 failure frequency data。
  - 不展开 Article 27 的完整 cost/bloat analysis。
- Transition purpose: 反模式收束后，最后明确本篇的 claim/evidence 上限与下一篇边界。
- Practical action: 评审任一 Harness 提案时，先找它有没有把 business owner、runtime loop、capability registry、knowledge base、evidence store、review gate 写成一个不可替换大块。
- Section takeaway: **Harness 的成熟不是管得更多，而是让共享控制一致、可审计、可替换，同时不吞业务。**

### 12. 本篇能建立什么，不能证明什么

- Reader Question: Article 24 完成后，读者可以带走哪些确定结论，哪些必须留给后续文章或实现验证？
- Claims / Evidence: `24-C01`—`24-C12` 全覆盖。
- Can establish:
  - 公开 agent/protocol/workflow 系统中，instruction、resources、tools、processes、guardrails、tracing、approvals 等确实常作为不同机制出现；本课程 Harness 名称不是外部标准。
  - Tool discovery/schema/invocation 不等于 permission、trust 或 evidence acceptance。
  - Approval、guardrail、trace、budget、evidence、eval 等需要状态、placement、refs 或独立合同，不能全由 prompt 文本承担。
  - 重复实现横切治理逻辑会带来 drift pressure；这是有架构/治理类比支持的 `PARTIAL` 工程判断。
  - Article 24 可以提出 Harness 作为 shared control plane / carrying boundary，并保留 non-God-Object 边界。
  - BuildPilot Unity requirement-change scenario 可以作为 read-only, suggestion-first design case，用来展示横切治理链路。
  - Article 24 Required Lab=`NONE`、Experiment Count=`0`、Runtime Observation=`ABSENT`、BuildPilot=`NOT IMPLEMENTED / NOT RUN`。
- Cannot prove:
  - Harness 是行业统一组件或所有团队都应该采用。
  - 本课程的初步职责集是完整、最小、唯一或已被实现验证的 Capability model。
  - BuildPilot 已拥有 runtime、UI、schema、registry、trace store、knowledge store、Unity adapter 或 review integration。
  - 任何 Unity 项目已经被扫描、修改、构建、发布或 device-tested。
  - suggestion-first / Human Review / Harness 能保证安全、合规、无回归、无 bloat 或永久有效。
- Final boundary sentence:
  - `本文只证明横切治理压力需要一个共享承载边界；这个边界怎样与 Runtime 分工、怎样建能力模型、怎样控制复杂度，分别留给接下来的三篇。`
- Transition purpose: 自然交给 Article 25，而不是在本篇提前实现 Runtime/Harness split。
- Section takeaway: **Article 24 的终点不是完整 Harness 设计，而是让“为什么需要它”变成可审查的工程结论。**

## Claim-to-section coverage（12 / 12）

| Claim | Status ceiling | Primary sections | Evidence Card | Mandatory wording / boundary |
|---|---|---|---|---|
| `24-C01` | `CONFIRMED` | Opening, 1, 4, 12 | `24-E01` | public systems separate primitives; course Harness is not industry standard |
| `24-C02` | `CONFIRMED` | 1, 5, 11, 12 | `24-E02` | tool discovery/call != permission/trust/evidence acceptance |
| `24-C03` | `PARTIAL` | 1, 2, 5, 9 | `24-E03` | approval/guardrails need executable state/placement; examples are SDK-scoped |
| `24-C04` | `PARTIAL` | Opening, 3, 4, 12 | `24-E04` | trace/evidence/budget/eval related but independent |
| `24-C05` | `PARTIAL` | 1, 2, 5, 11 | `24-E05` | drift pressure by architecture analogy; no agent-specific statistics |
| `24-C06` | `PROPOSAL` | 4, 6, 11, 12 | `24-E06` | Harness as shared control plane, not God Object |
| `24-C07` | `PROPOSAL` | Opening, 3, 4, 6, 12 | `24-E07` | initial responsibility set is pressure map; Article 25/26 deferred |
| `24-C08` | `PARTIAL` | 2, 9, 11 | `24-E08` | owner routing/review gates/stale approval are governance examples, not BuildPilot integration proof |
| `24-C09` | `PARTIAL` | 7, 9, 12 | `24-E09` | requirement/intent/knowledge chain is synthesis/proposal grounded in practices |
| `24-C10` | `PARTIAL` | 8, 10, 12 | `24-E10` | Unity read-only evidence surfaces exist; no adapter/run/project evidence |
| `24-C11` | `PROPOSAL` | 7, 8, 9, 10, 12 | `24-E11` | BuildPilot scenario is design case only |
| `24-C12` | `CONFIRMED` | Opening, 10, 12 | `24-E12` | Required Lab NONE; Experiment 0; Runtime ABSENT; BuildPilot not implemented/run |

Coverage=`12 / 12`；Evidence Cards=`12 / 12`；Status mix=`3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Core Question coverage（9 / 9）

| Core Question | Primary sections | Claims / Evidence | Required boundary |
|---|---|---|---|
| Q1 什么是横切能力 | 1, 3, 4 | `24-C07 / E07` | course proposal, not standard |
| Q2 为什么不属于 Prompt | 1, 5, 11 | `24-C01/C03/C05 / E01/E03/E05` | prompt remains useful but insufficient alone |
| Q3 为什么不属于 Tool | 1, 5, 11 | `24-C02 / E02` | tool discovery/call != governance |
| Q4 为什么不属于业务 Workflow | 1, 2, 5, 6 | `24-C05/C06 / E05/E06` | workflow owns domain sequence |
| Q5 重复实现如何漂移 | 2, 11 | `24-C05 / E05` | partial, no statistics |
| Q6 Harness 不是什么 | 4, 5, 6, 11, 12 | `24-C01/C06/C07 / E01/E06/E07` | not prompt, tool, workflow, God Object, standard |
| Q7 初步职责集 | 3, 4, 6, 12 | `24-C07 / E07` | pressure map; full model deferred |
| Q8 BuildPilot 为什么需要 Harness | 7, 8, 9, 10 | `24-C08/C09/C10/C11 / E08-E11` | design case only |
| Q9 suggestion-first 为什么仍需治理 | 7, 9, 10 | `24-C03/C08/C09/C11 / E03/E08/E09/E11` | Human Review is gate, not full governance store |

## Figures, tables and examples plan

| ID | Form | Teaching responsibility | Evidence source | Mandatory label / restraint |
|---|---|---|---|---|
| `T24-01` | local surface responsibility table | 展示 Prompt/Tool/Workflow/CI/Review/Convention 各自只能管一段 | `E01/E02/E03/E05` | local surfaces remain necessary |
| `T24-02` | drift type table | 把 duplication 的风险从“重复代码”升级为 failure semantics / audit drift | `E05/E08` | PARTIAL / analogy-supported |
| `F24-01` | cross-cutting capability test | 判断 concern 是否应上移到 shared boundary | `E07` | COURSE PROPOSAL |
| `T24-03` | Harness responsibility pressure map | identity/permission/context/evidence/budget/trace/approval/recovery/knowledge/capability discovery | `E04/E07` | not final Capability model |
| `T24-04` | candidate-owner comparison | 解释 prompt/tool/workflow/CI 为什么不能独占治理 | `E02/E03/E05/E08` | not anti-prompt/tool/workflow |
| `F24-02` | non-God-Object boundary diagram | 分离 Business Agent/Workflow、Harness、Runtime/Tool Runtime/Host | `E06/E07` | not deployment diagram |
| `F24-03` | BuildPilot requirement-change sequence | 把抽象落到 Unity suggestion-first governance chain | `E08/E09/E10/E11` | DESIGN CASE / NOT IMPLEMENTED / NOT RUN |
| `T24-05` | anti-pattern table | 汇总常见误读和最小修正 | `E01-E12` | review heuristic |

Asset policy: Outline/Draft 优先使用 Markdown 表和 ASCII 图；本 Gate 不创建 `assets/`，不生成截图，不伪造 UI、Trace 或 BuildPilot artifact。若 Publisher 后续需要静态图，图中必须保留 `PROPOSAL / PARTIAL / NOT RUN` 标签。

## Learning Check（题目 + answer expectations）

1. 为什么 Article 18—22 已经有 Evidence、Permission、Budget、Trace、Eval，还仍然需要 Article 24？
   - Expected: 这些是不同控制合同；一旦跨多个 Agent/Tool/Workflow 复用，就需要共享身份、权限、状态、审批、证据、预算、恢复和知识语义。
2. 横切能力和“重要能力”有什么区别？
   - Expected: 横切能力的核心是多条局部链路需要同一语义、后续审计/恢复/审批依赖它，而不只是功能重要。
3. 为什么更长的 System Prompt 不能替代 Harness？
   - Expected: Prompt 能描述规则，不能单独 enforce authorization、persist approval、join trace identity、accept evidence、route owner 或 pause/resume。
4. Tool discovery 为什么不等于 permission 或 evidence acceptance？
   - Expected: schema/list/call 只说明能力可发现/可请求；当前用户、请求、输出可信度、预算和审批仍需独立治理。
5. Workflow 已有确定步骤，为什么仍不能拥有所有横切治理？
   - Expected: Workflow 表达 domain sequence；跨 workflow 的 policy、trace、budget、evidence、approval 和 recovery 语义会漂移。
6. Harness 为什么不是 God Object？
   - Expected: Harness 承载共享控制不变量；业务 Agent/Workflow 保留 domain goal、需求解释、owner conversation 和真实实施。
7. Article 24 的 Harness 职责集为什么只能叫 pressure map？
   - Expected: 它由课程前文和公开机制综合而来，完整 Runtime/Harness split 与 Capability model 留给 25/26，没有实现或标准证明。
8. BuildPilot 需求变更案例的第一步为什么是 Requirement Contract candidate？
   - Expected: 先暴露平台、约束、影响范围、验证信号、unknown/ambiguous/contradictory 条件；owner 尚未批准前不能当最终 spec。
9. Unity read-only evidence surfaces 能支持什么，不能支持什么？
   - Expected: 支持代码引用、配置关系、资源/import/dependency、BuildReport 或 Addressables layout 类证据类别；不证明 BuildPilot adapter 已存在、项目已扫描、runtime/device 指标已验证。
10. Human Review 为什么不是全部治理语义？
    - Expected: Review 是 gate；仍需记录 approval scope、expiry/stale、owner routing、trace/evidence refs、re-verification 和 knowledge intake。
11. `INTENT_DRIFT` 和 `CANDIDATE_INTENT` 怎样区分？
    - Expected: 前者必须有证据支持需求/实现意图偏离；仅从代码或配置推断时只能保留 candidate。
12. 如果 BuildPilot 输出了 Change Request，文章能否说修复完成？
    - Expected: 不能；BuildPilot 未实现/未运行，owner 未实施，re-verification 未运行，没有 Unity/CI/device/runtime 证据。
13. Article 25、26、27 分别接什么，Article 24 不能提前做什么？
    - Expected: 25 接 Runtime vs Harness；26 接 Capability minimum model；27 接 design tradeoff/bloat/adoption；24 只证明必要性。

## Practical reader actions

| Action | Minimum artifact | Review question | Evidence ceiling |
|---|---|---|---|
| locate scattered governance | local-surface inventory | permission/evidence/budget/trace/approval/recovery 分别写在哪里？ | project self-audit |
| identify drift pressure | duplication/drift table | 同一 `PASS/APPROVED/RETRY` 在不同链路是否含义相同？ | analogy-informed |
| classify cross-cutting concern | concern test checklist | 是否跨多 surface、可审计、影响 owner/replay/recovery/capability exposure？ | COURSE PROPOSAL |
| define minimal Harness boundary | responsibility pressure map | 哪些共享不变量归 Harness，哪些领域意图仍归业务链路？ | PROPOSAL |
| preserve local surface value | Prompt/Tool/Workflow candidate-owner comparison | 当前 concern 是描述、执行、业务编排，还是共享治理？ | source-supported partial |
| structure requirement change | Requirement Contract candidate | 约束、影响范围、验证信号和未知是否已写清？ | design case |
| collect read-only evidence | evidence category ledger | 每类 evidence 证明什么、不证明什么？ | Unity-doc-backed partial |
| route Change Request | CR with refs/owner/stale/reverify plan | 人审时范围、证据、过期条件和复验是否可追？ | design case |
| propose capability evolution | tool gap / capability gap record | 新能力是否走审批和最小权限，而不是静默扩展？ | proposal |

## Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Cross-cutting architecture | local surface table + concern test | 能判断 concern 何时从局部实现上移到共享边界 | course proposal, not universal standard |
| Reliable-agent integration | Part IV bridge + responsibility pressure map | 能让 Evidence、Permission、Budget、Trace、Eval 各守 owner 又可关联 | no full Runtime/Harness model |
| Governance drift reasoning | drift examples + anti-pattern table | 能识别 policy/evidence/identity/failure/review/knowledge drift | architecture analogy only |
| Boundary discipline | non-God-Object diagram | 能分离 business intent、shared controls、runtime execution 和 host/tool surfaces | not final deployment/API |
| Evidence communication | claim/evidence/status matrix | 能保留 `CONFIRMED/PARTIAL/PROPOSAL` 和 `NOT RUN` 标签 | no status upgrade |
| Unity design-case thinking | BuildPilot requirement-change chain | 能把需求、证据、owner review、re-verification 和 knowledge loop 串成受控建议链 | BuildPilot not implemented/run |
| Human review engineering | Change Request minimum content | 能说明 human review 需要 scope、expiry、stale、trace/evidence refs | no real GitHub/PR integration claim |
| Capability governance | Tool Gap / Governed Capability Evolution wording | 能拒绝 silent tool install / privilege expansion | Article 26 model deferred |
| Technical writing | problem -> abstract model -> concrete case -> boundary | 不以 API、SDK 或厂商产品开篇 | not product tutorial |

## Frontmatter and publication plan

```yaml
---
title: "为什么最终需要 Harness：横切能力由谁承载"
slug: "agent-engineering-24-why-harness-cross-cutting-capabilities"
date: "2026-08-29T00:00:00+08:00"
description: "从身份、权限、证据、预算、Trace、审批、恢复、知识和能力发现的漂移压力，解释为什么 Agent 系统最终需要一个共享治理边界。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 250
weight: 3250
---
```

- Published Path: `content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md`
- Previous link: Article 22 exact published path；Article 23 is `ADVANCED / OPTIONAL / SKIPPED / PLANNED / NOT_STARTED`，Draft 可用一句话说明 22 后可直接进入 24，不必创建 Article 23 link。
- Next link: Article 25 planned title only if Publisher later confirms path exists；Draft 阶段可用 prose bridge，不创建 broken `relref`。
- Course index link: existing Agent Engineering series index。
- Metadata rationale: series plan assigns ID 24 after skipped optional 23；`series_order=(24+1)*10=250`，`weight=3000+250=3250`。
- YAML quote rule: title/description 当前不含 ASCII quotes；若后续加入引号，需按 AGENTS.md 改用安全 YAML quoting。

## Exact no-new-fact boundary for Draft

Draft may:

- paraphrase and reorganize only `24-C01`—`24-C12` and `24-E01`—`24-E12`；
- use Published Article 21 only for trace/eval handoff and identity/causality/replay/effect boundaries that are needed as continuity, not as repeated main content；
- use Published Article 22 only for candidate-to-Golden/Eval/Regression boundary and Part IV closure, not to re-teach Eval model；
- state Harness as course-defined shared carrying boundary / control plane；
- present identity、permission、context、evidence、budget、trace、approval、recovery、knowledge、capability discovery as initial responsibility pressure map；
- write BuildPilot as read-only, suggestion-first Unity requirement-change design case with mandatory `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN` wording；
- mention external sources exactly within evidence-card ceilings, preserving accessed-date/drift limits when needed；
- include `UNKNOWN`、`INSUFFICIENT_EVIDENCE`、`PERMISSION_BLOCKED`、`TOOL_GAP`、`CANDIDATE_INTENT` and `INTENT_DRIFT` only as design-case status labels。

Draft must not:

- introduce a new core Claim/Evidence Card beyond `24-C01`—`24-C12`；
- claim Harness is an industry standard, product requirement, universal architecture or complete interface model；
- upgrade `PARTIAL` or `PROPOSAL` claims to `CONFIRMED`；
- define Article 25 Runtime/Harness split, Article 26 Capability model or Article 27 adoption/bloat/cost tradeoff beyond short forward pointers；
- describe BuildPilot as implemented, run, integrated, deployed, measured, production-ready or capable of direct automated fixes；
- claim any Unity project was scanned, modified, built, profiled, packaged, device-tested or runtime-observed；
- fabricate metrics, screenshots, logs, PRs, owner approvals, BuildReports, Addressables Analyze outputs or device observations；
- turn suggestion-first into proof of safety, compliance, no regression, no human error or future prevention；
- propose silent tool install, privilege expansion, registry mutation or direct production write；
- create Draft/Review/Published Content/assets/Lab/runtime/global/canonical/Git/future-Article artifacts during OUTLINE gate。

Trigger: if Draft needs any fact outside this boundary, return `RETURN_TO_RESEARCH` with the exact missing Claim/Evidence need; do not fill it with memory, inference or “common practice”。

## Explicit non-scope

- 不写成任何 MCP、OpenAI Agents SDK、Microsoft Agent Framework、GitHub、OpenTelemetry、NIST、Unity 或 Azure 产品教程。
- 不实现或设计完整 Harness API、Plugin system、Capability Registry、Policy Engine、Session Store、Trace Store、Evidence Store、Knowledge Store、Replay Engine、Eval Runner 或 BuildPilot Runtime。
- 不运行 Lab、外部网络、Provider call、Unity Editor、Jenkins、CI、package build、device test、Addressables Analyze 或真实项目扫描。
- 不创建 Article 25—28 workspace/assets/content，不启动 Article 28。
- 不修改 `research.md`、`evidence.md`、README、article-card、series plan、status tracker、published content 或任何全局/canonical 文件。
- 不创建 branch/worktree/commit/push。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`。

## Closing bridge

- Closing sentence: `Harness 不是更聪明的 Prompt，也不是更大的业务 Agent；它是当横切控制必须跨多条执行链保持同一语义时，团队为这些不变量设置的共享承载边界。`
- Bridge to Article 25:
  - Article 24 只证明“为什么需要共享边界”。
  - Article 25 才回答“Agent Runtime 执行什么，Harness 治理什么，Host 又在什么位置承载它们”。
  - Article 26 再把 Capability、Policy、Session、Trace、Recovery 做成最小模型。
  - Article 27 最后讨论复杂度、Bloat、可替换性、演化和何时不该引入。
- Mandatory final boundary sentence: **知道 Harness 为什么出现，只是进入 Part V 的第一步；真正的工程问题，是下一篇要把执行内核和治理控制面切清楚。**

## OUTLINE Gate checklist

- [x] Article Type fixed as `PRINCIPLE`；L-weight structure follows problem space -> abstract model -> concrete design case -> engineering boundary，not API-first。
- [x] Teaching Spine begins from Part IV control contracts and cross-system drift pressure, then introduces Harness as shared carrying boundary。
- [x] Required central pressure is explicit: local prompt/tool wiring cannot consistently own identity、permission、context、evidence、budget、trace、approval、recovery、knowledge、capability discovery across many agents/workflows。
- [x] Harness is introduced as course-defined shared control plane with strict non-God-Object boundary。
- [x] BuildPilot Unity requirement-change scenario is bounded as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN` and read-only/suggestion-first。
- [x] Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；no experiment output, metric, screenshot, runtime observation or project experience invented。
- [x] Article 25 Runtime/Harness split, Article 26 Capability model and Article 27 design tradeoffs are explicit next-article boundaries, not defined here。
- [x] Every major section includes Reader Question、Claims/Evidence、Boundary/Non-goal、Transition purpose or Practical action、Learning Check and takeaway。
- [x] Claim coverage=`12 / 12`；Evidence Cards=`24-E01`—`24-E12` only；new core Claim/Card=`NONE`。
- [x] Evidence posture preserved exactly: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。
- [x] Figures/Tables、Learning Checks、Practical Actions、Job Competency、Frontmatter plan and Draft no-new-fact boundary are complete。
- [x] No Draft/Review/content/assets/Lab/runtime/global/canonical/Git/future-Article artifact belongs to this Author result。
- [x] OUTLINE Gate recommendation: `PASS`；next allowed gate candidate: `AUTHOR_DRAFT`；Master validation remains outside this artifact。
