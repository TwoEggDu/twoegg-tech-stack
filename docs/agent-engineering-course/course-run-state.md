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
- `last_successful_commit` 记录最近一个已落盘、可恢复的完整内容 checkpoint；state-pointer commit 自身不要求自引用。Resume 时仍须与 Git history 核对。

## Update events

只在 transaction-level 事件更新：worker start、Gate pass、Gate fail、Article `PUBLISHED`、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。不要为每个小动作更新或提交本文件。

## Foundation boundary

当前 `READY + 02 + PRECHECK` 只表示未来允许执行 `START_ARTICLE_02_PRECHECK`。Article 02 在 [status.md](status.md) 中仍为 `PLANNED`，没有 workspace、Research、Evidence、Draft 或 active worker。
