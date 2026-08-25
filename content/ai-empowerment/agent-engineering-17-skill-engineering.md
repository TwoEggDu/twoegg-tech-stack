---
title: "Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt"
slug: "agent-engineering-17-skill-engineering"
date: "2026-08-25T00:00:00+08:00"
description: "说明如何将可复用的领域方法做成可发现、按需加载、独立验证并受控执行的 Skill。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Skill Engineering"
  - "Prompt Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 180
weight: 3180
---

> **上一篇**：[Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite]({{< relref "ai-empowerment/agent-engineering-16-knowledge-base-rag.md" >}})

# Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt

> 如果这篇只记一句话：`Skill Engineering 不是给 Prompt 换目录，而是让领域方法可发现、按需装载、独立验证、受控执行，并在不适用时退出。`

一个团队开始用 Agent 处理构建问题时，最自然的做法通常是继续加 Prompt：遇到 Jenkins 日志，就补一段 Jenkins 规则；遇到 Unity 编译错误，再补一段 Unity 排查清单；遇到 YooAsset 制品问题，又把 manifest、缓存和远端核验经验一起塞进去。

文本越来越完整，系统却未必越来越工程化。团队仍然很难回答：这些方法对哪些任务生效？谁决定本次加载哪一份？版本怎样追踪？脚本能访问什么？两份方法冲突时采用哪份？某套方法已经过期时如何禁用和退役？

把这段长文本从 system prompt 移到一个名为 `SKILL.md` 的文件，也没有自动回答这些问题。许多实现最终仍会把被选中的完整 `SKILL.md` 指令注入模型 Context。Skill 并不是脱离 Prompt 的神秘新原语；真正发生的变化，是一套领域方法获得了可发现身份、适用范围、配套资源和独立生命周期。

本文会用一个连续的 BuildPilot 设计场景落地这些边界：有人报告“Jenkins 上的 Unity Android 构建在 YooAsset 更新后失败”。这只是 **DESIGN / NOT IMPLEMENTED / NOT RUN** 的教学场景，不是真实事故；没有读取任何 Job、日志、源码、制品或远端状态，也没有根因、运行结果或生产结论。

## 长 Prompt 为什么没有自然长成 Skill

[Prompt Engineering]({{< relref "ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md" >}})解决的是当次任务怎样表达：Goal、Constraints、Inputs、Examples、Output Requirements 与 Failure Semantics。它可以要求“只定位第一处失败”“证据不足返回 UNKNOWN”，却不负责让一套方法可被发现、独立版本化或按需退役。

因此，常驻长 Prompt 的问题不只是“占了多少 token”。本篇没有做 token 实验，也不会给出节省数字。更基础的问题是它缺少治理接缝：

- 方法和本次任务纠缠在一起，scope 与 near-miss 难以单独审查；
- 所有领域规则默认常驻，调用方无法解释本次为什么需要它；
- 参考资料、脚本与模板缺少明确的来源、依赖和版本边界；
- 修改、回滚、禁用与退役只能依附整段 Prompt 一起进行；
- 是否误触发、漏触发，以及加载后是否真的改善结果，都没有独立测试面。

这是一项工程推断，而不是本文测出的效果：把方法组织成可发现、可选择、可验证的包，能够暴露这些治理问题；它不保证更省、更快或更准。

在 BuildPilot 设计场景中，报告里同时出现 Jenkins、Unity、Android 与 YooAsset，并不意味着应该把所有构建知识一次性加载。正确的第一步也不是根据“YooAsset 更新后”猜根因，而是先问：当前任务需要哪一类方法，以及现有证据允许走到哪一层。场景状态始终是 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 先给 Skill 一个窄定义

本课程把 Skill 定义为：

> **可发现、在相关任务出现时按需加载、可携带说明、脚本、参考资料与模板的领域方法包。**

这个定义强调“方法包”，而不是“新能力”或“新执行主体”。它还刻意把治理责任留给 Host：谁能发现、怎样选择、何时加载、允许调用什么、上下文怎样收尾，都不能由一份方法文本自行宣布。

开放的 Agent Skills 格式提供了一个可观察的最小共同面：一个目录至少包含 `SKILL.md`，frontmatter 必填 `name` 与 `description`，并可携带 `scripts/`、`references/`、`assets/` 等资源；其披露思路是先暴露 metadata，激活后读取完整 instructions，再按需读取资源。这些是开放格式事实，不代表所有名为 Skill 的产品都完全同构。[Agent Skills Specification，访问于 2026-08-24](https://agentskills.io/specification)

这里必须说清一个常被标题掩盖的事实：**按需加载不等于指令永远不进入 Context。**在许多实现中，Host 先用 `name + description` 形成 catalog；选中之后，完整 `SKILL.md` 仍会成为 Context contributor。区别不是“Prompt 消失了”，而是全文不再默认无条件常驻，并且包的发现、范围、资源和生命周期可以被单独治理。

## 八个对象怎样分账

Skill 最容易长歪的原因，是它看起来什么都能写。方法、工具说明、流程、历史经验、权限要求都可以放进 Markdown；物理上能放在一起，不等于责任上应该混成一个对象。

下面是本文采用的课程审查模型。它不是行业统一 taxonomy，也不要求部署八个服务。

| 对象 | 它拥有的责任 | Skill 不会因此拥有 |
|---|---|---|
| Prompt | 当次 Goal、Constraints、I/O 要求与失败语义 | discovery、版本、真实权限 |
| Skill | 可复用的领域方法与配套资源 | 外部能力、权威事实、完成证明 |
| Tool Runtime | Validate、Policy、Execute、Result 与 Trace | Skill 只能指导，不能代替执行 gate |
| Workflow | State、Guard、合法路径与 Terminal | Skill checklist 不能提交权威状态 |
| Agent / Subagent | 执行主体、身份、工具、Context 与生命周期 | Skill 不是独立主体 |
| Memory | 跨 Step / Session 的保存、召回和更新 | Skill 不保存当前调查状态，也不把旧记录变成当前事实 |
| KB / RAG | 动态候选的 Retrieve、Filter、Rerank、Inject、Cite | 包内 reference 不替代检索与核验 |
| Harness / Policy | Context 装配、预算、权限、恢复与治理 | Skill 声明不能替代 enforcement |

这张表复用前文已经建立的边界：[Tool Runtime]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}})负责执行，[Agent Loop]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}})负责反馈推进，[Workflow]({{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}})守住合法状态边，[Context Engineering]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})审查每个 Step 看见什么，[Memory]({{< relref "ai-empowerment/agent-engineering-15-session-long-term-project-memory.md" >}})治理历史复用，[KB / RAG]({{< relref "ai-empowerment/agent-engineering-16-knowledge-base-rag.md" >}})只把检索结果当候选。

同一个进程可以承载多项职责，但审查问题不能消失。在 BuildPilot 设计场景中，排查顺序属于 Skill；读取 exact Jenkins build 属于 Tool Runtime；何时转向编译或资源链属于 Workflow；凭据、网络和文件范围属于 Policy；历史事故只能从 KB / Memory 成为待核验候选。全部仍为 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## Package Anatomy：格式最小面与生产合同不是一回事

开放格式的三层披露可以写成：

```text
catalog metadata
  -> selected SKILL.md instructions
  -> required scripts / references / assets
```

`name` 与 `description` 让 Host 能建立 catalog；完整 body 承载每次激活都需要的方法；体积较大、只在特定变体下需要的参考资料和脚本可以延迟读取。这样做减少的是“所有完整方法默认一起加载”的必要性，并不消除 catalog、激活全文、多 Skill 重叠和资源读取的成本。

产品 surface 也不统一。以下公开资料按各条所示访问日核对；未单列日期者访问于 2026-08-24：

- OpenAI Codex 文档描述了显式 `$` / `/skills` 与基于描述的隐式匹配，并按 repo、user 等 scope 发现 Skill；同名项不会被合并。该页面与官方仓库 `main` 都是会变化的当前资料，不应外推到所有 OpenAI surface。[OpenAI Codex Skills](https://developers.openai.com/codex/skills)
- Anthropic 当前官方文档需要按 surface 和 endpoint 分账。Messages / container 的 Skills Guide 使用 custom Skill ID `skill_...`，把调用时的 `version` selector 写成 `skver_...` 或 `latest`；它还在该 Guide 的更新流程里说明每个新版本是完整 snapshot，必须重传完整文件集，省略文件不会继承。该 Guide 的高层 prerequisites 只列 API key 与 Code Execution，示例不展示 Skills beta header。与此同时，Skill Version 管理 API 的 Get / List / Create reference 把 path / response `version` 定义为 Unix epoch timestamp，另返回 `skillver_...` 形态的对象 `id`，cURL 示例则显式携带 `anthropic-beta: skills-2025-10-02`。当前页面没有解释 invocation selector、management `version` 与 response object `id` 的稳定映射；Guide 缺少 header 也不能证明 raw management API 不需要它。因此这些移动事实必须在使用前按具体 endpoint 复核，不能把三个字段压成同一个“版本 ID”。Managed Agents 仍是独立 beta surface，其 `managed-agents-2026-04-01` header 只属于自身 API contract。[Anthropic Agent Skills API Guide](https://platform.claude.com/docs/en/build-with-claude/skills-guide)、[Get Skill Version API](https://platform.claude.com/docs/en/api/beta/skills/versions/retrieve)、[Managed Agents Overview](https://platform.claude.com/docs/en/managed-agents/overview)
- GitHub Copilot CLI / cloud agent 描述了按任务使用 Skill，而 Copilot SDK 的 custom-agent Skills 会 eager preload；SDK subagent 不继承 parent Skills。这说明即使同一产品家族，不同 surface 的加载时机与继承也可能不同。[GitHub Copilot Agent Skills](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)、[Copilot SDK Skills](https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/skills)

所以，progressive disclosure 是一个共享设计模式，不是所有实现都 lazy load 的保证。开放格式也没有统一规定 registry、collision precedence、权限、rollback、unload 或 context-end。

生产环境还需要一份额外的 review contract。下面这些字段是 **COURSE DESIGN / NOT OPEN-SPEC FIELDS**：

| 合同面 | 最小问题 |
|---|---|
| Scope | 应处理什么？哪些 near-miss 明确不处理？ |
| Input | 输入来自哪里、对应哪个 revision、是否足够新？ |
| Output | 产生什么 artifact，locator 在哪里？ |
| Dependency | 依赖哪些 Tool、package、network 或 credential？ |
| Permission | 需要什么 access / approval？由谁真正 enforcement？ |
| Procedure | 方法步骤与允许的分支是什么？ |
| Verification | 哪个 validator 能检查哪一层结果？ |
| Failure / Stop | 缺凭据、证据冲突、输入不完整时如何停止？ |
| Lifecycle | owner、source、version、review、rollback 与 retirement 是什么？ |

`description` 应写清 what、when 与 near-miss，帮助匹配；它不是 typed I/O schema，也不授予权限。包内写着“只读”不能撤销写权限，写着“必须成功”也不能改变 Runtime 结果。

## 一次 Skill 使用的六段生命周期

如果只记录“Skill 被加载了”，调查仍然无法回答它为什么被选、调用了什么、怎样结束。本文采用一条 owner-aware 的 **COURSE DESIGN** 链路：

```text
Discover -> Select -> Load -> Execute -> Verify -> Context Disposition
```

### Discover：发现只产生候选

Host 从允许的 roots、上传对象或注册表构建 catalog。此时最多知道候选 ID、name、description、来源和版本；catalog 中出现不等于本次已经选中，更不等于获得执行资格。

### Select：显式与隐式都需要理由

用户可以显式点名，Host / 模型也可能根据 description 隐式匹配。具体算法、冲突与 precedence 由产品决定。选择记录至少要保留 candidate、selected、trigger mode 与 reason；无法决定时应该 ask、reject 或保持 UNKNOWN，而不是伪造统一规则。

### Load：Skill 成为 Context contributor

Host 读取完整 `SKILL.md`，再按当前分支读取必需资源。此时方法会与 Goal、State、Evidence 和其他 instructions 一起竞争 Context；“已经加载”仍不等于“已经执行”。

### Execute：能力和状态仍由别的层拥有

Skill 可以要求读取日志、检查 manifest 或生成报告，但真正的 Tool call 仍需经过 Validate、Policy、Execute、Result 与 Trace。流程走哪条合法路径、何时终止，则由 Workflow / Runtime 决定。

### Verify：验证器只证明自己的检查面

Static validator 可以确认 frontmatter、文件路径或必填段落；领域 validator 可以确认输出 artifact 是否具备约定字段。它们不会自动证明诊断正确、来源当前适用、权限合规或任务成功。

### Context Disposition：结束后由 Host 收尾

Host 决定保留、压缩、释放或在下一 Step 重建哪些内容。开放格式没有统一 unload / context-end 合同，所以本步骤只能作为课程设计，不能写成所有产品都会“卸载 Skill”。

一份最小 lifecycle receipt 可以记录：

```text
run / step
catalog digest
candidate IDs + versions
selected + reason + trigger mode
loaded resources + digests
tool / workflow refs
permission verdict
artifact refs
terminal
context disposition
```

它能帮助回查一次选择与执行链，不能单独证明最终 Claim 已被接受。

## BuildPilot：先选方法，再让证据决定路径

> 本节全部内容均为 **DESIGN / NOT IMPLEMENTED / NOT RUN**。Experiment Count = 0，Observed Result = ABSENT。没有真实 Jenkins、Unity、YooAsset、制品、设备、CDN 或发布观测。

设计场景仍是同一句报告：“Jenkins 上的 Unity Android 构建在 YooAsset 更新后失败。”时间上的“更新后”不是因果证据，报告中的多个名词也不是同时加载全部方法的理由。

第一步只选择 `jenkins-build-triage` 这一候选。它指导系统取得 exact job / build identity，定位 first failing stage，保存 log locator 与 artifact inventory；它明确不修改 Job、不重跑构建。随后 Tool Runtime 才能在真实授权边界内读取 exact build。若凭据缺失、日志截断、build identity 漂移，或只有截图而没有可追溯 locator，设计终态是停止并保留缺口，不是猜测。

只有 first-stage Evidence 建立后，Workflow 才在两个窄方向之间选择：

- 若首个可行动失败属于 compiler / player error，候选是 `unity-compile-diagnosis`；
- 若证据指向 manifest、package、cache、download 或 remote delivery 链，候选是 `yooasset-artifact-chain-audit`。

这里是“compile **或** resource path”，不是两个都跑。最后，调查 handoff 才使用 `release-evidence-pack` 组织 Claim、locator、缺口与 decision log；它不执行发布，也不把 proposal、request object 或文件位置升级为 observed verification。

四个候选的设计合同如下，恰好只有这四个：

| Candidate | Classification | Trigger / exclusion | Designed artifact | Fail-closed boundary |
|---|---|---|---|---|
| `jenkins-build-triage` | DESIGN / NOT IMPLEMENTED / NOT RUN | job / build / stage / log；不改 Job、不重跑 | first failing stage、log locator、artifact inventory | credential missing、log truncated、identity drift、screenshot-only |
| `unity-compile-diagnosis` | DESIGN / NOT IMPLEMENTED / NOT RUN | compiler / player error；不处理一般 CDN 问题 | Unity version、target、first error、source packet | stale log、generated pollution、missing revision |
| `yooasset-artifact-chain-audit` | DESIGN / NOT IMPLEMENTED / NOT RUN | manifest / package / cache / download / remote；不处理纯 compile error | source / config / artifact / runtime / request / cache / remote matrix | request 冒充 bytes、build success 冒充 usable package、无 remote readback |
| `release-evidence-pack` | DESIGN / NOT IMPLEMENTED / NOT RUN | investigation handoff；不执行发布 | Claim mapping、gaps、decision log | locator 冒充 verification、proposal 冒充 observed、越权发布 |

整条设计时序是：

```text
task
  -> select Jenkins read-only method
  -> Tool Runtime reads exact build
  -> verify first failing stage
  -> select compile OR resource method
  -> produce bounded artifact + explicit UNKNOWN
  -> Workflow derives terminal
  -> Harness records receipt
```

方法属于 Skill；读取和外部动作属于 Tool Runtime；guard 与 terminal 属于 Workflow；credential、approval 与 sandbox 属于 Policy；catalog、Context 与 Trace 治理属于 Host / Harness；动态历史资料属于 KB / Memory；artifact 能否成为被接受的 Evidence 留给下游合同。这个分账也是 **COURSE DESIGN**，不说明 BuildPilot 已有上述 Runtime 或能力。

## False Trigger、Missed Trigger 与 Context 成本

按需加载的价值，要和误触发、漏触发一起讨论。

| 实际相关？ | 被选择？ | 结果 |
|---|---|---|
| 是 | 是 | 应进一步检查方法与输出，而不是直接宣布有效 |
| 否 | 是 | false trigger：引入无关方法、资源与潜在冲突 |
| 是 | 否 | missed trigger：任务缺少预期领域方法 |
| 否 | 否 | 正确排除，但仍需验证 near-miss 覆盖 |

官方 Agent Skills authoring guidance 建议分别准备 should-trigger positives 与 near-miss should-not-trigger negatives，并把 trigger eval 和 output eval 分开；with / without 或 old / new 的行为比较还应保留固定案例、断言、原始输出、Trace、timing / token receipt 与 human review。这是方法指南，不是本文已经执行的实验。[Optimizing Skill Descriptions](https://agentskills.io/skill-creation/optimizing-descriptions)、[Evaluating Skills](https://agentskills.io/skill-creation/evaluating-skills)

BuildPilot 四候选的 **DESIGN / NOT RUN** trigger 边界也应这样测：compile first error 不应触发 YooAsset 链审计；纯 CDN / remote 问题不应触发 Unity compile diagnosis；只有 investigation handoff 才考虑 evidence pack。本文没有运行这些 query，因此 trigger precision、recall、token cost 与质量影响全部 UNKNOWN。

Context pollution 也不能被写成“已经消除”。catalog metadata 仍占空间，激活后的全文仍会参与 Context，多份相互重叠的方法仍可能冲突，某些 surface 还会 eager preload。更稳的 package 设计是：把每次激活都必需的内容留在 `SKILL.md`，把平台变体和长参考延迟读取，并为每次选择留下 candidate、reason、version 与 resources receipt。它降低默认全量加载的必要性，不提供任何收益保证。

## Trust、权限与 Sandbox：Skill 文本没有执行权

Skill 能携带脚本和外部资源，因此它也是供应链与指令信任边界。评审至少要问：来源和 owner 是谁？版本或 digest 是什么？所有资源是否一起审过？依赖、网络、credential 和副作用有哪些？validator 能检查什么？失败后怎样 disable 或 rollback？

产品文档也给出了范围有限但一致的提醒。OpenAI ChatGPT 要求上传外部 Skill 前审查来源，并说明产品扫描不能替代组织政策与人工判断；Anthropic Managed Agents 把 repository Skill 明确视为 trust boundary；GitHub 警告预批准 shell / bash 会移除确认并放大 attacker-controlled instructions 风险。结论只限这些 2026-08-24 核对的 surface，不构成统一安全认证。[OpenAI：Skills in ChatGPT](https://help.openai.com/en/articles/20001066)、[Anthropic Managed Agents Skills](https://platform.claude.com/docs/en/managed-agents/skills)、[GitHub Copilot Agent Skills](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)

下面几个等号尤其危险：

```text
scan passed        != safe
allowed-tools      != cross-product / unlimited authority
sandboxed          != principal is permitted
version pinned     != method is correct
validator passed   != diagnosis verified
```

Skill 可以声明 permission need；在支持 `allowed-tools` 的 Host 上，该字段可被消费为预授权或 policy input，例如 GitHub Copilot 会让列出的工具免于逐次确认。但它不是跨产品通用、无限或脱离 Host / Policy / Runtime 的授权；真正的 approval、credential scope、least privilege 与 sandbox enforcement 仍按具体 surface 的 Host / Policy / Runtime 合同执行。对 BuildPilot 四候选的设计，默认都是 read-only；是否允许访问 Jenkins、Unity workspace、artifact storage 或 remote endpoint，仍要由 Runtime 在每次调用时判定。`release-evidence-pack` 只组装 handoff，不发布、不重跑、不修改 Job。以上仍为 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 通用 Agent + Skill，还是专用 Agent？

判断标准不在于“领域知识多不多”，而在于变化轴是否已经超出方法包。

当同一执行主体只是偶尔需要一套可复用方法、参考或模板，而且不需要独立的 model、system instructions、tool set、credential、permission、Context isolation、lifecycle 与 delegated owner 时，通用 Agent + Skill 通常更贴近问题：主体不变，按任务装入方法。

如果任务需要独立身份、隔离凭据、不同工具授权、单独 Context、独立生命周期或明确的 delegated ownership，就应该把专用 Agent / Subagent 作为另一层设计，而不是指望 Skill 文件承担主体职责。

多 Agent 环境中尤其不能假定 Skill 自动继承。截至 2026-08-24，Anthropic Managed Agents 为每个 Agent 配置独立 skills，context、tools 与 MCP 也不共享；GitHub Copilot SDK 则明确说明 subagent 不继承 parent skills，需要显式列出。它们是两项产品范围事实，不是所有平台的共同合同。[Anthropic Multi-agent Orchestration](https://platform.claude.com/docs/en/managed-agents/multiagent-orchestration)、[GitHub Copilot SDK Custom Agents](https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/custom-agents)

所以 dispatch 前更稳的课程设计，是解析并记录 effective Skill set：目标 Agent 最终看见了哪些候选、版本和资源，哪些来自自己的配置，哪些没有继承。跨产品的共享、隔离、冲突与 precedence 若没有当前合同，保持 UNKNOWN。

本文的 BuildPilot 场景故意保持单一通用诊断 Agent + 四个候选方法包，不设计 subagent topology。它只说明怎样切方法边界，不能证明这种形态普遍更优，更不能说明 BuildPilot 已有 multi-agent Runtime。场景仍为 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 从一次编写走向完整生命周期

Skill 不是写完 `SKILL.md` 就完成。一个可审查的生命周期可以写成下面这条 **COURSE DESIGN**：

```text
Source
  -> Review
  -> Static Validate
  -> Trigger Eval
  -> With/Without or Old/New Eval
  -> Pin / Deploy
  -> Observe
  -> Roll Back / Disable
  -> Deprecate / Retire
```

每个 Gate 的证据上限不同：

| Gate | 最多证明什么 | 不能据此宣称什么 |
|---|---|---|
| Source / Review | 来源、owner、资源与变更已被审查 | 内容安全、方法正确 |
| Static Validate | 格式与机械合同满足 | trigger 正确、任务有效 |
| Trigger Eval | 冻结正负例下的选择行为 | 真实分布上的 precision / recall |
| Behavior Eval | 固定 workload、版本和 rubric 下的对照结果 | 跨模型、跨环境普遍收益 |
| Pin / Deploy | 本次引用身份被固定并进入目标 surface | 质量、可靠性或授权正确 |
| Observe | 保存了 activation、Trace 与 artifact | 已解释因果或完成验收 |
| Rollback / Disable | 能回到 last-known-good 或停止资格 | 所有影响已消失 |
| Retire | 生命周期和迁移责任已处理 | 历史引用已物理删除 |

具体产品有不同管理面。Anthropic 的 Messages / container Guide 使用 custom `skill_...` ID 与 `skver_...` / `latest` invocation selector，并只在该 Guide 的更新流程内给出完整 snapshot 语义；Skill Version 管理 API reference 却以 Unix epoch timestamp 表达 path / response `version`，把 `skillver_...` 作为另一个 response object `id`，其 cURL 还携带 `skills-2025-10-02` beta header。Guide 的高层 prerequisites / examples 未展示该 header，不能据此否定 management endpoint 的 header；当前跨页映射属于 moving-source limitation，生产 pin 必须按具体 endpoint 复核。Managed Agents 的 beta header 仍只属于独立 surface。Codex 本地 Skill 可以借助 Git 与 config disable，plugins 可承担分发；ChatGPT workspace 提供 owner、access、invocations、timestamps 与 delete。它们都不能被概括成一个开放格式自带的统一 registry 或 rollback 协议。

BuildPilot 四候选的生命周期目前只有设计要求：owner review、static check、positive / near-miss trigger set、bounded fixture、permission review、version / digest、disable、last-known-good 与 deprecation owner。它们都没有创建、部署、触发或回归执行，仍为 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 什么时候不应该创建 Skill

创建 Skill 之前，可以用下面这份 fail-closed 清单。它是工程判断工具，不是行业准入标准，也不保证收益。

1. 这套方法是否会重复使用，而且只在部分任务需要？
2. 能否写清 should-trigger 正例与 near-miss 反例？
3. 输入、来源、新鲜度、输出、失败和停止语义能否被审查？
4. 需要的真是“方法”，而不是新能力、流程控制、独立主体、动态事实或授权？
5. 能否把每次必需内容与变体资源分开，支持 progressive disclosure？
6. 若带脚本，dependency、permission、sandbox、side effect 与 validator 是否明确？
7. owner、source、version、review、rollback、disable 与 retirement 是否可落盘？
8. 是否准备了 baseline、fixture、assertions、raw output、Trace 与 human review？

多项回答为“否”，先不要建 Skill。按问题本体分流：

| 真实需要 | 更合适的落点 |
|---|---|
| 一次性简单请求 | Prompt |
| 所有任务都必须遵守的全局规范 | stable instructions / Harness policy |
| 新的外部读写能力 | Tool / MCP + Tool Runtime |
| 确定性步骤、审批或发布权 | Workflow / Policy |
| 独立身份、权限或 Context isolation | Agent / Subagent |
| 动态且需要检索、更新、引用的事实 | KB / RAG / Memory |
| 无法写 procedure 与 failure semantics 的泛泛建议 | 先澄清方法，不急于封包 |

重复出现并不自动等于值得封装；Skill 数量也不代表系统成熟。对 BuildPilot 而言，一个覆盖“Jenkins + Unity + YooAsset + 发布”的万能包会把 scope、权限和失败语义重新混在一起。本文只保留前述四个窄候选，它们也尚未通过任何实测清单，状态仍是 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 验证边界：事实、设计与未知必须并列保存

截至 2026-08-24，本文能够安全留下三类结论。

**FACT（有 scope）**：开放 Agent Skills 格式提供 `SKILL.md`、必填 `name / description`、可选资源与 metadata -> instructions -> resources 的 progressive disclosure；OpenAI、Anthropic、GitHub 在已核对 surface 上展示了不同 roots、activation、version、context timing 与 multi-agent 行为。产品事实不能扩成行业统一实现。

**INFERENCE / COURSE DESIGN**：Skill 的工程价值来自可发现身份、scope、resources 与 lifecycle；生产合同、六段生命周期、receipt、create-or-not checklist，以及 BuildPilot 四候选都是课程设计。它们用于暴露 owner 与验证接缝，不是开放规范字段或已实现系统。

**UNKNOWN**：统一 precedence、collision、merge、unload、context-end、跨 Agent inheritance，以及 Skill 对 accuracy、quality、token、latency、cost、trigger precision / recall、success rate、adoption 和 production benefit 的影响，均没有被本篇证明。

本文 `EXPERIMENT COUNT = 0`，`OBSERVED RESULT = ABSENT`。没有 Lab、fixture、provider run、BuildPilot Runtime、benchmark 或真实事故证据；因此也没有正面或负面的效果结论。

后续 Article 18 将接住 artifact 怎样成为可审计 Evidence，Article 19 将展开 Permission、Approval 与 Sandbox，Article 22 才讨论 Eval 与 Regression。Harness 的横切控制、DeepSeek Harness 的源码映射，以及 BuildPilot capability 的正式设计也都留在各自后文；本文不提前给出实现。

## Learning Check

1. 把一段长领域说明移入 `SKILL.md` 后，为什么还不能说已经完成 Skill Engineering？它与 Prompt 为什么仍有交集？
2. Progressive disclosure 减少的是什么默认加载，catalog、激活全文、资源与重叠方法还会留下哪些成本？
3. Production Skill Contract、Tool Runtime permission enforcement 与 Workflow state ownership 应怎样分账？
4. 通用 Agent + Skill 何时应该升级成专用 Agent？为什么不能假定 subagent 继承 parent Skills？
5. Static validator、trigger eval、behavior eval、version pin 与 rollback 分别最多证明什么？
6. BuildPilot 为什么只保留四个窄候选？`release-evidence-pack` 为什么不能执行发布，也不能把 locator 变成 Verification？
7. 当 Experiment Count = 0、Observed Result = ABSENT 时，可以写哪些设计判断，哪些效果结论必须保持 UNKNOWN？

## 最短结论

`Skill 仍会通过 Context 影响模型；Skill Engineering 的关键不是让 Prompt 消失，而是让领域方法能够被发现、选择、验证、治理，并把执行权与事实判断留给正确的工程层。`
