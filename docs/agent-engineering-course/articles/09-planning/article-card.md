# Article Card｜09 Planning

> 来源基线：`docs/agent-engineering-series-plan.md` 与其声明的结构基线 `docs/agent-engineering-course-plan-v3.1-review.md` Article 09 frozen section。本文件只机械实例化既有课程职责，不预设 Research / Evidence 结论。

## Canonical identity

- Title：`Planning：Agent 为什么需要计划，又为什么不能迷信计划`
- Part：`Part II｜从模型到 Agent`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`

## Positioning

计划机制篇。研究 Agent 为什么需要形成候选步骤，以及 Plan 为什么既不是执行结果，也不能凌驾于确定性约束之上。

## Reader questions

1. Implicit Plan 与 Explicit Plan 有什么差别？
2. ReAct、Plan-and-Execute、Planner / Executor 各强调什么控制方式？
3. Plan Revision 与 Re-planning 在何时发生？
4. 为什么 Plan 不等于 Execution、Workflow 或 Verified State？
5. 哪些机制有权拒绝模型计划？

## Dependencies

- Article 08：Agent Loop；基本任务分解经验。

## Candidate mental model

```text
Goal + Current Evidence
→ Candidate Plan
→ Policy / Workflow / Evidence Gate
→ Execute one safe step
→ Observe
→ Keep / Revise / Replace Plan
```

这是冻结的研究对象，不是已确认的行业统一 Planning runtime。

## Canonical content spine

1. 多步目标为什么需要表达依赖、顺序与未知。
2. Implicit / visible / structured Plan 的边界。
3. ReAct、Plan-and-Execute、Planner / Executor 作为控制方式的轻量比较。
4. 新 Observation、Tool failure、前提证伪或目标变化触发的 Revision / Re-planning。
5. Tool Policy、Workflow Guard、Evidence Gate 与 Budget 对计划的拒绝或收窄权。
6. 把不应每次重想的顺序和不变量交给 Article 10 的确定性骨架。

## Evidence requirements

- Agent Loop / Planning 的权威一手资料；
- 至少一条计划在反证后被修订的可核验 fixture / trace；
- 所选 Planning control pattern 的定义、证明范围与反证边界；
- 版本、产品与课程抽象必须分层标注。

## Explicit non-goals

- 不做 Planning 论文综述或复杂搜索算法比较。
- 不把 Chain-of-Thought 当作必须持久化的 Plan。
- 不把 Plan 写成执行结果、Verified State、Workflow、授权或成功证明。
- 不展开 Article 10 State Machine / Workflow。
- 不展开 Article 11 Checkpoint / Retry / Cancellation / Recovery。
- 不读取 DeepSeek Harness 源码，不实现 BuildPilot Runtime。
