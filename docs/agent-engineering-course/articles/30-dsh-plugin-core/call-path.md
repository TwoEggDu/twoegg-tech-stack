# Article 30 Plugin Lifecycle Call Path

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / CALL PATH CLOSED`

> 本文只闭合 pinned source 与 tracked owner tests 的调用关系，不声称本轮执行过 Loader、owner test 或生命周期实验。后续 runtime 结果必须进入 `experiments/plugin-lifecycle-trace.md`，不能回填成此处的静态观察。

## 1. Selected path and endpoint

Selected real plugin: `@deepseek-ai/dsh-time-context` at frozen DSH commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`.

Path start: Loader receives a configured row naming the plugin.

Operation endpoint: an accepted Agent `pre-step` receives one sourced `UserMessage`, which Agent Loop appends as a durable `user/message` Session event.

Disposal endpoint: disposing the plugin Fiber unloads the `ctx.on('agent/pre-step')` effect and removes the listener, so later dispatches cannot produce another time-context contribution. Existing Session events are retained.

## 2. Phase A — configured row to imported plugin namespace

| Step | Caller -> callee | File and exact anchor | Relation | Proof and limitation | Status |
|---:|---|---|---|---|---|
| 1 | Schedule overlay -> `time-context` Loader row | `apps/cli/config/examples/schedule/cordis.yml:1-9` | `CONFIG_ROW` | real shipped opt-in patch names the package; it does not prove profile boot or activation | `SOURCE_CONFIRMED / CONFIG_ONLY` |
| 2 | Loader tree -> `Entry` | `vendor/loader/src/config/entry.ts:55-69` | `CONSTRUCT` | Entry owns options, derived Context and optional Fiber | `SOURCE_CONFIRMED` |
| 3 | `Entry.refresh/update` -> disabled evaluation | `entry.ts:83-120,122-170` | `GATE` | own/ancestor disabled state can prevent `init`; configured is not active | `SOURCE_CONFIRMED` |
| 4 | `Entry.init` -> `_init` -> tree `import(options.name)` | `entry.ts:258-288` | `IMPORT` | imports exact configured module; wraps import/apply failures with entry identity | `SOURCE_CONFIRMED` |
| 5 | imported namespace -> `Loader.unwrapExports` | `entry.ts:277-285`; `vendor/loader/src/index.ts:192-199` | `NORMALIZE` | namespace without default remains the plugin object; owner test asserts time-context metadata survives | `SOURCE_CONFIRMED + OWNER_TEST_DECLARED` |
| 6 | `Entry._start` -> `_patchContext` | `entry.ts:291-297,114-120` | `CONTEXT_LINK` | entry Context inherits current parent tree Context before start | `SOURCE_CONFIRMED` |
| 7 | `_start` -> `ctx.registry.plugin(plugin, config)` -> `fiber.await()` | `entry.ts:291-300` | `PLUGIN_START / SETTLEMENT` | Loader retains returned Fiber and waits; rejected start is disposed | `SOURCE_CONFIRMED` |

The `time-context` owner “real Loader export path” test (`time-context.spec.ts:481-500`) asserts the normalized namespace has `name`, `inject`, `Config`, and `apply`, then mounts it. This is a precise owner-test relation, not a current execution result.

## 3. Phase B — Registry, dependency and service activation

| Step | Caller -> callee | File and exact anchor | Relation | Proof and limitation | Status |
|---:|---|---|---|---|---|
| 8 | `RegistryService.plugin` -> `resolve(plugin)` | `vendor/cordis/src/registry.ts:221-240,316-320` | `PLUGIN_SHAPE` | accepts function, class or `{ apply }`; namespace resolves to `apply` callback | `SOURCE_CONFIRMED` |
| 9 | plugin metadata -> `Inject.resolve(['agents'])` | `registry.ts:65-87,330` | `SERVICE_INJECT` | array becomes `{ agents: null }`; runtime string key, not constructor injection | `SOURCE_CONFIRMED` |
| 10 | Registry -> new `Fiber(parent, config, inject, runtime)` | `registry.ts:322-334`; `fiber.ts:222-245` | `FIBER_CREATE` | Fiber gets uid, derived plugin Context, raw config and inject map | `SOURCE_CONFIRMED` |
| 11 | parent Fiber -> child disposer effect | `fiber.ts:265-297` | `OWNERSHIP` | `ctx.plugin()` itself is an effect of the parent; parent teardown owns child disposal | `SOURCE_CONFIRMED` |
| 12 | new Fiber -> `internal/plugin` -> `_checkImpl('agents')` | `fiber.ts:299-319,597-609` | `DEPENDENCY_RESOLVE` | Loader may extend inject during publication; active matching provider is captured afterward | `SOURCE_CONFIRMED` |
| 13 | `AgentRegistry` class plugin constructor -> `super(ctx, 'agents')` | `packages/core/agent/src/index.ts:246-258`; `vendor/cordis/src/service.ts:32-58` | `SERVICE_PROVIDE` | real service provider registers under exact required name | `SOURCE_CONFIRMED` |
| 14 | `Service` constructor -> `ctx.reflect.provide` -> provider Fiber effect | `service.ts:35-58`; `reflect.ts:277-304` | `SERVICE_PROVIDE / EFFECT_REGISTER` | implementation and provider Fiber enter Reflect store; disposer removes them and notifies dependents | `SOURCE_CONFIRMED` |
| 15 | consumer `_refresh` -> epoch from provider Fiber uid | `fiber.ts:611-638` | `ACTIVATION_GATE` | missing provider yields `INACTIVE`; available provider schedules `_reload` | `SOURCE_CONFIRMED` |
| 16 | `_reload` -> `_resolveConfig` -> runtime `Config` schema | `fiber.ts:641-672`; `fiber.ts:42-61` | `CONFIG_VALIDATE` | validated config precedes callback; schema issues reject activation | `SOURCE_CONFIRMED` |
| 17 | `_reload` -> `_execute(_runner)` -> `runtime.callback(ctx, config)` | `fiber.ts:646-657,247-263` | `PLUGIN_APPLY` | function plugin invokes `time-context.apply` on its Fiber-derived Context | `SOURCE_CONFIRMED` |
| 18 | settled Fiber -> ACTIVE or FAILED/PENDING | `fiber.ts:574-595,646-709`; `app-boot/src/index.ts:707-735` | `STATE` | only ACTIVE is activation; boot reports pending injected services | `SOURCE_CONFIRMED` |

Ordinary DI boundary:

```text
compile-time: module augmentation describes ctx.agents
runtime:      inject metadata names "agents"
provider:     AgentRegistry registers "agents" in Reflect store
resolver:     Fiber captures active implementation in matching isolation label
access:       Context proxy rejects undeclared/inactive required-service reads
```

The mechanism supplies lifecycle-aware named DI. It does not prove dependency semantics from TypeScript imports, package manifests, YAML order, or property presence alone.

## 4. Phase C — plugin apply to reversible listener effect

| Step | Caller -> callee | File and exact anchor | Relation | Result | Status |
|---:|---|---|---|---|---|
| 19 | `apply` -> `validateRefreshInterval` | `time-context/src/index.ts:127-148` | `VALIDATE` | invalid negative/fractional/unsafe values fail at load | `SOURCE_CONFIRMED` |
| 20 | `apply` -> `createTimestampFormatter` | `index.ts:149-168` | `SETUP` | configured/system zone resolves once; failure rejects activation | `SOURCE_CONFIRMED` |
| 21 | `apply` -> `ctx.on('agent/pre-step', listener, { prepend: true })` | `index.ts:170-208` | `PLUGIN_EVENT_REGISTER` | registers the plugin's only operational contribution | `SOURCE_CONFIRMED` |
| 22 | `ctx.on` -> Fiber active assertion -> listener bind | `vendor/cordis/src/events.ts:288-301` | `REGISTER_GATE` | inactive Context cannot create new effects | `SOURCE_CONFIRMED` |
| 23 | `ctx.on` -> `EventsService.register(label, hooks, callback, options)` | `events.ts:254-260,299-301` | `CALL` | prepended hook record retains registering plugin Context | `SOURCE_CONFIRMED` |
| 24 | `register` -> `ctx.fiber.effect(execute, label)` | `events.ts:254-259`; `fiber.ts:402-560` | `EFFECT_REGISTER` | effect inserts hook and returns unregister disposer; Fiber collects wrapper | `SOURCE_CONFIRMED` |
| 25 | Fiber setup settles | `fiber.ts:655-672,704-709` | `ACTIVE` | Loader await can now return ACTIVE if callback did not fail | `SOURCE_CONFIRMED` |

There is no direct `ctx.effect` call in `time-context`, but `ctx.on` is not a side table outside lifecycle ownership: it is explicitly implemented as a Fiber effect. Likewise, `AgentRegistry`'s service registration is a `ctx.provide` effect. Helpers preserve the same disposer model.

## 5. Phase D — exact Agent-scoped operation

| Step | Caller -> callee | File and exact anchor | Relation | Result | Status |
|---:|---|---|---|---|---|
| 26 | `ReactLoopAgent.preStep` -> `this.dispatch.waterfall('agent/pre-step', payload, default)` | `core/agent-loop/src/agent.ts:232-249` | `PLUGIN_EVENT_DISPATCH` | dispatch happens after inbox claim/context projection but before accepted Step append | `SOURCE_CONFIRMED` |
| 27 | per-Agent dispatcher -> `agentCarrier(agent)` -> `scopeTarget(agent, agent)` | `core/agent/src/dispatch.ts:84-107` | `SCOPE_CARRIER` | the exact Agent is both payload subject and opaque scope key | `SOURCE_CONFIRMED` |
| 28 | dispatcher -> Cordis `waterfall(carrier, name, fusedPayload, next)` | `dispatch.ts:107-147` | `FUSED_DISPATCH` | caller cannot override injected `agent` field | `SOURCE_CONFIRMED` |
| 29 | `EventsService.dispatch` -> carrier `Context.filter` over hooks | `vendor/cordis/src/events.ts:165-174`; `core/scope/src/index.ts:170-184` | `SCOPE_FILTER` | untagged hooks are global; matching/ancestor-tagged hooks are admitted | `SOURCE_CONFIRMED` |
| 30 | Cordis `waterfall` -> ordered callbacks -> inner default | `events.ts:224-242` | `WATERFALL` | each listener must call `next()` to delegate; prepend affects hook position | `SOURCE_CONFIRMED` |
| 31 | time listener -> `await next()` | `time-context/src/index.ts:170-175` | `DELEGATE_FIRST` | downstream rejection/throw/abort prevents contribution | `SOURCE_CONFIRMED` |
| 32 | accepted listener -> inspect Agent Session events and proposed messages | `index.ts:54-107,176-187` | `READ` | per-Agent timing and browser-zone input derive from exact payload Agent | `SOURCE_CONFIRMED` |
| 33 | listener -> render clock text -> `createUserMessage` | `index.ts:110-125,183-205` | `MODEL_CONTEXT_CONTRIBUTION` | sourced snapshot is a user-role message, not Cordis Context mutation | `SOURCE_CONFIRMED` |
| 34 | listener -> return `{ ...decision, messages: [..., contributed] }` | `index.ts:198-207` | `DECISION_TRANSFORM` | no Session mutation occurs inside the plugin | `SOURCE_CONFIRMED` |

Important scope nuance: the exact **operation** is Agent-scoped because `agentEvents` dispatches through the Agent carrier. The selected plugin row itself is not shown to be mounted under a per-Agent `createScope` Context, so its untagged hook is global and receives every admitted Agent event. This is one global plugin Fiber performing per-Agent work, not one plugin Fiber per Agent.

## 6. Phase E — plugin decision to durable Session event

| Step | Caller -> callee | File and exact anchor | Relation | Result | Status |
|---:|---|---|---|---|---|
| 35 | `preStep` returns accepted decision to `turn` | `agent-loop/src/agent.ts:241-249,270-286` | `RETURN` | rejected/empty decisions take separate branches | `SOURCE_CONFIRMED` |
| 36 | `turn` -> `Session.append('step/start')` | `agent.ts:285-287` | `SESSION_APPEND` | durable Step boundary opens before messages | `SOURCE_CONFIRMED` |
| 37 | `turn` loops decision messages -> `Session.append('user/message', ..., surfaceOp:'append')` | `agent.ts:288-291` | `SESSION_APPEND` | time-context snapshot becomes model-visible durable vocabulary | `SOURCE_CONFIRMED` |
| 38 | `Session.append` -> JSON snapshot/validation/freeze | `core/session/src/index.ts:602-632` | `VALIDATE` | non-JSON event data is rejected before log mutation | `SOURCE_CONFIRMED` |
| 39 | `Session.append` -> `log.push(event)` -> contained `session/event` observers | `session/src/index.ts:634-646` | `COMMIT_THEN_LIVE_EVENT` | durable in-memory log precedes live observer callbacks | `SOURCE_CONFIRMED` |
| 40 | Session surface -> later LLM request projection | `SessionEventMap.user/message` at `session/src/types.ts:215-250`; Agent Loop step path | `MODEL_CONTEXT` | contribution is ordinary sourced history; not system header or Tool | `SOURCE_CONFIRMED` |

This closes the terminology split:

```text
Cordis Plugin Event (`agent/pre-step`, process-local)
  -> PreStepDecision.messages (proposed model input)
  -> Session Event (`user/message`, durable in-memory log)
  -> model request history projection
```

A live Plugin Event is not durable. A returned message is not durable until Agent Loop appends it. A Session append is not proof that an external persistence provider flushed it; persistence is Article 34 territory.

## 7. Phase F — Fiber/context disposal and effect reversal

| Step | Caller -> callee | File and exact anchor | Relation | Result | Status |
|---:|---|---|---|---|---|
| 41 | explicit `fiber.dispose()` or parent teardown -> child plugin disposer | `vendor/cordis/src/fiber.ts:265-297` | `DISPOSE` | uid cleared, runtime membership removed, epoch becomes inactive | `SOURCE_CONFIRMED` |
| 42 | epoch inactive -> `_unload()` | `fiber.ts:625-638,675-696` | `UNLOAD` | Fiber drains all collected effect wrappers and clears service snapshot | `SOURCE_CONFIRMED` |
| 43 | hook effect wrapper -> its disposer list in reverse registration order | `fiber.ts:402-441,504-560` | `EFFECT_DISPOSE` | single-shot disposer is idempotent and joins async cleanup | `SOURCE_CONFIRMED` |
| 44 | listener effect disposer -> `EventsService.unregister(hooks, callback)` | `events.ts:254-274` | `UNREGISTER` | exact hook record is spliced from event hook list | `SOURCE_CONFIRMED` |
| 45 | later `agent/pre-step` dispatch -> hook absent | dispatch/filter path above | `NO_FUTURE_CONTRIBUTION` | no new time-context message from disposed Fiber | `SOURCE_CONFIRMED` |
| 46 | existing Session `user/message` events | Session append-only log | `RETAIN` | prior contribution remains; disposal does not rewrite history | `SOURCE_CONFIRMED` |

The owner lifecycle test at `time-context/tests/time-context.spec.ts:398-409` encodes steps 41-45 directly:

```text
dispatch step 1 -> one time-context message
await pluginFiber.dispose()
dispatch step 2 -> still exactly one message
```

This is stronger than inferring cleanup from a returned callback, because it checks the representative plugin's listener through the same event surface after Fiber disposal. It remains `OWNER_TEST_DECLARED` until the Lab runs it.

## 8. Provider-loss and replacement side path

The selected happy path starts with `agents` already active in the unit harness, but Cordis source also closes the dynamic dependency path:

```text
provider ctx.provide('agents') disposer
  -> remove Reflect implementation
  -> reflect.notify(['agents'])
  -> every matching consumer Fiber _checkImpl('agents')
  -> consumer epoch becomes INACTIVE
  -> consumer _unload()
  -> time-context listener unregisters

new matching agents provider
  -> reflect.notify(['agents'])
  -> consumer captures new provider Fiber uid
  -> epoch changes
  -> config resolves again
  -> time-context apply runs again
  -> a fresh listener effect is registered
```

Exact anchors: `vendor/cordis/src/reflect.ts:277-335`; `fiber.ts:597-696`.

This is lifecycle-aware dependency reactivation. It is not evidence that arbitrary plugin state migrates across reloads. `time-context`'s formatter Map is closure state and is recreated by a fresh `apply` invocation.

## 9. `createScope` side mechanism, separated from the selected row

`packages/core/scope/src/index.ts:104-146` implements registration scopes by mounting a no-op Cordis plugin, deriving a tagged Context from its Fiber Context, and exposing `rawDispose` plus a quiescent `dispose`. Its owner tests assert:

- registrations made synchronously through `scope.ctx` are owned by the backing Fiber;
- repeat/raw-disposer-first callers share teardown completion;
- nested effects dispose in reverse order;
- scoped listeners route by key while untagged listeners remain global (`scope.spec.ts:24-110`).

This mechanism explains how DSH can scope plugin contributions. It is not inserted into the main path as an unobserved call: `time-context.apply` receives the Loader entry Context, and the selected config does not show `createScope` wrapping it.

## 10. Full closed source chain

```text
Schedule patch row: @deepseek-ai/dsh-time-context
  -> Loader Entry configured
  -> disabled gate
  -> import package namespace
  -> Loader.unwrapExports(namespace)
  -> RegistryService.plugin(namespace, config)
  -> Inject.resolve(['agents'])
  -> new Fiber(parent, config, { agents }, runtime)
  -> parent owns child via ctx.plugin() effect
  -> resolve active agents implementation
       AgentRegistry class plugin
         -> Service constructor
         -> reflect.provide('agents', instance)
  -> Fiber epoch active
  -> config schema resolution
  -> time-context.apply(pluginCtx, config)
  -> ctx.on('agent/pre-step', listener, prepend)
  -> EventsService.register
  -> plugin Fiber effect inserts hook + owns unregister
  -> Fiber ACTIVE

Agent turn
  -> ReactLoopAgent.preStep
  -> agentEvents.waterfall
  -> scopeTarget(agent, agent)
  -> Cordis dispatch filters hooks
  -> time-context listener
  -> await next()
  -> accepted/non-aborted decision
  -> derive exact Agent Session/browser/time state
  -> create sourced UserMessage
  -> return augmented PreStepDecision
  -> Agent Loop appends step/start
  -> Agent Loop appends user/message
  -> Session validates/freezes/pushes
  -> model-history projection may consume it

Teardown
  -> plugin fiber.dispose (directly or through parent)
  -> epoch inactive
  -> Fiber._unload
  -> listener effect disposer
  -> EventsService.unregister
  -> future pre-step dispatches cannot call the plugin
  -> already appended Session events remain
```

## 11. Owner-test register and experiment handoff

| Test asset | Exact asserted relationship | Static disposition | Suggested Lab use |
|---|---|---|---|
| `time-context.spec.ts:361-409` | config failures and listener removal after plugin Fiber disposal | `OWNER_TEST_DECLARED` | focused lifecycle test |
| `time-context.spec.ts:412-478` | real Agent Loop: no commit on downstream failure; one ordered sourced context per request | `OWNER_TEST_DECLARED` | focused operational test |
| `time-context.spec.ts:481-500` | real Loader namespace unwrap/mount path | `OWNER_TEST_DECLARED` | focused Loader test |
| `time-context.e2e.ts:30-83` plus fixture driver/config | source and built Loader smokes inspect JSONL ordering and attribution | `OWNER_TEST_DECLARED` | source/built e2e if environment permits |
| `core/scope/tests/scope.spec.ts:24-110` | backing Fiber ownership, disposal order and scoped/global event routing | `OWNER_TEST_DECLARED / SUPPORTING` | optional framework-support test, not substitute for plugin test |
| `app-boot/tests/app-boot.spec.ts:796-803` | enabled Loader entry with unresolved injection is pending/not activated | `OWNER_TEST_DECLARED / COUNTEREVIDENCE` | optional negative activation test |

Minimum experiment should preserve four observations separately:

1. configured entry exists;
2. plugin Fiber reached ACTIVE;
3. one pre-step produced one attributed `user/message` through the real loop;
4. after plugin Fiber disposal, the same operation produced no new contribution.

Passing only step 4's unit test does not establish Loader activation. Passing only Loader smoke does not isolate the removal edge. A complete evidence record names both commands and their exact outcomes.

## 12. Source-stage decision

- `install -> dependency/service -> event/effect registration -> scoped operate -> durable handoff -> dispose` is statically closed.
- `configured != activated` is established by Loader/Fiber implementation and pending-entry owner tests.
- `ctx.on` disposer ownership and post-Fiber-dispose listener removal are source-confirmed with a direct owner-test assertion.
- `Plugin Context != model context`, `Plugin Event != Session Event`, and `Plugin != Tool` are closed by distinct owners/call boundaries.
- Runtime pass/fail remains intentionally unset.
- Historical next gate: `EXPERIMENT_DESIGN`.
