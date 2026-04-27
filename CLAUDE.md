# TechStackShow 项目规范

## 项目定位

Hugo 静态站点，中文技术专栏。部署在 GitHub Pages (`twoeggdu.github.io/twoegg-tech-stack/`)。

- **作者**：游戏引擎 / 客户端方向工程师，覆盖 Unity / Unreal、渲染、构建流水线、性能、工具链
- **隐含目标**：通过技术深度作品集做求职背书（资深架构 / Tech Lead 方向）
- **风格基调**：内容站感为先，技术决策与工程判断为主线；**不做露骨自我推销**，能力靠文章质量隐式传达
- **优先栏目**：交付工程、长线运营、问题解决、技术决策——比纯技术深挖优先级更高

## 内容架构

### 三层规划文档（不可混淆）

```
doc-plan.md                          ← 系列级路由（不放篇级）
└─ docs/<topic>-series-plan.md       ← 单系列 canonical 篇级目录
   └─ docs/<topic>-execution-plan.md / -outline.md / -workorders.md   ← 执行层
      └─ content/<section>/<slug>.md ← 实际发布的文章
```

- 根 `doc-plan.md` 只放系列级路由表，**不要回填篇级清单**
- 一个系列只能有一个 canonical `*-series-plan.md`
- 执行层（execution / outline / workorders）不反向覆盖系列 plan
- 历史篇级总表已迁到 [docs/doc-plan-archive-v26.md](./docs/doc-plan-archive-v26.md)，**只读不更新**
- 文章写完后，先回写系列 plan，再考虑动 doc-plan

### content/ 栏目分工

| 栏目 | 主题 |
|------|------|
| `rendering/` | 渲染管线、URP、Shader、变体 |
| `et-framework/` + `et-framework-prerequisites/` | ET 服务端框架与前置 |
| `delivery-engineering/` | 多端交付闭环 |
| `live-ops-engineering/` | 长线运营工程 |
| `engine-toolchain/` | 引擎工具链 |
| `performance/` | 游戏性能 |
| `problem-solving/` | 问题解决案例 |
| `system-design/` | 系统设计与架构 |
| `code-quality/` | 代码质量到工程质量 |
| `ai-empowerment/` | AI 赋能 |
| `projects/` | 项目级叙事 |
| `essays/` | 杂文 |

## 双仓工作流（源码级深挖文章）

| 仓库 | 范围 | 写作自由度 |
|------|------|-----------|
| `GameEngineDev`（私仓） | Unity C++ 源码、struct 定义、函数体、内部注释 | 完全自由 |
| `twoegg-tech-stack`（本仓，公开） | 行为描述 + 实验验证，不直接引 C++ 源码 | 受限 |

**理由**：Unity C++ 引擎源码受 license 限制。公仓文章用"观测行为 + Profiler/工具实验"代替"我读了源码"。URP / SBP / Addressables 这类 Package 级代码是公开的，可以直接引用。

**操作约定**：

- 用户让 AI 写源码深挖文章时，先问"目标仓库是哪个"
- 私仓 → 公仓同步时：剥离 C++ 源码引用，保留结论与行为描述；保留函数名 / 文件路径作为事实指引
- 不要把私仓文件直接复制到公仓 content/

## AI 协作要点

进入仓库工作前请记住：

1. **三层规划文档**：见上，doc-plan / series-plan / execution-plan / content/ 各司其职
2. **YAML 引号 / Hugo shortcode 引号**：见下文「文章格式规范」——这是最高频构建失败原因
3. **weight 子组规约**：见下文，URP 等系列有固定子组范围
4. **写完先 hugo 本地构建**：`ERROR` 为零才能提交（`REF_NOT_FOUND` 指向计划中未写文章不算）
5. **私仓内容隔离**：不要把 GameEngineDev 的 C++ 源码引用直接搬进本仓
6. **kb/ 是知识沉淀层**：与 docs/ 的"前置规划"职责不同，详见 [kb/CLAUDE.md](./kb/CLAUDE.md)

## 文章格式规范

### Frontmatter（YAML）

每篇文章以 `---` 包裹的 YAML frontmatter 开头。

**引号规则（最重要，违反会导致整站构建失败）：**

- title / description / series_intro / series_summary 等字符串值，如果**内容包含中文引号**（`"` `"`）或英文引号，**外层必须用单引号包裹**
- 不包含引号的普通值用英文双引号即可

```yaml
# ✅ 正确：内容有中文引号，外层用单引号
title: '交付总论 01｜发布 vs 交付——为什么"包能打出来"不等于"产品能上线"'

# ✅ 正确：内容没有引号，双引号正常
title: "URP 深度前置 01｜CommandBuffer：Blit、SetRenderTarget、DrawRenderers"

# ❌ 错误：内容的中文引号和外层双引号冲突，YAML 解析失败
title: "为什么"包能打出来"不等于"产品能上线""
```

**标准 frontmatter 模板：**

```yaml
---
title: "文章标题"
slug: "article-slug"
date: "2026-04-14"
description: "一句话描述"
tags:
  - "Tag1"
  - "Tag2"
series: "系列名"
weight: 1500
---
```

**系列索引页额外字段：**

```yaml
series_id: "series-slug"
series_role: "index"
series_order: 0
series_entry: true
series_audience:
  - "目标读者1"
series_level: "进阶"
series_best_for: "一句话场景描述"
series_summary: "系列概要"
series_intro: "系列介绍"
series_reading_hint: "阅读建议"
```

### Hugo Shortcodes

站内链接使用 `relref` shortcode：

```markdown
[文章标题]({{< relref "section/article-slug.md" >}})
```

**shortcode 内的参数必须用英文双引号（ASCII `"`），绝对不能用中文引号（`"` `"`）。**

```markdown
<!-- ✅ 正确 -->
{{< relref "rendering/urp-config-01-pipeline-asset.md" >}}

<!-- ❌ 错误：中文引号会导致构建失败 -->
{{< relref "rendering/urp-config-01-pipeline-asset.md" >}}
```

### 文章正文结构

**前置知识卡片**（可选，放在 frontmatter 之后、正文第一段之前）：

```markdown
> **读这篇之前**：本篇会用到 XX、YY 概念。如果不熟悉，建议先看：
> - [文章标题]({{< relref "path/to/article.md" >}})
```

**版本说明**（URP 等有版本敏感性的文章）：

```markdown
> 版本说明：本篇基于 Unity 2022.3 LTS（URP 14）。Unity 6 差异见 [相关文章]。
```

**动手验证段落**（配置类文章推荐加）：

```markdown
### 动手验证：参数名

1. 步骤一
2. 步骤二
3. 观察结果
```

### Weight 编号约定

URP 深度系列 weight 范围：

| 子组 | Weight 范围 |
|------|------------|
| 索引 | 1490 |
| 入门与速查 | 1492-1494 |
| 前置基础 | 1500-1520 |
| 管线配置 | 1530-1550 |
| 光照与阴影 | 1560-1584 |
| 扩展开发 | 1590-1648 |
| 平台与优化 | 1650-1710 |

同子组内按 10 递增，插入时用 5 或 2 递增。

## 构建验证

**每次提交前必须本地构建验证：**

```bash
hugo
```

- `ERROR` 为零才可提交（`REF_NOT_FOUND` 指向计划中未写的文章不算）
- 重点检查 YAML 解析错误和 shortcode 引号错误

## 待补数据标记

文章中需要作者补充真实数据的位置用 HTML 注释标记：

```markdown
<!-- DATA-TODO: 描述需要什么数据、测试设备、截图存放路径 -->
<!-- EXPERIENCE-TODO: 描述需要什么项目经验叙事框架 -->
```

搜索 `DATA-TODO` 或 `EXPERIENCE-TODO` 可以找到所有待补位置。

## Git 提交规范

- 新文章：`Add {series-prefix}-{number}: {short title}`
- 修改文章：描述改了什么
- 批量修复：描述修复了什么问题和影响范围
