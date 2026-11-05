# 客户端性能工程：Unity 与 Unreal 官方参考资料

> 获取与核对日期：2026-11-05，北京时间。本文维护资料来源、版本、已读范围与工作流依据，不维护篇级目录。正式篇目归[专栏主规划](client-performance-engineering-series-plan.md)，固定写作与性能验证步骤归[执行计划第 4 节](client-performance-budget-tiering-monitoring-execution-plan.md#writing-workflow)。

## 1. 已下载什么

本地资料根目录：`E:\workspace\TechStackShow\.tmp\performance-official-reference-20261105\`。原件保留在已忽略的 `.tmp/` 中；本仓库只保存来源索引与自行整理的判断。下载原文不等于获得向公开站点重新分发整本资料的许可。

| 资料组 | 已取得的内容 | 版本与完整性 |
| --- | --- | --- |
| `unity-guides/` | 2 本官方 PDF、逐页提取 TXT、来源入口和 manifest | 共 170 个物理页；2022 年 Profiling 手册与 2020 LTS 版 PC/主机优化手册，不标为最新 Unity 6 电子书 |
| `unity-manual/` | 12 个官方文档 HTML 与正文 TXT、manifest | 固定 Unity 6.0（6000.0）URL；只下载选定章节，非整站离线手册 |
| `unreal/` | 9 个官方文档 HTML 与正文 TXT、manifest、阅读提要 | URL 选择器及标题均为 Unreal Engine 5.8；8 篇正文加 1 个 Scalability 索引，非整站离线手册 |

各组 `manifest.json` 保存原始/最终 URL、取得时间、版本依据、文件名、内容类型和 SHA-256。PDF 检查文件头、页数及文本提取；网页检查实际正文和标题，HTTP 200 网站空壳不算成功。HTML 为来源快照，图片、脚本等依赖没有完整镜像；离线检索优先使用 TXT。`.tmp/` 不随 Git 分发，换机器时可依据清单重新取得并记录新的日期与 hash，不能假定内容未变。

Unity Mobile 电子书当前取得的是[官方登记入口](https://create.unity3d.com/optimize-mobile-game-eBook)，未找到普通公开下载链接，未提交个人资料，**未取得移动版 PDF**。Epic 本次选定页面没有提供 PDF 下载入口，实际交付为网页快照与正文。

## 2. Unity 官方电子书

PDF 页码均为从 1 开始的物理页码；PC/主机书的印刷页脚比物理页码少 1。以下是实际读取的重点，不代表已通读全部 170 页。

| ID / 资料 | 官方入口与原件 | 版本依据、已读位置及用途 |
| --- | --- | --- |
| U-BOOK-PROFILE / Ultimate guide to profiling Unity games | [官方入口](https://unity.com/resources/ultimate-guide-to-profiling-unity-games) → [该入口链接的 PDF](https://cdn.bfldr.com/S5BC9Y64/at/jmv8gbjf4gc35jptsnmrrsz/Ultimate_Guide_to_Profiling_Unity_Games.pdf)；本地 `unity-guides/ultimate-guide-to-profiling-unity-games.pdf` | 75 页，©2022；p6 基线/持续分析/改动后验证，p9–10 与 p17 采集开销和由粗到细，p13 帧预算，p37–40 Editor/设备迭代，p49–54 多帧比较与确定性任务 |
| U-BOOK-PC / Optimize your game performance for consoles and PC | [官方入口](https://unity.com/resources/optimize-your-console-and-pc-game-performance) → [该入口链接的 PDF](https://cdn.bfldr.com/S5BC9Y64/at/j52w7wk3xnmp9nkhpwqz6w/OptimizeGamePerformance_eBook_Final.pdf)；本地 `unity-guides/optimize-console-and-pc-game-performance.pdf` | 95 页，封面 2020 LTS edition，©2021；物理 p8–11 的设备、模块、Deep Profiling、保存原始记录及 profile/optimize/compare 流程 |

两本 PDF 均沿 Unity 官方页面的实际下载链接取得；CDN 域名并非 `unity.com`，官方入口与最终下载 URL 一起保留。电子书版本不自动等于用户项目版本。

## 3. Unity 6.0 文档章节

以下官方 URL 均固定 `6000.0`。本地 HTML/TXT 使用表中文件名，位于 `unity-manual/`。

| ID | 官方章节 | 本地文件名；选读用途 |
| --- | --- | --- |
| U-ROOT | [Unity Profiler](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler) | `profiler`；总入口与模块、工具关系 |
| U-INTRO | [Profiler introduction](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/introduction) | `introduction`；Profiler 观测范围 |
| U-COLLECT | [Collecting performance data](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/profiling-applications) | `collecting-data`；采集入口索引 |
| U-COLLECT-INTRO | [Collect performance data introduction](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/profiling-applications/profiling-collect-data-introduction) | `collecting-introduction`；只采启用模块；目标设备发现问题、Editor 快速迭代、回设备验证 |
| U-TARGET | [Collect performance data on a target platform](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/profiling-applications/profiling-target-device) | `target-device`；Development Build 与连接条件，平台支持差异 |
| U-EDITOR | [Play mode and Editor profile samples](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/play-edit-samples) | `editor-player`；PlayerLoop/EditorLoop、Editor-only 记录和 Deep Profiling 开销 |
| U-DEEP | [Instrument all function calls](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/deep-profiling) | `deep-profiling`；默认插桩范围、全方法采集的时间与内存代价 |
| U-STANDALONE | [Running the Profiler in its own process](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/standalone-process) | `standalone-profiler`；Profiler 进程与被测应用的关系 |
| U-TRACE | [Analyzing Profiler traces](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/performance-profiler-traces) | `analyzing-traces`；启动、运行和加载记录的调查入口；具体符号与路径仍按项目版本验证 |
| U-GRAPHICS | [Graphics performance and profiling](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling) | `graphics-profiling`；图形分析子专题入口 |
| U-TOOLS | [Profiling tools reference](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/performance-profiling-tools) | `profiling-tools`；Profiler、Profile Analyzer、内存与平台工具的职责 |
| U-MEMORY | [Memory in Unity introduction](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/performance-memory/overview) | `memory-overview`；托管、原生等内存概念入口 |

## 4. Epic 官方性能文档

本次获取固定 `application_version=5.8`，并核对实际标题中的版本。这里选择的是参考文档版本，没有替用户确定项目引擎版本。本地文件位于 `unreal/`。

| ID | 官方章节 | 本地文件名；已读位置与用途 |
| --- | --- | --- |
| UE-INTRO | [Introduction to Performance Profiling and Configuration](https://dev.epicgames.com/documentation/en-us/unreal-engine/introduction-to-performance-profiling-and-configuration-in-unreal-engine?application_version=5.8) | `01-introduction`；CPU-Bound vs. GPU-Bound、Profiling Tools、Test Early and Test Often；限制路径、采集资源争用、目标设备 |
| UE-SCALE | [Scalability and the Developer](https://dev.epicgames.com/documentation/en-us/unreal-engine/scalability-and-the-developer-for-unreal-engine?application_version=5.8) | `02-scalability-developer`；预算与 QA 小节；可复现场景、CPU/GPU/内存矩阵及低配置的功能/可读性 |
| UE-INSIGHTS | [Unreal Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-insights-in-unreal-engine?application_version=5.8) | `03-unreal-insights`；整体工具与 trace 会话入口 |
| UE-TIMING | [Timing Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/timing-insights-in-unreal-engine?application_version=5.8) | `04-timing-insights`；Frames、Timing、Timers and Counters、Callers and Callees；选帧和窗口后关联线程与调用 |
| UE-MEM | [Memory Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/memory-insights-in-unreal-engine?application_version=5.8) | `05-memory-insights`；Run Your Game Project with Memory Tracing；分配追踪需从进程启动启用，打包项目的 Development 前置 |
| UE-STAT | [Stat Commands](https://dev.epicgames.com/documentation/en-us/unreal-engine/stat-commands-in-unreal-engine?application_version=5.8) | `06-stat-commands`；Unit 小节的粗定位入口，不作为单独根因证据 |
| UE-SCALE-INDEX | [Scalability](https://dev.epicgames.com/documentation/en-us/unreal-engine/scalability-in-unreal-engine?application_version=5.8) | `07-scalability`；专题索引，未把所有子页算作已下载 |
| UE-DEVICE | [Customizing Device Profiles and Scalability on Android](https://dev.epicgames.com/documentation/en-us/unreal-engine/customizing-device-profiles-and-scalability-in-unreal-engine-projects-for-android?application_version=5.8) | `08-android-device-profiles`；Changing Scalability Settings at Runtime；默认 Profile 与实际生效值分开记录 |
| UE-TRACE | [Unreal Insights Reference](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-insights-reference-in-unreal-engine-5?application_version=5.8) | `09-insights-reference`；Trace Channels；未启用通道不发出相应事件，可固定采集 preset |

<a id="workflow-evidence"></a>

## 5. 哪些依据进入固定工作流

下表将官方依据连接到[固定性能流程 T1–T8](client-performance-budget-tiering-monitoring-execution-plan.md#performance-workflow)。流程顺序、退回条件和记录格式是本站综合设计；官方文档支持其中明确的技术原则，不背书我们的完整协议已经可用。

| 决定 | 直接依据 | 本专栏的落实与边界 |
| --- | --- | --- |
| 先定义目标与代表负载，再记录基线 | U-BOOK-PROFILE p6、p13、p17；UE-SCALE 的 QA | T1/T2/T4 所需任务、设备与场景条件；预算值由项目校准，不能复制书中示例 |
| 采集先粗后细，配置改变须重新核对有效性 | U-BOOK-PROFILE p9–10、p17；U-DEEP；UE-TRACE 的 Trace Channels | T3/T5 记录模块、通道、支持与扰动；缺轨道不等于没有工作 |
| 以可重复任务做前后比较并保留原始记录 | U-BOOK-PROFILE p53 的 Compare view；U-BOOK-PC 物理 p11 | T2/T4/T7 固定路线与操作，保留 A/B 原始记录；重复策略、缓存/热漂移和统计推断另由测试协议补齐 |
| Editor 加快迭代，性能接受回目标设备 | U-COLLECT-INTRO；U-BOOK-PROFILE p40；UE-INTRO 的 Test Early and Test Often | T6/T7 可在 Editor 调试，接受条件仍绑定实际设备及构建；Development 诊断结果不直接冒充发布构建表现 |
| 从目标窗口追踪到相关工作，而不是只看最长条 | UE-TIMING 的 Frames/Timing/Timers；UE-STAT 的 Unit；UE-INTRO 的 display-bound 说明 | T5 保留 CPU、GPU、等待与反馈依赖的不同解释；整体统计先作线索 |
| 内存与设备档有自己的前置和口径 | UE-MEM 的启动追踪前置；UE-DEVICE 的 runtime settings；U-MEMORY | T2/T3 保存实际配置与统计层次；标签、默认值、内存计数不能替代目标任务验收 |

CPE-02 优先使用 U-BOOK-PROFILE p49–54、U-COLLECT-INTRO、U-DEEP、UE-TRACE 和 UE-TIMING。已有《一帧拆解》《性能诊断方法》继续承担指标定义与对照原则；新篇用教学记录把定义变成可比性判断，不另写同义 Profiler 操作总论。

## 6. 引用时必须保留的限制

- 官方旧资料也会过时。U-BOOK-PROFILE p15 的固定空闲比例及电量概括、U-BOOK-PC 的历史启动与插桩开销数字，不直接转成通用阈值或固定工具成本。
- 为诊断关闭 VSync、改分辨率、打开 Deep Profiling 等动作改变了条件。只能用来检验相应假设；体验和收益需回到约定的配置复测。
- UE 5.8 页面仍包含 UE 4.24 的设备示例和历史性说明。页面版本不能证明每项硬件建议已重新校准；以实际相关段落和项目运行条件为准。
- UE-INTRO 对分析器开销的概述不能推导“附带分析器通过就一定更保守”。采集可能改变执行与调度，需独立检查，不预设开销恒定或可忽略。
- Profile Analyzer 的确定性会话建议不保证硬件调度、热状态和缓存也完全一致。工具支持前后比较，不自动建立因果或统计可靠性。
- 原始 HTML/TXT 用于查找与回溯，正式引用回到官方 URL、版本和具体章节；需要确认界面、图示或 PDF 排版时再看原件。页面更新时间等动态文字不能替代本次获取日期。

本次完成资料下载、正文有效性检查、重点章节阅读和两套流程设计；没有运行 Unity、Unreal、设备实验或校准采集器，没有修改 CPE-01 正文，也没有形成新的性能达标或优化收益结论。
