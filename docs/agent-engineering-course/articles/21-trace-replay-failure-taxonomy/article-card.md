# Article 21 Card｜Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层

## Identity

- Canonical ID: `21`
- Part: `IV｜Reliable Agent Engineering`
- Weight: `L`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

## Problem Space

Agent 失败时，普通日志往往只能说明“某处报错”，却不能回答输入、决策、策略、工具、状态提交或外部依赖究竟在哪一层偏离。本文要研究怎样把一次 Run 保存为有身份、有因果、有边界的 Trace，并区分确定性重放、受控再执行与仅用于诊断的投影，最终建立可操作的 Failure Taxonomy。

## Core Questions

1. Log、Metric、Trace 与 Audit Record 各自回答什么问题，为什么不能互相替代？
2. 一次 Agent Run 的最小 Trace identity、event envelope、causal order 和 payload reference 应包含什么？
3. Replay、Resume、Retry、Rerun、Simulation 和 Projection 应怎样切开？
4. 哪些非确定性输入必须冻结或显式记录，才能诚实描述 replayability？
5. Failure 应如何按 Model / Policy / Tool / Runtime / State / Infrastructure / External Dependency 分层？
6. 一个外层 symptom 如何保留内层 root failure、contributing factor 与 recovery outcome？
7. Sensitive input、tool output、approval evidence 与 redaction 应如何同时满足可诊断性和权限边界？
8. Trace 如何为 Article 22 的 Eval / Golden Dataset / Regression 提供输入，而不提前吞掉评估结论？

## Frozen Boundaries

- 不把“有日志”写成“可重放”，不把同一 prompt 再调用模型写成确定性 replay。
- 不宣称跨 Provider、跨版本、跨时间或跨外部环境可获得 bit-for-bit 一致结果。
- 不把 retry、resume、rerun、projection、simulation 与 replay 合并为一个动作。
- 不把外层 exception message 直接当 root cause；Failure Taxonomy 必须保留发生层、观察层与恢复层。
- 不提前完成 Article 22 的 Eval、Golden Dataset、Regression verdict 或 Lab 06。
- BuildPilot 只允许作为课程构造的设计样例；不得声称实现、运行、生产收益或真实故障结果。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`，除非 Factory 重新授权。

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md`
