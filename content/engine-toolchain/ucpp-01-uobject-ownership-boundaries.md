---
title: "UCPP-1｜普通对象与 UObject：创建、指针、持有和结束的边界"
slug: "ucpp-01-uobject-ownership-boundaries"
date: "2026-10-25"
description: "把普通 C++、UObject 与 Actor 的创建、保存、保活、业务结束和最终回收分开，纠正 Outer 保活误解，并用一个自有 UE 样例设计强持有、弱观察与解除持有的对照。"
tags: ["Unreal", "C++", "UObject", "GC", "生命周期"]
series: "UE 全栈工程师成长路线"
series_order: 3.6
weight: 4605
---

> **读这篇之前**：先掌握 [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})、[CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) 和 [CXX-03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}})。动手部分接在 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}}) 与 [旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}}) 之后，沿用同一个 TwinArena。

> **版本与证据**：沿用本路线的 UE 5.8.2 目标；本篇于 2026-10-25 核对可访问的 Epic **5.8 文档**。未取得该 patch 的本地源码或可执行构建环境，下面的自有 UE 代码尚未经过 UHT、编译或运行。文档依据、验证设计和实测结果分别说明，不把预期日志写成已测日志。

一个对象不再参与游戏以后，保存它的变量可能仍然非空；另一个对象的创建函数早已返回，它却仍然需要被保活。把这两个现象都解释成“指针还在，所以对象还在”，很容易在 Unreal 中写错生命周期。

公共课已经解释了普通 C++ 的拥有、借用和析构。本篇继续问：**进入 UE 以后，哪种对象仍按普通 C++ 管理，哪种对象要接入引擎引用图，哪种对象还要先结束它在 World 中的游戏生命？**

## 1. 先把五件事分开

读一个成员或一次赋值时，留下五项记录：

| 问题 | 要找的证据 |
|---|---|
| 怎样创建 | 普通构造，还是 UObject、默认子对象、Actor 的创建入口 |
| 保存了什么 | 一个值、裸地址、反射字段、弱观察，还是显式强引用包装 |
| 谁负责保活 | 原生拥有者，还是从 GC 根可达的引用路径；持有者自己是否可达 |
| 何时停止业务 | 取消、解绑、EndPlay、Destroy 或项目自己的停用状态 |
| 何时最终销毁 | 原生析构/释放，还是引擎销毁与 GC 清理流程 |

“有一个地址”只回答了访问路径的一部分。它不能同时证明保活、业务资格、线程使用权限和最终释放时刻。

可以先画两张不同的图：

```text
命名/归属：Token --Outer--> 某个 Actor

保活路径：根可达对象 --GC 可见强引用--> Token
```

第一条关系本身，不能作为第二条路径已经成立的证明。后面会专门用对照来检查这个误解。

## 2. 三种对象，三种创建与结束协议

| 对象 | 常见创建方式 | 持有与结束方式 |
|---|---|---|
| 普通 C++ 对象，例如原生数据/文件句柄包装 | 局部对象、成员构造、MakeUnique/MakeShared 等 | 按 C++ 对象寿命和删除器协议清理；GC 不会替任意原生分配接管责任 |
| 一般 UObject | 运行时 NewObject；构造默认子对象时 CreateDefaultSubobject | 建立 GC 可见引用或合适的显式持有；不用普通 delete 或通用智能指针删除器接管 |
| Actor / Component | World 的 SpawnActor；固定组件结构使用 CreateDefaultSubobject，动态组件另有注册流程 | 除 UObject 的内存管理外，还有 World、组件注册和游戏阶段协议；Actor 通常以 Destroy 请求移除，组件有自己的销毁入口 |

这是常见入口的责任划分，不是“每个 UObject 都只会经过一次 NewObject”的完整引擎流程。加载、复制、蓝图与 CDO 还会进入各自路径。[Epic：Objects][objects]、[Actor Lifecycle][actor-life]

### 构造完成，不等于进入游戏

[旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}}) 在构造函数中创建 Mesh 默认子对象，在 BeginPlay 读取运行实例状态。这个分工保留：构造阶段主要建立轻量默认值和固定子对象，不把查询当前玩家、发请求等工作塞进去。编辑器模板、CDO 与游戏实例都可能涉及构造，不能数构造日志来推算实际游戏对象数。[Epic：Objects][objects]

同样，NewObject 返回一般 UObject，不会为它凭空建立 Actor 的 World 注册、BeginPlay 或 Tick。SpawnActor 的实际开始时机还与 World 状态和普通/延迟生成路径有关，不能把函数返回简单等同于“所有游戏初始化一定已经做完”。

### 普通资源可以放在 UObject 里面

两套机制可以组合。Actor 的普通成员可以持有 `TUniquePtr<FNativeData>`：这个包装的责任仍按普通 C++ 解释；它所在 Actor 的最终析构时机却由引擎管理。

如果原生资源应该在离开游戏时就释放，应在合适的业务清理阶段显式 Reset，不等待未来某次 GC。RAII 是后续清理的保障，不能替代及时停止任务、解绑回调和结束业务。公共原理回看 CXX-02，本篇不重新展开智能指针语法。

## 3. 保存指针时，要同时读持有者与字段协议

下面按本篇的游戏线程、一般运行对象场景阅读；不把它当成允许任意线程访问 UObject 的表格。

| 访问/持有形式 | 能表达什么 | 还需要核对什么 |
|---|---|---|
| 局部参数或短期借用的 `T*` | 一个地址，不会仅因复制地址就新增 GC 强持有 | 借用期间谁保持目标有效；是否跨越延迟调用、收集或业务结束 |
| 可达 UObject 上的 `UPROPERTY() TObjectPtr<T>` | 被反射系统识别的强引用字段，可参与 GC 引用图 | 持有者本身的可达性；目标是否已被显式结束或进入销毁流程 |
| 未登记的局部/成员 `TObjectPtr<T>` | 仍不能单凭类型名承诺 GC 保活 | 是否另有引擎登记/持有机制；不能把它当 std::shared_ptr |
| `TWeakObjectPtr<T>` | 不保活的观察；使用时通过 Get/IsValid 判断 | 检查结果只服务当前使用条件，不是跨线程、跨帧或业务授权保证 |
| `TSoftObjectPtr<T>` | 带资产路径的软引用，不强制立即加载，也不靠自身保活已加载目标 | 谁加载、谁维持加载后的使用区间；有路径不等于已有对象 |
| native 拥有者中的 `TStrongObjectPtr<T>` | 为 UObject 建立显式强持有；包装离开作用域或 Reset 时解除这份持有 | 解除持有不等于立即 delete；避免把它乱放入 UObject 成员形成难以清理的持有环 |
| `TUniquePtr/TSharedPtr` 等通用原生智能指针 | 管普通 C++ 对象的释放责任 | 不拿默认删除器接管 UObject；F/U/A 前缀也不能替代实际类型与协议检查 |

Epic 的指针指南将这些类型的 GC 影响分别列出；其对 `TObjectPtr` 的强引用说明以 `UPROPERTY` 字段为条件，`TStrongObjectPtr` 则面向非 UObject 拥有者。长期 UObject 成员优先用引擎支持的反射字段，不把所有持有都改成显式强指针。[Object Pointers][pointers]、[TStrongObjectPtr API][strong]

`Transient` 与“弱引用”是两回事。`UPROPERTY(Transient)` 限定持久化行为，不会把本来参与 GC 的强引用字段改成弱引用。`private` 也只是 C++ 访问控制，不会让一个已反射登记的字段从 GC 引用图中消失。[Unreal Object Handling][handling]

### 非空、有效、可用，仍是不同判断

一个来源可靠、当前有效的 UObject 可以使用 `Cast<T>` 检查引擎类型，失败时处理空结果；Cast 不负责为任意悬空地址恢复有效性，也不会新增保活。名字相似的 C++ cast 与 UE 类型系统要分别判断。

弱指针检查应靠近使用点。在本篇的游戏线程同步代码中，可以 `if (T* Object = Weak.Get())` 后短暂使用；不要把取出的裸指针保存到以后执行的任务，再说“我取的时候有效”。跨线程与 Delegate 的附加约定留到 UCPP-3/5，公共寿命与同步规则分别见 [CXX-05]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})、[CXX-11]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}})。[TWeakObjectPtr API][weak]

## 4. Outer、Owner、附着与强持有，不要画成同一条线

本系列旧文曾把 Outer 写成“强引用子对象，子对象不会因为没有其他引用而被回收”。这里修正：**Outer 描述对象的外层归属，参与命名/路径等关系；不能只因传了 Outer，就认定外层对象无条件强持有全部内部对象。**

判断保活时，仍需要找到从根可达的 GC 引用、显式登记或相应引擎机制。Actor 的 Owner、组件/场景的附着关系，也各有用途，不能凭“父”“拥有者”这些词推导任意对象的 GC 引用方向。

为什么现有 Mesh 示例里组件没有消失？因为它是按默认子对象协议创建的组件，又有反射成员、Actor 的组件管理等条件。这个样本同时具备多种关系，无法单独证明“只要 Outer 存在，任意 NewObject 都保活”。官方对象处理文档也分别说明了引用图、Actor/组件管理和 Cluster；不同条件不能缩成一个 Outer 规则。[Unreal Object Handling][handling]

这里也不反过来声称“Outer 在所有实现路径中绝不涉及 GC”。特定子对象、引用收集与 Cluster 行为需要固定版本的源码或实验。对业务持有的可依赖约定，先显式表达，再观察。

## 5. 自有验证样例：把保活关系变成可检查的对照

下面提供两个完整自有文件，放进已有 TwinArena 模块；依赖沿用 `Core`、`CoreUObject`、`Engine`，不需要 UnrealEd。`TWINARENA_API` 要与实际模块名一致。正常关闭编辑器后构建，不手建 generated.h。

样例创建三个普通 UObject，全部只保存弱观察；其中第二个另外放进反射强引用字段。原生资源单独使用 TUniquePtr。创建与请求收集是不同调用，避免把创建函数里尚未结束的临时变量当成对照结果。

| 目标 | Outer | 额外强持有 | 要检查的问题 |
|---|---|---|---|
| Id=1 | 探针 Actor | 无；只留弱观察 | 外层 Actor 仍在，能否仅凭 Outer 承诺目标保活 |
| Id=2 | 同一个 Actor | 可达 Actor 的 UPROPERTY 字段 | 保留字段与清空字段前后有什么区别 |
| Id=3 | Transient Package | 无；只留弱观察 | 换一个外层，弱观察本身是否变成了强持有 |

### Source/TwinArena/Public/UcppOwnershipProbe.h

```cpp
#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "UObject/Object.h"
#include "UObject/ObjectPtr.h"
#include "UObject/WeakObjectPtrTemplates.h"
#include "Templates/UniquePtr.h"
#include "UcppOwnershipProbe.generated.h"

struct FNativeOwnershipToken
{
    FNativeOwnershipToken();
    ~FNativeOwnershipToken();
};

UCLASS()
class TWINARENA_API UUcppOwnershipToken : public UObject
{
    GENERATED_BODY()

public:
    UPROPERTY(Transient)
    int32 Id = 0;

    virtual void BeginDestroy() override;
};

UCLASS()
class TWINARENA_API AUcppOwnershipProbe : public AActor
{
    GENERATED_BODY()

public:
    AUcppOwnershipProbe();

    UFUNCTION(BlueprintCallable, Category = "UCPP1")
    void CreateProbes();

    UFUNCTION(BlueprintCallable, Category = "UCPP1")
    void ReleaseProbes();

    UFUNCTION(BlueprintCallable, Category = "UCPP1")
    void RequestCollection();

    UFUNCTION(BlueprintCallable, Category = "UCPP1")
    void LogProbeState();

protected:
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

private:
    bool CanRunProbe() const;

    UPROPERTY(Transient)
    TObjectPtr<UUcppOwnershipToken> PropertyHeld;

    TWeakObjectPtr<UUcppOwnershipToken> OuterOnlySeen;
    TWeakObjectPtr<UUcppOwnershipToken> PropertySeen;
    TWeakObjectPtr<UUcppOwnershipToken> WeakOnlySeen;
    TUniquePtr<FNativeOwnershipToken> NativeOwner;
    bool bCreated = false;
    int32 Observation = 0;
};
```

### Source/TwinArena/Private/UcppOwnershipProbe.cpp

```cpp
#include "UcppOwnershipProbe.h"

#include "Components/SceneComponent.h"
#include "Engine/Engine.h"
#include "UObject/UObjectGlobals.h"

DEFINE_LOG_CATEGORY_STATIC(LogUcppOwnership, Log, All);

FNativeOwnershipToken::FNativeOwnershipToken()
{
    UE_LOG(LogUcppOwnership, Display, TEXT("UCPP1 Native created"));
}

FNativeOwnershipToken::~FNativeOwnershipToken()
{
    UE_LOG(LogUcppOwnership, Display, TEXT("UCPP1 Native destroyed"));
}

void UUcppOwnershipToken::BeginDestroy()
{
    UE_LOG(LogUcppOwnership, Display,
        TEXT("UCPP1 Token BeginDestroy Id=%d"), Id);
    Super::BeginDestroy();
}

AUcppOwnershipProbe::AUcppOwnershipProbe()
{
    PrimaryActorTick.bCanEverTick = false;
    SetRootComponent(CreateDefaultSubobject<USceneComponent>(TEXT("Root")));
}

bool AUcppOwnershipProbe::CanRunProbe() const
{
    return IsInGameThread() && HasActorBegunPlay() && !IsActorBeingDestroyed();
}

void AUcppOwnershipProbe::CreateProbes()
{
    if (!CanRunProbe() || bCreated)
    {
        return;
    }
    bCreated = true;
    NativeOwner = MakeUnique<FNativeOwnershipToken>();

    UUcppOwnershipToken* Token = NewObject<UUcppOwnershipToken>(this);
    Token->Id = 1;
    OuterOnlySeen = Token;

    PropertyHeld = NewObject<UUcppOwnershipToken>(this);
    PropertyHeld->Id = 2;
    PropertySeen = PropertyHeld.Get();

    Token = NewObject<UUcppOwnershipToken>(GetTransientPackage());
    Token->Id = 3;
    WeakOnlySeen = Token;

    LogProbeState();
}

void AUcppOwnershipProbe::ReleaseProbes()
{
    if (!CanRunProbe())
    {
        return;
    }
    PropertyHeld = nullptr;
    NativeOwner.Reset();
    UE_LOG(LogUcppOwnership, Display, TEXT("UCPP1 Released own strong references"));
    LogProbeState();
}

void AUcppOwnershipProbe::RequestCollection()
{
    if (!CanRunProbe() || !GEngine)
    {
        return;
    }
    UE_LOG(LogUcppOwnership, Display, TEXT("UCPP1 GC requested, not a completion receipt"));
    GEngine->ForceGarbageCollection(true);
}

void AUcppOwnershipProbe::LogProbeState()
{
    if (!CanRunProbe())
    {
        return;
    }
    ++Observation;
    UE_LOG(LogUcppOwnership, Display,
        TEXT("UCPP1 State N=%d Created=%d OuterOnly=%d PropertyTarget=%d WeakOnly=%d NativeOwner=%d"),
        Observation, bCreated ? 1 : 0,
        OuterOnlySeen.IsValid() ? 1 : 0,
        PropertySeen.IsValid() ? 1 : 0,
        WeakOnlySeen.IsValid() ? 1 : 0,
        NativeOwner ? 1 : 0);
}

void AUcppOwnershipProbe::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    UE_LOG(LogUcppOwnership, Display,
        TEXT("UCPP1 Actor EndPlay Reason=%d"), static_cast<int32>(EndPlayReason));
    PropertyHeld = nullptr;
    NativeOwner.Reset();
    Super::EndPlay(EndPlayReason);
}
```

代码刻意不在构造函数创建三个测试目标，不给它们 AddToRoot，也不把目标返回给蓝图或放进其他容器。观测使用弱指针，不在 GC 后解引用旧裸指针；`BeginDestroy` 日志也只记录自己的 Id，不调用其他业务对象。

`FNativeOwnershipToken` 没有 USTRUCT/UCLASS，它的构造/析构按普通 C++ 解释。`NativeOwner.Reset()` 会结束它独占管理的原生对象；`PropertyHeld = nullptr` 只解除一条强引用，不承诺对应 UObject 此刻已最终回收。

## 6. 怎样运行对照，怎样解释结果

### 先建立同一份工程与运行身份

1. 按工程地图完成正常构建，把探针放进一张专用测试地图。该 Actor 只有根组件，没有网格，不能以“场景里看不见”判断它不存在。
2. 打开 Level Blueprint，创建这一个探针实例的引用。添加键盘事件：1 调 CreateProbes，2 调 RequestCollection，3 调 LogProbeState，4 调 ReleaseProbes，5 调 Destroy Actor。均接同一实例；没有 Tick 自动调用或其他目标引用。若采用 Enhanced Input，也可以用等价测试按键，不为这篇建立完整输入系统。
3. 确认游戏视口获得输入，按 1 创建，等该事件结束后再按 2。随后另按 3 观察；不要把创建、请求收集和验收写成一个立即比较的蓝图执行链。
4. 按 4 解除本探针的强持有，再单独按 2、3 记录。`bCreated` 防止重复创建；下一轮用新的运行，不以同一实例重复叠加样本。
5. 优先用独立 Development 包执行基线，再单独记录 PIE；构建身份、引擎 Build.version、配置、exe/PDB、run_id 和日志对应起来。步骤沿用 UFS 工程地图，不新建第二套构建身份规则。

Level Blueprint 对探针 Actor 的引用是有意保留的控制条件：观察期间宿主仍在。它没有获得三个 Token 的强引用。还要记录 GC/Cluster 设置、是否处于调试或编辑器选择环境，避免把额外持有遗漏掉。

### 预期与实测必须分列

下面是验证设计，**实际结果尚未填写**：

| 阶段 | 模型预测 / 需要核对的现象 | 本轮实际结果 |
|---|---|---|
| CreateProbes 返回后 | 三个弱观察起初应能对应各自新对象，原生拥有者已建立 | 未运行 |
| 宿主可达、属性仍持有，发生有效收集后 | Id=2 的强引用应维持一般对象；Id=1/3 没有本例新增的强持有，不能以 Outer 或弱观察承诺它们继续存活 | 未运行 |
| ReleaseProbes 返回 | 原生 token 析构可在这次 Reset 调用中观察；Id=2 只解除本例的属性持有，不要求立即失效 | 未运行 |
| 再次有效收集后 | 没有其他持有/Cluster 等路径的普通测试目标可以回收；观察弱有效性及 BeginDestroy 记录 | 未运行 |
| 新一轮运行中不按 4，直接按 5 | Destroy 结束 Actor 的游戏阶段，EndPlay 清理本例持有；不能从返回值或 EndPlay 推断存储已全部释放 | 未运行 |

`ForceGarbageCollection(true)` 的参数是 **bFullPurge**。官方 UEngine API 将它描述为更新收集计时，在下一次合适机会运行；`true` 不是“函数在这里等待全部清理完毕”。请求日志与后续观察必须分开。[UEngine API：ForceGarbageCollection][engine-api]

不要写“等两帧必定回收”或“某个对象没消失，所以 Outer 保活成立”。如果弱观察仍然有效，先确认是否真正发生了收集，再排查反射属性、额外强指针、蓝图变量、编辑器/调试器引用和 Cluster。需要时按旧 GC 专题的引用链工具继续取证；没有证明所有持有都消失，就不能下唯一原因结论。

反过来，弱观察失效也不等于整个进程工作集立即下降，更不等于相关 GPU/其他原生资源已全部释放。对象可用性、销毁阶段和内存容量属于不同证据。

## 7. Actor 的业务结束，比最终内存回收更早

Actor 可能因为显式 Destroy、关卡退出、PIE 结束或其他引擎路径进入 EndPlay。EndPlay 适合取消业务、解除订阅和清理本篇主动持有的资源；它不是普通 C++ 析构函数的另一种拼写，也不在所有路径中等同于对象内存立即消失。流关卡快速重入等情形还需要单独核对复用行为。[Actor Lifecycle][actor-life]

因此，长期保存了一个 Actor 引用，也不能据此阻止别人按游戏规则 Destroy 它。调用者仍要检查目标是否可用、是否处于当前业务阶段。强引用解决一部分 GC 可达性问题，不提供“这个角色永远继续参战”的保证。

本例按 5 前可先创建目标；EndPlay 中主动清理原生拥有者和属性引用。之后不要再用保存的旧裸地址调用探针。需要观察 Actor 的外部系统应使用适合的弱观察，并将游戏阶段停止与最终 GC 分开记录。

BeginDestroy、IsReadyForFinishDestroy、FinishDestroy 属于后续销毁协议，不把主要跨对象游戏交互拖到那里。具体线程资源、GPU 释放等待和模块退出顺序继续看所属系统；本篇没有实现这些协议。

## 8. 带着这张检查表进入源码

遇到一个“拥有”“父对象”或指针成员，依次找：

1. **真实类型与创建点**：普通 C++、UObject、Actor 还是组件；当前走创建、加载还是复制路径？
2. **保存与保活证据**：反射成员在哪里，持有者是否可达，是否还有显式引用登记？
3. **调用资格**：只做类型检查够不够，是否已结束游戏、取消或进入销毁流程？
4. **结束顺序**：先停止外部调用，再解除自己的持有；最后才讨论析构、GC 和存储释放。

已有文章继续各司其职：

- [UObject、UClass 与 CDO]({{< relref "system-design/ue-01-uobject-system.md" >}})：对象、类型和默认对象；Outer 的旧保活表述已修正。
- [GC 与 TObjectPtr]({{< relref "system-design/ue-03-gc.md" >}})：引用图、强弱持有及排查入口。
- [Unreal GC 深入阅读]({{< relref "system-design/ds-23-unreal-gc.md" >}})：原生资源、显式引用与分阶段清理。
- [旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})：把对象、默认组件和运行阶段放回一个可见行为。

下一项是 UCPP-2“容器、字符串与模板惯用法的阅读”，仍待补写；从 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}}) 查看状态，不生成待写正文的假链接。

## 9. 本篇验证到了哪里

本轮核对了 Epic 5.8 文档中的对象创建、指针类型、Actor 阶段和收集请求语义；正文代码是自有样例，完成声明/定义、头文件、反射字段、调用顺序和弱观察路径的静态核对。相关目录、旧文口径和链接随正文同步。

**未完成**：UE 5.8.2 安装/源码核查、UHT/UBT、实际编译、PIE/独立包、Outer/属性对照、引用链取证、GC 日志及 Actor 退出验证。没有用普通 C++ 编译、引擎桩或预计日志代替这些结果，也没有据此更改 M0 的工程或个人能力验收。

<!-- DATA-TODO: 使用实际 UE 5.8.2 或明确替代版本，在 TwinArena 专用地图执行本篇两文件样例；记录 build_id/run_id、工具链、GC/Cluster 配置、正常构建与符号、四阶段弱观察/BeginDestroy/EndPlay 日志，以及未回收目标的引用链。分别记录独立包与 PIE，不覆盖本文未运行的历史说明，新增带日期的实测段。 -->

**创建入口决定对象怎样进入系统，真正的持有维持它的寿命，业务结束与最终回收分别完成。**

[objects]: https://dev.epicgames.com/documentation/unreal-engine/objects-in-unreal-engine
[pointers]: https://dev.epicgames.com/documentation/unreal-engine/object-pointers-in-unreal-engine?lang=en-US
[handling]: https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-object-handling-in-unreal-engine
[actor-life]: https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle
[strong]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/TStrongObjectPtr
[weak]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/TWeakObjectPtr
[engine-api]: https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/UEngine?lang=en-US
