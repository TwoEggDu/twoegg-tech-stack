---
title: "Unity DOTS E05｜ComponentLookup 与随机访问：在 Job 里安全地查另一个 Entity"
slug: "dots-e05-componentlookup"
date: "2026-03-28"
description: "ComponentLookup 是 ECS 里随机访问另一个 Entity 组件的标准方式，但随机访问打破了 SoA 的 cache 优势。本篇讲清楚 ComponentLookup 的正确用法、安全系统对它的限制，以及什么时候用它是合理的。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "ComponentLookup"
  - "随机访问"
  - "Job Safety"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 5
weight: 1850
---

ECS 的核心优势是 SoA（Struct of Arrays）布局：同一类型的 Component 在内存里连续排列，IJobChunk 顺序遍历时 CPU 预取命中率极高。但现实的游戏逻辑很难全部写成"只遍历自己"的形式。

伤害系统需要读目标的 `Health`，AI 需要读目标的 `Position`，寻路系统需要查邻居节点的 `Walkable`——这些都是"给定一个具体的 Entity，读取它的某个 Component"，不是批量遍历，而是**随机访问**。`ComponentLookup<T>` 就是 ECS 为这类需求提供的标准 API。

---

## 为什么批量遍历解决不了随机访问

`IJobChunk` 和 `IJobEntity` 的访问模式是：遍历所有符合 Query 的 Chunk，逐个处理其中的 Entity。这在"处理自己"的场景下完美，但无法表达"处理自己时，顺带读一下另一个特定 Entity 的数据"——因为那个目标 Entity 在哪个 Chunk、哪个索引，在编译期完全未知。

典型需求举例：

- **伤害系统**：每个 `DamageEvent` 携带 `target: Entity`，Job 需要找到这个 target 的 `Health` 并扣减
- **AI 追踪**：AI Entity 持有 `targetEntity: Entity`，每帧需要读取目标的 `LocalTransform`
- **技能系统**：施法者需要读取自身 Buff 列表里每个 Buff Entity 的参数

---

## ComponentLookup 的基本用法

### 获取 Lookup

在 `SystemBase` 或 `ISystem` 的 `OnUpdate` 里，通过 `SystemAPI` 创建 Lookup：

```csharp
// isReadOnly: true  — 只读，允许多个 Job 并行读
// isReadOnly: false — 读写，独占
var healthLookup = SystemAPI.GetComponentLookup<Health>(isReadOnly: true);
var posLookup    = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);
```

`GetComponentLookup` 本质上是从当前 World 的 EntityManager 里拿到类型 T 的全局访问句柄，代价极低，每帧调用一次即可。

### 两种读取方式

```csharp
// 方式一：直接索引，Entity 不存在或没有该 Component 时抛异常
Health hp = healthLookup[targetEntity];

// 方式二：安全读取，返回 bool，不存在则返回 false
if (healthLookup.TryGetComponent(targetEntity, out Health hp))
{
    // 安全使用 hp
}
```

生产代码里**几乎都应该用 `TryGetComponent`**，因为目标 Entity 在上一帧可能已被销毁，直接索引会在 SafetyChecks 模式下触发异常，Release 模式下则是未定义行为。

### 在 Job 里声明 Lookup 字段

Lookup 必须作为 Job struct 的字段传入，不能在 Execute 方法里临时创建：

```csharp
[BurstCompile]
public partial struct ApplyDamageJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Health> HealthLookup;

    public void Execute(ref DamageEvent dmgEvent)
    {
        if (HealthLookup.TryGetComponent(dmgEvent.Target, out Health hp))
        {
            // 这里是只读，要写回需要用非 ReadOnly 的 Lookup
            // 或者把修改拆到下一个 Job
        }
    }
}
```

### 完整示例：DamageSystem

下面演示一个更实际的 DamageSystem，分两步处理：先用只读 Lookup 查询目标是否存活，再用读写 Lookup 修改 HP。

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public partial struct DamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 读写 Lookup：Job 内会修改 Health
        var healthLookup = SystemAPI.GetComponentLookup<Health>(isReadOnly: false);

        var job = new ApplyDamageJob
        {
            HealthLookup = healthLookup
        };

        // Complete 依赖，然后调度（含写权限的 Job 不能与读同类型的 Job 并行）
        state.Dependency = job.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct ApplyDamageJob : IJobEntity
{
    public ComponentLookup<Health> HealthLookup; // 读写，不加 [ReadOnly]

    public void Execute(Entity self, in DamageEvent dmgEvent)
    {
        if (!HealthLookup.TryGetComponent(dmgEvent.Target, out Health hp))
            return; // 目标已不存在

        hp.Value -= dmgEvent.Amount;

        // TryGetComponent 只读了一份拷贝，修改后需要写回
        HealthLookup[dmgEvent.Target] = hp;
    }
}
```

---

## Job Safety System 的约束

Unity 的 Safety System 追踪每个 ComponentType 的读写状态，规则如下：

| Lookup 类型 | 两个 Job 能否并行 |
|---|---|
| 两个 `[ReadOnly]` Lookup&lt;T&gt; | 可以并行 |
| 一个写 Lookup&lt;T&gt; + 一个读 Lookup&lt;T&gt; | 必须串行 |
| 两个写 Lookup&lt;T&gt; | 必须串行 |

这与 `IJobChunk` 里 `[ReadOnly]` 的规则完全一致。**错误地省略 `[ReadOnly]`** 会让本可并行的 Job 被强制串行，是常见的性能陷阱。

实际调度时，Safety System 在 Editor 和 Development Build 下会在 Job 完成后校验读写冲突；违规会抛出 `InvalidOperationException`，告知哪两个 Job 对同一 ComponentType 产生了冲突。

---

## 随机访问的 Cache 代价

ComponentLookup 的内部实现大致是：

1. 通过 Entity（含 Index + Version）定位到 EntityInChunk 记录
2. 找到对应的 Chunk 和该 Entity 在 Chunk 内的行号
3. 计算 Component T 在 Chunk 内的偏移，读出数据

如果 100000 个请求访问的 Entity **散落在不同的 Chunk**，每次步骤 2 的 Chunk 指针都指向不同的内存页，CPU 预取完全失效，每次都是 cache miss。

粗略量化（桌面端，实测因硬件而异）：

- 顺序遍历 100000 个 Entity 的 Component：约 **0.3–0.5 ms**
- 随机 ComponentLookup 100000 次（完全随机分布）：约 **3–8 ms**

差距在 10 倍量级。这不是 ComponentLookup 的 bug，而是**随机内存访问的固有代价**。

---

## 什么时候随机访问是合理的

不是说随机访问就不能用，关键在量级和模式：

**合理场景：**

- 每帧随机访问次数远小于总 Entity 数量（经验值：< 1%）。10000 个 Entity 里，每帧只有几十次 Lookup，代价可忽略
- 访问有局部性：同一 Archetype 的 Entity 倾向于互相引用，它们大概率在同一批 Chunk，Cache 命中率较高
- 数据依赖不可避免：伤害事件确实需要知道目标的 HP，没有办法改写成纯批量遍历

**应考虑替代方案的场景：**

- 每个 Entity 都需要访问多个目标：考虑用 `IBufferElementData` 存储目标引用列表，改写成批量处理
- 目标集合固定：考虑把数据冗余到发起方的 Component 里，用空间换时间
- 访问模式可预测：考虑提前排序或分组，让访问顺序接近内存顺序

---

## EntityStorageInfoLookup：先确认 Entity 存在

有时不只是"没有某个 Component"，而是整个 Entity 已经被销毁。`TryGetComponent` 在 Entity 不存在时也会返回 false，但如果你需要**明确区分"Entity 不存在"和"Entity 存在但没有该 Component"**，用 `EntityStorageInfoLookup`：

```csharp
var storageInfo = SystemAPI.GetEntityStorageInfoLookup();

// 在 Job 里：
if (!storageInfo.Exists(dmgEvent.Target))
{
    // Entity 已被销毁，跳过或清理 DamageEvent
    return;
}

// Entity 存在，再查 Component
if (HealthLookup.TryGetComponent(dmgEvent.Target, out Health hp))
{
    // ...
}
```

典型用例：目标在上一帧被 DestructionSystem 销毁，但 DamageEvent 还没来得及处理，这帧需要优雅地跳过。

---

## 常见错误

**1. 在同一 Job 里对同一 Entity 的同一 Component 同时读写**

```csharp
// 错误示例：self 和 dmgEvent.Target 可能是同一个 Entity
hp.Value = HealthLookup[self].Value + HealthLookup[dmgEvent.Target].Value;
HealthLookup[self] = hp; // 如果 self == dmgEvent.Target，逻辑已经出错
```

Safety System 不能检测出"同一 Entity 的逻辑冲突"（它只检测类型级别的读写冲突），这类 bug 需要通过业务逻辑保证不会自伤。

**2. Structural Change 后 Lookup 失效**

`ComponentLookup` 在获取后是一个快照句柄。如果在 Job 调度和执行之间发生了 **Structural Change**（AddComponent、RemoveComponent、DestroyEntity），Lookup 里缓存的 Chunk 指针可能已经失效。

正确做法：每帧在 `OnUpdate` 里重新调用 `GetComponentLookup` 获取新句柄，不要把 Lookup 缓存为 System 的字段跨帧使用（`ISystem` 里用字段缓存类型句柄是可以的，但 Lookup 本身要每帧刷新）。

```csharp
// 错误：跨帧缓存 Lookup
public partial struct BadSystem : ISystem
{
    ComponentLookup<Health> _cachedLookup; // 危险！

    public void OnCreate(ref SystemState state)
    {
        _cachedLookup = SystemAPI.GetComponentLookup<Health>(); // 只获取一次
    }

    public void OnUpdate(ref SystemState state)
    {
        // _cachedLookup 可能已过期
    }
}

// 正确：每帧重新获取
public void OnUpdate(ref SystemState state)
{
    var healthLookup = SystemAPI.GetComponentLookup<Health>(isReadOnly: true); // 每帧刷新
    // ...
}
```

---

## 小结

`ComponentLookup<T>` 是 ECS 里随机访问的合法出口，正确使用它需要记住三点：

1. **读写权限要准确标注**，`[ReadOnly]` 不是可选项，它直接影响 Job 的并行度
2. **优先用 `TryGetComponent`**，防御性编程，避免目标不存在时崩溃
3. **量入为出**，随机访问的 cache 代价是真实的，数量多时需要考虑数据布局的替代设计

---

下一篇 **DOTS-E06「IBufferElementData：给 Entity 挂动态列表」** 将介绍如何给单个 Entity 附加一个可变长的组件列表——这正是"替代大量 ComponentLookup"的常见方案之一：把目标引用列表存成 Buffer，改随机访问为顺序遍历。
