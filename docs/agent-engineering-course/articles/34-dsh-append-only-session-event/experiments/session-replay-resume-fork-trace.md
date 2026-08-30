# Article 34 Session Replay / Resume / Fork Trace

Status: `PASS / PINNED OWNER-TEST BASELINE`

## 1. Frozen boundary

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Captured at: `2026-08-30T09:07:06+08:00`
- Runtime: Windows x64, Node `v24.18.1`, Vitest `4.1.8`
- Scope: repository-owned in-memory sessions, JSONL persistence fixture, `MockAdapter`, deterministic tools and host fakes only. No network, credential, real Provider, production Tool, billing, or external process was used.

This is a selected owner-test baseline, not a new instrumentation harness. Assertions below are the exact observable contracts encoded by the pinned tests; facts that the selected tests do not observe stay explicit limitations.

## 2. Command receipt

The shell-resolved `pnpm.cmd` came from Codex's fallback runtime. Although the checkout had `node_modules/.bin/vitest.cmd`, `pnpm exec vitest ...` exited `1` with `'vitest' is not recognized`. The four selected runs therefore invoked the checkout-local `node_modules\.bin\vitest.cmd` directly.

| Run | Selected files / pattern | Exit | Result |
|---|---|---:|---|
| `A34-T01` | `loop.spec.ts`, `registry.spec.ts`; simple turn, tool round-trip, replay, projection drive | `0` | `2 files passed`; `4 passed / 75 skipped` |
| `A34-T02` | `resume.spec.ts`; legacy prefix, fork lineage, persisted continuation | `0` | `1 file passed`; `3 passed / 25 skipped` |
| `A34-T03` | core and Host fork specs; latest/earlier boundary, anchored/cold fork | `0` | `2 files passed`; `4 passed / 18 skipped` |
| `A34-T04` | `compaction.spec.ts`; compaction event/log-only contract | `0` | `1 file passed`; `1 passed / 4 skipped` |

Aggregate successful selection: `6` file executions, `12 passed`, `122 skipped`, `0 failed`. Each run printed only Vitest's `vite-tsconfig-paths` deprecation notice in addition to the pass summary.

## 3. Raw event and projection observations (`34-X01`, `34-X02`)

### Ordered durable/live feed

The simple-turn owner test records boundary delivery through `session/event` and asserts this exact live order:

```text
turn/start -> step/start -> step/end -> turn/end
```

The same Session's durable event types begin with `agent/inbox/spliced`, contain `turn/start`, `user/message`, and `assistant/message`, and end with `turn/end`. The assistant event retains `{ inputTokens: 10, outputTokens: 11 }` for the deterministic `hello there` response. `deriveMessages()` projects that stream to roles `[user, assistant]`, with assistant content `hello there`; it is therefore not the raw event sequence.

The two-Step tool test observes exactly two MockAdapter requests. Its durable log contains `tool/call` and `tool/result`; request 2 contains a correlated `tool-result` block `{ toolCallId: "c1", isError: false, content: "echo: ping" }`. This proves the model-history projection consumes the recorded result rather than treating the UI-visible transcript as its oracle.

The domain-projection test appends two `test/mark` events and observes snapshot value `{ marks: ["a", "b"] }` at `asOfSeq = session.seq - 1`. Thus the selected runtime observations cover raw/live event order, Model History, and a registered Domain State fold. The UI Transcript and SessionQuery Trace are mapped by pinned source but are not independently snapshotted by this selected baseline.

### Replay

After a deterministic tool turn, `Session.create("replayed", { seed: [...events] })` produces `deriveMessages()` exactly equal to the original Session. The inherited prefix's event-type sequence is equal event-for-event, followed by a new `session/end-seed` marker in the replayed Session. The replay operation itself has no Provider or Tool body in its path; the selected test does not add a separate counter assertion, so the zero-invocation conclusion remains source-and-fixture scoped rather than a real-provider claim. Replay reconstructs recorded projections; it does not promise identical output from a new model sample.

## 4. Resume observations (`34-X03`)

- A stored legacy prefix with seq `0..6` (`turn/start`, user, step, assistant, steering, `step/end`, `turn/end`) resumes under the same Session id. Derived History contains the three legacy message nodes; a fresh follow-up appends a completed turn and produces five derived messages.
- A fresh Context over the same JSONL root resumes `sess-resume`; its events are the old prefix plus one `session/end-seed`, and `firstLiveSeq` equals the old event count. Before the follow-up, its derived History equals a fresh replay of the old events.
- The follow-up keeps all seqs exactly `0..N` with no duplicates and advances turn starts from `[1]` to `[1, 2]`.
- A persisted fork header round-trips `parentSession`, `cwd`, original `seedLength`, and `delegationDepth: 1`. This is the only selected explicit budget-like inheritance observation. Generic approval, credential, cost, and turn-budget inheritance remain absent/unproved.

## 5. Fork observations (`34-X04`)

- Default core fork copies the completed prefix into distinct arrays and distinct nested event objects, freezes child seed content, leaves the parent's `hello` content unchanged after a rejected child mutation, and records child id, `parentSession`, `cwd`, and `seedLength`.
- Forking at the first completed boundary while the parent has a second closed turn and an open third turn yields exactly `parent.events.slice(0, boundary + 1)` plus the child's `session/end-seed`. Child History contains only `first`.
- Host fork at an anchored completed turn yields child types `[turn/start, user/message, turn/end, session/end-seed]`, with parent lineage and cwd.
- Cold Host fork reads persisted events and creates a child without calling `agents.resume`, without publishing the source as a live Agent, and without copying the source's `origin: subagent` marker.

The selected deterministic mutation sentinel proves detached in-memory event graphs and parent-prefix immutability. It does not clone, roll back, or re-execute a real external side effect, so production external-world semantics remain outside the evidence boundary. No selected test shows silent permission or cost-budget elevation.

## 6. Compaction observations (`34-X05`)

Starting from an original appended `user/message`, the owner test observes ordered raw compaction facts:

```text
compaction/start -> compaction/summary -> compaction/end
```

`compaction/start` has no `surfaceOp`; summary/end seqs strictly advance; `shadowedRange` identifies the original event and `shadowedSeqs` is `[original.seq]`. A separate replacement `user/message` carries the compaction-checkpoint source. The raw original remains in `session.events`; compaction appends facts and changes the model surface through a checkpoint message rather than rewriting the old array entry.

There is no generic verified/unverified field asserted by this test. Provenance beyond compaction id/source linkage is therefore `NOT REPRESENTABLE BY THIS GENERIC EVENT CONTRACT`, not silently preserved evidence semantics.

## 7. Acceptance and limitation ledger

| Experiment | Selected evidence | Result |
|---|---|---|
| `34-X01` | live boundary order, durable types/usage, History/tool correlation, Domain projection watermark | `PASS`, with UI Transcript / SessionQuery Trace runtime snapshots not selected |
| `34-X02` | equal reconstructed History and inherited event-type prefix; replay adds end-seed | `PASS`, fixture-scoped; no model-resampling guarantee |
| `34-X03` | same identity, retained prefix/History, fresh suffix, contiguous seqs, turn continuation, explicit lineage/delegation fields | `PASS`; generic permission/cost budget is `ABSENT/UNKNOWN` |
| `34-X04` | detached/frozen child prefix, earlier-boundary History, parent unchanged, cold fork without source resume | `PASS`, in-memory boundary only; no real external-world claim |
| `34-X05` | append-only compaction facts, retained original raw event, replacement checkpoint source | `PASS`; generic Evidence/unverified semantics are not representable |

Replay, Resume, and Fork are all directly exercised by passing pinned owner tests. The baseline supports fixture-scoped article claims when combined with the Source Investigator's exact owners and call paths; it must not be inflated into real-provider determinism, production side-effect rollback, generic permission inheritance, or billing-state inheritance.

## 8. Cleanliness receipt

After all runs, `git status --short` and `git diff --stat` in the external fixture produced no output. `git rev-parse HEAD` remained `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
