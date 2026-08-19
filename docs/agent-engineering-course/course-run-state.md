# Agent Engineering Course Factory Run State

> This file is the Factory execution pointer, not a second course database. Article facts remain in [status.md](status.md); execution rules remain in [course-factory.md](course-factory.md).

```yaml
schema_version: 1
factory_mode: SEQUENTIAL_SUBAGENT_FACTORY
factory_status: READY
current_article: "02"
current_gate: ARTICLE_COMMIT_VERIFY
last_published_article: "02"
active_worker: NONE
review_cycle: 1
active_blocker: NONE
stop_reason: NONE
human_decision_required: false
last_successful_commit: 798443c1d41f03960253b1190fcbc91425d4f285
next_action: START_ARTICLE_03_PRECHECK_AFTER_ARTICLE_02_COMMIT_VERIFIED
last_updated: "2026-08-20T00:00:24+08:00"
```

## Field rules

- `factory_status` 只使用 `READY / RUNNING / PAUSED / BLOCKED / COMPLETE`。
- `current_article` 是下一或当前 transaction pointer，不表示该 Article 已经启动；是否启动必须结合 `factory_status`、`current_gate` 与 [status.md](status.md) 判断。
- PRECHECK `PASS` 后必须执行显式 `ARTICLE_KICKOFF`，Factory 才能进入 `RUNNING` 并创建当前 workspace；pointer 指向 Article 不等于 Kickoff 已发生。
- `active_worker` 只使用 [subagent-contracts.md](subagent-contracts.md) 中的八种 role 或 `NONE`。
- `review_cycle` 只在一次 `Findings -> Revision -> Recheck` 完成后递增，最大值为 `3`。
- `stop_reason` 只使用 `NONE / BLOCKED_EVIDENCE / FAILED_LAB / FAILED_REVIEW / FAILED_PUBLICATION / HUMAN_DECISION_REQUIRED / REPOSITORY_CONFLICT`。
- `last_successful_commit` 是最近一个已知可恢复的 durable checkpoint hint。它不是 blind checkout target、当前 `HEAD` 的绝对真相或 Resume 的唯一依据；state-pointer commit 自身不要求自引用，也不为同步 hash 制造 commit loop。
- Resume 必须联合检查本文件、`status.md`、current Article workspace、Published Content、`git status`、Git `HEAD` / history、checkpoint hint 与 required artifacts。不得默认执行 `git checkout <last_successful_commit>`，也不得因 pointer 落后 state commit 自动 rewind。
- Lifecycle `PUBLISHED` 仍不等于 transaction completed；必须在 Git history 中找到该 Article 的独立 `Publish Agent Engineering Article NN` checkpoint，并完成 commit message、files scope、working tree 与 lifecycle verification，才可开始下一篇。

## Update events

只在 transaction-level 事件更新：`ARTICLE_KICKOFF`、worker start、Gate pass、Gate fail、Article `PUBLISHED` candidate、Article Commit Verify、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。不要为每个小动作更新或提交本文件；每篇 Article 与每次 Part / Final Audit 仍必须遵守各自独立 commit boundary。

## Current transaction boundary

Article 02 已完成 `ARTICLE_KICKOFF`、Research、Evidence、Outline、Draft、一次 Revision / Recheck、Final Gate、Publisher、Build 与 Master State Reconciliation；Reviewer `92 / 100 PASS`，Publisher `PASS`，Hugo `1231 Pages / 0 ERROR / 0 WARNING`，Lifecycle 为 `PUBLISHED`。`last_successful_commit` 仍保留上一个可恢复 checkpoint，避免当前 state commit 自引用；只有 Git history 进一步证明 `Publish Agent Engineering Article 02` 已提交且 `ARTICLE_COMMIT_VERIFIED = PASS` 后，才允许启动 Article 03 PRECHECK。
