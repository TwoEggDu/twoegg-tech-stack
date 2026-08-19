# Article 00 Evidence Register

- Evidence Status：`PARTIAL`（组合状态；无核心 Claim 为 `BLOCKED`）
- Evidence Gate：`PASSED_WITH_NOTES`
- Claim Count：`14`
- Claim Summary：`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 6 PROPOSAL`
- Evidence Card Count：`15`
- Retrieved / Verified At：`2026-08-19（Asia/Shanghai）`

## Claim Register

| Claim ID | 可进入 M2 的收窄主张 | RQ | Status | Evidence Card |
|---|---|---|---|---|
| 00-C01 | Model 提供基于输入生成输出的能力；AI Application 把模型调用与软件逻辑、数据、工具或界面组合起来，二者不是同一概念层。 | RQ-01 | `CONFIRMED` | `00-E01` |
| 00-C02a | 官方 Copilot 用法至少覆盖通用数字伴侣、同步辅助与自主 Agentic 能力，因此产品名本身不能证明固定内部架构。 | RQ-02 | `CONFIRMED` | `00-E02` |
| 00-C02b | 本课程把 Copilot 作为产品术语，而不是 Agent 的固定前置阶段或架构层。 | RQ-02 | `PROPOSAL` | `00-E03` |
| 00-C03 | Article 00 采用的 Agent 稳定核心是：围绕目标，由模型参与决定推进方式，通过行动 / 工具与反馈处理多步任务，并在完成、限制或人工介入处停止。 | RQ-03 | `CONFIRMED` | `00-E04` |
| 00-C04a | Agentic 在官方资料中既可指包含 workflow 与 agent 的宽泛系统，也可指产品中的自主能力 / 模式；用法存在生态差异。 | RQ-04 | `CONFIRMED` | `00-E05` |
| 00-C04b | 本课程只用 Agentic 描述系统的自主行为或程度，不把它设为严格架构类型。 | RQ-04 | `PROPOSAL` | `00-E06` |
| 00-C05 | 本课程用 Agent Runtime 指代 model invocation、tool dispatch、loop、state / continuation 与 stop 等执行职责；具体归属和“Runtime”命名依框架而异。 | RQ-05 | `PROPOSAL` | `00-E07` |
| 00-C06a | 多个官方生态真实使用 harness / agent harness，但已观察到的含义并不相同；现有样本不足以支持统一行业定义。 | RQ-06 | `PARTIAL` | `00-E08` |
| 00-C06b | 本课程用 Harness 指代 Runtime 周围可复用的工程控制与约束层，并明确这是课程导航抽象。 | RQ-06 | `PROPOSAL` | `00-E09` |
| 00-C07 | 本课程区分 Product、外部可观察 Surface 与内部 Host：Host 指承载或集成 Agent 执行的宿主程序、进程或运行环境；公开 Surface 不得用于反推 Host 映射或内部 Runtime。 | RQ-07 | `PROPOSAL` | `00-E10` |
| 00-C08 | Article 00 对 Prompt、Context、Tool、Skill、Workflow、Memory、RAG 只给一句定位定义和后续路由，不讲正式机制。 | RQ-08 | `PROPOSAL` | `00-E11`、`00-E12` |
| 00-C09a | Claude Code 官方公开为可读代码库、编辑文件、运行命令，并覆盖 terminal / IDE / desktop / web 的 agentic coding tool。 | RQ-09 | `CONFIRMED` | `00-E13` |
| 00-C09b | Codex CLI 官方公开为可在本地仓库检查与编辑文件、运行命令，并可交互或用于脚本 / CI 的终端工具。 | RQ-09 | `CONFIRMED` | `00-E14` |
| 00-C09c | DeepSeek Harness 官方仓库把 DSH 定位为开源 agent harness，当前处于 developer preview，并公开 Web UI 运行入口。 | RQ-09 | `CONFIRMED` | `00-E15` |

## Evidence Cards

### Evidence 00-E01｜直接模型请求与 Agent 应用不是同一抽象

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C01`
- Claim：Model 提供生成能力；应用负责把模型调用放进更大的软件边界。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official API documentation`
- Source：[OpenAI Text generation](https://developers.openai.com/api/docs/guides/text)；[OpenAI Agents SDK](https://developers.openai.com/api/docs/guides/agents)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`OpenAI public documentation retrieved on date above`
- Reproduction：打开 Text generation 的 direct model request 段与 Agents SDK 的 agent application / loop ownership 段。
- Observation：Text generation 文档展示应用向模型发送 prompt 并获得输出；Agents SDK 文档把 Agent 描述为能规划、调用工具并保留多步状态的 application，同时区分应用自管 loop 与 SDK 管理 loop。
- Counter-evidence Searched：对照 Google Cloud Generative AI glossary 中 agent = application、model = agent component 的表述。
- Interpretation：Model 是可被调用的能力组件；Application 承担编排、数据、工具、界面或产品逻辑。Agent 是应用的一种可能形态，不是 Model 的同义词。
- Proves：可以在 00 区分 Model capability 与 AI Application。
- Does Not Prove：不能证明所有模型都只有文本输入输出，也不能给出 AI Application 的唯一软件架构。
- Limitations：OpenAI 文档是单一生态；课程只提取与 Google 资料一致的最低边界。
- Course Usage：Section 1，作为稳定抽象；更完整 API / Messages / Token 留给 01。
- BuildPilot Implication：`DEFER`；本篇不开始 BuildPilot 架构。
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E02｜Copilot 是跨产品能力的产品标签

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C02a`
- Claim：已观察到的官方 Copilot 用法跨越通用数字伴侣、同步辅助与自主 Agentic 能力。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official product pages and documentation`
- Source：[Microsoft Copilot for individuals](https://www.microsoft.com/en-us/microsoft-copilot/for-individuals/get-copilot)；[GitHub Copilot features](https://docs.github.com/en/copilot/get-started/features)；[GitHub Copilot cloud agent](https://docs.github.com/en/copilot/concepts/agents/cloud-agent/about-cloud-agent)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Current Microsoft and GitHub public product documentation`
- Reproduction：比较 Microsoft 产品页的 AI companion 定位、GitHub 的 Assistive / Agentic feature 分类，以及 cloud agent 与 IDE agent mode 的公开差异。
- Observation：Microsoft 把 Copilot 定位为数字伴侣；GitHub 在同一 Copilot 产品族内同时列出同步建议类能力和可自主工作的 Agentic 能力，并明确 cloud agent 与 IDE agent mode 是不同公开形态。
- Counter-evidence Searched：主动寻找官方通用 Copilot architecture definition；本轮找到的是产品和能力定义，没有找到可跨 Microsoft / GitHub 产品套用的统一架构规范。
- Interpretation：可确认的是官方用法的多样性；不能把“未找到”升级成全球不存在任何定义的证明。
- Proves：Copilot 名称本身不足以判断一个系统处于 Agent 之前或采用何种架构。
- Does Not Prove：不能证明所有 Copilot 产品都具备同一能力，也不能证明行业绝无统一定义。
- Limitations：样本只覆盖 Microsoft consumer Copilot 与 GitHub Copilot。
- Course Usage：Section 2，只作为产品术语与反例。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-19`

### Evidence 00-E03｜课程不把 Copilot 设为固定层级

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C02b`
- Claim：本课程把 Copilot 作为产品术语，而不是 Agent 的固定前置阶段。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`course definition decision`
- Source：[Evidence 00-E02](#evidence-00-e02copilot-是跨产品能力的产品标签)；[Article 00 Definition Matrix](definition-matrix.md)
- Repository / Commit / File / Symbol / Call Path：`TechStackShow / working tree / definition-matrix.md / N/A / N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Agent Engineering course v3.1, Article 00 M1`
- Reproduction：复核 Copilot 官方用例差异，再检查课程是否需要它承担后续机制依赖；不需要。
- Observation：同一名称可覆盖辅助与自主能力，把它固定进能力成熟度阶梯会制造错误依赖。
- Counter-evidence Searched：检查 canonical progressive definition；Copilot 只在 00 出现，没有后续机制主链职责。
- Interpretation：把 Copilot 留在 Product Term 类别，能保留真实产品用法并避免虚构架构阶段。
- Proves：`N/A`；这是课程设计选择。
- Does Not Prove：不声称官方厂商采用本课程分类。
- Limitations：若未来课程专门分析某个 Copilot 产品，需要重新按版本取证。
- Course Usage：Section 2，必须使用“本课程不把它视为固定层”语态。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E04｜Agent 的跨生态稳定工程核心

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C03`
- Claim：Agent 围绕目标，由模型参与决定推进方式，通过行动 / 工具与反馈处理多步任务，并在完成、限制或人工介入处停止。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official SDK documentation, official engineering article, official cloud documentation`
- Source：[OpenAI Agents SDK](https://developers.openai.com/api/docs/guides/agents)；[Anthropic Building effective agents](https://www.anthropic.com/engineering/building-effective-agents)；[Anthropic Trustworthy agents in practice](https://www.anthropic.com/research/trustworthy-agents)；[Google Cloud: What is an AI agent?](https://cloud.google.com/discover/what-are-ai-agents)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Public definitions current on retrieval date; Anthropic engineering article published 2024 with its own update note`
- Reproduction：对比各来源关于 goal、planning / decision、tools / actions、environment feedback、multi-step state 与 termination 的表述。
- Observation：OpenAI 强调 plan、tools、state 和 multi-step；Anthropic 强调模型动态决定过程、通过环境反馈循环并在完成或人工检查时停止；Google 强调软件系统追求目标并 reasoning / acting / observing。
- Counter-evidence Searched：记录 Anthropic 明示“Agent 可被多种方式定义”，并保留 workflow / agent 的生态区分，不追求唯一标准。
- Interpretation：交集足以支持地图级稳定核心；State、Turn、Step、Stop Condition 的精确定义仍属 Article 08。
- Proves：可以把 Agent 与单次模型调用区分为目标导向、模型介入决策、多步行动与反馈推进的系统。
- Does Not Prove：不证明所有 Agent 必须公开计划、必须拥有长期记忆、必须完全自主或必须使用某个 SDK。
- Limitations：来源仍偏当前生成式 AI / 软件 Agent；不覆盖经典 AI agent 的全部学术谱系。
- Course Usage：Section 2，只给一句 working definition；机制留给 08。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E05｜Agentic 的官方用法并非固定架构类型

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C04a`
- Claim：Agentic 在官方资料中存在宽泛系统类别与产品能力 / 模式等不同用法。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official engineering article and product documentation`
- Source：[Anthropic Building effective agents](https://www.anthropic.com/engineering/building-effective-agents)；[GitHub Copilot features](https://docs.github.com/en/copilot/get-started/features)；[Claude Code overview](https://code.claude.com/docs/en/overview)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Public terminology current on retrieval date`
- Reproduction：比较 Anthropic 的 agentic systems、GitHub 的 agentic features / agent mode、Claude Code 的 agentic coding tool。
- Observation：Anthropic 把预定义 workflow 与动态 agent 都归入 agentic systems；GitHub 用 Agentic 标记可自主工作的功能；Claude Code 用其描述 coding tool。
- Counter-evidence Searched：没有把任一来源的局部分类当作全行业规范；保留不同用法本身。
- Interpretation：Agentic 能表达行为倾向、能力或宽泛家族，但跨生态不具备稳定的层级边界。
- Proves：课程不应把 Agentic 画成 Agent 之后 / 之前的固定节点。
- Does Not Prove：不证明 Agentic 在语法上或所有语境中只能作形容词。
- Limitations：只抽样 Anthropic、GitHub / Claude 产品文档。
- Course Usage：Section 2，作为生态依赖术语。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E06｜课程把 Agentic 当作程度 / 特征描述

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C04b`
- Claim：本课程只用 Agentic 描述系统自主行为或程度，不把它设为严格类型。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`course terminology decision`
- Source：[Evidence 00-E05](#evidence-00-e05agentic-的官方用法并非固定架构类型)；[Article 00 Definition Matrix](definition-matrix.md)
- Repository / Commit / File / Symbol / Call Path：`TechStackShow / working tree / definition-matrix.md / Agentic row / N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Agent Engineering course v3.1`
- Reproduction：用不同官方用法检验固定类型是否可迁移；结果不稳定。
- Observation：程度 / 特征描述可以容纳 workflow、agent mode 和 agentic tool，不需要抹平生态差异。
- Counter-evidence Searched：检查是否有课程后续文章依赖 Agentic 作为类型；没有。
- Interpretation：这是更安全的教学约定，而非外部事实。
- Proves：`N/A`；属于课程选择。
- Does Not Prove：不要求其他作者或框架采用相同用法。
- Limitations：若引用某生态原话，仍需按该生态语境解释。
- Course Usage：Section 2，使用 Proposal 语态。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E07｜Runtime 执行职责可抽象，但命名与归属会变化

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C05`
- Claim：本课程用 Agent Runtime 组织模型调用、工具分派、循环、状态 / continuation 与停止等执行职责；该词不是跨框架固定边界。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`official SDK docs plus conflicting official term usage`
- Source：[OpenAI Agents SDK](https://developers.openai.com/api/docs/guides/agents)；[Google agents-cli deployment](https://google.github.io/agents-cli/guide/deployment/)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Current OpenAI and Google public docs`
- Reproduction：对照 OpenAI 对应用 / SDK 的 loop、tool、state、stop 责任分配，与 Google 把 Agent Runtime 作为 fully managed deployment target 的用法。
- Observation：OpenAI 明确展示执行职责可由应用或 SDK 承担；Google 的 Agent Runtime 指向托管容器部署目标，语义比课程“执行内核”更偏基础设施。
- Counter-evidence Searched：主动寻找完全一致的跨框架 Agent Runtime 定义；观察到明确冲突用法，因此不标 `CONFIRMED` 行业定义。
- Interpretation：执行职责本身适合教学抽象，但名称、模块边界与承载者必须标成课程选择。
- Proves：官方生态确实分别讨论 loop / state / tool execution / stop，也确实存在不同 Runtime 用法。
- Does Not Prove：不证明这些职责必然集中在一个模块，也不证明 OpenAI 或 Google 采用课程分层。
- Limitations：只比较两个生态；详细框架对比留给 25。
- Course Usage：Section 3，一句导航定义并标 `COURSE_WORKING_DEFINITION`。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E08｜Harness 有真实官方用法，但样本含义不一致

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C06a`
- Claim：多个官方生态使用 harness / agent harness，但当前证据不足以支持统一行业定义。
- Evidence Status：`PARTIAL`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official research article and official repository README`
- Source：[Anthropic Trustworthy agents in practice](https://www.anthropic.com/research/trustworthy-agents)；[DeepSeek Harness official repository](https://github.com/deepseek-ai/deepseek-harness)
- Repository：`deepseek-ai/deepseek-harness（仅公开 README 定位）`
- Commit / File / Symbol / Call Path：`N/A / README / N/A / N/A`（M1 明确不固定源码 revision、不研究内部调用链）
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Anthropic article published 2026-04-09; DSH developer-preview README as retrieved`
- Reproduction：比较 Anthropic 四组件说明中的 harness 与 DSH README 对整套产品的自我定位。
- Observation：Anthropic 用 harness 指 instructions 与 guardrails；DeepSeek 把 DSH 整体称为 open-source agent harness，并强调 plugin architecture 与 developer preview。
- Counter-evidence Searched：检索 Agent Runtime / Agent Harness 的其他官方用法；没有得到可证明全行业统一边界的 specification。
- Interpretation：可确认多个真实用法和语义差异；有限样本仍不足以支持“存在统一行业定义”或“不存在统一行业定义”，故为 `PARTIAL`。
- Proves：课程必须明确 Harness 不是从单一官方定义直接搬来的通用层。
- Does Not Prove：不能证明行业所有 Harness 都不同，也不能证明 DSH 内部具体承载哪些能力。
- Limitations：未做全行业普查；DSH 未 pinned-source，符合 M1 停止线。
- Course Usage：Section 3，只陈述多用法与不统一边界。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-19`

### Evidence 00-E09｜课程 Harness 导航抽象

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C06b`
- Claim：本课程用 Harness 指 Runtime 周围可复用的工程控制与约束层。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`course architecture decision`
- Source：[Evidence 00-E08](#evidence-00-e08harness-有真实官方用法但样本含义不一致)；[canonical series plan](../../../agent-engineering-series-plan.md)；[Article 00 Definition Matrix](definition-matrix.md)
- Repository / Commit / File / Symbol / Call Path：`TechStackShow / working tree / docs/agent-engineering-series-plan.md / Part V / N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Agent Engineering course v3.1`
- Reproduction：检查 24—27 的课程职责，并验证该导航词是否能承接横切工程问题而不提前定义能力清单。
- Observation：Part V 已把 Harness 作为横切能力的教学组织词；官方用法能证明其有现实语境，但不能提供统一边界。
- Counter-evidence Searched：检查是否可直接采用某一家官方定义；会过窄或误把产品自称当行业标准，因此拒绝。
- Interpretation：课程需要一个明确标注的控制层 working definition，详细能力与取舍留给 24—27。
- Proves：`N/A`；这是课程设计提案。
- Does Not Prove：不证明所有产品存在独立 Harness 模块，也不证明 DSH 等同课程抽象。
- Limitations：00 不列 Capability、Policy、Session、Trace、Budget、Recovery 细节。
- Course Usage：Section 3，只回答“为什么地图上需要这个词”。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E10｜产品 Surface 与内部 Host / Runtime 不可互相推断

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C07`
- Claim：课程区分 Product、外部可观察 Surface 与内部 Host / Runtime；CLI、IDE、Web、Desktop、CI 只能证明公开入口，不能证明其内部映射。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`official product docs plus official environment definition`
- Source：[Claude Code overview](https://code.claude.com/docs/en/overview)；[Codex CLI](https://learn.chatgpt.com/docs/codex/cli)；[Anthropic Trustworthy agents in practice](https://www.anthropic.com/research/trustworthy-agents)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Current public product docs`
- Reproduction：列出公开 surface / environment，再检查页面是否公开内部 Runtime 模块边界。
- Observation：Claude Code 明确覆盖 terminal、IDE、desktop、web；Codex CLI 公开本地仓库与 terminal / CI 用法；Anthropic 把 environment 定义为 agent 运行的产品与可访问系统。
- Counter-evidence Searched：未使用 UI、入口或产品名推导未公开模块；只记录来源直接声明。
- Interpretation：用 Surface 表示外部可观察入口，用 Host 表示承载或集成 Agent 执行的课程抽象；Surface 到 Host / Runtime 的映射需要独立实现证据。
- Proves：来源能证明公开入口、可见能力和环境差异。
- Does Not Prove：不能证明 Claude Code、Codex 或 DSH 的 Surface 对应几个 Host，也不能证明其内部按课程的 Host / Harness / Runtime 分层。
- Limitations：Surface 到 Host 的实现映射及 Host 的代码级责任要到 25、29 再取证。
- Course Usage：Section 3 与产品示例边界。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-19`

### Evidence 00-E11｜横向术语的最低一手定义

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C08`
- Claim：Prompt、Context、Tool、Skill、Workflow、Memory、RAG 均能获得地图级最低定义，但标准化程度不同。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official docs, open specification, original paper`
- Source：[Anthropic Context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)；[OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling)；[Agent Skills specification](https://github.com/agentskills/agentskills/blob/main/docs/specification.mdx)；[Anthropic Building effective agents](https://www.anthropic.com/engineering/building-effective-agents)；[Google Generative AI glossary](https://docs.cloud.google.com/docs/generative-ai/glossary)；[RAG original paper](https://arxiv.org/abs/2005.11401)
- Repository / Commit / File / Symbol / Call Path：`agentskills/agentskills / mutable main (not pinned) / docs/specification.mdx / N/A / N/A`; others `N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Public docs/spec retrieved on date; RAG paper arXiv:2005.11401v4`
- Reproduction：分别定位 prompt / context、tool calling、skill directory、workflow、memory 与 RAG 的最低定义段。
- Observation：Prompt 是指令组织问题；Context 是推理时 token 集合；Tool 是模型可请求的外部能力且由应用执行；Agent Skills 规范定义含 SKILL.md 与可选资源的目录；Anthropic workflow 强调预定义代码路径；Google 定义 memory / RAG；原始 RAG 论文把参数化模型与可检索的非参数记忆结合。
- Counter-evidence Searched：比较不同生态后，没有把 Skill、Workflow、Memory 误标为统一行业类型；Definition Matrix 对其标 `ECOSYSTEM_DEPENDENT`。
- Interpretation：足以给一句定位，但不足以在 00 讲实现管线和治理规则。
- Proves：各词可以被可靠地放到世界地图，并区分最低含义与生态差异。
- Does Not Prove：不证明所有 Skill 都用 Agent Skills 格式、所有 Workflow 都完全确定性、所有 Memory 都持久化、所有 RAG 都使用向量数据库。
- Limitations：Agent Skills main 未固定 commit，只用于公开规范现状，不承担后续源码事实。
- Course Usage：Section 4 的定义来源；具体机制不得在 00 展开。
- BuildPilot Implication：`DEFER`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E12｜横向术语只做定位与路由

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C08`
- Claim：Article 00 每个横向术语只保留一句定义与正式文章路由。
- Evidence Status：`PROPOSAL`
- Evidence Class：`DESIGN_PROPOSAL`
- Source Type：`canonical dependency and progressive-definition review`
- Source：[canonical series plan](../../../agent-engineering-series-plan.md)；[course glossary](../../glossary.md)；[Article 00 Definition Matrix](definition-matrix.md)
- Repository / Commit / File / Symbol / Call Path：`TechStackShow / working tree / docs/agent-engineering-series-plan.md / Concept Progressive Definition / N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-18（Asia/Shanghai）`
- Version Scope：`Agent Engineering course v3.1`
- Reproduction：逐词对照首次引入、正式展开文章和 Article 00 权重 `S`。
- Observation：Prompt、Tool、Workflow、Context、Memory、RAG、Skill 都已有 02、05—06、10、12、14—17 的正式职责。
- Counter-evidence Searched：检查 Article Card 是否要求 00 解释内部机制；其职责是世界地图和阅读顺序，不是机制课。
- Interpretation：把详细内容留给正式文章是课程范围选择，不是信息不足。
- Proves：`N/A`；这是课程范围提案。
- Does Not Prove：不代表后续定义已完成研究或已经 `EVIDENCE_READY`。
- Limitations：M2 仍需控制每个定位句的篇幅。
- Course Usage：Section 4 与 Part I 桥接。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-18`

### Evidence 00-E13｜Claude Code 的公开产品事实

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C09a`
- Claim：Claude Code 是可读代码、编辑文件、运行命令并覆盖多种 surface 的 agentic coding tool。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official product documentation`
- Source：[Claude Code overview](https://code.claude.com/docs/en/overview)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Claude Code public product documentation retrieved on date above`
- Reproduction：查看 overview 的产品定义、可见能力与 surfaces 列表。
- Observation：官方页面将其描述为 agentic coding tool，公开读取代码库、编辑文件、运行命令及 terminal / IDE / desktop / web 入口。
- Counter-evidence Searched：没有把 marketing 页面或界面截图当内部模块证据；使用产品文档直接声明。
- Interpretation：适合作为 coding agent 产品例子，不适合作为通用内部架构样板。
- Proves：公开能力与产品入口。
- Does Not Prove：不证明内部 Harness / Runtime / Memory / Workflow 的模块划分或实现方式。
- Limitations：产品持续更新，正式文章需保留检索日期。
- Course Usage：Section 5，一行公开事实 + 一行不可推测边界。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-19`

### Evidence 00-E14｜Codex CLI 的公开产品事实

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C09b`
- Claim：Codex CLI 可在本地仓库检查、编辑、运行命令，并支持交互与脚本 / CI 场景。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official OpenAI product documentation`
- Source：[Codex CLI](https://learn.chatgpt.com/docs/codex/cli)
- Repository / Commit / File / Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Codex CLI public documentation retrieved on date above`
- Reproduction：查看页面的 local repository、control 与 scripts / CI 段。
- Observation：官方页面公开 Codex CLI 能检查和编辑代码、运行本机工具，并可交互使用或通过 codex exec 进入重复工作流和 pipeline。
- Counter-evidence Searched：没有使用本地 Codex 运行体验替代官方产品范围，也没有从命令行入口推断内部架构。
- Interpretation：可作为 coding agent 产品入口例子；只证明公开能力。
- Proves：Codex CLI 的公开表面、能力与集成方式。
- Does Not Prove：不证明内部采用本课程定义的 Host / Harness / Runtime 层，也不证明所有 Codex surface 行为相同。
- Limitations：文档和产品版本会变化；OpenAI 事实仅使用官方域名。
- Course Usage：Section 5，一行公开事实 + 证明边界。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-19`

### Evidence 00-E15｜DeepSeek Harness 的公开官方定位

- Article：`00｜Agent Engineering 世界地图`
- Claim ID：`00-C09c`
- Claim：DSH 官方定位为开源 agent harness，处于 developer preview，并提供 Web UI 运行入口。
- Evidence Status：`CONFIRMED`
- Evidence Class：`OFFICIAL_DOC`
- Source Type：`official repository README`
- Source：[DeepSeek Harness official repository](https://github.com/deepseek-ai/deepseek-harness)
- Repository：`deepseek-ai/deepseek-harness`
- Commit：`N/A（M1 不做 pinned-source 研究）`
- File：`README.md（公开仓库首页呈现）`
- Symbol / Call Path：`N/A`
- Experiment / Fixture / Trace：`N/A`
- Retrieved / Run At：`2026-08-19（Asia/Shanghai）`
- Version Scope：`Developer preview README retrieved on date above; compatibility-breaking changes explicitly expected`
- Reproduction：打开官方仓库 README，查看产品定位、preview 警告和 npm Web UI 启动说明。
- Observation：README 将 DSH 称为 DeepSeek AI 开发的 open-source agent harness，说明 everything-is-a-plugin 方向、developer preview 状态与 `npx @deepseek-ai/dsh web` 入口。
- Counter-evidence Searched：没有打开 architecture docs、源码文件、Plugin internals 或运行 DSH；这些都属于 28—37。
- Interpretation：足以把 DSH 作为“官方自称 agent harness 的公开产品”举例，但不能拿其名称证明课程 Harness 定义。
- Proves：官方产品定位、预览状态与公开运行入口。
- Does Not Prove：不证明内部 Plugin、Profile、PromptContext、Agent Loop、Session、Tool Pipeline 的结构，也不证明运行成功或稳定性。
- Limitations：未固定 commit；开发者预览会快速变化；正式源码篇必须重新 pinned-source + runtime 取证。
- Course Usage：Section 5，只用公开定位并紧邻版本 / 推断限制。
- BuildPilot Implication：`N/A`
- Owner：`Codex`
- Verified At：`2026-08-19`

## Gate Notes

- `00-C06a` 的 `PARTIAL` 是非阻塞限制：Article 00 只需要证明存在真实但不一致的用法，并据此避免声称行业标准。
- 所有 `PROPOSAL` 都是课程 working definition 或范围选择，不允许在 M2 改写成外部事实。
- 产品例子没有源码、运行或内部架构主张。
- M1 到此停止；下一步只能是人工确认后的 `M2｜Article 00 Detailed Outline`。
