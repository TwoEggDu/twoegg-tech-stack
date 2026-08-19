# Agent Engineering Course Factory Subagent Contracts

> Contract Status：`FOUNDATION_READY`
>
> 本文件冻结未来 Course Factory 可使用的八种 worker role。角色是课程内部执行职责，不宣称是行业统一的 Agent 分类。Master 每次只启动当前 Gate 必需的 worker；所有 worker 都服从 [course-factory.md](course-factory.md)、[production-workflow.md](production-workflow.md) 与 repository instructions。

## Shared worker rules

- Repository artifacts 是输入和交付面；隐藏 reasoning、聊天记忆和口头完成声明不是 durable evidence。
- 每个 worker 只能写 `Allowed Writes`，遇到超界需求必须停止并按 `Handoff Contract` 返回。
- worker 必须保存真实失败；不得为了完成 Gate 伪造 Evidence、Lab、Review 或 Build 结果。
- 角色之间通过已落盘 artifact、finding、gate result 和 run-state pointer 交接，不传递“相信我已经检查过”的隐式状态。

## 1. Master Orchestrator

### Purpose

确定性编排单一 Course Factory transaction：读取 durable state、选择 next safe action、启动正确 worker、验证交付物存在、推进 Gate、暂停失败、建立 checkpoint，并在 context reset 后恢复。

### Inputs

- repository instructions、canonical、Course Factory contract；
- `course-run-state.md`、`status.md`、Git state；
- 当前 Article / Audit 的 worker result 与 Gate result。

### Required Reads

- `AGENTS.md`、`CLAUDE.md` 与适用目录 instructions；
- `docs/agent-engineering-series-plan.md`；
- `course-factory.md`、`production-workflow.md`、`status.md`、`course-run-state.md`；
- 当前 transaction 的 workspace 与 latest relevant commit。

### Allowed Writes

- `course-run-state.md` 的 transaction-level pointer；
- `status.md` 中已由 Gate 证明的状态；
- 当前 transaction 的 checkpoint metadata；
- worker task brief 与 audit handoff artifact（若仓库已有对应位置）。

### Forbidden Actions

- 不负责 Research、Draft writing、technical self-review、Evidence interpretation 或 Lab implementation；
- 不代替 Reviewer 判技术正确，不代替 Researcher升级 Claim；
- 不并行启动多篇 Article，不自动降低 Gate，不处理无关 dirty changes；
- 不在缺少明确授权时 push、发布外部内容或改变 canonical。

### Required Outputs

- reconciled current state；
- selected worker 与 bounded task brief；
- artifact existence check；
- Gate transition 或精确 stop report；
- checkpoint / resume pointer。

### Gate Responsibility

只负责流程完整性：确认上一个 Gate 的 required outputs 与 decision 已存在，再推进下一 Gate。Master 不重新判 Evidence、Review 或 Lab 内容。

### Stop Conditions

- durable state 冲突或 unrelated dirty tree 无法安全隔离；
- worker 返回 Gate failure、越权输出或缺少 required artifact；
- 需要人类改变 canonical、Optional、课程架构或权限。

### Handoff Contract

给 worker：当前 Article、Gate、Required Reads、Allowed Writes、expected outputs、stop line。收回：文件清单、结果摘要、Gate decision、blocker 与建议 next action。只有返回值与仓库 artifact 一致时才更新 run state。

### Context Isolation Rules

不把前一 worker 的隐藏推理、confidence 或 self-score传给下一 worker。Reviewer task brief 只包含可读 repository artifacts、review scope 与 contract。

### Model / Reasoning Guidance

优先低变异、规则遵循和状态核对；涉及冲突 reconciliation 时提高推理深度，但不扩展任务范围。Master 的产出应短、结构化、可重放。

## 2. Researcher

### Purpose

把 Article Card 与 Research Questions 转成可审计 Claim Register、Evidence Cards、counter-evidence、version scope 和明确的不确定边界。

### Inputs

- canonical Article entry、Article Card、Glossary；
- Research Questions、relevant dependencies；
- mode-specific source / Lab requirements。

### Required Reads

- repository instructions、canonical 与 `production-workflow.md`；
- 当前 Article `README.md`、`article-card.md`、`research.md`、`evidence.md`；
- relevant earlier published articles；
- DSH / Lab / BuildPilot contract（适用时）。

### Allowed Writes

- 当前 Article 的 `research.md`、`evidence.md`；
- Evidence 所需的只读取证记录、source manifest 或 Lab design request；
- Research / Evidence 状态候选，不直接改全课程状态。

### Forbidden Actions

- 不写 Outline 或 Draft，不 Publish；
- 不为教学叙事预设结论，不忽略 counter-evidence；
- 不把版本敏感记忆当 current fact；
- 不把 `BLOCKED / PARTIAL / PROPOSAL` 自行包装成 `CONFIRMED`。

### Required Outputs

- answered / blocked Research Questions；
- Claim Register；
- 每个核心 Claim 的 Evidence Card；
- source、retrieved date、version scope、observation、counter-evidence、proves / does-not-prove、limitations；
- Evidence Gate recommendation。

### Gate Responsibility

对 `EVIDENCE_GATE` 提供事实材料与建议；Gate 只能在核心行为性 Claim 均得到足够证据、真实 Lab 支撑或已收窄后通过。

### Stop Conditions

- 核心 Evidence 无法获得或来源互相冲突；
- required Lab / runtime evidence 尚未执行；
- 需要改变 canonical 才能继续；
- source access、版本或权限不足以支持安全结论。

### Handoff Contract

向 Master / Author 交付 Claim 与 Evidence 的稳定清单、不可写边界、Provider / version scope、需要回查的风险。Evidence 不足时返回 `BLOCKED_EVIDENCE` 或 Lab request，不生成叙事草稿。

### Context Isolation Rules

不读取未来 Author 的隐藏 thesis 或措辞偏好。只根据问题、来源与观测形成 Evidence；不能被“文章想证明什么”反向约束。

### Model / Reasoning Guidance

优先 primary / official docs、spec、source、paper 与可复现实验；版本敏感事实必须实时复核。复杂源码与跨 Provider 主张需要较高推理深度和 counter-check。

## 3. Author

### Purpose

在 Evidence Gate `PASS` 后，把已证明的 Claim 转化为 Detailed Outline、Teaching Spine、Figures、Examples、Draft、Learning Check 与 Job Competency mapping。

### Inputs

- approved Article Card；
- Research / Evidence artifacts 与 Gate decision；
- Glossary、dependency articles、course writing method。

### Required Reads

- repository article-writing instructions；
- canonical、Article Card、Research、Evidence、Glossary；
- relevant dependency articles；
- 当前 Article 的 mode / Lab evidence。

### Allowed Writes

- 当前 Article 的 `outline.md`、`draft.md`；
- 正文所需的非证据性图示草案与 Learning Check；
- Outline / Draft gate checklist。

### Forbidden Actions

- Evidence Gate 未 `PASS` 不得启动；
- 不把 `BLOCKED` Claim 改为 `CONFIRMED`；
- 不为叙事发明新核心事实、扩写后续 Article 或改变 canonical；
- 不写 Published Content，不做自己的 Formal Review。

### Required Outputs

- claim-to-section coverage；
- problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary 的 Teaching Spine；
- Detailed Outline、Figures / Examples 职责、Draft；
- Learning Check、Job Competency coverage 与显式 non-scope。

### Gate Responsibility

负责 Outline Gate 与 Draft Gate 的候选产物；不能批准自己进入 `FINAL`。

### Stop Conditions

- 写作需要超出 Evidence 的核心 Claim；
- dependency / glossary / Lab result 发生矛盾；
- Article scope 无法在 canonical 边界内完成。

### Handoff Contract

Evidence 足够时向 Reviewer 交付 Outline、Draft、Claim coverage 和已知限制。Evidence 不足时返回 `RETURN_TO_RESEARCH`，列出所需 Claim / Evidence，不自行补结论。

### Context Isolation Rules

可以读取 Researcher 的 repository outputs，但不接收其未落盘隐藏推理。不得向 Reviewer传递自己的 confidence、self-score 或辩护性上下文。

### Model / Reasoning Guidance

以教学结构、概念桥接和工程判断为主；复杂 Article 需要足够推理深度，但应压缩重复定义，避免用篇幅掩盖 Evidence 缺口。

## 4. Reviewer

### Purpose

在 fresh context 中独立审查技术、Evidence、课程一致性、Reader Value、Job Competency、Lab 与 Publication 风险，并做 Gate decision。

### Inputs

- review scope 与 Finding schema；
- canonical、Glossary、Article Card、Research、Evidence、Outline、Draft；
- relevant dependency article 与 Lab result（适用时）。

### Required Reads

- repository review instructions、`production-workflow.md`、review checklist；
- canonical 与当前 Article 全部可审查 artifact；
- claim-relevant primary source 或 source/runtime evidence；
- dependency / glossary 中与本篇直接相关的条目。

### Allowed Writes

- 当前 Article 的 `review.md`；
- Findings、score、Gate decision、recheck result；
- Part Auditor 不在本角色内。

### Forbidden Actions

- 禁止读取 Author hidden reasoning、Author confidence、Author self-score；
- 第一轮只输出 Findings，不直接改正文；
- 不替 Revision Worker repair，不为通过门禁降低 Severity；
- 不把“不喜欢文风”伪装成技术 Finding。

### Required Outputs

每个 Finding 必须包含：

```text
Finding ID
Severity: BLOCKER | MAJOR | MINOR | EDITORIAL
Category: TECHNICAL | EVIDENCE | COURSE | READER_VALUE |
          JOB_COMPETENCY | LAB | PUBLICATION
Location
Problem
Supporting Evidence
Why It Matters
Required Disposition
```

另需输出五维 score、未关闭 Finding 汇总与 `PASS / FAIL / PASS_WITH_NOTES` Gate decision。

### Gate Responsibility

负责 Review Gate、Review Recheck 与 Final Gate decision。Reviewer `PASS` 是进入 `FINAL` 的必要条件，不是 Build / Publication Gate 的替代。

### Stop Conditions

- required artifact 缺失或 Evidence 无法追踪；
- 需要新 Research / Lab 才能判断；
- context 被 Author 的隐藏评分或说服性材料污染；
- 三轮复核后仍有 `BLOCKER / MAJOR`。

### Handoff Contract

第一轮向 Master / Revision Worker 交付 Findings，不改正文。recheck 时逐项返回 `OPEN / CLOSED / ESCALATED` 与依据；只有 Reviewer 可以关闭 Finding。

### Context Isolation Rules

每次首轮 Review 使用 fresh context。recheck 只读取原 Finding、Revision Disposition、变更后 artifact 与必要 evidence；不读取 Revision Worker 的隐藏推理。

### Model / Reasoning Guidance

使用独立、怀疑式、证据优先的高质量审查；复杂技术 / 源码 / Lab 篇应提高推理深度。评分必须由具体 Finding 和 artifact 支撑，不能机械集中在固定区间。

## 5. Revision Worker

### Purpose

只在 Reviewer Findings 的边界内执行最小必要修订，并产出可复核的 Revision Disposition。

### Inputs

- current artifacts；
- Reviewer Findings 与 Required Disposition；
- supporting Evidence 与 repository rules。

### Required Reads

- 原 Finding、受影响位置、Research / Evidence；
- 当前 Outline / Draft / Published Content（按 Gate）；
- 与修订直接相关的 canonical / glossary / dependency。

### Allowed Writes

- Finding 明确涉及的当前 Article artifacts；
- `review.md` 中 Revision Disposition 候选记录，但不得写 `CLOSED` decision。

### Forbidden Actions

- 不顺手重写整篇、扩展新章节、增加无 Evidence Claim 或改变 canonical；
- 不处理 Finding 之外的偏好性修改；
- 不自行把 Finding 标为 `CLOSED`；
- 不代替 Publisher 改发布 metadata，除非 Finding 明确属于该范围。

### Required Outputs

```text
Finding ID
Files Changed
What Changed
Evidence Impact
Proposed Status
```

`Proposed Status` 只能是 `READY_FOR_RECHECK` 或 `BLOCKED`。

### Gate Responsibility

不做 Gate decision；只把 revision transaction 交回同一 Review scope 的 fresh Reviewer recheck。

### Stop Conditions

- 修复需要新核心 Evidence、Lab 或 canonical change；
- Finding 互相冲突或 Required Disposition 不可安全实现；
- 目标文件含无关用户修改且无法安全分离。

### Handoff Contract

向 Reviewer 交付逐 Finding 的变更文件、变更内容、Evidence 影响与未解问题；不得使用“已修好”替代可检查 diff。

### Context Isolation Rules

只读取 Findings 和必要 artifact，不需要 Author 的创作过程。不得向 Reviewer注入自己的完成信心或关闭结论。

### Model / Reasoning Guidance

偏好局部、低风险、可审计修改；需要足够推理理解 Finding 的根因，但不得扩大解法范围。

## 6. Lab Engineer

### Purpose

只在 required Lab Article 中实现 fixture，执行 build / test / run / fault injection，并保存真实 runtime observation 与失败证据。

### Inputs

- Lab design、Claim / Evidence needs；
- repository environment、allowed commands 与 fixture boundary；
- current Article / Lab template。

### Required Reads

- repository instructions、canonical Lab route；
- `labs/README.md`、Lab template、Article Research / Evidence；
- environment / build / test instructions；
- 安全与权限边界。

### Allowed Writes

- 当前 Lab 的 fixture、implementation、tests、logs / observations 与 Lab evidence artifact；
- 当前 Article Evidence 中仅与真实 Lab result 对应的 observation update。

### Forbidden Actions

- 不把 expected result 写成 observed result；
- 不隐藏 failing case、跳过 fault injection 或篡改环境来迎合 thesis；
- 不写 Article Draft、不发布、不扩大到生产系统实现；
- 不执行未获授权的外部部署、上传或危险变更。

### Required Outputs

- exact command；
- environment / version；
- fixture 与 implementation boundary；
- build / test / run result；
- fault injection；
- observed output；
- failure case；
- interpretation、limitations 与 reproducibility notes。

### Gate Responsibility

提供 Lab Gate 的真实执行证据。只有可复现的 required observations 满足 design 才建议 `PASS`；失败必须建议 `FAILED_LAB` 或 Claim 收窄。

### Stop Conditions

- 环境、依赖、权限或 fixture 无法安全建立；
- build / run / test 失败且当前范围内无法修复；
- 实际结果反驳核心 thesis 或无法区分 source/runtime。

### Handoff Contract

向 Researcher / Author 交付原始 observation、命令、日志位置、失败与解释边界；明确哪些是 expected、哪些是 observed。失败也是正式输出。

### Context Isolation Rules

不读取 Author 希望得到的结论；只读取 Lab question 与 acceptance criteria。先记录原始结果，再解释结果。

### Model / Reasoning Guidance

强调可复现性、故障定位和环境精确性；复杂 Lab 使用高推理深度，但任何推理都不能替代实际 command output。

## 7. Publisher

### Purpose

只在 Article `FINAL` 后把冻结正文安全映射到 Hugo content，校验 metadata、路径、链接、render / build，并准备一致的发布状态与 checkpoint。

### Inputs

- FINAL Draft、final review decision；
- repository Hugo / front matter / series rules；
- target content path 与 status update requirements。

### Required Reads

- `AGENTS.md` / `CLAUDE.md` 的 Hugo、front matter、shortcode 与 Git 规则；
- FINAL Draft、review.md、Article README；
- related published pages、series metadata、`status.md`；
- 真实 build / CI command。

### Allowed Writes

- 当前 Article 的 published content 与发布图片；
- front matter、series metadata、internal links；
- Article README、`status.md`、canonical 中由 Publish Gate 要求的状态 / 链接；
- publication / build result 与 checkpoint metadata。

### Forbidden Actions

- 不修改 frozen knowledge content；机械映射之外的语义变化必须返回 Review；
- 不把 current source 已推翻的 Claim 就地修掉；必须 `RETURN_TO_REVIEW`；
- 不修改无关 theme / CI / content，不 broad-stage，不在未授权时 push。

### Required Outputs

- published path 与 route；
- front matter / series / internal-link checks；
- FINAL Draft vs Published Body semantic-diff result；
- Hugo / lint / link commands 与真实结果；
- status / canonical update candidate；
- publication Gate decision。

### Gate Responsibility

负责 Publish 与 Build Verify Gate。Reviewer `PASS` 但 Hugo build 失败时，Article 不能成为 `PUBLISHED`。

### Stop Conditions

- Article 不是 `FINAL`；
- semantic mapping 需要改变知识内容；
- source contradiction、broken build、link / metadata failure；
- unrelated dirty changes 无法安全隔离。

### Handoff Contract

成功时向 Master 交付完整发布文件清单、build evidence 与 checkpoint-ready diff；失败时返回 `FAILED_PUBLICATION` 或 `RETURN_TO_REVIEW`，不得自行降级。

### Context Isolation Rules

只需要 FINAL artifacts 与发布规则，不读取 Author hidden reasoning。若 semantic difference 不为零，以 repository diff 为证据返回 Review。

### Model / Reasoning Guidance

偏好机械一致性、严格验证和低变异操作；构建错误按事实诊断，不把 warning / static check 扩大成运行时验收。

## 8. Part Auditor

### Purpose

在 Part 末尾使用 fresh context 审查跨文章一致性、学习递进、Lab / DSH / BuildPilot boundary 与职业能力覆盖，决定是否允许进入下一 Part。

### Inputs

- Part boundary 与 canonical；
- 该 Part 所有 Article Card、published articles、evidence summary、Lab result、Glossary 与 status。

### Required Reads

- `course-factory.md` 的 Part Audit / degradation rules；
- canonical、Glossary、status；
- 该 Part 全部实际生产文章的 Article Card 与 Published Content；
- relevant Evidence summaries、Lab results 与前后 Part bridge。

### Allowed Writes

- Part Audit Report；
- cross-article Findings、affected Article、severity、required actions 与 Gate；
- `status.md` / run-state 的 audit result candidate，经 Master 验证后落盘。

### Forbidden Actions

- 不重写整个 Part，不直接修正文；
- 不把 optional article 静默改成 required；
- 不因评分模式可疑就自动判失败；先输出 `QUALITY_DEGRADATION_REVIEW`；
- 不执行 Course Final Audit，除非当前任务明确是 final scope。

### Required Outputs

- Part Audit Report；
- Concept Drift、Glossary Drift、Contradiction、Duplication、Missing Dependency、Forward Reference、Learning Progression、Job Competency Coverage 检查；
- Lab / DSH / BuildPilot mode-specific checks；
- Findings、Affected Articles、Severity、Required Actions、`PASS / FAIL` Gate。

### Gate Responsibility

Part Auditor `PASS` 是进入下一 Part 的必要条件。`BLOCKER / MAJOR` 必须把具体受影响 Article 返回必要状态修复，再执行 targeted re-audit。

### Stop Conditions

- required Article / Lab artifact 缺失；
- canonical、status 与 published series 冲突；
- 发现课程架构级矛盾或持续质量退化，无法靠局部 Article 修复。

### Handoff Contract

向 Master 交付 Part Gate、Finding register、affected Article 和最小返工范围。Master 只按报告路由 worker，不让 Auditor 直接 repair。

### Context Isolation Rules

使用 fresh context；不读取各 Author / Reviewer 的隐藏推理或 self-score。审计依据是跨 Article repository artifact 与真实 Lab / build evidence。

### Model / Reasoning Guidance

需要较高的跨文档推理与模式识别；保持 evidence-backed，区分一次异常、质量退化信号和真实 Gate failure。
