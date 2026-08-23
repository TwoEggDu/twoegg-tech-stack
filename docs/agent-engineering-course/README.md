# Agent Engineering 课程生产中心

本目录是《Agent Engineering》课程从规划进入生产后的操作入口。课程结构、文章编号与依赖关系以 [canonical series plan](../agent-engineering-series-plan.md) 为唯一基线；本目录只维护生产状态、工作流、模板和逐篇工作区，不复制课程目录。

## 当前基线

- 结构版本：v3.1，文章编号 `00—44` 已冻结
- 当前里程碑：Article 15 Lifecycle checkpoint=`PUBLISHED`；Persisted Checkpoint=`PRE_COMMIT_RECONCILIATION PASS`；Completion Resolution=`DERIVED_FROM_GIT_HISTORY`；Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 15`；retrospective resolver observation=`END_ARTICLE` / completion commit observation=`0c9465ca55e095bb1d78e71016b9c6ba357c7ac6`（仅说明，非 future authority）
- 当前生产对象：Article 16 `PRECHECK POINTER CANDIDATE / NOT_STARTED / FORBIDDEN CURRENT RUN`；workspace与Published Content均不存在
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
- 当前 Article 01 Independent Review：`01-IR-F01 / 01-IR-F02 CLOSED`（post-publication hotfix；Lifecycle 继续为 `PUBLISHED`）
- 当前 Article 01 Published Content：`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md`
- 当前 Article 01 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`
- 当前 Article 02 状态：`PUBLISHED`；Evidence `PARTIAL`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）；Final Review `PASS`（`92 / 100`）；Published Content：`content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md`
- 当前 Article 02 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1231 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 03 状态：`PUBLISHED`；Evidence `CONFIRMED`（`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`）；Required Lab 01 `CONFIRMED / EVIDENCE_MERGED`；Final Review `PASS / 93`；Publisher / Build `PASS`；Published Content：`content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md`
- 当前 Article 03 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1232 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 03 Checkpoint：`857fe9fdc6baa541ced28d428d0c7fbe07d45ed9`；message / 34-file scope / clean tree / log / show verification `PASS`
- 当前 Article 04 状态：`PUBLISHED`；Evidence `PASS`（`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`）；Final Review `PASS / 93`；`04-F01 CLOSED`；Publisher / Build / Master Reconciliation `PASS`；Published Content：`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`
- 当前 Article 04 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1233 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 04 Checkpoint：`ac10060b82d21534a014d7a4bef3b3e03f7bd475`；message / 13-file scope / clean tree / log / show verification `PASS`
- 当前 Part I Audit：[durable report](audits/part-i-audit.md)；Gate `PASS`；checkpoint `b7fafc5f2e490a5d6590da1cfb54d9f2ced5968c` verified；`PI-F01`—`PI-F03 CLOSED / 0 OPEN MINOR`
- 当前 Article 05 状态：`PUBLISHED`；Evidence `PASS`（`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`）；Final Review `PASS / 95`；Publisher / Build / Master Reconciliation `PASS`；Published Content：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`
- 当前 Article 05 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1234 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 05 Checkpoint：`c0cf180c281ea5dbb70c891176735f4ed9e34d3f`；message / 13-file scope / clean tree / log / show verification `PASS`
- 当前 Article 06 状态：`PUBLISHED`；Evidence=`PASS / 8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`；Required Lab 02 `CONFIRMED / EVIDENCE_MERGED`；Review / Final Gate=`PASS / 93`；Publisher / Build / Master Reconciliation=`PASS`
- 当前 Article 06 Published Content：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`
- 当前 Article 06 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1235 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 06 Checkpoint：`199d4e19ba6150c8c598788a2daa8488e6e855f3`；message / 36-file scope / clean tree / log / show verification `PASS`
- 当前 Article 07 状态：`PUBLISHED`；Cycle 2=`07-F01 / 07-F02 CLOSED`；Review / Final Gate=`PASS / 92`；Publisher / Build / Master Reconciliation=`PASS`
- 当前 Article 07 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1236 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 07 Checkpoint：`f3de0f2a7b1e06c530900627183bd364ca0b4314`；message / 14-file scope / clean tree / push / live remote verification `PASS`
- 当前 Article 08 状态：`PUBLISHED`；Evidence=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`；Required Lab 03 `VERIFIED / EVIDENCE_MERGED`；Review / Final Gate=`PASS / 92`；`08-F01 CLOSED`；Publisher / Build / Master Reconciliation=`PASS`
- 当前 Article 08 Published Content：`content/ai-empowerment/agent-engineering-08-agent-loop.md`
- 当前 Article 08 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1237 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 09 状态：`PUBLISHED`；Evidence=`5 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`；Review / Final Gate=`PASS / 91 / 0 OPEN`；`09-F01 / 09-F02 CLOSED`；checkpoint `7b9d733f` pushed / live-remote verified
- 当前 Article 09 Published Content：`content/ai-empowerment/agent-engineering-09-planning.md`
- 当前 Article 09 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1238 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 10 状态：`PUBLISHED`；Evidence=`6 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`；Review / Final Gate Cycle 2=`PASS / 96 / 0 OPEN`；`10-F01 / 10-F02 / 10-F03 CLOSED`；checkpoint `b35b1f32` pushed / live-remote verified
- 当前 Article 10 Published Content：`content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`
- 当前 Article 10 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1239 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 11 状态：`PUBLISHED`；Evidence=`PASS / 9 of 9 TRACEABLE / 0 CORE BLOCKED / C08 SPLIT-SCOPED`；Required Lab 04 `CONFIRMED / EVIDENCE_MERGED / 8 of 8`；Review / Final Gate=`PASS / 94 / 0 OPEN`；`11-R0-F01 / 11-R0-F02 CLOSED`；checkpoint `31aef0aa` pushed / live-remote verified
- 当前 Article 11 Published Content：`content/ai-empowerment/agent-engineering-11-long-running-agent.md`
- 当前 Article 11 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1240 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 12 状态：`PUBLISHED / COMPLETED / END_ARTICLE`；Evidence=`PASS / 9 of 9 TRACEABLE / 0 CORE BLOCKED`；Review / Final Gate=`PASS / 93 / 0 OPEN`；`12-R0-F01`—`F04 CLOSED`；completion commit `a87f058ae2642870ade75fa7f23ac4396f17b94c` pushed / live-remote verified
- 当前 Article 12 Published Content：`content/ai-empowerment/agent-engineering-12-context-engineering.md`
- 当前 Article 12 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1241 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 13 状态：`PUBLISHED / COMPLETED / END_ARTICLE`；Evidence=`PASS / 9 of 9 TRACEABLE / 3 CONFIRMED / 6 PROPOSAL / 0 BLOCKED`；Required Lab 05=`EVIDENCE_MERGED / EVIDENCE_GATE_PASS / FIXTURE-SCOPED`；Final Gate Cycle 2=`PASS / 91 / 0 OPEN`；`13-F01`—`F05 CLOSED`；completion commit `8b18b85b5a0f6a95f042832e36a8f7cb09f8609a`；local / origin / live remote equality=`PASS`
- 当前 Article 13 Published Content：`content/ai-empowerment/agent-engineering-13-context-debugging.md`
- 当前 Article 13 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1242 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- 当前 Article 14 状态：`PUBLISHED / COMPLETED / END_ARTICLE`；Evidence=`PASS / 5 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED`；Final=`PASS / 93 / 0 OPEN`；completion commit `a53d151ba051403ff5ef369e5c3860a9fbded03d`；local / origin / live remote equality=`PASS`
- 当前 Article 14 Published Content：`content/ai-empowerment/agent-engineering-14-working-memory-investigation-state.md`
- 当前 Article 14 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1243 Pages / 0 ERROR / 0 WARNING`，exit code `0`；fixed-clock future hits=`0`
- 当前 Article 15 Published Content：`content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md`
- 当前 Article 15 Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1244 Pages / 0 ERROR / 0 WARNING / 0 REF_NOT_FOUND`，exit code `0`；future hits=`0`
- 当前 Part II Audit：[durable report](audits/part-ii-audit.md)；Gate `PASS`；`0 BLOCKER / 0 MAJOR / 0 OPEN MINOR / 4 CLOSED MINOR / 0 EDITORIAL`；Hugo / Labs 02—04 / navigation / checkpoint evidence `PASS`
- Factory Status：`READY / Article 16 / PRECHECK pointer candidate / NOT_STARTED / FORBIDDEN CURRENT RUN / active worker NONE`；Continuous Run已在inclusive stop_after Article 15处关闭；下一条持久化策略是等待明确的人类指令，禁止执行Article 16 PRECHECK或`ARTICLE_KICKOFF`
- Factory Git Contract：`MAIN_ONLY_PRODUCTION / ONE_ARTICLE_ONE_COMMIT / ONE_ARTICLE_ONE_PUSH / POST_COMMIT_WRITES_ZERO`；completion SHA 由 Git history 提供，checkpoint 后 reconciliation 只读
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
| [Part I Audit](audits/part-i-audit.md) | Article 01—04 的跨篇一致性、Lab、publication 与 checkpoint 审计；Gate `PASS`，`PI-F01`—`PI-F03 CLOSED` |
| [Part II Audit](audits/part-ii-audit.md) | Article 05—11 的跨篇一致性、Labs 02—04、publication 与 checkpoint 审计；Gate `PASS`，`PII-F01`—`PII-F04 CLOSED` |
| [templates/](templates/) | Article、Evidence、Review、Lab 的可复用模板 |
| [labs/README.md](labs/README.md) | 6 个最小实验的职责与实例化规则 |
| [articles/00-agent-engineering-world-map/](articles/00-agent-engineering-world-map/) | Article 00 已通过 M5 Publish Gate 并进入 `PUBLISHED` |
| [articles/01-model-api-messages-token/](articles/01-model-api-messages-token/) | Article 01 已通过 A1—A6 Full Production Run 并进入 `PUBLISHED` |
| [articles/04-model-adapter-llm-gateway/](articles/04-model-adapter-llm-gateway/) | Article 04 已完成 `PUBLISHED`、独立 checkpoint verification 与 Part I Audit |
| [articles/05-function-calling-tool-use/](articles/05-function-calling-tool-use/) | `PUBLISHED`；checkpoint `c0cf180c281ea5dbb70c891176735f4ed9e34d3f` verified |
| [articles/06-tool-runtime/](articles/06-tool-runtime/) | `PUBLISHED`；checkpoint `199d4e19ba6150c8c598788a2daa8488e6e855f3` verified |
| [articles/07-mcp-external-capability-boundary/](articles/07-mcp-external-capability-boundary/) | `PUBLISHED`；checkpoint `f3de0f2` pushed / live-remote verified |
| [articles/08-agent-loop/](articles/08-agent-loop/) | `PUBLISHED`；Review / Final Gate `PASS / 92 / 0 OPEN`；Required Lab 03 `VERIFIED / EVIDENCE_MERGED`；checkpoint `d4693bd` verified |
| [articles/09-planning/](articles/09-planning/) | `PUBLISHED`；Review / Final Gate `PASS / 91 / 0 OPEN`；checkpoint `7b9d733f` pushed / live-remote verified |
| [articles/10-state-machine-workflow/](articles/10-state-machine-workflow/) | `PUBLISHED`；Final Gate Cycle 2 `PASS / 96 / 0 OPEN`；checkpoint `b35b1f32` pushed / live-remote verified |
| [articles/11-long-running-agent/](articles/11-long-running-agent/) | `PUBLISHED`；Final Gate `PASS / 94 / 0 OPEN`；Required Lab 04 `CONFIRMED / EVIDENCE_MERGED`；checkpoint `31aef0aa` pushed / live-remote verified |
| [articles/12-context-engineering/](articles/12-context-engineering/) | `PUBLISHED / END_ARTICLE`；Final Gate `PASS / 93 / 0 OPEN`；checkpoint `a87f058ae2642870ade75fa7f23ac4396f17b94c` pushed / live-remote verified；Hugo `1241 Pages / 0 ERROR / 0 WARNING` |
| [articles/13-context-debugging/](articles/13-context-debugging/) | `PUBLISHED / COMPLETED / END_ARTICLE`；completion commit `8b18b85b5a0f6a95f042832e36a8f7cb09f8609a`；local / origin / live remote equality=`PASS`；Final Gate Cycle 2 `PASS / 91 / 0 OPEN`；Required Lab 05 `EVIDENCE_GATE_PASS / FIXTURE-SCOPED`；Hugo `1242 Pages / 0 ERROR / 0 WARNING` |

## 资产边界

- `docs/` 保存写作前的规划、研究、证据、提纲与审查材料。
- `kb/` 保存写作后可复用的知识沉淀；M5 未向 `kb/` 写入内容。
- `content/ai-empowerment/` 保存通过 Final Gate 后的 Hugo正文；Article 00—15 已进入Published Content。Article 13已由 completion commit `8b18b85b5a0f6a95f042832e36a8f7cb09f8609a` 证明完成；Article 14已由 completion commit `a53d151ba051403ff5ef369e5c3860a9fbded03d` 与 local / origin / live remote equality=`PASS` 证明为 `PUBLISHED / COMPLETED / END_ARTICLE`；Article 15 的 `PUBLISHED` completion 由 Git history 与 remote refs 派生，retrospective resolver / commit observations 仅用于解释迁移，不构成未来 authority。
- 发布图片最终进入 `static/images/agent-engineering/<id>-<slug>/`。
- 工作区中的 `draft.md` 只在 `DRAFTING` Gate 创建；`assets/` 只在确有工作资产时创建。

## 硬规则

- 证据状态为 `BLOCKED` 的行为性主张不得进入正文，更不得进入 `FINAL`。
- `PROPOSAL` 只能表达设计方案，不能伪装成已实现或已验证的事实。
- DSH 源码结论必须同时记录固定版本、源码位置和运行验证边界。
- Lab 结论只覆盖当次 fixture、环境与观测条件。
- BuildPilot 在本课程中是设计案例；未实现前不得写成 Runtime 已存在。
- 每次只实例化当前生产文章，禁止预建 45 个空工作区。
