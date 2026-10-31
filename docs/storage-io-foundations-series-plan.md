# 存储设备与 IO 基础系列计划

本文件是已有“存储设备与 IO 基础”系列的唯一 canonical 篇级目录。2026-10-31 补齐规划归属，复用既有六篇正文与索引，不新建系列或改 URL；历史 `docs/doc-plan.md` 和归档保持只读。

## 定位与边界

读者是需要判断游戏加载、资源布局与 streaming 成本的客户端／引擎开发者。沿存储设备 → 文件系统 → OS I/O → 游戏资源可用解释因果，支撑 FDE 的技术取舍。基础说明、平台实现、工程建议与实测结果分别记账，不扩成完整 OS 教材。

公开入口：[存储设备与 IO 基础索引](../content/engine-toolchain/storage-io-series-index.md)，`series_id: storage-io-foundations`。

## 现有篇目与本轮状态

| 编号 | 正文 | 状态与范围 |
| --- | --- | --- |
| IO-01 | [为什么碎文件更慢](../content/engine-toolchain/storage-io-01-why-fragmented-files-are-slower.md) | 既有正文，本轮仅补主系列元数据以恢复 01—06 目录，正文未审校 |
| IO-02 | [存储设备类型](../content/engine-toolchain/storage-io-02-device-types-hdd-ssd-nvme-emmc-ufs.md) | 既有正文，本轮未审校 |
| IO-03 | [文件系统基础](../content/engine-toolchain/storage-io-03-filesystem-path-directory-metadata-page-cache.md) | 既有正文，本轮未审校 |
| IO-04 | [OS I/O 机制](../content/engine-toolchain/storage-io-04-os-io-sync-async-dma-mmap-read-ahead-queue-depth.md) | 2026-10-31 原位审校：缓存路径、同步／异步、DMA、mmap、预读与分层队列；官方资料已核对。修订未提交发布，无设备实测；验证状态见[审校记录](storage-io-04-review.md) |
| IO-05 | [移动端存储特性](../content/engine-toolchain/storage-io-05-mobile-storage-emmc-vs-ufs.md) | 既有正文，本轮未审校 |
| IO-06 | [IO 性能分析](../content/engine-toolchain/storage-io-06-profiling-with-fio-iostat-perfetto-instruments.md) | 2026-10-31 原位修订：观测口径、同一任务证据关联、按问题选工具；官方文档与命令适用条件已核对，本地验证通过。无目标设备命令／性能实测；验证范围见[审校记录](storage-io-06-review.md)，提交／推送状态以 Git 记录为准，线上部署未验证 |

## 跨系列阅读与复用

[线程调度](../content/performance/game-performance-thread-scheduling-waits.md) → [虚拟内存与预算](../content/performance/game-performance-memory-accounting.md) → IO-04 → [资源可用链](../content/performance/game-performance-read-does-not-mean-ready.md)。线程、内存与资源可用文章仍归[性能系列](game-performance-series-plan.md)，FDE 只维护精选入口。

本轮复用决定：IO-04 已有六层机制主线，增量是事实修正、来源、平台边界和验证设计；不新开同义 OS I/O 篇。后续若做加载测量，复用 IO-04 与资源可用文章，按同一任务记录提交、完成、消费和最终可用阶段，实验状态与正文审校分开。

IO-06 继续使用原路径、编号和系列归属，复用 IO-03／04、线程调度、内存记账和资源可用文章；不另开测量基础篇。新增的是观测与证据关联方法，不将配置示例或验证设计视为项目实测。

IO-01／02／03／05 仍按自身材料与条件阅读，本轮通过不代表整个存储系列已经完成事实复核。
