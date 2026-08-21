# Agent Engineering Course Factory Run State

> This file is the Factory execution pointer, not a second course database. Article facts remain in [status.md](status.md); execution rules remain in [course-factory.md](course-factory.md).

```yaml
schema_version: 3
factory_mode: SEQUENTIAL_SUBAGENT_FACTORY
production_branch: main
checkpoint_sha_source: GIT_HISTORY
factory_status: READY
current_article: "13"
current_gate: PRECHECK
last_published_article: "12"
active_worker: NONE
active_worker_execution_id: NONE
active_worker_record_ref: NONE
last_worker_result:
  role: MASTER_ORCHESTRATOR
  article: "12"
  gate: PRE_COMMIT_RECONCILIATION
  execution_type: MASTER_DETERMINISTIC
  execution_id: /root
  result_ref: docs/agent-engineering-course/articles/12-context-engineering/subagent-trace.md#wr-master-article-12-pre-commit-reconciliation-retry-1-20260821t204153
  status: PASS
  gate_completed: true
  artifact_verified: true
  validation_status: PASS
  next_allowed_gate: GIT_DIFF_VERIFY
  blocker: NONE
last_worker_result_error: NONE
review_cycle: 0
active_blocker: NONE
stop_reason: NONE
human_decision_required: false
last_successful_commit: e7f88c03151707d00b7d307645e99cf4710f3363
next_action: START_ARTICLE_13_PRECHECK
last_updated: "2026-08-21T20:41:53+08:00"
```

## Field rules

- `factory_status` 只使用 `READY / RUNNING / PAUSED / BLOCKED / COMPLETE`。
- `production_branch` 固定为 `main`。每次 PRECHECK 与 Resume 必须用 `git branch --show-current` 实时验证；任何 role 都没有 branch creation authority。wrong branch 只允许 Master 在 clean worktree、不会覆盖用户修改时安全 `git switch main` 并 `git pull --ff-only origin main`，否则 `PAUSED / REPOSITORY_CONFLICT`。
- `checkpoint_sha_source` 固定为 `GIT_HISTORY`：当前 Article completion SHA 由 `Publish Agent Engineering Article NN` commit、files scope 与 main history共同确定，不从聊天或 checkpoint 后的 Markdown 回写确定。
- `current_article` 是下一或当前 transaction pointer，不表示该 Article 已经启动；是否启动必须结合 `factory_status`、`current_gate` 与 [status.md](status.md) 判断。
- PRECHECK `PASS` 后必须执行显式 `ARTICLE_KICKOFF`，Factory 才能进入 `RUNNING` 并创建当前 workspace；pointer 指向 Article 不等于 Kickoff 已发生。
- `PRE_COMMIT_RECONCILIATION` 必须把最终 checkpoint 内容写成可恢复状态：Article N Lifecycle=`PUBLISHED`、`last_published_article = N`、`current_article = N+1`、`current_gate = PRECHECK`、`factory_status = READY`、`active_worker = NONE`、`next_action = START_ARTICLE_N+1_PRECHECK`。这些字段在 working tree 中只是 commit candidate；只有对应 completion commit 已进入 main history 后才成为下一 transaction 的 durable pointer，且仍不等于 `ARTICLE_KICKOFF`。
- `active_worker` 只使用 [subagent-contracts.md](subagent-contracts.md) 中的八种 role 或 `NONE`。
- `active_worker_execution_id` 与 `active_worker_record_ref` 在 worker start 时由 Master 写入。record ref 必须指向当前 Article `subagent-trace.md` 的 stable Worker Result Record，或 Part / Course Audit Report 中的等价 record；record 同时保存 bounded task brief、execution ID、raw envelope 与 validation result。worker 仍运行时保留 active fields；确认结束后才把 `active_worker` 和两个 active fields 统一清为 `NONE`。
- `last_worker_result` 初始或 legacy-migration 值可以为 `NONE`。只有 Master 收到 schema-valid envelope、写入 canonical raw record 并完成 validation 后，才能写入以下 durable projection：

  Schema v3 migration keeps the latest checkpoint-contained Article 08 projection at `GIT_DIFF_VERIFY`; the later Article 08 reconciliation record remains historical regression evidence in its trace but is no longer projected as a future-valid post-commit write.

  ```yaml
  last_worker_result:
    role: AUTHOR
    article: "08"
    gate: OUTLINE
    execution_type: REAL_SUBAGENT
    execution_id: /root/example_author_outline
    result_ref: docs/agent-engineering-course/articles/08-agent-loop/subagent-trace.md#wr-example-author-outline
    status: PASS
    gate_completed: true
    artifact_verified: true
    validation_status: PASS
    next_allowed_gate: AUTHOR_DRAFT
    blocker: NONE
  ```

- `role / article / gate / execution_type / status / gate_completed / blocker` 来自 envelope；`execution_id / result_ref / artifact_verified / validation_status` 只能由 Master 从实际 dispatch、canonical raw record 与验证结果写入。`artifact_verified: true` 表示 created 与 modified paths 均真实存在于 actual diff、全部属于该 role 的 `Allowed Writes`，并且没有未声明 delete / rename。
- 结构有效但 artifact、Gate 或 State Machine validation 失败时，Master 可以写 `artifact_verified: false` 或 `validation_status: FAIL`；`next_allowed_gate` 仅在 mapping / transition validation 通过时保留。`status: FAIL / BLOCKED` 的 non-`NONE` Gate 只是 recovery candidate，不改变 `current_gate` 且不触发自动 dispatch。
- worker 没有返回 envelope，或 envelope 的 root / fields / types / assignment 无效时，Master 不得制造 projection，也不得覆盖最近一次 schema-valid `last_worker_result`。Master 必须把 dispatch identity 与 failure 写入 canonical record，并设置 `last_worker_result_error`：

  ```yaml
  last_worker_result_error:
    code: MISSING_OR_INVALID_WORKER_RESULT
    role: AUTHOR
    article: "08"
    gate: OUTLINE
    execution_type: REAL_SUBAGENT
    execution_id: /root/example_author_outline
    result_ref: docs/agent-engineering-course/articles/08-agent-loop/subagent-trace.md#wr-example-author-outline
  ```

  随后设置 `factory_status: PAUSED`、`active_blocker: MISSING_OR_INVALID_WORKER_RESULT`、`stop_reason: HUMAN_DECISION_REQUIRED`。若 runtime 已确认结束，active worker fields 清为 `NONE`；若仍在运行，则保留 active fields，禁止重复 dispatch。
- `last_worker_result.next_allowed_gate` 只保存通过 Master common mapping 与 State Machine validation 的 forward transition 或 recovery candidate。非 terminal `status: PASS` 时不得为 `NONE`；`gate_completed: false` 只允许指向合同冻结的 retry / return Gate。该 pointer 仍不是 Article / Transaction completion，也不能替代 `current_gate`、`factory_status`、canonical raw record、required artifacts 或 Git evidence。未来 Article 以 `PRE_COMMIT_RECONCILIATION` 作为 completion commit 中最后一个可持久化的 result projection；Git Diff Verify、Checkpoint Commit、Commit Verify、Push、Remote Verify 与 Post-Commit Reconciliation results 不写入本文件，它们由 runtime envelope 验证并由 final commit / Git / remote refs 持久化，Resume 时重新执行只读检查。Article 08 的 `GIT_DIFF_VERIFY` projection 是 schema migration 保留的 legacy boundary。
- `review_cycle` 只在一次 `Findings -> Revision -> Recheck` 完成后递增，最大值为 `3`。
- `stop_reason` 只使用 `NONE / BLOCKED_EVIDENCE / FAILED_LAB / FAILED_REVIEW / FAILED_PUBLICATION / HUMAN_DECISION_REQUIRED / REPOSITORY_CONFLICT`。
- Part Auditor 返回 `PART_AUDIT_FINDINGS` 时不得把 role-specific code 直接写入 `stop_reason`；Master 必须唯一映射为 `factory_status: PAUSED`、`active_blocker: PART_AUDIT_FINDINGS`、`stop_reason: HUMAN_DECISION_REQUIRED`、`human_decision_required: true`。只有人类批准 Audit Report 中的 affected Article 与 targeted repair scope 后，Resume 才能选择具体 Article / Gate。
- `last_successful_commit` 是最近一个已知可恢复的 durable checkpoint hint，可以保存 previous verified checkpoint 或 `PENDING_SELF`。它不是 blind checkout target、当前 `HEAD` 的绝对真相或 Resume 的唯一依据；checkpoint commit 不得自引用，也不得为了同步新 SHA 制造第二个 reconciliation commit。
- Resume 必须联合检查本文件、`status.md`、current Article workspace、Published Content、`git status`、Git `HEAD` / history、checkpoint hint 与 required artifacts。不得默认执行 `git checkout <last_successful_commit>`，也不得因 pointer 落后 state commit 自动 rewind。
- Lifecycle `PUBLISHED` 仍不等于 transaction completed；必须在 `main` history 中找到该 Article 唯一 `Publish Agent Engineering Article NN` completion commit，并完成 commit message、files scope、working tree、`origin/main`、remote main 与 read-only reconciliation verification，才可开始下一篇。

## Update events

只在 `PRE_COMMIT_RECONCILIATION` 结束前的 transaction-level 事件更新：`ARTICLE_KICKOFF`、worker start、Worker Result validation、Gate pass、Gate fail、Article `PUBLISHED` candidate、`PRE_COMMIT_RECONCILIATION`、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。`last_worker_result` 只在 Master 完成 artifact、Allowed Writes、Gate 与 State Machine validation 后更新；不要为 worker 的每条消息或每个小动作更新本文件。Pre-Commit Reconciliation 后 repository writes 必须为 `ZERO`；Git Diff Verify、Checkpoint Commit、Commit Verify、Push Main、Remote Verify、Post-Commit Reconciliation 与 `END_ARTICLE` 不回写本文件。每篇 Article 与每次 Part / Final Audit 仍必须遵守各自独立 commit boundary。

## Historical transaction log and current boundary

> 下列记录按时间保留 Article 08 runtime regression evidence；旧记录中的 branch production、checkpoint 后 write 或当时的“当前”状态只描述历史执行，不是 schema v3 的允许流程。现行 pointer 与规则以上方 YAML 和 Field rules 为准。

Article 07 独立 checkpoint `f3de0f2a7b1e06c530900627183bd364ca0b4314` 已完成 commit / push / live remote verification。2026-08-20 fresh resume reconciliation 进一步确认 local `HEAD`、fresh-fetched `origin/main` 与 live `ls-remote refs/heads/main` 均为 `1045264057f1eced21f8e7438b43bb7448a67091`（`Checkpoint Article 08 at OUTLINE`），worktree clean，Article 08 published content / `outline.md` / `draft.md` 均不存在。Article 08 Lab 03=`VERIFIED / EVIDENCE_MERGED`，Evidence Gate=`PASS`，Claim=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`。本次恢复先后派发 fresh real Author `/root/article_08_author_outline` 与最小上下文 `/root/article_08_author_outline_minimal`；两者均在重复等待和明确收敛消息后保持运行态、没有创建 `outline.md`，已安全中断。连同仓库记录的三次历史 Author 无输出执行，当前 worker runtime 判定为 `SUBAGENT_RUNTIME_UNAVAILABLE`。Factory 安全暂停在 `OUTLINE`；Draft、Review 与 Article 09 均未启动。

2026-08-20 17:05 repository reconciliation 确认 local `HEAD == origin/main == cfd763c0ba52f6d2cfacd3dc7f8323b913529eec`、worktree clean、Article 08 Evidence / Lab Gate 与缺失资产仍一致。唯一 fresh real Author `/root/article_08_author_outline_fresh` 随后只创建 `articles/08-agent-loop/outline.md`，返回 `8 / 8 COVERED`、`NO NEW CORE FACT REQUIRED` 与 `PASS_RECOMMENDED`；Master artifact / write-boundary check=`PASS`。Factory 现为 `READY / AUTHOR_DRAFT`，但 Draft、Review、Published Content 与 Article 09 均未启动；继续生产需新的显式任务。

2026-08-20 18:07 仅执行 Worker Result Contract schema migration：`schema_version = 2`。17:05 Author OUTLINE execution 发生在 closed-schema contract 生效前，只有 legacy natural-language result，没有 canonical raw envelope，因此不得回填或伪造 `last_worker_result`；当前值保持 `NONE`，直到未来收到并验证首个合规 envelope。本次没有执行新的 Article Gate，没有启动 Draft / Review / Article 09，也没有修改 Article 或 Lab artifact。

2026-08-20 19:06 fresh resume reconciliation 确认 `main == origin/main == d01234cc0cf9480e72d689b2e86166ae52ccdf66`、worktree clean，Article 08 durable state 一致为 `OUTLINE_READY / AUTHOR_DRAFT`。Master 已在隔离分支 `codex/article-08-production` 登记 fresh Author `/root/article_08_author_draft`；Allowed Writes 仅为当前 Article `draft.md`，等待 closed-schema `worker_result`。Review、Published Content 与 Article 09 尚未启动。

2026-08-20 19:16 Author `/root/article_08_author_draft` 返回 schema-valid `PASS` envelope；Master 验证 `draft.md` 为唯一 worker-created artifact、Allowed Writes 与 actual diff 一致、无 delete / rename，Draft 包含 `8 / 8` Claim traceability、Learning Check、最短结论、Proposal / fixed-fixture scope 与完整 non-scope。Draft Gate=`PASS`，State Machine 合法推进到 `REVIEW`，并自动登记 fresh Reviewer `/root/article_08_reviewer_cycle0`。Published Content 与 Article 09 仍未启动。

2026-08-20 19:23 fresh Reviewer `/root/article_08_reviewer_cycle0` 返回 schema-valid `PASS` envelope，唯一修改为 `review.md`。Master 验证 Review=`92 / 100`，五维阈值全部通过，但 `08-F01 OPEN MINOR` 要求补齐 pre-decision guard terminal record / trace，故合法 route 为 `REVISION` 而非 `FINAL_GATE`。已登记 Revision Worker `/root/article_08_revision_cycle1`，只允许处理 `08-F01`。

2026-08-20 19:27 Revision Worker `/root/article_08_revision_cycle1` 返回 schema-valid `PASS` envelope；Master 验证只在 `draft.md` 补充 guard terminal commit / terminal-only trace 与 no-consumed-Step 说明，并在 `review.md` 写 `READY_FOR_RECHECK` disposition，未自行关闭 Finding、未扩展 Evidence 或 Article 11。已合法推进 `REVIEW_RECHECK` 并登记 fresh Reviewer `/root/article_08_reviewer_recheck_cycle1`；`review_cycle` 在 recheck 完成前仍为 `0`。

2026-08-20 19:32 fresh Reviewer recheck 返回 schema-valid `PASS` envelope；Master 验证 `review.md` 为唯一 worker-modified path、cycle=`1 / 3`、`08-F01 CLOSED`、unclosed Findings=`0`、score=`92 / 100` 且四项冻结最低线均满足。空 notes item 不参与任何验证结论；required fields 与 repository evidence 完整。已推进独立 `FINAL_GATE` 并登记 Reviewer `/root/article_08_final_gate`；尚未进入 Publish。

2026-08-20 19:37 Reviewer `/root/article_08_final_gate` 返回 schema-valid `PASS` envelope；Master 验证 Final Gate durable decision=`PASS`、Review=`92 / 100`、unclosed Findings=`0`、8 / 8 Claim 与 Evidence / Lab / non-scope 边界保持成立。Article Lifecycle 合法进入 `FINAL`，并自动登记 Publisher `/root/article_08_publisher` 执行机械发布映射；Build Verify 尚未开始。

2026-08-20 19:47 Publisher `/root/article_08_publisher` 返回 schema-valid `PASS` envelope；Master 验证新 Article 08 content、Article 07 单一 next-link 与 Article README Publication Result 均真实存在且属于 Allowed Writes。Front matter / series order / weight、5 个 ASCII-quote relref、7 个 Lab GitHub links、0 repository-relative links、paired fences、0 trailing whitespace 与 frozen Draft semantic mapping均通过；Build 仍明确为 `NOT_YET_EXECUTED`。已登记 Publisher `/root/article_08_build_verify` 执行独立 `BUILD_VERIFY`。

2026-08-20 19:53 Publisher `/root/article_08_build_verify` 返回 schema-valid `PASS` envelope；Master 独立核验 ignored `public/` 中 Article 08 route 与 Article 07↔08 rendered navigation，Build=`hugo --gc --minify / Hugo 0.157.0 / exit 0 / 1237 Pages / 0 ERROR / 0 WARNING`。Master 随后完成 `MASTER_STATE_UPDATE`：Article README、status、course README、canonical Article 08 link 与 run state 对齐为 `PUBLISHED` candidate，并登记 `GIT_DIFF_VERIFY`。Article checkpoint 尚未创建或验证，Article 09 未启动。

2026-08-20 19:56 Master 完成 `GIT_DIFF_VERIFY`：branch=`codex/article-08-production`；worktree 仅含 10 个 Article 08 transaction paths；Article 09 workspace=`ABSENT`；无 delete / rename / unrelated path；`git diff --check`=`PASS`；Master 重新执行 `hugo --gc --minify` 得 `1237 Pages / 0 ERROR / 0 WARNING / exit 0`。Factory 已进入 `ARTICLE_CHECKPOINT_COMMIT`，只允许显式 stage 这 10 个路径并创建 `Publish Agent Engineering Article 08` 本地 commit；尚未 push。

2026-08-20 19:59 Article 08 独立 checkpoint `d4693bd6d78ed63a669e181516e28247460fee11` 已完成 commit message、10-file scope、clean worktree、`git log`、`git show` 与 `git diff HEAD^ HEAD --check` verification；branch=`codex/article-08-production`，相对 `origin/main` 为 `0 behind / 1 ahead`，尚未 push。`END ARTICLE 08` 成立。Factory 已回到 `READY`，durable pointer 指向 Article 09 `PRECHECK`；Article 09 workspace=`ABSENT`，transaction、Research、Evidence、Lab 与 Draft 均未启动。

2026-08-20 20:37 Contract-only reconciliation from repository reality confirmed `main == origin/main == remote main == 1f4d26f51bc93437517cc7ad3e32319563222bf1`, worktree clean, Article 08 Lifecycle=`PUBLISHED`, Article checkpoint `d4693bd6d78ed63a669e181516e28247460fee11` and post-checkpoint reconciliation commit `1f4d26f51bc93437517cc7ad3e32319563222bf1` both exist, and Article 09 workspace=`ABSENT`。Schema v3 freezes `production_branch=main`、Git-history-authoritative checkpoint SHA、pre-commit final state reconciliation and zero post-commit repository writes；the Article 08 two-commit history remains regression evidence and is not rewritten。Article 09 PRECHECK remains `NOT_STARTED`。

2026-08-20 23:23 Article 09 fresh Reviewer recheck returned a schema-valid `PASS` envelope. Master verified `review.md` as the sole worker-modified path, `09-F01 / 09-F02 CLOSED`, unclosed Findings=`0`, cycle=`1 / 3`, score=`91 / 100`, and every frozen quality threshold met. `REVIEW_RECHECK -> FINAL_GATE` is valid; independent Reviewer `/root/article_09_final_gate` is registered. Published Content and Article 10 remain absent.

2026-08-20 23:31 Article 09 independent FINAL_GATE returned a schema-valid `PASS` envelope. Master verified the appended durable decision, `9 / 9` Claim traceability, score=`91 / 100`, zero unclosed Findings, preserved C03 PARTIAL / C01-C05-C08 PROPOSAL strength, AL-02 and authority boundaries, later-Article non-scope, and current-docs / 0.22.0-anchor limitation. Lifecycle is `FINAL`; Publisher `/root/article_09_publisher` is registered for mechanical PUBLISH only. Build Verify and Article 10 have not started.

2026-08-20 23:41 Article 09 Publisher returned a schema-valid `PASS` envelope. Master verified the three-path write boundary, standard front matter, exact frozen-Draft semantic reconstruction, Article 08↔09 source navigation, four AL-02 GitHub blob links, paired fences, no future relref, and zero trailing whitespace. Lifecycle remains `FINAL`; independent Publisher execution `/root/article_09_build_verify` is registered for `hugo --gc --minify` and rendered-route/navigation checks only. Article 10 remains absent.

2026-08-20 23:46 Article 09 BUILD_VERIFY returned a schema-valid `PASS` envelope and Master independently reran the build: `hugo --gc --minify / Hugo 0.157.0 / exit 0 / 1238 Pages / 0 ERROR / 0 WARNING`; rendered Article 09 route and Article 08↔09 navigation exist, tracked build-output changes=`0`. PRE_COMMIT_RECONCILIATION then verified Final / Publisher / Build / workspace / canonical / global state and wrote the completion-commit candidate: Article 09 Lifecycle=`PUBLISHED`, last published=`09`, next pointer=`Article 10 / PRECHECK`, Factory=`READY`, active worker=`NONE`. Article 10 workspace remains absent and PRECHECK is not started. Repository writes after this point are `ZERO`; Git / push / remote / post-commit results must remain runtime-only.

2026-08-20 23:50 first GIT_DIFF_VERIFY attempt correctly failed before commit because staged `git diff --cached --check` reported one extra blank line at EOF in `article-card.md`. No commit or push occurred. Master returned to PRE_COMMIT_RECONCILIATION retry 1, removed only that terminal blank line, recorded this recovery, and preserved every publication, state, canonical and Article 10 absence invariant. This retry supersedes the earlier persistence cut; after it, repository writes are again `ZERO` and GIT_DIFF_VERIFY must restart from the full 14-path scope.

2026-08-21 20:41 Article 12 first GIT_DIFF_VERIFY attempt correctly failed before commit because staged `git diff --cached --check` reported one extra blank line at EOF in `article-card.md`. No commit or push occurred. Master returned to PRE_COMMIT_RECONCILIATION retry 1, removed only that terminal blank line, recorded this recovery, and preserved every publication, state, canonical and Article 13 absence invariant. This retry supersedes the earlier persistence cut; after it, repository writes are again `ZERO` and GIT_DIFF_VERIFY must restart from the full 14-path scope.
