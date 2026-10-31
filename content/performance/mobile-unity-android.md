---
title: "Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化"
slug: "mobile-unity-android"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "区分 Android 图形 API、构建分发、热管理和内存终止，并沿进程、Activity、Surface 与游戏会话说明保存恢复责任及验证证据。"
tags: ["Unity", "Android", "Vulkan", "OpenGL ES", "Adaptive Performance", "移动端"]
series: "移动端硬件与优化"
weight: 2200
---

> 版本说明：Unity 设置与脚本行为以 2022.3 LTS 文档为基准；Adaptive Performance 单独注明包版本。商店政策与插件支持范围以发布时的官方文档为准。本文没有提供本项目的真机 A/B 测量记录，下面的验证方法不代表已经获得性能收益。

## 1. Vulkan vs OpenGL ES：如何做选择

### Vulkan 的潜在收益来自哪里

Vulkan 将较多状态、资源分配与同步责任交给应用或引擎，提供降低驱动 CPU 开销和并行录制命令的机会。但机会能否转化为收益，要看场景是否受 CPU 提交限制、引擎实现与目标驱动，不能按“高端机”直接判定更快。

**显式内存管理**允许引擎选择符合资源要求的内存类型。`vkAllocateMemory` 使用的是 `memoryTypeIndex`，不是直接传入 `HOST_VISIBLE` 等标志；还要满足资源的 memory type 要求。CPU 可映射内存主要涉及 CPU 访问与上传策略，不能据此声称 Unity 自动避免了 GPU → CPU 回读。[Vulkan 内存分配规范](https://registry.khronos.org/vulkan/specs/latest/html/vkspec.html#memory-device)说明了这些约束。

**多线程 Command Buffer 录制**也有前提：不能无同步地并发使用同一 Command Pool；常见做法是为录制线程安排独立池。图形 API 支持并行，不代表任意 Unity Job 都在录制 GPU 命令。[Vulkan threading 指南](https://docs.vulkan.org/guide/latest/threading.html)可用于区分 API 能力与引擎调度。

### 驱动兼容性：记录组合，不贴芯片标签

某个 GPU 家族并不只对应一个驱动版本。黑屏、花屏或帧时退化，应至少记录设备、Android 构建、驱动、Unity / SRP 版本、API 与复现场景，再查对应问题记录。

设备排除列表应关联具体 issue 或复现材料，系统版本只能作为测试条件之一。功能支持、画面正确和持续性能是三项独立检查。

### Unity 的 Graphics API 配置策略

在 Android Player Settings 关闭 **Auto Graphics API** 后，可以安排 Vulkan 与 OpenGLES3 的顺序。Unity 按支持情况选择 API；不支持 Vulkan 时可回退到 OpenGL ES，但**这不是检测到花屏、崩溃或性能差后自动切换的保险机制**。具体设置见 [Unity 2022.3 Android Player settings](https://docs.unity3d.com/2022.3/Documentation/Manual/class-PlayerSettingsAndroid.html)。

`SystemInfo.graphicsDeviceType` 用于观察当前 API，普通 C# 场景代码不能在图形设备初始化后直接切换后端。若需要按设备选择，应使用目标 Unity 版本支持的启动阶段方案，并单独验证；仅保存一个 PlayerPrefs 键不会改变下一次启动使用的 API。

| 检查项 | Vulkan / GLES 对照应保持什么一致 |
|---|---|
| 画面正确性 | 场景、相机、质量档、Shader 变体与资源 |
| 性能 | 分辨率、帧率目标、录制流程、构建优化配置 |
| 持续负载 | 起始温度、运行时长、电源与系统模式 |
| 发布决策 | 崩溃、异常设备覆盖及可执行的回退方案 |

<!-- DATA-TODO: 补充至少一组真机 Vulkan/GLES 对照的设备与驱动版本、帧时间分布、热状态和画面结果；记录齐全前不写固定百分比收益。 -->

---

## 2. Android Build Settings 逐项解析

### Target API Level

`minSdkVersion` 决定安装所需的最低系统版本，`targetSdkVersion` 声明应用针对的系统行为版本。提高 target 后需要验证行为变更和 SDK 兼容性，并不等于同步提高最低安装门槛。

Google Play 的目标 API 政策随时间调整，提交前查[官方目标 API 要求](https://developer.android.com/google/play/requirements/target-sdk)。最低系统版本则依据 Unity 支持范围、依赖 SDK 与实际用户设备决定；不能用未经统计的覆盖率直接推荐某个 API Level。

### Scripting Backend 与 Target Architectures

面向 Google Play 的 Unity 2022.3 项目，需要按其[64-bit 要求](https://developer.android.com/google/play/requirements/64-bit)提供对应架构支持；该版本 Unity 的 ARM64 构建需要 IL2CPP。这不等于所有渠道的所有 Android 开发构建都只能用 IL2CPP。

是否保留 ARMv7，应依据支持设备和渠道要求。32-bit 设备不能简单等同于 Android 4–5；增加一个 ABI 会增加原生库产物，但用户下载量还受 AAB 按设备拆分影响，没有统一的“包体增加 30%”。

IL2CPP 的 C# → C++ → Native 编译链需要匹配的工具链。Managed Stripping 与反射、序列化的关系也要在发布配置下验证：Editor 能运行不代表类型或方法在裁剪后仍存在。

### Internet Access

`Auto` 依赖 Unity 对网络使用的判断，`Require` 明确要求网络权限。若接入 Native / Java SDK，还要检查合并后的 Manifest；不能仅凭 Unity 设置推断最终权限。对实际需要联网的产品，应把权限检查纳入构建产物验证。

### Minify（R8 混淆）

R8 处理 Java / Kotlin 字节码的收缩、优化与混淆，与 C# Managed Stripping 是两条链。启用时核对 SDK 的 keep rules，测试反射、JNI 和组件入口，并归档 mapping 文件。Native 符号不能替代 R8 mapping。

Manifest 可用 `bundletool dump manifest --bundle=myapp.aab --module=base` 检查；类是否保留则需要查看 DEX / APK Analyzer 并验证运行路径。`dump resources` 不能用来代替 Manifest 检查。

### Split Application Binary（OBB / PAD）

拆包结果依 APK / AAB 构建方式及 Unity 版本而异，不能把 OBB 与 PAD 当成同一种文件。先确定目标渠道的分发方式，再验证首包与资源包的归属；不要沿用旧的 100 / 150 MB 限制判断今天的发布要求。

---

## 3. AAB 与包体优化

### AAB vs APK：分发产物不同

AAB 是发布格式，不是直接安装到设备的 APK。Google Play 可按 ABI、密度、语言等生成面向设备的 APK 集合，包括 base 与必要的 split APK。减少多少下载量，取决于原包内容；不能承诺统一比例。

Unity 2022.3 在 Android **Build Settings** 中提供 **Build App Bundle (Google Play)**。本地模拟分发后还要在目标渠道测试，特别是原生库、资源包和升级安装。

### Play Asset Delivery 的三种模式

| 模式 | 交付时机 | 应用需要处理的边界 |
|---|---|---|
| Install-time | 随应用安装交付 | 初始安装内容与体积 |
| Fast-follow | 安装后自动开始下载 | 首次启动时可能尚未就绪 |
| On-demand | 应用发起请求后下载 | 请求、进度、失败重试和资源可用状态 |

On-demand 不要求玩家一定点击按钮；Fast-follow 也不保证加载界面结束前完成。PAD 负责交付 Asset Pack，Addressables 负责资源寻址和加载，两者需要匹配的构建与加载集成，不能把安装一个包写成自动打通。[PAD 官方说明](https://developer.android.com/guide/playcore/asset-delivery)列出了交付模式和资源可用性约束。

### 纹理压缩格式分包

ASTC 是否可用应按设备 / 图形 API 的能力确认，不能用芯片系列名称或未经统计的覆盖率代替。ETC2 是 OpenGL ES 3.0 的标准格式之一；ETC1 不直接保存 Alpha。格式选择还要比较具体纹理的质量、块大小与运行时占用，不能笼统认为某一种格式画质总更好。

Texture Compression Format Targeting 允许按设备能力交付不同纹理版本，但 Unity 内置构建支持与 PAD 插件配置依版本变化。接入前按[Android 官方纹理格式分发文档](https://developer.android.com/guide/playcore/asset-delivery/texture-compression)确认构建链，随后验证设备实际收到并使用的格式；不是在 Graphics 设置中添加几个名称就完成了分包。

### 包体分析工具

下面的命令是 `bundletool` 接口示例，需准备对应 AAB 和工具；它们不是本文已执行的测试记录：

```bash
bundletool build-apks --bundle=myapp.aab --output=myapp.apks
bundletool dump manifest --bundle=myapp.aab --module=base
bundletool get-size total --apks=myapp.apks
```

[官方 bundletool 文档](https://developer.android.com/tools/bundletool)还支持设备规格与设备安装测试。大小估算应说明对应设备，不要把整个 APKS 压缩包大小当成用户下载量。

Unity 侧可保存 `BuildPipeline.BuildPlayer` 返回的 BuildReport，结合最终 AAB、APK 集合与 PAD 内容分析占用；不依赖某个临时目录在所有 Unity 版本都存在。

---

## 4. Adaptive Performance 接入

### 背景：热状态改变了可持续性能

移动设备的散热条件有限，持续负载可能触发系统限制频率或功耗。Adaptive Performance 提供状态与调节机制，但它不保证提前预测每次降频，也不能绕过系统热保护。

### 接入路径与 Provider 的能力边界

Google 的 [Unity ADPF 接入说明](https://developer.android.com/games/engines/unity/unity-adpf)将 Android Provider 支持对应到 Adaptive Performance 5.0 起的版本。接入时固定 Unity、主包和 Provider 版本，在设备上确认 Provider 激活及所需数据是否有效。

例如 `com.unity.adaptiveperformance.google.android` 1.0 的[包文档](https://docs.unity.cn/Packages/com.unity.adaptiveperformance.google.android@1.0/manual/getting-started.html)明确其为数据 Provider。不能由“安装 Adaptive Performance”推导出它支持手动设定 CPU / GPU 性能等级，更不能把请求等级等同于设置硬件频率。Provider 名称和功能应以项目锁定版本的官方包文档为准。

### 热状态如何进入质量策略

质量调整应由项目统一管理，下面是策略示意，不是现成插件代码：

```text
采集：Provider 可用性 + 热状态 + CPU/GPU 帧时 + 当前质量档
判断：持续超预算或热风险升高 → 降低一档工作量
恢复：状态稳定一段时间且仍有性能余量 → 尝试恢复一档
约束：变化冷却时间、上下限、用户选择、设备档位
验证：帧时分布、画面质量、持续运行与功耗变化
```

“NoWarning” 只能说明当前告警状态，不能直接恢复最高画质。分辨率应从固定基准计算，避免每次事件在已缩小的值上继续相乘；恢复策略也要同步恢复曾修改的帧率、阴影等参数。

CPU / GPU 受限的场景适合不同调整项。每次只改变可解释的一组工作量，才能确认改善是否来自预期机制。

---

## 切后台再回来：进程、Activity、Surface 与游戏会话

玩家看到的是“离开游戏，再打开游戏”。工程上却可能是同一个进程继续执行、界面组件被重建，或者整个进程已经结束、从头启动。把这些情况都交给一个“恢复回调”，容易出现状态丢失、旧请求覆盖新页面，或逻辑继续了却没有正确画面的情况。

这一节先建立平台模型，再接到 Unity 的保存、资源与业务恢复；下面的流程和验证矩阵是设计建议，没有目标设备上的恢复实测。

### 四种寿命，不能用一个“还活着”概括

| 对象 | 管什么 | 失效后需要确认什么 |
| --- | --- | --- |
| 应用进程 | 本次运行的地址空间、线程与内存状态 | 是否进入了新的进程启动；原内存、句柄与在途任务不能当成仍然有效 |
| Activity | 承载界面与 Android 生命周期交接 | 是否是新的组件实例；界面状态与平台入口是否重新建立 |
| Surface | 接收渲染输出的目标，以及它的尺寸与有效期 | 旧目标是否停止使用，新目标是否就绪并已接回渲染路径 |
| 游戏会话 | 项目定义的账号、存档、场景、交互及联网状态 | 哪些业务状态可继续，哪些需读档、重新确认或结束 |

Android 的 `onCreate`、`onStart`、`onResume`、`onPause`、`onStop`、`onDestroy` 描述的是 Activity 的变化。配置变化可能导致旧 Activity 销毁并建立新实例，这不等于进程一定重新启动；反过来，系统终止进程时也不保证调用 `onDestroy`。可见、获得焦点和处于 Resumed 状态需要分别观察，不能把 `onPause` 简化为“已经完全不可见”。依据见 [Activity 生命周期](https://developer.android.com/guide/components/activities/activity-lifecycle)与[进程生命周期](https://developer.android.com/guide/components/activities/process-lifecycle)。

也不能把“旋转一次”固定当成重建测试。是否重建受配置变化类型、合并后的 Manifest 中 `android:configChanges`、系统与窗口模式影响；接管某类配置变化也不等于免除了适配责任。先看本次组件实例和实际事件，见 [Android 配置变化说明](https://developer.android.com/guide/topics/resources/runtime-changes)。

Surface 还有自己的一组 created／changed／destroyed 事件。AOSP 将 SurfaceView 所在 Activity 与 Surface 描述为两组相互关联的状态机；不能只凭 Activity 恢复就认定绘制目标已经可用。[SurfaceHolder.Callback](https://developer.android.com/reference/android/view/SurfaceHolder.Callback)要求直接使用 Surface 的渲染线程在 `surfaceDestroyed` 返回前停止访问该目标。这里限定的是 SurfaceView／SurfaceHolder 模型，不能把它当作 Unity 所有图形后端的固定内部调用顺序，参见 [AOSP 图形说明](https://source.android.com/docs/core/graphics/arch-sv-glsv)。

在 Unity 项目中，应由引擎及相应原生集成管理它们拥有的绘制目标。自有插件也要遵守目标寿命，不能长期缓存旧 Surface 继续提交；但 Surface 变化本身不证明所有纹理、场景对象或 GPU 设备都已失效。恢复黑屏需要继续核对目标、资源、提交与显示证据，不能直接归因于驱动或统一“重载整个游戏”。

游戏会话则是业务约定。进程仍在不证明连接或登录仍有效；重新建立进程也不证明玩家进度应归零。联网状态按服务端结果确认，本地状态按有效存档与产品规则恢复，这些都不是 Android 替游戏决定的事。

### Unity 回调提供入口，恢复责任仍要分开

本篇沿用 **Unity 2022.3 LTS** 基线。[默认 Unity Activity](https://docs.unity3d.com/2022.3/Documentation/Manual/AndroidUnityPlayerActivity.html)承担平台交互；项目有自定义 Activity、SDK 插件或其他版本的入口实现时，先核对合并 Manifest、入口类与事件传递，不照搬一套 Java／C# 回调对应表。

Unity 2022.3 的 [OnApplicationPause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html)与 [OnApplicationFocus](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html)是引擎层通知。该版本文档明确：Android 软键盘可以触发失焦；键盘打开时再按 Home，通知路径又有区别。初始化也可能收到 `OnApplicationPause(false)`，所以不能把每次 false 都当成一次完成的后台恢复，更不能把失焦直接当成断线或销毁会话的指令。

建议把回调送入同一恢复协调入口，记录当前状态、合并重复请求，再按职责推进：

| 责任位置 | 保存／离开时 | 恢复／重新启动时 |
| --- | --- | --- |
| 平台与 SDK 集成 | 正确传递生命周期、焦点和目标变化，保留必要事件 | 核对实际入口与组件实例，避免多个 SDK 各自启动一次恢复 |
| 玩法与存档 | 定义必须保留的进度，平时形成可确认的检查点 | 校验存档格式与完整性；无有效记录时按明确规则回退，不能装作读档成功 |
| 网络与会话 | 记录待确认请求及业务身份，不把本地暂停当作服务端也暂停 | 重新确认连接、会话与请求结果，避免重复扣费、重复领取或复用过期结果 |
| 资源、渲染与异步调用方 | 区分可保留、可重建与已失效内容，撤销不再合法的上下文 | 检查本轮对象与目标有效性，按依赖恢复；旧任务结果不能写入新会话 |
| 内容负责人及 QA | 约定必须保留的玩法进度、画面和交互预期 | 核对进度、画面、输入、音频与联网结果，不只确认“没闪退” |

这张表描述职责，可以由同一人承担，不要求重建一套团队岗位。

### 保存不是退出瞬间的一次大写盘

先分三类状态：仅在内存里保留的运行对象、用于重建界面的少量标识、必须持久保存的业务数据。Android 的 saved state 可用于系统重建后的界面恢复，但适合少量简单数据；它不是大型存档，也不会自动保存 Unity 对象图、GPU 资源或游戏会话。其恢复条件也不能推广到用户主动结束界面、强制停止或清除数据后的所有启动。见 [Android 保存 UI 状态](https://developer.android.com/topic/libraries/architecture/saving-states)。

不要把全部存档、网络上传和资源释放放到 `onPause` 等最后时机。Android 文档提醒该回调时间短，不适合承担重保存；系统结束进程时也未必给应用补做收尾的机会。Unity 的 [OnApplicationQuit](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html)同样说明，移动平台上不能依赖退出通知完成关键保存。

退后台也不承诺一个普通线程或异步任务能持续运行。[进程生命周期文档](https://developer.android.com/guide/components/activities/process-lifecycle)特别说明，Android 13 起缓存进程可能得到很少甚至没有执行时间；这限定的是相应进程状态，不能推广为所有后台组件的统一行为。需要后台传输时另按平台允许的工作方式设计，不把“任务已经启动”当作最终保存或上传完成。

工程上可以在进度形成时增量保存，把“请求保存”“本地提交完成”“服务端确认”分开记账，恢复时读取最后一份有效检查点。若采用临时文件替换、数据库事务或其他提交办法，需按实际存储接口与故障条件验证；函数返回、写进缓冲或启动了异步任务，都不能独自证明突然终止后仍能恢复。这里给出设计约束，不承诺某种写盘方案已经具备崩溃一致性。

### 恢复的是本轮上下文，不是让所有旧任务继续

同一进程内，后台期间发起的请求可能在回来时才交付结果。若账号、场景或 UI 已经变了，即使对象没有销毁，结果也可能不该再应用。项目可给恢复批次与业务上下文独立编号，结果落地前核对归属；哪些任务暂停、取消、保留或重新请求，由其真实所有者决定，不因短暂失焦就一律取消。

对象、页面、场景和任务的收口机制复用[《Unity 异步运行时 06》]({{< relref "engine-toolchain/unity-async-runtime-06-cancellation-destroy-playmode-lifecycle.md" >}})，这里不重复讲 CancellationToken。进程已结束后，旧内存任务不存在可继续的现场；应从持久记录与当前业务状态重新建立工作，不能靠原来的 token 或静态变量跨进程恢复。

可以用下面的职责链检查恢复设计；各步允许重叠，但要满足相应依赖：

```text
确认本次进程／组件与恢复批次
  → 取得有效检查点，确认当前业务会话
  → 核对所需资源及绘制目标，撤销过期结果
  → 验证首个正确画面与交互，标记本轮 Ready
```

`OnApplicationPause(false)`、Surface 创建、提交一帧与玩家已能正确操作，是不同终点。Ready 按产品约定选择，比如“进度正确、画面正确且输入可用”；分阶段记录方法复用 [IO 06 的任务证据关联]({{< relref "engine-toolchain/storage-io-06-profiling-with-fio-iostat-perfetto-instruments.md" >}})，不把一个回调耗时称为完整恢复耗时。

### 用事件与身份证明恢复了哪一层

每轮固定构建／资源／存档版本、Android 版本与构建、targetSdk、Unity／图形 API、入口配置和触发动作。记录外部测试轮次、进程启动标识与 PID、Activity 实例、Surface 目标代次、业务会话和恢复批次；这些是建议的关联字段，不是 Android 自动提供的一套统一 ID。PID 可能复用，单独一个 PID 或一条“resume”日志不够。

把原生与 Unity 通知、检查点提交结果、异步任务归属、资源就绪和首个正确显示／交互结果放回可对齐的时间线。仅修改 Surface 尺寸不应误记成创建了新目标，目标代次要来自实际创建／替换记录。拿不到某层事件时标记缺口，不从黑屏或返回首页倒推出发生了哪种重建。

以下是**待执行的验证矩阵**，不是已有通过记录：

| 场景 | 需要保留的证据 | 验收重点 |
| --- | --- | --- |
| 正常启动与短暂 Home／回前台 | 初始通知、进程／组件身份、保存与恢复批次、Ready | 初始化不会被误认成恢复；不重复读档、重连或注册事件 |
| 软键盘、失焦与支持的多窗口操作 | 焦点、暂停、组件可见性及实际操作；记录系统版本／窗口模式 | 按产品规则处理输入与暂停，不因焦点变化错误退出会话 |
| 受支持的配置变化／受控 Activity 重建 | Manifest 配置、实际实例变化及是否保留同一进程 | 不把旋转当作必然重建；新入口取得正确状态且无重复回调副作用 |
| 实际发生 Surface 重建的恢复／窗口变化 | 目标创建／销毁／尺寸事件、渲染与显示结果 | 停用旧目标并接回新目标；未发生重建就记“该分支未覆盖” |
| 后台进程终止后重新启动 | 终止方式、旧／新进程启动身份、最后有效检查点、退出原因证据 | 从记录重建状态，过期结果不回写；未确认的联网操作重新核对 |
| 保存中断、失败或记录损坏 | 保存提交阶段、失败输入、加载结果与回退路径 | 能回到明确的有效状态或给出失败处理，不把坏数据当成功恢复 |

人为触发和真实系统事件必须分开。[开发选项](https://developer.android.com/studio/debug/dev-options)中的 **Don't keep activities** 用于触发离开后的 Activity 销毁，不能当成进程已死；[ADB 的 force-stop](https://developer.android.com/tools/adb)是强制停止，也不能作为一次真实内存压力回收的证明。测试时写明注入方式、系统和构建条件，完成后恢复测试设置，不能用一条受控路径代替全部恢复行为。

Android 11／API 30 起，可在应用侧使用 [ActivityManager.getHistoricalProcessExitReasons](https://developer.android.com/reference/android/app/ActivityManager#getHistoricalProcessExitReasons(java.lang.String,int,int))读取近期退出原因，关联退出时间、进程与当前复现；这是版本条件，不是仅调整 targetSdk 就能在旧系统使用。记录有保留范围，读取其他 UID 的记录有额外权限要求；没有记录不能反推没有退出。部分设备也不支持明确报告低内存终止，应核对 [ApplicationExitInfo](https://developer.android.com/reference/android/app/ApplicationExitInfo)及 `isLowMemoryKillReportSupported()`，不能把所有 SIGKILL、ANR、崩溃或用户停止都叫作 LMK。

<!-- DATA-TODO: 固定 Android／Unity 2022.3 具体补丁、入口与图形 API，在目标设备执行本节矩阵；归档 Manifest、组件／目标／进程身份、保存提交结果、退出证据和首个正确显示／交互记录。明确人为注入、真实系统事件、未覆盖分支及失败样本，不预填通过或恢复耗时。 -->

---

## 5. Android 特有的性能陷阱

### 陷阱一：第三方 SDK 的 ContentProvider 导致启动慢

SDK 可通过 ContentProvider 在早期自动初始化，使启动链变长。日志中出现 Provider 名称只能提示入口，不能单凭日志条数得到初始化耗时。

先固定冷 / 热启动条件，采集启动时间与系统 trace，再关联到合并 Manifest 中的 Provider。Activity 启动完成也不等于游戏已经可交互，业务首屏需要额外标记。

[App Startup](https://developer.android.com/topic/libraries/app-startup)可以集中声明初始化依赖并支持手动初始化，但不会自动把所有工作移到后台线程。只有在 SDK 明确支持手动初始化、依赖顺序经过验证时，才移除它的自动入口；否则可能破坏登录、广告或统计功能。

### 陷阱二：把 LMK 理解成“超过固定值就 OOM”

Android 的 `lmkd` 根据系统内存压力和进程重要性等信息回收进程；前台应用受较高保护，但也可能被终止。分配失败、运行时抛出 OutOfMemoryError 与系统杀进程需要分别排查。[Android LMK 说明](https://developer.android.com/google/play/vitals/lmk)明确包括用户可感知的前台终止。

同一份游戏包在不同后台负载下，能承受的峰值可能不同。重点检查：

- **切场景双驻留**：旧场景未释放，新场景资源已加载。
- **解压与上传重叠**：Bundle 缓冲、纹理上传、Shader 预热、RT 分配同时发生。
- **回前台恢复**：恢复资源时系统也可能处于压力区间。
- **常驻线过高**：对象池、可读纹理 / Mesh 与 RT 链挤占峰值余量。

`Application.lowMemory` 可以作为响应入口，但回调覆盖受 Unity / Android 版本影响，不能假定每次终止前都能收到。不要在回调里默认执行强制 GC、等待终结器、全局卸载和修改 mip 等一整套昂贵操作；它们不一定释放当前持有的资源，也可能增加卡顿或重载成本。

更稳妥的是提前设置资源预算，在压力信号到来时暂停非必要预加载、释放明确可重建的缓存，并由资源生命周期系统安排后续卸载。用 `adb shell dumpsys meminfo com.your.package` 观察进程内存，与场景标记、系统日志和退出原因对齐。

预算拆分可继续看 [CPU 性能优化 05：内存预算管理]({{< relref "performance/cpu-opt-05-memory-budget.md" >}})。

### 陷阱三：把 Attachment 生命周期错误归咎于驱动

如果 StoreOp 设为 `DONT_CARE`，就不能依赖该 Attachment 的内容在 Pass 结束后被保留。后续还要读它却没有安排正确的 Store / Resolve，本身就可能违反资源使用约定，不足以证明驱动 Bug。

先检查资源依赖、布局转换和同步，再用最小场景、验证层及目标驱动复现。Workaround 应绑定确认过的问题范围和回归用例，不宜按整个 GPU 家族无条件启用。依据见 [Vulkan Render Pass 规范](https://registry.khronos.org/vulkan/specs/latest/html/vkspec.html#renderpass)。

### 陷阱四：把桌面 vSyncCount 方案搬到手机

Unity 2022.3 中，Android 和 iOS **忽略 `QualitySettings.vSyncCount`，通过 `Application.targetFrameRate` 选择目标帧率**：

```csharp
// 申请目标帧率，不保证设备在当前负载下能够达到
Application.targetFrameRate = 60;
```

实际帧率还受显示刷新率、整除关系、调度与负载影响。Android 可结合 **Optimized Frame Pacing** 检查呈现节奏，但平均 FPS 正常也不代表帧间隔稳定。用真机时间轴检查长帧与显示间隔；不要写一个固定毫秒抖动值作为所有设备的行为。[Unity targetFrameRate 文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-targetFrameRate.html)区分了桌面与移动端规则。

---

## 6. Android 崩溃分析（IL2CPP 符号化）

### Native Crash：获取可用的崩溃记录

Native Crash 的 backtrace 可来自 logcat、bugreport 或平台崩溃服务。`/data/tombstones/` 是受保护的系统目录；普通设备打开 USB 调试或使用应用的 `run-as`，不代表有权读取它。

调试时可先保存 crash 日志：

```bash
adb logcat -b crash -d > crash.txt
```

日志可能轮转或不完整，应尽早采集并保留原始记录。没有 Native crash 栈的回桌面，还要检查 LMK、ANR 和其他退出原因。

### 用 ndk-stack 符号化

每次构建归档对应的符号，核对 **Build ID、ABI 与二进制版本**。不要固定依赖旧版 `Temp/StagingArea` 路径；输出位置和符号生成选项应按当前 Unity 构建配置确定。

下面假设已将该构建的未剥离库整理到 `symbols/arm64-v8a`，并把 NDK 工具加入 PATH：

```bash
ndk-stack -sym ./symbols/arm64-v8a -dump crash.txt
```

[ndk-stack 文档](https://developer.android.com/ndk/guides/ndk-stack)说明了日志格式与符号输入要求。优化和内联会影响行号与调用栈，能解析地址不意味着已经得到完整 C# 语义栈。

### 线上崩溃平台与验证

以 Firebase 为例，Unity 接入说明要求把 `google-services.json` 放到项目 **Assets** 中，而不是自行指定 StreamingAssets；随后按所用 SDK 的流程安装 Crashlytics、初始化并上传 Android IL2CPP 符号。分别参考 [Unity 接入说明](https://firebase.google.com/docs/unity/setup)和 [Crashlytics Unity 配置](https://firebase.google.com/docs/crashlytics/unity/get-started)。

接入完成后，应在测试版本主动触发可控崩溃，确认平台收到记录、符号匹配且能定位到预期位置；同时确认 C# 异常与 Native Crash 的采集边界。上传命令成功不能替代这一步。

---

## 总结

Android 的优化依赖可验证的设备组合：图形 API 要对照画面和持续帧时，分发要检查设备实际得到的产物，热管理要区分状态与控制能力，恢复要分别确认进程、组件、绘制目标与业务状态，崩溃分析要保留匹配符号。把这些检查纳入构建与设备回归，才能判断一项设置是否适合自己的产品。
