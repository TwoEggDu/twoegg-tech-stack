# 性能优化分层阅读首页 Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan in the current session. Steps use checkbox syntax for tracking.

**Goal:** 搭好性能优化二级首页及 Unity、Unreal 三级首页，复用现有正文，并明确阅读路线的内容缺口。

**Architecture:** 保留现有 `/reading/performance/`。新增两份平铺的阅读页，以显式 URL 输出到 `/reading/performance/unity/` 和 `/reading/performance/unreal/`，避免与已有主题页形成 Hugo section 路由冲突。入口使用现有阅读样式和严格的站内链接解析，正文继续归属原系列。

**Tech Stack:** Hugo、Go templates、Markdown、CSS。

**Spec:** 用户在本次对话确认的“性能优化二级首页 → Unity / Unreal 三级首页”结构及七模块要求。

**Files:** `content/reading/performance.md`、`content/reading/unity-performance.md`、`content/reading/unreal-performance.md`；`layouts/partials/reading/topic.html`、`layouts/partials/reading/performance-hub.html`、`layouts/partials/reading/performance-engine.html`；`static/css/reading.css`；本执行记录。

## Global Constraints

- 本轮采用明确文件白名单；保留工作区其他未提交改动，不提交或发布。
- 不移动原文，不改已有文章 URL，不新建重复的篇级规划。
- Unity 按已读取的 UWA 七模块组织；仅参考其主题，不复制正文或沿用未经核对的参数建议。
- 已有正文、部分覆盖、待补专篇与待补实验分别表述；缺口不创建虚假文章链接。
- 本轮是阅读入口与静态内容调整，以 Hugo 全站构建、链接检查与桌面/移动页面检查验证，不增加重复实现的单元测试。

## Review Focus

- 三页的 URL 和面包屑应体现同一层级，且站点前缀路径正确。
- Unity 七模块均有阅读材料与明确缺口，不把相邻文章描述为完整覆盖。
- Unreal 复用四篇现有性能文章，单列其版本核对和专篇缺口。
- 新入口必须能从二级首页进入，两个三级首页能返回上级并切换引擎。
- 非性能主题与原有启动加载页保持可达，移动端无横向溢出。

## Tasks

- [x] 盘点现有入口、UWA 七模块及候选文章；运行 Hugo 基线（1306 页，exit 0）。
- [x] 新建 Unity / Unreal 阅读页，逐模块匹配已有文章并列出待补内容。
- [x] 实现二级首页和通用引擎阅读模板，复用现有样式并补充窄屏布局。
- [x] Hugo 全站构建；检查三页所有站内链接与模块锚点；对照基线路由确认原页面可达。
- [x] 浏览器检查三个入口及窄屏布局；复核本轮白名单与构建结果。

## Execution Record

- 2026-10-23：用户已明确授权按确认结构直接实施，本会话内完成，不另行要求选择执行方式。
- 采用当前工作区文件白名单，原因是现有首页与未提交内容需要一起预览；不复制、重置或提交其他改动。
- 内容状态只在阅读导航中说明；这里不替代任何 canonical 系列 plan，也不登记新正文已完成。
- 实施时发现另一个会话正在扩展框架导航，故把性能页分派放入原本未被修改的 `reading/topic.html`，没有改动该会话的 `reading/document.html`、站点导航或共享数据。
- Hugo 全站构建 exit 0，生成 1310 页；相较基线新增路径包括本轮两个引擎页及其他会话的两个框架页。
- 三页 123 个站内文章链接与锚点检查通过；基线 1287 个 `index.html` 路径全部保留。
- Chrome 检查二级入口、引擎互切、面包屑与 Unity 模块锚点；桌面及 390px 视口下三页显示正常，窄屏页面内容宽度与可用宽度均为 375px，无横向溢出。
- Unity 正文恰有七个模块和七个待补区；Unreal 有四条已有正文主线及验证延伸，五个待补区。
- 按 requesting-code-review 技能完成一次独立只读审阅，未发现影响使用的问题；没有提交、推送或部署。
- 本地预览：`http://127.0.0.1:13150/twoegg-tech-stack/reading/performance/`；子页为 `unity/`、`unreal/`。截图保存在本会话的 visualizations 目录。
- 2026-10-23 后续调整：按用户截图移除二级首页四项页内导航，以及引擎入口上方的标题与说明；共同起点提前到引擎卡片之前。其他专题与三级首页保留。
- 2026-10-23 精简：用户认为二级首页内容过多，故仅保留共同起点的三个简短文章链接和两个引擎入口；移除下方问题索引、专题列表、基础折叠与完整目录入口，三级首页继续承接具体文章和缺口。
- 2026-10-23 内容主线调整：用户明确要求从玩家感受进入操作系统、硬件，最后到引擎。二级首页据此改为四层导读：先说明流畅、响应、等待、能耗、稳定与画质取舍，再分别精选系统和硬件阅读入口，保留两条引擎路线。该顺序是理解与阅读路径，不把操作系统、硬件和引擎误写成单向执行顺序；不恢复先前移除的完整专题目录。
