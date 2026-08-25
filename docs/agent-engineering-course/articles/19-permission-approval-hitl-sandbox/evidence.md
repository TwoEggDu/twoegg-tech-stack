# Article 19 Evidence｜Permission、Approval、Human-in-the-loop 与 Sandbox

## Evidence metadata

- Status: COMPLETE
- Claim coverage: 10 / 10
- Evidence Cards: 12
- Core BLOCKED Claims: 0
- Required Lab: NONE
- Experiments: 0
- Runtime observation: ABSENT
- BuildPilot: DESIGN / NOT IMPLEMENTED / NOT RUN
- Access date for moving sources: 2026-08-25 (Asia/Shanghai)

## Claim Register

| Claim | Question | Status | Evidence | Wording ceiling |
|---|---:|---|---|---|
| 19-C01 | 1 | CONFIRMED | 19-E01, E02 | Evidence acceptance 与 action authority 独立；不产生真实权限 |
| 19-C02 | 2 | PARTIAL | 19-E02, E03, E09, E10 | 五分法仅称课程 working model |
| 19-C03 | 3 | PROPOSAL | 19-E02, E03, E11 | 最小链为设计，不称标准原文模型 |
| 19-C04 | 4 | PARTIAL | 19-E02, E03, E04 | 标准支持分离；具体阶段为课程设计 |
| 19-C05 | 5 | PROPOSAL | 19-E04, E11 | 风险路由不称普适最优，未知 fail closed |
| 19-C06 | 6 | PROPOSAL | 19-E04, E05, E08 | 字段为最小审计设计，不称产品 schema |
| 19-C07 | 7 | PROPOSAL | 19-E03, E05, E07, E10 | 状态机为设计；产品示例不外推 |
| 19-C08 | 8 | CONFIRMED | 19-E03, E04, E05, E06 | 降低暴露面；不保证零风险或即时撤销 |
| 19-C09 | 9 | CONFIRMED | 19-E09 | 只确认 Linux namespaces/network namespace/seccomp；filesystem/secret broker 是未取证课程示例 |
| 19-C10 | 10 | PROPOSAL | 19-E11, E12 | BuildPilot 仅 DESIGN / NOT IMPLEMENTED / NOT RUN |

## Evidence Cards

### Evidence 19-E01｜Evidence acceptance is not authority

- Source: Published Article 18, repository snapshot 2026-08-25；NIST SP 800-162 (2014, updated 2019), §§2.1–2.3.
- Locator: `content/ai-empowerment/agent-engineering-18-evidence-contract.md`；https://csrc.nist.gov/pubs/sp/800/162/upd2/final
- Retrieval boundary: 课程文档证明本课程 Evidence contract；NIST 固定出版物证明 access decision 有独立 subject/object/operation/environment/policy 输入。
- Supported Claims: 19-C01.
- Evidence type: course contract + primary standard.
- Proves: claim acceptance 和 access authorization 是不同判定对象。
- Does Not Prove: 任一 accepted Claim 已获得 credential、approval 或执行权。
- Limitations: NIST 不讨论本课程 Evidence Card。
- Falsifier: 若课程 canonical contract 明确规定 accepted Evidence 自动签发相应 action authority，则本 Claim 需推翻。
- Course usage: 文章开篇的硬边界。

### Evidence 19-E02｜ABAC request, policy decision and enforcement

- Source: NIST SP 800-162 (2014, updated 2019), §§2.1–2.3 and Figure 2.
- Locator: https://csrc.nist.gov/pubs/sp/800/162/upd2/final
- Retrieval boundary: 固定出版物；术语在 ABAC 范围内。
- Supported Claims: 19-C02, 19-C03, 19-C04.
- Evidence type: primary standard publication.
- Proves: authorization 可依 subject、object、requested operation、environment 与 policy 判定，并由 decision/enforcement components 承担不同责任。
- Does Not Prove: Permission/Approval/HITL/Sandbox 五分法是 NIST taxonomy。
- Limitations: 未规定 agent approval state machine。
- Falsifier: 若原文不包含这些 request attributes 或 decision/enforcement separation，则删除对应标准归因。
- Course usage: authority chain 的事实底座。

### Evidence 19-E03｜Dynamic, per-session authorization and revocation

- Source: NIST SP 800-207 (2020), §§2.1, 3.2, 3.3.
- Locator: https://csrc.nist.gov/pubs/sp/800/207/final
- Retrieval boundary: zero-trust architecture 语境。
- Supported Claims: 19-C02, 19-C03, 19-C04, 19-C07, 19-C08.
- Evidence type: primary standard publication.
- Proves: access 可按 session 动态评估；Policy Engine/Administrator/Enforcement Point 分别决定、建立/终止并执行访问，决定可 grant/deny/revoke。
- Does Not Prove: 所有 agent tool 都必须采用 NIST ZTA 部署拓扑。
- Limitations: session 不是任意副作用动作的事务边界。
- Falsifier: 若引用章节不支持动态判定或 grant/deny/revoke 分工，则缩窄相关 Claim。
- Course usage: provisioning、decision、enforcement 与 revalidation 分离。

### Evidence 19-E04｜Least privilege, access enforcement and approval controls

- Source: NIST SP 800-53 Rev. 5.1.1 OSCAL catalog, official `usnistgov/oscal-content` release/tag `v1.2.0`；catalog metadata version=`5.1.1+u2`.
- Locator: tag-pinned catalog `https://raw.githubusercontent.com/usnistgov/oscal-content/v1.2.0/nist.gov/SP800-53/rev5/json/NIST_SP-800-53_rev5_catalog.json`，OSCAL control IDs `ac-2`, `ac-3`, `ac-3.2`, `ac-3.8`, `ac-6`, `ac-24`；official release identity `https://github.com/usnistgov/oscal-content/releases/tag/v1.2.0`；publication entry `https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final`.
- Retrieval boundary: release notes state alignment with the NIST SP 800-53 v5.1.1 CPRT release；annotated tag `686109e7516295dce79e6db806e721492586da74` resolves to commit `1763607deb4ffb3d67c59dc669a7c6404a6f93a6`；2026-08-26 readback returned `200`, bytes=`10192497`, SHA-256=`ADC257A7A9019BED1EE11E108E62D74B531BA9C33EA89F1B9AD49FD9149ACA23`；generic CSRC page is only the publication entry and does not replace the pinned catalog.
- Supported Claims: 19-C04, 19-C05, 19-C06, 19-C08.
- Evidence type: primary controls catalog.
- Proves: pinned artifact 中 `ac-2` 处理账户、创建审批与 access authorization；`ac-3` 执行 approved authorization；`ac-3.2` 要求 dual authorization；`ac-3.8` 执行授权撤销并承认生效时点可不同；`ac-6` 要求 least privilege；`ac-24` 把 access-control decision 与 enforcement 区分并要求 decision 先于 enforcement。这些是可分离控制关注点。
- Does Not Prove: 本文 R0–R3 分类或 ApprovalRecord 字段是 NIST 规定。
- Limitations: control catalog 需要组织按系统语境裁剪。
- Falsifier: 若指定 control locator 在钉住版本中不存在或含义不同，则移除对应映射。
- Course usage: 风险审批与最小权限设计的上界依据。

### Evidence 19-E05｜Scope, expiry and revocation

- Source: RFC 6749 (2012), §§3.3, 4.2.2；RFC 7009 (2013), §§2–3.
- Locator: https://www.rfc-editor.org/rfc/rfc6749.html ; https://www.rfc-editor.org/rfc/rfc7009.html
- Retrieval boundary: OAuth 2.0 token protocol 语境。
- Supported Claims: 19-C06, 19-C07, 19-C08.
- Evidence type: primary Internet standards.
- Proves: delegated credentials can carry scope and expiry；revocation exists and distributed propagation may leave an implementation-dependent window.
- Does Not Prove: OAuth token 等同 approval，或撤销在所有组件中瞬时完成。
- Limitations: approval request digest 与 resource revalidation 是课程追加设计。
- Falsifier: 若协议不支持所引 scope/expiry/revocation semantics，则删除类比。
- Course usage: scope/expiry/revocation 字段和传播窗口说明。

### Evidence 19-E06｜TOCTOU

- Source: MITRE CWE-367, CWE 4.20, accessed 2026-08-25.
- Locator: https://cwe.mitre.org/data/definitions/367.html
- Retrieval boundary: moving taxonomy page；仅使用 check/use race 定义与 mitigation guidance。
- Supported Claims: 19-C08.
- Evidence type: primary weakness catalog.
- Proves: check 与 use 之间状态变化可使已检查条件失效；缩短窗口/重检或合并边界是缓解方向。
- Does Not Prove: revalidation 能消灭所有竞态。
- Limitations: taxonomy 不是完整并发控制规范。
- Falsifier: 若页面当前定义不再描述 check/use 间资源变化，则重做来源。
- Course usage: resume 与 use-time 必须重验证。

### Evidence 19-E07｜Decision idempotency is not action exactly-once

- Source: RFC 9110 (2022), §9.2.2；Published Articles 06 and 11, repository snapshot 2026-08-25.
- Locator: https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2；`content/ai-empowerment/agent-engineering-06-tool-runtime.md`；`content/ai-empowerment/agent-engineering-11-long-running-agent.md`.
- Retrieval boundary: RFC 仅限定 HTTP method retry/idempotency；课程文档限定 checkpoint 与副作用 ownership。
- Supported Claims: 19-C07.
- Evidence type: primary standard + course contracts.
- Proves: 重复同意/拒绝可设计为同一 decision outcome；Article 06 的 Tool Runtime execution/result boundary 与 Article 11 的 long-running side-effect/idempotency/revalidation seam 表明，动作重试安全仍取决于动作自身的执行与副作用协议。
- Does Not Prove: approval 幂等可带来 exactly-once external effect。
- Limitations: RFC 不定义 approval lifecycle。
- Falsifier: 若动作协议能原子绑定 decision 与 effect 并有可验证 exactly-once 语义，可为该特定动作提高上限。
- Course usage: 防止把两类幂等混写。

### Evidence 19-E08｜Audit record content

- Source: NIST SP 800-53 Rev. 5.1.1 OSCAL catalog, official `usnistgov/oscal-content` release/tag `v1.2.0`；catalog metadata version=`5.1.1+u2`.
- Locator: tag-pinned catalog `https://raw.githubusercontent.com/usnistgov/oscal-content/v1.2.0/nist.gov/SP800-53/rev5/json/NIST_SP-800-53_rev5_catalog.json`，OSCAL control ID `au-3`；official release identity `https://github.com/usnistgov/oscal-content/releases/tag/v1.2.0`；publication entry `https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final`.
- Retrieval boundary: release notes state alignment with the NIST SP 800-53 v5.1.1 CPRT release；annotated tag `686109e7516295dce79e6db806e721492586da74` resolves to commit `1763607deb4ffb3d67c59dc669a7c6404a6f93a6`；2026-08-26 readback returned `200`, bytes=`10192497`, SHA-256=`ADC257A7A9019BED1EE11E108E62D74B531BA9C33EA89F1B9AD49FD9149ACA23`；generic CSRC page is not the version-fixed locator.
- Supported Claims: 19-C06.
- Evidence type: primary controls catalog.
- Proves: pinned `au-3` exists and requires audit records to establish event type、time、location、source、outcome and associated individual/subject/object identity；authority decisions therefore need enough bound context to audit.
- Does Not Prove: 本文完整 ApprovalRequest/DecisionRecord schema 是 NIST 原文。
- Limitations: request digest、policy version 等为针对 agent action 的设计补充。
- Falsifier: 若 AU-3 的钉住文本不再包含这些 audit content elements，则缩窄 Claim。
- Course usage: record 最小字段的审计动机。

### Evidence 19-E09｜Sandbox mechanisms and explicit limits

- Source: Linux kernel `seccomp_filter` documentation, accessed 2026-08-25；Linux man-pages 6.18 `namespaces(7)` and `network_namespaces(7)`.
- Locator: https://www.kernel.org/doc/html/latest/userspace-api/seccomp_filter.html ; https://man7.org/linux/man-pages/man7/namespaces.7.html ; https://man7.org/linux/man-pages/man7/network_namespaces.7.html
- Retrieval boundary: current kernel docs + versioned man-pages；仅限这些 Linux mechanisms。
- Supported Claims: 19-C02, 19-C09.
- Evidence type: official mechanism documentation.
- Proves: namespaces isolate named resource views；network namespace separates network resources；seccomp limits syscall exposure and the kernel explicitly says filtering is not a sandbox and needs complementary hardening/LSM for logical behavior/information flow.
- Does Not Prove: namespace/seccomp 组合自动构成完整、安全、无逃逸 sandbox；也不授权业务动作。
- Limitations: 未覆盖所有 OS、container runtime、filesystem、secret broker configurations。
- Falsifier: 若 current kernel documentation removes or reverses the explicit limitation, re-check and version-pin before reuse.
- Course usage: confirmed matrix usage 只能覆盖 Linux namespaces、network namespace 与 seccomp；filesystem view/mount/allow-write 和 secret broker/mount/environment 必须明确标为 `COURSE DESIGN EXAMPLE / UNCONFIRMED IN THIS ARTICLE`。

### Evidence 19-E10｜Current product-scoped HITL example

- Source: OpenAI Agents SDK Python, “Human-in-the-loop” guide, accessed 2026-08-25.
- Locator: https://openai.github.io/openai-agents-python/human_in_the_loop/
- Retrieval boundary: moving product documentation；未运行 SDK，版本/部署行为未实测。
- Supported Claims: 19-C02, 19-C07.
- Evidence type: official product documentation.
- Proves: current docs describe per-tool-call interruption, approve/reject, serializable RunState and resume as one concrete product interaction.
- Does Not Prove: 通用 HITL protocol、跨版本兼容、exactly-once、revalidation 或安全保证。
- Limitations: 文档行为可能随 SDK version 漂移。
- Falsifier: 若 current guide no longer documents those transitions, remove the example rather than generalizing from history.
- Course usage: 只作受限例子，不作原理证据。

### Evidence 19-E11｜Risk routing and BuildPilot envelope

- Source: 19-C03/C05/C06/C07/C10 design synthesis constrained by 19-E02–E10.
- Locator: this Research/Evidence package, 2026-08-25.
- Retrieval boundary: course proposal；no implementation or runtime observation.
- Supported Claims: 19-C03, 19-C05, 19-C06, 19-C07, 19-C10.
- Evidence type: DESIGN synthesis.
- Proves: the proposed fields/state transitions are internally traceable to stated constraints.
- Does Not Prove: feasibility、correctness、performance、user benefit or security in a running BuildPilot.
- Limitations: DESIGN / NOT IMPLEMENTED / NOT RUN；experiment 0；runtime ABSENT。
- Falsifier: an internal contradiction, missing fail-closed transition, or future implementation observation violating the model requires revision.
- Course usage: explicit proposal surface only。

### Evidence 19-E12｜Neighboring article ownership

- Source: Published Articles 06, 10, 11, 18 and Article 19 card/README, repository snapshot 2026-08-25.
- Locator: `content/ai-empowerment/agent-engineering-06-tool-runtime.md`；`content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`；`content/ai-empowerment/agent-engineering-11-long-running-agent.md`；`content/ai-empowerment/agent-engineering-18-evidence-contract.md`；Article 19 workspace files.
- Retrieval boundary: course-local canonical snapshot.
- Supported Claims: 19-C10.
- Evidence type: repository primary documents.
- Proves: Article 06 owns Tool Runtime and its tool execution/result boundary；Article 10 owns explicit State Machine/Workflow state and transitions；Article 11 owns long-running checkpoint/resume、side-effect/idempotency and the revalidation seam；Article 18 owns Evidence acceptance；Article 19 owns action authority/HITL/sandbox；Article 20 budget and Article 21 trace/replay/failure taxonomy remain out of scope.
- Does Not Prove: future article content or implementation behavior。
- Limitations: repository ownership can change through later authorized edits。
- Falsifier: a canonical series/workorder revision that reassigns these topics requires remapping。
- Course usage: prevent Article 19 from pre-completing Articles 20/21。

## Evidence Gate

- Decision: **RECOMMEND PASS**
- Coverage: 10 / 10 approved questions；12 Evidence Cards；Core BLOCKED Claims: 0。
- Wording discipline: CONFIRMED/PARTIAL/PROPOSAL ceilings are explicit；`19-C09` confirmed scope is limited to Linux namespaces/network namespace/seccomp，with filesystem/secret-broker items unconfirmed course examples；no product example is generalized；no sandbox/security/benefit guarantee is claimed。
- Required Lab path: N/A；Required Lab NONE；Experiments 0；Runtime observation ABSENT。
- Gate owner after Research result: MASTER_ORCHESTRATOR。
- Hard stop retained: any later core behavioral Claim that lacks primary evidence must be removed, narrowed to PROPOSAL, or marked BLOCKED before publication。
