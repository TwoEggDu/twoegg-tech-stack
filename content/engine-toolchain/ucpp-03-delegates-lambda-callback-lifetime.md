---
title: "UCPP-3｜Delegate 与 Lambda：绑定、捕获、解绑和延迟调用"
slug: "ucpp-03-delegates-lambda-callback-lifetime"
date: "2026-10-25"
description: "用同一订阅与自有延迟队列区分弱观察、业务关闭、解绑和完成，保留UE运行待验证边界。"
tags: ["Unreal", "C++", "源码阅读"]
series: "UE 全栈工程师成长路线"
series_order: 3.8
weight: 4607
---

> **前置**：[CXX-02拥有与结束]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})、[CXX-05回调与捕获]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})，以及[UCPP-1对象]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})与[UCPP-2容器]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})。

> **版本与证据**：目标UE5.8.2；2026-10-25核对Epic5.8文档。完整自有文件已写并做静态责任检查；本机没有已登记TwinArena位置和UE构建入口，以下预期尚未经过UHT、UE编译或运行。

界面已经关闭，事件源也移除了订阅，但一个提前保存到队列的闭包仍然可能被调用。把“解绑成功”当成“此后任何地方都不会调用接收者”，会漏掉一整段在途工作。

本篇沿一个订阅追五件事：谁保存行为、捕获是否拥有目标、谁允许派发、谁允许更新业务，以及由谁确认任务已经结束。

## 1. 订阅表与任务队列是两份状态

```text
事件源 → 订阅条目(handle) → 对接收者的访问
队列   → 已保存的闭包副本 → 对接收者的另一条访问
```

从事件源移除一个handle，只改变第一条。要让第二条在接收者业务结束后安全返回，需要任务自己的弱观察、业务状态或代次检查；要销毁任务使用的基础设施，还需要等待/排空。单个原子取消标志也不自动产生完成确认。

下面只在Game Thread上绑定、广播、清理和执行教学队列。没有数据竞争不等于已经设计好了跨线程取消。

## 2. 先选Delegate的职责，再看绑定形式

单播保存一个目标，执行前按签名选择Execute/ExecuteIfBound；多播保存多个订阅，用Broadcast，不应依赖订阅的执行顺序。动态委托接入UObject反射和可序列化描述，绑定入口及允许的签名需另查；不是把普通Lambda原样塞进AddDynamic。本例使用原生多播，不扩大成蓝图事件架构。[Epic Delegates](https://dev.epicgames.com/documentation/en-us/unreal-engine/delegates-and-lambda-functions-in-unreal-engine)

| 绑定 | 访问责任 | 结束条件 |
|---|---|---|
| AddRaw | 裸访问；不延长普通对象寿命 | 源停止使用或提前Remove，再结束目标 |
| AddSP | 对共享拥有对象作弱观察 | 最后强持有结束后不可调用；业务停用仍另判断 |
| AddUObject | 对UObject作引擎弱观察 | 不代替UCPP-1的保活/EndPlay协议 |
| AddLambda | 保存闭包；捕获决定实际持有关系 | 复制捕获的资源要追到闭包副本结束 |
| AddWeakLambda / AddSPLambda | 绑定目标的弱条件之外仍保存闭包 | 额外捕获可能自己强持有别的对象；不可一概称全弱 |

这些绑定入口及Remove按5.8多播文档核对；弱绑定不会自动检查你的请求编号、界面是否关闭或业务代次。[Epic Multicast Delegates](https://dev.epicgames.com/documentation/en-us/unreal-engine/multicast-delegates-in-unreal-engine)

## 3. 完整自有样例：先停业务，再移除订阅

将以下两个文件接入既有TwinArena的Source/TwinArena；实际模块名不同就同步替换TWINARENA_API。模块依赖Core、CoreUObject、Engine，接入步骤沿[工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})。场景中放一个AUcppDelegateProbe。没有工程路径时先保留文件，不伪造创建/构建回执。

`UcppDelegateProbe.h`：
```cpp
#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "UcppDelegateProbe.generated.h"

DECLARE_MULTICAST_DELEGATE_OneParam(FUcppValueEvent, int32);

UCLASS()
class TWINARENA_API AUcppDelegateProbe : public AActor
{
    GENERATED_BODY()
public:
    int32 Calls = 0;
    void Stop();
protected:
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
private:
    FUcppValueEvent Event;
    FDelegateHandle Subscription;
    bool bAccepting = true;
    void OnValue(int32 Value);
};
```

`UcppDelegateProbe.cpp`：
```cpp
#include "UcppDelegateProbe.h"
#include "Templates/SharedPointer.h"

namespace
{
class FSubscriber : public TSharedFromThis<FSubscriber>
{
public:
    explicit FSubscriber(FUcppValueEvent& InEvent) : Event(InEvent) {}
    ~FSubscriber() { Close(); }
    void Open()
    {
        check(IsInGameThread());
        if (!bAccepting || Subscription.IsValid()) return;
        Subscription = Event.AddSP(AsShared(), &FSubscriber::OnValue);
    }
    void Close()
    {
        check(IsInGameThread());
        bAccepting = false;
        if (Subscription.IsValid())
        {
            Event.Remove(Subscription);
            Subscription.Reset();
        }
    }
    void OnValue(int32 Value)
    {
        check(IsInGameThread());
        if (bAccepting) Total += Value;
    }
    int32 Total = 0;
private:
    FUcppValueEvent& Event; // caller guarantees source outlives subscriber
    FDelegateHandle Subscription;
    bool bAccepting = true;
};

void RunNativeCases()
{
    check(IsInGameThread());
    FUcppValueEvent Event;
    TArray<TFunction<void()>> Queue;
    TSharedPtr<FSubscriber> Receiver = MakeShared<FSubscriber>(Event);
    Receiver->Open();
    Receiver->Open(); // no duplicate subscription
    Event.Broadcast(2);
    ensureAlwaysMsgf(Receiver->Total == 2, TEXT("normal broadcast"));

    // This saved invocation is separate from Event's subscription list.
    TWeakPtr<FSubscriber> Weak = Receiver;
    Queue.Add([Weak]() { if (auto Pinned = Weak.Pin()) Pinned->OnValue(20); });
    Receiver->Close();
    Receiver->Close();
    Event.Broadcast(3); // removed: cannot dispatch a new subscriber call here
    Queue[0](); // still queued, but subscriber's business gate rejects it
    ensureAlwaysMsgf(Receiver->Total == 2, TEXT("closed receiver unchanged"));

    Receiver.Reset();
    Queue[0](); // cannot pin; no dangling call
    Queue.Reset();

    struct FRawReceiver { int32 Total = 0; void Add(int32 N) { Total += N; } };
    {
        FRawReceiver Raw;
        FDelegateHandle Handle = Event.AddRaw(&Raw, &FRawReceiver::Add);
        Event.Broadcast(4);
        ensureAlwaysMsgf(Raw.Total == 4, TEXT("raw inside valid scope"));
        Event.Remove(Handle); // before Raw leaves scope
    }
    Event.Broadcast(5); // no raw receiver is left in the list
    UE_LOG(LogTemp, Display, TEXT("UCPP3 native cases reached end"));
}
}

void AUcppDelegateProbe::BeginPlay()
{
    Super::BeginPlay();
    RunNativeCases();
    Subscription = Event.AddUObject(this, &AUcppDelegateProbe::OnValue);
    Event.Broadcast(1);
    Stop();
    Event.Broadcast(2);
    ensureAlwaysMsgf(Calls == 1, TEXT("UObject explicit stop"));
}

void AUcppDelegateProbe::OnValue(int32 Value)
{
    check(IsInGameThread());
    if (bAccepting) Calls += Value;
}

void AUcppDelegateProbe::Stop()
{
    check(IsInGameThread());
    bAccepting = false;
    if (Subscription.IsValid())
    {
        Event.Remove(Subscription);
        Subscription.Reset();
    }
}

void AUcppDelegateProbe::EndPlay(const EEndPlayReason::Type Reason)
{
    Stop();
    Stop();
    Super::EndPlay(Reason);
}
```

Open必须在MakeShared之后调用，因为AsShared要找到已经建立的共享控制关系；不在构造函数内自行制造shared拥有。FSubscriber只借用Event，调用者保证源先创建、后销毁。Close幂等：第一次设为业务关闭并移除条目，第二次没有可移除handle。这个示例不把“重复调用没崩”当通用线程安全。

## 4. 已复制任务为什么还要检查

教学Queue保存的是另一份TFunction。Remove不会遍历这个队列。Weak.Pin成功只证明临时强持有建立，随后OnValue仍检查bAccepting；Receiver关闭但尚未析构时，Pin可以成功，业务门必须拒绝20这次更新。Receiver.Reset之后再执行同一闭包，Pin失败，连成员调用都不发生。

Raw对照只在有效作用域内使用，先Remove再析构；绝不故意留下悬垂指针广播。UObject部分使用AddUObject并在Stop/EndPlay明确移除。即使对象尚未被GC，业务仍可先结束。

这里没有启动真正后台任务，所以没有“正在执行”的线程需等待。换成工作线程后，Close、读bAccepting、执行及销毁间还需要同步协议；单线程bool不能原样搬过去。[跨线程前先读公共交接模型]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}})，再接[UCPP-5任务交接]({{< relref "engine-toolchain/ucpp-05-task-thread-data-handoff.md" >}})的完整受控样例。

## 5. 怎样验收，哪些仍然未知

| 操作 | 待观察结果 | 防止哪种误判 |
|---|---|---|
| Open两次，再广播2 | Total=2 | 重复绑定不是正常广播次数 |
| 保存Queue，再Close两次 | 新广播不更新，Queue外壳仍可执行 | Remove不清理其他队列 |
| 关闭但仍有强持有 | Pin成功、Total不变 | 存活不等于可用 |
| Receiver.Reset后再次执行 | Pin失败、无悬垂访问 | 弱观察不保活 |
| 结束Actor | Stop可重复；Calls保持1 | UObject内存寿命与业务结束分开 |

以上均为待UE执行的预期；不是已测日志。接入后按A08记录引擎patch、source_revision、目标/配置、build_id/run_id、实际日志和断言配置。ensureAlways用于把失败写入本轮日志，不能只看函数末尾的Display就判通过；正常控制流也不依赖check产生副作用。尚未验收多线程竞争、在途任务等待、完整蓝图动态绑定与性能。

**绑定决定怎样找到接收者；解绑、业务关闭和任务完成分别结束不同的责任。**
