+++
title = "Unity 渲染系统 03｜动画变形：骨骼蒙皮与 Blend Shape 怎么改变渲染结果"
description = "讲清楚骨骼动画的蒙皮原理（骨骼权重如何在顶点阶段混合多个变换矩阵）、Blend Shape 的顶点偏移机制，以及两者如何改变最终覆盖的像素范围和法线朝向。"
slug = "unity-rendering-03-skeletal-animation"
weight = 600
featured = false
tags = ["Unity", "Rendering", "Animation", "SkinnedMesh", "BlendShape", "Skinning", "GPU"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：骨骼动画和 Blend Shape 改变的不是"播放效果"，而是送进光栅化的顶点坐标本身——动画本质上是每帧重新计算一遍顶点位置，再走和静态 Mesh 完全相同的渲染路径。

前面几篇讲的都是"静止的表面"——Mesh 顶点固定，Material 决定颜色，光照影响明暗。但游戏里大量物体是运动的：角色在奔跑，衣物在飘动，面部在说话。

这篇讲动画是怎么介入渲染管线的。

---

## 静态 Mesh 和蒙皮 Mesh 的区别

静态物体用 **MeshRenderer + MeshFilter** 组件——Mesh 数据固定，每帧只需要更新 Transform 矩阵（位移/旋转/缩放），Vertex Shader 用一个 Model Matrix 把所有顶点变换到世界空间。

动画物体用 **SkinnedMeshRenderer**——Mesh 数据本身每帧都在变化，每个顶点的位置是动态计算出来的，不能用单一的 Model Matrix 描述。

这个计算过程叫做**蒙皮（Skinning）**。

---

## 骨骼动画的数据结构

### 骨骼层级（Skeleton / Rig）

骨骼是一组有层级关系的变换节点（Transform），类似 Scene 里的 GameObject 层级：

```
Hips（根骨骼）
  ├─ Spine
  │    ├─ Chest
  │    │    ├─ Neck
  │    │    │    └─ Head
  │    │    ├─ LeftShoulder
  │    │    │    └─ LeftArm → LeftForeArm → LeftHand
  │    │    └─ RightShoulder
  │    │         └─ RightArm → RightForeArm → RightHand
  └─ LeftUpLeg → LeftLeg → LeftFoot
  └─ RightUpLeg → RightLeg → RightFoot
```

每块骨骼都有自己的局部变换（相对于父骨骼的位置/旋转/缩放）。动画播放时，骨骼的变换在每帧更新。

### 蒙皮权重（Skinning Weights）

Mesh 里每个顶点绑定了 1～4 块骨骼，以及对应的权重。权重之和为 1，表示这个顶点的最终位置"由这几块骨骼各自贡献多少比例"：

```
顶点（大腿侧面）：
  骨骼 LeftUpLeg，权重 0.7
  骨骼 Hips，      权重 0.3
```

这意味着这个顶点随大腿骨骼运动 70%，随髋部骨骼运动 30%——这样大腿弯曲时，过渡区域的皮肤不会出现尖锐的折叠。

### AnimationClip

AnimationClip 存储的是每块骨骼的变换曲线——在时间轴上，每个关键帧记录骨骼的 Rotation / Position / Scale 值，帧间用曲线插值。

播放动画时，AnimationClip 按时间采样曲线，得到当前帧每块骨骼的变换值，更新骨骼层级。

---

## 蒙皮的计算过程

骨骼变换更新之后，要把这些变换应用到顶点位置上。这就是蒙皮计算：

### 第一步：计算每块骨骼的蒙皮矩阵

每块骨骼的**蒙皮矩阵（Skinning Matrix）**是两个矩阵的乘积：

```
蒙皮矩阵 = 当前骨骼世界变换矩阵 × 骨骼绑定姿态逆矩阵
```

- **当前骨骼世界变换矩阵**：动画播放时，这块骨骼现在在世界空间的位置/旋转
- **骨骼绑定姿态逆矩阵（Inverse Bind Pose Matrix）**：模型导入时记录的"T-Pose 时骨骼的世界变换的逆矩阵"

两者相乘，得到的是"从 T-Pose 到当前动画姿态的变换差量"。对顶点应用这个矩阵，就相当于"把顶点从 T-Pose 位置搬到动画当前帧对应的位置"。

### 第二步：混合多块骨骼的贡献

对每个顶点，用权重混合最多 4 块骨骼的蒙皮矩阵，得到这个顶点最终的变换矩阵：

```
最终变换矩阵 = 权重0 × 蒙皮矩阵0
             + 权重1 × 蒙皮矩阵1
             + 权重2 × 蒙皮矩阵2
             + 权重3 × 蒙皮矩阵3
```

再用这个矩阵变换顶点的 Position 和 Normal：

```
蒙皮后位置 = 最终变换矩阵 × 原始 Position
蒙皮后法线 = 最终变换矩阵（法线变换版本）× 原始 Normal
```

**法线也要跟着变换**，这一点很重要——如果只变换位置，不更新法线，大腿弯曲后光照方向感会完全错误。

### GPU Skinning Vertex Shader 完整伪代码

把上面两步翻译成 GPU 侧的 Vertex Shader，就是现代引擎 GPU Skinning 的核心代码：

```hlsl
// 蒙皮矩阵数组：CPU 每帧计算并上传到 GPU CBuffer
// 每块骨骼一个 float4x4，共 MAX_BONE_COUNT 个
// 每个矩阵已经是"当前帧世界变换 × 绑定姿态逆矩阵"的结果
float4x4 _SkinningMatrices[MAX_BONE_COUNT];

// 顶点输入：除了普通的 Position/Normal，还带骨骼绑定数据
struct VertexInput {
    float3 positionOS   : POSITION;       // 绑定姿态（T-Pose）下的模型空间位置
    float3 normalOS     : NORMAL;         // 绑定姿态下的法线
    float4 boneWeights  : BLENDWEIGHTS;   // 4 块骨骼的权重（和为 1）
    uint4  boneIndices  : BLENDINDICES;   // 4 块骨骼在 _SkinningMatrices 里的索引
};

float4x4 ComputeBlendedSkinMatrix(uint4 idx, float4 w) {
    // 步骤一：用权重混合 4 块骨骼的蒙皮矩阵
    // 本质是 4 个 4×4 矩阵的加权平均
    return w.x * _SkinningMatrices[idx.x]
         + w.y * _SkinningMatrices[idx.y]
         + w.z * _SkinningMatrices[idx.z]
         + w.w * _SkinningMatrices[idx.w];
}

VertexOutput vert(VertexInput v) {
    // 步骤二：计算当前顶点的混合蒙皮矩阵
    float4x4 skinMatrix = ComputeBlendedSkinMatrix(v.boneIndices, v.boneWeights);

    // 步骤三：用蒙皮矩阵变换顶点位置（从 T-Pose 搬到当前动画姿态）
    float3 skinnedPositionOS = mul(skinMatrix, float4(v.positionOS, 1.0)).xyz;

    // 步骤四：变换法线
    // 严格来说应该用 (M^{-1})^T（逆转置），但对于只含旋转和均匀缩放的蒙皮矩阵
    // 直接取左上 3×3 即可（Unity 的实现也是这样）
    float3x3 rotMatrix = (float3x3)skinMatrix;
    float3 skinnedNormalOS = normalize(mul(rotMatrix, v.normalOS));

    // 步骤五：和普通静态 Mesh 完全相同的后续流程
    VertexOutput o;
    o.positionCS = mul(UNITY_MATRIX_VP,
                       mul(UNITY_MATRIX_M, float4(skinnedPositionOS, 1.0)));
    o.normalWS   = TransformObjectToWorldNormal(skinnedNormalOS);
    return o;
}
```

几个细节：

- `_SkinningMatrices` 由 CPU 每帧上传，大小 = 骨骼数 × 64 字节（一个 `float4x4`）。75 块骨骼 = 约 4.7 KB，很小
- `boneWeights.w` 的权重通常是 `1 - (x + y + z)`，保证四权重之和精确为 1
- 当 `boneWeights = (1, 0, 0, 0)` 时，等同于普通刚性绑定（该顶点只跟一块骨骼走），矩阵混合退化为单矩阵乘法

### CPU Skinning vs GPU Skinning

这个计算可以在 CPU 或 GPU 上执行：

**CPU Skinning**：所有蒙皮计算在 CPU 上完成，结果写入一个新的顶点缓冲，再作为普通 Mesh 提交 Draw Call。Unity 早期默认使用这种方式，适合骨骼数少的简单角色。

**GPU Skinning**：把蒙皮矩阵数组上传到 GPU，在 Vertex Shader 里执行蒙皮计算。大量角色同时动画时性能更好，是现代项目的主流方式（Unity 的 `GPU Skinning` 选项在 Player Settings 里开启）。

**Compute Shader Skinning**：更进一步，用 Compute Shader 并行计算蒙皮结果，写入共享缓冲区，适合场景里有几百个动态角色的情况（如 GPU Crowd 方案）。

---

## 蒙皮对渲染结果的影响

蒙皮完成后，SkinnedMeshRenderer 拿到的是**每帧更新的顶点缓冲**。这份数据随后和普通 Mesh 完全一样地进入渲染管线：

```
蒙皮后的顶点缓冲
    → Vertex Shader（MVP 变换）
    → 光栅化（三角面覆盖像素，插值 UV/法线）
    → Fragment Shader（采样贴图，PBR 计算）
```

所以动画改变的是：
- **哪些像素被覆盖**：腿抬起来后，覆盖的屏幕像素区域变了
- **每个像素的法线方向**：肌肉隆起时，光照方向感随之变化
- **UV 的分布**（通常不变，UV 跟随顶点变形）

不影响的是：Material 参数、Texture 内容、光照资产——这些在蒙皮完成后才进入计算。

---

## Blend Shape（形态键）

### 什么是 Blend Shape

Blend Shape 是另一种顶点变形机制，原理比骨骼蒙皮更直接：

**直接存储顶点的目标偏移量**——对于每个顶点，记录"从默认形态变换到目标形态时，这个顶点需要移动多少"（deltaPosition、deltaNormal、deltaTangent）。

运行时，用一个 0～1 的权重值对默认形态和目标形态进行线性插值：

```
最终顶点位置 = 默认位置 + weight × deltaPosition
最终法线方向 = 默认法线 + weight × deltaNormal（归一化后）
```

weight = 0：完全是默认形态
weight = 1：完全是目标形态
weight = 0.5：两个形态各占一半（嘴巴半张）

### 多个 Blend Shape 的叠加

一个 Mesh 可以有几十个 Blend Shape，分别对应不同的表情或变形：

```
BlendShape_Smile      weight = 0.8   ← 微笑 80%
BlendShape_BrowRaise  weight = 0.3   ← 眉毛上扬 30%
BlendShape_EyeBlink_L weight = 1.0   ← 左眼完全闭合
BlendShape_EyeBlink_R weight = 0.0   ← 右眼完全睁开
```

多个 Blend Shape 的 delta 相加，再叠加到默认位置上：

```
最终位置 = 默认位置
         + 0.8 × deltaPos_Smile
         + 0.3 × deltaPos_BrowRaise
         + 1.0 × deltaPos_EyeBlink_L
         + 0.0 × deltaPos_EyeBlink_R
```

### Blend Shape 和骨骼动画的对比

| | 骨骼动画 | Blend Shape |
|---|---|---|
| **数据** | 骨骼变换矩阵 + 权重 | 顶点偏移量 |
| **适合** | 四肢运动、身体动作 | 面部表情、肌肉细节 |
| **精度** | 受骨骼数量和权重分配影响 | 每个顶点独立控制，精度高 |
| **内存** | 骨骼数 × 关键帧数 | 顶点数 × Blend Shape 数 × 3（位置/法线/切线） |
| **动态混合** | AnimationClip 驱动 | 任意权重随时可调 |

面部动画通常**同时使用两者**：骨骼控制大范围的颌骨、眼球运动，Blend Shape 精细控制嘴角、眉毛、眼皮的微表情——骨骼提供运动框架，Blend Shape 提供细节精度。

---

## 蒙皮性能开销在哪里

**顶点数量**：蒙皮计算量正比于顶点数 × 骨骼影响数（通常 1～4）。高精度角色模型（10 万顶点以上）在 CPU 蒙皮时开销显著。

**骨骼数量**：每块骨骼需要计算一次蒙皮矩阵。Unity 默认支持最多 75 块骨骼影响一个 Mesh（可配置）。

**Blend Shape 内存**：每个 Blend Shape 需要存储所有顶点的偏移量，面部高精度模型（5 万顶点）× 50 个表情 = 大量内存。Unity 的 `Legacy Blend Shape Normals` 选项可以只存 Position 偏移，不存 Normal/Tangent 偏移，节省约 2/3 的内存（代价是法线不随形态变化）。

**Draw Call 合批限制**：SkinnedMeshRenderer 的 Draw Call 通常无法参与 Static Batching 和常规的 GPU Instancing（骨骼数据的差异性阻止了合并）。场景里有大量动画角色时，需要专门的 GPU Crowd 方案。

---

## 在 RenderDoc 里验证蒙皮结果

如果角色某个部位的动画形变不对（穿模、奇怪的扭曲），可以用 RenderDoc 的 Mesh Viewer 验证：

1. 捕获帧，找到角色的 SkinnedMeshRenderer Draw Call
2. 切到 Mesh Viewer → **VS Input**：看到的是蒙皮计算完成后的顶点数据（GPU Skinning 时，蒙皮在 Vertex Shader 里完成，VS Input 里看到的是原始绑定姿态；CPU Skinning 时，VS Input 已经是蒙皮后的坐标）
3. 切到 **VS Output**：MVP 变换后的裁剪空间坐标，用 3D 预览可以看到变换后的形状是否正确

---

## 和下一篇的关系

骨骼动画和 Blend Shape 处理的是"形状变形"。还有另一类特殊的几何体——粒子系统——它的几何形状不是预先存在的，而是在 CPU 上每帧动态生成的。下一篇讲粒子与特效的渲染机制。
