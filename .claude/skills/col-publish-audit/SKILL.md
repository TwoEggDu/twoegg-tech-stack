---
name: col-publish-audit
description: 系列发布对账器。当用户要求"对账"、"审计发布"、"plan vs content"、"系列进度核查"，或调用 /col-publish-audit 时使用。对照 doc-plan / 系列 plan 与 content/ 实际发布，输出漂移报告，不修改任何文件。
argument-hint: [可选：系列名 / 系列 plan 文件名，留空则全量审计]
allowed-tools: [Read, Glob, Grep]
---

# 系列发布对账器 — Plan ↔ Content 漂移审计

## 执行身份

你是专栏发布审计员。
任务是**只读对账**——找出"计划说有"和"content/ 实际有"之间的差异。
不写文章，不改 plan，不动任何文件。

用户输入：**$ARGUMENTS**（可为空 / 系列名 / plan 文件名）

---

## 与 col-next 的区别

- `col-next`：前瞻型——"下一篇该写哪个"，看的是依赖与就绪度
- `col-publish-audit`（本 skill）：回顾型——"已写的和计划的对得上吗"，看的是漂移

两者都读 doc-plan 与 content/，但视角不同。

---

## 执行步骤

### 第一步：确定审计范围

- 若 `$ARGUMENTS` 为空：全量审计——读根 `doc-plan.md` 系列路由表里的所有 canonical plan
- 若 `$ARGUMENTS` 指向具体系列：只审计该系列对应的 `docs/*-series-plan.md`

读取 `doc-plan.md`，提取系列路由表（"系列路由表"那一节）每行的：
- 主题名
- canonical 文件路径
- 状态（已拆出 / 暂存归档）

### 第二步：对每个 canonical plan 抽取声明清单

对每份 `docs/<topic>-series-plan.md`，提取：
- 每篇文章的编号、标题、计划 slug（如果 plan 里写了）
- plan 标记的状态（✅ 已完成 / 待写 / 草稿 / 暂缓）
- 计划归属的 `content/<section>/`（如果 plan 里写了）
- 计划 weight（如果 plan 里写了）

### 第三步：扫描 content/ 实际产物

用 Glob 扫描 `content/**/*.md`（跳过 `_index.md`），对每篇：
- 读 frontmatter 提取：`title` / `slug` / `series` / `weight` / `date`
- 用 `series` 字段把文章映射回系列

### 第四步：六类漂移检查

逐项对照，输出问题清单。

#### 漂移 1 · 计划 ✅ 但 content 缺失

plan 标"已完成"但 `content/` 里找不到对应 slug 或 title。
→ 标记 `[A. 状态存疑]`

#### 漂移 2 · content 已发布但 plan 未登记

`content/` 里某文章的 `series` 字段命中本系列，但 plan 篇级清单里没有它。
→ 标记 `[B. 未登记完成]`

#### 漂移 3 · content 文章 series 字段在 doc-plan 路由表里查不到

文章 `series:` 写了 X，但根 `doc-plan.md` 系列路由表 + 系列 plan 篇级标题里都没有 X。
→ 标记 `[C. 孤立 series]`

#### 漂移 4 · weight 越界

文章 weight 不在所属系列子组规约范围内（CLAUDE.md「Weight 编号约定」一节定义了 URP 等系列的子组范围）。
→ 标记 `[D. weight 越界]`
→ 若文章所属系列在 CLAUDE.md 没列子组范围，跳过此项

#### 漂移 5 · 同 weight 冲突

同一栏目下两篇文章 weight 完全相同。
→ 标记 `[E. weight 冲突]`

#### 漂移 6 · plan 引用的 docs/ 文件不存在

`doc-plan.md` 路由表 / 系列 plan 链接到的 `docs/<file>.md` 实际不存在。
→ 标记 `[F. 路由断链]`

---

## 输出格式

### 一、审计范围与样本量

```
审计时间：[YYYY-MM-DD]
范围：[全量 / 单系列：系列名]
样本：
  - 系列路由表登记的系列：[N] 个
  - 已拆出 canonical plan：[N] 份
  - 扫描到的 content/ 文章：[N] 篇
  - 系列字段命中已登记系列的文章：[N] 篇
```

### 二、漂移摘要

```
[A. 状态存疑]      [N] 处
[B. 未登记完成]    [N] 处
[C. 孤立 series]   [N] 处
[D. weight 越界]   [N] 处
[E. weight 冲突]   [N] 处
[F. 路由断链]      [N] 处
```

### 三、详细问题清单

每条一行，按漂移类型分组：

```
[标记] series=<系列> | content=<相对路径或"无">
plan 声明：<plan 文件 + 行号或编号>
content 实测：<frontmatter 摘要>
处理建议：<一句话，不超过 25 字>
```

### 四、结论与下一步

```
关键风险（必须处理）：[列 1-3 条]
建议清理（机械改动）：[列 1-3 条]
可观察暂缓：[列 1-3 条]
```

---

## 执行规范

- **只读，不改任何文件**——发现的问题只输出报告，让用户决定怎么改
- 如果一条 plan 没标状态，按"待写"处理，不算漂移
- 如果系列 plan 用模糊计数（"约 8 篇"）而不是逐篇列表，报告里注明 `[plan 未列篇级]` 并跳过该系列的 A/B 检查
- 文章 frontmatter 缺 `series` 字段不算漂移，但在第一节"样本"里单独计数报告
- 报告控制在 200 行内——超长时只列每类前 10 条，剩余写"另有 [N] 条同类，省略"
- 不评价文章质量，不评价命名美丑，只评价"对得上 / 对不上"

---

## 何时不该用本 skill

- 写新文章前——用 `col-next` 找下一篇
- 一致性审查（术语 / 版本 / 层级）——用 `col-consistency`
- 风险审稿——用 `col-risk`
- 单篇验证设计——用 `col-verify`
