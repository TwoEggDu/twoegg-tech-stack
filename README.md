# TwoEgg Tech Stack

这是一个 Hugo 驱动的游戏工程知识站，以游戏产品交付为主线，记录从引擎工具、内容生产到客户端与服务端协作，再到上线运行与持续迭代的工程实践与技术研究。

内容从作者的职业经历和实际问题出发：此前的引擎工具工作是需要持续补强的根基，当前聚焦策划、美术、客户端与服务端的协作交付，并逐步研究运营数据分析、服务器部署、压力测试与服务端设计。

全生命周期地图提供整体视野；Unity / Unreal、渲染、运行时、操作系统等专题解释实现机制与约束。内容成熟度、个人职责和验证结果分别说明。

当前原则与渐进调整顺序统一维护在[站点定位与演进原则](docs/site-entry-and-audience-plan.md)。2026-10-14 已确认暂时撤下求职与顾问合作入口，本轮先固化文档，网站页面和导航待后续调整。

## 知识阅读入口

- [商业游戏全生命周期地图](docs/game-lifecycle-knowledge-map.md)：按产品阶段寻找核心问题、现有文章、交付物与待补提纲。
- [技术系列总路由](doc-plan.md)：按领域进入唯一系列规划，深入阅读原有技术主线。
- [跨仓覆盖与归并审查](docs/game-lifecycle-coverage-audit.md)：本轮仓库版本、抽读范围、缺口、ET 版本边界与检查结果。

公开规划与文章维护在本仓；固定 ET 工程、源码研究、实验与原始验证记录维护在 ET-Learning-Lab。地图只串联阅读路径，不复制两套教材。

## 页面入口

现有页面由 `content/` 与 `layouts/` 生成，主导航配置在 `hugo.toml`。当前仍保留旧入口；后续按站点定位逐步调整为研发总览、阶段阅读、技术专题和工程案例等阅读入口，具体栏目安排在入口调整阶段确定。

如果以 GitHub Pages 项目站点方式部署，这些路径会挂在：

```text
https://twoeggdu.github.io/twoegg-tech-stack/
```

## Site Positioning

站点标题：`TwoEgg Tech Stack`

生命周期导航回答何时需要哪些知识，技术系列回答问题如何深入研究，工程案例与实践演进连接实际经历和证据。详细组织原则只维护在[站点定位文档](docs/site-entry-and-audience-plan.md)。

根 `doc-plan.md` 保持系列级路由，各系列主规划维护篇级目录；网站入口调整复用已有文章和 URL。

## Project Layout

```text
.
├─ .github/workflows/hugo.yaml   # GitHub Pages 发布工作流
├─ archetypes/default.md         # Hugo 新文章模板
├─ content/                      # Hugo 正式站点内容
├─ docs/                         # 原始策划、定位和草稿资料
├─ articles/                     # 原始长文草稿
├─ layouts/                      # 自定义 Hugo 模板
├─ static/css/site.css           # 站点样式
└─ hugo.toml                     # Hugo 配置
```

说明：

- `docs/` 和 `articles/` 保留为原始素材区。
- 它们同样属于公开仓库；不参与 Hugo 生成不等于私有，素材也须满足公开与许可边界。
- `content/` 是真正会被 Hugo 发布的内容区。
- 这样做的目的是把“草稿整理”和“对外发布”分开，后面继续写会更稳。

## Local Development

先安装 Hugo Extended，然后在仓库根目录运行：

```bash
hugo server -D
```

默认访问地址通常是：

```text
http://localhost:1313/
```

如果只是生成静态文件：

```bash
hugo --gc --minify
```

产物会输出到 `public/`。

## GitHub Pages

仓库已经配置了 GitHub Pages 工作流：

- 工作流文件：`.github/workflows/hugo.yaml`
- 发布方式：`GitHub Actions`
- 部署分支：由 GitHub Pages 环境接管，不需要手动维护 `gh-pages`

如果你的仓库名不是 `username.github.io`，而是像 `twoegg-tech-stack` 这种项目站点，线上地址通常会是：

```text
https://twoeggdu.github.io/twoegg-tech-stack/
```

工作流会在构建时自动注入正确的 `baseURL`，所以不需要手动改发布路径。

## Writing Rules

更适合这个站点的文章结构：

1. 背景
2. 问题
3. 约束
4. 候选方案
5. 为什么这么选
6. 实现细节
7. 踩坑
8. 结果
9. 复盘

这样比“写了什么 API / 做了什么小工具”更能体现工程判断。

## Next Steps

下一步先把现有文章、真实经历与可公开证据对应到贯穿阅读路径，再调整网站入口，随后补强引擎工具与交付连接、逐步扩展上线反馈主题。阶段范围与验收点见[站点定位与演进原则](docs/site-entry-and-audience-plan.md)，本文件不另建写作排期。
