---
title: "C++ 公共基础 07｜模板调用怎样选择实现：推导、特化与常见类型工具"
slug: "cpp-foundations-07-template-reading"
date: "2026-10-25"
description: "用独立 C++17 程序和一个多文件工程，沿类型推导、重载与特化、替换失败、类型工具和完美转发，读懂模板调用怎样选择实现，并区分编译期选择与运行时泛型。"
tags: ["C++", "源码阅读", "模板", "类型推导", "完美转发"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 7
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "模板调用怎样选择实现：推导、特化与常见类型工具"
reading_summary: "从调用处推回类型实参、候选与特化，再沿定义、实例化和转发检查实际选择与寿命边界。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 497
---

读源码时，跳到一个同名函数还不能确定调用会执行它：旁边可能有普通重载、另一个函数模板，甚至还有 `template<>`。换一个实参，原本能编译的调用也可能报出几十行错误，而最初出问题的地方只是某个类型没有 `size()`。

更容易混淆的是“泛型”：Piccolo 的组件查询、UE 的模板工具、HybridCLR 的原生辅助函数和 IL2CPP 处理的托管泛型，都可能出现类型参数；决定实现的阶段与依据却不相同。

本篇要建立的阅读顺序是：**先确定类型实参和候选，再判断选择规则，最后核对所需定义与实例化；运行时选择另追它自己的入口。**

> **读这篇之前**：先掌握 [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})、[CXX-03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}}) 和 [CXX-04 移动与容器]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}})。最后的三文件练习衔接 [CXX-10 编译、链接与符号]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}})。
>
> **复用与范围**：复用 [Piccolo 模板、宏与反射篇]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}}) 对类型实参、字符串与生成阶段的区分，以及 [UFS 综合练习]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 的模板和头文件起点。下面全部是自有 C++17 示例；宏/外部生成留给 CXX-08，完整模板元编程与 C++20 Concepts 不在本课范围。

## 1. 把“找到实现”拆成几步

对一次普通函数模板调用，可以先用这张表做阅读记录。它是定位问题的顺序，省略了完整名字查找、ADL 与重载规则的细节，不能当成编译器的全部算法。

| 步骤 | 要记录什么 | 常见误判 |
|---|---|---|
| 找声明 | 当前可见的普通函数、函数模板、命名空间与成员 | IDE 跳到一个声明，就认定只剩一个候选 |
| 确定模板实参 | 显式 `<...>`、函数实参推导、默认实参 | 从返回值的接收变量猜 `T` |
| 检查候选 | 替换是否成功、参数能否绑定、转换与排序 | 认为函数体里任意错误都能触发回退 |
| 选中实现 | 普通重载/函数模板；类模板另查匹配的特化 | 把函数显式特化当成独立重载 |
| 检查定义与使用 | 本次要求实例化哪些定义，谁提供链接所需实现 | 以为有声明或有 `extern template` 就有机器码 |

还要区分两种时间：编译器按静态类型选中了某个函数，该函数**内部**仍然可以根据运行时数据分支、查表或进行虚调用。模板本身没有取消这些运行时行为。

## 2. 类型实参从哪里来

函数模板里的 `T` 既可以显式指定，也可以从函数实参推导。例如 `Twice<int>(7)` 已经指定 `T=int`；`Twice(7)` 则尝试把实参类型与形参模式匹配。普通调用不会因为左边写了 `double result`，就从接收变量反推返回类型里的 `T`。

推导也不是“试遍所有隐式转换，找到一种能运行的类型”。对于 `template<class T> T combine(T, T)`，`combine(1, 2.0)` 会从两个位置分别得到 int 和 double，发生冲突；`combine<double>(1, 2.0)` 已明确 `T`，之后再检查 int 到 double 的参数转换。这两步应分开读。[C++17：调用推导][deduct-call]

形参模式还决定哪些信息留下来：

| 形参模式 | 传入 `const int n` | 传入 `int values[3]` |
|---|---|---|
| `T value` | `T=int`，顶层 const 不进入 T | `T=int*`，数组发生调整 |
| `T& value` | `T=const int` | `T=int[3]`，保留数组类型 |
| `const T& value` | `T=int`，const 由形参提供 | 可以保留数组信息；不是复制数组 |

`T` 是类型参数不代表尖括号只能放类型。`std::array<int, 3>` 的第二个实参是非类型参数；`template<class T, std::size_t N>` 同时描述类型与编译期数量。`using Ints = std::array<int, 3>` 只是别名，没有另造一种运行时容器。普通函数实参的这个推导表，也不能直接套到列表初始化或所有 `auto` 情形。

### 程序一：重载与特化分别选什么

下面三个 `Kind` 声明先给出主模板，再给出指针偏特化，最后给出 `int*` 全特化。偏特化匹配的是一组类型模式；全特化指定完整实参。在本例中 `Kind<int*>` 用全特化，`Kind<double*>` 用偏特化，`Kind<int>` 用主模板。[C++17：类模板特化匹配][partial]

函数要分开看：显式特化附属于某个主函数模板，不作为一个独立重载候选。先在普通函数与主函数模板间选择，再考虑所选模板对应的显式特化。[C++17：重载选择][overload]、[显式特化][specialization]

```cpp
// template_selection.cpp
#include <array>
#include <cassert>
#include <iostream>
#include <type_traits>

template<class T> struct Tag {};
template<class T> Tag<T> by_value(T);
template<class T> Tag<T> by_reference(T&);

int choose(int) { return 1; }
template<class T> int choose(T) { return 2; }

template<class T> int pointer_case(T) { return 10; }
template<> int pointer_case<int*>(int*) { return 20; }
template<class T> int pointer_case(T*) { return 30; }

template<class T> struct Kind { static constexpr int code = 0; };
template<class T> struct Kind<T*> { static constexpr int code = 1; };
template<> struct Kind<int*> { static constexpr int code = 2; };

template<class T, std::size_t N> using Fixed = std::array<T, N>;

int main()
{
    const int n = 7;
    int values[3] = {1, 2, 3};
    static_assert(std::is_same_v<decltype(by_value(n)), Tag<int>>);
    static_assert(std::is_same_v<decltype(by_reference(n)), Tag<const int>>);
    static_assert(std::is_same_v<decltype(by_value(values)), Tag<int*>>);
    static_assert(std::is_same_v<decltype(by_reference(values)), Tag<int[3]>>);
    static_assert(std::is_same_v<Fixed<int, 3>, std::array<int, 3>>);
    std::cout << "deduction=const-and-array-checked\n";

    assert(choose(7) == 1);
    assert(choose('a') == 2);
    assert(choose<>(7) == 2);
    std::cout << "overload=1,2,2\n";

    int* p = &values[0];
    assert(pointer_case(p) == 30);
    assert(pointer_case<int*>(p) == 20);
    std::cout << "function-specialization=30,20\n";

    static_assert(Kind<int>::code == 0);
    static_assert(Kind<double*>::code == 1);
    static_assert(Kind<int*>::code == 2);
    std::cout << "class-specialization=0,1,2\n";
}
```

`choose(7)` 的两个候选转换同样好，普通函数在这里胜出；`choose('a')` 中模板可精确匹配 char，普通函数需要提升到 int，因此模板胜出。**“普通函数总比模板优先”不成立。** `choose<>(7)` 使用模板实参列表，让这次选择落到函数模板。

`pointer_case(p)` 则先选中形参为 `T*` 的更专门模板，不会因为另外有一个 `int*` 显式特化就跳过去。`pointer_case<int*>(p)` 显式给出 `T=int*` 后，`T*` 那个候选要求 int**，本次不可行；于是选中通用模板对应的 `int*` 特化。声明与特化的归属必须读清，不能只按函数参数的表面拼写判断。

函数模板不能像类模板这样写偏特化；需要按参数形状区分时，常用函数重载，或把选择转给类模板/类型工具。特化的可见位置也有约束，应在会使用它的首次实例化之前让相应声明可见，不能在文件末尾随意“补一个覆盖”。

## 3. 依赖名和类型工具，在帮助编译器判断什么

看到 `typename C::value_type`，先把它读成“依赖 C 的嵌套类型”。C 未确定时，编译器不能普遍把 `C::value_type` 当类型；在下面这个 C++17 别名声明中需要 `typename`。模板形参列表中的 `typename T` 与 `class T` 均可声明类型参数，它们与这里的消歧职责不同。[C++17：依赖名][dependent]

另一个常见形式是 `reader.template read<int>()`：reader 的类型依赖模板参数，`template` 告诉解析器后面的名字要按模板处理。这两个关键字不生成对象，也不做运行时类型检查。[C++17：模板名字][template-names]

常见工具可以按“产生类型”还是“回答问题”来读：

| 写法 | 在这里回答什么 |
|---|---|
| `decltype(expr)` | 表达式按对应规则得到什么类型，保留引用/值类别信息 |
| `std::remove_reference_t<T>`、`std::remove_cv_t<T>` | 去掉引用、顶层 cv 后的类型是什么 |
| `std::decay_t<T>` | 按该变换去引用/cv，并做数组或函数到指针的变换 |
| `std::is_same_v<A, B>`、`std::is_integral_v<T>` | 给出编译期 bool 性质；`_v` 对应值，`_t` 通常对应嵌套 type 的别名 |
| `std::void_t<...>` | 实参类型都能形成时得到 void，可用于检测 |
| `std::enable_if_t<condition, R>` | 条件为真时得到 R，为假时没有相应 type |

不能把 `decay_t` 简化为“去 const”：它还会改变引用和数组/函数形状。本篇用两个变换组合去除 cv/ref，避免使用 C++20 才提供的 `remove_cvref_t`。[C++17：类型变换][transforms]

`decltype(n)` 对未加括号的变量名有特殊规则，得到其声明类型；`decltype((n))` 中 `(n)` 是左值表达式，得到左值引用类型。对一般表达式，左值对应 `T&`、亡值对应 `T&&`、纯右值对应 T；不能把所有情形都按“变量是什么类型”处理。[C++17：decltype][decltype]

### 程序二：检测失败，还是函数体真的出错

```cpp
// template_tools.cpp
#include <cassert>
#include <cstddef>
#include <iostream>
#include <type_traits>
#include <utility>
#include <vector>

template<class C> using Element = typename C::value_type;
template<class T> using Bare = std::remove_cv_t<std::remove_reference_t<T>>;

template<class C, class = void> struct HasSize : std::false_type {};
template<class C>
struct HasSize<C, std::void_t<decltype(std::declval<const C&>().size())>>
    : std::true_type {};

template<class C>
auto extent(const C& c) -> decltype(c.size()) { return c.size(); }
std::size_t extent(...) { return 0; }

template<class C>
std::size_t count(const C& c)
{
    if constexpr (HasSize<C>::value)
        return c.size();
    else
        return 1;
}

template<class T, std::enable_if_t<std::is_integral_v<T>, int> = 0>
bool integral(T) { return true; }
bool integral(...) { return false; }

struct Reader
{
    template<class T> T read() const { return T{7}; }
};
template<class R> int read_number(const R& r) { return r.template read<int>(); }
struct NoSize {};
constexpr int next(int n) { return n + 1; }

int main(int argc, char**)
{
    std::vector<int> data{1, 2, 3};
    NoSize scalar;
    static_assert(std::is_same_v<Element<decltype(data)>, int>);
    static_assert(std::is_same_v<Bare<const int&>, int>);
    static_assert(std::is_same_v<decltype(argc), int>);
    static_assert(std::is_same_v<decltype((argc)), int&>);
    static_assert(HasSize<decltype(data)>::value);
    static_assert(!HasSize<NoSize>::value);
    assert(extent(data) == 3 && extent(scalar) == 0);
    assert(count(data) == 3 && count(scalar) == 1);
    std::cout << "size=3 fallback=0 scalar=1\n";
    assert(integral(7) && !integral(2.5));
    assert(read_number(Reader{}) == 7);
    std::cout << "enable-if=integral reader=7\n";
    static_assert(next(4) == 5);
    assert(next(argc) == argc + 1);
    std::cout << "constexpr=constant-and-call-checked\n";
}
```

这里有三种不同的选择：

1. `HasSize` 用类模板偏特化检测表达式。`declval<const C&>()` 在不求值的上下文里提供表达式类型，不需要构造 C；它不是可以拿到真实对象的工厂，不能拿到运行时调用。
2. `extent` 把 `c.size()` 放进依赖的返回类型。替换 NoSize 时无法形成这个类型，该候选被排除，才由省略号回退接住。本例 NoSize 是空的平凡类，不把省略号回退作为任意资源对象的通用接口。
3. `count` 始终是可选择的模板，进函数体后才通过 `if constexpr` 选择适合 C 的分支。它不是把 count 从候选中删除。

SFINAE 是“替换失败不是错误”的缩写，关键是**错误出现在哪里**。函数模板类型、模板参数声明等规定的直接上下文内的替换失败可以导致推导失败；实例化别的模板等副作用产生的错误，以及选中后函数体中的错误，并不因此自动变成回退。[C++17：替换失败边界][deduct]

同理，`enable_if_t` 在本例的模板参数位置为 double 形成失败，模板候选被排除；`static_assert` 则通常是在已选择的实例化中拒绝类型，不能用它冒充候选筛选。模板定义里的非依赖语法与名字也不是等 T 确定后才统一检查。

`if constexpr` 的条件是编译期条件；在模板实例化中，条件确定后，丢弃分支不实例化。因此 `count<NoSize>` 不要求 `NoSize::size()` 存在。普通 `if` 即使条件值为 false，两个分支仍须满足相应编译要求。丢弃分支也并非预处理删除：语法错误、非依赖错误，以及 C++17 中对所有特化都无效的构造，不能靠它隐藏。[C++17：constexpr if][constexpr-if]

最后，`constexpr` 函数允许满足条件的调用参与常量求值，不承诺每一次调用都在编译期完成。程序同时检查 `next(4)` 的常量表达式与接受 argc 的调用；仅凭输出相同，不能判定优化后的机器码采用了什么求值时机。

## 4. 转发要保留什么，不能保证什么

[CXX-04]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}}) 已说明，`std::move` 本身是转换，是否移动取决于后续操作。本篇继续回答：一个包装函数收到参数后，再调用下一层，怎样保留调用者提供的值类别？

对下面 `relay(T&&)` 中参与函数实参推导、且无 cv 限定的类型参数 T，`T&&` 是转发引用。传入 int 左值时 T 推为 int&，传入 const int 左值时 T 推为 const int&，传入 int 右值时 T 推为 int。引用折叠可用四项速查：`& + &`、`& + &&`、`&& + &` 都形成 `&`；只有 `&& + &&` 形成 `&&`。[C++17：调用推导][deduct-call]、[引用折叠][references]

但**名字 `value` 这个表达式始终是左值**，即使变量的声明类型是 int&&。`std::forward<T>(value)` 根据 T 还原应传递的类别；对推导的 T=int& 返回 int&，对 T=int 返回 int&&。[C++17：forward][forward]

不是每个 T&& 都是转发引用。`const T&&` 不符合上述条件；`Box<T>` 的非模板成员 `put(T&&)` 中，T 已由类模板实参确定，不会在这次成员调用中重新推导。显式固定 T 也会改变可绑定的实参，不能只见 `&&` 就套用“万能”。

### 程序三：经过一层包装，还能选中原来的重载吗

```cpp
// template_forwarding.cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <type_traits>
#include <utility>

int sink(int&) { return 1; }
int sink(const int&) { return 2; }
int sink(int&&) { return 3; }

template<class T> int relay(T&& value) { return sink(std::forward<T>(value)); }
template<class T> int named_only(T&& value) { return sink(value); }

template<class F, class... Args>
decltype(auto) call(F&& f, Args&&... args)
{
    return std::forward<F>(f)(std::forward<Args>(args)...);
}

template<class T, class... Args>
std::unique_ptr<T> make(Args&&... args)
{
    return std::make_unique<T>(std::forward<Args>(args)...);
}

struct Packet
{
    std::unique_ptr<int> payload;
    int priority;
    Packet(std::unique_ptr<int> p, int order)
        : payload(std::move(p)), priority(order) {}
};

int main()
{
    int n = 7;
    const int cn = 8;
    assert(relay(n) == 1 && relay(cn) == 2 && relay(9) == 3);
    assert(named_only(9) == 1);
    std::cout << "relay=1,2,3 named=1\n";

    auto owner = std::make_unique<int>(42);
    int* target = owner.get();
    auto packet = make<Packet>(std::move(owner), 5);
    assert(!owner && packet->payload.get() == target);
    assert(*packet->payload == 42 && packet->priority == 5);
    std::cout << "pack=2 owner-transferred target=42\n";

    auto borrow = [](int& value) -> int& { return value; };
    static_assert(std::is_same_v<decltype(call(borrow, n)), int&>);
    int& result = call(borrow, n);
    result = 10;
    assert(n == 10);
    std::cout << "return-reference=10\n";
}
```

`Args...` 是类型参数包，`args...` 是函数参数包。`std::forward<Args>(args)...` 按模式展开，对应本例的两个参数分别转发；`sizeof...(Args)` 可以得到数量。它不是运行时循环，也不意味着各参数的求值顺序由“从左到右的文字”保证。[C++17：参数包][packs]

`call` 用 `decltype(auto)` 根据返回表达式保留 int&；若这里改为普通 `auto`，本例会返回一个 int 值。一般返回 `decltype(auto)` 时要仔细看表达式与括号：`return local;` 和 `return (local);` 对局部变量可能产生不同返回类型，后者可能返回即将失效的引用。上面的程序只返回调用者仍然存活的 n，没有演示悬垂访问。

完美转发的“完美”不能扩展成寿命承诺：包装可以把右值继续交给下一层，但不会延长借用对象寿命、撤销 const、自动转移资源或让异步回调安全。这里的 call 只演示可用 `f(args...)` 调用的函数/函数对象，没有实现覆盖成员指针等形式的通用调用工具。`make` 的拥有关系来自 unique_ptr 和 Packet 的构造协议；如果下一层保存的是引用，仍须回到 [CXX-02 所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) 与 [CXX-05 回调寿命]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}}) 逐项核对。

## 5. 有模板声明，谁提供具体实现

模板常把定义放头文件，是为了让需要隐式实例化的使用处看到相应定义。并非每次出现 `Box<int>` 都立即实例化所有成员函数，更不是每次运行调用都生成新代码；实例化哪些实体取决于实际使用与规则。类成员函数的函数体也可能到被需要时才检查。[C++17：隐式实例化][implicit-inst]

如果希望把某些实现集中到 cpp，可以显式实例化。下面三个文件只提供 int 这一种实现；这是一份有限支持列表，不是“任何 T 都能用”的通用接口。[C++17：显式实例化][explicit-inst]

```cpp
// scale.h
#pragma once
template<class T> T scale(T value);
extern template int scale<int>(int);
```

```cpp
// scale.cpp
#include "scale.h"
template<class T> T scale(T value) { return value + value; }
template int scale<int>(int);
```

```cpp
// scale_main.cpp
#include "scale.h"
#include <cassert>
#include <iostream>
int main()
{
    assert(scale(7) == 14);
    std::cout << "explicit-instantiation=14\n";
}
```

`extern template` 是显式实例化声明，抑制相应隐式实例化；`template int scale<int>(int);` 是显式实例化定义，从已有模板定义提供 int 实现。它们与 `template<>` 的职责不同：显式特化提供特定实参的另一个定义，显式实例化则要求使用模板定义产生相应特化的实现。

将 `scale(7)` 改为 `scale(7L)`，调用处仍能根据声明推导 T=long，但本工程没有提供 long 定义。分开执行编译和链接，能看到“调用处通过了编译”与“链接器找不到实现”的区别。实际工程可选择把模板定义放进可见头文件，或在实现端补足支持的显式实例化；不能只在头文件添加 `extern template long scale<long>(long);` 就指望它生产定义。

同样，模板并不保证“一种 T 一份最终机器码”或“零开销”：实例化、优化、链接合并与最终产物是不同问题。本课不以生成符号数量推断四个项目的包体或性能。

## 6. 故意失败，判断卡在选择之前还是之后

先分别保存下面三个文件，只做编译检查，不执行失败产物。

```cpp
// deduction_conflict.cpp: expected compile failure
template<class T> T combine(T a, T b) { return a + b; }
int main() { return combine(1, 2.0); }
```

两个位置给出不同 T，无法完成本次推导。改为 `combine<double>(1, 2.0)`，则明确模板实参后检查参数转换；本次修复变体也纳入验证。

```cpp
// body_error.cpp: expected compile failure
#include <cstddef>
template<class C> std::size_t size_or_zero(const C& c) { return c.size(); }
std::size_t size_or_zero(...) { return 0; }
struct NoSize {};
int main() { return static_cast<int>(size_or_zero(NoSize{})); }
```

候选的参数类型能成功形成，模板以精确匹配优于省略号而被选中；随后才因函数体访问不存在的成员失败。回退函数存在也救不了它。把检测移动到合适的直接上下文，或如程序二用 `if constexpr` 选择合法实现，才是在不同层修正问题。

```cpp
// dependent_type_error.cpp: expected compile failure
template<class C> using Element = C::value_type;
struct Buffer { using value_type = int; };
int main() { Element<Buffer> value = 7; return value; }
```

这个 C++17 依赖类型别名缺少 `typename`，问题出在模板声明的解析，而不是 Buffer 真没有这个类型。改成 `typename C::value_type` 后再检查。

还可以在程序二的练习副本中，**只把 count 内的 `if constexpr` 改成普通 `if`**。调用 `count(scalar)` 时，虽然条件是 false，`c.size()` 仍产生编译错误。保留原文件，分别记录这个变体，避免把失败版覆盖成可发布示例。

### 本篇怎样验证

前三个正常程序各自独立保存；三文件工程放在同一目录。在 Windows PowerShell 中可依次执行：

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic template_selection.cpp -o template-selection.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\template-selection.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }

g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic template_tools.cpp -o template-tools.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\template-tools.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }

g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic template_forwarding.cpp -o template-forwarding.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\template-forwarding.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }

g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic -c scale.cpp -o scale.o
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic -c scale_main.cpp -o scale_main.o
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
g++ scale.o scale_main.o -o scale-demo.exe
if ($LASTEXITCODE -ne 0) { throw 'link failed' }
.\scale-demo.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

按程序顺序，输出分别为：

```text
deduction=const-and-array-checked
overload=1,2,2
function-specialization=30,20
class-specialization=0,1,2
```

```text
size=3 fallback=0 scalar=1
enable-if=integral reader=7
constexpr=constant-and-call-checked
```

```text
relay=1,2,3 named=1
pack=2 owner-transferred target=42
return-reference=10
```

```text
explicit-instantiation=14
```

三个失败文件分别用 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only 文件名.cpp` 检查，预期非零退出。普通 if 变体用同样参数检查。long 变体先用 `-c` 编译调用处，再与原来的 scale.o 链接，预期只有链接阶段失败。

2026-10-25，已从正文提取全部九个 C++ 代码块，在 Windows x64、GCC 8.3.0（x86_64-posix-seh）下执行上述 C++17 检查，不定义 NDEBUG。三个单文件程序与三文件工程均编译、运行成功，在所列选项下无警告，全部断言通过，输出分别与上述 4、3、3、1 行一致。

四个编译失败用例均以退出码 1 拒绝：推导类型冲突、选中后函数体缺少成员、依赖类型缺少 typename、普通 if 的不合法分支。long 调用文件以 `-c` 编译成功，随后链接退出码 1，报告缺少 `long scale<long>(long)`；明确类型实参与补 typename 的两个修复变体通过编译检查。没有执行失败产物或悬垂访问，也没有执行四条项目路线的实际构建、泛型代码共享、跨编译器一致性、性能或异步寿命验证。

## 7. 回到四条源码路线，要多追哪一步

| 路线 | 本课先帮你读什么 | 进入项目后继续核对 |
|---|---|---|
| Piccolo | 查询类型是显式给出还是从输出参数推导，Serializer 的声明属于重载还是特化 | 宏是否另外产生名字字符串，生成定义是否进入构建，运行时注册的名字与对象是否一致 |
| Unreal | 嵌套类型、traits、参数包与转发怎样影响候选和构造 | 当前版本的模板声明、引擎容器/对象/Delegate 协议；语言匹配不代替 UObject 或异步寿命规则 |
| HybridCLR | 原生辅助代码里模板选择了什么 C++ 操作 | 托管类型与方法的 metadata、泛型上下文和 AOT/解释执行路径，不能由 C++ 类型推导直接得出 |
| IL2CPP | 生成代码与 runtime 的 C++ 声明怎样编译、实例化和链接 | C# 泛型语义由哪段转换/运行时机制实现，身份、共享代码与注册信息分别由谁维持 |

Piccolo 可回到 [模板、宏与反射篇]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：`tryGetComponent` 的类型参数与运行时名字各有来源，`loadAsset` 则能从输出对象推导类型。本文复用这些已有项目证据，没有重新执行生成器。

UE 的容器类型、参数包与 Forward 已接 [UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})（UE 实验待验证）；公共推导/转发规则不变，具体包装和诊断按引擎契约核对。Delegate 与其他后续专项继续走 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}})，综合调试可复用 [UFS 首课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})。

HybridCLR 与 IL2CPP 先接 [从泛型语义到 IL2CPP 代码共享]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}})，再按需进入 [HybridCLR 入口]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) 和 [IL2CPP 入口]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})。C++ 模板的显式实例化练习没有验证 AOT 泛型保留、补充元数据或代码共享；相同的尖括号语法不能作为相同执行机制的证据。

遇到长报错，先圈出“调用点 → 推导出的类型 → 被移除或选中的候选 → 最先不成立的表达式”。这通常比从标准库报错栈最上层逐行猜名字更有效，但不意味着每条模板错误都能用 SFINAE 修复。

按补写顺序，下一篇进入 [CXX-08：眼前的代码由谁产生]({{< relref "engine-toolchain/cpp-foundations-08-macros-code-generation.md" >}})，继续分清宏、条件编译、外部生成与运行时注册。完整路径见 [C++ 公共基础目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**读模板调用，先还原类型和选择，再追定义与寿命。**

[deduct-call]: https://timsong-cpp.github.io/cppwp/n4659/temp.deduct.call
[deduct]: https://timsong-cpp.github.io/cppwp/n4659/temp.deduct
[partial]: https://timsong-cpp.github.io/cppwp/n4659/temp.class.spec
[specialization]: https://timsong-cpp.github.io/cppwp/n4659/temp.expl.spec
[dependent]: https://timsong-cpp.github.io/cppwp/n4659/temp.res
[transforms]: https://timsong-cpp.github.io/cppwp/n4659/meta.trans.other
[decltype]: https://timsong-cpp.github.io/cppwp/n4659/dcl.type.simple
[constexpr-if]: https://timsong-cpp.github.io/cppwp/n4659/stmt.if
[forward]: https://timsong-cpp.github.io/cppwp/n4659/forward
[packs]: https://timsong-cpp.github.io/cppwp/n4659/temp.variadic
[explicit-inst]: https://timsong-cpp.github.io/cppwp/n4659/temp.explicit
[overload]: https://timsong-cpp.github.io/cppwp/n4659/temp.over
[implicit-inst]: https://timsong-cpp.github.io/cppwp/n4659/temp.inst
[template-names]: https://timsong-cpp.github.io/cppwp/n4659/temp.names
[references]: https://timsong-cpp.github.io/cppwp/n4659/dcl.ref
