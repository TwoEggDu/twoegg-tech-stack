# Article Card｜12 Context Engineering

> 权威基线是`docs/agent-engineering-series-plan.md`。`docs/agent-engineering-course-plan-v3.1-review.md`的 Article 12 frozen section只作结构输入，不承担生产期状态或事实权威。本文件只机械实例化既有课程职责，不预设 Research / Evidence 结论。

## Canonical identity

- Title：`Context Engineering：每一个 Step 到底应该看到什么`
- Part：`Part III｜Context Engineering 与 Memory`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`STANDARD_ARTICLE`
- Required Lab：`NONE`；需为未来 Lab 05 产出 3 个 Context Snapshot。

## Positioning

信息主线篇。在读者已经理解 Step、Plan、State、Checkpoint 与 Tool Result 后，研究一次模型请求的完整装配；核心调试对象从“Prompt 写了什么”推进到“这个 Step 实际看到了什么”。

## Reader questions

1. Prompt 与 Context 的边界是什么？
2. 一个 Step 的 Context 有哪些来源？
3. Tool Schema、Tool Result 与 Workflow State 为什么都属于 Context？
4. Selection、Ordering、Priority、Scope 与 Budget 怎样协作？
5. 怎样记录该 Step 实际收到的 Context Snapshot / Receipt？

## Dependencies

- Article 02：Prompt Contract。
- Article 06：Tool Schema / Result / Trace。
- Article 08：Step 与 Agent Loop。
- Article 09：Planning 与 History 边界。
- Article 10—11：Workflow State、Checkpoint 与 Recovery。

## Candidate mental model

```text
Prompt + State + History + Tool Schema / Result + Environment
  -> Select -> Order -> Fit Budget -> Snapshot
  -> Model Step
```

这是课程待验证的工程模型，不是任何特定产品的统一请求 schema。

## Canonical content spine

1. Context 是每个 Step 重新装配的构建产物，不只是 Message List。
2. 六类来源：指令、当前目标、Working State、History、Capabilities、External Facts。
3. Select / Order / Scope：常驻信息与按 Stage / Project / Agent 加载的信息。
4. Context Budget：输入、输出、Tool Schema 与 Result 共享有限窗口；预算同时约束质量与成本。
5. Snapshot / Receipt：记录来源、版本、冲突、裁剪、未知与最终装配结果。
6. 调试桥：答案错误时先问“这个 Step 看到了什么”，但不展开 Article 13。

## Required examples

- 同一调查 Step 的 Request Breakdown。
- Contributor Priority 表。
- 至少一份 Context Receipt schema / sample。
- 三个 Context Snapshot，作为未来 Lab 05 的设计输入，不实现 Lab 05。

## Evidence requirements

- 当前模型请求 / Context Window 官方资料，记录产品、版本或检索日期与适用范围。
- 可追溯的模型请求 Trace 或等价请求装配证据。
- 至少一份 Context Receipt 样例，明确哪些字段是课程 Proposal。
- Context != Prompt、Context != Session、Tool Result != 永久历史、Snapshot != Memory 的反证搜索。
- 明确 Stable / Dynamic Context、Selection、Ordering、Priority、Scope 与 Budget 的证据强度和产品边界。

## Explicit non-goals

- 不讲向量检索、长期 Memory 与具体 Compaction 算法。
- 不把某个 SDK 的 request/message/tool schema 写成行业统一接口。
- 不把最终 Prompt 文本等同完整 Context provenance。
- 不把 Snapshot 等同 Session、Memory 或 Checkpoint。
- 不实现或预演 Lab 05，不启动 Article 13。
- 不读取 DeepSeek Harness 私有源码，不把 BuildPilot Design 写成已实现 Runtime。

## Learning check candidates

1. Workflow State 未进入请求，属于 Prompt 问题还是 Context 问题？
2. Tool Schema 常驻 80 个，会挤占哪些预算？
3. 只保存最终 Prompt 文本，能否追踪每段信息来源、版本与裁剪原因？

## Job competency target

- 能把一次模型请求拆成可解释、可预算、可重建的 Context Assembly。
- 能依据阶段、优先级与作用域选择 Contributor，并说明被舍弃信息的后果。
- 能用 Snapshot / Receipt 回答“这个 Step 实际看到了什么”，同时守住产品与课程 Proposal 边界。
