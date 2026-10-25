# CXX-11：数据交给另一个线程后，谁保证它有效且可见

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇，依赖 CXX-02、04～05；存储与对象寿命按需回看 CXX-09。公共课最后一个既定单元，不新增完整无锁算法/调度器体系。
- 复用 CXX 的所有权、失效和回调模型，Piccolo PCPP-3 的写入/发布/消费/归还阶段，以及硬件 F05 的内存序问题入口；硬件收益、缓存行大小和指令成本不作为本课实测事实。
- 公共模型：执行者与任务完成；对象寿命；共享不变量的互斥；发布与读取的 happens-before；关闭、排空与复用协议。保活、互斥、可见性不能互相替代。
- 五个完整 C++17 程序：移动独占拥有者与异常退出 join；mutex 快照与 async 异常传播；可关闭单槽通道；一次性 release/acquire 发布；全部原子访问仍可能丢更新的确定性逻辑反例与 fetch_add 修复。
- 两个预期编译失败：复制 atomic、复制 thread；分别用 load 取得值快照、移动线程拥有关系修复。
- 不运行普通数据竞争、悬空访问或 relaxed 发布普通载荷的非法变体；不使用 sleep 制造“同步”；不声称重复运行是内存模型或无死锁的完整证明。

## 正文提纲与验证设计

1. 从任务排队后对象被删除、标记为真却缺少发布证明、双缓冲复用三个问题进入。
2. 执行、保活、同步、协议四层；thread/async/future、joinable/join/detach 与 shutdown 的边界。
3. 程序一：unique_ptr 初始化捕获转移责任，JoinOnExit 在主线程异常展开时等待，join 后读取普通结果。
4. 程序二：同一 mutex 保护两个字段及快照；lock_guard 的 RAII，不能在退出锁后继续使用未受保护的引用；强制 async 与 future::get 的异常。
5. 程序三：unique_lock+condition_variable 谓词、消息移动、满/关闭拒绝且保留输入拥有者；关闭可排空，先关闭再等待，最后销毁共享状态。
6. 程序四：载荷写入→release→读取该发布值的 acquire→载荷读取；只发布一次且之后不改载荷，不将布尔槽直接扩展成可复用多生产者队列。
7. 程序五：闸门保证两个原子 load 都先于 store，结果确定为 1；atomic fetch_add 累计 2000。relaxed 的原子性与普通数据发布分开。
8. volatile、shared_ptr/weak_ptr、const、引用捕获与取消，逐项说明不足以替代同步或完成协议。
9. 四条路线按真实线程入口/完成条件跳转；CPU 规则不能替代 GPU、UE 线程亲和与托管 GC 协作。
10. 全部公共 11 单元成稿；专项状态仍归各路线，不宣称四条源码前置或引擎运行全覆盖。更新目录、canonical plan、根系列行和六篇旧文回链。

## 资料依据

以 C++17 N4659 为基线，2026-10-25 核对：

- [数据竞争与 happens-before](https://timsong-cpp.github.io/cppwp/n4659/intro.races)、[thread 构造](https://timsong-cpp.github.io/cppwp/n4659/thread.thread.constr)、[析构](https://timsong-cpp.github.io/cppwp/n4659/thread.thread.destr)、[join/detach](https://timsong-cpp.github.io/cppwp/n4659/thread.thread.member)。
- [mutex 同步](https://timsong-cpp.github.io/cppwp/n4659/thread.mutex.requirements.mutex)、[条件变量](https://timsong-cpp.github.io/cppwp/n4659/thread.condition.condvar)、[async](https://timsong-cpp.github.io/cppwp/n4659/futures.async)、[原子内存序](https://timsong-cpp.github.io/cppwp/n4659/atomics.order)。
- [shared_ptr](https://timsong-cpp.github.io/cppwp/n4659/util.smartptr.shared)、[cv 限定](https://timsong-cpp.github.io/cppwp/n4659/dcl.type.cv)。具体库接口的细节在正文补充对应条款。

## 验证记录

- 2026-10-25：Windows x64、GCC 8.3.0（x86_64-posix-seh）。五个正常程序使用 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic -pthread`，不定义 NDEBUG。全部编译/运行退出码 0，所列选项下无诊断，断言通过；输出分别为正文的 1、2、3、1、2 行。
- owner_and_join：输入 unique_ptr 移入工作闭包，主线程受控异常展开时 JoinOnExit 完成等待，普通 result 在 join 后为 42。未注入工作线程未捕获异常或 join 失败。
- mutex_snapshot：两个任务各更新 1000 次，100 个快照满足两字段相等，最终 2000/2000；显式 launch::async 的 TaskError 经 get 被观察。没有声称 100 个快照均与实际写入重叠。
- closed_mailbox：发送成功才移动输入，消费者获得 42 并观察关闭后的空；满槽和关闭拒绝保留拥有者，已有消息可在关闭后排空。关闭守卫在 future 之前析构，正常顺序显式 close/get；未证明每轮一定发生阻塞/虚假唤醒，未测试满/关闭的所有并发交错。
- release_acquire：只发布一次的普通 Snapshot 读出 frame=7、sum=60；解释依赖 release 被 acquire 读取的同步链，未运行 relaxed 发布普通载荷的非法变体。
- atomic_update：合法闸门强制两个原子 load 均先于 store，结果确定为 1；fetch_add 的两个 1000 次更新得到 2000，区分无数据竞争与算法正确。没有把 relaxed 用作普通载荷的发布。
- atomic_copy_error 与 thread_copy_error 以 `-std=c++17 -Wall -Wextra -Wpedantic -pthread -fsyntax-only` 检查，均退出 1，诊断为删除的复制构造。load 快照与移动线程修复变体通过编译检查，没有运行失败产物。
- 额外 20 轮 × 5 程序，共 100 次进程运行，均在验证器单次 15 秒上限内退出 0，输出与断言一致。该上限不是延迟承诺；重复运行不代替 happens-before 推导、无死锁/调度证明或内存模型检查。
- 最终正文七个 C++ 代码块与实际测试文件逐字一致。未运行普通数据竞争、悬垂访问、非法内存序、ThreadSanitizer、长期压力、资源耗尽/同步调用失败、其他平台、引擎/GC/GPU 或性能验证。
- 从当时 HEAD `7b34d9835556b7fcbe218049a223184f430011f4` 导出站点，叠加本次 11 个正文/目录/规划/回链文件；根 doc-plan 只叠加 C++ 系列行，隔离其他工作区和暂存内容。Hugo v0.157.0 生成 1409 页，退出码 0、无 ERROR；不代表全部并行改动验证通过。
- 新正文、公共索引、CXX-09/05/02/04、Piccolo PCPP-3、硬件 F05 共 8 个生成页面，434 处本地链接、资源和锚点检查通过；七个已有页面均有新课入口，正文正常代码和输出可见。
- Canonical plan 与公共索引已回写既定 11 篇正文均成稿，公共待整理/待写为 0；后续进入各路线专项。六篇旧文保留原 URL、归属、项目材料与原验证记录；PCPP-3 不再标 CXX-11 待写，硬件篇新增语言与实测边界提示。公共成稿不表示引擎构建、专项或工程验收完成。提交/推送状态以 Git 记录为准。
