---
date: "2026-03-26"
title: "崩溃分析 Windows 篇｜minidump、WinDbg、PDB 完整流程"
description: "崩溃分析系列第 3 篇。Windows native crash 的完整分析链路：Unity Editor/Player 崩溃日志的位置，minidump (.dmp) 文件的生成与读取，用 WinDbg / Visual Studio 加载 PDB 做符号化，以及 Windows Error Reporting 的崩溃收集。"
weight: 53
featured: false
tags:
  - "Crash"
  - "Debug"
  - "Windows"
  - "NativeCrash"
  - "minidump"
  - "WinDbg"
  - "PDB"
  - "Unity"
  - "IL2CPP"
series: "CrashAnalysis"
---
> Windows 的崩溃调试工具链是三个平台里最成熟的，但同时也是文档最分散的——WinDbg 的命令语法和 Unity 崩溃日志的位置都不直观。

这是崩溃分析系列第 3 篇，专注 Windows 平台。

前置概念在 [第 0 篇]({{< relref "engine-toolchain/crash-analysis-00-what-is-a-crash.md" >}}) 里。

## Windows 崩溃的两类场景

开发和发布阶段遇到的崩溃在工具链上有差异：

| 场景 | 可用工具 | 符号情况 |
|------|----------|----------|
| Unity Editor 崩溃 | Editor.log、crash dump、Visual Studio | Unity 有 PDB，IL2CPP 视构建类型 |
| Windows Player 发布版崩溃 | minidump、WinDbg、WER | 需要自己保存 PDB |
| Windows Player 开发版崩溃 | Visual Studio Debugger | PDB 完整，可直接附加调试 |

---

## Unity 崩溃日志的位置

Unity 在崩溃时会尝试写 log 文件。先找到这些文件是分析的第一步。

### Editor 崩溃日志

```
# Windows 上的 Editor.log 位置
%LOCALAPPDATA%\Unity\Editor\Editor.log

# 上一次的 Editor.log（覆盖前的版本）
%LOCALAPPDATA%\Unity\Editor\Editor-prev.log
```

Editor.log 里有崩溃前的最后几行日志，有时足以定位问题。

### Player 崩溃日志

Unity Player 在 Windows 上的日志位置：

```
# 默认路径（Development Build）
%APPDATA%\..\LocalLow\<CompanyName>\<ProductName>\Player.log

# 或
%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Player.log
```

Unity 崩溃时，如果来得及，会在 Player.log 末尾写：

```
========== OUTPUTTING STACK TRACE ==================

0x00007FF71234ABCD (GameAssembly) UnityEngine::Camera::Render
0x00007FF712345678 (GameAssembly) ...

========== END OF STACKTRACE ===========
```

这些地址和 Android/iOS 一样，需要用 PDB 做符号化。

### Crash dump 文件位置

Unity 在 Windows 上崩溃时可能产出 `.dmp` 文件（minidump）：

```
# Player 崩溃时的默认 dump 位置
%APPDATA%\..\LocalLow\<CompanyName>\<ProductName>\

# 或 Windows Error Reporting 的 dump
%LOCALAPPDATA%\CrashDumps\
%APPDATA%\Microsoft\Windows\WER\ReportArchive\
```

---

## Windows 的符号文件：PDB

Windows 平台使用 `.pdb`（Program Database）作为符号文件，对应 Android 的带符号 `.so`，iOS 的 `.dSYM`。

Unity IL2CPP 打包时产出的 PDB：

```
# 从 *-IL2CPP.symbols.zip 解压后
symbols/
  Windows/
    GameAssembly.pdb       ← IL2CPP 生成代码的符号
    UnityPlayer.pdb        ← Unity 引擎本体（可选）
```

PDB 需要和对应的 `.exe` / `.dll` 精确匹配（通过 GUID + age 校验，类似 iOS 的 UUID 机制）。

---

## Step 1：读 Player.log 里的栈

```
========== OUTPUTTING STACK TRACE ==================

0x00007FF6ABCD1234 (GameAssembly) (function-name not available)
0x00007FF6ABCD5678 (UnityPlayer) UnityEngine::Application::Quit
0x00007FF6ABCD9ABC (GameAssembly) (function-name not available)

========== END OF STACKTRACE ===========
```

Player.log 里的栈通常**没有符号**（因为 release 构建的 dll 已经 strip 了），显示 `(function-name not available)`。

需要用 minidump + PDB 来得到完整符号化结果。

---

## Step 2：生成 / 获取 minidump

### 方法 1：让 Unity 自动生成

Unity 的 Windows Player 默认会生成 minidump，位置在上述路径。

也可以在代码里主动触发（开发调试时）：

```csharp
// 需要通过 P/Invoke 调用 MiniDumpWriteDump
// 或者用 Unity 的 CrashReporter API（不同版本接口有差异）
```

### 方法 2：Windows Error Reporting (WER) 自动收集

Windows 10/11 有内置的崩溃收集机制，会把 dump 写入：

```
%LOCALAPPDATA%\Microsoft\Windows\WER\ReportArchive\
%LOCALAPPDATA%\CrashDumps\
```

在注册表里可以配置 WER 的 dump 级别：

```reg
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps]
"DumpType"=dword:00000002          ; 2 = Full dump
"DumpCount"=dword:00000010         ; 保存最近 16 个
"DumpFolder"="C:\\CrashDumps"
```

### 方法 3：用 ProcDump 手动抓取（测试阶段）

```cmd
# 安装 Sysinternals ProcDump
# 监控进程，崩溃时自动生成 dump
procdump -e -ma -w MyGame.exe C:\CrashDumps\
```

---

## Step 3：用 WinDbg 分析 minidump

WinDbg 是 Windows 上最强大的调试器，处理 dump 文件的标准工具。

### 安装 WinDbg

从 Microsoft Store 安装 **WinDbg Preview**（推荐，现代界面）：
- 搜索 "WinDbg Preview" 安装
- 或从 Windows SDK 获取

### 加载 dump 文件

```
File → Open Dump File → 选择 .dmp 文件
```

或命令行：

```cmd
windbg -z C:\path\to\crash.dmp
```

### 设置符号路径

WinDbg 使用微软符号服务器 + 本地 PDB：

```
# 在 WinDbg 命令行里设置符号路径
.sympath srv*C:\symbols*https://msdl.microsoft.com/download/symbols;C:\path\to\GameAssembly.pdb

# 重新加载符号
.reload /f
```

或者通过 GUI：File → Symbol File Path

### 常用分析命令

```
# 查看崩溃时的调用栈（最常用）
k

# 带参数和本地变量的调用栈
kv

# 查看所有线程的调用栈
~*k

# 查看异常信息
!analyze -v

# 查看寄存器
r

# 查看内存（address 替换为实际地址）
dd <address>   ; 显示 DWORD 格式
db <address>   ; 显示 byte 格式

# 反汇编当前 PC 附近的指令
u @rip

# 查看模块列表
lm
```

**最实用的命令是 `!analyze -v`**，WinDbg 会自动分析崩溃原因并给出建议：

```
0:000> !analyze -v
*******************************************************************************
*                                                                             *
*                        Exception Analysis                                  *
*                                                                             *
*******************************************************************************

FAULTING_IP:
GameAssembly!SomeFunction+0x123
00007ff6`12345678 48890a          mov qword ptr [rdx],rcx

EXCEPTION_RECORD:
ExceptionCode: c0000005 (Access violation)
ExceptionFlags: 00000000
...
STACK_TEXT:
00 (Inline Function) --------`-------- GameAssembly!SomeFunction+0x123
01 00007ff6`12345abc GameAssembly!CallerFunction+0x456
```

---

## Step 4：用 Visual Studio 分析 dump（更友好的界面）

如果不熟悉 WinDbg 命令行，Visual Studio 也可以打开 dump 文件：

1. File → Open → File → 选择 `.dmp` 文件
2. 在 "Minidump File Summary" 页面点击 **"Debug with Managed Only"** 或 **"Debug with Native Only"**
3. 设置符号路径：Debug → Options → Debugging → Symbols → 添加 PDB 所在目录
4. Visual Studio 会自动符号化并显示调用栈

**优势**：直接跳转到源代码（如果有的话），比 WinDbg 直观。

**劣势**：对 minidump 的支持比 WinDbg 弱，某些 dump 类型无法完整分析。

---

## Step 5：用 llvm-addr2line 分析 Player.log 里的地址

如果只有 Player.log 里的地址，也可以用 `llvm-addr2line`（Unity 内置 NDK 里有 Windows 版）：

```bash
# Windows 版 llvm-addr2line 路径
<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/NDK/
  toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-addr2line.exe

# 但它处理的是 ELF 格式（.so），不是 PE 格式（.dll/.exe）
# 对 Windows 的 GameAssembly.dll，需要用 Windows 工具链
```

对于 Windows 平台，推荐用 Microsoft 的 `addr2line` 等价工具 `llvm-symbolizer`（也在 LLVM 工具链里）：

```cmd
llvm-symbolizer.exe --obj=GameAssembly.pdb 0x00007FF6ABCD1234
```

或者用 `dia2dump`（Visual Studio 自带，处理 PDB）：

```cmd
# 在 Visual Studio 的 Developer Command Prompt 里
dia2dump /l GameAssembly.pdb
```

---

## Windows Unity 崩溃的特殊情况

### Managed Exception 在 Windows 上的表现

在 Windows Standalone Player 里，未捕获的 C# 异常会：
1. 打印到 Player.log（`E Unity: ...Exception...`）
2. 如果是 Development Build，可能弹出错误对话框
3. 不会触发 minidump（这不是 native crash）

### Editor 崩溃 vs Player 崩溃

- **Editor 崩溃**：通常有 `Editor.log` + 可能有 dump，但 Unity Editor 的 PDB 不公开分发，函数名可能无法完全显示
- **Player 崩溃**：需要自己在构建时保存好 PDB + symbols.zip，否则无法符号化

### IL2CPP symbols.zip 里的 Windows 符号

```
symbols/
  Windows/
    x86_64/
      GameAssembly.pdb
      GameAssembly.dll  （可选，有时也在里面）
```

这是 Unity 打 Windows 包时产出的，要和对应的安装包精确匹配。

---

## 崩溃收集服务（Windows 发布版）

对于发布的 Windows 游戏，需要崩溃收集服务自动上报：

| 服务 | 特点 |
|------|------|
| **Sentry** | 开源可自建，支持上传 PDB，Native crash 支持好 |
| **Backtrace.io** | 专注游戏 crash 分析，Unity 有官方插件 |
| **BugSplat** | 专注 C++ 和游戏 native crash |
| **Firebase Crashlytics** | 主要是移动端，Windows 支持较弱 |

这些服务的工作原理：
1. 在游戏里集成 SDK
2. 崩溃时 SDK 生成 minidump 并上传
3. 你提前上传 PDB 到服务平台
4. 平台自动符号化并展示分析结果

---

## 完整操作清单

```
1. 先看 Player.log / Editor.log 末尾的 stack trace
2. 找 .dmp 文件（%LocalAppData%\CrashDumps 或 WER 目录）
3. 打开 WinDbg → .sympath 设置 PDB 路径 → !analyze -v
4. 用 k 或 kv 查看完整调用栈
5. 识别模式：
   - 单帧崩溃 = 定位那个函数的问题
   - 栈帧重复 = 递归/栈溢出
   - Access violation at 0 = 空指针
   - Access violation at 高地址 = 越界或野指针
6. 对照符号化结果和源码定位根因
```

---

## 系列位置

- 上一篇：[崩溃分析 iOS 篇｜.dSYM、atos、symbolicatecrash 完整流程]({{< relref "engine-toolchain/crash-analysis-02-ios.md" >}})
- 下一篇：[崩溃分析 Unity + IL2CPP 篇｜symbols.zip、global-metadata.dat 和三平台统一视角]({{< relref "engine-toolchain/crash-analysis-04-unity-il2cpp.md" >}})
