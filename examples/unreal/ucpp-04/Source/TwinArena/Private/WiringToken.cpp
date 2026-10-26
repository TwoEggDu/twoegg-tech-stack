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
