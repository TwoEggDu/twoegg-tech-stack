---
title: "Unity DOTS E09｜Baking Pipeline 全景：Authoring → Baker → Runtime Data，为什么需要这一层"
slug: "dots-e09-baking-pipeline"
date: "2026-03-28"
description: "Baking 是 Unity DOTS 把编辑器数据转换为运行时 ECS 数据的机制。本篇讲清楚 Authoring → Baker → Entity 这条转换链的完整工作原理，以及构建期前移的本质——把不确定性消灭在离线阶段。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Baking"
  - "SubScene"
  - "Baker"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 9
weight: 1890
---

## 问题从哪里来

Unity 编辑器里的数据是 Managed 的。GameObject、MonoBehaviour、SerializedObject——这些对象分散在托管堆上，带有引用语义，依赖 GC，访问时不保证内存连续。它们对编辑器工具友好：Inspector 能序列化它们，Undo 系统能追踪它们，Prefab 嵌套也依赖这套机制。

运行时 ECS 需要的是另一套东西：Unmanaged、结构体、按 Archetype 连续排布在 Chunk 里的数据。Systems 用 Burst 编译，以 SIMD 宽度批量处理组件，期间不能触碰托管对象、不能调用虚函数、不能引发 GC。

这两套世界之间有一道鸿沟，Baking 就是跨越这道鸿沟的桥。它在构建期（或编辑器保存 SubScene 时）把 Managed 的编辑器描述，转译成 Unmanaged 的运行时布局。这不是"运行时懒转换"，而是彻底的**构建期前移**——把不确定性消灭在离线阶段，让运行时只看到已经处理好的确定性数据。

这种思路并不陌生。Shader 编译把 GLSL/HLSL 源码在构建期变成 GPU 字节码；AssetBundle 打包把散落的资产在构建期合并成流式可加载的二进制块；纹理压缩把 PNG 在构建期转成 BC7/ASTC。Baking 是同一类工程决策：让运行时什么都不用想，直接消费。

---

## 转换链全景

```
[Editor / SubScene]
        |
        |  Authoring MonoBehaviour
        |  (Managed, 挂在 GameObject 上，Inspector 可见)
        |
        v
   +----------+
   |  Baker   |   <- 用户实现的转换逻辑
   +----------+
        |
        |  Baker.AddComponent<T>()
        |  Baker.GetEntity(TransformUsageFlags)
        |
        v
[Baking World]
        |
        |  BakingSystem（可选，跨 Entity 后处理）
        |
        v
[Entity + IComponentData]
  (Unmanaged, Chunk 连续内存, 运行时直接消费)
```

Baker 的运行时机有三种：

- **保存 SubScene 时**：编辑器把 SubScene 里所有 GameObject 烘焙一遍，结果序列化为 `.entities` 文件存在磁盘上。
- **打包（Build）时**：全量烘焙，和保存 SubScene 行为一致，确保包内数据是最新的。
- **Live Baking 模式**：在编辑器 Play Mode 或 SubScene 打开状态下，每次修改 Authoring 组件，Baker 实时重新运行，可以在 Entity Debugger 里立即看到结果。

---

## Baker 的正确写法

Baker 以 `Baker<TAuthoring>` 为基类，泛型参数绑定到它负责转换的 Authoring 类型。核心方法只有几个，但每个都有其用意。

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// 1. Authoring：挂在 GameObject 上，Inspector 填数据
public class EnemyAuthoring : MonoBehaviour
{
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public int rewardScore = 10;
}

// 2. ECS Component：运行时数据，纯 Unmanaged 结构体
public struct EnemyStats : IComponentData
{
    public float MaxHealth;
    public float CurrentHealth;
    public float MoveSpeed;
    public int   RewardScore;
}

// 3. Baker：转换逻辑，通常作为 Authoring 的内嵌类
public class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        // GetEntity 获取对应这个 GameObject 的主 Entity。
        // TransformUsageFlags 告诉 Baking 系统这个 Entity 是否需要 Transform 组件：
        //   Dynamic  → 需要 LocalTransform，会移动
        //   None     → 纯数据 Entity，不需要 Transform
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new EnemyStats
        {
            MaxHealth     = authoring.maxHealth,
            CurrentHealth = authoring.maxHealth,
            MoveSpeed     = authoring.moveSpeed,
            RewardScore   = authoring.rewardScore,
        });
    }
}
```

如果 Baker 需要访问同一 GameObject 上的其他 MonoBehaviour，用 `GetComponent<T>()`——这里拿到的是 MonoBehaviour，不是 ECS Component：

```csharp
public class WeaponBaker : Baker<WeaponAuthoring>
{
    public override void Bake(WeaponAuthoring authoring)
    {
        // 访问同一 GameObject 上的 Collider（MonoBehaviour / Unity Component）
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new WeaponHitbox
        {
            Size   = (float3)col.size,
            Center = (float3)col.center,
        });
    }
}
```

`GetComponent` 在 Baker 里的调用会被 Baking 系统记录为依赖——如果 BoxCollider 的值变了，Baker 会被标记为需要重新运行。这就是依赖追踪的基础。

---

## 依赖追踪

Live Baking 的核心问题是：哪些数据发生变化时，Baker 应该重新运行？

Baking 系统会自动追踪你通过 Baker API 读取的数据：`GetComponent<T>()`、`GetComponentInChildren<T>()`、`GetComponents<T>()` 等调用都会隐式注册依赖。当这些组件的序列化数据变化时，Baker 自动重跑。

对于非标准来源的数据——比如你从某个静态字典或外部 ScriptableObject 读值——需要手动声明：

```csharp
public override void Bake(EnemyAuthoring authoring)
{
    // 手动声明对某个 ScriptableObject 的依赖
    DependsOn(authoring.configAsset);

    Entity entity = GetEntity(TransformUsageFlags.Dynamic);
    AddComponent(entity, new EnemyStats
    {
        MaxHealth = authoring.configAsset.baseHealth,
        // ...
    });
}
```

**漏声明依赖的后果**：在 Live Baking 模式下，你改了外部数据源，但 Baker 不知道需要重跑，Entity 的数据维持旧值。场景看起来没问题，打包后才发现数据不对——这是很难定位的 bug。养成习惯：Baker 读了什么，就声明什么。

---

## 多 Baker 与 IBaker

一个 Authoring 组件上可以附加多个 Baker。这在拆分关注点时有用：

```csharp
// 同一个 EnemyAuthoring，两个 Baker 各自负责一块
public class EnemyStatsBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var e = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(e, new EnemyStats { /* ... */ });
    }
}

public class EnemyAITagBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var e = GetEntity(TransformUsageFlags.Dynamic);
        // 添加 Tag Component，供 AI System 查询
        AddComponent<EnemyAITag>(e);
    }
}
```

两个 Baker 独立运行，**执行顺序不保证**。它们拿到的是同一个 Entity（`GetEntity` 对同一 GameObject 总是返回同一个 Entity），因此可以分别往上叠加组件，互不干扰。

`IBaker` 是更底层的接口，`Baker<TAuthoring>` 已经封装了它，日常不需要直接实现 IBaker。了解它存在即可。

---

## BakingSystem：跨 Entity 的后处理

Baker 以单个 GameObject 为粒度运行，没有全局视角。某些操作需要看到整个 Baking World 的状态，比如：

- 计算父子 Entity 关系（一个 Entity 的某个字段需要引用另一个 Entity 的 Handle）
- 生成 Blob Asset（多个 Baker 产出的数据需要合并成一块不可变二进制）
- 处理场景范围的全局配置

这时需要 `BakingSystem`：

```csharp
// 声明为 BakingSystem，只在 Baking 期执行，不进入运行时 World
[BakingVersion("MyProject", 1)]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial class EnemyGroupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 可以查询 Baking World 里所有已烘焙的 Entity
        // 例如：给所有 EnemyStats Entity 计算一个全局 GroupID
        int groupId = 0;
        foreach (var (stats, entity) in
            SystemAPI.Query<RefRW<EnemyStats>>().WithEntityAccess())
        {
            stats.ValueRW.RewardScore += groupId;
            groupId++;
        }
    }
}
```

`WorldSystemFilterFlags.BakingSystem` 是关键标记。带这个标记的 System 只存在于 Baking World，打包后的运行时 World 里完全没有它。它做的是离线数据加工，不产生任何运行时开销。

---

## Live Baking vs 构建 Baking

| 维度 | Live Baking | Build Baking |
|------|-------------|--------------|
| 触发时机 | 编辑器修改 Authoring 组件时实时触发 | `File > Build` 或 `BuildPipeline.BuildPlayer` 时 |
| 结果去向 | 临时 Baking World，Entity Debugger 可见 | 序列化为 `.entities` 文件，打入包内 |
| Baker 代码 | 同一套 | 同一套 |
| 一致性保证 | 两套流程共用同一 Baker，结果应完全一致 | |

一致性是设计目标，但有一个常见陷阱：**非确定性代码**。如果 Baker 里有依赖 `Time.time`、`Random.value`、或字典遍历顺序（C# Dictionary 遍历顺序在不同运行间可能不同）的逻辑，Live Baking 和 Build Baking 会产出不同结果。Baker 应当是纯函数：相同输入 → 相同输出，任何时候都成立。

调试工具：打开 **Window > Entities > Hierarchy** 面板，在 SubScene 打开状态下可以实时看到每个 GameObject 对应的 Entity 和它携带的 Components，是验证 Baker 输出最直接的手段。

---

## 小结

Baking 的本质是一条单向数据流：Managed 编辑器描述 → Baker 转换逻辑 → Unmanaged ECS 数据。它把原本可能在运行时发生的类型转换、反射、布局计算全部提前，让游戏运行时面对的只有已经整理好的、缓存友好的连续内存。

这一层的设计决策影响深远：SubScene 流式加载能工作，是因为 Baking 已经把数据序列化成可以直接 mmap 的格式；Burst 编译器能无虚函数地运行 System，是因为 Baking 保证了运行时没有 Managed 对象混入。下一篇 **E10「SubScene 与流式加载」** 会在这个基础上，讲 Baking 产出的 `.entities` 文件怎么在运行时按需加载和卸载，以及大世界场景管理的完整模型。
