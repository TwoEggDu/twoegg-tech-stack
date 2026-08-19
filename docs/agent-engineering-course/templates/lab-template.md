# Agent Engineering Lab 模板

Lab Card 同时承载 Researcher 冻结的 `LAB_DESIGN` 与 Lab Engineer 写入的 `LAB_OBSERVATION`。Expected 与 Observed 必须分开；Lab Engineer 不修改 Design，Researcher在执行后负责 `EVIDENCE_MERGE`。

## Metadata

- Lab ID：`Lab NN`
- Title：
- Owning Article：
- Lifecycle Status：`PLANNED`
- Evidence Status：`BLOCKED`
- Runtime / Language：`C# / .NET`
- Fixture Version：
- Environment：
- Last Run：

## Goal

这个实验要消除哪一个具体的不确定性？

## Lab Design（Owner：Researcher）

- Related Article：
- Related Claim IDs：
- Research Question：
- Hypothesis：
- What Would Falsify It：
- Fixture Boundary：
- Environment：
- Inputs：
- Variables：
- Expected Observable：
- Fault Injection：
- Commands / Execution Needs：
- Acceptance Criteria：
- Evidence Mapping：
- Limitations：
- Safety / Permission Constraints：

`Expected Observable` 是运行前冻结的判据，不是 `Observed Result`，不得在 Lab 执行后反向修改来适配结果。

## Prerequisites

- 所需 SDK、模型 Provider、凭证或本地替身
- 固定版本与环境
- 输入 fixture 与预期可观测输出

## Question and Claims

| Claim ID | 可判定问题 | 成功判据 | 失败判据 |
|---|---|---|---|
| | | | |

## Fixture

说明最小目录、输入、配置、mock/stub 和刻意保留的失败条件。Fixture 必须能独立复现，不能依赖作者机器上的隐式状态。

## Run Instructions

```text
<commands or steps>
```

## Observations（Owner：Lab Engineer）

记录原始输入、输出、trace、错误和时间信息；不要在本节提前写解释。

- Environment：
- Commands：
- Exit Codes：
- Build Result：
- Test Result：
- Runtime Output：
- Fault Injection Result：
- Observed Behavior：
- Unexpected Behavior：
- Reproduction Notes：
- Runtime Limitations：

| Run | Input | Raw Output / Trace | Result |
|---|---|---|---|
| | | | |

## Expected Failure Paths

- 无效输入：
- Provider / Tool 失败：
- 超时或取消：
- 结构化输出不满足合同：
- 预算耗尽：

## Interpretation / Evidence Merge（Owner：Researcher）

从原始观测到 Claim 的推理链是什么？是否存在替代解释？

解释顺序必须是：`Experiment -> Observation -> Evidence Interpretation -> Claim Status`。不得从 Article Thesis 反向改写 Observation。

## Conclusion

- Confirmed：
- Partial：
- Blocked：
- Follow-up：

## Limitations

实验结论只覆盖本 fixture、固定版本、运行环境和观测条件。它不能自动外推到其他 Provider、模型、规模或生产负载。

## Evidence Links

- Evidence Card：
- Raw trace / log：
- Source revision：
- Article section：
