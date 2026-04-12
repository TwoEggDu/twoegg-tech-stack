---
title: "Unity DOTS E02｜第一个完整 ECS 程序：World、EntityManager、IComponentData、ISystem 的最小组合"
slug: "dots-e02-first-ecs-program"
date: "2026-03-28"
description: "先走通一遍完整的 ECS 程序，再讲为什么这样设计。本篇用一个可运行的「移动 Entity」示例，串联 World、EntityManager、IComponentData、ISystem、SystemGroup 这五个核心概念，建立第一个完整的 ECS 程序心智模型。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "ISystem"
  - "World"
  - "EntityManager"
  - "IComponentData"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 2
weight: 1820
---

E01 讲了 Archetype 和 Chunk 的存储模型——数据按类型连续排列，Cache 友好，CPU 可以批量处理。这一篇不再重复那些原理，而是直接走通一遍能运行的代码，把五个核心概念串联起来：**World、EntityManager、IComponentData、ISystem、SystemGroup**。

目标：跟完本篇之后，你手上有一个在 Unity 6 + Entities 1.x 中真正能跑的最小 ECS 程序，知道每一行代码的职责。

---

## 1. 依赖与场景设置

需要的 Package 只有一个：

```
com.unity.entities  版本 1.0 ~ 1.3
```

在 Package Manager 中通过 **Add package by name** 安装。Unity 6 已经把渲染集成进去，不需要额外安装旧版的 Hybrid Renderer。安装完成后，Project Settings → Editor → Enter Play Mode Settings，建议关掉 Domain Reload（Enable Domain Reload 取消勾选），迭代速度会快很多。

本篇选择最直白的方式创建 Entity：**Authoring GameObject + Baker**。这不需要手动建 SubScene，只要在普通场景里放一个带 Authoring 组件的 GameObject 就行。SubScene 是生产项目的推荐做法，留到后面单独一篇讲。

---

## 2. IComponentData：只放数据，不放逻辑

ECS 中的"C"是 Component，但它和 MonoBehaviour 组件的概念不同——它只是一个装数据的容器，没有任何 Unity 生命周期回调。

定义两个组件，一个存位置，一个存速度：

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct Position : IComponentData
{
    public float3 Value;
}

public struct Velocity : IComponentData
{
    public float3 Value;
}
```

几点需要注意：

- **必须是 unmanaged struct**。`IComponentData` 要求组件是 blittable 值类型，不能包含 `string`、`List<T>` 或任何 class 引用。违反这一点编译器不会报错，但 Burst 编译会失败，运行时也可能出现意外行为。
- **不含任何方法或逻辑**。和 MonoBehaviour 字段的本质区别：MonoBehaviour 的字段和它的 `Update()` 住在同一个 class 里，而 ECS 把数据和逻辑彻底分开。`Position` 不知道自己如何移动，移动逻辑写在 System 里。
- **命名用 `Value` 包裹**。这是社区约定，方便后续用 `SystemAPI` 的 lambda 简写，也让意图更清晰。

---

## 3. Authoring + Baker：编辑期到运行期的桥梁

直接问题：GameObject 上的 MonoBehaviour 怎么变成 ECS 的 Entity？

答案是 **Baker**。Baking 是 Unity 在进入 Play Mode（或构建时）执行的一个转换阶段，它把场景里的 Authoring GameObject 转换成 Entity 和 Component 数据。运行时，Authoring 组件已经不存在了，只剩下纯粹的 ECS 数据。这个分离的好处是：编辑器工具、序列化、Inspector 继续使用熟悉的 MonoBehaviour 体系，而运行时完全是高性能的 ECS 路径。

先写 Authoring 组件：

```csharp
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class MoverAuthoring : MonoBehaviour
{
    public Vector3 InitialVelocity = new Vector3(1f, 0f, 0f);

    class Baker : Baker<MoverAuthoring>
    {
        public override void Bake(MoverAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Position
            {
                Value = authoring.transform.position
            });

            AddComponent(entity, new Velocity
            {
                Value = authoring.InitialVelocity
            });
        }
    }
}
```

几个细节：

- `Baker<T>` 作为嵌套 class 写在 Authoring 里是约定，也可以分开写，嵌套只是让文件更内聚。
- `GetEntity(TransformUsageFlags.Dynamic)` 告诉 Baking 系统：这个 Entity 需要 Transform，且会在运行时移动。如果只是静态物体，用 `TransformUsageFlags.None` 减少开销。
- `AddComponent` 在 Baking 阶段执行，不是运行时调用。Baker 里不要写任何依赖运行状态的逻辑。

使用方式：在场景里创建一个空 GameObject，挂上 `MoverAuthoring`，在 Inspector 里设置 Initial Velocity，进入 Play Mode，Baker 就会在那一刻执行转换。

---

## 4. ISystem：在正确的地方执行逻辑

System 是 ECS 的"S"，负责每帧读写组件数据。这里选用 `ISystem` 而不是 `SystemBase`，原因是 `ISystem` 是 **unmanaged**（值类型），可以被 Burst 编译，性能更好。两者的选择留到 E03 详细对比，本篇先用 `ISystem` 建立概念。

```csharp
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (position, velocity) in
            SystemAPI.Query<RefRW<Position>, RefRO<Velocity>>())
        {
            position.ValueRW.Value += velocity.ValueRO.Value * deltaTime;
        }
    }
}
```

逐行解释：

**`[BurstCompile]`** 标在 struct 和方法上，告诉 Burst 编译器把这段逻辑编译成原生机器码。移除它程序依然正确，但失去性能增益。

**`SystemAPI.Query<RefRW<Position>, RefRO<Velocity>>()`** 是遍历 Entity 的主要方式。它返回所有同时拥有 `Position` 和 `Velocity` 两个组件的 Entity 的迭代器。

- `RefRW<T>`：Read-Write 引用，表示这个查询会写入 `Position`。ECS 调度器看到 `RefRW` 就知道这个 System 对 Position 有写依赖，会据此排列 System 执行顺序，避免并行写冲突。
- `RefRO<T>`：Read-Only 引用，表示只读 `Velocity`。多个 System 可以同时读同一个组件而不冲突。

**为什么不直接用普通 `foreach` 或 `GetComponent`？** `SystemAPI.Query` 的类型信息在编译期确定，Burst 可以对它生成高效的向量化代码；而 `EntityManager.GetComponent<T>(entity)` 是逐 Entity 的随机访问，无法批量优化，也不兼容 Burst。

**`position.ValueRW.Value`** 这个链式写法：`ValueRW` 获取可写的引用，`.Value` 是我们在 `Position` struct 里定义的字段。

---

## 5. World 与 SystemGroup：执行容器和顺序

**World** 是整个 ECS 运行环境的容器。每个 World 持有：
- 一个 `EntityManager`，管理该 World 内所有 Entity 的创建、销毁、组件增删
- 一组 System，按 SystemGroup 组织

Unity 在进入 Play Mode 时自动创建一个 **Default World**，离开 Play Mode 时销毁。绝大多数情况下你只和这个 Default World 打交道。`World.DefaultGameObjectInjectionWorld` 可以在代码中拿到它的引用，但日常写 System 不需要手动访问 World。

**SystemGroup** 决定 System 的执行时机。三个默认 Group 按顺序执行：

| Group | 用途 |
|---|---|
| `InitializationSystemGroup` | 初始化、状态重置 |
| `SimulationSystemGroup` | 游戏逻辑、物理模拟 |
| `PresentationSystemGroup` | 渲染准备、动画 |

`[UpdateInGroup(typeof(SimulationSystemGroup))]` 把 `MoveSystem` 注册到模拟阶段，这是移动逻辑最自然的位置。不加这个 Attribute 也能运行，因为默认就放在 `SimulationSystemGroup`，但显式声明意图更清晰，也方便以后调整顺序。

System 不需要手动注册。只要 class 或 struct 实现了 `ISystem` 或继承了 `SystemBase`，并且处于编译范围内，Unity 就会自动在 Default World 里实例化它。

---

## 6. 验证运行结果

进入 Play Mode 后，通过两个窗口确认程序正常工作：

**Entities Hierarchy 窗口**
菜单路径：`Window → Entities → Hierarchy`

这里列出当前 World 内所有 Entity。找到由 `MoverAuthoring` 转换来的那个 Entity，展开可以看到它挂载的所有 Component，包括我们定义的 `Position` 和 `Velocity`。

**Entity Inspector**
在 Entities Hierarchy 里选中一个 Entity，右侧的 Inspector 会显示它的 Component 数据。Play Mode 运行中，你可以看到 `Position.Value` 的 X 值每帧增加——这就是 `MoveSystem` 在工作的直接证据。

如果 Entity 没出现，先检查场景里的 GameObject 是否挂了 `MoverAuthoring`，以及 Package Manager 里 `com.unity.entities` 是否正确安装。如果 Position 没变化，检查 `MoveSystem` 的 `[BurstCompile]` 是否导致了编译错误（Console 窗口里看）。

---

## 7. 和传统写法的对比

走完这个示例，可以做一个横向对比：

| 概念 | MonoBehaviour 写法 | DOTS ECS 写法 |
|------|-------------------|--------------|
| 数据定义 | class 字段（可含引用类型） | `IComponentData` struct（unmanaged） |
| 逻辑执行 | `MonoBehaviour.Update` | `ISystem.OnUpdate` |
| 对象容器 | `GameObject` | `Entity`（只是一个 ID） |
| 运行环境 | `Scene` | `World` |
| 批量处理 | `FindObjectsOfType` 逐个调用 | `SystemAPI.Query` 批量迭代 |
| 编辑器集成 | 直接使用 MonoBehaviour | Authoring + Baker 转换 |

这个对比最重要的一行是"对象容器"：`Entity` 只是一个 32 位整数 ID，它本身不持有任何数据。数据存在 Chunk 里（E01 讲过），System 通过 Query 批量访问，而不是通过 Entity 引用逐个访问。这个设计差异决定了 ECS 能做到 MonoBehaviour 做不到的 Cache 利用率。

---

## 小结

本篇的五个核心概念，一句话总结各自的职责：

- **IComponentData**：unmanaged struct，只存数据，不含逻辑
- **Baker**：Baking 阶段把 Authoring GameObject 转换为 Entity + Component，运行时不存在
- **ISystem**：unmanaged struct，每帧执行逻辑，通过 `SystemAPI.Query` 批量访问组件
- **World**：ECS 运行容器，持有 EntityManager 和所有 System
- **SystemGroup**：决定 System 执行时机，默认三组按 Initialization → Simulation → Presentation 顺序执行

代码量很少，但这五个概念的组合方式是所有 DOTS 程序的基础骨架，后续的 Job System、Burst 优化、复杂 Query 都是在这个骨架上叠加。

下一篇 E03 聚焦一个具体的选择题：**SystemBase vs ISystem——什么时候该用哪个，托管代码与非托管代码的边界在哪里**。
