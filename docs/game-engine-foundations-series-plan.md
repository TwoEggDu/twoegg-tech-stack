# 游戏引擎公共基础：Piccolo、Unity、Unreal 前置课程规划

> 建立日期：2026-10-25。面向已经做过游戏、希望系统补齐引擎原理的开发者。
> 本文件是本课程唯一的 canonical 单元目录，维护内容边界、复用与补写、知识依赖、分流和建设顺序。根 [doc-plan](../doc-plan.md) 只保留系列级路由；单篇 outline 和验证记录在开写时建立。
> GEF-02 已有公共正文与独立程序验证；其余单元按下表复用或补写，三引擎学习与运行验收仍分别开展。已有文件存在、原文可读、课程单元完成、实际引擎运行通过是不同状态。

## 1. 课程定位与完成目标

这门课为 Piccolo、Unity、Unreal 提供一套共同的引擎认知：从内容生产、对象与资源，到一帧执行、专业系统和工具链，解释各部分为什么存在、怎样传递数据、怎样管理生命周期和执行成本。

学完相关单元后，读者应能：

1. 画出系统的输入、输出、状态、持有关系和更新时机。
2. 区分通用问题、教学模型和具体引擎的实现选择。
3. 带着问题进入具体实现，知道要找哪些创建点、调用点、状态交接和释放条件。
4. 比较至少两种设计的前提与代价，并设计一个最小验证。
5. 用实际证据解释现象；尚未验证的推导保持为推导。

教学组织为 **公共底座 → 按系统补充 → 各引擎专项前置与具体实现**。箭头表示学习关系，不表示运行时串行顺序。读者不必先完成全部 28 个单元才能开始阅读某台引擎。

### 与其他系列的分工

| 内容 | 本课程承担什么 | 后续归属 |
|---|---|---|
| 引擎整体地图 | 复用分层认识，补共同术语与学习路线 | [游戏引擎架构地图](game-engine-architecture-series-plan.md) 保留原正文与归属 |
| C++、数学与计算基础 | 指向当篇所需的最低知识，说明它解决什么引擎问题 | [C++ 公共基础](cpp-foundations-series-plan.md)、[图形数学](../content/rendering/graphics-math-series-index.md)、硬件和构建调试专题 |
| 渲染 | 建立空间、表示、Pass、资源与提交的最低模型 | [渲染栈](game-engine-rendering-stack-series-plan.md) 管深入的通用设计；具体引擎管后端实现 |
| 数据导向 | 解释布局、批处理、查询与结构变化的动机 | [数据导向运行时](data-oriented-runtime-series-plan.md) 管 DOTS、Mass、自研执行与存储专题 |
| 引擎实现 | 提供后续入口和观察问题 | [Piccolo](piccolo-engine-series-plan.md)、[Unity 阅读路线](../content/reading/unity-engine.md)、[Unreal 阅读路线](../content/reading/unreal-engine.md) |
| 项目工程与交付 | 讲清代码、资产、平台和产物关系 | 交付、资源发布、UE 全栈、性能等专项继续深入 |

本课程不要求完成整台自研引擎，不把某台引擎的编译运行设为公共原理课的先决条件。Lumen、Nanite、高级物理、完整网络服务端、运行时内部实现属于后续选读。

## 2. GAMES104 的参考方式与证据范围

课程覆盖面以 [GAMES104 官方实际课件目录](https://games-cn.org/games104-slides/)为参考，不沿用开课预告的讲次排列，也不将本专栏写成逐讲转录。

| 官方讲次 | 课程主题 | 本课程承接 |
|---|---|---|
| 01—03 | 总览、分层、世界组织 | GEF-01—03，并前移对象与资产基础 |
| 04—07 | 渲染 | GEF-11—15；具体效果转入选读 |
| 08—12 | 动画、物理、特效 | GEF-16—19、23 |
| 13—14 | 工具链 | GEF-05—06、22、26—27 |
| 15—17 | Gameplay、脚本与 AI | GEF-08、20—21、24 |
| 18—19 | 网络架构 | GEF-25；完整架构由网络专题展开 |
| 20—22 | 数据导向与任务、Lumen、Nanite | GEF-09—10；后两者作为后续案例 |

2026-10-25 已核对官方目录，抽读 [Lecture 13：Tool Chains](https://games-1312234642.cos.ap-guangzhou.myqcloud.com/course/GAMES104/GAMES104_Lecture13.pdf) 和 [Lecture 15：Gameplay](https://games-1312234642.cos.ap-guangzhou.myqcloud.com/course/GAMES104/GAMES104_Lecture15.pdf) 的相关文字内容。未逐段看完全部视频；部分其他 PDF 抓取失败，不记为已审读。

工具链课件中的类型描述、反序列化、资产引用和编辑/运行数据，以及 Gameplay 课件中的回调寿命、队列时序和脚本对象关系，是公共底座的参考起点。这里的调整是把分散概念前移并连接，不声称原课程没有涉及它们。后续每篇仍需核对自己的原始资料、具体版本和实验。

## 3. 状态、编号与复用规则

**GEF-01—28 是课程单元编号，不等于 28 篇新增文章。** 本次逐项盘点正文范围与部分关键段落，尚未对全部旧文做事实与版本审计，也未验证原文实验。

| 建设方式 | 本次判定 | 完成前还要做什么 |
|---|---|---|
| 选读复用 | GEF-01、28，共 2 个 | 保留原文；补课程阅读提示、适用范围与自检，不能自动登记课程验收通过 |
| 补桥 | 共 23 个；GEF-02 已有公共正文，其余 22 个待补桥 | 已有主要片段或实现案例，需组织独立公共模型、补缺口与练习，再接回原专题；GEF-02 已完成独立程序验证，引擎运行另记 |
| 新增 | GEF-17、22、23，共 3 个 | 当前已核查材料不足以承担整个单元；已有相邻内容可辅助，但公共解释需重新起稿 |

“新增”是本次盘点的建设判断，不宣称全仓没有任何相关段落。“补桥”也不意味着已有文章整体准确、完整或已经实测。

本课程目标正文状态统一为：`已可选读`、`待补桥`、`待写`、`待修订`。实验与引擎运行另外记载，不与正文状态合并。各单元下的“验收”是检查要求，已执行结果见相应执行记录。

- 复用文章保留原 URL、编号、series 和原 canonical plan；本文件维护课程映射，不接管原系列进度。
- 补桥优先形成一篇只解决当前缺口的公共文章，已讲清的部分链接原文，不拼贴多篇长文。
- 新公共正文默认放在 `content/engine-toolchain/`，使用 `game-engine-foundations-<两位编号>-<英文主题>.md`；实际开写时冻结 slug。选读单元不为凑编号创建同名副本。
- GEF-11—15 只补最低概念和跨系统连接；需要深入的 API、驱动、GPU 或渲染架构文章，继续交给渲染栈 plan，本课程仅链接，避免同一正文出现两套篇级计划。
- 不创建空壳正文或宣称可读的课程首页；已有公共正文直接从对应单元进入，阅读路线继续复用原专题入口。

## 4. 公共底座：GEF-01—10

“前置”指已经具备相应知识，可以用现有文章、官方材料或自有实验达到，不要求等新文章出版；多项前置为共同要求。引擎案例是后续验证与应用，不反过来成为公共课前置。

### GEF-01｜一台游戏引擎怎样把内容、逻辑和硬件连接起来

- **讲什么与边界**：编辑器、运行时、游戏逻辑、专业系统、资产构建和平台的职责；用实时性与资源预算解释协作要求。止于整体地图。
- **建设与复用**：选读复用／已可选读。[架构总论](../content/system-design/game-engine-architecture-00-overview.md)提供职责地图，[00A职责、依赖与资源边界](../content/system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md)补接口依赖、资源服务/实例/设备对象的边界，[内容生产](../content/system-design/game-engine-architecture-01-content-production.md)用模板修改连接工具与预览。2026-10-25前10篇完成一轮修订，新增[08同任务比较](../content/system-design/game-engine-architecture-08-unity-unreal-same-tasks.md)，系列共11篇正文；08以警戒参数和外观交付对照Unity与Unreal。职责分类不代表运行顺序，引擎实验仍待验证；范围与检查见[修订记录](game-engine-architecture-revision-outline.md)和[08执行记录](game-engine-architecture-08-outline.md)。
- **待补**：课程导读中的术语表、三条学习路线和预算提示；00A是架构专题的选读补篇，不等于本单元或整个公共课程验收通过，不另写一份架构大全，也不把原图某一种分层当唯一标准。
- **前置与验收**：有游戏开发经验，无 GEF 前置。为“角色换装”标出内容制作、资产、世界对象、动画和渲染各自的责任。
- **后续接入**：三引擎路线的整体入口；Piccolo 架构地图、Unity 系统专题、Unreal 架构与系统。

### GEF-02｜游戏为什么能够持续运行：启动、一帧与退出

- **讲什么与边界**：初始化依赖、主循环、模拟时间与真实时间、固定/可变步长、插值、暂停和逆向清理。CPU/GPU 跨帧执行留到 09、15。
- **建设与复用**：补桥／已可选读。[公共正文](../content/engine-toolchain/game-engine-foundations-02-startup-frame-shutdown.md)已连接启动依赖、失败回滚、时间账本、固定步/补步/插值与退出责任，配 Python 最小程序及六题自检。[Game Loop](../content/system-design/patterns/patterns-29-game-loop.md)与[一帧拆解](../content/performance/game-performance-frame-breakdown.md)保留选读，Piccolo 02/03、回调与缓冲专项保留具体实现归属。复用盘点、来源与验证见[执行记录](game-engine-foundations-02-startup-frame-shutdown-outline.md)。
- **待补**：三引擎实际运行、窗口事件、线程与 GPU 时序验证。公共示例的正常输出、三处启动失败、过载、暂停、主动退出、运行异常与插值隔离已通过独立检查，不等于引擎运行通过。
- **前置与验收**：GEF-01。给定不同帧间隔，列出模拟次数、余量、渲染插值；画初始化与退出的依赖顺序。
- **后续接入**：Piccolo 02—03；Unity 生命周期/PlayerLoop；Unreal Tick 与世界更新。

### GEF-03｜引擎怎样表示一个世界：对象、组件、场景与实体

- **讲什么与边界**：对象组合、组件职责、场景/世界归属、装配与更新；区分组织树、Transform 层级和空间索引。实体存储优化交给 10。
- **建设与复用**：补桥／待补桥。[GameObject 与 Actor](../content/system-design/game-engine-architecture-02-gameobject-vs-actor.md)、[Component 模式](../content/system-design/patterns/patterns-31-component.md)分别提供映射和组合模型。
- **待补**：用不带引擎类名的对象装配图解释世界、组件与更新入口；区分共享资产与运行实例。
- **前置与验收**：GEF-01。画出两个角色共享资产、拥有各自组件的结构，说明父子关系是否必然代表所有权或更新顺序。
- **后续接入**：Piccolo 04；Unity Scene/GameObject/Component；Unreal World/Level/Actor；再分流 DOTS/Mass。

### GEF-04｜对象存在于哪里，又由谁结束它的生命

- **讲什么与边界**：类型、实例、身份、地址、句柄、强弱持有；逻辑停用、从世界移除、对象销毁与资源释放的区别。语言内存规则引用 C++ 公共课。
- **建设与复用**：补桥／待补桥。[运行时底座](../content/system-design/game-engine-architecture-03-runtime-foundation.md)提供站位，[句柄与存储](../content/engine-toolchain/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md)提供身份模型，[Unity 引用身份](../content/engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md)作专项选读。
- **待补**：独立持有图、代际句柄示例和延迟删除时序；分别标明引用计数、追踪 GC、显式管理的适用层与责任。
- **前置与验收**：GEF-03；值、借用、所有权按需补 C++ 公共课。预测删除后哪些访问仍有效，解释存储地址复用为什么不等于同一对象。
- **后续接入**：Piccolo PCPP-1；Unity 原生/托管对象与加载句柄；Unreal UObject/GC 及对象指针专项。

### GEF-05｜文件怎样恢复成对象：类型信息、反射与序列化

- **讲什么与边界**：类型注册、工厂、字段描述、对象 ID、引用恢复与 schema 演进；区分反射、序列化和外部代码生成。
- **建设与复用**：补桥／待补桥。[Piccolo 对象恢复](../content/engine-toolchain/piccolo-engine-05-resource-type-serialization.md)、[UE 反射与序列化](../content/system-design/ue-02-reflection-serialization.md)是实现材料；GAMES104 工具链提供概念参考。
- **待补**：独立的“两对象互相引用”恢复模型、缺失类型/字段处理与版本变化示例；不能由 JSON 解析成功推出对象可用。
- **前置与验收**：GEF-04。演示先建立身份与实例再连接引用的一种设计，说明循环引用、缺失字段和未知类型如何处理。
- **后续接入**：Piccolo 04—05 与生成代码；Unity 序列化、Scene/Prefab 恢复；Unreal 反射、类型与序列化专项。

### GEF-06｜美术文件怎样变成游戏资产

- **讲什么与边界**：源文件、导入设置、可引用子资产、中间产物、依赖图、缓存与目标平台；区分导入、构建和运行时加载。
- **建设与复用**：补桥／待补桥。[Importer](../content/engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md)、[资产与发布](../content/system-design/game-engine-architecture-05-asset-and-delivery.md)提供生产链路。
- **待补**：引擎无关的源输入/配置/工具版本/产物关系图，以及修改一份共享纹理后的失效范围示例；不把特定缓存键或打包规则普遍化。
- **前置与验收**：GEF-01、05。区分改内容、改导入设置、改目标平台分别可能影响哪些产物，并标出需实现证据确认的部分。
- **后续接入**：Piccolo 资源文件与离线生成链；Unity Importer、资产构建；Unreal Import/Cook。不假设 Piccolo 具备完整商业引擎导入管线。

### GEF-07｜资源怎样从“找到”走到“可以使用”

- **讲什么与边界**：定位、读取、解码、依赖、创建、上传、就绪、共享、取消、驻留和卸载；资源状态与对象状态分别表达。
- **建设与复用**：补桥／待补桥。[读盘不等于可用](../content/performance/game-performance-read-does-not-mean-ready.md)、[加载内存控制](../content/engine-toolchain/unity-loading-memory-control-cache-pool-refcount-unload.md)提供阶段与持有视角。
- **待补**：完整状态机、失败/取消后的清理、同资源多请求及最后使用者退出；旧文中具体压缩与缓存路径需按版本复核。
- **前置与验收**：GEF-04、06。列出一次加载的各阶段完成条件，并解释使用者退出时哪些工作仍可能进行。
- **后续接入**：Piccolo 15；Unity AssetBundle/Addressables/YooAsset；Unreal 资产加载与流送。GPU 具体完成条件在 15 再展开。

### GEF-08｜系统怎样交换消息：调用、事件与回调

- **讲什么与边界**：直接调用、发布订阅、回调注册、同步重入、事件队列、处理顺序、取消和寿命；不把“异步”直接等同于多线程。
- **建设与复用**：补桥／待补桥。[委托、回调、事件](../content/system-design/pattern-prerequisites-04-delegate-callback-event.md)、[Event Queue](../content/system-design/patterns/patterns-35-event-queue.md)、[Piccolo 回调](../content/engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md)。
- **待补**：把行为保存、事件派发、接收者删除连成一个最小程序；比较立即派发和延后派发的时序、所有权与积压代价。
- **前置与验收**：GEF-02、04。预测接收者退出、处理期间再发事件、队列积压三种情况下的行为；写清队列策略。
- **后续接入**：Piccolo PCPP-2 和输入；Unity 事件/异步回调；Unreal Delegate、Gameplay 消息和脚本调用。

### GEF-09｜工作怎样分配与交接：线程、任务和状态缓冲

- **讲什么与边界**：任务与线程、读写依赖、同步点、生产消费、快照/双缓冲；区分所有权、可见性和完成条件。GPU 只在此定位，具体契约交给 15。
- **建设与复用**：补桥／待补桥。[调度模型](../content/engine-toolchain/data-oriented-runtime-04-how-scheduling-works-burst-jobs-mass-processor-and-self-built-execution-graph.md)、[Piccolo 状态缓冲](../content/engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md)、[UE 线程模型](../content/system-design/ue-05-threading-model.md)。
- **待补**：独立读写依赖图及串行/并行两种执行；证明“有缓冲”和“真的跨线程”是两个问题。语言竞争与同步规则链接 CXX-11。
- **前置与验收**：GEF-02、04、08；线程和同步最低概念。为同一数据列生产者、消费者、访问时段及复用条件。
- **后续接入**：Piccolo 03、PCPP-3；Unity Jobs 与异步完成；Unreal Game/Render/RHI 交接、任务系统。

### GEF-10｜数据怎样摆放，为什么会改变执行成本

- **讲什么与边界**：访问模式、局部性、AoS/SoA、热冷拆分、批处理、实体查询与结构变化；性能结论附工作负载前提。
- **建设与复用**：补桥／待补桥。[Archetype、Chunk、Fragment](../content/engine-toolchain/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md)提供深入材料，[数据导向运行时入口](../content/engine-toolchain/data-oriented-runtime-series-index.md)作后续。
- **待补**：先用普通数组解释访问量和布局，再引 ECS；给出同一工作负载下的对照方案，不预设 ECS 必然更快。
- **前置与验收**：GEF-03、04；缓存基础按需补。为一个只更新位置的循环选择布局并解释取舍；测速需记录构建、规模与设备。
- **后续接入**：Unity DOTS、Unreal Mass；Piccolo 对象遍历可作比较对象，不强行要求改造成 ECS。

## 5. 系统补充：GEF-11—28

### GEF-11｜坐标、变换、相机与可见性怎样连接

- **讲什么与边界**：点与方向、局部/世界/观察/裁剪空间、层级变换、投影与透视除法、包围体和可见性。空间索引只交代用途，不展开全部算法。
- **建设与复用**：补桥／待补桥。[向量矩阵](../content/rendering/math-01-vectors-matrices.md)、[视锥数学](../content/rendering/math-03-frustum-math.md)是专项材料，[Piccolo 相机](../content/engine-toolchain/piccolo-engine-11-camera-follow-render-view.md)提供数值案例。
- **待补**：现有矩阵篇偏布局，先补几何直觉和一条点坐标变换；明确矩阵乘法约定、坐标系和 API 深度范围。
- **前置与验收**：GEF-03；向量、点积、矩阵乘法按需补。手算一个点经过父子变换与相机后的结果，解释可见性测试在哪个空间进行。
- **后续接入**：Piccolo 06、11；Unity Transform/Camera；Unreal 组件变换、视图与剔除。

### GEF-12｜网格、材质与着色程序怎样组成渲染输入

- **讲什么与边界**：顶点/索引、属性、纹理采样、材质参数、着色程序、渲染表示与一次绘制；光照模型交给 13。
- **建设与复用**：补桥／待补桥。[模型、材质和贴图](../content/rendering/game-graphics-stack-02b-mesh-material-texture-pipeline.md)是主要起点，GPU 绑定案例后接 Piccolo 15。
- **待补**：补逻辑对象与渲染表示、程序与资源接口；复用前核对原文“一个物体一次 Draw”、NDC 范围、插值和 Shader 调用次数等简化的适用条件，不能照抄为普遍规律。
- **前置与验收**：GEF-07、11。画出两个实例共享网格/材质但各有变换的输入关系，说明改参数、换纹理、换程序影响什么。
- **后续接入**：Piccolo 06、15；Unity Renderer/Material；Unreal 渲染表示、Mesh 与 Material。

### GEF-13｜光照、阴影与表面参数怎样决定颜色

- **讲什么与边界**：表面法线、入射光、反射模型、直接/间接光、可见性与阴影；建立 PBR 参数的最小意义，暂不深入 Lumen 等算法。
- **建设与复用**：补桥／待补桥。[PBR 基础](../content/rendering/shader-lighting-05-pbr.md)、[Shadow Map](../content/rendering/unity-rendering-02b-shadow-map.md)提供技术片段；[Piccolo 表面颜色](../content/engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md)已有固定实现、Shader 独立编译与数值检查，可作案例，完整引擎与 GPU 画面仍未验证。
- **待补**：剥离特定 Shader API，串联表面参数、照明、遮挡和输出颜色；用同一表面变化说明各项影响，环境光照指向深入阅读。
- **前置与验收**：GEF-12。分别改变法线、粗糙度、光源和遮挡，解释应该改变哪类结果及模型限制。
- **后续接入**：Piccolo 16 的已有正文；Unity 光照/URP；Unreal 材质与光照。公共 GEF-13 仍待补桥，具体版本和算法由专项核对。

### GEF-14｜为什么要把一帧拆成 Pass 与资源依赖

- **讲什么与边界**：阶段输入输出、颜色/深度目标、读写依赖、临时资源、后处理；比较显式流程和依赖图的适用规模。
- **建设与复用**：补桥／待补桥。[Piccolo 场景到一帧](../content/rendering/piccolo-rendering-01-scene-to-frame.md)、[RenderGraph](../content/rendering/unity-rendering-10b-render-graph.md)。
- **待补**：独立的三阶段资源图和生命周期表；区分教学 Pass、API render pass、pipeline 对象，不把 Piccolo 显式流程直接称为完整 RenderGraph。
- **前置与验收**：GEF-12；用占位颜色即可讲依赖，13 为光照案例选读。为一张中间图像判断首次写入、最后读取和可回收位置。
- **后续接入**：Piccolo 07、17（后者已有正文，GPU 后处理、UI 画面与显示时序仍待验证）；Unity SRP/RenderGraph；Unreal RDG。

### GEF-15｜图形 API、GPU 资源、提交与呈现各自负责什么

- **讲什么与边界**：CPU 数据、设备资源、视图/绑定、命令记录、提交、同步和呈现；分别定位驱动、OS 和显示系统。具体屏障与后端代码由专项承担。
- **建设与复用**：补桥／待补桥。[图形 API](../content/rendering/game-graphics-stack-04-rendering-apis.md)、[OS 与驱动](../content/rendering/game-graphics-stack-05-os-and-driver.md)、[Piccolo 文件到 GPU 资源](../content/engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md)、[GLSL 到 GPU](../content/rendering/piccolo-rendering-02-shader-api-driver-gpu.md)。
- **待补**：把程序编译、资源准备与命令执行画成分别推进又汇合的关系；给出资源复用的完成条件，不把提交或 Present 返回当作屏幕已显示。
- **前置与验收**：GEF-07、09、14。给出 CPU 生产、GPU 访问、资源复用的时序，并说明哪些结论需要目标 API 的契约支持。
- **后续接入**：Piccolo 07、15；Unity 图形后端与 GPU 调试；Unreal RHI、渲染线程和 GPU 资源。

### GEF-16｜动画数据怎样得到骨骼姿态与蒙皮结果

- **讲什么与边界**：动画曲线采样、骨骼层级、局部/全局姿态、绑定姿态、权重、蒙皮输出；先串生产消费链，再选读算法。
- **建设与复用**：补桥／待补桥。[骨骼蒙皮与 Blend Shape](../content/rendering/unity-rendering-03-skeletal-animation.md)、[LBS](../content/system-design/algorithms/algo-07-linear-blend-skinning.md)。
- **待补**：采样时间到最终顶点的一条两骨骼数值示例，明确关节变换和蒙皮矩阵的区别；CPU/GPU 路径作为实现选择。
- **前置与验收**：GEF-02、11、12。给定姿态和权重计算一个顶点结果，并解释绑定姿态用在哪里。
- **后续接入**：Piccolo 12（待写）；Unity 动画与 SkinnedMeshRenderer；Unreal 骨骼动画与蒙皮。

### GEF-17｜动画混合、状态机、IK 与角色运动怎样协作

- **讲什么与边界**：姿态混合、过渡、分层、IK、Root Motion、动画事件与移动权威；止于最小协作模型，Motion Matching 等为选读。
- **建设与复用**：新增／待写。[动画与表现解耦](../content/system-design/skill-system-09-animation-and-presentation-decoupling.md)可用于逻辑/表现边界，尚不能替代动画求值课程。
- **待补**：完整公共正文，比较动画驱动移动与控制逻辑驱动移动；说明多来源写同一位置时怎样仲裁。
- **前置与验收**：GEF-16、08。画“行走转奔跑”的参数、姿态与位移来源，解释 IK 和事件分别在哪个阶段参与。
- **后续接入**：Unity Animator/动画控制；Unreal Animation Blueprint；Piccolo 对应能力须核对实际实现，不预设所有高级功能均存在。

### GEF-18｜碰撞查询、接触检测与刚体模拟有什么区别

- **讲什么与边界**：形状、包围体、空间筛选、候选对、接触信息、Raycast/Sweep、触发器与刚体；求解细节交给 19。
- **建设与复用**：补桥／待补桥。[BVH](../content/system-design/ds-12-bvh.md)、[CCD](../content/system-design/algorithms/algo-04-continuous-collision.md)提供局部材料；DOTS P02/P03 是草稿，只作作者备料。
- **待补**：从几何表示到查询/模拟两条路径的通用图；区分“检测到接触”与“产生运动响应”，说明离散和连续检测的前提。
- **前置与验收**：GEF-03、11。为射线选中、角色位移检查和箱子落地分别选择数据与处理路径。
- **后续接入**：Piccolo 10（待写）；Unity Physics/Physics2D，DOTS 物理另作选修；Unreal Chaos 与碰撞查询。

### GEF-19｜时间步长、约束求解与物理表现怎样协调

- **讲什么与边界**：积分、力/冲量、接触约束、迭代、子步进、状态插值；说明误差、稳定性和计算成本的关系。
- **建设与复用**：补桥／待补桥。[刚体动力学](../content/system-design/algorithms/algo-02-rigid-body-dynamics.md)、[约束求解](../content/system-design/algorithms/algo-03-constraint-solver.md)为深入材料；Unity 物理优化篇只选实现侧观察点。
- **待补**：先用落体或两物体接触建立直觉，再进入数学；分别讨论模拟结果和渲染插值，不把固定步长等同于确定性。
- **前置与验收**：GEF-02、18。改变步长与迭代数，先预测误差和成本，再设计记录方法；不能提前写实验收益。
- **后续接入**：Piccolo 10 与实际 Jolt 配置；Unity 固定更新/物理；Unreal 物理步进与状态同步。

### GEF-20｜输入怎样经过角色控制变成相机与世界变化

- **讲什么与边界**：设备采样、动作映射、输入上下文、控制器、移动约束、相机跟随和反馈时序；11 负责相机数学，此处负责控制关系。
- **建设与复用**：补桥／待补桥。[输入与施法请求](../content/system-design/skill-system-04-input-and-cast-request.md)提供输入/意图区分，[Piccolo 相机](../content/engine-toolchain/piccolo-engine-11-camera-follow-render-view.md)提供状态消费案例。
- **待补**：非技能场景的完整输入—意图—运动—观察模型；区分相机朝向、角色朝向和移动方向，说明 UI 焦点如何影响输入。
- **前置与验收**：GEF-02、08、11、18。画一次按键与一次转动相机的数据流，并标出碰撞查询和跟随更新的位置。
- **后续接入**：Piccolo 09（待写）、11；Unity 输入/角色控制；Unreal Pawn/Controller/Camera。选用具体输入框架时另核版本。

### GEF-21｜游戏逻辑怎样通过脚本和可视化程序接入引擎

- **讲什么与边界**：源代码/图、编译产物、解释/JIT/AOT 的位置、调用绑定、异常与对象寿命；区分语言、执行方式和可视化编辑。
- **建设与复用**：补桥／待补桥。[运行时底座](../content/system-design/game-engine-architecture-03-runtime-foundation.md)、[Unity 脚本编译](../content/engine-toolchain/unity-script-compilation-pipeline-01-overview.md)、[Blueprint VM](../content/system-design/ue-06-blueprint-vm.md)为后续材料。
- **待补**：用一次脚本调用原生对象的最小模型解释边界；旧蓝图文的分类表述和历史功能需复核，不能默认所有脚本都经过同一种 VM 或都支持热更新。
- **前置与验收**：GEF-04、05、08；编译产物概念按需补。标出类型信息、调用入口和双方对象责任，解释原生对象先结束时会发生什么。
- **后续接入**：Piccolo 14（待写）；Unity C#/Mono/IL2CPP；Unreal 蓝图与原生调用。CLI/GC 深挖继续运行时专题。

### GEF-22｜UI 的布局、事件、状态与绘制怎样协作

- **讲什么与边界**：布局树、坐标、命中测试、焦点、事件路由、状态更新、绘制和失效重建；比较立即模式与保留模式的职责。
- **建设与复用**：新增／待写。[Unity UI 渲染](../content/rendering/unity-rendering-supp-d-ui-rendering.md)可补绘制成本，但不能覆盖整套交互模型。
- **待补**：完整公共模型与一个按钮改变面板状态的例子；输入/状态/绘制三条关系分开，GUI 分类和性能不能直接照搬课程或旧文的概括。
- **前置与验收**：GEF-08、11；12 为绘制侧选读。追踪点击坐标、目标控件、状态变化、布局/绘制失效，解释为何不是每次都重建全部 UI。
- **后续接入**：Piccolo 编辑器 UI；Unity uGUI/UI Toolkit；Unreal Slate/UMG。只在实际采用的实现中核对分类与行为。

### GEF-23｜粒子和音频怎样参与同一次游戏反馈

- **讲什么与边界**：粒子发射、模拟、表示与绘制；音频声源/监听者、采样/缓冲、混音和流式播放；连接事件触发、资源就绪、预算与退出。
- **建设与复用**：新增／待写。[粒子与特效](../content/rendering/unity-rendering-04-particles-vfx.md)提供视觉半边，[表现解耦](../content/system-design/skill-system-09-animation-and-presentation-decoupling.md)提供事件边界；已核查材料未形成完整音频公共课。
- **待补**：补音频基本执行模型及视觉/声音不同时间尺度；共用一次触发与清理案例，两套系统分别解释，不能以粒子知识代替音频。
- **前置与验收**：GEF-07、08；粒子绘制细节按需读 12、14。为一次爆炸列出资源准备、触发、持续更新、停止与释放条件。
- **后续接入**：Piccolo 13（粒子，待写）；音频能力按实际实现核对；Unity 粒子/音频；Unreal Niagara/音频专题。

### GEF-24｜感知、决策、寻路和行动怎样分工

- **讲什么与边界**：感知输入、状态/黑板、FSM/行为树、导航路径与局部移动；解释路径存在不等于行动完成。
- **建设与复用**：补桥／待补桥。[State/FSM/行为树](../content/system-design/pattern-06-state-fsm-behavior-tree.md)、[NavMesh](../content/system-design/algorithms/algo-19-navmesh.md)。
- **待补**：把已有决策与导航材料连到行动反馈、取消和重新规划；避免把一种树结构或寻路实现当成 AI 的全部。
- **前置与验收**：GEF-03、08、11、20。用“发现目标—追赶—路径失效—重新决策”说明各层更新和失败责任。
- **后续接入**：Unity 导航与项目 AI；Unreal AI/导航专题。Piccolo 当前计划未列完整 AI 主线，不强配现成入口。

### GEF-25｜同一个世界怎样在多台机器上运行

- **讲什么与边界**：多进程状态、权威、输入/事件/状态传输、延迟与丢包、快照、插值、预测与校正；确定性只是部分方案的要求，不作普遍假设。
- **建设与复用**：补桥／待补桥。[UE 网络架构](../content/system-design/ue-net-01-architecture.md)、[移动同步](../content/system-design/ue-net-03-movement-sync.md)、[快照插值](../content/system-design/algorithms/algo-14-snapshot-interpolation.md)。
- **待补**：不带 Actor/RPC API 的双端时间线；分别解释协议、引擎复制机制和游戏规则。最低网络概念在篇前补，不扩为业务后端课程。
- **前置与验收**：GEF-02、04、08；了解消息、延迟、丢包。解释一次迟到输入和一次校正分别影响哪些状态，并比较插值与预测。
- **后续接入**：Unreal 复制/移动/GAS 预测；Unity 按实际选择的网络框架接入，不预设统一内建方案。Piccolo 当前阅读主线无网络前置要求。

### GEF-26｜编辑器怎样修改、撤销、保存与预览

- **讲什么与边界**：编辑数据、运行实例、属性描述、修改命令、撤销、持久化、预览和运行模式；插件接口只说明扩展边界。
- **建设与复用**：补桥／待补桥。[内容生产](../content/system-design/game-engine-architecture-01-content-production.md)、[Command](../content/system-design/pattern-02-command.md)、[Piccolo 保存重载](../content/engine-toolchain/piccolo-engine-08-editor-save-reload.md)。
- **待补**：独立的编辑值—预览值—保存值关系和撤销模型；比较共用世界与复制运行世界，明确保存范围、脏标记和失败恢复。
- **前置与验收**：GEF-05、08；3D 操作选读 11。追一次字段修改、撤销、保存、重载，逐步列出谁的数据发生变化。
- **后续接入**：Piccolo 08、18（后者待写）；Unity Editor/序列化属性；Unreal 编辑器/事务/PIE。

### GEF-27｜平台差异、代码构建、资产构建与打包怎样连接

- **讲什么与边界**：平台能力、后端与目标、代码编译链接、资产转换、产物装配、配置和符号；包的版本兼容与发布策略只指出接口。
- **建设与复用**：补桥／待补桥。[平台抽象](../content/system-design/game-engine-architecture-06-platform-abstraction.md)、[资产与发布](../content/system-design/game-engine-architecture-05-asset-and-delivery.md)、[UE 模块构建](../content/system-design/ue-07-module-system.md)。
- **待补**：代码与资产两条生产线的输入输出图；区分开发机能构建、目标平台能运行、功能被支持和交付验收通过。
- **前置与验收**：GEF-06；编译、链接、符号的最低概念另读构建调试基础。给定一个目标平台，列出代码、内容、配置和符号如何对应同一产物。
- **后续接入**：Piccolo 01；Unity Player Build/内容构建；Unreal UBT/Cook/Stage/Package。构建入门可以提前补，不必等课程编号走到 27。

### GEF-28｜怎样用证据分析一次引擎性能问题

- **讲什么与边界**：现象、测量对象、时间线、计算/等待、资源准备、内存与预算，建立假设—证据—复测的流程。崩溃、竞态等深入排障继续专项。
- **建设与复用**：选读复用／已可选读。[性能诊断方法](../content/performance/game-performance-diagnosis-method.md)为主体，[一帧拆解](../content/performance/game-performance-frame-breakdown.md)作回看。
- **待补**：课程自检卡：为一次首次使用卡顿列出假设、观察点和反证；只在拿到真实数据后写结论，不把旧文测量直接套用到新环境。
- **前置与验收**：GEF-02、07、09。提出可区分 CPU 计算、同步等待、资源准备的采样方案；实际采集条件不足时仅登记设计。
- **后续接入**：三引擎的实际调试和性能路线；沿已选系统追加观察点，不要求读完全部系统。

## 6. 推荐阅读顺序与三引擎分流

### 完整课程的组织顺序

1. **公共起步**：01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09。01—03 先建立整体、时间与对象，后面围绕身份、资源和交接展开。
2. **渲染分支**：11 → 12 → 13 → 14 → 15。只学 Pass 依赖时，13 可后读；完整光照链按上述顺序。
3. **角色分支**：在 11、12 后读 16 → 17；在 11 后读 18 → 19 → 20，再按需求回到 17 的运动权威。
4. **玩法与工具分支**：公共底座后读 21、26；22 另需 11；23 的绘制部分按需补 12、14。
5. **其他按需分支**：10 服务数据导向；24 在角色控制后；25 在公共时间/身份/事件基础后；27 在资产基础后。
6. **诊断练习**：28 在 02、07、09 后即可开始，后续每学一个系统就补一次观察。

章节编号用于稳定引用；“后续接入”不代表已完成相关引擎实验。下面的入口只列真实文件，未写主题以文字注明。

### Piccolo：公共原理与固定实现往返阅读

具体状态以 [Piccolo canonical plan](piccolo-engine-series-plan.md) 为准；不将课程覆盖面当成 Piccolo 功能清单。

| 进入位置 | 先补哪些公共知识 | 现有入口与阅读目的 |
|---|---|---|
| 架构、构建与启动 | 01、02；构建障碍时按需取 27 的构建概念与 C++ 基础 | [架构地图](../content/system-design/piccolo-engine-00-architecture-map.md) → [构建基线](../content/engine-toolchain/piccolo-engine-01-build-debug-baseline.md) → [启动](../content/engine-toolchain/piccolo-engine-02-startup-first-frame.md) |
| 世界对象、资源恢复 | 03、04、05、06 | [场景对象](../content/engine-toolchain/piccolo-engine-04-scene-to-objects-components.md) → [类型与序列化](../content/engine-toolchain/piccolo-engine-05-resource-type-serialization.md)；C++ 生成代码专项按需补 |
| 一帧与数据交接 | 02、08、09；涉及对象借用先补 04 | [一帧执行](../content/engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md)及 PCPP-1/2/3；明确调用顺序、缓冲和线程证据 |
| 相机、资源与渲染 | 07、11、12，再读 14、15；光照前补 13 | [位置变化](../content/engine-toolchain/piccolo-engine-06-position-to-render-representation.md) → [相机](../content/engine-toolchain/piccolo-engine-11-camera-follow-render-view.md) → [GPU 资源](../content/engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md) → [Pass/提交](../content/engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md) → [材质光照](../content/engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md) |
| 输入、物理、动画、粒子、脚本 | 按主题取 18—20、16—17、23、21 | Piccolo 09、10、12、13、14 为计划中的后续篇，当前不建立虚构正文链接 |
| 编辑器 | 05、08、26；拾取/拖轴再补 11、18 | [保存重载](../content/engine-toolchain/piccolo-engine-08-editor-save-reload.md)；18 对象操作仍按原计划推进 |

最短起步为 **GEF-01—02 + 必要 C++/构建知识 → Piccolo 架构、构建、启动**。03—07 在进入对象和资源时补，08—09 在读交接时补。完整编译或运行受阻时，可以做公共小程序与源码阅读，但不记为引擎调试验收。

### Unity：围绕资产、运行时与渲染补齐机制

| 进入位置 | 公共前置 | 现有入口与下一步 |
|---|---|---|
| 对象、场景、引用和资产恢复 | 03 → 04 → 05 → 06 → 07 | [资产与序列化](unity-asset-system-and-serialization-series-plan.md)，先分身份/持有，再看 Importer 与恢复链 |
| 生命周期、异步和脚本执行 | 02、04、08、09、21 | [Unity 阅读路线](../content/reading/unity-engine.md)的脚本编译与运行时栏目；具体 C#/CLI 概念继续对应公共课 |
| 渲染 | 11—15；资源不足时回补 06—07 | [Unity 渲染入口](../content/rendering/unity-rendering-series-index.md)，再进入 SRP、URP、RenderGraph 和后端专题 |
| 动画、物理、输入与 UI | 分别取 16—17、18—20、22；特效/音频取 23 | 从 Unity 阅读路线和对应主题继续；已存在性能文章不能替代机制基础 |
| DOTS | 03、04、09、10；物理/网络再取相应系统前置 | [数据导向运行时](data-oriented-runtime-series-plan.md) → DOTS 专项；草稿状态按原入口显示 |
| 网络、工具与构建 | 分别取 25、26、27 | 网络按选定框架进入；编辑器和构建沿现有 Unity 入口，不把所有 Package 当作同一运行时 |

已有 Unity 经验的读者可从 **04—07 的身份与资源链**开始，自检缺口再回读 01—03；阅读具体引擎 C++ 不作为本公共课要求，公开文章遵守本仓源码隔离规则。

### Unreal：先对象与世界，再按任务进入各系统

| 进入位置 | 公共前置 | 现有入口与下一步 |
|---|---|---|
| C++ 工程、对象和世界 | 必要 C++/构建知识；01—05 | [Unreal 阅读路线](../content/reading/unreal-engine.md)中的工程地图/Actor → [UObject](../content/system-design/ue-01-uobject-system.md) → [反射](../content/system-design/ue-02-reflection-serialization.md) → [GC](../content/system-design/ue-03-gc.md) |
| 蓝图与玩法机制 | 08、20、21；角色表现再取 16—19 | [Blueprint VM](../content/system-design/ue-06-blueprint-vm.md)与 Gameplay/GAS 专题；GAS 网络预测另需 25 |
| 渲染与多线程 | 07、09、11—15 | [渲染架构](../content/system-design/ue-04-rendering-architecture.md)、[线程模型](../content/system-design/ue-05-threading-model.md)；Lumen/Nanite 为后续选读 |
| 资产、编辑器与交付 | 06—07、26—27 | Unreal 阅读路线及 [UE 全栈路线](ue-fullstack-expert-series-plan.md)；公共课不包含业务后端、数据库与运维全部前置 |
| 网络、AI 与大规模实体 | 网络取 25；AI 取 24；Mass 取 09—10 | [网络架构](../content/system-design/ue-net-01-architecture.md)、移动同步、AI 专项和 Mass 入口；分别核对当前版本能力 |

最短起步为 **必要 C++/工程知识 + GEF-01—05 → 第一个 Actor 与对象系统**。渲染、网络和高级 AI 均按项目阶段补，不阻塞最初的对象与世界学习。

## 7. 建设顺序、实验与完成标准

### 写作顺序

| 批次 | 建设单元 | 产物与理由 |
|---|---|---|
| 0：接入已有材料 | 01、28 的选读说明；确认下列单元的复用段落 | 给读者地图与证据方法，不新增重复长文 |
| 1：公共底座 | 02、03、04 → 05、06、07 → 08、09 | 优先解决启动、世界、身份、资源、消息与交接；每篇先满足最小自检 |
| 2：当前图形缺口 | 11 → 12 → 13、14 → 15 | 对接 Piccolo 相机、GPU 资源、光照，也支撑 Unity/Unreal 图形主线；12 先纠正复用风险 |
| 3：对象驱动的运行与工具 | 18 → 19、20；16 → 17；21、26 | 支撑角色系统、脚本、编辑保存，不要求全部图形深挖完成 |
| 4：其余按需内容 | 10、22—25、27 | 10 随 DOTS/Mass 提前；22 随 UI/编辑器提前；27 的最低构建知识随工程启动提前 |

这份表是内容建设优先级，不是所有读者必须顺序走完的课程，也不替代上面的知识依赖。

### 统一案例与实验规模

建议使用一个可拆开的小场景作为叙事载体：角色、障碍、可替换材质、两种动作、一次声音/粒子反馈、一个控制面板和一次场景切换。

- 每篇先用独立模型、数值推演或短程序完成主要解释，不绑定整个场景必须已经运行。
- 语言按问题选择：伪代码先行；涉及 C++ 寿命可沿公共课基线，几何用独立计算；具体引擎例子放到后续实现文章。
- 三条贯穿线分别为“对象从内容到销毁”“输入到状态与表现”“改动到构建与运行”；图中说明是概念、数据流还是实际执行时序。
- 参数变化、失败输入、取消/退出等边界与正常路径一起设计；实验尚未执行时标为设计。

### 每篇开写与验收

开写时建立执行层 outline，至少包含：主问题、前置、最小模型、明确不覆盖的内容、复用段落、补写点、原始来源、实验设计、三引擎后续入口。

正文按“熟悉的现象 → 中文概念 → 最小模型 → 可选设计及代价 → 示例/验证 → 自检与分流”组织。每篇只承担一个主问题；GEF-23 等组合主题用共同触发和生命周期串联，若仍超出单篇容量，先在本 plan 记录拆分决定，保留原编号映射。

一个单元完成至少需要：

1. 内容范围满足该单元要求，复用段落经事实和版本核查。
2. 自检能检查理解，而非只重复术语；必要程序或数值检查有可重现记录。
3. 具体引擎案例注明版本/提交和证据类型，公共模型不冒充普遍实现保证。
4. 独立程序、源码核对、引擎构建、运行、Profiler/图形捕获分别登记；没有后几项也能形成原理文，但必须说明验证边界。
5. 内链、frontmatter、shortcode 与 Hugo 构建通过；原文归属和课程入口正确。
6. 先更新本课程单元状态，原系列正文若发生变化再回写原 canonical plan；根路由只同步总体状态。

## 8. 本轮盘点限制与后续维护

- 本轮确认了文中列出的本地材料存在及其相关范围；“已可选读”不承诺线上已部署，不等于本课程验收完成。
- 部分引用材料是当前工作区的未提交稿；链接表示本地可查，不能从本计划推导 Git 提交、推送或站点发布状态。
- DOTS 物理 P02/P03 当前为 `draft: true`，只作为备料，不纳入读者必须完成的现有正文路径。
- GEF-12 的图形简化、GEF-21 的蓝图分类与历史能力、GEF-07 的特定加载路径，以及旧文中的测量和版本判断，需在复用时核对，不能因纳入公共课而升级为事实。
- Piccolo 的固定实现、构建与运行状态由原 plan 维护；不将这份课程规划写成新增运行验证。Unity/Unreal 的工具、版本和实际工程验收同理。
- 后续新增或拆分课程单元，统一更新本文件的编号、依赖、分流、建设方式与状态，避免在多个执行文件重复维护课程目录。
