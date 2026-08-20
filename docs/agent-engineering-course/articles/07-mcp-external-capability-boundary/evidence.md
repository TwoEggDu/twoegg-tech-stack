# Article 07 Evidence Register｜MCP 与外部能力边界

- Evidence Phase：`RESEARCH / EVIDENCE_GATE`
- Evidence Status：`PASS_CANDIDATE`
- Evidence Gate：`PASS_CANDIDATE / OUTLINE_NEXT`
- Claim Count：`9`
- Claim Summary：`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Evidence Card Count：`9`
- Protocol Baseline：`MCP 2026-07-28`
- Retrieved / Verified At：`2026-08-20（Asia/Shanghai）`
- Required Lab：`NONE`
- Provider / external runtime calls：`0`
- Local MCP runtime runs：`0`
- Runtime Evidence：`NONE / NOT_REQUIRED`

> 所有协议行为 Claim 都固定到 MCP `2026-07-28`。`07-C08` 是课程工程 proposal；`07-C09` 是由协议范围与本课程词汇共同得到的边界解释，保留 `PARTIAL`。本文没有把 spec-derived JSON 示例登记为 runtime Observation。

## Claim Register

| Claim ID | Narrow Claim | Status | Evidence Class | Version / runtime scope | Evidence |
|---|---|---|---|---|---|
| `07-C01` | 官方架构把 Host 定义为承载 MCP Client 的 LLM application，Client 与 Server 通过 transport 交互；wire message 的直接角色是 Client / Server，Host 怎样持有 Agent context 属于应用实现。 | `CONFIRMED` | `OFFICIAL_DOC + VERSIONED_SPEC` | component role；current message direction | `07-E01` |
| `07-C02` | MCP `2026-07-28` 的 Server 必须实现 `server/discover`，Client 可选调用；每个 request 声明 protocol version 与 client capabilities，discovery identity 是 self-reported，不能用于安全决策。 | `CONFIRMED` | `VERSIONED_SPEC` | `2026-07-28` | `07-E02` |
| `07-C03` | 声明 tools capability 的 Server 必须响应 `tools/list`；Tool definition 含 name 与有效 JSON Schema input，可选 output schema；list 可按本次 request authorization 变化，annotation 默认不可信。 | `CONFIRMED` | `VERSIONED_SPEC` | `2026-07-28` | `07-E03` |
| `07-C04` | `tools/call` 用 name + arguments 调用；request / protocol failure 用 JSON-RPC error，Tool execution / business failure 可用 result `isError: true`；两者不是同一失败层。 | `CONFIRMED` | `VERSIONED_SPEC` | `2026-07-28` | `07-E04` |
| `07-C05` | MCP `2026-07-28` 没有 negotiation handshake 或 protocol session；每请求声明版本，不支持时 Server 返回 `-32022` 与 supported versions，Client 应选择共同版本重试或报错。legacy initialize 仅适用于 `2025-11-25` 及以前。 | `CONFIRMED` | `VERSIONED_SPEC + RELEASE` | modern `2026-07-28` vs legacy | `07-E05` |
| `07-C06` | stdio 与 Streamable HTTP 共享 core message semantics，但 framing / metadata mirror / cancellation signal 不同；取消存在竞态且通常只要求 Server SHOULD stop，timeout 也是 implementation SHOULD，不保证底层工作被强制终止。 | `CONFIRMED` | `VERSIONED_SPEC` | stdio + Streamable HTTP `2026-07-28` | `07-E06` |
| `07-C07` | MCP authorization 是 optional，并为 HTTP-based transport 定义授权流；采用时 Server 必须验证 token 与 intended audience，并在有 scope 限制时拒绝不足 scope；stdio 不使用该 flow。自报 client/server info 不是安全 identity。 | `CONFIRMED` | `VERSIONED_SPEC + SECURITY_GUIDANCE` | auth adopted；transport-scoped | `07-E07` |
| `07-C08` | 课程采用 Host Policy + Server Policy 双层 fail-closed，并为 domain validation、timeout / cancellation、identity binding、result shaping 与 audit 分配显式 owner；这不是 MCP 规定的唯一实现。 | `PROPOSAL` | `COURSE_INTERPRETATION` | Article 07 course boundary only | `07-E08` |
| `07-C09` | MCP discovery / call / result 的协议成功不能单独充当 Agent loop、Permission、Article 06 Tool Runtime gates 或完整 Evidence 已成立的证明。该结论是课程范围解释，不否定某个产品可在 MCP 外另行实现这些层。 | `PARTIAL` | `SPEC_SCOPE + COURSE_BOUNDARY` | no runtime observation | `07-E09` |

## Source Manifest

只登记本轮真实打开并核对的 official / primary pages；本地课程文件只用于课程边界。

| ID | Source | Checked / version | Used by |
|---|---|---|---|
| `S-01` | [MCP 2026-07-28 release](https://blog.modelcontextprotocol.io/posts/2026-07-28/) | `2026-08-20` / release `2026-07-28` | `C05` |
| `S-02` | [Versioning and Compatibility](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/versioning.mdx) | `2026-08-20` / spec `2026-07-28` | `C05` |
| `S-03` | [Discovery](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/discover.mdx) | `2026-08-20` / spec `2026-07-28` | `C02, C05` |
| `S-04` | [Tools](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/tools.mdx) | `2026-08-20` / spec `2026-07-28` | `C03, C04` |
| `S-05` | [Transport overview](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/index.mdx) | `2026-08-20` / spec `2026-07-28` | `C01, C06` |
| `S-06` | [Streamable HTTP](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/streamable-http.mdx) | `2026-08-20` / spec `2026-07-28` | `C06` |
| `S-07` | [Cancellation](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/patterns/cancellation.mdx) | `2026-08-20` / spec `2026-07-28` | `C06` |
| `S-08` | [Authorization](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/authorization/index.mdx) | `2026-08-20` / spec `2026-07-28` | `C07, C08` |
| `S-09` | [Security best practices](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/tutorials/security/security_best_practices.mdx) | `2026-08-20` / docs `2026-07-28` | `C07, C08` |
| `S-10` | [Versioned schema](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2026-07-28/schema.ts) | `2026-08-20` / schema `2026-07-28` | `C02, C05, C06` |
| `S-11` | [Versioned server concepts](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/server-concepts.mdx) | `2026-08-20` / docs `2026-07-28` | `C01, C03` |
| `S-12` | [Versioned core architecture](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/docs/2026-07-28/learn/architecture.mdx) | `2026-08-20` / docs `2026-07-28` / component role only | `C01` |
| `R-01` | [`glossary.md`](../../glossary.md) | local course contract | `C08, C09` |
| `R-02` | [Article 05](../../../../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md) | published dependency | `C08, C09` |
| `R-03` | [Article 06](../../../../content/ai-empowerment/agent-engineering-06-tool-runtime.md) | published dependency | `C08, C09` |
| `R-04` | [`agent-engineering-series-plan.md`](../../../agent-engineering-series-plan.md) + [`v3.1 review`](../../../agent-engineering-course-plan-v3.1-review.md) | canonical + frozen Article 07 | `C08, C09` |

## Evidence Cards

### Evidence 07-E01｜Host, Client, Server and transport roles

- Claim ID：`07-C01`
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC + VERSIONED_SPEC`
- Source：`S-12` component overview；`S-05` current message directions；`S-11` current Server concept。
- Version Scope：component role 与 wire semantics 均固定为 `2026-07-28`；`S-12` 用于角色定义，`S-05` 用于 current message directions。
- Observation：官方架构把 Host 表述为发起连接的 LLM application，Client 位于 Host 内并连接 Server；current transport spec 把 client requests/notifications 与 server responses/notifications 定为 wire directions。
- Counter-evidence Searched：`S-12` 是 teaching overview，不能取代 `S-05` 的 normative transport contract，也不能推出统一进程拓扑或 Client lifetime。
- Interpretation：Host 是承载 Client 与 Agent context 的应用边界；协议消息合同直接落在 Client / Server。
- Proves：本文的三层角色图与 Client—Transport—Server seam。
- Does Not Prove：每个产品都采用相同进程拓扑、Client lifetime 或 Host Policy 结构。
- Limitations：official architecture 是 versioned teaching page，不是 normative replacement；只采用其组件定义。
- Course Usage：解释 MCP Client 不等于 Agent，也不替 Host 做全部 policy。
- Allowed Wording：“Host application 包含 MCP Client；Client 通过 transport 与 Server 交换协议消息。”
- Stop Line：不得把 `S-12` 的角色概览提升为 normative spec，也不得扩展 C01。
- Verified At：`2026-08-20`

### Evidence 07-E02｜Discovery, request capabilities and self-reported identity

- Claim ID：`07-C02`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC`
- Source：`S-03`、`S-10`。
- Version Scope：MCP `2026-07-28`。
- Observation：Server MUST implement `server/discover`；Client MAY call it，或直接发 RPC 并处理 unsupported version。每个 request 要带 protocol version 与 client capabilities；discover result 返回 supported versions / server capabilities，serverInfo 是 self-reported。
- Counter-evidence Searched：Client 不需要在每个业务调用前先 discover；serverInfo name 不保证唯一且不得用于安全决策。
- Interpretation：Discovery 是可缓存的协议/能力广告，不是 connection handshake，也不是 trust establishment。
- Proves：current capability discovery / request metadata contract。
- Does Not Prove：Server 能力实现正确、调用被授权、identity 已验证。
- Limitations：不验证任何 SDK 的 caching / probe behavior。
- Course Usage：Capability Discovery 层；与 Permission 分栏。
- Allowed Wording：“Server 必须支持 discover；Client 可选先 discover；身份字段是自报 metadata。”
- Stop Line：不得写“Client 必须先 discover”或“serverInfo 可用于鉴权”。
- Verified At：`2026-08-20`

### Evidence 07-E03｜Tool list and schema contract

- Claim ID：`07-C03`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC`
- Source：`S-04`、`S-11`。
- Version Scope：MCP `2026-07-28`。
- Observation：支持 tools 的 Server MUST declare capability and respond to `tools/list`；definition 有 name / description / inputSchema，可选 outputSchema。inputSchema 是有效 JSON Schema object；未给 `$schema` 时默认 2020-12。tool set 可按 request authorization 变化；annotations 对非 trusted Server 是 untrusted。
- Counter-evidence Searched：tool name uniqueness 只在单 Server 内；Server name 也不保证全局唯一；聚合 Client 需处理碰撞。
- Interpretation：Schema 形成机器可读参数合同与候选能力清单，不形成业务权限。
- Proves：discovery + schema 的 current wire surface。
- Does Not Prove：schema 覆盖全部 domain invariants、tool 安全、annotation 真实或调用获准。
- Limitations：不展开 resources / prompts / extensions 或所有 JSON Schema features。
- Course Usage：承接 Article 03/05 的 machine contract，再引出 Host / Server 双侧 validation。
- Allowed Wording：“`tools/list` 返回当前请求者可见的 Tool definitions 与 input schema。”
- Stop Line：不得写“被列出的 Tool 已获 Agent / user 授权”。
- Verified At：`2026-08-20`

### Evidence 07-E04｜Tool call, result and two error layers

- Claim ID：`07-C04`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC`
- Source：`S-04`。
- Version Scope：MCP `2026-07-28`。
- Observation：Client 用 `tools/call` 发送 name + arguments；complete result 可含 content / structured content / `isError`。unknown tool、malformed request、server error 用 JSON-RPC error；API、input validation、business logic failure 可在 Tool result 中用 `isError: true`。
- Counter-evidence Searched：Tool execution error 到达不表示外部副作用没有部分发生；protocol error 是否给模型由 Client 选择。
- Interpretation：协议区分“消息/请求失败”与“Tool 已处理但执行失败”，但不替 Host 决定 retry / idempotency。
- Proves：call / result / error representation。
- Does Not Prove：exactly-once、retry safety、result redaction、schema-valid result 或完整 trace。
- Limitations：不研究 multi-round-trip input-required 与 extension behavior。
- Course Usage：支持 `Protocol Error != Tool Error` 与 `Result != Evidence`。
- Allowed Wording：“Tool execution error 可作为 `isError: true` result；协议错误走 JSON-RPC error。”
- Stop Line：不得把成功 JSON-RPC response 写成业务动作已安全完成。
- Verified At：`2026-08-20`

### Evidence 07-E05｜Current stateless lifecycle and version negotiation

- Claim ID：`07-C05`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC + OFFICIAL_RELEASE`
- Source：`S-01`、`S-02`、`S-03`、`S-10`。
- Version Scope：modern `2026-07-28`；legacy comparison `<=2025-11-25`。
- Observation：current spec 明确“no negotiation handshake”；每个 request 带版本。unsupported version 返回 JSON-RPC code `-32022`、supported/requested；Client SHOULD 选共同版本重试或报错。旧 initialize/session 属于 legacy era。
- Counter-evidence Searched：dual-era Client 对 stdio / HTTP 的探测与 fallback 不同；SDK 可以默认 legacy、auto 或 pin，不能从 spec 推断单一默认。
- Interpretation：版本协商是 per-request accept/reject + retry；`server/discover` 是可选预探测，不是新握手。
- Proves：Article 07 的 current lifecycle / version risk。
- Does Not Prove：任一 SDK 已支持 current spec、默认升级、fallback 一定成功。
- Limitations：不覆盖 draft 或 `2026-07-28` 之后版本。
- Course Usage：正文必须显示 version / checked date，并把 legacy 作为反例。
- Allowed Wording：“在 `2026-07-28`，请求自带版本；不支持时返回 `-32022`，Client 决定重试或报错。”
- Stop Line：不得把 `initialize -> initialized` 写成 current lifecycle。
- Verified At：`2026-08-20`

### Evidence 07-E06｜Transport, cancellation and timeout boundary

- Claim ID：`07-C06`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC`
- Source：`S-05`、`S-06`、`S-07`、`S-10`。
- Version Scope：stdio + Streamable HTTP，MCP `2026-07-28`。
- Observation：transport 负责 framing / delivery / metadata carry / cancellation signal，不改变消息语义。stdio 以 `notifications/cancelled` 取消；HTTP 关闭 request SSE response stream。Server SHOULD 停止 processing，但可因完成、未知 request 或不可取消而忽略；timeout 是实现 SHOULD 且应可按 request 配置。
- Counter-evidence Searched：取消通知可能晚于完成；HTTP 断流后 Server MUST 不再发送，但底层 downstream work 停止仍只有 cooperative boundary。
- Interpretation：Protocol cancellation 是“结果不再需要 / 请求停止”的信号，不是 thread/process/side-effect rollback。
- Proves：transport-specific cancellation 与 timeout ownership seam。
- Does Not Prove：强制终止、rollback、deadline 精度、network retry safety。
- Limitations：未运行 network / stdio fixture，也不研究 custom transport。
- Course Usage：Host 与 Server 分别保留 timeout/cancellation result 与 cause；文章使用 SHOULD 语态。
- Allowed Wording：“stdio 发 cancel notification；HTTP 关闭该 request stream；取消仍可能竞态且不保证底层工作强停。”
- Stop Line：不得写“取消一定停止外部系统”。
- Verified At：`2026-08-20`

### Evidence 07-E07｜Authorization, identity and trust boundary

- Claim ID：`07-C07`
- Evidence Status：`CONFIRMED`
- Evidence Class：`VERSIONED_SPEC + OFFICIAL_SECURITY_GUIDANCE`
- Source：`S-03`、`S-08`、`S-09`、`S-10`。
- Version Scope：MCP `2026-07-28`；HTTP auth adopted；stdio explicitly separate。
- Observation：Authorization 对 MCP implementation 是 OPTIONAL；HTTP-based transport SHOULD conform，stdio SHOULD use environment credentials instead。HTTP Server 采用时必须验证 token 与 intended audience，不能透传别的 resource token；有 scope 限制时不足 scope 被拒绝，所需 scope 可随具体 request 动态决定。clientInfo/serverInfo 是 self-reported。
- Counter-evidence Searched：token passthrough 会导致 confused deputy、control bypass 与 audit trail 问题；持有 token 也不等于某业务资源操作应获准。
- Interpretation：MCP 规定的是 transport auth flow / token handling；resource-level authorization 与 domain decision 仍在 Server。
- Proves：auth transport scope、identity self-report limit、Server trust boundary。
- Does Not Prove：用户意图、Agent Policy、业务 Permission、downstream token exchange 或完整 consent UX。
- Limitations：不研究所有 authorization extensions、IdP / AS implementation 或 production deployment。
- Course Usage：将“认证传输到谁”与“允许对哪个资源做什么”分栏。
- Allowed Wording：“HTTP auth adopted 时 Server 验证面向自己的 token；stdio 走不同 credential boundary。”
- Stop Line：不得写“MCP 自带统一权限系统”或用 serverInfo 当 identity principal。
- Verified At：`2026-08-20`

### Evidence 07-E08｜Host Policy + Server Policy course model

- Claim ID：`07-C08`
- Evidence Status：`PROPOSAL`
- Evidence Class：`COURSE_INTERPRETATION`
- Source：`S-03`、`S-04`、`S-07`—`S-09`；`R-01`—`R-04`。
- Version Scope：Article 07 course model；not a universal MCP implementation claim。
- Observation：规范分别暴露 untrusted metadata、per-request authorization、scope / token validation、timeout / cancellation 与 Server-side trust risks；Article 05—06 已把 model call、Host execution gates 与 Evidence 分开。
- Counter-evidence Searched：某些产品可把 Host / Server 合并进同进程，或由一个 policy engine 统一决策；协议不强制两套命名或固定 stage order。
- Interpretation：课程用两层 fail-closed 帮助分配责任：Host 不因 discovery 就放行，Server 不因 Host 已放行就跳过 caller/resource/domain checks。
- Proves：一个可审查的课程责任模型有必要且与规范不冲突。
- Does Not Prove：它是 MCP normative requirement、行业唯一最佳实践或已被 runtime 验证。
- Limitations：无 Lab；不设计 production IAM、distributed audit 或 approval UI。
- Course Usage：正文必须标“课程模型 / 工程建议”，并单列 owner / failure / trace。
- Allowed Wording：“本课程建议 Host Policy 与 Server Policy 各自 fail closed。”
- Stop Line：不得写“MCP 要求双层 policy”或“二者已由协议自动对齐”。
- Verified At：`2026-08-20`

### Evidence 07-E09｜MCP is not Agent, Permission or Tool Runtime closure

- Claim ID：`07-C09`
- Evidence Status：`PARTIAL`
- Evidence Class：`SPEC_SCOPE + COURSE_BOUNDARY`
- Source：`S-02`—`S-09`；`R-01`—`R-03`。
- Version Scope：MCP `2026-07-28` positive scope + current course terminology；no runtime observation。
- Observation：规范确认 discovery / schema / call / result / transport auth 与 cancellation；课程分别定义 Agent loop、Permission、Tool Runtime gates、Trace 与 Evidence。没有一个 MCP success message 同时携带这些课程层的完成证明。
- Counter-evidence Searched：具体 Host 或 Server 可以在协议之外实现 policy、approval、runtime、trace；因此不能断言“MCP implementation 没有这些能力”。
- Interpretation：能确认的是“协议成功不能单独证明课程其他层已闭合”，不是“使用 MCP 的系统一定缺少其他层”。
- Proves：Article 07 的核心反混淆边界。
- Does Not Prove：任一具体产品架构、实现质量或实际授权 / runtime 状态。
- Limitations：负边界依赖课程词汇；非协议 normative negative statement。
- Course Usage：采用“不能由 MCP 消息单独推出”语态；保留 `PARTIAL` 标签。
- Allowed Wording：“MCP call/result 不是 Agent loop、Permission、Tool Runtime 与 Evidence 的替代证明。”
- Stop Line：不得写“MCP 没有 security/policy”或否定产品在协议外的实现。
- Verified At：`2026-08-20`

## Evidence Gate audit

| Requirement | Result | Direct evidence |
|---|---|---|
| current official version and checked date | `PASS` | `S-01 / S-02`；`2026-07-28` checked `2026-08-20` |
| Host / Client / Server / Transport | `PASS` | `C01 / E01` |
| capability discovery + Tool schema | `PASS` | `C02-C03 / E02-E03` |
| Tool call / result / error | `PASS` | `C04 / E04` |
| lifecycle / version negotiation | `PASS` | `C05 / E05`；legacy/current explicitly split |
| transport / timeout / cancellation | `PASS` | `C06 / E06`；SHOULD and race preserved |
| authorization / security / identity / trust | `PASS` | `C07 / E07` |
| Host Policy / Server Policy / audit ownership | `PASS_AS_PROPOSAL` | `C08 / E08` |
| MCP != Agent / Permission / Tool Runtime closure | `PASS_WITH_PARTIAL_WORDING` | `C09 / E09` |
| minimum official message trace | `PASS / SPEC_DERIVED_ONLY` | `research.md`；explicitly not runtime Observation |
| Provider calls / local MCP runtime / required Lab | `0 / 0 / NONE` | research scope |
| blocked core Claim | `NONE` | 9-card disposition |

## Gate Result

| Status | Count | Claim IDs |
|---|---:|---|
| `CONFIRMED` | 7 | `07-C01`—`07-C07` |
| `PARTIAL` | 1 | `07-C09` |
| `BLOCKED` | 0 | `NONE` |
| `PROPOSAL` | 1 | `07-C08` |

### Recommendation：`PASS_CANDIDATE / NEXT OUTLINE`

核心协议行为都有 current official primary evidence，并已把 normative guarantee、transport / implementation scope 与 course interpretation 分开。`C09` 的 PARTIAL 不阻塞 Outline，因为 allowed wording 已收窄为“不能由协议消息单独推出”，未把负边界扩大成所有实现事实。

## Stop Line

Evidence Gate=`PASS_CANDIDATE`，next=`OUTLINE` after Master acceptance。Author 不得升级 `C08`，不得删除 `C09` 的 PARTIAL / allowed wording，不得把 spec-derived example 写成 local trace，不得把 `S-12` teaching overview 提升为 normative spec 或扩展 C01。Researcher不得创建 Outline / Draft / assets / Published Content，不修改 global state、canonical、glossary、Article 05—06 或 Article 08。
