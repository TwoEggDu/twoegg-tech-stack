---
title: "Shader 进阶技法 19｜GPU Scene 与 Per-Instance Data：现代引擎如何用 GPU 管理场景数据"
slug: "shader-advanced-19-gpu-scene"
date: "2026-03-28"
description: "GPU Scene 是现代引擎把场景数据全量上传 GPU、让着色器直接读取 per-instance 信息的核心机制。理解它如何替代 CPU 逐对象提交，以及 Unreal 和 Unity 各自的实现方式。"
tags:
  - "Shader"
  - "HLSL"
  - "进阶"
  - "GPU Scene"
  - "Per-Instance Data"
  - "GPU Driven"
  - "性能优化"
series: "Shader 手写技法"
weight: 4470
---

GPU Driven Rendering 解决了"谁来发 DrawCall"的问题——把 Culling 和 DrawCall 生成搬到 GPU。但还有一个问题没解决：**DrawCall 发出后，着色器怎么知道这个实例的材质参数、变换矩阵、自定义数据是什么？**

传统答案是 CPU 在每次 DrawCall 前用 `SetFloat` / `SetMatrix` 推送 constant buffer。GPU Driven 环境下这条路走不通——DrawCall 由 GPU 自己生成，CPU 根本不知道会画哪些实例，也无从提前推送。

GPU Scene 是解决这个问题的答案：**把所有实例的数据一次性上传到 GPU 侧的大缓冲区，着色器在运行时自己根据 instance ID 去读。**

---

## 传统 Per-Instance 数据提交的瓶颈

传统渲染管线里，每个物体的参数通过 constant buffer 提交：

```hlsl
// CPU 侧伪代码
foreach (var obj in visibleObjects)
{
    cb.SetMatrix("_ObjectToWorld", obj.transform);
    cb.SetVector("_Color", obj.color);
    cb.SetFloat("_Metallic", obj.metallic);
    DrawMesh(obj.mesh, obj.material);
}
```

这套方式的问题：

- **CPU-GPU 同步**：每个 DrawCall 前 CPU 必须写完数据，GPU 才能开始，串行等待
- **带宽浪费**：每帧重复上传大量未变化的数据（静态物体的矩阵每帧都传）
- **扩展性差**：场景有 10 万个物体，CPU 遍历本身就是瓶颈

---

## GPU Scene 的核心思路

GPU Scene 的做法：

1. **场景启动或物体变化时**，把所有实例的数据写入一个 GPU 侧的 `StructuredBuffer`
2. **每帧渲染时**，着色器通过 `SV_InstanceID` 或等价的 instance index 去这个 buffer 里取数据
3. CPU 只需在数据真正变化时做增量更新，不再每帧全量提交

```hlsl
struct InstanceData
{
    float4x4 objectToWorld;
    float4x4 worldToObject;
    float4   albedoTint;
    float    metallic;
    float    smoothness;
    uint     materialIndex;
    uint     pad;
};

StructuredBuffer<InstanceData> _GPUScene;

Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
{
    InstanceData data = _GPUScene[instanceID];

    float3 posWS = mul(data.objectToWorld, float4(input.posOS, 1.0)).xyz;
    // ...
}
```

---

## Unreal 的 GPU Scene 实现

Unreal Engine 5 的 GPU Scene 是 Nanite 和 Lumen 的基础设施之一，位于 `GPUScene.h / GPUScene.cpp`。

**数据结构**：GPU Scene 维护两张大表：
- `GPUScene.PrimitiveBuffer`：每个 Primitive Component 的变换、包围盒、材质槽信息
- `GPUScene.InstanceSceneDataBuffer`：每个实例的 per-instance 覆盖数据

**更新机制**：Unreal 使用 **dirty list** 增量更新——只有标记为 dirty 的 primitive 才会在当帧重新上传，静态场景几乎零开销。

**着色器访问**：UE5 的材质系统通过内置宏 `GetPrimitiveData(PrimitiveId)` 访问 GPU Scene：

```hlsl
// UE5 内部宏，展开后读 GPUScene buffer
FPrimitiveSceneData PrimitiveData = GetPrimitiveData(Parameters.PrimitiveId);
float4x4 LocalToWorld = LWCToFloat(PrimitiveData.LocalToWorld);
float4 ObjectBounds = PrimitiveData.ObjectBoundsAndFlags;
```

Nanite 的 Visibility Buffer 正是依赖 GPU Scene：知道 triangle 所属的 primitive ID 后，直接从 GPU Scene 查材质参数，无需传统的 per-DrawCall constant buffer。

---

## Unity 的实现：DOTS Instancing 与 BRG

Unity 侧对应的机制叫 **DOTS Instancing**（在 Batch Renderer Group / BRG API 下使用）。

**BatchRendererGroup (BRG)**：Unity 的底层渲染批次接口，允许用户完全控制 per-instance 数据的上传方式：

```csharp
// C# 侧：分配 GPU 缓冲区，写入 per-instance 数据
var batchID = _brg.AddBatch(batchMetadata, gpuBuffer.bufferHandle);

// 每帧通过 OnPerformCulling 回调决定哪些实例可见
public JobHandle OnPerformCulling(
    BatchRendererGroup rendererGroup,
    BatchCullingContext cullingContext,
    BatchCullingOutput cullingOutput,
    IntPtr userContext)
{
    // 填写 DrawCommand，引擎根据此发 DrawCall
}
```

**DOTS Instancing Shader**：在 Shader 侧，用 `UNITY_ACCESS_DOTS_INSTANCED_PROP` 宏读取 per-instance 数据：

```hlsl
// 声明 per-instance 属性
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float,  _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float,  _Smoothness)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

// Fragment Shader 读取
half4 baseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
float metallic  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _Metallic);
```

与普通 `UNITY_INSTANCING_BUFFER` 的区别：

| | GPU Instancing (传统) | DOTS Instancing (BRG) |
|---|---|---|
| 数据来源 | constant buffer，CPU 每帧提交 | StructuredBuffer，增量更新 |
| 实例上限 | 受 constant buffer 大小限制 | 受 GPU 内存限制，可达数十万 |
| 与 SRP Batcher | 互斥 | 兼容（BRG 绕过 SRP Batcher） |
| 适用场景 | 中等数量同材质实例 | 大规模 GPU Driven 场景 |

---

## Per-Instance Data 的典型用途

除了变换矩阵，GPU Scene / DOTS Instancing 常用来存以下数据：

**颜色/材质变体**：同一个 Mesh，不同实例有不同颜色或磨损程度，用 per-instance color 而不是创建多份材质。

**动画状态**：骨骼动画烘焙成 Texture（Vertex Animation Texture），per-instance 存当前帧索引和动画 ID，让大量角色以不同相位播放同一个动画。

```hlsl
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float, _AnimFrame)
    UNITY_DOTS_INSTANCED_PROP(float, _AnimID)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

float frame  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AnimFrame);
float animID = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AnimID);

// 用 frame 计算 VAT UV，采样骨骼位置贴图
float2 vatUV = float2(frame / _TotalFrames, animID / _TotalAnims);
float3 posOffset = SAMPLE_TEXTURE2D_LOD(_VATPosition, sampler_VATPosition, vatUV, 0).xyz;
```

**LOD 渐变参数**：per-instance 存 LOD crossfade 权重，在 Shader 里做 dithering 过渡。

**破坏/状态**：建筑破损度、植被被踩弯曲程度，由游戏逻辑写入 GPU buffer，Shader 读取后改变顶点偏移或贴图混合权重。

---

## 数据更新策略

GPU Scene 的性能关键在**减少无效上传**：

**全量上传**：适合动态场景初始化，简单但开销大。

**Dirty Flag 增量更新**：只上传当帧发生变化的实例。Unreal 的实现：每次 `MarkRenderStateDirty()` 把 primitive 加入 dirty list，渲染线程在帧初统一处理。

**Double Buffering**：GPU 读当前帧 buffer 时，CPU 写下一帧 buffer，避免 CPU-GPU 同步等待。

```csharp
// Unity BRG 侧的增量更新模式
var writeBuffer = _gpuPersistentInstanceData.LockBufferForWrite<float4>(
    startIndex * kSizeOfFloat4,
    dirtyCount * kSizeOfFloat4);

// 只写脏数据
for (int i = 0; i < dirtyCount; i++)
    writeBuffer[i] = dirtyInstanceData[i];

_gpuPersistentInstanceData.UnlockBufferAfterWrite<float4>(dirtyCount * kSizeOfFloat4);
```

---

## 与 GPU Driven Rendering 的关系

GPU Scene 和 GPU Driven Rendering 是互补的两层：

```
CPU                    GPU
 │                      │
 │  上传 GPU Scene       │
 ├─────────────────────>│  InstanceData Buffer
 │                      │
 │  发 Dispatch          │
 ├─────────────────────>│  Culling Compute
 │                      │   └─ 读 GPU Scene 做视锥剔除
 │                      │   └─ 输出可见实例列表
 │                      │
 │  发 DrawIndirect      │
 ├─────────────────────>│  Vertex/Fragment Shader
 │                      │   └─ 用 instanceID 读 GPU Scene
 │                      │   └─ 取变换矩阵、材质参数
```

GPU Driven 决定**画哪些**，GPU Scene 决定**用什么数据画**。两者结合才是完整的现代大规模场景渲染方案。
