# 引擎研发与三个引擎首页实施计划

**目标：** 落地用户已确认的引擎研发二级首页，以及 Unity、Unreal、Piccolo 三级首页。

**设计依据：** 本会话已确认的完整文案与 Piccolo 六栏结构。二级首页直接展示公共专题，不增加公共专题汇总层。

**架构：** 四页使用 Markdown、站内 relref 与独立 reading-guide layout，复用站点导航和现有阅读页样式。入口目录采用页面内目录；三级页可返回二级页并切换引擎。

**技术：** Hugo、Markdown、Go template、CSS。

## 范围与决定

- 二级入口保留 `/reading/engine/`。
- 三级入口为 `/reading/engine/unity/`、`/reading/engine/unreal/`、`/reading/engine/piccolo/`。
- Unity 八栏、Unreal 七栏、Piccolo 六栏；Piccolo 明确标为专题筹备中。
- 草稿只显示名称与状态，不生成指向默认构建中不存在页面的链接。
- Unity 更新发布的七篇提纲、UE 工程实践待验证、性能待补内容分别说明。
- Piccolo 前置文章与官方资料可跳转；本轮拟题标为待写，不冒充已有正式篇目或提纲。
- 共享脏工作区使用精确文件范围；独立 layout 避开正在修改的公共 document/topic 模板。不切分支，不提交、推送或部署。
- 不改原文章、系列目录与 canonical 篇级规划；首页只路由现有内容。

## 文件

- 修改 `content/reading/engine.md`。
- 新增 `content/reading/unity-engine.md`、`unreal-engine.md`、`piccolo-engine.md`。
- 新增 `layouts/reading-guide/engine-directory.html`、`static/css/engine-directory.css`。
- 仅更新 `data/reading_paths.yaml` 的 engine 条目，让首页主题入口露出三个引擎。
- 本文件记录实施范围和验证结果，不承担文章规划状态表。

## 任务

- [x] 四页内容：写入已确认文案，内部链接统一用 relref；不开放草稿和待写条目的跳转。
- [x] 阅读展示：独立 layout、三级面包屑、引擎切换、页内目录和窄屏样式。
- [x] 接入：精确替换 reading_paths 的 engine 分组，保留文件内其他任务改动。
- [x] 验证：Hugo 构建、四页链接与锚点、状态文案、导航高亮、桌面与窄屏预览、精确 diff 审阅。

## 审阅重点

1. Hugo 项目路径前缀必须正确，四页互相可达。
2. draft 和待写内容不得产生读者可点击的失效内部链接。
3. 文章存在不等于工程实践、真机实验或线上发布已经完成。
4. 普通页面和其他任务修改的公共模板不受本轮样式影响。
5. Piccolo 保持拟题与前置材料边界，所有待写主题无虚构目标文件。

## 验证记录

2026-10-23 本地验证：

- 最终 Hugo v0.157.0 extended 构建退出码 0，生成 1340 页，输出无 ERROR。构建产物、缓存与资源目录位于 `.codex_tmp/engine-reading-20261023/`；未发布网站。
- 四页 249 个本地链接、资源路径和锚点检查通过，canonical 和 `/twoegg-tech-stack/` 前缀正确；二级首页 6 个分组、Unity 8 栏、Unreal 7 栏、Piccolo 6 栏。
- 草稿无跳转；七篇提纲、工程实践待验证、性能待补及 Piccolo 筹备／待写说明均存在。
- 浏览器观察二级页桌面布局，以及 Unity／Unreal／Piccolo 在约 508px 视口下的窄屏布局；后者页面宽度 493px，无横向溢出。三级切换和返回二级页已实际点击检查。
- 独立只读审阅未发现可行动的功能或内容问题。既有改动通过 `git diff --check`，新文件另检查尾随空格和末尾换行。
- 本轮保留已有无关改动，未提交、推送或部署。页面构建与浏览器检查不代表引擎示例或真机实验已验收。
