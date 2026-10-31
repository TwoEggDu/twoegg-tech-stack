# 一帧、呈现与帧预算关联审校记录

> 2026-10-31。复用现有文章与 URL，只落实本次帧定义、呈现、指标、关键路径及必要关联修正；不新开系列，不提交或推送。既有工作区其他改动保留。

## 范围与发现

主文为 `content/performance/game-performance-frame-breakdown.md`；关联文为 `content/performance/game-budget-06-frame-budget-breakdown.md`。同步性能系列索引、性能与预算 canonical plan、FDE 基础与案例执行对照表。本记录不接管篇级规划。

| 原位置／表述 | 风险与修正 |
| --- | --- |
| 帧预算段：“每 16.67ms 必须把这一轮该结算的账结完”及“从输入到显示的接力” | 将目标输出间隔与单次反馈延迟混用；新增逻辑／渲染／显示对象和独立指标表 |
| CPU N、GPU N-1、显示 N-2 图 | 易被理解成固定帧偏移；显式标为非定比例示意，补流水与非流水模式边界 |
| 任务段：“结果不必在同一帧立刻消费”作为并行价值的必要前提 | 同帧消费也可受益于重叠；改按依赖期限、实际重叠和调度成本判断 |
| 提交段：“只要同步边界出现在这里……从并行变成串行” | 同步限制具体依赖，不等于停住全部并行；改查范围及目标关键路径 |
| Present 段：等待意味着节奏没对齐 | 等待可为正常节奏控制；区分接口返回、渲染完成、系统呈现与屏幕反馈 |
| 指标口径缺项 | 补 CPU 墙钟与运行时间、GPU 范围、呈现间隔、输入起点、加载用途；统计对象、分位数算法、窗口和事件定义分别记录 |
| 主文首次成本、平台差异等经验性归因 | 改为调查候选与需记录的条件，不预定哪个阶段最贵，不认领平台发生率 |
| 预算 06 的五项分账与等待“空转” | 加载是事件维度，父子、并行和等待区间可重叠；补分项记录表、正常等待及团队责任，不直接求和 |

## 核对依据

以下为 2026-10-31 查阅的官方资料。正文只复用所列机制，不引用设备收益数字或受限引擎源码。

- [Unity 6.0 Highlights](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/visualizing-data/highlights)：CPU Active Time 的模块口径及 CPU／GPU 并行预算。它不是系统级各核运行时间总和。
- [Unity 6.0 Profiler markers](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/markers)：等待标记须结合上下文与其他线程；Editor／Player 观测有别。没有将单个标记直接判为根因。
- [Android Frame Pacing](https://developer.android.com/games/sdk/frame-pacing)：重复旧图像、排队与反压、有意安排等待、pipeline／non-pipeline 模式。具体模式不推广为所有平台保证。
- [Vulkan vkQueuePresentKHR](https://docs.vulkan.org/refpages/latest/refpages/source/vkQueuePresentKHR.html)：排队呈现、信号量依赖与可能阻塞。采用其公开接口契约，没有运行 Vulkan 实验。
- [DXGI Present](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present)：flip 模式的 sync interval 与队列丢弃示例，只用于说明渲染输出不保证逐份显示。
- [Epic Threaded Rendering](https://dev.epicgames.com/documentation/en-us/unreal-engine/threaded-rendering-in-unreal-engine)：访问时页面标 UE 5.8；只映射 Game／Rendering Thread 的职责和交接，不采用旧宏或固定落后帧数。

## 教学模型与未验证范围

软件输入在 1 ms 到达，主线程 2–4 准备，独立工作 4–8，Worker 4–11，合并提交 11–13，GPU 13–20。人为设定固定选图点 0／16.67／33.33 ms、无额外排队或合成成本，因此选择包含反馈图像的时刻为 33.33 ms，软件事件至选图为 32.33 ms。

缩短非关键分支不改变 20 ms 就绪；Worker 改为 4–7 后就绪为 17 ms，在本模型仍错过 16.67。算例用于解释依赖和期限，不是实际调度、屏幕扫描或像素响应模型。没有设备 trace、输入到光子延迟、优化收益、真实项目或持续门禁验证。

## 本地验证与交付

本轮实际结果：

- `python -m unittest discover -s tests -p test_reading_layouts.py -v`：19 项通过。
- `hugo --gc --minify --destination .tmp/frame-audit-20261031/public`：1443 页构建成功；最终正文空白修正后已刷新构建。
- `python scripts/check-reading-pages.py --public .tmp/frame-audit-20261031/public`：1377 个阅读页，0 errors。
- 本机 Hugo 预览已目视检查指标表、教学时间线与目录；主文 → 帧预算 → 主文 → 性能索引 → 主文跳转正常。未改样式，未做本轮窄屏验证。
- 教学三组时刻复算：就绪时间分别 20／20／17 ms，按设定均在 33.33 ms 选图；这是算术核对，不是性能测试。
- 本轮精确增量及前后快照保存在 `.tmp/frame-audit-20261031/`。性能 plan 原有未提交段落保留；并行的渲染补篇 I 与其规划改动不纳入本轮。

本地检查基于当前工作区，含其他尚未交付内容；以后提交时仍需按精确范围隔离候选。当前未提交、未推送、未部署。
