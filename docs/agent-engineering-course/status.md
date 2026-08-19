# Agent Engineering 课程状态台账

- Canonical：[Agent Engineering 系列计划](../agent-engineering-series-plan.md)
- 更新时间：2026-08-20
- 当前里程碑：Article 03 Reviewer / Publisher / Build / Master Reconciliation 已通过；独立 checkpoint commit 尚待验证
- 当前生产对象：Article 03；Mode `LAB_ARTICLE`；当前 Gate 为 `GIT_DIFF_VERIFY`
- Article 00 Published Path：`content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md`
- Article 01 Published Path：`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md`
- Article 02 Published Path：`content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md`
- Article 03 Published Path：`content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md`
- Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1232 Pages / 0 ERROR / 0 WARNING`，exit code `0`
- Article 01 Workspace：`docs/agent-engineering-course/articles/01-model-api-messages-token/`
- Article 01 Independent Review：`01-IR-F01 / 01-IR-F02 CLOSED`；Lifecycle 继续为 `PUBLISHED`；最新热修复 commit `798443c1d41f03960253b1190fcbc91425d4f285`
- Factory Run State：[course-run-state.md](course-run-state.md)（`RUNNING / GIT_DIFF_VERIFY`）
- Foundation Independent Review：`CF-IR-F01`—`CF-IR-F05 CLOSED`；`ARTICLE_KICKOFF` 与逐篇 checkpoint commit boundary 已补齐
- 下一允许动作：Master 审计 Article 03 transaction diff、精确 staging、checkpoint commit 与 commit verification

## 状态图例

- Lifecycle：`PLANNED -> RESEARCHING -> BLOCKED / EVIDENCE_READY -> OUTLINE_READY -> DRAFTING -> REVIEW -> FINAL -> PUBLISHED`
- Evidence：`CONFIRMED / PARTIAL / BLOCKED / PROPOSAL`
- Lab：`N/A / PLANNED / RUNNING / CONFIRMED / PARTIAL / BLOCKED`
- `BLOCKED` 解除后先回到 `RESEARCHING`；不能直接跳过 Evidence Gate。

## 全课程状态

| ID | Article | Part | Weight | Optional | Lifecycle | Evidence | Lab | 当前阻塞项 |
|---:|---|---|:---:|:---:|---|---|---|---|
| 00 | Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host | 导论 | S | 否 | `PUBLISHED` | `PARTIAL` | N/A | `NONE`；发布于 2026-08-19；`content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md` |
| 01 | 模型调用到底发生了什么：LLM、Model API、Messages 与 Token | I | M | 否 | `PUBLISHED` | `CONFIRMED` | N/A | `NONE`；发布于 2026-08-19；`01-IR-F01 / 01-IR-F02 CLOSED`；`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md` |
| 02 | Prompt Engineering：任务合同、角色、示例与边界 | I | M | 否 | `PUBLISHED` | `PARTIAL` | N/A | `NONE`；Review `92 / 100 PASS`；Publisher / Build / Master Reconciliation `PASS`；`content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md` |
| 03 | Structured Output：让模型输出成为机器可消费的合同 | I | L | 否 | `PUBLISHED` | `CONFIRMED`（`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`） | Lab 01 `CONFIRMED / EVIDENCE_MERGED` | `NONE`；Review `PASS / 93`，Publisher / Build / Master Reconciliation `PASS`；checkpoint pending |
| 04 | Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异 | I | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 05 | Function Calling 与 Tool Use：模型如何表达行动意图 | II | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 06 | Tool Runtime：Validate、Policy、Execute、Result 与 Trace | II | L | 否 | `PLANNED` | `BLOCKED` | Lab 02 `PLANNED / BLOCKED` | Lab fixture 与执行合同未设计 |
| 07 | MCP 与外部能力边界：协议解决什么，宿主仍需解决什么 | II | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 08 | Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop | II | L | 否 | `PLANNED` | `BLOCKED` | Lab 03 `PLANNED / BLOCKED` | Lab loop 与停止判据未设计 |
| 09 | Planning：Agent 为什么需要计划，又为什么不能迷信计划 | II | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 10 | State Machine 与 Workflow：确定性骨架和 Agent Decision Point | II | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 11 | Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery | II | M | 否 | `PLANNED` | `BLOCKED` | Lab 04 `PLANNED / BLOCKED` | 恢复 fixture 与故障注入未设计 |
| 12 | Context Engineering：每一个 Step 到底应该看到什么 | III | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 13 | Context Debugging：Packing、Compression、Pollution 与可重建性 | III | L | 否 | `PLANNED` | `BLOCKED` | Lab 05 `PLANNED / BLOCKED` | Context fixture 与判据未设计 |
| 14 | Working Memory 与 Investigation State：当前任务正在想什么 | III | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 15 | Session、Long-term Memory 与 Project Memory：事实、经验和作用域 | III | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 16 | Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite | III | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 17 | Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt | III | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 18 | Evidence Contract：把自然语言推断变成可审计工程数据 | IV | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 19 | Permission、Approval、Human-in-the-loop 与 Sandbox | IV | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 20 | Budget Engineering：Token、Step、Cost 与 Latency | IV | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 21 | Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层 | IV | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 22 | Eval、Golden Dataset 与 Regression：修复以后还会不会再坏 | IV | L | 否 | `PLANNED` | `BLOCKED` | Lab 06 `PLANNED / BLOCKED` | Golden Dataset 与退化 fixture 未设计 |
| 23 | Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent | IV | M | 是 | `PLANNED` | `BLOCKED` | N/A | Advanced / Optional；未开始研究 |
| 24 | 为什么最终需要 Harness：横切能力由谁承载 | V | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 25 | Agent Runtime vs Harness：执行内核与工程控制面 | V | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 26 | Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery | V | L | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 27 | Harness 的设计取舍：可替换性、复杂度、Bloat 与演化 | V | M | 否 | `PLANNED` | `BLOCKED` | N/A | 未开始研究 |
| 28 | 怎样把 DeepSeek Harness 当作 Evidence-first 源码教材 | VI | S | 否 | `PLANNED` | `BLOCKED` | N/A | DSH 仓库、commit 与运行入口未固定 |
| 29 | DeepSeek Harness 总图：从 Host 启动到一次 Agent Run | VI | M | 否 | `PLANNED` | `BLOCKED` | N/A | DSH 仓库、commit、Host 入口与端到端运行路径均未固定 |
| 30 | Everything is a Plugin：插件内核如何承载 Capability 与生命周期 | VI | M | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 31 | Profile、Bundle、Provider 与 Capability Seam | VI | M | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 32 | System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成 | VI | L | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 33 | Inbox、Turn、Step 与 Agent Loop | VI | L | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 34 | Append-only Session Event：Replay、Resume、Fork 与 Projection | VI | L | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 35 | Tool Registry 与 Tool Execution Pipeline | VI | L | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 36 | Cost、Compaction、Trace、Cancellation 与 Recovery | VI | L | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、源码路径与运行验证未固定 |
| 37 | RAG、Skill、Workflow、Subagent 与 Web / Headless：核心事实和扩展映射 | VI | M | 否 | `PLANNED` | `BLOCKED` | N/A | DSH commit、核心/扩展边界与运行验证未固定 |
| 38 | 游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent | VII | M | 否 | `PLANNED` | `PROPOSAL` | N/A | 仅允许设计语态；案例事实尚未取证 |
| 39 | 案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent | VII | L | 否 | `PLANNED` | `PROPOSAL` | N/A | 仅设计；fixture 尚未实现或验证 |
| 40 | 案例 B：启动性能调查——设计一个长链路、多假设 Agent | VII | L | 否 | `PLANNED` | `PROPOSAL` | N/A | 仅设计；调查 fixture 尚未实现或验证 |
| 41 | 从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块 | VII | L | 否 | `PLANNED` | `PROPOSAL` | N/A | 依赖 39—40 的设计输入 |
| 42 | BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位 | VII | L | 否 | `PLANNED` | `PROPOSAL` | N/A | BuildPilot 仅为 Design v1 |
| 43 | BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval | VII | L | 否 | `PLANNED` | `PROPOSAL` | N/A | BuildPilot 仅为 Design v1 |
| 44 | BuildPilot Design v1：设计评审、里程碑与退出条件 | VII | S | 否 | `PLANNED` | `PROPOSAL` | N/A | 等待前述设计篇完成后评审 |

## 更新规则

1. 状态变化必须对应 [生产工作流](production-workflow.md)中的 Gate 证据。
2. 一次更新同时写清 Evidence、Lab 和 Blocker，禁止只改 Lifecycle。
3. 文章发布后先回写本表，再回写 canonical；根 `doc-plan.md` 只维护系列级路由。
4. Article 00 已完成 M5：在原 `PLANNED -> RESEARCHING -> EVIDENCE_READY -> OUTLINE_READY -> DRAFTING -> REVIEW -> FINAL` 与 M4.1 `FINAL -> REVIEW -> FINAL` 后，通过发布 Gate 进入 `PUBLISHED`；Evidence 继续为 `PARTIAL`。
5. Article 01 已完成 A1—A6 Full Production Run：`PLANNED -> RESEARCHING -> EVIDENCE_READY -> OUTLINE_READY -> DRAFTING -> REVIEW -> FINAL -> PUBLISHED`；随后完成 `01-IR-F01 / 01-IR-F02` 两次 post-publication hotfix，Evidence 继续为 `CONFIRMED`，Lab 为 `N/A`。
6. Article 02 已完成 `PRECHECK -> ARTICLE_KICKOFF -> RESEARCH -> EVIDENCE_READY -> OUTLINE_READY -> DRAFTING -> REVIEW -> REVISION -> REVIEW_RECHECK -> FINAL -> PUBLISHED`；Evidence 为 `PARTIAL`，Lab 为 `N/A`，Publisher / Build / Master Reconciliation 均为 `PASS`。下一篇仍受 Article 02 独立 checkpoint commit verification 约束。
7. Foundation Independent Review 已关闭 `CF-IR-F01`—`CF-IR-F05`；只修复 transaction ownership / ordering / resume semantics，没有启动 Article 02。
8. Foundation Article Kickoff Hotfix 已补齐 `ARTICLE_KICKOFF`、Article checkpoint commit、commit verification 与 next-Article stop line；独立提交为 `9b75fa7`。Article 02 checkpoint `b359a32` 已验证，Article 03 transaction 已按该边界启动。
9. Article 03 按 Required Lab Article 流程完成 `PRELIMINARY_EVIDENCE -> LAB_DESIGN -> LAB_EXECUTE -> LAB_OBSERVATION -> EVIDENCE_MERGE -> EVIDENCE_GATE`，随后完成 Outline、Draft、Cycle 1 Review / Revision / Recheck、Publisher、Build 与 Master Reconciliation；独立 checkpoint 仍待验证。
