---
title: "CPU 性能优化 06｜Unity 物理系统移动端优化：FixedTimestep 调参、碰撞矩阵裁剪与 Physics Profiler 解读"
slug: "cpu-opt-06-physics-optimization"
date: "2026-03-28"
description: "Unity 物理系统在移动端很容易被忽视，直到 CPU Profiler 里出现一条长长的 FixedUpdate 色块。本篇从 FixedTimestep 调参、Layer Collision Matrix 裁剪、碰撞体形状精简，到 Physics Profiler 的 Broad Phase 和 Contact 解读，给出移动端物理性能的完整优化路径。"
tags:
  - "Physics"
  - "CPU"
  - "Optimization"
  - "Unity"
  - "移动端"
series: "移动端硬件与优化"
weight: 2135
---

Unity 物理系统的性能问题有一个典型特征：**不是每帧都慢，而是特定帧出现尖峰**。这种尖峰在 Profiler 里表现为 `FixedUpdate.PhysicsFixedUpdate` 的周期性长条，而且往往与 FixedTimestep 和帧率的关系错误有直接联系。

---

## 一、移动端物理的成本来源

理解物理开销在哪里，才知道该优化哪里。Unity 的物理引擎（PhysX 3.x，或 Unity 2022+ 的 Unity Physics）每帧的计算分为四个阶段：

```
① Broad Phase（宽相检测）
   用 BVH（包围体层次结构）树做 AABB 重叠测试
   目的：快速排除绝对不可能碰撞的对象对
   成本：O(N log N)，N 为活跃的 Rigidbody 数量
   但 Layer Collision Matrix 可以跳过整个 Layer 对，大幅减少 N²

② Narrow Phase（窄相检测）
   对 Broad Phase 通过的候选对，进行精确接触计算（GJK / EPA 算法）
   成本：与接触对数量、碰撞体形状复杂度成正比
   MeshCollider（凹面）的代价远高于 Sphere / Capsule / Box

③ Solver（约束求解器）
   对所有接触点和关节约束，做迭代求解（默认 6 次速度迭代 + 1 次位置迭代）
   成本：与接触点数量 × 迭代次数成正比

④ FixedUpdate 回调
   你写的 FixedUpdate() 代码在这个阶段运行
   如果 FixedUpdate 里有重度逻辑（AI、寻路），开销计入物理步进时间
```

---

## 二、FixedTimestep：最容易被错误设置的参数

### 默认值的问题

Unity 的默认 `Fixed Timestep = 0.02`（50Hz）。大多数移动游戏的目标帧率是 30fps 或 60fps，与 50Hz 的物理步进不对齐。

**不对齐时发生什么**：

```
目标帧率 30fps → 渲染帧周期 ≈ 33.3ms
物理步进 50Hz → 物理步进周期 20ms

Frame 1（33ms）：
  执行 1 次 FixedUpdate（20ms 已过）
  等待下一个 FixedUpdate（累积剩余 13ms）

Frame 2（33ms）：
  累积时间 = 13ms + 33ms = 46ms
  需要执行 2 次 FixedUpdate（46ms / 20ms = 2.3 → 执行 2 次）
  → 这一帧 CPU 比上一帧多做一倍物理计算 → 帧时间尖峰
```

这种"追赶"机制在 Profiler 里表现为周期性的 FixedUpdate 时间加倍。

### 正确配置

```csharp
// 方案一：在代码里动态对齐
// 在 Awake 或游戏启动时设置
void Awake()
{
    int targetFps = Application.platform == RuntimePlatform.Android ? 30 : 60;
    Application.targetFrameRate = targetFps;
    Time.fixedDeltaTime = 1f / targetFps;
}

// 方案二：Project Settings → Time
// Fixed Timestep：0.0333（30fps）或 0.0167（60fps）
```

### 限制追赶步数

即使步进对齐，偶发的长帧（GC、IO）仍然可能触发多步追赶。设置最大追赶限制：

```csharp
// Project Settings → Time → Maximum Allowed Timestep
// 设置为与 Fixed Timestep 相同（如 0.0333）
// 效果：最多追赶 1 步，防止因为一帧卡顿导致后续帧雪崩

// 代码方式
Time.maximumDeltaTime = Time.fixedDeltaTime; // Unity 2021.2+

// 更激进：完全禁止追赶（物理时间不追赶真实时间）
// 适合对物理精度要求不高的游戏（如卡牌、消除类）
Time.maximumDeltaTime = Time.fixedDeltaTime;
Physics.defaultMaximumSimulationStepsPerFrame = 1; // Unity 2022.2+
```

---

## 三、Layer Collision Matrix：Broad Phase 最有效的裁剪手段

Layer Collision Matrix 决定哪些 Layer 对之间进行碰撞检测。默认所有层都互相检测，这在游戏对象多的场景里会造成巨大浪费。

### 理解 Broad Phase 的复杂度

```
N 个 Rigidbody，全部互相检测 → O(N²) 个候选对
  100 个对象 → ~5000 个候选对
  500 个对象 → ~125000 个候选对

Layer Collision Matrix 裁剪后：
  对于两个关闭碰撞的 Layer，整个 Layer 对被跳过
  → 不需要逐对检测
  → 实际复杂度：O(k × N²)，k 是开启碰撞的 Layer 对占比
```

### 实际配置

```
典型游戏的 Layer 结构：
  Player (0)
  Enemy (1)
  Projectile (2)
  Environment (3)
  FX (4)
  UI (5)

需要检测的碰撞对：
  Player vs Enemy ✅
  Player vs Environment ✅
  Player vs Projectile ✅（被射中）
  Enemy vs Environment ✅
  Projectile vs Enemy ✅
  Projectile vs Environment ✅（撞墙消失）

不需要检测的碰撞对（关闭）：
  Enemy vs Enemy ❌（敌人互相穿透）
  Projectile vs Projectile ❌（子弹不碰子弹）
  FX vs 任何 ❌（特效粒子不需要物理碰撞）
  UI vs 任何 ❌

关闭 6 个无用对，在 100 个对象的场景下
Broad Phase 候选对约减少 50-70%
```

**Project Settings → Physics → Layer Collision Matrix** 中取消勾选不需要的 Layer 对。

### 用代码验证配置

```csharp
// 调试：打印所有当前开启碰撞的 Layer 对
[ContextMenu("Print Active Collision Pairs")]
void PrintActiveCollisionPairs()
{
    var sb = new System.Text.StringBuilder();
    int activePairs = 0;

    for (int i = 0; i < 32; i++)
    {
        for (int j = i; j < 32; j++)
        {
            if (!Physics.GetIgnoreLayerCollision(i, j))
            {
                string layerA = LayerMask.LayerToName(i);
                string layerB = LayerMask.LayerToName(j);
                if (!string.IsNullOrEmpty(layerA) && !string.IsNullOrEmpty(layerB))
                {
                    sb.AppendLine($"  {layerA} ↔ {layerB}");
                    activePairs++;
                }
            }
        }
    }
    Debug.Log($"Active collision pairs: {activePairs}\n{sb}");
}
```

---

## 四、碰撞体形状优化

Narrow Phase 的成本与碰撞体形状的复杂度直接相关。

### 形状成本排序（从低到高）

| 碰撞体类型 | 相对成本 | 适用场景 |
|----------|---------|---------|
| SphereCollider | 1x | 子弹、小道具、圆形角色 |
| CapsuleCollider | 1.5x | 人形角色（强烈推荐） |
| BoxCollider | 2x | 矩形建筑、平台 |
| Convex MeshCollider | 5-10x | 复杂凸形动态对象 |
| MeshCollider（非凸，静态） | 高，但只参与静态 BVH | 地形、复杂静态场景 |
| MeshCollider（非凸，动态）| 极高，禁止使用 | **不应在任何动态 Rigidbody 上使用** |

### 实际建议

```
人形角色（玩家、NPC、Boss）：
  用 CapsuleCollider（主碰撞）
  如果需要精确击打判定：在骨骼上挂多个 BoxCollider（Hit Zone）
  → 不要用 SkinnedMeshCollider 或 MeshCollider

地面和环境（静态）：
  MeshCollider 可以接受，前提是：
  ① isKinematic = true（或纯静态，不挂 Rigidbody）
  ② 勾选 Bake Mesh（让 Unity 提前 bake 碰撞 BVH）

子弹、投射物：
  SphereCollider（直径设为弹体大小）
  如果需要穿透效果：用 Physics.Raycast 或 SphereCast，而不是物理碰撞器

可破坏物件：
  初始状态：Kinematic，无 Rigidbody（完全不参与物理 Broad Phase）
  被击打时：切换为 Rigidbody，用几个 BoxCollider 近似
  破碎后（不再需要物理）：Destroy 或 SetActive(false)
```

### 减少常驻 Trigger 的数量

大量常驻的 Trigger 碰撞体（用于范围检测）是 Broad Phase 的隐形消耗：

```csharp
// ❌ 不推荐：每个可交互对象都有一个 SphereCollider Trigger
// 100 个 NPC × 1 个触发器 = 100 个 Broad Phase 参与者

// ✅ 推荐：需要时做 OverlapSphere，而不是常驻 Trigger
void CheckNearbyEnemies()
{
    // 只在需要时执行，且不增加物理世界的 Rigidbody 数量
    Collider[] nearby = Physics.OverlapSphere(
        transform.position,
        detectionRadius,
        enemyLayerMask
    );

    foreach (var col in nearby)
    {
        // 处理检测到的敌人
    }
}

// 如果每帧都需要，配合 InvokeRepeating 降低频率
void Start() {
    InvokeRepeating(nameof(CheckNearbyEnemies), 0f, 0.1f); // 10 Hz 而不是 60 Hz
}
```

---

## 五、Rigidbody Sleeping 机制

PhysX 对静止的 Rigidbody 会自动进入 Sleep 状态，Sleep 中的 Rigidbody 几乎不参与任何物理计算。

### 睡眠阈值调整

```
Project Settings → Physics → Sleep Threshold（默认 0.005）
  含义：速度低于此值的 Rigidbody 视为静止，进入睡眠
  
  调整建议：
  可以提高到 0.01 或 0.02（大多数游戏中不会有明显的视觉差异）
  → 让对象更快进入睡眠 → 减少 Awake Rigidbody 数量
```

```csharp
// 主动让不需要运动的对象睡眠
rigidbody.Sleep();

// 查看当前有多少 Rigidbody 处于 Awake 状态（调试诊断用）
void LogAwakeRigidbodies()
{
    var rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
    int awake = 0, sleeping = 0;
    foreach (var rb in rbs) {
        if (rb.IsSleeping()) sleeping++; else awake++;
    }
    Debug.Log($"Rigidbody: {awake} awake / {sleeping} sleeping / {rbs.Length} total");
}
```

**设计建议**：对于"可破坏物件"（箱子、罐子等），出生时设置 `isKinematic = true`（完全不进入物理世界），被击打时才切换为 Active Rigidbody + Wake Up。这比 Sleeping 机制节省更多——Kinematic 对象不参与 Broad Phase。

---

## 六、Physics Profiler 解读

Unity 2021+ 提供了专用的 Physics Profiler 模块（Window → Analysis → Profiler → Physics 模块）。

### 关键指标

```
Broad Phase（宽相）：
  Bodies           → 参与 Broad Phase 的 Rigidbody 总数
  Overlapping AABB → Broad Phase 通过的候选碰撞对数
  → 这个数越小越好。如果 > 500 对，检查 Layer Matrix 是否裁剪

Narrow Phase（窄相）：
  Active Contacts  → 当前帧的实际接触点数
  → > 200 时开始影响 Solver 性能
  → 密集堆叠的物件（大量箱子叠在一起）会爆炸式增加接触点

Solver（约束求解）：
  Position Iterations / Velocity Iterations → 求解迭代次数
  可以在 Project Settings → Physics 里调低（移动端建议 Velocity=4, Position=1）
```

### 在 CPU Profiler 里识别物理开销

在 CPU Profiler 的时间线里，找以下 Sample：

```
FixedBehaviourUpdate          → 你的 FixedUpdate 代码
Physics.Processing            → PhysX 物理步进（Broad + Narrow + Solver）
Physics.UpdateBodies          → 将物理结果写回 Transform
Physics.Interpolation         → 物理插值（开启时）
Rigidbody.MovePosition/Rotation → Kinematic 移动调用

诊断结论：
  Physics.Processing 占主导 → 碰撞体太复杂或接触点太多
  FixedBehaviourUpdate 占主导 → 你的 FixedUpdate 代码太重，与物理无关
  Physics.UpdateBodies 占主导 → Transform 数量太多（罕见）
```

---

## 七、2D 物理（Physics2D / Box2D）

Unity 的 2D 物理使用 Box2D，与 PhysX 是独立系统，但优化原则完全相同：

```
Layer Collision Matrix（Physics2D 有独立设置）：
  Project Settings → Physics 2D → Layer Collision Matrix
  → 同样裁剪不需要的 Layer 对

碰撞体形状（2D 版本）：
  CircleCollider2D   → 最便宜
  CapsuleCollider2D  → 次之
  BoxCollider2D      → 普通
  PolygonCollider2D  → 顶点越少越好，避免自动生成的高精度 Polygon
  CompositeCollider2D → 将多个 Collider 合并为单个形状，减少碰撞对数

FixedTimestep：同样需要与目标帧率对齐
  2D 物理和 3D 物理共享同一个 FixedTimestep
```

---

## 八、移动端物理优化清单

| 优化项 | 默认状态 | 推荐配置 | 主要收益 |
|------|--------|--------|---------|
| FixedTimestep 对齐 | 0.02（50Hz） | 1/targetFps（30 或 60Hz） | 消除多步追赶尖峰 |
| 最大追赶步数 | 无限制 | 1 步 | 防止卡顿后雪崩 |
| Layer Collision Matrix | 全部开启 | 按需裁剪（关闭 50-80%） | Broad Phase -50% 以上 |
| 动态对象碰撞体 | 可能有 MeshCollider | 只用 Sphere / Capsule / Box | Narrow Phase 大幅降低 |
| 常驻 Trigger 数量 | 每对象一个 | 改为 OverlapSphere 按需检测 | Broad Phase 参与者减少 |
| Sleep Threshold | 0.005 | 0.01-0.02 | 减少 Awake Rigidbody |
| Solver 迭代次数 | Velocity=6, Position=1 | Velocity=4（一般游戏） | Solver 时间 -30% |
| 不活跃物件 | Sleeping Rigidbody | Kinematic 或 Disabled | 完全不参与 Broad Phase |
