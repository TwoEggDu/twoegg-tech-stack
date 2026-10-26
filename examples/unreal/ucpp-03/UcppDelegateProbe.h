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
