---
title: "Unreal 引擎架构 02｜反射与序列化：UPROPERTY、UFUNCTION 的工作机制"
slug: "ue-02-reflection-serialization"
date: "2026-03-28"
description: "UPROPERTY 和 UFUNCTION 背后是 Unreal Header Tool 生成的完整反射系统。理解它如何工作，才能真正读懂引擎源码里那些看起来像魔法的宏。"
tags:
  - "Unreal"
  - "C++"
  - "反射"
  - "序列化"
  - "UPROPERTY"
  - "UHT"
series: "Unreal Engine 架构与系统"
weight: 6020
---

Unreal 的 `UCLASS()`、`UPROPERTY()`、`UFUNCTION()` 这些宏让很多初学者困惑——它们看起来像注解，但实际上驱动了整个引擎的反射、序列化、Blueprint 集成和网络同步系统。理解它们的工作机制，是读懂引擎源码的关键。

---

## UHT：宏背后的代码生成器

这些宏本身在 C++ 编译阶段几乎是空的（大多数展开为空或简单标记），真正的工作由 **UHT（Unreal Header Tool）** 完成。

UHT 在 C++ 编译之前运行，扫描所有带有这些宏的头文件，为每个 `UCLASS` 生成一个 `.generated.h` 文件，这就是为什么每个类的头文件末尾必须包含：

```cpp
#include "MyClass.generated.h"
```

生成的代码包含：
- `GENERATED_BODY()` 宏展开后的完整类声明（构造函数、StaticClass()、序列化函数等）
- 反射数据的静态初始化代码
- Blueprint 可调用的 thunk 函数

---

## FProperty 体系

每个 `UPROPERTY` 标注的成员变量，在运行时都对应一个 `FProperty` 对象，存储在该类的 `UClass` 里：

```
UClass
  └─ TArray<FProperty*> Properties
       ├─ FIntProperty    (对应 int32 成员)
       ├─ FFloatProperty  (对应 float 成员)
       ├─ FStrProperty    (对应 FString 成员)
       ├─ FObjectProperty (对应 UObject* 成员)
       └─ FArrayProperty  (对应 TArray 成员)
```

`FProperty` 存储了：属性名、类型、偏移量（在对象内存中的字节偏移）、属性标志（是否可编辑、是否复制等）。

通过偏移量，反射系统可以在不知道具体类型的情况下读写任意 UObject 的属性：

```cpp
// 通过反射动态读写属性
void SetPropertyByName(UObject* Obj, const FName& PropName, float Value)
{
    FProperty* Prop = Obj->GetClass()->FindPropertyByName(PropName);
    if (FFloatProperty* FloatProp = CastField<FFloatProperty>(Prop))
    {
        // ContainerPtrToValuePtr 根据偏移量得到属性的实际地址
        float* ValuePtr = FloatProp->ContainerPtrToValuePtr<float>(Obj);
        *ValuePtr = Value;
    }
}

// 使用示例
SetPropertyByName(MyCharacter, TEXT("Health"), 100.f);
```

---

## UFunction 与反射调用

`UFUNCTION()` 标注的函数同样有对应的 `UFunction` 对象，存储在 `UClass` 中，包含函数名、参数列表（每个参数也是 FProperty）、函数指针。

Blueprint 调用 C++ 函数，底层走的就是 `UFunction` 反射机制：

```cpp
// 通过名字查找并调用函数（Blueprint VM 的底层原理）
void CallFunctionByName(UObject* Obj, const FName& FuncName)
{
    UFunction* Func = Obj->FindFunction(FuncName);
    if (Func)
    {
        // ProcessEvent：Unreal 的通用函数调用入口
        // 参数通过栈内存传递，Func 知道参数布局
        Obj->ProcessEvent(Func, nullptr); // nullptr = 无参数
    }
}

// 带参数的调用需要构造参数结构体
UFUNCTION(BlueprintCallable)
void TakeDamage(float DamageAmount, AActor* DamageCauser);

// C++ 侧用反射调用
struct FTakeDamageParams
{
    float DamageAmount;
    AActor* DamageCauser;
};

FTakeDamageParams Params{ 50.f, AttackerActor };
UFunction* Func = MyChar->FindFunction(TEXT("TakeDamage"));
MyChar->ProcessEvent(Func, &Params);
```

---

## 序列化原理

`UObject::Serialize()` 是序列化的核心入口，它遍历 `UClass` 中的所有 `FProperty`，根据属性类型调用对应的序列化方法：

```cpp
// 引擎内部的序列化流程（简化）
void UObject::Serialize(FArchive& Ar)
{
    UClass* Class = GetClass();

    // 遍历所有需要序列化的属性
    for (TFieldIterator<FProperty> It(Class); It; ++It)
    {
        FProperty* Prop = *It;

        // 只序列化与 CDO 不同的属性（节省空间）
        if (!Prop->Identical_InContainer(this, Class->GetDefaultObject()))
        {
            Prop->SerializeItem(FStructuredArchive::FSlot(Ar),
                                Prop->ContainerPtrToValuePtr<void>(this));
        }
    }
}
```

序列化的两个方向：
- **保存（Save）**：对象内存 → FArchive → 字节流 → 磁盘/.uasset
- **加载（Load）**：字节流 → FArchive → 内存，触发 `PostLoad()`

---

## UPROPERTY 说明符的实际含义

```cpp
UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()

    // EditAnywhere：可在编辑器任何地方（场景实例 + 蓝图）修改
    // BlueprintReadWrite：蓝图可读写
    // Category：在 Details Panel 中的分组
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Stats")
    float Health = 100.f;

    // Replicated：通过网络同步到客户端
    // ReplicatedUsing：同步后在客户端触发 OnRep_Ammo()
    UPROPERTY(ReplicatedUsing = OnRep_Ammo)
    int32 Ammo;

    // Transient：不参与序列化，每次加载都是默认值
    UPROPERTY(Transient)
    float CachedDamageMultiplier;

    // VisibleAnywhere：编辑器只读，不可修改
    // Instanced：允许在编辑器中为每个实例指定不同的子对象
    UPROPERTY(VisibleAnywhere, Instanced)
    UInventoryComponent* Inventory;

    UFUNCTION()
    void OnRep_Ammo();
};
```

这些说明符本质上是标志位，存在 `FProperty::PropertyFlags` 中，引擎各系统读取这些标志位决定行为。

---

## 运行时反射的完整示例

```cpp
// 用反射遍历任意 UObject 的所有属性并打印
void PrintAllProperties(const UObject* Obj)
{
    if (!Obj) return;

    UClass* Class = Obj->GetClass();
    UE_LOG(LogTemp, Log, TEXT("=== %s ==="), *Class->GetName());

    for (TFieldIterator<FProperty> It(Class, EFieldIteratorFlags::IncludeSuper); It; ++It)
    {
        FProperty* Prop = *It;
        FString ValueStr;

        // ExportText：把属性值转换为可读字符串
        Prop->ExportTextItem_Direct(
            ValueStr,
            Prop->ContainerPtrToValuePtr<void>(Obj),
            nullptr,
            nullptr,
            PPF_None
        );

        UE_LOG(LogTemp, Log, TEXT("  %s = %s"),
            *Prop->GetName(), *ValueStr);
    }
}

// 动态设置任意属性（JSON 配置驱动初始化的常见做法）
bool SetPropertyFromString(UObject* Obj, const FString& PropName, const FString& Value)
{
    FProperty* Prop = Obj->GetClass()->FindPropertyByName(*PropName);
    if (!Prop) return false;

    // ImportText：从字符串解析并写入属性
    const TCHAR* Result = Prop->ImportText_Direct(
        *Value,
        Prop->ContainerPtrToValuePtr<void>(Obj),
        Obj,
        PPF_None
    );

    return Result != nullptr;
}
```

---

## Blueprint 为什么依赖反射

Blueprint 的变量、函数调用，在底层全部走反射接口：

- **Blueprint 变量**：每个蓝图变量对应 UClass 上的一个 `FProperty`
- **Blueprint 函数调用**：通过 `ProcessEvent()` 调用，运行时查找 `UFunction`
- **蓝图 Cast**：`Cast<T>` 在蓝图里就是 `UObject::IsA()` 的反射检查
- **Get/Set 节点**：读写操作通过 `FProperty::GetValue` / `SetValue` 实现

这就是为什么 `UFUNCTION(BlueprintCallable)` 能让 C++ 函数直接出现在蓝图节点列表里——UHT 生成了对应的 `UFunction` 对象和 thunk 包装函数。
