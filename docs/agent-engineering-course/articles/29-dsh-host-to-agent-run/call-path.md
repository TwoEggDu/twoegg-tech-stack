# Article 29 Host-to-Agent Call Path

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / STATIC CALL PATH CLOSED`

> 时间语义：本文保留 Source Investigation Gate 当时的静态路径快照；当时 Article 29 Lab 尚未执行。当前 lifecycle 以 [Article README](README.md) 为准，当前 Claim status 以 [merged Research](research.md) 与 [merged Evidence](evidence.md) 为准，实际 runtime result 以 [Host-to-Agent Runtime Trace](experiments/host-agent-run-trace.md) 为准。

## 1. What this path certifies

This document closes one exact source path at `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`:

```text
dsh --profile headless "<task>"
-> CLI/profile composition
-> Cordis Context + Loader settlement
-> headless-startup task service
-> headless-runner
-> AgentRegistry factory dispatch
-> AgentLoop / ReactLoopAgent + Session publication
-> followup / Inbox / wake / Turn / Step
-> Session events
-> whenIdle / flush / summarize
-> stdout/stderr + appExit / ProcessShutdown
```

Every arrow below names the caller, callee, file and relation. `SOURCE_CONFIRMED` means the caller/callee relation exists in the pinned source. It does **not** mean the Lab observed that arrow at runtime. At this Source Investigation Gate, the complete runtime status was historically `PENDING` because Article 29's bounded experiment had not executed; the later merged status and raw result are recorded in `research.md`, `evidence.md` and `experiments/host-agent-run-trace.md`.

The term Host means the launch/application process on this main path. It does not mean `WebServer`, SessionController or the browser Host layer.

## 2. Phase A: argv to a settled plugin tree

| Step | Caller -> callee | File and symbol | Relation | Why the arrow holds | Status |
|---:|---|---|---|---|---|
| 1 | installed `dsh` bin -> `apps/cli/src/bin.ts` | `apps/cli/package.json#bin.dsh = lib/bin.js`; source entry `bin.ts` | generated bin/source entry | package bin selects the built counterpart of this source entry | `SOURCE_CONFIRMED / ARTIFACT EXECUTION PENDING` |
| 2 | top-level entry -> `parseDshArgs(process.argv.slice(2), readVersion())` | `apps/cli/src/bin.ts:24`; `args.ts:parseDshArgs` | `CALL` | the call is unconditional before branch dispatch | `SOURCE_CONFIRMED` |
| 3 | `parseDshArgs` -> `resolveBoot(..., profile, options, args)` -> `ProfileInvocation` | `args.ts:83-103,112-190` | `CALL / RETURN` | `--profile headless` resolves mode `profile`, ordered patches and inner task args | `SOURCE_CONFIRMED` |
| 4 | profile switch -> dynamic import `profile-boot.ts` -> `runProfile` | `bin.ts:26-35` | `CALL` | only `invocation.mode === 'profile'` takes this branch | `SOURCE_CONFIRMED` |
| 5 | `runProfile` -> `composeProfile(profile, patchFiles)` | `profile-boot.ts:209-210` | `CALL` | first statement of selected run | `SOURCE_CONFIRMED` |
| 6 | `composeProfile` -> `prepareProfile` -> `loadProfile` | `profile-boot.ts:156-172,118-121`; `app-boot/src/profile.ts:805-843` | `CALL` | profile is loaded/materialized before layers are composed | `SOURCE_CONFIRMED` |
| 7 | absent shipped profile -> `PROFILE_TEMPLATES.headless` -> `initProfile` | `profile.ts:137-149,809-818` | `PROFILE_TEMPLATE / CALL` | shipped template names `dsh-base`, then `dsh-headless`, reload `startup` | `SOURCE_CONFIRMED`; an existing mutable profile may differ |
| 8 | `loadProfile` -> each bundle manifest `dsh.bundle.patch` -> `loadOverlayPatches` | `profile.ts:829-838`; both bundle `package.json` files | `BUNDLE_EXPORT / CALL` | package name resolves to directory; manifest patch path is loaded | `SOURCE_CONFIRMED` |
| 9 | `composeProfile` -> `allPatches` | `profile-boot.ts:136-143,160-172` | `PATCH_COMPOSE` | bundle layers precede profile, home, `--patch`, then optional telemetry patch | `SOURCE_CONFIRMED` |
| 10 | `runProfile` -> `boot(NAME, rootConfig, allPatches, prepare)` | `profile-boot.ts:230-263` | `CALL` | empty root config plus cloned patch stack enter app-boot | `SOURCE_CONFIRMED` |
| 11 | `boot` -> `new Context` -> `ctx.plugin(Loader)` -> launcher `prepare` | `app-boot/src/index.ts:772-789`; `profile-boot.ts:251-263` | `CALL / SERVICE_PROVIDE` | Context/Loader are installed; launch environment and cmdline/exit/readiness services are provided before rows mount | `SOURCE_CONFIRMED` |
| 12 | `boot` -> `mountRootInclude` -> `ctx.loader.create(rootInclude)` | `app-boot/src/index.ts:501-543,789` | `LOADER_MOUNT` | a pinned `cordis:include` row carries the root config path and patch list | `SOURCE_CONFIRMED` |
| 13 | Loader `EntryTree.create` -> `EntryGroup.create` -> `Entry.update/init` -> `_start` -> Cordis registry plugin | `vendor/loader/src/config/tree.ts:96-103`; `group.ts:20-39`; `entry.ts:258-301` | `CALL / LOADER_MOUNT` | created rows are imported and started through their fibers | `SOURCE_CONFIRMED` |
| 14 | `boot` -> `ctx.loader.await()` -> `assertEntriesActivated` | `app-boot/src/index.ts:790-800`; `vendor/loader/src/config/tree.ts:42-63` | `SETTLEMENT` | Loader drains import/lifecycle tasks and reports fiber failures before activation audit | `SOURCE_CONFIRMED` |

`prepareProfile` rewrites the profile's empty `cordis.yml`; composition lives in patches. This means the main chain is not `profile YAML contains every plugin -> execute in list order`. It is `empty root -> patch composition -> Include/Loader transactional entry tree`.

## 3. Phase B: Loader rows to the direct headless runner

| Step | Caller -> callee | File and symbol | Relation | Why the arrow holds | Status |
|---:|---|---|---|---|---|
| 15 | base bundle patch -> core rows | `packages/bundle/base/cordis.patch.yml` rows `llm`, `session`, `agent`, `agent-default-model`, `tools`, `system-prompt`, `agent-loop`, persistence | `LOADER_MOUNT candidates` | headless template applies base before headless; the rows name their owning packages | `SOURCE_CONFIRMED` as composition, runtime activation pending |
| 16 | headless bundle patch -> `headless-startup` | `bundle/headless/cordis.patch.yml`; `startup.ts:apply` | `LOADER_MOUNT` | patch inserts the package's `/startup` export | `SOURCE_CONFIRMED` |
| 17 | launcher `provideCmdline` -> `ctx.cmdlineArgs` and `ctx.appExit` | `boot/cmdline/src/index.ts:84-89`; `profile-boot.ts:258-262` | `SERVICE_PROVIDE` | the launcher provides immutable inner args and bounded exit before tree mount | `SOURCE_CONFIRMED` |
| 18 | startup `apply` -> `parseCmdline` -> commander action -> `ctx.provide('headlessStartup', { task })` | `bundle/headless/src/startup.ts:43-51`; `boot/cmdline/src/index.ts:165-185` | `CALL / SERVICE_PROVIDE` | accepted task is joined from app-owned args; missing task/help publishes no task | `SOURCE_CONFIRMED` |
| 19 | provided `headlessStartup` -> lazy runner row config | `headless/cordis.patch.yml` runner `inject: [headlessStartup]`, `task: !!js ctx.headlessStartup.task` | `SERVICE_INJECT / CONFIG_RESOLUTION` | runner is service-gated and reads the provider's task | `SOURCE_CONFIRMED` |
| 20 | Loader starts runner -> `headless apply(ctx, config)` -> `void run(...).catch(fail)` | `bundle/headless/src/index.ts:207-220` | `LOADER_MOUNT / CALL` | runner validates launcher `appExit`, builds IO and starts the owned async operation | `SOURCE_CONFIRMED` |
| 21 | `run` -> `await ctx.get('loader')?.await()` | `headless/src/index.ts:164-167` | `SETTLEMENT` | runner explicitly waits for sibling composition before creating an Agent | `SOURCE_CONFIRMED` |
| 22 | `run` -> read `agents`, `agentDefaultModel`, `sessions` -> `currentSelection` | `headless/src/index.ts:167-175` | `SERVICE_LOOKUP / CALL` | early tree disposal returns; otherwise selected services drive creation | `SOURCE_CONFIRMED` |

The patch's row order is not claimed as lifecycle order. `headless-runner` is constrained by injection of `headlessStartup`, while its own `run` awaits Loader settlement before touching Agent services. Those are the source-backed ordering relations.

## 4. Phase C: registry dispatch and ordered Session/Agent publication

| Step | Caller -> callee | File and symbol | Relation | Why the arrow holds | Status |
|---:|---|---|---|---|---|
| 23 | base Loader row -> `new AgentLoop(ctx, config)` -> `ctx.agents.setFactory(this)` | `core/agent-loop/src/index.ts:295-352` | `FACTORY_REGISTER` | constructor registers the default factory as an effect | `SOURCE_CONFIRMED` |
| 24 | headless `run` -> `agents.create(CreateAgentOptions)` | `headless/src/index.ts:176-185`; `core/agent/src/index.ts:396-406` | `CALL` | headless supplies new Session id, cwd, model route and scoped setup | `SOURCE_CONFIRMED` |
| 25 | `AgentRegistry.create` -> `requireFactory` -> `Reflect.apply(target.createAgent, ...)` | `core/agent/src/index.ts:381-405` | `FACTORY_DISPATCH` | missing factory fails; active factory receives caller context and options | `SOURCE_CONFIRMED` |
| 26 | `AgentLoop.createAgent` -> `SessionStore.prepare` -> `SessionPreparation.create` | `agent-loop/src/index.ts:607-623`; `session/src/index.ts:861-887`; `preparation.ts:20-48` | `CALL / PREPARE` | Session exists detached before publication and is rollback-owned | `SOURCE_CONFIRMED` |
| 27 | `createAgent` -> `setupAndPublish` -> private `prepare` | `agent-loop/src/index.ts:625-645,460-579` | `CALL` | factory builds lifecycle transaction and runs caller setup before publication | `SOURCE_CONFIRMED` |
| 28 | `prepare` -> `new ReactLoopAgent(loopCtx, id, options, session)` | `agent-loop/src/index.ts:549-570`; `agent.ts:87-104` | `CALL` | default driver owns Inbox, scope, agent context and runtime projection | `SOURCE_CONFIRMED` |
| 29 | headless setup -> `installModelSelection(agentCtx, selected)` | `headless/src/index.ts:180-183` | `SETUP CALL` | the creation setup binds the selection in the Agent scope before publish | `SOURCE_CONFIRMED` |
| 30 | `prepared.publish('startup')` -> `sessions.enter` -> `agents.enter` -> `sessions.announce` -> `agents.announce` -> `agent/session-start` | `agent-loop/src/index.ts:557-570`; Session/Agent registry source | `PUBLICATION / LIVE_EVENT` | exact order is encoded in the returned publish closure with rollback checks | `SOURCE_CONFIRMED` |
| 31 | `setupAndPublish` -> `{ agent, dispose }` -> headless destructures `agent` | `agent-loop/src/index.ts:639-645`; `headless/src/index.ts:176-185` | `RETURN` | headless receives only after setup and publication succeed | `SOURCE_CONFIRMED` |

There is an alternate `AgentLoop.create(...)` path for declaratively configured startup Agents and a `resume(...)` path for persisted Sessions. Neither is used to fill the selected headless arrow: headless calls `AgentRegistry.create`, which dispatches to `AgentLoop.createAgent`.

## 5. Phase D: followup through Inbox, Turn and Step

| Step | Caller -> callee | File and symbol | Relation | Durable effect | Status |
|---:|---|---|---|---|---|
| 32 | headless `run` -> initial `agent.whenIdle()` | `headless/src/index.ts:186`; `agent.ts:202-207` | `CALL` | none by itself; joins any startup activity | `SOURCE_CONFIRMED` |
| 33 | headless snapshots `firstSeq` -> `agent.followup(createUserMessage(...))` | `headless/src/index.ts:187-194`; `agent.ts:129-131` | `CALL` | establishes the owned interval and sends a next-turn waking message | `SOURCE_CONFIRMED` |
| 34 | `followup` -> `send(input, 'next-turn', true)` -> `Inbox.splice` | `agent.ts:120-131`; `agent/inbox.ts:139-192` | `CALL` | `agent/inbox/spliced` is appended before the live projection mutates | `SOURCE_CONFIRMED` |
| 35 | `send` -> `wakeDriver` -> `agents.withInitiator(this, () => kick())` | `agent.ts:120-127,179-200` | `CALL / ASYNC OWNERSHIP` | Agent status may change live; no Turn event yet | `SOURCE_CONFIRMED` |
| 36 | `kick` -> loop `while (await turn())` | `agent.ts:217-230` | `CALL` | contains driver failure and returns to idle | `SOURCE_CONFIRMED` |
| 37 | `turn` -> `session.append('turn/start')` | `agent.ts:252-267` | `DURABLE_APPEND` | opens the durable Turn boundary | `SOURCE_CONFIRMED` |
| 38 | `turn` -> `preStep('next-turn', position)` -> `Inbox.claim` | `agent.ts:232-249,268-274`; `inbox.ts:63-78` | `CALL` | claim appends deletion splices and emits live claimed notifications | `SOURCE_CONFIRMED` |
| 39 | `preStep` -> `systemPrompt.assemble` -> runtime-context projection -> `agent/pre-step` waterfall | `agent.ts:236-249` | `CALL / LIVE_EVENT` | resulting messages are not yet logged as user messages until accepted | `SOURCE_CONFIRMED`; assembly semantics deferred to Article 32 |
| 40 | accepted decision -> `session.append('step/start')` -> append each `user/message` | `agent.ts:285-294` | `DURABLE_APPEND` | step boundary and model-visible user inputs enter the Session log | `SOURCE_CONFIRMED` |
| 41 | `turn` -> `step(assembly, startsRequestSeries)` | `agent.ts:294,339-435` | `CALL` | step owns one model request plus any tool calls | `SOURCE_CONFIRMED` |
| 42 | `step` -> `buildRequest(..., session.deriveMessages(), ...)` | `agent.ts:346-357`; `session/src/index.ts:724-745` | `CALL / PROJECTION` | model history is projected from durable surface events | `SOURCE_CONFIRMED`; exact request assembly deferred |
| 43 | `buildRequest` -> `agent/request` waterfall -> `llm.prepareCall` -> request/header/context appends | `agent.ts:442-541` | `CALL / LIVE_EVENT / DURABLE_APPEND` | route and request envelope metadata become durable before stream consumption | `SOURCE_CONFIRMED`; adapter success pending |
| 44 | `step` -> prepared adapter stream or `ctx.llm.stream` -> `assistant/chunk` appends | `agent.ts:359-369` | `CALL / DURABLE_APPEND` | every received chunk is logged | `SOURCE_CONFIRMED`; provider output not observed |
| 45 | assembler finish -> `assistant/message` append | `agent.ts:388-425` | `DURABLE_APPEND` | final assembled assistant message derives from chunk seqs | `SOURCE_CONFIRMED` |
| 46 | tool calls absent -> completed; present -> `executeToolCalls` and possible next-step Inbox insertion | `agent.ts:426-434` | `BRANCH / CALL` | tool result semantics are outside this article | `SOURCE_CONFIRMED` branch anchor only |
| 47 | `turn` finally -> `step/end`, optional `agent/turn-stopping`, then `turn/end` | `agent.ts:298-330` | `DURABLE_APPEND / LIVE_EVENT` | terminal Turn reason is always attempted in `finally` | `SOURCE_CONFIRMED` |

This is the minimum normal skeleton, not a claim that every Turn executes exactly one Step. `turn()` may reject before a Step, loop over next-step input/tool obligations, abort, or end with error/max-tokens. Article 33 owns those scenario traces.

## 6. Phase E: quiescence, durable checkpoint, terminal projection and exit

| Step | Caller -> callee | File and symbol | Relation | Why the arrow holds | Status |
|---:|---|---|---|---|---|
| 48 | headless -> second `agent.whenIdle()` | `headless/src/index.ts:194`; `agent.ts:202-207` | `CALL / JOIN` | waits until the current and any replacing activity promise settle | `SOURCE_CONFIRMED` |
| 49 | driver event writes -> `Session.append` -> log push -> `session/event` observers | `session/src/index.ts:602-653` | `DURABLE_APPEND / LIVE_EVENT` | event is validated/frozen and pushed before contained observer callbacks | `SOURCE_CONFIRMED` |
| 50 | headless -> `sessions.flush(agent.session)` | `headless/src/index.ts:198`; `session/src/index.ts:1020-1037` | `CALL / CHECKPOINT` | SessionStore awaits every scoped `session/flush` listener and rejects first failure | `SOURCE_CONFIRMED` |
| 51 | headless -> `summarize(agent.session.events, firstSeq)` | `headless/src/index.ts:62-85,199` | `PROJECTION` | last assistant text and last Turn reason are folded only from the owned durable interval | `SOURCE_CONFIRMED` |
| 52 | outcome -> stdout final text; optional error -> stderr | `headless/src/index.ts:199-204` | `PRESENTATION` | terminal output is a projection after flush | `SOURCE_CONFIRMED`; output bytes pending runtime |
| 53 | outcome reason -> `io.exit(0 or 1)` -> provided `ctx.appExit` | `headless/src/index.ts:205,213-219`; `cmdline/src/index.ts:84-89` | `CALL` | completed Turn maps to 0; every other/missing terminal reason maps to 1 | `SOURCE_CONFIRMED` |
| 54 | `ctx.appExit` -> `shutdown.shutdown(code)` -> root fiber dispose -> `process.exitCode = code` | `profile-boot.ts:213,258-262`; `apps/cli/src/process-shutdown.ts:22-76` | `CALL / TEARDOWN` | normal requests coalesce, arm 5s bound, dispose tree, then record natural completion; rejection/timeout forces exit | `SOURCE_CONFIRMED` |

`SessionStore.flush` returns whether any listener participated. Headless awaits it but does not inspect that boolean. Therefore the static chain proves an awaited flush checkpoint, not that a persistence provider was mounted or wrote bytes in every composition. The shipped base patch contains a JSONL persistence row, yet successful activation and persisted-file equality remain runtime questions.

## 7. Full closed chain in compact form

```text
package bin.dsh
  -> apps/cli/src/bin.ts
  -> parseDshArgs -> resolveBoot(mode=profile, profile=headless, args=[task])
  -> runProfile
  -> composeProfile -> prepareProfile -> loadProfile
  -> PROFILE_TEMPLATES.headless [dsh-base, dsh-headless] when initialized
  -> bundle manifests -> cordis.patch.yml layers
  -> allPatches(bundle -> profile -> home -> --patch -> telemetry)
  -> boot
  -> new Context -> ctx.plugin(Loader) -> provide launch/cmdline/appExit
  -> mountRootInclude -> loader.create(include)
  -> Loader Entry import/start/await -> activation audit
  -> headless-startup.apply -> parseCmdline -> provide headlessStartup.task
  -> injected headless-runner.apply -> run
  -> await loader settlement
  -> agentDefaultModel.currentSelection
  -> AgentRegistry.create -> requireFactory
  -> AgentLoop.createAgent
  -> SessionStore.prepare -> ReactLoopAgent -> scoped setup
  -> Session enter/announce -> Agent enter/announce -> agent/session-start
  -> whenIdle -> followup
  -> send -> Inbox.splice(agent/inbox/spliced) -> wakeDriver -> kick
  -> turn(start) -> Inbox.claim -> preStep
  -> step(start) -> user/message -> buildRequest -> LLM stream
  -> assistant/chunk* -> assistant/message -> optional tool branch
  -> step/end -> turn/end
  -> whenIdle -> SessionStore.flush
  -> summarize(Session.events[firstSeq..])
  -> stdout/stderr -> appExit
  -> ProcessShutdown -> Context/Fiber disposal -> process exit code
```

## 8. Web/Control side path, not part of headless

The source-confirmed side branch is:

```text
dsh --profile web
  -> PROFILE_TEMPLATES.web [dsh-base, dsh-web-app]
  -> web-app patch
  -> WebServer (HTTP carrier)
  -> client-connection /api route and HostConnectionService
  -> Typert Remote namespaces including SessionController
  -> SessionCommandController
  -> ApiSessionAgentController
  -> ctx.agents.create/resume/get
  -> Agent.followup/steer/cancel
  -> Session events/control projections
  -> browser connection and AppWebEntry/UI
```

Exact supporting source:

- `packages/bundle/web-app/cordis.patch.yml` inserts `webserver`, `session-controller`, `connection`, host/client runners and browser UI roster；
- `packages/host/webserver/src/index.ts:WebServer` owns the Node HTTP carrier and route registry；
- `packages/client/connection/src/index.ts:apply` creates `HostConnectionService` and registers `/api` on `ctx.webServer`；
- `packages/api/session-controller/src/index.ts:SessionController` is the Host Session Remote owner and consumes Agent/Session services；
- `packages/api/session-controller/src/commands.ts:prompt` resolves an Agent and calls `steer` or `followup`；
- `apps/web/src/main.ts` runs `AppWebEntry` in the browser。

This branch proves a distinct Control/Presentation route over shared core candidates. It does not prove a Web runtime activation, request security, browser rendering, or equivalence with headless.

## 9. Gaps and counter-evidence

| Candidate overclaim | Counter-evidence in pinned source | Required wording |
|---|---|---|
| “Host means WebServer” | headless manifest/patch explicitly exclude Host/HTTP/browser | Host on main path means launch/application process |
| “Profile template always equals effective profile” | `loadProfile` reads mutable `$DSH_HOME/profiles/<name>/package.json` and user/home/CLI patches | template is initialization/default evidence; experiment records effective config |
| “Patch row means active plugin” | Loader can leave service-gated fibers pending or reject import/apply | activation requires Loader settlement/runtime evidence |
| “Factory symbol means selected product uses it” | configured Agent startup, Web control and tests are alternate callers | selected headless path is certified through `agents.create -> createAgent` only |
| “Static path means Agent Run succeeded” | Historical Source Investigation snapshot: Article 28 keyless probe exited at `MISSING_CREDENTIAL`, and Article 29 Lab had not yet executed | at this Gate the main path was `SOURCE_CONFIRMED / RUNTIME_PENDING`; current bounded result is in the merged evidence and runtime trace |
| “One Turn means one Step” | Turn loop permits rejected/empty input, multiple tool-driven steps, abort and error | call path is a skeleton; scenario counts deferred |
| “assistant/message means persistence succeeded” | Session append commits in memory; flush listeners are optional and persistence is a plugin | distinguish durable log semantics from observed backend write |
| “stdout is the result authority” | `summarize` folds `session.events`; stdout is written afterward | Session interval is authority; stdout is projection |
| “Web side branch proves Web behavior” | only manifests, patches and source callers were inspected | Web remains source-only, no bind/runtime claim |

## 10. Historical source-stage Evidence decision

This disposition is the pre-Lab Source Investigation snapshot and is superseded by the Researcher's Evidence Merge. Current Claim conclusions live in `research.md` and `evidence.md`; runtime observations live in `experiments/host-agent-run-trace.md`.

- `29-C02` supported CLI/profile startup: upgrade to `SOURCE_CONFIRMED`.
- `29-C04` headless profile/bundle ordering: upgrade to `SOURCE_CONFIRMED`, with mutable materialized-profile limitation.
- `29-C05` historical disposition: `PARTIAL` for runtime activation at Source Investigation. This is superseded by the merged, narrowed declaration-only Claim `29-C05 = CONFIRMED` in `research.md` / `evidence.md`.
- `29-C06` Plugin Core boot/mount/settlement bridge: upgrade to `SOURCE_CONFIRMED`; full lifecycle remains Article 30.
- `29-C07` Agent registry/default factory/driver ownership: upgrade to `SOURCE_CONFIRMED`.
- `29-C08` Session durable-event and terminal-projection ownership: upgrade to `SOURCE_CONFIRMED`; backend persistence runtime remains pending.
- `29-C09` headless direct runner/no-Web boundary: upgrade to `SOURCE_CONFIRMED`.
- `29-C10` complete static Host/profile-to-Agent Run path: upgrade to `SOURCE_CONFIRMED`.
- `29-C11` historical disposition: `PARTIAL / EXPERIMENT_PENDING` before Lab execution. This wording is superseded by the merged Claim `29-C11 = CONFIRMED`, which records the exact Windows owner-test failure and `UNKNOWN_TOOL` counter-evidence; see `research.md`, `evidence.md` and the runtime trace.
- `29-C13` Web/Control versus headless composition split: upgrade to `SOURCE_CONFIRMED / SIDE_PATH`.

At Source Investigation completion, source path closure was sufficient to proceed but insufficient for runtime-success wording. Historical next gate: `EXPERIMENT_DESIGN`; that route is superseded by the current lifecycle in `README.md`.
