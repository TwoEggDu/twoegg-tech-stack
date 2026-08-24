# Article 17 Evidence｜Skill Engineering

## Gate status

- Owner: RESEARCHER
- Status: COMPLETED
- Evidence Gate Recommendation: PASS
- Required Lab: NONE
- Experiment count: 0
- Observed result: ABSENT
- Access date for web sources: 2026-08-24；F02 targeted refresh: 2026-08-25（EC05 / EC06）
- Version boundary: current official web documentation plus unpinned `main` source where stated；发布前需刷新。

## Claim register

`Label` 只使用 `FACT / INFERENCE / DESIGN / UNKNOWN`；`Evidence Status` 遵守课程 `CONFIRMED / PARTIAL / PROPOSAL / BLOCKED`。

| Claim ID | Core | Label | Claim | Evidence Status | Evidence | Wording boundary |
|---|:---:|---|---|---|---|---|
| 17-C01 | YES | FACT | 开放 Agent Skills 的最小包是含 `SKILL.md` 的目录，必填 name / description，可带 scripts / references / assets。 | CONFIRMED | 17-EC01 | 只说开放格式；不外推所有“skill”产品同构。 |
| 17-C02 | YES | FACT | 开放格式采用 metadata -> instructions -> resources 的 progressive disclosure。 | CONFIRMED | 17-EC01、EC02 | 是共享模式，不保证每个 surface 同加载时机。 |
| 17-C03 | YES | INFERENCE | Skill 不是“完全脱离 Prompt 的新原语”，而是有 discovery / scope / resources / lifecycle 的按需方法包。 | PARTIAL | EC01—EC04 | 明说许多实现会把指令注入 Context；不做本体论断言。 |
| 17-C04 | YES | FACT | description 是主要匹配信号；可显式或隐式激活，具体 trigger algorithm 由 Host 决定。 | CONFIRMED | EC02—EC04、EC07 | 不宣称统一关键词算法或无误触发。 |
| 17-C05 | YES | INFERENCE | 渐进披露可降低默认装载量，但不会自动消除 Context 成本、冲突或 pollution。 | PARTIAL | EC01—EC03、EC08 | 只说机制与风险，不写 token / quality 收益。 |
| 17-C06 | NO | UNKNOWN | 没有本轮证据支持统一 instruction precedence、collision、unload / context-end 语义。 | PARTIAL | EC02—EC07 | 必须写“产品特定 / UNKNOWN”，不得补成统一规则。 |
| 17-C07 | YES | DESIGN | 生产 Skill 应显式写 scope、I/O、dependency、permission、failure、verification、owner 与 version。 | PROPOSAL | EC01、EC08、EC09 | 课程合同，不冒充规范字段。 |
| 17-C08 | YES | FACT | Skill 需要分别测试 trigger 与 output；output eval 应有 baseline、realistic cases、assertions、trace 与 human review。 | CONFIRMED | EC08 | 官方方法指南，不宣称已执行 Article 17 eval。 |
| 17-C09 | YES | FACT | Version / release / rollback 是产品机制；当前 Anthropic Agent Skills API 使用 custom `skill_...` ID、`skver_...` exact version 或 `latest`，新版本是完整 snapshot；Codex / ChatGPT 有不同管理面。 | CONFIRMED | EC03—EC05 | 当前 Skills guide 未展示或要求 `skills-2025-10-02` beta header；Managed Agents 的 beta 状态与 header 只属于其独立 surface；不宣称开放格式提供统一 registry / rollback。 |
| 17-C10 | YES | FACT | Skill / scripts 构成 trust boundary；扫描、预批准或上传检查都不能替代来源审查、least privilege 与 sandbox。 | CONFIRMED | EC04、EC06、EC07 | 安全事实保持 product-scoped。 |
| 17-C11 | YES | FACT | Multi-agent 中 Skill ownership / sharing / isolation 依实现；Anthropic、GitHub 都要求 per-agent 配置语义。 | CONFIRMED | EC06、EC07 | 不外推其他产品，不假定继承。 |
| 17-C12 | YES | DESIGN | Skill lifecycle receipt 应记录 candidate、selection、version、resources、permission、artifact、terminal 与 context disposition。 | PROPOSAL | EC02、EC05、EC08、EC11 | BuildPilot / course design，不是开放 schema。 |
| 17-C13 | YES | FACT | OpenAI、Anthropic、GitHub 对 roots、collision、context、version、multi-agent 行为存在可观察差异。 | CONFIRMED | EC03—EC07 | 只覆盖 2026-08-24 核对的 surfaces。 |
| 17-C14 | YES | DESIGN | BuildPilot 应拆 Jenkins、Unity compile、YooAsset artifact chain、evidence pack 四个窄 Skill，并把执行 / 权限 /状态留在其他层。 | PROPOSAL | EC09—EC11 | NOT IMPLEMENTED / NOT RUN；不得写生产效果。 |
| 17-C15 | NO | UNKNOWN | Article 17 Skill 对 accuracy、quality、token、latency、cost、trigger precision / recall、success rate 的影响未知。 | PARTIAL | EC08、EC12 | `EXPERIMENT COUNT=0`，任何数字或赢家结论禁止。 |

## Evidence cards

### 17-EC01｜Open format anatomy and progressive disclosure

- Source: Agent Skills Specification
- URL: https://agentskills.io/specification
- Source type / authority: official open-format specification maintained by Agent Skills project
- Version / date: current page accessed 2026-08-24；page 未暴露 release tag / pinned commit
- Claim supported: 17-C01、17-C02、17-C03、17-C05、17-C07
- What it proves: `SKILL.md`、name / description、optional fields、scripts / references / assets 的格式；完整 body 在激活后读取；三层 progressive disclosure；`allowed-tools` 为 experimental 且实现支持可能不同。
- What it does NOT prove: 不证明统一扫描路径、trigger algorithm、instruction precedence、permission enforcement、version registry、unload、runtime quality 或 adoption。
- Counter-evidence / alternative: GitHub Copilot SDK 对 custom-agent Skills eager preload；说明同一生态的 surface 也可偏离“选择后才加载全文”的普通路径。
- Limitations: current web spec 是 moving target；本轮未 pin repo SHA。
- Applicability: 用于开放格式共有层；产品行为必须再读对应官方文档。

### 17-EC02｜Client implementor lifecycle and its non-normative edges

- Source: How to add skills support to your agent
- URL: https://agentskills.io/client-implementation/adding-skills-support
- Source type / authority: official implementor guidance for Agent Skills-compatible clients；不是核心格式规范本身
- Version / date: current page accessed 2026-08-24
- Claim supported: 17-C02—C07、17-C12
- What it proves: guide 覆盖 discover、parse、catalog disclose、model / user activation、structured wrapping、permissions hook、context protection、dedup 与 optional subagent delegation；说明 model-driven selection 是多数实现路径而非必须算法。
- What it does NOT prove: guide 中 collision precedence、allowlisting、compaction protection 等建议不是所有产品已经实现的事实；不定义统一 context end。
- Counter-evidence / alternative: guide 称 project-over-user 为 existing implementations 的 universal convention；OpenAI Codex 当前文档却说同名 Skill 不 merge、两者都可出现在 selector。故文章不得复述“universal precedence”。
- Limitations: implementation guide 含推荐语态，应与 normative specification 分账。
- Applicability: 支持课程生命周期抽象与“Host owns activation / context management”。

### 17-EC03｜OpenAI Codex implementation

- Source: OpenAI Build skills / Codex documentation；OpenAI Codex `skill-creator` source
- URL: https://developers.openai.com/codex/skills ; https://github.com/openai/codex/blob/main/codex-rs/skills/src/assets/samples/skill-creator/SKILL.md
- Source type / authority: official OpenAI product documentation + official product repository
- Version / date: docs accessed 2026-08-24（redirected to ChatGPT Learn）；GitHub `main` moving target, unpinned
- Claim supported: 17-C02—C05、17-C09、17-C13
- What it proves: Codex 先列 name / description / path、选择后读全文；initial catalog 有 2% context / unknown-window 8000-char budget并可能缩短 / 省略；支持 explicit `$` / `/skills` 与 description implicit matching；扫描 repo/user/admin/system scopes；同名不 merge；可 config disable；plugins 用于分发。
- What it does NOT prove: 不证明所有 OpenAI surfaces 同目录 / 管理语义；不证明某 Skill 被正确触发；`skill-creator` 的 authoring policy 不是开放规范。
- Counter-evidence / alternative: Anthropic API 使用 uploaded skill IDs / versions；GitHub Copilot SDK 可 eager preload，说明 local filesystem scan 不是通用实现。
- Limitations: redirect 与 moving `main` 可能变化；发布前刷新。
- Applicability: 只用于 2026-08-24 的 ChatGPT / Codex 产品事实。

### 17-EC04｜OpenAI ChatGPT governance and trust

- Source: Skills in ChatGPT
- URL: https://help.openai.com/en/articles/20001066
- Source type / authority: official OpenAI Help Center product documentation
- Version / date: page shows updated 25 days before access；accessed 2026-08-24
- Claim supported: 17-C03、17-C04、17-C09、17-C10、17-C13
- What it proves: ChatGPT Skill 可含 instructions / examples / code，可自动使用一个或多个；外部 Skill 上传前应审来源；ChatGPT 会 scan，但 scan 不替代组织政策 / 人工判断；workspace 有 access、owner、invocations、created / updated、delete 与 compliance logs 管理面。
- What it does NOT prove: 不证明 scan 能检测所有恶意行为；不证明 Codex 受相同 admin policy；不提供通用签名 / provenance schema。
- Counter-evidence / alternative: 本地 Codex repo Skill 主要受文件系统 / Git 与本地 config 管理，不等同 ChatGPT workspace lifecycle。
- Limitations: eligibility / admin behavior 受 plan 与 workspace policy 影响。
- Applicability: ChatGPT product-scoped security / lifecycle evidence。

### 17-EC05｜Anthropic API versions, execution environment and observability

- Source: Using Agent Skills with the API
- URL: https://platform.claude.com/docs/en/build-with-claude/skills-guide
- Source type / authority: official Anthropic API documentation
- Version / date: current guide live-refreshed 2026-08-25；custom Skill IDs use `skill_...`；custom version selectors use `skver_...` or `latest`；the guide exposes no release tag / pinned revision
- Claim supported: 17-C03、17-C05、17-C09、17-C12、17-C13
- What it proves: API attaches Skill by type / ID / optional version；custom Skill IDs are `skill_...`；custom version selection uses `skver_...` or `latest`；each new version is a complete snapshot whose omitted files are not carried forward；current prerequisites/examples require API key and Code Execution tool and do not show or require `skills-2025-10-02`；Skills run via code execution container；Compliance Activity Feed can record create / delete when enabled。
- What it does NOT prove: 不证明 local Claude Code / Managed Agents 采用相同 lifecycle；audit feed 不补录未启用期间操作；sandbox 不等于主体授权；exact pin 不证明 skill quality。
- Counter-evidence / alternative: Codex local skills use file / Git lifecycle；GitHub CLI uses its own distribution commands。
- Limitations: current official guide is a moving target；本卡的 ID、version format、snapshot 与 prerequisite 结论只限所列 API surface 和 2026-08-25 live refresh；页面未展示 beta header 不证明未来永不需要；limits、retention 和 model examples 可变。
- Applicability: Anthropic API product fact and production pin example only。

### 17-EC06｜Anthropic Managed Agents trust and multi-agent isolation

- Source: Managed Agents overview；Managed Agents Skills；Multiagent orchestration
- URL: https://platform.claude.com/docs/en/managed-agents/overview ; https://platform.claude.com/docs/en/managed-agents/skills ; https://platform.claude.com/docs/en/managed-agents/multiagent-orchestration
- Source type / authority: official Anthropic Managed Agents documentation
- Version / date: overview refreshed 2026-08-25 and explicitly labels Managed Agents beta with `managed-agents-2026-04-01` header；Skills / multiagent pages accessed 2026-08-24
- Claim supported: 17-C04、17-C06、17-C10、17-C11、17-C13
- What it proves: repo `.claude/skills/<name>/SKILL.md` 在 session start 扫描；repository Skill 是 trust boundary，能 commit 的主体可改变 instructions；同名 repo / attached / mounted Skills 均可用并保留 path；每个 agent 有独立 model/system/tools/MCP/skills，contexts / tools / MCP 不共享。
- What it does NOT prove: 不证明 filesystem / vault 完全隔离（文档反而说明共享 sandbox、filesystem、vault credentials）；不证明其他 Anthropic surfaces 或其他 vendors 同语义。
- Counter-evidence / alternative: OpenAI Codex 同名 selector 行为、GitHub SDK inheritance 细节不同。
- Limitations: beta surface；self-hosted / cloud repository support 不同。
- Applicability: 只支撑 Anthropic Managed Agents 的 trust 与 per-agent ownership。

### 17-EC07｜GitHub Copilot Skill / Tool / Agent boundaries

- Source: Adding agent skills；Comparing Copilot CLI customization；Copilot SDK custom skills / agents
- URL: https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills ; https://docs.github.com/en/copilot/concepts/agents/copilot-cli/comparing-cli-features ; https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/skills ; https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/custom-agents
- Source type / authority: official GitHub product documentation
- Version / date: accessed 2026-08-24
- Claim supported: 17-C03—C06、17-C10、17-C11、17-C13
- What it proves: CLI Skill 用于 just-in-time task instructions；Tool 提供能力；custom agent 提供独立 specialization / tool permission / context；Skill 可 implicit / slash invoke；pre-approving shell / bash 会移除确认并带来 attacker-controlled instructions 风险；SDK per-agent Skills eager preload，subagent 不继承 parent skills。
- What it does NOT prove: 不证明 `allowed-tools` 跨客户端等价；不证明所有 Copilot surfaces 有统一 precedence / inheritance；不证明预批准脚本安全。
- Counter-evidence / alternative: 常规 Copilot CLI / cloud agent 是 description-based 按需，SDK custom-agent field 是 eager preload。
- Limitations: 多个 surface，必须在正文标明 CLI / cloud / SDK。
- Applicability: 支撑职责边界、权限风险与多 Agent 不可假定继承。

### 17-EC08｜Trigger and output evaluation

- Source: Optimizing skill descriptions；Evaluating skill output quality；Best practices for skill creators
- URL: https://agentskills.io/skill-creation/optimizing-descriptions ; https://agentskills.io/skill-creation/evaluating-skills ; https://agentskills.io/skill-creation/best-practices
- Source type / authority: official Agent Skills authoring guidance；非核心格式规范
- Version / date: current pages accessed 2026-08-24
- Claim supported: 17-C04、17-C05、17-C08、17-C12、17-C15
- What it proves: trigger eval 应含 should / should-not prompts 与 near-miss negatives；output eval 比较 with / without 或 old / new，使用 clean context、assertions、execution traces、timing / tokens 与 human review；validator / script 用于机械检查。
- What it does NOT prove: 页面示例 benchmark 数字只是示例，不是 BuildPilot / 本文实验结果；不证明所有模型、任务收益一致。
- Counter-evidence / alternative: 简单任务可能无需 Skill；若 without-skill 已稳定完成，Skill 可能没有增量价值。
- Limitations: guidance，不是已经执行的 Article 17 Lab；grader 也需审查。
- Applicability: 支撑 lifecycle 设计和禁止虚构收益。

### 17-EC09｜Course boundary with Prompt, Tool Runtime, Workflow, Context, Memory and KB

- Source: Published Agent Engineering Articles 02 / 06 / 08 / 10 / 12 / 13 / 15 / 16；course glossary
- URL: repository://content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md ; repository://content/ai-empowerment/agent-engineering-06-tool-runtime.md ; repository://content/ai-empowerment/agent-engineering-08-agent-loop.md ; repository://content/ai-empowerment/agent-engineering-10-state-machine-workflow.md ; repository://content/ai-empowerment/agent-engineering-12-context-engineering.md ; repository://content/ai-empowerment/agent-engineering-13-context-debugging.md ; repository://content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md ; repository://content/ai-empowerment/agent-engineering-16-knowledge-base-rag.md
- Source type / authority: repository canonical course continuity evidence；不是外部产品 authority
- Version / date: working tree read 2026-08-24；published Article 16 completion is previous durable boundary
- Claim supported: 17-C03、17-C07、17-C14
- What it proves: 本课程已冻结 Prompt 只表达任务、Tool Runtime 管执行 gate、Workflow 管确定性骨架、Context 管 Step view、Memory 管跨 Session、KB / RAG 管候选检索与引用；Article 17 必须复用这些边界。
- What it does NOT prove: 不证明开放 Agent Skills 采用同一 taxonomy；不证明 BuildPilot 已实现。
- Counter-evidence / alternative: 产品可能把多个职责放在同一物理组件；课程分账是审查模型，不要求微服务化。
- Limitations: internal course definitions must be labeled course model when not industry terms。
- Applicability: 防止 Article 17 重写前文或提前吞掉 Harness / Permission / Evidence Contract。

### 17-EC10｜Security synthesis and supply-chain boundary

- Source: EC01、EC04、EC06、EC07 primary sources
- URL: https://agentskills.io/specification ; https://help.openai.com/en/articles/20001066 ; https://platform.claude.com/docs/en/managed-agents/skills ; https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills
- Source type / authority: cross-vendor official source synthesis
- Version / date: accessed 2026-08-24
- Claim supported: 17-C07、17-C10、17-C12
- What it proves: Skill 可含 executable scripts / code；repository contributor 或外部 package 能改变 instructions；产品文档均要求 trust / review 或警告预批准执行风险。
- What it does NOT prove: 不存在本轮证据支持“scan 后安全”“sandbox 后已授权”“allowed-tools 是通用 enforcement”或完整软件供应链安全。
- Counter-evidence / alternative: ChatGPT upload scanning、Anthropic isolated container、GitHub confirmation 都降低部分风险，但各自不能替代 provenance、pin、review、least privilege 与 runtime policy。
- Limitations: 没有恶意 Skill fixture、签名验证或 SBOM 实验。
- Applicability: 只支持分层安全结论，不做绝对安全评级。

### 17-EC11｜BuildPilot Design v1 boundary and continuous case

- Source: canonical Article 17 Card；Course Factory BuildPilot boundary；repository project conventions
- URL: repository://docs/agent-engineering-course/articles/17-skill-engineering/article-card.md ; repository://docs/agent-engineering-course/course-factory.md ; repository://AGENTS.md
- Source type / authority: canonical repository contract for course scope
- Version / date: working tree read 2026-08-24
- Claim supported: 17-C12、17-C14
- What it proves: Article 17 要回答 BuildPilot boundary；BuildPilot 仅允许 Design v1，不得实现 / 宣称 production Runtime；项目要求区分 source/config/artifact/runtime/request/cache/remote evidence。
- What it does NOT prove: 不证明四个候选 Skill 存在、被触发或有效；不证明真实 Jenkins / Unity / YooAsset 生产事故。
- Counter-evidence / alternative: 若未来真实 repository evidence 显示已有不同模块边界，应以实现 / runtime evidence 更新 proposal，而不是维护本文虚构名称。
- Limitations: 全部 BuildPilot objects 为 authoring design input。
- Applicability: 贯穿案例必须逐段带 `DESIGN / NOT RUN` 标签。

### 17-EC12｜No experiment / no measured benefit

- Source: Article 17 workspace and repository diff inspection
- URL: repository://docs/agent-engineering-course/articles/17-skill-engineering/
- Source type / authority: current Article transaction artifact inventory
- Version / date: inspected 2026-08-24
- Claim supported: 17-C15
- What it proves: Required Lab=NONE；没有 lab、fixture、raw observation、provider run、BuildPilot runtime 或 benchmark artifact；`EXPERIMENT COUNT=0`。
- What it does NOT prove: 不证明 Skill 有效或无效；不支持任何正负性能结论。
- Counter-evidence / alternative: 官方 eval 文档中的数字属于说明性示例，不能移植；未来真实 fixture 可能产生不同结果。
- Limitations: 仅表示本次 Research Gate 的事实；未来实验需独立记录。
- Applicability: Author 必须保持所有数字与 production outcome 缺席。

## Counter-evidence and alternatives

1. **“Skill 就不是 Prompt”过强。** 多个产品明确把 Skill instructions 注入 Context / system prompt；可守住的差异是 package + discovery + resources + lifecycle。
2. **“所有兼容 Agent 都 project-over-user”不成立。** implementor guide 的推荐与 Codex 当前“同名均出现”行为冲突；必须写 Host-specific collision policy。
3. **“按需加载一定省 token / 提质”无证据。** catalog、激活全文、多 Skill 与 eager preload 均可能增加成本；需 workload eval。
4. **“description 能准确触发”无证据。** 官方专门要求正负 trigger eval，说明 false positive / negative 是需要测量的问题。
5. **“allowed-tools 是跨产品通用、无限授权”不成立。** 开放字段 experimental，支持程度因实现而异；支持它的 Host 可将其消费为预授权 / policy input，GitHub 还明确说明预批准 shell 会移除确认，并要求先审查并信任第三方 Skill 及其脚本。最终 authority 仍受具体 Host / Policy / Runtime 边界约束。
6. **“Skill 自动跨 Agent 共享”不成立。** Anthropic / GitHub 当前都显示 per-agent config / no inheritance 语义。
7. **“版本 pin 等于可靠”不成立。** pin 只固定 bytes / version identity，不证明方法正确、安全或适用于 current environment。
8. **“BuildPilot 已有 Skill 系统”不成立。** 当前只有 Design Proposal；没有 runtime、trigger trace、artifact、build 或 production result。

## Evidence statistics

- Claims: 15 total
- Label classes: 8 FACT / 2 INFERENCE / 3 DESIGN / 2 UNKNOWN
- Evidence status: 8 CONFIRMED / 4 PARTIAL / 3 PROPOSAL / 0 BLOCKED
- Core claims: 13；core BLOCKED: 0
- Non-core unresolved: 2（17-C06 universal semantics；17-C15 measured effect）
- Evidence cards: 12
- Official external source families: 4（Agent Skills open project、OpenAI、Anthropic、GitHub）
- Real product implementation families compared: 3（OpenAI、Anthropic、GitHub）
- Experiments / Labs / provider runs: 0 / 0 / 0
- Invented performance / adoption / production claims: 0

## Unresolved gaps

### Core

NONE。所有核心主张均有足够一手来源，或已显式降为 `INFERENCE / DESIGN` 并限定措辞。

### Non-core

- `17-C06`: universal precedence / collision / unload / inheritance remains UNKNOWN；解除条件是各产品提供并核对 exact current contract，仍不能据此推成行业统一。
- `17-C15`: measured effect remains UNKNOWN；解除条件是冻结 BuildPilot-like fixture、provider / model / host / Skill versions、positive / near-miss negative triggers、with / without baseline、assertions、raw outputs、timing / token receipts 与重复运行。

## Author handoff boundaries

- 可以写：共享 package / progressive disclosure 模式；三家产品差异；Prompt / Tool / Workflow / Agent / KB / Memory 分账；trigger 风险；安全 trust boundary；lifecycle checklist；BuildPilot design case。
- 必须标记：课程 I/O schema、decision log、context disposition、BuildPilot candidate Skills 都是 `DESIGN`。
- 禁止写：统一 precedence / trigger algorithm / unload；“所有产品”；任何 token、accuracy、latency、cost、success、adoption 或 production benefit；BuildPilot 已实现 / 已验证。
- 当引用产品行为时，紧邻写清 surface、access date 与 beta / moving-target caveat。

## Evidence Gate recommendation

**PASS。** 核心 Claims `0 BLOCKED`；十个 human questions 均有可用 Evidence 或明确 Design boundary；cross-implementation invariant 与 product fact 已分开；counter-evidence 已保留；`EXPERIMENT COUNT=0 / Observed Result=ABSENT`。下一允许 Gate 建议为 `EVIDENCE_GATE`，由 Master 验证后决定是否进入 Author。
