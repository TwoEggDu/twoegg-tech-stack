---
title: "软件工程基础 12｜Code Smell 识别：危险信号的诊断"
description: "Code Smell 是代码里预示深层问题的表面症状。这篇讲游戏项目中最常见的 10 种 Code Smell，它们对应什么 SOLID 违反，以及如何快速识别。"
slug: "solid-sw-12-code-smell"
weight: 723
tags:
  - 软件工程
  - Code Smell
  - 重构
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> Martin Fowler 把 Code Smell 定义为：**代码结构上的某种特征，预示着可能存在更深层的问题。**
>
> Code Smell 不一定是 Bug，但它是警告信号——表示这里将来可能出问题，或者已经在悄悄增加技术债的利息。

识别 Code Smell 的价值在于：它给了"这段代码可能有问题"一个具体的、可讨论的名字。当你能给问题命名，你才能和团队讨论并系统性地解决它。

---

## Smell 1：上帝类（God Class）

**症状**：一个类知道得太多、做得太多。几千行代码，几十个方法，覆盖了系统里几乎所有的职责。

```csharp
// 典型的上帝类
public class GameManager : MonoBehaviour
{
    // 2000+ 行
    // 管理游戏状态
    // 管理玩家数据
    // 管理敌人生成
    // 管理关卡进度
    // 管理 UI 状态
    // 管理存档
    // 管理成就
    // 管理网络状态
    // ...
}
```

**诊断标准**：
- 类名叫 `Manager`、`Controller`、`System`，但这个名字不能描述它具体管什么
- 几乎系统的每个改动都会影响这个类
- 新入职的程序员需要先读完这个类才能做任何事

**对应 SOLID 违反**：SRP（N 个职责）、DIP（知道太多具体类型）

**处理方向**：按变化理由拆分，抽出独立的 `PlayerDataManager`、`EnemySpawner`、`ProgressTracker` 等。

---

## Smell 2：散弹式修改（Shotgun Surgery）

**症状**：每次修改一个需求，都需要在多个不同的文件里做对应改动。改一件事，要开 5 个文件。

```
需求变更：给战斗系统加"护盾"状态

需要修改：
  - BattleSystem.cs（受伤时判断护盾）
  - PlayerStats.cs（加 shieldHP 字段）
  - UIManager.cs（加护盾条显示）
  - SaveSystem.cs（护盾状态要存档）
  - AnimationController.cs（加护盾动画）
  - AchievementSystem.cs（加护盾相关成就）
```

**诊断标准**：一个功能变化需要在超过 2–3 个不相关的文件里修改。

**对应 SOLID 违反**：SRP 和 DIP 的组合违反——职责分布在多处，且没有通过抽象接口隔离

**处理方向**：把相关的逻辑聚合到同一个模块，通过事件/接口让外部系统订阅，而不是直接被修改

---

## Smell 3：特性依恋（Feature Envy）

**症状**：一个方法对另一个类的数据的兴趣，明显超过对自己类的数据。

```csharp
// AttackSystem 里的方法，大部分时间在操作 Character 类的数据
// 而不是 AttackSystem 自己的数据——这个方法应该在 Character 里
public class AttackSystem
{
    public void PerformAttack(Character attacker, Character target)
    {
        // 大量访问 attacker 的内部数据
        int rawDamage = attacker.stats.attackPower + attacker.equipment.weapon.damage;
        float critChance = attacker.stats.critRate + attacker.buffs.GetCritBonus();
        bool isCrit = Random.value < critChance;
        float critMultiplier = attacker.stats.critDamage;

        // 大量访问 target 的内部数据
        int defense = target.stats.defense + target.equipment.armor.defenseValue;
        bool isBlocking = target.state == CharacterState.Blocking;
        float blockReduction = isBlocking ? target.stats.blockRate : 0f;

        // ... 基本上所有数据都来自 attacker 和 target
        // AttackSystem 自己的成员变量一个都没用到
    }
}
```

**诊断标准**：方法里，其他类的成员访问次数远多于自身类的成员访问次数

**对应 SOLID 违反**：SRP——这个方法属于它更"依恋"的那个类

**处理方向**：把方法移到它所依恋的类里，或者拆分成多个更聚合的函数

---

## Smell 4：过长参数列表（Long Parameter List）

**症状**：函数有四个以上的参数。

```csharp
// 五个参数——调用时你记得住哪个是哪个吗？
void SpawnEffect(Vector3 position, Quaternion rotation, float scale,
                 float duration, bool isLooping, Color tintColor,
                 int sortingOrder, string parentTag)
{
    // ...
}

// 调用时
SpawnEffect(hitPoint, Quaternion.identity, 1.5f, 2f, false, Color.red, 10, "Effects");
// 这一串数字和枚举，读代码的人完全不知道哪个是什么含义
```

**对应 SOLID 违反**：SRP（可能这个函数做了太多事）、ISP（接口过于宽泛）

**处理方向**：把参数打包成 Parameter Object：

```csharp
public struct EffectSpawnConfig
{
    public Vector3 position;
    public Quaternion rotation;
    public float scale;
    public float duration;
    public bool isLooping;
    public Color tintColor;

    public static EffectSpawnConfig Default(Vector3 pos) => new()
    {
        position = pos,
        rotation = Quaternion.identity,
        scale = 1f,
        duration = 1f,
        isLooping = false,
        tintColor = Color.white
    };
}

void SpawnEffect(EffectSpawnConfig config) { ... }

// 调用时清晰
var config = EffectSpawnConfig.Default(hitPoint);
config.tintColor = Color.red;
config.duration = 2f;
SpawnEffect(config);
```

---

## Smell 5：基本类型偏执（Primitive Obsession）

**症状**：用 int、string、float 这样的基本类型来表示有业务含义的概念，导致类型系统无法帮你防错。

```csharp
// 差：用 string 表示"武器类型"
void EquipWeapon(string weaponType)
{
    if (weaponType == "Sword") { ... }
    else if (weaponType == "Bow") { ... }
    // "swrod"（拼写错误）会在运行时才发现
}

// 差：用两个 float 表示"坐标"
void Teleport(float x, float z)
{
    // 调用时 Teleport(z, x) 编译器不会报错
}

// 好：用类型来表达概念
public enum WeaponType { Sword, Bow, Staff, Dagger }

void EquipWeapon(WeaponType weaponType)
{
    // 编译期就能发现错误，IDE 提供自动补全
}

// 好：用结构体封装坐标（如果不想用 Vector3）
public readonly struct WorldPosition
{
    public readonly float X, Z;
    public WorldPosition(float x, float z) { X = x; Z = z; }
}
```

**游戏中常见的基本类型偏执**：
- 用 `string` 表示技能 ID（应该用 `SkillId` 类型或枚举）
- 用 `int` 表示玩家 ID 和敌人 ID（两种 ID 混用不会报编译错误）
- 用 `float` 表示百分比（0.5 还是 50？）

---

## Smell 6：Switch 语句（Switch Statement）

**症状**：在多个地方重复对同一个枚举/类型做 switch，每次增加新类型都要找到所有 switch 并修改。

```csharp
// 三个地方都在 switch EnemyType
void RenderEnemy(EnemyType type, Vector3 position)
{
    switch (type) {
        case EnemyType.Goblin: /* 渲染哥布林 */ break;
        case EnemyType.Orc: /* 渲染兽人 */ break;
    }
}

void GetEnemyReward(EnemyType type)
{
    switch (type) {
        case EnemyType.Goblin: return new Reward(10, 5);
        case EnemyType.Orc: return new Reward(30, 15);
    }
}

// 加新敌人类型 Dragon，需要找到所有 switch 并修改
// 漏改一个，Bug 在运行时才发现
```

**对应 SOLID 违反**：OCP（每次扩展都需要修改现有代码）

**处理方向**：用多态替换 switch——把每种类型的行为封装在各自的子类里（OCP 那篇的核心内容）

---

## Smell 7：平行继承层次（Parallel Inheritance Hierarchies）

**症状**：每次给一个类层次加一个子类，都必须同时给另一个类层次加一个对应的子类。

```csharp
// 两个层次需要同步维护
// 层次 1：角色类型
abstract class Character { }
class PlayerCharacter : Character { }
class EnemyCharacter : Character { }
class BossCharacter : Character { }  // 加了 Boss

// 层次 2：动画控制器（必须同时加）
abstract class CharacterAnimator { }
class PlayerAnimator : CharacterAnimator { }
class EnemyAnimator : CharacterAnimator { }
class BossAnimator : CharacterAnimator { }  // 也要加 Boss

// 层次 3：AI 控制器（也必须同步）
abstract class CharacterAI { }
class PlayerAI : CharacterAI { }
class EnemyAI : CharacterAI { }
class BossAI : CharacterAI { }  // 也要加
```

每次加新角色类型，需要在三个层次里各加一个类，而且这三个层次通常在代码里是通过类型名强绑定的。

**处理方向**：用组合替代继承——`Character` 组合 `IAnimator`、`IAIBehavior` 等接口，各个实现不需要平行的层次。

---

## Smell 8：数据块（Data Clumps）

**症状**：一组数据总是同时出现，但没有被封装成一个对象。

```csharp
// 这三个值总是一起出现
void SpawnEnemy(float posX, float posY, float posZ) { }
void TeleportTo(float posX, float posY, float posZ) { }
void DrawGizmo(float posX, float posY, float posZ, float radius) { }

// 每次都要写三个参数，而不是一个 Vector3
// 如果以后需要增加 Y 轴（从 2D 到 3D），每个函数都要改
```

类似的模式：HP 和 MaxHP 总是一起出现、宽度和高度总是一起出现。

**处理方向**：把总是一起出现的数据封装成一个类或结构体。

---

## Smell 9：不恰当的亲密关系（Inappropriate Intimacy）

**症状**：两个类互相过于深入地了解对方的内部实现，形成了紧密的双向耦合。

```csharp
// Enemy 知道 Player 的内部字段
public class Enemy
{
    public void Attack(Player player)
    {
        // 直接访问 Player 的私有/公开内部字段
        if (player.isShieldActive && player.shieldHP > 0)
        {
            player.shieldHP -= damage;
        }
        else
        {
            player.hp -= damage;
            player.lastHitTime = Time.time;
            player.consecutiveHits++;
        }
    }
}

// Player 也知道 Enemy 的内部状态
public class Player
{
    void Parry(Enemy enemy)
    {
        if (enemy.attackState == EnemyAttackState.WindUp &&
            enemy.attackTimer < 0.3f)
        {
            enemy.isStunned = true;
            enemy.stunDuration = 2f;
        }
    }
}
```

**处理方向**：通过方法（接口）来通信，而不是直接操作对方的字段：

```csharp
// 通过方法通信，不暴露内部细节
player.TakeDamage(damage); // Player 自己决定怎么扣血、更新护盾
enemy.TriggerStun(duration); // Enemy 自己处理眩晕状态
```

---

## Smell 10：过度注释的代码（Comments That Explain Bad Code）

**症状**：代码里有大量注释，但如果把代码写得更清楚，这些注释根本不需要存在。

```csharp
// 计算基础伤害乘以暴击倍率
int fd = bd * (cr ? cm : 1);

// 这段注释只是在重复代码在做什么
// 如果把变量名写好，注释就不需要了：
int finalDamage = baseDamage * (isCriticalHit ? criticalMultiplier : 1);
```

过度注释是代码质量差的代偿——用注释来弥补代码本身的不可读性。

**处理方向**：改善代码本身（命名、结构），让代码自解释，而不是用注释来解释。

---

## 使用 Code Smell 的注意事项

Code Smell 是**信号，不是判决**。发现 Smell 不意味着立即重构，而是意味着：

1. 这里有潜在风险，在做相关修改时要格外小心
2. 如果这里经常需要修改（它是一个热点），优先重构
3. 如果这里已经稳定，没有计划改动，可以暂时保留（YAGNI——不需要的重构也不要做）

重构的时机：**当你正在为其他原因打开这个文件时**，顺手消灭你发现的 Smell。不要为了消灭 Smell 而专门开一个"重构 Sprint"——那是在还技术债，代价很高。

---

## 小结

| Code Smell | 对应违反 | 核心问题 |
|---|---|---|
| 上帝类 | SRP、DIP | 知道太多、管太多 |
| 散弹式修改 | SRP、DIP | 一个概念分散在多个文件里 |
| 特性依恋 | SRP | 方法在错误的类里 |
| 过长参数列表 | SRP | 函数做了太多事，或需要 Parameter Object |
| 基本类型偏执 | ISP | 没有用类型系统来表达业务概念 |
| Switch 语句 | OCP | 应该用多态替代条件分支 |
| 平行继承层次 | SRP | 应该用组合替代平行继承 |
| 数据块 | SRP | 相关数据应该被封装在一起 |
| 不恰当的亲密关系 | DIP | 通过接口通信，而不是直接操作内部字段 |
| 过度注释 | Clean Code | 改善代码本身，让代码自解释 |

掌握这 10 种 Smell 的识别，就掌握了在代码 Review 时快速诊断潜在问题的能力。
