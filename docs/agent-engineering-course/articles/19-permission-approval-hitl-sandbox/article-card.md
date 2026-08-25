# Article 19 Card｜Permission、Approval、Human-in-the-loop 与 Sandbox

## Canonical identity

- ID: 19
- Title: Permission、Approval、Human-in-the-loop 与 Sandbox
- Part: IV｜Reliable Agent Engineering
- Weight: L / Deep Core Lesson
- Optional: NO
- Mode: NORMAL_ARTICLE
- Required Lab: NONE
- Article Type: PRINCIPLE
- Concept Maturity: ENGINEERING

## Course position

- Upstream: Article 06 Tool Runtime；Article 10 State Machine / Workflow；Article 11 Long-running Agent；Article 18 Evidence Contract。
- Direct bridge: Article 18解决“一个判断凭什么被接受”；本篇解决“即使判断被接受，谁可以在什么约束下执行哪个动作”。
- Downstream: Article 20 Budget；Article 21 Trace / Replay / Failure Taxonomy；Article 22 Eval / Regression；Article 24—27 Harness control plane。

## Problem statement

Agent 获得工具、凭据或结构化 Evidence 后，并不自动获得执行所有动作的权威。Permission、Authorization、Approval、Human-in-the-loop 与 Sandbox 分别约束静态能力、具体请求、人工决策、暂停恢复流程与运行时隔离；若把它们混成一个“允许”开关，系统会失去最小权限、审批作用域、撤销、过期、重验证与审计边界。本文要建立从 principal 到 enforcement 的最小 action-authority 模型，并用 BuildPilot 高风险动作说明何时必须停下来等待人类决定。

## Human-approved questions

1. 为什么 accepted Evidence 不等于 action authority？
2. Permission、Authorization、Approval、Human-in-the-loop 与 Sandbox 分别解决什么问题？
3. 最小 action-authority 模型应如何连接 Principal、Capability / Action、Resource、Constraints、Policy、Approval 与 Enforcement？
4. 静态 permission、动态 authorization / approval 与 runtime enforcement 如何分层？
5. 如何按风险分类动作，哪些动作必须进入显式 Approval？
6. 最小 Approval Request / Decision Record 必须记录哪些对象、范围、有效期、理由和责任者？
7. Human-in-the-loop 的 pause、approve、reject、cancel、resume、幂等与重验证如何设计？
8. least privilege、credential scope、revocation、expiry 与 TOCTOU 风险如何进入执行边界？
9. Sandbox 能保证什么、不能保证什么，为什么它不能替代权限和审批？
10. BuildPilot 的高风险动作包如何落地这些边界，又如何与 Article 06 / 10 / 11 / 18 / 20 / 21 分工？

## Teaching and evidence contract

- 原理篇：问题空间 -> 五概念分账 -> action-authority model -> risk / approval records -> pause / resume -> sandbox limits -> BuildPilot design -> course boundaries。
- 核心 Claim 必须映射 Evidence Card；标准、规范、平台事实优先使用有版本或访问日期的一手来源。
- Product-specific HITL 或 sandbox 行为只能作为带版本/产品作用域的例子，不外推为通用行业保证。
- BuildPilot 只使用 `DESIGN / NOT IMPLEMENTED / NOT RUN` 语态；不虚构真实审批、凭据、sandbox、审计、收益或生产运行证据。
- Required Lab 为 `NONE`；不得为了制造验证感临时增加 Lab。

## Non-goals

- 不把 Sandbox 写成万能安全边界，也不把人工点击等同于正确授权。
- 不实现 IAM、policy engine、approval service、credential broker、sandbox runtime 或 BuildPilot。
- 不提前完成 Article 20 Budget，或 Article 21 Trace / Replay / Failure Taxonomy。
- 不声称某一套字段、风险等级或审批策略是所有组织的统一标准。
- 不提供规避审批、提权、绕过 sandbox 或扩大凭据作用域的方法。
