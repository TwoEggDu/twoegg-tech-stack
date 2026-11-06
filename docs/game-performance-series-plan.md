# 游戏性能判断系列计划

## 已完成

1. 已发布：《为什么某些操作会慢：给游戏开发的性能判断框架》
2. 已发布：《一帧到底是怎么完成的：游戏里一个 Frame 到底在做什么》
3. 已发布：《什么事不能在什么时候做：游戏开发里最危险的时机管理》
4. 已发布：《为什么一个大整文件，往往比很多小散文件更稳》
5. 已发布：《读盘完成，为什么还是不等于资源可用》
6. 已发布：《内存不是够不够，而是行为稳不稳》
7. 已发布：《Unity 里，这些性能问题通常怎么显形》
8. 已发布：《Unreal 里，这些性能问题通常怎么显形》
9. 已发布：《怎么判断你到底卡在哪：CPU / GPU / I/O / Memory / Sync / Thermal 的诊断方法》
10. 已发布：《手机和 PC 为什么要用不同的性能直觉》
11. 已发布：《从现象到方法：把游戏性能判断连成一套工作流》

## OS 基础补篇

2026-10-31 按作者确认的 FDE 技术纵深补强顺序，先承接调度与等待的共同模型。本文仍由本系列维护，不新开完整 OS 课程；OS 知识提纲负责知识点映射，FDE 只维护精选入口。

| 标识与标题 | 归属与复用 | 当前状态与证据 |
| --- | --- | --- |
| GP-OS-01：《线程为什么没有在执行：从进程、调度到等待与唤醒》 | [正文](../content/performance/game-performance-thread-scheduling-waits.md)；承接 OS 01.1～01.3 的部分模型，复用 Unity 异步番外 01／03／04 | 正文已写；官方资料已核对，24 ms 时间线为教学算例；无设备 trace、引擎运行或收益实测。本地检查与交付状态见[写作记录](game-performance-thread-scheduling-waits-outline.md) |
| GP-OS-02：《内存到底占在哪里：从虚拟地址、物理页到游戏内存预算》 | [正文](../content/performance/game-performance-memory-accounting.md)，复用内存行为旧文与 CPU F04；[写作记录](game-performance-memory-accounting-outline.md) | 正文与相关旧文审校已随 `77f4cf1` 发布，Pages #580 与线上路径已核验；官方机制与统计定义已核对，场景为教学算例，无设备内存采样或优化实测 |

GP-OS-01／02 是规划标识，公开标题不强加篇号；使用既有系列名与显式 reading_index 作为无编号补篇接入，避免混入角色入口的章节序列。GP-OS-01 已随 `a55a90f` 发布，Pages #578 与线上阅读路径已核验。OS I/O 04 审校已随 `361095b` 发布，Pages #582 与线上路径已核验；该篇仍归[存储系列 plan](storage-io-foundations-series-plan.md)，机制审校范围见[审校记录](storage-io-04-review.md)，不登记为性能系列新增篇。

## 资源可用链审校

2026-10-31 原位修订[《读盘完成，为什么还是不等于资源可用》](../content/performance/game-performance-read-does-not-mean-ready.md)，沿用已发布文章、URL 与性能系列归属。增量为完成条件及阶段责任表、非固定串行的依赖与调度、并发／分帧／驻留预算、部分失败与退出清理，以及明确标注的技能加载教学场景。

复用线程、内存、OS I/O、资源加载与生命周期文章；不新增同义加载总论，不扩写完整资源框架。Unity 6.0、Addressables 1.21.21 与 Epic 页面标注 UE 5.8 的契约分别核对；无设备 trace、引擎运行与优化收益实测。本次修订未提交发布，本地验证与范围见[审校记录](game-performance-resource-ready-review.md)。IO-06 的其他工作仍归原存储计划，不计入本轮。

## 一帧、呈现与指标口径审校

2026-10-31 原位审校[《一帧到底是怎么完成的》](../content/performance/game-performance-frame-breakdown.md)，保留 URL 与系列归属。补逻辑／渲染／显示对象、指标起止点与统计窗口、并行依赖与关键路径、Present 等待边界，以及技能反馈教学时间线；同步[帧预算拆账](../content/performance/game-budget-06-frame-budget-breakdown.md)的重叠计时与团队记录口径。

Unity 6.0、Android Frame Pacing、Vulkan 与 DXGI 的公开契约用于机制核对；Unreal 仅映射线程职责。教学算例不代表真机 trace 或输入延迟实测。本节修订已随 `f8ec017` 发布，Pages #589 及线上路径已核验，审校范围见[记录](game-performance-frame-review.md)。诊断方法的后续原位审校见下一节，把现象、假设、证据和对照分开；持续性能 05 仍需完整优化对照，不据此升级为已验证案例。

## 诊断方法审校

2026-10-31 原位修订[《怎么判断你到底卡在哪》](../content/performance/game-performance-diagnosis-method.md)，沿用原文、URL 与六类排查入口。补现象／假设／证据／结论的区分、CPU／GPU／I/O／内存／同步／热约束的交叉边界、同一任务证据关联、六类假设与对照表，以及最小 A／B／恢复复测与结论限制。同步系列索引和 FDE 执行对照表的必要入口。

复用一帧链路、帧预算、帧时序、线程与内存、IO 06、功耗与发热及工具选择旧文，不新增同义诊断篇。Unity 2022.3 LTS、UE 5.6 工具入口、Windows WPA 和 Android API 条件分别核对；对照方案未执行，没有设备 trace 或优化收益实测。诊断审校及续审已随 `a3bd121`／`8f3cf71` 发布，本会话核验 Pages #591 与线上回路；范围见[审校记录](game-performance-diagnosis-review.md)。本条不改变其他既有修订的交付状态，也不将持续性能 05 提纲升级为已验证案例。

## 性能工作流与平台关联审校

2026-10-31 原位审校[《从现象到方法》](../content/performance/game-performance-methodology-summary.md)，复用已审校的帧指标、诊断、线程、内存、加载与热约束。补目标／观察／假设／取证与对照／方案取舍／复测接受的回退关系、决策交接表，以及首次技能反馈迟到的教学分支。六类名称作为调查入口，不再当固定顺序的根因分类；平台、构建和采集条件从调查开始记录。

同步[平台直觉](../content/performance/game-performance-mobile-vs-pc-intuition.md)中与工作流冲突的必要关联表述：去掉平台原因优先级、必然持续降频和用户偏好假定，区分读取与后处理、正常等待与依赖迟到；保留跨平台调查主题，不扩写 API、驱动或硬件教程。入口沿原索引维护，标题、URL 与系列归属保持。

责任表为工程建议，技能过程为无数据教学推演，未执行设备实验或团队流程验证。本轮未提交发布，范围与本地检查见[记录](game-performance-workflow-review.md)。下一步沿 M02 选择一个可公开的任务或独立实验，取得同场景基线、诊断、单项改动和复测；持续性能 05 仍为提纲，不能用本篇补成已验证案例。

## 客户端观测首篇的关联修订（2026-11-04）

随[客户端性能工程 CPE-01](../content/performance/client-performance-task-observation-model.md)写作，原位修正[一帧拆解](../content/performance/game-performance-frame-breakdown.md)摘要中的“一部分后台工作最终同帧收口”，明确为后台结果未在消费期限前就绪而导致主线程等待。与原文“同帧消费仍可有效并行”的解释一致，不另改机制或引擎映射。R1 经 AI 编辑复审关闭，仍无设备实验或优化收益证据。

CPE-01 是引擎无关的任务观测方法，独立篇级归属在[客户端性能工程主规划](client-performance-engineering-series-plan.md)，本系列继续维护帧、内存、就绪与诊断定义。相关页面检查与本地交付记录见[执行计划](client-performance-budget-tiering-monitoring-execution-plan.md)的 P1 记录；本轮未提交推送。

## 计划中

2026-11-03 新增[预算、设备分档、观测与监测学习路线](client-performance-budget-tiering-monitoring-outline.md)，并形成[执行计划](client-performance-budget-tiering-monitoring-execution-plan.md)：先补成本模型、观测指标、预算与高中低机型分档，再覆盖 UI、场景、角色、动画和特效，最后推导准确采集与持续监测设计。本系列承接公共指标、测量与诊断；预算、硬件及对象机制沿原系列复用。当前完成范围、缺口和阶段任务整理，首批为最小指标字典与教学任务贯通，尚未新增正文、实测阈值或监测实现；特效作为后续首个专项应用，篇级计划仍单独维护。

1. 继续补系列中的案例篇
可选方向：
- 真实匿名案例：首次技能卡顿、UI 首开抖动、切场景最后一卡
- 典型抽象案例：不依赖真实项目，也能把“现象 -> 判断 -> 动作”走一遍

## 写作约束

- 保持长文解释风格，但要持续服务工程判断。
- 不把“计算机基础”单独写成教材，而是嵌进游戏运行时问题里解释。
- 优先回答：为什么会慢、慢在哪、什么事不能在什么时候做、该先怀疑哪一层。
- Unity / Unreal 只做原理映射，不先掉进 API 细节。

## 客户端监测职责的数据流补桥（2026-11-06）

按客户端性能工程 F1 的接续，在[原性能工作流](../content/performance/game-performance-methodology-summary.md#performance-report-dataflow-teaching)新增报告追溯节，保留本系列标题、URL 与身份。复用调查/人员交接和预算绑定，增量为配置到领域执行/采集、原始保存、有效性/规则报告及复验的职责与产物；J-A/B/C 和 T0 为虚构教学输入，不是运行系统、设备结果或团队采用。

只登记本批正文修订，不改变既有 M02、OS 补篇或其他工作状态。局部 AI 编辑自审、候选检查和 Git 接续见[客户端执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#f1-report-dataflow-20261106)；下一项为 F4 基线/规则变化时的重算与复测方法，知识出口与作者反馈仍待通过。

## 规则与基线变更的方法补桥（2026-11-06）

按客户端性能工程 F4 的接续，在[原性能工作流](../content/performance/game-performance-methodology-summary.md#performance-rule-baseline-changes)补原始覆盖、重算与复测的判据，保留原标题、URL 和归属。复用预算及质量门的规则/基线责任；五种变更、K 的均值/P95 反例及 B0/B1 的差值均为教学推演，不是实际回归、基线批准或优化收益。

仅登记本批局部知识增量，不改变 M02、OS 补篇及其他状态。局部 AI 编辑自审、候选检查和 Git 接续见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#f4-rule-baseline-teaching-20261106)；下一项先复读已有告警/豁免处理，再补必要的例外到期与复验恢复教学，知识出口和作者理解待反馈。

## 告警例外到期与复验恢复的知识补桥（2026-11-07）

按客户端性能工程 F4 的接续，在[原性能工作流](../content/performance/game-performance-methodology-summary.md#performance-exception-recovery-teaching)补独立教学时间线，区分报告事实、通知处理、接单和接受；覆盖例外范围/到期、异常补验、恢复依据及重新打开。复用既有质量门豁免职责，不新增篇号或更改本篇身份；时间、报告与接受事件均为假设，不是生产响应、放行或团队采用。

本批仅追加此修订登记，保留 M02、OS 补篇及其他状态。来源、局部 AI 编辑自审和候选检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#f4-exception-recovery-20261107)；下一步核对批量、取消/超时及旧候选结果隔离的方法覆盖，优先复用已完成材料，作者反馈与知识出口仍待通过。

## 批次取消、历史结果与补验关联的知识补桥（2026-11-07）

按客户端性能工程 F1/F4 最新覆盖核对，在[原性能工作流](../content/performance/game-performance-methodology-summary.md#performance-batch-ledger-teaching)补固定四目标的教学清单，贯通未启动取消、已启动超时、迟到/旧候选错配及新身份补跑。B1 原截止快照与 B2 后续补证视图区分；八个必需证据位置的清点、18/19 ms 及 20 ms 线均为题设，不是实测、通用阈值或实际系统能力。

仅登记这一局部增量，不改原标题、URL、篇目或其他工作状态。局部 AI 编辑自审及独立候选检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#f4-batch-ledger-teaching-20261107)；下一步在原执行记录汇总第 2.2 节待反馈位置，已有教学和审校直接复用，作者理解、实际测量、工具及线上部署无新增验收。
