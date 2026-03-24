+++
title = "GPU Instancing 与 SRP Batcher：两种减少 CPU 开销的机制"
slug = "unity-rendering-01b2-gpu-instancing-srp-batcher"
date = 2025-01-26
description = "GPU Instancing 如何用一次 Draw Call 渲染大量同 Mesh 物体，SRP Batcher 如何通过持久化 CBuffer 减少 SetPass 开销，以及两者的选择逻辑和常见陷阱。"
[taxonomies]
tags = ["Unity", "GPU Instancing", "SRP Batcher", "Draw Call", "性能优化"]
series = ["Unity 渲染系统"]
[extra]
weight = 275
+++

如果只用一句话概括这篇：GPU Instancing 解决"大量相同物体"的 Draw Call 爆炸，SRP Batcher 解决"大量不同物体但用同一 Shader"的 SetPass 开销——两者机制不同，针对不同瓶颈，不能互相替代。

---

## 从上一篇出发

01b（Draw Call 与批处理）列出了四种合批方式，但对 GPU Instancing 和 SRP Batcher 只写了"条件和效果"，没有说清楚 GPU 侧的数据组织方式。理解这个机制，才能知道什么情况下它会失效，以及为什么 `MaterialPropertyBlock` 是正确的用法。

---

## GPU Instancing：一次 Draw Call，N 个实例

### 问题场景

场景里有 500 棵树，每棵树用同一个 Mesh 和同一个 Material，但位置和旋转各不同。

普通渲染：500 次 Draw Call，每次 CPU 把不同的 Transform 矩阵写入 CBuffer，然后调用一次 DrawMesh。

GPU Instancing 的方案：**一次 DrawMeshInstanced 调用，把 500 个 Transform 矩阵打包成一个数组传给 GPU，GPU 内部循环 500 次，每次用不同的矩阵渲染同一个 Mesh**。

```
普通渲染（500 次 Draw Call）：
  CPU → GPU: DrawMesh(mesh, mat, matrix0)
  CPU → GPU: DrawMesh(mesh, mat, matrix1)
  CPU → GPU: DrawMesh(mesh, mat, matrix2)
  ... × 500

GPU Instancing（1 次 Draw Call）：
  CPU → GPU: DrawMeshInstanced(mesh, mat, [matrix0, matrix1, ..., matrix499])
               GPU 内部：for instanceID in 0..499 { render(mesh, mat, matrices[instanceID]) }
```

### GPU 侧的数据结构

GPU Instancing 在 GPU 侧使用一个 **Instance Data Buffer**（本质是一个结构体数组）：

```hlsl
// GPU 侧（Shader 里自动展开，开发者不需要手写）
// 开启 INSTANCING_ON 时，Unity 自动注入这套机制

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_ObjectToWorld)   // 每个实例的 M 矩阵
    UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_WorldToObject)   // M 矩阵的逆
    UNITY_DEFINE_INSTANCED_PROP(float4,   unity_Color)           // 每实例颜色（可选）
UNITY_INSTANCING_BUFFER_END(PerInstance)

// 在 Vertex Shader 里，通过 instanceID 索引：
float4x4 objectToWorld = UNITY_ACCESS_INSTANCED_PROP(PerInstance, unity_ObjectToWorld);
```

`gl_InstanceID`（OpenGL）/ `SV_InstanceID`（HLSL）是 GPU 硬件内置的变量，在同一次 Instanced Draw Call 里，每个实例的这个值从 0 递增到 N-1，Shader 用它索引 Instance Data Buffer 里对应的数据。

### 每实例不同的属性

除了 Transform 矩阵，你可以让每个实例有不同的材质属性（颜色、UV 偏移等），方法是把这些属性也放进 Instance Data Buffer：

```csharp
// CPU 侧：通过 MaterialPropertyBlock 设置每实例属性
var mpb = new MaterialPropertyBlock();
for (int i = 0; i < count; i++)
{
    mpb.SetColor("_Color", colors[i]);
    // 重要：这里传的是 mpb，不是直接设 material.color
    Graphics.DrawMesh(mesh, matrices[i], material, 0, null, 0, mpb);
}
```

```hlsl
// Shader 侧：声明这个属性在 Instance Buffer 里
UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// 用宏取值，而不是直接用 _Color
float4 color = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
```

### MaterialPropertyBlock vs material.SetXxx

这是 GPU Instancing 最常见的陷阱：

```csharp
// ❌ 错误：直接修改 material 会创建 material 的副本
//    每个物体用了不同的 material 实例 → 不满足"相同 Material"条件 → Instancing 失效
renderer.material.color = Color.red;

// ✅ 正确：MaterialPropertyBlock 不修改 Material 本身
//    数据存在 per-renderer 的 PropertyBlock 里，合批时被打包进 Instance Buffer
var mpb = new MaterialPropertyBlock();
mpb.SetColor("_Color", Color.red);
renderer.SetPropertyBlock(mpb);
```

`renderer.material`（注意是属性访问，不是 `sharedMaterial`）每次访问都会自动创建 Material 的一个副本，这个副本和原 Material 不再是同一个实例，GPU Instancing 的合批条件（必须是同一个 Material 对象）立即被打破。

### GPU Instancing 失效的条件

| 条件 | 说明 |
|---|---|
| Mesh 不同 | 必须是同一个 Mesh |
| Material 不同 | 必须是同一个 Material 实例（sharedMaterial）|
| Shader 未开启 `#pragma multi_compile_instancing` | Shader 没有声明支持 Instancing |
| SkinnedMeshRenderer | 蒙皮动画的 Mesh 每帧变化，GPU 侧没有固定 VB，默认不支持 Instancing（需要 GPU Skinning 特殊处理）|
| 物体开了 Static Batching | Static Batching 会把 Mesh 合并，合并后的 Mesh 不再是原始 Mesh |
| 单次 Instanced Draw 超过上限 | 每次 DrawMeshInstanced 最多 1023 个实例，超出需要分批 |

---

## SRP Batcher：持久化 CBuffer，减少 SetPass 开销

### 问题场景

场景里有 500 个物体，每个物体用了不同的 Material，但所有 Material 都用同一个 Shader（比如 URP Lit Shader）。

这种情况 GPU Instancing 不适用（Material 不同），Static/Dynamic Batching 也不适用（Mesh 各异）。

但这 500 次 Draw Call 之间有一个共同点：**它们都用同一个 Shader**，SetPass（切换 Shader Program + 上传全局参数）只需要做一次。

**SRP Batcher 的核心思路**：不减少 Draw Call 数量，而是消除 Draw Call 之间不必要的数据上传。

### CBuffer 持久化原理

在没有 SRP Batcher 的情况下，每次 Draw Call 前 CPU 都需要把当前物体的 CBuffer（Transform、Material 参数）重新上传到 GPU：

```
普通渲染流程：
  Draw Object A:
    CPU → GPU: 上传 CBuffer A（Transform + Material params）← 每次都要传
    GPU: 执行 Draw A

  Draw Object B:
    CPU → GPU: 上传 CBuffer B（Transform + Material params）← 每次都要传
    GPU: 执行 Draw B
```

SRP Batcher 把每个物体的 CBuffer 在 GPU 显存里持久化：

```
SRP Batcher 流程：
  初始化：
    GPU 显存里为每个物体分配固定的 CBuffer 槽位

  每帧：
    如果 Object A 的 Transform 没变 → 不上传
    如果 Object A 的 Material params 变了 → 只上传变化的部分

  Draw Object A:
    GPU: 直接读已在显存里的 CBuffer A → 执行 Draw A（无 CPU→GPU 数据传输）

  Draw Object B:
    GPU: 直接读已在显存里的 CBuffer B → 执行 Draw B
```

本质上，SRP Batcher 把"每帧重新上传 CBuffer"变成了"只上传变化的 CBuffer"，对于静止或参数不变的物体，CPU 侧的 SetPass 开销几乎为零。

### SRP Batcher 兼容性要求

SRP Batcher 对 Shader 有严格要求：

```hlsl
// Shader 必须把 per-object 数据（Transform、材质参数）放在两个固定名称的 CBuffer 里

// 固定的 Transform CBuffer（Unity 自动提供）
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4   unity_LODFade;
    // ... 其他 per-object 数据
CBUFFER_END

// 固定的材质参数 CBuffer（Shader 作者定义，名字必须是 UnityPerMaterial）
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float  _Smoothness;
    float  _Metallic;
    // ... 所有 Material Properties
CBUFFER_END
```

如果 Shader 里的材质参数没有放在 `UnityPerMaterial` CBuffer 里（比如直接写 `float4 _BaseColor;`），SRP Batcher 会拒绝这个 Shader，回退到普通渲染。

在 Frame Debugger 里可以确认：SRP Batch 一行显示 `SRP Batch (N objects)`，括号里的数字是本次批次合并了多少个 Draw Call。

### GPU Instancing vs SRP Batcher 选择逻辑

```
场景里的物体是否使用同一个 Mesh + 同一个 Material，且大量重复？
  → YES：GPU Instancing（一次 Draw Call 渲染所有实例）
  → NO：是否用同一个 Shader（但 Material 参数各不同）？
          → YES：SRP Batcher（减少 SetPass 和 CBuffer 上传，Draw Call 数量不变）
          → NO：考虑 Static Batching（静态场景）或 Dynamic Batching（小 Mesh）
```

**两者可以同时工作**：一个场景里，植被用 GPU Instancing（大量同 Mesh），角色用 SRP Batcher（不同 Mesh 但同 Shader），互不干扰。

**SRP Batcher 不能与 GPU Instancing 同时作用于同一物体**：当 GPU Instancing 生效时，SRP Batcher 的 per-object CBuffer 持久化机制会被绕过（因为 Instancing 用的是 Instance Buffer，不是普通 CBuffer）。

---

## 在 Frame Debugger 里诊断

Frame Debugger 里可以直接看到哪种优化在工作：

```
Frame Debugger 显示                       含义
────────────────────────────────────────────────────────
SRP Batch (150 objects)                  150 个物体被 SRP Batcher 合并
  ↕ 展开后显示 150 行 Draw                每行仍是独立 Draw Call，但 SetPass 只做了 1 次

Draw Mesh "Tree_LOD0" (Instance 500)     500 个实例被 GPU Instancing 合并成 1 次 Draw
  ↕ 展开后是 1 行（单次 Instanced Draw）

Draw Mesh "Cube_001"                     没有合批，单独一次 Draw Call
  → 右键 → "Why not batched?"
    "GameObject is not in same layer"    揭示原因
```

**SRP Batcher 和 GPU Instancing 的 Frame Debugger 区别**：
- SRP Batch 的 N 个 Draw Call 展开后仍然是 N 行（Draw Call 数量没变，减少的是 SetPass）
- GPU Instanced Draw 展开后是 1 行（Draw Call 数量变成了 1）

---

## 小结

| | GPU Instancing | SRP Batcher |
|---|---|---|
| 解决的瓶颈 | Draw Call 数量（CPU 提交次数）| SetPass 开销（CBuffer 上传）|
| 要求 | 同 Mesh + 同 Material | 同 Shader（CBuffer 结构符合规范）|
| Draw Call 变化 | N → 1 | N → N（不变）|
| 典型场景 | 植被、人群、弹幕、粒子 Mesh 模式 | 场景物件、角色、UI |
| 每实例差异 | MaterialPropertyBlock + Instanced Buffer | 每个 Material 独立的 UnityPerMaterial CBuffer |
| 与 Skinned Mesh | 默认不支持 | 支持（CBuffer 结构满足要求即可）|
