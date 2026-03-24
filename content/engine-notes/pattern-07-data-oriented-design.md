---
title: "游戏编程设计模式 07｜Data-Oriented Design：从面向对象到面向数据"
description: "DOD 不是替代 OOP 的新范式，而是一种针对现代 CPU 缓存架构的优化思维。这篇解释为什么 OOP 在大规模数据处理时会产生性能问题，DOD 如何从根本上解决它，以及 DOTS/ECS 为什么是这个思维的工业实现。"
slug: "pattern-07-data-oriented-design"
weight: 739
tags:
  - 软件工程
  - 设计模式
  - DOD
  - ECS
  - DOTS
  - 性能优化
  - 游戏架构
series: "游戏编程设计模式"
---

> **DOD（Data-Oriented Design）的核心观点**：与其设计对象的层次结构，不如设计**数据的布局**。
>
> 因为代码运行的瓶颈，往往不是"做了多少计算"，而是"等待内存数据的时间"。优化数据布局，让 CPU 缓存能被充分利用，是大规模实体处理的关键。

---

## CPU 缓存：现代性能的关键

理解 DOD，需要先理解现代 CPU 的内存访问模型。

CPU 的运算速度远快于内存的访问速度。为了弥补这个速度差，CPU 有多级缓存（L1/L2/L3），把"可能马上要用的数据"提前加载进来：

```
访问速度（近似值）：
L1 缓存：~4 周期
L2 缓存：~12 周期
L3 缓存：~40 周期
主内存（RAM）：~200 周期

结论：从 RAM 读数据比从 L1 读慢约 50 倍
```

关键机制：**缓存行（Cache Line）**。CPU 读取内存时，不是一次读一个字节，而是一次读 64 字节（一个 Cache Line）。如果你要读的数据刚好在这 64 字节里，就是"缓存命中"，非常快。如果数据散落在内存各处，每次都要从 RAM 读新的 64 字节，就是"缓存缺失（Cache Miss）"，非常慢。

DOD 的目标：让需要被同时处理的数据，在内存里**紧密相邻**，最大化缓存命中率。

---

## OOP 的缓存问题：AoS vs SoA

面向对象的典型设计是一个对象包含它的所有数据：

```csharp
// AoS：Array of Structures（结构体数组）——OOP 的自然结果
public class Enemy
{
    public Vector3 position;    // 12 bytes
    public Vector3 velocity;    // 12 bytes
    public float health;        //  4 bytes
    public float attackPower;   //  4 bytes
    public bool isAlive;        //  1 byte
    public Sprite sprite;       //  引用（指针）
    public AudioClip deathSound;//  引用（指针）
    // 每个 Enemy 对象约 60+ bytes
}

Enemy[] enemies = new Enemy[1000];
```

内存布局（简化）：

```
[pos|vel|hp|atk|alive|sprite|sound] [pos|vel|hp|atk|alive|sprite|sound] [pos|vel|hp|atk|alive|sprite|sound]...
   Enemy[0]                              Enemy[1]                              Enemy[2]
```

现在，游戏每帧要**只更新所有敌人的位置**（根据速度移动）：

```csharp
void UpdateAllPositions(Enemy[] enemies)
{
    foreach (var enemy in enemies)
    {
        enemy.position += enemy.velocity * Time.deltaTime;
    }
}
```

这段代码只需要 `position` 和 `velocity`，但每次读取一个 Enemy 的数据时，整个 60 字节的对象都被加载进缓存行。60 字节里只有 24 字节（position + velocity）是我们需要的，其余 36 字节（health、sprite、deathSound...）被白白加载了，占用了宝贵的缓存空间。

1000 个敌人 = 1000 次缓存行加载，大量数据被无效占用。

DOD 的思路是**SoA（Structure of Arrays）**：把同类数据放在一起。

```csharp
// SoA：Structure of Arrays（数组的结构体）——DOD 的结果
public struct EnemyDatabase
{
    public Vector3[] positions;   // 所有敌人的位置连续存储
    public Vector3[] velocities;  // 所有敌人的速度连续存储
    public float[]   healths;
    public float[]   attackPowers;
    public bool[]    isAlives;
    // ...
}

EnemyDatabase db = new EnemyDatabase
{
    positions  = new Vector3[1000],
    velocities = new Vector3[1000],
    // ...
};
```

内存布局：

```
positions:  [pos0][pos1][pos2][pos3][pos4][pos5]...  ← 紧密相邻！
velocities: [vel0][vel1][vel2][vel3][vel4][vel5]...  ← 紧密相邻！
healths:    [hp0][hp1][hp2][hp3][hp4][hp5]...
```

更新位置时：

```csharp
void UpdateAllPositions(EnemyDatabase db, int count)
{
    for (int i = 0; i < count; i++)
    {
        db.positions[i] += db.velocities[i] * Time.deltaTime;
    }
}
```

现在每次缓存行加载，装的全是 `positions` 数组里的连续 position 数据（64 bytes ≈ 5 个 Vector3），每个都是我们需要的。缓存命中率从 40%（AoS）提升到接近 100%（SoA）。

在处理 1000~10000 个同类实体时，这个差距是**数倍到数十倍**的性能提升。

---

## 从 DOD 到 ECS

DOD 的思维自然导向 **ECS（Entity-Component-System）** 架构：

- **Entity**：只是一个 ID（整数），没有数据，没有行为，只是"一个存在的标识"
- **Component**：纯数据结构，没有行为，只有字段（struct）
- **System**：纯函数，操作拥有特定 Component 组合的所有 Entity

```
OOP 的 Enemy 对象：
Enemy = { position, velocity, health, attackPower, sprite, sound, Update(), TakeDamage(), Die() }
（数据和行为混在一起）

ECS 的 Enemy：
Entity ID = 42

Components（纯数据）：
  PositionComponent { x: 5, y: 0, z: 3 }
  VelocityComponent { x: 0, y: 0, z: -1 }
  HealthComponent   { current: 80, max: 100 }
  AttackComponent   { power: 20, range: 2.0 }

Systems（纯行为）：
  MovementSystem    → 处理所有有 Position + Velocity 的 Entity
  AttackSystem      → 处理所有有 Attack + Position 的 Entity
  HealthSystem      → 处理所有有 Health 的 Entity
```

ECS 的内存布局（Archetype，原型）：

Unity DOTS 把所有拥有**相同 Component 组合**的 Entity 打包存储在连续内存块（Chunk）里：

```
Chunk（具有 Position + Velocity + Health 的 Entities）：
positions:  [e0_pos][e1_pos][e2_pos]... ← 连续
velocities: [e0_vel][e1_vel][e2_vel]... ← 连续
healths:    [e0_hp ][e1_hp ][e2_hp ]... ← 连续
```

System 处理时，只读取它需要的 Component 数组，完美的 SoA 布局，CPU 缓存利用率极高。

---

## Unity DOTS 的代码示例（概念展示）

```csharp
// Component：纯数据，struct
public struct PositionComponent : IComponentData
{
    public float3 Value;
}

public struct VelocityComponent : IComponentData
{
    public float3 Value;
}

public struct HealthComponent : IComponentData
{
    public float Current;
    public float Max;
}

// System：纯行为，处理所有有 Position + Velocity 的 Entity
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        // Entities.ForEach 在底层是高度优化的并行循环
        // 编译器（Burst Compiler）会把它编译成 SIMD 指令
        Entities
            .ForEach((ref PositionComponent pos, in VelocityComponent vel) =>
            {
                pos.Value += vel.Value * dt;
            })
            .ScheduleParallel(); // 自动多线程并行
    }
}
```

DOTS 的优势不只是缓存命中，还有：
- **Burst Compiler**：把 C# 代码编译成高度优化的原生代码（SIMD 指令）
- **Job System**：自动多线程并行，充分利用多核 CPU
- **Zero GC**：Component 是 struct，分配在 Chunk 上，没有堆分配，没有 GC

---

## DOD 什么时候适用

DOD 不是"总是比 OOP 好"——它解决的是**特定规模**下的**特定问题**：

**适合 DOD/ECS 的场景**：
- 大量同类实体（几千到几万：子弹、粒子、NPC、敌群）
- 频繁的批量处理（每帧都要更新所有实体的位置、状态）
- 性能是关键约束（开放世界、RTS 大规模单位）

**不适合 DOD/ECS 的场景**：
- 少量复杂实体（玩家角色、Boss）——OOP 更直观
- 高度不规则的行为（每个对象的逻辑都完全不同）——ECS 没有优势
- 团队不熟悉 ECS 思维——学习曲线很陡，早期生产力会下降

**现实项目的常见策略**：

```
OOP（GameObject 系统）：
  ├── 玩家角色（唯一的，逻辑复杂）
  ├── Boss（少量，逻辑复杂）
  └── UI 系统（状态复杂，性能不是瓶颈）

ECS/DOD（DOTS 系统）：
  ├── 子弹 / 箭矢（每帧数千个，批量移动碰撞）
  ├── 路人 NPC（开放世界中数千个，只需简单 AI）
  └── 粒子效果（GPU 侧本来就是 DOD 的，CPU 驱动部分用 ECS）
```

两套系统共存，各司其职。

---

## DOD 的思维方式总结

DOD 不是语法，是思维方式的转变：

| OOP 思维 | DOD 思维 |
|---|---|
| 先设计"对象是什么" | 先设计"数据长什么样" |
| 行为和数据绑定在对象里 | 行为（System）和数据（Component）分离 |
| 多态通过继承实现 | 多态通过 Component 组合实现 |
| 关注单个对象的生命周期 | 关注所有对象的批量处理效率 |

这个思维转变很难，特别是对有多年 OOP 经验的程序员。但一旦掌握，它会给你一个全新的视角来评估游戏系统的设计：**这个设计的数据局部性如何？CPU 缓存利用率如何？**

---

## 小结

- **DOD 的根本**：把"CPU 等待内存数据"的时间降到最低，方法是让需要同时处理的数据在内存里连续存放
- **AoS vs SoA**：OOP 自然产生 AoS（对象数组），DOD 要求 SoA（数组的结构体）
- **ECS**：DOD 思维的完整架构实现——Entity（ID）+ Component（纯数据）+ System（纯行为）
- **Unity DOTS**：ECS + Burst Compiler + Job System，是工业级的 DOD 实现
- **适用场景**：大量同类实体的批量处理；不适合少量复杂实体或逻辑高度差异化的对象
