# Article 18 Card｜Evidence Contract

## Canonical identity

- ID: 18
- Title: Evidence Contract：把自然语言推断变成可审计工程数据
- Part: IV｜Reliable Agent Engineering
- Weight: L / Deep Core Lesson
- Optional: NO
- Mode: NORMAL_ARTICLE
- Required Lab: NONE
- Article Type: PRINCIPLE
- Concept Maturity: ENGINEERING

## Course position

- Upstream: Article 03 Structured Output；Article 06 Tool Runtime；Articles 12—17 Context、State、Memory、KB/RAG 与 Skill。
- Direct bridge: Part III 已建立“信息从哪里来、如何进入执行”；本篇建立“一个工程判断凭什么被接受”。
- Downstream: Article 19 Permission / Approval / Sandbox；Article 20 Budget；Article 21 Trace / Replay / Failure Taxonomy；Article 22 Eval / Golden Dataset / Regression；Article 24—27 Harness。

## Problem statement

Agent 可以生成流畅的解释、诊断和建议，但自然语言的确定语气不等于事实成立。若系统只保存一句结论，后续读者无法判断它来自直接观测、外部来源、推断还是设计选择，也无法知道适用版本、作用域、限制条件和反证。本文要把“看起来合理”改造成可验证、可拒绝、可追溯、可复核的工程证据合同。

## Human-approved questions

1. 为什么自然语言判断不能直接成为工程事实？
2. Claim、Evidence、Observation、Inference、Proposal 与 Unknown 应如何区分？
3. 一个最小可审计 Evidence Record 必须包含哪些字段？
4. source identity、version / time、scope、limitations 与 falsifier 为什么不可省略？
5. Citation、Provenance、Confidence 与 Acceptance 分别解决什么问题？
6. 多条证据冲突、过期或只覆盖部分 Claim 时如何处理？
7. Parse / Schema Validate 之后，Evidence Contract 还需要哪些语义验收 Gate？
8. Evidence 的追加、替换、失效、复核与审计历史如何表达？
9. BuildPilot 应怎样输出可审计的诊断证据包，而不是一句“根因是 X”？
10. 本篇与 Structured Output、Trace / Replay、Failure Taxonomy、Eval 的边界是什么？

## Teaching and evidence contract

- 原理篇：问题空间 -> 抽象模型 -> 最小记录 -> 接受状态机 -> 冲突/失效 -> BuildPilot 设计案例 -> 验证边界。
- 核心 Claim 必须映射 Evidence Card；任何 `FACT / OBSERVATION / INFERENCE / PROPOSAL / UNKNOWN` 都使用可区分措辞。
- 标准、规范和产品事实优先使用当前官方或固定版本的一手来源；moving source 必须记录访问日期与漂移边界。
- BuildPilot 仅用 `DESIGN / NOT IMPLEMENTED / NOT RUN` 语态；不虚构诊断准确率、成本、延迟、收益或生产运行证据。
- canonical 不要求 Lab；不得为了制造“验证感”临时增加无合同 Lab。

## Non-goals

- 不把 Evidence Contract 缩成 JSON 合法性或一个 citation URL 字段。
- 不提前完成 Article 19 的权限/审批模型、Article 21 的完整 Failure Taxonomy 或 Article 22 的 Eval 系统。
- 不宣称所有组织或工具共享统一 evidence schema。
- 不实现 BuildPilot Runtime、Evidence Store 或生产审计平台。
