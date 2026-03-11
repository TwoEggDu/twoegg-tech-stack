# TwoEgg Tech Stack

这个仓库现在是一个 Hugo 驱动的个人技术站点，用来展示：

- 项目案例
- Unity / Unreal 与客户端基础架构理解
- 工具链、构建发布、问题拆解
- 价值观、职业判断和长期输入

目标不是做“技术日记”，而是让读者在几分钟内判断：

- 你做过什么
- 你理解到了什么深度
- 你如何做技术判断
- 你适合承担什么样的工程职责

## 页面入口

当前站点的主要 page 入口如下：

- `Home`：`/`
- `Projects`：`/projects/`
- `Engine Notes`：`/engine-notes/`
- `Problem Solving`：`/problem-solving/`
- `Essays`：`/essays/`
- `About`：`/about/`

如果以 GitHub Pages 项目站点方式部署，这些路径会挂在：

```text
https://twoeggdu.github.io/twoegg-tech-stack/
```

## Site Positioning

站点标题：`TwoEgg Tech Stack`

内容结构：

- `Projects`
- `Engine Notes`
- `Problem Solving`
- `Essays`
- `About`

这个结构是按求职展示来排的，优先让读者先看到项目、工程判断和技术深度，再去看价值观和个人表达。

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

## First Batch

初始化版本先挂这一批内容：

- 工具链负责人不是写几个工具的人
- Unity 工具链开发真正要懂的三条引擎链路
- 从上线流程反推主程价值
- 从项目内工具到跨项目平台
- 客户端基础架构为什么要同时懂工具链和渲染优化
- 特效性能检查器案例
- 用读过的书表达你的价值观

## Next Steps

后面最值得持续补的是：

- 每个项目的量化结果
- Unreal 相关内容
- 更具体的实用技巧栏目
- 首页项目封面图、流程图和性能对比图