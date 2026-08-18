# Article 00 Research Questions

- Lifecycle Status：`PLANNED`
- Research Status：`NOT_STARTED`
- Evidence Status：`BLOCKED`
- M0 Rule：只定义问题，不在本文件回答问题

| RQ | Research Question | 目标证据类别 | 当前状态 |
|---|---|---|---|
| RQ-01 | LLM / Model / AI Application 的最低共同定义是什么？ | `OFFICIAL_DOC` / specification | `NOT_STARTED` |
| RQ-02 | Copilot 是否存在可引用的统一架构定义？如果没有，应怎样限定表述？ | 多个一手产品定义与反例 | `NOT_STARTED` |
| RQ-03 | Agent 有哪些相对稳定、可用于工程判断的特征？ | specification / paper / official SDK | `NOT_STARTED` |
| RQ-04 | Agentic 为什么应该作为描述性术语，而不是严格产品层级？ | 一手术语用法与生态差异 | `NOT_STARTED` |
| RQ-05 | Agent Runtime 可以稳定抽象出哪些责任，哪些只是具体框架选择？ | official SDK / pinned source / runtime observation | `NOT_STARTED` |
| RQ-06 | Harness 是行业标准术语，还是本课程的工程抽象？正文应怎样明确这个边界？ | 多生态术语调查与课程设计依据 | `NOT_STARTED` |
| RQ-07 | Host / Product 与 Runtime 应怎样区分，哪些职责不能仅凭产品形态推断？ | official architecture docs / public product facts | `NOT_STARTED` |
| RQ-08 | Prompt / Context / Tool / Skill / Workflow / Memory / RAG 在 00 中解释到什么深度，既能定位又不抢后文？ | canonical dependency + glossary consistency review | `NOT_STARTED` |
| RQ-09 | Claude Code、Codex、DeepSeek Harness 的哪些公开事实可以用于举例，哪些内部实现不能推测？ | official public docs；DSH 另需 pinned source | `NOT_STARTED` |

## M1 研究输出要求

- 每个 Research Question 拆成可核验 Claim。
- 每个 Claim 建立独立 Evidence Card，并填写 `Proves / Does Not Prove`。
- 产品示例只使用公开可确认事实；没有证据时删去或保持 `BLOCKED`。
- Harness 的课程抽象必须显式标注，不能伪装成行业统一定义。
- 本篇不做 DSH 源码研究；需要源码判断的内容留给 28—37。
