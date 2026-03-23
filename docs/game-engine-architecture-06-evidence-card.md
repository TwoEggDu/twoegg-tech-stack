# 游戏引擎架构地图 06｜证据卡：跨平台引擎到底在抽象什么

## 本卡用途

- 对应文章：`06`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`Unity 和 Unreal 所谓“跨平台”，到底把哪些差异收进统一抽象，哪些差异仍以图形后端、编译目标、平台配置、SDK 与特性开关的形式保留下来。`
- 这篇不展开：`00 总论里的整张六层地图，只借它做定位，不重写整篇总论`
- 这篇不展开：`03 里脚本后端、反射、GC、任务系统、PlayerLoop / Task Graph 的运行时底座机制`
- 这篇不展开：`04 里渲染、物理、动画、音频、UI 等专业子系统的内部组织`
- 这篇不展开：`05 里 Asset Database / Asset Manager / Cook / Package / Build 的完整资产与发布链`
- 这篇不展开：`07 里 DOTS / Mass 这类数据导向扩展层`
- 这篇不展开：`08 里 Unity / Unreal 的总体气质收束`
- 这篇不写成：`iOS / Android / 主机平台接入教程、平台设置按钮说明书、图形 API 百科或产品优劣比较`
- 本篇允许做的事：`只锁定 Unity 的 graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences，与 Unreal 的 RHI / target platform / target platform settings / build configurations / target-platform requirements 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与 API，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把图形后端写成可配置的一组 graphics APIs，而不是默认只绑定一个固定 API

- Unity 入口：
  - [Configure graphics APIs](https://docs.unity3d.com/Manual/configure-graphicsAPIs.html)
- 可直接证明的事实：
  - Unity 官方明确 Unity 会使用内置的一组 graphics APIs，或者使用你在 Editor 里选定的 graphics APIs。
  - Unity 官方明确当 `Auto Graphics API` 开启时，Player build 会包含该平台的一组内置 graphics APIs，并在运行时选择合适的 API。
  - Unity 官方明确当 `Auto Graphics API` 关闭时，Unity 会显示该平台支持的 graphics API 列表，并按列表顺序选择默认与回退项。
  - Unity 官方明确你可以重排 graphics API 列表，Editor 也会随之切换到列表顶部的图形后端。
- 暂定判断：
  - Unity 的“跨平台图形”不是把所有平台都压成同一个 API，而是提供一个统一工程入口来管理同一项目可落到哪些后端、默认优先级是什么、运行时如何选用。

### 2. Unity 官方把条件编译写成按平台、编辑器版本与环境差异裁剪代码的正式机制，而不是“同一份代码原样到处编译”

- Unity 入口：
  - [Conditional compilation in Unity](https://docs.unity3d.com/Manual/platform-dependent-compilation.html)
- 可直接证明的事实：
  - Unity 官方明确可以通过 directives 根据 scripting symbols 是否定义来选择性地包含或排除代码。
  - Unity 官方明确像 `UNITY_STANDALONE_WIN` 这样的符号只会让代码进入对应平台的编译结果，在其他目标或 Editor 中会被完全省略。
  - Unity 官方明确预定义符号覆盖选中的 `Platform`、`Editor Version` 以及其他系统环境场景。
  - Unity 官方明确在更高层组织上，推荐通过 assembly definition 与 define constraints 管理条件编译。
- 暂定判断：
  - Unity 的跨平台抽象包含一条“平台感知的编译边界”；它追求的是同一工程在不同目标上有受控变体，而不是所有代码路径天然完全一致。

### 3. Unity 官方把 Player settings 与 build profiles 写成统一工作流中的平台配置层，而不是把平台差异藏掉

- Unity 入口：
  - [Player](https://docs.unity3d.com/Manual/class-PlayerSettings.html)
  - [Introduction to build profiles](https://docs.unity3d.com/Manual/build-profiles.html)
- 可直接证明的事实：
  - Unity 官方明确 Player settings 决定最终应用“如何构建、如何显示”。
  - Unity 官方明确 Player settings 会随着已安装的 `platform modules` 不同而不同，每个平台都有自己的 Player settings。
  - Unity 官方明确 build profile 是“用于在特定平台上构建应用的一组 configuration settings”，并且可以为每个平台建立多个 profile。
  - Unity 官方明确平台 profile 之间会共享一部分设置与 scene 数据，而独立的 build profiles 可以保存彼此独立的 build configurations。
- 暂定判断：
  - Unity 的跨平台不是消灭平台配置，而是把平台配置纳入统一的 Editor 与资产化工作流里，让同一工程可以带着明确的 per-platform build profile 运行。

### 4. Unity 官方同时明确图形 API 之间仍有不能完全隐藏的差异

- Unity 入口：
  - [Write HLSL for different graphics APIs](https://docs.unity3d.com/Manual/SL-PlatformDifferences.html)
- 可直接证明的事实：
  - Unity 官方明确不同 graphics APIs 的图形渲染行为存在差异，“大多数时候” Editor 会隐藏这些差异，但有些情况无法替你处理。
  - Unity 官方明确 shader 语义、buffer layout、坐标系方向、depth direction 等行为会随 graphics API 改变。
  - Unity 官方明确要通过 `UNITY_UV_STARTS_AT_TOP`、`UNITY_REVERSED_Z`、`SystemInfo.usesReversedZBuffer` 等宏或运行时信息处理这些差异。
  - Unity 官方明确 DirectX、Metal、Vulkan 与 OpenGL 系列在纹理坐标和深度空间上并不完全一致。
- 暂定判断：
  - Unity 的抽象能覆盖大量通用工作，但不会把底层图形世界抹平成“所有平台行为完全相同”；平台差异在 shader 语义与坐标约定处仍会露出来。

### 5. Unreal 官方把 RHI 写成动态模块接口与具体后端实现的分层，而不是单一图形实现

- Unreal 入口：
  - [FNullDynamicRHIModule](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/NullDrv/FNullDynamicRHIModule)
  - [FNullDynamicRHIModule::CreateRHI](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/NullDrv/FNullDynamicRHIModule/CreateRHI)
  - [RHI API module](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/RHI)
- 可直接证明的事实：
  - Unreal 官方明确 `FNullDynamicRHIModule` 是“dynamic RHI providing module”，并继承 `IDynamicRHIModule`。
  - Unreal 官方明确 `CreateRHI` 的职责是“创建由该模块实现的 dynamic RHI 实例”。
  - Unreal 官方 API 结构把 `RHI` 单独作为运行时模块暴露出来，而具体实现还会落到如 `NullDrv`、`VulkanRHI` 这类模块中。
  - Unreal 官方文档命名本身已经区分了 `RHI` 抽象层与具体后端模块，而不是把所有图形平台写成同一套具体实现。
- 暂定判断：
  - Unreal 的跨平台图形抽象核心是 RHI 这层接口与模块边界，它让引擎把“统一渲染调用面”与“各图形后端实现”分开管理，而不是宣称所有后端等价。

### 6. Unreal 官方把 target platform 写成显式的平台对象模型，而不是隐藏在几个编译宏背后

- Unreal 入口：
  - [ITargetPlatform](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/ITargetPlatform)
  - [ITargetPlatformModule](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformModule)
  - [ITargetPlatformSettings::IniPlatformName](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings/IniPlatformName)
- 可直接证明的事实：
  - Unreal 官方明确 `ITargetPlatform` 就是“Interface for target platforms”。
  - Unreal 官方明确 `ITargetPlatform` 同时继承 `ITargetPlatformSettings` 与 `ITargetPlatformControls`。
  - Unreal 官方明确如果某个能力“不需要 SDK”应放进 `Settings`，如果“需要 SDK”应放进 `Controls`。
  - Unreal 官方明确 `ITargetPlatformModule` 是 target platform modules 的接口，并维护 `PlatformControls` 与 `PlatformSettings` 集合。
  - Unreal 官方明确 `IniPlatformName()` 用于让离线工具加载对应 target platform 的 INI 配置。
- 暂定判断：
  - Unreal 的跨平台不是把平台差异埋进黑盒，而是把平台本身抽象成带 settings、controls、SDK 边界与配置读取入口的正式对象。

### 7. Unreal 官方把 shader format、架构与渲染特性开关写成 target platform settings 的正式职责

- Unreal 入口：
  - [ITargetPlatformSettings](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings)
  - [ITargetPlatformSettings::GetAllPossibleShaderFormats](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings/GetAllPossibleShaderFormats)
  - [ITargetPlatformSettings::GetAllTargetedShaderFormats](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings/GetAllTargetedShaderFormats)
  - [ITargetPlatformSettings::UsesRayTracing](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings/UsesRayTracing)
  - [ITargetPlatformSettings::SupportsValueForType](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/Interfaces/ITargetPlatformSettings/SupportsValueForType)
- 可直接证明的事实：
  - Unreal 官方明确 `ITargetPlatformSettings` 提供 `GetAllPossibleShaderFormats` 与 `GetAllTargetedShaderFormats` 这类能力查询。
  - Unreal 官方明确 target platform settings 还会暴露 `GetHostArchitecture`、`GetPossibleArchitectures` 这类架构相关接口。
  - Unreal 官方明确 target platform settings 会回答 `UsesForwardShading`、`UsesDBuffer`、`UsesDistanceFields`、`UsesRayTracing` 等渲染特性问题。
  - Unreal 官方明确 `SupportsValueForType` 这种接口专门用来判断目标平台是否支持某类能力值。
- 暂定判断：
  - Unreal 的平台抽象不是“统一后就不再关心平台能力”，而是通过 target platform settings 把 shader format、架构与特性差异显式纳入统一查询面。

### 8. Unreal 官方把 build target 与 target-platform requirements 写成统一构建词汇下的显式差异，而不是“一份构建到处直接跑”

- Unreal 入口：
  - [Build Configurations Reference for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/build-configurations-reference-for-unreal-engine)
  - [Packaging Your Project](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project)
- 可直接证明的事实：
  - Unreal 官方明确 UE 使用 Unreal Build Tool 作为自定义构建方法。
  - Unreal 官方明确每个 build configuration 都由两个关键字组成：`state` 与 `target`。
  - Unreal 官方明确 `Game` target 需要“specific to the platform”的 cooked content，`Editor` target 则服务于在 Unreal Editor 中打开项目。
  - Unreal 官方明确 target platform 就是项目要运行的操作系统或主机平台；某些 target platforms 还需要额外 SDK、UE 组件，主机平台甚至要求源码版引擎。
- 暂定判断：
  - Unreal 的跨平台构建不是把所有目标压成同一产物，而是在统一的 UBT 词汇之下，清楚区分 state、target、cooked content 与平台依赖要求。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把 graphics APIs 写成可自动选择或手动排序的一组平台后端，Player build 会按平台和运行时条件选用。
- `事实`：Unity 官方把条件编译写成按平台、编辑器版本与环境裁剪代码的正式机制，而不是运行时再判断的附属技巧。
- `事实`：Unity 官方把 Player settings 与 build profiles 写成统一工作流里的平台配置层，并明确不同 platform modules 会带来不同设置。
- `事实`：Unity 官方同时明确图形 API 在 shader 语义、buffer layout、坐标系与深度方向上仍有无法完全隐藏的差异。
- `事实`：Unreal 官方把 RHI 写成独立模块与 dynamic RHI 实例创建机制，并把具体图形后端落到不同模块实现。
- `事实`：Unreal 官方把 target platform 写成显式接口体系，区分 settings、controls、SDK 边界与 per-platform INI 配置读取。
- `事实`：Unreal 官方把 shader format、架构与渲染特性支持写进 target platform settings 的正式查询接口里。
- `事实`：Unreal 官方把 build configuration 写成 `state + target` 的组合，并明确某些 target platforms 需要特定 cooked content、SDK、组件或源码版引擎。
- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `06` 可以把“平台抽象层”定义为那一层负责在统一工程入口下管理图形后端、编译变体、目标平台配置与平台能力差异的引擎层。
- `判断`：对 Unity 来说，最稳的落点是 `graphics APIs + conditional compilation + Player settings + build profiles + platform-specific rendering differences` 共同说明它是在统一工作流里包裹平台差异，而不是消灭平台差异。
- `判断`：对 Unreal 来说，最稳的落点是 `RHI + target platform + target platform settings + build configurations` 共同说明它把平台差异显式对象化、模块化与查询化。
- `判断`：本篇最安全的比较方式不是比较哪台引擎“更跨平台”，而是说明两台引擎都要把无法回避的后端、能力、架构与构建差异纳入统一抽象壳中。
- `判断`：文章 `06` 的稳定结论不应是“跨平台等于所有平台完全一样”，而应是“跨平台等于用统一工程语言管理不可避免的平台差异”。

## 本卡暂不支持的强结论

- 不支持：`Unity 的 build profiles / Player settings 与 Unreal 的 target platform / build configuration 已经可以严格一一映射`
- 不支持：`有了 Unity 的 graphics API 列表或 Unreal 的 RHI，就说明 DirectX / Metal / Vulkan / OpenGL 的实现代价与行为已经完全一致`
- 不支持：`只凭官方文档就下出后端实现、RHI 调度、驱动交互、平台性能损耗或特性兼容性的源码级定论`
- 不支持：`任意平台都能在不做条件编译、不做 shader 适配、不管 SDK 要求的前提下直接共享同一套最终结果`
- 不支持：把这篇写成 `iOS / Android / 主机平台接入` 的按钮教程、平台发布手册、图形 API 百科或产品优劣比较
- 不支持：把 `05` 的完整 Cook / Build / Package 链路、`03` 的运行时底座或 `08` 的总体气质顺手混写进本篇

## 下一次最合适的增量

- 基于本卡给 `06` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么这不是“所有平台完全一样”
  7. 常见误解
  8. 我的结论
