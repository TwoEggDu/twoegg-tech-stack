---
title: "Unreal GAS 08｜网络预测：客户端预测与服务器验证"
slug: "ue-gas-08-network-prediction"
date: "2026-03-28"
description: "GAS 内置了一套客户端预测机制，让技能激活在本地即时响应，同时由服务器验证并回滚不一致的状态。理解预测 Key 和预测窗口是处理联机技能延迟的关键。"
tags:
  - "Unreal"
  - "GAS"
  - "网络预测"
  - "客户端预测"
series: "Unreal Engine 架构与系统"
weight: 6160
---

GAS 最复杂、也最有价值的特性之一是内置的**客户端预测**机制。玩家按下技能键后，客户端立刻在本地"预测"执行技能（播放动画、扣除费用），同时向服务器发送请求。服务器验证通过则确认，验证失败则回滚。这套机制让联机游戏的操作感更即时。

---

## 为什么需要预测

不预测的情况下，玩家按键到看到反馈的延迟 = RTT（往返延迟）：

```
玩家按键 → 发送给服务器 → 服务器执行 → 发回客户端 → 播放动画
          ←────── 200ms ──────────────────────────►
```

有预测的情况下：

```
玩家按键 → 本地立即播放动画（预测）
          → 同时发送给服务器
          → 服务器验证（150ms 后）
          → 服务器确认 / 客户端无需修正
```

---

## 预测 Key（FPredictionKey）

预测的核心是 `FPredictionKey`：一个客户端生成的唯一 ID，用于关联"客户端的预测行为"和"服务器的验证结果"。

```
客户端：
  1. 生成 PredictionKey = #42
  2. 在 Key #42 的上下文下：
     - 扣除 Mana（预测）
     - 开始冷却（预测）
     - 播放动画（本地）
  3. 发送 [激活技能] + [PredictionKey #42] 给服务器

服务器：
  4. 收到请求，验证条件
  5. 确认：广播 [Key #42 已确认]
  6. 拒绝：广播 [Key #42 已拒绝]

客户端：
  7. 收到确认 → 无需回滚
     收到拒绝 → 回滚预测的 GE（Mana 退还、冷却清除）
```

---

## NetExecutionPolicy 与预测的关系

```cpp
// LocalPredicted：本地预测 + 服务器验证（最常用）
NetExecutionPolicy = EGameplayAbilityNetExecutionPolicy::LocalPredicted;

// 在 ActivateAbility 中，CommitAbility 就在预测上下文里执行：
// - 客户端：立即扣 Cost、开始 Cooldown GE（带 PredictionKey）
// - 服务器：验证后确认或拒绝

// ServerOnly：只在服务器执行，客户端没有预测（AI 技能常用）
NetExecutionPolicy = EGameplayAbilityNetExecutionPolicy::ServerOnly;
```

---

## 预测窗口（Prediction Window）

技能激活时会建立一个"预测窗口"，窗口内的 GE 应用都带有 PredictionKey：

```cpp
void UGA_Attack::ActivateAbility(...)
{
    // CommitAbility 在预测窗口内执行
    // → Cost GE 和 Cooldown GE 都是预测的，服务器会确认/拒绝
    if (!CommitAbility(Handle, ActorInfo, ActivationInfo))
    {
        EndAbility(...);
        return;
    }

    // 在预测窗口内应用伤害 GE 也是预测的
    ApplyGameplayEffectToTarget(..., DamageEffect, ...);
}
```

---

## 预测 GE 的应用与回滚

```cpp
// 只有 LocalPredicted 技能激活时才有预测上下文
// 预测的 GE 在客户端标记为 Predicted，等待服务器确认

// 服务器拒绝时，带有该 PredictionKey 的所有 GE 自动回滚：
// - Mana 恢复（取消 Cost GE）
// - 冷却取消（取消 Cooldown GE）
// - 已应用的 Buff/Debuff 移除

// 注意：动画、音效、粒子不会自动回滚（这是预测的固有局限）
// 通常做法是接受轻微不一致，或在服务器拒绝时播放"取消"动画
```

---

## WaitNetSync：客户端服务器同步点

有时需要在技能的某个时机等待两端同步，`UAbilityTask_WaitNetSync` 提供了这个机制：

```cpp
// 等待两端（客户端和服务器）都到达某个代码点
UAbilityTask_WaitNetSync* SyncTask =
    UAbilityTask_WaitNetSync::WaitNetSync(this, EAbilityTaskNetSyncType::BothWait);

SyncTask->OnSync.AddDynamic(this, &ThisClass::OnSyncPoint);
SyncTask->ReadyForActivation();

// 典型场景：近战技能在攻击出手瞬间同步位置，防止服务器/客户端位置不一致导致伤害判定差异
```

---

## RPC 类型与技能网络

| 操作 | 发送方 | 接收方 | 说明 |
|------|--------|--------|------|
| 激活技能 | Client | Server | ServerTryActivateAbility RPC |
| 确认激活 | Server | Client | ClientActivateAbilitySucceed |
| 拒绝激活 | Server | Client | ClientActivateAbilityFailed |
| GE 同步 | Server | Client | 通过属性复制同步 |
| Cue 执行 | Server | Client | NetMulticast 或通过 GE 同步 |

---

## 预测的局限与实践建议

**能预测的**：
- 属性修改（Cost、伤害）
- Tag 的添加/移除
- 技能的激活和结束
- GE 的应用和移除

**不能预测的**：
- SpawnActor（需服务器执行，客户端只能预测视觉替代）
- 物理对象的生成
- 数据库操作

**实践建议**：
```
1. 对于玩家控制的角色：使用 LocalPredicted
2. 对于 AI：使用 ServerOnly（不需要预测）
3. 接受轻微的视觉不一致（快速拒绝时约 100ms 的错误动画）
4. 关键状态（物品消耗、存档）永远以服务器为准
5. 纯视觉的 Cue 可以用 LocalGameplayCue 完全本地化，不需要服务器同步
```
