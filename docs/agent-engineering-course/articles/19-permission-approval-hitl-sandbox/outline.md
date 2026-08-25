# Article 19 Outline｜Permission、Approval、Human-in-the-loop 与 Sandbox

## Outline contract

- Article Type: PRINCIPLE
- Teaching Spine: 问题空间 -> 五概念分账 -> action-authority 抽象模型 -> 判定时点 -> 风险路由与审批记录 -> HITL 暂停/恢复 -> least privilege / TOCTOU -> sandbox 机制边界 -> BuildPilot 设计落地 -> 工程与验证边界
- Core Claim Scope: `19-C01`—`19-C10` only；不新增核心 Claim
- Evidence Posture: `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

> 如果这篇只记一句话：`被接受的判断仍不是执行许可证；一次动作只有在主体、动作、资源、约束、策略、审批与执行点都对得上时，才有资格进入执行边界。`

## Opening hook｜Evidence 已经 accepted，为什么动作仍不能执行

- Reader Question: BuildPilot 的诊断 Claim 已通过 Article 18 的 Evidence acceptance，为什么它仍不能直接发布制品、使用凭据或修改权限？
- Claim / Evidence: `19-C01` / `19-E01`, `19-E02`；边界桥接 `19-C10` / `19-E12`。
- Teaching Role: 用一个明确标注为 `CONSTRUCTED COURSE DESIGN / NOT IMPLEMENTED / NOT RUN` 的 BuildPilot action request 开场：诊断包已 accepted，但 `principal`、具体 `action/resource`、当前 policy、approval、expiry/revocation 与 use-time enforcement 仍未闭合。
- Planned contrast:

  ```text
  accepted Evidence
    = “这个 scoped Claim 达到当前证据门槛”

  action authority
    = “这个 Principal 此刻能否在这些 Constraints 下
       对这个 Resource 执行这个 Action”

  accepted Evidence != credential != approval != execution authority
  ```

- Wording Boundary: 可以断言 Evidence acceptance 与 action authority 逻辑独立；不得暗示课程 Evidence、Reviewer PASS 或一个 `confidence` 标签已经签发真实凭据、批准或执行权。
- Section Takeaway: **先证明判断可接受，再独立判断动作是否获准；二者不能合并成一个“可信所以能做”。**

## Part A｜问题空间：一个“允许”开关吞掉了哪些责任

### 1. 工具可用、凭据存在、判断可信，都不是本次动作获准

- Reader Question: 为什么 Tool Registry 中存在 handler、环境里存在 credential、请求结构合法或 Evidence 已通过，都不足以让 Runtime 执行动作？
- Claim / Evidence: `19-C01`, `19-C02`, `19-C04` / `19-E01`, `19-E02`, `19-E03`, `19-E04`。
- Section Responsibilities:
  - 从 Article 06 接回 `Schema Valid != Policy Allowed` 与 `Sandbox != Permission`，但不重讲 Tool Runtime pipeline。
  - 展示四种错误等式：`tool registered = allowed now`、`credential exists = authorized use`、`approval button clicked = any later request approved`、`sandboxed = business action permitted`。
  - 把问题重写为一组可审查输入：谁、做什么、作用于什么、在什么约束下、由哪版 policy 判断、是否需要谁批准、最终在哪里强制。
- Wording Ceiling: `19-C02` 与 `19-C04` 保持 `PARTIAL`；五分法与五个时点是课程 working model，不称行业统一 taxonomy 或产品通用流程。
- Section Takeaway: **能力存在只说明“可能被请求”，不说明“这次请求可以执行”。**

### 2. 为什么人工在场也不自动形成控制

- Reader Question: “有人看过”或“页面上有 Approve 按钮”为什么仍可能缺少作用域、责任和恢复边界？
- Claim / Evidence: `19-C02`, `19-C06`, `19-C07` / `19-E03`, `19-E05`, `19-E08`, `19-E10`, `19-E11`。
- Section Responsibilities:
  - 区分人类参与的事实与可审计 Approval：身份、冻结请求、决定范围、期限、理由和撤销状态必须绑定。
  - 区分 Approval 与 HITL：前者是 decision，后者是 pause / decision / resume control flow。
  - 用产品限定例子只说明“可以存在 per-tool-call interruption、approve/reject、serialized state 与 resume”这一窄交互；不把产品文档外推为通用协议、revalidation 或安全保证。
- Proposal Boundary: Approval records 与 HITL state machine 均为课程设计；人工决定可能错误，不能被写成安全正确性的证明。
- Section Takeaway: **Human-in-the-loop 的工程价值不在“有人出现”，而在决定绑定了哪份请求、怎样暂停、怎样拒绝，以及恢复前重新检查什么。**

## Part B｜抽象模型：先把五个概念分账

### 3. Permission、Authorization、Approval、HITL、Sandbox

- Reader Question: 五个经常被统称为“允许”的概念，分别拥有哪一类判断？
- Claim / Evidence: `19-C02` / `19-E02`, `19-E03`, `19-E09`, `19-E10`。
- Mandatory Label: `COURSE WORKING MODEL / PARTIAL / NOT AN INDUSTRY-UNIFIED TAXONOMY`。
- Planned responsibility table:

  | Concept | 本课程最小工作定义 | 主要回答 | 明确不能替代 |
  |---|---|---|---|
  | Permission | 预先配置或授予的 capability ceiling | 原则上可请求什么 | 本次 request 的 allow、人工 decision、runtime isolation |
  | Authorization | 当前 Principal/Action/Resource/Constraints 经 Policy 得到的决定 | 这次 request 是否 allow/deny/approval-required | 永久 permission、人工责任或实际 enforcement |
  | Approval | 有身份、有 scope、有期限、可撤销的主体对冻结请求作出的显式决定 | 谁对哪份 request 决定 approve/reject | 扩大 request、永久权限、动作 exactly-once |
  | HITL | 在需要人类决定时 pause，绑定 decision，并在 resume 前重验证的控制流 | 何时停、怎样决定、怎样恢复 | decision 本身、业务 policy、sandbox mechanism |
  | Sandbox | 隔离或过滤运行时接触面的机制组合；本篇只确认 Linux namespaces/network namespace/seccomp，filesystem/secret broker 为未取证课程示例 | 代码运行时能接触哪些 surface | 身份、业务授权、审批正确性或完整安全保证 |

- Progressive Definition: 先给“本篇职责”而不是从 IAM 产品/API 开始；产品名只在后文作为受限例子。
- Section Takeaway: **Permission 定上界，Authorization 判本次请求，Approval 承担显式责任，HITL 管暂停恢复，Sandbox 收窄执行面。**

### 4. 五个判定时点：每次 PASS 都不能替下一次 PASS

- Reader Question: 权限在什么时候 provision，何时决定本次 request，何时由人确认，为什么 resume 和 execute 前还要再查？
- Claim / Evidence: `19-C04` / `19-E02`, `19-E03`, `19-E04`。
- Mandatory Label: `COURSE LAYERING / PARTIAL；standard-supported separation, course-designed workflow`。
- Planned decision-time table:

  | Time | Decision object | Minimum question | PASS does not prove |
  |---|---|---|---|
  | Provision | Permission / capability | 主体原则上可请求哪些 action/resource scope？ | 当前请求获准 |
  | Request | Authorization | 当前上下文与 policy 对冻结请求返回什么？ | 人工已批准或执行已发生 |
  | Human decision | Approval | 指定 approver 对哪个 digest/scope/expiry 作出什么决定？ | 请求变化后仍有效 |
  | Resume | Revalidation | principal、digest、resource、policy、scope、expiry、revocation 是否仍成立？ | use-time 状态不再变化 |
  | Use time | Enforcement | 此刻具体 action 是否满足全部输入，并在允许 surface 内执行？ | 动作一定安全、成功或有收益 |

- Teaching Bridge: 连接 Article 10 的 deterministic transition commit 与 Article 11 的 resume boundary，但不重写两篇的状态机/恢复机制。
- Section Takeaway: **授权不是一次性布尔值，而是一组在不同时间、对不同对象负责的判定。**

## Part C｜核心 authority chain：从请求身份走到执行点

### 5. `Principal -> Capability/Action -> Resource -> Constraints -> Policy -> Approval -> Enforcement`

- Reader Question: 最小 action-authority model 需要哪些对象，为什么不能只保存一个 `allowed=true`？
- Claim / Evidence: `19-C03` / `19-E02`, `19-E03`, `19-E11`。
- Mandatory Label: `COURSE PROPOSAL / AUDITABLE DESIGN / NOT IMPLEMENTED`。
- Proposed model:

  ```text
  Principal
    -> Capability / concrete Action
    -> Resource
    -> Constraints
         scope + environment + expiry + request_digest
    -> Policy (id + version)
         ALLOW | DENY | APPROVAL_REQUIRED
    -> Approval (only when required; frozen request only)
    -> use-time Enforcement
         revalidate -> execute or deny
  ```

- Object responsibilities:
  - `Principal`: 让请求、approver 与 credential use 可归因；不把 session 名称当真实身份。
  - `Capability / Action`: Capability 是上界，Action 是本次具体候选；不让宽泛 tool name 覆盖精确参数。
  - `Resource`: 绑定具体目标与作用域，不只写“部署环境”之类模糊名词。
  - `Constraints`: 至少含 scope、environment、expiry、request digest；缺项时不猜测授权。
  - `Policy`: 输出 allow / deny / approval-required，并保留 `policy_id/version`。
  - `Approval`: 只能确认或收窄被冻结请求，不得扩大 resource、parameters 或期限。
  - `Enforcement`: 在 use time 重验并执行；没有可用 enforcement point 的“allow”不能成为真实执行保证。
- Fail-closed rule: 关键 identity、resource、constraints、policy version 或 enforcement input 缺失时，课程设计选择 deny 或进入明确的 approval-required/blocked route，不从自然语言猜授权。
- Proposal Boundary: 这是一套可审计设计，不是 NIST 原文模型、现成协议或 BuildPilot 已实现架构。
- Section Takeaway: **`allowed=true` 丢掉了谁、对什么、到什么时候、由谁决定和在哪里强制；authority envelope 要保留整条输入链。**

### 6. Risk route：先分类动作，再选择自动、审批或拒绝

- Reader Question: 哪些动作可以按策略自动，哪些必须显式 Approval，哪些应直接 deny？
- Claim / Evidence: `19-C05` / `19-E04`, `19-E11`。
- Mandatory Label: `COURSE PROPOSAL / ORGANIZATION-SPECIFIC POLICY INPUT`。
- Proposed risk table:

  | Route | Course-level example | Default handling | Boundary |
  |---|---|---|---|
  | R0 | 纯读且无敏感输出 | 可按当前 policy 自动 | 仍需明确 principal/resource/scope |
  | R1 | 本地、可逆、边界明确 | 可按 policy 自动或要求 approval | “可逆”与边界必须由组织定义 |
  | R2 | 外部发布、凭据使用、权限变更、不可逆/破坏性动作 | 必须显式 approval | approval 仍受 digest/scope/expiry 限定 |
  | R3 | 超出 hard policy | 直接 deny | 人工 approval 不能越过 hard deny |

- Unknown handling: 未识别 action/resource/risk 时默认 deny 或 approval-required；不让模型自行把未知动作降级到 R0/R1。
- Explicit non-scope: 不定义 Token、Step、Cost、Latency 数值或消耗策略；这些属于 Article 20 Budget。
- Wording Boundary: 不声称 R0—R3 是行业标准、普适最优或已经验证的 BuildPilot policy。
- Section Takeaway: **风险路由决定“进入哪条控制路径”，不是给动作贴一个看起来客观的永久等级。**

## Part D｜Approval records：决定必须绑定冻结请求

### 7. `ApprovalRequest` 与 `DecisionRecord` 的最小审计设计

- Reader Question: 一次 Approval 至少要记录什么，才能防止“批准 A，却执行了更宽的 B”？
- Claim / Evidence: `19-C06` / `19-E04`, `19-E05`, `19-E08`。
- Mandatory Label: `COURSE PROPOSAL / MINIMUM AUDIT DESIGN / NOT A PRODUCT SCHEMA`。
- Planned two-record table:

  | Record | Minimum fields | Audit purpose |
  |---|---|---|
  | ApprovalRequest | `request_id`, principal, action, resource, parameters/request_digest, constraints, risk_reason, policy_id/version, requested_at, expires_at, required_approver rule | 固定“请求了什么、为什么需要审批、哪版 policy 要求谁决定” |
  | DecisionRecord | `decision_id`, request_id/digest, approver principal, approve/reject, decision scope, reason, decided_at, expires_at, revocation state, policy version | 固定“谁对哪份请求作出什么、有效到何时、是否已撤销” |

- Binding rules:
  - digest 不同、resource 更宽、parameters 改变、scope 扩大或 request 已过期时，旧 decision 不可复用。
  - `approve` 与 `reject` 都是正式结果；不存在“只有 approve 才记录”的捷径。
  - Approval 不能把 capability ceiling 或 hard policy deny 静默扩大。
- Honest defaults: approver、reason、expiry 或 revocation state 不存在时明确 `UNKNOWN/NONE` 并 fail closed；不伪造真实 ID、时间或签名。
- Section Takeaway: **Approval 的最小单位不是一个按钮事件，而是一份 decision 对一份冻结 request 的可审计绑定。**

## Part E｜HITL：pause、decision、resume 是状态机，不是一段聊天

### 8. 最小 HITL state machine（COURSE PROPOSAL）

- Reader Question: 系统遇到 `APPROVAL_REQUIRED` 后，怎样暂停、批准、拒绝、取消、过期并恢复，而不丢失原请求？
- Claim / Evidence: `19-C07` / `19-E03`, `19-E05`, `19-E07`, `19-E10`。
- Mandatory Label: `CONSTRUCTED COURSE STATE MACHINE / NOT IMPLEMENTED / NOT RUN`。
- Proposed state machine:

  ```text
  READY
    -> AUTHZ_CHECK
       -> DENIED
       -> WAITING_APPROVAL
            -> APPROVED
            -> REJECTED
            -> CANCELLED
            -> EXPIRED

  APPROVED
    -> REVALIDATING
       -> DENIED
       -> READY_TO_EXECUTE
            -> EXECUTING
               -> SUCCEEDED | FAILED
  ```

- Transition guards:
  - `WAITING_APPROVAL` 必须持久化冻结 request/digest 与所需 approver rule。
  - `REJECTED / CANCELLED / EXPIRED` 不得直接 resume；需要新 request 时生成新 identity。
  - 只有 `APPROVED` 可以进入 `REVALIDATING`，但 Approval PASS 不是 execution PASS。
  - `decision_id` 使重复 approve/reject 返回同一 decision outcome；不得由此推出动作 exactly-once。
- Product example boundary: OpenAI Agents SDK 文档只作为当前产品限定的 interruption/approve/reject/serializable state/resume 例子；本文状态机与 guards 不是产品实测结果。
- Section Takeaway: **HITL 的暂停点要保存一份可重验的请求；恢复不是把旧调用从停顿处无条件放行。**

### 9. Resume revalidation：批准之后，执行之前，再问一次“还是同一件事吗”

- Reader Question: 为什么已批准请求在恢复时仍可能变成 deny？
- Claim / Evidence: `19-C07`, `19-C08` / `19-E03`, `19-E05`, `19-E06`, `19-E07`。
- Revalidation checklist:
  1. request digest 与 approved digest 是否相同；
  2. principal / approver identity 与 credential binding 是否仍成立；
  3. policy version 与 hard policy 是否变化；
  4. resource identity/state 与 scope 是否仍匹配；
  5. approval / credential 是否 expiry；
  6. permission / approval / credential 是否 revoked；
  7. execution surface 是否仍满足约束。
- Idempotency boundary:
  - Approval decision 的重复提交可以返回相同 decision。
  - Action retry 仍需 Article 06/11 的 invocation identity、副作用语义与恢复合同。
  - 不使用 `exactly-once`、`durable idempotency` 或“审批后动作不会重复”的措辞。
- Section Takeaway: **Resume 重新验证的是“批准对象仍未变化”；动作是否能安全重试仍属于执行与副作用协议。**

## Part F｜Least privilege、撤销与 TOCTOU：授权会随时间失效

### 10. 最小权限不是角色名，而是 action/resource/scope/expiry 的交集

- Reader Question: 怎样把 least privilege 落到一次动作，而不是停在“低权限账号”口号？
- Claim / Evidence: `19-C08` / `19-E03`, `19-E04`, `19-E05`, `19-E06`。
- Section Responsibilities:
  - capability / credential 只授予完成任务所需的最小 action、resource、scope 与最短期限。
  - permission、approval 与 delegated credential 分别记录 expiry/revocation；任何一项失效都要求重新判定。
  - 明确 revocation 可能存在传播窗口，不能写成“撤销后所有组件瞬时失效”。
- Confirmed Boundary: 可以断言 least privilege、scope、expiry、revocation 与 check/use race 是独立控制关注点；只能说这些控制降低暴露面，不证明零风险。
- Section Takeaway: **最小权限要落到具体动作与有效期；一个宽泛角色名无法替代 request-level scope。**

### 11. TOCTOU：check 通过后，use 之前世界可能已经改变

- Reader Question: Authorization / Approval 都通过后，为什么 Enforcement 仍要在 use time 检查？
- Claim / Evidence: `19-C08` / `19-E03`, `19-E05`, `19-E06`。
- Planned sequence:

  ```text
  check principal/resource/policy/approval
       -- time passes / state changes -->
  use credential on resource

  mitigation direction:
    revalidate at resume/use time
    + shorten check/use window
    + move check and use into the same enforcement boundary where possible
  ```

- Mandatory limitation: revalidation 与缩短窗口是缓解方向，不能宣称消灭所有 race；资源、策略、revocation propagation 与外部系统状态仍可能变化。
- Section Takeaway: **授权决定有时间边界；越靠近副作用执行点，越要用当前事实重验。**

## Part G｜Sandbox：逐机制说明能限制什么，也说明不能证明什么

### 12. Sandbox mechanism / limit matrix

- Reader Question: 本篇已确认的 Linux namespaces/network namespace/seccomp 与未确认的 filesystem/secret course examples 各处于什么证据强度，为什么任一单项都不能替代 Authorization / Approval？
- Claim / Evidence: `19-C09` / `19-E09`。
- Mixed-evidence mechanism matrix；global label 必须是 `COURSE DESIGN / MIXED EVIDENCE POSTURE`，不能把整表标为 confirmed：

  | Evidence posture | Mechanism family | 可以按具体配置陈述的限制面 | 不能据此自动断言 |
  |---|---|---|---|
  | `CONFIRMED / 19-E09` | Linux namespaces | 各 namespace 所管辖的 resource view | 多种 namespace 组合已形成完整 Sandbox |
  | `CONFIRMED / 19-E09` | Network namespace | network devices、protocol stacks、routing tables、firewall rules 等 network resources 的隔离视图 | 信息不会泄露、目标业务调用获批 |
  | `CONFIRMED / 19-E09` | seccomp syscall filter | 允许的 syscall / kernel surface | 完整 Sandbox、安全无逃逸或逻辑行为正确 |
  | `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE` | Filesystem view / mount / allow-write policy | 示例目标：限定 path visibility / write surface | 本篇已验证具体 filesystem mechanism/configuration |
  | `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE` | Secret broker / mount / environment policy | 示例目标：限定 credential exposure / scope / lifetime | 本篇已验证具体 secret-delivery mechanism/configuration |

- Mandatory source boundary: Linux kernel 文档明确说明 seccomp filtering 本身“isn't a sandbox”；正文采用释义，不用单一机制、容器或产品模式声明完整安全边界。
- Composition boundary: `19-E09` 直接证明只到 Linux namespaces/network namespace/seccomp；filesystem 与 secret-broker 两行只演示还可能需要哪些 surface controls，不进入 confirmed conclusion。多种机制可以组合收窄执行面，但仍需 identity、policy、approval、use-time enforcement、hardening/LSM 与信息流设计；组合存在也不证明配置正确或安全收益。
- Section Takeaway: **Sandbox 限制“运行时能碰到什么”，Authorization / Approval 决定“这次是否允许做”；二者必须协作，不能互相替代。**

### 13. 为什么“在 sandbox 里执行”仍不是安全结论

- Reader Question: 哪些未经证明的跳跃会把 mechanism presence 写成 security guarantee？
- Claim / Evidence: `19-C09`, `19-C08` / `19-E09`, `19-E06`。
- Planned anti-guarantee table:
  - namespace exists -> no escape；
  - seccomp profile loaded -> full sandbox；
  - filesystem read-only -> no information disclosure（`COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`）；
  - no network -> no side effect；
  - approval exists -> sandbox configuration correct；
  - sandboxed execution -> action safe / successful / beneficial。
- Minimum review question: 每项 guarantee 必须指明 mechanism、configuration、resource scope、threat/limit、enforcement point 与未覆盖面；缺失时只描述机制存在，不升级结果。
- Section Takeaway: **“Sandbox”不是可一次盖章的属性；保证只能落到具体机制、配置和未覆盖面。**

## Part H｜具体设计：BuildPilot `ActionAuthorityEnvelope`

### 14. 高风险动作包：把 Evidence package 接到 authority，而不是直接接 Tool

- Reader Question: BuildPilot 将来面对外部发布类高风险动作时，怎样把 Article 18 的 accepted Claim 转成一份仍需授权的动作候选？
- Claim / Evidence: `19-C10` / `19-E11`, `19-E12`；字段与 route 受 `19-C03`, `19-C05`, `19-C06`, `19-C07` 约束。
- Global label: `CONSTRUCTED COURSE DESIGN / SYNTHETIC SHAPE / DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Proposed flow:

  ```text
  accepted Evidence package                 [Article 18]
      -> ActionAuthorityEnvelope candidate  [Article 19]
      -> policy: APPROVAL_REQUIRED
      -> WAITING_APPROVAL
      -> DecisionRecord
      -> resume revalidation
      -> sandboxed Tool Runtime enforcement [Article 06 boundary]
      -> execution/result remains NOT RUN in this article
  ```

- Proposed envelope sketch:

  ```yaml
  # CONSTRUCTED COURSE DESIGN / NOT IMPLEMENTED / NOT RUN
  envelope_id: BP-AAE-DESIGN-001
  principal: BUILD_PILOT_OPERATOR_REQUIRED
  capability: PUBLISH_BUILD_ARTIFACT
  action:
    name: publish_artifact
    request_digest: REQUIRED_NOT_COMPUTED
  resource:
    target: EXTERNAL_RELEASE_TARGET_REQUIRED
    artifact_identity: REQUIRED_NOT_ACQUIRED
  constraints:
    scope: EXACT_TARGET_AND_ARTIFACT_REQUIRED
    environment: DESIGN_ONLY
    expires_at: REQUIRED
  policy:
    id: buildpilot-action-authority-course-v1-design
    version: v1-design
    decision: APPROVAL_REQUIRED
  approval:
    request_state: NOT_CREATED
    decision_state: NOT_RUN
  revocation_state: UNKNOWN
  enforcement:
    revalidation: NOT_RUN
    sandbox_profile: NOT_IMPLEMENTED
    execution: NOT_RUN
  ```

- Teaching purpose:
  - 故意保留 `REQUIRED_* / UNKNOWN / NOT_RUN`，展示缺口不会被 accepted Evidence 自动补齐。
  - 让 approval 的对象是 exact digest/resource/scope，而不是“允许 BuildPilot 发布”的宽泛口令。
  - 让 sandbox profile 成为 enforcement input，而不是 action authority 的来源。
- Mandatory disclaimer: 没有真实 approval、credential、sandbox、Tool call、artifact publish、runtime Trace、收益、安全或 production evidence。
- Section Takeaway: **BuildPilot 的 Evidence package 只能生成 action candidate；`ActionAuthorityEnvelope` 负责把“谁可对什么做什么”送入独立的 policy、approval 与 enforcement 链。**

### 15. 用同一 envelope 做 fail-closed walk-through

- Reader Question: 这份构造设计在哪些位置必须停止，什么条件满足后也仍不能宣称执行成功？
- Claim / Evidence: `19-C03`, `19-C05`, `19-C06`, `19-C07`, `19-C08`, `19-C09`, `19-C10` / `19-E03`—`19-E12`（按各 Card 的 `Proves / Does Not Prove` 使用）。
- Walk-through:
  1. `artifact_identity` 缺失 -> request 不能冻结，保持 not authorized。
  2. exact target/scope 缺失 -> risk route 不得降级，保持 deny/approval-required。
  3. request digest 未计算 -> ApprovalRequest 不可创建。
  4. DecisionRecord 不存在 -> 保持 `WAITING_APPROVAL / NOT_RUN`。
  5. 即使未来 approve，resume 仍需核对 digest、policy、resource、expiry 与 revocation。
  6. revalidation PASS 也只允许进入 enforcement candidate；不证明 Tool success、sandbox 完整或收益。
- Boundary: 这是设计 walk-through，不是 Workflow run、HITL product demo、sandbox test、release simulation 或 Article 21 trace/replay。
- Section Takeaway: **可审计设计的成熟，不是每条路径都能执行，而是每个缺口都知道应停在哪里。**

## Part I｜相邻文章边界：Article 19 只拥有 action authority

### 16. Article 06 / 10 / 11 / 18 / 20 / 21 的责任分账

- Reader Question: Permission / Approval / HITL / Sandbox 与 Tool Runtime、状态机、恢复、Evidence、Budget、Trace 各自站在哪一层？
- Claim / Evidence: `19-C10` / `19-E12`。
- Boundary matrix:

  | Owner | Owns | Article 19 只承接什么 | Article 19 不做什么 |
  |---|---|---|---|
  | Article 06 Tool Runtime | tool execution/result boundary、前置 Policy terminal | 把 enforcement 落到 Tool Runtime seam | 不重讲 validate/result/trace，不把其 Policy v1 当完整 approval system |
  | Article 10 State Machine / Workflow | 显式 state、legal transition、guard/commit | 把 approval-required 与 resume 表达成确定性控制点 | 不重讲通用 Workflow 或 Agent Decision Point |
  | Article 11 Long-running Agent | checkpoint/resume、side-effect/idempotency、revalidation seam | 要求 resume 前重验，区分 decision idempotency 与 action effect | 不设计 Retry/Recovery/compensation 或 exactly-once |
  | Article 18 Evidence Contract | Claim/Evidence acceptance | 以 accepted Claim 作为 authority 输入之一 | 不把 acceptance 当 permission/approval |
  | Article 20 Budget | Token/Step/Cost/Latency budget | 只保留约束接口 | 不定义数值、计账、耗尽或调度策略 |
  | Article 21 Trace/Replay/Failure Taxonomy | 跨 step correlation、reconstruction/re-execution、failure layer | 要求 future records 可引用 decision/request identity | 不设计完整 trace schema、replay 或 failure taxonomy |

- Canonical Boundary: 课程 ownership 是 repository-local fact，不称行业统一系统切分。
- Section Takeaway: **Article 19 只回答 action authority、HITL 与 sandbox；执行、恢复、预算和跨步骤重建仍由相邻责任面拥有。**

### 17. 一个坏设计通常怎样写坏

- Reader Question: 哪些“看起来更自动”的捷径，会把 authority chain 重新压成一个布尔值？
- Claim / Evidence: 不新增 Claim；仅把 `19-C01`—`19-C10` 转成 design-review heuristic。
- Planned anti-pattern table:
  - Evidence accepted -> execute now；
  - Tool registered / credential present -> permission granted；
  - policy allow once -> permanent authorization；
  - approve button -> any changed request approved；
  - decision idempotent -> action exactly-once；
  - resume -> skip revalidation；
  - revocation -> instant propagation；
  - sandbox exists -> secure / safe / beneficial；
  - unknown risk -> infer lowest route；
  - trace planned -> Article 21 complete。
- Minimum repair direction: 回到 principal/action/resource/constraints/policy/approval/enforcement、当前时点和 wording ceiling；缺输入时收窄、deny 或等待明确 decision。
- Section Takeaway: **authority 设计最常见的失败，是把“某一层通过”误写成“后面所有层都通过”。**

## Part J｜验证边界、Learning Check 与 Job Competency

### 18. 本篇能证明什么，不能证明什么

- Reader Question: Required Lab 为 NONE、experiment 为 0、runtime observation 为 ABSENT 时，这篇原理文的可信上限在哪里？
- Claim / Evidence: `19-C01`—`19-C10` / `19-E01`—`19-E12`，逐 Claim 遵守最终 status ceiling。
- Can establish:
  - Evidence acceptance 与 action authority 是独立判断；least privilege/revocation/TOCTOU，以及 Linux namespaces/network namespace/seccomp 的窄机制边界有 primary-source 支撑。
  - standards/product docs 可以为 access-decision separation、scope/expiry/revocation、有限 HITL 交互和 Linux mechanism limits 提供窄证据。
  - 课程可以提出五概念 working model、authority chain、risk route、approval records、HITL state machine 与 BuildPilot envelope 设计。
- Must remain absent:
  - 任何真实 permission、credential、approval、human decision、sandbox、enforcement 或 BuildPilot Runtime；
  - sandbox/approval 对安全、可靠性、成功率、成本、时延或收益的保证；
  - 统一行业 taxonomy、通用最优 risk route、标准 Approval schema 或通用 HITL protocol；
  - Article 20 Budget 或 Article 21 Trace/Replay/Failure Taxonomy 的详细设计与实现。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Section Takeaway: **来源可以约束设计边界，但没有 Runtime observation 就没有资格声称这套设计已经产生安全或收益。**

### 19. Learning Check

1. BuildPilot 的一个诊断 Claim 已经 accepted，为什么仍不能直接执行外部发布动作？还缺哪些 authority inputs？
2. Permission、Authorization、Approval、HITL 与 Sandbox 分别回答什么？哪两组最容易被误写成同一个“允许”？
3. `Principal -> Capability/Action -> Resource -> Constraints -> Policy -> Approval -> Enforcement` 中，缺少 request digest 或 policy version 时为什么应 fail closed？
4. Provision、request authorization、human decision、resume revalidation 与 use-time enforcement 分别在什么时点作决定？为什么前一步 PASS 不能替后一步？
5. 一个未知 action 为什么不能由模型自行归入 R0？R2 与 R3 的控制路径有什么本质差别？
6. ApprovalRequest 与 DecisionRecord 为什么必须分开？request parameters 或 resource 变化后，旧 approval 能否继续使用？
7. `decision_id` 让重复 approve 返回同一结果，为什么这仍不能证明 action exactly-once？
8. approval 未过期，但 policy、resource state 或 revocation state 已变化，Resume 应怎样处理？
9. `19-E09` 对 seccomp、network namespace 确认了什么？为什么 read-only filesystem 在本篇只能作为未取证课程示例？这些内容为什么都不能替代 Authorization / Approval？
10. BuildPilot `ActionAuthorityEnvelope` 的 `execution: NOT_RUN` 为什么必须保留？Article 20/21 还各自拥有哪一段问题？

### 20. Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Authority modeling | 五概念 ledger + authority chain | 能把 capability ceiling、current decision、human decision、control flow 与 isolation 分账 | 课程 working model，不称行业 taxonomy |
| Policy / governance design | risk route + ApprovalRequest/DecisionRecord | 能把主体、request digest、scope、policy version、expiry、revocation 与 approver 绑定 | proposal schema，不称已实现 service |
| Reliable workflow design | HITL state machine + resume checklist | 能设计 reject/cancel/expired terminal，并在 resume/use-time 重验证 | NOT RUN，不证明 exactly-once |
| Security boundary reasoning | least privilege/TOCTOU + sandbox matrix | 能逐机制说明限制面与未覆盖面，拒绝总括安全保证 | 不做 threat-model completeness 或 production assurance |
| Cross-system architecture | Articles 06/10/11/18/20/21 boundary matrix | 能把 execution、state、recovery、evidence、budget、trace 与 action authority 分层 | repository-local ownership，不等于行业统一架构 |
| Evidence discipline | 10/10 Claim coverage + wording ceilings | 能让 CONFIRMED/PARTIAL/PROPOSAL 语态与来源强度一致 | Required Lab NONE；runtime ABSENT |

### 21. Closing bridge

- Closing sentence: `真正可执行的 Agent 动作，不是“模型想做、证据支持、人点过按钮”三件事的叠加，而是一条在当前时点仍能被 policy 与 enforcement 复核的 action-authority chain。`
- Next bridge: Article 20 才讨论 Token、Step、Cost、Latency 的预算边界；Article 21 才讨论跨步骤 Trace、Replay 与 Failure Taxonomy。本篇不提前给出两篇的字段、算法或运行结论。

## Claim-to-section coverage（10 / 10）

| Claim | Status ceiling | Primary sections | Evidence IDs | Mandatory wording / boundary |
|---|---|---|---|---|
| `19-C01` | CONFIRMED | Opening, 1, 18 | `19-E01`, `19-E02` | Evidence acceptance 与 action authority 独立；不产生真实 permission/credential/approval |
| `19-C02` | PARTIAL | 1, 2, 3 | `19-E02`, `19-E03`, `19-E09`, `19-E10` | 五分法只称课程 working model，不称行业统一 taxonomy |
| `19-C03` | PROPOSAL | 5, 14, 15 | `19-E02`, `19-E03`, `19-E11` | authority chain 是可审计设计，不称标准模型或已实现协议 |
| `19-C04` | PARTIAL | 1, 4 | `19-E02`, `19-E03`, `19-E04` | 分层有来源支撑；五时点 workflow 是课程设计 |
| `19-C05` | PROPOSAL | 6, 14, 15 | `19-E04`, `19-E11` | R0—R3 是课程建议，不称普适最优；未知 fail closed |
| `19-C06` | PROPOSAL | 2, 7, 14, 15 | `19-E04`, `19-E05`, `19-E08` | fields 是最小审计设计，不称产品 schema |
| `19-C07` | PROPOSAL | 2, 8, 9, 14, 15 | `19-E03`, `19-E05`, `19-E07`, `19-E10` | HITL state machine 为设计；产品示例不外推；decision idempotency != action exactly-once |
| `19-C08` | CONFIRMED | 9, 10, 11, 15, 18 | `19-E03`, `19-E04`, `19-E05`, `19-E06` | 控制降低暴露面；不保证零风险或即时撤销 |
| `19-C09` | CONFIRMED | 12, 13, 15, 18 | `19-E09` | confirmed 只限 Linux namespaces/network namespace/seccomp；filesystem/secret broker 为未取证课程示例 |
| `19-C10` | PROPOSAL | Opening, 14, 15, 16, 21 | `19-E11`, `19-E12` | BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`；相邻篇 ownership 不外推运行事实 |

Coverage: `10 / 10`；new core Claim: `NONE`；core `BLOCKED`: `0`。

## Source and visual plan

### Source plan

- Draft 不新增来源或核心事实；逐节只消费 `19-E01`—`19-E12` 已验证的 `Proves / Does Not Prove / Limitations`。
- Fixed standards anchors: NIST SP 800-162 (`19-E02`)、SP 800-207 (`19-E03`)、SP 800-53 Rev. 5.1.1 via official `usnistgov/oscal-content` release/tag `v1.2.0` and tag-pinned OSCAL catalog (`19-E04`, `19-E08`)、RFC 6749/7009/9110 (`19-E05`, `19-E07`)。
- Moving/mechanism anchors 只用于窄边界：MITRE CWE-367 (`19-E06`)、Linux kernel/man-pages (`19-E09`)、OpenAI Agents SDK HITL guide (`19-E10`)；产品示例不外推，机制存在不升级为完整安全保证。
- Course ownership 与 BuildPilot posture 只取 `19-E01`, `19-E11`, `19-E12`；不把 repository design 写成行业事实。

### Visual and table plan

1. **Opening contrast diagram**: `accepted Evidence != action authority`。标注 `CONSTRUCTED RELATIONSHIP DIAGRAM / NOT A RUNTIME OBSERVATION`；职责是切开 Article 18 acceptance 与 Article 19 authority，不增加事实。
2. **Authority-chain diagram**: `Principal -> Capability/Action -> Resource -> Constraints -> Policy -> Approval -> Enforcement`，在箭头上标五个判定时点。标注 `COURSE PROPOSAL / NOT IMPLEMENTED`；职责是展示输入、decision 与 enforcement 的 owner。
3. **HITL state-machine diagram**: `WAITING_APPROVAL -> APPROVED/REJECTED/CANCELLED/EXPIRED -> REVALIDATING`。标注 `CONSTRUCTED COURSE STATE MACHINE / NOT RUN`；职责是展示 reject/cancel/expiry 与 resume revalidation，不能包装成产品截图。
4. **Five-concept ledger table**: 对比 Permission/Authorization/Approval/HITL/Sandbox 的问题与 non-substitution boundary；职责是术语分账。
5. **Risk-route table**: R0—R3 + default route + unknown handling；标注 `COURSE PROPOSAL`，不伪装为 industry scoring standard。
6. **Approval record table**: ApprovalRequest vs DecisionRecord 字段与绑定职责；标注 `MINIMUM AUDIT DESIGN / NOT A PRODUCT SCHEMA`。
7. **Sandbox mechanism/limit matrix**: 用逐行 evidence posture 区分 confirmed Linux namespaces/network namespace/seccomp 与 unconfirmed filesystem/secret-broker course examples；职责是阻止 sandbox guarantee 越界。
8. BuildPilot 只使用 inline YAML `ActionAuthorityEnvelope` + fail-closed walk-through；明确 `CONSTRUCTED COURSE DESIGN / DESIGN / NOT IMPLEMENTED / NOT RUN`，不创建 asset 或伪运行截图。

## Explicit non-scope

- 不实现 IAM、policy engine、approval service、credential broker、sandbox runtime 或 BuildPilot。
- 不声称 Approval、人类参与、Sandbox、least privilege 或 revalidation 保证安全、成功、可靠、低成本、低时延或收益。
- 不把 product-specific HITL 文档外推为通用协议、跨版本行为或 production guarantee。
- 不把 R0—R3、Approval records、HITL state machine 或 `ActionAuthorityEnvelope` 写成标准原文、现成产品 schema 或 observed runtime。
- 不新增 Required Lab；保持 Required Lab `NONE`、experiments `0`、runtime observation `ABSENT`。
- 不重讲 Article 06 Tool Runtime、Article 10 State Machine/Workflow 或 Article 11 Recovery；只使用它们的明确 seam。
- 不提前完成 Article 20 的 Token/Step/Cost/Latency budget、计账与耗尽策略。
- 不提前完成 Article 21 的 cross-step Trace、Replay、re-execution 或 Failure Taxonomy。
- 不创建 Draft、Published Content、Lab、assets 或未来 Article artifact。

## Outline Gate self-check

- [x] Problem space -> abstract model -> concrete BuildPilot design -> engineering / verification boundary 完整。
- [x] `19-C01`—`19-C10` coverage = `10 / 10`；无新核心 Claim。
- [x] `19-C01 / C08 / C09` 保持 CONFIRMED 的窄证据边界；C09 只确认 Linux namespaces/network namespace/seccomp，filesystem/secret broker 明确为未取证课程示例；无安全或收益保证。
- [x] `19-C02 / C04` 保持 PARTIAL wording ceilings，五概念和五时点明确标为课程模型/设计。
- [x] `19-C03 / C05 / C06 / C07 / C10` 全部保持 Proposal language。
- [x] accepted Evidence≠authority 开场、五概念分账、authority chain、判定时点、risk route、Approval records、HITL resume revalidation、least privilege/revocation/TOCTOU、sandbox matrix 与 BuildPilot envelope 均已落段。
- [x] Article 06 / 10 / 11 / 18 / 20 / 21 ownership boundary 显式，未预写 Article 20 budget 或 Article 21 trace/replay/failure taxonomy。
- [x] Required Lab `NONE`；Experiment Count `0`；Runtime Observation `ABSENT`。
- [x] BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN`；无真实 approval、credential、sandbox、runtime、security、benefit 或 production Claim。
- [x] Learning Check、Job Competency、source plan 与 figure/table teaching duties 已包含；所有构造图/表/样例均要求显式 DESIGN 标签。
