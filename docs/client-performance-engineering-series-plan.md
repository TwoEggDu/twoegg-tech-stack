# 游戏客户端性能工程：专栏主规划

> 2026-11-04。本文件是“游戏客户端性能工程”专栏首页、归属与本系列新增篇目的唯一 canonical plan。跨系列旧文继续由原 plan 维护；本文件不复制它们的篇级目录和完成状态。

## 定位与分工

专栏面向客户端程序、主程、TA、工具开发者及内容验收负责人，沿共同基础、预算、机型分档、五类内容、准确测量、监测体系组织学习。目标是解释成本与指标，学会从项目条件推导预算和分档，再把可靠测量落实到测试与持续回归。

当前推进原则为**先建立约定范围的知识体系，再开展具体测量与工程实现**。知识阶段包括共同基础、预算与分档、五类内容成本、测量原理与校准设计、监测方法；通过教学推演和理解检查后再进入实测。阶段出口唯一维护在[执行计划第 2.2 节](client-performance-budget-tiering-monitoring-execution-plan.md#knowledge-stage-exit)，不以已有设备数据或工具原型作为基础学习前置。

- 公开首页：[游戏客户端性能工程](../content/performance/client-performance-engineering-series-index.md)。
- [学习路线](client-performance-budget-tiering-monitoring-outline.md)：知识范围、概念边界和旧文复用盘点。
- [执行计划](client-performance-budget-tiering-monitoring-execution-plan.md)：任务批次、依赖、验收与接续位置；第 2.1 节维护交付节点，第 2.2 节维护知识阶段完成条件，第 4.1 节维护每批交付及退出条件，P1 执行单明确首份读者正文的范围。
- [性能专栏质量标准](client-performance-quality-standard.md)：专家方向的八项审查条款、各类文章证据要求及整套作品验收；不作为网易内部职级标准。
- [官方参考资料](client-performance-official-references-2026-11-05.md)：Unity 电子书、Unity 6.0 与 UE 5.8 文档的下载来源、版本、选读位置及使用限制；[固定写作](client-performance-budget-tiering-monitoring-execution-plan.md#writing-workflow)与[性能验证工作流](client-performance-budget-tiering-monitoring-execution-plan.md#performance-workflow)统一维护在执行计划。
- 公共基础提纲：本地支撑材料 `docs/client-performance-foundations-outline.md` 整理硬件、OS、引擎等按需回读的依赖；尚待独立交付，此处不建立仓库链接。
- [特效测试 plan](unity-vfx-testing-series-plan.md)：特效专项应用；ParticleEffectProfiler 是后续测量案例。

预算、性能方法、硬件、加载、渲染与美术协作已有文章保持原系列身份。原文补强仍回原 plan；只有属于本专栏独立增量的新篇，才在下表登记，不搬迁或重编号已有文章。

## 引擎无关主线与具体实现

2026-11-04 按作者提出的方向，将引擎无关内容明确为专栏主干。已有共同模型直接复用，独立增量在本文件登记；不另建一套同义基础系列。六阶段仍描述学习顺序，下列三层描述每项知识的职责，两者不是新增的两套篇目。

| 层次 | 主要回答什么 | 写作边界 |
| --- | --- | --- |
| 通用原理与工程方法 | 工作量、执行、等待与关键路径怎样影响体验？怎样定义观测、比较数据、推导预算和校准分档？ | 先用时间线、依赖图、记录表或教学算例建立模型，关键判断不依赖 Unity API 才能成立 |
| 内容领域的成本模型 | UI、特效、角色、动画与场景的规模、更新频率、生命周期及共享资源怎样产生成本？ | 说明算法和组织方式的适用条件；例如布局失效可作为模型讨论，但某种 UI 框架的重建边界须放到实现映射核对 |
| 引擎实现与验证 | 在选定引擎、管线、平台和构建中如何取得数据、验证假设和接入回归？ | Unity 是首条具体路径，ParticleEffectProfiler 是其特效案例；其他引擎按公开证据或实际实验映射，不预填支持状态 |

“引擎无关”表示核心模型和判断可以迁移，不表示硬件、OS、渲染后端或运行时没有差异。通用篇要写明前提和反例；具体毫秒值、内存额度、工具标记、线程位置与设备结论保留环境条件。同名计数器不能直接视为同口径，GC、GPU 队列、UI 更新等实现也不做无条件的一一对应。

通用篇允许使用有身份说明的具体例子，不要求每篇都并列 Unity、Unreal 和自研引擎。机制依赖按问题回读已有硬件、OS 和引擎基础，不把完整底层课程设为统一前置。

### 已有通用正文怎样复用

下表登记阅读分工及本路线的增量，不复制原系列的篇级状态，也不表示本轮重新验证了所有外部资料和运行数据。

| 通用问题 | 已有正文 | 处理决定 |
| --- | --- | --- |
| 工作由谁完成，容量、延迟、带宽与吞吐有什么区别？ | [硬件入门](../content/performance/mobile-hardware-00-getting-started.md) | 复用共同模型；更深的 Cache、GPU、OS 机制按需链接原专题 |
| 一帧、执行、等待和反馈怎样关联？ | [一帧拆解](../content/performance/game-performance-frame-breakdown.md)、[线程与等待](../content/performance/game-performance-thread-scheduling-waits.md) | 复用指标和关键路径；R1 留在一帧原文修订，不新增同义帧入门 |
| 内存与加载为什么要分层计量？ | [内存记账](../content/performance/game-performance-memory-accounting.md)、[资源就绪](../content/performance/game-performance-read-does-not-mean-ready.md) | 复用资源/实例、持有、用途就绪与不同统计口径；新篇只补任务到观测的映射 |
| 怎样从现象走到可验证的改动？ | [诊断方法](../content/performance/game-performance-diagnosis-method.md)、[性能工作流](../content/performance/game-performance-methodology-summary.md) | 复用竞争解释、对照、正确性与取舍；不另写通用优化流程总论 |
| 预算怎样拆、怎样形成规则？ | [帧预算](../content/performance/game-budget-06-frame-budget-breakdown.md)、[预算总表](../content/performance/game-budget-16-budget-master-sheet-template.md)、[交付预算体系](../content/delivery-engineering/delivery-performance-stability-02-budget-system.md) | 交付预算篇已补同任务推导、跨资源 R0 记录并通过基础/预算方法职责的全文 AI 编辑复审；总表的指标/配置绑定缺口 R2 已关闭。实际阈值与设备校准待开展，不把本篇通过扩大为预算系列全部通过 |
| 设备能力、配置和持续状态怎样分开？ | [能力指纹](../content/performance/from-model-table-to-capability-fingerprint-android-and-pc-tiering.md)、[持续性能](../content/performance/mobile-hardware-02b-sustained-performance.md)、[分档验证矩阵](../content/code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md) | 原位落实 R3–R5 及能力/配置/状态分离、边界设备校准，不新增同义分档总论 |

预算各分项仍由[预算规划](game-budget-management-series-plan.md)维护；交付预算篇保留交付框架职责，在其原位提供完整推导样例并链接分项。资产身份、跨导入/构建/运行的采集、存储查询和告警处理由资产性能工程方向承接，其本地规划 `docs/game-asset-performance-engineering-series-plan.md` 尚待独立交付，此处不建立仓库链接；不另做一套同义资产平台规划。相关旧文的局部调整随 B/C 等批次执行，不扩成 P1 的前置。

## 首页身份与目录维护

| 页面 | 身份 | 当前状态 |
| --- | --- | --- |
| [专栏首页](../content/performance/client-performance-engineering-series-index.md) | `series_id: client-performance-engineering`；`type: reading-series`；`topic_key: performance`；索引顺序 0 | 首页已写，六阶段路线、精选入口、当前边界与下一步齐备；本地检查结果见下文 |
| 本系列独立新增正文 | 下表登记 CPE-01、CPE-02 两篇引擎无关方法文章 | CPE-01 正文已写并接入首页；CPE-02 初稿及全文 AI 编辑复审完成，保留 `draft: true`。设备实验与采集实现未执行；不把执行单元或复用旧文计为新增篇 |

首页以锚点维护六阶段：`foundations`、`budgets`、`device-tiers`、`content-domains`、`measurement`、`monitoring`；统一目录入口为 `chapters`。将来的独立章节使用 `primary_series: client-performance-engineering`，按实际登记给予唯一顺序；跨系列引用保持普通站内链接。

### 本系列新增篇级目录

下表维护篇目与顺序，状态分别记录。CPE 编号只用于维护；待写文件名使用代码标记，不产生指向空页的链接。正文成稿后再接入首页。

| 标识与规划标题 | 独立问题与交付 | 复用及排除边界 | 执行映射与状态 |
| --- | --- | --- | --- |
| CPE-01 从一次操作到性能指标：对象、事件与观测边界 | 用首开界面并出现特效的任务，连接请求、资源、实例、应用/渲染/呈现事件；交付依赖图、最小观测表及竞争解释，说明哪些只能作代理观测 | 引用一帧、线程、内存、资源就绪与诊断的已有定义；独立增量是跨对象、跨阶段的业务观测方案。主线不依赖 Prefab、GameObject、特定 UI 框架或 Profiler API，不重写加载或帧总论 | A1/A2，P1 首批；`series_order: 1`；[正文已写](../content/performance/client-performance-task-observation-model.md)，AI 编辑审阅及复审通过；无新增实测，页面检查与交付状态见执行计划 P1 记录 |
| CPE-02 两次性能测试能否比较：样本、窗口与采集扰动 | 给同一任务的两组明确标为教学的数据记录，辨认首次事件被长窗口掩盖、重复/缺失样本、配置或热状态变化、采集扰动；输出可比/不可比/证据不足及补测理由 | 引用已有均值/分位数定义及诊断对照方法，解释帧样本与独立运行的层次。公共知识关联基础 09.2/09.4；不承包统计学课程、不替代 APE-03 的跨阶段采集，也不替代 E3/VFX 的采集器校准和准确性实证 | A4/E1，P2；`series_order: 2`；[初稿已写](../content/performance/client-performance-test-comparability.md)，全文 AI 编辑复审通过，保留 `draft: true`，未转为公开成稿；[两组教学记录](client-performance-test-comparability-teaching-records.md)提供准备输入；教学算例不证明真实工具已校准 |

通用新篇不与已有七篇 VFX-T 重复计数：CPE-01 定义要观察什么，CPE-02 判断记录是否足以比较，VFX-T 将需求落实到特效采集、校准与设备验证。当前本系列登记 1 篇首页、1 篇已写正文与 1 篇已完成 AI 编辑复审的初稿；特效专项原 plan 仍登记 7 篇待写。连同首页是 10 个已明确登记的页面位置，复用旧文另计；这不是整个性能站点或专栏最终篇数。

## 旧文复用与本次增量

已核对性能主题首页、预算索引、机型分档索引、移动硬件索引以及本轮学习和执行材料。前几轮的对象正文盘点继续有效，详见学习路线。

原入口分别承担问题查阅、预算、设备或硬件专题。本次新增入口承担从必要基础到预算、分档、对象观察、测量与监测建设的连续学习路线；以原文章为精选前置，不新建同义总论。历史芯片表、未校准阈值、需要版本审校的 UI 渲染结论没有作为首页核心推荐依据。

首页明确已有文章可读与专项补强、校准实验和工具待建设的区别。各阶段只显示真实页面链接；待写内容用正文说明，不生成空文章或占位链接。站点性能主题页提供专栏入口，文章布局沿用现有阅读模板，无新增 CSS。

2026-11-04 按作者要求补做[已有正文复用与调整审查](client-performance-existing-content-review-2026-11-04.md)：完整审读首页 16 篇推荐正文及能力指纹 1 篇。基础定义已较充分，优先复用和连接；6 篇登记 8 处局部调整，R1 已随 P1 关闭，R2–R8 待执行。后续每批开稿均按[旧文检索与复用判断](article-production-workflow.md#article-reuse-preflight)，先说明已有覆盖、需要调整的位置、复用方式与真正增量；不以篇名或旧计划状态代替正文审阅。

## 当前交付记录

- 2026-11-04 P1：CPE-01 成稿并接入首页，包含任务约定、事件图、7 类观测、竞争解释、记录单与条件变化练习；R1 在一帧旧文原位修订。两轮 AI 编辑审阅关闭关键路径转移、输入 Ready 及全局成本/体验边界问题；本篇基础/方法职责通过，非人类同行评审或设备实证。作者理解待反馈；本地检查和接续见[执行计划](client-performance-budget-tiering-monitoring-execution-plan.md)的 P1 交付记录。
- P1 最终本地验收：19 项模板回归通过，Hugo 1470 页，生成页检查 1401 页、0 错误；实际浏览器核对首页/正文导航及 Mermaid，桌面与窄屏无页面横向溢出。当前交付预览位于 `.tmp/cpe01-final-20261104/`，本地端口 13869。下方 13868 固定预览及构建失败记录属于首页初次交付时的历史状态，本次已无该项构建阻断。
- 2026-11-04 引擎无关主线：核对现有基础、预算、分档、测量及资产性能工程的职责，明确通用方法/领域成本/引擎实现三层分工，登记 CPE-01/02。预算与分档优先补旧文，正文、首页页面及实验本轮均未改写或执行；后续以实际成稿接入阅读入口。
- 2026-11-04 执行安排：已将质量目标接到“共同语言、预算分档、首条测量链、第二领域与案例、持续回归”五个交付节点；共同基础先行，特效测量与 UI 复用验证按依赖接续。首批仍为 A1/A2，交付必须包含可阅读正文增量；基础正文、实测和原型状态未因计划更新而改变。
- 2026-11-04：首页与主规划已写；根路由、学习路线和执行计划补入归属链接。
- 首页完成后，Hugo 0.157.0 本地构建通过；19 项阅读模板回归通过；生成页检查覆盖 1385 页，错误为 0。首页、主规划、学习提纲与执行计划的 70 个本地文件链接目标存在，首页锚点有效。
- 浏览器检查：桌面与 390px 窄屏排版已检查；窄屏无横向溢出。性能主题页已增加首页入口，预算锚点可跳转。
- 后续工作区变动使另一组项目管理页面对 `project-management-06-dependencies-estimation-schedule.md` 的引用触发 `REF_NOT_FOUND`，整站再次构建失败。本轮未修改该组文章；交付预览使用上述已通过检查的固定构建结果，目录为 `.tmp/client-performance-home-20261104/verified-static/`，不代表当前工作区最新整体可构建。
- 首页初次交付时，A1/A2、设备实验及工具实现未推进；现 A1/A2 文稿状态见本节 P1 记录，设备实验与工具实现仍待开展。
- P1 提交交付：`ef4cd7b316edb0dd9015f584f60f40c905fe0467` 已提交推送，交付时远端 SHA 回读一致；精确候选的检查结果见执行计划 P1 记录。线上部署未核验。
- 2026-11-05 官方资料与固定流程：已取得 2 本 Unity 官方 PDF、12 页 Unity 6.0 文档和 9 页 Epic UE 5.8 文档（含索引页）；原件本地保存，版本与重点阅读见[资料索引](client-performance-official-references-2026-11-05.md)。写作与性能验证分别采用执行计划第 4.2、4.3 节流程。CPE-02 仍待写，首篇未增强；本次流程文档尚未提交，未新增引擎/设备实验。
- 2026-11-05 知识先行收口：执行计划与学习路线已统一为先完成约定知识体系，再进入实测与工程实现，并登记知识阶段的理解检查与反馈要求；此顺序替代 2026-11-04 的提前进入特效测量安排。规划版本完成，知识阶段仍待逐批推进；CPE-02 仍为下一篇，篇数及归属不变，本次未新增正文、实测或 Git 交付。
- 2026-11-05 P2 准备：完成 CPE-02 两组可复算教学记录，分别处理首次事件被长窗口稀释、运行/请求/帧层次，以及条件变化、超时、缺失和采集扰动。材料位于上述篇级条目，检查与接续见执行计划；公开正文仍待写，作者理解待反馈，实验及工具状态未改变。
- 2026-11-05 P2 初稿：CPE-02 已从准备材料组织为完整初稿，保留 `draft: true`，待下一轮复审。19 项模板回归、含草稿的 Hugo 构建及生成页检查通过；检查的是当前工作区预览，非提交候选或线上发布。详细范围与证据见执行计划“P2 CPE-02 初稿”记录；作者理解、设备实证和工具状态未改变。
- 2026-11-05 P2 复审及预算补桥：CPE-02 全文 AI 编辑复审已完成，当前状态见上表，仍保留草稿；接续 B1/B2 原位修订交付预算文章的帧预算口径，未增加篇目。局部审校、教学复算、19 项模板回归、默认 Hugo 构建及 1409 个阅读页面检查通过，详细证据与待补内存/渠道事项见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-frame-revision-20261105)。作者理解待反馈，实际测量、工具、提交推送和线上发布没有新增完成项。
- 2026-11-05 B1/B2 内存补桥：原位修订交付预算文章的内存小节及推导步骤 4，移除物理 RAM 直接换算与分类比例，复用既有切场景模型解释候选预算；局部审校、复算、19 项模板回归、默认 Hugo 构建与 1409 页阅读检查通过。来源、范围与待补项见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-memory-revision-20261105)。没有新增篇号、实际预算标定或知识掌握完成声明；Git 与线上状态未改变。
- 2026-11-05 B1/B2 包体补桥：原位修订交付预算文章的包体小节，区分平台限制与任务下载预算，核对 Apple/Google 口径并用完整必要字节的教学账本解释包体转移；微信现行额度保持待核查。局部审校、复算、19 项模板回归、默认 Hugo 构建与 1409 页阅读检查通过，范围及接续见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-package-revision-20261105)。原系列归属及本系列篇数不变；作者理解、实际测量、工具与发布状态分别保持待反馈或未执行。
- 2026-11-05 B1/B2 加载补桥：原位修订交付预算文章的加载小节和对应检查项，以必要条件共同成立定义任务终点，区分加载回调、实际反馈、超时与缺失；教学时限不作为通用推荐。局部 AI 编辑审校、复算、19 项模板回归、默认 Hugo 构建与生成页检查结果见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-loading-revision-20261105)。下一批为 CI 规则及接受边界；本系列篇数不变，作者理解待反馈，实际测量、工具、提交与发布未新增完成项。
- 2026-11-05 B3 局部补桥：原位修订交付预算文章的 CI 小节与检查项，区分静态合规、有效观测和性能接受，补三轮教学判定并复用质量门协议。审校、复算和本地站点检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-ci-revision-20261105)。下一批回到完整预算教学推导；归属与篇数不变，整篇尚未复审通过，作者理解待反馈，实际测量、CI 实现与发布未新增完成项。
- 2026-11-05 B1/B2 贯通小节：原位补同任务的体验目标、代表负载、26 ms CPU 候选配额、29 ms 组合反例及复验设计，修正篇首、检查单与复用声明；局部 AI 编辑审核和本地检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-derivation-20261105)。上一批 15 文件候选已随 `b9bb7a2` 提交推送，本批增量仅本地；跨资源规则记录与整篇复审待续，不新增篇号。作者理解待反馈，实际测量、工具与线上验收未开展。
- 2026-11-05 预算框架收口：交付预算篇新增同任务 R0 规则记录，区分目标、候选、缺证据、检查事实和接受决定；修正跨首次窗口的长间隔漏计条件，并原位补总表的指标/配置引用，关闭 R2。交付预算篇基础/预算方法职责的全文 AI 编辑复审通过；检查、分层结论与 C1 接续见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#budget-rule-record-20261105)。上一批推导及交付策略已随 `d8af7f2` 推送，线上未核验；本批按持续授权交付。不新增篇目，作者理解待反馈，实际校准、工具和团队采用仍未验证。
