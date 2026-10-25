---
title: "C++ 公共基础 01｜一次传参到底复制、借用还是只读访问"
slug: "cpp-foundations-01-values-references-signatures"
date: "2026-10-25"
description: "从同一个对象的值、指针与引用出发，理解 const、auto、函数参数和返回值，用独立 C++17 程序练习四条源码路线共用的访问与寿命判断。"
tags: ["C++", "源码阅读", "指针与引用", "const", "auto"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 1
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "一次传参到底复制、借用还是只读访问"
reading_summary: "先分清对象、指针变量和引用别名，再判断一次调用能修改谁，以及访问何时失效。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 491
---

读引擎和运行时源码时，经常会遇到一个看似很小的问题：一个函数接收了某个变量，又修改了它，调用者为什么没有变化？换一处代码，函数明明也是按值接收参数，却修改了调用者正在使用的对象。

如果只记“值会复制、引用不会复制”，这两种情况仍然很容易混在一起。因为被复制的可能是对象本身，也可能只是一个地址，或者一个内部保存地址的包装对象。

这一篇先建立四条源码路线共用的起点：**每次调用，先判断参数本身是什么，再判断它通过哪条路径访问了哪个对象。** 对象由谁创建、由谁销毁，是需要继续追踪的另一件事。

> **读这篇之前**：会变量、函数和基本条件判断即可。有 C# 经验可以帮助理解业务场景，但不要把 C# 的 class 赋值规则直接套到 C++。完整路线见 [公共专栏目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。
>
> **范围与证据**：使用不依赖引擎的自有 C++17 程序，复用 [UFS 首课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 的值与借用模型。编译和运行记录见末节；四个项目的对象系统、GC、ABI 与实际调试不由这份程序验证。

## 1. 先分清对象、指针变量和引用别名

先用一个只保存整数的类型讨论。这里的 `struct` 定义类型，`Score score{10}` 创建对象并把成员初始化为 10；它没有引擎基类、隐藏的资源协议或自定义复制逻辑。C++ 的 class 和 struct 都可以定义按值使用的类型，不能仅凭 class 关键字就套用 C# 引用类型的赋值规则。

```cpp
struct Score
{
    int value;
};

Score score{10};
Score copy = score;
Score& alias = score;
Score* pointer = &score;
```

这四行建立的关系不同：

| 名字 | 它是什么 | 修改时会发生什么 |
| --- | --- | --- |
| `score` | 一个 `Score` 对象 | `score.value` 修改它的成员 |
| `copy` | 另一个 `Score` 对象，本例复制了整数成员 | `copy.value` 不会改变 `score.value` |
| `alias` | `score` 的引用别名 | `alias.value` 访问的就是 `score.value` |
| `pointer` | 一个保存地址的指针对象，当前指向 `score` | 修改 `pointer` 是改地址；修改 `pointer->value` 是改所指对象 |

**指针变量和它指向的对象，要画成两个位置。引用则用于指称已经存在的对象。** 不必先猜它们在栈、堆还是寄存器里；本篇讨论的是语言中的对象关系，不能从语法直接推出机器存储布局。

同一个符号在声明和表达式里含义不同：

- `Score& alias` 中的 `&` 声明引用；表达式 `&score` 中的 `&` 取得对象地址。
- `Score* pointer` 中的 `*` 声明指针；表达式 `*pointer` 通过有效指针访问所指对象。
- `score.value` 直接访问成员；对这里的普通指针，`pointer->value` 等价于 `(*pointer).value`。

引用初始化后不能改绑。下面是给 `score` 赋值，不是让 `alias` 转而引用 `other`：

```cpp
Score other{20};
alias = other;        // score.value 变为 20，alias 仍指称 score
pointer = &other;     // pointer 则改为指向 other
```

引用必须有合法的绑定对象，不能把它当成可为空的指针。C++ 中可以出现悬空引用，这并不赋予它访问已销毁对象的权利。[引用初始化与赋值规则](https://eel.is/c++draft/dcl.init.ref)

## 2. 传参时，先问形参本身接收了什么

调用者提供的是实参，函数声明里接收它的是形参。函数调用会用实参初始化形参；因此，读参数类型比只看调用处的变量名更有用。[函数调用规则](https://eel.is/c++draft/expr.call)

```cpp
void IncrementValue(Score value);       // 一个独立的 Score 形参
void IncrementRef(Score& value);        // 绑定到现有 Score 的引用
void IncrementPointer(Score* value);   // 一个保存地址的指针形参
int Read(const Score& value);          // 经只读路径访问 Score
```

用前面的 `score` 调用时，可以这样追踪：

| 调用 | 这次形参怎样建立 | 函数内的操作意味着什么 |
| --- | --- | --- |
| `IncrementValue(score)` | 从本例 `score` 复制出一个 `Score` 形参 | `++value.value` 只改形参 |
| `IncrementRef(score)` | `value` 绑定到 `score` | `++value.value` 改原对象 |
| `IncrementPointer(&score)` | 地址值复制到指针形参 `value` | `++value->value` 经地址改原对象；`value = nullptr` 只改局部指针 |
| `Read(score)` | 建立到 `score` 的 const 引用 | 可以读普通成员，不能经此引用给它赋值 |

这里的“按值”是**形参有自己的值**，不是“函数永远不会影响调用者”。一个按值传入的指针仍然可以访问共享的目标；一个按值传入、内部带指针的包装对象也一样。后面的完整程序会演示这一点。

也不要把 `T value` 理解成所有场景都执行一次复制构造：参数如何初始化，还与实参和类型自身的规则有关。本课传入的是已有的简单 `Score` 对象；移动、复制省略和自定义复制语义放到 CXX-04 展开。

### 修改目标，与修改调用者的指针，是两件事

```cpp
Score* RedirectLocal(Score* pointer, Score* replacement)
{
    pointer = replacement;
    return pointer;
}

void Rebind(Score*& pointer, Score* replacement)
{
    pointer = replacement;
}
```

`RedirectLocal` 修改自己那份指针并返回它。调用者原来的指针不会自动变；调用者可以显式接收返回值。

`Score*&` 则是“对一个 `Score*` 指针对象的引用”。`Rebind` 的形参绑定到调用者的指针变量，所以赋值能改掉调用者保存的地址。运行时接口中的 `T**` 也可能用于访问一个指针变量，但需要再解一层指针；具体输入/输出约定仍要读实现。

### 空值是接口约定的一部分

指针可以保存 `nullptr`。本篇把空指针作为可选目标，在访问前检查；这不表示所有 `T*` 接口都允许传空。

`if (pointer)` 只能在这里排除空值，不能证明对象仍然存活、类型匹配或允许在当前线程访问。裸指针类型也没有自动写出释放责任。**本篇将裸指针和引用作为借用使用；这是教学接口的约定，不是 C++ 对所有项目的强制规定。**

## 3. const 限制哪一层，就先读哪一层

`const` 最容易被说成“这个对象永远不会变”。更准确的起点是：这条访问路径允许怎样使用对象。

```cpp
Score score{10};
const Score& view = score;
score.value = 11;
// view.value 也读到 11；view 不是创建时刻的快照
```

`view` 不允许直接给这个普通成员赋值，但另一个合法的可写路径仍然可以修改原对象。只读访问也不提供线程同步。[const 访问路径规则](https://eel.is/c++draft/dcl.type.cv)

### 指针自己只读，还是它指向的对象只读

| 声明 | 可以改变指向吗 | 可以经它修改本例 `Score::value` 吗 |
| --- | --- | --- |
| `Score* p` | 可以 | 可以 |
| `const Score* p` / `Score const* p` | 可以 | 不可以 |
| `Score* const p` | 不可以 | 可以 |
| `const Score* const p` | 不可以 | 不可以 |

这里说的“可以访问”都以目标有效为前提。靠近 `Score` 的 const 限定所访问的类型；`*` 后面的 const 限定指针对象自身。[指针声明规则](https://eel.is/c++draft/dcl.ptr)

这种限定不会自动沿任意指针传递。假设包装类型只有一个 `Score* target` 成员，包装本身为 const 时，不能给这个指针成员重新赋值，但仍可以经它修改有效目标的 `value`。因此，“只读包装”“只读目标”和“整个对象关系图都不可变”不能混用。

### 成员函数尾部的 const 又管什么

先看这个教学类型，函数体写在类型内部，不需要额外的类知识才能跟读：

```cpp
struct Board
{
    Score current{10};

    Score Snapshot() const { return current; }
    const Score& Current() const { return current; }
    void Set(int next) { current.value = next; }
};
```

`Current()` 中有两个需要分别阅读的位置：

1. 返回类型 `const Score&`：交给调用者一个只读引用，仍然访问 `current`。
2. 函数尾部 `const`：这个普通成员函数通过指向 const `Board` 的 `this` 访问自身，不能直接修改这里的 `current.value`。

`Snapshot()` 尾部同样有 const，但返回类型是 `Score`，所以返回的是值。尾部 const 不会把返回类型自动改成只读引用。[this 与成员函数限定](https://eel.is/c++draft/expr.prim.this)

更复杂的类可能使用 `mutable`，或通过指针访问别的对象；const 成员函数也不等于“没有任何副作用”。本课先用没有这些额外机制的 `Board` 确立边界。

## 4. auto 会推导类型，不会替你决定借用

`auto` 是编译期的类型推导，不是运行时随意换类型。面对一个已有对象，少写一个 `&`，就可能从别名变成新对象。

假设有 `Score score{10}; const Score& view = score;`：

| 声明 | 本例的最终类型 | 关系 |
| --- | --- | --- |
| `auto a = view;` | `Score` | 新对象，从 view 所指对象复制 |
| `auto& b = score;` | `Score&` | 原对象的可写别名 |
| `auto& c = view;` | `const Score&` | 保留目标的 const 限定 |
| `const auto& d = score;` | `const Score&` | 原对象的只读别名 |
| `auto p = &score;` | `Score*` | 新指针对象，指向 score |
| `const auto q = p;` | `Score* const` | 指针自身不可改绑，目标仍可写 |
| `const auto* r = p;` | `const Score*` | 可以改指向，经它只读目标 |

在这些普通变量初始化中，按值的 `auto` 不保留来源的引用关系和最外层 const；指针所指类型的 const 则不会因此消失。例如，从 `const Score*` 复制得到的 `auto` 仍是 `const Score*`。要判断复制了什么，先确定最终类型。[auto 推导规则](https://eel.is/c++draft/dcl.type.auto.deduct)、[相关的类型推导规则](https://eel.is/c++draft/temp.deduct.call)

这也解释了一个常见的调用端问题：

```cpp
Board board;
auto saved = board.Current();        // saved 是独立的 Score
const auto& live = board.Current();  // live 仍访问 board.current
board.Set(20);
// saved.value 为 10，live.value 为 20
```

函数返回引用，不代表调用者一定保留引用。返回类型与接收方式要连起来读。遇到 `auto&&`、`decltype(auto)` 时需要补值类别与推导规则，本课的表不能直接推广到所有形式。

## 5. 返回一个值，还是借用一个仍需存活的对象

前面的 `Snapshot()` 返回一个 `Score` 值，调用者拿到自己的结果；`Current()` 返回成员引用，调用者继续依赖原 `Board` 对象及该成员的有效性。

例如，把 `Current()` 的结果保存到更长的任务中，仅仅保存引用不会让 `Board` 延长寿命。容器修改、对象重建或项目自身的失效协议，也可能影响返回的访问路径。这些约束需要结合接口实现判断。

下面是一个**只用于分析的错误示例，不要运行它来观察结果**：

```cpp
const Score& Broken()
{
    Score local{10};
    return local;  // 函数返回后 local 已结束生命周期
}
```

类型写着 `const Score&`，并不会替 `local` 保活。之后经这个引用读取成员属于无效访问；某次恰好读到 10、读到别的数或崩溃，都不是可依赖的行为。

### 临时对象确实有延寿规则，但不能沿调用任意传递

以下两种绑定要分开：

```cpp
const Score& Echo(const Score& value) { return value; }

void LifetimeBoundary()
{
    const Score& direct = Score{30};       // 此处直接绑定，临时对象延寿
    const Score& escaped = Echo(Score{40}); // 此语句结束，40 对应的临时对象销毁
    // direct 可在此作用域继续读取；不要再通过 escaped 读取成员
}
```

在第一行初始化中，临时对象随这个局部引用延长寿命；第二行里，绑定到函数引用形参的临时对象只活到包含调用的完整表达式结束，在此例中就是该声明语句的分号处。把引用返回出来再接一次，不会接力延寿。[临时对象寿命规则](https://eel.is/c++draft/class.temporary)

这不是要求现在背完所有临时对象规则。先建立一个阅读习惯：**借用准备跨出当前调用或作用域时，明确写出目标是谁、它至少活到哪里。**

## 6. 用一个完整程序，核对前面的判断

在独立练习目录保存下面的 `main.cpp`。这份程序不申请动态内存，也不使用引擎头文件；`assert` 用来检查预期关系，断言失败意味着需要重新检查程序或预测。

```cpp
#include <cassert>
#include <iostream>

struct Score
{
    int value;
};

void IncrementValue(Score value)
{
    ++value.value;
    std::cout << "inside value=" << value.value << '\n';
}

void IncrementRef(Score& value)
{
    ++value.value;
}

void IncrementPointer(Score* value)
{
    if (value != nullptr)
        ++value->value;
}

Score* RedirectLocal(Score* pointer, Score* replacement)
{
    pointer = replacement;
    return pointer;
}

void Rebind(Score*& pointer, Score* replacement)
{
    pointer = replacement;
}

struct ScoreView
{
    Score* target;  // 本例要求使用时目标仍有效；不负责销毁目标
};

void IncrementView(ScoreView view)
{
    ++view.target->value;
}

int main()
{
    Score score{10};
    IncrementValue(score);
    assert(score.value == 10);
    std::cout << "after value=" << score.value << '\n';

    IncrementRef(score);
    assert(score.value == 11);
    std::cout << "after reference=" << score.value << '\n';

    IncrementPointer(&score);
    IncrementPointer(nullptr);  // 此接口允许空值，不执行修改
    assert(score.value == 12);
    std::cout << "after pointer=" << score.value << '\n';

    Score other{20};
    Score* selected = &score;
    Score* redirected = RedirectLocal(selected, &other);
    assert(selected == &score && redirected == &other);
    std::cout << "local redirect: original=" << selected->value
              << " result=" << redirected->value << '\n';

    Rebind(selected, &other);
    assert(selected == &other);
    std::cout << "reference redirect=" << selected->value << '\n';

    const Score& view = score;
    auto copy = view;
    ++copy.value;
    assert(score.value == 12 && copy.value == 13);
    std::cout << "auto value: source=" << score.value
              << " copy=" << copy.value << '\n';

    auto& alias = score;
    ++alias.value;
    assert(&alias == &score && view.value == 13);
    std::cout << "auto reference: source=" << score.value
              << " view=" << view.value << '\n';

    auto pointerCopy = &score;
    const auto fixedPointer = pointerCopy;
    ++fixedPointer->value;  // const 限定的是指针自身
    assert(score.value == 14 && view.value == 14);
    std::cout << "const pointer: source=" << score.value
              << " view=" << view.value << '\n';

    const ScoreView handle{&score};
    IncrementView(handle);  // 复制包装；复制后仍指向同一个目标
    assert(score.value == 15);
    std::cout << "copied view: source=" << score.value << '\n';

    const Score& temporary = Score{30};
    assert(temporary.value == 30);  // 只验证这里的直接绑定
}
```

### 最小编译与运行

使用机器上已有、支持 C++17 的编译器即可。下面给出 Windows PowerShell 的 GCC 路径；不需要安装引擎，也不需要为了重现本文而安装同一历史版本。

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic main.cpp -o values.exe
if ($LASTEXITCODE -ne 0) { throw 'compile or link failed' }
.\values.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

这条 `g++` 命令调用编译和链接步骤，成功后运行本次生成的程序。不要在构建失败后继续运行遗留的旧 exe；也不要加 `-DNDEBUG`，否则 `assert` 检查会被关闭。头文件、翻译单元、目标文件与链接的完整练习继续见 [UFS 首课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})，后续公共 CXX-10 再统一展开。

先预测，再对照这十行输出：

```text
inside value=11
after value=10
after reference=11
after pointer=12
local redirect: original=12 result=20
reference redirect=20
auto value: source=12 copy=13
auto reference: source=13 view=13
const pointer: source=14 view=14
copied view: source=15
```

最后一行尤其值得解释：`IncrementView` 按值接收 `ScoreView`，却改到了原 `score`。原因是包装里的指针值被复制，目标 `Score` 没被复制。不能从“按值”直接推导“目标完全独立”，也不能从“const 包装”推导“目标只读”。

## 7. 让编译器拒绝两种不成立的访问

正常输出说明了一部分关系，还需要知道哪些用法根本不应该通过编译。下面两个文件**分别编译**，不要粘进正常程序；预期都是编译失败，不生成可运行的验收结果。

`readonly-error.cpp`：尝试通过只读引用修改普通成员。

```cpp
struct Score { int value; };

void Change(const Score& score)
{
    ++score.value;
}

int main() {}
```

`binding-error.cpp`：试图把普通可写左值引用绑定到临时对象。

```cpp
struct Score { int value; };

void Increment(Score& score) { ++score.value; }

int main()
{
    Increment(Score{10});
}
```

依次执行 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only readonly-error.cpp` 和对应的 `binding-error.cpp` 命令，查看非零退出码及首个有效诊断。前者应指向只读成员的修改，后者应指向无法绑定的引用。具体报错文本随编译器而变。

相对地，`RedirectLocal` 没改到调用者的指针并不是语法错误；它完全合法，只是可能不符合业务意图。悬空借用又属于另一类问题，编译器未必替你完整查出，不能用“编译成功”作为有效访问的证明。

## 8. 回到源码：用四个问题读一个接口

把本篇的模型放回函数声明时，按下面顺序问：

1. **输入建立了什么？** 值对象、指针对象，还是绑定到现有对象的引用？有类型别名时先展开别名。
2. **经这条路径能改谁？** 原对象、局部副本、指针变量、指向的目标，分别判断 const。
3. **返回的是什么？** 一个值，还是仍依赖别处对象的访问路径？调用者又用什么类型接收？
4. **访问有效到何时？** 谁维持目标寿命，函数是否保存了参数，空值是否允许，是否另有线程或状态约束？

这里的“读签名”指阅读声明中的参数、返回类型和成员限定，不等于说这些成分都属于 C++ 标准术语中同一种函数的 signature。仅凭声明也不能知道函数会不会保存一个指针；还要继续读函数体与调用契约。

### 四条路线共用到哪里，接下来各补什么

| 路线 | 本课可以直接使用的判断 | 进入具体系统仍需补充 |
| --- | --- | --- |
| Piccolo | 先确认变量是对象、标准智能指针还是项目包装；`auto` 复制包装后，目标是否共享要读包装语义 | 从 [智能指针与生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) 继续追保活与销毁；普通裸指针复制不产生强持有关系 |
| Unreal | C++ 值、引用、const 和接收方式仍按语言规则判断；名字像指针的引擎类型需看其定义 | 沿 [UFS 综合练习]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 到 [UE 路线入口]({{< relref "reading/unreal-engine.md" >}})，另补普通对象与 UObject 的创建、持有和结束约定 |
| HybridCLR | 阅读参数、地址和返回位置时，先分清指针自身与它所访问的数据；类型别名也不能跳过 | 沿 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 补方法指针、参数槽、ABI 和解释帧；本课不推导槽位布局或托管对象寿命 |
| IL2CPP | 在允许使用的自有生成代码样本中，同样逐项区分值参数、地址、const 与返回方式 | 沿 [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) 补生成代码、metadata 和 runtime 的关系；C++ 访问类型不能代替托管语义与 GC 规则 |

这张表是跟读方法，不是四个系统使用同一对象协议的证明。方法指针、内存布局、GC、引用计数都需要自己的知识和证据；本篇先解决“这一行正在访问谁”。

## 9. 自测与验证边界

不打开编译器，先回答三题，再回到完整程序验证同类关系：

1. `const Score& view = score; auto next = view;` 后修改 `next.value`，会修改 `score` 吗？**本例不会。** `next` 是复制出的 `Score`，并没有保留引用关系。
2. `const auto pointer = &score;` 能否执行 `++pointer->value`？**可以，前提是目标有效。** const 限定的是指针自身。
3. 某函数返回 `const Score&`，能否把结果无条件存到下一帧？**不能。** 先确认目标是谁、何时失效，以及谁让它活到下一帧；只读不提供保活。

设计自己的接口时，也不必把所有输入一律改成引用。便宜的小值常适合按值传入；较大的只读输入可以考虑 const 引用。需要可选目标或需要修改调用者状态时，再明确表达相应约定。这里是接口设计原则，不是“引用总比复制快”的性能结论。[C++ Core Guidelines F.16](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rf-in)

| 验证项目 | 状态 |
| --- | --- |
| 完整 C++17 程序 | 2026-10-25，从正文代码块提取；Windows x64 GCC 8.3.0（Strawberry 分发），按上述参数编译无诊断、运行退出 0；全部断言通过，十行输出逐行一致 |
| auto 表与返回方式 | 从正文提取七种声明，用编译期类型断言核对；提取 Board 与调用片段，验证 saved 为 10、live 为 20；编译和运行通过 |
| 两个编译失败练习 | 分别从正文提取并以 C++17 编译，均退出 1；诊断分别指出修改只读成员、非 const 左值引用无法绑定临时对象 |
| 悬空引用与临时对象反例 | 依据语言规则分析，不通过执行无效访问采集结果 |
| 引擎与运行时 | 未进行 Piccolo / Unreal / HybridCLR / IL2CPP 的构建、断点或运行测试；未做性能比较 |

资料核对日为 2026-10-25。外部标准链接指向持续维护的工作草案，本文示例限定为 C++17；本机验证不能替代其他编译器、平台和项目版本的验证。

下一篇进入 [CXX-02：对象与资源由谁结束]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})，沿 RAII、独占/共享拥有和弱观察，继续追踪谁保活、谁最终释放。项目应用可读 [Piccolo 智能指针篇]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}})，完整顺序见 [公共专栏目录]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**先分清复制的是谁、访问的是谁，再确认这条访问能持续多久。**
