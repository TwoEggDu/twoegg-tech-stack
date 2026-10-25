---
title: "C++ 公共基础 04｜复制、移动和容器修改以后，哪些访问还有效"
slug: "cpp-foundations-04-move-containers-invalidation"
date: "2026-10-25"
description: "从复制/移动构造与赋值走到 vector 扩容、擦除和 unordered_map 重排，用独立 C++17 程序区分元素地址、拥有者、目标对象与业务身份，并解释 noexcept 和失效边界。"
tags: ["C++", "源码阅读", "移动语义", "容器", "生命周期"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 4
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "复制、移动和容器修改以后，哪些访问还有效"
reading_summary: "先确认操作发生在哪个对象，再按容器规则判断访问有效性，分别追踪地址、拥有关系和业务身份。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 494
---

先从三个问题开始：vector 追加了一项，刚保存的元素引用还能用吗？删除第 0 项后，第 1 项还是原来的对象吗？把一个 unique_ptr 移进容器，原来借到的目标地址会不会跟着变化？

它们都涉及“东西变了位置”，但变化可能发生在不同层：容器重新组织元素，包装转移拥有关系，或者某个槽位中的值换成了另一个业务对象。只记住“用了 std::move”或“指针还非空”，无法判断后续访问。

这一篇的主线是：**先确定操作的是哪个对象，再分别追踪它的值、拥有关系、访问路径和业务身份。**

> **读这篇之前**：掌握 [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})、[CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) 和 [CXX-03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}})。本篇按补写顺序接在 CXX-06 后，编号仍按知识结构排列。
>
> **复用与范围**：复用 [UFS 综合练习]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 的 move/vector 模型，以及 [Piccolo 对象容器、借用与遍历]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}}) 对包装、目标和成员资格的区分。下面使用自有 C++17 程序，普通标准库规则与引擎协议分别记录，不执行失效访问。

## 1. 先确定“复制或移动了谁”

遇到一个 `vector<unique_ptr<Entity>>`，至少有三层对象：vector 容器、直接存放的 unique_ptr 元素、另外创建的 Entity 目标。元素重新安放与目标重新安放是两个问题。

再加上 Entity 的业务 ID，就有第四件事：即使一个位置上仍然有可访问的对象，它的值也可能已经换成另一个 ID。**地址、索引、业务身份不能互相代替。**

| 要区分的东西 | 典型问题 |
| --- | --- |
| 对象与它的值 | 原对象仍存在吗，还是已经销毁；成员值有没有被赋值改变 |
| 包装与目标 | 复制/移动的是拥有者，还是目标对象本身 |
| 访问路径 | 指针、引用或迭代器是否因容器修改而失效 |
| 业务身份 | 这个地址或下标现在对应哪个 ID，是否仍属于活动集合 |

CXX-02 已经解释了谁负责保活；这里还要补上容器和类型允许哪些操作。保活一个 Entity，并不会自动修复指向某个容器元素的迭代器。

## 2. 构造、赋值与值类别

先看普通写法的区别：`T b = a;` 正在初始化一个新对象，等号不代表调用赋值运算符；`b = a;` 则是在已有 b 上赋值。复制或移动又取决于源表达式和可用重载。

| 常见特殊成员形式 | 本篇中的用途 |
| --- | --- |
| `T(const T&)` | 从源对象复制构造一个新对象 |
| `T(T&&)` | 从适合该重载的源表达式移动构造一个新对象 |
| `T& operator=(const T&)` | 给已有对象复制赋值 |
| `T& operator=(T&&)` | 给已有对象移动赋值 |

这些是常见接口形式，不是唯一合法声明。隐式生成的操作通常逐个处理基类和成员；成员复制若只是复制裸指针，就不会因此取得资源所有权。自定义操作则要读它实际做了什么。[复制/移动构造](https://timsong-cpp.github.io/cppwp/n4659/class.copy.ctor)、[复制/移动赋值](https://timsong-cpp.github.io/cppwp/n4659/class.copy.assign)

**值类别属于表达式。** 普通命名变量的使用通常是左值；`std::move(x)` 提供一个指向同一对象的 xvalue 表达式，让重载选择可以考虑移动接口。它本身不搬字节、不调用移动构造，也不删除 const。一个命名的 `T&&` 变量，在用它的名字访问时仍是左值表达式。[move 的定义](https://timsong-cpp.github.io/cppwp/n4659/forward)

先把常见的三类表达式认出来：

| 表达式例子 | 值类别 | 这里怎样理解 |
| --- | --- | --- |
| 命名变量 x、有效指针的 `*p` | lvalue，左值 | 确定一个对象，可以继续通过它访问 |
| `T{...}` | prvalue，纯右值 | 提供用于初始化结果对象的值 |
| `std::move(x)` | xvalue，将亡值 | 仍指向原对象，允许相应的移动重载参与选择 |

prvalue 与 xvalue 都属于右值，但“右值”不意味着必然出现一次移动。C++17 的某些同类型 prvalue 初始化会直接构造结果对象；返回命名局部对象时也可能发生允许的复制消除。程序里的操作日志只解释列出的调用，不能当作所有返回值的固定次数。[值类别](https://timsong-cpp.github.io/cppwp/n4659/basic.lval)、[复制消除](https://timsong-cpp.github.io/cppwp/n4659/class.copy.elision)

赋值还要处理目标的原状态，例如原来持有的资源何时释放，由类型的赋值契约决定。不能把构造或赋值统一改成 memcpy；对象表示与资源语义的边界可回看 [CXX-06]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})。

### 程序一：先看实际选择了哪个操作

保存为 `copy_move_selection.cpp`：

```cpp
#include <cassert>
#include <iostream>
#include <string>
#include <utility>
#include <vector>

struct Value
{
    int value;
    explicit Value(int v) : value(v) {}
    Value(const Value& other) : value(other.value)
    {
        std::cout << "copy-ctor\n";
    }
    Value(Value&& other) : value(other.value)
    {
        other.value = -1;
        std::cout << "move-ctor\n";
    }
    Value& operator=(const Value& other)
    {
        value = other.value;
        std::cout << "copy-assign\n";
        return *this;
    }
    Value& operator=(Value&& other)
    {
        if (this != &other)
        {
            value = other.value;
            other.value = -1;
        }
        std::cout << "move-assign\n";
        return *this;
    }
};

struct Document
{
    std::string name;
    std::vector<int> values;
};

int main()
{
    Value a{10};
    Value b = a;
    Value c = std::move(a);
    Value assigned{0};
    assigned = b;
    assigned = std::move(c);
    const Value frozen{20};
    Value d = std::move(frozen);
    Value&& named = std::move(b);
    assert(b.value == 10);
    Value e = named;
    assert(a.value == -1 && c.value == -1);
    assert(b.value == 10 && assigned.value == 10 && d.value == 20 && e.value == 10);
    std::cout << "values=" << b.value << ',' << assigned.value << ','
              << d.value << ',' << e.value << '\n';

    Document source{"source", {1}};
    Document copy = source;
    copy.values[0] = 2;
    assert(source.values[0] == 1);
    Document moved = std::move(source);
    source = Document{"new", {3}};
    assert(moved.name == "source" && moved.values[0] == 1);
    assert(source.name == "new" && source.values[0] == 3);
    std::cout << "rule-zero=copy-independent, source-reassigned\n";
}
```

输出应为：

```text
copy-ctor
move-ctor
copy-assign
move-assign
copy-ctor
copy-ctor
values=10,10,20,10
rule-zero=copy-independent, source-reassigned
```

前四行对应两次构造和两次赋值。第五行来自 const 来源：这里的 `Value(Value&&)` 不能接收 const Value，`Value(const Value&)` 仍可用，所以复制。第六行来自 `named`：它的声明用了右值引用，但使用名字时仍按左值选择复制。

Value 的移动操作明确把来源成员设为 -1，因此本程序可以断言这个结果。它只是我们定义的类型契约；移动其他对象后，不能照此猜来源值。

### Rule of Zero：让成员承担自己的资源责任

Document 没有手写复制、移动或析构。string 和 vector 管理自己的资源，编译器生成的组合操作便可以服务于这个类型的值语义。这是 **Rule of Zero** 的设计方向：优先组合已经正确管理资源的成员，减少自己协调特殊成员的需要。[Core Guidelines C.20](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rc-zero)

它不承诺所有组合类型都能复制。例如成员换成 unique_ptr，复制通常就不可用；成员若是裸借用指针，默认复制也只会复制访问关系。要先决定类型应该表达什么，再选择默认、删除或自定义操作。

**移动后，来源通常仍然是一个对象。** 标准库对象通常处于有效但状态未指定的状态，具体类型可以有更强约定；自定义类型则要读其契约。有效不等于满足所有操作的前置条件：不能因为移动后的 vector 有效，就在没有检查 size 时访问第 0 项。[标准库 moved-from 状态](https://timsong-cpp.github.io/cppwp/n4659/lib.types.movedfrom)

Document 的来源在示例中被重新赋值，然后按新值使用。没有断言来源 string/vector 移动后必须为空。unique_ptr 的普通移动构造则有具体的来源置空保证，后面的包装实验会使用它。

## 3. noexcept 为什么会影响容器搬迁

vector 重新分配存储时，需要在新存储中构造已有元素。假如已经移动了一部分来源元素，下一次移动又抛异常，恢复原状可能很困难；复制若保留来源，就提供了另一种实现选择。

因此，要把两件事分开：标准规定的操作与异常保证，以及标准库实现如何满足它们。不能把某个实现的“复制还是移动”次数写成所有 vector 的固定算法。

`std::move_if_noexcept` 可以显式观察一种选择规则：当移动构造可能抛异常、而复制构造可用时，返回 const T&；否则返回 T&&。它自己仍然只提供表达式，实际构造随后发生。

### 程序二：保证的选择规则与本机观察

保存为独立程序 `noexcept_relocation.cpp`：

```cpp
#include <cassert>
#include <iostream>
#include <type_traits>
#include <utility>
#include <vector>

struct Risky
{
    inline static int copies = 0;
    inline static int moves = 0;
    int value = 7;
    Risky() = default;
    Risky(const Risky& other) : value(other.value) { ++copies; }
    Risky(Risky&& other) noexcept(false) : value(other.value) { ++moves; }
};

struct Safe
{
    inline static int copies = 0;
    inline static int moves = 0;
    int value = 7;
    Safe() = default;
    Safe(const Safe& other) : value(other.value) { ++copies; }
    Safe(Safe&& other) noexcept : value(other.value) { ++moves; }
};

int main()
{
    Risky risky;
    Safe safe;
    static_assert(std::is_same_v<decltype(std::move_if_noexcept(risky)), const Risky&>);
    static_assert(std::is_same_v<decltype(std::move_if_noexcept(safe)), Safe&&>);
    Risky copied(std::move_if_noexcept(risky));
    Safe moved(std::move_if_noexcept(safe));
    assert(copied.value == 7 && moved.value == 7);
    assert(Risky::copies == 1 && Risky::moves == 0);
    assert(Safe::copies == 0 && Safe::moves == 1);
    std::cout << "fallback copy=" << Risky::copies << " move=" << Risky::moves << '\n';
    std::cout << "nothrow copy=" << Safe::copies << " move=" << Safe::moves << '\n';

    Risky::copies = Risky::moves = 0;
    Safe::copies = Safe::moves = 0;
    std::vector<Risky> risky_values;
    risky_values.reserve(1);
    risky_values.emplace_back();
    risky_values.reserve(risky_values.capacity() + 1);
    std::vector<Safe> safe_values;
    safe_values.reserve(1);
    safe_values.emplace_back();
    safe_values.reserve(safe_values.capacity() + 1);
    assert(risky_values.size() == 1 && risky_values[0].value == 7);
    assert(safe_values.size() == 1 && safe_values[0].value == 7);
    std::cout << "vector-risky copy=" << Risky::copies << " move=" << Risky::moves << '\n';
    std::cout << "vector-safe copy=" << Safe::copies << " move=" << Safe::moves << '\n';
}
```

前两行及相关断言验证显式 move_if_noexcept 的规则；最后两行记录当前标准库 vector 的实际选择，没有把复制/移动次数写进这些 vector 断言。它们的本机输出记录于验证段。

本例的 Risky 实际没有抛异常，只是接口声明允许抛。`noexcept` 则是异常承诺，不是让编译器替你捕获错误；违背非抛出承诺可能走向 terminate。不要为了获得移动路径而随意标记 noexcept。

对于不能复制而移动又可能抛异常的类型，vector 无法凭空选择复制；相应异常路径可能不具有同样的“操作无影响”保证。要结合具体操作和类型条件读规范。[vector 容量与异常边界](https://timsong-cpp.github.io/cppwp/n4659/vector.capacity)

这个实验没有触发抛出、分配失败或回滚，也没有测量性能。emplace_back 只改变新元素的构造方式，不能免除已有元素因重新分配而搬迁。

## 4. 两个看似已经用了 move 的编译失败

分别保存，只做编译检查。

`const_unique_move_error.cpp`：

```cpp
#include <memory>
#include <utility>
int main()
{
    const auto source = std::make_unique<int>(7);
    auto target = std::move(source); // const 来源不能绑定这里需要的移动接口。
}
```

unique_ptr 不能复制，这次也没有适用的移动构造，因此应被拒绝。std::move 没有消除来源的 const。

`suppressed_move_error.cpp`：

```cpp
#include <memory>
#include <utility>
struct Owner
{
    std::unique_ptr<int> value = std::make_unique<int>(7);
    ~Owner() = default;
};
int main()
{
    Owner source;
    Owner target = std::move(source);
}
```

用户声明析构函数会影响隐式移动的生成，即使析构写的是 `= default`。这里没有隐式移动，隐式复制又因 unique_ptr 成员不可复制而被删除，所以也应失败。删除不必要的析构声明，可以回到 Rule of Zero；若类型确实需要自定义特殊成员，则要整体审查所需的复制、移动、构造和清理语义。

还要避免另一个推断：`is_move_constructible_v<T>` 为真，只说明能从相应右值实参构造 T，不等于已经找到一个独立的 T(T&&)。若 const T& 的复制构造能接受这个实参，也可能满足该检测。读源码时仍需确认真正选中的操作。

## 5. vector 修改以后，重新判断每条访问

vector 为元素提供连续存储。重新分配时，元素可能在新的存储中被重新构造，旧元素的指针、引用和迭代器失效。不能通过比较旧地址和新地址、或试着读一次，来检验旧访问是否还能使用。

`reserve(n)` 调整容量准备，不改变 size，也不创建 n 个元素；`resize(n)` 改变元素数量，增大时需要相应构造元素。本篇使用小容器上的 `reserve(capacity() + 1)` 明确请求超过旧容量，强制重新分配；实际工程还要考虑长度、分配和异常。

### 程序三：元素位置、索引身份和独占目标

保存为第三个独立程序 `vector_access.cpp`：

```cpp
#include <algorithm>
#include <cassert>
#include <iostream>
#include <memory>
#include <utility>
#include <vector>

struct Entry { int id; };
struct Entity
{
    int id;
    explicit Entity(int value) : id(value) {}
    Entity(const Entity&) = delete;
    Entity(Entity&&) = delete;
};

int main()
{
    std::vector<Entry> rows{{10}, {20}, {30}};
    Entry* borrowed = &rows.front();
    assert(borrowed->id == 10);
    borrowed = nullptr; // 先放弃这条访问，再修改存储。
    const auto old_capacity = rows.capacity();
    rows.reserve(old_capacity + 1);
    borrowed = &rows.front();
    assert(rows.capacity() > old_capacity && borrowed->id == 10);
    std::cout << "reallocation=reacquired\n";

    borrowed = nullptr;
    rows.reserve(rows.size() + 2);
    borrowed = &rows.front();
    rows.push_back({40}); // 已预留足够容量，本次不会重新分配。
    assert(borrowed == &rows.front() && borrowed->id == 10);
    std::cout << "append=first-still-valid\n";

    const std::size_t saved_index = 1;
    const int saved_id = rows[saved_index].id;
    borrowed = nullptr;
    auto next = rows.erase(rows.begin());
    assert(next == rows.begin() && next->id == 20);
    assert(rows[saved_index].id == 30);
    auto same_id = std::find_if(rows.begin(), rows.end(),
                               [saved_id](const Entry& e) { return e.id == saved_id; });
    assert(same_id != rows.end() && same_id - rows.begin() == 0);
    std::cout << "identity=" << saved_id << " index=" << same_id - rows.begin()
              << " old-index=" << rows[saved_index].id << '\n';

    for (auto it = rows.begin(); it != rows.end(); )
    {
        if (it->id % 20 == 0) it = rows.erase(it);
        else ++it;
    }
    assert(rows.size() == 1 && rows[0].id == 30);
    std::cout << "erase-loop=" << rows[0].id << '\n';

    std::vector<std::unique_ptr<Entity>> owners;
    owners.push_back(std::make_unique<Entity>(90));
    Entity* target = owners[0].get();
    owners.reserve(owners.capacity() + 1);
    assert(target == owners[0].get() && target->id == 90);
    auto keeper = std::move(owners[0]);
    assert(!owners[0] && target == keeper.get());
    owners.clear();
    assert(target->id == 90);
    std::cout << "unique-owner=target-still-90\n";
    keeper.reset();
    target = nullptr; // 目标已经析构，不再保留可用借用的假象。
}
```

输出应为：

```text
reallocation=reacquired
append=first-still-valid
identity=20 index=0 old-index=30
erase-loop=30
unique-owner=target-still-90
```

第一段在修改前放弃旧指针，修改后从当前容器重新取得访问。重新分配是否发生由容量请求和规范判断，不靠读取旧对象验证。

第二段保证容量足够，然后在尾部追加。原来的首元素指针仍有效，但**旧 end 不能继续当作当前 end 使用**。缓存了 end 的遍历即使没碰到重新分配，也需要重新审查。

第三段没有重新分配，却仍然改变了身份对应：删除前索引 1 的 ID 是 20，删除后索引 1 是 30。这里的 ID 不复用且唯一，所以可以重新查到 20；真实系统若复用 ID，还需要版本、代数等规则，不能把任意整数称为永不失效的句柄。

擦除点及之后的旧访问不能继续沿用；程序接收 erase 返回的有效迭代器，并在过滤循环中按该规则推进。若擦除后已到末尾，返回的就是 end，必须先检查再解引用。范围遍历中直接删当前项，不能自动获得这种推进方式。

最后一段改变的是独占包装的存放位置和拥有关系。Entity 甚至禁止复制和移动，但 vector 的 unique_ptr 元素仍可重新安放，目标 Entity 地址不因此改变。把包装移到 keeper 后，原包装为空；清空原 vector 也不会删掉 keeper 仍拥有的目标。最后 reset 才结束目标寿命。[unique_ptr 移动构造](https://timsong-cpp.github.io/cppwp/n4659/unique.ptr.single.ctor)

这里没有保存 unique_ptr 元素自身的地址；如果保存的是 `&owners[0]`，重新分配会使那条访问失效，不能套用 Entity 目标地址保持有效的结论。

### 常见失效规则要对应具体操作

下表针对普通 vector 元素，不包含 vector<bool> 的代理接口。规则前提是相应操作满足要求并成功完成：

| 操作 | 原有访问怎样处理 |
| --- | --- |
| reserve 超过旧容量，发生重新分配 | 所有元素指针、引用、迭代器和旧 end 都不能继续使用 |
| 追加且没有重新分配 | 原有元素访问保持有效，旧 end 失效 |
| 中间插入且没有重新分配 | 插入点之前的访问保持有效；插入点及之后的访问需重新取得 |
| erase | 擦除点及之后的访问失效，使用返回结果继续推进 |
| clear | 所有元素都被销毁，容量即使还在，也不代表元素仍存在 |
| shrink_to_fit | 这是非强制请求；若发生重新分配，同样会使原访问失效 |

容量和重新分配依据见 [vector.capacity](https://timsong-cpp.github.io/cppwp/n4659/vector.capacity)，插入与擦除依据见 [vector.modifiers](https://timsong-cpp.github.io/cppwp/n4659/vector.modifiers)。预留容量只约束相应的重新分配，不保护中间插入、删除或业务重排；不能把 reserve 当作引用永久稳定的开关。

本篇的断言检查的是仍然有效的路径。被放弃或已失效的旧指针、迭代器没有被解引用或拿来与新位置比较。

## 6. 换成 unordered_map，规则也会改变

不能把 vector 的规则直接套到所有容器。unordered_map 发生 rehash 时，迭代器失效，但对元素的指针和引用保持有效；擦除则使被擦除元素的访问失效，其他元素的相应访问不因此全部失效。[无序容器要求](https://timsong-cpp.github.io/cppwp/n4659/unord.req)

若映射值又是 shared_ptr，还要继续区分这个映射元素与它指向的目标。rehash、erase 和目标析构是三种操作。

### 程序四：元素还在、成员移除与目标保活

保存为独立程序 `map_membership.cpp`：

```cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <unordered_map>

struct Node
{
    int id;
    explicit Node(int value) : id(value) {}
};

int main()
{
    std::unordered_map<int, std::shared_ptr<Node>> table;
    table.emplace(1, std::make_shared<Node>(1));
    std::shared_ptr<Node>* wrapper = nullptr;
    std::shared_ptr<Node> keeper;
    {
        auto found = table.find(1);
        assert(found != table.end());
        wrapper = &found->second;
        keeper = found->second;
    } // 此迭代器在 rehash 前已结束使用。

    const auto old_buckets = table.bucket_count();
    table.rehash(old_buckets * 2 + 1);
    assert(table.bucket_count() > old_buckets);
    assert(wrapper == &table.at(1) && wrapper->get() == keeper.get());
    std::cout << "rehash=wrapper-and-target-valid\n";

    std::weak_ptr<Node> observed = keeper;
    const auto erased = table.erase(1);
    wrapper = nullptr;
    assert(erased == 1 && table.find(1) == table.end());
    assert(!observed.expired() && keeper->id == 1);
    std::cout << "erase=removed-but-kept-alive\n";
    keeper.reset();
    assert(observed.expired());
    std::cout << "last-owner=destroyed\n";
}
```

预期输出为：

```text
rehash=wrapper-and-target-valid
erase=removed-but-kept-alive
last-owner=destroyed
```

第一次修改后，wrapper 仍能访问原映射值，但没有沿用旧迭代器。第二次修改后，映射元素已经被擦除，不能再使用 wrapper；局部 keeper 是另一份强拥有者，所以目标 Node 仍然存活。最后一个强拥有者释放，目标才析构。

这也解释了源码里“从对象表删除后还读 ID”的一种合法条件：读的必须是仍被保活的目标，而不是已经擦除的容器元素。即便目标仍在，世界成员资格、活动状态和其他依赖也可能已改变，继续调用业务接口仍需相应协议。

交换两个容器、移动整个容器或使用带分配器参数的构造/赋值，还各有条件。不能从本例推出“任何容器移动都保证所有借用稳定”，也不能把节点容器和连续容器的行为混成一句话。[容器通用要求](https://timsong-cpp.github.io/cppwp/n4659/container.requirements.general)

## 7. 把四层关系带回源码

读容器修改时，可以依次记录：**容器操作 → 元素类型 → 拥有/借用关系 → 修改后的合法访问 → 业务身份与资格**。

| 路线 | 接下来核对什么 |
| --- | --- |
| Piccolo | 在 [对象容器、借用与遍历]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}}) 继续追对象表、局部 shared_ptr 和 ReflectionPtr 组件包装；保留项目中谁删除组件、谁处理渲染移除的具体协议。 |
| Unreal | 在 [UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}}) 核对 TArray/TMap 的查询、修改和访问有效性，以及 MoveTemp 的额外限制；引擎对象寿命继续按 UE 专项判断。标准库失效表不能直接替代引擎容器契约；UE 实验状态见专项正文。 |
| HybridCLR | 从 [源码起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 追参数槽、解释帧、metadata 索引与原生保存位置；C++ 容器元素变化与 runtime 对托管对象、metadata 的寿命规则分别判断。 |
| IL2CPP | 从 [运行时产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) 区分生成代码中的原生值、托管对象、句柄和注册表；不能把 std::move 当成 GC 搬迁或托管引用更新。 |

需要更稳定的访问时，常见选择各有代价：索引需要处理删除和重排，ID/句柄需要查找和复用检测，间接拥有需要追目标寿命与分配，节点存储需要考虑访问局部性。选择哪种方式取决于操作协议，而不是仅看当前地址是否碰巧没变。

回调中保存的 this、引用或容器元素地址，也要在上述修改后重新判断，继续回看 [CXX-05]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})。跨线程修改还需要同步，局部强持有不保证遍历和字段访问线程安全；并发基础见 [CXX-11：数据交给另一个线程后，谁保证它有效且可见]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}})。

## 8. 自测与验证边界

先预测四个程序的输出，再回答：

1. 初始化时写 `=`，为什么可能调用构造而非赋值？
2. 为什么 std::move(const Value) 在本例复制，而 std::move(const unique_ptr) 会编译失败？
3. 为什么命名的 T&& 没有自动让下一次构造继续移动？
4. noexcept、能否复制和 vector 的异常保证怎样影响搬迁选择？
5. 为什么容量足够仍不能保证所有迭代器、end 和索引身份稳定？
6. vector 中 unique_ptr 元素的地址与目标 Entity 地址，分别受什么影响？
7. unordered_map 的 rehash、erase 与最后一个拥有者释放，分别改变哪一层？

正常程序分别保存、独立编译。例如在 PowerShell 中：

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic copy_move_selection.cpp -o copy_move_selection.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\copy_move_selection.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

其余正常程序替换文件名。保留断言，不定义 NDEBUG；两个失败文件分别用 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only 文件名.cpp` 检查，不生成或运行失败产物。

资料核对日为 2026-10-25，语言与标准库依据为 C++17 工作草案 N4659。Rule of Zero 属于设计建议，容器失效规则属于相应接口契约，本机的复制/移动计数属于实现观察。

2026-10-25 本地环境为 Windows x64、GCC 8.3.0（`x86_64-posix-seh`）。从正文提取四个正常程序，分别以所列 C++17 参数编译，所列警告选项下无警告；编译、运行退出码均为 0，全部断言通过。

| 用例 | 本次结果 | 验证内容 |
| --- | --- | --- |
| copy_move_selection.cpp | 8 行输出与正文一致 | 四种操作、const 来源复制、命名 T&& 复制、成员管理资源的组合语义 |
| noexcept_relocation.cpp | 显式选择分别为 copy=1/move=0、copy=0/move=1；vector 本机观察分别也为 1/0、0/1 | 前两组断言验证接口规则；vector 计数只作实现记录，未测试异常回滚 |
| vector_access.cpp | 5 行输出与正文一致 | 重新取得访问、追加稳定性、ID 重查、erase 推进、不可移动目标与独占包装 |
| map_membership.cpp | 3 行输出与正文一致 | rehash 后元素引用、擦除后的局部保活、最后拥有者释放 |
| const_unique_move_error.cpp | 编译退出码为 1 | 诊断指出尝试使用 unique_ptr 已删除的复制构造 |
| suppressed_move_error.cpp | 编译退出码为 1 | 诊断指出 Owner 隐式复制被删除，并追到 unique_ptr 成员 |

本篇不包含四条路线的实际引擎构建/断点、异常回滚、分配失败、性能测量、并发修改或 GC 行为验证。没有读取失效指针或使用失效迭代器，也没有以来源对象“看起来为空”推导一般移动保证。

按补写顺序，下一篇进入 [CXX-07：模板调用怎样选择实现]({{< relref "engine-toolchain/cpp-foundations-07-template-reading.md" >}})，继续讲推导、特化与常见类型工具。项目应用可回看 [Piccolo 模板、宏与反射材料]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})，完整顺序见 [C++ 公共基础目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**操作之后，重新确认访问是否有效，再确认它是否仍然代表你要找的对象。**
