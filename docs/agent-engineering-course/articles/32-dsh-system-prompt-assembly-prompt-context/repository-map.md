# Article 32 Repository Map

Status: `SOURCE_MAP PASS`

## Pinned source boundary

- Repository: `deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Source-only result: this map names production symbols and candidate owner tests. It is not a runtime request receipt and does not claim that any Lab command ran.

## Vocabulary reconciliation

The pinned source does **not** expose a general `Contributor` interface, an `IContextContributor`, or a first-class `Receipt` type. The prompt's broad word “contributor” maps to several concrete inputs that converge only at the request boundary:

| Prompt term | Pinned source term | Where it lands |
|---|---|---|
| stable identity / host / tool guidance | `PromptSection` registered by `SystemPrompt.section()` | flattened `GenerateOptions.system` |
| dynamic policy/state | `PromptContext` registered by `SystemPrompt.context()` | a durable user-role snapshot in `GenerateOptions.messages` |
| variable source | `SystemPrompt.variable(name, provider)` | interpolated into sections and contexts during render |
| tool provider | `SystemPrompt.tools(provider)` | `GenerateOptions.tools` |
| task / injected context | claimed `UserMessage` or an `agent/pre-step` waterfall contribution | durable session surface, then `GenerateOptions.messages` |
| history | `Session.deriveMessages()` | `GenerateOptions.messages` |
| model provider | `LlmRuntime` adapter registration selected by `{ provider, model }` | provider adapter / wire request |

Therefore “Section / PromptContext / Provider” is not one inheritance hierarchy. `PromptSection.text`, `PromptContext.text`, variables, and tools each accept provider callbacks, while `LlmAdapter` is a separate model-provider seam.

## Core schema and registry

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/system-prompt/src/index.ts` | `AssembleContext`, 41-50 | One assembly carries optional `scope` and `signal`; the signal belongs only to that assembly request. |
| same | `PromptSection`, 52-75 | Fields are `name`, `order`, `text`, optional `complete`; `text` is a string or per-assembly provider. |
| same | `PromptContext`, 77-85 | Fields are `name`, `order`, `text`; it is explicitly documented as a dynamic model-context contribution. |
| same | `AssembledSection`, 87-93; `AssembledContext`, 95-101 | Effective entries retain contributor `name` and resolved text before interpolation. |
| same | `ToolProviderResult`, 103-109 | A tool provider returns `schemas` and optionally the pre-restriction `knownNames`. |
| same | `PromptAssembly`, 111-120 | Effective assembly has four lanes: `sections`, `contexts`, `tools`, `variables`. |
| same | `FIRST_PARTY_SECTION_ORDER`, 130-161 | Repository-owned system sections use sparse numeric placements from identity `-1000` through structured output `9900`. |
| same | `PERSONA_SECTION` / `PERSONA_ORDER`, 163-172 | Persona replacement intentionally uses the stable name `deployment:persona` at order `0`. |
| same | `SystemPrompt.Config`, 236-253; schema 388-396 | Deployment config controls fixed identity, runtime-context inclusion, persona, and tool ordering. |
| same | `renderPrompt`, 255-268 | Interpolates each section, drops empty text, joins surviving sections with two newlines. |
| same | `renderContextSections`, 293-306 | Interpolates contexts and returns named non-empty snapshot sections. |
| same | `joinContextSections`, 279-290 | Joins context text and prepends the “supersedes earlier” marker. |
| same | `interpolate`, 308-345 | Strict single-pass `{{name}}` interpolation; malformed, unknown, or undefined values throw; substituted values are not rescanned. |
| same | `PromptLayer`, 348-385 | Each global/scoped layer owns named sections, named contexts, anonymous suppressors, anonymous tool providers, and named variables. |
| same | `SystemPrompt.section`, 424-440 | Rejects non-finite order; registration is effect-owned and duplicate checking is delegated to the layer. |
| same | `SystemPrompt.context`, 443-458 | Same finite-order and scoped-effect contract for dynamic contexts. |
| same | `SystemPrompt.suppressRuntimeContext`, 460-471 | A scope can suppress all context contributions without disabling their policy services. |
| same | `SystemPrompt.tools`, 474-486 | Global and matching scope tool providers both contribute; this lane is additive, not name-shadowing. |
| same | `SystemPrompt.variable`, 489-505 | Variable names must match `[a-z][a-z0-9_]*`; duplicate names within a layer throw. |
| `packages/core/scope/src/store.ts` | `NamedEntries.insert`, 30-54 | Duplicate names in one exact layer throw immediately; disposal removes only that entry. |
| same | `ScopedLayers.chainLayers`, 185-199 | Existing overlays are returned farthest ancestor first and exact scope last. |
| same | `ScopedLayers.merge`, 201-217 | Global named entries are overlaid by the scope chain; nearest scope wins the same name. |

## Ordered assembly and conflict behavior

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/system-prompt/src/index.ts` | `comparePromptSections`, 221-229 | Sections sort ascending by numeric `order`, then by code-unit `name`; registration order cannot perturb equal-order sections. |
| same | `SystemPrompt.assemble`, 518-533 | Variable providers run global first, then scope overlays farthest-to-nearest, so the nearest scope overwrites the same variable. |
| same | `SystemPrompt.assemble`, 534-554 | Sections/contexts are name-merged; tool-provider results are collected additively and detached by copying name/description/parameters. |
| same | `SystemPrompt.assemble`, 555-569 | Sections sort by `(order, name)`; more than one effective `complete` section fails before assembly is returned. |
| same | `SystemPrompt.assemble`, 570-582 | Contexts sort by `order` only; modern stable sort preserves effective-map insertion order for ties, but no name tie-breaker is encoded here. Tools receive their separate configured/canonical ordering. |
| same | `SystemPrompt.assemble`, 583-592 | `system-prompt/assemble` is an authoritative waterfall. Afterwards, an effective `complete` section is restored as the sole section and runtime-context suppression is restored as an empty context list. |
| same | `validateToolOrder`, 183-198; `orderTools`, 200-219 | Configured tool order must include `<unlisted-tools>`; unknown configured names fail. Without config, tools sort lexicographically. The code does not deduplicate duplicate schemas returned by providers. |
| `packages/core/system-prompt/src/invariant.ts` | `validateAssembly`, 15-43 | Companion invariant rejects duplicate assembled section/context names and invalid values after the waterfall. It checks tool names are non-empty but does not reject duplicate tool names. |

### Conflict matrix

| Conflict | Pinned behavior |
|---|---|
| same section/context/variable name, same layer | registration throws immediately |
| same named entry in ancestor and descendant scope | descendant shadows ancestor for that viewing scope |
| same section `order`, different names | deterministic code-unit name tie-break |
| same context `order`, different names | stable effective-map insertion order; no explicit name tie-break |
| two effective `complete` sections | `assemble()` rejects |
| waterfall replaces/adds sections while a complete section is active | waterfall still runs, but the originally resolved complete section is restored as the sole section |
| waterfall removes contexts while suppression is active | suppression is restored as `contexts: []` |
| bad/missing variable reference | rendering rejects, not assembly registration |
| duplicate tool schemas from additive providers | no registry deduplication; downstream receives duplicates unless another invariant rejects them |

## Concrete contributor sources

### Identity and host facts

| Concern | File / symbol / lines | Route |
|---|---|---|
| fixed Harness identity | `packages/core/system-prompt/src/index.ts:SystemPrompt.constructor`, 404-420 | global section `harness:identity`, order `-1000` |
| Harness checkout location | `packages/boot/app-boot/src/index.ts:addHarnessSourceSection`, 838-845 | global section `harness:source`, order `-900`; explicitly distinct from cwd |
| Web surface URL | `packages/bundle/web-app/src/index.ts:apply`, 235-251 | optional global section `app:web-surface`, order `-800`, text provider resolves the current local URL |
| deployment persona | `packages/core/system-prompt/src/index.ts:SystemPrompt.constructor`, 415-420 | global `deployment:persona`, order `0` |
| per-agent/preset persona | `packages/preset/persona/src/index.ts:apply`, 54-67 | scoped section with the same name/order shadows deployment persona; may set `complete` |

### Task, variables, tool guidance, dynamic state, and history

| Concern | File / symbol / lines | Route |
|---|---|---|
| task/user input | `packages/core/agent-loop/src/agent.ts:preStep`, 232-249; `turn`, 286-294 | Inbox messages are claimed, optionally transformed by `agent/pre-step`, then appended as durable `user/message` events before request construction. There is no `PromptSection` named “task”. |
| provider/model/cwd variables | `packages/core/agent-loop/src/index.ts:AgentLoop constructor`, 347-355 | global variable providers resolve from the active `context.agent` on every assembly. |
| tool schemas | `packages/core/tools/src/index.ts:Tools constructor`, 827-837; `wireSchemas`, 976-986 | anonymous provider supplies scope-filtered visible schemas to the assembly. |
| tool guidance | `packages/fs/tool-fs/src/read.ts:applyReadTool`, 64-83 | section `tool:read`, order `1100`, is independent of the `read` JSON schema registered below it. |
| Windows tool guidance | `packages/shell/tool-pwsh/src/index.ts:apply`, 195-249 | section `tool:pwsh`, order `1010`; schema registration begins at 251. |
| sandbox state | `packages/sandbox/sandbox-policy/src/index.ts:SandboxPolicy.constructor`, 101-123 | context `sandbox:policy`, order `110`, resolves per active session. |
| approval state | `packages/interaction/user-approval/src/index.ts:ApprovalService.constructor`, 162-180 | context `approval:policy`, order `115`; absent agent renders empty. |
| delegated child scope | `packages/subagent/subagent/src/child-agent.ts:applyChildComposition`, 199-210 | context `subagent:delegation`, order `120`; child persona and tool restriction are in the same scope. |
| time snapshot | `packages/context/time-context/src/index.ts:apply`, 145-208 | not a `PromptContext`; a prepended `agent/pre-step` listener appends a source-attributed plugin `UserMessage` to the current decision. |
| prior transcript/history | `packages/core/session/src/index.ts:Session.deriveMessages`, 699-745 | derives only the current ordered session surface; compaction replacement removes shadowed nodes and forces cache rebuild. |

The time plugin is counter-evidence to any claim that all dynamic context uses `SystemPrompt.context()`. Both paths ultimately become user-role messages, but only `PromptContext` uses `RuntimeContextProjection`'s complete-snapshot/change-suppression policy.

## Request assembly and provider boundary

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/agent-loop/src/agent.ts` | `preStep`, 232-249 | Every Step asks `SystemPrompt.assemble(assembleContextFor(agent, signal))`, renders named context sections, and projects a snapshot candidate. |
| same | `step`, 339-364 | System sections flatten with `renderPrompt`; request history comes from `Session.deriveMessages()`; then a prepared call or `ctx.llm.stream()` is invoked. |
| same | `buildRequest`, 442-501 | Route comes from agent options/persisted header plus `agent/request` waterfall; `LlmRuntime.prepareCall` resolves the exact adapter generation; canonical header snapshots config/system/tools. |
| same | `buildRequest`, 502-541 | Header changes are logged as initial/resume/change/series; final frozen request contains config, messages, optional system/tools, session id, and signal. |
| `packages/llm/llm/src/index.ts` | `LlmRuntime.prepareCall`, 881-934 | Binds normalized exact-model metadata, defaults, retry policy, and a one-shot adapter generation; reuse/config mismatch rejects. |
| same | `LlmRuntime.streamWithRegistration`, 1054-1063 | `llm/stream` waterfall is the final middleware seam before adapter streaming. |
| same | `adapterStream`, 958-1037 | Adapter resolution/dispatch/iteration are normalized into terminal failure chunks; middleware/consumer failures remain thrown. |
| `packages/llm/llm-deepseek/src/adapter.ts` | `DeepSeekAdapter.prepareCall`, 432-437 | Captures one connection generation and binds it to `streamWithConnection`. |
| same | `request`, 522-560, 565-648 | Resolves headers/credential/user/session, serializes a request, then `fetch`es `/chat/completions`. |
| `packages/llm/llm-deepseek/src/serialize.ts` | `serializeRequest`, 373-392 | Flattened `options.system` becomes one wire `system` message followed by serialized history messages. |

## Provenance boundary

1. Before render, `PromptAssembly.sections[]` and `.contexts[]` retain names and resolved text.
2. `renderContextSections()` retains `{ name, text }`; `RuntimeContextProjection` stores those in `MessageSource.form === 'snapshot'` with `sections`, so the durable history can attribute dynamic snapshot parts.
3. `renderPrompt()` returns only a flat string. `request/header` and `GenerateOptions.system` retain the flat effective value, not a list of contributing section names.
4. Tool schemas retain tool names/descriptions/parameters, but not the identity of the anonymous tool provider callback.
5. A model provider receives the final flattened system string, tools, and message history. It does not receive the pre-render `PromptAssembly` or section-level system provenance.

Therefore an “Effective Assembly receipt” can be captured at the assembly boundary, but the pinned runtime does not persist a general receipt for all four lanes. The dynamic snapshot's `source.sections` is the narrow existing provenance mechanism.

## Compaction and re-injection source facts

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/agent-loop/src/runtime-context.ts` | `RuntimeContextProjection.constructor`, 24-55 | Restores the latest retained owned snapshot from the current surface; if a replacement shadows that event, retained state becomes `null`. |
| same | `project`, 58-75 | Emits a complete current snapshot when text differs or retention was removed; emits an explicit cleared marker when no contexts remain. |
| `packages/compaction/compaction-basic/src/region.ts` | `commitCompactionBody`, 436-475 | Replaces a selected surface span with a summary `UserMessage`; this is history rewrite, not system-prompt mutation. |
| `packages/core/agent-loop/tests/loop.spec.ts` | owner cases, 442-508 | Candidate owner contracts explicitly cover re-emitting unchanged runtime context after its retained snapshot is replaced and emitting a clear marker if active context disappears. These lines are source evidence only here, not a fresh test result. |
| `packages/compaction/compaction-basic/src/region.ts` | `buildSummarizationInput`, 498-523 | The summarizer reuses the last routed flat system prompt and tool schemas plus only the selected region's derived messages. |

The pinned version **does** have narrow dynamic-context re-injection after a compaction replacement removes the retained snapshot. Stable system sections are not “re-injected into history”; they are reassembled each Step and placed in the request header/system field. There is no generic invariant registry that replays arbitrary task/plugin messages after compaction.

## Counter-evidence and limits to preserve

- No production `IContextContributor` or general `Receipt` exists in the pinned tree; that remains a BuildPilot proposal.
- “PromptContext” is not the full model context: task messages, time context, history, tool schemas, and the system string take different paths.
- A `PromptSection` may itself be dynamic because `text` can be a callback; “Section = stable” is a convention, not a type guarantee.
- A `PromptContext` snapshot becomes durable history only if `RuntimeContextProjection.project()` decides the current full snapshot differs or was removed.
- System-section provenance is lost when `renderPrompt()` flattens it. A provider request trace alone cannot reconstruct section contributors unless assembly or registration was separately recorded.
- `system-prompt/assemble` can transform the assembly. The waterfall return is authoritative except for complete-section and context-suppression restoration.
- Same-layer duplicates reject; cross-scope same names shadow. These are different behaviors and must not be collapsed into “last write wins”.
- Tool-provider aggregation is additive and can expose duplicate tool names; the section/context invariant does not close that gap.
- Compaction re-injection is specific to the runtime-context projection source marker. There is no evidence here that arbitrary removed plugin context or task messages are regenerated.
- Source paths prove implementation shape, not two-Step request values, a provider call, or a wire receipt. Those remain Lab responsibilities.

## Source verdict

`PASS` for `SOURCE_MAP`: the pinned source closes the production path from concrete contributor classes through ordered/scoped assembly, durable dynamic-context projection, history derivation, frozen request construction, LLM runtime dispatch, DeepSeek serialization, and HTTP request. Negative and provenance boundaries are explicit. The next allowed gate is `EXPERIMENT_DESIGN`.
