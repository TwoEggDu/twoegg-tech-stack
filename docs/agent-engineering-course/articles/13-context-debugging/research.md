# Article 13 Research｜Context Debugging

## Status

- Gate：`EVIDENCE_GATE / PASS`
- Research：`COMPLETE / FROZEN`
- Preliminary Evidence：`COMPLETE / FROZEN`
- Required Lab：`Lab 05 / OBSERVED / EVIDENCE_MERGED`
- Evidence Gate recommendation：`PASS / EVIDENCE_READY`
- Retrieved scope：`2026-08-22 / Asia/Shanghai`
- Boundary：本文件已完成 Research、Preliminary Evidence、Evidence Merge 与 Evidence Gate；merge 只读取 Lab 05 README observation summary，不重读 raw/source/tests/fixtures，也未调用真实模型。

## Scope and fixed boundary

继承 Article 12 Final Gate：`Context Snapshot` 是应用实际组装出的 application-visible 视图；`Context Receipt` 只能 describe / audit / compare 该 Snapshot。它不保证重建 Provider 内部最终输入、隐式变换或完整 token 序列。

下列均为 `COURSE PROPOSAL`，不是 Provider / SDK 官方 taxonomy：三层故障定位、八类诊断标签、Reconstruction Ladder、本篇 disposition / transform 字段与调试协议。Provider 事实仅在 source manifest 固定的 Provider / API / model / feature / version / retrieved-date 范围内成立。

## Research answers

### RQ1｜application-visible packing 的变换点

课程诊断面包括：candidate selection、scope filtering、precedence / ordering、representation、compression / summarization、budget fitting、materialization。每一步都应记录 contributor identity、scope、revision、pre/post digest、transform、disposition、reason 与 unknown。差异只证明应用侧 Snapshot 变化，不证明模型看到了相同字节，也不证明输出差异的因果。

### RQ2｜诊断 taxonomy

可形成非穷尽、非互斥的 `COURSE PROPOSAL`，但必须绑定 frozen predicate：

| Label | application-visible predicate | Boundary |
|---|---|---|
| `Missing` | required contributor 未进入 candidate / selected set，或 required field 缺失 | 先排除带 reason 的 intentional omission |
| `Stale` | revision / observed-at 落后于 frozen required revision / authoritative state | 历史版本可能正是任务要求 |
| `Wrong Scope` | contributor 存在但 tenant / user / task / step / environment / time scope 不匹配 | scope rule 必须事前冻结 |
| `Conflict` | in-scope contributors 对同一 key / decision 不兼容且未裁决 | 多来源不必然冲突 |
| `Pollution` | frozen relevance / trust policy 本应排除或降权的 obsolete、duplicate、out-of-scope、untrusted contributor 被纳入 | 不能从“回答不好”倒推污染 |
| `Overpacked` | 超过 frozen budget policy、侵占 reserve 或触发 documented trim / transform threshold | token 多本身不是质量失败 |
| `Compression Loss` | transform 前可验证的 required provenance / scope / uncertainty / conflict / ordering / negative evidence / locator，transform 后不可验证或语义降级 | Provider 文档只证明替换/摘要机制；具体 loss 需 Lab |
| `Truncation` | 应用或 Provider-documented capacity policy 丢 item，或 hard limit 失败/停止 | compaction 与 intentional omission 不得混记 |

同一事件可命中多个标签；Receipt 记录原子事实与多个 diagnosis，不强制单选。

### RQ3｜Assembly / Packing / Consumption

这是 `COURSE PROPOSAL`：

- `Assembly Failure`：candidate discovery、authority、scope、revision 或 conflict resolution 已使输入材料错误。
- `Packing Failure`：材料正确，但 ordering、representation、compression、budget、trim 或 materialization 使 Snapshot 偏离 contract。
- `Consumption Failure`：应用侧 Snapshot / Receipt 检查通过，结果仍不满足 frozen fixture contract；没有独立 runtime evidence 时，注意力、推理与 Provider 内因必须保持 `UNKNOWN`。

三层不能由最终回答质量单点判定。Lab 05 只验证 deterministic application-visible fixture，不证明真实模型消费质量。

### RQ4｜compression 风险

Provider 文档确认部分 feature 会以 summary、compaction item 或 placeholder 替换/省略先前内容；它们不证明某字段必然丢失。provenance、scope、uncertainty、conflict、ordering、negative evidence、locator 与 exact representation 因此是 `SOURCE-INFORMED ENGINEERING RISK` 和 Lab 05 pre/post comparison 的测试目标，而非已证实的 Provider loss。

### RQ5｜区分 omission、trim、Provider transform 与 limit

| Mechanism | Observable boundary | Scoped example |
|---|---|---|
| intentional omission | 应用按 frozen policy 在 materialization 前排除 contributor，并记录 reason | 课程应用逻辑 |
| application-side trim | 应用 transformer 修改 candidates / bytes，pre/post Snapshot 可比 | Lab 05 待验证 |
| provider truncation control | 公开 API parameter 与 response / error contract | OpenAI Responses `truncation=auto` 从会话开头丢 item；`disabled` 默认 400，见 `OAI-01` |
| provider-managed transform | 服务端压缩/清理并返回 documented marker / artifact | OpenAI compaction；Anthropic context editing / compaction，见 `OAI-02`、`ANT-03/04` |
| hard limit / stop | 特定 model/version 的 documented rejection / stop reason | Anthropic 行为随模型代际不同，见 `ANT-01` |

应用保存 history、提交 request、Provider-documented transformed view 与最终 internal input 是不同层；无法取得的层标 `UNKNOWN`。

### RQ6｜Receipt 与 reconstruction

`Reconstruction Ladder` 是 `COURSE PROPOSAL`，且各层前提独立：

| Level | Minimum prerequisite | Supports | Does not support |
|---|---|---|---|
| L0 metadata audit | identity / scope / revision / disposition / transform / digest | describe / compare fields | recover content |
| L1 app-visible bytes | retained immutable bytes，或 resolvable locator + retention + canonicalization | fixture-scoped byte reconstruction / equality check | digest 反推内容；Provider token stream |
| L2 semantic | L1-equivalent material + frozen parser / schema / invariant | fixture-scoped facts / conflict / unknown | 全语义等价 |
| L3 decision | frozen rule engine / inputs / policy version | deterministic application decision replay | 真实模型输出或内部推理 |
| L4 Provider-internal / full-token | Provider 提供可验证的完整接口与证据 | 本篇未满足 | Receipt 无此保证，保持 `UNKNOWN / UNSUPPORTED` |

正式主张仍只到 application-visible Snapshot 的 describe / audit / compare；L1-L3 等待 Lab 05，L4 不在可证范围。

### RQ7｜Lab 05 后续验证需求

Research Gate 冻结了 request / scope / revision / budget / canonicalization / transformer version 与 Cases A–G。后续 Lab 05 README observation summary 记录：offline deterministic fixture 完成真实 TDD RED/GREEN、mandatory A–G、fresh-process run A/B 与 direct-byte/SHA-256 compare；Provider/model/network/credentials 均为 NONE。Evidence Merge 只把这些 application-visible observation 映射回 Claim，不将它们外推到 Provider 或真实模型。

## Current primary source manifest

全部检索于 `2026-08-22 / Asia/Shanghai`；产品文档是 retrieved-date snapshot，论文只在其公开实验范围内成立。

| ID | Primary source | Fixed scope | Observation and boundary |
|---|---|---|---|
| `OAI-01` | OpenAI Responses create — https://developers.openai.com/api/reference/cli/resources/responses/methods/create | Provider=`OpenAI`; API=`POST /responses`; feature=`deprecated truncation control`; model=`request-selected`; docs retrieved 2026-08-22 | `auto` 超窗时从会话开头丢 item；`disabled` 默认返回 400。只证明文档契约，不证明请求已触发或内部 token 输入。 |
| `OAI-02` | OpenAI Compaction — https://developers.openai.com/api/docs/guides/compaction | Provider=`OpenAI`; API=`Responses`; model example=`gpt-5.3-codex`; feature=`context_management server-side compaction`; docs retrieved 2026-08-22 | 阈值后可出现 opaque encrypted compaction item，并在继续前 prune。示例不外推全部模型；opacity 不是 corruption 证据。 |
| `OAI-03` | OpenAI Compact response — https://developers.openai.com/api/reference/java/resources/responses/methods/compact | Provider=`OpenAI`; API=`POST /responses/compact`; feature=`compact response`; model=`request-selected`; docs retrieved 2026-08-22 | 返回 user messages 与 compaction item；不证明 summary 无损或可重建内部 token。 |
| `OAI-04` | OpenAI Token counting — https://developers.openai.com/api/docs/guides/token-counting ; https://developers.openai.com/api/reference/python/resources/responses/subresources/input_tokens/methods/count | Provider=`OpenAI`; API=`POST /responses/input_tokens`; model=`request-selected`; docs retrieved 2026-08-22 | model-scoped input count 可覆盖 tools / images / files 等 formatting cost；count 不是内容/provenance receipt。 |
| `OAI-05` | OpenAI Agents SDK Python Tracing — https://openai.github.io/openai-agents-python/tracing/ | Provider=`OpenAI`; SDK=`hosted current docs, package version not pinned`; feature=`tracing / generation span`; docs retrieved 2026-08-22 | span 可记录 model input/output；tracing 可关闭、ZDR 不可用、敏感内容可排除。因此 trace 可补充但不保证存在/完整。 |
| `ANT-01` | Anthropic Context windows — https://platform.claude.com/docs/en/build-with-claude/context-windows | Provider=`Anthropic`; API=`Messages`; models/overflow behavior=`as named by current page`; docs retrieved 2026-08-22 | request components 占 context；超窗 error / stop behavior 随模型代际与 feature 不同，不作跨模型外推。 |
| `ANT-02` | Anthropic Token counting — https://platform.claude.com/docs/en/build-with-claude/token-counting ; https://platform.claude.com/docs/en/api/messages/count_tokens | Provider=`Anthropic`; API=`POST /v1/messages/count_tokens`; model=`request-selected`; docs retrieved 2026-08-22 | 返回输入 token estimate，实际值可能略有差异且可含系统添加 token；count 不重建内容或内部 token。 |
| `ANT-03` | Anthropic Context editing — https://platform.claude.com/docs/en/build-with-claude/context-editing | Provider=`Anthropic`; API=`Messages beta`; header=`context-management-2025-06-27`; features=`clear_tool_uses_20250919 / clear_thinking_20251015`; models=`current page list`; docs retrieved 2026-08-22 | 服务端可在 prompt 到达 Claude 前清理 tool results / thinking，并可用 placeholder；client 可保留完整 history。只证明 transform，不证明课程 loss taxonomy。 |
| `ANT-04` | Anthropic Compaction — https://platform.claude.com/docs/en/build-with-claude/compaction | Provider=`Anthropic`; API=`Messages beta`; header=`compact-2026-01-12`; feature=`compact_20260112`; model example=`claude-opus-5`; docs retrieved 2026-08-22 | threshold 后生成 summary / block，后续丢弃 block 前 content 并从 summary 继续；不证明 summary 必然遗漏或语义/决策等价。 |
| `PAPER-01` | Liu et al., Lost in the Middle, TACL 2024 — https://aclanthology.org/2024.tacl-1.9/ | tasks=`multi-document QA / key-value retrieval`; models=`paper-listed 2023-era models` | 实验中表现随 relevant position 改变；不代表 2026 当前模型、全部任务或生产系统。 |
| `PAPER-02` | Shi et al., Irrelevant Context, ICML 2023 — https://proceedings.mlr.press/v202/shi23a.html | benchmark=`GSM-IC`; models/prompting=`paper-listed` | 特定受控实验中 irrelevant context 降低准确率；不证明所有额外 context 有害。 |

## Claim Inventory

| ID | Core claim | Final primary status | Local Lab support | Maximum scope |
|---|---|---|---|---|
| `13-C01` | 三层 application-visible diagnosis 分开，Provider 内因保持 UNKNOWN | `PROPOSAL` | `PARTIAL / A-D,F-G APPLICATION-VISIBLE` | course diagnostic design；不证明真实 consumption cause |
| `13-C02` | 八类标签是带 observable predicate 的非互斥、非穷尽 taxonomy | `PROPOSAL` | `PARTIAL / MANDATORY A-G CASE COVERAGE` | course taxonomy；Missing/Wrong Scope/Overpacked optional variants 未由 summary 记录为执行 |
| `13-C03` | more context 不是可靠性保证；相关性、位置、任务、模型会影响测试结果 | `CONFIRMED` | `NOT_APPLICABLE / NO LAB UPGRADE` | current-source + cited paper test scope only |
| `13-C04` | truncation、compaction、context editing、hard limit 是不同且 versioned 的机制 | `CONFIRMED` | `NOT_APPLICABLE / NO PROVIDER CALL` | fixed Provider/API/model/feature/retrieved-date docs only |
| `13-C05` | `BAD_COMPRESSOR_V1` 在 `lab05-fixture-v1` 把 `EV-1 SUPPORTED + EV-2 CONTRADICTS + UNKNOWN` 压成 `Root cause confirmed.`，verifier 检出 uncertainty/conflict/provenance/claim-strength loss | `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED` | `CONFIRMED / CASE E` | local deterministic fault only；非 Provider compaction / model-quality claim |
| `13-C06` | omission、app trim、provider truncation/transform、hard limit 应分开落账 | `PROPOSAL` | `PARTIAL / CASE F LOCAL DISPOSITION` | course observability contract；V4/Provider events 未由 Lab 验证 |
| `13-C07` | Receipt 只保证 app-visible describe/audit/compare，不保证 Provider/full-token reconstruction | `PROPOSAL` | `CONFIRMED / A,F,G LOCAL CONFORMANCE` | application-visible fixture only |
| `13-C08` | metadata/bytes/semantic/decision/Provider-internal reconstruction 各有独立前提 | `PROPOSAL` | `PARTIAL / G VALIDATES L0-vs-L1 CEILING` | ladder remains course design；L2/L3 未完整验证，L4 UNKNOWN/UNSUPPORTED |
| `13-C09` | 调试协议需冻结 scope/version/policy、比较 pre/post、保留 unknown、deterministic 回归 | `PROPOSAL` | `CONFIRMED / TDD+A-G+RUN-A/B CONFORMANCE` | deterministic local protocol only |

## Counter-evidence register

| ID | Counter-evidence | Consequence |
|---|---|---|
| `CE-01` | 相关且正确组织的新增 context 可能有帮助；论文只给有限反例 | 只写“不是保证”，不写“越多越差” |
| `CE-02` | 论文模型/任务早于当前产品面 | 不宣称 2026 当前模型有相同降幅 |
| `CE-03` | compaction 目标是保留关键状态，文档未证明 summary 必然错误 | loss 保持 `PARTIAL`；opacity 不等于 corruption |
| `CE-04` | token-count API 改善预算观测 | count 仍不是内容/provenance/full-token receipt |
| `CE-05` | Anthropic client 可保留完整 history | storage loss 与 provider-visible transform 分层 |
| `CE-06` | digest 可验证已保留 bytes 的 equality | digest 不能反推出遗失内容 |
| `CE-07` | Snapshot diff 与 output diff 不证明因果 | Lab 只做 deterministic local fixture，不做模型 claim |
| `CE-08` | trace 在启用且未删敏时可补充 input/output | 不假定 trace 稳定存在或完整 |

## Final Evidence Merge and Lab dependency

current-source manifest 与 counter-evidence 继续冻结。Lab 05 README summary 已按 `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 合并：final primary status 为 `3 CONFIRMED / 6 PROPOSAL / 0 PARTIAL / 0 BLOCKED`。`13-C03/04` 未被 Lab 升级；`13-C05` 只升级为 `BAD_COMPRESSOR_V1 / lab05-fixture-v1` 范围的 `CONFIRMED`；course taxonomy、protocol、Receipt 与 reconstruction ladder 仍是 `PROPOSAL`，其 local conformance 另列，不把 design status 改写成 Provider/industry fact。Evidence Gate 已完成 `9 / 9` Claim audit，decision=`PASS`，next allowed gate=`OUTLINE`。

## Stop line

未宣称真实模型效果、注意力或 Provider 内因；未覆盖 Article 14 Working Memory 或 Articles 15–16 Memory / RAG；Lab 05 只提供 offline deterministic application-visible evidence，未改变 Article 12 Receipt ceiling。Provider-internal/full-token reconstruction 保持 `UNKNOWN / UNSUPPORTED`。
