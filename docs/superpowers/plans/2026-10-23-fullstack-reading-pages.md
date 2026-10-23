# 全栈框架阅读入口实施计划

> 执行方式：在当前会话逐项实施并验证。用户已经确认信息架构并授权准备页面；不再重复发起设计确认。使用 executing-plans 的连续执行与最终独立审阅方式。

**目标：** 将客户端／服务端两个顶层入口合并为全栈框架二级首页，提供全栈、客户端、服务端三个三级首页，并写明学习产出提纲。

**架构：** 延用 Hugo 阅读页框架与蓝灰视觉。专用数据与 partial 组织新入口；既有正文和专栏索引保留原地址。Unity／Unreal 作为适用范围，不增加固定层级。

**技术：** Hugo、YAML、Go template、CSS。

**设计依据：** 本轮已确认的讨论，以及 `../../fullstack-framework-learning-outline.md`。

## 范围与决定

- 二级入口 `/reading/frameworks/`；三级入口 `/reading/fullstack/`、`/reading/client/`、`/reading/server/`。
- 二级与全栈三级入口均显示“全栈框架”，用面包屑、说明和职责区分。
- 复用已有专栏。新专题显示待写／待验证；ET9 文章与 ET10 实验分开表述。
- 不放私仓地址、私有源码、原始日志，不声称 Demo、帧同步或小游戏已完成。
- 仅编辑以下文件，不处理现有无关改动；不提交、推送或部署。
- 当前工作区为共享脏工作区，本轮采用精确文件范围，不切分支或覆盖其他工作。

## 文件与任务

- [x] 提纲：新增 `docs/fullstack-framework-learning-outline.md`，按问题、产出、依据与完成条件组织，引用既有 canonical 系列计划。
- [x] 数据与内容：新增 `data/framework_reading.yaml`、`content/reading/frameworks.md`、`content/reading/fullstack.md`；修改 `content/reading/client.md`、`content/reading/server.md`、`data/reading_paths.yaml`。
- [x] 展示：新增 `layouts/partials/reading/framework.html`、`static/css/framework-reading.css`；调整 `layouts/partials/reading/document.html` 与 `layouts/partials/site-masthead.html`。
- [x] 维护说明：更新 `docs/homepage-delivery-map.md` 的层级和路径说明，不建立第二套篇目状态表。
- [x] 验证：运行 Hugo；检查五项顶层导航、四页链接与锚点、三级面包屑和当前导航；检查窄屏展示；最终审阅精确 diff。

## 审阅重点

1. 子页和 ET／异步文章应高亮全栈入口，旧客户端与服务端地址仍可访问。
2. 所有阅读链接必须实际存在且非 draft；规划卡片不能伪装为已完成正文。
3. 重复页名不造成面包屑语义混淆；三级首页能回到总入口并切换路线。
4. 新样式只在四个框架阅读页加载，不影响其他首页／正文。
5. 构建与页面验收不被表述为私有 Demo、真机或线上发布验收。

## 验证记录

2026-10-23 本地验证：

- Hugo v0.158.0 最终构建退出码 0，生成 1310 页，输出无 ERROR；产物、缓存和资源目录均位于 `.codex_tmp/`。
- 静态检查首页及四个入口页：顶层导航均为五项，本地链接目标和锚点有效；提纲引用的 Markdown 文档均存在。
- 三级页面包屑、回到总入口和路线切换已在浏览器检查；ET、异步及资源选型文章的全栈导航高亮已检查。
- 浏览器直接预览宽度约 508px，并通过 320px／1280px 内嵌页面观察响应式布局；320px 总入口换行正常。未执行全设备矩阵。
- 独立只读审阅发现 Addressables／YooAsset 对比系列的导航归属遗漏，已补充并重新构建、验证高亮。
- 本轮修改的既有文件通过 `git diff --check`，无空白错误；未提交、推送或发布。页面验证不代表私有 Demo、同步实验或小游戏验收。

提交前补充：按用户后续要求移除二级首页面包屑与“技术主题 · 总览”；2026-10-23 当前共享工作区再次构建退出码 0，生成 1345 页、52 个静态文件，输出无 ERROR。用户随后授权仅提交并推送本轮页面、导航和提纲文件；此前“不提交”记录对应页面准备阶段。
