# Piccolo PCPP-3：状态缓冲怎样交接，哪些地方真的跨线程

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定 PCPP-3，延续先补文章、后调试引擎；原理篇，以读懂发布、消费和复用的协议为目标。
- 主线 03 已展开循环与槽位表，06 已追 Wall_1 数值，07 已追 Vulkan；本篇补“C++ 值/借用、角色/物理槽位、可见性/复用完成”之间的桥，不重复整个主循环教程。
- 固定公开源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读；线程结论来自线程创建、任务执行和等待入口，不来自类名或配置字段名。
- 网页插在 PCPP-2 后、GFX-01 前，series_order 为 6；公共 CXX-04/06/11 的完整单元仍保留自身状态。
- 独立 C++17 模型验证同线程角色交接，并用带 mutex/condition_variable 的单槽通道说明真实跨线程所需协议；不把该通道写成 Piccolo 已有实现，不执行数据竞争示例。
- 产物为正文、提纲、相关阅读入口与规划更新；不修改引擎、共享模板和 CSS，不提交推送。

## 正文提纲

1. 两份数据与两个线程之间没有自动对应；先问承载什么、谁能访问、何时发布与复用。
2. 对照 Transform 两份状态、RenderSwapContext 两份描述、Vulkan 三份在途帧资源，各自索引和完成条件分开。
3. Transform setter/tick、Mesh 数值描述和 GameObjectDesc 的容器复制；具名右值引用参数不等于自动 move，更不等于异步发送。
4. getter 返回物理槽位引用；交换角色不重绑定引用。optional 有值和内部队列有元素分别判断；消费 pop 后还要 reset。
5. 消费后的 RenderEntity 是另一份表示，可见节点又借其地址；同线程分阶段协议支撑借用，不能随意移到线程上。
6. 跨线程要同时证明 payload 可见、消费完成后才复用；只给索引加 atomic 或使用 volatile 都不构成完整方案。
7. Jolt：默认配置 4 传入线程数参数，确实创建 worker，Update 任务路径有 barrier/WaitForJobs，调用线程也可执行任务，无活动刚体时另有提前返回路径。
8. spdlog：线程池容量 8192、1 个 worker，消息拥有正文缓冲，阻塞队列配 mutex/cv；async 满队列仍可阻塞，flush 入队不等于 drain/join。
9. GPU：vkQueueSubmit 与 fence/semaphore 完成条件；CPU 描述 reset 不等于设备已结束，当前帧资源索引不等于 CPU 交换槽或交换链图像索引。
10. 两段合一的独立 C++17 练习：复制后的值、旧引用与角色交换、空但有值的 optional；真实两线程交接、关闭和 join。只验证有效访问与确定性断言。
11. 运行证据计划：记录线程 ID、槽位地址/角色、任务入口和 GPU 提交标识；异步日志显示顺序不替代跨线程时间线。

## 固定证据入口

| Piccolo 相对路径 | 核对内容 |
| --- | --- |
| `engine/source/runtime/engine.cpp` | logicalTick → swap → rendererTick 顺序 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h`、`.cpp` | 当前/下一份状态、持久字段与 setter/tick |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | 复制 part 描述、恢复局部矩阵与提交变化 |
| `engine/source/runtime/function/render/render_object.h`、`render_swap_context.h`、`.cpp` | 描述值、两个槽位、引用、具名参数与复制、七项 optional 就绪条件 |
| `engine/source/runtime/function/render/render_system.cpp`、`render_scene.h`、`.cpp` | 消费复制、pop/reset、RenderEntity 与可见节点借用 |
| `engine/source/runtime/function/physics/physics_config.h`、`physics_scene.cpp` | 默认 4、线程池构造与 Update |
| `engine/3rdparty/JoltPhysics/Jolt/Core/JobSystemThreadPool.h`、`.cpp` | worker 创建、执行、等待协作和停止 join |
| `engine/3rdparty/JoltPhysics/Jolt/Physics/PhysicsSystem.cpp` | 无活动体分支、任务 barrier 和 WaitForJobs |
| `engine/source/runtime/core/log/log_system.cpp` | 8192/1、async logger、block 策略与 flush |
| `engine/3rdparty/spdlog/include/spdlog/details/thread_pool-inl.h`、`log_msg_buffer-inl.h`、`mpmc_blocking_q.h`、`../async_logger-inl.h` | 真正线程入口、消息拥有正文、队列同步、flush 入队和线程池 join |
| `engine/source/runtime/function/render/render_pipeline.cpp`、`interface/vulkan/vulkan_rhi.h`、`.cpp` | 帧 fence 等待、提交、呈现与在途帧索引 |

规则核对 C++ 标准工作草案的 intro.races、mutex、condition_variable、thread::join 与 move helper，以及 Khronos vkQueueSubmit/vkWaitForFences 主文档；不把新版本附加能力套回固定源码。

## 验证记录

- 回读固定源码 SHA 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，克隆工作区保持干净；正文 11 个原文 C++ 片段去除空白后匹配固定源码，26 个固定版本链接和正文 relref 目标存在。
- 核对 Jolt 默认配置 4 到线程数实参、实际 worker 创建、任务执行、barrier 等待/销毁与无活动刚体分支；核对 spdlog 8192/1、block 策略、消息缓冲与队列同步、flush 入队及线程池 join；Vulkan 在途帧资源数量为 3，提交与 fence 等待分别核对。
- 从正文直接提取完整程序到 `%TEMP%/TechStackShow-piccolo-handoff-pcpp3-20261025/main.cpp`。Windows x64、MSVC 14.51.36231，使用 `/std:c++17 /utf-8 /EHsc /W4 /Od /MDd /D_ITERATOR_DEBUG_LEVEL=2`；编译退出 0、无警告，运行退出 0，断言与四行输出通过。
- 独立程序覆盖描述值复制、槽位角色切换后引用仍指向原元素、空但有值的 optional 阻止交换、两个 std::thread 通过受锁保护的单槽通道传递 1/2/3、关闭后排空与拒绝后续发布。主线程在 join 后读取结果，未执行数据竞争或失效引用；最终正文程序与实际编译源逐字一致。
- Hugo v0.158.0 最终构建退出 0，ERROR 为零，生成 1369 页、54 个静态文件；输出与缓存为上述临时目录下的 public-final/cache-final。早次生成页检查结果与最终构建的 20 个相关页面逐字节一致。
- 当前系列含同期新增的 GFX-02，共 17 篇正文，series_order 连续唯一为 0～16；本篇位于 PCPP-2 与 GFX-01 之间，GFX-02 仍在主线之后。保留 GFX-02 原文、验证范围与入口，仅因插入本篇顺延导航 metadata。
- 17 篇正文、两个阅读首页和公共 C++ 入口共 20 个生成页，1202 处站内链接、锚点及资源引用检查通过；实际上一篇/下一篇 PCPP-2 ↔ PCPP-3 ↔ GFX-01 双向通过。新页含一段 Mermaid 图源并沿用 series-reading.css；静态检查不替代浏览器目视或 Mermaid 实际渲染。
- 相关入口、03/06/07 与 PCPP-2 回链完成；专项 canonical 表将三项登记为正文已写，公共 CXX-04/05/06/11 保持自身待写状态；保留同期 CXX-01 已成稿和 GFX-02 的规划更新。根 doc-plan 仅维护系列汇总。
- 限定任务路径的 git diff --check 通过，暂存区为空。未改引擎源码、共享模板/CSS，未暂存、提交或推送；共享工作区其他内容保留。
- 未运行 Piccolo、Jolt、spdlog 或 Vulkan，也未取得引擎断点、线程采样、GPU 捕获和显示时序。源码、独立模型与引擎运行证据分别登记，三项专项正文齐备不等于整个基础课程或引擎实践验收完成。

## A12 原始回执复核（2026-10-25）

只读复核既有`%TEMP%/TechStackShow-piccolo-baseline-01-20261025/verification-receipt.json`及`build-clean-debug.log`。固定revision仍f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；历史干净Debug配置0、生成目标0、完整编辑器构建1，原始日志仍含Jolt的C3688 us字面量和fmt的C2653/checked_array_iterator错误。四份代表reflection/serializer/shader生成文件的当前SHA256均与回执相同。

定向检查原source/bin与build-vs18-debug-clean下没有PiccoloEditor.exe，故没有新的引擎运行、事件退出、线程/Trace或GPU捕获。这里是复核旧回执，不声称本轮又做了一次构建。未改源码/工具链时重跑相同已知失败不会产生新证据；不改第三方依赖或安装系统组件来制造通过。下一步是单独解决该固定revision与工具链兼容，再新建build目录重建并保留完整身份，之后执行PCPP各运行矩阵。
