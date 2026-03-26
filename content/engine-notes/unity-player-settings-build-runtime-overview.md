---
date: "2026-03-26"
title: "Unity Player Settings 总览｜哪些参数真的会影响裁剪、构建速度和运行时"
description: "把 Unity Player Settings 里最容易混在一起的 5 个参数拆开：Scripting Backend、API Compatibility Level、IL2CPP Code Generation、C++ Compiler Configuration 和 Use Incremental GC。"
slug: "unity-player-settings-build-runtime-overview"
weight: 55
featured: false
tags:
  - Unity
  - Build
  - IL2CPP
  - Performance
---

> 如果这篇只记一句话，我建议记这个：`这 5 个 Player Settings 不是同一层参数：Scripting Backend 决定执行模型，API Compatibility 决定 API 面，IL2CPP Code Generation 和 C++ Compiler Configuration 决定原生构建取舍，而 Incremental GC 决定运行时回收节奏。`

Unity 的 Player Settings 里，有一组参数特别容易被一起讨论：

- `Scripting Backend`
- `API Compatibility Level`
- `IL2CPP Code Generation`
- `C++ Compiler Configuration`
- `Use incremental GC`

它们之所以容易混在一起，不是因为名字相似，而是因为它们都出现在同一块设置面板里。

但如果真按工程问题来分，这 5 个参数其实分别作用在：

- 托管执行模型
- 可用 API 面
- IL2CPP 生成代码策略
- 原生 C++ 编译优化档位
- 运行时 GC 调度

如果先不拆开，团队很容易把几类完全不同的问题混成一句话：

`这个 Player Settings 到底该怎么配？`

这一篇就想先把这张地图画清楚。

## 这篇要回答什么

这篇文章主要回答五个问题：

1. 这 5 个参数分别影响哪一层。
2. 哪些参数真的会影响 Unity 裁剪和构建链。
3. 哪些参数主要影响构建速度、包体和运行时性能。
4. 哪些参数经常被误会成“能解决别的层面的问题”。
5. 项目里如果只能先做一轮粗配置，应该优先怎么理解和选择。

如果先压成一个最短版答案，可以写成这样：

- `Scripting Backend` 先决定你是在聊 `Mono` 还是 `IL2CPP`。
- `API Compatibility Level` 决定你能用哪些 .NET API，以及第三方库更容易兼容哪一档。
- `IL2CPP Code Generation` 决定 IL2CPP 在“更快运行”还是“更快构建/更小代码”之间怎么取舍。
- `C++ Compiler Configuration` 决定原生编译器优化有多激进。
- `Use incremental GC` 是运行时平滑度参数，不是裁剪参数。

## 先给一张总表

如果把这 5 个设置先按“它到底控制哪一层”排一遍，大概可以先得到这样一张表：

| 参数 | 它主要控制什么 | 更直接影响什么 | 最容易被误会成什么 |
| --- | --- | --- | --- |
| `Scripting Backend` | 托管代码最终怎么执行 | `Mono` / `IL2CPP`、AOT、构建链、部分平台可选项 | “只是另一个编译开关” |
| `API Compatibility Level` | 可用 .NET API 面 | 第三方库兼容性、跨平台性、部分包体差异 | “能解决运行时性能或裁剪问题” |
| `IL2CPP Code Generation` | IL2CPP 生成 C++ 的策略 | 构建时间、生成代码体量、泛型代码运行时性能 | “等同于 stripping level” |
| `C++ Compiler Configuration` | 原生编译优化档位 | 编译时间、运行时性能、二进制大小、可调试性 | “等同于 IL2CPP Code Generation” |
| `Use incremental GC` | GC 工作如何摊到多帧 | 帧时间尖峰、运行时卡顿表现 | “能减小包体或减少代码裁剪风险” |

这张表最重要的不是背选项名，而是先记住：

`前四个都和构建链有关，但只有前一个半真正决定“你在裁什么”；最后一个主要是运行时行为，不是构建链行为。`

顺手再记一个很实用的边界：

`这些选项的可见性和具体枚举，会随 Unity 版本与目标平台变化。`

所以后面这篇文章讲的，是它们在官方文档里最稳定的职责分工，而不是保证所有平台都出现完全相同的下拉项。

## 一、Scripting Backend：先决定你是在聊 Mono 还是 IL2CPP

这一组参数里，最该先看的一定是 `Scripting Backend`。

因为它决定的不是某个小优化点，而是：

`你的 C# 代码最终是怎么执行的。`

常见的两种就是：

- `Mono`
- `IL2CPP`

如果压成最短理解，可以记成这样：

- `Mono`：C# 先编成 CIL，再由运行时去执行
- `IL2CPP`：C# 先编成 CIL，再转成 C++，最后编成本地机器码执行

这意味着它会直接影响：

- 你是不是处在 AOT 世界里
- 后面有没有 IL2CPP 这一整条原生构建链
- 某些 Player Settings 选项是不是会出现
- 某些平台上能不能选 `Mono`
- `Strip Engine Code` 这种能力是不是有意义

### 它会影响什么

从工程上看，`Scripting Backend` 至少会改变下面这些事：

- 运行时代码执行模型
- 构建产物形态
- 构建时间和迭代体验
- 某些平台的调试方式
- 某些裁剪路径是否存在

尤其在 Unity 裁剪这条线上，它最重要的意义是：

`它先决定你后面谈的是 Mono 裁剪，还是 IL2CPP + native build 这整条链。`

所以很多“为什么这个选项在我这里没有”“为什么 Strip Engine Code 不生效”的问题，第一步都不是先看 `Managed Stripping Level`，而是先看 `Scripting Backend`。

### 它不会直接决定什么

但它也不是万能旋钮。

它不会直接替你决定：

- 可用 .NET API 面
- 第三方库到底兼不兼容
- GC 是否增量
- IL2CPP 生成的 C++ 更偏向运行时还是更偏向构建速度

这些是后面另外几组参数管的。

### 实战上怎么理解

如果把它压成一句工程判断，我会这么说：

`Scripting Backend 不是“优化档位”，而是你整个构建和执行模型的分岔口。`

所以这组设置里，永远先看它。

## 二、API Compatibility Level：它管的是 API 面，不是执行模型

第二个最容易被误会的参数，是 `API Compatibility Level`。

它看起来像个偏“底层”的设置，但它真正管的是：

`你项目里能依赖哪一档 .NET API。`

更具体一点说，它最直接影响的是：

- 第三方程序集兼不兼容
- 某些 BCL API 能不能直接用
- 构建出来的可用 API 面大小
- 跨平台支持边界

从 Unity 官方文档的常见描述看，更大的 `.NET Framework` 兼容档通常意味着：

- API 更多
- 第三方老库更容易兼容
- 构建更大
- 某些额外 API 不一定所有平台都支持

而 `.NET Standard` / `.NET Standard 2.1` 这类档位通常意味着：

- API 面更收敛
- 体量更小
- 跨平台支持更稳定

### 它会影响什么

`API Compatibility Level` 最该优先关联到的问题是：

- 某个第三方 DLL 为什么引用失败
- 某个 API 为什么在编辑器里能看到，player 里却不稳定
- 为什么切回 `.NET Framework` 后某个旧库突然能用了

如果项目里一上第三方库就出各种兼容问题，这个参数通常比 `stripping` 更值得先查。

### 它不会直接决定什么

它不会直接决定：

- 你是 `Mono` 还是 `IL2CPP`
- 代码会不会被 AOT
- `Managed Stripping Level` 更不更激进
- C++ 编译优化有多强
- GC 是否增量

也就是说：

`API Compatibility Level 不是性能开关，也不是裁剪开关。`

它更像“API 和依赖兼容性边界”的设置。

### 实战上怎么理解

如果你的目标是：

- 尽量稳地跨平台
- 尽量少背历史包袱
- 只在必要时才为老库妥协

那更自然的默认思路通常是：

- 先尝试 `.NET Standard` / `.NET Standard 2.1`
- 只有第三方库确实要求更大的 API 面，再退到 `.NET Framework`

这里最该避免的误区是：

`不要把第三方兼容问题、裁剪问题和运行时性能问题都甩给 API Compatibility Level。`

## 三、IL2CPP Code Generation：它是“生成多少代码、偏向什么目标”的取舍

这个选项只在 `IL2CPP` backend 下有意义。

它真正回答的问题不是：

`要不要 IL2CPP`

而是：

`既然已经选了 IL2CPP，那生成 C++ 时更偏向运行时表现，还是更偏向构建和迭代成本？`

Unity 官方文档里常见的两档表述是：

- `Faster runtime`
- `Faster (smaller) builds`

这两个名字其实已经说得很直白了。

### Faster runtime

这档更偏向：

- 运行时性能
- 更激进地为运行时生成代码

代价通常是：

- 生成代码更多
- 构建更慢
- 对日常频繁迭代不够友好

### Faster (smaller) builds

这档更偏向：

- 更少的生成代码
- 更快的构建速度
- 更小的构建产物

但文档也明确提醒了一点：

`它可能降低泛型代码的运行时性能。`

这句话非常值得记，因为它说明这个参数的核心不是“画质档位”，而是：

`代码生成策略的取舍。`

### 它会影响什么

这个参数更直接影响的是：

- IL2CPP 生成 C++ 的体量
- 构建时间
- 包体的一部分大小
- 某些泛型路径的运行时性能

### 它不会直接决定什么

它不会直接决定：

- 代码会不会被 UnityLinker 保留
- 你能不能用某个 .NET API
- GC 行为
- 原生 C++ 编译器优化级别

所以它和 `Managed Stripping Level` 也不是一回事。

更准确的说法应该是：

- `Managed Stripping Level` 更关心“删什么”
- `IL2CPP Code Generation` 更关心“剩下的这些代码怎么生成”

## 四、C++ Compiler Configuration：它决定 IL2CPP 生成代码怎么被原生编译

如果说 `IL2CPP Code Generation` 还处在“生成什么 C++”这一层，那 `C++ Compiler Configuration` 就已经到了下一层：

`这些 C++ 最后用什么优化档位编出来。`

Unity 官方文档常见的几档是：

- `Debug`
- `Release`
- `Master`

不同平台和 Unity 版本下，具体可见项可能会有差异，但理解方式基本一致。

### Debug

更偏向：

- 更快编译
- 更少优化
- 更容易调试

代价通常是：

- 运行更慢
- 二进制更大

### Release

更偏向：

- 日常正式构建的平衡点
- 打开优化
- 运行更快
- 包体通常更小

### Master

更偏向：

- 最激进的优化
- 更长的构建时间
- 更适合 shipping 构建

官方文档甚至明确提到，在某些编译器上它会打开更激进的链接时优化。

### 它和 IL2CPP Code Generation 的区别

这两个参数经常被一起提，但它们其实不是一回事：

- `IL2CPP Code Generation` 解决的是“生成多少、生成成什么形态”
- `C++ Compiler Configuration` 解决的是“这些生成出来的 C++ 怎么优化编译”

所以从工程视角看，这两者更像是：

`代码生成策略`

加上：

`原生编译优化策略`

而不是一个东西的两个名字。

## 五、Use incremental GC：它是运行时平滑度参数，不是裁剪参数

最后这个参数最容易被放错层。

`Use incremental GC` 解决的核心问题不是：

- 构建时间
- 包体大小
- 裁剪风险

而是：

`垃圾回收造成的帧时间尖峰。`

Unity 官方文档的表述很明确：它会把 GC 工作摊到多个 frame 上，以减少 GC 相关的帧时间尖峰。

所以它更像是：

- 运行时平滑度参数
- 帧稳定性参数
- 内存回收节奏参数

### 它会影响什么

它主要会影响：

- 某些 GC 尖峰是不是更刺眼
- 某些帧卡顿是不是更容易被摊平

### 它不会直接决定什么

它不会直接决定：

- 代码是否更容易被裁掉
- 构建是否更快
- IL2CPP 生成代码多少
- 第三方库兼容性

所以如果你现在在排查的是：

- 反射路径为什么没了
- `link.xml` 为什么没保住
- bundle 类型为什么在 player 里被裁了

那 `Incremental GC` 基本不在这条问题链上。

## 六、如果只想先形成一套不容易跑偏的默认理解，可以先这样记

把前面几节压成一个工程化的理解框架，我建议先这样记：

1. 先看 `Scripting Backend`，先搞清楚自己是在 `Mono` 还是 `IL2CPP` 世界里。
2. 再看 `API Compatibility Level`，先确认第三方库和 API 面是不是站得住。
3. 如果已经是 `IL2CPP`，再用 `IL2CPP Code Generation` 决定更偏向运行时还是更偏向构建速度。
4. 然后用 `C++ Compiler Configuration` 决定原生编译到底开到多激进。
5. 最后把 `Use incremental GC` 当成运行时平滑度旋钮，不要和裁剪、API、代码生成混在一起。

如果用一句更短的话来概括就是：

`先定执行模型，再定 API 面，再定 IL2CPP 生成策略，再定原生优化档位，最后再看运行时 GC 节奏。`

## 七、如果你的目标是“更小包体、更快构建、更稳运行”，这些参数该怎么配合看

很多人真正想问的，其实不是“每个参数是什么意思”，而是：

`我到底该优先调哪一个？`

如果把目标拆开，我会更建议这样理解：

### 想先解决第三方库兼容

优先看：

- `API Compatibility Level`
- `Scripting Backend`

不要先怪：

- `stripping`
- `Incremental GC`

### 想先解决 IL2CPP 构建太慢

优先看：

- `IL2CPP Code Generation`
- `C++ Compiler Configuration`

不要先怪：

- `API Compatibility Level`
- `Incremental GC`

### 想先解决运行时帧卡顿尖峰

优先看：

- `Use incremental GC`

但也要记住：

它解决的是 GC 节奏，不是所有性能问题。

### 想先搞清 Unity 裁剪链

优先看：

- `Scripting Backend`
- `Managed Stripping Level`
- `link.xml` / `[Preserve]`

而不是先把 `Incremental GC` 或 `API Compatibility Level` 混进来。

## 这篇最该带走的三句话

如果把这篇文章最后压成三句话，我建议记住这三句：

- `Scripting Backend` 决定你是在 `Mono` 还是 `IL2CPP` 世界里，这是后面所有构建与裁剪讨论的前提。
- `API Compatibility Level` 管的是 API 面和第三方库兼容性，`IL2CPP Code Generation` 和 `C++ Compiler Configuration` 管的是 IL2CPP 后半段的生成与编译取舍，`Incremental GC` 管的是运行时回收节奏。
- 真想把 Player Settings 配对，先别把所有问题混成一个“优化开关”；先问自己当前面对的是兼容性、裁剪、构建时间，还是运行时卡顿。

## 延伸阅读

- 如果你现在真正关心的是 Unity 裁剪入口和保留策略，建议先看 <a href="{{< relref "engine-notes/unity-stripping-practice-linkxml-preserve.md" >}}">Unity 裁剪实战｜什么时候用 link.xml，什么时候用 [Preserve]</a>。
- 如果你想继续看 Unity 为什么有时看不懂动态依赖，可以接着看 [Unity 裁剪 03｜Unity 为什么有时看不懂你的反射]({{< relref "engine-notes/unity-stripping-03-why-unity-misses-reflection.md" >}})。
- 如果你想看 IL2CPP 下 `Strip Engine Code` 这一层，再看 [Unity 裁剪 05｜Strip Engine Code 到底在裁什么]({{< relref "engine-notes/unity-stripping-05-strip-engine-code.md" >}})。
