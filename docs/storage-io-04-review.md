# 存储与 IO 04 审校记录

日期：2026-10-31。对应[正文](../content/engine-toolchain/storage-io-04-os-io-sync-async-dma-mmap-read-ahead-queue-depth.md)与[系列 plan](storage-io-foundations-series-plan.md)。本文件是执行记录，不另建篇级目录。

## 范围与复用

用户授权审校既有 IO-04，修正机制与证据边界，补工程取舍及必要阅读入口。已读原文、存储索引、性能与 FDE 的相关计划。保留 URL、系列、编号和六层顺序；复用线程、内存与资源可用文章，不新增同义正文，不审校其他五篇存储文章或工作区的其他改动。

规划补缺：原存储目录只在历史 `docs/doc-plan.md`，未发现独立 canonical plan。本次登记既有六篇并加入根系列路由，不修改历史快照；性能与 FDE 仅保留跨系列入口。

浏览器检查发现原有归属混杂：存储 01 缺 `primary_series`，而“大整文件”和“读盘不等于可用”两篇性能文章误登记为存储主系列，导致 04 显示为 05／07。仅补存储 01 的主系列，并将两篇性能文章改为显式性能阅读索引；保留正文与先前改动，不修改共用模板。两篇仍以交叉链接进入存储阅读路径。

## 已修正的 10 组问题

| 原位置与代表表述 | 问题与修正 | 核对依据 |
| --- | --- | --- |
| 开头 `await stream.ReadAsync(buffer)` 未处理返回长度 | 明确仅示意一次读取；区分打开和读取，提示累计长度、短读、EOF 与错误 | [FileStream.ReadAsync](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.readasync) |
| 第一层“最终都要落到操作系统…如果不在才会真的往底层设备发 I/O” | 补本地普通文件缓存读取模型，区分运行时缓冲、OS 缓存与设备路径，去掉逐次调用和设备请求的一一对应暗示 | [.NET 6 File I/O](https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/)、[Windows File Caching](https://learn.microsoft.com/en-us/windows/win32/fileio/file-caching) |
| 完成路径“这次 I/O 做完了” | 区分底层完成、读取结果、唤醒与获得执行机会，明确成功也可能短读 | [read(2)](https://man7.org/linux/man-pages/man2/read.2.html)、既有线程调度篇 |
| 第二层“异步 I/O：调用先返回，结果稍后…” | 补可同步完成、提交可能阻塞、运行时线程池实现；.NET 6 Unix 示例不外推 Unity 或所有 Unix 接口 | [Windows 同步与异步 I/O](https://learn.microsoft.com/en-us/windows/win32/fileio/synchronous-and-asynchronous-i-o)、.NET 6 官方说明 |
| 原文异步取舍缺生命周期 | 补缓冲区到最终完成才可复用、取消与正常完成竞争、过期结果释放 | [ReadFile](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile)、[CancelIoEx](https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelioex) |
| 第三层“DMA 之后往往已经进入 CPU 侧解析” | 补地址映射、缓存到用户缓冲及中转复制，完成处理与调度仍有成本；不以 DMA 当阶段分界 | [Linux DMA](https://docs.kernel.org/core-api/dma-api-howto.html)、[SWIOTLB](https://docs.kernel.org/core-api/swiotlb.html) |
| 第四层“按页触发装载” | 缺页不等于设备读取；映射不保证驻留或端到端零复制，补有效范围、截短和映射寿命 | [Linux 内存概念](https://docs.kernel.org/admin-guide/mm/concepts.html)、[mmap(2)](https://man7.org/linux/man-pages/man2/mmap.2.html) |
| 第五层预读像“白送的优化” | 改为额外读取与等待的交换，补提示非保证、无用读取与缓存压力；大包不等于顺序访问 | [posix_fadvise(2)](https://man7.org/linux/man-pages/man2/posix_fadvise.2.html)；成本取舍为机制推导，非设备收益 |
| 第六层串行 32 KB“设备看到 QD=1”“没有被真正喂饱” | 区分应用、OS、设备三层计数，缓存、合并与其他负载影响映射；低并发不直接证明性能不足 | [Linux blk-mq](https://docs.kernel.org/block/blk-mq.html)；并发预算为工程建议 |
| 合并链把 File.OpenRead／AssetBundle.LoadFromFileAsync 写成固定流水线 | 改为排查阶段图，明确非 Unity 内部调用栈；增加受控比较、实际触页、缓存条件、输出正确性和资源可用指标 | 前述官方资料、既有资源可用篇；实验为未执行的验证设计 |

## 证据与验证状态

- 官方机制/API 文档已读取核对，正文就近给出来源。.NET 实现例子限定为官方说明的 .NET 6；Windows 与 Linux 分开，不声称 Unity 内部路径已验证。
- 无设备 I/O trace、无 mmap／异步接口性能对比、无项目收益实测。正文 DATA-TODO 留待固定环境后补证。
- 本地验证已通过：阅读模板回归 19 项；Hugo 生成 1443 页；`check-reading-pages.py` 检查 1377 个阅读页，errors 为 0；本次范围 `git diff --check` 通过。
- 浏览器已核对原有阅读样式、04／06 编号、03／05 上下篇和返回存储索引；实际点击 FDE → OS I/O 04 → 资源可用 → 性能索引，性能索引内的 OS I/O 链接存在。预览使用独立目录与本机地址，临时服务与检查页已关闭。
- 检查基于当前工作区，含既有未提交模板／文章改动，不表示这些改动属于本轮交付。快照与本轮文件清单在 `.tmp/os-io-audit-20261031/`；根路由、性能 plan 与资源可用文章包含原有改动，后续提交必须按本轮差异取范围。
- 本轮修订未提交、未推送、未部署。
