---
title: "Shader 进阶技法 09｜GPU 粒子 Shader：StructuredBuffer 与 Instancing"
slug: "shader-advanced-09-gpu-particles"
date: "2026-03-26"
description: "CPU 粒子系统的瓶颈在于每帧 CPU 更新和 DrawCall 数量。GPU 粒子把粒子数据存入 StructuredBuffer，Compute Shader 并行更新，Shader 用 SV_InstanceID 读取并渲染——实现十万级粒子的实时渲染。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "GPU粒子"
  - "StructuredBuffer"
  - "Compute Shader"
  - "Instancing"
series: "Shader 手写技法"
weight: 4370
---
Unity 内置粒子系统（ParticleSystem）的每个粒子需要 CPU 更新，DrawCall 合批有限制。大量粒子（10 万+）时性能不足。GPU 粒子方案把粒子数据完全放在 GPU：Compute Shader 更新，DrawMeshInstancedIndirect 批量绘制。

---

## 整体架构

```
C#（每帧）
    ├── Compute Shader.Dispatch()   → 并行更新所有粒子（位置/速度/生命）
    └── Graphics.DrawMeshInstancedIndirect()  → 一次 DrawCall 绘制全部粒子

GPU 流水线
    ├── Compute Shader（CS）  读写 ParticleBuffer（RWStructuredBuffer）
    └── Vertex/Fragment Shader  读取 ParticleBuffer（StructuredBuffer），
                                 用 SV_InstanceID 索引当前粒子
```

---

## 粒子数据结构

在 C# 和 HLSL 中定义相同布局的结构体：

**C# 端：**
```csharp
struct ParticleData
{
    public Vector3 position;   // 12 bytes
    public Vector3 velocity;   // 12 bytes
    public float   life;       // 4  bytes（剩余寿命，0=死亡）
    public float   maxLife;    // 4  bytes
    public float   size;       // 4  bytes
    public Color   color;      // 16 bytes（RGBA float4）
}
// 总计 52 bytes / 粒子
```

**HLSL 端（`ParticleData.hlsl`）：**
```hlsl
struct ParticleData
{
    float3 position;
    float3 velocity;
    float  life;
    float  maxLife;
    float  size;
    float4 color;
};
```

---

## Compute Shader：粒子更新

```hlsl
// ParticleUpdate.compute
#pragma kernel UpdateParticles

RWStructuredBuffer<ParticleData> _Particles;
float  _DeltaTime;
float  _Gravity;
float3 _EmitterPos;
float  _EmitRate;    // 每帧重生概率（简化）
uint   _ParticleCount;

// 随机数（简单哈希）
float rand(uint seed)
{
    seed = seed * 747796405u + 2891336453u;
    seed = ((seed >> 16) ^ seed) * 277803737u;
    return float(seed) / 4294967295.0;
}

[numthreads(64, 1, 1)]
void UpdateParticles(uint3 id : SV_DispatchThreadID)
{
    uint idx = id.x;
    if (idx >= _ParticleCount) return;

    ParticleData p = _Particles[idx];

    if (p.life <= 0)
    {
        // 重生：随机初始化
        if (rand(idx + (uint)(_DeltaTime * 10000)) < _EmitRate)
        {
            p.position = _EmitterPos;
            p.velocity = float3(
                (rand(idx * 3    ) - 0.5) * 2.0,   // X
                rand(idx * 3 + 1) * 3.0 + 1.0,     // Y（向上）
                (rand(idx * 3 + 2) - 0.5) * 2.0    // Z
            );
            p.life    = rand(idx + 1) * 2.0 + 0.5;  // 0.5~2.5 秒
            p.maxLife = p.life;
            p.size    = rand(idx * 7) * 0.2 + 0.05;
            p.color   = float4(1, rand(idx * 11) * 0.5 + 0.5, 0.2, 1);  // 橙黄色
        }
    }
    else
    {
        // 更新物理
        p.velocity.y  -= _Gravity * _DeltaTime;
        p.position    += p.velocity * _DeltaTime;
        p.life        -= _DeltaTime;

        // 颜色随寿命渐暗
        float t    = p.life / p.maxLife;    // 1=新生，0=即将消亡
        p.color.a  = t;
        p.color.g  = t * 0.5 + 0.2;
    }

    _Particles[idx] = p;
}
```

---

## C# 管理脚本

```csharp
using UnityEngine;
using UnityEngine.Rendering;

public class GPUParticleSystem : MonoBehaviour
{
    [Header("Config")]
    public int         particleCount = 100000;
    public ComputeShader computeShader;
    public Material    particleMaterial;
    public Mesh        particleMesh;    // 一个 Quad

    [Header("Physics")]
    public float gravity   = 9.8f;
    public float emitRate  = 0.1f;     // 每粒子每帧重生概率

    private ComputeBuffer _particleBuffer;
    private ComputeBuffer _argsBuffer;
    private int           _kernel;
    private uint[]        _args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds        _bounds;

    void Start()
    {
        // 创建粒子缓冲区（sizeof 需手动对齐，此处按 13 个 float = 52 bytes）
        _particleBuffer = new ComputeBuffer(particleCount, 13 * sizeof(float));

        // 初始化粒子（全部标记为死亡，等待 Compute 激活）
        var initData = new float[particleCount * 13];
        _particleBuffer.SetData(initData);

        // Indirect Draw 参数
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint),
                                         ComputeBufferType.IndirectArguments);
        _args[0] = particleMesh.GetIndexCount(0);    // 索引数量
        _args[1] = (uint)particleCount;               // 实例数
        _args[2] = particleMesh.GetIndexStart(0);
        _args[3] = particleMesh.GetBaseVertex(0);
        _argsBuffer.SetData(_args);

        _kernel = computeShader.FindKernel("UpdateParticles");
        _bounds = new Bounds(Vector3.zero, Vector3.one * 200);
    }

    void Update()
    {
        // 设置 Compute 参数
        computeShader.SetBuffer(_kernel, "_Particles", _particleBuffer);
        computeShader.SetFloat ("_DeltaTime",    Time.deltaTime);
        computeShader.SetFloat ("_Gravity",      gravity);
        computeShader.SetVector("_EmitterPos",   transform.position);
        computeShader.SetFloat ("_EmitRate",     emitRate);
        computeShader.SetInt   ("_ParticleCount", particleCount);

        // Dispatch：每 64 个粒子一组
        computeShader.Dispatch(_kernel, Mathf.CeilToInt(particleCount / 64f), 1, 1);

        // 传给 Render Shader
        particleMaterial.SetBuffer("_Particles", _particleBuffer);

        // 一次 DrawCall
        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial,
                                           _bounds, _argsBuffer);
    }

    void OnDestroy()
    {
        _particleBuffer?.Release();
        _argsBuffer?.Release();
    }
}
```

---

## 粒子渲染 Shader

```hlsl
Shader "Custom/GPUParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "Queue" = "Transparent" }
        Pass
        {
            Name "GPUParticleForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One   // 加法混合（火焰/魔法效果）
            ZWrite Off  Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma instancing_options procedural:ParticleInstancingSetup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 粒子数据结构（与 C# 端布局一致）
            struct ParticleData
            {
                float3 position;
                float3 velocity;
                float  life;
                float  maxLife;
                float  size;
                float4 color;
            };

            StructuredBuffer<ParticleData> _Particles;

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };

            void ParticleInstancingSetup() {}   // procedural 回调（此处留空）

            Varyings vert(Attributes i, uint instanceID : SV_InstanceID)
            {
                Varyings o;

                ParticleData p = _Particles[instanceID];

                // 公告牌：让 Quad 始终朝向摄像机
                // 从摄像机矩阵取右向量和上向量
                float3 camRight = float3(UNITY_MATRIX_V[0][0], UNITY_MATRIX_V[1][0], UNITY_MATRIX_V[2][0]);
                float3 camUp    = float3(UNITY_MATRIX_V[0][1], UNITY_MATRIX_V[1][1], UNITY_MATRIX_V[2][1]);

                float3 worldPos = p.position
                                + camRight * i.pos.x * p.size
                                + camUp    * i.pos.y * p.size;

                o.hcs   = TransformWorldToHClip(worldPos);
                o.uv    = i.uv;
                o.color = p.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // 死亡粒子 color.a=0，直接透明
                return tex * input.color;
            }

            ENDHLSL
        }
    }
}
```

---

## 关键点说明

**SV_InstanceID** — 每个实例的编号（0 ~ particleCount-1），用来索引 `_Particles[instanceID]`。

**公告牌（Billboard）** — 粒子是 Quad，需要始终朝向摄像机。通过从 `UNITY_MATRIX_V`（View 矩阵）提取摄像机的右/上方向，把 Quad 顶点偏移到世界空间，实现公告牌效果。

**Blend SrcAlpha One（加法混合）** — 适合发光粒子（火焰、魔法）。普通烟雾用 `Blend SrcAlpha OneMinusSrcAlpha`。

**Bounds** — `DrawMeshInstancedIndirect` 需要一个包围盒用于摄像机剔除。设置足够大的 Bounds 避免粒子被错误剔除。

---

## 性能对比

| 方式 | 10 万粒子帧时 | 备注 |
|------|------------|------|
| CPU 粒子系统（C# 更新） | ~50ms | CPU 瓶颈 |
| GPU 粒子（Compute + Instancing） | ~2ms | GPU 并行 |

---

## 移动端适配

| 特性 | 支持情况 |
|------|---------|
| StructuredBuffer | OpenGL ES 3.1+（Android 7+）、Metal（iOS 9+）支持 |
| Compute Shader | OpenGL ES 3.1+、Metal 支持；ES 3.0 不支持 |
| DrawMeshInstancedIndirect | 同上 |

移动端低档机不支持 Compute Shader，需准备 CPU 粒子回退方案：检测 `SystemInfo.supportsComputeShaders` 决定走哪套路径。

---

## 小结

GPU 粒子 = StructuredBuffer（粒子数据）+ Compute Shader（并行更新）+ DrawMeshInstancedIndirect（单次绘制）+ SV_InstanceID（Shader 读取数据）。相比 CPU 粒子，性能提升数十倍，适合大规模特效场景。

下一篇：移动端 Shader 完整优化检查表——从数据类型到采样次数的系统性优化指南。
