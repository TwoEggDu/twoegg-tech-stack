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
