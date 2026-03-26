+++
date = 2026-03-26
title = "崩溃分析 Android 篇｜adb logcat、tombstone、llvm-addr2line 完整流程"
description = "崩溃分析系列第 1 篇。从 adb logcat 过滤 CRASH tag 开始，到 tombstone 文件提取，到用 llvm-addr2line / ndk-stack 做符号化，再到 Firebase Crashlytics 的在线符号化，走完 Android native crash 的完整分析链路。"
weight = 51
featured = false
tags = ["Crash", "Debug", "Android", "NativeCrash", "Symbols", "adb", "Unity", "IL2CPP"]
series = "CrashAnalysis"
+++

> Android 的崩溃日志不会主动推到你面前——你得知道去哪里找，用什么过滤，才能拿到有用的东西。

这是崩溃分析系列第 1 篇，专注 Android 平台。

前置概念在 [第 0 篇]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}}) 里，这里直接讲操作。

## 工具准备

| 工具 | 来源 | 用途 |
|------|------|------|
| `adb` | Android SDK Platform Tools | 连接设备、抓 logcat |
| `llvm-addr2line` | Unity 内置 NDK | 地址 → 函数名 |
| `ndk-stack` | Android NDK | 批量符号化 logcat 输出 |
| `*-IL2CPP.symbols.zip` | Unity 打包产出 | 带调试符号的 libil2cpp.so |

Unity 内置 NDK 路径（Windows）：

```
<Unity安装目录>/Editor/Data/PlaybackEngines/AndroidPlayer/NDK/
  toolchains/llvm/prebuilt/windows-x86_64/bin/
    llvm-addr2line.exe
    llvm-objdump.exe
```

---

## Step 1：连接设备，实时抓日志

```bash
adb devices           # 确认设备在线
adb logcat -c         # 清空历史日志（可选）
adb logcat            # 开始实时输出
```

日志量通常很大，用 `grep` 或 `-s` 参数过滤：

```bash
# 只看 Unity 托管层日志
adb logcat -s Unity

# 只看崩溃信息（native crash）
adb logcat | grep -E "CRASH|FATAL|signal [0-9]"

# 把完整日志输出到文件（推荐，便于后续分析）
adb logcat > crash.log
```

---

## Step 2：识别 native crash 的 log 结构

```
E CRASH: *** *** *** *** *** *** *** *** *** *** *** *** *** ***
E CRASH: Version '2022.3.60f1', Build type 'Release', Scripting Backend 'il2cpp'
E CRASH: pid: 1234, tid: 5678, name: UnityMain  >>> com.example.game <<<
E CRASH: signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr --------
E CRASH: Cause: null pointer dereference
E CRASH:
E CRASH: backtrace:
E CRASH:   #00 pc 000000000123abcd  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #01 pc 00000000009f1234  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #02 pc 00000000009f5678  /data/app/.../lib/arm64/libil2cpp.so
```

几个关键字段：

| 字段 | 含义 |
|------|------|
| `signal 11 (SIGSEGV)` | 信号类型，见第 0 篇信号表 |
| `fault addr --------` | 虚线 = 空指针；有具体地址 = 野指针或越界 |
| `Cause:` | 内核对崩溃原因的猜测（不总是准确） |
| `backtrace:` | 调用栈，每行一帧，`#00` 是崩溃点 |
| `.so` 文件路径 | 帮助确认崩溃在哪个模块 |

**地址重复出现**（如 `#02`、`#05`、`#08` 是同一个地址）= 栈溢出，往递归/死循环方向查，不是真的空指针。

---

## Step 3：提取带符号的 .so 文件

Unity 打包时会在构建输出目录产出 `*-IL2CPP.symbols.zip`，解压可得：

```
symbols/
  arm64-v8a/
    libil2cpp.so        ← 带 debug 符号，用于符号化
  armeabi-v7a/
    libil2cpp.so
```

> 注意：设备上安装的 `libil2cpp.so` 是 strip 版本（去掉了符号），用于减小包体。带符号的版本只在 `symbols.zip` 里。

确认架构匹配：crash log 里的路径如果是 `arm64/libil2cpp.so`，就用 `arm64-v8a` 目录里的 .so。

---

## Step 4：用 llvm-addr2line 符号化

```bash
ADDR2LINE="<Unity_NDK>/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-addr2line.exe"
LIB="<symbols_dir>/arm64-v8a/libil2cpp.so"

# 单个地址
$ADDR2LINE -f -C -e "$LIB" 000000000123abcd

# 参数含义：
# -f  输出函数名（function name）
# -C  C++ demangle，把 _ZN...E 转成可读函数名
# -e  指定带符号的二进制
```

**批量查多个地址：**

```bash
for addr in 0123abcd 009f1234 009f5678; do
  echo -n "$addr  →  "
  $ADDR2LINE -f -C -e "$LIB" $addr
done
```

符号化结果示例：

```
0123abcd  →  AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted<...>
             /path/to/generated_cpp/Bulk_Assembly-CSharp_0.cpp:4891

009f1234  →  hybridclr::interpreter::Interpreter::Execute(...)
             /path/to/HybridCLR/interpreter/Interpreter.cpp:2134

009f5678  →  hybridclr::interpreter::InterpreterInvoke(...)
             /path/to/HybridCLR/interpreter/Interpreter.cpp:2981
```

---

## Step 5：用 ndk-stack 批量符号化（更快）

`ndk-stack` 可以直接处理 logcat 的输出，不用手动逐行查地址：

```bash
# 方法一：实时符号化（设备连接状态下）
adb logcat | ndk-stack -sym <symbols_dir>/arm64-v8a

# 方法二：对已保存的 log 文件处理
ndk-stack -sym <symbols_dir>/arm64-v8a < crash.log
```

输出会在每行地址后面自动插入函数名和文件行号：

```
#00  AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted  Bulk_Assembly-CSharp_0.cpp:4891
#01  hybridclr::interpreter::Interpreter::Execute  Interpreter.cpp:2134
```

---

## Step 6：读 tombstone 文件（更完整的信息）

logcat 里的 backtrace 有时会被截断，或者崩溃发生时 logcat 没有开。这时可以从设备读取 **tombstone 文件**。

tombstone 是 Android 在 native crash 时写入设备的完整崩溃转储，包含：
- 完整 backtrace（不截断）
- 所有寄存器值（PC、SP、LR、x0-x28）
- 崩溃时的内存 dump
- 所有线程的栈

```bash
# tombstone 在设备上的位置
adb shell ls /data/tombstones/

# 拉取最新的 tombstone 文件（通常是序号最大的）
adb pull /data/tombstones/tombstone_07 .

# 或拉取所有
adb pull /data/tombstones/ ./tombstones/
```

tombstone 文件是纯文本，直接打开即可查看。把里面的地址送给 `llvm-addr2line` 做符号化，流程与 logcat 相同。

> 注意：`/data/tombstones/` 通常需要 root 权限或通过 `adb bugreport` 获取。

**通过 adb bugreport 获取（无 root）：**

```bash
adb bugreport bugreport.zip
# 解压后在 FS/data/tombstones/ 目录里
```

---

## Step 7：Firebase Crashlytics（线上崩溃）

真实上线的 app，崩溃通常是用户在使用时发生的，无法连 adb。这时需要崩溃收集服务。

Firebase Crashlytics 的流程：

1. **上传符号**（集成阶段，每次发布包时）：

```bash
# Crashlytics Gradle 插件会在 assembleRelease 后自动上传
# 也可以手动上传
firebase crashlytics:symbols:upload \
  --app <APP_ID> \
  <symbols_zip_path>
```

2. **查看崩溃**：在 Firebase Console 的 Crashlytics 面板里，已上传符号的崩溃会自动显示函数名和行号。

3. **下载原始 crash 报告**：通过 Firebase Admin SDK 或 Crashlytics REST API 可以拿到原始报告做进一步分析。

Unity 项目的 Crashlytics 接入要点：
- `libil2cpp.so` 的带符号版本需要从 `IL2CPP.symbols.zip` 里提取后上传
- 符号要与发布 APK 完全对应（同一次构建产出）
- 每次打包都要重新上传符号

---

## 常见 signal 和典型原因

| 信号 | 在 Android Unity 项目里常见原因 |
|------|---------------------------------|
| `SIGSEGV` | 空指针 dereference、栈溢出（帧地址重复）、野指针、越界 |
| `SIGABRT` | `abort()` 主动调用、C++ 未捕获异常、断言失败 |
| `SIGBUS` | ARM 内存对齐错误，通常是不对齐的内存访问 |
| `SIGILL` | 执行了非法指令，常见于错误的函数指针、内存损坏后跳转 |

---

## 完整操作清单

```
1. adb logcat > crash.log          # 抓日志
2. grep "CRASH\|signal" crash.log  # 确认是 native crash
3. 找到 backtrace，提取地址列表
4. 解压 *-IL2CPP.symbols.zip → arm64-v8a/libil2cpp.so
5. llvm-addr2line -f -C -e libil2cpp.so <addr>   # 逐个或批量
6. 根据符号化结果读调用栈（从下往上）
7. 识别模式（地址重复 = 栈溢出；单帧崩溃 = 单点问题）
8. 结合代码定位根因
```

---

## 系列位置

- 上一篇：[崩溃分析基础｜信号、异常、托管与 native，先把概念底座立住]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}})
- 下一篇：[崩溃分析 iOS 篇｜.dSYM、atos、symbolicatecrash 完整流程]({{< relref "engine-notes/crash-analysis-02-ios.md" >}})
