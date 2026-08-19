# Agent Engineering Course Factory Run State

> This file is the Factory execution pointer, not a second course database. Article facts remain in [status.md](status.md); execution rules remain in [course-factory.md](course-factory.md).

```yaml
schema_version: 1
factory_mode: SEQUENTIAL_SUBAGENT_FACTORY
factory_status: READY
current_article: "02"
current_gate: PRECHECK
last_published_article: "01"
active_worker: NONE
review_cycle: 0
active_blocker: NONE
stop_reason: NONE
human_decision_required: false
last_successful_commit: 36d41ff06f989a7aa120c6ce8068f6d8afa23797
next_action: START_ARTICLE_02_PRECHECK
last_updated: "2026-08-19T21:59:26+08:00"
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

## Foundation boundary

当前 `READY + 02 + PRECHECK` 只表示未来允许执行 `START_ARTICLE_02_PRECHECK`。Article 02 在 [status.md](status.md) 中仍为 `PLANNED`，没有 workspace、Research、Evidence、Draft 或 active worker；PRECHECK 通过后仍须执行 `ARTICLE_KICKOFF`。
