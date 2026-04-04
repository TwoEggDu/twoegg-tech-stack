---
title: "Apple GPU 深度｜Tile Memory、Memoryless Render Target、Rasterization Order 与 Metal 绑定"
slug: "zero-b-deep-02-apple-gpu"
date: "2026-03-28"
description: "Apple GPU 是当前移动端性能天花板，但也是最难移植的目标：Tile Memory 在 Metal 里有专用 API，Memoryless Render Target 能省掉大量 DRAM 带宽，Rasterization Order 支持复杂透明混合。不理解这些机制，就无法解释为什么同一个场景在 iPhone 和 Android 旗舰上的性能差距如此之大。"
tags:
  - "Apple GPU"
  - "Metal"
  - "iOS"
  - "GPU"
  - "移动端"
series: "零·B 深度补充"
series_id: "zero-b-deep"
series_order: 2
weight: 2310
---

Apple GPU 的性能优势不是来自更快的时钟频率或更多的 Shader 核心——它来自一套专门为移动端约束设计的内存架构，以及与 Metal API 的深度协同。

理解这套机制，是解释"为什么 iPhone 15 Pro 的图形性能能持平桌面显卡"的关键，也是在 iOS 上做针对性优化的前提。

---

## 一、Apple GPU 的架构特点概览

Apple GPU 是完全自研的 IP（Intellectual Property），不授权自第三方：

```
历史沿革：
  A7（2013）：首个 Apple 自研 GPU（基于 PowerVR Series 6 授权，深度定制）
  A9-A11：逐步过渡，减少外部依赖
  A12 Bionic（2018）：Tile-based 架构成熟，Neural Engine 独立，GPU 完全自研
  A14-A17 Pro：每代 GPU 性能提升 20-40%，能效比领先同期 Android 旗舰 2x+
  A18 Pro（2024）：硬件光追，GPU 吞吐进入桌面级区间

与 Mali / Adreno 的根本差异：
  ✅ Tile Memory 的编程接口在 Metal 里完全暴露
  ✅ Memoryless Render Target（彻底消除某些 RT 的 DRAM 流量）
  ✅ Rasterization Order Groups（有序透明混合，无需 CPU 排序）
  ✅ Metal 与硬件协同设计，无翻译层开销
  ✅ SoC 统一内存架构（UMA），CPU 和 GPU 共享同一块 DRAM
  ⚠  完全封闭：没有第三方 GPU 调试工具，只有 Xcode / Instruments
  ⚠  MoltenVK（Vulkan 转 Metal）有翻译开销，无法完全发挥硬件特性
```

---

## 二、Tile Memory：超越普通 TBDR 的片上存储

所有移动 GPU 都用 TBDR（Tile-Based Deferred Rendering），但 Apple GPU 的 Tile Memory 有一个关键区别：**它被 Metal API 完全暴露给开发者**，可以在同一个 Render Pass 里被任意读写，而不仅仅是隐式的 framebuffer 存储。

### 普通 TBDR 的局限

在 Mali 和 Adreno 上，Tile Buffer 的生命周期是：
1. 光栅化阶段：Fragment Shader 写入 Tile Buffer（片上）
2. Render Pass 结束：Tile Buffer 内容 Store 到 DRAM（如果需要被后续 Pass 读取）
3. 下一个 Render Pass：从 DRAM Load 回来

对于 Deferred Shading（GBuffer → Lighting），中间的 GBuffer 必须经过 DRAM：

```
Pass 1（GBuffer）：写 Albedo, Normal, Depth → Store 到 DRAM
Pass 2（Lighting）：从 DRAM Load GBuffer → 计算 Lighting → 写 Color

每像素额外 DRAM 流量（1080p，4-channel GBuffer）：
  Albedo（RGBA8） = 4 bytes/pixel
  Normal（RGB10A2）= 4 bytes/pixel
  Depth（R32F）   = 4 bytes/pixel
  → 合计：12 bytes/pixel × 2,073,600 pixels = ~24 MB per GBuffer set
  → 读写两次（Store + Load）= ~48 MB per frame @ 60fps = ~2.9 GB/s 带宽
```

### Apple Tile Memory 的解决方案

Metal 中可以将 GBuffer 声明为 `imageblock`（Tile Memory 内的数据块），在同一个 Render Pass 内的多个 Sub-pass 之间直接传递，**完全不经过 DRAM**：

```metal
// 在 Metal Shading Language 中声明 Tile Memory GBuffer
struct GBuffer {
    half4 albedoMetallic   [[color(0)]]; // fp16，4 分量
    half4 normalRoughness  [[color(1)]]; // fp16，4 分量
    float depth            [[color(2)]]; // fp32 深度
};

// Geometry Pass Fragment Shader：写入 Tile Memory
fragment GBuffer gBufferPass(VertexOut in [[stage_in]],
                              texture2d<float> albedoTex [[texture(0)]])
{
    GBuffer gBuffer;
    gBuffer.albedoMetallic = half4(albedoTex.sample(s, in.uv));
    gBuffer.normalRoughness = half4(encodeNormal(in.worldNormal), roughness, 0);
    gBuffer.depth = in.depth;
    return gBuffer;  // 写入片上 Tile Memory，不走 DRAM
}

// Lighting Pass Fragment Shader：从 Tile Memory 直接读取
fragment half4 lightingPass(GBuffer gBuffer [[imageblock_data]],
                             constant LightParams& light [[buffer(0)]])
{
    // 从 imageblock 读取延迟仅 ~1 cycle
    half3 albedo = gBuffer.albedoMetallic.rgb;
    float3 normal = decodeNormal(gBuffer.normalRoughness.xyz);
    half roughness = gBuffer.normalRoughness.w;

    return half4(computePBR(albedo, normal, roughness, light), 1.0);
}
```

在 Unity 中：URP 在 iOS 上通过 `FramebufferFetch` 自动利用这个特性（当 Deferred Rendering 路径开启时）。

---

## 三、Memoryless Render Target：彻底消除 DRAM 流量

这是 Apple GPU 最独特、对带宽影响最大的优化之一。

### 什么是 Memoryless

```
普通 Render Target 的内存生命周期：
  分配 DRAM → 每帧渲染写入 → 读取用于后续 Pass → 最终输出 → 释放

Memoryless RT 的内存生命周期：
  只存在于 Tile Buffer（片上）
  帧开始时不从 DRAM 加载（loadAction = DontCare）
  帧结束时不写回 DRAM（storeAction = DontCare）
  → 完全没有 DRAM 分配，0 带宽消耗

适用场景（该 RT 的内容在 Render Pass 结束后不再需要）：
  ① Depth Buffer：用于深度测试，但最终不输出到屏幕
  ② MSAA Color Buffer：多重采样缓冲，Resolve 到低分辨率后原始 MSAA 数据废弃
  ③ Deferred GBuffer：在 Tile Memory 路径下，GBuffer 不需要跨 Pass 存储
  ④ Shadow Map 临时缓冲：某些技术中间需要的临时深度缓冲
```

### 带宽节省的量级

```
在 iPhone 15 Pro（2796×1290 分辨率）下：

  Depth Buffer（32-bit）：
  2796 × 1290 × 4 bytes = ~14.4 MB/frame
  不用 Memoryless：每帧 14.4 MB 写入 + 14.4 MB 读取 = 28.8 MB DRAM 流量
  使用 Memoryless：0 DRAM 流量

  MSAA 4x Color Buffer（RGBA8）：
  2796 × 1290 × 4 bytes × 4 samples = ~57.7 MB（仅存一帧）
  不用 Memoryless：57.7 MB 写入 + Resolve 读取
  使用 Memoryless：仅 Resolve 操作，无 DRAM 写入

  合计每帧节省：~70-100 MB
  @ 60fps = ~4.2-6 GB/s 带宽节省
  约占 Apple A17 Pro 总内存带宽的 15-25%
```

### 在 Unity 中的配置

Unity URP 在 iOS 上默认使用 Memoryless Depth（当 depth 不需要跨 Pass 传递时）：

```csharp
// URP 的 Depth 设置（Project Settings → URP Asset）
// "Depth Texture" 设置为 "After Opaques"（仅当 transparent/post-process 需要时）
// 如果游戏没有用到 Depth Texture（没有 soft particle，没有 depth-based fog）：
// 完全关闭 Depth Texture → Depth Buffer 自动成为 Memoryless

// 验证（在 Xcode GPU Frame Capture 里）：
// Render Pass 的 Depth Attachment → Load Action = DontCare，Store Action = DontCare
// 这就是 Memoryless 生效的标志
```

---

## 四、Rasterization Order Groups：无需 CPU 排序的透明混合

### 传统透明渲染的 CPU 瓶颈

经典的 Alpha Blending 要求 **Back-to-Front 渲染**（远处先画，近处后画），这依赖 CPU 对透明对象按深度排序：

```
传统流程：
  CPU：对 N 个透明对象按深度排序 → O(N log N)
  GPU：按排好的顺序逐个提交 Draw Call

  问题：
  CPU 排序开销随场景复杂度增加
  排序结果在每帧之间变化，无法有效批处理（每帧 Sort 后 Draw Call 顺序不同）
  粒子系统尤其明显：1000 个粒子 → 每帧 1000 个带顺序的 Draw Call
```

### Rasterization Order Groups（ROG）

Apple GPU 的 ROG 允许：**同一个像素上的 Fragment 操作按 Draw Call 提交顺序串行执行，即使 GPU 在并行光栅化多个三角形**。

```metal
// Metal Shading Language 中的 ROG 声明
fragment half4 transparentFragment(
    VertexOut in [[stage_in]],
    // 声明这个颜色附件为 ROG 0 组，同组操作按顺序执行
    half4 currentColor [[color(0), raster_order_group(0)]],
    texture2d<half, access::read_write> blendBuffer [[texture(0), raster_order_group(0)]])
{
    half4 newColor = computeColor(in);
    // GPU 保证：对于同一个像素，下一个 Fragment 会在此之后执行
    return blend(currentColor, newColor);
}
```

**实际效果**：CPU 可以任意顺序提交透明对象的 Draw Call，GPU 内部自动保证正确的混合顺序。

**在 Unity 中的支持**：
- Unity 的 Custom Render Pass 可以通过 Metal-specific RenderPass 利用 ROG
- 标准 URP/HDRP 的透明渲染还是使用 CPU 排序
- 完整利用需要 C++ / ObjectiveC 的 Native Rendering Plugin

---

## 五、Apple GPU 的 Shader 执行模型

### SIMD-group：Apple 的 Warp 等价物

```
Metal 术语：
  Thread     = 单个 Fragment / Vertex / Compute 调用（等同于 GLSL 的 invocation）
  Threadgroup = 等同于 GLSL Compute 的 Workgroup
  SIMD-group = 同时执行的 32 个线程（Apple GPU 的 Warp）

  Apple GPU 的 SIMD-group size = 32
  与 NVIDIA 一致（Mali Valhall 是 16，是 Apple 的一半）

  含义：
  Compute Shader 的 Threadgroup size 应该是 32 的倍数
  否则最后一个 SIMD-group 有空线程 → 浪费 ALU 利用率
```

### Apple GPU 的 fp16 支持

与 Mali Valhall 类似，Apple GPU 对 fp16 有专用 ALU 路径：

```metal
// Metal 中的 half（fp16）类型
half3 color = half3(albedo);   // fp16，ALU 2x 吞吐
float3 worldPos = float3(pos); // fp32，用于精度敏感计算

// Apple GPU 上 fp16 的特点：
//   最大值：±65504（比世界坐标小，慎用）
//   精度：~3位有效小数（在 0~1 范围内约 0.001 精度）
//   ALU 吞吐：fp16 是 fp32 的 2x（SIMD pair packing）
```

---

## 六、Metal vs Vulkan/MoltenVK 的性能差距

### MoltenVK 的翻译开销

许多跨平台游戏在 iOS 上使用 Vulkan，通过 MoltenVK 翻译到 Metal：

```
Vulkan API 调用路径（MoltenVK）：
  应用层 Vulkan 调用
      ↓
  MoltenVK 翻译层（C++ 状态机，模拟 Vulkan 资源模型）
      ↓
  Metal API 调用
      ↓
  Apple GPU 硬件

翻译开销的来源：
  状态追踪：MoltenVK 需要维护完整的 Vulkan 状态来决定如何映射到 Metal
  Barrier 转换：Vulkan 的细粒度 Image Layout 转换 → Metal 的 Blit 命令
  Descriptor Set 映射：Vulkan 的 descriptor 模型和 Metal 的 argument buffer 有概念差异
```

### 关键特性损失

```
通过 MoltenVK 无法使用的 Apple 专有优化：
  ❌ Tile Memory（imageblock_data）→ MoltenVK 无法映射 Vulkan 的等价概念
  ❌ Memoryless RT → MoltenVK 部分支持，但行为依赖 Metal Compiler 推断
  ❌ Rasterization Order Groups → Vulkan 没有等价扩展（ROV 在 iOS 不支持）
  ❌ Tile Shader（Metal-specific）

  实际性能差距（复杂场景）：
  Metal 原生 vs MoltenVK：约 10-30% 性能差距
  主要来源：Tile Memory / Memoryless 无法利用 → DRAM 带宽增加 20-40%
```

### Unity 的 Metal 利用状况

```
Unity URP 在 iOS 上的 Metal 利用（2024 年）：
  ✅ Framebuffer Fetch（等同于 Tile Memory 读取，用于 Deferred Shading）
  ✅ Memoryless Depth（当 Depth Texture 未启用时）
  ✅ Memoryless MSAA（默认启用）
  ⚠  Tile Shader（仅通过 Native Rendering Plugin 支持）
  ⚠  Rasterization Order Groups（标准 API 不支持）

  Unity 对 Metal 的支持逐年改善，2022 LTS 之后 Framebuffer Fetch 已经稳定
```

---

## 七、Xcode Instruments 调试 Apple GPU

Apple 不允许第三方 GPU 调试工具，所有 GPU 分析必须通过 Xcode。

### Metal System Trace

```
用途：
  CPU-GPU 同步分析
  找出 CPU 等待 GPU 或 GPU 等待 CPU 的情况
  Command Buffer 提交延迟分析

关键视图：
  GPU Timeline：各个 Render Pass 的实际 GPU 时间
  CPU Timeline：CPU 提交 Draw Call 的时间轴
  对齐分析：CPU 提交快但 GPU 排队慢 → GPU 是瓶颈
             GPU 完成快但 CPU 提交慢 → CPU 是瓶颈
```

### GPU Frame Capture

```
用途：
  Draw Call 级别的 GPU 时间
  每个 Render Pass 的带宽（Tile Load/Store vs DRAM Read/Write）
  Shader 热点分析

关键指标（带宽分析）：
  Tile Memory Bandwidth（片上）→ 这部分不消耗 DRAM
  DRAM Read Bandwidth → 外部内存读取
  DRAM Write Bandwidth → 外部内存写入

  理想状态：DRAM Bandwidth << Tile Bandwidth
  问题标志：DRAM Write 异常高 → 检查是否有 RT 未使用 Memoryless

连接方式：
  Xcode → Debug → Capture GPU Frame（游戏运行时）
  或在 Build Settings 里设置 GPU Frame Capture = Metal（允许运行时抓帧）
```

### Shader Profiler

```
用途：
  每个 Shader 的 ALU 利用率、内存等待时间
  找出 ALU-bound 还是 Memory-bound 的 Shader

分析结果示例：
  Fragment Shader "StandardLit_FRAG"
    ALU utilization:      78%    ← 较高，ALU 是瓶颈
    Memory wait:          22%
    fp16 instruction ratio: 61%  ← 可以继续提升

  Fragment Shader "Terrain_FRAG"
    ALU utilization:      31%
    Memory wait:          69%    ← 内存等待是主要开销，优化纹理采样
    fp16 instruction ratio: 45%
```

---

## 八、Apple GPU 优化清单

| 优化项 | 操作 | 预期收益 |
|------|------|---------|
| Memoryless Depth | 关闭不必要的 Depth Texture | 每帧节省 ~14-28 MB DRAM |
| Memoryless MSAA | 确保 MSAA Buffer 不被 Store | 每帧节省 ~50-100 MB DRAM |
| Framebuffer Fetch | URP Deferred 路径（自动） | GBuffer 带宽 -60~80% |
| fp16（half）使用 | 颜色、法线、UV 用 half 类型 | ALU 吞吐 2x |
| SIMD-group 对齐 | Compute Shader threadgroup = 32 倍数 | ALU 利用率提升 |
| 减少 Render Pass 数量 | 合并相邻 Pass，减少 Load/Store | 带宽节省 |
| Xcode 验证 | GPU Frame Capture 确认 DRAM 带宽 | 确认优化是否生效 |
| Metal 原生路径 | 优先 Metal，而不是 Vulkan+MoltenVK | 10-30% 总体性能 |
