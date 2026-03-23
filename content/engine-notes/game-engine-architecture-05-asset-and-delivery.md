---
title: "游戏引擎架构地图 05｜资源导入、Cook、Build、Package，为什么也是引擎本体"
description: "基于 Unity 与 Unreal 官方资料，把资源导入、序列化、资产分发与最终构建收回同一层资产与发布层，而不是把它们看成外围流程。"
slug: "game-engine-architecture-05-asset-and-delivery"
weight: 460
featured: false
tags:
  - Unity
  - Unreal Engine
  - Asset Pipeline
  - Build Pipeline
  - Cook
  - Packaging
series: "游戏引擎架构地图"
---

> 这篇只回答一个问题：`为什么资源导入、序列化、资产分发与最终 Build / Cook / Package 不该被看成外围发布流程，而该被看成引擎内部的资产与发布层。`  
> 它不重讲 `00` 的六层总地图，不重写 `01` 的内容生产层，不展开 `03` 的运行时底座，不提前把 `06` 的平台抽象抢写进来，也不把本文写成 `Addressables / AssetBundles / Cook / Package` 的操作教程。

先把证据边界说明白。

当前这版首稿只使用官方文档证据。`docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径都还不是 `READY`，所以下文里凡是“事实”，都只落在官方资料明确写出来的范围；凡是“判断”，都会明确写成工程判断，而不是伪装成源码结论。

## 这篇要回答什么

很多人一提到资源导入、打包、Cook、Package，脑子里先冒出来的都是流程词：

- 导资源
- 构建资源包
- 打平台包
- 发版本

这套记法不能说完全错，但它非常容易把问题写偏。

因为只要你把这些词都理解成“项目后期会经过的一串步骤”，它们立刻就会看起来像外围工具链，而不是引擎架构的一部分。这样一来，文章就会滑向两种常见写法。

第一种写法，是教程：怎么配置导入规则，怎么点 Build，怎么出包。  
第二种写法，是工程流程总览：编辑器做完内容，后面再接 CI、发布脚本和发版平台。

这两种写法都会漏掉同一个更关键的问题：

`如果编辑器里的内容不先经过引擎自己的导入、索引、序列化、Cook 和构建规则，就根本不能成为可运行产品，那么这条链路还能被叫作外围流程吗？`

从官方资料能直接成立的事实是：

- Unity 官方把 `Asset Database`、`serialization`、`Addressables / AssetBundles`、`BuildPipeline` 都写成正式系统，而不是零散小技巧。
- Unreal 官方把 `Asset Registry`、`Asset Manager`、`cook / package`、`Unreal Build Tool` 都写成正式构建与资产组织结构，而不是外部建议流程。

基于这些事实，我在这篇里先给出的判断是：

`资源导入、Cook、Build、Package 在架构图上的稳定站位，不是“外围发布流程”，而是负责把编辑态内容重组为产品的资产与发布层。`

## 这一层负责什么

如果把“资产与发布层”压成一句更工程化的话，它主要负责五件事：

1. 把编辑态内容接管为引擎可识别、可跟踪的资产对象与元数据。
2. 把项目里的对象状态和资源关系转成可存储、可重建、可查询的数据。
3. 把资产进一步组织成可加载、可分发、可审计的交付单元。
4. 按目标平台把编辑态内容重新转换为运行态产物、缓存或 cooked content。
5. 把代码、配置、资源与交付单元重新装配成最终可发布产品。

先压一张对照表，会更容易看清这层到底在做什么。

| 对照维度 | Unity 资产与发布层 | Unreal 资产与发布层 | 这层真正负责什么 |
| --- | --- | --- | --- |
| 编辑态入口 | `Asset Database`、source asset、`.meta`、artifact、reimport | `Asset Registry`、unloaded assets、`FAssetData`、package metadata | 资产先被引擎接管，而不是裸文件直接变产品 |
| 数据与身份 | `serialization`、可重建对象状态、GUID | package metadata、Primary Asset、tag/value pairs | 编辑态内容先变成可重建、可查询的数据 |
| 交付组织 | `Addressables / AssetBundles`、dependencies、locations | `Asset Manager`、bundles、chunks、audit | 资源系统同时也是交付组织系统 |
| 平台转换 | build target 对应不同 artifact 与 Player build | cook 到 target platform、优化、压缩、裁剪 | 可运行产品必须经过平台重组 |
| 最终装配 | `BuildPipeline` 组装 Player 与 AssetBundles | `UBT + Package` 组装代码与 cooked content | build 不是外围脚本，而是引擎装配链 |

从官方资料能直接落下的事实是：

- 两边文档都不只是写“如何导出”，而是直接写到了 artifact、metadata、dependency、bundle、chunk、cook、build operation、package、build tool 这些系统级关键词。
- 两边都把编辑器内容、平台转换、运行时加载与最终交付写进了同一条正式工程链路。

基于这些事实，我在这里的判断是：

`资产与发布层真正负责的不是“帮你导出文件”，而是“把编辑器里的内容重新组织成产品”。`

也正因为如此，这一层最不该被写成：

- 发版按钮说明书
- 资源系统功能百科
- 外部工具链附录

## 这一层不负责什么

边界不先压清，后面一定会串题。

这篇明确不做下面几件事：

- 不把 `Inspector / Prefab / Blueprint / Plugin` 重讲成内容生产层文章，那是 `01` 的任务。
- 不把 `Scene / World / GameObject / Actor / Gameplay Framework` 重讲成默认对象世界文章，那是 `02` 的任务。
- 不把 `GC / reflection / PlayerLoop / Task Graph / scripting backend` 重讲成运行时底座文章，那是 `03` 的任务。
- 不把渲染、物理、动画、音频、UI 重新展开成专业子系统层文章，那是 `04` 的任务。
- 不把 `RHI / graphics backend / target platform abstraction` 提前写成平台抽象文章，那是 `06` 的任务。
- 不把 `DOTS / Mass` 混写成数据导向扩展层文章，那是 `07` 的任务。
- 不做 `Addressables / AssetBundles / Cook / Package / Build` 的按钮路径教程、参数百科、CI 模板或产品优劣比较。

为什么必须克制？

因为只要把内容生产、运行时底座、平台抽象和交付工具全揉进来，这篇就会从架构文章滑成“工程流程大全”。

而本文真正只想先证明一件事：

`为什么没有资产与发布层，编辑器里的内容就还不是产品。`

## Unity 怎么落地

先看 Unity 官方文档给出的资产与发布层链条。

### `Asset Database` 说明资源导入先是一套引擎管理的资产转换系统

从 Unity 关于 `Asset Database` 的文档里，能直接落下几件事实：

- source asset file 与 imported counterpart 会保持同步。
- 导入会把源资源转换成 `Unity-optimized artifacts`，供编辑器和运行时使用。
- `.meta` 文件会保存 import settings 与 `GUID`。
- 资源内容、依赖、importer version 或 build target 变化都可能触发 reimport，并缓存不同平台的 artifact。

这组事实说明，Unity 的资源导入不是“把文件放进工程目录”这么简单。

更接近官方资料支持的说法是：

- 引擎先接管资源身份。
- 引擎跟踪依赖关系。
- 引擎缓存面向不同平台的导入产物。

基于这些事实，我的判断是：

`Unity 的导入链从一开始就不是外围文件管理，而是引擎级资产转换与同步系统。`

### `serialization` 说明项目数据和对象状态先要变成可存储、可重建的引擎数据

从 Unity 的 `Script serialization` 文档里，还能直接看到：

- Unity 会把数据结构或 `GameObject` state 转成可存储并可稍后重建的格式。
- 数据组织方式会直接影响 serialization 行为，并可能影响项目性能。
- serialization rules、custom serialization、how Unity uses serialization 被官方作为完整主题组织。

如果只把序列化理解成“磁盘格式”，会把它在整条资产链里的站位写没掉。

更接近事实的写法是：

- 编辑器里的状态要先变成 Unity 自己可保存的数据。
- 这些数据以后还要能被重建、引用和继续参与构建。
- 所以 serialization 不是外围存盘细节，而是资产与发布层的基础设施。

基于这些事实，我的判断是：

`在 Unity 里，序列化不是边角料，它负责把可编辑内容转换成可持续重建的工程数据。`

### `Addressables / AssetBundles` 说明交付边界从一开始就是引擎内建结构

从 Unity 关于 `Addressables` 与 `AssetBundles` 的文档里，能直接落下几件事实：

- Addressables 提供组织、管理、load 与 release assets 的正式 `API and editor interface`。
- Addressables 建立在 `AssetBundle` API 之上，并自动处理 bundle creation and management。
- 它会处理 dependencies、locations、memory management，并支持 local / CDN 等不同交付位置。
- AssetBundles 本身被官方定义成可用于 patches 与 DLC 的 archive file format。

这组事实说明，Unity 的资源系统到了这里已经不只是“存资源”，而是开始直接负责：

- 怎么分组
- 怎么定位
- 怎么交付
- 怎么在运行时加载和释放

基于这些事实，我的判断是：

`Unity 的资源系统从 Addressables / AssetBundles 这里开始，已经同时是交付系统，而不是构建完主包以后再补一层外部资源逻辑。`

### `BuildPipeline` 说明最终 Player 装配本身就是统一引擎职责

从 Unity 的 `BuildPipeline` 与自定义构建脚本文档里，还能直接看到：

- `BuildPipeline` 同时覆盖 `building players or AssetBundles`。
- 自定义 build script 可以介入 `pre-build / post-build` 流程，也可以从 command line 触发。
- 官方示例明确允许先构建 AssetBundles，再构建 Player，并把相关 type information 传入最终 build。
- 构建结果还能和 `StreamingAssets`、build profile、PlayerSettings、EditorUserBuildSettings 一起决定最终产物边界。

这组事实说明，Unity 的 build 不是工程外部最后做的一次拷贝动作。

更接近事实的说法是：

- 场景、类型、资源分发单元和目标平台配置都要在这里重新装配。
- Player 不是编辑器世界的直接镜像，而是引擎构建链产出的正式产品形态。

基于这些事实，我的判断是：

`Unity 的 BuildPipeline 是产品装配链的一部分，而不是 IDE 外面再接的一段脚本。`

把这一节收成一句话，就是：

`Unity 的资产与发布层更像一条从资产接管、状态序列化、资源分发到最终 Player 装配连续打通的产品装配链。`

这里还不是在讲 `IL2CPP`、底层 runtime 或平台 backend；这里只先解释这条链为什么属于引擎本体。

## Unreal 怎么落地

再看 Unreal 官方文档给出的资产与发布层链条。

### `Asset Registry` 说明资产先进入引擎维护的可查询索引世界

从 Unreal 关于 `Asset Registry` 的文档里，能直接落下几件事实：

- `Asset Registry` 是编辑器子系统，会异步收集 unloaded assets 信息。
- 这些信息会保存在内存里，使编辑器可以在不加载资产的情况下构建 asset list。
- `FAssetData` 包含 object path、package name、class name、tag/value pairs 等元数据。
- 许多 tag 会写进 `uasset header`，Registry 会把它们当作权威、最新的数据读取出来。

这组事实说明，Unreal 的资产不是“需要时才临时读文件”的松散集合。

更接近事实的说法是：

- 资产先被登记进一张持续维护的索引图。
- 资产身份、包名、标签和值都在这张图里保持可查询状态。
- 编辑器工作和后续分发组织都建立在这层索引之上。

基于这些事实，我的判断是：

`Unreal 的资产层首先是一套引擎维护的索引世界，而不是内容浏览器表层 UI 的附属功能。`

### `Asset Manager` 说明资产组织、分发和审计边界都是正式引擎结构

从 Unreal 的 `Asset Management` 文档里，还能直接看到：

- `Asset Manager` 是存在于 Editor 与 packaged game 的 `unique, global object`。
- 它围绕 `Primary Assets` 与 `Secondary Assets` 工作。
- 它能把内容划分为 `chunks`，并提供 disk / memory usage 审计能力。
- `Asset Bundles` 可以和 Primary Asset 关联，并可在保存时声明或运行时注册。

如果只把 Unreal 的资源交付理解成“打包时生成一些 pak”，会把真正的组织结构写没掉。

更接近事实的说法是：

- 引擎先定义哪些资产是主要组织单元。
- 引擎再定义 bundles、chunks 和 audit 这些分发结构。
- 这些结构既面向编辑器，也面向 packaged game。

基于这些事实，我的判断是：

`Unreal 的资产分发边界不是外部脚本后补出来的，而是引擎内建的正式资产管理结构。`

### `cook / package` 说明编辑器世界必须先被转换成目标平台运行世界

从 Unreal 的 `Packaging Your Project` 文档里，还能直接落下几件事实：

- packaging 被官方直接定义为 `build operation`。
- build、cook、stage、package 是 packaging 过程中的核心阶段。
- `Cook` 会把 geometry、materials、textures、Blueprints、audio 等资产转成目标平台可运行格式，并执行优化、压缩、裁剪未使用数据。
- `Package` 会把 compiled code 与 cooked content 组成 distributable files。

这组事实说明，Unreal 的编辑器内容不能直接等于最终产品。

更接近事实的写法是：

- 编辑态世界必须先经过平台转换。
- 运行态产品要经过 cook、stage、package 这些正式阶段重新装配。
- 最终产品的内容边界是在这里重新被确定的。

基于这些事实，我的判断是：

`在 Unreal 里，Cook / Package 不是“做完游戏后顺手导出”，而是把编辑器世界转成平台运行世界的正式引擎流程。`

### `Unreal Build Tool` 说明 build system 本身就是引擎内部规则系统

从 Unreal 关于 `GenerateProjectFiles` 与 `Build Configurations` 的文档里，还能直接看到：

- `GenerateProjectFiles` 只是 Unreal Build Tool 的 wrapper。
- UE build system 编译代码并不依赖 IDE project files。
- UBT 会根据 `module` 与 `target build files` 发现源文件并组织编译。
- build configuration 被官方作为正式主题来说明不同编译与分发形态。

这组事实说明，Unreal 的 build 不是 IDE 附件，也不是 CI 外围脚本的别名。

更接近事实的说法是：

- module、target、configuration 这些规则本身就是引擎工程结构的一部分。
- 最终产品如何被组装，首先要服从 UBT 的规则系统。

基于这些事实，我的判断是：

`Unreal 的 build 是引擎自己的产品装配机制，而不是外部工具顺手帮它编译一下。`

把这一节收成一句话，就是：

`Unreal 的资产与发布层更像一条由资产索引、全局资产管理、cook / package 和 UBT 共同组成的产品分发与装配链。`

这里还不是在讲 `RHI`、平台抽象或 pak 内部布局细节；这里只先解释这条链为什么必须被放回引擎本体。

## 为什么这不是外围流程

把前面两节再压一次，至少能先看出四个稳定判断。

### 第一，这条链从资源进入工程时就已经开始

Unity 从 `Asset Database` 和 reimport 开始。  
Unreal 从 `Asset Registry` 和资产索引开始。

所以这条链不是发版前最后一天才出现，而是内容刚进入工程时就已经被引擎接管。

### 第二，它处理的不是文件拷贝，而是资产身份、依赖和平台产物的再组织

Unity 维护 artifact、GUID、serialization、bundle 与 Player build 的关系。  
Unreal 维护 package metadata、Primary Asset、chunk、cook 与 package 的关系。

所以这条链真正负责的不是“拷到某个目录”，而是把内容重组为可以交付的工程对象。

### 第三，它同时连接编辑器、运行时和 packaged product

Unity 的 Addressables / AssetBundles 既影响编辑期组织，也影响运行时 load / release 和最终 Player 装配。  
Unreal 的 Asset Manager 既存在于 Editor，也存在于 packaged game，cook / package 则继续决定最终产品形态。

所以它不是纯外部运维动作，而是贯穿编辑态、运行态和交付态的正式引擎层。

### 第四，最终产品的内容边界、代码边界和平台边界都要在这里重新确定

Player 要通过 `BuildPipeline` 装配。  
Packaged product 要通过 `cook / package / UBT` 装配。

所以最稳的写法不是：

`打包也是项目流程的一部分。`

而是：

`没有资产与发布层，编辑器里的内容还不是产品；这层存在的意义，就是把内容重组为产品。`

## 常见误解

### 误解一：会点 `Build / Package` 按钮，就等于理解了这一层

这会把架构问题压扁成操作路径。  
本文关心的是资产如何被重新组织成产品，而不是按钮在哪里。

### 误解二：资源导入规则写在团队文档里就够了，不算引擎架构

真正的架构边界不在群公告里，而在引擎是否真的维护 artifact、metadata、dependencies、bundle、chunk、cook rule 和 build rule。

### 误解三：`Addressables / AssetBundles` 与 `Asset Manager / chunk / pak` 可以严格一一对应

这篇不做这种对译。  
本文只比较架构站位，不主张两边术语和实现可以简单互换。

### 误解四：构建系统只是 IDE 或 CI 的外部外挂

官方资料已经足够说明，Unity 的 `BuildPipeline` 和 Unreal 的 `UBT` 都是正式构建入口，而不是外部外挂层。

### 误解五：编辑器里能运行的内容已经天然等于最终产品

事实恰好相反。  
Unity 还要经过 artifact、bundle 与 Player build；Unreal 还要经过 registry、cook、package 与 build rules。编辑态世界不会自动等于交付态世界。

## 我的结论

先重申这篇能直接成立的事实。

- Unity 官方把 `Asset Database / serialization / Addressables / AssetBundles / BuildPipeline` 写成连续的资产转换与构建链。
- Unreal 官方把 `Asset Registry / Asset Manager / cook / package / Unreal Build Tool` 写成连续的资产索引、平台转换与产品装配链。
- 当前本地源码路径还没有任何 `READY` 标记，因此这篇不能声称自己做了源码级验证。

基于这些事实，我在这篇里愿意先给出的工程判断是：

`资源导入、Cook、Build、Package 在现代游戏引擎里的稳定站位，不是外围发布流程，而是资产与发布层。`

进一步说：

- Unity 这层更接近一条围绕 `Asset Database`、serialization、Addressables / AssetBundles、`BuildPipeline` 组织起来的资产转换与产品装配链。
- Unreal 这层更接近一条围绕 `Asset Registry`、`Asset Manager`、cook / package、`Unreal Build Tool` 组织起来的资产索引、分发与产品装配链。

所以这篇最值得先记住的一句话不是：

`Build / Cook / Package 是发布步骤。`

而是：

`Build / Cook / Package 是引擎把编辑态内容二次组织为产品的装配链。`

这也解释了为什么后面还需要单独写 `06`。  
因为当内容已经被重组为产品，下一层问题才会变成：跨平台引擎到底在抽象什么。
