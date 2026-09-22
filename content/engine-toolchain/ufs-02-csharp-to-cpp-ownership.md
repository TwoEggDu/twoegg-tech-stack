---
title: "从 C# 到 C++：对象由谁持有，代码怎样变成程序？"
slug: "ufs-02-csharp-to-cpp-ownership"
date: "2026-09-22"
description: "用三个完整 C++ 文件练习值、借用、unique_ptr、RAII、容器失效和断点，再亲手区分编译错误、链接错误与运行逻辑错误，为 TwinArena 的 UE C++ 工程打底。"
tags: ["Unreal", "C++", "C#", "Debugging"]
series: "UE 全栈工程师成长路线"
series_order: 2
weight: 4602
---

在 C# 里，把一个对象交给另一个方法，通常不用同时决定“最后由谁释放这块内存”。到了 C++，一句看似相同的赋值，可能复制一个值、借用现有对象，也可能转移释放责任。程序能编译，并不能替你回答这些问题。

我们先离开 UE，做一个只有三个文件的练习：预测对象何时销毁，找到一次链接失败，再用调试器解释一个“改了值却没改到调用者”的错误。这样进入 UE 时，才能分清普通 C++ 规则和引擎对象规则。

这是路线 **UFS-02 的 M0 最小桥梁课**，服务 M0-T02。你需要会 C# 方法、类、集合和基本断点，不需要先读完 C++ 教科书。后续进入 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})，然后完成 [旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})。并发、模板元编程和自制分配器不在本课验收范围。

> 环境与证据：练习使用标准 C++17，不依赖 UE。本文代码的编译、运行、故障与调试验证记录见末节；UE 系列安装目标为用户选定的 **5.8.2**，本篇普通 C++ 通过不能证明 UE 工具链可用。

## 1. 先拆开三个问题：值在哪里，谁能访问，谁负责结束它

| 写法 | 这次操作表达什么 | C# 背景最容易带来的误解 |
| --- | --- | --- |
| `Score b = a;` | 新建一个值，按类型的复制规则初始化 | C++ `class` 也能按值复制；不是见到 class 就共享对象 |
| `Score& b = a;` | `b` 是已有对象的别名，初始化后不能改绑 | 引用不是自动延长任意对象寿命的托管句柄 |
| `Score* p = &a;` | 保存地址，可以为空或改指向 | 非空不证明对象仍活着；裸指针本身没写明所有权 |
| `const Score& b = a;` | 经这条访问路径只读，通常避免复制 | 不代表其他路径不能修改对象，也不自动保证线程安全 |
| `std::unique_ptr<Tracked>` | 持有一个独占释放责任，离开作用域时调用删除器 | `get()` 得到的裸指针只是借用，不获得释放权 |

这里的“借用”是接口约定，不是 C++ 编译器替你完整检查的生命周期系统。本课约定：裸指针/引用不转移所有权；拥有关系优先用值或明确的资源包装类型表达。`const T*` 限制经指针修改对象，`T* const` 限制指针改绑，两者不是一回事。

局部自动对象的生命周期通常随作用域结束；动态创建的对象可以跨作用域存活。不要把规则缩成“值一定在栈上、引用一定在堆上”：对象还可能是另一个对象的成员；语言的生命周期与存储位置是两层问题。

**RAII** 把资源的获取/释放绑定到对象的构造/析构。资源可以是内存，也可以是文件句柄、锁或网络连接。它与 C# `using`/`IDisposable` 的“到边界就释放”思路相近，不等于等待 C# finalizer。正常离开作用域与异常栈展开会调用已构造自动对象的析构；强杀进程不是这种保证的适用场景。[C++ Core Guidelines：资源管理](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rr-raii)

## 2. 代码为什么要分头文件和实现文件

声明告诉使用者“这个名字存在，参数和返回类型是什么”；定义提供函数体或对象实体。头文件通常承载共享声明，`.cpp` 放非内联实现。`#include` 让当前翻译单元看到头文件内容，不会自动把另一个 `.cpp` 加入链接。

```text
main.cpp + 展开的 Training.h   ──编译──> main.o
Training.cpp + Training.h      ──编译──> Training.o
main.o + Training.o + 标准库   ──链接──> training.exe
```

`.o` 是下文 GCC 的目标文件名；MSVC 常用 `.obj`。调用者看见 `int BonusPoints();` 后可以通过编译，但链接器仍要找到匹配的定义。头文件缺失、类型不匹配通常先卡编译；声明有了而实现没提供，通常卡链接。语法都正确、算法却错，是第三层问题。

`#pragma once` 防止同一翻译单元重复包含此头文件；它不是跨所有 `.cpp` 消除重复定义的万能开关。把普通非内联函数体随便放进头文件，可能在链接时得到重复定义。下面的函数模板放在头文件，是为了让使用处能看见实例化需要的定义。

## 3. 建一个独立练习目录，保存三个完整文件

在你自己的练习位置新建 `cpp-ownership` 空目录，不放到 UE 的 `Source/`，也不覆盖已有项目。三个文件使用 UTF-8 保存，文件名区分清楚，尤其避免编辑器偷偷保存成 `.cpp.txt`。

```text
cpp-ownership/
  Training.h
  Training.cpp
  main.cpp
```

### `Training.h`

```cpp
#pragma once

#include <string>

struct Score
{
    int Value = 10;
};

class Tracked
{
public:
    explicit Tracked(std::string name);
    ~Tracked();

    Tracked(const Tracked&) = delete;
    Tracked& operator=(const Tracked&) = delete;

    const std::string& Name() const;

private:
    std::string name_;
};

void IncrementValue(Score value);
void IncrementRef(Score& value);
void Borrow(const Tracked& value);
void BorrowOptional(const Tracked* value);
int BonusPoints();

template<class T>
T Twice(T value)
{
    return value + value;
}

class Reporter
{
public:
    virtual ~Reporter() = default;
    virtual const char* Kind() const = 0;
};

class ConsoleReporter final : public Reporter
{
public:
    const char* Kind() const override;
};
```

### `Training.cpp`

```cpp
#include "Training.h"

#include <iostream>
#include <utility>

Tracked::Tracked(std::string name) : name_(std::move(name))
{
    std::cout << "ctor " << name_ << '\n';
}

Tracked::~Tracked()
{
    std::cout << "dtor " << name_ << '\n';
}

const std::string& Tracked::Name() const
{
    return name_;
}

void IncrementValue(Score value)
{
    ++value.Value;
    std::cout << "value copy=" << value.Value << '\n';
}

void IncrementRef(Score& value)
{
    ++value.Value;
}

void Borrow(const Tracked& value)
{
    std::cout << "borrow " << value.Name() << '\n';
}

void BorrowOptional(const Tracked* value)
{
    if (value == nullptr)
    {
        std::cout << "null borrow\n";
        return;
    }
    Borrow(*value);
}

int BonusPoints()
{
    return 3;
}

const char* ConsoleReporter::Kind() const
{
    return "console";
}
```

### `main.cpp`

```cpp
#include "Training.h"

#include <fstream>
#include <iostream>
#include <memory>
#include <string>
#include <utility>
#include <vector>

int main()
{
    std::cout << std::boolalpha;
    std::cout << "begin\n";
    Tracked local("local");

    Score score;
    IncrementValue(score);
    std::cout << "caller=" << score.Value << '\n';
    IncrementRef(score);
    std::cout << "after ref=" << score.Value << '\n';
    BorrowOptional(nullptr);
    Borrow(local);

    {
        auto owner = std::make_unique<Tracked>("owned");
        const Tracked* borrowed = owner.get();
        Borrow(*borrowed);

        auto transferred = std::move(owner);
        std::cout << "source null=" << (owner == nullptr) << '\n';
        Borrow(*borrowed); // Ownership moved; the Tracked object stayed alive.

        transferred.reset();
        borrowed = nullptr; // Never dereference it after reset().
        std::cout << "owner reset\n";
    }

    std::vector<int> values{7};
    const int* first = &values[0];
    const auto oldCapacity = values.capacity();
    values.reserve(oldCapacity + 1); // Forces reallocation for this tiny vector.
    // The previous first is invalid. Do not read, compare or dereference it.
    first = &values[0]; // Acquire a new pointer to the still-existing element.
    std::cout << "capacity grew=" << (values.capacity() > oldCapacity) << '\n';
    std::cout << "first=" << *first << '\n';
    std::cout << "double=" << Twice<int>(*first) << '\n';

    ConsoleReporter reporter;
    const Reporter& reporterView = reporter;
    std::cout << "report=" << reporterView.Kind() << '\n';
    std::cout << "bonus=" << BonusPoints() << '\n';

    {
        std::ofstream note("raii-note.txt");
        if (!note)
        {
            std::cerr << "cannot open note\n";
            return 1;
        }
        note << "closed by scope\n";
        note.flush();
        if (!note)
        {
            std::cerr << "cannot write note\n";
            return 1;
        }
    } // ofstream's destructor closes the file; no manual delete/close needed.

    std::ifstream note("raii-note.txt");
    std::string line;
    if (!std::getline(note, line))
    {
        std::cerr << "cannot read note\n";
        return 1;
    }
    std::cout << "file=" << line << '\n';
    std::cout << "end body\n";
    return 0;
}
```

## 4. 先预测，再编译运行

先在纸上回答：`caller` 和 `after ref` 各是多少？移动 `unique_ptr` 会不会出现第二次 `ctor owned`？`dtor owned` 出现在 `owner reset` 前还是后？`end body` 是不是最后一行？预测写完后再运行。

### 用已存在的 GCC 工具链

在 PowerShell 进入上述目录，先执行 `g++ --version` 和 `gdb --version`。本文实际核验环境为 Windows 上的 GCC 8.3.0（x86_64-posix-seh，Strawberry 分发）和 GDB 8.2.1；这是已存在的小练习工具链，**不是 UE 5.8 的 Windows 编译器推荐**，不用为跟课专门安装这套旧版本。

```powershell
g++ -std=c++17 -Wall -Wextra -Wpedantic -g -O0 -c Training.cpp -o Training.o
if ($LASTEXITCODE -ne 0) { throw 'Training.cpp compile failed' }
g++ -std=c++17 -Wall -Wextra -Wpedantic -g -O0 -c main.cpp -o main.o
if ($LASTEXITCODE -ne 0) { throw 'main.cpp compile failed' }
g++ -g Training.o main.o -o training.exe
if ($LASTEXITCODE -ne 0) { throw 'link failed' }
.\training.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

三条构建命令把编译和链接分开。`-g` 保留调试信息，`-O0` 关闭优化，让第一次单步更易读；这不是发布构建的性能结论。命令失败就停下，不要运行目录里上一次成功留下的 exe。找不到 `g++` 时，先核对安装位置和终端 PATH，不要用“运行了旧 exe”冒充构建通过。

### 如果使用 Visual Studio / MSVC

已有匹配的 MSVC 时，可以在 **x64 Native Tools Command Prompt**（开发者命令提示符，不是普通 PowerShell）中进入同一目录，执行：

```bat
cl /nologo /std:c++17 /EHsc /W4 /Zi /Od /c Training.cpp main.cpp
```

确认这一步成功，再执行下一条，不能忽略错误继续：

```bat
cl /nologo /Zi /Od Training.obj main.obj /Fe:training.exe /link /DEBUG
```

运行 `training.exe`。这是 MSVC 操作路径，本文未在 MSVC 上执行；若采用此路径，记录实际 `cl` 版本，并用 Visual Studio 打开该 exe、加载同次 PDB 后设断点。不要把 GCC 的 `.o`/DWARF 与 MSVC 的 `.obj`/PDB 混用。

### 正常输出应该是什么

在文件创建成功的正常路径，输出应为：

```text
begin
ctor local
value copy=11
caller=10
after ref=11
null borrow
borrow local
ctor owned
borrow owned
source null=true
borrow owned
dtor owned
owner reset
capacity grew=true
first=7
double=14
report=console
bonus=3
file=closed by scope
end body
dtor local
```

`local` 活到 `main` 的作用域结束；`owned` 被 `transferred.reset()` 提前释放。`borrowed` 既没复制对象，也没延长寿命。把它改成 `nullptr` 只是清掉本地借用记录；它不会神奇地清除其他地方保存的悬空指针。

`ofstream` 的例子把“资源”从内存扩展到文件句柄：退出内部花括号时文件关闭，之后再读取写入内容。文件打不开时检查**工作目录与写权限**；程序返回 1 也仍会析构已成功构造的局部对象。

## 5. 移动和容器：地址不是承诺

`std::move(owner)` 本身做的是类型转换，让后续重载有机会选择移动操作；实际转移发生在 `unique_ptr` 的移动构造中。这次只是移动拥有者，`Tracked` 对象没有被重新构造。`unique_ptr` 的这类移动构造保证来源变空；不能把该结果推广成“所有 moved-from 对象都为空”。标准库类型通常保证有效但状态未指定，除非该类型另外给出更强保证；自定义类型要读自己的契约。[unique_ptr 移动构造契约](https://eel.is/c++draft/unique.ptr.single.ctor)

本例刻意禁用 `Tracked` 的复制构造与复制赋值，避免误复制一个被当成独立资源的对象；移动 `unique_ptr<Tracked>` 并不要求移动 `Tracked`。`Score` 则是普通可复制值。它们解决的问题不同，不能用一句“C++ 赋值都是深拷贝”概括。

`vector` 为连续元素申请存储。容量不够时，它可能换一块存储并移动/复制元素，旧元素地址随之失效。本例用 `reserve(oldCapacity + 1)` 确保触发一次重新分配，随后重新取得 `&values[0]`。它验证规则的方法是观察容量与重新取得的元素，**没有解引用失效指针来碰运气**。[vector 容量与失效规则](https://eel.is/c++draft/vector.capacity)

此例仍保留逻辑上的第 0 个元素，所以可以重新按索引取回；如果同时发生删除或重排，索引也未必代表原来的业务对象。固定地址、固定索引和固定业务身份，要分别设计。

## 6. 继承、override 和模板，先读到够用

`Reporter` 有纯虚函数 `Kind()`，通过基类引用 `reporterView` 调用时执行派生类实现。虚析构让以后“经基类指针删除派生对象”的普通 C++ 用法有正确析构链；本例的 reporter 自身是局部值。`final` 表示本类不再允许派生。

三个近似词需要分开：

| 词 | 判断依据 | 练习 |
| --- | --- | --- |
| override / 覆盖 | 派生类提供匹配基类虚函数的实现，包含 `const` 等签名条件 | 去掉 `ConsoleReporter::Kind` 声明与定义中的 `const`，保留 `override`，编译器应拒绝“并未覆盖”的函数 |
| overload / 重载 | 同一名字有不同参数列表，由调用表达式选择 | `Print(int)` 与 `Print(double)` 是重载；不能只靠返回类型区分 |
| name hiding / 名字隐藏 | 派生类同名声明影响基类同名成员的查找 | 基类 `Print(int)`、派生类 `Print(double)` 即使都不是 virtual，也涉及隐藏；必要时用 `using Base::Print` 引入基类重载 |

做完覆盖故障后还原 `const`，不要带着错误进入下一练习。对函数末尾的 `const`，读作“这次成员调用不通过 `this` 修改普通成员”，不等于返回值自动为常量。

`Twice<int>(7)` 先把模板参数 `T` 换成 `int`，再看函数体是否支持 `+`。`std::vector<int>` 是元素为 int 的容器，`std::unique_ptr<Tracked>` 是以 Tracked 为目标类型的拥有者。模板与 C# 泛型有语法相似处，但不是 CLR 泛型运行模型；这里先做到能读“参数类型、约束来自哪些运算、返回什么”。

## 7. 故意制造两类失败，留下根因而不只留下报错

### 7.1 有声明、缺定义：链接失败

先备份正确文件。在练习副本里只从 `Training.cpp` 删除下面**整个定义**，保留头文件声明与 `main.cpp` 的调用：

```cpp
int BonusPoints()
{
    return 3;
}
```

重新执行第 4 节两条 `-c` 命令：两次编译应成功。再链接，应出现与 `BonusPoints()` 有关的 undefined reference；MSVC 对应 unresolved external symbol，常见错误号 LNK2019。报错文本随平台不同，不要求逐字相同。

先问三个问题：调用者有没有声明？目标文件有没有生成？链接输入里有没有**匹配签名**的定义？把定义恢复、重编 `Training.cpp`、重新链接，输出应重新包含 `bonus=3`。仅添加 `#include` 不能补出不存在的函数体。

对照一个编译失败：把调用临时改成 `BonusPoint()`，它没有声明，编译 `main.cpp` 就应失败。还原后重新编译。不要同时制造多种错误，否则你不知道哪一层先阻塞了另一层。

### 7.2 编译链接都成功，调用者却没变：按值传参故障

在另一份练习副本中，把头文件和实现文件里的 `IncrementRef(Score& value)` **同时**改成 `IncrementRef(Score value)`。重新构建，预期 `after ref` 由 11 变成 10。只改一处会得到签名不匹配的另一类问题。

用调试构建运行 GDB：

```text
gdb ./training.exe
(gdb) break IncrementRef
(gdb) run
(gdb) print value.Value
(gdb) print &value
(gdb) bt
(gdb) next
(gdb) print value.Value
(gdb) finish
(gdb) print score.Value
(gdb) print &score
(gdb) continue
(gdb) quit
```

在 `++value.Value` 执行前应看到 10，单步后是 11，但回到 `main` 的 `score.Value` 仍是 10。`bt` 应把 `IncrementRef` 与调用它的 `main` 连起来；两个地址不同揭示了这次操作作用于副本。行号随编辑器换行变化，以函数和语句为准。

把两处 `&` 恢复后重编、重链，再重复断点：函数参数指向调用者对象，返回后 `score.Value` 为 11。记录“源码签名→调用栈→地址/数值→修复后输出”的证据链，别只写“AI 说传引用就行”。

如果停不住，先确认加载的是刚构建的绝对路径、使用 `-g -O0`、断点不是 pending、源码与 exe 来自同一副本。Visual Studio 路径则检查 Modules 中实际模块与 PDB 加载状态，再在 Locals/Watch/Call Stack 看同样的数据。[调试的几层依赖]({{< relref "engine-toolchain/build-debug-02b-how-debugging-works-breakpoints-symbols-runtime.md" >}}) 可以在这里补读。

## 8. 到 UE 之前，保留这条分界线

UE C++ 仍是 C++，包含普通值、模板、构造析构和资源管理；但是 `UObject` 及其派生对象加入了 UE 的创建、反射和 GC 规则。不要把本例的 `std::make_unique<Tracked>()` 机械改成 `std::make_unique<AActor>()`，也不要用普通 `delete` 或通用共享指针去接管 UObject。

普通文件句柄和非 UObject 工具对象仍然可以应用 RAII；Actor/组件怎样创建、持有和结束，在 [第三篇的对象边界]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}}) 里用实际类解释。你不需要为了开始建工程先读完 GC 源码，但要知道这里不能套用同一套删除策略。

## 9. 自测、AI 辅助与验证记录

- [ ] 在运行前写下正常输出预测，解释两个 `dtor` 的位置。
- [ ] 指出值、引用、裸指针、独占拥有者各在哪一行，解释哪条借用何时失效。
- [ ] 亲手得到一次编译失败、一次链接失败，并在恢复后重新构建成功。
- [ ] 在调试器观察按值故障的调用栈、地址与数值变化，再验证修复。
- [ ] 解释为何 vector 示例不能读取旧指针，为何本例的 `unique_ptr` 结论不能直接套到 UObject。

三个小问题，先答再看答案：

1. `Borrow(const Tracked&)` 会延长 `Tracked` 寿命吗？**不会。** 调用者必须保证借用期间对象有效；不要把临时对象绑定到引用的特定延寿规则推广到任意保存/返回引用。
2. `std::move` 后能说任何来源对象都为 null 吗？**不能。** 本例 `unique_ptr` 有具体保证；其他类型看自己的移动契约。
3. 编译两份 `.cpp` 都成功，exe 仍生成不了，优先检查什么？**链接错误指向的符号、签名和链接输入。** 不要先怀疑运行时 GC。

AI 适合解释一个签名、审查借用区间、出输出预测题，或根据**完整构建命令与第一处错误**分析失败。可以让 AI 生成样板，不必逐字手敲；但输出预测、断点、调用栈和“谁负责释放”的解释要亲自完成。要求它给出可验证的反例，再用编译器/调试器检验；AI 自评不是成绩。

| 项目 | 本文交付时的状态 |
| --- | --- |
| 三个文件及输出 | 2026-09-22 从本文代码块提取至隔离临时目录；GCC 8.3.0 按上述 C++17 参数分步编译/链接成功、无警告；运行退出 0，21 行输出与本篇逐行一致，文件读写通过 |
| 普通 C++ 故障与调试 | 缺定义时两次编译成功、链接报 BonusPoints 未定义；未声明调用和错误 override 均编译失败。按值故障实测返回后为 10，修复后为 11；GDB 8.2.1 实际命中 IncrementRef，观察了调用栈、参数/调用者地址和修改前后数值 |
| UE / MSVC / 读者能力 | 未在 UE 或 MSVC 编译；读者能力待本人操作验收 |

资料核对日为 2026-09-22。文中的标准链接是持续维护的 C++ 工作草案入口，用于核对这里使用的所有权/容器规则；示例本身限制在 C++17。官方原则不是对本地工程已通过的证明。

下一步打开 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})，把“编译输入、链接产物、符号匹配”的模型放到 TwinArena 中。
