---
title: "C++ 公共基础 11｜数据交给另一个线程后，谁保证它有效且可见"
slug: "cpp-foundations-11-concurrency-handoff"
date: "2026-10-25"
description: "用独立 C++17 程序讲清线程与任务收尾、寿命、数据竞争、锁、条件变量和原子发布，建立四条源码路线共用的数据交接模型。"
tags: ["C++", "源码阅读", "并发", "生命周期", "内存序", "数据交接"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 11
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "数据交给另一个线程后，谁保证它有效且可见"
reading_summary: "把保活、互斥、发布和完成协议分别证明，再决定数据何时允许消费、关闭与复用。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 501
---

把回调放进任务队列，只完成了“稍后调用”的安排：任务运行时，捕获的对象可能已经结束；对象即使还活着，两个线程也可能同时改它；即使每个状态标记都是 atomic，组合起来的更新仍可能丢失。

多缓冲还多一个问题：新数据已经发布，是否代表旧槽位已经没人读，可以马上覆盖？这些都不能只靠“用了智能指针”“加一个原子变量”或“睡一会再读”回答。

本篇的共同模型是：**先证明对象活到使用结束，再证明访问满足同步规则，最后证明发布、消费、关闭与复用的协议成立。**

> **读这篇之前**：掌握 [CXX-02 所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})、[CXX-04 容器与访问失效]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}})、[CXX-05 回调寿命]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})。存储与对象何时存在可回看 CXX-09。
>
> **复用与范围**：复用 [Piccolo 状态缓冲与线程边界]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}}) 的写入/发布/消费/归还模型，以及 [硬件多核篇]({{< relref "performance/hardware-cpu-f05-multicore-false-sharing.md" >}}) 的内存序问题入口。这里先讲 C++17 的语言与库规则，不把硬件示意、某个缓存行大小或性能倍数作为本课实测；不实现完整线程池和无锁回收算法。

## 1. 一次交接，至少要回答四组问题

| 层次 | 必须找到的依据 | 常见混淆 |
|---|---|---|
| 执行与完成 | 谁执行任务，何时等待完成，异常交给谁 | 提交成功就当作已经执行；取消标记就当作已经退出 |
| 寿命与拥有 | 捕获值、借用、独占转交还是共享保活 | 把引用捕获、this 或裸指针当成保活 |
| 同步与可见性 | 哪个 mutex 或原子同步关系约束哪些访问 | 认为对象没释放就可以随意并发写 |
| 协议与复用 | 空/满/关闭如何改变，何时可以重用数据 | 把发布完成与消费完成当成同一信号 |

任务是一段待执行工作，线程是执行它的上下文；任务系统可能在同一线程串行执行，也可能由工作线程调度。不能从函数名字有 Async 或保存了两份数据，就推断实际执行者。

C++ 的 happens-before 是语言规定的先后与可见性关系。两个不同线程访问同一内存位置、至少一方修改、涉及非原子访问且没有必要的 happens-before 关系，可能构成数据竞争，后果是未定义行为。写入相同值也仍然是写入。[C++17：数据竞争与同步][races]

日志时间先后、调试器暂停、yield 和 sleep 都不能替这套关系作证明。本篇不运行普通数据竞争的程序；先把合法交接链写出来，再解释失败边界。

## 2. 线程结束之前，借用的结果对象不能消失

std::thread 构造时会保存可调用对象与参数的相应副本/移动结果，再让新线程执行；传入引用包装或引用捕获，仍然是在借用原对象。`[this]` 保存的也只是访问路径，不延长接收者寿命。[C++17：thread 构造][thread-start]

下面把 unique_ptr 移进闭包，由工作线程独占输入；result 则由主线程提供，在等待结束之后读取。主线程故意抛出一个受控异常，检查退出路径有没有漏掉 join。

```cpp
// owner_and_join.cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <thread>
#include <utility>
struct JoinOnExit
{
    std::thread& worker;
    explicit JoinOnExit(std::thread& value) noexcept : worker(value) {}
    JoinOnExit(const JoinOnExit&) = delete;
    JoinOnExit& operator=(const JoinOnExit&) = delete;
    ~JoinOnExit() { if (worker.joinable()) worker.join(); }
};
struct Interrupted {};
int main()
{
    int result = 0;
    bool caught = false;
    try
    {
        auto input = std::make_unique<int>(21);
        std::thread worker([owned = std::move(input), &result] { result = *owned * 2; });
        JoinOnExit wait(worker);
        assert(!input);
        throw Interrupted{};
    }
    catch (const Interrupted&) { caught = true; }
    assert(caught && result == 42);
    std::cout << "owner=moved result=42 joined-during-unwind=1\n";
}
```

输出：

```text
owner=moved result=42 joined-during-unwind=1
```

成功的 join 返回与该线程的完成建立同步，因此 result 可以是普通 int：工作线程写完并结束之后，主线程才读它。这个保证不能用于主线程在 join **之前**与工作线程同时访问 result 的情况。[C++17：join 同步][thread-join]

std::thread 的析构不会自动帮你等待；仍处于 joinable 状态时会调用 terminate。joinable 也不等于“线程现在还在计算”：即使执行已经结束，只要尚未 join/detach，包装仍可能需要收尾。[C++17：thread 析构][thread-dtor]

这里的 JoinOnExit 只用于父线程等待自己创建的线程，没有其他线程同时操作该 thread 对象，也不在工作线程自身调用 join。它没有实现停止请求、超时或底层 join 失败的恢复策略，等待时也不能持有工作线程退出所需的锁。C++20 jthread 的行为不能直接套给本篇的 C++17 thread。

detach 只是放弃当前 thread 包装对执行的管理，不延长引用捕获的对象寿命，也不证明任务已经结束。不能为绕过析构错误，把一个借用局部变量的线程随手 detach。

## 3. 同一把锁，要保护完整的不变量

对象活着以后，还要保证共享状态的访问合法。例如 accepted 与 completed 必须作为一份状态观察；仅给写入加锁、读取不加锁，或者分别锁住两个字段，都没有提供这份复合快照的同一协议。

```cpp
// mutex_snapshot.cpp
#include <cassert>
#include <future>
#include <iostream>
#include <mutex>
#include <utility>
class Ledger
{
    mutable std::mutex mutex;
    int accepted = 0;
    int completed = 0;
public:
    void account()
    {
        std::lock_guard<std::mutex> lock(mutex);
        ++accepted;
        ++completed;
    }
    std::pair<int, int> snapshot() const
    {
        std::lock_guard<std::mutex> lock(mutex);
        return {accepted, completed};
    }
};
struct TaskError {};
int main()
{
    Ledger ledger;
    auto work = [&] { for (int i = 0; i < 1000; ++i) ledger.account(); };
    auto first = std::async(std::launch::async, work);
    auto second = std::async(std::launch::async, work);
    for (int i = 0; i < 100; ++i)
    {
        const auto state = ledger.snapshot();
        assert(state.first == state.second);
    }
    first.get();
    second.get();
    const auto final = ledger.snapshot();
    assert(final.first == 2000 && final.second == 2000);
    std::cout << "snapshot=2000,2000\n";
    auto failed = std::async(std::launch::async, []() -> int { throw TaskError{}; });
    bool caught = false;
    try { (void)failed.get(); }
    catch (const TaskError&) { caught = true; }
    assert(caught);
    std::cout << "task-error=observed\n";
}
```

输出：

```text
snapshot=2000,2000
task-error=observed
```

两个字段的所有访问都经过同一 mutex。一个线程的 unlock 与后续成功取得该 mutex 的相应 lock 建立同步；lock_guard 将锁的释放接到作用域退出与异常清理上。[C++17：mutex][mutex]、[lock_guard][guard]

snapshot 在持锁期间复制结果，返回独立的 pair；没有把内部引用交出去，期待调用者在解锁后继续安全访问。mutex 是 mutable 只为允许 const 成员维护同步设施，**mutable 和 const 自身都不是同步工具**。

这里用 `std::async(std::launch::async, ...)` 明确选择异步执行，并用 future::get 等待、取得结果或重新抛出任务异常。默认 async 策略还允许 deferred，不能一律当成后台启动；get 后也不能把同一普通 future 随意当成可重复读取的结果容器。[C++17：async][async]、[future][future]

future 的结果/异常通道与普通 thread 不同：thread 入口的异常若没有被处理，会触发 terminate；它不会自动跨线程进入主线程的 catch。本篇两个异常用例分别是主线程展开时等待、以及 async 任务异常由 get 观察，不能混为一种机制。

锁解决受该协议覆盖的访问，不自动保证无死锁。多个锁的次序、持锁等待任务、锁内调用未知回调，都要另查；RAII 能保证作用域清理，也不能消除逻辑上的等待环。

## 4. 交接不只有“有数据”，还要能关闭和排空

下面复用 Piccolo 专项中的单槽交接动机，改成传递 unique_ptr 的公共教学通道。协议很小：发送成功才转移拥有；槽满或关闭时拒绝，保留调用者的输入；关闭后仍允许取走已经在槽内的消息，排空后 receive 返回空。

```cpp
// closed_mailbox.cpp
#include <cassert>
#include <condition_variable>
#include <future>
#include <iostream>
#include <memory>
#include <mutex>
#include <utility>
class Mailbox
{
    std::mutex mutex;
    std::condition_variable changed;
    std::unique_ptr<int> slot;
    bool closed = false;
public:
    bool try_send(std::unique_ptr<int>& message)
    {
        if (!message) return false;
        std::unique_lock<std::mutex> lock(mutex);
        if (closed || slot) return false;
        slot = std::move(message);
        lock.unlock();
        changed.notify_one();
        return true;
    }
    std::unique_ptr<int> receive()
    {
        std::unique_lock<std::mutex> lock(mutex);
        changed.wait(lock, [&] { return closed || static_cast<bool>(slot); });
        return std::move(slot);
    }
    void close()
    {
        {
            std::lock_guard<std::mutex> lock(mutex);
            closed = true;
        }
        changed.notify_all();
    }
};
struct CloseOnExit
{
    Mailbox& box;
    ~CloseOnExit() { box.close(); }
};
int main()
{
    Mailbox box;
    auto message = std::make_unique<int>(42);
    auto consumer = std::async(std::launch::async, [&] {
        auto value = box.receive();
        if (!value) return -1;
        const int result = *value;
        const auto end = box.receive();
        assert(!end);
        return result;
    });
    CloseOnExit close_before_wait{box};
    const bool sent = box.try_send(message);
    assert(sent && !message);
    box.close();
    const int received = consumer.get();
    assert(received == 42);
    std::cout << "handoff=42 drained=closed\n";

    Mailbox full;
    auto first = std::make_unique<int>(1);
    auto rejected = std::make_unique<int>(2);
    const bool inserted = full.try_send(first);
    const bool busy = full.try_send(rejected);
    assert(inserted && !first && !busy && rejected && *rejected == 2);
    full.close();
    const auto taken = full.receive();
    const auto end = full.receive();
    assert(taken && *taken == 1 && !end);
    std::cout << "full=rejected owner=retained\n";
    const bool late = box.try_send(rejected);
    assert(!late && rejected && *rejected == 2);
    std::cout << "closed=rejected owner=retained\n";
}
```

输出：

```text
handoff=42 drained=closed
full=rejected owner=retained
closed=rejected owner=retained
```

unique_lock 允许显式解锁并配合条件变量。wait 会在等待时释放锁，返回前重新取得锁；可能虚假唤醒，因此使用谓词，反复在同一 mutex 保护下检查 `closed || slot`。通知是让等待者有机会重新检查状态，不是可积攒的“消息数量”，也不是替代状态和锁的发布证明。[C++17：unique_lock][unique-lock]、[条件变量][condvar]

发送者在锁内将消息移入槽，消费者在锁内将其移出，随后各自只访问自己的拥有者。槽清空后可以再接收另一份独立消息；本例交出的是 owning 值，不是仍指向可被覆盖槽内容的裸引用。调用 try_send 的线程也必须独占其传入的 unique_ptr 变量，不能让别的线程同时改那个变量。

关闭的顺序同样属于正确性：**先让等待者能退出，再等待任务结束，最后销毁 mutex、条件变量和通道。** CloseOnExit 在 consumer 之后声明，异常展开时先 close，再轮到 async 的 future 释放/等待相关共享状态，最后才销毁 box。不能把 close 只写在 box 析构里，却让排在它前面的等待永远阻塞。

本例只定义单槽的阻塞接收和非阻塞发送，不承诺公平性、超时、批量提交、取消正在执行的消息或生产级故障恢复。处理完消息与关闭通道也是两件事；项目若需要取消而不是排空，必须另定义已取出的工作怎么办。

## 5. 原子发布，要能写出同步链

对于只发布一次、发布后不再修改的 Snapshot，可以用一个原子标记连接生产与消费：

```cpp
// release_acquire.cpp
#include <array>
#include <atomic>
#include <cassert>
#include <future>
#include <iostream>
#include <thread>
struct Snapshot
{
    int frame = 0;
    std::array<int, 3> positions{};
};
int main()
{
    Snapshot data;
    std::atomic<bool> ready{false};
    auto producer = std::async(std::launch::async, [&] {
        data.frame = 7;
        data.positions = {10, 20, 30};
        ready.store(true, std::memory_order_release);
    });
    auto consumer = std::async(std::launch::async, [&] {
        while (!ready.load(std::memory_order_acquire)) std::this_thread::yield();
        const Snapshot local = data;
        assert(local.frame == 7);
        return local.positions[0] + local.positions[1] + local.positions[2];
    });
    producer.get();
    const int sum = consumer.get();
    assert(sum == 60);
    std::cout << "publication=frame7 sum60\n";
}
```

输出：

```text
publication=frame7 sum60
```

合法性的依据是下面这条链，而不是“x64 看上去先写数据”：

```mermaid
flowchart LR
    W[生产者写普通载荷] -->|sequenced-before| S[ready.store release]
    S -->|消费者 acquire 读到这次发布值| L[ready.load acquire 返回 true]
    L -->|sequenced-before| R[消费者读取载荷]
```

release 与读取相应发布值的 acquire 建立 synchronizes-with，再与各线程内的先后组合成 happens-before。本例 ready 从 false 只改为 true 一次，载荷随后不再变化，data 与 ready 也活到两个任务结束；因此可以读取普通字段。[C++17：原子顺序][atomic-order]

只写上 acquire 并不意味着“无论读到什么都同步了”；本例必须读到那次 release 发布的 true。把这对操作改成 relaxed，再让消费者根据标记读取普通载荷，会失去本例依赖的发布关系；不能因为重复运行没出错，就宣布该非法变体安全。本篇不运行这个变体。

yield 只用于示例等待循环的调度提示，不是同步边；实际同步来自 atomic。长期等待通常还需要合适的阻塞/唤醒协议，本例没有测 CPU 成本、等待时延或调度公平性。

这也不能直接变成可反复覆盖的布尔槽：第二批写入之前，谁确认第一批的消费者已经读完？清标记、换索引、双缓冲数量本身都不能提供完整反馈。可重用队列还要建立槽的所有权、完成确认与复用顺序；程序四只证明一次性发布。

## 6. 都是 atomic，更新仍然可能丢

“没有数据竞争”是语言正确性的一个条件，不代表算法满足业务要求。把一次加一拆成原子 load 和原子 store，两步各自不可分割，不意味着组合起来也是一次不可分割的读改写。

下面用合法的原子闸门，让两个任务都先读取 0，再允许写回 1；没有靠调度碰运气，也没有普通变量的数据竞争。

```cpp
// atomic_update.cpp
#include <atomic>
#include <cassert>
#include <future>
#include <iostream>
#include <thread>
struct OpenGateOnExit
{
    std::atomic<bool>& gate;
    ~OpenGateOnExit() { gate.store(true, std::memory_order_release); }
};
int main()
{
    std::atomic<int> split{0};
    std::atomic<int> loaded{0};
    std::atomic<bool> go{false};
    std::atomic<int> combined{0};
    std::future<void> first, second;
    OpenGateOnExit release_before_wait{go};
    auto split_update = [&] {
        const int old = split.load(std::memory_order_relaxed);
        loaded.fetch_add(1, std::memory_order_release);
        while (!go.load(std::memory_order_acquire)) std::this_thread::yield();
        split.store(old + 1, std::memory_order_relaxed);
    };
    first = std::async(std::launch::async, split_update);
    second = std::async(std::launch::async, split_update);
    while (loaded.load(std::memory_order_acquire) != 2) std::this_thread::yield();
    go.store(true, std::memory_order_release);
    first.get();
    second.get();
    assert(split.load(std::memory_order_relaxed) == 1);
    std::cout << "load-plus-store=1 updates=2\n";

    auto increment = [&] {
        for (int i = 0; i < 1000; ++i)
            combined.fetch_add(1, std::memory_order_relaxed);
    };
    first = std::async(std::launch::async, increment);
    second = std::async(std::launch::async, increment);
    first.get();
    second.get();
    assert(combined.load(std::memory_order_relaxed) == 2000);
    std::cout << "fetch-add=2000\n";
}
```

输出：

```text
load-plus-store=1 updates=2
fetch-add=2000
```

第一段是确定性的逻辑反例：两次更新最终只有 1。第二段使用原子 fetch_add，把“读旧值并累加”作为原子读改写操作，两个任务各执行 1000 次，最后得到 2000。

闸门守卫在 future 之后声明、在其之前析构，用于展开时先允许已启动的任务离开等待，再释放相关 async 状态；所有被捕获的原子对象则声明得更早。不要只看正常路径的 get，把异常路径上的等待与被引用状态销毁顺序漏掉。

这里 relaxed 只用于原子计数值自身的更新；最终结果在任务结束后检查，没有通过这个计数发布另一份普通载荷。常见内存序可先按职责读：

| 形式 | 在本课说明什么 | 没有额外保证什么 |
|---|---|---|
| relaxed | 原子访问及该原子对象的修改顺序 | 不提供程序四所需的普通载荷发布 |
| release store / acquire load | 满足读取对应发布值等条件时连接先后关系 | 不保活目标，也不代替消费完成反馈 |
| acq_rel 读改写 | 相应操作兼具获取与发布职责 | 不把多个独立对象自动变成事务 |
| 默认 seq_cst | 对相应顺序一致原子操作提供额外总序约束 | 不修复悬垂访问、错误协议或分开的 load/store 更新 |

内存序不是随意可交换的标签，load、store 和读改写各有允许的参数。atomic 也不普遍保证 lock-free；是否无锁与具体类型/实现有关。本课不展开 CAS、ABA、hazard pointer 或无锁内存回收。[C++17：atomic 接口与性质][atomic-type]

## 7. 哪些“看起来安全”的工具，还没有完成证明

| 已经做的事 | 得到了什么 | 仍要继续证明什么 |
|---|---|---|
| 复制 shared_ptr / weak_ptr::lock 成功 | 相应的共享保活 | 对象字段与容器的并发访问；同一个包装变量如何更新 |
| 捕获 const 引用或 this | 一条受类型限制的访问路径 | 目标寿命，别的线程是否仍在修改对象 |
| 使用 volatile | 按相应规则进行 volatile 访问 | 不提供 C++ 的原子性或线程间发布关系 |
| 把取消标志改为 atomic | 标志本身可合法并发访问 | 任务是否已观察标志、退出、排空，捕获对象何时能释放 |
| 给两个索引加锁/改 atomic | 覆盖这些索引的访问与所选协议 | 数据槽、容器借用和旧消费者的完成时刻 |

shared_ptr 的共享控制与其指向对象是不同层。不同包装副本可按库规则管理共享所有权，但被指向的普通成员不会因此自动加锁；同一个非原子包装变量被一方 reset、另一方读取，也需要合适同步。[C++17：shared_ptr][shared]

volatile 在可移植 C++ 中不能代替 mutex 或 atomic；特定编译器扩展、硬件设备访问协议也不应被搬来充当所有平台的线程规则。[C++17：cv 限定][cv]

此外，发送一个浅拷贝描述若仍含指向临时对象的借用，通道本身有锁也不会补齐目标寿命。把 vector 的内部引用留到解锁后使用，后续扩容/擦除也仍可能使它失效。继续回到 CXX-02、04、05 分别追拥有、位置与回调协议。

## 8. 两个编译失败，区分对象与状态的转交

```cpp
// atomic_copy_error.cpp: expected compile failure
#include <atomic>
int main()
{
    std::atomic<int> source{7};
    std::atomic<int> copy = source;
    return copy.load();
}
```

atomic 不能这样复制。若需要的是某一时刻的值，使用 `int copy = source.load();`，然后把它当作独立普通值；这是值快照，不是复制了一套持续同步的关系。

```cpp
// thread_copy_error.cpp: expected compile failure
#include <thread>
int main()
{
    std::thread first([] {});
    std::thread second = first;
    second.join();
}
```

thread 不允许复制拥有关系。改为 `std::thread second = std::move(first);` 后，等待责任转到 second；first 不再代表那条执行。本次修复变体补上 utility 头并通过编译检查。不能保留原来的两个包装，都自认为负责同一次 join。

两例分别以 `g++ -std=c++17 -Wall -Wextra -Wpedantic -pthread -fsyntax-only 文件名.cpp` 检查，预期非零退出，不执行失败产物。真正的数据竞争、悬垂、死锁和错误协议未必能被编译器发现，本节的诊断不能替代前面的运行时契约分析。

## 9. 回到四条源码路线，先找真实执行者

| 路线 | 用本课先核对什么 | 项目自己的约束 |
|---|---|---|
| Piccolo | 实际线程入口、值/借用、发布与归还，交换槽是否只是同线程角色切换 | Jolt/日志等实际任务边界、RenderScene 与 GPU 完成条件，不能按缓冲数量推导逻辑/渲染已并行 |
| Unreal | 工作任务提交、结果回送、对象保活、关闭等待分别在哪里 | 引擎对象的线程使用约定、Delegate/任务系统与引擎指针；公共 mutex 不自动授权任何线程访问 UObject |
| HybridCLR | 哪条原生线程执行解释/AOT 调用，共享状态怎样交接 | 托管线程接入、GC 可达性、runtime 状态与异常协议，不能用 native join 代替全部运行时协作 |
| IL2CPP | 原生执行者、生成/运行时对象的访问与完成条件 | 托管引用、GC、运行时初始化和目标平台约定；普通 atomic 不替它们登记根或初始化对象 |

Piccolo 从 [状态缓冲与线程边界]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}}) 继续。本文复用它的固定源码分层，新的 Mailbox 仍是自有教学模型，不是给 RenderSwapContext 加上了线程安全；CPU 的 join/mutex/atomic 也不代替 GPU fence 等设备完成协议。

UE 从 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}}) 接专项任务线程与对象规则；HybridCLR 继续读 [线程接入、GC 与运行时状态]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})，IL2CPP 则从 [运行时产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) 对应实现。这里列的是项目核对任务，不是四条引擎路线的新增运行验证。

先建立语言层的正确性，再讨论 [硬件多核与 False Sharing]({{< relref "performance/hardware-cpu-f05-multicore-false-sharing.md" >}}) 中的缓存与代价；某条路径是否更快、是否争用、是否需要填充，要由目标平台测量，不能从加锁/原子的名字直接得出结论。

## 10. 本篇怎样验证

五个程序分别保存，保留断言、不定义 NDEBUG。以第一个为例，Windows PowerShell 命令如下；其余换成各自文件名：

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic -pthread owner_and_join.cpp -o owner_and_join.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\owner_and_join.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

本例 GCC 工具链使用 `-pthread` 选择线程相关支持，分步编译/链接时应按工具链要求保持选项一致；其他编译器不机械照搬这个参数。[GCC 8.3：链接选项][gcc-thread]

2026-10-25，在 Windows x64、GCC 8.3.0（x86_64-posix-seh）下，五个正常程序均按上述 C++17/-pthread 参数编译运行成功，退出码为 0，所列选项下无警告，断言全部通过；输出分别为正文记录的 1、2、3、1、2 行。两个失败文件均以退出码 1 拒绝，诊断指向被删除的 atomic/thread 复制构造；load 快照与 thread 移动修复变体通过编译检查。

随后五个正常程序各额外运行 20 次，共 100 次进程运行，输出与断言均符合预期；测试器每次设 15 秒超时，未发生超时。这个上限是验证保护，不是程序的完成时延承诺，也不证明所有调度、虚假唤醒或硬件平台都已覆盖。

程序的合法性依据仍是各节的拥有、锁与 happens-before 推导。没有执行普通数据竞争、悬垂访问、relaxed 发布普通载荷的非法变体或无效内存序；没有执行 ThreadSanitizer、长期压力、线程/内存资源耗尽、系统同步调用失败、ARM/其他平台、引擎运行、GC 或 GPU 完成验证，也没有性能测量。

至此，公共目录的 11 个既定正文单元均已成稿。接下来按 [公共入口]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}}) 进入四条路线各自的专项前置与源码主线；公共小程序通过，不表示专项内容、项目构建、运行和工程验收已经完成。

**活到什么时候、谁能修改、怎样发布、何时完成，要分别给出依据。**

[races]: https://timsong-cpp.github.io/cppwp/n4659/intro.races
[thread-start]: https://timsong-cpp.github.io/cppwp/n4659/thread.thread.constr
[thread-join]: https://timsong-cpp.github.io/cppwp/n4659/thread.thread.member
[thread-dtor]: https://timsong-cpp.github.io/cppwp/n4659/thread.thread.destr
[mutex]: https://timsong-cpp.github.io/cppwp/n4659/thread.mutex.requirements.mutex
[guard]: https://timsong-cpp.github.io/cppwp/n4659/thread.lock.guard
[unique-lock]: https://timsong-cpp.github.io/cppwp/n4659/thread.lock.unique
[condvar]: https://timsong-cpp.github.io/cppwp/n4659/thread.condition.condvar
[async]: https://timsong-cpp.github.io/cppwp/n4659/futures.async
[future]: https://timsong-cpp.github.io/cppwp/n4659/futures.unique_future
[atomic-order]: https://timsong-cpp.github.io/cppwp/n4659/atomics.order
[atomic-type]: https://timsong-cpp.github.io/cppwp/n4659/atomics.types.generic
[shared]: https://timsong-cpp.github.io/cppwp/n4659/util.smartptr.shared
[cv]: https://timsong-cpp.github.io/cppwp/n4659/dcl.type.cv
[gcc-thread]: https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Link-Options.html
