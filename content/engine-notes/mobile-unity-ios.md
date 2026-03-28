---
title: "Unity on Mobile｜iOS 专项：Metal 渲染行为、内存警告机制与 Instruments 联用"
slug: "mobile-unity-ios"
date: "2026-03-28"
description: "iOS 平台以统一的 Apple Silicon 生态提供了更可预测的性能，但也有其独特的约束：Metal 独占、内存警告机制、App Store 审核的包体要求。本篇覆盖 iOS 专项的核心知识点。"
tags: ["Unity", "iOS", "Metal", "Instruments", "Apple", "移动端"]
series: "移动端硬件与优化"
weight: 2210
---

## 1. Metal 的特点与 Unity 的适配

### iOS 为什么只有 Metal

OpenGL ES 在 iOS 12（2018 年）已被 Apple 正式标记为 Deprecated，iOS 不会新增 OpenGL ES 功能，驱动层也不再接受 Bug 修复。Unity 从 2020.1 起移除了 iOS 上的 OpenGL ES 后端，iOS 项目只能使用 Metal。

这意味着：面向 iOS 的 Unity 项目，Graphics API 选择不需要任何决策，Metal 是唯一答案。

### Metal 与 OpenGL ES 的核心概念差异

**CommandBuffer 的显式管理**

OpenGL ES 的命令提交是隐式的，驱动在某个时机将命令批量提交给 GPU。Metal 的 `MTLCommandBuffer` 是显式对象，开发者（或引擎）需要：
1. 从 `MTLCommandQueue` 获取 CommandBuffer。
2. 编码渲染指令（通过 `MTLRenderCommandEncoder`）。
3. 调用 `commit()` 提交，调用 `presentDrawable()` 显示帧。

Unity 在 Metal 后端封装了这一流程，但理解这个模型有助于分析 GPU Capture 的时间轴。

**Render Pass Descriptor：Load Action 和 Store Action**

Metal 的 Render Pass 使用 `MTLRenderPassDescriptor` 描述每个 Attachment 的加载 / 存储行为，这直接影响 GPU 的带宽消耗：

| Action | 含义 | 带宽代价 |
|---|---|---|
| `MTLLoadActionClear` | 用指定颜色填充 Attachment | 写操作（低开销） |
| `MTLLoadActionLoad` | 从 DRAM 读取之前的内容 | 读 DRAM（中等开销） |
| `MTLLoadActionDontCare` | 不关心初始内容 | 无带宽消耗 |
| `MTLStoreActionStore` | 将结果写回 DRAM | 写 DRAM（中等开销） |
| `MTLStoreActionDontCare` | 不保留结果 | 无带宽消耗 |
| `MTLStoreActionMultisampleResolve` | MSAA Resolve 并写回 | 读 + 写 DRAM |

**Unity 的自动设置逻辑**：URP 会根据 Camera 的 Clear Flags 自动选择 Load Action（`SolidColor` → Clear，`Nothing` → DontCare），Store Action 根据是否有后续 Pass 读取该 RT 来决定（中间 RT 用 DontCare，最终输出用 Store）。

**在 URP 中手动控制 Load/Store Action**：

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPass : ScriptableRenderPass
{
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // 告诉 URP 这个 Pass 不需要读取 Color Buffer 的历史内容
        ConfigureClear(ClearFlag.All, Color.black);

        // 在 Pass 末尾配置 Store Action
        // 如果后续没有 Pass 读取这个 RT，设置为 DontCare 节省带宽
        ConfigureTarget(colorAttachmentHandle, depthAttachmentHandle);
    }
}
```

对于需要完全控制的情况，可以通过 `CommandBuffer.SetRenderTarget()` 的重载版本指定 `RenderBufferLoadAction` 和 `RenderBufferStoreAction`：

```csharp
// 明确指定 Load/Store Action
cmd.SetRenderTarget(
    colorBuffer,
    RenderBufferLoadAction.DontCare,   // 不从 DRAM 读取（这帧会重新渲染全部内容）
    RenderBufferStoreAction.Store,     // 写回 DRAM（后续帧需要读取）
    depthBuffer,
    RenderBufferLoadAction.DontCare,
    RenderBufferStoreAction.DontCare   // 深度不需要保留
);
```

### Memoryless Render Targets：节省 DRAM 的关键

Apple Silicon 的 GPU 采用 Tile-Based Deferred Rendering（TBDR）架构，Tile Memory 是 GPU 内的片上缓存，访问速度远高于 DRAM。

**Memoryless RT**（在 Unity 中称为 Memoryless Depth / MSAA）的原理：Render Target 仅存在于 Tile Memory 中，完全不占用 DRAM。这适用于：

- **MSAA 的中间缓冲区**：MSAA 需要一个高采样率的中间 RT，最终 Resolve 到普通 RT。这个中间 RT 只在当前 Pass 内使用，设为 Memoryless 可节省大量内存和带宽。
- **Depth Buffer**（如果后续不需要读取深度）：大多数情况下深度只在当前帧内使用，Resolve 之后就没用了。

在 Unity 的 Player Settings 中启用：**Player Settings → iOS → Memoryless Depth**。

对于 MSAA Resolve Buffer，URP 会在支持 Memoryless 的设备上（iOS 10+）自动使用，无需手动配置。

---

## 2. iOS Build Settings 关键配置

### Target iOS Version

**推荐：iOS 15+**。

理由：
- iOS 15 覆盖率约 98%（Apple 设备的系统更新率极高）。
- iOS 15 引入了 `UISheetPresentationController`（弹窗 UI）、`StoreKit 2`（新内购 API）等常用功能。
- iOS 14 及以下设备大多是 iPhone 6s / 7 系列（A9 / A10 芯片），性能较弱，排除后有助于简化性能分级。

API Level 对应关系：iOS 15 = iPhone 6s 及以上（A9 芯片），iOS 16 = iPhone 8 及以上（A11 芯片，Neural Engine 可用）。

### Architecture：ARM64 Only

**ARMv7（32-bit）在 iOS 11 起不再被 Apple 支持**，Xcode 默认已移除。Unity iOS 构建只输出 ARM64，无需配置。

### Bitcode：已废弃，不要启用

Apple 在 Xcode 14（2022 年）正式移除了 Bitcode 支持，现有应用的 Bitcode Slices 会被忽略。Unity 的 Xcode 项目模板已将 `ENABLE_BITCODE` 默认设为 `NO`。

如果发现旧项目里 Bitcode 仍为 `YES`，或者某个第三方 `.framework` 强制要求 Bitcode，需要清理：在生成的 Xcode 项目的 Build Settings 中搜索 `ENABLE_BITCODE`，确认为 `NO`。

### ProMotion（120Hz）启用

iPhone 13 Pro / 14 Pro / 15 Pro 支持 ProMotion（1-120Hz 动态刷新率），但默认情况下 App 的最高帧率被限制在 60fps。

启用 120Hz 的方式：在 `Info.plist` 中添加键值：

```xml
<key>CADisableMinimumFrameDurationOnPhone</key>
<true/>
```

在 Unity 中，通过 **Player Settings → iOS → Other Settings → Disable Depth and Stencil** 旁边没有直接入口；需要通过 `Assets/Plugins/iOS/Info.plist`（或 PostProcessBuild 脚本）添加：

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

        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // 启用 ProMotion 120Hz
        plist.root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);

        plist.WriteToFile(plistPath);
    }
}
```

启用后，还需要在代码里正确设置目标帧率（见第 6 节的性能陷阱）。

### App Store 包体要求

| 限制 | 说明 |
|---|---|
| 超过 50MB | 通过 Cellular 下载时系统会弹出提示（iOS 13 以下是强制提示，iOS 13+ 可在设置中关闭提示） |
| 单个二进制文件不超过 500MB | Mach-O 可执行文件的上限 |
| App 总大小不超过 4GB | 含所有资源 |

对于超过 50MB 的资源，推荐使用 **On Demand Resources（ODR）** 或将内容放到 CDN 通过 Addressables 下载，而不是塞进初始包。

ODR 的 Unity 配置：**Player Settings → iOS → On Demand Resources**，将 Asset Bundle 标记为对应的 ODR Tag，Xcode 会在 Archive 时自动上传到 App Store。

---

## 3. iOS 内存警告机制

### iOS 内存分级：没有明确上限

Android 有明确的 `ActivityManager.MemoryInfo.totalMem` 和 OOM 杀进程机制，iOS 的内存管理则更像一个连续的"压力梯度"：

1. **正常状态**：App 自由使用内存，系统维护 Page Cache 作为缓冲。
2. **内存压力上升**：系统开始向 App 发送 `applicationDidReceiveMemoryWarning`（对应 Unity 的 `Application.lowMemory`）。App 应主动释放非必要资源。
3. **后台 App 被 Jettison**：内存继续紧张时，系统按优先级（后台挂起 → 后台执行 → 前台）终止进程。
4. **前台 App 被 Jettison**：极端情况下前台 App 也会被强杀。Crash Log 里会有 `jettisoned` 标记（而非普通崩溃）。

**与 Android 的关键差异**：iOS 没有类似 OOM Score 的公开机制，也不提供"当前可用内存"的系统 API（`os_proc_available_memory()` 在 iOS 13+ 提供了近似值，但不精确）。开发者只能被动响应 `lowMemory` 事件，而不能主动查询剩余空间。

### 实测内存数据（仅供参考，受系统版本和后台 App 影响）

| 设备 | RAM | 游戏可用内存（估算） |
|---|---|---|
| iPhone 12（A14，4GB RAM） | 4GB | 约 1.8-2.5GB |
| iPhone 14（A15，6GB RAM） | 6GB | 约 2.5-3.5GB |
| iPhone 15 Pro（A17 Pro，8GB RAM） | 8GB | 约 3.5-4.5GB |
| iPad Pro M2（16GB RAM） | 16GB | 约 8-10GB |

这里的"可用内存"指游戏实际可分配的内存上限（超过后很快触发 Jettison），并非物理空闲内存。

### lowMemory 的响应策略

```csharp
using UnityEngine;

public class MemoryWarningHandler : MonoBehaviour
{
    void OnEnable()
    {
        Application.lowMemory += HandleLowMemory;
    }

    void OnDisable()
    {
        Application.lowMemory -= HandleLowMemory;
    }

    void HandleLowMemory()
    {
        Debug.LogWarning($"[Memory] 低内存警告 | 当前 GC 堆：{System.GC.GetTotalMemory(false) / 1048576}MB");

        // 1. 强制 GC（Managed 堆压缩）
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();

        // 2. 卸载未被引用的 Asset（异步，避免阻塞主线程）
        Resources.UnloadUnusedAssets();

        // 3. 降低纹理 mipmap 质量（立即生效，无需重新加载）
        int currentLimit = QualitySettings.globalTextureMipmapLimit;
        if (currentLimit < 2)
        {
            QualitySettings.globalTextureMipmapLimit = currentLimit + 1;
            Debug.Log($"[Memory] 纹理 Mipmap 限制提升至 {QualitySettings.globalTextureMipmapLimit}");
        }

        // 4. 通知游戏逻辑层（可以触发关卡内的资源精简策略）
        GameEvents.Broadcast(GameEventType.LowMemoryWarning);
    }
}
```

注意：`Resources.UnloadUnusedAssets()` 触发后，下次访问已卸载的资源会重新从磁盘加载，有延迟。对于当前场景必须持续使用的资源，不要卸载。

---

## 4. Xcode Instruments 与 Unity 联用

Unity Profiler 看的是 Managed（C#）层的视图，Instruments 看的是 Native 层的视图。两者互补，覆盖完整的性能分析链路。

### 前置：在真机上以 Development Build 连接

1. Unity 中勾选 **Development Build** 和 **Autoconnect Profiler**，构建并安装到测试设备。
2. 在 Xcode 中，选择 **Product → Profile**（或 Cmd+I）打开 Instruments，选择目标 App 和设备。
3. 也可以在 App 运行中通过 **Debug → Attach to Process** 连接到已运行的进程。

### Time Profiler：CPU 函数耗时

Time Profiler 以固定频率（默认 1ms）对所有线程进行采样，记录调用栈。

**与 Unity Profiler 的互补**：
- Unity Profiler 可以看到 `Update()`、`LateUpdate()`、`Physics.Step` 等 C# 层的耗时，但 IL2CPP 生成的 Native 函数名被截断（显示为 `il2cpp_gc_allocate` 等）。
- Time Profiler 可以看到完整的 Native 调用栈，包括 Metal API 调用（`MTLCommandBuffer commit`）、UIKit 回调、第三方 SDK 的 ObjC 方法。

**典型使用场景**：发现 Unity Profiler 显示某帧在 `Player Loop` 里有 8ms 的未知耗时，在 Time Profiler 里能定位到是某个第三方 SDK 的 `[AnalyticsManager flushEvents:]` 方法占用了 6ms。

**实用技巧**：在 Time Profiler 的 Call Tree 中，打开 **Invert Call Tree** 可以快速找到最耗时的叶子函数；打开 **Hide System Libraries** 过滤掉系统框架，专注于 App 代码。

### Allocations：内存分配追踪

Allocations 追踪 ObjC（`alloc`）、C（`malloc`）、C++（`operator new`）的内存分配。

**Unity 中 Managed 堆的分配不会被 Allocations 追踪**（Mono / IL2CPP 有自己的内存管理，使用大块 `vm_allocate` 预留虚拟地址空间）。Allocations 主要用于：
- 检测 iOS 原生插件（.mm 文件）的内存泄漏。
- 检测第三方 SDK 的内存问题。
- 追踪 Metal Resource（`MTLBuffer`、`MTLTexture`）的分配，这些在 Unity 的 GPU Memory 报告中可见，但 Allocations 可以追踪到具体的分配调用栈。

**使用 VM Tracker**：在 Allocations 的 VM Tracker 视图中，可以看到按内存类型分组的占用：
- `IOSurface`：Metal Texture（GPU / CPU 共享内存）
- `CG image`：UIKit 的图片缓存
- `MALLOC_TINY / MALLOC_SMALL / MALLOC_LARGE`：C 堆的不同大小分配池

### Energy Diagnostics：功耗分析

Energy Log 工具（在 Instruments 中是 Energy Diagnostics 模板）记录 CPU、GPU、网络、定位等各模块的功耗热力图。

**实用场景**：优化游戏后台功耗（iOS 后台时游戏应暂停渲染），或者分析某个特定时段功耗异常高（比如进入战斗时 CPU 功耗突增，可能是粒子系统 CPU 计算过多）。

使用步骤：
1. 在 Instruments 中选择 **Energy Log** 模板。
2. 在真机上运行 App（需要从 Xcode 启动或已安装 Development 版本）。
3. 模拟正常游戏流程（包括进入高负载场景、切换后台、返回前台）。
4. 停止记录，查看时间轴上各组件的功耗分布。

### Metal System Trace：GPU / CPU 同步关系

Metal System Trace 是最接近 GPU 底层的 Instruments 模板，它显示：
- CPU 线程何时提交 `MTLCommandBuffer`。
- GPU 何时开始 / 结束执行（Vertex / Fragment 分开显示）。
- CPU 和 GPU 之间的等待（Pipeline Stall）。

**与 Xcode GPU Frame Capture 的互补**：
- GPU Frame Capture（在 Xcode 调试时按相机图标触发）用于分析单帧内各 Draw Call 的详细信息（Shader 耗时、Attachment 状态）。
- Metal System Trace 用于分析多帧的时间轴，找 CPU-GPU 同步瓶颈。

**典型问题：CPU 等待 GPU**

如果在 Metal System Trace 里看到 CPU 线程有长时间的 `MTLCommandBuffer waitUntilCompleted` 等待，说明 CPU 提交命令过快，GPU 还没处理完，CPU 被迫等待。解决方案：减少单帧 Draw Call 数量，或在 CPU 端增加帧间缓冲（使用 `MTLEvent` 做异步通知而非阻塞等待，Unity 内部的 Metal 实现已经处理了这一点，但过多的 `AsyncGPUReadback` 可能打破平衡）。

---

## 5. iOS 崩溃分析

### dSYM 文件的生成与管理

dSYM（Debug Symbols）是 Xcode 在编译时生成的符号文件，用于将 crash log 中的内存地址映射回源码行号。

**生成时机**：
- Development Build：Xcode 自动生成 `.dSYM`，位于 `<Xcode Build Dir>/Products/Debug-iphoneos/`。
- Archive（发布用）：**Product → Archive** 时，dSYM 存储在 `.xcarchive` 包内（右键 Show Package Contents → dSYMs 目录）。

**重要原则**：每次发布的 dSYM 必须归档，且要与对应的 App 版本严格匹配（UUID 绑定）。dSYM 的 UUID 变更意味着无法符号化旧版本的崩溃。

Unity 的 IL2CPP 构建会生成两个关键文件：
- `libil2cpp.dylib`（或编译进 App 的 .a 文件）
- `il2cpp_backtrace` 目录内的 `il2cpp_method_addresses.json`（用于 C# 方法的额外映射）

### 用 symbolicatecrash 符号化

```bash
# 设置 DEVELOPER_DIR（指向 Xcode 的 Developer 目录）
export DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer

# 找到 symbolicatecrash 工具
SYMBOLICATE=$(find $DEVELOPER_DIR -name "symbolicatecrash" 2>/dev/null | head -1)

# 符号化 crash log（.crash 文件从 Xcode Organizer 导出）
$SYMBOLICATE MyApp.crash MyApp.app.dSYM > symbolized.crash

# 如果有多个 dSYM（App 本体 + 框架），可以指定目录
$SYMBOLICATE MyApp.crash ./dSYMs/ > symbolized.crash
```

或者使用 **Xcode Organizer**（更简单）：**Window → Organizer → Crashes**，Xcode 会自动从 App Store Connect 拉取崩溃报告并符号化（需要 Archive 时的 dSYM 已上传）。

### 常见 iOS 特有崩溃类型

**Signal SIGABRT**：通常是 ObjC 异常被抛出后未捕获，或者 NSAssert 断言失败。常见原因：
- 使用了 nil 的 weak 指针（ObjC 下 nil message 不崩溃，但某些 API 对 nil 参数会抛异常）。
- 第三方 SDK 的 `@throw` 异常未被 Unity 的异常处理链捕获。

**EXC_BAD_ACCESS**：野指针或 nil 对象的内存访问。在 IL2CPP 中常见原因：
- C# 侧已销毁的对象被 Native 回调引用（典型：iOS 广告 SDK 的完成回调在 Unity 对象已销毁后触发）。
- `DllImport` 的 Native 函数参数传递错误（类型不匹配导致内存越界）。

**Jettison（内存压力终止）**：Crash Log 里没有 Exception Type，而是有以下特征：
```
Exception Type:  EXC_RESOURCE
Exception Subtype: MEMORY
Termination Reason: Namespace SPRINGBOARD, Code 0x8badf00d
```
或者在 `jetsam_event_report.json`（系统日志，可通过 Settings → Privacy → Analytics & Improvements 导出）中有对应记录。

Jettison 不是 Bug，是系统行为。应对方式是减少内存峰值用量（见第 3 节的 lowMemory 响应策略）。

---

## 6. iOS 特有的性能陷阱

### 陷阱一：Metal Shader 的同步编译卡顿

**问题**：首次进入包含新 Shader 的场景时，游戏卡顿 0.5-3 秒，帧率骤降到个位数。

**原因**：Metal 的 Pipeline State Object（PSO）必须在 GPU 驱动层编译，这个编译发生在**首次渲染使用该 PSO 的 Draw Call 时**，是同步阻塞的。与 OpenGL ES 不同，Metal 没有懒编译优化，整个编译过程发生在 CPU 端，完成前 GPU 无法执行后续命令。

**解决方案 1：Shader Warmup（Unity 内置）**

```csharp
// 在加载界面预热 Shader
using UnityEngine.Rendering;

void PrewarmShaders()
{
    // 收集场景中所有 Renderer 的 Material
    Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
    var materials = new System.Collections.Generic.HashSet<Material>();
    foreach (var r in renderers)
        foreach (var m in r.sharedMaterials)
            if (m != null) materials.Add(m);

    // 提交一次全屏渲染触发 PSO 编译（发生在加载界面，用户感知不到卡顿）
    Shader.WarmupAllShaders();
}
```

**解决方案 2：Metal Binary Archive（iOS 16+）**

iOS 16 引入了 Metal Binary Archive，可以将 PSO 的编译结果缓存到磁盘。首次运行后，后续启动直接加载预编译的 PSO，彻底消除 Shader 编译卡顿。

在 Unity 6 的 URP 中，这个功能通过 **Project Settings → Graphics → Pipeline Specific Settings → Metal Binary Archives** 配置，会在发布包内预置一份 Binary Archive。

### 陷阱二：ProMotion 自适应帧率的正确设置

iPhone 15 Pro 支持 1-120Hz 动态刷新（ProMotion），`CADisplayLink` 的回调频率会根据内容动态调整。

**错误做法**：只设置 `Application.targetFrameRate = 120`。
这样设置后，iOS 内部的 `CADisplayLink` 的 `minimumFramesDuration` 会被设为 `1/120`，系统会保持 120Hz 刷新率，**即使 GPU 渲染时间不足 8.3ms**，也会空转等待，浪费电量。

**正确做法**：通过 `preferredFrameRateRange` 设置帧率范围（iOS 15+）：

```csharp
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;

public static class iOSFrameRate
{
    // iOS Native 插件（Assets/Plugins/iOS/FrameRateHelper.mm）
    [DllImport("__Internal")]
    private static extern void SetPreferredFrameRateRange(float minimum, float maximum, float preferred);

    public static void SetAdaptiveFrameRate(int targetFps)
    {
        // 允许系统在 30-120Hz 之间自适应，目标 60fps
        // 系统会在内容帧率稳定时降低刷新率节省电量
        SetPreferredFrameRateRange(30f, 120f, targetFps);
    }
}
#endif
```

对应的 ObjC 插件（`FrameRateHelper.mm`）：

```objc
#import <UIKit/UIKit.h>

extern "C" void SetPreferredFrameRateRange(float minimum, float maximum, float preferred)
{
    if (@available(iOS 15.0, *)) {
        // 找到 Unity 的 CADisplayLink 并设置帧率范围
        // Unity 使用私有 API 持有 DisplayLink，这里通过 UnityGetMainDisplayLink 获取
        // 实际项目中可能需要通过 UnityAppController 桥接
        CAFrameRateRange range = CAFrameRateRangeMake(minimum, maximum, preferred);
        // UnityGetMainDisplayLink().preferredFrameRateRange = range;
        // 简化版：设置 Application.targetFrameRate
        // Unity 6 已经原生支持 preferredFrameRateRange，可通过 Player Settings 配置
    }
}
```

**Unity 6 的原生支持**：Unity 6 在 **Player Settings → iOS → Frame Rate → Preferred Frame Rate Range** 提供了直接配置入口，无需手写插件。

### 陷阱三：JIT 禁止与 AOT 限制

**iOS 不允许在运行时生成可执行代码（JIT）**，这是 Apple 的安全策略。所有 C# 代码必须通过 IL2CPP 提前编译（AOT，Ahead of Time）。

这带来的限制：
- **`System.Reflection.Emit` 不可用**：运行时动态生成 IL 代码会直接崩溃。
- **`Assembly.Load()` 从字节流加载程序集不可用**（iOS 12 之前的 Mono 版本存在此问题）。
- **某些序列化库依赖 JIT**：比如旧版本的 Newtonsoft.Json 的部分代码路径使用 `Emit`，在 iOS 上会 fallback 到 Reflection（较慢），但不崩溃。确认使用支持 AOT 的版本。
- **Expression Tree 的编译**：`Expression.Compile()` 内部使用 `Emit`，在 iOS 上不可用，需要改用 `Expression.Interpret()` 或避免运行时 Lambda 编译。

检测方式：在 Unity 的 iOS Build Report 中搜索 `System.Reflection.Emit` 的引用，或使用 **IL2CPP Strip 的 AOT Analysis** 工具（需要 IL2CPP 调试构建）。

### 陷阱四：App 进入后台后的挂起限制

iOS App 进入后台（用户按 Home 键或切换到其他 App）后，系统会给约 **5 秒的时间**让 App 做必要的保存工作，之后 App 被挂起（进程存在但不执行任何代码）。

**Unity 的处理**：Unity 在收到 `applicationWillResignActive` 时会暂停渲染，在 `applicationDidEnterBackground` 时暂停游戏（等效于 `Application.pauseMessage`）。

**常见陷阱**：

1. **后台 BGTask 超时**：如果申请了 `beginBackgroundTask` 做后台工作（比如存档、上传积分），但任务超过系统限制时间（约 30 秒），App 会被强杀，crash log 里显示 `0x8badf00d`（watchdog timeout）。

2. **网络请求在后台继续**：Unity 的 `UnityWebRequest` 在 App 进入后台时不会自动取消，但 iOS 的网络权限会限制后台请求（除非声明了 Background Modes → Background fetch）。

3. **`OnApplicationPause(false)` 的时机**：从后台返回前台时，`OnApplicationPause(false)` 触发，这时 GPU 可能已经 Lost Context（实际 Metal 不存在 Lost Context 问题，但 RT 的内容可能已被系统回收）。如果游戏依赖后台时不变的 RT 内容，需要在恢复时重新渲染。

```csharp
void OnApplicationPause(bool paused)
{
    if (paused)
    {
        // 进入后台：保存游戏状态，暂停计时器
        GameStateManager.Instance.SaveProgress();
        GameTimer.Pause();
    }
    else
    {
        // 从后台恢复：刷新 UI（时间可能已经过了很久）
        GameTimer.Resume();
        UIManager.Instance.RefreshAllTimeDisplays();
    }
}
```

---

## 总结

iOS 平台相比 Android 的优势在于**生态统一**：只需针对 Metal 优化，不需要处理 GPU 厂商差异；Xcode Instruments 提供了完整的 Native 分析工具链。

核心要点：

1. **Metal Load/Store Action**：正确设置可以显著降低 GPU 带宽消耗，TBDR 架构对 DontCare 优化最敏感。
2. **内存管理**：iOS 没有明确内存上限，必须响应 `lowMemory` 事件，Memoryless RT 是节省内存的有效手段。
3. **Instruments 工具链**：Time Profiler 看 CPU Native 耗时，Allocations 看原生内存，Metal System Trace 看 GPU 同步关系。三者配合 Unity Profiler 覆盖全层次分析。
4. **Shader 编译**：Metal Binary Archive 是 iOS 16+ 消除首次进场卡顿的根本方案。
5. **AOT 约束**：IL2CPP + JIT 禁止意味着所有依赖运行时代码生成的库需要提前验证 iOS 兼容性。
6. **后台挂起**：5 秒限制是硬约束，后台工作必须在 BGTask 的时间窗口内完成或妥善取消。
