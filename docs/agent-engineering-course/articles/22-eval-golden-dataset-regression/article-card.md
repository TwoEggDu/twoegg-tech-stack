# Article 22 Card｜Eval、Golden Dataset 与 Regression：修复以后还会不会再坏

## Identity

- Canonical ID: `22`
- Part: `IV｜Reliable Agent Engineering`
- Weight: `L`
- Optional: `NO`
- Required Lab: `Lab 06｜Trace + Eval`
- Article Type: `PRINCIPLE`
- Mode: `LAB_ARTICLE`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

## Problem Space

一次修复能让当前样例通过，却不能回答系统是否真正改善、旧能力是否退化、评估数据是否泄漏，或阈值变化是否只是把失败藏起来。本文要把 Eval、Golden Dataset 与 Regression 从“跑几个案例看起来不错”拆成可审计的任务、样本、oracle、metric、threshold、baseline 与 verdict 合同，并用 Lab 06 真实验证一个已知退化能否被固定评估集捕获。

## Core Questions

1. Demo、Test、Benchmark、Eval 与 Regression 分别回答什么问题，为什么不能互相替代？
2. 一个可审计 Eval Case 的最小 identity、input、lineage、oracle、scorer 与 version manifest 是什么？
3. Trace candidate 怎样经过筛选、去敏、去重、review 与 acceptance 才能成为 Golden sample？
4. Exact、rule-based、structured、semantic 与 human-judged oracle 各自有哪些错误边界？
5. Metric、threshold、baseline、aggregation 与 uncertainty 应怎样分账，避免一个总分吞掉关键失败？
6. Train/dev/test、Golden、canary 与 regression corpus 怎样防止数据泄漏和过拟合？
7. Regression verdict 应怎样同时保存新失败、既有失败、改善、退化、未知与不可比状态？
8. Lab 06 能否在固定 C#/.NET fixture 中，用同一数据集与冻结判据捕获一次已知退化，并保留 raw observation？
9. Eval 结果怎样进入发布门禁而不伪装成生产质量、跨模型泛化或统计显著性？
10. deterministic Regression 与 stochastic Agent Eval 怎样分账，后者需要哪些 multi-trial、manifest、distribution、judge calibration 与 uncertainty 记录才能判断退化而不是正常波动？
11. Article 22怎样收束Part IV，同时不提前启动Article 23 Multi-Agent或Article 24 Harness？

## Frozen Boundaries

- 不把单次 Demo、单元测试通过或人工“看起来不错”写成 Eval / Regression PASS。
- 不把 Trace candidate 自动升级为 Golden sample；必须保留来源、选择、review、版本、redaction 与 acceptance lineage。
- 不把 exact/rule scorer 覆盖不到的语义质量伪装成已测；也不把 LLM-as-judge 当无偏真值。
- 不把 aggregate score 掩盖关键 case、分组退化、不可比版本或缺失观测。
- Lab 06 只证明固定 fixture、数据、判据、fault injection 与环境；不外推 Provider、模型、生产负载或统计泛化。
- `22-C13` 只以 source-backed `PARTIAL / COURSE PROPOSAL` 进入教学闭环；不规定固定 trial 数，不声称统计显著，也不声称 Lab06 验证 stochastic Agent Eval。
- Lab06 v1 是 fixture-specific evaluator：`scorer-policy.json` 同时是 fixture contract manifest 与部分配置输入，Runtime 只解析 aggregate threshold；未来 scorer/gate/system manifest 分离仅为 `BuildPilot / Harness design candidate / PROPOSAL / NOT IMPLEMENTED / NOT RUN`。
- BuildPilot 仍是课程设计案例；Lab fixture不是BuildPilot runtime实现或production evidence。
- Article 23为Advanced/Optional且明确SKIP/PLANNED/零资产；Article 24不得启动或产生资产。

## Claim / Evidence Coverage

- Claims: `13 / 13`
- Evidence Cards: `13 / 13`
- Evidence posture: `3 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Post-publication addition: `22-C13 / 22-E13 = PARTIAL`；Lab Dependency=`NONE — LAB06 DOES NOT TEST STOCHASTIC AGENT EVAL`

## Expected Artifacts

- `research.md`
- `evidence.md`
- `outline.md`
- `draft.md`
- `review.md`
- `subagent-trace.md`
- `docs/agent-engineering-course/labs/lab-06-trace-eval/`
- final Published Content candidate: `content/ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md`
