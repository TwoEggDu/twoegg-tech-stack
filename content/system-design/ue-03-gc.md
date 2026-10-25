---
title: "Unreal 引擎架构 03｜Unreal GC：标记清除、对象生命周期与 TObjectPtr"
slug: "ue-03-gc"
date: "2026-03-28"
description: "分清 UObject 的 GC 可达引用、弱观察与显式持有，理解 TObjectPtr、TWeakObjectPtr 和 AddToRoot 的责任，并区分请求收集、业务结束与最终清理。"
lastmod: "2026-10-25"
tags:
  - "Unreal"
  - "C++"
  - "GC"
  - "内存管理"
  - "UObject"
series: "Unreal Engine 架构与系统"
weight: 6030
---

> **对象与持有前置**：先读 [UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})，分开创建入口、GC 可见持有、弱观察、Outer、业务结束与最终回收。本篇保留原专题定位；2026-10-25 的相关口径按 Epic 5.8 文档修订，未新增 UE 编译或运行验证。

Unreal 的 UObject GC 不替普通 C++ 分配接管释放责任。对 UObject，保存裸地址本身不新增 GC 强引用；如果不存在其他有效持有，目标可能被回收，旧地址也不会因此自动变安全。反射字段是常见引用来源，但不是唯一机制。

---

## GC 的适用范围

```
受 GC 管理（需遵守规则）         不受 GC 管理（自己负责生命周期）
─────────────────────────────    ──────────────────────────────────
UObject 及所有子类               普通 C++ 类（无 UObject 基类）
  AActor                         TSharedPtr / TUniquePtr 管理的对象
  UActorComponent                FVector、FTransform 等值类型结构体
  UGameInstance                  原始指针指向的 non-UObject 对象
  UDataAsset
  等
```

---

## 可达性分析：UPROPERTY 是 GC 的眼睛

Unreal GC 使用**标记清除（Mark and Sweep）**算法：

1. **标记阶段**：从根集合与引擎登记的引用关系出发，沿 GC 可见引用判断可达性；对象注册表本身不意味着其中每个对象都是根
2. **清除阶段**：未被标记的 UObject 进入销毁流程

关键点：**普通裸地址不是强持有；UPROPERTY 是常见入口，也有显式强指针、引用收集等机制**。下面的属性成员示例以宿主可达为前提，UPROPERTY 也不表示能阻止 Actor 被显式 Destroy。

```cpp
UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()

    // ✅ 正确：GC 能看到这个引用，不会意外回收 MyComponent
    UPROPERTY()
    UMyComponent* MyComponent;

    // ❌ 危险：裸指针，GC 看不见，可能在某帧后变成悬空指针
    UMyComponent* DangerousPtr;

    // ✅ 正确：TArray 里的 UObject* 也需要 UPROPERTY
    UPROPERTY()
    TArray<UItemData*> Items;
};
```

---

## 根集与 AddToRoot

并非所有 UObject 都从 GWorld 可达。如果你在全局或静态变量中持有一个 UObject，它可能不在任何 Actor 的引用链上，会被 GC 认为是垃圾：

```cpp
// 全局静态指针，GC 看不见
static UMyManager* GManager = nullptr;

void InitManager()
{
    GManager = NewObject<UMyManager>(GetTransientPackage());
    // ❌ 此时 GManager 指向的对象可能随时被 GC 回收
}
```

解决方案：**AddToRoot**——手动将对象加入根集，GC 保证不回收它：

```cpp
void InitManager()
{
    GManager = NewObject<UMyManager>(GetTransientPackage());
    GManager->AddToRoot();  // ✅ 固定在根集，不会被回收
}

void ShutdownManager()
{
    if (GManager)
    {
        GManager->RemoveFromRoot();  // 解除固定，允许 GC 回收
        GManager = nullptr;
    }
}
```

**注意**：滥用 `AddToRoot` 会导致内存泄漏，只在确实需要全局持久对象时使用。

---

## 三种引用类型

### TObjectPtr（强引用，推荐用于 UPROPERTY）

UE5 引入，替代裸指针用于 UPROPERTY：

```cpp
UPROPERTY()
TObjectPtr<UStaticMeshComponent> MeshComp;  // UE5 推荐写法

// 等价的老写法（仍然有效）
UPROPERTY()
UStaticMeshComponent* MeshComp;
```

这里的强引用条件是可达宿主上的反射字段。未登记的局部/成员 TObjectPtr 不因类型名自动保活；也不把它的所有构建配置和内部表示概括成与裸指针完全等价。当前指针协议见 [Epic：Object Pointers](https://dev.epicgames.com/documentation/unreal-engine/object-pointers-in-unreal-engine?lang=en-US)。

### TWeakObjectPtr（弱引用，不阻止 GC 回收）

弱引用不会阻止 GC 回收对象，访问前必须检查有效性：

```cpp
TWeakObjectPtr<APlayerController> WeakPC;

void CachePlayerController(APlayerController* PC)
{
    WeakPC = PC;  // 持有弱引用，不阻止 GC
}

void UsePlayerController()
{
    // ✅ 使用前检查
    if (WeakPC.IsValid())
    {
        WeakPC->ClientMessage(TEXT("Hello"));
    }

    // 等价写法
    if (APlayerController* PC = WeakPC.Get())
    {
        PC->ClientMessage(TEXT("Hello"));
    }
}
```

适用场景：观察者模式、跨系统的松散引用、缓存（对象消失后引用自动失效）。

### TSoftObjectPtr（软引用，延迟加载）

不直接持有对象，只持有对象路径，需要时异步加载：

```cpp
UPROPERTY(EditDefaultsOnly)
TSoftObjectPtr<UTexture2D> LazyTexture;

void LoadTexture()
{
    // 同步加载（会阻塞，谨慎使用）
    UTexture2D* Tex = LazyTexture.LoadSynchronous();


}
```

异步加载还需管理请求、完成回调、捕获对象的有效期和加载后持有，不在这里用裸捕获 this 的片段替代完整协议。容器、Delegate 与回调寿命继续按 UCPP 后续专项补齐。

---

### TStrongObjectPtr 与原生持有者

普通 native 类不能靠随意写一个 UPROPERTY 宏就加入对象引用图；持有单个 UObject 时可使用适合该版本的 TStrongObjectPtr，其他显式引用收集方案按具体接口接入。它解除持有时不会立即 delete UObject，也不保证游戏资格或线程安全。不要将它泛化为 UObject 成员的默认选择，持有环与退出责任仍需检查。

---

## GC 触发时机

收集请求、可达性分析、销毁与清理不是同一个时刻。自动触发、关卡流程、增量/并行设置和实际耗时都要按目标版本与配置核对，不能从“UE5 有 GC”推导一个固定周期或停顿结论。

`GEngine->ForceGarbageCollection(true)` 的参数是 **bFullPurge**，不是“等待完成”。[UEngine 官方 API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UEngine?lang=en-US)说明它更新收集计时，使收集在下一次合适机会运行；返回时不能作为所有对象已清理的验收点。UCPP-1 将创建、请求和后续观察分成独立动作。

---

## 常见的 GC 相关 Bug

**Bug 1：裸指针悬空**
```cpp
// ❌ 错误
class FMySystem
{
    UDataTable* DataTable;  // 不是 UPROPERTY，GC 看不见

    void Init()
    {
        DataTable = LoadObject<UDataTable>(...);
        // 下一次 GC 运行后，DataTable 可能被回收
        // 之后访问 DataTable 导致崩溃
    }
};

// 修复方向：可达 UObject 的反射强引用，或 native owner 的显式强持有。
// 例如 native 类使用 TStrongObjectPtr<UDataTable>，并明确解除持有时机。
```

**Bug 2：Lambda 捕获的 UObject 失效**
```cpp
// ❌ 危险：Lambda 持有的裸指针不受 GC 保护
AMyActor* Actor = ...;
FTimerHandle Handle;
GetWorldTimerManager().SetTimer(Handle, [Actor]()
{
    Actor->DoSomething();  // Actor 可能已被 GC 回收
}, 2.f, false);

// ✅ 修复：用 TWeakObjectPtr
TWeakObjectPtr<AMyActor> WeakActor = Actor;
GetWorldTimerManager().SetTimer(Handle, [WeakActor]()
{
    if (WeakActor.IsValid())
        WeakActor->DoSomething();
}, 2.f, false);
```

**Bug 3：容器里的 UObject 未用 UPROPERTY**
```cpp
// ❌ 错误
TArray<UItemData*> Items;  // 没有 UPROPERTY，GC 不追踪

// ✅ 正确
UPROPERTY()
TArray<UItemData*> Items;
```

---

## GC 调试工具

调试时分别记录三类证据：

1. **构建与配置**：实际引擎版本、构建身份、GC/Cluster 设置与复现步骤。
2. **访问与阶段**：弱观察是否有效、是否发生 EndPlay/BeginDestroy；请求收集不作为完成回执。
3. **引用路径**：用目标版本支持的引用链/对象检查工具寻找实际持有者，排查属性、显式强指针、编辑器与 Cluster 等额外关系。

不要把一个内部状态标志当作长期保活证明，也不要用对象数、分配器容量或进程工作集替代引用路径。UCPP-1 的对照步骤先建立这些观察边界；具体调试工具需在所用 UE 版本实际执行确认。
