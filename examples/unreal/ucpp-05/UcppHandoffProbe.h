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
