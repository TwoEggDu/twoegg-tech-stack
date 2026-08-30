# Article 34 Repository Map

Status: `SOURCE_MAP PASS`

## Pinned boundary

- Repository/tag/commit: `deepseek-ai/deepseek-harness` / `dsh-v0.1.2-alpha.1` / `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Evidence ceiling: pinned source and owner-test anchors only. No experiment or real-provider, network, permission-service, billing, or external-side-effect observation was made.

## Event envelope and complete pinned vocabulary

`SessionEvent<T>` is `{ type, seq, time, data }`; `seq` is monotonic within one Session. Only `user/message`, `assistant/message`, and `tool/result` may also carry `surfaceOp` and `sourceEventSeqs` (`packages/core/session/src/types.ts:383-417`). The generated fail-closed catalog is the complete in-repository vocabulary known by this build (`packages/core/session/src/known-event-types.ts:1-70`):

| Family | Event types | Correlation / role |
|---|---|---|
| lifecycle | `turn/start`, `turn/end`, `step/start`, `step/end`, `session/end-seed` | Turn/Step numbers are payload correlation; `session/end-seed` marks constructor seed versus this lifecycle's live appends. No universal Run id exists in the core envelope. |
| model surface | `user/message`, `assistant/message`, `tool/result` | Only message-producing surface types; tool result's message carries call id, and assistant/tool payloads carry Turn/Step. |
| request/raw trace | `assistant/chunk`, `tool/call`, `request/header`, `request/context`, `llm/retry`, `llm/retry-started` | Request/stream/tool-attempt facts; most are log-only. |
| compaction | `compaction/start`, `compaction/summary`, `compaction/prune`, `compaction/end` | Transaction/provenance/metering events are log-only; a separate replacement `user/message` changes model surface. |
| inbox/control | `agent/inbox/spliced`, `command/run`, `command/done`, `feedback/record`, `hook/invoked`, `hook/result` | Durable control and input facts. |
| policy/mode | `approval/asked`, `approval/decided`, `approval/policy`, `permission/preset`, `sandbox/mode`, `plan/mode`, `model/selection`, `agent-preset/selected` | Logged configuration/decision facts, not external authorization tokens. |
| domain | `goal/change`, `todo/write`, `schedule/change`, `session/title`, `session/title-llm-request`, `subagent/descriptor`, `subagent/model-selection-policy` | Domain plugins fold these into state/read views. |
| team/workflow | `team/member`, `team/message/delivered`, `team/message/queued`, `team/task`, `tool-workflow/agent-start`, `tool-workflow/agent-end`, `tool-workflow/run-start`, `tool-workflow/run-end` | Plugin-specific orchestration facts; `run-*` is not a universal Session Run envelope. |
| extension | `tool/code-dispatch`, `tool/code-dispatch-start`, `session-log-deepseek/delivery-accepted`, `web/deepseek-search-llm-request` | Extension facts in the same append-only log. |

Unknown types fail closed (`known-event-types.ts:8-16`). Downstream event registration is deferred, so this table is build-scoped.

## Durable/live owners and write path

| File | Symbol / lines | Pinned fact |
|---|---|---|
| `packages/core/session/src/index.ts` | `Session`, 423-470 | `log` and `SurfaceManager` own live in-memory log/surface. `firstLiveSeq` distinguishes constructor seed from appends in this process; seed events are not republished. |
| same | constructor, 480-545 | Seed events are detached/validated, require `seq === index`, fold through the same surface transition, then get `session/end-seed` if needed. |
| same | `events` / `append`, 548-653 | Events are frozen. `append` assigns `seq = log.length`, validates, commits to `log`, then publishes `session/event`; observer failure cannot roll it back. |
| `packages/session/session-persistence/src/index.ts` | `SessionPersistence`, 99-214 | Backend contract owns durable `create`, contiguous `append`, balanced `load`, and resume `prepare`; `append` resolves only after durability. |
| `packages/session/session-persistence/src/coordinator.ts` | `installWritePath`, 1162-1213 | `session/created` initializes, `session/event` queues through write-behind, and `session/flush` is the durability barrier. |

Thus live acceptance precedes asynchronous durable completion. A notification is not a durability receipt; use flush or a successful durable read.

## Read paths and projections

| Projection/read | Anchors | Includes / excludes |
|---|---|---|
| Model History | `SurfaceManager`, `deriveEventMessage`, `Session.deriveMessages`; `core/session/src/surface.ts:14-114`, `index.ts:699-754` | Current surface nodes become user, non-empty assistant, and tool messages. Replacements shadow old nodes; boundaries/chunks/log-only events stay out. |
| UI/history transport | `SessionHistoryController.page/follow`; `api/session-controller/src/history.ts:24-169` | Contiguous durable/live event pages, packed chunk runs, then gap-free live frames. Append-origin messages remain transcript material while replacements are model-only (`surface.ts:40-67`). |
| Domain State | `ProjectionDefinition` / `SessionProjectionRegistry`; `session-projection/src/index.ts:34-83,164-204,300-387,570-635` | Registered pure folds receive every committed event and own state/version/watermark plus optional validated wire view. Cache is a shortcut, not authority. |
| Trace/query | `SessionQueryEngine.readSession/observeSession`; `session-query/src/index.ts:115-171`; `observation.ts:13-187` | Selects exact live/prepared source and exposes header, contiguous raw events, cursor, and optional consistent projections. `readSession` replay-validates without live publication. |

UI Transcript, Model History, Domain State and raw Trace are intentionally unequal projections of one stream.

## Replay, Resume, Fork

| Operation | Source path | Proven semantics |
|---|---|---|
| reconstruction replay | `Session.create(id, seed)` -> validation/surface fold -> `deriveMessages`; `core/session/src/index.ts:480-545,699-754` | Rebuilds equal derived History from an accepted prefix. Owner test: `core/agent-loop/tests/loop.spec.ts:1507-1531`. No model call or external effect. |
| resume | `AgentLoop.resume` -> `SessionPersistence.prepare` -> `SessionStore.prepare` -> setup/publish; `core/agent-loop/src/index.ts:625-695`, persistence `index.ts:176-199` | Same Session id/full stored log, then new live appends. Tests: `resume.spec.ts:130-165,575-601,650-680`; live/open owner rejection `203-220`. |
| core fork | `SessionStore.fork` -> completed prefix -> new id/header `{parentSession, seedLength}`; `core/session/src/index.ts:1065-1151` | Detached frozen seed and independent future appends; open-turn boundaries reject. Tests: `core/session/tests/fork.spec.ts:63-319`. |
| Host fork | `SessionCommands.fork` -> observation -> completed-turn cut -> new Agent -> workspace attach; `api/session-controller/src/commands.ts:182-275` | Can fork a persisted source without resuming it; child is ordinary with new id/lineage. Tests: `session-fork.host.spec.ts:87-285`. |

## Compaction, permission and external-world boundary

- `compaction/start|summary|prune|end` are appended log-only facts. A new replacement `user/message` uses `surfaceOp: replace` and `sourceEventSeqs` covering transaction plus every shadowed node (`compaction/src/types.ts:16-89`; `tests/compaction.spec.ts:122-151`). Original events remain; only model surface changes.
- No generic `verified` field exists on `SessionEvent`; provenance links are not semantic verification. Evidence status is domain-schema responsibility.
- Fork copies a selected event prefix and selected header metadata. It does not clone processes, files, credentials, remote transactions, already-executed effects, or current external authorization.
- `permission/preset`, `approval/*`, and `sandbox/mode` in the prefix are historical facts. A persistence location is explicitly not an authorization token (`session-persistence/src/index.ts:87-97`).
- `delegationDepth` is durable so a recursion budget survives resume (`core/session/src/types.ts:86-98`; `resume.spec.ts:575-601`). No generic cost/turn-budget field or fork-transfer contract was found: generic budget inheritance is `ABSENT_IN_PINNED_SOURCE`.

## Counter-evidence / verdict

- No universal `runId`; plugin run events cannot be generalized.
- Replay reconstructs; it does not guarantee identical stochastic output or repeat Tool effects.
- Resume retains identity; Fork creates a new identity/lineage. Neither snapshots external reality.
- Transcript differs from compacted Model History; projection cache differs from source truth.
- No generic permission, credential, external-world, or cost-budget inheritance is established.

`PASS`: exact pinned symbols close the event table, live/durable write/read path, four projections, reconstruction Replay, Resume, isolated Fork, and append-only Compaction. External-world/authorization/generic-budget claims remain outside the evidence ceiling.
