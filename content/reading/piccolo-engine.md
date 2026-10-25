---
title: "Piccolo"
description: "从架构地图与前置知识进入启动、对象恢复、相机、渲染提交和编辑器保存重载，沿实际代码学习读懂并调试一台具体引擎。"
url: "/reading/engine/piccolo/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "piccolo"
series_id: "piccolo-engine"
series_role: "index"
series_title: "Piccolo 源码阅读与调试"
reading_status: "27 篇正文已写，原定主线齐备；完整构建和运行待验证"
---

> **正文进度与验证状态**
>
> 主线 00—18、五篇前置与渲染补充、三篇 C++ 专项共 27 篇正文已写，覆盖架构、构建基线、启动、对象恢复、输入运动、相机、GPU 资源、表面着色、后处理输出与编辑器操作/保存重载。01 已实际完成 Windows 配置、反射/序列化和 shader 生成；09 含原输入/运动/控制器函数及数学实现的独立条件检查；10 含原形状转换/步进/更新函数的记录适配与几何检查；12 含原骨骼/数学的独立资源桥接与 Shader 编译；13 含原类型布局/字节、独立池状态检查及五份 Shader 编译，实际 GPU 粒子待验证；14 含随附 Lua/sol2 与原桥接/反射的独立桩宿主检查，实际引擎脚本与角色运行待验证；GFX-02 核对程序产物与系统边界；11/15 含独立数学与实际资源检查；16/17 含相关 Shader 编译和条件数值检查；18 含拾取 Shader 编译及原 ID/删除/坐标函数的条件检查。完整 Debug 编辑器编译仍被第三方兼容问题阻塞。实际启动、输入运动、相机跟随、GPU 上传/着色/后处理、拾取/创建/删除、保存/重载、断点与图形捕获尚未完成，各篇分别说明源码推导和运行证据范围。
>
> 原定 00—18 主线正文现已齐备；文章成稿不等于完整运行验收。前置和对照链接继续指向已有公共专题或其他引擎文章。

**官方材料**

- [Piccolo 官方仓库与构建说明](https://github.com/BoomingTech/Piccolo)
- [Piccolo 官方 Wiki](https://github.com/BoomingTech/Piccolo/wiki)

建议先用架构地图认识目标；缺少 C++ 基础时，先走下面的公共入口，再读 Piccolo 专项前置和渲染基础。随后读 01 构建与调试基线，沿 02—08 进入启动、逐帧执行、对象恢复、渲染与编辑器。01 记录当前工具环境中的真实构建边界，实际调试前仍需解决完整编译阻塞并核对程序、配置和符号。源码基线为 `f5053707fed4`，每篇分别说明核对与实践范围。

公共架构阅读：[00A：引擎怎样分层——职责、依赖与资源服务的边界]({{< relref "system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md" >}})。先掌握职责、接口依赖与资源状态的区分，再回到Piccolo检查实际目录与调用；该公共补篇不计入本系列27篇。

## 公共基础之后，补 Piccolo 的约定

公共入口：[C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。先理解值与借用、所有权、虚调用，再按当前系统补容器、模板/宏、回调和编译链接；其中缺课状态见公共目录。

现有三篇 Piccolo C++ 文章继续承担专项案例：谁持有关卡与组件、Component/RHI 怎样分派、ReflectionPtr 与生成器怎样接线。已经读过通用规则的读者可直接追这些项目约定，不必再次顺读所有语言解释。

**容器专项已写**：[对象容器修改后，持有者、借用与遍历怎样变化]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}})，从编辑器删除路径解释对象表成员资格、局部强持有、组件列表复制与迭代器失效，含独立 C++17 练习。公共容器课尚待成稿，本篇先补必要规则并核对 Piccolo 的约定。

**回调专项已写**：[窗口与编辑器回调怎样绑定、调用和注销]({{< relref "engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md" >}})，沿 GLFW、ImGui 和输入管理器核对绑定参数、实际分发、Lambda 捕获与退出清理，明确固定版本尚无按订阅移除接口。

**缓冲与线程专项已写**：[状态缓冲怎样交接，哪些地方真的跨线程]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}})，区分值复制、槽位角色、消费与复用，沿 Jolt 和异步日志找到实际 CPU 线程，再把 GPU 提交与完成单独核对。三项专项正文已齐备，公共课程缺口与实际引擎运行状态分别维护。

<a id="chapters"></a>

## 工程、构建与启动

理解怎样建立工程基线，引擎从哪里启动，以及各个系统怎样完成初始化与退出。

**已有专篇**

- [Piccolo 源码阅读 00｜架构地图：编辑器、世界、资源与渲染怎样协作]({{< relref "system-design/piccolo-engine-00-architecture-map.md" >}})：先分清职责、构建和数据流三种关系，再选择启动与一帧调试入口。源码已核对，实践待验证。
- [建立构建与调试基线：源码、生成文件、配置与程序怎样对应]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}})：实际核对 Windows 工具链、配置和生成目标，记录完整 Debug 编译阻塞，并说明 exe、ini、资源与符号怎样对应；启动与断点待验证。
- [从启动走到第一帧：初始化、世界加载与退出怎样衔接]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}})：沿真实编辑器入口读配置、系统启动、首次 tick 的世界加载与正常退出；源码已核对，运行待验证。

**已有前置阅读**

- [读懂引擎 C++：智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}})：沿 Piccolo 系统、关卡、对象和组件区分强持有、弱观察、显式清理与实际析构，含独立小程序。
- [读懂引擎 C++：类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：沿组件与 RHI 找到虚调用的实际实现，理解 override、切片与虚析构，含独立小程序。
- [读懂引擎 C++：模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：从组件查询读到类型参数、宏展开与生成文件，区分构建阶段和运行时注册。
- [从 C# 到 C++：所有权与编译链接]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})：普通 C++ 练习，补充生命周期、RAII、编译与链接基础。
- [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})：补充构建配置、符号、断点与调试原理。

当前已取得配置与代码生成实测，完整编辑器编译、运行和断点验证仍需继续；具体工具版本与失败入口见 01。

## 一帧执行与系统协作

理解一帧工作怎样推进，以及输入、逻辑、场景更新和渲染之间怎样交换数据。这条完整链路将作为后续研究各个系统的主入口。

**已有专篇**

- [一帧怎样推进：调用顺序、状态交接与线程怎样核对]({{< relref "engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md" >}})：沿编辑器循环核对输入与世界更新、两槽位交接、物理任务和 GPU 工作；含条件推导与后续观察表，运行待验证。

**已有前置阅读**

- [状态缓冲怎样交接，哪些地方真的跨线程]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}})：用发布、消费与复用协议读状态缓冲，区分主调用链、物理/日志 worker 和 GPU 工作。
- [引擎运行时底座]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}})：理解执行模型、任务、对象元信息等机制在引擎中的位置。
- [CPU 与内存体系]({{< relref "performance/hardware-cpu-index.md" >}})：研究数据布局、缓存和并行成本时查阅。

## 场景、对象与组件

理解世界中的对象怎样被描述、创建、更新和销毁，以及组件怎样参与运行。

**已有专篇**

- [场景文件怎样成为世界中的对象与组件]({{< relref "engine-toolchain/piccolo-engine-04-scene-to-objects-components.md" >}})：沿默认关卡的 Wall_1，追踪实例与定义合并、运行身份、组件 postLoad 接线及对象持有；源码与资源已核对，运行待验证。
- [改一个位置，怎样沿逻辑数据追到渲染表示]({{< relref "engine-toolchain/piccolo-engine-06-position-to-render-representation.md" >}})：固定 Wall_1 的位置改动，逐值核对 Transform 双缓冲、描述交接、实例身份与 RenderEntity 更新，含较晚写入的时序反例；运行待验证。

**已有前置阅读**

- [对象容器修改后，持有者、借用与遍历怎样变化]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}})：区分 map 擦除、对象保活与组件包装复制；源码与独立程序的验证范围见正文。
- [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：先从 Component 接口追到具体组件的更新与析构。
- [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：拆开组件查询宏、模板返回类型和名字匹配协议。

**已有对照阅读**

- [GameObject 与 Actor：两种世界组织方式]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})：提供比较对象模型的观察角度，正文讨论的是 Unity／Unreal。

## 资源、反射与序列化

研究资源文件怎样描述对象、类型信息怎样参与读写，以及数据怎样恢复成可运行的场景。

**已有专篇**

- [资源、类型信息与序列化怎样共同恢复对象]({{< relref "engine-toolchain/piccolo-engine-05-resource-type-serialization.md" >}})：沿 Wall_1 的实例 Transform，连起已知类型字段读取、运行时类注册与具体组件构造，并区分写出、兼容和业务接线边界；源码与模板已核对，实际生成与运行待验证。
- [14：Lua 脚本怎样读写组件并调用引擎方法]({{< relref "engine-toolchain/piccolo-engine-14-lua-component-reflection.md" >}})：沿默认 Player 文本、每组件执行状态和三个绑定追字段/方法，核对类型、弱引用和业务消费；独立桩宿主通过，完整引擎运行待验证。

**已有前置与对照阅读**

- [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：理解字段标记、生成访问器、Serializer 特化与运行时注册，含独立小程序。
- [资产与发布在引擎中的位置]({{< relref "system-design/game-engine-architecture-05-asset-and-delivery.md" >}})：理解导入、资产、构建与交付之间的关系。
- [Unity 资产系统与序列化]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})：作为另一种实现的对照阅读。

## 渲染链路与图形接口

研究场景数据怎样进入渲染系统，经过资源准备、渲染阶段和图形接口提交形成画面。

**已有专篇**

- [从渲染场景进入 Pass 与 Vulkan 提交]({{< relref "engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md" >}})：沿 Wall_1 的可见节点、材质/网格分组和实例矩阵，核对 GBuffer 绘制、命令记录、提交同步与呈现目标；源码与接口语义已核对，运行及图形捕获待验证。
- [11：相机怎样跟随角色，并把视角交给渲染系统]({{< relref "engine-toolchain/piccolo-engine-11-camera-follow-render-view.md" >}})：从第三人称视点与目标追到相机交接、视图来源、FOV 更新、主相机筛选与顶点输入；含独立数值检查，实际跟随与断点待验证。
- [15：网格与纹理怎样从文件成为 GPU 资源]({{< relref "engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md" >}})：沿 Wall 与 gold 材质追 CPU 载荷、缓存身份、暂存上传、Image/mip 和绘制绑定；含实际资源的独立 CPU 检查，GPU 上传与采样待验证。
- [16：材质、光照与阴影怎样共同决定表面颜色]({{< relref "engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md" >}})：沿 G-buffer、BRDF、方向光阴影与环境贡献追到场景颜色，核对实际字段消费；Shader 编译和条件数值检查通过，GPU 画面待验证。
- [17：场景颜色怎样经过后处理成为最终画面]({{< relref "engine-toolchain/piccolo-engine-17-post-processing-final-frame.md" >}})：追 tone mapping、LUT 透传、FXAA 分支、UI 选色与交换链输出，核对附件复用和颜色约定；Shader 编译及条件检查通过，GPU/UI 与显示待验证。
- [12：一段动画怎样成为骨骼姿态与网格蒙皮]({{< relref "engine-toolchain/piccolo-engine-12-animation-skeleton-mesh-skinning.md" >}})：追采样、层级姿态、逆绑定修正和关节矩阵到顶点；核对 clip0/dirty/编号约定，原骨骼数值与 Shader 编译通过，实际动画/GPU 待验证。
- [13：粒子发射器怎样进入 GPU 模拟与绘制]({{< relref "engine-toolchain/piccolo-engine-13-particle-emitter-gpu-simulation.md" >}})：追发射请求、槽位复用、双存活表、三个计算阶段和后续 billboard 绘制；布局/状态与 Shader 编译通过，GPU 运行待验证。

**已有前置专篇**

- [读懂渲染管线：从场景对象到一帧画面]({{< relref "rendering/piccolo-rendering-01-scene-to-frame.md" >}})：结合 Piccolo 的渲染表示、可见性、Pass、Draw Call 与 Vulkan 呈现，建立完整链路；区分源码推导与运行证据。
- [从 GLSL 到 GPU：用 Piccolo 串起渲染引擎、API 与系统]({{< relref "rendering/piccolo-rendering-02-shader-api-driver-gpu.md" >}})：追踪编译产物、pipeline、资源绑定与矩阵布局，接到 Loader、驱动、OS 和呈现；含两个 Shader 的实际编译与字节核对，GPU 运行待验证。

**已有前置阅读**

- [游戏图形系统]({{< relref "rendering/game-graphics-stack-series-index.md" >}})：建立引擎、图形 API、驱动与 GPU 的整体链路。
- [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：从 RHI 接口追到 VulkanRHI，区分后端分派与图形命令提交。
- [图形 API 基础]({{< relref "rendering/graphics-api-series-index.md" >}})：理解命令、资源与平台后端的抽象。
- [图形数学]({{< relref "rendering/graphics-math-series-index.md" >}})：补充空间变换、可见性与几何判断。

渲染主线围绕 **引擎无关的原理与 Piccolo 的具体实现** 组织。可以先读 GFX-01 建立完整链路，再用 GFX-02 分清语言、API 和系统边界，沿 06—07 追踪具体实例怎样进入 GBuffer 绘制与提交。实际断点和图形捕获待完成构建基线后开展。

通用篇解释程序与资源接口、Pass 依赖、资源生命周期和提交完成条件，可独立阅读；Piccolo 篇沿实际代码说明这些问题怎样落地。阅读时分别辨认通用约束、API/平台前提与 Piccolo 的实现选择，相关基础按需查阅。

先读 [09：从一次按键到角色位移]({{< relref "engine-toolchain/piccolo-engine-09-input-motor-character-movement.md" >}})，追输入、速度/方向、控制器目标及 Transform/Character 时序，再进入 **11 相机 → 15 GPU 资源 → 16 材质、光照与阴影 → 17 后处理输出**。这是学习顺序；实际绘制中相机、几何资源与表面数据会汇合，资源也会跨帧复用。[10 刚体与碰撞查询]({{< relref "engine-toolchain/piccolo-engine-10-rigidbody-collision-character-motion.md" >}}) 已补齐 Static 创建、胶囊查询、形状更新与模拟步进的关系；随后可用下方 18 回到编辑器对象操作。

## 编辑器、调试与最小改动验证

研究编辑器怎样改变运行数据，怎样观察系统行为，以及一次改动怎样证明生效。

**已有专篇**

- [编辑器怎样修改、保存并恢复场景状态]({{< relref "engine-toolchain/piccolo-engine-08-editor-save-reload.md" >}})：沿 Wall_1 区分反射面板与 setter 写入，追踪保存字段、实例组件覆盖和重载重建，并核对关卡元数据遗漏与运行身份变化；源码已核对，实际保存/重载待验证。
- [18：编辑器怎样拾取、创建和删除场景对象]({{< relref "engine-toolchain/piccolo-engine-18-editor-pick-create-delete.md" >}})：沿整数 ID 图回读、资源定义实例化与删除描述追踪对象变化，核对未命中 ID、蒙皮拾取和多部件清理的边界；Shader/CPU 条件检查通过，实际编辑器操作与 GPU 回读待验证。

**已有前置阅读**

- [窗口与编辑器回调怎样绑定、调用和注销]({{< relref "engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md" >}})：从窗口事件与属性面板两类调用，区分注册、捕获寿命、实际分发和退出边界。
- [为什么引擎首先是一套内容生产工具]({{< relref "system-design/game-engine-architecture-01-content-production.md" >}})：理解编辑器工作流怎样影响引擎结构。
- [RenderDoc 阅读入口]({{< relref "rendering/renderdoc-reading-entry.md" >}})：补充图形捕获与读图方法，Piccolo 的具体接入步骤仍待补充。

**待写建议**

- **Piccolo 最小验证：断点、日志、画面与图形捕获怎样互相印证（待写）**

后续实践文章将分别记录源码版本、构建结果和运行观察，方便复现。

---

**继续阅读**

[引擎研发]({{< relref "reading/engine.md" >}}) · [游戏引擎架构地图]({{< relref "system-design/game-engine-architecture-series-index.md" >}})
