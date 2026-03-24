---
title: "软件工程基础 05｜里氏替换原则（LSP）：子类必须能替换父类"
description: "LSP 是继承机制是否被正确使用的检验标准。这篇解释什么情况下继承是错误的，游戏中最常见的 LSP 违反模式，以及如何用组合代替不正当的继承。"
slug: "solid-sw-05-lsp"
weight: 709
tags:
  - 软件工程
  - SOLID
  - LSP
  - 里氏替换
  - 继承
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> **LSP（Liskov Substitution Principle）：如果 S 是 T 的子类，那么程序里所有使用 T 的地方，都可以用 S 来替换，且不会改变程序的正确性。**
>
> 简单说：**子类必须能完全替换父类**，而不需要调用方做任何特殊处理。

这条原则是继承机制的正确性保证。如果你的子类不能替换父类，你的继承就是错误的——不是继承本身错了，而是这个继承关系不该存在。

---

## 先看一个经典反例：正方形不是矩形

这是 LSP 教科书里最著名的例子，但在游戏里版本更明显，先用它建立直觉。

```csharp
public class Rectangle
{
    public virtual float Width { get; set; }
    public virtual float Height { get; set; }

    public float Area() => Width * Height;
}

public class Square : Rectangle
{
    // 正方形的宽和高必须相等
    public override float Width
    {
        set { base.Width = value; base.Height = value; } // 改宽度时同时改高度
    }
    public override float Height
    {
        set { base.Width = value; base.Height = value; } // 改高度时同时改宽度
    }
}
```

问题在哪？假设有这样一个函数：

```csharp
// 这个函数假设：设置了宽度，高度不会变
void DoubleWidth(Rectangle rect)
{
    float originalHeight = rect.Height;
    rect.Width *= 2;
    // 如果传入的是 Rectangle，Area 正确 = 2 × Width × Height
    // 如果传入的是 Square，Height 也被改变了，Area 不是 2 × Width × originalHeight
    Debug.Assert(rect.Area() == rect.Width * originalHeight); // 对 Rectangle 成立，对 Square 失败
}
```

`Square` 不能替换 `Rectangle`——把 `Square` 传给 `DoubleWidth`，程序的行为不符合预期。

从"现实世界"来看，正方形确实是一种特殊的矩形。但在代码里，**继承不是描述"是一种"的关系，而是描述"行为兼容"的关系**。

---

## 游戏中的 LSP 违反案例

### 案例一：飞行单位不能移动到地面格子

在一个策略游戏里，所有单位都有移动功能：

```csharp
public class Unit
{
    public virtual void MoveTo(GridCell targetCell)
    {
        // 走格子，检查地面障碍
        if (targetCell.HasObstacle)
        {
            Debug.Log("Cannot move: obstacle in the way.");
            return;
        }
        Position = targetCell;
    }

    public virtual List<GridCell> GetReachableCells(int moveRange)
    {
        // 返回地面格子
        return GridManager.GetGroundCells(Position, moveRange);
    }
}

public class FlyingUnit : Unit
{
    public override void MoveTo(GridCell targetCell)
    {
        // 飞行单位可以越过障碍——但 targetCell.HasObstacle 这个检查不应该在这里
        // 问题：父类约定了"不能移动到有障碍的格子"，子类却跳过了这个约定
        Position = targetCell; // 直接移动，忽略了父类的行为约定
    }

    public override List<GridCell> GetReachableCells(int moveRange)
    {
        // 飞行单位可以到达空中格子，但父类的调用方假设返回的是地面格子
        return GridManager.GetAllCells(Position, moveRange); // 包含空中格子
    }
}
```

现在假设 AI 系统有这样的代码：

```csharp
// AI 系统假设所有 Unit 的行为符合同一套约定
public class AISystem
{
    public void MoveToTarget(Unit unit, GridCell target)
    {
        var reachable = unit.GetReachableCells(unit.MoveRange);
        // 假设 reachable 里只有地面格子
        GridCell bestCell = FindBestGroundCell(reachable, target);
        unit.MoveTo(bestCell); // 假设这个格子是可以到达的
    }
}
```

当 `FlyingUnit` 被传进来：
- `GetReachableCells` 返回了包含空中格子的列表
- `FindBestGroundCell` 可能选出空中格子（因为没想到会有空中格子）
- `MoveTo` 忽略了障碍检查，做了父类明确不允许的事

飞行单位违反了 LSP：它不能替换父类 `Unit` 来正常工作。

### 案例二：沉默的魔法免疫单位

```csharp
public class Character
{
    public virtual void CastSpell(SpellData spell)
    {
        // 约定：施法会消耗法力
        if (CurrentMana < spell.ManaCost) throw new Exception("Not enough mana");
        CurrentMana -= spell.ManaCost;
        spell.Execute(this);
    }
}

public class MagicImmuneCharacter : Character
{
    public override void CastSpell(SpellData spell)
    {
        // 魔法免疫的角色不能施法
        // 错误的做法：什么都不做（违反父类的隐式约定：调用后法术应该被执行）
        // 或者：直接抛异常
        throw new InvalidOperationException("Cannot cast spells while magic immune");
    }
}
```

所有调用 `CastSpell` 的地方都假设：如果调用成功（没有抛出 "Not enough mana" 异常），法术就会被执行。`MagicImmuneCharacter` 打破了这个约定，使得调用方必须额外检查：

```csharp
// 调用方被迫做特殊处理，说明 LSP 被违反了
void BattleAI_CastBestSpell(Character character)
{
    // 不得不在这里做类型检查——这是 LSP 被违反的信号
    if (character is MagicImmuneCharacter)
        return; // 单独处理

    character.CastSpell(GetBestSpell(character));
}
```

一旦调用方需要用 `is` 或者 `as` 来检查具体类型，就说明 LSP 被违反了——父类的接口不够用了，调用方需要知道实际类型才能正确工作。

---

## LSP 违反的识别方法

### 信号一：子类方法里出现空实现或直接抛异常

```csharp
public class BossEnemy : Enemy
{
    public override void TakeDamage(int damage)
    {
        // Boss 第一阶段免疫所有伤害
        // 错误：什么都不做
    }
}
```

父类约定了"TakeDamage 会减少 HP"，子类空实现打破了这个约定。

### 信号二：调用方里出现 is / as 类型检查

```csharp
// LSP 被违反的典型信号
void ProcessEnemy(Enemy enemy)
{
    if (enemy is FlyingEnemy flyingEnemy)
    {
        // 飞行敌人需要特殊处理
        flyingEnemy.DoFlyingThing();
    }
    else if (enemy is BossEnemy boss)
    {
        // Boss 需要特殊处理
        boss.DoBossThing();
    }
    else
    {
        enemy.DoNormalThing();
    }
}
```

每次加新的 Enemy 类型，这里都要加一个 `else if`——这违反了 OCP，而根本原因是违反了 LSP。

### 信号三：子类的前置条件比父类更严格

```csharp
// 父类：任何正整数伤害都可以
public virtual void TakeDamage(int damage)
{
    HP -= damage;
}

// 子类：只接受 10 以上的伤害（加强了前置条件）
public override void TakeDamage(int damage)
{
    if (damage < 10) return; // 错误：比父类更严格的前置条件
    HP -= damage;
}
```

LSP 要求：子类的前置条件只能**更宽松**，不能更严格；子类的后置条件只能**更强**，不能更弱。

---

## 正确的解法：重新审视继承关系

当发现 LSP 被违反时，通常意味着继承关系本身就是错的，需要重新设计。

### 解法一：接口分离，不用继承

飞行单位的例子里，问题是"飞行单位"和"地面单位"的移动规则根本不同，不应该共享同一个 `MoveTo` 约定。

```csharp
// 用接口描述能力，而不是用继承描述"是一种"
public interface IGroundMovable
{
    void MoveTo(GridCell cell);
    List<GridCell> GetReachableGroundCells(int range);
}

public interface IAirMovable
{
    void FlyTo(GridCell cell); // 不同的方法名，不同的约定
    List<GridCell> GetReachableAirCells(int range);
}

public class GroundUnit : IGroundMovable
{
    public void MoveTo(GridCell cell) { /* 地面移动逻辑 */ }
    public List<GridCell> GetReachableGroundCells(int range) { /* 只返回地面格子 */ }
}

public class FlyingUnit : IAirMovable
{
    public void FlyTo(GridCell cell) { /* 飞行移动逻辑 */ }
    public List<GridCell> GetReachableAirCells(int range) { /* 返回空中格子 */ }
}
```

### 解法二：用组合代替继承

魔法免疫的例子里，"魔法免疫"是一种**状态/能力**，不是一种**角色类型**。用组合而不是继承来表达：

```csharp
// 魔法免疫是一个可以附加的状态
public class MagicImmuneBuff : StatusEffect
{
    public bool IsActive { get; private set; }
    // ... 持续时间等
}

public class Character
{
    private List<StatusEffect> activeBuffs = new();

    public bool CanCastSpell()
    {
        return !activeBuffs.OfType<MagicImmuneBuff>().Any(b => b.IsActive);
    }

    public void CastSpell(SpellData spell)
    {
        if (!CanCastSpell()) return;
        if (CurrentMana < spell.ManaCost) return;
        CurrentMana -= spell.ManaCost;
        spell.Execute(this);
    }
}
```

"魔法免疫"不是通过继承子类实现，而是通过给 `Character` 附加一个 Buff 来实现。不需要子类，不破坏 LSP。

---

## 什么时候可以用继承

继承是合法的，当：

1. **子类完全符合父类的所有约定**（能够替换父类的所有使用场景）
2. **子类只是"增强"了父类的能力**，而不是改变了父类的行为
3. **"是一种"的关系在行为层面也成立**，不只是概念层面

```csharp
// 合法继承：MeleeEnemy 完全符合 Enemy 的所有约定，只是增加了近战专有行为
public class MeleeEnemy : Enemy
{
    // 完全保留父类所有行为
    public override void TakeDamage(int damage) => base.TakeDamage(damage);

    // 只增加了近战专有的能力
    public void PerformMeleeCombo() { /* 近战连击 */ }
}
```

---

## 游戏开发中继承的最常见滥用

### "为了复用代码而继承"

```csharp
// 错误用法：Projectile 继承 GameObject 只是为了用 Transform
// 但 Projectile 并不是一种 "可以挂 Component 的 GameObject"
public class MagicBolt : MonoBehaviour // 这个没问题，Unity 要求如此
{ }

// 这是错误的：为了复用 "能移动" 的代码而继承
public class Rocket : Bullet // Rocket 是 Bullet 吗？不是，Rocket 是追踪导弹，行为完全不同
{
    public override void Move() // 打破了 Bullet 的移动约定
    {
        // 追踪逻辑完全不同
    }
}
```

应该用组合：`Rocket` 和 `Bullet` 共享一个 `IProjectile` 接口，各自实现 `Move`。

---

## 小结

- **LSP 的本质**：子类必须能在任何使用父类的场合中，无缝替换父类，且行为符合父类的约定
- **违反的信号**：子类里出现空实现/直接抛异常；调用方需要 `is`/`as` 类型检查；子类加强了前置条件
- **根本原因**：用继承来描述"概念上是一种"，而不是"行为上是一种"
- **修复方法**：重新审视继承关系，通常用接口分离或组合来替代不当的继承
- **继承的正确场景**：子类完全符合父类约定，且只是增强而不改变父类行为

LSP 实际上是在保护你的继承层级不被滥用。继承是一个强大但容易误用的工具，LSP 给了它一个清晰的正确性判断标准。
