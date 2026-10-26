---
title: "C++ 公共基础 03｜一次成员调用执行谁"
slug: "cpp-foundations-03-classes-dispatch-destruction"
date: "2026-10-25"
description: "从静态类型、名字查找和重载选择走到虚函数覆盖，用独立 C++17 程序讲清同名隐藏、对象切片、构造析构阶段与通过基类删除的边界。"
tags: ["C++", "源码阅读", "继承", "虚函数", "对象切片"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 3
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "一次成员调用执行谁"
reading_summary: "先确定调用处能选择的声明，再沿对象来源、覆盖关系和生命周期阶段找到实际实现。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 493
---

在更新循环里看到一次 `object.tick()`，跳转到定义，却只找到一个空函数。继续搜索，多个派生类都有同名函数；有的参数不同，有的末尾多了一个 `const`。这些函数都会参与这次调用吗？

另一个困惑发生在复制之后：原来的派生对象明明有自己的实现，传进一个按基类值接收的函数后，却调用了基类版本。是虚函数失效了，还是这时已经换了对象？

前两篇分别解决了“通过什么路径访问对象”和“谁让对象保持有效”。这一篇接着解决第三件事：**调用处先选中了哪个声明，当前对象又让这次调用进入哪个实现。** 最后再把这条线接到复制和销毁。

> **读这篇之前**：先读 [CXX-01：值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) 和 [CXX-02：生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})。本篇会使用引用、const、unique_ptr 与析构。
>
> **材料与范围**：复用 [Piccolo 类、继承与虚函数前置]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}}) 的类型与调用模型，补齐不依赖引擎的重载、隐藏和切片练习。采用 C++17；先讨论普通单继承和单对象删除，不展开多继承布局、RTTI 实现或托管运行时的分派机制。

## 1. 对象没变，访问它的接口可以变

类定义描述成员和操作，对象则是按这个类型创建的实例。派生类对象包含基类子对象：可以把它理解为完整对象中属于基类的那一部分。这是类型关系，不是在承诺某一种内存排列。

假设 `Derived` 公开继承 `Base`，当前完整对象是一个 `Derived`。用 `Base&` 或 `Base*` 访问它，通常只是换了一条访问路径，没有另造一个 `Base` 对象，也没有把原对象变成基类。

阅读时要分别记下这些信息：

| 要追踪的事 | 一个具体判断 | 它影响什么 |
| --- | --- | --- |
| 对象来源 | 这里实际创建了 Derived | 正常使用期间的动态类型和可用状态 |
| 表达式的静态类型 | `Base& view` 所指对象表达式按 Base 接口使用 | 查找哪些成员，检查哪些调用形式 |
| 被选函数的性质 | 非虚函数、虚函数，还是显式限定调用 | 是否继续按覆盖关系选择实现 |
| 当前生命周期阶段 | 完整使用阶段，还是某层构造/析构中 | 虚调用可以进入哪一层 |
| 访问与拥有关系 | 只是借用，还是负责最终释放 | 调用是否有效，谁发起销毁 |

**动态类型不会给基类接口自动增加成员。** 即使 `Base*` 指向 `Derived`，也不能直接通过它调用只在 `Derived` 中声明的成员。应该先确认接口设计和对象来源，而不是见到编译错误就加一个强制转换。

读类头时还需要几个最低限度的语法约定。`class Derived : public Base` 表示公开继承；本篇的 `struct Derived : Base` 默认也是公开继承。`class` 的默认成员访问和默认继承方式是 private，`struct` 则是 public。`public`、`protected`、`private` 决定哪些上下文可以访问成员，不能替代对象存在与所有权判断。

头文件中的 `void tick(float) override;` 是声明，类外的 `void Derived::tick(float) { ... }` 是定义；其中 `Derived::` 指出函数所属的类。先找到声明，再找对象创建处和覆盖定义，比只搜索同名函数更可靠。

## 2. 把名字查找、重载和覆盖分成三步

先看三个经常被混成“同名函数”的概念：

| 概念 | 它解决的问题 | 典型现象 |
| --- | --- | --- |
| 名字查找与隐藏 | 从这个接口出发，能找到哪些同名声明？ | 派生类的同名声明可能使基类的一组重载不再被直接找到 |
| 重载选择 | 候选中哪个函数最适合这次参数和对象表达式？ | `step(int)` 与 `step(double)` 根据调用选择不同声明 |
| 虚函数覆盖 | 选中虚函数后，当前对象应执行哪个覆盖实现？ | 通过 Base 引用调用，最终进入 Derived 的实现 |

对于本文这些普通成员调用，可以按下面的顺序推理：

1. 从对象表达式的静态类型出发做成员查找，形成候选。
2. 根据参数类型、转换以及对象的 const 等条件做重载选择，并满足访问权限等检查。
3. 如果选中非虚函数，执行这个函数；如果是没有显式限定的虚调用，再按对象及其当前阶段寻找最终覆盖实现。

这里的“最终覆盖实现”是某条继承关系在当前对象上最终采用的版本，不等于一定写在继承树的最末层；派生类没有覆盖时，可以沿用继承来的实现。[名字查找](https://timsong-cpp.github.io/cppwp/n4659/class.member.lookup)、[重载选择](https://timsong-cpp.github.io/cppwp/n4659/over.match)、[成员调用规则](https://timsong-cpp.github.io/cppwp/n4659/expr.call)

这个顺序说明了一个关键限制：虚分派不会重新收集所有派生类重载，再按运行时情况挑一个“最合适”的同名函数。它接着处理的是前一步已经选中的虚函数。

### 完整练习一：同名隐藏为什么会改变重载结果

保存为 `lookup_dispatch.cpp`。`Base` 有两个 `step` 重载，只有接收 `int` 的版本是虚函数。`Hidden` 和 `Visible` 都覆盖这个版本，但只有 `Visible` 用 `using Base::step` 把基类同名函数引入查找范围。

`Check` 先核对结果，再打印调用与实际函数名；返回的字符串只是教学标记，没有引擎含义。

```cpp
#include <cassert>
#include <iostream>
#include <string>

struct Base
{
    virtual ~Base() = default;
    const char* label() const { return "Base::label"; }
    virtual const char* step(int) const { return "Base::step(int)"; }
    const char* step(double) const { return "Base::step(double)"; }
};

struct Hidden : Base
{
    const char* step(int) const override { return "Hidden::step(int)"; }
};

struct Visible : Base
{
    using Base::step;
    const char* step(int) const override { return "Visible::step(int)"; }
    const char* label() const { return "Visible::label"; }
};

void Check(const char* expression, const char* actual, const char* expected)
{
    assert(std::string(actual) == expected);
    std::cout << expression << " -> " << actual << '\n';
}

int main()
{
    Hidden hidden;
    Visible visible;
    Base& view = visible;

    Check("hidden.step(1.5)", hidden.step(1.5), "Hidden::step(int)");
    Check("visible.step(1.5)", visible.step(1.5), "Base::step(double)");
    Check("view.step(1)", view.step(1), "Visible::step(int)");
    Check("view.step(1.5)", view.step(1.5), "Base::step(double)");
    Check("view.label()", view.label(), "Base::label");
    Check("visible.label()", visible.label(), "Visible::label");
    Check("view.Base::step(1)", view.Base::step(1), "Base::step(int)");
}
```

要核对的输出是：

```text
hidden.step(1.5) -> Hidden::step(int)
visible.step(1.5) -> Base::step(double)
view.step(1) -> Visible::step(int)
view.step(1.5) -> Base::step(double)
view.label() -> Base::label
visible.label() -> Visible::label
view.Base::step(1) -> Base::step(int)
```

逐句解释，比背“虚函数看实际类型”更有用：

- `hidden.step(1.5)` 从 Hidden 查找，找到它自己的 `step(int)`；基类的 `step(double)` 被同名隐藏。本例允许把 1.5 转换成 int 后调用，所以代码可以通过编译，但参数转换未必符合业务意图。
- `visible.step(1.5)` 有 `using Base::step`，基类的 double 版本参与候选选择，以精确匹配胜出。`using` 没有调用函数，也没有复制一个函数体。
- `view.step(1)` 先在 Base 接口中选中虚函数 `step(int)`，再进入 Visible 的覆盖实现。
- `view.step(1.5)` 选中的是非虚的 double 版本，执行 Base 的实现；不能因为旁边的 int 版本是虚函数，就把整个名字都当成虚调用。
- 两个 `label()` 是非虚函数。通过不同静态接口查找，分别进入 Base 与 Visible；派生类同名函数没有把基类函数变成虚函数。
- `view.Base::step(1)` 显式指定基类版本，抑制这次调用的虚分派。[限定调用规则](https://timsong-cpp.github.io/cppwp/n4659/expr.call)

因此，派生类增加一个同名函数时，要同时检查它有没有覆盖既有虚函数、有没有隐藏其他重载，以及是否需要 `using` 保留原接口。不能把所有现象都归到“多态”这一个词下。

## 3. override 把“我想覆盖”变成编译器检查

覆盖必须满足相应规则。名字相同还不够，参数类型列表、成员函数尾部的 const 和引用限定等都会影响关系；返回类型也要符合相同或受限制的协变规则。本篇不展开协变细则。

派生函数覆盖成功后，即使不重复写 `virtual`，它仍然是虚函数。`override` 的作用是要求编译器检查这份声明确实覆盖了某个基类虚函数。[覆盖与 override 规则](https://timsong-cpp.github.io/cppwp/n4659/class.virtual)

下面的 `override_error.cpp` 是一个完整的**预期编译失败**用例：

```cpp
struct Base
{
    virtual ~Base() = default;
    virtual void tick(float) const {}
};

struct Wrong : Base
{
    void tick(float) override {} // 少了尾部 const，没有覆盖基类版本。
};

int main() {}
```

去掉 `override` 不是修复覆盖关系：那只会取消这项检查，让一个不同签名的同名函数留在派生类中。此时通过 Base 接口调用原来的虚函数，仍找不到这份期望中的覆盖实现。

还有三个声明边界需要区分：

- 带函数体的空虚函数是一个合法默认实现，不要求每个派生类都覆盖。
- `virtual void tick(float) = 0;` 是纯虚声明。仍具有纯虚最终覆盖实现的类是抽象类，不能直接创建它的完整对象，但可以使用其指针或引用。
- 虚函数上的 `final` 禁止后续派生类再次覆盖这个函数。它表达扩展约束，不能单凭这个关键字断言性能提高了多少。[纯虚与抽象类](https://timsong-cpp.github.io/cppwp/n4659/class.abstract)、[final 规则](https://timsong-cpp.github.io/cppwp/n4659/class.virtual)

覆盖普通成员函数后，基类版本也不会自动先执行一遍。需要复用基类逻辑时，应在函数体里显式安排调用。构造和析构自动串起基类与成员，是另外一组规则。

## 4. 切片之后，调用的已经是另一个对象

前面的 `Base& view = visible` 借用同一个完整对象的基类部分。换成 `Base copy = visible`，含义就变了：现在要创建一个独立的 Base 值对象，用源对象的基类部分初始化它。

在本篇默认复制规则下，基类自己的状态会被复制，派生层的额外状态不会进入新对象。新对象的动态类型就是 Base，也没有原派生类的覆盖行为。这通常称为**对象切片**。它不是把原对象中的派生部分删掉，也不是复制了一个“少几行数据的 Derived”。[复制构造规则](https://timsong-cpp.github.io/cppwp/n4659/class.copy.ctor)

### 完整练习二：借用、基类值与按值参数

保存为 `slicing.cpp`。Base 的 `id` 是基类状态，Derived 的 `extra` 是派生类增加的状态。`Show` 通过引用访问目标，`ByValue` 则明确接收一个 Base 值。

```cpp
#include <cassert>
#include <iostream>
#include <string>

struct Base
{
    int id;
    explicit Base(int value) : id(value) {}
    virtual ~Base() = default;
    virtual const char* kind() const { return "Base"; }
};

struct Derived : Base
{
    int extra;
    Derived(int value, int detail) : Base(value), extra(detail) {}
    const char* kind() const override { return "Derived"; }
};

void Show(const char* label, const Base& value)
{
    std::cout << label << " -> " << value.kind()
              << " id=" << value.id << '\n';
}

void ByValue(Base value)
{
    assert(std::string(value.kind()) == "Base");
    value.id = 99;
    Show("by value", value);
}

int main()
{
    Derived source(7, 42);
    Base& borrowed = source;
    Base sliced = source;
    assert(std::string(borrowed.kind()) == "Derived");
    assert(std::string(sliced.kind()) == "Base");
    Show("borrow", borrowed);
    Show("sliced", sliced);

    source.id = 8;
    assert(borrowed.id == 8 && sliced.id == 7);
    Show("borrow after edit", borrowed);
    Show("sliced after edit", sliced);
    ByValue(source);

    assert(source.id == 8 && source.extra == 42);
    std::cout << "source -> " << source.kind()
              << " id=" << source.id << ", extra=" << source.extra << '\n';
}
```

要核对的输出是：

```text
borrow -> Derived id=7
sliced -> Base id=7
borrow after edit -> Derived id=8
sliced after edit -> Base id=7
by value -> Base id=99
source -> Derived id=8, extra=42
```

`borrowed` 和 `source` 关联的是同一个对象，所以修改 `source.id` 后，借用看到 8。`sliced` 是独立的 Base 对象，仍然保存 7；它调用自己的 Base 实现是正确结果，不是虚函数失效。

`ByValue(source)` 初始化参数时也会建立一个 Base 值。进入函数以后再调用虚函数，面对的已经是这个 Base 参数对象；修改它不会把 `source.id` 改成 99。把参数改成 `const Base&`，才是在表达通过只读借用观察原对象。

同样，`vector<Base>` 按值保存 Base 对象，不能拿来保存不同派生对象的完整多态身份。若需要保存这些对象，可以按前一篇的所有权规则选择 `unique_ptr<Base>` 等持有方式；析构是否正确还要继续看下一节。

本例故意让基类可复制，以观察切片。面向多态接口的设计可以限制这种公开复制；确实需要复制完整动态类型时，应另行设计清楚的复制协议。不要先把对象切成 Base，再期待一个强制转换把丢失的派生状态找回来。

## 5. 虚析构把基类释放接到完整对象

第二篇解释了谁负责发起释放。现在假设拥有者保存的是 `unique_ptr<Base>`，里面实际指向 Derived，使用默认删除器。释放时执行的是通过 Base 指针的删除操作，必须检查它能否正确进入派生析构链。

在本文的 C++17 普通单对象 `delete` 场景下，通过基类指针删除动态类型不同的派生对象，基类需要虚析构函数；否则是**未定义行为**。不能把这种错误描述成“保证只少调用一次派生析构”。本篇不运行这种错误程序，也不使用某次输出为它背书。[C++17 delete 规则](https://timsong-cpp.github.io/cppwp/n4659/expr.delete)

`virtual ~Base() = default;` 是常见的公开多态删除接口。基类已有普通虚函数，不会自动让析构函数变成虚函数；派生类没有手写析构函数，也不能据此省掉基类的这份契约。

普通单继承对象的构造，可以先按“基类 → 本层成员 → 本层构造函数体”阅读。成员依声明顺序初始化，不依初始化列表的书写顺序。销毁时，本层析构函数体先执行，再按逆序销毁成员，随后进入基类清理。[初始化顺序](https://timsong-cpp.github.io/cppwp/n4659/class.base.init)、[析构顺序](https://timsong-cpp.github.io/cppwp/n4659/class.dtor)

### 完整练习三：从基类拥有者追到成员销毁

保存为 `lifecycle.cpp`。Trace 记录三个成员对象的构造、销毁和存活数量；`kind()` 用来观察不同阶段的虚调用。

```cpp
#include <cassert>
#include <iostream>
#include <memory>
#include <type_traits>
#include <utility>

struct Trace
{
    inline static int alive = 0;
    const char* name;
    explicit Trace(const char* value) : name(value)
    {
        ++alive;
        std::cout << "construct " << name << '\n';
    }
    ~Trace()
    {
        std::cout << "destroy " << name << '\n';
        --alive;
    }
};

struct Base
{
    Trace base_member{"base member"};
    Base() { std::cout << "Base ctor -> " << kind() << '\n'; }
    virtual ~Base() { std::cout << "Base dtor -> " << kind() << '\n'; }
    virtual const char* kind() const { return "Base"; }
};

struct Derived : Base
{
    Trace first;
    Trace second;
    Derived() : first("first member"), second("second member")
    {
        std::cout << "Derived ctor -> " << kind() << '\n';
    }
    ~Derived() override
    {
        std::cout << "Derived dtor -> " << kind() << '\n';
    }
    const char* kind() const override { return "Derived"; }
};

int main()
{
    static_assert(std::has_virtual_destructor_v<Base>);
    auto owner = std::make_unique<Derived>();
    assert(Trace::alive == 3);
    Base& view = *owner;
    std::cout << "normal call -> " << view.kind() << '\n';

    std::unique_ptr<Base> through_base = std::move(owner);
    assert(!owner && through_base);
    std::cout << "reset through Base\n";
    through_base.reset();
    assert(Trace::alive == 0);
    // view 的目标已经销毁，后面不再通过它访问对象。
}
```

要核对的输出是：

```text
construct base member
Base ctor -> Base
construct first member
construct second member
Derived ctor -> Derived
normal call -> Derived
reset through Base
Derived dtor -> Derived
destroy second member
destroy first member
Base dtor -> Base
destroy base member
```

这段输出同时回答了两个问题。

第一个是析构链：最后的拥有者保存 Base 接口，但虚析构先接到 Derived 析构函数体，然后销毁 second、first，再进入 Base 析构函数体与 base_member 清理。责任转交给 `through_base` 没有改变目标的完整类型。

第二个是阶段：基类构造中的 `kind()` 返回 Base，派生构造函数体与完整使用阶段返回 Derived；销毁下降到 Base 析构函数体后，又返回 Base。作用于正在构造或析构对象的虚调用，采用当前构造或析构所属类的最终覆盖实现，不会继续进入更派生的层次。[构造析构期间的调用](https://timsong-cpp.github.io/cppwp/n4659/class.cdtor)

所以，不应在基类构造中调用一个虚初始化函数，就期待它完成派生类初始化；也不应在基类析构中依赖虚调用回到已经结束的派生层。本例的各阶段都有可调用的普通实现；构造/析构路径若对该对象虚调用到纯虚函数，会涉及未定义行为，不能照搬这个例子。[纯虚调用边界](https://timsong-cpp.github.io/cppwp/n4659/class.abstract)

### 不允许经基类删除，也可以明确表达

并不是所有类都需要公开虚析构。如果一个基类只提供借用接口，不允许外部通过它删除目标，可以用 protected 非虚析构限制这条路径。对象仍由知道具体类型的拥有者按约定销毁；这不是自动解决所有权。

下面的 `protected_delete_error.cpp` 应该编译失败，即使指针值为空，析构访问权限也需要通过检查：

```cpp
struct BorrowOnly
{
    virtual void tick() {}
protected:
    ~BorrowOnly() = default;
};

int main()
{
    BorrowOnly* view = nullptr;
    delete view; // 错误：外部代码不能访问 protected 析构函数。
}
```

还要注意删除协议的差异。这里使用 `unique_ptr<Base>` 的默认删除器，所以要追通过 Base 指针的删除；`make_shared<Derived>()` 后转换成 `shared_ptr<Base>` 则保留原先建立的目标销毁方式，不能只按包装的 Base 类型推断它一定执行同样的删除表达式。自定义删除器也必须回到实际契约判断。

纯虚析构还有一个特殊点：将析构声明为 `= 0`，并不免除它参与派生对象析构链的需要；在这种使用中仍要提供定义。已有 Piccolo 文章的 RHI 案例展示了这种声明与定义的配对。

## 初始化列表先看声明顺序

构造初始化列表在构造函数体之前初始化基类与成员；构造函数体里的`member = value`则是对已完成初始化的成员赋值。const成员和引用成员等不能靠后续赋值补上初始化。

同一个类的非静态数据成员按**成员声明顺序**初始化，和冒号后书写顺序无关。下面完整程序把id声明在twice之前，后者读取已经初始化的id：

```cpp
#include <cassert>
struct Row {
    int id;
    int twice;
    explicit Row(int n) : id(n), twice(id * 2) {}
};
int main() { Row row{3}; assert(row.id == 3 && row.twice == 6); }
```

本轮编译运行退出0。若将twice声明到id之前，即使初始化列表仍写id在前，依赖也会反过来；不要运行读取未初始化id的版本来“观察顺序”。通过重排成员声明或消除依赖修复，而不是只重排冒号后的文本。[C++17成员初始化](https://timsong-cpp.github.io/cppwp/n4659/class.base.init)

`explicit Row(int)`允许`Row{3}`，限制`Row row = 3`这样的隐式转换；它不改变成员初始化顺序。`= default/delete`与特殊成员选择继续见CXX-04，不在这里重复模板或移动课。

## 6. 带着调用链进入四条源码路线

把本篇的模型放回项目时，可以为一次调用留下五项记录：

| 记录项 | 要找的证据 |
| --- | --- |
| 调用资格 | 哪个循环、条件、状态或事件让这行代码得到执行？ |
| 静态接口 | 成员、参数或包装最终提供什么类型，选中了哪个声明？ |
| 对象来源 | 哪个创建、恢复或赋值位置提供了具体对象？ |
| 实现选择 | 是否是 C++ 虚调用，有没有覆盖、名字隐藏或显式限定？ |
| 有效阶段 | 对象是否仍有效，处于完整使用还是构造/析构阶段，谁负责销毁？ |

这五项中的“调用资格”尤其容易漏掉：派生类写了覆盖函数，不代表这一帧一定会调用它。外层系统可能根本没走到调用点。

- **Piccolo**：继续读 [类、继承与虚函数前置]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})，沿 Component / RHI 的声明、创建点和调用点核对。本篇的独立程序解释公共规则，原文保留固定版本的实现证据。
- **Unreal**：从 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}}) 进入具体对象系统；遇到反射、事件包装或生命周期回调，继续追它们生成或调用了什么，不仅凭函数同名判断覆盖与执行资格。
- **HybridCLR**：沿 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 找方法指针、解释入口和运行时选择逻辑，先确认调用是哪一层机制完成的。
- **IL2CPP**：沿 [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) 区分生成代码、运行时方法信息与原生调用，不把托管语言的“虚方法”直接等同于当前看到的某个 C++ 虚成员函数。

本篇没有测量虚表布局、对象大小、间接调用成本或编译器去虚化。源码层面是虚调用，也不等于机器码必然包含一次间接跳转；实现可以在符合语言行为的前提下优化。判断实际生成代码和性能，需要对应工具链、构建配置与测量。

## 7. 自测与验证边界

读完后，试着回答五个问题：

1. `hidden.step(1.5)` 为什么没有选中 Base 的 double 重载？补 `using` 改变了哪一层？
2. 两次调用都使用同一个 Visible 对象，为什么 `view.label()` 与 `visible.label()` 执行不同函数？
3. `ByValue(source)` 中调用 Base 实现，为什么不是虚函数失效？原 Derived 对象发生了什么？
4. 为什么 `unique_ptr<Base>` 的所有权正确，不足以单独证明通过它删除 Derived 正确？
5. 为什么 Base 构造和析构中的虚调用，不会自动替你运行 Derived 的初始化与清理？

本地验证日期为 2026-10-25，环境是 Windows x64、GCC 8.3.0（`x86_64-posix-seh`），语言模式为 C++17。三个正常程序分别保存、独立编译，不把它们放进一个文件；各程序中的 Base / Derived 是各自的教学类型。

以第一个程序为例，PowerShell 命令如下；其他两个将文件名分别换成 `slicing`、`lifecycle`。若 `g++` 不在 PATH 中，应使用实际安装的编译器路径，且只在编译成功后运行程序。

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic lookup_dispatch.cpp -o lookup_dispatch.exe
.\lookup_dispatch.exe
```

不定义 `NDEBUG`，保留断言。两个失败用例分别以 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only 文件名.cpp` 检查，预期返回非零退出码。

| 用例 | 本次结果 | 验证内容 |
| --- | --- | --- |
| `lookup_dispatch.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 7 行输出逐行一致；隐藏、using、虚/非虚与限定调用符合断言 |
| `slicing.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 6 行输出逐行一致；基类值和参数发生切片，原对象保持 Derived 状态 |
| `lifecycle.cpp` | 编译、运行退出码均为 0，所列警告选项下无警告 | 12 行输出逐行一致；析构链和阶段调用符合预期，Trace::alive 最后为 0 |
| `override_error.cpp` | 编译退出码为 1，符合预期 | 诊断指出声明没有覆盖基类虚函数 |
| `protected_delete_error.cpp` | 编译退出码为 1，符合预期 | 诊断指出不能访问 protected 析构函数 |

三个正常程序的断言全部通过。两个失败用例没有生成或运行可执行文件，也没有执行非虚基类删除派生对象等未定义行为。构造/析构日志只服务于教学，不作为生产日志或性能测试方案。

资料依据为 C++17 工作草案 N4659，核对日期为 2026-10-25。独立程序不包含四个引擎或运行时的实际构建、断点观察、跨平台验证与性能测试；项目结论继续以各路线的源码基线和验证记录为准。

按当前补写顺序，下一步进入 [CXX-10：源文件怎样成为正在调试的程序]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}})，用同一个三文件工程练习声明、定义、翻译单元、链接、静态库与断点。完整知识顺序和缺口见 [C++ 公共基础目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**先确定调用处选中的声明，再沿对象来源和当前阶段找到真正执行的实现。**
