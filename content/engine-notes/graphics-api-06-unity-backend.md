+++
title = "图形 API 06｜Unity 的图形后端：GraphicsDeviceType、API 选择与平台兼容性"
slug = "graphics-api-06-unity-backend"
date = 2026-03-26
description = "Unity 在应用程序代码和底层图形 API 之间插入了一层图形后端（Graphics Backend），封装了 DX11/DX12/Vulkan/Metal/OpenGL ES 的差异。这篇讲清楚 Unity 图形后端的结构、各平台默认选择、如何在运行时查询，以及切换 API 时需要注意什么。"
weight = 750
[taxonomies]
tags = ["图形API", "Unity", "GraphicsDeviceType", "Vulkan", "Metal", "DX11", "平台兼容性"]
[extra]
series = "图形 API 基础"
+++

## 图形后端的位置

Unity 的渲染调用链从上到下是这样的：

```
Unity C# 代码（CommandBuffer、Graphics.DrawMesh、RenderPipelineManager）
    ↓
Unity 内部 GfxDevice 抽象层（C++ 接口）
    ↓
平台图形后端（GfxDeviceD3D11 / GfxDeviceD3D12 / GfxDeviceVulkan / GfxDeviceMetal / ...）
    ↓
原生图形 API（Direct3D 11/12、Vulkan、Metal、OpenGL ES）
    ↓
GPU 驱动
```

这层 GfxDevice 抽象是 Unity 跨平台能力的核心基础设施。同一份 C# 代码和 HLSL Shader，在 PC 上走 D3D11，在 iPhone 上走 Metal，在 Android 上走 Vulkan，上层逻辑完全不感知差异。

这套抽象的代价是有限制的：某些原生 API 能力（如 DX12 的 ExecuteIndirect、Metal 的 argument buffer tier 2）Unity 不一定立刻暴露，需要通过 `CommandBuffer.IssuePluginEventAndData` 或 Native Plugin 才能直接调用。

---

## 各平台默认图形 API

Unity 2022.3 LTS 各平台的默认 API 和可选项：

| 平台 | 默认 API | 可选项 |
|------|---------|--------|
| Windows PC | Direct3D 11 | Direct3D 12、Vulkan、OpenGL Core |
| macOS | Metal | OpenGL Core（已弃用，Unity 2023 移除）|
| iOS | Metal | — |
| Android | Vulkan（Android 7.0+）| OpenGL ES 3.0（fallback）|
| Linux | Vulkan | OpenGL Core |
| WebGL | WebGL 2（OpenGL ES 3.0 子集）| 待 WebGPU（Unity 6 实验性）|
| PS5 | GNM / GNMX（索尼专有）| — |
| Xbox Series X\|S | Direct3D 12 | — |
| Nintendo Switch | NVN（任天堂专有）| Vulkan（2022.3+）|

主机平台的图形 API 不对外公开，必须签署 NDA 才能拿到 SDK。这也是为什么 Unity 对 PC/移动端的后端实现是开源的（URP/HDRP Shader 代码可以直接读），主机后端一个字都看不到。

---

## 运行时查询图形后端

`SystemInfo` 类暴露了当前运行时图形后端的所有关键信息：

```csharp
using UnityEngine;

public class GraphicsInfoDump : MonoBehaviour
{
    void Start()
    {
        // 当前使用的图形 API 类型
        Debug.Log(SystemInfo.graphicsDeviceType);    // e.g. GraphicsDeviceType.Direct3D11

        // GPU 设备名称（驱动上报）
        Debug.Log(SystemInfo.graphicsDeviceName);    // e.g. "NVIDIA GeForce RTX 4080"

        // Shader Model 级别（50 = SM 5.0，60 = SM 6.0）
        Debug.Log(SystemInfo.graphicsShaderLevel);

        // 是否支持 DXR / VK_KHR_ray_tracing
        Debug.Log(SystemInfo.supportsRayTracing);

        // 显存大小（MB），部分平台返回 0（共享内存 GPU）
        Debug.Log(SystemInfo.graphicsMemorySize);

        // 是否支持 Compute Shader
        Debug.Log(SystemInfo.supportsComputeShaders);

        // 是否支持 Geometry Shader（Metal 不支持）
        Debug.Log(SystemInfo.supportsGeometryShaders);
    }
}
```

在需要按 API 走不同代码路径时，直接判断 `GraphicsDeviceType` 枚举：

```csharp
if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
{
    // Vulkan 专用路径，例如开启 subpass
}
```

---

## Player Settings 里的 API 顺序

路径：**Edit → Project Settings → Player → [选择平台] → Other Settings → Rendering → Graphics APIs**

默认开启 **Auto Graphics API**，Unity 自动选择当前平台的最佳 API。手动关闭后，列表里第一个是首选 API，后面的是 fallback（首选不可用时依次尝试）。

几个常用的手动配置场景：

**强制 Vulkan（Android）**

移除 OpenGL ES 3.0，只保留 Vulkan。效果：

- APK 里只打包 Vulkan 版本的 Shader bytecode（SPIR-V），包体更小
- 不再支持 Android 6 以下及某些只有 OpenGL ES 的低端设备
- 需要在 `AndroidManifest.xml` 里加 `<uses-feature android:name="android.hardware.vulkan.version">`，让 Google Play 自动过滤不兼容设备

**PC 加入 Vulkan 作为 DX12 替代**

部分 AMD 老驱动（Radeon RX 500 系列）在 DX12 下有已知 bug，Vulkan 驱动反而更稳定。可以把 Vulkan 列在 DX12 之后作为第二 fallback。

**强制 Direct3D 12（PC）**

适合明确只支持 Windows 10+（1507 以上）且需要多线程渲染优化的项目，但要做老 GPU 的排除名单。

---

## NDC 深度范围与 UNITY_REVERSED_Z

各 API 的 NDC（Normalized Device Coordinates）深度范围不同，这是跨平台写 Shader 时最容易踩的坑之一：

| API | NDC 深度范围 | Unity Reversed-Z |
|-----|------------|-----------------|
| Direct3D 11/12 | [0, 1] | 开（近=1，远=0）|
| Vulkan | [0, 1] | 开 |
| Metal | [0, 1] | 开 |
| OpenGL / OpenGL ES | [-1, 1] | 不开 |

**Reversed-Z** 的作用是改善远距离的深度精度（浮点数在 0 附近精度更高，把远平面映射到 0）。但它意味着深度比较方向反转，在 Shader 里手写 NDC 重建时必须区分：

```hlsl
// 从深度缓冲重建 View Space Position
float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);

#if UNITY_REVERSED_Z
    // DX / Vulkan / Metal：近=1，远=0，需要翻转回线性
    float depth = 1.0 - rawDepth;
#else
    // OpenGL ES：直接用
    float depth = rawDepth;
#endif
```

Unity 已经在 `UnityCG.cginc` 和 `Packages/com.unity.render-pipelines.core` 里封装了大部分这类处理，但自己写 Post-process 或 Compute Shader 时仍然需要手动处理。

---

## Shader 在不同后端的处理

Unity 的 HLSL Shader 在构建时经过多层转译才能运行在不同平台：

```
Unity HLSL (.shader / .hlsl)
    ↓ Unity Shader Compiler
HLSL 中间表示（每个变体 + 每个目标平台单独编译）
    ↓
FXC → DXBC（Direct3D 11，SM 5.0）
DXC → DXIL（Direct3D 12，SM 6.x）
DXC → SPIR-V（Vulkan）
HLSLcc → MSL（Metal）
HLSLcc → GLSL ES（OpenGL ES）
```

关键点：

- **DXC**（DirectXShaderCompiler）支持 SM 6.x，包括 Wave Intrinsics（`WaveActiveSum`、`WaveGetLaneIndex`）、Mesh Shader、Ray Tracing。FXC 不支持 SM 6.x。
- **HLSLcc** 是转译器而非真正的编译器，把 DXBC 反编译再输出 MSL/GLSL ES，转译质量直接影响 iOS/Android 的 Shader 性能。某些情况下转译出的 MSL 有多余的临时变量，需要手动优化原始 HLSL 来规避。
- **SPIR-V** 是 Vulkan 的中间字节码，不依赖平台，可以被不同 GPU 驱动编译为本地机器码。Unity 用 DXC 直接从 HLSL 生成 SPIR-V，质量优于 HLSLcc 路径。

---

## 常见后端切换问题

**DX11 → DX12 出现渲染黑屏**

DX11 的 Compute Shader 任务之间，驱动会隐式插入 UAV Barrier。切到 DX12 后这个隐式同步消失，前一个 Compute Pass 写入的结果还没刷新，后一个 Pass 就开始读，导致读到旧数据。修复方法：在 DX12 后端的相关 CommandBuffer 里显式加 UAV Barrier，或者使用 `ComputeBuffer` 时注意 `ComputeBufferType.IndirectArguments` 的读写顺序。

**Android 切 Vulkan 后崩溃**

Adreno 3xx、Mali T7xx 等老芯片的 Vulkan 驱动质量参差不齐。常见处理方式：

1. 在 `AndroidManifest.xml` 设置 `minSdkVersion 24`（Android 7.0）
2. 用 Unity 的 `Graphics.activeTier` 或 `SystemInfo.graphicsDeviceName` 做运行时检测，对特定机型强制回退 OpenGL ES

**Metal 的 storeAction 问题**

Metal 的 Render Pass 里，Tile Memory（Tile-Based Deferred Rendering）的 Color/Depth 附件如果 storeAction 设置为 `Store`，会把 Tile Memory 数据写回主存，产生额外带宽开销。Unity HDRP 在 iOS 上默认已优化这个设置，但自定义 ScriptableRenderPass 里手动创建 `RenderPassDescriptor` 时要注意显式设置 `dontCare` 或 `resolve`，不要全部用 `store`。

---

## 小结

- Unity 的 GfxDevice 抽象层隔离了所有平台图形 API 差异，上层 C# 和 HLSL 代码不感知
- 各平台默认 API：PC = DX11，macOS/iOS = Metal，Android = Vulkan，Xbox = DX12，主机用专有 API
- `SystemInfo.graphicsDeviceType` 可在运行时查询当前后端，用于条件分支
- DX/Vulkan/Metal 的 NDC 深度 [0,1]，OpenGL ES 是 [-1,1]，Reversed-Z 影响所有手写深度计算
- Unity HLSL 编译路径：DX11 用 FXC，DX12/Vulkan 用 DXC，Metal/OpenGL ES 用 HLSLcc 转译
- 后端切换常见问题：DX12 的隐式 UAV Barrier 消失、Android Vulkan 老驱动 bug、Metal 的 storeAction 带宽
