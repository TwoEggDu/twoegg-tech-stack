# Article 00 Review Record

- Lifecycle Status：`FINAL`
- Review Status：`M4_1_HUMAN_REVIEW_FIX_COMPLETE`
- Formal Draft Review Gate：`PASSED_WITH_NOTES`
- Checklist：[Agent Engineering 课程审查清单](../../templates/review-checklist.md)
- Reviewer：`Codex self-review`
- Date：`2026-08-19`

## M1 Research Completeness Review

- Outcome：`PASS`
- Findings / Disposition：`RQ-01` 至 `RQ-09` 均有状态、主发现、Claim 映射、剩余不确定性和课程影响；没有通过删掉困难问题来制造完成度。

## M1 Evidence Quality Review

- Outcome：`PASS_WITH_NOTES`
- Findings / Disposition：核心事实优先使用官方文档、官方技术文章、公开规范与原始论文。每个核心 Claim 至少有一张 Evidence Card，且填写 `Proves / Does Not Prove / Limitations`。`00-C06a` 保持 `PARTIAL`，因为两个官方用例不足以证明全行业术语状态。

## M1 Definition Consistency Review

- Outcome：`PASS`
- Findings / Disposition：Definition Matrix 与 glossary 已对齐；Agent 保留跨生态稳定核心，Copilot 标为产品术语，Agentic 标为生态依赖，Runtime / Harness / Host 标为课程工作定义。没有把 working definition 写成行业标准。

## M1 Course Scope Review

- Outcome：`PASS`
- Findings / Disposition：00 只提供定位句与后续路由；没有展开 Agent Loop、Tool Runtime、Context、Memory、RAG、Harness 能力模型；没有做 DSH pinned-source 研究、Lab、BuildPilot 或 Article 01。

## Risk Checks

| Check | Result | Disposition |
|---|---|---|
| 二手资料是否替代一手资料 | `PASS` | 重要主张均有一手来源；未使用 SEO / 培训总结作为证据。 |
| 是否把课程定义写成行业标准 | `PASS` | 所有课程选择均为 `PROPOSAL / DESIGN_PROPOSAL`。 |
| 是否从产品入口推测内部架构 | `PASS` | 三个产品卡片均明确 `Does Not Prove`。 |
| 是否抹平术语真实差异 | `PASS` | Copilot、Agentic、Runtime、Harness 均保留差异。 |
| 是否提前吞掉后续文章 | `PASS` | Definition Matrix 保持一句定位，正式展开文章明确。 |

## Evidence Gate Decision

- Outcome：`PASS_WITH_NOTES`
- Decision：`Article 00 is EVIDENCE_READY`
- Rationale：9 个 RQ 均已处理；核心术语均有直接证据或明确 Proposal 边界；`BLOCKED` Claim 为 0；产品内部实现保持未知；glossary 与课程范围已校正。
- Non-blocking Notes：Harness 的行业普查、Runtime 的框架对比与 DSH 内部结构均留给后续正式文章，不影响 00 的导航职责。

## Formal Review

M4 Technical / Evidence / Course / Reader Value 正式 Review 已完成；完整 Findings、修订处置、评分与 Final Gate 见本文末尾的 `M4 Formal Review`。

## M2 Teaching Structure Review

- Outcome：`PASS`
- Findings / Disposition：提纲以“混层问题 -> Model / Application -> Copilot / Agent / Agentic -> Product / Host / Harness / Runtime -> 横向术语 -> 产品证据边界 -> Article 01”形成单一 Teaching Spine。每个主体 Section 均登记 Teaching Question、Core Thesis、Claim / Evidence、定义类型、措辞强度、示例 / 图、停止线和 Bridge；没有从术语百科式罗列开场。

## M2 Evidence Mapping Review

- Outcome：`PASS_WITH_NOTES`
- Findings / Disposition：14 个 Claim 均进入 Claim Coverage Matrix，15 张 Evidence Card 均有使用位置或明确的不呈现内容。`00-C06a` 继续保持 `PARTIAL`：只能陈述已观察到的官方 Harness 用法含义不同以及当前样本不足，不得升级成全行业结论。6 个 `PROPOSAL` 均指定课程选择语态。

## M2 Scope Review

- Outcome：`PASS`
- Findings / Disposition：Agent 只到导航级稳定核心；Runtime / Harness / Host 只给课程工作定义；Prompt、Context、Tool、Skill、Workflow、Memory、RAG 只给一句定位和正式路由。产品例子限定为三张小卡、预计正文占比不超过 20%；未进入 DSH 源码、Lab、Article 01、BuildPilot 或 Draft。

## M2 Course Dependency Review

- Outcome：`PASS`
- Findings / Disposition：Product / Application 与 Host 已拆开：Product 可使用或暴露一个或多个 Host，Host 是具体运行 / 集成入口。两张计划图分别承担导航关系与定义确定性，不复制后续机制篇。收束唯一桥接 Article 01，未更改 canonical 结构。

## Outline Gate Decision

- Outcome：`PASS_WITH_NOTES`
- Decision：`Article 00 is OUTLINE_READY`
- Rationale：教学问题、论证顺序、Claim / Evidence 映射、图表职责、Learning Check、范围停止线与后续桥接均已明确；没有核心 `BLOCKED` Claim。
- Non-blocking Notes：Draft 必须继续执行 `00-C06a` 的谨慎措辞，并保持 Product / Host 分层与产品例子篇幅上限。
- Next Allowed Action：`M3｜Article 00 Draft`，等待人工 Review，不自动执行。

## M3 Technical Consistency Review

- Outcome：`PASS`
- Findings / Disposition：Draft 保持 Model / Application、Copilot / Agent / Agentic、Product / Host / Harness / Runtime 的既定边界。Agent 只给导航级稳定核心，没有展开 Turn、Step、State 或 Stop；七个横向术语只给最低定位。没有新增需要退回 Research 的核心行为性主张。

## M3 Evidence Consistency Review

- Outcome：`PASS_WITH_NOTES`
- Findings / Disposition：外部事实均使用 Evidence Register 已登记的官方来源入口；产品卡同时写明公开事实与不可推断边界。`00-C06a` 保持有限样本语气，正文同时说明“已观察到的含义不同”和“不足以证明统一行业结论”；所有 Runtime / Harness / Host 分类均明确为课程学习约定。

## M3 Teaching Consistency Review

- Outcome：`PASS`
- Findings / Disposition：正文沿用 M2 Teaching Spine，从真实混层困惑进入，经 Model / Application、三词辨析、课程导航图、横向术语、产品证据练习，最终桥接 Article 01。六个主体 Section、两张图与五道 Learning Check 均已进入 Draft，没有提前吞掉后续机制篇。

## M3 Reader Quality Review

- Outcome：`PASS`
- Findings / Disposition：开头直接进入工程师会遇到的术语混层；以 Unity Build Log 摘要按钮落地 Model / Application 边界；Figure 1 提供长期心智模型；结尾形成“概念层 / 定义来源 / 证据边界”三问法。正文不是 Research Report、Glossary 或产品横评。

## M3 Compression Review

- Outcome：`PASS`
- Findings / Disposition：产品卡压缩到正文约 10%；删除框架部署差异、Agent Loop 细节、Harness 能力清单、DSH Plugin 机制和七个横向术语的实现管线。Draft 只保留直接兑现 Reader Promise 的定义、例子、地图、证据边界与课程桥接。

## Draft Readiness Gate Decision

- Outcome：`PASS_WITH_NOTES`
- Decision：`Article 00 Draft is ready for Formal Review`
- Rationale：第一版正文完整可读，定义、证据强度、Teaching Spine、篇幅、图表和学习检查均满足 M3 要求；核心 `BLOCKED` Claim 为 0。
- Non-blocking Notes：M4 继续重点审查 Harness 的 `PARTIAL` 措辞、Figure 1 的非通用架构图注和产品卡的版本敏感事实。
- Next Allowed Action：`M4｜Article 00 Formal Review & Revision`，等待人工 Review，不自动执行。

## M4 Formal Review

### First-pass Review Findings（Pre-revision）

| Finding ID | Severity | Category | Location | Issue | Why it matters | Required change |
|---|---|---|---|---|---|---|
| `M4-F01` | `MAJOR` | `COURSE` | Section 3，Figure 1 | Product、Host、Harness、Runtime 仍画成单向向下的调用链，图注虽有限定，视觉上仍容易被当成通用物理部署架构。 | 这会把课程的职责导航误读成产品内部模块事实，并暗示每个产品都有独立 Harness。 | 将图拆成“用户 / 产品观察视角”和“课程工程职责视角”，明确虚线表示分析映射而非部署调用关系。 |
| `M4-F02` | `MINOR` | `READER_VALUE` | Section 4，Figure 2 | 标题和图内“术语确定性”容易形成高低排序感。 | 读者可能把稳定抽象理解成更正确，把生态相关或课程工作定义理解成较差、较不可靠。 | 改为“定义来源不同”，并明确稳定抽象不等于机制完整、生态相关不等于错误、课程定义不等于低价值。 |
| `M4-F03` | `MAJOR` | `COURSE` | Section 5，三个产品卡 | 产品卡只按“公开可以确认 / 不能因此推出”呈现，没有逐项执行开头的三问。 | 三问法没有从开头贯穿到实践，读者看完事实卡仍未必能迁移到陌生 CLI + Web Agent 产品。 | 三张卡统一改成“哪一层 / 定义来源 / 证据边界”，并增加一个陌生产品的迁移练习。 |
| `M4-F04` | `MINOR` | `EVIDENCE` | Section 3，Harness 段落 | 证据边界正确，但“有限样本不足以证明……”连续出现，研究报告语气较重。 | 容易让正文节奏停在证据声明上，也可能被读成在暗示一个更强的行业结论。 | 保留 `00-C06a=PARTIAL` 的全部限制，改成自然说明：只确认多个官方用法、含义不同、尚不足以给出统一行业定义。 |
| `M4-F05` | `MINOR` | `EVIDENCE` | Section 2、Section 5、Evidence `00-E10/E13/E14/E15` | Claude Code、Codex CLI、Copilot、DSH 属于版本敏感产品事实，正文和证据卡的检索日期停在 M3。 | M4 发布候选必须证明这些事实经过本轮官方来源复核；若入口迁移也应保留可追踪记录。 | 仅用官方一手资料做定向复核；事实不扩写，更新核验日期与必要的来源说明。 |
| `M4-F06` | `EDITORIAL` | `READER_VALUE` | Section 3—6 | 若干边界提醒和课程停止线重复表达。 | 重复会削弱 12—18 分钟导论的推进感，使文章更像研究记录。 | 合并重复句，保留三问、两张图、六段 Teaching Spine 和 Article 01 桥接。 |

- Finding Count：`BLOCKER 0 / MAJOR 2 / MINOR 3 / EDITORIAL 1`
- Revision Rule：只处理以上 Findings；不新增 Claim，不重写 Teaching Spine，不扩大 Article 00 范围。

### Finding Disposition

| Finding ID | Disposition | Revision |
|---|---|---|
| `M4-F01` | `RESOLVED` | Figure 1 已拆成“用户 / 产品观察视角”和“课程工程职责视角”，中间明确为课程分析映射；图注排除通用部署拓扑、固定调用顺序与独立 Harness 假设。 |
| `M4-F02` | `RESOLVED` | Figure 2 改为“术语的定义来源”，并补齐三项非等价说明。 |
| `M4-F03` | `RESOLVED` | Claude Code、Codex CLI、DeepSeek Harness 三张卡均显式执行“哪一层 / 定义来源 / 证据边界”，并加入陌生 CLI + Web 产品迁移练习。 |
| `M4-F04` | `RESOLVED` | Harness 段改为三个受证据约束的自然判断，未使用“没有行业标准”或“控制面”结论。 |
| `M4-F05` | `RESOLVED` | 仅以官方一手资料定向复核 Claude Code、Codex CLI、DeepSeek Harness、Microsoft / GitHub Copilot；更新相关 Evidence Card 日期与 Microsoft 稳定入口。 |
| `M4-F06` | `RESOLVED` | 合并 Model / Application、Product / Host / Runtime 与 Section 6 的重复说明；修订后正文未增长。 |

### Technical Review

- Reviewer：`Codex self-review`
- Date：`2026-08-19`
- Outcome：`PASS`
- Findings：Model / AI Application、Copilot / Agent / Agentic、Product / Host / Agent Runtime / Harness 与七个横向术语均保持导航级边界；没有把一次 LLM 调用写成 Agent，没有把 Agentic 写成等级，也没有暗示 Agent 必须具备 Memory / RAG / Skill。Figure 1 不再呈现标准五层架构。
- Disposition：Draft、glossary 与 Definition Matrix 已对齐。Harness 的两处历史强措辞已收窄为“现有证据不足以支持统一行业定义”；Agent Loop、Runtime 机制与 Harness 能力模型仍留给后续文章。

### Evidence Review

- Reviewer：`Codex self-review`
- Date：`2026-08-19`
- Outcome：`PASS_WITH_NOTES`
- Findings：Draft 仍使用既有 14 个 Claim 与 15 张 Evidence Card，`New Claims = 0`，核心 `BLOCKED = 0`。`00-C06a` 继续为 `PARTIAL`：正文只确认多个官方用法、含义不同、现有证据不足以支持统一行业定义。
- Disposition：已用官方一手来源定向复核 [Claude Code](https://code.claude.com/docs/en/overview)、[Codex CLI](https://learn.chatgpt.com/docs/codex/cli)、[DeepSeek Harness](https://github.com/deepseek-ai/deepseek-harness)、[Microsoft Copilot](https://www.microsoft.com/en-us/microsoft-copilot/for-individuals/get-copilot) 与 [GitHub Copilot](https://docs.github.com/en/copilot/get-started/features)。产品事实没有扩写；Evidence `00-E02/E08/E10/E13/E14/E15` 更新核验记录。

### Course Review

- Reviewer：`Codex self-review`
- Date：`2026-08-19`
- Outcome：`PASS`
- Findings：Reader Promise、六个主体 Section、两张图、五道 Learning Check 与 Article 01 桥接均保留。三问法形成“开头提出 -> Figure 2 辨来源 -> 产品卡实践 -> 结尾回收”的闭环。
- Disposition：没有提前展开 Agent Loop、Tool Runtime、Context / Memory / RAG 机制、Harness 能力模型、DSH 源码、Lab、BuildPilot 或 Article 01。

### Reader Value Review

- Reviewer：`Codex self-review`
- Date：`2026-08-19`
- Outcome：`PASS`
- Findings：读者可带走 Model ≠ Application、Application 不一定是 Agent、Copilot / Agent / Agentic 不是等级链、入口 ≠ Runtime、Harness 是显式课程抽象五个认知变化。长期模型集中在 Figure 1、Figure 2 与三问法。
- Disposition：新增未出现在产品卡中的“AI Copilot with agentic workflow + CLI / Web”迁移练习，读者可以按概念层、定义来源与证据边界分析陌生产品，而不依赖背诵正文。

### Formal Review Score

| Dimension | Score |
|---|---:|
| Technical Accuracy | `19 / 20` |
| Evidence Discipline | `19 / 20` |
| Teaching Quality | `18 / 20` |
| Engineering Transfer Value | `18 / 20` |
| Readability & Compression | `18 / 20` |
| **Total** | **`92 / 100`** |

- Threshold Check：总分 `>= 88`；Technical `>= 18`；Evidence `>= 18`；Teaching `>= 17`；Transfer `>= 17`，全部满足。

### Compression Check

- Draft Before：`8831 chars / 268 lines`
- Draft After：`8790 chars / 267 lines`
- Estimated Reading Time：`14—16 minutes`
- Compressed：Model / Application 类比、Copilot 判断、Product / Host / Runtime 重复限定、横向术语停止线与 Section 6 重复铺垫。
- Preserved：Teaching Spine、Reader Promise、六个主体 Section、两张图、三问法、五道 Learning Check 与 Article 01 桥接。

### Final Gate Decision

- Decision：`Article 00 is FINAL`
- Rationale：四类正式 Review 均为 `PASS / PASS_WITH_NOTES`，无 unresolved blocker；总分 `92 / 100` 达到发布候选阈值。所有首轮 Findings 已关闭，`00-C06a` 的 `PARTIAL` 被保留而未包装成确定事实。
- Remaining Notes：版本敏感产品事实只覆盖 `2026-08-19` 的官方公开资料；Harness 的行业统一定义仍未得到充分证据；当前两张图仍是 Draft 内 ASCII 版本，M4 未生成最终图片。
- Evidence Status：`PARTIAL`（保持真实状态）
- Next Allowed Action：`M5｜Article 00 Publish`，不自动执行。

## M4.1 Human Independent Review

- Lifecycle Trace：`FINAL -> REVIEW -> FINAL`
- Review Scope：仅处理人工独立 Review 指定的两个 `MAJOR` Finding；不重跑 M4，不新增研究、Claim、章节、Lab、DSH 源码或 BuildPilot 内容。
- Historical Note：上方 M1—M4 记录按当时 Review 结果保留；其中将 CLI / IDE / Web 等入口视为 Host、将 Model Call 视为共同技术依赖根的表述，由本节校正并取代为当前口径。

### Human Review Findings

| Finding ID | Severity | Status | Finding | Revision |
|---|---|---|---|---|
| `HR-F01` | `MAJOR` | `RESOLVED` | 旧稿把 CLI、IDE、Web、Desktop、CI、Unity Editor Integration 等公开入口直接等同 Host，混淆外部可观察 Surface 与内部承载职责。 | 新增一句 `Surface / Entry Point` 最小定义；Host 收窄为承载或集成 Agent 执行 / Agent Runtime 的宿主程序、进程或运行环境；Figure 1、产品卡、glossary、Definition Matrix 与学习检查统一明确 `Surface != Host`，且映射需要独立实现证据。 |
| `HR-F02` | `MAJOR` | `RESOLVED` | 旧稿把 Runtime、Tool、Context、Memory、Harness 等都写成最终依赖一次 Model Call，误把课程顺序写成运行时硬依赖。 | Section 6 明确这些能力可独立设计与测试、在 Agent 任务中协作；路线图标为 `Learning Dependency / Course Progression`，并声明 Model API 只是课程选择的最简单可观察学习起点，不是唯一技术依赖根。 |

### Targeted Consistency Verification

| Dimension | Outcome | Verification |
|---|---|---|
| Technical | `PASS` | Draft、Figure 1、三个产品卡、Article Card、Outline、glossary 与 Definition Matrix 均不再把 Surface 写成 Host；Section 6 不再建立 Model Call 硬依赖。 |
| Evidence | `PASS_WITH_NOTES` | 没有新增产品内部事实或 Core Claim；`00-C07 / 00-E10` 只做课程定义收窄；Evidence 总状态继续为 `PARTIAL`。 |
| Course | `PASS` | 六段 Teaching Spine 与 Article 01 桥接保留；路线图明确是课程推进，不是运行时依赖图。 |
| Reader | `PASS` | 只增加一句 Surface 定义，并在主图与产品卡应用；没有新增 Surface Architecture 章节或扩大阅读负担。 |

- New Core Claims：`0`
- Remaining Human Review Findings：`0`

### M4.1 Gate Decision

- Decision：`Article 00 is FINAL`
- Rationale：`HR-F01` 与 `HR-F02` 均已解决，四项定向一致性验证通过，无新 Claim、无新 Blocker；原 `00-C06a=PARTIAL` 证据边界保持不变。
- Evidence Status：`PARTIAL`
- Next Allowed Action：`M5｜Article 00 Publish`，不自动执行。
