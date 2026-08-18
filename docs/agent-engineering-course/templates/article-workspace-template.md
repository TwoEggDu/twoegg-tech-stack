# Article Workspace 模板

每次只为当前生产文章复制一次本模板结构。不要提前生成完整课程的空目录。

## 目录约定

```text
articles/<id>-<slug>/
├─ README.md          # 当前状态、Gate、入口与下一动作
├─ article-card.md    # 文章定位与课程职责
├─ research.md        # 研究问题、来源计划与未决问题
├─ evidence.md        # Claim Register 与 Evidence Cards
├─ outline.md         # 详细提纲；研究阶段只能放空骨架
├─ review.md          # 三类审查记录
├─ draft.md           # 仅在 DRAFTING Gate 创建
└─ assets/            # 有真实工作资产时才创建
```

## README 元数据

```markdown
# Article <ID>｜<Title>

- Canonical ID: `<ID>`
- Workspace: `<id>-<slug>`
- Lifecycle Status: `PLANNED`
- Evidence Status: `BLOCKED`
- Required Lab: `NONE`
- Current Gate: `Article Card`
- Next Allowed Action: `<action>`
- Blocker: `<blocker>`
```

## 文件职责

| 文件 | 可以写 | 不可以写 |
|---|---|---|
| `article-card.md` | 文章问题、依赖、边界、读者变化、证据与 Lab 需求 | 正文段落、预设研究结论 |
| `research.md` | 问题、来源候选、验证计划、风险 | 把候选来源写成已确认事实 |
| `evidence.md` | 主张、证据状态、证明边界、复现信息 | 没有来源的确定性结论 |
| `outline.md` | 已获证据支撑的段落职责与论证顺序 | 证据未就绪时自动填满结论 |
| `draft.md` | 依据批准提纲形成的正文 | 越过 Evidence Gate 的新主张 |
| `review.md` | 技术、证据、课程审查及处置 | 用“整体没问题”替代逐项审查 |

## 发布映射

- 正文：`content/ai-empowerment/agent-engineering-<id>-<slug>.md`
- 发布图片：`static/images/agent-engineering/<id>-<slug>/`
- 课程系列：`Agent Engineering`
- `primary_series`：`agent-engineering`
- `series_order`：`(ID + 1) × 10`
- `weight`：`3000 + series_order`

工作区是生产过程记录，不直接作为 Hugo 页面发布。发布后仍保留它，用于证据追溯和后续修订。
