# Article 20 Card｜Budget Engineering：Token、Step、Cost 与 Latency

## Identity

- Canonical ID: `20`
- Part: `IV｜Reliable Agent Engineering`
- Weight: `M`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

## Problem Space

Agent 运行不是一次无限资源的推理。一次 Run 同时消耗模型输入/输出 Token、Agent Step、工具尝试、金额和墙钟时间；如果只在结束后统计，预算就只是报表，不是执行约束。本篇要解释如何把多维 Budget 变成 admission、reservation、enforcement、exhaustion routing 与 audit record 的工程合同。

## Core Questions

1. Token、Step、Cost、Latency 为什么是相互关联但不可互换的四类预算？
2. Context Window、Token usage 与 Token Budget 有什么不同？
3. Step Budget 应怎样约束 Agent loop，而不伪装成任务完成保证？
4. Cost estimate、reservation 与 actual usage 在价格/usage未知时怎样保持诚实？
5. Deadline、timeout、queue/service time 与端到端 Latency Budget 怎样分层？
6. Budget 应在哪些执行点检查、预留、扣减、释放和重新验证？
7. 某一维耗尽时，系统何时 stop、degrade、request approval 或返回 partial result？
8. Budget record 最小需要哪些 identity、estimate、actual、remaining、decision 与 uncertainty 字段？

## Frozen Boundaries

- 不固化任何易漂移的模型窗口、价格或 service-tier 数值；若必须举例，只能使用版本/日期固定的官方 contract，并标明访问日期。
- 不把 Provider usage 字段当作跨 Provider 统一语义，不推测不可见的内部 tokenization、queueing 或计费实现。
- 不把一个 timeout 当成完整 latency budget，也不把 step cap 当成功率、质量或安全保证。
- 不提前完成 Article 21 的 cross-step Trace/Replay/Failure Taxonomy，也不提前完成 Article 22 的 Eval/Golden Dataset/Regression。
- BuildPilot 只允许作为课程构造的设计样例；不得声称实现、运行、生产收益或真实成本结果。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`，除非 Factory 重新授权。

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md`
