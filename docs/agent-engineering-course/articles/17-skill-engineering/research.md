# Article 17 Research｜Skill Engineering

## Gate status

- Owner: RESEARCHER
- Status: COMPLETED
- Evidence Gate Recommendation: PASS
- Required Lab: NONE
- Source access date: 2026-08-24；Anthropic C09 targeted refresh: 2026-08-25
- Experiment count: 0
- Runtime observation: ABSENT

## 最短研究判断

Skill 是**可发现、在相关任务出现时加载、可携带说明 / 脚本 / 参考资料 / 模板的领域方法包**。开放 Agent Skills 格式与 OpenAI、Anthropic、GitHub 实现都支持这一共享模式；目录位置、触发、冲突、权限、版本、上下文驻留和多 Agent 继承属于 Host / 产品行为，不可写成行业统一保证。

“不是再堆一层 Prompt”不等于 Skill 指令从不进入 Context。许多实现会注入完整 `SKILL.md`。差别是 Skill 有可发现身份、范围、按需装载、配套资源和独立生命周期；只把长文本常驻 system prompt，没有 selection、version、validation、trust 与 retirement，仍未解决 Skill Engineering。

## 十个 Research Questions

### 17-RQ01｜定义与产品差异 — SUPPORTED / SCOPED

开放格式最小对象是含 `SKILL.md` 的目录；文件有 YAML frontmatter 与 Markdown body，必填 `name`、`description`，可带 `scripts/`、`references/`、`assets/`。三层披露为 metadata -> full instructions -> resources。课程模型为 `Discovery Metadata + Domain Method + Optional Resources + Host-owned Governance`；最后一项不是开放格式字段。三家产品的路径、显式调用、上传、版本和权限不同，故不存在已被本轮证据支持的统一 Skill Runtime。

### 17-RQ02｜Prompt / Tool / Workflow / Agent / Subagent / KB / Memory 边界 — SUPPORTED

| Object | 责任 | Skill 不负责 |
|---|---|---|
| Prompt | 本次目标、约束、输入、输出、失败语义 | discovery、版本、权限、供应链 |
| Skill | 一类任务的按需领域方法与资源 | 外部能力、权威事实、授权、完成证明 |
| Tool / Runtime | 能力及 Validate、Policy、Execute、Result、Trace | Skill 只能指导调用，不能授予权限 |
| Workflow | 状态、步骤、分支、guard、terminal | Skill checklist 不是全局 commit authority |
| Agent / Subagent | 执行主体及独立配置 / context | Skill 是资源；继承由产品决定 |
| KB / RAG | 动态检索、过滤、引用候选知识 | reference 不替代 Retrieve / Cite / Verify |
| Memory | 跨 Step / Session 保存与召回 | Skill 不保存当前调查状态或把旧记录变事实 |
| Harness / Policy | packing、budget、权限、trace、recovery | Skill 不拥有 precedence、approval、sandbox |

### 17-RQ03｜metadata 与 instructions 分层 — SUPPORTED

启动只暴露 `name + description`，选中后加载完整 `SKILL.md`，资源再按需读取，避免所有完整方法常驻默认 Context。但 catalog 仍有成本，激活后的 instructions 仍竞争窗口；GitHub Copilot SDK 对 custom-agent Skills 还会 eager preload。progressive disclosure 是共享模式，不保证每个 surface 同时机，也不证明 token savings 数字。

### 17-RQ04｜discover -> select -> load -> execute -> verify -> end — SUPPORTED COURSE ABSTRACTION

```text
Discover catalog -> Match scope -> Select explicit / implicit
-> Load SKILL.md -> Load required resources -> Follow method
-> Tool / Workflow -> Verify result -> Record activation / version / outcome
-> Host decides retain / compact / release
```

官方 implementor guide 说明多数实现由模型基于 catalog 判断，也建议显式激活、去重和保护 Skill content 不被 compaction 丢失；未定义统一 unload / context-end，所以最后一步只能标 `DESIGN`。

### 17-RQ05｜I/O、依赖、权限、范围、版本、失败 — FORMAT FACTS + DESIGN

开放规范只冻结 `name`、`description`，可选 `license`、`compatibility`、`metadata`、实验性 `allowed-tools` 与自由 body / resources；没有统一 typed I/O / failure schema。生产包应另写 Scope / near-miss、input provenance / freshness、output artifact、tool / package / network dependency、permission / approval、procedure、validator、failure / stop、owner / version / review。这是课程设计；真实授权与 sandbox 仍由 Host / Tool Runtime 执行。

### 17-RQ06｜context pollution、误触发、漏触发 — SUPPORTED MECHANISM

description 要写 what / when / near-miss；用 should-trigger 与 near-miss should-not-trigger queries；保持 coherent unit；只在 `SKILL.md` 放每次激活必需内容；延迟读 variant reference；记录 candidate / selected / reason / version / resource。误触发增加冲突与 Context 成本，漏触发缺失领域方法。未运行 BuildPilot eval，trigger precision / recall 和 token savings 均 UNKNOWN。

### 17-RQ07｜通用 Agent + Skill vs 专用 Agent — SUPPORTED BOUNDARY

同一主体偶尔需要可复用方法 / 模板 / 脚本时，用通用 Agent + Skill。需要独立 model、system、tool、credential、permission、context isolation、生命周期或 delegated ownership 时，用专用 Agent / Subagent。Anthropic Managed Agents 每个 Agent 有自身 skills 且 context/tools/MCP 不共享；GitHub Copilot SDK subagent 不继承 parent skills，需显式列出；开放 guide 仅把 subagent delegation 列为 optional。

### 17-RQ08｜何时创建 / 不创建 — SUPPORTED + DESIGN

适合：重复、领域特定、可行动、只在部分任务需要、可观察验收、值得独立评审与版本化。不适合：一次性简单请求；全局规范（常驻 instructions）；新能力（Tool / MCP）；确定性编排 / 审批 / 发布权（Workflow / Harness / Policy）；独立身份（Agent）；动态事实（KB / RAG）；无法写可靠 procedure / failure semantics 的泛泛建议。

### 17-RQ09｜测试、版本、发布、回滚、观测、退役 — SUPPORTED PRACTICES

```text
Source -> Review -> Static Validate -> Trigger Eval
-> With/Without or Old/New Eval -> Pin / Deploy -> Observe
-> Promote / Roll Back -> Deprecate -> Disable -> Retire
```

Static validator 不证明行为；Trigger eval 覆盖 realistic positives / near-miss negatives；Behavior eval 保存 outputs、assertions、tokens / timing、execution trace、human review；安全审查覆盖所有资源、依赖、network、credential、side effect、provenance。生产 pin exact version；失败后保留 input / trace，再 pin last-known-good、disable 或回滚 Git。Anthropic 当前官方文档存在需要按 endpoint 分账的跨页不一致：Messages / container 的 Skills Guide 使用 custom `skill_...` ID，把 invocation `version` selector 写成 `skver_...` 或 `latest`，并只在该 Guide 范围内说明新版本是完整 snapshot；Skill Version 管理 API 的 Get / List / Create reference 则把 path / response `version` 定义为 Unix epoch timestamp，另返回 `skillver_...` 形态的对象 `id`，其 cURL 示例携带 `anthropic-beta: skills-2025-10-02`。Guide 的高层 prerequisites 只列 API key 与 Code Execution、示例不展示该 header，不能据此证明 raw management API 不需要 header。当前 invocation selector、management `version` 与 response object `id` 的跨页映射保持 moving-source limitation，调用前按具体 endpoint 复核。Managed Agents 仍是独立 surface。Codex 可 disable，本地可 Git rollback，plugins 用于分发；ChatGPT workspace 有 owner / access / invocations / timestamps / delete。均为产品范围事实。

### 17-RQ10｜BuildPilot 连续案例 — DESIGN / NOT IMPLEMENTED / NOT RUN

用户报告“Jenkins 上 Unity Android 构建在 YooAsset 更新后失败”。先判断 Evidence 指向 CI、Unity compile，还是 YooAsset artifact / remote delivery，再加载窄 Skill，不默认加载万能包。

| Candidate | Trigger boundary | Artifact | Failure modes |
|---|---|---|---|
| `jenkins-build-triage` | job/build/stage/log；不改 Job、不重跑 | first failing stage、log locator、artifact inventory | credential 缺失、log 截断、identity 漂移、只有截图 |
| `unity-compile-diagnosis` | compiler / player error；非一般 CDN 故障 | version/target/first error/source diagnosis packet | 旧日志、生成目录污染、缺 revision、误取 stack tail |
| `yooasset-artifact-chain-audit` | manifest/package/cache/download/remote；非纯 compile | source/config/artifact/runtime/request/cache/remote matrix | request object 冒充 CDN bytes、build success 冒充 usable package、无 remote readback |
| `release-evidence-pack` | 调查结果 handoff；不执行发布 | claim mapping、gaps、decision log | locator 冒充 verification、Proposal 冒充 observed、越权发布 |

```text
Task -> select Jenkins read-only Skill -> Tool Runtime reads exact build
-> verify first failing stage -> choose compile OR resource path
-> load one narrower Skill -> evidence artifact + UNKNOWN
-> Workflow decides terminal -> Harness records version/resource/permission/result/context
```

Decision log proposal：`run_id / step_id / catalog_digest / candidate IDs+versions / selected+reason / trigger_mode / resource digests / tool+workflow refs / permission / artifact refs / verdict / terminal / context_disposition`。没有 BuildPilot runtime 或生产结果。

## Shared invariants vs product facts

共享：`SKILL.md` 入口；name / description 参与 discovery；selection 后使用全文；资源按需；Skill 可指导 Tool 但不等于 Tool / Permission / Agent；trigger 与 output 都需 eval。

| Concern | Open guide | OpenAI | Anthropic | GitHub Copilot |
|---|---|---|---|---|
| Roots | format 不规定 | Codex CWD->repo + user/admin/system | Managed repo root `.claude/skills`; API upload | 多 repo / personal roots |
| Invocation | model-driven + explicit 建议 | ChatGPT `@`; Codex `$` / `/skills` + implicit | Managed relevant auto；API attach | CLI slash + auto |
| Collision | guide 建议 deterministic precedence | Codex 同名不 merge、均可出现 | 同名 repo/attached/mount 均可用并带 path | 本轮未确认统一规则 |
| Version | 无统一 registry | Git/disable/plugins；ChatGPT admin | Messages/container Guide：`skver_...` selector / `latest`、complete snapshot；management API path/response `version`：epoch timestamp；response `id`：`skillver_...`；management cURL 含 Skills beta header；跨页映射未解决 | `gh skill` install/update/publish；pin 未确认 |
| Context | 三层披露 | catalog 有预算 | API render system prompt | CLI 按需；SDK eager preload |
| Multi-agent | optional | 本轮未确认 | per-agent config | 不继承 parent Skill |

## Decision checklist

1. 是否重复，且通用 Agent 缺少非显然方法？
2. 能否写 scope / near-miss 并做正负 trigger cases？
3. I/O、来源、新鲜度、失败、停止是否可审查？
4. 需要的是方法，而非 Tool、Agent、Workflow、Policy、KB？
5. 能否渐进披露并隔离 variants？
6. scripts 有 dependency、permission、sandbox、validator？
7. owner、source、version、review、rollback、deprecation 可落盘？
8. 有 baseline、fixture、assertions、trace、human review？多项为否则先不建 Skill。

## Limitations / gaps

- Core gaps: NONE；核心 Claim 有 official source，或已收窄为 `INFERENCE / DESIGN`。
- Non-core UNKNOWN：没有统一 precedence、unload、collision、subagent inheritance contract。
- `EXPERIMENT COUNT = 0`：accuracy、quality、trigger precision/recall、token、latency、cost、success rate、production benefit 全部 UNKNOWN。
- 未覆盖所有 surface / plan / region / admin policy；current docs 与 `main` source 是 moving target，发布前刷新。

## Primary source manifest（accessed 2026-08-24）

1. https://agentskills.io/specification
2. https://agentskills.io/client-implementation/adding-skills-support
3. https://agentskills.io/skill-creation/best-practices
4. https://agentskills.io/skill-creation/optimizing-descriptions
5. https://agentskills.io/skill-creation/evaluating-skills
6. https://developers.openai.com/codex/skills （当日重定向 ChatGPT Learn）
7. https://help.openai.com/en/articles/20001066
8. https://github.com/openai/codex/blob/main/codex-rs/skills/src/assets/samples/skill-creator/SKILL.md (`main`, unpinned)
9. https://platform.claude.com/docs/en/managed-agents/skills (`managed-agents-2026-04-01` beta)
10. Anthropic, “Using Agent Skills with the API”, https://platform.claude.com/docs/en/build-with-claude/skills-guide；Get / List / Create Skill Version API references, https://platform.claude.com/docs/en/api/beta/skills/versions/retrieve , https://platform.claude.com/docs/en/api/beta/skills/versions/list , https://platform.claude.com/docs/en/api/beta/skills/versions/create (official pages live-refreshed 2026-08-25；Guide uses custom Skill ID `skill_...` and invocation selector `skver_...` or `latest`, scopes complete-snapshot semantics to its update flow, and lists API key + Code Execution without a Skills beta header；management references define path / response `version` as Unix epoch timestamp, return separate `skillver_...` object `id`, and show `anthropic-beta: skills-2025-10-02` in cURL；the cross-page mapping is unresolved and requires endpoint-specific verification；Managed Agents remains a separate beta surface)
11. https://platform.claude.com/docs/en/managed-agents/multiagent-orchestration (beta)
12. https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills
13. https://docs.github.com/en/copilot/concepts/agents/copilot-cli/comparing-cli-features
14. https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/skills

## Evidence Gate recommendation

**PASS。** 十问全部回答；开放格式与三种实现分账；核心 Claim 无 `BLOCKED`；BuildPilot 与扩展 lifecycle 为 `DESIGN`；实验和收益未虚构。Author 只能在 `evidence.md` wording boundary 内写作。
