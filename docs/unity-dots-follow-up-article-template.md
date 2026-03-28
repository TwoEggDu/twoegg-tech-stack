# Unity DOTS 后续专题正文模板

> 这份模板服务于 `docs/unity-dots-follow-up-series-plan.md` 对应的 20 篇后续专题。目标不是套壳，而是把并行起草时最容易漂的结构、命名和 front matter 先钉住。

---

## 编号与排序约定

| 系列 | 编号前缀 | `series_id` | 索引页 `weight` | 正文 `weight` 规则 |
|------|----------|-------------|-----------------|--------------------|
| Unity DOTS Physics | `DOTS-Pxx` | `unity-dots-physics` | `2100` | `2100 + series_order` |
| Unity DOTS 项目落地与迁移 | `DOTS-Mxx` | `unity-dots-project-migration` | `2200` | `2200 + series_order` |
| Unity DOTS NetCode | `DOTS-Nxx` | `unity-dots-netcode` | `2300` | `2300 + series_order` |

> `series_order` 决定系列内部顺序；`weight` 只是兜底与列表排序辅助，不承担系列内阅读顺序。

---

## 标题样式

- `Unity DOTS P01｜标题`
- `Unity DOTS M01｜标题`
- `Unity DOTS N01｜标题`

标题第一段负责标识系列和编号，冒号后的句子必须明确这篇回答的工程问题，而不是只写 API 名。

---

## Front Matter 模板

```yaml
---
title: "Unity DOTS P01｜标题"
slug: "dots-p01-slug"
date: "2026-03-28"
description: "先说清这篇解决什么工程问题，再说本文覆盖的机制、边界和不展开的部分。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Physics"
series: "Unity DOTS Physics"
primary_series: "unity-dots-physics"
series_role: "article"
series_order: 1
weight: 2101
---
```

必填项：

- `title`
- `slug`
- `date`
- `description`
- `tags`
- `series`
- `primary_series`
- `series_role`
- `series_order`
- `weight`

---

## 正文主模板

```md
开场第 1 段：问题现象或工程痛点。
开场第 2 段：本文要回答什么，不回答什么。

---

## 问题入口：为什么这件事会卡住
用一个具体场景把问题钉住，不先讲 API。

## 核心机制 / 世界模型
讲清数据怎么组织、系统怎么执行、约束从哪里来。
优先放图、表、内存布局、执行链。

## 标准写法 / 最小实现
放一段能跑通或足够完整的代码。

## 代价、限制与边界
明确这套方案贵在哪里、限制在哪里、为什么不能乱用。

## 什么时候该用，什么时候不该用
给出场景判断，不只给正向推荐。

## 常见踩坑 / 误用 / 排障入口
列 3~5 个最常见错误，告诉读者先看哪里。

## 小结
用 3 条以内结论收束，不重复全文。

下一篇 / 后续阅读：
一句话说明它和下一篇或前文的关系。
```

---

## 工具 / 调试篇变体

`DOTS-P07`、`DOTS-N07` 这类文章允许换成下面的中段结构：

```md
## 工具 / 入口一
## 工具 / 入口二
## 常见症状速查
## 诊断顺序
## 小结
```

但仍然必须保留：

- 开场两段
- 问题入口
- 小结
- 下一篇 / 后续阅读

---

## 强制统一的写法

- 一级结构统一使用**非编号式 H2**，不要混用 `## 1.`、`## 一、` 和纯描述句。
- 每篇都必须有：
  - 1 个问题入口
  - 1 段核心机制解释
  - 1 段完整代码或伪代码
  - 1 段代价 / 边界
  - 1 个 `## 小结`
  - 1 段下一篇导读
- `小结` 统一成 `3 条以内结论 + 1 句前向导读`。

---

## 开写前检查

- 这篇的唯一职责是否已经写成一句话。
- 相邻文章的重复风险是否已列清。
- 是否已经写清“本文不展开什么”。
- 是否已经明确前置知识和验证环境。
- 是否已经对照 `docs/unity-dots-follow-up-shared-glossary.md` 统一术语。

---

## 发文前检查

- `primary_series / series_role / series_order / weight` 是否完整。
- 标题、摘要、正文术语是否和共享术语表一致。
- 文末导读是否指向正确的下一篇。
- 如果是版本敏感题目，是否已经写明验证环境与 API 锚点。
- 是否已通过 `hugo --gc --minify` 本地构建。
