---
title: "CoreCLR 实现分析｜异常处理：两遍扫描模型与 SEH 集成"
slug: "coreclr-exception-handling-two-pass-seh-integration"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解异常处理的完整实现：两遍扫描模型的查找与展开阶段、Windows SEH 框架与 managed exception 的集成机制、PAL_TRY/PAL_EXCEPT 对非 Windows 平台的模拟、Linux 信号处理与 libunwind 栈展开、Exception 对象与 ExceptionDispatchInfo 的栈信息保留、filter clause 在 Pass 1 中的执行时机、零成本异常模型的性能特征，以及与 IL2CPP / HybridCLR / LeanCLR 的异常处理策略对比。"
weight: 45
featured: false
tags:
  - CoreCLR
  - CLR
  - Exception
  - SEH
  - ErrorHandling
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> 异常处理是 runtime 中最需要和操作系统深度配合的子系统——CoreCLR 在 Windows 上嵌入 SEH 框架，在 Linux 上接管信号处理，目标只有一个：让 managed exception 在 native 栈帧上正确传播。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 6 篇。

B5 讲了 CoreCLR 的 GC 子系统——分代式精确回收、Workstation vs Server 模式、Pinned Object Heap。GC 在运行时保证对象的生命周期正确性。这篇讲的是另一种"控制流异常"——当程序抛出异常时，runtime 怎样沿着调用栈找到 handler、执行 finally、恢复执行。

## 异常处理在 CoreCLR 中的位置

ECMA-A4 已经从规范层面定义了 CLI 的异常处理模型：四种子句类型（catch / filter / finally / fault）、两遍扫描机制、protected region 在 metadata 中的表示。那篇讲的是"规范说了什么"，这篇讲的是"CoreCLR 怎么实现的"。

CoreCLR 的异常处理涉及三个层次：

**VM 层（`src/coreclr/vm/`）** — 异常处理的核心逻辑。`exceptionhandling.cpp` 实现两遍扫描的调度逻辑，`exinfo.cpp` 维护异常追踪信息，`excep.cpp` 处理 managed 与 unmanaged 异常的转换。

**JIT 层（`src/coreclr/jit/`）** — RyuJIT 在编译每个方法时，为 try/catch/finally 区域生成对应的 unwind info 和 exception handling table。这些表在运行时被 VM 层查询。

**PAL 层（`src/coreclr/pal/`）** — Platform Abstraction Layer。在非 Windows 平台上模拟 SEH 语义，把 Unix 信号转换为异常，提供 `PAL_TRY` / `PAL_EXCEPT` / `PAL_FINALLY` 宏。

三个层次的协作关系可以概括为：JIT 在编译时生成元数据（哪些 IL 偏移范围是 protected region、handler 在哪里），VM 在运行时利用这些元数据执行两遍扫描算法，PAL 在底层提供跨平台的栈展开和信号处理基础设施。

## 两遍扫描模型

ECMA-335 定义了异常处理的两遍扫描语义，CoreCLR 严格遵循这个模型。核心实现在 `ExceptionTracker::ProcessManagedCallFrame` 和 `ProcessOSExceptionNotification` 中。

### 第一遍（Pass 1）：查找匹配的 handler

异常抛出后，runtime 从抛出点的栈帧开始，沿调用栈向上遍历。对每一帧：

1. 根据当前指令指针（IP）查询该方法的 exception handling table，确定 IP 是否落在某个 try block 的 protected region 内
2. 如果在 protected region 内，依次检查关联的 catch 子句——异常对象的类型是否与 catch 声明的类型匹配（包括继承关系）
3. 如果遇到 filter 子句，在此刻执行 filter 表达式（这一点很关键，后面单独讨论）
4. 找到第一个匹配的 handler 后，记录该 handler 的位置信息，Pass 1 结束

Pass 1 的关键特征是**不修改栈帧**。它只做查找，不执行任何 finally、不销毁任何栈帧。调用栈在 Pass 1 结束时保持抛出异常前的状态。

如果 Pass 1 遍历完整个调用栈都没找到匹配的 handler，runtime 触发未处理异常（unhandled exception）流程——通常意味着进程终止。这个决定在栈展开之前做出，意味着 finally 子句是否执行取决于 runtime 的策略：CoreCLR 在未处理异常时默认不执行 finally 子句（fail fast），这是一个有意的设计选择——如果程序处于未知状态，继续执行 finally 中的资源清理代码可能造成更大的问题。

### 第二遍（Pass 2）：执行 finally/fault 并跳转到 handler

Pass 1 确认了 handler 的位置后，Pass 2 从抛出点重新开始栈展开：

1. 从抛出点所在帧开始，向上展开（unwind）到 handler 所在帧
2. 展开过程中，逐帧检查途经的 finally 和 fault 子句
3. 遇到 finally 子句就执行它——这意味着 finally 的执行发生在栈展开过程中
4. 到达 handler 所在帧后，把控制权交给 catch handler，正常执行继续

```
线程调用栈（从下到上）：
┌──────────────────────────────┐
│  Main()                      │
│    try { A(); }              │
│    catch (Exception) { ... } │ ← Pass 1 在这里找到匹配
├──────────────────────────────┤
│  A()                         │
│    try { B(); }              │
│    finally { Cleanup(); }    │ ← Pass 2 展开时执行
├──────────────────────────────┤
│  B()                         │
│    throw new Exception();    │ ← 抛出点
└──────────────────────────────┘

Pass 1：B → A（finally 不是 catch，跳过）→ Main（catch 匹配）→ 记录位置
Pass 2：B → A（执行 finally: Cleanup()）→ Main（进入 catch handler）
```

### 为什么是两遍而不是一遍

一个自然的问题：为什么不在一遍扫描中同时查找 handler 和执行 finally？

如果采用单遍扫描策略——向上遍历栈帧，遇到 finally 就执行，遇到匹配的 catch 就进入——会产生一个语义问题：如果执行了若干层的 finally 之后，发现整个调用栈上没有匹配的 handler，栈帧已经被展开了，finally 也已经执行了，程序处于一个不可回退的状态。

两遍扫描保证了一个关键不变量：**在确认异常能被处理之前，不做任何不可逆操作**。Pass 1 是纯读操作——只查找不修改。只有确认了 handler 存在之后，Pass 2 才开始执行 finally 和展开栈帧。

这个设计还有一个实际好处：调试器可以在 Pass 1 结束、Pass 2 开始之前介入（first-chance exception notification）。此时调用栈完整保留，调试器能看到异常抛出时的完整上下文。如果是单遍扫描，到调试器介入时栈帧可能已经被部分销毁了。

## 与 OS 异常机制的集成

CoreCLR 不自己实现栈展开——它借助操作系统的异常处理基础设施。这是 CoreCLR 异常处理中工程复杂度最高的部分。

### Windows SEH

在 Windows 上，CoreCLR 的 managed exception 通过 Structured Exception Handling（SEH）框架传播。

SEH 是 Windows 提供的 OS 级异常处理机制。每个线程维护一个异常处理链（exception handler chain），当异常发生时，OS 沿着这条链依次调用 handler，直到某个 handler 声明自己处理了这个异常。

CoreCLR 的做法是把自己的异常处理逻辑注册为 SEH handler：

```
managed 代码抛异常
  → COMPlusThrow / RaiseException
  → OS 触发 SEH 分派
  → CoreCLR 注册的 SEH handler 被调用
  → handler 内部执行两遍扫描逻辑
  → 找到 managed catch handler
  → 通过 SEH 的 unwind 机制展开栈帧
  → 控制权到达 catch handler
```

JIT 在编译 try/catch 方法时，为每个方法生成 `RUNTIME_FUNCTION` 和 `UNWIND_INFO` 结构——这是 Windows x64 ABI 要求的栈展开元数据。OS 的异常分派器利用这些结构遍历调用栈上的每一帧。CoreCLR 提供 personality routine（`ProcessCLRException`），OS 在遍历到每一帧时调用它，CoreCLR 在这个回调中执行自己的 handler 查找和 finally 执行逻辑。

这种集成方式的优势是：managed 异常和 native 异常使用同一套栈展开机制。当 managed 代码通过 P/Invoke 调用 native 代码、native 代码又回调 managed 代码时，异常可以正确地穿越 managed/native 边界。

### PAL_TRY / PAL_EXCEPT：非 Windows 平台的模拟

CoreCLR 的 runtime 代码（C++）在 Windows 上直接使用 `__try` / `__except` / `__finally`——这是 MSVC 编译器提供的 SEH 扩展。在非 Windows 平台上，GCC/Clang 不支持 SEH 语法。

PAL（Platform Abstraction Layer）提供了一组宏来模拟 SEH 语义：

- `PAL_TRY` — 对应 `__try`，在内部使用 `setjmp` 保存执行上下文
- `PAL_EXCEPT` — 对应 `__except`，提供异常过滤和处理
- `PAL_FINALLY` — 对应 `__finally`，保证清理代码执行
- `PAL_ENDTRY` — 标记 try block 的结束

这些宏的底层实现基于 C++ 异常或 `setjmp`/`longjmp`。PAL 层的目标不是完美复制 SEH 的全部能力，而是让 CoreCLR 的 runtime 代码能在 Unix 平台上编译和运行，保持异常处理的核心语义。

## Linux / macOS 平台

在非 Windows 平台上，CoreCLR 面对的是完全不同的 OS 异常机制。

### libunwind 实现栈展开

Linux 和 macOS 没有 SEH，栈展开依赖 DWARF unwind info 和 libunwind 库。

JIT 在非 Windows 平台上生成的方法需要提供 DWARF 格式的 `.eh_frame` 信息（而不是 Windows 的 `UNWIND_INFO`）。libunwind 库读取这些信息来遍历调用栈上的帧。

CoreCLR 在 Linux 上的异常分派流程：

```
managed 代码抛异常
  → 构造异常对象
  → 调用 PAL_CppRethrow / RaiseException 的 Unix 实现
  → 使用 libunwind 遍历栈帧
  → 在每一帧调用 CoreCLR 的 handler 查找逻辑
  → 执行两遍扫描
  → 栈展开到目标 handler
```

### 信号处理：硬件异常转 managed 异常

某些 managed 异常不是由 `throw` 指令触发的，而是由硬件异常引起。最典型的例子：

- **空引用访问** — 解引用 null 指针触发 SIGSEGV（Linux）或 SIGBUS（macOS）
- **除零操作** — 整数除零触发 SIGFPE
- **栈溢出** — 栈空间耗尽触发 SIGSEGV（访问 guard page）

CoreCLR 注册这些信号的 handler（`PAL_initialize` 阶段设置 `sigaction`）。当信号到达时，CoreCLR 的信号处理函数检查信号发生的上下文：

1. 确认信号发生在 managed 代码执行期间（通过检查指令指针是否在 JIT 生成的代码范围内）
2. 如果是，把硬件异常转换为对应的 managed 异常对象——SIGSEGV 变成 `NullReferenceException`，SIGFPE 变成 `DivideByZeroException`
3. 修改信号上下文的指令指针，重定向到异常分派入口
4. 信号处理函数返回后，执行流自动进入异常分派逻辑

这种"信号 → managed 异常"的转换让 JIT 可以省掉显式的 null check 指令。对于引用类型的字段访问或方法调用，JIT 不生成 `if (obj == null) throw NullReferenceException` 的检查代码——直接访问，如果对象是 null，硬件会产生 SIGSEGV，runtime 的信号处理函数把它转换为 `NullReferenceException`。这被称为**隐式 null check**，是一种利用硬件保护的性能优化——在绝大多数情况下对象不是 null，省掉检查指令消除了热路径上的一次分支。

## Exception 对象

### System.Exception 继承层次

所有 managed 异常都是 `System.Exception` 的实例。CoreCLR 预定义了一套异常类型层次：

```
System.Exception
  ├── System.SystemException
  │     ├── System.NullReferenceException
  │     ├── System.InvalidOperationException
  │     ├── System.IndexOutOfRangeException
  │     ├── System.DivideByZeroException
  │     ├── System.StackOverflowException
  │     ├── System.OutOfMemoryException
  │     └── ...
  └── System.ApplicationException（不推荐使用）
```

`StackOverflowException` 和 `OutOfMemoryException` 是特殊的——它们不能被 catch（CoreCLR 对这两种异常做了特殊处理，默认触发 fail fast）。这是因为栈溢出时没有栈空间来执行 catch handler，内存不足时分配异常对象本身都可能失败。CoreCLR 为这些情况预分配了异常对象实例，避免在异常路径上再做内存分配。

### ExceptionDispatchInfo：保留原始栈信息的重新抛出

C# 中直接 `throw;`（不带操作数的 rethrow）会保留原始的栈信息。但如果把异常存到变量里再抛出（`throw ex;`），栈信息会被重置到当前位置——原始抛出点的调用栈丢失了。

`ExceptionDispatchInfo` 解决这个问题。它在捕获异常时保存完整的栈追踪信息（包括 Watson bucket 信息），之后通过 `ExceptionDispatchInfo.Throw()` 重新抛出时，恢复原始栈信息：

```csharp
ExceptionDispatchInfo edi = null;
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    edi = ExceptionDispatchInfo.Capture(ex);
}

// 稍后在另一个上下文中重新抛出，保留原始栈
if (edi != null)
    edi.Throw();  // 栈追踪包含 RiskyOperation 的原始抛出点
```

在 CoreCLR 内部，`ExceptionDispatchInfo.Capture` 调用 `Exception.CaptureDispatchState`，保存当前的 `_stackTrace` 和 `_remoteStackTraceString`。`Throw` 时通过 `Exception.RestoreDispatchState` 恢复这些信息，然后执行常规的异常抛出流程。

这个机制对 `async/await` 至关重要。当 async 方法中的异常需要跨越 await 边界传播到调用方时，runtime 使用 `ExceptionDispatchInfo` 保证异常的栈追踪信息不会在跨线程传播过程中丢失。

## Filter Clause

### catch 前的 filter（when 关键字）

C# 6.0 引入的异常过滤器（exception filter）对应 ECMA-335 的 filter clause：

```csharp
try
{
    ProcessRequest(request);
}
catch (HttpException ex) when (ex.StatusCode == 404)
{
    HandleNotFound();
}
catch (HttpException ex) when (ex.StatusCode >= 500)
{
    HandleServerError();
}
```

`when` 子句编译为 filter clause。filter 是一段可执行代码，返回布尔值——true 表示匹配，false 表示不匹配。

### filter 在 Pass 1 中执行

filter 的执行时机是关键：**它在 Pass 1（查找阶段）中执行，此时栈帧尚未被展开**。

这意味着 filter 表达式执行时：
- 调用栈完整保留——从抛出点到 filter 所在帧之间的所有栈帧都存在
- 还没有任何 finally 被执行
- 异常对象已经创建，但栈展开尚未开始

这个时机提供了一个调试和日志的窗口：filter 可以在栈展开之前检查异常状态、记录日志，甚至不匹配（返回 false）而让异常继续向上传播。

```csharp
catch (Exception ex) when (LogAndReturn(ex))
{
    // 如果 LogAndReturn 返回 true，进入这里
    // 此时 finally 已经在 Pass 2 中执行过了
}

bool LogAndReturn(Exception ex)
{
    // 此时调用栈完整——可以在 Pass 1 中获取完整的诊断信息
    Logger.Error(ex);
    return true;
}
```

filter 表达式本身如果抛出异常，这个异常会被 runtime 吞掉，filter 视为返回 false（不匹配）。这个行为是 ECMA-335 规范规定的——filter 不应该影响异常传播的正常流程。

## 性能影响

### try 块的零运行时开销

CoreCLR 采用**零成本异常模型（zero-cost exception model）**。进入和退出 try block 时，JIT 生成的 native code 不执行任何额外指令——没有 handler 注册，没有栈帧标记，没有运行时数据结构的维护。

这通过元数据查表实现：JIT 在编译时生成 exception handling table，记录每个 protected region 的 IL 偏移范围和关联的 handler。运行时抛异常时，通过当前 IP 查询这张表来确定所在的 protected region——这是一个 O(log n) 的查表操作，只在异常发生时执行。

```
正常执行路径（无异常）：
  try { A(); B(); C(); }  →  JIT 生成的 native code 和没有 try 时完全相同
                              没有额外的分支、没有 handler 注册、零开销

异常路径：
  throw → 查 exception table → 找到 handler → 栈展开 → 进入 handler
  全部开销集中在这条路径上
```

这意味着用 try/catch 保护代码不会影响正常路径的性能。性能代价完全在 throw 端：

### throw 的代价

throw 的开销来自几个方面：

- **异常对象构造** — 创建 `Exception` 对象、捕获栈追踪（`StackTrace`）。栈追踪的捕获需要遍历当前调用栈的所有帧，这是 throw 开销的主要来源之一
- **Pass 1 栈遍历** — 沿调用栈向上查找 handler，每一帧都要查询 exception table
- **Pass 2 栈展开** — 执行 finally 子句、销毁栈帧。如果途经的 finally 数量多，开销相应增加
- **OS 交互** — 在 Windows 上涉及 SEH 分派，在 Linux 上涉及信号处理和 libunwind 调用

这些开销使得 throw 比普通的函数返回慢几个数量级。CoreCLR 的设计假设是：异常是异常情况，不应该出现在正常的控制流中。用异常做流程控制（比如用 `try { int.Parse(...) } catch { }` 替代 `int.TryParse(...)`）是对这个设计假设的滥用，性能会显著退化。

## 与其他 runtime 的异常处理对比

四种 runtime 面对同一个规范（ECMA-335 异常处理语义），选择了截然不同的实现策略。差异的根源是执行模型不同——JIT 产出 native code 需要和 OS 的栈展开机制集成，AOT 翻译成 C++ 需要映射到 C++ 的异常模型，解释器在自己的循环内处理一切。

| 维度 | CoreCLR | IL2CPP | HybridCLR | LeanCLR |
|------|---------|--------|-----------|---------|
| **异常传播机制** | Windows SEH / Linux libunwind | C++ exception 或 setjmp/longjmp | 解释器内部 ExceptionFlowInfo | 解释器内部 RtInterpExceptionClause |
| **扫描模型** | 两遍扫描（Pass 1 查找 + Pass 2 展开） | 单遍（C++ 异常的展开即查找） | 两遍（解释器内部模拟） | 两遍（解释器内部模拟） |
| **栈展开** | OS 提供（UNWIND_INFO / DWARF .eh_frame） | C++ 运行库提供 | 解释器循环内切换执行状态 | 解释器循环内回退 try 深度 |
| **try 开销** | 零（元数据查表） | 接近零（C++ try 的零成本模型）或有开销（setjmp 模式） | 低（解释器追踪 protected region） | 低（解释器追踪 clause 列表） |
| **throw 开销** | 高（OS 异常分派 + 栈遍历） | 中（C++ 异常分派）或低（longjmp） | 低（解释器内部状态切换） | 低（解释器内部状态切换） |
| **硬件异常转换** | 支持（信号 → managed 异常） | 不需要（AOT 生成显式 null check） | 依赖 IL2CPP 的 null check | 不需要（解释器内显式检查） |
| **跨 managed/native 边界** | 通过 SEH 框架统一 | 不存在边界（全是 native） | AOT ↔ 解释器需桥接 | 解释器 ↔ internal call 需桥接 |

几个值得展开的差异：

**CoreCLR vs IL2CPP 的扫描模型。** CoreCLR 严格执行两遍扫描。IL2CPP 把 CIL 的 try/catch 翻译成 C++ 的 `try`/`catch`，异常传播交给 C++ 运行库处理。C++ 异常是单遍模型——展开和查找同时进行。在不支持 C++ 异常的平台上（如某些 WebAssembly 配置），IL2CPP 退回到 `setjmp`/`longjmp` 方案，用 `setjmp` 在进入 try 时保存上下文，异常发生时 `longjmp` 跳回最近的保存点。setjmp 方案的特点是 try 入口有开销（保存寄存器上下文），但 throw 很轻量（直接跳转）。

**解释器的优势。** HybridCLR 和 LeanCLR 的异常处理在解释器循环内部完成，不涉及 OS 机制。throw 时解释器设置异常状态标志，在主循环中检查这个标志来切换到异常处理模式——从当前方法的 exception clause 列表中查找匹配项。如果当前方法没有匹配的 handler，解释器退出当前方法返回到调用方继续查找。这种方式的 throw 开销远低于 CoreCLR（不需要 OS 异常分派），但正常执行路径多了一个状态检查——每次方法调用返回时需要检查异常标志。

**跨 managed/native 边界的异常传播。** CoreCLR 通过 SEH 框架统一了 managed 和 native 的异常传播——managed 异常被包装为 SEH 异常后，可以穿越 P/Invoke 边界上的 native 栈帧。HybridCLR 面临更复杂的情况：当解释器执行的方法抛出异常、异常需要穿越 IL2CPP AOT 编译的栈帧时，两种执行模式的异常处理机制需要正确衔接。CoreCLR 和 IL2CPP 不存在这个问题——CoreCLR 全栈是 native + SEH，IL2CPP 全栈是 C++ native。

## 收束

CoreCLR 的异常处理可以压缩为三个层次的协作：

**元数据层。** JIT 编译方法时生成 exception handling table 和 unwind info，记录 protected region 与 handler 的映射关系。这张表让 try block 实现了零运行时开销——正常路径不执行任何异常相关代码。

**算法层。** 两遍扫描保证了"先确认 handler 存在再展开栈帧"的不变量。Pass 1 纯读，Pass 2 执行不可逆操作（finally 和栈展开）。filter clause 在 Pass 1 中执行，为诊断提供了栈展开前的观测窗口。

**OS 集成层。** Windows 上嵌入 SEH 框架，利用 OS 的栈展开基础设施遍历 native 栈帧。Linux/macOS 上通过信号处理转换硬件异常，通过 libunwind 实现栈展开。PAL 层在两套 OS 机制之上提供统一的抽象。

三个层次的分工体现了 CoreCLR 的一个设计原则：尽可能复用 OS 基础设施，而不是自己重新实现。SEH 集成让 managed 异常和 native 异常共享同一条传播路径，信号处理让 JIT 省掉了显式的 null check 指令。代价是实现复杂度高——异常处理是 CoreCLR 中对 OS 依赖最深的子系统，也是跨平台移植工作量最大的部分。

## 系列位置

- 上一篇：CLR-B5 GC：分代式精确 GC、Workstation vs Server、Pinned Object Heap
- 下一篇：CLR-B7 泛型实现：代码共享（reference types）vs 特化（value types）
