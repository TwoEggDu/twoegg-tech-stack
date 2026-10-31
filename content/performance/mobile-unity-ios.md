---
title: "Unity on Mobile｜iOS 专项：Metal 渲染行为、内存警告机制与 Instruments 联用"
slug: "mobile-unity-ios"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "以 Unity 2022.3 为基准，解释 Metal、内存与诊断边界，并区分 App、UIKit Scene、进程和游戏会话，明确保存恢复责任、版本条件与验证证据。"
tags: ["Unity", "iOS", "Metal", "Instruments", "Apple", "移动端"]
series: "移动端硬件与优化"
weight: 2210
---

> 版本说明：Unity 行为以 2022.3 LTS 为基准，涉及 URP 时以 14.x 为讨论范围。Xcode 工具、商店要求及平台 API 应按实际版本核对。本文没有提供对应项目的设备性能或内存上限实测，示例不能作为已经完成的真机验证。

## 1. Metal 的特点与 Unity 的适配

### Unity iOS 的图形后端

在本文的 Unity 版本范围内，iOS 项目使用 Metal。Apple 将 OpenGL ES 标记为弃用，并不等于可以推导出所有历史系统从某日起停止修复；本篇关注当前项目实际使用的后端，而不是据弃用声明推测驱动维护情况。[Unity 2022.3 iOS Player settings](https://docs.unity3d.com/2022.3/Documentation/Manual/class-PlayerSettingsiOS.html)列出了对应的构建设置。

### Metal 与 OpenGL ES 的核心概念差异

**CommandBuffer 的显式管理**

Metal 通过 `MTLCommandQueue` 创建 Command Buffer，由 Encoder 编码命令。结束编码后，可在 **commit 之前**安排 `presentDrawable`，再提交执行。安排呈现、提交、GPU 完成和画面真正显示是不同的时间点；`commit` 返回不等于 GPU 已经画完。[Apple 的 Command Buffer 提交说明](https://developer.apple.com/documentation/metal/mtlcommandbuffer/commit())定义了提交后的约束。

Unity 在后端封装了这一流程。读 GPU 时间轴时，应先区分 CPU 编码、队列等待、GPU 执行与呈现，不能把它们合成一个“渲染耗时”。

**Render Pass Descriptor：Load Action 和 Store Action**

Attachment 的 Load / Store Action 表达的是内容生命周期，不是一张固定的 DRAM 耗时表：

| Action | 语义 | 使用前要确认什么 |
|---|---|---|
| Load | 保留进入 Pass 前的内容 | 后续计算是否真的依赖旧值 |
| Clear | 用指定值初始化 | 清理范围与后续读写是否匹配 |
| Load DontCare | 初始内容不要求保留 | 读取或混合前是否已正确写入所需像素 |
| Store | 保留 Pass 的结果 | 后续消费者需要哪些结果 |
| Store DontCare | 不要求保留结果 | Pass 之后不再依赖这份内容 |
| MultisampleResolve | 将多重采样结果解析到目标纹理 | 后续需要解析结果，还是还需要 MSAA 原始数据 |

合理设置可减少不必要的数据搬运，但真实成本依 GPU、资源模式、Pass 合并与实现而定。尤其不能把 DontCare 当作 Clear，也不能把“不保证内容”误认为“驱动错误丢弃数据”。语义见 [Metal Load / Store Actions](https://developer.apple.com/documentation/metal/setting-load-and-store-actions)。

URP 的实际行为要结合相机堆叠、渲染目标、后续读者和 Native Render Pass 等条件确认。Camera 的 `Nothing` 不等于一律 DontCare；中间 RT 如果下一步要读，就仍需保留有效结果。`ConfigureTarget` 只指定目标，也不能单独证明已经设置 Store Action。先在 Frame Debugger / Metal Capture 中确认资源依赖，再修改自定义 Pass。

### Memoryless Render Targets：按 Render Pass 生命周期选择

Apple GPU 支持利用 tile memory 保存临时 Attachment。适合的例子是仅在同一 Render Pass 使用的深度，或解析后不再需要原始样本的 MSAA Attachment；解析目标需要能保存后续使用的结果。

关键边界是 **Render Pass 内的临时数据**，不是“这一帧内都可以随便读取”。若后续 Pass 需要采样深度、回读或跨帧复用，就不能直接把对应持久资源改为 memoryless。它减少特定 Attachment 的系统内存存储需求，也不等于整帧不占 DRAM。[Apple 存储模式说明](https://developer.apple.com/documentation/metal/choosing-a-resource-storage-mode-for-apple-gpus)区分了 shared、private 与 memoryless。

Unity 提供 `RenderTextureDescriptor.memoryless` 等接口，但能否使用、URP 是否自动采用，需要检查版本、平台支持与实际生成的 Pass，不能用一个未经核实的 Player Settings 菜单保证结果。

---

## 2. iOS Build Settings 关键配置

### Target iOS Version

最低系统版本是安装门槛，所用 SDK / Xcode 的提交要求是另一条约束。选择时应核对 Unity、原生 SDK 和业务 API 的支持范围，再根据实际设备覆盖决定；不能用没有采样日期的系统占比作为依据。

系统版本也不能直接代替设备性能分级：同一 iOS 版本可能运行在多个芯片与内存档位。是否支持某台设备，仍要看产品预算与真机表现。

### Architecture：区分真机与模拟器

Unity iOS 真机构建使用 ARM64；模拟器属于不同构建目标，原生插件需要匹配相应平台切片，不能因为都叫 arm64 就认为真机库与模拟器库可以互换。第三方 SDK 的目标平台和架构应随发布产物一起检查。

### Bitcode：区分弃用与移除

Apple 在 Xcode 14 中弃用 Bitcode 并默认不再启用，在 Xcode 15 中移除了支持。不要继续把它作为现代 iOS 发布的优化选项；旧构建脚本或 SDK 遗留配置需要按当前工具链清理。依据见 [Xcode 14 发布说明](https://developer.apple.com/documentation/xcode-release-notes/xcode-14-release-notes)与 [Xcode 15 发布说明](https://developer.apple.com/documentation/xcode-release-notes/xcode-15-release-notes)。

### ProMotion（高刷新率）启用

支持 ProMotion 的设备也存在刷新范围差异，不能把不同代际统一写为 1–120 Hz。在支持的 iPhone 上，`CADisableMinimumFrameDurationOnPhone` 是允许更高刷新率的相关配置；应用仍需设置目标帧率并验证实际运行状态。

下面是修改**导出的 Xcode 工程** Info.plist 的 Unity Editor 示例，放在 Editor 编译范围内。不要假定把一个 Info.plist 放进 `Assets/Plugins/iOS` 就会自动完成合并。本示例经文档核对，未在本次审校中执行 iOS 构建。

```csharp
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class iOSPostBuild
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        string plistPath = System.IO.Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);
        plist.WriteToFile(plistPath);
    }
}
```

最终检查生成的 plist，避免其他构建后处理覆盖设置。高刷新率不是性能承诺，具体目标与动态变化见第 6 节。[Apple ProMotion 指南](https://developer.apple.com/documentation/quartzcore/optimizing-iphone-and-ipad-apps-to-support-promotion-displays)说明了应用请求与系统选择的关系。

### App Store 包体要求

App 总大小、可执行文件限制、设备裁剪后的下载量与蜂窝下载提示是不同口径，且政策会变化。发布时以 [App Store Connect 的最大构建大小说明](https://developer.apple.com/help/app-store-connect/reference/app-uploads/maximum-build-file-sizes/)核对，并分别记录初始安装与后续资源下载的大小。

资源拆分要分别设计首包、按需下载与缓存回收。使用 ODR 或 CDN / Addressables 前，应确认所用工具链的构建和运行时加载支持；不能把 AssetBundle 的标签直接等同于已经完成 ODR 交付。

---

## 3. iOS 内存警告机制

### 有内存限制，但不是按机型固定不变的安全额度

设备 RAM、系统当前空闲内存、进程 footprint 和进程可用额度是不同指标。iOS 会在内存压力或进程达到相关限制时终止进程，前台游戏也可能受到影响；不能把处理流程写成必然先警告、再杀后台、最后杀前台的固定顺序。

[`os_proc_available_memory`](https://developer.apple.com/documentation/os/os_proc_available_memory)可以查询当前应用在调用时的可用额度参考值。它对应当前限制减去应用 footprint，不是整机空闲 RAM，也不是预留给游戏的保证；限制和用量会变化，不应缓存这个值作为长期预算。

### 怎样建立项目内存预算

按机型制定预算，需要设备、构建、场景与原始记录相互对应。本文尚未提供这组实测；可用额度也不应被当成要尽量填满的目标。

| 需要记录的项 | 用途 |
|---|---|
| 机型、系统版本、构建配置、后台条件 | 说明结果适用范围 |
| 稳态 footprint 与资源分类 | 找到常驻成本 |
| 切场景、解压、上传时的峰值 | 找到阶段重叠 |
| 可用额度参考、内存警告与 jetsam 记录 | 区分观测信号和真实终止原因 |
| 降载后的占用与重新加载成本 | 验证应对策略是否有效 |

<!-- DATA-TODO: 补充项目最低支持设备上的稳态、切场景峰值、系统压力条件与原始 Instruments / jetsam 记录；不要用设备 RAM 比例推算安全上限。 -->

### lowMemory 的响应策略

`Application.lowMemory` 是可用的响应入口，但不能把收到警告视为每次终止前的必经步骤。预算与阶段峰值治理应提前进行。

收到信号后，优先停止非必要加载，释放已经明确不再使用、可以重建的缓存，随后由资源管理器安排卸载。强制 `GC.Collect()` 只处理可回收的托管对象，不会自动释放仍被引用的纹理，也不能称为托管堆压缩；`WaitForPendingFinalizers` 还可能阻塞。

`Resources.UnloadUnusedAssets()` 返回异步操作不等于完全没有主线程成本。调整 mip 限制也受纹理类型、Streaming 和版本规则约束，不能保证所有纹理立即释放内存。应验证释放量、后续重载和长帧，避免在压力回调中启动更昂贵的一轮工作。

---

## 4. Xcode Instruments 与 Unity 联用

Unity Profiler 同时包含引擎 Native 标记、脚本、渲染等视图，并非只能看到 Managed；Instruments 提供系统与原生调用的另一种观测。两边需要用相同场景和时间标记对齐，而不是把它们当作互不重叠的内存或耗时账本。

### 前置：明确构建与采集条件

用 Development Build 连接 Unity Profiler 定位问题，再用接近发布配置的构建确认收益。开发检查、Deep Profiling 和连接工具本身都可能改变性能。

Xcode 的 **Product → Profile** 可以启动 Instruments；也可在 Instruments 中选择设备与目标进程。Xcode 的 **Debug → Attach to Process** 是调试器附加，不等于已经开始 Instruments 采集。记录符号、优化级别、设备状态与采集模板。

### Time Profiler：CPU 调用栈采样

Time Profiler 帮助定位热点调用栈，采样间隔和可见性受工具配置、系统及符号影响，不能保证每个短函数都被捕获，也不能把样本占比直接当成逐次调用计时。

若 Unity 时间轴里某段耗时解释不清，可查看同一时间段原生插件或 SDK 的栈。Invert Call Tree 有助于聚合叶子热点；过滤系统库时也要回看被隐藏的等待、I/O 与驱动调用，避免漏掉实际瓶颈。

### Allocations 与 VM Tracker：区分对象、分配与驻留

Allocations 可追踪许多 malloc / ObjC 等分配，适合排查原生插件的生命周期。托管运行时申请的大块内存与其中每个 C# 对象不是同一级视图，不能期待它替代 Unity 的对象分析。

VM Tracker 按虚拟内存区域分类；IOSurface 等分类也不能简单等同于全部 Metal 纹理。GPU 资源应结合 Metal Capture 与 Unity 资源统计确认。在统一内存设备上，CPU / GPU 视图可能指向同一份资源，不宜直接相加。

### Energy Diagnostics：持续负载与功耗线索

使用所装 Xcode 提供的能耗或功率分析工具，比较固定场景与运行时长下的 CPU、GPU、网络活动及热状态。工具名称和指标依版本而异；活动等级不等于精确的组件瓦数测量。

看启动几秒的 FPS 无法证明持续体验。至少覆盖进入高负载场景、稳定运行、切后台与恢复，并保留环境条件，才能判断降载是否改善长期帧时。

### Metal System Trace：GPU / CPU 同步关系

时间轴可帮助区分提交、GPU 执行和等待；GPU Frame Capture 则用于分析单帧命令、资源和 Attachment。工具可见的阶段与计数器受设备支持影响。

看到 `waitUntilCompleted` 只能证明当前线程在等待完成，原因可能是同步读取、资源复用、引擎调度或 GPU 积压。先找谁发起等待、等哪一批工作，再决定减少 GPU 工作、调整数据依赖还是改变缓冲。增加缓冲还会增加延迟与内存，不能把“减少 Draw Call”或“多加缓冲”当作通用答案。

---

## 离开游戏再回来：App、Scene、进程与游戏会话

玩家重新打开游戏，可能是在继续原进程，也可能已经进入一次新的启动。画面看似还在原位置，还可能只是系统显示的界面快照，不能直接证明渲染和交互已经恢复。要安排保存、重连与资源恢复，先分清这次变化发生在哪一层。

本节建立恢复模型与责任分工，复用前文的内存、Metal 和诊断内容；验证矩阵是待执行设计，没有目标项目的 iPhone／iPad 恢复实测。

### App 级协调、界面 Scene 与进程寿命分开看

| 对象 | 本节中的含义 | 恢复时要确认什么 |
| --- | --- | --- |
| App 级协调 | 应用启动、共享服务及平台事件的管理 | 本次是否重新初始化应用级系统，SDK 与共享服务是否只建立一次 |
| UIKit Scene | 应用的一份 UI 实例及其窗口相关状态 | 哪个界面连接、失活、进入后台或断开；对应数据与显示目标是否仍有效 |
| 进程 | OS 承载本次执行的地址空间、线程与内存状态 | 是同一运行现场继续，还是新进程从记录重建；旧任务和句柄不能跨进程沿用 |
| 游戏会话 | 项目定义的账号、存档进度、玩法状态与联网关系 | 哪些业务可继续，哪些需读档、重新确认或结束 |

**这里的 UIKit Scene 不是 Unity 的场景。** UIKit 的界面连接变化，不自动等于 Unity Scene 被卸载；某个界面断开，也不能独自证明整个进程已结束或所有游戏会话都应退出。

Apple 将进程级启动与界面生命周期分工：在 **iOS 13 及以后启用 Scene 支持**时，界面事件由相应 `UISceneDelegate` 接收，`UIApplicationDelegate` 仍负责应用启动等工作。多个 Scene 可以各有状态；是否支持多窗口还需要相应配置和实现。见 [Apple 生命周期说明](https://developer.apple.com/documentation/uikit/managing-your-app-s-life-cycle)及 [Scene 配置](https://developer.apple.com/documentation/uikit/specifying-the-scenes-your-app-supports)。这说明 API 与事件分工，不证明任意 Unity 2022.3 导出工程已经接入 Scene，也不替代目标 SDK 的适配要求。

沿这个模型理解几种状态：前台 active 表示可与用户交互；inactive 可能只是系统面板等临时中断，不能直接认定已进入后台。进入 background 后，普通前台游戏需要收敛工作并为挂起做准备，允许继续的任务取决于实际后台能力。suspended 表示应用暂不执行，不能把它当作进程已死；进程终止后则需要重新启动。这是解释模型，不是一组在每个 Unity 回调中都能直接读到的枚举。

Apple 的[后台 UI 准备说明](https://developer.apple.com/documentation/uikit/preparing-your-ui-to-run-in-the-background)同时描述了临时失活、后台转换和系统快照。App 切换器中的旧画面或恢复时短暂显示的快照都不能作为 Ready 证据，仍需实际的新画面与交互结果。

### 把平台事件接到 Unity，先确认真实入口

本篇仍以 **Unity 2022.3 LTS** 为基线。该版本的[导出工程说明](https://docs.unity3d.com/2022.3/Documentation/Manual/StructureOfXcodeProject.html)列出 `main.mm` 与 `UnityAppController` 等入口。实际项目可能还有原生宿主、自定义 AppDelegate 或 SDK 集成，应检查最终主 App 的 Info.plist、`UIApplicationSceneManifest`、Scene delegate 配置、入口类和事件传递链。不能只加一个 Scene 配置键，就宣称原生窗口、Unity 渲染与回调已经配套。

Unity 的 [OnApplicationPause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html)与 [OnApplicationFocus](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html)提供引擎层通知，不能直接替代每个 UIKit Scene 的完整状态。初始化也可能收到 `OnApplicationPause(false)`；不要仅凭一次 false 就启动读档、重连和重建全部资源。回调与原生事件的实际关系需随 Unity 补丁、宿主和插件配置验证，Editor 中的焦点变化也不能充当 iOS 挂起实验。

若集成确实支持多个 Scene，应分别记录界面身份与状态，协调共享游戏数据；一个界面失活不能直接翻译成“所有会话退出”。目标只支持单窗口时，就按单窗口范围验收，不预填多窗口能力。

平台适配层负责把已有事件交给统一的恢复协调入口，保留重复通知与真实状态变化的区别。恢复入口再判断本轮上下文是否仍有效，避免每个 SDK、页面与系统各自发起一轮恢复。

### UI 恢复、业务保存与后台工作分别负责

Apple 的 [UI 状态保存说明](https://developer.apple.com/documentation/uikit/preserving-your-app-s-ui-across-launches)明确区分界面状态与持久数据：UIKit 可以丢弃 UI 恢复记录，所以它不能替代业务数据落盘。Scene 路线可用 `NSUserActivity` 保存重建界面的必要信息；具体接入见[官方恢复示例](https://developer.apple.com/documentation/uikit/restoring-your-app-s-state)。这些机制不会自动保存 Unity 对象图、纹理内容或服务端会话，项目仍需定义恢复入口与数据契约。

可以按下面的责任划分检查设计；同一人可以承担多项职责：

| 责任位置 | 保存／离开时 | 恢复／重新启动时 |
| --- | --- | --- |
| 玩法与存档 | 平时增量形成有效检查点，区分临时 UI、必须保留进度与可重建内容 | 校验存档版本、完整性与账号归属；失败时回退到明确的有效状态 |
| 原生与 SDK 集成 | 正确传递应用／Scene 事件，按已启用能力收敛后台工作 | 核对新启动、界面重连与共享服务初始化，避免重复注册和重复恢复 |
| 网络与业务会话 | 保留待确认操作身份，不把本地暂停当作服务端也暂停 | 重新确认连接、登录与请求结果，避免重复结算或把旧会话结果应用到新账号 |
| 资源、渲染与异步调用方 | 标明保留、释放与重建范围，停止不再合法的目标使用 | 按依赖确认资源与显示目标，再接受本轮任务结果；不默认重载全部资源 |
| 内容负责人及 QA | 约定进度、画面、输入、音频和离线计时的预期 | 验收实际功能与表现，记录失败及未覆盖项，不只确认“没有闪退” |

关键状态应在形成时持续保存，记录“请求保存”“本地提交完成”和“服务端确认”各自的结果。恢复时选择最后一份有效检查点；若保存失败或记录损坏，应进入可解释的回退路径。提交方式、存储错误及突然终止下的一致性要按实际实现验证，不把启动异步写入当成已经持久保存。

也不要把 `applicationWillTerminate` 或 Unity 的 `OnApplicationQuit` 当作必达的最后保存机会。Apple 的[终止通知说明](https://developer.apple.com/documentation/uikit/uiapplicationdelegate/applicationwillterminate(_:))与 [Unity 2022.3 退出通知](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html)都需要结合实际挂起／终止路径理解；原进程未执行的收尾代码，不会因下一次启动而自动补做。

短时额外执行时间、系统调度的后台任务与后台传输也不是同一能力。已有第 6 节说明 `beginBackgroundTask` 的配对、失败与过期处理，这里只补一个保存约束：按 [Apple 后台执行说明](https://developer.apple.com/documentation/uikit/extending-your-app-s-background-execution-time)，应在需要的长任务开始前安排相应申请，不能等已经退后台才假定获得额外时间。不要把文档中的回调返回要求或 `backgroundTimeRemaining` 解释成每次保存稳定拥有的完成窗口。

### 恢复本轮上下文，再判断画面与交互是否可用

同一进程恢复时，原来的异步工作可能仍关联旧页面、旧账号或旧玩法状态。任务返回前后都应核对归属，按所有者约定取消、保留、重新请求或丢弃结果。对象与业务上下文的收口复用[《Unity 异步运行时 06》]({{< relref "engine-toolchain/unity-async-runtime-06-cancellation-destroy-playmode-lifecycle.md" >}})，不在这里重讲 CancellationToken；也不因短暂失活就一律取消所有任务。

进程已经结束时，原内存任务没有可继续的执行现场；应从有效检查点及界面恢复信息重新建立工作，涉及联网时再核对当前服务端状态。界面恢复成功也不证明网络会话、资源与画面已准备好。项目可以采用下面的职责链；步骤可并行，但必须满足实际依赖：

```text
确认进程启动／界面身份与本轮恢复上下文
  → 取得有效检查点，核对当前业务会话
  → 确认所需资源、渲染目标及本轮任务结果
  → 验证新画面、输入与相关业务结果，标记 Ready
```

退后台与恢复时，原生渲染插件还需遵守 [Metal 的后台使用约束](https://developer.apple.com/documentation/metal/preparing-your-metal-app-to-run-in-the-background)，配合引擎管理其拥有的资源和目标。收到 active 或 Unity 恢复通知，不等于下一帧已经正确显示；界面重连也不自动意味着全部 Metal 资源失效。沿前文的提交、执行、呈现区分取证，再判断恢复黑屏属于目标、资源、业务还是其他问题。

内存压力下如何解释 footprint、驻留与引擎资源，复用[内存记账文章]({{< relref "performance/game-performance-memory-accounting.md" >}})中的 Apple 口径。同一进程的多个 Scene 不构成几份可直接相加的进程内存。本篇仍按 2022.3 核对 Unity 工具字段，不把那篇的 Unity 6 示例直接套用。警告、释放与真实终止分别看证据，不能假定每次终止前必有足够的卸载时间。

### 用同一轮事件与结果证明恢复了什么

每轮固定设备、iOS／iPadOS、Unity 补丁、Xcode／SDK、构建与资源版本、存档、主 App 配置和触发动作。记录外部测试轮次、进程启动身份与 PID、采用 Scene 时的会话／连接实例、业务会话、恢复批次和任务关联。关联字段由项目设计；PID 可能复用，只有一条“resume”日志不足以区分继续运行与新启动。

将原生事件、Unity 通知、检查点提交、资源就绪和首个正确显示／交互结果放回可对齐的时间线；跨进程的旧／新记录要显式关联，不能只比较各自计时器数值。没有日志也不能证明系统已挂起，因为采集缺失、缓冲或事件覆盖范围都需要另查。

以下是**待执行的验证矩阵**：

| 场景 | 需要保留的证据 | 验收重点 |
| --- | --- | --- |
| 正常启动与短暂系统面板／中断 | 初始通知、实际前后台事件、进程／界面与恢复批次 | 初始化不误认恢复；临时失活不错误结束会话或重复结算 |
| 切后台／锁屏后回前台 | 原生与 Unity 事件、保存结果、业务状态和新画面／输入 | 区分原进程继续与新启动；快照不作为恢复成功证据 |
| 实际支持的 Scene 断开／重连或多窗口变化 | Scene 配置与身份、连接事件、共享服务和显示结果 | 按单个界面及共享数据分别验收；不支持的分支标“未覆盖” |
| 进程终止后重新启动 | 终止方式、旧／新启动身份、有效检查点及可获得的退出报告 | 从有效记录重建，旧任务结果不回写；不能默认退出回调执行过 |
| 保存失败／中断、UI 恢复记录不可用 | 失败输入、保存提交阶段、读档结果和回退动作 | 业务数据与 UI 记录分别处理，不把无效记录当作成功恢复 |
| 后台任务失败／过期、联网操作结果待确认 | 申请与结束记录、过期处理、操作身份及服务端结果 | 完成清理和状态回流，不重复执行已完成业务，也不预填后台工作通过 |

工具模拟的通知、Xcode 暂停／停止运行、模拟器测试与真实设备上的挂起、系统终止要分别标记。保留是否连接调试器及采集方式，并在相应运行条件下复查；不能用一次模拟内存警告证明已经覆盖 jetsam，也不能把停止调试当作真实系统回收。

发现重新启动时，复用第 5 节的报告分类：普通崩溃、jetsam 与 watchdog 各用对应记录关联构建、时间和复现场景。Apple 的 [watchdog](https://developer.apple.com/documentation/xcode/addressing-watchdog-terminations)与 [jetsam 报告](https://developer.apple.com/documentation/xcode/identifying-high-memory-use-with-jetsam-event-reports)承担不同证据职责；拿不到报告时保留“原因未确认”，不要仅凭回到桌面就写成内存终止。

<!-- DATA-TODO: 在固定 Unity 2022.3 补丁、Xcode／SDK 与目标 iPhone／iPad 上执行本节矩阵，归档主 App 配置、入口及 Scene 采用情况、原生／Unity 事件、有效检查点、新画面与交互结果及相关退出报告。区分工具注入和真实系统事件，保留失败样本、未覆盖分支和调试连接条件，不预填通过或恢复耗时。 -->

---

## 5. iOS 崩溃分析

### dSYM 文件的生成与管理

dSYM 是否生成取决于 Xcode 的调试信息设置，不是勾选 Unity Development Build 就能保证。发布时归档 `.xcarchive`、对应 App / Framework 二进制和 dSYM；按 UUID 匹配崩溃报告中的镜像。

IL2CPP 代码的链接位置随 Unity 工程结构而异，可能涉及 App 或 UnityFramework。不要固定寻找未经核实的 `libil2cpp.dylib` 和方法映射目录。先检查实际构建产物有哪些镜像，再保存它们的符号。[Apple 符号化说明](https://developer.apple.com/documentation/xcode/adding-identifiable-symbol-names-to-a-crash-report)解释了匹配要求。

### 符号化：先核对镜像，再看栈

优先用 Xcode 提供的崩溃报告工具，并确认所需符号齐全；Organizer 中能看到报告，不代表所有第三方 Framework 都已符号化。macOS 上可用 `dwarfdump --uuid` 对比二进制、dSYM 与报告中的 UUID。

手动符号化还要匹配架构、加载地址和工具支持的报告格式。不能用另一次“相同版本号”的重编译替代原发布符号，也不能把一个成功解析的函数名视为根因结论。

### 区分崩溃、jetsam 与 watchdog

| 记录 / 现象 | 可以初步说明什么 | 下一步证据 |
|---|---|---|
| SIGABRT | 进程被 abort，例如断言或未处理异常 | 异常信息、终止原因与线程栈 |
| EXC_BAD_ACCESS | 非法内存访问 | 故障地址、对象生命周期、Native 互操作 |
| jetsam event report | 系统因内存相关条件终止进程 | 终止原因、footprint、场景峰值 |
| `0x8badf00d` | watchdog 终止 | 启动 / 生命周期超时、主线程阻塞或未结束的后台任务 |

**`0x8badf00d` 不能作为内存终止的标志。** [Apple watchdog 文档](https://developer.apple.com/documentation/xcode/addressing-watchdog-terminations)与 [jetsam 文档](https://developer.apple.com/documentation/xcode/identifying-high-memory-use-with-jetsam-event-reports)描述的是不同问题；jetsam 报告通常没有普通 crash 的线程 backtrace。

同样，向 Objective-C 的 nil 对象发送普通消息，并不能直接解释 EXC_BAD_ACCESS。需要检查悬空指针、越界、调用约定与具体 API 的参数约束，避免仅凭异常类型猜测原因。

---

## 6. iOS 特有的性能陷阱

### 陷阱一：把所有首次卡顿都归因于 Shader 编译

Pipeline 创建可能涉及编译和驱动工作，但 Metal 也提供异步创建路径；不能声称所有 PSO 一定在第一次 Draw 时同步编译。首次进入场景还可能包含资源读取、解压、上传与脚本初始化，应以调用栈和时间轴区分。

Unity 的 Shader 预热可以把部分工作前移，但预热数据需要覆盖真实使用条件。Unity 2022.3 明确提醒：在 Metal、Vulkan、DX12 上，顶点布局或 Render Target 设置与预热时不同，驱动仍可能继续工作。[ShaderVariantCollection.WarmUp 文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ShaderVariantCollection.WarmUp.html)说明了这个边界。

Metal Binary Archive 在 Apple 的 [WWDC20 介绍](https://developer.apple.com/videos/play/wwdc2020/10615/)中已出现，不能写成 iOS 16 才引入。它支持复用兼容的 GPU 编译结果，但受命中范围和兼容条件限制；平台存在这个能力，不证明某个 Unity / URP 版本有指定菜单，也不能承诺消除所有编译卡顿。

验证预热时要覆盖冷启动、重新安装、不同设备与实际材质 / Pass 组合，同时记录加载耗时和内存代价。

### 陷阱二：把 ProMotion 请求当作固定刷新率承诺

Unity 移动端使用 `Application.targetFrameRate`，不使用桌面式的 `vSyncCount` 控制。结合第 2 节配置后，在支持设备上请求目标值，再测量真实帧时；低电量模式、热状态和负载都可能影响实际结果。[Unity targetFrameRate 文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-targetFrameRate.html)列出了移动端规则。

原生 `CADisplayLink.preferredFrameRateRange` 表达偏好，不是锁定硬件频率。若维护自己的 DisplayLink，可按 Apple API 使用；若要修改 Unity 持有的 DisplayLink，必须先确认目标版本的公开集成方式。原生 API 的存在不能代替 Unity 集成与设备验证。

### 陷阱三：AOT、裁剪与运行时代码生成混为一谈

本文范围内的 Unity iOS Player 使用 IL2CPP AOT。依赖运行时生成并执行新代码的库，需要确认它们提供可用的 AOT 路径。Unity 的 [Scripting restrictions](https://docs.unity3d.com/2022.3/Documentation/Manual/ScriptingRestrictions.html)说明了 `System.Reflection.Emit`、泛型等限制。

反射本身不等于 JIT；类型或成员被裁剪、泛型实例没有生成，以及 API 不支持，是不同失败原因。不能写成“调用 Emit 必然直接崩溃”，也不能用不存在的 `Expression.Interpret()` 作为修复方案。表达式树、序列化或热更新框架，应按实际版本验证支持的解释 / 代码生成路径。

最小验证需要在目标设备的发布裁剪配置下，执行实际泛型、反射和序列化用例。静态搜索 Emit 引用只能帮助找风险，不足以证明整条路径可用。

### 陷阱四：把后台执行时间当成固定预算

进入后台后，普通应用可能被挂起。Apple 对 `applicationDidEnterBackground` 回调有及时返回的要求，但不能把文档中的回调时限理解成所有 Unity 保存逻辑都稳定拥有的执行窗口。

`beginBackgroundTask` 申请的是有限的额外执行时间，不会自动启动任务，也不等同于 `BGTaskScheduler`。必须处理申请失败、expiration handler，并与 `endBackgroundTask` 配对；不能假定一定有 30 秒。依据见 [Apple 后台执行说明](https://developer.apple.com/documentation/uikit/extending-your-app-s-background-execution-time)与 [Background Task Notes](https://developer.apple.com/forums/thread/85066/)。

Unity 层可在 `OnApplicationPause` 等生命周期回调中协调保存与恢复，但关键状态应平时增量保存，避免退出瞬间才做大文件序列化。勾选 Background fetch 不会让普通 UnityWebRequest 持续执行；需要后台传输时，应采用平台支持的传输机制并验证完成回调。

恢复前台时，重新核对网络、时间、资源有效性与渲染结果，不要把一切异常都解释为 Metal “Lost Context”。

---

## 总结

iOS 的关键是明确生命周期：App 与界面事件由谁处理、游戏会话怎样保存恢复、Attachment 何时还要被读取、资源何时可以释放，以及崩溃符号对应哪一次构建。Metal、Instruments 与 Unity Profiler 提供不同层面的证据，组合使用才能把平台机制落实为产品预算与可验证的优化。
