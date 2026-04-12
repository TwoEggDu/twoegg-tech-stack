---
title: "Shader 进阶技法 14｜Compute Shader 入门：线程组、SV_GroupID 与 RWTexture2D"
slug: shader-advanced-14-compute-shader
date: "2026-03-28"
description: "Compute Shader 不经过光栅化管线，直接在 GPU 上做通用计算。本文从线程模型出发，讲清 numthreads、SV_DispatchThreadID 的含义，并用一个棋盘格生成示例串联 RWTexture2D 与 C# 端的完整调用流程。"
tags: ["Shader", "HLSL", "URP", "进阶", "Compute Shader", "GPU并行"]
series: "Shader 手写技法"
weight: 4420
---

大多数 Shader 都绑定在渲染管线上：顶点着色器处理几何，片元着色器决定像素颜色，整个流程围绕"画什么"展开。Compute Shader 完全不同，它不依附于任何几何或像素，纯粹就是让 GPU 上的大量核心同时跑一段程序。图像处理、粒子模拟、AI 推理加速、程序化纹理生成——只要能并行化，都可以交给 Compute Shader。

理解 Compute Shader 的关键是理解它的线程模型。GPU 不像 CPU 那样顺序执行，它是用海量小线程覆盖计算任务的。Compute Shader 把这些线程用一套三层结构来组织：Dispatch → Thread Group → Thread。

---

## 线程模型：从 Dispatch 到 Thread

C# 端调用 `ComputeShader.Dispatch(kernelIndex, x, y, z)` 时，三个整数 `x, y, z` 表示启动多少个线程组（Thread Group）。每个线程组内部又有固定数量的线程，这个数量由 Shader 里的 `[numthreads(tx, ty, tz)]` 声明决定。

最终实际跑起来的线程总数是：

```
totalThreads = (x * tx) * (y * ty) * (z * tz)
```

举个具体例子：要处理一张 512×512 的纹理，可以这样划分：

```hlsl
[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    // id.xy 对应纹理的像素坐标
}
```

C# 端：

```csharp
computeShader.Dispatch(kernel, 512 / 8, 512 / 8, 1);
// 即 Dispatch(kernel, 64, 64, 1)
// 每组 8×8 = 64 个线程，共 64×64 = 4096 个组
// 总线程数 = 512 × 512 = 262144
```

`numthreads` 的三个维度并没有固定含义，`(8,8,1)` 和 `(64,1,1)` 总线程数相同，只是逻辑组织方式不同。处理 2D 纹理通常用二维布局，更直观。

---

## 三种 SV 语义的区别

Compute Shader 提供三种内置的系统语义来告诉每个线程"自己是谁"：

| 语义 | 含义 | 典型用途 |
|---|---|---|
| `SV_DispatchThreadID` | 全局线程 ID（跨所有组） | 映射到纹理像素坐标 |
| `SV_GroupID` | 当前线程所在组的 ID | 组级别的数据分配 |
| `SV_GroupThreadID` | 线程在组内的局部 ID | 共享内存（groupshared）访问 |

绝大多数情况下，写纹理处理用 `SV_DispatchThreadID` 就够了。`SV_GroupThreadID` 在需要 `groupshared` 共享内存优化时才真正重要。

```hlsl
[numthreads(8, 8, 1)]
void CSMain(
    uint3 dispatchID : SV_DispatchThreadID,
    uint3 groupID    : SV_GroupID,
    uint3 localID    : SV_GroupThreadID)
{
    // dispatchID.xy = groupID.xy * 8 + localID.xy
}
```

---

## RWTexture2D：可读写的纹理资源

普通 Shader 里用 `Texture2D` 只能读取。Compute Shader 需要写入结果，所以要用 `RWTexture2D`（Read-Write Texture 2D）。

```hlsl
RWTexture2D<float4> _ResultTex;

[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    float4 color = float4(1, 0, 0, 1); // 写入红色
    _ResultTex[id.xy] = color;
}
```

注意 `_ResultTex[id.xy]` 用方括号索引，而不是 `Sample`——Compute Shader 里没有采样器，直接按像素坐标读写。

---

## StructuredBuffer 与 RWStructuredBuffer

除了纹理，Compute Shader 还常用结构化缓冲区传递任意数据。`StructuredBuffer<T>` 只读，`RWStructuredBuffer<T>` 可读写。

```hlsl
struct Particle
{
    float3 position;
    float3 velocity;
    float  lifetime;
};

RWStructuredBuffer<Particle> _Particles;

[numthreads(64, 1, 1)]
void UpdateParticles(uint3 id : SV_DispatchThreadID)
{
    uint idx = id.x;
    _Particles[idx].position += _Particles[idx].velocity * 0.016;
    _Particles[idx].lifetime -= 0.016;
}
```

C# 端用 `ComputeBuffer` 对应：

```csharp
ComputeBuffer buffer = new ComputeBuffer(count, sizeof(float) * 7);
computeShader.SetBuffer(kernel, "_Particles", buffer);
```

---

## 完整示例：生成棋盘格纹理

下面是一个完整示例：Compute Shader 生成棋盘格写入 RenderTexture，C# 端创建 RT 并触发计算。

**CheckerBoard.compute**

```hlsl
#pragma kernel CSMain

RWTexture2D<float4> _Result;
int _TileSize;

[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint2 tile = id.xy / (uint)_TileSize;
    float checker = (float)((tile.x + tile.y) % 2);
    _Result[id.xy] = float4(checker, checker, checker, 1.0);
}
```

**CheckerBoardGenerator.cs**

```csharp
using UnityEngine;

public class CheckerBoardGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public int textureSize = 512;
    public int tileSize = 32;

    private RenderTexture _rt;

    void Start()
    {
        _rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        _rt.enableRandomWrite = true; // 必须设置，才能被 Compute Shader 写入
        _rt.Create();

        int kernel = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernel, "_Result", _rt);
        computeShader.SetInt("_TileSize", tileSize);

        int groups = textureSize / 8;
        computeShader.Dispatch(kernel, groups, groups, 1);

        // 把结果赋给 MeshRenderer 的材质
        GetComponent<Renderer>().material.mainTexture = _rt;
    }

    void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }
}
```

`enableRandomWrite = true` 是关键，没有它 RenderTexture 不能被 Compute Shader 写入，运行时会报错。`textureSize / 8` 是线程组数量，前提是纹理尺寸能被 8 整除——实际项目中应该做对齐处理或在 Shader 里判断边界。

---

## 关于线程组大小的选择

`numthreads` 总线程数通常选 64 的倍数（64、128、256）。这是因为 NVIDIA GPU 以 32 个线程为一个 Warp 执行，AMD 以 64 为一个 Wavefront。让 `numthreads` 对齐这些硬件粒度，可以减少"空跑"的线程槽浪费。

处理 2D 纹理时 `(8, 8, 1)` = 64 是常见选择；纯一维数据处理用 `(64, 1, 1)` 或 `(256, 1, 1)`；需要大量 `groupshared` 的算法需要结合共享内存大小（通常上限 32KB）来反推 `numthreads`。

Compute Shader 打开了 GPU 通用计算的大门。后续章节在讲 GPU Driven Rendering 时，Compute Shader 做 Frustum Culling 的模式会再次用到这里的基础——线程 ID 映射到实例 ID，写入 Append Buffer，和这里写纹理像素是完全一样的思路。
