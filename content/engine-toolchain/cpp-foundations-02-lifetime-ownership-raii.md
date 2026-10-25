---
title: "C++ 公共基础 02｜对象与资源由谁结束"
slug: "cpp-foundations-02-lifetime-ownership-raii"
date: "2026-10-25"
description: "分开作用域、对象寿命、资源释放和业务停用，用 RAII、独占拥有、共享持有与弱引用，建立四条源码路线共用的生命周期和所有权模型。"
tags: ["C++", "源码阅读", "RAII", "智能指针", "生命周期"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 2
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "对象与资源由谁结束"
reading_summary: "沿着拥有关系判断谁负责释放，再检查借用、业务状态和清理协议是否仍然有效。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 492
---

上一篇区分了对象、指针变量和引用别名，也留下了一个问题：拿到了对象的地址，谁保证使用期间对象仍然存在？

读源码时，这个问题往往藏在几行普通代码里。函数提前返回，文件还要不要手动关闭？从容器删除一个智能指针，对象会不会立即析构？给某个系统调用了 `clear()`，其他地方保存的指针还能不能用？

这些问题不能只靠“有没有 `delete`”回答。我们需要同时追踪两条线：**谁负责维持对象或资源，谁只是暂时访问它；拥有者结束时，实际执行什么清理协议。**

> **读这篇之前**：先读 [CXX-01：值、指针、引用与函数签名]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})，能分清指针变量与指向对象即可。
>
> **材料与范围**：本篇从 [UFS 首课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 的 RAII 与独占责任、[Piccolo 智能指针前置]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) 的共享持有与逻辑清理模型中提取共性。例子是自有 C++17 程序；项目里的对象系统、托管 GC、线程与图形资源协议要回到各自路线继续学习。

## 1. 名字、对象、资源和业务状态是四件事

先把容易混用的词拆开：

| 概念 | 回答的问题 | 一个具体例子 |
| --- | --- | --- |
| 作用域 | 在哪段代码里能用这个名字？ | 局部变量名 `file` 在函数块中可见 |
| 对象生命周期 | 哪个对象已经完成初始化，何时结束寿命？ | 局部智能指针对象与它管理的目标对象各有自己的寿命 |
| 资源有效区间 | 哪种外部能力已经取得，何时被归还？ | 打开的文件需要 `fclose`，锁需要按协议解锁 |
| 业务状态 | 对象此刻是否允许执行某种业务？ | 对象仍存在，但已经从世界移除或标记为停止服务 |

作用域是名字的规则，生命周期是运行时对象的规则。一个指针变量还在作用域内，不代表它指向的对象仍存在；名字离开作用域，也不一定销毁它曾指向的对象。

资源又不局限于动态分配的内存。文件、锁、数据库事务以及某些平台句柄，都有取得与归还的协议。C++ 不会仅凭一个整数或裸指针，自动推断它代表哪种资源、应该调用哪个释放函数。

还要分清**对象销毁过程与存储释放**。例如普通局部对象退出作用域会析构，但不需要为它调用 `delete`；动态创建的对象则可能先执行析构，再由对应的分配机制回收存储。严格按语言规则说，类对象的寿命在析构函数调用开始时结束，析构过程仍继续完成成员和基类清理，所以一条“进入析构函数”的日志不等于存储已经归还。[对象生命周期规则](https://eel.is/c++draft/basic.life)

读业务代码时，可以先使用一个更实用的检查顺序：

1. 业务上是否仍允许使用？
2. 是否有有效的拥有关系，保证这次访问所需的对象和资源仍在？
3. 最后一个拥有者放手时，谁调用哪一个清理函数？

第三问就是本篇的主线：**所有权是一份责任，不是“我手里有地址”的同义词。**

## 2. RAII 把释放责任放进对象

RAII 的全称是 Resource Acquisition Is Initialization，通常译作“资源获取即初始化”。理解它时，先不要背缩写，而要看它安排了什么：成功取得资源后，立即让一个对象承担释放责任；这个管理对象销毁时，通过析构完成对应清理。[C++ Core Guidelines R.1](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rr-raii)

这让调用者不必在每一个 `return`、每一个异常出口后重复写释放代码。清理职责集中在资源管理类型中，使用资源的函数主要表达正常业务流程。`std::string`、`std::vector` 和智能指针都在运用这种思想；RAII 并不要求对象一定在堆上。

这里有三个需要记牢的顺序：

- 普通局部自动对象离开作用域时，按完成构造的逆序销毁。
- 一个完整类对象析构时，先执行析构函数体，再销毁成员；成员按声明的逆序销毁，而不是按构造函数初始化列表的书写顺序。
- 构造函数中途抛出异常时，尚未完成构造的外层对象不会执行自己的析构函数体，但已经构造完成的成员和基类会被清理。

这里讨论普通自动对象；局部 `static` 等有不同的存储期。异常路径也特指能够展开到相应处理器的情形，不把强制终止进程或所有终止路径都当成正常作用域退出。[局部对象与作用域退出](https://eel.is/c++draft/stmt.dcl)、[析构顺序](https://eel.is/c++draft/class.dtor)、[构造与异常清理](https://eel.is/c++draft/except.ctor)

第三条很关键。如果先把文件放进裸指针成员，再执行一段可能失败的初始化，把关闭动作只写在外层对象的析构函数里，那么外层构造失败时，这份关闭责任就可能落空。让一个已经完成构造的资源成员接管文件，才能利用成员清理规则兜住这个出口。

### 完整练习一：提前返回和构造失败，谁关闭文件

下面的程序保存为 `raii.cpp`，在这个文件所在的目录编译、运行。先用两个局部对象观察退出顺序，再以只读方式打开自己的源文件，观察真实的取得与关闭。程序不会修改这个文件。

先认识几处写法：构造函数与类同名，析构函数在类名前加 `~`，两者都没有返回类型；`: file(...)` 表示在进入构造函数体前初始化成员。`explicit` 限制隐式转换，完整的类规则留到下一篇。

`using File = ...` 给类型起别名；`FileCloser::operator()` 让对象能够像函数一样被调用。这里的 `unique_ptr` 管理一个 `std::FILE*`，释放时调用我们提供的 `FileCloser`，而不是默认的 `delete`。

```cpp
#include <cassert>
#include <cstdio>
#include <iostream>
#include <memory>
#include <stdexcept>

struct Trace
{
    const char* name;
    explicit Trace(const char* value) : name(value)
    {
        std::cout << "construct " << name << '\n';
    }
    ~Trace() { std::cout << "destroy " << name << '\n'; }
};

void ScopeDemo(bool stop)
{
    Trace first("first");
    Trace second("second");
    if (stop)
    {
        std::cout << "return from ScopeDemo\n";
        return;
    }
    std::cout << "work continues\n";
}

int close_calls = 0;
int close_errors = 0;

struct FileCloser
{
    void operator()(std::FILE* file) const noexcept
    {
        ++close_calls;
        if (std::fclose(file) != 0)
            ++close_errors;
        std::puts("fclose called");
    }
};

using File = std::unique_ptr<std::FILE, FileCloser>;
struct InitFailed {};

struct Session
{
    File file;

    explicit Session(bool fail) : file(std::fopen("raii.cpp", "rb"))
    {
        if (!file)
            throw std::runtime_error("cannot open raii.cpp; run beside the source file");
        std::cout << "file acquired\n";
        if (fail)
        {
            std::cout << "session construction fails\n";
            throw InitFailed{};
        }
    }

    ~Session() { std::cout << "Session destructor body\n"; }
};

void OpenSession(bool fail)
{
    Session session(fail);
    std::cout << "session ready, return\n";
    return;
}

int main()
{
    ScopeDemo(true);
    try
    {
        OpenSession(false);
        try
        {
            OpenSession(true);
            return 2; // 本例要求这次构造抛出 InitFailed。
        }
        catch (const InitFailed&)
        {
            std::cout << "caught init failure\n";
        }
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        return 1;
    }

    assert(close_calls == 2 && close_errors == 0);
    std::cout << "close calls=" << close_calls
              << ", errors=" << close_errors << '\n';
}
```

本次本地运行的输出如下；如果源文件不存在或不可读，程序会报告错误并以非零状态退出：

```text
construct first
construct second
return from ScopeDemo
destroy second
destroy first
file acquired
session ready, return
Session destructor body
fclose called
file acquired
session construction fails
fclose called
caught init failure
close calls=2, errors=0
```

`ScopeDemo(true)` 在分支中提前返回，后续工作没有执行，但两个已构造对象仍按逆序销毁。`OpenSession(false)` 正常返回时，`session` 是完整对象，因此先执行 `Session` 析构函数体，再由成员 `file` 调用删除器关闭文件。失败路径中，`Session` 没构造完，所以没有第二条 `Session destructor body`；但 `file` 已经完成构造，它仍会被销毁并关闭已经取得的文件。

这个程序验证的是只读文件的关闭调用及其返回状态，没有验证写入持久性或其他文件系统行为。`close_errors` 是单线程练习的观测变量，实际库应该提供适合自身错误处理方式的接口。

还有一个设计边界：**RAII 保证按对象规则进入清理，不保证外部清理操作永远成功。** 删除器不应通过抛异常报告失败；`noexcept` 也不会把失败变成成功。若“关闭成功”“提交成功”影响业务结果，就应提供可检查结果的显式操作，并让析构承担约定好的收尾职责。项目禁用异常时，也可以在返回错误码的流程里使用 RAII；本例的异常展开部分不直接套用到那种构建设置。

## 3. unique_ptr：同一时刻，一份明确的释放责任

`std::unique_ptr<T>` 把一个目标的独占释放责任放进一个对象里。默认删除器通常通过 `delete` 结束目标；前面的 `File` 则明确换成了 `fclose`。**管理工具和释放协议必须配对**，不能看到一个裸指针，就随手交给默认删除器。

几个操作看起来都与“指针变化”有关，责任变化却不同：

| 操作 | 责任如何变化 | 使用时要追问什么 |
| --- | --- | --- |
| `owner.get()` | 返回借用地址，拥有者继续负责释放 | 借用结束前，拥有者会不会释放目标？ |
| `std::move(owner)` 用于构造另一个 `unique_ptr` | 接收方取得责任，源智能指针变空 | 后续业务应该使用哪个拥有者？ |
| `owner.reset()` | 放弃当前目标并调用删除器，自己变空 | 是否仍有人借用这个目标？ |
| `owner.release()` | 返回地址并放弃责任，不调用删除器 | 哪个新拥有者立即接管释放责任？ |
| `owner` 自身销毁 | 非空时调用删除器 | 删除器是否匹配目标的取得方式？ |

`release()` 的名字尤其容易误导：它释放的是智能指针的管理责任，不是被管理的资源。没有明确的接收协议时，不应该用它逃开所有权问题。[unique_ptr 修改操作](https://eel.is/c++draft/unique.ptr.single.modifiers)、[unique_ptr 析构](https://eel.is/c++draft/unique.ptr.single.dtor)

独占责任不能通过普通复制一分为二。因此下面是一个**应该编译失败**的独立练习，保存为 `unique_copy_error.cpp`：

```cpp
#include <memory>

int main()
{
    auto owner = std::make_unique<int>(7);
    auto copy = owner; // 错误：unique_ptr 的复制构造被删除。
}
```

如果接口确实需要接管资源，可以显式移动。注意，`std::move` 本身只是把表达式转换成允许被移动的形式；实际转移发生在接收方的移动构造或移动赋值中。`unique_ptr` 移动后源指针为空，是这个类型的契约，不是所有 C++ 类型移动后的统一状态。[unique_ptr 构造规则](https://eel.is/c++draft/unique.ptr.single.ctor)

对象的拥有者改变，已有借用不一定立刻失效。例如移动一个 `unique_ptr` 并没有把目标对象搬到另一个地址，原先 `get()` 得到的地址仍可能有效；但它现在依赖的是新拥有者的寿命。新拥有者随后 `reset()`，借用就失去了目标。

## 4. shared_ptr 和 weak_ptr：谁保活，谁只是观察

有时确实有多方需要独立维持同一个对象的存活。这时 `shared_ptr<T>` 的复制会加入同一个共享拥有关系。它复制的是持有关系，不是复制一份 `T`。

一个 `shared_ptr` 退出或 `reset()`，只表示这一份持有结束。对这里由 `make_shared<T>()` 创建的普通对象，最后一份强持有结束时才会销毁 `T`。如果使用自定义删除器，还要按删除器的契约判断真正归还了什么。[共享指针及其析构规则](https://eel.is/c++draft/util.smartptr.shared.dest)

这也解释了为什么从 `vector<shared_ptr<T>>` 中 `erase` 一个元素，未必马上析构 `T`：容器中的那份持有消失了，其他持有者可能仍在。相反，仅仅从 `vector<T*>` 删除一个裸指针元素，通常连“释放目标”的责任都没有执行。

`weak_ptr<T>` 用来观察某个共享拥有关系，**它不维持目标存活**。使用目标前调用 `lock()`：成功得到一个 `shared_ptr`，在这个结果持有期间目标得到保活；失败得到空指针，需要走目标已不存在的分支。[weak_ptr 观察与 lock](https://eel.is/c++draft/util.smartptr.weak.obs)

不要先调用 `expired()`，再假定之后一定能够访问目标；观察与使用之间可能发生变化。直接检查一次 `lock()` 的结果，并把它保留到使用结束，才表达了这段代码需要的寿命保证。

`lock()` 这个名字也不代表给对象内容加了互斥锁。共享持有解决的是拥有关系；对对象字段的并发访问、对同一个智能指针变量的并发修改，仍需要各自的同步规则。业务上把 `active` 设为 `false`，同样不会自动消灭所有强持有者。

### 完整练习二：转交责任、临时保活与业务停用

保存为 `ownership.cpp`。`Item` 的构造和析构只打印编号；`active` 表示示例里的业务可用状态，与对象是否销毁分开观察。

```cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <utility>

struct Item
{
    int id;
    bool active = true;
    explicit Item(int value) : id(value)
    {
        std::cout << "construct " << id << '\n';
    }
    ~Item() { std::cout << "destroy " << id << '\n'; }
};

int main()
{
    std::cout << std::boolalpha;

    auto owner = std::make_unique<Item>(1);
    Item* borrowed = owner.get();
    auto receiver = std::move(owner);
    assert(!owner && receiver.get() == borrowed);
    std::cout << "source empty=" << !owner
              << ", same target=" << (receiver.get() == borrowed) << '\n';
    receiver.reset();
    // borrowed 已失效，后面不再读取或解引用它。

    auto first = std::make_shared<Item>(2);
    auto second = first;
    std::weak_ptr<Item> observer = first;
    assert(first.use_count() == 2);
    std::cout << "strong owners=" << first.use_count() << '\n';

    auto pin = observer.lock();
    assert(pin && pin.use_count() == 3);
    std::cout << "after lock=" << pin.use_count() << '\n';
    first.reset();
    second.reset();
    assert(pin.use_count() == 1);

    pin->active = false;
    std::cout << "active=" << pin->active
              << ", expired=" << observer.expired() << '\n';
    pin.reset();
    assert(observer.expired() && !observer.lock());
    std::cout << "expired after last owner=" << observer.expired() << '\n';
}
```

本次运行输出：

```text
construct 1
source empty=true, same target=true
destroy 1
construct 2
strong owners=2
after lock=3
active=false, expired=false
destroy 2
expired after last owner=true
```

把这段输出对回责任变化：

- `Item 1` 的拥有者从 `owner` 变成 `receiver`；直到 `receiver.reset()` 才析构目标，借用地址没有参与保活。
- `Item 2` 最初有两份强持有，`observer` 没增加这个数量；成功的 `lock()` 创建了第三份强持有。
- 前两份强持有结束后，`pin` 仍维持目标。`active=false` 只是业务停用，直到 `pin.reset()` 才开始销毁目标。
- `observer` 变量仍存在，但已经不能通过 `lock()` 得到对象。这是观察者存在与目标存在的区别。

这里的 `use_count()` 只用于封闭的单线程练习，帮助核对持有关系；不能据此设计“看到计数为 1，所以并发访问一定安全”的业务判断。

另一个常见错误是把同一个裸地址分别交给两个独立创建的 `shared_ptr`。它们不会自动识别出“这是同一组拥有者”，可能各自释放同一目标。共享关系应该来自已有 `shared_ptr` 的复制或相应共享机制；不要把 `get()` 返回的借用地址当成新资源接管。

还要保留一条内存边界：目标已经析构，不等于与共享管理有关的所有存储都已回收。弱引用仍需要管理信息来判断能否提升；`make_shared` 的实现还可能将对象与管理信息放在同一块分配中，使那块分配保留到弱引用不再需要它为止。这不代表目标仍可访问，也不是本例测出的分配次数或回收时点。[make_shared 创建规则](https://eel.is/c++draft/util.smartptr.shared.create)

## 5. 强引用环：每个对象都在等待别人先放手

共享拥有还有一个结构问题。假设 A 的成员强持有 B，B 的成员又强持有 A。外部拥有者全部退出后，A 和 B 的强持有计数仍然不为零；双方都等不到“最后一份强持有结束”。

这与对象是否还在业务容器里无关，也不会因为外部变量离开作用域就自动解开。

### 完整练习三：观察未析构，再显式断开环

保存为 `cycle.cpp`。`alive` 是所有 Node 实例共用的计数成员，C++17 的 `inline static` 写法允许在这里直接定义它。为了让这个练习结束时不遗留资源，我们留下一个弱观察者，在确认环仍存在后，将其中一条强持有边断开。

```cpp
#include <cassert>
#include <iostream>
#include <memory>

struct Node
{
    inline static int alive = 0;
    std::shared_ptr<Node> next;
    Node() { ++alive; }
    ~Node() { --alive; }
};

int main()
{
    std::weak_ptr<Node> observer;
    {
        auto a = std::make_shared<Node>();
        auto b = std::make_shared<Node>();
        a->next = b;
        b->next = a;
        observer = a;
    }

    assert(Node::alive == 2 && !observer.expired());
    std::cout << "after outside owners leave=" << Node::alive << '\n';
    {
        auto pin = observer.lock();
        assert(pin);
        pin->next.reset();
    }
    assert(Node::alive == 0 && observer.expired());
    std::cout << "after breaking the cycle=" << Node::alive << '\n';
}
```

本次运行输出：

```text
after outside owners leave=2
after breaking the cycle=0
```

断环过程可以一步步追踪：`pin` 暂时持有 A；清空 A 的 `next` 后，B 失去最后一份强持有而开始销毁；B 的 `next` 随之释放它对 A 的强持有；最后 `pin` 离开作用域，A 也被销毁。

正式设计中，应先判断关系是否需要拥有。例如父节点拥有子节点，而子节点只是为了查询父节点，回指就可以使用 `weak_ptr`，在访问时再 `lock()`。不能为了消除引用环而任意把关系改成弱引用：如果两边都只是观察，真正负责维持目标的拥有者可能反而消失了。

这个练习中的显式断环是可控的清理动作，不是一个自动追踪可达性的垃圾收集器。

## 6. 把责任读进接口，也写进接口

上一篇从参数形式判断“访问了谁”，这一篇再叠加“有没有取得责任”。下面是阅读普通 C++ 接口的起点，具体是否保存参数、是否触发业务关闭，仍然要查函数体与约定：

| 参数形式 | 可以先作出的判断 | 还要查的内容 |
| --- | --- | --- |
| `T&` / `const T&` / `T*` | 传递访问路径，语法本身不增加拥有关系 | 是否允许为空、是否保存借用、调用期间由谁保活 |
| `unique_ptr<T>` 按值 | 接口接收一份可转移的独占责任 | 是立即使用后释放，还是转存到更长寿的拥有者 |
| `shared_ptr<T>` 按值 | 参数自身参与共享持有 | 是否继续复制保存，延长多久 |
| `const shared_ptr<T>&` | 参数本身没有新建一份强持有 | 是否在函数中复制保存；不要把指针包装的 const 当成 T 的 const |
| `weak_ptr<T>` | 可以观察并尝试临时取得强持有 | lock 失败时如何处理，成功结果保留到哪里 |

裸指针在一些 C API 或历史代码中也可能表示移交责任，所以“看到 `T*` 就断言绝不拥有”同样不可靠。好的封装会把这个契约写清楚，再用资源管理类型承接它。

选择类型时，可以从简单责任开始：能够按值作为成员，就先用值；确实需要独立分配且只有一个拥有者，再考虑 `unique_ptr`；只有多方必须各自维持目标时，才使用 `shared_ptr`。共享持有增加的不只是一个计数机制，还可能延长资源存在的时间、形成环，并使最后一次清理发生在不直观的位置。

回到源码，可以沿着五个动作做笔记：**谁创建 → 谁保存拥有关系 → 谁只借用 → 谁停止业务 → 谁最终释放。** 如果它们不是同一个对象或同一个函数，就把边界明确写出来。

## 7. 四条源码路线怎样复用这个模型

| 路线 | 带着本篇去追踪什么 | 仍需补充的项目规则 |
| --- | --- | --- |
| Piccolo | 从上下文、世界、对象和组件的持有关系追到 clear / reset / 析构 | 业务清理顺序、借用失效与资源系统协议；继续读 [智能指针与寿命前置]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) |
| Unreal | 区分普通 C++ 资源管理与 UObject 对象系统中的引用、有效性判断 | 项目智能指针类型、反射与 GC 约定；从 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}}) 进入 |
| HybridCLR | 分开 native 辅助对象、元数据结构和托管对象的责任 | 解释执行与运行时共同遵守的对象、句柄和 GC 协议；从 [ECMA-335 系列]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) 进入 |
| IL2CPP | 在生成代码与运行时调用之间，区分 C++ 包装对象和被管理的托管目标 | 元数据寿命、GC、写屏障等运行时约定；从 [IL2CPP 阅读入口]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) 进入 |

这些是下一步的阅读问题，不是独立程序已经验证的项目结论。尤其不能用“C++ 局部对象离开作用域会析构”，推导“托管对象在某次 C++ 函数返回时就会被 GC 回收”；也不能把“有一个非空地址”推导成“引擎业务对象仍然有效”。

## 8. 自测与本篇验证边界

读完后，试着不运行程序回答：

1. `Session` 构造失败时，为什么没有执行自己的析构函数体，文件却仍能关闭？
2. `unique_ptr` 移动之后，为什么旧借用可能还有效？哪个动作会让它失去目标？
3. `shared_ptr` 所在的容器删除一个元素后，为什么目标可能仍存在？
4. `weak_ptr` 变量还存在，为什么 `lock()` 可以返回空？成功时又是哪一个对象负责保活？
5. 把对象标记为停用、销毁对象、回收存储，为什么不能用同一条日志证明？

如果能把每个问题对应到具体拥有者与具体清理动作，生命周期就不再只是“什么时候调用析构”这一个问题。

验证日期为 2026-10-25，环境是 Windows x64、GCC 8.3.0（`x86_64-posix-seh`），语言模式为 C++17。这只是当前教学程序的验证环境，不代表四个项目的工具链要求。

把三个程序分别保存为对应文件，在源文件所在目录执行编译和运行。以第一个程序为例，PowerShell 命令如下；另外两个把文件名换成 `ownership`、`cycle` 即可：

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic raii.cpp -o raii.exe
.\raii.exe
```

只有编译成功后才运行程序；若 `g++` 不在 PATH 中，需要使用自己实际安装的编译器路径。这里不定义 `NDEBUG`，保留断言检查。复制失败练习单独使用 `g++ -std=c++17 -fsyntax-only unique_copy_error.cpp`，预期它报告被删除的复制构造函数。

| 用例 | 本次结果 | 核对内容 |
| --- | --- | --- |
| `raii.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 14 行输出逐行一致；两次 fclose 调用均返回成功，构造失败路径没有执行外层析构函数体 |
| `ownership.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 9 行输出逐行一致；独占转交、弱提升和最后一次强持有释放符合断言 |
| `cycle.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 2 行输出逐行一致；环内两个对象在外部持有结束后仍存在，断环后全部销毁 |
| `unique_copy_error.cpp` | 编译退出码为 1，符合预期 | 诊断指向被删除的 unique_ptr 复制构造函数，没有执行程序 |

正常程序均通过断言；本次没有执行悬空访问、双重释放或故意遗留未清理的引用环。示例的构造/析构日志用于解释顺序，不是性能测量。

本篇没有完成四个引擎或运行时的构建、调试、GC 观测、图形资源释放、跨线程压力测试及性能测量。语言规则依据 C++ 工作草案与 Core Guidelines 核对，例子限定在 C++17；不把编译器的某次输出扩展成所有平台的实现保证。

下一篇进入 [CXX-03：一次成员调用执行谁]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}})，从静态接口和对象来源追踪重载、覆盖、切片与虚析构。项目应用继续读 [Piccolo 类、继承与虚函数前置]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})，完整顺序见 [C++ 公共基础目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。
