# Article 00 Research Conclusion Index

- Lifecycle Status：`EVIDENCE_READY`
- Research Status：`COMPLETE`
- Evidence Status：`PARTIAL`（核心主张均已获得直接证据或明确标为课程提案；无 `BLOCKED`）
- Research Window：`2026-08-18（Asia/Shanghai）`
- Scope：只建立地图级 working definitions，不展开 Article 08、12—17、24—27 或 DSH 28—37 的机制研究

| RQ | Status | Main Finding | Supporting Claim IDs | Remaining Uncertainty | Course Impact |
|---|---|---|---|---|---|
| RQ-01 | `ANSWERED` | Model 提供基于输入生成输出的能力；AI Application 把模型调用与软件逻辑、数据、工具或界面组合起来。二者不是同一层。 | `00-C01` | “Model”在不同产品中还可能指服务、部署或具体版本；00 不展开。 | 先建立能力与应用边界，再进入 01 的 API / Messages / Token。 |
| RQ-02 | `ANSWERED` | Microsoft 与 GitHub 都把 Copilot 用作产品名；GitHub Copilot 同时包含同步辅助和自主 Agentic 能力。没有足够证据把 Copilot 固定成一种架构或 Agent 的前置阶段。 | `00-C02a`、`00-C02b` | 本次样本不能证明全球不存在任何统一定义，只能证明课程不应假设一个统一架构。 | Copilot 作为产品术语出现，不进入课程分层主链。 |
| RQ-03 | `ANSWERED` | 跨 OpenAI、Anthropic、Google 可保留的稳定核心是：围绕目标，由模型参与决定推进方式，通过行动 / 工具与反馈处理多步任务，并在完成、限制或人工介入处停止。 | `00-C03` | State、Loop、Stop 的精确定义和必要性不在 00 定论。 | 00 给地图级 working definition；正式机制留给 08。 |
| RQ-04 | `ANSWERED` | Anthropic 用 agentic systems 同时包住 workflow 与 agent；GitHub 又用 agentic features / agent mode 描述产品能力。它不是可靠的固定架构层级。 | `00-C04a`、`00-C04b` | 其他生态还会有更多用法，00 不做穷举。 | 课程把 Agentic 当作行为特征 / 自主程度描述，并显式标成教学选择。 |
| RQ-05 | `ANSWERED` | 执行职责可稳定观察到 model invocation、tool dispatch、loop、state / continuation 与 stop；但由应用、SDK、部署平台还是其他层承载因框架而异，“Agent Runtime”本身也有不同用法。 | `00-C05` | Runtime 与 Harness 的详细责任分配要到 25 才正式建立。 | 00 只采用“执行 Agent 的内核职责”这一课程抽象。 |
| RQ-06 | `PARTIAL` | Anthropic 把 harness 解释为模型运行所受的 instructions / guardrails；DeepSeek 把完整开源系统命名为 agent harness。多个真实用法存在，但含义并不相同，无法据此声称行业统一标准。 | `00-C06a`、`00-C06b` | 尚未做全行业术语普查；DSH 内部能力必须等待 pinned-source 专题。 | Harness 明确标为课程控制层抽象，只用于组织 24—27 的学习。 |
| RQ-07 | `ANSWERED` | CLI、IDE、Web、Desktop、CI 等是公开产品入口或运行环境；它们只能证明用户入口与可见能力，不能反推内部 Runtime 分层。 | `00-C07` | 不公开的产品内部边界保持未知。 | Host / Product 与 Runtime 分开画，但导航图不宣称是通用部署拓扑。 |
| RQ-08 | `ANSWERED` | 00 对 Prompt、Context、Tool、Skill、Workflow、Memory、RAG 只给一句定位定义和正式文章路由；不讲内部管线、生命周期或治理细节。 | `00-C08` | 各正式篇仍需各自 Evidence-first Research。 | 防止导论吞掉 02、05—06、10、12、14—17。 |
| RQ-09 | `ANSWERED` | Claude Code 与 Codex 可确认为能读写代码、运行命令并出现在多种入口中的 coding agent / agentic coding tool；DSH 可确认为 DeepSeek 官方开源、开发者预览中的 agent harness。公开资料不能证明三者内部采用同一 Runtime / Harness 架构。 | `00-C09a`、`00-C09b`、`00-C09c` | 产品持续变化；DSH 开发者预览明确可能发生兼容性破坏。 | 例子只帮助区分公开产品形态，不承担内部架构证明。 |

## Stable Core 与生态差异

- Stable Engineering Core：Model 是能力；Agent 是围绕目标推进多步工作的应用 / 软件系统；工具把模型请求连接到外部数据和动作；公开产品入口与内部执行职责不是同一概念层。
- Ecosystem-dependent：Copilot、Agentic、Agent Runtime、Harness 的命名和边界会随生态变化。
- Course Proposal：用 `User Goal -> Host / Product -> Harness -> Agent Runtime -> Model + Tool + State -> External World` 作为学习导航图，而不是通用产品部署声明。

## M1 Stop Line

- 不创建 `draft.md`。
- 不把本文件改写成文章叙事。
- 不进入 DSH 源码、Lab、BuildPilot 或 Article 01。
- 下一允许动作仅为 `M2｜Article 00 Detailed Outline`，等待人工 Review。
