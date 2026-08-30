# Article 32 Call Path

Status: `SOURCE_MAP PASS`

Pinned source: `deepseek-ai/deepseek-harness@cd5ef8148158c3a752a658978873241fdf8e2bbc` (`dsh-v0.1.2-alpha.1`).

## Path A — registrations form four independent lanes

```text
SystemPrompt.constructor
  packages/core/system-prompt/src/index.ts:404-421
  -> section({ name: "harness:identity", order: -1000, text: ... })
  -> section({ name: "deployment:persona", order: 0, text: config.persona })
  -> optional suppressRuntimeContext()

addHarnessSourceSection
  packages/boot/app-boot/src/index.ts:838-845
  -> SystemPrompt.section({ name: "harness:source", order: -900, text: ... })

WebApp.apply
  packages/bundle/web-app/src/index.ts:235-251
  -> optional SystemPrompt.section({
       name: "app:web-surface",
       order: -800,
       text: () => webSurfacePrompt(localWebUrl(promptCtx))
     })

Persona.apply(agent-scoped context)
  packages/preset/persona/src/index.ts:60-67
  -> SystemPrompt.section({
       name: "deployment:persona",
       order: 0,
       text: config.text,
       complete?: true
     })
  -> same-name scoped section shadows the global deployment persona

AgentLoop constructor
  packages/core/agent-loop/src/index.ts:347-355
  -> SystemPrompt.variable("provider", ctx => ctx.agent?.options.provider)
  -> SystemPrompt.variable("model", ctx => ctx.agent?.options.model)
  -> SystemPrompt.variable("cwd", ctx => ctx.agent?.session.header.cwd)

Tools constructor
  packages/core/tools/src/index.ts:827-837
  -> SystemPrompt.tools(ctx => wireSchemas(ctx.scope))

SandboxPolicy constructor
  packages/sandbox/sandbox-policy/src/index.ts:101-123
  -> SystemPrompt.context({
       name: "sandbox:policy",
       order: 110,
       text: ctx => renderPolicyContext(resolve(ctx.agent.session))
     })

ApprovalService constructor
  packages/interaction/user-approval/src/index.ts:162-180
  -> SystemPrompt.context({ name: "approval:policy", order: 115, text: provider })

applyReadTool / PowerShell.apply
  packages/fs/tool-fs/src/read.ts:69-83
  packages/shell/tool-pwsh/src/index.ts:244-263
  -> SystemPrompt.section(tool guidance)
  -> Tools.register(tool schema + executor)
```

The arrows converge in the registry but do not erase lane identity yet:

```text
PromptLayer.sections : NamedEntries<PromptSection>
PromptLayer.contexts : NamedEntries<PromptContext>
PromptLayer.variables: NamedEntries<VariableProvider>
PromptLayer.toolProviders: AnonymousEntries<ToolProvider>
```

Source: `packages/core/system-prompt/src/index.ts:348-385`.

## Path B — exact scope and ordering rules

```text
SystemPrompt.section/context/variable
  packages/core/system-prompt/src/index.ts:424-505
  -> ScopedLayers.effect(registration Context, mutation)
       packages/core/scope/src/store.ts:226-266
  -> scopeOf(registration Context)
  -> NamedEntries.insert(name, value)
       packages/core/scope/src/store.ts:30-54
       -> duplicate in same exact layer => THROW
```

At assembly:

```text
scopeChainOf(scope)
  -> ScopedLayers.chainLayers(scope)
       packages/core/scope/src/store.ts:185-199
       -> farthest ancestor ... nearest exact scope

global NamedEntries
  -> ScopedLayers.merge(scope, table)
       packages/core/scope/src/store.ts:201-217
       -> global
       -> farthest ancestor overlay
       -> ...
       -> nearest scope overlay (same name wins)
```

The resulting order is not one generic rule:

```text
sections
  -> sort(order ASC, code-unit name ASC)
     packages/core/system-prompt/src/index.ts:221-229,555

contexts
  -> sort(order ASC)
     packages/core/system-prompt/src/index.ts:572-579
  -> equal-order tie follows stable effective-map insertion order

variables
  -> evaluate global, then farthest ... nearest scope
     packages/core/system-prompt/src/index.ts:523-533

tools
  -> collect every global + matching scoped provider
  -> configured toolOrder OR lexicographic name order
     packages/core/system-prompt/src/index.ts:538-554,580
```

## Path C — one Step creates an effective assembly

```text
ReactLoopAgent.turn
  packages/core/agent-loop/src/agent.ts:252-307
  -> choose next Step
  -> preStep(target, { turn, step })

ReactLoopAgent.preStep
  packages/core/agent-loop/src/agent.ts:232-249
  -> Inbox.claim(target, turn)
  -> loopCtx.systemPrompt.assemble(assembleContextFor(agent, signal))
```

Inside `SystemPrompt.assemble`:

```text
assemble({ scope: agent, signal, agent, ... })
  packages/core/system-prompt/src/index.ts:518-592
  -> chainLayers(scope)
  -> detect any global/scoped runtime-context suppressor
  -> evaluate variable providers
  -> merge sections by name
  -> merge contexts by name
  -> invoke all tool providers; structuredClone schema parameters
  -> sort sections
  -> reject if completeSections.length > 1
  -> evaluate section text providers
  -> sort/evaluate context text providers unless suppressed
  -> order tool schemas
  -> assembly = { sections, contexts, tools, variables }
  -> ctx.waterfall(scopeTarget(...), "system-prompt/assemble", assembly, context)
  -> accept waterfall return as authoritative
  -> if complete section active: restore it as sole section
  -> if suppression active: restore contexts = []
  -> return effective PromptAssembly
```

This is the last point where all system-section contributor names coexist with the effective values.

## Path D — rendering separates stable header from dynamic history

After `assemble()` returns:

```text
PromptAssembly.contexts
  -> renderContextSections(assembly)
       packages/core/system-prompt/src/index.ts:302-306
       -> interpolate each named context
       -> drop empty entries
       -> [{ name, text }, ...]
  -> joinContextSections(sections)
       packages/core/system-prompt/src/index.ts:287-290
       -> "Current runtime context..." + joined texts
  -> RuntimeContextProjection.project(current, sections)
       packages/core/agent-loop/src/runtime-context.ts:58-75
       -> unchanged retained text => undefined
       -> changed / removed retention => source-attributed UserMessage
          source = {
            kind: "plugin",
            plugin: "@deepseek-ai/dsh-system-prompt",
            form: "snapshot",
            sections: [{ name, text }, ...]
          }
       -> no active contexts after a previous snapshot => explicit clear marker
```

Then the `agent/pre-step` waterfall can transform the entered message batch:

```text
claimed messages + optional runtime snapshot
  -> dispatch.waterfall("agent/pre-step", ...)
       packages/core/agent-loop/src/agent.ts:241-249
  -> e.g. TimeContext prepended listener calls next(), then appends its own
     source-attributed UserMessage
       packages/context/time-context/src/index.ts:170-208
```

This yields an important source distinction:

```text
System Prompt stable lane
  PromptAssembly.sections
  -> renderPrompt(assembly)
       packages/core/system-prompt/src/index.ts:263-268
  -> one flat system string

Dynamic/context/task/history lane
  claimed task UserMessages
  + projected PromptContext snapshot when changed
  + other agent/pre-step contributors such as time-context
  -> append to session surface
  -> Session.deriveMessages()
```

`PromptSection.text` can be a callback, so “stable” means “placed in the cacheable system header lane,” not “guaranteed never to change.” If the rendered value changes, a changed header is logged.

## Path E — task and history enter the same frozen request

```text
ReactLoopAgent.turn
  packages/core/agent-loop/src/agent.ts:286-300
  -> append step/start
  -> append every entered UserMessage as user/message with surfaceOp: append
  -> step(assembly, ...)

ReactLoopAgent.step
  packages/core/agent-loop/src/agent.ts:339-364
  -> system = renderPrompt(assembly)
  -> boundaryMessages = session.deriveMessages()
       packages/core/session/src/index.ts:699-745
       -> ordered current surface only
       -> raw non-surface events omitted
       -> compaction replacement shadows old nodes
  -> buildRequest(turn, step, assembly.tools, system, boundaryMessages, ...)
```

Thus the “task contributor” is not a system-prompt registry object. The current task is a claimed `UserMessage`, and older tasks/replies are current surface history.

## Path F — header, route, and final `GenerateOptions`

```text
ReactLoopAgent.buildRequest
  packages/core/agent-loop/src/agent.ts:442-541
  -> seed route from AgentOptions.provider/model
  -> optionally reuse compatible persisted reasoning effort
  -> dispatch.waterfall("agent/request", { turn, step, signal })
  -> reject missing provider or model
  -> loopCtx.llm.prepareCall(proposedConfig, signal)
  -> canonicalHeader({
       config,
       adapterDefaults?,
       system? flat rendered string,
       tools? ordered schemas
     })
  -> append request/header when initial, resume, change, or new series
  -> append request/context when route/capacity changes
  -> markAgentLoopRequest(deepFreeze({
       ...header.config,
       messages: boundaryMessages,
       system?: header.system,
       tools?: header.tools,
       sessionId,
       signal
     }))
```

Provenance transformation at this boundary:

```text
sections [{ name, text }, ...]
  -> renderPrompt
  -> system: string                  # section names no longer present

contexts [{ name, text }, ...]
  -> RuntimeContextProjection
  -> UserMessage.source.sections     # names preserved durably

tool providers
  -> tools: ToolSchema[]             # tool names preserved, provider identity absent

task/history events
  -> Message[]                       # each Message.source preserved
```

## Path G — LLM runtime to DeepSeek HTTP wire

Prepared path:

```text
LlmRuntime.prepareCall(config, signal)
  packages/llm/llm/src/index.ts:889-934
  -> registration(config.provider)
  -> adapter.prepareCall(provider, model, signal)
  -> normalize exact-model info
  -> resolve defaults / retry policy / context capacity
  -> return one-shot PreparedLlmCall.stream(options)
```

Dispatch path:

```text
ReactLoopAgent.step
  packages/core/agent-loop/src/agent.ts:362-368
  -> preparedCall.stream(request) OR loopCtx.llm.stream(request)

LlmRuntime.streamWithRegistration
  packages/llm/llm/src/index.ts:1054-1063
  -> ctx.waterfall("llm/stream", options, terminal)
  -> adapterStream(options, prepared)
       packages/llm/llm/src/index.ts:963-1037
  -> registration-bound adapter stream
```

Concrete DeepSeek adapter path:

```text
DeepSeekAdapter.prepareCall
  packages/llm/llm-deepseek/src/adapter.ts:432-437
  -> snapshot connection generation
  -> bind streamWithConnection(options, connection)

DeepSeekAdapter.streamWithConnection
  packages/llm/llm-deepseek/src/adapter.ts:444-519
  -> validate image capability if needed
  -> resolve API key + anonymous user id
  -> combine caller and consumer abort signals
  -> request(options, signal, connection, apiKey, userId, ...)

DeepSeekAdapter.request
  packages/llm/llm-deepseek/src/adapter.ts:522-648
  -> form attribution/session/authorization headers
  -> serializeRequest(requestOptions, defaults)
       packages/llm/llm-deepseek/src/serialize.ts:381-392
       -> optional flattened system string becomes wire role=system message
       -> serializeMessages(options.messages)
       -> include tools/model/sampling fields
  -> JSON.stringify(body + extension fields)
  -> fetch(baseURL + "/chat/completions", POST, headers, payload, signal)
```

This closes the source path to a real provider request. It does not prove that a request was sent during Article 32's Lab.

## Path H — duplicate, override, bad variable, and terminal semantics

### Same-layer duplicate

```text
SystemPrompt.section({ name: "x", ... })
  -> PromptLayer.sections.insert("x", section)
  -> NamedEntries.data.has("x") === true
  -> THROW duplicate diagnostic
```

Sources: `packages/core/system-prompt/src/index.ts:355-375,432-440`; `packages/core/scope/src/store.ts:43-53`.

### Cross-scope override

```text
global section "deployment:persona"
  + agent-scope section "deployment:persona"
  -> ScopedLayers.merge
  -> nearest scope Map.set(same name, scoped value)
  -> one effective persona section
```

Sources: `packages/core/scope/src/store.ts:201-217`; `packages/preset/persona/src/index.ts:60-66`.

### Bad variable

```text
section.text contains "{{modle}}"
  -> assemble() retains uninterpolated section text
  -> renderPrompt(assembly)
  -> interpolate(input, variables, "section")
  -> Object.hasOwn(variables, "modle") === false
  -> THROW "unknown prompt variable"
```

Malformed name and registered-but-undefined value are separate terminal errors. Sources: `packages/core/system-prompt/src/index.ts:308-345,489-505`.

### Complete-section terminal rule

```text
effective sections
  -> completeSections = sections.filter(complete)
  -> count > 1 => THROW
  -> resolve exact complete section
  -> still run system-prompt/assemble waterfall
  -> restore resolved complete section as sole sections[] entry
```

Sources: `packages/core/system-prompt/src/index.ts:555-592`.

The waterfall may still alter contexts, tools, and variables. `complete` is terminal only for the **system-section lane**, not the whole model request.

## Path I — compaction and narrow re-injection

History rewrite:

```text
compaction selects current surface span
  -> summarizeCompaction()
  -> checkpoint UserMessage
  -> commitCompactionBody()
       packages/compaction/compaction-basic/src/region.ts:436-475
  -> session.append(user/message, checkpoint, {
       surfaceOp: { op: "replace", start, end },
       sourceEventSeqs: [...shadowed]
     })
  -> Session.surface.replaceGeneration increments
  -> next deriveMessages() rebuilds from replacement surface
```

Runtime-context retention reaction:

```text
RuntimeContextProjection constructor subscribes to session/event
  packages/core/agent-loop/src/runtime-context.ts:34-55
  -> replacement sourceEventSeqs includes retained snapshot seq
  -> retained = null

next Step
  -> SystemPrompt.assemble() resolves current PromptContexts again
  -> renderContextSections + joinContextSections
  -> RuntimeContextProjection.project()
  -> retained text is absent/null, so unchanged current snapshot is emitted again
  -> new source-attributed runtime-context UserMessage enters history
```

Candidate owner cases are at `packages/core/agent-loop/tests/loop.spec.ts:442-508`; this source pass does not claim they were freshly executed.

Stable system behavior differs:

```text
next Step
  -> reassemble sections
  -> render flat system
  -> canonicalHeader compares to persisted request header
  -> byte-equal header may stay implicit; changed value logs reason=change
```

There is no generic “compaction re-inject every invariant” call. The proven mechanism is:

- system sections are reconstructed for each Step outside session history;
- `PromptContext` owns a specific retained-snapshot detector and re-emission path;
- arbitrary task/plugin messages removed by compaction are represented only by the summary unless their own plugin independently regenerates them.

## Source-level request-diff expectations for Lab, not Lab results

A valid two-Step trace should distinguish these source-predicted cases:

| Change between Steps | Expected lane from source | Expected request effect |
|---|---|---|
| unchanged system sections and unchanged PromptContexts | stable header + retained snapshot | same system; no new runtime snapshot |
| dynamic PromptContext text changes | history snapshot | same system, new source-attributed user message |
| section text/provider value changes | system header | changed system and `request/header` reason `change` |
| task changes | history | new user message, system may remain byte-equal |
| tool visibility changes by scope | tools/header | changed ordered tool schemas; section guidance may or may not also change depending on scoped registration |
| compaction removes retained runtime snapshot | replacement + re-emission | summary replaces old span, current full snapshot is appended again |

These are experiment targets only. A runtime receipt must still show the actual two requests and their diff.

## Counter-paths that must not be narrated as the main path

1. `time-context` is an `agent/pre-step` message contributor, not a `PromptContext` registration.
2. `PromptAssembly` is not sent to the model. It is transformed into flat `system`, ordered `tools`, and source-bearing messages.
3. A flat provider request does not retain system-section names; calling it a complete provenance receipt would overclaim.
4. Same-name scoped sections shadow; same-name same-layer sections throw. There is no unrestricted last-registration-wins rule.
5. Context equal-order sorting lacks the section name tie-breaker. Do not copy the section comparator claim onto contexts.
6. Tool providers are additive and anonymous. Tool name ordering is deterministic, but provider provenance and duplicate-tool rejection are not supplied by `SystemPrompt`.
7. The `complete` flag does not bypass the assembly waterfall and does not erase tools/dynamic messages.
8. A compaction summarizer uses the previous flat system/tools as input (`packages/compaction/compaction-basic/src/region.ts:498-523`), but that is not itself proof of post-compaction invariant re-injection.

## Gate verdict

`SOURCE_MAP PASS`. Exact production arrows now cover registrations, scoped merge, ordering, strict interpolation, complete-section semantics, dynamic snapshot projection, task/history derivation, frozen request construction, model-provider dispatch, DeepSeek serialization, and the narrow compaction re-emission mechanism. `EXPERIMENT_DESIGN` is the next allowed gate.
