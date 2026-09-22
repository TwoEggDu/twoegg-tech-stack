---
title: "UE C++ 第一课：一个旋转 Actor 怎样连接代码、反射和蓝图？"
slug: "ufs-03b-first-rotating-actor"
date: "2026-09-22"
description: "在同一个 TwinArena 工程中创建带可见网格的旋转 Actor，连接完整 UE C++、反射、蓝图默认值和实例配置，再通过断点、帧率故障与 Windows 独立包验证行为。"
tags: ["Unreal", "C++", "Blueprint", "Debugging", "Windows"]
series: "UE 全栈工程师成长路线"
series_order: 3.5
weight: 4604
---

把旋转公式写进 Tick 很容易。真正让 UE 初学者卡住的，通常是另几个问题：类已经编译，为什么场景里什么都没有？C++ 默认速度改了，为什么蓝图还是旧值？`new` 一个组件为什么不等于把它正确放进 Actor？

这一课把这些问题接成一个能动手验证的小场景：**C++ 创建组件并负责旋转，蓝图选择网格/材质并设置默认速度，关卡实例覆盖自己的速度**。最后在 Windows 包里复验，并故意漏乘 DeltaTime，用断点找到原因。

这是 **UFS-03 子课 B**，对应 M0-T04/T06/T07，并与 [工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}}) 共用 T05 打包步骤。前置是能完成上一课 TwinArena 的正常 Editor 构建，以及 [普通 C++ 所有权课]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})。它为 UFS-04 的生命周期/状态归属准备观察入口，不提前展开重生、切图、网络权威或完整 UI。

> 安装目标：用户选定的 **UE 5.8.2**。本文按 2026-09-22 可访问的官方 **5.8 文档**核对宏、属性、指针和 API；未在 UE 5.8.2 编译/运行。以下画面、日志和数值是**验证预期**，不是作者已测回执，patch、工具链、蓝图行为与包内调试仍需实际确认。

## 1. 先确定谁决定什么

| 层 | 本课负责的状态 | 你应该到哪里检查 |
| --- | --- | --- |
| 原生 `ARotatingActor` | 有一个网格组件；每帧按秒换算角度；默认 45 度/秒 | `.h/.cpp`、正常编译产物、断点 |
| `BP_RotatingActor` | 继承原生类，选择可见网格与材质，默认 60 度/秒 | 蓝图的 Components / Class Defaults |
| 关卡实例 | 位置、比例，以及可选的单个实例速度覆盖 | 关卡中选中实例后的 Details |
| 运行中的实例 | BeginPlay 后实际读到的速度、每帧 DeltaTime | Output Log、断点、实际画面 |

蓝图资产不是一个已经在场景运行的对象，原生类也不是关卡实例。类似 Unity 的“组件代码—可配置资产—场景实例”可以帮助你找入口，但 UObject/CDO/蓝图生成类有自己的初始化与序列化规则，不能把它们当作 C# 类/Prefab 的同义词。

旋转模型只有一条：

```text
本帧请求角度（度） = 角速度（度/秒） × 本帧游戏时间（秒）
```

这里使用绕本地 Z 轴的 Yaw，`FRotator(Pitch, Yaw, Roll)` 的角度单位是**度**。本课无物理模拟、无旋转碰撞反馈、无时间缩放；后续这些条件变化时，要重新检查时间和变换的适用边界。

## 2. 在 TwinArena 中创建原生类

1. 打开上一课同一个 **TwinArena**，先保存。在 Editor Preferences 搜索 Live Coding，本课暂时关闭自动 Live Coding；这里只用正常编译建立可重复的基线。
2. 选择 **Tools → New C++ Class → Actor → Next**，名字输入 `RotatingActor`，不要输入前缀 `A`。确认模块是 `TwinArena`，选择 Public 放置方式，使头文件落在 `Source/TwinArena/Public/`，实现落在对应 Private 目录。
3. 创建文件后保存并关闭编辑器，再在 IDE/文本编辑器写下方完整代码。如果向导仍触发了自动编译，先等它结束，避免两路构建同时写产物；那次结果不代替写完代码后的正常构建。
4. 若向导生成在模块根目录，关闭编辑器后只移动这一对自有文件到 Public/Private，移走原件而非复制出两个同名类，然后刷新 IDE 工程文件。不要移动 `Intermediate` 里的生成文件。

入口按官方 [C++ Class Wizard](https://dev.epicgames.com/documentation/unreal-engine/using-the-cplusplus-class-wizard-in-unreal-engine) 核对；不同语言界面的名字可能不同。最终以磁盘上的文件路径、类名和构建结果为准。

## 3. 完整代码：一个组件，一项配置，一条时间公式

`TwinArena.Build.cs` 使用上一课的 `Core`、`CoreUObject`、`Engine` 依赖，无第三方插件、无 UnrealEd 依赖。下面两个文件可直接作为这一个类的实现，不包含省略的函数体。

### `Source/TwinArena/Public/RotatingActor.h`

```cpp
#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "UObject/ObjectPtr.h"
#include "RotatingActor.generated.h"

class UStaticMeshComponent;

UCLASS()
class TWINARENA_API ARotatingActor : public AActor
{
    GENERATED_BODY()

public:
    ARotatingActor();
    virtual void Tick(float DeltaTime) override;

    UFUNCTION(BlueprintCallable, Category = "Rotation")
    void SetRotationSpeed(float NewDegreesPerSecond);

protected:
    virtual void BeginPlay() override;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Rotation")
    float DegreesPerSecond = 45.0f;

private:
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components",
        meta = (AllowPrivateAccess = "true"))
    TObjectPtr<UStaticMeshComponent> MeshComponent;

    double WindowSeconds = 0.0;
    double WindowAppliedYaw = 0.0;
    FString RunId;
};
```

### `Source/TwinArena/Private/RotatingActor.cpp`

```cpp
#include "RotatingActor.h"

#include "Components/StaticMeshComponent.h"
#include "Misc/CommandLine.h"
#include "Misc/EngineVersion.h"
#include "Misc/Parse.h"

DEFINE_LOG_CATEGORY_STATIC(LogTwinArenaM0, Log, All);

namespace
{
    // Labels the teaching source scenario; build attempts are identified outside the binary.
    constexpr TCHAR TutorialSourceLabel[] = TEXT("baseline");
}

ARotatingActor::ARotatingActor()
{
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.bStartWithTickEnabled = true;

    MeshComponent = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Mesh"));
    SetRootComponent(MeshComponent);
    MeshComponent->SetMobility(EComponentMobility::Movable);
    MeshComponent->SetSimulatePhysics(false);
    MeshComponent->SetCollisionEnabled(ECollisionEnabled::NoCollision);
}

void ARotatingActor::BeginPlay()
{
    Super::BeginPlay();

    WindowSeconds = 0.0;
    WindowAppliedYaw = 0.0;
    RunId = TEXT("unrecorded");
    FParse::Value(FCommandLine::Get(), TEXT("M0RunId="), RunId);

    UE_LOG(LogTwinArenaM0, Display,
        TEXT("M0Begin SourceLabel=%s Run=%s Engine=%s Object=%s SpeedDegPerSec=%.2f HasMesh=%d"),
        TutorialSourceLabel, *RunId, *FEngineVersion::Current().ToString(),
        *GetPathName(), DegreesPerSecond,
        MeshComponent->GetStaticMesh() != nullptr ? 1 : 0);
}

void ARotatingActor::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    const double DeltaYaw = static_cast<double>(DegreesPerSecond) * DeltaTime;
    AddActorLocalRotation(FRotator(0.0, DeltaYaw, 0.0));

    WindowSeconds += DeltaTime;
    WindowAppliedYaw += DeltaYaw;
    if (WindowSeconds >= 5.0)
    {
        UE_LOG(LogTwinArenaM0, Display,
            TEXT("M0Window SourceLabel=%s Run=%s Object=%s Seconds=%.3f AppliedYaw=%.3f RateDegPerSec=%.3f"),
            TutorialSourceLabel, *RunId, *GetPathName(), WindowSeconds,
            WindowAppliedYaw, WindowAppliedYaw / WindowSeconds);
        WindowSeconds = 0.0;
        WindowAppliedYaw = 0.0;
    }
}

void ARotatingActor::SetRotationSpeed(float NewDegreesPerSecond)
{
    DegreesPerSecond = NewDegreesPerSecond;
    // Begin a new observation window so old and new speeds are not averaged.
    WindowSeconds = 0.0;
    WindowAppliedYaw = 0.0;

    UE_LOG(LogTwinArenaM0, Display,
        TEXT("M0Speed Object=%s SpeedDegPerSec=%.2f"),
        *GetPathName(), DegreesPerSecond);
}
```

为什么多了五秒观察窗口？它让你看到输入时间、累计请求角度和换算速度，又不在每帧刷日志。五秒是累计 **DeltaTime 的游戏时间**，不是承诺严格每五秒墙钟打印；窗口也会包含最后跨过阈值的一帧。`AppliedYaw` 记录我们送出的旋转量，**不是对最终渲染画面或物理结果的独立测量**，所以还要看画面与变换。

`TutorialSourceLabel` 是教学源场景标签：正常基线保持 `baseline`，故障练习可改为 `fault-delta-time`，修复版按实际输入决定恢复 `baseline` 还是使用 `fixed-delta-time`。相同源码/资产/配置/构建规则的重复构建不改这个常量。每次构建尝试唯一的 build_id 由第二篇的输出目录、`build-identity.txt` 和外部 manifest 记录，不编进代码。`RunId` 由命令行 `-M0RunId=run-001` 提供；编辑器未传参数时会明确记 `unrecorded`，每次正式独立运行都应使用新 run_id。运行回执把 run_id 关联到 build_id 和实际 exe；日志标签本身不能取代 source_revision、产物哈希与 PDB 匹配。[FParse API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Core/FParse)

## 4. 这些 UE C++ 写法分别补了哪一层

### 普通 C++ 类与 UObject/Actor

`ARotatingActor` 仍然是一个 C++ 类，`: public AActor`、构造函数、虚函数 `override` 都是普通 C++。AActor 是 UObject 派生体系中的世界对象；放入关卡、组件、Tick 等行为来自引擎约定。并非所有 UObject 都自带 Actor 的世界/Tick 能力。

前缀帮助读类型：`A` 常用于 Actor 派生类，`U` 用于其他 UObject 派生类，`F` 常用于普通结构/值类型，`T` 常用于模板。它们是 UE 的命名约定，不能只看前缀就决定释放方式。`TWINARENA_API` 的作用见上一课模块导出边界。

### 反射不是“给 C++ 换一种语法名字”

| 写法 | 在本课中的作用 | 不要误解成 |
| --- | --- | --- |
| `UCLASS()` | 声明参与 UE 对象/反射体系的类 | 任意普通 C++ 类加一个宏就可随意 new/delete 的对象 |
| `GENERATED_BODY()` | 接入 UHT 生成的类配套声明 | 可以手写一个空宏跳过生成 |
| `RotatingActor.generated.h` | 配套生成声明的包含入口 | 自己维护的头文件 |
| `UPROPERTY(EditAnywhere)` | 允许在默认对象/实例的属性编辑器中配置 | 自动允许蓝图图表读写 |
| `BlueprintReadWrite` | 允许蓝图图表读写该字段 | 自动同步网络或自动写入存档 |
| `VisibleAnywhere, BlueprintReadOnly` | 组件引用可见、图表可读，不允许通过该字段随便替换指针 | 组件内部的 Mesh/Material 设置全都只读 |
| `UFUNCTION(BlueprintCallable)` | 让 `SetRotationSpeed` 能成为蓝图调用节点 | 所有原生函数都要加此宏才能在 C++ 调用 |

`generated.h` 必须是这个头文件中**最后一个 `#include`**，不是说它必须放在整个文件最后一行。前置声明可以在它后面。宏拼错、类名/文件名错或 UHT 失败时，先看首个 UHT 错误，不要手建 `RotatingActor.generated.h`。[Epic：Objects 的头文件格式](https://dev.epicgames.com/documentation/unreal-engine/objects-in-unreal-engine)

`EditAnywhere` 与蓝图读写是两组维度。我们不把速度限制为正数，因为稍后要验证零速与反向旋转。`WindowSeconds` 等普通成员没有反射宏，它们仍然是正常 C++ 状态，只是不出现在属性编辑界面。[Property Specifiers](https://dev.epicgames.com/documentation/unreal-engine/unreal-engine-uproperties)、[UFunctions](https://dev.epicgames.com/documentation/unreal-engine/ufunctions-in-unreal-engine)

代码所需类型只读到够用：`FString` 保存本次运行字符串；`TEXT` 创建 TCHAR 字面量，`*FString` 给 `%s` 提供字符指针。`FName` 适合引擎标识，`FText` 适合需要本地化的用户文字，不必为了日志全换成 FText。`FVector` 表达位置/向量，`FRotator` 表达欧拉角；`TArray<T>` 是 UE 容器，动态增容同样需要考虑元素地址失效。本例无需为了练类型额外造容器。

### 构造、CDO、BeginPlay 与 Tick

构造函数负责轻量默认值与默认子对象，不查询“现在玩家是谁”，不开始业务请求。UE 需要类默认对象 **CDO**，编辑器还会创建模板/预览相关对象，因此构造日志次数不能直接当作进入游戏的 Actor 数量。蓝图生成类也有自己的默认值，保存到资产里的覆盖会参与后续实例初始化。[Epic：Objects](https://dev.epicgames.com/documentation/unreal-engine/objects-in-unreal-engine)

BeginPlay 用来观察已经进入游戏阶段的当前实例；本例在这里记录实际速度和对象路径。Tick 用本次实例的状态按帧执行。`Super::BeginPlay()` / `Super::Tick()` 保留父类流程；尤其不要同时在蓝图 Event Tick 再加一份旋转，否则你测到的是两个行为叠加。

### 创建方式和持有方式要一起看

| 你要做什么 | 本课要记住的入口/约束 |
| --- | --- |
| 在构造中建立固定组件结构 | `CreateDefaultSubobject<UStaticMeshComponent>`，设置为根；不是每帧新建组件 |
| 在运行中创建一般 UObject | `NewObject<T>`，并安排 GC 可见的持有关系；需要注册的动态组件还要履行组件注册流程 |
| 向 World 生成 Actor | World 的 `SpawnActor<T>`；不是拿 NewObject 当作完整 Actor 生成流程 |
| 长期保存 UObject 成员引用 | 可达持有者上的 `UPROPERTY() TObjectPtr<T>`；普通 C++ 私有可见性不妨碍反射登记 |
| 观察可能消失的 UObject | `TWeakObjectPtr<T>`，不保活，使用前 `Get()`/`IsValid()` 检查 |
| 结束 Actor 的游戏生命 | `Destroy()` 走引擎流程；不等于马上执行普通析构并释放全部内存 |

这里 `MeshComponent` 是由引擎创建的默认子对象，又有反射可见成员与 Actor 组件关系。不能只记“有 TObjectPtr 就永远不会被回收”：普通局部/未登记字段不会自动成为 GC 根，持有者本身也必须可达。`Outer` 提供对象归属/路径等关系，**不能单靠 `NewObject(..., Outer)` 推出 Outer 无条件强持有全部子对象**。[Object Pointers](https://dev.epicgames.com/documentation/unreal-engine/object-pointers-in-unreal-engine)

本站旧文 [UObject 系统]({{< relref "system-design/ue-01-uobject-system.md" >}}) 的 Outer 强持有表述已在主计划 R-03 登记，本课不采用它。普通 `std::unique_ptr`、`std::shared_ptr`、UE 通用 `TSharedPtr` 都不用于接管 UObject 的 delete；原生资源仍可使用第一课的 RAII。Actor 的 Destroy 是延迟处理的引擎操作，对象的游戏可用性与最终 GC/内存回收要分开观察。[AActor::Destroy](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/AActor/Destroy)

## 5. 正常编译，再让它在场景里真正可见

### 5.1 编译与创建蓝图

1. 关闭编辑器，按上一课构建 **TwinArenaEditor / Win64 / Development**。出现 `RotatingActor.generated.h` 相关错误时，从 UHT 第一处错误开始；找不到组件头文件时核对 include 和 Engine 依赖。
2. 构建成功后重新打开工程。在 Content Drawer 的设置中显示 C++ Classes，找到 TwinArena 下的 RotatingActor。若找不到，先核对实际加载工程/模块和构建结果，不重复创建同名类。
3. 在 `Content/Blueprints` 创建该类的蓝图子类，命名 **BP_RotatingActor**。可从原生类右键 Create Blueprint class based on RotatingActor，或新建 Blueprint Class 后在 All Classes 搜索它。
4. 打开蓝图，在 Components 中应看到继承的 **Mesh** 根组件。在它的 Details → Static Mesh 中使用资产选择器，开启 Show Engine Content，选择当前安装确实可用、预览看得见的简单 Cube 网格。记录选中资产的真实引用路径；**代码没有假定某个 Starter Content 路径一定存在**。
5. 如果当前安装没有可用基础网格，先准备一个合法的小型网格并导入 `Content/M0` 后再选它；这是未满足的资源条件，不是继续运行一个空组件。无需下载 Lyra 或 City Sample。

### 5.2 材质和不对称形状

在 `Content/M0` 创建材质 **M_M0Orange**。打开 Material Editor，把 Shading Model 设为 Unlit，放一个 Constant3Vector，设橙色，连接到 Emissive Color，Apply/Save。回到蓝图，给 Mesh 的 Materials 第一个槽指定此材质。它让这次几何体观察不依赖复杂灯光；这不是渲染效果教学。

选择蓝图的 **Class Defaults**，把 Rotation 分类下 **Degrees Per Second** 设为 **60**，Compile/Save。此时尚未创建关卡实例。

### 5.3 地图、实例和固定视角

1. **File → New Level → Basic**（或已有空白基础关卡），保存为 `Content/Maps/M0_Rotation.umap`。不要用大型开放世界模板作为本课前置。
2. 将 `BP_RotatingActor` 拖入关卡，位置设为 `(X=0,Y=0,Z=100)`，旋转全 0，Actor Scale 设为 `(2,1,0.5)`。非对称长方体比正方体容易看出绕 Z 旋转，均匀材质下也能观察朝向。
3. 在实例 Details 确认 Degrees Per Second 为 60、Mesh 的 Mobility 为 Movable、Simulate Physics 关闭、未勾 Hidden in Game。本课不让物理系统同时决定姿态。
4. 从 Place Actors 放置 **CameraActor**，位置 `(-600,0,200)`、旋转 `Pitch=-10,Yaw=0,Roll=0`，保留普通透视相机即可。固定相机朝向物体，避免独立包打开后视角落在地板里。
5. 为明确指定视角，选中该 CameraActor，打开此地图的 **Level Blueprint**。空白处右键创建所选 CameraActor 的引用；添加 **Event BeginPlay → Set View Target with Blend** 执行连线，Target 接 **Get Player Controller（Player Index 0）**，New View Target 接 CameraActor 引用，Blend Time 设为 0。Compile/Save。这条地图级连线只负责本课观察，不是完整相机系统。
6. Save All，点击 Play（PIE）。应从固定相机看到橙色长方体绕竖直轴旋转。打开 **Window → Developer Tools → Output Log**，搜索 `M0Begin` 与 `M0Window`。

如果画面空白，先在编辑器选中 Actor 按 F 聚焦：检查位置与相机朝向，再看 `HasMesh` 是否为 1，随后查可见性、材质槽和蓝图/地图是否保存。`HasMesh=1` 只证明设置了网格引用，不证明相机正在看它。

第一次先预测日志关键值，再看实际输出：蓝图默认 60，实例没覆盖，`M0Begin` 的 SpeedDegPerSec 应为 60；五秒窗口的 RateDegPerSec 应接近 60。前缀时间、路径和窗口 Seconds 随运行不同。普通编辑器启动 Run 为 unrecorded 是未传参数的表现，不要把它写成正式独立运行已通过。

## 6. 用默认值实验看清 C++、蓝图和实例

停止 PIE 后再改持久化配置，不把运行时临时值当作已保存资产。按顺序做：

| 操作 | 运行前预测 | 去哪里验证 |
| --- | --- | --- |
| C++ 45；蓝图默认明确保存为 60；实例没有覆盖 | 当前实例使用 60 | Class Defaults、实例 Details、M0Begin |
| 仅把该实例速度改为 -30 并保存地图 | 该实例反向，日志为 -30；蓝图资产仍是 60 | 对比资产与实例，不只看一个面板 |
| 再拖一个同蓝图实例到旁边 | 新实例应使用蓝图默认 60 | 两个不同 Object 路径与各自速度 |
| 将 C++ 头文件中的初始值改成 90，关闭编辑器、正常构建、重开 | 已明确保存的蓝图 60 和实例 -30 不应被当成“必须全部变 90” | 逐层检查覆盖；出现差异时记录真实加载/序列化行为 |
| 新建一个原生类的蓝图子类，不改速度 | 预期初始值来自当前原生默认 90 | 新蓝图 Class Defaults 与运行实例 |

实例属性旁的重置箭头通常是重置到其上层默认，不是跨过蓝图直接强制取最新 C++ 初始值。先观察“重置到谁”，再决定是否保存。编辑器重实例化、已有覆盖和模板数据会影响你看到的结果，所以本实验采用完整重开；若不符合预测，先查实际默认值和保存状态，不能一句“CDO 缓存”盖过证据。

做完后把 C++ 默认恢复为 45、主蓝图默认恢复为 60，最终验收地图只保留需要的一个旋转实例，速度 60；保存、关闭编辑器并重新构建。对照用的蓝图可以留在练习资产目录，记录其用途，不要误设为打包默认地图中的实例。

### 可选的小实验：让蓝图调用一次原生函数

在 **BP_RotatingActor 的 Event Graph** 连接 `Event BeginPlay → Delay（2 秒）→ Set Rotation Speed（0）`，Target 为 Self。保存后 PIE：预期先转后停，并有一次 M0Speed 日志；C++ setter 会重置观察窗口。它让你看到 `BlueprintCallable` 的作用，不需要造输入或 UI 系统。

做完删除/断开这条测试执行连线并 Compile/Save，确认再次 PIE 不会两秒后停，否则会干扰下面的恒速测试。不要在蓝图 Event Tick 重复旋转，也不要把运行时设成 0 误认为 C++ 默认值丢失。

## 7. 亲自命中断点，检查当前实例

在 `Tick` 的 `const double DeltaYaw = ...` 处设断点。可以用 VS 将 TwinArena 作为启动项目，选择 Development Editor 后启动；或附加当前 **UnrealEditor.exe 的 Native 调试器**，再点击 Play。

暂停后打开 **Debug → Windows → Modules**，确认加载的 TwinArena 模块路径、对应 PDB 和当前源码属于同次构建。打开 Call Stack/Locals/Watch，观察：

- `this`：确认是当前地图里的实例；不要用构造次数猜对象身份。
- `DegreesPerSecond`：先应为 60，与运行前实例配置一致。
- `DeltaTime`：是本帧游戏时间增量。正常运行约 60 fps 时可能接近 0.0167，但单步暂停会干扰节奏，不能要求每帧等于该数。
- `DeltaYaw`：在赋值语句执行后观察，60 度/秒 × 约 1/60 秒，预期约 1 度。读赋值前的未初始化局部值没有意义。

如果局部变量被优化不可见，不要伪造截图。先关闭编辑器，构建 **DebugGame Editor / Win64**，在对应配置从 VS 启动；手动启动对应编辑器时使用上一课真实路径变量：

```powershell
& $buildScript TwinArenaEditor Win64 DebugGame "-Project=$projectFile" -WaitMutex
if ($LASTEXITCODE -ne 0) { throw 'DebugGame Editor build failed' }
$editorExe = Join-Path $ueRoot 'Engine\Binaries\Win64\UnrealEditor.exe'
& $editorExe $projectFile -debug -log -M0RunId=editor-debug-001
```

此处 `$ueRoot/$projectFile/$buildScript` 来自上一课；新终端需先重新设置。启动后再次检查模块/PDB，而不是只相信选项框。Gameplay 断点应在 Play 时命中；构造断点可能在载入/模板阶段先停下，不能据此标记游戏逻辑已运行。

在持续运行的 Tick 中不宜每帧都暂停；观察到目标数据后禁用断点，让程序继续。帧率对照必须在没有调试暂停的独立运行中进行。

## 8. 对照矩阵：先记录预期，再填写实际值

先停止可选蓝图测试，保持单实例、无物理旋转、时间缩放为 1。PIE 里可以先试零速/反向；然后按 [工程地图第 6 节]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}}) 生成第一份基线包、关闭编辑器，再回本节做正式帧率对照。包内断点的详细步骤见本篇第 10 节。

在 Development 包中按控制台键（通常是 `~`）输入 `t.MaxFPS 30`、`t.MaxFPS 60`，用 `stat fps` 观察实际帧率。如果键盘布局没有打开控制台，在 Project Settings → Input → Console 查 Console Keys 并配置可用键后重新打包，不改 Shipping 包来做此实验。**帧率上限不是实际帧率保证**，硬件跑不到目标时记录实际值。

| 输入 | 预期方向/速度 | 观察方式 |
| --- | --- | --- |
| 0 度/秒 | 保持朝向；窗口请求角度与 Rate 接近 0 | 画面、变换、日志三者核对 |
| +60 度/秒 | 正向；累计 5 秒游戏时间约请求 +300 度 | Rate 接近 +60；检查朝向确实改变 |
| -60 度/秒 | 相反方向；5 秒约请求 -300 度 | Rate 接近 -60；不是靠镜头移动判断 |
| +60，实际约 30 fps | 每帧约 +2 度，按秒速度约 +60 | 记录实际 fps 与 DeltaTime，不开逐帧断点 |
| +60，实际约 60 fps | 每帧约 +1 度，按秒速度仍约 +60 | 对比同一输入的窗口 Rate 和画面 |

这些是公式预测，不是实测值。FRotator 显示可能归一化绕回，不能用窗口结束减开始的单个 Yaw 值直接当作无限累计角度。短时间朝向观察、累计请求量和帧率一起用，足够检验本课公式，但不是物理或性能基准。

零速/负速若用于包内验证，需要分别保存实例配置并构建新 ID 的包，或为受控实验加入明确的运行时设置路径；不要偷偷在旧包里改源码后声称已经生效。完成测试后恢复 60、移除临时行为再打最终包。

## 9. 故意漏乘 DeltaTime，完成一次定位和修复

先保存正确版本/输入快照。只把 Tick 中这一行：

```cpp
const double DeltaYaw = static_cast<double>(DegreesPerSecond) * DeltaTime;
```

改为故障版本：

```cpp
const double DeltaYaw = static_cast<double>(DegreesPerSecond);
```

其他日志累计代码保持不变。先写预测：这时“每秒 60 度”变成“每帧 60 度”。约 30 fps 时每秒请求约 1800 度，约 60 fps 时约 3600 度；画面甚至可能出现视觉混叠，看起来异常慢或方向奇怪，因此不能仅凭肉眼估速度。

将 `TutorialSourceLabel` 改为 `fault-delta-time`，并为改动后的真实源码记录新的 `source_revision` 或不可变快照。关闭编辑器→正常构建→重新运行，先在 Editor 断点比较 `DegreesPerSecond / DeltaTime / DeltaYaw`：当 DeltaTime 约 1/60，DeltaYaw 却仍为 60，单位已经不对。调用栈把你带到自有 Tick，窗口日志则显示每秒请求量随着实际帧率改变。两个证据指向同一个遗漏乘法，不需要先怀疑材质或垃圾回收。

随后按同一打包步骤为故障构建分配新的外部 build_id，保留第一份正确包，在新进程以新的 run_id 对照约 30/60 fps，并按第 10 节命中包内 Tick。故障归档目录和身份文件使用同一个 build_id，不能让坏包覆盖基线包；原始日志应能读回 `SourceLabel=fault-delta-time`，运行回执再把 run_id、build_id 与实际 exe 路径连起来。

修回乘法，按实际输入记录 source_revision 和标签：若完整恢复到与基线相同的不可变输入，可恢复 `baseline`；若形成新的修复提交/快照，可使用 `fixed-delta-time` 并记录该新身份。无论哪种情况，都为修复构建分配新的外部 build_id，执行正常构建与重新打包，再复跑零速、正负速度、约 30/60 fps 以及独立启动。记录故障输入、公式预测、真实变量/日志、修复 diff、修复后结果。修复后只在 PIE 看一眼，不能代替之前失败的独立包路径复验。

## 10. 打包后，在真正游戏进程里验证

回到 [工程地图第 6 节]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}}) 设置 Game Default Map、Cook 地图列表、Development 和符号，打包**本工程**。固定相机的 Level Blueprint 与 M0_Rotation 一并保存；独立包中必须看到同一个蓝图实例与行为。

关闭编辑器，从包的运行副本启动 `TwinArena.exe -log -M0RunId=package-001`，核对 `M0Begin` 的 SourceLabel、run_id、引擎版本、Object 与速度；再用运行回执中的 build_id 和实际 exe 绝对路径确认它来自哪个不可变归档。保存原始日志。日志位置以该进程实际输出为准：开发包可能使用包内项目 Saved/Logs，也可能走用户目录配置；不要套用 Unity Player.log 路径或断言永远只有一个目录。

再做一次包内断点：

1. 用 VS **Debug → Attach to Process**，选择这个包的实际游戏进程，调试代码类型为 Native。顶层启动器与 `Binaries/Win64` 下游戏进程可能不是同一个 PID，用路径和命令行辨认。
2. 在 Modules 找到包含自有代码的模块：Editor 通常是游戏 DLL，独立 Game 可能把游戏模块链接进 exe，**不要在包里强找 UnrealEditor-TwinArena.dll**。
3. 加载此次包对应的 PDB，确认 Symbols loaded；打开来自该输入 revision/快照的 `.cpp`。在 Tick 下断点，命中后查看 `this`、速度和调用栈。附加时 BeginPlay 往往已经过去，Tick 更适合首次包内命中。
4. 若 Development 优化影响局部值，保留“该变量不可见”的事实，优先看成员与调用栈。需要 DebugGame 包时，重新选择对应 Game 配置构建/打包，完整保存其独立 ID、PDB、输入与实际启动路径；不能给旧 Development exe 硬配 DebugGame PDB。

只有正确模块、匹配符号、对应源码、命中断点与变量观察同时成立，才记录“包内调试通过”。一个蓝色成功提示、一个 `.pdb` 文件或 AI 解释都不足以替代这些观察。

## 11. 常见卡点与验收

| 卡点 | 优先检查 |
| --- | --- |
| 编译成功但看不到 Actor | 是否真的拖了 BP 实例到当前地图；网格引用/材质/相机/隐藏状态 |
| 看得到但不转 | 是否 Play；Tick 是否启用；速度是否为 0；Mobility；是否被蓝图或物理覆盖 |
| 改 C++ 默认却仍旧值 | 实际模块版本；蓝图默认；实例覆盖；是否保存并重开 |
| 一边转一边额外抖动 | 蓝图 Tick/Construction Script/物理是否另改变换；本课只保留一处旋转 |
| 包中与 PIE 不同 | 地图/蓝图保存、Cook 资源、Game 配置、实际包 ID、启动视角 |
| 日志有值但断点不命中 | 实际进程/模块、加载 PDB 与源码、断点阶段、优化与旧产物 |

- [ ] 本人指出头文件最后一个 include、类名/API 宏、模块依赖；解释普通 C++ 与反射各做什么。
- [ ] 网格实际可见，根组件/Tick 正确；能区分蓝图资产、类默认与关卡实例。
- [ ] 完成默认值/覆盖实验，并留下当前实例身份与运行值。
- [ ] 在自有 Tick 命中断点，解释参数、成员、局部值与调用栈。
- [ ] 留下零速、正负速度、不同实际帧率的预测和真实结果；预测没跑不能打勾。
- [ ] 人为漏乘 DeltaTime，定位单位错误，修复后正常构建、重新打包与独立运行复验。
- [ ] 包、符号、source_revision/补丁或快照、tutorial_source_label、build_id/run_id 与原始日志可互相对应；读者本人能力仍按 M0 执行计划验收。

自测与参考答案：

1. 为什么构造函数里不开始“当前玩家”的游戏逻辑？**构造会涉及 CDO/模板等情境；本课默认结构在构造，当前游戏实例观察在 BeginPlay。**
2. `UPROPERTY` 与 `BlueprintReadWrite` 是同一个开关吗？**不是。** 前者登记属性，具体 specifier 分别控制编辑器、蓝图等能力，网络/存档也有额外要求。
3. 为何 Destroy 后不能立刻当作普通 delete 完成？**引擎的游戏生命周期、引用有效性和最终内存回收不是同一个瞬间。** 调用者应停止依赖已销毁 Actor，不用析构时点决定业务清理。
4. 窗口日志的 Rate 为 60 能证明画面一定转对吗？**不能。** 它度量的是请求角度；还需核查应用对象、最终变换与可见结果。

AI 可以帮助解释宏、检查 include/声明定义一致、拟定测试矩阵、比较修复前后的真实日志。先由本人预测，运行后再让 AI 分析偏差；要求它把证据和猜测分开。样板代码可以生成，但“这是谁的实例、谁持有组件、这次错误发生在哪一层”必须本人对着断点解释，不用 AI 自评分替代验收。

## 12. 已核对什么，仍缺什么

代码为本课自行编写的最小示例，未复制受限引擎源码。`AddActorLocalRotation` 的接口按 [官方 API](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/AActor/AddActorLocalRotation) 核对；上述宏、属性、指针、创建边界与工具入口依据官方 5.8 页面。页面提供的头文件位置是查询入口，不代表已经阅读或验证指定引擎 revision 的实现。

| 证据层 | 本文交付状态 |
| --- | --- |
| 正文与完整代码 | 已写；类名、路径、include、依赖、生成头顺序及日志/步骤做静态核对 |
| UE 编译与 UHT | 未执行，需目标 5.8.2 与真实工具链验证 |
| 蓝图/实例/CDO、画面与调试 | 未执行，表格是预期，不是截图或已测日志 |
| Build/Cook/Stage/Package、独立包故障回归 | 未执行，需按两篇联动步骤保留真实证据 |
| 个人能力与完整 M0 | 未验收；三篇成稿不等于全部 M0-T01–T08 通过 |

完成这次练习后，再进入路线 UFS-04 的创建、接管、销毁、重生与切图：先有一个能被看见、解释和定位的实例，再讨论状态究竟该放在哪个对象上。
