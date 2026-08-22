# Article 13 Detailed Outline｜Context Debugging：Packing、Compression、Pollution 与可重建性

> Gate：`OUTLINE / FROZEN CANDIDATE`
>
> Article type：`CASE / DIAGNOSTIC`。本文件是 Draft 的结构合同，不是最终正文。

## 0. Frozen teaching contract

- 核心判断：`同一 Prompt 不等于同一 Context；先冻结失败 Step 的 application-visible Snapshot，再沿 Assembly / Packing 的可观察变换定位，证据不足就停在 UNKNOWN。`
- Article 12 回答 `What should this Step see?`，建立 Select / Order / Scope / Fit Budget、Context Snapshot 与 Receipt。
- Article 13 回答：view 错误、过期、越界、冲突、污染、压缩或截断时，怎样找 failure layer；证据只允许 audit、某级 reconstruction，还是必须 `UNKNOWN`。
- 核心 case：`Prompt bug != Context bug`。Prompt 是任务合同；Context bug 是本 Step 实际可见材料偏离合同。两者可能共存，不能由最终回答单点判定。
- Required rhythm：`具体问题 -> 直觉 -> failure cases -> mechanism -> Lab -> Engineering / Evidence Boundary -> learning check -> shortest conclusion`。

| 正文段落 | 目的 | 预计强调 |
|---|---|---:|
| 具体问题 | 同一 Prompt、昨天 `CS0103`、今天 `build succeeded` | 8% |
| 直觉 | 从改 Prompt 切到比较 Step view | 7% |
| Failure cases | 至少七种故障架构作为 teaching spine | 18% |
| Mechanism | distortion chain、三层与八类 taxonomy | 15% |
| 调试协议 | 可执行、可停止、可回归 | 14% |
| Lab 05 | A–G Observation、RED/GREEN、repeatability | 20% |
| Engineering / Evidence Boundary | 工程落法、L0–L4、9/9 Claim ceiling | 15% |
| Learning check + conclusion | 检查迁移能力并最短收口 | 3% |

### Figures / tables

1. `Figure 13-1`：相同 Prompt / Step contract，左右比较两份 Snapshot 的 contributor revision、conflict、transform、budget、omitted set；不画模型脑内过程。
2. `Figure 13-2`：packing distortion chain，并叠加 Assembly / Packing；Snapshot 下游只标 `Consumption candidate / internal UNKNOWN`。
3. `Table 13-1`：failure architecture、observable predicate、首查 artifact、Lab 锚点。
4. `Figure 13-3`：Lab 05 offline fixture -> Runtime artifacts -> independent verifier；边框标 Provider / model / network / credentials=`NONE`。
5. `Table 13-2`：Reconstruction Ladder L0–L4。
6. `Table 13-3`：9 / 9 Claim traceability、exact maximum wording 与 forbidden overclaim。

## 1. 具体问题：同一句 Prompt，昨天是 `CS0103`，今天却说构建成功

**Section purpose / emphasis：8%**

- 第一屏给项目故障，不先讲 taxonomy、Provider 文档或限制。
- 冻结任务合同示例：`只根据本次构建证据定位第一个可行动失败点；无法确认时写 UNKNOWN；不要修改项目。`
- 昨天：当前日志含 Unity `CS0103`，State / source revision 对齐，Step 定位变量未定义。
- 今天：Prompt digest 未变，输出却是 `build succeeded`。
- 场景后必须原样出现：**“先不要改 Prompt，先看这个 Step 当时看到了什么。”**
- Figure 13-1 展示：今天可能纳入旧成功摘要、漏掉 current Evidence、混入两个 build job 的冲突，或 packing 时裁掉 required Evidence。

**Teaching move**

- `prompt_digest 相同` 只说明任务文本可比，不说明 contributor set、revision、scope、order、representation、budget、transform 或最终 token 相同。
- 先冻结 `run_id / step_id / workflow_state_revision / task contract`，再找两份 Snapshot / Receipt 的第一处分叉。
- Prompt 合同正确但 current Evidence 未进入、State 过期、冲突被折叠或 Evidence 被裁掉，先进入 Context diagnosis；Snapshot 合规但任务合同含糊，才回到 Prompt bug。

**Claim / evidence / examples**

- Claims：`13-C01`, `13-C07`, `13-C09`；Article 12 Snapshot / Receipt；Lab `A/B/D/F`。
- 示例只作教学组合，不冒充模型实验。

**Forbidden overclaims**

- 不由输出漂移断言模型或 Provider 变化。
- 不把“Prompt 相同”写成“模型输入 token 相同”。

## 2. 直觉：模型面对的是打包后的 view，不是项目的全部真相

**Section purpose / emphasis：7%**

- 用最小桥接把 Article 12 的“应该看什么”推进到 Article 13 的“哪里失真”。

**Teaching move**

- Prompt bug：目标、约束、失败语义或输出合同表达错误。
- Context bug：合同可能正确，但本 Step 的来源、revision、scope、冲突、representation、budget 或 materialization 偏离。
- Consumption candidate：应用侧 Snapshot / Receipt 通过冻结检查，但 deterministic contract 仍失败；没有独立 runtime evidence 时，注意力、推理与 Provider 内因保持 `UNKNOWN`。
- 过渡句职责：Article 12 设计装配单；Article 13 找装配单与成品 view 的第一处分叉。

**Examples / refs**

- 同一句 `build succeeded` 可来自 current authoritative build、obsolete Plan、unrelated history 或冲突的一侧；文本相同，provenance 与 diagnosis 不同。
- Claims：`13-C01`, `13-C03`, `13-C07`。

**Forbidden overclaims**

- 不写“答案差一定是 Context bug”。
- C03 只写“更多 context 不是通用可靠性保证”，不写“越多越差”。

## 3. Failure cases：九种故障架构，先看差异再贴标签

**Section purpose / emphasis：18%**

- 用至少七种具体 failure architecture 做全文 teaching spine；A 是 Lab 控制组，不算故障。
- 每例按 `frozen expectation -> observable delta -> diagnosis candidate -> remaining UNKNOWN` 四拍写。

| ID | 具体架构 | 第一可观察差异 | 标签 / Lab 锚点 |
|---|---|---|---|
| `FA-01` | current `CS0103` Evidence / State 根本没进入 | required ID 缺失且无 omission reason | `Missing`；optional V1 未执行 |
| `FA-02` | Goal=`rev17`，State summary=`rev14` | required vs source revision mismatch | `Stale`；Lab B |
| `FA-03` | history 中旧 tool schema 被当成当前 Stage 能力 | registry version 或 tenant/task/step/environment/time scope 错 | `Stale + Wrong Scope`；V2 未执行 |
| `FA-04` | build 4310 排查装入 build 4291 日志 / 旧源码 | build/source revision 与 frozen request 不符 | `Stale`，可与 Conflict 共现 |
| `FA-05` | obsolete Plan、old tool result、unrelated history、duplicate / untrusted material 被纳入 | 违反 frozen relevance / trust policy | `Pollution`，必要时 `Overpacked`；Lab C，V3 未执行 |
| `FA-06` | `build failed` 与 `build succeeded` 被静默选边 | 同一 conflict key 有两份 in-scope provenance，无 frozen resolution | `Conflict`；Lab D |
| `FA-07` | `SUPPORTED + CONTRADICTS + UNKNOWN` 压成确定根因 | pre/post uncertainty、conflict、provenance、claim strength 不守 invariant | `Compression Loss`；仅 Lab E / `BAD_COMPRESSOR_V1` |
| `FA-08` | optional 未先删、reserve 被吞、required Evidence 静默裁掉 | budget ledger / disposition / explicit failure 缺失 | `Overpacked / Truncation`；Lab F |
| `FA-09` | Receipt 尚在，original bytes / locator 已丢 | L0 metadata 可审计，L1 bytes 前提不成立 | reconstruction boundary；Lab G |

**易混对照**

- Missing vs intentional omission：后者必须有 frozen policy、disposition、reason。
- Pollution vs merely long：token 多本身不命中；需违反 relevance/trust 或 budget/reserve threshold。
- Compression vs truncation：前者改变 representation 并查 pre/post invariant；后者丢 item 或触发 capacity policy；两者可共现。

**Claims / forbidden overclaims**

- Claims：`C01/C02/C03/C05/C06/C08`；Lab `B–G`。
- 不从回答不好倒推 Pollution、Compression Loss 或 Consumption Failure。
- 九种架构不是新 taxonomy；正式 taxonomy 仍是八类、非互斥、非穷尽的 `COURSE PROPOSAL`。
- `V1 Missing / V2 Wrong Scope / V3 Overpacked / V4 Event separation` 不得写成已观察。

## 4. Mechanism：沿 packing distortion chain 找第一处分叉

**Section purpose / emphasis：15%**

- 建立引擎 / Provider 无关的可观察模型，避免退化成 API 清单。

```text
candidate sources
  -> selection
  -> scope filtering
  -> precedence / ordering
  -> representation
  -> compression / summarization
  -> budget fitting
  -> request materialization
  -> application-visible Context Snapshot
```

- Figure 13-2 在箭头旁放 candidate IDs、source/version、scope decision、order、pre/post bytes/digest、transform version、budget/reserve、Snapshot digest。
- 图注必须说明：这是课程审查 application-visible assembly / packing 的诊断面，不是 Provider 内部统一 pipeline。

### 4.1 Assembly / Packing / Consumption（COURSE PROPOSAL）

| Layer | Observable predicate | 典型 case | 合法下一步 |
|---|---|---|---|
| Assembly Failure | candidate discovery、authority、scope、revision、conflict resolution 已使材料违约 | Missing、Stale、Wrong Scope、Pollution selection、unresolved Conflict | 修 source / registry / scope / revision / conflict policy |
| Packing Failure | pre-transform 材料正确，但 order、representation、compression、budget、trim、materialization 使 Snapshot 违约 | Lab E/F | 修 transformer / ordering / budget / fail-closed contract |
| Consumption candidate | application-visible Snapshot / Receipt 通过，deterministic contract 仍失败 | Lab 未测试真实消费 | 增加独立 runtime/eval evidence；否则 UNKNOWN |

### 4.2 八类标签（COURSE PROPOSAL）

| Label | Frozen minimum predicate | 反误判边界 |
|---|---|---|
| Missing | required contributor 未进 candidate / selected，或 required field 缺失 | intentional omission 有 reason，不算 Missing |
| Stale | revision / observed-at 落后于 frozen required / authoritative state | 历史版本可能正是 task scope |
| Wrong Scope | tenant/user/task/step/environment/time scope 不匹配 | scope rule 事前冻结 |
| Conflict | in-scope contributors 对同一 key 不兼容且未裁决 | 多来源不自动等于冲突 |
| Pollution | obsolete/duplicate/out-of-scope/untrusted item 违反 frozen relevance/trust policy仍被纳入 | 不从答案质量倒推 |
| Overpacked | 超预算、侵占 reserve 或触发 local threshold | token 多本身不是质量失败 |
| Compression Loss | required provenance/scope/uncertainty/conflict/ordering/negative evidence/locator 在 pre 有、post 不可验证或被强化 | Provider docs 不证明具体字段必丢 |
| Truncation | application 或 Provider-documented capacity policy 丢 item，或 hard limit 失败/停止 | 与 compaction、intentional omission 分账 |

### 4.3 Atomic event record

- 建议字段：`actor/stage/mechanism/control-or-version/contributor/source-revision/scope/authority/disposition/reason/pre-digest/post-digest/budget/output-reserve/unknowns`。
- 同一 event 允许多个 diagnosis refs。
- intentional omission、app trim、Provider-documented transform/truncation、hard limit 分开；C06 是课程 design，Lab 只观察 Case F local dispositions。

**Refs / forbidden overclaims**

- Claims：`C01/C02/C04/C05/C06`；Evidence `E01/E02/E04-E06`；Lab `D-F`。
- 不把三层 / 八标签称为行业或 Provider 标准。
- 不把 truncation、compaction、context editing、hard limit 合并；不因 opaque 称 corruption。

## 5. 可执行调试协议：冻结、比较、分层、重建、回归

**Section purpose / emphasis：14%**

- 把 Snapshot / Receipt diff 变成可复跑步骤，并让 UNKNOWN 成为合法停止结果。

### Inputs

- Step identity；Prompt digest；Goal；required IDs/revisions/scopes；authority/trust/conflict policy；packer/compressor/budget version；output reserve；Provider/API/model/feature/retrieved-date scope。
- candidate list、pre-transform view、Snapshot、Receipt、transform event、budget ledger、omitted set、trace refs、unknowns。

### Eight-step protocol

1. Freeze failing Step；确认同一 task / Step contract，Prompt digest 只是一个字段。
2. 建 known-good Snapshot / Receipt control（Lab A），不用“好回答”代替 artifact。
3. 先 diff source、revision、scope、authority、trust、disposition、order，查 Missing/Stale/Wrong Scope/Pollution/Conflict。
4. 再走 packing chain，diff representation、pre/post digest、transform version、budget/reserve、materialization。
5. 只按 frozen predicate 多标签诊断；记录 actor/stage/mechanism/reason，不从回答倒推。
6. 有证据才落 Assembly / Packing；都通过时仅记 consumption candidate，内部仍 UNKNOWN。
7. 只爬到前提满足的 Reconstruction level；缺 bytes/locator/parser/rules 就停。
8. 只修第一处分叉，保存原 failure，以 frozen input 重跑 normalized Snapshot/Receipt/verdict；真实模型另做独立 eval。

### Output / stop conditions

```text
failing_step_identity
control_snapshot_ref / failing_snapshot_ref
first_divergence_stage
atomic_observations[] / diagnoses[]
failure_layer = ASSEMBLY | PACKING | CONSUMPTION_CANDIDATE | UNKNOWN
highest_reconstruction_level
unsupported_claims[]
repair_scope / regression_refs
```

- 无 revision/scope baseline：不能判 Stale/Wrong Scope。
- 无 pre bytes/invariant：不能判具体 Compression Loss。
- 只有 output diff：不能判 Consumption / Provider cause。
- 只有 digest 且 bytes/locator 丢失：可 audit，不可 byte reconstruction。
- required Evidence 放不下：explicit fail closed，不生成 silent Snapshot。

**Refs / forbidden overclaims**

- Claims：`C01/C02/C06-C09`；`E09/LE01/LE03/LE04`。
- 协议是 course design，在 frozen local fixture 有 scoped conformance；不称 universal production best practice。
- Snapshot diff 与 output diff 不构成因果证明。

## 6. Lab 05：固定夹具里的真实 Observation

**Section purpose / emphasis：20%**

- Expected 与 Observed 分离；完整保留真实 RED、GREEN、fault injection 与 repeatability。

### 6.1 Fixed scope

- `lab05-fixture-v1`；C# / .NET SDK `10.0.301` / Runtime `10.0.9` / `net10.0` / BCL-only。
- Windows 10 build 19045 / X64 / win-x64 / China Standard Time +08:00。
- Provider / model / network / credentials=`NONE / NONE / NONE / NONE`。
- `BAD_COMPRESSOR_V1` 是 named local fault seam，不模拟 Provider algorithm。
- Case F 是 deterministic integer budget units，不是 Provider tokens / billing / real output limit。

### 6.2 Genuine TDD and retained failures

1. Release shell build 成功后 mandatory RED：Spec exit=`1`、Runtime exit=`3`，A–G `7/7` 因缺行为失败。
2. 第一次实现后 build 出现 `CS0411` 三处 `JsonValue.Create` method-group failure；保存原始失败，以 explicit lambda 恢复。
3. Final build=`0 warnings / 0 errors`；GREEN exit=`0`、`15/15`，RED 后 Spec / fixture bytes 未削弱。
4. 保留并正确分类 non-escalated `helper_unknown_error`、初始 timestamp gap、invalid PowerShell `ReadOnlySpan<byte>` audit helper；它们是已恢复 tooling failures，不是行为 RED。

### 6.3 Exact A–G observations

| Case | Exact observation | Maximum local use |
|---|---|---|
| A | `GOOD_CONTEXT`；required contributors、ordering ledger、output reserve 保留 | local baseline internally consistent |
| B | `STALE + REVISION_MISMATCH`；`expected=rev17`, `actual=rev14`，source ref / provenance 保留 | 只确认该 revision mismatch detector |
| C | `POLLUTION`；识别 `C-OLD-TOOL`, `C-OBSOLETE-PLAN`, `C-UNRELATED-HISTORY` | frozen relevance predicate；无模型质量 claim |
| D | `CONFLICT_UNRESOLVED`；保留 `Build failed.` / `Build succeeded.` 与 `build-job-41/42` provenance，不选边 | 显式冲突保存 / 检出 |
| E | `BAD_COMPRESSOR_V1` exact output=`Root cause confirmed.`；检出 `UNCERTAINTY/CONFLICT/PROVENANCE/CLAIM_STRENGTH` loss | 仅 named compressor + exact fixture |
| F | optional history 先删；P0/P1 与 four output units 保留；overflow=`REQUIRED_EVIDENCE_BUDGET_EXCEEDED/FAIL_CLOSED`，Snapshot=`ABSENT` | local budget/disposition/fail-closed conformance |
| G | metadata=`AUDITABLE`；bytes=`NOT_RECONSTRUCTABLE`；`ORIGINAL_BYTES_ABSENT/LOCATOR_UNRESOLVABLE/DIGEST_NOT_CONTENT`；Provider internal=`UNKNOWN_UNSUPPORTED` | `AUDITABLE != RECONSTRUCTABLE` local boundary |

### 6.4 Repeatability

- run A/B 是 fresh processes；每个 manifest 58 normalized files。
- compare 59 files（含 manifest / Spec result）：relative set、length、direct bytes、per-file SHA-256、aggregate 全相同。
- aggregate=`621cde0ec3c1ed7b7f6334dac4d0187b522625f37e220957e48b6dcac3bd3f50`。
- closure restore/build/GREEN/runA/verifyA/runB/verifyB/compare=`8/8 exit 0`。

**Refs / forbidden overclaims**

- `13-LE01`—`13-LE04`; Lab README 14.1–14.5, 15, 16, 18。
- 不写真实模型 accuracy/hallucination/attention/context rot/Provider internals。
- two-run / one-host 不等于 production、cross-platform、distributed 或 universal determinism。
- Missing/Wrong Scope/Overpacked/V4 未执行。

## 7. Engineering / Evidence Boundary

**Section purpose / emphasis：15%**

- 全文在此集中处理工程取舍、证据等级、Provider scope、Receipt ceiling 与 non-scope；避免 disclaimer-first。

### 7.1 Engineering moves

- 版本化 Step identity、required revision/scope、authority/trust/conflict policy、packer/compressor/budget policy。
- 保存 pre/post digest；要 L1，保存 immutable bytes 或 resolvable locator + retention + canonicalization。
- omission、app trim、Provider-documented transform/truncation、hard limit 原子分账。
- conflict / UNKNOWN / negative evidence / locator / provenance / claim strength 进入 invariant。
- output reserve first；optional-first；required overflow explicit fail closed。
- deterministic regression fixture 保留 RED/fault/GREEN/fresh-process compare；它不替代真实模型 eval。

### 7.2 Reconstruction Ladder（COURSE PROPOSAL）

| Level | Minimum prerequisite | Supports | Stop boundary |
|---|---|---|---|
| L0 metadata audit | identity/scope/revision/disposition/transform/digest | describe/audit/compare fields | 缺字段=`NOT_AUDITABLE/UNKNOWN` |
| L1 app-visible bytes | immutable bytes，或 locator+retention+canonicalization | fixture-scoped bytes/equality | digest 不反推内容；G=`NOT_RECONSTRUCTABLE` |
| L2 semantic | L1-equivalent material + frozen parser/schema/invariant | fixture facts/conflict/unknown | 不承诺完整 semantic equivalence |
| L3 decision | frozen rule engine/inputs/policy version | deterministic app decision replay | 不重放真实模型；Lab 未完整验证 |
| L4 Provider-internal/full-token | Provider 完整可验证接口与证据 | 本篇未满足 | `UNKNOWN/UNSUPPORTED` |

- Level 前提独立，不因有 Receipt 自动获得 L1–L3。
- G 是 negative boundary；若另存 bytes / locator，L1 verdict 可以不同。

### 7.3 Receipt ceiling

- 固定句义：Receipt 只 `describe / audit / compare application-visible Context Snapshot`。
- Receipt 不是 Provider request trace、complete effective Context、hidden system text、reasoning trace、final token sequence 或 full-token replay。
- token count 不是 content/provenance receipt；enabled/unredacted trace 可补充，但不保证存在或完整。

### 7.4 Claim traceability：9 / 9 exact ceilings

| Claim | Status | Section / refs | Exact maximum wording | Forbidden overclaim |
|---|---|---|---|---|
| C01 | `PROPOSAL`; local `PARTIAL` | 2/4/5；`E01/LE01/LE03` | “课程把 Assembly/Packing/Consumption 分层；fixture 能定位若干应用侧差异，模型/Provider 内因仍 UNKNOWN。” | 不断言真实 consumption cause |
| C02 | `PROPOSAL`; local `PARTIAL` | 3/4/6；`E02/LE01-03` | “这是 COURSE PROPOSAL；Lab 只验证 mandatory A-G 的 frozen predicates。” | 不称穷尽、单选、Provider taxonomy；variants 未执行 |
| C03 | `CONFIRMED / CURRENT-SOURCE TEST-SCOPED` | 2/3/7；`E03` | “更多 context 不是通用可靠性保证”；限 TACL 2024 paper-listed multi-doc QA/key-value、ICML 2023 GSM-IC 与固定 source scope。 | 不写“越多越差”、2026 当前模型相同降幅或普遍因果 |
| C04 | `CONFIRMED / CURRENT PRODUCT-DOC SCOPE` | 4/7；`E04`, manifest `OAI-01-03/ANT-01/03/04` | 只按 2026-08-22 manifest 的 Provider/API/model/feature/version/retrieved-date 陈述 truncation、compaction、context editing、hard limit 差异。 | 不写生产 request 已触发、跨模型一致或文档永久稳定 |
| C05 | `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED` | 3/4/6；`E05/LE02` | “在 lab05-fixture-v1，BAD_COMPRESSOR_V1 的确定结论被检出 uncertainty/conflict/provenance/claim-strength loss。” | 不映射 Provider compaction、summary quality、模型 accuracy/hallucination |
| C06 | `PROPOSAL`; local `PARTIAL` | 4/5/7；`E06/LE03` | “课程建议分开落账；Lab 只确认 Case F local dispositions。” | 不声称 V4 / Provider event schema 已验证 |
| C07 | `PROPOSAL`; local `CONFIRMED` | 1/5/6/7；`E07/LE03` | “该 fixture Receipt 可 describe/audit/compare app-visible Snapshot；不保证 Provider/full-token reconstruction。” | 不写 full effective Context / token replay guarantee |
| C08 | `PROPOSAL`; local `PARTIAL` | 3/5/6/7；`E08/LE03` | “G 证明 digest metadata 可 audit 但不能恢复 bytes；L2/L3 未完整证明，L4 UNKNOWN/UNSUPPORTED。” | 不称行业标准、自动递进、L1-L3 已完整实现 |
| C09 | `PROPOSAL`; local `CONFIRMED` | 5/6/7；`E09/LE01/LE04` | “frozen offline protocol 在同一 Windows/.NET fixture 可重复；不外推生产/跨平台。” | 不称 universal best practice / production determinism |

### 7.5 Consolidated non-scope

- 不教 Prompt Workshop、“请更认真”或 Prompt 改写；Prompt bug 只作对照。
- 不证明真实模型 accuracy、hallucination、attention、reasoning、context rot 或输出差异因果。
- 不证明 Provider 内部 truncation algorithm、hidden Context 或 complete token sequence；C04 只在 fixed manifest scope。
- 不把 `BAD_COMPRESSOR_V1` 外推为 Provider fault。
- 不把 Lab fixed fixture 外推 production/cross-provider/cross-platform/large-scale/distributed/security/multi-tenant。
- 不展开 Article 14 Working Memory lifecycle/mutation/persistence；不展开 Articles 15–16 Long-term/Project Memory、Vector DB、Embedding、Retriever、Reranker、RAG。

## 8. Learning outcomes / check

**Section purpose / emphasis：2%**

读完应能：

1. 冻结 Step identity，以 Prompt digest + Snapshot/Receipt 区分 Prompt bug 与 Context bug。
2. 检查 source/revision/scope/authority/trust/disposition/order/omitted set/unknown。
3. 沿 packing chain 找第一处分叉，收窄到 Assembly/Packing/Consumption candidate/UNKNOWN。
4. 用 predicate 区分 stale/pollution/conflict/compression/budget-truncation，并知道未执行 variants 的边界。
5. 选择最高可证 Reconstruction level；缺 bytes/locator 时写 `AUDITABLE != RECONSTRUCTABLE`。
6. 设计 genuine RED/GREEN、fault injection、fail-closed、repeatability fixture，不冒充模型实验。

### Learning Check

1. Prompt 未变，昨天有 current `CS0103`，今天只有旧成功摘要：先冻结什么，为什么不能先改 Prompt？
2. summary 删除 `UNKNOWN`、revision、conflict 后写 `Root cause confirmed.`：哪些 invariant 失守，能否据此指控 Provider？
3. Receipt 只有 ref/digest/order，source/locator 已丢：为什么 L0 可 audit、L1 不成立？
4. required Evidence 超 usable input 但系统仍返回 silent Snapshot：属于哪层，正确 disposition 是什么？

参考判据只给 reviewer / draft：Step + revision diff；four loss dimensions + fixture scope；digest-not-content；Packing + explicit fail closed。

## 9. 最短结论

**Section purpose / emphasis：1%**

`先不要改 Prompt；先冻结失败 Step 的 application-visible Snapshot，沿 Assembly / Packing 逐层比较，证据不足就停在 UNKNOWN。`
