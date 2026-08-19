# 模型调用到底发生了什么：LLM、Model API、Messages 与 Token

> 本文资料核对时间：2026-08-19。OpenAI 代码示例基于官方 .NET 2.x 文档面；Provider 的模型名、字段、角色和 SDK 类型都可能演进，使用时应重新核对当前文档。本文只依据公开 API contract 建立工程模型，没有执行真实 API 调用实验。

很多人第一次接入大模型，看到的代码可能只有几行：创建一个客户端，传入模型名和一段文本，等待返回结果。

于是，“调用模型”很容易被理解成一个原子动作：我把 Prompt 发给 LLM，LLM 给我一个答案。

这个说法用于聊天没有问题，用于工程协作却太粗。团队很快就会遇到一连串无法靠它回答的问题：

- 换模型是不是只需要改一个字符串？
- SDK、API 和模型到底是不是同一个东西？
- 我把十轮历史消息一起传过去，模型是不是就“拥有记忆”了？
- 文档只有几万字，为什么还会超过 Context Window？
- HTTP 200 了，为什么程序仍然不能信任这次结果？
- 打开 streaming，是否等于看到了模型完整的思考过程？

这些问题共同指向一件事：**一次模型调用不是一行魔法，而是一条由多个对象和契约组成的工程链路。**

本文先建立这条最小链路。后续的 Prompt、Structured Output、Tool Calling 和 Agent Loop，都会站在这条链路上继续生长。

## 先把五个对象分开

日常沟通中，我们经常把 Model、Provider、API、SDK 和 Application 混称为“模型”。但它们承担的职责并不相同。

| 对象 | 在工程中负责什么 | 它不等于什么 |
|---|---|---|
| Model | 提供文本生成、理解或推理等能力，是请求中被选择的能力对象 | 不等于远程地址，也不等于客户端代码 |
| Provider | 提供模型服务、账号、认证、配额和 API contract 的主体或平台 | 不等于某一个 Model |
| Model API | 规定 endpoint、认证、请求结构、响应结构和错误语义的远程软件契约 | 不等于 SDK 中的某个方法 |
| SDK | 把远程 API 包装成某种语言中的类型和方法，处理一部分序列化与客户端样板 | 不等于 API 本身，更不等于 Model |
| Application | 选择模型、组织输入、发起调用、解析结果，并决定结果如何进入业务流程 | 不等于官方示例代码 |

这个划分有一个直接收益：团队讨论“换模型”时，可以继续追问到底换哪一层。

如果只是同一 API 下切换一个 model selector，改动可能较小；如果更换 Provider，endpoint、认证、消息结构、角色集合、响应字段和错误语义都可能变化。甚至在同一个 Provider 内，model selector 与 endpoint / deployment 也可能是两个不同的 contract 元素。

再看 SDK。OpenAI 当前官方 .NET 文档中的 Responses 示例，可以简化成下面这样：

```csharp
using OpenAI.Responses;
#pragma warning disable OPENAI001

string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY is missing.");

ResponsesClient client = new(apiKey);

ResponseResult response = await client.CreateResponseAsync(
    "gpt-5.6",
    "用一句话解释：为什么 SDK 不等于 Model API？");

Console.WriteLine(response.GetOutputText());
```

在这段代码里，`ResponsesClient` 和 `CreateResponseAsync` 是 SDK 提供的 C# 表达；`gpt-5.6` 是 model selector；两段字符串是 Application 构造的输入；`GetOutputText()` 是 SDK 帮助 Application 提取文本的便利方法。

它们最终依赖的远程 contract，可以用一个职责等价的 HTTP 请求显露出来：

```http
POST https://api.openai.com/v1/responses
Authorization: Bearer $OPENAI_API_KEY
Content-Type: application/json

{
  "model": "gpt-5.6",
  "input": "用一句话解释：为什么 SDK 不等于 Model API？"
}
```

SDK 调用和 raw HTTP 的语法不同，却都要服从同一个远程 API contract。官方 .NET 库也明确把自己定位为访问 OpenAI REST API 的客户端库，并由 OpenAPI specification 生成。由此可以得到一个很实用的判断：

> SDK 是我们访问 API 的一种客户端方式；它能隐藏样板代码，但不会把远程契约变成模型本身。

这也是为什么本文用一家 Provider 落地，却不能把它的类名、role 或 JSON 字段写成行业定义。

## 一次 Single Model Call 的可观察链路

把五个对象放回一次调用，可以得到下面这张最小工程图：

```text
Application
  ├─ 选择 model / 构造 input / 设置参数
  ↓
SDK 或 HTTP Client
  ├─ 序列化 / 认证 / 发送
  ├─ input tokens ──[model-specific Context Window]── output tokens
  ↓  ─────────── Public API Contract Boundary ───────────
Provider Model API
  ├─ 按公开 contract 接收请求、交付响应或错误
  ↓
[Provider 内部执行：本文不根据客户端代码推测]
  ↓
Response 或 API Error
  ↓
Application Handling
  ├─ 提取内容 / 检查 usage 与 finish metadata
  └─ 解析 / 校验 / 展示 / 继续工作流
```

这张图最重要的地方，不是箭头数量，而是中间那条公开边界。

在边界上方，Application 能看到自己构造了什么，SDK 或 HTTP Client 发出了什么。在边界下方，Provider 的公开文档可以告诉我们 endpoint、schema、响应和错误；但它通常不足以证明服务端到底经过几个组件，也不足以证明 validation、routing、tokenization 和 inference 的内部先后顺序。

所以，本文说的是**应用可观察的 contract chain**，不是“所有 Provider 内部都按这张图部署”。这是阅读任何 Harness 或 Agent Runtime 时都很重要的证据边界：不能从一段客户端调用代码，反推出一个未公开的服务端 pipeline。

## Messages 是当前请求输入，不是模型长期记忆

Single Model Call 中最常见的误解，是把 Messages 直接叫作 Memory。

假设用户先问“我的项目使用 Unity 2022”，下一轮再问“那我该选哪个 C# 版本”。为了让第二次调用理解“那”指什么，Application 可以把第一轮对话重新放入请求。Provider 也可能提供 conversation object、previous response id 等状态机制。产品还可能从自己的数据库中检索用户资料，再拼入当前请求。

这三种做法都能制造“它还记得”的用户体验，却来自不同位置：

```text
Previous turns
   ├─ Application 重发历史 ───────┐
   ├─ Provider conversation state ┼─> Current request context ─> Model call
   └─ Product memory retrieval ────┘

Messages / input = 当前请求的结构化表达
Long-term memory = 产品或运行时额外实现的能力，不能由 Messages 单独证明
```

OpenAI 的 conversation state 文档明确指出：在手工管理状态时，每次文本生成请求都是独立且 stateless 的，连续性来自重新提供历史，或使用它提供的状态机制。Anthropic 的 Messages API 同样允许在一次请求中放入 prior turns，并把这种多轮用法描述为 stateless conversation。

因此，更准确的表达是：

> 历史 Messages 可以成为本次调用的上下文，但“成为输入”不等于“模型自己拥有跨请求长期记忆”。

Message role 也不是一个可以随意跨 Provider 复制的统一枚举。以 2026-08-19 的公开 contract 为例：

| Provider contract | 当前公开的输入表达示例 |
|---|---|
| OpenAI Responses | input message 允许 system / developer / user / assistant；另有顶层 `instructions` instruction mechanism |
| Anthropic | input messages 使用 user / assistant；system 是顶层参数，不是 system role |
| Google | contents 使用 user / model；systemInstruction 独立 |

三家都在表达“谁提供了什么内容”，但字段、role 集合和 system instruction 的位置不同。面对新 Provider，正确动作不是先设计一个看似通用的固定 enum，而是先读取它当前的 API contract。

至于 Prompt 的层级、优先级和模板如何维护，属于下一篇；Session、Working Memory 和 Long-term Memory 的正式分类，则留到课程后面的 Memory 章节。

## Token 与 Context Window：容量单位，不是字符换算题

另一个常见误区是：“这份文档只有三万字，肯定放得进去。”

Model API 通常不是按“中文字符数”计算输入输出，而是按 Token 计量。文本会经过与模型相关的 tokenization；文件、图片和其他模态也可能按相应 contract 变成可计量的输入。请求中的 role、消息边界、工具定义等结构同样可能贡献 Token。

这意味着下面几个量之间不存在一个可以跨模型长期使用的固定换算：

```text
字符数 ≠ 单词数 ≠ 文件字节数 ≠ Token 数
```

“一个 Token 约等于几个字符”最多只能作为特定语言、模型和输入形态下的粗略直觉，不能当作容量校验。OpenAI 为 Responses payload 提供 token counting 能力，并提醒字符估算面对文件、图片等输入会失真；Google 也为 input / output 提供 usage 与 countTokens 能力。

Context Window 则是绑定具体模型 contract 的 token capacity。公开文档通常把输入、输出以及 contract 指定的其他 Token 一起纳入这个边界。于是，真正要问的不是“文档有多少字”，而是：

1. 最终 request 被计为多少 input tokens？
2. system instruction、历史消息、工具定义和其他输入占了多少？
3. 还需要为输出或 reasoning tokens 预留多少空间？
4. 目标模型和 API 对超出窗口如何处理？

这里还要再加一条边界：内容能放进 Context Window，不等于模型一定能高质量利用窗口里的任意信息。容量问题与信息选择、相关性、污染和长上下文质量是不同问题，后者会在 Context Engineering 章节继续展开。

本篇也不固化某个模型的 128k、1M 等数值。具体窗口、计数方式和价格都可能变化，应在真正选型时读取当前 model contract；Token、Step、Cost 和 Latency 的预算控制会在后面的 Budget Engineering 章节正式处理。

## Response 不是一个字符串，而是一份结果信封

SDK 经常提供 `GetOutputText()` 一类便利方法，让调用看起来像“传字符串，返回字符串”。这对最小示例很友好，但 Application 真正收到的通常是一个 response envelope。

不同 Provider 的 schema 不一样，不过经常可以找到三类职责：生成内容、usage，以及 finish / stop metadata。下面是概念图，不是任何一家 Provider 的统一 JSON schema：

```jsonc
{
  "generated_content": "...",      // 实际字段可能是 output、content 或 candidates
  "usage": {
    "input_tokens": 123,
    "output_tokens": 45
  },
  "finish_metadata": "..."         // 实际字段可能是 stop_reason 或 finishReason
}
```

OpenAI 当前 Responses API 把输出组织为 output items，并由 SDK 提供 `output_text` / `GetOutputText()` 等帮助能力；Anthropic 使用 content、stop_reason 和 usage；Google 则使用 candidates、finishReason 与 usageMetadata。它们支持“response 不只承担文本内容”这个抽象，却不支持我们发明一个跨 Provider 的共同字段名。

从 Application 视角，还要把一次结果分成三个判断层：

| 判断层 | 典型问题 | 不能推出什么 |
|---|---|---|
| Transport / API contract | 是否连接成功、通过认证、满足 schema、未触发限流、服务端正常响应 | 不能推出生成内容正确 |
| Generation completion | 是否正常停止、达到长度上限、发生 refusal，或出现其他 Provider-specific stop metadata | 不能推出业务任务完成 |
| Application quality | 内容是否正确、格式是否可解析、是否满足业务规则 | 不能只靠 HTTP status 判断 |

因此，HTTP 429 表示限流、额度或服务策略等 API 层问题，不证明模型“推理失败”；HTTP 200 只说明请求在 API contract 层得到成功响应，也不证明答案正确。类似 stop reason 的字段是成功 response 中的完成信息，同样不能和 request processing error 混成一类。

这层区分会直接影响日志和诊断。如果团队只记录“调用失败”，认证错误、超时、长度截断、拒答、格式解析失败和事实错误就会掉进同一个桶，后续既无法制定正确的恢复策略，也无法判断真正该优化哪一层。

本篇只建立分层。完整 Error Taxonomy、Retry 和错误归一化会在后文展开；Structured Output 的 parse、validate 和 repair 则属于 Article 03 与 Lab 01。

## Streaming 改变的是交付方式

默认情况下，Application 可以等待完整 response 返回后再处理。开启 HTTP streaming 后，Provider 会按照公开的事件协议逐步交付内容，Application 得以在完整生成结束前先展示或消费前面的增量。

```text
Non-streaming：request ─────────────> complete response ─> handle
Streaming：    request ─> event ─> event ─> event ─> done ─> aggregate / handle
```

OpenAI 与 Anthropic 当前公开的 HTTP streaming 文档都使用 SSE 事件描述增量交付；Anthropic 还展示了如何把增量事件最终聚合为完整 Message。由此能确认的是：streaming 改变了 response delivery 和 Application consumption。

由此不能推出的是：

- Model Capability 因 `stream=true` 自动改变；
- Application 自动获得完整、可验证的中间推理；
- 开启 streaming 就不需要处理结束事件、聚合、错误和中断。

Event 中能看到什么，取决于 Provider 对该 API 公开了什么 schema。即使某些产品提供 reasoning 或 thinking 类型，它也属于另行定义的 contract，而不是“只要 streaming 就公开隐藏思考”。

断线恢复、backpressure、增量解析和 streaming lifecycle 会显著增加工程复杂度，但它们属于 Provider Integration 章节。本篇只需要记住它在调用地图上的位置：它是响应交付方式，不是模型能力的同义词。

## 面对一个新 Provider，先检查哪些 contract？

到这里，我们可以把可迁移的知识压缩成一句话：**稳定的是职责问题，不稳定的是字段答案。**

下一次接入一个新 Provider，不妨先沿下面的清单查文档：

1. endpoint、authentication 和 API version 是什么？
2. model 或 deployment 如何选择？
3. input / messages 的 schema 与 role 集合是什么？
4. system instruction 放在哪里，优先级如何定义？
5. 生成内容、usage 与 finish / stop metadata 在哪里？
6. streaming event 与 error contract 如何表达？
7. Token 如何计数，Context Window 从哪里查询？

这不是一个“统一 Provider interface”的设计，而是一张阅读 API contract 的检查表。等到 Article 04，我们再讨论是否、以及如何用 Adapter / Gateway 收敛这些差异。

现在回头看开头那行 C# 代码，它仍然很简单，但不再神秘：

```text
Model != Provider != API != SDK != Application
Messages != Long-term Memory
Token != Character
Context Window != File Size
Streaming != Hidden Reasoning
API Success != Correct Answer
```

这就是后续 Agent Engineering 所需的最小 Model Call 心智模型。下一篇会继续沿这条 contract chain 前进：当 Application 已经知道如何把输入交给 Model API，Prompt 应该如何从一段临时文字，变成可维护、可测试的输入契约？

## Learning Check

1. 面对下面这个假想 Provider SDK，你能指出 SDK、Provider API、model selector、input、response 和 Application handling 分别在哪里吗？

   ```csharp
   var client = new NovaClient(endpoint, apiKey);
   var result = await client.GenerateAsync(model: "nova-pro", input: question);
   Console.WriteLine(result.Text);
   ```

2. 为什么 SDK 方法名不能直接当作 Model API 的行业定义？
3. 为什么把历史 Messages 重发给 API，不等于模型拥有跨请求长期记忆？
4. 为什么不能用固定字符数换算 Token 或 Context Window？
5. Streaming 主要改变了 Model Capability，还是 response delivery？
6. 为什么 HTTP 200、stop reason 和“答案正确”必须分开判断？

### 参考思路

1. `NovaClient` / `GenerateAsync` 是假想 SDK 表达，`endpoint` 指向 Provider API，`nova-pro` 是 model selector，`question` 是 input，`result` 是 response，最后一行是最小 Application handling；远程 schema 仍需查文档。
2. SDK 与远程 API 位于不同层，而且 Provider 的 role、schema 和类型并不统一。
3. 历史在本次调用里是输入；连续性可能来自 Application 重发、Provider state 或产品自己的 Memory。
4. tokenization 依模型、语言、输入形态和请求结构；Context Window 约束的是 Token capacity，不是字符或字节。
5. Streaming 主要改变响应的增量交付与应用消费方式。
6. 三者分别对应 API contract、generation completion 和 application quality。

## 参考资料

- [OpenAI：Text generation](https://developers.openai.com/api/docs/guides/text)
- [OpenAI：Conversation state](https://developers.openai.com/api/docs/guides/conversation-state)
- [OpenAI：Counting tokens](https://developers.openai.com/api/docs/guides/token-counting)
- [OpenAI：Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses)
- [OpenAI：API error codes](https://developers.openai.com/api/docs/guides/error-codes)
- [OpenAI：Official .NET library](https://github.com/openai/openai-dotnet)
- [Anthropic：Create a Message](https://platform.claude.com/docs/en/api/messages/create)
- [Anthropic：Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows)
- [Anthropic：Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming)
- [Anthropic：Handling stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons)
- [Google：GenerateContent API](https://ai.google.dev/api/generate-content?hl=en)
- [Google：Tokens](https://ai.google.dev/gemini-api/docs/tokens)
