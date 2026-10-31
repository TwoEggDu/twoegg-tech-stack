---
title: "Unity 渲染系统补I｜帧时序与显示技术：VSync、Frame Pacing、输入延迟"
slug: "unity-rendering-supp-i-frame-timing"
date: "2026-03-26"
lastmod: "2026-10-31"
description: "区分应用帧间隔、CPU/GPU 计时、实际显示节奏与输入到显示延迟，解释同步、缓冲和刷新率的取舍，并按 Unity 2022.3 核对计时样本、平台条件与验证边界。"
weight: 1580
tags:
  - "Unity"
  - "Rendering"
  - "VSync"
  - "Frame Pacing"
  - "显示技术"
  - "输入延迟"
  - "G-Sync"
series: "Unity 渲染系统"
---
“我们跑满 60fps”，还不足以说明画面间隔稳定、输入反馈及时。先确认这个 fps 数的是应用循环、提交帧，还是实际显示的新画面；再讨论同步与缓冲设置，否则团队可能拿着不同口径的数字验收同一个问题。

> 版本与证据：Unity API 以 2022.3 LTS 为基线，Input System 单独固定为 1.6.3 文档。本文核对官方契约；数字与时间线为教学算例，没有设备帧采样、显示延迟或优化收益实测。计时示例尚未在目标 Unity Player 运行。

一帧经过哪些系统，复用[一帧链路]({{< relref "performance/game-performance-frame-breakdown.md" >}})；逻辑、提交、GPU、加载与等待的责任划分，复用[帧预算拆账]({{< relref "performance/game-budget-06-frame-budget-breakdown.md" >}})。本篇补的是这些阶段怎样被度量，以及哪些结论仍需要显示端证据。

## 先统一口径：算的是哪段时间

每个指标都记录对象、起止点、单位、样本数、窗口与采集方式。以下口径不能因为都叫“帧时间”就互换。

| 指标 | 测量对象与边界 | 不能直接推出什么 |
| --- | --- | --- |
| 应用帧间隔 | 同一种应用循环标记连续两次发生的墙钟间隔，ms | 不等于纯 CPU 运算，也不保证显示端同样更新 |
| CPU／GPU 计时 | 按工具定义的线程／GPU 工作区间；具体字段另见下文 | 两者可能重叠，不能相加当作整帧或输入延迟 |
| Present 调用／等待耗时 | 指定线程进入到返回或相关等待区间，ms | 等待可能涉及队列与节流，不自动证明 GPU 太慢；返回也不是像素已显示 |
| 显示的新画面间隔 | 同一显示位置、相邻确认显示的内容帧之间的间隔；记录重复、丢弃及观测方法 | 刷新率、提交速率与内容更新速率不是同一个数 |
| 输入到显示延迟 | 从约定的输入起点到指定屏幕位置达到约定响应阈值，ms | OS 事件到逻辑响应只是其中一段，不覆盖输入设备和屏幕响应 |

60fps 对应的目标节拍约为 16.67ms，但不保证一次输入到像素变化也只用 16.67ms。流水线可以同时推进不同帧，吞吐达标与端到端响应达标需要分别验证。

例如，假设测到的是两组**显示间隔**：`16 / 16ms` 和 `8 / 24ms`。两组各有两个有效间隔、总时长都是 32ms，按“区间数 ÷ 总秒数”计算均为 62.5 次／秒，但后者间隔不均匀。这是算例，不是某工具实测出的 60fps，也不能用两个样本得出稳定的 P95／P99 结论。不要逐帧计算 `1000 / 间隔` 后再取算术平均来冒充同一窗口的更新速率。

关联时保留输入事件、应用源帧、GPU 工作与显示内容的身份及时间域。拿不到显示时刻，就报告已测的应用／GPU 区间，不给它换上“玩家已看到”的名字。

---

## 显示刷新原理

### 显示器逐行扫描

先用逐行扫描的简化模型理解：显示系统把待显示图像送到屏幕，目标像素所在位置影响何时出现变化，之后还涉及像素响应。固定刷新显示的 60Hz 表示约每 16.67ms 一个刷新周期，并不说明每次都有新游戏内容。

这个模型不覆盖所有面板、合成与可变刷新实现。数字接口中的 blanking／同步时机也不能直接理解成所有现代面板都在做 CRT 式的物理回扫。完整输入延迟需要约定观察位置与亮度／颜色阈值，不套固定的“半帧到一帧扫描耗时”。

### 撕裂（Screen Tearing）的产生

当一次扫描期间采用的图像发生变化，不同区域可能来自不同内容帧，从而出现撕裂。它与实际呈现时机和模式有关，不能只比较 fps 与 Hz 是否相等。

也不能把所有现代呈现路径都画成“GPU 直接写前缓冲”。例如 Windows 的 [DXGI flip model](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model)可将后缓冲与 DWM 共享用于合成；这说明路径存在差异，不证明某次 Unity 会话采用了哪一种。先核对图形 API、交换链、窗口模式和系统显示路径。

---

## VSync（垂直同步）

### 工作原理

同步呈现会按显示时机安排图像更新。请求可以先入队，所需 GPU 工作再完成；不要把 Present 调用、GPU 完成、图像被显示系统采用和目标像素变化画成同一时刻。

以 Vulkan 的 [VkPresentModeKHR](https://docs.vulkan.org/refpages/latest/refpages/source/VkPresentModeKHR.html)为具体例子：FIFO 按队列先后在垂直同步时机采用请求；Mailbox 的待呈现请求可被较新的请求替换；Immediate 不等待垂直同步，可能撕裂。这些是各模式的契约，不是 Unity 跨平台始终选用某模式的保证。

错过显示时机会延后新内容出现，排队和节流也可能拉长响应。在固定刷新、特定双缓冲与同步条件下可以出现档位式下降，但不能把所有系统都写成“一超 16.67ms 就从 60 掉到 30”。同样，不能由 VSync 开关推导出总输入延迟最多只增加一帧。

### Unity 中的 VSync 设置

按 [Unity 2022.3 帧率控制文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-targetFrameRate.html)区分平台：桌面／Web 的 `vSyncCount > 0` 时，`targetFrameRate` 被忽略；`vSyncCount = 0` 时才由相应的目标帧率机制参与节奏控制，不能直接解释为“完全不限速”。在固定 60Hz、按两个刷新周期安排一帧的模型中，目标节拍是 30fps，仍需满足工作量与实际系统条件。

Android／iOS 忽略 `vSyncCount`，使用 `Application.targetFrameRate` 选择游戏目标帧率；XR 则按对应运行时控制。应用设置、驱动策略与实际显示是三项记录，不拿一个 Unity 字段证明最终呈现模式。

---

## 自适应同步（G-Sync / FreeSync）

### 核心思路的反转

可变刷新率（VRR）允许显示设备在支持范围内调整刷新时机，帮助匹配内容节奏。它改变的是显示时机的约束，不能消除输入采样、应用计算、排队、扫描和像素响应，也不保证 GPU 一完成就立即变成可见像素。

### G-Sync vs FreeSync

比较具体配置，而不是用品牌表格预填固定频率范围、价格或延迟：

| 条件 | 要核对的内容 |
| --- | --- |
| 显示与连接 | 具体型号、接口、分辨率、可用刷新范围与相关认证 |
| GPU 与驱动 | 支持组合、是否启用 VRR、对当前显示器与窗口模式是否生效 |
| 范围外行为 | 超过上限或低于下限怎样处理，是否具备低帧率补偿 |
| 实际效果 | 显示节奏、撕裂、延迟与正确性分别取证 |

[NVIDIA G-SYNC 设置说明](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nvdsp/To_use_variable_refresh_rates.htm)区分显示器和窗口模式设置；[AMD FreeSync FAQ](https://www.amd.com/en/products/graphics/technologies/freesync.html)说明 LFC 可通过重复显示帧处理低于最小刷新率的情况。重复显示不等于应用生成了更多新帧，也不能据此把所有 FreeSync 设备的范围外行为写成同一种结果。

### Unity 的支持

Unity 的帧率设置不能单独保证 VRR 已工作。固定 Unity、图形 API、窗口模式、驱动与显示器后，核对对应平台的呈现支持和驱动状态，再测实际显示节奏。把“设备具备能力”“已配置启用”和“本次运行观察到效果”分别报告。

---

## Frame Pacing（帧时序）

### 问题描述

Frame Pacing 关注内容帧怎样按节奏出现。前文算例中的相同平均速率可以对应不同间隔分布；分析时需要沿同一显示窗口记录新帧、重复帧和较长的空档，不能只看应用循环的平均 fps。

间隔波动、长时间没有新画面与输入反馈拖后是相关但不同的观察。是否达到产品体验目标，应结合操作、场景和显示条件验收，不只拿一个标准差判流畅。

### 导致 Frame Pacing 问题的原因

- 应用工作量或等待变化，例如逻辑、GC、加载结果收口与系统调度。
- GPU 工作、资源依赖或提交节奏变化。
- 呈现队列、合成与显示时机不匹配。

这些是候选原因。先将同一批帧的应用、GPU 与显示证据关联，再判断哪一段阻止新画面出现；不能仅凭一个 CPU 尖峰解释全部显示抖动。

### Android Frame Pacing Library（Swappy）

Google 的 [Frame Pacing 文档](https://developer.android.com/games/sdk/frame-pacing)用 SurfaceFlinger、呈现时间戳和同步等待解释短帧、长帧及队列积压。Swappy 在支持路径中协调呈现节奏、避免不必要的队列填塞；它不会代替游戏完成超预算的逻辑或 GPU 工作，也不保证所有场景都无抖动。

在 Unity 2022.3 的 Android 项目中核对 **Optimized Frame Pacing** 配置及实际图形 API、设备和运行状态，再做对照；“引擎包含相关集成”不等于当前包已启用，更不等于已经获得收益。不用旧的集成年份替代本次配置与效果证据。

### Unity 的 Frame Timing Stats

先固定 Unity 2022.3 的 Player、平台与图形 API，按[该版本手册](https://docs.unity3d.com/2022.3/Documentation/Manual/frame-timing-manager.html)核对 Frame Timing Stats 和支持矩阵，再解释字段：

| 字段／计数 | 2022.3 文档中的口径 | 阅读边界 |
| --- | --- | --- |
| `cpuFrameTime` | 连续两帧结束之间的 CPU 帧间时间，ms，包含等待与开销 | 不等于纯 CPU 计算量 |
| `cpuMainThreadFrameTime` | 帧开始到主线程完成该帧工作的区间，ms | 不能与其内部或重叠区间重复相加 |
| `cpuMainThreadPresentWaitTime` | 主线程上与 Present 相关的等待，ms | 不自动归因到 GPU，也不应从总量中减掉后宣称“真实帧时间” |
| `gpuFrameTime` | GPU 执行该帧的计时，ms | 不包含完整输入和显示链；支持与准确性受后端限制 |

API 字段定义另见 [FrameTiming](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTiming.html)。这张表不是分项相加公式。手册还指出 Apple TBDR 路径可能报告大于总帧间时间的 GPU 时间，Linux OpenGL 不支持 GPU 时间；不能把异常、缺失或不可比数据统一填成零。

下面只展示**最近可获得的完成帧数值**，未知值使用 `NaN`，不在每次 Update 打日志，也不负责累计分位数：

```csharp
using UnityEngine;

public sealed class FrameTimingLatestSample : MonoBehaviour
{
    private readonly FrameTiming[] timings = new FrameTiming[1];

    public double LatestCpuMs { get; private set; } = double.NaN;
    public double LatestGpuMs { get; private set; } = double.NaN;

    private void Update()
    {
        LatestCpuMs = LatestGpuMs = double.NaN;
        if (!FrameTimingManager.IsFeatureEnabled())
            return;

        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, timings);
        if (count == 0)
            return;

        FrameTiming sample = timings[0];
        if (IsUsable(sample.cpuFrameTime))
            LatestCpuMs = sample.cpuFrameTime;
        if (IsUsable(sample.gpuFrameTime))
            LatestGpuMs = sample.gpuFrameTime;
    }

    private static bool IsUsable(double value)
    {
        return value > 0 && !double.IsNaN(value)
                         && !double.IsInfinity(value);
    }
}
```

示例保守地把零和非有限值视为不可用，不把它们解释成零成本；正数也不证明测量必然准确。[IsFeatureEnabled](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.IsFeatureEnabled.html)只用于查询统计功能是否开启，不保证每项 GPU 数据都存在。

[GetLatestTimings](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/FrameTimingManager.GetLatestTimings.html)返回实际取得的数量，可能少于请求数量；数组里的最近样本属于已完成帧，不是调用所在的当前 Update。2022.3 手册描述了四帧的返回延迟，同时说明延迟不能保证结果准确，不能把它误写成测得的输入延迟。

若要统计，需要按可识别的源帧／时间戳去重、记录缺失与平台条件，不能每次 Update 取同一条旧记录就再算一个样本。无法确认源帧身份时，保留为最近值观察，不生成 P95／P99。将 CPU、GPU 数据与目标任务及显示事件对齐后，它们才能支持进一步诊断；两个数本身不能证明实际显示间隔或点击到像素延迟。

---

## Triple Buffering vs Double Buffering

### Double Buffering

前后缓冲的简化模型有助于理解图像生产与消费的交接。缓冲正在被显示系统使用时，生产者可能等待可复用图像；这些等待也可能承担同步与节流，不能一概认作无效工作，更不能因此无条件移除等待。

### Triple Buffering

增加图像可以缓解生产者等待可用目标，但也增加资源占用，并可能允许更多工作排队。关键是同时记录**图像数量、允许在途的工作量、队列策略与显示采用顺序**。

三张图像不自动等于 Mailbox，也不保证“显示最新完成的一帧”。FIFO 与 Mailbox 的请求处理不同，合成路径还可能另有队列。吞吐、内存与输入延迟应一起比较，不能给所有三缓冲写一个固定的“额外一帧”代价，也不能保证增加缓冲就消除帧率档位变化。

---

## 输入延迟链路

### 完整链路

先定义起点：物理按键动作、OS 输入事件时间，还是 Unity 处理该事件的时刻？再定义终点：逻辑状态改变、提交图像，还是指定屏幕位置达到约定的亮度／颜色阈值？只有起止点相同的结果才能直接比较。

```
物理输入 → 设备／OS 事件 → 应用采样并生成响应帧 A
                               ↓ 提交与 GPU 工作
                            GPU 完成 A ─┐
呈现请求、同步与显示时机条件 ─────────────┴→ 合成／采用 A → 扫描与像素变化
```

这是依赖关系示意，不是实测时间线，也不规定 Present 必须在 GPU 完成之后才调用。输入事件要关联到真正包含其响应的内容帧；不能把同一采样时刻附近的 CPU、GPU 和显示记录直接相加。

完整点击到像素变化的测量需要覆盖设备与显示端，例如经校验的硬件测量或高速摄影方案，并交代触发检测、屏幕位置、响应阈值和时间分辨率。软件事件或录屏只能证明各自覆盖的区间，不能自动替代物理显示证据。[NVIDIA 的系统延迟说明](https://www.nvidia.com/en-us/geforce/guides/system-latency-optimization-guide/)也区分外设、PC 与显示延迟；其中具体产品与调优结果不能当作所有 Unity 游戏的数值。

### Unity Input System 的处理时机

按 [Input System 1.6.3 的 UpdateMode 文档](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.6/api/UnityEngine.InputSystem.InputSettings.UpdateMode.html)：

- `ProcessEventsInDynamicUpdate` 是默认模式，在 `Update` 前处理输入。
- `ProcessEventsInFixedUpdate` 在 `FixedUpdate` 前处理输入，应与读取状态的循环和模拟设计配套。
- 手动模式需要自行调用 `InputSystem.Update`；部分设备的 BeforeRender 更新另有机制，不由该枚举统一控制。

这些是事件处理时机，不是通用的“Low-Latency Input”开关。改变模式会影响输入何时进入逻辑，但最终反馈还取决于模拟、渲染、排队与显示；先验证操作语义和响应帧，再比较延迟。不把 XR 的姿态更新机制及某设备结果外推成普通输入的固定毫秒收益。

---

## 移动端特殊性

### iOS ProMotion

设备能力、主 App 配置、Unity 请求与实际显示分别核对。`Application.targetFrameRate = 120` 表达目标，不能单独证明该型号、系统和当前功耗／温度条件下持续显示 120 个新画面。

ProMotion 配置条件与 `CADisableMinimumFrameDurationOnPhone` 的边界复用 [iOS 专项]({{< relref "performance/mobile-unity-ios.md" >}})，不在这里重写平台适配。提高目标后，重新验收 CPU／GPU 预算、持续节奏、功耗和温度，不能预填“耗电或渲染代价恰好翻倍”。

### Android 可变刷新率

枚举支持的模式、请求刷新率与当前实际模式是三种证据。Android 的 [Frame rate API 文档](https://developer.android.com/media/optimize/performance/frame-rate)明确说明，系统还会考虑其他条件，不保证满足 `setFrameRate` 请求；不能用“请求成功”证明显示已切换。

Unity 项目先核对引擎的帧率与 Optimized Frame Pacing 路径，不把原生 API 示例直接当作可叠加的 Unity 配方。设备与构建条件复用 [Android 专项]({{< relref "performance/mobile-unity-android.md" >}})，本篇保留实际刷新与内容节奏的测量责任。

### Application.targetFrameRate

沿用前文 Unity 2022.3 契约：Android／iOS 忽略 `vSyncCount`；`targetFrameRate = -1` 在移动端默认目标为 **30fps**，不能写成不限速。设为 60 或 30 也只是选择目标，不保证实际达到，更不自动构成某类游戏的最优功耗策略。

固定刷新模型下还要考虑刷新周期的整除关系，例如 60Hz 下每两个周期更新一次对应 30fps。遇到高刷或动态刷新设备，记录运行时条件与实际内容更新，不能把固定刷新算例当作全部设备的硬性规律。

---

## 实践建议

### 按问题选证据

| 要回答的问题 | 先找什么证据 | 保留什么限制 |
| --- | --- | --- |
| 应用是否被逻辑、GC、加载或等待拖后 | Unity Profiler 的线程时间线、标记及源帧，与帧预算拆账对应 | CPU 墙钟区间不全是计算；Editor／Deep Profile 的开销不能冒充发布包表现 |
| GPU 工作、队列或合成是否阻止新内容出现 | 对应平台／图形 API 支持的 GPU 与系统呈现轨迹，关联源帧和目标 Surface／图层 | 先核对字段覆盖与时钟，不把呈现事件直接当成物理像素响应 |
| 玩家实际看到的节奏与输入响应是否改善 | 显示内容身份、同一观察位置的更新，以及定义清楚的输入到显示测量 | 软件计时缺失的设备／屏幕段仍需补证据 |

例如 [Perfetto FrameTimeline](https://perfetto.dev/docs/data-sources/frametimeline)要求 Android 12+；本次核对的文档还列出 SurfaceViews 不支持的限制。不能保证任意 Unity 渲染 Surface 都会出现完整应用轨迹，更不能直接把 Actual Timeline 的区间当作点击到像素时间。先确认目标版本和本次 trace 中的数据，再决定它能回答哪一段问题。

### 先定统计窗口，再比较方案

每次报告附一张指标记录：**指标对象与起止点、单位、设备／构建／图形 API、同步与刷新设置、采样窗口、有效样本数、缺失数、采集方法和判定阈值**。启动、首次加载、稳定操作与长时间运行分别统计；对照时固定操作及视觉正确性，记录温度、电源状态与采集开销。

均值、P95／P99 与标准差描述的是所选样本集合，不直接代表完整体验。分位数要说明算法与样本数，例如最近秩法使用排序后的第 `ceil(p × N)` 项；少量样本的尾部分位不稳定。长帧比例还要写清阈值、分母和窗口；连续卡顿可另按事件统计。不存在通用的“标准差小于 2ms 就流畅”或“超过预算 50% 才算丢帧”规则，显示截止时机与产品验收需要另行定义。

不要把缺失值记为零，也不要把重复返回的完成帧反复计数。CPU、GPU、应用间隔和显示间隔分别统计，跨时钟先校准关联；没有关联证据时，不画一条看似精确的端到端时间线。

最小验证可以从以下检查开始，**本篇尚未执行这些设备实验**：

1. 关闭或不支持 Frame Timing Stats、暂时拿不到完成帧时，示例保持未知值；GPU 字段不可用不影响 CPU 可用值的独立报告。
2. 连续读取时核对返回数量、源帧与延迟，确认统计器去重，没有将调用帧当作结果所属帧。
3. 在同一任务中分别观察稳定运行、首次加载与持续运行，关联 CPU／GPU／显示证据，保存长间隔与失败样本。
4. 每次只改一项同步、目标帧率或缓冲策略，同时比较节奏、输入响应、资源／功耗与正确性；缺少显示或物理输入证据就保留未测区间。

定位到逻辑、提交、GPU 或加载责任后，回到[帧预算拆账]({{< relref "performance/game-budget-06-frame-budget-breakdown.md" >}})选择处理方法。这里不把对象池、异步加载或增加缓冲当成通用答案。

<!-- DATA-TODO: 补目标设备、Unity/包/系统/图形 API 版本、同步与刷新配置、采样开销、原始时间戳与源帧/内容帧身份、窗口/有效及缺失样本数、统计算法、操作与视觉正确性、显示及输入测量证据；标明未覆盖区间，不填虚构收益。 -->

---

## 小结

先说清测量边界，再讨论同步与缓冲。CPU／GPU 工作、内容出现的节奏与输入反馈各有证据；VSync、VRR、Frame Pacing 和队列配置改变约束与取舍，都不能代替实际验收。将同一响应帧从应用关联到显示，才能判断卡点在哪里，以及一次调整是否同时满足节奏、响应和持续运行的目标。
