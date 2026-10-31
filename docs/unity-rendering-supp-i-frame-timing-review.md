# 帧时序与显示技术：原位修订审校记录

日期：2026-10-31。对应[原文](../content/rendering/unity-rendering-supp-i-frame-timing.md)与[系列计划](unity-rendering-system-series-plan.md)。本文件为执行记录，不新增文章或篇级目录。

## 范围与复用

按用户确认，原位补清帧时间、显示节奏和输入延迟的测量口径，校正与这些口径直接相关的同步、缓冲、移动端设置及计时示例。保留标题、slug、date、series、tags、weight 和原章节顺序，增加口径节、更新 description 与 lastmod。只维护上述正文、系列计划的既有补篇条目及本记录，不修改根路由、缺口表或其他正文。

复用《游戏性能：一帧链路》的阶段模型与《帧预算拆账》的责任划分，本篇负责测量边界、证据关联和显示端条件；移动端配置复用已有 iOS／Android 专项，不重复开篇。教学数字与依赖图、指标卡、最小验证清单均不是作者项目实测或已实施系统。

## 官方核对与条件

| 主题 | 官方依据 | 本次处理与边界 |
| --- | --- | --- |
| CPU／GPU 字段、支持与返回延迟 | [Unity 2022.3 Frame Timing Manager](https://docs.unity3d.com/2022.3/Documentation/Manual/frame-timing-manager.html)、[FrameTiming](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTiming.html) | CPU 帧时间含等待与开销，各区间不能重复相加；Apple TBDR 与 Linux OpenGL 限制单列。四帧返回延迟是该版手册描述，不当作输入延迟或准确性保证 |
| 示例 API 与完成帧 | [CaptureFrameTimings](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.CaptureFrameTimings.html)、[IsFeatureEnabled](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.IsFeatureEnabled.html)、[GetLatestTimings](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.GetLatestTimings.html) | 返回数量可小于请求，最近样本属于已完成帧；先检查功能与数量，未知／非有限值不填零，不逐帧日志，不冒充统计器。统计需另行确认源帧、去重及缺失 |
| 平台帧率控制 | [Application.targetFrameRate（2022.3）](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-targetFrameRate.html) | 桌面／Web、移动端、XR 分开；移动端忽略 vSyncCount，-1 默认目标 30fps；目标不等于实测，不把固定刷新整除算例外推所有动态刷新设备 |
| 呈现与缓冲 | [Vulkan VkPresentModeKHR](https://docs.vulkan.org/refpages/latest/refpages/source/VkPresentModeKHR.html)、[DXGI flip model](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model) | 请求、GPU 完成、显示采用与像素变化分开；FIFO／Mailbox／Immediate 为具体模式契约，不证明当前 Unity 后端选择。图像数量不等于队列策略；删除普遍 60→30 悬崖及固定一帧延迟断言 |
| VRR 条件 | [NVIDIA G-SYNC 设置](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nvdsp/To_use_variable_refresh_rates.htm)、[AMD FreeSync FAQ](https://www.amd.com/en/products/graphics/technologies/freesync.html) | 按显示器、连接、驱动、窗口与范围核对；LFC 重复帧不等于生成新帧。不预填固定范围、价格、延迟或通用范围外行为 |
| 输入处理与端到端口径 | [Input System UpdateMode](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/api/UnityEngine.InputSystem.InputSettings.UpdateMode.html)、[NVIDIA 系统延迟说明](https://www.nvidia.com/en-us/geforce/guides/system-latency-optimization-guide/) | 包文档元数据为 1.6.3，动态／固定／手动和 BeforeRender 分开；移除泛化的 Low-Latency Input 开关与 XR 固定收益。端到端需约定物理输入和目标像素阈值，不用软件子区间替代 |
| Android 节奏与刷新请求 | [Frame Pacing](https://developer.android.com/games/sdk/frame-pacing)、[Frame rate API](https://developer.android.com/media/optimize/performance/frame-rate) | 核对 Unity 配置及实际 API／设备，不以集成年份证明启用与收益；原生刷新请求不保证满足，不直接叠加成 Unity 配方 |
| 系统轨迹的覆盖 | [Perfetto FrameTimeline](https://perfetto.dev/docs/data-sources/frametimeline) | 文档要求 Android 12+，并列 SurfaceViews 不支持的限制；先检查当前 trace 的实际覆盖，不保证 Unity Surface 完整可见，不当作物理输入到像素测量 |

官方网页核对日期为本记录日期。Vulkan／Windows／厂商和系统工具资料用于说明各自契约或条件，不证明当前 Unity 工程配置、设备行为或收益。iOS ProMotion 配置直接复用已核对的 iOS 专项，本次不新增 Apple 配置结论。

## 验证与未核验范围

- 静态核对示例的命名、参数、返回数量与无数据处理；没有目标 Unity 2022.3 工程的编译／Player 运行记录，没有执行设备采样、GPU／系统轨迹、VRR、功耗、输入或显示实验。
- 示例仅展示最近可获得值，零／非有限值保守报告未知，正数不保证准确。最小验证清单和 DATA-TODO 明确保留未执行项目，不填虚构结果。
- 教学算例用 Python 复核：两组各两个间隔、总长 32ms，均为 62.5 次／秒、平均间隔 16ms；逐间隔求倒数再平均不等于同一窗口速率。最近秩法的示意索引与代码无数据保护做了静态检查，不将这些检查冒充 Unity 编译或设备实验。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 构建：`hugo --gc --minify --destination .tmp/frame-timing-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页：`python -B scripts/check-reading-pages.py --public .tmp/frame-timing-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML 检查：四张表格、计时代码与新增口径／工具选择小节已生成；一帧链路、帧预算与 iOS／Android 专项链接均解析且目标存在，官方链接完整，无残留 shortcode，DATA-TODO 不显示为正文。没有改样式／模板，本次未做浏览器视觉验收。
- 文章 title／slug／date／series／tags／weight 与原章节顺序保持；description 与 lastmod 为本次维护。正文与系列计划保留原 LF，差异空白检查通过，暂存区与任务前一致。
- 工作区同期出现一帧链路、帧预算、性能入口及相关计划的其他任务更新，均保留。复核其当前阶段划分、不重复记账及呈现／反馈边界后，与本篇复用分工一致。本次只写正文、Unity 渲染系统系列计划的补篇维护条目及本记录。
- `.tmp/frame-timing-20261031/` 保存修改前快照、静态／生成页检查、范围核对及单文件增量，供后续核对本次范围；不是发布产物。
- 本次不提交、不推送；工作区无关改动保留。
