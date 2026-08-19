---
title: "Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异"
slug: "agent-engineering-04-model-adapter-llm-gateway"
date: "2026-08-20T00:00:00+08:00"
description: "区分 Model Adapter、LLM Gateway 与 Agent Runtime，建立 Streaming、Error、Retry 和 Provider Capability 的保真边界，并明确 Capability Descriptor 仍是未执行的设计提案。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Model Adapter"
  - "LLM Gateway"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 50
weight: 3050
---

> **上一篇**：[Structured Output：让模型输出成为机器可消费的合同]({{< relref "ai-empowerment/agent-engineering-03-structured-output-machine-contract.md" >}})

> 本文资料核对时间：2026-08-20。文中的 Provider 事实来自 OpenAI Responses、Anthropic Messages 及其当前官方 SDK / 产品文档；Cloudflare AI Gateway、Azure API Management AI Gateway tier (preview) 与 Azure API Management all-tier capabilities 文档只用于观察各自 scoped Gateway 能力。本文没有调用 Provider，没有执行 Fake Provider，也没有获得 streaming 或 retry 的 runtime observation。课程中的 Adapter、Gateway、Retry 三分法与 Capability Descriptor 是工程责任模型，不是行业统一标准。

很多团队第一次做多模型接入，会先写出这样一层包装：

```csharp
var client = new ModelClient(endpoint, apiKey);
var result = await client.GenerateAsync(model, messages);
```

接入第二家 Provider 时，看上去只要替换 endpoint、API key 和 model name。方法仍叫 `GenerateAsync`，返回值也可以继续叫 `ModelResult`，于是“Provider 已可替换”似乎成立了。

真正切换后，问题才逐个冒出来：原有的 message role 不一定能原样映射；stream consumer 把一个 content fragment 当成完整结果；结束事件到了，业务却没有检查 refusal 或 incomplete；所有 Usage 被压成一个 total；HTTP 429 一律 sleep 后重试；SDK 已经 retry，上层包装又 retry；最后，一家 Provider 的 stop reason、request id 与错误 body 全部消失在 `ModelCallFailedException` 里。

这不是某一家 SDK “不好用”，也不是所有 Provider 都无法抽象。问题在于：**Provider switch 迁移的是一组合同，而不只是一条 URL。** 一个合格的 Model Adapter 不应假装差异不存在，而应把可归一化的调用职责收口，同时让不可归一化的能力、终态和失败保持可见。

## 换 Provider 时，真正迁移的是哪些合同？

至少在 2026-08-20 核对的 OpenAI Responses 与 Anthropic Messages 当前公开合同中，请求、streaming、终止、Usage、错误和 SDK retry 都存在可点名的差异。两家也都有 streaming、tool input、Usage 与 retry 能力，这说明共同抽象确实存在；但共同能力不等于共同字段，更不等于语义可以无损压成一个字符串。

| 迁移面 | 应重新核对什么 | 不能静默丢掉什么 |
|---|---|---|
| Request / instruction | system / instructions / messages / content blocks 的位置、角色与生命周期 | Provider-specific placement、unsupported combination |
| Stream protocol | event vocabulary、block / item lifecycle、unknown event policy | raw event type、sequence context、stream error |
| Terminal semantics | completed、incomplete、failed、stop reason、refusal、truncation | 原始 reason、未知终止分支 |
| Usage | input / output / cache 等维度，以及 incremental、cumulative 或 final-only 语义 | Provider scope、计数维度与累计方式 |
| Error | network、timeout、rate limit、quota/action、5xx、stream-after-success-status | status、error type、request id、headers、raw body |
| SDK retry | eligible category、内建 owner、attempt 与 backoff surface | SDK language、version scope、实际 attempt metadata |
| Capability / limits | structured output、tool streaming、modalities、context / output limits | native、fallback、unsupported 的差别 |

这张表的用途不是维护一份永久的 Provider 对照百科，而是给迁移建立审查顺序。稳定的是“要问哪些问题”，不稳定的是每家当前的字段答案。

一个很容易被忽略的例子是 SDK 自带 retry。截至 2026-08-20，OpenAI 官方 .NET SDK current main 文档写明，对其列出的 408、429、500、502、503、504 最多自动额外重试 3 次；Anthropic 官方 C# SDK current docs 写明，对其列出的连接错误、408、409、429 与 5xx 默认重试 2 次。**这些数字不构成课程默认值，也不能外推到其他语言、版本、模型或 Provider。** 它们真正提醒我们的是：上层再加 retry 前，必须先知道底层是否已经拥有 retry。

## 抽象模型：Adapter 负责翻译，不负责宣布业务成功

本课程采用下面这条责任链：

```text
Domain Request / Task Policy
        ↓
Provider-neutral Model Request
        ↓
Model Adapter
        ↓
Provider SDK / HTTP / SSE
        ↓
Normalized partial events
  + provider terminal state
  + usage with provenance
  + raw diagnostics / unknowns
        ↓
Parse → Schema → DTO → Domain Validation
        ↓
Final Structured Result
```

这是课程工作模型，不是 Provider 官方架构，也不是行业统一接口标准。它按责任切层，不要求每层都部署成独立进程。

Adapter 适合承担的是：把 provider-neutral request 映射到当前 Provider；解码它的 streaming 状态机；将错误映射到稳定类别，同时保留 raw error、request id 与 headers；交付 refusal、incomplete、stop reason 和 Usage 的保真结果；暴露 retry owner、attempt metadata 与当前 capability scope。

Adapter 不应吞掉的是：领域 DTO、领域不变量和业务成功条件；未经上层策略批准的 prompt rewrite、模型切换、fallback 或无限 retry；Agent step、tool dispatch、workflow checkpoint 与 recovery；未知事件、未知终止原因和 Provider-specific Usage 维度。

可以把接口想成下面这个职责草图：

```csharp
public interface IModelAdapter
{
    CapabilityDescriptor Describe(ModelScope scope);
    IAsyncEnumerable<ModelEvent> StreamAsync(
        ModelRequest request,
        CancellationToken cancellationToken);
}

// ModelEvent 需要区分 Partial、Terminal、Error、Usage，
// 并保留 Provider scope 与 raw diagnostics。
```

这不是可直接投入生产的完整接口。它只强调一件事：Adapter 的输出不是“一个答案字符串”，而是一组仍带来源与状态的调用证据。现实 SDK 可能直接生成 typed object，Gateway 也可能做 request / response transform；这些实现可以减少样板，却不能把 Provider completion 自动升级成 business completion。

## Streaming：Partial、Terminal、Final 是三个阶段

streaming 最常见的误判，是“用户已经看到一句完整的话，所以程序也拿到了完整结果”。对纯文本 UI，partial content 可以边到边显示；对机器合同，partial 只能是 partial。

```text
Provider stream
  ├─ text delta --------------------> display / buffer / observe
  ├─ tool-arguments delta ----------> buffer only
  ├─ usage update ------------------> scoped accumulator
  ├─ unknown event -----------------> preserve / diagnose
  └─ terminal or stream error ------> classify terminal state
                                         ↓
                              aggregate candidate if allowed
                                         ↓
                              Parse → Schema → DTO → Domain
                                         ↓
                              Final Validated Result
```

这条链必须保留三个不同判断：

| 阶段 | 当前允许做什么 | 当前仍未证明什么 |
|---|---|---|
| `STREAM_PARTIAL` | 展示、缓冲、记录进度与 raw provenance | JSON 完整、DTO 可构造、Tool 可执行、任务完成 |
| `PROVIDER_TERMINAL` | 保存 stop / incomplete / refusal / error、final Usage，并在允许时聚合候选 | Schema / Domain 有效、Agent 已停止、外部副作用成功 |
| `FINAL_VALIDATED_RESULT` | 交付通过 Parse、Schema、DTO、Domain 的 typed result | Tool 已授权或执行、workflow 已恢复、业务目标已完成 |

OpenAI 当前 official generated type 把 function call arguments delta 表达为部分字符串；Anthropic 当前 streaming 文档也把 `input_json_delta.partial_json` 描述为需要累积的部分 JSON。两家当前合同共同支持一个保守边界：**tool argument fragment 只能进入 buffer，不能进入 DTO，更不能直接进入 execute。** 对应 block / item 完成后才可以尝试 Parse；Provider terminal 状态允许继续后，才依次进入上一篇建立的 Schema、DTO 与 Domain Validation。

SDK 提供 accumulator helper，不会改变这个责任顺序。helper 可以帮我们聚合，不会把早期 fragment 变成已验证参数。

还要再切开一组概念：`Streaming event != Agent event`。前者描述一次 Provider 调用内部的交付或生成进度；后者描述更高层的 step、tool、state transition 与 stop。把一个 token delta 或 content-block stop 直接记成 Agent step completed，会跳过验证，也会把后续运行时状态写乱。Agent event 的正式模型留到后面的 Agent Loop 与 Workflow 章节。

本文没有执行 Provider call，因此上图表达的是依据当前官方合同形成的处理模型，不是本 transaction 观测到的真实事件顺序、断线恢复或重连行为。

## Error：先定位失败层，再讨论 Retry

把所有失败都包装成 `ModelCallFailedException`，调用方确实更容易 catch；代价是它不知道接下来该等待、换输入、停止、升级，还是回到领域数据检查。

| 失败类别 | 典型信号 | 首个解释者 | 不能直接推出什么 |
|---|---|---|---|
| Network / connection | SDK transport error | transport owner | 自动重放安全 |
| Timeout / unknown outcome | connect、read 或 overall timeout | transport owner + upper policy | 服务端一定没有处理 |
| Rate limit | Provider 分类后的 429 与 headers | Provider error mapper | 所有 429 都同样处理 |
| Quota / billing / action | Provider error code / message | operator / business policy | 等待后会自行恢复 |
| Transient Provider error | Provider-scoped 5xx / overload type | transport policy | 所有 5xx 必须 retry |
| Stream error after success status | typed stream error event | Adapter decoder | 初始 HTTP success 等于最终成功 |
| Refusal | output / terminal semantics | task policy | 这是 transport failure |
| Truncation / incomplete | terminal status / reason | task policy | 重放同一请求会解决 |
| Parse / Schema / DTO / Domain | 本地 first-failure stage | structured-output / domain layer | 网络重试就是修复 |

429 是最直接的反例。OpenAI 当前错误文档同时列出临时 rate limit 与余额、配额或组织限制等需要不同处置的情况。因此，`status == 429` 只能成为分类入口，不能自动映射成“sleep 后再试”。必须继续读取当前 Provider 的 error type、message、headers 与限制语义。

另一个反例来自 Anthropic 当前 streaming contract：SSE 可以在初始 HTTP 200 后交付 error event。这证明当前官方合同存在“成功 status 后仍有 stream error”的分支，不证明本文实际观测过它。它提醒 Adapter：stream 的终态不能只看最初的 HTTP status，更不能在收到第一批 partial 后提前记成功。

统一异常类型不是错误，它可以作为稳定入口；危险的是在归一化时丢掉决定动作所需的原始证据。也不能反向写成“所有 4xx 永不 retry”或“所有 5xx 都应 retry”。先保留失败层与 Provider cause，Retry 才有可靠输入。

## Retry：同一句“再试一次”，可能是三种不同动作

本课程按“重复了什么、改变了什么、依赖什么状态”区分三种动作：

| 动作 | 重复或改变的对象 | 适用候选 | 不应偷偷做什么 |
|---|---|---|---|
| Transport retry | 重放同一逻辑 Provider request | eligible network、rate、transient error | 改 prompt、改 Schema、换模型、掩盖 unknown request state |
| Semantic / business retry | 创建新的业务 attempt，可能改变 prompt、input、Schema、模型或预算 | refusal、truncation、Parse / Schema / Domain failure，且 policy 明确允许 | 冒充底层自动 retry，复用旧 attempt 的成功标签 |
| Recovery | 从 durable workflow state 核对已完成 step / side effect 后继续或补偿 | crash、restart、long-running interruption、unknown side-effect state | 只靠再次调用模型恢复完整 workflow |

这三个名字是课程 working model；Provider 官方文档并未统一采用同一套术语。现实 orchestrator 也可能在同一组件中承载三种机制，但部署在一起不代表证据语义相同。

自动 transport retry 至少要依次通过下面七个 Gate：

1. **分类明确**：当前错误属于该 Provider / SDK 文档列出的 retry-eligible category，而不是 quota、refusal、Schema 或 Domain failure。
2. **所有者唯一**：明确由 SDK、Gateway、Adapter 或 Application 中哪一层 retry，避免 `SDK × Gateway × App` 的嵌套次数乘法。
3. **请求状态明确**：没有把 terminal result、已接受的调用意图或 unknown outcome 当成“请求未发生”。
4. **重放安全**：操作语义已知可 replay，或者能确认原请求未被应用；不能把“调用的是模型”当作天然幂等。
5. **Provider 指导优先**：优先遵守当前合同的 `Retry-After` 等指导；否则才考虑有 jitter 的 bounded exponential backoff。
6. **预算有界**：限制额外 attempt、总 elapsed time、token / cost 与队列等待。本篇只要求这些维度可见，完整 Budget policy 留到后文。
7. **停止可审计**：命中不可重试类别、deadline / budget、unknown state 或 attempt 上限时停止并升级，而不是继续隐藏失败。

timeout 特别容易暴露第四项问题：客户端没拿到结果，不等于服务端一定没处理。当前 Evidence 没有建立 OpenAI / Anthropic create API 的跨 Provider exactly-once 保证，也没有证明 timeout 后重放默认安全。RFC 9110 对非幂等请求的自动重试同样要求已知重放安全语义，或能够确认原请求未被应用。

另一方面，官方 SDK 确实提供 bounded automatic retry，这反驳“所有模型生成请求都绝不能自动 retry”。正确结论不是绝对禁止或绝对允许，而是：分类、ownership、request state、replay safety、Provider guidance、budget 和 stop condition 必须同时可审查。

## Adapter、Gateway 与 Agent Runtime：按责任切，不按产品名猜

当团队开始集中管理多个 Provider，常会引入 LLM Gateway。它也可能做 routing、credential、rate limiting、retry、fallback 与 telemetry，于是两个问题随之出现：Application 内是否还需要 Adapter？Gateway 是否已经等于 Agent Runtime？

本课程使用下面的责任切分：

| 组件 | 课程中的主要责任 | 状态与作用域 | 不能仅据此证明什么 |
|---|---|---|---|
| Model Adapter | request translation，解码 stream / terminal / error / Usage，暴露 capability 与 raw diagnostics | 一个 Provider integration、一次调用的短期状态 | Domain truth、完整 Gateway、Agent loop |
| LLM Gateway | credential、routing、rate / quota、retry / fallback policy、audit / telemetry 等集中 traffic concern | 跨应用、Provider、租户或流量策略 | 完整 Agent step / state / stop / recovery |
| Agent Runtime | model call、tool dispatch、loop、state、stop、checkpoint / recovery | 长于一次 model call 的 execution state | Gateway 流量治理本身 |

这是责任边界，不是强制部署图。Adapter 可以在应用进程、服务模块或 Gateway 内；Gateway 也可以和 Runtime 属于同一个产品。当前 Cloudflare AI Gateway overview 直接列出 Provider / model 接入、analytics / logging、rate limiting、request retry 与 model fallback；Azure API Management AI Gateway tier (preview) 的 public-preview overview 直接列出集中 endpoint、backend routing、credentials / policies、request / token limits、telemetry，以及 model / MCP tool 管理。Azure API Management 的 all-tier capabilities 页面还列出 authentication、backend load balancing、token limits / quotas、observability 与 model / agent / tool governance，但能力可用性依 service tier 而异，部分子能力另标 preview。它们是不同 scope 下的不同能力集合，不能彼此补成产品全集。这恰好说明：**不存在可以只凭“Gateway”产品名推出的唯一行业边界。**

因此，`Gateway != Agent Runtime` 的精确含义不是“所有 Gateway 都没有 Agent 功能”，而是：上面逐产品、逐 scope 映射的 traffic-plane evidence，本身不足以证明 goal、step、tool dispatch、state、stop、checkpoint 与 recovery 已闭合。一个产品可以同时提供两类能力，但仍要分别验证它的 Gateway responsibility 与 Runtime responsibility。

同理，Gateway 做了 request transform，也不一定让 Application 不再需要 Adapter contract。只要上层仍依赖 partial / terminal、Usage provenance、raw diagnostics 与 capability scope，这些翻译责任就需要有明确 owner；owner 可以位于 Gateway，不能凭“已经过网关”自动消失。

## Capability Descriptor：把不可归一化差异暴露成决策输入

如果 Adapter 只返回 `SupportsModel = true`，上层仍不知道当前 Provider 是否原生支持 strict structured output，能否 stream tool arguments，Usage 怎样累计，谁拥有 retry，以及遇到未知 event / stop reason 应怎样处理。

下面是本课程的**设计提案**，状态为 `PROPOSAL / NOT_EXECUTED`。它没有实现，没有运行 Fake Provider，也不是官方统一 schema。

```text
CapabilityDescriptor
  scope:
    provider / api / model / version
  instruction_and_message_model
  structured_output:
    native_strict | json_only | prompt_fallback | unsupported
  stream_text
  stream_tool_arguments + fragment_lifecycle
  usage_delivery + dimensions
  terminal_semantics
  retry_owner + eligible_classes + attempt_metadata
  limits_and_modalities
  unknown_event_policy
```

调用前的 negotiation 也只需要先建立三种明确结果：

| 结果 | 含义 | 上层动作 |
|---|---|---|
| `NATIVE` | 当前 scoped contract 原生满足任务要求 | 保存 scope 与 diagnostics 后调用 |
| `EXPLICIT_FALLBACK` | 只能使用更弱或语义不同的替代路径 | 由上层显式批准，不把它写成 native guarantee |
| `UNSUPPORTED` | 当前 scope 不满足必需能力 | fail closed、换候选或返回不可执行 |

简单的单 Provider 应用可以用静态配置固定这些能力，现有 SDK 或 Gateway 也可能提供自己的 discovery。这个反例说明 Descriptor 不必在所有系统里动态实现，更不证明课程提案优于现有产品。提案真正要守住的是：能力缺口不能被一个模糊的“兼容”静默吞掉。

如果未来要验证这个设计，可以设计一个 Fake Provider，输入文本增量、tool argument fragments、rate-limit 429、quota/action 429、terminal truncation、unknown event 与 stream error，再检查 fragment 是否只进入 buffer、terminal 后是否才进入 Parse / Schema / DTO / Domain、retry owner 是否唯一、unsupported 是否 fail closed。**当前没有执行这项实验，没有 Expected / Observed 结果，也不能宣称任何检查已经 PASS。**

## 怎样审查一个“可切换 Provider”的模型层？

下一次看到一个 `IModelClient`、Adapter 或 Gateway 方案，可以按下面的顺序复核：

1. request、instruction 与 messages 怎样映射到当前 Provider / API / model / version？
2. partial、terminal 与 final validated result 是否由不同类型或状态表达？
3. tool argument fragment 是否只缓冲，完成后才进入 Parse / Schema / DTO / Domain？
4. unknown event、stop reason、Usage dimension 与 raw diagnostics 是否被保留？
5. error 属于 network、timeout、rate、quota/action、transient、refusal、truncation，还是 Parse / Schema / Domain？
6. transport retry owner 是否唯一，request state、replay safety、budget 与 stop 是否可审查？
7. semantic retry 与 recovery 是否创建自己的 attempt / state，而不是冒充 transport retry？
8. Gateway traffic policy 与 Agent Runtime execution state 是否分别有证据？
9. capability 是 native、显式 fallback 还是 unsupported？scope 是否随结果返回？
10. 当前结论来自 official contract、runtime observation，还是 design proposal？

上一篇负责让候选输出经过 Parse、Schema、DTO 与 Domain；本篇负责在候选进入那条链之前，保留 Provider 的 stream、terminal、error 与 Usage evidence，并决定它是否可以形成候选。[下一篇]({{< relref "ai-empowerment/agent-engineering-05-function-calling-tool-use.md" >}})才会继续讨论模型怎样表达“我要行动”的 Function Calling / Tool Use contract；本篇到“tool argument fragment 不能越过 validation 直接执行”为止。

## Learning Check

1. 为什么替换 endpoint、API key 和 model selector，不足以证明 Provider migration 已完成？
2. Model Adapter 应该收口哪些调用职责，又必须保真暴露哪些信息？
3. UI 已显示一句完整文本时，为什么 tool argument fragment 仍不能进入 DTO 或 execute？
4. 为什么 HTTP 429 不能自动映射成“等待后重试”？
5. transport retry、semantic / business retry 与 recovery 分别重复或改变了什么？
6. Gateway 已提供 routing、retry 和 telemetry，为什么仍不能仅据此宣布 Agent Runtime 完整？
7. Capability Descriptor 为什么建议返回 `NATIVE / EXPLICIT_FALLBACK / UNSUPPORTED`？为什么本文不能宣称它已经工作？
8. 截至 2026-08-20 核对的 OpenAI .NET 与 Anthropic C# retry 默认值，为什么不能成为课程默认次数？

### 参考思路

1. 迁移面还包括 request / message、stream、terminal、Usage、error、SDK retry 与 capability；本篇证据只覆盖两家当前公开合同。
2. 收口 Provider request / stream / error / Usage translation；保留 raw diagnostics、terminal、capability scope 与 retry ownership，不吞掉领域 DTO、Agent state 或 recovery。
3. partial 不证明 block / item 或 stream 已 terminal，更不证明 Parse / Schema / DTO / Domain 已通过。
4. 同一状态可能对应临时 rate limit 或 quota/action 等不同原因；即使 eligible，仍要通过 ownership、request state、replay safety、budget 与 stop Gate。
5. 分别是重放同一 request、创建新的业务 attempt、从 durable workflow state 恢复；这是课程 working model，不是 Provider 统一术语。
6. traffic governance evidence 不证明 goal、step、tool、state、stop、checkpoint 与 recovery；产品可以组合职责，但证据仍需分开。
7. 它让能力缺口显式化；但当前只是一项未实现、未执行、无 runtime evidence 的设计提案。
8. 两组值绑定不同 Provider、SDK language、current main/docs、eligible categories 与核对日期，不能外推到其他语言、版本或 Provider。

## 最短结论

`可替换性不是把 Provider 名字藏起来，而是让差异有明确归属、失败有明确语义、能力缺口有明确出口。`

## 参考资料

- [OpenAI：Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses)
- [OpenAI：API error codes](https://developers.openai.com/api/docs/guides/error-codes)
- [OpenAI：Rate limits](https://developers.openai.com/api/docs/guides/rate-limits)
- [OpenAI：Official .NET library](https://github.com/openai/openai-dotnet)
- [Anthropic：Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming)
- [Anthropic：Handling stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons)
- [Anthropic：API errors](https://platform.claude.com/docs/en/api/errors)
- [Anthropic：Rate limits](https://platform.claude.com/docs/en/api/rate-limits)
- [Anthropic：C# SDK](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp)
- [Cloudflare：AI Gateway](https://developers.cloudflare.com/ai-gateway/)
- [Microsoft Azure：Azure API Management AI Gateway tier (preview)（public preview）](https://learn.microsoft.com/en-us/azure/api-management/ai-gateway-overview)
- [Microsoft Azure：AI gateway in Azure API Management（all tiers；能力依 service tier 而异，部分子能力为 preview）](https://learn.microsoft.com/en-us/azure/api-management/genai-gateway-capabilities)
- [RFC 9110：9.2.2 Idempotent Methods](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2)
