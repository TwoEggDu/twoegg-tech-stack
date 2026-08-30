# Article 34 Outline｜Append-only Session Event：Replay、Resume、Fork 与 Projection

Status: `AUTHOR OUTLINE / REVIEW PENDING`

## Article identity

- Type：原理篇。
- Series position：承接 Article 33 的 Turn / Step 主链，回答一次运行结束后记录如何恢复、继续与分叉；Article 35 才展开完整 Tool Pipeline，Article 36 才集中处理 Cost / Compaction / Recovery。
- Core problem：同一条 Session event stream 怎样同时支撑 Model History、UI Transcript、Domain State 与 raw Trace，而不把 Replay、Resume、Fork、Compaction 混成“恢复聊天”。
- Shortest claim：Event stream 记录已经发生的事实，Projection 决定当前怎样读取；Replay、Resume 与 Fork 的差异，必须用 identity、prefix、suffix 与 external-world boundary 描述。
- Evidence posture：15 Claims / 15 Cards；9 CONFIRMED / 5 PARTIAL / 1 PROPOSAL / 0 BLOCKED；5/5 required experiments；12 passed / 122 skipped / 0 failed。

## Navigation

- Previous：Article 33 published relref。
- Course index：Agent Engineering series index relref。
- Next：Article 35 只写计划中文本，不创建 future relref。

## Opening｜“恢复聊天”为什么不是工程定义

- 用同一条记录的三个动作开场：Replay、Resume、Fork。
- 问：它们是否会再次调用模型、是否保留 Session id、是否继续 seq、是否复制外部副作用。
- 固定 source/tag/commit、fixture cleanliness 与 runtime scope。
- 亮出 9C/5P/1P/0B 和 12 passed tests。
- 明确无真实 Provider/network/permission/billing/external side effect。

## 1. 问题空间｜一份聊天记录为何不够

- 聊天文本遗漏 request、tool correlation、policy、compaction、lineage 等事实。
- Live notification 不是 durable receipt。
- Transcript 不是 Model History。
- “恢复成功”无法区分读旧记录、原 Session 续写、新 Session 分叉。

## 2. 抽象模型｜Event Stream + Projection

- Event envelope：identity / order / time / typed payload。
- Durable stream：恢复真相来源。
- Live channel：低延迟通知，不能独立承担恢复。
- Projection：`P(stream prefix) -> view`。
- 四种 projection：History、Transcript、Domain、Trace。
- stable fact 与 current surface 分离。

## 3. Pinned DSH event contract

- `SessionEvent<T> = {type, seq, time, data}`。
- `seq` 是 Session 内序号；core 无 universal runId。
- Turn/Step/tool-call correlation 在 payload。
- event family 表：lifecycle、model surface、request/trace、compaction、policy/control、domain/team/extension。
- unknown types fail closed，catalog 仅对 pinned build 完整。

## 4. Write path｜先 live acceptance，再 async durable completion

- producer -> `Session.append`。
- `seq = log.length`、freeze/validate、live in-memory commit。
- publish `session/event` -> write-behind -> backend contiguous append。
- `session/flush` 才是 durability barrier。
- observer failure 不回滚 accepted event。

## 5. Read path｜读取记录与形成视图是两件事

- load/prepare/follow/read 的入口和 source selection。
- prepared durable vs live source。
- contiguous prefix、cursor、gap-free frames。
- replay validation 不发布 live notification。

## 6. 四种 Projection

- Model History：current surface；只保留模型需要的消息。
- UI Transcript：append-origin history transport；面向人类追踪过程。
- Domain State：registered pure fold + watermark。
- raw Trace：header + contiguous raw events + optional projections。
- 对比表写 inputs / transform / output / authority / runtime evidence。
- UI/Trace 独立 runtime snapshot 未执行，保持 PARTIAL。

## 7. Transcript 为什么不等于 History

- compaction replacement 让 current model surface shadow old nodes。
- 原 append-origin events 仍可作为 transcript/trace 材料。
- History equality 不能代替 Transcript equality。
- X01 只覆盖 History、Domain、raw/live representative observations。

## 8. Replay｜从 accepted prefix 重建，不是重新演算世界

- `Session.create(seed) -> validate/fold -> deriveMessages`。
- 不调用 Provider/Tool；X02 重建 equal History。
- Replay 的对象是 recorded prefix 与 projection。
- 明确不保证再次模型采样输出相同。

## 9. Resume｜同一 identity 上继续追加

- `AgentLoop.resume -> persistence.prepare -> store.prepare`。
- 保留 Session id 与 full prefix。
- 新事件按 contiguous seq 形成 suffix。
- X03：Turn `[1] -> [1,2]`、History `3 -> 5`。
- fresh live subscription；旧 prefix 不改写。

## 10. Fork｜从 completed boundary 创建新 lineage

- selected immutable prefix -> new child id/header。
- detached/frozen seed，child suffix 与 parent 隔离。
- earlier-boundary History reconstruction。
- cold Host fork 不必 resume source。
- X04 parent unchanged、historical Tool 不重跑。

## 11. Replay / Resume / Fork 对比矩阵

- identity、prefix、suffix、seq、History、live subscriber、model/tool invocation、external world、permission/budget。
- `inherit / reset / reconstruct / absent / unknown` 逐格表达。
- 不允许用“复制会话”概括三者。

## 12. External world 与继承边界

- event prefix 不等于 process/files/remote transaction snapshot。
- Fork 不复制或回滚 external world。
- in-memory sentinel 只证明 event-copy boundary。
- historical approval/permission events 不是当前 authorization token。
- `delegationDepth` 有 durable contract；generic cost/turn budget 未证明。

## 13. Compaction｜raw stream append，History surface replace

- append `compaction/start|summary|prune|end` log-only facts。
- replacement `user/message` 使用 `surfaceOp: replace` 与 `sourceEventSeqs`。
- old raw events remain；future History current surface changes。
- 用 before/after 双视图解释 append vs rewrite。

## 14. Verified / unverified 为什么不能从 provenance 猜

- generic `verified` 不在 pinned `SessionEvent` contract。
- `sourceEventSeqs` 是 provenance link，不是 verification semantics。
- compaction summary 不等于原始证据。
- Evidence semantics 应由 domain schema/receipt 独立表达。

## 15. 从 stream 重建 History，再安全 Fork

- Step 1 冻结 durable prefix 与 identity。
- Step 2 validate contiguous seq / known types / completed boundary。
- Step 3 fresh projector 重建 History。
- Step 4 new child identity + lineage + detached prefix。
- Step 5 分别追加 parent/child suffix，比较 isolation。
- Step 6 单独列 external/permission/budget receipts。
- 给伪代码与验收 invariant。

## 16. 五组实验与 12 passed tests

- X01 Durable/live + projection。
- X02 Replay equal History / zero re-execution。
- X03 Resume identity/prefix/seq continuity。
- X04 Fork isolation / cold Host fork。
- X05 Compaction append + surface replacement。
- 6 file executions / 12 passed / 122 skipped / 0 failed。
- 不把 selected tests 外推到真实 Provider/external world。

## 17. 一个更稳的工程骨架

- `EventEnvelope`、`DurabilityReceipt`、`ProjectionSnapshot`、`ForkReceipt`。
- Projection 记录 input cursor、transform version、output digest。
- Replay/Resume/Fork 使用 typed receipt，不使用一个 `restore()`。
- live notification 明确非 durable。
- external authority 独立重新获取或验证。

## 18. BuildPilot implication｜仅 Proposal

- 候选 `IContextContributor` 产出带 provenance 的 `Receipt`。
- Receipt 不自动等于 durable SessionEvent，也不照搬 DSH。
- 需要 Part VII ADR/design review 才能决定 ownership/schema/migration。
- Article 35 与 Part VII 均未启动。

## 19. Evidence Boundary

- Source：exact pinned ownership/call path/event contract。
- Runtime：repo-owned deterministic fixtures selected behaviors。
- PARTIAL：UI/Trace independent runtime、real external world、generic permission/cost inheritance、verified semantics。
- Proposal：BuildPilot interface only。
- Absence 是 pinned bounded search，不是跨版本永恒断言。

## 20. Claim mapping and learning check

- 15-row compact mapping。
- 18 个问题覆盖 event/projection、write/read、Replay/Resume/Fork、compaction、inheritance 与 limits。

## 21. Series handoff and shortest conclusion

- Article 35 Tool Pipeline 未启动，不在本篇预写。
- Article 36 才集中处理 Cost / Compaction / Recovery 的更大闭环。
- Article 38 / Part VII NOT STARTED。
- Final sentence：先保存可排序事实，再生成带 provenance 的视图；Replay、Resume 与 Fork 只在明确的 identity、prefix、suffix 和外部边界下才有工程意义。
