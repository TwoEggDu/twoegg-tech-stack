---
title: "Unity DOTS E10｜SubScene 与流式加载：大世界的内容单元、生命周期与内存管理"
slug: "dots-e10-subscene-streaming"
date: "2026-03-28"
description: "SubScene 是 DOTS 的内容流式加载单元，和传统 Addressable Scene 的根本区别在于它加载的是预烘焙的 Entity 数据而非 GameObject。本篇讲清楚 SubScene 的加载生命周期、内存管理，以及大世界场景管理的工程模式。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "SubScene"
  - "流式加载"
  - "大世界"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 10
weight: 1900
---

传统 Unity 项目做大世界时，绕不开 Addressable Scene 的加载延迟——每次加载都要跑完整的 GameObject 实例化、Awake、Start 链路，光是 MonoBehaviour 的初始化开销就足以让帧率抖动。DOTS 的解法是从数据源头切断这个链路：SubScene 在构建期把场景内容烘焙成二进制的 Entity 快照，运行时加载只是把这份快照反序列化进 World，没有实例化，没有 Baking Pipeline，也没有 MonoBehaviour 生命周期。

---

## SubScene 是什么

SubScene 是一个挂在 GameObject 上的组件，它引用一个普通的 Unity Scene Asset。这个 Scene Asset 的用途不是在运行时直接加载，而是作为 Baking 的输入源。

当你保存 SubScene（或执行构建）时，Unity 的 Baking Pipeline 把 Scene 里所有 GameObject 转换成 Entity + Component 数据，写出为 `.entities` 二进制文件（以及配套的 `.entityheader`）。运行时，`SceneSystem` 加载的是这份 `.entities` 文件，和原始 Scene Asset 没有任何运行时关联。

这意味着：

- SubScene 里的 GameObject 只在编辑器里存在，它们是 Baking 的「模具」
- 运行时的 Entity 数据完全独立，修改 `.entities` 文件不会影响编辑器里的 GameObject
- 构建包里只打包 `.entities` 文件，原始 Scene Asset 不包含在内（可以走 Addressable 分发）

---

## 和 Addressable Scene 的本质区别

理解这个区别对性能决策至关重要。

**Addressable Scene 的加载路径：**

```
加载 .unity 文件
  → 实例化 GameObject 层级
  → 执行 Awake / OnEnable
  → 执行 Start
  → MonoBehaviour 开始运行
```

每个 GameObject 都是堆对象，每个 MonoBehaviour 都要走完整的 C# 生命周期。一个包含 5000 个静态道具的场景，光是 Awake 阶段就可能消耗数十毫秒。

**SubScene 的加载路径：**

```
加载 .entities 文件（内存映射读取）
  → 反序列化 Chunk 数据
  → Entity + Component 写入 World
  → 完成
```

加载过程是纯内存操作：按 Chunk 布局批量写入 ComponentData，没有 C# 对象分配，没有虚函数调用，没有 MonoBehaviour。加载速度接近于文件 IO 的上限。

另一个关键差异是**确定性**：SubScene 的 Entity 数据在 Baking 时已经确定，运行时不会因为脚本逻辑而产生差异；而 Addressable Scene 的最终状态依赖 Awake/Start 的执行顺序，调试困难。

---

## 加载生命周期

SubScene 的状态机如下：

```
Closed（未加载）
  → Loading（异步加载中）
  → Loaded（Entity 已在 World 里）
  → Unloading（正在清理）
  → Closed
```

核心 API 在 `SceneSystem` 里（`Unity.Scenes` 命名空间）：

```csharp
using Unity.Entities;
using Unity.Scenes;

public partial class SubSceneLoaderSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 通常在初始化或距离检测时调用一次，不要每帧都调
    }

    // 加载一个 SubScene（通过 SceneReference）
    public void Load(SceneReference sceneRef)
    {
        var sceneEntity = SceneSystem.LoadSceneAsync(
            World.Unmanaged,
            sceneRef,
            new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.NewInstance
            });
    }

    // 卸载
    public void Unload(Entity sceneEntity)
    {
        SceneSystem.UnloadScene(World.Unmanaged, sceneEntity);
    }
}
```

检测加载完成不能用回调，需要在 System 里轮询：

```csharp
public partial class SceneLoadCheckSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 遍历所有正在加载的场景实体
        foreach (var (state, entity) in
            SystemAPI.Query<RefRO<SceneReference>>()
                     .WithEntityAccess())
        {
            if (SceneSystem.IsSceneLoaded(World.Unmanaged, entity))
            {
                // SubScene 已加载完成，可以查询其中的 Entity
                // 做一次性初始化后可以用 EntityManager 移除标记组件
            }
        }
    }
}
```

加载完成的时机由文件大小和磁盘 IO 速度决定，通常是异步的——不要在发起加载的同一帧就假设数据已就绪。

---

## 内存管理

SubScene 加载后，其中所有 Entity 和 ComponentData 都存放在 World 的 Chunk 内存里，和手动用 `EntityManager.CreateEntity()` 创建的 Entity 没有任何结构上的区别。Chunk 按 Archetype 组织，SubScene 的内容会合并进全局的 Chunk 池。

**卸载时的清理机制：**

卸载 SubScene 时，`SceneSystem` 会找出所有属于该 SubScene 的 Entity（通过内部的场景标签组件），批量销毁它们。这等价于对该场景的所有 Entity 执行 `EntityManager.DestroyEntity()`——Chunk 内存回收，Component 数据清零。

几个需要注意的内存行为：

1. **SubScene 之间不共享 Chunk**：即使两个 SubScene 里有相同 Archetype 的 Entity，它们的 Chunk 也是独立分配的，不会因为卸载其中一个而破坏另一个的 Chunk 布局
2. **Blob Asset 的生命周期独立**：SubScene 可以引用 Blob Asset，Blob Asset 有自己的引用计数，卸载 SubScene 不会立即释放被共享的 Blob Asset
3. **Shared Component 数据**：SubScene 里如果用了 Shared Component，卸载后对应的 Shared Component 值会在 Shared Component 版本号系统里保留，直到没有 Entity 再引用它

---

## 大世界流式加载模式

SubScene 是大世界流式加载的基本单元。典型的工程模式是按地图区域划分 SubScene，用玩家位置决定哪些区域需要加载：

```csharp
public partial class WorldStreamingSystem : SystemBase
{
    // 存储 SubScene 实体和对应的区域中心
    private NativeList<(Entity sceneEntity, float3 center)> _regions;

    protected override void OnUpdate()
    {
        var playerPos = GetPlayerPosition();
        float loadRadius = 200f;
        float unloadRadius = 250f;

        for (int i = 0; i < _regions.Length; i++)
        {
            var (sceneEntity, center) = _regions[i];
            float dist = math.distance(playerPos, center);
            bool isLoaded = SceneSystem.IsSceneLoaded(World.Unmanaged, sceneEntity);

            if (dist < loadRadius && !isLoaded)
            {
                SceneSystem.LoadSceneAsync(World.Unmanaged, sceneEntity);
            }
            else if (dist > unloadRadius && isLoaded)
            {
                SceneSystem.UnloadScene(World.Unmanaged, sceneEntity);
            }
        }
    }

    float3 GetPlayerPosition()
    {
        // 从玩家 Entity 读取 LocalTransform
        foreach (var transform in
            SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<PlayerTag>())
        {
            return transform.ValueRO.Position;
        }
        return float3.zero;
    }
}
```

### Section：SubScene 的子粒度

一个 SubScene 可以拆分成多个 **Section**（通过在 SubScene 内放置 `SceneSectionComponent` 标记）。Section 是比 SubScene 更细的加载粒度，允许你只加载一个 SubScene 的部分内容：

```csharp
// 只加载 SubScene 的第 0 节（默认节，包含必要数据）
var loadParams = new SceneSystem.LoadParameters
{
    Flags = SceneLoadFlags.NewInstance,
    // Section 0 总是会加载；其他 Section 可以按需请求
};

// 请求加载特定 Section
// 通过给 SceneSectionEntity 添加 RequestSceneLoaded 组件实现
EntityManager.AddComponent<RequestSceneLoaded>(sectionEntity);
```

Section 的典型用法：Section 0 放区域的核心逻辑实体（触发器、AI 节点），Section 1 放高精度视觉 Mesh，玩家进入区域时先加载 Section 0，摄像机进入时再加载 Section 1。

---

## 编辑器工作流

SubScene 的编辑体验刻意模仿了 Prefab 编辑模式：

- **双击 SubScene 组件** 进入编辑模式，SubScene 内的 GameObject 变为可编辑状态
- **在编辑模式里修改** GameObject，退出时（点击场景层级里的返回按钮）自动触发 Baking，更新 `.entities` 文件
- **Live Baking**：编辑器里打开 SubScene 时，World 里同时存在该 SubScene 对应的 Entity，每次保存都会实时更新——你可以在 Entity Debugger 里看到 Baking 结果即时刷新

这套工作流的含义是：**你永远不直接编辑 `.entities` 文件**，所有修改都通过 GameObject 层做，Baking 是单向的、自动的。

---

## 注意事项

**1. SubScene 内的 GameObject 引用**

SubScene 里的 Baker 不能直接把 `GameObject` 或 `MonoBehaviour` 引用序列化进 Entity（运行时没有 GameObject）。需要通过以下方式处理：

- **Prefab 引用**：用 `GetEntity(prefab, TransformUsageFlags.Dynamic)` 把 Prefab 转换成 Entity 引用
- **只读静态数据**：用 Blob Asset 打包（E11 会专门讲）
- **运行时资产引用**（Texture、Mesh 等）：用 `WeakObjectReference<T>` 延迟加载

**2. Cross-SubScene Entity 引用**

如果 SubScene A 里的 Entity 持有 SubScene B 里某个 Entity 的引用（存在 ComponentData 里），当 B 被卸载时，A 里的 Entity Reference 会变成悬空引用（指向已销毁的 Entity）。

处理策略：
- 尽量让 SubScene 之间的依赖单向，或者完全解耦
- 用弱引用模式：存 Entity 的稳定标识符（如自定义的 ID），查询时先检查 Entity 是否存活（`EntityManager.Exists(entity)`）
- 卸载前发送事件，让依赖方提前清理引用

**3. 加载时机与首帧安全**

`SceneSystem.LoadSceneAsync` 是异步的，当帧内无法查询加载结果。不要在 `OnCreate` 里发起加载后立即查询，要在后续帧通过 `IsSceneLoaded` 确认。

---

## 小结

SubScene 把「内容」和「运行时数据」彻底分离：编辑器里操作 GameObject，运行时只有 Entity 数据。这个设计让流式加载的性能开销降到接近理论下限，也让大世界的内存管理有了清晰的边界——一个 SubScene 就是一个独立的内存单元，加载即写入，卸载即销毁。

Section 机制进一步把加载粒度从「场景」细化到「场景内的逻辑分组」，配合距离检测系统，可以实现非常细腻的 LOD 式内容调度。

下一篇 **E11「Blob Asset」** 会讲 SubScene 里只读静态数据的标准存储方案：当你有大量不变的表格数据、曲线、导航网格片段需要在多个 Entity 之间共享时，Blob Asset 是比 ComponentData 更合适的容器，而且它和 SubScene 的 Baking Pipeline 深度集成。