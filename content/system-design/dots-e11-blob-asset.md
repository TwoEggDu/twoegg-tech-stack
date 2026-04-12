---
title: "Unity DOTS E11｜Blob Asset：只读数据的高效打包、引用计数与访问方式"
slug: "dots-e11-blob-asset"
date: "2026-03-28"
description: "Blob Asset 是 DOTS 存储只读配置数据（技能参数、导航图、动画曲线）的机制，把可变长度的复杂数据打包进连续内存，通过引用计数管理生命周期。本篇讲清楚 Blob 的内存布局、创建方式和在 Job 中的安全访问。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Blob Asset"
  - "只读数据"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 11
weight: 1910
---

## 问题的起点：ECS Component 放不下复杂数据

ECS 的 Component 必须是 unmanaged struct——不能包含数组、`string`、`class`，也不能有任何托管引用。这个约束对于 `float`、`int`、`bool` 等简单数值完全没问题，但一旦涉及到游戏配置数据，麻烦就来了。

考虑一个技能配置：

```csharp
// 这段代码在 ECS 中无法编译通过
public struct SkillConfig : IComponentData
{
    public float[] damages;        // 不允许：托管数组
    public string name;            // 不允许：托管 string
    public AnimationCurve curve;   // 不允许：托管 class
}
```

所有这些"复杂只读数据"都触犯了 unmanaged 规则。直觉上的替代方案——把数据存在 `NativeArray` 里——也行不通，因为 `NativeArray` 同样不能直接放进 Component（生命周期难以管理，且 Job Safety System 会拒绝）。

**Blob Asset** 正是为这个场景设计的：把复杂的只读数据打包进一块连续的 Native Memory，然后让 Component 持有一个轻量的 `BlobAssetReference<T>` 指针。这个指针是 unmanaged struct，可以安全地放进 Component，也可以在 Job 中传递。

---

## 内存布局：偏移量，而不是指针

Blob 最核心的设计是：**整块数据在一片连续的 Native Memory 中**，内部所有引用（`BlobArray<T>`、`BlobString`）存储的都是**相对偏移量**，而不是绝对指针。

这样做的原因是可移植性：Blob 可以序列化到磁盘、从 SubScene 加载时映射到任意内存地址，只要基地址确定，偏移量就能正确还原出所有字段的位置。

以一个包含 `BlobArray<float>` 和 `BlobString` 的技能配置为例：

```
[ BlobAssetHeader ][ SkillConfigBlob root ][ float[3] 数据 ][ "FireBall\0" 字符串 ]
  ^                  ^                       ^                ^
  基地址             root 偏移 0             damages 偏移     name 偏移

BlobArray<float>.Offset  ──────────────────►  0x0010（相对于 BlobArray 字段自身地址）
BlobString.Offset        ──────────────────────────────────►  0x001C
```

`BlobArray<T>` 和 `BlobString` 在访问时，会用自身字段的地址加上内部存储的偏移量，计算出实际数据的地址。**你永远不需要手动计算这些偏移量**——`BlobBuilder` 在构建阶段全部处理好了。

---

## 创建方式：BlobBuilder

`BlobBuilder` 是构建 Blob 的唯一官方途径，用完即 `Dispose`，它本身是一个临时构建工具。

先定义 Blob 根结构（只能含 unmanaged 类型、`BlobArray<T>`、`BlobString`、`BlobPtr<T>`）：

```csharp
using Unity.Entities;

public struct SkillConfigBlob
{
    public BlobArray<float> damages;   // 各段伤害
    public float cooldown;
    public BlobString name;
}
```

然后用 `BlobBuilder` 填充数据：

```csharp
using Unity.Entities;
using Unity.Collections;

public static BlobAssetReference<SkillConfigBlob> CreateSkillBlob(
    string skillName, float[] damageValues, float cooldown)
{
    var builder = new BlobBuilder(Allocator.Temp);

    // 1. 声明根结构
    ref SkillConfigBlob root = ref builder.ConstructRoot<SkillConfigBlob>();

    // 2. 分配 BlobArray：返回一个可写的 BlobBuilderArray
    BlobBuilderArray<float> dmgArray =
        builder.Allocate(ref root.damages, damageValues.Length);
    for (int i = 0; i < damageValues.Length; i++)
        dmgArray[i] = damageValues[i];

    // 3. 分配 BlobString
    builder.AllocateString(ref root.name, skillName);

    // 4. 填写普通字段
    root.cooldown = cooldown;

    // 5. 生成不可变的 BlobAssetReference
    BlobAssetReference<SkillConfigBlob> blobRef =
        builder.CreateBlobAssetReference<SkillConfigBlob>(Allocator.Persistent);

    builder.Dispose(); // BlobBuilder 使用完毕立即释放
    return blobRef;
}
```

几个关键细节：

- `ConstructRoot<T>()` 返回的 `ref` 只在 `builder.Dispose()` 之前有效，不要缓存它。
- `Allocate(ref root.damages, count)` 返回 `BlobBuilderArray<T>`，这才是构建期间可写的视图；最终固化后只能通过 `BlobArray<T>` 只读访问。
- `CreateBlobAssetReference` 通常使用 `Allocator.Persistent`，因为 Blob 的生命周期往往与游戏运行时一样长。

---

## 在 Baker 中创建 Blob

游戏中的技能配置通常来自 ScriptableObject。Baking 流程负责把 Authoring 数据转换为 ECS 数据，Blob 应当在 Baker 里创建。

Baker 提供了 `CreateBlobAssetReference<T>()` 的专用重载，内部会**自动计算 Hash、检测相同内容是否已存在并复用**，避免重复分配。

```csharp
using UnityEngine;
using Unity.Entities;

// --- Authoring 侧（MonoBehaviour）---
public class SkillAuthoring : MonoBehaviour
{
    public SkillConfigSO config; // ScriptableObject
}

// --- ScriptableObject 定义 ---
[CreateAssetMenu]
public class SkillConfigSO : ScriptableObject
{
    public string skillName;
    public float[] damages;
    public float cooldown;
}

// --- Component 定义 ---
public struct SkillConfigComponent : IComponentData
{
    public BlobAssetReference<SkillConfigBlob> config;
}

// --- Baker ---
public class SkillBaker : Baker<SkillAuthoring>
{
    public override void Bake(SkillAuthoring authoring)
    {
        var so = authoring.config;
        if (so == null) return;

        // 注册依赖：SO 内容变化时重新 Bake
        DependsOn(so);

        var builder = new BlobBuilder(Allocator.Temp);
        ref SkillConfigBlob root = ref builder.ConstructRoot<SkillConfigBlob>();

        BlobBuilderArray<float> dmgArray =
            builder.Allocate(ref root.damages, so.damages.Length);
        for (int i = 0; i < so.damages.Length; i++)
            dmgArray[i] = so.damages[i];

        builder.AllocateString(ref root.name, so.skillName);
        root.cooldown = so.cooldown;

        // 使用 Baker 专用方法：自动处理 hash 与复用
        var blobRef = builder.CreateBlobAssetReference<SkillConfigBlob>(Allocator.Persistent);
        builder.Dispose();

        // 注册到 Baker，使引擎能跟踪此 Blob 的生命周期
        AddBlobAsset(ref blobRef, out _);

        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new SkillConfigComponent { config = blobRef });
    }
}
```

`AddBlobAsset` 是关键调用：它把 Blob 的所有权交给 Baker 系统，后者会在 SubScene 卸载时自动释放内存，开发者无需手动 `Dispose`。

---

## 在 Job/System 中访问

`BlobAssetReference<T>` 是 unmanaged struct，可以直接放进 `IJobEntity` 或 `ISystem` 的局部变量，Job Safety System 允许在 Job 中读取它（但不允许写入，Blob 是只读的）。

```csharp
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct SkillDamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var config in
            SystemAPI.Query<RefRO<SkillConfigComponent>>())
        {
            // .Value 解引用：返回对 Blob 根结构的 ref（只读）
            ref readonly SkillConfigBlob blob = ref config.ValueRO.config.Value;

            // 访问普通字段
            float cd = blob.cooldown;

            // 访问 BlobArray：用索引，不支持 foreach
            for (int i = 0; i < blob.damages.Length; i++)
            {
                float dmg = blob.damages[i];
                // ... 业务逻辑
            }

            // 访问 BlobString（转为 FixedString 使用）
            // blob.name 本身是 BlobString，可用 blob.name.ToString() 或直接比较
        }
    }
}
```

注意 `BlobArray<T>` **不支持 `foreach`**，必须用下标索引。这是因为 Blob 在 Burst 环境下需要完全确定的内存访问模式。

---

## 生命周期与引用计数

`BlobAssetReference<T>` 内部维护一个**引用计数**。多个 Component、多个 Entity 可以持有指向同一块 Blob 内存的引用，引用计数记录着当前有多少持有者。

- **手动创建的 Blob**（在 System 或工具代码里用 `BlobBuilder` 直接创建）：需要调用 `blobRef.Dispose()` 来减少引用计数；计数归零时内存自动释放。
- **Baker 创建的 Blob**（通过 `AddBlobAsset` 注册）：由 Entities 的 SubScene 管理系统托管，SubScene 卸载时自动调用 `Dispose`，开发者不需要手动释放。

**常见错误**：在 SubScene 已经卸载后仍然持有 `BlobAssetReference`，此时 Blob 内存已被释放，通过 `.Value` 访问会导致悬空引用崩溃。解决方法是始终通过 Baker 注册 Blob，让引擎管理生命周期。

---

## 为什么 Blob 不能包含托管引用

有时会有这样的想法：把一个 `GameObject` 引用或 `UnityEngine.Object` 塞进 Blob 里，借助 Blob 的连续布局传递到 Job 中。这是**绝对不可行的**，原因涉及 GC 的工作机制。

GC（垃圾回收器）通过扫描托管堆上的对象图来判断哪些对象还"活着"。Blob 分配在 **Native Memory**（Unity 的非托管堆），GC 的扫描根本不会到达这里。如果一个托管对象的引用被存入 Blob，GC 就看不到这条引用路径，会误判该托管对象无人持有，在下一次 GC 回收中将其销毁，留下一个悬空指针。

结论：**Blob 只能存储 unmanaged 数据**。`BlobArray<T>` 的 `T` 必须是 unmanaged，`BlobString` 只存储 UTF-8 字节而非托管 `string`，整个 Blob 根结构也必须满足 unmanaged 约束。Burst 编译器会在编译期强制检查这一点。

---

## 小结

| 方面 | 要点 |
|---|---|
| 适用场景 | 只读的复杂配置数据：技能参数、导航数据、动画关键帧 |
| 内存布局 | 单块连续 Native Memory，内部用偏移量而非指针 |
| 创建工具 | `BlobBuilder`，用完立即 `Dispose` |
| Baker 集成 | `AddBlobAsset` 注册后由引擎托管生命周期 |
| Job 访问 | `.Value` 解引用，`BlobArray` 只能下标访问 |
| 引用计数 | 手动创建需 `Dispose`，Baker 注册自动释放 |
| 不能存托管引用 | GC 无法扫描 Native Memory，会导致悬空引用 |

---

至此，Baking 三篇（E09 Entity 生成与组件赋值、E10 Baker 依赖与增量重烘、E11 Blob Asset 只读数据打包）已全部覆盖。下一组将进入 **Jobs 与 Burst（E12~E14）**：IJobEntity 的调度模型、Burst 编译约束与性能收益、以及 Job 依赖链与并行安全。
