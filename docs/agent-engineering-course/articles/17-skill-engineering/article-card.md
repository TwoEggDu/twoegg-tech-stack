# Article 17 Card｜Skill Engineering

## Canonical identity

- ID: 17
- Title: Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt
- Part: III｜Agent 的信息、状态与知识
- Weight: M / Standard Core Lesson
- Optional: NO
- Mode: NORMAL_ARTICLE
- Required Lab: NONE
- Article Type: PRINCIPLE
- Concept Maturity: ENGINEERING

## Course position

- Upstream: Article 12—13 Context / Context Debugging；Article 16 Knowledge Base / RAG。
- Relevant prior boundaries: Article 02 Prompt；Article 06 Tool Runtime；Article 08 Agent Loop；Article 10 Workflow；Article 15 Memory。
- Downstream: Article 18 Evidence Contract；19 Permission；22 Eval；24—27 Harness；37 DSH mapping；42 BuildPilot capability design。

## Problem statement

Agent 不只需要“知道什么”，还需要在任务出现时载入“这类任务通常怎样做”。把所有领域方法长期堆进系统 Prompt 会扩大默认上下文、模糊适用范围，也难以单独测试、版本化和退役。本篇建立 Skill 的工程边界：可发现、按需加载、可验证的领域方法包，而不是行业统一对象或另一层无限 Prompt。

## Human-approved questions

1. Skill 是什么，解决什么问题？
2. Skill、Prompt、Tool、Workflow、Agent 与 Knowledge Base 的边界是什么？
3. 为什么 Skill 是按需加载的领域方法，而不是再堆一层 Prompt？
4. Skill 的发现、选择、加载、执行和结果验证过程是什么？
5. 输入、输出、依赖、权限、适用范围与失败语义如何表达？
6. Skill 如何减少 context pollution？
7. 通用 Agent + Skill 与专用 Agent 的职责边界是什么？
8. 什么时候应该或不应该创建 Skill？
9. Skill 如何测试、版本化、审查和回归验证？
10. BuildPilot 中哪些能力属于 Skill，哪些留在 Harness、Workflow、Tool Runtime 或 Policy？

## Teaching and evidence contract

- 原理篇：问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界。
- 至少一个贯穿案例：task -> need decision -> discovery -> load -> domain method -> Tool / Workflow -> verify -> end Skill context。
- 产品事实使用 primary / official sources，至少比较两个真实实现，不把单一实现写成行业标准。
- BuildPilot 只用 PROPOSAL 语态；不虚构实验、成功率、Token 节省、准确率、延迟、质量提升或生产收益。
- canonical 不要求 Lab。未运行 fixture 保持 PROPOSAL / NOT_RUN / Observed Result ABSENT。

## Non-goals

- 不定义统一 Skill 格式或触发算法。
- 不实现 BuildPilot Runtime。
- 不提前写完 Article 18、19、22、24—27、37 或 42。
- 不把正文写成只有协议和审计条款的规范。
