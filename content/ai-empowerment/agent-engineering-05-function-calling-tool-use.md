---
title: "Function Calling 与 Tool Use：模型如何表达行动意图"
slug: "agent-engineering-05-function-calling-tool-use"
date: "2026-08-20T00:00:00+08:00"
description: "区分 Function Calling、Tool Schema、Tool Choice、Call ID、Arguments 与 Tool Result，明确模型只表达行动意图，执行、授权、证据与 Agent Loop 仍由 Host 和后续机制负责。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Function Calling"
  - "Tool Use"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 60
weight: 3060
---

> **上一篇**：[Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异]({{< relref "ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md" >}})

> 资料核对时间：2026-08-20。依据为 OpenAI Responses 与 Anthropic Messages 当前官方合同。Provider Calls / Tool Execution=`NONE / NONE`，runtime=`UNVERIFIED`。Calculator=`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`；两家 trace=`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`。

假设 Application 收到 `deleteFile` 和一个文件路径便直接调用同名函数。它省掉了关键判断：Tool 是否注册？参数是否完整、有效、获准？执行是否发生？Result 来自哪里？

对于 **client-executed tools**，Function Calling 让模型表达结构化行动请求；Host / Application 仍决定怎样处理、是否执行，以及怎样把关联结果带入下一次模型请求。因此：

```text
Tool Call != Executed
Schema Valid != Authorized
Tool Result by itself != Evidence
One Tool Use is not sufficient evidence of an Agent Loop
```

## 从结构化结果到行动意图

Structured Output 与 Tool Call 都可以机器读取，但职责不同。业务 Structured Result 是候选数据，Application 可以展示、存储或拒绝；Tool Call intent 则请求 Host 考虑某项能力及候选参数，没有替 Host 作出执行决定。

下面是课程概念图，不是任何 Provider 的 payload：

```text
BUSINESS STRUCTURED RESULT
  -> candidate data for Application handling

TOOL CALL INTENT
  -> requested capability + argument candidate
  -> waiting for Host decision
```

对 client-executed tools，最小责任链是：

```text
Provider-scoped tool definitions
  -> model response contains tool-call intent
  -> Host decides route / reject / optional execute
  -> Host returns correlated tool-result content
  -> next model request
  -> text or additional tool calls
```

这是责任链，不是统一消息格式。OpenAI 还有 built-in tools，Anthropic 还区分 server-executed tools；这些能力可以由 Provider 基础设施执行，反驳“所有 Tool 都由应用进程执行”。但对 client-tool flow，model call 仍不是应用副作用已发生的证明。

SDK Tool Runner 可以自动承载部分链路；它改变 owner 位置，不会消除 correlation、validation 或 authorization，也不要求每个 Application 手写循环。

## Tool Schema 与 Tool Choice：模型看见的是能力合同

两家官方合同都向模型提供名称、说明、输入结构与 Tool choice；具体字段不同。

| Contract Surface | OpenAI Responses 当前文档 | Anthropic Messages 当前文档 |
|---|---|---|
| Definition | `type`、`name`、`description`、`parameters`，可带 scoped `strict` | `name`、`description`、`input_schema`，也有 scoped strict tool use |
| Choice | `auto`、`required`、forced function、allowed tools、`none` | `auto`、`any`、specific `tool`、`none` |
| Arguments | JSON-encoded `arguments` | object `input` |

可迁移的是 model-visible capability、input shape 与 selection constraint。字段与兼容范围按 Provider / API / model / version 核对。Tool choice 可以约束选择，不能授予本地权限。

Calculator 对照只展示 contract surface：

> Fixture Status：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`

| Fixture | Model-visible input | 静态能说明什么 | 不能说明什么 |
|---|---|---|---|
| `Calculator-A` | `calculate(expression: string)` | 表达较开放 | 错误率或运行效果 |
| `Calculator-B` | `calculate_binary(operation: enum(add, subtract), left: number, right: number)` | type / required / enum 收窄参数空间 | selection、adherence 或质量提升 |

本轮没有 prompt、model、sample、Expected、Observed 或原始输出。只能说两个 contract surface 不同；任何效果结论都要另做 Provider A/B observation。

Scoped strict mode 说明某些 Provider scope 有更强 schema guarantee；它仍不会生成 registry、领域事实、authorization 或副作用。

## 两家 documented trace：共同的是 correlation

Host 必须保存 call/result correlation，并让 Result 进入下一次模型请求。两家 current contract 都展示了这条往返，但 placement 与 identifier 不同。

### OpenAI Responses

> Trace Status：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`

```text
request with function tools
  -> model output: function_call(call_id, name, arguments)
  -> application routes / rejects / optionally executes
  -> next input: function_call_output(call_id, output)
  -> next response
```

`call_id` 关联 model output 中的 `function_call` 与后续 input 中的 `function_call_output`。这证明当前 Responses 文档里的 correlation / continuation shape，不证明本地 credentials、network、Tool side effect、output truth 或 final answer quality。

### Anthropic Messages

> Trace Status：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`

```text
request with tools
  -> assistant content: tool_use(id, name, input)
  -> application routes / rejects / optionally executes
  -> next user message: tool_result(tool_use_id, content)
  -> next assistant response
```

这里是 assistant `tool_use.id` 对应下一条 user message 中的 `tool_result.tool_use_id`。它证明当前 Messages 文档里的 content-block placement 与 correlation，同样不是本地 roundtrip。

共同抽象是 `model call -> Host decision -> correlated result -> next request`，不是统一 payload。OpenAI 使用 output / input item，Anthropic 使用 assistant / user content block；课程抽象连接职责，不抹掉协议差异。

## Arguments 的五个状态与 multiple calls

Streaming 让“参数是什么”继续分层。看到一段像 JSON 的文本，不等于 item / block 已完成，更不等于 Host 已验证和授权。

```text
ARGUMENT_FRAGMENT
  -> COMPLETED_ARGUMENTS_CANDIDATE
  -> VALIDATED_ARGUMENTS
  -> AUTHORIZED_ACTION
  -> EXECUTED_RESULT
```

1. fragment 只按 call / item / block 缓冲；
2. Provider completion 后才得到 candidate；
3. candidate 再进入 Parse / Schema / DTO / Domain；
4. validation 后仍有 authorization；
5. operation 返回后才有 executed result，本文没有这类 observation。

OpenAI 文档展示 arguments delta / done / completed item；Anthropic standard streaming 的 `input_json_delta.partial_json` 是 partial string，block 完成后才得到 final `tool_use.input` object。Accumulator helper 不能提前升级 fragment。

Anthropic fine-grained tool streaming 是 scoped counterexample：当前文档允许 invalid / incomplete JSON，要求 parse guard，解析失败不得执行。不能外推到 standard buffered mode 或 OpenAI strict，也不是本地 observation。

一个 model response 还可以包含多个 calls。每个 call 都要有独立 id、buffer、Host decision 与 correlated result：

```text
one model response
  ├─ Call A -> Buffer A -> Decision A -> Result A
  └─ Call B -> Buffer B -> Decision B -> Result B
```

“多个”不等于 Host 必须并行。是否 concurrent、sequential 或混合，取决于 Provider control surface、Tool semantics 与 Host 决策。本文不主张实际 parallel 频率、顺序或最佳调度策略。

## Host 的 fail-closed 决定权

Completed candidate 到达 Host 后，unknown、invalid 与 unauthorized 都应停在副作用之前：

```text
call intent
  -> registered name?       no -> reject / error result
  -> completed arguments?   no -> reject / error result
  -> parse / validate?      no -> reject / error result
  -> authorized?            no -> reject / approval boundary
  -> optional execute
```

OpenAI 当前 routing sample 对未注册 function name 走 unknown-function error；Anthropic fine-grained streaming 文档要求 invalid JSON 不得执行 Tool，而应返回 error result。这支持 Host seam，不证明本轮模型生成过 unknown name，也不证明 error 后模型一定 recover。

> `deleteFile` Example Status：`SYNTHETIC NEGATIVE / NOT_EXECUTED`

若 `deleteFile` 不在允许 registry，Host 应按 unknown fail closed；即使 name 存在、参数符合 scoped schema，当前 authorization 仍可能拒绝执行。因此 `Schema Valid != Authorized`。

这个负例没有被发送给模型，也没有删除文件。本文只指出 seam：Article 06 才展开 Tool Runtime 的 Canonicalize、Validate、Policy、Execute、Timeout、Retry、Result validation 与 Trace；Article 19 才处理 Permission、Approval 与 Sandbox。

## Tool Result：消息闭环不等于事实闭环

Host 按 Provider contract 回注 correlated Result。这个 envelope 解决 correlation 与 continuation，不自动解决真实性。

OpenAI `function_call_output` 可以承载 string / JSON-like content；Anthropic `tool_result.content` 可以承载 string 或多种 content blocks，并可表达 error。Generic contract 没有统一要求 provenance、retrieval time、claim mapping 或 independent verification。因此本篇只采用窄命题：

```text
Generic Tool Result by itself != Evidence
```

特定 server/search tool、document block 或业务 Tool 可以携带 citation、source metadata 或 verified artifact；这反驳“Tool Result 永远不能成为 Evidence”。当前结论保持 `PARTIAL / COURSE WORKING BOUNDARY`，完整 Evidence Contract 留给 Article 18。

## 为什么一次 Tool Use 仍不足以证明 Agent Loop？

提供 definitions、收到 call、由 Host 决定、回注 Result，再取得文本或更多 calls，是 Agent 可能使用的机制；但一次往返只证明一次 Tool Use：

```text
request -> one call -> one result -> answer
```

它不能单独证明课程定义下围绕目标推进、多步行动与反馈、runtime state 和 stop semantics 已闭合。准确表述是：

> Tool Use 可以属于 Agent Loop；一次 Tool Use 不足以证明课程定义下的 Agent Loop。

这保持 `PARTIAL / COURSE WORKING BOUNDARY`，不主张行业唯一分类。Runner 或 server-side loop 可以持续多轮，生态也可以称其为 agentic；一次 call/result 仍不能证明完整 Loop。

边界到此停住：[Article 06]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}})负责 Tool Runtime 与独立 failure-injection evidence；07 负责 MCP transport / discovery / interoperability；08 正式定义 Turn、Step、Decide、Act、Observe、state 与 stop；18 建立 Evidence Contract；19 建立 Permission、Approval、Human-in-the-loop 与 Sandbox。本篇不展开 timeout、MCP、multi-step、permission implementation 或 error recovery。

## 怎样审查一条 Tool Use 链？

1. 当前是 client-executed tool，还是 Provider-managed built-in / server tool？
2. definition / choice 的 Provider、API、model、version scope 是什么？
3. 当前对象是 fragment、candidate、validated、authorized，还是 executed？
4. identifier 怎样关联 Result 并跨入下一次 request？
5. multiple calls 是否各自保留 buffer、decision 与 result，而没有把“多个”写成“必须并行”？
6. unknown、invalid 与 unauthorized 是否都停在副作用之前？
7. Result 只完成消息回注，还是另有 provenance / verification contract？
8. 当前证据只证明 Tool Use，还是已经由后续机制证明 Agent Loop？

检查的核心是阻止语义提前升级：fragment 不是 candidate，call 不是 execution，result 也不是天然 Evidence。

## Learning Check

1. 模型返回 client-tool `deleteFile` call，文件是否已经删除？
2. Tool arguments 符合 scoped schema，是否意味着业务允许执行？
3. Calculator-B 多了 enum 与 typed operands，本文能否说它让模型更准确？
4. OpenAI `call_id` 与 Anthropic `tool_use.id -> tool_use_id` 的共同职责是什么？为什么不能合成统一 payload？
5. 收到 arguments delta 后，为什么不能立即 Parse 或 execute？
6. 同一 response 有两个 calls，Host 是否必须并行？
7. Tool Result 位于专用 result block，为什么仍不能自动叫 Evidence？
8. 一次查天气的 call → result → answer，是否证明课程定义下的 Agent Loop？

### 参考思路

1. 没有；还要经过 registry、completion、validation、authorization 与 execution。本例未执行。
2. 不意味着；schema、domain、authorization 是不同判断。
3. 不能；只有 contract 差异，没有 A/B observation。
4. 都关联 call 与下一次 request 中的 Result；identifier、placement、shape 不同。
5. delta 仍是 fragment；completion 后才进入后续 gate。
6. 不必须；每个 call 独立保存 id、buffer、arguments、decision、result。
7. generic envelope 不保证 provenance / verification；rich result 可有额外合同。
8. 不足以证明；一次往返不证明 multi-step、runtime state 与 stop closure。

## 最短结论

`Function Calling 让模型表达行动意图；是否执行仍由 Host 决定，回注 Result 也仍不等于 Evidence 或 Agent Loop。`

## 参考资料

- [OpenAI：Function calling](https://developers.openai.com/api/docs/guides/function-calling)
- [Anthropic：Define tools](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools)
- [Anthropic：Handle tool calls](https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls)
- [Anthropic：Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming)
- [Anthropic：Fine-grained tool streaming](https://platform.claude.com/docs/en/agents-and-tools/tool-use/fine-grained-tool-streaming)
- [Anthropic：Parallel tool use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/parallel-tool-use)
- [Anthropic：How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works)
