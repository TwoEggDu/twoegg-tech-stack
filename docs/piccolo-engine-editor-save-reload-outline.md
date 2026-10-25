# Piccolo 主线 08：编辑器怎样修改、保存并恢复场景状态

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定 08，结合 Piccolo 实际代码完成正文和导航；延续先写阅读文章、后构建调试的安排。
- 类型：原理篇。用 Wall_1 的位置修改贯穿编辑字段、运行缓冲、关卡资源写出、重载与派生状态重建。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；2026-10-25 回读 SHA，克隆工作区干净。
- 条件：编辑器模式、Wall_1 一直选中、位置初值 P0=(1.94597,-0.38554,0)，面板写 P1=P0+(1,0,0)；无其他位置写入，保存和重载在输入完成后分别触发，文件和组件恢复成功。
- 最小模型：持久字段、运行状态、派生表示、磁盘描述四层；修改不等于落盘，重载不等于恢复原内存身份。
- 前置复用 04 组件合并、05 字段选择和类型恢复、06 Transform/渲染交接、07 UI 在渲染阶段的调用位置；不扩展为编辑器搭建教程或存档/Undo 系统。
- 重大边界：Level::save 只填新 LevelRes 的 objects，未回填 gravity/character_name；模板按字段写出，因此默认关卡保存重载不能表述成完整无损恢复。正文以源码推导说明，不修改引擎或实际资源。
- 产物：正文（series_order=11，weight=505）、本提纲、07 继续阅读、两个阅读首页、canonical plan、根系列路由摘要，共 7 个任务文件。保持模板和样式，不暂存、提交或推送。

## 正文提纲

1. 问题空间：画面改了、菜单执行了、重启恢复了分别证明哪一层。
2. 抽象模型：持久字段 m_transform、双缓冲、渲染/物理派生状态、关卡 JSON；编辑器选择和相机不自动属于持久场景。
3. 选择 ID → 当前 Level 查找对象 → ReflectionInstance → field.get → Transform 控件；属性面板直接改 m_transform，拖轴通过 setter，两条路径分别描述。
4. 固定面板案例，UI 在本轮逻辑更新后运行；持续选中让 EditorSceneManager 每轮置脏，Transform 先交换再向 next 拷贝持久字段，因此两次后续 Transform tick 的位置传播表与渲染交接。
5. Save Current Level → WorldManager::saveCurrentLevel → Level::save → GObject::save → AssetManager::saveAsset；实际输出路径要由 root folder 和 level URL 解析，不能猜源仓库文件。
6. 保存对象的 name、definition 和全部当前组件的可序列化字段，而非仅保存原 instanced_components 或组件差量；可能把定义组件固化为实例覆盖。
7. 字段白名单：持久 Transform 可写，buffer/index/dirty/parent 等不自动保存；Serializer 与生成模板负责类型/字段写出，不负责补漏业务数据。
8. LevelRes 默认值与 Level::save 的遗漏：gravity -15 → -9.8，character_name Player → 空；Player 对象仍存在，活动 Character 按名字绑定受影响。保存 bool 也未完整检测 flush 后状态，UI 不接收结果；失败原子性与无损性分别说明。
9. Reload Current Level：旧 Level unload/erase → 新 Level load → 新 GObject 和 postLoad → UI 清 RenderScene 三个容器并取消选择；不称为全缓存清空或事务回滚。
10. 同一资源记录的内容可恢复，GObject ID 重新分配；实例组件优先使未来定义修改可能不再覆盖同类型组件。
11. 对照“未保存重载”“保存后重载”“只切 Game/Editor Mode”；模式按钮本身不克隆/重载/回滚场景。
12. 源码静态任务、后续隔离副本中的保存/重载验证表；区分字段/语义比较与文件顺序、格式变化。

## 固定证据

| 路径（相对 Piccolo） | 核对内容 |
| --- | --- |
| `engine/source/editor/source/editor_ui.cpp` | 选择列表、反射控件、Transform 控件、保存重载菜单、模式按钮、UI 顺序 |
| `engine/source/editor/source/editor_scene_manager.cpp`、`editor_input_manager.cpp` | 选择 ID 查询、每轮置脏、拖轴 setter、取消选择与轴显示 |
| `engine/source/editor/source/editor.cpp`、`engine/source/runtime/engine.cpp`、`function/render/passes/ui_pass.cpp` | 编辑器 tick、逻辑/渲染/UI 调用次序与模式筛选 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h`、`.cpp` | 持久字段白名单、setter、缓冲交换、拷贝与加载初始化 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | current matrix 描述产生、入队与清脏 |
| `engine/source/runtime/function/framework/object/object.cpp`、`object_id_allocator.cpp` | 实例/定义合并、保存全部组件、新对象 ID |
| `engine/source/runtime/function/framework/level/level.cpp`、`.h` | 保存收集、未回填元数据、清理/加载、角色按名匹配、无序对象容器 |
| `engine/source/runtime/function/framework/world/world_manager.cpp` | 活动关卡保存、重载生命周期与失败路径 |
| `engine/source/runtime/resource/res_type/common/level.h`、`object.h` | 资源字段与默认值，实例没有持久 GObject ID |
| `engine/source/runtime/resource/asset_manager/asset_manager.h`、`.cpp`、`config_manager/config_manager.cpp` | 写出/flush/返回、读取、实际绝对路径 |
| `engine/source/runtime/core/meta/serializer/serializer.h`、`engine/template/allSerializer.ipp.mustache`、解析器 `language_types/field.cpp` | 反射指针类型上下文、字段选择与写出模板 |
| `engine/source/runtime/function/render/render_system.cpp`、`render_scene.cpp` | UI 调用的 reload 清理边界 |
| `engine/asset/level/1-1.level.json`、`objects/environment/wall/wall.object.json` | 位置、重力/角色名、实例与定义组件来源 |

## 验证记录

- 正文 10 个 C++ 节选去除空白后均匹配固定源码；27 个固定源码/资源/许可链接目标、8 个正文 relref 目标存在。源码克隆仍干净，未保存、重载或修改实际关卡。
- 只读解析默认关卡，确认 Wall_1 的位置与单个实例 Transform、定义中的 Transform/Mesh/RigidBody，以及 gravity=(0,0,-15)、character_name=Player。检查 Level::save 函数体只收集 objects，结合 LevelRes 默认值、字段选择和写出模板，核对元数据变化推导；没有运行生成器或伪造保存文件。
- 人工沿 EditorUI、UIPass、编辑器循环、Transform tick、Mesh tick 核对属性面板写入和两次后续逻辑 tick 的位置传播表。持续选中、编辑器模式、双缓冲初始稳定与无其他位置写入的条件已写明；该表不是运行帧数测量。
- 核对保存当前组件、实例优先合并、新对象 ID、按名建立活动 Character、菜单重载顺序、RenderScene 三容器清理与模式切换分支。正文没有将重载表述成事务回滚或完整缓存重置。
- Hugo v0.158.0 构建退出码 0，生成 1356 页、53 个静态文件，ERROR 为零。输出为 `%TEMP%/TechStackShow-piccolo-editor-08-20261025/public`，缓存为同级 `cache`。
- 本篇、此前 Piccolo 文章和两个阅读首页共 14 页、831 处生成后站内链接/锚点/资源引用检查通过，无缺失目标；本篇有 7 张 HTML 表、2 段 Mermaid 图源，沿用 series-reading.css。图源静态检查不代替图形渲染或浏览器目视。
- 静态确认 07 下一篇指向 08、08 上一篇指向 07、返回系列指向 chapters，两个首页均有新入口；旧编辑器待写占位移除，01 构建基线仍保留待写。
- 本次 7 个任务文件完成；导航和规划文件的 `git diff --check` 通过，暂存区仍为空，其他文章和共享文件的已有改动保留。不暂存、提交或推送。
- 不执行 Piccolo 保存、重载或资源改写；不把脚本模拟或源码推导记为引擎运行结果。浏览器目视、生成器运行、断点和画面验证均待开展。
- 前文预览曾被 URL 安全策略拒绝，保持限制，不换地址或工具绕过。构建输出沿用允许写入的系统临时目录。
