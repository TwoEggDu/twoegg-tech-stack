# 游戏引擎架构地图 04｜证据卡：渲染、物理、动画、音频、UI，为什么都像半台小引擎

## 本卡用途

- 对应文章：`04`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`为什么渲染、物理、动画、音频、UI 不适合被当成平铺功能点，而应该被看成各自带着资源格式、编辑器工具、运行时管线、调试链与扩展边界的专业子系统层。`
- 这篇不展开：`00 总论里的整张六层地图，只借它做定位，不重写整篇总论`
- 这篇不展开：`02 里 Scene / World、GameObject / Actor、Gameplay Framework 的默认对象世界细节`
- 这篇不展开：`03 里脚本、反射、GC、任务系统、PlayerLoop / Task Graph 的运行时底座机制`
- 这篇不展开：`05 里资源导入、Cook、Build、Package 的完整交付链`
- 这篇不展开：`06 里平台抽象、RHI、跨平台目标与硬件差异`
- 这篇不展开：`07 里 DOTS / Mass 这类数据导向扩展层`
- 这篇不展开：`08 里 Unity / Unreal 的总体气质收束`
- 本篇允许做的事：`只锁定 Unity 的 render pipeline / physics / animation / audio / UI Toolkit，与 Unreal 的 Lumen / Nanite / Chaos / Animation Blueprint / MetaSounds / UMG / Slate 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把渲染写成一套可替换、可定制、按帧重复执行的 pipeline，而不是一个单点 API

- Unity 入口：
  - [Introduction to render pipelines](https://docs.unity3d.com/Manual/render-pipelines-overview.html)
- 可直接证明的事实：
  - Unity 官方明确 `render pipeline` 是一系列把 `Scene` 内容显示到屏幕上的操作。
  - Unity 官方明确渲染流水线至少包含 `culling`、`rendering`、`post-processing` 三段，并且会在每一帧重复执行。
  - Unity 官方明确 Unity 提供三套 `prebuilt render pipelines`，它们具有不同的能力与性能特征。
  - Unity 官方明确你可以使用 `URP`、`HDRP`，也可以自己创建 custom render pipeline，并且 `Scriptable Render Pipelines` 允许直接在 C# 中改写 culling、rendering 与 post-processing。
- 暂定判断：
  - Unity 的渲染不是“最后把东西画出来”的薄封装，而是一套拥有项目级选择、流水线阶段、可定制执行骨架与专门术语边界的专业子系统。

### 2. Unity 官方把物理写成多套 integration 选择，而不是一个简单的 Rigidbody 开关

- Unity 入口：
  - [Physics](https://docs.unity3d.com/Manual/PhysicsSection.html)
- 可直接证明的事实：
  - Unity 官方明确物理部分负责碰撞、重力与各种力的模拟。
  - Unity 官方明确 Unity 提供不同的 `physics engine integrations`，并且可以按项目需要在 `3D / 2D / object-oriented / data-oriented` 之间选择。
  - Unity 官方明确内建 3D physics 是 `Nvidia PhysX engine` 的集成。
  - Unity 官方明确物理集成甚至可以被禁用和裁剪，以影响项目构建边界。
- 暂定判断：
  - Unity 的物理不是挂在对象上的几项属性，而是一套独立集成、独立能力边界、独立项目配置选择的模拟子系统。

### 3. Unity 官方把动画写成带导入器、编辑器、状态机、重定向能力的完整系统

- Unity 入口：
  - [Animation](https://docs.unity3d.com/Manual/AnimationSection.html)
- 可直接证明的事实：
  - Unity 官方明确 animation system 提供的是让模型与资产属性动起来的 `tools and processes`，而不是单一播放接口。
  - Unity 官方明确常见动画工具包含 `importers`、用于创建和修改动画的 `editors`、以及决定何时播放什么动画的 `real-time animation state machines`。
  - Unity 官方明确某些动画系统还包含 humanoid 定义与 retargeting 工具。
  - Unity 官方明确 `Mecanim` 通过 `Animator component`、`Animation window`、`Animator window` 组成推荐动画系统，并强调它适合复杂角色动画、曲线与 blending。
- 暂定判断：
  - Unity 的动画是“导入 - 编辑 - 状态切换 - 运行时求值”一整套体系，不是一个附属播放器。

### 4. Unity 官方把音频写成带 mixer、snapshot、effect、profiler、plug-in SDK 的独立系统

- Unity 入口：
  - [Audio](https://docs.unity3d.com/Manual/Audio.html)
- 可直接证明的事实：
  - Unity 官方明确音频功能包含 `full 3D spatial sound`、`real-time mixing and mastering`、`hierarchies of mixers`、`snapshots` 与 `predefined effects`。
  - Unity 官方把 `Audio mixer`、`Scriptable Audio Pipeline`、`Native audio plug-in SDK`、`Audio Profiler module` 列成独立文档入口。
  - Unity 官方明确音频部分不仅覆盖 clips、sources、listeners 与导入，还覆盖 mixer UI、profiling 与 plug-in 接口。
- 暂定判断：
  - Unity 的音频不是“播 wav 文件”的小模块，而是一套有运行时混音链、编辑器界面、性能分析与扩展接口的音频子系统。

### 5. Unity 官方把 UI Toolkit 写成一套同时覆盖编辑器 UI 与运行时 UI 的 UI stack

- Unity 入口：
  - [UI Toolkit](https://docs.unity3d.com/Manual/UIElements.html)
- 可直接证明的事实：
  - Unity 官方明确 `UI Toolkit` 是一组用于开发 UI 的 `features, resources, and tools`。
  - Unity 官方明确它提供 `UI Builder` 这种 visual authoring tool，用来创建和编辑 `UXML / USS` 资产。
  - Unity 官方明确它包含 `event` 系统，并明确点出 dispatcher、handler、synthesizer 与 event type library。
  - Unity 官方明确它带有 `UI Renderer`，而且这个 renderer 是直接构建在 Unity graphics device layer 之上的。
  - Unity 官方明确它同时提供 `Support for Editor UI` 与 `Support for runtime UI`，并把 data binding、test UI、migration guides 也纳入体系。
- 暂定判断：
  - Unity 的 UI 不只是“在屏幕上盖一层控件”，而是一套有资源格式、可视化编辑器、事件系统、渲染器、编辑器端与运行时双落地的完整子系统。

### 6. Unreal 官方把渲染写成由 Lumen / Nanite 这类专门系统组成的渲染平台，而不是一串 draw call

- Unreal 入口：
  - [Lumen Technical Details in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-technical-details-in-unreal-engine)
  - [Nanite Virtualized Geometry in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/nanite-virtualized-geometry-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Lumen` 使用多种 ray tracing 方法来求解 global illumination 与 reflections，并区分 `Screen Traces`、`Hardware Ray Tracing`、`Software Ray Tracing` 等路径。
  - Unreal 官方明确 `Lumen Scene` 围绕相机运行，并带有 `Surface Cache`、view distance、far field、visualization modes、quality / performance settings 与 profiling 关注点。
  - Unreal 官方明确 `Nanite` 是新的 `virtualized geometry system`，采用新的内部 mesh format 与 rendering technology。
  - Unreal 官方明确 Nanite 在导入阶段会把 mesh 分解成 hierarchical clusters，在渲染阶段按视角动态切换细节并按需 streaming。
  - Unreal 官方明确 Nanite `runs in its own rendering pass`，而且 `completely bypasses traditional draw calls`，并提供 visualization modes 与 fallback mesh 机制。
- 暂定判断：
  - Unreal 的渲染不是一个单薄“渲染模块”，而是由带自己数据格式、缓存、streaming、可视化、性能档位与独立 pass 的专业渲染系统群组成。

### 7. Unreal 官方把 Chaos 写成一整族物理系统，而不是单一碰撞求解器

- Unreal 入口：
  - [Physics in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/physics-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Chaos Physics` 是 Unreal 中的轻量物理解算方案，而且是 `built from scratch` 来满足新一代游戏需求。
  - Unreal 官方明确这套系统不仅包括 rigid body，还包括 `Destruction`、`Networked Physics`、`Visual Debugger`、`Cloth`、`Ragdoll`、`Vehicles`、`Physics Fields`、`Fluid Simulation`、`Hair Physics`、`Flesh` 等。
  - Unreal 官方明确 `Chaos Destruction` 使用 `Geometry Collections` 这种资产类型，带 fracture workflow、cache/replay 系统，并与 `Niagara` 和 `Physics Fields` 深度集成。
  - Unreal 官方明确车辆、布料、字段等能力都各自带着运行时与编辑器工作流。
- 暂定判断：
  - Unreal 的物理明显不是“Actor 打开模拟”那么简单，而是一套贯穿资产、工具、运行时、缓存、调试与其他系统集成的物理平台。

### 8. Unreal 官方把动画写成专门的 Animation Blueprint 编辑器、图系统与逐帧 pose 求值流程

- Unreal 入口：
  - [Animation Blueprint Editor in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprint-editor-in-unreal-engine)
  - [Animation Blueprint Nodes in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprint-nodes-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Animation Blueprint` 是专门控制对象动画行为的 Blueprint 类型。
  - Unreal 官方明确 Animation Blueprint Editor 自带 `Viewport`、`My Blueprint`、`Graph`、`Details`、`Anim Preview Editor`、toolbar 与 debug object 等专门界面。
  - Unreal 官方明确 Animation Blueprint 里至少有 `Event Graph`、`Anim Graph`、`State Machines` 三类图，而且 `Anim Graph` 负责求值当前帧的 final pose。
  - Unreal 官方明确这套系统支持预览、编译、错误定位、线程更新警告与运行时调试。
- 暂定判断：
  - Unreal 的动画不是“播骨骼动画文件”的单点功能，而是一套专门的图编辑器、状态组织方式、预览调试工具与逐帧 pose 生成系统。

### 9. Unreal 官方把 MetaSounds 写成每个图都像一个独立 audio rendering engine 的系统

- Unreal 入口：
  - [MetaSounds: The Next Generation Sound Sources in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/metasounds-the-next-generation-sound-sources-in-unreal-engine?application_version=5.6)
  - [Audio in Unreal Engine 5](https://dev.epicgames.com/documentation/en-us/unreal-engine/audio-in-unreal-engine-5?application_version=5.6)
- 可直接证明的事实：
  - Unreal 官方明确 `MetaSound` 是高性能音频系统，允许音频设计师直接控制 `DSP graph`。
  - Unreal 官方明确 MetaSounds 支持 `sample-accurate timing` 与 audio-buffer-level control，并能在运行时合成程序化声音。
  - Unreal 官方明确每个 MetaSound 都可以看作它自己的 `audio rendering engine`，彼此并行渲染，甚至可能拥有独立的 rendering format。
  - Unreal 官方明确 MetaSounds 使用新的 `MetaSound Editor`，提供 node-based interface、live preview、real-time meter、参数可视化与 extensible C++ node API。
- 暂定判断：
  - “音频像半台小引擎”在 Unreal 上最容易被官方文档直接支撑，因为 MetaSounds 本身就被写成带独立图、独立编辑器、独立运行时与独立扩展 API 的音频执行系统。

### 10. Unreal 官方把 UI 写成 UMG + Slate 组合，而不是 HUD API 的薄壳

- Unreal 入口：
  - [Widget Blueprints in UMG for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/widget-blueprints-in-umg-for-unreal-engine)
  - [Creating User Interfaces With UMG and Slate in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/creating-user-interfaces-with-umg-and-slate-in-unreal-engine?application_version=5.6)
  - [Slate Overview for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/slate-overview-for-unreal-engine)
  - [Using the Slate Widget Reflector in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-the-slate-widget-reflector-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方明确 `Widget Blueprint Editor` 默认带 `Designer` 与 `Graph` 两种模式，并提供 `Palette`、`Hierarchy`、`Visual Designer`、`Details`、`Animations` 轨道等专门 UI 编辑界面。
  - Unreal 官方明确 `Creating User Interfaces` 文档把 `UMG Editor Reference`、`Slate UI Framework Reference`、`Testing and Debugging` 组织为一整套 UI 工具链。
  - Unreal 官方明确 `Slate` 是完全自定义、与平台无关的 UI framework，用来构建像 `Unreal Editor` 这样的工具界面和 in-game UI。
  - Unreal 官方明确 Slate 提供声明式语法、布局/样式系统、输入系统、target-agnostic rendering primitives、docking framework，以及 `Widget Reflector` 调试工具。
  - Unreal 官方明确 Unreal Editor 的 UI 本身就是用 Slate 构建的。
- 暂定判断：
  - Unreal 的 UI 不是“屏幕控件接口”而已，而是一套从底层 UI framework 到可视化 Widget 编辑器，再到调试工具的完整 UI 子系统。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把渲染写成带 `culling / rendering / post-processing` 阶段、可选 `URP / HDRP / Built-In / custom SRP` 的 pipeline 体系。
- `事实`：Unity 官方把物理写成多种 physics integration 的项目级选择，其中包括 `PhysX`、2D、object-oriented 与 data-oriented 路线。
- `事实`：Unity 官方把动画写成带 importers、editors、state machines、retargeting、Animator/Animation 窗口的系统。
- `事实`：Unity 官方把音频写成包含 3D spatial sound、mixer hierarchy、snapshots、effects、audio profiler 与 plug-in SDK 的体系。
- `事实`：Unity 官方把 UI Toolkit 写成覆盖 visual authoring、UXML/USS、event system、UI renderer、Editor UI 与 runtime UI 的 UI stack。
- `事实`：Unreal 官方把 `Lumen`、`Nanite` 写成拥有独立数据格式、缓存、streaming、visualization、profiling 与渲染 pass 的专门渲染系统。
- `事实`：Unreal 官方把 `Chaos` 写成包括 destruction、cloth、vehicles、fields、debugger、Niagara integration 等在内的物理系统家族。
- `事实`：Unreal 官方把 `Animation Blueprint` 写成带专门编辑器、图类型、预览与调试能力的动画系统。
- `事实`：Unreal 官方把 `MetaSounds` 写成可并行执行、带 DSP graph、sample-accurate control、MetaSound Editor 与 C++ 扩展 API 的音频系统。
- `事实`：Unreal 官方把 `UMG + Slate` 写成从 Widget Blueprint 编辑器到底层跨平台 UI framework、再到 Widget Reflector 调试工具的完整 UI 栈。
- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `04` 可以把“专业子系统层”定义为那些各自拥有专门资源类型、作者工具、运行时求值/调度、可视化与调试链路的引擎内子系统，而不是平铺的功能点列表。
- `判断`：`渲染 / 物理 / 动画 / 音频 / UI` 之所以“像半台小引擎”，最稳妥的含义不是它们能脱离引擎主体独立存在，而是它们都各自带着一套接近子平台级别的数据、工具、运行时与诊断边界。
- `判断`：对 Unity 来说，`render pipeline / physics integrations / Mecanim / audio stack / UI Toolkit` 已足够支撑“这些不是平铺模块”的写法。
- `判断`：对 Unreal 来说，`Lumen / Nanite / Chaos / Animation Blueprint / MetaSounds / UMG / Slate` 更直接地展示出“子系统内还有资产、编辑器、图、调试器、profiling、平台条件”的结构。
- `判断`：本篇最安全的比较方式不是去比哪个子系统“更强”，而是说明两台引擎都会把这些领域做成半自治的专业子系统，再挂回共同的对象世界、运行时底座和发布链。

## 本卡暂不支持的强结论

- 不支持：`五类子系统在 Unity 与 Unreal 中已经可以做严格一一映射`
- 不支持：`URP / HDRP` 与 `Lumen / Nanite`、`Mecanim` 与 `Animation Blueprint`、`UI Toolkit` 与 `UMG / Slate` 已经是完全同构的概念
- 不支持：`每一类子系统在两台引擎里的边界深度都完全相同`
- 不支持：`只凭官方文档就下内部调度、内存布局、线程模型、缓存实现的源码级定论`
- 不支持：`哪台引擎的专业子系统天然更先进、更完整或更适合所有项目`
- 不支持：把这篇写成 `URP / HDRP / Chaos / MetaSounds / UI Toolkit / UMG` 的教程、功能百科或产品优劣比较
- 不支持：把 `05` 的资源与发布链、`06` 的平台抽象、`07` 的 DOTS / Mass 扩展层，顺手混写进本篇

## 下一次最合适的增量

- 基于本卡给 `04` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是平铺功能列表
  7. 常见误解
  8. 我的结论
