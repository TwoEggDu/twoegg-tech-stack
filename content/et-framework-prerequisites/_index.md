---
title: "ET 前置桥接"
description: "把读者从会用 Unity、懂一点网络和异步，桥接到能稳定进入 ET9 正文源码主线。"
series: "ET 前置桥接"
series_id: "et-framework-prerequisites"
series_role: "index"
series_order: 0
series_nav_order: 45
series_title: "ET 前置桥接"
series_audience:
  - "Unity 客户端 / 服务端开发者"
  - "想系统阅读 ET9 的读者"
series_level: "进阶"
series_best_for: "当你想先建立 ET 所依赖的最小语义地图，再进入源码与架构正文"
series_summary: "先补 ET 真正依赖的异步、装配、网络、Actor、对象树、服务端地图、热更与同步模型，再进入 36 篇 ET 正文。"
series_intro: "这组文章不是通用 C# 教程，也不是网络或 Unity 工程的完整学科地图。它只做一件事：把 ET 反复依赖、但读者最容易在正文里卡住的那层语义先补出来。"
series_reading_hint: "如果你刚接触 ET，建议按 ET-Pre-01 到 ET-Pre-10 顺序读；如果你已经知道自己卡在某一层，可以直接跳到对应篇目。"
---

`ET` 很容易被误读成“一个 Unity 项目里放了很多陌生名词”。真正的问题不是名词多，而是这些名词分别站在不同层：有些属于语言运行时，有些属于代码装配，有些属于网络模型，有些属于分布式与同步策略。如果不先把这张地图补出来，后面的源码阅读就会不断被基础误解打断。

这组前置因此不追求“面面俱到”，只追求一件事：让你带着稳定的判断框架进入 ET 正文。它会先拆线程、任务、Fiber 和 `async/await`，再拆程序集、Package、会话、Actor、对象树、服务端角色地图、热更和同步模型，最后把这些概念收成一张能直接服务 ET9 阅读的最小地图。

## 推荐阅读顺序

1. [ET-Pre-01｜线程、任务、协程、纤程到底不是一回事]({{< relref "et-framework-prerequisites/et-pre-01-threads-tasks-coroutines-and-fibers.md" >}})
2. [ET-Pre-02｜`async/await` 到底在调度什么]({{< relref "et-framework-prerequisites/et-pre-02-async-await-state-machine-and-continuation.md" >}})
3. [ET-Pre-03｜程序集、DLL、反射、动态加载]({{< relref "et-framework-prerequisites/et-pre-03-assemblies-dlls-reflection-and-dynamic-loading.md" >}})
4. [ET-Pre-04｜Unity Package、asmdef、代码装配]({{< relref "et-framework-prerequisites/et-pre-04-unity-package-asmdef-and-code-assembly.md" >}})
5. [ET-Pre-05｜会话、请求响应、超时、心跳]({{< relref "et-framework-prerequisites/et-pre-05-session-request-response-timeout-and-heartbeat.md" >}})
6. [ET-Pre-06｜Actor 模型最小桥接]({{< relref "et-framework-prerequisites/et-pre-06-actor-mailbox-serial-processing-and-location-transparency.md" >}})
7. [ET-Pre-07｜对象树与序列化树]({{< relref "et-framework-prerequisites/et-pre-07-object-tree-and-serialization-tree.md" >}})
8. [ET-Pre-08｜游戏服务端角色地图]({{< relref "et-framework-prerequisites/et-pre-08-game-server-role-map-login-gate-scene-watcher.md" >}})
9. [ET-Pre-09｜热更不是魔法]({{< relref "et-framework-prerequisites/et-pre-09-hot-update-aot-hybridclr-code-and-asset-delivery.md" >}})
10. [ET-Pre-10｜状态同步、帧同步、权威服]({{< relref "et-framework-prerequisites/et-pre-10-state-sync-lockstep-and-authoritative-server.md" >}})

## 这组前置要解决什么

- 让读者知道 `Fiber` 在 ET 里解决的是调度与隔离边界，而不是把协程改个名字。
- 让读者知道 `ETTask`、`CodeLoader`、`MailBox`、`ActorLocation` 这些词分别建立在什么前提之上。
- 让读者知道为什么 ET9 会同时谈到 Package、程序集、热更、场景服、Router、同步模型，而这些词并不是杂乱堆在一起的。

{{< series-directory >}}
