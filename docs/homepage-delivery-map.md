# 首页与二级主题页

## 当前阅读层级

首页 → 技术主题 → 专栏／系列 → 原有文章。每层保留直达文章的入口。

首页只保留站点方向、首篇推荐与交付地图。不放作者介绍，不展开个人实践列表、六主题重复目录或多工种协作列表。作者介绍留给后续独立页面；当前页脚保留已有的关于作者入口。

首页采用已选定的 B 地图结构与 C 蓝灰配色，顶部五个主题是主导航。交付地图的四个目标整块可点，文字入口带下划线和键盘焦点。

## 五个二级首页

| 主题 | 地址 | 阅读起点与系列范围 |
| --- | --- | --- |
| 工程与交付 | `/reading/delivery/` | 从资源漏提交实践进入；组织生产管线、CI/CD、资源管线，并承接六组跨工种协作。发布、回退和 A/B 作为文章入口。 |
| GamePlay | `/reading/gameplay/` | 以可玩性与游戏设计为主线，从阅读地图进入 24 篇章节提纲；正文与真实案例待补，技术实现跳转全栈框架。 |
| 全栈框架 | `/reading/frameworks/` | 解释全栈视角与学习产出，连接全栈、客户端、服务端三个三级首页。 |
| 引擎研发 | `/reading/engine/` | 从引擎分层总论进入；组织引擎架构、资产系统与序列化。 |
| 性能优化 | `/reading/performance/` | 从性能判断进入；选择加载、硬件、内存、分档、预算与持续回归。基础入口按需展开。 |

一般二级首页使用“主题说明 → 推荐一篇 → 选择系列 → 直达文章”。工程与交付增加协作索引；性能保留按需展开的基础入口。全栈框架改为“价值与目标 → 三条路径 → 文章／实验／Demo 产出 → 引擎范围”，再由三级首页提供系列入口。完整技术目录继续负责全面检索。

2026-10-31：工程与交付以 **游戏研发 FDE：从技术机制到产品交付** 为推荐起点，沿用[原专栏首页](../content/delivery-engineering/fde-methods-series-index.md)。入口连接 Missing／AOT、两篇方法正文与四层精选技术深读；客户端生产体系是第一组代表实践。目录关系与状态由[FDE 规划](fde-methods-series-plan.md#fde-flagship)及原案例／技术系列各自维护，本页不复制篇级表。本轮为本地入口修订，未发布。

### 全栈框架的三级首页

| 名称 | 地址 | 职责 |
| --- | --- | --- |
| 全栈框架 | `/reading/fullstack/` | 整体运行模型、两端契约、同步、配置与总结 Demo |
| 客户端框架 | `/reading/client/` | 执行与异步、资源／场景／UI、热更新、小游戏与引擎路线 |
| 服务端框架 | `/reading/server/` | 会话、Actor、地图、业务数据、持久化与运行恢复 |

保留原 client／server 地址。三级页通过面包屑回到总入口，并互相切换；顶部统一高亮“全栈框架”。Unity、Unreal、通用原理与跨引擎对照是适用范围，不增加固定层级。现有 ET9 专栏与 ET10 研究分开标记；Luban 专题、总结 Demo、同步实验与小游戏真机验证按待完成状态展示。

学习目标、产出与完成条件见 [全栈框架学习提纲](fullstack-framework-learning-outline.md)。本页记录导航职责，不另行维护 canonical 篇目清单。

`/reading/startup-loading/` 继续承担系列页职责：安排七篇主线、按症状跳读、说明实践与验证边界。各主题不复制这一层的长阅读顺序。

## 文件与维护

- `layouts/index.html`、`layouts/partials/reading/home.html`：正式首页。
- `content/reading/*.md`：主题与精选系列的路由、标题和说明。
- `data/reading_paths.yaml`：五主题导航、推荐起点、系列和文章捷径的共用数据。
- `data/framework_reading.yaml`：全栈总入口、三个三级首页与状态标记。
- `layouts/partials/reading/topic.html`：一般主题共用模板。
- `layouts/partials/reading/framework.html`、`static/css/framework-reading.css`：四个框架阅读页专用展示与样式。
- `layouts/partials/reading/guide.html`：启动与加载精选阅读页。
- `layouts/partials/reading/document.html`：页框架、SEO、导航、搜索与页脚。
- `static/css/reading.css`、`delivery-home.css`：只由首页和导读页面加载。

既有文章与栏目地址不变，正文不复制；导读不加入文章列表、搜索索引和 RSS。预览方案保留在 `previews/reading-experience/`，普通构建不挂载。

## 既有首页检查记录

- 普通 Hugo 构建 1298 页，零 ERROR / WARN。
- 首页、六主题与启动加载页的构建链接、锚点，以及本机 HTTP 目标均检查。
- 浏览器验证页签、当前主题状态和文章跳转；320、768、1280px 检查无横向溢出，未做实体手机实测。
- 同为 1280px 宽度，首页页面高度由约 3247px 减至 1125px，减少约 65%。高度会随字体与浏览器变化。
- 以上为本地构建与浏览器验证，不代表线上部署验收。

本地首页：`http://localhost:13149/`。主题路径与表中一致。

2026-10-23 全栈入口调整的验证以本轮实施计划中的实际记录为准；以上六主题检查是调整前的历史记录。
