+++
title = "Unity 渲染系统补B｜CBuffer 超限与常量缓冲区管理"
slug = "unity-rendering-supp-b-cbuffer"
date = 2026-03-26
description = "Shader 里声明了很多属性，它们在 GPU 里存在哪？CBuffer（Constant Buffer）容量有限，超限后性能骤降。这篇讲清楚 CBuffer 的工作原理、Unity 的 UnityPerMaterial 规范、超限的表现与诊断方法。"
weight = 1510
[taxonomies]
tags = ["Unity", "Rendering", "Shader", "CBuffer", "性能", "HLSL"]
[extra]
series = "Unity 渲染系统"
+++

每次写 Shader 时，`CBUFFER_START(UnityPerMaterial) ... CBUFFER_END` 这个结构经常让人困惑——为什么要放在这里？不放会怎样？这篇把 Constant Buffer 的机制讲清楚，以及 Unity 是怎么用它来支撑 SRP Batcher 的。

---

## Constant Buffer 是什么

GPU 执行着色器时，每个线程（每个顶点/像素）都要读取一批**不随顶点/像素变化的参数**——比如变换矩阵、材质颜色、光照强度。这些参数叫 **Uniform（统一变量）**，在 HLSL 里叫 **Constant（常量）**。

GPU 为这些常量提供了专用的高速缓存：**Constant Buffer（CBUFFER）**。它的特点是：
- 读取速度远快于普通显存
- 一次批量上传整个 CBUFFER 块，不逐变量上传
- 容量有限（通常 64KB / CBUFFER 槽）

```hlsl
// HLSL 里的 CBuffer 声明
cbuffer MyParams : register(b0)
{
    float4x4 _MatrixMVP;   // 64 bytes
    float4   _BaseColor;   // 16 bytes
    float    _Roughness;   // 4 bytes
    float3   _padding;     // 隐式填充至 16 bytes 对齐
}
```

---

## float4 对齐：超限的根源

CBUFFER 内部每个元素按 **float4（16 bytes）** 对齐。一个单独的 `float` 不是占 4 bytes，而是独占一个 float4 槽位：

```hlsl
// 看起来只有 3 个 float（12 bytes）
cbuffer Test
{
    float _A;   // 实际占用 float4 = 16 bytes（后 12 bytes 浪费）
    float _B;   // 再占 16 bytes
    float _C;   // 再占 16 bytes
}
// 总计：48 bytes，不是 12 bytes
```

**正确做法：** 把同类 float 打包到 float4：

```hlsl
cbuffer Test
{
    float4 _Params;   // x=_A, y=_B, z=_C, w=unused → 只占 16 bytes
}
```

HLSL 也允许跨 float4 边界声明（但不推荐，某些驱动行为不一致）：

```hlsl
// 危险做法：float3 + float 刚好 16 bytes，但顺序和对齐依赖实现
float3 _Dir;    // 12 bytes
float  _Speed;  // 4 bytes → 这两个会被打包到同一个 float4
```

---

## Unity 的 CBuffer 规范

### UnityPerMaterial

Unity 的 SRP Batcher 要求所有材质属性放入名为 `UnityPerMaterial` 的 CBUFFER：

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseMap_ST;    // Tiling + Offset（float4）
    float  _Metallic;
    float  _Smoothness;
    float  _Cutoff;
    float  _padding;       // 手动填充至 float4 对齐
CBUFFER_END
```

SRP Batcher 的工作原理：把每个材质的 `UnityPerMaterial` 数据缓存在 GPU 显存里的一个 **Per-Object** CBUFFER 数组中。DrawCall 时只需更新 offset 指针，不重新上传数据——这是它比旧版 MaterialPropertyBlock 快的原因。

**前提条件：** 同一 Shader 的所有 Pass 的 `UnityPerMaterial` 布局必须完全一致（变量数量和顺序相同）。不一致会导致 SRP Batcher 不能合批。

### UnityPerDraw 和 UnityPerFrame

除了 `UnityPerMaterial`，URP 还有两个内置 CBUFFER：

| CBUFFER | 更新频率 | 内容 |
|---------|---------|------|
| `UnityPerFrame` | 每帧一次 | 时间、摄像机位置、雾参数 |
| `UnityPerDraw` | 每个 DrawCall | 物体变换矩阵（M、MVP）、LightProbe SH 系数 |
| `UnityPerMaterial` | 材质变化时 | 用户自定义材质属性 |

这三个 CBUFFER 共同支撑一次 DrawCall，分别绑定到不同的寄存器槽（`b0` / `b1` / `b2`）。

---

## CBUFFER 超限的表现与危害

### 超限条件

不同 GPU 的 CBUFFER 大小限制：

| 平台 | 单个 CBUFFER 最大 | 总 CBUFFER 槽数 |
|------|----------------|---------------|
| PC DX11/DX12 | 64 KB | 14 个（b0~b13）|
| Metal（Apple GPU） | 4 KB~64 KB（视驱动）| 31 个 |
| OpenGL ES 3.0 | 16 KB | 实现相关 |
| Vulkan | 通常 64 KB | 实现相关 |

`UnityPerMaterial` 填满 64 KB = 4096 个 float4 = 16384 个 float，极难超限。**真正的风险不是容量超限，而是布局不规范导致的问题。**

### 常见问题

**问题一：属性在 CBUFFER 外声明**

```hlsl
// ❌ 在 CBUFFER 外声明 uniform
float _MyParam;    // 这是全局 uniform，不走 SRP Batcher

// ✅ 正确
CBUFFER_START(UnityPerMaterial)
    float _MyParam;
CBUFFER_END
```

属性在 CBUFFER 外声明时，每次 DrawCall 都需要单独上传，破坏 SRP Batcher 合批，增加 CPU 开销。

**问题二：跨 Pass 布局不一致**

```hlsl
// ForwardLit Pass
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float  _Cutoff;
CBUFFER_END

// ShadowCaster Pass（漏写了属性）
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    // ❌ 缺少 _Cutoff → 布局不一致 → SRP Batcher 失效
CBUFFER_END
```

SRP Batcher 兼容性检查可在 Frame Debugger 里验证——看 DrawCall 是否显示 "SRP Batch"。

**问题三：float 未打包，浪费槽位**

```hlsl
// ❌ 浪费：每个 float 占一个 float4
CBUFFER_START(UnityPerMaterial)
    float _A;    // → 16 bytes
    float _B;    // → 16 bytes
    float _C;    // → 16 bytes
    float _D;    // → 16 bytes
CBUFFER_END
// 总计：64 bytes

// ✅ 打包：共用一个 float4
CBUFFER_START(UnityPerMaterial)
    float4 _ABCD;   // x=_A, y=_B, z=_C, w=_D → 16 bytes
CBUFFER_END
```

---

## 检查 SRP Batcher 兼容性

Frame Debugger → 找到 DrawCall → 右侧信息栏：

- **"SRP Batch"** = 成功合批
- **"Node different CBuffer layout"** = CBuffer 布局不一致
- **"Renderers not compatible"** = 某个 Pass 有属性在 CBuffer 外

也可以通过代码检查：
```csharp
// 检查某个 Material 是否 SRP Batcher 兼容
bool isCompatible = material.enableInstancing == false
    && material.shader.isSupported;
// 更准确的方式是看 Frame Debugger
```

---

## MaterialPropertyBlock vs CBUFFER

有些场景需要在运行时给单个物体设置不同的参数（如角色血量颜色），有两种方式：

**MaterialPropertyBlock（旧方案）：**
```csharp
var mpb = new MaterialPropertyBlock();
mpb.SetColor("_BaseColor", healthColor);
renderer.SetPropertyBlock(mpb);
```
问题：绕过了 SRP Batcher 的缓存机制，每帧都要重新上传数据。

**GPU Instancing + InstanceID（新方案）：**
```hlsl
UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_INSTANCING_BUFFER_END(Props)

// Fragment Shader 里
half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
```
GPU Instancing 把每个实例的属性打包进一个数组，一次 DrawCall 绘制多个实例，性能远优于 MaterialPropertyBlock。

---

## 小结

- CBUFFER 是 GPU 上的高速常量缓存，按 float4 对齐
- `UnityPerMaterial` 是 SRP Batcher 合批的关键——所有材质属性必须放进去，且各 Pass 布局一致
- float 属性尽量打包成 float4，减少槽位浪费
- 运行时逐实例变化的属性用 GPU Instancing，不用 MaterialPropertyBlock
- Frame Debugger 是验证 CBuffer 合批状态的最直接工具
