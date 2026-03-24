---
title: "软件工程基础 08｜五条原则如何协同：一个从违反到修正的完整重构案例"
description: "用一个真实的战斗系统，展示 SOLID 五条原则在同一个代码库里如何相互配合、共同发力——每条原则解决什么问题，修复一条之后如何自然地引出下一条。"
slug: "solid-sw-08-solid-in-practice"
weight: 715
tags:
  - 软件工程
  - SOLID
  - 重构
  - 代码质量
  - 战斗系统
series: "软件工程基础与 SOLID 原则"
---

> 单独讲每条 SOLID 原则比较抽象。这篇把五条原则放在同一个问题里——一个真实的战斗系统，从违反所有原则的初始版本，一步步重构到符合 SOLID 的最终版本。
>
> 每一步重构都对应一条原则，你可以看到它们是怎么互相配合的。

---

## 起点：一个典型的问题代码

这是一个游戏早期版本的战斗系统，它能跑，但存在各种各样的问题：

```csharp
// v1 版本：能运行，但是技术债的温床
public class BattleSystem : MonoBehaviour
{
    public GameObject player;
    public GameObject currentEnemy;

    // 包罗万象的战斗处理函数
    public void ExecutePlayerAttack(string attackType)
    {
        // --- 取数据（低层细节混在战斗逻辑里）---
        int playerAtk = player.GetComponent<PlayerStats>().attackPower;
        int enemyDef  = currentEnemy.GetComponent<EnemyStats>().defense;

        // --- 伤害计算（各种类型混在一起）---
        int damage = 0;
        if (attackType == "Normal")
        {
            damage = Mathf.Max(0, playerAtk - enemyDef);
        }
        else if (attackType == "Heavy")
        {
            damage = Mathf.Max(0, playerAtk * 2 - enemyDef);
            // 重击有击退效果
            currentEnemy.transform.position += Vector3.back * 2f;
        }
        else if (attackType == "Magic")
        {
            int playerMagic = player.GetComponent<PlayerStats>().magicPower;
            int enemyMagicRes = currentEnemy.GetComponent<EnemyStats>().magicResistance;
            damage = Mathf.Max(0, playerMagic * 3 - enemyMagicRes);
            // 魔法攻击可能造成燃烧状态
            if (Random.value < 0.3f)
                currentEnemy.GetComponent<EnemyStats>().isBurning = true;
        }
        else if (attackType == "Heal")
        {
            // "攻击"函数里处理治疗... 职责混乱
            int playerMagic = player.GetComponent<PlayerStats>().magicPower;
            int healAmount = playerMagic * 2;
            player.GetComponent<PlayerStats>().hp = Mathf.Min(
                player.GetComponent<PlayerStats>().maxHp,
                player.GetComponent<PlayerStats>().hp + healAmount
            );
            // 治疗不需要后面的受伤流程，直接返回
            UpdateHPBars();
            return;
        }

        // --- 扣血 ---
        currentEnemy.GetComponent<EnemyStats>().hp -= damage;

        // --- 死亡判断 ---
        if (currentEnemy.GetComponent<EnemyStats>().hp <= 0)
        {
            // 给经验
            player.GetComponent<PlayerStats>().exp += currentEnemy.GetComponent<EnemyStats>().expReward;
            // 播放死亡动画
            currentEnemy.GetComponent<Animator>().SetTrigger("Die");
            // 存一下
            PlayerPrefs.SetInt("player_exp", player.GetComponent<PlayerStats>().exp);
            // 延迟销毁
            Destroy(currentEnemy, 1f);
            currentEnemy = null;
        }

        // --- UI 更新 ---
        UpdateHPBars();

        // --- 检查成就 ---
        int totalKills = PlayerPrefs.GetInt("total_kills", 0) + 1;
        PlayerPrefs.SetInt("total_kills", totalKills);
        if (totalKills == 100)
        {
            // 直接在这里更新 UI 显示成就
            GameObject.Find("AchievementPanel")
                      .GetComponent<AchievementPanel>()
                      .Show("百人斩");
        }
    }

    private void UpdateHPBars()
    {
        // 直接查找 UI 对象
        GameObject.Find("PlayerHPBar").GetComponent<HPBar>()
                  .SetValue(player.GetComponent<PlayerStats>().hp,
                            player.GetComponent<PlayerStats>().maxHp);
        if (currentEnemy != null)
            GameObject.Find("EnemyHPBar").GetComponent<HPBar>()
                      .SetValue(currentEnemy.GetComponent<EnemyStats>().hp,
                                currentEnemy.GetComponent<EnemyStats>().maxHp);
    }
}
```

**这段代码违反了什么？** 几乎是 SOLID 全线崩溃：

| 原则 | 违反情况 |
|---|---|
| SRP | `BattleSystem` 同时负责：伤害计算、状态效果、HP 管理、经验系统、存档、UI 更新、成就系统 |
| OCP | 每次加新攻击类型都要改 `ExecutePlayerAttack` 函数 |
| LSP | 治疗逻辑混在攻击逻辑里，通过 `return` 提前退出，行为不一致 |
| ISP | 所有东西都用具体类型（`PlayerStats`、`EnemyStats`），没有接口隔离 |
| DIP | 高层战斗逻辑直接依赖：`GetComponent<PlayerStats>()`、`GameObject.Find()`、`PlayerPrefs`、`Animator` |

---

## 第一步：应用 SRP——拆分职责

首先识别这个函数里有多少个不同的"变化理由"：

1. 伤害计算逻辑（数值策划关心）
2. 攻击类型定义（战斗策划关心）
3. 状态效果应用（系统程序关心）
4. HP 管理（系统程序关心）
5. 经验/升级（数值策划关心）
6. 存档（技术程序关心）
7. UI 更新（UI 程序/美术关心）
8. 成就系统（运营/策划关心）

把它们分开：

```csharp
// 战斗数据：只管数值
public class CombatStats
{
    public int HP { get; private set; }
    public int MaxHP { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public int MagicPower { get; }
    public int MagicResistance { get; }

    public event Action<int, int> OnHPChanged;
    public event Action OnDied;

    public void ModifyHP(int delta)
    {
        HP = Mathf.Clamp(HP + delta, 0, MaxHP);
        OnHPChanged?.Invoke(HP, MaxHP);
        if (HP == 0) OnDied?.Invoke();
    }
}

// 伤害计算器：只管计算最终伤害数值
public class DamageCalculator
{
    public int CalculatePhysical(int attack, int defense)
        => Mathf.Max(0, attack - defense);

    public int CalculateHeavy(int attack, int defense)
        => Mathf.Max(0, attack * 2 - defense);

    public int CalculateMagical(int magic, int magicResistance)
        => Mathf.Max(0, magic * 3 - magicResistance);
}

// 状态效果系统：只管 Buff/Debuff
public class StatusEffectSystem
{
    public void TryApplyBurning(CombatStats target, float chance)
    {
        if (Random.value < chance)
            target.ApplyBurning();
    }
}
```

---

## 第二步：应用 OCP——攻击类型对扩展开放

把 `if/else if` 的攻击类型判断，改成可扩展的策略：

```csharp
// 每种攻击是一个独立的实现
public abstract class AttackAction : ScriptableObject
{
    public string actionName;
    public abstract void Execute(CombatStats attacker, CombatStats target);
}

[CreateAssetMenu(menuName = "Combat/NormalAttack")]
public class NormalAttack : AttackAction
{
    private DamageCalculator calc = new();
    public override void Execute(CombatStats attacker, CombatStats target)
    {
        int damage = calc.CalculatePhysical(attacker.AttackPower, target.Defense);
        target.ModifyHP(-damage);
    }
}

[CreateAssetMenu(menuName = "Combat/HeavyAttack")]
public class HeavyAttack : AttackAction
{
    private DamageCalculator calc = new();
    public override void Execute(CombatStats attacker, CombatStats target)
    {
        int damage = calc.CalculateHeavy(attacker.AttackPower, target.Defense);
        target.ModifyHP(-damage);
        // 击退是 HeavyAttack 自己的责任
        ApplyKnockback(target);
    }
    void ApplyKnockback(CombatStats target) { /* 击退逻辑 */ }
}

[CreateAssetMenu(menuName = "Combat/MagicAttack")]
public class MagicAttack : AttackAction
{
    private DamageCalculator calc = new();
    [SerializeField] private float burnChance = 0.3f;
    public override void Execute(CombatStats attacker, CombatStats target)
    {
        int damage = calc.CalculateMagical(attacker.MagicPower, target.MagicResistance);
        target.ModifyHP(-damage);
        new StatusEffectSystem().TryApplyBurning(target, burnChance);
    }
}

// 治疗是一种 AttackAction 吗？不，这里用 LSP 来修正——见第三步
```

---

## 第三步：应用 LSP——治疗不是"攻击"

原代码里治疗混在攻击函数里，通过 `return` 提前退出——这就是 LSP 违反的信号：一个"攻击"的实现居然不遵循攻击的行为约定。

修正：治疗不是攻击类型，是一种独立的战斗行为。

```csharp
// 战斗行为的顶层抽象
public abstract class CombatAction : ScriptableObject
{
    public string actionName;
    public int manaCost;
    // 所有战斗行为都有：名字、法力消耗、执行方法
    public abstract void Execute(CombatStats user, CombatStats optionalTarget);
    public abstract bool NeedsTarget();
}

// 攻击行为：需要目标
public abstract class AttackAction : CombatAction
{
    public override bool NeedsTarget() => true;
}

// 支援行为：不一定需要目标
public abstract class SupportAction : CombatAction
{
    public override bool NeedsTarget() => false;
}

// 治疗：是 SupportAction 的子类，不是 AttackAction 的子类
[CreateAssetMenu(menuName = "Combat/HealAction")]
public class HealAction : SupportAction
{
    [SerializeField] private float healMultiplier = 2f;
    public override void Execute(CombatStats user, CombatStats _)
    {
        int healAmount = (int)(user.MagicPower * healMultiplier);
        user.ModifyHP(healAmount); // 给自己加血
    }
}
```

现在治疗和攻击在继承层级上就是分开的，不会再出现"攻击行为里提前 return"的奇怪现象。

---

## 第四步：应用 ISP——用接口精准描述能力

战斗系统需要知道"某个东西能受伤"、"某个东西能被选为目标"，但不需要知道它是玩家还是敌人。

```csharp
// 精准的小接口：能受伤
public interface IDamageable
{
    void TakeDamage(int amount);
    int CurrentHP { get; }
    int MaxHP { get; }
}

// 能被治疗
public interface IHealable
{
    void ReceiveHeal(int amount);
}

// 可以是战斗目标
public interface ICombatTarget : IDamageable
{
    Transform Transform { get; }
    bool IsAlive { get; }
}

// 战斗系统框架只依赖接口，不依赖具体类型
public class CombatActionExecutor
{
    public void Execute(CombatAction action, CombatStats user, ICombatTarget target)
    {
        if (user.CurrentMana < action.manaCost) return;
        user.ConsumeMana(action.manaCost);
        action.Execute(user.Stats, target?.Stats);
    }
}
```

---

## 第五步：应用 DIP——解除对具体实现的依赖

战斗系统现在还有问题：它直接调用了 `PlayerPrefs.SetInt`（存档）、`GameObject.Find`（UI）、成就系统具体类。

用 DIP 解除这些依赖：

```csharp
// 定义抽象接口——高层战斗系统只依赖这些
public interface IProgressTracker
{
    void OnEnemyDefeated(EnemyDefeatedContext context);
}

public interface ICombatUIProvider
{
    void UpdateHP(string targetId, int current, int max);
    void ShowDamageNumber(int damage, Vector3 worldPosition);
}

// 战斗系统：只依赖抽象，不依赖具体
public class CombatSystem : MonoBehaviour
{
    // 通过注入获得依赖
    [SerializeField] private CombatActionExecutor executor;
    private ICombatUIProvider uiProvider;
    private IProgressTracker progressTracker;

    // 依赖注入入口
    public void Initialize(ICombatUIProvider ui, IProgressTracker tracker)
    {
        this.uiProvider = ui;
        this.progressTracker = tracker;
    }

    public void PlayerAttack(CombatAction action, ICombatTarget target)
    {
        executor.Execute(action, playerStats, target);

        // 通过抽象接口通知 UI
        uiProvider.UpdateHP("enemy", target.CurrentHP, target.MaxHP);

        // 通过抽象接口通知进度追踪
        if (!target.IsAlive)
            progressTracker.OnEnemyDefeated(new EnemyDefeatedContext { target = target });
    }
}

// 具体实现：存档系统实现 IProgressTracker
public class SaveAndAchievementTracker : MonoBehaviour, IProgressTracker
{
    public void OnEnemyDefeated(EnemyDefeatedContext context)
    {
        int kills = PlayerPrefs.GetInt("total_kills", 0) + 1;
        PlayerPrefs.SetInt("total_kills", kills);
        CheckAchievements(kills);
    }

    void CheckAchievements(int kills)
    {
        if (kills == 100) AchievementSystem.Unlock("百人斩");
    }
}

// 具体实现：UI 系统实现 ICombatUIProvider
public class CombatUI : MonoBehaviour, ICombatUIProvider
{
    [SerializeField] private HPBar playerHPBar;
    [SerializeField] private HPBar enemyHPBar;

    public void UpdateHP(string targetId, int current, int max)
    {
        if (targetId == "player") playerHPBar.SetValue(current, max);
        else enemyHPBar.SetValue(current, max);
    }

    public void ShowDamageNumber(int damage, Vector3 worldPosition)
    {
        // 实例化浮动伤害数字
    }
}
```

---

## 最终架构对比

**重构前**：

```
BattleSystem（一个类管所有）
    ├── 伤害计算（直接写在函数里）
    ├── 攻击类型（if/else if 硬编码）
    ├── 治疗（混在攻击函数里）
    ├── HP 管理（直接操作 GetComponent）
    ├── 经验/升级（直接写在函数里）
    ├── 存档（直接调用 PlayerPrefs）
    ├── UI 更新（直接 GameObject.Find）
    └── 成就系统（直接找 UI 组件）
```

**重构后**：

```
CombatSystem（只管战斗流程协调）
    ├── 依赖 CombatActionExecutor（执行战斗行为）
    ├── 依赖 ICombatUIProvider（UI 抽象接口）
    └── 依赖 IProgressTracker（进度追踪抽象接口）

CombatAction（抽象，可扩展）
    ├── AttackAction（攻击基类）
    │   ├── NormalAttack
    │   ├── HeavyAttack
    │   └── MagicAttack
    └── SupportAction（支援基类）
        └── HealAction

CombatStats（只管战斗数值）
DamageCalculator（只管计算）
StatusEffectSystem（只管状态效果）

CombatUI（实现 ICombatUIProvider）
SaveAndAchievementTracker（实现 IProgressTracker）
```

---

## 改变的代价与收益

| | 重构前 | 重构后 |
|---|---|---|
| 加新攻击类型 | 改 `ExecutePlayerAttack`（风险：影响所有攻击） | 新建一个 ScriptableObject 子类（零风险） |
| 换存档方式 | 改 `BattleSystem`（战斗逻辑文件） | 换一个 `IProgressTracker` 实现 |
| 换 UI 框架 | 改 `BattleSystem` | 换一个 `ICombatUIProvider` 实现 |
| 写战斗系统单元测试 | 需要真实 UI、存档、成就所有系统 | Mock `ICombatUIProvider` 和 `IProgressTracker` 即可 |
| 多人协作（同时加技能） | 冲突风险高（都改同一个文件） | 无冲突（每人开发一个攻击类文件） |

重构后的初始代码量确实更多，但每一次需求变化的边际成本，都比之前低得多。

---

## 小结

这个完整案例展示了 SOLID 五条原则的协同方式：

1. **SRP** 把职责拆开，让每个类只有一个变化理由
2. **OCP** 把变化点封装在子类里，让框架不需要为新功能而修改
3. **LSP** 保证继承层级的行为一致性，避免"攻击函数里做治疗"这样的概念混乱
4. **ISP** 用精准的小接口描述能力，不强迫依赖不相关的功能
5. **DIP** 让高层和低层都依赖抽象，实现真正的低耦合

这五条原则不是独立的规则，而是同一种设计思维从不同角度的表达：**让代码的每个部分都能独立变化，而不影响其他部分**。
