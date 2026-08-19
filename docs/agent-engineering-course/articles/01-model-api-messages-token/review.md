# Article 01 Review Record

- Lifecycle Status：`PUBLISHED`
- Current Review Scope：`01-IR-F02 POST_PUBLICATION_HOTFIX / RECHECK_COMPLETE`
- Formal Review Status：`PASS`（历史 A5 记录，不代表 `01-IR-F02` recheck）
- Evidence Review Status：`PASS`（历史 A5 记录，不代表 `01-IR-F02` recheck）
- Course Review Status：`PASS`（历史 A5 记录，不代表 `01-IR-F02` recheck）
- Post-publication Recheck Status：`01-IR-F02 CLOSED / PASS`
- Review Date：`2026-08-19（Asia/Shanghai）`

## Gate History

### A1 Production Kickoff

- Outcome：`PASS`
- Disposition：Workspace、Article Card、11 个 Research Questions、12 个 Claim Skeleton、Scope Boundary 与 Job Competency Mapping 完整；未提前创建 Draft / Lab。

### A2 Evidence-first Research

- Outcome：`PASS`
- Disposition：12 张 Evidence Cards；`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`；OpenAI 主示例已由 Anthropic / Google 当前官方 contract 定向反查。

### A3 Detailed Outline

- Outcome：`PASS`
- Disposition：12 个 Claim 全覆盖；两张图、三个代码职责、Learning Check 与后文边界冻结。

### A4 Draft

- Outcome：`PASS`
- Disposition：完成 Single Model Call 正文；未新增无 Evidence 支撑的核心事实；进入 Formal Review。

## A5 First-pass Findings

> 本节先记录、后修改。以下 Finding 均基于未修订 Draft；修订处置见下一节。

| ID | Severity | Category | Finding | Required Disposition |
|---|---|---|---|---|
| `01-F01` | `MINOR` | `TECHNICAL / EVIDENCE` | C# 示例与当前 OpenAI 官方示例的核心类型一致，但遗漏官方示例中的 `#pragma warning disable OPENAI001`，复制时会出现实验性 API warning，影响“最小真实示例”的完整性。 | 增加 pragma，并在版本说明中保持 SDK surface 的时间边界；不扩写 SDK 教程。 |
| `01-F02` | `MINOR` | `TEACHING` | Single Model Call 图已区分 public API boundary 与 Provider 内部未知实现，但没有像任务要求那样在图中显式标出 Token / Context 是横跨 input 与 output 的容量边界。 | 在原图内增加一行横向 capacity 注记，不新增图或章节。 |
| `01-F03` | `MINOR` | `JOB_COMPETENCY / READER_VALUE` | Learning Check 能复述边界，但缺少“面对陌生 Provider SDK 现场识别各层”的最小迁移题，职业能力验证仍偏口头。 | 把第 1 题改成带陌生 Provider 伪代码的辨识题，并补齐参考思路。 |
| `01-F04` | `EDITORIAL` | `READABILITY / COMPRESSION` | 正文信息密度符合 M 篇，但原始 Markdown 约 10.4k 字符；Token 与 Response 两节存在少量重复强调“不是固定 schema / 换算”。 | 只删除重复句，不删工程边界，不新增主题；保留 12—16 分钟目标。 |

### First-pass Blocker Check

- `BLOCKER`：`0`
- `MAJOR`：`0`
- `MINOR`：`3`
- `EDITORIAL`：`1`
- Outcome：`PASS_WITH_NOTES`，允许在 A5 内执行最小修订后复审。

## Targeted Re-verification

- OpenAI Text generation 页面在 2026-08-19 再次核对：C# 示例仍使用 `OpenAI.Responses`、`ResponsesClient`、`ResponseResult`、`CreateResponseAsync`、`GetOutputText()`，并带 `#pragma warning disable OPENAI001`。
- OpenAI 同页仍并列 `POST /v1/responses` 的 model / input 请求，并提醒 output 是 item array，SDK 的 output text 是便利聚合。
- Anthropic Messages 与 Google GenerateContent 页面再次核对：两者当前 role / system instruction 位置仍能构成 Provider-specific counter-check。
- 复核没有发现需要扩大 Research 或将 Claim 降级为 `PARTIAL` 的变化。

## Revision Disposition

| Finding | Status | Revision |
|---|---|---|
| `01-F01` | `CLOSED` | 在 C# 示例加入官方当前示例使用的 `#pragma warning disable OPENAI001`；未引入 SDK 教程内容。 |
| `01-F02` | `CLOSED` | 在 Single Model Call 原图中加入 input / output Token 共享 model-specific Context Window 的横向注记。 |
| `01-F03` | `CLOSED` | 第 1 道 Learning Check 改为假想 `NovaClient` SDK 迁移辨识题，并给出分层参考思路。 |
| `01-F04` | `CLOSED` | 压缩 Token 段落的重复定义；复核正文约 3.3k CJK 字符、1.1k 空白分词单元，M 篇阅读负担可接受。 |

## Formal Review Score｜First Pass

| Dimension | Score | Rationale |
|---|---:|---|
| Technical Accuracy | `18/20` | 分层与版本事实准确；代码 warning 呈现需补齐。 |
| Evidence Discipline | `19/20` | 12 个 Claim 均可追踪，无 BLOCKED；Provider-specific contract 标注清楚。 |
| Teaching Quality | `17/20` | 主线与两张心智模型成立；Figure 1 的 Token / Context 横向边界需显式化。 |
| Engineering Transfer | `17/20` | Provider checklist 有效；Learning Check 仍需一个陌生 SDK 迁移题。 |
| Readability & Compression | `17/20` | 工程语言清楚，局部重复可压缩。 |
| **Total** | **`88/100`** | 达到最低总分，但三个 MINOR 未关闭前不能进入 FINAL。 |

## Second-pass Reviews

### Technical Review

- Outcome：`PASS`
- [x] Model / Provider / API / SDK / Application 没有混层
- [x] Messages / Current Context / Long-term Memory 没有混层
- [x] Token / Character / File Size / Context Window 没有混层
- [x] Streaming 被限制在 response delivery，而非 Hidden Reasoning
- [x] API failure、generation completion 与 application quality 分层
- [x] Provider-specific contract 没有包装成行业统一实现
- [x] C#、raw HTTP、图与正文职责一致

### Evidence Review

- Outcome：`PASS`
- [x] 12 个核心主张均可追踪到 Evidence Card
- [x] 11 个事实性 Claim 的措辞与 `CONFIRMED` 状态匹配
- [x] `01-C12` 明确作为课程 `PROPOSAL`，没有伪装成运行事实
- [x] 正文不依赖 `BLOCKED` 或 `PARTIAL` Claim
- [x] 版本敏感 Provider 事实已在 A5 定向复核
- [x] Provider 内部执行保持 unknown boundary

### Course / Reader Value / Job Competency Review

- Outcome：`PASS`
- [x] 文章停在 Single Model Call，没有提前吞掉 02 / 03 / 04 / 08 / 14—15 / 20—21
- [x] 开头从工程问题进入，结尾只桥接 Article 02
- [x] Single Model Call Map 与五层职责模型均可复述和迁移
- [x] `Messages != Memory` 成为后续 Memory 篇的明确预埋
- [x] Learning Check 覆盖陌生 Provider SDK 辨识与六项职业能力
- [x] 没有创建 Lab 或工程框架

## Formal Review Score｜Final

| Dimension | Score | Rationale |
|---|---:|---|
| Technical Accuracy | `19/20` | 分层、代码和失败轴准确；仍保留 Provider 演进的不确定性。 |
| Evidence Discipline | `19/20` | Claim → Evidence → Draft 可追踪，边界与版本日期明确。 |
| Teaching Quality | `18/20` | 问题、图、代码与抽象递进完整；密度适合基础核心篇。 |
| Engineering Transfer | `18/20` | Contract checklist 与陌生 SDK 迁移题可验证职业能力。 |
| Readability & Compression | `18/20` | 工程语言直接，表格与图承担压缩；保留必要边界说明。 |
| **Total** | **`92/100`** | 超过 FINAL 阈值，所有分项满足门禁。 |

## Final Gate

- [x] `BLOCKER = 0`
- [x] `MAJOR = 0`
- [x] `01-F01`—`01-F04` 全部关闭
- [x] Technical / Evidence / Course Review 均为 `PASS`
- [x] Total `92 >= 88`；Technical `19 >= 18`；Evidence `19 >= 18`；Teaching `18 >= 17`；Transfer `18 >= 17`

Outcome：`PASS`。Article 01 进入 `FINAL`，允许执行 A6 Publish；知识内容自此冻结。

## Post-publication Independent Review Hotfix

### 01-IR-F01｜OpenAI Responses input role 集合不完整

- Review Type：`INDEPENDENT_REVIEW`
- Review Date：`2026-08-19（Asia/Shanghai）`
- Severity：`MINOR`
- Category：`TECHNICAL / EVIDENCE`
- Lifecycle Treatment：`PUBLISHED + POST_PUBLICATION_HOTFIX`；未伪造完整生产生命周期重跑。
- Original Issue：Evidence、Research、Outline、Draft 与 Published Content 把 OpenAI 输入表达概括为 `developer / user / assistant`，容易被读成 Responses API 的完整 input message role 集合，遗漏当前 contract 中允许的 `system`。
- Evidence：OpenAI 官方 Responses API Reference 当前将 input message role 列为 `user / assistant / system / developer`；同一 contract 还提供顶层 `instructions`，可插入 system 或 developer instruction，字符串形式等价于 developer-role text input。
- Required Fix：补全 OpenAI Responses 的 input message role 集合，并只增加 `instructions` 的最低表示关系；不得扩写 Prompt hierarchy、instruction priority engineering 或 prompt design。
- Revision：已同步修订 `research.md`、`evidence.md`、`outline.md`、`draft.md` 与 Published Content；`01-C04` 的 Provider-specific 主张不变，`01-E04` 补充直接 API Reference 证据与证明边界。
- Recheck Result：`PASS`。上述资产均明确写为 OpenAI Responses 的当前 contract；未再把三种 role 写成完整集合，且未进入 Article 02 的 Prompt Engineering 范围。`hugo --gc --minify` 通过：Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`。
- Finding Status：`CLOSED`

### 01-IR-F02｜Anthropic role 表述遗漏 mid-conversation system 例外

- Review Type：`POST_PUBLICATION_HOTFIX REVISION_CANDIDATE`
- Source Finding：Article 02 Independent Review `02-F01`
- Review Date：`2026-08-19（Asia/Shanghai）`
- Severity：`MAJOR`
- Category：`COURSE / TECHNICAL / EVIDENCE`
- Lifecycle Treatment：保持 `PUBLISHED`，不伪造完整生产生命周期重跑。
- Original Issue：Research、Evidence、Outline、Draft 与 Published Content 把 Anthropic role 无条件概括为“input messages 使用 user / assistant；system 是顶层参数，不是 system role”。这只能作为 generic / conversation-start baseline，遗漏当前部分模型支持的 mid-conversation `role: system` 例外。
- Current Primary Evidence：[Anthropic Create a Message](https://platform.claude.com/docs/en/api/messages/create) 仍给出顶层 `system` / 常规 user-assistant baseline；[Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages) 当前列出 Claude Fable 5、Mythos 5、Opus 4.8、Opus 5、Sonnet 5 的 `role: system` 支持及 placement rules。
- Required Fix：把原绝对表述限定为 generic / top-level baseline，补入核对日期、model support 与 placement boundary；同步 Research / Evidence / Outline / Draft / Published Content；不建立跨 Provider 统一 role enum / hierarchy，不扩写 Prompt priority 教程。
- Revision Candidate：已按上述边界同步 `research.md`、`evidence.md`、`outline.md`、`draft.md` 与 Published Content；`01-E04` 增加 claim-relevant current official source、Proves / Does Not Prove 及 model / placement / version boundary。README 与本 Review 只记录 revision candidate。
- Evidence Impact：`01-C04` 仍为 `CONFIRMED`，但证明范围从绝对的 Anthropic role 集合改为 generic baseline + current model-specific exception；未新增核心 Claim，未改变课程主线。
- Build Verification：`NOT_RUN`（Revision Worker 本次任务明确禁止执行 Hugo）。
- Proposed Status：`READY_FOR_RECHECK`
- Recheck Result：`NOT_RUN`；只有 Independent Reviewer 可以把 Finding 标记为 `CLOSED` 或给出 Final decision。

#### Fresh Independent Reviewer Recheck｜内容与证据

- Reviewer Boundary：fresh independent review；只读取本次 Article 01 热修复 diff、Research / Evidence / Outline / Draft / Published Content 与当前 Anthropic 一手资料，不继承 Revision Worker 的隐藏推理。
- Primary-source Recheck：Anthropic 当前 [Create a Message](https://platform.claude.com/docs/en/api/messages/create) 仍把常规输入说明为 user / assistant messages，并把从会话开始生效的 system prompt 放在顶层 `system`；这支持 generic / conversation-start baseline，但不能单独支撑“messages 永远没有 system role”的全集结论。
- Feature Recheck：Anthropic 当前 [Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages) 明确列出 Claude Fable 5、Claude Mythos 5、Claude Opus 4.8、Claude Opus 5 与 Claude Sonnet 5 支持 messages 数组中的 `role: system`，且不需要 beta header。
- Placement Recheck：该 feature page 要求 system message 不能作为首条；必须紧跟 user turn，或紧跟一个以 server tool result 结束的 assistant turn；同时必须位于 messages 数组末尾，或紧接 assistant turn 之前，其他位置返回 400。热修复正文、`01-E04` 与 Outline 对这些位置边界的表述一致，没有放宽为任意位置。
- Asset Synchronization：`RQ-04`、`01-C04 / 01-E04`、Outline、Draft 与 Published Content 均保留 generic baseline + current model-specific exception + placement / version boundary；Draft body 与 Published body 字符级一致。旧的绝对表述只保留在 Original Issue 历史记录中，不再作为当前主张。
- Scope Recheck：未建立跨 Provider 固定 role enum 或完整 instruction hierarchy，也未扩写 Article 02 的 Prompt priority 教程；`01-C04` 可继续保持 `CONFIRMED`，证明范围以 `01-E04` 的 Does Not Prove / Limitations 为界。
- Build Evidence：内容复核后，Master 执行 `hugo --gc --minify`；Hugo `0.157.0`，`1230 Pages`，exit code `0`，输出无 `ERROR` / `WARNING`。
- Recheck Result：`PASS`。当前官方 contract、Claim / Evidence / Draft / Published 同步与 fresh Hugo build 均通过复核。
- Finding Status：`CLOSED`
