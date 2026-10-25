# Piccolo 主线 06：改一个位置，怎样沿逻辑数据追到渲染表示

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定主线 06，结合 Piccolo 实际代码写正文并更新导航，延续先补文章、后做引擎调试。
- 类型：原理篇，以固定对象的条件推导贯穿实现。问题是位置写入怎样跨越运行缓冲、脏描述与逻辑/渲染边界，更新到相应 RenderEntity。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。日期 2026-10-25；主线 06，网页 series_order 为 9。
- 案例：默认关卡 Wall_1，初始 P0=(1.94597,-0.38554,0)，纸上目标 P1=P0+(1,0,0)。壁体只有一个网格 part，其局部变换为单位变换。
- 主路径条件：已加载且已有渲染实体、资源已准备；编辑器模式；在下一次 Transform tick 之前调用 setPosition；保持旋转/缩放及网格/材质来源，不引入其他位置写入，交换就绪。只给缓冲索引安排一个便于说明的起点，不冒充断点快照。
- 区分已有编辑器拖轴调用与受控 setter 推导：实际 moveEntity 会调用位置、旋转和缩放三个 setter，属性面板又经反射写 m_transform；主文不把它们当成完全相同的写入路径。完整编辑器保存/重载留给 08。
- 停在渲染表示和可见节点的使用边界，Pass、GPU 命令与 Vulkan 提交留给 07 和既有 GFX-01。物理更新只说明旁路依赖，不做完整模拟审计。
- 产物：本提纲、正文、05 继续阅读、两个阅读首页、系列 plan 与根 doc-plan Piccolo 行；不改共享模板/CSS，不提交推送。

## 正文提纲

1. 问题空间：setter 返回、逻辑读取、变化排队和渲染表示分别是哪些状态；固定对象身份和具体值。
2. 最小追踪模型：写入者、当前/下一份值、脏标记、携带描述、消费更新和后续使用。
3. 实际改动入口：编辑器 onCursorPos → moveEntity → 三个 setter；本文单独追位置 setter 的明确时点，排除猜测鼠标像素与世界位移的关系。
4. Transform：setPosition 写 next 与 m_transform 并置脏，getPosition/getMatrix 读 current；tick 先交换，再更新刚体，编辑器再向 next 拷贝 m_transform。
5. 用 P0/P1 和索引表走一遍，标明新值何时成为当前值。两份 Transform 与两份 RenderSwapData 是两种不同缓冲。
6. Mesh：按脏条件取 current matrix，与 part local matrix 相乘、复制到 dirty parts，再恢复原始 part local matrix；产生含 GObjectID 的描述并清脏。
7. Wall_1 的 local part 为 I，故描述中矩阵平移列约为 P1；保留原旋转缩放，解释矩阵与位置字段不能一般地混同。
8. RenderSwapData 队列是值拷贝、可含同对象多条；准备好才交换索引，不清脏等于渲染已消费。具体规则链接 03。
9. processSwapData 用 (GObjectID,part_index) 稳定定位实例 ID，矩阵进新 RenderEntity，已有实体用整体赋值更新；资源来源未改且缓存正常时不重复走首次网格/材质加载上传分支。
10. 主相机可见性使用模型矩阵变换包围盒，节点引用实体矩阵；渲染表示更新不保证最终像素或 GPU 完成。
11. 时点反例：若 setter 在 Transform tick 后、Mesh 前执行，Mesh 可读旧 current 并清脏；后续是否补发取决于其他置脏者，不能给统一丢失/延迟结论。属性面板修改原始字段也要另查。
12. 静态任务与后续断点观察：锁定 GObjectID、part 0、缓冲/描述索引、矩阵平移列、实体与可见节点；源码推导与运行证据分开。

## 固定证据

| 路径（相对 Piccolo） | 核对内容 |
| --- | --- |
| `engine/asset/level/1-1.level.json`、`engine/asset/objects/environment/wall/wall.object.json` | Wall_1 起点、组件顺序、模型/材质和单位 part 变换 |
| `engine/source/editor/source/editor_input_manager.cpp`、`editor_scene_manager.cpp` | 拖轴 setter 调用，选中对象每次 tick 置脏 |
| `engine/source/editor/source/editor_ui.cpp` | 属性面板通过 Transform 指针修改字段，与 setter 路径区别 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h`、`.cpp` | current/next、setter、交换、编辑器复制与刚体旁路 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | part 描述准备、矩阵组合、恢复 local、入队与清脏 |
| `engine/source/runtime/function/framework/object/object.cpp`、`engine.cpp` | 组件顺序、模式筛选、逻辑/交换/渲染次序 |
| `engine/source/runtime/core/math/transform.h`、`matrix4.cpp` | 矩阵构造、平移列和局部变换组合 |
| `engine/source/runtime/function/render/render_object.h`、`render_entity.h` | 描述、对象/part 身份和实体矩阵 |
| `engine/source/runtime/function/render/render_swap_context.h`、`.cpp` | 描述值拷贝、队列、就绪检查、槽位交换、消费 reset |
| `engine/source/runtime/function/render/render_guid_allocator.h` | 已有元素复用实例 GUID |
| `engine/source/runtime/function/render/render_system.cpp` | 描述消费、缓存分支、实体替换、pop/reset 和可见性阶段 |
| `engine/source/runtime/function/render/render_scene.cpp` | 模型矩阵变换包围盒、可见节点保存矩阵引用 |
| `engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp` | transform/scale dirty 对刚体更新分支的影响 |

## 验证记录

- 写作前回读固定 SHA，源码工作区干净。只读解析 Wall_1 与墙体定义，确认实例 Transform、定义组件顺序以及一个单位变换的网格 part。
- P1、缓冲表和矩阵列为条件推导，不运行教学程序伪造引擎日志；没有执行位置改动、组件 tick、渲染或 GPU 捕获。
- 正文 7 个 C++ 节选在去除空白后均匹配固定源码；19 个固定源码与资源链接目标存在，8 个正文 relref 目标存在。
- 只读资源核对确认 Wall_1 的初始 P0、一个单位局部变换的 part，以及实例 Transform + 定义 Mesh/RigidBody 的顺序。P1=P0+(1,0,0) 的算术结果与正文一致；Matrix4x4 源码确认 position.x/y/z 赋给第 0/1/2 行第 3 列。
- 8 项阶段顺序核对通过：位置 setter、Transform tick、Mesh 描述产生、引擎交接、实例 GUID 复用、渲染消费更新、消费先于可见性以及主相机矩阵使用。缓冲表与较晚写入反例人工按这些函数推导，没有执行组件或假造引擎日志。
- Hugo v0.158.0 构建退出码 0，生成 1353 页、53 个静态文件，ERROR 为零。
- 新篇、主线 02—05、渲染篇、三篇 C++、架构地图和两个阅读首页共 12 页、379 处生成后站内链接、锚点与资源引用检查通过。
- 生成页有 4 张 HTML 表、2 段 Mermaid 图源，沿用 series-reading.css。静态确认 05 下一篇指向 06、06 上一篇指向 05、返回系列指向 chapters；两个首页均有新入口，旧位置变化待写占位移除，无未解析 relref。系列 plan 中 GFX-01 与 06 的关系也已同步。
- 本次修改范围为 7 个任务文件；源码工作区与暂存区未改变，其他文章和共享文件的已有改动保留。生成页静态检查不代替浏览器目视或图形运行验收。
- 前文浏览器访问被 URL 安全策略拒绝；本次不变更地址、浏览器或执行通道绕过，目视和 Mermaid 实际渲染检查保留待验证。
- 本次不构建/运行 Piccolo，不暂存、提交或推送。
