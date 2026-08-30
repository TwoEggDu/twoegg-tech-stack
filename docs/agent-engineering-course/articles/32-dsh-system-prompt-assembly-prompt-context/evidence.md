# Article 32 Evidence

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## Evidence summary

- Frozen DSH revision：`dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Source/runtime closure：`contributors -> PromptAssembly -> render -> Agent pre-step -> Session history -> MockAdapter requests`
- Claim count：`15`
- Final status：`13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED`
- Request trace：`2 MockAdapter requests; messageCount 2 -> 5; system/tools/route stable; dynamic snapshot changed`
- Runtime tests：`system-prompt 68/68; focused AgentLoop 5/5 selected`
- Direct negatives：`duplicate section / invalid variable name / unknown variable reference`
- Excluded：`real provider/model/network/token/cost / BuildPilot implementation`

### Evidence 32-E01｜Pinned identity and clean research input

- Claim ID: `32-C01`
- Claim: `Article 32 research is bounded to the official frozen revision and began from a clean external fixture.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Commit / Tag: `cd5ef8148158c3a752a658978873241fdf8e2bbc / dsh-v0.1.2-alpha.1`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Observation: `git rev-parse HEAD and git describe --tags --exact-match matched the frozen baseline; git status --short returned no rows.`
- Proves: `version and initial fixture identity`
- Does Not Prove: `prompt assembly behavior`
- Course Decision: `BOUND ALL DSH FACTS TO THIS REVISION`

### Evidence 32-E02｜Exact prompt assembly data contracts

- Claim ID: `32-C02`
- Claim: `PromptSection, PromptContext, AssembleContext, PromptAssembly and ContextSnapshotSection expose the pinned fields recorded in research; the selected source has no standalone PromptProvider interface.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Location: `packages/core/system-prompt/src/index.ts:41-120; packages/llm/llm/src/message.ts:63-88`
- Symbols: `AssembleContext; PromptSection; PromptContext; PromptAssembly; ContextSnapshotSection; ContextFormed`
- Observation: `PromptSection has name/order/text/complete; PromptContext has name/order/text; text accepts static string or per-assembly callback; PromptAssembly has sections/contexts/tools/variables; snapshot source sections have name/text.`
- Counter-evidence Searched: `a named PromptProvider interface in the selected core path`
- Interpretation: `Provider is a role played by callbacks/tool-variable provider types, not a source type that should be invented for prose convenience.`
- Proves: `selected exact source fields and terminology boundary`
- Does Not Prove: `runtime-loaded contributors for a product Profile`
- Course Decision: `USE SOURCE NAMES; DO NOT INVENT A UNIFIED CURRENT API`

### Evidence 32-E03｜Deterministic assembly algorithm

- Claim ID: `32-C03`
- Claim: `Section, Context, Tool and Variable providers enter PromptAssembly through a deterministic merge/order/evaluation path followed by the scoped assembly waterfall.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Location: `packages/core/system-prompt/src/index.ts:518-592`
- Symbols: `SystemPrompt.assemble; ScopedLayers.merge; orderTools; system-prompt/assemble`
- Call Path: `variables -> scoped merges -> tool providers -> section sort -> provider evaluation -> context sort/evaluation -> waterfall -> complete/suppression restoration`
- Experiment: `two-Step Effective Assembly capture in experiments/prompt-assembly-trace.md`
- Observation: `Sections sort by order then code-unit name; equal-order zeta/alpha registered in that order assembled alpha/zeta. Contexts sort by order only; equal-order zeta/alpha remained zeta/alpha by stable effective-map insertion order. Tools are separately canonicalized.`
- Proves: `source algorithm and selected runtime ordering observation`
- Does Not Prove: `every product Profile's contributor set`
- Course Decision: `REQUIRE EFFECTIVE ASSEMBLY DUMP BEFORE RUNTIME CLAIM`

### Evidence 32-E04｜Duplicate, shadow and scope rules

- Claim ID: `32-C04`
- Claim: `Same-layer duplicate Section/Context/Variable registrations fail, while a scoped same-name contribution shadows global only for matching scoped assembly.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + EXPERIMENT + OWNER_TEST_RUNTIME`
- Source Location: `packages/core/system-prompt/src/index.ts:354-375,424-505,518-536; packages/core/system-prompt/tests/scoped.spec.ts:32-162`
- Symbols: `PromptLayer; section; context; variable; ScopedLayers.merge`
- Experiment: `direct duplicate-section probe; complete system-prompt owner suite`
- Observation: `duplicate-demo second same-layer registration produced the exact already-registered error; NamedEntries owns per-layer diagnostics; scoped same-name entries merge as overrides. The owner command exited 0 with 68/68 tests passed.`
- Counter-evidence: `same name across global and scope is intentional override, not duplicate registration.`
- Proves: `registration and scope mechanism in source`
- Does Not Prove: `unrestricted last-write-wins or arbitrary Profile activation`
- Course Decision: `KEEP DUPLICATE AND SCOPE SHADOW AS DIFFERENT RULES`

### Evidence 32-E05｜Complete-section terminal semantics

- Claim ID: `32-C05`
- Claim: `One effective complete section becomes the sole system section after waterfall; multiple effective complete sections fail; Context, Tool and Variable assembly continues.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + OWNER_TEST_RUNTIME`
- Source Location: `packages/core/system-prompt/src/index.ts:52-75,555-592; packages/core/system-prompt/tests/system-prompt.spec.ts:305-330`
- Symbols: `PromptSection.complete; completeSections; completeSection`
- Experiment: `system-prompt owner suite, included in 68/68 passing tests`
- Observation: `The callback-resolved complete contribution is copied before waterfall, restored afterward as sections:[completeSection], while transformed contexts/tools/variables remain unless runtime context was suppressed; one-complete restoration and multiple-complete rejection tests passed.`
- Proves: `terminal semantics of the system-section lane`
- Does Not Prove: `turn/request termination; those are separate AgentLoop states`
- Course Decision: `DO NOT CALL COMPLETE A TURN TERMINAL`

### Evidence 32-E06｜Strict variables and bad-variable cases

- Claim ID: `32-C06`
- Claim: `Prompt variables are strictly registered and interpolated at render; unknown, undefined and malformed references fail, while substituted values are not recursively scanned.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + EXPERIMENT + OWNER_TEST_RUNTIME`
- Source Location: `packages/core/system-prompt/src/index.ts:174-178,255-346,489-505; packages/core/system-prompt/tests/system-prompt.spec.ts:449-600`
- Symbols: `VARIABLE_NAME; GROUP_AT; interpolate; variable; renderPrompt; renderContextSections`
- Experiment: `direct invalid-name and unknown-reference probe; 68/68 system-prompt owner tests; 5/5 focused loop tests`
- Observation: `Bad-Name failed at registration; {{missing}} survived assembly then failed in render with section name and registered variables (none). Owner cases also passed for undefined, malformed, nested, prototype-property and non-rescanned replacement values; focused loop proved a bad variable prevents a request and a later repaired turn can proceed.`
- Proves: `bad-variable rules and attribution in source`
- Does Not Prove: `every possible external waterfall mutation`
- Course Decision: `KEEP REGISTRATION, ASSEMBLY AND RENDER FAILURES SEPARATE`

### Evidence 32-E07｜Stable system lane versus dynamic snapshot lane

- Claim ID: `32-C07`
- Claim: `Stable prompt sections render into request.system, while PromptContext renders into optional sourced user-message snapshots after retained history.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + MOCK_ADAPTER_RUNTIME`
- Source Location: `packages/core/system-prompt/src/index.ts:263-305; packages/core/agent-loop/src/agent.ts:232-249,339-357; packages/core/agent-loop/src/runtime-context.ts:58-75`
- Symbols: `renderPrompt; renderContextSections; joinContextSections; RuntimeContextProjection.project; Agent.preStep; Agent.step`
- Call Path: `assemble -> render contexts -> project optional message -> session append -> render system -> buildRequest`
- Experiment: `two-Step request trace plus focused AgentLoop owner cases`
- Observation: `Both requests had byte-identical system and tool list. Dynamic PromptContext changed from mode=read-only/tick=1 to mode=write-enabled/tick=2 and appended a new sourced snapshot; messageCount changed 2 -> 5.`
- Proves: `channel and update-semantics separation in the selected MockAdapter path`
- Does Not Prove: `that all Section text is byte-stable across steps`
- Course Decision: `MODEL STABLE AND DYNAMIC LANES SEPARATELY`

### Evidence 32-E08｜Representative multi-owner contributor map

- Claim ID: `32-C08`
- Claim: `Identity, host guidance, tool guidance, variables, policy state, clock/terminal state, workspace instructions, user task and history have distinct representative owners and channels.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Location: `packages/core/system-prompt/src/index.ts:388-421; packages/boot/app-boot/src/index.ts:825-845; packages/bundle/web-app/src/index.ts:235-250; packages/core/agent-loop/src/index.ts:347-354; packages/core/tools/src/index.ts:830-836; packages/sandbox/sandbox-policy/src/index.ts:104-123; packages/interaction/user-approval/src/index.ts:162-181; packages/context/time-context/src/index.ts:170-208; packages/context/tmux-context/src/index.ts:218-246; packages/context/agent-instructions/src/index.ts:322-348`
- Observation: `Some owners register PromptSection/PromptContext, while time/tmux/instructions modify pre-step user messages and task/history flow through Session.`
- Proves: `representative multi-channel source map`
- Does Not Prove: `exhaustive inventory of every installed plugin or a given Profile's activation`
- Course Decision: `DO NOT FORCE ALL CONTEXT THROUGH ONE CURRENT API`

### Evidence 32-E09｜Per-Step convergence into request

- Claim ID: `32-C09`
- Claim: `AgentLoop assembles before every admitted Step and converges rendered system, ordered tools and Session-derived messages in buildRequest.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + MOCK_ADAPTER_RUNTIME`
- Source Location: `packages/core/agent-loop/src/agent.ts:232-249,339-357,442-541`
- Symbols: `preStep; step; buildRequest`
- Call Path: `inbox.claim -> systemPrompt.assemble -> runtime context projection -> session user/message append -> renderPrompt -> session.deriveMessages -> canonicalHeader -> frozen GenerateOptions`
- Experiment: `real AgentLoop two-Step trace captured at MockAdapter.requests`
- Observation: `Two normalized frozen requests reached the terminal MockAdapter; route/system/tools/messages matched the pre-render assembly and durable Session evolution described in call-path.md.`
- Proves: `source request construction path joined to the selected adapter receipt`
- Does Not Prove: `real provider SDK/HTTP dispatch or model behavior`
- Course Decision: `KEEP MOCK ADAPTER BOUNDARY EXPLICIT`

### Evidence 32-E10｜Durable header and history ownership

- Claim ID: `32-C10`
- Claim: `Request headers durably record route/system/tools, while Session surface is authoritative for derived model history and rebuilds after replacement generation changes.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Location: `packages/core/agent-loop/src/agent.ts:496-517; packages/core/session/src/index.ts:699-745`
- Symbols: `canonicalHeader; request/header; Session.deriveMessages; SessionSurface.replaceGeneration`
- Observation: `Header append reasons distinguish initial/resume/change/series; deriveMessages walks surface nodes and resets its cache when replacement generation changes.`
- Proves: `selected durable ownership and reconstruction path`
- Does Not Prove: `external persistence backend durability, provider wire format or real call success`
- Course Decision: `KEEP HEADER RECEIPT AND MESSAGE HISTORY DISTINCT`

### Evidence 32-E11｜Two-Step MockAdapter request diff

- Claim ID: `32-C11`
- Claim: `In the Article 32 target fixture, stable system/tools remain equal across two Steps while dynamic/history inputs produce an attributable request diff.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `MOCK_ADAPTER_RUNTIME`
- Experiment: `experiments/prompt-assembly-trace.md Experiment A`
- Observation: `Two AgentLoop Steps reached MockAdapter. Request messageCount changed 2 -> 5; provider/model stayed mock/mock; rendered system and tools [flip_mode] were stable; request 2 added assistant tool-call, successful tool result and changed named PromptContext snapshot.`
- Trace: `request hashes 72326D5189BF92BC67C41745A3F61358291B670E0CDB07D0972927C9120B78CA -> 5705EE4D9EF5B6A6F3654D92D3EFC8D058D2C301AC3B2721F5976C4FB735AE89`
- Proves: `selected two-Step assembly/request mechanics and attributable diff`
- Does Not Prove: `real model/provider request, token usage or cost`
- Course Decision: `CONFIRMED WITH MOCK BOUNDARY`

### Evidence 32-E12｜Narrow provenance and flattened-system loss

- Claim ID: `32-C12`
- Claim: `Current mechanisms retain partial provenance, but a unified source-to-transform Effective Assembly receipt is not yet confirmed in the pinned path.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + BOUNDED ABSENCE SEARCH + MOCK TRACE`
- Source Location: `PromptAssembly names; ContextSnapshotSection name/text; message sources; request/header system/tools`
- Observation: `Dynamic snapshots retain ordered named text; rendered system is flat; variables expose effective values; waterfall mutation has no receipt field in PromptAssembly.`
- Source Search: `repository-map.md/call-path.md found no general IContextContributor or Receipt in the pinned production tree.`
- Observation: `Effective Assembly retains pre-render names; PromptContext source.sections durably retains ordered name/text. renderPrompt flattens system sections, so final system/request-header loses section names; anonymous tool-provider identity and waterfall transform ledger are also absent.`
- Proves: `existing narrow provenance and exact loss boundary in the searched pinned path`
- Does Not Prove: `official design intent or permanent repository-wide absence in future revisions`
- Course Decision: `DO NOT CALL A FLAT REQUEST A COMPLETE ASSEMBLY RECEIPT`

### Evidence 32-E13｜Compaction-aware runtime-context re-projection path

- Claim ID: `32-C13`
- Claim: `When replacement shadows the retained system-prompt-owned runtime snapshot, source contains a next-pre-step re-projection path for the current complete snapshot.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + FOCUSED OWNER RUNTIME`
- Source Location: `packages/core/agent-loop/src/runtime-context.ts:24-75; packages/core/agent-loop/src/agent.ts:232-249; packages/core/agent-loop/tests/loop.spec.ts:439-556`
- Symbols: `RuntimeContextProjection.retained; isReplacementSurfaceEvent; project; preStep`
- Call Path: `replacement sourceEventSeqs contains retained seq -> retained=null -> next assemble/render contexts -> project current snapshot -> user/message`
- Experiment: `focused loop.spec.ts command: 1 file, 5/5 selected passed, 51 skipped`
- Observation: `Source watches authoritative replacement events; runtime owner cases passed for unchanged active snapshot re-emission after replacement and explicit clear marker when active context becomes empty.`
- Scope Limit: `system-prompt-owned PromptContext snapshot only; not a generic reinjection guarantee for time/tmux/instructions; stable system is re-rendered per Step rather than restored from history.`
- Proves: `narrow RuntimeContextProjection source and owner-runtime behavior`
- Does Not Prove: `generic reinjection of arbitrary context/task messages or compaction provider correctness`
- Course Decision: `CONFIRMED ONLY FOR PROMPTCONTEXT PROJECTION`

### Evidence 32-E14｜BuildPilot IContextContributor proposal

- Claim ID: `32-C14`
- Claim: `BuildPilot may adopt one explicit IContextContributor interface over stable/dynamic/history lanes.`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Basis: `confirmed multi-owner/multi-channel assembly boundaries`
- Proposed Fields: `id; lane; contribute(input)`
- Rationale: `Unify observability and ownership without pretending DSH currently exposes this interface.`
- Counter-evidence: `One interface can erase different durability semantics unless lane is explicit.`
- Proves: `N/A — future design candidate`
- Does Not Prove: `BuildPilot architecture, ADR, code or runtime`
- Course Decision: `PROPOSAL ONLY`

### Evidence 32-E15｜BuildPilot effective assembly receipt proposal

- Claim ID: `32-C15`
- Claim: `BuildPilot should emit a safe per-request Receipt that records source, scope, order, transforms, output hash and include/shadow/reject decision, with stable and dynamic lanes separated.`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Basis: `partial DSH provenance plus flattened system/variable/waterfall gaps`
- Proposed Invariant: `every effective contribution has an attributable decision; secret plaintext is not logged`
- Required Future Validation: `duplicate/override/bad-variable/terminal/compaction acceptance tests and redaction review`
- Proves: `N/A — future design candidate`
- Does Not Prove: `implemented receipt, security approval or performance cost`
- Course Decision: `PROPOSAL ONLY`

## Evidence Gate recommendation

`EVIDENCE_MERGE PASS / OUTLINE ELIGIBLE`。

15 个 Claim 与 15 张 Evidence Card 一一对应：`13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。Two-Step MockAdapter diff、three direct negatives、`68/68` system-prompt owner tests、`5/5` focused AgentLoop tests、exact order/scope/conflict、narrow PromptContext compaction re-emission 与 flat-system provenance loss 均已闭合。MockAdapter 不等于 real provider；BuildPilot `IContextContributor + Receipt` 仍仅为 proposal。Next allowed gate：`OUTLINE`。
