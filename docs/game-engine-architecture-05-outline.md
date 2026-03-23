# 游戏引擎架构地图 05｜详细提纲：资源导入、Cook、Build、Package，为什么也是引擎本体

## 本提纲用途

- 对应文章：`05`
- 本次增量类型：`详细提纲`
- 证据基础：`docs/game-engine-architecture-05-evidence-card.md`
- 证据等级：`官方文档`
- 约束说明：`docs/engine-source-roots.md` 中 Unity / Unreal 仍不是 `READY`，本提纲只安排“官方资料明确写了什么”和“基于这些事实的暂定判断”，不写源码级定论。

## 文章主问题与边界

- 这篇只回答：`为什么资源导入、序列化、资产分发与最终 Build / Cook / Package 不是外围流程，而是把编辑中的内容重新组织成可运行产品的资产与发布层。`
- 这篇不展开：`00` 里的六层总地图全量说明
- 这篇不展开：`01` 里的编辑器工作流、Prefab / Blueprint、Package / Plugin 的内容生产层组织
- 这篇不展开：`02` 里的 `Scene / World`、`GameObject / Actor`、Gameplay Framework 默认对象世界差异
- 这篇不展开：`03` 里的脚本后端、反射、GC、任务系统、PlayerLoop / Task Graph 的运行时底座机制
- 这篇不展开：`04` 里的渲染、物理、动画、音频、UI 为什么像半自治专业子系统
- 这篇不展开：`06` 里的平台抽象、RHI、目标平台、硬件差异与 backend 细节
- 这篇不展开：`07 / 08` 里的 DOTS / Mass 站位与 Unity / Unreal 总体气质收束
- 这篇不做：`Addressables / AssetBundles / Cook / Package / Build` 的教程、参数百科、产品优劣比较、严格一一对译或源码级内部实现猜测
- 本篇允许落下的判断强度：`只把 Unity 的 Asset Database / serialization / Addressables / AssetBundles / BuildPipeline 与 Unreal 的 Asset Registry / Asset Manager / cook / package / Unreal Build Tool 收回“资产与发布层”，说明它们为什么都属于引擎本体；不做源码级强结论。`

## 一句话中心判断

- `Unity 的这层更接近“围绕 Asset Database、serialization、Addressables / AssetBundles、BuildPipeline 组织起来的资产转换与产品装配链”；Unreal 的这层更接近“围绕 Asset Registry、Asset Manager、cook / package、Unreal Build Tool 组织起来的资产索引、分发与发布装配链”。`
- `因此，资源导入、Cook、Build、Package 最稳的地图站位不是外围发布流程，而是引擎内部负责把编辑态内容、元数据、依赖关系和目标平台配置重组为可运行产品的资产与发布层。`

## 行文顺序与字数预算

| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇要回答什么 | 350 - 500 | 把“如何打包/如何发资源”改写成“为什么这属于引擎架构”的问题 |
| 2. 这一层负责什么 | 700 - 950 | 定义资产与发布层负责哪些再组织动作，并给出总对照表 |
| 3. 这一层不负责什么 | 350 - 500 | 明确本篇不越界到内容生产、运行时底座、平台抽象和产品比较 |
| 4. Unity 怎么落地 | 950 - 1200 | 沿 `Asset Database / serialization / Addressables / AssetBundles / BuildPipeline` 铺开 Unity 的资产与发布层 |
| 5. Unreal 怎么落地 | 950 - 1200 | 沿 `Asset Registry / Asset Manager / cook / package / Unreal Build Tool` 铺开 Unreal 的资产与发布层 |
| 6. 为什么这不是外围流程 | 550 - 750 | 把差异收回到编辑态重组、平台转换、依赖组织与最终装配 |
| 7. 常见误解 | 450 - 650 | 集中拆掉“只是发布按钮”“只是工程化约定”“只是 IDE 外挂”等误读 |
| 8. 我的结论 | 250 - 400 | 收束成一条架构判断，并把后续系列重新挂回总地图 |

## 详细结构

### 1. 这篇要回答什么

- 开篇切口：
  - 先写常见看法：`资源导入、打包、发版` 往往被看成开发后期的外围流程。
  - 再写常见误读：只要会点 `Build` 按钮、会配 `Addressables` 或会跑 `Package`，就算懂了这一层。
- 要抛出的核心问题：
  - `如果编辑器里的内容不经过引擎自己的导入、索引、序列化、cook 和 build 规则，就根本不能成为可运行产品，那么这些环节还能被当成外围工具吗？`
- 这一节要完成的动作：
  - 把问题从“怎么发布”改写成“为什么发布链属于引擎架构”。
  - 明确本文不写操作步骤、CI/CD 实操、参数清单或产品优劣比较。
  - 提醒当前证据边界只到官方文档，不假装已经做过源码级验证。
- 可直接引用的证据锚点：
  - 证据卡 `1` 到 `8`
- 本节事实与判断分界：
  - `事实`：Unity / Unreal 官方都把导入、索引、序列化、cook、build、package 写成正式系统，而不是“外部建议流程”。
  - `判断`：所以本文真正要解释的不是“发布有哪些步骤”，而是“为什么这些步骤构成了一层独立的资产与发布层”。

### 2. 这一层负责什么

- 本节要先定义“资产与发布层”到底负责什么：
  - 把编辑态内容接管为引擎可跟踪的资产对象、元数据和依赖关系。
  - 把可编辑状态转成可存储、可重建、可索引的项目数据结构。
  - 把资产组织成运行时可发现、可加载、可分组、可分发的交付单元。
  - 按目标平台把编辑态内容重新转换为运行态格式、缓存或 cooked content。
  - 把代码、配置、资源与分发单元重新装配成最终可发布产品。
- 建议放一张总对照表：

| 对照维度 | Unity 资产与发布层 | Unreal 资产与发布层 | 本节要压出的意思 |
| --- | --- | --- | --- |
| 编辑态入口 | `Asset Database`、source asset、`.meta`、artifact、reimport | `Asset Registry`、unloaded assets、`FAssetData`、package metadata | 资产先被引擎接管，而不是裸文件直接变产品 |
| 数据重建 | `serialization`、可存储的 `GameObject` state、项目数据重建 | package header tags、asset metadata、Primary / Secondary Asset 组织 | 编辑态内容要先变成引擎可重建和可查询的数据 |
| 交付组织 | `Addressables / AssetBundles`、dependencies、locations、release | `Asset Manager`、bundles、chunks、audit | 资源系统同时也是交付组织系统 |
| 平台转换 | build target 变化触发 reimport、不同平台 artifact、Player build | cook 到 target platform、优化、压缩、裁剪未使用数据 | 可运行产品要在这层被重新组织 |
| 最终装配 | `BuildPipeline` 组装 Player 与 AssetBundles | `Package` 组合 compiled code 与 cooked content，`UBT` 驱动构建 | build 不是外围脚本，而是引擎装配链的一部分 |

- 本节必须压出的判断：
  - `资产与发布层` 负责的不是“帮你导出文件”，而是“把编辑器里的内容重新组织成产品”。
  - 这层之所以单列，不是因为按钮多，而是因为它控制了资产身份、依赖关系、平台转换和最终交付边界。
  - 没有这层，编辑态世界和运行态产品之间就没有稳定的工程桥梁。
- 证据锚点：
  - 证据卡 `1` 到 `8`
- 本节事实与判断分界：
  - `事实`：官方资料直接支持 import、artifact、serialization、registry、bundle、chunk、cook、build operation、package、build tool 这些关键词。
  - `判断`：这些关键词已经足以把“资源系统”从辅助流程提升为“资产与发布层”。

### 3. 这一层不负责什么

- 必须明确写出的边界：
  - 不把 `Inspector / Prefab / Blueprint / Plugin` 的编辑器工作流重讲成内容生产层文章，那是 `01` 的任务。
  - 不把 `Scene / World / GameObject / Actor / Gameplay Framework` 重讲成默认对象世界文章，那是 `02` 的任务。
  - 不把 `GC / reflection / PlayerLoop / Task Graph / scripting backend` 重讲成运行时底座文章，那是 `03` 的任务。
  - 不把 `RHI / graphics backend / target platform abstraction / build target policy` 重讲成平台抽象文章，那是 `06` 的任务。
  - 不把 `DOTS / Mass` 重讲成数据导向扩展层文章，那是 `07` 的任务。
  - 不做 `Addressables / AssetBundles / Cook / Package / Build` 的按钮路径教程、参数百科、CI 脚本模板或产品比较。
- 建议用一段“为什么必须克制”收尾：
  - 如果把内容生产、运行时底座、平台抽象和交付工具全混进来，这篇就会从架构文章滑成“工程流程大全”。
  - 本文只先证明一件事：`为什么资产导入与最终发布链本身就是引擎本体的一层。`

### 4. Unity 怎么落地

- 本节只沿着 Unity 官方文档给出的资产与发布层证据往下写，不做工具教程。

#### 4.1 `Asset Database` 说明导入先是一套引擎管理的资产转换与同步系统

- 可用材料：
  - `Contents of the Asset Database`
- 可落下的事实：
  - source asset file 与 imported counterpart 会保持同步。
  - 导入会把源资源转换成 `Unity-optimized artifacts`。
  - `.meta` 文件会保存 import settings 与 `GUID`。
  - 资源内容、依赖、importer version 或 build target 变化都可能触发 reimport，并缓存不同平台 artifact。
- 可落下的暂定判断：
  - Unity 的资源导入不是“把文件放进工程”，而是引擎先把文件接管成带身份、依赖和平台产物缓存的资产系统。
- 证据锚点：
  - 证据卡 `1`

#### 4.2 `serialization` 说明项目数据和对象状态先要变成可存储、可重建的引擎数据

- 可用材料：
  - `Script serialization`
- 可落下的事实：
  - Unity 会把数据结构或 `GameObject` state 转成可存储并可稍后重建的格式。
  - 数据组织方式会直接影响 serialization 行为和项目性能。
  - serialization rules、custom serialization、how Unity uses serialization 被官方作为完整主题组织。
- 可落下的暂定判断：
  - 在 Unity 里，序列化不是孤立文件格式细节，而是连接编辑器状态、资产数据和后续构建链的基础设施。
- 证据锚点：
  - 证据卡 `2`

#### 4.3 `Addressables / AssetBundles` 说明交付边界从一开始就是引擎内建系统

- 可用材料：
  - `Addressables package`
  - `Use AssetBundles to load assets at runtime`
- 可落下的事实：
  - Addressables 提供组织、管理、load 与 release assets 的 `API and editor interface`。
  - Addressables 建立在 `AssetBundle` API 之上，并自动处理 bundle creation and management。
  - 它会处理 dependencies、locations、memory management，并支持 local / CDN 等交付位置。
  - AssetBundles 本身就被定义为可用于 patches / DLC 的 archive file format。
- 可落下的暂定判断：
  - Unity 的资源系统从这里开始已经同时是交付系统，而不是“构建完主包以后再想办法补资源”。
- 证据锚点：
  - 证据卡 `3`

#### 4.4 `BuildPipeline` 说明最终 Player 装配也是统一引擎职责

- 可用材料：
  - `BuildPipeline`
  - `Create a custom build script`
- 可落下的事实：
  - `BuildPipeline` 同时覆盖 `building players or AssetBundles`。
  - 自定义 build script 可以介入 `pre-build / post-build` 流程，也可由 command line 触发。
  - 官方示例允许先构建 AssetBundles，再构建 Player，并把 type information 传入 Player build。
  - AssetBundle 结果还可以注入 `StreamingAssets` 并与 build profile、PlayerSettings、EditorUserBuildSettings 一起决定最终产物。
- 可落下的暂定判断：
  - Unity 的 build 不是工程外部的最后拷贝动作，而是引擎内部把场景、类型、资源分发单元和目标平台配置装配为最终产品的过程。
- 证据锚点：
  - 证据卡 `4`

#### 4.5 本节收口

- 必须收成一句话：
  - `Unity 的资产与发布层更像一条从资产接管、状态序列化、资源分发到最终 Player 装配连续打通的产品装配链。`
- 必须提醒的边界：
  - 这里还不是在讲 `IL2CPP`、底层 runtime、Package Manager 或平台 backend。
  - 这里也不是在讲具体发版策略谁更先进。

### 5. Unreal 怎么落地

- 本节只沿着 Unreal 官方文档给出的资产与发布层证据往下写，不做项目打包教程。

#### 5.1 `Asset Registry` 说明资产先进入引擎维护的可查询索引世界

- 可用材料：
  - `Asset Registry in Unreal Engine`
- 可落下的事实：
  - `Asset Registry` 是编辑器子系统，会异步收集 unloaded assets 信息。
  - 这些信息会保存在内存里，使编辑器可以在不加载资产的情况下构建 asset list。
  - `FAssetData` 包含 object path、package name、class name、tag/value pairs 等元数据。
  - 许多 tag 会写进 `uasset header`，Registry 会把它们当作权威、最新的数据读取出来。
- 可落下的暂定判断：
  - Unreal 的资产不是“要用时再临时读文件”，而是先被引擎接管为一张持续维护的资产索引图。
- 证据锚点：
  - 证据卡 `5`

#### 5.2 `Asset Manager` 说明资产组织、分发和审计边界都是正式引擎结构

- 可用材料：
  - `Asset Management in Unreal Engine`
- 可落下的事实：
  - `Asset Manager` 是存在于 Editor 和 packaged game 的 `unique, global object`。
  - 它围绕 `Primary Assets` 与 `Secondary Assets` 工作。
  - 它能把内容划分为 `chunks`，并提供 disk / memory usage 审计能力。
  - `Asset Bundles` 可以和 Primary Asset 关联，并可在保存时声明或运行时注册。
- 可落下的暂定判断：
  - Unreal 的资产交付边界不是外部脚本后补出来的，而是引擎自己定义的资产分组、审计和分发结构。
- 证据锚点：
  - 证据卡 `6`

#### 5.3 `cook / package` 说明编辑器世界必须先被转换成目标平台运行世界

- 可用材料：
  - `Packaging Your Project`
- 可落下的事实：
  - packaging 被官方直接定义为 `build operation`。
  - build、cook、stage、package 是 packaging 过程中的核心阶段。
  - `Cook` 会把 geometry、materials、textures、Blueprints、audio 等资产转成目标平台可运行格式，并执行优化、压缩、裁剪未使用数据。
  - `Package` 会把 compiled code 与 cooked content 组装为 distributable files。
- 可落下的暂定判断：
  - Unreal 的编辑器内容不能直接等于产品，必须先经过 cook 和 package 这条正式引擎链路才能变成平台运行世界。
- 证据锚点：
  - 证据卡 `7`

#### 5.4 `Unreal Build Tool` 说明 build system 本身就是引擎内部规则系统

- 可用材料：
  - `How to Generate Unreal Engine Project Files for Your IDE`
  - `Build Configurations Reference for Unreal Engine`
- 可落下的事实：
  - `GenerateProjectFiles` 只是 Unreal Build Tool 的 wrapper。
  - UE build system 编译代码并不依赖 IDE project files。
  - UBT 会根据 `module` 与 `target build files` 发现源文件并组织编译。
  - build configuration 被官方作为正式主题来说明不同编译与分发形态。
- 可落下的暂定判断：
  - Unreal 的 build 不是 IDE 附件或外部脚本集合，而是由引擎自己的 module / target / configuration 规则驱动的产品装配系统。
- 证据锚点：
  - 证据卡 `8`

#### 5.5 本节收口

- 必须收成一句话：
  - `Unreal 的资产与发布层更像一条由资产索引、全局资产管理、cook / package 和 UBT 共同组成的产品分发与装配链。`
- 必须提醒的边界：
  - 这里还不是在讲 `RHI`、平台抽象、源码内部 cook 调度或 pak 布局细节。
  - 这里也不是在做和 Unity 的一一对译。

### 6. 为什么这不是外围流程

- 本节要把前两节材料收回成 4 个判断：
  - `判断一`：这条链路从资源进入工程时就已经开始，不是发版前最后一天才出现。
  - `判断二`：它处理的不是文件拷贝，而是资产身份、依赖关系、平台产物和可分发单元的再组织。
  - `判断三`：它同时连接编辑器、运行时和 packaged product，所以不是纯外部运维动作。
  - `判断四`：最终产品的内容边界、代码边界和平台边界都要在这里重新装配。
- 建议收束段：
  - `最容易写偏的，是把这篇写成“打包步骤总览”。更稳的写法，是反复收回到：没有这层，编辑器里的内容还不是产品；这层存在的意义，就是把内容重组为产品。`
- 本节事实与判断分界：
  - `事实`：Unity 官方写 artifact、serialization、Addressables、BuildPipeline；Unreal 官方写 registry、manager、cook、package、UBT。
  - `判断`：所以“资源系统”在现代引擎里本质上同时也是“交付系统”。

### 7. 常见误解

- 误解 `1`：
  - `会点 Build / Package 按钮，就等于理解这层。`
  - 纠正方式：本文关心的是资产与产品之间如何被重新组织，不是按钮路径。
- 误解 `2`：
  - `资源导入规则写在团队文档里就行，不算引擎架构。`
  - 纠正方式：真正的架构边界体现在引擎是否维护 artifact、metadata、dependencies、chunk、bundle、cook rule 和 build rule。
- 误解 `3`：
  - `Addressables / AssetBundles` 与 `Asset Manager / chunk / pak` 可以严格一一映射。
  - 纠正方式：本文只比较架构站位，不做严格术语对译或实现等价判断。
- 误解 `4`：
  - `构建系统只是 IDE 或 CI 的外部外挂。`
  - 纠正方式：官方文档明确 `BuildPipeline` 与 `UBT` 都是引擎自己的正式构建入口。
- 误解 `5`：
  - `编辑器里能运行的内容已经天然等于最终产品。`
  - 纠正方式：Unity 要经过 artifacts / bundles / Player build；Unreal 要经过 registry / cook / package / build rules，编辑态世界不会自动等于交付态世界。

### 8. 我的结论

- 收束顺序建议：
  - 先重申本文不是教程，而是在回答“为什么资产与发布链属于引擎本体”。
  - 再重申可直接成立的事实。
  - 最后给出工程判断，并把后续文章挂回总地图。
- 本段必须写出的事实：
  - Unity 官方把 `Asset Database / serialization / Addressables / AssetBundles / BuildPipeline` 写成连续的资产转换与构建链。
  - Unreal 官方把 `Asset Registry / Asset Manager / cook / package / Unreal Build Tool` 写成连续的资产索引、平台转换与产品装配链。
  - 当前没有本地 `READY` 的 Unity / Unreal 源码根路径。
- 本段必须写出的判断：
  - `资源导入、Cook、Build、Package` 在地图上的稳定站位是 `资产与发布层`。
  - 这层真正的重要性，不在“发版按钮”本身，而在“把编辑态内容重组为产品”的工程职责。
  - 这也解释了为什么后续还要单独写 `06`：当内容已经被装配为产品，下一层问题才是“跨平台引擎到底在抽象什么”。

## 起草时必须保留的一张对照表

| 对照维度 | Unity | Unreal | 本文要落下的判断 |
| --- | --- | --- | --- |
| 资产接管入口 | `Asset Database`、`.meta`、artifact、reimport | `Asset Registry`、`FAssetData`、unloaded asset metadata | 资产先被引擎接管，而不是裸文件直接变产品 |
| 数据与身份 | `serialization`、可重建对象状态、GUID | package metadata、Primary Asset、tag/value pairs | 编辑态内容要先变成可重建、可查询的数据 |
| 交付组织 | `Addressables / AssetBundles`、dependencies、locations | `Asset Manager`、bundles、chunks、audit | 资源系统本质上也是交付组织系统 |
| 平台转换 | build target 对应不同 artifact 与 Player build | cook 到 target platform、优化、裁剪 | 可运行产品必须经过平台重组 |
| 最终装配 | `BuildPipeline` 组装 Player 与 AssetBundles | `UBT + Package` 组装代码与 cooked content | build 不是外围脚本，而是引擎装配链 |

## 可直接拆出的两条短观点

- `Build / Cook / Package 不是发布按钮，而是引擎把编辑态内容二次组织为产品的装配链。`
- `资源系统如果只会存文件而不会追踪依赖、平台产物和交付边界，就还称不上现代引擎的资产与发布层。`

## 起草时必须反复自检的三件事

- `我有没有把这篇写成 Addressables / AssetBundles / Cook / Package 的操作教程、参数百科或产品比较，而不是资产与发布层文章。`
- `我有没有把 01 / 03 / 06 的内容生产、运行时底座、平台抽象细节抢写进来。`
- `我有没有把官方资料事实和工程判断明确分开，并持续提醒当前没有源码级验证。`
