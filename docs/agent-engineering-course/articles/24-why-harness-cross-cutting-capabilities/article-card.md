# Article 24 Card｜为什么最终需要 Harness：横切能力由谁承载

## Identity

- Canonical ID: `24`
- Part: `V｜Harness Engineering`
- Weight: `L`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED`

## Problem Space

Prompt、Tool 和业务 Workflow 可以分别完成局部任务，但当系统扩展到多个 Agent、Tool、Workflow 与长时间运行后，身份、权限、Evidence、Budget、Trace、审批、上下文、恢复、知识和能力发现会散落在每条业务链里。本文只研究为什么这些横切能力需要一个共享执行与治理边界，以及为什么团队约定或更长的 System Prompt 无法替代它。

## Core Questions

1. 什么是横切能力，为什么它不会自然归属于单个 Prompt、Tool 或业务 Workflow？
2. 为什么复制 Permission、Evidence、Budget、Trace 与 Approval 逻辑会造成策略和失败语义漂移？
3. Harness 为什么不是更长的 System Prompt，也不是无所不包的 God Object？
4. BuildPilot 为什么需要统一身份、权限、Context、Evidence、Approval、Trace、Knowledge 与 Capability Discovery？
5. 业务 Agent 与共享控制面的初步区别是什么？
6. 为什么“建议优先 + Human Review”仍需要可执行的治理边界，而不能只靠团队约定？

## Teaching Spine

```text
局部任务可以工作
-> 横切治理开始散落
-> 重复实现产生策略漂移与不可审计状态
-> 共享不变量需要统一承载者
-> Harness 作为共享执行与治理边界出现
```

## Required BuildPilot scenario

```text
策划需求变更
-> Requirement Contract candidate
-> 缺失条件与歧义显式化
-> 只读检查 C# / 跨表配置 / 资源规范 / 构建证据
-> Finding / Intent Drift / Tool Gap
-> Evidence-backed Change Request
-> Owner 审核并实施
-> BuildPilot 重新验证
-> Intent Ledger / Knowledge Store
-> Rule / Test / Gate candidate
```

## Frozen Boundaries

- 本篇只证明 Harness 的必要性；Runtime/Harness 精确责任边界留给 Article 25。
- 不提前给出 Article 26 的完整最小能力模型、接口矩阵和失败语义。
- 不提前完成 Article 27 的成本、风险、演进阶段和不适用条件。
- Harness 不是更长的 Prompt、业务 God Object、完整 Runtime、Knowledge Base、RAG 或单一厂商产品。
- BuildPilot 始终是 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED`；V1 只读、建议优先，不直接修改生产代码、策划配置表、美术资源、Importer/Meta、Policy 或 Capability Registry。
- `Intent Drift` 只能在证据支持下确认；仅从代码推断的原因保持 `CANDIDATE_INTENT`。
- 能力缺口使用 `Governed Capability Evolution`；不得写成自主提权、静默安装或“永不复发”。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md`
