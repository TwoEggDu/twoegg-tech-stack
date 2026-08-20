# MCP 与外部能力边界：协议解决什么，宿主仍需解决什么

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Outline Gate：`PASS_RECOMMENDED`（候选；由 Master 独立核对后决定）
- Article Type：原理篇 / 协议映射篇
- Concept Maturity：`Engineering`
- Length Level：`M`
- Draft Target：约 `5,000—6,500` 中文字
- Protocol Baseline：MCP `2026-07-28`，资料核对日 `2026-08-20`
- Required Lab：`NONE`
- Runtime Evidence：Provider calls=`0`；local MCP runtime=`0`
- Claim Register：`07-C01`—`07-C09`
- Hard Status Guard：`07-C01`—`07-C07 = CONFIRMED`；`07-C08 = COURSE PROPOSAL`；`07-C09 = PARTIAL`

> 本 Outline 只规划由已批准 Research / Evidence 支撑的正文。所有协议行为固定到 MCP `2026-07-28`；示例 trace 仅为 `SPEC-DERIVED ILLUSTRATION / NOT LOCALLY EXECUTED`，不得改写成本地运行观察。

## 1. Article Thesis

接入 MCP 解决的是一段可互操作的外部能力消息边界：Client 可以发现 Server 能力、读取 Tool schema、发出调用并接收结构化结果或协议错误。它不会仅凭“消息走通”就替 Host 决定模型候选是否应执行，也不会替 Server 完成调用者、资源与业务域的最终授权，更不能单独证明 Agent、Permission、完整 Tool Runtime 或 Evidence 已经成立。

全文用一条责任链组织内容：

```text
Host Tool Runtime
        -> MCP Client
        -> Client / Transport
        -> MCP Server
        -> External System
```

这条链先回答“谁与谁交换什么”，再回答“每个责任面仍由谁关闭”。协议字段只在抽象模型之后出现，避免把 API 顺序误当成完整工程闭环。

### Type decision

- 本篇是原理篇 / 协议映射篇，不是 MCP SDK 教程。
- 不从创建 Client、安装包或启动 Server 开场；先呈现“接上 MCP 后仍可能错误执行”的问题空间。
- Concrete mechanism 只覆盖 current MCP `2026-07-28` 已批准的 discovery、schema、call/result、version、transport/cancel、authorization。
- Engineering judgment 用课程的双层 fail-closed proposal 与窄范围 C09 边界收口。
- Required Lab 为 `NONE`；不得用伪代码或 JSON 示例冒充 runtime closure。

## 2. Reader Change

完成本篇后，读者应能：

1. 画出 Host Tool Runtime、MCP Client、Transport、MCP Server、External System 的责任链；
2. 区分 protocol role、application owner、transport binding 与 external side effect；
3. 解释 `server/discover`、`tools/list`、`tools/call` 与 result/error 各自证明到哪里；
4. 明确 current `2026-07-28` 没有 `initialize / initialized` negotiation handshake，也没有 protocol session；
5. 区分 stdio 与 Streamable HTTP 的共同消息语义和不同 framing / cancellation signal；
6. 解释 transport authorization、self-reported info、业务 Permission 与 Host / Server Policy 不是同一概念；
7. 用“Host Policy + Server Policy 双层 fail-closed”审查责任所有权，同时明确它是课程 proposal；
8. 对任何“已经接入 MCP，所以能力、权限、Agent 和 Evidence 都完成了”的表述给出 scoped 反证。

最终可判定能力：读者拿到一张 MCP 调用图或一段成功 trace，能够标出协议已证明的事实、仍需外部证据的责任面，以及应该停止推断的位置。

## 3. Teaching Spine

| Teaching Phase | Reader Movement | Main Placement | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“能列出 Tool / 能收到结果”退回到“哪些工程责任仍未关闭” | Opening | `07-C01`、`07-C08`、`07-C09` |
| Abstract Model | 建立 Host Tool Runtime -> Client / Transport -> Server -> External System | Section 1 | `07-C01 / 07-E01` |
| Concrete Mechanism A | 理解 discovery、capability、schema 与 self-reported identity | Section 2 | `07-C02`、`07-C03` |
| Concrete Mechanism B | 理解 call、result、protocol error 与 Tool execution error | Section 3 | `07-C04` |
| Version Correction | 从 legacy handshake 心智切到 current per-request version model | Section 4 | `07-C05` |
| Transport / Cancellation | 区分 core semantics、binding、cooperative cancellation 与 timeout | Section 5 | `07-C06` |
| Authorization Boundary | 区分可选 HTTP authorization、stdio credential boundary、业务权限 | Section 6 | `07-C07` |
| Engineering Judgment | 用双层 fail-closed proposal 分配 owner，不伪装成规范保证 | Section 7 | `07-C08` |
| Boundary Closure | 明确 protocol success 不能单独推出 Agent / Permission / Runtime / Evidence | Section 8 | `07-C09` |
| Verification | 用审查表与 Learning Check 形成可迁移判断 | Section 9 + Learning Check | `07-C01`—`07-C09` |

### M-level scope discipline

- 主线只有一条：**MCP 标准化远程能力的消息边界，但责任闭环仍跨越 Host、Server 与 External System。**
- 用一个最小 Tool 贯穿 discovery -> list -> call -> result，不扩展成 SDK 项目。
- 只保留一段最小 JSON trace；字段用于解释消息关系，不用于展示运行成功。
- current / legacy 对照只服务 C05，不展开协议演化史。
- transport 只比较 stdio 与 Streamable HTTP；不评价性能、延迟、重试与互操作性。
- authorization 只讲已批准的 transport-scoped contract；不设计完整身份平台或 Permission 系统。
- 双层 fail-closed 明确标 `COURSE PROPOSAL`；不声称是 MCP 唯一或规范架构。
- C09 始终使用“不能由 MCP 消息单独推出”的窄表述；不否定产品可以在协议外实现相关层。

## 4. Opening｜为什么“接了 MCP”不是一句完成声明？

- Problem：团队看到 Client 能发现 Server、界面出现 Tool、一次调用返回文本，便把三个不同判断压成一句“能力已经接好”：模型能不能选、这次该不该执行、系统是否已经形成 Agent 闭环。
- Conflict：同一条成功 trace 可以同时满足协议交换，却仍缺少 Host 候选过滤、Server 资源授权、External System 业务约束、result validation、audit 与 claim-level evidence。
- Core Thesis：**MCP 让边界可交换，不让责任自动消失。**
- Opening Example：一个 `delete_build` Tool 被列出，schema 也合法，但请求指向 production artifact。正文不判断它应执行；只追问 Host 与 Server 分别在哪一层拒绝、协议成功能否代表业务许可。
- Claim IDs：`07-C01`、`07-C08（COURSE PROPOSAL）`、`07-C09（PARTIAL）`。

### Opening must separate three questions

| 常见完成句 | 应拆成的问题 | 本篇回答方式 |
|---|---|---|
| “MCP 已暴露这个能力” | Client 看见了什么协议声明？ | discovery + tools/list；不等于实际授权 |
| “MCP 已允许执行” | Host 和 Server 各自依据什么 Policy？ | 双层 fail-closed proposal；不是规范唯一实现 |
| “MCP 已经是 Agent” | loop、state、termination、permission、evidence 在哪里？ | 只能说 MCP 消息本身不能单独证明这些层 |

- Guardrail：不要写“MCP 没有安全”“MCP 没有权限”“所有 MCP 产品都没有 Agent Runtime”。
- Transition：先暂时移开具体 method name，画清参与者与责任流。

## 5. Section 1｜抽象模型：协议连接了 Client 与 Server，没有吞掉 Host 与外部系统

- Section Goal：建立全文责任地图，并区分 official component role 与课程工程 owner。
- Problem：把 MCP Server 画成“Agent 的全部能力层”会同时遮住 Host application context 与 External System 的真实资源边界。
- Document Thesis：official architecture 用 Host、Client、Server 描述组件关系；current wire message 的直接双方是 Client / Server，transport 承载消息，Host 如何保存上下文与治理候选属于应用实现。
- Claim IDs：`07-C01`
- Evidence：`07-E01`；`S-05`、`S-11`、`S-12`。

### Figure 1｜责任链，不是完成链

```text
User intent / application context
              |
      Host Tool Runtime
              |
          MCP Client
              |
   Client / Transport boundary
              |
          MCP Server
              |
       External System
```

图注必须写：

- `S-12` 只支持 Host contains Client、Server exposes features 等角色定义；
- `S-12` 固定到 `2026-07-28`，且作为 teaching overview 不代替 normative spec；
- Host Tool Runtime、Host Policy 与 owner 分工是课程映射，不是 wire protocol 新角色；
- External System 的副作用与业务状态不因 MCP result 自动被证明。

### Responsibility reading order

1. Host Tool Runtime 接受模型产生的行动候选，但候选不是执行权；
2. MCP Client 形成符合协议的 request 并接收 response；
3. Transport 决定 message 如何被承载，而不是业务动作是否应该发生；
4. MCP Server 暴露 Tool、处理调用并连接下游；
5. External System 才可能持有真实资源、业务规则与副作用。

- Boundary Sentence：`Protocol role != application responsibility owner`。
- Previous Article Link：Article05 已把 Tool Call 与 executed、authorized、evidence 分开；Article06 已把 ToolDefinition、Schema、Policy、Result / Trace 分开。本篇只把这些责任接到外部协议边界，不重做完整 Tool Runtime。
- Transition：角色清楚后，进入 current MCP 让 Client 认识 Server 与 Tool 的第一组机制。

## 6. Section 2｜Discovery 与 Schema：看见候选能力，不等于获得执行许可

- Section Goal：解释 `server/discover`、request `_meta`、tools capability、`tools/list` 与 Tool schema 的证明边界。
- Problem：UI 中出现一个 Tool 名称，很容易被误读为“Server 已认证、Tool 对所有请求都可用、调用一定安全”。
- Claim IDs：`07-C02`、`07-C03`
- Evidence：`07-E02`、`07-E03`；`S-03`、`S-04`、`S-10`、`S-11`。

### 2.1 `server/discover` 是可选 preflight，不是新的 initialize handshake

- MCP `2026-07-28` 中，Server 必须实现 `server/discover`，Client 可以选择调用。
- current request 声明 protocol version 与 client capabilities。
- discovery 返回的能力与 info 用于协议协调；client / server info 是 self-reported，不能被提升成安全身份。
- `server/discover` 不建立 protocol session，不替代每请求版本字段，也不恢复 legacy `initialize / initialized`。
- Guardrail：避免“Client 必须先 discover 才能调用”“discover 完成身份认证”“discover 开启 session”。

### 2.2 `tools/list` 是本次请求可见的 Tool contract

- Server 声明 tools capability 后，必须响应 `tools/list`。
- Tool definition 至少用 name 与有效 JSON Schema `inputSchema` 描述模型可提交的参数；可以有 `outputSchema`。
- Tool list 可以随本次 request authorization 变化，因此不能把一次列表缓存解释成永久授权事实。
- Tool annotations 在非受信 Server 情况下默认不可信，不能替 Host 做安全决策。

### Table 1｜四个“可见”事实不能互换

| 观察 | 可以支持 | 不能单独支持 |
|---|---|---|
| discovery 返回 tools capability | Server 声明支持 Tool feature | 当前用户获准调用某个 Tool |
| tools/list 返回 name | 当前请求可见候选 Tool | implementation 一定存在且安全 |
| inputSchema 有效 | 参数合同可被机器理解 | 参数符合业务 Policy |
| client/server info 存在 | 协议参与者自报信息 | 已验证的安全 identity |

- Boundary Sentence：`Discovered != Authorized`；`Schema Valid != Policy Allowed`。
- Transition：schema 只定义调用候选的形状；下一节追踪调用发出后，错误究竟属于哪一层。

## 7. Section 3｜Call 与 Result：协议错误和 Tool 执行错误为什么不能压成一个失败？

- Section Goal：用最小消息 trace 解释 `tools/call`、result、JSON-RPC error 与 `isError: true`。
- Problem：调用失败若都变成一个“请求失败”，Host 无法区分消息合同不成立，还是 Server 已接收调用但 Tool / business execution 失败。
- Document Thesis：`tools/call` 用 name + arguments 表达调用；request / protocol failure 使用 JSON-RPC error，Tool execution / business failure可以由 result 的 `isError: true` 表达。二者不是同一失败层。
- Claim IDs：`07-C04`
- Evidence：`07-E04`；`S-04`。

### Example 1｜最小消息 trace

> **SPEC-DERIVED ILLUSTRATION / NOT LOCALLY EXECUTED**
> 以下 JSON 由已批准 Research 中的 official specification example 裁剪，用来解释顺序与字段关系。它不是 local MCP runtime observation，也不支持 latency、SDK behavior、interop 或 external side effect claim。

```json
{"jsonrpc":"2.0","id":"d1","method":"server/discover","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"d1","result":{"resultType":"complete","supportedVersions":["2026-07-28"],"capabilities":{"tools":{}},"_meta":{"io.modelcontextprotocol/serverInfo":{"name":"ExampleServer","version":"1.0.0"}}}}
{"jsonrpc":"2.0","id":"l1","method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"l1","result":{"resultType":"complete","tools":[{"name":"get_weather","inputSchema":{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}}]}}
{"jsonrpc":"2.0","id":"c1","method":"tools/call","params":{"name":"get_weather","arguments":{"location":"New York"},"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"c1","result":{"resultType":"complete","content":[{"type":"text","text":"Current weather in New York: ..."}],"isError":false}}
```

### Trace interpretation

这段 trace 只支持：

1. Client 可以请求 discovery；
2. Server 可以返回能力声明；
3. Client 可以列出 Tool schema；
4. Client 可以发出 name + arguments；
5. Client 可以收到 protocol result。

它不支持：

- 用户已经授权；
- Host 已完成 local Policy gate；
- Server 已完成业务域校验；
- External System 的副作用 exactly-once；
- result 已通过 Host validation / shaping / trace；
- 模型已经正确消费结果；
- Agent Loop 或完整 Evidence 已成立。

### Figure 2｜两条失败通道

```text
request / protocol contract failure
  -> JSON-RPC error

Tool accepted but execution / business failure
  -> tools/call result with isError: true
```

- Guardrail：不要把 `isError: false` 写成“业务一定成功且副作用已落地”；本篇没有 runtime observation。
- Boundary Sentence：`Protocol Result != Evidence`。
- Transition：这段 trace 每个 request 都带版本字段；它正好揭示 current MCP 与旧教程最容易混淆的 lifecycle 差异。

## 8. Section 4｜Current 2026-07-28：没有 initialize handshake，也没有 protocol session

- Section Goal：显式纠正 legacy lifecycle 心智，建立 per-request version model。
- Problem：大量旧资料从 `initialize -> initialized` 开始；若直接搬到 current `2026-07-28`，文章会把已移除的 negotiation handshake 写成现行事实。
- Claim IDs：`07-C05`
- Evidence：`07-E05`；`S-01`、`S-02`、`S-03`、`S-10`。

### Mandatory correction box

> MCP `2026-07-28` **没有 `initialize / initialized` negotiation handshake，也没有 protocol session**。每个 request 都声明 protocol version；Server 不支持时返回 `-32022` 与 supported versions，Client 应选择共同版本重试或报错。`initialize / initialized` 只用于 `2025-11-25` 及以前的 legacy era。

### Figure 3｜Modern 与 legacy 不可混写

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

### Version reasoning steps

1. current request 自带 version 与 client capabilities；
2. Server 针对该 request 判断是否支持版本；
3. unsupported 时显式返回 `-32022` 与 supported versions；
4. Client 选择共同版本重试，或向上报告不兼容；
5. `server/discover` 可以帮助 preflight，但不创建 session。

- Claim Scope Guard：`S-12` 只取 component role lines，不扩展 C01，也不代替 C05 的 normative sources。
- SDK Guard：不推断任何 SDK 的默认协商、fallback、cache、retry 或兼容行为。
- Transition：per-request semantics 仍需要 transport 承载；共享 core message 不代表所有 transport 的 framing 与取消动作相同。

## 9. Section 5｜Transport 与 Cancellation：共享语义，不共享所有信号

- Section Goal：区分 core message semantics、transport binding、cancellation race 与 timeout owner。
- Problem：把 stdio 和 HTTP 都称为“同一个 MCP”后，工程实现容易误以为 framing、metadata、cancel signal 与停止保证也完全相同。
- Claim IDs：`07-C06`
- Evidence：`07-E06`；`S-05`、`S-06`、`S-07`、`S-10`。

### Table 2｜共同语义与 transport-specific binding

| 维度 | stdio | Streamable HTTP | 能否扩大推断 |
|---|---|---|---|
| Core messages | 承载相同 MCP request / result / error 语义 | 承载相同 MCP request / result / error 语义 | 不能推出 implementation interop |
| Framing | stdio 对应自身 message framing | HTTP request / response，可涉及 SSE | 不能互抄 framing 行为 |
| Metadata | 依 transport contract 处理 | 有 HTTP metadata / header mirror 要求 | 不等于安全 identity |
| Cancellation signal | 使用 transport 对应的 cancel message | 通过 HTTP / SSE 绑定表达相关 signal | 不保证 External System 已停止 |

### Cancellation reasoning

- cancellation 是 cooperative，且 request completion 与 cancellation 之间可能有 race；
- Server 通常是 SHOULD stop，不是协议保证强制终止任意底层工作；
- timeout 是 implementation SHOULD，需要明确 owner；
- Host 停止等待、Server 收到 cancel、Tool handler 停止与 External System 回滚是不同事实；
- 正文不提供 deadline 精度、强制 kill、network retry 或下游补偿事实。

### Figure 4｜停止请求与工作停止之间仍有边界

```text
Host no longer waits
        |
transport cancellation signal
        |
Server attempts cooperative stop
        |
External System may require its own control
```

- Boundary Sentence：`Cancellation Requested != External Work Stopped`。
- Forbidden Shorthand：“MCP cancel 会杀死 Tool”“HTTP 断开就等于业务回滚”“stdio 比 HTTP 更快 / 更可靠”。
- Transition：transport 还影响凭据与授权流，但 transport authentication 仍不是业务 Permission 的全部。

## 10. Section 6｜Authorization 与 Trust：协议身份信息、Token 与业务权限各守一层

- Section Goal：讲清 optional HTTP authorization、token validation、stdio credential boundary 与 self-reported identity。
- Problem：看到 `clientInfo`、access token 或 scope 后，团队可能把“协议参与者信息”“transport 授权”“具体资源可操作”当成同一事实。
- Claim IDs：`07-C07`
- Evidence：`07-E07`；`S-03`、`S-08`、`S-09`、`S-10`。

### Authorization boundary

- MCP authorization 整体是 optional，并为 HTTP-based transport 定义授权流；
- 采用该 flow 时，Server 必须验证 token 与 intended audience；
- 当 Tool / resource 需要 scope 时，Server 应拒绝 scope 不足的请求；
- stdio 不使用该 HTTP authorization flow，凭据边界可由环境变量等启动环境承载；
- `clientInfo` / `serverInfo` 是 self-reported，不是经过验证的安全 identity。

### Table 3｜不要把四种信息互相替代

| 信息 | 它回答什么 | 它没有自动回答什么 |
|---|---|---|
| client/server info | 对端自报的软件信息 | 调用者真实安全身份 |
| HTTP token | transport flow 中的 credential | 具体业务动作一定获准 |
| token audience / scope | token 面向谁、允许哪些 scope | 参数是否符合 domain rule |
| stdio environment credential | 进程启动边界中的 credential | Host candidate 一定应执行 |

- Security Wording Guard：可以说“MCP 定义可选 authorization flow 和 security guidance”；不能说“MCP 没有 security”。
- Permission Wording Guard：可以说“transport authorization 不能单独推出本课程的完整 Permission”；不能说“有 token 就等于通过所有业务 Policy”。
- Transition：既然 protocol 不会替所有 owner 做同一个决定，下一节给出课程采用的责任分配方式，并明确它只是 proposal。

## 11. Section 7｜课程工程模型：Host Policy + Server Policy 双层 fail-closed

- Section Goal：给出可审查的 owner matrix，让模型候选与真实资源分别在掌握相应上下文的位置被拒绝。
- Status Banner：**`07-C08 = COURSE PROPOSAL`。这是本课程的工程解释，不是 MCP normative requirement，也不是唯一产品架构。**
- Problem：只在 Host 检查会把 Server 当成可信执行代理；只在 Server 检查则让 Host 把不应发生的模型候选直接发送到外部边界。
- Course Thesis：Host 对模型候选、本地上下文与用户意图 fail closed；Server 对调用者、具体资源与 domain constraint 再次 fail closed。任何一层不能判定时，都不把“不知道”降级为允许。
- Claim IDs：`07-C08（PROPOSAL）`
- Evidence：`07-E08`；规范 / guidance 只支持局部责任背景，组合模型来自课程解释。

### Figure 5｜双层拒绝面

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
  deny -> result / error at the appropriate layer
  allow
      |
External System
```

### Table 4｜Course responsibility proposal

| Concern | Host-side proposal | Server-side proposal | Stop condition |
|---|---|---|---|
| Candidate admission | 用户意图、当前会话、local Tool Registry / Policy | 不依赖 Host 已检查的假设 | Host 不确定则不发调用 |
| Identity binding | 选择可接受的 Client / credential context | 验证 token audience、caller / resource relation | 无法绑定则拒绝 |
| Domain validation | 提前拒绝明显越界候选 | 以真实资源与业务规则做最终校验 | 资源或规则不允许则拒绝 |
| Timeout / cancellation | 决定等待预算与 caller intent | 响应 cancel，并管理下游 cooperative stop | 不声称强制终止 |
| Result shaping | 校验并限制进入 model / UI 的内容 | 返回符合协议的 Tool result / error | 无效结果不进入下一层 |
| Audit | 记录候选、Policy decision、correlation | 记录 caller、resource、downstream outcome | 缺少记录不伪称完整 evidence |

### Proposal limitations

- Host 与 Server 可以在一个进程，也可以由不同团队运营；表格不要求物理分离。
- 产品可以把 Policy 放入独立 engine 或合并 owner；本篇只要求责任显式且 fail closed。
- 此模型不提供完整 Permission schema、audit schema、retry policy、compensation 或 production architecture。
- 不将 Article06 的完整 Tool Runtime pipeline复制到 MCP Server；只继承“每层都有 owner、失败后停止”的判断。
- 不声明该模型已经过 local runtime 验证。

- Boundary Sentence：`Host Approval != Server Authorization`；反向也成立。
- Transition：最后把协议成功与课程其他概念逐项拆开，防止在收尾时又把它们合成一句“Agent 已完成”。

## 12. Section 8｜协议成功之后，哪些结论仍然不能单独推出？

- Section Goal：用 C09 的窄边界关闭全文，不把“规范范围有限”扩大成“实现一定缺失”。
- Status Banner：**`07-C09 = PARTIAL`。只允许 scoped wording，不得升级为普遍否定。**
- Claim IDs：`07-C09（PARTIAL）`
- Evidence：`07-E09`；spec scope + course glossary / Article05—06 boundary。

### Mandatory narrow wording

> MCP discovery / call / result 的协议成功，**不能由这些 MCP 消息单独推出** Agent Loop、Permission、Article06 Tool Runtime gates 或完整 Evidence 已经成立。某个产品可以在 MCP 外另行实现这些层；本篇不否定这种实现。

### Table 5｜消息成功与工程完成不是同一证明

| 成功观察 | 仍需独立回答 | Stop line |
|---|---|---|
| server/discover 成功 | Host / Server 是否信任自报信息 | 不推出 secure identity |
| tools/list 成功 | 本次候选是否应执行 | 不推出 Permission |
| tools/call 返回 result | external side effect 与业务状态是什么 | 不推出 exactly-once 或业务成功 |
| `isError: false` | Host 是否校验、整形、记录并正确回送模型 | 不推出完整 Tool Runtime |
| 一次完整 trace | loop 是否继续、何时终止、状态如何推进 | 不推出 Agent |
| protocol log 存在 | observation 如何绑定 claim、来源与限制 | 不推出完整 Evidence |

### Adjacent concept boundaries

- Article05：Tool Call 是候选，不等于 executed / authorized / evidence。
- Article06：ToolDefinition 不是 function；Schema Valid 不等于 Policy Allowed；Result / Trace 不等于 Evidence；Sandbox 不等于 Permission。
- Article08：Agent Loop、state 与 termination 才开始 formalize；本篇只留自然语言桥，不创建文件或链接。
- Later Permission / Evidence articles：分别 formalize授权与 claim closure；本篇不提前定义完整模型。

- Forbidden Expansions：
  - “MCP 不是 Tool Runtime，所以 MCP Server 不可能有 Runtime”；
  - “MCP 没有 Permission / Security”；
  - “接 MCP 的产品都不是 Agent”；
  - “只要外加双层 Policy 就获得完整 Agent 与 Evidence”。

- Transition：边界建立后，给读者一套对架构图、trace 与发布声明都适用的审查顺序。

## 13. Section 9｜怎样审查一条 MCP 外部能力链？

- Section Goal：把全文转换成可执行的设计 / Review 问题，不新增机制事实。
- Claim Coverage：综合 `07-C01`—`07-C09`。

### Nine-question review sequence

1. 图中是否同时保留 Host Tool Runtime、Client / Transport、Server、External System，而非只画一个“MCP”方框？
2. current baseline 是否明确为 `2026-07-28`，并使用同版本 architecture page、且只把它用于角色来源？
3. discovery / tools/list 的能力声明是否被误写成 secure identity 或永久授权？
4. Tool schema validation 与 Host / Server Policy 是否仍被区分？
5. JSON-RPC protocol error 与 result `isError: true` 是否进入不同失败解释？
6. 文档是否错误保留 current `initialize / initialized` handshake 或 session？
7. stdio / HTTP 的 framing、metadata、cancel signal 是否被当成完全相同，或 cancellation 被写成强制终止？
8. transport authorization 是否被误当成业务 Permission，Host 与 Server 的 decision owner 是否显式？
9. success trace 是否被过度推成 Agent Loop、完整 Tool Runtime、Permission 或 Evidence closure？

### Review output shape

每条审查结论都写成：

```text
Observed protocol fact
  -> supported interpretation
  -> missing owner or evidence
  -> allowed / must stop
```

- Example Review Sentence：`tools/list 返回了 delete_build schema` 只支持候选能力可见；Host 是否允许 production target、Server 是否允许该 caller 操作该 artifact 仍需独立 owner，因此当前不得写“删除权限已获得”。
- Transition：以最短结论、可判定 Learning Check 和后续自然语言桥收尾。

## 14. Closing Plan

- Recap 1：MCP 把 Client / Server 之间的 discovery、schema、call、result 与 error 标准化。
- Recap 2：current `2026-07-28` 通过每请求版本字段工作，不再使用 legacy initialize handshake / session。
- Recap 3：transport、authorization 与 cancellation 有明确规范边界，但不会自动关闭业务 Policy 与 external side effect。
- Recap 4：课程用双层 fail-closed proposal 分配 Host / Server owner；协议成功仍不能单独证明 Agent / Permission / Runtime / Evidence。
- Shortest Conclusion：`MCP 让外部能力可连接；工程是否可执行、可拒绝、可追责，仍取决于 Host、Server 与外部系统各自没有被省略的责任。`
- Next Bridge：下一篇才进入 Agent Loop、state 与 termination；本篇停在 protocol mapping，不链接、不创建 Article08 workspace。

## 15. Figures / Tables / Example Plan

| ID | Placement | Form | Teaching Purpose | Claim Coverage | Evidence Label |
|---|---|---|---|---|---|
| Figure 1 | Section 1 | responsibility chain | 把 Host / Client / Transport / Server / External System 拆开 | `07-C01` | official roles + course mapping |
| Table 1 | Section 2 | capability boundary | 区分 capability、schema、authorization、identity | `07-C02`、`07-C03` | spec-derived |
| Figure 2 | Section 3 | two error lanes | 区分 protocol failure 与 Tool execution failure | `07-C04` | spec-derived |
| Example 1 | Section 3 | six-line JSON trace | 展示 discover -> list -> call -> result | `07-C02`—`07-C04` | `SPEC-DERIVED / NOT LOCALLY EXECUTED` |
| Figure 3 | Section 4 | modern / legacy sequence | 纠正 current handshake / session 误读 | `07-C05` | versioned spec-derived |
| Table 2 + Figure 4 | Section 5 | transport matrix + cancel chain | 区分语义、binding 与 cooperative stop | `07-C06` | spec-derived |
| Table 3 | Section 6 | trust matrix | 拆开 self-report、token、scope、business policy | `07-C07` | spec + guidance |
| Figure 5 + Table 4 | Section 7 | fail-closed model + owner matrix | 给出课程 proposal 与限制 | `07-C08` | `COURSE PROPOSAL` |
| Table 5 | Section 8 | implication boundary | 阻止 protocol success 过度推断 | `07-C09` | `PARTIAL / COURSE BOUNDARY` |

### Example discipline

- 贯穿例子只承担结构解释，不提供 runtime output、性能数据或互操作结论。
- `delete_build` 只作为 Policy 问题，不描述实际 SDK、Server implementation 或 external API behavior。
- `get_weather` JSON 保持 official illustration 的 illustrative data，不把天气结果当 Observation。
- 不新增 Provider、Agent Loop implementation、Permission system、complete Tool Runtime 或 Evidence workflow 示例。

## 16. Learning Check Plan

| # | 判定题 / 任务 | Claim Coverage | Pass criterion |
|---|---|---|---|
| 1 | 画出 MCP 外部能力责任链，并标出 wire message 的直接双方 | `07-C01` | Host Tool Runtime -> Client / Transport -> Server -> External System；Client / Server 是直接协议角色 |
| 2 | `server/discover` 返回 tools capability，能否证明当前用户有权调用任意 Tool？ | `07-C02` | 不能；capability 与 info 是协议声明，identity self-reported，授权另判 |
| 3 | `tools/list` 返回有效 inputSchema，能否证明参数符合业务 Policy？ | `07-C03` | 不能；Schema contract 与 Policy decision 分层 |
| 4 | 区分 unknown / malformed request 与 Tool business failure 的表达层 | `07-C04` | JSON-RPC error vs result `isError: true` |
| 5 | 写出 current `2026-07-28` 的版本处理，并指出旧 handshake 适用期 | `07-C05` | per-request version；`-32022` + supported versions；legacy <= `2025-11-25` |
| 6 | 收到 cancellation signal 后，能否断言 external work 已停止？ | `07-C06` | 不能；cooperative / racy，Server SHOULD stop，不保证强制终止 |
| 7 | 有效 HTTP token 或 clientInfo 能否直接充当业务 Permission？ | `07-C07` | 不能；token audience/scope、self-report 与 domain authorization 分层；stdio flow 不同 |
| 8 | 为 Host / Server 分配 candidate、identity、domain、cancel、result、audit owner | `07-C08` | 双层 fail-closed，且明确标为 COURSE PROPOSAL / 非唯一架构 |
| 9 | 一段 discover/list/call/result 成功 trace 能否证明 Agent、Permission、Runtime、Evidence？ | `07-C09` | 不能由消息单独推出；不否定产品在协议外实现这些层 |

### Learning Check answer constraints

- 每道题必须能用正文已有模型作答，不要求安装 SDK 或运行 Server。
- 第 8 题答案必须保留 proposal 标签。
- 第 9 题答案必须保留 partial / narrow wording。
- 不设置 latency、throughput、network retry、interop 或 production security 题目。

## 17. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence ID / Sources | Semantic Guard |
|---|---|---|---|---|
| `07-C01` | `CONFIRMED` | Section 1、Figure 1、Learning 1 | `07-E01 / S-05, S-11, S-12` | `S-12` 为 `2026-07-28` teaching overview，只取 component role，不代替 normative spec |
| `07-C02` | `CONFIRMED` | Section 2.1、Example 1、Learning 2 | `07-E02 / S-03, S-10` | Server MUST discover；Client MAY call；self-report 非安全身份 |
| `07-C03` | `CONFIRMED` | Section 2.2、Table 1、Learning 3 | `07-E03 / S-04, S-11` | tools/list / schema 不等于业务授权；annotations 不默认可信 |
| `07-C04` | `CONFIRMED` | Section 3、Figure 2、Learning 4 | `07-E04 / S-04` | JSON-RPC error 与 `isError: true` 是不同失败层 |
| `07-C05` | `CONFIRMED` | Section 4、Figure 3、Learning 5 | `07-E05 / S-01, S-02, S-03, S-10` | current 无 handshake/session；legacy 只到 `2025-11-25` |
| `07-C06` | `CONFIRMED` | Section 5、Table 2、Figure 4、Learning 6 | `07-E06 / S-05, S-06, S-07, S-10` | cancel cooperative / racy；不保证强制终止 |
| `07-C07` | `CONFIRMED` | Section 6、Table 3、Learning 7 | `07-E07 / S-03, S-08, S-09, S-10` | optional HTTP auth；stdio不使用此flow；self-report非identity |
| `07-C08` | `PROPOSAL` | Section 7、Figure 5、Table 4、Learning 8 | `07-E08 / S-03, S-04, S-07—S-09 + course interpretation` | 双层 fail-closed 是 COURSE PROPOSAL，非 MCP 唯一架构 |
| `07-C09` | `PARTIAL` | Opening、Section 8、Table 5、Learning 9 | `07-E09 / S-02—S-09 + course boundary` | 只能写“不能由 MCP 消息单独推出”；不否定外部实现 |

Coverage Result：`9 / 9 Claims semantically mapped`；状态保持 `7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。

## 18. Job Competency Mapping

| Competency | 可观察能力 | Article Placement | Pass Signal |
|---|---|---|---|
| Boundary architecture | 能拆开 Host、protocol、transport、Server 与 External System | Section 1 + Learning 1 | 架构图不把 MCP 画成全部 Runtime |
| Version discipline | 能识别 current / legacy contract，并对齐 teaching docs 与 normative spec 的版本和职责 | Section 4 + Learning 5 | 明确 current无handshake/session与per-request version |
| Contract modeling | 能区分 discovery、schema、call、result、两类error | Sections 2—3 + Learning 2—4 | 每个消息事实都有proof boundary |
| Distributed responsibility | 能区分 transport binding、cancel request 与 external work stop | Section 5 + Learning 6 | 不把cooperative cancel写成强制终止 |
| Security judgment | 能拆开self-report、credential、scope与domain permission | Section 6 + Learning 7 | 不用token或clientInfo替代完整业务授权 |
| Policy design | 能给Host与Server分配双层fail-closed owner | Section 7 + Learning 8 | proposal标签、deny owner、limitation齐全 |
| Evidence literacy | 能从success trace识别缺失claim evidence | Section 8—9 + Learning 9 | 使用“不能单独推出”，保留产品外部实现可能 |

Job-level Signal：读者不只会“接一个 MCP Server”，而是能在版本变化、跨进程消息、安全上下文与外部副作用之间做有证据边界的责任划分。

## 19. Explicit Non-scope and Adjacent Stop Lines

| Adjacent / Future Topic | Article 07 May Introduce | Article 07 Must Stop Before |
|---|---|---|
| MCP SDK / Runtime | 用 protocol method 与字段解释 contract | 任一 SDK default、fallback、cache、retry、runtime behavior |
| Provider / Model Adapter | Host 中存在模型候选背景 | Provider lifecycle、adapter实现、error mapping扩写 |
| Article 06 Tool Runtime | 继承 schema / policy / result / trace seam | 复制完整 Runtime pipeline、Lab行为、实现代码 |
| Article 08 Agent Loop | 只说 protocol success不能单独证明Agent | loop、state、termination、planner / executor、Article08 workspace |
| Permission | 区分 transport auth 与 business permission | 完整 permission model、consent UX、policy language |
| Evidence | 说明result/log不是claim closure | provenance schema、evidence pipeline、claim adjudication流程 |
| Security architecture | token audience/scope与官方guidance边界 | threat model大全、production hardening或合规保证 |
| Transport engineering | stdio / HTTP语义与binding差异 | latency、benchmark、network retry、load balancing、interop |
| External System | 承认真实资源与副作用存在 | 任一具体外部API、transaction、exactly-once或compensation事实 |
| Lab | 使用spec-derived illustrative trace | local MCP run、fixture、asset、provider call、runtime observation |

### Mandatory stop sentences

- 到 `MCP Client / Transport / Server` 为止，不把协议连接扩写成完整 Agent Runtime。
- 到“authorization flow 与 business Permission 分层”为止，不提前 formalize Permission。
- 到“result / trace 不能单独成为 Evidence”为止，不提前 formalize Evidence closure。
- 到“下一篇进入 Agent Loop”为止，不创建、不链接 Article08 文件。
- 需要任一未批准 SDK、runtime、latency、interop、external side effect 核心事实时，立即 `RETURN_TO_RESEARCH`。

## 20. Source / Link Plan

### External source whitelist

Draft 只允许使用下列 12 个 external URLs；不得引入搜索结果、SDK README、博客二手文、旧版 specification URL 或新的 external source。

| Source ID | Link | Allowed Use | Boundary |
|---|---|---|---|
| `S-01` | [MCP 2026-07-28 release](https://blog.modelcontextprotocol.io/posts/2026-07-28/) | current release、stateless core、removed handshake/session | 不代替每项 normative spec |
| `S-02` | [Versioning and Compatibility](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/versioning.mdx) | per-request version、`-32022`、modern / legacy | 不证明SDK默认协商行为 |
| `S-03` | [Discovery](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/discover.mdx) | discover contract、capability、self-reported info | 不证明安全identity |
| `S-04` | [Tools](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/tools.mdx) | tools/list、schema、call、result、error | 不证明业务授权或side effect |
| `S-05` | [Transport overview](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/index.mdx) | semantics vs binding、message direction | 不证明custom transport安全 |
| `S-06` | [Streamable HTTP](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/streamable-http.mdx) | HTTP framing、metadata、SSE cancellation | 不证明downstream work停止 |
| `S-07` | [Cancellation](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/patterns/cancellation.mdx) | transport-specific cancel、timeout、race | 不保证强制终止或精确deadline |
| `S-08` | [Authorization](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/authorization/index.mdx) | optional HTTP auth、audience、scope、errors | 不等于Agent Policy或完整业务Permission |
| `S-09` | [Security best practices](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/tutorials/security/security_best_practices.mdx) | confused deputy、token passthrough、trust / audit risk | 不自动生成完整audit架构 |
| `S-10` | [Versioned schema](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2026-07-28/schema.ts) | `_meta`、unsupported version、cancel types | 不证明SDK实现一致性 |
| `S-11` | [Versioned server concepts](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/server-concepts.mdx) | Server / tools teaching model | 不代替normative spec |
| `S-12` | [Versioned core architecture](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/architecture.mdx) | component-definition lines only | docs `2026-07-28`；teaching overview 不代替 normative spec |

### Local source and navigation plan

| Purpose | Link | Use Constraint |
|---|---|---|
| Formal vocabulary | [Course Glossary](../../glossary.md) | 沿用Tool / Host / Evidence等课程词义，不提前formalize Agent Loop |
| Previous boundary | [Published Article 05](../../../../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md) | 只继承Tool Call / Schema / Result边界 |
| Immediate prerequisite | [Published Article 06](../../../../content/ai-empowerment/agent-engineering-06-tool-runtime.md) | 只继承Host Runtime责任与stop lines，不复制Lab |
| Canonical row | [Agent Engineering Series Plan](../../../agent-engineering-series-plan.md) | identity、Part II、M、non-optional |
| Frozen scope | [v3.1 Course Plan](../../../agent-engineering-course-plan-v3.1-review.md) | Article07 responsibility/questions/non-goals |
| Current card | [Article 07 Card](article-card.md) | positioning、dependency、content spine |
| Current research | [Article 07 Research](research.md) | stable model、current lifecycle、Source Manifest |
| Approved evidence | [Article 07 Evidence](evidence.md) | 9 Claims / 9 Cards、wording、limitations |

### Link guards

- 不添加 Article08 link、relref、workspace path 或 placeholder。
- Local links 全部相对当前 Article07 workspace，并在 Outline Gate 验证存在。
- Published links只指向已存在 Article05 / Article06 Markdown source；Outline不使用Hugo shortcode。
- External URL 必须逐字属于以上 whitelist。

## 21. Length Budget

| Part | Target | Compression Rule |
|---|---:|---|
| Opening + thesis | 450—600 字 | 只用一个“接了MCP”误判场景 |
| Abstract responsibility model | 650—850 字 | 图后只解释role / owner差异 |
| Discovery + schema | 750—950 字 | 不列完整schema字段 |
| Call + result + trace | 750—950 字 | 只保留一段六消息trace |
| Current version correction | 550—750 字 | legacy只作一屏对照 |
| Transport + cancellation | 650—850 字 | 不扩写network工程 |
| Authorization + trust | 650—850 字 | 不设计完整Permission系统 |
| Double fail-closed proposal | 700—900 字 | owner matrix替代重复散文 |
| Boundary review + closing | 550—750 字 | 用审查表与最短结论收口 |

Budget Result：Draft target约 `5,000—6,500 中文字`。超预算时优先压缩字段复述、重复guard、legacy背景与图注；不得删除 current lifecycle纠偏、spec-derived标签、C08 proposal标签、C09 narrow wording、9/9 Claim coverage、Learning Check或stop lines。

## 22. New Core Facts Audit

| Outline Element | Classification | Existing Support / Decision |
|---|---|---|
| “接了MCP”完成错觉 | problem-space synthesis | Article Card + frozen questions + `07-C08` / `07-C09` |
| Host -> Client / Transport -> Server -> External System | approved abstract model | Article Card + Research stable model + `07-C01` |
| discovery / schema / call / result | approved protocol mechanisms | `07-C02`—`07-C04` |
| current no handshake / session | approved version correction | `07-C05` |
| stdio / HTTP + cooperative cancel | approved transport boundary | `07-C06` |
| optional HTTP auth + identity boundary | approved authorization boundary | `07-C07` |
| double fail-closed owner matrix | editorial expansion of approved proposal | `07-C08 PROPOSAL`；不升级状态 |
| MCP success implication matrix | editorial expansion of approved partial boundary | `07-C09 PARTIAL`；只用narrow wording |
| JSON trace | exact approved Research example | `SPEC-DERIVED / NOT LOCALLY EXECUTED` |
| Figures、section order、Learning Check、length | editorial planning metadata | 不构成technical behavior Claim |

New Core Facts Result：`0`。

### RETURN_TO_RESEARCH decision

- Decision：`NOT_REQUIRED`。
- Reason：问题空间、抽象模型、current concrete mechanisms、engineering proposal、partial boundary、figures/examples 与 Learning Check 均由 `07-C01`—`07-C09` 覆盖，没有引入新的 SDK / runtime / latency / interop / external behavior Claim。
- Mandatory Return Triggers：Draft若需要以下任一项才能成立，必须停止并返回 `RETURN_TO_RESEARCH`：
  1. 某 MCP SDK 的初始化、默认重试、版本fallback、cache或错误行为；
  2. 任一 local / provider MCP runtime observation；
  3. stdio / Streamable HTTP latency、throughput、可靠性或互操作结论；
  4. external system transaction、side effect、exactly-once或compensation事实；
  5. 把双层fail-closed从proposal升级为normative claim；
  6. 把C09扩写成对所有产品实现的普遍否定；
  7. 新的Agent Loop、Permission、完整Tool Runtime或Evidence closure Claim。

## 23. Outline Gate Checklist and Recommendation

- [x] 唯一 H1 与 canonical Article07 标题一致。
- [x] 文章从问题空间进入，经抽象模型，再到 concrete mechanism 与工程判断。
- [x] Teaching Spine 完整覆盖 problem -> model -> mechanism -> judgment -> verification。
- [x] MCP baseline 固定为 `2026-07-28`，checked date为 `2026-08-20`。
- [x] current 无 `initialize / initialized` handshake / protocol session 的纠偏显式且独立成节。
- [x] `S-12` 使用 `2026-07-28` versioned architecture page，且只用于 component role。
- [x] discovery、schema、call/result、version、transport/cancel、authorization均有 placement。
- [x] 最小消息 trace 标为 `SPEC-DERIVED ILLUSTRATION / NOT LOCALLY EXECUTED`。
- [x] `9 / 9 Claims semantically mapped`。
- [x] `07-C01`—`07-C07` 保持 `CONFIRMED`。
- [x] `07-C08` 保持 `COURSE PROPOSAL`，双层 fail-closed 不伪装成 MCP normative requirement。
- [x] `07-C09` 保持 `PARTIAL`，只写“不能由 MCP 消息单独推出”。
- [x] 新核心事实=`0`，无需 `RETURN_TO_RESEARCH`。
- [x] M-level Draft budget、Figures、Examples、Learning Check、Job Competency齐全。
- [x] external source只使用12项whitelist；local source / navigation plan齐全。
- [x] SDK behavior、runtime、latency、interop、Provider、Agent Loop、Permission、complete Tool Runtime、Evidence closure均停在边界外。
- [x] Required Lab=`NONE`；没有把spec example登记为runtime evidence。
- [x] 不创建draft、assets、Published Content或Article08 workspace。
- [x] Author不修改Lifecycle / Gate状态，不自批准Outline Gate。

Recommendation：`PASS_RECOMMENDED`。由 Master 独立核对 Outline Gate；若通过，唯一下一动作是 `AUTHOR_DRAFT`，由 Author只依据本 Outline 与已批准 Evidence创建当前 Article 的 `draft.md`。本轮在 Outline 停止，不进入 Review、Publish、Build或Article08。
