# Course Factory Completion Derived-State Hotfix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace stale post-commit Markdown state with a deterministic, read-only Git-derived Article completion resolver, migrate Article 15 completion metadata once, and publish the Factory hotfix without starting Article 16.

**Architecture:** Markdown persists only the last writable checkpoint, expected completion identity, resolution rule, and next-transaction candidate. Resume, PRECHECK, Continuous Run, and Part Audit derive runtime completion from the unique exact publish commit, valid commit scope, commit containment in aligned local/origin/live main refs, and read-only reconciliation. No runtime completion store or post-commit metadata write is introduced.

**Tech Stack:** Hugo 0.157.0, Markdown contracts, Git, Windows PowerShell.

**Spec:** `docs/superpowers/specs/2026-08-23-course-factory-completion-derived-state-design.md`

## Global Constraints

- Work on existing `main`; do not create or switch to a production branch.
- Do not run Article 16 PRECHECK or `ARTICLE_KICKOFF`; do not create Article 16 workspace, Research, Evidence, Outline, Draft, Review, Lab, or Published Content.
- Do not modify Article 15 Published Content knowledge body, `research.md`, `evidence.md`, `outline.md`, `draft.md`, `review.md`, final score, or `subagent-trace.md`.
- Preserve `schema_version: 4` and `last_worker_result_semantics: LAST_PERSISTED_PRE_COMMIT_RESULT`.
- Do not add a completion database, runtime status file, post-commit event log, resolver script, dependency, role, Lab, theme, or CI change.
- Historical PRE_COMMIT / BUILD / candidate records remain intact and are Historical Transaction Records, not current authority.
- Use explicit path staging only; never run `git add .` or `git add -A`.
- User requirements override frequent-commit defaults: create exactly one hotfix commit and push `main` exactly once.
- Commit message: `Derive Article completion from Git history`.
- Use `Completion Architecture Hotfix: PASS / FAIL` only as the Canary/outcome verdict; it does not replace or limit the full `FACTORY COMPLETION DERIVED-STATE HOTFIX` final report template required later in this plan and by the original user request. Do not declare the 14 -> 15 Canary PASS.
- Stop after the final report.

---

## File Map

| File | Responsibility |
|---|---|
| `docs/agent-engineering-course/course-factory.md` | Normative resolver, state priority, Resume/PRECHECK/Continuous/Part Audit integration, A-E regressions |
| `docs/agent-engineering-course/course-run-state.md` | Schema 4 candidate-pointer and checkpoint semantics |
| `docs/agent-engineering-course/status.md` | Stable fact ledger and Article 15 migration |
| `docs/agent-engineering-course/README.md` | Stable course baseline and Article 15 migration |
| `docs/agent-engineering-course/production-workflow.md` | Article checkpoint versus runtime completion boundary |
| `docs/agent-engineering-course/subagent-contracts.md` | Master/Publisher resolver responsibilities |
| `docs/agent-engineering-course/templates/article-workspace-template.md` | Future-safe checkpoint fields |
| `docs/agent-engineering-course/articles/15-session-long-term-project-memory/README.md` | Metadata-only Article 15 migration |
| `docs/superpowers/specs/2026-08-23-course-factory-completion-derived-state-design.md` | Approved design |
| `docs/superpowers/plans/2026-08-23-course-factory-completion-derived-state-hotfix.md` | Implementation and verification plan |

### Task 1: Freeze baseline and establish RED checks

**Files:**
- Read: all File Map paths
- Protect: Article 15 semantic artifacts and Article 16 absence
- Modify: none

**Interfaces:**
- Consumes: approved design and current `main`
- Produces: baseline refs, protected hashes, RED failures, and forbidden-asset invariant

- [ ] **Step 1: Reconcile repository and remote truth**

Run:

```powershell
git branch --show-current
git status --short --untracked-files=all
git log --oneline -20
git rev-parse HEAD
git rev-parse origin/main
git ls-remote origin refs/heads/main
git log main --format='%H%x09%s' --grep='^Publish Agent Engineering Article 14$'
git log main --format='%H%x09%s' --grep='^Publish Agent Engineering Article 15$'
```

Expected: branch `main`; Article 14 unique completion `a53d151ba051403ff5ef369e5c3860a9fbded03d`; Article 15 unique completion and all three refs `0c9465ca55e095bb1d78e71016b9c6ba357c7ac6`. Only the approved spec and plan may be untracked.

- [ ] **Step 2: Capture protected Article 15 hashes**

```powershell
$protectedArticle15 = @(
  'content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/research.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/evidence.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/outline.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/draft.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/review.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/subagent-trace.md'
)
$protectedArticle15 | ForEach-Object { '{0} {1}' -f (git hash-object --no-filters -- $_), $_ }
```

Expected: seven hash/path lines retained outside repository files for Task 6 comparison.

- [ ] **Step 3: Run the current-interface RED assertion**

```powershell
$currentInterfaces = @(
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/README.md',
  'docs/agent-engineering-course/status.md',
  'docs/agent-engineering-course/README.md',
  'docs/agent-engineering-course/course-run-state.md'
)
$violations = Select-String -Path $currentInterfaces -Pattern 'Current Gate:.*PRE_COMMIT|Next Allowed Action:.*(GIT_DIFF_VERIFY|COMMIT|PUSH)|当前working tree是唯一completion commit candidate|下一动作是完成Article 15 Git|下一允许动作：完成Article 15'
if ($violations) { $violations; throw 'RED: transient completion pipeline wording controls current interface' }
```

Expected: FAIL with current Article 15/state metadata matches.

- [ ] **Step 4: Run the resolver-contract RED assertion**

```powershell
$factoryPath = 'docs/agent-engineering-course/course-factory.md'
$markers = @(
  'Persisted State != Resolved Runtime State',
  'ResolveArticleCompletion(N)',
  'AMBIGUOUS_COMPLETION_IDENTITY',
  'INVALID_COMPLETION_SCOPE',
  'NEEDS_PUSH',
  'Regression Scenario A',
  'Regression Scenario B',
  'Regression Scenario C',
  'Regression Scenario D',
  'Regression Scenario E'
)
$missing = $markers | Where-Object { -not (Select-String -LiteralPath $factoryPath -SimpleMatch $_ -Quiet) }
if ($missing) { $missing; throw 'RED: resolver contract incomplete' }
```

Expected: FAIL with missing resolver/regression markers.

- [ ] **Step 5: Assert Article 16 absence**

```powershell
$workspace16 = @(Get-ChildItem 'docs/agent-engineering-course/articles' -Directory | Where-Object Name -Like '16-*')
$content16 = @(Get-ChildItem 'content/ai-empowerment' -File | Where-Object Name -Like 'agent-engineering-16-*.md')
if ($workspace16.Count -ne 0 -or $content16.Count -ne 0) { throw 'Article 16 assets exist' }
```

Expected: PASS.

### Task 2: Define the normative resolver and A-E regressions

**Files:**
- Modify: `docs/agent-engineering-course/course-factory.md:108-129,205-229,359-376,416-424,555`
- Test: Task 1 resolver assertion

**Interfaces:**
- Consumes: canonical identity, Git refs/history, persisted checkpoint, stop policies
- Produces: `ResolveArticleCompletion(N)` returning `END_ARTICLE` or `INCOMPLETE / <reason>`

- [ ] **Step 1: Add the state distinction and priority**

Add:

```text
Persisted State != Resolved Runtime State.
Completion is derived from Git reality.

Canonical
-> Git Reality
-> Persisted Checkpoint
-> Derived Runtime State
-> Allowed Next Action
```

Markdown persists checkpoint facts, expected completion identity, resolution rule, and next candidate; it does not persist live commit/push/remote progress.

- [ ] **Step 2: Add the deterministic resolver**

Specify this exact order; remote materialization is mandatory before history traversal:

```text
1. Resolve N from persisted last_published_article; if absent, use the latest canonical Article with persisted PUBLISHED checkpoint.
   If both exist and disagree -> INCOMPLETE / REPOSITORY_CONFLICT / AMBIGUOUS_COMPLETION_IDENTITY.
2. Cross-check current_article = N+1 and expected message "Publish Agent Engineering Article N".
   Never resolve N+1 first; pointer candidate is not completion authority.
3. Query live refs/heads/main, fetch/materialize remote main as FETCH_HEAD, and compare query SHA with FETCH_HEAD SHA.
   Query/fetch/materialization failure -> INCOMPLETE / NEEDS_REMOTE_VERIFY.
   SHA mismatch -> INCOMPLETE / REPOSITORY_CONFLICT / REMOTE_MISMATCH.
4. Build the deduplicated reachable union from local main, origin/main, and freshly materialized FETCH_HEAD.
5. Find exactly one commit in that union with subject "Publish Agent Engineering Article N".
6. Zero -> INCOMPLETE / NEEDS_COMMIT.
7. Multiple -> INCOMPLETE / REPOSITORY_CONFLICT / AMBIGUOUS_COMPLETION_IDENTITY.
8. Validate current-transaction-only scope.
9. Future Article/unrelated scope -> INCOMPLETE / REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE.
10. Commit must be contained by local HEAD, origin/main, and live FETCH_HEAD.
11. Local only -> INCOMPLETE / NEEDS_PUSH.
12. Remote containment missing or untraversable -> INCOMPLETE / NEEDS_REMOTE_VERIFY.
13. Current HEAD, origin/main, and live main must equal each other.
14. Branch/conflict/artifact/checkpoint/read-only reconciliation pass.
15. All pass -> END_ARTICLE; repository write required = false.
```

State that refs contain the completion commit and equal each other; later valid commits need not equal the older completion SHA.

- [ ] **Step 3: Integrate Resume, PRECHECK, Continuous Run, and Part Audit**

Freeze:

```text
Resume:
Repository Reconciliation -> resolver -> derived Factory state
-> compare candidate pointer -> commit/push/verify/pause/next consideration.

PRECHECK:
Article N+1 may pass only if ResolveArticleCompletion(N) == END_ARTICLE.

Continuous Run:
Evaluate policy only after resolver END_ARTICLE; Markdown END wording has no authority.

Part Audit:
Every required Article in the Part must resolve END_ARTICLE.
```

Preserve inclusive `stop_after_article`, `forbidden_articles`, and STOP POLICY WINS.

- [ ] **Step 4: Add Regression Scenarios A-E**

Add named scenarios with exact outcomes:

```text
A: checkpoint, no publish commit
   -> INCOMPLETE / NEEDS_COMMIT; N+1 forbidden.
B: byte-identical checkpoint, valid fully pushed commit
   -> END_ARTICLE; repository writes = ZERO.
C: valid local-only commit
   -> INCOMPLETE / NEEDS_PUSH; N+1 forbidden.
D: exact message/aligned refs but scope contains N+1
   -> REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE.
E: Historical PRE_COMMIT wording plus completed Git reality
   -> historical prose ignored; END_ARTICLE.
   Unlabelled stale current-interface prose fails reconciliation.
```

A and B must explicitly share the same persisted checkpoint.

- [ ] **Step 5: Rerun Task 1 Step 4**

Expected: PASS with no missing markers. Then run:

```powershell
rg -n 'Regression Scenario [A-E]|NEEDS_COMMIT|NEEDS_PUSH|INVALID_COMPLETION_SCOPE|repository writes = ZERO|same persisted checkpoint|同一.*checkpoint' docs/agent-engineering-course/course-factory.md
```

Expected: all five scenarios and A/B identity visible. Do not commit.

### Task 3: Propagate resolver semantics

**Files:**
- Modify: `docs/agent-engineering-course/production-workflow.md:35-45,130`
- Modify: `docs/agent-engineering-course/subagent-contracts.md:161-173,520-589`
- Modify: `docs/agent-engineering-course/course-run-state.md:1-134`
- Modify: `docs/agent-engineering-course/templates/article-workspace-template.md:14-31`

**Interfaces:**
- Consumes: `ResolveArticleCompletion(N)`
- Produces: stable workflow, role, pointer, and template contracts

- [ ] **Step 1: Update production workflow**

Add:

```text
Persisted Checkpoint Gate = PRE_COMMIT_RECONCILIATION.
Current Runtime Completion = ResolveArticleCompletion(N).
The same checkpoint resolves INCOMPLETE before commit and END_ARTICLE
after valid commit/push/remote reconciliation. No write bridges them.
```

Next-Article authority consumes resolver output, not Lifecycle or README prose.

- [ ] **Step 2: Update Master and Publisher contracts**

Master must persist only the checkpoint, run the read-only resolver, route exact incomplete reasons, and never write `END_ARTICLE` back.

Publisher templates use two phases. At initialization, before the persistence cut:

```text
Lifecycle Candidate: NOT_REACHED
Persisted Checkpoint: ABSENT
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article NN
Next Transaction Candidate: ABSENT
```

At the persistence cut, replace the complete six-field placeholder interface with the actual worker checkpoint and stable expected identity; do not replace only the `ABSENT` checkpoint. Retire any transient `Current Gate` and `Next Allowed Action` as historical observations (for example `Historical Gate` / `Historical Next Allowed Action`); they must not remain current authority. The resolver may later derive `Lifecycle = PUBLISHED`, `Completion = END_ARTICLE`, and `Article N+1 PRECHECK` only from the persisted interface plus Git/remote reality. Publisher cannot claim current commit/push status or `END_ARTICLE`.

- [ ] **Step 3: Narrow run-state without schema change**

Keep schema and `last_worker_result`. State:

```text
current_article/current_gate = candidate pointer
last_worker_result = last persisted pre-commit result
last_successful_commit = checkpoint hint, not authority
commit/push/remote/END_ARTICLE = runtime resolver facts
start authority = candidate pointer + resolver END_ARTICLE + policy
```

Remove the current-working-tree completion-candidate sentence. The migration may set `last_successful_commit` to Article 15 commit `0c9465ca55e095bb1d78e71016b9c6ba357c7ac6` as a hint only.

- [ ] **Step 4: Replace template completion fields**

Use a two-phase template:

Initialization before the persistence cut:

```markdown
- Lifecycle Candidate: `NOT_REACHED`
- Persisted Checkpoint: `ABSENT`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article NN`
- Next Transaction Candidate: `ABSENT`
```

At the persistence cut, replace the complete six-field placeholder interface with the actual worker checkpoint and stable expected identity; do not replace only the `ABSENT` checkpoint. Retire transient `Current Gate` and `Next Allowed Action` into explicitly historical fields; do not preserve them as current state. The persisted interface is later resolved against Git/remote reality, so it can remain incomplete before commit and resolve to `END_ARTICLE` only after valid commit/push/remote reconciliation.

- [ ] **Step 5: Verify propagation**

```powershell
$required = @{
  'docs/agent-engineering-course/production-workflow.md' = @('Persisted Checkpoint','ResolveArticleCompletion')
  'docs/agent-engineering-course/subagent-contracts.md' = @('DERIVED_FROM_GIT_HISTORY','GIT_HISTORY + REMOTE_REFS')
  'docs/agent-engineering-course/course-run-state.md' = @('LAST_PERSISTED_PRE_COMMIT_RESULT','pointer candidate','ResolveArticleCompletion')
  'docs/agent-engineering-course/templates/article-workspace-template.md' = @('Persisted Checkpoint','DERIVED_FROM_GIT_HISTORY','same persisted checkpoint')
}
$missing = foreach ($path in $required.Keys) {
  foreach ($marker in $required[$path]) {
    if (-not (Select-String -LiteralPath $path -SimpleMatch $marker -Quiet)) { "$path :: $marker" }
  }
}
if ($missing) { $missing; throw 'Resolver propagation incomplete' }
```

Expected: PASS. Do not commit.

### Task 4: Migrate Article 15 completion metadata once

**Files:**
- Modify: `docs/agent-engineering-course/articles/15-session-long-term-project-memory/README.md:7-20,34-80`
- Modify: `docs/agent-engineering-course/status.md:5-34,62`
- Modify: `docs/agent-engineering-course/README.md:8-9,63-66,110`
- Modify: `docs/agent-engineering-course/course-run-state.md:11-62`
- Protect: Task 1 semantic files

**Interfaces:**
- Consumes: stable fields and Article 15 commit identity
- Produces: retrospective explanation that is not completion authority

- [ ] **Step 1: Migrate Article 15 README top metadata**

Use:

```markdown
- Lifecycle Checkpoint: `PUBLISHED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 15`
- Next Transaction Candidate: `Article 16 PRECHECK / NOT_STARTED / FORBIDDEN CURRENT RUN`
- Retrospective Resolver Observation (2026-08-23): `END_ARTICLE`
- Completion Commit Observation: `0c9465ca55e095bb1d78e71016b9c6ba357c7ac6`
```

Explain that retrospective observation is read-only migration evidence, not future authority or a required post-commit write. Label existing Gate records Historical without rewriting their chronology.

- [ ] **Step 2: Migrate status and Course README**

Both must state stable facts:

```text
Article 15 Lifecycle checkpoint = PUBLISHED
Completion resolution = DERIVED_FROM_GIT_HISTORY
Expected message = Publish Agent Engineering Article 15
Retrospective observation = END_ARTICLE
Completion commit observation = 0c9465ca...
Article 16 = pointer candidate / NOT_STARTED / FORBIDDEN CURRENT RUN
Next durable policy = wait for explicit human instruction
```

The Article 15 status table row cannot present PRE_COMMIT as current lifecycle. Course README changes “Article 00—14” Published Content to “Article 00—15” and removes pending Article 15 Git-chain prose.

- [ ] **Step 3: Migrate run-state prose and hint**

Keep:

```yaml
schema_version: 4
current_article: "16"
current_gate: PRECHECK
last_published_article: "15"
last_worker_result_semantics: LAST_PERSISTED_PRE_COMMIT_RESULT
continuous_run:
  enabled: false
  stop_after_article: "15"
  forbidden_articles:
    - "16"
```

Set `last_successful_commit` to Article 15 completion SHA as hint only. State that resolver interprets the checkpoint and Article 16 remains forbidden.

- [ ] **Step 4: Rerun current-interface assertion and protected hashes**

Rerun Task 1 Steps 2 and 3.

Expected: no transient current-interface violations; all seven protected hashes unchanged. Do not commit.

### Task 5: Verify real Git resolution and static regressions

**Files:**
- Read: history and modified contracts
- Modify: none

**Interfaces:**
- Consumes: implemented resolver
- Produces: unique identity, containment, scope, and A-E evidence

- [ ] **Step 1: Verify exact unique identities**

```powershell
$liveQuery = @(git ls-remote origin refs/heads/main 2>&1)
if ($LASTEXITCODE -ne 0 -or $liveQuery.Count -eq 0) { throw 'NEEDS_REMOTE_VERIFY' }
$liveSha = (($liveQuery | Select-Object -First 1) -split '\s+')[0]
git fetch --no-tags origin refs/heads/main:refs/remotes/origin/main
if ($LASTEXITCODE -ne 0) { throw 'NEEDS_REMOTE_VERIFY' }
$originSha = (git rev-parse origin/main).Trim()
$fetchSha = (git rev-parse FETCH_HEAD).Trim()
if (-not $originSha -or -not $fetchSha -or $originSha -ne $fetchSha -or $fetchSha -ne $liveSha) { throw 'REMOTE_MISMATCH' }
$historyTips = @('main', 'origin/main', 'FETCH_HEAD')
$historyUnion = @($historyTips | ForEach-Object { git log $_ --format='%H%x09%s' }) | Sort-Object -Unique
foreach ($article in 14, 15) {
  $subject = 'Publish Agent Engineering Article {0:D2}' -f $article
  $matches = @($historyUnion | Where-Object { $_ -match ([char]9 + [regex]::Escape($subject) + '$') })
  if ($matches.Count -ne 1) { throw "Article $article completion count = $($matches.Count)" }
  $matches
}

Expected: one match per Article.

- [ ] **Step 2: Verify ref equality and containment**

```powershell
$headSha = git rev-parse HEAD
$originSha = git rev-parse origin/main
$liveSha = ((git ls-remote origin refs/heads/main) -split '\s+')[0]
if ($headSha -ne $originSha -or $originSha -ne $liveSha) { throw 'REMOTE_MISMATCH' }
foreach ($commit in @('a53d151ba051403ff5ef369e5c3860a9fbded03d','0c9465ca55e095bb1d78e71016b9c6ba357c7ac6')) {
  git merge-base --is-ancestor $commit HEAD
  if ($LASTEXITCODE -ne 0) { throw "$commit absent from local main" }
  git merge-base --is-ancestor $commit origin/main
  if ($LASTEXITCODE -ne 0) { throw "$commit absent from remote main" }
}
```

Expected: PASS before edits and after Task 7 push. During the intentional dirty hotfix worktree, full resolver completion is not claimed.

- [ ] **Step 3: Verify Article 14/15 scopes contain no future assets**

```powershell
foreach ($pair in @(
  @{ Article = 14; Commit = 'a53d151ba051403ff5ef369e5c3860a9fbded03d'; Future = '15' },
  @{ Article = 15; Commit = '0c9465ca55e095bb1d78e71016b9c6ba357c7ac6'; Future = '16' }
)) {
  $files = @(git diff-tree --no-commit-id --name-only -r $pair.Commit)
  $futureHits = @($files | Where-Object {
    $_ -match "^docs/agent-engineering-course/articles/$($pair.Future)-" -or
    $_ -match "^content/ai-empowerment/agent-engineering-$($pair.Future)-" -or
    $_ -match "^static/images/agent-engineering/$($pair.Future)-"
  })
  if ($futureHits) { $futureHits; throw "INVALID_COMPLETION_SCOPE Article $($pair.Article)" }
  $files
}
```

Expected: no future hits. Review displayed paths against resolver allowlist.

- [ ] **Step 4: Verify A-E and absence of new stores**

```powershell
$factory = Get-Content -Raw 'docs/agent-engineering-course/course-factory.md'
$assertions = @('Regression Scenario A','Regression Scenario B','Regression Scenario C','Regression Scenario D','Regression Scenario E','NEEDS_COMMIT','NEEDS_PUSH','INVALID_COMPLETION_SCOPE','Historical Transaction Record')
$missing = $assertions | Where-Object { -not $factory.Contains($_) }
if ($missing) { $missing; throw 'A-E matrix incomplete' }
$stores = @('completion-state.json','runtime-completion.md','post-commit-status.md')
$hits = foreach ($name in $stores) { Get-ChildItem -Recurse -File -Filter $name -ErrorAction SilentlyContinue }
if ($hits) { $hits.FullName; throw 'Forbidden completion store exists' }
```

Expected: PASS.

### Task 6: Run Hugo, navigation, protected-content, and diff gates

**Files:**
- Read: worktree and ignored `public/`
- Modify: none

**Interfaces:**
- Consumes: complete hotfix diff
- Produces: final pre-commit gate evidence

- [ ] **Step 1: Run Hugo**

```powershell
hugo --gc --minify
```

Expected: exit 0, 0 ERROR, 0 WARNING.

- [ ] **Step 2: Verify Article 15 route and navigation**

```powershell
$article14Route = 'public/ai-empowerment/agent-engineering-14-working-memory-investigation-state/index.html'
$article15Route = 'public/ai-empowerment/agent-engineering-15-session-long-term-project-memory/index.html'
$courseIndex = 'public/ai-empowerment/agent-engineering/index.html'
foreach ($path in @($article14Route,$article15Route,$courseIndex)) {
  if (-not (Test-Path -LiteralPath $path)) { throw "Missing route: $path" }
}
if (-not (Select-String -LiteralPath $article14Route -SimpleMatch 'agent-engineering-15-session-long-term-project-memory' -Quiet)) { throw 'Article 14 -> 15 missing' }
if (-not (Select-String -LiteralPath $courseIndex -SimpleMatch 'agent-engineering-15-session-long-term-project-memory' -Quiet)) { throw 'Index -> Article 15 missing' }
```

Expected: PASS.

- [ ] **Step 3: Verify Article 16 remains absent**

```powershell
$forbidden = @(
  Get-ChildItem 'docs/agent-engineering-course/articles' -Directory | Where-Object Name -Like '16-*'
  Get-ChildItem 'content/ai-empowerment' -File | Where-Object Name -Like 'agent-engineering-16-*.md'
  Get-ChildItem 'public/ai-empowerment' -Directory -ErrorAction SilentlyContinue | Where-Object Name -Like 'agent-engineering-16-*'
)
if ($forbidden.Count -ne 0) { $forbidden.FullName; throw 'Article 16 asset or route exists' }
```

Expected: PASS.

- [ ] **Step 4: Recheck protected hashes and full diff**

Repeat Task 1 Step 2 and compare all seven hashes. Then run:

```powershell
git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
```

Expected: exactly ten File Map paths, no protected/Article16/Lab/theme/CI changes, diff-check exit 0.

### Task 7: Commit once, push once, reconcile, and stop

**Files:**
- Stage: exactly ten File Map paths
- Commit: once
- Push: once to `origin main`

**Interfaces:**
- Consumes: all prior PASS gates
- Produces: remotely verified hotfix and final report

- [ ] **Step 1: Explicitly stage ten paths**

```powershell
$stagePaths = @(
  'docs/agent-engineering-course/course-factory.md',
  'docs/agent-engineering-course/course-run-state.md',
  'docs/agent-engineering-course/status.md',
  'docs/agent-engineering-course/README.md',
  'docs/agent-engineering-course/production-workflow.md',
  'docs/agent-engineering-course/subagent-contracts.md',
  'docs/agent-engineering-course/templates/article-workspace-template.md',
  'docs/agent-engineering-course/articles/15-session-long-term-project-memory/README.md',
  'docs/superpowers/specs/2026-08-23-course-factory-completion-derived-state-design.md',
  'docs/superpowers/plans/2026-08-23-course-factory-completion-derived-state-hotfix.md'
)
git add -- $stagePaths
```

- [ ] **Step 2: Audit staged diff**

```powershell
git diff --cached --stat
git diff --cached
git diff --cached --check
git status --short --untracked-files=all
```

Expected: exactly ten staged paths, no unstaged tracked changes or forbidden files.

- [ ] **Step 3: Commit and verify**

```powershell
git commit -m 'Derive Article completion from Git history'
git status --short --untracked-files=all
git log -1 --format='%H%n%s'
git show --stat --oneline HEAD
git diff HEAD^ HEAD --check
```

Expected: clean worktree, exact message and ten-file scope. Do not amend or create another commit.

- [ ] **Step 4: Push once and verify equality**

```powershell
git push origin main
git fetch origin main
$headSha = git rev-parse HEAD
$originSha = git rev-parse origin/main
$liveSha = ((git ls-remote origin refs/heads/main) -split '\s+')[0]
if ($headSha -ne $originSha -or $originSha -ne $liveSha) { throw 'REMOTE_MISMATCH' }
git merge-base --is-ancestor 0c9465ca55e095bb1d78e71016b9c6ba357c7ac6 HEAD
if ($LASTEXITCODE -ne 0) { throw 'Article 15 missing from local main' }
git merge-base --is-ancestor 0c9465ca55e095bb1d78e71016b9c6ba357c7ac6 origin/main
if ($LASTEXITCODE -ne 0) { throw 'Article 15 missing from remote main' }
git status --short --untracked-files=all
```

Expected: hotfix HEAD equals origin/live, worktree clean, Article 15 completion contained. With identity, scope, artifacts, and read-only reconciliation already verified, Article 15 resolves `END_ARTICLE`.

- [ ] **Step 5: Report using the required template and stop**

Use this complete report structure. Fill every value from fresh Task 1-7 evidence; do not invent or omit a field:

```text
FACTORY COMPLETION DERIVED-STATE HOTFIX

STATUS:
PASS / BLOCKED

BASELINE
- HEAD:
- Article 15 completion commit:
- origin/main:
- live remote:
- Article 16 started:

ROOT CAUSE
- why Article 14 needed reconciliation:
- why Article 15 repeated it:
- old state model:

NEW MODEL
- Persisted Checkpoint:
- Completion Authority:
- Completion Resolver:
- Derived Runtime State:
- Next-Article authority:

RESOLVER RULES
- commit identity:
- commit scope:
- local:
- origin:
- live remote:
- post-commit read-only reconciliation:

REGRESSION SCENARIOS
A. pre-commit:
B. fully pushed:
C. local-only commit:
D. invalid scope:
E. stale historical wording:

ARTICLE 15 MIGRATION
- semantic body changed:
- completion resolution:
- completion commit:
- status.md:
- Course README:
- Article README:

FUTURE ARTICLE CONTRACT
- post-commit write required:
- second reconciliation commit required:
- same checkpoint works pre/post commit:
- One Article = One Commit preserved:

ARTICLE 16
- PRECHECK executed:
- ARTICLE_KICKOFF:
- workspace:
- Published Content:

VERIFICATION
- Hugo:
- git diff --check:
- forbidden assets:

GIT
- hotfix commit:
- push:
- remote equality:

HOTFIX VERDICT:
SAFE_FOR_PART_CONTINUOUS_PRODUCTION
or
NOT_READY

NEXT ALLOWED HUMAN ACTION:
Explicitly start 16 -> 17 -> Part III Audit

DO NOT START ARTICLE 16
```

Then stop.
