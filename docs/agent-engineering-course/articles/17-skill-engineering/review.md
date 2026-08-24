# Article 17 Review｜Skill Engineering

## Review state

- Reviewer: FRESH_INDEPENDENT_REAL_SUBAGENT
- Review Cycle: 0 / 3
- Gate Decision: PASS_WITH_NOTES
- Score: 92 / 100
- Open Findings: 2（MINOR 2）
- Required Lab: NONE
- Next Allowed Gate: REVISION
- Cycle Accounting: 初始 Findings，不递增 review_cycle

本轮独立复核覆盖 Article 17 全部 durable artifacts、系列与课程工厂契约、术语表、当前运行状态，以及已发布 Article 02 / 06 / 08 / 10 / 12 / 13 / 15 / 16。产品事实按证据卡所列官方来源复查；未读取或使用 Author 的聊天总结、隐藏推理、置信度或自评分。

## Technical Review

- [x] Skill 与 Prompt / Tool Runtime / Workflow / Agent / Subagent / Memory / KB-RAG / Harness-Policy 的主体边界整体准确；allowed-tools 的宿主特定授权语义需按 17-F01 再窄化。
- [x] 开放规范、OpenAI Codex / ChatGPT、Anthropic API / Managed Agents、GitHub Copilot 的产品实现均有显式 surface 与日期边界，没有被写成统一行业标准。
- [x] Discovery、Selection、Load、Execution、Verification、Context End 六步生命周期完整，且区分“入口被加载”“脚本真的执行”“结果通过验证”。
- [x] 输入、输出、依赖、权限、适用条件、版本、失败语义均进入八对象契约与生命周期收据；安全责任仍归 Host / Policy / Runtime。
- [x] FACT / DESIGN / UNKNOWN 使用不同措辞；四个 BuildPilot Skill 均保持候选设计，不冒充产品事实。
- [x] 通用 Agent 加 Skill 与专用 Agent / Subagent 的拆分条件明确；多 Agent 的线程、上下文、工具、Skill 与凭证语义保持产品限定。

## Evidence Review

- [x] Claim Register 为 15 条，Evidence Card 为 12 张；正文与 Outline 的 C01-C15 覆盖完整。
- [x] 每张核心证据卡包含来源、访问日期、产品 / surface 范围、Proves、Does Not Prove、限制及反证；Anthropic beta 范围的发布前刷新问题见 17-F02。
- [x] C03 / C05 / C06 / C15 的 PARTIAL 结论均已收窄；没有 BLOCKED claim，也没有结论依赖 BLOCKED evidence。
- [x] C07 / C12 / C14 的 DESIGN 内容持续使用“课程契约”“建议”“候选方案”，与产品事实分离。
- [x] 实验边界明确为 EXPERIMENT COUNT = 0、Observed 空白；未声称生产落地、实测收益、自动降本或质量提升。

## Course and teaching review

- [x] 首屏从“同类诊断任务反复重讲、重找、重拼”建立工程问题，没有从产品 API 起笔。
- [x] 先给“可发现、可加载、可验证的能力包”与八对象抽象模型，再进入具体产品 surface。
- [x] BuildPilot 从候选识别、触发消歧、上下文预算、安全边界到版本退役形成连续案例。
- [x] Article Card 批准的 10 个问题均在正文与 Learning Check 中得到回答或回收，段落转换与非目标边界清楚。
- [x] 对 Workflow、Context、Memory / KB、Evaluation、Safety、Multi-Agent 的后续文章只建立接口桥，不提前展开正文。
- [x] 工程迁移落到 capability registry、owner、版本、适用条件、验证标准、失败语义与退役收据，能支撑 Tech Lead 级治理判断。
- [x] Required Lab 为 NONE；无实验是本篇原则型文章的声明边界，不构成缺陷。

## BuildPilot boundary audit

- 候选 Skill 恰好 4 个：jenkins-build-triage、unity-compile-diagnosis、yooasset-artifact-chain-audit、release-evidence-pack。
- 四者均为 DESIGN / NOT IMPLEMENTED / NOT RUN；没有第五个候选 Skill。
- EXPERIMENT COUNT = 0，Observed 为空；正文未把候选方案写成生产能力或测量结果。
- 触发、上下文、权限、验证、生命周期与 owner 均是设计合同，不是已部署事实。

## Publication risk review

- Draft 阶段无 YAML frontmatter；现有 relref 均使用 ASCII 双引号，链接目标属于已发布前置文章。
- 未出现内部 Course Factory 路径、worker 编排、review cycle、隐藏分数等读者侧泄漏。
- 未发现长段官方文档复刻或超量直接引语；产品行为主要采用受范围限定的转述。
- 后续文章仅作职责桥接；未越界写成 Context、Evaluation、Safety 或 Multi-Agent 的完整正文。

## Findings

### 17-F01

- Finding ID: 17-F01
- Severity: MINOR
- Category: TECHNICAL
- Location: draft.md，“权限先于执行”表格及其后两段（allowed-tools 文本 ≠ 授权）
- Problem: 这条等式作为跨产品安全原则过于绝对。开放规范把 allowed-tools 定义为预批准工具字段，GitHub Copilot 文档更明确说明该字段可让列出的 Bash / shell 工具免于确认；因此它在支持该字段的宿主上可以成为授权或策略输入。正文后文虽说明 GitHub 的具体行为，却仍把字段本身与授权完全切开，造成同段内的语义冲突。正确边界应是：Skill 文本不能自行获得宿主之外的权限，allowed-tools 也不是跨产品通用或无限授权；但宿主可以将其消费为预授权策略。
- Supporting Evidence: Agent Skills 规范将 allowed-tools 标为实验性的预批准工具列表（https://agentskills.io/specification）；GitHub 官方“Creating agent skills”说明该字段可在 Copilot CLI 中移除特定 shell 命令的确认提示，并要求仅用于可信 Skill（https://docs.github.com/en/copilot/customizing-copilot/extending-copilot-chat-with-mcp/creating-agent-skills）。Article 13 已建立“文本建议不等于最终权限，授权由宿主 / 策略 / 运行时执行”的上层边界，但不排除宿主把结构化字段作为授权输入。
- Why It Matters: 这是安全敏感字段。若读者把它理解为纯注释，可能忽略安装第三方 Skill 后宿主实际放宽确认门槛的风险；若把它理解为通用授权，又会跨产品过度外推。
- Required Disposition: 将该行及相邻说明改为宿主限定表述，例如“allowed-tools 可被支持它的宿主解释为预授权策略，但不构成跨产品通用、无限或脱离 Host / Policy / Runtime 的授权”；保留 GitHub 的具体反例，并明确安装前审查要求。Finding 保持 OPEN，待 Revision 后复核。

### 17-F02

- Finding ID: 17-F02
- Severity: MINOR
- Category: EVIDENCE
- Location: draft.md，“不同产品如何实现 Skill”中的 Anthropic 条目；evidence.md EC05 / C09 版本边界
- Problem: 正文把 Anthropic API Skills 与 Managed Agents 合并后称“相关接口是 beta”，但当前官方 surface 已不同步：Managed Agents 文档仍显式要求 beta header，而当前 Agent Skills API 指南展示 skver_... 版本 ID、latest 与精确版本固定，却不再在页面上标出 evidence.md 记录的 skills-2025-10-02 beta header。证据卡自己也把该页面标为 moving target、要求发布前刷新。按当前文档，不能继续用一个笼统的 beta 标签覆盖两个 surface。
- Supporting Evidence: 当前 Anthropic Agent Skills API 指南描述自定义 Skill 的完整版本快照、latest 与精确版本固定（https://platform.claude.com/docs/en/agents-and-tools/agent-skills/guide），页面未展示 EC05 所记 beta header；Managed Agents 概览仍显式要求 managed-agents-2026-04-01 beta header（https://platform.claude.com/docs/en/agent-sdk/managed-agents/overview）。EC05 的 Limitations 已要求“正式发布前必须刷新原页”。
- Why It Matters: 版本与 beta 状态是高漂移的产品事实。把两个 surface 合并会让读者误判 API 稳定性，也削弱全文反复强调的“产品 × surface × 日期”证据纪律。
- Required Disposition: 发布前刷新 EC05 与正文，并分别陈述 Agent Skills API 和 Managed Agents 的当前状态；若无法从当前官方 API 页面证明 beta，就删除 API 的 beta 断言，只保留精确版本 / latest 机制及访问日期，同时把 beta header 明确限定到 Managed Agents。Finding 保持 OPEN，待 Revision 后复核。

## Open Finding Summary

- OPEN: 17-F01（MINOR / TECHNICAL）
- OPEN: 17-F02（MINOR / EVIDENCE）
- Severity counts: BLOCKER 0 / MAJOR 0 / MINOR 2 / EDITORIAL 0

## Scores

| Dimension | Score | Artifact evidence |
|---|---:|---|
| Technical Accuracy | 18 / 20 | 八对象与六步生命周期成立，层间责任清楚；扣分对应 17-F01 的宿主特定授权语义。 |
| Evidence Discipline | 18 / 20 | 15 claims / 12 cards、PARTIAL / DESIGN / UNKNOWN 分层与零实验账完整；扣分对应 17-F02 的高漂移 surface 状态。 |
| Teaching Quality | 19 / 20 | 问题先行、抽象先于实现、BuildPilot 连续案例及 10 个批准问题均完整。 |
| Engineering Transfer | 19 / 20 | 能迁移到 registry、owner、版本、验证、失败语义与退役治理，并体现边界决策。 |
| Readability & Compression | 18 / 20 | 长文层级与转场清晰、结论可回收；产品对照与证据边界略密，但未形成独立 actionable Finding。 |
| **Total** | **92 / 100** | 达到总分及 Technical / Evidence / Teaching / Engineering Transfer 当前门槛。 |

## Final Gate

- REVIEW Gate 已完成，决定为 PASS_WITH_NOTES。
- 两个 actionable Findings 保持 OPEN；不得直接进入 FINAL_GATE。
- next_allowed_gate = REVISION。
- blocker = NONE。

## Cycle 1 Revision Disposition

### 17-F01

- Finding ID: 17-F01
- Files Changed: `draft.md`、`evidence.md`、`review.md`（Revision Disposition only）
- What Changed: 将 `allowed-tools text != authorization` 的绝对表述改为宿主限定语义：支持该字段的 Host 可把它消费为预授权 / policy input，但它不构成跨产品通用、无限或脱离 Host / Policy / Runtime 的 authority；保留 GitHub 移除逐次确认的具体风险与第三方 Skill / 脚本审查要求。
- Evidence Impact: 不新增核心 Claim；仅把 EC01 / EC07 已支持的开放字段与 GitHub 产品行为对齐，并同步收窄 counter-evidence。
- Proposed Status: READY_FOR_RECHECK

### 17-F02

- Finding ID: 17-F02
- Files Changed: `draft.md`、`evidence.md`、`research.md`、`review.md`（Revision Disposition only）
- What Changed: 分开 Anthropic Agent Skills API 与 Managed Agents 两个 surface；API 只保留当前指南证明的 `skver_...`、完整 snapshot、exact pin / `latest` 机制，不再声明 beta；Managed Agents 的 beta 与 `managed-agents-2026-04-01` header 只限定到其当前 overview。
- Evidence Impact: EC05 / C09 与 research source manifest 已按 2026-08-25 官方页面刷新；没有升级 Claim，也没有改变 Evidence Gate、实验数或 BuildPilot 设计边界。
- Proposed Status: READY_FOR_RECHECK

## Cycle 1 Recheck

### 17-F01

- Finding ID: 17-F01
- Reviewer Status: CLOSED
- Verification: `draft.md`与`evidence.md`已将`allowed-tools`限定为支持该字段的Host可消费的预授权 / policy input；明确排除跨产品通用、无限或脱离Host / Policy / Runtime的authority，并保留第三方Skill审查风险。

### 17-F02

- Finding ID: 17-F02
- Reviewer Status: OPEN
- Verification: Cycle 1把Agent Skills API写成`skver_...`且不需要beta，与2026-08-25当前官方API指南不符。当前页面使用自定义`skill_...` ID、自定义epoch timestamp版本或`latest`，并要求`skills-2025-10-02` beta；Managed Agents必须继续作为另一surface单独陈述其beta要求。
- Required Correction: 在`draft.md`、`evidence.md`与直接失效的`research.md` source manifest中恢复上述Agent Skills API当前事实；不得把Agent Skills API与Managed Agents的beta header合并。

## Cycle 1 Recheck Summary

- CLOSED: 17-F01（MINOR / TECHNICAL）
- OPEN: 17-F02（MINOR / EVIDENCE）
- next_allowed_gate = REVISION
- review_cycle = 1

## Cycle 2 Revision Disposition

### 17-F02

- Finding ID: 17-F02
- Files Changed: `draft.md`、`evidence.md`、`research.md`、`review.md`（Revision Disposition only）
- What Changed: 将 Agent Skills API 刷新为当前独立 surface：custom Skill ID 为 `skill_...`，custom version 使用 epoch timestamp 或 `latest`，并要求 `skills-2025-10-02` beta header；删除未获当前指南直接支持的“complete snapshot”表述。Managed Agents 仍作为另一独立 surface，保留自己的 beta header 要求（包括适用时的 `managed-agents-2026-04-01`），未与 API header 合并。
- Evidence Impact: 仅更新 C09、EC05 与直接失效的 Anthropic research statements / source manifest；15 Claims、12 Evidence Cards、零实验和 BuildPilot 设计边界均未改变。
- Proposed Status: READY_FOR_RECHECK

## Cycle 2 Recheck

### 17-F01

- Finding ID: 17-F01
- Reviewer Status: CLOSED / FROZEN
- Verification: Cycle 1 已将 `allowed-tools` 收窄为 Host 可消费的预授权 / policy input，明确排除跨产品通用、无限或脱离 Host / Policy / Runtime 的 authority。本轮未重开或改变该处的已关闭处置。

### 17-F02

- Finding ID: 17-F02
- Reviewer Status: CLOSED
- Verification: 已对 `draft.md`、`evidence.md`、`research.md` 的活跃陈述逐项复核。它们均将 Anthropic Agent Skills API 限定为独立 surface：custom Skill ID 为 `skill_...`，custom version 为 Unix epoch timestamp 或 `latest`，并要求 `skills-2025-10-02` beta。未发现活跃的 `complete snapshot`、`skver_...` 或“API 不需要 beta”陈述。Managed Agents 保持独立表述，并仅在其自身 surface 下使用 `managed-agents-2026-04-01` beta header；两者未混用。`skver_...` / no-beta / complete-snapshot 仅保留在 Cycle 0/1 的历史 Finding 与 disposition 中，作为审计轨迹，不构成当前文章事实。
- Primary-source rationale: 2026-08-25 当前 Anthropic Agent Skills API guide 显示 custom `skill_...` ID、epoch timestamp / `latest` version，以及 `skills-2025-10-02`；Managed Agents 官方 Skills 页面将 `managed-agents-2026-04-01` 规定为其 API 请求的独立 beta header。

## Cycle 2 Recheck Summary

- CLOSED / FROZEN: 17-F01（MINOR / TECHNICAL）
- CLOSED: 17-F02（MINOR / EVIDENCE）
- Static invariants: 15 Claims；12 Evidence Cards；BuildPilot 恰好四个命名候选；`EXPERIMENT COUNT = 0` 且 `OBSERVED RESULT = ABSENT`；7 个 relref 全部存在；Article 18 / Part III audit assets absent；UTF-8 无 U+FFFD；`git diff --check` clean。
- Open Findings: 0
- Score: 96 / 100
- next_allowed_gate = FINAL_GATE
- review_cycle = 2

## Final Gate Cycle 2

- Gate Decision: FAIL
- Open Findings: 1（MAJOR 1）
- next_allowed_gate = REVISION

### Reopened 17-F02

- Finding ID: 17-F02
- Severity: MAJOR
- Category: EVIDENCE
- Location: `draft.md` Anthropic product-surface bullet and lifecycle paragraph；`evidence.md` C09 / EC05；`research.md` RQ09 / comparison table / source manifest.
- Problem: 2026-08-25 Final Gate live refresh contradicts the active package. The current Anthropic Agent Skills API guide uses custom Skill IDs `skill_...`, custom version IDs `skver_...` or `latest`, and says a new version is a complete snapshot. Its current prerequisites and examples do not require or show the package’s `skills-2025-10-02` beta header. Managed Agents remains a separate beta surface.
- Supporting Evidence: current official guide https://platform.claude.com/docs/en/build-with-claude/skills-guide, live lines 208-210 and 559-564 accessed 2026-08-25；current Managed Agents official documentation for its separately scoped beta header.
- Why It Matters: these are high-drift product facts. Publishing the opposite version format and API header requirement would make the article technically false at the publication checkpoint and violate the article’s own product × surface × date discipline.
- Required Disposition: refresh all active Agent Skills API statements to `skill_...` Skill ID, `skver_...` exact version or `latest`, and complete-snapshot update semantics；remove the API beta-header requirement unless the current official guide explicitly proves it. Keep Managed Agents and its own beta header separate. Then run a fresh independent recheck before another Final Gate.

## Final Gate Cycle 2 Scores

| Dimension | Score | Reason |
|---|---:|---|
| Technical Accuracy | 16 / 20 | Active version/header facts conflict with the live official guide. |
| Evidence Discipline | 14 / 20 | Publication-time moving-target refresh was not preserved in the frozen package. |
| Teaching Quality | 19 / 20 | Problem-first structure and ten approved questions remain complete. |
| Engineering Transfer | 18 / 20 | Lifecycle, registry, verification and retirement transfer remain useful. |
| Readability & Compression | 18 / 20 | Structure remains clear；the reopened product facts prevent publication. |
| **Total** | **85 / 100** | Below 88；Technical and Evidence below required 18. |

- Static regression checks: PASS；15 Claims / 12 Evidence Cards / ten approved questions / exactly four BuildPilot candidates / DESIGN-NOT IMPLEMENTED-NOT RUN / experiment 0 / Observed absent / 7 relrefs / Article 18 and Part III Audit absent / UTF-8 no U+FFFD / git diff --check clean.
- Final Gate Cycle 2 result: FAIL.
- Blocker: 17-F02 live-documentation contradiction.

## Cycle 3 Revision Disposition

- Reopened item: 17-F02.
- Disposition: corrected current Anthropic custom-Skill facts in active Article 17 materials: generated custom Skill IDs use `skill_...`; custom version selectors use `skver_...` or `latest`; each new custom-Skill version is a complete snapshot; the current Skills guide prerequisites/examples do not show or require `skills-2025-10-02`.
- Boundary retained: Managed Agents remains a separate beta surface with its own header; that header is not a Skills prerequisite.
- Scope: no changes to historical review records, F01, state/README/outline, Article18, PartIII audit, or published content.
- Verification candidate: restore current official guide facts without changing the preserved 15 claims, 12 cards, ten questions, exact four candidates, design-not-run status, experiment 0, or `Observed absent`.
- Proposed Status: READY_FOR_RECHECK

## Cycle 3 Recheck

### 17-F01

- Finding ID: 17-F01
- Reviewer Status: CLOSED / FROZEN
- Verification: Preserved as closed. This recheck did not reopen, reinterpret, or modify the host-scoped `allowed-tools` disposition.

### 17-F02

- Finding ID: 17-F02
- Reviewer Status: CLOSED
- Verification: Fresh live read of the current official [Using Agent Skills with the API](https://platform.claude.com/docs/en/build-with-claude/skills-guide) confirms the active statements in `draft.md`, `evidence.md`, and `research.md`: custom Skill IDs use `skill_...`; custom version selectors use `skver_...` or `latest`; a new custom-Skill version is a complete snapshot and omitted files are not carried forward; the current guide’s prerequisites/examples require an API key and Code Execution and do not show or require `skills-2025-10-02`. Managed Agents remains separately scoped: its current overview labels that surface beta and requires its own `managed-agents-2026-04-01` header. No active statement merges the two surfaces or assigns the Managed Agents header to the Skills API.

## Cycle 3 Recheck Summary

- CLOSED / FROZEN: 17-F01 (MINOR / TECHNICAL).
- CLOSED: 17-F02 (MAJOR / EVIDENCE).
- Static invariants: 15 Claims; 12 Evidence Cards; 10 human-approved questions; exactly four named BuildPilot candidates (`jenkins-build-triage`, `unity-compile-diagnosis`, `yooasset-artifact-chain-audit`, `release-evidence-pack`); all remain `DESIGN / NOT IMPLEMENTED / NOT RUN`; experiment count is 0 and Observed Result is ABSENT; 7 `relref` targets exist; Article 18 and Part III audit assets are absent; Article 17 UTF-8 files contain no U+FFFD; `git diff --check` passes.
- Open Findings: 0 (BLOCKER 0 / MAJOR 0 / MINOR 0 / EDITORIAL 0).
- Score: 96 / 100.
- next_allowed_gate = FINAL_GATE.
- review_cycle = 3.

## Final Gate Cycle 3

- Gate Decision: PASS.
- Open Findings: 0 (BLOCKER 0 / MAJOR 0 / MINOR 0 / EDITORIAL 0).
- Finding closure: 17-F01 remains CLOSED / FROZEN. 17-F02 remains CLOSED: a fresh official-source read on 2026-08-25 confirms that custom Agent Skills use a generated `skill_...` ID, accept a `skver_...` exact version or `latest`, and create each update as a complete snapshot; the current Skills guide requires an API key and Code Execution and does not show or require `skills-2025-10-02`. Managed Agents is separately scoped and remains beta with its own `managed-agents-2026-04-01` header. [Using Agent Skills with the API](https://platform.claude.com/docs/en/build-with-claude/skills-guide) [Managed Agents overview](https://platform.claude.com/docs/en/managed-agents/overview)
- Frozen-package checks: 15 Claims; 12 Evidence Cards; 10 Research Questions; exactly four named BuildPilot candidates; all remain DESIGN / NOT IMPLEMENTED / NOT RUN; Experiment Count = 0; Observed Result = ABSENT; 7 `relref` targets exist; Article 18 and Part III audit assets are absent; all Article 17 package files are valid UTF-8 with terminal LF and no U+FFFD; `git diff --check` passes.
- Scope guard: this gate appended this review record only; it creates no Article 18, Part III audit, published content, BuildPilot runtime, experiment, or production assertion.

## Final Gate Cycle 3 Scores

| Dimension | Score | Reason |
|---|---:|---|
| Technical Accuracy | 20 / 20 | The reopened Anthropic facts now agree with the live official API guide, and permission, runtime, and product-surface boundaries remain scoped. |
| Evidence Discipline | 20 / 20 | All 15 claims retain status and wording boundaries; the moving Anthropic surface was refreshed at publication time and remains separated from Managed Agents. |
| Teaching Quality | 19 / 20 | The principle-article path holds: problem space, abstract model, concrete mechanisms, engineering boundaries, and ten approved questions. |
| Engineering Transfer | 19 / 20 | Contract, trigger/eval, provenance, version, rollback, and fail-closed boundaries transfer without misrepresenting the four candidates as implemented. |
| Readability & Compression | 18 / 20 | The long-form comparison is navigable and the final FACT / DESIGN / UNKNOWN boundary is explicit; necessary product detail remains dense. |
| **Total** | **96 / 100** | Meets the total >= 88 threshold; Technical / Evidence >= 18 and Teaching / Engineering Transfer >= 17. |

- Final Gate Cycle 3 result: PASS.
- next_allowed_gate = PUBLISH.
- blocker = NONE.

~~~yaml
worker_result:
  role: REVIEWER
  article: "17"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/17-skill-engineering/review.md
  gate_completed: true
  next_allowed_gate: PUBLISH
  blocker: NONE
  notes:
    - "Final Gate Cycle 3 PASS; 17-F01 CLOSED/FROZEN and 17-F02 CLOSED against live official Anthropic documentation."
    - "Score 96 / 100; 0 OPEN Findings; all required score thresholds met."
    - "15 claims, 12 cards, 10 questions, exact four DESIGN / NOT IMPLEMENTED / NOT RUN candidates, experiment 0, Observed absent, 7 relrefs, UTF-8, and diff check pass."
~~~
