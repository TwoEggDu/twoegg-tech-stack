# Article 07 Research｜MCP 与外部能力边界

- Research Phase：`RESEARCH / EVIDENCE_GATE`
- Research Status：`COMPLETE / PASS_CANDIDATE`
- Lifecycle Candidate：`EVIDENCE_READY`
- Evidence Gate Recommendation：`PASS / NEXT_OUTLINE`
- Required Lab：`NONE`
- Research Window：`2026-08-20（Asia/Shanghai）`
- Protocol Baseline：`MCP 2026-07-28`
- Official Sources Rechecked At：`2026-08-20`
- Provider / external runtime calls：`NONE`
- Local MCP runtime execution：`NONE`
- Runtime Evidence：`NONE / NOT_REQUIRED`
- Claim Summary：`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`

> 本轮只做 current primary-source research，没有启动 MCP Client / Server、Provider、network trace 或本地 Lab。文中的 JSON-RPC 片段是由 MCP `2026-07-28` 官方示例裁剪出的 `SPEC-DERIVED ILLUSTRATION`，不是本机运行 Observation。

## Scope and method

本篇接住 Article 05—06 的两条课程边界：模型产出的 Tool Call 只是结构化行动候选；Host Tool Runtime 仍要完成 route、validate、policy、execute、result shaping 与 trace。Article 07 只研究 MCP 在这条链路上怎样标准化 Client 与 Server 之间的 discovery / schema / call / result，以及协议没有替 Host 或 Server 关闭哪些工程责任。

研究顺序按“问题空间 -> 抽象模型 -> 协议机制 -> 工程边界”收敛：

1. 先用官方架构材料定位 Host、Client、Server 与 Transport；Host 只作为应用边界，不当成 wire message 的发送角色。
2. 以 versioned MCP `2026-07-28` specification 为协议事实基线；release blog 只确认发布日期与迁移风险。
3. 对 discovery、Tool schema、call/result、versioning、transport、cancellation、authorization 与 security 分别建立窄 Claim。
4. 对 Host Policy、Server Policy、domain validation、timeout、identity 与 audit，逐项标成规范保证、transport / implementation scope 或课程解释。
5. 不因“官方文档出现建议”把 `SHOULD` 扩大成 `MUST`，也不因 MCP 有 authorization flow 就写成业务权限或 Agent Policy 已闭合。

## Research Question Answers

| RQ | Status | Answer | Claim / Evidence |
|---|---|---|---|
| `RQ-01` | `ANSWERED / SCOPED` | 官方架构把 Host 定义为承载 MCP Client 的 LLM application；Client 与 Server 通过 transport 交互。current wire specification 的直接消息角色是 Client / Server，Host Policy 是课程放在 Client 外侧的应用责任。 | `07-C01 / 07-E01` |
| `RQ-02` | `ANSWERED / SPEC` | `2026-07-28` 中 Server 必须实现 `server/discover`，Client 可选调用；每个请求携带 protocol version 与 client capabilities。支持 tools 的 Server 声明 tools capability，并通过 `tools/list` 返回当前请求者可见的 schema。 | `07-C02, 07-C03 / 07-E02, 07-E03` |
| `RQ-03` | `ANSWERED / SPEC` | `tools/call` 用 name + arguments 调用；成功或可恢复的 Tool execution failure 都在 result 中表达，而 malformed / unknown tool 等 protocol failure 使用 JSON-RPC error。 | `07-C04 / 07-E04` |
| `RQ-04` | `ANSWERED / VERSIONED` | current `2026-07-28` 没有 initialize handshake 或 protocol session；版本由每请求 `_meta` 声明，unsupported version 返回 `-32022` 后重试共同版本。旧 `initialize / initialized` 只属于 `2025-11-25` 及以前的 legacy era。 | `07-C05 / 07-E05` |
| `RQ-05` | `ANSWERED / TRANSPORT-SCOPED` | stdio 与 Streamable HTTP 共享消息语义，但 framing、metadata mirror 与 cancellation signal 不同。取消是 cooperative / racy；timeout 是 implementation SHOULD，不能写成协议强制终止外部工作。 | `07-C06 / 07-E06` |
| `RQ-06` | `ANSWERED / AUTH-SCOPED` | MCP authorization 规范只定义 HTTP-based transport 的授权流且整体可选；Server 仍须验证 token 与 intended audience，并在有 scope 限制时拒绝不足 scope。`serverInfo` / `clientInfo` 是 self-reported，不能作为安全身份。 | `07-C07 / 07-E07` |
| `RQ-07` | `ANSWERED_AS_PROPOSAL` | 课程采用 Host Policy 与 Server Policy 两层 fail-closed：Host 决定是否允许模型候选进入调用，Server 对调用者与具体资源重做 authn/authz/domain validation。timeout、result shaping 与 audit 也要有显式 owner。此分工是课程工程解释，不是 MCP 规定的唯一架构。 | `07-C08 / 07-E08` |
| `RQ-08` | `ANSWERED / PARTIAL_BOUNDARY` | MCP 只证明标准化消息交换可发生；它不自动证明 Agent loop、用户/业务 Permission、Article 06 Tool Runtime 的本地 gates 或完整 Evidence。这个结论由规范正面范围和课程词汇边界共同约束，故保留 `PARTIAL`，不写成对所有实现的否定事实。 | `07-C09 / 07-E09` |

## Stable boundary model

```text
Agent / Host application
  Host Policy + user intent + local Tool Runtime + audit
                     |
                  MCP Client
                     |
       stdio or Streamable HTTP transport
                     |
                  MCP Server
  Server Policy + caller/resource validation + downstream control
                     |
               External System
```

协议能标准化的是中间的消息边界：capability / schema / call / result / protocol error。上图两侧的 Policy、业务语义和审计闭环不是因为“接入 MCP”就自动成立。

### Responsibility matrix

| Concern | MCP `2026-07-28` specification guarantee | Transport / implementation scope | Course interpretation |
|---|---|---|---|
| Client / Server message roles | JSON-RPC request / notification 从 Client 到 Server，response / notification 从 Server 到 Client；Server 不发起 JSON-RPC request。 | Host 怎样创建、复用、隔离 Client 由应用决定。 | Host 在 Client 外侧持有 Agent / user / policy context。 |
| Capability discovery | Server `MUST` implement `server/discover`；Client `MAY` call；Tool Server `MUST` declare tools capability and answer `tools/list`。 | Client 可先 discover，也可 inline call 后处理 version error；list 可缓存且可按 request authorization 变化。 | discovery 只形成候选能力集，不能当授权决定。 |
| Tool schema | `tools/list` 提供 name、description、valid JSON Schema input；output schema 可选；annotations 默认不可信。 | Schema 的业务完整性、描述质量与聚合重名处理由实现负责。 | Host 仍应执行 DTO / domain / policy gates；Server 也不能只相信 schema 已过。 |
| Tool call / result | `tools/call`、complete / input-required result、JSON-RPC protocol error 与 `isError: true` Tool execution error 有明确形状。 | External side effect、retry / idempotency、result truncation / redaction 不由这一消息成功自动保证。 | 沿用 Article 05—06：Call != Executed，Result != Evidence。 |
| Version / lifecycle | modern request 每次声明版本与能力；不支持返回 `-32022`；`2026-07-28` 无 initialize session。 | dual-era fallback 与缓存探测结果有 transport-specific 规则。 | 文章必须显式写协议版本，禁止把 legacy handshake 当 current lifecycle。 |
| Cancellation / timeout | transport 定义取消信号；Server 对取消通常是 `SHOULD` 停止，存在完成竞态。 | timeout、最大时限、底层操作是否可取消由 SDK / Server / downstream 决定。 | Host 与 Server 都记录 timeout / cancellation source，失败关闭，不能声称强杀。 |
| Authorization / identity | HTTP authorization flow 可选；采用时有 token、audience、scope 等规范要求。 | stdio 凭据来自环境；custom transport 使用自身安全实践；server/client info 是自报。 | Host user approval / Agent Policy 与 Server resource authorization 分开建模。 |
| Audit | security guidance说明错误 token forwarding 会破坏 accountability / audit trail。 | 协议不提供一份自动完整的跨 Host、Server、downstream 审计记录。 | 两侧以 correlation / invocation identity 对齐各自 trace；这仍需实现和验证。 |

## Current lifecycle and version boundary

`2026-07-28` 是本轮核对日 `2026-08-20` 的 current released specification。该版是重大断点：

```text
modern 2026-07-28+
  request(_meta.protocolVersion + clientCapabilities)
    -> result
  or UnsupportedProtocolVersionError(-32022)
    -> choose mutual version and retry

legacy <=2025-11-25
  initialize -> initialize result -> notifications/initialized
```

因此本篇只能把“每请求版本声明 + 可选 server/discover + error/retry”写成 current lifecycle。兼容旧 Server 时，stdio 与 HTTP 怎样探测 / fallback 是 transport-specific；不能把 SDK 的 auto fallback 写成所有 Client 的协议保证。

## Capability, schema and identity boundary

1. `server/discover` 返回 supported versions、capabilities、optional instructions、cache metadata 与 self-reported server info。
2. 支持 tools 的 Server 通过 `tools/list` 返回当前 requesting client 可见的 tools；集合可按本次请求携带的 authorization 变化。
3. Tool `inputSchema` 必须是有效 JSON Schema object，未声明 `$schema` 时默认 JSON Schema 2020-12；`outputSchema` 可选。
4. Tool annotations 在 trusted Server 之外必须按 untrusted metadata 处理。
5. `serverInfo.name` 不保证全局唯一；client/server info 都是自报信息，不能作为安全身份或授权依据。

这里的 capability 表示“Server 声明其能处理的协议 surface”，schema 表示“调用参数的机器可读形状”。二者都不等于“这个 Agent 现在被允许执行此业务动作”。

## Tool call, result and error boundary

current spec 把两类失败明确分开：

- Protocol error：unknown tool、malformed request、server error，使用 JSON-RPC `error`。
- Tool execution error：API failure、input validation、business logic failure，放在 Tool result 中并标记 `isError: true`，便于模型调整输入。

这一区分只说明 wire representation；它不证明 Server 没有产生部分副作用，也不规定 Host 的 retry、idempotency、redaction、spill 或审计策略。

## Minimum official message trace

下列片段由 `server/discover` 与 Tools 规范官方示例裁剪并补齐 current request `_meta`。用途是解释协议顺序，不是本地 runtime 证据；示例中的名称、版本、天气结果均为 illustrative data。

```json
{"jsonrpc":"2.0","id":"d1","method":"server/discover","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"d1","result":{"resultType":"complete","supportedVersions":["2026-07-28"],"capabilities":{"tools":{}},"_meta":{"io.modelcontextprotocol/serverInfo":{"name":"ExampleServer","version":"1.0.0"}}}}
{"jsonrpc":"2.0","id":"l1","method":"tools/list","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"l1","result":{"resultType":"complete","tools":[{"name":"get_weather","inputSchema":{"type":"object","properties":{"location":{"type":"string"}},"required":["location"]}}]}}
{"jsonrpc":"2.0","id":"c1","method":"tools/call","params":{"name":"get_weather","arguments":{"location":"New York"},"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientCapabilities":{}}}}
{"jsonrpc":"2.0","id":"c1","result":{"resultType":"complete","content":[{"type":"text","text":"Current weather in New York: ..."}],"isError":false}}
```

这条 trace 只支持：发现协议/能力、列出 schema、发出调用、收到结果。它不支持：用户已授权、Server 已做业务校验、底层副作用 exactly-once、Host 已完成 result validation / trace，或模型已正确使用结果。

## Counter-evidence and version risks

1. `S-12` 的 `2026-07-28` architecture page 可用于 Host / Client / Server 的组件定位；C01 仍只采用角色定义，不把 teaching overview 提升为 normative specification。
2. `server/discover` 对 Server 是 MUST、对 Client 是 MAY；“所有调用前都必须 discover”是错误扩大。
3. capabilities、tool list、schema、annotation 与 server info 都是描述/协商 surface，不是业务授权凭据；tool list 甚至可按本次 request authorization 变化。
4. HTTP authorization 是 optional protocol feature，且只覆盖 HTTP-based transport flow；stdio 明确走环境凭据，不存在一套跨 transport 的统一 OAuth 保证。
5. `SHOULD stop`、timeout SHOULD 与 cancellation race 不能改写成“取消后工作必然停止”。
6. JSON-RPC success / Tool result 不证明 external side effect、idempotency、redaction、audit 或 Evidence closure。
7. 本轮没有运行 MCP Server；任何 latency、SDK interoperability、network retry 或 auth provider 行为都保持 `NOT_OBSERVED`。

## Source Manifest

所有网页均在 `2026-08-20（Asia/Shanghai）` 实际打开核对。规范与架构资料均固定到 `2026-07-28`；architecture page 只用于角色定义。

| ID | Primary / local source | Version / retrieved scope | Used for | Does not prove |
|---|---|---|---|---|
| `S-01` | [MCP 2026-07-28 release](https://blog.modelcontextprotocol.io/posts/2026-07-28/) | released `2026-07-28`；checked `2026-08-20` | current release、stateless core、removed handshake/session | 不代替每项 normative spec |
| `S-02` | [Versioning and Compatibility](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/versioning.mdx) | spec `2026-07-28` | per-request version、`-32022`、modern / legacy / dual-era | 不证明某 SDK 默认 negotiation mode |
| `S-03` | [Discovery](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/discover.mdx) | spec `2026-07-28` | `server/discover` contract、capability / identity / caching | self-reported info 不是安全身份 |
| `S-04` | [Tools](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/tools.mdx) | spec `2026-07-28` | tools capability/list/schema/call/result/error | 不证明业务授权、side effect 或 Host Runtime |
| `S-05` | [Transport overview](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/index.mdx) | spec `2026-07-28` | semantics vs binding、stdio / HTTP、message direction | 不证明 custom transport 安全 |
| `S-06` | [Streamable HTTP](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/streamable-http.mdx) | spec `2026-07-28` | HTTP headers、status、SSE cancellation | 不证明 downstream work 已停止 |
| `S-07` | [Cancellation](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/patterns/cancellation.mdx) | spec `2026-07-28` | transport-specific cancel、timeouts、race | 不保证强制终止或精确 deadline |
| `S-08` | [Authorization](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/authorization/index.mdx) | spec `2026-07-28` | optional HTTP auth、token audience / scope / errors | 不等于 Agent Policy 或业务权限模型 |
| `S-09` | [Security best practices](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/tutorials/security/security_best_practices.mdx) | docs aligned to `2026-07-28` | confused deputy、token passthrough、audit / trust risk | 不自动生成 Host + Server 完整 audit |
| `S-10` | [Versioned schema](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2026-07-28/schema.ts) | schema `2026-07-28` | `_meta` fields、`UnsupportedProtocolVersionError`、cancel types | 不证明 SDK 实现一致性 |
| `S-11` | [Versioned server concepts](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/server-concepts.mdx) | docs `2026-07-28` | Server / tools teaching model | 不是 normative replacement for spec |
| `S-12` | [Versioned core architecture](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/architecture.mdx) | docs `2026-07-28`；component-definition lines only | Host contains Clients；Servers expose features | teaching overview 不代替 normative spec，也不证明具体进程拓扑、Client lifetime 或 Host Policy 结构 |
| `R-01` | [`glossary.md`](../../glossary.md) | local course contract | Agent / Tool / Permission / Trace / Evidence 词汇边界 | 不证明 MCP wire behavior |
| `R-02` | [Article 05](../../../../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md) | published local dependency | Tool Call != Executed；Result != Evidence | 不证明 MCP server behavior |
| `R-03` | [Article 06](../../../../content/ai-empowerment/agent-engineering-06-tool-runtime.md) | published local dependency | Host Tool Runtime stages / failure boundaries | 不规定远端 Server 实现 |
| `R-04` | [`agent-engineering-series-plan.md`](../../../agent-engineering-series-plan.md) + [`v3.1 review`](../../../agent-engineering-course-plan-v3.1-review.md) | canonical + frozen Article 07 section | scope、questions、non-goals、no-Lab decision | 课程合同不是协议事实 |

## Evidence Gate decision

- Research Questions：`8 / 8 ANSWERED`。
- Claim Register：`9`；`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
- Evidence Cards：`9`，稳定映射 `07-Cxx -> 07-Exx`。
- Current primary source coverage：Client / Server / Host、discovery / schema、call / result、version / lifecycle、transport / error / cancellation、authorization / security / trust boundary 均有 official source。
- Provider Calls：`0`。
- Local MCP Runtime：`0`；Required Lab=`NONE`。
- Evidence Gate：`PASS_CANDIDATE`。
- Blocker：`NONE`。
- Next Action：`OUTLINE`（由 Master 接受 Gate 后）。

## Research Stop Line

Researcher 在 `PASS_CANDIDATE` 后停止。Author 只能依据 Evidence Register 的 scoped wording 进入 Outline；不得把 `07-C08` 写成 MCP 规范保证，不得把 `07-C09` 扩大成“所有 MCP implementation 都不含 policy/runtime”，不得把 legacy initialize 当成 current `2026-07-28` lifecycle。任何新增核心行为、SDK / transport implementation 事实或 runtime claim 必须返回 Research；本轮不得创建 Outline / Draft / assets / Published Content，也不得启动 Article 08。
