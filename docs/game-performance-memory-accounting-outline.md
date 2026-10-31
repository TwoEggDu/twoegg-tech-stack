# 内存到底占在哪里：写作与验证记录

## 定位与范围

- 用户确认的原理补篇，篇级归属：[游戏性能判断规划](game-performance-series-plan.md)，规划标识 GP-OS-02，公开标题不加篇号。
- 问题：业务已经释放对象，为什么系统占用没有同步下降；怎样从虚拟地址、物理页和引擎分配，得到可验收的游戏内存预算。
- 前置：了解对象、资源和基本分配行为；衔接调度篇的执行／等待模型，不要求先学完整内核。
- 边界：以通用分层为主，Windows、Linux/Android、Apple 和 Unity 各标明口径；不展开内核回收算法、驱动内部、GC 实现或具体机型安全阈值。
- 复用：旧文《内存不是够不够，而是行为稳不稳》的生命周期问题；CPU F04 的延迟／带宽模型；资源可用链和现有 OS I/O 的阅读入口。OS I/O 全文审校仍是下一项。

## 写作结构

1. 同一进程的多个内存数字：先确认统计层次，不能相加当总量。
2. 虚拟地址、页与映射：连续地址不保证连续物理页；页表和 TLB 分工。
3. 保留、提交、驻留与释放：Windows 例子及 Linux overcommit 边界；分配器可复用空间与归还 OS 分开。
4. 缺页、文件页、匿名页与写时复制：区分缺页、磁盘 I/O、Cache Miss；mmap 不免除运行时资源构建。
5. 指标口径：Working Set、RSS/PSS、Apple footprint、Unity tracked/reserved/untracked；图形资源与共享页避免重复计账。
6. 教学场景切换：同一时刻、互斥分类的预算算例；并发预加载、驻留与释放顺序的取舍。
7. 工程验收：采样条件、峰值与残留、责任归属、复测路径；不把预算线冒充系统终止阈值。

## 旧文审校与入口范围

- 保留旧文 URL 和生命周期主线，修正 memory bound、工作集、缺页与长玩卡顿的混用，降低无证据的普遍归因，补 GC／池化的代价边界。
- 内存专题索引增加基础入口，并修正与本篇直接冲突的工作集、缺页及平台故障归因；不扩审所有预算与平台文章。
- 性能系列 plan 与索引、FDE plan 与索引、CPU 索引、线程篇延伸阅读；执行对照表只更新 G08 与其当前任务，不更新根路由或全量学习提纲。
- 所有现有未提交改动保留。正文、事实核对、本地构建、设备实验、Git 发布分别记录。

## 证据与验证

2026-10-31 已核对以下公开资料，正文各段就近链接：

| 依据 | 用途与限制 |
| --- | --- |
| [Microsoft 虚拟地址空间](https://learn.microsoft.com/en-us/windows/win32/memory/virtual-address-space)、[VirtualAlloc](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc) | 地址、保留、提交与按需取得物理页；不外推 Linux 分配契约 |
| [Microsoft Working Set](https://learn.microsoft.com/en-us/windows/win32/memory/working-set) | 可分页驻留集合、软／硬缺页；保留特殊分配例外 |
| [Linux 页表](https://docs.kernel.org/mm/page_tables.html)、[overcommit](https://docs.kernel.org/mm/overcommit-accounting.html)、[mmap](https://man7.org/linux/man-pages/man2/mmap.2.html) | TLB、映射、配置策略与写时复制；未追踪内核实现或运行实验 |
| [Linux 6.8 proc](https://www.kernel.org/doc/html/v6.8/filesystems/proc.html)、[Android 内存管理](https://developer.android.com/topic/performance/memory-management) | RSS/PSS 共享记账、zRAM；算例排除特殊页差异，不定义设备阈值 |
| [Apple WWDC22](https://developer.apple.com/videos/play/wwdc2022/10106/)、[jetsam 报告](https://developer.apple.com/documentation/xcode/identifying-high-memory-use-with-jetsam-event-reports) | footprint 与驻留差别、系统终止取证；报告页 JS 正文读取受限，仅核对官方检索摘要中的用途，不据此展开字段解析 |
| [Unity 6.0 Profiler](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/visualizing-data/memory/memory)、[托管内存](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/performance-memory/managed-memory/introduction) | 引擎跟踪范围、堆内复用与归还 OS；不引用固定扩容倍数、页大小或跨版本 GC 保证 |
| [Intel VTune 2024.0 指标](https://www.intel.com/content/www/us/en/docs/vtune-profiler/user-guide/2024-0/cpu-metrics-reference.html) | 旧文 Memory Bound 定义修正，不推广成所有架构的计数器公式 |

场景数字仅为教学算例，不是作者项目数据；本轮不执行设备内存压力或引擎实验。

## 本轮完成与验证

- 共 11 个文件：新增正文与本记录；审校内存行为旧文和内存专题索引；性能索引、CPU 索引、线程篇及 FDE 总入口补阅读连接；性能 plan、FDE plan 与执行对照表只更新相关内容。
- 核对与修正：Memory Bound 不等于生命周期波动；Working Set 不等于业务常驻列表；缺页不总是读盘；释放对象不保证即时降低 OS 占用；不同口径不直接加总；预算线不冒充系统终止阈值。
- 教学算术通过：400／760／440 MiB 列合计、660／700 MiB 条件推演、RSS 120 与 PSS 100 MiB；只验证模型条件与计算，没有收益实测。
- 19 项阅读布局回归通过。首次沙箱执行因不能启动 Hugo 子进程失败，在允许启动的环境重跑通过；未改变测试或模板。
- 最终本地 Hugo 构建生成 1443 页；生成页检查覆盖 1377 个阅读页，errors 为 0。最终预览使用独立目录与 localhost baseURL，并向检查脚本传入相同 baseURL。
- 浏览器确认新文标题、无编号的性能系列归属、正文与预算表格、目录锚点正常；FDE → 新文、性能索引与新文往返、新文 → 内存行为旧文均实际走通。其余入口通过生成页链接检查。
- 本轮范围内 relref 目标与 UTF-8 检查通过；新文 5 处站内链接有效。未改样式、模板或根系列路由。
- 工作期间另一项任务提交了 `3933790`（专栏方向与 AI 路由）。保留其规划变更，只更新本轮相关段落；任务前快照对照未发现本轮范围外的文件内容变化。
- 当前为本地成稿与本地验证完成，尚未提交、推送或部署。没有真机内存时间线、引擎运行或项目优化结论。下一项为既有 OS I/O 审校。
