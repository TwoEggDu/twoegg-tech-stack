# 游戏开发全栈知识体系 v10

> 目标读者：有 Unity 开发经验的开发者。
> 终极目标：能读懂 Unity / Unreal 源码，具备自研游戏引擎和游戏后端的基础。
> 原则：不怕章节多，不允许讲不清楚。每篇先讲原理，再讲实现，再讲项目对应。

---

## 全局结构

```
Layer 0  背景与历史              ← 理解"为什么现在是这样"
Layer 1  底层基础                ← C++、数学、OS、图形 API、网络协议
Layer 2  引擎技术                ← Unity 渲染、Unreal 架构、引擎子系统
Layer 3  工具与调试              ← Profiler、RenderDoc、调试工具链
Layer 4  优化实践                ← 移动端 GPU/CPU 优化、Shader 优化
Layer 5  系统设计                ← GAS、网络同步、后端架构、自研引擎设计
Layer 6  美术协作                ← DCC 流程、规范制定、问题排查
Layer 7  工程与交付              ← CI/CD、安全、本地化、分析
```

---

## 系列零：背景与历史（15 篇）

### 零·A — 图形发展史（5 篇）

| 编号 | 标题 |
|------|------|
| 零A-01 | 从线框到光栅化：1960s–1980s 的早期 3D 图形 |
| 零A-02 | 固定管线时代：1990s GPU 出现前，软件渲染器做了什么 |
| 零A-03 | 可编程管线革命：2001 年 Shader Model 1.0 到 SM 3.0 |
| 零A-04 | 统一着色器架构：2006 年 G80，Vertex/Fragment 合并成通用计算单元 |
| 零A-05 | 现代渲染的转折：PBR 普及、实时光追、神经渲染的出现 |

### 零·B — 图形硬件发展史（5 篇）

| 编号 | 标题 |
|------|------|
| 零B-01 | 早期显卡：从帧缓冲控制器到 3Dfx Voodoo |
| 零B-02 | NVIDIA GeForce 256 到 G80：硬件 T&L、可编程 Shader、统一架构 |
| 零B-03 | 移动 GPU 的兴起：PowerVR、Mali、Adreno——为什么 TBDR 成了移动标准 |
| 零B-04 | GPU 通用计算：CUDA / OpenCL，GPU 从专用到通用的演变 |
| 零B-05 | 现代 GPU 架构一览：SM、Warp、光追核心、Tensor 核心 |

### 零·C — 计算机与网络发展史（5 篇）

| 编号 | 标题 |
|------|------|
| 零C-01 | 进程与线程：主线程、渲染线程、Worker Thread 的分工 |
| 零C-02 | 内存模型：虚拟地址空间、堆/栈/显存，为什么 GPU 有自己的内存 |
| 零C-03 | 驱动层是什么：用户态驱动 vs 内核态驱动，图形 API 调用如何到达硬件 |
| 零C-04 | 互联网基础设施演进：从 ARPANet 到现代 CDN |
| 零C-05 | 移动端操作系统的特殊性：iOS/Android 内存限制、后台限制、GPU 访问限制 |

---

## 系列一：底层基础

### 一·A — 图形数学（5 篇）

| 编号 | 标题 |
|------|------|
| 数学-01 | 向量与矩阵：内存布局、精度问题、列主序 vs 行主序 |
| 数学-02 | 四元数：为什么旋转不用欧拉角，Slerp 插值原理 |
| 数学-03 | 视锥体数学：裁剪平面提取、物体可见性判断 |
| 数学-04 | 射线与包围盒：Ray-AABB、Ray-OBB、Ray-Triangle 求交 |
| 数学-05 | 数值稳定性：浮点误差、深度精度问题、Kahan 求和 |

### 一·B — C++ 引擎代码阅读基础（7 篇）

*目标：能读懂 Unity/Unreal C++ 源码，不需要从零学 C++。*

| 编号 | 标题 |
|------|------|
| C++-01 | 从 C# 到 C++：指针、引用、栈堆的本质区别 |
| C++-02 | 模板与泛型：TArray\<T\> 背后是什么，特化与偏特化 |
| C++-03 | 宏展开：UCLASS() / GENERATED_BODY() 展开后的真实代码 |
| C++-04 | 内存管理：为什么引擎要自己写分配器，线性/Pool/Arena |
| C++-05 | 虚函数与多态：vtable 内存布局，RTTI 开销 |
| C++-06 | 现代 C++：move semantics、lambda、constexpr 阅读能力 |
| C++-07 | 如何阅读大型 C++ 项目：导航、跳转、全局搜索策略 |

### 一·C — 图形 API 基础（7 篇）

| 编号 | 标题 |
|------|------|
| API-01 | 图形 API 是什么：OpenGL / Vulkan / Metal / DirectX 解决的共同问题 |
| API-02 | OpenGL：状态机模型、驱动隐式管理、为什么逐渐被取代 |
| API-03 | Vulkan：显式控制、Command Buffer、RenderPass 与 Framebuffer |
| API-04 | Metal：苹果的图形 API，与 Vulkan 设计哲学的异同 |
| API-05 | DirectX 12：Windows 平台的显式 API，D3D12 与 DX11 的代差 |
| API-06 | Unity 的图形后端：GraphicsDeviceType、API 选择与兼容性 |
| API-07 | Shader 编译管线：HLSL → SPIR-V / MSL / DXBC，Variant 爆炸与缓存 |

### 一·E — 存储设备与 IO 基础（6 篇）

*理解存储硬件的特性，是理解打包策略、加载优化、资源管线的物理基础。*

| 编号 | 标题 |
|------|------|
| IO-01 | 为什么碎文件更慢：从 HDD / SSD / UFS、文件系统和 OS I/O 机制，到游戏加载链 |
| IO-02 | 存储设备类型：HDD / SSD / NVMe / eMMC / UFS 的读写特性与延迟差异 |
| IO-03 | 文件系统基础：路径、目录项、元数据和页缓存为什么会影响游戏加载 |
| IO-04 | OS I/O 机制：同步 / 异步 I/O、DMA、mmap、Read-Ahead、队列深度 |
| IO-05 | 移动端存储特性：eMMC vs UFS 随机读写差异，碎访问为什么在手机上也照样危险 |
| IO-06 | IO 性能分析：用 fio / iostat / Perfetto / Instruments 定位 IO 瓶颈 |

### 一·D — 网络技术基础（8 篇）

| 编号 | 标题 |
|------|------|
| 网络-01 | TCP / UDP 基础：连接模型、可靠性、为什么游戏常用 UDP |
| 网络-02 | HTTP / WebSocket / gRPC：各自的使用场景与性能边界 |
| 网络-03 | 游戏专用协议设计：序列化（Protobuf / FlatBuffers）、包格式、心跳 |
| 网络-04 | 延迟、抖动、丢包：网络质量指标与游戏体验的关系 |
| 网络-05 | 客户端-服务器模型 vs P2P：权威服务器的意义 |
| 网络-06 | 网络同步基础：状态同步 vs 帧同步，各自的适用场景 |
| 网络-07 | 延迟补偿：客户端预测、服务器回溯、插值与外推 |
| 网络-08 | 防作弊基础：权威服务器校验、行为异常检测思路 |

---

## 系列二：Unity 渲染系统（已完成 + 待补充）

| 编号 | 标题 | 状态 |
|------|------|------|
| 00a | 渲染入门：CPU、GPU 与 Shader 的分工 | ✅ |
| 00b | 渲染入门：顶点为什么要经过五个坐标系 | ✅ |
| 01 | Mesh / Material / Texture 怎么决定像素颜色 | ✅ |
| 01b | Draw Call 与批处理 | ✅ |
| 01b-2 | GPU Instancing 与 SRP Batcher | ✅ |
| 01c | Render Target：Color Buffer / Depth Buffer / G-Buffer | ✅ |
| 01c5 | 调试视角补桥：为什么工具里总在看 Draw Call、Pass 和 Render Target | ✅ |
| 01d | Frame Debugger 使用指南 | ✅ |
| 01e | RenderDoc 入门 | ✅ |
| 01f | RenderDoc 进阶 | ✅ |
| 02 | 四条光照路径 | ✅ |
| 02b | Shadow Map：生成、级联与阴影质量问题 | ✅ |
| 03 | 骨骼动画蒙皮 | ✅ |
| 04 | 粒子系统 | ✅ |
| 05 | 后处理：Volume 系统与全屏 Pass | ✅ |
| 06 | Built-in 管线 | ✅ |
| 07 | 为什么需要 SRP | ✅ |
| 08 | SRP 核心概念 | ✅ |
| 09 | URP 架构 | ✅ |
| 10 | URP 扩展开发 | ✅ |
| 10b | RenderGraph | ✅ |
| 11 | HDRP 定位 | ✅ |
| 补A | Mesh 与 Texture 存储基础（顶点格式、压缩、Mip） | 待写 |
| 补B | CBuffer 超限与常量缓冲区管理 | 待写 |
| 补C | LOD 与 Culling 系统：Frustum / Occlusion / HZB | 待写 |
| 补D | UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas | 待写 |
| 补E | 2D 渲染：Sprite Atlas、九宫格、2D 光照 | 待写 |
| 补F | 渲染算法横向对比：GI 方案（Lightmap/Probe/SSGI/Lumen） | 待写 |
| 补G | 渲染算法横向对比：抗锯齿（MSAA/TAA/FXAA/DLSS/FSR） | 待写 |
| 补H | 渲染算法横向对比：反射方案（Cubemap/Planar/SSR/RTXGI） | 待写 |
| 补I | 帧时序与显示技术：VSync/G-Sync/FreeSync、Frame Pacing、输入延迟链路 | 待写 |
| 补J | HDR 显示输出：Display P3 / HDR10、色彩空间管理、SDR vs HDR 渲染路径 | 待写 |
| 补K | PC 平台现代特性：DLSS/FSR/XeSS 升采样、Variable Rate Shading、DirectStorage | 待写 |
| 补L | Virtual Texturing：UDIM、Virtual Texture 原理、Unreal Virtual Heightfield Mesh | 待写 |

---

## 系列三：移动端硬件与优化

### 三·A — 移动端硬件基础（4 篇）

| 编号 | 标题 |
|------|------|
| 硬件-01 | 移动端 SoC 总览：CPU、GPU、内存、闪存在一块芯片上意味着什么 |
| 硬件-02 | TBDR 架构详解：Tile、On-Chip Buffer、HSR 如何改变渲染逻辑 |
| 硬件-03 | 移动端功耗与发热：为什么帧率稳定比峰值帧率更重要 |
| 硬件-04 | 移动端 vs 主机/PC：带宽瓶颈、内存带宽共享、驱动差异 |

### 三·B — 性能分析工具（8 篇）

| 编号 | 标题 | 状态 |
|------|------|------|
| 工具-01 | Unity Profiler 与 Frame Debugger：CPU 侧分析入口 | ✅ |
| 工具-02 | RenderDoc 完整指南：帧捕获、Pipeline State、资源查看、Shader 调试 | ✅ |
| 工具-03 | ARM Mali Graphics Debugger：Mali GPU Counter 解读与瓶颈定位 | ✅ |
| 工具-04 | Snapdragon Profiler：Adreno GPU 的 Counter 体系与瓶颈定位 | ✅ |
| 工具-05 | Xcode GPU Frame Capture：iOS / Metal 渲染调试 | ✅ |
| 工具-06 | 如何读懂 GPU Counter：填充率、带宽、ALU 利用率、Early-Z 命中率 | ✅ |
| 工具-07 | 真机问题排查流程：从闪退到黑屏到画面异常的系统性方法 | |
| 工具-09 | 性能诊断工具选择指南：什么问题用 Frame Debugger / RenderDoc / Unity Profiler / Mali Debugger / Snapdragon Profiler | ✅ |

### 三·C — GPU 渲染性能优化（6 篇）

| 编号 | 标题 |
|------|------|
| GPU优化-01 | Draw Call 与 Overdraw 优化：合批策略与 Alpha 排序 |
| GPU优化-02 | 带宽优化：纹理压缩（ASTC/ETC2）、RT 格式选择、Resolve 时机 |
| GPU优化-03 | Shader 优化：精度（half vs float）、分支、采样次数 |
| GPU优化-04 | 阴影优化：Cascade 配置、Distance Shadow、Shadow Proxy |
| GPU优化-05 | 后处理在移动端的取舍与降质策略 |
| GPU优化-06 | URP 管线配置优化：关闭不需要的 Pass、Depth Priming、MSAA |

### 三·D — CPU 性能优化（5 篇）

| 编号 | 标题 |
|------|------|
| CPU优化-01 | C# GC 压力：堆分配来源、避免 GC 的写法、对象池 |
| CPU优化-02 | IL2CPP vs Mono：编译差异、性能影响、调试限制 |
| CPU优化-03 | Update 调用链优化：减少 Update 数量、手动调度管理器 |
| CPU优化-04 | Unity Profiler CPU 深度分析：调用堆栈、GC.Alloc 定位、HierarchyMode |
| CPU优化-05 | 内存预算管理：按系统分配上限、Texture Streaming、OOM 防护 |

---

## 系列四：Shader 手写技法

### 四·A — 入门（4 篇）

| 编号 | 标题 |
|------|------|
| 入门-00 | 我的第一个 Shader：让一个物体显示纯色的完整流程 |
| 入门-01 | 让颜色动起来：用 _Time 驱动颜色变化，理解 Shader 执行时机 |
| 入门-02 | 采样一张贴图：UV 是什么，tex2D 怎么用 |
| 入门-03 | 加上光照：接入 URP 主光方向，做最简单的 Lambert 漫反射 |

### 四·B — 语法基础（5 篇）

| 编号 | 标题 |
|------|------|
| 基础-01 | ShaderLab 结构：Properties / SubShader / Pass / Tags |
| 基础-02 | HLSL 在 Unity 中的基础：数据类型、内置变量、include 体系 |
| 基础-03 | Vertex Shader 完整写法：输入结构、变换链、输出插值 |
| 基础-04 | Fragment Shader 完整写法：UV 采样、法线、输出颜色 |
| 基础-05 | URP Lit Shader 拆解：从 LitInput.hlsl 到 Lighting.hlsl |

### 四·C — 核心技法（11 篇）

| 编号 | 标题 |
|------|------|
| 技法-01 | 透明与半透明：Blend 方程、ZWrite、渲染队列与排序 |
| 技法-02 | 卡通渲染：阶梯光照、描边（外扩法线 / 后处理描边） |
| 技法-03 | 法线贴图：切线空间 TBN 矩阵构建与采样 |
| 技法-04 | 自发光与 HDR：Emission 贴图与 Bloom 联动 |
| 技法-05 | UV 动画：Offset / Scroll / 序列帧 |
| 技法-06 | 顶点动画：草的摆动、旗帜飘动 |
| 技法-07 | 溶解效果：噪声贴图 + clip() |
| 技法-08 | 深度采样技巧：软粒子、边缘高亮、水面焦散 |
| 技法-09 | Stencil Buffer 应用：遮罩、X 光透视、描边辅助 |
| 技法-10 | 噪声函数：Value / Perlin / FBM 在 Shader 里的常见用途 |
| 技法-11 | 数学工具包：saturate / lerp / smoothstep / frac / step |

### 四·D — 进阶（8 篇）

| 编号 | 标题 |
|------|------|
| 进阶-01 | Multi-Pass Shader：描边 Pass + 主体 Pass |
| 进阶-02 | 自定义 Shadow Caster Pass：半透明物体投影 |
| 进阶-03 | GPU Instancing Shader：UNITY_INSTANCING_BUFFER 手写 |
| 进阶-04 | Compute Shader 入门：线程组、SV_GroupID、RWTexture2D |
| 进阶-05 | 项目实战：卡通渲染 Shader 拆解（Body / Hair / Face 差异） |
| 进阶-06 | 项目实战：SDF Height Shadow 自定义阴影系统 |
| 进阶-07 | 实时光线追踪基础：DXR / Vulkan RT，混合渲染思路 |
| 进阶-08 | Visibility Buffer：Nanite 背后的思路与传统 G-Buffer 的区别 |
| 进阶-09 | GPU Driven Rendering：GPU Culling、Indirect Draw、Multi-Draw Indirect 原理 |
| 进阶-10 | GPU Scene 与 Per-Instance Data：现代引擎如何用 GPU 管理场景数据 |

### 四·E — 游戏常用渲染效果（21 篇）

**角色（5 篇）**

| 编号 | 标题 |
|------|------|
| 角色-01 | 皮肤渲染：次表面散射（SSS）原理与移动端近似方案 |
| 角色-02 | 头发渲染：各向异性高光（Kajiya-Kay）与 Alpha 排序 |
| 角色-03 | 眼睛渲染：折射、高光、瞳孔缩放 |
| 角色-04 | 卡通角色全流程：描边 + 阶梯光照 + Rim Light + 自定义 Shadow |
| 角色-05 | 角色 LOD 与性能：骨骼数量、材质合并、Imposter |

**地形（4 篇）**

| 编号 | 标题 |
|------|------|
| 地形-01 | Terrain 渲染原理：Heightmap、SplatMap 与多层纹理混合 |
| 地形-02 | 地形细节层：Detail Mesh / Grass 的 GPU Instancing |
| 地形-03 | 地形阴影：Height Shadow、SDF Shadow 与性能权衡 |
| 地形-04 | 自定义地形 Shader：替换 Unity 内置地形材质 |

**场景物件（4 篇）**

| 编号 | 标题 |
|------|------|
| 场景-01 | 草地渲染：顶点动画风吹效果 + GPU Instancing 大批量绘制 |
| 场景-02 | 树木渲染：LOD、Billboard、SpeedTree 原理与替代方案 |
| 场景-03 | 水面渲染：法线流动、SSPR 反射、折射、焦散 |
| 场景-04 | Decal（贴花）：延迟贴花与 URP Decal Projector |

**天气与大气（4 篇）**

| 编号 | 标题 |
|------|------|
| 天气-01 | 天空盒与大气散射：Rayleigh / Mie 散射，程序化天空 |
| 天气-02 | 雾效：线性雾 / 指数雾 / 高度雾在 Shader 里的计算 |
| 天气-03 | 雨雪效果：雨滴法线扰动、积雪顶面权重、粒子配合 |
| 天气-04 | 体积云与体积光：Ray Marching 原理与移动端近似 |

**特效（4 篇）**

| 编号 | 标题 |
|------|------|
| 特效-01 | 扭曲特效：屏幕空间 UV 偏移，热浪、传送门效果 |
| 特效-02 | 描边与轮廓：后处理描边（深度/法线边缘检测）vs Geometry 外扩 |
| 特效-03 | 序列帧与 Flipbook：合图优化与 UV 计算 |
| 特效-04 | 粒子 Shader 技法：软粒子、顶点色控制透明度、自定义混合 |

---

## 系列五：动画系统（7 篇）

*从蒙皮到 IK 到现代 Motion Matching，动画系统的完整知识链。*

| 编号 | 标题 |
|------|------|
| 动画-01 | Animator State Machine：状态、过渡条件、Blend Tree 深度解析 |
| 动画-02 | Root Motion：位移来源于动画时的处理方式与常见坑 |
| 动画-03 | IK（逆向运动学）：FABRIK / CCD 原理，脚踩地、手持武器的实现 |
| 动画-04 | Animation Rigging：约束系统，运行时程序化动画叠加 |
| 动画-05 | 动画压缩：关键帧减少、曲线压缩，为什么动画包很大 |
| 动画-06 | Motion Matching：放弃状态机，用数据库驱动动画的现代方案 |
| 动画-07 | Unreal 动画系统：AnimGraph、AnimInstance、AnimNotify、Control Rig |

---

## 系列六：AI 与游戏逻辑系统（7 篇）

*游戏客户端最核心的逻辑层，完全独立于渲染。*

| 编号 | 标题 |
|------|------|
| AI-01 | 有限状态机（FSM）：原理、实现、局限性 |
| AI-02 | 行为树（Behavior Tree）：Selector / Sequence / Decorator 节点模型 |
| AI-03 | GOAP（目标导向行动规划）：动态规划 AI 行为链 |
| AI-04 | NavMesh 寻路：生成原理、A* 算法、动态障碍处理 |
| AI-05 | 感知系统：视野锥、听觉、Unreal AIPerception 组件原理 |
| AI-06 | Unreal AI 框架：AIController / BlackBoard / BehaviorTree 的协作关系 |
| AI-07 | Unity AI 方案：NavMesh Agent、ML-Agents 概述、第三方 BT 库对比 |

---

## 系列七：游戏核心系统设计（8 篇）

*游戏里每个项目都会遇到的通用系统，讲清楚设计原则和实现方式。*

| 编号 | 标题 |
|------|------|
| 系统-01 | 游戏循环与时间：Fixed Timestep vs Variable、物理帧率、插值补帧 |
| 系统-02 | 输入系统：Unity Input System 架构、Unreal Enhanced Input、多平台抽象 |
| 系统-03 | 相机系统：Cinemachine 原理、跟随/望向/碰撞、FOV 动画 |
| 系统-04 | 存档系统：数据结构设计、序列化方案、云存档、防篡改 |
| 系统-05 | 背包与道具系统：数据模型设计、运行时管理、网络同步考量 |
| 系统-06 | 任务与成就系统：事件驱动设计、条件链、进度持久化 |
| 系统-07 | 对话与剧情系统：对话树结构、Ink / Yarn Spinner 原理 |
| 系统-08 | 编辑器扩展开发：Custom Inspector、Gizmo、EditorWindow、ScriptableObject 设计 |

---

## 系列七·A：软件工程基础与 SOLID 原则（13 篇）

*这是整个系列中最重要的基础篇之一。SOLID 原则是写出可维护、可扩展代码的理论根基，所有架构决策都建立在这之上。游戏项目尤其容易因忽视这些原则而积累难以偿还的技术债。*

| 编号 | 标题 |
|------|------|
| SW-01 | 游戏代码为什么容易腐化：技术债的成因、利息和真实代价 |
| SW-02 | 耦合与内聚：衡量代码质量的两把尺子 |
| SW-03 | 单一职责（SRP）：一个类只做一件事——怎么定义"一件事"，游戏中的违反案例 |
| SW-04 | 开闭原则（OCP）：对扩展开放，对修改封闭——技能系统、道具系统如何设计才不用改旧代码 |
| SW-05 | 里氏替换（LSP）：子类必须能替换父类——继承滥用的危害，游戏中的典型错误 |
| SW-06 | 接口隔离（ISP）：不强迫依赖不需要的接口——Component 设计的 ISP 视角 |
| SW-07 | 依赖倒置（DIP）：依赖抽象而非具体——这是 SOLID 中最重要的一条，是所有架构的根基 |
| SW-08 | 五条原则如何协同：一个从违反到修正的完整重构案例（以战斗系统为例） |
| SW-09 | SOLID 在引擎源码中的体现：从 Unity / Unreal 的架构设计理解这五个原则 |
| SW-10 | DRY / KISS / YAGNI：与 SOLID 互补，防止过度设计与代码重复 |
| SW-11 | Clean Code 基础：命名、函数长度、注释规范，让代码自解释 |
| SW-12 | Code Smell 识别：散弹式修改、上帝类、特性依恋——危险信号的诊断 |
| SW-13 | 重构手法：如何在测试保护下安全改善已有代码，不引入新 Bug |

---

## 系列七·B：游戏编程设计模式（7 篇）

*读引擎源码和写大型游戏系统的通用认知框架，与 AI 里的状态机不同，这是程序架构层的模式。*

| 编号 | 标题 |
|------|------|
| 模式-01 | 为什么游戏需要设计模式：游戏代码的特殊性（实时循环、状态爆炸、性能敏感） |
| 模式-02 | Command 模式：操作对象化，撤销/重做、技能队列、录像回放的实现基础 |
| 模式-03 | Observer / Event Bus：事件驱动解耦，与 Unity 的 UnityEvent / C# 委托的关系 |
| 模式-04 | Object Pool：池化原理、通用实现、Unity 内置 ObjectPool，何时用何时不用 |
| 模式-05 | Service Locator 与依赖注入：全局服务访问的两种方式，各自的权衡 |
| 模式-06 | State 模式 vs FSM vs 行为树：三种状态管理方式的适用边界 |
| 模式-07 | Data-Oriented Design 原则：从面向对象到面向数据，为什么 DOTS/ECS 这样设计 |

---

## 系列七·C：数据结构与算法（21 篇）

*游戏开发中真正用得上的算法与数据结构，从复杂度分析到空间索引，从内存分配器到 GC 原理。*

### 七·C·1 — 基础工具（2 篇）

| 编号 | 标题 |
|------|------|
| DS-01 | 算法复杂度实战：Big-O 不是理论，是选数据结构的判断依据 |
| DS-02 | 连续内存与缓存局部性：为什么数组比链表快，游戏里的数据布局实战 |

### 七·C·2 — 排序（2 篇）

| 编号 | 标题 |
|------|------|
| DS-03 | 排序算法选择：快排 / 归并 / 插入排序，游戏里如何根据场景选排序 |
| DS-04 | 渲染排序与 Z-order：不透明 / 透明物体排序，2D 层级排序，DrawCall 合批 |

### 七·C·3 — 图论与寻路（4 篇）

| 编号 | 标题 |
|------|------|
| DS-05 | 图论基础 + BFS / DFS：地图遍历、连通性检测、迷宫生成 |
| DS-06 | Dijkstra → A*：最短路径原理与启发函数 |
| DS-07 | A* 实现细节：Open/Closed 列表、启发函数选择、性能优化 |
| DS-08 | 拓扑排序：技能依赖树、资源加载顺序、任务调度 |

### 七·C·4 — 基础数据结构（4 篇）

| 编号 | 标题 |
|------|------|
| DS-09 | 二叉堆与优先队列：A* 的底层结构，伤害优先级，Skill Queue |
| DS-10 | 哈希表深度：碰撞处理、开放寻址、负载因子，自己实现一个 |
| DS-11 | LRU Cache：双向链表 + 哈希表，资源缓存、Chunk 缓存的实现 |
| DS-12 | 环形缓冲区与双缓冲：输入缓冲、渲染命令缓冲、网络包缓冲 |

### 七·C·5 — 空间数据结构与碰撞（6 篇）

| 编号 | 标题 |
|------|------|
| DS-13 | AABB 与碰撞宽相：Sweep & Prune，空间数据结构的入口 |
| DS-14 | 四叉树与八叉树：空间查询、场景管理、视锥剔除 |
| DS-15 | BVH：层次包围体，渲染遮挡剔除与光线投射 |
| DS-16 | 空间哈希：均匀网格，O(1) 近邻查询，移动端轻量方案 |
| DS-17 | BSP 树：室内场景分割，可见性判断，PVS 计算 |
| DS-18 | SAT 与 GJK：窄相碰撞检测原理，凸体碰撞的数学基础 |

### 七·C·6 — 内存管理与过程生成（3 篇）

| 编号 | 标题 |
|------|------|
| DS-19 | 内存分配器：线性、栈、池、空闲链表分配器的实现与对比 |
| DS-20 | GC 算法：标记清除、分代 GC、三色标记、Unity 增量 GC 与 Unreal UObject GC |
| DS-21 | 程序化噪声：Perlin / Simplex / Worley，地形生成与纹理合成 |

---

## 系列八：打包、加载与流式系统（10 篇）

*从资产打包策略到运行时加载调度，覆盖完整的资源管线下半段。*

### 八·A — 打包策略（4 篇）

| 编号 | 标题 |
|------|------|
| 打包-01 | Asset Bundle 打包粒度：粗粒度 vs 细粒度，依赖关系与冗余控制 |
| 打包-02 | 首包体积优化：分包策略、按需下载、差量更新（热更 patch）原理 |
| 打包-03 | 资源压缩方案：LZ4 vs LZMA，解压速度与包体大小的权衡 |
| 打包-04 | Addressables / YooAsset 打包配置：Group 划分、Bundle 布局最佳实践 |

### 八·B — 加载优化（4 篇）

| 编号 | 标题 |
|------|------|
| 加载-01 | 异步加载管线：AsyncOperation / UniTask / 协程在加载中的正确用法 |
| 加载-02 | 加载优先级调度：关键路径资源优先、后台预加载、加载进度反馈 |
| 加载-03 | 加载时的内存控制：缓存池、引用计数、卸载时机，避免内存尖峰 |
| 加载-04 | 场景加载优化：Loading Screen 设计、帧时间分摊、分帧实例化 |

### 八·C — 开放世界流式加载（2 篇）

| 编号 | 标题 |
|------|------|
| 流式-01 | Level Streaming 基础：Scene 异步加载/卸载、触发区域设计 |
| 流式-02 | Unreal World Partition：新一代大世界管理，HLOD 自动生成 |

---

## 系列九：UI/UX 系统（6 篇）

| 编号 | 标题 |
|------|------|
| UI-01 | UI Toolkit 架构：VisualElement、USS、UXML，与 UGUI 的本质区别 |
| UI-02 | Unreal UMG：Widget 层级、动画、数据绑定原理 |
| UI-03 | 多分辨率适配：锚点系统、安全区（刘海屏/挖孔屏）、DPI 缩放 |
| UI-04 | UI 性能深度：重建触发条件、动态 vs 静态 Canvas、DrawCall 合并 |
| UI-05 | UI 动画系统：Tween、状态机驱动、过渡效果实现 |
| UI-06 | SDF 字体渲染：TextMeshPro 原理，为什么比普通 Text 清晰 |

---

## 系列十：美术与程序协作桥梁（9 篇）

*程序理解美术流程，才能制定合理规范、快速排查问题。*

### 十·A — 规范与流程（4 篇）

| 编号 | 标题 |
|------|------|
| 美术协作-01 | 美术资产规范制定：面数预算、贴图尺寸、LOD 层级标准的依据 |
| 美术协作-02 | DCC 工具导出流程：Maya/Blender → FBX → Unity/Unreal，坐标系差异、命名规范 |
| 美术协作-03 | PBR 贴图工作流：Albedo/Normal/Metallic/Roughness，Substance Painter 输出配置 |
| 美术协作-04 | Lightmap 烘焙协作：UV2 展开规范、接缝处理、烘焙参数与程序配置 |

### 十·B — 资产制作技术（3 篇）

| 编号 | 标题 |
|------|------|
| 美术协作-05 | 法线烘焙：高模烘低模的完整流程，切线空间 vs 世界空间法线的选择 |
| 美术协作-06 | Shader 参数暴露：哪些参数给美术调，Material 命名与管理规范 |
| 美术协作-07 | 骨骼与绑定规范：骨骼命名、蒙皮权重要求，对 GPU 蒙皮的影响 |

### 十·D — DCC 工具链（5 篇）

*程序理解美术工具的定位，才能制定合理的交付规范与管线。*

| 编号 | 标题 |
|------|------|
| DCC-01 | 3D 建模软件定位：Maya / Blender / 3ds Max 的适用场景与游戏项目选型 |
| DCC-02 | 贴图材质工具：Substance Painter（手绘贴图）/ Substance Designer（程序化材质）/ Marmoset Toolbag（预览验证）职责区别 |
| DCC-03 | 动画制作工具：Maya 动画 / MotionBuilder（动捕处理）/ Cascadeur（物理辅助），导出到引擎的规范 |
| DCC-04 | 特效工具：Houdini 特效导出到 Unity/Unreal 的流程，VFX Graph 与 Houdini 的分工边界 |
| DCC-05 | 2D 工具链：Photoshop / Spine / DragonBones，2D 动画与 UI 素材的导出规范 |

### 十·E — 资源规范制定方法论（3 篇）

| 编号 | 标题 |
|------|------|
| 规范-01 | 从目标帧率倒推资产预算：Draw Call / 面数 / 贴图内存的预算制定方法 |
| 规范-02 | 平台差异对规范的影响：同一项目 iOS / Android / PC 的差异化规范策略 |
| 规范-03 | 规范落地与执行：工具化检查（自动化资产审查脚本）、规范文档模板 |

| 编号 | 标题 |
|------|------|
| 美术协作-08 | 美术常见渲染问题定位：Z-fighting、UV 接缝、法线翻转、漏光的成因与修复 |
| 美术协作-09 | 美术资产性能问题排查：overdraw 定位、贴图内存超标、Draw Call 超标的排查流程 |

---

## 系列十一：Unreal Engine 架构与系统

### 十一·A — Unreal 引擎架构解读（7 篇）

| 编号 | 标题 |
|------|------|
| UE-01 | Unreal 对象系统：UObject / UClass / CDO |
| UE-02 | 反射与序列化：UPROPERTY / UFUNCTION 的工作机制 |
| UE-03 | Unreal GC：Mark-and-Sweep 与对象生命周期 |
| UE-04 | Unreal 渲染架构：RHI 抽象层与 RDG（Render Dependency Graph） |
| UE-05 | GameThread / RenderThread / RHIThread 三线程模型 |
| UE-06 | Blueprint VM：蓝图字节码的执行原理 |
| UE-07 | Unreal 模块系统与构建工具 |

### 十一·B — Gameplay Ability System（GAS）（8 篇）

| 编号 | 标题 |
|------|------|
| GAS-01 | GAS 总览：为什么需要 GAS，它解决了什么问题 |
| GAS-02 | AbilitySystemComponent（ASC）：角色的能力中枢 |
| GAS-03 | Gameplay Attributes 与 AttributeSet：数值系统设计 |
| GAS-04 | Gameplay Effects（GE）：数值修改、持续效果、堆叠逻辑 |
| GAS-05 | Gameplay Abilities（GA）：能力的生命周期与任务系统 |
| GAS-06 | Gameplay Tags：标签系统在 GAS 中的核心作用 |
| GAS-07 | Gameplay Cues：表现层（特效/音效）与逻辑层的解耦 |
| GAS-08 | GAS 网络同步：预测、回滚与 Ability 的服务器验证 |

### 十一·C — Unreal 网络系统（6 篇）

| 编号 | 标题 |
|------|------|
| UE网络-01 | Unreal 网络架构：NetDriver、NetConnection、Channel 层级 |
| UE网络-02 | Actor 复制：Replication Graph、相关性判断、优先级 |
| UE网络-03 | 属性同步：RepNotify、条件复制、带宽优化 |
| UE网络-04 | RPC：Reliable / Unreliable，Server / Client / Multicast 的选择 |
| UE网络-05 | 客户端预测：移动组件的预测回滚实现原理 |
| UE网络-06 | Dedicated Server 配置、打包与部署 |

---

## 系列十二：Unity 网络方案（4 篇）

| 编号 | 标题 |
|------|------|
| Unity网络-01 | Netcode for GameObjects：架构与基本用法 |
| Unity网络-02 | 属性同步与 RPC：NetworkVariable、ServerRpc / ClientRpc |
| Unity网络-03 | Mirror：社区方案与 NGO 的架构对比 |
| Unity网络-04 | Photon / 第三方托管方案：适用场景与接入成本 |

---

## 系列十三：游戏后端基础

### 十三·A — 数据库基础（5 篇）

| 编号 | 标题 |
|------|------|
| DB-01 | 关系型 vs 非关系型：MySQL / PostgreSQL vs MongoDB，游戏数据的选择依据 |
| DB-02 | 游戏数据库设计：玩家数据、背包、排行榜的表结构设计 |
| DB-03 | 索引与查询优化：为什么慢，怎么看执行计划 |
| DB-04 | 事务与并发：ACID、锁、为什么扣道具要用事务 |
| DB-05 | 数据库分库分表：水平拆分、垂直拆分，游戏场景的拆分策略 |

### 十三·B — 缓存系统（4 篇）

| 编号 | 标题 |
|------|------|
| 缓存-01 | Redis 基础：数据结构（String / Hash / List / ZSet）与游戏场景对应 |
| 缓存-02 | 缓存与数据库的一致性：Cache-Aside、Write-Through、常见踩坑 |
| 缓存-03 | 排行榜、Session、匹配队列：Redis 在游戏后端的典型用法 |
| 缓存-04 | 缓存穿透、击穿、雪崩：原理与防护方案 |

### 十三·C — 后端架构（6 篇）

| 编号 | 标题 |
|------|------|
| 后端-01 | 游戏后端架构总览：网关、逻辑服、数据服、推送服的职责划分 |
| 后端-02 | 消息队列：Kafka / RabbitMQ 在游戏事件系统中的作用 |
| 后端-03 | 微服务 vs 单体：游戏后端的拆分粒度权衡 |
| 后端-04 | 负载均衡与服务发现：多服务器部署的基础设施 |
| 后端-05 | 游戏服务器编排：Agones / 自建方案，服务器池的动态管理 |
| 后端-06 | CDN 与资源分发：热更新包、Asset Bundle 的分发策略 |

### 十三·D — Dedicated Server 实践（5 篇）

| 编号 | 标题 |
|------|------|
| DS-01 | Dedicated Server 架构：无头模式、服务器与客户端的代码共享边界 |
| DS-02 | Unity Dedicated Server：构建配置、启动参数、服务器专属逻辑 |
| DS-03 | Unreal Dedicated Server：Cook、打包、启动流程 |
| DS-04 | 服务器性能优化：Tick 率、物理精简、AI 关闭、渲染关闭 |
| DS-05 | 容器化部署：Docker 打包游戏服务器，Kubernetes 动态扩缩容 |

### 十三·E — 后端安全（4 篇）

| 编号 | 标题 |
|------|------|
| 后端安全-01 | 游戏后端的安全威胁模型：常见攻击面（接口刷取、数据篡改、账号劫持）|
| 后端安全-02 | 接口安全：JWT/OAuth 鉴权、接口签名验证、防重放攻击 |
| 后端安全-03 | 数据安全：SQL 注入防护、敏感数据加密存储、GDPR 基本合规 |
| 后端安全-04 | DDoS 防御与限流：Rate Limiting、IP 封禁、CDN 防护层设计 |

### 十三·F — Live Service / GaaS 基础设施（4 篇）

| 编号 | 标题 |
|------|------|
| GaaS-01 | 远程配置（Remote Config）：Feature Flag、参数热更新、A/B 测试基础设施 |
| GaaS-02 | 赛季与活动系统：时间窗口内容、Battle Pass 数据模型、活动服务器压力 |
| GaaS-03 | 玩家行为分析：埋点设计规范、漏斗分析、留存率计算 |
| GaaS-04 | 游戏运营告警：关键指标监控（DAU、崩溃率、付费率）、告警阈值设计 |

---

## 系列十四：引擎架构与自研引擎基础

### 十四·A — 引擎子系统基础（10 篇）

| 编号 | 标题 |
|------|------|
| 引擎-01 | 一个最小引擎需要哪些子系统 |
| 引擎-02 | 场景图与 ECS：两种对象管理哲学的权衡 |
| 引擎-03 | 线程模型与同步：游戏/渲染/IO/物理线程的职责与数据交换 |
| 引擎-04 | 反射系统的实现：运行时类型信息怎么做 |
| 引擎-05 | 事件系统：Observer / 消息总线 / 信号槽 |
| 引擎-06 | 物理系统基础：PhysX / Bullet 架构，物理与渲染的 Transform 同步 |
| 引擎-07 | 物理系统深度：Character Controller 设计、布娃娃（Ragdoll）、布料模拟 |
| 引擎-08 | 音频系统基础：采样、混音、空间化，FMOD / Wwise 解决了什么 |
| 引擎-09 | FMOD 完整集成：事件系统、参数驱动、快照、与引擎的生命周期绑定 |
| 引擎-10 | 空间音频：HRTF、Ambisonics 原理，音频性能优化与 Bus 混音架构 |

### 十四·B — 渲染架构设计（5 篇）

| 编号 | 标题 |
|------|------|
| 渲染架构-01 | 渲染抽象层（RHI）设计：如何隔离 Vulkan / Metal / DX12 |
| 渲染架构-02 | 帧资源管理：多帧 In-Flight，资源生命周期 |
| 渲染架构-03 | 资产管线：FBX / glTF 导入，纹理处理，运行时格式设计 |
| 渲染架构-04 | 编辑器与运行时分离：序列化、场景格式、热重载 |
| 渲染架构-05 | 自研引擎调试工具：内置 Profiler、Log 系统、Overlay 设计 |

### 十四·C — 脚本系统集成（3 篇）

| 编号 | 标题 |
|------|------|
| 脚本-01 | 脚本系统设计：为什么游戏引擎需要内嵌脚本语言 |
| 脚本-02 | Lua / Python 嵌入 C++ 引擎：绑定系统，C++ 对象暴露给脚本层 |
| 脚本-03 | 脚本 GC 与 C++ 内存的边界：所有权、生命周期管理 |

### 十四·D — 构建与工程（6 篇）

| 编号 | 标题 |
|------|------|
| 工程-01 | 构建系统：CMake / Premake，预编译头，跨平台工具链 |
| 工程-02 | 渲染正确性测试：Golden Image 对比，自动化回归测试 |
| 工程-03 | Git 工作流实践：游戏项目分支策略（Trunk-Based vs Feature Branch）、二进制冲突处理 |
| 工程-04 | Perforce 工作流：Stream 结构、Workspace、Changelist 管理，与 Git 的选型比较 |
| 工程-05 | 跨平台开发策略：平台抽象层设计、条件编译组织（#if 管理）、多平台测试策略 |
| 工程-06 | 大型项目代码组织：Assembly Definition、Package 化拆分、模块间依赖管理、Monorepo 策略 |

### 十四·E — Unreal 编辑器扩展（4 篇）

| 编号 | 标题 |
|------|------|
| UE编辑器-01 | Unreal 编辑器模块：Editor Module 与运行时模块的分离，编辑器专属代码的组织 |
| UE编辑器-02 | 自定义资产类型：AssetTypeActions、自定义导入流程、资产缩略图 |
| UE编辑器-03 | Detail 面板定制：IDetailCustomization、属性可见性控制、自定义编辑控件 |
| UE编辑器-04 | 编辑器工具蓝图与 Slate：EditorUtilityWidget、Slate UI 框架基础 |

---

## 系列十五：本地化与多语言（5 篇）

| 编号 | 标题 |
|------|------|
| 本地化-01 | Unity Localization 包：字符串表、资产本地化、运行时切换 |
| 本地化-02 | SDF 字体与 TextMeshPro：多语言字符集处理，动态字体 vs 静态图集 |
| 本地化-03 | CJK 与复杂文字：中日韩排版规则，RTL（阿拉伯语/希伯来语）布局 |
| 本地化-04 | 音频本地化：多语言配音管理，运行时动态加载 |
| 本地化-05 | 本地化资产管线：不同地区的贴图/视频/UI 替换策略 |

---

## 系列十五·B：无障碍与 Accessibility（3 篇）

| 编号 | 标题 |
|------|------|
| 无障碍-01 | 色盲模式：色盲类型分析，后处理滤镜实现，UI 颜色规范 |
| 无障碍-02 | UI 可访问性：字体最小尺寸、对比度标准、焦点导航（手柄/键盘支持）|
| 无障碍-03 | 听觉与运动障碍支持：字幕系统设计、按键重绑定、辅助输入方案 |

---

## 系列十六：安全与反外挂（4 篇）

| 编号 | 标题 |
|------|------|
| 安全-01 | 客户端安全威胁模型：内存修改、速度外挂、资产破解的原理 |
| 安全-02 | 代码保护：IL2CPP 混淆、字符串加密、关键逻辑服务器化 |
| 安全-03 | 资产加密：Asset Bundle 加密方案，防止资产提取 |
| 安全-04 | 运行时检测：完整性校验、异常行为监测、封号系统设计思路 |

---

## 系列十七：CI/CD 与工程质量（8 篇）

| 编号 | 标题 |
|------|------|
| CI-01 | 游戏项目自动化构建：Jenkins / GitHub Actions 打包流水线 |
| CI-02 | 自动化测试：Unity Test Framework，Play Mode 测试，性能回归测试 |
| CI-03 | 包体大小监控：资产分析、Bundle 大小告警、贡献度追踪 |
| CI-04 | 包体大小优化：IL2CPP Managed Stripping、Split APK/AAB、iOS On-Demand Resources、资产精简策略 |
| CI-05 | Crash 上报与分析：Firebase Crashlytics / Bugly 接入，崩溃归因 |
| CI-06 | 游戏分析接入：埋点 SDK 集成、事件上报规范、隐私合规（GDPR/COPPA） |
| CI-07 | 主机平台认证流程：TRC / XR 认证要求概览，常见不通过原因 |
| CI-08 | 游戏内调试工具系统：控制台命令（Console System）、Debug Overlay、开发者菜单的规范实现 |

---

## Unity Android 发布与包体 (6)

*把 Google Play 规则、AAB / PAD、Unity 打包设置、base module 边界和超限排查串成一条可执行的工程链路*

| 编号 | 标题 |
|------|------|
| AAB-index | Unity Android AAB / PAD 系列索引：从 APK 心智切到 base module 与 asset packs |
| AAB-01 | 从 APK + OBB 到 AAB + PAD：Google Play 为什么改了发布模型 |
| AAB-02 | Play Console 报 base 超 200MB，到底在算什么 |
| AAB-03 | Unity 里哪些设置真的决定 base module 和 asset packs |
| AAB-04 | 为什么你的 Unity 资源还在进 base：首场景、StreamingAssets、so 和 ABI |
| AAB-05 | Unity AAB 超限实战：base > 200MB 时怎么排、怎么改、怎么做 CI |

---

## 系列十八：DOTS 专题（5 篇）

| 编号 | 标题 |
|------|------|
| DOTS-01 | DOTS 概览：ECS / Jobs / Burst / Collections 各自解决什么 |
| DOTS-02 | IJobParallelForTransform：并行 Transform 更新原理与写法 |
| DOTS-03 | NativeArray 与内存管理：为什么不用 List，Dispose 时机 |
| DOTS-04 | [BurstCompile]：什么代码能编译，限制是什么 |
| DOTS-05 | 案例：飘字系统拆解——Jobs 并行更新 + GPU Instancing 批渲染 |

---

## 系列十九：项目常用插件与框架（5 篇）

| 编号 | 标题 |
|------|------|
| 插件-01 | YooAsset：资源包管理、热更新资源加载流程 |
| 插件-02 | HybridCLR：C# 热更新原理，与传统 Lua 热更的区别 |
| 插件-03 | TEngine 框架：模块分层与使用方式 |
| 插件-04 | DOTween：补间动画的性能边界与最佳实践 |
| 插件-05 | UniTask：async/await 在 Unity 中的正确用法 |

---

---

## 系列二十一：质量保证体系（14 篇）

*覆盖从开发阶段的单元测试到上线后的灰度监控，构建完整的游戏质量控制闭环。*

### 二十一·A — 测试策略与自动化测试（5 篇）

| 编号 | 标题 |
|------|------|
| QA-01 | 游戏质量体系概述：测试金字塔、质量门控点、QA 在开发流程中的位置 |
| QA-02 | 单元测试基础：游戏代码的可测试性设计，如何解耦才能写测试 |
| QA-03 | Unity 自动化测试：EditMode / PlayMode / 异步测试完整写法，NUnit 在游戏中的使用 |
| QA-04 | Unreal 自动化测试：Automation System，功能测试框架，截图比对 |
| QA-05 | 数值与战斗系统测试：技能数值、公式回归、边界条件的自动化验证方案 |

### 二十一·B — 性能与兼容性质量（4 篇）

| 编号 | 标题 |
|------|------|
| QA-06 | 性能基准（Baseline）系统：FPS / 内存 / 加载时间的合格线制定与 CI 回归检测 |
| QA-07 | 兼容性测试策略：设备矩阵划定优先级、Device Farm（AWS / Firebase Test Lab）、驱动差异检测 |
| QA-08 | 视觉质量测试：感知差异（Perceptual Diff）、渲染 Artifact 自动检测、多平台视觉一致性 |
| QA-09 | 代码与资产质量工具：静态分析（Roslyn Analyzer / Clang-Tidy）、资产导入时自动规范校验 |

### 二十一·C — 发布流程与线上质量（5 篇）

| 编号 | 标题 |
|------|------|
| QA-10 | 质量门控（Quality Gate）：发布前必须通过的检查清单，阻断合并的自动化卡点 |
| QA-11 | 灰度发布：分批放量策略（1% → 10% → 全量）、关键指标监控窗口、自动回滚触发条件 |
| QA-12 | 热修复流程：紧急修复的快速验证通道、Cherry-Pick 策略、回滚预案与演练 |
| QA-13 | Bug 管理与 Playtest：P0/P1/P2 严重级别定义、Bug 生命周期、结构化测试用例设计 |
| QA-14 | 线上质量监控：崩溃率 / ANR / 卡顿的实时监控告警、版本健康看板、问题响应 SLA |

---

## 系列二十：Unity 源码解读（待定）

### 保密原则

- **不出现** C++ 的文件名、类名、方法名、枚举值等任何内部标识符
- **不直接引用**专有 C++ 引擎代码，所有示例重写为示意代码
- **可以引用** URP/HDRP 包代码（MIT 开源）
- 文章**不注明源码路径**，不暴露内部目录结构

### 方向（具体篇目待源码分析后确定）

| 方向 | 说明 |
|------|------|
| 渲染循环 | Unity 每帧如何驱动渲染线程，CommandBuffer 如何被消费 |
| Culling 实现 | Frustum Culling 在引擎层的数据结构 |
| Batching 实现 | Static / Dynamic Batching 的内部合并逻辑 |
| 资源管理 | Texture / Mesh 上传时机、显存管理策略 |
| 动画系统 | Animator 状态机内部驱动机制 |
| 物理与渲染边界 | PhysX 结果如何同步到 Transform，再到渲染 |

---

## 阅读依赖关系

```
系列零（历史背景）
    ↓
系列一·A（数学）  系列一·B（C++）  系列一·D（网络基础）
    ↓                  ↓                  ↓
系列二（Unity渲染）  系列一·C（图形API）  系列十二（Unity网络）
    ↓                  ↓                  ↓
系列三（移动端 GPU+CPU 优化）           系列十三（游戏后端）
    ↓
系列四（Shader技法+游戏效果）
系列五（动画系统）
系列六（AI与逻辑系统）
系列七（游戏核心系统）
系列八（开放世界）
系列九（UI/UX）
    ↓
系列十（美术协作桥梁）
系列十一·A（Unreal架构）→ 十一·B（GAS）→ 十一·C（Unreal网络）
    ↓
系列十四（引擎架构 → 自研引擎）
    ↓
系列十五（本地化）  系列十六（安全）  系列十七（CI/CD）  Unity Android 发布与包体
系列十八（DOTS）   系列十九（插件）  系列二十（Unity源码）
```

---

## 总体统计

| 系列 | 篇数 |
|------|------|
| 系列零（背景历史）| 15 |
| 系列一（底层基础：数学 + C++ + 图形API + IO存储 + 网络）| 33 |
| 系列二（Unity 渲染）| 37 |
| 系列三（移动端硬件 + GPU/CPU 优化 + 工具）| 22 |
| 系列四（Shader 技法 + 游戏效果）| 51 |
| 系列五（动画系统）| 7 |
| 系列六（AI 与游戏逻辑）| 7 |
| 系列七（游戏核心系统）| 8 |
| 系列七·A（软件工程基础与 SOLID 原则）| 13 |
| 系列七·B（游戏编程设计模式）| 7 |
| 系列七·C（数据结构与算法）| 21 |
| 系列八（打包、加载与流式系统）| 10 |
| 系列九（UI/UX 系统）| 6 |
| 系列十（美术协作桥梁）| 17 |
| 系列十一（Unreal 架构 + GAS + 网络 + 编辑器扩展）| 25 |
| 系列十二（Unity 网络）| 4 |
| 系列十三（游戏后端 + 后端安全 + Live Service）| 28 |
| 系列十四（引擎架构：子系统 + 渲染架构 + 脚本 + 工程 + UE编辑器）| 29 |
| 系列十五（本地化）| 5 |
| 系列十五·B（Accessibility）| 3 |
| 系列十六（安全与反外挂）| 4 |
| 系列十七（CI/CD 与工程质量）| 8 |
| Unity Android 发布与包体 | 6 |
| 系列十八（DOTS）| 5 |
| 系列十九（插件框架）| 5 |
| 系列二十一（质量保证体系）| 14 |
| 系列二十（Unity 源码）| 待定 |
| **合计** | **约 390 篇（不含系列二十）** |

---

*最后更新：2026-03-24（v10）*
