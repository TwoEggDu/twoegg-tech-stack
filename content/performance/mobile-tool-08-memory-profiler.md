---
title: "性能分析工具 08｜Unity Memory Profiler：Snapshot 对比、Native 对象追踪与内存泄漏定位"
slug: "mobile-tool-08-memory-profiler"
date: "2026-03-28"
description: "Unity Memory Profiler 不是 Profiler 窗口的内存页——它是一个专门捕获和分析内存快照的独立工具，能看到托管堆的每一个对象、Native 分配的持有链，以及场景加载前后的内存增量。本篇从安装配置到 Snapshot 对比分析，给出完整的内存泄漏排查流程。"
tags:
  - "Memory"
  - "Profiler"
  - "Unity"
  - "内存泄漏"
  - "移动端"
series: "移动端硬件与优化"
weight: 2180
---

Unity 的内存问题通常有两种表现：一种是崩溃前的 OOM，一种是长时间运行后帧率下降——后者往往来自内存碎片或泄漏导致的 GC 压力升高。这两种情况都需要比 Profiler 窗口内存页更深的工具。

Unity Memory Profiler（独立 Package）就是为此设计的。

---

## 一、Memory Profiler vs Profiler 窗口内存页

两个工具的定位完全不同：

```
Unity Profiler → 内存页：
  显示粒度：每帧的内存使用计数器（总量 / GC Alloc）
  适合用途：运行时监控内存趋势，发现异常增长帧
  看不到的内容：具体是哪个对象占用，对象之间的引用关系

Unity Memory Profiler（Package）：
  显示粒度：完整的内存快照，每个对象的大小和引用链
  适合用途：定位内存泄漏，分析对象存活原因，比较两个时间点的差异
  看不到的内容：实时数据（只能看快照时刻的状态）
```

两者配合使用的工作流：**Profiler 监控趋势 → 发现内存异常增长 → Memory Profiler 拍快照 → 定位具体对象**。

---

## 二、安装与连接

### 安装

```
Package Manager → Add package by name:
  com.unity.memoryprofiler

支持的 Unity 版本：2019.4+
推荐版本：2022.3 LTS（Memory Profiler 1.1+，功能最完整）

打开方式：Window → Analysis → Memory Profiler
```

### 连接设备

Memory Profiler 通过 Profiler 连接通道获取数据，前提条件：

```
1. 勾选 Development Build
2. 勾选 Autoconnect Profiler（或在 Profiler 窗口手动连接设备 IP）
3. 在 Unity Editor 的 Memory Profiler 窗口左上角选择连接目标
   → 可以选择 Editor 本身，也可以选择已连接的设备进程
```

如果设备没有出现在列表：

```bash
# 确认 adb 能看到设备
adb devices

# 通过 adb forward 转发 Profiler 端口（部分网络环境需要）
adb forward tcp:34999 localabstract:Unity-com.yourcompany.game
```

---

## 三、Snapshot 对比：找增量

内存泄漏的核心排查方法是**对比两个快照的差异**，而不是分析单个快照的绝对大小。

### 标准工作流

```
操作序列（以背包界面泄漏为例）：

1. 进入游戏主界面，等待内存稳定
2. 拍快照 A（Memory Profiler → Capture → Take Snapshot）
3. 打开背包 → 关闭背包（重复 3-5 次）
4. 拍快照 B
5. 在 Memory Profiler 窗口点击 "Compare Snapshots"
6. 选择 A 为 Baseline，B 为 Comparison
```

### 解读 Diff 结果

Diff 视图的三列对象状态：

```
New（蓝色）：
  快照 B 中有，A 中没有
  → 这段时间内新分配且未释放的对象
  → 泄漏候选（但并非全部都是泄漏）

Deleted（灰色）：
  快照 A 中有，B 中没有
  → 已经被正确释放
  → 这是期望行为

Same（白色）：
  两个快照都有，且内容相同
  → 正常的持久对象（静态资产、常驻管理器）
```

关键指标：`Count` 列（对象数量变化）+ `Size` 列（内存大小变化）

**典型泄漏特征**：Diff 视图里 `Texture2D` 或 `GameObject` 的 New 数量在每次操作后线性增长。

---

## 四、托管堆分析：理解 C# 对象为什么还活着

GC 只会回收没有任何引用指向的对象。如果一个对象泄漏，意味着一定有某条引用链从 GC Root 到达它。

### GC Root 的类型

```
常见 GC Root（持有对象存活的源头）：

  静态字段（最常见的泄漏来源）：
    static List<T> 静态集合忘记清理
    static Dictionary<K,V> 缓存无限增长

  C# 事件订阅（event += 未对应 -=）：
    事件发布者持有订阅者的委托引用
    订阅者被 Destroy 后，委托里的 this 指针仍然让 GC 认为对象存活

  Coroutine（协程）：
    正在运行的协程让整个 MonoBehaviour 存活
    yield return 等待外部条件时，如果条件永不满足 → 协程永不结束

  GCHandle（显式 Pin）：
    与 Native 代码交互时 Pin 的对象
    如果没有释放 GCHandle → 对象永远不会被回收
```

### 追踪引用链

在 Memory Profiler 里点击某个泄漏的对象，切换到 **References** 标签：

```
References To（谁引用了它）：
  往上追溯，直到找到 GC Root
  → Root 通常是静态字段或活跃的 MonoBehaviour

References From（它引用了谁）：
  看这个对象持有了哪些其他对象
  → 帮助理解一个泄漏对象还拖着多少附带内存
```

### 常见托管堆泄漏模式

```csharp
// ❌ Pattern 1：静态集合持有场景对象
public static class UIManager
{
    // 场景卸载后，这个 List 仍然引用已销毁场景里的 Button
    public static List<Button> allButtons = new List<Button>();
}
// 修复：在 Scene.sceneUnloaded 事件里 allButtons.Clear()

// ❌ Pattern 2：C# event 订阅未取消
void OnEnable() {
    GameEvents.OnLevelComplete += HandleComplete;
}
// 没有对应的 OnDisable：
// void OnDisable() {
//     GameEvents.OnLevelComplete -= HandleComplete;  ← 漏写
// }
// 对象 Destroy 后，GameEvents 仍然持有 HandleComplete 的引用
// → 整个 MonoBehaviour 及其持有的所有对象都无法被回收

// ✅ 正确做法：
void OnDisable() {
    GameEvents.OnLevelComplete -= HandleComplete;
}

// ❌ Pattern 3：Lambda 捕获了不应存活的对象
void Start() {
    // lambda 隐式捕获了 this
    Timer.OnTick += () => UpdateUI();  // 相当于 this.UpdateUI
}
// 如果 Timer 是静态的，它会通过 lambda 持有 this
// → this 对应的 MonoBehaviour 永远不会被 GC
```

---

## 五、Native 对象追踪

Unity 的很多对象在 C# 层只是一个薄包装，真正的内存在 Native 层：

```
C# 对象（托管堆）  →  Native 对象（Unity 引擎内存）
Texture2D           →  像素数据（可能数 MB）
Mesh                →  顶点/索引 Buffer
AudioClip           →  PCM 音频数据
RenderTexture       →  GPU 帧缓冲（VBO）
```

**关键点**：`Destroy(gameObject)` 只销毁 C# 侧的对象，不会立即释放 Native 内存。Native 内存在**没有任何引用指向 C# 包装**，且 GC 运行过后才会释放。

### 在 Memory Profiler 里查看 Native 内存

切换到 **All of Memory** 视图，按 **Native Size** 降序排列：

```
通常占用最大的：
  Texture2D：纹理像素数据
  RenderTexture：后处理 RT，尤其是大分辨率的
  AudioClip：未压缩的 PCM（开启了 DecompressOnLoad 的音频）
  Mesh：复杂场景的网格数据
```

点击某个 Native 大对象，切换到 References 标签：

```
查看 "Referenced By" 链：
  如果根持有者是某个 Scene（已经卸载的）→ 资产未跟随场景释放
  如果根持有者是 Addressables AssetReference → 检查是否调用了 Release
  如果根持有者是 Resources.Load 加载的 → 检查是否调用了 Resources.UnloadAsset
```

### 追踪 Native 内存占用

```csharp
// 用代码统计运行时纹理内存
void PrintTextureMemoryUsage()
{
    var textures = Resources.FindObjectsOfTypeAll<Texture>();
    long totalBytes = 0;
    var report = new System.Text.StringBuilder();

    foreach (var t in textures)
    {
        long size = Profiler.GetRuntimeMemorySizeLong(t);
        totalBytes += size;
        if (size > 1024 * 1024) // 只打印超过 1MB 的
        {
            report.AppendLine($"{t.name} ({t.GetType().Name}): {size / 1024 / 1024}MB");
        }
    }

    Debug.Log($"Total Texture Memory: {totalBytes / 1024 / 1024}MB\n{report}");
}
```

---

## 六、RenderTexture 专项排查

RenderTexture 是移动端内存泄漏最高频的来源之一，因为它是动态创建的，且容易被遗忘。

```csharp
// ❌ 常见问题：动态创建 RenderTexture 但未释放
RenderTexture rt = new RenderTexture(1920, 1080, 24);
camera.targetTexture = rt;
// ... 用完后没有调用 rt.Release()

// ✅ 正确做法：明确生命周期
RenderTexture rt = null;

void OnEnable() {
    rt = new RenderTexture(1920, 1080, 24);
    camera.targetTexture = rt;
}

void OnDisable() {
    camera.targetTexture = null;
    if (rt != null) {
        rt.Release();
        Destroy(rt);
        rt = null;
    }
}
```

**临时 RenderTexture 的最佳实践**：

```csharp
// 使用 RenderTexture.GetTemporary / ReleaseTemporary
// Unity 内部有池化机制，比 new RenderTexture 更高效
RenderTexture temp = RenderTexture.GetTemporary(
    Screen.width / 2,
    Screen.height / 2,
    0,
    RenderTextureFormat.ARGB32
);

try {
    // 使用 temp 做后处理...
    Graphics.Blit(source, temp, blurMaterial);
    Graphics.Blit(temp, destination);
}
finally {
    // 无论是否出错，都要释放
    RenderTexture.ReleaseTemporary(temp);
}
```

**在 Memory Profiler 里检测 RT 泄漏**：

拍两张快照（进入后处理密集场景前后），在 Diff 视图里过滤 `RenderTexture`，如果 New 数量持续增长 → 存在 RT 泄漏。

---

## 七、典型排查案例

### 案例一：场景切换后内存持续增长

**现象**：玩家在大厅场景和战斗场景之间切换 5 次，内存从 400MB 增长到 650MB，且切回大厅后不下降。

**排查步骤**：

```
1. 在大厅初始状态拍快照 A
2. 切换到战斗场景 → 战斗一局 → 切回大厅
3. 拍快照 B
4. Diff A→B，按 Size 降序排列 "New" 对象

发现：
  - 战斗场景中的 Texture2D（技能特效纹理）仍然存在于快照 B
  - 这些纹理通过 Addressables 加载

根因：
  战斗场景的 AddressableAssetReference 在场景卸载时没有调用 Release()
  → Addressables 系统认为资产仍在使用
  → Native 纹理数据不会被释放
```

```csharp
// 修复：在场景卸载前释放 Addressable 资产
void OnDestroy() {
    foreach (var assetRef in loadedAssetRefs) {
        assetRef.ReleaseAsset();
    }
    loadedAssetRefs.Clear();
}
```

### 案例二：背包界面打开多次后内存线性增长

**现象**：每次打开背包 UI，内存增加约 2MB，关闭后不回落。

**排查步骤**：

```
Diff 分析发现：
  每次打开背包，新增约 200 个 Sprite 对象
  追踪 References：这些 Sprite 被 ItemIconPool（静态）持有

根因：
  背包的图标池没有上限，且不区分"已缓存"和"新创建"
  每次打开背包都创建新 Sprite 并加入池，但池从不清理
```

```csharp
// ❌ 有问题的图标池
static Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();

Sprite GetIcon(int itemId) {
    if (!iconCache.ContainsKey(itemId)) {
        // 每次都创建新 Sprite，没有大小限制
        iconCache[itemId] = CreateSpriteFromTexture(itemId);
    }
    return iconCache[itemId];
}

// ✅ 加入 LRU 上限或场景卸载时清理
void OnSceneUnloaded(Scene scene) {
    foreach (var sprite in iconCache.Values) {
        if (sprite != null) Destroy(sprite);
    }
    iconCache.Clear();
}
```

---

## 八、移动端内存预算参考

| 设备档次 | 系统 RAM | 可用游戏内存上限 | GC 托管堆建议上限 | 危险线（LMK 风险） |
|---------|---------|---------------|-----------------|-----------------|
| 低端（2GB） | 2048MB | ~500MB | ≤200MB | >600MB |
| 中端（4-6GB） | 4096-6144MB | ~900MB | ≤400MB | >1.1GB |
| 高端（8GB+） | 8192MB+ | ~1.5GB | ≤600MB | >2GB |

这些是经验值，实际上限还受 ROM 版本、厂商定制 LMK 阈值影响。关键是**监控增长趋势**，而不是单纯看绝对值。

---

## 九、与其他工具的配合

```
adb shell dumpsys meminfo：
  优势：不需要 Development Build，真机直接运行
  用途：快速确认当前总内存占用（Native Heap + Java Heap + Graphics）
  局限：无法定位到具体对象

Unity Profiler → 内存页：
  优势：实时查看每帧的内存分配（GC Alloc 计数器）
  用途：找出高频分配帧，发现内存趋势
  局限：无法钻取到具体对象

Unity Memory Profiler（本篇）：
  优势：对象级别的分析，完整的引用链追踪
  用途：泄漏定位，快照对比分析
  局限：快照是静态的，只反映拍摄时刻状态

建议工作流：
  开发期 → Profiler 监控 GC Alloc
  发现异常 → Memory Profiler 拍快照对比
  上线前 → adb dumpsys meminfo 检查总量是否在预算内
```

---

## 十、Memory Profiler 的失效条件

Memory Profiler 的快照机制本身存在几个使用边界，在以下场景需要特别注意：

**低端设备上拍快照可能触发 OOM**

拍摄快照时，Unity 需要在内存中同时保留：原有游戏数据、快照副本（约等于当前内存占用的 1.2–1.5 倍）。在 2GB 设备上，如果游戏当前已占用 500MB+，拍快照的瞬间内存峰值可能超过 LMK 的危险线，导致系统主动杀进程。

对策：在低端设备上只在"安全点"（加载完成后、非战斗状态）拍快照，不要在高负载场景中触发。也可以用 adb dumpsys meminfo 代替 Memory Profiler 来确认总量，只在内存可控时再用 Memory Profiler 做精细分析。

**快照是静态时刻，无法捕捉"一闪而过"的分配**

Memory Profiler 反映拍摄时刻的内存状态，对"每帧产生然后立即释放"的高频短生命周期分配无效。这类分配在帧时间里来去，但不在快照里留痕，却可能导致 GC 频繁触发。

这种情况要用 Unity Profiler 的内存页（Memory → Allocated 趋势图 + GC.Alloc 样本）来捕捉，而不是 Memory Profiler 快照。

**快照对比的"差量"可能被 GC 行为干扰**

两次快照之间，如果发生了一次 GC.Collect，托管堆会回收大量对象，导致快照 B 比快照 A 的托管对象数量少很多——这不是泄漏消失了，而是 GC 清理了本来就是临时分配的对象。

真正的泄漏在两次快照都应该可见：同一个对象类型的 Persistent 实例持续增长，且引用链指向的根节点不是临时变量（GCRoot 不是 Stack，而是 Static Field 或 MonoBehaviour 字段）。
