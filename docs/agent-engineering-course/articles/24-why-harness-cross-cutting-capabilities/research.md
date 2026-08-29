# Article 24 Research｜为什么最终需要 Harness：横切能力由谁承载

## Research Metadata

- Article: `24`
- Gate: `RESEARCH`
- Status: `COMPLETE`
- Research date: `2026-08-29`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`
- Evidence Gate recommendation: `PASS`
- Claim status count: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

## Scope Boundary

Article 24 是一篇 `PRINCIPLE` 型文章。它只证明一个窄结论：

> 当 Permission、Evidence、Budget、Trace、Approval、Context、Recovery、Knowledge、Capability Discovery 同时出现在多个 Prompt、Tool 和业务 Workflow 里时，它们就不再只是局部实现细节，而是需要被一致承载的横切工程不变量。本课程把这个承载边界称为 Harness。

本篇可以使用公开系统作为证据：MCP 的 Prompt/Resource/Tool 拆分与 tool security guidance、OpenAI Agents SDK 的 HITL/Guardrails/Tracing、Microsoft Agent Framework 的 approval middleware、GitHub review gate / CODEOWNERS、OpenTelemetry observability primitives、NIST AI RMF governance、Unity BuildReport / AssetDatabase / Addressables Analyze、requirements traceability、ADR、KCS。

必须保留三个上限：

1. `Harness` 是本课程术语，不是行业统一标准名。
2. `BuildPilot` 只是课程设计案例：`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`。
3. Article 25 才正式展开 Runtime/Harness 边界；Article 26 才正式展开 Capability 最小模型；Article 27 才讨论成本、采纳路径和何时不该做 Harness。

## Answered Research Questions

| RQ ID | Question | Answer | Status | Evidence |
|---|---|---|---|---|
| 24-RQ01 | 什么是本篇的横切能力？ | 横切能力是那些需要跨多个 Agent、Tool、Workflow 保持一致语义的控制面：identity、permission、evidence、budget、trace、approval、context、recovery、knowledge、capability discovery。它们之所以横切，是因为单个局部任务无法独占其完整语义。 | `PROPOSAL` | 24-E01, 24-E04, 24-E05, 24-E07 |
| 24-RQ02 | 为什么这些能力不自然属于一个 Prompt？ | 公开系统把 instruction、tool、resource、guardrail、trace、approval 分成不同机制。Prompt 可以描述规则，但不能单独持久化审批状态、约束工具面、统一 trace identity、决定 evidence acceptance 或进行 owner routing。 | `PARTIAL` | 24-E01, 24-E03, 24-E04 |
| 24-RQ03 | 为什么这些能力不自然属于一个 Tool？ | MCP 文档把 tool discovery、schema、invocation、annotation、security、human confirmation、validation、logging、error/result 语义拆开。发现工具和调用工具不等于当前请求有权限，也不等于输出已经满足 Evidence Contract。 | `CONFIRMED` | 24-E02 |
| 24-RQ04 | 为什么这些能力不自然属于一个业务 Workflow？ | Workflow/process 更自然地表达业务步骤。横切治理需要跨多个业务序列复用。NIST governance、Azure operations、Azure microservices guidance 都支持治理、可观测性、安全和公共操作能力会跨局部流程出现。 | `PARTIAL` | 24-E05 |
| 24-RQ05 | 什么证据支持“重复实现会漂移”？ | Azure operations 明确提醒日志格式不一致会导致难以甚至无法检索有用信息；Azure microservices guidance 提醒把安全和公共任务散落在服务中会增加维护复杂度并产生重复易错代码。这支持一般工程判断，但不是 agent Harness 的专门统计结论。 | `PARTIAL` | 24-E05 |
| 24-RQ06 | 为什么 Harness 不是更长的 System Prompt？ | HITL approval、guardrail placement、trace、authorization、status check、CODEOWNERS、BuildReport、asset dependency data 都需要可执行状态、外部接口或持久记录。Prompt 能写下规则，但不能独自实现 pause/resume、stale review invalidation、audit trail 或 objective build evidence。 | `PARTIAL` | 24-E02, 24-E03, 24-E08, 24-E10 |
| 24-RQ07 | 为什么 Harness 不是 God Object？ | Harness 只承载共享控制语义，不接管业务意图。业务 Agent/Workflow 仍负责 domain planning、requirement interpretation 和 owner decision；Harness 负责在它们之间保持 identity、permission、evidence、budget、trace、approval、context、recovery、knowledge、capability discovery 的一致性。 | `PROPOSAL` | 24-E06, 24-E07 |
| 24-RQ08 | 为什么 BuildPilot 的 Unity 场景需要 Harness？ | 完整场景需要把 Requirement Contract candidate、缺失/歧义条件、C#/配置表/资产规则/构建证据、owner routing、review、re-verification、Intent Ledger、Knowledge Store、future rule/test/gate candidate 连成一条可追踪链。这些责任跨分析、证据、审批和复用，不能干净地塞进单个 Prompt 或 Tool。 | `PROPOSAL` | 24-E08, 24-E09, 24-E10, 24-E11 |
| 24-RQ09 | “suggestion-first + Human Review” 为什么仍然需要 Harness？ | suggestion-first 降低直接写入风险，但仍然需要一致回答：读了什么、证据够不够、谁有权审批、审批是否因 diff 改变而过期、哪些未知被保留、哪些经验进入知识库。Human Review 是 gate，不是所有治理语义的存储位置。 | `PARTIAL` | 24-E03, 24-E08, 24-E09, 24-E11 |

## Source Notes

### Course Dependency Sources

| Source ID | Source | Article 24 use | Boundary |
|---|---|---|---|
| S-COURSE-18 | Published Article 18: Evidence Contract | 复用 Claim / Evidence / Observation / Inference / Proposal / Unknown 分层；Evidence acceptance 不等于 trace、eval 或 permission。 | 不重讲 Evidence Contract。 |
| S-COURSE-19 | Published Article 19: Permission / Approval / HITL / Sandbox | 复用 permission ceiling、authorization、approval、HITL pause/resume、sandbox execution surface。 | 不重讲权限模型。 |
| S-COURSE-20 | Published Article 20: Budget | 复用 budget 作为 admission/stopping contract，而不是 usage report。 | 不展开 budget taxonomy。 |
| S-COURSE-21 | Published Article 21: Trace / Replay / Failure Taxonomy | 复用 trace identity、causality、side-effect boundary、unknown preservation。 | 不重讲 trace/replay。 |
| S-COURSE-22 | Published Article 22: Eval / Golden Dataset / Regression | 复用 eval/golden/regression 边界；trace candidate 不是自动 golden case。 | 不定义 Harness eval model。 |
| S-GLOSSARY | `docs/agent-engineering-course/glossary.md` | 确认 Harness 是本课程对 Runtime 周围复用控制/约束的称呼；Runtime 在 Article 25 正式定义；Capability 在 Article 26 正式定义。 | Harness 保持课程术语。 |
| S-SERIES | `docs/agent-engineering-series-plan.md` | 确认 Article 24 是 Part V 首篇 Harness 文章，Article 25/26/27 分别承接后续模型层。 | 防止本篇吞掉后文。 |

### External Sources

所有外部来源访问日期：`2026-08-29`。

| Source ID | Source | Observed support | Use in Article 24 |
|---|---|---|---|
| S-MCP-OVERVIEW | MCP Specification 2025-06-18, Server Features Overview: `https://modelcontextprotocol.io/specification/2025-06-18/server/index` | Server features 分为 Prompts、Resources、Tools，并标注不同 control hierarchy。 | 证明 instruction、context/data、executable function 是不同局部原语。 |
| S-MCP-TOOLS | MCP Specification 2025-06-18, Tools: `https://modelcontextprotocol.io/specification/2025-06-18/server/tools` | Tool list/call、schema、annotations、security、human confirmation、validation、timeout/rate limit、audit logging 是不同责任。 | 证明 capability discovery/call 不等于 permission/trust/evidence acceptance。 |
| S-MCP-AUTH | MCP Authorization draft/current page: `https://modelcontextprotocol.io/specification/draft/basic/authorization` | Authorization 是 optional、transport/resource-server scoped；scope challenge 对当前 operation 有效；auth server 实现部分 out of scope。 | 支持 authorization 是独立层，同时不是完整 Harness。 |
| S-OAI-HITL | OpenAI Agents SDK Python, Human-in-the-loop: `https://openai.github.io/openai-agents-python/human_in_the_loop/` | HITL 会暂停 execution，等待 approve/reject，支持 RunState serialize/resume，argument inspection fail-closed，approval 跨 handoff/nested tool。 | 支持 approval 是可执行控制，不只是 prompt 文本。 |
| S-OAI-GUARDRAILS | OpenAI Agents SDK JS, Guardrails: `https://openai.github.io/openai-agents-js/guides/guardrails/` | Input/output/tool guardrails 有 placement 差异；agent-level guardrails 不一定覆盖 workflow 中每个 agent；blocking guardrail 可暂停模型进展。 | 支持控制语义需要 placement 和执行时机。 |
| S-OAI-TRACING | OpenAI Agents SDK JS, Tracing: `https://openai.github.io/openai-agents-js/guides/tracing/` | Built-in tracing 记录 LLM generation、tool call、handoff、guardrail、custom events，并有 browser/test/ZDR 限制。 | 支持 trace 是 runtime/product surface；不等于 evidence acceptance。 |
| S-MS-TOOL-APPROVAL | Microsoft Agent Framework Tool Approval: `https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval` | Approval 包装 function；framework intercepts tool request 并经 middleware 等待审批；另有产品/示例名 `Harness Agent`。 | 支持 middleware-based approval；也提醒不要把课程 Harness 说成行业统一定义。 |
| S-MS-PROCESS | Microsoft Semantic Kernel Process Framework: `https://learn.microsoft.com/en-us/semantic-kernel/frameworks/process/process-framework` | Process Framework 表达 business process 的 activities/tasks、event transitions，并可与 OpenTelemetry 审计关联；页面标 experimental。 | 支持 business workflow/process 与 shared control plane 的区分。 |
| S-GH-PROTECTED | GitHub Protected Branches: `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches` | 可要求 PR review、diff 改变后 dismiss stale approval、expected status checks、conversation resolution。 | 支持 Change Request / review gate / stale revalidation。 |
| S-GH-CODEOWNERS | GitHub CODEOWNERS: `https://docs.github.com/en/enterprise-server@3.20/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners` | CODEOWNERS 做 path-to-owner routing、auto review request、write access requirement；invalid lines 会影响 owner assignment。 | 支持 owner routing 以及配置本身也需要治理。 |
| S-OTEL | OpenTelemetry Specification 1.60.0: `https://opentelemetry.io/docs/specs/otel/` | 标准化 traces/metrics/logs/resource/context/conformance 等 observability primitives。 | 支持 common tracing/observability primitives，但不证明 agent evidence acceptance。 |
| S-NIST-RMF | NIST AI RMF 1.0 Core: `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/` | Govern 是 cross-cutting；risk management 是 continuous/iterative；documentation 支持 human review/accountability；measurement 需要持续测试和记录 uncertainty。 | 支持 cross-cutting governance 与 measurement。 |
| S-AZURE-OPS | Microsoft Azure Architecture, Design for Operations: `https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/design-for-operations` | Operations 包括 deployment、monitoring、escalation、incident response、security auditing；强调 standardized logging/tracing/metrics；日志格式不一致会导致检索困难甚至不可能。 | 支持重复实现横切策略会漂移的工程判断。 |
| S-AZURE-MICROSERVICES | Microsoft Azure Architecture, Microservices: `https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices` | API gateway、externalized configuration、security offload 可让 service focused；重复公共任务会 repetitive/error-prone。 | 支持 Harness 非 God Object 的类比：共享控制不等于接管业务。 |
| S-UNITY-BUILDREPORT | Unity 2022.3 BuildReport API: `https://docs.unity.cn/2022.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html` | BuildReport 暴露 build summary、files、packed assets、scenes、steps、stripping/platform info。 | 支持 BuildPilot read-only build evidence surface。 |
| S-UNITY-ASSETDB | Unity Asset Database manual: `https://docs.unity.cn/Manual/AssetDatabase.html` | AssetDatabase 将 source files 转为 Library imported representation，跟踪 source/import settings/target platform dependencies 与 GUID/hash metadata。 | 支持 read-only asset/import dependency evidence surface。 |
| S-UNITY-ADDRESSABLES | Unity Addressables Analyze window: `https://docs.unity.cn/Packages/com.unity.addressables%402.9/manual/analyze-addressables-window-reference.html` | Analyze 可检查 group layout、duplicate bundle dependencies、explicit/implicit assets，并区分 fixable/unfixable checks。 | 支持 asset/bundle 证据类别；不授权 auto-fix。 |
| S-ISO-29148 | ISO/IEC/IEEE 29148 public online browsing platform: `https://www.iso.org/obp/ui?_escaped_fragment_=iso:std:iso-iec-ieee:29148:ed-2:v1:en` | Requirement 是精确/无歧义、含 constraints/conditions 的 need statement；requirements management 维护、沟通、trace、track；verification/validation 使用 objective evidence。 | 支持 Requirement Contract 与 traceability 语言。 |
| S-MADR | MADR ADR template: `https://github.com/adr/madr/blob/develop/template/adr-template.md` | ADR 记录 status/date/decision makers/context/problem/drivers/options/outcome/consequences/confirmation。 | 支持 Intent Ledger 作为 rationale record pattern；不是正式标准。 |
| S-KCS | KCS Practices Guide: `https://library.serviceinnovation.org/KCS/Knowledge-Centered_Success_Practices_Guide` | KCS 强调在 workflow 中 reuse/improve/create knowledge，支持 operational efficiency、automation 和 structured up-to-date knowledge。 | 支持 Knowledge Store / reuse loop。 |

## Research Model

### 1. Local primitives vs shared control plane

| Local surface | Naturally owns | Leaks when the project grows |
|---|---|---|
| Prompt | 任务框定、角色、局部 instruction、偏好、分解提示。 | 不能单独 enforce authorization、persist approvals、normalize evidence、join traces、replay failure、route owner review。 |
| Tool | 具体 callable capability、schema、input/output、execution result、local error。 | 不自然拥有“谁可以用”“结果满足什么 evidence standard”“是否预算允许”“如何进入未来知识”。 |
| Business Workflow | domain sequence 与业务决策路径。 | 不自然拥有跨 workflow 复用的 permission、approval、evidence、trace、budget、recovery、context packaging、capability discovery。 |
| Runtime | execution loop：model calls、tool dispatch、state/continuation、stop conditions。 | 完整边界属于 Article 25；本篇只说明 runtime execution 不等于治理语义。 |
| Harness（课程提案） | 围绕 Runtime 和业务 workflow 的 shared execution/governance boundary。 | 必须避免 God Object：business planning 和 domain ownership 仍留在业务 Agent/Workflow。 |

### 2. Minimal Article 24 Definition

本篇只使用一个轻量定义：

> Harness 是承载横切控制与记录的共享边界；这些控制与记录必须在多个 Agent、Tool、Workflow 执行时保持一致。

这比 Article 25/26 的后续模型更弱，只用于回答“为什么需要这样一个边界”。

### 3. Cross-cutting Capability Test

一个 concern 满足以下三项以上时，应从局部 Prompt/Tool/Workflow 逻辑上移到 Harness：

1. 多个 agent/tool/workflow 需要同一规则。
2. 不同实现会导致 failure semantics 不一致。
3. 结果需要在 run 后审计。
4. 后续步骤依赖前序 approval/evidence/budget/trace state。
5. 需要 owner routing 或外部 review。
6. 需要跨 pause/resume/retry/replay 保存语义。
7. 会改变模型可发现或可调用的 capability 集合。
8. 创建组织责任，而不仅是局部计算。

这是课程提案，不是外部标准。

## Claim Register

| Claim ID | Claim | Status | Evidence Card | Article use |
|---|---|---|---|---|
| 24-C01 | 公开 agent/protocol/workflow 系统会把 instruction、resources、tools、processes、guardrails、tracing、approvals 拆成不同 primitives/layers；未找到来源证明本课程 Harness 是行业统一标准名。 | `CONFIRMED` | 24-E01 | 开篇呈现 scattered responsibility landscape。 |
| 24-C02 | Tool discovery、schema 或 invocation 不等于 permission、trust 或 evidence acceptance。 | `CONFIRMED` | 24-E02 | 反驳“注册工具就够了”。 |
| 24-C03 | Approval、HITL、sandbox-like interruption、guardrail behavior 是带 placement/state 的可执行控制，不能简化成更长的 prompt。 | `PARTIAL` | 24-E03 | 支撑 “Harness is not prompt engineering”。 |
| 24-C04 | Trace、Evidence、Budget、Eval 在课程中是相关但独立的控制面；trace 可以支持 evidence，但不自动决定 acceptance、budget 或 regression quality。 | `PARTIAL` | 24-E04 | 从 Part IV 接到 Part V。 |
| 24-C05 | 把横切治理逻辑复制到每个 agent/workflow 内，容易造成 policy、failure semantics、auditability、recovery 漂移。 | `PARTIAL` | 24-E05 | 本篇核心压力。 |
| 24-C06 | 课程 Harness 应作为围绕 agents/tools/workflows 的 shared control plane 引入，而不是拥有全部业务决策的 God Object。 | `PROPOSAL` | 24-E06 | 定边界，避免吞掉业务 workflow。 |
| 24-C07 | Article 24 的初步 Harness 职责集可以包括 identity、permission、context、evidence、budget、trace、approval、recovery、knowledge、capability discovery；完整 Runtime split 留给 Article 25，Capability model 留给 Article 26。 | `PROPOSAL` | 24-E07 | 本篇可用的安全定义。 |
| 24-C08 | Owner routing、review gates、stale review invalidation、expected status checks、conversation resolution 是必须超出单个局部 suggestion 的治理例子。 | `PARTIAL` | 24-E08 | 支撑 BuildPilot 的 Change Request / Human Review。 |
| 24-C09 | Requirement Contract、Intent record、postmortem-style learning、knowledge-centered workflow 支持从歧义需求到 evidence-backed finding 再到 reusable rule/test/gate candidate 的提议链路。 | `PARTIAL` | 24-E09 | 让 BuildPilot 场景有工程来源但不冒充实现。 |
| 24-C10 | Unity public APIs/docs 暴露 build report、asset/import metadata、dependency analysis、Addressables layout checks 等读取面；这支持 BuildPilot read-only evidence categories 的可行性。 | `PARTIAL` | 24-E10 | 具体化 Unity 场景。 |
| 24-C11 | 完整 BuildPilot requirement-change scenario 是课程设计提案：read-only diagnosis、suggestion-first Change Request、owner implementation、re-verification、Intent Ledger、Knowledge Store、rule/test/gate candidate。 | `PROPOSAL` | 24-E11 | 主案例，必须保留提案标签。 |
| 24-C12 | Article 24 没有 required lab、experiment count 为 0、runtime observation absent，且本仓没有 BuildPilot implemented/run evidence。 | `CONFIRMED` | 24-E12 | 防止发布稿误报运行证据。 |

## BuildPilot Scenario Research Assignment

Article 24 的 BuildPilot 段落应写成 design case，并显式标注证据等级。

### Scenario setup

> Unity 团队修改需求：“这个功能现在要在低内存移动场景中工作，并且不能让包体回退。”BuildPilot 接收需求文本，生成 Requirement Contract candidate。它不修改 production code、config、art import settings、Addressables groups、`.meta` files、policy 或 capability registry。

### Required chain

1. BuildPilot 提取 Requirement Contract candidate：
   - target platforms；
   - performance/build-size constraints；
   - affected scenes/assets/config tables；
   - expected validation signals；
   - unknowns and ambiguous clauses。
2. 条件缺失或冲突时，输出：
   - `AMBIGUOUS_REQUIREMENT`；
   - `CONTRADICTORY_REQUIREMENT`；
   - `MISSING_PREREQUISITE`。
3. 执行 read-only evidence collection：
   - C# reference scan；
   - cross-table config relationship scan；
   - asset/import/dependency rule scan；
   - BuildReport or equivalent build evidence if available；
   - Addressables layout/analyze evidence if available。
4. 归类 findings：
   - `CONFIRMED`；
   - `VIOLATION`；
   - `INSUFFICIENT_EVIDENCE`；
   - `PERMISSION_BLOCKED`；
   - `TOOL_GAP`；
   - `INTENT_DRIFT`。
5. 提出带证据链接和 owner routing 的 Change Request。
6. Human owner review 后，由 owner 在 BuildPilot 外完成真实修改。
7. BuildPilot 在 owner implementation 后 re-verify。
8. 结果进入 Intent Ledger 与 Knowledge Store。
9. 如果模式重复出现，提出 future rule/test/gate candidate。

### What this scenario proves

它证明 suggestion-first assistant 仍然需要 shared governance：系统必须一致回答谁提出需求、读过哪些文件、证据是否足够、谁可以批准、review 是否过期、哪些未知被保留、哪些结论进入未来知识。

### What this scenario does not prove

它不证明 BuildPilot 已存在、已修改 Unity 项目、已运行工具、已创建 PR、已拥有 capability registry、已有稳定 schema、或能够产出 production-ready automated fixes。

## Counter-evidence and Wording Ceilings

| Risk | Evidence / reason | Required wording |
|---|---|---|
| `Harness` 听起来像行业标准组件。 | Microsoft 文档有产品/示例名 `Harness Agent`；本课程 glossary 使用自己的 Harness 术语；未发现外部标准定义本课程模型。 | “本课程把这个边界称为 Harness……” |
| 过度解释 MCP。 | MCP 提供 Prompts/Resources/Tools 和 auth/security guidance，但不是完整 Harness。 | “MCP 证明局部能力与安全建议分层存在；它不解决全部治理问题。” |
| 过度解释 OpenAI/Microsoft SDK。 | HITL/guardrail/tracing/approval examples 都是 product/framework scoped。 | “这些例子说明控制必须在某处执行，不等于要求采用本课程架构。” |
| 暗示所有团队都必须做 Harness。 | Article 27 才处理成本与不采纳条件。 | “Article 24 只解释压力来源；Article 27 讨论什么时候不该做。” |
| 暗示集中所有业务逻辑。 | API gateway/security offload 类比支持共享 concern 与业务服务分离。 | “Harness 承载不变量；业务 Agent/Workflow 保留领域意图。” |
| Unity 场景听起来已实现。 | 当前没有 BuildPilot code/run evidence。 | 每个 BuildPilot 关键结论都用 `design case`、`proposal` 或等价措辞。 |

## Recommended Article Spine

1. 从痛点进入：同一套“安全 AI 工程师”规则散落在 Prompt、Tool wrapper、Workflow、CI、review checklist、logs 和团队约定中。
2. 展示漂移：permission 说一套，evidence 说另一套，trace identity 对不上，budget 停得太晚，approval stale，recovery 不能 replay。
3. 定义 cross-cutting capability：跨局部 surface 保持一致的不变量。
4. 解释局部归属失败：
   - Prompt 可以要求行为，但不能执行持久治理。
   - Tool 可以执行能力，但不决定 policy/trust/evidence。
   - Workflow 可以编排业务步骤，但不拥有所有共享控制。
5. 引入 Harness：shared carrying boundary。
6. 立刻澄清边界：
   - 不是更长的 System Prompt；
   - 不是 God Object；
   - 不是行业统一名称；
   - 还不是 Article 25 的完整 Runtime/Harness split。
7. 走完整 BuildPilot requirement-change design case。
8. 收束到 Article 25：既然知道共享边界为什么存在，下一篇回答它与 Runtime 的相对位置。

## Evidence Gate Recommendation

- Recommendation: `PASS`
- Next allowed gate: `EVIDENCE_GATE`
- Reason:
  - 所有核心 research questions 均有可用证据。
  - 公开来源足以支撑 principle-level 论证，只要将对应 claim 保持为 `CONFIRMED / PARTIAL / PROPOSAL`。
  - Article 24 没有 required lab。
  - 不需要也未声明 runtime observation。
  - BuildPilot scenario 可以安全写成 course proposal/design case。
  - 无 `BLOCKED` core claim。

## Notes for Downstream Workers

- 发布文章应为中文。
- 不要写“行业已经统一叫 Harness”或近似表达。
- 避免 API-first exposition。文章应遵循：problem pressure → abstract model → concrete Unity/BuildPilot design case。
- 不要完整定义 Article 25 的 Runtime boundary。
- 不要完整定义 Article 26 的 Capability model。
- 不要展开 Article 27 的成本/采纳/不采纳分析，只能预告。
- 不要声称 BuildPilot 改了文件、跑了工具、创建了 PR、查询了 Unity 或测量了 runtime behavior。
