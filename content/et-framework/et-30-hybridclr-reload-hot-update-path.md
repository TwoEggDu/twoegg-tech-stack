---
date: "2026-04-24"
title: "ET-30｜HybridCLR、Reload 与热更：ET 的代码更新路径为什么这样设计"
description: "把 ET 的代码更新链拆成代码装配、AOT 桥接和运行时重挂三层，再看 CodeLoader、HybridCLR 和 Reload 为什么必须一起设计。"
slug: "et-30-hybridclr-reload-hot-update-path"
weight: 30
featured: false
tags:
  - "ET"
  - "HybridCLR"
  - "Reload"
  - "Hot Update"
  - "CodeLoader"
series: "ET 框架源码解析"
primary_series: "et-framework"
related_series:
  - "et-framework-prerequisites"
  - "hybridclr"
series_role: "article"
series_order: 30
---

> **读这篇之前**：本篇会反复用到“程序集装配、对象树、热更边界”这三层概念。如果前置还不稳，建议先看：
> - ET-03（安装要求）
> - ET-09（Entity 对象树）
> - [ET-Pre-09｜热更不是魔法：AOT、HybridCLR、资源热更和代码热更分别在改什么]({{< relref "et-framework-prerequisites/et-pre-09-hot-update-aot-hybridclr-code-and-asset-delivery.md" >}})
> - <!-- RELREF-TODO: 指向 HybridCLR 系列入口，如系列入口 slug 调整，同步更新下面 relref -->
> - [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})

> 版本说明：本文基于当前仓库缓存中的 ET9 公开主仓库与公开包，重点锚定 `.tmp/ET/README.md`、`.tmp/ET/Book/1.1运行指南.md`、`.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`。运行指南当前要求的 Unity 版本为 `6000.0.25`。
>
> 证据标签：源码拆解 + 官方文档解析 + 工程判断。

ET 把 `HybridCLR`、`Reload`、`热更` 这三个词并排放在一起，最容易诱发一种错觉：它们像是同一件事的三个叫法。不是。`热更` 回答的是“我要换掉什么行为”；`HybridCLR` 回答的是“在 `IL2CPP/AOT` 约束下，这些新行为凭什么还能执行”；`Reload` 回答的是“在不重启进程、不清空运行时对象树的前提下，怎么把新代码挂回现有世界”。这三个问题只要混成一个词，后面就一定会读偏。

更关键的是，ET 不是把“代码更新”理解成某种神秘补丁术，而是把它理解成一条完整的运行时装配链。你要先把代码编成哪几段、让哪几段在首启时固定、让哪几段在运行中可替换、又让哪几段在 `IL2CPP` 下仍然可见可执行，然后才能谈调试效率、上线交付和回滚纪律。没有这条装配视角，`Reload` 会被误读成“重新编译”，`HybridCLR` 会被误读成“万能热更按钮”，而 `热更` 这个词本身则会被讲空。

作者在本专栏另有 HybridCLR 源码解析 37 篇深度系列，本篇只讲 ET 层使用路径，不重复 HybridCLR 内部机制。本文也不会展开 ET-29 的持久化边界，更不会提前代替 ET-31 去讲完整资源交付链。这里聚焦的只有一件事：ET 为什么要把 `CodeLoader`、`HybridCLR`、`Reload` 做成一条连续的代码更新路径。

## 热更首先是代码装配问题，不是“补丁”问题

很多团队一说热更，脑子里先跳出来的是“线上打一段补丁”。这个说法太粗，会把真正的工程问题抹平。对 ET 来说，代码更新的第一性问题从来不是“补丁怎么发”，而是“运行时到底由哪几段程序集构成，以及这些程序集什么时候加载、什么时候替换、替换后哪些注册要重新跑”。只要还没把这层说清，讨论就会不断滑向资源热更、配置热更甚至运维发版，最后失去焦点。

<!-- JUDGMENT-TODO: “热更是装配问题不是补丁问题”这句话为什么是专栏内已有 HybridCLR 37 篇的延伸起点 -->

这也是为什么前置篇 ET-Pre-09 必须先把“代码热更、资源热更、AOT 约束”拆成三层。落到 ET 这里，问题会进一步收敛成一句更硬的话：`代码更新的本质，是把运行时正在执行的程序集图重新装起来，而不是在原方法上抹一层补丁。` 你可以不喜欢这个表述，但公开源码给出的路径就是这样。客户端不是把某个方法体偷偷替换掉，而是把 `Model / ModelView / Hotfix / HotfixView` 这些程序集按顺序装进运行时，再决定哪一层可以被重新装载。

一旦从装配视角进入，很多边界会立刻清楚。比如本文不会去展开“资源包怎么灰度下发”，因为那是交付层；也不会去展开“运行时状态怎么持久化”，因为那是 ET-29 的边界。这里先钉死的是：`热更` 在 ET 语境里首先是一条代码装配路径，资源链路只是把这条路径运到设备上，状态管理则是这条路径运行之后要不要保留现场的问题。

## AOT 和 IL2CPP 逼出来的不是“热更方案”，而是代码桥接层

如果 Unity 客户端永远跑在普通桌面 CLR 语境里，这篇根本不需要 `HybridCLR` 出场。你把 DLL 下发下来，`Assembly.Load` 一下，再做反射和注册就够了。问题恰恰在于 ET 面向的是 Unity 正式交付环境，而正式交付环境又高度依赖 `IL2CPP` 和 `AOT`。这时候“能不能把一份新的 C# 代码装进运行时”已经不是语言问题，而是平台问题。

`IL2CPP` 的世界默认相信的是“构建时已经知道要执行哪些类型、哪些方法、哪些泛型实例”。可 ET 的客户端代码更新路径偏偏要求“运行时再把新的 IL 装进来”。冲突就出在这里。所以 `HybridCLR` 在 ET 里承担的第一职责，不是替你设计热更业务，而是给 `IL2CPP` 世界补一层桥，让运行时重新拥有解释和补充元数据的能力。换句话说，它解决的是“动态代码为什么还能跑”，而不是“业务逻辑为什么值得更新”。

<!-- COMPARE-TODO: HybridCLR vs xLua / ILRuntime / puerts 的边界差异（只对比代码装配维度，不全面横评） -->

公开材料能证明 ET 是把这层桥接当前置基础设施接进来的，而不是写成一个可有可无的外挂。`Packages/packages-lock.json` 里明确存在 `cn.etetet.hybridclr` 这个 embedded 包；`cn.etetet.loader/Runtime/ET.Loader.asmdef` 又直接引用了 `ET.HybridCLR`。而运行指南里的打包顺序也写得很清楚：先 `HybridCLR -> Generate -> All`，再 `ET -> HybridCLR -> CopyAotDlls`，然后才进入资源构建和最终包体生成。这个顺序本身已经说明 ET 的判断是：`HybridCLR 不是热更功能点，而是代码路径能否在 IL2CPP 上成立的前置桥。`

因此，本篇谈 HybridCLR 时只谈它在 ET 中扮演的桥接角色，不重复它在解释器、元数据、AOT 泛型层面的内部机制。那些内部细节应该回到 HybridCLR 深度系列里看；在 ET 这边，更重要的问题是：桥接一旦成立，ET 又怎么把它编进自己的装配链。

## HybridCLR 在 ET 里承担的是“让装配链成立”，不是替 ET 接管启动流程

最能说明问题的不是 README 上的口号，而是 `cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs` 这条真实启动链。它把 ET 的代码更新路径拆得非常直白。

第一步是拿到字节流。`DownloadAsync()` 在非编辑器模式下先去 `Packages/cn.etetet.loader/Bundles/Code` 目录加载热更代码资产；如果启用了 `IL2CPP`，还会额外去 `Bundles/AotDlls` 把 AOT DLL 一起拉下来。也就是说，ET 先把“代码本体”和“AOT 可见性修补材料”分成两条输入流，而不是把它们混成一份资源包。

<!-- SOURCE-TODO: CodeLoader 里 Model / ModelView / Hotfix / HotfixView 四条 DLL 的加载顺序 -->
<!-- SOURCE-TODO: HybridCLR 补充元数据、AOT dlls 目录和热更 dlls 目录的调用点 -->
<!-- SOURCE-TODO: packages-lock / asmdef 里 cn.etetet.hybridclr 与 ET.Loader 的依赖关系 -->

第二步是先补可见性，再装程序集。`Start()` 里如果检测到 `EnableIL2CPP`，会先遍历 `aotDlls`，逐个调用 `RuntimeApi.LoadMetadataForAOTAssembly(..., HomologousImageMode.SuperSet)`；之后才 `Assembly.Load` `ET.Model.dll` 和 `ET.ModelView.dll`，再通过 `LoadHotfix()` 装入 `ET.Hotfix.dll` 与 `ET.HotfixView.dll`。这个顺序很关键，因为它揭示了 ET 的真实判断：`HybridCLR` 先解决“运行时看不看得见、认不认得出这些类型”，然后 `CodeLoader` 才继续做自己的程序集装配。

第三步是把新装配过的程序集重新交给 ET 的类型系统。`CodeLoader` 在拿到 `World / Init / Model / ModelView / Hotfix / HotfixView` 这六段程序集后，会把它们一起塞进 `CodeTypes`。随后启动阶段调用 `ET.Entry.Start`，而 `Entry.Start` 内部又会先执行 `CodeTypes.Instance.CodeProcess()`，把带有 `CodeProcessAttribute` 的代码单例重新建起来。这个设计很说明问题：ET 没有让 HybridCLR 直接接管框架启动，而是把 HybridCLR 收敛成“让 `Assembly.Load` 在 IL2CPP 下成立”的桥，然后继续由自己的 Loader 和类型注册流程掌控运行时入口。

这就是为什么 ET 要保留 `Model / ModelView` 和 `Hotfix / HotfixView` 四段装配的分层。前两段更像首启就立住的骨架层，后两段则是运行中允许被替换的行为层。只要这四段的职责不分，后面 `Reload` 就没法精准替换，`HybridCLR` 也就只能退化成“能跑动态 IL 的大锤”。

## Reload 不是“重新编译”，而是在原运行时里重挂行为定义

如果前面三步都只是为了把 DLL 装进来，那 `Reload` 的意义仍然不够。因为开发阶段真正想要的是：代码改完之后，别让我每次都重启客户端、重新进场景、重新把现场搭回去。ET 的 `Reload` 就是在回答这个问题。

运行指南已经把这条路径写得很明确：先编译 ET.sln，再通过 Unity 菜单 `ET -> Reload`，或者直接按 `F7`。而 `CodeLoader.Reload()` 的实现也清楚得几乎没有歧义：它不会重载 `Model / ModelView`，不会重建整个运行时，更不会重新走一遍完整启动流程；它做的是重新 `LoadHotfix()`，重新把六段程序集交给 `CodeTypes`，然后执行 `codeTypes.CodeProcess()`，最后打出 `reload dll finish!` 日志。

<!-- JUDGMENT-TODO: Reload 为什么必须保留 Entity 对象树 / EventSystem 状态 -->
<!-- EXPERIENCE-TODO: Reload 后 EventSystem 回调失效、Entity 引用失效的踩坑 -->
<!-- SOURCE-TODO: CodeTypes.CodeProcess 与 LoadSystem / 重新注册 handler 的调用链 -->

这意味着什么？意味着 `Reload` 的目标从来不是“重新编译一下代码”这么浅的一层，而是：`在现有 World、现有 Entity 对象树、现有 Fiber 调度现场仍然活着的前提下，把可替换的行为层重新挂进去。` 如果一改代码就必须整个运行时清空，那它当然也能工作，但那不叫 Reload，那只是重启。

这也是 ET 为什么必须把 `CodeTypes.CodeProcess()` 和文档里的 `LoadSystem` 结合起来看。ET 的文档明确把 `LoadSystem` 描述成“加载 DLL 后做处理，比如重新注册 handler”的钩子；而 `CodeProcess()` 则负责把带 `CodeProcessAttribute` 的代码单例重新创建出来。两者合起来才构成真正的 Reload 语义：不是单纯产出一份新 DLL，而是在旧世界里把新 DLL 对应的注册点、处理器和代码单例重新安回去。

但这条路径也天然带着代价。运行时对象不重建，意味着你可以保留现场；可现场既然保留，旧委托、旧缓存、旧静态引用也可能一起保留。于是 Reload 从来都不是“无脑替换”，而是“给高频调试提供最快的行为重挂通道，同时把状态一致性的责任留给工程纪律”。这恰好解释了为什么 ET 要同时强调对象树和代码分层，而不是只谈一个热更按钮。

## ET 的代码更新链闭环，靠的是 Loader、HybridCLR 和 Reload 各守一层

把前面的层级收回来，ET 的代码更新链其实很完整，而且层次分工非常清楚。

开发态的闭环是这样的：

1. 你改的是 `Hotfix / HotfixView` 层代码。
2. 你先编译，让新的 DLL 产出。
3. 你执行 `Reload`，让 `CodeLoader` 只重挂可替换层。
4. `CodeTypes` 和相关注册流程重新跑起来。
5. 现有对象树继续存活，新的行为定义接管后续执行。

交付态的闭环则是另一条线：

1. 打包前先跑 `HybridCLR -> Generate -> All`。
2. 再执行 `ET -> HybridCLR -> CopyAotDlls`，把需要补充元数据的 AOT DLL 准备好。
3. 然后构建资源与包体，把代码 DLL 和 AOT DLL 一起纳入交付链。
4. 运行时由 `CodeLoader` 下载 `Bundles/Code` 和 `Bundles/AotDlls`。
5. 先补元数据，再装程序集，再进入 `ET.Entry.Start`。

<!-- DATA-TODO: Reload 一次的耗时、热更包体积实测 -->
<!-- JUDGMENT-TODO: 如果只从代码装配视角看，ET 的更新链路和 Orleans 的 silo 滚动升级有什么本质差异 -->

从这个角度看，`HybridCLR`、`Reload`、`热更` 必须一起设计，不是因为作者喜欢堆概念，而是因为三者确实各自补同一条链上的不同缺口。只有 `HybridCLR` 没有 `Reload`，你能在真机上执行新代码，但开发期仍然要忍受高成本重启；只有 `Reload` 没有 `HybridCLR`，你在编辑器里也许能舒服调试，可一到 `IL2CPP` 正式环境，动态装配就断掉；只有“热更”这个泛称，没有 `CodeLoader` 这条实际装配路径，那连“新代码怎么进运行时”都说不清。

这也解释了 ET 和 ET-31 的边界。资源交付当然重要，但在这篇里它只是配套层。资源系统负责把 DLL 和 AOT 数据运到设备上，真正让代码更新成立的，仍然是 Loader 的装配语义、HybridCLR 的桥接语义，以及 Reload 的重挂语义。把这三层混进一锅，后面谈调试效率、上线包体和异常排查都会失真。

## 这一篇真正想留下来的结论

`ET 的代码更新路径不是“HybridCLR + 一个热更按钮”，而是“CodeLoader 负责装配，HybridCLR 负责桥接，Reload 负责在原运行时里重挂行为层”。`

只有把这三层拆开，ET 为什么要把 `Model / ModelView` 和 `Hotfix / HotfixView` 分层、为什么打包链里一定要有 `GenerateAll` 和 `CopyAotDlls`、为什么开发期又要保留 `F7 Reload` 这种入口，才会同时变得合理。说到底，ET 不是在追求一个抽象概念上的“支持热更”，而是在 Unity + IL2CPP + 长生命周期运行时这个现实组合里，把代码更新链做成可调试、可交付、可持续迭代的一条工程路径。

## 文末导读

- 下一篇：ET-31
- 扩展阅读：ET-08（CodeLoader 四分法）、ET-27（Package 模式）
- 理由：代码链路讲清后，再谈资源链路如何与之配合，焦点才不会散。
