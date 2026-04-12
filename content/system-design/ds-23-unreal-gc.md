---
date: "2026-03-26"
title: "数据结构与算法 23｜Unreal GC 深度：UObject 体系、智能指针与两阶段销毁"
description: "Unreal 在 C++ 之上为 UObject 实现了一套完整的 GC 体系，完全绕开语言运行时。这篇讲清楚 UObject 的内存模型、UPROPERTY 引用追踪、智能指针（TSharedPtr/TWeakObjectPtr）的设计意图，以及 BeginDestroy → FinishDestroy 的两阶段销毁流程。"
slug: "ds-23-unreal-gc"
weight: 785
tags:
  - 软件工程
  - 内存管理
  - GC
  - Unreal
  - 性能优化
series: "数据结构与算法"
---

> 在 C++ 项目里，通常用 RAII（unique_ptr、shared_ptr）管内存。但 Unreal 的 Actor、Component 不用这套——它们由 UObject GC 管理，有自己的生命周期规则。弄不清楚这两套体系的边界，就会写出悬空指针或内存泄漏。

---

## 为什么 Unreal 要自己实现 GC

**C++ 没有托管运行时**。`new` 出来的对象必须手动 `delete`，否则泄漏；`delete` 后还用就是 Use-After-Free。

对小型项目，RAII 够用。但 Unreal 面对的是：

```
为什么 RAII 管不住 UObject？

1. 编辑器序列化：蓝图和关卡编辑器需要在不同场景间序列化、反序列化整个对象图
   → 需要运行时知道所有 UObject 的存在和引用关系

2. 热重载（Hot Reload）：修改 C++ 代码后，编辑器需要替换类，迁移已有对象的数据
   → 需要精确知道"谁引用了这个对象"

3. 复杂的生命周期：Actor 被 World 管理，Component 被 Actor 管理，
   引用关系是多对多的图，不是简单的树
   → shared_ptr 的循环引用问题会大量出现

4. 蓝图支持：蓝图（动态脚本）需要创建和销毁对象，语言运行时感知不到

结论：Unreal 需要一个"引擎级别的内存管理器"，对所有 UObject 统一追踪
```

---

## UObject 的内存模型

所有继承自 `UObject` 的对象都由 Unreal 的 GC 系统管理：

```cpp
// 创建 UObject：必须用 NewObject<T>，不能 new
UMyComponent* comp = NewObject<UMyComponent>(owner, UMyComponent::StaticClass());

// 创建 Actor：必须用 SpawnActor，不能 new
AEnemy* enemy = GetWorld()->SpawnActor<AEnemy>(AEnemy::StaticClass(), location, rotation);

// 不要这样做（GC 不知道这个对象的存在，会崩溃或泄漏）
UMyComponent* comp = new UMyComponent();  // 错误！
```

**GObject 数组（GUObjectArray）**：Unreal 维护一个全局的 UObject 注册表，所有通过 `NewObject` 创建的对象都在这里登记。GC 从这里出发遍历所有对象。

---

## UPROPERTY：GC 引用追踪的关键

GC 需要知道哪些 UObject 引用了哪些其他 UObject，才能判断哪些对象是"可达的"。

**`UPROPERTY()` 宏是 GC 引用追踪的入口**：

```cpp
UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()

public:
    // 有 UPROPERTY → GC 知道这个引用，会追踪 TargetEnemy 的可达性
    UPROPERTY()
    AEnemy* TargetEnemy;

    // 没有 UPROPERTY → GC 不知道这个引用
    AEnemy* RawEnemy;   // 危险！GC 可能在这个指针还在用时回收 RawEnemy
};
```

**标记清除流程**：

```
1. GC 从根集合出发（所有被 Root 标记的 UObject、所有 UWorld）
2. 递归遍历所有 UPROPERTY 引用
3. 标记所有可达的 UObject
4. 未被标记的 UObject → 不可达 → 销毁

如果 RawEnemy 没有 UPROPERTY：
  GC 看不到这个引用
  → 即使 AMyActor 还活着，RawEnemy 也可能被判定不可达
  → RawEnemy 被销毁
  → AMyActor 继续访问 RawEnemy → 崩溃
```

**实际规则**：
- UObject* 指针成员：**必须加 `UPROPERTY()`**，否则 GC 忽视该引用
- 非 UObject 的普通 C++ 对象（struct、int、std::vector）：不需要 UPROPERTY（GC 不管）
- `TArray<UObject*>`：整个数组加一个 `UPROPERTY()` 即可，GC 会遍历数组内的所有元素

---

## AddToRoot：防止 GC 回收

有些全局单例 UObject（比如 GameInstance、SubSystem）需要在整个游戏生命周期存活，不能被 GC 回收：

```cpp
// 把对象加到 GC 根集合，GC 不会回收它
MySingleton->AddToRoot();

// 不再需要时，从根移除（允许 GC 回收）
MySingleton->RemoveFromRoot();

// 检查是否在根集合
if (MySingleton->IsRooted()) { ... }
```

`GameInstance`、`GameMode`、`GameState` 等由引擎管理的对象，Unreal 内部已经调用了 `AddToRoot` 或等价操作，不需要手动处理。

---

## 两阶段销毁：BeginDestroy → FinishDestroy

UObject 的销毁不是立即完成的，而是分两个阶段：

```cpp
class UMyObject : public UObject
{
public:
    // 阶段一：开始销毁
    // 在这里：释放外部资源（文件句柄、网络连接、GPU 资源）
    // 不能在这里访问其他 UObject（它们可能也在销毁中）
    virtual void BeginDestroy() override
    {
        // 释放非 UObject 资源
        if (FileHandle) { FileHandle->Close(); FileHandle = nullptr; }

        // 必须调用 Super！
        Super::BeginDestroy();
    }

    // 阶段二：真正销毁
    // 只有当 IsReadyForFinishDestroy() 返回 true 时才会调用
    // 在这里：执行最终清理，之后对象内存被释放
    virtual void FinishDestroy() override
    {
        Super::FinishDestroy();
    }

    // 可以在这里等待异步操作完成
    // 比如：等待 GPU 异步加载完成才允许销毁
    virtual bool IsReadyForFinishDestroy() override
    {
        return !AsyncLoadHandle.IsValid();  // 异步加载完成后才允许销毁
    }
};
```

**为什么需要两阶段？**

```
GPU 资源（RHI）通常在渲染线程上管理。
当游戏线程决定销毁一个 Mesh Component 时：
  BeginDestroy：通知渲染线程"准备释放这个 Mesh 的 GPU 资源"
  IsReadyForFinishDestroy：等渲染线程确认 GPU 资源已释放
  FinishDestroy：游戏线程完成对象内存释放

如果只有一阶段（立即销毁），渲染线程可能还在用 GPU 资源
而游戏线程已经释放了对应的 CPU 数据 → 渲染崩溃
```

---

## Unreal 的智能指针体系

Unreal 有两套独立的指针管理：**UObject 体系**（GC 管理）和 **C++ 对象体系**（智能指针管理）。

### UObject 体系的指针

```cpp
// 强引用（Strong Reference）：GC 追踪，防止回收
UPROPERTY()
AEnemy* StrongRef;

// 弱引用（Weak Reference）：GC 不追踪，对象被回收后自动变 null
UPROPERTY()
TWeakObjectPtr<AEnemy> WeakRef;

// 使用弱引用
if (WeakRef.IsValid())
{
    AEnemy* enemy = WeakRef.Get();
    enemy->TakeDamage(10.f);
}
// 如果 enemy 已被 GC 回收，WeakRef.IsValid() 返回 false，安全

// 软引用（Soft Reference）：资产引用，支持异步加载
UPROPERTY()
TSoftObjectPtr<UTexture2D> LazyTexture;  // 不立即加载，引用资产路径
```

### 纯 C++ 对象的智能指针

对于**不继承 UObject** 的纯 C++ 类，用 Unreal 的智能指针（类似 std::shared_ptr，但性能更好）：

```cpp
// TSharedPtr：共享所有权（引用计数）
TSharedPtr<FMyData> SharedData = MakeShared<FMyData>();
TSharedPtr<FMyData> Copy = SharedData;  // 引用计数 +1
// Copy 析构：引用计数 -1
// SharedData 析构：引用计数 0 → 销毁 FMyData

// TWeakPtr：弱引用，不增加引用计数
TWeakPtr<FMyData> Weak = SharedData;
if (TSharedPtr<FMyData> Pinned = Weak.Pin())  // 尝试获取强引用
{
    Pinned->DoSomething();
}
// SharedData 被销毁后，Weak.Pin() 返回 nullptr

// TUniquePtr：独占所有权（类似 std::unique_ptr）
TUniquePtr<FMyData> Unique = MakeUnique<FMyData>();
// Unique 析构时自动 delete FMyData
```

**关键区别**：

```
TSharedPtr<T>：用于纯 C++ 的 F-类（FMyData、FHitResult 等）
UPROPERTY() T*：用于 UObject 继承体系（AActor、UComponent 等）

不要对 UObject 用 TSharedPtr！
  UObject 由 GC 管理，生命周期不归引用计数控制
  TSharedPtr 析构时会 delete，但 GC 也会尝试销毁 → double free 崩溃
```

---

## GC 触发时机与性能

```cpp
// GC 自动触发（默认每 60 秒，或内存压力触发）
// 可在 DefaultEngine.ini 配置
[/Script/Engine.GarbageCollectionSettings]
gc.TimeBetweenPurgingPendingKillObjects=60  // 秒

// 手动触发
GEngine->ForceGarbageCollection(true);  // 立即强制 GC
// 或
CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS);

// 异步 GC（减少帧时间影响，Unreal 5 改进）
// GC 的标记阶段在后台线程进行，清除阶段仍需主线程短暂介入
```

**Unreal GC 的性能特点**：

- GC 间隔较长（60 秒），每次运行代价相对较高，但不频繁
- 大型开放世界场景下，UObject 数量可达数万，GC 可能花费几毫秒到十几毫秒
- `PendingKill`（已标记待销毁但未完成清理的对象）数量影响 GC 时间

---

## 常见陷阱

### 陷阱一：裸指针悬空

```cpp
// 没有 UPROPERTY，GC 不追踪，指针随时可能悬空
class AMyActor : public AActor
{
    AEnemy* RawEnemyPtr;  // 危险！

    void Tick(float DeltaTime) override
    {
        RawEnemyPtr->Update();  // 如果 enemy 被 GC 回收，崩溃
    }
};

// 正确：加 UPROPERTY 或用 TWeakObjectPtr
UPROPERTY()
AEnemy* SafeEnemyPtr;  // GC 追踪，enemy 被销毁时引擎可以发出警告

TWeakObjectPtr<AEnemy> SafeWeakPtr;  // 安全弱引用，自动检测失效
```

### 陷阱二：在 BeginDestroy 里访问其他 UObject

```cpp
void UMyObject::BeginDestroy()
{
    // 危险：OtherObject 可能也在 BeginDestroy 中，或已经无效
    if (OtherObject)
        OtherObject->DoSomething();  // 可能崩溃

    // 正确：在 BeginPlay/EndPlay 中处理跨对象交互
    // BeginDestroy 只做自己的资源清理

    Super::BeginDestroy();
}
```

### 陷阱三：大量短命 UObject

```cpp
// 每帧 NewObject 创建临时 UObject，GC 积压大量待清理对象
void Update()
{
    UDamageData* data = NewObject<UDamageData>();  // 每帧分配
    data->amount = 100;
    ApplyDamage(data);
    // data 没有强引用持有 → 等待 GC 回收（不是立即）
}

// 更好：对频繁创建销毁的数据，用普通 C++ struct（不继承 UObject）
struct FDamageData { float amount; };
void Update()
{
    FDamageData data { .amount = 100 };  // 栈上，零开销
    ApplyDamage(data);
}
```

---

## 小结

- **UObject GC**：标记清除，基于 `UPROPERTY()` 追踪引用图，定期（60s）触发
- **UPROPERTY 是必须的**：UObject* 成员没有 UPROPERTY，GC 忽视该引用，导致悬空指针
- **AddToRoot**：防止 GC 回收全局对象；游戏结束时 RemoveFromRoot
- **两阶段销毁**：BeginDestroy（释放外部资源）→ IsReadyForFinishDestroy（等待异步完成）→ FinishDestroy（最终清理）
- **智能指针**：UObject 用 UPROPERTY + TWeakObjectPtr；纯 C++ 对象用 TSharedPtr / TUniquePtr；不要混用
- **性能**：GC 间隔长但单次代价高；避免大量短命 UObject，频繁创建的数据用普通 C++ struct
