---
title: "Unity DOTS N02｜CommandData 与输入链：客户端输入怎样进入预测系统"
slug: "dots-n02-commanddata-input-chain"
date: "2026-03-28"
draft: true
description: "Prediction 的入口不是状态，而是输入。先把客户端采样、CommandData、服务端权威消费和本地预测之间的输入链冻住，后面的 Prediction / Rollback 才不会写成一团。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "CommandData"
  - "Prediction"
  - "Input"
series: "Unity DOTS NetCode"
primary_series: "unity-dots-netcode"
series_role: "article"
series_order: 2
weight: 2302
---

N01 已经把 `Client World / Server World / Ghost / Authority` 的世界地图立住了，N03 又把“哪些状态该同步、哪些根本不该发”压住了。到了 N02，真正要回答的是另一条入口链：**客户端输入到底怎样进入预测系统，为什么它必须和状态复制链分开**。

很多多人同步项目最后写乱，不是因为 `Rollback` 太难，而是因为一开始就把“输入”写成了“状态”，或者把“客户端结果”错当成了“服务端可以直接相信的事实”。N02 的任务，就是先把这条输入链冻住，不让后面的 Prediction 和 Snapshot 再互相抢职责。

---

## 为什么预测系统的入口不是状态，而是输入

客户端之所以需要 Prediction，不是因为它想提前知道服务器最终状态，而是因为它必须在本地输入发生时立刻给玩家反馈。这个即时反馈如果等服务器确认，再快也会天然落后一个来回延迟。

所以 Prediction 的真正入口从来不是“我先猜一个状态”，而是“我先把本地输入按正确的 Tick 送进预测链”。状态是结果，输入才是起点。如果一开始就把状态和输入混在一起，后面你会同时遇到三种问题：

- 客户端把本地推导结果当成可同步事实。
- 服务端收到的是半成品状态，而不是可复放的输入。
- 回滚时你找不到应该重放哪一帧的哪份输入。

换句话说，Prediction 不是“多跑一遍状态机”，而是“基于本地输入先行推进，再等待权威状态回来对齐”。

## CommandData 在链路里到底站哪层

`CommandData` 不应该被理解成“一个更快的状态包”，它更像是**客户端送进权威仿真的输入切片**。它站在输入链上，不站在状态复制链上。

这条边界一旦立住，职责就很清楚了：

- `Snapshot` 负责从服务端往外复制权威状态。
- `CommandData` 负责从客户端往服务端提交输入意图。
- Prediction 负责先用本地输入推进本地对象。
- 服务端负责用收到的输入做最终裁决。

它们互相相关，但不是一回事。最常见的错误写法，是把“客户端已经算出来的朝向、位置、技能结果”顺手也塞进输入链，让服务端“省点事”。这样做短期看起来省代码，长期会把权威边界直接做没。

## 一条稳定的输入链应该长什么样

把输入链压成最小模型，通常就是下面这五步：

```text
本地采样输入
    -> 归一化 / 量化 / 绑定 Tick
    -> 写入 CommandData
    -> Client World 用这份输入做 Prediction
    -> Server World 用同一类输入做权威消费
```

这里最关键的，不是哪一步“最快”，而是同一份输入必须在链路里保持稳定语义：

- 它来自哪一帧、哪一个 Tick。
- 它到底表达“意图”还是表达“结果”。
- 它能不能在回滚时被重放。

如果这三个问题没有先冻结，你后面即使实现了 Prediction，也很难解释“为什么这次回滚之后结果不一样”。

## CommandData 里该放什么，不该放什么

一个很实用的判断规则是：**CommandData 里放的是客户端希望服务端拿来裁决的输入意图，不是客户端已经算完的结果**。

通常适合放进去的内容有：

- 方向输入、移动轴值、按键状态。
- 攻击、施法、交互这类离散意图。
- 与 Tick 强绑定、可回放的瞄准输入或朝向输入。

通常不该直接放进去的内容有：

- 已经算出来的位置、旋转、命中结果。
- 只用于本地手感或本地表现的瞬时值。
- UI、摄像机、屏幕抖动、准星动画等表现层信息。

也就是说，CommandData 里记录的是“我想做什么”，不是“我已经做成了什么”。前者能被服务端重放和裁决，后者只会把客户端结果偷运进权威链。

## 最小写法：先采样、再归一化、再进预测链

下面这段代码不是版本绑定教程，只是说明输入链该怎样分层：

```csharp
public struct PlayerCommand : IComponentData
{
    public int Tick;
    public float2 Move;
    public bool FirePressed;
    public float AimYaw;
}

public sealed class PlayerInputBridge : MonoBehaviour
{
    public PlayerCommand SampleCommand(int tick)
    {
        return new PlayerCommand
        {
            Tick = tick,
            Move = ReadMoveAxis(),
            FirePressed = ReadFireButton(),
            AimYaw = ReadAimYaw()
        };
    }
}

public partial struct PlayerPredictionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 读取 PlayerCommand，
        // 在 Client World 里先推进本地预测，
        // 等服务端权威状态回来后再做对齐。
    }
}
```

这段写法真正想说明的不是 API 名字，而是三层职责：

- 输入采样站在 Managed 侧。
- `PlayerCommand` 只表达输入意图。
- Prediction 消费输入，不消费客户端私自算好的状态。

只要这三层没有互相串位，后面的回滚和校正才有机会稳定。

## 服务端为什么不能“相信客户端结果”

很多刚开始做多人同步的团队，都会忍不住问一句：既然客户端已经本地算了一遍，服务端为什么不直接信它，省一次重算？

因为客户端 Prediction 的目标是“降低输入延迟感”，不是“替代权威裁决”。服务端如果直接信客户端结果，会立刻失去下面这些能力：

- 统一裁决碰撞、命中、冷却和资源消耗。
- 在丢包、抖动、乱序时稳定重建输入序列。
- 在需要反作弊或行为校验时保留权威证据链。

从工程角度看，客户端结果最多是一份本地临时猜测；服务端真正需要的，是可以被按 Tick 重放、按规则消费的输入流。

## 输入链最容易写坏的三个地方

第一个坑，是多个系统各自采样输入，再各自解释。这样你很快就会遇到“这一帧到底哪份输入才是真的”。

第二个坑，是把表现层输入和权威输入混在一起。比如本地摄像机微调、UI 拖拽、准星动画这些东西，本来就不该进入权威链。

第三个坑，是命令里没有明确 Tick，对齐时再靠当前帧猜。没有 Tick 的输入链，几乎注定会在 Prediction / Rollback 阶段变成黑箱。

## CommandData 和 Snapshot 为什么绝对不能混

N03 已经讲过，`Snapshot` 负责复制权威状态，服务端告诉客户端“现在世界是什么样”。N02 要补的就是另一半：`CommandData` 负责提交输入，客户端告诉服务端“我刚才想做什么”。

这两条链一旦混起来，后果通常是：

- 客户端把结果伪装成输入发出去。
- 服务端把状态回传和输入回放搅在一层里。
- 调试时你根本分不清问题出在输入采样、权威裁决，还是状态复制。

稳定的 NetCode 结构，必须先承认这两条链天然不同向、不同义、不同职责。

## 小结

Prediction 的入口不是状态，而是输入；`CommandData` 负责把输入意图送进权威仿真链，不负责偷运客户端结果。
只要输入采样、Tick 绑定和命令语义先冻结住，Prediction / Rollback 才有稳定的重放基础。
`CommandData` 和 `Snapshot` 一条向里、一条向外，职责绝对不能混，不然多人同步很快就会从“有点抖”变成“完全说不清哪边在负责”。

下一步应该：`DOTS-N04｜Prediction / Rollback：为什么“多跑一遍逻辑”远远不够`

理由：状态复制边界和输入入口都定住以后，才轮得到把 Prediction / Rollback 这条真正高风险的链拆开。
扩展阅读：[技能系统深度 11｜多人同步：服务器权威、预测、回滚、命中确认、冷却同步应该怎么拆]({{< relref "engine-notes/skill-system-11-multiplayer-sync.md" >}})
