---
date: "2026-03-26"
title: "崩溃分析 iOS 篇｜.dSYM、atos、symbolicatecrash 完整流程"
description: "崩溃分析系列第 2 篇。iOS native crash 的完整分析链路：从 Xcode Organizer 和 TestFlight 拿到崩溃报告，用 atos / symbolicatecrash 做符号化，处理 Unity IL2CPP 的 dSYM 特殊情况，以及 dSYM 和 crash report 的 UUID 匹配问题。"
weight: 52
featured: false
tags:
  - "Crash"
  - "Debug"
  - "iOS"
  - "NativeCrash"
  - "dSYM"
  - "Symbols"
  - "atos"
  - "Unity"
  - "IL2CPP"
series: "CrashAnalysis"
---
> iOS 的崩溃诊断相比 Android 多了一层：`.dSYM` 和崩溃报告的 UUID 必须完全匹配，不然符号化结果全是问号。

这是崩溃分析系列第 2 篇，专注 iOS 平台。

前置概念在 [第 0 篇]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}}) 里，Android 流程在 [第 1 篇]({{< relref "engine-notes/crash-analysis-01-android.md" >}})。

## 工具准备

| 工具 | 来源 | 用途 |
|------|------|------|
| Xcode | App Store | Organizer 查看崩溃，symbolicatecrash |
| `atos` | Xcode Command Line Tools | 单地址符号化 |
| `symbolicatecrash` | Xcode 内置 | 批量符号化 .ips/.crash 文件 |
| `dwarfdump` | Xcode Command Line Tools | 查 dSYM 的 UUID |
| `*-IL2CPP.symbols.zip` | Unity 打包产出 | Unity 项目的符号文件 |

---

## iOS 崩溃报告的来源

### 1. 设备直连（开发阶段）

Xcode → Window → Devices and Simulators → 选择设备 → View Device Logs

或者连接设备后：

```bash
# Xcode 会自动同步崩溃日志到
~/Library/Logs/DiagnosticReports/
```

### 2. Xcode Organizer（TestFlight / App Store）

Xcode → Window → Organizer → Crashes

App Store 的崩溃会以汇总形式展示，可以看到已符号化的 stack trace（前提是 Xcode 有对应的 dSYM）。

### 3. TestFlight 崩溃

TestFlight 崩溃会同步到 App Store Connect，也会出现在 Xcode Organizer 里。

### 4. 手动从设备提取

```bash
# 设备上的崩溃日志路径（iOS 15+）
# 通过 Xcode 的设备日志查看器，或用 idevicecrashreport（libimobiledevice）

idevicecrashreport -e ./crash_logs/
```

---

## iOS 崩溃报告格式

iOS 的崩溃报告（`.ips` 或 `.crash` 文件）是 JSON 或纯文本格式：

```
Incident Identifier: 12345678-ABCD-EF01-2345-6789ABCDEF01
Hardware Model:      iPhone15,2
Process:             MyGame [1234]
Path:                /private/var/containers/Bundle/Application/.../MyGame.app/MyGame
Identifier:          com.example.mygame
Version:             1.0.0 (100)
OS Version:          iPhone OS 17.0

Exception Type:  EXC_BAD_ACCESS (SIGSEGV)
Exception Subtype: KERN_INVALID_ADDRESS at 0x0000000000000000
Termination Reason: Namespace SIGNAL, Code 11 Segmentation fault

Thread 0 Crashed:
0   GameAssembly                  0x0000000102345678 0x100000000 + 36394616
1   GameAssembly                  0x000000010198abcd 0x100000000 + 26280909
2   GameAssembly                  0x00000001019a1234 0x100000000 + 26347060
3   libdispatch.dylib             0x00000001a1234567 _dispatch_call_block_and_release + 32
...
```

关键字段：

| 字段 | 含义 |
|------|------|
| `Exception Type: EXC_BAD_ACCESS (SIGSEGV)` | 信号类型 |
| `KERN_INVALID_ADDRESS at 0x0000...0000` | fault addr 为 0 = 空指针 |
| `Thread 0 Crashed:` | 崩溃发生在哪个线程 |
| 每行的第三列地址（如 `0x0000000102345678`） | 符号化要用的地址 |
| 第四列 `0x100000000 + 36394616` | load address + offset |

---

## dSYM：iOS 符号文件

iOS 的符号文件是 `.dSYM`（DWARF with dSYM），和 Android 的带符号 `.so` 功能类似，但格式不同。

每个 `.dSYM` 包里有一个 UUID，和它对应的二进制文件（app/dylib）的 UUID 完全绑定。**UUID 不匹配，符号化失败**，什么函数名都查不到。

### 查 dSYM 的 UUID

```bash
dwarfdump --uuid MyGame.app.dSYM/Contents/Resources/DWARF/MyGame
# 输出：
# UUID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX (arm64) MyGame
```

### 查 crash report 里的 UUID

crash 文件末尾的 `Binary Images` 段里有每个模块的 UUID：

```
Binary Images:
0x100000000 - 0x10xxxxxxx GameAssembly arm64
    <XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX>
    /var/containers/Bundle/.../GameAssembly
```

两个 UUID 必须完全一致才能符号化。

### Unity 项目的 dSYM

Unity iOS 项目导出 Xcode 工程，打包时会产出：

- `GameAssembly.dSYM`（Unity 2020+，IL2CPP 生成的 native 代码）
- `UnityFramework.dSYM`（Unity 引擎本体）

这两个 dSYM 在 Xcode 的 Archive 里，路径在：

```
~/Library/Developer/Xcode/Archives/<日期>/<AppName> <时间>.xcarchive/dSYMs/
```

如果使用 Unity 的 `*-IL2CPP.symbols.zip`，解压后的 iOS 符号路径一般是：

```
symbols/
  iOS/
    GameAssembly.dSYM/
    UnityFramework.dSYM/
```

---

## Step 1：用 atos 符号化单个地址

`atos` 是 macOS 上的地址符号化工具，用法：

```bash
atos -arch arm64 \
     -o <path/to/GameAssembly.dSYM/Contents/Resources/DWARF/GameAssembly> \
     -l <load_address> \
     <crash_address>
```

从 crash 报告里提取参数：

```
# crash 报告里的一行：
# 0   GameAssembly  0x0000000102345678  0x100000000 + 36394616
#                   ^—— crash_address   ^—— load_address
```

```bash
atos -arch arm64 \
     -o GameAssembly.dSYM/Contents/Resources/DWARF/GameAssembly \
     -l 0x100000000 \
     0x0000000102345678
```

输出：

```
AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted (in GameAssembly) (Bulk_Assembly-CSharp_0.cpp:4891)
```

**批量查多个地址：**

```bash
atos -arch arm64 \
     -o GameAssembly.dSYM/Contents/Resources/DWARF/GameAssembly \
     -l 0x100000000 \
     0x0000000102345678 \
     0x000000010198abcd \
     0x00000001019a1234
```

---

## Step 2：用 symbolicatecrash 批量符号化整个 crash 文件

`symbolicatecrash` 可以处理整个 `.crash` 或 `.ips` 文件，不需要手动提取每个地址：

```bash
# 找到 symbolicatecrash 的位置
SYMBOLICATECRASH=$(find /Applications/Xcode.app -name "symbolicatecrash" 2>/dev/null | head -1)

# 设置必要的环境变量
export DEVELOPER_DIR="$(xcode-select -p)"

# 运行符号化
$SYMBOLICATECRASH crash_report.ips GameAssembly.dSYM > symbolicated.crash
```

符号化后的文件里，原来的地址被替换为函数名和行号：

```
Thread 0 Crashed:
0   GameAssembly  AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted (Bulk_Assembly-CSharp_0.cpp:4891)
1   GameAssembly  hybridclr::interpreter::Interpreter::Execute (Interpreter.cpp:2134)
2   GameAssembly  hybridclr::interpreter::InterpreterInvoke (Interpreter.cpp:2981)
```

**常见问题：符号化后仍然是地址**

原因通常是：
1. dSYM UUID 与 crash 里的 UUID 不匹配
2. dSYM 不在 `~/Library/Developer/Xcode/DerivedData` 或 Spotlight 可搜索的位置
3. 架构不对（armv7 vs arm64）

排查方法：

```bash
# 检查 dSYM UUID
dwarfdump --uuid MyApp.dSYM

# 检查 crash 文件里的 UUID
grep -A2 "Binary Images" crash_report.ips | grep GameAssembly
```

---

## Step 3：Xcode Organizer 的自动符号化

如果 Xcode 本地有对应的 Archive（包含 dSYM），Organizer 会自动符号化从 App Store / TestFlight 收集到的崩溃。

在 Organizer 里看到的 crash stack 已经是函数名，可以直接分析。

如果 Organizer 里看到的是地址（没有符号化），说明本地没有对应的 dSYM——需要从构建时保存的 Archive 里提取 dSYM，或者从 App Store Connect 下载。

**从 App Store Connect 下载 dSYM：**

```bash
# 通过 Xcode 界面：
# Xcode → Organizer → Archives → 选择版本 → Download Debug Symbols

# 或通过命令行（需要 altool / notarytool）
xcrun altool --download-dsym ...
```

---

## Step 4：Crash Reporter 和 Firebase Crashlytics

**Firebase Crashlytics（iOS）的符号化流程：**

1. 构建时上传 dSYM：

```bash
# Xcode Build Phase 里添加脚本（Crashlytics 集成时自动添加）
"${PODS_ROOT}/FirebaseCrashlytics/run"

# 或手动上传
./Pods/FirebaseCrashlytics/upload-symbols \
  -gsp GoogleService-Info.plist \
  -p ios \
  GameAssembly.dSYM
```

2. 上传后，Firebase Console 里的崩溃会自动显示符号化结果。

Unity 项目注意：`GameAssembly.dSYM` 和 `UnityFramework.dSYM` 都需要上传，否则引擎层的栈帧无法显示函数名。

---

## 读 iOS 崩溃栈：几个模式

### 模式 1：单点崩溃（空指针、越界）

```
Thread 0 Crashed:
0   GameAssembly  SomeMethod (SomeClass.cpp:123)   ← 崩溃点
1   GameAssembly  CallerMethod (Caller.cpp:456)    ← 调用方
```

→ 从 `#0` 开始，找该函数里可能解引用的指针或数组访问。

### 模式 2：栈溢出（递归/死循环）

```
Thread 0 Crashed:
0   GameAssembly  FunctionA (...)
1   GameAssembly  FunctionB (...)
2   GameAssembly  FunctionA (...)   ← 同一函数再次出现
3   GameAssembly  FunctionB (...)
4   GameAssembly  FunctionA (...)
...（重复数百帧）
```

→ 不是真的访问了坏内存，是栈空间耗尽，触发 guard page 访问保护。找 A/B 之间的循环依赖。

### 模式 3：OOM（内存不足）

```
Exception Type:  EXC_RESOURCE
Exception Subtype: MEMORY
Termination Reason: Namespace SPRINGBOARD, Code 0x8badf00d
```

→ 这不是 native crash，是系统因内存压力强制终止进程（watchdog 或 jetsam）。没有崩溃点可分析，需要看内存使用曲线。

---

## 系列位置

- 上一篇：[崩溃分析 Android 篇｜adb logcat、tombstone、llvm-addr2line 完整流程]({{< relref "engine-notes/crash-analysis-01-android.md" >}})
- 下一篇：[崩溃分析 Windows 篇｜minidump、WinDbg、PDB 完整流程]({{< relref "engine-notes/crash-analysis-03-windows.md" >}})
