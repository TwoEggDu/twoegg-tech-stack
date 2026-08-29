# Article 31 Repository Map

Status: `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / LAB NOT YET RUN AT THIS GATE`

This artifact preserves the source-investigation snapshot from before Lab execution. For current lifecycle state, see `README.md`; for current Claim status, see merged `research.md` and `evidence.md`; for current runtime results, see `experiments/effective-config-diff.md`.

## Source baseline and scope

- Repository: `deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: external read-only investigation checkout; `git status --short` was empty at mapping time.
- This map is static source evidence. It proves composition rules and provider/consumer wiring, not that a particular runtime generation mounted successfully.

## 1. Profile contract and validation boundary

| Concern | Exact source | Symbol / row | What the source establishes |
|---|---|---|---|
| Manifest shape | `packages/boot/app-boot/src/profile.ts:52-91` | `DshBundleManifest`, `DshProfileManifest`, `ProfileTemplate`, `DshManifestSection`, `ProfileManifest` | A bundle declares `dsh.bundle.patch`; a profile declares an ordered `dsh.profile.bundles` list and optional `patchReload: live \| startup`. This is a TypeScript contract, not by itself runtime validation. |
| Shipped profile defaults | `packages/boot/app-boot/src/profile.ts:137-158` | `PROFILE_TEMPLATES` | `web = [dsh-base, dsh-web-app]` with live patch reload; `headless = [dsh-base, dsh-headless]` with startup-frozen reload. |
| Retired tuple migration | `packages/boot/app-boot/src/profile.ts:160-163, 714-743` | `INSTALLATION_OWNED_PROFILE_TUPLES`, `normalizeShippedProfile()` | The old shipped headless tuple containing `dsh-web-app` is normalized to the current template. A user-modified tuple is preserved. This is direct counterevidence to treating Web Host as part of current headless. |
| Directory/name validation | `packages/boot/app-boot/src/profile.ts:121-134` | `resolveProfileDir()` | Empty, slash-bearing, dot, dot-dot and `node_modules` profile names fail before path resolution. |
| JSON/object and enum validation | `packages/boot/app-boot/src/profile.ts:684-710, 805-843` | `readProfileManifest()`, `loadProfile()` | Manifest must parse to an object; `patchReload` must be `live` or `startup`. Missing shipped profiles initialize from templates; missing custom profiles fail loud. |
| Bundle validation | `packages/boot/app-boot/src/profile.ts:761-800, 830-838` | `resolveBundleDir()`, bundle mapping in `loadProfile()` | Bundle resolution is installation-first, profile-second. A listed package without `dsh.bundle.patch`, or a missing patch file, fails loud. |
| Patch-file schema | `packages/boot/app-boot/src/index.ts:271-307`; `vendor/include/src/index.ts:10-27, 173-208` | `loadOptionalPatches()`, `loadOverlayPatches()`, `entryListSchema`, `Include.read()` | Patch documents are top-level YAML/JSON arrays using the Loader entry-list dialect with `!!js`. Optional user files alone may be absent; named bundle/CLI overlay files may not. Parse and shape errors are fatal. |

There is no single Zod-style “profile schema” object. The effective runtime schema is split across TypeScript interfaces, explicit manifest checks in `loadProfile()`, the YAML `entryListSchema`, and Loader/plugin config schemas evaluated later during activation. Calling only the interfaces “validated config” would overstate the source.

## 2. Files that own composition and effective config

| Stage | Exact source | Ownership |
|---|---|---|
| CLI collection | `apps/cli/src/args.ts:58-60, 85-102, 125-134` | Repeated `--patch <path>` is collected in argv order; dump-default rejects CLI overlays. |
| CLI dispatch | `apps/cli/src/bin.ts:24-35` | The parsed profile, ordered patch paths, launch environment and inner app args enter `runProfile()`. |
| Profile preparation | `apps/cli/src/profile-boot.ts:72-123` | `prepareProfile()` calls `loadProfile()`, then rewrites an empty `cordis.yml` root so prior Loader write-back cannot be mistaken for a base layer. |
| Bundle and user loading | `packages/boot/app-boot/src/profile.ts:805-843` | `loadProfile()` resolves bundle manifests/patches in manifest order and optionally reads profile-local `cordis.patch.yml`. |
| Home and CLI overlays | `apps/cli/src/profile-boot.ts:156-174` | `composeProfile()` appends home patch, then each `--patch` file in argv order, then derives the telemetry hard-disable patch after inspecting the pre-telemetry effective rows. |
| Effective entry list | `packages/boot/app-boot/src/profile.ts:846-861`; `vendor/include/src/index.ts:44-112` | `composeEntries()` delegates to the same `applyEntryPatches()` used by boot. Starting from `[]`, it clones, flattens layers in order, inserts rows and applies id-targeted overrides. |
| Runtime mount | `apps/cli/src/profile-boot.ts:209-272`; `packages/boot/app-boot/src/index.ts:743-789` | `runProfile()` passes a cloned full patch stack to `boot()` over the empty root; Loader activation and service injection then determine what becomes active. |
| Boot-free observability | `apps/cli/src/dump-config.ts:20-55` | `--dump-config` renders the same bundle/profile/home/CLI layer order without booting or evaluating `!!js`; it is a composition diagnostic, not runtime-active proof. |
| Live recomposition | `apps/cli/src/profile-boot.ts:229-267` | Live profiles reread both user files each generation, keeping bundle layers below and CLI/telemetry overlays above. Startup profiles apply the layers once and install no patch watchers. |

## 3. Actual precedence and conflict rules

Lowest to highest precedence for a normal boot:

1. Bundle patches in `dsh.profile.bundles` order.
2. `$DSH_HOME/profiles/<name>/cordis.patch.yml`.
3. `$DSH_HOME/cordis.patch.yml`.
4. Each `--patch` file in argv order.
5. Launcher-derived telemetry disable patch, when `DSH_TELEMETRY_DISABLED` is non-empty and the telemetry row exists.

`vendor/include/src/index.ts:58-112` defines the non-obvious conflict semantics:

- An `insert` appends entries and immediately indexes their ids, so later patches—even later in the same list—can target them.
- A non-insert patch needs an existing id. A missing id target warns and is skipped; it does not create a row.
- If a patch also asserts `name`, a mismatch warns and skips the patch.
- Override fields are assigned at the top level. In particular, `config` is replaced as a whole, not deep-merged. Mode and user overlays therefore must restate every config key they own.
- The input and flattened patches are cloned. Removing an override on a later live generation can revert to the bundle default instead of retaining in-memory mutations.
- A duplicate inserted id does not remove the older row; the id index points to the last indexed entry. Therefore “last patch wins” is safe for a unique row id, but not a license to insert duplicate ids and assume structural deduplication.

## 4. Shipped headless versus Web topology

| Shared / divergent | Headless | Web | Source |
|---|---|---|---|
| Shared base | `@deepseek-ai/dsh-base` | `@deepseek-ai/dsh-base` | `PROFILE_TEMPLATES`, `profile.ts:142-149` |
| Surface bundle | `@deepseek-ai/dsh-headless` | `@deepseek-ai/dsh-web-app` | same |
| Shared base capabilities | Core LLM/session/agent/tools/sandbox/FS rows, including `fs-sandbox` and `tool-fs` | Same base rows | `packages/bundle/base/cordis.patch.yml` |
| Host difference | Adds `code-runtime`, `headless-startup`, `headless-runner`; explicitly mounts no Host, HTTP server, Web runtime or browser plugin | Adds Web controllers, `cordis-host-runner`, `web-startup`, `webserver`, `web-runtime`, browser roster and client-facing rows | `packages/bundle/headless/cordis.patch.yml:1-30`; `packages/bundle/web-app/cordis.patch.yml:39-137` |
| Lifecycle difference | One-shot task; `headless-runner` consumes `headlessStartup` | Long-lived server/browser surface; bind config consumes `webStartup`, runtime later publishes `webRuntime` | same |
| Patch reload | `startup` | `live` | `PROFILE_TEMPLATES` |

Counterevidence: the word “host” should not be projected onto every process. The CLI launcher is the application process for both profiles, but the repository’s explicit Host/Web stack (`dsh-host-webserver`, `cordis-host-runner`, browser modules) belongs to the Web bundle, not current headless.

## 5. Capability Seam: filesystem definition → provider → consumer

This seam is selected because all three roles are directly visible in source and the same base composition used by both shipped headless and Web includes it.

### Service Definition

- `packages/fs/fs/src/index.ts:86-240` — abstract `FileSystem extends Service`; its constructor calls `super(ctx, 'fs')` (`:87-89`), defining the `ctx.fs` service identity and operations such as `resolve`, `readText`, `writeText`, and `editText`.
- This package specifies the capability contract; it performs no host I/O by itself.

### Provider

- `packages/fs/fs-local/src/index.ts:64-238` — `LocalFileSystem extends FileSystem` implements host-path resolution, reads, atomic writes and serialized edits.
- `packages/fs/fs-sandbox/src/index.ts:55-135` — `SandboxedFileSystem extends LocalFileSystem`, injects `sandboxPolicy`, inherits reads unchanged, and overrides mutations to call `checkedTarget()` before delegating.
- `packages/bundle/base/cordis.patch.yml:491-494` — shipped base config inserts row `fs-sandbox` using `@deepseek-ai/dsh-fs-sandbox`; it does **not** separately mount `dsh-fs-local`.

### Consumer

- `packages/fs/tool-fs/src/index.ts:22, 54-75` — plugin `tool-fs` declares `inject = ['tools', 'fs', 'systemPrompt']`, then registers read/write/edit tools.
- `packages/fs/tool-fs/src/read-target.ts:24`, `read.ts:146` — read path resolves through `ctx.fs`, then uses `ctx.fs.readText()` (or streaming).
- `packages/fs/tool-fs/src/write.ts:62-122` — write registers through `ctx.tools`, resolves through `ctx.fs`, and calls `ctx.fs.writeText(...)` with the request-time sandbox policy.
- `packages/fs/tool-fs/src/edit.ts:76-135` — edit follows the same service seam via `ctx.fs.editText(...)`.
- `packages/bundle/base/cordis.patch.yml:266-267` inserts the consumer row `tool-fs`.

### Configured versus active

| State | Minimum evidence | Claim allowed |
|---|---|---|
| Configured | Effective entry list contains enabled `fs-sandbox` and `tool-fs` rows | The selected profile asks Loader to mount this provider/consumer topology. |
| Provider active | Loader instantiated `SandboxedFileSystem` after `sandboxPolicy` became available; `ctx.fs` resolves | The FS capability is published for that runtime generation. |
| Consumer active | `tools`, `fs`, and `systemPrompt` injections all settle and `tool-fs.apply()` registers tool definitions | The model-facing FS tools are registered against the active provider. |
| Operation observed | A tool call reaches `ctx.fs.*` and returns/throws an observed result | That particular capability path executed. |

Source alone establishes the wiring, not the last three runtime facts. Row order is presentation-only: `packages/bundle/base/cordis.patch.yml:12-13` explicitly says activation is service-availability driven. A config dump also leaves `!!js` unevaluated (`apps/cli/src/dump-config.ts:1-6`), so it cannot prove platform gates, provider construction, or tool registration.

## 6. Counterevidence and limits retained for the article

1. “Profile schema” is distributed validation, not one schema object.
2. “Later layer wins” means top-level replacement for matched unique ids; it is not a deep merge and missing targets merely warn.
3. Current headless does not contain the Web Host. The historical tuple is migration input, not current topology.
4. `fs-local` contains the mechanics but `fs-sandbox` is the shipped provider row; naming both as concurrently mounted providers would be false.
5. `fs-sandbox` is a trusted path-policy fence, explicitly not a kernel boundary (`packages/fs/fs-sandbox/src/index.ts:1-26`).
6. Configured rows are not active services. Only a runtime experiment can close construction, injection and operation observations.

## Historical handoff to Lab

The smallest useful experiment should create an isolated DSH home and two overlays with a unique row id or harmless config target, then compare `--dump-config` with boot/runtime observation. It should separately record: (a) source-layer order, (b) effective config replacement, (c) whether `ctx.fs` and FS tools become active, and (d) one operation result. Do not infer runtime activity from the dump alone.
