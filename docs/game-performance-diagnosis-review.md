# 性能诊断方法：原位修订审校记录

日期：2026-10-31。对应[正文](../content/performance/game-performance-diagnosis-method.md)与[系列计划](game-performance-series-plan.md)。本记录为执行材料，不新增文章或篇号。

## 范围与复用

按用户确认，补清诊断线索与结论、各类瓶颈的边界和最小对照验证。保留原文标题、slug、date、series、tags、weight 与原章节相对顺序，新增 lastmod 并更新 description。原有六类入口继续使用，修正将读取后处理泛称 I/O、将容量／GC／访存混成同类以及仅比较 CPU／GPU 时间等口径。

复用一帧链路、帧预算拆账、帧时序、线程调度、内存记账与行为、IO 06、功耗与发热以及工具选择文章。它们承担机制、计时或操作职责，本篇负责证据、归因与对照；旧文引用不代表本次全面复核其全部断言。固定战斗镜头为未执行的验证设计，不是作者项目经历或测试结果。

本次只写正文、本计划新增的“诊断方法审校”一节及本记录。计划在任务前已有其他未提交修订，全部保留，不视为本次增量；不修改缺口对照表、索引、根路由或其他文章。

## 官方核对与版本条件

| 内容 | 官方依据 | 本次边界 |
| --- | --- | --- |
| CPU／GPU／Present 的解释 | [Unity 2022.3 标记](https://docs.unity3d.com/2022.3/Documentation/Manual/profiler-markers.html)、[Frame Timing Manager](https://docs.unity3d.com/2022.3/Documentation/Manual/frame-timing-manager.html) | CPU 帧时间含等待与开销，返回样本与当前帧不能直接等同；Present 等待须关联渲染线程及同步条件，GPU 支持与准确性按平台核对，不由单一数字定因 |
| 执行、就绪、等待与关键路径 | [Microsoft WPA CPU Analysis](https://learn.microsoft.com/en-us/windows-hardware/test/wpt/cpu-analysis) | Windows 文档中的状态与关键路径用于解释模型，不跨系统照搬字段；等待存在不等于有性能问题，也不把 CPU 总利用率作为统一判据 |
| 内存分配与回收 | [Unity 2022.3 GC](https://docs.unity3d.com/2022.3/Documentation/Manual/performance-garbage-collector.html)、[CPU Usage](https://docs.unity3d.com/2022.3/Documentation/Manual/ProfilerCPU.html) | 分配、回收模式与时间线分开；增量回收可跨帧，分配量不直接推算停顿；不将托管 GC 机制外推到 Unreal |
| 访存与 GPU 负载 | [Vulkan profiling 指南](https://docs.vulkan.org/guide/latest/profiling.html) | 作为 GPU 带宽与 profiling 的具体说明，不当作 Vulkan 规范保证或通用计数器阈值；分辨率对照不足以区分 ALU、采样、带宽或唯一 Pass |
| 采集与交付配置 | [Unity 2022.3 采集说明](https://docs.unity3d.com/2022.3/Documentation/Manual/profiler-profiling-applications.html) | 连接 Player 的 Unity Profiler 使用 Development Build；记录 Editor／采集配置与 Deep Profile 开销，目标交付配置需用适用方式另验，不声称 Release 可直接按相同连接流程采样 |
| Unreal 入口 | [UE 5.6 Unreal Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-insights-in-unreal-engine?application_version=5.6) | 仅解释事件、记录与分析分工；实际采集按构建和 OS，未给通用命令，不逐项等同 Unity 标记 |
| 热状态与设备支持 | [Android PowerManager](https://developer.android.com/reference/android/os/PowerManager) | 热状态查询 API 29、headroom API 30；headroom 不等于摄氏温度，不支持可返回 NaN，未知不等于没有热限制；未新增相关代码或设备支持结论 |

官方资料核对日期为本记录日期。IO 与资源阶段、内存记账、显示与热模型的详细契约沿已有专题复用；本次没有新增加载／热管理代码或工具命令，没有设备或引擎运行结论。

## 验证与未核验范围

- 最小对照是工程建议／拟验证方案，没有目标工程、原始 trace、设备 A／B／恢复复测、显示／输入测量或优化收益。DATA-TODO 要求保存版本、条件、有效与缺失样本、失败与反例、正确性及资源代价。
- 静态检查通过：原 title／slug／date／series／tags／weight／featured 及原 H2 相对顺序保持；lastmod 与 description 为本次维护。正文与计划保留原 LF，10 个站内复用目标均存在，差异空白检查通过。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 构建：`hugo --gc --minify --destination .tmp/diagnosis-method-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页：`python -B scripts/check-reading-pages.py --public .tmp/diagnosis-method-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML 检查：15 个 H2 与三张新增表格已生成；10 个复用链接解析且目标存在，UE 官方链接的 5.6 查询参数完整，Present／GC 标记正常显示，无残留 shortcode，DATA-TODO 不显示为正文。没有改样式／模板，本次未做浏览器视觉验收。
- 暂存区与任务前一致；原有文件的快照对比仅观察到本次正文和计划变化，其他既有文件内容保持。计划保留全部任务前修订，只新增本篇审校节；新增审校记录另列，不把整份计划的差异算作本次修改。
- `.tmp/diagnosis-method-20261031/` 保存任务前快照、计划既有差异、本次增量与检查结果，供后续限定提交范围；不作为发布产物。
- 本次不提交、不推送，其他工作区改动保留。
