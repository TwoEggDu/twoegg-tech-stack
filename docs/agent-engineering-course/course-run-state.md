# Agent Engineering Course Factory Run State

> This file is the Factory execution pointer, not a second course database. Article facts remain in [status.md](status.md); execution rules remain in [course-factory.md](course-factory.md).

```yaml
schema_version: 2
factory_mode: SEQUENTIAL_SUBAGENT_FACTORY
factory_status: READY
current_article: "08"
current_gate: AUTHOR_DRAFT
last_published_article: "07"
active_worker: NONE
active_worker_execution_id: NONE
active_worker_record_ref: NONE
last_worker_result: NONE
last_worker_result_error: NONE
review_cycle: 0
active_blocker: NONE
stop_reason: NONE
human_decision_required: false
last_successful_commit: cfd763c0ba52f6d2cfacd3dc7f8323b913529eec
next_action: AWAIT_EXPLICIT_AUTHOR_DRAFT_ARTICLE_08
last_updated: "2026-08-20T18:07:59+08:00"
```

## Field rules

- `factory_status` 只使用 `READY / RUNNING / PAUSED / BLOCKED / COMPLETE`。
- `current_article` 是下一或当前 transaction pointer，不表示该 Article 已经启动；是否启动必须结合 `factory_status`、`current_gate` 与 [status.md](status.md) 判断。
- PRECHECK `PASS` 后必须执行显式 `ARTICLE_KICKOFF`，Factory 才能进入 `RUNNING` 并创建当前 workspace；pointer 指向 Article 不等于 Kickoff 已发生。
- `active_worker` 只使用 [subagent-contracts.md](subagent-contracts.md) 中的八种 role 或 `NONE`。
- `active_worker_execution_id` 与 `active_worker_record_ref` 在 worker start 时由 Master 写入。record ref 必须指向当前 Article `subagent-trace.md` 的 stable Worker Result Record，或 Part / Course Audit Report 中的等价 record；record 同时保存 bounded task brief、execution ID、raw envelope 与 validation result。worker 仍运行时保留 active fields；确认结束后才把 `active_worker` 和两个 active fields 统一清为 `NONE`。
- `last_worker_result` 初始或 legacy-migration 值可以为 `NONE`。只有 Master 收到 schema-valid envelope、写入 canonical raw record 并完成 validation 后，才能写入以下 durable projection：

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
- `last_worker_result.next_allowed_gate` 只保存通过 Master common mapping 与 State Machine validation 的 forward transition 或 recovery candidate。非 terminal `status: PASS` 时不得为 `NONE`；`gate_completed: false` 只允许指向合同冻结的 retry / return Gate。该 pointer 仍不是 Article / Transaction completion，也不能替代 `current_gate`、`factory_status`、canonical raw record、required artifacts 或 Git evidence。
- `review_cycle` 只在一次 `Findings -> Revision -> Recheck` 完成后递增，最大值为 `3`。
- `stop_reason` 只使用 `NONE / BLOCKED_EVIDENCE / FAILED_LAB / FAILED_REVIEW / FAILED_PUBLICATION / HUMAN_DECISION_REQUIRED / REPOSITORY_CONFLICT`。
- Part Auditor 返回 `PART_AUDIT_FINDINGS` 时不得把 role-specific code 直接写入 `stop_reason`；Master 必须唯一映射为 `factory_status: PAUSED`、`active_blocker: PART_AUDIT_FINDINGS`、`stop_reason: HUMAN_DECISION_REQUIRED`、`human_decision_required: true`。只有人类批准 Audit Report 中的 affected Article 与 targeted repair scope 后，Resume 才能选择具体 Article / Gate。
- `last_successful_commit` 是最近一个已知可恢复的 durable checkpoint hint。它不是 blind checkout target、当前 `HEAD` 的绝对真相或 Resume 的唯一依据；state-pointer commit 自身不要求自引用，也不为同步 hash 制造 commit loop。
- Resume 必须联合检查本文件、`status.md`、current Article workspace、Published Content、`git status`、Git `HEAD` / history、checkpoint hint 与 required artifacts。不得默认执行 `git checkout <last_successful_commit>`，也不得因 pointer 落后 state commit 自动 rewind。
- Lifecycle `PUBLISHED` 仍不等于 transaction completed；必须在 Git history 中找到该 Article 的独立 `Publish Agent Engineering Article NN` checkpoint，并完成 commit message、files scope、working tree 与 lifecycle verification，才可开始下一篇。

## Update events

只在 transaction-level 事件更新：`ARTICLE_KICKOFF`、worker start、Worker Result validation、Gate pass、Gate fail、Article `PUBLISHED` candidate、Article Commit Verify、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。`last_worker_result` 只在 Master 完成 artifact、Allowed Writes、Gate 与 State Machine validation 后更新；不要为 worker 的每条消息或每个小动作更新本文件。每篇 Article 与每次 Part / Final Audit 仍必须遵守各自独立 commit boundary。

## Current transaction boundary

Article 07 独立 checkpoint `f3de0f2a7b1e06c530900627183bd364ca0b4314` 已完成 commit / push / live remote verification。2026-08-20 fresh resume reconciliation 进一步确认 local `HEAD`、fresh-fetched `origin/main` 与 live `ls-remote refs/heads/main` 均为 `1045264057f1eced21f8e7438b43bb7448a67091`（`Checkpoint Article 08 at OUTLINE`），worktree clean，Article 08 published content / `outline.md` / `draft.md` 均不存在。Article 08 Lab 03=`VERIFIED / EVIDENCE_MERGED`，Evidence Gate=`PASS`，Claim=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`。本次恢复先后派发 fresh real Author `/root/article_08_author_outline` 与最小上下文 `/root/article_08_author_outline_minimal`；两者均在重复等待和明确收敛消息后保持运行态、没有创建 `outline.md`，已安全中断。连同仓库记录的三次历史 Author 无输出执行，当前 worker runtime 判定为 `SUBAGENT_RUNTIME_UNAVAILABLE`。Factory 安全暂停在 `OUTLINE`；Draft、Review 与 Article 09 均未启动。

2026-08-20 17:05 repository reconciliation 确认 local `HEAD == origin/main == cfd763c0ba52f6d2cfacd3dc7f8323b913529eec`、worktree clean、Article 08 Evidence / Lab Gate 与缺失资产仍一致。唯一 fresh real Author `/root/article_08_author_outline_fresh` 随后只创建 `articles/08-agent-loop/outline.md`，返回 `8 / 8 COVERED`、`NO NEW CORE FACT REQUIRED` 与 `PASS_RECOMMENDED`；Master artifact / write-boundary check=`PASS`。Factory 现为 `READY / AUTHOR_DRAFT`，但 Draft、Review、Published Content 与 Article 09 均未启动；继续生产需新的显式任务。

2026-08-20 18:07 仅执行 Worker Result Contract schema migration：`schema_version = 2`。17:05 Author OUTLINE execution 发生在 closed-schema contract 生效前，只有 legacy natural-language result，没有 canonical raw envelope，因此不得回填或伪造 `last_worker_result`；当前值保持 `NONE`，直到未来收到并验证首个合规 envelope。本次没有执行新的 Article Gate，没有启动 Draft / Review / Article 09，也没有修改 Article 或 Lab artifact。
