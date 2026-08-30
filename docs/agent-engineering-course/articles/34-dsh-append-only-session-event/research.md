# Article 34 Research

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## 1. Research boundary

本篇是原理型源码追踪文。工程问题不是“怎样重新打开一段聊天”，而是：一条 Session event stream 怎样被写入、读取并投影成不同视图；Replay、Resume、Fork 与 Compaction 分别改变什么；哪些事实能够从记录重建，哪些权限、预算和外部副作用绝不能靠“复制会话”推断出来。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Research date：`2026-08-30 / Asia/Shanghai`
- Article type：原理篇；先建立 event stream / projection 模型，再落到 pinned DSH，不写成事件 API 清单。

最终证据边界：

- `repository-map.md` 与 `call-path.md` 已闭合 complete pinned event table、write/read path、四投影源码路径以及 Replay/Resume/Fork/Compaction owners。
- `experiments/session-replay-resume-fork-trace.md` 已执行 6 个 file executions、`12 passed / 122 skipped / 0 failed`，fixture 结束后保持 clean pinned commit。
- `Durable Event` 不等于 `Live Event`；`Transcript` 不等于 `Model History`；`Replay` 不保证再次调用模型能得到相同输出；`Fork` 不复制 external world。
- `permission`、`approval`、`budget` 是否继承必须由 pinned source 或 trace 逐项证明；缺字段或缺路径时结论必须写成“未证明”，不能采用安全性乐观默认值。
- Compaction 必须回答“追加新事实还是改写旧事实”，并单独追踪 Evidence / unverified 标记是否仍可审计。
- BuildPilot 的 `IContextContributor + Receipt` 只允许作为 Part VII 候选接口，本篇不声称已实现，也不启动 Article 38。

## 2. Research Questions

| RQ | Question | Required closure |
|---|---|---|
| `34-RQ01` | Durable Event 与 Live Event 的 owner、生命周期和可恢复性边界是什么？ | event table + producer/consumer paths |
| `34-RQ02` | event type、`seq` 以及 Run/Turn/Step/tool-call correlation 怎样表达？ | exact type/field table；字段缺失也要记录 |
| `34-RQ03` | 一条 Session event 从 append 到持久化，再到读取和订阅的 write/read path 怎样闭合？ | exact File/Symbol/Call Path；不得只列 type |
| `34-RQ04` | 同一 stream 怎样形成 Model History、UI Transcript、Domain State、Trace 四种 projection？ | projection matrix + source consumers + runtime snapshot |
| `34-RQ05` | Replay 重放的是记录、projection 还是模型决策？ | no-provider replay trace；明确“不保证相同模型输出” |
| `34-RQ06` | Resume 是在原 Session 上继续追加，还是新建执行上下文？ | identity/seq/history trace + lifecycle path |
| `34-RQ07` | Fork 从哪个 event boundary 分叉，parent/child 各继承哪些记录？ | parent/child before/after stream diff |
| `34-RQ08` | external world、permission/approval 与 budget/cost 在 Replay、Resume、Fork 中分别如何处理？ | inheritance matrix；无证据项保持 BLOCKED |
| `34-RQ09` | Compaction 是 append 还是 rewrite；被压缩 history、Evidence 与 unverified 状态怎样保留？ | before/after raw stream + projection diff |
| `34-RQ10` | 能否从 pinned stream 重建 Model History，再 Fork 出隔离分支？ | deterministic owner-fixture test + invariant assertions |

## 3. Preliminary Claim Register

| Claim ID | Preliminary claim | Class | Status | Required closure |
|---|---|---|---|---|
| `34-C01` | 全篇绑定 clean official fixture 的 frozen tag/commit。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | identity + post-run cleanliness receipts |
| `34-C02` | Durable Session Event 与 Live Event 是不同证据通道；live acceptance 先于 async durable completion，通知不是 durability receipt。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | append/coordinator paths + X01 |
| `34-C03` | `SessionEvent` envelope 是 `{type,seq,time,data}`；seq 为 Session 内单调序号；Turn/Step/call correlation 在 payload，core 无 universal Run id。 | `PINNED_SOURCE` | `CONFIRMED` | complete generated catalog + bounded absence |
| `34-C04` | `Session.append` 以 `log.length` 分配 seq 并先 live commit；durable backend 要求 contiguous append，flush 才是 durability barrier。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | write path + X01 |
| `34-C05` | append/persist/load/follow/read/projection 的 write/read paths 已闭合到 storage 与 consumer。 | `PINNED_SOURCE` | `CONFIRMED` | repository map + call path |
| `34-C06` | Model History、UI/history transport、Domain State 与 raw Trace 是不同 projection；X01 运行覆盖 History/Domain/raw-live，UI/Trace 仅 source-confirmed。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | UI/Trace independent runtime snapshots absent |
| `34-C07` | Transcript 不等于 Model History：append-origin transcript 与 current surface 的 selection/replace 规则不同；独立 UI runtime snapshot 未执行。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | source closed; UI runtime gap retained |
| `34-C08` | Replay 从 accepted prefix 重建相等 History，不调用模型或 Tool；不保证新模型采样输出相同。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | Session.create/deriveMessages + X02 |
| `34-C09` | Resume 保留同一 Session id/full prefix，并以 contiguous seq 追加新 Turn/History。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | AgentLoop.resume + X03 |
| `34-C10` | Fork 在 completed boundary 创建新 id/lineage、detached frozen prefix 与隔离后缀，parent 不被 child 修改。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | core/Host fork + X04 |
| `34-C11` | Fork 只复制事件 prefix；不提供 external-world snapshot/rollback/clone。X04 仅证明内存图隔离，未执行真实副作用。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | production external world excluded |
| `34-C12` | `delegationDepth` 明确随 resume 持久化；generic permission/credential/cost/turn-budget inheritance 在 pinned source 中缺席或未证明。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | bounded absence; no authorization/billing runtime |
| `34-C13` | Compaction 追加 log-only transaction facts与 replacement `user/message`；原 raw events 保留，Model History surface 被替换。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | compaction path + X05 |
| `34-C14` | generic `verified/unverified` 不在 SessionEvent contract；compaction provenance link 不等于 Evidence verification preservation。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | absence confirmed; domain semantics not representable |
| `34-C15` | BuildPilot 可候选使用 `IContextContributor` 产出带 provenance 的 `Receipt`，但这只是未来设计提案。 | `DESIGN_PROPOSAL` | `PROPOSAL` | Part VII ADR/design review only |

Final distribution：`9 CONFIRMED / 5 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。

## 4. Source-investigation result

Source Investigator 已输出并由本轮 Evidence Merge 复核：

1. **Event table**：每个 exact type 的 durable/live 分类、producer owner、seq 分配点、Session/Run/Turn/Step/call correlation、payload、storage、consumer；同名或相似事件不能合并猜测。
2. **Write path**：Host/Agent/Tool/Compaction producer 到 append API、序号分配、持久化 backend、live emission 的完整路径；标出“先 durable 后 live”或其他真实顺序。
3. **Read path**：load/iterate/subscribe/replay/resume/fork 的入口与过滤、排序、起点/终点语义；标出谁处理缺口、重复和未知事件。
4. **Projection matrix**：Model History、UI Transcript、Domain State、Trace 分别消费哪些 types/fields，执行哪些 filter/transform，以及可否从 durable prefix 独立重建。
5. **Inheritance matrix**：Replay、Resume、Fork 对 session identity、event prefix、seq、history、live subscribers、permission/approval、budget/cost、external world 的 `inherit / reset / reference / absent / unknown`。
6. **Compaction path**：触发条件、写入类型、原事件是否仍在 storage、History 如何替换/注入摘要、Evidence/unverified 是否有结构化保留字段。
7. **Counter-evidence search**：查找 in-place update/delete、seq reuse、projection 直接消费 live-only event、fork mutates parent、自动复制权限/预算/外部状态等反例；没有命中只可写 bounded absence。

结果：complete generated event catalog、append/persistence/query/projection paths、`AgentLoop.resume`、core/Host fork 与 compaction replacement path 均已闭合。Core envelope 无 universal Run id；generic permission/cost-budget 与 verified/unverified contract 缺席，均按 bounded absence 收窄。

## 5. Frozen required experiment design

Lab Dependency: `REQUIRED`

本节是 Article-specific trace contract，不创建新的课程 Lab。Lab Engineer 可以补充执行细节，但不得修改 hypothesis、falsifier、acceptance 或用“测试通过”替代 raw event/projection evidence。

### Common fixture and safety

- Fixture：clean pinned checkout；优先复用 repository-owned in-memory storage、MockAdapter 与 deterministic tools；禁止真实 Provider、credential、network、生产数据和费用。
- Required capture：raw durable events（type、seq、payload identity/correlation）、live notifications、四类 normalized projections、parent/child/session identity、provider/tool invocation counts、permission/budget snapshots、exit code/test counts。
- Ordering：用 deferred promise/latch 等确定性握手，不用随机 sleep；若 owner fixture 缺少必要观测点，先记录 exact gap，再用最小临时 instrumentation 并保存 diff。
- Oracle：断言基于 pinned source 定义的 event identity 和 projection，不以 UI 文本“看起来一样”作为 Replay/Resume/Fork 成功。
- Safety boundary：外部世界只用 in-memory sentinel 表示；它验证“未被 event copy 克隆”，不证明生产 Tool 的幂等性、事务性或回滚。

### `34-X01`｜Durable/live ordering and four projections

- Related Claims：`34-C02—C06`。
- Research Question：一条含 user、assistant、tool call/result 与 lifecycle boundary 的 Session 怎样同时产生 durable/live 观测和四种 projection？
- Hypothesis：durable stream 有可排序、无重复的 event identity；live notifications 可与其关联但不承担恢复真相；四种 projection 从同一 durable prefix 得到不同 normalized outputs。
- What Would Falsify It：缺少 total-order key、同一 identity payload 冲突、任一 projection 只能靠未持久化 live event 重建，或 Transcript 与 History 被错误断言为同一序列。
- Inputs：deterministic two-Step interaction；一个只读 Tool 返回结构化结果；固定 clock/id provider（若 repository owner fixture 支持）。
- Expected Observable：event table 中的 representative types 均出现；`seq`/identity 有序；durable/live 对照可联结；四份 projection snapshot 均保存 provenance/transform 说明。
- Acceptance：raw stream 无 gap/duplicate；write/read trace 闭合；History、Transcript、Domain State、Trace 至少有一项结构差异且都可解释；provider/tool count 与脚本一致。
- Evidence Mapping：满足后可升级 `34-C02/C04/C06`；`34-C03/C05` 仍需 source map 共同闭合。
- Observed Result：`PASS`；live boundary order、durable types/usage、History/tool correlation 与 Domain projection watermark 均通过；UI Transcript/SessionQuery Trace 无独立 runtime snapshot。

### `34-X02`｜Replay from pinned stream and History reconstruction

- Related Claims：`34-C06—C08`。
- Research Question：只给定 X01 的 durable prefix，能否不再次调用 Provider/Tool 重建 Model History 与其他 projection？
- Hypothesis：Replay 读取已记录事实并重建可比较 projection；Provider/Tool invocation count 保持 `0`；重建的 History 与原 History 等价，但不要求 Transcript 与 History 相等。
- What Would Falsify It：Replay 必须重新调用模型/Tool、重建结果依赖 live-only state、event 顺序改变、call/result correlation 丢失，或把“再次生成相同模型文本”当作 acceptance。
- Inputs：X01 保存的 pinned durable stream；fresh in-memory readers/projections；禁用 Provider/Tool body。
- Expected Observable：fresh projections 与 X01 normalized snapshots 对比；History equality、Transcript intentional difference、zero invocation receipts。
- Acceptance：History/Domain/Trace 按各自 contract 重建；Transcript 按其 selector 重建而不被要求等同 History；Provider/Tool count `0`；未知/不支持字段形成显式 gap。
- Evidence Mapping：满足后可升级 `34-C07/C08` 的 fixture-scoped 部分。
- Limitation guard：Replay 成功不证明重新采样模型会产生相同输出。
- Observed Result：`PASS`；accepted prefix 重建出相等 History，继承 event-type prefix 后追加 `session/end-seed`；无 model-resampling guarantee。

### `34-X03`｜Resume append and inheritance boundary

- Related Claims：`34-C09`、`34-C12`。
- Research Question：从已持久化 prefix 恢复后继续输入，identity、seq、History、permission/approval 与 budget/cost 怎样变化？
- Hypothesis：Resume 不改写既有 prefix；新事实按 source-defined identity/seq 继续；History 来自 durable prefix；权限与预算只按明确 owner 的规则 inherit/reset，不由测试猜测。
- What Would Falsify It：既有 event payload 被修改、seq 冲突/倒退、恢复依赖旧 live subscriber、权限或预算在没有 source rule 时静默继承。
- Inputs：持久化到中间 boundary 的 X01 stream；销毁原 reader/runtime；用 fresh runtime resume 后发送一个 deterministic followup。
- Expected Observable：before/after raw stream diff、old-prefix byte/semantic equality、new suffix、fresh live subscription、History request diff、permission/budget before/after receipt。
- Acceptance：parent prefix unchanged；new suffix 可排序；恢复后的 request 能追溯到 durable History；permission/budget 每项有 observed value 或明确 `ABSENT/UNKNOWN`，不得脑补。
- Evidence Mapping：满足后可升级 `34-C09/C12` 的实际支持范围。
- Observed Result：`PASS`；同 Session id、prefix 保留、seq 连续、Turn `[1] -> [1,2]`、History `3 -> 5`；仅 `delegationDepth` 有明确 budget-like inheritance。

### `34-X04`｜Fork isolation from reconstructed History

- Related Claims：`34-C10—C12`。
- Research Question：能否从指定 event boundary 重建 History 并 Fork，使 child 产生独立后缀而 parent 与 external-world sentinel 不被复制或回滚？
- Hypothesis：Fork 共用或复制 source-defined immutable prefix、拥有独立 child identity/后缀；child 输入不修改 parent；event copy 不复制 external-world sentinel；permission/budget 只按 explicit rule 处理。
- What Would Falsify It：child append 出现在 parent、parent projection 随 child 改变、child 自动得到未声明 external state、重复执行历史 Tool side effect，或权限/预算静默扩大。
- Inputs：X01 pinned stream + chosen fork boundary；一个曾递增 external sentinel 的 deterministic Tool record；fork 后 parent/child 发送不同 followup。
- Expected Observable：prefix relation、parent/child stream diff、两份 History/request snapshot、Tool body invocation count、external sentinel identity/value、permission/budget receipts。
- Acceptance：fork 前 projection 可重建；parent immutable；child suffix isolated；历史 Tool 不重跑；external sentinel 未被克隆/回滚；权限/预算无 silent elevation。
- Evidence Mapping：满足后可升级 `34-C10/C11/C12` 的 fixture-scoped部分。
- Limitation guard：in-memory sentinel 只证明 event-copy boundary，不证明真实远端系统状态。
- Observed Result：`PASS`；child detached/frozen、earlier-boundary History、parent unchanged、cold Host fork 不 resume source；真实 external side effect 未执行。

### `34-X05`｜Compaction append-vs-rewrite and provenance

- Related Claims：`34-C07`、`34-C13`、`34-C14`。
- Research Question：触发 Compaction 后 raw stream 与 Model History 怎样变化，Evidence/unverified provenance 是否仍可审计？
- Hypothesis：实际 source behavior 可由 before/after raw stream 判定为 append、rewrite 或混合；History projection 可能改变，但 durable evidence 的保留/丢失必须被逐字段记录。
- What Would Falsify It：只截图压缩后的 prompt、不保存原 stream；用 token 数下降推断 append/rewrite；Evidence/unverified 丢失却仍宣布“完整可恢复”。
- Inputs：包含 verified evidence、unverified observation、tool call/result 与足够 history 的 deterministic stream；使用 source-defined compaction trigger/entry。
- Expected Observable：raw event diff、compaction event/payload、History/Transcript/Trace before-after、provenance field matrix、Provider invocation count（若摘要需要模型则显式记录）。
- Acceptance：append/rewrite/mixed 有直接 raw proof；旧事实是否仍在 storage 可判定；Evidence/unverified 每项标 `preserved / transformed / lost / not representable`；不得把摘要等同原证据。
- Evidence Mapping：满足后可升级 `34-C13/C14`；若 provenance 无结构支持，则以限制结论而非乐观推断收口。
- Observed Result：`PASS`；原事件留在 raw stream，compaction facts 与 replacement checkpoint 追加；generic verified/unverified 不可由该 contract 表达。

## 6. Evidence Gate recommendation

`PASS / OUTLINE ELIGIBLE`。

15 Claims/Cards 最终为 `9 CONFIRMED / 5 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。核心 Event/History/Replay/Resume/Fork/Compaction 路径与 owner-test 已闭合；正文必须保留 UI/Trace runtime、真实 external world、generic permission/cost budget 与 verified/unverified 的限制。
