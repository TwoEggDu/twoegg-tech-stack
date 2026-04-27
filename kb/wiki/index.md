# Wiki Index

> AI 维护的索引页。每次 ingest / publish-sync 后更新。

## 维度入口

- [coverage-map.md](coverage-map.md) — 已发布文章的概念覆盖地图
- [log.md](log.md) — 操作日志（审计用）

## Concepts

> 概念页。第一次 ingest 后建立。

_（暂空——等待第一份 raw ingest）_

## Entities

> 具体版本 / 工具 / 框架页。

_（暂空——等待第一份 raw ingest）_

## Sources

> 来源摘要。一份 raw 一份 source。

_（暂空——等待第一份 raw ingest）_

## Queries

> 有沉淀价值的查询存档。

_（暂空——等待第一次有价值的 query）_

---

## 索引维护规则

1. AI 在 ingest / publish-sync / query 后必须更新本页的对应小节
2. 每个 concept / entity 至少要从 index 或某个 source 入链——孤立页在 lint 时会被标红
3. 本页不超过 500 行——超长时按主题拆分到子索引（`concepts/_index.md` 等）
