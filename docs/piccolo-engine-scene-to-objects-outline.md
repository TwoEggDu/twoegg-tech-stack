# Piccolo 主线 04：场景文件怎样成为世界中的对象与组件

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定主线 04，结合 Piccolo 实际代码写正文，并更新相关导航；延续先补文章、后做引擎构建与调试的安排。
- 类型：原理篇。问题是持久化的场景描述怎样恢复为带身份、组件和系统依赖的运行对象。
- 最小模型：世界/关卡引用、对象实例与可复用定义、类型化资源恢复、运行对象组装与 postLoadResource 接线。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。日期为 2026-10-25；主线编号 04，网页 series_order 为 7。
- 贯穿案例：默认世界 `hello.world.json` → `1-1.level.json` → `Wall_1` → `wall.object.json`；用 Wall_2 对照实例与定义，用 Player 说明活动角色选择。
- 只展开反序列化与对象组装的分界，完整生成器/注册/字段读写机制留给 05；位置逐值追踪和编辑器保存分别留给 06 与 08。
- 产物：本提纲、正文、03 继续阅读、两个阅读首页、系列 plan 与根 doc-plan 中 Piccolo 行。复用已有样式，不修改共享模板/CSS，不提交推送。

## 正文提纲

1. 场景文件不是运行世界：描述、运行身份、组件状态和系统接线分别完成什么。
2. 世界、关卡、对象定义与实例资源的职责；默认路径只加载 default_level_url，不把列表表达能力当实际加载。
3. 解析真实默认关卡：31 条对象记录、重力 (0,0,-15)、character_name 为 Player；静态记录数量不是成功创建数量。
4. 给出完整 Wall_1 对象 JSON；Wall_1 与 Wall_2 共享定义 URL，各有 Transform，区分名字、资源 URL 和运行时 ID。
5. AssetManager 读文件、解析 JSON、调用 Serializer；LevelRes 已持有类型化组件指针，GObject::load 不是创建所有组件的起点。ReflectionPtr 拷贝不做深拷贝。
6. loadLevel 临时 active level、Level::load 创建物理场景，按资源数组创建 GObject；加载成功才加入 map。
7. GObject::load 的真实合并：实例组件先进入列表并 postLoad；定义整体读取后，按类型名跳过已有类型，其余依序 postLoad、追加。不是逐字段覆盖或自动按依赖排序。
8. 用 Transform、Mesh、RigidBody 三个 postLoad 落地：变换双缓冲、网格/材质描述、物理场景与刚体。依赖查找与字段就绪分开。
9. 世界/关卡/对象/组件持有与正常清理，活动角色复用已有 GObject；不把文件引用关系画成内存所有权。
10. 失败分层与边界：对象定义失败不入 map，Level 不汇总所有 createObject 结果，void postLoad 不代表所有系统资源成功。首次 tick 的渲染交接链接 03/GFX-01。
11. 静态文件交叉检查与后续断点表；不修改真实资产或编造调试结果。

## 固定证据

| 路径（相对 Piccolo） | 核对范围 |
| --- | --- |
| `engine/asset/world/hello.world.json` | 默认关卡引用 |
| `engine/asset/level/1-1.level.json` | 对象记录、实例 Transform、重力与活动角色名字 |
| `engine/asset/objects/environment/wall/wall.object.json` | 定义的 Transform/Mesh/RigidBody 顺序与依赖资源 |
| `engine/asset/objects/character/player/player.object.json` | Player 定义的六类组件 |
| `engine/source/runtime/resource/res_type/common/{world,level,object}.h` | WorldRes、LevelRes、ObjectInstanceRes 与 ObjectDefinitionRes 字段 |
| `engine/source/runtime/resource/asset_manager/asset_manager.h`、`.cpp` | 路径解析、loadAsset 与 Serializer 边界 |
| `engine/source/runtime/core/meta/serializer/serializer.h` | ReflectionPtr 的 $typeName/$context 读取入口 |
| `engine/template/allSerializer.ipp.mustache`、`commonReflectionFile.mustache` | 数组元素 read 与按类型构造模板；未运行生成器 |
| `engine/source/runtime/function/framework/world/world_manager.h`、`.cpp` | 默认加载、临时活动关卡、成功后持有与清理 |
| `engine/source/runtime/function/framework/level/level.h`、`.cpp` | 物理场景、对象创建/加入 map、活动角色选择、unload |
| `engine/source/runtime/function/framework/object/object.h`、`.cpp`、`object_id_allocator.cpp` | 运行身份、实例与定义组装、按类型去重、显式删除组件 |
| `engine/source/runtime/core/meta/reflection/reflection.h` | ReflectionPtr 的指针值拷贝与删除宏 |
| `engine/source/runtime/function/framework/component/transform/transform_component.cpp` | 两份变换与初始脏标记 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | postLoad 的描述与材质资源准备，后续 tick 交接 |
| `engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp` | 变换与当前物理场景依赖，创建/删除刚体 |
| `engine/source/runtime/function/physics/physics_manager.cpp` | 物理场景强持有和删除 |
| `engine/source/runtime/function/character/character.cpp` | 活动角色包装已有 GObject |

## 验证记录

- 写作前回读源码 SHA，源码工作区干净；资源解析使用只读脚本，不执行 Piccolo 序列化器或修改资产。
- 已解析默认关卡及其 9 个不同定义 URL，确认 31 条对象记录。Wall_1 与 Wall_2 都只有实例 Transform，定义按顺序为 Transform、Mesh、RigidBody。
- 正文 4 个 C++ 节选去除行注释和空白后均匹配固定源码；两个完整 JSON 示例分别与原世界资源和 Wall_1 对象记录逐字段比较一致。Wall_1/Wall_2 的变换与最终三类组件顺序已核对，模型和材质目标文件存在。
- 6 项调用/状态顺序核对通过：默认世界到默认关卡、临时活动关卡、Level 创建物理场景并恢复对象、GObject 成功后入 map、实例/定义组件合并、刚体依赖查询与创建。
- 29 个固定源码与资源链接均对应存在的文件或仓库目录，10 个正文 relref 目标存在。
- Hugo v0.158.0 构建退出码 0，生成 1351 页、53 个静态文件，ERROR 为零。
- 新篇、03、02、渲染篇、三篇 C++、架构地图和两个阅读首页共 10 页、318 处生成后站内链接、锚点与资源引用检查通过。
- 生成页有 5 张 HTML 表、2 段 Mermaid 图源，并沿用 series-reading.css。静态确认 03 下一篇指向 04、04 上一篇指向 03、返回系列指向 chapters 锚点；两个阅读首页均有新入口，旧世界模型待写占位移除，无未解析 relref。
- 本次只改动 7 个任务文件；源码工作区和暂存区未改变，其他文章及共享模板/CSS 的已有改动保留。生成页静态检查不代替浏览器视觉验收。
- 03 的浏览器预览访问曾被 URL 安全策略拒绝；本次不通过换地址、换浏览器或其他执行通道绕过，采用生成页静态检查，浏览器目视与图形实际渲染状态保持待验证。
- 未构建或运行 Piccolo，未执行断点或 GPU 捕获；本次不暂存、提交或推送。
