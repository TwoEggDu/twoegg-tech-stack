---
date: "2026-03-28"
title: "Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么"
description: "从一次脚本修改出发，拆解 Unity 编辑器的编译链：Assembly Definition 如何切割编译单元，Roslyn 做什么，bee_backend 是什么，编译产物放在哪，以及哪些改动会触发完整重编。"
slug: "unity-script-compilation-pipeline-01-overview"
weight: 62
featured: false
tags:
  - "Unity"
  - "Build"
  - "Compilation"
  - "Roslyn"
  - "Assembly Definition"
series: "Unity 脚本编译管线"
series_order: 1
---

> 如果这篇只记一句话，我建议记这个：`Unity 编译脚本不是把所有 .cs 一把扔给编译器，而是先按程序集切分、再增量调度、最后还要改字节码。`

你在 Unity 编辑器里改了一行 C#，按下 Ctrl+S，然后低头喝了口水。

抬起头，进度条还在转。

如果你遇到过这种场景——改一行等十几秒、有时候改了不生效、有时候全部重编——那这篇文章就是为你写的。

它不需要你了解编译器原理，只需要你会写 C#、用过 Unity。

## 这篇要回答什么？

1. Unity 为什么要把脚本"分组"编译，而不是一起编？
2. 它调用的是哪个 C# 编译器，输出的是什么？
3. `bee_backend` 是什么东西，为什么你在任务管理器里能看到它？
4. 哪些操作会触发全量重编，哪些只会重编一部分？

---

## 第一站：切分编译单元——Assembly Definition

Unity 不是把项目里所有 `.cs` 文件丢给编译器一起编。

它的编译单位是**程序集（Assembly）**，而划分程序集的依据是 `.asmdef` 文件（`Assembly Definition`）。

规则很简单：

- 一个文件夹里放了 `.asmdef`，该文件夹及其子文件夹下的脚本就属于这个程序集。
- 没有任何 `.asmdef` 覆盖的脚本，全部默认归入 `Assembly-CSharp`。

| 有没有 .asmdef | 归属程序集 |
| --- | --- |
| 有，自定义名称 | 该 .asmdef 定义的程序集 |
| 没有 | `Assembly-CSharp`（默认程序集） |
| Package 内部脚本 | Package 自带的 .asmdef 定义的程序集 |

**为什么这很重要？**

因为程序集之间有依赖关系。当你修改某个文件时：

- 只有**包含该文件的程序集**需要重新编译。
- **依赖它的程序集**也需要重新编译（向下传播）。
- **不依赖它的程序集**完全不受影响。

如果你的项目全部脚本都在 `Assembly-CSharp` 里，改一行 = 整个大程序集重编。这就是"改一行等很久"的最常见原因之一。

```
Assembly-CSharp（你的大部分游戏逻辑）
  ↑ 依赖
MyUI.asmdef（UI 相关代码）
  ↑ 依赖
MyCore.asmdef（核心工具类）
```

改了 `MyCore.asmdef` 里的一个工具函数 → `MyCore` + `MyUI` + `Assembly-CSharp` 都要重编。
改了 `Assembly-CSharp` 里的逻辑 → 只有 `Assembly-CSharp` 重编。

合理拆分 `.asmdef`，能让大多数日常修改只触发一小部分重编。

---

## 第二站：Roslyn——把 C# 变成 IL

程序集划好了，接下来就是真正的编译。

Unity 使用的 C# 编译器是 `Roslyn`，这是微软的开源 C# 编译器平台（也是 Visual Studio 和 .NET SDK 在用的那个）。

`Roslyn` 的工作：

```
.cs 源文件（若干个）
    ↓  Roslyn
.dll 文件（包含 IL 字节码 + 元数据）
```

`IL`（Intermediate Language，中间语言）是 .NET 世界里的"通用字节码"。它不是机器码，而是一种平台无关的中间表示，之后由运行时（Mono 或 IL2CPP）再做进一步处理。

这一步和你用 `dotnet build` 编译一个普通 .NET 项目没有本质区别：输入是 C# 源文件，输出是装有 IL 的 `.dll`。

Unity 不改 `Roslyn` 本身。`Roslyn` 的工作做完后，Unity 才会在后续步骤里对 `.dll` 动手（那是下一篇要讲的 ILPP）。

---

## 第三站：bee_backend——增量构建调度器

如果你在编译时打开任务管理器，会看到一个叫 `bee_backend.exe` 的进程。

`bee_backend` 是 Unity 自研的增量构建系统，基于 [Tundra](https://github.com/deplinenoise/tundra) 演化而来。

它的职责不是编译代码本身，而是**决定哪些东西要重新处理、用什么顺序、能不能并行**。

可以把它理解成一个"构建任务调度员"：

```
bee_backend 收到任务清单
    ↓
检查每个输入文件的签名（内容哈希）
    ↓
和上次记录的签名对比
    ↓
只有签名变化的输入 → 触发对应的编译任务
没有变化的输入     → 直接跳过
    ↓
有依赖关系的任务按顺序执行
没有依赖关系的任务并行执行
```

**增量编译的关键**：`bee_backend` 用文件内容哈希（而不是时间戳）判断是否需要重建。这意味着：

- 你改了一个文件又改回去 → 哈希没变 → 不重编。
- 你只改了注释 → 哈希变了 → 会重编（这是正常代价）。

---

## 编译产物在哪里？

Unity 的编译产物分两个位置：

| 目录 | 存放内容 |
| --- | --- |
| `Library/ScriptAssemblies/` | 编辑器直接使用的 `.dll` 文件（运行游戏、Inspector 反射等都从这里读） |
| `Library/Bee/` | `bee_backend` 的工作目录，含中间产物、依赖图、构建日志等 |

`Library/ScriptAssemblies/` 是你最常会间接碰到的地方——当 Unity 提示"找不到程序集"或者出现版本冲突时，通常就和这个目录有关。

`Library/Bee/` 一般不需要手动去看，但如果你遇到编译缓存损坏的问题，删掉它重来是常见的排查手段之一。

---

## 什么会触发重编？

| 触发原因 | 重编范围 |
| --- | --- |
| 脚本内容改变 | 该脚本所在程序集 + 依赖它的程序集 |
| `.asmdef` 配置改变（依赖项、名称等） | 受影响的程序集 + 下游依赖 |
| Package 版本升级 | Package 本身的程序集 + 下游依赖 |
| Player Settings 中的 `Define Symbols` 改变 | **全量重编**（所有程序集） |
| Unity 版本升级 | **全量重编** |

`Define Symbols`（脚本宏定义）那一行要特别注意：每次修改都会触发所有程序集的完整重编，因为任何一个 `.cs` 文件都可能用 `#if` 依赖这些符号。如果你需要频繁切换宏，合理使用 `.asmdef` 也可以在一定程度上减少范围（但无法完全规避）。

---

## 编译完成之后

当 `bee_backend` 完成所有编译任务，`Library/ScriptAssemblies/` 里的 `.dll` 已经更新。但脚本在编辑器里真正可以运行，还需要两步：

1. **ILPP（IL Post-Processing）**：Unity 和各个 Package 会对刚编译出来的 `.dll` 进行字节码后处理——往里注入代码、改写调用、添加诊断逻辑等。这是下一篇要专门讲的内容。

2. **Domain Reload**：Unity 卸载旧的程序集，重新加载所有新 `.dll`，并重建整个 C# 运行环境。这也是你进入 Play Mode 有时需要等待的原因，第 03 篇会详细拆解。

这两步都发生在 `Roslyn` 编译之后、代码真正执行之前。你在编辑器里等待的"进度条时间"，往往是这三步加在一起：编译 + ILPP + Domain Reload。

---

## 整个流程一览

```
你改了一行 C#，保存
    ↓
Unity 检测到文件变化
    ↓
bee_backend 确认哪些程序集需要重新处理
    ↓
Roslyn 编译受影响的程序集：.cs → .dll（含 IL）
    ↓
ILPP：各 Package 对 .dll 做字节码后处理  ← 下一篇
    ↓
Domain Reload：卸载旧程序集，加载新程序集  ← 第 03 篇
    ↓
编辑器恢复可用，代码生效
```

---

## 小结

1. Unity 以**程序集**为单位编译脚本，`.asmdef` 决定边界；没有 `.asmdef` 的脚本默认进 `Assembly-CSharp`。
2. 编译器是微软的 `Roslyn`，输出是包含 IL 字节码的 `.dll`，这一步和普通 .NET 项目无异。
3. `bee_backend` 是 Unity 的增量构建调度器，用内容哈希判断是否需要重编，并调度并行任务。
4. 编译产物在 `Library/ScriptAssemblies/`（编辑器使用）和 `Library/Bee/`（中间产物）。
5. 修改 `Define Symbols` 会触发**全量重编**；合理拆分 `.asmdef` 是减少等待时间的最直接手段。
6. 编译完成后还有 ILPP 和 Domain Reload 两步，才算真正生效。

---

- 下一篇：[Unity 脚本编译管线 02｜ILPP：Unity 为什么要改你的字节码]({{< relref "engine-toolchain/unity-script-compilation-pipeline-02-ilpp.md" >}})
