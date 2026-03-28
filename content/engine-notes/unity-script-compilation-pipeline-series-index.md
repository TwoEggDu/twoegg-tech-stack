---
date: "2026-03-28"
title: "Unity 脚本编译管线系列索引｜从改一行代码到编辑器可用，中间发生了什么"
description: "给 Unity 脚本编译管线系列补一个稳定入口：先说明 asmdef、Roslyn、bee_backend、ILPP、Domain Reload 这条链怎么读，再列出当前全部文章。"
slug: "unity-script-compilation-pipeline-series-index"
weight: 61
featured: false
tags:
  - "Unity"
  - "Compilation"
  - "ILPP"
  - "Domain Reload"
  - "Index"
series: "Unity 脚本编译管线"
series_id: "unity-script-compilation-pipeline"
series_role: "index"
series_order: 0
series_nav_order: 140
series_title: "Unity 脚本编译管线"
series_entry: true
series_audience:
  - "Unity 客户端开发"
  - "构建 / 排障"
series_level: "基础"
series_best_for: "当你想搞清楚 Unity 编辑器里脚本编译的完整链路，或者遇到编译卡死想知道从哪里看"
series_summary: "把 Unity 编辑器里的脚本编译链——从 asmdef 切分、Roslyn 编译、ILPP 字节码注入，到 Domain Reload——收进一张可查的地图"
series_intro: "这组文章处理的不是某个 Unity 菜单怎么点，而是把"你保存了一个 .cs 文件，到编辑器重新可用"这段等待时间里发生的事情拆开：asmdef 如何决定哪些脚本一起编，Roslyn 把 C# 变成什么，bee_backend 怎么调度增量编译，ILPP 为什么要偷偷改你的字节码，以及 Domain Reload 是什么、为什么代价那么高。最后一篇给出从日志定位卡死位置的实操方法。只有这张地图先立住，遇到编译慢、卡死、改代码进 Play Mode 等待这些问题时，才知道从哪里入手。"
series_reading_hint: "第一次读建议按 01 → 02 → 03 → 04 顺序读；如果你现在就是编译卡死，可以直接跳第 04 篇；如果只是想搞清楚 ILPP 是什么，直接看第 02 篇。"
---
> 这组文章的出发点只有一个：当你在 Unity 编辑器里保存一个 `.cs` 文件，那段等待时间里到底发生了什么。

这是 Unity 脚本编译管线系列第 0 篇。
它不补新的技术细节，只做一件事：

`给这条编译链补一个稳定入口，让你知道先读哪篇、遇到什么问题该跳去哪篇。`

## 这个系列为什么存在

Unity 的脚本编译不是一个黑盒按钮，而是一条有明确分层的链路：asmdef 决定编译边界，Roslyn 把 C# 编译成 IL，bee_backend 调度增量构建，ILPP 在产物上做字节码注入，最后 Domain Reload 把一切装进编辑器可用的状态。

这条链上的每一环都有自己的代价和失效模式。把它们拆开看，才能在遇到"编译慢""卡死""进 Play Mode 要等"这类问题时，知道该从哪里入手，而不是靠重启编辑器碰运气。

## 先补一个前置

这组文章会在必要处提到 IL 和 .NET 程序集，但它本身不是 IL 入门。
如果你对下面这些问题还没有稳定直觉：

- `.dll` 文件里到底装的是什么
- C# 编译之后、运行之前，代码以什么形式存在
- IL 和机器码的关系是什么

那建议先补这篇前置，后面的内容会顺很多：

- [构建与调试前置 02c｜.NET 程序集与 IL：C# 代码到可执行文件中间那层是什么]({{< relref "engine-notes/build-debug-02c-dotnet-assembly-and-il.md" >}})

## 推荐阅读顺序

如果你是第一次系统读，建议按下面这个顺序：

0. 当前这篇索引
   先建地图。

1. [Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么]({{< relref "engine-notes/unity-script-compilation-pipeline-01-overview.md" >}})
   先把整条链的全貌立住：asmdef、Roslyn、bee_backend、ILPP、Domain Reload 各自在哪个位置，彼此是什么关系。

2. [Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-notes/unity-script-compilation-pipeline-02-ilpp.md" >}})
   专门把 ILPP 这一环拆开：它改的是什么、为什么要在编译之后改、改完的产物是什么。

3. [Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-notes/unity-script-compilation-pipeline-03-domain-reload.md" >}})
   把 Domain Reload 的代价讲清楚：它在做什么、为什么慢、为什么不能简单跳过，以及 Enter Play Mode 选项改变了哪些部分。

4. [Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环]({{< relref "engine-notes/unity-script-compilation-pipeline-04-debug-hang.md" >}})
   这是实操篇。前三篇的分层在这里变成可用的排查工具：日志在哪里看，哪一行日志对应哪一环，卡死时该先怀疑哪里。

## 如果你带着问题来查

### 你只想先知道 ILPP 是什么

直接看第 02 篇：

- [Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-notes/unity-script-compilation-pipeline-02-ilpp.md" >}})

### 你现在就是编译卡死，想知道怎么看

直接看第 04 篇：

- [Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环]({{< relref "engine-notes/unity-script-compilation-pipeline-04-debug-hang.md" >}})

### 你想搞清楚为什么进 Play Mode 那么慢

看第 03 篇：

- [Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-notes/unity-script-compilation-pipeline-03-domain-reload.md" >}})

### 你想从头看清楚整条链

从第 01 篇开始顺读：

- [Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么]({{< relref "engine-notes/unity-script-compilation-pipeline-01-overview.md" >}})

{{< series-directory >}}
