# 为什么最终需要 Harness：横切能力由谁承载

> 如果这篇只记一句话：`Harness 不是把 Agent 变大的新盒子，而是当身份、权限、证据、预算、Trace、审批、上下文、恢复、知识和能力发现开始跨多个局部链路漂移时，用来承载共享工程不变量的边界。`

Article 21 讲 Trace、Replay 与 Failure Taxonomy 时，把一件事说得很清楚：有记录，不等于能定位；有 `trace_ref`，不等于能重建同一次执行；有 failure candidate，也不等于已经找到了 root cause。

Article 22 又继续往前推了一步：Trace slice 可以成为 Eval 的候选来源，但不能自动变成 Golden sample；一次修复重新跑绿，也不能自动证明不会回归。Eval 需要 dataset、oracle、scorer、metric、baseline、manifest 和 verdict policy，各自都有自己的合同。

到这里，Reliable Agent 的几块重要拼图已经出现了：Evidence、Permission、Budget、Trace、Replay、Eval、Regression、Human Review。可是一个新的问题也浮出来了：这些控制面如果都存在，究竟由谁把它们连成同一种语义？

在单 Agent、单 Tool、单 Workflow 的原型里，这个问题不明显。我们可以把规则写进 System Prompt，把校验写进 Tool wrapper，把步骤写进 Workflow，把风险写进 review checklist。只要链路短、入口少、人还记得上下文，它们看起来都能工作。

但系统一旦扩展到多个 Agent、多个 Tool、多个 Workflow 和长时间运行，局部写法就会开始松动。一个地方的 `APPROVED` 只表示“用户在聊天里点过同意”，另一个地方的 `APPROVED` 表示“owner 对某个 diff scope 审过且还没 stale”；一个 Workflow 的 `PASS` 表示 HTTP 200，另一个 Workflow 的 `PASS` 表示 artifact digest、运行日志和人工确认都齐了。词一样，合同已经不一样。

本文要回答的不是“有没有某个标准组件叫 Harness”。恰恰相反，当前证据不足以把 Harness 写成行业统一标准名。本课程只是把 Runtime 周围那层可复用的工程控制与约束边界称为 Harness。Article 24 只证明一个窄结论：当一组控制事实必须跨多个 Prompt、Tool、Agent 和 Workflow 保持一致时，它们就不再是局部实现细节，而需要一个共享承载边界。

先把证据上限说清楚。Article 24 没有 Required Lab，Experiment Count=`0`，Runtime Observation=`ABSENT`。本文共有 `12 / 12` Claims 与 `12 / 12` Evidence Cards，状态是 `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。BuildPilot 只作为 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN` 使用；它没有实现、没有运行、没有扫描 Unity 项目、没有修改生产代码，也没有生成真实 BuildReport、PR、设备观测或运行指标。

## 1. 局部链路可以工作，横切治理会散落

先看一个常见的 Agent 原型。

团队希望它像一个“安全的 AI 工程师”：读需求、查代码、看配置、找资源依赖、给修改建议、必要时让人审批。第一次做时，最自然的实现通常是这样：

- System Prompt 写：“只读优先，不要擅自修改生产文件，证据不足就说 unknown。”
- Tool wrapper 写：参数校验、超时、异常捕获、局部日志。
- Workflow 写：先扫描，再分析，再提出建议，再请求 review。
- CI 写：构建、测试和状态检查。
- Review checklist 写：高风险修改需要 owner 看过。
- 团队约定再补一句：“别乱发版，别乱提权，有不确定就问人。”

这套东西并不荒唐。Prompt、Tool、Workflow、CI、Review 和团队约定都是真实有用的局部 surface。问题不在于它们没用，而在于它们各自只自然拥有一部分责任。

| Local surface | 自然拥有 | 系统扩大后开始泄漏 |
|---|---|---|
| Prompt | 任务框定、角色、局部 instruction、示例和偏好 | 不能单独持久化 approval、限制实际 tool surface、统一 evidence acceptance、串起 trace identity 和 owner routing |
| Tool wrapper | schema、validate、execute、local result、局部 error | 不自然决定当前请求是否有权、结果是否可信、预算是否允许、后续知识是否可吸收 |
| Business Workflow | domain sequence、业务状态、确定性步骤和 Agent decision point | 不自然拥有跨 workflow 复用的 permission、approval、evidence、trace、budget、context、recovery |
| CI / Review gate | 构建、检查、owner review、merge gate | 不理解 Agent 内部 step、context、unknown，也不替 Agent 保存 run semantics |
| Team convention | 人的判断、例外处理和非正式经验 | 难审计、难 replay、难判断 approval 是否 stale 或 policy 是否漂移 |

MCP 的文档把 Prompts、Resources、Tools 分成不同 server primitives；OpenAI Agents SDK 和 Microsoft Agent Framework 的文档把 HITL、guardrails、tracing、approval 作为单独机制呈现；GitHub 的 protected branch / CODEOWNERS 把 owner routing、review、stale approval 和 status checks 做成可执行门禁；OpenTelemetry 和 NIST AI RMF 又分别给 observability 与 governance 提供了独立关注面。这些来源足以支持“控制责任确实分散在多个机制里”这个判断。

但它们不证明本课程的 Harness 是行业标准，也不要求所有团队都采用同一架构。本文只借这些来源说明一个更朴素的事实：Prompt、Tool、Workflow 不是同一种东西，approval、trace、permission、evidence、eval 也不是同一种东西。把它们都塞进同一段文本或同一个 wrapper，短期能跑，长期会把合同写糊。

**局部链路能把一次任务跑通，但它们不会自动把跨任务的不变量管成同一种语义。**

## 2. 漂移不只是代码重复，而是同一治理词变成不同合同

重复实现横切逻辑，最浅的一层问题是重复代码。更深的问题是语义漂移。

同样叫 `PASS`，可能代表“Tool 返回了 0”，也可能代表“Evidence Contract 已接受”，还可能代表“Eval 在某个 frozen dataset 上过线”。同样叫 `APPROVED`，可能代表“这次工具调用获准”，也可能代表“某个 owner 对某个变更范围承担责任”。同样叫 `RETRY`，可能只是“再调用一次”，也可能要求 same action intent、effect reconciliation、budget 和 authority 同时成立。

| Drift type | 看起来只是局部代码 | 跨系统后实际坏在哪里 |
|---|---|---|
| Policy drift | 每个 wrapper 都有一点权限判断 | 同一动作在不同入口得到不同 allow / deny |
| Evidence drift | 每条 workflow 自己定义完成条件 | `PASS` 无法审计，不同证据层被混用 |
| Identity drift | 每个系统都有自己的 ID | trace、approval、budget 和 review 无法 join |
| Failure semantics drift | error code 各写各的 | recovery、retry、eval 无法判断是否同一类失败 |
| Review drift | approval 保存在评论或 checklist | diff、需求、证据或 owner 变化后，不知道审批是否过期 |
| Knowledge drift | 经验被塞进 prompt 或文档 | 不知道哪些经验来自证据，哪些只是猜测 |

Azure 的 operations guidance 强调 standardized logging、tracing、metrics，否则日志格式不一致会让有用信息难以检索；Azure microservices guidance 也提醒，把安全和公共任务重复塞进多个服务，会增加维护复杂度并产生重复易错代码。NIST AI RMF 则把 governance 作为跨生命周期的关注面。这些并不是 agent Harness 的统计实验，所以本文把“重复实现会漂移”保持为 `PARTIAL` 工程判断，而不是 `CONFIRMED` 通用定律。

不过这个判断足够指导设计。越是跨 workflow 复用的词，越不能只看字段名。要看它的 owner、scope、lifetime、evidence、stale condition 和 recovery 语义是否一致。

一个典型例子是审批。单次聊天里，用户说“可以改”，这也许足够让当前 Agent 写某个临时文件。但如果后续 diff 变了、owner 变了、需求变了、证据过期了，旧 approval 是否还有效？这个问题不是 Prompt 能自己回答的，也不是某个 Tool wrapper 能独自回答的。它需要一个能保存 approval scope、关联 run / step / change request、检查 stale condition、决定是否暂停和恢复的控制事实。

另一个例子是证据。Tool 返回 HTTP 200，只能证明请求成功返回；BuildReport 只能证明某次构建里的构建摘要、文件、步骤或打包资产等信息；设备观测又是另一层证据。把这些都叫 `PASS`，会让后续 Eval、Review 和 Knowledge Store 不知道自己继承的到底是什么。

**重复实现的真正风险，是同一治理词在不同链路里悄悄变成不同合同。**

## 3. 什么才算横切能力

不是所有重要能力都应该上移到 Harness。一个 Tool 很重要，不代表它是 Harness；一个业务 Workflow 很复杂，也不代表它应该变成共享控制面。关键判断不是“它重不重要”，而是“它是否需要跨多个局部链路保持同一种语义”。

本文采用一个课程级测试。它不是外部标准，也不是后续 Article 26 的完整 Capability 模型，只是 Article 24 用来识别压力的轻量工具：

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

这里的重点是 pressure candidate。它不是“满足三条就立刻做平台”，也不是“每个团队都该抽象一层 Harness”。小型单 Agent、低风险、短生命周期的内部工具，完全可能把规则留在 Prompt、Tool wrapper 或 Workflow 里。Article 27 才会专门讨论成本、Bloat、采纳时机和什么时候不该做 Harness。

但如果一个 concern 同时满足多项信号，它就已经不是局部逻辑了。比如：

- 多个 Agent 都要判断同一类写权限；
- 多个 Tool 都要使用同一套 evidence acceptance；
- 多个 Workflow 都要暂停等待 owner review；
- retry/resume/replay 都依赖同一个 effect status；
- future run 会吸收本次结论作为 knowledge；
- capability 是否可见会随用户、项目、审批状态变化。

这种 concern 如果继续被复制到每个局部链路里，后续最难排查的往往不是“代码在哪”，而是“哪个版本的规则才算数”。

**横切能力不是“重要能力”，而是必须跨多个局部链路保持同一语义的不变量。**

## 4. 本课程为什么把共享边界叫 Harness

本课程使用一个轻量定义：

> Harness 是承载横切控制与记录的共享边界；这些控制与记录必须在多个 Agent、Tool、Workflow 执行时保持一致。

这个定义刻意比后续文章弱。Article 25 才会正式展开 Agent Runtime vs Harness：哪些属于执行内核，哪些属于工程控制面。Article 26 才会把 Capability、Policy、Session、Trace 与 Recovery 等最小能力模型展开。Article 27 才讨论复杂度、Bloat、可替换性、演化和不适用条件。

所以，在 Article 24 里，Harness 不是一个完整 API，不是某个产品，不是一个必须单独部署的服务，也不是所有系统都要照抄的架构。它只是一个命名过的讨论边界：当 identity、permission、context、evidence、budget、trace、approval、recovery、knowledge、capability discovery 这些东西开始跨链路漂移时，我们需要知道“谁承载它们”。

可以先把它看作一张 responsibility pressure map：

| Concern | 为什么局部 owner 不够 | Article 24 的措辞上限 |
|---|---|---|
| Identity | trace、approval、budget、review、knowledge 都需要 join 到同一 run / step / action | pressure map，不是最终 identity model |
| Permission | tool discovery / call 不等于当前请求获权 | 由 MCP tool/security 证据支撑，不展开完整 policy engine |
| Context | prompt 只是 context 的一部分，多来源 packing / receipt 会跨 step | 不定义 Article 25/26 的 context runtime |
| Evidence | evidence acceptance 不能被 HTTP 200 或 tool success 冒充 | 接 Article 18，不重讲 |
| Budget | budget 是 admission / stopping contract，不是 usage report | 接 Article 20，不展开 taxonomy |
| Trace | trace 支持诊断和 replay lineage，不自动接受 evidence 或 eval | 接 Article 21，不重讲 |
| Approval | human review 需要 scope、expiry、stale invalidation 和 resume state | 写成可执行控制，不写成 prompt rule |
| Recovery | retry / resume / rerun / reconcile 要共享 effect knowledge | 接 Article 11/21，不实现 recovery engine |
| Knowledge | 经验进入 future runs 需要来源、作用域和可信度 | 接 Article 15/16/17 |
| Capability discovery | 模型能看见 / 调用什么需要治理，不只是工具列表 | 完整 Capability model 留给 Article 26 |

这个表也解释了为什么“命名”本身有价值。没有一个名字时，这些 concern 会散在 prompt engineering、tool runtime、workflow orchestration、observability、security、review process、knowledge management 里。每个词都对，但没有一个词独自覆盖“共享控制语义的承载边界”。

本课程把这个边界称为 Harness。这个名称不是行业统一标准；它是本课程为了讨论 Runtime 周围可复用工程控制与约束层而采用的工作定义。

**给 Harness 命名，不是为了造一个大词，而是为了让共享不变量有一个可审查的承载边界。**

## 5. 它为什么不是更长的 Prompt、Tool wrapper 或业务 Workflow

最容易误读 Harness 的方式，是把它当成“把规则写得更详细”。比如：

- Prompt 再长一点，把禁止项都写清楚；
- Tool wrapper 再厚一点，把权限和 evidence 都校验掉；
- Workflow 再固定一点，把每个步骤都排好；
- Review checklist 再完整一点，让人最后把关。

这些做法都可能有用，但它们不是同一层能力。

| Candidate owner | 它能做什么 | 它不能独自拥有 |
|---|---|---|
| Longer System Prompt | 描述规则、解释偏好、要求输出格式、提醒未知边界 | enforce authorization、persist approval、join trace identity、verify evidence、route owner、pause / resume |
| Tool wrapper | validate inputs、execute capability、return result / error、record local timeout | globally decide request authority、accept evidence、manage budget、control future capability exposure |
| Business Workflow | arrange domain steps、encode deterministic gates、define local decision points | keep policy / evidence / trace / recovery semantics consistent across unrelated workflows |
| CI / Review platform | enforce repo gate、owner review、status checks | understand every Agent step / context、preserve model / tool / runtime state |

Prompt 的上限很明确：它能要求模型“应该怎么做”，却不能自己保存审批状态、暂停执行、判断某个 diff 是否让审批 stale、把同一 run 的 trace/evidence/budget 关联起来，也不能独自决定 Tool 的实际可见面。

Tool wrapper 的上限也很明确：它能把单次调用做得规范，验证参数、执行动作、返回结果、记录异常。但“工具可调用”不是“当前请求被授权”；“工具返回成功”不是“输出满足 Evidence Contract”；“本 Tool 认为安全”也不是“后续 Workflow、Review、Eval 都承认这个状态”。

业务 Workflow 的上限则更微妙。Workflow 很适合表达业务顺序：先做需求澄清，再做只读检查，再生成建议，再等 owner review。可是一旦多个 Workflow 都需要相同的 permission、approval、budget、trace、evidence 和 recovery 语义，这些东西就会被复制到每条业务链里。Workflow 应该拥有 domain sequence，不应该被迫拥有所有共享控制语义。

这也是为什么 OpenAI / Microsoft 文档里的 HITL、guardrail、approval middleware、tracing 等例子有启发意义：它们说明某些控制必须在某个执行位置生效，而不是只作为一句 prompt 建议存在。但这些例子是产品或框架范围内的证据，不等于本课程要求照搬某个 SDK 的结构。

**Prompt 写得再长，也不能替代需要状态、权限、证据和审批生命周期的可执行控制。**

## 6. Harness 不是 God Object

如果 Harness 承载这么多 concern，另一个误读也很自然：那它是不是要变成一个什么都管的总控中心？

不应该。

Harness 的边界如果没有切清，很快会从 shared control plane 变成业务 God Object：它既解释需求，又决定方案，又改代码，又管工具，又存知识，又跑评估，还替 owner 做发布判断。看起来很强，实际上所有责任都被揉成了一团，最后没有哪个团队成员说得清某个结论是谁批准的、哪个证据支持的、哪个 runtime 状态产生的、哪个业务 owner 承担的。

Article 24 只采用下面这条边界：

```text
Business Agent / Workflow
  owns: domain goal, interpretation, planning,
        suggested change, owner conversation
    |
    v uses / reports through
Harness
  owns: shared identity, permission, evidence labels, budget, trace,
        approval state, recovery policy, knowledge intake,
        capability exposure
    |
    v delegates execution to
Runtime / Tool Runtime / Host surfaces
  owns: model calls, loop execution, tool validation/execution,
        IO adapters and concrete hosting surface
```

这张图不是最终部署图，也不是 Article 25 的完整 Runtime/Harness split。它只是用来守住一个重要判断：Harness 承载共享控制语义，不接管领域意图。

业务 Agent / Workflow 仍然要回答：需求是什么意思，哪些路径可能受影响，怎样组织调查，给 owner 提什么建议。Owner 仍然要回答：是否接受建议，是否实施，是否承担业务风险。Runtime / Tool Runtime / Host 仍然要回答：模型调用、循环推进、工具验证执行、IO adapter 和宿主环境如何运行。

Harness 处在这些东西之间，负责让共同事实不漂移：同一次 run 是谁，哪个 step 请求了什么能力，当前请求有没有权限，证据标签是什么，预算如何约束，审批范围是什么，是否过期，恢复能不能继续，哪些结论能进入知识，哪些能力应该对模型可见。

也就是说，Harness 不应该吞掉 Knowledge Base、RAG、Skill、Tool、Runtime、CI 或业务系统。它可以记录和治理它们之间的控制事实，但不能把它们都改名成自己的一部分。

**Harness 承载不变量，不接管领域意图。**

## 7. BuildPilot：需求变更先变成 Requirement Contract candidate

下面用 BuildPilot 做一个具体设计案例。

> **BUILDPILOT COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST**

假设 Unity 团队收到一个需求变更：

> 这个功能现在要在低内存移动场景中工作，并且不能让包体回退。

一个粗糙的 Agent 可能会立刻开始搜代码、改配置、压资源、调构建参数。看起来主动，风险却很高。因为这句话里还有很多没有固定的条件：低内存指哪类设备？包体回退和哪个 baseline 比？影响哪些场景、资源、配置表和平台？验证信号是什么？谁是 owner？如果性能目标和包体目标冲突，哪个优先？

BuildPilot 在这个设计案例里不直接改 production code、config、art import settings、Addressables groups、`.meta` files、policy 或 capability registry。它的第一步，是生成一份 Requirement Contract candidate，把自然语言需求拆成可审查对象：

```yaml
requirement_contract_candidate:
  target_platforms: REQUIRED_FROM_OWNER
  memory_constraint: REQUIRED_FROM_OWNER
  package_size_baseline: REQUIRED_FROM_OWNER
  affected_features: CANDIDATE
  affected_scenes: CANDIDATE
  affected_assets: CANDIDATE
  affected_config_tables: CANDIDATE
  validation_signals:
    - build_artifact_evidence
    - asset_dependency_evidence
    - owner_review
  unknowns:
    - device_class
    - exact_package_baseline
    - acceptance_threshold
  status:
    - AMBIGUOUS_REQUIREMENT
    - MISSING_PREREQUISITE
```

这不是 owner-approved spec，只是 candidate。它的价值不是替人拍板，而是把缺失条件显式化：哪些句子有歧义，哪些前置条件缺失，哪些约束可能冲突。

这里可以借 requirements traceability、ADR 和 KCS 这些工程实践作为支撑：需求需要清晰条件和约束，决策需要记录背景、选项和后果，知识应该在工作流中被复用和改进。但这些来源没有定义 BuildPilot schema；本文只把它们作为设计案例的工程依据。

**Suggestion-first 的第一步不是给方案，而是把需求缺口暴露出来。**

## 8. 只读证据收集：读到什么，不等于证明什么

Requirement Contract candidate 之后，BuildPilot 才进入只读证据收集。这里的“只读”很关键：它不是先改一版再看，也不是自动修复资源和配置，而是把可能相关的工程面变成可审查 evidence。

| Evidence category | 可以回答什么 | 不能证明什么 |
|---|---|---|
| C# reference scan | 入口、调用、条件分支、受影响模块和潜在 owner | runtime path 一定执行、真实用户会走到、修改一定安全 |
| Cross-table config scan | 配置表引用、ID 关系、缺失行、冲突条件 | 线上数据已同步、策划意图已确认 |
| Asset / import / dependency scan | 资源格式、导入设置、依赖、bundle / group / layout 风险 | device memory、包体 delta 或运行性能已经变化 |
| BuildReport or equivalent build evidence | 构建目标、摘要、文件、步骤、packed assets 等构建事实 | 用户体验、设备性能或线上发布状态 |
| Addressables Analyze or equivalent layout check | duplicate dependencies、explicit / implicit assets、layout risk | 自动修复被授权、所有项目都足够覆盖 |

Unity 的 BuildReport、AssetDatabase 和 Addressables Analyze 文档支持这些 read-only evidence surfaces 的可行性：Unity 确实提供了构建摘要、文件、步骤、资源导入表示、依赖关系和 Addressables layout 分析等读取面。但这只支持“可以作为证据类别设计”的判断，不证明 BuildPilot adapter 已存在，也不证明本文执行过任何 Unity Editor、构建、项目扫描或设备测试。

证据收集之后，Finding 也必须保留状态，而不是把所有东西都写成确定结论：

```text
CONFIRMED
VIOLATION
INSUFFICIENT_EVIDENCE
PERMISSION_BLOCKED
TOOL_GAP
INTENT_DRIFT
```

其中 `INTENT_DRIFT` 尤其要小心。只有证据支持“需求意图和当前实现意图出现偏离”时，才可以这样写。仅从代码或配置推断出来的原因，应该保留为 `CANDIDATE_INTENT`。这点很像前几篇反复强调的证据边界：看到了一个现象，不等于可以补完背后的动机。

**只读证据的价值在于缩小判断范围，不在于替 owner 执行修改。**

## 9. Change Request：Human Review 是 gate，不是全部治理语义

只读证据足够后，BuildPilot 在这个设计案例里输出的不是 patch，而是 Evidence-backed Change Request。

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

注意这条链里有两个很容易被写错的边界。

第一，Human Review 是 gate，不是全部治理语义。人可以决定是否接受建议，是否要求更多证据，是否由某个 owner 实施。但系统仍然要保存 approval scope、decision state、expiry / stale condition、trace linkage 和 re-verification requirement。否则，review comment 只是一段文本；一旦 diff、需求、证据或 owner 变化，系统就不知道旧审批还能不能用。

第二，Change Request 不是 implementation。它最多应该包含这些东西：

| Change Request field | 作用 |
|---|---|
| requirement contract ref | 把建议绑定回需求候选 |
| affected files / assets / config refs | 说明影响面来自哪里 |
| evidence refs + status labels | 说明哪些是 CONFIRMED，哪些仍是 INSUFFICIENT |
| proposed change intent | 说明建议方向，而不是直接提交 patch |
| owner routing candidate | 指向应该 review 的人或职责面 |
| unknowns / blocked evidence | 保留不能判断的地方 |
| stale conditions | diff、需求、证据、owner 或 capability 变化时需要重审 |
| re-verification plan | owner 实施后怎样再查 |
| knowledge / rule / test / gate candidate | 重复模式如何进入后续治理 |

GitHub protected branch 和 CODEOWNERS 是很好的类比：真实工程平台会区分 owner routing、required review、stale approval、status checks 和 conversation resolution。但这里仍然只是类比，不是 BuildPilot 已经集成 GitHub，也不是说所有 Harness 都应该照搬 GitHub 的语义。

当 owner 在 BuildPilot 外实施真实修改后，BuildPilot 可以进入 re-verification candidate：重新检查声明过的证据面，确认之前的 finding 是否被关闭，哪些 unknown 仍然保留，是否产生新的 Tool Gap 或 Capability Evolution candidate。若同类问题反复出现，系统可以提出 Rule / Test / Gate candidate。

这里仍然不能静默提权。发现 `TOOL_GAP` 不等于安装更多工具；发现需要新能力，也不等于修改 Capability Registry。正确说法是 `Governed Capability Evolution`：提出能力缺口、说明最小权限、交给 owner 审核，再决定是否扩展。

**Human Review 是关键门禁，但门禁本身仍需要身份、范围、过期和复验语义。**

## 10. 这个 BuildPilot 案例到底证明什么

这个设计案例能证明的东西很窄，但很有用。

它说明，一个 suggestion-first assistant 仍然需要 shared governance。就算它不直接写生产代码，系统也必须一致回答：

- 谁提出了需求；
- 当前需求合同有没有缺口；
- 读过哪些代码、配置、资源或构建证据；
- 每条 evidence 的状态是什么；
- 哪些 finding 是 confirmed，哪些只是 candidate；
- 谁有权审批；
- 审批绑定什么 scope；
- 什么时候 approval 会 stale；
- owner 实施后怎样 re-verify；
- 哪些未知不能被写成结论；
- 哪些经验可以进入未来知识；
- 哪些能力缺口只能作为受治理演进候选。

这些问题跨越了 Prompt、Tool、Workflow、Review、Knowledge 和 Capability discovery。把它们塞进任意一个局部点，都会让另一些点失去同一语义。

同时，这个案例不能证明下面这些事：

- BuildPilot 已经存在、已运行、已修改 Unity 项目或创建 PR；
- BuildPilot 已经调用 Unity、Jenkins、CI、Addressables Analyze 或设备测试；
- Requirement Contract、Intent Ledger、Knowledge Store 或 Rule/Test/Gate candidate 已有稳定 schema；
- `INTENT_DRIFT` 已在真实项目中确认；
- Unity read-only evidence 一定足以覆盖所有项目；
- suggestion-first、Human Review 或 Harness 能保证安全、合规、无回归或永不复发。

因此，本文所有 BuildPilot 关键描述都必须读作：

```text
COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN
READ-ONLY / SUGGESTION-FIRST
```

它只说明一条受治理建议链为什么需要共享承载边界，不说明 BuildPilot 已经具备这些能力。

**设计案例的价值，是把 Harness 压力落到可审计链路，而不是伪造实现事实。**

## 11. 一套 Harness 设计通常怎样写坏

引入 Harness 之后，最危险的不是“做得不够大”，而是边界写错。

| Shortcut | 被吞掉的责任 | 最小修正 |
|---|---|---|
| `longer prompt = governance` | executable state、approval lifecycle、evidence acceptance | Prompt 描述行为；Harness 保存和执行控制事实 |
| `tool registered = safe to use` | permission、trust、budget、audit | 分开 discovery、authorization、approval、execution、evidence |
| `workflow has steps = shared control solved` | cross-workflow identity and policy semantics | Workflow 管业务顺序；共享控制保持公共含义 |
| `trace exists = evidence accepted` | claim / evidence acceptance | Trace 提供 lineage；Evidence Contract 决定 acceptance |
| `eval pass = release safe` | production risk owner and monitoring | Eval 是有范围的 release input，不是生产真值 |
| `human approved = always valid` | scope、expiry、stale invalidation | approval 绑定 diff、需求、证据、版本并随变化 revalidate |
| `knowledge captured = true forever` | source scope、staleness、confidence | 保存 provenance、applicability、confidence 和 retirement condition |
| `capability gap = install more tools` | permission and capability governance | 提出 Governed Capability Evolution，不静默扩权 |
| `Harness owns everything` | domain intent and business ownership | 业务 planning、owner decision 和 implementation 留在业务链路 |
| `Harness is an industry standard` | course terminology boundary | 明说“本课程把这个边界称为 Harness” |

这些坏法的共同点，是让一种机制替另一种责任面作决定。Prompt 替审批作决定，Tool 替权限作决定，Trace 替 Evidence 作决定，Eval 替发布风险 owner 作决定，Human Review 替全部治理状态作决定，Knowledge Store 替事实有效期作决定。

好的 Harness 设计反而更克制。它要让共享控制一致、可审计、可恢复、可替换；同时要让业务意图、领域 owner、具体 Runtime、Tool Runtime、Host 和 Knowledge Base 继续保持自己的边界。

**Harness 的成熟不是管得更多，而是让共享控制一致、可审计、可替换，同时不吞业务。**

## 12. 本篇能建立什么，不能证明什么

本文可以安全建立的上限是：

- 公开 agent / protocol / workflow 系统会把 instruction、resources、tools、processes、guardrails、tracing、approvals 等拆成不同 primitives 或 control mechanisms；这支持“责任分层存在”。
- Tool discovery、schema 或 invocation 不等于 permission、trust 或 evidence acceptance。
- Approval、guardrail、trace、budget、evidence、eval 等需要状态、placement、refs 或独立合同，不能全由 prompt 文本承担。
- 把横切治理逻辑复制到每个 Agent / Workflow 内，会带来 policy、failure semantics、auditability、recovery 漂移压力；这是架构类比支持的 `PARTIAL` 判断。
- 本课程可以把 Harness 作为 shared carrying boundary / shared control plane 引入，同时保留 non-God-Object 边界。
- BuildPilot Unity requirement-change scenario 可以作为 read-only、suggestion-first design case，用来展示一条横切治理链。
- Article 24 Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`，BuildPilot=`NOT IMPLEMENTED / NOT RUN`。

本文不能证明：

- Harness 是行业统一组件，或者所有团队都应该采用。
- 本篇的初步职责集是完整、最小、唯一或已被实现验证的 Capability model。
- BuildPilot 已拥有 runtime、UI、schema、registry、trace store、knowledge store、Unity adapter 或 review integration。
- 任何 Unity 项目已经被扫描、修改、构建、发布或 device-tested。
- suggestion-first、Human Review 或 Harness 可以保证安全、合规、无回归、无 bloat 或永久有效。

这条边界也决定了接下来三篇各自要回答什么。

Article 24 只证明“为什么需要共享边界”。Article 25 才回答 Agent Runtime 执行什么，Harness 治理什么，Host 又在什么位置承载它们。Article 26 再把 Capability、Policy、Session、Trace 与 Recovery 做成最小能力模型。Article 27 最后讨论复杂度、Bloat、可替换性、演化和什么时候不该引入。

## Claim Traceability（12 / 12）

| Claim | Evidence ceiling | 正文落点 | 保留边界 |
|---|---|---|---|
| `24-C01 / 24-E01` | `CONFIRMED` | 开场、1、4、12 | public systems separate primitives；course Harness is not industry standard |
| `24-C02 / 24-E02` | `CONFIRMED` | 1、5、11、12 | tool discovery / call 不等于 permission / trust / evidence acceptance |
| `24-C03 / 24-E03` | `PARTIAL` | 1、2、5、9 | approval / guardrails need executable state / placement；examples are SDK-scoped |
| `24-C04 / 24-E04` | `PARTIAL` | 开场、3、4、12 | Trace、Evidence、Budget、Eval 相关但独立 |
| `24-C05 / 24-E05` | `PARTIAL` | 1、2、5、11 | drift pressure by architecture analogy；no agent-specific statistics |
| `24-C06 / 24-E06` | `PROPOSAL` | 4、6、11、12 | Harness as shared control plane, not God Object |
| `24-C07 / 24-E07` | `PROPOSAL` | 开场、3、4、6、12 | initial responsibility set is pressure map；Article 25/26 deferred |
| `24-C08 / 24-E08` | `PARTIAL` | 2、9、11 | owner routing / review gates / stale approval are governance examples |
| `24-C09 / 24-E09` | `PARTIAL` | 7、9、12 | requirement / intent / knowledge chain is synthesis / proposal grounded in practices |
| `24-C10 / 24-E10` | `PARTIAL` | 8、10、12 | Unity read-only evidence surfaces exist；no adapter / run / project evidence |
| `24-C11 / 24-E11` | `PROPOSAL` | 7、8、9、10、12 | BuildPilot scenario is design case only |
| `24-C12 / 24-E12` | `CONFIRMED` | 开场、10、12 | Required Lab NONE；Experiment 0；Runtime ABSENT；BuildPilot not implemented / run |

Coverage=`12 / 12`；Evidence Cards=`12 / 12`；状态保持 `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。

## Learning Check

1. 为什么 Article 18—22 已经有 Evidence、Permission、Budget、Trace、Eval，还仍然需要 Article 24？
2. 横切能力和“重要能力”有什么区别？
3. 为什么更长的 System Prompt 不能替代 Harness？
4. Tool discovery 为什么不等于 permission 或 evidence acceptance？
5. Workflow 已有确定步骤，为什么仍不能拥有所有横切治理？
6. Harness 为什么不是 God Object？
7. Article 24 的 Harness 职责集为什么只能叫 pressure map？
8. BuildPilot 需求变更案例的第一步为什么是 Requirement Contract candidate？
9. Unity read-only evidence surfaces 能支持什么，不能支持什么？
10. Human Review 为什么不是全部治理语义？
11. `INTENT_DRIFT` 和 `CANDIDATE_INTENT` 怎样区分？
12. 如果 BuildPilot 输出了 Change Request，文章能否说修复完成？
13. Article 25、26、27 分别接什么，Article 24 不能提前做什么？

### 参考思路

1. Part IV 建立的是多个控制合同；Article 24 讨论这些合同跨多个 Agent、Tool、Workflow 复用时，身份、权限、证据、预算、Trace、审批、恢复和知识语义由谁保持一致。
2. 重要能力可能只属于一个局部链路；横切能力要求多条局部链路共享同一规则、状态、审计和恢复语义。
3. Prompt 能描述规则，不能单独 enforce authorization、persist approval、join trace identity、accept evidence、route owner 或 pause / resume。
4. schema / list / call 只说明能力可发现或可请求；当前用户、请求、输出可信度、预算、审批和证据接受仍需独立治理。
5. Workflow 表达 domain sequence；跨 workflow 的 policy、trace、budget、evidence、approval 和 recovery 语义会漂移。
6. Harness 承载共享控制不变量；业务 Agent / Workflow 保留 domain goal、需求解释、owner conversation 和真实实施。
7. 它由课程前文和公开机制综合而来，没有外部标准或实现验证；完整 Runtime/Harness split 与 Capability model 留给 25/26。
8. 先暴露平台、约束、影响范围、验证信号、unknown / ambiguous / contradictory 条件；owner 尚未批准前不能当最终 spec。
9. 它们支持代码引用、配置关系、资源/import/dependency、BuildReport 或 Addressables layout 类证据类别；不证明 BuildPilot adapter 已存在、项目已扫描、runtime/device 指标已验证。
10. Review 是 gate；仍需保存 approval scope、expiry / stale、owner routing、trace/evidence refs、re-verification 和 knowledge intake。
11. `INTENT_DRIFT` 必须有证据支持需求/实现意图偏离；仅从代码或配置推断时只能保留 `CANDIDATE_INTENT`。
12. 不能。Change Request 是建议与治理记录；owner 尚未实施，re-verification 未运行，也没有 Unity/CI/device/runtime evidence。
13. 25 接 Runtime vs Harness，26 接 Capability minimum model，27 接 design tradeoff / bloat / adoption；24 只证明必要性。

## 参考资料

- [MCP Specification 2025-06-18: Server Features](https://modelcontextprotocol.io/specification/2025-06-18/server/index) 与 [Tools](https://modelcontextprotocol.io/specification/2025-06-18/server/tools)：用于说明 Prompts、Resources、Tools 与 tool security / confirmation / validation 等责任分层；不证明本课程 Harness 是行业标准。
- [MCP Authorization](https://modelcontextprotocol.io/specification/draft/basic/authorization)：用于说明 authorization 是独立 concern；不把它写成完整 Harness。
- [OpenAI Agents SDK Human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/)、[Guardrails](https://openai.github.io/openai-agents-js/guides/guardrails/) 与 [Tracing](https://openai.github.io/openai-agents-js/guides/tracing/)：用于说明 approval、guardrail placement 和 tracing 需要执行位置与状态；不要求采用本文架构。
- [Microsoft Agent Framework Tool Approval](https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval) 与 [Semantic Kernel Process Framework](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/process/process-framework)：用于说明 approval middleware 和 business process / workflow 可以分层；不把产品名或示例名等同于课程 Harness。
- [GitHub Protected Branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches) 与 [CODEOWNERS](https://docs.github.com/en/enterprise-server@3.20/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)：用于 owner routing、review gate、stale review 和 status check 的工程类比；不证明 BuildPilot 集成 GitHub。
- [OpenTelemetry Specification 1.60.0](https://opentelemetry.io/docs/specs/otel/) 与 [NIST AI RMF 1.0 Core](https://airc.nist.gov/airmf-resources/airmf/5-sec-core/)：用于支持 observability / governance / measurement 是独立关注面；不决定 evidence acceptance 或 eval verdict。
- [Azure Design for Operations](https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/design-for-operations) 与 [Azure Microservices architecture guidance](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices)：用于重复公共逻辑、日志/治理不一致和 shared concern 的架构类比；不提供 agent-specific 统计结论。
- [Unity BuildReport 2022.3](https://docs.unity.cn/2022.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html)、[Unity Asset Database](https://docs.unity.cn/Manual/AssetDatabase.html)、[Addressables Analyze window](https://docs.unity.cn/Packages/com.unity.addressables%402.9/manual/analyze-addressables-window-reference.html)：用于 BuildPilot design case 中的 read-only evidence category；不证明本文运行过 Unity 或存在 BuildPilot adapter。
- [ISO/IEC/IEEE 29148 public OBP page](https://www.iso.org/obp/ui?_escaped_fragment_=iso:std:iso-iec-ieee:29148:ed-2:v1:en)、[MADR ADR template](https://github.com/adr/madr/blob/develop/template/adr-template.md)、[KCS Practices Guide](https://library.serviceinnovation.org/KCS/Knowledge-Centered_Success_Practices_Guide)：用于 Requirement Contract、Intent Ledger 和 Knowledge Store 的设计依据；不定义 BuildPilot schema。
- [上一篇：Eval、Golden Dataset 与 Regression：修复以后还会不会再坏]({{< relref "ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md" >}})：Article 22 只在本文中作为 Part IV 到 Part V 的边界衔接。

## 最短结论

`Harness 不是更聪明的 Prompt，也不是更大的业务 Agent；它是当横切控制必须跨多条执行链保持同一语义时，团队为这些不变量设置的共享承载边界。`

知道 Harness 为什么出现，只是进入 Part V 的第一步；真正的工程问题，是下一篇要把执行内核和治理控制面切清楚。
