---
title: "图形 API 04｜Metal：苹果的图形 API，与 Vulkan 设计哲学的异同"
slug: "graphics-api-04-metal"
date: "2026-03-26"
description: "Metal 是 Apple 在 2014 年推出的图形/计算 API，比 Vulkan 早两年。它同样是低开销 API，但在设计上更贴近苹果平台的硬件特性（Tile Memory、Memoryless RT）。这篇对比 Metal 与 Vulkan 的设计差异，以及 Unity Metal 后端的关键参数。"
weight: 730
tags:
  - "图形API"
  - "Metal"
  - "Apple"
  - "iOS"
  - "macOS"
  - "Tile Memory"
  - "Vulkan对比"
series: "图形 API 基础"
---
## 诞生背景

2014 年 WWDC，Apple 在发布 iOS 8 时同步推出了 Metal。当时 OpenGL ES 2.0/3.0 是 iOS 上唯一的图形 API，但 Apple 的 GPU 团队（收购 Imagination Technologies 授权，后来自研 Apple Silicon GPU）已经在 A7 芯片里为 Metal 设计了专用硬件支持。

OpenGL ES 的问题和 PC 端一样：全局状态机、驱动隐式管理、CPU 单线程瓶颈。但在移动端更突出，因为移动 CPU 频率更低、GPU 是 Tile-Based 架构，OpenGL ES 驱动对 Tile Memory 的管理策略不透明，应用程序没有办法告诉驱动"这个 Render Target 用完就扔，不用写回 DRAM"。

Metal 解决的就是这类控制粒度的问题。Vulkan 在 2016 年发布时，Metal 已经有两年成熟度了，两者在设计上有大量共同点（都受 AMD Mantle 影响），但在细节层面走了不同的路。

## 核心对象对比

Metal 和 Vulkan 解决同一个问题，但命名体系不同。横向对照：

| Metal | Vulkan | 作用 |
|-------|--------|------|
| `MTLDevice` | `VkDevice` | GPU 逻辑设备，所有资源的创建入口 |
| `MTLCommandQueue` | `VkQueue` | 命令提交队列 |
| `MTLCommandBuffer` | `VkCommandBuffer` | 一帧命令的容器 |
| `MTLRenderCommandEncoder` | `VkRenderPass` 录制段 | 一次 RenderPass 的命令录制 |
| `MTLRenderPassDescriptor` | `VkRenderPassBeginInfo` | RT Attachment 和 Load/Store 操作 |
| `MTLComputeCommandEncoder` | Compute Pass 录制 | Compute Shader Dispatch |
| `MTLBlitCommandEncoder` | Transfer 命令 | 纹理/Buffer 拷贝、Mipmap 生成 |
| `MTLHeap` | `VkDeviceMemory` | 手动内存堆（非强制使用）|
| `MTLRenderPipelineState` | `VkPipeline` | 编译完成的完整管线状态对象 |

Metal 的 `MTLCommandBuffer` 用 Encoder 模式组织命令：一个 `MTLCommandBuffer` 里可以串联多个 Encoder，每个 Encoder 代表一种操作类型（Render / Compute / Blit）。Encoder 是互斥的——结束上一个 Encoder 才能开始下一个。

## 一个 Metal 渲染 Pass 的基本结构

```swift
// 获取当前帧的 Command Buffer
let commandBuffer = commandQueue.makeCommandBuffer()!

// 设置 RenderPassDescriptor，描述 RT 的 Load/Store 行为
let rpd = MTLRenderPassDescriptor()
rpd.colorAttachments[0].texture = currentDrawable.texture
rpd.colorAttachments[0].loadAction = .clear          // 渲染开始时清空
rpd.colorAttachments[0].clearColor = MTLClearColor(red: 0, green: 0, blue: 0, alpha: 1)
rpd.colorAttachments[0].storeAction = .store         // 渲染结束后写回

// 创建 RenderCommandEncoder，开始录制渲染命令
let encoder = commandBuffer.makeRenderCommandEncoder(descriptor: rpd)!
encoder.setRenderPipelineState(pipelineState)
encoder.setVertexBuffer(vertexBuffer, offset: 0, index: 0)
encoder.drawPrimitives(type: .triangle, vertexStart: 0, vertexCount: 3)
encoder.endEncoding()

// 提交并 Present
commandBuffer.present(currentDrawable)
commandBuffer.commit()
```

结构和 Vulkan 高度相似：创建 CommandBuffer → 描述 RenderPass Attachment → 录制 Draw → 提交。

## Metal 相对 Vulkan 的简化点

Metal 在显式控制的同时，保留了一些驱动侧的自动推断，让代码量比 Vulkan 少约 30%~50%。

**不需要显式 Pipeline Barrier**

Vulkan 要求在两个 Pass 之间如果有纹理从"写"切换到"读"，必须手动插入 `VkImageMemoryBarrier`，并且准确指定 `srcStageMask` 和 `dstStageMask`。

Metal 在 Encoder 边界隐式推断资源 hazard。两个 `MTLRenderCommandEncoder` 之间，Metal 驱动会自动检测是否存在 Read-After-Write 依赖，并插入必要的等待。代价是灵活性稍低，但大幅降低了开发出错的概率。

**Resource Residency 更简单**

Vulkan 中要确保 GPU 访问一个资源之前，该资源所在的 `VkDeviceMemory` 必须处于正确的可访问状态（`VkMemoryBarrier` + Flush）。Metal 的 `useResource:usage:` 声明更简洁：

```objc
// 声明这个 Pass 里会读取 texture
[encoder useResource:texture usage:MTLResourceUsageRead];
```

**MTLHeap 非强制**

Vulkan 中每个 Buffer 和 Image 都必须绑定 `VkDeviceMemory`，手动管理内存堆是不可绕过的步骤。Metal 中你可以直接用 `[device newBufferWithLength:options:]` 创建 Buffer，驱动自动分配内存；`MTLHeap` 是性能优化手段（Sub-allocation），不是必须路径。

## Tile Memory 与 Memoryless Render Texture

这是 Metal 在 Apple GPU（TBDR 架构）上的独特能力，也是移动端图形优化的核心话题。

Apple GPU 每次渲染时，把屏幕分成若干 Tile（通常 32×32 像素），每个 Tile 的 Color Buffer、Depth Buffer、Stencil Buffer 都保存在片上 Tile Memory 里。渲染该 Tile 的所有三角形时，读写全在片上完成——速度是访问 DRAM 的数十倍，功耗也低得多。

渲染完成后，根据 `storeAction` 决定是否把 Tile Memory 的内容写回 DRAM：
- `.store`：写回主存，下一帧或其他 Pass 可以继续用
- `.dontCare`：不写回，数据直接丢弃

对于延迟渲染的 G-Buffer（存 Normal、Albedo、Roughness 等），在同一帧里用完就丢掉，根本不需要写回 DRAM。设置 `.dontCare` 可以省掉一次完整的 DRAM 写带宽。

Metal 更进一步，提供了 **Memoryless Render Texture**：这类纹理根本没有 DRAM 分配，只存在于 Tile Memory 里：

```objc
MTLTextureDescriptor *desc = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat:MTLPixelFormatRGBA8Unorm
                                                                                 width:width
                                                                                height:height
                                                                             mipmapped:NO];
desc.storageMode = MTLStorageModeMemoryless; // 只在 Tile Memory 里存在
desc.usage = MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead;
id<MTLTexture> gbufferNormal = [device newTextureWithDescriptor:desc];
```

在 Unity 里，等价操作是：

```csharp
RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height);
desc.memoryless = RenderTextureMemoryless.Color | RenderTextureMemoryless.Depth;
RenderTexture rt = new RenderTexture(desc);
```

URP 在 iOS 上默认对 Depth Buffer 使用 Memoryless，是开箱即得的优化。

## Metal Shading Language（MSL）

MSL 基于 C++14，是 Metal 的 Shader 语言。与 HLSL/GLSL 的主要差异：

```metal
// MSL：用属性标记 Shader 入口和绑定位置
struct VertexOut {
    float4 position [[position]];   // [[position]] = SV_Position
    float2 uv       [[user(uv)]];
};

vertex VertexOut vertexShader(
    uint vertexID [[vertex_id]],                        // 顶点索引
    constant Uniforms &uniforms [[buffer(0)]],          // Buffer 绑定槽 0
    const device Vertex *vertices [[buffer(1)]]         // Buffer 绑定槽 1
) {
    VertexOut out;
    out.position = uniforms.mvp * float4(vertices[vertexID].position, 1.0);
    out.uv = vertices[vertexID].uv;
    return out;
}

fragment float4 fragmentShader(
    VertexOut in [[stage_in]],
    texture2d<float> albedo [[texture(0)]],             // 纹理绑定槽 0
    sampler smp [[sampler(0)]]                          // Sampler 绑定槽 0
) {
    return albedo.sample(smp, in.uv);
}
```

`[[vertex]]`/`[[fragment]]` 标记入口函数，`[[buffer(n)]]`/`[[texture(n)]]` 直接在函数参数上声明绑定位置，不需要 GLSL 的 `layout(binding=n)` 或 HLSL 的 `: register(tn)`。

Unity 的 HLSL Shader 通过 DXC（DirectX Shader Compiler）或 HLSLcc 编译为 SPIR-V，再经过 SPIRV-Cross 转译为 MSL，最终在 Metal 上运行。

## Metal Performance Shaders（MPS）

MPS 是 Apple 提供的高性能 GPU 计算库，内置了大量经过手工优化的 GPU 内核：

- 卷积（用于 CNN 推断）
- FFT / 逆 FFT
- 矩阵乘法（GEMM）
- 图像滤波（高斯模糊、直方图均衡化）
- BVH 构建和光线追踪

Unity 的 Sentis（原 Barracuda）神经网络推断库在 iOS 上的后端就调用 MPS，利用 Apple GPU 的矩阵运算单元做推断加速，而不是用通用 Compute Shader 手写卷积。

## Unity Metal 后端设置

**设置图形 API 优先级**

iOS 平台只有 Metal 一个选项。macOS 上需要注意：Unity Editor 本身运行在 Metal，但 Standalone Player 可以选择 Metal 或 OpenGL Core。

```
Player Settings → Other Settings → Auto Graphics API
关闭后可手动排序：
  macOS: Metal（优先）/ OpenGLCore（回退）
  iOS:   Metal（唯一选项）
```

**MTLParallelRenderCommandEncoder**

Metal 支持并行录制同一个 RenderPass 的命令，通过 `MTLParallelRenderCommandEncoder` 把一个 RenderPass 的 Draw Call 分发给多个线程：

```objc
id<MTLParallelRenderCommandEncoder> parallelEncoder =
    [commandBuffer parallelRenderCommandEncoderWithDescriptor:rpd];

// 线程 1
id<MTLRenderCommandEncoder> enc1 = [parallelEncoder renderCommandEncoder];
// 录制前半部分 Draw Call...
[enc1 endEncoding];

// 线程 2
id<MTLRenderCommandEncoder> enc2 = [parallelEncoder renderCommandEncoder];
// 录制后半部分 Draw Call...
[enc2 endEncoding];

[parallelEncoder endEncoding];
```

Unity URP 在多相机场景下利用这个机制，把不同相机的 Shadow Pass 录制并行化，减少主线程提交开销。

## 小结

- Metal 比 Vulkan 早两年，是低开销图形 API 的先行者，专为 Apple TBDR GPU 设计
- 核心对象层次（`MTLDevice` → `MTLCommandQueue` → `MTLCommandBuffer` → Encoder）与 Vulkan 一一对应，但 Metal 在 Encoder 边界隐式推断资源 hazard，省去了手动 Pipeline Barrier
- Tile Memory 和 Memoryless Render Texture 是 Apple GPU 的杀手级特性：G-Buffer 等中间 RT 设置 `.dontCare` / `memoryless`，省掉 DRAM 回写带宽
- MSL 基于 C++14，用属性语法（`[[texture(0)]]`）声明绑定，Unity HLSL 通过 SPIRV-Cross 转译到 MSL
- MPS 提供内置高性能 GPU 算子，Unity Sentis 在 iOS 上的推断加速依赖 MPS
