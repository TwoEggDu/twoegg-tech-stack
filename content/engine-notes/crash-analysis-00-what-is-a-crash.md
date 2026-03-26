+++
title = "崩溃分析基础｜信号、异常、托管与 native，先把概念底座立住"
description = "崩溃分析系列第 0 篇。从操作系统信号、C++ 异常、托管异常三条线拆清楚"崩溃"这件事，讲清楚符号是什么、调用栈怎么读，为后续 Android / iOS / Windows 三个平台篇建立共同语言。"
weight = 50
featured = false
tags = ["Crash", "Debug", "NativeCrash", "Symbols", "Callstack", "Unity", "IL2CPP"]
series = "CrashAnalysis"
+++

> 大多数人第一次遇到 native crash 的反应，是去搜那几行 log；但如果连"这次 crash 是因为什么机制终止的"都不清楚，搜到的答案通常也对不上。

这是崩溃分析系列第 0 篇。
它不讲任何具体平台的操作，只做一件事：

`把后面三个平台篇（Android / iOS / Windows）共同依赖的基础概念先立住。`

## 这篇要回答什么

1. "崩溃"这个词背后有哪几种不同的机制，为什么要分开
2. 信号（signal）、C++ 异常、托管异常分别是什么，对调试有什么不同影响
3. 符号（symbols）是什么，为什么 release 包里看不到函数名
4. 调用栈怎么读，帧、PC、SP、LR 分别指什么
5. 托管 crash 和 native crash 在 Unity 里从外部看起来有什么不同

## 崩溃不是一件事，是三件事

很多人说的"崩溃"其实是三种完全不同的终止机制混在一起叫的。

### 1. 操作系统信号终止

进程因为做了某件操作系统不允许的事，被内核发送一个 **信号（signal）** 强制结束。

常见信号：

| 信号 | 全称 | 常见原因 |
|------|------|----------|
| `SIGSEGV` | Segmentation Violation | 访问了未映射的内存地址（空指针、越界、栈溢出） |
| `SIGABRT` | Abort | 代码主动调用 `abort()`，或断言失败，或 C++ 异常未捕获 |
| `SIGBUS` | Bus Error | 内存对齐错误（ARM 上常见） |
| `SIGILL` | Illegal Instruction | 执行了非法 CPU 指令 |
| `SIGFPE` | Floating Point Exception | 除以零或浮点溢出 |

信号终止是**最难调试的一类**，因为它发生在 native 层，没有托管语言的异常机制，没有 try/catch，crash 的位置就是出问题的那行机器码。

### 2. C++ 异常未捕获

C++ 代码里抛出了一个异常，但没有任何一层 `catch` 接住它，最终触发 `std::terminate()`，进而调用 `abort()`，发出 `SIGABRT`。

从外部看，这和直接的 `SIGSEGV` 有时很像，但调用栈里通常能看到 `__cxa_throw`、`terminate`、`abort` 这几个函数。

### 3. 托管异常未捕获

C# / Java 等托管语言里的异常。这类异常有语言运行时兜底，通常不会直接杀死进程，而是：

- 在 Unity 里：打印到 log，可能触发 error pause，但进程本身继续运行
- 在 Android Java 层：未捕获的异常会触发 `UncaughtExceptionHandler`，Android 通常会终止 Activity

**Unity 项目的关键区分**：Unity 里的 C# `Exception` 默认只是一个 log，不会闪退；只有 native crash（信号终止）才会让 app 真正消失。

---

## 为什么 release 包看不到函数名

编译器把源代码变成机器码时，可以选择一起输出**调试符号**，也可以选择丢掉它。

调试符号包含：
- 每个机器码地址对应的函数名
- 每个函数名对应的源文件和行号
- 局部变量名、类型信息（可选）

Release 构建默认丢掉这些信息，目的是：
1. 减小包体
2. 防止逆向

结果是：crash 时看到的只有 **内存地址**，不是函数名。

```
#00 pc 00000000046951fc  /data/app/.../lib/arm64/libil2cpp.so
#01 pc 0000000003939604  /data/app/.../lib/arm64/libil2cpp.so
```

**符号化（Symbolication）** 就是把这些地址翻译回函数名的过程。各平台的符号化工具不同，但原理相同：

```
输入：地址 + 带符号的二进制文件（或独立符号文件）
输出：函数名 + 源文件 + 行号（如果有）
```

各平台的符号文件格式：

| 平台 | 符号文件格式 | 符号化工具 |
|------|-------------|------------|
| Android | `.so`（带符号的版本）| `llvm-addr2line`、`ndk-stack` |
| iOS | `.dSYM`（独立符号包）| `atos`、`symbolicatecrash` |
| Windows | `.pdb`（Program Database）| WinDbg、Visual Studio |

Unity IL2CPP 项目打包时会额外产出一个 `*-IL2CPP.symbols.zip`，里面是带符号的 `libil2cpp.so` / `GameAssembly.dll`（含 pdb），三个平台都要靠它做符号化。

---

## 调用栈怎么读

符号化之后，看到的是一张调用栈（backtrace / call stack）：

```
#00  AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted  [crash point]
#01  hybridclr::interpreter::Interpreter::Execute
#02  hybridclr::interpreter::InterpreterInvoke
#03  AsyncUniTaskMethodBuilder_Start
#04  hybridclr::interpreter::Interpreter::Execute
...
#14  FsmModule_Update
#15  ModuleSystem_Update
```

读法规则：

- **编号越小越晚**：`#00` 是崩溃点，编号越大是越早被调用的帧
- **从下往上读**是调用方向：`ModuleSystem_Update` → `FsmModule_Update` → ... → 崩溃
- **帧地址重复**：同一个地址在不同帧里出现，几乎可以确定是递归/死循环，最终因栈溢出导致 `SIGSEGV`

四个寄存器的含义（ARM64，Android/iOS 通用）：

| 寄存器 | 含义 |
|--------|------|
| `PC` | Program Counter，当前执行的指令地址，也是符号化要查的地址 |
| `SP` | Stack Pointer，当前栈顶指针 |
| `LR` | Link Register，函数返回地址（`#01` 通常就是 LR 指向的地方） |
| `FP` / `x29` | Frame Pointer，栈帧基址 |

---

## Unity 里托管 crash 和 native crash 的外观区别

在实际项目里，这两类 crash 有一些可以快速区分的外部特征：

**托管异常（C# Exception）**

- 日志 tag：`Unity`（Android）
- 日志级别：`E Unity`
- 格式：`Exception: System.TypeLoadException: ...` + 托管调用栈（有 C# 类名和方法名）
- 进程状态：进程通常还在，只是报了个错；可能后续逻辑无法继续但不一定立刻退出

**Native crash（信号终止）**

- 日志 tag：`CRASH`（Android）/ Crash Reporter（iOS）/ Windows Error Reporting
- 格式：`signal 11 (SIGSEGV)` + 寄存器转储 + native 调用栈（全是地址，需要符号化）
- 进程状态：进程已被内核强制终止，`Activity` 退出，app 回到桌面或被系统关闭

一个快速记忆方式：

> 有 C# 类名在日志里 = 托管层，能看到 IL2CPP/系统函数地址 = native 层。

---

## 小结：建立这套共同语言的理由

后面三篇（Android / iOS / Windows）讲的都是具体操作，但所有操作都在做同一件事：

1. 判断这次 crash 是信号终止、C++ 异常还是托管异常
2. 拿到原始调用栈
3. 用符号化工具把地址翻译成函数名
4. 从调用栈里读出"哪里出问题、什么模式"

理解了这套逻辑，三个平台的差别只是工具不同，不是思维框架不同。

## 系列位置

- 上一篇：无，这是系列入口
- 下一篇：[崩溃分析 Android 篇｜adb logcat、tombstone、llvm-addr2line 完整流程]({{< relref "engine-notes/crash-analysis-01-android.md" >}})
