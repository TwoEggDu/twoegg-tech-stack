# Article 19 Research｜Permission、Approval、Human-in-the-loop 与 Sandbox

## Research metadata

- Status: COMPLETE
- Evidence Gate Recommendation: PASS
- Required Lab: NONE
- Source access date: 2026-08-25 (Asia/Shanghai)；SP 800-53 pinned-artifact recheck: 2026-08-26
- Experiment count: 0
- Runtime observation: ABSENT
- BuildPilot posture: DESIGN / NOT IMPLEMENTED / NOT RUN
- Research boundary: 只研究 authority model、approval lifecycle、HITL 与 sandbox limits；无实现、运行或收益/安全保证。

## Source and drift register

| Source | Version / locator | Used for | Drift boundary |
|---|---|---|---|
| NIST SP 800-162 | 2014, updated 2019, §§2.1–2.3 | subject/object/operation/environment、policy、PDP/PEP | 固定出版物；不外推为某产品实现 |
| NIST SP 800-207 | 2020, §§2.1、3.2、3.3 | per-session least privilege、动态授权、grant/deny/revoke enforcement | 固定出版物；zero trust 语境 |
| NIST SP 800-53 Rev. 5.1.1 OSCAL catalog | official `usnistgov/oscal-content` release/tag `v1.2.0`；annotated tag `686109e7516295dce79e6db806e721492586da74` -> commit `1763607deb4ffb3d67c59dc669a7c6404a6f93a6`；tag-pinned JSON `https://raw.githubusercontent.com/usnistgov/oscal-content/v1.2.0/nist.gov/SP800-53/rev5/json/NIST_SP-800-53_rev5_catalog.json` | AC-2、AC-3、AC-3(2)、AC-3(8)、AC-6、AC-24、AU-3 | release notes state alignment with the SP 800-53 v5.1.1 CPRT release；catalog metadata title identifies Rev. 5.1.1 and version=`5.1.1+u2`；generic CSRC page is publication entry only |
| RFC 6749 / RFC 7009 / RFC 9110 | §§3.3, 4.2.2；§§2–3；§9.2.2 | scope/expiry、revocation、幂等重试 | 标准语义仅限各自协议，不等同 agent action exactly-once |
| MITRE CWE-367 | CWE 4.20, accessed 2026-08-25 | check/use race | 当前网页会漂移；仅取 TOCTOU 定义与缓解方向 |
| Linux kernel documentation | `userspace-api/seccomp_filter.rst`, accessed 2026-08-25 | seccomp 的明确能力上限 | moving source；仅限当前文档所述机制 |
| Linux man-pages | 6.18, `namespaces(7)`, `network_namespaces(7)` | namespace 隔离对象 | 版本限定；不证明完整 sandbox |
| OpenAI Agents SDK docs | HITL guide, accessed 2026-08-25 | 产品限定 pause/approve/reject/resume 示例 | moving product docs；不外推为通用保证 |
| Published Articles 06/10/11/18 | repository snapshot accessed 2026-08-25 | 课程 ownership boundary | 仓库局部事实；后续修订会漂移 |

### SP 800-53 v5.1.1 pinned control recheck

Official release identity: `usnistgov/oscal-content` `v1.2.0`；the release notes say the catalog was aligned with the NIST SP 800-53 v5.1.1 CPRT release. On 2026-08-26 the tag-pinned JSON above returned `200`, UTF-8 bytes=`10192497`, SHA-256=`ADC257A7A9019BED1EE11E108E62D74B531BA9C33EA89F1B9AD49FD9149ACA23`, and reported title `Electronic Version of NIST SP 800-53 Rev 5.1.1 Controls and SP 800-53A Rev 5.1.1 Assessment Procedures`, version `5.1.1+u2`, last-modified `2023-12-05T01:16:10`.

| OSCAL control ID | Presence / narrow semantic recheck |
|---|---|
| `ac-2` | present；account management includes account creation approval、valid access authorization and account lifecycle concerns |
| `ac-3` | present；enforces approved authorizations for logical access under applicable access-control policy |
| `ac-3.2` | present；dual authorization / two-person control for organization-selected actions |
| `ac-3.8` | present；enforces revocation after subject/object security-attribute changes and notes revocation timing may vary |
| `ac-6` | present；least privilege limits users/processes to authorized access necessary for assigned tasks |
| `ac-24` | present；access-control decisions are applied before enforcement and decision/enforcement may be separate entities |
| `au-3` | present；audit records establish event type、time、location、source、outcome and associated identities/entities |

This recheck supports only the existing `19-C04 / C05 / C06 / C08` mappings；it does not add a Claim or turn the course risk route、record schema or workflow into NIST-prescribed designs.

## Terminology ledger

- **Permission**：预先配置或授予的 capability ceiling；说明“原则上可请求什么”，不证明本次请求可执行。
- **Authorization**：把当前 Principal、Action、Resource、Constraints、Environment 输入 Policy 后得到的 allow/deny 决定。
- **Approval**：有身份、有作用域、有期限、可撤销的责任主体对一份冻结请求作出的显式决定；它不是永久权限。
- **HITL**：需要人类决定时暂停，把决定绑定到原请求，并在恢复前重验证的控制流；“有人看过”本身不是控制。
- **Sandbox**：在运行时隔离或过滤接触面的机制组合；本篇 primary-source confirmed scope 只到 Linux namespaces、network namespace 与 seccomp。filesystem view/mount/allow-write 及 secret broker/mount/environment 仅作 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`；Sandbox 不回答身份、业务策略或是否获批。

## Answers to the ten approved questions

### 1. Evidence 与 action authority

**19-C01 — CONFIRMED.** accepted Evidence 只说明某个 Claim 达到证据门槛；action authority 必须由独立的当前授权链产生。研究/审查通过不能生成 credential、扩大 permission、替代 approval 或越过 enforcement。**Wording ceiling：**可断言“二者逻辑独立”，不可断言任何课程 Evidence 已授予真实动作权限。

### 2. 五个概念的责任拆分

**19-C02 — PARTIAL.** 标准分别支持访问请求/策略/执行与机制边界；五分法是本课程为 agent workflow 建立的工作模型，不是某一标准的原文 taxonomy。Permission 定上界，Authorization 判本次请求，Approval 承担需要人工负责的显式例外/确认，HITL 管暂停与恢复，Sandbox 限制执行面。**Wording ceiling：**称“课程模型”，不称“行业唯一标准定义”。

### 3. 最小 authority model

**19-C03 — PROPOSAL.** 使用：`Principal -> Capability/Action -> Resource -> Constraints -> Policy -> Approval -> Enforcement`。其中 Constraints 至少包含 scope、environment、expiry、request digest；Policy 输出 allow/deny/approval-required；Approval 只能收窄或确认被冻结请求；Enforcement 在 use-time 校验后执行。缺任一关键输入、策略版本或执行点时 fail closed。**Wording ceiling：**这是可审计设计，不是已实现协议。

### 4. 五个判定时点

**19-C04 — PARTIAL.** 静态授予仅 provision capability；请求时 authorization 使用当前上下文；人工 approval 决定冻结请求；resume 前 revalidation 检查身份、资源、策略、scope、expiry、revocation；执行点 enforcement 对具体动作强制。任一层的 PASS 都不替代下一层。**Wording ceiling：**分层受到 NIST 模型支持，具体工作流为课程设计。

### 5. 风险路由

**19-C05 — PROPOSAL.** 路由表必须版本化并给出理由：R0 纯读且无敏感输出可自动；R1 本地、可逆、边界明确可按策略自动；R2 外部发布、凭据使用、权限变更、不可逆/破坏性动作必须显式 approval；R3 超出 hard policy 直接 deny。未知 action/resource/risk 默认 deny 或 approval-required，不从自然语言猜授权。此处不定义 Article 20 的 Token/Step/Cost/Latency budget。**Wording ceiling：**风险层级是课程建议，不宣称普适最优。

### 6. Approval records

**19-C06 — PROPOSAL.** `ApprovalRequest` 最小字段：request_id、principal、action、resource、parameters/request_digest、constraints、risk_reason、policy_id/version、requested_at、expires_at、required_approver rule。`DecisionRecord`：decision_id、request_id/digest、approver principal、approve/reject、decision scope、reason、decided_at、expires_at、revocation state、policy version。审批不能覆盖 digest 不同、资源更宽、参数变化或已过期请求。**Wording ceiling：**字段集是最小审计设计，不是现成产品 schema。

### 7. pause / decision / resume

**19-C07 — PROPOSAL.** 最小状态机：`READY -> AUTHZ_CHECK -> WAITING_APPROVAL -> {APPROVED|REJECTED|CANCELLED|EXPIRED}`；只有 `APPROVED -> REVALIDATING -> READY_TO_EXECUTE -> {EXECUTING -> SUCCEEDED|FAILED, DENIED}`。decision_id 使重复 approve/reject 返回同一决定；action idempotency 另由 idempotency key/业务协议处理，不能由审批幂等推出 exactly-once。resume 必须重验 request digest、principal、policy version、resource state、scope、expiry、revocation；reject/cancel/expired 不可直接 resume。**Wording ceiling：**状态机为设计提案；OpenAI SDK 仅是产品限定示例。

### 8. Least privilege、scope、revocation、TOCTOU

**19-C08 — CONFIRMED.** capability/credential 应按完成任务所需的最小 action/resource/scope 和最短期限授予；授权与审批都应可到期、撤销。检查与使用分离会产生 TOCTOU，因此恢复/执行时必须重新验证并尽量把检查与使用置于同一 enforcement boundary；撤销存在传播延迟时必须明确窗口。**Wording ceiling：**这些控制降低暴露面，不证明零风险或绝对即时撤销。

### 9. Sandbox guarantees and limits

**19-C09 — CONFIRMED（narrow Linux mechanism scope）.** 本篇直接确认的范围只有：Linux namespaces 隔离各 namespace 所管辖的资源视图，network namespace 分离网络资源，seccomp 限制 syscall / kernel surface；Linux kernel 同时明确说明 seccomp filtering “isn't a sandbox”，仍需 complementary hardening/LSM 来处理逻辑行为与信息流。filesystem view/mount/allow-write 以及 secret broker/mount/environment policy 只保留为 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`，不能进入本 Claim 的 confirmed mechanism conclusion。**Wording ceiling：**只把 namespaces/network namespace/seccomp 写成已确认机制边界；不得把示例、单一机制、容器或产品模式写成完整安全边界，也不得用 Sandbox 替代 Authorization/Approval。

### 10. BuildPilot 与相邻文章边界

**19-C10 — PROPOSAL.** BuildPilot 只展示设计态 `ActionAuthorityEnvelope`：固定 principal/action/resource/digest，记录 policy、approval、expiry/revocation，在 resume/use-time 重验证，再交给 sandboxed enforcement。Article 06 拥有 Tool Runtime 的 tool execution/result boundary；10 拥有 State Machine/Workflow 的显式 state 与 transition；11 拥有 long-running checkpoint/resume、side-effect/idempotency 与 revalidation seam；18 拥有 Evidence acceptance；19 只拥有 action authority/HITL/sandbox；20 才拥有 budget；21 才拥有 trace/replay/failure taxonomy。**Wording ceiling：**DESIGN / NOT IMPLEMENTED / NOT RUN；无 runtime、收益或安全保证。

## Counter-evidence and rejected shortcuts

- “证据已接受，所以允许执行”被 19-C01 排除。
- “批准一次即可永久恢复”被 expiry/revocation/use-time revalidation 排除。
- “审批请求幂等，所以动作 exactly-once”被 RFC 9110 的有限重试语义与 Article 11 边界排除。
- “用了 seccomp/namespace 就安全”与 Linux 官方限制冲突。
- 产品文档只能证明该产品当前文档中的交互，不证明通用协议或部署保证。

## Evidence Gate recommendation

- Recommendation: **PASS**
- Coverage: 10 / 10 approved questions；`19-C01`—`19-C10` 均有 Evidence Card 映射。
- Core BLOCKED Claims: 0
- Status mix: CONFIRMED = C01/C08/C09（C09 仅限 Linux namespaces/network namespace/seccomp）；PARTIAL = C02/C04；PROPOSAL = C03/C05/C06/C07/C10。
- Required Lab: NONE；Experiments: 0；Runtime observation: ABSENT。
- Gate ceiling: 后续只能把 PARTIAL/PROPOSAL 写成模型或设计，不得升级为实证结论。
