# Piccolo 源码阅读与调试：系列规划

> 本文件是 Piccolo 系列的唯一 canonical 篇级目录。根 `doc-plan.md` 只维护系列路由；阅读入口为 `content/reading/piccolo-engine.md`。

## 定位与读者

面向具备客户端或 C++ 编程经验、熟悉调试器并希望读懂一台陌生引擎的开发者。先建立架构地图，再沿启动、一帧、对象、资源和渲染链路逐步下钻；遇到语言或图形概念卡点时补充对应前置，不要求预先学完整套基础。每篇围绕一个问题组织源码入口、关键状态和可执行的验证任务。

源码基线：公开仓库 `BoomingTech/Piccolo`，提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。后续采用其他提交时逐篇声明。

## 边界

- 本系列负责 Piccolo 的具体实现与阅读、调试方法；所需通用原理可用引擎无关模型解释，系统化通用篇由相应专题维护。
- 通用架构、C++、构建调试和图形基础复用已有专题；以问题需要为准补前置。
- 保留阅读首页的六组导航：工程启动、一帧协作、场景对象、资源元信息、渲染、编辑器与验证。篇级状态只在本文件维护。
- 固定源码事实、作者的工程判断和实际运行观察分别表述。
- 不将源码核对记成构建、断点或图形捕获完成；不扩写成完整自研引擎教程。
- 与“引擎源码阅读到自研引擎”专题并列衔接：本系列以具体引擎的可验证理解为目标，后者负责跨引擎认知与自研取舍。

## 渲染主线的推进顺序

2026-10-25 按用户最新要求，将本专栏主线限定为 **引擎无关的原理与设计，以及 Piccolo 的具体实现**。文章按问题选择定位，不要求每篇都包含源码，也不要求每个原理都先写一篇 Piccolo 案例。公共 C++ 基础继续按需补充。

| 文章定位 | 核心问题与产物 | 当前安排 |
| --- | --- | --- |
| 引擎无关的原理与设计 | 解释问题、职责、输入输出、数据契约、生命周期和取舍；用自有模型或最小程序说明 | 由 [渲染栈系列规划](game-engine-rendering-stack-series-plan.md) 管理通用篇，引用案例时明确实现边界 |
| Piccolo 源码分析 | 在固定版本追场景、程序、资源、Pass、提交和呈现，解释它对通用问题的具体选择 | 由本文件管理源码篇。已有 GFX-01、GFX-02、06—07 与 PCPP-3 提供入口，构建与运行证据接 01 |

当前围绕以下问题推进，分别选择通用原理篇或 Piccolo 源码篇：

1. **程序和资源接口**：以 GFX-02 已验证的 GLSL 编译产物为起点，继续核对 CPU 数据、布局、绑定与实际 Draw 输入。GLSL/Vulkan 属于该案例的具体环境；通用篇解释需要匹配的接口契约，语言对照按选题需要另行安排。
2. **Pass 和图像依赖**：复用 GFX-01 与 07 的 G-buffer、光照和合成路径，追输入输出与生命周期；不能提前把这套实现称为完整通用 RenderGraph 设计。
3. **提交和完成边界**：复用 07 与 PCPP-3，分别记录 CPU 交接、GPU 同步和呈现观察；独立 Shader 编译不充当运行闭环。

通用篇可以独立论证；从 Piccolo 提炼设计时，至少说明输入、输出、持有者、调用/数据路径和失败边界，并区分通用约束与项目选择。API、平台或算法的特定前提在结论旁说明；没有运行证据的部分保留为源码分析或待验证假设。其他引擎的既有专题保留原有归属，本专栏不再把它们列为后续必经阶段。

原理篇按“具体问题 → 最小模型 → 示例与取舍”组织；Piccolo 篇按“具体问题 → 必要概念 → 固定源码路径 → 状态推导与验证”组织。概念图、CPU 调用顺序和线程/GPU 执行关系分别表达，标题和图注说明当前讨论层次。

## 公共基础之后的专项前置

2026-10-25 起采用“[C++ 公共基础](cpp-foundations-series-plan.md) → Piccolo 专项前置 → 本系列主线”。公共 CXX 编号与本系列 CPP-01～03 分开；原文章、URL 和系列归属保留。

公共前置优先 CXX-01～03、04、07～08、10，涉及回调与缓冲访问时补 CXX-05～06。公共 CXX-01～11 正文已齐备；UFS 首课保留综合练习，三篇 Piccolo C++ 材料继续承担项目案例。

现有 CPP 三篇继续负责 Piccolo 的持有图、Component/RHI 分派和反射生成实例。公共课独立成稿后，原文压缩重复语言解释并回链，保留项目特有协议、固定源码证据和练习；本轮不迁移正文。

| 补充次序 | 专项拟题 / 工作 | 状态与现有材料 | 依赖与完成标准 |
|---|---|---|---|
| PCPP-1 | [对象容器修改后，持有者、借用与遍历怎样变化](../content/engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md) | 已写：源码、独立 C++17 程序、Hugo 与生成页静态检查通过；引擎运行及浏览器目视待验证 | CXX-02、04；在固定源码核对保存/查找/擦除，区分容器元素与所指对象寿命，补正常/失效边界练习 |
| PCPP-2 | [窗口与编辑器回调怎样绑定、调用和注销](../content/engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md) | 已写：源码、独立 C++17 程序、Hugo 与生成页静态检查通过；引擎事件/退出及浏览器目视待验证 | CXX-05；追捕获或上下文对象、调用时机、退出解绑及有效访问条件 |
| PCPP-3 | [状态缓冲怎样交接，哪些地方真的跨线程](../content/engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md) | 已写：源码、独立 C++17 缓冲/线程程序、Hugo 与生成页静态检查通过；引擎线程采样/GPU 捕获及浏览器目视待验证 | CXX-04、06、11；以实际任务/线程与同步证据核对，不能由双缓冲推断并行；CPU 与 GPU 同步分开 |

专项顺序为 PCPP-1 → 2 → 3。PCPP-1/2/3 专项正文已齐备，加上下方 GFX-02 渲染补充和主线 09/10/11/12/13/14/15/16/17/18，当前共 27 篇。公共 CXX-04/05/06/11 均已成稿，专项回链公共规则，再核对项目协议；已有构建、生成和引擎运行状态沿下表保留。

## 首批目录与推荐顺序

| 编号 | 主问题 / 拟题 | 状态 | 正文 |
| --- | --- | --- | --- |
| 00 | 架构地图：编辑器、世界、资源与渲染怎样协作 | 已写：源码核对、站点构建和图形检查通过，运行待验证 | [正文](../content/system-design/piccolo-engine-00-architecture-map.md) |
| 01 | 建立构建与调试基线：源码、生成文件、配置与程序怎样对应 | 已写：配置、反射/序列化与 shader 生成实测通过，站点检查通过；完整 Debug 编译被第三方兼容问题阻塞，运行与断点待验证 | [正文](../content/engine-toolchain/piccolo-engine-01-build-debug-baseline.md) |
| 02 | 从启动走到第一帧：初始化、世界加载与退出怎样衔接 | 已写：源码、站点构建与页面检查通过；引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-02-startup-first-frame.md) |
| 03 | 一帧怎样推进：调用顺序、状态交接与线程怎样核对 | 已写：源码、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md) |
| 04 | 场景文件怎样成为世界中的对象与组件 | 已写：源码、资源、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-04-scene-to-objects-components.md) |
| 05 | 资源、类型信息与序列化怎样共同恢复对象 | 已写：源码、模板、站点构建与生成页静态检查通过；01 已核对实际生成片段，浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-05-resource-type-serialization.md) |
| 06 | 改一个位置，怎样沿逻辑数据追到渲染表示 | 已写：源码、资源数值、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-06-position-to-render-representation.md) |
| 07 | 从渲染场景进入 Pass 与 Vulkan 提交 | 已写：源码、图形接口语义、站点构建与生成页静态检查通过；浏览器目视、引擎运行及图形捕获待验证 | [正文](../content/engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md) |
| 08 | 编辑器怎样修改、保存并恢复场景状态 | 已写：源码、资源、模板、站点构建与生成页静态检查通过；实际保存/重载、浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-08-editor-save-reload.md) |

编号代表当前推荐顺序。后续拟题在取材时细化，不代表已有正文或实验。

## 后续主线 09—18

按用户确认的选题保留编号。已有输入运动、刚体查询、相机、GPU 资源、着色/后处理与编辑器对象操作；12 接通片段采样、骨骼姿态和主相机蒙皮；13 接通发射器、GPU 模拟与后续绘制。14 接通每组件 Lua 状态、脚本桥接、反射字段与方法的消费边界。文章编号和阅读顺序不代表运行时串行流水线。

| 编号 | 主问题 / 拟题 | 状态与边界 |
| --- | --- | --- |
| 09 | [从一次按键到角色位移：输入、运动组件与控制器怎样协作](../content/engine-toolchain/piccolo-engine-09-input-motor-character-movement.md) | 已写：固定源码与 Player/关卡资源、原输入/运动/控制器函数的 C++ 适配及原数学实现检查、Hugo 与生成页静态检查通过；真实输入、Jolt 和角色/GPU 运行待验证。核对 jump 寿命、加减速/方向、终点重叠、Z 平面条件及组件/位置缓冲时序；物理内部留给 10 |
| 10 | [刚体与碰撞查询怎样参与角色运动](../content/engine-toolchain/piccolo-engine-10-rigidbody-collision-character-motion.md) | 已写：固定项目/随附 Jolt 源码、实际资源与原形状转换/步进/更新函数的 C++ 记录适配、原数学检查、Hugo 与生成页静态检查通过；完整引擎、Jolt 碰撞和角色/GPU 运行待验证。区分 Static 创建、临时胶囊查询、模拟层/查询过滤、固定每 tick 步长、缩放重建与延迟删除 |
| 11 | [相机怎样跟随角色，并把视角交给渲染系统](../content/engine-toolchain/piccolo-engine-11-camera-follow-render-view.md) | 已写：源码、独立数学/RenderCamera 数值检查、Hugo 与生成页静态检查通过；实际跟随、断点与 GPU 捕获待验证 |
| 12 | [一段动画怎样成为骨骼姿态与网格蒙皮](../content/engine-toolchain/piccolo-engine-12-animation-skeleton-mesh-skinning.md) | 已写：固定源码、真实资源结构/编号检查、原 Skeleton/Node/数学的独立 JSON 桥接与两骨骼/权重条件检查、四份顶点 Shader 编译、Hugo 与生成页静态检查通过；引擎动画与 GPU 蒙皮待验证。明确 clip0/固定权重、dirty 发布、palette 槽0与索引契约、主相机/阴影/拾取消费差异 |
| 13 | [粒子发射器怎样进入 GPU 模拟与绘制](../content/engine-toolchain/piccolo-engine-13-particle-emitter-gpu-simulation.md) | 已写：固定源码与实际粒子资源、原类型布局/字节及独立串行状态检查、五份 Shader 编译、Hugo 与生成页静态检查通过；引擎/GPU 粒子待验证。明确批次 ID 协议、双存活表/空闲池、先绘制后模拟、计数回读、发射间隔契约与屏幕空间碰撞边界 |
| 14 | [Lua 脚本怎样读写组件并调用引擎方法](../content/engine-toolchain/piccolo-engine-14-lua-component-reflection.md) | 已写：固定源码、真实 Player 脚本、随附 Lua/sol2 与原 LuaComponent/反射的独立桩宿主检查、Hugo 与生成页静态检查通过；完整引擎脚本/角色运行待验证。明确 base 库、逐 tick 文本执行、点分字段 owner/leaf、类型/弱引用/错误边界、无参方法与嵌套分支缺口、直接赋值及 Motor 消费时序 |
| 15 | [网格与纹理怎样从文件成为 GPU 资源](../content/engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md) | 已写：源码、实际 OBJ/TGA 的独立 CPU 检查、资源键/身份与字节检查、Hugo 和生成页静态检查通过；Vulkan 上传、GPU 采样与引擎运行待验证。区分材质与纹理缓存、等待与释放边界 |
| 16 | [材质、光照与阴影怎样共同决定表面颜色](../content/engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md) | 已写：固定源码、四个 GLSL 独立编译、原 BRDF 文本的 C++ 适配数值检查、Hugo 与生成页静态检查通过；实际阴影/IBL/GPU 画面待验证。明确默认延迟路径的字段消费、量化与阴影开关边界，输出止于场景颜色 |
| 17 | [场景颜色怎样经过后处理成为最终画面](../content/engine-toolchain/piccolo-engine-17-post-processing-final-frame.md) | 已写：固定源码、五个 GLSL 独立编译、原曲线文本的 C++ 适配与分支检查、Hugo 和生成页静态检查通过；GPU 后处理、UI 画面与显示时序待验证。核对 LUT 透传、FXAA 两路附件、UI 条件选色与格式/呈现边界 |
| 18 | [编辑器怎样拾取、创建和删除场景对象](../content/engine-toolchain/piccolo-engine-18-editor-pick-create-delete.md) | 已写：固定源码、两份拾取 GLSL 独立编译、原 ID 映射/删除与坐标表达式的 C++ 适配检查、实际墙资源检查、Hugo 与生成页静态检查通过；编辑器实际拾取/创建/删除、GPU 回读与保存验证待进行。明确 ID 无效值不一致、拾取蒙皮位置未消费及多部件删除范围 |

09—18 现均有独立正文，00—18 主线文章已齐备。12 补齐片段到姿态与网格变形，13 接通粒子状态、计算调度与后续绘制，14 接通脚本与反射访问；后续优先按各篇待验证项建立完整构建、实际断点与运行证据，不自动扩充新主线编号。仍按“问题 → 必要概念 → 固定源码路径 → 状态推导 → 验证”写作。执行证据见 [09 输入](piccolo-engine-input-motor-controller-outline.md)、[10 物理](piccolo-engine-rigidbody-collision-query-outline.md)、[11 相机](piccolo-engine-camera-follow-render-outline.md)、[12 动画与蒙皮](piccolo-engine-animation-skeleton-skinning-outline.md)、[13 粒子](piccolo-engine-particle-gpu-simulation-outline.md)、[14 Lua](piccolo-engine-lua-component-reflection-outline.md)、[15 资源](piccolo-engine-mesh-texture-gpu-resources-outline.md)、[16 着色](piccolo-engine-material-light-shadow-outline.md)、[17 后处理](piccolo-engine-post-processing-final-frame-outline.md)、[18 编辑器](piccolo-engine-editor-pick-create-delete-outline.md)。

## 随主线补充的前置

补篇按学习需要插入，不占主线编号。网页先读 00、C++ 前置/专项与渲染基础，再沿 01—10 进入构建、启动、对象、资源、变换、提交、编辑器和输入/物理；GFX-02、11/12/13/14/15/16/17/18 随后接续。主线 00—18、五篇前置与渲染补充、PCPP-1/2/3 共 27 篇，series_order 连续 0～26。14 使用 22，原 15/16/17/18 四篇顺移为23～26，URL 不变；原定主线正文齐备。完整 Debug 构建与实际运行状态不随文章成稿升级。

| 编号 | 主题 | 状态 | 正文 |
| --- | --- | --- | --- |
| CPP-01 | 读懂引擎 C++：智能指针与对象生命周期 | 已写：源码核对、独立程序和页面检查通过，Piccolo 运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md) |
| CPP-02 | 读懂引擎 C++：类、继承与虚函数 | 已写：源码核对、独立程序和页面检查通过，Piccolo 运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md) |
| CPP-03 | 读懂引擎 C++：模板、宏与反射代码生成 | 已写：源码核对、独立程序、预处理与页面检查通过；01 已记录 PiccoloParser 实际生成，引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md) |
| GFX-01 | 读懂渲染管线：从场景对象到一帧画面 | 已写：源码、API 语义与页面检查通过；引擎运行与图形捕获待验证 | [正文](../content/rendering/piccolo-rendering-01-scene-to-frame.md) |
| GFX-02 | 从 GLSL 到 GPU：用 Piccolo 串起渲染引擎、API 与系统 | 已写：源码、两份 Shader 编译与嵌入字节、Hugo、链接和浏览器图形检查通过；引擎运行、GPU 与显示时序待验证 | [正文](../content/rendering/piccolo-rendering-02-shader-api-driver-gpu.md) |

GFX-01 建立完整渲染链路的阅读模型，不替代主线 06 的具体对象变化案例与 07 的 Pass/Vulkan 深挖。06 追踪 Wall_1 的位置变化到渲染实体；07 继续沿可见输入、实例批次和缓冲绑定追到命令记录、队列提交与呈现请求。两篇均提供源码条件推导，运行与图形捕获待开展。

GFX-02 作为渲染补充，解释程序、数据和命令的交接：从 GLSL 编译到 SPIR-V 与 C++ 嵌入头，核对 pipeline、资源绑定和矩阵布局，再用官方资料解释 Loader、驱动、OS 与 GPU 的边界。只保留语言与 API 非一一对应的必要说明，完整 HLSL 对照按需另行安排；实际编译验证不等于完整引擎运行或硬件性能验证。

08 将编辑器面板和拖轴的写入路径接入前面的对象/渲染链，再追保存收集、实例组件覆盖和重载重建。当前源码的关卡保存未回填 gravity 与 character_name，正文明确给出默认值变化的推导；实际文件写出和业务恢复仍待验证，不能把 08 记成保存闭环已通过运行验收。

## 单篇完成标准

1. 先说明问题与最小模型，再定位具体源码。
2. 图中箭头明确含义，不把目录、构建依赖、调用和数据流混在一起。
3. 实现结论附固定提交的源文件链接；推断或建议说明理由。
4. 实践篇记录真实工具链、断点、调用栈和状态；缺失证据明确列为待验证。
5. 回写本表及相关阅读入口，执行 Hugo 构建与页面检查。

## 当前执行记录

- 首篇提纲、源码证据入口和验证记录：[2026-10-23 架构地图执行计划](superpowers/plans/2026-10-23-piccolo-architecture-article.md)。
- 2026-10-23：首篇正文完成，源码事实与图关系已核对，Hugo 构建和页面图形渲染检查通过；阅读入口与根路由已接入。提交、远端与站点部署状态以 Git 和 CI 实时回执为准。
- Piccolo 完整编辑器构建、调试器运行和图形捕获尚未通过；01 的配置与生成已实测，编译阻塞和后续验收边界见其执行记录。
- CPP-01 提纲与源码、小程序验证记录：[智能指针与生命周期](piccolo-cpp-smart-pointers-outline.md)。
- CPP-02 提纲与源码、小程序验证记录：[类、继承与虚函数](piccolo-cpp-classes-virtual-functions-outline.md)。
- CPP-03 提纲与源码、小程序验证记录：[模板、宏与反射代码生成](piccolo-cpp-templates-macros-reflection-outline.md)。
- PCPP-1 提纲与源码、独立容器程序及导航验证记录：[对象容器、借用与遍历](piccolo-object-containers-borrowing-outline.md)。
- PCPP-2 提纲与源码、独立回调程序及导航验证记录：[窗口与编辑器回调](piccolo-window-editor-callbacks-outline.md)。
- PCPP-3 提纲与源码、独立缓冲/线程程序及导航验证记录：[状态缓冲与线程边界](piccolo-state-buffer-thread-boundaries-outline.md)。
- GFX-01 提纲与源码、页面验证记录：[从场景对象到一帧画面](piccolo-rendering-scene-to-frame-outline.md)。
- GFX-02 提纲与源码、Shader 编译及导航验证记录：[从 GLSL 到 GPU](piccolo-rendering-shader-api-driver-gpu-outline.md)。
- 主线 02 提纲与源码、页面验证记录：[从启动走到第一帧](piccolo-engine-startup-first-frame-outline.md)。
- 主线 03 提纲与源码、页面验证记录：[一帧怎样推进](piccolo-engine-frame-order-data-handoff-outline.md)。
- 主线 04 提纲与源码、资源及页面验证记录：[场景文件怎样成为对象与组件](piccolo-engine-scene-to-objects-outline.md)。
- 主线 05 提纲与源码、模板及页面验证记录：[资源、类型信息与序列化](piccolo-engine-resource-type-serialization-outline.md)。
- 主线 06 提纲与源码、数值推导及页面验证记录：[从位置到渲染表示](piccolo-engine-position-to-render-outline.md)。
- 主线 07 提纲与源码、接口语义及页面验证记录：[从渲染场景进入 Pass 与 Vulkan 提交](piccolo-engine-pass-vulkan-outline.md)。
- 主线 08 提纲与源码、资源/模板及页面验证记录：[编辑器怎样修改、保存并恢复场景状态](piccolo-engine-editor-save-reload-outline.md)。
- 主线 11 提纲与源码、数学/RenderCamera 数值检查及导航验证记录：[相机怎样跟随角色，并把视角交给渲染系统](piccolo-engine-camera-follow-render-outline.md)。
- 主线 15 提纲与源码、实际 OBJ/TGA 解码、资源键/身份与字节检查及导航验证记录：[网格与纹理怎样从文件成为 GPU 资源](piccolo-engine-mesh-texture-gpu-resources-outline.md)。
- 主线 16 提纲与源码、Shader 编译、BRDF 条件数值及导航验证记录：[材质、光照与阴影怎样共同决定表面颜色](piccolo-engine-material-light-shadow-outline.md)。
- 主线 17 提纲与源码、Shader 编译、曲线/分支数值及导航验证记录：[场景颜色怎样经过后处理成为最终画面](piccolo-engine-post-processing-final-frame-outline.md)。
- 主线 18 提纲与源码、拾取 Shader、ID/删除/坐标条件与导航验证记录：[编辑器怎样拾取、创建和删除场景对象](piccolo-engine-editor-pick-create-delete-outline.md)。
- 主线 14 提纲与源码、真实脚本、随附 Lua/sol2 与原桥接/反射的桩宿主及导航验证：[Lua 脚本、组件访问与反射调用](piccolo-engine-lua-component-reflection-outline.md)。
- 主线 13 提纲与源码、原类型布局/字节、独立池状态和 Shader/导航验证：[粒子发射器、GPU 模拟与绘制](piccolo-engine-particle-gpu-simulation-outline.md)。
- 主线 12 提纲与源码、原 Skeleton/Node、实际资源与 Shader/导航验证：[动画、骨骼姿态与蒙皮](piccolo-engine-animation-skeleton-skinning-outline.md)。
- 主线 10 提纲与源码、形状转换/物理步进/刚体更新的记录检查及导航验证：[刚体与碰撞查询](piccolo-engine-rigidbody-collision-query-outline.md)。
- 主线 09 提纲与源码、原输入/运动/控制器函数与数学实现的条件检查及导航验证记录：[从一次按键到角色位移](piccolo-engine-input-motor-controller-outline.md)。
- 主线 01 提纲与工具链、配置/生成实测及页面验证记录：[建立构建与调试基线](piccolo-engine-build-debug-baseline-outline.md)。
