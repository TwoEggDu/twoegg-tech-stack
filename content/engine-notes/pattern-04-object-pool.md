---
title: "游戏编程设计模式 04｜Object Pool：对象池化原理与实践"
description: "频繁的对象创建和销毁是游戏 GC 卡顿的主要来源。这篇讲清楚对象池的原理、Unity 内置 ObjectPool 的使用、通用泛型池的实现，以及何时用何时不用。"
slug: "pattern-04-object-pool"
weight: 733
tags:
  - 软件工程
  - 设计模式
  - Object Pool
  - 性能优化
  - 游戏架构
series: "游戏编程设计模式"
---

> **Object Pool 的核心思想**：不要频繁创建和销毁对象，而是预先创建一批对象放进"池子"里，需要时从池子里取出，用完后放回池子，而不是销毁。
>
> 这个模式解决的是一个非常具体的性能问题：**GC 卡顿**。

---

## 为什么游戏里的 GC 是问题

.NET 的 GC（垃圾回收器）是自动内存管理的机制：当没有任何引用指向一个对象时，GC 会在某个时机回收它占用的内存。

对于 Web 服务器，GC 触发时花费几十毫秒不是大问题——用户顶多感觉这个请求慢了一点。

对于游戏，每帧只有 16ms（60fps）。一次 GC 触发可以轻易占用 1~5ms，严重时达到 10ms 以上——直接导致一帧被跳过，表现为明显的画面卡顿（Stutter）。

**GC 被触发的原因**：堆内存（Heap）上的垃圾积累到一定量。每一次 `new SomeClass()` 都在堆上分配内存，用完后这块内存就是垃圾，等待 GC 回收。

游戏里最典型的高频分配场景：

```csharp
// 子弹系统：每秒射出 20 发子弹，每发子弹 Instantiate 一个 GameObject
// 子弹击中后 Destroy
// 每秒 20 次创建 + 20 次销毁，GC 压力极大
void Update()
{
    if (Input.GetMouseButton(0))
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        // 子弹飞一会儿后
        Destroy(bullet, 3f); // 每次 Destroy 都产生 GC 垃圾
    }
}
```

这段代码在低射速下没问题，但如果射速提高到每秒 100 发（比如机枪、散弹枪），每帧都有大量 Instantiate/Destroy，GC 压力就会显现。

---

## Object Pool 的原理

把"创建+销毁"换成"借出+归还"：

```
传统方式：
需要子弹 → new Bullet() → 使用 → Destroy → GC 回收

对象池方式：
需要子弹 → 从池中取出（SetActive(true)） → 使用 → 放回池（SetActive(false)）
（对象本身一直存在，只是激活/停用）
```

---

## 手写一个简单的 GameObject 池

```csharp
public class GameObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 20;

    private readonly Queue<GameObject> available = new();
    private readonly List<GameObject> all = new();

    void Awake()
    {
        // 预热：提前创建好对象
        for (int i = 0; i < initialSize; i++)
            CreateNew();
    }

    private GameObject CreateNew()
    {
        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        available.Enqueue(obj);
        all.Add(obj);
        return obj;
    }

    // 借出对象
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        if (available.Count > 0)
            obj = available.Dequeue();
        else
            obj = CreateNew(); // 池子空了，动态扩容

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    // 归还对象
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        available.Enqueue(obj);
    }

    // 归还所有对象（比如关卡重置时）
    public void ReturnAll()
    {
        foreach (var obj in all)
        {
            obj.SetActive(false);
            if (!available.Contains(obj))
                available.Enqueue(obj);
        }
    }
}
```

子弹使用对象池：

```csharp
public class BulletShooter : MonoBehaviour
{
    [SerializeField] private GameObjectPool bulletPool;

    public void Shoot()
    {
        var bullet = bulletPool.Get(firePoint.position, firePoint.rotation);
        // 子弹需要知道自己该归还到哪个池
        bullet.GetComponent<Bullet>().Initialize(bulletPool);
    }
}

public class Bullet : MonoBehaviour
{
    private GameObjectPool pool;
    private float speed = 20f;

    public void Initialize(GameObjectPool pool)
    {
        this.pool = pool;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 击中后归还到池，而不是 Destroy
        pool.Return(gameObject);
    }

    void OnEnable()
    {
        // 每次从池里取出时重置状态
        Invoke(nameof(ReturnToPool), 5f); // 5秒后自动归还（飞出屏幕）
    }

    void OnDisable()
    {
        CancelInvoke(); // 归还时取消自动回收
    }

    void ReturnToPool() => pool.Return(gameObject);
}
```

---

## Unity 内置 ObjectPool（Unity 2021+）

Unity 从 2021 版本开始提供了内置的泛型对象池 `UnityEngine.Pool.ObjectPool<T>`：

```csharp
using UnityEngine.Pool;

public class BulletShooter : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;

    private IObjectPool<Bullet> bulletPool;

    void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            createFunc: () =>
            {
                // 创建新对象（池子空了时调用）
                var bullet = Instantiate(bulletPrefab);
                bullet.SetPool(bulletPool); // 让子弹知道自己的池
                return bullet;
            },
            actionOnGet: bullet =>
            {
                // 从池里取出时调用（激活、重置状态）
                bullet.gameObject.SetActive(true);
            },
            actionOnRelease: bullet =>
            {
                // 归还到池时调用（停用）
                bullet.gameObject.SetActive(false);
            },
            actionOnDestroy: bullet =>
            {
                // 池子满了，多余的对象被销毁时调用
                Destroy(bullet.gameObject);
            },
            collectionCheck: true,  // 开发时检测重复归还
            defaultCapacity: 20,    // 初始容量
            maxSize: 100            // 最大容量（超出时销毁多余对象）
        );
    }

    public void Shoot()
    {
        Bullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
    }
}

public class Bullet : MonoBehaviour
{
    private IObjectPool<Bullet> pool;

    public void SetPool(IObjectPool<Bullet> pool) => this.pool = pool;

    public void ReturnToPool() => pool.Release(this);

    void OnTriggerEnter(Collider other)
    {
        pool.Release(this); // 通过 Release 归还，而不是 Return
    }
}
```

内置 `ObjectPool` 的 `collectionCheck: true` 会在开发模式下检测你是否把同一个对象归还了两次（双重归还），这是对象池最常见的 Bug 之一。

---

## 通用非 MonoBehaviour 对象池

不只是 GameObject，纯 C# 对象（粒子数据、伤害数字、路径节点）也可以池化：

```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> pool = new();
    private readonly Action<T> resetAction; // 归还时重置状态的函数

    public ObjectPool(int initialSize, Action<T> resetAction = null)
    {
        this.resetAction = resetAction;
        for (int i = 0; i < initialSize; i++)
            pool.Push(new T());
    }

    public T Get()
    {
        return pool.Count > 0 ? pool.Pop() : new T();
    }

    public void Return(T obj)
    {
        resetAction?.Invoke(obj); // 归还前重置状态
        pool.Push(obj);
    }
}

// 使用：伤害数字的数据对象
public class DamageNumberData
{
    public int amount;
    public Vector3 position;
    public Color color;
    public float lifetime;
}

// 池化伤害数字数据，避免频繁 new
private ObjectPool<DamageNumberData> damageDataPool = new ObjectPool<DamageNumberData>(
    initialSize: 50,
    resetAction: data =>
    {
        data.amount = 0;
        data.position = Vector3.zero;
        data.color = Color.white;
        data.lifetime = 0;
    }
);
```

---

## 对象池的注意事项

### 注意一：归还前必须重置状态

对象从池里取出时，它保留着上次使用的状态。如果不重置，会出现奇怪的 Bug：

```csharp
// 错误：没有重置速度，子弹取出时还保留着上次的速度方向
void actionOnGet = bullet => bullet.gameObject.SetActive(true);

// 正确：取出时完整重置
void actionOnGet = bullet =>
{
    bullet.gameObject.SetActive(true);
    bullet.velocity = Vector3.zero;
    bullet.hasHit = false;
    bullet.damage = baseDamage;
};
```

### 注意二：避免双重归还

```csharp
// Bug：子弹同时触发了"5秒自动归还"和"碰撞归还"
// 导致同一个对象被归还两次，可能在对象还在使用中时被别人取走
void OnTriggerEnter(Collider other)
{
    pool.Release(this);
    // 如果 Invoke 的计时还没取消，会再归还一次
}

void OnEnable()
{
    Invoke(nameof(ReturnToPool), 5f);
}

void OnDisable()
{
    CancelInvoke(); // 必须在 OnDisable 里取消，而不是仅在 OnTriggerEnter 里取消
}
```

### 注意三：不是所有对象都值得池化

对象池的开销：
- 内存常驻（即使对象不在使用中，也占用内存）
- 代码复杂度增加（归还逻辑、状态重置）

**值得池化的对象**：高频创建（每秒超过 10 次）、短生命周期（秒级别）、内存分配量较大的对象。

**不需要池化的对象**：低频创建（每局游戏一次，或分钟级别）、生命周期很长的对象、轻量级值类型（struct，分配在栈上）。

---

## 小结

```
不用对象池：Instantiate → 使用 → Destroy → GC 积累 → GC 触发 → 卡顿
用对象池：预分配 → Get → 使用 → Release → 重用 → 零 GC 压力
```

| 场景 | 是否推荐池化 |
|---|---|
| 子弹、箭矢、粒子（高频、短命） | 必须 |
| 伤害数字、飘字（高频、短命） | 推荐 |
| 敌人（中频，有复杂初始化） | 推荐（但归还时要完整重置） |
| 关卡 Boss（低频，每局一个） | 不需要 |
| UI 列表项（动态数量但相对稳定） | 推荐用 UI 虚拟化代替 |

Unity 内置 `ObjectPool<T>` 是项目首选，比手写实现更稳定，且有双重归还检测。
