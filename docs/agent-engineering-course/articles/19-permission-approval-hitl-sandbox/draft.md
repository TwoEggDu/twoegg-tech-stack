# Permission、Approval、Human-in-the-loop 与 Sandbox

> 如果这篇只记一句话：`被接受的判断仍不是执行许可证；一次动作只有在主体、动作、资源、约束、策略、审批与执行点都对得上时，才有资格进入执行边界。`

假设 BuildPilot 已经完成一次构建失败调查。日志、配置和制品身份都被固定，诊断 Claim 也通过了 Evidence acceptance。现在模型提出下一步：把构建制品发布到外部目标。

这时最危险的推理是：

> 诊断已经可信，所以可以执行。

Article 18 解决的是“这个 scoped Claim 凭什么被当前 Evidence policy 接受”。它没有回答谁能使用发布凭据、能发布哪一份制品、目标是不是生产环境、批准到什么时候有效，也没有让任何 Runtime 获得真实 credential。一个判断被接受，最多说明它达到了当前证据门槛；动作是否获准，仍要经过另一条 authority chain。

> **构造关系图｜COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> accepted Evidence
>   = 这个 scoped Claim 达到当前证据门槛
>
> action authority
>   = 这个 Principal 此刻能否在这些 Constraints 下
>      对这个 Resource 执行这个 Action
>
> accepted Evidence != credential != approval != execution authority
> ```

这篇要处理的，就是这条经常被一个“允许”开关吞掉的控制链。我们会先拆开 Permission、Authorization、Approval、Human-in-the-loop 与 Sandbox，再建立最小 action-authority 模型，最后把它落到一份 BuildPilot 高风险动作设计中。

本文没有 Lab，没有实验，也没有运行 BuildPilot。Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。所有 BuildPilot 结构、状态机、风险路由和记录字段都属于 **DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**；它们不证明安全、成功、可靠性、成本或收益。

## 一个“允许”开关，吞掉了哪些责任

Tool 已注册、参数通过 Schema、credential 存在、Evidence 被接受，看起来都像“可以继续”的信号。但这些信号回答的是不同问题。

- Tool 已注册，只说明当前执行方知道这个能力对应哪个 handler。
- 参数合法，只说明候选请求满足数据合同。
- credential 存在，只说明某种凭据可以被取得或已经暴露在环境中。
- Evidence 被接受，只说明某个 Claim 在指定 scope 下通过了证据门槛。
- Sandbox 已配置，只说明某些运行时接触面可能被限制。

它们都没有单独回答：**这个主体，此刻，能否用这份凭据，对这个具体资源，执行这组具体参数所描述的动作。**

于是下面四个等式都不成立：

> **错误等式对照｜COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**

```text
tool registered = allowed now
credential exists = authorized use
approval clicked once = any later request approved
sandboxed = business action permitted
```

这也接回 Article 06 的两条边界：`Schema Valid != Policy Allowed`，`Sandbox != Permission`。Tool Runtime 可以在 Policy Gate 返回 `APPROVAL_REQUIRED` 并保持 handler 未进入，但“需要审批”并不等于已经拥有一套完整审批系统。身份、请求冻结、approver、有效期、撤销与恢复重验证，仍要由独立责任面补齐。

人工出现在流程里也不自动形成控制。有人看过页面、在聊天里说“可以”，或者点过一次 Approve，都不足以说明：这个人是谁、看的是哪份请求、批准范围是什么、决定是否过期、请求后来有没有变化，以及恢复时是否重新检查了当前策略。

所以 Approval 与 Human-in-the-loop 也不能混写：

- Approval 是一个可归因、可定界、可过期、可撤销的 decision。
- HITL 是需要人类决定时 pause，把 decision 绑定回原请求，并在 resume 前重验证的 control flow。

“人类参与过”是一个事件事实；“当前动作拥有可执行 authority”则需要完整的绑定与执行检查。人工也可能误判，因此 Approval 不能被写成安全正确性的证明。

## 先把五个概念分账

下面的五分法是本课程为 Agent workflow 建立的工作模型。访问控制标准、平台文档与操作系统机制支持其中若干责任分离，但没有一个来源把这五项规定为统一行业 taxonomy。

> **五概念分账表｜COURSE DESIGN / SYNTHETIC / PARTIAL / NOT IMPLEMENTED / NOT RUN / NOT AN INDUSTRY-UNIFIED TAXONOMY**

| Concept | 本课程最小工作定义 | 主要回答 | 明确不能替代 |
|---|---|---|---|
| Permission | 预先配置或授予的 capability ceiling | 原则上可请求什么 | 当前 request 的 allow、人工 decision、runtime isolation |
| Authorization | 当前 Principal、Action、Resource、Constraints 经 Policy 得到的决定 | 这次 request 是 allow、deny，还是 approval-required | 永久 Permission、人工责任或实际 Enforcement |
| Approval | 有身份、有 scope、有期限、可撤销的主体对冻结请求作出的显式决定 | 谁对哪份 request 决定 approve/reject | 扩大 request、永久权限或动作 exactly-once |
| HITL | 需要人类决定时 pause，绑定 decision，并在 resume 前重验证的控制流 | 何时停、怎样决定、怎样恢复 | decision 本身、业务 Policy 或 Sandbox mechanism |
| Sandbox | 隔离或过滤运行时接触面的机制组合；本篇只确认 Linux namespaces、network namespace 与 seccomp | 代码运行时能接触哪些 surface | 身份、业务授权、审批正确性或完整安全保证 |

可以把这五项压缩成一句话：

`Permission 定上界，Authorization 判本次请求，Approval 承担显式责任，HITL 管暂停恢复，Sandbox 收窄执行面。`

这里的证据强度并不覆盖所有可能的 Sandbox surface。本文直接确认的机制范围只有 Linux namespaces、network namespace 与 seccomp；filesystem view/mount/allow-write 及 secret broker/mount/environment 只会作为 **COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE** 出现，不能与前三者共享 confirmed conclusion。

这几个对象可以由同一个产品界面承载，也可以分散在 policy engine、approval service、credential broker、workflow runtime 与 operating-system controls 中。组件如何部署不是本篇的统一规定；本篇只要求审查时能够回答每类决定由谁拥有，以及一层的 PASS 为什么不能替代另一层。

OpenAI Agents SDK 当前 Human-in-the-loop 文档可以作为一个产品限定例子：文档描述了按 tool call 中断、approve/reject、可序列化 RunState 与 resume 的交互。它只证明该产品文档当前呈现了这样一种交互，不证明通用 HITL 协议、跨版本行为、resume revalidation、exactly-once 或安全保证。

## 授权发生在五个时点，而不是一次布尔判断

把授权理解为 `allowed=true`，会丢掉时间。Permission 可能在几个月前 provision；Approval 可能在十分钟前作出；真正执行发生时，policy、resource、credential 或 revocation state 都可能已经变化。

下面的五时点是课程分层设计。NIST 的访问判定、动态授权、decision/enforcement separation 与最小权限控制为分层提供了依据，但具体阶段和顺序不是标准原文工作流。

> **五个判定时点｜COURSE DESIGN / SYNTHETIC / PARTIAL / NOT IMPLEMENTED / NOT RUN**

| Time | Decision object | Minimum question | PASS does not prove |
|---|---|---|---|
| Provision | Permission / capability | 主体原则上可请求哪些 action/resource scope？ | 当前请求获准 |
| Request | Authorization | 当前上下文与 policy 对冻结请求返回什么？ | 人工已批准或执行已发生 |
| Human decision | Approval | 指定 approver 对哪个 digest、scope、expiry 作出什么决定？ | 请求变化后仍有效 |
| Resume | Revalidation | principal、digest、resource、policy、scope、expiry、revocation 是否仍成立？ | use-time 状态不再变化 |
| Use time | Enforcement | 此刻具体 action 是否满足全部输入，并在允许 surface 内执行？ | 动作一定安全、成功或有收益 |

五个时点分别保护不同窗口：Provision 控制能力上界；Request 处理当前上下文；Human decision 固定责任主体和批准对象；Resume 处理等待期间的漂移；Use time 把决定落到最接近副作用的位置。

因此，Authorization 不是一次性布尔值，而是一组在不同时间、对不同对象负责的判定。每次 PASS 都只是下一层的一个输入，不能替下一次 PASS。

## 最小 action-authority chain

如果只保存 `allowed=true`，事后无法回答它允许谁、做什么、对哪个目标、有效到何时、由哪版 policy 决定，又在哪里被强制。课程为此提出一条最小 authority chain：

> **Authority chain｜COURSE PROPOSAL / SYNTHETIC / AUDITABLE DESIGN / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> Principal
>   -> Capability / concrete Action
>   -> Resource
>   -> Constraints
>        scope + environment + expiry + request_digest
>   -> Policy (id + version)
>        ALLOW | DENY | APPROVAL_REQUIRED
>   -> Approval (only when required; frozen request only)
>   -> use-time Enforcement
>        revalidate -> execute or deny
> ```

这条链里的对象各自承担一个不能被省略的责任。

### Principal：让请求和决定可归因

Principal 是动作请求的责任主体。它可能是用户、服务身份或受委托的执行身份，但不能只写一个 session name 就假装完成身份绑定。请求者、approver 与 credential 使用者也不应因为都出现在同一进程里就被视为同一个 principal。

### Capability 与 Action：上界不等于本次动作

Capability 表达可被授予和治理的一类能力上界，例如“发布制品”。Action 则是本次具体候选：对哪个 artifact、哪个 target、使用哪些 parameters。只保存宽泛 tool name，会让一次窄批准被误用到更宽请求。

### Resource：批准必须指向具体目标

`production`、`release`、`workspace` 这类名称通常太模糊。Resource 要绑定可识别的目标与作用域，例如 exact environment、artifact identity 或 repository path。否则 approver 看到的 A 与 enforcement 使用的 B 可能只是名称相似。

### Constraints：把范围和时间写进授权

最小 Constraints 至少包括 scope、environment、expiry 与 request digest。还可以承载组织需要的其他限制，但不能在缺项时从自然语言猜测。request digest 的职责，是让“批准的请求”和“准备执行的请求”能够做内容身份比较。

### Policy：保留决定依据的身份和版本

Policy 输出 `ALLOW / DENY / APPROVAL_REQUIRED`，同时保留 `policy_id/version`。如果规则在等待期间变化，resume 不能只复用旧布尔结果；它需要重新判断新版本是否仍允许当前路径。

### Approval：只能确认或收窄冻结请求

Approval 只能绑定被冻结的 request。它不能把 resource 扩大、parameters 改写、期限延长，也不能越过 capability ceiling 或 hard policy deny。R3 hard deny 不会因为有人点击 Approve 就变成 R2。

### Enforcement：在 use time 强制，而不是只留下建议

Enforcement 是动作进入实际执行面的最后检查点。没有可用的 enforcement point，即使上游记录了 allow，也不能据此声称真实执行一定受到了该限制。执行点还要重验当前输入，随后才把候选交给 Tool Runtime 或其他执行 owner。

这是一套可审计设计，不是 NIST 原文模型、现成协议或已经实现的 BuildPilot 架构。课程选择的 fail-closed 原则是：关键 identity、resource、constraints、policy version 或 enforcement input 缺失时，deny、保持 approval-required，或进入明确 blocked route；不让模型补猜授权。

## 先按风险路由，再决定自动、审批或拒绝

并不是所有动作都需要相同的人类介入。纯读取公开、非敏感数据，与修改权限或向外部生产目标发布制品，风险面显然不同。但风险等级也不应由模型临场自由解释。

下面的 R0—R3 是课程建议，组织需要按自己的资源、威胁、合规和恢复能力版本化定义。它不是行业标准，也没有证据证明它普适最优。

> **风险路由表｜COURSE PROPOSAL / SYNTHETIC / ORGANIZATION-SPECIFIC POLICY INPUT / NOT IMPLEMENTED / NOT RUN**

| Route | Course-level example | Default handling | Boundary |
|---|---|---|---|
| R0 | 纯读且无敏感输出 | 可按当前 policy 自动 | 仍需明确 principal、resource、scope |
| R1 | 本地、可逆、边界明确 | 可按 policy 自动，或按组织规则要求 approval | “可逆”与边界必须由组织定义 |
| R2 | 外部发布、凭据使用、权限变更、不可逆或破坏性动作 | 必须显式 approval | approval 仍受 digest、scope、expiry 限定 |
| R3 | 超出 hard policy | 直接 deny | 人工 approval 不能越过 hard deny |

R2 与 R3 的差别不是风险分数高低，而是控制路径不同：R2 表示 policy 允许在满足指定 approver、scope 与期限后继续；R3 表示该动作超出当前系统允许边界，没有“多找一个人点批准”这条路径。

未知 action、resource 或 risk 不能由模型自行归入 R0。课程设计选择默认 deny 或 approval-required，让风险字典缺口显式暴露。风险路由回答的是“进入哪条控制路径”，不是给动作贴一个永久不变的标签。

这里也必须停在 Budget 的边界。本篇不定义 Token、Step、Cost、Latency 的额度、计账、耗尽或调度策略；风险路由不是预算模型。

## ApprovalRequest 与 DecisionRecord：批准必须绑定冻结请求

一个 Approve 按钮事件为什么不够？因为它通常只保存“有人点了”，却没有保存点之前看到的精确请求。参数、目标或制品后来发生变化时，系统仍可能把旧决定套到新动作上。

课程提出把请求和决定分成两份记录。

> **审批记录表｜COURSE PROPOSAL / SYNTHETIC / MINIMUM AUDIT DESIGN / NOT A PRODUCT SCHEMA / NOT IMPLEMENTED / NOT RUN**

| Record | Minimum fields | Audit purpose |
|---|---|---|
| ApprovalRequest | `request_id`、principal、action、resource、parameters/request_digest、constraints、risk_reason、policy_id/version、requested_at、expires_at、required_approver rule | 固定“请求了什么、为什么需要审批、哪版 policy 要求谁决定” |
| DecisionRecord | `decision_id`、request_id/digest、approver principal、approve/reject、decision scope、reason、decided_at、expires_at、revocation state、policy version | 固定“谁对哪份请求作出什么、有效到何时、是否已撤销” |

> **审批记录片段｜COURSE DESIGN / SYNTHETIC / NOT A PRODUCT SCHEMA / NOT IMPLEMENTED / NOT RUN**
>
> ```yaml
> approval_request:
>   request_id: REQUIRED_NOT_CREATED
>   principal: REQUIRED
>   action: REQUIRED
>   resource: REQUIRED
>   request_digest: REQUIRED_NOT_COMPUTED
>   scope: REQUIRED
>   policy_id: REQUIRED
>   policy_version: REQUIRED
>   expires_at: REQUIRED
>   required_approver_rule: REQUIRED
> decision_record:
>   decision_id: NOT_CREATED
>   request_id: REQUIRED_NOT_BOUND
>   request_digest: REQUIRED_NOT_BOUND
>   approver_principal: NONE
>   decision: NOT_RUN
>   expires_at: UNKNOWN
>   revocation_state: UNKNOWN
> ```

这份片段故意保留 `REQUIRED / UNKNOWN / NOT_RUN`。没有真实 ID、时间或决定时，填一个看起来合理的值不是“让示例完整”，而是伪造审计数据。

绑定规则至少包括：

1. digest 不同，旧 decision 不可复用；
2. resource 更宽、parameters 改变或 scope 扩大，旧 decision 不可复用；
3. request 或 decision 已过期，不能直接继续；
4. revocation state 表明已撤销时，必须重新判定；
5. `approve` 与 `reject` 都是正式结果，不能只记录成功批准；
6. Approval 不能扩大 capability ceiling，也不能覆盖 hard policy deny。

因此 Approval 的最小单位不是一次按钮事件，而是一份 decision 对一份冻结 request 的可审计绑定。

## HITL 是状态机，不是一段聊天

当 Authorization 返回 `APPROVAL_REQUIRED`，Runtime 不能一边继续执行，一边异步等人回复；也不能恢复时只读取一句“同意”。它需要把 request identity、等待状态、决定结果和下一合法 transition 保存下来。

下面是课程提出的最小 HITL 状态机。

> **HITL 状态机｜CONSTRUCTED COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> READY
>   -> AUTHZ_CHECK
>      -> DENIED
>      -> WAITING_APPROVAL
>           -> APPROVED
>           -> REJECTED
>           -> CANCELLED
>           -> EXPIRED
>
> APPROVED
>   -> REVALIDATING
>      -> DENIED
>      -> READY_TO_EXECUTE
>           -> EXECUTING
>              -> SUCCEEDED | FAILED
> ```

状态机至少要守住四条 transition guard。

第一，进入 `WAITING_APPROVAL` 前，必须持久化冻结 request/digest 与 required approver rule。否则批准回来时没有对象可以绑定。

第二，`REJECTED / CANCELLED / EXPIRED` 都是正式终止结果，不能直接 resume。若需求仍然存在，应创建一份新 request identity，再重新走 Authorization。

第三，只有 `APPROVED` 可以进入 `REVALIDATING`，但 Approval PASS 不等于 execution PASS。恢复期间发生的 policy、resource 或 revocation 变化，都可能让新判定进入 `DENIED`。

第四，系统要区分 workflow decision 与 action outcome。`READY_TO_EXECUTE` 只表示当前 authority inputs 允许进入 execution candidate；它不证明 handler 已调用、Tool 成功、外部副作用完成或 Sandbox 配置正确。

Pause 的工程意义，是保存一份以后仍能重验的 request；Resume 的工程意义，是在新时点恢复控制，而不是把旧调用从停顿处无条件放行。

## Resume 前要重新验证什么

批准发生后，真实执行之前，系统至少要再问一次：“现在准备做的，还是当时批准的同一件事吗？”

一个最小 revalidation checklist 包括：

1. 当前 request digest 与 approved digest 是否相同；
2. requester、approver identity 与 credential binding 是否仍成立；
3. policy version 或 hard policy 是否变化；
4. resource identity、resource state 与批准 scope 是否仍匹配；
5. Approval 与 credential 是否已到期；
6. Permission、Approval 或 credential 是否已撤销；
7. execution surface 是否仍满足环境与 Sandbox constraints。

任何一项失败，都不能靠旧 `APPROVED` 覆盖。Revalidation 的目的也不是证明未来不再变化，而是尽量把 decision 与 use 之间的事实差缩短到可控制范围。

### Decision idempotency 不等于 action exactly-once

HITL 系统还经常把两种幂等混在一起。

`decision_id` 可以让同一 approver 的重复提交返回同一个 decision outcome。例如网络重试导致 Approve 被发送两次，approval service 可以识别这是同一决定，不产生两份互相冲突的 DecisionRecord。

但这不能推出外部动作只发生一次。动作是否可安全重试，仍取决于 Tool Runtime 的 invocation identity、canonical request digest、业务副作用协议，以及 Long-running Runtime 对 response-loss window 的处理。Article 11 已经说明：稳定 identity 能帮助 lookup/reconcile 或同 intent 重放，却不提供通用 exactly-once 保证。

> **两类幂等对照｜COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**

| Concern | Identity | 可以约束什么 | 不能证明什么 |
|---|---|---|---|
| Approval decision | `decision_id` + bound request/digest | 重复 approve/reject 返回同一决定结果 | Tool 没有二次执行、外部 effect exactly-once |
| Action execution | invocation/action identity + intent digest + business protocol | 按具体合同识别 replay、conflict、lookup 或 reconcile 候选 | 所有外部副作用只发生一次 |

所以 `decision idempotency != action exactly-once`。本篇不设计 Retry、Recovery、compensation 或分布式事务；它只要求 authority 记录不要冒充副作用保证。

## Least privilege 要落到一次动作

“使用低权限账号”还不是最小权限设计。一个账号名可能覆盖大量 action、resource 与环境，也可能拥有远长于任务需要的 credential lifetime。

对一次 Agent 动作，更可审查的 least-privilege 交集是：

> **最小权限交集｜COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**

```text
minimum action
  ∩ exact resource
  ∩ narrow scope
  ∩ required environment
  ∩ shortest useful expiry
```

Capability 与 credential 只授予完成任务所需的最小 action/resource/scope 和最短期限。Permission、Approval 与 delegated credential 分别需要 expiry/revocation；其中任何一项失效，都要求重新判定，而不是只看最外层角色名。

OAuth 2.0 的 scope、expiry 与 token revocation 提供了一个协议限定的先例：delegated credential 可以带范围与期限，也可以被撤销；但这不表示 OAuth token 就是 Approval，更不表示撤销能在所有分布式组件中瞬时传播。实现可能存在传播窗口，设计和运维判断都要把这个窗口写出来。

这些控制的证明上限也必须收窄：least privilege、scope、expiry 和 revocation 可以降低暴露面，不证明零风险、不证明即时撤销，更不证明动作一定正确。

## TOCTOU：check 通过后，use 之前世界可能已经改变

Authorization 和 Approval 都通过后，Resource 仍可能在执行前被替换，权限可能被撤销，policy 可能升级，目标环境也可能发生切换。这就是 check 与 use 分离带来的 TOCTOU 风险。

> **TOCTOU 序列｜COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> check principal / resource / policy / approval
>      -- time passes / state changes -->
> use credential on resource
>
> mitigation direction:
>   revalidate at resume / use time
>   + shorten the check/use window
>   + move check and use into the same enforcement boundary where possible
> ```

MITRE CWE-367 支持一个窄事实：check 与 use 之间的状态变化会让已检查条件失效，缩短窗口、重检或合并边界是缓解方向。它不证明 revalidation 能消灭所有竞态，也不是完整并发控制规范。

因此，越靠近副作用执行点，越应该用当前事实重验；但“重验过”仍不能升级成“之后不会再变化”。外部系统状态、撤销传播和并发 actor 都可能继续改变世界。

## Sandbox：逐机制说明限制面，不做总括安全承诺

Sandbox 最容易被误写成一个可以盖章的属性：进程“在 Sandbox 里”，所以动作安全。但 Sandbox 实际由一组具体机制和配置组成，每项只控制一部分 surface。

> **Sandbox mechanism / limit matrix｜COURSE DESIGN / SYNTHETIC / MIXED EVIDENCE POSTURE / NOT IMPLEMENTED / NOT RUN**

| Evidence posture | Mechanism family | 可以按具体配置陈述的限制面 | 不能据此自动断言 |
|---|---|---|---|
| `CONFIRMED / 19-E09` | Linux namespaces | 各 namespace 所管辖的 resource view | 多种 namespace 组合已形成完整 Sandbox |
| `CONFIRMED / 19-E09` | Network namespace | network devices、protocol stacks、routing tables、firewall rules 等 network resources 的隔离视图 | 信息不会泄露、目标业务调用获批 |
| `CONFIRMED / 19-E09` | seccomp syscall filter | 允许的 syscall / kernel surface | 完整 Sandbox、安全无逃逸或逻辑行为正确 |
| `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE` | Filesystem view、mount、allow-write policy | 示例目标：限定 path visibility / write surface | 本篇已验证具体 filesystem mechanism/configuration |
| `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE` | Secret broker、mount、environment policy | 示例目标：限定 credential exposure / scope / lifetime | 本篇已验证具体 secret-delivery mechanism/configuration |

`19-E09` 的 confirmed conclusion 只覆盖前三行：Linux namespace 文档支持按 namespace 类型描述隔离的资源视图；network namespace 可以分离 network devices、protocol stacks、routing tables、firewall rules 等网络资源；seccomp filter 用于限制进程可进入的 syscall surface。后两行只是课程设计示例，用来提醒读者还可能需要 filesystem 与 credential exposure controls；本文没有为它们核验具体机制或配置。

但 Linux kernel 文档明确提醒：seccomp filtering 本身不是 Sandbox。它缩小 kernel surface，却不了解业务逻辑，也不是信息流策略；完整 hardening 仍可能需要其他机制与 LSM。这个限制必须保留，因为“加载了 seccomp profile”不能被写成“拥有完整、安全、无逃逸的 Sandbox”。

在不升级证据强度的前提下，还可以做下面这些反例检查：

- read-only filesystem 不证明没有信息泄露（`COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`）；
- no-network 配置不证明没有其他副作用；
- network namespace 不证明业务 endpoint 已获批准；
- secret broker 若只暴露短期 credential，也不能由此推出 action 合法（`COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`）；
- Approval 存在，也不证明 Sandbox configuration 正确。

多种机制可以组合收窄执行面，但机制组合存在不证明配置正确，更不证明安全收益。每项保证都应该明确 mechanism、configuration、resource scope、threat/limit、enforcement point 与未覆盖面；信息不足时，只能描述机制存在，不能升级为安全结论。

Sandbox 限制“运行时能碰到什么”，Authorization/Approval 决定“这次是否允许做”。两者要协作，却不能互相替代。

## BuildPilot：让 Evidence package 进入 authority chain，而不是直接接 Tool

现在回到开头的外部发布动作。Article 18 的 accepted Evidence package 可以成为 authority 判断的输入之一，但不能直接触发 Tool。课程为 BuildPilot 提出一份 `ActionAuthorityEnvelope` 候选，把尚未闭合的 authority inputs 显式保存下来。

> **BuildPilot 流程｜CONSTRUCTED COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN**
>
> ```text
> accepted Evidence package                 [Article 18 boundary]
>     -> ActionAuthorityEnvelope candidate  [Article 19 proposal]
>     -> policy: APPROVAL_REQUIRED
>     -> WAITING_APPROVAL
>     -> DecisionRecord
>     -> resume revalidation
>     -> sandboxed Tool Runtime enforcement [Article 06 boundary]
>     -> execution/result remains NOT RUN in this article
> ```

> **BuildPilot `ActionAuthorityEnvelope`｜CONSTRUCTED COURSE DESIGN / SYNTHETIC SHAPE / NOT IMPLEMENTED / NOT RUN**
>
> ```yaml
> envelope_id: BP-AAE-DESIGN-001
> principal: BUILD_PILOT_OPERATOR_REQUIRED
> capability: PUBLISH_BUILD_ARTIFACT
> action:
>   name: publish_artifact
>   request_digest: REQUIRED_NOT_COMPUTED
> resource:
>   target: EXTERNAL_RELEASE_TARGET_REQUIRED
>   artifact_identity: REQUIRED_NOT_ACQUIRED
> constraints:
>   scope: EXACT_TARGET_AND_ARTIFACT_REQUIRED
>   environment: DESIGN_ONLY
>   expires_at: REQUIRED
> policy:
>   id: buildpilot-action-authority-course-v1-design
>   version: v1-design
>   decision: APPROVAL_REQUIRED
> approval:
>   request_state: NOT_CREATED
>   decision_state: NOT_RUN
> revocation_state: UNKNOWN
> enforcement:
>   revalidation: NOT_RUN
>   sandbox_profile: NOT_IMPLEMENTED
>   execution: NOT_RUN
> ```

这份 envelope 故意保留大量缺口。accepted Evidence 不会自动补齐 operator identity、artifact identity、release target、request digest、expiry 或 revocation state。它也不会自动创建 ApprovalRequest、DecisionRecord、credential 或 Sandbox profile。

让我们按 fail-closed 顺序走一遍。

1. `artifact_identity` 缺失：系统无法证明准备发布的是哪一份制品，请求不能冻结，保持 not authorized。
2. exact target 或 scope 缺失：风险路由不得把未知动作降到 R0/R1，保持 deny 或 approval-required。
3. request digest 未计算：ApprovalRequest 没有可绑定的内容身份，因此不可创建。
4. DecisionRecord 不存在：状态保持 `WAITING_APPROVAL / NOT_RUN`，不能因为 Evidence accepted 而跳过。
5. 即使未来产生 approve decision，Resume 仍要核对 digest、principal、policy、resource、scope、expiry 与 revocation。
6. Revalidation PASS 只允许进入 enforcement candidate；它不证明 Tool success、external publish 已发生、Sandbox 完整、安全或有收益。

这不是 Workflow run、HITL product demo、Sandbox test 或 release simulation，也不包含 Article 21 所拥有的 Trace/Replay 设计。可审计设计的成熟，不是让每条路径都能执行，而是让每个缺口都知道应该停在哪里。

## 相邻文章的责任边界

Action authority 横跨 Tool、State、Recovery 与 Evidence，但 L 权重不意味着 Article 19 可以把这些主题重新吞掉。

> **课程 ownership 表｜COURSE DESIGN / SYNTHETIC / REPOSITORY-LOCAL BOUNDARY / NOT IMPLEMENTED / NOT RUN**

| Owner | Owns | Article 19 只承接什么 | Article 19 不做什么 |
|---|---|---|---|
| Article 06 Tool Runtime | tool execution/result boundary、前置 Policy terminal | 把 Enforcement 落到 Tool Runtime seam | 不重讲 Validate/Result/Trace，不把其 Policy v1 当完整审批系统 |
| Article 10 State Machine / Workflow | 显式 State、legal Transition、Guard 与 commit | 把 approval-required、pause 与 resume 表达成确定性控制点 | 不重讲通用 Workflow 或 Agent Decision Point |
| Article 11 Long-running Agent | Checkpoint/Resume、side-effect/idempotency、revalidation seam | 要求 Resume 前重验，区分 decision idempotency 与 action effect | 不设计 Retry、Recovery、compensation 或 exactly-once |
| Article 18 Evidence Contract | Claim/Evidence acceptance | 以 accepted Claim 作为 authority 输入之一 | 不把 acceptance 当 Permission/Approval |
| Article 20 Budget | Token、Step、Cost、Latency budget | 只保留“约束仍有其他 owner”的接口 | 不定义额度、计账、耗尽或调度策略 |
| Article 21 Trace/Replay/Failure Taxonomy | 跨 step correlation、reconstruction/re-execution、failure layer | 要求未来记录可引用 request/decision identity | 不设计完整 Trace schema、Replay 或 Failure Taxonomy |

这个 ownership 是当前课程的 repository-local 事实，不是行业统一架构。Article 19 只回答 action authority、HITL 与 Sandbox；execution、通用状态推进、long-running recovery、预算和跨步骤重建仍由相邻责任面拥有。

## 一个 authority 设计通常怎样写坏

> **反模式表｜COURSE DESIGN / SYNTHETIC / DESIGN-REVIEW HEURISTIC / NOT IMPLEMENTED / NOT RUN**

| 捷径 | 被吞掉的责任 | 最小修正方向 |
|---|---|---|
| Evidence accepted -> execute now | authority chain | 独立检查 principal/action/resource/constraints/policy/approval/enforcement |
| Tool registered或credential存在 -> allowed | current Authorization | 把 capability ceiling 与本次 request 分开 |
| policy allow once -> permanent authorization | time、version、revocation | 在 Request、Resume、Use time 重验 |
| approve button -> changed request approved | frozen request binding | 比较 request/digest/resource/scope |
| decision idempotent -> action exactly-once | execution/effect protocol | 回到 invocation identity 与业务副作用合同 |
| resume -> skip revalidation | TOCTOU 与漂移 | 检查 policy/resource/expiry/revocation |
| revocation -> instant propagation | distributed propagation window | 明确传播窗口与 use-time handling |
| Sandbox exists -> safe/successful/beneficial | mechanism/config/evidence boundary | 逐机制陈述限制面和未覆盖面 |
| unknown risk -> infer R0 | risk policy owner | deny 或 approval-required，补齐分类 |
| planned request/decision IDs -> Trace complete | cross-step record ownership | 停在 Article 21 边界 |

这些坏法有同一个根因：把“某一层通过”误写成“后面所有层都通过”。修复也不是再加一个 `safe=true`，而是回到当前时点的完整 authority inputs；缺输入时收窄、deny、等待明确 decision，或保持 `NOT_RUN`。

## 本篇能建立什么，不能证明什么

本篇的证据强度保持为：3 个 CONFIRMED、2 个 PARTIAL、5 个 PROPOSAL。

能够建立的窄结论包括：

- accepted Evidence 与 action authority 是逻辑独立的判断；
- least privilege、scope、expiry、revocation 与 TOCTOU 是独立控制关注点；
- Linux namespace、network namespace、seccomp 等机制只能按具体机制和配置陈述限制面；seccomp filtering 本身不能被称为完整 Sandbox；
- access decision、policy 与 enforcement separation，scope/expiry/revocation，以及一个产品限定 HITL 交互，都可以为课程设计提供有界依据。

必须保持为课程模型或 Proposal 的内容包括：

- Permission/Authorization/Approval/HITL/Sandbox 五分法；
- Provision/Request/Human decision/Resume/Use-time 五时点工作流；
- authority chain、R0—R3 风险路由；
- ApprovalRequest/DecisionRecord 字段；
- HITL state machine；
- BuildPilot `ActionAuthorityEnvelope`。

当前明确不存在：

- 真实 Permission、credential、Approval、human decision、Sandbox、Enforcement 或 BuildPilot Runtime；
- Sandbox、Approval、least privilege 或 revalidation 带来的安全、成功、可靠性、成本、时延或收益保证；
- 统一行业 taxonomy、通用最优 risk route、标准 Approval schema 或通用 HITL protocol；
- Article 20 Budget 与 Article 21 Trace/Replay/Failure Taxonomy 的详细设计、实现或运行结论。

冻结现实仍是：Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`，BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。来源可以约束设计边界；没有 Runtime observation，就没有资格声称设计已经带来安全或收益。

## Claim Traceability（10 / 10）

> **Claim 覆盖表｜COURSE DESIGN / SYNTHETIC / DRAFT AUDIT VIEW / NOT IMPLEMENTED / NOT RUN**

| Claim | Evidence ceiling | 正文主落点 | 保留的边界 |
|---|---|---|---|
| `19-C01` | CONFIRMED | 开场、问题空间、BuildPilot 输入 | accepted Evidence 与 authority 独立，不产生真实权限 |
| `19-C02` | PARTIAL | 五概念分账、人工参与边界 | 只称课程 working model，不称行业 taxonomy |
| `19-C03` | PROPOSAL | authority chain、BuildPilot envelope | 可审计设计，不称标准模型或已实现协议 |
| `19-C04` | PARTIAL | 五个判定时点 | 分层有来源支持，具体 workflow 为课程设计 |
| `19-C05` | PROPOSAL | R0—R3 风险路由 | 不称普适最优，未知输入 fail closed |
| `19-C06` | PROPOSAL | ApprovalRequest / DecisionRecord | 最小审计设计，不称产品 schema |
| `19-C07` | PROPOSAL | HITL 状态机、Resume、两类幂等 | 产品例子不外推；decision idempotency 不等于 action exactly-once |
| `19-C08` | CONFIRMED | least privilege、expiry/revocation、TOCTOU | 只说降低暴露面，不保证零风险或即时撤销 |
| `19-C09` | CONFIRMED | Sandbox mechanism/limit matrix | 只确认 Linux namespaces/network namespace/seccomp；filesystem/secret broker 为未取证课程示例 |
| `19-C10` | PROPOSAL | BuildPilot walk-through、相邻文章边界 | DESIGN / NOT IMPLEMENTED / NOT RUN，不提前完成 Budget 或 Trace/Replay |

Coverage=`10 / 10`；new core Claim=`NONE`；core `BLOCKED=0`。正文保持 `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL`，没有把产品文档、课程设计或 mechanism presence 升级成通用运行保证。

## Learning Check

1. BuildPilot 的一个诊断 Claim 已经 accepted，为什么仍不能直接执行外部发布动作？还缺哪些 authority inputs？
2. Permission、Authorization、Approval、HITL 与 Sandbox 分别回答什么？哪两组最容易被误写成同一个“允许”？
3. authority chain 缺少 request digest 或 policy version 时，为什么应 fail closed？
4. Provision、request Authorization、human decision、Resume revalidation 与 use-time Enforcement 分别在什么时点作决定？为什么前一步 PASS 不能替后一步？
5. 未知 action 为什么不能由模型自行归入 R0？R2 与 R3 的控制路径有什么本质差别？
6. ApprovalRequest 与 DecisionRecord 为什么必须分开？request parameters 或 resource 变化后，旧 Approval 能否继续使用？
7. `decision_id` 让重复 approve 返回同一结果，为什么这仍不能证明 action exactly-once？
8. Approval 未过期，但 policy、resource state 或 revocation state 已变化，Resume 应怎样处理？
9. `19-E09` 对 seccomp、network namespace 确认了什么？为什么 read-only filesystem 在本篇只能作为未取证课程示例？这些内容为什么都不能替代 Authorization/Approval？
10. BuildPilot `ActionAuthorityEnvelope` 的 `execution: NOT_RUN` 为什么必须保留？Article 20/21 还各自拥有哪一段问题？

### 参考答案

1. Evidence acceptance 只确认 scoped Claim 达到证据门槛。仍缺 principal、exact action/resource、constraints、current policy、必要 Approval、expiry/revocation 与 use-time Enforcement；它也不会签发 credential。
2. Permission 定 capability ceiling；Authorization 判当前请求；Approval 是责任主体对冻结请求的决定；HITL 管 pause/decision/resume；Sandbox 限制 runtime surface。最常混淆的是 Permission 与 Authorization，以及 Approval 与 HITL；Sandbox 也经常被误当成前三者。
3. 没有 digest，无法证明执行对象与批准对象相同；没有 policy version，无法重建决定依据或判断规则是否漂移。继续执行会靠猜测补齐 authority，因此应 deny、保持 approval-required 或 blocked。
4. Provision 决定能力上界；Request 对当前上下文做 Authorization；Human decision 对冻结请求作 Approval；Resume 重验等待期间是否发生变化；Use time 在副作用前 Enforcement。它们面向的对象和时点不同，所以前一步 PASS 只是后一步输入。
5. 模型不是风险 taxonomy 的 authority，未知输入降级会绕过 policy。R2 是 policy 允许在满足显式 Approval 后继续；R3 是 hard deny，没有人工覆盖路径。
6. ApprovalRequest 固定请求内容与审批要求，DecisionRecord 固定谁对它作出什么决定。参数、resource、scope 或 digest 变化后已经是另一份请求，旧 Approval 不可复用。
7. `decision_id` 只约束重复 decision delivery。Tool 是否二次执行、外部 effect 是否重复，取决于 action identity、intent digest、业务幂等与 recovery 协议，不能从审批幂等推出 exactly-once。
8. 进入 `REVALIDATING`，按当前 policy、resource、scope、expiry 与 revocation 重新判断；任一关键条件不再成立就 deny 或创建新 request，不能直接进入 execution。
9. `19-E09` 确认 seccomp 限制 syscall surface，network namespace 分离一部分 network resources；它没有覆盖 filesystem configuration，所以 read-only filesystem 在本篇只能说明课程想讨论的一个限制目标，不能当作已核验机制事实。无论已确认机制还是未确认示例，都不回答业务主体是否获准，也不证明完整 Sandbox、安全或无信息泄露。
10. `NOT_RUN` 保留“没有真实执行证据”的事实，防止设计被误读成 runtime result。Article 20 拥有 Token/Step/Cost/Latency budget；Article 21 拥有跨步骤 Trace、Replay 与 Failure Taxonomy。

## Job Competency Mapping

> **职业能力映射表｜COURSE DESIGN / SYNTHETIC / READER-VISIBLE RUBRIC / NOT IMPLEMENTED / NOT RUN**

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Authority modeling | 五概念 ledger 与 authority chain | 能把 capability ceiling、current decision、human decision、control flow 与 isolation 分账 | 课程 working model，不称行业 taxonomy |
| Policy / governance design | risk route 与 ApprovalRequest/DecisionRecord | 能绑定 principal、request digest、scope、policy version、expiry、revocation 与 approver | Proposal schema，不称已实现 service |
| Reliable workflow design | HITL state machine 与 Resume checklist | 能设计 reject/cancel/expired terminal，并在 Resume/Use time 重验证 | NOT RUN，不证明 exactly-once |
| Security boundary reasoning | least privilege/TOCTOU 与 Sandbox matrix | 能逐机制说明限制面和未覆盖面，拒绝总括安全保证 | 不做完整 threat model 或 production assurance |
| Cross-system architecture | Articles 06/10/11/18/20/21 ownership | 能把 execution、state、recovery、evidence、budget、trace 与 authority 分层 | repository-local ownership，不等于行业统一架构 |
| Evidence discipline | 10/10 Claim coverage 与 wording ceilings | 能让 CONFIRMED/PARTIAL/PROPOSAL 语态匹配来源强度 | Required Lab NONE；runtime ABSENT |

这些能力的可观察产物不是“会不会说安全术语”，而是能否指出谁拥有决定、记录绑定了什么、恢复前要重验什么，以及证据不足时系统为什么必须停止。

## 参考资料

- [NIST SP 800-162：Guide to Attribute Based Access Control Definition and Considerations](https://csrc.nist.gov/pubs/sp/800/162/upd2/final)（subject/object/operation/environment、policy decision 与 enforcement boundary）
- [NIST SP 800-207：Zero Trust Architecture](https://csrc.nist.gov/pubs/sp/800/207/final)（动态、按 session 的授权与 grant/deny/revoke 分工）
- NIST SP 800-53 Rev. 5.1.1：[`usnistgov/oscal-content` official release/tag `v1.2.0`](https://github.com/usnistgov/oscal-content/releases/tag/v1.2.0)；[tag-pinned OSCAL JSON catalog](https://raw.githubusercontent.com/usnistgov/oscal-content/v1.2.0/nist.gov/SP800-53/rev5/json/NIST_SP-800-53_rev5_catalog.json)（catalog metadata version `5.1.1+u2`；已逐项核对 AC-2、AC-3、AC-3(2)、AC-3(8)、AC-6、AC-24、AU-3）；[CSRC publication entry](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)仅作出版物入口
- [RFC 6749：The OAuth 2.0 Authorization Framework](https://www.rfc-editor.org/rfc/rfc6749.html)（scope 与 expiry 的协议限定语义）
- [RFC 7009：OAuth 2.0 Token Revocation](https://www.rfc-editor.org/rfc/rfc7009.html)（revocation 与实现相关传播窗口）
- [RFC 9110 §9.2.2：Idempotent Methods](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2)（HTTP method retry/idempotency 的有限语义，不是 action exactly-once）
- [MITRE CWE-367：Time-of-check Time-of-use Race Condition](https://cwe.mitre.org/data/definitions/367.html)（check/use race 与缓解方向；2026-08-25 访问）
- [Linux kernel：Seccomp BPF](https://www.kernel.org/doc/html/latest/userspace-api/seccomp_filter.html)（syscall surface 与明确机制限制；2026-08-25 访问）
- [Linux man-pages 6.18：namespaces(7)](https://man7.org/linux/man-pages/man7/namespaces.7.html)；[network_namespaces(7)](https://man7.org/linux/man-pages/man7/network_namespaces.7.html)（namespace 隔离对象）
- [OpenAI Agents SDK Python：Human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/)（当前产品限定的 interruption、approve/reject、serialized state 与 resume 例子；2026-08-25 访问，本文未运行）
- Published Article 06：Tool Runtime（课程 tool execution/result boundary）
- Published Article 10：State Machine 与 Workflow（课程显式 State 与 legal Transition boundary）
- Published Article 11：Long-running Agent（课程 Checkpoint/Resume、side-effect/idempotency 与 revalidation seam）
- Published Article 18：Evidence Contract（课程 Claim/Evidence acceptance boundary）

## 最短结论

`真正可执行的 Agent 动作，不是“模型想做、证据支持、人点过按钮”三件事的叠加，而是一条在当前时点仍能被 Policy 与 Enforcement 复核的 action-authority chain。`

下一步才讨论另一类约束：Token、Step、Cost 与 Latency 怎样形成预算边界；再下一步才处理跨步骤 Trace、Replay 与 Failure Taxonomy。本篇不提前给出它们的字段、算法或运行结论。
