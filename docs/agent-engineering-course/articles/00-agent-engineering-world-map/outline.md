# Article 00 Detailed Outline Gate Preparation

- Lifecycle Status：`EVIDENCE_READY`
- Outline Status：`GATE_PREP_ONLY`
- Evidence Dependency：`READY_WITH_PROPOSAL_BOUNDARIES`

> 本文件仍不是 Detailed Outline。M1 只登记每节要回答的问题、可使用的 Claim 和证据就绪度；论证顺序、段落展开与教学表达留给 M2。

| Section | Question | Supported Claim IDs | Evidence Readiness |
|---|---|---|---|
| Opening Question | 为什么 Agent 工程讨论中会同时出现产品名、行为描述与工程层名？ | `00-C01`、`00-C02a`、`00-C04a` | `READY` |
| 1. 从 Model 到 AI Application | 模型能力与承载它的软件应用如何区分？ | `00-C01` | `READY` |
| 2. Copilot、Agent 与 Agentic | 哪些是产品术语，哪些有稳定工程核心，哪些只适合描述程度？ | `00-C02a`、`00-C02b`、`00-C03`、`00-C04a`、`00-C04b` | `READY_WITH_PROPOSALS` |
| 3. Runtime、Harness 与 Host / Product | 执行职责、工程控制与产品入口为什么要在地图上分开？ | `00-C05`、`00-C06a`、`00-C06b`、`00-C07` | `READY_WITH_PROPOSALS` |
| 4. 横向能力的地图位置 | Prompt、Context、Tool、Skill、Workflow、Memory、RAG 在 00 应讲到哪里停止？ | `00-C08` | `READY_WITH_PROPOSAL` |
| 5. 公开产品例子与不可推测边界 | Claude Code、Codex、DSH 的公开资料能证明什么，不能证明什么？ | `00-C09a`、`00-C09b`、`00-C09c` | `READY` |
| 6. 课程主链与 Part I 桥接 | 为什么看完地图后要回到 Model API，而不是直接跳进 Harness？ | `00-C01`、`00-C08` | `READY` |
| Learning Check | 读者能否给术语标出 `稳定抽象 / 课程定义 / 产品术语 / 生态依赖`？ | Definition Matrix 全部条目 | `READY` |

## M2 Entry Conditions

- 只能按 [Evidence Register](evidence.md) 的证明边界展开。
- `PROPOSAL` 必须使用“本课程采用”语态。
- `PARTIAL` 只能陈述已观察到的多生态差异，不得升级为全行业否定判断。
- 不提前展开 Agent Loop、Context、Memory、RAG 或 Harness 能力模型。
