# 游戏交付三条阅读路径：存量内容、经历与证据盘点

> 2026-10-14，P1 文档交付。依据[站点原则与改进计划](site-entry-and-audience-plan.md)，整理“玩法到版本、资源到设备、线上问题到改进”三条路径。本文维护跨系列阅读关系与审查快照；篇级提纲仍由原系列规划维护。

## 1. 范围与证据口径

本轮读取的博客仓库为 `TwoEggDu/twoegg-tech-stack`，分支 `main`，基准完整 HEAD 为 `b2be07f9b46ea5f74d6b764a612a18f39e720235`。开始时只有既存未跟踪临时目录，保留不处理；本文描述该基准与本轮文档增量。没有重新查询远程，不据此声称当前远程相同。

[跨仓覆盖审查](game-lifecycle-coverage-audit.md)仅作为历史入口：本轮没有重新访问 ET-Learning-Lab，没有把其历史 SHA、实验状态或审查结果刷新为本轮结果。本轮也没有访问私人项目、原始日志、Unity 受限源码或运营数据。

采用“总入口 → 主规划 → 索引 → 代表正文”的分层阅读：

- 读取根规则、根路由、站点定位和 P1 验收要求；参照文章方法、提纲模板、系列规划方法与生产流程。
- 定向读取交付、资产序列化、打包加载、技能、服务端、长线运营、性能与实践演进规划的职责及相关段落；补读质量护栏、代码质量和产品证据规划相关部分。
- 阅读资产系统、引擎架构、数据导向运行时、.NET Runtime 等索引的定位与阅读结构；没有按索引中的历史篇数推断完成度。
- 下表文章读取 frontmatter、开篇、章节结构；资源粒度、压测、跨职能边界、案例占位与版本协调等争议点进一步抽读关键段。不是全站逐篇全文审核，也不是所有机制或示例代码的技术验证。第 5 节列出的底层系列只接入入口，不宣称已逐篇复审。

**状态分开记录**：“已有正文”只说明有解释内容；“框架待填”说明有页面但主体仍待补；“仅规划”没有对应正文。“已覆盖”表示主问题在抽查内容中有可用解释，不表示项目经验、平台适用性或性能结果已验证。优先级“先”表示下批先处理，“后”表示取得材料后再开展，不是自动排期。

## 2. 经历与证据索引

下表 E1～E4 是后面每个环节的经历、职责和证据字段，须与对应行合读。作者本次对职业方向的确认可用于站点定位；它不能证明某个具体项目的所有权、规模或效果。尚未提供的材料保持未知，不请求或公开原始内部资料。

| 标识 | 已确认的经历范围 / 个人责任边界 | 现有材料与状态 | 可公开证据需求 / 未核实项 |
| --- | --- | --- | --- |
| E1 引擎与工具 | 作者确认过去从事游戏引擎工具相关工作；具体规则、模块、决策与协作职责待确认 | [资产管线案例](../content/projects/case-asset-pipeline-design.md)主要为 EXPERIENCE-TODO 框架；[特效检查器](../content/projects/vfx-checker-case.md)有规则线索，但仍含简历/面试包装 | 需本人确认的一次问题、实际职责、规则输入输出、失败样例、豁免边界及授权后的检查摘要。旧文列出的源码位置本轮未访问，不作为已核验贡献证据 |
| E2 跨工种交付 | 作者确认现在主要把策划、美术、客户端和服务端的产物组成产品；某次版本的负责人和放行权待确认 | [搭建交付管线](../content/projects/case-build-system-from-zero.md)、[工具与业务协作](../content/projects/case-toolchain-business-collaboration.md)、[跨职能边界](../content/essays/cross-discipline-boundaries.md)均有明显待填段落 | 需脱敏版本清单、角色确认的输入输出、变更记录、联调失败及重验摘要；旧文的项目数量、稳定运行与效率结果尚未核实 |
| E3 上线运行与数据 | 作者确认近期接触运营数据分析、部署、压测和服务端设计；参与、实施、决策或负责的具体边界未知 | 有部署、压测、监控和分析方法正文，见路径 C；没有本轮可核验的个人项目回执 | 需适用环境、本人负责环节、版本、负载或事件口径、样本窗口、失败结果与复核摘要；容量与业务收益均未知 |
| E4 旧案例事实待核 | 尚不能把既有第一人称案例归为本次已确认经历 | [热更新事故](../content/projects/case-hotupdate-production-incident.md)、[性能优化](../content/projects/case-performance-optimization.md)有完整叙述和量化结果，未见本轮核验材料 | 需来源与授权、职责、版本设备、基线、采样及结果对应关系。未核实前不用于首页成绩或个人贡献背书；没有 TODO 不等于事实已验证 |

案例证据及改写的唯一主维护位置是[实践演进 P1 案例核对](engineering-journey-column-plan.md#p1-case-evidence)。技术机制由各原系列负责，不能在案例规划里另建同名机制提纲。

## 3. 三条路径对应表

### A. 一项玩法怎样成为可交付的版本

面向客户端、服务端、策划、美术、QA 与交付负责人。阅读顺序是产物交接顺序，不要求先读完全部技术系列。

| 环节与核心问题 | 实际文章入口 | 内容状态 / 覆盖 | 经历、职责与本环节证据缺口 | 唯一主规划 / 处理与优先级 |
| --- | --- | --- | --- | --- |
| A1 玩法目标：希望玩家理解、选择和感受到什么？ | [Bug 与 Playtest](../content/code-quality/bug-management-and-playtest.md)提供版本风险验证；不能替代用户研究 | 相邻已有正文，玩法研究仅规划 / 部分覆盖 | E2；体验目标的制定者、观察记录与可玩性反例未知 | [产品证据 GPD-02](game-product-evidence-series-plan.md#gpd-02)负责体验研究；复用已有提纲，后续取材，不新增“玩法百科” |
| A2 策划与美术输入：配置与表现怎样形成可消费的契约？ | [策划数据管线](../content/system-design/skill-system-27-designer-data-pipeline.md)、[美术交付标准](../content/system-design/skill-system-28-artist-vfx-delivery-standard.md) | 已有正文 / 已覆盖技能场景 | E1、E2；需某次字段、引用、时序和退回记录；不能从技能场景推断所有玩法已覆盖 | [技能系统](skill-system-series-plan.md)负责技能协作机制；直接复用，先 |
| A3 客户端与服务端：谁拥有状态，表现怎样对账？ | [多人执行与同步](../content/system-design/skill-system-11-multiplayer-sync.md)、[程序基础设施](../content/system-design/skill-system-29-programmer-infrastructure.md)、[Unity 与 GAS](../content/system-design/skill-system-14-unity-self-built-vs-unreal-gas.md) | 已有正文 / 已覆盖所选机制；引擎版本待核 | E2、E3；需本人负责的请求/权威/表现边界，端间时序与失败样例 | [技能系统](skill-system-series-plan.md)负责执行链；直接复用。GAS 为实现对照，不是 Unity 的硬前置 |
| A4 联调：各方用的是否为同一组代码、内容、协议与配置？ | [端服版本协调](../content/delivery-engineering/delivery-server-versioning-01-coordination.md)、[多团队协作](../content/delivery-engineering/delivery-org-governance-03-multi-team.md) | 已有正文 / 部分覆盖；前者重兼容，后者重协作节奏，仍缺串联到一次玩法验收的产物关系 | E2；需版本集合、角色确认、失败退回和变更后重验的同一条记录 | [交付 DE-P1-01](delivery-engineering-column-plan.md#de-p1-01)主维护；原位补衔接提纲，先，不另起联合验收系列 |
| A5 功能、性能与体验：通过了哪类验收？ | [技能回归](../content/system-design/skill-system-13-testing-and-regression.md)、[分档体验验收](../content/performance/game-performance-design-tier-acceptance.md)、[验证门](../content/delivery-engineering/delivery-verification-testing-02-gates.md) | 已有正文 / 已覆盖分项方法，真实联合回执缺少 | E2；需同一玩法版本的功能结果、设备分档和体验观察；三类结果不能互相代替 | [交付 V13 / DE-P1-01](delivery-engineering-column-plan.md#de-p1-01)维护交接验收关系；各机制复用原篇，先 |
| A6 发布：什么版本可放量，异常时退回什么？ | [灰度放量](../content/delivery-engineering/delivery-release-operations-01-canary.md)、[回退机制](../content/delivery-engineering/delivery-release-operations-02-rollback.md) | 已有正文，平台细节/示例待核 / 部分覆盖 | E2、E3；需版本绑定的观察与回退回执，不能用构建通过代替线上验收 | [交付 V17](delivery-engineering-column-plan.md)维护发布方法；复用，具体平台结论进入 DE-P1-03 核对后再推荐 |
| A7 反馈：上线变化是否说明玩法目标达成？ | [事件契约](../content/code-quality/game-analytics-integration-event-schema-and-privacy.md)、[玩家分析](../content/system-design/game-backend-gaas-03-player-analytics.md) | 已有正文，数据计算与因果解释待核 / 部分覆盖 | E3；需事件字典、版本/cohort、匿名观察与反例；指标变化不是玩法因果结论 | [长线运营 LO-LC-01/02](live-ops-engineering-column-plan.md#lifecycle-additions)维护数据质量与实验；复用提纲，后；产品解释反馈 A1 |

**产物与验收问题**：体验假设 → 配置/表现契约 → 权威事件链 → 版本集合 → 分项验收 → 发布记录 → 反馈结论。每次交接都应能回答“谁确认输入、失败退给谁、输入变化后哪些结果失效”。这条跨环节联系进入 DE-P1-01，本文不再展开篇级小节。

### B. 一份资源怎样到达玩家设备

面向引擎工具、TA、美术、客户端与构建维护者；优先作为站点体现既有技术深度的路径。

| 环节与核心问题 | 实际文章入口 | 内容状态 / 覆盖 | 经历、职责与本环节证据缺口 | 唯一主规划 / 处理与优先级 |
| --- | --- | --- | --- | --- |
| B1 创作规格：内容目标怎样落成资源预算与准入规则？ | [内容边界](../content/delivery-engineering/delivery-content-pipeline-01-boundaries.md)、[美术生产](../content/delivery-engineering/delivery-content-pipeline-02-art-production.md)、[资源规范](../content/delivery-engineering/delivery-content-pipeline-03-asset-standards.md) | 已有正文，量化泛称待核 / 已覆盖方法主干 | E1、E2；需规则提出者、协商过程、允许例外及验收摘要 | [交付 V02](delivery-engineering-column-plan.md)；复用，量化口径归 DE-P1-03 修订，先 |
| B2 导入与身份：源文件、导入对象、引用和运行时对象是什么关系？ | [Importer](../content/engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md)、[GUID/fileID/PPtr](../content/engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md)、[资产总索引](../content/engine-toolchain/unity-asset-system-and-serialization-series-index.md) | 已有正文 / 已覆盖阅读入口，版本机制未重验 | E1；需合法公开的版本文档和导入观察；不搬受限引擎源码 | [资产与序列化](unity-asset-system-and-serialization-series-plan.md)；直接复用，先 |
| B3 校验与依赖：资源为何被拦，错误怎样返回内容生产者？ | [资产规则](../content/code-quality/static-analysis-and-asset-quality-tools.md)、[配置验证](../content/delivery-engineering/delivery-content-pipeline-05-config-validation.md) | 已有正文 / 已覆盖规则方法，E1 实例缺证据 | E1；需通过/失败/豁免样例、误报和修复责任；检查成功不等于运行时预算达标 | [代码质量到工程质量](code-quality-to-engineering-quality-series-plan.md)维护资产规则；直接复用。个人实例回实践演进，先 |
| B4 构建与分包：边界怎样影响依赖、冗余与平台变体？ | [AB 粒度](../content/engine-toolchain/unity-assetbundle-pack-granularity-coarse-vs-fine-dependencies-redundancy.md)、[多端资源](../content/delivery-engineering/delivery-content-pipeline-06-multiplatform-assets.md) | 已有正文，平台信息待更新核对 / 已覆盖机制，平台适用性部分覆盖 | E1、E2；需版本化构建清单、依赖摘要、差分与真实目标设备 | [资产与序列化](unity-asset-system-and-serialization-series-plan.md)主维护粒度；复用。平台旧文修订独立归交付 DE-P1-03，先 |
| B5 分发与缓存：玩家拿到的内容是否属于同一发布快照？ | [版本/hash/CDN/回滚](../content/engine-toolchain/unity-assetbundle-governance-version-hash-cdn-cache-rollback.md)、[Catalog 与版本](../content/delivery-engineering/delivery-resource-pipeline-06-catalog-versioning.md) | 已有正文 / 已覆盖治理模型，案例真实性待核 | E1、E2、E4；需授权后的内容身份、清单切换、缓存矩阵与回退摘要 | [资产与序列化](unity-asset-system-and-serialization-series-plan.md)维护 Unity 治理机制；直接复用。交付通用流程为延伸 |
| B6 加载与生命周期：下载完成、对象可用与可卸载是否被混为一谈？ | [AB 加载链](../content/engine-toolchain/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md)、[加载与生命周期](../content/delivery-engineering/delivery-resource-pipeline-05-loading-lifecycle.md) | 已有正文，后者异步表述待修订 / 部分覆盖一致性 | E1；需阶段耗时、主线程活动、引用与卸载观察；未运行测量 | [交付 DE-P1-02](delivery-engineering-column-plan.md#de-p1-02)只维护旧文修订；深层机制继续复用资产系列，先 |
| B7 设备成本：为什么字节读完，首次使用仍会卡？ | [读完不等于可用](../content/performance/game-performance-read-does-not-mean-ready.md)、[一帧的链路](../content/performance/game-performance-frame-breakdown.md)、[CI 预算](../content/code-quality/baseline-budgets-in-ci.md) | 已有正文 / 已覆盖方法；设备结果未知 | E1、E4；需固定场景、设备、版本、冷热状态及 CPU/GPU/内存观察；不复用未经核实的优化成绩 | [游戏性能](game-performance-series-plan.md)主维护成本解释；直接复用，先；案例沿既有候选取材，不再新增同题 |

**产物与验收问题**：规格与规则 → 导入身份 → 依赖闭合及校验结果 → 构建清单 → 发布快照 → 加载/卸载观察 → 设备预算结果。设备反馈可以反推 B1 的预算、B4 的分包和 B6 的加载时机，不能把问题一律推给美术压缩资源。

### C. 一个线上问题怎样推动产品改进

面向客户端、服务端、QA、运营分析与交付负责人。符合作者目前接触领域，但个人成果需要另行补证据。

| 环节与核心问题 | 实际文章入口 | 内容状态 / 覆盖 | 经历、职责与本环节证据缺口 | 唯一主规划 / 处理与优先级 |
| --- | --- | --- | --- | --- |
| C1 反馈与信号：玩家问题和指标异常怎样成为可分诊的问题？ | [Bug 与 Playtest](../content/code-quality/bug-management-and-playtest.md)、[事件契约](../content/code-quality/game-analytics-integration-event-schema-and-privacy.md) | 已有正文 / 已覆盖入口方法 | E2、E3；需脱敏问题单、事件字典、时间窗及版本，不收集真实玩家身份 | [质量护栏](quality-guardrails-series-plan.md)主维护问题流；事件契约为延伸，直接复用 |
| C2 复现与观测：能否把环境、请求和运行状态关联起来？ | [服务端监控](../content/delivery-engineering/delivery-server-operations-03-monitoring.md)、[性能诊断](../content/performance/game-performance-diagnosis-method.md) | 已有正文 / 已覆盖观测框架，阈值与个人实践待核 | E3；需环境身份、复现步骤、版本与匿名观测摘要；日志存在不证明根因 | [交付 V11](delivery-engineering-column-plan.md)维护运维观测，性能系列提供客户端诊断延伸；复用 |
| C3 定位与容量：是业务、程序、环境还是压测端的限制？ | [服务端压测](../content/system-design/game-backend-depth-03-load-testing.md)、[性能诊断](../content/performance/game-performance-diagnosis-method.md) | 已有正文，统计和容量证据待修订 / 部分覆盖 | E3；需到达/并发模型、场景分布、环境差异、生成端瓶颈与采样单位；容量未知 | [服务端 SV-P1-01](game-server-topic-plan.md#sv-p1-01)；原位修订提纲，先，不另开压测系列 |
| C4 修复与退回：如何止损，如何保留重验和回退条件？ | [回退机制](../content/delivery-engineering/delivery-release-operations-02-rollback.md)、[服务端部署](../content/delivery-engineering/delivery-server-operations-01-deployment.md) | 已有正文 / 部分覆盖，平台策略与服务状态边界待核 | E2、E3；需修复差异、兼容前提、失败回退、连接/持久化状态摘要；不能预设零停机 | [交付 V11/V17](delivery-engineering-column-plan.md)；复用并归 DE-P1-03 登记版本和表述核对 |
| C5 回归：修了本次问题，是否损害其他路径？ | [技能回归](../content/system-design/skill-system-13-testing-and-regression.md)、[CI 预算](../content/code-quality/baseline-budgets-in-ci.md) | 已有正文 / 已覆盖方法，具体回归结果缺少 | E2、E3；需失败样例变成的断言、固定基线与设备/环境矩阵 | [质量护栏](quality-guardrails-series-plan.md)维护跨环节回归门槛；技能机制仍归技能系列，直接复用 |
| C6 放量：在什么观察下继续、暂停或退回？ | [灰度放量](../content/delivery-engineering/delivery-release-operations-01-canary.md) | 已有正文，比例/时间/平台举例待核 / 部分覆盖 | E3；需曝光范围、阈值来源、观察窗口与授权后的决策记录 | [交付 V17](delivery-engineering-column-plan.md)；直接复用框架，具体值不作为通用标准 |
| C7 效果复核：问题是否减少，行动项是否进入下一版？ | [复盘](../content/delivery-engineering/delivery-release-operations-05-retrospective.md)、[玩家分析](../content/system-design/game-backend-gaas-03-player-analytics.md) | 已有正文，指标与归因待核 / 部分覆盖 | E3、E4；需同口径前后窗口、混杂因素、行动项责任与复测结果；业务收益未知 | [长线运营 LO-LC-01/02](live-ops-engineering-column-plan.md#lifecycle-additions)负责效果证据，交付负责复盘流程；复用已有提纲，后，反馈 A1/B1/C1 |

**产物与验收问题**：问题单 → 复现与观测摘要 → 有反证的定位判断 → 修复/回退方案 → 回归记录 → 放量记录 → 效果与行动项。核心验收是“相同口径下问题是否改善，仍有哪些解释不能排除”，不是只确认代码已提交。

## 4. 缺口、修订与唯一主归属

本轮没有新建系列。表内编号仅为原规划的增量登记，不占用文章 slug、URL、series 或 weight。

| 决定 | 主维护记录 | 与现有内容的关系 / 未解决事项 |
| --- | --- | --- |
| 补衔接，先 | [DE-P1-01](delivery-engineering-column-plan.md#de-p1-01) | 扩充现有端服协调文章的版本集合和联合验收关系；不重写技能协作、门所有权或团队发布列车 |
| 修订，先 | [DE-P1-02](delivery-engineering-column-plan.md#de-p1-02) | 对照已有资产/性能文章修订加载生命周期旧文的异步边界；机制核对与实验尚未执行 |
| 登记口径与版本核对，先 | [DE-P1-03](delivery-engineering-column-plan.md#de-p1-03) | 资源量级、校验成本倍数、纹理/平台限制、灰度与回退适用范围、服务部署状态均需区分示意与证据；不把旧值再发布成规范 |
| 修订，先 | [SV-P1-01](game-server-topic-plan.md#sv-p1-01) | 已有压测正文包含 Bot、模型、指标和容量；补采样/聚合、负载与结果外推边界，不重复写入门篇 |
| 案例核对，先 | [实践演进 P1](engineering-journey-column-plan.md#p1-case-evidence) | 首选资源规则到交付的一个小案例；先核职责与合法证据，再改既有案例，不默认接受旧文第一人称与数字 |
| 复用已有缺口，后 | [GPD-02](game-product-evidence-series-plan.md#gpd-02)、[LO-LC-01/02](live-ops-engineering-column-plan.md#lifecycle-additions) | 玩法研究、数据质量与因果实验已有提纲；本轮未开展研究，未补重复规划 |

审查还发现：跨职能边界页面实际为待填框架，不能仅凭历史覆盖表将它算成完整协作正文；本轮以技能 27～29 的解释内容支撑 A2/A3。旧覆盖审查保留原快照，不改写历史。多团队协作、验证门和灰度文章中的“真实故事/实例”也未核验，统一进入 DE-P1-03 的来源核对，不作为作者经历。

## 5. 保留技术深度的下钻入口

以下是按问题选择的延伸，不是三条路径的统一硬前置；不复制各系列目录。

| 交付问题 | 现有技术入口 | 本轮处理 |
| --- | --- | --- |
| 内容生产与运行时怎样分层？ | [引擎架构索引](../content/system-design/game-engine-architecture-series-index.md) | 保留架构深度；按所涉子系统下钻 |
| Unity 资产为何出现引用、还原、依赖和卸载问题？ | [资产索引](../content/engine-toolchain/unity-asset-system-and-serialization-series-index.md)、[打包加载规划](unity-packaging-loading-streaming-series-plan.md) | 前者解释资产机制，后者负责打包/加载/流式策略；不合并两份主规划 |
| 玩法执行怎样映射不同引擎？ | [Unity 与 GAS](../content/system-design/skill-system-14-unity-self-built-vs-unreal-gas.md)、[UE 全栈路径](ue-fullstack-expert-series-plan.md) | 保留 Unreal 自身实现路径，不把它附属于 Unity API |
| 数据布局与调度怎样影响高密度场景？ | [数据导向运行时索引](../content/engine-toolchain/data-oriented-runtime-series-index.md) | 按问题延伸至 DOTS/Mass；不设为所有读者前置 |
| 一帧成本怎样落到渲染、GPU 和显示？ | [帧链路](../content/performance/game-performance-frame-breakdown.md)、[渲染栈规划](game-engine-rendering-stack-series-plan.md) | 保留渲染独立研究，不把所有卡顿归于渲染 |
| C# 对象、GC、线程与 I/O 的成本从何而来？ | [.NET Runtime 索引](../content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md)、[性能诊断](../content/performance/game-performance-diagnosis-method.md) | 结合所用运行时、操作系统和硬件；UE/C++ 沿自身对象与内存路径研究 |
| 动画、物理、游戏 AI 及自动化工具怎样接入？ | [引擎架构](game-engine-architecture-series-plan.md)、[技术总路由](../doc-plan.md)中的算法、Agent/Harness、工程判断 | 保留原入口与独立规划；本轮未新审这些领域的正文或实验 |

## 6. 首批写作与 P2 输入

先安排三项，均需后续明确写作任务才改正文：

1. **一个资源规则从检查到交付的真实小案例**：沿 E1 选择一条规则，准备本人职责、合法脱敏的规则输入输出、失败/豁免、实际交接与验收；优先修订既有特效检查器或资产管线案例，不预设效益数字。
2. **现有端服版本协调文章的联合验收补充**：沿 DE-P1-01 准备一组脱敏版本关系和变更导致重验的记录；材料不足时只保留研究提纲，不包装为项目复盘。
3. **服务端压测文章的证据边界修订**：沿 SV-P1-01 核对统计方法、工具官方资料和适用版本；真实容量案例还需环境、负载、采样与生成端限制证据。没有数据可以修正方法说明，不能写容量成绩。

加载边界、平台版本与旧案例真实性属于推荐前的修订队列，应与上述少量选题一并排序。P2 可先选资产索引、策划/美术/程序协作、加载到可用、性能诊断作为内容候选；这只是内容匹配结果，尚未完成推荐前技术复审。E1/E2 的待填案例与 E4 的量化叙事暂不承担首页个人成绩证明。

P1 的路径、状态、证据需求与候选归属已整理；个人材料、技术修订、P2 页面改版、P3/P4 正文与实践仍待开展。求职与顾问入口本轮未调整。

## 7. 检查记录

本轮实际执行结果如下，均针对当前文档增量；未将历史检查当作本轮结果。

| 命令 / 检查 | 结果与范围 |
| --- | --- |
| `git rev-parse HEAD`、`git status --short` | 基准为第 1 节 SHA；保留既存未跟踪临时目录。最终改动为下列 5 份 Markdown，未暂存、提交或推送 |
| `python -`（内联标准库脚本） | 检查新报告全文及四份规划新增行中的相对链接：132 处文件/显式锚点引用通过；检查 5 个增量 ID 各只有一个标题定义，显式锚点无重复；新增文本的绝对私人路径和常见凭据模式扫描无命中。扫描不等同于全面保密审计 |
| `git diff --check`；新文件空白检查 | 跟踪文件检查通过；新报告另查行尾空白。Git 提示实践演进文件后续检出可能转换 LF/CRLF，无内容或构建错误 |
| `git diff --name-only`、`git diff --stat` 及差异审读 | 仅修改四份既有规划，另新增一份路径审查。未改 content、文章身份、根路由、Agent 规则、工程代码、配置、布局、样式或 CI；历史归档未改 |
| `hugo --destination (Join-Path $env:TEMP 'twoegg-p1-reading-paths-20261014')` | 首次因沙箱拒绝启动，未进入 Hugo；获准后以同一命令完成。Hugo v0.157.0 extended，退出码 0，1266 Pages，未输出 ERROR 或 REF_NOT_FOUND。使用已有工具，输出在临时目录，未安装依赖或发布 |
| 人工核对归属、前置与状态 | 三条路径共 21 个环节；5 个新增提纲记录归交付、服务端和实践演进，产品研究与数据实验复用已有候选。反馈关系及延伸阅读未改成循环硬前置；旧案例未标成已验证 |

变更与长期主维护位置：

- 本文：三条路径与本轮审查快照，不维护权威篇级目录。
- `site-entry-and-audience-plan.md`：定位、阶段状态与本文入口。
- `delivery-engineering-column-plan.md`：DE-P1-01～03 的唯一增量提纲。
- `game-server-topic-plan.md`：SV-P1-01 的唯一修订提纲。
- `engineering-journey-column-plan.md`：EJ-P1-01 与个人案例证据状态。

未验证项：私人项目事实、全文技术正确性、官方平台最新要求、外部网页可达性、游戏运行与压测、真实设备/线上效果以及移动端阅读。Hugo 不检查 docs 内普通 Markdown 链接，已另作上述静态检查；通过构建不消除旧文已登记的风险。ET9 与 ET10 的既有版本边界保持不变，本轮未重新审查 ET 结论，不能为旧版本提供新背书。
