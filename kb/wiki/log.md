# Wiki Operation Log

> 每次 AI 对 wiki/ 的操作必须追加到本文件。
> 格式：`## [YYYY-MM-DD] <action> | <target>` + 简短说明。

## [2026-04-27] init | kb/ 初始化

- 创建 kb/CLAUDE.md（schema）
- 创建 kb/raw/README.md（边界声明）
- 创建 kb/wiki/index.md（索引占位）
- 创建 kb/wiki/log.md（本文件）
- 创建 kb/wiki/coverage-map.md（覆盖地图占位）

下一步：等待第一份 raw 进入 `kb/raw/`，开始第一次 ingest。

---

## Action 类型参考

- `init` — 初始化
- `ingest` — 处理新 raw，建 source / concepts / entities
- `publish-sync` — 同步新发布文章到 coverage-map
- `query` — 回答问题（有沉淀价值时存档）
- `lint` — 自检（断链 / 孤立页 / 漂移）
- `fix` — 修复 lint 报告里的问题
- `restructure` — 重组（拆 / 合并页面，必须写明理由）
