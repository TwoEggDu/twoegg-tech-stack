# Article 10 Article Card

## Metadata

- ID：`10`
- Title：`State Machine 与 Workflow：确定性骨架和 Agent Decision Point`
- Part：`II｜从模型到 Agent`
- Weight：`L（Major Core Lesson）`
- Optional：`NO`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`（Lab 04 在 Article 11 同时验证 State Machine、Checkpoint 与 Resume）
- Lifecycle：`RESEARCHING`
- Evidence：`BLOCKED`

## Positioning

核心边界篇。回答“流程可以写代码，为什么还需要 Agent”。

## Why Now

Article 08 的自由 Loop 能执行，Article 09 也能形成候选 Plan，但长任务仍可能漏步骤、重复或违反不变量。本篇研究怎样把确定状态与转换交给程序，只保留真正需要上下文和证据判断的 Decision Point。

## Reader Questions

1. Plan、Workflow Definition 与当前 State 为什么是三种对象？
2. Agent Loop 与 State Machine 有什么差别？
3. 哪些状态转移必须确定性验证？
4. Workflow 何时调用 Agent，Agent 何时调用受控 Workflow Tool？
5. Checkpoint 与普通 State 有何区别？

## Prerequisites

- Article 08：Agent Loop。
- Article 09：Planning。
- 传统状态机基础。

## Core Concepts

`State`、`State Machine`、`Workflow`、`Stage`、`Transition`、`Guard`、`Invariant`、`Agent Decision Point`、`Terminal State`。

## Candidate Mental Model

```text
确定状态 / 转换 -> Program / Workflow
需要上下文判断的转移 -> Agent Decision Point
```

这是冻结的研究对象，不是已确认的行业统一 Workflow 架构。

## Canonical Content Spine

1. 自由 Loop 的失败：漏步骤、重复 Tool、不可恢复，以及为什么自主性应只放在真正不确定的位置。
2. State Machine：State、Transition、Guard、Invariant、Terminal。
3. Workflow + Agent：Workflow 调 Agent、Agent 调受控 Workflow Tool、Code Orchestration。
4. Agent Decision Point：输入允许的 State / Evidence / Candidate Plan，输出受 Schema 与 Guard 约束的选择。
5. 从 State 到 Checkpoint：当前状态可表达流程位置，但跨中断恢复还需要持久化与副作用语义。

## Evidence Requirements

- 状态机的一手规范或权威资料。
- 三种编排模式的可核验示例，且明确产品 / 版本范围。
- 一条坏 Trace 或最小 fixture，用来观察自由 Loop 的重复、漏步或非法转移。
- 一份状态图 / transition table，把 Model suggestion 与 legal transition 分开。
- Counter-evidence：现实产品可能组合 Workflow、Agent 与 Runtime 职责，不能把课程分层写成行业唯一实现。

## Adjacent Boundaries

- Workflow ≠ Agent Loop。
- Plan ≠ Workflow State。
- Stage ≠ Step。
- Model suggestion ≠ legal transition。
- Checkpoint / Retry / Cancellation / Recovery 留给 Article 11。
- 不做 BPM 教程，不引入 Multi-Agent，不处理分布式事务。

## DSH / BuildPilot Bridge

- DSH：Article 33—34 再观察 Loop 与 Session State；Article 37 判断 Workflow 是核心事实还是扩展映射。
- BuildPilot：用于推导固定 Intake / Evidence / Diagnosis / Review 阶段，并把调查路径留给 Agent；本篇不实现 BuildPilot Runtime。

## Learning Check

1. Unity Build 固定阶段应由模型逐步决定吗？
2. 模型计划跳过 Evidence 阶段，谁拒绝？
3. 状态固定但分支条件需读取多源 Evidence，哪部分适合 Agent？

## Job Competency Target

- 能把开放式模型判断嵌入可验证的确定性执行骨架。
- 能区分 candidate decision、legal transition 与 authoritative state。
- 能为后续 long-running recovery 设计清晰的状态边界。
