---
title: "图形 API 03｜Vulkan：显式控制、Command Buffer、RenderPass 与 Framebuffer"
slug: "graphics-api-03-vulkan"
date: "2026-03-26"
description: "Vulkan 把之前由驱动隐式完成的事情全都交给应用程序：内存分配、命令录制、同步屏障、RenderPass 结构。代码量大了 5~10 倍，但 CPU 开销和可预测性大幅提升。这篇讲清楚 Vulkan 的核心概念和它与 OpenGL 的根本差异。"
weight: 720
tags:
  - "图形API"
  - "Vulkan"
  - "Command Buffer"
  - "RenderPass"
  - "同步"
  - "显式控制"
series: "图形 API 基础"
---
## Everything Explicit

Vulkan 的核心设计原则可以用两个词概括：显式控制（Explicit Control）。

在 OpenGL 里，你调用 `glDraw`，驱动在背后帮你完成：内存屏障、Shader 状态合并、命令队列管理、资源生命周期跟踪。这些工作有开销，而且时机不由你控制。

Vulkan 的立场是：驱动只做硬件必须做的事，其余的全部交给应用程序。你要告诉 GPU：
- 这块内存什么时候可以读、什么时候可以写
- 这次 RenderPass 之后 Render Target 的内容要保留还是丢弃
- GPU 的哪个队列执行渲染命令、哪个执行 Compute、哪个执行数据传输

代价是代码量。用 Vulkan 画一个三角形，不计注释大约需要 800 行 C++ 代码。但在这 800 行里，没有任何一行是驱动替你做了你不知道的事。

## 核心对象层次

Vulkan 初始化按照固定的层次结构创建对象：

```
VkInstance
    └── VkPhysicalDevice（枚举系统里的所有 GPU）
            └── VkDevice（逻辑设备，应用程序操作 GPU 的入口）
                    ├── VkQueue（命令提交队列）
                    ├── VkCommandPool → VkCommandBuffer（命令录制）
                    ├── VkRenderPass（描述渲染流程结构）
                    ├── VkFramebuffer（绑定 RenderPass 的实际 RT）
                    ├── VkPipeline（完整的 Graphics/Compute 管线状态）
                    └── VkDeviceMemory（GPU 内存堆）
```

**VkInstance**：代表整个 Vulkan 运行时的实例。创建时选择需要启用的 Validation Layer 和 Instance Extension（如 `VK_KHR_surface` 用于窗口系统对接）。

**VkPhysicalDevice / VkDevice**：`vkEnumeratePhysicalDevices` 列出所有 GPU，你选择一个创建逻辑设备 `VkDevice`。逻辑设备是后续所有操作的根对象。创建 VkDevice 时声明需要哪些 Device Extension（如 `VK_KHR_swapchain`）和哪些 Queue Family。

## Queue Family 和多队列并行

这是 Vulkan 多线程性能提升的关键。现代 GPU 硬件上有多种队列：

- **Graphics Queue**：处理图形渲染命令（Draw Call、RenderPass）
- **Compute Queue**：处理 Compute Shader Dispatch，可与 Graphics 并行执行
- **Transfer Queue**：专用 DMA 传输，上传纹理/Buffer 不占用 Graphics 时间

```cpp
// 查询 Queue Family，找到支持 Graphics 的队列族
uint32_t queueFamilyCount = 0;
vkGetPhysicalDeviceQueueFamilyProperties(physDevice, &queueFamilyCount, nullptr);
std::vector<VkQueueFamilyProperties> families(queueFamilyCount);
vkGetPhysicalDeviceQueueFamilyProperties(physDevice, &queueFamilyCount, families.data());

for (uint32_t i = 0; i < queueFamilyCount; i++) {
    if (families[i].queueFlags & VK_QUEUE_GRAPHICS_BIT) {
        graphicsQueueFamilyIndex = i;
    }
    if (families[i].queueFlags & VK_QUEUE_TRANSFER_BIT &&
        !(families[i].queueFlags & VK_QUEUE_GRAPHICS_BIT)) {
        dedicatedTransferQueueIndex = i; // 专用传输队列
    }
}
```

把纹理流式加载提交到 Transfer Queue，同时 Graphics Queue 继续渲染，两者真正并行——OpenGL 的单线程 Context 做不到这一点。

## Command Buffer：多线程录制的基础

`VkCommandBuffer` 是所有 GPU 命令的容器。录制和提交是分离的两个步骤：

```cpp
// 1. 开始录制
VkCommandBufferBeginInfo beginInfo{};
beginInfo.sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;
beginInfo.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
vkBeginCommandBuffer(cmdBuffer, &beginInfo);

// 2. 录制 RenderPass
VkRenderPassBeginInfo rpInfo{};
rpInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
rpInfo.renderPass = renderPass;
rpInfo.framebuffer = framebuffer;
rpInfo.renderArea = {{0, 0}, {width, height}};
rpInfo.clearValueCount = 1;
rpInfo.pClearValues = &clearColor;
vkCmdBeginRenderPass(cmdBuffer, &rpInfo, VK_SUBPASS_CONTENTS_INLINE);

vkCmdBindPipeline(cmdBuffer, VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline);
vkCmdBindVertexBuffers(cmdBuffer, 0, 1, &vertexBuffer, offsets);
vkCmdDrawIndexed(cmdBuffer, indexCount, 1, 0, 0, 0);

vkCmdEndRenderPass(cmdBuffer);
vkEndCommandBuffer(cmdBuffer);

// 3. 提交到队列
VkSubmitInfo submitInfo{};
submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
submitInfo.commandBufferCount = 1;
submitInfo.pCommandBuffers = &cmdBuffer;
vkQueueSubmit(graphicsQueue, 1, &submitInfo, fence);
```

多线程场景：每个线程从 `VkCommandPool` 分配自己的 `VkCommandBuffer`，并行录制，最后在主线程合并成一个 Submit 批次。这是 Vulkan CPU 开销降低的核心来源。

## RenderPass 和 Framebuffer

`VkRenderPass` 不是执行渲染，而是**描述一次渲染的结构**：有哪些 Attachment（颜色/深度/Stencil），每个 Attachment 在渲染开始时如何初始化（loadOp），渲染结束后如何处理（storeOp）。

```cpp
VkAttachmentDescription colorAttachment{};
colorAttachment.format = swapchainFormat;
colorAttachment.samples = VK_SAMPLE_COUNT_1_BIT;
colorAttachment.loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;      // 开始时清空
colorAttachment.storeOp = VK_ATTACHMENT_STORE_OP_STORE;    // 结束后写回
colorAttachment.stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
colorAttachment.stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
colorAttachment.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
colorAttachment.finalLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
```

`loadOp` 和 `storeOp` 对移动端 Tile-Based GPU（Mali、Adreno、Apple）性能影响极大：
- `LOAD_OP_DONT_CARE` 代替 `LOAD_OP_LOAD`：GPU 不需要从主存（DRAM）把上一帧数据加载到 Tile Memory，省去一次 DRAM 读取
- `STORE_OP_DONT_CARE` 代替 `STORE_OP_STORE`：渲染结果不写回 DRAM，适用于 G-Buffer 这类只在当帧使用的中间 RT

`VkFramebuffer` 是 RenderPass 的实例化：把具体的 `VkImageView`（纹理视图）绑定到 RenderPass 描述的 Attachment 槽位上。

## 同步原语

Vulkan 的同步是显式的，开发者必须主动声明依赖关系。

**VkSemaphore（Queue 间同步）**：

```cpp
// 渲染完成后再 Present（Semaphore 用于 GPU-GPU 同步）
VkSubmitInfo submitInfo{};
submitInfo.waitSemaphoreCount = 1;
submitInfo.pWaitSemaphores = &imageAvailableSemaphore;   // 等待 Swapchain 图像可用
submitInfo.signalSemaphoreCount = 1;
submitInfo.pSignalSemaphores = &renderFinishedSemaphore; // 渲染完成后 signal
```

**VkFence（CPU-GPU 同步）**：CPU 调用 `vkWaitForFences` 阻塞，直到 GPU 完成该帧的提交。用于控制 Flight Frame 数量（通常 2~3 帧）。

**VkPipelineBarrier（同一 Queue 内资源状态转换）**：

```cpp
// 把纹理从 "传输写入目标" 转换为 "Shader 只读"
VkImageMemoryBarrier barrier{};
barrier.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;
barrier.oldLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
barrier.newLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
barrier.srcAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
barrier.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
vkCmdPipelineBarrier(
    cmdBuffer,
    VK_PIPELINE_STAGE_TRANSFER_BIT,         // 等待 Transfer 阶段完成写入
    VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,  // 阻塞 Fragment Shader 开始读取
    0, 0, nullptr, 0, nullptr, 1, &barrier
);
```

如果没有这个 Barrier，Fragment Shader 可能读到还没写完的纹理数据（Write-After-Read hazard）。

## Memory Allocation

Vulkan 不做隐式内存管理。每个 Buffer 和 Image 创建后是没有内存的，必须手动分配 `VkDeviceMemory` 并绑定：

```cpp
vkGetBufferMemoryRequirements(device, buffer, &memRequirements);
// 找到满足要求的 Memory Type（Device Local / Host Visible 等）
VkMemoryAllocateInfo allocInfo{};
allocInfo.allocationSize = memRequirements.size;
allocInfo.memoryTypeIndex = findMemoryType(memRequirements.memoryTypeBits,
    VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);
vkAllocateMemory(device, &allocInfo, nullptr, &bufferMemory);
vkBindBufferMemory(device, buffer, bufferMemory, 0);
```

实际项目中几乎不直接调用 `vkAllocateMemory`（每次分配都是系统调用，GPU 驱动限制分配次数上限约 4096 次）。GPUOpen 的 VMA（Vulkan Memory Allocator）库是事实上的标准，内部做 Memory Pool 和 Sub-allocation。

## 在 Unity 里的体现

Unity 选择 Vulkan 作为 Android 7.0+ 的默认图形后端。几个直接对应点：

**CommandBuffer 对应 VkCommandBuffer**：Unity C# 的 `CommandBuffer` API 录制的 `DrawMesh`、`Blit`、`SetRenderTarget` 等指令，在渲染线程被翻译为 `VkCommandBuffer` 录制调用，最终批量提交。

**RenderGraph 对应 RenderPass 思路**：URP 14+ 的 Render Graph（`RenderGraph.AddRasterRenderPass`）强制你声明每个 Pass 的输入输出资源，这和 `VkRenderPassCreateInfo` 中声明 Attachment 依赖是同一套思路——提前知道依赖关系，才能做 Barrier 自动推导和 Pass 合并（Pass Merging）。

**Memoryless RT 对应 STORE_OP_DONT_CARE**：在 Unity 里对中间 RT 设置 `RenderTexture.memorylessMode = RenderTextureMemoryless.Color`，底层会映射到 Vulkan 的 `storeOp = DONT_CARE`，在 Android Mali/Adreno 设备上减少 DRAM 带宽消耗。

## 小结

- Vulkan 的设计哲学是 Everything Explicit：内存、同步、命令录制全部由应用程序控制
- `VkCommandBuffer` 分离录制和提交，多线程并行录制是 CPU 性能提升的核心
- `VkRenderPass` 的 `loadOp`/`storeOp` 直接控制 Tile-Based GPU 的 DRAM 带宽，是移动端优化的关键参数
- `VkPipelineBarrier` 替代了 OpenGL 的隐式同步，开发者需要主动声明资源状态转换
- 实际项目中 Memory Allocation 用 VMA，Unity 的 RenderGraph 和 Memoryless RT 是对这套机制的高层封装
