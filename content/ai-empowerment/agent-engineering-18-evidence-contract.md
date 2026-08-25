---
title: "Evidence Contract：把自然语言推断变成可审计工程数据"
slug: "agent-engineering-18-evidence-contract"
date: "2026-08-25T00:00:00+08:00"
description: "用 Claim、Evidence、Observation、Inference 与 policy-bound decision 建立可审计的工程判断。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Evidence Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 190
weight: 3190
---

> **上一篇**：[Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt]({{< relref "ai-empowerment/agent-engineering-17-skill-engineering.md" >}})

# Evidence Contract：把自然语言推断变成可审计工程数据

> 如果这篇只记一句话：`工程判断不是一句更确定的话，而是一条能回答“主张是谁、证据在哪、适用到哪、谁按什么规则接受”的可审计链。`

假设 BuildPilot 返回了下面这份诊断：

> **构造示例｜COURSE DESIGN / NOT EXECUTED / NOT A RUNTIME OBSERVATION**
>
> ```yaml
> conclusion: "root cause is X"
> citation: "build.log#L420"
> trace_id: "trace-001"
> confidence: HIGH
> ```

它有自然语言结论，有合法的结构，有 citation，有 Trace ID，甚至还有 confidence。可 Reviewer 继续追问时，问题马上出现了：这是哪个 build？`build.log#L420` 对应哪一份内容？那里直接观察到了什么？从 Observation 到 root cause 经过了什么推断？有没有相反证据？`HIGH` 使用什么尺度？最后又是谁按哪一版规则接受了它？

只要这些问题答不出来，这份输出就仍是一个 **Claim candidate**，不是已经成立的工程事实。

这并不表示自然语言、JSON、引用或 Trace 必然错误。它只说明这些信号各自解决了一部分问题，单独都不足以完成 Evidence support 与 policy acceptance。Article 03 已经把 Parse、Schema、DTO 与 Domain Validation 分开；Article 06 已经让 Tool Runtime 保存 Result 和 terminal Trace。但“对象能被机器处理”“执行记录真实存在”，仍不等于“某个工程主张已经得到当前作用域内的接受”。

本文没有 Lab，也没有执行 BuildPilot。Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。后文所有 BuildPilot 内容均为 **DESIGN / NOT IMPLEMENTED / NOT RUN**，不包含准确率、成本、延迟、收益或生产结论。

## 为什么“说得像事实”不是“事实已经成立”

自然语言特别擅长把多层责任压成一句顺滑结论：

- “日志表明 X，所以根因就是 Y。”Observation 与 Inference 被写在一起。
- “这里有一个链接，所以有证据。”Citation 与 Evidence support 被写在一起。
- “这条记录有来源链，所以可信。”Provenance 与 Acceptance 被写在一起。
- “置信度很高，所以可以执行。”Confidence 与 approval 被写在一起。
- “Trace 里出现了这一步，所以因果关系已经证明。”execution record 与 Claim acceptance 被写在一起。

这种压平在 Agent 系统里尤其危险。信息可以被装入 Context、写进 Working Memory、保存到 Project Memory、从 KB/RAG 检索出来，或者由 Skill 带入当前任务；这些动作最多说明信息被选择、保存、召回或加载过，不会自动把它升级为 accepted fact。

JSON Schema Draft 2020-12 的 Validation §3 把 validation 定义在 instance 是否满足 schema assertions 的范围内。它能帮助程序拒绝结构不合法的对象，却不负责证明对象中的诊断真实、来源适用或已经获得批准。in-toto Attestation Framework v1.0 又把 `subject`、`predicateType` 与 `predicate` 分开；SLSA v1.2 的 artifact verification 还要继续检查 subject digest、信任根、expectations 和 policy，并在 Verification Summary Attestation 中另外记录 verifier、policy、`timeVerified` 与 `verificationResult`。

这些规范共同支持一个窄结论：**结构检查、对象身份、来源材料与 policy-bound decision 是可以分开的检查面。**它们不证明所有自然语言 Claim 都是错的，也不规定本文稍后提出的完整字段和 Gate 顺序。

如果要把问题拉回工程数据，可以先问六个问题：

1. 精确的 Claim 是什么，它指向哪个 subject？
2. Evidence 从哪里来，是否能定位到固定版本或内容？
3. Observation 直接记录了什么，哪些内容只是 Inference？
4. 结论适用于哪个时间、版本和 scope？
5. 已知 limitation、counter-evidence 与 falsifier 是什么？
6. 谁按哪一版 policy，对哪一组固定输入作出了什么 decision？

结构正确让对象可处理；回答这些问题，才开始建立 Evidence Contract。

## 抽象模型：先把六种陈述角色分开

下面六个定义是 **本课程的工作模型**，不是 W3C、SLSA 或所有组织共享的统一 ontology。

| 角色 | 本课程中的定义 | 不能冒充什么 |
|---|---|---|
| Claim | 等待支持、反驳，或等待明确标为设计选择的可审计命题 | 已成立事实 |
| Evidence | 在明确 scope 下支持或反驳 Claim 的可定位来源、制品、Observation、实验记录或设计记录 | 自动可信的结论 |
| Observation | 从指定来源或记录直接读到的有界事实 | 因果解释 |
| Inference | 从 Evidence 推出的解释，需公开输入、规则、替代解释与 falsifier | 直接观测 |
| Proposal | 尚未实现或尚未运行的设计选择 | 运行行为或效果 |
| Unknown | 输入缺失、冲突未解或 scope 不足时的诚实终态 | 低 confidence 的猜测 |

这组六分法属于 `18-C02 = PROPOSAL`。它与 `CONFIRMED / PARTIAL / BLOCKED / PROPOSAL` 证据状态是两条正交的轴：前者说明一段陈述扮演什么角色，后者说明当前 Evidence 对 Claim 支持到什么程度。一个来自标准的 Observation 可以支持 `CONFIRMED` Claim；一套设计精良但没有实现的字段模型，仍然必须保持 `PROPOSAL`。

这个区分能阻止一种常见偷换：模型先把解释写成 Observation，系统再因为“Observation 已记录”把它接受为事实。真正可审计的链路应该保留每一步的角色，而不是只保留最后一句话。

## Citation、Provenance、Confidence、Acceptance 是四次不同审查

这四个词经常同时出现在报告里，却回答不同问题。

| Concern | 它回答什么 | 它不自动证明什么 |
|---|---|---|
| Citation | 去哪里查看支持材料 | 内容真实、适用，或完整支持 Claim |
| Provenance | 材料由哪些 entity、activity、agent 产生、派生或修订 | 材料可信，或 Claim 已被接受 |
| Confidence | 评估者在已声明、可能未校准的 scheme 下有多确定 | 概率、真值或批准 |
| Acceptance | 哪版 policy 对固定 Claim/subject 与 Evidence inputs 作了什么决定 | 永真、production approval，或未来版本仍成立 |

W3C PROV-DM 2013 Recommendation 在 §§2–3 建立 entity/activity/agent 与 generation/derivation 关系，并在 §5.1.8、§5.2.2、§5.2.4、§5.4.1 分别讨论 invalidation、revision、primary source 与 bundle。它把 provenance 视为可用于质量、可靠性或可信度评估的信息，而不是自动完成信任判断。SLSA v1.2 的 VSA 则把 verifier、policy、输入 attestations、验证时间与结果单独记录。

这些来源支持 Provenance 与 Acceptance 分开，却没有统一规定本文的四分法，更没有提供一套跨系统校准的 confidence scale。因此 `18-C05` 只能保持 `PARTIAL`：本文若使用 `HIGH / MEDIUM` 一类标签，必须同时声明 scheme 与 rationale，并明确它不是概率，也不是批准。

审查时最值得警惕的是从左列跳到右列：有 citation 就宣称来源支持 Claim；有 provenance 就宣称可信；confidence 高就自动接受；一次接受后就把 Claim 当成永久真值。Evidence Contract 的职责正是让这些跳跃无法悄悄发生。

## 最小 Evidence Record：七组字段，而不是一个 URL

如果暂时不实现完整 Evidence Store，一条记录至少应该让“对象、支持、解释、决定、演化”能够分别复核。下面七组字段是 **COURSE PROPOSAL**，对应 `18-C03`；它们不是某个标准强制的 schema，本文也没有交付或运行 JSON Schema 实现。

| 字段组 | 代表字段 | 审计问题 |
|---|---|---|
| Record identity | `record_id`、`schema_version`、`record_revision` | 正在审查哪一版记录？ |
| Claim identity | `claim_id`、`statement`、`claim_kind`、`scope` | 精确主张和 subject 是什么？ |
| Evidence references | `evidence_refs`、`counter_evidence_refs`；source identity、version/time、locator、可选 digest | 支持与反驳材料在哪里，指向哪份内容？ |
| Interpretation boundary | `observation`、可选 `inference_rule`、`limitations`、`does_not_prove`、`falsifier` | 直接看到什么，解释走了多远，何时会被推翻？ |
| Evidence status | `CONFIRMED | PARTIAL | BLOCKED | PROPOSAL` + rationale | 当前支持强度到哪里？ |
| Acceptance | `acceptance_policy_id/version`、`decision`、`reviewer/verifier`、`decided_at`；可选 confidence scheme | 谁按什么规则决定怎样使用？ |
| Lifecycle | `created_at`、`supersedes`、`invalidated_at/reason`、`review_history_refs` | 新证据到来后，历史怎样保留？ |

in-toto v1.0 的 Statement 与 Resource Descriptor 提供了 subject、predicate type、digest、URI/content 等身份与 locator 先例；SLSA v1.2 展示了 policy-bound verification result；NIST SP 800-53 Rev. 5.1 的 AU-3、AU-8、AU-9、AU-11 又把 audit record content、time、protection 与 retention 作为不同控制关注点。它们支持“身份、时间、来源和历史应显式化”的设计方向，却没有共同规定这七组字段，也没有证明字段一旦存在，内容就真实或能够自动通过验收。

诚实缺省也属于合同。没有 reviewer，就写 `NONE`；没有实际时间，就写 `UNKNOWN`；当前字段不适用，就写 `NOT_APPLICABLE`。用一个看起来真实的 ID、digest 或 decision 填满表格，只会把证据缺口变成伪造数据。

## 为什么“有来源”仍然太粗

`18-C04` 是一条 `PARTIAL` Claim，必须拆成两层来写。

第一层有直接标准先例：

- 缺 source/subject identity，可能证错对象；
- 缺 version/time，可能把旧状态套到新版本；
- 缺 scope，可能把局部 Observation 外推成全局结论；
- 缺 policy context，无法知道 decision 在什么规则下成立。

W3C PROV-DM 的 generation、revision 与 invalidation，in-toto 的 subject/digest，SLSA 的 verifier/policy/time/result，都直接说明这些维度值得分开记录。

第二层是本文的 fail-closed 扩展：

- 缺 `limitations`，已知缺口容易在 handoff 中消失；
- 缺 `falsifier`，Inference 容易变成无法被反驳的故事。

selected standards 并没有统一强制名为 `limitations` 与 `falsifier` 的精确字段。因此，准确说法是：**primary standards 直接支持 identity、version/time、scope 与验收上下文的显式化；limitations 与 falsifier 是本课程为限制越界外推而增加的设计要求。**不能把这五项一起写成统一标准合同。

## Parse / Schema 之后：语义验收链

Article 03 已经说明 Parse 与 Schema 能把候选输出变成可分层拒绝的机器对象。本篇从那里继续，但不重讲 parser、DTO 或 Domain Validation。

> **构造关系图｜COURSE PROPOSAL / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> Claim candidate
>   -> Record
>   -> Parse / Schema                         [Article 03]
>   -> Claim / Subject Identity
>   -> Source Integrity / Resolution
>   -> Provenance
>   -> Version / Time / Scope Applicability
>   -> Support / Refute Mapping               [Article 06 Result/Trace 可作为输入]
>   -> Counter-evidence / Alternatives
>   -> Limitations / Falsifier
>   -> Confidence Scheme
>   -> Acceptance Policy Decision
>              |
>              +-> Article 21：Trace / Replay / Failure Taxonomy
>              +-> Article 22：Eval / Golden Dataset / Regression
> ```

这条顺序对应 `18-C07 = PROPOSAL`。它不是标准规定的统一 pipeline，也没有在 Runtime 中实现。它的教学价值在于为每一层保留一个可停止的问题：

| Gate | 核心问题 | 课程建议的 fail-closed disposition |
|---|---|---|
| Parse / Schema | 记录可读且满足结构合同吗？ | malformed record 直接拒绝，不猜语义 |
| Identity | Claim 与 subject 唯一、明确吗？ | `BLOCKED` |
| Source | 引用可解析，内容可固定吗？ | `PARTIAL` 或 `BLOCKED` |
| Provenance | 来源与必要派生可归因吗？ | `PARTIAL` 或 `BLOCKED` |
| Applicability | version/time/scope 覆盖当前 Claim 吗？ | 收窄 Claim 或保持 `PARTIAL` |
| Support mapping | Observation 真正支持或反驳 Claim 的哪一部分？ | `PARTIAL` 或 `BLOCKED` |
| Alternatives | 冲突和替代解释可见吗？ | 未解时 `Unknown / BLOCKED` |
| Boundaries | limitations、does-not-prove、falsifier 明确吗？ | 不接受绝对或因果措辞 |
| Confidence | 标签是否绑定 scheme 与 rationale？ | 删除，或标为未校准 |
| Acceptance | 固定 policy、inputs 与 decision owner 是否存在？ | 不产生 accepted decision |

这里的 fail closed 不是“所有缺口都报错”。有些缺口要求收窄 statement，有些要求保留 `Unknown`，有些才让 Claim 进入 `BLOCKED`。共同原则是：后置的高 confidence 不能覆盖前置的 identity、scope 或 support 缺口。

Acceptance 也不是永久真值章。一次 decision 应绑定 Claim/subject、Evidence IDs、policy version、reviewer/verifier 与 `decided_at`。Evidence 集、subject revision 或 policy 改变后，需要重新 review；过去的 accepted decision 可以保留为历史，却不能自动继续支配新的 scope。

## 生命周期：不要靠覆盖制造“当前真相”

新证据到来时，最省事的实现是直接改掉旧记录。但这样会让后来的读者看到一个从未真实存在过的“干净历史”：原 Observation、当时的 policy 和曾经作出的 decision 全部消失。

本文为 `18-C08` 提出 append-only revision 模型。它仍是 **COURSE PROPOSAL**；W3C PROV 的 revision/invalidation 与 NIST audit controls 只是关系和保存先例，不规定这套完整状态机，也不表示本文实现了 append-only storage。

> **构造生命周期图｜COURSE PROPOSAL / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> APPEND -> REVIEW -> ACCEPT | REJECT | NEEDS_REVIEW
>    |          \-> review event binds policy/version + Evidence IDs + reviewer + time
>    |-> SUPERSEDE -> points to prior revision + reason
>    |-> INVALIDATE -> target + reason + actor + time
>    \-> CONFLICT -> retain both sides; unresolved => BLOCKED / Unknown
>
> current view = projection of retained records and events
> ```

几个动作必须分账：

- `APPEND` 创建新记录或新 revision，不改写捕获到的原 Observation。
- `SUPERSEDE` 说明新 revision 替代哪一版、为什么替代；旧记录仍可寻址。
- `INVALIDATE` 记录 target、reason、actor、time，不把失效伪装成“从未存在”。
- `REVIEW` 固定 policy version、Evidence IDs、reviewer 与 decision。
- current view 只是这些记录与事件的投影，不是唯一保存的“最终真相”。

## 冲突、过期与部分覆盖：允许结果停在 Unknown

下面的冲突策略对应 `18-C06 = PROPOSAL`，不是组织级统一规则，也没有 Evidence Store 实现。

| 情况 | 课程建议 | 拒绝的捷径 |
|---|---|---|
| Conflict | 双方并存并互相引用；只按显式 scope/version/authority rule 裁决 | 自动 newest-wins，或保留更顺的一边 |
| Stale | 保留历史 identity，追加 superseded/invalidated relation、reason、time、actor | 原地改写旧 Observation |
| Partial | 收窄 statement 与 scope，保持 `PARTIAL` | 把局部支持升级为完整因果结论 |
| New Evidence | 生成新 revision 并触发 review；重跑指定 acceptance policy 后才改变 decision | 看到新材料就自动翻转 accepted 状态 |

“最新”只是时间关系，不是 authority。两份记录可能在不同 build 或版本内分别成立；一份较新的相似记录也可能属于错误 project。若现有规则无法裁决，`Unknown` 是正式工程结果，不是系统失败后留下的空白。

这与 Article 15 的 Memory 边界相容：historical record 可以成为下一次调查的 contributor，却不能冒充 Current Reality。Evidence Contract 继续要求它在当前 Claim 下重新经过 scope、support 与 policy review。

## 具体设计：BuildPilot 交付诊断证据包

> **本节全局分类：COURSE DESIGN / SYNTHETIC SHAPE / NOT IMPLEMENTED / NOT RUN。**
>
> 本文没有 BuildPilot Runtime，没有真实诊断包、Trace、实验、样本、accuracy、cost、latency、production 或 benefit evidence。

如果 BuildPilot 将来要交付一次可复核诊断，它不应该只交付“根因是 X”。一个最小包需要把以下对象关联起来：

1. `case_id`、诊断目标与精确 subject/build scope；
2. Claim set 及各自 statement/scope；
3. source manifest：日志、配置、制品、版本、时间窗、digest/locator；
4. raw Observations 与不可用 inputs；
5. Observation 到 candidate cause 的 inference graph；
6. alternatives、counter-evidence、limitations、falsifiers 与 Unknowns；
7. 每个 Claim 的 Evidence status；
8. acceptance policy/version、decision 与 review history refs；
9. lifecycle/revision relations。

下面故意给出一份“资料仍为空”的设计包。它没有伪造 Observation，恰好展示缺口也能成为结构化数据。

> **构造片段｜COURSE DESIGN / SYNTHETIC SHAPE / NOT IMPLEMENTED / NOT RUN**
>
> ```yaml
> case_id: BP-DIAG-DESIGN-001
> classification: DESIGN_NOT_IMPLEMENTED_NOT_RUN
> diagnostic_target: "a Unity/Jenkins build whose exact identity is still required"
> claims:
>   - claim_id: BP-C01
>     statement: "root cause remains UNKNOWN"
>     status: BLOCKED
> source_manifest:
>   - {source_id: BUILD_LOG, state: REQUIRED_NOT_ACQUIRED}
>   - {source_id: BUILD_CONFIG, state: REQUIRED_NOT_ACQUIRED}
>   - {source_id: ARTIFACT_INVENTORY, state: REQUIRED_NOT_ACQUIRED}
> observations: []
> inference_graph: []
> alternatives: []
> unknowns: [EXACT_BUILD_IDENTITY, FIRST_FAILING_STAGE, ROOT_CAUSE]
> acceptance:
>   policy: buildpilot-evidence-course-v1-design
>   decision: NOT_RUN
> ```

这个 shape 不证明 BuildPilot 已有 Evidence Store，也不证明它能够生成正确诊断。它只是一项课程设计：未来取得真实 inputs 后，系统应追加 Observation、Inference 与 review event，而不是回头把 `REQUIRED_NOT_ACQUIRED` 改写成一份看似从一开始就完整的记录。

### Reviewer 怎样审查这个包

Reviewer 可以按前面的语义 Gate 顺序走一遍：

1. fixed case identity、Claim identity 和 subject/build 是否存在？
2. source 能否解析，版本、时间与 scope 是否覆盖 Claim？
3. Observation 是否与 Inference 分开？
4. alternatives、counter-evidence、limitations 与 falsifier 是否可见？
5. policy/version/reviewer 是否对固定 Evidence inputs 作出 decision？
6. 任一前置缺口命中时，是否诚实保持 `BLOCKED / Unknown / NOT_RUN`？

在当前构造包里，第一个问题就无法通过，因为 exact build identity 仍缺失。正确的审查结果不是继续推演一个 root cause，而是停止在 `BLOCKED / Unknown / NOT_RUN`。这只是对设计对象的 walk-through，不是 BuildPilot execution、Trace replay 或 Eval run。

可审计系统的成熟，不体现在每个 package 都能得到 `PASS`，而体现在它知道什么时候没有资格接受。

## 与相邻系统的边界

Evidence Contract 位于结构合同、运行记录和系统评估之间。L 权重不意味着它可以把相邻主题全部吞掉。

| 主题 | Owner | Article 18 只承接什么 | Article 18 不做什么 |
|---|---|---|---|
| Parse / Schema / machine contract | Article 03 Structured Output | 把 schema-valid record 作为语义 Gate 入口 | 不重讲 parser、DTO、Domain Validation；不把 schema validity 当 truth |
| Tool Result / terminal Trace foundation | Article 06 Tool Runtime | 把 Result 或 Trace ref 当候选 Evidence source | 不把 Result/Trace presence 当 Claim acceptance；不重讲 Runtime pipeline |
| Permission / Approval / Sandbox | Article 19 | 只留下接口：Evidence acceptance 不授予 action authority | 不定义 principal、approval flow、credential scope 或 enforcement |
| Budget | Article 20 | 只说明本篇没有成本/延迟结果 | 不设计 Token、Step、Cost、Latency budget |
| Cross-step Trace / Replay / Failure Taxonomy | Article 21 | 只要求未来 Trace 可被 Evidence Record 引用 | 不设计 correlation、重建/重执行或完整 failure classification |
| Eval / Golden Dataset / Regression | Article 22 | 只说明单个 Claim acceptance 不等于系统质量 | 不设计 dataset、grader、metrics、regression 或 Lab 06 |

OpenTelemetry Specification 1.60.0 的 Trace API 定义 Span、SpanContext、Links、Events 与 Status，适合记录一次操作及其关联执行数据，却不定义外部 Claim 的 support、falsifier 或 acceptance policy。它支持“Trace 可以成为 Evidence source，但 Trace presence 不是 Claim acceptance”这个窄边界；它不证明 Trace 完整、真实、可重放，也不替 Article 21 提前完成 Failure Taxonomy。

NIST AI RMF 1.0 §5.3 MEASURE 把 TEVV 放在有上下文、可记录、可重复的测量过程中。它支持“系统级 evaluation 比接受一个孤立 Claim 更广”这一有限判断，却不提供 Article 22 的 dataset/grader/regression 实现。因此本篇不借一个 Evidence Record 宣称 Agent 质量，也不借一次 accepted decision 宣称回归已经关闭。

下一篇 Article 19 只承接一座桥：即使一个 Claim 已有足够 Evidence，系统仍要回答“谁有资格批准哪种行动、Runtime 怎样实施隔离”。Evidence acceptance 与 action authorization 是两道不同 Gate；本文到此为止，不提前展开 Article 19 的内容。

## 一份“证据化”实现通常怎样写坏

| 反模式 | 错误等式 | 应回到的最小检查 |
|---|---|---|
| 合法 JSON 直接成为事实 | schema-valid = true | Claim/subject、source、support、policy |
| 有链接就算有证据 | citation present = Claim supported | locator、passage、scope、does-not-prove |
| 有来源链就算可信 | provenance present = accepted | source assessment + policy decision |
| 高 confidence 自动批准 | high confidence = accepted/approved | scheme、rationale、independent decision |
| 最新记录覆盖冲突 | latest = authoritative truth | scope/version/authority + conflict event |
| Trace 出现某一步就证明根因 | trace exists = causal proof | Observation/Inference、alternatives、falsifier |
| 一次接受永久有效 | accepted once = forever valid | policy/input/version/time-bound review |
| 设计包写得完整就算实现 | design package = BuildPilot Runtime | implementation、execution 与 runtime evidence |

Evidence Engineering 不是给结论加更多 metadata，而是防止 metadata 被当成真值捷径。字段越多，越需要明确每个字段最多证明什么；否则一份复杂对象只会让未经证明的结论显得更正式。

## 本篇能证明什么，不能证明什么

按 `18-E01`—`18-E08` 的来源与限制，本篇能够安全建立三件事。

第一，fixed primary sources 支持结构 validation、subject/resource identity、provenance relation 与 policy-bound verification decision 可以分层记录。这里的证据包括 JSON Schema Draft 2020-12、W3C PROV-DM 2013 Recommendation、in-toto Attestation v1.0、SLSA v1.2 与 NIST audit-control precedents。

第二，当前课程 canonical 支持 Article 03、06、18、21、22 的 ownership 分账：结构、运行记录、Evidence acceptance、Trace/Replay/Failure Taxonomy 与 Eval 各有自己的责任面。这个分账是课程事实，不是行业统一命名。

第三，课程可以提出一套可审查的 Record、semantic Gate、lifecycle/conflict policy 与 BuildPilot diagnostic package design。它们的状态仍是 Proposal，而不是实现事实。

本篇不能证明：

- 所有组织都应采用统一 Evidence ontology、字段名、Gate 顺序或 confidence scale；
- selected standards 强制本文的完整七组字段、`limitations`、`falsifier` 或 append-only 状态机；
- citation、provenance、Trace 或 accepted decision 自动等于真实、完整、当前适用或 production-approved；
- BuildPilot 已实现、已运行或生成过诊断包；
- BuildPilot 的 accuracy、cost、latency、benefit、production behavior 或可靠性；
- Article 21 的 Replay/Failure Taxonomy 或 Article 22 的 Eval/Regression 已在本文完成。

冻结的现实保持不变：Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。设计可审查，不等于设计已实现；来源可复核，也不等于运行结果已经存在。

## Claim Traceability（10 / 10）

| Claim | 当前上限 | 正文落点 | 不得越过的边界 |
|---|---|---|---|
| `18-C01` | `CONFIRMED` | 开场、问题空间、语义验收链、反模式 | fluency/parse/schema 单独不足；不说自然语言必错 |
| `18-C02` | `PROPOSAL` | 六种陈述角色 | 课程模型，不称统一 ontology |
| `18-C03` | `PROPOSAL` | 七组最小字段 | 课程设计，不称标准 schema 或已实现 |
| `18-C04` | `PARTIAL` | identity/version/time/scope/limitations/falsifier | 前三类边界有直接先例；limitations/falsifier 是课程扩展 |
| `18-C05` | `PARTIAL` | Citation/Provenance/Confidence/Acceptance | confidence scheme 未校准、非概率/批准；四分法不称标准 |
| `18-C06` | `PROPOSAL` | Conflict/Stale/Partial policy | 未实现 Evidence Store，不称组织统一规则 |
| `18-C07` | `PROPOSAL` | Semantic Acceptance Chain | Gate 顺序与 fail-closed disposition 是课程设计 |
| `18-C08` | `PROPOSAL` | Append/Supersede/Invalidate/Review | 不称标准状态机或已实现 append-only infrastructure |
| `18-C09` | `PROPOSAL` | BuildPilot diagnostic evidence package | `DESIGN / NOT IMPLEMENTED / NOT RUN`；无效果或 Runtime Claim |
| `18-C10` | `CONFIRMED` | 相邻系统边界 | 只确认课程 ownership，不提前讲完 Articles 19、21、22 |

Coverage=`10 / 10`；new core Claim=`NONE`；core `BLOCKED=0`。

## Learning Check

1. 一份对象通过 Parse 与 Schema 后，为什么仍不能宣布它的 Claim 成立？接下来至少要检查哪些语义输入？
2. `Observation` 与 `Inference` 的最小差别是什么？为什么 `Unknown` 不是低 confidence 的别名？
3. Citation、Provenance、Confidence、Acceptance 各回答什么？哪两类最容易被误写成批准？
4. 为什么 identity、version/time、scope 有直接标准先例，而 limitations 与 falsifier 必须保留课程扩展语态？
5. 两条 in-scope Evidence 冲突且 authority 无法裁决时，为什么不能 newest-wins？
6. 新 Evidence 到来后，为什么不能直接改写旧 Observation 或自动翻转 decision？
7. BuildPilot package 的 sources 与 observations 仍为空时，为什么 `BLOCKED / Unknown / NOT_RUN` 比一个 root-cause sentence 更正确？
8. Article 06 的 terminal Trace、Article 21 的 Replay/Failure Taxonomy 与 Article 22 的 Eval，为什么不能被 Article 18 吞掉？

### 参考思路

1. Parse/Schema 只确认可读与结构约束；还要检查 Claim/subject、source、provenance、version/time/scope、support/refute mapping、counter-evidence、limitations/falsifier 与 policy decision。
2. Observation 只保存直接读到的有界事实；Inference 要公开输入、规则、替代解释与 falsifier。Unknown 表示当前输入不足或冲突未解，不是一个更含糊的猜测。
3. Citation 找材料，Provenance 追来源链，Confidence 表达某个已声明 scheme 下的确定程度，Acceptance 保存 policy-bound decision。Confidence 与 Provenance 都不能自动变成批准。
4. 标准直接展示 subject/source identity、revision/time、scope/policy context；没有统一强制本文命名的 limitations/falsifier 字段，后两者是课程 fail-closed 扩展。
5. 最新只表达时间，不表达 scope 或 authority；双方可能各自在不同版本成立，也可能形成未解冲突。未解时保留 `Unknown / BLOCKED`。
6. 原地改写会丢失当时 Observation、policy、inputs 与 decision。应追加 revision/event，并重新运行指定 acceptance policy。
7. 因为它忠实保存了确切缺口，没有把缺失输入伪造成运行事实；当前没有 BuildPilot execution 或 runtime evidence。
8. Result/Trace 是候选 Evidence 来源；Replay/Failure Taxonomy 负责跨步骤重建与分类；Eval 负责固定 workload 与系统行为度量。单个 Claim 的接受不能替代三者。

## Job Competency

| 能力 | 可观察产物 | 达标表现 | 明确上限 |
|---|---|---|---|
| Evidence modeling | Minimum Evidence Record 字段组 | 能把 identity、support、interpretation、acceptance、lifecycle 分账 | schema Proposal 不等于 implementation |
| Diagnostic reasoning | Observation -> Inference -> alternatives -> falsifier -> Unknown | 能拒绝把相关性、Trace 或流畅措辞写成因果事实 | 没有真实 BuildPilot diagnosis |
| Reliability design | semantic Gate 与 fail-closed disposition | 能说明缺哪项输入时收窄、BLOCK 或停止 | Gate 顺序是课程 Proposal |
| Audit / governance | policy-bound decision + append/supersede/invalidate/review | 能保留 decision inputs、owner、version 与历史 | 不宣称 compliance 或 production store |
| Cross-system architecture | Article 03 / 06 / 19 / 21 / 22 边界 | 能把 machine contract、runtime record、action authority、trace/replay 与 eval 分层 | 课程 ownership 不等于行业 taxonomy |

## 参考资料

- [JSON Schema Core, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-core)；[JSON Schema Validation, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation)（Validation §3；结构验证边界）
- [W3C PROV-DM Recommendation, 30 April 2013](https://www.w3.org/TR/2013/REC-prov-dm-20130430/)（§§2–3；§5.1.8 Invalidation；§5.2.2 Revision；§5.2.4 Primary Source；§5.4.1 Bundle）
- [in-toto Attestation Framework v1.0 Statement](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/statement.md)；[Resource Descriptor v1.0](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md)（`subject`、`predicateType`、digest/URI/content）
- [SLSA v1.2 Provenance](https://slsa.dev/spec/v1.2/provenance)；[Verifying Artifacts](https://slsa.dev/spec/v1.2/verifying-artifacts)；[Verification Summary Attestation](https://slsa.dev/spec/v1.2/verification_summary)（subject digest、trust/expectations、verifier/policy/time/result）
- [OpenTelemetry Specification 1.60.0](https://opentelemetry.io/docs/specs/otel/)；[Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/)（Span、SpanContext、Links、Events、Status；2026-08-25 访问快照）
- [NIST AI Risk Management Framework 1.0](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10)（NIST AI 100-1，§5.3 MEASURE）
- [NIST SP 800-53 Rev. 5.1 OSCAL-derived PDF](https://csrc.nist.gov/CSRC/media/Projects/risk-management/800-53%20Downloads/800-53r5/SP_800-53_v5_1-derived-OSCAL.pdf)（AU-3、AU-8、AU-9、AU-11）
- TechStackShow Agent Engineering 课程 canonical、Glossary，以及已发布 Articles 03、06、12–17（课程 ownership 与 BuildPilot 设计边界）

## 最短结论

`工程判断真正可交接的形态，不是更确定的结论，而是可定位、可反驳、可复核、可重新验收的 Claim / Evidence / Decision 链。`

下一篇只继续一个问题：即使判断已经有 Evidence，谁可以批准哪种行动，Runtime 又怎样实施隔离。Evidence acceptance 不替代 permission 或 approval。
