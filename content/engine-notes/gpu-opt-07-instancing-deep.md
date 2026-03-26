---
title: "GPU 渲染优化 07｜GPU Instancing 深度：DrawMeshInstanced vs Indirect、PerInstance Data 填充"
slug: "gpu-opt-07-instancing-deep"
date: "2026-03-25"
description: "GPU Instancing 有两套 API：DrawMeshInstanced 和 DrawMeshInstancedIndirect。本篇讲清楚两者的执行模型差异、PerInstance Data 怎么填充、移动端的实际限制，以及与 SRP Batcher 的关系和选择依据。"
tags:
  - "移动端"
  - "GPU"
  - "GPU Instancing"
  - "DrawMeshInstanced"
  - "Compute Shader"
  - "性能优化"
  - "URP"
series: "移动端硬件与优化"
weight: 2270
---
GPU Instancing 的基本概念（同一 Mesh 多实例一次 Draw Call）在入门文章里已经覆盖。这篇深入两个更实用的问题：两套 API 的差异和选择、PerInstance Data 的填充方式与限制。

---

## 两套 API 的本质区别

### DrawMeshInstanced

```csharp
Graphics.DrawMeshInstanced(
    mesh,
    submeshIndex,
    material,
    matrices,        // Matrix4x4[]，每个实例的 Transform
    count,
    materialPropertyBlock  // 可选，每实例的自定义属性
);
```

**执行模型**：CPU 每帧准备好所有实例的 `Matrix4x4[]` 数组，调用 API，Unity 在内部把数组上传到 GPU，发出一次 Draw Call。

**限制**：
- 每次调用最多 1023 个实例（Unity 硬性上限）
- 每帧 CPU 需要准备和上传整个数组，实例数多时 CPU → GPU 的数据传输有开销
- `materialPropertyBlock` 里的 per-instance 数据也需要 CPU 每帧填充

**适用场景**：实例数 < 1000，每帧实例数量变化不大，CPU 端有完整的实例列表。

---

### DrawMeshInstancedIndirect

```csharp
Graphics.DrawMeshInstancedIndirect(
    mesh,
    submeshIndex,
    material,
    bounds,
    bufferWithArgs,   // ComputeBuffer，包含 Draw 参数（实例数、索引数等）
    argsOffset,
    materialPropertyBlock
);
```

**执行模型**：Draw 参数（包括实例数量）存在 GPU 端的 `ComputeBuffer` 里，CPU 不需要知道具体数量，GPU 直接从 Buffer 里读取参数并执行。

实例数据（Transform、颜色等）也存在 GPU 端的 `ComputeBuffer` 里，Shader 通过 `StructuredBuffer` 读取，完全不经过 CPU。

**优势**：
- 实例数量由 GPU 决定（比如 GPU Culling 后动态确定），CPU 不参与
- 实例数据更新在 GPU 端完成（Compute Shader），零 CPU → GPU 传输代价
- 无 1023 实例数上限（受 GPU 内存限制）

**适用场景**：大量实例（>1000）、GPU Culling（视锥/遮挡剔除在 GPU 上做）、实例数量动态变化（粒子系统、植被 LOD）。

---

## PerInstance Data 的填充

### DrawMeshInstanced 的 MaterialPropertyBlock 方式

```csharp
var mpb = new MaterialPropertyBlock();

// 方式 1：SetFloatArray / SetVectorArray（每实例单个属性）
float[] scales = new float[count];
// 填充 scales...
mpb.SetFloatArray("_Scale", scales);

// 方式 2：配合 Matrix4x4[] 传 Transform
Matrix4x4[] matrices = new Matrix4x4[count];
// 填充 matrices...

Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count, mpb);
```

Shader 侧读取：

```hlsl
#pragma multi_compile_instancing

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float, _Scale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

half4 frag(Varyings i) : SV_Target
{
    float scale = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Scale);
    half4 color = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
    // ...
}
```

**注意**：`MaterialPropertyBlock.SetFloatArray` 设置的数组长度必须和实例数完全一致，否则运行时报错。每帧更新时需要重新填充整个数组（无增量更新）。

---

### DrawMeshInstancedIndirect 的 StructuredBuffer 方式

实例数据定义一个结构体，存在 `ComputeBuffer` 里：

```csharp
// 定义实例数据结构（需要和 Shader 里的结构体对齐）
struct InstanceData
{
    public Matrix4x4 objectToWorld;
    public Vector4 color;
}

// 创建 GPU Buffer（persistently mapped，每帧只更新变化的部分）
ComputeBuffer instanceBuffer = new ComputeBuffer(
    instanceCount,
    Marshal.SizeOf(typeof(InstanceData))
);

// 上传数据（仅首次或数据变化时）
instanceBuffer.SetData(instanceDataArray);

// 传给 Material
material.SetBuffer("_InstanceBuffer", instanceBuffer);

// Draw 参数 Buffer
uint[] args = new uint[5] {
    mesh.GetIndexCount(0),   // 索引数
    (uint)instanceCount,      // 实例数
    mesh.GetIndexStart(0),    // 索引起始偏移
    (uint)mesh.GetBaseVertex(0), // 顶点基础偏移
    0                         // 保留
};
ComputeBuffer argsBuffer = new ComputeBuffer(
    1, args.Length * sizeof(uint),
    ComputeBufferType.IndirectArguments
);
argsBuffer.SetData(args);

Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
```

Shader 侧读取：

```hlsl
struct InstanceData
{
    float4x4 objectToWorld;
    float4 color;
};

StructuredBuffer<InstanceData> _InstanceBuffer;

Varyings vert(Attributes v, uint instanceID : SV_InstanceID)
{
    InstanceData data = _InstanceBuffer[instanceID];
    float3 posWS = mul(data.objectToWorld, float4(v.positionOS, 1.0)).xyz;
    // ...
}
```

---

## GPU Culling 与 Indirect 结合

DrawMeshInstancedIndirect 最强大的用法是配合 Compute Shader 做 GPU 端视锥剔除，让 CPU 完全不参与实例筛选：

```hlsl
// Compute Shader：GPU 端视锥剔除
#pragma kernel CSMain

StructuredBuffer<InstanceData> _AllInstances;   // 所有实例（不分可见性）
AppendStructuredBuffer<InstanceData> _VisibleInstances; // 可见实例（追加写入）
float4 _FrustumPlanes[6];

[numthreads(64, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    InstanceData inst = _AllInstances[id.x];
    float3 center = inst.objectToWorld._m03_m13_m23;
    float radius = 1.0; // 假设包围球半径

    // 视锥平面测试
    bool visible = true;
    for (int i = 0; i < 6; i++)
    {
        if (dot(_FrustumPlanes[i].xyz, center) + _FrustumPlanes[i].w < -radius)
        {
            visible = false;
            break;
        }
    }

    if (visible)
        _VisibleInstances.Append(inst);
}
```

CPU 侧每帧只需要 Dispatch Compute Shader，不需要遍历实例列表，剔除工作完全在 GPU 上完成。`_VisibleInstances` 的实际写入数量通过 `ComputeBuffer.CopyCount` 写到 `argsBuffer` 的实例数字段。

---

## 移动端的实际限制

### Compute Shader 支持

DrawMeshInstancedIndirect 搭配 Compute Shader 的完整 GPU Culling 方案需要：
- Vulkan 1.0+ 或 Metal（iOS）
- Compute Shader 支持（OpenGL ES 3.1+）

**覆盖情况**：iOS A8+（iPhone 6）支持 Metal Compute，Android 中端以上支持 Vulkan / OpenGL ES 3.1。低端 Android（OpenGL ES 3.0）只支持 DrawMeshInstanced，不支持 Indirect + Compute 方案。

如果项目需要覆盖低端设备，需要提供回退路径：

```csharp
if (SystemInfo.supportsComputeShaders)
    DrawWithGPUCulling();
else
    DrawMeshInstanced(culledInstances); // CPU 端软剔除
```

### StructuredBuffer 在移动端的注意事项

部分旧 Android 设备（OpenGL ES 3.1，特定驱动版本）对 `StructuredBuffer` 的支持有 bug，表现为随机渲染错误。保险做法：在 Unity Import Settings 里对目标平台做驱动版本检测，或直接用 `DrawMeshInstanced` 作为低端设备回退。

---

## 与 SRP Batcher 的关系

GPU Instancing 和 SRP Batcher 不能同时工作，两者互斥。理解选择逻辑：

| 场景 | 推荐方案 | 原因 |
|------|---------|------|
| 相同 Mesh + 相同 Material，大量重复实例（植被、石头）| GPU Instancing | 真正合并 Draw Call，一次提交多个实例 |
| 相同 Shader，不同 Material（不同角色、不同材质参数）| SRP Batcher | 减少 CPU 设置材质参数的开销，不合并 Draw Call 但减少状态切换 |
| 完全不同 Shader 的物体 | 无法批处理 | 每个 Shader 都需要独立 Draw Call |

**SRP Batcher 的优化原理**：SRP Batcher 把每个材质的 Constant Buffer（CBUFFER）持久化在 GPU 端，每帧只上传变化的部分，避免 CPU 每帧重新设置所有材质参数。它减少的是 CPU 的准备时间，不是 Draw Call 数量。

**GPU Instancing 的优化原理**：真正把 N 个 Draw Call 合并成 1 个，减少的是 Draw Call 数量和对应的驱动开销。

**最常见的误解**：认为 SRP Batcher 开启后 GPU Instancing 就没用了。实际上：SRP Batcher 对"同 Shader 不同 Material"有效；GPU Instancing 对"同 Mesh 同 Material 大量实例"有效。两个场景不重叠，项目里通常两者都需要（不同物体用不同方案）。

---

## 实际项目中的植被方案

大规模植被是 GPU Instancing 最典型的使用场景，结合 LOD 和 GPU Culling：

```
远距离植被（> 50m）：
  DrawMeshInstancedIndirect + GPU 视锥剔除 + 低 LOD Mesh

中距离植被（20~50m）：
  DrawMeshInstanced + CPU 视锥剔除 + 中 LOD Mesh

近距离植被（< 20m）：
  DrawMeshInstanced + 完整 Mesh，允许 SSAO 和阴影接收
```

每个距离档单独一次 DrawMeshInstancedIndirect 调用，GPU 根据实例 Transform 自动判断 LOD，CPU 只负责按距离分桶，不做逐实例剔除。

---

## 小结

- **DrawMeshInstanced**：CPU 准备数组，每帧上传，上限 1023 实例，适合中等规模、数量稳定的场景
- **DrawMeshInstancedIndirect**：实例数据和 Draw 参数在 GPU 端，配合 Compute Shader 做 GPU Culling，适合大规模实例（植被、粒子）
- **PerInstance Data**：Instanced 用 `UNITY_INSTANCING_BUFFER`，Indirect 用 `StructuredBuffer<T>`，Shader 通过 `SV_InstanceID` 索引
- **移动端限制**：Compute Shader 方案需要 Vulkan / Metal，低端 Android（ES 3.0）只支持 DrawMeshInstanced；需要提供回退路径
- **vs SRP Batcher**：两者互斥但场景不重叠——SRP Batcher 优化同 Shader 不同 Material，GPU Instancing 优化同 Mesh 同 Material 大量实例
