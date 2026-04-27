# kb/raw/ — 只读源材料

这个目录是 KB 的"事实层"：源材料的原始摘录。

## AI 边界

**AI 一律不准修改、删除、移动、重命名 `raw/` 下的任何文件。** 只能读。

如果发现 raw 里某份材料过时或有错，标记到 `wiki/log.md` 让人工处理，不要自己动手。

## 推荐子目录

按需建，不必一开始就建齐：

- `source-reading/` — Unity / Mono / CoreCLR / IL2CPP 等源码阅读笔记
- `papers/` — 渲染 / GC / 编译器 / 数据库等论文摘录
- `articles/` — 外部技术文章摘录（保留原作者署名 + 链接）
- `notes/` — 你自己的临时笔记 / 决策记录 / 会议纪要

## 文件命名

- 一份材料一个文件，不要把多份合并到一个 .md 里
- 命名：`<topic>-<short-name>.md`，比如 `gc-mono-boehm-overview.md`
- 文件第一行用 `# 标题`，后面附原始来源 URL 或物理路径

## 一份典型 raw 长什么样

```markdown
# GC.Collect 在 Mono 与 CoreCLR 的语义差异

来源：https://learn.microsoft.com/dotnet/standard/garbage-collection/
读取时间：2026-04-27
读者：作者本人

[原文摘录或自己整理的笔记内容...]

[可以是大段复制，可以是要点提炼，AI 不准动这个文件的内容]
```

这份材料处理之后会在 `wiki/sources/<slug>.md` 出现一份摘要页，concepts / entities 也会被建立或更新。但 `raw/` 这份原始文件永远保持原样。
