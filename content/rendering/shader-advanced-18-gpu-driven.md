---
title: "Shader 进阶技法 18｜GPU Driven Rendering：GPU Culling、Indirect Draw 与 Multi-Draw Indirect"
slug: shader-advanced-18-gpu-driven
date: "2026-03-28"
description: "GPU Driven Rendering 把场景剔除和 DrawCall 生成都移到 GPU 端，彻底绕开 CPU-GPU 之间的瓶颈。本文讲清 Indirect Draw 的机制、Compute Shader 做 Frustum Culling 的完整流程，以及 Unity 中的 API 支持现状。"
tags: ["Shader", "HLSL", "进阶", "GPU Driven", "Indirect Draw", "Culling"]
series: "Shader 手写技法"
weight: 4460
---

传统渲染里，CPU 负责遍历场景、执行剔除、决定哪些物体需要绘制，然后为每个可见物体发出一条 DrawCall。DrawCall 本身的 CPU 开销是固定的——设置状态、提交命令、驱动层处理。场景规模增大，DrawCall 数量线性增长，CPU 成为瓶颈，GPU 大部分时间在等待 CPU 喂给它工作。

GPU Driven Rendering 翻转了这个模式：CPU 只做最粗粒度的"准备数据"，剔除和 DrawCall 的生成全部在 GPU 上通过 Compute Shader 完成。CPU 发出一条 Indirect DrawCall，GPU 自己决定要画多少个实例、画哪些。

---

## DrawMeshInstancedIndirect：args buffer 由 GPU 写入

普通的 `DrawMeshInstanced` 调用：CPU 告诉 GPU 要画多少个实例。Indirect 版本把这个数字放进一个 GPU 缓冲区里，CPU 发出命令时并不知道（也不需要知道）具体数量，GPU 读取缓冲区里的值来决定。

Indirect Args Buffer 的结构（对应 `DrawMeshInstancedIndirect`）：

```
uint indexCountPerInstance;  // 每个实例的索引数量（网格三角形数 × 3）
uint instanceCount;          // 实例数量（由 GPU Culling Compute 写入）
uint startIndexLocation;     // 从哪个索引开始
int  baseVertexLocation;     // 顶点偏移
uint startInstanceLocation;  // 实例偏移
```

C# 端创建：

```csharp
// Args Buffer：5 个 uint
ComputeBuffer argsBuffer = new ComputeBuffer(
    1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

uint[] args = new uint[5];
args[0] = mesh.GetIndexCount(0);    // 索引数量
args[1] = 0;                        // 初始实例数为 0，由 GPU Culling 填写
args[2] = mesh.GetIndexStart(0);
args[3] = (uint)mesh.GetBaseVertex(0);
args[4] = 0;
argsBuffer.SetData(args);

// 每帧渲染
Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
```

`instanceCount` 这个字段由 GPU Culling 的 Compute Shader 写入，CPU 端不需要知道最终画了多少个。

---

## Compute Shader 做 Frustum Culling

每个实例都有一个 AABB（轴对齐包围盒）。Frustum Culling 就是检查这个 AABB 是否和视锥体的六个平面有交集，全部在某个平面外侧则剔除。

```hlsl
#pragma kernel FrustumCull

struct InstanceData
{
    float3 boundsCenter;
    float3 boundsExtent;
    float4x4 localToWorld;
};

struct DrawData
{
    float4x4 objectToWorld;
};

StructuredBuffer<InstanceData>    _AllInstances;
AppendStructuredBuffer<DrawData>  _VisibleInstances;
RWStructuredBuffer<uint>          _ArgsBuffer;

float4 _FrustumPlanes[6]; // 视锥体六个平面（法线 + 距离）
int    _InstanceCount;

bool IsAABBInFrustum(float3 center, float3 extent)
{
    for (int i = 0; i < 6; i++)
    {
        float3 normal = _FrustumPlanes[i].xyz;
        float  dist   = _FrustumPlanes[i].w;

        // 沿法线方向的最远点到平面的距离
        float r = dot(abs(normal), extent);
        float d = dot(normal, center) + dist;

        if (d + r < 0) return false; // AABB 完全在平面外
    }
    return true;
}

[numthreads(64, 1, 1)]
void FrustumCull(uint3 id : SV_DispatchThreadID)
{
    uint idx = id.x;
    if (idx >= (uint)_InstanceCount) return;

    InstanceData inst = _AllInstances[idx];

    // 把包围盒变换到世界空间
    float3 worldCenter = mul(inst.localToWorld, float4(inst.boundsCenter, 1)).xyz;
    // 近似：用缩放最大轴估算 extent（精确做法需逐轴变换）
    float3 worldExtent = abs(mul((float3x3)inst.localToWorld, inst.boundsExtent));

    if (IsAABBInFrustum(worldCenter, worldExtent))
    {
        DrawData draw;
        draw.objectToWorld = inst.localToWorld;
        _VisibleInstances.Append(draw);

        // 原子递增 args buffer 里的 instanceCount（偏移 4 字节 = 第二个 uint）
        InterlockedAdd(_ArgsBuffer[1], 1);
    }
}
```

C# 端每帧：

```csharp
// 重置 instanceCount 为 0
argsBuffer.SetData(new uint[] {
    (uint)mesh.GetIndexCount(0), 0,
    (uint)mesh.GetIndexStart(0), (uint)mesh.GetBaseVertex(0), 0
});

// 更新视锥体平面
computeShader.SetVectorArray("_FrustumPlanes", GetFrustumPlanes(Camera.main));
computeShader.SetInt("_InstanceCount", totalInstanceCount);

// 分配 AppendBuffer（每帧 Reset Counter）
visibleBuffer.SetCounterValue(0);
computeShader.SetBuffer(kernel, "_VisibleInstances", visibleBuffer);
computeShader.SetBuffer(kernel, "_ArgsBuffer", argsBuffer);
computeShader.SetBuffer(kernel, "_AllInstances", allInstanceBuffer);

int groups = Mathf.CeilToInt(totalInstanceCount / 64.0f);
computeShader.Dispatch(kernel, groups, 1, 1);

// 渲染，GPU 从 argsBuffer 读取 instanceCount
Graphics.DrawMeshInstancedIndirect(mesh, 0, material, infiniteBounds, argsBuffer);
```

---

## AppendStructuredBuffer 与 Counter

`AppendStructuredBuffer<T>` 是一种特殊的 RWStructuredBuffer，内置一个原子计数器，调用 `.Append(item)` 会自动递增计数器并把数据写到下一个槽位，无需手动管理索引。它是 GPU Culling 输出可见列表的标准做法。

C# 端用 `ComputeBuffer.CopyCount()` 可以把计数器的值拷贝到另一个缓冲区（通常就是 args buffer），但这需要一次 GPU-CPU 同步，有延迟。更高效的方案是直接在 Culling Compute 里用 `InterlockedAdd` 写 args buffer，如上例所示。

---

## Hi-Z Occlusion Culling

Frustum Culling 只能剔除视锥体外的物体，视锥体内被遮挡的物体还是会浪费 GPU。Hi-Z（Hierarchical Z）Occlusion Culling 用上一帧的深度图来预测遮挡：

1. 构建深度图的 mipmap 链（每级取 4 个子像素的最大深度值）
2. 对每个实例，把包围盒投影到屏幕空间，算出覆盖的屏幕范围
3. 选择合适 mip 层级（让包围盒屏幕范围正好落在 2×2 像素内），采样深度
4. 如果包围盒的最近深度比 Hi-Z 深度图的值还大（更远），说明被遮挡，剔除

```hlsl
// Hi-Z 采样（简化）
float2 screenMin = ...; // 包围盒投影后的屏幕最小坐标
float2 screenMax = ...; // 包围盒投影后的屏幕最大坐标
float  boxMinDepth = ...; // 包围盒最近深度（NDC）

float  size   = max(screenMax.x - screenMin.x, screenMax.y - screenMin.y);
float  mip    = ceil(log2(size * max(_ScreenWidth, _ScreenHeight)));
float2 center = (screenMin + screenMax) * 0.5;

float hiZDepth = _HiZBuffer.SampleLevel(sampler_HiZBuffer, center, mip).r;

if (boxMinDepth > hiZDepth) // 包围盒比遮挡物更远
{
    // 被遮挡，不输出到 VisibleInstances
    return;
}
```

Hi-Z 用上一帧深度，对快速移动的相机会有一帧的误判（幽灵剔除），通常用保守估计或小幅扩大包围盒来减少误判。

---

## 完整流程

```
每帧 GPU Driven 渲染流程：

① CPU：更新所有实例的 Transform → StructuredBuffer
② CPU：重置 argsBuffer instanceCount = 0
③ CPU：计算视锥体平面，SetVectorArray
④ GPU Compute：FrustumCull（可选接 Hi-Z Cull）
   → 输出 VisibleInstances（AppendBuffer）
   → 写 argsBuffer.instanceCount
⑤ GPU：DrawMeshInstancedIndirect（读 argsBuffer）
   → Vertex Shader 用 SV_InstanceID 从 VisibleInstances 取 Transform
   → Fragment Shader 正常渲染
```

---

## Unity 的支持

Unity 中 GPU Driven 的 API 演进经历了几个阶段：

```csharp
// 旧 API（Unity 2018+，至今仍有效）
Graphics.DrawMeshInstancedIndirect(mesh, subMesh, material, bounds, argsBuffer);

// 新 API（Unity 6 / URP 17+）
// RenderMeshIndirect 整合进 BatchRendererGroup，
// 支持更精细的 Batch 控制和多材质
RenderParams rp = new RenderParams(material);
rp.worldBounds  = new Bounds(Vector3.zero, Vector3.one * 1000);
Graphics.RenderMeshIndirect(rp, mesh, commandBuffer, commandCount);
```

Unity 6 引入的 GPU Resident Drawer 在引擎内部自动为静态和动态物体启用类似 GPU Driven 的批处理，开发者不需要手动写 Compute Shader——但如果需要自定义 Culling 逻辑或跟上一节 Visibility Buffer 配合，还是要走手动路线。

GPU Driven 和 Compute Shader 是现代渲染架构的两大支柱，理解它们之间的数据流——Cull Compute 写 Append Buffer，Indirect Draw 消费 Buffer——是迈向高性能大世界渲染的必经之路。
