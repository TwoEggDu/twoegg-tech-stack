---
title: "横切对比｜异常处理：两遍扫描 vs setjmp/longjmp vs 解释器展开"
slug: "runtime-cross-exception-handling-seh-setjmp-interpreter"
date: "2026-04-14"
description: "同一份 ECMA-335 异常处理契约（catch / filter / finally / fault 四种 clause），在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出五种完全不同的实现路线：OS SEH 两遍扫描、setjmp/longjmp 单遍扫描、C++ exception、解释器内部展开。从异常机制、try 开销、throw 开销、filter 支持、栈展开方式到嵌套异常处理，逐维度横切对比五种异常处理实现的设计 trade-off。"
weight: 85
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Exception"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> try 块应该是零成本的——ECMA-335 规范只说"如果异常发生，去这个 handler 执行"，没有要求你在进入 try 时做任何事。但在 setjmp/longjmp 实现里，每次进入 try 都要调用 setjmp 保存上下文。异常处理不是一个独立的语言特性问题，它是 JIT、AOT、解释器三种执行策略在"如何控制栈帧"这个核心能力上的直接分野。

这是 .NET Runtime 生态全景系列的横切对比篇第 6 篇（CROSS-G6）。

CROSS-G5 对比了泛型实现——五个 runtime 怎么从一份泛型定义产生多种封闭实例。泛型面对的是类型的"诞生"问题。异常处理面对的是执行流的"中断与恢复"问题——当一条指令抛出异常时，runtime 必须找到正确的 handler、展开中间的栈帧、执行 finally 清理、然后在 handler 位置恢复执行。五个 runtime 用了五种截然不同的机制来完成这个过程。

## ECMA-335 的异常处理契约

ECMA-335 Partition I 12.4.2 定义了 CLI 异常处理模型。规范不规定实现机制，只定义语义契约。

### 四种保护子句

每个方法的 metadata 中可以包含一个异常处理表（Exception Handling Table），表中每条记录描述一个保护区域（protected block，即 try 块）和对应的处理子句（handler clause）。ECMA-335 定义了四种子句类型：

**catch 子句。** 当 try 块内抛出的异常类型匹配 catch 指定的类型（或其子类）时，执行 catch handler。这是最常见的异常处理方式。

**filter 子句。** 和 catch 类似，但匹配条件不是类型，而是一段用户代码。filter 代码接收异常对象，返回一个布尔值决定是否处理。C# 中的 `when` 关键字编译为 filter 子句。

**finally 子句。** 无论 try 块正常完成还是异常退出，finally handler 都会执行。用于资源清理。

**fault 子句。** 仅当 try 块因异常退出时执行。和 finally 的区别是：正常退出时 fault 不执行。C# 没有直接的 fault 语法，但 C++/CLI 和 IL 层面可以使用。

### 两个关键语义

**第一个：handler 搜索必须从内到外。** 如果多个 try 块嵌套，runtime 必须从最内层开始搜索匹配的 handler。只有内层的所有 handler 都不匹配时，才向外层搜索。

**第二个：finally 必须在栈展开过程中执行。** 当异常从内层 try 传播到外层 handler 时，中间所有 try 块的 finally 都必须按顺序执行。这不是可选的——跳过 finally 是违反规范的。

规范只定义了这些语义，不规定 runtime 用什么机制实现。每个 runtime 自行决定：怎么在 throw 时找到 handler、怎么展开栈帧、怎么保证 finally 执行、try 块的进入和退出是否有运行时开销。

这些决策的差异，构成了五个 runtime 异常处理实现最核心的分化点。

## CoreCLR — 两遍扫描 + OS SEH/libunwind 集成

CoreCLR 的异常处理是五个 runtime 中最复杂的，也是和操作系统集成最深的。

### 零成本 try

CoreCLR 的 try 块在正常执行路径上是零成本的——进入 try 不生成任何额外指令，退出 try 也不生成任何额外指令。异常处理的全部信息都在 metadata 中，以表格形式存储。

RyuJIT 在编译方法时，为每个方法生成一个异常处理表（EH table），记录每个 try 块的 IL 范围和对应 handler 的 native code 地址。这个表只在异常发生时查询，正常执行路径完全不碰它。

这种设计叫"table-based exception handling"——异常处理的控制流信息不嵌入到正常代码路径中，而是存在旁路表格里。正常执行快，异常路径慢（需要查表 + 栈展开），但异常本来就应该是稀少的。

### 两遍扫描模型

CoreCLR 的异常处理分两遍扫描（two-pass exception handling）：

**第一遍：handler 搜索（Pass 1）。** 从异常抛出点开始，沿调用栈向上遍历每个栈帧。对每个栈帧，查询该方法的 EH table，检查当前 IP（instruction pointer）是否在某个 try 块范围内。如果是，检查对应的 handler 是否匹配（catch 类型匹配或 filter 返回 true）。第一遍只搜索不执行——找到匹配的 handler 后记录其位置，但不立即跳转。

**第二遍：栈展开（Pass 2）。** 再次从异常抛出点开始，向上遍历到第一遍找到的 handler 位置。这一次，对中间每个栈帧的 finally 和 fault 子句按序执行。所有清理完成后，跳转到 handler 执行。

两遍扫描的关键价值在于 filter 支持。filter 子句的代码可能需要访问栈上的局部变量——如果第一遍就开始展开栈帧，filter 执行时那些变量已经被销毁了。两遍扫描保证 filter 在第一遍执行时，整个调用栈还完好无损。

### 平台集成

在 Windows 上，CoreCLR 直接使用 OS 的 Structured Exception Handling（SEH）机制。SEH 本身就是两遍扫描模型——Windows 内核在异常发生时先调用 exception filter 找 handler，再调用 unwind handler 做清理。CoreCLR 把 .NET 异常映射到 SEH 异常，利用 OS 已有的两遍扫描基础设施。

在 Linux/macOS 上，CoreCLR 使用 libunwind 和 DWARF 展开信息来遍历栈帧。异常触发时通过信号处理（SIGSEGV/SIGFPE 等）或显式 throw 进入 CoreCLR 的异常处理管线，由 runtime 自己实现两遍扫描逻辑。

### throw 的开销

throw 在 CoreCLR 中是昂贵的操作。一次 throw 需要：

1. 构造异常对象（分配 + 填充栈跟踪）
2. 第一遍栈遍历（搜索 handler）
3. 可能执行 filter 代码
4. 第二遍栈遍历（展开 + 执行 finally/fault）
5. 跳转到 handler

栈跟踪的填充尤其昂贵——需要遍历整个调用栈，为每个栈帧解析方法名和 IL 偏移。在深调用栈上，一次 throw 可能花费数十微秒到毫秒级。

但 try 是零成本的。这个 trade-off 是明确的设计选择：优化正常路径，接受异常路径的高开销。

## Mono — setjmp/longjmp 或 C++ exception

Mono 的异常处理策略取决于执行模式。JIT 模式和 Full AOT 模式走不同的路线。

### JIT 模式：类似 CoreCLR

在 JIT 模式下，Mono 的 Mini JIT 生成 native code 时也采用表驱动的异常处理。方法的 EH 信息以表格存储，正常路径不生成额外指令。throw 时通过栈遍历找 handler，和 CoreCLR 的两遍扫描模型在概念上一致。

Mono JIT 在 Linux 上使用 DWARF 展开信息，在 Windows 上通过自己的栈遍历逻辑处理。和 CoreCLR 的区别主要在实现细节层面——Mono 的栈遍历更轻量，但优化也更少。

### Full AOT 模式：setjmp/longjmp

在 Full AOT 模式下（iOS、游戏主机），Mono 的异常处理走 setjmp/longjmp 路线。

setjmp/longjmp 是 C 标准库提供的非本地跳转机制。`setjmp` 保存当前执行上下文（寄存器、栈指针等），`longjmp` 恢复之前保存的上下文并跳转回 `setjmp` 的位置。

Mono AOT 用 setjmp/longjmp 实现异常处理的方式：

1. 每个 try 块的入口生成一个 `setjmp` 调用，保存当前上下文到一个 jmp_buf 结构
2. 这些 jmp_buf 链成一个链表，最新的在链表头
3. throw 时，从链表头取最近的 jmp_buf，调用 longjmp 跳回去
4. 跳回后检查是否匹配——不匹配就继续沿链表找下一个

### try 有运行时开销

和 CoreCLR 的零成本 try 不同，setjmp/longjmp 方案中每次进入 try 块都需要调用 setjmp。setjmp 的成本不高（通常几十纳秒——保存若干寄存器到内存），但它不是零。在一个循环内有 try 块的场景下，每次循环迭代都要调用一次 setjmp，积累起来开销可观。

### finally 的处理

longjmp 直接跳过了中间的栈帧，不会自动执行 finally。Mono AOT 在 longjmp 之前需要手动遍历 jmp_buf 链表，对中间的 try 块执行对应的 finally handler。这增加了 throw 路径的复杂度。

### C++ exception 模式

Mono 还有一种异常处理模式：使用 C++ exception。在这种模式下，.NET 的 throw 映射为 C++ 的 `throw`，catch 映射为 C++ 的 `catch`。这种方式利用 C++ 运行时已有的栈展开机制，不需要自己实现栈遍历。

C++ exception 模式的 try 也接近零成本（现代 C++ 编译器用表驱动的异常处理），但和 Mono 自身的 metadata 集成更松散——需要在 C++ exception 和 .NET exception 类型之间做映射。

## IL2CPP — C++ exception 或 setjmp/longjmp

IL2CPP 把 CIL 转译成 C++ 代码，异常处理自然映射到 C++ 层面的机制。但具体用哪种机制，取决于平台和配置。

### 标准模式：C++ exception

在大部分平台上，IL2CPP 把 .NET 的异常处理转译为 C++ 的 try/catch/throw：

```cpp
// C# 源码：
// try { DoWork(); }
// catch (Exception e) { HandleError(e); }
// finally { Cleanup(); }

// IL2CPP 转译后的 C++ 代码（简化）：
Il2CppException* __exception = nullptr;
try {
    DoWork_m12345(NULL);
}
catch (Il2CppExceptionWrapper& e) {
    __exception = e.ex;
    if (il2cpp_codegen_class_is_assignable_from(
            Exception_il2cpp_TypeInfo, __exception->klass)) {
        HandleError_m67890(NULL, __exception);
    } else {
        throw;  // 重新抛出
    }
}
// finally 块被内联到正常路径和异常路径的出口
Cleanup_m11111(NULL);
```

C++ exception 模式下，try 的开销取决于 C++ 编译器的异常实现。现代 C++ 编译器（Clang、MSVC）普遍使用表驱动的零成本异常——正常路径不生成额外指令，throw 时通过查表 + 栈展开到达 catch。

### IL2CPP_TINY / setjmp 模式

在某些受限平台上，IL2CPP 使用 setjmp/longjmp 代替 C++ exception。这种模式下：

```cpp
// setjmp 模式的异常处理（简化）
jmp_buf __jmp;
int __jmp_result = il2cpp_setjmp(__jmp);
if (__jmp_result == 0) {
    // 正常路径——try 块内的代码
    DoWork_m12345(NULL);
} else {
    // 异常路径——从 longjmp 跳回
    Il2CppException* __exception = il2cpp_get_current_exception();
    if (il2cpp_codegen_class_is_assignable_from(
            Exception_il2cpp_TypeInfo, __exception->klass)) {
        HandleError_m67890(NULL, __exception);
    }
}
```

setjmp 模式的特点和 Mono AOT 一样：try 有运行时开销（每次进入 try 调用 setjmp），throw 通过 longjmp 跳转。

### 单遍扫描

IL2CPP 的异常处理本质上是单遍扫描——C++ exception 机制本身不做 .NET 规范要求的两遍扫描。throw 触发后直接开始栈展开，遇到 catch 就停下来。

这意味着 IL2CPP 对 filter 子句的支持受限。C++ exception 没有原生的 filter 机制——catch 块只能按类型匹配。IL2CPP 通过在 catch 块内部做类型检查来模拟 filter 行为，但这和规范要求的"在栈展开之前执行 filter"有语义差异。

在实践中，C# 的 `catch (Exception e) when (e.Message.Contains("timeout"))` 在 IL2CPP 上的行为和 CoreCLR 上基本一致——但边界情况下（filter 代码访问调用栈上其他帧的局部变量）可能出现差异。

### finally 的内联

IL2CPP 对 finally 的处理比较独特：il2cpp.exe 在转译时把 finally 块的代码内联到所有可能的出口路径——正常退出路径内联一份，每个异常退出路径也内联一份。这避免了运行时动态调用 finally 的开销，但增加了生成的 C++ 代码体积。

## HybridCLR — 解释器内部的 ExceptionFlowInfo 展开

HybridCLR 的异常处理完全在解释器内部完成，不走 OS 的异常机制。

### 解释器掌控全部栈帧

HybridCLR 的解释器维护自己的执行栈——每个被解释的方法在解释器的栈上有一个 InterpFrame。当解释器内部发生异常时，不需要和 OS 的栈展开机制交互，因为所有被解释的栈帧都在解释器的控制之下。

这是解释器做异常处理的根本优势：**解释器看得见自己管理的所有栈帧，可以直接遍历、展开、恢复，不需要依赖任何外部机制。**

### ExceptionFlowInfo 机制

HybridCLR 用 ExceptionFlowInfo 结构来管理异常传播：

1. 解释器在执行某条指令时检测到异常（比如 null 解引用、显式 throw）
2. 构造 ExceptionFlowInfo，记录异常对象和抛出位置
3. 在当前方法的 EH table 中搜索匹配的 handler——按 try 块范围和类型匹配
4. 如果找到 catch，把 IP（instruction pointer）跳转到 catch handler 的位置，继续解释执行
5. 如果当前方法没有匹配的 handler，退出当前 InterpFrame，在调用方的 InterpFrame 中继续搜索
6. 搜索过程中遇到 finally 子句就先执行 finally，然后继续向外搜索

### try 是零成本的

在 HybridCLR 的解释器中，进入 try 块不需要任何额外操作。try 块的范围信息在 transform 阶段已经从 IL metadata 提取到方法的 EH table 中。解释器正常执行时完全不碰 EH table——只有异常发生时才查询。

这一点和 CoreCLR 的 table-based 方案在哲学上一致：正常路径零开销，异常路径查表。区别在于 CoreCLR 查的是 JIT 生成的 native code 级别的表，HybridCLR 查的是解释器 transform 后的指令级别的表。

### throw 比 native 快

一个反直觉的事实：解释器内部的 throw 比 native 的 throw 更轻量。原因是解释器不需要做 OS 级别的栈展开。

CoreCLR 的 throw 需要通过 SEH/libunwind 遍历 native 栈帧，每一帧需要解析 DWARF 展开信息或 SEH 展开数据。HybridCLR 的 throw 只需要在解释器自己的 InterpFrame 链表上遍历——这是纯用户态的链表遍历，不涉及 OS 调用。

当然，这个"throw 更快"的优势被解释器本身的执行速度劣势所抵消——如果方法体本身就慢 10 倍以上，throw 快 2-3 倍在整体性能上没有实际意义。

### 解释器到 AOT 的边界

当异常从 HybridCLR 解释器内部抛出，但 handler 在 AOT 代码中时（或反过来），需要跨越解释器和 native code 的边界。HybridCLR 在解释器的入口和出口设置了桥接——从解释器抛出的异常在桥接点转换为 C++ exception（或 IL2CPP 的异常机制），让 AOT 侧的异常处理能接管。

这个桥接是 HybridCLR 异常处理中最复杂的部分。纯解释器内部的异常处理很简单，跨边界的异常传播需要确保两套机制无缝衔接。

## LeanCLR — 解释器内部的 RtInterpExceptionClause 展开

LeanCLR 的异常处理和 HybridCLR 在设计哲学上一致——都是解释器内部展开，不依赖 OS 机制。但 LeanCLR 是独立实现，有自己的数据结构和流程。

### RtInterpExceptionClause

LeanCLR 用 RtInterpExceptionClause 描述异常处理子句。在方法 transform 阶段，IL metadata 中的 EH table 被转换为 RtInterpExceptionClause 数组，每个 clause 记录：

- try 块的 LL-IL 指令范围（起始和结束偏移）
- handler 类型（catch / filter / finally / fault）
- handler 的 LL-IL 指令偏移
- catch 的目标类型（如果是 catch 子句）

### 异常处理流程

LeanCLR 解释器执行时的异常处理流程：

1. 执行中检测到异常条件（throw 指令、null 访问、溢出等）
2. 在当前方法的 RtInterpExceptionClause 数组中搜索——检查当前 IP 是否在某个 clause 的 try 范围内
3. 按 clause 在数组中的顺序搜索（metadata 保证从内到外排列）
4. 如果找到匹配的 catch clause，将 IP 跳转到 handler 偏移，将异常对象压入求值栈
5. 如果找到 finally clause（在搜索 catch 的过程中），先执行 finally handler
6. 如果当前方法所有 clause 都不匹配，退出当前帧，在调用方继续搜索

### 与 HybridCLR 的差异

虽然原理相同，但两个实现在细节上有差异：

**指令集层面。** HybridCLR 的 ExceptionFlowInfo 工作在 HybridCLR transform 后的指令集上，LeanCLR 的 RtInterpExceptionClause 工作在 LL-IL 指令集上。两者的指令偏移、栈布局、帧结构都不同。

**边界问题。** HybridCLR 需要处理解释器和 IL2CPP AOT 代码之间的异常传播边界。LeanCLR 是纯解释器——所有代码都在解释器内部执行，不存在解释器/native 边界问题。这让 LeanCLR 的异常处理比 HybridCLR 更简单。

**GC 安全点。** CoreCLR 的异常处理需要在栈展开过程中维护 GC 安全性——展开过程中可能触发 GC，需要确保所有引用仍然可达。LeanCLR 的 GC 接口是 stub 状态，不需要在异常处理路径中考虑 GC 安全问题。

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 异常机制 | OS SEH (Win) / libunwind (Linux) | setjmp/longjmp (AOT) / C++ exception (JIT) | C++ exception / setjmp/longjmp | 解释器内部 ExceptionFlowInfo | 解释器内部 RtInterpExceptionClause |
| try 开销 | 零成本（表驱动） | JIT 零成本 / AOT setjmp 有开销 | C++ exception 零成本 / setjmp 有开销 | 零成本（表驱动） | 零成本（表驱动） |
| throw 开销 | 高（OS 栈展开 + 栈跟踪构建） | 中（longjmp）/ 高（C++ throw） | 中-高（C++ throw / longjmp） | 低（用户态链表遍历） | 低（用户态链表遍历） |
| 扫描模式 | 两遍扫描 | 取决于模式 | 单遍扫描 | 单遍（解释器内部） | 单遍（解释器内部） |
| filter 支持 | 完整（两遍扫描保证） | JIT 完整 / AOT 受限 | 受限（catch 内模拟） | 完整（解释器可控） | 完整（解释器可控） |
| 栈展开方式 | DWARF / SEH 展开 | DWARF / longjmp | C++ unwind / longjmp | InterpFrame 链表遍历 | 帧链表遍历 |
| nested exception | OS 机制原生支持 | 链表管理 | C++ 嵌套 throw | ExceptionFlowInfo 嵌套 | clause 数组嵌套搜索 |
| 性能影响 | try 零成本，throw 昂贵 | AOT try 有开销，throw 中等 | 平台依赖 | try 零成本，throw 轻量 | try 零成本，throw 轻量 |
| **源码锚点** | `src/coreclr/vm/exceptionhandling.cpp` | `mono/mini/mini-exceptions.c` | `il2cpp/vm/Exception.cpp` | `hybridclr/interpreter/Interpreter_Execute.cpp` | `src/runtime/interp/interpreter.cpp` |

## 为什么解释器的异常处理比 JIT 简单

五个 runtime 的异常处理实现复杂度差异巨大。CoreCLR 的异常处理是整个 runtime 中最复杂的子系统之一，而 HybridCLR 和 LeanCLR 的异常处理相对简洁。这不是因为解释器偷了懒，而是因为解释器和 JIT/AOT 在"栈帧控制权"上有根本性差异。

### JIT/AOT：栈帧属于 OS

JIT 和 AOT 编译器生成的是 native code，运行在 OS 管理的调用栈上。每个方法调用产生一个 native 栈帧，由 CPU 的 call/ret 指令和 OS 的栈管理机制控制。

当异常发生时，runtime 需要"逆向操作" OS 的栈——遍历 native 栈帧、解析展开信息、恢复寄存器状态、执行清理代码。这个过程必须和 OS 合作，因为栈帧的布局和管理权在 OS 和编译器手中。

CoreCLR 在 Windows 上用 SEH，在 Linux 上用 libunwind + DWARF——两种方案都是在 OS/ABI 层面的标准化栈展开协议上构建的。这些协议本身就很复杂（DWARF 展开信息的编码格式、SEH 的多级分发机制），CoreCLR 还需要在这些协议上叠加 .NET 特有的语义（两遍扫描、filter、GC 安全）。

### 解释器：栈帧属于自己

解释器的执行栈是自己管理的数据结构——一个 InterpFrame 链表或数组。每个方法调用创建一个 InterpFrame，记录局部变量、求值栈、当前 IP。这些帧完全在用户态内存中，由解释器代码直接操作。

当异常发生时，解释器直接遍历自己的 InterpFrame 链表，查 EH table，跳转 IP——整个过程是纯粹的应用层逻辑，不涉及 OS 调用、ABI 约定、展开信息解析。

这就是为什么 HybridCLR 和 LeanCLR 的异常处理实现可以比 CoreCLR 简单一个数量级。它们不需要和 OS 的栈展开机制交互，不需要解析 DWARF 或 SEH 数据，不需要处理 native code 和 managed code 之间的栈帧交错。

### 代价：性能

解释器在异常处理上的简单性是有代价的。解释器管理的栈帧意味着所有方法调用都通过解释器的 dispatch loop——方法调用本身就比 native call 慢很多。虽然 throw 路径更轻量，但正常的方法调用和执行已经慢了 10-100 倍。

此外，解释器的异常处理只覆盖解释器内部的代码。HybridCLR 需要处理解释器和 AOT 代码之间的异常边界，这个边界处理的复杂度并不低于 JIT 的异常处理。LeanCLR 因为是纯解释器，没有这个边界问题——但也因此没有 native code 的性能。

### 一个有趣的对称性

JIT/AOT 的 trade-off 是：try 零成本，throw 昂贵（因为需要 OS 级栈展开）。解释器的 trade-off 是：try 零成本，throw 轻量（因为栈帧在自己手里），但整体执行本身就慢。

两种方案都在优化正常路径、接受异常路径的开销——只不过"正常路径"的含义不同。JIT/AOT 的正常路径已经是 native 速度，异常路径偶尔的高开销可以接受。解释器的正常路径本身就慢，但至少异常路径不会雪上加霜。

## 收束

同一份 ECMA-335 异常处理契约，五个 runtime 给出了五种实现机制。差异的根源是各自的执行策略决定了"栈帧由谁管理"这个根本问题的答案：

- CoreCLR 的栈帧由 OS 管理，异常处理必须和 OS 的栈展开机制深度集成，复杂但 try 零成本
- Mono 在 JIT 模式下和 CoreCLR 类似，在 AOT 模式下退回 setjmp/longjmp，用 try 的运行时开销换取实现的可移植性
- IL2CPP 把异常处理映射到 C++ 层面，利用 C++ 编译器已有的异常机制，但受限于 C++ exception 的语义边界
- HybridCLR 在解释器内部用 ExceptionFlowInfo 完成异常处理，简洁高效，但需要处理解释器和 AOT 的边界
- LeanCLR 在解释器内部用 RtInterpExceptionClause 完成异常处理，纯解释器架构让实现最简单，没有跨边界问题

异常处理不是一个孤立的语言特性问题。它是 runtime 执行策略的直接后果——你选择了 JIT、AOT 还是解释器，就同时选择了栈帧管理模型，也就决定了异常处理的实现路线。理解了这个约束结构，就理解了为什么 CoreCLR 需要和 SEH/libunwind 深度集成，也理解了为什么解释器的异常处理可以如此简洁。

## 系列位置

这是横切对比篇第 6 篇（CROSS-G6），也是 Phase 3 的首篇横切对比。

Phase 1 的三篇横切对比（CROSS-G1 metadata 解析、G2 类型系统、G3 方法执行）覆盖了 runtime 的基础层三大环节。Phase 2 的两篇（CROSS-G4 GC 实现、G5 泛型实现）覆盖了 GC 和泛型两个跨 runtime 差异最显著的维度。Phase 3 的两篇（CROSS-G6 异常处理、G7 程序集加载与热更新）覆盖异常处理和程序集加载——这两个维度直接关系到热更新工程实践中最常遇到的跨 runtime 行为差异。

下一篇 CROSS-G7 将对比程序集加载与热更新——五个 runtime 怎么加载新代码、能不能卸载旧代码、热更新能力的边界在哪里。
