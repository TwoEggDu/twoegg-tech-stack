---
title: "Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化"
slug: "mobile-unity-android"
date: "2026-03-28"
description: "Android 是碎片化最严重的移动平台，覆盖从骁龙 8 Gen 3 到 MT6762 的几十种 GPU 架构。本篇覆盖 Android 专项的核心决策：Vulkan vs OpenGL ES 的选择、Adaptive Performance 接入、AAB 包体优化，以及 Android 特有的性能陷阱。"
tags: ["Unity", "Android", "Vulkan", "OpenGL ES", "Adaptive Performance", "移动端"]
series: "移动端硬件与优化"
weight: 2200
---

## 1. Vulkan vs OpenGL ES：如何做选择

### 为什么 Vulkan 在高端设备上更好

Vulkan 是现代图形 API，相比 OpenGL ES 3.2，它的根本性优势体现在三个维度：

**CPU 驱动开销更低**。OpenGL ES 是状态机模型，驱动层需要在每次 Draw Call 前做大量状态校验和隐式同步，这部分开销完全落在 CPU 上。Vulkan 的 Pipeline State Object（PSO）把状态提前固化，驱动在运行时做的事情更少。在高通骁龙 865 设备上，实测 Vulkan 的 CPU 帧时间比 OpenGL ES 低约 **15-20%**，在 Draw Call 密集的场景下差距更明显。

**显式内存管理**。OpenGL ES 的内存分配完全由驱动决定，开发者无法控制纹理 / Buffer 放在哪块内存区域。Vulkan 提供了 `vkAllocateMemory`，可以指定内存类型（Device Local、Host Visible、Host Coherent）。Unity 在 Vulkan 后端会利用这一特性，把频繁更新的 Uniform Buffer 放在 Host Visible 区域，避免每帧的 GPU → CPU 回读。

**多线程 Command Buffer 录制**。OpenGL ES 的 Context 是线程绑定的，主线程以外录制命令需要共享 Context，有严格限制。Vulkan 的 Command Buffer 可以在任意线程独立录制，Unity 的 Job System 渲染线程能充分利用这一点。

### Vulkan 的劣势：驱动成熟度问题

Vulkan 的理论优势在低端设备上经常被驱动 Bug 抵消。几个典型问题：

- **联发科低端 SoC 的 Vulkan 驱动**（MT6762、MT6765 等）在部分 Android 8-9 设备上存在 Render Pass 执行顺序错误，表现为渲染错位或黑屏。
- **Mali G52/G57 的 Early-Z 优化在 Vulkan 下有时失效**，导致 overdraw 比 OpenGL ES 更严重。
- Android 7.0（API Level 24）是 Vulkan 1.0 的最低要求，但 API 24 的 Vulkan 驱动普遍质量差；实际上 API 28（Android 9）以下的 Vulkan 驱动建议谨慎使用。

### Unity 的 Graphics API 配置策略

在 **Player Settings → Other Settings → Graphics APIs** 中：

- 默认情况下 Unity 的列表顺序就是优先级顺序，运行时按顺序找第一个可用的 API。
- **推荐配置**：Vulkan 放第一位，OpenGL ES 3.2 / 3.1 作为 fallback。
- 取消勾选 **Auto Graphics API** 后可以手动控制。

对于需要按设备型号做精细控制的项目，可以在运行时检查 `SystemInfo.graphicsDeviceType` 并结合设备数据库（比如 GameBench 的 GPU 列表）在 C# 层做策略选择，但这需要通过 Unity 的 `-force-vulkan` / `-force-gles` 启动参数或自定义 Launcher 来实现，主工程内无法在初始化后切换 Graphics API。

**实用建议总结**：

| 设备档次 | 推荐 API | 依据 |
|---|---|---|
| 旗舰（骁龙 8 系、天玑 9 系、Xclipse 2 系） | Vulkan | 驱动成熟，CPU 收益明显 |
| 中端（骁龙 7 系、天玑 8 系、Mali G710+） | Vulkan（谨慎测试） | 驱动质量参差，需覆盖测试 |
| 低端（骁龙 4 系、天玑 6 系以下、Android < 9） | OpenGL ES 3.2 | Vulkan 驱动 Bug 风险高 |

---

## 2. Android Build Settings 逐项解析

### Target API Level

Google Play 要求 **targetSdkVersion 必须跟随最新要求**。截至 2025 年，新应用 / 更新应用均需 targetSdk = 34（Android 14）。

- `targetSdkVersion` 影响系统行为开关（比如 Android 12 的精确闹钟权限、Android 14 的 Photo Picker 强制使用）。
- `minSdkVersion` 决定安装门槛，建议设 24（Android 7.0），覆盖约 99% 的在用设备。
- 在 Unity 中：**Player Settings → Other Settings → Minimum / Target API Level**。

### Scripting Backend：必须 IL2CPP

**Google Play 强制要求 64-bit 支持（2019 年起）**，Mono 后端仅输出 ARMv7（32-bit），无法满足要求，因此 Android 发布必须使用 IL2CPP。

IL2CPP 的副作用：

- 构建时间增加（C# → C++ → Native 编译链）。
- 需要 NDK；Unity 会在安装时提供匹配版本，也可以在 **Preferences → External Tools** 指定自定义 NDK 路径。
- Strip Engine Code 和 Managed Stripping Level 要谨慎——过激的裁剪会删除反射依赖的类型，导致运行时 `MissingMethodException`。

### Target Architectures

- **ARM64**：必选，Google Play 强制要求。
- **ARMv7**：可选，仅用于覆盖极少数 32-bit Android 设备（主要是 Android 4-5 时代的遗留机型）。代价是包体增加约 **30%**（因为 IL2CPP 的 libil2cpp.so 需要额外编译一份 ARMv7 版本）。对于面向 2024 年以后的新游戏，直接去掉 ARMv7 即可。

### Internet Access

- `Auto`：仅当项目中有网络 API 调用时才添加 `android.permission.INTERNET`。
- `Require`：强制添加，无论是否用到网络。
- 建议设 `Auto`，让权限声明跟随实际需求，减少不必要的权限暴露。

### Minify（R8 混淆）

- 在 **Player Settings → Publishing Settings → Minify** 中，Debug 构建通常不开，**Release 构建建议启用 R8**。
- R8 会做：代码收缩（删除未用类）、混淆（重命名类 / 方法）、优化（内联、死代码删除）。
- 陷阱：**反射调用的类会被 R8 误删**。解决方案：
  1. 在 `proguard-user.txt`（Unity 自动包含进构建）中添加 `-keep` 规则保留反射用到的类。
  2. 第三方 SDK 通常会提供自己的 `proguard-rules.pro`，确认已合并进去。
- 打包后用 `bundletool dump resources` 检查 manifest，或用 Android Studio 的 APK Analyzer 验证类是否保留。

### Split Application Binary（OBB / PAD）

如果 APK 超过 100MB 限制（Google Play 针对 APK；AAB 可到 150MB），可以启用 **Split Application Binary**，Unity Assets 会打进独立的 OBB / Play Asset Delivery 包。现代项目推荐直接用 AAB + Play Asset Delivery，不用老的 OBB 方案。

---

## 3. AAB 与包体优化

### AAB vs APK：为什么必须切 AAB

Google Play 自 2021 年 8 月起**强制要求新应用提交 AAB**（Android App Bundle）。

AAB 的核心机制：开发者上传一个包含所有 ABI、语言、屏幕密度资源的 AAB，Google Play 在分发时按照用户设备的具体参数动态裁剪，生成一个只包含该设备所需内容的 APK（称为 Base APK + Configuration Split APKs）。

实际效果：相比提交通用 APK，用户下载的安装包通常减少 **15-30%**。

在 Unity 中启用 AAB：**Player Settings → Publishing Settings → Build → Build App Bundle (Google Play)**。

### Play Asset Delivery 的三种模式

| 模式 | 触发时机 | 适用场景 |
|---|---|---|
| Install Time | 与 App 一起安装 | 启动必须的核心资源（UI、第一关） |
| Fast Follow | 安装后立即在后台下载 | 游戏加载界面期间下载的资源 |
| On Demand | 玩家主动触发（进入关卡前） | 后期关卡、DLC 内容 |

Unity Addressables 与 Play Asset Delivery 的集成：使用 **Google Play Plugins for Unity**（com.google.play.assetdelivery）可以在 Addressables 的 Build Script 中直接输出 Asset Pack。

### 纹理压缩格式分包（Android Texture Compression Targeting）

Android 设备的 GPU 对纹理压缩格式的支持不一致：

- **ASTC**：高通骁龙 400+、Mali T760+、PowerVR GT7000+，支持覆盖约 95% 的 2020 年以后设备。
- **ETC2**：OpenGL ES 3.0 强制要求，几乎所有 Android 设备都支持，但压缩质量低于 ASTC。
- **ETC1**：最老的格式，不支持 Alpha 通道（需要拆分 RGB + Alpha）。

Unity 2021.2+ 支持 **Texture Compression Targeting**（在 Build Settings 中启用），打包时生成包含多套纹理的 AAB，Google Play 按设备分发对应版本。这样既保证高端设备的画质，又减少低端设备的 VRAM 占用。

配置路径：**Build Settings → Android → Texture Compression → 勾选 Use texture compression targeting**，然后在 Project Settings → Graphics 中设置各 Texture Format 对应的资源。

### 包体分析工具

**bundletool**（Google 官方工具，命令行）：

```bash
# 从 AAB 生成 APKS 集合（用于模拟分发）
bundletool build-apks --bundle=myapp.aab --output=myapp.apks

# 分析 AAB 内容
bundletool dump manifest --bundle=myapp.aab
bundletool dump resources --bundle=myapp.aab --resource=drawable/icon

# 查看 AAB 的实际分发大小估算
bundletool get-size total --apks=myapp.apks
```

**Unity Build Report**：构建完成后在 `Library/LastBuild.buildreport`（二进制格式），用 **Build Report Inspector** 包（com.unity.build-report-inspector）可以在 Editor 中可视化查看各资源的包体占用。

---

## 4. Adaptive Performance 接入

### 背景：为什么需要 Adaptive Performance

移动设备没有主动散热，长时间高负载运行会触发 Thermal Throttling（热降频）——CPU / GPU 主动降频以控制温度，帧率因此下跌。Adaptive Performance 的目标是在降频前主动感知热状态，提前降低渲染质量，保持帧率稳定而不是等待系统强制降频。

### 接入路径

1. 在 Package Manager 中安装 **Adaptive Performance**（com.unity.adaptiveperformance）。
2. 根据目标平台安装对应的 Provider：
   - 三星设备：**Adaptive Performance Samsung** (com.unity.adaptiveperformance.samsung.android)，依赖 Samsung GameSDK。
   - 高通设备（骁龙 7 Gen 2 及以上）：**Adaptive Performance Qualcomm** (com.unity.adaptiveperformance.qualcomm.snapdragon.spaces)。
3. 在 **Project Settings → Adaptive Performance** 中启用对应 Provider。

### 核心 API：IThermalStatus 与 IDevicePerformanceControl

```csharp
using UnityEngine.AdaptivePerformance;

public class ThermalManager : MonoBehaviour
{
    IAdaptivePerformance ap;
    IThermalStatus thermalStatus;
    IDevicePerformanceControl perfControl;

    void Start()
    {
        ap = Holder.Instance;
        if (ap == null || !ap.Active)
        {
            Debug.LogWarning("Adaptive Performance 不可用，跳过热管理初始化");
            enabled = false;
            return;
        }

        thermalStatus = ap.ThermalStatus;
        perfControl = ap.DevicePerformanceControl;

        // 订阅热事件
        thermalStatus.ThermalEvent += OnThermalEvent;
    }

    void OnDestroy()
    {
        if (thermalStatus != null)
            thermalStatus.ThermalEvent -= OnThermalEvent;
    }

    void OnThermalEvent(ThermalMetrics metrics)
    {
        switch (metrics.WarningLevel)
        {
            case WarningLevel.NoWarning:
                ApplyQualityLevel(QualityLevel.High);
                break;

            case WarningLevel.ThrottlingImminent:
                // 热降频即将发生，提前降质
                ApplyQualityLevel(QualityLevel.Medium);
                break;

            case WarningLevel.Throttling:
                // 已在降频，激进降质以稳定帧率
                ApplyQualityLevel(QualityLevel.Low);
                break;
        }
    }

    void ApplyQualityLevel(QualityLevel level)
    {
        switch (level)
        {
            case QualityLevel.High:
                QualitySettings.SetQualityLevel(2);
                Screen.SetResolution(
                    (int)(Screen.currentResolution.width),
                    (int)(Screen.currentResolution.height), true);
                break;

            case QualityLevel.Medium:
                QualitySettings.SetQualityLevel(1);
                // 降低分辨率到 80%
                Screen.SetResolution(
                    (int)(Screen.currentResolution.width * 0.8f),
                    (int)(Screen.currentResolution.height * 0.8f), true);
                // 关闭阴影
                QualitySettings.shadows = ShadowQuality.Disable;
                break;

            case QualityLevel.Low:
                QualitySettings.SetQualityLevel(0);
                // 降低分辨率到 65%
                Screen.SetResolution(
                    (int)(Screen.currentResolution.width * 0.65f),
                    (int)(Screen.currentResolution.height * 0.65f), true);
                QualitySettings.shadows = ShadowQuality.Disable;
                // 降低目标帧率
                Application.targetFrameRate = 30;
                break;
        }
    }

    enum QualityLevel { High, Medium, Low }
}
```

### 主动性能控制：CPU / GPU 性能等级

`IDevicePerformanceControl` 提供了 `cpuLevel` 和 `gpuLevel` 两个属性（范围 0 到 MaxCpuPerformanceLevel），直接影响硬件的时钟频率（通过 Samsung GameSDK / 高通 SDK 下发）。

```csharp
// 在加载界面允许 CPU 满速，进入游戏后适当降低以省电
void OnEnterGameplay()
{
    if (perfControl != null)
    {
        perfControl.cpuLevel = perfControl.MaxCpuPerformanceLevel - 1;
        perfControl.gpuLevel = perfControl.MaxGpuPerformanceLevel - 1;
    }
}

void OnEnterLoadingScreen()
{
    if (perfControl != null)
    {
        perfControl.cpuLevel = perfControl.MaxCpuPerformanceLevel;
        perfControl.gpuLevel = 0; // 加载期间 GPU 压力小
    }
}
```

---

## 5. Android 特有的性能陷阱

### 陷阱一：第三方 SDK 的 ContentProvider 导致启动慢

Android 的 ContentProvider 会在应用进程启动时（早于 `Application.onCreate()`）由系统自动初始化。大量第三方 SDK（广告 SDK、分析 SDK）为了"零配置接入"会注册自己的 ContentProvider，导致启动链变长。

**问题现象**：游戏冷启动时间超过 3 秒，adb logcat 里能看到大量 ContentProvider 初始化日志。

**排查方法**：

```bash
# 用 adb 启动并抓取启动时间
adb shell am start-activity -W -n com.your.package/.MainActivity

# 查看各 ContentProvider 初始化耗时
adb logcat | grep "ContentProvider"
```

**解决方案**：使用 **App Startup 库**（Jetpack）统一管理 ContentProvider 初始化时序，将非关键初始化推迟到主线程空闲后执行。对于无法修改的第三方 SDK，在 `AndroidManifest.xml` 中用 `tools:node="remove"` 移除其 ContentProvider 声明，然后手动在合适时机调用 SDK 初始化。

### 陷阱二：把 LMK 理解成"超过某个固定值就 OOM"

Android 的低内存问题，最容易被误解成："游戏一旦超过 1GB / 1.5GB 就会被系统杀掉。"

实际不是这样。Android 的内存回收是**全局系统行为**：`lmkd` 会结合当前可回收内存、Page Cache 回收效果、进程优先级（前台 / 可见 / 后台）来决定先杀谁。前台应用是**最后被杀**，不是**绝对不会被杀**。

这意味着同样一份游戏包、同样一个场景：
- 在后台很干净的 8GB 手机上，可能完全安全
- 在一台 4GB 手机上，如果系统里还挂着微信、输入法、相机、广告 SDK 的独立进程，就可能在切场景时直接回桌面

最常见的触发时机不是"慢慢涨爆"，而是几类瞬时峰值：
- **切场景双驻留**：旧场景还没卸载，新场景纹理和 Mesh 已经开始进来
- **资源解压与上传重叠**：Bundle 解压、纹理上传、Shader WarmUp、RenderTexture 分配同时发生
- **回前台恢复**：系统本来就在紧张区间，游戏又要恢复自己的常驻资源
- **常驻线过高**：对象池、可读纹理、可读 Mesh、过大的 RT 链先把稳态线顶高，剩下的峰值空间不够

Unity 能接到的标准信号之一，是 Android 的 `onTrimMemory()` 最终映射到 **`Application.lowMemory`**。

在 Unity 层最基础的兜底做法是：

```csharp
void OnEnable()
{
    Application.lowMemory += OnLowMemory;
}

void OnDisable()
{
    Application.lowMemory -= OnLowMemory;
}

void OnLowMemory()
{
    // 强制触发 GC
    System.GC.Collect();

    // 卸载未使用的 Asset（谨慎：会触发重新加载开销）
    Resources.UnloadUnusedAssets();

    // 降低纹理质量（下移一个 mipmap 级别）
    QualitySettings.masterTextureLimit = Mathf.Min(
        QualitySettings.masterTextureLimit + 1, 2);
}
```

注意两点：
- `Application.lowMemory` 通常已经比较晚了，它更像最后一道保险，不是舒适的提前预警
- `TRIM_MEMORY_RUNNING_CRITICAL` 通知意味着系统已经处在非常紧张的区间，此时不及时响应，前台应用也可能被系统强杀

**更稳的观测方式**：

```bash
# 查看进程内存分布（PSS / Private Dirty / Graphics / Native Heap）
adb shell dumpsys meminfo com.your.package

# 观察系统是否出现 lmkd / lowmemorykiller 相关日志
adb logcat | grep -E "lmkd|lowmemorykiller|onTrimMemory"
```

**工程上真正该做的事**：
1. 不要把预算定在"Development Build 的高配测试机还能跑"这一档，而要按最低支持机型定常驻线和峰值线。
2. 不要等 `lowMemory` 才第一次处理，应该提前在切场景、回前台、下载解压前后做分阶段清理。
3. 不要只盯着托管堆，Texture、RenderTexture、Mesh、对象池、Bundle 解压缓冲往往才是 LMK 的真正推手。

更完整的预算拆分、LMK / jetsam 区别和响应梯子，可继续看：
[CPU 性能优化 05｜内存预算管理：按系统分配上限、Texture Streaming 与 OOM/LMK 防护]({{< relref "performance/cpu-opt-05-memory-budget.md" >}})

### 陷阱三：Vulkan 驱动 Bug 的常见 Workaround

**问题一：Mali G76 / G77 上的 Render Pass Store Action 错误**

某些版本的 Mali 驱动在 Vulkan Render Pass 的 StoreOp 为 `DONT_CARE` 时会错误地丢弃仍在使用的 Attachment 数据。

Workaround：在 Unity 的 URP 配置中，将 Depth Buffer 的 Store Action 从 `DontCare` 改为 `Store`（在 Universal Renderer Data 的 Rendering 设置中调整）。

**问题二：高通 Adreno 500 系列的 Compute Shader 同步问题**

Adreno 530-540 的 Vulkan 驱动在某些 Compute Shader 之后的 Image Layout Transition 存在顺序错误，表现为 Compute 结果偶发性错误。

Workaround：在 Compute Dispatch 之后插入显式的 Pipeline Barrier，或者在 Player Settings 中对该系列设备强制使用 OpenGL ES。

可以在 C# 运行时检测并切换（需要通过启动参数，不能在运行时动态切换 Graphics API）：

```csharp
// 在 Application.quitting 前或通过自定义 Launcher 保存偏好
// 下次启动时读取并决定是否用 -force-gles 参数启动
string gpuName = SystemInfo.graphicsDeviceName; // e.g., "Adreno (TM) 530"
if (gpuName.Contains("Adreno") && gpuName.Contains("530"))
{
    PlayerPrefs.SetString("ForceGraphicsAPI", "GLES");
}
```

### 陷阱四：targetFrameRate 的精度问题

`Application.targetFrameRate = 60` 在 Android 上不能保证精确的 60fps，原因是 Android 的垂直同步由 **Choreographer** 控制，Choreographer 的回调精度受系统调度影响，在高负载时可能出现 ±2ms 的抖动。

**更稳定的方案**：使用 `QualitySettings.vSyncCount`，配合设备的 `Screen.currentResolution.refreshRateRatio`：

```csharp
// 在支持 120Hz 的设备上实现稳定 60fps
void SetStableFrameRate(int targetFps)
{
    int screenRefreshRate = (int)Screen.currentResolution.refreshRateRatio.value; // e.g., 120
    int divisor = Mathf.Max(1, screenRefreshRate / targetFps);
    QualitySettings.vSyncCount = divisor; // 120Hz 屏幕上设 2 = 60fps
    Application.targetFrameRate = -1; // 禁用 targetFrameRate，完全交给 vSync
}
```

---

## 6. Android 崩溃分析（IL2CPP 符号化）

### Native Crash：tombstone 文件

Android 的 Native Crash（SIGSEGV、SIGABRT 等）会在 `/data/tombstones/` 目录生成 tombstone 文件（需要 root 或 adb 调试权限）。

```bash
# 拉取最新的 tombstone
adb shell run-as com.your.package cat /data/tombstones/tombstone_00 > tombstone.txt

# 或者（需要 root）
adb root
adb pull /data/tombstones/tombstone_00
```

tombstone 文件包含崩溃时的寄存器状态和 backtrace，但地址是未符号化的。

### 用 ndk-stack 符号化

IL2CPP 构建会生成带调试信息的 `.so` 文件，位于：

```
<ProjectRoot>/Temp/StagingArea/libs/<abi>/libil2cpp.so
```

（Release 构建时这些符号文件会从最终 APK/AAB 中剥离，但 Unity 会在本地保留一份。建议每次发布时归档对应的符号文件和 Symbols 包。）

```bash
# 使用 NDK 自带的 ndk-stack 符号化
$ANDROID_NDK_HOME/ndk-stack -sym ./Temp/StagingArea/libs/arm64-v8a/ \
  -dump tombstone.txt > symbolized_crash.txt

# 或者用 addr2line 对单个地址符号化
$ANDROID_NDK_HOME/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-addr2line \
  -C -f -e libil2cpp.so 0x000000000012abcd
```

### Unity Cloud Diagnostics / Firebase Crashlytics

对于线上崩溃，手动拉 tombstone 不现实。推荐接入崩溃收集平台：

**Firebase Crashlytics**（推荐，行业主流）：
1. 在 Firebase Console 创建项目，下载 `google-services.json` 放入 `Assets/StreamingAssets/`（或 Android Plugins 目录）。
2. 通过 External Dependency Manager 导入 Firebase Unity SDK（firebase_unity_sdk.zip 中的 `FirebaseCrashlytics.unitypackage`）。
3. 构建时生成 `crashlytics-build.properties`，包含 Build ID；上传 `.so` 符号文件：

```bash
# Firebase CLI 上传符号
firebase crashlytics:symbols:upload --app=<APP_ID> \
  ./Temp/StagingArea/libs/arm64-v8a/libil2cpp.so
```

4. Crashlytics 控制台会自动符号化，显示 C# 堆栈（IL2CPP 映射）和 Native 堆栈。

**Unity Cloud Diagnostics**（适合已有 Unity Gaming Services 的项目）：
- 在 **Window → Services → Cloud Diagnostics** 中启用。
- 自动收集 C# Exception 和 Native Crash，与 Unity Dashboard 集成。
- IL2CPP 符号文件需要在每次构建后上传到 Dashboard（或通过 CI 自动化）。

---

## 总结

Android 平台的优化核心在于**分层决策**：

1. **Graphics API**：高端设备优先 Vulkan，低端设备 fallback OpenGL ES，不要一刀切。
2. **Build Settings**：IL2CPP + ARM64 是必选项，Minify 要配合 ProGuard 规则，避免 R8 误删反射类。
3. **包体**：切换到 AAB，使用 Play Asset Delivery 做资源分发，Texture Compression Targeting 处理 GPU 格式差异。
4. **热管理**：接入 Adaptive Performance，在热降频前主动降质，而不是等系统强制限频。
5. **崩溃分析**：IL2CPP 符号文件要归档，接入 Firebase Crashlytics 实现线上符号化。

Android 碎片化的本质是：不存在"一套配置打天下"的方案，需要持续的设备测试矩阵和运行时适配逻辑。
