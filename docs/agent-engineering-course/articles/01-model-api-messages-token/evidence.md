# Article 01 Claim Skeleton & Evidence Plan

- Lifecycle Status：`PLANNED`
- Evidence Status：`BLOCKED`
- Research Status：`NOT_RESEARCHED`
- Claim Count：`12`
- Confirmed Claim Count：`0`
- Evidence Card Count：`0`

> 以下条目只是待验证 Claim Skeleton，不是课程结论。A2 必须根据一手来源收窄、拆分、重写或删除它们；当前一律不得进入正文。

## Claim Register Skeleton

| Claim ID | 待验证 Claim / Hypothesis | Priority | Related RQ | Research Status | Evidence Status |
|---|---|:---:|---|---|---|
| 01-C01 | Model、Provider、Model API、SDK 与 Application 可以通过公开职责边界区分，但具体命名和承载关系可能依 Provider 而变。 | High | RQ-01 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C02 | 一次最小 Model Call 可以从 Application 可观察边界拆成 request construction、client serialization、Provider API interaction、generation result 与 application handling；Provider 内部执行不得无证据展开。 | High | RQ-02 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C03 | Messages / Input 是当前 API 请求的结构化输入表达；重复发送历史 Messages 不自动证明 Model 拥有跨调用 Long-term Memory。 | Critical | RQ-03 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C04 | Message role 集合与语义至少部分属于 Provider API Contract，不能预设为全行业统一标准。 | High | RQ-04 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C05 | Token 是 Model API 输入、输出、usage accounting 与 context limitation 的重要工程单位；字符、单词和 Token 不能直接按固定比例互换。 | High | RQ-05 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C06 | Context Window 通常需要按 Token / Model Contract 解释，不能直接等同于 Messages 数量、字符数、文件大小或长期 Memory。 | High | RQ-06 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C07 | Model API Response 需要区分 generated content、response envelope、usage、finish / stop metadata 与 status / error；字段和语义依 Provider / Version 变化。 | High | RQ-07 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C08 | Streaming 的最低可迁移定位应落在 Application 接收 / 消费输出的接口模式；不能据此宣称模型隐藏推理过程被公开。 | Medium-High | RQ-08 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C09 | Model、Provider、endpoint / deployment 与 API schema 不是天然一一对应关系；具体映射必须查看 Provider Contract。 | High | RQ-09 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C10 | SDK 是开发者侧客户端封装，API 是 Provider 对外契约；二者边界及例外需要按 Provider 实际文档确认。 | High | RQ-01 / RQ-02 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C11 | Transport / HTTP、Auth、Rate Limit、Schema、Provider、Refusal / Stop、Application Parse 与 Wrong Answer 属于不同候选 Failure Layer。 | High | RQ-11 | `NOT_RESEARCHED` | `BLOCKED` |
| 01-C12 | 单一 Provider 的 C# 示例只能证明该版本 Contract 与教学路径，不能自动升级为 Industry Definition。 | Medium | RQ-10 | `NOT_RESEARCHED` | `BLOCKED` |

## Evidence Plan

| Claim ID | Primary Evidence Category | Cross-check / Counter-evidence | Version Record Needed | Experiment Candidate |
|---|---|---|---|---|
| 01-C01 | `OFFICIAL_DOC` + `OFFICIAL_API_REFERENCE` | 至少一个其他 Provider 的官方术语 / contract | Provider、文档日期、API version | No |
| 01-C02 | `OFFICIAL_API_REFERENCE` + `PROVIDER_SDK_DOC` | raw HTTP 与 SDK 表达是否一致 | endpoint、SDK version、request schema | Serialization only if needed |
| 01-C03 | `OFFICIAL_API_REFERENCE` | Session / Memory 官方资料的反向边界 | message schema、API version | No by default |
| 01-C04 | 多家 `OFFICIAL_API_REFERENCE` | 查找 OPEN_SPEC 或明确冲突角色 | Provider、role support、retrieved date | No |
| 01-C05 | `OFFICIAL_DOC` + usage reference | tokenizer / billing 文档与模型卡 | model version、tokenizer / pricing date | Usage observation if needed |
| 01-C06 | official model / context documentation | 不同模型窗口与计量说明 | exact model / version | Token count fixture only if needed |
| 01-C07 | response schema / SDK response type | 跨 Provider metadata 对照 | API + SDK version | No by default |
| 01-C08 | streaming API reference / event schema | 普通 response 与 stream response 对照 | protocol、event schema、SDK version | Minimal stream capture if needed |
| 01-C09 | model / endpoint / deployment docs | 至少两个映射反例或限制条件 | region / deployment / model alias | No |
| 01-C10 | official API + SDK architecture docs | raw HTTP sample 与多个 language SDK | SDK version、API version | No |
| 01-C11 | official error guide / status reference | SDK exception mapping 与 refusal / stop docs | status code、error schema、SDK version | Failure fixture deferred to 04 / 21 |
| 01-C12 | `COURSE_DESIGN_REVIEW` + chosen official sample | 检查是否误把 provider-specific fields 写成通用字段 | sample commit / SDK version | Compile example may be decided later |

## Provider Evidence Strategy

- `Primary Provider`：A2 优先评估 OpenAI 官方 API + C# / .NET SDK 是否足以承担主示例。
- `Counter-check Providers`：Anthropic / Google 只用于检查概念稳定性和 Provider-specific 差异。
- `Rule`：不要求三家对称覆盖；只为每个通用化 Claim 找到足够反查。
- `Rule`：Provider-specific field、role、endpoint、model selector、usage 与 error 均绑定版本。
- `Rule`：官方 API Contract 优先于 SDK convenience API；SDK 代码不能单独证明服务端内部实现。

## Evidence Card Creation Rule

A2 开始后，每个保留的 Claim 至少建立一张正式 Evidence Card，并填写：Source、Retrieved At、Version Scope、Observation、Counter-evidence Searched、Proves、Does Not Prove 与 Limitations。在此之前不得把 `BLOCKED` 改为 `CONFIRMED` 或 `PARTIAL`。

## A1 Stop Line

当前没有正式 Evidence Card、Research Finding、实验结果或可进入正文的 Claim。下一步只能是 A2 Evidence-first Research。
