# Article 00 Detailed Outline｜Agent Engineering 世界地图

- Lifecycle Status：`OUTLINE_READY`
- Outline Gate：`PASSED_WITH_NOTES`
- Evidence Dependency：`PARTIAL`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 6 PROPOSAL`）
- Article Type：`INDEX / BRIDGE`，以一张原则型抽象地图组织课程入口
- Course Weight：`S（Bridge / Overview）`
- Main Sections：`6`（另含 Opening Plan 与 Learning Check）
- Planned Figures：`2`
- Product Example Budget：正文预计 `15%—20%`，只使用小卡片
- Source Baseline：[Article Card](article-card.md)、[Research Conclusion Index](research.md)、[Definition Matrix](definition-matrix.md)、[Evidence Register](evidence.md)

> 本文件是 M2 详细提纲，不是正文。所有“开头如何说”“结尾如何收”都只记录教学意图、论证顺序和证据边界，不提供可直接发布的完整段落。

## Intended Reader Takeaway

读者遇到一个 AI / Agent 产品时，不再只问“它是不是 Agent”，而会先区分：这是模型能力、应用 / 产品、可观察 Surface，还是课程用于解释工程职责的 Host / Runtime / Harness 抽象；同时知道哪些是公开事实，哪些只是课程定义，哪些必须留待后续机制篇验证。

## Teaching Spine

1. 用“同一讨论混入产品名、行为描述和工程层名”的识别困难建立问题，而不是从术语词典开场。
2. 先切开最底层误解：Model 是可调用能力，AI Application 是承载软件边界。
3. 再处理名称混用：Copilot 是产品术语，Agent 有地图级稳定核心，Agentic 只作生态相关的程度 / 特征描述。
4. 引入本课程导航图：Product / Application 可以提供一个或多个 Surface；Surface 到内部 Host 的映射需要实现证据，再在课程职责视角定位 Harness 与 Agent Runtime。
5. 立即声明导航图的证据等级：Product 事实、课程工作定义与生态依赖术语不能使用同一种措辞强度。
6. 把 Prompt、Context、Tool、Skill、Workflow、Memory、RAG 放到地图上，但每个词只给一句定位与后续路由。
7. 用 Claude Code、Codex、DeepSeek Harness 三张小卡验证“公开入口可观察、内部架构不可反推”，不把产品当作架构样板。
8. 回到课程路径：地图只告诉读者将去哪里；要真正理解整套系统，仍必须从 Model API、Messages 与 Token 开始。

## Opening Plan｜先识别“混层”，不要先背定义

- Teaching Question：为什么一句“这是一个 Agentic Copilot，也是一套 Agent Harness”可能同时混入产品名、行为描述和工程抽象？
- Teaching Intent：让读者意识到困惑来自概念层次与证据类型混在一起，而不只是术语数量多。
- Core Thesis：Article 00 的任务不是给所有术语下终局定义，而是提供一套能判断“它在谈什么、凭什么这么说、后面去哪里学”的导航方法。
- Claim IDs：`00-C01`、`00-C02a`、`00-C04a`、`00-C06a`
- Evidence IDs：`00-E01`、`00-E02`、`00-E05`、`00-E08`
- Definition Type：混合预告；只点出 `STABLE_ABSTRACTION / PRODUCT_TERM / ECOSYSTEM_DEPENDENT / COURSE_WORKING_DEFINITION` 四类，不在开头解释完。
- Wording Strength：对已观察差异用“官方资料中至少出现这些不同用法”；对课程地图用“本课程采用”。不得写成“行业一直混乱”或“只有一种正确分层”。
- Planned Example：一行混层表达的拆标签练习；只列词和标签，不写完整故事或产品评价。
- Planned Diagram：无；把视觉注意力留给 Section 3 的主导航图。
- Scope Boundary / Not Covered：不开产品历史，不定义 Loop / State / Stop，不解释 Harness 能力清单。
- Bridge：既然名称会跨层使用，先从最稳定、最底部的 Model / Application 边界开始。

## Section 1｜Model 不等于 AI Application

- Teaching Question：一个按钮调用模型完成摘要，模型和应用各自是什么？
- Core Thesis：Model 提供依据输入生成输出的能力；AI Application 把模型调用与软件逻辑、数据、工具或界面组合成可用能力。Agent 是 Application 的一种可能形态，不是 Model 的同义词。
- Claim IDs：`00-C01`
- Evidence IDs：`00-E01`
- Definition Type：`LLM / Model = STABLE_ABSTRACTION`；`AI Application = STABLE_ABSTRACTION`
- Wording Strength：`CONFIRMED`；可使用“可以区分”“不是同一概念层”，不得推出唯一应用架构。
- Planned Example：并排两个最小框：`输入 -> Model -> 输出` 与 `UI / Logic / Data -> Model Call -> Result Handling`。第二个框只表示应用多了承载职责，不引入 Agent Loop。
- Planned Diagram：局部对照框，不计为主图；保持极简。
- Scope Boundary / Not Covered：不讲 Messages、Token、Streaming、Provider、Structured Output；这些从 Article 01—04 展开。
- Bridge：当应用开始围绕目标组织多步行动时，才有必要讨论 Agent；产品名和“Agentic”标签仍不能替代这个判断。

### Section 1 展开节奏

1. 先标出“模型能力”和“软件边界”两层。
2. 用普通摘要功能说明“使用模型”并不自动成为 Agent。
3. 用一句导航级提示引到 Agent，不提前解释 Turn / Step / Stop。

## Section 2｜Copilot、Agent、Agentic：三种词，三种用法

- Teaching Question：为什么 Copilot、Agent、Agentic 不能排成从低级到高级的三段阶梯？
- Core Thesis：Copilot 是厂商定义的产品 / 产品族名称；Agent 有可迁移的地图级工程核心；Agentic 在不同生态中描述宽泛系统、产品能力或自主程度。本课程不把三者组织成成熟度阶梯。
- Claim IDs：`00-C02a`、`00-C02b`、`00-C03`、`00-C04a`、`00-C04b`
- Evidence IDs：`00-E02`、`00-E03`、`00-E04`、`00-E05`、`00-E06`
- Definition Type：`Copilot = PRODUCT_TERM`；`Agent = STABLE_ABSTRACTION（导航级）`；`Agentic = ECOSYSTEM_DEPENDENT`，课程用法另属 `PROPOSAL`
- Wording Strength：Copilot 用法多样、Agent 稳定核心、Agentic 官方用法差异可按 `CONFIRMED` 表达；“本课程如何分类”必须使用 Proposal 语态。
- Planned Example：三列表格：`词 / 在本篇承担什么 / 不能据此推出什么`。Agent 一栏只保留 goal、model-involved decisions、actions / tools、feedback、multi-step 五个定位词。
- Planned Diagram：不画阶梯，避免视觉上制造不存在的演进关系。
- Scope Boundary / Not Covered：不判定所有 Copilot 是否为 Agent；不定义 Agent 的 Run / Turn / Step / State / Stop Condition；不穷举 Agentic 生态。
- Bridge：名称分清后，还需要一张工程地图说明用户看到的产品、具体入口和内部执行职责为什么不能混作一层。

### Section 2 展开节奏

1. 先用官方 Copilot 用法跨度拆掉“Copilot = 固定架构阶段”的假设。
2. 给 Agent 一句稳定核心，只建立辨识条件。
3. 展示 Agentic 的生态差异，并声明课程只把它当程度 / 特征描述。
4. 以“分类帮助导航，不替厂商定义产品”收束。

## Section 3｜课程导航主图：Product、Surface、Host、Harness 与 Runtime

- Teaching Question：一个面向用户的 Agent 产品、它的 CLI / IDE / Web 入口、执行内核和工程控制层为什么要分开看？
- Core Thesis：在本课程导航中，Product / Application 是面向用户的软件边界；Surface / Entry Point 是 CLI、IDE、Web 等外部可观察入口；Host 是承载或集成 Agent 执行 / Agent Runtime 的具体宿主程序、进程或运行环境；Harness 是 Runtime 周围可复用的工程控制与约束层；Agent Runtime 是承担模型调用、工具分派、循环、状态 / continuation 与停止等执行职责的内核抽象。Surface 不等于 Host，二者映射需要独立实现证据。
- Claim IDs：`00-C05`、`00-C06a`、`00-C06b`、`00-C07`
- Evidence IDs：`00-E07`、`00-E08`、`00-E09`、`00-E10`
- Definition Type：`Product / Surface = PRODUCT_TERM`；`Host / Harness / Agent Runtime = COURSE_WORKING_DEFINITION`；`harness` 的外部用法证据为 `PARTIAL`
- Wording Strength：始终使用“本课程用……指代”“导航图把……分开”；只说“已观察到的官方 harness 用法含义不同、当前样本不足以支持统一标准”，不得升级为全行业断言。
- Planned Example：同一个 Product 同时提供 terminal、IDE、Web 三个 Surface 的抽象例；不预设它们与内部 Host 的数量或映射。
- Planned Diagram：`Figure 1｜课程导航模型`，见下方图表规格。
- Scope Boundary / Not Covered：不把图当部署拓扑；不声称每个产品都有独立 Harness 模块；不列 Policy、Permission、Session、Trace、Budget、Recovery 能力；不解释 Runtime 的具体循环。
- Bridge：主图给出了纵向位置，但 Prompt、Context、Tool、Skill、Workflow、Memory、RAG 不是简单的下一层节点，需要换成横向路由视角。

### Section 3 展开节奏

1. 先拆 Product 与 Surface：产品是交付边界，Surface 是外部可观察入口，一个产品可有多个 Surface。
2. 再拆 Surface、Host 与 Runtime：Host 是承载或集成 Agent 执行的内部课程抽象；看见 CLI / IDE / Web 不能确定 Host 数量，也不能证明执行职责如何实现。
3. 用课程 Proposal 引入 Runtime 和 Harness，并紧邻放置定义等级。
4. 用 Harness 的两个不同官方用法解释“为什么课程必须声明自己的定义”，不做行业普查结论。
5. 展示 Figure 1，并在图注中写明“学习导航，不是通用运行架构”。

## Section 4｜横向术语地图：只定位，不提前讲机制

- Teaching Question：Prompt、Context、Tool、Skill、Workflow、Memory、RAG 应该挂在地图哪里，又为什么不能都画成固定架构层？
- Core Thesis：这些词描述的是输入组织、可见信息、外部能力、领域方法、流程骨架、跨步骤信息与外部知识检索等不同关注点；Article 00 只提供最低定位和正式学习路由。
- Claim IDs：`00-C08`
- Evidence IDs：`00-E11`、`00-E12`
- Definition Type：Prompt / Context / Tool / RAG 为 `STABLE_ABSTRACTION`；Skill / Workflow / Memory 为 `ECOSYSTEM_DEPENDENT`；“00 只做定位”是 `PROPOSAL`
- Wording Strength：最低定义可按 `CONFIRMED` 使用；关于封装方式、确定性、持久性和实现管线只写“不在本篇假设”；路由范围使用课程 Proposal 语态。
- Planned Example：七行定位表，每行固定三列：`最低定位 / 正式文章 / 本篇不讨论`。
- Planned Diagram：`Figure 2｜术语确定性与定义类型地图`，见下方图表规格。
- Scope Boundary / Not Covered：不讲 Prompt 模板技巧、Context packing / compression、Tool 执行管线、Skill 激活、Workflow 状态机、Memory 生命周期、RAG retrieve / rerank / cite 实现。
- Bridge：术语地图仍是抽象的；下一节用三个公开产品检查“事实能走到哪一步、推断必须在哪里停”。

### Section 4 七行定位表计划

| Term | Article 00 最低定位 | 正式展开 | 本篇停止线 |
|---|---|---:|---|
| Prompt | 给模型的任务、指令、示例与输出要求，是 Context 的一部分 | 02 | 不讲模板、评测与注入防护 |
| Context | 某一步推理时模型实际可见的信息 / token 集合 | 12—13 | 不讲 packing、compression、pollution |
| Tool | 模型可选择或请求的外部数据 / 动作能力，实际执行由应用或 Runtime 控制 | 05—07 | 不讲 schema、policy、execution pipeline |
| Skill | 可按需加载的领域说明、方法和配套资源 | 17 | 不假设所有生态采用同一封装或激活方式 |
| Workflow | 以较预定义步骤、分支和决策点推进任务的骨架 | 10 | 不讲状态机实现，不与 Agent 强行二选一 |
| Memory | 系统在步骤或会话之间保留、恢复或检索信息 / 状态的机制统称 | 14—15 | 不讲作用域、持久化和遗忘策略 |
| RAG | 检索外部知识、加入模型输入，再生成回答的技术模式 | 16 | 不展开 Filter、Rerank、Inject、Cite 管线 |

## Section 5｜三个产品小卡：公开事实不是内部架构

- Teaching Question：从 Claude Code、Codex、DeepSeek Harness 的官方公开资料中，哪些信息可以直接使用，哪些推断必须停止？
- Core Thesis：产品文档可以证明公开定位、能力和 Surface；仅凭名称、入口或界面不能证明 Surface 到 Host 的映射，也不能证明内部采用本课程的 Host / Harness / Runtime 分层。
- Claim IDs：`00-C09a`、`00-C09b`、`00-C09c`；边界复用 `00-C07`
- Evidence IDs：`00-E13`、`00-E14`、`00-E15`、`00-E10`
- Definition Type：`PRODUCT_FACT`；不把产品自称转换为课程架构事实
- Wording Strength：只使用官方直接陈述；每张卡必须同时包含 `Public Facts` 与 `Does Not Prove`。DSH 必须紧邻标注 `developer preview` 和 M1 未固定 commit。
- Planned Example：三张等宽小卡，不画产品内部架构图，不做能力打分或优劣排序。
- Planned Diagram：无；卡片总量控制在预计正文的 `15%—20%`。
- Scope Boundary / Not Covered：不做安装教程、功能巡礼、产品横评、版本承诺；不打开 DSH architecture docs / 源码，不运行 DSH，不推断插件内核。
- Bridge：产品卡证明地图是一种阅读方法，而不是答案本身；最后要把读者送回课程的第一块可验证基础——Model 调用。

### Section 5 小卡字段计划

| Product | Public Facts（最多两点） | Does Not Prove（固定一条） | Evidence |
|---|---|---|---|
| Claude Code | 官方定位为 agentic coding tool；公开 terminal / IDE / desktop / web 等 Surface 及读写代码、运行命令能力 | 不证明这些 Surface 对应几个 Host，也不证明内部 Harness / Runtime / Memory / Workflow 如何划分 | `00-E13` |
| Codex CLI | 官方公开本地仓库检查、编辑和运行命令；支持交互及脚本 / CI 场景 | 只证明 CLI Surface，不证明它如何映射到 Host / Runtime | `00-E14` |
| DeepSeek Harness | 官方自称开源 agent harness；当前为 developer preview，并公开 Web UI Surface | Product Fact、Surface 都不证明其内部等同课程 Host / Harness / Runtime 定义，也不证明已稳定运行 | `00-E15` |

## Section 6｜地图之后，为什么课程仍从 Model 开始

- Teaching Question：既然最终关心 Agent、Harness 与工程治理，为什么 Article 01 不直接进入 Harness？
- Core Thesis：Tool Runtime、Context Assembly、Memory、Permission、Trace、Checkpoint、Harness 等能力可以独立设计与测试，并在 Agent 任务执行时协作；课程选择最容易观察输入输出的 Model API 调用作为学习起点，不把它声明为这些能力唯一的技术依赖根。
- Claim IDs：`00-C01`、`00-C08`
- Evidence IDs：`00-E01`、`00-E12`；课程顺序以 canonical series plan 为边界
- Definition Type：`COURSE_NAVIGATION_PROPOSAL`
- Wording Strength：使用“课程将”“学习依赖 / 课程推进”；明确不是运行时依赖图，也不得写成所有团队构建 Agent 的唯一顺序。
- Planned Example：压缩成六站路线，不复制 45 篇目录：`Model -> Output Contract / Adapter -> Tool / Loop / Workflow -> Context / Memory / RAG / Skill -> Governance -> Harness -> Evidence-first Case / Design`。
- Planned Diagram：不新增主图；可复用 Figure 1 的“向下理解、向上组合”阅读提示。
- Scope Boundary / Not Covered：不介绍 Article 01 的 API 细节，不展开后续七个 Part 的文章摘要，不把 BuildPilot 写成已实现系统。
- Bridge：明确下一篇唯一入口为 `Article 01｜模型调用到底发生了什么：LLM、Model API、Messages 与 Token`。

### Section 6 收束计划

1. 用一句话回收辨识法：先问术语类别，再问证据边界，最后问工程职责。
2. 用六站路线说明课程为什么逐层生长。
3. 把下一学习动作收敛到 Article 01，不预写结尾金句或宣传性总结。

## Planned Figures

### Figure 1｜课程导航模型：从 Product 的多个 Surface 到内部职责

```text
User Goal
   ↓
Product / Application
   ↓ provides one or more
┌──────────┬──────────┬──────────┬──────────┬───────────┐
│CLI Surface│IDE Surface│Web Surface│CI Surface│Unity Surface│
└──────────┴──────────┴──────────┴──────────┴───────────┘
                     ⋮ mapping requires implementation evidence
Host（课程工作定义：承载或集成 Agent 执行的环境）
                     ↓ course responsibility view
Harness（课程工作定义：Runtime 周围的工程控制与约束）
                     ↓
Agent Runtime（课程工作定义：执行内核职责）
                     ↓
Model + Tool + State
                     ↓
External World
```

- Teaching Purpose：把 Product、可观察 Surface 与内部 Host 拆开，并让读者看到 Host、Harness、Runtime 属于不同阅读问题。
- Claim Coverage：`00-C05`、`00-C06b`、`00-C07`
- Caption Boundary：这是课程导航模型，不是通用部署拓扑；公开 Surface 到内部 Host 的映射需要独立实现证据，现实产品可以合并、拆分或用不同名称承载这些职责。
- Must Not Imply：Surface 与 Host 一一对应；每个系统都有独立 Harness 模块；Claude Code、Codex 或 DSH 已被证实采用此结构。

### Figure 2｜术语确定性与定义类型地图

计划使用二维分组图，而不是成熟度阶梯：横向为“事实来源 / 课程选择”，纵向按 Definition Type 分组。

| Definition Type | Terms | 图中措辞规则 |
|---|---|---|
| `STABLE_ABSTRACTION` | LLM / Model、AI Application、Agent（地图级）、Prompt、Context、Tool、RAG | 可陈述最低稳定核心，正式机制仍后置 |
| `PRODUCT_TERM` | Product、Surface / Entry Point、Copilot | 只按厂商 / 产品与可观察入口语境解释，不映射固定架构层 |
| `ECOSYSTEM_DEPENDENT` | Agentic、Skill、Workflow、Memory | 明示生态差异，不宣称唯一边界 |
| `COURSE_WORKING_DEFINITION` | Host、Agent Runtime、Harness | 必须使用“本课程采用 / 用来指代” |

- Teaching Purpose：让读者把“知道这个词”升级为“知道这个定义有多确定、从哪里来”。
- Claim Coverage：`00-C02b`、`00-C04b`、`00-C05`、`00-C06b`、`00-C07`、`00-C08`
- Caption Boundary：Definition Type 表示本课程此阶段可使用的陈述强度，不是行业成熟度评分。
- Must Not Imply：`STABLE_ABSTRACTION` 已包含全部机制；`ECOSYSTEM_DEPENDENT` 等于错误；`COURSE_WORKING_DEFINITION` 已被行业采纳。

## Learning Check Plan

### Q1｜一个摘要按钮调用 LLM，为什么不自动成为 Agent？

- Expected Reasoning：先区分 Model capability 与 AI Application；是否为 Agent 还要看是否围绕目标由模型参与推进方式，并通过行动 / 反馈处理多步任务，不能只看“用了 LLM”。
- Claims：`00-C01`、`00-C03`

### Q2｜某产品叫 Copilot，能否据此判断它比 Agent 更低一级？

- Expected Reasoning：不能。Copilot 是产品术语，官方用法可同时覆盖同步辅助和自主 Agentic 能力；本课程不把它放进固定成熟度阶梯。
- Claims：`00-C02a`、`00-C02b`

### Q3｜一个 Product 同时提供 CLI、IDE 和 Web，地图上应该怎样画？

- Expected Reasoning：把 Product / Application 画成软件交付边界，把 CLI、IDE、Web 画成多个 Surface；不能把这些 Surface 直接画成 Host 或 Runtime，公开入口到内部职责的映射需要实现证据。
- Claims：`00-C07`

### Q4｜为什么课程可以使用 Harness，却不能说它已有统一行业定义？

- Expected Reasoning：现有官方样本确实使用 harness / agent harness，但含义不同且样本不足以证明统一标准；课程只能明确声明自己的工作定义，并把它当导航抽象。
- Claims：`00-C06a`、`00-C06b`

### Q5｜读完世界地图后，为什么下一篇是 Model API，而不是 DSH 源码？

- Expected Reasoning：地图只给位置与课程学习顺序；Model API 是最简单的可观察输入输出边界，不是 Tool、Runtime、Context、Memory、Harness 等能力唯一的技术依赖根。DSH 还需要固定版本、源码位置与运行证据，不能跳过基础和证据门禁。
- Claims：`00-C01`、`00-C08`、`00-C09c`

## Claim Coverage Matrix

| Claim | Section | Evidence | Planned Usage | Wording |
|---|---|---|---|---|
| `00-C01` | Opening、1、6、Q1 / Q5 | `00-E01` | 区分 Model capability 与 AI Application，并支撑课程回到 Model 的入口 | `CONFIRMED`；“不是同一概念层”，不推出唯一架构 |
| `00-C02a` | Opening、2、Q2 | `00-E02` | 说明 Copilot 官方用法跨度及产品名的有限证明力 | `CONFIRMED`；限定“已观察到的官方用法” |
| `00-C02b` | 2、Figure 2、Q2 | `00-E03` | 课程不把 Copilot 设成架构层或前置阶段 | `PROPOSAL`；“本课程不把……” |
| `00-C03` | 2、Q1 | `00-E04` | 给 Agent 一句导航级稳定核心 | `CONFIRMED`；不展开 Run / Turn / Step / Stop 机制 |
| `00-C04a` | Opening、2 | `00-E05` | 展示 Agentic 在官方资料中的不同用法 | `CONFIRMED`；只说存在生态差异 |
| `00-C04b` | 2、Figure 2 | `00-E06` | 课程只把 Agentic 当自主行为 / 程度描述 | `PROPOSAL`；不要求其他生态采用 |
| `00-C05` | 3、Figure 1 / 2 | `00-E07` | 定位 Agent Runtime 的执行职责 | `PROPOSAL`；“本课程用……指代”，承载者依框架而异 |
| `00-C06a` | Opening、3、Q4 | `00-E08` | 解释多个真实 Harness 用法与有限样本边界 | `PARTIAL`；不得写成全行业无统一定义的确定事实 |
| `00-C06b` | 3、Figure 1 / 2、Q4 | `00-E09` | 引入课程 Harness 导航抽象 | `PROPOSAL`；不声称存在独立模块 |
| `00-C07` | 3、5、Figure 1 / 2、Q3 | `00-E10` | 区分 Product、可观察 Surface 与内部 Host；保留入口不能反推 Runtime 的原证明边界 | `PROPOSAL`；“Surface 不等于 Host，映射需实现证据” |
| `00-C08` | 4、6、Figure 2、Q5 | `00-E11`、`00-E12` | 七个横向词各一句定位与后续路由 | 最低定义 `CONFIRMED`；篇幅 / 路由为 `PROPOSAL` |
| `00-C09a` | 5 | `00-E13` | Claude Code 小卡：公开能力 / Surface 与不可推断边界 | `CONFIRMED`；只陈述公开事实 |
| `00-C09b` | 5 | `00-E14` | Codex CLI 小卡：公开能力 / 集成方式与不可推断边界 | `CONFIRMED`；只陈述公开事实 |
| `00-C09c` | 5、Q5 | `00-E15` | DSH 小卡：官方定位、preview、Web 入口与停止线 | `CONFIRMED`；紧邻版本限制，不推断内部实现 |

## Evidence Intentionally Not Surfaced

| Evidence | M2 有意不进入正文的内容 | 原因 / 后续去向 |
|---|---|---|
| `00-E01` | OpenAI SDK 对 loop ownership 的具体分配 | 会提前进入 Runtime / Framework 机制；留给 08、25 |
| `00-E02` | GitHub cloud agent 与 IDE agent mode 的详细产品差异 | 00 不做 Copilot 功能巡礼或产品横评 |
| `00-E04` | 各生态关于 planning、state、termination 的详细措辞比较 | Agent 只到导航级；留给 08—11 |
| `00-E07` | Google Agent Runtime 作为托管部署目标的具体部署语义 | 仅用于限制课程定义强度；框架 / 部署对比留给 25 |
| `00-E08` | DSH plugin architecture 的展开描述 | M1 未 pinned-source；源码与运行事实留给 28—37 |
| `00-E11` | Context token 管理、Tool 调用协议、Agent Skills 目录规范、Workflow 分类、Memory / RAG 机制细节 | 00 每词只保留一句定位；分别留给 02、05—06、10、12—17 |
| `00-E13` | Claude Code 的更多功能与 surface 细节 | 产品卡最多两点，防止例子吞掉教学主线 |
| `00-E14` | Codex CLI 的命令、模式与控制项 | 不写安装 / 使用教程，也不以本地体验替代证据 |
| `00-E15` | `everything-is-a-plugin` 的架构含义、安装命令与运行结果 | 当前只证明 README 公开定位；28—37 重新固定 commit 并验证 |

## M3 Draft Guardrails

- 开头必须保持问题导向，不写术语百科式引言。
- Section 3 必须区分 `Product / Application -> one or more Surfaces` 与内部 `Host / Harness / Runtime` 职责；不得把 Surface 直接写成 Host，也不得预设映射关系。
- `Runtime / Harness / Host` 每次首次定义都要带课程工作定义提示。
- `00-C06a` 所在段落必须同时出现“已观察到的含义不同”和“样本不足以证明统一行业结论”。
- Agent 不得展开 Run / Turn / Step / State / Stop 的内部机制。
- 七个横向词每个最多一句最低定义、一句路由 / 停止线。
- 三张产品卡合计不得超过预计正文的 `20%`；卡片必须同时写 Public Facts 与 Does Not Prove。
- Figure 1 图注必须明确“课程导航模型，不是通用部署拓扑，Surface 到 Host 的映射需要实现证据”；Figure 2 不得画成成熟度阶梯。
- 不新增未登记 Claim；若 Draft 出现新的行为性主张，必须返回 Research / Evidence Gate。
- 结尾只桥接 Article 01，不提前进入 DSH、BuildPilot 或 Harness 机制篇。

## M2 Gate Decision

- Teaching Structure Review：`PASS`
- Evidence Mapping Review：`PASS_WITH_NOTES`
- Scope Review：`PASS`
- Course Dependency Review：`PASS`
- Decision：`EVIDENCE_READY -> OUTLINE_READY`
- Non-blocking Note：`00-C06a` 继续保持 `PARTIAL`；Draft 只能使用本提纲规定的谨慎措辞。
- Next Allowed Action：`M3｜Article 00 Draft`，等待人工 Review，不自动执行。
