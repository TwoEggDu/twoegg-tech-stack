---
title: "Unreal 网络 03｜移动同步：CharacterMovementComponent 与客户端预测"
slug: "ue-net-03-movement-sync"
date: "2026-03-28"
description: "Unreal 的角色移动同步通过 CharacterMovementComponent 的预测-校正机制实现。理解 SavedMove、服务器校正和位置回滚，是解决联机移动问题的关键。"
tags:
  - "Unreal"
  - "网络"
  - "移动同步"
  - "CharacterMovement"
series: "Unreal Engine 架构与系统"
weight: 6190
---

角色移动的网络同步是联机游戏中最复杂的问题之一。Unreal 的 `CharacterMovementComponent` 内置了一套完整的客户端预测与服务器校正机制，让本地玩家的移动感觉即时，同时由服务器保持权威位置。

---

## 移动同步的三个角色

```
本地玩家（AutonomousProxy）：
  1. 采集输入（WASD）
  2. 本地立即执行移动（不等服务器）
  3. 保存 SavedMove（用于可能的回滚）
  4. 发送 ServerMove RPC 给服务器

服务器（Authority）：
  5. 收到 ServerMove，在服务器位置执行相同移动
  6. 比较结果位置与客户端报告的位置
  7. 如果误差 < 阈值：发送 ClientAckGoodMove（确认）
  8. 如果误差 > 阈值：发送 ClientAdjustPosition（校正）

其他客户端（SimulatedProxy）：
  9. 收到服务器广播的位置，通过插值平滑显示
```

---

## SavedMove：记录每一帧的移动

```cpp
// SavedMove 保存了发送给服务器的移动数据（用于校正时重播）
class FSavedMove_Character
{
    // 保存的输入状态
    uint8 bPressedJump : 1;
    float TimeStamp;
    float DeltaTime;
    FVector SavedLocation;
    FRotator SavedRotation;
    FVector SavedVelocity;
    FVector Acceleration;
    // ...
};
```

---

## 客户端校正（Correction / Reconciliation）

当服务器发现位置误差过大时，会触发客户端校正：

```
服务器发送 ClientAdjustPosition(ServerTimeStamp, ServerLocation)
  ↓
客户端收到：
  1. 将位置重置为服务器位置
  2. 找到所有 TimeStamp > ServerTimeStamp 的 SavedMove
  3. 重新回放这些 Move（Replay）
  4. 最终得到更接近服务器的正确位置
```

这就是为什么网络延迟高时你会看到角色"弹回去"——那是服务器校正。

---

## 自定义移动组件（添加新的移动模式）

```cpp
// 1. 继承 FSavedMove 添加新数据
class FSavedMove_MyCharacter : public FSavedMove_Character
{
    typedef FSavedMove_Character Super;
public:
    uint8 bWantsToDash : 1;  // 新增：冲刺输入

    virtual void Clear() override
    {
        Super::Clear();
        bWantsToDash = 0;
    }

    virtual void SetMoveFor(ACharacter* C, float DeltaTime, FVector const& NewAccel,
        FNetworkPredictionData_Client_Character& ClientData) override
    {
        Super::SetMoveFor(C, DeltaTime, NewAccel, ClientData);
        if (UMyMovementComponent* MC = Cast<UMyMovementComponent>(C->GetCharacterMovement()))
        {
            bWantsToDash = MC->bWantsToDash;
        }
    }

    virtual bool CanCombineWith(const FSavedMovePtr& NewMove, ACharacter* InCharacter,
        float MaxDelta) const override
    {
        // 冲刺输入变了就不能合并
        if (bWantsToDash != ((FSavedMove_MyCharacter*)&NewMove.Get())->bWantsToDash)
            return false;
        return Super::CanCombineWith(NewMove, InCharacter, MaxDelta);
    }
};

// 2. 继承 FNetworkPredictionData
class FNetworkPredictionData_Client_MyCharacter
    : public FNetworkPredictionData_Client_Character
{
    typedef FNetworkPredictionData_Client_Character Super;
public:
    virtual FSavedMovePtr AllocateNewMove() override
    {
        return FSavedMovePtr(new FSavedMove_MyCharacter());
    }
};

// 3. 继承 UCharacterMovementComponent
UCLASS()
class UMyMovementComponent : public UCharacterMovementComponent
{
    GENERATED_BODY()
public:
    bool bWantsToDash = false;

    virtual FNetworkPredictionData_Client* GetPredictionData_Client() const override
    {
        if (!ClientPredictionData)
        {
            const_cast<UMyMovementComponent*>(this)->ClientPredictionData =
                new FNetworkPredictionData_Client_MyCharacter(*this);
        }
        return ClientPredictionData;
    }

    virtual void OnMovementUpdated(float DeltaSeconds, const FVector& OldLocation,
        const FVector& OldVelocity) override
    {
        Super::OnMovementUpdated(DeltaSeconds, OldLocation, OldVelocity);

        if (bWantsToDash)
        {
            // 执行冲刺逻辑（客户端本地预测，服务器会验证）
            PerformDash();
        }
    }
};
```

---

## SimulatedProxy 的平滑插值

其他玩家（SimulatedProxy）的位置通过插值平滑：

```cpp
// 控制台变量（可在项目中调整）
p.NetProxyShrinkRadius   // 碰撞胶囊缩小量（防止穿透）
p.NetProxyShrinkHalfHeight

// 平滑方式：Linear 或 Exponential
// 可通过 NetworkSmoothingMode 配置
// NetworkSmoothingMode = Linear / Exponential / Replay / Disabled
```

---

## 移动同步的常见问题

**问题 1：角色在高延迟时弹跳**
- 原因：服务器与客户端位置差异超过阈值，频繁触发校正
- 解决：提高 `MaxDepenetrationWithGeometryAsProxy`，或增大 `MAXPOSITIONERRORSQUARED`

**问题 2：AddMovementInput 在服务器不生效**
- 原因：`AddMovementInput` 只更新 Acceleration，而移动逻辑在 `PerformMovement` 中。服务器上的 Simulated Actor 不会调用这个
- 解决：通过 Server RPC 传递移动意图，或确认 AI/服务器移动走正确路径

**问题 3：自定义移动模式网络不同步**
- 原因：新增的移动参数没有加入 SavedMove
- 解决：参照上面的自定义移动组件示例，将所有影响移动结果的数据加入 SavedMove
