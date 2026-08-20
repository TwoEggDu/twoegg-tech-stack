# Article Card｜08 Agent Loop

> 权威基线是 `docs/agent-engineering-series-plan.md`。`docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 08 frozen section 只作结构输入，不承担生产期状态或事实权威。本文件只机械实例化既有课程职责，不预设 Research / Evidence / Lab 结论。

## Canonical identity

- Title：`Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop`
- Part：`Part II｜从模型到 Agent`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Required Lab：`Lab 03 Minimal Agent Loop`

## Positioning

核心机制篇。接住一次 Tool Use、Tool Runtime 与 MCP 的能力边界，正式研究应用怎样根据 Observation 推进下一 Step，以及外部控制面怎样决定 Continue / Stop。

## Reader questions

1. Run、Turn 和 Step 怎样区分？
2. Tool Result 何时才成为 Observation？
3. State 由谁更新？
4. Continue / Stop 由哪些条件共同决定？
5. 怎样避免无限循环和伪完成？

## Dependencies

- Article 03：Structured Output contract 与 domain validation。
- Article 05：Function Calling 与 Tool Call intent。
- Article 06：Tool Runtime、Result 与 Trace。
- Article 07：协议层能力边界；MCP success 不等于 Agent Loop 已成立。

## Candidate mental model

```text
Goal -> Decide -> Act -> Observe -> Update
          ^                         |
          +------ Continue / Stop <-+
```

这只是 canonical 研究对象，不是已验证的统一 Agent 实现或停止合同。

## Canonical content spine

1. 一次 Tool Use 为什么不足以形成反馈循环。
2. Run / Turn / Step 的层级与 trace 边界。
3. 原始 Tool Result、规范化 Observation 与任务 State 的关系。
4. 目标、模型完成信号、输出合同、预算、Policy、取消与错误怎样共同参与 Stop。
5. 最小 Agent Loop，并把 Planning、Workflow / State Machine 与 Long-running 明确留给 Article 09—11。

## Lab 03 responsibility

- Runtime：`C# / .NET`。
- Core observation：Turn、Step、Observation、Stop。
- Candidate input：Mock Build Log、`parseMockLog`、`readMockFile`。
- Required observations：Step Trace 与 State Snapshot。
- Required failure themes：重复调用、没有证据却 Stop、预算 / Max Step 停止，以及失败结果不能冒充成功。
- Evidence requirement：必须完成并保存四条最小轨迹；精确 case matrix、Provider / deterministic substitute、trace schema 与 acceptance criteria 由 Researcher 在 frozen Lab Design 中确定。

## Evidence requirements

- current official / primary-source Agent SDK lifecycle 与 stop / max-turn contracts；
- Preliminary Evidence 与明确的 version / runtime scope；
- Lab 03 frozen Design、真实 source / tests / build / run / fault injection / raw trace；
- `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 映射；
- Stop 与 Success、Tool Result 与 Observation、Turn 与 Step 的反证搜索。

## Explicit non-goals

- 不深入 Article 09 Planning。
- 不展开 Article 10 Workflow / State Machine 的确定性编排。
- 不展开 Article 11 checkpoint / retry / cancellation / recovery。
- 不进入 Context、Memory 或 Multi-Agent。
- 不把历史结构输入、伪代码、expected output 或单一 SDK 行为写成跨 Provider 通用事实。
- 不读取 DeepSeek Harness 源码，不实现 BuildPilot Runtime。
