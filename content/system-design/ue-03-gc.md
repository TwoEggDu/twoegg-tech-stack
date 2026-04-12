---
title: "Unreal 引擎架构 03｜Unreal GC：标记清除、对象生命周期与 TObjectPtr"
slug: "ue-03-gc"
date: "2026-03-28"
description: "Unreal 的 GC 只管 UObject，靠 UPROPERTY 追踪引用链。理解 TObjectPtr、TWeakObjectPtr 和 AddToRoot 的区别，才能写出不崩溃的 Unreal C++ 代码。"
tags:
  - "Unreal"
  - "C++"
  - "GC"
  - "内存管理"
  - "UObject"
series: "Unreal Engine 架构与系统"
weight: 6030
---

Unreal 引擎有 GC，但它和 C# / Java 的 GC 很不同：**它只管理 UObject 子类，不管理普通 C++ 对象**。更关键的是，GC 的可达性分析依赖 `UPROPERTY` 宏——如果你用裸指针持有一个 UObject，GC 看不见这个引用，会把对象回收掉，留下悬空指针。

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

1. **标记阶段**：从根集（GUObjectArray 中标记为 Root 的对象，如 GEngine、GWorld）出发，通过 `UPROPERTY` 引用链递归标记所有可达对象
2. **清除阶段**：未被标记的 UObject 进入销毁流程

关键点：**只有 `UPROPERTY` 标注的指针才会被 GC 追踪**。

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

`TObjectPtr` 在编辑器模式下提供额外的访问追踪，Release 模式下与裸指针等价。

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

    // 异步加载（推荐）
    FStreamableManager& Manager = UAssetManager::GetStreamableManager();
    Manager.RequestAsyncLoad(LazyTexture.ToSoftObjectPath(),
        FStreamableDelegate::CreateLambda([this]()
        {
            UTexture2D* Tex = LazyTexture.Get();
            // 使用 Tex
        })
    );
}
```

---

## GC 触发时机

Unreal GC 不是实时的，它在特定时机才运行：

- **帧间自动触发**：每隔一定帧数，引擎检查是否需要 GC
- **内存压力触发**：内存使用超过阈值
- **手动触发**：`GEngine->ForceGarbageCollection(true)`（true = 等待完成）
- **关卡切换时**：加载新关卡前会强制 GC

GC 运行期间，**GameThread 会暂停**（Stop-the-World），这就是为什么大型场景切换时会有卡顿。UE5 引入了增量 GC 来缓解这个问题。

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

// ✅ 修复：让 FMySystem 继承 UObject，或把 DataTable 放到某个 Actor/Component 的 UPROPERTY 上
```

**Bug 2：Lambda 捕获的 UObject 失效**
```cpp
// ❌ 危险：Lambda 持有的裸指针不受 GC 保护
UMyActor* Actor = ...;
FTimerHandle Handle;
GetWorldTimerManager().SetTimer(Handle, [Actor]()
{
    Actor->DoSomething();  // Actor 可能已被 GC 回收
}, 2.f, false);

// ✅ 修复：用 TWeakObjectPtr
TWeakObjectPtr<UMyActor> WeakActor = Actor;
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

```cpp
// 打印所有 UObject 的内存占用统计
Obj.DumpAll  // 控制台命令

// 查看 GC 是否认为某对象可达
bool bReachable = !Obj->HasAnyFlags(RF_Unreachable);

// 强制 GC 并等待完成（调试用）
GEngine->ForceGarbageCollection(true);

// 查看引用链（谁在引用这个对象）
FReferenceChainSearch RefChainSearch(Obj, EReferenceChainSearchMode::Shortest);
RefChainSearch.PrintResults();
```
