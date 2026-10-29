# 默认阅读样式与发布检查实施计划

**Goal:** 普通文章默认使用 Piccolo 的阅读外观，创建入口和发布检查共同防止旧样式回退。

**Spec:** 本次用户已确认的要求：统一默认样式、创建模板、写作规范和发布检查。沿用已选定的 Piccolo 样式及“首页 / 主题 / 系列 / 篇目”导航。

**Architecture:** 普通文章与 `reading-series` 共用同一文档和正文模板。单独解析主题、系列与顺序；没有系列时仍使用相同阅读外观，导航返回主题或技术目录。显式系列模板的错误配置报错，历史未建立索引的文章不生成虚假的系列链接。专题落地页保持其专用布局。

**Tech Stack:** Hugo 0.157.0、现有 reading.css / series-reading.css、Python 3 标准库、GitHub Actions。

## 范围与约束

- 保留现有 URL、文章内容、首页和主题页结构；复用现有样式，不创建另一套皮肤。
- 系列顺序优先使用完整的 `series_order`；旧内容未完整编号时使用已有 weight 排序并按位置显示。
- 独立文章不虚构篇数；没有 `chapters` 锚点的旧索引返回索引页本身。
- 检查输出 HTML，而不只检查 frontmatter；覆盖缺省类型、系列页和具体导航目标。
- 当前工作区有无关改动，仅修改本计划涉及的模板、规范与验证文件。实现阶段仅做本地修改；用户已于 2026-10-29 后续明确授权与 FDE 修订一起提交推送。

## Review Focus

- 缺省类型且没有系列的文章，仍有新样式与有效返回路径。
- 旧文章仅有系列名称、缺少顺序或没有索引时，不产生错误编号或死链接。
- Piccolo 从 00 编号、YooAsset 的现有样式和运行时专题归属保持正确。
- 无目录的短文、没有 chapters 锚点的索引、子路径部署均可访问。
- CI 能识别旧样式、错误锚点和遗漏的系列配置；新建草稿不意外发布。

## Task 1：默认模板与导航

- [x] 增加 `tests/test_reading_layouts.py`，用最小 Hugo 站点验证缺省文章、系列章节与独立文章；先确认旧实现失败。
- [x] 增加共享 `reading/article-context.html`、`reading/topic-key.html`、`reading/article-document.html`；默认与系列入口均复用它们。
- [x] 扩展现有正文 partial 支持无系列、旧顺序和可选目录锚点，保持 Piccolo 的实际结构。
- [x] 运行测试与全站 Hugo 构建，并检查浏览器中的代表页面。

## Task 2：创建与发布防回退

- [x] 更新默认 archetype；新增系列章节和索引 archetype，并实测 Hugo 创建命令。
- [x] 增加 `scripts/check-reading-pages.py`，检查生成 HTML 的阅读外观、导航目标和锚点；加入负例验证。
- [x] 在发布工作流上传产物前运行回归测试与生成页检查。
- [x] 更新 AGENTS.md、文章写法规范、生产流程与创建说明，明确默认行为和验证命令。

## 验证记录

2026-10-29，本地工作区验证：

- `python -m unittest discover -s tests -p test_reading_layouts.py -v`：18 项通过。包含真实创建草稿、系列索引与章节、无 ID 旧索引隔离、重复编号和非整数篇号负例。
- `hugo --gc --minify --destination .tmp/reading-style-public`：成功，1428 页，0 ERROR。
- `python scripts/check-reading-pages.py --public .tmp/reading-style-public`：检查 1362 个阅读页，0 错误。
- 浏览器检查：CoreCLR 普通文章与 about 独立页面采用共用阅读外观；1280 像素视口的侧栏正常；Piccolo 保留 `00 / 27` 和原有系列入口；390 像素视口中 FDE 的面包屑、正文与折叠目录正常，目录可以展开。系列返回链接到达 FDE 索引；检查后已恢复默认视口。
- 独立只读审阅发现三项系列元数据问题，已修复并先验证失败、再运行通过：空 ID 不得匹配所有无归属文章；严格章节依附普通索引时仍检查重复顺序；直接作为篇号的顺序必须为非负整数。
- 兼容性决定：YooAsset 已有 `reading_number_by_position: true`，小数仅用于插入排序，继续保留；显示编号按位置取整数，另有回归测试覆盖。
- GitHub Actions 已配置发布前测试与生成页检查。本次未提交、推送或触发线上部署；上述全站结果包含当前工作区已有内容，不等于独立提交候选或生产站点验收。
