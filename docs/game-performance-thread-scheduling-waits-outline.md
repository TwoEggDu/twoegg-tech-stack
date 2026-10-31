# 线程为什么没有在执行：写作与验证记录

## 定位与范围

- 用户已确认按 OS 调度 → 虚拟内存 → 现有 I/O 审校推进，本轮只交付第一项。
- 原理补篇，唯一篇级归属：[游戏性能判断规划](game-performance-series-plan.md)。目标正文：`content/performance/game-performance-thread-scheduling-waits.md`。
- 承接进程隔离、线程执行上下文、调度与时间的共同模型；不等于完整 OS 基础课程。
- 问题：操作耗时较长时，怎样区分执行、OS 调度等待、阻塞和应用任务排队，再决定调整计算、依赖、并发还是主线程提交。
- 不展开：具体内核调度算法、绑核教程、无锁实现、虚拟内存细节、I/O 内部实现、真实项目优化收益。

## 结构与复用

1. 进程持有资源，线程承载可调度执行；逻辑任务、线程、硬件执行上下文分层。
2. 运行／就绪／阻塞的教学模型；Cache Miss、自旋、系统调用、上下文切换的边界。
3. 从等待条件到唤醒、再到实际执行；条件检查与等待的竞态、超时与寿命。
4. 明确假设的单线程 24 ms 时间线；异步任务另看运行时队列和关键路径。
5. 复用异步番外 01、03、04，补“同步接口可用有界 Worker 隔离”的取舍；不改写旧文。
6. 用假设的场景准备链比较工作拆分、限流、提前准备、分帧、优先级与亲和性。
7. 最小观测与复测设计，接回团队接口和验收。

## 资料核对（2026-10-31）

| 依据 | 用途与适用边界 |
| --- | --- |
| [Microsoft：进程与线程](https://learn.microsoft.com/en-us/windows/win32/procthread/about-processes-and-threads) | Windows 地址空间、资源与执行上下文；不把进程结构细节推广到所有 OS |
| [Microsoft：上下文切换](https://learn.microsoft.com/en-us/windows/win32/procthread/context-switches) | 就绪、等待、上下文保存恢复；不引用固定切换耗时 |
| [Microsoft：等待函数](https://learn.microsoft.com/en-us/windows/win32/sync/wait-functions) | 条件满足／超时与等待；不承诺计时器或回调精确准时 |
| [POSIX 条件变量](https://man7.org/linux/man-pages/man3/pthread_cond_wait.3p.html) | 原子释放锁并等待、返回后重获锁与重检条件；不作为所有同步原语的统一实现 |
| [Linux 内核 completions](https://docs.kernel.org/scheduler/completion.html) | 已完成与等待的先后、通知及对象寿命；只作内核机制例证，不推荐用户态直接调用 |
| [Linux syscalls](https://man7.org/linux/man-pages/man2/syscalls.2.html) | 应用与内核接口，与线程切换区分 |
| [C# await](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/await) | 挂起方法、不因 await 本身阻塞线程、已完成操作不挂起 |
| [Unity 6.0 continuation](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/programming-distribute-work-threads/async-await-support/async-awaitable-continuations) | 主线程 API 与 continuation 调度边界；不扩大为所有 Unity 版本和 awaitable 的规则 |
| [Perfetto 调度事件](https://perfetto.dev/docs/data-sources/cpu-scheduling) | Android/Linux 的切换、唤醒与状态观察；未采集设备 trace |
| [WPA CPU Analysis](https://learn.microsoft.com/en-us/windows-hardware/test/wpt/cpu-analysis) | Windows 的 sampled／precise 诊断入口；本轮未执行 WPR/WPA |

## 修改清单与验收

- 正文与本记录；游戏性能系列 plan／索引；FDE plan／总入口；CPU 索引；OS 知识提纲与客户端基础提纲各补一处进度连接。
- 保留已有未提交修改；不重写异步文章、不更换模板、不另开 OS 大课程。
- 自查：唤醒不等于立即运行；任务队列不等于 OS 就绪队列；on-CPU 不等于有效计算；等待不等于任意工具中的全部空白；收益与调度策略不作跨平台保证。
- 导航核对发现原系列显式 primary_series 成员主要是角色入口；新文沿用历史正文的 series 归属，显式 reading_index 返回现有索引，以无编号补篇接入，不改模板或其他成员。
- 时间线只做算术与状态闭合检查。运行仓库规定的回归、Hugo 构建、生成页检查，并在浏览器检查正文、系列归属与跨系列入口。
- 完成状态（2026-10-31）：正文、必要规划与三个阅读入口已补齐；19 项阅读布局回归通过，Hugo 生成 1442 页，生成页检查覆盖 1376 个阅读页、错误为 0。时间线连续性及 5 + 5 + 14 = 24 ms 的算术已检查，正文 8 处 relref 均有目标文件。
- 浏览器核验：新文标题、无编号系列归属、状态／时间线表格与目录正常；性能系列目录往返、FDE → 新文、CPU 索引 → 新文，以及新文 → 异步番外 04 均实际走通。异步表格链接换行，定位器中心点击未命中文字，改为点击可见文字后正常跳转。
- 没有真机 trace、引擎运行或优化前后对照；尚未提交、推送或部署。
- 并行工作边界：任务期间另有技能、站点定位和 FDE 规划等更新。本轮在 FDE plan 只替换 OS 精选入口行，其他并发修改保留；游戏性能 plan、客户端基础提纲、OS 知识提纲的原有未提交内容也保留，后续提交需按本轮增量区分。

## 发布范围

本次交付正文、写作记录、游戏性能与 FDE 规划的相关增量，以及性能系列、FDE、CPU 三个入口，共 7 个文件。客户端基础提纲与 OS 知识提纲此前为未入库的全量草稿，其本地进度连接保留，暂不连带发布。其他规划段落和模板改动不纳入本次提交。
