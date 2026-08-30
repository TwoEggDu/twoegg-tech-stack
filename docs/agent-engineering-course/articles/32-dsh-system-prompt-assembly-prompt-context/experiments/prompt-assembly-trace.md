# Article 32 Prompt Assembly / Request Trace

Status: `PASS / RAW OBSERVATION COMPLETE`

## 1. Baseline and safety envelope

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Host: Windows 10 x64; Node `24.18.1`; pnpm `11.7.0`; PowerShell `7.6.4`
- Fixture policy: read-only. No source, config, lockfile, or dependency was changed.
- Provider boundary: all request observations used the repository's in-memory `MockAdapter`. No real provider was selected, no credential was read, and no network/token/cost path was entered.
- Workspace boundary: only this report was written. Inline instrumentation was piped to
  `node --import tsx/esm --input-type=module -`; it did not create a script in the DSH fixture.

The required Request Trace was obtained: two real Agent Loop steps reached the terminal mock
adapter, and the exact normalized requests were captured there. Therefore this gate is not
`BLOCKED_EVIDENCE`.

## 2. Source-owned transformation chain

The experiment observes this pinned implementation path rather than inventing an article-local
assembler:

1. `SystemPrompt.assemble()` reads global and scoped variable providers, with nearer scoped
   values overwriting farther/global values
   (`packages/core/system-prompt/src/index.ts:518-533`).
2. It merges section/context registries by name, collects tool providers, and clones tool
   parameters (`index.ts:534-554`).
3. Prompt sections sort by `(order, code-unit name)`; contexts sort by numeric `order` only
   (`index.ts:555-579`). Equal-order contexts therefore preserve the registry's stable iteration
   order, while equal-order sections use name as an explicit tie-breaker.
4. The mutable `PromptAssembly` passes through the authoritative
   `system-prompt/assemble` waterfall (`index.ts:580-590`).
5. `renderPrompt()` interpolates strict variables, removes empty sections, and joins sections
   with blank lines (`index.ts:263-267`). `renderContextSections()` retains contributor names
   while interpolating and dropping empty context (`index.ts:293-305`).
6. Before every step, Agent Loop assembles again, renders the context sections, joins them into
   a superseding snapshot, and asks `RuntimeContextProjection` whether a new durable user-role
   message is required (`packages/core/agent-loop/src/agent.ts:232-249`).
7. The step renders the stable system field once for that assembly, derives current messages,
   builds a frozen request, and dispatches it to the prepared adapter or `llm.stream`
   (`agent.ts:339-362`, `agent.ts:496-540`).

This gives two different receipts:

- **Effective Assembly**: ordered sections, ordered contexts, tools, and variables after the
  assembly waterfall, before rendering into the request.
- **Context Snapshot**: a durable `user/message` whose `source.form` is `snapshot` and whose
  `source.sections` preserves each named contributor and its already-rendered text.

They are related but not interchangeable. An Effective Assembly is per-step transient state;
the Context Snapshot is model-visible session history with provenance.

## 3. Experiment A — real two-step request trace

### 3.1 Setup

The inline harness mounted the production service chain:

```text
LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime
           -> AgentRegistry -> AgentLoop -> MockAdapter
```

It then registered these contributors:

| Kind | Registration | Order / value | Purpose |
|---|---|---:|---|
| built-in section | `harness:identity` | `-1000` | stable identity |
| configured section | `deployment:persona` | `0` | strict `persona_name` and `model` interpolation |
| section | `zeta-section` | `100` | equal-order conflict observation |
| section | `alpha-section` | `100` | equal-order conflict observation |
| context | `zeta-context` | `50` | dynamic `mode` provider |
| context | `alpha-context` | `50` | per-assembly incrementing `tick` provider |
| variable | `persona_name` | `trace-agent` | explicit provenance for template expansion |
| loop variables | `provider`, `model` | `mock`, `mock` | agent-owned request facts |
| tool | `flip_mode` | one schema | forces a second step and changes `mode` during execution |

The mock's first response requested `flip_mode`; executing it changed `mode` from `read-only` to
`write-enabled`. The second mock response stopped normally. A
`system-prompt/assemble` listener captured the authoritative result returned by `next()`, while
`MockAdapter.requests` captured the requests that actually reached the terminal adapter.

### 3.2 Command and exit

```powershell
@'<inline TypeScript harness described above>'@ |
  node --import tsx/esm --input-type=module -
```

- Exit code: `0`
- Adapter requests: `2`
- Durable `step/start` events: `(seq=3, turn=1, step=1)` and
  `(seq=21, turn=1, step=2)`
- Durable runtime-context messages: `seq=5` and `seq=22`, both `surfaceOp=append`

### 3.3 Effective Assembly receipts

The two authoritative assembly captures were:

```json
{
  "step": 1,
  "sections": [
    "harness:identity",
    "deployment:persona",
    "alpha-section",
    "zeta-section"
  ],
  "contexts": [
    { "name": "zeta-context", "text": "mode=read-only" },
    { "name": "alpha-context", "text": "tick=1" }
  ],
  "variables": {
    "provider": "mock",
    "model": "mock",
    "persona_name": "trace-agent"
  },
  "tools": ["flip_mode"]
}
```

```json
{
  "step": 2,
  "sections": [
    "harness:identity",
    "deployment:persona",
    "alpha-section",
    "zeta-section"
  ],
  "contexts": [
    { "name": "zeta-context", "text": "mode=write-enabled" },
    { "name": "alpha-context", "text": "tick=2" }
  ],
  "variables": {
    "provider": "mock",
    "model": "mock",
    "persona_name": "trace-agent"
  },
  "tools": ["flip_mode"]
}
```

Normalized assembly SHA-256 values (JSON insertion order as printed):

- Step 1: `0420ABBCE3215D69C33564A5575944777FE1C57C27D4A1B0978B417271584551`
- Step 2: `17AABD2AA449A9EDC23A349A15CC34DC880591CDB954DEBC3859F99229011473`

The stable rendered system text was byte-identical for both steps:

```text
You are an AI agent powered by DeepSeek Harness.

Persona: trace-agent; model=mock.

Alpha stable for trace-agent.

Zeta stable.
```

### 3.4 Request 1 receipt

Normalized SHA-256:
`72326D5189BF92BC67C41745A3F61358291B670E0CDB07D0972927C9120B78CA`

```json
{
  "step": 1,
  "provider": "mock",
  "model": "mock",
  "tools": ["flip_mode"],
  "messageCount": 2,
  "messages": [
    {
      "role": "user",
      "source": { "kind": "user" },
      "text": "exercise two steps"
    },
    {
      "role": "user",
      "source": {
        "kind": "plugin",
        "plugin": "@deepseek-ai/dsh-system-prompt",
        "form": "snapshot",
        "sections": [
          { "name": "zeta-context", "text": "mode=read-only" },
          { "name": "alpha-context", "text": "tick=1" }
        ]
      },
      "text": "Current runtime context. This snapshot supersedes earlier runtime-context snapshots.\n\nmode=read-only\n\ntick=1"
    }
  ]
}
```

### 3.5 Request 2 receipt and diff

Normalized SHA-256:
`5705EE4D9EF5B6A6F3654D92D3EFC8D058D2C301AC3B2721F5976C4FB735AE89`

Stable fields:

- `provider`: `mock` -> `mock`
- `model`: `mock` -> `mock`
- rendered `system`: byte-identical
- tool names: `["flip_mode"]` -> `["flip_mode"]`
- section order and rendered stable section text: unchanged

Changed history:

```diff
 messageCount: 2 -> 5
+ assistant/model(mock): text "switching" + tool-call flip_mode(trace-call-1)
+ user/tool(trace-call-1): tool-result "mode flipped", isError=false
+ user/plugin(@deepseek-ai/dsh-system-prompt, form=snapshot):
+   sections:
+     zeta-context = "mode=write-enabled"
+     alpha-context = "tick=2"
+   text:
+     Current runtime context. This snapshot supersedes earlier runtime-context snapshots.
+
+     mode=write-enabled
+
+     tick=2
```

Request 2 still contains the first snapshot earlier in history. DSH does not rewrite the old
event for an ordinary step; it appends a new complete snapshot whose explicit model-facing text
says that it supersedes earlier runtime-context snapshots. The newest snapshot is therefore the
effective dynamic context, while both events remain attributable.

### 3.6 Expected / Observed / Interpretation / Does Not Prove

**Expected**

- Equal-order sections resolve deterministically; context providers are evaluated again before
  step 2; only dynamic context changes after the tool executes.
- A new snapshot appears in the second request with named provenance.

**Observed**

- Equal-order sections registered `zeta` then `alpha` assembled as `alpha`, `zeta`.
- Equal-order contexts registered `zeta` then `alpha` stayed `zeta`, `alpha`.
- Stable system and tools remained byte/list equal; dynamic context changed from
  `read-only/tick=1` to `write-enabled/tick=2`.
- The terminal adapter received two requests with `2` and `5` messages.

**Interpretation**

- Stable Context belongs in registered prompt sections: it is regenerated into the `system`
  field at every step and does not consume a durable transcript message.
- Dynamic Context belongs in `PromptContext`: providers are sampled per assembly, then materialized
  as a complete, named, append-only snapshot only when the effective text changes.
- The source metadata is a useful receipt: it records which named contributions were rendered and
  their exact text, not merely an opaque combined blob.

**Does Not Prove**

- The mock proves assembly/request semantics, not behavior of any external model or provider SDK.
- Two contributors do not prove that every shipped context plugin has correct order or content.
- Stable request equality here does not prove all deployments have stable system text; dynamic
  section providers or waterfall listeners may deliberately change it.
- SHA values cover the normalized evidence shape documented here, not `AbortSignal`, timestamps,
  generated message IDs, or provider wire serialization.

## 4. Experiment B — negative cases and conflict behavior

### 4.1 Direct negative probe

An isolated `SystemPrompt` instance ran three failing operations. The process exited `0` because
each failure was caught and printed as an expected observation; no Agent Loop or adapter was
mounted.

```jsonl
{"label":"duplicate-section-same-layer","outcome":"ERROR","name":"Error","message":"prompt section \"duplicate-demo\" is already registered (for a per-agent override, register through that agent's `agent.ctx` instead)"}
{"label":"bad-variable-registration","outcome":"ERROR","name":"Error","message":"invalid prompt variable name \"Bad-Name\" (must match /^[a-z][a-z0-9_]*$/)"}
{"label":"unknown-variable-render","outcome":"ERROR","name":"Error","message":"unknown prompt variable \"{{missing}}\" in section \"unknown-variable-demo\"; registered variables: (none)"}
```

**Expected**: same-layer duplicates and invalid/unknown variables fail before any provider request.

**Observed**: duplicate registration and invalid variable name threw synchronously; the unknown
reference survived assembly as uninterpolated text and threw during `renderPrompt()`.

**Interpretation**: registration integrity, assembly, and rendering are separate failure
boundaries. A valid-looking Effective Assembly is not yet proof that strict interpolation will
succeed.

**Does Not Prove**: the probe does not cover every malformed brace form. The owner suite below
covers empty, spaced, nested, prototype-property, and undefined-value cases.

### 4.2 Owner tests

Command:

```powershell
.\node_modules\.bin\vitest.cmd run `
  packages/core/system-prompt/tests/system-prompt.spec.ts `
  packages/core/system-prompt/tests/scoped.spec.ts `
  packages/core/system-prompt/tests/invariant.spec.ts `
  --reporter=verbose
```

Result: exit `0`; `3` files passed; `68/68` tests passed.

This suite directly covered order, equal-order tie-breaking, duplicate sections/contexts/variables,
bad variable forms, unknown and undefined variables, scoped shadowing, authoritative waterfall
transforms, complete-prompt conflicts, lifecycle disposal, and invariant validation.

## 5. Experiment C — compaction and invariant re-injection

The requested mechanism is **present for current runtime-context snapshots** in this pinned
version; it must not be labelled absent.

`RuntimeContextProjection` restores the newest retained owned snapshot from the current surface,
then watches replacement events. If a replacement names the retained event in
`sourceEventSeqs`, it sets retained state to `null`
(`packages/core/agent-loop/src/runtime-context.ts:24-55`). At the next pre-step, `project()` emits
the current complete snapshot again when retained text is missing/different, preserving named
sections in `source.form=snapshot` (`runtime-context.ts:58-75`).

Focused command:

```powershell
.\node_modules\.bin\vitest.cmd run `
  packages/core/agent-loop/tests/loop.spec.ts `
  -t "renders harness identity|contains a strict-variable|materializes changed runtime context|re-emits unchanged runtime context|clears compacted runtime context" `
  --reporter=verbose
```

Result: exit `0`; `1` file passed; `5/5` selected tests passed (`51` skipped by the filter).

The selected runtime tests prove:

- stable system rendering plus variable interpolation reaches an adapter request;
- bad variables prevent the request, and the loop can serve a later repaired turn;
- changed dynamic context appends a new snapshot without changing the system header;
- when a surface replacement removes the retained snapshot, unchanged active context is re-emitted;
- when the active set becomes empty after replacement, a clearing marker is emitted instead.

**Boundary**: this is not a generic "re-inject arbitrary invariants after any compaction" API.
Stable prompt sections are independently reassembled into the request's `system` field every
step; runtime-context snapshots have their own retained-surface projection. The exact symbol
`IContextContributor` has `0` matches in the pinned repository, so BuildPilot's
`IContextContributor + Receipt` remains a design proposal, not current DSH terminology or an
implemented interface.

## 6. Failed attempts retained as evidence

### 6.1 `pnpm exec tsx -` launcher failure

The first stdin launcher attempt exited `1`:

```text
'tsx' is not recognized as an internal or external command,
operable program or batch file.
```

Both `node_modules/.bin/tsx.cmd` and `vitest.cmd` existed. The non-writing recovery was to invoke
Node's loader directly: `node --import tsx/esm --input-type=module -`. This is a Windows launcher
observation, not a DSH prompt failure.

### 6.2 Incorrect manual assembly context

An early probe manually called assembly with `{ scope: agent.scope }` and then rendered it. It
exited `1` with:

```text
prompt variable "{{model}}" has no value for this assembly
(section "deployment:persona")
```

The failure was valid: Agent Loop's actual call uses `assembleContextFor(agent, signal)`, which
sets `agent` and `scope` together. The final experiment stopped pretending a hand-built context was
a real request and instead captured the assembly inside the real loop's waterfall. This preserves
an important provenance rule: scope selection alone does not supply agent-owned variables.

## 7. Risks exposed by the trace

1. **Order collision risk**: sections and contexts do not share the same equal-order rule. Sections
   use name tie-break; contexts use numeric order with stable input order. A plugin that assumes a
   universal name tie-break can silently misread precedence.
2. **Snapshot accumulation risk**: ordinary changes append a full superseding snapshot. Consumers
   must respect the newest snapshot semantics and compaction surface, not scan for the first owned
   message.
3. **Late render risk**: unknown or undefined variables fail at render after assembly. Logging only
   raw assembly success can falsely report readiness.
4. **Waterfall authority risk**: listeners can transform or short-circuit the assembly; registry
   state alone is not the final Effective Assembly receipt.
5. **Provenance drift risk**: a combined text dump without `source.sections` loses subsystem
   attribution. Preserve both rendered text and named contributions.
6. **Over-generalization risk**: compaction re-emission here is owned by the runtime-context
   projection; it is not evidence for an arbitrary cross-subsystem invariant framework.

## 8. Final verification

- Required assembly order and conflict behavior: observed.
- Two real Step requests and exact normalized diff: observed.
- Duplicate section and bad-variable negative paths: observed.
- Stable versus Dynamic Context: observed in the same two-step run.
- Effective Assembly and Context Snapshot provenance/transformation: observed and source-traced.
- Compaction re-emission: present, source-traced, and selected owner tests passed.
- Real provider/credential/token/cost activity: none.
- DSH fixture after all commands: exact HEAD
  `cd5ef8148158c3a752a658978873241fdf8e2bbc`; `git status --short` empty (`CLEAN`).

Gate result: `PASS`. Next allowed gate: `EVIDENCE_MERGE`.
