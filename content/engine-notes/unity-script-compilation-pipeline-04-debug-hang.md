---
date: "2026-03-28"
title: "Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环"
description: "当 Unity 编辑器编译脚本卡住不动时，如何从 Editor.log 判断是卡在 Roslyn 编译、bee_backend 调度、ILPP 字节码处理还是 Domain Reload 阶段，以及每个阶段常见的卡死原因和排查方向。"
slug: "unity-script-compilation-pipeline-04-debug-hang"
weight: 65
featured: false
tags:
  - "Unity"
  - "Compilation"
  - "Debugging"
  - "ILPP"
  - "bee_backend"
  - "Domain Reload"
series: "Unity 脚本编译管线"
series_order: 4
---

> 编译卡住时，进度条告诉你"还在转"，日志才告诉你"卡在哪"。

---

## 这篇要回答什么

1. Unity 编译链分几个阶段？每个阶段在日志里长什么样？
2. 怎么从日志判断卡在哪一个阶段？
3. 每个阶段最常见的卡死原因是什么？
4. 拿到一份卡死日志，实际该怎么一步步查下去？

---

## 先找到日志文件

在开始分析之前，你需要知道日志在哪。

- **Windows**：`%LOCALAPPDATA%\Unity\Editor\Editor.log`
  完整路径通常是 `C:\Users\<用户名>\AppData\Local\Unity\Editor\Editor.log`
- **macOS**：`~/Library/Logs/Unity/Editor.log`
- **打包机 / CI**：Unity 启动时通过 `-logFile <路径>` 参数指定，具体路径看你的构建脚本

日志是实时写入的，编译过程中可以用 `tail -f` 持续跟踪，不需要等编译结束。

```bash
# macOS / Linux
tail -f ~/Library/Logs/Unity/Editor.log

# Windows PowerShell
Get-Content "$env:LOCALAPPDATA\Unity\Editor\Editor.log" -Wait
```

---

## 编译链的四个阶段

Unity 脚本编译从你保存代码到 Editor 可以运行，依次经过四个阶段：

```
源码变动
  ↓
阶段 1：Roslyn 编译（bee_backend 驱动）
  ↓
阶段 2：ILPP 字节码处理
  ↓
阶段 3：bee_backend 收尾，输出 ExitCode
  ↓
阶段 4：Domain Reload
  ↓
编译完成，Editor 可用
```

每个阶段都在 `Editor.log` 里留下可识别的标志字符串，下面逐个说明。

---

### 阶段 1：Roslyn 编译（bee_backend 启动）

`bee_backend` 是 Unity 的构建调度器，它负责调用 Roslyn 把 `.cs` 源码编译成 `.dll`。

**日志里的标志字符串：**

```
Starting: "C:/path/to/bee_backend.exe" ... ScriptAssemblies ...
```

你会在日志中看到 `bee_backend.exe` 的完整调用命令行，参数中包含 `ScriptAssemblies`。

**正常结束的样子：**

```
ExitCode: 0 Duration: 12.3s
```

或者：

```
ExitCode: 4 Duration: 8.1s
```

`ExitCode: 0` 表示全部成功。`ExitCode: 4` 是 `bee_backend` 的"部分失败"退出码，在 ILPP 配置发生变化、需要重建 DAG 时也会出现，不代表一定有代码错误。

**卡住时的样子：**

`bee_backend` 启动行出现之后，日志长时间没有新输出，没有 `ExitCode` 行。这通常是磁盘 IO 问题或文件被其他进程锁住。

---

### 阶段 2：ILPP 字节码处理

ILPP（IL Post Processing）在 Roslyn 编译完成后对每个 `.dll` 做字节码注入，比如 Burst、NetCode、自定义的 ILPostProcessor。每个 DLL 对应一次 HTTP 请求（`bee_backend` 向 ILPP 宿主进程发起请求）。

**日志里的标志字符串：**

```
Request starting HTTP/2 POST http://ilpp/UnityILPP.PostProcessing/PostProcessAssembly
```

一个 DLL 处理完之后，会紧跟一行：

```
Request finished in Xms 200
```

正常情况下，每条 `Request starting` 后面都会有对应的 `Request finished`，耗时通常在 **1ms 到 20ms** 之间。对于体积较大或注入逻辑较重的 DLL，可能到几百毫秒，但不会超过几秒。

**卡住时的样子：**

日志停在某一条 `Request starting` 后面，迟迟没有出现 `Request finished`。或者最后一条有输出的请求后，日志彻底停止新增内容。

此时 ILPP 宿主进程还活着，但在处理某个 DLL 时陷入了耗时操作或已经崩溃。

---

### 阶段 3：bee_backend 完成

ILPP 全部处理完之后，`bee_backend` 汇总结果，输出最终的退出码。

**日志里的标志字符串：**

```
ExitCode: 0 Duration: 45.7s
```

这行出现，表示编译任务（含 ILPP）已经全部完成，接下来进入 Domain Reload。

如果这行一直没出现，说明还卡在阶段 1 或阶段 2。

---

### 阶段 4：Domain Reload

`bee_backend` 退出后，Unity Editor 的主线程开始加载新编译出来的程序集，重建脚本域。

**日志里的标志字符串：**

```
Reloading assemblies after finishing script compilation.
```

**正常结束的样子：**

```
Reload completed in X.XXX seconds
```

**卡住时的样子：**

日志停在 `Reloading assemblies after finishing script compilation.` 之后，出现某个类名或程序集名，但没有 `Reload completed`。通常是某个带 `[InitializeOnLoad]` 的类的静态构造函数在执行阻塞操作（死循环、同步网络请求、`Thread.Sleep` 等）。

---

## 最常见的卡死模式

| 卡死位置 | 日志特征 | 常见原因 |
|---------|---------|---------|
| Roslyn / bee_backend 启动阶段 | `Starting: ...bee_backend.exe` 出现，此后无新输出 | 磁盘 IO 极慢、中间目录被锁、杀毒软件拦截 |
| ILPP 某个 DLL | 有 `Request starting` 无 `Request finished` | DLL 体积过大、Burst.CodeGen 处理异常、自定义 ILPostProcessor 死循环 |
| ILPP 宿主进程整体 | ILPP 进程无响应，所有请求停止 | ILPP 宿主进程 OOM 或崩溃退出 |
| Domain Reload | 停在 `Reloading assemblies` 后某个类名 | `[InitializeOnLoad]` 死循环、同步网络调用、等待外部进程 |
| Domain Reload | 停在 `Reloading assemblies`，无类名 | 程序集依赖关系异常，加载链断裂 |

---

## ExitCode 含义速查

`bee_backend` 输出的 `ExitCode` 是判断编译阶段是否正常结束的关键。

| ExitCode | 含义 |
|---------|-----|
| `0` | 全部任务成功 |
| `4` | 部分任务失败（bee_backend 在 continue-on-failure 模式下运行），不代表源码有语法错误，ILPP 配置变更触发的 DAG 重建也可能产生此码 |
| 其他非零值 | 编译过程发生错误，需结合日志中的错误信息进一步分析 |

---

## 快速排查 Checklist

拿到一份卡死日志，按以下顺序检查：

1. **搜 `ExitCode`**
   看日志里有没有 `ExitCode` 行。如果没有，说明 `bee_backend` 还没退出，卡在阶段 1 或阶段 2。

2. **搜 `PostProcessAssembly`**
   找所有 `Request starting HTTP/2 POST http://ilpp/.../PostProcessAssembly` 行，对比是否每一条都有对应的 `Request finished`。找到只有 `starting` 没有 `finished` 的那一条，那个 DLL 就是嫌疑目标。

3. **搜 `Reloading assemblies`**
   如果这行存在，说明阶段 1-3 都已完成，卡在 Domain Reload。继续往下看日志，找到停止输出前最后出现的类名。

4. **如果卡在 ILPP**
   搜 `Processing assembly`，找最后一条记录，这个程序集就是当前正在被 ILPP 处理的 DLL，也是最可能的问题来源。

5. **如果 ILPP 宿主整体无响应**
   检查系统内存使用情况，ILPP 宿主进程可能因 OOM 已退出但没有在日志中留下明确记录。

---

## 一个真实案例走读

以下是一份简化版日志片段，展示"ILPP 阶段卡住"的典型模式。

```
[19:42:01] Starting: "C:/Unity/Editor/Data/Tools/bee_backend.exe"
           ... -dagFile ... ScriptAssemblies ...

[19:42:03] ExitCode: 4 Duration: 1.8s

[19:42:03] Refreshing native plugins compatible for Editor in normalized ...
[19:42:03] Starting: "C:/Unity/Editor/Data/Tools/bee_backend.exe"
           ... -dagFile ... ScriptAssemblies ...

[19:42:05] Request starting HTTP/2 POST http://ilpp/.../PostProcessAssembly
[19:42:05] Request finished in 3ms 200
[19:42:05] Request starting HTTP/2 POST http://ilpp/.../PostProcessAssembly
[19:42:05] Request finished in 5ms 200
...（共 87 条 starting/finished 对）
[19:42:31] Request starting HTTP/2 POST http://ilpp/.../PostProcessAssembly
           ← 日志在此截止，没有 Request finished，没有 ExitCode
```

**逐步分析：**

- 第一次 `ExitCode: 4`：ILPP 检测到配置变化，触发 DAG 重建，这是正常行为。
- `bee_backend` 第二次启动，进入 ILPP 处理循环。
- 前 87 个 DLL 都正常处理完毕（每条耗时 3-5ms）。
- 第 88 个 DLL 的 `Request starting` 出现后，日志停止新增内容。

**结论：** 卡在 ILPP 处理第 88 个程序集。此时 `bee_backend` 仍在等待 ILPP 宿主进程响应，但 ILPP 侧已无输出，很可能是该 DLL 触发了某个 `ILPostProcessor` 的异常逻辑，或 ILPP 宿主进程在处理该 DLL 时崩溃。

**下一步：** 找到第 88 条 `Request starting` 前后是否有 `Processing assembly XXX.dll` 的记录，确认具体 DLL，然后考虑临时排除相关 package 或 ILPostProcessor 来缩小范围。

---

## 小结

1. Unity 编译日志的关键路径是四段：Roslyn（bee_backend 调度）→ ILPP 字节码处理 → bee_backend 退出 → Domain Reload。
2. 判断卡在哪一段，只需依次搜索 `ExitCode`、`Request starting/finished`、`Reloading assemblies` 三类关键字。
3. `ExitCode: 4` 不等于编译失败，ILPP 配置变更时也会出现。
4. ILPP 卡死的特征是有 `Request starting` 但没有 `Request finished`；Domain Reload 卡死的特征是日志停在某个类名后。
5. CI 上排查编译卡死，优先检查内存（ILPP OOM）和磁盘 IO（bee_backend 无输出）。

---

- 上一篇：[Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-notes/unity-script-compilation-pipeline-03-domain-reload.md" >}})
- 下一篇：[Unity 脚本编译管线 05｜点击 Build 之后：Mono 与 IL2CPP 的编译路径分叉]({{< relref "engine-notes/unity-script-compilation-pipeline-05-player-build.md" >}})
