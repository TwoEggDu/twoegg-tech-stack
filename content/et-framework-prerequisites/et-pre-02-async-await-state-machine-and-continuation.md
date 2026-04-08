---
date: "2026-04-08"
title: "ET-Pre-02｜`async/await` 到底在调度什么：状态机、continuation、上下文恢复"
description: "把 async/await 放回状态机与 continuation 协议，再去理解 ETTask 在替换什么。"
slug: "et-pre-02-async-await-state-machine-and-continuation"
weight: 2
featured: false
tags:
  - "ET"
  - "Async"
  - "Continuation"
  - "CSharp"
series: "ET 前置桥接"
primary_series: "et-framework-prerequisites"
related_series:
  - "et-framework"
series_role: "article"
series_order: 2
---

如果前一篇是在拆“执行单元”，这一篇拆的就是 `async/await` 的语义边界。因为很多人一看到 `await`，脑子里立刻跳出来的还是“开个后台线程，等它做完再回来”。这个想法太粗了，粗到会让你根本看不懂 ETTask 想替换什么。

`async/await` 首先不是线程 API，而是一套编译器与运行时之间的协议。编译器把你的方法改写成状态机；运行时在 await 点保存 continuation；某个 awaiter 完成后，再决定把 continuation 交给谁恢复。只要这个框架没建立起来，后面谈上下文切换、主线程恢复、自定义任务类型，都会飘在空中。

## `await` 到底没有替你做什么

它没有保证新开线程，没有保证并行执行，也没有保证一定切回你以为的那个线程。它只保证一件事：当前逻辑在这里暂停，等被等待的对象完成后，再从这个点继续。

这意味着 `await` 的关键不是“去哪里执行”，而是“完成之后如何恢复”。如果你把 `await` 想成“线程跳转”，很多现象都会被解释错。比如有些 await 根本没有离开当前线程，只是把 continuation 排到了稍后；有些 await 的工作是在 I/O 完成回调里继续，不涉及额外线程；有些 await 看起来回到了主线程，真正起作用的不是语法，而是捕获到的上下文。

## 状态机和 continuation 才是核心

一个 `async` 方法会被编译器改写成状态机，这件事非常关键。因为它解释了为什么 `await` 之后你还能写出像顺序代码一样的逻辑：并不是原始调用栈一直停在那儿等你，而是方法状态被拆成若干阶段，局部变量被保存，下一次恢复时再继续推进状态机。

continuation 则是这件事的另一半。所谓 continuation，本质上就是“这段逻辑接下来该怎么继续”。当一个 awaiter 完成时，它要么直接恢复 continuation，要么把 continuation 丢给某个调度器、某个上下文、某个消息循环。也就是说，`await` 的真正问题不是“有没有暂停”，而是“由谁来继续”。

## 为什么很多框架都要抢 continuation 的控制权

因为谁控制 continuation，谁就控制业务代码恢复的位置和时机。UI 框架希望 continuation 回到 UI 线程，避免你在后台线程直接改界面。游戏引擎通常也希望某些逻辑回到主线程或某个确定的运行时循环，以便维持帧更新和对象访问约束。

这也是 `SynchronizationContext` 重要的地方。很多人把它简单理解成“切回主线程工具”，其实它更像一个抽象的恢复入口：当你在某个上下文中发起 await，后续 continuation 是否回到这里、何时回来、怎样排队，都可以由它控制。主线程只是一个常见场景，不是它的全部。

## 把这个理解带回 ETTask

为什么要先理解这些，再看 ETTask？因为 ETTask 的价值不是“我也有一个任务类型”，而是“我想把 continuation 的保存、恢复与调度，纳入 ET 自己的运行时语义里”。如果你只会把 `Task` 看成“异步工作的包装盒”，你就看不到 ETTask 在替换哪一层。

ET 关心的是：continuation 在哪个 Fiber 或哪个运行时上下文恢复，异常如何收拢，消息链如何串起来，业务逻辑如何在它要求的边界里继续执行。也就是说，ETTask 不是只想提供一个 awaitable 类型，它更像是在告诉你：“这套 async 语义，我希望按 ET 的规则来恢复。”

## 最常见的错觉

最常见的错觉有三种。第一，以为 `await` 天生等于后台线程。第二，以为 `await` 恢复位置完全由语法决定。第三，以为自定义任务类型只是性能优化，不影响调度语义。前两种会让你读错 continuation，第三种会让你低估 ETTask 的设计动机。

一旦这三点理顺，你对“异步代码”为何能被框架接管，就不会再觉得奇怪。你会知道，框架真正接管的不是 `await` 关键字，而是它背后的 continuation 协议和恢复上下文。

## 这一篇之后该带着什么进入 ET 正文

读完这一篇，你应该已经能稳定地区分三层：`async/await` 是语言和编译器层的协议；`Task` 或 ETTask 是具体承载这套协议的运行时类型；恢复位置和调度时机则是框架要夺回控制权的关键。

带着这层理解去看 ET 的异步链路，你会更容易接受“同样是 await，为什么换了任务类型，整个恢复语义就能跟着框架走”。再往后读时，就会自然进入下一层：[`程序集、DLL、反射、动态加载：游戏框架为什么离不开它们`]({{< relref "et-framework-prerequisites/et-pre-03-assemblies-dlls-reflection-and-dynamic-loading.md" >}})。
