---
title: "C++ 公共基础 09｜有内存为什么还没有对象：分配、构造、析构与未定义行为"
slug: "cpp-foundations-09-storage-object-lifetime-ub"
date: "2026-10-25"
description: "用独立 C++17 程序分开存储、对象寿命、资源清理与访问路径，验证原位构造、异常回滚、union 活跃成员和对齐释放，并解释未定义行为的证据边界。"
tags: ["C++", "源码阅读", "对象寿命", "内存管理", "未定义行为"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 9
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "有内存为什么还没有对象：分配、构造、析构与未定义行为"
reading_summary: "存储先满足范围与对齐，对象再按规则开始和结束寿命；清理资源、归还空间与继续访问分别核对。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 499
---

从对象池取回一个地址，为什么还不能直接调用成员函数？已经执行析构，为什么那块空间仍能留着给下一个对象？Arena 把偏移清零，是否就已经替里面的对象释放了字符串、文件或其他资源？

这些问题都来自同一个接缝：**有一段可用存储，不等于所有想放进去的对象都已经存在；对象结束，也不等于空间已经归还。** 普通 new/delete 常把几步连接起来，读到底层分配、原位构造和运行时存储时，需要重新拆开。

> **读这篇之前**：先掌握 [CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) 和 [CXX-06 布局与字节访问]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})。容器失效与虚析构按需回看 CXX-04、CXX-03。
>
> **复用与范围**：复用上述两课的释放责任、异常清理、对齐和类型访问模型，以及 [分配器专题]({{< relref "system-design/ds-19-memory-allocators.md" >}}) 对 Arena/Pool 的空间策略划分。C# 托管对象池、原生存储池与 C++ 对象寿命分别讨论，不沿用按名字推断性能或 GC 行为的结论。
>
> **标准基线**：采用 C++17 工作草案 N4659，正常例子以需要构造/析构的类为主。这里不会笼统声称“任何原始分配在所有标准版本下都绝不创建任何对象”；隐式对象创建、缺陷修订和较新工具另有规则。本文使用 placement new 明确创建对象，不依赖 C++20 的 construct_at 或 C++23 的 start_lifetime_as。

## 1. 先给一段地址补上四份记录

| 层次 | 要记录什么 | 不能用什么代替 |
|---|---|---|
| 存储 | 谁提供空间、大小、对齐、何时回收或复用 | 指针非空 |
| 对象 | 哪种类型、寿命何时开始/结束、union 哪个成员活跃 | 强制转换后的指针类型 |
| 资源 | 字符串、句柄等由谁清理，失败时谁接手 | 只移动分配器偏移或只释放原始块 |
| 访问路径 | 当前指针/引用是否指向可访问的对象，类型是否允许 | 地址数值没变 |

本篇 Item 持有 string，具有非平凡初始化：不仅要取得足够且对齐的存储，还要完成相应初始化，才能把它作为已构造的完整对象使用。构造和析构过程有自己的访问规则，不能把这句话误读成构造函数体也不能操作自己的成员。[C++17：对象寿命][life]、[构造与析构期间][cdtor]

相应地，显式析构能结束这种对象的寿命并清理成员，但不替外部空间管理者归还存储。非平凡类的寿命结束点与析构调用有关；后文的“清理完成”还包括其成员的析构，不用一个计数混掉所有阶段。

对象存在也不代表每个值已经适合读取。例如普通局部 `int n;` 不是“没有 int 对象”，而是没有给它提供确定的初值；本篇不读取这种不确定值。反过来，把一块存储清成零，也不能通用地替代 string 等类的构造。**存储、寿命、值初始化和业务可用状态要分别判断。**[C++17：初始化](https://timsong-cpp.github.io/cppwp/n4659/dcl.init)

先对照几个容易混淆的写法：

| 写法 | 本篇关注的动作 | 额外责任 |
|---|---|---|
| `new T(args)` | new 表达式选择分配函数并初始化对象 | 正常拥有者与对应销毁方式 |
| `::operator new(bytes)` | 直接取得原始存储，不运行本篇 T 的构造函数 | 对齐、构造、异常与最终释放 |
| `::new (address) T(args)` | 本篇的标准非分配原位形式，在给定位置初始化 T | 调用者提供有效、足够、对齐且可复用的存储 |
| `p->~T()` / `std::destroy_at(p)` | 对已存在的相应对象执行析构 | 不据此推断存储已经归还 |
| `delete p` | 对符合条件的 new 对象执行销毁与相应释放 | 必须符合对象来源、类型与释放规则 |
| `::operator delete(raw)` | 释放匹配的原始存储 | 不自动调用其中对象的析构函数 |

`new (address)` 里的括号是传给分配函数的实参，不是“新申请一个地址”。本篇显式使用 `::new` 和 void*，选择标准库提供的非分配 placement 形式；它返回给定地址，其他放置参数形式可以有项目自定义行为。[C++17：new 表达式][new-expr]、[标准原位形式][placement]

## 2. 同一块空间，先后放两个对象

这个程序用有自动存储期的 byte 数组提供空间，`alignas(Item)` 与 `sizeof(Item)` 分别满足本例所需的对齐和大小。数组本身已经是对象，问题是其中是否存在我们要使用的 Item。

```cpp
// placement_lifetime.cpp
#include <cassert>
#include <cstddef>
#include <iostream>
#include <memory>
#include <new>
#include <string>
struct Item
{
    inline static int live = 0;
    inline static int destroyed = 0;
    std::string name;
    explicit Item(const char* text) : name(text) { ++live; }
    Item(const Item&) = delete;
    Item& operator=(const Item&) = delete;
    ~Item() noexcept { --live; ++destroyed; }
};
struct DestroyOnly
{
    void operator()(Item* p) const noexcept { p->~Item(); }
};
int main()
{
    alignas(Item) std::byte storage[sizeof(Item)];
    using Object = std::unique_ptr<Item, DestroyOnly>;
    Object object{::new (static_cast<void*>(storage)) Item("first")};
    Item* borrow = object.get();
    assert(Item::live == 1 && borrow->name == "first");
    std::cout << "constructed=first live=1\n";
    object.reset();
    borrow = nullptr;
    assert(Item::live == 0 && !borrow);
    std::cout << "destroyed=live0 storage-retained\n";
    object.reset(::new (static_cast<void*>(storage)) Item("second"));
    assert(Item::live == 1 && object->name == "second");
    assert(static_cast<void*>(object.get()) == static_cast<void*>(storage));
    std::cout << "reconstructed=second live=1\n";
    object.reset();
    assert(Item::live == 0 && Item::destroyed == 2);
    std::cout << "released=live0 destructions=2\n";
}
```

输出：

```text
constructed=first live=1
destroyed=live0 storage-retained
reconstructed=second live=1
released=live0 destructions=2
```

这里有两种责任：数组的存储随所在作用域结束；Object 的删除器只调用 Item 析构。第一次 reset 后 string 被清理，但数组仍在作用域内；第二次 placement new 使用同一存储，返回指向新 Item 的指针。

不能把这个 Object 换成默认删除器的 `unique_ptr<Item>`，也不能对它随手写 `delete object.get()`：这不是普通 new 获得、等待该 delete 归还的一块独立存储。定制删除器的用途是准确表达清理协议，不是让任意地址都获得合法对象身份。

程序主动清掉旧 borrow，并使用第二次 new 返回的指针。**地址复用不等于业务对象相同，也不单独证明旧引用可继续使用。** 某些同类型替换符合透明替换规则，另一些涉及 const、子对象或旧指针的情况需要进一步核对；这里不靠这些复杂条件继续使用旧借用。

`std::launder` 也不是补构造的工具：它要求目标位置已经存在寿命内、类型相应且满足可达性等条件的对象。先缺对象，再套一次 launder，不能恢复合法性。[C++17：launder 前置条件][launder]

不要显式析构一个普通自动局部对象后直接离开作用域，让隐式析构再次作用于已经结束寿命的对象。若要手动管理构造/销毁，就把责任封装起来；本例的自动局部对象是存储数组和 RAII 包装，Item 的销毁由包装明确管理。

## 3. 构造失败，谁负责清掉已经完成的部分

CXX-02 已说明：一个完整对象构造没有成功，不能期待它自己的析构函数体负责回收所有东西；已经完成初始化的成员/基类按规则清理。现在再把外面的存储加进来。[C++17：异常与已构造成员][exceptions]

下面用整数事件记录顺序，避免把成员日志和分配次数混在一起。1、2 表示两个成员构造完成，-2、-1 表示其析构，9 表示完整 Widget 的析构函数体。类专用分配/释放函数只统计 Widget 自己的空间，不是统计程序的一切内存申请。

```cpp
// construction_rollback.cpp
#include <algorithm>
#include <cassert>
#include <cstddef>
#include <initializer_list>
#include <iostream>
#include <memory>
#include <new>
int events[8]{};
std::size_t used = 0;
struct Trace
{
    int id;
    explicit Trace(int value) noexcept : id(value) { events[used++] = id; }
    ~Trace() noexcept { events[used++] = -id; }
};
bool order(std::initializer_list<int> expected)
{
    return used == expected.size() && std::equal(expected.begin(), expected.end(), events);
}
struct ConstructionError {};
struct Widget
{
    inline static int allocated = 0;
    inline static int freed = 0;
    inline static int destroyed = 0;
    Trace first{1};
    Trace second{2};
    explicit Widget(bool fail) { if (fail) throw ConstructionError{}; }
    ~Widget() noexcept { ++destroyed; events[used++] = 9; }
    static void* operator new(std::size_t bytes)
    {
        void* p = ::operator new(bytes);
        ++allocated;
        return p;
    }
    static void operator delete(void* p) noexcept
    {
        ++freed;
        ::operator delete(p);
    }
};
int raw_freed = 0;
struct RawFree
{
    void operator()(void* p) const noexcept { ::operator delete(p); ++raw_freed; }
};
int main()
{
    try { (void)new Widget(true); assert(false); }
    catch (const ConstructionError&) {}
    assert(order({1, 2, -2, -1}));
    assert(Widget::allocated == 1 && Widget::freed == 1 && Widget::destroyed == 0);
    std::cout << "new-failure=members-unwound object-dtor=0 storage-freed=1\n";

    used = 0;
    std::unique_ptr<void, RawFree> block{::operator new(sizeof(Widget))};
    try { (void)::new (block.get()) Widget(true); assert(false); }
    catch (const ConstructionError&) {}
    assert(order({1, 2, -2, -1}));
    assert(block && raw_freed == 0 && Widget::freed == 1);
    std::cout << "placement-failure=members-unwound storage-retained\n";
    block.reset();
    assert(raw_freed == 1 && Widget::destroyed == 0);
    std::cout << "manual-release=1 object-dtor=0\n";

    used = 0;
    { std::unique_ptr<Widget> complete{new Widget(false)}; }
    assert(order({1, 2, 9, -2, -1}));
    assert(Widget::allocated == 2 && Widget::freed == 2 && Widget::destroyed == 1);
    std::cout << "success=object-dtor=1 matched-class-delete=2\n";
}
```

输出：

```text
new-failure=members-unwound object-dtor=0 storage-freed=1
placement-failure=members-unwound storage-retained
manual-release=1 object-dtor=0
success=object-dtor=1 matched-class-delete=2
```

三条路径分别核对：

1. 普通 `new Widget(true)`：成员依次完成后构造抛出，事件是 `1,2,-2,-1`。完整 Widget 的析构函数体没有运行；本例查找到的匹配类专用 delete 归还此次分配。
2. `::new (block.get()) Widget(true)`：成员同样清理，但标准非分配 placement delete 不负责释放调用者的 block。RawFree 仍是原始存储的拥有者，reset 后才释放它。[标准 placement delete][placement]
3. 成功构造并交给默认 unique_ptr：事件是 `1,2,9,-2,-1`，最后通过匹配的类专用 delete 释放。累计分配/释放为 2 次，完整析构为 1 次。

不能把第一条简化成“构造失败永远会自动帮我 free 所有内存”：释放函数的查找/匹配、placement 参数与分配协议都有关；构造函数体自己额外取得、却未交给 RAII 管理的其他资源，也不会因此得到统一回收。原始空间与对象两层都要有异常路径上的负责者。[C++17：初始化失败与释放查找][new-expr]

这里的 Trace 成员按声明顺序初始化，清理按已完成初始化的逆序进行；不是按构造函数初始化列表的书写顺序随意挑选。本次只主动抛出 ConstructionError，没有注入分配失败，也没有测试析构抛出。

## 4. union 里有空间，不代表所有成员都活跃

union 为成员提供重叠存储，不代表可以同时把里面的数据按所有成员类型读取。通常需要知道当前活跃成员；公共初始序列等有专门例外，不能把某个平台的 union 类型双关扩展作为任意 C++17 程序的保证。[C++17：union 与活跃成员][union]

下面从 int 切到 string，再切回 int。Active 是本程序维护的状态记录，不是编译器自动替我们验证的护栏。

```cpp
// union_active_member.cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <new>
#include <string>
#include <variant>
union Payload
{
    int number;
    std::string text;
    Payload() : number(7) {}
    ~Payload() {}
};
enum class Active { empty, number, text };
int main()
{
    Payload payload;
    Active active = Active::number;
    assert(active == Active::number && payload.number == 7);
    active = Active::empty;
    auto* text = ::new (static_cast<void*>(std::addressof(payload.text))) std::string("hello");
    active = Active::text;
    assert(active == Active::text && *text == "hello");
    const auto text_size = text->size();
    std::destroy_at(text);
    active = Active::empty;
    text = nullptr;
    auto* number = ::new (static_cast<void*>(std::addressof(payload.number))) int(42);
    active = Active::number;
    assert(active == Active::number && *number == 42 && !text);
    std::cout << "union=number7 text" << text_size << " number" << *number << '\n';

    std::variant<int, std::string> value{7};
    value.emplace<std::string>("hello");
    assert(std::holds_alternative<std::string>(value) && std::get<std::string>(value).size() == 5);
    value.emplace<int>(42);
    assert(std::get<int>(value) == 42);
    std::cout << "variant=text5 number42\n";
}
```

输出：

```text
union=number7 text5 number42
variant=text5 number42
```

构造 string 会复用原先 number 的存储。number 是平凡整数，没有资源析构工作；string 活跃期间，只用返回的 text 访问它，记录长度后显式 destroy_at，再创建 int。最终 union 的空析构函数没有遗漏仍活跃的 string，因为这一条成功路径已经清理它。

这个 Payload 不是可直接投入业务的值类型：空析构不会替任意路径选择成员，拷贝/移动、赋值、自赋值和异常后的状态都还需要实现。例子没有在 string 存活区间加入可能抛出的业务调用；生产接口应封装这些责任，通常先考虑 variant 等已有工具。

variant 管理成员选择和销毁，但也不代表所有切换都不失败；类型改变的赋值或 emplace 抛出时可能出现 `valueless_by_exception()`，还要按接口检查。[C++17：variant 状态][variant]

C++17 还规定某些对 union 成员的内建赋值可以按条件隐式开始成员寿命；这不等于“任何非活跃成员都可以直接写”，更不能据此跳过 string 的构造/析构。本例统一用明确的原位构造切换，便于追踪。

## 5. 对齐解决位置，类型和寿命仍要成立

给缓冲加 alignas，只补了位置前提。把它 reinterpret_cast 成 T* 也只是得到某种指针转换结果，不会替本例的非平凡 Record 运行构造。字符/byte 访问对象表示与任意类型读写，是两种不同许可。[C++17：类型访问][access]、[对齐][alignment]

下面直接申请带对齐参数的存储，再构造 Record。两层 RAII 的声明顺序让对象先析构、空间后归还；它们的删除器也各自只完成一层。

```cpp
// aligned_storage.cpp
#include <cassert>
#include <cstdint>
#include <cstring>
#include <iostream>
#include <memory>
#include <new>
struct alignas(64) Record
{
    inline static int destroyed = 0;
    std::uint32_t value;
    explicit Record(std::uint32_t input) noexcept : value(input) {}
    ~Record() noexcept { ++destroyed; }
};
int blocks_freed = 0;
struct AlignedFree
{
    void operator()(void* p) const noexcept
    {
        ::operator delete(p, std::align_val_t{alignof(Record)});
        ++blocks_freed;
    }
};
struct DestroyOnly
{
    void operator()(Record* p) const noexcept { std::destroy_at(p); }
};
int main()
{
    {
        std::unique_ptr<void, AlignedFree> block{
            ::operator new(sizeof(Record), std::align_val_t{alignof(Record)})};
        assert(reinterpret_cast<std::uintptr_t>(block.get()) % alignof(Record) == 0);
        std::unique_ptr<Record, DestroyOnly> object{::new (block.get()) Record(123)};
        assert(object->value == 123 && Record::destroyed == 0 && blocks_freed == 0);
        std::cout << "aligned=" << alignof(Record) << " constructed=" << object->value << '\n';
        unsigned char bytes[sizeof(std::uint32_t)]{};
        std::uint32_t restored = 0;
        std::memcpy(bytes, &object->value, sizeof(bytes));
        std::memcpy(&restored, bytes, sizeof(restored));
        assert(restored == 123);
        std::cout << "representation=roundtrip" << restored << '\n';
    }
    assert(Record::destroyed == 1 && blocks_freed == 1);
    std::cout << "cleanup=object1 block1\n";
}
```

本机输出：

```text
aligned=64 constructed=123
representation=roundtrip123
cleanup=object1 block1
```

C++17 有带 `align_val_t` 的分配/释放形式；本例把同一个 `alignof(Record)` 用在直接申请和直接释放上。对齐 64 在本次工具链上受支持，不能由这个结果替所有平台规定扩展对齐支持范围。[C++17：单对象与对齐分配][allocation]

这里只把一个已经存在的 uint32_t 成员的合法表示，复制到字符数组，再复制到另一个已经存在的 uint32_t 对象。没有复制整个 Record，也没有把文件中的任意字节覆盖进 string、虚表或拥有者。其依据是相应平凡可复制类型的表示复制规则，不是“大小相同就能互换”。[C++17：对象表示复制][representation]

更详细的别名、void*、字节序与外部格式继续见 [CXX-06]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})。合法读取对象表示不能反向推成任意字节都是某类型的合法值；对齐、范围、活跃状态与类型访问也不能互相替代。

## 6. 释放匹配与未定义行为，不能靠运行一次判定

先按来源查清理方式：

| 来源与责任 | 对象清理 | 存储归还 |
|---|---|---|
| 普通 `new T` 的正常拥有者 | 对应 delete 表达式执行析构 | 按匹配规则查找释放函数 |
| 普通 `new T[n]` | 对应 `delete[]` 处理数组元素 | 使用数组释放协议，不能改用单对象 delete |
| `malloc` 获得空间并在其中原位构造本篇 T | 对象若成功创建，先结束相应寿命/清理资源 | 匹配 `free`；仍须满足大小/对齐等要求 |
| 直接调用 `operator new` 后原位构造 | 由对象层负责析构 | 匹配的 operator delete；带对齐形式对应处理 |
| Arena/外部缓冲提供空间 | 由项目协议或类型包装管理构造/析构 | 由 Arena/外部拥有者管理，不能默认逐个 delete |

malloc/free 不调用本例的 C++ 类构造/析构；它们与 operator new/delete 也不是能任意交叉配对的同一套接口。这里没有编写 malloc 路径的程序，更没有因为某实现底层可能使用 malloc 就推导用户可以交叉释放。[C++17：C 分配接口][malloc]、[delete 条件][delete-expr]

手动析构后再对同一对象执行普通 delete，会把对象清理重复一次；只调用 operator delete 而漏掉仍需清理的非平凡对象，又可能漏资源。默认删除器是否正确，应从分配与创建协议证明，不能从指针类型猜。

这些规则不是“编译器大多会容忍”的风格建议。未定义行为表示标准对该行为不施加相应要求；可能崩溃，也可能表现正常，优化后改变，或者没有任何诊断。它不同于“实现定义，查工具链文档即可”，也不同于“未指定，但仍在允许结果范围内”。[C++17：术语][definitions]、[程序执行][execution]

| 看似有说服力的现象 | 仍缺少什么 |
|---|---|
| 释放后还能打印原值 | 对象/存储有效性；旧字节碰巧未变不是寿命证明 |
| x64 上未对齐访问没崩 | C++ 对齐与访问规则，及目标平台条件 |
| union 两个成员读出相近数值 | 活跃成员与允许访问的依据 |
| memcpy 后 string 看起来能用 | 对象创建、资源拥有与合法复制契约 |
| 加 cast 后编译通过 | 转换以外的范围、寿命、类型和 const 前提 |

本篇不运行表中的非法访问。Sanitizer、调试分配器和断言可以发现它们覆盖的问题，但一次未报告也不是所有寿命/别名规则的完整证明；本次验证只使用编译诊断、合法程序断言与操作计数，没有声称做过 Sanitizer 检查。

## 7. 两个编译失败，把边界提前暴露

### 非平凡 union 不会自动替你写好状态机

```cpp
// union_default_error.cpp: expected compile failure
#include <string>
union Payload { int number; std::string text; };
int main() { Payload payload; }
```

本例的隐式默认构造与析构因非平凡 string 成员而被删除。只为 number 提供显式构造和空析构，可以让“仅使用 number”的修复变体通过编译；它仍不是 string 切换的完整实现。完整成功路径见程序三，不要把“能声明一个 union 局部变量”当成全路径安全。

### cast 不能把真正 const 的存储变成合法可写空间

```cpp
// const_storage_error.cpp: expected compile failure
#include <new>
int main()
{
    alignas(int) const unsigned char bytes[sizeof(int)]{};
    auto* value = ::new (static_cast<void*>(bytes)) int(7);
    return *value;
}
```

这里 static_cast 不能去掉 const，编译被拒绝。若确实需要这份可复用缓冲，应从声明上提供可变存储；本次“去掉 const”的变体通过编译检查。不要换成 const_cast 后在原来的 const 完整对象存储里重建对象，或者把编译器不再报错当作合法性的证明。

两例分别以 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only 文件名.cpp` 检查，预期非零退出；不执行失败产物。许多真正的寿命错误未必会得到这种提前诊断，不能把本节误读成“所有 UB 都会编译失败”。

## 8. 带回四条源码路线，公共课负责哪一层

| 路线 | 本篇提供的共同检查 | 项目还需要证明什么 |
|---|---|---|
| Piccolo | 谁构造对象，谁持有，显式清理与最终析构是什么关系 | 对象/组件容器与父子借用的实际协议；不能因看见 clear 就认定已结束全部对象寿命 |
| Unreal | 普通 C++ 构造、析构、原始存储和删除器怎样配对 | UObject/Actor 的创建、反射与 GC 约定，不能用本例 placement new 或通用 delete 机械替换 |
| HybridCLR | 参数槽/帧缓冲提供什么空间，访问类型与有效期从哪里来 | 托管引用、GC 可达性、线程与 AOT/解释路径，不能用普通 RAII 代替全部 runtime 契约 |
| IL2CPP | 生成表示和手写 runtime 中，哪一步提供存储，哪一步建立可用状态 | metadata、托管初始化与 GC/运行时规则；本例 Record 不是托管对象布局或分配实现 |

Piccolo 回到 [智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}})，沿实际创建与清理关系继续追；UE 回到 [UFS 综合练习]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})，保留普通对象与 UObject 的边界。

HybridCLR 可从 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) 与 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 继续；IL2CPP 回到 [运行时产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。这些是跟读任务，没有把原生缓冲中的 placement new 成功，记作托管对象创建或 GC 验证成功。

最后回看 Arena/Pool：重置偏移只是空间管理动作，不会自动遍历其中任意非平凡对象并调用析构；旧的资源、借用和业务身份仍要按协议处理。C# 对象池把已有对象重新交给业务，与 C++ 结束对象寿命再原位构造，是不同方案。优化是否有收益要测量，先把正确性责任说明白。

## 9. 怎样复做，以及本次验证边界

四个正常程序分别保存，保留断言、不定义 NDEBUG。以第一个为例，Windows PowerShell 命令如下，其余替换对应文件名：

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic placement_lifetime.cpp -o placement_lifetime.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\placement_lifetime.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

2026-10-25，已从本篇正常/失败程序对应的源文件核对内容，在 Windows x64、GCC 8.3.0（x86_64-posix-seh）下执行 C++17 检查。四个正常程序编译与运行退出码均为 0，所列选项下无警告，全部断言通过；输出分别与上文 4、4、2、3 行一致。alignof(Record)=64 是本机结果。

两个失败文件均以退出码 1 被拒绝，分别诊断 union 隐式默认构造/析构被删除，以及 const 数组不能 static_cast 为 void*；仅使用 number 的 union 显式构造/析构变体与可变缓冲变体通过编译检查。没有运行这些失败产物，没有执行错误释放或非法访问。

本次只验证这些合法路径与诊断：不包含 Sanitizer、异常析构、真实分配失败、所有 union 异常路径、复杂透明替换、跨平台布局/对齐、引擎运行、GC、并发与性能测量。C++17 实验也不替后续标准版本或各引擎规定统一对象模型。

按补写顺序，下一篇是 **CXX-11：数据交给另一个线程后，谁保证它有效且可见**，继续补寿命、互斥、原子与发布/消费的关系。完整路径见 [C++ 公共基础目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**空间由谁提供，对象由谁建立和结束，资源由谁清理，访问由谁保证。**

[life]: https://timsong-cpp.github.io/cppwp/n4659/basic.life
[cdtor]: https://timsong-cpp.github.io/cppwp/n4659/class.cdtor
[new-expr]: https://timsong-cpp.github.io/cppwp/n4659/expr.new
[delete-expr]: https://timsong-cpp.github.io/cppwp/n4659/expr.delete
[placement]: https://timsong-cpp.github.io/cppwp/n4659/new.delete.placement
[allocation]: https://timsong-cpp.github.io/cppwp/n4659/new.delete.single
[exceptions]: https://timsong-cpp.github.io/cppwp/n4659/except.ctor
[union]: https://timsong-cpp.github.io/cppwp/n4659/class.union
[access]: https://timsong-cpp.github.io/cppwp/n4659/basic.lval
[alignment]: https://timsong-cpp.github.io/cppwp/n4659/basic.align
[representation]: https://timsong-cpp.github.io/cppwp/n4659/basic.types
[launder]: https://timsong-cpp.github.io/cppwp/n4659/ptr.launder
[malloc]: https://timsong-cpp.github.io/cppwp/n4659/c.malloc
[variant]: https://timsong-cpp.github.io/cppwp/n4659/variant.status
[definitions]: https://timsong-cpp.github.io/cppwp/n4659/intro.defs
[execution]: https://timsong-cpp.github.io/cppwp/n4659/intro.execution
