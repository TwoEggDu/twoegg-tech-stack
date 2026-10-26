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
