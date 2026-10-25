---
title: "Piccolo"
description: "从架构地图与前置知识进入启动、对象恢复、渲染提交和编辑器保存重载，沿实际代码学习读懂并调试一台具体引擎。"
url: "/reading/engine/piccolo/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "piccolo"
series_id: "piccolo-engine"
series_role: "index"
series_title: "Piccolo 源码阅读与调试"
reading_status: "13 篇正文齐备；配置与生成已实测，完整构建和运行待验证"
---

> **已有内容与待写内容**
>
> 主线 00—08 与四篇前置共 13 篇正文齐备，覆盖架构、构建基线、启动、对象恢复、渲染提交与编辑器保存重载。01 已实际完成 Windows 配置、反射/序列化和 shader 生成；完整 Debug 编辑器编译仍被第三方兼容问题阻塞。实际启动、保存/重载、断点观察与图形捕获尚未完成，各篇分别说明源码推导和运行证据范围。
>
> 下方其余 Piccolo 篇目仍为**待写建议拟题**；前置和对照链接继续指向已有公共专题或其他引擎文章。

**官方材料**

- [Piccolo 官方仓库与构建说明](https://github.com/BoomingTech/Piccolo)
- [Piccolo 官方 Wiki](https://github.com/BoomingTech/Piccolo/wiki)

建议先用架构地图认识目标；缺少 C++ 基础时，先走下面的公共入口，再读 Piccolo 专项前置和渲染基础。随后读 01 构建与调试基线，沿 02—08 进入启动、逐帧执行、对象恢复、渲染与编辑器。01 记录当前工具环境中的真实构建边界，实际调试前仍需解决完整编译阻塞并核对程序、配置和符号。源码基线为 `f5053707fed4`，每篇分别说明核对与实践范围。

## 公共基础之后，补 Piccolo 的约定

公共入口：[C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。先理解值与借用、所有权、虚调用，再按当前系统补容器、模板/宏、回调和编译链接；其中缺课状态见公共目录。

现有三篇 Piccolo C++ 文章继续承担专项案例：谁持有关卡与组件、Component/RHI 怎样分派、ReflectionPtr 与生成器怎样接线。已经读过通用规则的读者可直接追这些项目约定，不必再次顺读所有语言解释。

**专项待补顺序**：先补对象容器修改、擦除与借用失效，再补窗口/编辑器回调的绑定和注销，最后补状态缓冲交接与实际线程边界。前两项接公共容器/回调课；最后一项接公共并发课，不凭双缓冲或类名推断多线程。它们目前是补写方向，不计入已有 13 篇正文。

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

- [引擎运行时底座]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}})：理解执行模型、任务、对象元信息等机制在引擎中的位置。
- [CPU 与内存体系]({{< relref "performance/hardware-cpu-index.md" >}})：研究数据布局、缓存和并行成本时查阅。

## 场景、对象与组件

理解世界中的对象怎样被描述、创建、更新和销毁，以及组件怎样参与运行。

**已有专篇**

- [场景文件怎样成为世界中的对象与组件]({{< relref "engine-toolchain/piccolo-engine-04-scene-to-objects-components.md" >}})：沿默认关卡的 Wall_1，追踪实例与定义合并、运行身份、组件 postLoad 接线及对象持有；源码与资源已核对，运行待验证。
- [改一个位置，怎样沿逻辑数据追到渲染表示]({{< relref "engine-toolchain/piccolo-engine-06-position-to-render-representation.md" >}})：固定 Wall_1 的位置改动，逐值核对 Transform 双缓冲、描述交接、实例身份与 RenderEntity 更新，含较晚写入的时序反例；运行待验证。

**已有前置阅读**

- [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：先从 Component 接口追到具体组件的更新与析构。
- [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：拆开组件查询宏、模板返回类型和名字匹配协议。

**已有对照阅读**

- [GameObject 与 Actor：两种世界组织方式]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})：提供比较对象模型的观察角度，正文讨论的是 Unity／Unreal。

## 资源、反射与序列化

研究资源文件怎样描述对象、类型信息怎样参与读写，以及数据怎样恢复成可运行的场景。

**已有专篇**

- [资源、类型信息与序列化怎样共同恢复对象]({{< relref "engine-toolchain/piccolo-engine-05-resource-type-serialization.md" >}})：沿 Wall_1 的实例 Transform，连起已知类型字段读取、运行时类注册与具体组件构造，并区分写出、兼容和业务接线边界；源码与模板已核对，实际生成与运行待验证。

**已有前置与对照阅读**

- [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：理解字段标记、生成访问器、Serializer 特化与运行时注册，含独立小程序。
- [资产与发布在引擎中的位置]({{< relref "system-design/game-engine-architecture-05-asset-and-delivery.md" >}})：理解导入、资产、构建与交付之间的关系。
- [Unity 资产系统与序列化]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})：作为另一种实现的对照阅读。

## 渲染链路与图形接口

研究场景数据怎样进入渲染系统，经过资源准备、渲染阶段和图形接口提交形成画面。

**已有专篇**

- [从渲染场景进入 Pass 与 Vulkan 提交]({{< relref "engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md" >}})：沿 Wall_1 的可见节点、材质/网格分组和实例矩阵，核对 GBuffer 绘制、命令记录、提交同步与呈现目标；源码与接口语义已核对，运行及图形捕获待验证。

**已有前置专篇**

- [读懂渲染管线：从场景对象到一帧画面]({{< relref "rendering/piccolo-rendering-01-scene-to-frame.md" >}})：结合 Piccolo 的渲染表示、可见性、Pass、Draw Call 与 Vulkan 呈现，建立完整链路；区分源码推导与运行证据。

**已有前置阅读**

- [游戏图形系统]({{< relref "rendering/game-graphics-stack-series-index.md" >}})：建立引擎、图形 API、驱动与 GPU 的整体链路。
- [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：从 RHI 接口追到 VulkanRHI，区分后端分派与图形命令提交。
- [图形 API 基础]({{< relref "rendering/graphics-api-series-index.md" >}})：理解命令、资源与平台后端的抽象。
- [图形数学]({{< relref "rendering/graphics-math-series-index.md" >}})：补充空间变换、可见性与几何判断。

先用前置专篇建立完整链路，再在 07 中追踪一个实例怎样进入 GBuffer 绘制与提交；实际断点和图形捕获待完成构建基线后开展。

## 编辑器、调试与最小改动验证

研究编辑器怎样改变运行数据，怎样观察系统行为，以及一次改动怎样证明生效。

**已有专篇**

- [编辑器怎样修改、保存并恢复场景状态]({{< relref "engine-toolchain/piccolo-engine-08-editor-save-reload.md" >}})：沿 Wall_1 区分反射面板与 setter 写入，追踪保存字段、实例组件覆盖和重载重建，并核对关卡元数据遗漏与运行身份变化；源码已核对，实际保存/重载待验证。

**已有前置阅读**

- [为什么引擎首先是一套内容生产工具]({{< relref "system-design/game-engine-architecture-01-content-production.md" >}})：理解编辑器工作流怎样影响引擎结构。
- [RenderDoc 阅读入口]({{< relref "rendering/renderdoc-reading-entry.md" >}})：补充图形捕获与读图方法，Piccolo 的具体接入步骤仍待补充。

**待写建议**

- **Piccolo 最小验证：断点、日志、画面与图形捕获怎样互相印证（待写）**

后续实践文章将分别记录源码版本、构建结果和运行观察，方便复现。

---

**继续阅读**

[引擎研发]({{< relref "reading/engine.md" >}}) · [游戏引擎架构地图]({{< relref "system-design/game-engine-architecture-series-index.md" >}})
