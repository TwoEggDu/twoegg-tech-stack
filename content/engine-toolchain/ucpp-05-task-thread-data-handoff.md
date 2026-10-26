---
title: "UCPP-5｜任务与线程：快照、完成和结果应用的边界"
slug: "ucpp-05-task-thread-data-handoff"
date: "2026-10-25"
description: "用有限纯数据任务串起GT快照、后台拥有、完成确认、弱目标、请求代次和退出排空。"
tags: ["Unreal", "C++", "源码阅读"]
series: "UE 全栈工程师成长路线"
series_order: 3.95
weight: 4609
---

> **前置**：[CXX-05捕获]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})、[CXX-11并发与完成]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}})、[UCPP-1对象]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})与[UCPP-3回调结束]({{< relref "engine-toolchain/ucpp-03-delegates-lambda-callback-lifetime.md" >}})。

> **版本与证据**：目标UE5.8.2；2026-10-25核对Epic5.8 FAsyncTask相关API。完整自有文件与静态所有权检查完成，UE编译、实际线程、PIE/独立包和退出观察均待验证。

后台计算正确返回，界面却显示了上一个请求的结果；退出地图时目标还没被GC，旧任务仍写回它。两种问题都不能只靠“给指针换成弱引用”解决。弱观察回答对象能否取得，业务代次回答这个结果还属于谁的当前请求，任务完成关系回答何时可以结束资源。

## 1. 一次交接要有两张图

```text
拥有关系：Pending → FAsyncTask → 输入TArray与输出Result
观察关系：Pending → TWeakObjectPtr → GT上的目标

完成关系：GT提交 → worker纯数据工作 → IsDone/EnsureCompletion
                                      → GT读结果 → 验目标/代次 → 应用
退出关系：停收新任务 → 等已有worker完成 → 丢弃/排空 → 释放拥有者
```

输入是取值快照，后台不访问原容器、UObject或控制Actor。结果在完成确认前由worker写，GT不抢读。弱目标和代次留在GT一侧；它们不必被后台解引用，更不能让后台替GT决定Actor当前业务状态。

本例用GT每帧收取完成结果，作为明确的回投位置。没有额外排队一个GT Lambda，所以停止时不会出现“GT等worker、worker又等GT执行回投”的依赖环。若改为异步回调排队，排队回调本身也需独立排空。

## 2. 选择一个能说清完成责任的任务

这里用FAsyncTask包住FNonAbandonableTask：提交后台工作；每帧检查IsDone；销毁前EnsureCompletion。API文档明确任务进行中不能直接销毁所有者。[Epic FAsyncTaskBase](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/FAsyncTaskBase)

这只是受控小例选择，不是要求所有UE项目用这一套API。UE任务系统、Async/Future及专用线程各有调度和等待条件。关键是把任务依赖和完成句柄留下，而不是记一个“取消了”的bool就扔掉工作。

## 3. 完整自有样例

源文件在`examples/unreal/ucpp-05/`。合入已有TwinArena模块，依赖Core、CoreUObject、Engine；实际模块名不同时同步导出宏。场景放一个AUcppHandoffProbe，测试目标由它创建。它不是可扩展游戏系统，只验证这次交接边界。

`UcppHandoffProbe.h`：
```cpp
#pragma once
#include "CoreMinimal.h"
#include "Async/AsyncWork.h"
#include "GameFramework/Actor.h"
#include "UcppHandoffProbe.generated.h"

class AUcppHandoffTarget;
struct FUcppResult
{
    int64 Sum = 0;
    uint32 ThreadId = 0;
    bool bOnGameThread = false;
};

class FUcppDataWork : public FNonAbandonableTask
{
    friend class FAsyncTask<FUcppDataWork>;
public:
    explicit FUcppDataWork(TArray<int32> InValues) : Values(MoveTemp(InValues)) {}
    FUcppResult Result;
    FORCEINLINE TStatId GetStatId() const
    {
        RETURN_QUICK_DECLARE_CYCLE_STAT(FUcppDataWork, STATGROUP_ThreadPoolAsyncTasks);
    }
private:
    TArray<int32> Values;
    void DoWork();
};

struct FUcppPending
{
    TWeakObjectPtr<AUcppHandoffTarget> Target; // only touched on Game Thread
    uint64 Generation = 0;
    TUniquePtr<FAsyncTask<FUcppDataWork>> Work;
};

UCLASS()
class TWINARENA_API AUcppHandoffTarget : public AActor
{
    GENERATED_BODY()
public:
    bool bAccepting = true;
    uint64 Generation = 0;
    int64 Applied = 0;
    void EndBusiness();
protected:
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
};

UCLASS()
class TWINARENA_API AUcppHandoffProbe : public AActor
{
    GENERATED_BODY()
public:
    AUcppHandoffProbe();
    bool Request(AUcppHandoffTarget* Target, TArray<int32> Snapshot);
    void StopAndDrain();
    UPROPERTY(EditAnywhere, Category="UCPP")
    bool bStopImmediately = false;
protected:
    virtual void BeginPlay() override;
    virtual void Tick(float DeltaSeconds) override;
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
private:
    bool bAccepting = true;
    bool bReported = false;
    TArray<TUniquePtr<FUcppPending>> Pending;
    UPROPERTY() TObjectPtr<AUcppHandoffTarget> Normal;
    UPROPERTY() TObjectPtr<AUcppHandoffTarget> Stale;
    UPROPERTY() TObjectPtr<AUcppHandoffTarget> Ended;
};
```

`UcppHandoffProbe.cpp`：
```cpp
#include "UcppHandoffProbe.h"
#include "Engine/World.h"
#include "HAL/PlatformTLS.h"

void FUcppDataWork::DoWork()
{
    Result.ThreadId = FPlatformTLS::GetCurrentThreadId();
    Result.bOnGameThread = IsInGameThread();
    for (int32 Value : Values) Result.Sum += Value;
}

void AUcppHandoffTarget::EndBusiness()
{
    check(IsInGameThread());
    bAccepting = false;
    ++Generation;
}

void AUcppHandoffTarget::EndPlay(const EEndPlayReason::Type Reason)
{
    EndBusiness();
    Super::EndPlay(Reason);
}

AUcppHandoffProbe::AUcppHandoffProbe() { PrimaryActorTick.bCanEverTick = true; }

bool AUcppHandoffProbe::Request(AUcppHandoffTarget* Target, TArray<int32> Snapshot)
{
    check(IsInGameThread());
    if (!bAccepting || !IsValid(Target) || !Target->bAccepting) return false;
    // Bound work size; arithmetic stays in int64 for int32 inputs.
    if (Snapshot.Num() > 1024) return false;
    auto Job = MakeUnique<FUcppPending>();
    Job->Target = Target;
    Job->Generation = ++Target->Generation;
    Job->Work = MakeUnique<FAsyncTask<FUcppDataWork>>(MoveTemp(Snapshot));
    Job->Work->StartBackgroundTask();
    UE_LOG(LogTemp, Display, TEXT("UCPP5 submit generation=%llu GT=%u"),
        static_cast<unsigned long long>(Job->Generation), FPlatformTLS::GetCurrentThreadId());
    Pending.Add(MoveTemp(Job));
    return true;
}

void AUcppHandoffProbe::BeginPlay()
{
    Super::BeginPlay();
    Normal = GetWorld()->SpawnActor<AUcppHandoffTarget>();
    Stale = GetWorld()->SpawnActor<AUcppHandoffTarget>();
    Ended = GetWorld()->SpawnActor<AUcppHandoffTarget>();
    AUcppHandoffTarget* Destroyed = GetWorld()->SpawnActor<AUcppHandoffTarget>();
    if (!Normal || !Stale || !Ended || !Destroyed)
    {
        UE_LOG(LogTemp, Error, TEXT("UCPP5 target spawn failed"));
        StopAndDrain();
        bReported = true;
        return;
    }
    const bool N = Request(Normal, TArray<int32>{1, 2, 3});
    const bool S1 = Request(Stale, TArray<int32>{4});
    const bool S2 = Request(Stale, TArray<int32>{5});
    const bool E = Request(Ended, TArray<int32>{9});
    const bool D = Request(Destroyed, TArray<int32>{11});
    ensureAlwaysMsgf(N && S1 && S2 && E && D, TEXT("all baseline requests accepted"));
    Ended->EndBusiness();
    Destroyed->Destroy();
    if (bStopImmediately)
    {
        StopAndDrain(); // complete pure-data jobs, discard all results
        const bool Late = Request(Normal, TArray<int32>{99});
        ensureAlwaysMsgf(!Late && Pending.Num() == 0 && Normal->Applied == 0,
            TEXT("closed: reject new jobs, old jobs drained without applying"));
        bReported = true;
    }
}

void AUcppHandoffProbe::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    check(IsInGameThread());
    for (int32 Index = Pending.Num() - 1; Index >= 0; --Index)
    {
        FUcppPending& Job = *Pending[Index];
        if (!Job.Work->IsDone()) continue; // once per frame, never spin
        Job.Work->EnsureCompletion(false);
        const FUcppResult Result = Job.Work->GetTask().Result;
        AUcppHandoffTarget* Target = Job.Target.Get(); // Game Thread only
        const TCHAR* Outcome = TEXT("target-ended");
        if (bAccepting && Target && Target->bAccepting)
        {
            if (Target->Generation == Job.Generation)
            {
                Target->Applied = Result.Sum;
                Outcome = TEXT("applied");
            }
            else Outcome = TEXT("stale-generation");
        }
        UE_LOG(LogTemp, Display, TEXT("UCPP5 %s generation=%llu worker=%u workerGT=%d applyGT=%u"),
            Outcome, static_cast<unsigned long long>(Job.Generation), Result.ThreadId,
            Result.bOnGameThread ? 1 : 0, FPlatformTLS::GetCurrentThreadId());
        Pending.RemoveAtSwap(Index); // work is complete before its owner is destroyed
    }
    if (!bReported && Pending.Num() == 0)
    {
        bReported = true;
        ensureAlwaysMsgf(Normal && Stale && Ended && Normal->Applied == 6 &&
            Stale->Applied == 5 && Ended->Applied == 0, TEXT("UCPP5 final state"));
        StopAndDrain();
        const bool Late = Request(Normal, TArray<int32>{99});
        ensureAlwaysMsgf(!Late, TEXT("closed rejects new work"));
    }
}

void AUcppHandoffProbe::StopAndDrain()
{
    check(IsInGameThread());
    bAccepting = false;
    for (auto& Job : Pending) Job->Work->EnsureCompletion(false);
    Pending.Reset(); // discard results only after completion
}

void AUcppHandoffProbe::EndPlay(const EEndPlayReason::Type Reason)
{
    StopAndDrain();
    StopAndDrain();
    Super::EndPlay(Reason);
}
```

Request的按值Snapshot在GT创建，随后MoveTemp进入工作对象；worker只看自己的值。1024项上界让此处int32输入的和可以安全用int64表示，不把无限输入、复杂算法或外部阻塞藏进演示。

## 4. 同一批任务分别回答五个问题

| 场景 | 访问/业务判断 | 预期结果 |
|---|---|---|
| Normal | 弱目标有效、业务开启、代次相同 | 应用6 |
| Stale第一代/第二代 | 提交第二次后代次已变；Tick消费才应用 | 第一代丢弃；第二代应用5 |
| Ended | 对象还被UPROPERTY持有，但EndBusiness已停用 | 保持0 |
| Destroyed | Actor进入结束/销毁，弱观察不授权应用 | 丢弃结果 |
| bStopImmediately | 控制者停止接收，等worker，结果不应用 | Pending=0，再提交返回false |

默认BeginPlay提交后才由Tick消费，所以旧代次检查不靠“故意让某个线程慢一点”的运气。任务调度和完成顺序可以变化，最终应用规则不变；不根据TArray遍历顺序推断执行顺序。

Destroy()与EndBusiness分开：前者走Actor退出，后者即使保持有效对象也拒绝结果。UCPP-1的存活与业务资格在这里落实为两次判断。

## 5. 关闭标志不等于完成

StopAndDrain先拒绝新Request，再对每个工作调用EnsureCompletion(false)，最后销毁Pending。因为worker只做有限纯数据运算，不等待GT；这里等待的是实际完成，而不是重复读一个取消标志。重复Stop时没有剩余工作。

但EndPlay中的等待会占用GT。这个有限样例可以用来验证退出协议，不能直接推广为大型IO或任意长任务的生产策略。实际系统可提前异步关停、控制任务预算或采用有完成回执的排空阶段；不能为了避免卡顿直接释放仍被worker使用的数据。

也不要把“等待返回”当成GPU完成。本例没有GPU提交，CPU任务完成不提供图形资源可复用证明。[线程模型专题]({{< relref "system-design/ue-05-threading-model.md" >}})继续负责更完整的渲染交接。

## 6. 验收记录要同时留线程与身份

取得工程后，先正常完整构建，再分两次独立运行：默认模式看6/5/0及各次discard；bStopImmediately看完成后Pending清空、未应用和新请求拒绝。再分别结束Actor/退出PIE或独立包，留存全部Error/ensure，而非只收集Display。

每次保存引擎patch、工具链、source_revision、目标/配置、build_id、run_id、模块/PDB和日志。submit记录GT ID，worker把实际ID和IsInGameThread带回结果，Tick记录应用线程；必要时用Trace关联。若配置使工作不在期望后台执行，就记录实际结果并排查，不能在文章里预填workerGT=0。

当前仅官方API核对与自有代码静态检查完成；上述正常、旧代次、结束、排空、实际线程和包形态均未运行，性能与其他平台也未覆盖。

**快照保证后台数据归属，完成关系保证可以收尾，目标和代次共同决定结果能否应用。**
