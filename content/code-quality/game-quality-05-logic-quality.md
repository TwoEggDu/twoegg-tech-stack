---
date: "2026-04-13"
title: "游戏质量保障 05｜逻辑质量：游戏逻辑的可测试性设计与回归策略"
description: "游戏逻辑天然难测：依赖帧循环、随机数、输入时序、全局状态。但这不是不写测试的理由，而是需要换一种测试策略。这篇讲怎样让核心系统可测试，以及回归测试应该覆盖什么、不该覆盖什么。"
slug: "game-quality-05-logic-quality"
weight: 104
featured: false
tags:
  - "Quality"
  - "Testing"
  - "Testability"
  - "Regression"
series: "游戏质量保障体系"
series_id: "game-quality"
series_role: "article"
series_order: 5
---

游戏逻辑天然难测，但这不是不写测试的理由，而是需要换一种测试策略。

这篇要回答的核心问题是：游戏项目里，哪些逻辑值得测、怎样让它们变得可测、回归应该覆盖什么。

## 游戏逻辑为什么比 Web 难测

Web 后端的大部分逻辑是请求 -> 处理 -> 响应，输入输出边界清楚，状态通常交给数据库管理。游戏逻辑的情况完全不同。

### 1. 帧循环依赖

游戏核心循环跑在 `Update` / `FixedUpdate` 里，每一帧都可能改变状态。一个技能释放逻辑可能横跨 30 帧，每帧都在读写共享状态。这种"持续运行的状态机"几乎不可能直接用请求-响应模型去测。

### 2. 全局状态泛滥

玩家等级、背包数据、当前关卡、系统时间、网络状态、音量设置，这些状态散落在各种单例和静态变量里。要测一个伤害计算函数，你可能需要先把半个游戏世界搭起来。

### 3. 随机性

掉落、暴击、怪物行为、地图生成，随机数贯穿整个玩法层。如果随机源不可控，同一个测试每次跑的结果都不一样。

### 4. 异步和时序

网络请求回调、资源加载完成、动画事件触发、协程恢复。游戏里大量逻辑依赖"某件事在某个时间点发生"，而不是"调用一个函数立刻得到结果"。

### 5. UI 和表现耦合

很多团队的业务逻辑直接写在 UI 组件里。判断"玩家能不能购买这个道具"的逻辑和"按钮变灰、弹出提示、播放动画"的代码缠在一起，根本没有独立的切口。

这五个特征叠加起来，意味着游戏逻辑如果不做任何设计，默认就是不可测的。

## 可测试性设计的 4 条原则

既然游戏逻辑默认不可测，那就需要主动创造可测试的切口。不是要把整个系统重写，而是在关键系统上遵循四条原则。

### 原则一：纯函数提取

把核心计算逻辑从引擎对象里拆出来，变成纯函数。

```csharp
// 不可测：逻辑嵌在 MonoBehaviour 里
public class DamageSystem : MonoBehaviour
{
    void ApplyDamage(Enemy enemy)
    {
        float dmg = player.Atk * skillMultiplier - enemy.Def;
        if (Random.value < critRate) dmg *= 2f;
        enemy.Hp -= Mathf.Max(0, dmg);
        PlayHitEffect(enemy.transform.position);
    }
}

// 可测：纯计算函数，无副作用
public static class DamageCalc
{
    public static DamageResult Calculate(DamageInput input)
    {
        float raw = input.Atk * input.SkillMultiplier - input.Def;
        float final = input.IsCrit ? raw * 2f : raw;
        return new DamageResult(Mathf.Max(0f, final));
    }
}
```

纯函数的好处不只是好测，更重要的是规则边界变清楚了。改公式的时候不用担心动到特效播放，review 的时候一眼就能看到计算逻辑。

### 原则二：依赖注入

时间、随机数、外部服务这三类依赖最该被显式注入。

```csharp
public sealed class CooldownChecker
{
    private readonly Func<float> _getTime;

    public CooldownChecker(Func<float> getTime)
    {
        _getTime = getTime;
    }

    public bool IsReady(float lastCastTime, float cooldown)
    {
        return _getTime() >= lastCastTime + cooldown;
    }
}
```

测试时传入可控的时间源，生产时传入 `() => Time.time`。不需要复杂的 DI 容器，一个 `Func` 或一个接口就够了。

### 原则三：状态可快照

如果一个系统的状态可以被序列化成快照、再从快照恢复，测试就可以精准构造任何场景。

```csharp
public struct BattleSnapshot
{
    public int Turn;
    public int[] PlayerHp;
    public int[] EnemyHp;
    public BuffState[] ActiveBuffs;
}
```

状态可快照还有一个副产品：bug 复现变得极其简单。QA 报告附上快照，开发直接从那个状态开始调试。

### 原则四：时间可控制

所有依赖时间推进的逻辑，都应该支持外部传入时间，而不是自己去读全局时钟。

这包括冷却、Buff 持续时间、倒计时、帧间隔。测试时可以一次性推进 10 秒，不用真等 10 秒。

## 什么值得测、什么不值得测

测试预算有限，不可能把所有逻辑都覆盖。游戏项目里需要一条清楚的线，区分"必须测"和"不值得测"。

### 值得测的

| 类型 | 例子 | 原因 |
|------|------|------|
| 核心公式 | 伤害计算、经验曲线、概率公式 | 改动频繁，出错代价极高 |
| 状态转换 | 战斗状态机、任务状态流转、商店交易流程 | 边界多、组合多，人工很难穷举 |
| 配置解析 | 技能表、掉落表、关卡配置 | 策划频繁改表，回归风险高 |
| 协议契约 | 客户端-服务端协议编解码 | 两端不一致直接导致线上事故 |
| 资源引用链 | 预制体加载路径、AB 依赖关系 | 一断就是运行时崩溃 |

### 不值得测的

| 类型 | 例子 | 原因 |
|------|------|------|
| UI 布局 | 按钮位置、文字对齐 | 变化极快，维护成本远超收益 |
| 动画时序 | 第 3 帧播什么特效 | 依赖引擎运行时，且变化频繁 |
| 纯表现逻辑 | 镜头震动幅度、粒子颜色 | 出错影响小，靠肉眼检查更高效 |
| 引擎胶水层 | MonoBehaviour 生命周期编排 | 本身就是 orchestration，越薄越好 |

这条线不是绝对的，但它给出一个判断框架：

`改动频率高 × 出错代价大 × 能被纯逻辑表达 = 值得测。`

## 回归测试策略

知道什么值得测以后，下一个问题是：回归应该怎么组织。

### 1. 公式回归

所有核心数值公式都应该有对应的单元测试，覆盖正常值、边界值和异常值。

```csharp
[Test]
public void Damage_ZeroDef_ReturnsFullDamage()
{
    var input = new DamageInput(atk: 100, def: 0, multiplier: 1.5f, isCrit: false);
    var result = DamageCalc.Calculate(input);
    Assert.AreEqual(150f, result.FinalDamage);
}

[Test]
public void Damage_DefHigherThanAtk_ReturnZero()
{
    var input = new DamageInput(atk: 50, def: 200, multiplier: 1f, isCrit: false);
    var result = DamageCalc.Calculate(input);
    Assert.AreEqual(0f, result.FinalDamage);
}
```

这类测试跑得极快，应该放在 CI 每次提交都跑。

### 2. 状态机回归

关键状态机（战斗流程、任务系统、商店交易）应该有状态转换矩阵测试。

重点不是测每个状态内部的行为，而是测状态之间的转换边界：

- 从状态 A 能不能到状态 B
- 从状态 A 到状态 B 以后，能不能正确回到状态 A
- 非法转换是否被正确拦截
- 并发输入下状态是否仍然一致

### 3. 配置校验回归

策划改表是游戏项目里最高频的变更来源之一。配置校验回归不是测逻辑，而是测数据。

```csharp
[Test]
public void SkillTable_AllSkillIds_AreUnique()
{
    var ids = SkillTable.LoadAll().Select(s => s.Id).ToList();
    Assert.AreEqual(ids.Count, ids.Distinct().Count());
}

[Test]
public void DropTable_AllReferencedItems_Exist()
{
    var dropItems = DropTable.LoadAll().SelectMany(d => d.ItemIds);
    var allItems = ItemTable.LoadAll().Select(i => i.Id).ToHashSet();
    foreach (var id in dropItems)
        Assert.IsTrue(allItems.Contains(id), $"Drop references missing item: {id}");
}
```

配置校验应该在策划提交配置时自动触发，不需要等到构建阶段。

## 服务端逻辑的可测试性

服务端逻辑反而比客户端更容易做到可测试，原因很简单：没有渲染、没有帧循环、没有输入设备。

服务端的大部分逻辑天然就是请求 -> 处理 -> 响应。需要额外注意的是：

### 1. 数据库依赖隔离

不要让业务逻辑直接依赖数据库连接。用 Repository 模式或类似的抽象，让测试可以注入内存实现。

### 2. 时间依赖隔离

服务端同样有大量时间相关逻辑：活动开关、限时商品、冷却、赛季重置。这些都应该通过可注入的时钟接口获取时间。

### 3. 随机数隔离

服务端的随机性通常更关键，因为它直接影响经济系统和公平性。随机源必须可注入、可回放。

### 4. 并发状态验证

服务端最容易出的问题不是单个请求处理错误，而是并发场景下的状态不一致。这类测试需要模拟多个请求同时操作同一个实体。

## 测试金字塔在游戏项目里的变形

经典测试金字塔是：底层单元测试多、中间集成测试适量、顶层 E2E 测试少。

在游戏项目里，这个金字塔会发生明显变形：

### 底层：纯逻辑单元测试 -- 应该多

公式、状态机、配置校验、协议编解码。这些跑得快、维护成本低、价值密度高。

### 中层：集成测试 -- 比 Web 项目少

原因是游戏的集成层往往涉及引擎运行时。要测"技能释放后 Buff 生效、怪物减血、UI 更新"这条链路，需要搭起大量引擎环境。投入产出比不好。

更实际的做法是把集成层切薄：只验证"纯逻辑层的输出是否被正确消费"，不验证"消费以后表现是否正确"。

### 顶层：E2E 测试 -- 极少

全自动化的端到端测试在游戏项目里成本极高。UI 变化快、流程路径多、引擎状态难以精确控制。

更务实的做法是：把 E2E 留给少数最关键的冒烟路径（启动 -> 登录 -> 主城 -> 核心玩法入口），用自动化脚本或录制回放工具执行，而不是试图覆盖所有功能。

所以游戏项目的测试金字塔更像一个倒三角被压扁的形状：

```
      E2E（极少，只覆盖冒烟路径）
   集成（少，切薄集成层）
纯逻辑单元测试（大量，核心公式/状态机/配置校验）
```

## 反模式

最后列出几个游戏项目里最常见的测试反模式。

### 1. 测试依赖运行时顺序

```csharp
// 反模式：Test_B 假设 Test_A 已经执行
[Test] public void Test_A() { GameState.Level = 5; }
[Test] public void Test_B() { Assert.AreEqual(5, GameState.Level); }
```

测试之间不应该有隐式依赖。每个测试必须自己构造所需状态。测试框架不保证执行顺序，而且并行执行会直接打破这种假设。

### 2. Mock 太多失去意义

如果一个测试 Mock 了 6 个依赖，只验证了一行调用是否发生，这个测试其实什么都没证明。它证明的是"你的 Mock 写对了"，而不是"你的逻辑对了"。

Mock 应该只用在真正需要隔离的外部依赖上。如果一个函数需要 Mock 太多东西才能跑起来，说明函数本身的边界有问题。

### 3. 只测 happy path

```csharp
// 只测了正常购买
[Test] public void Buy_Success() { ... }

// 缺少：余额不足、物品不存在、已下架、并发购买、数量溢出
```

游戏逻辑的 bug 几乎都出在边界和异常路径上。一个购买函数如果只测"能买"，就相当于没测。至少要覆盖：余额不足、物品不存在、物品已下架、数量为 0 或负数、并发购买同一物品。

### 4. 把测试当文档写

有些团队会把测试方法名写成一段话，测试体里写满注释，让测试"读起来像文档"。结果测试变得很长、很脆、很难维护。

测试应该短、准、独立。如果需要文档，写文档；如果需要测试，写测试。不要试图让一个东西同时承担两个职责。

### 5. 配置改了不跑测试

策划改了一个掉落表，没有触发任何校验，直到线上有玩家发现某个物品掉率变成了 0。

配置变更应该和代码变更一样，经过自动化校验。

## 结论

游戏逻辑确实比 Web 逻辑更难测，但难测不意味着不能测。真正的问题不是"测试太难写"，而是"代码从一开始就没有留出可测试的切口"。

可测试性设计的核心就是四件事：把计算逻辑提成纯函数、把关键依赖变成可注入、让状态可快照可恢复、让时间可外部控制。

回归策略的核心是分清层次：公式回归钉住数值正确性，状态机回归钉住转换边界，配置校验回归钉住数据一致性。

不需要追求 100% 覆盖率，也不需要把所有逻辑都变成可测的。先从改动最频繁、出错代价最大的那一小块开始，把可测试性设计和回归策略逐步铺开，这才是游戏项目里真正能落地的做法。
