---
title: "UCPP-4｜UHT/UBT：从生成文件到模块与反射接线"
slug: "ucpp-04-uht-ubt-generated-module-wiring"
date: "2026-10-25"
description: "沿一个自有反射函数与跨模块调用，分开生成、可见性、链接、装载和运行注册。"
tags: ["Unreal", "C++", "源码阅读"]
series: "UE 全栈工程师成长路线"
series_order: 3.9
weight: 4608
---

> **前置**：[CXX-07模板]({{< relref "engine-toolchain/cpp-foundations-07-template-reading.md" >}})、[CXX-08生成]({{< relref "engine-toolchain/cpp-foundations-08-macros-code-generation.md" >}})、[CXX-10编译链接]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}})。接入步骤复用[UE工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})。

> **版本与证据**：目标UE5.8.2，2026-10-25核对Epic5.8官方文档。M0尚未确认TwinArena实际路径和可用UE构建入口；本篇提供完整自有源文件与接入/故障步骤，未取得UHT、编译、链接或运行回执。

一个头文件已经写了UFUNCTION，编辑器却找不到函数；另一个函数声明可以跳转，最终仍链接失败。两者都不能只用“宏没生效”解释：生产者、消费者和生效时刻分别不同。

本篇沿一个自有反射函数，接一条跨模块普通C++调用。这样能同时看到生成链与链接链，又不需要复制引擎的生成实现。

## 1. 先把接线分成六个阶段

| 阶段 | 本例输入/输出 | 什么证据才能闭合 |
|---|---|---|
| 声明 | WiringToken.h的UCLASS/UFUNCTION | 源文件确属当前输入快照 |
| 构建组织 | Target、Build.cs、uproject、UHT manifest | 真实目标、配置与manifest包含该头 |
| 代码生成 | UHT读取声明，产生generated.h及相关实现 | 当前生成命令、输出路径/时间与hash |
| C++消费 | 原cpp和生成cpp进入编译 | 编译日志/response输入，不能只看文件存在 |
| 链接与加载 | TwinArena消费ProbeMath的Next | 真实模块路径、导出/符号与加载身份 |
| 反射执行 | FindFunction后ProcessEvent | 当前进程返回Value=8及匹配符号 |

UBT组织UHT，再调用平台C++编译器；UHT不是C++编译器，也不替代链接器。按实际构建日志定位产物，不把某个旧版本Intermediate路径写死成通用契约。[Epic UHT](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-header-tool-for-unreal-engine)

## 2. 完整自有声明与消费者

原工程模块假定名为TwinArena；实际不同就同步改目录、Build.cs类名、脚本模块名和导出宏。以下文件维护在`examples/unreal/ucpp-04/Source`。先备份/保存原工程输入，再合并；不把示例当完整uproject覆盖已有配置。

`TwinArena/Public/WiringToken.h`：
```cpp
#pragma once
#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "WiringToken.generated.h"

UCLASS()
class TWINARENA_API UWiringToken : public UObject
{
    GENERATED_BODY()
public:
    UPROPERTY()
    int32 Value = 1;

    UFUNCTION()
    void Bump();
};

TWINARENA_API void RunWiringProbe();
```

`TwinArena/Private/WiringToken.cpp`：
```cpp
#include "WiringToken.h"
#include "ProbeMath.h"
#include "UObject/StrongObjectPtr.h"

void UWiringToken::Bump() { Value = ProbeMath::Next(Value); }

void RunWiringProbe()
{
    check(IsInGameThread());
    TStrongObjectPtr<UWiringToken> Token(NewObject<UWiringToken>());
    UFunction* Function = Token->FindFunction(FName(TEXT("Bump")));
    if (!Function)
    {
        UE_LOG(LogTemp, Error, TEXT("UCPP4 missing reflected Bump"));
        return;
    }
    Token->ProcessEvent(Function, nullptr); // no parameters or return storage
    if (Token->Value != 8)
    {
        UE_LOG(LogTemp, Error, TEXT("UCPP4 bad result Value=%d"), Token->Value);
        return;
    }
    UE_LOG(LogTemp, Display, TEXT("UCPP4 reflected Bump reached imported Next Value=%d"), Token->Value);
}
```

generated.h放在最后一个include位置，它由工具生成，不能手写一个同名空文件“修好编译”。UFUNCTION只有无参void Bump，因而本例ProcessEvent没有参数缓冲布局需要猜。先检查FindFunction，随后真正执行，最后核对普通字段值；单看StaticClass存在或日志里出现类型名不能证明Bump执行过。

在现有旋转Actor.cpp包含`WiringToken.h`，从BeginPlay调用`RunWiringProbe()`。TStrongObjectPtr负责调用区间内的显式保活；函数返回解除持有，不宣称立刻GC。

## 3. 一个小模块把头可见性与导出分开

`ProbeMath/ProbeMath.Build.cs`：
```csharp
using UnrealBuildTool;
public class ProbeMath : ModuleRules
{
    public ProbeMath(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new string[] { "Core" });
    }
}
```

`ProbeMath/Public/ProbeMath.h`：
```cpp
#pragma once
#include "CoreTypes.h"
namespace ProbeMath
{
    PROBEMATH_API int32 Next(int32 Value);
}
```

`ProbeMath/Private/ProbeMath.cpp`：
```cpp
#include "ProbeMath.h"
#include "Modules/ModuleManager.h"
int32 ProbeMath::Next(int32 Value) { return Value + 7; }
IMPLEMENT_MODULE(FDefaultModuleImpl, ProbeMath)
```

原TwinArena.Build.cs保留已有配置，确认公开依赖Core、CoreUObject、Engine，并在私有依赖加入ProbeMath：

```csharp
PrivateDependencyModuleNames.Add("ProbeMath");
```

ProbeMath只从TwinArena的Private cpp被包含，因此无需把其依赖传播到所有TwinArena消费者。若后来在TwinArena公开头里暴露ProbeMath类型，必须重新判断公开依赖，不能照抄私有配置。[Epic Modules](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-modules)

原uproject的Modules数组追加以下**单个对象**，保持其他模块不变：

```json
{"Name":"ProbeMath","Type":"Runtime","LoadingPhase":"Default"}
```

| 机制 | 本例责任 | 不能代替什么 |
|---|---|---|
| include | 让当前翻译单元见到声明 | 不添加链接依赖 |
| Public/Private头位置 | 向模块外提供或隐藏头接口 | 不等于C++访问修饰符，也不自动导出函数 |
| Build.cs依赖 | 构建/链接关系与需要的编译环境传播 | 不保证头里每个函数都有定义 |
| PROBEMATH_API | 按构建形式导出/导入Next | 不决定模块何时加载、不创建反射函数 |
| Modules/加载阶段 | 运行模块组织 | 不修复编译或缺符号 |

在桌面modular DLL构建中，API宏可展开为导出/导入修饰；monolithic构建可能展开为空。不能在monolithic里删宏后仍成功，就说跨模块导出不需要。[Epic Module API Specifiers](https://dev.epicgames.com/documentation/en-us/unreal-engine/module-api-specifiers-in-unreal-engine)

## 4. 从真实生成物追到反射执行

本机尚无可执行工程，下面是取得环境后的操作，不是已执行回执。

1. 固定UE patch/Build.version与源码revision、TwinArena输入快照、工具链、Win64 Development Editor目标。用实际Build.bat/UBT绝对路径执行[工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})的正常构建命令，保存原始日志和exit code。
2. 从这次日志找到UHT manifest和真实Intermediate生成目录，按WiringToken搜索产物。只记录路径、声明身份、hash、生产/消费关系；不公开复制引擎生成实现。
3. 确认当前目标确实编译消费该生成文件，记录TwinArena与ProbeMath实际DLL/PDB、hash和符号匹配。旧文件时间或IDE能导航不算这一步完成。
4. 启动同次构建，放入或已有Actor执行RunWiringProbe。记录FindFunction非空、ProcessEvent进入自有Bump、跨模块Next、最终Value=8。没有匹配PDB时日志只证明结果，不额外声称精确原生断点路径。
5. 全新验证使用独立输入副本的未构建目录；增量验证只改自有Next的加数并恢复，核对实际参与阶段及结果。不要清掉已有工程缓存来破坏历史证据，也不强求每次增量都重跑UHT。

## 5. 一次只破坏一段接线

| 独立故障 | 预计失败阶段 | 恢复及必须核对 |
|---|---|---|
| 仅移除WiringToken.generated.h include | UHT处理声明 | 恢复原行；核对UHT实际诊断而非伪造文字 |
| 仅将ProbeMath/Public/ProbeMath.h移到ProbeMath/Private/ProbeMath.h | TwinArena消费者的C++头查找 | 恢复位置；不靠另加私人include路径绕过接口 |
| 仅去掉Next声明的PROBEMATH_API | 固定Windows modular配置的跨DLL链接 | 恢复宏；若目标monolithic则该实验不成立，不能登记“预期链接失败已验” |

每次从正常基线开始，保留故障输入差异、命令、退出码、第一处诊断和恢复后的构建/运行结果。当前六个阶段与三种故障均未执行；文档和自有代码静态检查完成，工程结论保持待验。

**先追谁生成、谁消费、谁链接，再用当前进程的真实反射调用确认接线生效。**
