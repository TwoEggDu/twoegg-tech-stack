---
title: "CPU 性能优化 04｜Unity Profiler CPU 深度分析：调用栈、GC.Alloc 定位与 HierarchyMode"
slug: "cpu-opt-04-profiler-cpu-deep"
date: "2026-03-28"
description: "Unity Profiler 的 CPU 模块包含大量信息，但大多数人只看 Total Time 列。本篇深入 Hierarchy / Timeline / RawHierarchy 三种视图的使用技巧，以及如何用 GC.Alloc 列、调用栈和自定义 Marker 精确定位性能热点。"
tags: ["Unity", "Profiler", "CPU", "性能分析", "工具"]
series: "移动端硬件与优化"
weight: 2170
---

## 三种视图的区别和用途

打开 Unity Profiler（Window → Analysis → Profiler），点击 CPU Usage 模块，左侧有三种视图切换：**Hierarchy**、**Timeline**、**Raw Hierarchy**。它们呈现的是同一份数据，但聚焦点不同。

### Hierarchy 视图：找"哪个函数贵"

Hierarchy 视图把调用树**按函数名折叠合并**。同一个函数在不同调用路径中被调用多次时，它们的耗时会被合并到同一行。

```
PlayerLoop (100%)
  └─ Update.ScriptRunBehaviourUpdate (45%)
       ├─ EnemyAI.Update [×100] (30%)    ← 100 个 Enemy，合并显示
       │    └─ Pathfinding.FindPath (25%)
       ├─ UIController.Update (8%)
       │    └─ Canvas.BuildBatch (6%)
       └─ ParticleSystem.Update (7%)
```

**关键列含义**：

| 列名        | 含义                                                |
|------------|-----------------------------------------------------|
| Total%     | 该函数（含子调用）占当帧总时间的百分比                 |
| Self%      | 该函数自身代码（不含子调用）占当帧总时间的百分比        |
| Total ms   | 绝对时间（含子调用）                                  |
| Self ms    | 自身时间（不含子调用）                                |
| GC Alloc   | 该函数（含子调用）在当帧产生的堆分配字节数              |
| Calls      | 该函数在当帧被调用的次数                               |

**优化时应该看 Self ms 高的函数**，而不是 Total ms。如果一个函数 Total 很高但 Self 很低，说明它只是一个"传递者"，热点在它的子调用里。

**实操技巧**：

```
1. 点击列头 "Self ms" 排序（降序）
2. 最顶部的条目就是当帧真正的 CPU 热点
3. 展开该条目查看调用来源（谁调用了它）

另：点击列头 "GC Alloc" 排序
→ 快速找到当帧分配最多内存的函数
```

### Timeline 视图：找"帧内的并发和等待关系"

Timeline 视图用横向时间轴展示，每一行是一个线程，可以看到：
- 主线程、渲染线程、Worker Thread 的并发情况
- 各个阶段的起止时间（例如 Physics 在哪段时间运行）
- `WaitForEndOfFrame`、`WaitForJobsToComplete` 等等待事件在哪里
- GC.Collect 事件的位置和持续时间

```
主线程  ████ Update ████ Render ██WaitGPU██
渲染线程         ████████████ Present
Worker       ████ Job ████ Job ████
```

**Timeline 的独特价值**：

```
场景 1：主线程在等 GPU（WaitForGPU 很长）
→ 瓶颈在 GPU，优化 Draw Call 或 Shader

场景 2：Worker Thread 空闲，主线程很忙
→ 逻辑没有并行化，考虑 Jobs

场景 3：GC.Collect 事件周期性出现
→ 找它前面的 GC.Alloc 来源（见下文）

场景 4：Physics.Processing 很长，但代码里没有复杂物理
→ 可能有大量静态碰撞体或 Rigidbody，检查场景设置
```

**在 Timeline 中放大特定时间段**：鼠标滚轮缩放，按住 Alt 拖动平移。找到可疑的色块后，点击它可以在右侧面板看到函数名和耗时。

### Raw Hierarchy 视图：找"重复调用同一函数的地方"

Raw Hierarchy **不折叠调用路径**，每一个调用都独立显示。这在以下场景非常有用：

**场景：同一函数从不同地方被调用，想知道各自的代价**

```
# Hierarchy 视图（折叠后）：
Pathfinding.FindPath  Total:25ms  Calls:100

# Raw Hierarchy 视图（不折叠）：
EnemyAI.Update
  └─ Pathfinding.FindPath  Total:20ms  Calls:80

TowerAI.Update
  └─ Pathfinding.FindPath  Total:5ms   Calls:20
```

在 Raw Hierarchy 下可以清楚看到：80% 的寻路开销来自 EnemyAI，而不是 TowerAI。Hierarchy 视图把它们合并了，无法区分。

---

## 关键列的深度理解

### Total Time vs Self Time 的实际应用

```
PlayerLoop
  └─ Update.ScriptRunBehaviourUpdate  Total:30ms  Self:0.1ms
       └─ EnemyAI.Update              Total:29ms  Self:2ms
            └─ Pathfinding.FindPath   Total:25ms  Self:22ms
                 └─ NavMesh.Sample    Total:3ms   Self:3ms
```

分析这棵树：
- `ScriptRunBehaviourUpdate`：Self 只有 0.1ms，它只是一个调度器，热点不在这里
- `EnemyAI.Update`：Self 2ms，说明 EnemyAI 自己的代码有 2ms 开销（除了调用 FindPath 之外）
- `Pathfinding.FindPath`：Self 22ms，这才是真正的热点，应该优先优化这个函数
- `NavMesh.Sample`：Self 3ms，FindPath 里调用的 Unity API，可以考虑缓存结果

### Call Count：高频调用的瓶颈

```
GC.Alloc        Total:0.8ms  Self:0.8ms  Calls:1500  GCAlloc:45KB
```

这表示某个函数**被调用了 1500 次**，每次分配约 30 bytes。即使单次分配很小，累计也产生 45KB 的 GC 压力。找到这个函数，优化零分配写法（见第 01 篇）。

**Call Count 异常高的常见原因**：
- LINQ 链式调用（多个迭代器嵌套）
- 递归函数没有提前终止条件
- 事件系统广播频率过高
- `FindObjectOfType` / `GetComponent` 在 Update 中每帧调用

### GC Alloc 列的展开技巧

在 Hierarchy 视图中，点击 **GC Alloc** 列头排序后：

1. 展开最顶部的条目（GC 分配最多的函数）
2. 继续展开子条目，找到叶节点——那就是实际触发 `new` 的代码
3. 双击该条目，如果是 Development Build 或编辑器模式，会跳转到对应的 C# 源码行

```
# 示例：展开 GC Alloc
UIManager.Update            GCAlloc:12KB
  └─ RefreshInventory        GCAlloc:12KB
       └─ GetItemsInSlot      GCAlloc:12KB   ← 继续展开
            └─ LINQ.Where     GCAlloc:8KB    ← 叶节点：LINQ 分配枚举器
            └─ List.ToArray   GCAlloc:4KB    ← 叶节点：ToArray 分配新数组
```

找到后，把 `LINQ.Where().ToArray()` 改为直接遍历预分配 List，GC 压力消除。

---

## 自定义 Profiler Marker

内置 Marker 只覆盖 Unity 引擎代码，业务代码需要手动插入 Marker 才能在 Profiler 中定位。

### ProfilerMarker 的正确用法

```csharp
using Unity.Profiling;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // 正确：static readonly，确保只创建一次
    // ProfilerMarker 是 struct，复用同一个实例避免重复字符串查找
    private static readonly ProfilerMarker s_DamageCalcMarker =
        new ProfilerMarker(ProfilerCategory.Scripts, "CombatSystem.DamageCalc");

    private static readonly ProfilerMarker s_HitDetectMarker =
        new ProfilerMarker(ProfilerCategory.Scripts, "CombatSystem.HitDetect");

    // 错误：每帧 new ProfilerMarker（产生 GC 分配 + 字符串哈希计算）
    // void Update() { new ProfilerMarker("...").Begin(); }

    void Update()
    {
        // 方式 1：手动 Begin/End（性能最佳，适合已知不会抛异常的路径）
        s_HitDetectMarker.Begin();
        int hitCount = DetectHits();
        s_HitDetectMarker.End();

        // 方式 2：using RAII（更安全，即使抛异常也会 End）
        using (s_DamageCalcMarker.Auto())
        {
            for (int i = 0; i < hitCount; i++)
                CalculateDamage(i);
        }

        // 方式 3：带参数的 Marker（在 Profiler 中显示额外信息）
        s_DamageCalcMarker.Begin(this); // 第二个参数是 UnityEngine.Object，Timeline 中可点击跳转
    }
}
```

### 为 Native 代码添加 Marker（C++ 侧）

如果项目有 Native Plugin（.so/.dll），可以在 C++ 中插入 Marker，它们会出现在 Profiler Timeline 的主线程上：

```cpp
// 需要包含 Unity 的 Profiler API 头文件
#include "IUnityProfiler.h"

static IUnityProfiler* s_UnityProfiler = nullptr;
static const UnityProfilerMarkerDesc* s_MyMarker = nullptr;

// 初始化（在 UnityPluginLoad 中）
extern "C" void UNITY_INTERFACE_EXPORT UnityPluginLoad(IUnityInterfaces* interfaces)
{
    s_UnityProfiler = interfaces->Get<IUnityProfiler>();
    if (s_UnityProfiler)
    {
        s_UnityProfiler->CreateMarker(
            &s_MyMarker, "MyPlugin.HeavyWork",
            kUnityProfilerCategoryScripts, kUnityProfilerMarkerFlagDefault, 0);
    }
}

// 使用（RAII 宏）
void DoHeavyWork()
{
    if (s_UnityProfiler) s_UnityProfiler->BeginSample(s_MyMarker);

    // ... 实际工作 ...

    if (s_UnityProfiler) s_UnityProfiler->EndSample(s_MyMarker);
}
```

### ProfilerMarker 的嵌套

Profiler 支持 Marker 嵌套，在 Timeline 视图中会形成层级色块：

```csharp
private static readonly ProfilerMarker s_FrameMarker =
    new ProfilerMarker("MySystem.Frame");
private static readonly ProfilerMarker s_PhysicsMarker =
    new ProfilerMarker("MySystem.PhysicsStep");
private static readonly ProfilerMarker s_AIMarker =
    new ProfilerMarker("MySystem.AIStep");

void Update()
{
    using (s_FrameMarker.Auto())        // 外层
    {
        using (s_PhysicsMarker.Auto())  // 内层 1
            RunPhysics();

        using (s_AIMarker.Auto())       // 内层 2
            RunAI();
    }
}
```

Timeline 中显示：
```
[────────────── MySystem.Frame ──────────────]
[─ MySystem.PhysicsStep ─][─ MySystem.AIStep ─]
```

---

## 定位 GC.Alloc 的完整流程

### 第 1 步：开启 Allocation Callstacks

默认情况下，Profiler 只记录 GC.Alloc 的大小，不记录调用栈。要看调用栈需要：

```
Profiler 窗口 → Allocation Callstacks 下拉菜单 → GC.Alloc
```

**代价**：开启后游戏性能下降约 30-50%，但 GC 分配信息最完整。只在定位问题时开启，平时关闭。

Unity 2021+ 还提供了 **Sample Allocations**（按比例采样），减少性能影响：

```
Allocation Callstacks → Sample Allocations (Every Nth Alloc)
```

### 第 2 步：在 Timeline 中找 GC.Collect

1. 切换到 **Timeline 视图**
2. 选择主线程（Main Thread）行
3. 搜索 `GC.Collect` 事件（颜色通常是红色或橙色）
4. 点击 GC.Collect 事件，右侧显示它前面几毫秒内的分配情况
5. 向左查看 GC.Collect 之前的时间段，找到"分配量突然增大"的时间点

```
主线程时间轴：
... [Update] ... [GC.Alloc 12KB] ... [GC.Alloc 8KB] ... [GC.Collect 6ms] ...
                 ↑ 这两次分配导致堆不够用，触发了 GC.Collect
```

### 第 3 步：切换到 Hierarchy 查看具体分配来源

1. 找到 GC.Collect 所在帧
2. 切换回 **Hierarchy 视图**
3. 按 **GC Alloc 列降序排序**
4. 逐层展开，找到叶节点的分配来源

### 第 4 步：Memory Profiler 交叉验证

对于偶发性大分配（不是每帧都有的），Memory Profiler 更有用：

1. 安装 Memory Profiler 包（Package Manager → com.unity.memoryprofiler）
2. Window → Analysis → Memory Profiler
3. 在问题复现时点击 **Capture New Snapshot**
4. 在 **Allocations** 视图中按 Size 排序
5. 点击某个分配，右侧显示完整的调用栈

### 第 5 步：用 ProfilerRecorder 验证修复效果

修复分配问题后，用代码验证每帧分配确实降到 0：

```csharp
using Unity.Profiling;
using UnityEngine;

public class GCAllocValidator : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private ProfilerRecorder _gcAllocRecorder;
    private int _warningThresholdBytes = 1024; // 超过 1KB 报警

    void OnEnable()
    {
        _gcAllocRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC.Alloc", 1);
    }

    void OnDisable()
    {
        _gcAllocRecorder.Dispose();
    }

    void LateUpdate()
    {
        long allocThisFrame = _gcAllocRecorder.LastValue;
        if (allocThisFrame > _warningThresholdBytes)
        {
            Debug.LogWarning(
                $"[GC] Frame {Time.frameCount}: {allocThisFrame} bytes allocated!");
        }
    }
#endif
}
```

---

## PlayerLoop 的结构

Unity 的帧循环（PlayerLoop）是一个层级系统。理解它的结构，才能看懂 Profiler 树形结构中各个节点的来源。

### PlayerLoop 的主要阶段

```
PlayerLoop
├─ Initialization          // 首帧初始化，后续帧很快
├─ EarlyUpdate             // 输入事件（Input）、网络消息处理
│   ├─ UnityWebRequestUpdate
│   ├─ ExecuteMainThreadJobs
│   └─ UpdatePreloading
├─ FixedUpdate             // 物理模拟（可能执行多次）
│   ├─ PhysicsFixedUpdate
│   ├─ Physics2DFixedUpdate
│   └─ ScriptRunBehaviourFixedUpdate  ← MonoBehaviour.FixedUpdate 在这里
├─ PreUpdate               // 物理同步到 Transform 前的处理
│   ├─ PhysicsUpdate       // 物理结果写回 Transform
│   └─ Physics2DUpdate
├─ Update                  // 主逻辑更新
│   ├─ ScriptRunBehaviourUpdate  ← MonoBehaviour.Update 在这里
│   ├─ DirectorUpdate
│   └─ ScriptRunDelayedDynamicFrameRate  ← InvokeRepeating 在这里
├─ PreLateUpdate           // Animator 状态机评估
│   ├─ DirectorUpdateAnimationBegin
│   ├─ LegacyAnimationUpdate
│   └─ ScriptRunBehaviourLateUpdate  ← MonoBehaviour.LateUpdate 在这里
└─ PostLateUpdate          // 渲染提交
    ├─ UpdateAllRenderers
    ├─ PlayerSendFramePostPresent
    └─ GarbageCollectAssetsIfNeeded  ← Resources.UnloadUnusedAssets 在这里
```

**Profiler 树对应**：Profiler 的 Hierarchy 视图就是这棵树的展开，每个节点对应一个 PlayerLoop 子系统。当你看到 `PreLateUpdate.ScriptRunBehaviourLateUpdate` 占用很多时间时，就知道是 LateUpdate 代码有问题。

### 自定义 PlayerLoop 阶段

高级用法：在特定位置插入自定义系统（绕过 MonoBehaviour）：

```csharp
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class CustomPlayerLoop
{
    struct MyCustomUpdate { } // 用 struct 作为 PlayerLoopSystem 的类型标识

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Install()
    {
        var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

        // 找到 Update 阶段
        for (int i = 0; i < playerLoop.subSystemList.Length; i++)
        {
            if (playerLoop.subSystemList[i].type == typeof(Update))
            {
                var updateSystem = playerLoop.subSystemList[i];
                var subSystems = new System.Collections.Generic.List<PlayerLoopSystem>(
                    updateSystem.subSystemList);

                // 在 Update 最开始插入自定义系统
                subSystems.Insert(0, new PlayerLoopSystem
                {
                    type = typeof(MyCustomUpdate),
                    updateDelegate = MyEarlyUpdate
                });

                playerLoop.subSystemList[i].subSystemList = subSystems.ToArray();
                break;
            }
        }

        PlayerLoop.SetPlayerLoop(playerLoop);
    }

    static void MyEarlyUpdate()
    {
        // 比所有 MonoBehaviour.Update 更早执行
        // 不占用 Native-Managed 桥接计数（但仍然是一次桥接）
    }
}
```

---

## 跨帧分析

### 选取多帧对比

1. 在 Profiler 顶部帧序列中，按住 Shift 点击选择多帧范围
2. Hierarchy 视图会显示这些帧的**平均值**
3. 或右键 → **Select Last N Frames**（N 可以是 5/10/30/60）

**用途**：
- 单帧分析可能选到不典型的帧（恰好触发 GC 的帧）
- 多帧平均能反映稳定状态下的真实开销

### 识别周期性 GC 峰值

选取 60 帧的时间窗口，查看 GC Alloc 列的变化：

```
Frame  1: GC Alloc 0 bytes
Frame  2: GC Alloc 0 bytes
...
Frame 28: GC Alloc 12 KB  ← 周期性峰值
...
Frame 56: GC Alloc 11 KB  ← 大约每 28 帧一次
```

周期性分配通常来自：
- `InvokeRepeating`（每 N 秒触发一次逻辑）
- 动画事件（每几帧触发一次回调）
- 定时器触发的 UI 刷新
- 网络消息到来时的反序列化

### 使用 Profiler Recorder 在游戏中实时收集

在真实设备上不方便连接 Profiler 时，用 ProfilerRecorder 在游戏内显示/记录关键指标：

```csharp
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using System.Text;

public class InGameProfiler : MonoBehaviour
{
    // 需要监控的指标
    private ProfilerRecorder _setPassCallsRecorder;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _gcAllocRecorder;
    private ProfilerRecorder _mainThreadTimeRecorder;

    private StringBuilder _statsBuilder = new StringBuilder(256);

    void OnEnable()
    {
        _setPassCallsRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "SetPass Calls Count", 15);
        _drawCallsRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Draw Calls Count", 15);
        _trianglesRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Triangles Count", 15);
        _gcAllocRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC.Alloc", 15);
        _mainThreadTimeRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal, "Main Thread", 15);
    }

    void OnDisable()
    {
        _setPassCallsRecorder.Dispose();
        _drawCallsRecorder.Dispose();
        _trianglesRecorder.Dispose();
        _gcAllocRecorder.Dispose();
        _mainThreadTimeRecorder.Dispose();
    }

    // 计算最近 N 帧平均值
    static double GetRecorderAverage(ProfilerRecorder recorder)
    {
        int count = recorder.Capacity;
        if (count == 0) return 0;
        double sum = 0;
        for (int i = 0; i < count; i++)
            sum += recorder.GetSample(i).Value;
        return sum / count;
    }

    void Update()
    {
        // 每 30 帧更新一次统计（避免 Update 本身成为开销）
        if (Time.frameCount % 30 != 0) return;

        _statsBuilder.Clear();
        double frameMs = GetRecorderAverage(_mainThreadTimeRecorder) * 1e-6;
        long gcAlloc = _gcAllocRecorder.LastValue;

        _statsBuilder.AppendLine($"Frame Time: {frameMs:F2} ms");
        _statsBuilder.AppendLine($"GC Alloc/Frame: {gcAlloc} B");
        _statsBuilder.AppendLine($"SetPass: {_setPassCallsRecorder.LastValue}");
        _statsBuilder.AppendLine($"Draw Calls: {_drawCallsRecorder.LastValue}");
        _statsBuilder.Append($"Triangles: {_trianglesRecorder.LastValue / 1000}K");

        // 可以写入文件或显示在 UI 上
        Debug.Log(_statsBuilder);
    }
}
```

**保存到文件（设备上运行时）**：

```csharp
// 把性能数据写入持久化目录，事后分析
string path = System.IO.Path.Combine(Application.persistentDataPath, "perf_log.csv");
System.IO.File.AppendAllText(path,
    $"{Time.frameCount},{frameMs:F3},{gcAlloc}\n");
```

---

## 常见 Profiler 误读和排查

### 误读 1：看到高 Total Time 就以为找到热点

```
# 误判
Physics.Processing  Total:8ms  Self:0.1ms
  └─ Rigidbody.Simulate  Total:7.9ms  Self:7.9ms

# 实际热点是 Rigidbody.Simulate，不是 Physics.Processing
# Physics.Processing 的 Self 只有 0.1ms，它只是入口
```

**正确做法**：始终以 **Self ms** 降序排序，找 Self 高的函数。

### 误读 2：把 Editor Overhead 当作游戏性能

Unity Editor 有额外的开销（Inspector 刷新、Scene 视图渲染等），导致 Editor 中的 Profiler 数据比设备上高 20-50%。

**正确做法**：用 **Development Build** 连接真实设备分析，或至少在 Editor 中关闭 Scene 视图（最小化 Editor 窗口）。

### 误读 3：忽略 Vsync 等待时间

```
# 开启了 Vsync 时
WaitForTargetFPS   Total:8ms  ← 等待显示器刷新，不是真实开销
Rendering          Total:3ms
Update             Total:5ms
```

如果开启了 VSync，`WaitForTargetFPS` 会占很大一部分 Total。实际的 CPU 工作时间是其他项目的总和。**性能分析时应关闭 VSync**（或在分析 CPU 时排除等待时间）。

### 误读 4：混淆 GPU 时间和 CPU 时间

在 CPU 模块看到 `Camera.Render` 耗时很高，但这包含了 CPU 提交命令的时间（实际很低）和等待 GPU 完成的时间（如果开了同步）。

**正确做法**：同时打开 GPU Usage 模块，对比 CPU 时间（提交 Draw Call 的时间）和 GPU 时间（实际渲染的时间）。

---

## 总结：Profiler 分析的标准流程

```
1. 连接目标设备（真机 Development Build）

2. 录制 100+ 帧，找到典型的"卡顿帧"
   → 帧序列中明显高的峰值

3. 选中卡顿帧，Hierarchy 视图按 Self ms 降序
   → 找到 CPU 热点函数

4. 同一帧，按 GC Alloc 降序
   → 找到 GC 分配来源，展开到叶节点

5. 切换 Timeline 视图
   → 确认是否有 GC.Collect、WaitForGPU、Jobs 等待

6. 插入 ProfilerMarker，缩小热点范围到具体业务代码

7. 修复 → 重新录制 → 对比修复前后的 Self ms 和 GC Alloc

8. 选取 60 帧平均值确认修复效果，而非单帧
```

掌握 Profiler 的三种视图和关键列的含义，配合自定义 Marker，可以把性能瓶颈的定位时间从"数小时猜测"缩短到"数分钟精确定位"。
