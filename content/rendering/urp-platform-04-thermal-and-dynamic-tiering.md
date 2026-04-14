---
title: "URP 深度平台 04｜热机后的质量分档：冷机、热机、长时运行与动态降档策略"
slug: "urp-platform-04-thermal-and-dynamic-tiering"
date: "2026-03-28"
description: "冷机时判成高档，不代表热机后还能稳。移动端分档真正难的部分，是 CPU / GPU 预算会在长时运行里持续变化。本篇讲清热机为什么会让冷机结论失真、动态降档状态机怎么设计、哪些动作适合运行中做、哪些必须等安全时机切。"
tags:
  - "Unity"
  - "URP"
  - "质量分级"
  - "移动端"
  - "Thermal"
  - "性能优化"
series: "URP 深度"
primary_series: "device-tiering"
weight: 1680
---
> **读这篇之前**：本篇建立在冷机分档和线上治理基础上。如果不熟悉，建议先看：
> - [URP 深度平台 02｜多平台质量分级]({{< relref "rendering/urp-platform-02-quality.md" >}})
> - [URP 深度平台 03｜机型分档怎样接线上]({{< relref "rendering/urp-platform-03-online-governance.md" >}})

前两篇平台文已经分别解决了两件事：

- [URP 深度平台 02｜多平台质量分级：三档配置的工程实现]({{< relref "rendering/urp-platform-02-quality.md" >}})：初始档位怎么判
- [URP 深度平台 03｜机型分档怎样接线上：遥测回写、Remote Config、灰度与回滚]({{< relref "rendering/urp-platform-03-online-governance.md" >}})：线上怎么持续修正

但移动端还有一个更难绕开的现实：

`同一台设备，冷机时能跑高档，不代表热机后还跑得动高档。`

很多团队第一次遇到这个问题时，表面现象会很像：

- 刚进游戏一切正常
- 玩了十几分钟后开始掉帧
- 回主城、切战斗、第一次大招、第一次多人场景，突然不稳

这时候最容易犯的错，是把它理解成”某个功能突然变差了”。

<!-- EXPERIENCE-TODO: 在这里插入一段项目经验叙事。建议框架：
     “我们第一次碰到这个问题是在 [项目代号] 的 [测试阶段/公测/正式上线]。
     QA 报了一个 bug：[设备型号] 上玩 [多长时间] 后帧率从 [X]fps 掉到 [Y]fps。
     一开始以为是 [最初怀疑的方向：内存泄漏/Shader 问题/场景复杂度]。
     后来用 [工具：Perfdog/Unity Profiler] 录了 [时长] 的帧时间曲线，发现 [关键发现：
     CPU/GPU 频率在第 N 分钟开始下降]。
     这次排查之后，我们才开始做动态降档——下面讲的状态机就是从这次经验里演化出来的。”
     要点：真实时间线、具体设备、具体数值、排查过程、最终结论。
-->

`设备预算在持续收缩，但你的档位仍然停留在冷机判断。`

所以这篇文章要解决的，是另一层分档问题：

`当设备进入热机、长时运行、后台切回或电量状态变化后，档位应该怎样动态调整，才能既保住稳定性，又不把体验做成一闪一闪的开关。`

如果说上一篇 [URP 深度平台 03｜机型分档怎样接线上：遥测回写、Remote Config、灰度与回滚]({{< relref "rendering/urp-platform-03-online-governance.md" >}}) 解决的是“设备群体层面的规则修正”，那这篇更像是在解决：

`同一台设备、同一次会话里，预算为什么还会继续变。`

## 为什么冷机分档会在持续运行后失真

冷机分档并不是错，它只是只看到了会话最开始的预算。

而移动端真正难的地方在于，预算不是固定的。

最常见的变化来源有四类：

### 1. 热与功耗在收缩 CPU / GPU 频率

冷机时设备可能能轻松顶住：

- 高分辨率
- 高阴影
- 较多附加光
- 高粒子密度

但持续运行一段时间后，`CPU / GPU` 会因为热和功耗限制进入更保守的频率区间。

这时同样的工作量，就会变成更高的帧时间。

### 2. 后台环境在变

刚启动游戏时，系统可能还很“干净”。

过一段时间后，后台下载、消息同步、录屏、系统服务和第三方应用会逐渐把环境扰动带进来。

这部分变化不会写在你的机型表里，但会写在真实帧时间里。

### 3. 长链路的首次成本会在热机后更危险

很多代价本来就不是每帧发生，而是发生在：

- 第一次大场景切换
- 第一次资源下载
- 第一次 `Shader` 命中
- 第一次特效密集战斗

如果这些首次成本恰好发生在热机后，它们就更容易直接从“小抖一下”变成“明显掉帧”。

### 4. 玩家设备状态并不恒定

同一机型在下面几种状态里，表现经常完全不同：

- 充电中 vs 非充电
- 高电量 vs 低电量省电模式
- 新机 vs 老化电池
- 空调房 vs 夏天室外

这也是为什么“机型对了”仍然不等于“这次会话对了”。

## 热机后到底是哪几类预算在变

动态分档要做得稳，前提不是先写状态机，而是先知道：

`热机后到底改变的是哪几类预算。`

我更建议至少从下面五类去看。

### 1. GPU 预算

最常见，表现通常是：

- `Render Scale` 原本够用，后来开始吃紧
- 阴影、后处理、透明特效在高负载场景里突然更容易超时
- 原本稳定的 `p95 frame time` 开始持续抬高

### 2. CPU 预算

热机后，原本“只是有点重”的逻辑、解压、反序列化、脚本收口，更容易直接顶到可感知卡顿。

所以动态分档看起来像在“调画质”，本质上很多时候是在给整条关键路径找回时间预算。

### 3. 带宽与内存行为

当设备进入热状态后，`RT` 带宽、纹理读取、内存压力和加载链上的代价也会更容易显形。

这会把一些平时勉强撑住的路径推到边界外。

### 4. 首次命中成本

如果项目里还有：

- `Shader` 首次编译 / 预热不足
- 首次资源挂载
- 第一次大特效实例化

那么它们在热机阶段的危险性会明显上升。

### 5. 用户可接受的体验窗口

动态分档不是只看技术指标。

玩家真正感知到的，是：

- 帧率是不是在持续掉
- 画面是不是突然明显糊掉
- 阴影 / 特效是不是一会开一会关

所以动态分档既是在管理设备预算，也是在管理用户感知。

## 动态分档不等于“任何时候都切 Quality Level”

很多团队第一次实现动态分档时，最容易直接写成：

- 掉帧了
- `QualitySettings.SetQualityLevel()` 降一级

这种做法最大的问题不是不能用，而是太粗。

因为运行时能安全调整的东西，和必须等安全时机再切的东西，并不是一类。

我更建议把降档动作先拆成两类。

### 一类：运行中可逆、可平滑调整的轻量旋钮

这类动作比较适合在会话中途动态做：

- 降低 `Render Scale`
- 关闭或降低 `SSAO`
- 降低阴影距离
- 降低特效密度
- 限制某些 `Renderer Feature`

它们的共同特点是：

- 不需要重建大量资源
- 切完结果相对可预期
- 出问题时也比较容易恢复

### 二类：结构性切换

这类动作更适合在场景切换、重进房间或重启后生效：

- 整体切换另一套 `URP Asset`
- 切换资源档内容
- 切换大块 `Shader` / 材质路径
- 切换大粒度 `LOD` 与资源分包策略

它们不是不能动态做，而是运行中做的代价通常更高：

- 资源边界更复杂
- 视觉跳变更明显
- 更容易引发“刚降完又恢复、刚恢复又降”的抖动

所以动态分档真正要做的，不是“任何时候都切大档”，而是：

`先用轻量旋钮吸收热波动，把结构性切换留给安全时机。`

## 一个更稳的动态分档状态机

如果把动态分档压成最小状态机，我更建议至少分成下面几种状态：

- `Warmup`：启动后的观察窗口，不立刻改档
- `Stable`：当前档位稳定运行
- `DegradePending`：观察到持续退化，等待安全时机
- `DegradedLocked`：已降档，并在锁定窗口内不再频繁反复
- `RecoverPending`：性能恢复，等待安全时机和观察窗口再考虑升档

伪代码大概可以这样写：

```csharp
public enum DynamicTierState
{
    Warmup,
    Stable,
    DegradePending,
    DegradedLocked,
    RecoverPending
}

public class RuntimeTierController
{
    public QualityTier CurrentTier { get; private set; }
    public DynamicTierState State { get; private set; } = DynamicTierState.Warmup;

    private float _badWindowSeconds;
    private float _goodWindowSeconds;
    private float _lockTimer;

    public void Tick(float avgFrameMs, bool atSafePoint)
    {
        bool tooSlow = avgFrameMs > GetTierBudgetMs(CurrentTier) * 1.15f;
        bool healthy = avgFrameMs < GetTierBudgetMs(CurrentTier) * 0.90f;

        if (tooSlow) _badWindowSeconds += Time.unscaledDeltaTime;
        else         _badWindowSeconds = 0f;

        if (healthy) _goodWindowSeconds += Time.unscaledDeltaTime;
        else         _goodWindowSeconds = 0f;

        switch (State)
        {
            case DynamicTierState.Warmup:
                if (TimeSinceStartup() > 20f)
                    State = DynamicTierState.Stable;
                break;

            case DynamicTierState.Stable:
                if (_badWindowSeconds > 8f)
                    State = DynamicTierState.DegradePending;
                break;

            case DynamicTierState.DegradePending:
                if (atSafePoint)
                {
                    ApplyLightweightDegrade();
                    _lockTimer = 30f;
                    State = DynamicTierState.DegradedLocked;
                }
                break;

            case DynamicTierState.DegradedLocked:
                _lockTimer -= Time.unscaledDeltaTime;
                if (_lockTimer <= 0f && _goodWindowSeconds > 20f)
                    State = DynamicTierState.RecoverPending;
                break;

            case DynamicTierState.RecoverPending:
                if (atSafePoint)
                {
                    TryRecoverOneStep();
                    State = DynamicTierState.Stable;
                }
                break;
        }
    }
}
```

这里面最关键的是三件事：

1. **不要凭单帧掉帧就切**
要看持续窗口，而不是某一帧尖峰。

2. **切完要锁一段时间**
否则很容易在边界附近来回抖。

3. **动作尽量放在安全时机**
比如场景切换、暂停、结算、开场加载后，而不是战斗中途突然改一大堆。

## 哪些时机可以降，哪些时机不该降

动态分档真正最容易做坏的地方，不是逻辑判断，而是时机。

### 相对安全的时机

- 进入场景前
- 战斗开始前的准备窗口
- 结算页
- 长加载结束后
- 暂停页或设置页确认后

### 高风险时机

- 大招演出中
- 战斗输入最密集时
- 精准判定玩法中
- 镜头快速切换中
- 关键叙事过场中

原因很简单：

玩家感知到的不是“你把 `Render Scale` 从 1.0 调成 0.9”，而是：

- 为什么这一秒突然糊了
- 为什么这个特效突然没了
- 为什么阴影刚才还有现在没有

所以动态分档不仅要考虑“有没有救回帧时间”，还要考虑“降档的瞬间是不是比不降更显眼”。

## 升档要比降档更保守

很多系统会把“降档”和“升档”写成对称逻辑。

在移动端这通常不够稳。

因为降档的目标，是先止血。
而升档的目标，是在不重新把自己送回热边界的前提下，慢慢恢复体验。

所以更稳的策略通常是：

- **降档看短窗口**
只要持续超预算，就可以准备降一档。

- **升档看长窗口**
要确认经过足够长的稳定运行后，才尝试恢复一步。

- **升档一步一步来**
不要从 `Low` 直接跳回 `High`。

这也是为什么我更喜欢把动态分档理解成：

`快速止血，慢速恢复。`

## 验证热机策略，最容易忽略的不是数据，而是样本

动态分档策略最怕的不是逻辑写错，而是验证样本太理想。

如果你只在下面这种条件下测试：

- 冷机
- 刚启动
- 同一静态场景
- 跑 3 分钟结束

那这套策略对真实线上几乎没有说服力。

更稳的验证样本至少应该包括：

### 1. 冷机启动样本

回答“初始判定有没有过度保守或过度激进”。

### 2. 热机持续运行样本

例如：

- 连续 15~20 分钟战斗
- 多次场景切换
- 高特效密度玩法循环

回答“预算收缩后会不会持续掉帧、会不会及时降档”。

### 3. 后台切回样本

回答“中断恢复后是否会重新进入错误档位或触发首次成本尖峰”。

### 4. 充电 / 低电量样本

回答“电源状态变化会不会把判定带偏”。

### 5. 老化设备样本

回答“同型号不同健康度是否需要更保守的默认值”。

如果项目真的重视移动端长期稳定性，这组样本不能省。

## 一条最小可执行路径

如果你现在还没有热机后的动态分档，我会建议先做下面这条最小链：

1. 先把运行中可逆的轻量旋钮列清楚
2. 先选一个固定预算指标，例如关键场景 `avg / p95 frame time`
3. 先加持续超预算观察窗口，不要看单帧
4. 先把降档动作只放到少数安全时机
5. 先给降档后的恢复加锁和长观察窗口

只要这五步立住，动态分档就已经从“想到就切”进化成一套有工程边界的运行时系统了。

## 小结

如果把这篇文章压成一句话，我会这样收：

`移动端真正难的不是冷机时怎么分档，而是当热、功耗、长时运行和首次成本把预算持续往下拉时，怎样用短窗口止血、长窗口恢复、轻量旋钮优先、结构性切换后移的方式，把动态分档做稳。`

下一篇更适合接着看的是 [URP 深度平台 05｜质量分档不只改 URP：资源、LOD、特效与包体怎么一起分层]({{< relref "rendering/urp-platform-05-content-tiering.md" >}})。

因为知道“什么时候该降”之后，下一个更实际的问题就是：

`降档时，到底应该降哪些东西，而不是只会切一张 URP Asset。`
