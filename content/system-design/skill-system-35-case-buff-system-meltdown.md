---
date: "2026-04-13"
title: "技能系统深度 35｜案例：Buff 系统失控 — 叠加、快照、实时重算混用的生产事故"
description: "一个 Buff 叠到 99 层后角色变无敌。表面原因是叠层上限没配，真正根因是 Modifier 聚合顺序依赖 Buff 添加顺序而非优先级，加上快照和实时重算混在同一个聚合链里。这篇从事故现场到根因到两种修法完整复盘。"
slug: "skill-system-35-case-buff-system-meltdown"
weight: 8035
tags:
  - Gameplay
  - Skill System
  - Buff
  - Modifier
  - Case Study
  - Postmortem
series: "技能系统深度"
series_order: 35
---

> Buff 系统失控的根因几乎从来不是"没加上限"，而是 Modifier 的聚合规则没有在架构层定义清楚。

这篇不是理论推演。它来自一个线上真实事故。事故等级 P1，从发现到热修复用了 2 小时，但从热修复到真正修干净，花了三周。

---

## 这篇要回答什么

1. 一个 Buff 怎样从正常运行到失控
2. 表面原因和真正根因的区别
3. Modifier 聚合的正确分阶段模型
4. 快照和实时重算为什么不能混在同一条聚合链里
5. 怎样用自动化测试防止类似问题复现

---

## 事故现场

周四晚上 9 点，运营群里连续贴了三张截图：某英雄在 PvP 对局里，护甲值显示 99999，对面打上去全是 0。

第一反应是外挂。但查了反作弊日志，账号全部干净。

调了战斗录像，发现规律：

- 该英雄有一个被动 Buff，每次受击叠加一层，每层 +30 护甲
- 正常对局叠到 3-5 层，最多 8 层
- 但这几场对局里叠到了 99 层
- 99 × 30 = 2970，但实际护甲显示 99999，远超线性叠加

两件事不对：没有叠层上限，且护甲值远超 `层数 × 单层值` 的预期。

影响范围：过去 24 小时内 PvP 模式约 5% 的对局出现过类似现象。

---

## 第一层：表面原因

Buff 配置表里缺少 `MaxStack` 字段：

```text
Buff: PassiveArmorStack
  TriggerCondition: OnHit
  StackMode: Additive
  MaxStack: -1          ← -1 表示无上限，策划本意是"暂不限制"
  Modifier:
    Target: Armor
    Op: Add
    Value: 30
```

团队第一反应：热修复把 `MaxStack` 改成 10。改了之后 10 × 30 = 300，加上基础护甲 200，总共 500，数值合理。

看起来问题解决了。但 QA 回归测试时发现了新问题。

---

## 第二层：为什么加了上限还有问题

QA 构造了一个场景：同时施加 Buff A（每层 +50 护甲）和 Buff B（护甲 ×1.5），Buff A 叠 5 层。

预期：`(200 + 250) × 1.5 = 675`。实际结果不稳定 — 有时 675，有时 712，有时 803。

规律：如果 A 的 5 层全部先于 B 生效，结果正确；如果 A 和 B 交替施加，结果偏高。

查了聚合代码：

```csharp
public float Aggregate(float baseValue)
{
    float result = baseValue;
    foreach (var mod in _modifiers) // ← List，按添加顺序遍历
    {
        switch (mod.Op)
        {
            case ModOp.Add:      result += mod.Value; break;
            case ModOp.Multiply: result *= mod.Value; break;
            case ModOp.Override: result = mod.Value;  break;
        }
    }
    return result;
}
```

`_modifiers` 按 Buff 添加时间顺序排列，没有按 Op 类型分阶段。

先加后乘和先乘后加结果不同，交替施加时每次 `×1.5` 都会放大前面所有 `+50` 的累积值，效果不等于最后统一乘一次。

---

## 第三层：真正根因 — 快照和实时重算混在一起

把聚合顺序改成分阶段后，以为问题解决了。但又一轮测试暴露了更深的问题。

英雄身上同时有两种 Buff：Buff C 是实时重算类型（每帧重新聚合），Buff D 是快照类型（Apply 时锁定当前值）。

Buff D 的快照逻辑：

```csharp
public override void OnApply(Entity target)
{
    float currentArmor = target.GetAttribute(AttributeType.Armor);
    float contribution = currentArmor * 0.3f; // ← 快照"整个属性的当前值"
    target.AddModifier(new Modifier {
        Source = this, Target = AttributeType.Armor,
        Op = ModOp.Add, Value = contribution
    });
}
```

看线上出问题的那个 Buff 组合：

```text
Buff: IronSkin (快照类型)
  OnApply: 快照当前护甲值，添加 Modifier → Armor + 快照值 × 0.3

执行过程：
  1. 基础护甲 200，PassiveArmorStack 叠 5 层 → +150 → 当前 350
  2. IronSkin Apply → 快照 350 → Modifier: Armor +105
  3. 护甲 = 200 + 150 + 105 = 455
  4. IronSkin 被刷新（重新 Apply）→ 快照 455 → 新 Modifier: +136.5
  5. 护甲 = 200 + 150 + 136.5 = 486.5
  6. 再刷新 → 快照 486.5 → +145.95 → ...
```

每次 IronSkin 刷新，快照值都包含上一次快照的贡献。这就是指数增长的来源。

和叠层上限无关。就算 PassiveArmorStack 只有 1 层，只要 IronSkin 反复刷新，护甲值就会不断膨胀。

这就是为什么"加上限"只治标不治本：`MaxStack` 限制了叠层数，但没有修复聚合链的重复计算。

根因有两个：

1. 聚合顺序依赖 Buff 添加顺序而非阶段划分
2. 快照类 Modifier 快照了整个属性的当前值，而非自身贡献值

两者叠加，任何涉及快照 + 实时重算混用的 Buff 组合都可能触发数值膨胀。

---

## Modifier 聚合的正确分阶段模型

理清根因后，重新设计了聚合模型。

### 阶段划分

```text
Phase 1: Base Value       → 角色表里的基础属性值
Phase 2: Flat Additive    → 所有 +N 的 Modifier 求和后加
Phase 3: Pct Additive     → 所有 +N% 的 Modifier 求和后乘 (1 + sum)
Phase 4: Multiplicative   → 所有 ×N 的 Modifier 依次相乘
Phase 5: Override         → 直接替换，多个取最高优先级
Phase 6: Clamp            → min/max 钳制
```

### 关键规则

- 每个阶段内部按优先级排序
- 阶段之间顺序固定，不随 Buff 添加顺序变化
- 快照类 Modifier 锁定的是"该 Modifier 的贡献值"，不是"整个属性的当前值"

```csharp
public float Aggregate(float baseValue, List<Modifier> modifiers)
{
    float result = baseValue;

    // Phase 2: 收集所有加法值，一次性加
    float flatSum = modifiers.Where(m => m.Op == ModOp.FlatAdd)
                             .Sum(m => m.Value);
    result += flatSum;

    // Phase 3: 百分比加法合并后乘
    float pctSum = modifiers.Where(m => m.Op == ModOp.PctAdd)
                            .Sum(m => m.Value);
    result *= (1f + pctSum);

    // Phase 4: 乘法逐个乘
    foreach (var mod in modifiers.Where(m => m.Op == ModOp.Multiply)
                                 .OrderBy(m => m.Priority))
        result *= mod.Value;

    // Phase 5-6: Override + Clamp
    var ov = modifiers.Where(m => m.Op == ModOp.Override)
                      .OrderByDescending(m => m.Priority).FirstOrDefault();
    if (ov != null) result = ov.Value;
    return Mathf.Clamp(result, _minValue, _maxValue);
}
```

无论 Buff 以什么顺序添加，聚合结果只取决于当前活跃的 Modifier 集合和它们的阶段归属。

---

## 快照类 Modifier 的正确处理

错误做法 — 快照整个属性的当前值：

```csharp
float currentArmor = target.GetAttribute(AttributeType.Armor);
float contribution = currentArmor * 0.3f; // 包含了其他 Modifier 的贡献
```

正确做法 — 只快照基础值或排除自身：

```csharp
float snapshotBase = target.GetBaseAttribute(AttributeType.Armor);
float contribution = snapshotBase * 0.3f;
```

为什么不能快照最终值：最终值包含其他实时 Modifier 的贡献。快照最终值等于把别人的当前值冻结进自己的贡献里。当那些 Modifier 下一帧更新，旧值残留在快照里，新值又加入聚合链 — 一份贡献被算了两次。

---

## 修法一：治标（热修复）

线上事故时不可能等三周改架构。热修复做了三件事：

**MaxStack 限制** — 所有缺少上限的 Buff 配置统一补上合理值。

**属性 Clamp** — 属性系统层面加硬上限，即使聚合逻辑有问题也兜底住：

```csharp
public static readonly Dictionary<AttributeType, (float min, float max)> Clamps = new()
{
    { AttributeType.Armor, (0f, 2000f) },
    { AttributeType.Attack, (0f, 5000f) },
    { AttributeType.MoveSpeed, (0f, 15f) },
};
```

**IronSkin 快照改为取 Base** — 不再快照当前值，临时改为快照基础值。

这三条改动 2 小时内上线，护甲溢出不再出现。但聚合顺序的底层问题还在。

---

## 修法二：治本（架构重构）

热修复之后用三周完成了以下改动。

### Modifier 聚合改为分阶段模型

`_modifiers` 不再按添加顺序排列，改为按阶段分桶：

```csharp
public class AttributeAggregator
{
    private readonly Dictionary<ModPhase, SortedList<int, Modifier>> _phases = new();

    public void AddModifier(Modifier mod)
    {
        if (!_phases.ContainsKey(mod.Phase))
            _phases[mod.Phase] = new SortedList<int, Modifier>();
        _phases[mod.Phase].Add(mod.Priority, mod);
        MarkDirty();
    }
}
```

### 快照策略显式化

新增 `SnapshotPolicy` 枚举，Buff 配置里必须明确指定：

```csharp
public enum SnapshotPolicy
{
    None,                // 实时重算
    SnapshotBase,        // 快照基础值
    SnapshotExcludeSelf, // 快照时排除自身 Source
}
```

### 每次聚合从 Base 重算

不在旧值上累加，每次从头算：

```csharp
public float RecalculateAttribute(AttributeType type)
{
    float baseValue = GetBaseAttribute(type);
    float result = _aggregator.Aggregate(baseValue);
    SetCurrentAttribute(type, result);
    return result;
}
```

即使 Modifier 集合发生变化，结果只取决于当前活跃集合，不依赖历史中间状态。

---

## 防回归：自动化测试

修完后补了一组针对 Buff 聚合的回归测试，核心用例：

```csharp
[Test] // 单 Buff 叠加到上限
public void SingleBuff_StackToMax_ValueCorrect()
{
    var entity = CreateEntity(baseArmor: 200);
    for (int i = 0; i < 15; i++) // 故意超过上限
        entity.ApplyBuff(CreateBuff("ArmorStack", perStack: 30, maxStack: 10));

    Assert.AreEqual(10, entity.GetBuffStackCount("ArmorStack"));
    Assert.AreEqual(500f, entity.GetAttribute(AttributeType.Armor));
}

[Test] // 加法 + 乘法交替施加，结果不依赖顺序
public void InterleavedApply_OrderIndependent()
{
    var entity = CreateEntity(baseArmor: 200);
    for (int i = 0; i < 20; i++)
    {
        if (i % 2 == 0) entity.ApplyBuff(CreateBuff("Flat", flatAdd: 50));
        else entity.ApplyBuff(CreateBuff("Mul", multiply: 1.5f));
    }
    float expected = CalculateExpectedPhased(entity);
    Assert.AreEqual(expected, entity.GetAttribute(AttributeType.Armor), 0.01f);
}

[Test] // 快照 Buff 不包含实时 Buff 的贡献
public void SnapshotBuff_DoesNotInclude_RealtimeBuff()
{
    var entity = CreateEntity(baseArmor: 200);
    entity.ApplyBuff(CreateBuff("Realtime", flatAdd: 100));
    entity.ApplyBuff(CreateBuff("IronSkin", snapshotBaseMultiplier: 0.3f,
        snapshotPolicy: SnapshotPolicy.SnapshotBase));
    // 快照基于 Base(200)，贡献 60，总计 200+100+60=360
    Assert.AreEqual(360f, entity.GetAttribute(AttributeType.Armor), 0.01f);
}

[Test] // 所有 Buff 移除后回到 Base
public void AllBuffsRemoved_ReturnsToBase()
{
    var entity = CreateEntity(baseArmor: 200);
    entity.ApplyBuff(CreateBuff("A", flatAdd: 50));
    entity.ApplyBuff(CreateBuff("B", multiply: 1.5f));
    entity.RemoveAllBuffs();
    Assert.AreEqual(200f, entity.GetAttribute(AttributeType.Armor), 0.01f);
}

[Test] // 快照 Buff 反复刷新不膨胀
public void SnapshotBuff_RepeatedRefresh_NoInflation()
{
    var entity = CreateEntity(baseArmor: 200);
    var buff = CreateBuff("IronSkin", snapshotBaseMultiplier: 0.3f,
        snapshotPolicy: SnapshotPolicy.SnapshotBase);
    entity.ApplyBuff(buff);
    float first = entity.GetAttribute(AttributeType.Armor);
    for (int i = 0; i < 100; i++)
    {
        entity.ApplyBuff(buff);
        Assert.AreEqual(first, entity.GetAttribute(AttributeType.Armor), 0.01f);
    }
}
```

这组测试验证的不是具体数值，而是三条不变量：

1. **顺序无关性** — 同样的 Modifier 集合，无论添加顺序如何，聚合结果相同
2. **移除完整性** — 所有 Buff 移除后，属性回到 Base，不留残余
3. **快照稳定性** — 快照 Buff 反复刷新，属性值不膨胀

只要这三条被打破，CI 就会红。

---

## 反思

### 做对了

线上发现后 2 小时内热修复了上限和 Clamp，止住了出血。

没有只修上限就收工，继续深挖了聚合链，找到了真正根因。

修完后补了自动化回归，把三条不变量固化成测试。

### 做错了

Modifier 聚合从项目第一天起就是一个 foreach 循环，没有人定义分阶段模型。大家默认"加法乘法的顺序无所谓"，直到它有所谓了。

快照和实时重算的混用没有文档化约定。哪些 Buff 是快照类型、快照的是什么值，全靠策划自己理解。没有统一的 `SnapshotPolicy` 枚举，也没有代码层面的约束。

`MaxStack = -1` 作为合法值长期存在，没有配置校验拦截。一条 CI 阶段的配置检查就能在上线前拦住它。

### 教训

Buff 系统的架构层问题，比单个 Buff 的配置问题危险 10 倍。

配置问题是局部的：一个 Buff 的上限没填，影响的是这一个 Buff。

架构问题是全局的：聚合顺序依赖添加顺序，影响的是所有 Buff 的所有组合。

而且架构问题有一个特征：它在简单场景下不暴露。单个 Buff、单种类型的 Modifier、固定的施加顺序 — 这些测试全能过。问题只在组合膨胀时才出现，而组合膨胀恰好是线上环境的常态。

---

## 这篇真正想留下来的结论

当你看到一个 Buff 系统出了数值问题，第一个要检查的不是配置表，而是聚合函数。

打开聚合函数，看三件事：

1. Modifier 是按阶段聚合还是按添加顺序聚合
2. 快照类 Modifier 快照的是什么 — Base？当前值？排除自身后的值？
3. 每次聚合是从 Base 重算还是在旧值上累加

如果这三个问题的答案不是确定的，那这个 Buff 系统迟早会失控。不是"可能"，是"迟早"。因为策划会不断加新 Buff，新 Buff 会产生新组合，新组合会触发你没想到的聚合路径。

唯一的防线是让聚合规则本身是确定性的，不依赖任何运行时状态。
