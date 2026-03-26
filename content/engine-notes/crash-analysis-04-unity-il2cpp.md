+++
title = "崩溃分析 Unity + IL2CPP 篇｜symbols.zip、global-metadata.dat 和三平台统一视角"
description = "崩溃分析系列第 4 篇。Unity IL2CPP 项目的崩溃分析统一视角：symbols.zip 的产出和用法，global-metadata.dat 的作用，libil2cpp.so 和 GameAssembly.dll 的崩溃模式识别，以及 HybridCLR 热更代码崩溃的定位思路。"
weight = 54
featured = false
tags = ["Crash", "Debug", "Unity", "IL2CPP", "NativeCrash", "Symbols", "HybridCLR", "global-metadata"]
series = "CrashAnalysis"
+++

> 前三篇分别讲了 Android / iOS / Windows 三个平台的操作，但 Unity IL2CPP 项目有自己的特殊性——崩溃经常发生在 `libil2cpp.so` 或 `GameAssembly` 里，这两个模块夹在操作系统和你的 C# 代码之间，理解它们是三平台通用的前置知识。

这是崩溃分析系列第 4 篇，收束整个系列。

---

## IL2CPP 的编译链路

C# 代码在 Unity IL2CPP 项目里不是直接执行的，而是：

```
C# 源码
  → IL2CPP 转译 → C++ 源码（生成在 Temp/StagingArea 或导出的 Xcode/Android 工程里）
    → C++ 编译器（Clang / MSVC）→ 机器码（.so / .dylib / .dll）
```

这条链路产生了两类关键文件：

| 文件 | 内容 | 平台 |
|------|------|------|
| `libil2cpp.so` | IL2CPP 运行时 + 转译后的 C# 代码（合并在一起） | Android |
| `GameAssembly.dylib` / `GameAssembly.dll` | 同上 | iOS / Windows |
| `global-metadata.dat` | 类型元数据（类名、方法名、字段名等字符串信息） | 三平台 |

符号化后能看到函数名，但**类型名和方法名**来自 `global-metadata.dat`——这也是逆向工程时的关注点。

---

## symbols.zip：Unity 崩溃分析的核心文件

每次打包时，Unity 都会在构建输出目录产出一个 `*-IL2CPP.symbols.zip`（文件名因版本不同略有差异）。

这个 zip 包含带调试符号的二进制文件：

```
symbols/
  Android/
    arm64-v8a/
      libil2cpp.so          ← 带符号版本（比包里的大很多）
    armeabi-v7a/
      libil2cpp.so
  iOS/
    GameAssembly.dSYM/      ← dSYM 格式
      Contents/
        Resources/
          DWARF/
            GameAssembly
    UnityFramework.dSYM/
  Windows/
    x86_64/
      GameAssembly.pdb      ← PDB 格式
```

**关键原则：**
- 每次打包都产出一套新的 symbols.zip
- symbols.zip 和对应的安装包是强绑定的，不能跨包混用
- 必须妥善归档：构建 #68 的崩溃，只能用构建 #68 的 symbols.zip 分析

---

## libil2cpp.so 崩溃的模式识别

在 Android 的 crash 里，看到崩溃在 `libil2cpp.so` 里时，可以根据符号化结果的函数名识别几种常见模式。

### 模式 1：空指针 dereference（真实的 null）

符号化后：
```
#00  SomeClass_SomeMethod (SomeClass.cpp:123)
#01  il2cpp::vm::Runtime::Invoke (...)
```

函数名是普通的业务代码，不是解释器循环。`#00` 帧就是问题所在——查那行代码里哪个对象是 null。

### 模式 2：栈溢出（帧地址重复）

符号化后：
```
#02  hybridclr::interpreter::InterpreterInvoke (...)
#03  SomeAsyncMethod_MoveNext (...)
#04  hybridclr::interpreter::InterpreterInvoke (...)
#05  SomeAsyncMethod_MoveNext (...)
...（循环数百次）
```

或者：
```
#04  RecursiveMethod (...)
#05  RecursiveMethod (...)
#06  RecursiveMethod (...)
```

→ 不是真的空指针，是栈空间耗尽。找循环依赖或递归出口缺失。

### 模式 3：IL2CPP 运行时内部错误

符号化后：
```
#00  il2cpp::vm::Type::GetOrCreateArrayType (...)
#01  il2cpp::vm::MetadataCache::GetArrayType (...)
```

→ 通常是 IL2CPP 运行时的 bug，或者 global-metadata.dat 损坏/不匹配。先确认 metadata 文件版本正确。

### 模式 4：HybridCLR 解释器中的崩溃

符号化后：
```
#00  hybridclr::interpreter::Interpreter::Execute (...)
#01  hybridclr::interpreter::InterpreterInvoke (...)
#02  SomeHotUpdateMethod (...)
```

→ 热更代码在解释器里执行时崩溃。需要结合 HybridCLR 的日志和热更代码逻辑定位。

---

## global-metadata.dat：崩溃分析里的角色

`global-metadata.dat` 存放的是运行时需要的**元数据**：类名、方法名、接口名、属性名等字符串。

在崩溃分析里，它的作用主要是：

1. **混淆相关问题**：如果 C# 代码被符号混淆（obfuscation），`global-metadata.dat` 里的名字也会被混淆，符号化后看到的函数名可能是乱码
2. **版本不匹配问题**：如果设备上的 `global-metadata.dat` 与 `libil2cpp.so` 版本不一致（常见于热更），可能导致运行时崩溃，通常表现为启动时 `SIGABRT` 或类型查找失败

`global-metadata.dat` 在 Android 包里的路径：

```
apk/assets/bin/Data/Managed/Metadata/global-metadata.dat
```

---

## 三平台操作对比

同一个 Unity IL2CPP 项目，在三个平台上的崩溃分析流程主体一致，差别只在工具上：

| 环节 | Android | iOS | Windows |
|------|---------|-----|---------|
| 崩溃日志来源 | `adb logcat` CRASH tag / tombstone | Xcode Organizer / `.ips` 文件 | Player.log / `.dmp` 文件 |
| 符号文件格式 | 带符号的 `.so` | `.dSYM` 包 | `.pdb` |
| 主符号化工具 | `llvm-addr2line` / `ndk-stack` | `atos` / `symbolicatecrash` | `WinDbg` / Visual Studio |
| 匹配验证 | 同一次构建产出 | UUID 匹配 | GUID+age 匹配 |
| 线上收集服务 | Firebase Crashlytics | Crashlytics / Xcode | Sentry / Backtrace |

---

## Unity Editor 崩溃 vs Player 崩溃

这两类在工作流上有重要区别：

**Editor 崩溃（开发阶段）**

- 通常有完整的 Editor.log
- Unity 本身的 PDB 没有公开发布，引擎内部函数名不一定能看到
- 最有用的信息是崩溃前的日志输出和调用栈里的业务代码部分
- 重现优先：在 Editor 里能稳定复现的崩溃，可以直接附加调试器（Visual Studio / Rider）

**Player 崩溃（测试/发布阶段）**

- 需要从构建时保存好 symbols.zip 才能分析
- 线上崩溃依赖崩溃收集服务
- Development Build 可以附加调试器，但性能和 Release 不同

---

## HybridCLR 热更代码的崩溃定位

HybridCLR 让热更代码在 IL2CPP 解释器里运行，崩溃时的调用栈里会同时出现：

- IL2CPP 运行时函数（`il2cpp::vm::*`）
- HybridCLR 解释器函数（`hybridclr::interpreter::*`）
- IL2CPP 生成的 AOT 函数（正常的 C# 业务代码）
- 热更代码通过解释器执行（通常看不到具体的热更函数名，只能看到解释器）

**热更代码崩溃的特征：**

```
#00  <某个 AOT 函数或解释器函数>  ← 崩溃点
...
#N   hybridclr::interpreter::Interpreter::Execute  ← 解释器入口
#N+1 hybridclr::interpreter::InterpreterInvoke
#N+2 il2cpp::vm::Runtime::Invoke
```

调用栈里出现了 `hybridclr::interpreter::Interpreter::Execute`，说明崩溃发生时正在执行热更代码。

**定位热更代码崩溃的思路：**

1. 确认热更代码的执行上下文（从外层的 AOT 调用往里追）
2. 结合游戏逻辑：崩溃前触发了什么操作
3. 检查是否有 AOT 泛型缺失（`TisIl2CppFullySharedGenericAny` = 没有 AOT 实例）
4. 加日志缩小范围（热更代码可以重新打包，不需要重新出 APK）

---

## 崩溃分析的通用思维框架

不管哪个平台，崩溃分析的步骤都是：

```
1. 判断崩溃类型
   - 信号终止（native crash）：有 signal 号、backtrace 是地址
   - C++ 异常未捕获（也是 SIGABRT）：栈里有 __cxa_throw / terminate
   - 托管异常：有 C# 类名、Exception: 字样

2. 获取调用栈
   - Android：adb logcat CRASH tag
   - iOS：.ips 文件 / Xcode Organizer
   - Windows：Player.log 末尾 / .dmp 文件

3. 符号化
   - 找到 symbols.zip → 提取对应平台的符号文件
   - Android：llvm-addr2line
   - iOS：atos / symbolicatecrash
   - Windows：WinDbg + PDB

4. 读调用栈
   - 从下往上是调用方向
   - #00 是崩溃点（最晚调用）
   - 帧地址重复 = 栈溢出 = 往递归/死循环方向查

5. 定位根因
   - 单帧崩溃：看崩溃点函数的代码
   - 栈溢出：找递归链里的出口缺失
   - 启动崩溃：优先查初始化顺序、版本兼容性
   - 热更相关：查 AOT 泛型、DLL 加载顺序
```

---

## 系列位置

- 上一篇：[崩溃分析 Windows 篇｜minidump、WinDbg、PDB 完整流程]({{< relref "engine-notes/crash-analysis-03-windows.md" >}})
- 系列入口：[崩溃分析基础｜信号、异常、托管与 native，先把概念底座立住]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}})

---

## 延伸阅读

这个系列讲的是通识框架，具体的真实案例可以看 HybridCLR 系列的：

[HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出，一次完整的 native crash 符号化分析]({{< relref "engine-notes/hybridclr-case-typeload-and-async-native-crash.md" >}})

——那篇记录了一次真实的从闪退到符号化到定位根因的完整过程。
