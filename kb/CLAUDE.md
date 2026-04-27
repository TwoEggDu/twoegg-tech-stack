# kb/ — 内容仓库的知识沉淀层

> 这是 AI 协作金字塔的「记忆层」。给人和 AI 的 schema 文档。

## 这一层是什么

`kb/` 与 `docs/` 完全不同。两者在仓库里平行存在，但职责互不重叠：

| 目录 | 职责 | 内容 | 维护节奏 |
|------|------|------|---------|
| `docs/` | **前置规划** | 系列 plan / outline / workorder | 写文章前规划用 |
| `kb/` | **回顾沉淀** | 概念 / 实体 / 来源 / 覆盖地图 | 写文章后整理用 |
| `content/` | **发布产物** | Hugo 渲染的实际文章 | 单调增长 |

通俗一点：

- `docs/` = "我打算写什么"
- `kb/` = "我读懂了什么 / 我已经写过什么 / 这个概念散落在哪几篇"
- `content/` = "我已经发布了什么"

**为什么需要 kb/**：当 600+ 规划、14 个栏目、跨多个系列时，作者本人会忘记"GC 这件事我之前在哪几篇讲过、各篇侧重什么角度"。`kb/` 就是给作者（人 + AI）的工作笔记。

## 二层结构：raw / wiki

```
kb/
├── CLAUDE.md              ← 本文件（schema）
├── raw/                   ← 只读源（人类策划，AI 不准改）
│   ├── source-reading/    Unity / Mono / CoreCLR 等源码笔记
│   ├── papers/            渲染 / GC / 编译器论文摘录
│   ├── articles/          外部技术文章摘录
│   └── notes/             你的临时笔记 / 决策记录
└── wiki/                  ← AI 全权维护
    ├── index.md           索引
    ├── log.md             操作日志
    ├── coverage-map.md    ★ 概念在已发布文章里的覆盖地图（本仓库特色）
    ├── concepts/          概念页（GC.Collect / Shader Variant / AssetBundle 等）
    ├── entities/          实体页（Unity 2022.3 LTS / URP 14 / HybridCLR 等）
    ├── sources/           来源摘要（一份 raw 一个摘要页）
    └── queries/           查询存档（你问过的有价值的问题 + 当时答案）
```

**首次启动时**：`raw/` 与 `wiki/` 下的子目录可以按需创建——AI 在第一次 ingest 时按需建。

## 设计哲学：分离"事实"与"理解"

| | raw | wiki |
|--|-----|------|
| 来源 | 外部（论文 / 文章 / 决策邮件 / 你的源码笔记） | AI 读完 raw 整理出来的 |
| 可信度 | 最高（原始事实） | 派生（AI 解读，可能有错） |
| 谁维护 | **只能由人手动放入** | **AI 主写**，人类策划方向 |
| 修改 | 不可篡改（AI 一个字都不准动） | AI 随时增删改 |

**没有这个分离，AI 自己写的解读迟早会"覆盖"原始事实。**

## 三个流程

### Ingest（处理新 raw）

触发：用户说"我把 X 放到 raw/ 了，跑下 ingest"。

步骤：
1. 读取 `raw/<path>/<file>` 全文
2. 在 `wiki/sources/<source-slug>.md` 建一份摘要页（不超过 300 字）
3. 提取核心概念，每个概念在 `wiki/concepts/<concept-slug>.md` 建页或更新
4. 提取核心实体（具体版本 / 工具 / 框架），更新 `wiki/entities/`
5. 在 `wiki/log.md` 追加一条 `[YYYY-MM-DD] ingest | <source>`
6. **不要**碰 `coverage-map.md`——那是 publish-sync 的活

### Publish-sync（同步已发布文章到 coverage-map）

触发：用户说"我刚发了篇文章，更新下 coverage-map"，或定期跑。

步骤：
1. 读 `content/<section>/<slug>.md` frontmatter + 正文
2. 提取这篇文章涉及的核心 concepts / entities
3. 在 `wiki/coverage-map.md` 对应概念条目下追加这篇文章及侧重角度
4. 在 `wiki/log.md` 追加 `[YYYY-MM-DD] publish-sync | <article-slug>`

### Query（回答问题）

触发：用户基于 wiki 问问题。

步骤：
1. 优先读 `wiki/index.md` 和 `wiki/coverage-map.md` 定位
2. 钻到 `wiki/concepts/` 或 `wiki/entities/` 找答案
3. 如果是"我之前在哪讲过 X"——直接看 coverage-map
4. 如果问题有沉淀价值，存到 `wiki/queries/<question-slug>.md`
5. 在 `wiki/log.md` 追加 `[YYYY-MM-DD] query | <question>`

### Lint（自检）

触发：定期 / 用户说"跑下 lint"。

检查项：
- 断链（`wiki/index.md` 引用的页面是否都存在）
- 孤立页（`concepts/*.md` 是否至少有 1 个入链）
- coverage-map 与 content/ 漂移（map 里的文章 slug 是否还在 content/ 里）
- raw 是否被 AI 篡改（git diff 检查）

输出报告到 `wiki/log.md`，不主动改东西。

---

## 页面格式

### concepts / entities 页

```markdown
---
title: "概念或实体名"
slug: "kebab-case-slug"
type: "concept"           # 或 "entity"
created: "2026-04-27"
updated: "2026-04-27"
---

## 一句话定义
[20 字内]

## 关键事实
- [事实 1，附 source 引用]
- [事实 2]

## 在已发布文章中的覆盖
- [文章标题](/content/<section>/<slug>.md) — 侧重 [角度]

## 来源
- [source page]({{ relative link }})

## 相关页面
- [双向链接，含 concepts / entities]
```

### sources 页

```markdown
---
title: "来源标题（论文 / 文章名 / 笔记标题）"
slug: "source-slug"
source_type: "paper" / "article" / "source-reading" / "note"
raw_path: "kb/raw/<path>/<file>"
created: "2026-04-27"
---

## 摘要
[300 字内]

## 提取的概念
- [concepts/<slug>.md](concepts/<slug>.md)

## 提取的实体
- [entities/<slug>.md](entities/<slug>.md)
```

### queries 页

```markdown
---
title: "问题原文"
slug: "question-slug"
asked: "2026-04-27"
---

## 当时的回答
[基于 wiki 的回答]

## 引用的页面
- [...]
```

---

## coverage-map.md 的特殊用法（本仓库核心特色）

`coverage-map.md` 是给"内容仓库"的 KB 加的特殊文件。它回答两类问题：

1. "GC.Collect 这个概念我之前在哪几篇文章讲过、各篇侧重什么角度？"
2. "我要写新一篇 X，已经被前文覆盖的角度有哪些？哪些角度还没人讲过？"

格式：

```markdown
# Coverage Map

## GC.Collect

- [csharp-to-clr-gc-basics](/content/code-quality/csharp-to-clr-gc-basics.md) — 入门概念
- [game-performance-gc-pitfalls](/content/performance/game-performance-gc-pitfalls.md) — 实战坑
- 待补角度：Unity 与 .NET 的 GC 差异、IL2CPP GC 行为

## Shader Variant
...
```

这个文件让"我已经写过什么"显式化——比单靠 doc-plan 状态准确得多。

---

## AI 边界（必读）

1. **`raw/` 下任何文件 AI 都不准修改、不准删除**——只能读
2. **`wiki/` 下 AI 全权维护**——不需要每次问用户
3. **每次 ingest / publish-sync / query / lint 必须追加到 `log.md`**——可审计
4. **不要把 raw 内容大段复制到 wiki**——wiki 是解读，不是搬运
5. **遇到与 content/ 已发布文章口径冲突的内容**：在 wiki 标 `[与 content/<slug> 口径冲突，待人工裁决]`，不要自动选边
6. **kb/ 不进 Hugo 渲染**——hugo.toml 默认只渲染 `content/`，但请永远不要在 `kb/` 里建 `_index.md` 触发渲染

## 与既有 skill 的关系

- `col-next` 看 doc-plan 决定下一篇——**未来可以扩展为也读 coverage-map，避免重复角度**
- `col-publish-audit` 对账 plan 与 content——**未来可以扩展为同时把新发布文章 sync 到 coverage-map**
- `col-consistency` 一致性审查——**未来可以从 wiki/concepts 读术语规范，比当前临时扫 docs/ 准确**

这些扩展不必现在做，但是 kb/ 长起来后值得回头打通。

---

## 启动节奏建议

参考金字塔 04 篇——"KB 的复利在第二个、第三个、第十个源进来之后才显现"。

第 1 周：
- 挑一份你最近读完的源材料（论文 / 源码笔记 / 决策记录）放 `raw/`
- 让 AI 跑一次 ingest，看建出的 wiki/ 是否符合预期
- 调整本 schema

第 1 个月：
- 至少 5 份 raw 进 KB
- coverage-map 至少覆盖 10 个核心概念
- 第一次 lint 跑通

第 1 季度：
- 写文章前先查 coverage-map 成为习惯
- 每次发布新文章后 publish-sync
- 半年回头看 log.md，能讲清楚"KB 怎么长成现在这样"
