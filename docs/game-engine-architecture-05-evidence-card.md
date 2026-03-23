# 游戏引擎架构地图 05｜证据卡：资源导入、Cook、Build、Package，为什么也是引擎本体

## 本卡用途

- 对应文章：`05`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`为什么资源导入、序列化、资产分发与最终 Build / Cook / Package 不是外围流程，而是把“编辑中的内容”重新组织成“可运行产品”的资产与发布层。`
- 这篇不展开：`00 总论里的整张六层地图，只借它做定位，不重写整篇总论`
- 这篇不展开：`01 里编辑器工作流、Prefab / Blueprint、Package / Plugin 的内容生产层组织`
- 这篇不展开：`02 里 Scene / World、GameObject / Actor、Gameplay Framework 的默认对象世界差异`
- 这篇不展开：`03 里脚本后端、反射、GC、任务系统、PlayerLoop / Task Graph 的运行时底座机制`
- 这篇不展开：`04 里渲染、物理、动画、音频、UI 为什么像半自治专业子系统`
- 这篇不展开：`06 里平台抽象、RHI、目标平台与硬件差异`
- 这篇不展开：`07 里 DOTS / Mass 这类数据导向扩展层`
- 这篇不展开：`08 里 Unity / Unreal 的总体气质收束`
- 本篇允许做的事：`只锁定 Unity 的 Asset Database / serialization / Addressables / AssetBundles / BuildPipeline，与 Unreal 的 Asset Registry / Asset Manager / Cooking / Packaging / Unreal Build Tool 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与 API，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把资源导入写成 Asset Database 驱动的导入与产物同步系统，而不是“把文件拷进工程”

- Unity 入口：
  - [Contents of the Asset Database](https://docs.unity3d.com/Manual/asset-database-contents.html)
- 可直接证明的事实：
  - Unity 官方明确 `Asset Database` 会让 source asset file 与 imported counterpart 保持同步。
  - Unity 官方明确导入时会把源资源转换成 `Unity-optimized artifacts`，供编辑器和运行时使用。
  - Unity 官方明确 `.meta` 文件保存 import settings 与 `GUID`，Library 中的 artifact 还会带 importer version 与 dependency information。
  - Unity 官方明确只要资源内容、依赖项、导入器版本或当前 build target 变化，就可能触发 reimport，并为不同平台缓存不同 artifact。
- 暂定判断：
  - Unity 的资源导入不是单纯文件管理，而是一套带元数据、依赖跟踪、平台产物缓存与重导入规则的引擎级资产转换系统。

### 2. Unity 官方把 serialization 写成能存储并重建项目数据的核心机制，而不是附属文件格式细节

- Unity 入口：
  - [Script serialization](https://docs.unity3d.com/Manual/script-serialization.html)
- 可直接证明的事实：
  - Unity 官方明确 serialization 是把数据结构或 `GameObject` state 自动转换成 Unity 可存储并稍后重建的格式。
  - Unity 官方明确项目中的数据组织方式会直接影响 serialization 行为，并可能显著影响项目性能。
  - Unity 官方把 serialization rules、custom serialization、how Unity uses serialization、best practices 作为完整主题组织，而不是把它只写成某个功能模块的附录。
- 暂定判断：
  - 在 Unity 里，序列化不是外围存盘格式，而是连接编辑器状态、资产数据与后续构建链的基础设施。

### 3. Unity 官方把 Addressables / AssetBundles 写成正式的资产交付系统，而不是外部压缩包

- Unity 入口：
  - [Addressables package](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/index.html)
  - [Use AssetBundles to load assets at runtime](https://docs.unity3d.com/Manual/assetbundles-section.html)
- 可直接证明的事实：
  - Unity 官方明确 Addressables 提供 `API and editor interface` 来组织、管理、load 与 release assets。
  - Unity 官方明确 Addressables 构建在 `AssetBundle` API 之上，并自动处理 `asset bundle creation and management`。
  - Unity 官方明确 Addressables 会处理 asset dependencies、asset locations、memory management，并支持 local / CDN 等不同交付位置。
  - Unity 官方明确 AssetBundles 用来把资源分组成 archive file format，可用于 patches 与 DLC，并影响项目 build time 与内容交付方式。
- 暂定判断：
  - Unity 的资源发布链不是“打完包再自己想办法发资源”，而是引擎内建的资源分组、依赖管理、远端定位与运行时加载体系。

### 4. Unity 官方把 BuildPipeline 写成同时覆盖 Player 与 AssetBundle 的统一构建入口

- Unity 入口：
  - [BuildPipeline](https://docs.unity3d.com/ScriptReference/BuildPipeline.html)
  - [Create a custom build script](https://docs.unity3d.com/Manual/build-script-build.html)
- 可直接证明的事实：
  - Unity 官方明确 `BuildPipeline` 是 `building players or AssetBundles` 的 API。
  - Unity 官方明确自定义 build script 可以在 `pre-build / post-build` 步骤里定制构建，并可从 command line 触发。
- Unity 官方示例明确可以先构建 AssetBundles，再构建 Player，并把 AssetBundle type information 传入 Player build，避免 managed code stripping 错删类型。
  - Unity 官方示例还明确可以把 AssetBundle 构建结果注入 `StreamingAssets`，并通过 build profile、PlayerSettings、EditorUserBuildSettings 共同控制最终产物。
- 暂定判断：
  - Unity 的 build 不是外部脚本对产物做最后拷贝，而是引擎内部把场景、类型、AssetBundle 与目标平台配置重新装配成最终可发布 Player 的过程。

### 5. Unreal 官方把 Asset Registry 写成对未加载资产持续建索引的引擎子系统，而不是内容浏览器表层 UI

- Unreal 入口：
  - [Asset Registry in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/asset-registry-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Asset Registry` 是编辑器子系统，会在编辑器加载时异步收集 `unloaded assets` 信息。
  - Unreal 官方明确这些信息会保存在内存里，使编辑器能在 `without loading them` 的情况下创建 asset list。
  - Unreal 官方明确 registry 中的 `FAssetData` 包含 object path、package name、class name、tag/value pairs 等可查询元数据。
  - Unreal 官方明确许多 tag 会在 asset 保存时写入 `uasset header`，Asset Registry 会把它们作为权威、最新的数据读取出来。
- 暂定判断：
  - Unreal 的资产层不是“运行时要用时再读文件”，而是先由引擎维持一张面向包、类、标签与查询的资产索引图。

### 6. Unreal 官方把 Asset Manager 写成贯穿 Editor 与 packaged game 的全局资产管理对象

- Unreal 入口：
  - [Asset Management in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/asset-management-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Asset Manager` 是 `unique, global object`，同时存在于 Editor 与 packaged games。
  - Unreal 官方明确它可以把内容划分为 `chunks`，并提供工具审计 disk / memory usage，以便为 `cooking and chunking` 优化资产组织。
  - Unreal 官方明确它围绕 `Primary Assets` 与 `Secondary Assets` 工作，并通过 `PrimaryAssetId` 管理 discover、load、audit 等行为。
  - Unreal 官方明确 `Asset Bundles` 是与 Primary Asset 关联的命名资产列表，可在保存时由元数据声明，也可在运行时动态注册。
- 暂定判断：
  - Unreal 的资产分发边界不是外围打包脚本附会出来的，而是引擎内建的 Primary Asset、Bundle、Chunk 组织方式。

### 7. Unreal 官方把 Cooking / Packaging 写成第一类 build operations，而不是发布前最后一步

- Unreal 入口：
  - [Packaging Your Project](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project)
- 可直接证明的事实：
  - Unreal 官方明确 packaging 是 `build operation`。
  - Unreal 官方明确 build、cook、stage、package 是 packaging 过程中的核心阶段。
  - Unreal 官方明确 `Cook` 会把 geometry、materials、textures、Blueprints、audio 等 assets 转成目标平台可运行的格式，并执行优化、压缩、剔除未使用数据、处理地图与关卡。
  - Unreal 官方明确 `Package` 会把 compiled code 与 cooked content 组成 distributable files，常见结果包括 `.exe` 与 `.pak` 文件。
- 暂定判断：
  - 在 Unreal 里，Cook / Package 不是“做好游戏以后顺手导出”，而是把编辑器世界转成平台运行世界的正式引擎流程。

### 8. Unreal 官方把 Unreal Build Tool 写成真正的构建系统，而不是 IDE 生成器的附庸

- Unreal 入口：
  - [How to Generate Unreal Engine Project Files for Your IDE](https://dev.epicgames.com/documentation/en-us/unreal-engine/how-to-generate-unreal-engine-project-files-for-your-ide)
  - [Build Configurations Reference for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/build-configurations-reference-for-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `GenerateProjectFiles` 脚本只是 Unreal Build Tool 的一个 wrapper，用来在特定模式下生成项目文件。
  - Unreal 官方明确 UE build system 编译代码并不依赖 IDE project files。
  - Unreal 官方明确 Unreal Build Tool 会依据 `module` 与 `target build files` 来发现源文件与组织编译。
  - Unreal 官方把 build configuration 作为正式文档主题来说明不同编译与分发形态，而不是把 build 只写成外部工具使用技巧。
- 暂定判断：
  - Unreal 的 build 不是 IDE 外挂，而是由引擎自己的 module / target / configuration 规则驱动的产品装配过程。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把资源导入写成 Asset Database 维护的 source file、meta、artifact、dependency 与 reimport 体系，而不是单纯文件拷贝。
- `事实`：Unity 官方把 serialization 写成 Unity 存储和重建数据结构与 GameObject 状态的核心机制，并明确其会影响项目性能。
- `事实`：Unity 官方把 Addressables / AssetBundles 写成正式的资产组织、依赖管理、远端定位与运行时加载体系。
- `事实`：Unity 官方把 `BuildPipeline` 写成同时覆盖 Player build 与 AssetBundle build 的统一 API，并展示了把 AssetBundle 构建结果并入最终 Player 的做法。
- `事实`：Unreal 官方把 Asset Registry 写成可查询未加载资产信息、读取包头标签并保持最新状态的编辑器子系统。
- `事实`：Unreal 官方把 Asset Manager 写成存在于 Editor 与 packaged game 的全局对象，能够围绕 Primary Asset、Asset Bundle 与 Chunk 组织资产。
- `事实`：Unreal 官方把 build、cook、stage、package 写成 packaging 过程中的第一类 build operations，并明确 cook 会把编辑器资源转成平台运行格式。
- `事实`：Unreal 官方明确 Unreal Build Tool 才是底层构建系统，IDE project files 只是包装层之一。
- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `05` 可以把“资产与发布层”定义为那一层负责把编辑器中的内容、元数据、依赖关系与平台目标重新组织成可运行产品的引擎层。
- `判断`：对 Unity 来说，`Asset Database / serialization / Addressables / AssetBundles / BuildPipeline` 已足够支撑“资源导入与发布链是引擎本体”的写法。
- `判断`：对 Unreal 来说，`Asset Registry / Asset Manager / cook / package / Unreal Build Tool` 更直接地展示出资产索引、内容分组、平台转换与最终分发是引擎自己的正式职责。
- `判断`：本篇最安全的比较方式不是比较哪套发布链“更强”，而是说明两台引擎都会把资产组织与最终交付写进自己的核心工程结构里。
- `判断`：文章 `05` 的稳定落点不在“怎么点按钮发包”，而在“为什么没有这层再组织，编辑器里的内容就还不是可分发产品”。

## 本卡暂不支持的强结论

- 不支持：`Unity 的 Addressables / AssetBundles 与 Unreal 的 Asset Manager / Chunk / Pak 已经可以严格一一映射`
- 不支持：`只凭官方文档就下出导入缓存格式、cook 调度、pak 内部布局、依赖裁剪算法的源码级定论`
- 不支持：`哪台引擎的资产与发布层天然更先进、更适合所有项目`
- 不支持：`BuildPipeline` 与 `Unreal Build Tool` 已经可以被简单视为同一种系统实现
- 不支持：把这篇写成 `Addressables / AssetBundles / Cook / Package / Build` 的操作教程、参数百科或产品优劣比较
- 不支持：把 `01` 的内容生产层、`03` 的运行时底座、`06` 的平台抽象或 `07` 的 DOTS / Mass 扩展层顺手混写进本篇

## 下一次最合适的增量

- 基于本卡给 `05` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么这不是外围流程
  7. 常见误解
  8. 我的结论
