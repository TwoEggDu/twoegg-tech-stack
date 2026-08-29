# Article 31 Configuration and Capability Call Path

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / LAB NOT YET RUN AT THIS GATE`

This artifact preserves the source-investigation snapshot from before Lab execution. For current lifecycle state, see `README.md`; for current Claim status, see merged `research.md` and `evidence.md`; for current runtime results, see `experiments/effective-config-diff.md`.

Pinned source: `deepseek-ai/deepseek-harness@cd5ef8148158c3a752a658978873241fdf8e2bbc` (`dsh-v0.1.2-alpha.1`). Every arrow below is static source evidence unless explicitly marked as a proposed observation.

## A. Invocation to effective configuration

```text
process argv
  -> apps/cli/src/args.ts :: parseDshArgs()
       profile name
       repeated --patch paths retained in argv order
       inner app args kept separate
  -> apps/cli/src/bin.ts :: runProfile({ profile, patchFiles, args, environment })
  -> apps/cli/src/profile-boot.ts :: composeProfile(name, patchFiles)
  -> prepareProfile(name)
  -> packages/boot/app-boot/src/profile.ts :: loadProfile(...)
       -> resolveProfileDir()
       -> readProfileManifest()
       -> normalizeShippedProfile()
       -> manifest.dsh.profile.bundles (ordered)
       -> each bundle: resolveBundleDir()
            installation anchor first
            profile directory second
       -> bundle package.json :: dsh.bundle.patch
       -> loadOverlayPatches(bundle patch)
       -> loadOverlayPatches(profile cordis.patch.yml), if present
  -> healProfilesModuleFallback()
  -> loadOptionalPatches($DSH_HOME/cordis.patch.yml)
  -> patchFiles.flatMap(loadOverlayPatches), argv order
  -> bundlePatches = profile.layers.flatMap(...), manifest order
  -> composeEntries([bundlePatches, profilePatches, homePatches, cliOverlays])
       -> vendor/include :: applyEntryPatches([], cloned flattened patches)
  -> resolveTelemetryPatch(env, effective rows)
  -> allPatches()
       bundle -> profile user -> home user -> CLI overlays -> telemetry hard-disable
  -> boot(NAME, empty cordis.yml, structuredClone(allPatches), beforeMount)
       -> Include applies the same applyEntryPatches algorithm
       -> Loader evaluates row-local !!js during activation
       -> service availability drives construction/injection
```

Key exact symbols: `args.ts:58-60,125-134`; `bin.ts:24-35`; `profile-boot.ts:118-174,209-244`; `profile.ts:761-861`; `vendor/include/src/index.ts:58-112`.

The intermediate `composeEntries()` pass inside `composeProfile()` is used only to decide whether the telemetry row exists. The actual boot still receives the ordered patch stack and applies it over the empty include root. Both paths deliberately use `applyEntryPatches()`.

## B. Layer conflict path

For one unique id, `applyEntryPatches()` follows this algorithm:

```text
structuredClone(empty root)
  -> build id index
  -> patch 1
       insert? append + index newly inserted rows
       override? find id; optional name assertion; assign top-level fields
  -> patch 2
  -> ...
  -> detached effective EntryOptions[]
```

Concrete shipped example:

```text
dsh-base bundle
  -> insert id=system-prompt config.persona=''
web-app OR headless bundle
  -> patch id=system-prompt
  -> replace the whole config object with surface persona
profile cordis.patch.yml
  -> may replace config again
$DSH_HOME/cordis.patch.yml
  -> may replace config again
--patch first.yml
  -> may replace config again
--patch second.yml
  -> final CLI-owned replacement
```

Counterpaths retained:

- Unknown id → warning + skipped patch, not row creation.
- `name` assertion mismatch → warning + skipped patch.
- `config` → whole-object assignment, not recursive merge.
- Duplicate inserted id → both structural rows remain, while subsequent id lookup points at the last indexed row; do not summarize this as deduplication.
- `--dump-config` → same static composition order, but no boot and no `!!js` evaluation.

## C. Shipped profile fork

```text
PROFILE_TEMPLATES
  -> web
       dsh-base
         -> shared agent/session/LLM/tools/sandbox/FS capability rows
       dsh-web-app
         -> cordis-host-runner
         -> web-startup (consumes cmdlineArgs; provides webStartup)
         -> host-webserver (injects webStartup)
         -> web-runtime (injects webStartup; later provides webRuntime)
         -> controllers + browser roster
       patchReload=live

  -> headless
       dsh-base
         -> same shared capability rows
       dsh-headless
         -> code-runtime
         -> headless-startup (consumes cmdlineArgs; provides headlessStartup)
         -> headless-runner (injects headlessStartup)
       patchReload=startup
       X no cordis-host-runner
       X no host-webserver
       X no web-runtime/browser roster
```

Exact profile fork: `packages/boot/app-boot/src/profile.ts:137-163`. Exact surface patches: `packages/bundle/headless/cordis.patch.yml:1-30` and `packages/bundle/web-app/cordis.patch.yml:39-137`.

The old installation-owned headless tuple `[base, web-app, headless]` is an explicit migration source normalized to `[base, headless]`; it is counterevidence, not a third current layer.

## D. Filesystem Capability Seam

### D1. Definition and provider publication path

```text
packages/fs/fs/src/index.ts
  FileSystem extends Cordis Service
  -> constructor super(ctx, 'fs')
  -> abstract resolve/readText/writeText/editText contract

packages/bundle/base/cordis.patch.yml
  id=fs-sandbox
  name=@deepseek-ai/dsh-fs-sandbox
  -> Loader waits for SandboxedFileSystem.static inject=['sandboxPolicy']
  -> packages/fs/fs-sandbox/src/index.ts :: SandboxedFileSystem(ctx, config)
       -> extends LocalFileSystem
       -> LocalFileSystem extends FileSystem
       -> FileSystem constructor publishes service identity 'fs'
       -> reads use LocalFileSystem implementation
       -> writes/edits pass checkedTarget(policy), then inherited atomic operation
```

This is the shipped provider. `LocalFileSystem` is implementation inheritance, not a second active provider row in `dsh-base`.

### D2. Consumer activation path

```text
packages/bundle/base/cordis.patch.yml
  id=tool-fs
  name=@deepseek-ai/dsh-tool-fs
  -> packages/fs/tool-fs/src/index.ts
       inject=['tools','fs','systemPrompt']
       -> only after all required services are available: apply(ctx, config)
            -> applyReadTool()
            -> applyWriteTool()
            -> applyEditTool()
            -> optional ctx.inject(['attachments']) -> applyReadImageTool()
```

The optional `attachments` path proves another useful rule: a plugin can be active while one conditional sub-capability (`read_image`) remains unregistered.

### D3. One read operation

```text
model/tool dispatcher
  -> registered read tool (tool-fs/src/read.ts:69-76)
  -> resolveReadTarget() (read-target.ts:24)
  -> ctx.fs.resolve(requestedPath, session options)
  -> ctx.fs.stat / streamText OR readText (read.ts:118-146)
  -> bounded rendered tool result
```

### D4. One write operation

```text
model/tool dispatcher
  -> registered write tool (tool-fs/src/write.ts:62-69)
  -> FsSandboxController resolves per-call policy / escalation result
  -> ctx.fs.resolve(filePath, session cwd/policy root) (write.ts:108)
  -> ctx.fs.writeText(target, content, intent, signal, sandboxPolicy) (write.ts:114)
  -> SandboxedFileSystem.checkedTarget()
       danger-full-access -> delegate
       read-only -> FS_SANDBOX_DENIED
       workspace-write -> re-resolve + writableRoots containment
  -> LocalFileSystem.withLock(targetKey)
  -> guard expected intent
  -> writeFileAtomic()
  -> FsWriteOutcome
```

## E. Configured is not active

```text
effective EntryOptions contains fs-sandbox + tool-fs
  = CONFIGURED
  != provider constructed
  != ctx.fs published
  != tool-fs injections settled
  != tools registered
  != model invoked a file operation
```

To advance each equality boundary, evidence must change:

1. Config dump/source map closes only `CONFIGURED`.
2. Loader/runtime service inspection closes provider `ACTIVE`.
3. Tool registry/runtime inspection closes consumer `ACTIVE`.
4. A captured call/result closes an operation path.

The base patch itself states that row order carries no load semantics; activation is service-availability driven (`packages/bundle/base/cordis.patch.yml:12-13`). Therefore a line appearing earlier in YAML cannot prove it constructed earlier, and an enabled row cannot prove construction succeeded.

## F. Live reload path and invariant

```text
web profile (patchReload=live)
  -> runProfile.composeLive()
       bundle snapshots
       + freshly reread profile user patch
       + freshly reread home user patch
       + frozen CLI/telemetry overlays
       + structuredClone per generation
  -> watchUserPatches(profile file)
  -> watchUserPatches(home file)
  -> Include/Loader transactional update

headless profile (patchReload=startup)
  -> full stack applies at boot
  -> no user-patch watcher installed
```

Invariant: user edits may replace configuration but cannot move above CLI or telemetry overlays, and rereading both user files avoids stitching one fresh file to the other watcher’s stale copy (`apps/cli/src/profile-boot.ts:229-267`).

## G. Historical Lab handoff: hypotheses, not results at this gate

1. `--dump-config` should show bundle → profile → home → CLI source sections and the final whole-config replacement.
2. Reversing two `--patch` arguments should reverse which unique-id override is effective.
3. A harmless runtime probe should distinguish configured rows from active `ctx.fs` and registered FS tools.
4. On a current headless profile, no Web Host rows should appear unless the experiment deliberately supplies a custom bundle/overlay.

Until Lab records those observations, label them `PROPOSED EXPERIMENT`, not runtime-confirmed behavior.
