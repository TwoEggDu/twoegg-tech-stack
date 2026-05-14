---
title: "AI 赋能 11｜kb/ 与 LLM Wiki 范式：把 Karpathy 的 idea 适配到内容站"
slug: "ai-empowerment-11-kb-and-llm-wiki-pattern"
date: "2026-05-13"
description: "Karpathy 2026 年开源的 LLM Wiki 是一个范式：让 LLM 增量构建持久 Wiki。本站 kb/raw + kb/wiki 二层架构跟它高度同构——这篇老老实实讲两边的对照、内容站场景下的适配、以及为什么没采用 GBrain 的向量混合检索。"
tags:
  - "AI Engineering"
  - "Knowledge Management"
  - "LLM Wiki"
  - "Content Production"
series: "AI 赋能游戏开发"
primary_series: "ai-empowerment"
series_role: "article"
series_order: 110
weight: 210
---

> **读这篇之前**：本篇假设你已经读过 [04｜知识缺口发现与回流]({{< relref "ai-empowerment/ai-empowerment-04-knowledge-gap-feedback.md" >}})。这篇不重讲知识闭环的基础概念，讲的是把 Karpathy 的 LLM Wiki 范式落到一个 Hugo 内容站时**哪些地方原样能用、哪些地方必须改**。

> **本篇当前状态**：骨架。需要 kb/wiki/ 下至少 20 篇 concept / entity 页面真实长出来之后才能完成。当前可读但留有大量 `DATA-TODO`。

## 立场标注（必读）

写这篇之前我必须先把一件事说清楚——**本站的 `kb/raw + kb/wiki` 二层架构不是我独立沉淀出来的**。

2026 年 4 月 Karpathy 在 GitHub gist 上开源了一个名叫 LLM Wiki 的范式文档，提出三层架构：

- **Raw Sources**：只读源、人类策划、LLM 不准改
- **The Wiki**：LLM 全权维护的 Markdown 结构化知识
- **The Schema**：CLAUDE.md / AGENTS.md 形式的元指令

我在那前后看过这份 gist。我现在的 `kb/` 二层架构（外加 `kb/CLAUDE.md` 作 schema），跟它**高度同构**。我没法说"完全独立"，最诚实的表述是：

> **本站 kb/ 的设计属于"读到过 LLM Wiki 的思想 + 内容站场景适配"——不是抄、但也不是从零独立沉淀**。

这件事在写之前就纠结过——要不要写得像"我独立想到的"。结论是不要。一是不真实，二是这种"独立收敛"在中文工程圈写多了，读者会失去判断力。我宁愿这篇文章的卖点退一格，从"独立收敛"退到"诚实适配"——也比读者后来发现作者在装强。

下面进入正文。

## 这篇解决什么问题

很多人读完 Karpathy 的 LLM Wiki 之后，第一反应是想抄一遍——建 `raw/`、建 `wiki/`、写 schema、让 AI 去维护。

直接抄会撞墙。撞的不是范式本身，是**场景差异**：

- LLM Wiki 的典型示例是个人研究（看论文 / 听播客 / 跟踪一个主题）
- 我的场景是 Hugo 内容站，**目标是发布文章**，不是积累一个私人脑库
- LLM Wiki 假设你"读了大量外部资料"——内容站作者读的不是外部资料，是自己写的代码、自己跑的实验、自己以前发的文章
- LLM Wiki 的 raw 多数是 web clipper 抓下来的 markdown——内容站的 raw 是源码笔记、决策记录、跨项目的草稿片段

这些差异落到具体设计上，决定了 **kb/wiki/ 长出来的形态跟 Karpathy 的示例不一样**。

这篇文章要回答的就是：

1. LLM Wiki 三层架构在内容站场景下哪些原样能用、哪些要改
2. raw / wiki / schema 在内容站里具体放什么、不放什么
3. 为什么没采用 GBrain 那套向量混合检索 + 实体关系图谱
4. kb/ 跟 docs/ 的边界为什么这么重要

## LLM Wiki 三层架构 vs 本站 kb/ 实装

先把对照画出来。Karpathy 原始范式：

```
Raw Sources（不可变、人类策划）
The Wiki（LLM 全权维护、index.md + log.md + concepts/ + entities/）
The Schema（CLAUDE.md / AGENTS.md）
```

本站 kb/ 当前实装：

```
kb/
├── CLAUDE.md              ← schema 层
├── raw/                   ← Raw Sources 层
│   ├── source-reading/    Unity / Mono / CoreCLR 等源码笔记
│   ├── papers/            渲染 / GC / 编译器论文摘录
│   ├── articles/          外部技术文章摘录
│   └── notes/             临时笔记 / 决策记录
└── wiki/                  ← Wiki 层
    ├── index.md           索引
    ├── log.md             操作日志
    ├── coverage-map.md    ★ 概念在已发布文章里的覆盖地图
    ├── concepts/          概念页
    ├── entities/          实体页
    ├── sources/           来源摘要
    └── queries/           查询存档
```

跟 Karpathy 原始范式的同构部分：

- 三层划分完全一致
- raw 不可变 / wiki 由 AI 维护 / schema 是元指令——都没改
- `index.md` 和 `log.md` 两个特殊文件保留了原始命名
- `concepts/` 和 `entities/` 分类直接采用

跟原始范式不同的部分：

| 维度 | Karpathy 原始 | 本站适配 |
|------|--------------|---------|
| raw 子目录 | 通用（articles / papers / notes / clippings） | **加了 `source-reading/`**——这是内容站特有的"读引擎源码的笔记"层 |
| wiki 子目录 | concepts + entities + sources | **加了 `coverage-map.md`**——这是本站特色 |
| 查询日志 | 没专门提 | **加了 `queries/` 目录**——存"有价值的问题 + 当时的答案" |
| 链接形式 | Obsidian 的 `[[wikilink]]` | **Hugo 的 `{{</* relref */>}}`**（虽然 kb 本身不被 Hugo 渲染） |

最后那一项很关键——我会在"水土不服"章节展开。

## raw 层在内容站里到底放什么

这是 LLM Wiki 落地内容站最容易踩偏的地方。

Karpathy 示例里的 raw 长什么样：

- Obsidian Web Clipper 抓下来的网页文章
- arXiv 论文 markdown 化
- 播客转录
- 你看完一本书的笔记

如果作者照搬这份 raw 设定，就会出问题——**内容站作者读的东西不是这些**。

本站 raw 层的实际内容（按 `kb/raw/README.md` 的规约）：

- `source-reading/`：Unity / Mono / CoreCLR / IL2CPP / HybridCLR 等的源码阅读笔记。这是私仓性质——可以直接引行号、贴函数签名，不受 license 限制
- `papers/`：渲染管线、GC、编译器、Shader 变体相关论文的摘录
- `articles/`：跟主题相关的外部技术文章——但**只摘核心论点和数据**，不全文转载
- `notes/`：跨项目的草稿、决策记录、问题清单

跟 Karpathy 范式的关键差异：**raw 是为"写文章"服务的，不是为"积累一个私人脑库"服务的**。

这导致几个具体的规约差异：

1. **raw 的 source-reading 子目录** 是内容站特有——通用 LLM Wiki 没这个概念
2. **raw 里的 articles 摘录方式** 更激进——只留观点和数据，不留行文。这是因为我们的目标是反哺自己写的文章，不是建一个二手知识库
3. **raw 不存生成内容**——AI 生成的任何东西都不算 raw，只能进 wiki

<!-- EXPERIENCE-TODO: 等 raw/ 下真的长出 100+ 文件之后，写一段"我的 raw 实际分布"——哪个子目录最大、哪个几乎没人用、有哪些一开始想错了的目录后来删掉。 -->

## wiki 层在内容站里的核心差异：coverage-map.md

Karpathy 的 wiki 层强调 `index.md`（按内容组织）和 `log.md`（按时间组织）。本站在这两个之上**加了第三个特殊文件：`coverage-map.md`**。

为什么需要它？

LLM Wiki 的典型场景是"你读了几十份资料、积累一个脑库"。脑库本身就是目的，按 concept / entity 组织就够。

内容站的场景不一样。**我有 600+ 规划、14 个栏目、跨多个系列**。作者本人会忘记"GC 这件事我之前在哪几篇讲过、每篇侧重什么角度"。这种"作者自己的覆盖地图"是 LLM Wiki 默认没解决的——因为它假设 raw 是输入、wiki 是输出，没有"已经发布的文章"这一层。

本站的 coverage-map.md 解决的是：

```
每个 concept 在哪几篇已发布文章里被讲过？
每篇的侧重角度是什么？
哪些 concept 还没文章覆盖？
哪些 concept 被多篇重复讲、需要合并或差异化？
```

这是内容站作者的独特需求——不是 LLM Wiki 的设计盲区，是它根本不在那个场景里。

<!-- DATA-TODO: 等 coverage-map.md 真填到 50+ concept 之后，贴一份脱敏的样例段落——展示一个"被多篇重复覆盖"的 concept 的真实条目。 -->

## schema 层（kb/CLAUDE.md）跟 Karpathy 设计的不同

Karpathy 的 schema 是给 LLM 看的元指令——告诉它 wiki 怎么组织、ingest 流程是什么、lint 怎么跑。

本站 `kb/CLAUDE.md` 在这之上**多了一层职责**：明确划清 `kb/` 跟 `docs/` 的边界。

为什么需要这件事？

LLM Wiki 假设你只有一个知识库目录。Hugo 内容站不是——它至少有：

- `docs/`：**前置规划层**（写文章前用——series-plan、execution-plan、outline、workorder）
- `kb/`：**回顾沉淀层**（写文章后用——concept / entity / coverage-map）
- `content/`：**发布产物层**（实际渲染的文章）

这三层职责互相不重叠，但在 AI 视角下**它们都是 markdown 文件**。如果不显式划界，AI 会把：

- 系列规划写进 kb/wiki/（应该写 docs/）
- 概念页写进 docs/（应该写 kb/wiki/）
- 半成品文章写进 kb/raw/（应该写 content/ 草稿区）

本站 `kb/CLAUDE.md` 顶部就明确钉死这个三层关系。这是 Karpathy 原始 schema **没碰过**的设计——因为他不在多层目录的工程环境里。

```
docs/  = "我打算写什么"
kb/    = "我读懂了什么 / 我已经写过什么"
content/ = "我已经发布了什么"
```

这条规约本身就是"Karpathy 范式不够用"的证据之一。

## 为什么没采用 GBrain 那套

读完 LLM Wiki 自然会读到 GBrain（Y Combinator 的 Garry Tan 出的）。GBrain 在 LLM Wiki 之上加了：

- **向量数据库** 做混合检索（语义 + 关键词）
- **实体关系图谱**（基于规则的实体抽取 + back-link 强制化）
- **Skillify 概念** 把任何 markdown 当 Skill 加载
- **多模态支持**（视频 / 音频 / PDF 转录）

GBrain 在 240 页基准库上的检索精度提升 +31.4pp。听起来非常诱人。但我没采用——三个理由：

### 理由 1：规模没到

GBrain 的优势在数百页之上才显现。本站 wiki 目前 3 个文件（index / log / coverage-map），concepts 和 entities 还没填。**在这个规模下，向量检索没有优势——`index.md` + grep 就够**。

提前引入向量库会带来：

- 额外的依赖（嵌入模型、向量库本身）
- 索引同步成本（每次改 wiki 都要重建索引）
- 维护成本（嵌入模型版本升级、向量库 schema 变更）

这是典型的过度工程化。

### 理由 2：内容站的检索路径不一样

GBrain 的典型场景：用户问个问题 → 混合检索 → 渐进式披露给 AI → AI 综合答案。

内容站作者的"检索"是：**我现在要写一篇文章、找一下我以前写过哪些相关的**。这个动作的本质是**翻自己的 coverage-map.md**——按 concept 找已发布文章。不是按语义找原始资料。

`coverage-map.md` + grep 在这个场景下比向量检索更直接。

### 理由 3：图谱关系抽取在内容站场景下信噪比低

GBrain 的实体关系图谱在"商业场景"里特别好用——"张三投资了李四的公司"这种关系明确、价值高。

内容站作者写的概念之间的关系是另一回事："URP 14 的 RenderGraph 行为跟 URP 17 不一样"——这是版本相关、上下文敏感的关系，规则抽取很难做对，强行做容易产生噪音。

**这是我目前的判断，不是绝对的——等 kb/wiki/ 长到 100+ 页之后可能要重新评估**。当前阶段不上 GBrain 是正确选择，未来不一定。

<!-- DATA-TODO: 等 kb/wiki/ 长到 100+ 页时，做一次"是否该引入向量检索"的评估，把决策回填进这一节。 -->

## 三大操作（Ingest / Query / Lint）在内容站的形态

Karpathy 的 LLM Wiki 给了三个标准操作。它们在内容站的具体形态：

### Ingest：把一份新 raw 处理进 wiki

Karpathy 示例：你 web-clip 了一篇文章 → AI 读 → 摘要进 wiki → 更新相关 entity / concept 页面 → 写 log。

本站适配：

- raw 多数来源不是 web-clip，是源码笔记 / 论文摘录 / 旧文章片段
- ingest 时**额外要做的事**：更新 `coverage-map.md`——这个 ingest 进来的 concept 在已发布文章里被讲过吗？哪几篇？侧重什么？
- 不写 sources/ 摘要的简化版本——内容站 raw 通常已经是摘录形式，不需要二次摘要

### Query：用 wiki 回答问题

Karpathy 示例：你问个问题 → AI 查 index → 读相关页 → 综合答案带引用。

本站适配：

- 主要 query 不是"回答问题"，是"我现在写文章、查一下相关 concept 我以前怎么讲的"
- 这个 query 的入口是 `coverage-map.md`——按 concept 找已发布文章和侧重
- 答案会落进 `queries/`——这一步跟 Karpathy 一样

### Lint：定期 wiki 健康检查

Karpathy 示例：找矛盾、找过期、找孤儿页、补缺失交叉引用。

本站适配：

- 找矛盾：本站特有的"同一个 concept 在不同 entity 页里讲法不一致"
- **找漂移**：concept 页里讲的 Unity API 行为，跟最新版本不一致了吗？这是内容站特有的——code-evolution 漂移
- 找重复覆盖：coverage-map 里某个 concept 被 5 篇文章重复讲、需要合并或差异化吗？

第二项是内容站独有的——通用 LLM Wiki 不假设知识有版本敏感性，但游戏引擎 / 客户端的 concept 几乎都跟版本绑死。

<!-- EXPERIENCE-TODO: 等 wiki/ 长到 50+ 页后，跑一次完整 lint，把 lint 报告脱敏版回填——展示真实 lint 发现的问题类型分布。 -->

## 总结一下"原样能用 vs 必须改"

| LLM Wiki 元素 | 内容站适配 |
|--------------|-----------|
| 三层架构 | **原样能用** |
| raw 不可变 / wiki 由 AI 维护 / schema 是元指令 | **原样能用** |
| index.md + log.md 两个特殊文件 | **原样能用** |
| concepts/ + entities/ 分类 | **原样能用** |
| raw 子目录命名 | **要改**——加 `source-reading/` |
| wiki 特殊文件 | **要加**——`coverage-map.md` |
| schema 职责 | **要扩**——加上 docs / kb / content 三层界 |
| Obsidian wikilink | **要换**——Hugo relref 形式 |
| 向量检索 / 图谱 | **不用**——规模没到、内容站需求不同 |
| Ingest 流程 | **要简化**——raw 多数已经是摘录 |
| Query 入口 | **要扩**——coverage-map 作主入口 |
| Lint 项目 | **要加**——版本漂移检测 |

这张表是这篇文章的核心结论。**不是"LLM Wiki 不好"，是"它的默认配置假设了一种场景、内容站是另一种"**。

## 收束

Karpathy 的 LLM Wiki 范式给了一个清晰的骨架：**raw 不可变、wiki AI 维护、schema 做元指令、三大操作维持健康**。这个骨架在内容站场景下原样能用——但需要在 raw 分类、wiki 特殊文件、schema 职责上做内容站特有的扩展。

GBrain 那套向量混合检索 + 图谱关系，在当前规模下不需要——以后规模上来可能要重新评估。

**最重要的一条规约**——`kb/` 跟 `docs/` 的边界——是 Karpathy 范式没解决的、内容站独有的难题。把它写进 schema 钉死，是这套适配成立的关键。

最短结论是：

**别整个抄 LLM Wiki。抄三层架构、抄 ingest / query / lint，但 raw 放什么、wiki 加什么特殊文件、schema 怎么扩——必须按你的场景重新想。**

回到本系列开头——[01 团队知识闭环]({{< relref "ai-empowerment/ai-empowerment-01-team-knowledge-closed-loop.md" >}}) 讲的 Dify + LKB + Wiki 闭环、[04 知识缺口回流]({{< relref "ai-empowerment/ai-empowerment-04-knowledge-gap-feedback.md" >}}) 讲的缺口反哺、本篇讲的 kb/ 二层架构——其实是三个抽象层的同一件事：**让 AI 工程化的知识能沉淀、能检索、能闭环**。

到这里，AI 赋能游戏开发系列的实践三切面（09 SDD / 10 Skill 沉淀 / 11 kb / LLM Wiki）就走完了。SDD 解决"任务输入怎么定"、Skill 沉淀解决"单次任务怎么学到东西"、本篇解决"长期知识怎么组织"——三件事拼起来，是 AI 工程化的最小可用闭环。

<!-- DATA-TODO: 全篇大量 TODO。要写到能发布的状态，必须：(1) kb/wiki/ 真长出 50+ 页 (2) 至少跑过 3 次 ingest 流程 (3) 至少跑过 1 次 lint 并产生有意义的报告 (4) coverage-map.md 至少填到 50 个 concept。当前发布是"范式对照可用、实际数据缺失"。 -->

<!-- EXPERIENCE-TODO: 跟 Karpathy LLM Wiki 的对照表是否真的成立——需要在 wiki/ 真长起来之后回头验证。有些差异点可能是我现在想偏了，规模上来才能验证。 -->
