---
title: "Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化"
slug: "mobile-unity-android"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "区分 Android 图形 API、构建分发、热管理和内存终止的机制与验证边界，说明 Unity 项目应如何建立设备测试与崩溃符号归档。"
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

Android 的优化依赖可验证的设备组合：图形 API 要对照画面和持续帧时，分发要检查设备实际得到的产物，热管理要区分状态与控制能力，崩溃分析要保留匹配符号。把这些检查纳入构建与设备回归，才能判断一项设置是否适合自己的产品。
