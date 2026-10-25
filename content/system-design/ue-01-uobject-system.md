---
title: "Unreal 引擎架构 01｜对象系统：UObject、UClass 与 CDO"
slug: "ue-01-uobject-system"
date: "2026-03-28"
lastmod: "2026-10-25"
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

> **对象与持有前置**：先读 [UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})，分开创建入口、GC 可见持有、弱观察、Outer、业务结束与最终回收。本篇保留原专题定位；2026-10-25 的相关口径按 Epic 5.8 文档修订，未新增 UE 编译或运行验证。

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
- `NewObject`：运行时创建一般 UObject；不拿它替代 World 的 Actor 生成流程，一般 UObject 不因此获得 BeginPlay/Tick。
- `SpawnActor`：用于 World 中的 Actor；具体初始化/BeginPlay 时机还取决于 World 阶段和普通/延迟生成路径。构造默认子对象则使用对应的 `CreateDefaultSubobject` 协议。

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
FinishDestroy()         ← 最后内部清理，之后由引擎完成后续销毁/释放
```

Actor 有更丰富的生命周期（BeginPlay / Tick / EndPlay），但底层仍走 UObject 这条链。

---

## Outer 与对象层级

**Outer** 表达对象的外层归属，参与路径与命名；创建入口可以显式接收 Outer，也可能使用默认值。Package 等最外层情况另有约定，不能把它理解为任意对象都必须由调用者传一个非空父对象：

```cpp
// Package 是根节点，直接位于 Package 下的对象在编辑器中可见
UPackage* Pkg = CreatePackage(TEXT("/Game/MyPackage"));
UMyAsset* Asset = NewObject<UMyAsset>(Pkg, TEXT("MyAsset"));

// Actor 的 Outer 通常是 Level（UWorld 的一部分）
// Component 的 Outer 通常是它的 Owner Actor
```

对象树的重要性：
- **GC 可达性**：不能由“Token 的 Outer 是 Actor”推出“Actor 无条件强持有 Token”。需要找实际的反射强引用、显式引用登记或其他引擎持有机制；持有者自己也必须可达。
- **序列化**：Outer/Package 参与归属，但不能仅凭同一外层就承诺其中所有对象都会持久化；实际保存还取决于对象、属性标记和保存流程。
- **路径寻址**：外层关系参与对象路径，例如关卡、Actor 与组件的路径；路径关系与 GC 强引用分别检查。

默认子对象、Actor 的组件管理和 GC Cluster 可能同时参与持有，不能从一个组件暂时存活推导一般规则。本段修正原来的无条件 Outer 保活表述；[Epic：Object Handling](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-object-handling-in-unreal-engine)说明引用图与组件/Cluster 边界，具体对照见 UCPP-1。

---

数组元素、字符串角色与复制语义继续见 [UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})。下方 Inventory 保存对象指针包装，复制数组不会复制出整套 UInventoryItem 对象；ItemName 目前作为原始字符串，若承担稳定标识或可本地化界面文本，应按用途另选 FName/FText。

## 示例：创建后交给可达的 Character 持有

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

// 以下成员声明放在 AMyCharacter 的类体内：
// UPROPERTY(Transient)
// TArray<TObjectPtr<UInventoryItem>> Inventory;

// 以下函数定义放在相应 .cpp 中；这是接入现有 Character 的片段。
void AMyCharacter::AddItem()
{
    UInventoryItem* Item = NewObject<UInventoryItem>(this);
    Item->ItemName = TEXT("Health Potion");
    Item->StackSize = 5;
    Inventory.Add(Item);  // 可达 Character 的反射成员维持这条强引用

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
