# Agent Engineering Course Factory Run State

> This file is the Factory execution pointer, not a second course database. Article facts remain in [status.md](status.md); execution rules remain in [course-factory.md](course-factory.md).

```yaml
schema_version: 1
factory_mode: SEQUENTIAL_SUBAGENT_FACTORY
factory_status: READY
current_article: "08"
current_gate: AUTHOR_DRAFT
last_published_article: "07"
active_worker: NONE
review_cycle: 0
active_blocker: NONE
stop_reason: NONE
human_decision_required: false
last_successful_commit: cfd763c0ba52f6d2cfacd3dc7f8323b913529eec
next_action: AWAIT_EXPLICIT_AUTHOR_DRAFT_ARTICLE_08
last_updated: "2026-08-20T17:05:42+08:00"
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

Article 07 独立 checkpoint `f3de0f2a7b1e06c530900627183bd364ca0b4314` 已完成 commit / push / live remote verification。2026-08-20 fresh resume reconciliation 进一步确认 local `HEAD`、fresh-fetched `origin/main` 与 live `ls-remote refs/heads/main` 均为 `1045264057f1eced21f8e7438b43bb7448a67091`（`Checkpoint Article 08 at OUTLINE`），worktree clean，Article 08 published content / `outline.md` / `draft.md` 均不存在。Article 08 Lab 03=`VERIFIED / EVIDENCE_MERGED`，Evidence Gate=`PASS`，Claim=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`。本次恢复先后派发 fresh real Author `/root/article_08_author_outline` 与最小上下文 `/root/article_08_author_outline_minimal`；两者均在重复等待和明确收敛消息后保持运行态、没有创建 `outline.md`，已安全中断。连同仓库记录的三次历史 Author 无输出执行，当前 worker runtime 判定为 `SUBAGENT_RUNTIME_UNAVAILABLE`。Factory 安全暂停在 `OUTLINE`；Draft、Review 与 Article 09 均未启动。

2026-08-20 17:05 repository reconciliation 确认 local `HEAD == origin/main == cfd763c0ba52f6d2cfacd3dc7f8323b913529eec`、worktree clean、Article 08 Evidence / Lab Gate 与缺失资产仍一致。唯一 fresh real Author `/root/article_08_author_outline_fresh` 随后只创建 `articles/08-agent-loop/outline.md`，返回 `8 / 8 COVERED`、`NO NEW CORE FACT REQUIRED` 与 `PASS_RECOMMENDED`；Master artifact / write-boundary check=`PASS`。Factory 现为 `READY / AUTHOR_DRAFT`，但 Draft、Review、Published Content 与 Article 09 均未启动；继续生产需新的显式任务。
