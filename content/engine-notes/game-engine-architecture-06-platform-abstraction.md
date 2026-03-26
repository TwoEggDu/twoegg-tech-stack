---
date: "2026-03-26"
title: "游戏引擎架构地图 06｜跨平台引擎到底在抽象什么？"
description: "基于 Unity 与 Unreal 官方资料，把 graphics APIs、条件编译、平台设置、RHI、target platform 与 build configuration 收回同一层平台抽象层。"
slug: "game-engine-architecture-06-platform-abstraction"
weight: 470
featured: false
tags:
  - Unity
  - Unreal Engine
  - Cross Platform
  - Graphics API
  - RHI
  - Build Configuration
series: "游戏引擎架构地图"
---

> 这篇只回答一个问题：`Unity 和 Unreal 所谓“跨平台”，到底把哪些差异收进统一工程抽象，哪些差异仍以图形后端、编译目标、平台配置、能力查询和 SDK 要求的形式保留下来。`  
> 它不重讲 `00` 的六层总地图，不重写 `03` 的运行时底座，不展开 `04` 的专业子系统，不把 `05` 的资产与发布链再写一遍，也不提前把 `08` 写成产品气质比较。本文也不写成 `iOS / Android / 主机平台接入` 教程、Player Settings 按钮说明书或图形 API 百科。

先把证据边界说明白。

当前这版首稿只使用官方文档证据。`docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径都还不是 `READY`，所以下文里凡是“事实”，都只落在官方资料明确写出来的范围；凡是“判断”，都会明确写成工程判断，而不是伪装成源码结论。

## 这篇要回答什么

很多人一说“跨平台引擎”，脑子里最先浮出来的往往是这种想象：

- 一套代码
- 一份资源
- 一次构建
- 所有平台结果几乎一样

这种记法的问题，不是它完全错，而是它太容易把“跨平台”写成宣传语。

因为只要你默认“跨平台”的意思是“平台差异已经被引擎彻底抹平”，你就会自动忽略一个反常识的事实：Unity 和 Unreal 的官方资料并没有把这些差异彻底藏起来，反而把它们正式写进了引擎自己的工程结构里。

从官方资料能直接成立的事实是：

- Unity 官方明确保留了 `graphics APIs` 列表、`Auto Graphics API`、条件编译符号、`Player settings`、`build profiles` 和 platform-specific rendering differences。
- Unreal 官方明确保留了 `RHI`、`target platform`、`target platform settings`、`build configurations`、shader format、平台能力查询和 target-platform requirements。

这就逼着我们把问题改写成另一种更架构的问法：

`如果平台差异没有被完全消灭，那跨平台引擎真正抽象掉的到底是什么？`

基于这些事实，我在这篇里先给出的判断是：

`跨平台引擎真正抽象掉的，不是所有平台差异本身，而是项目面对这些差异时的工程混乱。`

换句话说，本文要讲的不是“怎么发到多个平台”，而是：

`为什么 graphics APIs、条件编译、平台设置、RHI、target platform 和 build configuration 应该被看成同一层平台抽象层。`

## 这一层负责什么

如果把“平台抽象层”压成一句更工程化的话，它主要负责五件事：

1. 把同一项目能落到哪些图形后端、优先级如何、何时自动选择，纳入统一工程入口。
2. 把平台感知的编译变体写成正式规则，而不是把所有差异都拖到运行时再临时判断。
3. 把 per-platform settings、profile、SDK 要求和目标平台配置收进统一对象或配置边界。
4. 把 shader format、架构、渲染能力和特性开关写成正式可查询表面。
5. 把最终 build target 和平台要求放回统一术语下管理，但不假装所有目标会得到完全相同的产物。

先压一张对照表，会更容易看清这层到底在做什么。

| 对照维度 | Unity 平台抽象层 | Unreal 平台抽象层 | 这层真正负责什么 |
| --- | --- | --- | --- |
| 图形后端组织 | `graphics APIs` 列表、`Auto Graphics API`、后端优先级与回退顺序 | `RHI`、dynamic RHI module、backend modules | 先统一调用与管理边界，再保留后端差异 |
| 编译变体 | `conditional compilation`、scripting symbols、`define constraints` | `target platform` 边界、`build configurations`、目标相关编译路径 | 同一工程允许受控变体，而不是所有代码路径恒等 |
| 平台配置 | `Player settings`、`build profiles`、platform modules | `ITargetPlatformSettings`、`ITargetPlatformControls`、platform INI | 平台差异被纳入正式配置层，而不是散落在外 |
| 能力查询 | 渲染差异宏、运行时信息、不同 graphics APIs 行为差异 | shader formats、架构、`UsesRayTracing`、`SupportsValueForType` | 统一入口不等于取消能力边界 |
| 构建目标与要求 | per-platform profile、不同平台模块带来不同设置 | `state + target`、SDK / 组件 / source build requirements | 跨平台构建是在统一术语下管理显式要求 |

从官方资料能直接落下的事实是：

- 两边都没有把平台差异写成“外部世界的麻烦”，而是写成引擎内部正式结构的一部分。
- 两边都同时保留了“统一入口”和“差异显式存在”这两件事。

基于这些事实，我在这里的判断是：

`平台抽象层负责的不是“替你忘掉平台”，而是“让项目能用统一工程语言处理不可避免的平台差异”。`

也正因为如此，这一层最不该被写成：

- 各平台发包教程
- 图形 API 功能表
- 平台设置按钮索引
- 谁更强的产品比较

## 这一层不负责什么

边界不先压清，后面一定会串题。

这篇明确不做下面几件事：

- 不把 `GC / reflection / PlayerLoop / Task Graph / scripting backend` 重讲成运行时底座文章，那是 `03` 的任务。
- 不把 `render pipeline / physics / animation / audio / UI` 重讲成专业子系统文章，那是 `04` 的任务。
- 不把 `Asset Database / Asset Manager / cook / package / build` 的完整资产与发布链重讲成 `05`。
- 不把 `DOTS / Mass` 混写成数据导向扩展层文章，那是 `07` 的任务。
- 不做 `DirectX / Metal / Vulkan / OpenGL` 的 API 百科、平台接入教程、性能排名或兼容性结论。
- 不把 Unity 与 Unreal 写成“谁更跨平台”的产品优劣比较。

为什么必须克制？

因为只要把运行时、专业子系统、资产发布、图形 API 细节和平台接入步骤都揉进来，这篇就会从架构文章滑成“跨平台开发大全”。

而本文真正只想先证明一件事：

`跨平台不是一串零散技巧，而是引擎地图里一层正式的工程抽象。`

## Unity 怎么落地

先看 Unity 官方文档给出的平台抽象链条。

### `graphics APIs` 说明跨平台图形先是一组可管理后端

从 Unity 关于 `Configure graphics APIs` 的文档里，能直接落下几件事实：

- Unity 会使用内置的一组 `graphics APIs`，或者使用你在 Editor 中指定的 graphics APIs。
- 当 `Auto Graphics API` 开启时，Player build 会包含该平台的一组内置 graphics APIs，并在运行时选择合适的 API。
- 当 `Auto Graphics API` 关闭时，Unity 会显示该平台支持的 graphics API 列表，并允许你重排默认与回退顺序。
- Editor 也会随列表顶部的图形后端切换。

这组事实说明，Unity 的跨平台图形不是“反正底层你不用管”，而是：

- 先承认一个平台可能对应多种图形后端。
- 再把后端选择、优先级和回退顺序纳入统一工程入口。

基于这些事实，我的判断是：

`Unity 抽象掉的不是图形后端差异本身，而是项目如何统一管理这些后端。`

### `conditional compilation` 说明平台差异也会进入正式编译边界

从 Unity 关于 `Conditional compilation in Unity` 的文档里，还能直接看到：

- Unity 通过 scripting symbols 和 directives 正式管理代码包含或排除。
- 像 `UNITY_STANDALONE_WIN` 这样的符号只会让代码进入对应平台的编译结果，在其他目标里会被省略。
- 预定义符号会覆盖选中的平台、Editor version 以及其他系统环境。
- 更高层组织上，Unity 还推荐用 assembly definition 的 `define constraints` 管理条件编译。

如果只把条件编译理解成“补丁式小技巧”，会把它在跨平台层里的站位写没掉。

更接近官方资料支持的说法是：

- Unity 不假装所有代码路径在所有平台都相同。
- Unity 允许同一工程在不同目标上形成受控变体。
- 这些变体不是临时黑魔法，而是正式工程结构。

基于这些事实，我的判断是：

`Unity 的跨平台抽象包含一条平台感知的编译边界；它统一的是变体管理语言，而不是要求所有代码路径天然完全一致。`

### `Player settings + build profiles` 说明 per-platform 配置本身就是统一工作流的一部分

从 Unity 的 `Player` 和 `build profiles` 文档里，还能直接看到：

- `Player settings` 决定最终应用如何构建、如何显示。
- `Player settings` 会随着已安装的 `platform modules` 不同而不同，每个平台都有自己的设置面。
- `build profile` 被官方定义为“用于在特定平台上构建应用的一组 configuration settings”。
- 同一平台可以存在多个 profile，不同 profile 之间既能共享一部分内容，也能保留彼此独立的 build configurations。

这组事实说明，Unity 的跨平台不是消灭平台配置，而是把平台配置收编进同一套 Editor 工作流。

更接近事实的写法是：

- 项目仍然会有 per-platform settings。
- 项目仍然会有 per-platform build profile。
- 但这些东西不再散落在脚本、表格和群公告里，而是被引擎统一组织起来。

基于这些事实，我的判断是：

`Unity 的平台抽象层更像一套围绕 graphics APIs、条件编译、Player settings 与 build profiles 组织起来的统一平台工作流。`

### `platform-specific rendering differences` 说明后端差异并不会被彻底抹平

从 Unity 关于 `Write HLSL for different graphics APIs` 的文档里，还能直接落下几件事实：

- Unity 官方明确不同 graphics APIs 的渲染行为仍有差异。
- 有些差异 Editor 大多数时候会替你处理，但并不是全部。
- shader 语义、buffer layout、坐标系方向、depth direction 等行为会随 graphics API 改变。
- 官方还要求通过诸如 `UNITY_UV_STARTS_AT_TOP`、`UNITY_REVERSED_Z`、`SystemInfo.usesReversedZBuffer` 这样的宏和运行时信息处理差异。

这组事实非常关键。

因为它直接说明：即使有统一工作流，平台差异仍然会在 shader 语义、坐标约定和渲染行为这些地方露出来。

基于这些事实，我的判断是：

`Unity 统一的是“处理这些差异的工程表面”，不是承诺所有后端行为天然完全一致。`

把这一节收成一句话，就是：

`Unity 的平台抽象层更像一套把后端列表、编译变体、平台配置与渲染差异处理纳入同一项目工作流的工程组织层。`

这里还不是在讲 `IL2CPP`、运行时底座、完整打包链或具体平台接入按钮；这里只先解释这条层为什么属于引擎本体。

## Unreal 怎么落地

再看 Unreal 官方文档给出的平台抽象链条。

### `RHI` 说明统一的不是单一实现，而是统一调用边界

从 Unreal 关于 `RHI` 与 `FNullDynamicRHIModule` 的文档里，能直接落下几件事实：

- Unreal 把 `RHI` 单独作为运行时模块暴露出来。
- `FNullDynamicRHIModule` 被官方写成 dynamic RHI providing module。
- `CreateRHI` 的职责是创建由该模块实现的 dynamic RHI 实例。
- 具体图形后端还会继续落到 `NullDrv`、`VulkanRHI` 这类模块中。

这组事实说明，Unreal 的跨平台图形抽象不是“只有一套具体实现”，而是：

- 先把渲染调用面抽出来。
- 再把具体后端落到不同模块实现中。

基于这些事实，我的判断是：

`Unreal 的跨平台图形核心不是把所有 backend 视为同一实现，而是用 RHI 这层接口边界把统一调用面和具体后端实现分开管理。`

### `target platform` 说明平台本身被抽象成正式对象，而不是藏在几个宏后面

从 Unreal 关于 `ITargetPlatform`、`ITargetPlatformModule` 与 `IniPlatformName()` 的文档里，还能直接看到：

- `ITargetPlatform` 就是 target platform interface。
- `ITargetPlatform` 同时继承 `ITargetPlatformSettings` 与 `ITargetPlatformControls`。
- 官方明确不需要 SDK 的能力应放进 `Settings`，需要 SDK 的能力应放进 `Controls`。
- `ITargetPlatformModule` 会维护平台的 settings 与 controls 集合。
- `IniPlatformName()` 用于让离线工具加载对应 target platform 的 INI 配置。

如果只把“跨平台”理解成若干编译宏和少量 if 分支，就会看不见这里真正的结构。

更接近事实的说法是：

- Unreal 把平台本身抽象成正式对象。
- 这个对象有自己的 settings、controls、SDK 边界和配置入口。
- 平台差异不是被埋掉了，而是被对象化、模块化了。

基于这些事实，我的判断是：

`Unreal 的平台抽象层不是把平台差异塞进黑箱，而是把平台本身做成正式对象模型。`

### `target platform settings` 说明能力查询也是平台抽象的一部分

从 Unreal 关于 `ITargetPlatformSettings` 的文档里，还能直接看到：

- target platform settings 会暴露 `GetAllPossibleShaderFormats` 与 `GetAllTargetedShaderFormats`。
- 它还会暴露 `GetHostArchitecture`、`GetPossibleArchitectures` 这类架构相关接口。
- 它会回答 `UsesForwardShading`、`UsesDBuffer`、`UsesDistanceFields`、`UsesRayTracing` 这类渲染特性问题。
- 它还会通过 `SupportsValueForType` 这类接口判断目标平台是否支持某类能力值。

这组事实说明，Unreal 的平台抽象不是“统一后就再也不谈平台能力”。

恰恰相反，它是把平台能力差异写进了一套统一查询表面。

基于这些事实，我的判断是：

`Unreal 的平台抽象层统一的是能力查询语言，而不是能力本身。`

### `build configurations + target-platform requirements` 说明统一构建术语下仍保留显式要求

从 Unreal 的 `Build Configurations Reference` 和 `Packaging Your Project` 文档里，还能直接落下几件事实：

- UE 使用 `Unreal Build Tool` 作为自定义构建方法。
- build configuration 被官方写成 `state + target` 的组合。
- `Game` target 需要与平台相关的 cooked content。
- 某些 target platforms 需要额外 SDK、UE 组件，主机平台甚至可能要求源码版引擎。

这组事实说明，Unreal 的跨平台构建不是“一份产物直接跑遍所有目标”。

更接近事实的写法是：

- 同一套构建语言仍然要显式区分 target。
- 同一套 build 体系仍然要显式面对 cooked content、SDK 和组件要求。
- 统一存在于词汇和边界上，不存在于“最终结果完全相同”上。

基于这些事实，我的判断是：

`Unreal 的平台抽象层更像一套由 RHI、target platform 对象模型、能力查询接口与 build configurations 共同组成的平台管理边界。`

这里还不是在讲 RHI 内部调度、驱动交互、cook 细节或平台性能比较；这里只先解释这层为什么必须被放回引擎架构本体。

## 为什么这不是“所有平台完全一样”

把前面两节再压一次，至少能先看出四个稳定判断。

### 第一，统一调用面不等于消灭后端差异

Unity 仍保留 `graphics APIs` 列表与 platform-specific rendering differences。  
Unreal 仍保留 `RHI` 之下的 backend modules。

所以“统一”不等于“只剩一个底层世界”，而是：

`项目不需要直接对每个平台重新发明一套工程语言，但底层后端差异仍然存在。`

### 第二，统一工程语言不等于取消变体

Unity 有 `conditional compilation`、`define constraints`、`build profiles`。  
Unreal 有 `target platform`、`build configurations`、平台能力与目标相关要求。

所以成熟的跨平台引擎不是没有变体，而是：

`把变体收编成正式结构。`

### 第三，per-platform settings 越清晰，越说明抽象层把差异收束到了正式入口

很多人会误以为：“设置越多，说明引擎越不抽象。”

但更接近工程现实的判断恰好相反。

如果一个引擎真的要面对多个后端、多个平台和多个目标，它就必须给出：

- 正式的后端入口
- 正式的编译变体边界
- 正式的 per-platform settings
- 正式的能力查询表面
- 正式的 target 要求管理

所以 per-platform settings 的存在，不是抽象失败，而是：

`抽象层已经把差异从混乱状态收束进了制度化入口。`

### 第四，跨平台真正被抽象掉的是工程混乱，不是平台现实

平台现实依然存在：

- 图形后端不同
- shader 语义不同
- 平台能力不同
- SDK 要求不同
- 构建目标不同

引擎真正提供的价值，是让这些差异不再以碎片化方式侵入整个项目。

所以最稳的写法不是：

`跨平台等于所有平台完全一样。`

而是：

`跨平台等于用统一工程语言管理不可避免的平台差异。`

## 常见误解

### 误解一：能同时导出多个平台，就等于同一份最终产物天然跑遍所有平台

事实并不是这样。

Unity 仍然保留后端选择、条件编译与 per-platform profile。  
Unreal 仍然保留 target、requirements、cooked content 和 SDK 边界。

所以“多平台可发”不等于“同一份最终结果天然完全一致”。

### 误解二：有 `graphics APIs` 列表或 `RHI`，就说明 DirectX / Metal / Vulkan / OpenGL 已经等价

这也是过度推断。

Unity 官方仍明确写了 platform-specific rendering differences。  
Unreal 官方仍明确保留不同 backend modules 与 target-platform settings。

所以统一入口不等于底层行为等价。

### 误解三：平台设置越多，说明引擎越不抽象

真正没有完成抽象的情况，反而是：

- 平台差异散落在脚本里
- 构建规则散落在文档里
- 能力边界只能靠团队口耳相传

而不是引擎把它们集中到正式接口和配置层中。

### 误解四：跨平台只和渲染有关，与编译、SDK、build target 无关

两边官方资料都足够说明，这个判断站不住。

Unity 把条件编译、Player settings 和 build profiles 写成正式主题。  
Unreal 把 target platform settings、build configurations 与 target-platform requirements 写成正式主题。

所以平台抽象层同时管：

- 图形后端
- 编译变体
- 平台配置
- 能力查询
- 构建目标

而不是只管图形 API。

### 误解五：这篇应该顺手把 `cook / package / build` 或平台接入教程一起讲完

这会直接把文章写偏。

完整资产与发布链已经属于 `05`。  
具体接入步骤、按钮路径和平台发布操作也不是本文任务。

本文只先回答：

`为什么这些平台相关结构应该被看成同一层平台抽象层。`

## 我的结论

先重申这篇能直接成立的事实。

- Unity 官方把 `graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences` 写成统一工程工作流中的正式主题。
- Unreal 官方把 `RHI / target platform / target platform settings / build configurations / target-platform requirements` 写成正式接口、对象模型与构建语汇。
- 当前本地源码路径还没有任何 `READY` 标记，因此这篇不能声称自己做了源码级验证。

基于这些事实，我在这篇里愿意先给出的工程判断是：

`跨平台引擎抽象的不是“平台被抹平”，而是“项目如何以统一工程语言落到不同平台”。`

进一步说：

- Unity 这层更接近一套围绕 `graphics APIs`、条件编译、`Player settings`、`build profiles` 与渲染差异处理组织起来的统一平台工作流。
- Unreal 这层更接近一套围绕 `RHI`、`target platform`、`target platform settings` 与 `build configurations` 组织起来的平台对象模型和查询边界。

所以这篇最值得先记住的一句话不是：

`跨平台等于所有平台完全一样。`

而是：

`跨平台等于用统一工程语言管理不可避免的平台差异。`

这也解释了为什么后面还需要 `08`。  
只有把这层讲清楚，最后的总收束才不会滑成“谁更强”的产品比较，而能真正回到整张架构地图上。
