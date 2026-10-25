# GEF-02：游戏为什么能够持续运行：启动、一帧与退出

> 执行层提纲、复用盘点与验证记录。篇级状态由 [公共基础课程 plan](game-engine-foundations-series-plan.md) 维护。

## 定位与边界

- 用户已确认原理优先、三引擎衔接、最小示例与自检练习；特别要求先核对 Piccolo 已有流程，避免重复起稿。
- 读者已做过游戏。类型为原理篇，最低前置为 GEF-01 的架构地图；不以编译 Piccolo、掌握 C++ 或完成图形课为条件。
- 主问题：运行需要哪些状态，时间如何推进，失败或退出后怎样结束已建立的责任。
- 新正文：`content/engine-toolchain/game-engine-foundations-02-startup-frame-shutdown.md`。
- 示例由本篇自写，采用 Python 3 和给定时间序列，便于独立运行；不是任何引擎源码的翻译。先用纸上表格解释，再给程序。
- 本轮只写正文、本提纲，更新公共课 plan 与根路由对应行；Piccolo 文章和其原系列归属保持原样。没有新的提交、推送授权。

## Piccolo 已有内容与复用方式

| 已有材料 | 当前正文已讲清的范围 | 本篇如何使用 / 还需补什么 |
| --- | --- | --- |
| [主线 02：启动与首帧](../content/engine-toolchain/piccolo-engine-02-startup-first-frame.md) | main、系统初始化、编辑器入口、首 tick 世界加载、关闭标志与正常退出；失败传播和清理限制 | 链接具体实现，不重抄调用链；补引擎无关的启动状态、依赖、部分失败回滚与运行停止协议 |
| [主线 03：一帧推进](../content/engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md) | 外层顺序、字段生产消费、两槽位交接、delta/FPS/物理步长、内部任务 | 衔接实例；补固定步长与调用频率的区别、累积账本、插值时间与追赶策略的完整数值例子 |
| [PCPP-2：窗口回调](../content/engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md) | 注册与实际分发、捕获寿命、关闭列表未分发、注销边界 | 退出段链接；不在公共课重做 C++ 可调用对象教程 |
| [PCPP-3：状态缓冲与线程](../content/engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md) | 值与借用、交接/复用、Jolt 和日志线程、等待与关闭 | 停止/完成边界的后续；公共课只定位，不展开同步原语 |
| [主线 09：输入到位移](../content/engine-toolchain/piccolo-engine-09-input-motor-character-movement.md) | 回调写命令、Motor 消费、delta_time 与目标位移、组件状态时序 | 作为延伸练习，不复制输入与角色专项 |
| [Game Loop 旧文](../content/system-design/patterns/patterns-29-game-loop.md) / [一帧拆解](../content/performance/game-performance-frame-breakdown.md) | 累积器、固定步/插值示例；CPU/GPU 与等待成本 | 用作选读；新文独立核对数值与因果，不沿用把变量步一概判错或把固定步当确定性保证的说法 |

盘点依据是 2026-10-25 当前工作区正文，不等于所有本地稿已推送或上线。Piccolo 02/03 的固定源码核对与独立语言例子，也不等于完整引擎运行、输入延迟或 GPU 验证。

## 正文结构

1. 熟悉的三个现象：初始化返回但内容未就绪、帧率下降后补步、关闭后仍有工作；建立状态/时间/责任三条线。
2. 已有 Piccolo 文章的职责表，说明本篇补公共模型，具体函数路径仍回原文。
3. 启动：状态图、依赖图与可用条件；初始化对象和内容加载可分阶段，已成功步骤的清理与失败步骤内部回滚分工。
4. 一帧：外层迭代、模拟步、表现提交、显示刷新分别命名；串行模型只是示例，事件宿主与后台等待另行说明。
5. 时间：真实经过时间、接纳给模拟的时间、已模拟时间、累计余量；变量步也有合理场景，乘 dt 不能消除全部数值误差。
6. 固定步：20 ms、[16, 16, 50, 8] ms 逐行推算；previous/current 插值及一个固定步的表现时间滞后，初始无历史单独说明。
7. 过载与暂停：限步后丢弃整步欠账并保留余量是本例策略；与保留欠账、限制接纳时间比较；暂停不累计，恢复重设采样基准。
8. 退出：请求停止、停止接单、完成/取消在途访问、按依赖释放；正常关闭、失败回滚、强制终止不同。
9. 一个可直接运行的 Python 程序：启动记录与 finally、时间表、故障注入、关闭与暂停；日志不假装创建真实窗口或 GPU 资源。
10. 三引擎映射：Piccolo 固定源码；Unity 6.0 PlayerLoop/固定更新/退出限制；UE 5.6 Actor 生命周期/Tick 组与依赖。每段给观察问题，不声称教学例子复刻引擎。
11. 自检与参考答案：累积/插值、失败清理、过载代价、暂停、固定步不等于固定调用频率、同名生命周期不等于同一保证。

## 已核对的原始资料

- Piccolo 固定提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`：通过 GitHub 文件读取重新核对 `runtime/engine.cpp`、`runtime/function/global/global_context.cpp`、`runtime/function/framework/world/world_manager.cpp`、`runtime/function/physics/physics_scene.cpp`、`editor/source/editor.cpp`。确认编辑器入口、首 tick 加载、帧末事件轮询、外层 steady_clock、物理 tick 的固定参数与未含累积追赶循环。未运行 Piccolo。
- [Unity 6.0 Event function execution order](https://docs.unity3d.com/6000.0/Documentation/Manual/execution-order.html)：生命周期图只覆盖 PlayerLoop 的部分阶段，不能由图推成整个循环。
- [Unity 6.0 Fixed updates](https://docs.unity3d.com/6000.0/Documentation/Manual/fixed-updates.html)：每显示帧的固定更新次数可以不同，真实调用间隔不保证固定，追赶有上限。
- [Unity 6.0 OnApplicationQuit](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html)：平台和应用挂起/终止限制。
- [UE 5.6 Actor Ticking](https://dev.epicgames.com/documentation/en-us/unreal-engine/actor-ticking-in-unreal-engine?application_version=5.6)：Tick 组与先决依赖，Tick 不等于整台引擎循环。
- [UE 5.6 Actor Lifecycle](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle?application_version=5.6)：加载、PIE、生成入口；BeginPlay/EndPlay 与内存回收分开。
- [Glenn Fiedler：Fix Your Timestep!](https://gafferongames.com/post/fix_your_timestep/)：阅读固定步与累积/插值的原始解释作为交叉参考；本篇数值案例与程序独立设计，逐项验证。
- 未取得有效正文的旧 Unity TimeFrameManagement URL 与 UE Game Flow URL 不作已核实引用。Unity/UE 只使用公开文档，不读取或引用私有引擎 C++ 源码。

## 验证设计与记录

2026-10-25 从正文唯一 Python 代码块原样提取，以 Python 3.12.4 编译执行：

- 正常输出全文与正文 text 块逐字一致，四行状态与两张数值表逐项一致；浮点比较使用绝对误差 `1e-10`。
- 分别在 window、renderer、world 注入启动失败：失败步骤打印自身部分回滚，外层只清理成功步骤，均未进入帧循环。
- 过载：第五轮 `sim=140, left=10, drop=40, shown=1.30`，满足 `190=140+10+40`。
- 暂停：500 ms 不接纳，恢复后的 `sim=40, left=12, shown=0.32`；保持暂停前的时间余量。
- 主动退出：第一轮后看到 quit，退出项与后续项均不模拟，按 world → renderer → window 清理。
- 自检第 1 题：模拟次数 0/1/0，余量 10、表现 0.1；追加到六个 10 ms 间隔后模拟 60 ms、表现 0.4，确认插值没有改变后续模拟起点。
- 非法运行模式抛出 ValueError 后仍执行三项清理。
- 正文 15 处 relref、11 个唯一目标均存在，代码围栏成对。

- Hugo v0.157.0 本地构建退出码 0，生成 1379 页、54 个静态文件，无 ERROR。输出到独立临时目录，未写仓库 public/resources。
- 新正文生成页存在，标题、示例输出、自检及答案进入 HTML；23 处站内链接与锚点均有真实目标，7 张表存在。课程 plan 与本提纲的相对链接均通过文件检查。
- 本地浏览器读取整页结构，并目视核对状态表与完整 Mermaid 状态转换图：5 个节点、启动失败/退出分支和文字正常显示。没有把 HTML 中未出现 Mermaid 容器误判为渲染失败，实际图由前端脚本生成。
- 本轮改动范围为新正文、新执行提纲、公共课 plan 和根路由的本课程一行；既有 Piccolo 正文未修改，其他工作区改动保留。

以上为写作阶段记录，尚未构建、运行或采样三引擎，没有设备性能、实际引擎画面或线上部署结论。

### 限定范围交付检查

- 用户随后授权仅提交 GEF-02 正文、执行提纲和对应规划更新。正文原引用的 Piccolo PCPP-2、PCPP-3、09 尚未进入 HEAD；保留本提纲的工作区材料盘点，正文只链接已提交的 Piccolo 02/03 与阅读入口，避免将其他稿件带入本次交付。
- 正文现在有 13 处 relref、9 个唯一目标，全部存在于已提交内容中；上面的 15 处引用和整工作区构建记录属于交付修订前版本。
- 以 `a0dd5ab45f4a3294004ae227a95c6d5eda1176aa` 的 Git archive 内容，叠加新正文、执行提纲、课程 plan 和根路由单行补丁，独立执行 Hugo v0.157.0：退出码 0，生成 1368 页、54 个静态文件，无 ERROR。新页 21 处站内链接与锚点全部存在。独立示例代码没有修改，沿用上面的程序验证结果。
