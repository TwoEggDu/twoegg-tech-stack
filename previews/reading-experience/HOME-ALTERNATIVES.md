# 首页版式对比

2026-10-23：在当前预览之外新增三个完整首页，使用相同内容数据和文章地址。正式首页、正文、主题页与系列页未改动，无新依赖、提交或部署。

| 版本 | 地址 | 主要差异 | 评审重点 |
| --- | --- | --- | --- |
| 当前版 | http://localhost:13149/preview/ | 行式目标与协作列表 | 作为内容与信息密度的对照 |
| A · 杂志 | http://localhost:13149/preview/editorial/ | 暖白与深绿首读、大标题、四列目标、不对称实践布局 | 首屏是否更有主次，是否仍像个人技术博客 |
| B · 交付地图 | http://localhost:13149/preview/atlas/ | 四个可点击目标围绕共同交付对象，协作提前，技术主题成对排列 | 是否容易理解整体关系，地图是否有助于选入口 |
| C · 阅读手册 | http://localhost:13149/preview/reading/ | 蓝灰色、连续阅读列、桌面侧边目录、逐条问题导读 | 是否更适合开始阅读，页面长度是否可以接受 |

三版均保留四个交付目标、六个技术主题、六组协作、三篇个人实践、实际搜索及完整目录入口。首页不出现 YooAsset / Addressables，进入资源管线系列后再阅读具体方案。三版为有区别的编排，不强制读者按顺序点击；所有目录和地图节点均为实际链接或锚点。

## 文件

- `content/preview/editorial.md`、`atlas.md`、`reading.md`：独立路由。
- `layouts/partials/reading-preview/home-variants.html`：三种首屏和章节顺序。
- 同目录 `variant-*.html`：共享目标、协作、实践、主题及版本切换；读取现有 `reading_preview.yaml`。
- `static/css/home-variants.css`：仅首页加载，三版按 body class 隔离。
- `layouts/reading-preview/baseof.html` 与 `page.html`：首页分流及切换导航。当前版仅增加对比导航。
- `check_links.py`：自动检查所有预览路由，覆盖新增页面。

## 本轮验证

- 预览与普通 Hugo 构建成功，零 ERROR / WARN。
- 六页共 337 个静态链接／资源引用、60 个不同目标、43 个锚点均有效；本机 61 个 HTTP 目标全部成功。搜索索引没有混入预览页面。
- 浏览器实际切换 A / B / C，检查 C 首读到原文、B 目标到性能主题、本页锚点和搜索结果；Escape 关闭搜索。
- 三版在 320、390、768、1024、1280px 下无横向溢出，均含六个主题与六组协作。保存桌面与手机截图；没有实体手机或跨浏览器实测。
- 普通构建原有文件与初始基线哈希一致，不产生预览页面；原有已跟踪文件哈希未变。
- 检查结束已恢复浏览器原视口。

## 截图

- [A 桌面](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-a-desktop.jpg) · [A 手机](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-editorial-mobile.jpg)
- [B 桌面](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-b-desktop.jpg) · [B 手机](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-atlas-mobile.jpg)
- [C 桌面](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-c-desktop.jpg) · [C 手机](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-reading-mobile.jpg)

## 选定方向

用户选择 B 的结构与 C 的颜色。已直接更新 `/preview/atlas/`：采用 C 的暖白背景、蓝灰正文、蓝色链接与浅蓝灰底色；地图连接线、中心节点、首读条与实践分隔线统一为同色系。B 的标题、地图、章节顺序和全部链接保留。
