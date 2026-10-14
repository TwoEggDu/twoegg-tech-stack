# 跨仓商业游戏知识覆盖与归并审查

> 审查日期：2026-10-14（Asia/Shanghai）。交付限于导航、提纲、状态和问题登记；没有教程正文、游戏实验、网站发布、提交、推送或 PR。

以上及下文“尚未提交”等描述记录导航与提纲整合完成时的快照；后续经用户授权的提交、推送状态以 Git 历史及交付回执为准，不改变本轮知识与验证边界。

[生命周期知识地图](game-lifecycle-knowledge-map.md) · [技术系列总路由](../doc-plan.md)

## 1. 本轮版本与证据口径

| 仓库 | 分支 | 本轮实际核对的完整 HEAD | 与远程的关系 / 工作区 |
| --- | --- | --- | --- |
| TwoEggDu/twoegg-tech-stack | main | `54e4713cbd9d687460c1bc1185c0a4246ad576e9` | 本轮 `git ls-remote --heads origin main` 相同；修改前仅有既存未跟踪临时目录，保留。本报告描述该基准加本轮文档增量，尚未提交。 |
| TwoEggDu/ET-Learning-Lab | main | `53e173274a5790833c1b60ea2e8c595bbfb1070d` | 本轮远程 main 相同；有既存未提交修改。以 `git show HEAD:<path>` 的已提交快照为主要审查材料；入口文档差异另外检查，不将工作区内容冒充该 SHA。全程只读。 |

这里的“与远程相同”只指本轮查询时刻，不保证以后仍是最新。学习库源码来源记录中的 ET 官方 SHA 为 `743c635dd5b0f6f6bab549c0093382c85f3f8307`，与学习仓库 HEAD 是两个身份；本轮没有重新审计该官方实现。

学习库工作区差异中，根 README、ET 目录和 VERIFICATION 涉及工具目录整理与历史记录说明；本轮读了这三份差异。未深入审计其工具代码、其他工作区修改或未跟踪内容。知识覆盖表据已提交文档，不据正在变化的工具状态。

本报告为公开摘要：学习库来源仅列用户指定仓库的相对文档标识、提交身份和经归纳的知识边界；访问仍受该仓库权限控制，不保证公众可读。未复制源码、内部路径、账号、原始日志、用户数据或私人项目材料，也没有重新分发第三方材料。

### 状态与覆盖分开

- **内容状态**：仅规划 / 已有正文 / 待核对 / 待更新。多个状态可以并列，如“已有正文，版本待核对”。
- **覆盖判断**：已覆盖＝抽查能找到相应问题的入口及正文主干；部分覆盖＝有相邻机制但责任链或深度不完整；缺少＝本轮目录、关键词与相关正文范围内未发现等价问题主线，不表示全站每句话都没有涉及。
- **提纲补齐**只提高可导航性，不把“部分覆盖/缺少”自动改成正文已覆盖；“已有正文”也不等于技术、设备或生产验收。
- 原理说明、官方资料、源码核对、实验观察、工程建议和待验证假设分别表达；本轮仅核对文档存在、定位、结构及选定段落，没有重新核验所有文章引用。

## 2. 实际阅读范围

采用入口 → 主规划 → 索引 → 争议正文的分层阅读。以下是结构审查与定向抽读，不是全站逐篇全文审核。

### 博客

1. 读取根 AGENTS.md、CLAUDE.md 的适用规范、README 与 doc-plan。目录扫描未发现 docs 下附加 AGENTS/CLAUDE；本轮不涉及 kb。
2. 读取站点定位、全站扩展、系列规划方法，以及文章方法、提纲模板和生产流程；技术底座与执行优先级读取定位/结构和相关状态。旧文档中的数量、完成度不作为本轮事实。
3. 主规划结构审查覆盖 doc-plan 所路由的：代码质量、质量护栏、DOD、DOTS/Mass、自研 ECS、C#→CLR、Runtime 总图、HybridCLR 补桥、IL2CPP 地图、异步；引擎架构、渲染栈、UE 全栈、模式、算法、性能、源码阅读；资产序列化、打包加载、Addressables/YooAsset、裁剪、DOTS 后续、三条 Unity 6 升级；ET 前置/正文、服务端主题/ECS、技能、预算；交付、长线运营、AI、Agent、Harness、Route B、工程判断与实践。按定位、边界、标题结构读取；服务端、技能、预算、交付、长线运营、质量和工程判断进一步读取相关目录/状态。本轮未展开执行工单或课程实验目录。
4. 索引抽查：图形栈、引擎架构、Unreal、DOD、Runtime、资产序列化、后端、技能、质量、Agent、Harness、工程判断、内容生产、灰度运营、平台发布。部分短索引通过 frontmatter 自动聚合文章，不能把索引文件短误判为无内容。
5. 正文定向抽查（有些为全文，有些为目录、开篇或关键段）：反作弊运营、灾备、Bug/Playtest、玩家分析、远程配置、IAP、虚拟货币、事件契约与隐私、内容生产边界、主机发布、ET-01、GPU 硬件、引擎专业子系统、GUID/fileID/PPtr、UE 网络、Agent 总图。导航里其余文章做存在性/身份检查，未逐篇审读技术结论。
6. 对 docs/content 做主题关键词与文件名检索，检查是否已有生命周期地图、跨仓审查或产品决策等价主规划。本轮未发现，故新增三份文件。历史归档只作为原路由所指的只读存量，不更新、不复制篇级清单。

### 学习库（固定在上述学习仓库 SHA）

| 阅读层 | 本轮实际范围 | 局限 |
| --- | --- | --- |
| 总入口 | README；game-lifecycle 的 README、rpg-case-study-outline、COVERAGE-AUDIT；game-engineering/README | 读取范围/阶段/交接/状态，历史自述的检查成功未当本轮检查 |
| G01～G16 | 全部专题的章说明、二三级目录、前置与未验证范围；G09～G16 的职责和缺口定向对比 | 提纲结构审查，未逐段评审每条研究问题；不称完成教材审核 |
| foundations | README 的十专题总目录与依赖；04 运行时、06 分布式、09 性能的结构与状态 | 其余七专题未逐篇展开；不把目录覆盖当深度教程 |
| et-design | README 的十八章目录、主归属和状态；01、03、06、12、15 的结构；03 的 EntityRef、06 的锁超时段定向抽读 | 01/03 声明正文草稿，其他章声明提纲；本轮未重读源码、未核对全部机制或其保证 |
| LABS / VERIFICATION | 七项实验的计划边界、历史 StateSync 与持久化/性能未验证范围 | 只读文档记录，未打开原始日志或重跑。历史登录/进图/移动记录不能升级为玩家持久化、商业支付、容量或可靠性验收 |

下表 G 编号均指学习库 `docs/game-engineering/` 中现存专题：G01 业务架构、G02 同步、G03 Unity 客户端、G04 工具链、G05 质量、G06 运营观测、G07 发布、G08 跨职能、G09 安全信任、G10 数据实验、G11 玩法体验、G12 市场立项、G13 发行增长、G14 商业化经济、G15 平台合规、G16 连续性退役。精确文件入口见文末来源表。

## 3. 领域覆盖与归并表

“主规划”列即公开知识的长期主维护位置；学习库继续独立维护固定 ET 研究与原始实验。跨系列只分配职责，不建立第二份权威篇级目录。

| 领域与核心问题 | 学习库入口 | 博客唯一主规划 / 主归属 | 实际代表正文 | 内容状态 / 覆盖判断 | 处理决定及依据、未核实范围 |
| --- | --- | --- | --- | --- | --- |
| 立项：机会、制作能力与投入是否成立 | G12；G08 里程碑 | 新[产品决策](game-product-evidence-series-plan.md#gpd-01) | [工程健康度](../content/essays/engineering-health-metrics.md)仅为相邻组织证据 | 相邻正文已有；目标主线仅规划 / 缺少 | 新建一个产品决策系列，补立项提纲。未发现市场与现金流决策等价正文；未做市场/财务研究 |
| 玩法与用户研究：是否理解并愿意玩 | G11；G01/G03 | [产品决策](game-product-evidence-series-plan.md#gpd-02)；执行机制仍归技能 | [Bug/Playtest](../content/code-quality/bug-management-and-playtest.md)、[技能索引](../content/system-design/skill-system-series-index.md) | 已有相邻正文，研究提纲新增 / 部分覆盖 | 补衔接；现有 Playtest 以质量风险为主，不能代替招募/观察/可玩性；无用户研究结果 |
| 跨职能生产：接口、规格与责任 | G08/G04 | [交付](delivery-engineering-column-plan.md)；判断模型归[工程判断](engineering-judgment-series-plan.md) | [内容边界](../content/delivery-engineering/delivery-content-pipeline-01-boundaries.md)、[跨职能](../content/essays/cross-discipline-boundaries.md) | 已有正文 / 已覆盖主干 | 复用；不新建协作系列。外包合同、产能与真实联合验收未复核 |
| 客户端与业务架构：谁拥有状态 | G01/G03；ET 03/10 | [引擎架构](game-engine-architecture-series-plan.md)、[技能](skill-system-series-plan.md)分别负责系统与执行链 | [专业子系统](../content/system-design/game-engine-architecture-04-professional-subsystems.md) | 已有正文 / 已覆盖主干 | 复用；ET 特定对象语义与通用引擎分开，具体实现未经本轮重验 |
| 网络、同步、存储：请求与失败保证 | G02；基础 03/05/06/10；ET 07～12/15 | [服务端](game-server-topic-plan.md)，ECS 子线归其[专属规划](game-server-ecs-high-performance-series-plan.md) | [持久化边界](../content/system-design/sv-ecs-10-persistence-boundary.md)、[UE 网络](../content/system-design/ue-net-01-architecture.md) | 已有正文，部分状态待更新 / 已覆盖主干 | 复用、登记版本问题；未跑同步、数据库或规模实验 |
| 安全与玩家信任：检测到申诉恢复 | G09；ET 15 | [服务端 SV-LC-01](game-server-topic-plan.md#lifecycle-additions) | [反作弊运营](../content/system-design/game-backend-anticheat-05-operations.md)、[威胁模型](../content/system-design/game-backend-security-01-threat-model.md) | 已有正文、待更新 / 部分覆盖 | 原位修订提纲，复用威胁/检测/申诉；不另建安全系列。指标与阈值、GM/账号恢复交接待核 |
| 数据工程与产品实验：数据可信与因果边界 | G10；G06/G07 | [长线运营 V13/V11](live-ops-engineering-column-plan.md#lifecycle-additions) | [玩家分析](../content/system-design/game-backend-gaas-03-player-analytics.md)、[远程配置](../content/system-design/game-backend-gaas-01-remote-config.md)、[事件契约](../content/code-quality/game-analytics-integration-event-schema-and-privacy.md) | 已有正文，增量仅规划 / 部分覆盖 | 补质量/血缘与暴露/统计设计；已有分桶不能代表完整因果设计，未验证数据或效果 |
| 自动化与性能回归：怎样有证据放行 | G05；基础 09；ET 16/17 | [质量主线](code-quality-to-engineering-quality-series-plan.md)、[质量护栏](quality-guardrails-series-plan.md)各管其原目录 | [质量门禁](../content/code-quality/automated-testing-is-quality-gate.md)、[Baseline](../content/code-quality/baseline-budgets-in-ci.md) | 已有正文 / 已覆盖主干 | 复用；具体产品/设备/长稳和 AI 探索能力未验证 |
| 内容、构建、发布：版本集合一致 | G04/G07；ET 14/18 | [交付](delivery-engineering-column-plan.md)；资产机制归[资产规划](unity-asset-system-and-serialization-series-plan.md) | [内容边界](../content/delivery-engineering/delivery-content-pipeline-01-boundaries.md)、[多端资产](../content/delivery-engineering/delivery-content-pipeline-06-multiplatform-assets.md) | 已有正文，旧规划状态待更新 / 已覆盖主干 | 复用，不重写构建教材；无本轮产品构建或渠道验收 |
| 灰度与健康：何时扩大或止损 | G06/G07；ET 17/18 | [交付](delivery-engineering-column-plan.md)的放量与[长线运营](live-ops-engineering-column-plan.md)的趋势分别维护 | [灰度](../content/delivery-engineering/delivery-release-operations-01-canary.md)、[监控](../content/delivery-engineering/delivery-release-operations-03-monitoring.md) | 已有正文 / 已覆盖主干 | 复用；补与产品实验交点，不声称 SLO、监控有效性已验证 |
| 发行、渠道与增长：如何进入市场并维系玩家 | G13；G12 | 新[产品决策](game-product-evidence-series-plan.md#gpd-03) | [平台发布索引](../content/delivery-engineering/delivery-platform-publishing-series-index.md)是技术相邻入口 | 相邻正文已有；目标主线仅规划 / 缺少 | 补市场发行与增长提纲；个人站 SEO 规划不等于游戏获客，未做投放/合同研究 |
| 商业化、经济、支付与经营 | G14；G01/G09 | [服务端](game-server-topic-plan.md#lifecycle-additions)管资产/支付；[产品决策](game-product-evidence-series-plan.md#gpd-04)管经营 | [虚拟货币](../content/system-design/game-backend-economy-01-currency-design.md)、[支付](../content/system-design/game-backend-economy-04-iap-payment.md) | 已有正文、待核对；衔接仅规划 / 部分覆盖 | 复用经济机制，补退款对账与全成本判断；运行预算非现金预算，平台接口/事务结论未验证 |
| 平台生态、国际化、无障碍与适用义务 | G15；G03/G07 | [交付 DE-LC-01/02](delivery-engineering-column-plan.md#lifecycle-additions) | [主机发布](../content/delivery-engineering/delivery-platform-publishing-06-console.md)、[兼容质量](../content/code-quality/game-quality-06-compatibility-quality.md) | 已有局部正文、待核对 / 部分覆盖 | 补身份权益、语言生产与辅助体验验收；未核任何最新法规/认证规则，不输出强制结论 |
| 连续性、重大灾备与退役 | G16；基础 05/06/08/10 | [长线运营 LO-LC-03/04](live-ops-engineering-column-plan.md#lifecycle-additions)；技术灾备仍归服务端 | [灾难恢复](../content/system-design/game-backend-depth-04-disaster-recovery.md)、[回滚](../content/delivery-engineering/delivery-release-operations-02-rollback.md) | 灾备已有正文、待核对；退役仅规划 / 部分覆盖（退役主线缺少） | 复用备份/多区机制，补业务恢复与完整退出；无演练、账实核对或退役证据 |

### 反向检查：博客技术深线接入学习路径

| 领域 / 核心问题 | 学习库相邻入口 | 博客唯一主规划 | 实际代表入口或正文 | 内容状态 / 判断与处理 | 未核实范围 |
| --- | --- | --- | --- | --- | --- |
| 渲染/GPU：一帧成本从何而来 | G03 §3.5/3.6；基础 07 | [渲染栈](game-engine-rendering-stack-series-plan.md) | [GPU 硬件正文](../content/rendering/game-graphics-stack-06-gpu-hardware-basics.md) | 已有正文 / 博客已覆盖主干，学习库仅接口；复用导航 | 未逐篇核验图形算法、URP 或设备行为 |
| 引擎架构、Unity/Unreal：分层和跨引擎差异 | G03、ET 10；UE 无同深度主线 | [架构](game-engine-architecture-series-plan.md)、[UE 贯通](ue-fullstack-expert-series-plan.md) | [专业子系统](../content/system-design/game-engine-architecture-04-professional-subsystems.md)、[UE 网络](../content/system-design/ue-net-01-architecture.md) | 已有正文 / 博客已覆盖机制主干；只接路径 | UE 贯通实践仍需自己的状态/证据，不由文章数量证明 |
| DOTS/Mass：布局与调度代价 | 基础 07、G03；ET 03 非等价实现 | [DOD](data-oriented-runtime-series-plan.md)、[深线](dots-mass-hardware-deep-series-plan.md) | [DOD 索引](../content/engine-toolchain/data-oriented-runtime-series-index.md)、[Mass 索引](../content/system-design/unreal-mass-series-index.md) | 已有正文 / 博客已覆盖主干，学习库部分；复用 | 本轮仅索引结构，未验 Chunk、调度或性能主张 |
| 运行时内部：语言到执行 | 基础 04；ET 05 | [C#→CLR](csharp-to-clr-series-plan.md)、[Runtime](dotnet-runtime-ecosystem-master-plan.md) | [Runtime 索引](../content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md) | 已有正文 / 博客已覆盖主干；导航接深线 | 学习库基础仍为提纲，运行时/包版本未逐项核查 |
| 资产序列化：源文件、引用与运行对象 | G03/G04/G07 | [资产主规划](unity-asset-system-and-serialization-series-plan.md) | [GUID/fileID/PPtr](../content/engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md) | 已有正文 / 博客已覆盖主干；复用 | 未读取受限引擎 C++ 源码，未实验 |
| 动画、物理、游戏 AI：专业机制与执行接缝 | G03/G08；ET 11 的同步接口 | [算法](game-engine-algorithms-plan.md)管算法；[技能](skill-system-series-plan.md)管接缝 | [动画接缝](../content/system-design/skill-system-09-animation-and-presentation-decoupling.md)、[物理索引](../content/system-design/dots-physics-index.md) | 已有正文 / 部分覆盖（按专项深度），只接原路径 | 本轮未逐篇评审动画/物理/寻路/音频深度，不新造完整体系 |
| Agent/Harness：工具执行与证据闭环 | G04 §4.8、G05 §5.3 | [Agent](agent-engineering-series-plan.md)、[Harness](harness-engineering-series-plan.md) | [Agent 总图](../content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md)、[Harness 索引](../content/harness-engineering/harness-engineering-series-index.md) | 已有正文 / 博客已有独立体系，学习库应用接口；复用 | 未复审课程实验状态，不推出游戏 Agent 已可用 |
| 工程判断：结果、权责与反馈 | G08 | [工程判断](engineering-judgment-series-plan.md) | [跨职能边界](../content/essays/cross-discipline-boundaries.md) | 已有正文、部分新规划 / 部分覆盖；复用 | 组织案例与本人职责证据未核；不虚构履历 |

## 4. 十阶段覆盖矩阵

| 阶段 | 本轮前可复用基础 | 本轮处理 | 尚未解决的缺口 |
| --- | --- | --- | --- |
| L01 市场与立项 | 组织/技术预算，部分覆盖 | 新产品系列的立项提纲；导航已接入 | 市场、访谈、产能、资金与决策证据；正文未写 |
| L02 产品与玩法 | 技能/QA Playtest，部分覆盖 | 产品研究衔接提纲 | 可玩原型、招募观察、设计反证；正文未写 |
| L03 跨职能协作 | 已有内容生产/权责主干 | 直接复用、补导航 | 真实产能、许可与联合验收未验证 |
| L04 双端研发 | 引擎/服务端/资产/ET 主干已有 | 反向技术导航；安全修订和支付衔接提纲 | ET 版本差异、具体实现保证、商业业务验收 |
| L05 测试与性能回归 | 质量/性能主干已有 | 直接复用 | 真实设备、负载、长稳和 AI 探索能力 |
| L06 构建与发布 | 交付主干已有 | 复用；补平台/语言/辅助体验提纲 | 最新平台适用性、真实多端制品与地区体验验收 |
| L07 灰度与监控 | 放量/回滚/监控主干已有 | 复用；与实验和恢复衔接 | SLO、观察窗口、分群效果与监控有效性 |
| L08 玩家运营与分析 | 事件/留存/活动，部分覆盖 | V13 数据质量、V11 实验细化 | 数据血缘、因果与社区反馈效果 |
| L09 商业化与迭代 | 经济/支付，部分覆盖 | 产品发行/经营提纲、支付衔接 | 获客、全成本、退款结算、长期体验证据 |
| L10 灾备与退役 | 技术灾备已有；退役主线缺少 | 业务恢复衔接、退役提纲 | 隔离演练、账实核对、适用退出义务与回执 |

十阶段都已形成导航入口；这不是十阶段都已有正文或通过验收。

<a id="issues"></a>

## 5. 版本、状态与公开化问题登记

| 编号 | 发现与定位 | 本轮处理 / 后续证据要求 |
| --- | --- | --- |
| V01 | 博客 ET 正文及前置规划明确是 ET9；学习库固定 ET10，ET 01/03 为草稿，其余为提纲 | 在 ET 主规划登记分轨边界。ET-01 的包装配、依赖入口；EntityRef、Fiber/调度、Mailbox/CoroutineLock、RPC/Location、持久化和更新机制需按各自公开基线重新核对，不批量换名 |
| V02 | 学习库 03/06 已细分引用 Dispose、锁域/持有超时等问题；博客中同名术语不证明同语义 | 只登记待比较问题，不复制私库源码、不以 ET10 源码说明为 ET9 勘误证据；未来需找到可公开、匹配版本的一手材料 |
| S01 | 服务端主题旧表把部分已有 SV-ECS 正文标为待写；交付/长线运营开头仍是早期规划状态 | 服务端六行校正为“已有正文，待核对”，三份规划均添加限定状态说明；不批量改写未经逐篇核查的完成度。正文存在和验证状态分开 |
| S02 | 旧扩展/执行规划中的文章数与缺索引描述早于当前后端、质量等索引 | 本轮以实际文件为准，历史表保持原日期；不再建立重复索引/教材 |
| S03 | 根路由仍把历史渲染、URP、动画等主题放在只读归档；IL2CPP 路由描述“已升级系列”，文件仍呈单篇规划结构 | 保留已有路由和专属规划身份，不临时造第二主规划；登记目录粒度待后续单独治理 |
| S04 | 技术底座等旧规划含本机绝对链接，质量相关旧清单含旧栏目纯文本路径 | 属于原有问题，本轮不复制这些路径到新导航、不批量修复；新增链接全部使用当前真实相对路径。下方链接检查不把代码块/纯文本路径算作 Markdown 链接 |
| Q01 | [反作弊运营](../content/system-design/game-backend-anticheat-05-operations.md)含 FPR 命名与“误封/总封禁”分母、通用误判阈值和具体游戏平台例子 | 登记定义/来源/版本待核，原位修订提纲；本轮不输出替代阈值，不把原句当行业标准 |
| Q02 | [玩家分析](../content/system-design/game-backend-gaas-03-player-analytics.md)含行业留存基准、固定误差范围与 SQL 示例 | 待核样本/来源、窗口、事件顺序和统计口径；本轮仅复用问题结构，不将数字当产品门槛 |
| Q03 | [货币设计](../content/system-design/game-backend-economy-01-currency-design.md)的兑换方向、单 UPDATE 多货币原子性、缓存写策略表述需核对；[支付](../content/system-design/game-backend-economy-04-iap-payment.md)涉及接口与退款通知鉴别 | 登记内部一致性/平台版本问题。后续用数据库及平台官方资料、状态不变量和隔离验证支撑，不作为可靠实现模板直接复制 |
| Q04 | [灾备](../content/system-design/game-backend-depth-04-disaster-recovery.md)包含具体 RTO/RPO、复制延迟和成本/体验概括 | 来源、适用环境与测量未本轮核验；数字不转成承诺，新提纲要求恢复点及玩家业务验收 |
| Q05 | [主机发布](../content/delivery-engineering/delivery-platform-publishing-06-console.md)包含帧率、语言、功能与周期等平台要求概括 | 登记待核。新规划只列平台/地区/版本、官方来源与适用性研究项，不复制受限认证要求或给法律强制结论 |
| P01 | docs 不经 Hugo 输出也仍属于公开仓库；源码/实验引用有独立许可和脱敏条件 | 本轮只写自主归纳的规划和来源标识；不复制私库原始材料，不使用私有项目经历作案例证据 |

问题登记是后续研究队列，不是已完成技术勘误。本轮未浏览外部平台政策或学术/官方资料，因此没有新增具体法律、认证或版本机制结论。

## 6. 文档变更与唯一主维护位置

| 文件 | 变更 / 长期职责 |
| --- | --- |
| [本报告](game-lifecycle-coverage-audit.md) | 新建；保存本轮跨仓归并依据、版本、范围、问题与检查快照 |
| [生命周期地图](game-lifecycle-knowledge-map.md) | 新建；只维护阶段、阅读入口、交付物和反馈关系 |
| [产品决策主规划](game-product-evidence-series-plan.md) | 唯一新增系列；四个产品证据候选的唯一篇级目录 |
| [服务端主规划](game-server-topic-plan.md#lifecycle-additions) | 原位补安全修订、支付对账两项提纲和限定状态说明 |
| [长线运营主规划](live-ops-engineering-column-plan.md#lifecycle-additions) | 原位补四项：V13 数据、V11 实验、恢复衔接、产品退役 |
| [交付主规划](delivery-engineering-column-plan.md#lifecycle-additions) | 原位补两项：平台身份/适用性、国际化/无障碍验收 |
| [ET 主规划](et-framework-series-plan.md#version-boundary-20261014) | 仅登记 ET9/ET10 与公开证据边界，不改正文或前置系列名称 |
| [README](../README.md)、[doc-plan](../doc-plan.md) | 最小入口链接；根 doc-plan 仅加导航与系列路由，没有篇级清单 |

共四个主规划承载十二项增量提纲/细化或修订候选，不把它们都算成十二篇全新正文。未修改 content、历史归档、Agent 规则、工程代码、Hugo 配置、布局、样式或 CI。学习库没有本轮写入。

## 7. 本轮检查记录

检查只针对本轮文档和相关入口；不引用旧任务的成功回执。命令中的 `<学习库>`、`<临时输出>`为占位符，公开报告不记录本机私人路径。

| 实际检查命令 / 方法 | 结果与范围 |
| --- | --- |
| 两仓 `git status --short`、`git branch --show-current`、`git rev-parse HEAD`、`git remote -v` | 得到第 1 节基准与工作区状态；没有切分支或修改学习库 |
| 两仓 `git ls-remote --heads origin main` | 沙箱 SSH 首次拒绝；获准后只读重试成功，两仓 SHA 均与本地相同 |
| `git -C <学习库> show HEAD:<path>`；指定入口的 `git diff -- <path>` | 已提交知识快照与未提交入口变更分开核对；未执行文档里的工程命令 |
| `rg --files`、主题关键词检索；Python 读取 frontmatter、标题、目录及选定段落 | 分层盘点与去重；不能证明全站技术结论正确 |
| 修改前 `hugo --destination <临时输出>` | 沙箱不能启动已有程序；获准后 Hugo 0.157.0 extended 退出 0，1266 Pages，无 ERROR。输出在临时目录，不发布 |
| `python -`（本轮通过 stdin 执行只读检查器：比较 HEAD/当前 Markdown 链接计数，检查目标路径及显式/标题锚点） | 241 处新增链接引用，0 新增断链/锚点错误；9 份本轮文件中的既有 Markdown 链接也未检出失效。56 个去重 content 目标路径存在；不等于逐篇全文或外链审查 |
| `python -`（候选标题字段、编号和显式硬前置检查） | 12 个候选均有主问题、责任边界、前置、小节、可复用内容、证据、状态；标识无重复。产品系列显式硬前置无环，其余前置为概念/契约；延伸与反馈未算硬依赖 |
| 增量文本路径/凭据模式检查及人工公开化审阅 | 未检出新增本机绝对路径、UNC 路径或常见令牌模式；未复制受限源码、原始日志、真实用户数据和私人项目经历。模式扫描不能替代全面保密/许可审计 |
| 修改后 `hugo --destination <临时输出>` | 同一已有 Hugo 0.157.0 extended，退出 0，1266 Pages，无 ERROR；与修改前页数一致。本轮无新增 Hugo 错误，未安装依赖或发布网站 |
| `git diff --check`；`git diff --stat` / `--numstat` / `--name-only`；新文件补充扫描 | 差异检查通过；6 份已跟踪 Markdown 修改、3 份新 Markdown，共 9 份。无 content/历史归档差异；不修改文章身份字段。Git 的 LF→CRLF 提示不属于构建错误 |
| 最终两仓 `git status --short`、`git rev-parse HEAD` | 两仓 HEAD 与开始一致，学习库状态条目与开始一致；博客保留原有未跟踪临时目录，新增范围仅上述文档。无暂存、提交或推送 |

未执行：游戏实验、性能/安全/支付/容灾测试、浏览器逐页展示验收、外链逐个联网检查、所有历史 Markdown 链接修复、全站全文或许可法律审计。既有静态问题和本轮新增问题分别记录，不给新增断链免责。

## 8. 建议首批深入写作

| 顺序 | 小范围选题 | 开写前证据需求 |
| --- | --- | --- |
| 1 | GPD-01 立项门槛 | 一份可公开市场/竞品资料样本、一份带未知项的制作/现金流情景、明确停止标准；无真实数据时只写方法及标明假设的例子 |
| 2 | GPD-02 玩法研究 | 明确目标玩家、原型版本、招募条件、观察记录和反例；没有实验时不声称证明可玩性 |
| 3 | LO-LC-02 灰度与产品实验 | 官方/一手实验方法、分配与暴露的合成反例、指标护栏和分析计划；先修相关旧文口径 |
| 4 | LO-LC-04 产品退役 | 可公开停运案例、目标地区/平台义务官方来源、责任/依赖与未结交易表；不执行真实关服 |

安全指标、经济一致性和平台表述的旧文勘误应在复用这些具体结论之前完成；本轮止于登记和提纲，不继续写正文。

## 来源定位：学习库文档标识

以下相对路径均归属本报告第 1 节的 ET-Learning-Lab 提交，方便有权限的维护者复查；不是本仓相对链接，也不要求公众获得访问权。

| 标识 | 固定快照中的路径 |
| --- | --- |
| 总入口 / 地图 / 案例 / 审查 | `README.md`；`docs/game-lifecycle/README.md`；`docs/game-lifecycle/rpg-case-study-outline.md`；`docs/game-lifecycle/COVERAGE-AUDIT.md` |
| G 目录 | `docs/game-engineering/README.md` |
| G01 / G02 | `docs/game-engineering/01-gameplay-and-business-architecture.md`；`docs/game-engineering/02-multiplayer-synchronization.md` |
| G03 / G04 | `docs/game-engineering/03-unity-client-engineering.md`；`docs/game-engineering/04-game-development-toolchain.md` |
| G05 / G06 | `docs/game-engineering/05-quality-engineering-and-testing.md`；`docs/game-engineering/06-liveops-and-observability.md` |
| G07 / G08 | `docs/game-engineering/07-content-delivery-and-release.md`；`docs/game-engineering/08-cross-functional-game-development.md` |
| G09 / G10 | `docs/game-engineering/09-game-security-and-trust.md`；`docs/game-engineering/10-game-data-and-experimentation.md` |
| G11 / G12 | `docs/game-engineering/11-game-design-and-player-experience.md`；`docs/game-engineering/12-market-research-and-greenlight.md` |
| G13 / G14 | `docs/game-engineering/13-publishing-and-user-growth.md`；`docs/game-engineering/14-monetization-and-game-economy.md` |
| G15 / G16 | `docs/game-engineering/15-platform-ecosystem-and-compliance.md`；`docs/game-engineering/16-business-continuity-and-sunset.md` |
| 基础目录与抽读 | `docs/foundations/README.md`；`04-dotnet-runtime.md`、`06-distributed-systems.md`、`09-performance-engineering.md`（后三项同目录） |
| ET 目录与抽读 | `docs/et-design/README.md`；同目录 `01-design-goals-and-overview.md`、`03-entity-component-system.md`、`06-actor-mailbox-and-concurrency.md`、`12-mongodb-and-persistence.md`、`15-failures-consistency-and-security.md` |
| 实验与历史验证状态 | `docs/learning/LABS.md`；`docs/learning/VERIFICATION.md`；仅摘取状态边界，不复制原始证据 |
