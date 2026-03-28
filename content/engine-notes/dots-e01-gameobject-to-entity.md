---
title: "Unity DOTS E01｜从 GameObject 到 Entity：数据模型的本质转变"
slug: "dots-e01-gameobject-to-entity"
date: "2026-03-28"
description: "ECS 不是「DOTS 好 OOP 坏」的风格之争，而是两套数据模型面对同一个问题时不同的权衡。本篇讲清楚 GameObject 模型的内存结构、ECS 的 Archetype/Chunk 布局，以及什么该留在 OOP、什么适合迁移到 ECS。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Entity"
  - "GameObject"
  - "Archetype"
  - "数据导向"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 1
weight: 1810
---

GameObject 模型被批评最多的不是"面向对象"这件事本身，而是它对内存的假设：每个对象自己管自己的数据，彼此独立分配，按需索取。这套假设在对象数量少时几乎感觉不到代价，但当场景里有 500 个 Enemy 同时需要更新位置时，CPU 的 cache 每秒要为此付出数百万次的等待周期。ECS 解决的正是这个问题——不是通过更好的算法，而是通过重新安排数据在内存中的物理位置。

---

## GameObject 的内存模型

Unity 的 **GameObject** 在堆上是一个引用类型对象，它持有一个 Component 列表，每个 Component 同样是独立分配在堆上的引用类型实例。一个 EnemyController MonoBehaviour 里有 `transform.position`、`rigidbody`、`health` 等字段，但这些字段指向的对象可能分散在堆的不同区域。

500 个 Enemy 在内存里的实际样子：

```
堆内存（示意，地址不连续）

0x1A20  [Enemy_000: GameObject]  --> 0x3F10 [Transform]  --> 0x7C44 [position: float3]
                                 --> 0x5B20 [Rigidbody]
                                 --> 0x2D80 [EnemyCtrl]  --> 0x9E30 [health: float]

0x1B60  [Enemy_001: GameObject]  --> 0x4102 [Transform]  --> 0x8F12 [position: float3]
                                 --> 0x6C44 [Rigidbody]
                                 --> 0x3A10 [EnemyCtrl]  --> 0xA210 [health: float]

0x2C90  [Enemy_002: GameObject]  --> ...（地址跳跃，与 Enemy_000 的 Transform 不相邻）

...（500 个 Enemy，每个的数据各自散落在堆的不同角落）
```

当你在 `Update` 里写 `enemy.transform.position += velocity * dt` 时，CPU 实际的执行路径是：

1. 从 `enemy` 引用跳转到 GameObject 对象（一次指针解引用）
2. 从 GameObject 的 component list 找到 Transform（可能触发一次 **cache miss**）
3. 读取 Transform 的 `position` 字段（可能再次 cache miss，因为 Transform 和 GameObject 不相邻）
4. 写入新值，继续处理 Enemy_001——它的 Transform 地址与 Enemy_000 的 Transform 毫无关联

现代 CPU 的 L1 cache 大小约 32KB，cache line 为 64 bytes。当数据分散在堆上时，读取每个 Enemy 的 position 几乎必然带来一次 cache miss，每次 cache miss 的代价约为 100～300 个时钟周期（从 L3 或主存加载）。

**数字对比**：500 个 Enemy 的位置更新
- GameObject 方式：每个 Enemy 至少 2 次指针跳跃，最坏情况约 500×2 = 1000 次 cache miss，按 200 周期/次估算，约 200,000 周期的纯等待
- ECS 方式（后文展开）：500 个 position 连续存储，约 500×12 bytes = 6000 bytes，装入约 94 条 cache line，加载后顺序读取，cache miss 次数接近 94 次

---

## ECS 的 Archetype 与 Chunk 布局

ECS 中的 **Entity** 本身只是一个 64 位整数（Index + Version），不持有任何数据。数据由 **Component**（纯 struct，实现 `IComponentData`）持有，System 负责处理数据。

将数据与身份分离之后，ECS 可以按 Component 类型集合来组织内存。

**Archetype** 是相同 Component 类型集合的分类标签。所有同时拥有 `Position`、`Velocity`、`Health` 这三种 Component 的 Entity，无论有多少个，都属于同一个 Archetype，它们的数据被集中管理。

Archetype 的数据存储在 **Chunk** 中。每个 Chunk 固定 **16 KB**。16 KB 的选择并非随意——它与 CPU L1 cache 的典型大小（32KB）和 cache line（64B）协同工作：一个 Chunk 可以完整地装入 L1 cache 的一半，遍历时几乎不会有 L1 miss。

Chunk 内部采用 **SoA（Structure of Arrays）** 布局：同一类型的所有 Component 数据连续存放，不同类型的 Component 分开存放。

```
Chunk（16 KB，Archetype = [Position, Velocity, Health]）

┌─────────────────────────────────────────────────────────┐
│  Chunk Header（元数据：EntityCount、ArchetypeIndex...）  │
├─────────────────────────────────────────────────────────┤
│  Entity IDs:  [e0][e1][e2][e3]...[eN]                   │  8B × N
├─────────────────────────────────────────────────────────┤
│  Position[]:  [p0.xyz][p1.xyz][p2.xyz]...[pN.xyz]       │  12B × N（float3）
├─────────────────────────────────────────────────────────┤
│  Velocity[]:  [v0.xyz][v1.xyz][v2.xyz]...[vN.xyz]       │  12B × N（float3）
├─────────────────────────────────────────────────────────┤
│  Health[]:    [h0][h1][h2]...[hN]                       │  4B × N（float）
└─────────────────────────────────────────────────────────┘
```

**计算一个 16KB Chunk 能放多少个「Position + Velocity」Entity**：

- Header 估算约 128 bytes
- 每个 Entity 占用：Entity ID（8B）+ Position（12B）+ Velocity（12B）= 32B
- 可用空间：(16 × 1024 - 128) / 32 ≈ **511 个 Entity**

遍历时，CPU 读取所有 Position 数组是一次完整的顺序扫描：地址从 `pos_base` 到 `pos_base + 12 × N`，每 64 bytes 一条 cache line，预取器（prefetcher）可以轻松预测并提前加载。访问 Velocity 数组同理。两个数组都在同一个 16KB Chunk 里，L1 cache 完全容得下。

---

## Structural Change 的代价

ECS 的连续内存布局带来了一个必须正视的约束：**Structural Change**。

每当你对一个 Entity 执行 `AddComponent`、`RemoveComponent`、`SetArchetype` 操作，这个 Entity 的 Archetype 就发生了变化——它从一个 Archetype 的 Chunk 迁移到另一个 Archetype 的 Chunk。迁移的物理含义是：把该 Entity 的所有 Component 数据从旧 Chunk 的对应位置，逐字节复制到新 Chunk 的末尾，然后填补旧 Chunk 留下的空洞（用最后一个 Entity 的数据覆盖被移走的槽位）。

```
Structural Change 示意（为 Entity_003 添加 Stunned Component）

Before:
  Archetype A [Position, Velocity, Health]
  Chunk_A: [e0][e1][e2][e3][e4]...

After AddComponent<Stunned>(e3):
  Archetype A [Position, Velocity, Health]  → Chunk_A: [e0][e1][e2][e4]...（e4 填补 e3 的空洞）

  Archetype B [Position, Velocity, Health, Stunned]（新建或已有）
  Chunk_B: [...existing...][e3]（e3 的数据被复制过来）
```

Structural Change 有两个核心限制：

**必须在主线程同步点完成**。Job 执行期间，Chunk 的布局不能被改变，否则正在读写 Chunk 数据的 Job 会访问到已失效的地址。因此所有 Structural Change 都被推迟到 Job 全部完成后的同步点执行。这正是 **EntityCommandBuffer（ECB）** 存在的原因：在 Job 里记录"想要做的 Structural Change"，在同步点回放。

**热路径里必须避免**。如果你的 System 每帧对大量 Entity 执行 Add/Remove Component，迁移开销会抵消掉 SoA 布局带来的所有收益。常见的替代方案是使用 **Enabled Component**（Enableable Components，Unity DOTS 1.0+）：Component 的存在性不变，只切换启用/禁用状态，不触发 Archetype 迁移，代价接近零。

---

## 什么该留在 GameObject，什么适合 ECS

这个问题没有非黑即白的答案，但有清晰的判断标准。

**适合 ECS 的场景**：

- 大量同构对象的高频数值更新——位置、速度、HP、AI 决策数值、子弹轨迹、粒子物理。数量从数百到数十万，更新频率为每帧，且对象之间逻辑高度相似。
- 需要 Burst 编译和 Job System 并行化的计算密集型逻辑。
- 需要确定性模拟或快照回滚的系统（ECS 的纯数据结构便于序列化）。

**适合留在 GameObject 的场景**：

- **UI 组件**：Canvas、Button、ScrollRect 深度依赖 Unity 的 UGUI 系统，迁移收益接近零。
- **单例或低频系统**：GameManager、AudioManager、存档系统——这些逻辑本来就没有并行遍历需求。
- **与 Unity 内置系统深度绑定的组件**：物理关节（ConfigurableJoint）、Animator 状态机、NavMeshAgent——这些组件的内部状态由 Unity 引擎管理，没有公开的 ECS 对等实现，强行封装的代价远大于收益。
- **需要大量 MonoBehaviour 回调的逻辑**：`OnTriggerEnter`、`OnAnimatorIK`、协程（Coroutine）——这些生命周期回调在 ECS 里没有直接对应，模拟它们需要额外的桥接层。

**混合架构是现实**。不存在一个真实商业项目是纯 ECS 的。典型的分工是：战斗单位、子弹、特效粒子走 ECS；UI、对话、过场动画、主角控制器留在 GameObject。边界设计比全量迁移更重要——关键是把高频、高并发、同构的部分识别出来，其余保持原状。

---

## 代码对比：位置更新

**传统 MonoBehaviour 方式**：

```csharp
// EnemyMover.cs
public class EnemyMover : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 _direction;

    void Start()
    {
        _direction = (target.position - transform.position).normalized;
    }

    void Update()
    {
        // 每帧：通过 transform 属性（内部 C++ 调用）读写位置
        // 500 个 Enemy → 500 次独立的 transform 读写，500 次分散的堆访问
        transform.position += _direction * speed * Time.deltaTime;
    }
}
```

每次 `transform.position +=` 背后是一次 C#→C++ 的 interop 调用，读取的数据在引擎内部的 TransformData 数组中，但 500 个对象的 TransformData 未必连续——Unity 的 TransformAccessArray 可以批量处理，但普通 MonoBehaviour 的 Update 调用顺序由引擎调度，无法保证数据局部性。

**DOTS ISystem + IJobEntity 方式**：

```csharp
// EnemyComponents.cs
public struct Position : IComponentData { public float3 Value; }
public struct Velocity : IComponentData { public float3 Value; }

// EnemyMoverSystem.cs
[BurstCompile]
public partial struct EnemyMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // IJobEntity 自动按 Archetype 遍历所有匹配的 Chunk
        // Burst 编译后生成 SIMD 指令，顺序扫描连续内存
        new MoveJob { DeltaTime = dt }.ScheduleParallel();
    }
}

[BurstCompile]
partial struct MoveJob : IJobEntity
{
    public float DeltaTime;

    // 编译器自动生成：对每个 Chunk 顺序遍历 Position 和 Velocity 数组
    void Execute(ref Position pos, in Velocity vel)
    {
        pos.Value += vel.Value * DeltaTime;
    }
}
```

`IJobEntity` 生成的底层代码等价于：

```
for each Chunk in matching Archetypes:
    float3* positions = chunk.GetComponentDataPtr<Position>()   // 连续数组起始地址
    float3* velocities = chunk.GetComponentDataPtr<Velocity>()  // 连续数组起始地址
    for i in 0..chunk.Count:
        positions[i] += velocities[i] * dt                      // 顺序访问，SIMD 可矢量化
```

两段代码的内存访问模式对比：

| 维度 | MonoBehaviour Update | IJobEntity + Burst |
|---|---|---|
| 数据布局 | AoS，对象散落堆上 | SoA，Chunk 内连续 |
| 每次更新的指针跳跃 | 2～4 次（GameObject→Transform→data） | 0（直接数组偏移） |
| 500 Enemy cache miss 估算 | ~1000 次 | ~94 次（≈6KB / 64B） |
| 是否可 SIMD 矢量化 | 否 | 是（Burst 自动） |
| 是否可多线程并行 | 否（主线程） | 是（ScheduleParallel） |

---

数据模型的转变不是代码风格的转变，而是对「数据在内存里应该长什么样」这个问题给出了不同的回答。GameObject 的回答是「每个对象自己负责」，ECS 的回答是「相似的数据住在一起」。理解了这个差异，Archetype、Chunk、Structural Change 就不再是需要记忆的概念，而是这个回答的自然推论。

下一篇 DOTS-E02 将从零开始写第一个完整的 ECS 程序：World 初始化、Entity 创建、System 注册，以及如何在编辑器里验证 Chunk 布局是否符合预期。
