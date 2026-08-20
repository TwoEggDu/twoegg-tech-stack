# MCP 与外部能力边界：协议解决什么，宿主仍需解决什么

团队把一个 MCP Server 接进应用后，界面很快就能列出 Tool。模型也确实能生成调用参数，甚至第一次调用就收到了结果。于是项目里很容易出现一句过早的完成声明：

> “MCP 已经接好了，Agent 现在拥有这个能力。”

这句话把三件事混在了一起：Server 是否声明了一个能力、当前调用是否应该被允许、整个系统是否已经具备 Agent 的运行闭环。

假设 Server 列出了一个 `delete_build` Tool，`inputSchema` 要求传入构建编号。模型给出的参数结构完全合法，但目标是 production artifact。此时，“Tool 可见”和“参数合法”都不能回答最重要的问题：Host 是否应该把这个候选发出去？Server 是否允许当前调用者操作这个资源？外部系统实际发生了什么？返回结果又足不足以支持“删除已安全完成”这项主张？

MCP 解决的是其中一段非常重要、但范围明确的问题：让 Client 与 Server 以标准消息发现能力、描述参数、发起调用并返回结果或错误。它让外部能力可连接，却不会让协议两侧的工程责任自动消失。

如果这篇只记一句话，我建议记住：

`MCP 标准化的是外部能力的消息边界；能否执行、为何拒绝、结果能证明什么，仍需要 Host、Server 与外部系统各自关闭责任。`

> 版本范围：本文协议行为固定到 MCP `2026-07-28`，资料核对日为 `2026-08-20`。文中不推断具体 SDK 的默认行为，也没有执行本地 MCP Runtime。

## 先把 MCP 放回整条能力链

讨论某个 method 之前，先把系统画完整：

```text
User intent / application context
              |
      Host Tool Runtime
              |
          MCP Client
              |
     Client / Transport
              |
          MCP Server
              |
       External System
```

这不是一条“完成链”，而是一条责任链。

Host Tool Runtime 位于应用侧。它掌握用户意图、当前上下文、本地 Tool Registry 与 Policy，接收模型产生的行动候选，并决定候选是否有资格进入外部调用。MCP Client 负责形成协议请求、接收协议响应。Transport 负责怎样承载这些消息。MCP Server 暴露 Tool、处理调用，并可能继续访问真正持有资源和业务状态的 External System。

官方 `2026-07-28` architecture page 把 Host 定义为承载 MCP Client 的 LLM application，把 Client 与 Server 定义为通过 transport 交互的组件。current wire contract 的直接消息角色是 Client 与 Server；Host 如何保存应用上下文、怎样组织 Policy，则属于产品实现。这里尤其要注意资料边界：本文只从这份 versioned teaching page 取得 **Host / Client / Server 的角色定义**；current wire facts 仍分别追到对应的 normative specification，不从角色概览扩大推断。

这条链也解释了为什么不能把 MCP Server 直接画成“Agent 的全部能力层”。协议可以告诉 Client 怎样发送 `tools/call`，却不知道 Host 当前面对的是普通开发环境还是生产资源；Server 可以收到结构正确的 arguments，却仍要面对调用者、具体资源与领域规则；External System 的副作用也不会因为 MCP result 到达就自动得到证明。

前两篇已经建立了必要的前置边界：[Function Calling 与 Tool Use](../../../../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md)说明 Tool Call 是结构化行动候选，不等于已经执行；[Tool Runtime](../../../../content/ai-empowerment/agent-engineering-06-tool-runtime.md)则把 Schema、Policy、Execute、Result 与 Trace 拆开。本篇不重写完整 Runtime，而是把这些责任接到 Client / Server 的远程协议边界上。

这也回答了“MCP Tool 与本地 ToolDefinition 是什么关系”。MCP Tool definition 是 Server 通过协议暴露的远程能力合同；Host 为了让模型使用它，可以把这份合同映射进自己的候选能力视图。但这不意味着远程 definition 就是一个可直接执行的本地函数，也不意味着 Host 必须把 Server 列出的所有 Tool 原样交给模型。Host 仍要保留来源、当前上下文与本地 Policy，MCP Client 则负责把批准后的候选转换成协议调用。

协议也不因此规定本地 Registry 的类名、Client lifetime 或进程拓扑。Host 与 Server 可以采用不同部署方式，Host 也可以按自己的应用合同创建、复用或隔离 Client；这些选择都不能从角色图本身推出。评审真正要保留的是来源、路由与责任关联：哪个 Host decision 形成了哪次 Client request，它被哪个 Server 处理，而不是强迫所有产品复制同一种对象结构。

因此，第一条判断是：

```text
Protocol role != application responsibility owner
```

## Discovery 与 Schema：看见能力，不等于获得许可

MCP `2026-07-28` 提供 `server/discover` 来暴露协议版本、Server capabilities 与相关信息。Server 必须实现它，但 Client 可以选择调用，也可以直接发送其他请求并处理不支持版本的错误。换句话说，discovery 是可选的预探测，不是所有业务调用之前都必须完成的仪式。

current request 还会声明 protocol version 与 client capabilities。discovery result 中的 `serverInfo`，以及请求中的 `clientInfo`，都是参与者自报的信息。它们能帮助描述对端软件，却不能被提升成安全身份，更不能直接参与“这个主体是否获准操作资源”的判断。

如果 Server 声明支持 tools capability，它必须响应 `tools/list`。返回的 Tool definition 用 name 和有效的 JSON Schema `inputSchema` 描述参数合同，也可以带 `outputSchema`。Tool 集合可以随着本次 request authorization 变化；非受信 Server 提供的 annotations 也应按不可信 metadata 处理。

把这些事实放在一起，可以得到四条互不替代的边界：

| 观察到的事实 | 它能够说明 | 它不能单独说明 |
|---|---|---|
| discovery 返回 tools capability | Server 声明支持 Tool feature | 当前用户获准调用任意 Tool |
| `tools/list` 返回 name | 当前请求看见一个候选能力 | 实现一定安全、永远可用 |
| `inputSchema` 有效 | 参数具有机器可读形状 | 参数符合业务 Policy |
| client/server info 存在 | 参与者提供了自报信息 | 已建立安全 identity |

因此，discovery 与 list 更像“当前协议视图”，不是一张永久能力证书。Client 可以保存发现结果，但本篇不会从规范推导缓存多久、何时刷新或具体 SDK 怎样更新；更重要的是，Tool set 本来就可以按本次 request authorization 变化。评审如果只看启动时的一次列表，就没有资格推断稍后每个调用者仍看到相同能力，更没有资格绕过调用时的授权判断。

回到 `delete_build` 的例子：`tools/list` 让模型知道它可以怎样表达删除候选，schema 可以拒绝缺少构建编号或类型错误的输入。但“目标是否属于 production”“当前用户是否允许删除”“这个构建是否仍被发布引用”，都不是 JSON Schema 能回答的问题。

所以，Capability、Schema 和 Permission 不能压成一个“Tool 已接好”：

```text
Discovered != Authorized
Schema Valid != Policy Allowed
```

## Call 与 Result：失败发生在哪一层

Client 使用 `tools/call` 发送 Tool name 与 arguments。current Tools contract 把两类失败分开表达：

- unknown tool、malformed request 或 Server protocol failure 使用 JSON-RPC `error`；
- Tool 已进入处理，但 API、输入校验或业务逻辑失败，可以在 Tool result 中使用 `isError: true`。

这不是字段风格差异，而是两个不同的责任层。前者说明请求或协议合同没有成立；后者说明 Server 接受了 Tool 调用，但执行层给出了失败结果。如果 Host 把二者统一压成“调用失败”，就会失去该重查协议、调整 arguments，还是停止业务动作的判断基础。

下面是一条最小消息 trace。

> **SPEC-DERIVED ILLUSTRATION / NOT LOCALLY EXECUTED**
>
> 这段 JSON 由 MCP `2026-07-28` 官方示例裁剪并补齐 current request `_meta`。它只解释消息顺序，不是本地 Runtime Observation；名称、版本与天气结果都是 illustrative data。

```json
{"jsonrpc":"2.0","id":"d1","method":"server/discover","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"d1","result":{"resultType":"complete","supportedVersions":["2026-07-28"],"capabilities":{"tools":{}},"_meta":{"io.modelcontextprotocol/serverInfo":{"name":"ExampleServer","version":"1.0.0"}}}}
{"jsonrpc":"2.0","id":"l1","method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"l1","result":{"resultType":"complete","tools":[{"name":"get_weather","inputSchema":{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}}]}}
{"jsonrpc":"2.0","id":"c1","method":"tools/call","params":{"name":"get_weather","arguments":{"location":"New York"},"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"c1","result":{"resultType":"complete","content":[{"type":"text","text":"Current weather in New York: ..."}],"isError":false}}
```

这条 trace 只支持五个窄结论：Client 发出了 discovery；Server 返回了能力声明；Client 取得了 Tool schema；Client 发出了 name + arguments；Client 收到了 protocol result。

它不支持“用户已经授权”，也不支持“Server 已完成所有业务校验”。`isError: false` 不能证明 External System 的副作用 exactly-once，不能证明 Host 已经完成 result validation、内容整形与 Trace，更不能证明模型正确使用了结果。

反过来，`isError: true` 也不是“什么都没有发生”的证明。它只表明 Tool execution / business failure 被放进了 result 通道；外部系统是否已经产生部分副作用，不在这一位布尔值的保证范围内。Host 若要决定 retry、补偿或向用户展示什么，仍要结合 Tool 语义和另外的执行证据，不能看到一个可解析 result 就假设调用可以安全重放。

这就是另一个重要边界：

```text
Protocol Result != Evidence
```

## Current 2026-07-28：没有 initialize handshake，也没有 protocol session

MCP 的 lifecycle 在 `2026-07-28` 发生了需要特别警惕的版本断点。很多旧教程从 `initialize -> initialize result -> notifications/initialized` 开始；如果把这段顺序直接搬进 current 文章，就会把 legacy contract 写成现行事实。

> **Current correction**：MCP `2026-07-28` 没有 `initialize / initialized` negotiation handshake，也没有 protocol session。每个 request 都声明 protocol version；`initialize / initialized` 只属于 `2025-11-25` 及以前的 legacy era。

current version handling 可以压缩成下面的关系：

```text
modern 2026-07-28+
  request(_meta.protocolVersion + clientCapabilities)
    -> result
  or UnsupportedProtocolVersionError(-32022)
    -> choose a mutually supported version and retry, or report error

legacy <= 2025-11-25
  initialize
    -> initialize result
    -> notifications/initialized
```

Server 收到 request 后，针对该请求判断版本是否受支持。不支持时，它返回 `-32022` 与 supported versions；Client 应选择双方都支持的版本重试，或者向上报告不兼容。`server/discover` 可以作为 preflight 帮助 Client 了解版本与能力，但它不会建立 session，也不是换了名字的 initialize handshake。

“没有 protocol session”也不等于“应用可以忘记所有状态”。它只是在说明 current MCP 的协议协商不再依赖一段 initialize lifecycle。用户意图、Tool routing、Policy decision、调用关联与结果处理仍由 Host 应用管理；Server 若需要业务状态，也必须由自己的实现合同说明。不能因为 wire protocol 变得 stateless，就把应用责任一起删掉。

这项纠偏还有一个工程含义：不要从规范直接推断任一 SDK 的默认协商、缓存、fallback 或重试行为。规范给出了 wire contract；具体实现是否支持 current 版本、采用何种兼容策略，需要另外验证。本文没有这类 Runtime Evidence，因此到 contract 为止。

## Transport 与 Cancellation：共享消息语义，不共享所有信号

stdio 与 Streamable HTTP 都可以承载 MCP 的 request、result 与 error，但“共享 core message semantics”不等于 framing、metadata 和取消动作完全相同。

| 维度 | stdio | Streamable HTTP | 不能扩大成什么 |
|---|---|---|---|
| Core message | 相同的 MCP 消息语义 | 相同的 MCP 消息语义 | 具体实现一定互操作 |
| Framing | 使用 stdio 的消息 framing | 使用 HTTP request / response，可涉及 SSE | 两边 framing 可以互抄 |
| Metadata | 按自身 transport contract 承载 | 存在 HTTP metadata / header mirror 要求 | metadata 就是安全身份 |
| Cancellation | 使用 `notifications/cancelled` | 关闭该 request 的 SSE response stream | External System 一定停止 |

取消尤其容易被说得过强。request 可能已经完成，cancel signal 也可能晚到；Server 对取消通常是 SHOULD stop，而不是协议保证强制终止任意底层工作。timeout 同样是 implementation SHOULD，需要由实现给出明确 owner 与可配置边界。

因此，“Host 已停止等待”“transport 已发送取消信号”“Server 停止处理”“External System 已回滚”是四个不同事实。前一个事实成立，不能替后面三个一起成立。准确的工程表述应该是：取消是 cooperative，而且存在 race；若要确认底层工作停止，需要额外的实现合同与证据。

这四层还决定了 Trace 应该记录什么。只记一个 `cancelled=true`，事后无法区分是谁不再需要结果、Server 是否看见信号、handler 是否已经进入，以及下游是否仍在工作。本文不设计新的 Trace Schema，但 owner 至少要避免把这些不同观察压成一句“任务已取消”。当确认信息不足时，fail closed 的含义是停止进一步声称成功，而不是凭空把未知状态改写成已回滚。

```text
Cancellation Requested != External Work Stopped
```

本文不比较两种 transport 的延迟、吞吐、可靠性，也不推断网络重试或下游补偿行为。那些结论需要新的实验或实现证据，不能从 core protocol 直接得到。

## Authorization 与 Trust：Token 仍不是完整业务 Permission

MCP `2026-07-28` 的 authorization 是 optional，并为 HTTP-based transport 定义授权流。采用该 flow 时，Server 必须验证 token 与 intended audience；当具体请求受 scope 限制时，Server 还要拒绝不足 scope。stdio 不使用这套 HTTP authorization flow，规范把它放在不同的 credential boundary，例如由进程启动环境提供凭据。

这些规则很重要，但它们仍然没有把四种信息合成一个东西：

| 信息 | 它主要回答 | 它没有自动回答 |
|---|---|---|
| client/server info | 对端自报的软件信息 | 调用者的安全 identity |
| HTTP token | transport flow 中携带什么 credential | 具体业务动作一定获准 |
| token audience / scope | token 面向谁、覆盖哪些 scope | arguments 符合 domain rule |
| stdio environment credential | 进程启动边界怎样提供 credential | Host 当前候选应该执行 |

这也是为什么不能说“MCP 没有安全”。它有 authorization contract 和安全指导；准确的边界是：transport authorization 不能单独推出本课程所说的完整 Permission，也不能替 Server 做调用者与具体资源之间的业务授权。

同样，`serverInfo.name` 不是 principal。一个自报名称可以用于显示或协议协调，不能承担 token validation、caller binding 或 resource authorization。若把这些责任藏在一个“已连接”布尔值后面，系统会同时失去拒绝理由与审计语义。

官方安全指导还提醒了另一个边界：Server 不能把面向自己的 token 随意透传给不同的下游资源。token passthrough 会模糊 intended audience，也会制造 confused deputy 与 audit trail 风险。协议授权做对的第一步，是 Server 确认真正面向自己的 credential；至于随后允许调用者对哪个具体资源做什么，仍是 resource-level 与 domain-level decision。

## 课程工程建议：Host Policy + Server Policy 双层 fail-closed

> **`07-C08 = COURSE PROPOSAL`**：下面是本课程采用的工程责任模型，不是 MCP normative requirement，不是行业唯一架构，也没有经过本文的本地 Runtime 验证。

只在 Host 检查有一个明显风险：Server 被当成完全可信的执行代理，因而跳过 caller、resource 与 domain validation。只在 Server 检查也有代价：Host 会把本来就不该离开应用边界的模型候选直接发送到外部系统。

本课程因此建议两层都 fail closed：Host 掌握模型候选、用户意图与本地上下文，决定候选是否有资格发出；Server 掌握真实调用者、具体资源与下游规则，再做一次资源和领域判断。任何一层无法确认，都不把“不知道”降级成允许。

```text
model candidate
      |
Host Policy
  deny -> stop
  allow candidate
      |
MCP request
      |
Server Policy
  deny -> protocol-appropriate error or Tool result
  allow
      |
External System
```

把 owner 继续拆开，评审时会更清楚：

| Concern | Host-side proposal | Server-side proposal | Fail-closed boundary |
|---|---|---|---|
| Candidate admission | 用户意图、当前上下文、local Tool Registry / Policy | 不假设 Host 已完成所有检查 | Host 不确定就不发调用 |
| Identity binding | 选择可接受的 Client / credential context | 验证 token audience、caller 与 resource 关系 | 无法绑定就拒绝 |
| Domain validation | 提前拒绝明显越界候选 | 依据真实资源与业务规则最终校验 | 规则不允许就拒绝 |
| Timeout / cancellation | 决定等待预算、保留 caller intent | 响应 cancel，管理下游 cooperative stop | 不声称强制终止 |
| Result shaping | 校验并限制进入 model / UI 的内容 | 返回符合协议的 result / error | 无效结果不进入下一层 |
| Audit | 记录候选、Policy decision 与 correlation | 记录 caller、resource 与 downstream outcome | 缺少记录不伪称完整 Evidence |

把 `delete_build(build_id="release-1042")` 代入这张表，可以看到两层检查并不重复。Host 先根据用户当前目标、环境和本地 Policy 判断：模型提出的是否是一个允许离开应用边界的候选。如果上下文只允许查询，它应该在发出 MCP request 之前停止。只有 Host 放行后，Server 才依据实际 caller、token audience / scope、artifact 身份与真实业务规则做最终判断。Server 拒绝时，Host 不能用“我已经批准”覆盖它；Server 放行时，也不能反向证明模型候选符合用户原始意图。

结果返回后，两侧仍各有责任。Server 要用合适的协议 error 或 Tool result 表达处理结果，Host 要验证并限制进入 model / UI 的内容，再保存足以关联决策的记录。任何一层缺少 owner，都不能用“协议 round trip 完成”补齐。这个例子没有执行删除，也不提供 production 行为事实；它只把批准的责任模型落回一个可审查的调用候选。

这张表要求的是责任显式，不要求物理部署一定分成两个进程。产品可以让 Host 与 Server 位于同一进程，也可以把 Policy 交给独立 engine；名称和 stage order 都可以不同。关键是不能因为两层被部署在一起，就让“谁依据什么拒绝”从设计中消失。

这份 proposal 也不提供完整 Permission schema、approval UX、分布式审计、retry policy 或补偿机制。它只建立一个评审起点：Host approval 不能替代 Server authorization，Server authorization 也不能反向证明 Host 已正确处理用户意图。

## 协议成功后，哪些结论仍不能单独推出

> **`07-C09 = PARTIAL / COURSE BOUNDARY`**：MCP discovery / call / result 的协议成功，**不能由这些 MCP 消息单独推出** Agent Loop、Permission、Article06 Tool Runtime gates 或完整 Evidence 已经成立。这个结论不否定某个产品可以在 MCP 外另行实现这些层。

这是一个窄范围判断，不是对所有 MCP 产品的否定。规范正面定义了 discovery、schema、call、result、transport、authorization 与 cancellation；它没有要求每条成功消息同时携带本课程其他层的完成证明。但具体 Host 或 Server 完全可以在协议之外实现 Policy、approval、Runtime 与 Trace。因此，准确说法不是“MCP 没有这些能力”，而是“不能从 MCP 消息本身推导这些能力已经闭合”。

这种说法之所以保留 `PARTIAL`，是因为它依赖“协议正面定义了什么”与“本课程怎样定义 Agent、Permission、Tool Runtime、Evidence”两组边界，而不是一条规范中的普遍否定句。它能阻止证据越界，却不能替我们评价任一具体产品：若产品声称已经实现了这些层，仍应读取它在 MCP 消息之外的设计与 Runtime Evidence，再逐项判断。

| 成功观察 | 仍需独立回答 | 必须停止的推断 |
|---|---|---|
| `server/discover` 成功 | Host / Server 是否信任自报信息 | secure identity 已建立 |
| `tools/list` 成功 | 当前候选是否应执行 | Permission 已获得 |
| `tools/call` 返回 result | external side effect 与业务状态是什么 | exactly-once 或业务成功 |
| `isError: false` | Host 是否校验、整形、记录并正确回送 | 完整 Tool Runtime 已通过 |
| 一次成功 trace | 后续运行责任由谁继续处理 | Agent 已经完成 |
| protocol log 存在 | Observation 如何绑定来源、限制与 Claim | 完整 Evidence 已形成 |

这里延续的是课程已有词汇边界，而不是新定义。Tool 的当前含义、Host 的应用责任与 Evidence 的证明要求以[课程词汇表](../../glossary.md)为准。本篇只做到协议映射，不提前展开后续概念的完整机制。

## 怎样审查一条 MCP 外部能力链

面对一张架构图、一段成功 trace 或一句“已接入”的发布说明，可以按下面顺序检查：

1. 图中是否同时保留 Host Tool Runtime、Client / Transport、Server 与 External System？
2. protocol baseline 是否明确为 `2026-07-28`？architecture page 是否使用同版本来源、且只被用于角色定义？
3. discovery 与 `tools/list` 是否被误写成 secure identity 或永久授权？
4. Schema validation 与 Host / Server Policy 是否仍是不同判断？
5. JSON-RPC error 与 Tool result `isError: true` 是否保留了不同失败层？
6. current lifecycle 是否错误保留了 `initialize / initialized` handshake 或 session？
7. stdio 与 HTTP 的 framing、metadata、cancel signal 是否被当成完全相同？
8. cancellation 是否被误写成 External System 已经强制停止？
9. transport authorization 是否被当成完整业务 Permission？
10. success trace 是否被过度推成 Agent、完整 Tool Runtime 或 Evidence closure？

每条审查结论都可以写成四步：先记录 observed protocol fact，再给出它支持的 interpretation，然后列出缺失的 owner 或 Evidence，最后决定允许继续还是必须停止。比如：`tools/list` 返回了 `delete_build` schema，只支持“候选能力对当前请求可见”；Host 是否允许 production target、Server 是否允许该 caller 操作该 artifact 仍要独立回答，因此当前不得写“删除权限已获得”。

## Learning Check

1. 画出 Host Tool Runtime -> Client / Transport -> Server -> External System，并指出 wire message 的直接角色是谁。
2. `server/discover` 返回 tools capability，为什么不能证明当前用户有权调用任意 Tool？
3. `tools/list` 返回有效 `inputSchema`，为什么仍不能证明参数符合业务 Policy？
4. unknown / malformed request 与 Tool business failure 分别怎样表达？
5. MCP `2026-07-28` 怎样处理版本不兼容？`initialize / initialized` 属于哪个时期？
6. 收到 cancellation signal 后，为什么不能断言 External System 已停止？
7. 有效 HTTP token 或 `clientInfo` 为什么不能直接充当业务 Permission？
8. 在课程 proposal 中，candidate、identity、domain validation、cancel、result 与 audit 分别由 Host / Server 哪一侧负责？
9. 一段 discover / list / call / result 成功 trace，为什么不能单独证明 Agent、Permission、Runtime 与 Evidence 已经成立？

判断答案是否合格，有两个不能省略的标志：第 8 题必须明确“双层 fail-closed 是 COURSE PROPOSAL，不是 MCP 唯一架构”；第 9 题必须使用“不能由 MCP 消息单独推出”，并承认产品可以在协议外实现这些层。

### 参考答案

1. Host Tool Runtime 位于应用责任面；MCP Client / Transport 连接 Server，Server 再面向 External System。wire message 的直接协议角色是 Client 与 Server。
2. discovery 返回的是 capability 与自报 info；它不是 secure identity，也没有替资源授权作决定。
3. `inputSchema` 只验证参数形状。用户意图、资源范围与 domain rule 仍要由 Host / Server Policy 判断。
4. request / protocol failure 使用 JSON-RPC `error`；Tool execution / business failure可以在 result 中使用 `isError: true`。
5. current request 自带 version；不支持时返回 `-32022` 与 supported versions，由 Client 选择共同版本重试或报错。`initialize / initialized` 只属于 `2025-11-25` 及以前。
6. cancellation 是 cooperative 且存在 race；停止等待、发出信号、Server 停止和外部工作停止是不同事实。
7. token 需要 audience / scope validation，clientInfo 又只是自报 metadata；二者都不能替具体资源的业务授权。
8. 课程 proposal 让 Host 管候选与用户上下文，让 Server 管 caller / resource / domain 最终判断，并显式分配 cancel、result 与 audit owner；这不是规范唯一架构。
9. success trace 只证明协议消息交换。它不能由自身单独推出其他课程层已经闭合，但产品可以在协议之外提供相应实现与证据。

## 资料与证据边界

以下 external sources 是本文唯一允许的协议资料白名单，均在 `2026-08-20` 核对。规范与架构资料均固定到 `2026-07-28`；architecture page 只用于组件角色定义。

- [MCP 2026-07-28 release](https://blog.modelcontextprotocol.io/posts/2026-07-28/)
- [Versioning and Compatibility](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/versioning.mdx)
- [Discovery](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/discover.mdx)
- [Tools](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/tools.mdx)
- [Transport overview](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/index.mdx)
- [Streamable HTTP](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/streamable-http.mdx)
- [Cancellation](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/patterns/cancellation.mdx)
- [Authorization](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/authorization/index.mdx)
- [Security best practices](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/tutorials/security/security_best_practices.mdx)
- [Versioned schema](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2026-07-28/schema.ts)
- [Versioned server concepts](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/server-concepts.mdx)
- [Versioned core architecture](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/architecture.mdx)

本文没有执行 Provider call、本地 MCP Server、stdio 或 HTTP fixture，也没有把上面的 JSON 示例登记成 Runtime Observation。因此，它能给出的结论是 versioned specification 与课程边界判断，不是 SDK 兼容性、性能、网络行为或 production security 的验证报告。

## 最短结论

MCP 把 discovery、schema、call、result、error 与 transport 边界标准化；Host 与 Server 仍要分别回答候选是否能发出、资源是否允许访问、取消是否真正完成、结果能给谁看，以及证据究竟证明到哪里。

`MCP 让外部能力可连接；工程是否可执行、可拒绝、可追责，仍取决于协议两侧没有被省略的责任。`

下一篇才进入 Agent 的运行循环；本文在协议边界处停止。
