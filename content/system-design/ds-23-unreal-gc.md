---
date: "2026-03-26"
title: "数据结构与算法 23｜Unreal GC 深度：UObject 体系、智能指针与两阶段销毁"
description: "Unreal 在 C++ 之上为 UObject 实现了一套完整的 GC 体系，完全绕开语言运行时。这篇讲清楚 UObject 的内存模型、UPROPERTY 引用追踪、智能指针（TSharedPtr/TWeakObjectPtr）的设计意图，以及 BeginDestroy → FinishDestroy 的两阶段销毁流程。"
slug: "ds-23-unreal-gc"
lastmod: "2026-10-25"
weight: 785
tags:
  - 软件工程
  - 内存管理
  - GC
  - Unreal
  - 性能优化
series: "数据结构与算法"
---

> **对象与持有前置**：先读 [UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})，分开创建入口、GC 可见持有、弱观察、Outer、业务结束与最终回收。本篇保留原专题定位；2026-10-25 的相关口径按 Epic 5.8 文档修订，未新增 UE 编译或运行验证。

> 在 C++ 项目里，通常用 RAII（unique_ptr、shared_ptr）管内存。但 Unreal 的 Actor、Component 不用这套——它们由 UObject GC 管理，有自己的生命周期规则。弄不清楚这两套体系的边界，就会写出悬空指针或内存泄漏。

---

## 为什么 Unreal 要自己实现 GC

普通 C++ 动态分配需要明确的释放责任，可以交给 RAII 包装；对象结束后继续访问可能导致未定义行为。UObject 则额外遵守引擎创建、引用和回收协议。

这不是项目大小决定 RAII 能否使用，而是 Unreal 还要维护以下对象系统能力：

```
为什么还需要引擎对象管理协议？

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
// 运行时创建一般 UObject 的常见入口；默认子对象另用相应构造协议
UMyComponent* comp = NewObject<UMyComponent>(owner, UMyComponent::StaticClass());

// 创建 Actor：必须用 SpawnActor，不能 new
AEnemy* enemy = GetWorld()->SpawnActor<AEnemy>(AEnemy::StaticClass(), location, rotation);

// 不符合 UObject 创建协议；不依赖它一定以哪种编译/运行失败表现
UMyComponent* comp = new UMyComponent();  // 错误！
```

**对象登记与可达性分开**：全局 UObject 登记使引擎知道对象身份，但“登记在册”不等于“是 GC 根”。判断保活仍需从根集合和引擎支持的引用关系出发。

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
1. GC 从根集合与引擎登记的引用出发
2. 沿 GC 可见引用关系追踪，反射属性是常见来源
3. 标记所有可达的 UObject
4. 未被标记的 UObject → 不可达 → 销毁

如果 RawEnemy 没有 UPROPERTY：
  GC 看不到这个引用
  → 即使 AMyActor 还活着，RawEnemy 也可能被判定不可达
  → RawEnemy 被销毁
  → AMyActor 继续访问 RawEnemy → 崩溃
```

**实际规则**：
- 长期 UObject 成员引用：通常使用可达宿主上的 `UPROPERTY() TObjectPtr<T>`；宿主不可达时，单独加宏不保证目标继续存活。
- 不需要保活的观察：使用 TWeakObjectPtr；短期裸指针借用另查有效区间，不要求把所有局部参数都改成属性。
- native 拥有者：可用 TStrongObjectPtr 或相应显式引用收集机制；UPROPERTY 不是唯一 GC 引用入口。
- 容器中的 UObject 引用要通过受支持的反射/引用收集路径接入；普通 std::vector 不会自动替其 UObject* 元素建立 GC 强持有。

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

引擎对象由各自的 World/框架关系维持，不在未核源码时一概写成“都调用 AddToRoot”。对业务对象，先选择合适持有者和持有方式；显式加根必须有对应解除责任，也不能据此阻止 Actor 的游戏销毁流程。

---

## 两阶段销毁：BeginDestroy → FinishDestroy

UObject 的销毁不是立即完成的，而是分两个阶段：

这是一条职责链，不是立即释放的时间承诺：

```text
BeginDestroy：开始自身的清理/资源释放协议
    ↓
IsReadyForFinishDestroy：按真实完成信号及父类条件判断能否继续
    ↓
FinishDestroy：最后的内部清理，之后由引擎完成后续销毁/释放
```

重写这些钩子时保留父类协议。不能用“某个异步句柄是否仍有效”替代操作是否真正完成，也不在这里临时开始依赖其他业务 UObject 的交互。主要 Actor 游戏清理应更早放在 EndPlay；独立资源与 GPU 协作按各系统实现。[Epic：Actor Lifecycle](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle)

**为什么需要两阶段？**

```
GPU 资源（RHI）通常在渲染线程上管理。
当游戏线程决定销毁一个 Mesh Component 时：
  BeginDestroy：通知渲染线程"准备释放这个 Mesh 的 GPU 资源"
  IsReadyForFinishDestroy：等渲染线程确认 GPU 资源已释放
  FinishDestroy：最后内部清理，随后由引擎完成后续销毁/释放

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

// 弱观察：不保活，失效通过 Get/IsValid 等弱指针接口检测
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

对于**不继承 UObject** 的普通 C++ 对象，可使用 Unreal 的原生智能指针。按所有权和删除器语义选择；没有目标负载测量，不概括为一定比 std::shared_ptr 更快：

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
TSharedPtr<T>：用于合适的非 UObject 堆对象；F 前缀不意味着必须共享分配
UPROPERTY() T*：用于 UObject 继承体系（AActor、UComponent 等）

不要对 UObject 用 TSharedPtr！
  UObject 由 GC 管理，生命周期不归引用计数控制
  通用删除器与 UObject 创建/回收协议不匹配；不能依赖一定以哪种崩溃表现
```

---

## GC 触发时机与性能

收集请求、可达性分析、清理与工作集变化分别观察。自动触发和增量/并行策略随版本、配置与负载变化，不给所有项目写死 60 秒周期或几毫秒开销。

在本篇的调试场景中可以请求：

```cpp
GEngine->ForceGarbageCollection(true);
```

这里 `true` 是 **bFullPurge**，不是“同步等待”。[UEngine API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UEngine?lang=en-US)说明它更新计时，使收集在下一次合适机会运行。请求返回后，另行观察弱有效性、销毁阶段及引用链；不要立即据此断言目标或全部原生资源已释放。

GC 成本需要在实际场景记录对象数、引用图、配置、Trace 与运行环境。本篇未补 UE 性能测量，不能由普通结构体、智能指针类型或“增量”名称推导固定性能收益。

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
    FDamageData data { 100 };  // 普通值对象；是否优化与具体构建有关
    ApplyDamage(data);
}
```

---

## 小结

- **UObject GC**：从根与已登记引用关系判断可达性；收集/清理时机与耗时按版本、配置和负载核对
- **持有与观察分开**：可达宿主的反射强引用、native 显式强持有和弱观察分别选择；Outer 不提供无条件保活保证
- **AddToRoot**：显式根持有需要与 RemoveFromRoot 配对；解除根不等于立即回收
- **分阶段清理**：BeginDestroy 发起清理，IsReadyForFinishDestroy 判断真实完成条件，FinishDestroy 完成最后清理；业务结束更早处理
- **智能指针**：UObject 用 UPROPERTY + TWeakObjectPtr；纯 C++ 对象用 TSharedPtr / TUniquePtr；不要混用
- **性能**：按目标版本和场景测量；不需要 UObject 能力的数据可以考虑普通值对象，但不承诺固定收益
