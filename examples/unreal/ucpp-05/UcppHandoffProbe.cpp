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
