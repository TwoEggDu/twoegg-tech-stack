# Article 34 Call Path

Status: `SOURCE_MAP PASS`

Pinned source: `deepseek-ai/deepseek-harness@cd5ef8148158c3a752a658978873241fdf8e2bbc` (`dsh-v0.1.2-alpha.1`).

## A. Accepted live event to durable append

```text
producer
  -> Session.append(type, data, surfaceIntent?)
     core/session/src/index.ts:602-653
  -> snapshot + validate -> { type, seq: log.length, time, data }
  -> log.push(frozen event)                         [live commit]
  -> contained session/event publication
  -> PersistenceCoordinator                         [1162-1213]
  -> SessionWriteBehind.enqueue
  -> backend.append(id, contiguous batch)           [durable commit]
  -> session/flush                                  [explicit barrier]
```

Live acceptance precedes durable completion; event delivery is not a durability receipt.

## B. One pinned stream to Model History

```text
Session.create(newId, seed)
  -> validate contiguous seq / known types / surface transitions
  -> append session/end-seed
  -> Session.deriveMessages()
  -> current surface.nodes
  -> deriveEventMessage:
       user/message | non-empty assistant/message | tool/result -> Message
       boundaries | chunks | log-only events -> null
```

Anchors: `core/session/src/index.ts:480-545,699-754`; `surface.ts:70-114`. Owner test `core/agent-loop/tests/loop.spec.ts:1507-1531` asserts equal derived History and inherited event types.

## C. Same stream, different projections

```text
UI/history: SessionHistoryController.page/follow
  -> SessionQuery.observeSession(live preferred, else prepared durable)
  -> raw page/snapshot(cursor) -> gap-free session/event frames

Domain State: SessionProjectionRegistry
  -> init(header) -> apply(state, every event) -> observedSeq
  -> optional validated wire snapshot

Trace: SessionQuery.readSession/observeSession
  -> exact header + contiguous raw events + cursor + optional projections
  -> replay validation without publication
```

UI uses append-origin transcript material, Model History uses current surface, Domain State uses plugin folds, and Trace retains raw events. They are not aliases.

## D. Replay versus Resume

```text
Reconstruction Replay
  copied/stored events -> Session.create(seed) -> deriveMessages()
  result: equal History for accepted prefix; no model/Tool/external execution

Resume
  AgentLoop.resume(resumeSessionId)
  -> SessionPersistence.prepare(id) -> balanced load
  -> SessionStore.prepare(id, full stored seed/header)
  -> setupAndPublish(source="resume")
  -> same id; later work appends new events
```

Resume anchors: `core/agent-loop/src/index.ts:648-695`, persistence `index.ts:176-199`; tests `resume.spec.ts:130-165,203-220,575-601`.

## E. Isolated Fork closure

```text
SessionStore.fork(source, boundary, childId)
  -> exact live source -> validate seq and closed-turn prefix
  -> seed = source.events[0..boundary]
  -> create(new childId, { seed, parentSession, seedLength, cwd })
  -> detach/freeze seed -> independent child future log
```

Core tests `core/session/tests/fork.spec.ts:79-135` prove detached arrays/nested events, equal History at the cut, lineage, and exclusion of later/open work.

```text
SessionCommands.fork(request)
  -> observe live or cold durable source (no source Agent resume)
  -> choose completed turn/end cut
  -> new random SessionId + new Agent composition
  -> agents.create(seed prefix, parentSession, seedLength, cwd)
  -> optional workspace attach
```

Host anchors: `api/session-controller/src/commands.ts:182-275`; tests `session-fork.host.spec.ts:87-187`.

## F. Compaction is append plus surface replacement

```text
append compaction/start                    [log-only]
append compaction/summary                  [log-only provenance/metering]
append user/message(
  surfaceOp=replace(start,end),
  sourceEventSeqs=[transaction,...shadowed nodes])
append compaction/end                      [log-only]
```

Anchors: `compaction/src/types.ts:16-89`; `core/session/src/surface.ts:210-242`; owner test `compaction/tests/compaction.spec.ts:122-151`. Original events remain in raw Trace; only future Model History sees the replacement summary.

## G. Inheritance boundary and Lab handoff

Fork copies the selected immutable event prefix and a new lineage header. It does not copy processes, credentials, files/services, remote transactions, prior side effects, current external authority, or a generic cost budget. Historical permission events remain facts, not authorization tokens. Only `delegationDepth` has an explicit persisted recursion-budget contract.

Required fresh Lab candidates:

1. stream -> equal History: `loop.spec.ts:1507-1531`;
2. History -> isolated earlier-prefix Fork: `fork.spec.ts:79-135`;
3. Resume continuation: `resume.spec.ts:130-165`;
4. cold Host Fork without resume: `session-fork.host.spec.ts:146-186`;
5. append-only compaction: `compaction.spec.ts:122-151`.

These remain `OWNER_TEST_ANCHORS` until the Lab Engineer executes them.
