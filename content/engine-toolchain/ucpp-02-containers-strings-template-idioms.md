---
title: "UCPP-2｜容器、字符串与模板惯用法的阅读"
slug: "ucpp-02-containers-strings-template-idioms"
date: "2026-10-25"
description: "把 CXX-04、CXX-07 的值、借用、失效与转发规则落到 TArray/TMap、FString/FName/FText 和 MoveTemp/Forward，区分 UE 自身契约与标准库经验。"
tags: ["Unreal", "C++", "TArray", "TMap", "FString", "模板"]
series: "UE 全栈工程师成长路线"
series_order: 3.7
weight: 4606
---

> **读这篇之前**：先读 [CXX-04 移动与容器访问]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}})、[CXX-07 模板阅读]({{< relref "engine-toolchain/cpp-foundations-07-template-reading.md" >}}) 和 [UCPP-1 对象与持有边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})。编译步骤沿用 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})，样例从已有 [旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}}) 的 BeginPlay 调用。

> **版本与证据**：目标沿用 UE 5.8.2；2026-10-25 实际核对的 Epic 指南/API 显示 **5.8 文档**。本机未具备登记为可用的 UE 源码与构建入口。下面代码是完整自有教学样例，尚未经过 UE 编译或运行；正常结果与编译失败均为待执行的验证预期。

一张角色列表删掉了第一项，保存的下标仍在合法范围内，却已经指向另一个角色；一次名字查询返回了地址，随后添加其他条目，这个地址也未必还能继续使用。日志里相同的文字，还可能分别承担数据、标识或界面显示三种职责。

这些问题不能靠记住“UE 有一套自己的容器”解决。本篇沿用公共课的模型，再核对引擎增加的契约：**保存了谁，修改影响哪条访问，文本表达什么，模板实际选择了什么。**

## 1. 从一个成员声明读出四层关系

| 看到的类型 | 容器直接拥有的元素 | 还需要另外判断的对象 |
|---|---|---|
| `TArray<FRow>` | 每个 FRow 值 | 字段若保存借用或间接引用，目标寿命仍有自己的协议 |
| `TArray<TUniquePtr<FRow>>` | 每个独占包装 | 另行分配的 FRow，不与包装元素地址混为一谈 |
| `TArray<TObjectPtr<UItem>>` | 每个对象指针包装 | UObject 的 GC 可达性与游戏有效性，继续按 UCPP-1 判断 |
| `TMap<FName, FRow>` | 每个键值对 | 查询返回的是表内借用还是值副本，修改后如何重查 |

复制容器时，要看元素的复制语义；复制一个指针包装不等于深复制它指向的业务对象，元素不支持复制时，相应容器复制操作也可能不成立。遍历中的 `auto Row`、`auto& Row` 和 `const auto& Row` 也分别意味着值副本、可修改借用和只读借用。语法回看 CXX-01/04，这里重点检查具体容器契约。

如果元素是 UObject 引用，容器还要通过受支持的反射/引用收集路径参与保活。普通局部 TArray 不能仅因装着 TObjectPtr 就成为 GC 根。本篇的演示只使用普通原生值，不以容器示例新增 UObject 保活或线程安全结论。

## 2. TArray：有顺序，不代表位置就是身份

TArray 管理一段有序元素，具有值语义。`Num()` 描述现有元素数量，`Max()` 描述当前分配可容纳的数量；容量不是已经构造好的业务对象数量。Reserve 预留容量，不会把 Num 改成同样的数。[TArray 指南][array]、[TArray API][array-api]

### 修改以后，先决定哪些访问需要重新取得

下面是阅读时的检查表，不把 std::vector 的完整失效表直接移植过来：

| 修改 | 应当重新检查什么 |
|---|---|
| Add/Emplace/Insert 等可能扩容的操作 | 先前的元素指针、引用和迭代状态是否仍成立；需要跨修改保存时，优先保存业务键并重查 |
| RemoveAt | 后续元素会前移，旧下标可能仍合法却换了业务对象；删除还可能影响存储 |
| RemoveAtSwap 等交换删除 | 不要求保留原顺序，不能继续依赖原来的位置映射 |
| 排序或其他重排 | 数量未变，也不代表下标或元素位置仍对应原身份 |
| Reset/Empty | 原有元素已经结束；即使保留部分容量，也不能继续访问旧元素 |

Reserve 只处理分配需求，不保护删除、排序或业务停用。`IsValidIndex(Index)` 只能证明这一刻下标落在范围内，不能证明 `Rows[Index].Id` 仍然是之前的 Id。需要稳定身份，就保存键/句柄并核对重新查找的结果。[TArray API][array-api]

### Add、Emplace 与可重定位，是不同层的问题

Add 接收元素，Emplace 把参数传给元素构造；它们怎样选中重载，仍按 CXX-04/07 阅读。Emplace 不自动保活引用，不保证不扩容，也不能仅凭名字宣称任意场景更快。

还有一个不能从 std::vector 经验直接照搬的前提：Epic 的 TArray API 明确说明了元素的 **trivially relocatable（可直接重定位）假设**。不要把每次存储搬迁都画成逐个调用用户移动构造。含自指针或以自身地址登记外部状态的自定义类型，需要额外核对是否满足容器的重定位契约；只写一个移动构造并不能证明所有搬迁路径安全。[TArray API][array-api]

遇到 AddUninitialized/SetNumUninitialized 等低层接口时，还要重新区分存储与已构造元素。它们不是“省掉不重要的初始化”，相关前提先回看 [CXX-09 存储与对象]({{< relref "engine-toolchain/cpp-foundations-09-storage-object-lifetime-ub.md" >}})。本篇不使用这些接口。

## 3. TMap：先分清查询、默认值与插入

TMap 使用唯一键管理键值对；对已有键再次 Add 会替换对应条目，不是保留第二个同名项。遍历顺序不应当作插入顺序或长期稳定的排序结果。业务若需要明确输出顺序，应另建并维护排序规则。[TMap 指南][map]

| 写法 | 结果 | 缺失时的语义 |
|---|---|---|
| Find(Key) | 指向当前表内值的指针；const 表返回只读指针 | 返回空，不插入 |
| FindRef(Key) | 值副本，名字中的 Ref 不代表返回引用 | 返回默认构造值，不插入；不能单靠结果区分“缺失”和“存在但恰好默认值” |
| FindOrAdd(Key) | 表内值的引用 | 建立缺失条目并返回其值，是修改操作 |
| operator[] | 访问已有键对应的值 | 必须满足键存在的前提，不按 std::map 的缺失插入方式使用 |

Find 得到的地址是容器内部的借用。官方指南特别提醒，FindOrAdd 新增条目可能扩展后端存储，使先前取得的指针/引用失效。因此不能套用 CXX-04 中 std::unordered_map 的 rehash 引用稳定规则。保存键，完成修改后重新 Find；要保留快照，就在有效时复制所需值。[TMap 查询与失效说明][map]

修改时也不要边 ranged-for 遍历边随意 Add/Remove。同一操作需要筛选并删除时，可以先收集要删除的键，结束遍历后再执行；如采用容器迭代器提供的删除接口，则按该接口自身契约推进。

自定义键还需要相容的相等与散列规则：相等的键应给出相同散列。不要在键已入表后随意修改决定相等/散列的内容。FName 已有适用规则，但它的名字相等条件也必须符合业务需求。

## 4. FString、FName、FText 分别保存哪一种意义

| 类型 | 本篇用它表达什么 | 不要顺手推导什么 |
|---|---|---|
| FString | 需要保存、拼接、解析的字符数据，例如 RunId 和调试描述 | 保存了一份 FString，不等于另外取得的字符地址永久有效 |
| FName | 引擎名字与经常比较的标识，例如属性名、骨骼名、表内键 | 不是任意大小写敏感外部 ID 的无损替代，也不是通用可编辑字符缓冲 |
| FText | 面向用户、需要保留本地化语义的显示文本 | 转成 FString 再转回去，不会自动恢复原来的本地化身份与历史 |

这也解释了旧文的取材：旋转 Actor 的 RunId 是 FString；反射篇按 FName 查属性/函数；对象示例中的字段若改作界面名称，则要另行考虑 FText，而不是只按“看起来都是字符串”选类型。[FString][string]、[FName][name]、[FText][text]

### 字符地址是借用，不是新的拥有者

`UE_LOG(..., TEXT("%s"), *Description)` 中，FString 的 `*` 提供相应字符数据供这次同步调用使用。若另存 `const TCHAR*`，就要保证原字符串仍存在，且期间没有使其内部字符存储改变的操作。不要把 `*FString::Printf(...)` 的结果保存下来供后续异步使用；临时 FString 会在完整表达式结束后销毁。[FString 指南][string]

TEXT 处理字符字面量类型，不赋予本地化身份；也不要把 TCHAR 缓冲强行转成 char* 就当成 UTF-8。长度、编码转换和文本边界需要相应接口，本篇不假设固定 TCHAR 宽度或把字符单元数当作人眼字数。

### 名字相等与显示文字相同，是两套判断

FName 的常规相等比较不区分大小写。`Player` 和 `player` 用作 FName 键时，不能当成两个不同业务标识；因此示例会验证后一次 Add 替换同一键。不要依赖它的显示大小写或内部表索引作跨进程永久 ID。[FName 指南][name]

FText 的文本身份/历史用于本地化重建，不只是当前显示字符。样例以 NSLOCTEXT 提供命名空间、键和源文本，再用 FText::Format 插入数量；把结果 ToString 只是为了本次日志。真正的本地化还需要文本收集、翻译与打包，写一个宏不等于多语言已经可用。[Text Localization][localization]

## 5. MoveTemp 与 Forward：先读转换，再读消费者

CXX-04 已说明 std::move 不负责自己搬运对象；在 UE 中也应先问：这次表达式是否只是转换成右值引用，后面哪个构造/赋值/重载才真的消费它？

| 写法 | 要表达的意图 | 使用边界 |
|---|---|---|
| MoveTemp(Value) | 当前这层明确允许后续消费者从这个非 const 左值取走状态 | 当前 API 会拒绝 const 对象与已经是右值的实参；不能机械等同于 std::move 的所有可编译情形 |
| Forward<T>(Value) | 包装函数按本次推导的 T 继续传递值类别 | 需要正确的 T 与转发引用上下文；不会自动保活、延寿或检查业务消费次数 |
| Args&&... 与 Forward<Args>(ArgsValue)... | 逐个保留参数类别交给下一层 | 仍须看接收方如何存储参数，不能默认保存了独立副本 |

MoveTemp 和 Forward 的当前入口都在 Templates/UnrealTemplate.h。MoveTemp 的额外诊断是引擎约定；公共课里“对 const 使用 std::move 可能仍选中复制”的例子，不能照抄成“UE MoveTemp(constValue) 也会照样复制”。[MoveTemp API][move]、[Forward API][forward]

命名的右值引用参数仍是左值表达式。模板 `Relay(T&& Value)` 里直接 `Select(Value)` 会丢失调用方右值类别；满足转发引用条件时用 `Forward<T>(Value)`。但类模板中已经固定的 T、`const T&&` 或人为固定模板实参，并不会仅因写了 `&&` 就获得同样的推导行为，详细规则继续回看 CXX-07。

移动后能依赖什么，由被消费类型的契约决定，不把“已移动对象必定为空”推广到所有类型。也不要给每个返回语句加 MoveTemp，或者把一份参数反复 Forward 给多个会消费它的调用；复制消除、源状态与消费次数都要单独判断。

### 模板参数里不只有业务元素类型

读 `TArray<FRow, Allocator>` 时，Allocator 也是类型的一部分。一个只接默认 `TArray<FRow>` 的引用参数，不会自动接受所有 allocator 变体。需要保留这种泛化时，可以让函数推导 Allocator；样例里的 CountRows 就只借用相应数组，并用 decltype(Rows.Num()) 保留实际数量类型，不把所有变体都硬编码为 int32。具体分配策略和性能仍按目标类型与版本检查。

## 6. 两个自有文件，把这些问题接到同一次阅读练习

把下面文件放进现有 TwinArena 模块。它们是普通 C++ 声明/定义，没有新的 UCLASS，不需要 generated.h；模块仍沿用已有 Core/CoreUObject/Engine 依赖。模板辅助定义放在这个 .cpp 内，因为调用也全部在同一翻译单元。

### Source/TwinArena/Public/UcppContainerReading.h

```cpp
#pragma once

#include "CoreMinimal.h"

TWINARENA_API void RunUcppContainerReading();
```

### Source/TwinArena/Private/UcppContainerReading.cpp

```cpp
#include "UcppContainerReading.h"

#include "Containers/Array.h"
#include "Containers/Map.h"
#include "Internationalization/Text.h"
#include "Templates/UnrealTemplate.h"

DEFINE_LOG_CATEGORY_STATIC(LogUcppContainers, Log, All);

// Normal case: 0. Set to 1 or 2 for a separate expected compile-failure build.
#define UCPP2_FAILURE_CASE 0

namespace
{
    struct FReadingRow
    {
        FReadingRow(FName InId, int32 InScore) : Id(InId), Score(InScore) {}
        FName Id;
        int32 Score;
    };

    template <typename Allocator>
    auto CountRows(const TArray<FReadingRow, Allocator>& Rows) -> decltype(Rows.Num())
    {
        return Rows.Num();
    }

    template <typename... Args>
    void EmplaceRow(TArray<FReadingRow>& Rows, Args&&... Values)
    {
        Rows.Emplace(Forward<Args>(Values)...);
    }

    enum class EInputPath : uint8 { MutableLvalue, ConstLvalue, Rvalue };

    EInputPath Select(FString&) { return EInputPath::MutableLvalue; }
    EInputPath Select(const FString&) { return EInputPath::ConstLvalue; }
    EInputPath Select(FString&&) { return EInputPath::Rvalue; }

    template <typename T>
    EInputPath Relay(T&& Value)
    {
        return Select(Forward<T>(Value));
    }
}

void RunUcppContainerReading()
{
    check(IsInGameThread());

    TArray<FReadingRow> Rows;
    Rows.Reserve(3);
    check(Rows.Num() == 0);
    EmplaceRow(Rows, FName(TEXT("Alpha")), 10);
    EmplaceRow(Rows, FName(TEXT("Beta")), 20);
    EmplaceRow(Rows, FName(TEXT("Gamma")), 30);

    const int32 SavedIndex = 1;
    const FName Wanted = Rows[SavedIndex].Id;
    auto Snapshot = Rows;
    Rows[SavedIndex].Score = 25;
    check(Snapshot[SavedIndex].Score == 20);

    Rows.RemoveAt(0);
    check(Rows.IsValidIndex(SavedIndex));
    check(Rows[SavedIndex].Id == FName(TEXT("Gamma")));
    const int32 ReboundIndex = Rows.IndexOfByPredicate(
        [Wanted](const FReadingRow& Row) { return Row.Id == Wanted; });
    check(ReboundIndex != INDEX_NONE);
    check(Rows[ReboundIndex].Score == 25);
    check(CountRows(Rows) == 2);

    TMap<FName, int32> Scores;
    const FName PlayerKey(TEXT("Player"));
    Scores.Add(PlayerKey, 10);
    Scores.Add(FName(TEXT("player")), 20);
    check(Scores.Num() == 1);
    int32 ValueSnapshot = 0;
    {
        const int32* Found = Scores.Find(PlayerKey);
        check(Found);
        ValueSnapshot = *Found;
    } // No element pointer is retained across the following mutation.

    const FName Missing(TEXT("Missing"));
    check(Scores.Find(Missing) == nullptr);
    const int32 DefaultValue = Scores.FindRef(Missing);
    check(DefaultValue == 0 && Scores.Num() == 1);
    Scores.FindOrAdd(Missing) = 0;
    Scores.Add(PlayerKey, 25);
    const int32* Rebound = Scores.Find(PlayerKey);
    check(Rebound && *Rebound == 25 && ValueSnapshot == 20);
    Rebound = nullptr;

    TArray<FName> KeysToRemove;
    for (const auto& Pair : Scores)
    {
        if (Pair.Value == 0)
        {
            KeysToRemove.Add(Pair.Key);
        }
    }
    for (const FName Key : KeysToRemove)
    {
        Scores.Remove(Key);
    }
    check(!Scores.Contains(Missing));

    FString Mutable(TEXT("payload"));
    const FString ReadOnly(TEXT("readonly"));
    check(Relay(Mutable) == EInputPath::MutableLvalue);
    check(Relay(ReadOnly) == EInputPath::ConstLvalue);
    check(Relay(FString(TEXT("temporary"))) == EInputPath::Rvalue);
    check(Select(MoveTemp(Mutable)) == EInputPath::Rvalue);
    check(Mutable == TEXT("payload")); // Select only binds a reference.

    FString Destination(MoveTemp(Mutable));
    check(Destination == TEXT("payload"));
    Mutable = TEXT("reused"); // Do not assume its pre-assignment contents.
    UE_LOG(LogUcppContainers, Display, TEXT("UCPP2 string=%s"), *Destination);

    const FText Display = FText::Format(
        NSLOCTEXT("UCPP2", "Remaining", "Remaining: {0}"),
        FText::AsNumber(CountRows(Rows)));
    UE_LOG(LogUcppContainers, Display, TEXT("UCPP2 display=%s"), *Display.ToString());

#if UCPP2_FAILURE_CASE == 1
    FString Rejected = MoveTemp(ReadOnly);
    UE_LOG(LogUcppContainers, Display, TEXT("%s"), *Rejected);
#elif UCPP2_FAILURE_CASE == 2
    FString Rejected = MoveTemp(FString(TEXT("already-rvalue")));
    UE_LOG(LogUcppContainers, Display, TEXT("%s"), *Rejected);
#endif

    UE_LOG(LogUcppContainers, Display,
        TEXT("UCPP2 normal case reached end: rows=%d scores=%d"),
        CountRows(Rows), Scores.Num());
}

#undef UCPP2_FAILURE_CASE
```

这里的 FString 重载只返回一个枚举，没有真的取走字符缓冲；因此 MoveTemp 后第一条检查仍能看到原值。真正的消费发生在后面的 Destination 构造中。样例没有打印或断言移动后的旧内容，更没有保存失效地址来观察“是否碰巧还能用”。

## 7. 怎样验证，而不把预期写成结果

在旋转 Actor 的 .cpp 加入 `#include "UcppContainerReading.h"`，然后在其 BeginPlay 的 `Super::BeginPlay()` 之后调用一次 `RunUcppContainerReading();`。只放一个测试实例，关闭 Live Coding 后正常构建，沿用同一张测试地图和已有构建身份记录。TWINARENA_API 随实际模块名调整，不覆盖 UCPP-1 的探针文件。

| 构建/观察项 | 预期与验收问题 | 本轮结果 |
|---|---|---|
| FAILURE_CASE=0 的 Development 构建与运行 | 编译通过并执行检查；最终 rows=2、scores=1；这只是本例有限断言，不是所有容器场景正确的证明 | 未运行 |
| 删除后的身份 | 保存下标仍合法但对应 Gamma；按 Beta 键重查得到 Score=25，副本仍保留20 | 未运行 |
| TMap 查询/修改 | Player/player 合并为同键；FindRef 缺失不插入，FindOrAdd 才新增；修改后重查结果 | 未运行 |
| 转发与消费 | 三种 Relay 调用选择对应类别；Select 的右值引用绑定未消费字符，Destination 才执行消费操作 | 未运行 |
| FAILURE_CASE=1 | 根据 MoveTemp 当前 API，const 实参应被编译拒绝；保存实际诊断，不预先写死报错行 | 未编译 |
| FAILURE_CASE=2 | 已是右值的实参应被拒绝；与第一种分开构建，验证后恢复0 | 未编译 |
| FText 显示 | 记录当前文化与格式结果，不把英语源文本、数字样式写成跨文化固定输出 | 未运行 |

check 在本例用于 Development 验证，不把必要的修改操作藏在 check 表达式中。Shipping 的检查配置和实际行为另行处理；日志“到达末尾”也不代替核对配置、检查是否启用和真正的运行身份。

本篇没有测试容器扩容次数、地址保持率、分配器变体、FName 内部布局、文本收集/打包、文化切换或性能。不在没有引擎环境时用 std::vector/std::unordered_map 替身的通过结果证明 UE 行为。

## 8. 回到现有文章，继续追职责与调用

- [旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})：RunId 的拥有与 UE_LOG 的字符借用；不要把临时字符地址带到延迟调用中。
- [对象系统]({{< relref "system-design/ue-01-uobject-system.md" >}})：Inventory 的数组元素与所指 UObject 分别判断；复制容器不会复制整套对象世界。
- [反射与序列化]({{< relref "system-design/ue-02-reflection-serialization.md" >}})：按 FName 找属性/函数时，先问名字的比较语义，再问对象、属性类型和调用资格；查到名字不等于可以无条件访问。
- 公共规则继续回 [CXX-04]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}}) 与 [CXX-07]({{< relref "engine-toolchain/cpp-foundations-07-template-reading.md" >}})；UObject 生命周期继续回 UCPP-1。

下一项 UCPP-3“Delegate 与 Lambda：绑定、捕获、解绑和延迟调用”仍待写。届时把这里的元素借用、字符地址和转发结果放进回调保存/取消协议；状态见 [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}})。

## 9. 本篇的证据边界

本轮完成官方文档核对与自有样例静态审阅，分别登记容器有效性、字符串语义、MoveTemp/Forward 约定和失败预期。UE 本地源码仍未登记为可用，没有新增源码级验证、UHT/UBT、实际编译、PIE 或独立包结果，也不改变 M0/个人能力验收。

<!-- DATA-TODO: 在实际 UE 5.8.2 或明确替代版本，将两文件样例接入 TwinArena 的单实例 BeginPlay；记录 build_id/run_id、检查配置、正常运行断言/日志、两个独立编译失败与恢复、实际文化。需要本地化结论时另补收集/翻译/打包与文化切换，不用源文本日志代替。 -->

**先确认拥有与借用，再核对修改后的访问和业务身份；引擎类型与模板写法都要回到各自契约。**

[array]: https://dev.epicgames.com/documentation/unreal-engine/array-containers-in-unreal-engine?lang=en-US
[array-api]: https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Core/TArray?lang=en-US
[map]: https://dev.epicgames.com/documentation/en-us/unreal-engine/map-containers-in-unreal-engine
[string]: https://dev.epicgames.com/documentation/unreal-engine/fstring-in-unreal-engine?lang=en-US
[name]: https://dev.epicgames.com/documentation/en-us/unreal-engine/fname-in-unreal-engine
[text]: https://dev.epicgames.com/documentation/en-us/unreal-engine/ftext-in-unreal-engine
[localization]: https://dev.epicgames.com/documentation/en-us/unreal-engine/text-localization-in-unreal-engine
[move]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/MoveTemp
[forward]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/Forward

接下来沿[UCPP-3 Delegate与Lambda]({{< relref "engine-toolchain/ucpp-03-delegates-lambda-callback-lifetime.md" >}})读容器保存的行为：handle指向订阅条目，已复制闭包有独立寿命，删除条目不自动取消所有任务。
