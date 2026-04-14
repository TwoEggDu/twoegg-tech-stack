---
date: "2026-04-14"
title: "技能系统深度 31｜数值平衡框架：伤害公式设计、数值曲线与平衡验证方法论"
description: "数值不是'拍脑袋填数'，而是一套可验证的工程问题。这篇讲伤害公式的设计空间、等级缩放的数值模型、TTK 作为平衡指标、'改一个数全盘崩'的根因，以及自动化平衡验证的工程方法。"
slug: "skill-system-31-balance-framework"
weight: 8031
tags:
  - Gameplay
  - Skill System
  - Balance
  - Formula
  - Numerical Design
series: "技能系统深度"
series_order: 31
---

> 数值平衡不是策划一个人的事。公式架构决定了数值能不能被验证、能不能被维护。

很多项目在原型期的数值是这样定的：

"这个怪 100 血，主角打一下掉 20，五下打死，差不多吧。"

原型期这么做完全没问题。10 个怪，3 个技能，手动试几遍就能调好。

但只要项目进入以下阶段，这套做法就会崩：

- 等级从 1 到 60，属性需要随等级缩放
- 装备系统上线，属性来源从 1 个变成 5 个
- Buff/Modifier 叠加使最终伤害变成多阶段计算
- PvP 模式开放，不同角色之间要求"公平"
- 策划改了某个 Buff 的数值，结果全盘 TTK 偏移 30%

到这一步你会发现：

`问题不是"某个数填错了"，而是"填数的框架本身不可维护"。`

这篇就是把这个框架立出来。

---

## 这篇要回答什么

1. 伤害公式有哪几种设计空间，各自的 tradeoff 是什么。
2. 等级缩放的数值曲线怎么选择。
3. 为什么 DPS 不等于平衡，TTK 才是更好的指标。
4. "改一个数全盘崩"的结构性根因是什么。
5. 怎样用自动化手段做平衡验证。

---

## 伤害公式的设计空间

伤害公式不是只有一种写法。不同的公式形态会带来完全不同的数值行为。

### 纯加法模型

```
Damage = max(0, ATK - DEF)
```

防御力是绝对减伤——DEF 从伤害里直接扣除固定值。ATK 比 DEF 大多少就打多少，低于 DEF 则伤害归零。

适用场景：数值范围小、等级差距不大的游戏。早期 RPG 和回合制大量使用。

问题很明显：等级碾压严重——高等级一刀秒低等级，反过来完全打不动。防御力没有收益递减，堆到和攻击力一样就变成不死。不适合 PvP。

### 百分比减伤模型

```
Damage = ATK × (1 - DEF / (DEF + K))
```

K 是一个常数，通常和等级挂钩。以 K = 100 为例：

| DEF | 减伤比例 | 含义 |
|-----|---------|------|
| 0   | 0%      | 完全不减伤 |
| 50  | 33%     | 三分之一减伤 |
| 100 | 50%     | 减半 |
| 200 | 67%     | 三分之二减伤 |
| 500 | 83%     | 接近上限但永远达不到 100% |

核心特性：防御力有收益递减，永远不会完全免疫伤害，K 值可以随等级变化来控制不同等级段的减伤节奏。

这个模型在 League of Legends、Dota 2、大量 MMO 中广泛使用。K 的取值通常在 50-300 之间，具体取决于期望的满级减伤率。

### 层级 Modifier 模型

当伤害涉及暴击、元素加成、技能倍率、Buff 加成时，需要分层计算：

```
Final = Base × SkillMultiplier
     × (1 + CritBonus + ElementBonus)   ← Additive 阶段：先求和再乘
     × MultBuff_1 × MultBuff_2          ← Multiplicative 阶段：逐个乘
     × (1 - EnemyReduction)             ← 防御减伤
```

关键设计决策：

- **Additive 和 Multiplicative 分开**。同一阶段内的加成先求和再乘，避免多个小加成相乘产生意外的指数增长。
- **哪些加成放 Additive、哪些放 Multiplicative，在架构层定死**。如果策划可以随意选择某个 Buff 是加法还是乘法，后期一定会出现[第 35 篇]({{< relref "system-design/skill-system-35-case-buff-system-meltdown.md" >}})里描述的聚合失控。
- **保底伤害**。最终结果 Clamp 到至少 1，防止极端减伤组合下打出 0。

### 选择依据

三种模型不是演进关系，而是适用场景不同：

| 模型 | 适合 | 不适合 |
|------|------|--------|
| 纯加法 | 数值范围小、无 PvP、回合制 | 等级跨度大、需要 PvP 平衡 |
| 百分比减伤 | 大部分 ARPG、MOBA、MMO | 极简休闲 |
| 层级 Modifier | 多属性来源、深度构建系统 | 原型期或轻量战斗 |

实际项目通常以百分比减伤为核心，加上层级 Modifier 处理 Buff 和构建加成。纯加法很少单独使用，但会作为层级 Modifier 中某个阶段的内部逻辑出现。

---

## 数值曲线

公式决定了"怎么算"，曲线决定了"数值随等级怎么涨"。

### 线性曲线

```
ATK(level) = baseATK + growthPerLevel × level
```

每升一级涨的量相同。适用于等级跨度小（1-20 级）的游戏。问题是等级跨度大时，高等级段的固定提升在总量中占比太小，玩家感知不到升级的意义。

### 指数曲线

```
ATK(level) = baseATK × growthRate ^ level
```

每升一级涨的百分比相同。growthRate = 1.05 表示每级涨 5%。适用于需要"后期和前期不在同一量级"的 Diablo 类刷子游戏。

问题是数值膨胀。60 级时 ATK = 100 × 1.05^60 = 1868。如果 growthRate 取 1.1，60 级 ATK = 30448。策划很快失去对绝对值的感知。

### S 曲线

前期增长慢，中期增长快，后期趋近上限。适用于数值有明确上限、后期角色差异靠构建而非碾压的游戏。

实际使用时通常不用 Sigmoid 公式，而是用分段线性模拟：

```csharp
public static float LevelCurve(int level, LevelCurveConfig cfg)
{
    if (level <= cfg.earlyEnd)  // 1-10 级
        return cfg.baseValue + cfg.earlyGrowth * level;
    if (level <= cfg.midEnd)    // 11-40 级
        return cfg.earlyMax + cfg.midGrowth * (level - cfg.earlyEnd);
    return cfg.midMax + cfg.lateGrowth * (level - cfg.midEnd); // 41-60 级
}
```

分段线性比 Sigmoid 更容易让策划理解和控制：每一段的斜率都是一个明确的数字。

### 装备和 Buff 怎样与基础曲线叠加

基础曲线定义角色"裸体"的属性。装备和 Buff 是额外来源。

**加法叠加**：装备 +50 ATK。简单可控，但后期装备数值也需要跟着曲线涨，否则装备变无感。

**百分比叠加**：装备 +10% ATK。自动随等级缩放，但多件装备的百分比如果是乘法关系（1.1 × 1.1 × 1.1 = 1.33），膨胀速度比看上去快。

推荐做法和[第 08 篇]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})的 Modifier 分阶段模型一致：

```text
最终 ATK = (基础ATK + 所有Flat加成之和) × (1 + 所有Pct加成之和) × 逐个乘法Modifier
```

同一类型的加成先合并再运算，避免顺序依赖。

### 数值膨胀的征兆和控制

当项目出现这些现象，说明数值已经开始膨胀：

1. **伤害数字从四位数变成六位数**。两个版本之间涨了两个数量级，策划开始"砍一刀"。
2. **新装备属性必须越来越高才有感知**。上个版本 +100 ATK 是好装备，这个版本 +500 才算好。
3. **低等级内容对高等级玩家完全没有挑战**。等级之间的数值差距太大。

控制方法：

- 用百分比而非绝对值思考增长。"每级涨 5%"比"每级涨 100 点"在后期更可控。
- 等级同步（Level Sync）：高等级玩家进入低等级区域时属性压缩。
- 硬上限：每个属性有最大值，无论多少来源叠加都不超过。

---

## TTK 作为平衡指标

DPS 是策划最常用的平衡指标。但 DPS 相同的两个角色，体验可以完全不同。

### DPS 相同，体验不同

角色 A：DPS = 100（每秒打 10 下，每下 10 伤害）
角色 B：DPS = 100（每 2 秒打 1 下，每下 200 伤害）

纸面 DPS 相同。但在实战中：

- A 打有护盾的目标更好（持续输出快速削护盾）
- B 在短交火中更强（一发就是 200，对方来不及反应）
- 如果目标有"每次受击减伤 10 点"的效果，A 的有效 DPS 只有 0，B 的有效 DPS 是 95

所以光看 DPS 不够。更实际的指标是 TTK——Time to Kill，击杀一个标准目标的时间。

### TTK 的计算

基础版 `TTK = TargetHP / DPS` 太粗糙。真实战斗中 TTK 受很多因素影响：

```csharp
public static float EstimateTTK(TTKContext ctx)
{
    float effectiveDPS = ctx.baseDPS
        * ctx.hitRate                                   // 命中率
        * (1f + ctx.critRate * ctx.critMultiplier)       // 暴击贡献
        * ctx.uptimeRatio;                               // 输出占空比

    float effectiveHP = ctx.targetHP + ctx.targetShield
        + ctx.targetHealPerSec * ctx.combatDuration;     // 治疗贡献

    return effectiveDPS > 0f ? effectiveHP / effectiveDPS : float.MaxValue;
}
```

`uptimeRatio` 是最容易被忽略的参数。近战角色的 uptimeRatio 通常在 0.4-0.6（大量时间在追目标），远程角色在 0.7-0.9。不考虑这个，近战角色的纸面 DPS 再高也没意义。

### 不同类型游戏的 TTK 参考

| 游戏类型 | TTK 参考 | 设计意图 |
|----------|---------|---------|
| FPS（CoD 类） | 0.2s - 0.5s | 反应和瞄准决定胜负 |
| FPS（Halo 类） | 1.0s - 2.0s | 给对手反应和反打机会 |
| MOBA | 2s - 5s（对线），0.5s - 1.5s（团战 Burst） | 前期慢，后期爆发 |
| MMO PvP | 5s - 15s | 技能循环和资源管理 |
| MMO PvE Boss | 120s - 300s | 团队配合和持续输出 |

当 TTK 偏离目标范围时，调的不应该是某个角色的 ATK，而应该回去看公式里哪个阶段的系数出了问题。

---

## "改了一个数全盘崩"的根因

几乎每个项目都经历过这种场景：策划把某个 Buff 的加成从 20% 改成 25%，结果 PvP 前三名全变成同一个角色。

这不是策划的问题。是公式结构的问题。

### Modifier 组合爆炸

[第 08 篇]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})里讲过 Modifier 的分阶段聚合。但即使聚合逻辑正确，多个 Modifier 的组合效应仍然是非线性的。

```text
Base ATK: 100
Buff A: +30% (Additive)    → 单独贡献 +30
Buff C: ×1.5 (Multiplicative) → 单独贡献 +50

A + C 组合: 100 × 1.3 × 1.5 = 195 → 组合贡献 +95，大于 30 + 50 = 80
```

每个 Multiplicative 层都会放大 Additive 层的总量。Additive 加成越多，Multiplicative 的收益越高。反过来也一样。

当系统里有 5 个 Additive 加成和 3 个 Multiplicative 加成时，改动其中任何一个都会改变所有其他 Modifier 的"有效贡献"。策划在表格里看的是"单个 Buff 的数值"，但实际影响的是整个组合空间。

### 间接依赖链

比组合爆炸更隐蔽的是间接依赖。

```text
Buff X: 攻速 +30%
→ 每秒攻击次数增加 → DPS 提升
  → 吸血速度提升 → 生存能力提升
    → 可以站撸不走位 → uptimeRatio 提升
      → 有效 DPS 进一步提升
```

策划改的是"攻速 +30%"，但实际影响链条的最终放大倍数远超 30%。

这类间接依赖在属性系统里随处可见：

- 暴击率影响吸血效率（暴击吸血更多 → 生存更强）
- 移速影响 uptimeRatio（跑得快追得上 → 输出时间更长）
- 减 CD 影响技能频率（技能放得多 → 某些"每次施法叠一层"的 Buff 叠得更快）

### 和前序篇目的关系

[第 08 篇]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})解决的是"单次聚合怎么算对"——分阶段、顺序无关、快照隔离。[第 35 篇]({{< relref "system-design/skill-system-35-case-buff-system-meltdown.md" >}})展示的是"聚合规则没定好时会怎么爆"——Buff 叠到 99 层、快照和实时重算混用导致指数增长。

这一篇讲的是更上层的问题：即使单次聚合完全正确，多个 Modifier 之间的组合效应和间接依赖链仍然会让"改一个数全盘崩"。解决方案不在公式层面，而在验证层面。

---

## 自动化平衡验证

人工验证数值平衡不可扩展。30 个角色、50 个装备、80 个 Buff，可能的组合是天文数字。策划不可能一个个试。

### 模拟对局

让 AI 控制的角色自动对打，收集结果。不需要渲染、动画、物理，只跑数值逻辑。一次模拟在毫秒级完成，可以批量跑数千场。

```csharp
public class HeadlessBattleSim
{
    public SimResult Run(SimConfig config)
    {
        var attacker = CreateEntity(config.attackerProfile);
        var defender = CreateEntity(config.defenderProfile);
        float elapsed = 0f;

        while (elapsed < config.maxDuration && !attacker.IsDead && !defender.IsDead)
        {
            attacker.AI.ExecuteRotation();
            defender.AI.ExecuteRotation();
            elapsed += config.tickInterval;
            _battleSystem.Tick(config.tickInterval);
        }

        return new SimResult { ttk = elapsed, winner = attacker.IsDead ? Side.Defender : Side.Attacker };
    }
}
```

### 参数扫描

对关键参数做网格扫描，观察输出的变化趋势。典型做法是遍历 ATK 等级 × DEF 等级的全组合，计算每个点的伤害和 TTK，输出热力图或 CSV。

参数扫描能快速暴露：

- 某个等级段 TTK 突然跳变（曲线分段不连续）
- 同等级对打时某个职业 TTK 远低于其他职业
- 高等级打低等级时伤害溢出（一刀秒但打了十倍 HP 的伤害）

### 异常值检测

对模拟结果设定边界，自动标记异常：

| 指标 | 正常范围 | 告警阈值 |
|------|---------|---------|
| 同等级 PvP TTK | 2s - 10s | < 0.5s 或 > 30s |
| 同等级 PvE TTK | 3s - 8s（小怪） | < 0.1s 或 > 60s |
| 角色胜率 | 45% - 55% | > 60% 或 < 40% |
| 伤害方差系数 | < 0.3 | > 0.5（暴击依赖过重） |

TTK < 0.1s 意味着一击秒杀，TTK > 60s 意味着打不动。任何一种都是公式或曲线出了问题。

### CI 集成

数值表通常存在 Excel 或 JSON 里。每次策划改了数值表并提交，CI 自动跑平衡测试：

1. 策划改了某个 Buff 的数值，提交 PR
2. CI 自动跑模拟对局 + 参数扫描
3. 如果有告警（某角色胜率超标、某 TTK 低于阈值），PR 标红
4. 策划看告警报告，确认是预期变化还是意外失控
5. 通过后合并

这套流程不是替代策划的判断，而是帮策划在改数之前就看到改动的影响范围。

---

## 这篇的结论

数值平衡是一个工程问题，不是感觉问题。

1. **选对公式**。百分比减伤 + 层级 Modifier 适合大部分项目。纯加法只在数值范围极小时可用。哪些加成是 Additive、哪些是 Multiplicative，在架构层定死，不给策划自行选择的空间。

2. **选对曲线**。线性适合短等级跨度，指数适合刷子游戏但要做好膨胀控制，S 曲线（或分段线性）适合需要数值上限的项目。装备和 Buff 的叠加方式要和基础曲线一起设计，不能独立决定。

3. **用 TTK 而不是 DPS 做平衡指标**。TTK 包含了命中率、暴击、护盾、治疗、uptimeRatio 等 DPS 忽略的因素。不同品类的 TTK 目标不同，先定 TTK 范围再反推公式参数。

4. **理解"改一个数全盘崩"的根因**。不是策划粗心，是 Modifier 之间的非线性交互和间接依赖链。Multiplicative 层放大 Additive 层的总量，间接依赖（攻速 → 吸血 → 生存 → uptimeRatio）放大单个属性的实际影响。

5. **自动化验证是防线**。模拟对局跑 TTK 分布、参数扫描画热力图、异常值检测设阈值、CI 集成拦截风险 PR。策划改数之前就看到影响范围，而不是上线之后被玩家发现。
