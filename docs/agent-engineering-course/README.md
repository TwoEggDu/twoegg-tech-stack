# Agent Engineering 课程生产中心

本目录是《Agent Engineering》课程从规划进入生产后的操作入口。课程结构、文章编号与依赖关系以 [canonical series plan](../agent-engineering-series-plan.md) 为唯一基线；本目录只维护生产状态、工作流、模板和逐篇工作区，不复制课程目录。

## 当前基线

- 结构版本：v3.1，文章编号 `00—44` 已冻结
- 当前里程碑：Course Factory Foundation Article Kickoff Hotfix 已完成；Factory 继续为 `READY`，尚未启动
- 当前生产对象：`NONE`；execution pointer 为 Article 02 `PRECHECK`，Article 02 仍为 `PLANNED`
- 当前 Article 00 状态：`PUBLISHED`
- 当前 Article 00 证据状态：`PARTIAL`（直接证据与课程 Proposal 边界齐全；无核心 `BLOCKED`）
- 当前 Article 00 Formal Review：`PASSED_WITH_NOTES`（`92 / 100`）
- 当前 Article 00 Human Review：`HR-F01 / HR-F02 RESOLVED`（`New Core Claims = 0`）
- 当前 Article 00 Published Content：`content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md`
- 当前 Article 00 Build Verification：`hugo --gc --minify`，`1229 Pages / 0 ERROR`
- 当前 Article 01 状态：`PUBLISHED`
- 当前 Article 01 Evidence：`CONFIRMED`（`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- 当前 Article 01 Workspace：`docs/agent-engineering-course/articles/01-model-api-messages-token/`
- 当前 Article 01 Formal Review：`PASSED`（`92 / 100`）
- 当前 Article 01 Independent Review：`01-IR-F01 CLOSED`（post-publication hotfix；Lifecycle 继续为 `PUBLISHED`）
- 当前 Article 01 Published Content：`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md`
- 当前 Article 01 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`
- Factory Status：`READY`；下一允许动作：`START_ARTICLE_02_PRECHECK`，本次未执行
- Foundation Independent Review：`CF-IR-F01`—`CF-IR-F05 CLOSED`；`ARTICLE_KICKOFF` 与逐篇 checkpoint commit boundary 已补齐；Review history 见 [course-factory.md](course-factory.md)

## 从哪里开始

1. 多文章运行或恢复先读取 [Factory execution pointer](course-run-state.md)，再与 [课程状态台账](status.md)和 Git state 对齐。
2. 按 [Course Factory contract](course-factory.md)完成 PRECHECK 与显式 `ARTICLE_KICKOFF`，再按 [Subagent contracts](subagent-contracts.md)启动当前 Gate 必需角色。
3. 按 [课程生产工作流](production-workflow.md)推进篇内 Gate，不跳过证据与审查。
4. 新建文章工作区时使用 [文章工作区模板](templates/article-workspace-template.md)，但只为当前要生产的文章创建目录。
5. 技术主张使用 [Evidence Card 模板](templates/evidence-card-template.md)；实验使用 [Lab 模板](templates/lab-template.md)。
6. 进入审查前使用 [课程审查清单](templates/review-checklist.md)；术语含义与首次引入位置查 [课程术语表](glossary.md)。

## 生产资产

| 资产 | 职责 |
|---|---|
| [canonical series plan](../agent-engineering-series-plan.md) | 冻结课程结构、依赖、文章标题与课程边界 |
| [course-factory.md](course-factory.md) | Article 02—44 的顺序编排、恢复、特殊模式、Part Audit 与 Final Audit 合同 |
| [subagent-contracts.md](subagent-contracts.md) | Master、Researcher、Author、Reviewer、Revision、Lab、Publisher 与 Part Auditor 的职责边界 |
| [course-run-state.md](course-run-state.md) | 小型、可恢复的 Factory execution pointer；不是第二套课程台账 |
| [production-workflow.md](production-workflow.md) | 生命周期、状态机、Gate 与发布规则 |
| [status.md](status.md) | 45 篇文章的当前状态、证据、Lab 与阻塞项 |
| [templates/](templates/) | Article、Evidence、Review、Lab 的可复用模板 |
| [labs/README.md](labs/README.md) | 6 个最小实验的职责与实例化规则 |
| [articles/00-agent-engineering-world-map/](articles/00-agent-engineering-world-map/) | Article 00 已通过 M5 Publish Gate 并进入 `PUBLISHED` |
| [articles/01-model-api-messages-token/](articles/01-model-api-messages-token/) | Article 01 已通过 A1—A6 Full Production Run 并进入 `PUBLISHED` |

## 资产边界

- `docs/` 保存写作前的规划、研究、证据、提纲与审查材料。
- `kb/` 保存写作后可复用的知识沉淀；M5 未向 `kb/` 写入内容。
- `content/ai-empowerment/` 保存通过 Final Gate 后的 Hugo 正文；Article 00—01 已正式发布，workspace Draft 继续独立保留。
- 发布图片最终进入 `static/images/agent-engineering/<id>-<slug>/`。
- 工作区中的 `draft.md` 只在 `DRAFTING` Gate 创建；`assets/` 只在确有工作资产时创建。

## 硬规则

- 证据状态为 `BLOCKED` 的行为性主张不得进入正文，更不得进入 `FINAL`。
- `PROPOSAL` 只能表达设计方案，不能伪装成已实现或已验证的事实。
- DSH 源码结论必须同时记录固定版本、源码位置和运行验证边界。
- Lab 结论只覆盖当次 fixture、环境与观测条件。
- BuildPilot 在本课程中是设计案例；未实现前不得写成 Runtime 已存在。
- 每次只实例化当前生产文章，禁止预建 45 个空工作区。
