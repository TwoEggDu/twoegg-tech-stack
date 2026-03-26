---
date: "2026-03-26"
title: "数据结构与算法 22｜Unity GC 深度：Boehm → 增量 GC，Alloc 热点与零 GC 实践"
description: "Unity 的 GC 卡顿是最常见的性能问题之一。这篇讲清楚 Unity GC 的历史演进（Boehm → 增量 GC）、常见 GC Alloc 热点的根因，以及零 GC 编程的系统性方法：NativeArray、ArrayPool、对象池、值类型技巧。"
slug: "ds-22-unity-gc"
weight: 783
tags:
  - 软件工程
  - 内存管理
  - GC
  - Unity
  - 性能优化
series: "数据结构与算法"
---

> Unity Profiler 里那条周期性的 GC.Collect 尖刺，是很多游戏帧时间不稳定的元凶。理解 Unity GC 的工作方式，才能系统性地消灭它——而不是靠猜测打补丁。

---

## Unity GC 的历史演进

### 阶段一：Boehm GC（Unity 5 之前，一直沿用到 Unity 2021）

Unity 早期使用 **Boehm-Demers-Weiser GC**，一个为 C/C++ 设计的保守式 GC。

**保守式（Conservative）** 意味着：GC 不知道内存里哪些值是指针、哪些是整数，它把所有"看起来像堆地址的值"都当作潜在指针对待。

```
内存中某处有值 0x00A4B2C8
这是一个 int？还是一个指向 Enemy 对象的指针？
Boehm GC：不确定，保守地认为它是指针，Enemy 对象不回收

后果：
  偶尔会"误留"本该回收的对象（false positive）
  导致少量内存无法被回收，但不会误回收（不崩溃）
```

**Boehm GC 的关键特性**：

- **Stop-the-World**：GC 运行时，所有托管线程暂停
- **非分代**：每次 GC 扫描整个托管堆（堆越大，越慢）
- **非压缩**：回收内存后不移动对象，堆会碎片化
- **触发时机**：托管堆扩容时（分配内存超过当前堆容量）

```
典型 Boehm GC 卡顿时序：
帧 1~60：正常运行，每帧 new 大量临时对象
帧 61：托管堆满 → GC 运行 → STW 暂停 10~50ms → 帧时间尖刺
帧 62~120：又慢慢积累垃圾...
```

### 阶段二：增量 GC（Unity 2019+，基于三色标记）

Unity 2019.1 引入增量 GC，Unity 2021 起默认启用。

**核心改变**：把 GC 的标记工作拆分到多帧，每帧只做一小部分，避免单帧长时间暂停：

```
增量 GC 模式：
帧 N：做 0.5ms 的标记工作
帧 N+1：再做 0.5ms
帧 N+2：再做 0.5ms
...
帧 N+K：标记完成，执行清除（仍需短暂 STW，但时间极短）

vs Boehm GC：
帧 N：一次做完所有标记 + 清除 → 暂停 10~50ms
```

**启用方式**：

```csharp
// Project Settings → Player → Other Settings → Use incremental GC
// 或代码控制
GarbageCollector.GCMode = GarbageCollector.Mode.Incremental;

// 指定每帧最多用于 GC 的时间（毫秒）
GarbageCollector.incrementalTimeSliceNanoseconds = 1_000_000;  // 1ms
```

**增量 GC 的代价**：写屏障——每次写引用都有额外的几条指令检查三色不变式。在引用写入极密集的代码路径上，有可感知的 CPU 开销（通常 < 5%）。

---

## Unity 托管堆的结构

```
Unity 托管堆（Managed Heap）：
  ┌────────────────────────────────┐
  │  已分配对象区域                 │
  │  (class 实例、数组、闭包...)    │
  ├────────────────────────────────┤
  │  空闲内存（碎片）               │
  └────────────────────────────────┘
  堆大小：只增不减（Boehm），增量 GC 可归还部分

堆扩容触发 GC：
  当前堆 = 100MB，分配了 99MB
  再分配 5MB → 堆不够 → GC 尝试回收
  回收后仍不够 → 堆扩容到 200MB（向 OS 申请）
```

**关键点**：Unity 托管堆**只增不减**（默认）。一次大量分配会让堆永久保持在高水位，即使之后对象都被回收，内存占用显示仍然很高。

---

## 常见 GC Alloc 热点

### 热点一：装箱（Boxing）

值类型（struct、int、float、bool）赋值给 `object` 或接口时，会在堆上分配一个包装对象：

```csharp
// 装箱：int → object，堆分配
int hp = 100;
object boxed = hp;  // 装箱，分配堆内存

// 常见触发场景
string.Format("HP: {0}", hp);      // {0} 接受 object → 装箱
Debug.Log("HP: " + hp);            // 字符串拼接 + 整数 → 装箱
Dictionary<string, object> dict;
dict["hp"] = hp;                   // value 是 object → 装箱

// enum 作为 Dictionary 键（旧版 .NET）
Dictionary<MyEnum, int> dict;
dict[MyEnum.Fire] = 1;             // enum.GetHashCode() 可能装箱

// 避免
$"HP: {hp}"                        // 字符串插值，.NET 5+ 无装箱（ValueStringBuilder）
Debug.Log($"HP: {hp}");            // 同上
```

### 热点二：闭包（Closure）与 Lambda

Lambda 捕获外部变量时，会在堆上分配一个闭包对象：

```csharp
// 每次调用 GetEnemiesInRange 都会 new 一个闭包
void Update()
{
    float range = 10f;
    // ↓ 捕获了 range，编译器生成一个包含 range 字段的 class → 堆分配
    var nearby = enemies.Where(e => Vector3.Distance(e.pos, pos) < range);
}

// 避免：把 lambda 提取为静态方法（无捕获 = 无闭包）
static bool IsInRange(Enemy e) => Vector3.Distance(e.pos, playerPos) < 10f;
void Update()
{
    var nearby = enemies.Where(IsInRange);  // 无堆分配（静态委托缓存）
}

// 或者：缓存委托（只分配一次）
private Predicate<Enemy> rangeFilter;
void Awake() { rangeFilter = e => ...; }  // 只 new 一次
void Update() { enemies.Where(rangeFilter); }  // 复用
```

### 热点三：LINQ

LINQ 几乎所有操作都会分配：迭代器对象、中间集合等：

```csharp
// 每次都分配迭代器 + 可能的中间集合
void Update()
{
    var alive = enemies.Where(e => e.hp > 0).ToList();  // 2 次分配
    var nearest = enemies.OrderBy(e => e.dist).First(); // 排序 + 迭代器
}

// 避免：手写循环
void Update()
{
    aliveCache.Clear();  // 复用 List
    foreach (var e in enemies)
        if (e.hp > 0) aliveCache.Add(e);
}
```

### 热点四：字符串操作

```csharp
// 字符串拼接每次产生新的 string 对象
void Update()
{
    string log = "Frame: " + Time.frameCount + " FPS: " + fps;  // 多次分配
    Debug.Log(log);
}

// 避免：StringBuilder 复用（适合高频拼接）
private StringBuilder sb = new();
void Update()
{
    sb.Clear();
    sb.Append("Frame: ").Append(Time.frameCount)
      .Append(" FPS: ").Append(fps);
    Debug.Log(sb);
}

// 或者：字符串插值（低频）
Debug.Log($"Frame: {Time.frameCount} FPS: {fps}");
// .NET 6+ 插值用 DefaultInterpolatedStringHandler，减少分配
```

### 热点五：协程的 yield return new WaitForSeconds

```csharp
// 错误：每次都 new WaitForSeconds
IEnumerator Shoot()
{
    while (true)
    {
        Fire();
        yield return new WaitForSeconds(0.5f);  // 每次循环都分配！
    }
}

// 正确：缓存
private WaitForSeconds waitHalfSec = new WaitForSeconds(0.5f);
IEnumerator Shoot()
{
    while (true)
    {
        Fire();
        yield return waitHalfSec;  // 复用，零分配
    }
}
```

### 热点六：GetComponent 和 Find 的间接分配

```csharp
// GetComponent<T>() 本身不分配，但某些 Unity API 会
void Update()
{
    // gameObject.GetComponents<T>() 返回新数组 → 分配
    var colliders = GetComponents<Collider>();

    // 用非分配版本
    GetComponents<Collider>(cachedColliderList);  // 填充到已有 List
}
```

---

## 用 Profiler 定位 GC Alloc

```
Unity Profiler → CPU Usage → GC Alloc 列

筛选方法：
1. 打开 Deep Profile（性能开销大，只用于定位问题）
2. 查看 GC Alloc 列不为 0 的帧
3. 展开调用栈，找到最深的分配点
4. 按 GC Alloc 从大到小排序，优先处理大户
```

常用快捷排查：

```csharp
// 标记某段代码，方便在 Profiler 里识别
using (new ProfilerMarker("BulletUpdate").Auto())
{
    UpdateBullets();
}
```

---

## 零 GC 编程工具箱

### 工具一：NativeArray（完全绕开 GC）

```csharp
// NativeArray 存储在非托管内存，GC 完全看不到它
var positions = new NativeArray<float3>(1000, Allocator.Persistent);
// 使用...
positions.Dispose();  // 手动释放（必须！）

// 在 Job System 中使用（多线程安全）
var job = new MoveJob { positions = positions, deltaTime = Time.deltaTime };
JobHandle handle = job.Schedule(positions.Length, 64);
handle.Complete();
```

### 工具二：ArrayPool（托管数组复用）

```csharp
// 借出数组（从池里取，不 new）
int[] buffer = ArrayPool<int>.Shared.Rent(256);
try
{
    // 使用 buffer...
    ProcessData(buffer);
}
finally
{
    ArrayPool<int>.Shared.Return(buffer);  // 归还，不触发 GC
}
```

### 工具三：对象池（DS 设计模式篇已详述）

```csharp
// Unity 内置对象池（Unity 2021+）
var pool = new ObjectPool<Bullet>(
    createFunc:    () => Instantiate(bulletPrefab).GetComponent<Bullet>(),
    actionOnGet:   b => b.gameObject.SetActive(true),
    actionOnRelease: b => b.gameObject.SetActive(false),
    actionOnDestroy: b => Destroy(b.gameObject),
    maxSize: 200
);

Bullet b = pool.Get();
// 使用...
pool.Release(b);
```

### 工具四：Span\<T\> 和 stackalloc（栈上临时数组）

```csharp
// 小数组直接在栈上分配，完全绕开 GC
// 注意：大数组不要 stackalloc（栈空间有限，约 1MB）
Span<int> temp = stackalloc int[16];
for (int i = 0; i < 16; i++) temp[i] = i * 2;
// temp 在方法结束时自动释放（栈弹出），无 GC
```

### 工具五：结构体（值类型）代替类

```csharp
// class：堆分配，GC 追踪
class BulletData { public Vector3 pos; public float speed; }

// struct：栈分配或内嵌在其他对象，无堆分配
struct BulletData { public Vector3 pos; public float speed; }

// 在 NativeArray 里存 struct：完全无 GC
NativeArray<BulletData> bullets = new NativeArray<BulletData>(1000, Allocator.Persistent);
```

---

## 主动 GC 管理

```csharp
// 在合适的时机主动触发 GC（比如场景加载完成后）
// 避免在游戏进行中被动触发
IEnumerator LoadScene()
{
    yield return SceneManager.LoadSceneAsync("GameScene");
    // 场景加载完，清理加载过程的临时对象
    System.GC.Collect();
    System.GC.WaitForPendingFinalizers();
    System.GC.Collect();  // 两次确保 Finalizer 对象也被清理
    Resources.UnloadUnusedAssets();
}

// 禁止 GC 在关键时段自动运行（Unity 2019+）
GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
// ... 关键战斗逻辑 ...
GarbageCollector.GCMode = GarbageCollector.Mode.Incremental;
```

---

## 小结

- **Boehm GC**：保守式，Stop-the-World，整堆扫描，帧时间尖刺明显——Unity 的历史包袱
- **增量 GC**：三色标记，分帧执行，STW 极短——Unity 2019+ 的推荐选项，写屏障有小额开销
- **六大 Alloc 热点**：装箱、闭包、LINQ、字符串拼接、yield new、GetComponents 新数组
- **零 GC 工具箱**：NativeArray（完全绕开）、ArrayPool（托管复用）、对象池、stackalloc、struct
- **主动 GC**：在场景加载等"无感知"时机手动 `GC.Collect`，而不是让它在游戏进行中意外触发
- **下一篇（DS-23）**：Unreal UObject GC、智能指针体系、两阶段销毁
