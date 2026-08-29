# Article 25 Card｜Agent Runtime vs Harness：执行内核与工程控制面

## Identity

- Canonical ID: `25`
- Part: `V｜Harness Engineering`
- Weight: `L`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`

## Problem Space

Article 24证明共享控制边界为什么出现，但“Runtime执行什么、Harness治理什么、Host承载什么、业务Agent决定什么”仍未切清。若只按产品类名或厂商模块名分类，同一能力会被重复归属；若把所有运行能力都叫Harness，又会失去可替换性、失败定位与责任审计。本篇以责任、状态所有权和不变量为判断轴，建立可操作的边界规则。

## Core Questions

1. 谁真正执行模型调用、Tool调用、调度、等待、状态推进与恢复动作？
2. Context的选择、组装、裁剪、隔离与持久化分别由谁负责？
3. Identity、Permission、Approval与Sandbox Policy由谁定义、执行和保存？
4. Budget、Trace、Evidence、Checkpoint与Replay分别属于执行事实还是治理语义？
5. Tool/Skill Registry与Capability Discovery怎样跨Runtime、Harness和Host分工？
6. 业务状态与治理/执行状态为什么不能混成一份State？
7. Failure classification、retry boundary和human takeover由谁决定？
8. 同一产品可以实现多层责任时，为什么概念边界仍需要保留？

## Teaching Spine

```text
同一产品可以拥有很多模块
-> 产品边界不等于责任边界
-> 用 owner / state / invariant / failure / replacement 五个问题切层
-> Runtime 承担执行闭环，Harness施加共享控制，Host连接环境，业务Agent保留领域判断
-> BuildPilot同一需求变更链按层分账
```

## Required BuildPilot scenario

```text
Host 接收策划需求与Owner交互
-> BuildPilot业务逻辑决定检查目标与领域顺序
-> Runtime运行任务图、请求模型、调用只读Tool并推进Step
-> Harness在每一步施加Identity / Permission / Evidence / Budget / Trace / Approval / Recovery约束
-> Owner在BuildPilot外实施变更
-> Runtime执行re-verification，Harness保存可审计治理结果
```

## Frozen Boundaries

- 本篇讲Boundary，不重复Article 24的Why，不提前完整给出Article 26 Minimum Model或Article 27 Trade-off/Adoption。
- Runtime、Harness、Host、Agent Framework、Workflow Engine和业务Agent可由同一产品/进程实现，但概念责任仍需区分；不得冒充行业唯一划分。
- 不把所有Runtime能力重新命名为Harness，也不把Harness写成God Object或业务决策者。
- BuildPilot始终是`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`；只读、建议优先、Human Review，不能直接修改生产资产或静默提权。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`
