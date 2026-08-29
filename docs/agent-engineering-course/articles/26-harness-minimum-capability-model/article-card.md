# Article 26 Card｜Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery

## Identity

- Canonical ID: `26`
- Part: `V｜Harness Engineering`
- Weight: `L`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`

## Problem Space

Article 24解释了共享治理边界为什么出现，Article 25切清了Runtime、Harness、Host与业务Agent的责任。Article 26继续回答：如果团队要实现一个最小但闭环的Harness，哪些能力由已证明的不变量推导出来，哪些只是环境相关扩展；每项核心能力的输入、输出、依赖、信任边界、失败语义、降级方式和可观测证据是什么。

## Core Questions

1. “最小但闭环”要维持哪些跨步骤、跨工具、跨会话不变量？
2. Identity、Session与Ownership怎样建立可归属的执行边界？
3. Context Assembly、隔离、Capability Registry和版本怎样避免错误能力进入执行？
4. Permission、Approval、Sandbox与Policy Enforcement怎样形成拒绝优先的信任边界？
5. Execution Control、State、Checkpoint与Recovery怎样避免把恢复等同于盲目重试？
6. Trace、Evidence、Replay与Failure Taxonomy怎样形成可审计但不夸大可复现性的记录链？
7. Budget、HITL、Evaluation与Knowledge controls哪些是最小核心，哪些可按风险延后？
8. BuildPilot最小闭环怎样从需求读取走到Finding、Change Request、Human Review、re-verification与知识沉淀？

## Teaching Spine

```text
不是功能菜单
-> 先写必须长期成立的不变量
-> 再推导最少责任闭环
-> 对每项能力定义合同、失败与证据
-> 区分最小核心和环境相关扩展
-> 用BuildPilot映射可落地但未实现的V1闭环
```

## Frozen Boundaries

- 本篇讲Minimum Model，不重复Article 24 Why或Article 25 Boundary，不提前完成Article 27 Trade-off/Adoption。
- “最小核心”必须由问题与不变量推导，不得把所有候选能力都判定为Mandatory。
- Harness不是God Object；Runtime、业务Agent、Tool、Workflow、Policy、Knowledge Base和RAG边界保持清楚。
- BuildPilot始终是`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`；Owner在BuildPilot外实施真实修改。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md`
