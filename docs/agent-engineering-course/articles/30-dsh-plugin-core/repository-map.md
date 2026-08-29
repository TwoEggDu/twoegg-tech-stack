# Article 30 Repository Map

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / SOURCE MAP COMPLETE`

> 时间语义：本文记录 Source Investigation Gate 的静态源码结论。该 Gate 不执行实验；owner tests 只作为“仓库声明并维护了哪些行为”的测试资产证据，不等于本轮已观察到测试通过。后续实验结果应写入 `experiments/plugin-lifecycle-trace.md`，Claim 最终状态以 Research/Evidence Merge 为准。

## 1. Frozen source and verification boundary

- Official repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Verified at: `2026-08-30 / Asia/Shanghai`
- Fresh check: `origin` is the official repository; `HEAD` and dereferenced tag both equal the frozen commit; `git status --porcelain=v1 --untracked-files=all` returned no rows.

Primary evidence is pinned tracked implementation plus its tracked owner tests. Generated `lib/**`, `node_modules`, runtime profile materialization, session logs, and current process state are not used to fill source arrows. This role ran no test, CLI, Loader fixture, or lifecycle probe.

The vendored framework is part of the pinned DSH source of record. `vendor/cordis/src/**` owns `Context`, `Registry`, `Fiber`, `Service`, event registration and reversible effects; `vendor/loader/src/**` owns configured-entry import/start/update/disposal. Package adjacency, comments, manifests, and YAML rows are never treated as call edges by themselves.

## 2. Representative plugin decision

The representative real plugin is `@deepseek-ai/dsh-time-context` (`packages/context/time-context`). It was selected because one small production plugin exposes the complete article-sized seam without importing unrelated capability code:

1. a real opt-in composition row names it;
2. its namespace exports Loader metadata (`name`, `inject`, `Config`, `apply`);
3. it requires the real `agents` Cordis service;
4. `apply` registers an `agent/pre-step` waterfall listener;
5. `ctx.on` lowers that listener to a Fiber-owned effect;
6. an agent-scoped dispatch selects the listener and passes the exact Agent payload;
7. the plugin contributes a model-visible `UserMessage` that the Agent Loop later appends as a durable Session event;
8. the owner lifecycle test disposes the plugin Fiber and asserts that a second dispatch adds no second contribution;
9. the real Loader-path test and headless Loader smoke anchor namespace unwrapping and persisted ordering.

This choice has one deliberate limitation: `time-context` is a **consumer**, not the provider, of `agents`. The service-registration edge therefore belongs to the representative composition: `AgentRegistry extends Service -> super(ctx, 'agents') -> ctx.reflect.provide`. The plugin itself contributes an event listener, not a new service or Tool.

## 3. Vocabulary firewall

| Term | Exact object in this map | Must not be conflated with |
|---|---|---|
| Plugin Context | Cordis `Context` proxy extended with the plugin's `Fiber`; passed to `apply(ctx, config)` | model input/history, token context window, `SessionEventMap.RequestContext` |
| Model context | `UserMessage[]`/system/tool material that enters `GenerateOptions`; here the time reading becomes one sourced user-role message | Cordis DI container |
| Plugin Event | process-local Cordis event, here `agent/pre-step`; dispatch is a waterfall and is not durable by itself | append-only Session event |
| Session Event | JSON-safe record appended by `Session.append`, here eventually `user/message`, `step/start`, etc. | Cordis hook invocation |
| Scope | opaque DSH identity carried by `scopeTarget` plus optional registration scope created by `createScope` | JavaScript lexical scope or service isolation label |
| Effect | Fiber-owned disposer produced by `ctx.effect`, directly or through helpers such as `ctx.on`/`ctx.provide` | model side effect or arbitrary function call |
| Plugin | Loader/Cordis function, class, or `{ apply }` object with optional metadata | Tool definition exposed to a model |
| Tool | `ToolDefinition` registered through `ToolRuntime.register`, visible/dispatchable through tool layers | every plugin or event listener |

Specific negative finding: `time-context` does **not** call `ctx.tools.register`; it is therefore not a Tool. It also does not append a Session event inside `apply`. Its listener returns a modified pre-step decision; `ReactLoopAgent.turn` owns the subsequent `Session.append('user/message', ...)`.

## 4. Core object model

| Object | Source owner | Construction/registration | Lifetime owner | Observable responsibility |
|---|---|---|---|---|
| Root `Context` | `vendor/cordis/src/context.ts:Context` | `new Context()` installs root Fiber, Reflect, Registry, Events, Logger | root Fiber | DI/service lookup, plugin registry, event methods and effect methods via proxy/mixins |
| Loader `Entry` | `vendor/loader/src/config/entry.ts:Entry` | config tree creates one object per row | containing Entry tree/Fiber | retains raw options, imported plugin Fiber and disabled state |
| Plugin `Runtime` | `vendor/cordis/src/registry.ts:Plugin.Runtime` | one registry record per callback identity | Registry | shared metadata plus all live Fibers for that callback |
| Plugin `Fiber` | `vendor/cordis/src/fiber.ts:Fiber` | `RegistryService.plugin` creates it with normalized inject map | parent Fiber via the `ctx.plugin()` effect | activation state, resolved service store, config, effects, unload/reload/dispose |
| Plugin `Context` | `Fiber.ctx` | `parent.extend({ fiber: this })` | same Fiber | makes every registration performed by `apply` belong to that Fiber |
| Service implementation | `vendor/cordis/src/reflect.ts:Impl` | `ctx.provide` or `Service` constructor | provider Fiber effect | named runtime API plus provider identity/check predicate |
| Event hook | `vendor/cordis/src/events.ts:Hook` | `ctx.on` -> `EventsService.register` | registering Fiber effect | callback, registering Context and filter/global/prepend metadata |
| DSH scope carrier | `packages/core/scope/src/index.ts:scopeTarget` | opaque object with `Context.filter` | transient routing value | admits untagged global listeners and matching/ancestor-scoped listeners |
| DSH registration Scope | `createScope` | no-op Cordis plugin creates a backing Fiber and tagged derived Context | returned Scope/Fiber | groups scoped registrations under one quiescent disposer |
| Agent service | `packages/core/agent/src/index.ts:AgentRegistry` | class plugin constructor calls `super(ctx, 'agents')` | AgentRegistry Fiber | satisfies `time-context.inject = ['agents']`; owns agent runtime APIs |
| `time-context` plugin | `packages/context/time-context/src/index.ts` namespace plugin | Loader unwraps namespace; Registry invokes `apply` after dependency/config resolution | its plugin Fiber | prepended `agent/pre-step` listener and formatter cache |

## 5. Configured is not activated

The shipped Schedule example contributes a patch row:

```yaml
- insert:
    - id: time-context
      name: '@deepseek-ai/dsh-time-context'
```

That row proves only **configured membership**. The activation chain adds all of the following gates:

```text
Entry options
  -> disabled/ancestor-disabled evaluation
  -> module import
  -> Loader.unwrapExports(namespace)
  -> RegistryService.plugin(plugin, config)
  -> Fiber created in PENDING state
  -> inject map normalized (`agents`)
  -> active matching service implementation resolved
  -> Config schema resolves
  -> apply(ctx, config)
  -> listener effect registered
  -> Fiber settles ACTIVE
```

`Entry._start` awaits `fiber.await`; app boot separately rejects enabled entries that remain non-ACTIVE. The repository even owns a real Loader test whose unresolved `neverProvided` injection remains pending and is reported as “did not activate”. Therefore the allowed wording is “the row configures the plugin” until Fiber activation is established.

## 6. Edge legend

| Edge | Meaning | Non-meaning |
|---|---|---|
| `CONFIG_ROW` | a Loader tree row names a module and config | imported, activated or operational |
| `IMPORT` | Loader resolves/imports and unwraps a module namespace | plugin callback completed |
| `PLUGIN_START` | Registry created a Fiber and scheduled activation | Fiber is ACTIVE |
| `SERVICE_PROVIDE` | provider Fiber registered a named implementation | consumer already resolved it |
| `SERVICE_INJECT` | consumer Fiber requires a name and stays pending without it | constructor parameter injection or arbitrary service locator access |
| `EFFECT_REGISTER` | disposer was collected by the current Fiber | effect ran in a separate process/thread |
| `PLUGIN_EVENT` | process-local Cordis event listener/dispatch edge | durable history |
| `SCOPE_FILTER` | dispatch carrier admits a listener's Context | service isolation or authorization |
| `DECISION_TRANSFORM` | waterfall listener returns a modified pre-step decision | Session already changed |
| `SESSION_APPEND` | Agent Loop appends JSON-safe durable event data | persistence backend flush completed |
| `DISPOSE` | owner invokes Fiber/effect disposer and awaits teardown | process exit or Session deletion |
| `OWNER_TEST` | tracked test asserts the relationship | this Source Investigation executed and passed it |

## 7. Exact owner map

| Plane | Exact source anchors | Relationship closed here | Status |
|---|---|---|---|
| Composition | `apps/cli/config/examples/schedule/cordis.yml:1-9`; time-context test fixture `cordis.yml` | opt-in row and real Loader test topology exist | `SOURCE_CONFIRMED`; runtime not observed |
| Loader | `vendor/loader/src/config/entry.ts:83-120,258-301`; `vendor/loader/src/index.ts:192-199` | disabled check, import, namespace unwrap, Registry start, await and rollback/dispose | `SOURCE_CONFIRMED` |
| Plugin registry | `vendor/cordis/src/registry.ts:65-87,316-335` | inject normalization, runtime identity and Fiber creation | `SOURCE_CONFIRMED` |
| Dependency lifecycle | `vendor/cordis/src/fiber.ts:222-319,597-696`; `reflect.ts:277-304` | missing service keeps Fiber inactive; provider changes trigger unload/reload; service removal is an effect | `SOURCE_CONFIRMED` |
| Service provider | `vendor/cordis/src/service.ts:5-42`; `core/agent/src/index.ts:246-289` | AgentRegistry class plugin provides `agents` and owns other effects | `SOURCE_CONFIRMED` |
| Plugin activation | `context/time-context/src/index.ts:20-38,127-170` | namespace metadata, config validation and `apply` setup | `SOURCE_CONFIRMED` |
| Event/effect | `time-context/index.ts:170-208`; `vendor/cordis/src/events.ts:254-301`; `fiber.ts:402-560` | `ctx.on` registers a hook through a single-shot reversible Fiber effect | `SOURCE_CONFIRMED` |
| Scoped operation | `agent/src/dispatch.ts:94-148`; `scope/src/index.ts:158-184`; `events.ts:165-174,234-242` | exact Agent is fused into payload; carrier filters hooks; waterfall reaches listener | `SOURCE_CONFIRMED` |
| Model-visible contribution | `time-context/index.ts:174-208`; `agent-loop/src/agent.ts:232-300` | listener delegates first, appends a sourced UserMessage to decision, Agent Loop later opens Step and appends it | `SOURCE_CONFIRMED` |
| Durable event | `core/session/src/types.ts:215-301`; `core/session/src/index.ts:602-652` | `user/message` is Session vocabulary; append validates/freezes/pushes before live observers | `SOURCE_CONFIRMED`; backend flush outside this trace |
| Teardown | `fiber.ts:265-297,675-696`; `events.ts:254-274`; time-context owner test `time-context.spec.ts:398-409` | Fiber unload runs hook unregister disposer; owner test asserts no post-disposal contribution | `SOURCE_CONFIRMED + OWNER_TEST_DECLARED`; test execution pending |
| Loader e2e | `time-context.spec.ts:481-500`; `time-context.e2e.ts:30-83` | namespace Loader path and headless JSONL ordering are owner-tested | `OWNER_TEST_DECLARED`; no current run |

## 8. Source-confirmed edge register

| ID | Exact edge | Proves | Limitation | Status |
|---|---|---|---|---|
| `30-S01` | Schedule patch inserts `@deepseek-ai/dsh-time-context` | real opt-in composition candidate | Schedule overlay/profile was not booted | `SOURCE_CONFIRMED / CONFIG_ONLY` |
| `30-S02` | `Entry.init/_init` imports row module and calls `Loader.unwrapExports` | namespace export reaches Loader normalization | import success not observed | `SOURCE_CONFIRMED` |
| `30-S03` | `Entry._start` -> `ctx.registry.plugin` -> `fiber.await` | Loader delegates lifecycle to Cordis and awaits settlement | enabled row may still fail or stay pending | `SOURCE_CONFIRMED` |
| `30-S04` | Registry normalizes `plugin.inject` and creates `Fiber` | `inject = ['agents']` becomes a required-name map | not constructor DI; name/type correctness still compile/config owned | `SOURCE_CONFIRMED` |
| `30-S05` | `AgentRegistry` constructor -> `Service` constructor -> `reflect.provide('agents', instance)` | real provider publishes required service under provider Fiber | AgentRegistry activation itself is a prerequisite | `SOURCE_CONFIRMED` |
| `30-S06` | `Fiber._checkImpl/_refresh` builds epoch from active provider Fiber uid | consumer activates only with matching active provider | YAML order alone is insufficient | `SOURCE_CONFIRMED` |
| `30-S07` | `Fiber._reload` resolves config then invokes namespace `apply` | config/dependencies precede plugin body | a thrown validation/setup error makes Fiber FAILED/PENDING | `SOURCE_CONFIRMED` |
| `30-S08` | `time-context.apply` validates refresh/timezone and calls `ctx.on('agent/pre-step', ..., { prepend: true })` | plugin's only runtime contribution is a prepended waterfall hook | it does not register a service or Tool | `SOURCE_CONFIRMED` |
| `30-S09` | `ctx.on` -> `EventsService.register` -> `ctx.fiber.effect` -> hook-list insert + unregister disposer | listener ownership is the plugin Fiber | listener registration is process-local, not durable | `SOURCE_CONFIRMED` |
| `30-S10` | `agentEvents(...).waterfall` uses `scopeTarget(agent, agent)`; Cordis dispatch applies `Context.filter` | operation is routed for the exact Agent subject and matching/global listener contexts | time-context's untagged Context is global, not a per-Agent plugin instance | `SOURCE_CONFIRMED` |
| `30-S11` | time listener `await next()` then returns `decision.messages + createUserMessage(source.kind='plugin')` | waterfall contribution and attribution are explicit | rejected/aborted decisions contribute nothing; no append yet | `SOURCE_CONFIRMED` |
| `30-S12` | Agent Loop accepted decision -> `step/start` -> `Session.append('user/message', message)` | plugin contribution becomes durable Session vocabulary before model call | persistence provider flush not covered | `SOURCE_CONFIRMED` |
| `30-S13` | plugin `fiber.dispose` -> epoch inactive -> `_unload` -> collected hook effect -> `unregister` | disposal removes contribution mechanism | in-flight callback already running is not retroactively cancelled by unregister | `SOURCE_CONFIRMED` |
| `30-S14` | owner lifecycle test dispatches, disposes Fiber, dispatches again, expects one total context | source/test relation specifically covers listener removal | this investigation did not run it | `OWNER_TEST_DECLARED` |
| `30-S15` | owner real-loop test expects one sourced message per request and no system/header contamination | contribution enters user-message history, not system prompt/header | deterministic adapter is not a real provider | `OWNER_TEST_DECLARED` |
| `30-S16` | owner Loader smoke inspects JSONL and expects ordered plugin-attributed events | deployable Loader topology and persisted projection have coverage | passing runtime remains Lab evidence | `OWNER_TEST_DECLARED` |

## 9. Dependency and ordering constraints

1. `inject = ['agents']` is mandatory. Without an ACTIVE matching `agents` provider, the plugin Fiber remains PENDING and `apply` does not run.
2. Provider replacement is an epoch change, not an in-place pointer swap. Fiber unloads its effects and reloads against the new provider uid.
3. `ctx.on(..., { prepend: true })` places this listener before hooks already in the same hook list. It does not create a universal order across plugins that have not activated yet.
4. Waterfall semantics require `await next()`. `time-context` deliberately delegates first; a downstream reject/throw prevents its contribution. Returning without `next()` would veto inner listeners/default behavior.
5. The selected plugin samples time after downstream acceptance and before Agent Loop appends `step/start`/`user/message`. The owner test explicitly covers downstream throw/cancel producing no durable reading.
6. Scope filtering is event routing, not DI. Untagged listeners remain global. A plugin mounted through a tagged `createScope(...).ctx` would be filtered, but the selected shipped row is not proven to mount that way.
7. Service names are runtime string keys. TypeScript module augmentation describes `ctx.agents`, but runtime access still depends on declared injection, provider availability and matching isolation scope.

## 10. Counter-evidence and anti-overclaims

| Tempting claim | Pinned counter-evidence | Required wording |
|---|---|---|
| “Everything in YAML is active” | disabled entries have no Fiber; missing injected services leave PENDING Fibers; app boot audits ACTIVE state | configured candidate versus activated Fiber |
| “Every plugin is a service” | `time-context.apply` only calls `ctx.on`; `AgentRegistry` is the separate Service provider | plugins may provide services, consume services, or only register effects |
| “Every plugin is a Tool” | Tool definitions have a separate `ToolRuntime.register` path; time-context never calls it | plugin and Tool are orthogonal concepts |
| “Plugin Context is model context” | Cordis Context owns DI/effects; listener creates a UserMessage that later enters model history | explicitly name Cordis Context versus model-visible messages |
| “Plugin Event is Session Event” | `agent/pre-step` runs before Session append; `Session.append('user/message')` is a later Agent Loop action | process-local interception versus durable vocabulary |
| “Scoped dispatch means one plugin instance per Agent” | `scopeTarget` admits untagged global listeners; selected plugin row is untagged | exact Agent-scoped operation through one global listener |
| “Dispose deletes prior contribution” | unregister removes future hook calls; existing durable `user/message` events remain in Session history | reversible registration, not history erasure |
| “ctx.on return is the only owner” | hook disposer is also collected by the current Fiber; Fiber unload invokes it | manual disposer and structural Fiber owner are both valid teardown paths |
| “Dependency means import order” | Fiber resolves active implementations dynamically and reloads on provider uid change | lifecycle relation, not list-order inference |
| “Owner test proves current pass” | no test command was run in this Gate | tracked test declares coverage; runtime result pending |

## 11. BuildPilot boundary

Safe abstractions to carry forward:

- contribution APIs should return exact disposers;
- one owner scope should collect every reversible registration;
- dependency availability and activation state must be observable separately from configuration membership;
- model-visible contributions must cross an explicit durable append boundary;
- scoped dispatch should carry an explicit subject/key instead of relying on ambient state;
- teardown removes future behavior but does not rewrite historical records.

Not safe to copy yet:

- the entire Cordis `Context` proxy/service-isolation mechanism;
- string-keyed plugin DI as BuildPilot's default public API;
- Loader HMR/reload epochs;
- DSH's exact waterfall/event vocabulary;
- “Everything is a Plugin” as a product requirement. BuildPilot remains `SIMPLIFY` until its actual extension and lifecycle needs justify these mechanisms.

## 12. Source-stage verdict

- Representative plugin: `@deepseek-ai/dsh-time-context`.
- Static lifecycle chain: `CLOSED` from configured Loader row through import, dependency resolution, activation, event/effect registration, Agent-scoped operation, durable-message handoff, and Fiber-owned listener removal.
- Central disposal relationship: `SOURCE_CONFIRMED + OWNER_TEST_DECLARED`.
- Runtime/lab result: `PENDING`; no command was executed by Source Investigator.
- Main limitation: the selected plugin is a global consumer of `agents`, not a per-Agent scoped plugin instance and not a Tool/service provider.
- Next allowed gate at this historical snapshot: `EXPERIMENT_DESIGN`.
