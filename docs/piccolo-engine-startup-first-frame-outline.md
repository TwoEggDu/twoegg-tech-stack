# Piccolo 主线 02：从启动走到第一帧

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定《从启动走到第一帧：初始化、世界加载与退出怎样衔接》，要求结合实际代码并更新导航。
- 类型：原理篇。问题为启动函数返回、世界状态可用、首帧工作和退出清理分别由谁推进，怎样判断完成。
- 先建立阶段与依赖模型，再落到 Piccolo 编辑器入口。主线编号仍为 02，网页按已完成的阅读顺序取 `series_order: 5`；01 构建与调试基线仍待写，不伪造运行前置。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，只读核对。用户选择先补文章，暂不构建、运行或调试 Piccolo。
- 范围覆盖入口、配置路径、系统初始化、编辑器循环、首次 tick 的世界加载、首帧交接与正常退出；不展开完整反射生成、所有组件和 Vulkan 同步审计。
- 产物为正文、本提纲、系列 plan、两个阅读首页、上一篇继续阅读与根路由的 Piccolo 行；复用已有 reading-series 样式。

## 正文提纲

1. 问题空间：初始化结束为什么还没有世界；函数名字不能直接充当完成证据。
2. 最小模型：基础设施、内容恢复、逐帧推进、显式清理；每阶段写清输入、依赖和可确认状态。
3. 从真实 main 进入：startEngine 承担主要初始化，initialize/clear 在该版本为空，入口运行 editor->run。
4. argv[0] 附近的 ini、BinaryRootFolder 和 DefaultWorld 怎样组成实际资源路径；区分开发与部署配置以及构建声明与现有产物。
5. startSystems 实际顺序与依赖；世界管理器先建立、渲染后建立，不表示场景已经加载。
6. 编辑器模式、管理器和 UI 接入；editor->run 调用 tickOneFrame，区分另一条 engine->run 入口。
7. WorldManager 首 tick 延迟加载：默认世界只加载 default_level_url；loadLevel 临时设置 active level 以支持组件取得物理场景。
8. 同一 tick 内世界加载、组件更新、逻辑渲染数据交接与渲染入口；编辑器组件筛选；世界成功标志不证明所有对象或屏幕画面就绪。
9. 失败传播边界：配置 void 返回、loadWorld bool 被 tick 忽略、Level::load 不汇总 createObject 失败、tickOneFrame bool 只表示是否继续循环。
10. 窗口关闭链：注册关闭函数与实际分发分开核对，当前 windowCloseCallback 只设置标志；退出回到 main。
11. shutdownSystems 完整实际顺序；世界清理依赖物理管理器；clear、reset、析构分别理解，正常路径不等于异常回滚或可重复关闭。
12. 静态阅读任务与后续断点表，明确未执行运行验证。

## 固定源码证据

路径相对于 Piccolo 仓库。

| 入口 | 核对内容 |
| --- | --- |
| `engine/source/editor/source/main.cpp`、`editor.cpp` | ini 定位、引擎与编辑器创建、运行入口与正常退出；编辑器模式和 tick |
| `engine/source/runtime/engine.cpp` | 注册、startSystems、空 initialize/clear、tickOneFrame、logicalTick、注销 |
| `engine/source/runtime/function/global/global_context.cpp` | 系统创建初始化和关闭的完整实际顺序 |
| `engine/source/runtime/resource/config_manager/config_manager.cpp` | 根目录与默认世界 URL 的解析，不把 void 返回当完整校验 |
| `engine/configs/{development,deployment}/PiccoloEditor.ini`、`engine/source/editor/CMakeLists.txt` | 两种根路径与构建后复制规则；不宣称已生成这些产物 |
| `engine/source/runtime/resource/asset_manager/asset_manager.cpp`、`.h` | 根目录拼接、资源文件读取、解析与反序列化入口 |
| `engine/asset/world/hello.world.json` | 默认关卡 URL 与关卡列表 |
| `engine/source/runtime/function/framework/world/world_manager.cpp` | 首 tick 加载、成功标志、临时 active level、未消费 loadWorld 返回值 |
| `engine/source/runtime/function/framework/level/level.cpp` | 物理场景先创建、对象恢复与失败处理、unload/clear |
| `engine/source/runtime/function/framework/object/object.cpp` | 对象与组件恢复、编辑器 tick 筛选 |
| `engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp` | postLoadResource 从 active level 取得物理场景 |
| `engine/source/runtime/function/framework/component/transform/transform_component.cpp`、`mesh/mesh_component.cpp` | 变换初始脏标记与网格渲染描述 |
| `engine/source/runtime/function/render/render_system.cpp` | 全局资源初始化与逐帧渲染入口，和世界加载区分 |
| `engine/source/runtime/function/render/window_system.h`、`.cpp` | GLFW 回调注册、关闭标志、pollEvents 与 shouldClose；关闭函数列表未被分发 |
| `engine/source/editor/source/editor_input_manager.cpp`、`editor_global_context.cpp` | 已注册但无分发的关闭函数；编辑器管理器显式删除 |

## 验证记录

- 写作前已核对固定 SHA 与源码工作区，未修改 Piccolo。
- GLFW 关闭标志与事件处理语义对照官方 Window guide；代码调用链以固定版本源码为准。
- 正文 5 个 C++ 代码块分别核对了 6 段源码：main、编辑器 run、世界 initialize/tick、loadLevel 与窗口关闭回调；去除空白与已说明省略的行注释后均匹配。JSON 示例与默认世界资源解析后的内容一致。
- 4 条完整顺序核对通过：startSystems、shutdownSystems、tickOneFrame 与 loadWorld。关闭函数列表的源码搜索确认：只存在注册与存储，没有分发读取；没有把未调用的处理函数画入退出路径。
- 24 个固定源码链接均对应本地基线中的文件或仓库目录；正文 8 个 relref 目标存在。新篇、架构地图、三篇 C++、渲染篇和两个阅读首页共 8 页、255 处生成后站内链接、锚点及资源引用检查通过。
- Hugo v0.158.0 本地构建退出码 0，生成 1349 页、52 个静态文件，ERROR 为零。
- 浏览器核对既有 reading-series 样式、引擎/Piccolo 面包屑、5 张表及 2 张 Mermaid SVG；图中文字可读，无图语法错误或页面整体横向溢出。
- 新篇上一篇指向 GFX-01，GFX-01 下一篇指向新篇；新篇返回系列指向 Piccolo 首页 chapters 锚点。两个阅读首页均有新链接，旧启动篇待写占位移除。主线 01 仍保留待写，网页阅读位置与主线编号分别说明。
- 源码工作区保持干净，未触及其他任务与前几篇的未提交成果，暂存区未改变。
- 未构建或运行 Piccolo；断点表是后续任务，不是执行记录。本次不提交或推送。
