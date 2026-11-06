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
| 设备能力、配置和持续状态怎样分开？ | [能力指纹](../content/performance/from-model-table-to-capability-fingerprint-android-and-pc-tiering.md)、[体验一致性](../content/performance/device-tier-experience-consistency.md)、[持续性能](../content/performance/mobile-hardware-02b-sustained-performance.md)、[热管控](../content/performance/mobile-hardware-03-power-thermal.md)、[分档验证矩阵](../content/code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md) | R3–R5 已局部修订并复审；C1/C2 已补能力选样及候选画质，C3 已补动态状态推演、四维校准教学矩阵与重标定触发。接续 D1 知识补强；作者理解待反馈，具体实现及真实边界待验证，纸面决策不等于配置通过，不新增同义总论 |

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

2026-11-04 按作者要求补做[已有正文复用与调整审查](client-performance-existing-content-review-2026-11-04.md)：完整审读首页 16 篇推荐正文及能力指纹 1 篇。基础定义已较充分，优先复用和连接；6 篇登记 8 处局部调整，R1 随 P1、R2 随 B、R3–R5 随 C、R6 随 D3 首批修订并复审关闭，R7/R8 待执行。后续每批开稿均按[旧文检索与复用判断](article-production-workflow.md#article-reuse-preflight)，先说明已有覆盖、需要调整的位置、复用方式与真正增量；不以篇名或旧计划状态代替正文审阅。

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
- 2026-11-05 C1 局部补桥：能力指纹原位补四类记录、兼容异常分支、候选映射教学伪代码与同任务条件变化，关闭 R3/R4；分档入口仅同步相关介绍。局部 AI 编辑复审、来源和本地检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#device-capability-boundaries-20261105)。预算上一批已随 `1fadcc6` 推送；本批按持续授权交付，不改变原分档专题身份，不新增篇目。C1 详细能力矩阵、C2/C3 与 R5 尚待推进，作者理解待反馈，实际标定、实现和线上部署未核验。
- 2026-11-05 分档验证口径：原位补日志配置绑定、六类指标、缺失状态、首用跨窗教学例及持续运行设计，关闭 R5；能力指纹同步去除过期的“指标口径待补”说明。局部 AI 编辑复审、复算与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#device-tier-metric-protocol-20261105)，原归属回写 Code Quality plan。上一批已随 `cd34f4f` 推送，本批按持续授权交付；C1 详细能力矩阵与 C2/C3 待续，知识理解待作者反馈，实际采集、校准、工具和线上部署未新增验收。
- 2026-11-05 C1 能力矩阵与选样：能力指纹新增四份虚构候选记录、六项拟验证问题及集合复算，连接必要体验、证据状态和未覆盖项处理；验证文原位修正固定台数/smoke 数量建议。局部 AI 编辑复审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#device-capability-matrix-20261105)。上一批 R5 已随 `0a60d99` 推送；本批按持续授权交付，不增篇目。下一项为 C2 通用候选画质矩阵，具体 URP 旧参数和检测代码仍待按选用范围核查；作者理解待反馈，真实设备结论与工具实现未完成。
- 2026-11-05 C2 候选画质矩阵：在体验一致性旧文原位补同任务 K0/Kp/Ku 三个未验证候选、必要体验表、名义像素复算、能力/用户请求/实际配置关系及维护组合空间；保留原机型分档身份、标题、URL 与旧 ARPG 对比。局部 AI 编辑复审、检查和 C3 接续见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#candidate-quality-matrix-20261105)。上一批 C1 已随 `ede05b4` 推送，本批按持续授权交付；不新增篇目，不继承旧目标的支持证据，不把候选或像素算式当作实测。作者理解待反馈，具体实现、真实设备校准和线上部署未核验。
- 2026-11-05 C3 动态策略推演：原位复用硬件 03 的控制职责，新增 N/L/R/U 纸面状态、四条教学规则、15 窗决策及试探失败/缺证据分支；02b 不再另立策略。局部 AI 编辑复审、原系列精确增量与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#dynamic-load-recovery-20261105)。上一批 C2 已随 `782f0e9` 推送，本批按持续授权交付；不新增篇号，不将题设可用标记当成 K0/Kp 或真实控制已通过。下一步补 C3 设备×配置×场景×阶段的校准设计；作者理解待反馈，实际运行与线上部署未核验。
- 2026-11-06 C3 校准矩阵：原位补 Code Quality 验证文的四维适用表、72/24/48/32/16 格教学复算、记录与判定、异常安排及重标定触发；动态未触发、未知兼容和未覆盖保持独立状态。原归属回写 Code Quality plan，局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#device-calibration-matrix-20261106)。上一批动态推演已随 `286f622` 推送，本批按持续授权交付；不新增篇目，下一步为 D1 特效成本与观测。作者理解待反馈，实际校准、实现及线上部署未核验。
- 2026-11-06 D1 覆盖口径：复用特效预算 09，新增同粒子数三布局的覆盖教学例及分母、材质/Pass、原渲染和制作预警/验收边界。局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-coverage-foundations-20261106)，原归属回写预算 plan；不新增篇目，不启动 VFX-T 工具研究。上一批 C3 已随 `6f7a25b` 推送，本批按持续授权交付；D1 发射/模拟、生命周期与观测表待续，作者理解待反馈，设备实测、工具和线上部署未核验。
- 2026-11-06 D1 生命周期：预算 09 接续发射输入、活跃量递推、停止后的尾部和局部观测表，纸面复算 9/6/27 并核对 Unity 6.0 接口语义。来源、自审和检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-lifecycle-foundations-20261106)，原归属/篇数不变。上一批覆盖教学已随 `7800074` 推送；本批按持续授权交付，D1 模拟模块、资源/实例与组合增量账本待续，作者理解待反馈，未执行引擎、设备或工具校准，线上部署未核验。
- 2026-11-06 D1 资源与组合：预算 09 增加六快照的共享/实例/池教学分配和同时峰值，区分任务耗时净增与单体归因；模板 20 补指标/条件绑定和字段范围。复用内存记账，包语义仅核对 Addressables 1.21；原归属与篇数不变，局部自审和检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-resource-ledger-20261106)。上一批生命周期已随 `7480da7` 推送，本批按持续授权交付；D1 模拟/几何成本桥接待续，作者理解待反馈，实际采集、实现和线上部署未核验。
- 2026-11-06 D1 模拟与几何：预算 09 补同活跃数三配置、逻辑几何复算与 Noise/实例化/离屏恢复边界，汇合五条工作线的候选观测。官方资料按 Unity 6.0 核对，局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-simulation-geometry-20261106)；原归属与篇数不变。上一批资源/组合已随 `f0b50c4` 推送，本批按持续授权交付；D1 当前知识职责已有材料，可接续 D2 UI，作者理解、真实采集/校准、工具和线上部署未新增通过项。
- 2026-11-06 D2 更新与重建：复用 UI 首开/叠层桥接与 UI 渲染旧文，只在渲染补D 原位修订更新链；以 uGUI 2.5.0 公开快照和三组纸面记录分开写入、脏请求、队列项·轮及批次/耗时，并纠正直接相关旧结论。原归属先回写 Unity 渲染 plan，局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-rebuild-foundations-20261106)。上一批 D1 已随 `32e22a0` 推送；本批按持续授权交付，不新增篇号、不改首开桥接。D2 文本/图集、批次/覆盖及生命周期观测待续，作者理解、真实校准、工具与线上部署仍待反馈或未核验。
- 2026-11-06 D2 状态与图集：渲染补D 原位补纹理/材质/裁剪与顺序模型，用甲/乙/丙区分纸面段数、必要叠层、实际批次与覆盖；核对旧 Text 字体纹理、Image Sprite 纹理、Atlas 配置/准备，以及该包文档 UI Profiler 模块仅支持 Editor 的边界。原归属回写渲染 plan，局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-batching-atlas-20261106)。上一批更新链已随 `169e928` 推送；本批按持续授权交付，篇数不变。覆盖诊断与生命周期等 D2 缺口待续，作者理解、真实采集/校准、工具与线上部署未新增通过项。
- 2026-11-06 D2 覆盖与诊断：补D 原位分开几何覆盖、着色、写回与时间；用两种假设映射解释饱和图不能反推层数，修正透明必写入、固定亮度、Tight 自动应用与阴影固定额外 Mesh/TMP 单次等结论。按 uGUI 2.5.0 公开实现和 Unity 6.0 ShaderLab/HLSL 契约分别核对，局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-coverage-diagnostics-20261106)。上一批已随 `fa1bea9` 推送，本批按持续授权交付；原归属与篇数不变。D2 生命周期协议待接续，作者理解、实际 Shader/诊断模式校准、采集实现和线上部署未新增验收。
- 2026-11-06 D2 生命周期：补D 原位补隐藏/停更/布局/输入/持有边界、首开至再开的观测协议及四条虚构请求记录；完成、超时和缺失分开，回池不代释放，旧结果按代次与持有责任收尾。复用首开美术桥接、资源就绪链和内存记账，原归属先回写渲染 plan；局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-lifecycle-protocol-20261106)。上一批已随 `23eec3f` 推送，本批按持续授权交付，不增篇。World Space/UI Toolkit 的旧概括待核，D2 与全篇未认证；作者理解、真实启停/采集/校准、工具及线上部署仍待反馈或未执行。
- 2026-11-06 D2 模式与框架边界：补D 原位修订 World Space 的固定批数/逐帧裁剪概括，区分事件相机输入查询与渲染证据；将框架比较改为结构、更新、图集和输出目标的观察入口，去除无依据成熟度/后端归纳。Unity 6.0 路径比较页正文标记 6.6，未用作 6.0 支持矩阵；来源、局部自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-render-mode-boundaries-20261106)。上一批已随 `c7bc92c` 推送，本批按持续授权交付；原归属与篇数不变。D2 观测字段和知识覆盖待汇合核对，不宣布全篇/阶段通过；作者理解、实际路径/校准、工具和线上部署仍待反馈或未执行。
- 2026-11-06 D2 观测字段汇合：补D 接上八字段任务记录、规则适用性、逐指标有效性和处理责任；复用 P1–P4，明确教学超时截止不自动成为性能预算，完成不代替接受。收窄开篇及静动分层的绝对概括，保留五批机制/算例；原归属先回写渲染 plan，局部 AI 编辑自审、覆盖核对及检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#ui-observation-record-20261106)。上一批已随 `d9bad7b` 推送，本批按持续授权交付，不增篇。D2 当前知识职责已有对应材料，下一批接 D3 角色资源/实例账本与 R6，动画求值/蒙皮留 D4；作者理解、第 2.2 节、实际测量/校准、工具及线上部署没有新增通过项，不认证全篇。
- 2026-11-06 D3 资源/实例首批：预算 08 将乘法改成共同约束并关闭 R6；补人数/外观种类与共享、缓存/池的四快照教学，复算 28/48/48/36 MiB，以 Unity 6.0 引用/材质实例化契约限定观测。原归属先回写预算 plan，局部 AI 编辑自审、检查及边界见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#character-resource-ledger-20261106)。上一批已随 `8096156` 推送，本批按持续授权交付，不增篇。D3 几何/材质/LOD/阴影与完整观测待续，R7/R8 留 D4；作者理解、知识阶段完成、真实测量/校准、工具及线上部署未新增通过项，不认证全篇。
- 2026-11-06 D3 绘制范围：预算 08 复用几何/Shader 定义，补实际 Renderer/LOD、子网格/材质与用途/目标映射、主画面/阴影及两级过渡的教学计数；原位收窄 LOD 固定距离概括。三份 Unity 6.0 契约分别限用，原归属先回写预算 plan，局部 AI 编辑自审及检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#character-draw-scope-20261106)。上一批已随 `1449d91` 推送，本批按持续授权交付，不增篇。下一步汇合 D3 八字段观测与模板 19 的适用性；前批账本、R6 状态不变，R7/R8 留 D4。作者理解、实际测量/标定、工具与线上部署无新增通过项，不认证全篇或整个 D3。
- 2026-11-06 D3 观测汇合：预算 08 增加八字段与五阶段记录，区分规则适用性、逐指标有效性及接受；模板 19 最小绑定指标/配置/阶段/证据/规则，保留原矩阵，结尾改为共同约束。复用两批教学和通用定义，不另建字典或篇目；原归属先回写预算 plan，局部 AI 编辑自审、D3 覆盖核对及检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#character-observation-record-20261106)。上一批已随 `6d484dc` 推送，本批按持续授权交付。D3 当前资源、用途和观测知识已有材料衔接，下一批 D4 先复读动画制作/成本旧文及 R7/R8；骨骼/权重当前只作规格输入，不提前认证求值或蒙皮。作者理解、第 2.2 节、实际采集/标定、工具和线上部署无新增通过项，不认证角色全文。
- 2026-11-06 D4 旧文入口小修订：复读动画制作与渲染蒙皮旧文，先关闭已确认的 R7 执行位置措辞与 R8 延伸阅读过期状态；两篇仅改一处概括及两条阅读说明，原归属分别回写预算/美术 plan，不新增篇目。局部 AI 编辑自审、精确候选与共享规划的部分提交边界见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-entry-corrections-20261106)。上一批已随 `ef5d107` 推送，本批按持续授权交付。D4 求值/蒙皮职责与父子观测待下一批，渲染 03 的旧权重上限、坐标空间和执行路径须按版本核对后使用；本批未认证该文。作者理解、知识阶段、实际测量/校准、工具与线上部署无新增通过项。
- 2026-11-06 D4 职责与位置模型：渲染 03 原位补状态/采样/混合/IK/写回与蒙皮边界、对象空间位置模型和 24/44 纯平移反例；按 Unity 6.0 七份公开资料修正 GPU 必在顶点阶段、影响数/骨骼上限及捕获输入的旧概括，关联收窄 Blend Shape 教学和内存表述。先回写渲染原规划，局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-evaluation-skinning-20261106)。上一批 `bf9fa73` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇。下一批补 D4 生命周期/更新条件、组合教学与八字段观测连接，必要时继续核对形态键直接依赖；不重复本批平移例。作者理解、知识阶段、真实采集/标定、工具与线上部署无新增通过项，不认证全篇。
- 2026-11-06 D4 离屏与恢复：渲染 03 原位补可见性/裁剪/离屏更新的范围、四轮虚构职责清单和返回时的连续性检查；保留官方概述与阶段文档差异，确切调度待固定 Player 核验。先回写渲染原规划，来源、局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-update-gates-20261106)。上一批 `e72b3a6` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇，原位置例不重做。下一批补层/过渡组合教学与动画八字段衔接，形态键仅按直接依赖核查；作者理解、第 2.2 节、实际采集/校准、工具和线上部署无新增验收，不认证全文或 D4 完成。
- 2026-11-06 D4 层/过渡组合：渲染 03 新增片段实例与输出属性的贡献清单及标量混合，区分 K0–K3 的 4/8/7/11、四项输出与实际工作/耗时；复用制作 05，三份 Unity 6.0 文档仅支撑对应职责。原归属先回写渲染 plan，局部 AI 编辑自审和检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-layer-transition-20261106)。上一批 `c15f23c` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇、不重做旧教学。下一批汇合动画八字段与阶段/规则，再核对 D4 剩余直接依赖；作者理解、知识阶段、实测/校准、工具及线上部署无新增验收，不认证全篇。
- 2026-11-06 D4 观测汇合：渲染 03 新增八字段、五阶段与证据/规则判定表，关联动作/代次、片段使用实例、写回、变形与消费用途；复用通用定义、角色记录和 J3/K3，不重算旧教学。原归属先回写渲染 plan，局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-observation-record-20261106)。上一批 `4f3f03c` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇、不认证 D4/全文。下一批先核对形态键直接依赖，再补压缩/误差、更新频率的实际缺口；作者理解、第 2.2 节、实际测量/校准、工具及线上部署无新增通过项。
- 2026-11-06 D4 形态帧与权重：渲染 03 原位修订形态键，区分形态/多帧/运行值、0/100 用法与归一化系数、属性流和位置空间；两帧插值及旋转反例均为虚构教学，不作 Unity 全路径组合顺序证明。六份 Unity 6.0 来源、局部 AI 编辑自审和检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#blendshape-frame-boundaries-20261106)，原归属先回写渲染 plan。上一批 `174e6d4` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇。下一批接压缩/误差与更新频率的实际缺口；作者理解、第 2.2 节、实际采集/校准、工具与线上部署无新增通过项，不认证 D4/全文。
- 2026-11-06 D4 压缩误差：渲染 03 连接压缩控制、局部姿态与世界挂点/反馈，补同角误差的不同杆长及首尾采样反例；引用五份 Unity 6.0 文档的单位、条件和模式范围，不推导真实压缩收益。原归属先回写渲染 plan，局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-compression-error-20261106)。上一批 `8e63f62` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇、不改制作教程。下一批接更新频率/时序取舍；作者理解、第 2.2 节、实际采集/校准、工具与线上部署无新增通过项，不认证 D4/全文。
- 2026-11-06 D4 更新频率与时序：渲染 03 区分动作时间、求值、写回/重建与消费；虚构 30/60 Hz 教学复算保持的陈旧样本和延迟一周期插值，说明未来样本、峰值遗漏及必要反馈。四份 Unity 6.0 来源、局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-update-frequency-20261106)，原归属先回写渲染 plan，不增篇。上一批 `a3ff417` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不重算旧教学。下一步汇合 D4 当前覆盖、保留待核验限用后接 D5；作者理解、第 2.2 节、真实更新/采集/校准、工具和线上部署无新增通过项，不认证 D4/全文。
- 2026-11-06 D4 覆盖审校：七组动画基础材料与制作前置已映射到正文/教学/观测，尚缺 IK/约束输入到求解工作、写回与输出检查的短桥；先复用制作 04/算法 10 的相关原理，不继承未经核验的代码、复杂度、耗时或旧版本支持。原归属先回写渲染 plan，覆盖表、局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-knowledge-coverage-20261106)。上一批 `7018f55` 已推送且交付时远端 SHA 一致；本批仅维护四份规划/审校记录，不增篇、不重复教学或认证 D4/全文。下一批补该短桥，再接 D5 场景；作者理解、第 2.2 节、实际测量/校准、工具和线上部署无新增通过项。
- 2026-11-06 D4 IK/约束短桥：渲染 03 补目标/空间/时刻、输入姿态与链、求解/权重及停止条件到写回/消费者的连接；按 Unity 6.0 与 Animation Rigging 1.4.1 区分三条文档入口，以虚构骨长可达性/残差反例说明工作与质量边界。原归属先回写渲染 plan，来源、局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#animation-ik-constraint-20261106)。上一批 `53b1207` 已推送且远端回读一致；本批按持续授权交付，不增篇、不重做七组教学。上批解释缺项关闭后接 D5 可见集合/用途；作者理解、第 2.2 节、实际求解/时序/测量/校准、工具和线上部署无新增验收，不认证全文。
- 2026-11-06 D5 可见集合与用途：预算 07 原位补对象候选/参与、相机/用途和渲染事件/执行边界；虚构路线 Q0/Q1 区分主画面 3/3、去重实例 5/6 和参与关系 9/11，不换算 DrawCall 或成本。复用角色几何/Pass 教学，四份 Unity 6.0 文档各自限用；原归属先回写预算 plan，来源、局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#scene-visible-purpose-20261106)。上一批 `ce46871` 已推送且交付时远端 SHA 一致；本批按持续授权交付，不增篇、不重复 D4。下一批灯光/阴影，之后 Streaming/双驻留及完整观测/模板 18；作者理解、第 2.2 节、真实采集/校准、工具和线上部署未新增验收，不认证全文或 D5 完成。
