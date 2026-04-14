# TechStackShow 项目规范

## 项目概述

Hugo 静态站点，中文技术专栏。部署在 GitHub Pages (`twoeggdu.github.io/twoegg-tech-stack/`)。

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
