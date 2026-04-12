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
series_summary: "把 Unity 编辑器和打包的脚本编译链——从 asmdef 切分、Roslyn 编译、ILPP 字节码注入、Domain Reload，到 Mono/IL2CPP 打包路径和 .asmdef 性能设计——收进一张可查的地图"
series_intro: >-
  这组文章处理的不是某个 Unity 菜单怎么点，而是把“你保存了一个 .cs 文件，到编辑器重新可用”这段等待时间里发生的事情拆开，以及点击 Build 之后发生了什么：asmdef 如何决定哪些脚本一起编，Roslyn 把 C# 变成什么，bee_backend 怎么调度增量编译，ILPP 为什么要偷偷改你的字节码，Domain Reload 是什么、为什么代价那么高，Mono 和 IL2CPP 打包路径如何分叉，如何通过 .asmdef 设计让增量编译更快，CI 上如何正确缓存编译产物，以及编译报错和编译机器人的完整实践。只有这张地图先立住，遇到编译慢、卡死、报错、CI 打包慢这类问题时，才知道从哪里入手。
series_reading_hint: "第一次读建议按 01 → 02 → 03 → 04 顺序读完基础链路，再按需选读后续篇；遇到编译卡死跳 04，想优化速度看 06-07，搭建打包系统看 09。"
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

- [构建与调试前置 02c｜.NET 程序集与 IL：C# 代码到可执行文件中间那层是什么]({{< relref "engine-toolchain/build-debug-02c-dotnet-assembly-and-il.md" >}})

## 推荐阅读顺序

如果你是第一次系统读，建议按下面这个顺序：

0. 当前这篇索引
   先建地图。

1. [Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么]({{< relref "engine-toolchain/unity-script-compilation-pipeline-01-overview.md" >}})
   先把整条链的全貌立住：asmdef、Roslyn、bee_backend、ILPP、Domain Reload 各自在哪个位置，彼此是什么关系。

2. [Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-toolchain/unity-script-compilation-pipeline-02-ilpp.md" >}})
   专门把 ILPP 这一环拆开：它改的是什么、为什么要在编译之后改、改完的产物是什么。

3. [Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-toolchain/unity-script-compilation-pipeline-03-domain-reload.md" >}})
   把 Domain Reload 的代价讲清楚：它在做什么、为什么慢、为什么不能简单跳过，以及 Enter Play Mode 选项改变了哪些部分。

4. [Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环]({{< relref "engine-toolchain/unity-script-compilation-pipeline-04-debug-hang.md" >}})
   这是实操篇。前三篇的分层在这里变成可用的排查工具：日志在哪里看，哪一行日志对应哪一环，卡死时该先怀疑哪里。

5. [Unity 脚本编译管线 05｜点击 Build 之后：Mono 与 IL2CPP 的编译路径分叉]({{< relref "engine-toolchain/unity-script-compilation-pipeline-05-player-build.md" >}})
   从编辑器编译延伸到打包：Scripting Backend 如何决定走 Mono 还是 IL2CPP，IL2CPP 路径里 IL 是怎么变成机器码的，以及为什么打包比编辑器编译慢得多。

6. [Unity 脚本编译管线 06｜.asmdef 设计：如何分包让增量编译更快]({{< relref "engine-toolchain/unity-script-compilation-pipeline-06-asmdef-design.md" >}})
   把前面的原理转成可操作的设计建议：如何分 .asmdef 才能让增量编译影响范围最小，Editor-only、第三方库、测试代码各该怎么处理。

7. [Unity 脚本编译管线 07｜CI 编译缓存：Library 哪些能缓存、哪些不能]({{< relref "engine-toolchain/unity-script-compilation-pipeline-07-ci-cache.md" >}})
   CI 上全量重编的根源和解法：哪些 Library 子目录值得缓存、缓存 key 怎么设计、Jenkins 和 GitHub Actions 的具体配置。

8. [Unity 脚本编译管线 08｜编译报错排查：从错误信息定位根因]({{< relref "engine-toolchain/unity-script-compilation-pipeline-08-compilation-errors.md" >}})
   把编译错误分成五类（CS 错误、找不到程序集、循环依赖、运行时类型缺失、ILPP 失败），每类给出识别特征和处理思路。

9. [Unity 脚本编译管线 09｜编译机器人实践：从触发到通知的全链路]({{< relref "engine-toolchain/unity-script-compilation-pipeline-09-build-robot.md" >}})
   游戏团队打包系统的完整实践：触发层（IM Bot）、调度层（队列/优先级）、执行层（Unity 专属注意事项）、通知层（失败摘要/责任人推断）。

## 如果你带着问题来查

### 你只想先知道 ILPP 是什么

直接看第 02 篇：

- [Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-toolchain/unity-script-compilation-pipeline-02-ilpp.md" >}})

### 你现在就是编译卡死，想知道怎么看

直接看第 04 篇：

- [Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环]({{< relref "engine-toolchain/unity-script-compilation-pipeline-04-debug-hang.md" >}})

### 你想搞清楚为什么进 Play Mode 那么慢

看第 03 篇：

- [Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-toolchain/unity-script-compilation-pipeline-03-domain-reload.md" >}})

### 你想优化编译速度 / 减少等待

看第 06 篇（本地设计）+ 第 07 篇（CI 缓存）：

- [Unity 脚本编译管线 06｜.asmdef 设计：如何分包让增量编译更快]({{< relref "engine-toolchain/unity-script-compilation-pipeline-06-asmdef-design.md" >}})
- [Unity 脚本编译管线 07｜CI 编译缓存：Library 哪些能缓存、哪些不能]({{< relref "engine-toolchain/unity-script-compilation-pipeline-07-ci-cache.md" >}})

### 你遇到了编译报错

看第 08 篇：

- [Unity 脚本编译管线 08｜编译报错排查：从错误信息定位根因]({{< relref "engine-toolchain/unity-script-compilation-pipeline-08-compilation-errors.md" >}})

### 你要搭建打包机器人系统

看第 09 篇：

- [Unity 脚本编译管线 09｜编译机器人实践：从触发到通知的全链路]({{< relref "engine-toolchain/unity-script-compilation-pipeline-09-build-robot.md" >}})

### 你想了解打包时的编译链

看第 05 篇：

- [Unity 脚本编译管线 05｜点击 Build 之后：Mono 与 IL2CPP 的编译路径分叉]({{< relref "engine-toolchain/unity-script-compilation-pipeline-05-player-build.md" >}})

### 你想从头看清楚整条链

从第 01 篇开始顺读：

- [Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么]({{< relref "engine-toolchain/unity-script-compilation-pipeline-01-overview.md" >}})

{{< series-directory >}}
