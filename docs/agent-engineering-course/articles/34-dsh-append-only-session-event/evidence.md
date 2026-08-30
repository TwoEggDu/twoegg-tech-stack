# Article 34 Evidence

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## Evidence summary

- Frozen revision：`dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Claim/Card count：`15 / 15`
- Final distribution：`9 CONFIRMED / 5 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Source artifacts：`repository-map.md`、`call-path.md`
- Runtime artifact：`experiments/session-replay-resume-fork-trace.md`
- Selected owner tests：`6 file executions / 12 passed / 122 skipped / 0 failed`
- Current Gate recommendation：`PASS / OUTLINE ELIGIBLE`

### Evidence 34-E01｜Pinned identity

- Claim / Status / Class：`34-C01 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source：official fixture `deepseek-ai/deepseek-harness@cd5ef8148158c3a752a658978873241fdf8e2bbc`，exact tag `dsh-v0.1.2-alpha.1`。
- Observation：post-run `HEAD` unchanged；`git status --short` 与 `git diff --stat` 无输出。
- Proves：本篇 source/runtime 输入身份与本轮 cleanliness。
- Does Not Prove：任何 event behavior 本身。

### Evidence 34-E02｜Durable Event vs Live Event

- Claim / Status / Class：`34-C02 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source Location / Call Path：`core/session/src/index.ts:Session.append -> session/event -> session-persistence/coordinator.ts:installWritePath -> backend.append -> session/flush`。
- Observation：append 先冻结并写 live log/publish；write-behind 后 durable append；observer failure 不回滚 live acceptance，flush 才是 durability barrier。X01 观察 boundary live order。
- Counter-evidence：通知即 durability receipt 被源码顺序否定。
- Does Not Prove：process crash durability 或跨节点一致性。

### Evidence 34-E03｜Event type, sequence and correlation

- Claim / Status / Class：`34-C03 / CONFIRMED / PINNED_SOURCE`
- Source：`core/session/src/types.ts:SessionEvent`；`known-event-types.ts` generated fail-closed catalog。
- Observation：envelope `{type,seq,time,data}`；seq 在 Session 内单调；Turn/Step/tool call correlation 位于 payload；core 无 universal Run id，plugin `run-*` 不可泛化。
- Proves：complete build-scoped vocabulary 与 correlation boundary。
- Limitation：downstream registration deferred，结论限定 pinned build。

### Evidence 34-E04｜Ordering owner and append semantics

- Claim / Status / Class：`34-C04 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source：`Session.append` assigns `seq=log.length`；persistence requires contiguous append。
- Trace：X01 durable log 含 inbox/turn/user/assistant/end，live boundary `turn/start -> step/start -> step/end -> turn/end`。
- Proves：pinned owner-fixture 的 seq/order 与 live-before-durable boundary。
- Does Not Prove：distributed total order。

### Evidence 34-E05｜Write/read path closure

- Claim / Status / Class：`34-C05 / CONFIRMED / PINNED_SOURCE`
- Source paths：append/persistence coordinator；`Session.create/deriveMessages`；`SessionHistoryController.page/follow`；`SessionProjectionRegistry`；`SessionQueryEngine.readSession/observeSession`。
- Observation：write、durable load、live follow、surface fold、domain fold 与 raw query 均闭合到明确 owner/consumer。
- Proves：pinned production paths；不是仅凭 type name 推断。
- Does Not Prove：每条 path 都在本轮 runtime 被执行。

### Evidence 34-E06｜Four projections

- Claim / Status / Class：`34-C06 / PARTIAL / PINNED_SOURCE + EXPERIMENT`
- Source：Surface/deriveMessages、HistoryController、ProjectionRegistry、SessionQueryEngine。
- Trace：X01/X02 runtime 覆盖 raw/live、Model History、tool correlation 与 Domain State watermark。
- Proves：四者源码 selection/transform 不同；History/Domain/raw-live 有 runtime observation。
- Gap：UI Transcript 与 SessionQuery Trace 未做独立 runtime snapshot，故不升级为 full runtime confirmation。

### Evidence 34-E07｜Transcript is not Model History

- Claim / Status / Class：`34-C07 / PARTIAL / PINNED_SOURCE + EXPERIMENT`
- Source：`surface.ts` 中 append-origin messages 保留 transcript material；Model History 只读 current surface，replacement shadow old nodes。
- Trace：tool round-trip 的 request 2 从 recorded tool result 构建 History；X05 证明 replacement 改变 surface、raw old event 仍在。
- Proves：pinned source 上 Transcript 与 History 不是同一 projection。
- Gap：selected baseline 没有独立 UI transcript snapshot。

### Evidence 34-E08｜Replay and History reconstruction

- Claim / Status / Class：`34-C08 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Path：stored/copied events -> `Session.create(seed)` -> validation/surface fold -> `deriveMessages()`。
- Trace：X02 reconstructed History exactly equals original；event-type prefix equal，child adds `session/end-seed`。
- Proves：accepted prefix 的 fixture-scoped reconstruction；path 不执行 model/Tool。
- Does Not Prove：重新采样 nondeterministic model 会得到相同输出；Replay 不是 rerun。

### Evidence 34-E09｜Resume continuation

- Claim / Status / Class：`34-C09 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Path：`AgentLoop.resume -> SessionPersistence.prepare -> SessionStore.prepare -> setupAndPublish`。
- Trace：X03 same Session id；old prefix retained；fresh follow-up keeps contiguous `0..N` seq；Turn `[1] -> [1,2]`；History `3 -> 5`。
- Proves：pinned JSONL/in-memory fixture 的 same-identity Resume append。
- Does Not Prove：real Provider continuation、remote side-effect recovery 或 crash consistency beyond owner fixture。

### Evidence 34-E10｜Fork isolation

- Claim / Status / Class：`34-C10 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Paths：`SessionStore.fork` completed-prefix cut；`SessionCommands.fork` live/cold observation -> new Agent/new id。
- Trace：X04 distinct arrays/nested events、frozen child seed、parent unchanged、earlier-boundary History only、cold Host fork without source resume。
- Proves：new lineage、detached prefix、isolated future suffix 与 parent immutability。
- Does Not Prove：cross-process/storage transaction isolation。

### Evidence 34-E11｜External world is not forked

- Claim / Status / Class：`34-C11 / PARTIAL / PINNED_SOURCE + EXPERIMENT`
- Source boundary：Fork copies selected event prefix/header metadata；无 process/file/credential/remote transaction/side-effect snapshot mechanism。
- Trace：X04 mutation sentinel 只证明 detached in-memory event graph；historical Tool 未因 cold fork 重跑。
- Proves：event-copy boundary 和 absence of external clone protocol in pinned path。
- Gap：未执行真实 external side effect；不证明生产 Tool 幂等、回滚或远端状态。

### Evidence 34-E12｜Permission and budget inheritance

- Claim / Status / Class：`34-C12 / PARTIAL / PINNED_SOURCE + EXPERIMENT`
- Source：permission/approval/sandbox events 是历史事实而非 authorization token；persistence location explicitly not authority；`delegationDepth` 是 durable recursion budget。
- Trace：X03 round-trips `delegationDepth:1`；未观察 credential、approval、cost 或 turn-budget transfer。
- Proves：delegationDepth survives selected resume；generic inheritance contract is absent in pinned source。
- Gap：真实 permission service、billing 与 authorization runtime 均未验证；不得推断继承。

### Evidence 34-E13｜Compaction append plus surface replacement

- Claim / Status / Class：`34-C13 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Path：append `compaction/start|summary|end` log-only facts + replacement `user/message(surfaceOp=replace,sourceEventSeqs=...)`。
- Trace：X05 seq strictly advances；original raw event remains；checkpoint message changes current model surface。
- Proves：pinned compaction 是 append transaction + logical surface replacement，不是 raw log rewrite。
- Does Not Prove：所有存储后端的物理保留策略或摘要语义完整性。

### Evidence 34-E14｜Evidence/unverified provenance boundary

- Claim / Status / Class：`34-C14 / PARTIAL / PINNED_SOURCE + EXPERIMENT`
- Source/Trace：generic `SessionEvent` 无 `verified/unverified` field；X05 only observes compaction id/source links and shadowed seqs。
- Proves：generic contract 不能表达 Evidence verification preservation；provenance link 不等于 verified status。
- Gap：domain-specific evidence schema 未在本篇验证；不能声称 verified/unverified 被完整保留。

### Evidence 34-E15｜BuildPilot `IContextContributor + Receipt` proposal

- Claim / Status / Class：`34-C15 / PROPOSAL / DESIGN_PROPOSAL`
- Proposal：`IContextContributor` 贡献 projection/context，并返回 `Receipt {contributorId,sourceRefs,inputEventRange,transformVersion,verifiedState,outputHash}`；持久化由独立 owner 负责。
- Counter-evidence guard：不得让 contributor 充当 event store、让 receipt 声称复制 external world，或自动把 unverified 升为 verified。
- Proves：`N/A — Part VII candidate only`。
- DSH Verification / Course Decision：`N/A / SIMPLIFY`。

## Evidence Gate recommendation

`PASS / OUTLINE ELIGIBLE`。

15 Claims/Cards 最终为 `9 CONFIRMED / 5 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。核心 Event/History/Replay/Resume/Fork/Compaction 已由 pinned source 与 passing owner tests 闭合；UI/Trace runtime、真实 external world、generic permission/cost budget 与 verified/unverified 仍必须作为限制写入正文。
