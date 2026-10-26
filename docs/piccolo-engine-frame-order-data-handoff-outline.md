# Piccolo 主线 03：一帧怎样推进

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户确认落地已建议的主线 03《一帧怎样推进：调用顺序、状态交接与线程怎样核对》，承接启动篇并更新相关导航。
- 类型：原理篇。问题空间是系统调用的先后怎样决定本次读到哪份状态，以及如何从顺序调用继续核对任务、线程和 GPU 边界。
- 最小模型：调用入口、阶段顺序、状态的生产与消费、执行载体和完成条件。先区分这些关系，再读固定实现。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；只读核对，沿编辑器入口和已加载世界的正常循环讲解。首次加载链接主线 02。
- 网页阅读顺序为 `series_order: 6`，主线编号为 03；主线 01 仍待实践。日期为 2026-10-25。
- 不展开对象恢复、特定对象位置的逐值追踪或所有 Pass/Vulkan 同步；分别留给 04—07。用户选择先补文章，本次不构建或调试 Piccolo，不提交推送。
- 只新增正文与本提纲，修改上一篇继续阅读、两个阅读首页、系列 plan 与根路由 Piccolo 行。已有共享模板/CSS 和其他文章的改动保留。

## 正文提纲

1. 问题空间：系统都 tick 了为何仍可能读到旧状态；调用顺序、数据版本、线程执行分别回答不同问题。
2. 确定本篇的一帧：编辑器循环的一次迭代，区别引擎帧计数、物理步长、GPU 在途帧和显示刷新。
3. 从 editor.run 进入 tickOneFrame：编辑器状态、世界、运行时输入、FPS、交接、渲染、事件；可选物理调试分支注明。
4. Level/GObject 内部顺序：对象 unordered_map 没有业务排序保证；组件 vector 按存储顺序并受编辑器模式筛选；物理在对象与可选活动角色更新后。
5. 输入事件与输入 tick 分开追：WindowSystem 回调分发、按键位字段、鼠标角度准备、编辑器相机命令、游戏模式 Motor 消费。条件纸上案例不能推广成统一的实际输入延迟。
6. RenderSwapData 是增量请求集合；生产 Mesh 描述、交换索引、processSwapData 消费并 reset；optional 有值与内部队列非空的区别。
7. 两槽位表：初始 0/1、产生 D_n、交换、消费、下一轮；交换条件失败不换索引，无等待循环。引用身份跟随槽位，不自动跟随角色。
8. RenderSystem 内部是消费、准备和渲染入口；UI preRender 在绘制路径内；交换链重建可提前返回，CPU 帧结束不等于画面已呈现。
9. 时间模型：外层 calculateDeltaTime；物理固定 1/update_frequency，默认 60Hz，但本函数没有累积/追赶循环；FPS 是平滑统计，不是各系统耗时证据。
10. 线程核对：外层逻辑与渲染顺序调用；物理接入 Jolt 线程池，依赖库创建线程并在 PhysicsSystem::Update 内 WaitForJobs；GPU 提交与 CPU 线程分别观察。双缓冲不提供独立并发协议。
11. 静态练习与后续调试观察表：入口栈、模式、对象组件次序、槽位/optional、任务线程/等待与提交。明确没有本次实测时间线。

## 固定源码证据

| 路径（相对 Piccolo） | 核对范围 |
| --- | --- |
| `engine/source/editor/source/main.cpp`、`editor.cpp` | 当前入口及编辑器循环；模式与组件注册 |
| `engine/source/runtime/engine.cpp`、`.h` | delta、帧计数/FPS、logicalTick、交换、渲染、事件与关闭 |
| `engine/source/runtime/function/framework/world/world_manager.cpp` | 世界延迟加载与活动关卡更新 |
| `engine/source/runtime/function/framework/level/level.cpp`、`.h` | 对象容器、角色与物理 tick 顺序 |
| `engine/source/runtime/function/framework/object/object.cpp`、`.h` | 组件容器、顺序和模式筛选 |
| `engine/source/runtime/function/input/input_system.cpp`、`.h` | 回调写按键与位移、tick 计算角度并清位移、焦点有效性 |
| `engine/source/editor/source/editor_input_manager.cpp`、`editor_scene_manager.cpp` | 编辑器命令消费、相机移动与选中对象脏标记 |
| `engine/source/runtime/function/framework/component/motor/motor_component.cpp` | 游戏模式活动角色路径消费输入命令与转向数据 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | 增量描述产生与脏标记清除 |
| `engine/source/runtime/function/render/render_swap_context.h`、`.cpp` | 七项 optional、两个槽位、就绪检查、重置及索引交换 |
| `engine/source/runtime/function/render/render_system.cpp` | 增量消费/reset、渲染 tick 内部顺序 |
| `engine/source/runtime/function/render/render_pipeline.cpp`、`passes/ui_pass.cpp` | 等待、重建返回、绘制/UI、提交及后续粒子工作 |
| `engine/source/runtime/function/render/window_system.h`、`.cpp` | 事件轮询、键盘回调分发 |
| `engine/source/runtime/function/physics/physics_scene.cpp`、`physics_config.h` | 固定物理步长、线程池参数、Update 接入 |
| `engine/3rdparty/JoltPhysics/Jolt/Core/JobSystemThreadPool.cpp` | 线程创建入口 |
| `engine/3rdparty/JoltPhysics/Jolt/Physics/PhysicsSystem.cpp` | Update 的任务 barrier 与 WaitForJobs |

## 外部规则核对

- GLFW 官方 Input guide：pollEvents 处理已收到的事件；其他窗口调用也可能同步触发回调，不能写成回调只能发生在帧尾。
- Khronos vkQueueSubmit：提交与设备执行完成分开；本篇只解释 Piccolo 使用的接口语义。

## 验证记录

- 写作前回读固定 SHA，Piccolo 源码工作区干净。
- 正文 5 个 C++ 代码块，去除空白与已说明省略的空行后，均与固定源码匹配；27 个固定源码链接对应文件或仓库目录均存在，7 个正文 relref 目标存在。
- 8 项入口或顺序核对通过：编辑器 run、tickOneFrame、logicalTick、Level tick、RenderSwapContext swap、RenderSystem tick、Jolt 线程池构造入口、Jolt Update 的 barrier/WaitForJobs/销毁路径。processSwapData 的七项 reset 入口均已核对。
- 两槽位表按初始索引 0/1、生产、交换、消费和复用人工推导；单独说明 optional 有值但内部队列为空时仍不满足交换条件。没有把纸上推导当成引擎执行测试。
- Hugo v0.158.0 本地构建退出码 0，生成 1350 页、53 个静态文件，ERROR 为零。
- 新篇、启动篇、渲染篇、三篇 C++、架构地图与两个阅读首页共 9 页，285 处生成后站内链接、锚点与资源引用检查通过。新篇生成 4 张 HTML 表及 2 段 language-mermaid 图源，沿用现有 series-reading.css。
- 生成 HTML 确认：02 下一篇指向 03，03 上一篇指向 02，返回系列指向 Piccolo 首页 chapters；两个阅读首页均有新入口，原两条一帧/更新顺序待写占位移除，无未解析 relref。
- 浏览器尝试绑定用户当前本地预览页时，被 URL 安全策略拒绝，未取得页面观察权限；没有改换浏览器、地址或其他执行通道绕过。故未执行浏览器目视、响应式溢出或 Mermaid SVG 实际渲染检查，静态检查不代替这些结果。
- 7 个任务文件无尾随空格或冲突标记，已有跟踪文件 diff --check 通过；Piccolo 源码工作区仍干净，未改变暂存区。其他任务的共享模板、样式和文章改动保留。
- 本次没有 Piccolo 构建、断点、线程采样或 GPU 捕获；纸上槽位表与输入案例均不是运行记录。


## Debug 首帧实测补充（2026-10-26）

03 新增实际槽位0/1→1/0、槽0对象描述有值→nullopt及主链同线程0x2784c记录。没有逐项读取七个optional、输入消费者、Jolt任务或GPU时间线；第二次采样后由CDB终止，正常退出证据仅来自02的独立运行。

固定输入仍为 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5，另加两处明确补丁；与此前仅源码/配置生成阶段区分。详见 [共享执行记录](piccolo-debug-first-frame-validation-outline.md) 和 [复现材料](../examples/piccolo-debug-validation/README.md)。历史验证记录保持其原时间范围，本节更新当前状态。
