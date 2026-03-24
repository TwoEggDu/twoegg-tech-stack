---
title: "软件工程基础 10｜DRY / KISS / YAGNI：与 SOLID 互补的三条辅助原则"
description: "DRY（不重复）、KISS（保持简单）、YAGNI（不需要的东西不要加）是 SOLID 的重要补充。它们防止两个相反方向的错误：代码重复，以及过度设计。"
slug: "solid-sw-10-dry-kiss-yagni"
weight: 719
tags:
  - 软件工程
  - DRY
  - KISS
  - YAGNI
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> SOLID 主要防止"设计太差"的问题——耦合过高、职责混乱、扩展困难。
> 但还有另一个方向的错误：**过度设计**——抽象层太多、提前为不存在的需求设计扩展点、写了大量用不上的代码。
>
> DRY、KISS、YAGNI 三条原则从另一个方向约束代码质量，和 SOLID 合在一起才是完整的。

---

## DRY：Don't Repeat Yourself

**原则**：系统里的每一个知识片段，应该有唯一的、权威的、明确的表示。

不要重复代码是 DRY 的表象，DRY 真正说的是：**每一个"决定"（规则、逻辑、数据）在系统里只存在一份**。

### 代码重复的类型

**Type-1 重复：直接复制粘贴**

```csharp
// 玩家受伤逻辑
void PlayerTakeDamage(int damage)
{
    int finalDamage = Mathf.Max(0, damage - playerDefense);
    playerHP -= finalDamage;
    if (playerHP <= 0) PlayerDie();
    UpdatePlayerHPBar();
}

// 敌人受伤逻辑——和玩家的逻辑几乎一样
void EnemyTakeDamage(int damage)
{
    int finalDamage = Mathf.Max(0, damage - enemyDefense);
    enemyHP -= finalDamage;
    if (enemyHP <= 0) EnemyDie();
    UpdateEnemyHPBar();
}
```

当防御力计算逻辑需要修改时（比如加上了"破甲"效果），需要在两个地方修改，容易漏改。

修复：提取到一个 `TakeDamage(int damage, ref int hp, int defense, Action onDie, Action updateUI)` 函数，或者用接口。

**Type-2 重复：相同逻辑分散在多处**

更隐蔽的重复——不是字面上的复制粘贴，而是同一个业务规则被用不同的代码实现了多遍：

```csharp
// 检查玩家是否能施放技能：散落在三个地方
// 地方 1：UI 按钮的显示逻辑
skillButton.interactable = player.currentMana >= skill.manaCost &&
                           !player.isSilenced &&
                           skill.cooldown <= 0;

// 地方 2：技能施放验证
void TryCastSkill(Skill skill)
{
    if (player.currentMana < skill.manaCost) { ShowToast("法力不足"); return; }
    if (player.isSilenced) { ShowToast("沉默中"); return; }
    if (skill.cooldown > 0) { ShowToast("冷却中"); return; }
    CastSkill(skill);
}

// 地方 3：AI 技能决策
bool AIShouldUseSkill(Skill skill)
{
    return aiUnit.currentMana >= skill.manaCost &&
           !aiUnit.isSilenced &&
           skill.cooldown <= 0;
}
```

"能否施放技能"的规则在三个地方都有定义。如果规则增加一个新条件（比如"眩晕中不能施法"），需要在三个地方同时修改。

修复：提取为一个权威的函数：

```csharp
// 规则只有一份
public bool CanCastSkill(Unit caster, Skill skill)
{
    if (caster.CurrentMana < skill.ManaCost) return false;
    if (caster.HasStatus(StatusEffect.Silence)) return false;
    if (caster.HasStatus(StatusEffect.Stun)) return false; // 新增条件，只改这里
    if (skill.CurrentCooldown > 0) return false;
    return true;
}

// 所有地方都调用这一个函数
skillButton.interactable = CanCastSkill(player, skill);
// TryCastSkill 里也调用
// AI 决策里也调用
```

**Type-3 重复：配置数据重复**

```csharp
// 敌人的名字在两个地方定义了
public enum EnemyType { Goblin, Orc, Dragon }

// 显示名字时再写一遍
string GetEnemyDisplayName(EnemyType type)
{
    switch (type) {
        case EnemyType.Goblin: return "哥布林";
        case EnemyType.Orc: return "兽人";
        case EnemyType.Dragon: return "巨龙";
        default: return "未知";
    }
}
```

加新敌人类型时，需要在两个地方同时加。

修复：数据驱动，把敌人类型和显示名一起放在配置资产里。

### DRY 的度量标准

**三次规则（Rule of Three）**：同样的代码出现两次，可以忍受。出现三次，必须提取抽象。

这个规则防止了"为了 DRY 而过度提前抽象"的问题——只有当代码确实出现了第三次，你才真正知道它们是同一类问题，值得提取。

---

## KISS：Keep It Simple, Stupid

**原则**：大多数系统在保持简单时效果最好，而不是变得复杂。

KISS 不是说"不要写复杂的功能"，而是说"**用最简单的能解决问题的方案**"。

### 不必要的复杂性

```csharp
// 过于复杂的版本：用了大量设计模式，但实际上只是"随机选一首音乐"
public class MusicSelectionStrategy
{
    private IRandomizationAlgorithm randomAlgorithm;
    private MusicDatabase database;
    private MusicFilterChain filterChain;
    private MusicWeightCalculator weightCalculator;

    public IMusicTrack Select(MusicSelectionContext context)
    {
        var candidates = database.Query(context.Criteria);
        var filtered = filterChain.Process(candidates);
        var weights = weightCalculator.CalculateWeights(filtered, context);
        return randomAlgorithm.SelectWeighted(filtered, weights);
    }
}

// 简单的版本：直接满足需求
public class BackgroundMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] musicTracks;

    public void PlayRandom()
    {
        int index = Random.Range(0, musicTracks.Length);
        audioSource.clip = musicTracks[index];
        audioSource.Play();
    }
}
```

如果项目没有"权重随机"、"根据场景过滤"等实际需求，复杂版本就是 KISS 的违反。

### 游戏中常见的不必要复杂性

**过度的抽象层**：

```csharp
// 为了"以后可能扩展"，加了很多永远不会用到的抽象
public interface IScoreCalculator { int Calculate(GameResult result); }
public abstract class BaseScoreCalculator : IScoreCalculator { }
public class DefaultScoreCalculator : BaseScoreCalculator { }
public class ScoreCalculatorFactory { }
public class ScoreCalculatorRegistry { }

// 实际需求：分数 = 击杀数 × 100 + 时间奖励
// 这里只需要一个函数
public static int CalculateScore(int kills, float timeLeft)
    => kills * 100 + (int)(timeLeft * 10);
```

**过度的配置化**：

```csharp
// 把一切都做成可配置的，包括那些永远不会变的数
[CreateAssetMenu]
public class PhysicsConfig : ScriptableObject
{
    public float gravity = -9.81f;           // 这真的需要配置吗？
    public float airResistanceCoefficient;   // 项目里根本不用
    public float terminalVelocity;           // 项目里根本不用
}
```

### KISS 的判断标准

问自己：**如果把这部分删掉，还能实现需求吗？**

如果答案是"能"，那这部分就是不必要的复杂性，删掉它。

---

## YAGNI：You Aren't Gonna Need It

**原则**：在你真正需要某个功能之前，不要实现它。

YAGNI 是 KISS 的一个特定应用：专门针对"为了未来的可能需求提前编写的代码"。

### YAGNI 的典型违反

```csharp
// 策划只要了"玩家可以拾取物品"
// 程序员想到"以后可能有多人模式"，于是实现了：
public class InventorySystem
{
    private Dictionary<string, Dictionary<Item, int>> playerInventories = new();
    // 支持多个玩家的背包
    // 支持背包合并
    // 支持背包交易
    // 支持背包权限（哪些玩家能看到哪些背包）
    // ... 200行代码
}

// 实际上，这个游戏从头到尾都只有一个玩家
// 只需要：
private List<Item> inventory = new List<Item>();
```

这种"防御性开发"（defensive programming，为未来的需求提前设计）有两个问题：

1. 你花了时间写了一个你不确定会用到的功能
2. 多余的代码使系统更复杂，增加了维护负担和出 Bug 的风险

### YAGNI 的应用边界

YAGNI 不是说"永远不要考虑扩展性"。

**不应该 YAGNI 的情况**：已经通过 OCP 等原则设计好扩展点——这不是"提前添加功能"，而是"设计好接缝让未来的扩展变得容易"。

```csharp
// 正确：设计好接缝（扩展点），但不提前实现
public abstract class SkillBase : ScriptableObject
{
    public abstract void Execute(Character caster, Character target);
    // 这是扩展接缝，不是提前实现的功能
}

// 错误的 YAGNI 违反：直接加了一个"以后可能需要"的技能
[CreateAssetMenu]
public class TimeStopSkill : SkillBase // 现在的项目里完全没有这个需求
{
    public override void Execute(Character caster, Character target)
    {
        // 停止时间... 整套系统都还没有时间系统
    }
}
```

区别：**设计扩展接缝**（OCP，对扩展开放）是正确的。**在没有需求时提前实现具体功能**是 YAGNI 的违反。

---

## 三条原则的配合

这三条原则一起防止两个相反方向的错误：

**DRY** 防止：逻辑重复，修改时需要在多处同步。

**KISS** 防止：无谓复杂，系统比解决问题所需要的更复杂。

**YAGNI** 防止：提前过度开发，为不确定的未来需求写了大量不用的代码。

它们也需要和 SOLID 保持张力：

- DRY + SRP：提取重复代码时，要确保提取出来的类/函数有清晰的单一职责，不要为了 DRY 而把不相关的代码硬塞进同一个函数
- KISS + OCP：保持简单，但要在明确的变化点上设计好扩展接缝（不是矛盾，是在不同场景下的侧重）
- YAGNI + OCP：不要提前实现用不到的功能，但可以为已知的变化点设计好扩展接口

---

## 一个包含所有三条原则的游戏案例

```csharp
// 需求：计算玩家通关得分，显示在结算界面

// 错误版本 1（违反 DRY）：得分计算写了两遍
void ShowResultUI()
{
    int score = killCount * 100 + (int)(timeLeft * 10); // 第一遍
    resultPanel.SetScore(score);
}

void SaveHighScore()
{
    int score = killCount * 100 + (int)(timeLeft * 10); // 第二遍，重复了
    if (score > PlayerPrefs.GetInt("high_score"))
        PlayerPrefs.SetInt("high_score", score);
}
```

```csharp
// 错误版本 2（违反 KISS）：为了"以后可能有复杂计分规则"过度抽象
public interface IScoreComponent { int GetScore(); }
public class KillScoreComponent : IScoreComponent { ... }
public class TimeScoreComponent : IScoreComponent { ... }
public class ScoreCompositor { List<IScoreComponent> components; ... }
public class ScoreCalculatorFactory { ... }
// 三层抽象，实际上只是 kills * 100 + time * 10
```

```csharp
// 错误版本 3（违反 YAGNI）：提前做了多难度计分、多人排行
public int CalculateScore(int kills, float time, Difficulty difficulty, string[] playerIds)
{
    // 50行代码，支持各种复杂情况
    // 但当前版本只有单人、单难度
}
```

```csharp
// 正确版本：DRY（只一份计算逻辑）+ KISS（够简单）+ YAGNI（只有需要的）
// 同时符合 SRP（职责单一）
public class ScoreCalculator
{
    public int Calculate(int kills, float timeLeft)
        => kills * 100 + (int)(timeLeft * 10);
}

// 使用时
private ScoreCalculator scoreCalculator = new ScoreCalculator();

void OnLevelComplete()
{
    int score = scoreCalculator.Calculate(killCount, timeLeft); // 只算一次，到处复用
    resultPanel.SetScore(score);
    highScoreTracker.TryUpdate(score);
}
```

---

## 小结

| 原则 | 防止的错误 | 一句话 |
|---|---|---|
| DRY | 逻辑重复，修改需要多处同步 | 每个规则只有一份 |
| KISS | 无谓复杂，系统比需要的更难理解 | 用最简单的方案解决问题 |
| YAGNI | 提前为不确定的需求开发功能 | 不需要的东西现在不要加 |

这三条和 SOLID 合在一起，形成了一个完整的代码质量指导框架：SOLID 让代码面对变化时有弹性，DRY/KISS/YAGNI 让代码在没有变化时保持精简。
