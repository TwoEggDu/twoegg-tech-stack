---
title: "CPU 性能优化 01｜C# GC 压力：堆分配来源、零分配写法与对象池"
slug: "cpu-opt-01-gc-pressure"
date: "2026-03-28"
description: "Unity 使用 Boehm GC（非分代），每次 GC 会暂停所有托管线程。移动端的帧率尖峰很大一部分来自 GC.Collect。本篇系统梳理堆分配的来源、零分配写法，以及对象池的正确实现。"
tags: ["Unity", "C#", "GC", "性能优化", "CPU"]
series: "移动端硬件与优化"
weight: 2140
---

## Unity GC 的工作原理

Unity 使用 **Boehm-Demers-Weiser GC**，这是一个保守式、非分代的垃圾收集器。理解它的特性，是写出低 GC 压力代码的前提。

### Boehm GC 的核心特点

**非分代（Non-generational）**：主流 GC 实现（如 .NET CoreCLR 的 GC）会把堆分为年轻代（Gen 0）、中间代（Gen 1）和老年代（Gen 2），短命对象优先在 Gen 0 回收，代价极小。Boehm GC 没有这个分层——每次 GC 都是**全堆扫描**，堆越大，暂停越久。

**保守式（Conservative）**：GC 不完全知道哪些内存位置是指针，它会把所有看起来像指针的值都当成指针来处理，导致部分垃圾无法被立即回收（误判为存活对象）。

**Stop-The-World**：GC 标记和清除阶段，所有托管线程都会暂停。在 Unity 2019.1 引入增量 GC 之前，这个暂停是不可分割的。

### 触发时机

GC 在以下情况触发：

1. **堆不够用**：托管堆没有足够空间分配新对象时，Unity 首先尝试扩展堆，如果超过阈值（或系统内存不足），就触发 GC。
2. **显式调用**：`GC.Collect()` 或 Unity 内部某些操作（如 `Resources.UnloadUnusedAssets()`）会触发。
3. **时间触发（增量 GC 模式）**：启用增量 GC 后，GC 会在每帧末尾分配一小段时间做增量标记。

### 暂停时间的量化

暂停时间与**存活对象数量**和**堆大小**近似线性相关：

| 托管堆大小 | 典型暂停时间（Android 中端机） |
|-----------|-------------------------------|
| 10 MB     | 0.5 - 1 ms                    |
| 50 MB     | 3 - 8 ms                      |
| 100 MB    | 8 - 20 ms                     |
| 200 MB    | 20 - 50 ms                    |

移动端帧时间预算：60fps = 16.6ms/帧，30fps = 33ms/帧。堆 50MB 时一次 GC 暂停就能吃掉半帧。

> **关键认知**：降低 GC 压力的根本手段不是"让 GC 更快"，而是**减少堆分配**，从源头上减少垃圾产生。

---

## 堆分配的来源

以下每类分配来源都附有可直接测试的代码示例。

### 1. `new` 关键字（引用类型）

C# 中 `class` 是引用类型，每次 `new` 都在托管堆上分配。

```csharp
// 坏：每帧在堆上分配一个新数组
void Update()
{
    Vector3[] positions = new Vector3[100]; // 堆分配：100 * 12 bytes = 1200 bytes
    FillPositions(positions);
    ProcessPositions(positions);
    // 函数结束，positions 成为垃圾，等待 GC 回收
}

// 坏：每次调用创建新列表
List<Enemy> GetNearbyEnemies(Vector3 pos, float radius)
{
    List<Enemy> result = new List<Enemy>(); // 堆分配
    foreach (var enemy in allEnemies)
    {
        if (Vector3.Distance(pos, enemy.Position) < radius)
            result.Add(enemy);
    }
    return result; // 调用者持有引用，但用完后成为垃圾
}
```

**struct 是值类型，在栈上分配**（或内联在包含它的对象中），不产生 GC 压力：

```csharp
// 好：struct 不触发堆分配
struct HitInfo
{
    public Vector3 Point;
    public float Distance;
    public int ColliderID;
}

HitInfo info = new HitInfo(); // 栈分配，零 GC
```

### 2. 装箱（Boxing）

当值类型（int、float、struct）被隐式或显式转换为 `object` 或非泛型接口时，会在堆上创建一个"盒子"来包装它。

```csharp
// 坏：装箱
int score = 42;
object boxed = score;           // 装箱：堆分配 ~20 bytes
string s = score.ToString();    // 某些情况下装箱（取决于实现）

// 坏：非泛型集合强制装箱
ArrayList list = new ArrayList();
list.Add(42);       // 装箱
list.Add(3.14f);    // 装箱

// 坏：LINQ 的 IEnumerable 枚举器（某些情况）
int[] arr = {1, 2, 3};
var sum = arr.Sum(); // Sum() 内部使用 IEnumerable，可能装箱

// 好：泛型集合，无装箱
List<int> genericList = new List<int>();
genericList.Add(42); // 无装箱，直接存储 int

// 坏：非泛型接口调用
void Sort(IComparable a, IComparable b)  // IComparable 是非泛型接口
{
    a.CompareTo(b); // 如果 a 是 struct，这里装箱
}

// 好：泛型接口，无装箱
void Sort<T>(T a, T b) where T : IComparable<T>
{
    a.CompareTo(b); // 无装箱，泛型约束保证类型安全
}
```

**LINQ 的装箱陷阱**（热路径禁用 LINQ）：

```csharp
// 坏：LINQ 在热路径中的多重分配
// 每次调用都可能分配枚举器对象、闭包对象
void Update()
{
    var nearEnemies = allEnemies
        .Where(e => e.IsAlive)          // 分配 WhereIterator
        .OrderBy(e => e.Distance)       // 分配 OrderedIterator
        .Take(5)                        // 分配 TakeIterator
        .ToList();                      // 分配 List<T>
}
```

### 3. 字符串操作

字符串在 C# 中是**不可变的引用类型**。每次拼接都创建新的字符串对象。

```csharp
// 坏：字符串拼接（每个 + 创建一个新字符串）
void Update()
{
    string log = "Player: " + playerName + " HP: " + hp + " MP: " + mp;
    // 产生 4 个中间字符串对象
    Debug.Log(log);
}

// 坏：string.Format 在旧版 .NET 中也会装箱值类型参数
string s = string.Format("HP: {0}", hp); // hp 是 int，被装箱

// 好：StringBuilder 复用
private StringBuilder _sb = new StringBuilder(128); // 字段，避免重复分配

void Update()
{
    _sb.Clear();
    _sb.Append("Player: ").Append(playerName)
       .Append(" HP: ").Append(hp)
       .Append(" MP: ").Append(mp);
    Debug.Log(_sb.ToString()); // 只有最终的 ToString 产生一个字符串
}

// 好：C# 6+ 的插值字符串（$""）底层仍会分配，热路径不用
// 好：在 Unity 中，非热路径可以用 $"Player: {playerName} HP: {hp}"
```

**UI 文本的特殊优化**：UI 显示数字时，可以维护一个预先格式化好的字符串数组：

```csharp
// 预缓存 0-999 的字符串表示
private static readonly string[] IntStrings = new string[1000];
static void BuildIntStringCache()
{
    for (int i = 0; i < 1000; i++)
        IntStrings[i] = i.ToString();
}

// 无分配的整数转字符串（范围内）
string GetIntString(int value)
{
    if (value >= 0 && value < IntStrings.Length)
        return IntStrings[value]; // 返回缓存，无分配
    return value.ToString(); // 超范围才分配
}
```

### 4. Lambda 和闭包

Lambda 本身如果不捕获外部变量，可以被编译器优化为静态委托（不分配）。但一旦**捕获了外部变量**（闭包），编译器会生成一个匿名类来存储捕获的变量，每次创建该 Lambda 就是一次堆分配。

```csharp
// 好：不捕获外部变量，编译器优化为静态委托，无分配
button.onClick.AddListener(() => Debug.Log("Clicked")); // 只分配一次

// 坏：捕获外部变量，每次调用函数都分配闭包对象
void RegisterCallback(int enemyId)
{
    // 捕获了 enemyId，产生闭包分配
    button.onClick.AddListener(() => KillEnemy(enemyId));
}

// 更坏：在 Update 中频繁创建捕获 Lambda
void Update()
{
    float threshold = hp * 0.5f; // 捕获 threshold 和 this
    enemies.RemoveAll(e => e.Hp < threshold); // RemoveAll 每帧分配闭包
}

// 好：用方法组代替 Lambda（无分配，但需要注意委托实例的缓存）
private bool IsEnemyDead(Enemy e) => !e.IsAlive;

void CleanupEnemies()
{
    // 方法组委托在赋值时分配一次，但如果每帧都这样写仍会分配
    enemies.RemoveAll(IsEnemyDead);
}

// 最好：缓存委托实例
private Predicate<Enemy> _isEnemyDead;
void Awake() { _isEnemyDead = IsEnemyDead; } // 只分配一次
void CleanupEnemies() { enemies.RemoveAll(_isEnemyDead); } // 无分配
```

### 5. `foreach` 的隐藏分配

`foreach` 语法糖依赖 `GetEnumerator()` 方法。对于不同集合，行为不同：

```csharp
// List<T> 的 foreach：在 Unity 旧版（5.x 时代）有分配，现代版本已优化
// 但数组（T[]）的 foreach 始终无分配（编译器展开为 for 循环）
// Dictionary<K,V> 的 foreach：枚举器是 struct，无装箱，无堆分配

// 安全：数组 foreach，无分配
int[] arr = {1, 2, 3};
foreach (int x in arr) { } // 编译为 for 循环

// 安全：List<T> foreach（Unity 2017+ 无分配）
List<int> list = new List<int>();
foreach (int x in list) { } // 枚举器是 struct

// 有分配：非泛型 IEnumerable
IEnumerable collection = someObject;
foreach (object x in collection) { } // GetEnumerator() 返回堆上的枚举器

// 有分配：自定义类实现 IEnumerable 但 GetEnumerator 返回 class
class MyCollection : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => new MyEnumerator(); // 堆分配！
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**最佳实践**：热路径用 `for` 循环 + 索引器，明确避免枚举器分配。

### 6. Unity API 的返回数组

许多 Unity API 返回新分配的数组，每次调用都产生 GC 压力：

```csharp
// 坏：每次调用都分配新数组
void Update()
{
    Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
    // hits 是新分配的 Collider[]，用完就成为垃圾

    Renderer[] renderers = GetComponentsInChildren<Renderer>();
    // 每次调用都分配新数组

    GameObject[] enemies = GameObject.FindObjectsOfType<Enemy>();
    // 最糟糕：全场景遍历 + 数组分配
}

// 好：使用 NonAlloc 版本
private Collider[] _hitBuffer = new Collider[20]; // 预分配，复用
private List<Renderer> _rendererList = new List<Renderer>(); // 复用 List

void Update()
{
    int hitCount = Physics.OverlapSphereNonAlloc(
        transform.position, 5f, _hitBuffer);
    // hitCount 是实际碰撞数，结果写入 _hitBuffer，无堆分配

    _rendererList.Clear();
    GetComponentsInChildren<Renderer>(_rendererList);
    // 结果写入 _rendererList，无堆分配（List 容量够的情况下）
}
```

**Unity API 的 NonAlloc 版本清单**：

| 原版（有分配）                    | NonAlloc 版本（无分配）                          |
|----------------------------------|------------------------------------------------|
| `Physics.RaycastAll`             | `Physics.RaycastNonAlloc(ray, results[])`       |
| `Physics.OverlapSphere`          | `Physics.OverlapSphereNonAlloc(pos, r, results[])`|
| `Physics.OverlapBox`             | `Physics.OverlapBoxNonAlloc`                    |
| `Physics.SphereCastAll`          | `Physics.SphereCastNonAlloc`                    |
| `GetComponents<T>()`             | `GetComponents<T>(List<T> results)`             |
| `GetComponentsInChildren<T>()`   | `GetComponentsInChildren<T>(List<T> results)`   |

---

## 零分配写法

### struct 代替 class

当一个数据类型满足以下条件时，优先用 struct：
- 逻辑上是一个值（坐标、颜色、范围）
- 生命周期短（临时计算结果）
- 数据量小（建议 < 32 bytes，否则拷贝开销反而大）

```csharp
// 坏：用 class 表示临时数据
class RaycastResult
{
    public Vector3 Point;
    public float Distance;
    public GameObject HitObject;
}

RaycastResult DoRaycast() => new RaycastResult { ... }; // 堆分配

// 好：用 struct
readonly struct RaycastResult
{
    public readonly Vector3 Point;
    public readonly float Distance;
    public readonly GameObject HitObject; // GameObject 本身是引用，但 struct 包装不额外分配

    public RaycastResult(Vector3 point, float distance, GameObject obj)
    {
        Point = point; Distance = distance; HitObject = obj;
    }
}

RaycastResult DoRaycast() => new RaycastResult(...); // 栈分配，零 GC
```

### `in` 参数：传递大型 struct 不拷贝

```csharp
// 大型 struct 传值会产生拷贝开销
struct BigTransform
{
    public Matrix4x4 LocalToWorld;
    public Matrix4x4 WorldToLocal;
    public Vector3 Position;
    public Quaternion Rotation;
    // 共 ~144 bytes
}

// 坏：传值，每次调用拷贝 144 bytes 到栈
void Process(BigTransform transform) { ... }

// 好：in 参数，传只读引用，无拷贝，无装箱
void Process(in BigTransform transform) { ... }
```

### Span<T> 和 Memory<T>：栈上的切片

`Span<T>` 是 .NET 中表示"一段连续内存"的 struct，可以指向栈、堆或 native 内存，**完全不分配**：

```csharp
// 坏：从数组创建子数组，分配新数组
int[] source = new int[1000];
int[] sub = source[10..20]; // 分配新数组（10 个元素）

// 好：Span 切片，零分配
Span<int> span = source.AsSpan(10, 10); // 不分配，只是指针+长度

// 在栈上分配临时缓冲区（不超过 ~1KB）
Span<byte> stackBuffer = stackalloc byte[256]; // 完全栈分配
ParseData(stackBuffer);

// 处理字符串而不分配
ReadOnlySpan<char> str = "Hello, World!".AsSpan();
ReadOnlySpan<char> hello = str.Slice(0, 5); // 无分配
```

> Unity 2021+ 支持 Span<T>（需要 .NET Standard 2.1 / .NET 5+）。

---

## 对象池实现

对象池的核心思想：**把"创建/销毁"改为"借用/归还"**，用预先分配好的对象避免 GC 压力。

### 泛型对象池的完整实现

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly int _maxSize;
    private int _countAll; // 已创建的总数（包括在外面使用的）

    public int CountInactive => _pool.Count;
    public int CountAll => _countAll;
    public int CountActive => _countAll - _pool.Count;

    public ObjectPool(
        Func<T> factory,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        int defaultCapacity = 10,
        int maxSize = 100)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (maxSize <= 0) throw new ArgumentException("maxSize must be > 0");

        _factory = factory;
        _onGet = onGet;
        _onRelease = onRelease;
        _maxSize = maxSize;
        _pool = new Stack<T>(defaultCapacity);
    }

    public T Get()
    {
        T item;
        if (_pool.Count > 0)
        {
            item = _pool.Pop();
        }
        else
        {
            item = _factory();
            _countAll++;
        }
        _onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        if (_pool.Count >= _maxSize)
        {
            // 池已满，丢弃对象（让 GC 回收）
            // 注意：如果 T 实现 IDisposable，这里应该调用 Dispose
            _countAll--;
            return;
        }

        _onRelease?.Invoke(item);
        _pool.Push(item);
    }

    /// <summary>
    /// RAII 模式：用 using 语句自动归还
    /// </summary>
    public PooledObject GetPooled(out T value)
    {
        value = Get();
        return new PooledObject(this, value);
    }

    public void Clear()
    {
        _pool.Clear();
        _countAll = 0;
    }

    // RAII 包装器，实现 IDisposable 以支持 using 语句
    public readonly struct PooledObject : IDisposable
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _value;

        public PooledObject(ObjectPool<T> pool, T value)
        {
            _pool = pool;
            _value = value;
        }

        public void Dispose() => _pool.Release(_value);
    }
}
```

**使用示例**：

```csharp
public class BulletManager : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;

    private ObjectPool<Bullet> _bulletPool;

    void Awake()
    {
        _bulletPool = new ObjectPool<Bullet>(
            factory: CreateBullet,
            onGet: bullet => bullet.gameObject.SetActive(true),
            onRelease: bullet =>
            {
                bullet.gameObject.SetActive(false);
                bullet.Reset();
            },
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    private Bullet CreateBullet()
    {
        var go = Instantiate(_bulletPrefab);
        return go.GetComponent<Bullet>();
    }

    public void FireBullet(Vector3 position, Vector3 direction)
    {
        var bullet = _bulletPool.Get(); // 借用
        bullet.transform.position = position;
        bullet.Launch(direction, () => _bulletPool.Release(bullet)); // 归还回调
    }
}
```

### Unity 内置的 `UnityEngine.Pool.ObjectPool<T>`

Unity 2021+ 提供了内置对象池，URP 和 HDRP 内部大量使用：

```csharp
using UnityEngine.Pool;

public class ParticleManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particlePrefab;

    // Unity 内置池，API 与自定义实现类似
    private IObjectPool<ParticleSystem> _pool;

    void Awake()
    {
        // collectionCheck: true 时，Release 重复对象会抛异常（便于调试）
        _pool = new ObjectPool<ParticleSystem>(
            createFunc: () => Instantiate(_particlePrefab),
            actionOnGet: ps => ps.gameObject.SetActive(true),
            actionOnRelease: ps => ps.gameObject.SetActive(false),
            actionOnDestroy: ps => Destroy(ps.gameObject),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 100
        );
    }

    public void PlayEffect(Vector3 position)
    {
        var ps = _pool.Get();
        ps.transform.position = position;
        ps.Play();

        // 播放完成后归还（使用 PooledObject 的 RAII 风格）
        StartCoroutine(ReturnAfterPlay(ps));
    }

    private IEnumerator ReturnAfterPlay(ParticleSystem ps)
    {
        yield return new WaitUntil(() => !ps.isPlaying);
        _pool.Release(ps);
    }
}
```

**`PooledObject` 的 RAII 写法（Unity 内置）**：

```csharp
// 使用 using 语句自动归还
using (var pooled = _pool.Get(out ParticleSystem ps))
{
    ps.transform.position = hitPoint;
    ps.Play();
    yield return new WaitUntil(() => !ps.isPlaying);
} // 自动调用 Release
```

### 对象池的副作用：增加 GC 扫描时间

对象池减少了**分配频率**，但池子里的对象始终是"存活对象"，GC 的标记阶段需要扫描它们。

- 池子存储 200 个 Enemy 对象：每次 GC 都要扫描这 200 个对象及其所有引用字段
- 对象越复杂（字段越多、引用链越深），扫描代价越高

**权衡原则**：
- 对象分配频繁（每帧多次）→ 用对象池，收益明显
- 对象分配不频繁（每秒几次）→ 评估是否值得，避免池子过大
- 池子的 `maxSize` 应该根据实际业务量设置，不要无限扩大

---

## 用 Profiler 定位 GC 分配

### GC.Alloc 列的使用

打开 Unity Profiler（Window → Analysis → Profiler），选择 CPU Usage 模块：

1. 点击某一帧，展开 Hierarchy 视图
2. 点击 **GC Alloc** 列头排序（从大到小）
3. 最顶部的条目就是当帧分配最多的函数
4. 双击条目跳转到对应的 C# 脚本行（需要 Development Build 或编辑器模式）

**关键技巧**：展开到叶节点，GC.Alloc 会显示在实际分配发生的函数上，而不是调用链顶部。

### Memory Profiler 的 Allocation CallStack

Unity Memory Profiler 包（Package Manager 安装）提供更详细的分配调用栈：

1. 在 Memory Profiler 窗口中捕获快照
2. 切换到 **Allocations** 视图
3. 按分配大小排序，查看每个分配的调用栈
4. 这对找"偶发性大分配"（不是每帧都有的）特别有用

> 注意：开启 Allocation CallStack 记录会显著降低游戏性能（约 30-50% 开销），只在定位问题时开启。

### 自定义 ProfilerMarker

在自己的代码中插入 Profiler 标记，方便在 Profiler 中找到对应的代码段：

```csharp
using Unity.Profiling;

public class EnemyAI : MonoBehaviour
{
    // 正确：ProfilerMarker 是 struct，声明为 static readonly，避免每次使用都构造
    private static readonly ProfilerMarker _pathfindMarker =
        new ProfilerMarker("EnemyAI.Pathfind");

    private static readonly ProfilerMarker _attackDecisionMarker =
        new ProfilerMarker("EnemyAI.AttackDecision");

    void Update()
    {
        // 方式 1：手动 Begin/End（性能最佳）
        _pathfindMarker.Begin();
        UpdatePath();
        _pathfindMarker.End();

        // 方式 2：using RAII（更安全，防止忘记 End）
        using (_attackDecisionMarker.Auto())
        {
            MakeAttackDecision();
        }
    }
}
```

在 Profiler 的 Timeline 视图中，自定义 Marker 会以独立色块显示，精确到微秒级别。

### ProfilerRecorder：运行时收集性能数据

不依赖 Profiler 窗口，在游戏运行时实时读取性能数据：

```csharp
using Unity.Profiling;
using UnityEngine;
using TMPro;

public class PerformanceHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text _statsText;

    private ProfilerRecorder _gcAllocRecorder;
    private ProfilerRecorder _mainThreadTimeRecorder;

    void OnEnable()
    {
        // 开始记录 GC 分配（每帧）
        _gcAllocRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC.Alloc", 15); // 保留最近 15 帧

        _mainThreadTimeRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal, "Main Thread", 15);
    }

    void OnDisable()
    {
        _gcAllocRecorder.Dispose();
        _mainThreadTimeRecorder.Dispose();
    }

    void Update()
    {
        // 每秒更新一次 UI，避免 UI 更新本身成为瓶颈
        if (Time.frameCount % 60 == 0)
        {
            long gcAlloc = _gcAllocRecorder.LastValue; // bytes
            double frameMs = _mainThreadTimeRecorder.LastValue * 1e-6; // ns → ms

            _statsText.text = $"Frame: {frameMs:F2}ms | GC: {gcAlloc / 1024}KB";
        }
    }
}
```

---

## 增量 GC（Incremental GC）

### Unity 2019.1+ 的增量 GC

增量 GC 把原本一次性完成的 GC 工作**分散到多帧**，每帧只做一小部分，避免单帧大暂停。

**启用方法**：
- Project Settings → Player → Other Settings → **Use incremental GC** 勾选
- 或通过脚本：`GarbageCollector.GCMode = GarbageCollector.Mode.Incremental;`

**原理**：增量 GC 在每帧末尾分配一个时间片（默认 `maxTimeMsPerFrame = 2ms`）进行增量标记。如果堆压力不大，多帧完成一次完整 GC；如果堆增长过快，仍会退化为全量 GC。

**代价：写屏障（Write Barrier）**：

增量标记期间，应用程序代码仍在运行，可能修改对象引用（导致 GC 标记结果过时）。为此，CLR 插入**写屏障**：每次给引用类型字段赋值时，都要通知 GC "这个引用变了"。

```csharp
// 以下赋值在增量 GC 模式下会触发写屏障
someObject.referenceField = anotherObject; // 写屏障开销 ~1-2 ns

// 对值类型字段赋值不触发写屏障
someObject.intField = 42; // 无写屏障
```

写屏障的开销极小（1-2 ns/次），在引用赋值频繁的热路径（如每帧数万次赋值）上才会有感知。

**增量 GC 的限制**：
- 不能完全消除 GC 暂停，只是把暂停分散
- 堆分配量过大时，增量步骤跟不上分配速度，仍会触发全量 GC
- **根本解决方案仍然是减少堆分配**，增量 GC 是最后一道保险，不是借口

### 配置增量 GC 的时间片

```csharp
// 设置每帧最多花多少时间在 GC 上（毫秒）
// 默认 2ms，可根据帧率预算调整
// 注意：这是最大值，不是保证值
void AdjustGCBudget(bool is60fps)
{
    // 60fps 下帧时间 16.6ms，留 1ms 给 GC
    // 30fps 下帧时间 33ms，可以给 3ms
    GarbageCollector.incrementalTimeSliceNanoseconds =
        (ulong)(is60fps ? 1_000_000 : 3_000_000); // ns 为单位
}
```

---

## 总结：GC 优化的优先级

按性价比排序：

1. **消除热路径中的堆分配**（Update 函数里的 `new`）：收益最大，改动最直接
2. **使用 NonAlloc API**：替换 `GetComponents`、`Physics.Overlap` 等，改动量小
3. **消除字符串操作**：UI 更新、日志输出用 StringBuilder 或缓存
4. **对象池**：子弹、特效、AI 等频繁创建/销毁的 GameObject
5. **消除装箱**：换用泛型接口和泛型集合
6. **启用增量 GC**：作为兜底手段，减少单帧暂停尖峰

在移动端，**把堆分配控制在每帧 0 bytes（或 < 1KB）** 是高质量项目的基准线。开 Profiler 的 GC.Alloc 列，把每帧 GC 分配降到零，帧率的稳定性会有质的提升。
