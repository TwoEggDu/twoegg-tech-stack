---
title: "ET 框架源码解析"
description: "先建立 ET9 的运行时与包化地图，再把这张地图落到公开源码和公开包上。"
series: "ET 框架源码解析"
series_id: "et-framework"
series_role: "index"
series_order: 0
series_nav_order: 46
series_title: "ET 框架源码解析"
series_audience:
  - "Unity 客户端 / 服务端开发者"
  - "准备系统阅读 ET9 的读者"
series_level: "进阶"
series_best_for: "当你不想把 ET 看成功能清单，而是想看清它的运行时骨架、分布式机制和工程边界"
series_summary: "围绕公开仓库与公开包，拆清 ET9 的真实形态、运行时骨架、消息与 Actor、分布式机制和工程化链路。"
series_intro: "这组文章不按 README 复述，也不按功能点平铺。它真正要做的是先给出 ET9 的真实地图，再把这张地图落到公开源码和公开包上，最后解释这套框架适合什么问题、不适合什么问题。"
series_reading_hint: "如果你还没看 ET 前置桥接系列，建议先补前置再进正文；如果你已经有异步、装配、网络和 Actor 的最小地图，可以直接从 ET-01 开始。"
---

`ET` 不是一个适合靠“包名猜功能”的框架。你如果只把它看成 Unity 工程目录，很快会被主仓库、公开包、课程版、运行指南、Package 中心这些结构打散；你如果只把它看成功能列表，又会很快失去主线，不知道哪一层才是它的脊梁骨。

所以这组正文的顺序不会按功能罗列，而会按框架层级推进：先看形态与入口，再看运行时骨架，再看网络、Actor 和分布式机制，最后回到工程化与 Demo 承载。这样读者脑子里先有地图，再去看具体代码，就不会被名词推着走。

## 推荐先读

- [ET 前置桥接系列入口]({{< relref "et-framework-prerequisites/_index.md" >}})
- [ET-Pre-01｜线程、任务、协程、纤程到底不是一回事]({{< relref "et-framework-prerequisites/et-pre-01-threads-tasks-coroutines-and-fibers.md" >}})
- [ET-Pre-03｜程序集、DLL、反射、动态加载：游戏框架为什么离不开它们]({{< relref "et-framework-prerequisites/et-pre-03-assemblies-dlls-reflection-and-dynamic-loading.md" >}})
- [ET-Pre-04｜Unity Package、asmdef、代码装配：ET9 的工程边界为什么长这样]({{< relref "et-framework-prerequisites/et-pre-04-unity-package-asmdef-and-code-assembly.md" >}})
- [ET-Pre-06｜Actor 模型最小桥接：邮箱、串行处理、位置透明到底在说什么]({{< relref "et-framework-prerequisites/et-pre-06-actor-mailbox-serial-processing-and-location-transparency.md" >}})

## 正文会优先围绕哪些公开材料展开

- 主仓库的运行指南、Book 文档和初始化工程
- 公开包 `cn.etetet.core`
- 公开包 `cn.etetet.loader`
- 公开包 `cn.etetet.actorlocation`
- 公开包 `cn.etetet.login`

也就是说，这组正文默认先围绕“公开源码可证”的部分推进。课程版、付费包和未公开源码会被当成生态背景，而不是源码拆解对象。

## 先从哪一篇进入

建议直接从第一篇开始：

- [ET-01｜ET9 还是那个 ET 吗：从单仓库框架到 Package 化框架]({{< relref "et-framework/et-01-is-et9-still-et-from-monorepo-to-package-based-framework.md" >}})

{{< series-directory >}}
