---
title: "Unreal 引擎架构 01｜对象系统：UObject、UClass 与 CDO"
slug: "ue-01-uobject-system"
date: "2026-03-28"
description: "Unreal 的一切托管对象都从 UObject 开始。理解 UObject、UClass 和 CDO 的关系，是读懂引擎源码、理解 GC 和反射的前提。"
tags:
  - "Unreal"
  - "C++"
  - "引擎架构"
  - "UObject"
  - "UClass"
series: "Unreal Engine 架构与系统"
weight: 6010
---

Unreal 引擎里几乎所有有意义的对象——Actor、Component、Asset、Widget——都继承自 `UObject`。这不是随意的设计，而是 Unreal 整个运行时的基石：GC、反射、序列化、网络复制，全部建立在 UObject 体系之上。

---

## UObject 是什么

`UObject` 是 Unreal 所有托管对象的基类。"托管"意味着：

- 由引擎的 GC 负责内存管理，不需要手动 delete
- 支持反射，运行时可以查询属性和方法
- 支持序列化，可以保存到磁盘（.uasset）或网络传输
- 支持编辑器集成，属性可以在 Details Panel 中显示和修改

不继承 UObject 的 C++ 类（比如纯粹的数学工具类、底层结构体）不受 GC 管理，需要自己负责生命周期。

---

## UClass：运行时类型信息

每个 `UCLASS()` 标注的类在运行时都有一个对应的 `UClass` 对象，存储该类的所有反射信息：

- 类名、父类指针
- 所有 `UPROPERTY` 的 `FProperty` 列表
- 所有 `UFUNCTION` 的 `UFunction` 列表
- 类的标志位（Abstract、Blueprintable 等）

```cpp
// 获取一个类的 UClass
UClass* MyClass = UMyObject::StaticClass();

// 运行时通过 UClass 创建对象
UMyObject* Obj = NewObject<UMyObject>(Outer, MyClass);

// 检查对象类型
if (Obj->IsA<UMyComponent>())
{
    // ...
}

// 遍历类的所有属性
for (TFieldIterator<FProperty> It(MyClass); It; ++It)
{
    FProperty* Prop = *It;
    UE_LOG(LogTemp, Log, TEXT("Property: %s"), *Prop->GetName());
}
```

`UClass` 本身也是 `UObject` 的子类（`UClass` → `UStruct` → `UField` → `UObject`），所以类信息也可以被序列化和引用。

---

## CDO：Class Default Object

每个 `UClass` 都有一个对应的 **CDO（Class Default Object）**，它是该类的默认实例，在引擎启动时自动创建。

CDO 的作用：
- **存储属性默认值**：在编辑器里对蓝图属性设置的默认值，实际上存在 CDO 上
- **作为属性比较基准**：序列化时只保存与 CDO 不同的属性值，节省空间
- **Blueprint 继承**：子蓝图的 CDO 从父类 CDO 复制初始值

```cpp
// 获取 CDO
UMyActor* CDO = GetDefault<UMyActor>();

// CDO 上的值就是类的默认属性值
float DefaultHealth = CDO->Health;

// 修改 CDO 会影响该类所有新创建对象的默认值
// （通常只在编辑器工具中这样做）
CDO->Health = 200.f;
```

**注意**：CDO 是单例，不要在游戏运行时修改它，除非你明确知道在做什么。

---

## UObject 的创建

永远不要用 `new` 创建 UObject 子类，必须使用引擎提供的工厂函数：

```cpp
// 通用方式：在指定 Outer 下创建对象
UMyDataAsset* Asset = NewObject<UMyDataAsset>(
    GetTransientPackage(),   // Outer：决定对象在对象树中的位置
    UMyDataAsset::StaticClass(),
    TEXT("MyAsset")          // 可选：对象名
);

// 在 Actor/Component 构造函数中创建子组件
UMyComponent::UMyComponent()
{
    MeshComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("MeshComp"));
    MeshComp->SetupAttachment(GetRootComponent());
}

// 在运行时 Spawn Actor（不用 NewObject）
AMyActor* Actor = GetWorld()->SpawnActor<AMyActor>(
    AMyActor::StaticClass(),
    SpawnLocation,
    SpawnRotation
);
```

`NewObject` 和 `SpawnActor` 的区别：
- `NewObject`：通用，适合任何 UObject 子类，不触发 BeginPlay
- `SpawnActor`：专门用于 AActor，会触发完整的 Actor 生命周期（BeginPlay 等）

---

## UObject 的生命周期

```
NewObject() / SpawnActor()
    │
    ▼
PostInitProperties()    ← 属性初始化完成后调用，适合做依赖初始化
    │
    ▼
PostLoad()              ← 从磁盘加载时调用（仅加载流程）
    │
    ▼
[正常使用]
    │
    ▼
BeginDestroy()          ← GC 决定回收时调用，开始异步清理
    │
    ▼
IsReadyForFinishDestroy() → true
    │
    ▼
FinishDestroy()         ← 实际销毁，释放资源
```

Actor 有更丰富的生命周期（BeginPlay / Tick / EndPlay），但底层仍走 UObject 这条链。

---

## Outer 与对象层级

每个 UObject 在创建时必须指定一个 **Outer**，Outer 决定该对象在对象树中的位置：

```cpp
// Package 是根节点，直接位于 Package 下的对象在编辑器中可见
UPackage* Pkg = CreatePackage(TEXT("/Game/MyPackage"));
UMyAsset* Asset = NewObject<UMyAsset>(Pkg, TEXT("MyAsset"));

// Actor 的 Outer 通常是 Level（UWorld 的一部分）
// Component 的 Outer 通常是它的 Owner Actor
```

对象树的重要性：
- **GC 可达性**：Outer 强引用子对象，子对象不会因为没有其他引用而被回收
- **序列化**：保存 Package 时会递归保存其下所有对象
- **路径寻址**：`/Game/Maps/TestMap.TestMap:PersistentLevel.BP_Player_C_0` 这样的路径就是沿 Outer 链拼接的

---

## 完整示例

```cpp
// 声明一个 UObject 子类
UCLASS(BlueprintType)
class MYGAME_API UInventoryItem : public UObject
{
    GENERATED_BODY()

public:
    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly)
    FString ItemName;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly)
    int32 StackSize = 1;

    UFUNCTION(BlueprintCallable)
    bool CanStack() const { return StackSize > 1; }
};

// 运行时创建并使用
void AMyCharacter::AddItem()
{
    UInventoryItem* Item = NewObject<UInventoryItem>(this);
    Item->ItemName = TEXT("Health Potion");
    Item->StackSize = 5;

    // 通过 UClass 获取类信息
    UClass* ItemClass = Item->GetClass();
    UE_LOG(LogTemp, Log, TEXT("Created: %s (IsA UObject: %d)"),
        *ItemClass->GetName(),
        Item->IsA<UObject>());

    // 通过 CDO 查看默认值
    const UInventoryItem* CDO = GetDefault<UInventoryItem>();
    UE_LOG(LogTemp, Log, TEXT("Default StackSize: %d"), CDO->StackSize);
}
```
