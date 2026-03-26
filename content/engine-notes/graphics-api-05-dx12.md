+++
title = "图形 API 05｜DirectX 12：Windows 平台的显式 API，D3D12 与 DX11 的代差"
slug = "graphics-api-05-dx12"
date = 2026-03-26
description = "DirectX 12 是微软在 2015 年随 Windows 10 发布的低层 API，设计思路与 Vulkan 基本一致：显式内存管理、Command List、Resource Barrier。这篇讲清楚 DX12 的核心概念、与 DX11 的代差，以及 PC 平台游戏引擎的 DX12 后端要处理什么。"
weight = 740
[taxonomies]
tags = ["图形API", "DirectX 12", "DX12", "D3D12", "Windows", "Command List", "Resource Barrier"]
[extra]
series = "图形 API 基础"
+++

## 为什么需要 DX12

DX11 在 2009 年发布时已经是当时最好的图形 API 之一，但它的设计假设是"让驱动替你管理一切"。驱动负责资源状态追踪、同步、内存分配，开发者只需要按照文档绑定资源、提交 Draw Call。这套方案降低了上手门槛，但代价是驱动层藏着大量隐式开销：状态比对、Hazard 检测、内存别名处理，这些逻辑在驱动里几乎是黑盒。

随着 GPU 计算能力指数级增长，CPU 向 GPU 提交命令的效率成了瓶颈。DX11 的 ImmediateContext 是单线程的，多核 CPU 无法并行录制命令。同时驱动的隐式同步在高并发渲染场景（如 GPU-driven pipeline）下频繁成为 stall 点。

DX12（代号 D3D12）和 Vulkan 几乎同期发布（2015 年），目标一致：**把隐式交给开发者，换取可预测的性能**。

---

## DX11 vs DX12 核心代差

| 维度 | DX11 | DX12 |
|------|------|------|
| 内存管理 | 驱动隐式分配 | 显式 Heap（`D3D12_HEAP_TYPE`）|
| 命令录制 | 单线程 `ImmediateContext` | 多线程 `CommandList` + `CommandAllocator` |
| GPU 同步 | 驱动隐式 Fence | 显式 `ID3D12Fence` + `Signal` / `Wait` |
| 资源状态 | 驱动推断 | 显式 Resource Barrier（`ResourceBarrier()`）|
| 资源绑定 | 简单 Slot 绑定 | Descriptor Heap（CBV/SRV/UAV）|
| 根签名 | 无 | Root Signature（对应 Vulkan PipelineLayout）|

这张表的每一行都意味着：DX12 把原本在驱动里完成的工作暴露给应用层。写对了性能更好，写错了行为未定义——通常直接 crash 或花屏。

---

## 核心概念逐一解析

### Command Allocator 与 Command List

`ID3D12CommandAllocator` 是命令的内存池，`ID3D12GraphicsCommandList` 向这个池里录制命令。录制完毕后调用 `Close()`，再提交到 `ID3D12CommandQueue`。

```cpp
// 录制阶段
commandAllocator->Reset();
commandList->Reset(commandAllocator.Get(), pso.Get());

commandList->SetGraphicsRootSignature(rootSignature.Get());
commandList->RSSetViewports(1, &viewport);
commandList->RSSetScissorRects(1, &scissorRect);

// 提交
commandList->Close();
ID3D12CommandList* ppCommandLists[] = { commandList.Get() };
commandQueue->ExecuteCommandLists(_countof(ppCommandLists), ppCommandLists);
```

多个线程可以各自持有独立的 `CommandAllocator` + `CommandList`，并行录制，最终汇总到同一个 `CommandQueue` 提交。这是 DX11 时代做不到的。

### Resource Barrier

Resource Barrier 是 DX12 里最容易踩坑的机制。GPU 内部对同一块内存的读写操作并不天然有序，驱动也不再自动插入同步。开发者必须在资源状态发生变化时手动调用 `ResourceBarrier()`。

最常见的 Transition Barrier：把 Render Target 的 Backbuffer 从 `PRESENT` 状态切到 `RENDER_TARGET`，渲染完再切回 `PRESENT`：

```cpp
// 渲染前：PRESENT → RENDER_TARGET
D3D12_RESOURCE_BARRIER barrierToRT = CD3DX12_RESOURCE_BARRIER::Transition(
    renderTargets[frameIndex].Get(),
    D3D12_RESOURCE_STATE_PRESENT,
    D3D12_RESOURCE_STATE_RENDER_TARGET
);
commandList->ResourceBarrier(1, &barrierToRT);

// ... 渲染命令 ...

// 渲染后：RENDER_TARGET → PRESENT
D3D12_RESOURCE_BARRIER barrierToPresent = CD3DX12_RESOURCE_BARRIER::Transition(
    renderTargets[frameIndex].Get(),
    D3D12_RESOURCE_STATE_RENDER_TARGET,
    D3D12_RESOURCE_STATE_PRESENT
);
commandList->ResourceBarrier(1, &barrierToPresent);
```

漏写 Barrier 是 DX12 开发中最常见的 bug 来源。Debug Layer（`D3D12_MESSAGE_SEVERITY_ERROR`）会在漏写时输出明确报错，开发阶段建议始终开启。

### Descriptor Heap

DX11 里绑定纹理是直接调用 `PSSetShaderResources(slot, 1, &srv)`，每帧可以随意换。DX12 不再支持这种方式。所有资源视图（CBV/SRV/UAV/Sampler）必须放进 `ID3D12DescriptorHeap`，Shader 通过 Heap 里的 Index 访问资源。

这个设计允许 **Bindless Rendering**：把场景里所有纹理一次性放进一个巨型 Descriptor Heap，Shader 里用一个 uint index 直接寻址，完全消除 CPU 端的资源绑定 overhead。现代 AAA 引擎（Unreal 5、Unity DOTS Renderer）的 GPU-driven pipeline 都依赖这套机制。

### Root Signature

Root Signature 描述 Shader 期望的资源布局：哪些 slot 是 CBV（Constant Buffer View），哪些是 SRV（Shader Resource View），哪些是直接内联的 32-bit Constants（等同于 Vulkan 的 Push Constant）。

```cpp
CD3DX12_ROOT_PARAMETER rootParameters[2];
// slot 0：一个 CBV（b0）
rootParameters[0].InitAsConstantBufferView(0);
// slot 1：一张纹理 SRV（t0）
rootParameters[1].InitAsShaderResourceView(0);
```

Root Signature 需要在录制命令前绑定，且必须与 PSO（Pipeline State Object）匹配，否则验证层直接报错。

---

## GPU 内存堆类型

DX12 把 GPU 内存分成三种堆：

- **DEFAULT**（`D3D12_HEAP_TYPE_DEFAULT`）：GPU 本地显存（VRAM），读写带宽最高，CPU 无法直接访问。Mesh Buffer、Texture 应该放这里。
- **UPLOAD**（`D3D12_HEAP_TYPE_UPLOAD`）：CPU 写入、GPU 读取的共享内存（AGP/PCIe 可见区域）。每帧更新的常量缓冲、顶点上传用这里。
- **READBACK**（`D3D12_HEAP_TYPE_READBACK`）：GPU 写入后 CPU 读取，用于 GPU 截图、Compute Shader 回传数据。

正确的资源上传流程是：CPU 数据先写入 UPLOAD Heap 的临时 Buffer，再用 `CopyBufferRegion` 把数据拷贝到 DEFAULT Heap，最后加一个 Transition Barrier 把资源状态切到 `COPY_DEST` → `SHADER_RESOURCE`。

---

## DX12 Ultimate 与 DirectStorage

**DX12 Ultimate** 是 DX12 的功能超集，要求 GPU 同时支持：

- **Mesh Shader**：替代 Vertex/Geometry Shader，允许 GPU 自主生成图元（用于 Nanite 等 Virtual Geometry 方案）
- **Variable Rate Shading（VRS）**：对画面边缘、高速运动区域降低 Shading Rate，节省 pixel shader 开销
- **Sampler Feedback**：追踪 Shader 实际采样了哪些 mip level，驱动流式纹理加载
- **DirectX Raytracing（DXR）**：硬件光线追踪，NVIDIA RTX 和 AMD RDNA 2+ 支持

**DirectStorage** 是 DX12 的 IO 扩展：绕过 CPU 和系统内存，直接从 NVMe SSD 把资源传输到 GPU 显存（GPU Decompression 用 GDeflate 格式在 GPU 端解压）。实测可把资源加载时间缩短 2~3 倍。Unity 在 2023 LTS 版本开始逐步引入 DirectStorage 支持。

---

## Unity 的 DX12 后端

在 Unity 里启用 DX12：**Edit → Project Settings → Player → PC → Other Settings → Graphics APIs**，把 Direct3D12 拖到列表首位（或移除 Direct3D11 强制使用 DX12）。

DX12 后端的收益主要体现在：

- **多线程 CommandList 录制**：Unity 的 Graphics Jobs 在 DX12 下效果更明显，CPU Render Thread 开销可降低 20~40%（视场景复杂度）
- **GPU-driven rendering**：URP 和 HDRP 的 GPU Occlusion Culling、Indirect Draw 在 DX12 后端有更完整的支持
- **Compute Shader 无锁提交**：DX12 的异步 Compute Queue 允许 Compute 和 Graphics 并行执行

需要注意的限制：

- 部分老旧 GPU（NVIDIA 700 系以下，AMD GCN 1.0）的 DX12 驱动质量差，出现花屏或 crash 时应回退 DX11
- 某些 Compute Shader 在 DX11 下依赖驱动隐式同步，迁移 DX12 后需要手动加 UAV Barrier
- Debug Layer 只在开发机上开，交付包里不要附带 D3D12 SDK Layers DLL

---

## 小结

- DX12 核心变化：显式内存（Heap）、多线程 CommandList、手动 Resource Barrier、Descriptor Heap 绑定
- Root Signature 对应 Vulkan 的 PipelineLayout，是 Shader 与资源布局的契约
- UPLOAD / DEFAULT / READBACK 三种堆分别对应 CPU 写入、GPU 本地、GPU 回传三种场景
- DX12 Ultimate 在 DX12 基础上加了 Mesh Shader、VRS、DXR 等现代特性
- DirectStorage 把 IO 路径从 CPU 搬到 GPU，资源加载速度显著提升
- Unity DX12 后端在多线程渲染和 GPU-driven 场景下收益明显，老旧 GPU 需做兼容测试
