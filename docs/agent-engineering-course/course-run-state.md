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
last_successful_commit: 8d220adbbec409c04d2421f16aabbbb83d208df1
next_action: START_ARTICLE_02_PRECHECK
last_updated: "2026-08-19T17:18:08+08:00"
```

## Field rules

- `factory_status` 只使用 `READY / RUNNING / PAUSED / BLOCKED / COMPLETE`。
- `current_article` 是下一或当前 transaction pointer，不表示该 Article 已经启动；是否启动必须结合 `factory_status`、`current_gate` 与 [status.md](status.md) 判断。
- `active_worker` 只使用 [subagent-contracts.md](subagent-contracts.md) 中的八种 role 或 `NONE`。
- `review_cycle` 只在一次 `Findings -> Revision -> Recheck` 完成后递增，最大值为 `3`。
- `stop_reason` 只使用 `NONE / BLOCKED_EVIDENCE / FAILED_LAB / FAILED_REVIEW / FAILED_PUBLICATION / HUMAN_DECISION_REQUIRED / REPOSITORY_CONFLICT`。
- `last_successful_commit` 是最近一个已知可恢复的 durable checkpoint hint。它不是 blind checkout target、当前 `HEAD` 的绝对真相或 Resume 的唯一依据；state-pointer commit 自身不要求自引用，也不为同步 hash 制造 commit loop。
- Resume 必须联合检查本文件、`status.md`、current Article workspace、Published Content、`git status`、Git `HEAD` / history、checkpoint hint 与 required artifacts。不得默认执行 `git checkout <last_successful_commit>`，也不得因 pointer 落后 state commit 自动 rewind。

## Update events

只在 transaction-level 事件更新：worker start、Gate pass、Gate fail、Article `PUBLISHED`、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。不要为每个小动作更新或提交本文件。

## Foundation boundary

当前 `READY + 02 + PRECHECK` 只表示未来允许执行 `START_ARTICLE_02_PRECHECK`。Article 02 在 [status.md](status.md) 中仍为 `PLANNED`，没有 workspace、Research、Evidence、Draft 或 active worker。
