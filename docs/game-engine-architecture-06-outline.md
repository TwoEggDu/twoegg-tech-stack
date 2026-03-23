# 游戏引擎架构地图 06：详细提纲：跨平台引擎到底在抽象什么？
## 本提纲用途
- 对应文章：`06`
- 本次增量类型：`详细提纲`
- 证据基础：`docs/game-engine-architecture-06-evidence-card.md`
- 证据等级：`官方文档`
- 约束说明：`docs/engine-source-roots.md` 中 Unity / Unreal 仍不是 `READY`，本提纲只安排“官方资料明确写了什么”和“基于这些事实的暂定判断”，不写源码级定论。

## 文章主问题与边界

- 这篇只回答：`Unity 和 Unreal 所谓“跨平台”，到底把哪些差异收进统一工程抽象，哪些差异仍以图形后端、编译目标、平台配置、能力查询和 SDK 要求的形式保留下来？`
- 这篇不展开：`00` 里的整张六层地图，只借它做定位，不重写总论。
- 这篇不展开：`03` 里的脚本后端、反射、GC、任务系统、PlayerLoop / Task Graph 等运行时底座机制。
- 这篇不展开：`04` 里的渲染、物理、动画、音频、UI 等专业子系统内部组织。
- 这篇不展开：`05` 里的 Asset Database / Asset Manager / cook / package / build 的完整资产与发布链路。
- 这篇不展开：`07` 里的 DOTS / Mass 这类数据导向扩展层。
- 这篇不展开：`08` 里的 Unity / Unreal 总体气质收束。
- 这篇不写成：`iOS / Android / 主机平台接入教程、平台设置按钮说明书、图形 API 百科或产品优劣比较。`
- 本篇允许落下的判断强度：`只把 Unity 的 graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences，与 Unreal 的 RHI / target platform / target platform settings / build configurations / target-platform requirements 收回“平台抽象层”；不宣称平台差异被彻底抹平，也不做一一等价映射。`

## 一句话中心判断

- `Unity 的这一层更接近“围绕 graphics APIs、条件编译、Player settings、build profiles 与渲染差异处理组织起来的统一平台工作流”；Unreal 的这一层更接近“围绕 RHI、target platform、target platform settings 与 build configurations 组织起来的平台对象模型和查询边界”。`
- `因此，跨平台引擎最稳定的架构站位不是“让所有平台完全一样”，而是“用统一工程语言管理图形后端、编译变体、平台配置、能力差异与构建目标”。`

## 行文顺序与字数预算
| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇要回答什么？ | 350 - 500 | 把“怎么发到多个平台”改写成“跨平台层到底抽象了什么” |
| 2. 这一层负责什么？ | 700 - 950 | 定义平台抽象层负责哪些统一入口，并给出总对照表 |
| 3. 这一层不负责什么？ | 350 - 500 | 明确本篇不越界到运行时、资产发布、专业子系统、DOTS / Mass 与产品比较 |
| 4. Unity 怎么落地 | 950 - 1200 | 沿 `graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences` 铺开 Unity 的平台抽象层 |
| 5. Unreal 怎么落地 | 950 - 1200 | 沿 `RHI / target platform / target platform settings / build configurations / target-platform requirements` 铺开 Unreal 的平台抽象层 |
| 6. 为什么这不是“所有平台完全一样” | 550 - 750 | 把差异收回到“统一工程语言管理不可避免差异” |
| 7. 常见误解 | 450 - 650 | 集中拆掉“跨平台等于一套产物跑所有平台”“设置越多越不抽象”等误读 |
| 8. 我的结论 | 250 - 400 | 收束成一条架构判断，并把后续系列重新挂回总地图 |

## 详细结构

### 1. 这篇要回答什么？
- 开篇切口：
  - 先写常见看法：`跨平台` 往往被理解成“一套代码、一份资源、一键导出，到处都一样”。
  - 再写常见误读：只要 Unity / Unreal 能同时面向多个平台，它们就一定把所有平台差异都藏掉了。
- 要抛出的核心问题：
  - `如果官方文档里仍然正式保留 graphics API 列表、条件编译、per-platform settings、target platform、shader format、SDK requirements 这些词，那跨平台层真正统一的到底是什么？`
- 这一节要完成的动作：
  - 把问题从“怎么适配平台”改写成“引擎把哪些差异提升成正式工程抽象”。
  - 明确本文不写移植步骤、平台按钮操作、图形 API 入门或产品优劣比较。
  - 提醒当前证据边界只到官方文档，不假装已经做过源码级验证。
- 可直接引用的证据锚点：
  - 证据卡 `1` 到 `8`
- 本节事实与判断分界：
  - `事实`：Unity / Unreal 官方都把图形后端、条件编译、平台配置、能力查询和构建目标写成正式接口或配置主题。
  - `判断`：因此“跨平台”可以被稳定地收成引擎的一层，而不是零散技巧集合。

### 2. 这一层负责什么？
- 本节先定义“平台抽象层”到底负责什么：
  - 把同一项目可落到哪些图形后端、后端优先级如何、何时自动选择，纳入统一工程入口。
  - 把平台感知的编译变体写成正式规则，而不是把所有差异都拖到运行时再临时判断。
  - 把每个平台自己的配置项、profile、SDK 要求、能力边界组织成正式平台对象或配置层。
  - 把 shader format、架构、图形特性、平台支持能力写成可查询的统一表面。
  - 把最终构建目标放回统一术语下管理，但不假装所有目标会得到完全相同的产物。
- 建议放一张总对照表：
| 对照维度 | Unity 平台抽象层 | Unreal 平台抽象层 | 本节要压出的判断 |
| --- | --- | --- | --- |
| 图形后端组织 | `graphics APIs` 列表、`Auto Graphics API`、按平台排序与回退 | `RHI`、dynamic RHI module、具体 backend module | 引擎先统一调用面，再保留 backend 差异 |
| 编译变体 | `conditional compilation`、scripting symbols、assembly definition define constraints | `target platform` 边界、`build configurations`、目标相关编译路径 | 跨平台包含“受控变体管理”，不是一份代码原样通吃 |
| 平台配置 | `Player settings`、`build profiles`、platform modules | `ITargetPlatformSettings`、`ITargetPlatformControls`、platform INI | per-platform 配置不是失败，而是抽象层的一部分 |
| 能力查询 | 平台相关渲染差异、宏与运行时信息 | shader formats、架构、`UsesRayTracing`、`SupportsValueForType` | 统一入口不等于放弃能力边界 |
| 构建目标与要求 | per-platform build profile、不同平台模块带来不同设置 | `state + target`、target-platform requirements、SDK / source build requirements | 跨平台构建是在统一术语下管理显式要求 |
- 本节必须压出的判断：
  - `平台抽象层` 负责的不是“替你忘掉平台”，而是“让项目可以用统一工程语言处理必然存在的平台差异”。
  - 这一层之所以单列，不是因为设置菜单多，而是因为它控制了后端选择、变体切分、平台配置、能力查询和构建目标边界。
- 证据锚点：
  - 证据卡 `1` 到 `8`
- 本节事实与判断分界：
  - `事实`：官方资料直接支持 graphics APIs / RHI、conditional compilation / build configurations、Player settings / target platform settings、capability queries、target requirements 这些结构。
  - `判断`：这些结构足以把“跨平台”从宣传词压成具体的架构层职责。

### 3. 这一层不负责什么？
- 必须明确写出的边界：
  - 不把 `GC / reflection / PlayerLoop / Task Graph / scripting backend` 重讲成运行时底座文章，那是 `03` 的任务。
  - 不把 `Asset Database / Asset Manager / cook / package / build` 的完整链路重讲成资产与发布层文章，那是 `05` 的任务。
  - 不把 `render pipeline / RHI 具体实现 / physics / animation / audio / UI` 重讲成专业子系统文章，那是 `04` 的任务。
  - 不把 `DOTS / Mass` 写进来，那是 `07` 的任务。
  - 不做 `DirectX / Metal / Vulkan / OpenGL` API 百科、平台兼容性排名、性能强弱判断或接入教程。
- 建议用一段“为什么必须克制”收尾：
  - 如果把运行时、资产发布、子系统内部细节和平台接入步骤都混进来，这篇就会从架构文章滑成“跨平台开发大全”。
  - 本文只先证明一件事：`跨平台` 在引擎地图上的稳定站位，是一层正式的工程抽象。

### 4. Unity 怎么落地
- 本节只沿着 Unity 官方文档给出的平台抽象证据往下写，不做按钮教程。
#### 4.1 `graphics APIs` 说明跨平台图形先是“一组可管理后端”
- 可用材料：
  - `Configure graphics APIs`
- 可落下的事实：
  - Unity 可以使用内置的一组 `graphics APIs`，也可以在 Editor 中指定平台使用的后端列表。
  - `Auto Graphics API` 开启时，Player build 会带上该平台的一组图形后端并在运行时做选择。
  - `Auto Graphics API` 关闭时，Unity 会显示该平台支持的 API 列表，并允许调整优先级与回退顺序。
- 可落下的暂定判断：
  - Unity 的跨平台图形不是把所有平台压成一个固定 API，而是把“后端选择与优先级管理”放进统一工程入口。
- 证据锚点：
  - 证据卡 `1`

#### 4.2 `conditional compilation` 说明跨平台也包含正式的编译变体边界
- 可用材料：
  - `Conditional compilation in Unity`
- 可落下的事实：
  - Unity 官方用 scripting symbols 和 directives 正式管理代码包含或排除。
  - 平台、Editor version 与环境差异都会影响可编译路径。
  - 更高层组织上还推荐用 assembly definition 的 `define constraints` 管理条件编译。
- 可落下的暂定判断：
  - Unity 的平台抽象不是让所有代码路径天然一致，而是让同一项目在不同目标上有受控变体。
- 证据锚点：
  - 证据卡 `2`

#### 4.3 `Player settings + build profiles` 说明 per-platform 配置是统一工作流的一部分
- 可用材料：
  - `Player`
  - `Introduction to build profiles`
- 可落下的事实：
  - Player settings 决定应用如何构建、如何显示，而且会随着已安装的 platform modules 发生变化。
  - build profile 被官方定义为面向特定平台的 configuration settings，并允许同一平台存在多个 profile。
  - profile 之间既能共享一部分场景数据，也能保存彼此独立的 build configurations。
- 可落下的暂定判断：
  - Unity 的跨平台不是消灭平台配置，而是把平台配置纳入统一 Editor 工作流。
- 证据锚点：
  - 证据卡 `3`

#### 4.4 `platform-specific rendering differences` 说明后端差异并不会被彻底抹平
- 可用材料：
  - `Write HLSL for different graphics APIs`
- 可落下的事实：
  - Unity 官方明确不同 graphics APIs 之间在 shader 语义、buffer layout、坐标系方向、depth direction 上仍有差异。
  - 有些差异 Editor 能帮你隐藏，但并非全部；官方也提供宏和运行时信息去处理这些差异。
- 可落下的暂定判断：
  - Unity 抽象的是“统一管理这些差异的工程表面”，不是承诺所有平台行为天然一致。
- 证据锚点：
  - 证据卡 `4`

#### 4.5 本节收口
- 必须收成一句话：
  - `Unity 的平台抽象层更像一套把后端列表、编译变体、平台配置与渲染差异处理纳入同一项目工作流的工程组织层。`
- 必须提醒的边界：
  - 这里不是在讲 `IL2CPP`、运行时底座、完整 build / package 链，也不是在讲具体平台接入步骤。

### 5. Unreal 怎么落地
- 本节只沿着 Unreal 官方文档给出的平台抽象证据往下写，不做项目打包教程。
#### 5.1 `RHI` 说明统一的不是单一实现，而是统一调用边界
- 可用材料：
  - `FNullDynamicRHIModule`
  - `FNullDynamicRHIModule::CreateRHI`
  - `RHI API module`
- 可落下的事实：
  - Unreal 官方把 `RHI` 单独作为运行时模块暴露出来。
  - `FNullDynamicRHIModule` 被写成 dynamic RHI providing module，`CreateRHI` 的职责是创建对应的 dynamic RHI 实例。
  - 具体图形后端会继续落到 `NullDrv`、`VulkanRHI` 等模块中。
- 可落下的暂定判断：
  - Unreal 的跨平台图形核心不是单一后端，而是 `RHI` 这层接口边界和模块分层。
- 证据锚点：
  - 证据卡 `5`

#### 5.2 `target platform` 说明平台本身被抽象成正式对象，而不是藏在零散宏里
- 可用材料：
  - `ITargetPlatform`
  - `ITargetPlatformModule`
  - `ITargetPlatformSettings::IniPlatformName`
- 可落下的事实：
  - Unreal 官方把 `ITargetPlatform` 写成 target platform interface。
  - `ITargetPlatform` 同时继承 settings 与 controls 边界。
  - `ITargetPlatformModule` 明确维护平台 settings / controls 集合，`IniPlatformName()` 也把 per-platform 配置读取写成正式入口。
- 可落下的暂定判断：
  - Unreal 不是把平台差异塞进黑箱，而是把平台本身对象化、模块化和配置化。
- 证据锚点：
  - 证据卡 `6`

#### 5.3 `target platform settings` 说明能力查询也是平台抽象的一部分
- 可用材料：
  - `ITargetPlatformSettings`
  - `GetAllPossibleShaderFormats`
  - `GetAllTargetedShaderFormats`
  - `UsesRayTracing`
  - `SupportsValueForType`
- 可落下的事实：
  - target platform settings 会暴露 shader formats、架构、渲染特性和能力支持查询。
  - 官方接口明确允许引擎主动询问某平台是否支持某类值或某类渲染能力。
- 可落下的暂定判断：
  - Unreal 的平台抽象不是“统一后就不再关心平台能力”，而是把能力差异写进统一查询表面。
- 证据锚点：
  - 证据卡 `7`

#### 5.4 `build configurations + target-platform requirements` 说明统一构建语汇下仍保留显式要求
- 可用材料：
  - `Build Configurations Reference for Unreal Engine`
  - `Packaging Your Project`
- 可落下的事实：
  - UE 使用 Unreal Build Tool 作为自定义构建方法。
  - build configuration 被官方写成 `state + target` 的组合。
  - `Game` target 需要与平台相关的 cooked content；某些 target platforms 还要求额外 SDK、UE 组件，甚至源代码版引擎。
- 可落下的暂定判断：
  - Unreal 的跨平台构建不是一份产物到处跑，而是在统一术语下管理显式的目标差异与依赖要求。
- 证据锚点：
  - 证据卡 `8`

#### 5.5 本节收口
- 必须收成一句话：
  - `Unreal 的平台抽象层更像一套由 RHI、target platform 对象模型、能力查询接口与 build configurations 共同组成的平台管理边界。`
- 必须提醒的边界：
  - 这里不是在讲 RHI 内部调度、驱动交互、cook 细节或平台性能比较，也不做和 Unity 的一一对译。

### 6. 为什么这不是“所有平台完全一样”
- 本节要把前两节材料收回成 4 个判断：
  - `判断一`：统一调用面不等于消灭后端差异；Unity 会保留 graphics API 差异，Unreal 会保留具体 backend module。
  - `判断二`：统一工程语言不等于取消变体；条件编译、target platform settings、build configurations 本身就是变体管理表面。
  - `判断三`：per-platform settings 越清晰，越说明抽象层把差异收束到了正式入口，而不是说明抽象失败。
  - `判断四`：最安全的比较方式不是谁“更跨平台”，而是谁如何把不可避免的差异组织进工程边界。
- 建议收束段：
  - `跨平台` 真正被抽象掉的，是项目面对多平台时的工程混乱；真正没有被抽象掉的，是后端行为、能力边界、SDK 要求和目标差异本身。
- 本节事实与判断分界：
  - `事实`：官方文档持续保留 backend、settings、capability、requirements 等显式结构。
  - `判断`：因此跨平台层的价值在于“可管理”，不在于“彻底同质化”。

### 7. 常见误解
- 误解 `1`：
  - `能同时导出多个平台，就等于同一份最终产物天然跑遍所有平台。`
  - 纠正方式：官方始终把 target、profile、requirements、cooked content 或 per-platform API 列表保留下来。
- 误解 `2`：
  - `有 graphics API 列表或 RHI，就说明 DirectX / Metal / Vulkan / OpenGL 行为已经等价。`
  - 纠正方式：Unity 仍明确写渲染差异，Unreal 仍明确保留不同 backend module。
- 误解 `3`：
  - `平台设置越多，说明引擎越不抽象。`
  - 纠正方式：真正的抽象不是隐藏设置，而是把设置、能力和要求收进统一边界。
- 误解 `4`：
  - `跨平台只和渲染有关，与编译、SDK、build target 无关。`
  - 纠正方式：官方资料把这些内容都写进正式的平台接口和构建配置体系。
- 误解 `5`：
  - `这篇应该顺手把 cook / package / build 全讲完。`
  - 纠正方式：完整资产与发布链路属于 `05`，本文只保留与平台抽象直接相关的目标与要求边界。

### 8. 我的结论
- 收束顺序建议：
  - 先重申本文不是平台接入教程，而是在回答“跨平台引擎到底在抽象什么”。
  - 再重申可直接成立的事实。
  - 最后给出工程判断，并把后续文章挂回总地图。
- 本段必须写出的事实：
  - Unity 官方把 `graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences` 写成统一工程工作流中的正式主题。
  - Unreal 官方把 `RHI / target platform / target platform settings / build configurations / target-platform requirements` 写成正式接口、对象模型与构建语汇。
  - 当前没有本地 `READY` 的 Unity / Unreal 源码根路径，因此本文不声称源码级验证。
- 本段必须写出的判断：
  - `跨平台引擎` 抽象的不是“平台被抹平”，而是“项目如何以统一工程语言落到不同平台”。
  - 这层的稳定站位就是 `平台抽象层`：它负责把后端选择、编译变体、平台配置、能力查询和构建目标收进一个可管理的边界。
  - 这也解释了为什么 `08` 必须等到前置文章都完成后再写：只有把这层讲清楚，最后的气质总结才不会滑成产品比较。

## 起草时必须保留的一张对照表

| 对照维度 | Unity | Unreal | 本文要落下的判断 |
| --- | --- | --- | --- |
| 图形后端组织 | `graphics APIs` 列表、`Auto Graphics API`、后端优先级 | `RHI`、dynamic RHI module、backend modules | 抽象的是统一调用与管理边界，不是单一实现 |
| 编译变体 | `conditional compilation`、define constraints | `target platform` 边界、`build configurations` | 同一项目允许受控变体，而不是所有路径恒等 |
| 平台配置 | `Player settings`、`build profiles`、platform modules | `target platform settings`、`controls`、platform INI | per-platform 配置是平台抽象层内部结构 |
| 能力查询 | 渲染差异宏与运行时信息 | shader formats、架构、`UsesRayTracing`、`SupportsValueForType` | 统一入口并不会取消能力边界 |
| 构建目标与要求 | per-platform profile、不同模块带来不同设置 | `state + target`、SDK / 组件 / source build requirements | 构建差异被纳入统一术语，而不是被忽略 |

## 可直接拆出的两条短观点
- `跨平台不是把差异抹平，而是把差异纳入统一工程语法。`
- `如果一个引擎不显式管理后端、编译变体、平台配置和能力查询，它就还没有真正完成跨平台抽象。`

## 起草时必须反复自检的三件事

- `我有没有把这篇写成 iOS / Android / 主机平台接入教程、Player settings 按钮说明书、图形 API 百科或产品比较，而不是平台抽象层文章？`
- `我有没有把 03 / 04 / 05 / 07 / 08 的运行时、专业子系统、资产发布或总体气质内容抢写进来？`
- `我有没有把官方资料事实和工程判断明确分开，并持续提醒当前没有源码级验证？`
