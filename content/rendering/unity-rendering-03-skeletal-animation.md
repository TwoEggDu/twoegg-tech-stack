---
date: "2026-03-24"
title: "Unity 渲染系统 03｜动画变形：骨骼蒙皮与 Blend Shape 怎么改变渲染结果"
description: "区分动画求值、姿态写回与蒙皮，用对象空间的加权变换和教学平移例解释顶点位置，并按 Unity 6.0 公开文档核对 CPU/GPU 执行边界。"
slug: "unity-rendering-03-skeletal-animation"
weight: 600
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "Animation"
  - "SkinnedMesh"
  - "BlendShape"
  - "Skinning"
  - "GPU"
series: "Unity 渲染系统"
---
> 本文关注角色变形：动画系统决定姿态与属性，蒙皮和 Blend Shape 据此影响顶点及后续画面。何时更新、在哪执行，以及是否复用中间结果，需要分别核对。

前面几篇讲的都是"静止的表面"——Mesh 顶点固定，Material 决定颜色，光照影响明暗。但游戏里大量物体是运动的：角色在奔跑，衣物在飘动，面部在说话。

这篇讲动画是怎么介入渲染管线的。

> **版本与证据范围**：Unity 映射按 6.0（6000.0）公开文档核对；下文位置公式、伪代码和纯平移数值是教学模型，不是 Unity 内部源码或设备实测。具体补丁、平台、图形 API 和配置仍须在实际验证时固定。制作、混合与打断的正确性前置复用[动画制作与接入]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})。

<a id="animation-evaluation-boundaries"></a>

## 从动作片段到顶点，先分清五种职责

角色动作不对或更新较慢时，先确定问题出在哪个输出边界。下面是职责关系，不是一条可以相加为总耗时的串行时间线；根运动、事件和其他属性还有自己的交付路径。

```text
状态/参数 → 选择并采样片段 → 混合、IK 等姿态处理 → 写回变换/属性
                                └→ 根运动、事件等相关输出另行关联
骨骼变换 + 绑定数据 + 网格/权重 → 蒙皮位置 → 参与主画面、阴影等绘制用途
```

| 职责 | 输入与要确认的输出 | 不足以代替什么 |
| --- | --- | --- |
| 状态推进 | 参数、状态机/过渡与当前动作实例，决定参与求值的内容 | 状态切换成功不证明姿态已写回或画面已显示 |
| 曲线求值 | 在片段的播放时间读取变换及其他属性，关联片段/时间与事件范围 | 源动作的采样率不等于本次运行求值频率 |
| 混合与姿态处理 | 按活动片段、层/遮罩及 IK 等条件处理姿态；相关回调可能提供输入 | 一个片段的姿势不能代表完整混合结果；IK 回调时间不等于全部求解成本 |
| 写回 | 将内部姿态写到目标变换层级，其他可动画属性另写 | 内部姿态结果与场景对象、挂点、后续使用者不是同一个观测终点 |
| 蒙皮与绘制消费 | 骨骼变换、绑定矩阵和权重生成变形位置，再供实际绘制路径使用 | 片段采样时间不含全部蒙皮/绘制成本；角色父区间与动画子项也不能重复相加 |

Unity 6.0 的[动画更新顺序](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/managing-update-order/execution-order)与[动画 Profiler 标记](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/markers#animation-markers)分别给出回调关系和内部职责：例如状态机准备、`ProcessGraph` 的属性求值/根运动/事件收集、`ProcessAnimations` 的混合，以及 `WriteTransforms`/`WriteProperties` 的写回。这些名称是调查入口，不是已适配采集 API。

同一页还区分调度/等待标记与工作任务，并说明某些状态机回调会限制主线程执行、写回任务按变换根分组，以及后续阶段受裁剪模式和可见性影响。因此不能把某个父标记、回调或单一线程条带叫作“这个角色全部动画耗时”，也不能从离屏直接推出全部状态与动画工作停止。后续应关联动作实例、Animator、变换根、运行/帧、实际配置和各层区间，再按[任务观测方法]({{< relref "performance/client-performance-task-observation-model.md" >}})处理包含、重叠和缺失。

---

## 静态 Mesh 和蒙皮 Mesh 的区别

在本例中，**MeshRenderer + MeshFilter** 使用的网格对象坐标保持不变，对象仍可通过 Transform 移动；对象矩阵把这些坐标变到世界空间。它不要求每帧重写资产中的顶点。

**SkinnedMeshRenderer** 则能根据骨骼/权重等输入产生变形位置。变形输出不等于原始 Mesh 资产每帧被改写；是否执行及中间结果放在哪里，要看具体路径。

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

每块骨骼都有自己的局部变换（相对于父骨骼的位置/旋转/缩放）。动画求值和写回可以改变它们，具体更新范围与时机按运行设置和实际记录确认。

### 蒙皮权重（Skinning Weights）

下面采用非负且归一化的权重描述一个顶点，权重之和为 1，表示位置由各影响骨骼贡献多少；参与项数是模型输入。它不是“Unity 资产只能有 1～4 个影响”的支持上限：

```
顶点（大腿侧面）：
  骨骼 LeftUpLeg，权重 0.7
  骨骼 Hips，      权重 0.3
```

这描述了两份变换的加权贡献；实际形变是否出现折叠，仍需检查绑定姿态、权重、动作和算法，不能由比例本身保证。

Unity 6.0 的[Skinned Mesh Renderer 说明](https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/asset-types/mesh/components-reference/class-skinned-mesh-renderer)区分导入的影响数与运行时 Quality 限制，并给出允许超过四个影响的配置条件；[GetBonesPerVertex](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh/getbonespervertex)描述 Mesh 每顶点非零权重的数量。源数据、导入后保留项和运行时实际使用项应分别记录，不把资产计数当作已执行工作。

### AnimationClip

AnimationClip 可以提供骨骼变换及其他可动画属性的曲线；播放时间决定读取哪个位置，之后还可能有混合、姿态处理和写回。

不能把“某片段采到了一个姿势”直接当作完整角色层级已更新。制作曲线、片段和运行状态的区别沿前面的动画制作文复用。

---

## 蒙皮的计算过程

<a id="skinning-position-model"></a>

### 先确定位置在哪个坐标空间

这里用**线性混合蒙皮的位置模型**解释变形，不还原 Unity 内部实现。采用列向量和齐次位置坐标，矩阵从右向左作用；所有输入必须属于同一套绑定关系。

| 符号 | 含义 |
| --- | --- |
| `M0`、`B_i0` | 绑定时网格对象、骨骼 i 各自从局部到世界的变换 |
| `Q_i = inverse(B_i0) × M0` | 从绑定网格空间到该骨骼绑定局部空间的变换 |
| `M(t)`、`B_i(t)` | 当前网格对象、骨骼 i 各自从局部到世界的变换 |
| `S_i(t) = inverse(M(t)) × B_i(t) × Q_i` | 将绑定网格位置变到当前网格对象空间的蒙皮矩阵 |

Unity 6.0 的 [Mesh.bindposes](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh/bindposes) 例子使用 `bones[i].worldToLocalMatrix * transform.localToWorldMatrix` 建立绑定关系，且 bind pose 与同索引的骨骼对应。因此不能忽略网格的参考变换，把它无条件简写为骨骼世界矩阵的逆；绑定姿态也不必是 T-Pose。

对某顶点的实际参与项，以非负且和为 1 的权重求和：

```text
p_mesh(t)  = Σ_i w_i × S_i(t) × p_mesh(bind)
p_world(t) = M(t) × p_mesh(t)
```

等价地，`Σ_i w_i × B_i(t) × Q_i × p_mesh(bind)` 已经给出世界位置。后续若还需要对象空间位置，就要先转换，不能再无条件乘一次对象矩阵。

下面只表达位置计算，`influenceCount` 是本顶点参与项数，不固定为四项。这是自写教学伪代码，不是可直接运行的 Shader，也不指定 CPU、Compute 或顶点阶段：

```text
positionMesh = (0, 0, 0, 0)
for k in 0 .. influenceCount-1:
    i = boneIndex[k]
    S = inverse(M_current) * B_current[i] * Q[i]
    positionMesh += weight[k] * (S * positionBind)
positionWorld = M_current * positionMesh
```

法线和切线还涉及方向、归一化、缩放及具体算法的处理，不能把这个位置模型直接当成它们的实现证明。

### 教学复算：为什么世界坐标不能再加一次对象位移

以下数值全部虚构，只保留 x 方向平移，旋转/缩放为单位变换；单骨骼影响，权重为 1，不是设备测量。

| 输入或中间量 | 平移量或位置 | 解释 |
| --- | --- | --- |
| 绑定网格 `M0`、骨骼 `B0` | 10、12 | 二者均相对世界 |
| 绑定关系 `Q` | -2 | `-12 + 10` |
| 当前网格 `M`、骨骼 `B` | 20、23 | 二者均相对世界 |
| 对象空间蒙皮变换 `S` | 1 | `-20 + 23 - 2` |
| 绑定网格位置 → 当前网格位置 | 3 → 4 | `3 + 1` |
| 当前世界位置 | 24 | `20 + 4`，也等于 `23 - 2 + 3` |
| 重复对象变换的错误结果 | 44 | 将世界位置 24 错当对象位置，再加 20 |

这个例子能检查空间是否混用，不能回答曲线求值花多久、蒙皮在哪执行，或某个权重方案是否有足够好的形变质量。

### 执行设备和 Shader 阶段分别核对

同一位置关系可以由不同执行方式实现。CPU 计算、Compute 先产出变形缓冲、在绘制的顶点阶段计算，是不同的组织方式；它们的准备、数据传输、结果复用和每次绘制工作需要分别观察，不能单凭“GPU”二字互相替换。

就本篇固定的 Unity 6.0 文档而言，[PlayerSettings.meshDeformation](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/playersettings/meshdeformation) 明确区分 CPU、GPU 与 GPUBatched：CPU 在 CPU 处理；后两者用 Compute Shader，GPUBatched 将多个网格/Blend Shape 的处理合批。旧 [gpuSkinning](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/playersettings/gpuskinning) 已标注未来弃用，`false` 对应 CPU，`true` 对应 GPUBatched。因此开启旧开关并不能证明蒙皮在当前 Draw 的 Vertex Shader 里完成。

这说明要查哪些执行与资源边界，不保证所有目标环境的可用性或收益。选路径前，先固定补丁、构建、平台、图形 API、实际配置及角色/用途，再核对支持情况和实际捕获；单个开关或角色数量不足以作出性能结论。

---

## 蒙皮对渲染结果的影响

变形位置会影响后续绘制，但不预设每帧都更新或固定产出一份新顶点缓冲。按实际执行路径，需把产生位置的工作与消费它的用途关联起来：

```
变形位置（由实际蒙皮路径提供）
    → 当前用途的顶点处理与坐标变换
    → 光栅化（三角面覆盖像素，插值 UV/法线）
    → Fragment Shader（采样贴图，PBR 计算）
```

所以动画改变的是：
- **哪些像素被覆盖**：腿抬起来后，覆盖的屏幕像素区域变了
- **每个像素的法线方向**：肌肉隆起时，光照方向感随之变化
- **UV 的分布**（通常不变，UV 跟随顶点变形）

这个位置模型没有修改 Material 参数、Texture 内容或光照资产；动画系统仍可以通过其他属性曲线改变参数，不能据此概括为动画不会影响它们。

---

## Blend Shape（形态键）

### 什么是 Blend Shape

Blend Shape 是另一种顶点变形机制，原理比骨骼蒙皮更直接：

**直接存储顶点的目标偏移量**——对于每个顶点，记录"从默认形态变换到目标形态时，这个顶点需要移动多少"（deltaPosition、deltaNormal、deltaTangent）。

为解释偏移，下面只用单目标、归一化为 0～1 的权重作教学插值；这不是 Unity API 权重单位或支持范围的定义，实际形态帧和属性处理须另核：

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
| **数据规模观察入口** | 姿态曲线、绑定/权重与运行数据分开 | 形态/帧、受影响顶点、属性流与运行数据分开 |
| **动态混合** | AnimationClip 驱动 | 任意权重随时可调 |

面部动画通常**同时使用两者**：骨骼控制大范围的颌骨、眼球运动，Blend Shape 精细控制嘴角、眉毛、眼皮的微表情——骨骼提供运动框架，Blend Shape 提供细节精度。

---

## 蒙皮性能开销在哪里

**顶点与参与项**：在上述逐项位置模型里，可以用各顶点实际参与项数之和描述工作规模；每顶点项数相同才简写为顶点数 × 项数。它不是耗时公式，不能据比例线性外推毫秒；实际裁剪、执行方式、结果复用与绘制用途还需核对。

**骨骼与姿态**：骨架节点数、网格绑定骨骼数与每顶点影响数是不同输入。状态、混合/IK、写回、矩阵准备与蒙皮各有工作边界；这里不给跨平台通用骨骼上限，也不以一份矩阵数组大小代替整条链的成本。

**Blend Shape 内存**：要区分形态/帧、受影响顶点、属性流、导入设置与运行分配。不能从 `Legacy Blend Shape Normals` 名称推出只存 Position，或承诺节省 2/3。后续须核对实际版本与产物；前面的简化插值与对比表不作为实际字节公式。

**绘制用途**：蒙皮方式不代替 Renderer、材质及主画面/阴影等用途的核对。变形处理合批也不等于绘制自动合并；可复用结果、实际绘制次数及数据传输，需在选定管线和环境中分别观察。

---

## 后续怎样观察蒙皮结果

如果角色某个部位的形变不对，可以按下面的顺序设计捕获。这里没有执行 RenderDoc，也没有核验目标图形 API 的捕获支持：

1. 固定运行/帧、角色、Renderer、用途/Pass 与实际配置，找到对应绘制事件，并记录当前姿态写回和绑定输入；仅凭外观不能排除上游求值错误。
2. 查当前绘制的输入缓冲、属性语义与 Shader 实际处理。若前面有 Compute，沿资源写入/读取关系核对是否为本次蒙皮输出；输入已变形可能来自前置 GPU 工作，不能据此判定为 CPU 蒙皮。
3. 在工具支持时比较 Mesh Viewer 的 VS Input/Output，并确认各自空间和实际 Shader 输出。对象/世界/裁剪位置不是同一量；顶点输出也不足以证明片段、阴影及最终画面都正确。

**理解检查**：平移例得到 24 后为什么不能再加 20？若只改变当前网格对象变换，哪些输入与空间关系要重查？若切换运行蒙皮方式，哪些数学关系仍可复用，哪些缓冲/执行证据必须重新取得？为什么角色父区间、动画调度/等待和跨线程子任务不能直接相加？

---

## 和下一篇的关系

骨骼动画和 Blend Shape 处理的是"形状变形"。另一类几何与模拟数据随发射和生命周期变化的对象是粒子系统；它的模拟方式、渲染模式及执行位置另需区分。下一篇讲粒子与特效的渲染机制。
