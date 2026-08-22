# Agent Engineering 最小实验集

Lab 用于回答文章中无法只靠文档或源码可靠回答的行为问题。M0 只冻结实验职责和模板，不创建代码、fixture 或运行结论。默认语言与运行时为 C# / .NET；确有跨语言必要时必须在 Lab 卡片中说明原因。

## 计划中的 6 个 Lab

| Lab | 依赖文章 | 最小问题 | M0 状态 |
|---|---:|---|---|
| Lab 01 | 03 | 结构化输出在合法、缺字段、类型错误和修复重试时表现如何 | `PLANNED / BLOCKED` |
| Lab 02 | 06 | Tool Runtime 如何完成 validate、policy、execute、result 与 trace | `PLANNED / BLOCKED` |
| Lab 03 | 08 | 最小 Agent Loop 如何推进 step，并在成功、失败和预算条件下停止 | `PLANNED / BLOCKED` |
| Lab 04 | 11 | 长运行任务如何 checkpoint、取消、重试和恢复 | `PLANNED / BLOCKED` |
| [Lab 05](lab-05-context-debugging/README.md) | 13 | Context packing、污染、压缩和重建如何影响结果 | `EVIDENCE_MERGED / EVIDENCE_GATE_PASS / FIXTURE-SCOPED` |
| Lab 06 | 22 | Golden Dataset 与回归评估能否捕获一次已知退化 | `PLANNED / BLOCKED` |

## 实例化规则

1. 只有所属文章进入 `RESEARCHING`，完成 Preliminary Evidence 且确认需要行为证据后，Researcher 才创建 `labs/lab-<nn>-<slug>/`。
2. Researcher 使用 [Lab 模板](../templates/lab-template.md)建立并冻结 Lab Design；Lab Engineer 只执行该 Design，不修改 hypothesis 或 acceptance criteria。
3. 运行前固定依赖、Provider、模型、输入、环境和判据。
4. 原始输出与解释分开保存；失败运行也是证据。
5. 实验结论只用于其 Claim 和明确的适用边界。
6. Lab Engineer 把 raw Observation 交回 Researcher执行 Evidence Merge；两者只返回状态候选。Master 验证后统一回写 [课程状态台账](../status.md)与 run state，Researcher只更新文章 Evidence Card。
