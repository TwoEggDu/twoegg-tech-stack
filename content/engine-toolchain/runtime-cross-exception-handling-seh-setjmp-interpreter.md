---
title: "横切对比｜异常处理：两遍扫描 vs setjmp/longjmp vs 解释器展开"
slug: "runtime-cross-exception-handling-seh-setjmp-interpreter"
date: "2026-04-14"
lastmod: "2026-10-26"
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

> 异常处理既要满足 catch／filter／finally／fault 的语义，也要接上实际执行栈。规范不要求每次进入 try 都注册 handler；具体成本取决于实现、平台与调用路径。异常处理不是一个独立的语言特性问题，它是 JIT、AOT、解释器三种执行策略在"如何控制栈帧"这个核心能力上的直接分野。

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

## HybridCLR：解释器帧查表与 native 异常边界

解释器管理自己的 InterpFrame、转换后指令与异常处理表，可以在这些逻辑帧之间寻找 catch、执行清理和继续展开。跨 AOT 调用、运行时辅助函数和解释器出口仍会进入 native 异常机制；不能写成“所有异常完全不经过 OS／C++ 展开”。本轮 Windows x64 固定构建实际观察到了 C++ 的 Il2CppExceptionWrapper 捕获。

### 当前方法上下文必须一致

`frame` 表示当前解释帧，`imi` 指向当前解释方法的转换信息；`ip`／`ipBase` 确定转换后指令偏移。异常搜索必须用同一方法的指令位置和 EH 表。只知道逻辑调用链里存在某个 catch，还不能证明现场搜索到了它。

固定 runtime v9.0.1 的 `Interpreter_Execute.cpp` 中，ExceptionFlowInfo 保存异常和抛出位置，搜索逻辑读取 `imi->exClauseCount` 与 `imi->exClauses`；需要退出解释帧时，再恢复调用方的帧与方法上下文。它与 native 边界的异常路径共同工作。[固定公开源码](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/Interpreter_Execute.cpp)

### 表驱动不等于跨边界零成本

try 的保护区间可以用静态表表达，不必为每次进入 try 主动注册 handler。类型初始化、桥接、native 展开、异常对象和堆栈构建仍有各自的路径与成本。本文没有同负载跨 runtime 性能数据，不据源码结构断言 throw 更快，也不保留原先无测量依据的倍数。

### 实测：.cctor 异常为什么跳过内部 catch {#exception-flow-player}

2026-10-26 在 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 新增 `projects/exception-flow`，追查[类型初始化实验]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#type-initialization-player" >}})中三次稳定复现的异常逃逸。环境仍是本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3／runtime v9.0.1、Windows x64 Development／IL2CPP C++ Debug／Minimal stripping。

先分清抛出位置、调用方向和调用方式。AOT／热更场景使用同一份自有代码，仅命名空间与来源标签不同；AOT 进入热更用缓存委托，反射只用于建立委托，不把其包装异常混进被测调用。普通方法抛 `InvalidOperationException`、手工抛 `TypeInitializationException`、失败 .cctor 的可内联短 void 调用及明确不可内联的含分支 int 调用分别成组。含分支与返回类型同时不同，不能把结果单独归因于“返回 int”。

| 调用方向／场景 | 原始固定运行时 | 隔离反事实 |
|---|---|---|
| AOT → AOT，四种抛出形状 | 每组 3 个进程，两次访问均捕获，finally 各一次 | 相同 |
| 热更 → AOT，四种形状 | 每组 3 个进程，两次均捕获，finally 各一次 | 相同 |
| AOT → 热更，四种形状，经缓存委托 | 每组 3 个进程，两次均捕获，finally 各一次 | 相同 |
| 热更 → 热更，普通抛出／手工 TypeInitializationException／短 void 初始化 | 每组 3 个进程，两次均捕获，finally 各一次 | 相同 |
| 热更 → 热更，不可内联的失败初始化调用 | 3 个进程，两次都逃逸，内部 catch 与 finally 均未执行 | 3 个进程，两次均捕获，finally 各一次 |

每版 48 个普通进程，两版共 96 个；驱动均退出 0 表示采集完成，是否捕获由事件独立判定。基线为 45 个处理成功、3 个逃逸对照，反事实为 48 个处理成功。两侧失败类型的 .cctor 都只执行一次，第二次访问继续报告已有初始化失败。

### 先证明输入和坐标，再检查查表对象

基线 build_id `0628a07c-8e75-40ed-bb91-c295c40414c7`；反事实为 `c811dcee-5771-4a89-86f9-ed53d862be1d`。两版 43 个输入、最终裁剪 AOT DLL、随包热更 DLL、4 份生成代码文件均逐字节相同。各 227 个产物有哈希绑定，919 个运行时输入中仅指定的公开 HybridCLR 文件不同。

最终两个自有 DLL 的 CIL 都把 `Failed.PingRet` 的 call 放在偏移 87、typed catch 保护区间 `[17,96)` 内；外层 finally 保护区间为 `[17,144)`。三轮匹配 PDB 的原生取证分别在每版采到 6 次异常现场：

| 现场状态 | 原始运行时 | 隔离反事实 |
|---|---|---|
| 当前帧和指令基址 | 都属于调用方 RunOwn | 相同 |
| 转换后抛出偏移 | 304，在 typed catch `[48,328)` 和 finally `[48,448)` 内 | 相同 |
| 活动 imi | 等于已准备的 PingRet 缓存，与调用方缓存不同 | 仍等于调用方缓存 |
| 活动 EH 条数／调用方 EH 条数 | 0／3 | 3／3 |
| PingRet 转换缓存 | 已准备 | 同样已准备 |

**CIL 的 87 与转换后指令的 304 是不同坐标系，不能直接比较。** 本轮分别核对了各自的保护区间；问题发生在“用哪份 EH 表”，不是把这两个偏移混为一谈，也不是源码忘了把 call 放进 try。

### 根因与最小反事实

原来的建帧宏先将 `imi` 改为被调方法的转换信息，再执行其类型初始化，最后才进入新帧并更新指令上下文。若 .cctor 在中途失败，`frame` 和 `ip`／`ipBase` 仍属调用方，`imi` 却已经属被调方。native catch 随后用这个不一致的组合查表：本例被调 PingRet 没有 EH 子句，于是调用方的 catch 与 finally 都被跳过。

这解释了为什么普通方法抛出或手工抛同一种异常可以捕获：它们实际进入方法体后，再按退出解释帧的路径恢复调用方上下文；失败 .cctor 则出现在新帧尚未建立的窗口内。

反事实只改新工程 Library 中的 `PREPARE_NEW_FRAME_FROM_INTERPRETER`：先把被调方法准备结果保存在 `nextImi` 临时变量，类型初始化成功后再赋给活动 `imi`。**仍在初始化前准备方法缓存**，不同时改变准备顺序、异常类型、调用方代码或 DLL。对应公开最小差异位于 `patches/exception-flow-delay-active-imi.patch`。发生异常时，调用方上下文得以保留，原先三轮逃逸对照的内部 catch 和 finally 都恢复执行。

这是对当前固定版本与样本路径的因果验证；原始上游仓库、共享 prepared runtime、旧工程及旧回执均保留。隔离变体不是官方版本更新，也没有把单一路径的结果扩展成所有平台／filter／嵌套展开路径的完整验收。

### 为什么短 void 对照没有暴露问题

每版另取一个匹配 PDB 的短 void 控制进程。两次访问都实际停在 `InitClassStaticCtor` 指令，转换后偏移 248，活动缓存始终属于调用方。这条内联路径在原调用方上下文里处理初始化，没有经过本例提前切换 imi 的建帧窗口。

固定版本的内联判断应结合 `MethodBodyCache`、`TransformContext` 和实际转换指令核对，不能只看 C# 写了 NoInlining。本轮含分支方法用于明确排除内联，不把返回类型当作唯一控制变量。

### 复现入口与记录

依次使用 `tools/eh_lab.py build/run/trace --variant baseline`，再执行 `--variant counterfactual` 的同组命令；`tools/eh_inline.py`采短 void 控制，最后 `verify`、`export`。普通运行 96 个、native 取证 8 个，共 104 份公开自有事件；原始日志、PDB、Player 和生成的 Unity C++ 留在忽略目录。

本轮固定提交为 [f5ba510](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/f5ba510382e170ac06d2fd7033db363e6c58f62e)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/results.json)与[公开最小补丁](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/patches/exception-flow-delay-active-imi.patch)。复现使用[实验工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/f5ba510382e170ac06d2fd7033db363e6c58f62e/projects/exception-flow)；前轮 `46ac71c` 不包含本轮工程或反事实补丁。旧原始 RunFailed 的只读复核也看到活动 EH0／调用方 EH1、偏移 88 位于转换后 `[88,136)`，与新样本定位一致；原始证据不被反事实结果改写。

## LeanCLR — 解释器内部的 RtInterpExceptionClause 展开

LeanCLR 用自己的解释器异常数据结构组织逻辑帧内的处理。它可以与 HybridCLR 对照 EH 表和帧恢复方式，但不能由此推出两者都不存在 native 边界。

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

**边界问题。** HybridCLR 需要处理解释器和 IL2CPP AOT／运行时辅助函数之间的异常传播。本篇对 LeanCLR 只讨论解释帧内部；宿主调用和原生接口的异常责任应按其版本另行核对，不从“纯解释器”名称推出没有边界问题。

**GC 安全点。** 异常展开与 GC 的协作需要保证仍然活跃的引用可见，不能从某版 GC 接口尚未完成就推出清理路径无需考虑引用安全。本轮没有验证 LeanCLR 的 GC／异常协作。

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 异常机制 | OS SEH (Win) / libunwind (Linux) | setjmp/longjmp (AOT) / C++ exception (JIT) | C++ exception / setjmp/longjmp | 解释器表驱动＋native边界 | 解释器内部 RtInterpExceptionClause |
| try 开销 | 零成本（表驱动） | JIT 零成本 / AOT setjmp 有开销 | C++ exception 零成本 / setjmp 有开销 | 表驱动保护区间；边界成本另计 | 零成本（表驱动） |
| throw 开销 | 高（OS 栈展开 + 栈跟踪构建） | 中（longjmp）/ 高（C++ throw） | 中-高（C++ throw / longjmp） | 路径相关，本文未测 | 低（用户态链表遍历） |
| 扫描模式 | 两遍扫描 | 取决于模式 | 单遍扫描 | 按解释器EH表／native边界分别分析 | 单遍（解释器内部） |
| filter 支持 | 完整（两遍扫描保证） | JIT 完整 / AOT 受限 | 受限（catch 内模拟） | 有filter指令与路径；本轮未验收完整语义 | 完整（解释器可控） |
| 栈展开方式 | DWARF / SEH 展开 | DWARF / longjmp | C++ unwind / longjmp | 解释帧恢复＋边界native展开 | 帧链表遍历 |
| nested exception | OS 机制原生支持 | 链表管理 | C++ 嵌套 throw | ExceptionFlowInfo 嵌套 | clause 数组嵌套搜索 |
| 性能影响 | try 零成本，throw 昂贵 | AOT try 有开销，throw 中等 | 平台依赖 | 表驱动；实际成本待同负载测量 | try 零成本，throw 轻量 |
| **源码锚点** | `src/coreclr/vm/exceptionhandling.cpp` | `mono/mini/mini-exceptions.c` | `il2cpp/vm/Exception.cpp` | `hybridclr/interpreter/Interpreter_Execute.cpp` | `src/runtime/interp/interpreter.cpp` |

## 逻辑帧可控，边界上下文仍需一致

解释器拥有自己的逻辑帧和指令表，使局部异常搜索可以直接操作这些结构；JIT／AOT 的 native 帧还需要遵守目标 ABI 与展开协议。两者都必须把异常位置、方法身份和清理责任保持一致。

本轮缺陷恰好出现在解释器调用运行时辅助函数、而新解释帧尚未建立的窗口。能够遍历自己的帧，不等于跨边界状态天然正确；表驱动也不能推出 throw 的固定性能优势。比较实现时应同时查看正常进入、初始化中途失败、异常返回和清理路径。

## 收束

同一份 ECMA-335 异常处理契约，五个 runtime 给出了五种实现机制。差异的根源是各自的执行策略决定了"栈帧由谁管理"这个根本问题的答案：

- CoreCLR 的栈帧由 OS 管理，异常处理必须和 OS 的栈展开机制深度集成，复杂但 try 零成本
- Mono 在 JIT 模式下和 CoreCLR 类似，在 AOT 模式下退回 setjmp/longjmp，用 try 的运行时开销换取实现的可移植性
- IL2CPP 把异常处理映射到 C++ 层面，利用 C++ 编译器已有的异常机制，但受限于 C++ exception 的语义边界
- HybridCLR 用解释帧 EH 表搜索，同时维护跨 AOT／运行时辅助函数的 native 边界及一致的方法上下文
- LeanCLR 的解释帧内部用 RtInterpExceptionClause 组织处理，宿主与原生接口边界需按版本另行核对

异常处理不是一个孤立的语言特性问题。它是 runtime 执行策略的直接后果——你选择了 JIT、AOT 还是解释器，就同时选择了栈帧管理模型，也就决定了异常处理的实现路线。理解了这个约束结构，就理解了为什么 CoreCLR 需要和 SEH/libunwind 深度集成，也能理解为什么解释器的逻辑帧处理与 native 边界必须分别核对。

## 系列位置

这是横切对比篇第 6 篇（CROSS-G6），也是 Phase 3 的首篇横切对比。

Phase 1 的三篇横切对比（CROSS-G1 metadata 解析、G2 类型系统、G3 方法执行）覆盖了 runtime 的基础层三大环节。Phase 2 的两篇（CROSS-G4 GC 实现、G5 泛型实现）覆盖了 GC 和泛型两个跨 runtime 差异最显著的维度。Phase 3 的两篇（CROSS-G6 异常处理、G7 程序集加载与热更新）覆盖异常处理和程序集加载——这两个维度直接关系到热更新工程实践中最常遇到的跨 runtime 行为差异。

下一篇 CROSS-G7 将对比程序集加载与热更新——五个 runtime 怎么加载新代码、能不能卸载旧代码、热更新能力的边界在哪里。
