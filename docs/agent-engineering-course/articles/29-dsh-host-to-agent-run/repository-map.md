# Article 29 Repository Map

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / SOURCE MAP COMPLETE`

> 时间语义：本文保留 Source Investigation Gate 当时的快照；该 Gate 完成时 Article 29 Lab 尚未执行，因此下文出现的 `runtime pending` 与 `EXPERIMENT_DESIGN` 只描述当时状态。当前 lifecycle 以 [Article README](README.md) 为准，当前 Claim status 以 [merged Research](research.md) 与 [merged Evidence](evidence.md) 为准，实际 runtime result 以 [Host-to-Agent Runtime Trace](experiments/host-agent-run-trace.md) 为准。

## 1. Frozen source and terminology

- Official repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Verified at: `2026-08-30 / Asia/Shanghai`
- Fresh check: `origin` points to the official repository; `HEAD` and the dereferenced local tag both equal the full pinned commit; `git status --porcelain=v1 --untracked-files=all` emitted no rows.

本文标题中的 **Host** 不是 `packages/host/*` 的 Web Host 同义词。主路径选择 shipped `headless` profile；在这条路径里，Host 只表示承载 `dsh` CLI、Cordis `Context`、Loader 和 application plugin tree 的 Node application process。`dsh-headless` 的 manifest、patch 和 runner source 都把自己定义为不含 Host、HTTP server 或 browser layer 的 direct core Agent/Session runner。真正的 Web Host/Control/Client 只在 Web 分支出现。

这个术语修正是主链能否成立的前提：若把 WebServer 画进 headless 必经链，固定源码会直接反证该图。

## 2. Evidence boundary

### 2.1 Primary source plane

- tracked `apps/cli/src/**` application source；
- tracked `packages/**/src/**` package source；
- tracked bundle manifests and `cordis.patch.yml` files；
- tracked `vendor/cordis/src/**`、`vendor/loader/src/**` and `vendor/include/src/**`；
- pinned official `AGENTS.md`、`SAFETY.md`、`docs/architecture.md` and package instructions。

The vendored framework is not an unpinned npm inference. `vendor/README.md` records Cordis `56b3d4f725681cf4556c1a8695a709cc3b6eed74`, Loader at the same upstream commit, Include `abb0a307cb1d3b0947f455d590cf5ba922d4caa4`, the `@deepseek-ai` rescope, and local transactional/lazy-config modifications. This map therefore treats the vendored files in the pinned DSH commit as the implementation source of record.

### 2.2 Runtime materialization plane

- `$DSH_HOME/profiles/<name>/package.json` and `cordis.patch.yml` are initialized/read by `loadProfile` and are mutable runtime inputs；
- `node_modules` links resolve package names at execution time；
- `apps/cli/lib/bin.js`、package `lib/**`、`apps/web/dist/**` and build records are generated artifacts；
- Session files, credentials, settings, caches and temporary Harness homes are local runtime state。

Generated files may support a producing-command observation, but they do not replace tracked source in this map. A profile directory materialized from `PROFILE_TEMPLATES` is runtime configuration, not a new authoritative source layer.

### 2.3 Excluded inference shortcuts

- directory adjacency is not a call edge；
- a `dependency` or `peerDependency` is not initialization order；
- a bundle patch row is not successful activation；
- JSDoc and README prose cannot fill a missing caller/callee；
- a live `agent/status` or stdout line is not the durable Session log；
- a static source path is not a runtime traversal result。

## 3. Edge legend

| Edge type | Meaning in this map | What it does not mean |
|---|---|---|
| `PACKAGE_DEP` | one manifest names another package | call direction, activation, lifecycle order |
| `PROFILE_TEMPLATE` | a shipped profile names ordered bundles and reload mode | profile was materialized or booted |
| `BUNDLE_EXPORT` | `package.json#dsh.bundle.patch` points to a patch file | any patch row activated |
| `PATCH_COMPOSE` | app-boot loads and orders patch lists | Loader has imported or started rows |
| `LOADER_MOUNT` | Loader/Include creates/imports/starts a configured entry | later run success |
| `SERVICE_PROVIDE` | a plugin publishes a named Cordis service | every consumer has resolved it |
| `SERVICE_INJECT` | a row/plugin waits for named services | method-call direction |
| `FACTORY_REGISTER` | AgentLoop installs itself as AgentRegistry's factory | a specific Agent exists |
| `FACTORY_DISPATCH` | AgentRegistry delegates creation to the registered factory | Turn completion |
| `CALL` | an exact source caller invokes a callee | observed execution |
| `DURABLE_APPEND` | `Session.append` commits an event to the in-memory append-only log | persistence backend flush success |
| `LIVE_EVENT` | Cordis publishes a process-local event | durability unless backed by a Session event |
| `PROJECTION` | durable events are folded into terminal/UI output | projection is authoritative storage |
| `CONTROL` | Web/API surface requests Agent/Session operations | headless depends on Web |
| `PRESENTATION` | browser or terminal renders/projects state | underlying state was created by that surface |

## 4. Owner map by plane

| Plane | Source owner and exact symbols | Inbound edges | Outbound edges | DSH status |
|---|---|---|---|---|
| Launch | `apps/cli/package.json#bin.dsh`; `apps/cli/src/bin.ts` top-level dispatch; `args.ts:parseDshArgs, resolveBoot`; `profile-boot.ts:runProfile` | OS/process argv | `CALL` to profile resolution and boot; `SERVICE_PROVIDE` for launch environment, cmdline, readiness and exit | `SOURCE_CONFIRMED` |
| Composition | `app-boot/src/profile.ts:PROFILE_TEMPLATES, loadProfile, resolveBundleDir`; `profile-boot.ts:composeProfile, allPatches`; bundle manifests and patches | profile name plus user/home/CLI layers | `PROFILE_TEMPLATE`, `BUNDLE_EXPORT`, `PATCH_COMPOSE` into root Include | `SOURCE_CONFIRMED` for selected headless composition path |
| Plugin Core | `app-boot/src/index.ts:boot, mountRootInclude`; `vendor/loader/src/config/{tree,group,entry}.ts`; vendored Cordis `Context/Fiber` | composed entry list | `LOADER_MOUNT`, service/effect lifetimes, Loader settlement and activation audit | `SOURCE_CONFIRMED`; disposal semantics routed to Article 30 |
| Runtime | `core/agent/src/index.ts:AgentRegistry`; `core/agent-loop/src/index.ts:AgentLoop`; `agent.ts:ReactLoopAgent`; `core/agent/src/inbox.ts:Inbox` | injected core services and Agent creation request | factory registration/dispatch, Agent publication, Inbox wake, Turn/Step driver | `SOURCE_CONFIRMED` for one creation/run skeleton |
| Session | `core/session/src/index.ts:SessionStore, Session, prepare, enter, announce, append, flush`; `types.ts:SessionEventMap` | Agent creation/publication and driver operations | append-only events, live `session/event`, optional flush listeners | `SOURCE_CONFIRMED`; persistence/replay semantics routed to Article 34 |
| Headless | `bundle/headless/cordis.patch.yml`; `startup.ts:apply, HEADLESS_STARTUP_SERVICE`; `index.ts:apply, run, streamReasoning, summarize` | headless profile plus launcher cmdline/exit services | direct `ctx.agents.create`, `Agent.followup`, Session flush, terminal projection, app exit | `SOURCE_CONFIRMED`; historical Source Investigation snapshot: Lab had not run yet |
| Web | `PROFILE_TEMPLATES.web`; `bundle/web-app/cordis.patch.yml`; `host/webserver/src/index.ts:WebServer`; `client/connection/src/index.ts:apply`; `apps/web/src/main.ts:AppWebEntry.run` | web profile composition | HTTP `/api` carrier, browser connection, client/plugin UI roster | `SOURCE_CONFIRMED` as a side branch only |
| Control | `api/session-controller/src/index.ts:SessionController`; `commands.ts:SessionCommandController`; `agent.ts:ApiSessionAgentController` | Web Typert Remote/API transport | `CONTROL` to `ctx.agents.create/resume/get`, `Agent.followup/steer/cancel`, Session reads/events | `SOURCE_CONFIRMED` as a side branch only |
| Observation | `Session.append`; `headless:index.ts:streamReasoning,summarize`; `SessionStore.flush`; Web `session/event` consumers | durable Session event stream | stderr reasoning projection, stdout final projection, Web event/control projection | `SOURCE_CONFIRMED` ownership; historical Source Investigation snapshot: Article 29 runtime observation had not run |

## 5. Package relationships that matter

| From | Relation | To | Why it matters here | Evidence ceiling |
|---|---|---|---|---|
| `@deepseek-ai/dsh` | `PACKAGE_DEP` | app-boot, base, headless, web-app, Loader/Include/Cordis | installed CLI can resolve shipped profile bundles and boot glue | availability only |
| `PROFILE_TEMPLATES.headless` | `PROFILE_TEMPLATE` | `dsh-base`, then `dsh-headless`; `patchReload: startup` | defines the selected ordered composition | source declaration, not activation |
| `dsh-base` manifest | `BUNDLE_EXPORT` | `./cordis.patch.yml` | supplies shared core/config rows | source declaration |
| `dsh-headless` manifest | `BUNDLE_EXPORT` | `./cordis.patch.yml` | supplies task provider and direct runner rows | source declaration |
| base patch | `LOADER_MOUNT` candidates | `dsh-llm`, `dsh-session`, `dsh-agent`, `dsh-agent-default-model`, `dsh-tools`, `dsh-system-prompt`, `dsh-agent-loop`, JSONL persistence | gives headless runner's injected core services their configured owners | row existence until Loader path/trace |
| `dsh-agent-loop` | `peerDependencies` plus runtime injection | agent, session, llm, tools, system-prompt | the factory/driver consumes those service contracts | no ordering inferred from manifest |
| `dsh-headless` | dependencies plus peer contracts | cmdline, Agent, default model, Session, Loader | runner reads launcher facts and core services | exact calls are established separately in `call-path.md` |
| `PROFILE_TEMPLATES.web` | `PROFILE_TEMPLATE` | `dsh-base`, then `dsh-web-app`; `patchReload: live` | defines a separate Web composition | no Web runtime claim |
| web-app patch | `LOADER_MOUNT` candidates | WebServer, SessionController, Host Connection, Web runtime, browser modules/UI | adds Host/Control/Client around shared base candidates | side-branch source only |

The important negative finding is that `dsh-headless` does not depend on or insert `dsh-host-webserver`, `dsh-api-session-controller`, `dsh-client-connection`, `cordis-host-runner`, `web-runtime`, or browser UI rows. Those packages are available elsewhere in the workspace and in the CLI installation, but package availability is not profile membership.

## 6. Composition graph: shared root, distinct applications

```text
OS argv
  -> dsh CLI / named profile
       -> app-boot + Cordis Context + Loader
            -> dsh-base patch (shared configured candidates)
                 -> Session / Agent registry / AgentLoop / LLM / Tools / persistence ...
            -> selected second bundle
                 headless: dsh-headless patch
                   -> headless-startup -> direct headless-runner -> Agent/Session
                 web: dsh-web-app patch
                   -> WebServer -> /api Host Connection -> SessionController
                   -> browser modules -> AppWebEntry/UI
```

The split occurs in the profile template and patch stack, not inside one universal Host class. Both profiles can share base rows, but this map does not claim that every base row activates identically under both surfaces.

## 7. Source-confirmed edge register

| ID | Exact source edge | Type | Counter-evidence checked | Proves | Does not prove | Status |
|---|---|---|---|---|---|---|
| `29-S01` | `apps/cli/src/bin.ts` reads argv -> `parseDshArgs` -> profile branch dynamically imports/calls `runProfile` | `CALL` | plugin and dump-config branches are separate | selected supported launch dispatch exists | selected branch ran | `SOURCE_CONFIRMED` |
| `29-S02` | `runProfile` -> `composeProfile` -> `prepareProfile/loadProfile`; headless template names base then headless | `PROFILE_TEMPLATE / CALL` | profile may already contain user-modified manifest | shipped initialization and later manifest loading are explicit | actual local profile equals untouched template | `SOURCE_CONFIRMED` with mutable-profile limitation |
| `29-S03` | `loadProfile` resolves each bundle manifest's `dsh.bundle.patch`; `composeProfile/allPatches` order bundle, profile, home, overlays, telemetry | `BUNDLE_EXPORT / PATCH_COMPOSE` | manifest dependency order was not used | exact patch acquisition and ordering code | semantic correctness of overlays | `SOURCE_CONFIRMED` |
| `29-S04` | `runProfile` -> `boot` -> `new Context` -> `ctx.plugin(Loader)` -> `mountRootInclude` -> `ctx.loader.create(include)` -> Loader settlement/audit | `CALL / LOADER_MOUNT` | docs and config dump were not substituted | source boot reaches Loader-created rows and waits for settlement | all rows activate in a concrete run | `SOURCE_CONFIRMED` |
| `29-S05` | headless patch inserts `headless-startup`, then injected `headless-runner`; startup parses `cmdlineArgs` and provides `headlessStartup.task` | `LOADER_MOUNT / SERVICE_PROVIDE / SERVICE_INJECT` | patch comment alone rejected; source `apply` inspected | task service is the lazy-config bridge | runtime scheduling/order beyond injection | `SOURCE_CONFIRMED` |
| `29-S06` | headless `apply` calls async `run`; `run` awaits Loader settlement, reads agents/defaultModel/sessions, calls `agents.create` | `CALL` | configured startup Agent path and Web controller path excluded | selected direct runner reaches registry creation call | credential/provider success | `SOURCE_CONFIRMED` |
| `29-S07` | `AgentLoop` constructor effects `ctx.agents.setFactory(this)`; `AgentRegistry.create` calls `requireFactory` then `AgentFactory.createAgent` | `FACTORY_REGISTER / FACTORY_DISPATCH` | direct `AgentLoop.create` constructor path separated | interface/default-factory seam and selected dispatch close | custom factory compatibility beyond contract | `SOURCE_CONFIRMED` |
| `29-S08` | `AgentLoop.createAgent` prepares Session, constructs `ReactLoopAgent`, runs setup, then enters/announces Session and Agent | `CALL / SERVICE LIFECYCLE / LIVE_EVENT` | same-name registry methods and resume path separated | creation/publication ownership and order | observed runtime events | `SOURCE_CONFIRMED` |
| `29-S09` | `followup` -> `send` -> `Inbox.splice` durable event -> `wakeDriver` -> `kick` -> `turn` -> `preStep` -> `step` | `CALL / DURABLE_APPEND` | architecture sequence diagram not used to fill arrows | selected driver skeleton reaches Turn/Step code | model call succeeds or tool branches execute | `SOURCE_CONFIRMED` |
| `29-S10` | driver appends turn/step/user/request/assistant/end events through `Session.append`; append publishes `session/event` after committing log entry | `DURABLE_APPEND / LIVE_EVENT` | `agent/status` and stdout rejected as durable substitutes | authoritative in-memory event owner and publication order | persistence has flushed | `SOURCE_CONFIRMED` |
| `29-S11` | headless waits idle, flushes `sessions.flush`, summarizes `session.events`, writes stdout/stderr, calls launcher-provided `appExit` | `CALL / PROJECTION` | terminal output not treated as source of truth | terminal result is derived from the Session interval and exit is bounded by launcher | persisted backend contains the same bytes unless a listener participated successfully | `SOURCE_CONFIRMED` |
| `29-S12` | Web profile adds WebServer/Connection/SessionController/browser rows; SessionController commands call Agent APIs | `CONTROL / PRESENTATION` | no Web row appears in headless patch | Web is a distinct control/presentation branch | Web server behavior or security in a runtime probe | `SOURCE_CONFIRMED / SIDE PATH` |

## 8. Owner boundaries that remain deliberately open

1. **Plugin lifecycle:** Article 29 sees Loader import/start/settlement and reversible registrations, but does not certify install/register/operate/dispose for a representative plugin. Route to Article 30.
2. **Profile conflict semantics:** this map proves layer order and selected rows, not precedence/conflict/missing-provider outcomes. Route to Article 31.
3. **Prompt assembly:** `preStep`, `systemPrompt.assemble`, `deriveMessages` and `buildRequest` are path anchors only. Exact ordered assembly and two-step diffs belong to Article 32.
4. **Loop variants:** one normal skeleton is closed statically; no-tool/single-tool/multi-tool/cancel terminal traces belong to Article 33.
5. **Persistence and continuation:** `Session.append` and `flush` ownership are visible, but replay/resume/fork semantics are not established. Route to Article 34.
6. **Tool enforcement:** `executeToolCalls` is an exact downstream anchor, not proof of canonicalize/validate/policy/execute/persist. Route to Article 35.
7. **Recovery:** `ProcessShutdown`, cancellation and error containment are visible locally; usage/compaction/recovery terminal behavior belongs to Article 36.
8. **Core versus extensions:** package/profile membership seeds the question but does not decide BuildPilot mapping. Route to Article 37.

## 9. Article 30-37 routing table

| Article | Source owner handed off from this map | Exact unanswered question | Current status |
|---|---|---|---|
| 30 | Cordis `Context/Fiber`, Loader `Entry/EntryGroup/EntryTree`, one selected plugin's effects | install/register/operate/dispose and contribution removal | `PARTIAL / DEFER` |
| 31 | `PROFILE_TEMPLATES`, `loadProfile`, `composeProfile`, `composeEntries`, bundle/user/home/CLI patches | precedence, conflict, missing service and effective config | `PARTIAL / DEFER` |
| 32 | `ReactLoopAgent.preStep/buildRequest`, system-prompt assembly, Session-derived messages | ordered model-visible request assembly and change diff | `PARTIAL / DEFER` |
| 33 | `Inbox`, `wakeDriver`, `kick`, `turn`, `step`, `executeToolCalls` | four concrete lifecycle traces and terminal reasons | `PARTIAL / DEFER` |
| 34 | `Session`, `SessionStore`, persistence/query/projection owners | append/write/read/projection plus replay/resume/fork | `PARTIAL / DEFER` |
| 35 | Tools registry, schemas, `executeToolCalls`, policy hooks | enforcement pipeline and negative traces | `PARTIAL / DEFER` |
| 36 | loop cancellation/error paths, checkpoint/flush, stats/compaction/persistence | usage, pressure, cancel/resume and recovery limits | `PARTIAL / DEFER` |
| 37 | base/headless/web bundle membership plus skill/workflow/subagent/Web packages | core/extension/default/optional matrix and course decision | `PARTIAL / DEFER` |

## 10. Historical source-stage verdict

The following verdict records the end of Source Investigation before Lab execution. It is not the current Article 29 lifecycle or merged Claim register; use `README.md`, `research.md`, `evidence.md` and `experiments/host-agent-run-trace.md` for those current results.

- Main source path: `CLOSED` from supported `dsh --profile headless` launch through profile composition, Loader settlement, task provider, direct runner, Agent factory, Session/Agent publication, Inbox wake, Turn/Step driver, Session event projection, flush and bounded process exit.
- Main-path DSH verification: `SOURCE_CONFIRMED` at the exact pinned revision.
- Runtime verification: `HISTORICAL PENDING AT SOURCE INVESTIGATION`. At that time Article 29 Lab had not executed; Article 28's keyless `MISSING_CREDENTIAL` and failed Windows full suite were inherited boundaries, not Article 29's Agent Run confirmation. The later Article 29 result is routed to the merged evidence and runtime trace above.
- Web/Control branch: `SOURCE_CONFIRMED / SIDE PATH`; no Web runtime was started and no network bind was needed.
- BuildPilot decision: `ADOPT` typed-owner/typed-edge mapping; `DEFER` concrete DSH runtime architecture until the owning articles and Part VI audit finish.
- Historical next gate at Source Investigation completion: `EXPERIMENT_DESIGN`. This route is superseded by the current lifecycle in `README.md`.
