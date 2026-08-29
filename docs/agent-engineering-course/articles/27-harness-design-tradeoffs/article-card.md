# Article 27 Card｜Harness 的设计取舍：可替换性、复杂度、Bloat 与演化

## Identity

- Canonical ID: `27`
- Part: `V｜Harness Engineering`
- Weight: `M`
- Optional: `NO`
- Required Lab: `NONE`
- Article Type: `PRINCIPLE`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`

## Problem Space

Article 24—26依次解释了Why、Boundary和Minimum Model，但“可以设计”不等于“值得建设”。统一治理会带来一致性，也会制造集中瓶颈、额外成本、耦合、错误配置和虚假安全感。本篇收束Part V：给出基于风险与规模的采用判断、渐进路径、退出条件和明确的不适用场景。

## Core Questions

1. 统一治理的收益在哪些条件下大于集中式瓶颈？
2. Context、Trace、Evidence、Policy会增加哪些token、存储、成本与延迟？
3. 如何避免Harness锁死模型、工具、框架或供应商？
4. Policy drift、错误配置、knowledge staleness和错误Intent会怎样制造虚假安全感？
5. Approval fatigue、恢复复杂度、隐私与可观测性冲突怎样影响组织吞吐？
6. 小团队、单一工作流、低风险任务何时不值得建设Harness？
7. 渐进采用的每一级有哪些进入信号、收益、成本、退出条件和明确不建设项？
8. BuildPilot V1为什么只应优先只读检查、Evidence、Trace、Change Request与Human Review？

## Teaching Spine

```text
最小模型已经成立
-> 仍需证明值得建设
-> 用规模、风险、异构性、恢复压力和治理成本做判断
-> 给出可退出、可替换的渐进路径
-> 正面列出不适用条件与失败模式
-> 用BuildPilot做克制的V1取舍
```

## Frozen Boundaries

- 本篇讲Trade-off and Adoption，不重复Article 24 Why、Article 25 Boundary或Article 26 Minimum Model。
- 不把Harness写成所有团队的必选答案；阶段越高不代表越成熟或越正确。
- 所有量化收益、成本、延迟、缺陷下降都必须有真实Evidence，否则只能是`PROPOSAL/PARTIAL`。
- BuildPilot始终是`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`；V1不承诺完整愿景。
- Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。
- Article 28与Part VI不得启动或预写。
