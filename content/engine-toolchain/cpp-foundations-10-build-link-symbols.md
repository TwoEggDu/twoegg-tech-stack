---
title: "C++ 公共基础 10｜源文件怎样成为正在调试的程序：翻译单元、链接、库与符号"
slug: "cpp-foundations-10-build-link-symbols"
date: "2026-10-25"
description: "用一个三文件 C++17 工程连接声明、定义、翻译单元、目标文件、静态库和调试符号，亲手区分编译失败、链接失败与产物不匹配，为四条源码路线建立共同构建模型。"
tags: ["C++", "源码阅读", "编译链接", "调试", "ABI"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 10
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "源文件怎样成为正在调试的程序"
reading_summary: "沿声明、实现、目标文件、库与实际加载模块，确认正在运行和调试的究竟是哪份代码。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 500
---

在源码里找到一个函数，编辑器能跳到它的声明，调用也通过了编译，最后却报“找不到符号”。再添一个头文件，错误仍然存在。

另一种困惑发生在构建成功以后：刚改过函数，运行结果却没有变化；断点打在眼前这一行，调试器却说没有对应位置。此时继续研究函数内部的逻辑，可能还没有碰到真正的问题。

前三篇分别解释了访问、资源责任和成员调用。这一篇补上它们成为真实程序的过程：**调用者看见声明，链接阶段找到实现，运行时加载产物，调试器再把执行位置映射回源码。** 每一步都需要自己的证据。

> **读这篇之前**：会基本函数、变量和命令行即可开始，签名可回看 [CXX-01]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})。本篇按补写顺序接在 CXX-03 后，编号按知识结构排列。
>
> **材料与范围**：复用 [UFS 综合练习]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) 的多文件与故障分层模型，以及 [构建调试对照]({{< relref "engine-toolchain/build-debug-02-cpp-vs-csharp-debug-and-release.md" >}})、[调试的几层依赖]({{< relref "engine-toolchain/build-debug-02b-how-debugging-works-breakpoints-symbols-runtime.md" >}})。下面使用自有 C++17 小工程；四条路线的工具链、生成器和托管调试细节仍由各自前置负责。

## 1. 先认清程序走过的几道边界

把常见的原生构建过程画成一条链：

```text
main.cpp + 它包含的头文件 ──预处理、编译、汇编──> main.o
Frame.cpp + 它包含的头文件 ──预处理、编译、汇编──> Frame.o
                                                        │
main.o + Frame.o（或包含它的库）+ 所需运行库 ──链接──> 可执行程序
                                                        │
                                    启动 / 装载依赖 ──> 进程中的模块
                                                        │
                            匹配的调试信息 + 对应源码 ──> 源码级调试
```

这是本篇选用工具链的常见分步模型。C++ 语言允许分离翻译，具体文件格式、链接器和加载器属于平台实现；启用链接期优化等配置后，中间产物也可能包含供后续优化的表示，不能把所有 `.o` 都理解成最终机器码。

**翻译单元**是一个源文件，加上经 `#include` 纳入的内容，并去掉条件编译排除的部分。两个 `.cpp` 分别包含同一个头文件，就各自看见一份展开内容。头文件通常通过包含进入这些单元，不会因为文件放在目录里就自动参与构建。[C++17：分离翻译](https://timsong-cpp.github.io/cppwp/n4659/lex.separate)

编辑器跳转依赖它掌握的工程索引；真正编译使用哪组宏、包含目录和生成文件，以构建命令为准。读引擎源码时应记录“哪个目标、哪个平台、哪个配置”，否则搜索到的函数可能根本没有进入当前产物。

还要提前区分三件经常都被叫作“符号”的东西：

| 信息 | 主要用来回答什么 | 本篇怎样观察 |
| --- | --- | --- |
| 目标文件中的符号与引用 | 哪个文件需要某个名字，哪个文件提供定义 | `nm` 查看引用与定义 |
| 动态模块的导出入口 | 其他模块能通过哪些对外名字连接到它 | 先建立模型，实际导出规则交给平台 |
| 调试信息 | 某个执行地址对应哪个函数、源码行和变量位置 | 带 `-g` 构建，进入 GDB |

能链接到函数，不代表有完整行号和局部变量；目标文件里存在一个名字，也不代表最终 DLL 对外导出了它。

## 2. 三个文件，分别承担什么

在独立空目录 `cpp-build-lab` 中保存下面三个完整文件，使用 UTF-8。它们不依赖任何引擎。例子只处理所列的小整数，并在单线程中运行；计数器用于观察跨文件的同一对象。

```text
cpp-build-lab/
  Frame.h
  Frame.cpp
  main.cpp
```

### `Frame.h`：让使用者知道有哪些名字

```cpp
#ifndef CPP_BUILD_LAB_FRAME_H
#define CPP_BUILD_LAB_FRAME_H

namespace lesson
{
inline constexpr int kStep = 2;
extern int completed_calls;

int Advance(int current);

inline bool IsEven(int value)
{
    return value % 2 == 0;
}
}

extern "C" int lesson_step(int current);

#endif
```

### `Frame.cpp`：提供共享状态和普通函数的定义

```cpp
#include "Frame.h"

namespace lesson
{
int completed_calls = 0;

static int AddStep(int current)
{
    return current + kStep;
}

int Advance(int current)
{
    ++completed_calls;
    return AddStep(current);
}
}

extern "C" int lesson_step(int current)
{
    return lesson::Advance(current);
}
```

### `main.cpp`：调用接口并检查结果

```cpp
#include "Frame.h"
#include <cassert>
#include <iostream>

int main()
{
    const int first = lesson::Advance(10);
    const int second = lesson_step(20);

    assert(first == 10 + lesson::kStep);
    assert(second == 20 + lesson::kStep);
    assert(lesson::completed_calls == 2);
    assert(lesson::IsEven(first) && lesson::IsEven(second));

    std::cout << "first=" << first << '\n';
    std::cout << "second=" << second << '\n';
    std::cout << "calls=" << lesson::completed_calls << '\n';
}
```

先解释最基本的一对关系。`int Advance(int current);` 声明一个函数，使调用者能检查参数和返回值；带函数体的版本提供定义。定义也是声明，但声明不一定提供函数体。实现文件包含自己的头文件，使编译器有机会在同一翻译单元检查接口的一致性；如果实现误写成另一组可重载参数，仍可能只是多定义了一个重载，原声明所需的实现仍然缺失。[C++17：声明与定义](https://timsong-cpp.github.io/cppwp/n4659/basic.def)

变量也要分开看：头文件的 `extern int completed_calls;` 不在这里定义对象，`Frame.cpp` 中的 `int completed_calls = 0;` 才提供本例唯一的定义。`main.cpp` 读取的与 `Advance` 修改的是同一计数器。`extern` 不等于“永远只有声明”：带初始化器的 `extern int value = 0;` 就是定义，不能靠加一个关键字消除重复定义问题。

`inline`、文件内 `static` 和 `extern "C"` 分别解决另外三种边界，后面在实际产物上解释。

## 3. 分开执行编译和链接

以下命令使用 **PowerShell + GCC 工具链**，在 `cpp-build-lab` 目录执行。需要 `g++`、`ar`、`nm`、`gdb` 可用；找不到时使用实际安装路径。本文本地验证使用机器已有的 GCC 8.3.0，不把它作为四个项目的工具链推荐。MSVC 的分步编译与 PDB 操作继续参见 UFS 综合练习。

### 3.1 先生成两个目标文件

```powershell
New-Item -ItemType Directory -Force build | Out-Null

g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic -c Frame.cpp -o build/Frame.o
if ($LASTEXITCODE -ne 0) { throw 'Frame.cpp compile failed' }

g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic -c main.cpp -o build/main.o
if ($LASTEXITCODE -ne 0) { throw 'main.cpp compile failed' }
```

这里的 `-c` 表示生成目标文件后停止，不做链接；`-std=c++17` 选语言模式，`-g` 生成调试信息，`-O0` 便于这个小实验逐步观察，三组 `-W` 选项开启相应警告。此时还没有生成本次可执行程序。[GCC：控制输出阶段](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Overall-Options.html)

### 3.2 再把引用与定义接起来

```powershell
g++ -g build/main.o build/Frame.o -o build/frame-demo.exe
if ($LASTEXITCODE -ne 0) { throw 'link failed' }

.\build\frame-demo.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

预期输出为：

```text
first=12
second=22
calls=2
```

`main.o` 需要 `Advance`、`lesson_step` 和共享计数器；`Frame.o` 提供它们。我们通过 C++ 编译驱动器 `g++` 发起链接，由它安排相应链接工具和 C++ 运行库输入。两次编译均成功，仍不足以证明最后一步成功。

命令里的退出码检查很有用：本次构建失败时立即停止，避免下一行误运行上次留下的 exe。只改源码不重建、只重编 `Frame.o` 不重新链接，也都不会把修改自动送进已有的 `frame-demo.exe`。

### 3.3 看见引用和定义

对正常构建的两个目标文件执行：

```powershell
nm -C build/main.o
nm -C build/Frame.o
```

`-C` 把常见 C++ 修饰名还原为较易读的形式。输出还包括标准库等名字，不必逐项学习；先找本例的 `lesson::Advance(int)`、`lesson::completed_calls`、`lesson_step` 和 `lesson::AddStep(int)`。

在本次 Windows x64 GNU 工具链中，调用方的相关引用带 `U`，`Frame.o` 中两个对外函数带 `T`，内部辅助函数 `AddStep` 带小写 `t`。`U` 表示该目标文件尚未提供定义，`T/t` 是代码区符号，大小写在这里对应外部/局部符号的区别。完整类型码和格式以工具文档为准，地址数字不应照抄成跨机器保证。[GNU nm](https://sourceware.org/binutils/docs/binutils/nm.html)

这些观察把问题缩小为：**谁在引用，谁在定义，定义所在文件有没有进入当前链接。** 它比只在编辑器里确认“这个函数确实存在”更接近构建事实。

## 4. 一次只制造一种失败

先保存正常版本，再分别复制到三个独立练习目录。每个副本使用自己的 `build`；每次只做表中那一项修改。前一节带退出码检查的命令会在预期失败处停止。

| 练习 | 唯一改动 | 执行到哪里 | 预期看到什么 | 怎样恢复 |
| --- | --- | --- | --- | --- |
| A：调用没有声明 | 在 `main.cpp` 把 `lesson::Advance(10)` 拼成 `lesson::Advanc(10)` | 编译 `main.cpp` | 名字不是 namespace 成员等编译诊断 | 恢复名字，再重新编译和链接 |
| B：定义没进链接 | 源码不变，两次编译照常成功；链接命令只保留 `build/main.o` | 链接 | 本例函数或计数器的 undefined reference | 把 `build/Frame.o` 加回链接 |
| C：头文件产生重复定义 | 只把 `inline bool IsEven` 改成 `bool IsEven`，保持其他代码不变 | 两次编译后链接 | `lesson::IsEven(int)` 的 multiple definition | 恢复 inline，重新编译两个单元再链接 |

B 的故障命令明确写成：

```powershell
g++ -g build/main.o -o build/missing-definition.exe
if ($LASTEXITCODE -ne 0) { throw 'expected link failure: Frame.o is missing' }
```

不要运行故障输出文件，也不要为解决 B 再加一次 `#include "Frame.h"`：调用者已经知道声明，缺的是链接输入中的实现。另一种常见原因是条件编译排除了定义，或命名空间、参数类型、成员 const、语言链接等没有对应；不能只凭函数短名字相同就认定匹配。

也不要以 `#include "Frame.cpp"` 修补工程配置。它会把实现文本并进当前翻译单元；如果原 `.cpp` 又被独立编译并链接，还可能得到重复定义。项目有意采用合并构建时应按它的构建规则理解，这与随手包含实现文件是两回事。

这三个用例的报错原文属于工具实现；其他编译器可能换成不同措辞和错误编号。要记录失败阶段、相关名字、完整命令与第一处有效诊断，不能把某个固定错误号当作语言规则。

## 5. 头文件、ODR 与几个常见关键字

### 5.1 包含保护只管同一个翻译单元

`Frame.h` 的 `#ifndef` / `#define` 让同一轮预处理重复包含它时跳过已有内容。编译 `main.cpp` 和 `Frame.cpp` 是两轮独立处理：两边依然各自出现头文件定义。

C 中去掉 `inline` 后，一个普通外部函数的定义同时进入两个单元，违反相应定义规则。本地链接器报告了重复定义；保护宏没有理由把第二个翻译单元里的函数删掉。

**ODR（One Definition Rule，单一定义规则）**并不是“全工程所有名字只能出现一次”。同一翻译单元不能重复定义同一个变量、函数、类、枚举类型或模板；跨单元的普通非 inline 函数/变量与类、模板、inline 实体有不同约束。允许多处定义的情形仍要求满足一致性条件，包括对应 token 和名字查找等要求。某些违反 ODR 的程序不要求实现给出诊断，链接成功不能充当正确性证明。[C++17：ODR](https://timsong-cpp.github.io/cppwp/n4659/basic.def.odr)

因此，不能用不同宏让两个翻译单元看到同一个外部 inline 函数的不同函数体，再指望链接器替你挑一个正确版本。

### 5.2 inline 同时关系到定义规则

正常版本中，`IsEven` 的相同 inline 定义放在共享头文件里，供包含它的单元使用；`inline constexpr int kStep` 则使用了 C++17 的 inline 变量能力。满足相关条件时，具有外部链接的 inline 函数/变量可在多个单元定义，语义上仍是同一实体。

这里的 `inline` **不保证机器码一定把函数体展开到调用点**。编译器可以不展开带 inline 的函数，也可以优化展开未写 inline 的函数。头文件能否合法放置定义与实际调用是否被优化，要分开判断。[C++17：inline](https://timsong-cpp.github.io/cppwp/n4659/dcl.inline)

常见类定义可以放头文件；C++17 中类定义内部定义的函数隐式 inline。模板也常把定义放在头文件以满足实例化可见性，但显式实例化等安排另有边界，留到 CXX-07。头文件并非只能放声明，也并非适合任意函数体。

### 5.3 static 要结合出现位置理解

`Frame.cpp` 里的命名空间作用域函数 `static int AddStep(...)` 具有内部链接：这个名字用于本翻译单元内的辅助实现。另一 `.cpp` 中独立声明一个同名普通外部函数，不能借此访问到它。未命名 namespace 也是组织文件内辅助实体的常用方式。[C++17：名字的链接属性](https://timsong-cpp.github.io/cppwp/n4659/basic.link)

如果把命名空间作用域的 `static int counter = 0;` 放进头文件，每个包含它的翻译单元会得到自己的计数器；它不再等价于本例 `extern` 声明加一处定义的共享计数。为消除链接报错而加 static，可能悄悄改变状态归属。

另外两处常见语法不能照搬这个解释：函数内部的 static 局部对象具有静态存储期；类里的 static 成员表示它不属于每个实例各一份。读代码时先确定它位于命名空间、函数还是类中，再谈链接与寿命。

## 6. 把同一份实现放进库

### 6.1 静态库改变实现的组织方式

回到正常工程，把已经生成的 `Frame.o` 放进归档，再让 `main.o` 从这个库获得所需定义：

```powershell
ar rcs build/libframe.a build/Frame.o
if ($LASTEXITCODE -ne 0) { throw 'archive failed' }

ar t build/libframe.a
if ($LASTEXITCODE -ne 0) { throw 'archive listing failed' }

g++ -g build/main.o -Lbuild -lframe -o build/frame-from-lib.exe
if ($LASTEXITCODE -ne 0) { throw 'library link failed' }

.\build\frame-from-lib.exe
if ($LASTEXITCODE -ne 0) { throw 'library run failed' }
```

`ar t` 应列出 `Frame.o`，程序仍输出同样三行。`ar` 把成员文件组织到归档中，并没有替它们完成最终程序的全部引用解析。[GNU ar](https://sourceware.org/binutils/docs/binutils/ar.html)

这次工程只在该目录放了 `libframe.a`；`-Lbuild` 加入库搜索目录，`-lframe` 按工具链规则查找 frame 库。GNU 常见静态库链接会依据当前未解决引用提取所需归档成员，所以这里把使用它的 `main.o` 放在 `-lframe` 前。更复杂的循环依赖、强制保留和平台差异再按链接器规则处理。[GCC：链接选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Link-Options.html)

顺带区分三个经常配错的位置：`-I` 增加头文件搜索目录，`-L` 增加链接时的库搜索目录，`-l` 指定要查找的库。增加 `-I` 不能让未链接的库自动加入程序；`-L` 也不能代替运行时的动态模块搜索配置。

静态链接本例 frame 实现后，运行这个 exe 不再需要读取 `libframe.a`。这不等于整个 exe 没有其他动态运行库依赖，也不保证归档每个成员都被装进程序。

### 6.2 动态库多了一次运行时连接

静态库把实现作为链接输入提供给最终模块；动态库让实现保留在单独模块中，程序启动或运行期间再装载并连接它。于是“构建时能找到”和“运行时加载到了正确版本”成为两个问题。

Windows 工程中的 `.lib` 可能是静态库，也可能是连接 DLL 的导入库；后者不能替代运行时的 DLL。动态模块还要有约定的对外入口。以 Windows 为例，可以通过 `__declspec(dllexport)` 或 `.def` 文件等方式指定导出，用 `dumpbin /exports` 检查导出表。**普通 C++ 外部链接并不自动等于 DLL 导出承诺。**[Microsoft：从 DLL 导出](https://learn.microsoft.com/en-us/cpp/build/exporting-from-a-dll?view=msvc-170)

本篇没有制作 DLL/so，也没有实测加载路径、热重载或跨编译器兼容。进入具体路线后，再核对目标架构、构建配置、实际加载模块路径和导出表；文件名相同不足以证明模块相同。

### 6.3 extern "C" 解决语言链接，不包办 ABI

本例 `Advance` 是普通 C++ 函数，而 `lesson_step` 使用 C 语言链接，并在内部转调前者。去掉 `nm` 的 `-C` 再观察 `Frame.o`：本次工具链给 `Advance` 生成 C++ 修饰名，`lesson_step` 则显示为这个 C 链接名字。名字修饰的精确拼写属于实现，不能推断所有平台的 C 名字都完全没有前后缀。

`extern "C"` 指定语言链接，用于与相应 C 接口约定连接；它没有把函数体改成 C，也没有自动建立动态库导出。声明与定义必须遵循一致的语言链接安排。[C++17：语言链接](https://timsong-cpp.github.io/cppwp/n4659/dcl.link)

本例仍只用 C++ 编译器构建，头文件含 namespace 和 inline 变量，也不是可直接交给 C 编译器的公共 C 头文件。这个包装展示的是入口形式，跨语言调用没有在本篇验证。

**ABI（应用二进制接口）**还涉及目标架构、调用约定、参数与返回值表示、布局，以及跨模块资源释放和异常策略等契约。仅给接收 `std::string` 的接口加上 `extern "C"`，不会使字符串对象布局或分配器自动跨工具链兼容。跟读 HybridCLR/IL2CPP 的桥接代码时，应继续核对这些契约，详细讨论见 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})。

## 7. 确认正在调试的就是这次产物

### 7.1 沿同一个函数走一次断点

先恢复全部正常代码，完整执行第 3 节，再在工程根目录启动：

```powershell
gdb .\build\frame-demo.exe
```

在 GDB 中依次输入；`(gdb)` 是提示符，不需要复制：

```text
(gdb) info files
(gdb) break lesson::Advance
(gdb) run
(gdb) info source
(gdb) print current
(gdb) print lesson::completed_calls
(gdb) bt
(gdb) next
(gdb) print lesson::completed_calls
(gdb) finish
(gdb) disable 1
(gdb) continue
(gdb) quit
```

这次会在第一个 `Advance(10)` 停住。先核对 `info files` 中的可执行文件路径确实是本次 `build/frame-demo.exe`；`info source` 应对应本副本的 `Frame.cpp`，`bt` 中能沿 `Advance` 回到 `main`。GDB 的文件与符号选择有明确入口，启动了另一目录的同名 exe 时，眼前打开的源码不会替你更换调试对象。[GDB：指定文件](https://sourceware.org/gdb/current/onlinedocs/gdb.html/Files.html)

在本次 `-O0` 验证中，首次停点位于 `++completed_calls`：参数为 10，计数为 0；执行一次 `next` 后计数为 1；`finish` 返回 `main` 并报告返回值 12。这里不在 `Advance` 栈帧里读取只属于 `main` 的 `first`，也不在赋值完成前假定它已经可用。随后禁用第一个断点，让第二次调用继续执行，最终应看到完整三行输出并正常退出。[GDB：继续与单步](https://sourceware.org/gdb/current/onlinedocs/gdb.html/Continuing-and-Stepping.html)

这次观察建立了“正在执行的函数 → 当前参数与状态 → 调用者 → 源码文件”的联系。它没有验证引擎中的任何调试配置。

### 7.2 符号、源码和优化分别核对

调试信息需要在编译相关源文件时生成。若目标文件当初没有生成所需信息，只在最终链接命令补 `-g`，不能还原已经丢失的源码行和局部变量信息。

`-g` 与优化选项是不同维度。GCC 允许它与 `-O` 组合；优化可能让变量没有独立存储、语句重排、调用内联，使源码级单步难以按书写顺序理解。选择 `-O0 -g` 是本篇便于观察的设置，不是所有工程的最佳调试配置，也不能用这次运行推断发布性能。[GCC：调试选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Debugging-Options.html)

不同工具链保存调试信息的方式不同。本实验使用 GNU 工具链的调试信息；在 MSVC / Visual Studio 路线上，要确认加载的是与 exe/DLL **精确匹配**的 PDB，而非仅名称相同的文件。Modules 窗口可用于核对模块路径和符号加载状态；源码也应来自对应版本。把源文件搜索路径改对，只解决“去哪里找文本”，不会把旧产物变成新产物。[Microsoft：符号与源文件](https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=visualstudio)

遇到断点问题，可以按证据由外向内排：

1. **构建输入**：修改的文件进入了哪个目标？配置、宏、生成文件和源码版本是否对应？
2. **执行对象**：启动/附加的是哪个进程，实际加载模块在哪，是否来自本次构建？
3. **映射材料**：当前模块的调试信息是否生成、匹配并被加载，源码是否是对应版本？
4. **实际执行**：这条路径是否真的运行，这一行是否有可执行位置，优化是否改变了观察方式？

原生函数断点成立，也不等于托管源码断点成立。C#、IL、AOT 生成代码和原生地址之间还有额外映射与调试协作，继续读构建调试专题，不把本例的 GDB 能力直接当成托管调试能力。

## 8. 把这条链带回四条源码路线

公共课建立同一组问题，专项前置负责给出项目中的实际答案：

| 路线 | 带着本篇追什么 | 继续阅读 |
| --- | --- | --- |
| Piccolo | 当前目标使用哪些配置与生成文件；实现进了哪个目标，启动程序与源码基线是否一致 | [构建与调试基线]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}}) |
| Unreal | 工程目标、模块与生成步骤怎样组织编译输入；实际加载模块及符号是否对应当前构建 | [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})，需要综合操作时回做 [UFS 首课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) |
| HybridCLR | 当前跟踪的是托管程序集、解释器入口还是原生运行时；方法桥接所依赖的版本和 ABI 是否对应 | [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) |
| IL2CPP | 生成代码、原生模块和 metadata 各由谁产生、怎样配套；当前符号解释的是哪一层 | [IL2CPP 运行时产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) |

读到生成文件时，还要追它的生产步骤：编译器能消费生成结果，不代表会自己完成项目的代码生成。构建依赖怎样保证“先生成、再编译”，以及宏与生成器的区别，后续 CXX-08 再展开。

这张表是阅读任务，不是四个项目已经完成构建验证的记录。C++17 教学程序通过，不能推出它们使用相同标准、库格式、调试器或构建命令。

## 9. 自测与验证边界

读完后，先不打开命令记录，回答以下问题：

1. `main.cpp` 已经包含 `Frame.h`，为什么仍可能找不到 `Advance` 的实现？
2. 包含保护为什么挡不住两个翻译单元里的普通函数重复定义？
3. 把共享计数器改成头文件 static 变量，为什么可能改变业务行为？
4. `.a`、导入库、DLL、调试信息分别在哪一步被使用？
5. `extern "C"`、导出声明和匹配 PDB 为什么不能互相替代？
6. 改完 `Frame.cpp` 只重新编译目标文件，为什么运行旧 exe 仍是原结果？

本文资料核对日为 2026-10-25，语言规则采用 C++17 工作草案 N4659。验证使用 Windows x64、GCC 8.3.0（`x86_64-posix-seh`）、GDB 8.2.1、GNU Binutils 2.32；不定义 `NDEBUG`，保留断言。正常与失败变体均从正文代码提取，分别放在独立目录。

| 用例 | 本次结果 | 验证内容 |
| --- | --- | --- |
| 正常分步编译、链接与运行 | 退出码均为 0，所列编译警告选项下无警告 | 三行输出与正文一致，全部断言通过 |
| 未声明调用 | 编译退出码为 1 | 诊断指出 `Advanc` 不是 lesson 的成员 |
| 漏链 `Frame.o` | 两次编译成功，链接退出码为 1 | 出现 undefined reference，指向缺少的函数/计数器定义 |
| 头文件普通函数重复定义 | 两次编译成功，链接退出码为 1 | 出现 `IsEven(int)` 的 multiple definition |
| 三个副本恢复后重建 | 编译、链接、运行均成功 | 每个副本恢复相同三行输出，断言通过 |
| 符号与静态库 | `nm`、`ar` 和库链接均成功 | 核对 U/T/t 及 C/C++ 名字；归档含 `Frame.o`，链接后输出一致 |
| GDB 断点与状态 | 正常退出，命令无错误 | 核对 exe 与源码路径，命中 Advance，参数 10、计数 0→1、返回 12，调用栈回到 main |

本篇不包含四个引擎/运行时的工程构建、MSVC GUI 操作、动态库加载、跨语言调用、跨平台 ABI 或性能测试。实际工具链命令和故障诊断属于这次实验，语言规则和项目验证需要分别阅读。

按当前补写顺序，下一篇是 **CXX-05：行为怎样被保存并稍后调用**，继续讲函数指针、Lambda、捕获与回调寿命；完整知识目录见 [C++ 公共基础入口]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**先确认代码进入了哪份产物，再用匹配的调试信息解释它正在怎样执行。**
