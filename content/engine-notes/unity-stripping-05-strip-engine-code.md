---
date: "2026-03-26"
title: "Unity 裁剪 05｜Strip Engine Code 到底在裁什么"
description: "顺着 Unity 当前实现把 Strip Engine Code 的工作方式串起来：Editor 怎样准备输入、linker 怎样做模块决策、结果又怎样影响后续原生构建。"
slug: "unity-stripping-05-strip-engine-code"
weight: 54
featured: false
tags:
  - Unity
  - Build
  - Stripping
  - IL2CPP
series: "Unity 裁剪"
---

> 如果这篇只记一句话，我建议记这个：`Strip Engine Code` 不是对现成 `libunity` 再跑一次普通 `strip`，而是 `Editor 准备依赖信息 -> linker 做模块/类型决策 -> 生成更小的注册代码 -> 再进入 native build` 这条实现链。

前四篇，我们基本都在讲 `managed stripping`：

- 第 01 篇先把三层 `strip` 地图拆开
- 第 02 篇讲 `Managed Stripping Level`
- 第 03 篇讲 Unity 当前到底能自动保留哪些入口
- 第 04 篇讲哪些代码模式最怕 strip，以及怎样写得更适合裁剪

这一篇把最后一个坑也填上：

`Strip Engine Code 到底在裁什么？`

很多人看到这个开关时，直觉都会把它理解成：

“把已经生成好的 `libunity` 或 player binary，再跑一遍更狠的原生精简。”

这个理解抓到了一点“结果会更小”，但没抓到真正关键的实现位置。

如果顺着 Unity 当前源码往下看，你会发现这件事更接近：

`让 linker 先根据项目实际依赖，决定哪些引擎模块和原生类型注册需要保留；再把这个决策重新写回注册源码，最后进入原生构建。`

也就是说，它不是简单的后处理，而是：

`build graph 改写`

## 这篇要回答什么

这一篇主要回答四个问题：

1. `Strip Engine Code` 为什么是 `IL2CPP-only`。
2. Editor 在跑 linker 之前，究竟准备了哪些输入。
3. `UnityLinker` 在这条链里扮演的到底是什么角色。
4. 为什么它本质上不是“对现成二进制做普通 strip”。

如果先压成最短答案，可以写成这样：

- Unity 会先整理场景类型、原生类型和模块依赖这些输入信息。
- 跑 linker 时会进入引擎模块裁剪模式。
- linker 会产出一份更小的引擎裁剪结果。
- 后续原生构建会据此生成更小的注册代码，再并入 `IL2CPP` / native build。

这条链的重点不在“最后删掉了几个符号”，而在：

`最终进入原生构建的注册代码，本来就已经变小了。`

## 先看最外层边界：它为什么是 IL2CPP-only

这件事其实 Unity 自己文档已经写得很直接。

Unity 的公开说明里，`stripEngineCode` 的定义就是：

`Remove unused Engine code from your build (IL2CPP-only).`

但只看文档还不够。更重要的是当前实现里这条边界是怎么真正落下来的。

从构建条件上看，这条链要同时满足几个前提：

- 平台真的支持 engine stripping
- 当前后端不是 Mono
- 你真的打开了 `stripEngineCode`

这几条合起来表达的是：

`只有平台声明支持 engine stripping，而且当前不是 Mono backend，同时你真的打开了 stripEngineCode，这条链才会生效。`

这也是为什么我不建议把它理解成“所有后端最后都会有的一层原生精简”。

它不是。

它从开关定义到运行条件，都是：

`跟 IL2CPP player build 绑定的。`

所以这里的关键不是“有个全局 strip 开关”，而是：

`IL2CPP 构建管线会专门把这个开关带进 linker 和后续 native build。`

## 先给一张实现链

如果把 `Strip Engine Code` 当前实现里的路径压成一张最简图，大概是这样：

`项目里的场景/类型/模块信息`
-> `linker`
-> `引擎裁剪结果`
-> `更小的原生注册代码`
-> `IL2CPP / native build`

这里最重要的不是文件名本身，而是两个事实：

1. linker 不是只在删 managed assemblies，它还会产出供 Editor 回读的引擎裁剪结果。
2. native build 吃到的不是“原样引擎 + 最后做个 strip”，而是已经被裁剪决策改写过的注册源码。

## 一、Editor 在喂什么给 UnityLinker

很多人一提到 linker，脑子里只会想到：

`DLL 进，DLL 出。`

但看这条构建链，你会发现 Editor 其实额外准备了不少信息。

### 1. 先准备 managed stripping 那几类已知保留输入

先是 managed stripping 那几类已知保留输入，会被整理给 linker：

- 场景类型清单
- 序列化类型清单
- 需要额外保留的方法清单
- `Assets/**/link.xml`
- 以及 build pipeline 额外生成的保留声明

这部分我们在前几篇已经讲过，它解决的是：

`哪些托管入口、类型和方法应该别被误删。`

### 2. 再额外准备一份给引擎裁剪用的项目与引擎关系数据

真正和 `Strip Engine Code` 直接相关的，是 Editor 还会额外准备一份“项目到底用了哪些引擎能力”的输入信息。

里面至少有四类数据：

| 数据 | 来源 | 它告诉 linker 什么 |
| --- | --- | --- |
| 场景里实际用到的类型 | 构建期场景与资源分析 | 这些类型最终落在哪些原生类型和模块上 |
| 当前可参与裁剪的原生类型信息 | 引擎类型元数据 | Unity 当前有哪些原生类型、属于哪个模块、继承关系是什么 |
| 强制保留模块 | 模块配置 | 哪些模块被明确要求保留 |
| 强制排除模块 | 模块配置 | 哪些模块被明确要求排除 |

这张表很关键，因为它说明：

`Strip Engine Code` 的输入不是“对一个现成引擎二进制做黑盒分析”，而是 Editor 先把项目和引擎的关系显式写出来。

再往下一点看这些数据的结构，信号会更清楚。

它不仅会记录场景里实际出现的类型，还会把这些类型和对应的原生类型、模块归属、场景来源一起串起来。

这意味着这条链关注的不是“任意 C++ 符号”，而是更靠上的东西：

- 场景到底依赖了哪些 Unity native class
- 这些 native class 又落在哪些 engine module 上

这已经非常不像“后处理 strip”了，更像：

`带场景语义和模块语义的注册裁剪。`

### 3. 模块依赖描述文件也会被一起喂进去

除了这些项目输入，Unity 还会把一份模块依赖描述文件一起交给 linker。

从后续生成逻辑也能看出来，这份文件记录的正是模块之间的依赖关系。

所以这里真正参与决策的，至少有三类信息：

- 项目到底用了哪些场景类型和托管入口
- Unity 原生类型和模块之间的映射
- 模块之间的依赖关系

如果只是对现成二进制跑普通 `strip`，这些信息根本不需要提前进入链路。

## 二、UnityLinker 在这里到底扮演什么角色

前面几篇我们一直把 `UnityLinker` 当成 managed stripping 的执行者。

到了 `Strip Engine Code` 这一层，它的角色要再往前走一步：

`它不只是在删托管程序集，还在基于 Editor 提供的数据，给出一份“引擎模块/类型保留结果”。`

这件事在参数层面就能直接看到。

无论走哪条构建路径，核心语义都一样：

- 把项目与引擎关系数据交给 linker
- 把模块依赖信息交给 linker
- 明确告诉 linker：这次除了 managed strip，还要顺手做 engine module stripping decision

这说明两条构建路径虽然代码结构不同，但表达的是同一件事：

`让 linker 在这次构建里顺手做 engine module stripping decision。`

### linker 还有一个关键输出：一份回传给后续构建的引擎裁剪结果

如果只看这条链，会发现 linker 不只是消费 Editor 给它的输入，它还会把裁剪结果再回传给后续构建步骤。

这件事本身就很说明问题了：

`不是只有 Editor 把数据喂给 linker，linker 也会把结果再回传给 Editor。`

这就是为什么我更愿意把 `Strip Engine Code` 理解成：

`Editor <-> UnityLinker 之间一轮关于引擎模块和原生类型注册的协作决策。`

而不是一句很模糊的：

“linker 顺手把 engine 删小了。”

## 三、linker 的结果怎样重新变成更小的原生注册代码

这一段是整条链最能说明问题的地方。

如果 `Strip Engine Code` 真的是“对现成 `libunity` 再跑一次普通 strip”，那你应该看到的是：

- 生成完原生库
- 再跑一个平台工具
- 然后把没用的符号或 section 扔掉

但 Unity 当前源码里最关键的一步，其实是：

`重新生成注册源码。`

### 1. 后续构建会显式调用一层注册代码生成步骤

如果平台支持 engine stripping，后续构建里会专门有一步去消费 linker 的裁剪结果，再生成更小的注册代码。

也就是说，最终那份原生注册源码根本不是一份固定模板文件。

它是：

`吃了 linker 输出结果之后，现算出来的一份构建产物。`

### 2. 生成器会根据裁剪结果，只写回真正需要的模块和类型注册

这一步的核心不是“再拷一份旧文件”，而是：

- 只为保留下来的模块写入注册调用
- 只为真正需要的原生类型保留注册项
- 最后生成一份更小的原生注册源码

看到这里，基本就可以把误解掐掉了。

因为这一步处理的根本不是：

- 调试符号
- 已生成二进制里的 section
- 一个现成 `libunity` 文件的 bytes

它处理的是：

`最终要编译进 player 的 C++ 注册源。`

### 3. 这份生成文件会被重新并入 IL2CPP / native build

最后这些新生成的注册源码会被重新并入 IL2CPP / native build。

也就是说，后续原生构建不是拿一份固定不变的引擎注册代码继续编译，而是：

`拿这次根据项目依赖重新生成过的注册代码去编译。`

这也是为什么我会把整条链的中心句写成：

`Strip Engine Code = linker 决策 + 更小的模块/类型注册源码 + 再进入 native build。`

## 四、为什么它不是“对 libunity 再跑一次普通 strip”

如果把上面几段压成一个对照表，这个边界会更清楚：

| 问题 | Strip Engine Code | 普通 native symbol strip |
| --- | --- | --- |
| 发生在什么时候 | linker 决策和 native build 之间 | 原生链接之后 |
| 处理对象是什么 | 引擎模块保留、原生类型注册源码 | 最终库 / 可执行文件里的符号 |
| 是否理解场景与模块依赖 | 会，Editor 会显式提供这些数据 | 通常不会 |
| 关键产物是什么 | 裁剪结果、重新生成的注册源码 | `stripped.so`、符号文件等 |
| 更像什么 | 构建图重写 | 二进制后处理 |

从这张表里最该带走的一点是：

`Strip Engine Code` 真正变小的，不只是“最后的文件”，而是“进入最终原生构建的那套注册与模块引用关系”。

更直白一点说：

- 普通 native strip 是“已经做完饭，再把包装裁掉一点”
- `Strip Engine Code` 更像是“下锅前就改了这顿饭到底做哪些菜”

当然，这里我还是要严谨一点。

从当前源码能直接确认的，是：

- linker 会得到模块/原生类型相关输入
- linker 会产出回写给 Editor 的结果
- 更小的原生注册源码会基于这个结果被重新生成并参与后续原生构建

因此我这里的结论是一个非常强的实现推断：

`最终 player 变小，核心不是“现成 libunity 被二次瘦身”，而是原生注册和模块引用图在构建前就被改写了。`

这个推断和源码链是对得上的。

## 五、工程上该怎么判断问题是不是这一层

把这条链看清之后，排查顺序也会比以前稳很多。

### 1. 先确认你是不是 IL2CPP

如果项目跑的是 Mono backend，那就别先把精力花在 `Strip Engine Code` 上。

这层本来就不是主要嫌疑人。

### 2. 如果现象更像“引擎能力或原生类型注册缺失”，再来查这层

更值得怀疑 `Strip Engine Code` 的现象通常是：

- 开启 `stripEngineCode` 后，某些 Unity 内建能力只在 IL2CPP player 里表现异常
- 包体或原生库大小明显变化，同时问题和引擎模块进入与否强相关
- 关闭 `stripEngineCode` 后问题消失

这类问题和前几篇那种“某个 C# 方法被反射不到”很像，但本质可能已经不是同一层。

### 3. 真要查，优先看的是模块和注册输入，不只是 `[Preserve]`

很多团队一出问题就本能去补：

- `[Preserve]`
- `link.xml`
- 降低 `Managed Stripping Level`

这些动作对 managed stripping 很有用，但对这层未必是第一抓手。

如果怀疑的是 `Strip Engine Code`，更该先看的通常是：

- 当前 backend 和平台是否真的支持 engine stripping
- `stripEngineCode` 有没有实际进入这次构建的裁剪链路
- 场景和模块信息有没有被构建阶段正确收集
- 是否存在模块 include / exclude 的特殊覆盖

也就是说，排查重点已经从：

`托管入口保没保住`

变成：

`模块和原生类型注册结果有没有被改写错。`

## 这一篇最该带走的三句话

如果把这篇文章最后再压成三句话，我建议记住这三句：

- `Strip Engine Code` 不是对现成 `libunity` 再跑一次普通 `strip`，而是 Unity 构建链里一条专门的引擎模块/原生类型注册裁剪路径。
- 这条路径是 `IL2CPP-only`，而且要同时满足平台支持、backend 不是 Mono、`stripEngineCode` 开启等条件才会生效。
- 它当前实现里最关键的链条是：`项目输入 -> linker 裁剪决策 -> 更小的原生注册源码 -> native build`。

到这里，Unity 裁剪这组文章的主线就算闭环了。

前四篇讲的是：

- 哪层在裁
- managed stripping 怎么裁
- 为什么 Unity 有时看不懂动态依赖
- 怎样把代码写得更适合裁剪

这一篇补上的，是最后一层：

`引擎模块和原生类型注册，究竟是怎样被裁进去的。`

## 系列导航

- 上一篇：<a href="{{< relref "engine-notes/unity-stripping-04-strip-friendly-code-patterns.md" >}}">Unity 裁剪 04｜哪些 Unity 代码最怕 Strip，以及怎样写得更适合裁剪</a>
- 下一篇：无。到这里，系列主线就收住了；工程决策建议优先从实战篇进入。
