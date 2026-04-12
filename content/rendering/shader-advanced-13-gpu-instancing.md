---
title: "Shader 进阶技法 13｜GPU Instancing Shader：UNITY_INSTANCING_BUFFER 手写"
slug: "shader-advanced-13-gpu-instancing"
date: "2026-03-28"
description: "手写支持 GPU Instancing 的 URP Shader，理解 UNITY_INSTANCING_BUFFER_START/END 的声明方式、UNITY_ACCESS_INSTANCED_PROP 的读取方式，以及与 SRP Batcher 的互斥关系。"
tags: ["Shader", "HLSL", "URP", "进阶", "GPU Instancing", "性能优化"]
series: "Shader 手写技法"
weight: 4410
---

场景里有一千棵树、两千块石头，如果每个 Mesh Renderer 都触发一次 Draw Call，CPU 光是提交渲染命令就会成为瓶颈。GPU Instancing 把这一千次调用合并成一次：同一个网格、同一个材质，一次 Draw Call，GPU 内部循环绘制所有实例。每个实例可以有不同的变换矩阵，也可以有不同的自定义属性，比如颜色、溶解进度、UV 偏移。手写这套机制需要几个特定的 HLSL 宏。

---

## GPU Instancing 的原理

普通渲染流程：CPU 遍历每个对象 → 设置 per-object 常量缓冲（变换矩阵、材质属性）→ 提交 Draw Call。每次 Draw Call 都有 CPU-GPU 通信开销。

GPU Instancing 流程：CPU 把所有实例的 per-instance 数据（变换矩阵 + 自定义属性）打包成一个大数组上传到 GPU → 提交一次 Draw Call，附带实例数量 → GPU Vertex Shader 收到一个内置变量 `instanceID`，用它索引数组取出当前实例的数据。

关键约束：所有实例**必须使用相同的网格和相同的材质**。不同材质（哪怕只差一个属性）无法合并。`MaterialPropertyBlock` 可以在 C# 侧为不同实例设置不同属性值，GPU Instancing Shader 再从 instancing buffer 读取这些值。

---

## Material 上的 Enable GPU Instancing 做了什么

勾选 Material Inspector 上的 **Enable GPU Instancing**，Unity 会：

1. 在提交 Draw Call 时使用 `DrawMeshInstanced` 而非 `DrawMesh`。
2. 把 per-instance 的变换矩阵和属性打包到 GPU Buffer。
3. 编译 Shader 时启用 `#pragma multi_compile_instancing` 生成的变体。

如果 Shader 里没有 `#pragma multi_compile_instancing`，勾选 Enable GPU Instancing 也不会生效——材质上看起来勾了，但 Shader 根本不认识 instancing 宏。

---

## UNITY_INSTANCING_BUFFER：声明 per-instance 属性

每个实例可以有不同值的属性，不能放在普通的 `CBUFFER_START(UnityPerMaterial)` 里，因为那是所有实例共享的常量缓冲。per-instance 属性要用专门的宏块声明：

```hlsl
UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float,  _DissolveProgress)
UNITY_INSTANCING_BUFFER_END(Props)
```

`Props` 是缓冲区名，可以自定义，但同一 Shader 里要保持一致。这个块展开后是一个结构化缓冲数组，每个元素对应一个实例的数据。

读取时用：

```hlsl
float4 color    = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
float  dissolve = UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveProgress);
```

这个宏内部等价于 `Props[unity_InstanceID]._Color`，`unity_InstanceID` 是 GPU 自动传入的当前实例索引。

---

## 完整示例：每实例不同颜色的 Unlit Shader

```hlsl
Shader "Custom/InstancedColorUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // _Color 在 Inspector 里显示，但实际值由 MaterialPropertyBlock 覆盖
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 必须有这一行，才会生成 instancing 变体
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // 非 per-instance 的属性仍放在 UnityPerMaterial 里
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            // per-instance 属性：每个实例可以有不同的值
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                // 声明 instancing 所需的 instanceID 语义
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                // 在 Varyings 里传递 instanceID，Fragment Shader 才能访问
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 设置当前实例 ID，后续 UNITY_ACCESS_INSTANCED_PROP 才能正确索引
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 读取当前实例的颜色
                float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                return texColor * instanceColor;
            }
            ENDHLSL
        }
    }
}
```

---

## C# 侧：用 MaterialPropertyBlock 设置 per-instance 数据

`MaterialPropertyBlock` 是 C# 侧为单个 Renderer 设置属性的接口，不会创建新材质实例，GPU Instancing 能正确识别其中的 per-instance 属性。

```csharp
using UnityEngine;

public class InstanceColorSetter : MonoBehaviour
{
    [SerializeField] private Mesh     _mesh;
    [SerializeField] private Material _material;
    [SerializeField] private int      _count = 500;

    private Matrix4x4[]          _matrices;
    private MaterialPropertyBlock _props;
    private Vector4[]            _colors;

    void Start()
    {
        _matrices = new Matrix4x4[_count];
        _colors   = new Vector4[_count];
        _props    = new MaterialPropertyBlock();

        for (int i = 0; i < _count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-20f, 20f), 0,
                Random.Range(-20f, 20f));
            _matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

            // 每个实例随机颜色
            _colors[i] = new Vector4(
                Random.value, Random.value, Random.value, 1f);
        }
    }

    void Update()
    {
        // 批量设置 per-instance 颜色数组
        _props.SetVectorArray("_Color", _colors);

        // 一次 Draw Call 绘制所有实例
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _count, _props);
    }
}
```

`Graphics.DrawMeshInstanced` 每帧提交一次，最多支持 1023 个实例（DirectX 11 的限制）。超过 1023 个实例需要拆成多批，或者改用 `Graphics.DrawMeshInstancedIndirect`，通过 ComputeBuffer 传入实例数据，突破数量限制。

---

## SRP Batcher 与 GPU Instancing 的互斥关系

URP 默认启用 SRP Batcher，这是另一种减少 CPU 开销的机制。两者不能同时工作，URP 有明确的优先级：**SRP Batcher 优先于 GPU Instancing**。

| 机制 | 原理 | 适用场景 |
|---|---|---|
| SRP Batcher | 缓存 per-object 常量缓冲，减少 CPU 上传开销 | 不同网格、不同材质，实例数量少 |
| GPU Instancing | 合并相同网格+材质的 Draw Call | 大量相同网格，per-instance 属性不同 |
| Static Batching | 合并静态网格的顶点缓冲 | 完全静止的场景物件 |

如果场景里有大量相同网格需要 GPU Instancing，但 Shader 同时满足 SRP Batcher 的要求（所有属性都在 `CBUFFER` 里），URP 会选择 SRP Batcher 而忽略 GPU Instancing。

强制让 Shader 走 GPU Instancing 路径的方法：让 Shader **不满足** SRP Batcher 要求，最简单的方式是把某个属性放到 `UNITY_INSTANCING_BUFFER` 里（SRP Batcher 不支持 instancing buffer）。或者在 URP Pipeline Asset 里关闭 SRP Batcher，但这会影响全局性能，通常不推荐。

实践建议：对草地、人群、粒子等大量重复物件，手写 GPU Instancing Shader；对场景道具、建筑等种类繁多但数量中等的物件，依赖 SRP Batcher 更省事。两者并存时，保证各自的 Shader 结构符合目标路径的要求。
