# Agent Engineering 课程生产中心

本目录是《Agent Engineering》课程从规划进入生产后的操作入口。课程结构、文章编号与依赖关系以 [canonical series plan](../agent-engineering-series-plan.md) 为唯一基线；本目录只维护生产状态、工作流、模板和逐篇工作区，不复制课程目录。

## 当前基线

- 结构版本：v3.1，文章编号 `00—44` 已冻结
- 当前里程碑：M4.1 Article 00 Human Review Fix 已完成
- 当前生产对象：Article 00
- 当前 Article 00 状态：`FINAL`
- 当前 Article 00 证据状态：`PARTIAL`（直接证据与课程 Proposal 边界齐全；无核心 `BLOCKED`）
- 当前 Article 00 Formal Review：`PASSED_WITH_NOTES`（`92 / 100`）
- 当前 Article 00 Human Review：`HR-F01 / HR-F02 RESOLVED`（`New Core Claims = 0`）
- 下一允许动作：`M5｜Article 00 Publish`，不自动执行

## 从哪里开始

1. 在 [课程状态台账](status.md)确认文章的前置依赖、证据和 Lab 状态。
2. 按 [课程生产工作流](production-workflow.md)推进 Gate，不跳过证据与审查。
3. 新建文章工作区时使用 [文章工作区模板](templates/article-workspace-template.md)，但只为当前要生产的文章创建目录。
4. 技术主张使用 [Evidence Card 模板](templates/evidence-card-template.md)；实验使用 [Lab 模板](templates/lab-template.md)。
5. 进入审查前使用 [课程审查清单](templates/review-checklist.md)。
6. 术语含义与首次引入位置查 [课程术语表](glossary.md)。

## 生产资产

| 资产 | 职责 |
|---|---|
| [canonical series plan](../agent-engineering-series-plan.md) | 冻结课程结构、依赖、文章标题与课程边界 |
| [production-workflow.md](production-workflow.md) | 生命周期、状态机、Gate 与发布规则 |
| [status.md](status.md) | 45 篇文章的当前状态、证据、Lab 与阻塞项 |
| [templates/](templates/) | Article、Evidence、Review、Lab 的可复用模板 |
| [labs/README.md](labs/README.md) | 6 个最小实验的职责与实例化规则 |
| [articles/00-agent-engineering-world-map/](articles/00-agent-engineering-world-map/) | 当前唯一实例化的文章工作区；Article 00 已通过 M4.1 Human Review Fix 并回到 `FINAL` |

## 资产边界

- `docs/` 保存写作前的规划、研究、证据、提纲与审查材料。
- `kb/` 保存写作后可复用的知识沉淀；M4.1 不向 `kb/` 写入发布前内容。
- `content/ai-empowerment/` 保存通过 Final Gate 后的 Hugo 正文；Article 00 当前只到 `FINAL`，M4.1 不创建发布正文。
- 发布图片最终进入 `static/images/agent-engineering/<id>-<slug>/`。
- 工作区中的 `draft.md` 只在 `DRAFTING` Gate 创建；`assets/` 只在确有工作资产时创建。

## 硬规则

- 证据状态为 `BLOCKED` 的行为性主张不得进入正文，更不得进入 `FINAL`。
- `PROPOSAL` 只能表达设计方案，不能伪装成已实现或已验证的事实。
- DSH 源码结论必须同时记录固定版本、源码位置和运行验证边界。
- Lab 结论只覆盖当次 fixture、环境与观测条件。
- BuildPilot 在本课程中是设计案例；未实现前不得写成 Runtime 已存在。
- 每次只实例化当前生产文章，禁止预建 45 个空工作区。
