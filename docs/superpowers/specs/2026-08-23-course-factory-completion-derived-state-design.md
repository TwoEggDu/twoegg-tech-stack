# Course Factory Completion Derived-State 架构设计

## 目标

修复 Article completion commit、push 与 remote verification 已完成后，`status.md`、Course README 和 Article README 仍把 `PRE_COMMIT_RECONCILIATION`、completion candidate 或待 commit / push 当作当前状态的问题。

新模型固定为：

```text
Persisted Checkpoint
+ Git History
+ Remote Refs
= Resolved Runtime Completion State
```

Article completion 是由 Git reality 推导出的 runtime state，不是 completion commit 后必须写回 Markdown 的 durable value。正常 transaction 保持 One Article = One Commit = One Push，不再需要额外的 completion reconciliation commit。

## 根因

现有合同已声明 Git history 是 completion authority，并禁止 checkpoint 后继续写 repository，但 durable interface 仍混合了两类语义：

- checkpoint 事实：最后一次允许持久化的 Gate 是 `PRE_COMMIT_RECONCILIATION`；
- runtime 事实：commit、push、remote verify、read-only reconciliation 与 `END_ARTICLE` 是否已经发生。

把 `Current Gate: PRE_COMMIT_RECONCILIATION` 和 `Next Allowed Action: GIT_DIFF_VERIFY / COMMIT / PUSH` 写进 completion commit 后，这些字段必然立即过期。Article 14 的额外 reconciliation commit 只修补了表现，没有消除接口对瞬时字段的依赖，因此 Article 15 再次复现。

## 核心原则

```text
Persisted State != Resolved Runtime State
Completion is Derived from Git Reality
Persist the checkpoint. Derive the completion.
```

状态解析优先级固定为：

```text
Canonical
-> Git Reality
-> Persisted Checkpoint
-> Derived Runtime State
-> Allowed Next Action
```

不新增 `completion-state.json`、`runtime-completion.md`、`post-commit-status.md` 或其他 completion store。Markdown 保存解释规则与 checkpoint；Git history 和 remote refs 提供 completion authority。

## 持久化模型

### Article checkpoint

未来 Article README 使用稳定的 completion interface：

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article NN
Next Transaction Candidate: Article N+1 PRECHECK / NOT_STARTED
```

不得把以下内容作为 checkpoint 后仍实时有效的 Current State：

```text
Current Gate: PRE_COMMIT_RECONCILIATION
Next Allowed Action: GIT_DIFF_VERIFY / COMMIT / PUSH / REMOTE_VERIFY
completion commit pending
```

历史 trace、Gate result 与 attempt record 可以保留，但必须位于明确的 Historical Transaction Record 语境；resolver 不把历史段落当作 current pointer。

### course-run-state.md

保持 `schema_version: 4`，不新增 post-commit event 字段。职责收窄为：

- current / next transaction pointer candidate；
- persisted checkpoint；
- active worker；
- continuous policy；
- blocker 与 stop policy；
- next transaction candidate。

`last_worker_result_semantics: LAST_PERSISTED_PRE_COMMIT_RESULT` 保持不变。commit、push、remote verify 与 `END_ARTICLE` 不进入 `last_worker_result`。

`current_article = N+1` 与 `current_gate = PRECHECK` 只表示 next transaction candidate。启动权限必须由 `ResolveArticleCompletion(N) == END_ARTICLE` 与 continuous / human policy 共同产生。

### status.md 与 Course README

两者是稳定事实台账和课程 baseline，不是 Git pipeline dashboard。可以保存 Lifecycle checkpoint、expected completion identity、resolution rule、published artifact、next transaction candidate，以及明确标注为 retrospective 的一次性 audit observation。

待 commit、待 push、`GIT_DIFF_VERIFY NEXT` 等瞬时动作不能作为 current durable state。Resume 不能依据这些 prose 直接推进 Gate。

## Deterministic Completion Resolver

`ResolveArticleCompletion(N)` 是只读 runtime algorithm，不修改 repository。

### 输入

- canonical Article 定义、依赖与 required Lab；
- persisted `last_published_article` 与 `current_article` pointer；
- 每篇 canonical Article 的 persisted lifecycle/checkpoint，以及 expected completion message；
- workspace、published content、navigation、canonical / index 与 state checkpoint；
- local `main`、`origin/main`、live `refs/heads/main`、remote query 与 freshly materialized `FETCH_HEAD`；
- main history、commit tree 与 worktree status。

### 算法

1. 先解析 Article N：N 必须来自 persisted `last_published_article`；若该字段不存在，才从 canonical Articles 中选择携带 persisted `Lifecycle Checkpoint = PUBLISHED` 的最新 Article。两者同时存在但不一致时，返回 `INCOMPLETE / REPOSITORY_CONFLICT / AMBIGUOUS_COMPLETION_IDENTITY`。
2. 交叉核对 persisted `current_article = N+1`、Article N 的 published path / required Lab，以及 expected completion message 必须精确为 `Publish Agent Engineering Article N`；不得先解析 N+1，pointer candidate 不改变 N。
3. 在任何 history traversal 之前查询 live `refs/heads/main`，再 fetch/materialize remote main 为 `FETCH_HEAD`，并核对 query SHA 与 `FETCH_HEAD` SHA。query、fetch 或 materialization 任一不可达时，fail closed 为 `INCOMPLETE / NEEDS_REMOTE_VERIFY`；SHA 不一致时返回 `INCOMPLETE / REPOSITORY_CONFLICT / REMOTE_MISMATCH`，不得退化为 local-only traversal。
4. 以 local `main`、`origin/main` 与 freshly materialized live `FETCH_HEAD` 为 tips，构造 reachable commit 的去重 union；只在该 union 中查找 Article N 的 exact subject `Publish Agent Engineering Article N`。
5. 零匹配返回 `INCOMPLETE / NEEDS_COMMIT`；多匹配返回 `INCOMPLETE / REPOSITORY_CONFLICT / AMBIGUOUS_COMPLETION_IDENTITY`。
6. 验证 commit scope 仅包含当前 Article workspace、Published Content / assets、required Lab、允许的前篇 navigation、series index、canonical series plan、Article / Course / status / run-state checkpoint 与 final trace。
7. 出现 Article N+1 workspace/content、未来 Lab、无关 docs、theme、CI 或其他 transaction 资产时，返回 `INCOMPLETE / REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE`。
8. completion commit 不是 local `HEAD` 的祖先时，返回 `INCOMPLETE / NEEDS_LOCAL_COMPLETION`。
9. local 包含但 `origin/main` 不包含时，返回 `INCOMPLETE / NEEDS_PUSH`。
10. live `FETCH_HEAD` 不包含时，返回 `INCOMPLETE / NEEDS_REMOTE_VERIFY`。
11. 当前 `HEAD != origin/main` 或 origin 与 live ref 不相等时，返回 `INCOMPLETE / REPOSITORY_CONFLICT / REMOTE_MISMATCH`。
12. 验证 branch=`main`、worktree 无冲突、checkpoint 与 artifacts 一致，并完成 Post-Commit Reconciliation Read-Only。
13. 全部通过时推导：

```text
resolved_lifecycle = PUBLISHED
resolved_completion = END_ARTICLE
resolved_last_published_article = N
completion_commit = matched commit
repository_write_required = false
```

completion commit 只需要被三个当前 main ref 包含；三个 ref 必须彼此相等，但不要求它们永远等于该 completion SHA。这样后续 Factory hotfix、Part Audit 或 Article N+1 commit 不会让已完成的 Article 退化为 incomplete。

### 输出与动作

| Resolver result | Allowed action | Forbidden action |
|---|---|---|
| `INCOMPLETE / NEEDS_COMMIT` | 当前 Article Git completion chain | Article N+1 PRECHECK |
| `INCOMPLETE / NEEDS_PUSH` | verify local commit 后 push main | END_ARTICLE、Article N+1 |
| `INCOMPLETE / NEEDS_REMOTE_VERIFY` | remote verify / reconcile | END_ARTICLE、Article N+1 |
| `INCOMPLETE / REPOSITORY_CONFLICT` | pause and report blocker | 自动修复、下一 Article |
| `END_ARTICLE` | 按 human / continuous policy 考虑 next PRECHECK | post-commit metadata write |

## Factory 集成

### Resume

```text
Repository Reconciliation
-> Resolve N = last_published_article (or latest persisted PUBLISHED Article)
-> derive actual Factory state
-> compare with pointer candidate
-> continue / push / verify / pause
```

README/status 中的历史或 stale prose 不具备 execution authority。

### PRECHECK

Article N+1 PRECHECK 在允许 Kickoff 前必须确认 `ResolveArticleCompletion(N) == END_ARTICLE`。pointer candidate 本身不授予 PRECHECK 或 Kickoff 权限。

### Continuous Run

只在 resolver 得出 `END_ARTICLE N` 后评估 `enabled`、`stop_after_article`、`forbidden_articles` 与其他 stop policy。Markdown 中出现 `END_ARTICLE` 文案不能触发自动继续。

### Part Audit

Part Audit 前对该 Part 每篇 required Article 执行 resolver，并确认全部为 `END_ARTICLE`。只检查 Lifecycle=`PUBLISHED` 或最后一篇 Article 不足以获得 Audit authority。

### Production Workflow 与 Publisher

Workflow 明确区分 checkpoint write boundary 与 runtime resolution。Publisher 只返回 future-safe readiness candidate；不得返回 current commit/push status，不创建 SHA 回写要求，也不宣布 `END_ARTICLE`。

## Article 15 一次性迁移

只修改 completion metadata：

- Article 15 README；
- `status.md`；
- Course README；
- `course-run-state.md` 的解释性语义或 checkpoint hint（仅在必要时）。

允许记录 retrospective observation：Article 15 completion commit 为 `0c9465ca55e095bb1d78e71016b9c6ba357c7ac6`，当前 resolver observation 为 `END_ARTICLE`。该记录不是 future completion authority，也不是未来 Article 必须写回的字段。

Article 15 Published Content knowledge body、Research、Evidence、Outline、Draft、Review、Final score 与 subagent trace 不修改。Article 14/15 的真实历史记录不删除。

## 静态回归

在 `course-factory.md` 加入 A—E：

- A：Article 16 checkpoint 存在但无 exact publish commit，结果 `INCOMPLETE / NEEDS_COMMIT`，禁止 Article 17 PRECHECK。
- B：与 A 使用完全相同的 checkpoint；commit/scope/refs/reconciliation 全部通过，结果 `END_ARTICLE`，repository write=`ZERO`。
- C：publish commit 仅 local 存在，结果 `INCOMPLETE / NEEDS_PUSH`，禁止下一 Article。
- D：message 和 refs 通过但 scope 含 N+1 assets，结果 `REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE`。
- E：Historical section 仍记 PRE_COMMIT，但 Git reality 完成，resolver 忽略其 current authority；若 stale wording 位于 current interface，则 reconciliation 失败。

A 与 B 必须共享同一 checkpoint 内容，证明同一 Markdown 能在不修改文件的情况下于 commit 前后推导出不同 runtime state。

## 实施范围

只修改：

- `docs/agent-engineering-course/course-factory.md`
- `docs/agent-engineering-course/course-run-state.md`
- `docs/agent-engineering-course/status.md`
- `docs/agent-engineering-course/README.md`
- `docs/agent-engineering-course/production-workflow.md`
- `docs/agent-engineering-course/subagent-contracts.md`
- `docs/agent-engineering-course/templates/article-workspace-template.md`
- `docs/agent-engineering-course/articles/15-session-long-term-project-memory/README.md`
- 本设计规格与后续实施计划

不修改 Article 16、Lab、Article 15 semantic artifacts、theme、CI 或其他无关文档。

## 验证

1. 静态核验 A—E 的 input、expected result 与 forbidden action。
2. 用真实 Git history 验证 Article 14/15 completion identity 唯一、scope 合法、completion commits 被三个 main ref 包含且当前 refs 相等。
3. 扫描 current interface，确认 PRE_COMMIT / pending commit / pending push 不再冒充 current state。
4. 验证未来模板以相同 checkpoint 支持 pre-commit=`INCOMPLETE`、post-push=`END_ARTICLE`。
5. 运行 `hugo --gc --minify`，要求 exit 0、0 ERROR、0 WARNING。
6. 检查 Article 15 route、Article 14 -> 15、Course index -> 15；确认 Article 16 route/workspace/content 不存在。
7. 执行完整 diff audit，确认白名单与 Article 15 semantic body unchanged。

## Git 边界

只显式 stage 白名单文件，创建一个 `Derive Article completion from Git history` commit，只 push 一次 `origin main`，随后验证 current `HEAD == origin/main == live refs/heads/main`。

## 非目标与停止线

- 不启动 Article 16 PRECHECK 或 `ARTICLE_KICKOFF`；
- 不创建 Article 16 workspace、Research、Evidence、Draft、Review 或 Published Content；
- 不把 14 -> 15 Canary 判为 PASS；
- 不新增 completion database；
- 不升 schema_version；
- 不生产 Article；
- hotfix 报告后立即停止。

最终给出 `Completion Architecture Hotfix: PASS / FAIL` 作为 Canary/架构 hotfix 的 outcome verdict；该 verdict 不替代、也不限制本计划后文及原始用户请求要求的完整 `FACTORY COMPLETION DERIVED-STATE HOTFIX` 最终报告模板。PASS 时，下一允许的人类动作是显式启动 `16 -> 17 -> Part III Audit`，但本次任务不得执行。
