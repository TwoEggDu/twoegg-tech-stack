# 商业游戏全生命周期知识地图

> 2026-10-14；导航与提纲整合版。按阶段回答“何时需要什么知识”，不维护篇级目录，不证明任何产品已完成这些阶段。审查基准、来源与限制见[覆盖报告](game-lifecycle-coverage-audit.md)。

[技术系列总路由](../doc-plan.md)回答“怎样深入研究”。两种入口共用已有文章与唯一系列规划；本地图不是第二套教材。公开知识规划、文章、工程案例与勘误长期维护在 twoegg-tech-stack；固定 ET 工程、源码研究、实验与原始记录长期维护在 ET-Learning-Lab。本地图不复制学习库原文或运行记录。

## 如何使用

先按当前决策找阶段，再打开对应真实文章；尚未成文的主题明确指向规划。只需补当前问题的前置概念，延伸链接不是全部必读。“已有正文”只描述内容存在，技术版本、实验有效性和业务验收需分别核实。

市场调研与立项 → 产品与玩法设计 → 策划美术程序协作 → 客户端与服务端研发 → 自动化测试与性能回归 → 内容构建与版本发布 → 灰度和线上监控 → 玩家运营与数据分析 → 商业化与持续迭代 → 灾难恢复与产品退役。

这些阶段是阅读顺序，不是瀑布式项目流程。安全、数据、平台、成本、恢复和退出责任从立项即需考虑；上线观察与玩家反馈持续返回设计、架构和投入评审。

<a id="l01"></a>

## L01 市场调研与立项

- **核心问题 / 读者**：为何做、为谁做、能否做、投入何时停止；制作人、产品、主策划、技术负责人、财务与发行。
- **系列与真实入口**：[产品证据与决策规划](game-product-evidence-series-plan.md)尚无正文；先按需读[工程健康度](../content/essays/engineering-health-metrics.md)和[技术预算总论](../content/performance/game-budget-00-overview-why-budget-management-fails.md)，只用于判断组织/技术约束。组织机制归[工程判断规划](engineering-judgment-series-plan.md)。
- **缺失 / 待补**：市场/竞品样本、目标玩家、制作产能、现金流情景、Greenlight 与停止条件；技术预算不能代替资金预算。
- **交付物 / 验收问题**：立项证据包、未知项和反证、投入上限与阶段评审记录。每个重要假设是否有来源、有效日期、责任人和复核条件？
- **前置 / 反馈**：基本研发和产品语境，无 ET 硬前置；L02 的原型、L08/L09 的数据与成本反馈可改变立项，L10 的退出成本反向约束投入。

<a id="l02"></a>

## L02 产品与玩法设计

- **核心问题 / 读者**：玩家是否理解核心循环，选择和反馈是否有意义；策划、UX、用户研究、客户端、美术与 QA。
- **系列与真实入口**：体验证据归[产品决策规划](game-product-evidence-series-plan.md)；执行机制复用[技能系统规划](skill-system-series-plan.md)与[技能索引](../content/system-design/skill-system-series-index.md)；已有[Bug 与 Playtest](../content/code-quality/bug-management-and-playtest.md)提供质量闭环。
- **缺失 / 待补**：核心/长期循环、原型选型、招募偏差、定性观察、可玩性复测；Bug 测试和技能实现不能代替用户研究。
- **交付物 / 验收问题**：体验目标、规则样例、原型研究计划、可验收 GDD。是否区分观察事实、设计解释与建议，是否有反例和复测目标？
- **前置 / 反馈**：目标玩家与体验假设；L03/L04 的生产及性能限制约束设计，L08 的分析只能提供证据，不自动替代设计判断。

<a id="l03"></a>

## L03 策划、美术与程序协作

- **核心问题 / 读者**：怎样将设计意图变为可联合验收的内容；策划、美术/TA、音频、程序、QA、项目管理、外包与运营。
- **系列与真实入口**：[交付规划](delivery-engineering-column-plan.md)、[工程判断规划](engineering-judgment-series-plan.md)；读[内容/资源/配置边界](../content/delivery-engineering/delivery-content-pipeline-01-boundaries.md)、[内容管线索引](../content/delivery-engineering/delivery-content-pipeline-series-index.md)及[跨职能边界](../content/essays/cross-discipline-boundaries.md)。
- **缺失 / 待补**：主线复用；真实产能、外包许可、变更交接和联合验收案例仍待证据，不新增协作教程。
- **交付物 / 验收问题**：角色责任表、资产规格、配置/协议契约、变更与退回记录。上游交付是否可检查，退回谁处理，谁批准变更？
- **前置 / 反馈**：L02 的需求样例与目标；L05 的缺陷反馈到规格，L06 的构建失败反馈到内容生产。

<a id="l04"></a>

## L04 客户端与服务端研发

- **核心问题 / 读者**：运行、表现、权威状态和存储如何分工；客户端、引擎、服务端、网络、基础设施与安全工程师。
- **系列与真实入口**：[引擎架构规划](game-engine-architecture-series-plan.md)→[架构索引](../content/system-design/game-engine-architecture-series-index.md)；[服务端规划](game-server-topic-plan.md)→[后端索引](../content/system-design/game-backend-series-index.md)；[ET 规划](et-framework-series-plan.md)→[ET9 历史入口](../content/et-framework/et-01-is-et9-still-et-from-monorepo-to-package-based-framework.md)。专项技术沿下表按需深入。
- **缺失 / 待补**：安全主要复用并待修订；支付对账衔接、跨平台身份与权益待补。ET9 与 ET10 分轨核对，不能将框架 Demo 视为商业业务完成。
- **交付物 / 验收问题**：架构/状态所有权图、协议/存储契约、威胁与版本清单、性能预算。失败后哪个状态仍可信，哪些保证只在单线程或单进程内成立？
- **前置 / 反馈**：对象寿命、并发、网络、存储基础按问题补齐；L05 回归和 L07 事故驱动架构修订，L10 恢复目标反向约束状态设计。

<a id="l05"></a>

## L05 自动化测试与性能回归

- **核心问题 / 读者**：哪些证据支持放行，怎样发现退化；QA、自动化、性能、程序、TA 和技术负责人。
- **系列与真实入口**：[质量主规划](code-quality-to-engineering-quality-series-plan.md)、[质量护栏规划](quality-guardrails-series-plan.md)、[性能规划](game-performance-series-plan.md)；读[游戏质量索引](../content/code-quality/game-quality-series-index.md)、[自动化与质量门禁](../content/code-quality/automated-testing-is-quality-gate.md)、[Baseline](../content/code-quality/baseline-budgets-in-ci.md)。
- **缺失 / 待补**：内容主干复用；具体产品的负载、设备矩阵、长稳、AI 探索可复现性和效果待验证，不新增通用测试提纲。
- **交付物 / 验收问题**：风险/设备矩阵、测试分层、固定版本性能基线、缺陷和放行记录。缺测是否可见，样本是否可比，失败是否确实改变放行决定？
- **前置 / 反馈**：L04 契约、不变量和性能预算；L07 事故回写永久回归，体验研究与确定性测试分工明确。

<a id="l06"></a>

## L06 内容构建与版本发布

- **核心问题 / 读者**：源码、配置、资源和平台制品能否形成可追溯的兼容版本；构建/工具、客户端、服务端、TA、QA 与发布负责人。
- **系列与真实入口**：[交付规划](delivery-engineering-column-plan.md)、[资源加载规划](unity-packaging-loading-streaming-series-plan.md)；读[CI/CD 索引](../content/delivery-engineering/delivery-cicd-pipeline-series-index.md)、[平台发布索引](../content/delivery-engineering/delivery-platform-publishing-series-index.md)。
- **缺失 / 待补**：技术交付主线复用；国际化/无障碍验收、身份/权益与适用性账本补入交付规划。平台政策和许可逐项待核。
- **交付物 / 验收问题**：冻结输入、制品清单、资源/协议/配置兼容矩阵、语言与平台验收、回退预案。测试和发布是否使用同一制品，哪些数据变更不可逆？
- **前置 / 反馈**：L03 输入契约、L05 质量证据；L07 决定放量，L09 市场发行与技术发布共同评审但分别验收。

<a id="l07"></a>

## L07 灰度和线上监控

- **核心问题 / 读者**：版本是否健康，何时扩大、暂停或回滚；发布、SRE、开发、QA、运营与客服。
- **系列与真实入口**：[交付规划](delivery-engineering-column-plan.md)、[长线运营规划](live-ops-engineering-column-plan.md)；读[灰度与运营索引](../content/delivery-engineering/delivery-release-operations-series-index.md)、[灰度](../content/delivery-engineering/delivery-release-operations-01-canary.md)、[监控](../content/delivery-engineering/delivery-release-operations-03-monitoring.md)。
- **缺失 / 待补**：复用现有体系；灰度与因果实验的证据差异补 V11，重大恢复交接补长线运营，不重复造灰度系列。
- **交付物 / 验收问题**：分组/版本/观察窗口、业务成功指标、告警升级和放行记录。无数据是健康还是观测断流？服务存活是否真的代表玩家成功？
- **前置 / 反馈**：L06 制品身份、业务不变量和探测有效性；异常回 L04/L05，严重事件升级 L10，产品效果归 L08。

<a id="l08"></a>

## L08 玩家运营与数据分析

- **核心问题 / 读者**：玩家遇到什么问题，活动和迭代是否改善体验；运营、产品、数据工程/分析、客服、社区及研发。
- **系列与真实入口**：[长线运营规划](live-ops-engineering-column-plan.md)；读[玩家分析](../content/system-design/game-backend-gaas-03-player-analytics.md)、[事件契约](../content/code-quality/game-analytics-integration-event-schema-and-privacy.md)、[赛季与活动](../content/system-design/game-backend-gaas-02-season-and-events.md)。
- **缺失 / 待补**：事件质量/血缘、实验暴露与因果边界补 V13/V11；社区、触达与召回归产品决策；申诉复用安全正文并待更新。
- **交付物 / 验收问题**：指标/事件字典、质量和血缘说明、实验计划、反馈工单与决策记录。分群/窗口能否比较，流失归因是否有反证，反馈是否回到责任人？
- **前置 / 反馈**：事件语义、基础统计与隐私边界；L02 提体验假设，L07 提版本背景，L09 消费经营数据，结果反向调整设计。

<a id="l09"></a>

## L09 商业化与持续迭代

- **核心问题 / 读者**：市场发行、付费设计、经济循环和服务成本是否支持长期投入；产品、发行、增长、数值、支付、财务与运营。
- **系列与真实入口**：[产品决策规划](game-product-evidence-series-plan.md)承接缺口；[服务端规划](game-server-topic-plan.md)维护资产机制；读[虚拟货币](../content/system-design/game-backend-economy-01-currency-design.md)、[IAP 支付](../content/system-design/game-backend-economy-04-iap-payment.md)。
- **缺失 / 待补**：渠道/获客、Soft Launch、收入与成本口径、长期体验护栏；补支付退款与恢复对账衔接。已有经济和支付文的数值、平台与一致性表述先核实。
- **交付物 / 验收问题**：发行研究包、经济产消模型、订单与结算说明、版本经营复盘。归因是否等于增量，收入是否覆盖全成本，付费收益是否侵蚀体验？
- **前置 / 反馈**：L01 成本/市场假设、L02 体验目标、L08 数据质量；结果决定新内容、重新立项、收缩或退出，不自动推出投资结论。

<a id="l10"></a>

## L10 灾难恢复与产品退役

- **核心问题 / 读者**：怎样恢复可信业务，怎样结束服务并关闭责任；制作人、SRE、数据库、支付、安全、合规核对、客服与供应商管理。
- **系列与真实入口**：[长线运营规划](live-ops-engineering-column-plan.md#lifecycle-additions)维护业务连续性/退役；技术恢复复用[服务端规划](game-server-topic-plan.md)与[灾备正文](../content/system-design/game-backend-depth-04-disaster-recovery.md)，日常版本恢复读[回滚](../content/delivery-engineering/delivery-release-operations-02-rollback.md)。
- **缺失 / 待补**：可信恢复点、账号/资产/支付联合验收及完整退役责任；已有备份、多区域正文不能证明演练成功。
- **交付物 / 验收问题**：连续性目标、恢复与核对方案、退出时间表、玩家支持/退款/数据处理待核表、资源及供应商核销。恢复后谁证明账实一致，退出后谁承担未结责任？
- **前置 / 反馈**：状态不变量、依赖清单、数据/平台适用性；本阶段的成本和可恢复性从 L01/L04 就要反馈，不能等事故或关服才补。

<a id="technical"></a>

## 技术阅读导航：沿原有专题深入

以下只是主线入口，既有子系列目录继续由原规划维护。学习库对这些方向多为基础/工程接口，不能据此判定博客也缺正文；本轮不生成重复提纲。

| 技术方向 / 何时需要 | 唯一主规划或既有路由 | 真实索引 / 代表正文 | 跨仓阅读关系 |
| --- | --- | --- | --- |
| 渲染与 GPU：L02/L04/L05 | [引擎渲染栈](game-engine-rendering-stack-series-plan.md)；URP 等子线仍走[总路由](../doc-plan.md) | [图形栈索引](../content/rendering/game-graphics-stack-series-index.md)、[GPU 硬件](../content/rendering/game-graphics-stack-06-gpu-hardware-basics.md) | 学习库 G03 预算入口之后，转博客补机制 |
| 引擎架构：L03/L04 | [架构地图](game-engine-architecture-series-plan.md) | [架构索引](../content/system-design/game-engine-architecture-series-index.md)、[专业子系统](../content/system-design/game-engine-architecture-04-professional-subsystems.md) | ET 是固定框架研究，不替代通用引擎架构 |
| Unity / Unreal：L04/L06 | [UE 全栈贯通](ue-fullstack-expert-series-plan.md)、[Unity 升级](unity6-upgrade-guide-series-plan.md)各管自己的边界 | [Unreal 索引](../content/system-design/unreal-engine-series-index.md)、[UE 网络](../content/system-design/ue-net-01-architecture.md) | 学习库以 Unity + ET 为主；UE 阅读按需进入，不要求先实现 TwinArena |
| DOTS / Mass：L04/L05 | [数据导向运行时](data-oriented-runtime-series-plan.md)→[工程深线](dots-mass-hardware-deep-series-plan.md) | [DOD 索引](../content/engine-toolchain/data-oriented-runtime-series-index.md)、[Mass 索引](../content/system-design/unreal-mass-series-index.md) | ET Entity 不能仅凭 ECS 名称映射为 Chunk/Archetype 设计 |
| 运行时内部：L04/L05/L06 | [C# 到 CLR](csharp-to-clr-series-plan.md)、[Runtime 总图](dotnet-runtime-ecosystem-master-plan.md) | [Runtime 索引](../content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md) | 学习库基础 04 后按 JIT/AOT/GC/异步问题深入；具体实现另核版本 |
| 资产、序列化、加载：L03/L04/L06 | [资产系统](unity-asset-system-and-serialization-series-plan.md)、[加载与流式](unity-packaging-loading-streaming-series-plan.md) | [资产索引](../content/engine-toolchain/unity-asset-system-and-serialization-series-index.md)、[GUID/fileID/PPtr](../content/engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md) | 补 G03/G04/G07 的机制层，原始资产与产品制品分开 |
| 动画、物理、游戏 AI：L02/L04/L05 | [游戏与引擎算法](game-engine-algorithms-plan.md)；执行接缝归[技能系统](skill-system-series-plan.md) | [动画接缝](../content/system-design/skill-system-09-animation-and-presentation-decoupling.md)、[DOTS 物理](../content/system-design/dots-physics-index.md)、[AI 与技能边界](../content/system-design/skill-system-10-ai-boundary.md) | 学习库 G03/G08 的概览后按专业问题进入；这些不是模型 Agent |
| 网络、服务端、ET：L04/L07/L10 | [服务端](game-server-topic-plan.md)、[ET](et-framework-series-plan.md) | [后端索引](../content/system-design/game-backend-series-index.md)、[ET9 入口](../content/et-framework/et-01-is-et9-still-et-from-monorepo-to-package-based-framework.md) | 学习库负责固定 ET10 实验；历史 ET9 只作分轨参考 |
| 性能与质量：L02/L05/L07 | [性能判断](game-performance-series-plan.md)、[质量护栏](quality-guardrails-series-plan.md) | [预算索引](../content/performance/game-budget-management-series-index.md)、[质量索引](../content/code-quality/game-quality-series-index.md) | 复用 G05/基础 09 的问题入口，真机/负载结论仍需独立证据 |
| 交付与运营：L03/L06～L10 | [交付](delivery-engineering-column-plan.md)、[长线运营](live-ops-engineering-column-plan.md) | [交付入口](../content/delivery-engineering/_index.md)、[运营 AI 场景](../content/live-ops-engineering/liveops-ai-series-index.md) | 将 G04～G08 接到已有文章；运营 AI 子组不代表全专栏完成 |
| AI / Agent / Harness：L03～L08 | [AI 赋能](ai-empowerment-series-plan.md)、[Agent](agent-engineering-series-plan.md)、[Harness](harness-engineering-series-plan.md)各为唯一主规划 | [Agent 索引](../content/ai-empowerment/agent-engineering-series-index.md)、[Harness 索引](../content/harness-engineering/harness-engineering-series-index.md) | G04/G05 只覆盖应用/测试接口；课程实验状态不外推为游戏自动化能力 |
| 工程判断：全部阶段 | [工程判断](engineering-judgment-series-plan.md) | [判断索引](../content/essays/engineering-judgment-series-index.md)、[跨职能边界](../content/essays/cross-discipline-boundaries.md) | G08 协作提纲可互读；真实职责与项目成果需另核授权和证据 |

## 前置、反馈和维护约定

生命周期阶段不互为“全部完成”的硬前置。建议最小路径：L01/L02 明确假设 → L03/L04 形成可验收契约 → L05/L06 获得候选版本证据 → L07 决定推广 → L08/L09 评估后续投入。L10 与安全、数据、平台约束全程参与。

系列变更先更新唯一主规划；这里仅更新阶段入口和缺口摘要。审查报告记录的是本轮快照，后续不能拿其历史状态代替重新盘点。外部资料、源码核对、实验观察、工程建议和待验证假设应在未来正文分别标注。
