# 游戏引擎架构地图 01｜证据卡：为什么游戏引擎首先是一套内容生产工具

## 本卡用途

- 对应文章：`01`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`为什么游戏引擎不只是运行时库，而首先是一套服务内容生产的编辑器与工作流系统。`
- 这篇不展开：`00 总论里的六层总地图细节`
- 这篇不展开：`02 里默认对象世界的完整对照`
- 这篇不展开：`03 里脚本、反射、GC、任务系统的运行时机制`
- 这篇不展开：`05 里资源导入、Cook、Build、Package 的完整发布链`
- 这篇不展开：`04 / 06 / 08` 里专业子系统、平台抽象与总收束判断
- 本篇允许做的事：`只锁定 Unity 的 Editor / Scene View / Inspector / Prefab / Package Manager 与 Unreal 的 Level Editor / Content Browser / Details / Blueprints / Plugins 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把 Scene View 与 Inspector 写成日常编辑场景和对象属性的核心工作界面

- Unity 入口：
  - [Scene view navigation](https://docs.unity3d.com/Manual/SceneViewNavigation.html)
  - [Inspector window reference](https://docs.unity3d.com/Manual/UsingTheInspector.html)
- 可直接证明的事实：
  - Unity 官方明确 `Scene View` 是 `An interactive view into the world you are creating`，用于选择和摆放场景、角色、相机、灯光等 `GameObject`。
  - Unity 官方明确 `Inspector window` 会显示当前选中的 `GameObject / asset / component` 属性，并允许直接查看和编辑这些属性。
  - Unity 官方把 `Scene View`、`Inspector`、`Hierarchy`、`Project` 组织在同一套 `Unity Editor interface` 之下，而不是把它们描述成外围附属工具。
- 暂定判断：
  - Unity 的官方入口首先强调的是“在编辑器里组织、选择、检查、修改内容”，而不是“只把运行时代码跑起来”。

### 2. Unity Prefab 是可复用资产模板，Prefab Mode 允许集中编辑并把改动传播到实例

- Unity 入口：
  - [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html)
  - [Editing a Prefab in Prefab Mode](https://docs.unity3d.com/es/2020.1/Manual/EditingInPrefabMode.html)
- 可直接证明的事实：
  - Unity 官方明确 `Prefab` 是把 `GameObject` 连同其 `components`、属性值、子对象一起保存成 `reusable asset` 的机制。
  - Unity 官方明确 prefab asset 会充当模板，用于在 Scene 中创建新的 prefab instances。
  - Unity 官方明确 `Prefab Mode` 允许把 Prefab 作为资产单独打开和编辑，并说明在该模式中的修改会影响该 Prefab 的所有实例。
  - Unity 官方明确进入 `Prefab Mode` 后，`Scene View` 与 `Hierarchy` 会围绕当前 Prefab 收束，允许在隔离或上下文模式下专注编辑该资产。
- 暂定判断：
  - `Prefab` 不是简单“复制对象更方便”的小功能，而是内容生产中组织可复用对象、集中维护改动、控制协作边界的核心资产单位。

### 3. Unity Package Manager 把功能、资产和模板作为项目级可管理单元，而不只是运行时链接结果

- Unity 入口：
  - [Get started with packages](https://docs.unity3d.com/Manual/Packages.html)
  - [The Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html)
- 可直接证明的事实：
  - Unity 官方明确 `package` 是可承载 `Editor tools and libraries`、`Runtime tools and libraries`、`Asset collections`、`Project templates` 的容器。
  - Unity 官方明确 `Package Manager window` 可以查看、安装、更新、移除 packages 和 feature sets，也能处理 asset packages。
  - Unity 官方明确每个 project 都通过 `manifest` 和依赖关系决定要加载哪些 packages。
  - Unity 官方明确内建功能也可以作为 built-in packages 在 Package Manager 中启停，而这会影响最终运行时代码与资源是否进入构建产物。
- 暂定判断：
  - Unity 的功能分发和扩展边界并不是“编译后自然存在的运行时库”，而是被编辑器和项目清单显式管理的生产组织单元。

### 4. Unreal 官方把 Level Editor、Details 面板和 Content Drawer / Browser 写成内容制作主界面

- Unreal 入口：
  - [Unreal Editor Interface](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-editor-interface?application_version=5.6)
  - [Content Browser Interface in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/content-browser-interface-in-unreal-engine?application_version=5.6)
- 可直接证明的事实：
  - Unreal 官方明确项目打开后默认进入 `Level Editor`，它提供核心创建功能，并且是你 `spend most of your time developing content for your project` 的地方。
  - Unreal 官方明确 `Details panel` 会在你选中 Actor 时显示其 `Transform`、`Static Mesh`、`Material`、`physics settings` 等属性。
  - Unreal 官方明确 `Content Drawer` / `Content Browser` 允许访问项目内全部资产。
  - Unreal 官方明确 `Content Browser` 是在 Unreal Editor 中 `creating, importing, organizing, viewing, and managing content Assets` 的 primary area。
- 暂定判断：
  - Unreal 官方给出的第一层工作入口同样是“做内容、管资产、改对象属性”的编辑器工作区，而不是把引擎只定义成运行时执行器。

### 5. Unreal Blueprints 既是编辑器内的完整脚本系统，也是可组装对象与行为的内容生产单位

- Unreal 入口：
  - [Blueprints Visual Scripting in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/blueprints-visual-scripting-in-unreal-engine?application_version=5.7)
  - [Blueprints Technical Guide](https://dev.epicgames.com/documentation/en-us/unreal-engine/technical-guide-for-blueprints-visual-scripting-in-unreal-engine?application_version=5.6)
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
- 可直接证明的事实：
  - Unreal 官方明确 `Blueprint Visual Scripting` 是 `complete gameplay scripting system`，并且是在 `Unreal Editor` 中用节点式界面创建 gameplay elements。
  - Unreal 官方明确 Blueprints 用来定义面向对象的 classes 或 objects，且 designers 可以借此使用许多原本偏程序员的能力。
  - Unreal 官方明确创建 Blueprint 时可以扩展 C++ class 或另一个 Blueprint class，并可添加、排列、自定义 Components，定义 Variables，响应 Events 和 Input，构造自定义对象类型。
  - Unreal 官方明确 `Blueprint can be thought of as a very powerful prefab system`。
- 暂定判断：
  - Blueprint 不只是“可视化脚本语法糖”，而是把对象装配、行为拼接、设计师协作和编辑器内生产流程绑在一起的核心生产单位。

### 6. Unreal Plugins 由编辑器窗口按项目启停，既能加新功能，也能改编辑器工作界面

- Unreal 入口：
  - [Working with Plugins in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/working-with-plugins-in-unreal-engine?application_version=5.6)
  - [Plugin Browser API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/PluginIndex/PluginBrowser)
- 可直接证明的事实：
  - Unreal 官方明确 plugin 是 `optional software component`，可以在不直接修改 Unreal Engine 源码的前提下增加特定功能。
  - Unreal 官方明确 plugins 可以增加新的 editor menu items、toolbar commands、editor sub-modes，甚至增加全新功能。
  - Unreal 官方明确 plugins 可以按 project 独立启用或禁用。
  - Unreal 官方明确 `Plugin Browser` 是 `managing installed plugins and creating new plugins` 的用户界面。
- 暂定判断：
  - Unreal 的扩展边界同样是生产体系边界，因为插件不仅决定运行时能力，也决定编辑器里有哪些创作入口、工具面板和工作模式。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把 `Scene View`、`Inspector`、`Prefab`、`Package Manager` 都写成 Unity Editor 内部的基础工作界面与生产单位。
- `事实`：Unity 官方明确 `Prefab` 是可复用资产模板，并允许在 `Prefab Mode` 中集中编辑且把改动传播到实例。
- `事实`：Unity 官方明确 `package` 可以同时承载 editor tools、runtime tools、assets、templates，并通过 Package Manager 与 manifest 在 project 级管理。
- `事实`：Unreal 官方明确 `Level Editor` 是内容开发的核心界面，`Content Browser` 是创建、导入、组织、查看、管理资产的 primary area，`Details panel` 负责编辑被选中 Actor 的属性。
- `事实`：Unreal 官方明确 `Blueprints` 是在 Unreal Editor 内创建 gameplay elements 和对象类型的完整脚本系统，并把 Blueprint 描述成一种强力 prefab system。
- `事实`：Unreal 官方明确 plugins 可以增加 editor menu、toolbar、sub-mode 等能力，并且通过 Plugins / Plugin Browser 在 project 级启停和管理。
- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `01` 可以把“内容生产层”定义为引擎中负责编辑、组织、复用、装配和扩展内容的那一层，而不把引擎压扁成纯运行时库。
- `判断`：Unity 的 `Scene View / Inspector / Prefab / Package Manager` 与 Unreal 的 `Level Editor / Content Browser / Details / Blueprints / Plugins` 都足以支持“现代引擎首先是一套内容生产工具链”的写法。
- `判断`：`Prefab / Blueprint` 最稳的工程定位不是“方便做原型的小功能”，而是团队组织对象、沉淀可复用资产和控制修改传播范围的生产边界。
- `判断`：`Package / Plugin` 最稳的定位也不只是技术分发机制，而是引擎能力怎样进入项目、进入编辑器、进入团队工作流的扩展边界。
- `判断`：本篇最安全的比较方式不是裁判 `Unity` 或 `Unreal` 谁的工具链更强，而是说明两者都把“内容生产”做成了引擎本体的一部分。

## 本卡暂不支持的强结论

- 不支持：`运行时在现代引擎里已经不重要`
- 不支持：`Prefab` 与 `Blueprint` 可以做严格一一语义映射
- 不支持：`Unity` 或 `Unreal` 的内容生产体系天然更先进或更适合所有团队
- 不支持：`编辑器工作流` 已经足以单独推出全部运行时底层架构结论
- 不支持：把 `Prefab / Blueprint / Package / Plugin` 顺手扩写成完整教程、功能百科或产品优劣比较
- 不支持：把 `资源导入 / Cook / Build / Package` 的完整交付链与本篇“内容生产层”直接混写为同一个问题

## 下一次最合适的增量

- 基于本卡给 `01` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是表面工具差异
  7. 常见误解
  8. 我的结论
