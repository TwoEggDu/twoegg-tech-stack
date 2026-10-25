# 游戏引擎架构地图系列规划

> 本文件是本系列唯一 canonical 篇级目录。根 `doc-plan.md` 只路由系列；单篇提纲、证据卡与执行记录不能反向覆盖这里的范围和状态。

## 定位与当前状态

面向已有游戏开发经验、希望读懂引擎职责与协作的读者。目标是从一个具体变化追出输入、输出、状态持有、接口依赖和清理责任，再进入某台引擎核对实现。公共模型先解释问题，Unity、Unreal、Godot等公开材料说明具体选择，不以产品名称或文档目录推导整台引擎的架构。

2026-10-25：已有 **11篇正文**（导读、00、00A、01—08）。前10篇完成技术与结构修订，08按新规划完成同任务比较，原定正文主线齐备。既有文章URL与编号保留；08新增独立正文与导航。公开资料核对、站点验证与引擎实验分别登记，正文齐备不表示源码、引擎运行或性能验收通过。

| 编号 | 正文 / 计划 | 本轮修订与当前状态 |
| --- | --- | --- |
| 导读 | [从玩家输入到屏幕画面](../content/rendering/player-input-and-game-content-to-screen.md) | 已修订：火球连接内容/实例/专业表示/设备；输入非唯一变化来源，GPU可计算，因果不等于同一帧 |
| 00 | [总论：现代游戏引擎到底该怎么分层](../content/system-design/game-engine-architecture-00-overview.md) | 已修订：六组职责覆盖平台，Core和数据导向横向讨论；职责、依赖、数据和时间分开 |
| 00A | [引擎怎样分层：职责、依赖与资源服务的边界](../content/system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md) | 已补强依赖与装配：资源服务、纹理加载器和世界的创建、注册、注入、请求及退出；区分拥有/借用、两类注册与异步收尾；[执行记录](game-engine-architecture-00a-layers-dependencies-outline.md) |
| 01 | [为什么游戏引擎首先是一套内容生产工具](../content/system-design/game-engine-architecture-01-content-production.md) | 已修订：标题从开发者视角理解；补模板改值、撤销、持久化与预览责任 |
| 02 | [Unity的GameObject和Unreal的Actor，到底差在哪](../content/system-design/game-engine-architecture-02-gameobject-vs-actor.md) | 已修订：Hierarchy是编辑视图；区分对象/组件、Transform父子关系和共享资产 |
| 03 | [脚本、反射、GC、任务系统，到底站在引擎的哪一层](../content/system-design/game-engine-architecture-03-runtime-foundation.md) | 已修订：JIT/AOT以编译时机定义；分开帧策略、对象回调、任务机制与平台设施 |
| 04 | [渲染、物理、动画、音频、UI，为什么都像半台小引擎](../content/system-design/game-engine-architecture-04-professional-subsystems.md) | 已修订：用角色落地交接替代功能名录，区分系统内部状态与跨系统契约 |
| 05 | [资源导入、Cook、Build、Package，为什么也是引擎本体](../content/system-design/game-engine-architecture-05-asset-and-delivery.md) | 已修订：随Player与独立内容分支；区分资产身份、对象实例和运行加载 |
| 06 | [跨平台引擎到底在抽象什么](../content/system-design/game-engine-architecture-06-platform-abstraction.md) | 已修订：运行接口/后端与离线目标描述两条线；能力缺失和降级责任 |
| 07 | [为什么DOTS和Mass不能只算“一个模块”](../content/system-design/game-engine-architecture-07-dots-and-mass.md) | 已修订：模块交付与架构影响分开；补创作数据、实体批处理和专业表示交接 |
| 08 | [用同一组问题读懂两台引擎：对象、模块与工具怎样组织](../content/system-design/game-engine-architecture-08-unity-unreal-same-tasks.md) | 已成稿：Unity2022.3 / UE5.6，对照警戒参数、外观交付与工具隔离；[执行记录](game-engine-architecture-08-outline.md)、[证据卡](game-engine-architecture-08-evidence-card.md)；引擎实验待验证 |

前10篇修订过程与验证结果见[修订工作单](game-engine-architecture-revision-outline.md)，08新增正文与验证见[本次执行记录](game-engine-architecture-08-outline.md)。旧六周执行计划、00—07初稿提纲和旧进度记录只保留历史依据，固定八段结构、先写完08才能细化等旧约束不再指导当前写作。

## 统一模型与文章分工

- **00是职责索引**：内容生产、世界框架、运行机制、专业系统、资产交付、平台适配。表格不是六层调用栈。
- **00A是依赖参考**：应用/工具使用框架，框架组织专业系统，后端承接环境；资源服务与Core提供共享能力。用启动装配入口、CPU纹理加载与世界退出把接口依赖和持有关系接起来；它是设计建议，不是所有引擎真实依赖图或已运行的教学程序。
- **01—06是边界例子**：每篇以一条变化或交接说明责任，在适用处给具体引擎的官方证据。
- **07是横向组织选择**：数据导向可能改变世界存储、更新、工具和表现桥接，不占一个固定的“更高层”。
- **08是迁移检查**：用相同任务找两台引擎的对象、模块、工具和产物接点，不重讲全部系统，也不从名词推导产品性格。

每篇保持一个主问题，不强求相同小标题或Unity/Unreal等长对照。重复的内部READY说明、功能目录和“本篇不展开”段落压缩；需要保留的证据边界放在一个清楚位置。进一步内容以链接承接，不能用压缩代替必要解释。

## 阅读路径与相邻系列

| 读者问题 | 推荐路径 | 深入出口 |
| --- | --- | --- |
| 内容怎样进入运行时 | 导读 → 00 → 00A | [公共基础GEF-02](../content/engine-toolchain/game-engine-foundations-02-startup-frame-shutdown.md)：启动、帧循环与退出 |
| 编辑与世界状态怎样连接 | 01 → 02 → 03 | 对象、序列化、编辑器和具体引擎专题 |
| 多个系统怎样协作 | 00A → 04 → 07 | 数据导向运行时、渲染、动画与物理专题 |
| 内容为何需要加工，平台差异放哪 | 05 → 06 → 00A资源边界 | 资源管线、构建交付、平台专项 |
| 怎样迁移到另一台引擎 | 02 / 05建立概念 → 08同任务比较 | Unity与Unreal专题；按配置、实例、工具和目标产物分别核对 |
| 想核对真实实现 | 00/00A建立问题，再进入具体实现 | [Piccolo系列](piccolo-engine-series-plan.md)；不把公共图替代其调用链 |

[游戏引擎公共基础](game-engine-foundations-series-plan.md)复用本系列作为GEF-01入口，补课程术语、预算和自检；本系列不复制28单元课程。GEF-02已有公共正文，本轮只增加衔接，不重新起草主循环文章。渲染和数据导向的算法、存储与调度细节继续由原专题维护。

## 证据与验证约定

1. 关键事实附近给官方直链，00—08另有对应证据卡。跨版本材料分别标注，不能拼成同一源码基线。
2. 01—03主要对照Unity2022.3；04—06采用Unity6000.0；07采用Entities/Entities Graphics1.0（查阅页为1.0.16）；08统一Unity2022.3与UE5.6，不依赖额外Package。UE案例按篇使用5.6。00A使用的在线架构资料以其查看日期和页面范围为准。
3. 公共图和假想示例是解释模型；源码调用顺序、线程、设备时序和性能需要单独证据。
4. 公开仓不引受限Unity C++源码。文档事实、设计推演、教学程序与实际引擎验证分别记录。
5. 发布前检查Hugo、站内链接、目录与元数据；图的静态检查、解析和浏览器视觉验收分别登记。不得把站点构建当作引擎运行验证。

## 后续工作

原定正文主线已齐备，当前没有必须新增的主线篇目。后续按读者使用反馈修订，并在需要动手时落实08的三组最小验证：模板与实例覆盖、配置与运行状态、工具隔离与目标交付。实际执行前固定工程、引擎补丁版本和目标平台，分别保存结果，不将文档推演记为运行通过。

依赖装配与服务获取的基础例子已补入00A：说明创建者、注册记录、借用接口、请求状态、关闭入口、任务收尾与注销条件，附启动失败回收和全局Context的边界。不新增编号，正文仍为11篇。

暂不为分层继续增加泛化总论。后续若有具体阅读障碍，再考虑补强共享异步请求中的实例取消、资源版本替换或跨系统故障追踪；候选不登记为已有文章。00A当前是设计推演，任务调度、取消与设备回收的实际实现仍需在具体引擎或独立实验中验证。
