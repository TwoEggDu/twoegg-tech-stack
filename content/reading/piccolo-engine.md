---
title: "Piccolo"
description: "沿启动、一帧和对象变化读懂Piccolo；按需深入输入、物理、动画、资源、渲染与编辑器。"
url: "/reading/engine/piccolo/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "piccolo"
series_id: "piccolo-engine"
series_role: "index"
series_title: "Piccolo 源码阅读与调试"
reading_status: "27 篇正文 · 源码阅读与调试"
---

<div class="actions piccolo-home-actions" aria-label="选择阅读方式">
  <a class="button" href="{{< relref "system-design/piccolo-engine-00-architecture-map.md" >}}">从架构地图开始 →</a>
  <a href="#chapters">按系统查阅 ↓</a>
</div>

**第一次读**：[00 架构地图]({{< relref "system-design/piccolo-engine-00-architecture-map.md" >}}) → [02 启动到第一帧]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}}) → [03 一帧执行]({{< relref "engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md" >}})。先找到入口和主循环，再沿对象变化深入各系统。准备实际动手时，先核对[01 构建与调试基线]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}})；语言或图形概念遇到卡点时，再查页末前置。

> **当前范围**：27篇正文已写，原定00—18主线齐备。已有固定源码核对与部分独立程序、资源和Shader编译验证。2026-10-26已完成带两处兼容补丁的Debug构建、启动到首次提交/呈现的实际断点与正常退出，并取得独立RenderDoc后续帧图像；06另有位置从Transform到渲染实体/可见节点的受控setter实测；其他系统专项仍待验证；[详细范围见页末](#validation)。

<a id="chapters"></a>

## 工程、构建与启动

从哪里进入引擎，怎样核对工程、配置与程序。

- [00｜架构地图：编辑器、世界、资源与渲染怎样协作]({{< relref "system-design/piccolo-engine-00-architecture-map.md" >}})：找到职责、构建和数据交接的主要入口。
- [01｜建立构建与调试基线：源码、生成文件、配置与程序怎样对应]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}})：核对工具链、生成产物、程序与符号，查看兼容修补和实际符号、路径证据。
- [02｜从启动走到第一帧：初始化、世界加载与退出怎样衔接]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}})：从编辑器入口追到系统启动、首次世界加载与退出。

## 一帧执行与系统协作

一轮更新怎样推进，逻辑与渲染怎样交换状态。

- [03｜一帧怎样推进：调用顺序、状态交接与线程怎样核对]({{< relref "engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md" >}})：沿主循环区分调用顺序、数据交接、CPU任务与GPU工作。
- [PCPP-3｜状态缓冲怎样交接，哪些地方真的跨线程]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}})：核对发布、消费与复用，判断哪些工作确实跨线程。

## 场景、对象与组件

场景描述怎样变成对象，一个位置变化怎样进入画面。

- [04｜场景文件怎样成为世界中的对象与组件]({{< relref "engine-toolchain/piccolo-engine-04-scene-to-objects-components.md" >}})：沿Wall_1追对象创建、组件装配和运行身份。
- [06｜改一个位置，怎样沿逻辑数据追到渲染表示]({{< relref "engine-toolchain/piccolo-engine-06-position-to-render-representation.md" >}})：逐段跟踪位置从Transform交到渲染实体，已有Wall_1的实际断点记录。

## 资源、反射与序列化

文件中的数据如何恢复为组件，脚本如何访问这些对象。

- [05｜资源、类型信息与序列化怎样共同恢复对象]({{< relref "engine-toolchain/piccolo-engine-05-resource-type-serialization.md" >}})：连接资源读取、生成的访问代码与具体组件构造。
- [14｜Lua脚本怎样读写组件并调用引擎方法]({{< relref "engine-toolchain/piccolo-engine-14-lua-component-reflection.md" >}})：追脚本状态、字段访问与反射调用的实际边界。

## 输入、运动与物理

从一次按键追到角色位移，分清运动规则和碰撞查询的职责。

- [09｜从一次按键到角色位移：输入、运动组件与控制器怎样协作]({{< relref "engine-toolchain/piccolo-engine-09-input-motor-character-movement.md" >}})：连接输入状态、移动计算、控制器目标与最终位置。
- [10｜刚体与碰撞查询怎样参与角色运动]({{< relref "engine-toolchain/piccolo-engine-10-rigidbody-collision-character-motion.md" >}})：区分刚体、胶囊查询、形状更新与模拟步进。

继续看[11 相机]({{< relref "engine-toolchain/piccolo-engine-11-camera-follow-render-view.md" >}})，理解角色状态怎样影响视角；看[12 动画]({{< relref "engine-toolchain/piccolo-engine-12-animation-skeleton-mesh-skinning.md" >}})，理解姿态怎样进入网格。

## 渲染链路与图形接口

先读两篇全景入口，再按相机、资源、着色、输出或动画特效深入。

- [GFX-01｜读懂渲染管线：从场景对象到一帧画面]({{< relref "rendering/piccolo-rendering-01-scene-to-frame.md" >}})：建立场景表示、可见性、Pass、Draw与呈现的整体链路。
- [GFX-02｜从GLSL到GPU：用Piccolo串起渲染引擎、API与系统]({{< relref "rendering/piccolo-rendering-02-shader-api-driver-gpu.md" >}})：分清程序产物、资源绑定、驱动和GPU各自负责什么。
- [11｜相机怎样跟随角色，并把视角交给渲染系统]({{< relref "engine-toolchain/piccolo-engine-11-camera-follow-render-view.md" >}})：追相机跟随、视图交接与绘制时的相机选择。
- [15｜网格与纹理怎样从文件成为GPU资源]({{< relref "engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md" >}})：连接文件载荷、缓存、上传和绘制绑定。
- [16｜材质、光照与阴影怎样共同决定表面颜色]({{< relref "engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md" >}})：沿G-buffer和光照计算核对表面颜色的来源。
- [17｜场景颜色怎样经过后处理成为最终画面]({{< relref "engine-toolchain/piccolo-engine-17-post-processing-final-frame.md" >}})：追色调映射、FXAA、UI合成与交换链输出。
- [07｜从渲染场景进入Pass与Vulkan提交]({{< relref "engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md" >}})：从可见对象追到批次、缓冲绑定、命令记录与队列提交。
- [12｜一段动画怎样成为骨骼姿态与网格蒙皮]({{< relref "engine-toolchain/piccolo-engine-12-animation-skeleton-mesh-skinning.md" >}})：连接动画采样、骨骼层级、关节矩阵与顶点变形。
- [13｜粒子发射器怎样进入GPU模拟与绘制]({{< relref "engine-toolchain/piccolo-engine-13-particle-emitter-gpu-simulation.md" >}})：追发射请求、粒子状态更新与后续绘制。

**建议路线**：11 相机 → 15 GPU资源 → 16 表面颜色 → 17 最终输出，再用07深入提交。它是学习顺序；实际绘制时相机、几何与材质数据会汇合，资源也会跨帧复用。

## 编辑器、调试与最小改动验证

编辑器怎样改变对象，保存之后又如何恢复。

- [08｜编辑器怎样修改、保存并恢复场景状态]({{< relref "engine-toolchain/piccolo-engine-08-editor-save-reload.md" >}})：区分面板写入、保存收集、组件覆盖与重载重建。
- [18｜编辑器怎样拾取、创建和删除场景对象]({{< relref "engine-toolchain/piccolo-engine-18-editor-pick-create-delete.md" >}})：沿拾取ID、对象实例化和删除路径追踪状态变化。
- [PCPP-2｜窗口与编辑器回调怎样绑定、调用和注销]({{< relref "engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md" >}})：检查回调注册、捕获寿命、实际分发与退出清理。

需要设计图形观察时，查[RenderDoc阅读入口]({{< relref "rendering/renderdoc-reading-entry.md" >}})。01—03已有首轮断点和独立场景捕获；逐Pass、资源和同步的图形验收仍待开展。

## 公共基础之后，补 Piccolo 的约定

前置按当前问题选读，不要求先学完全部C++或图形基础。[C++公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})解释通用规则，下面几篇继续核对Piccolo的具体约定。

- [CPP-01｜智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}})：看懂系统、关卡、对象和组件之间谁持有谁。
- [CPP-02｜类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}})：从Component和RHI接口找到实际实现。
- [CPP-03｜模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：拆开组件查询、宏标记与构建期生成代码。
- [PCPP-1｜对象容器修改后，持有者、借用与遍历怎样变化]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}})：检查删除对象后的容器成员资格、强持有和遍历边界。

PCPP-2回调与PCPP-3缓冲分别在上面的编辑器、一帧分组中。容器规则可先补CXX-04；熟悉公共规则后，可直接读对应Piccolo案例。

**按需查阅公共专题**

- 架构：[职责、依赖与资源服务边界]({{< relref "system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md" >}})、[运行时底座]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}})、[资产与发布]({{< relref "system-design/game-engine-architecture-05-asset-and-delivery.md" >}})。
- 工程：[构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})、[从C#到C++的所有权与编译链接]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})。
- 图形：[游戏图形系统]({{< relref "rendering/game-graphics-stack-series-index.md" >}})、[图形API]({{< relref "rendering/graphics-api-series-index.md" >}})、[图形数学]({{< relref "rendering/graphics-math-series-index.md" >}})；并行与布局问题可查[CPU与内存体系]({{< relref "performance/hardware-cpu-index.md" >}})。
- 其他实现对照：[GameObject与Actor]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})、[Unity资产系统与序列化]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})、[内容生产工具]({{< relref "system-design/game-engine-architecture-01-content-production.md" >}})。

<a id="validation"></a>

<details class="validation-details">
<summary>查看验证范围与实际运行记录</summary>

<p>主线 00—18、五篇前置与渲染补充、三篇 C++ 专项共 27 篇正文已写，覆盖架构、构建基线、启动、对象恢复、输入运动、相机、GPU 资源、表面着色、后处理输出与编辑器操作/保存重载。01—03 已实际完成带兼容补丁的 Windows Debug 完整构建、模块/PDB/配置身份核对、首次世界加载、提交/呈现返回成功及正常退出 0，03 另核对首轮槽位交换与对象描述清理；独立 RenderDoc 后续帧图像已补入02。06已核对Wall_1的一次真实setter、缓冲交换、描述入队/消费及可见节点矩阵引用；该实验不包含鼠标拖轴、保存或GPU像素。09 含原输入/运动/控制器函数及数学实现的独立条件检查；10 含原形状转换/步进/更新函数的记录适配与几何检查；12 含原骨骼/数学的独立资源桥接与 Shader 编译；13 含原类型布局/字节、独立池状态检查及五份 Shader 编译，实际 GPU 粒子待验证；14 含随附 Lua/sol2 与原桥接/反射的独立桩宿主检查，实际引擎脚本与角色运行待验证；GFX-02 核对程序产物与系统边界；11/15 含独立数学与实际资源检查；16/17 含相关 Shader 编译和条件数值检查；18 含拾取 Shader 编译及原 ID/删除/坐标函数的条件检查。原源码的MSVC19.51兼容失败及两处补丁均有记录；构建仍有运行库冲突等警告。实际输入运动、相机跟随、GPU上传/着色/后处理的分项核对、拾取/创建/删除和保存/重载尚未验收，各篇分别说明源码推导和运行证据范围。</p>

<p>源码基线为 <code>f5053707fed4</code>。源码推导、独立实验与完整引擎运行分别记录，每篇说明自己的证据范围。本轮基线为固定提交加两处已保存兼容补丁；断点、日志与独立捕获已开始落地，其他系统专项继续按各篇待验证项推进，不增加正文数量。</p>
</details>

**官方材料**：[Piccolo官方仓库与构建说明](https://github.com/BoomingTech/Piccolo) · [官方Wiki](https://github.com/BoomingTech/Piccolo/wiki)

**继续阅读**：[引擎研发]({{< relref "reading/engine.md" >}}) · [游戏引擎架构地图]({{< relref "system-design/game-engine-architecture-series-index.md" >}})
