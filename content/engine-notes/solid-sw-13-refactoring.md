---
title: "软件工程基础 13｜重构手法：如何在测试保护下安全改善已有代码"
description: "重构不是重写。这篇讲什么是真正的重构、为什么测试是重构的安全网、10 个最常用的重构手法，以及在 Unity 游戏项目里安全重构的实践路径。"
slug: "solid-sw-13-refactoring"
weight: 725
tags:
  - 软件工程
  - 重构
  - 测试
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> **重构的定义（Martin Fowler）**：在不改变软件可观察行为的前提下，改善其内部结构。
>
> 这个定义里有两个关键词：**不改变行为**（功能不变），**改善结构**（代码质量提升）。
> 如果同时修改了功能，那不是重构，那是修改——两件事一起做，出了 Bug 你不知道是哪步引入的。

---

## 重构和重写的区别

"这段代码太烂了，重写一遍"——这是重写，不是重构。

重写的风险极高：
- 重写期间，所有新功能都要暂停
- 重写的代码需要全面测试（相当于重新开发）
- 原代码里积累的隐性知识（边界情况、奇怪的 workaround）可能在重写中丢失
- 重写可能引入新的 Bug，且难以与原来的行为比较

重构是另一种策略：**小步快走，每步都保持代码可运行**。

```
重写流程：
代码烂 → 停工几个月重写 → 新代码上线（可能有新 Bug）→ 又慢慢烂起来

重构流程：
代码烂 → 理解一小块 → 写测试覆盖这一块 → 小步改善 → 验证行为不变 → 继续下一小块
        → 持续进行，代码质量持续提升，功能持续可用
```

---

## 测试：重构的安全网

没有测试，重构等于走钢丝。每改一步，你都不知道有没有破坏已有功能。

测试在重构中的作用：

1. **改之前**：测试记录了当前的行为（"这段代码做什么"）
2. **改之中**：每步改完立即跑测试，红灯意味着你破坏了什么
3. **改之后**：测试通过意味着行为没有变化，重构成功

```csharp
// 准备重构这个函数之前，先写测试来记录它的行为
[Test]
public void CalculateDamage_PhysicalAttack_ReturnsCorrectValue()
{
    int damage = BattleSystem.CalculateDamage(attackPower: 50, defense: 10);
    Assert.AreEqual(40, damage); // 记录当前行为
}

[Test]
public void CalculateDamage_ZeroDefense_ReturnFullDamage()
{
    int damage = BattleSystem.CalculateDamage(attackPower: 50, defense: 0);
    Assert.AreEqual(50, damage);
}

[Test]
public void CalculateDamage_DefenseExceedsAttack_ReturnsZero()
{
    int damage = BattleSystem.CalculateDamage(attackPower: 10, defense: 50);
    Assert.AreEqual(0, damage); // 不应该返回负数
}
```

这些测试写好之后，你可以放心地重构 `CalculateDamage` 的实现，因为测试会告诉你有没有破坏边界情况。

### Unity 里的测试工具

Unity 提供了 Test Runner（Edit Mode 和 Play Mode 测试）：

```csharp
// Edit Mode 测试（适合纯逻辑，不需要 GameObject）
using NUnit.Framework;

[TestFixture]
public class DamageCalculatorTests
{
    private DamageCalculator calc;

    [SetUp]
    public void Setup()
    {
        calc = new DamageCalculator();
    }

    [Test]
    public void PhysicalDamage_SubtractsDefense()
    {
        Assert.AreEqual(40, calc.CalculatePhysical(50, 10));
    }

    [TestCase(50, 0, 50)]
    [TestCase(50, 10, 40)]
    [TestCase(10, 50, 0)] // 防御超过攻击不返回负数
    public void PhysicalDamage_VariousScenarios(int attack, int defense, int expected)
    {
        Assert.AreEqual(expected, calc.CalculatePhysical(attack, defense));
    }
}
```

---

## 10 个最常用的重构手法

### 手法 1：提取函数（Extract Method）

**适用场景**：一段代码需要注释才能理解，或者一个函数太长

```csharp
// 重构前
void Update()
{
    // 处理输入
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    Vector3 moveDir = new Vector3(h, 0, v).normalized;
    if (moveDir.magnitude > 0)
        transform.position += moveDir * moveSpeed * Time.deltaTime;

    // 检查跳跃
    if (Input.GetButtonDown("Jump") && isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        isGrounded = false;
    }
}

// 重构后
void Update()
{
    HandleMovement();
    HandleJump();
}

void HandleMovement()
{
    Vector3 moveDir = GetMovementInput();
    if (moveDir.magnitude > 0)
        transform.position += moveDir * moveSpeed * Time.deltaTime;
}

void HandleJump()
{
    if (Input.GetButtonDown("Jump") && isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        isGrounded = false;
    }
}

Vector3 GetMovementInput()
{
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    return new Vector3(h, 0, v).normalized;
}
```

### 手法 2：提取类（Extract Class）

**适用场景**：一个类承担了多个职责（上帝类的修复手段）

```csharp
// 重构前：PlayerController 承担了移动和战斗两个职责
public class PlayerController : MonoBehaviour
{
    // 移动相关
    private float moveSpeed = 5f;
    private bool isGrounded;
    void HandleMovement() { ... }

    // 战斗相关
    private int attackPower = 20;
    private float attackCooldown;
    void HandleAttack() { ... }
    void CalculateDamage() { ... }
}

// 重构后：职责分离
public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed = 5f;
    private bool isGrounded;
    void Update() { HandleMovement(); }
    void HandleMovement() { ... }
}

public class PlayerCombat : MonoBehaviour
{
    private int attackPower = 20;
    private float attackCooldown;
    void HandleAttack() { ... }
    int CalculateDamage() { ... }
}
```

### 手法 3：用多态替换条件（Replace Conditional with Polymorphism）

**适用场景**：switch/if-else 判断对象类型，根据类型做不同的事

```csharp
// 重构前：根据类型 switch
float GetMovementSpeed(Character character)
{
    switch (character.type)
    {
        case CharacterType.Warrior: return 4f;
        case CharacterType.Mage: return 3f;
        case CharacterType.Rogue: return 6f;
        default: return 5f;
    }
}

// 重构后：每个子类自己知道自己的速度
public abstract class Character
{
    public abstract float MovementSpeed { get; }
}

public class Warrior : Character
{
    public override float MovementSpeed => 4f;
}

public class Mage : Character
{
    public override float MovementSpeed => 3f;
}

public class Rogue : Character
{
    public override float MovementSpeed => 6f;
}
```

### 手法 4：引入参数对象（Introduce Parameter Object）

**适用场景**：长参数列表里有几个参数总是一起出现

```csharp
// 重构前
void SpawnProjectile(Vector3 origin, Vector3 direction, float speed,
                     int damage, float range, bool isPiercing) { ... }

// 重构后
public struct ProjectileConfig
{
    public float speed;
    public int damage;
    public float range;
    public bool isPiercing;
}

void SpawnProjectile(Vector3 origin, Vector3 direction, ProjectileConfig config) { ... }
```

### 手法 5：封装字段（Encapsulate Field）

**适用场景**：公开字段被外部随意修改，没有验证或通知

```csharp
// 重构前：hp 是公开字段，任何人都可以随便改
public class Character
{
    public int hp; // 外部可以直接 character.hp = -999
}

// 重构后：封装为属性，加上验证和通知
public class Character
{
    private int hp;

    public int HP
    {
        get => hp;
        set
        {
            int clamped = Mathf.Clamp(value, 0, MaxHP);
            if (clamped != hp)
            {
                hp = clamped;
                OnHPChanged?.Invoke(hp, MaxHP);
                if (hp == 0) OnDied?.Invoke();
            }
        }
    }

    public event Action<int, int> OnHPChanged;
    public event Action OnDied;
}
```

### 手法 6：以查询替换临时变量（Replace Temp with Query）

**适用场景**：临时变量只是用来存某个表达式的结果，可以改成方法

```csharp
// 重构前
void ProcessAttack()
{
    double baseDamage = attackPower * weaponMultiplier;
    double finalDamage = baseDamage * (isCrit ? critMultiplier : 1.0);
    target.TakeDamage((int)finalDamage);
}

// 重构后（如果这个计算逻辑会在多处用到）
double GetBaseDamage() => attackPower * weaponMultiplier;
double GetFinalDamage() => GetBaseDamage() * (isCrit ? critMultiplier : 1.0);

void ProcessAttack() => target.TakeDamage((int)GetFinalDamage());
```

### 手法 7：移动方法（Move Method）

**适用场景**：特性依恋——方法对另一个类的数据的使用多于自身

```csharp
// 重构前：这个方法在 Order 类里，但大量使用 Customer 的数据
// 应该把它移到 Customer 里
public class Order
{
    public double GetDiscountedPrice(Customer customer)
    {
        double discount = 0;
        if (customer.isPremium) discount = 0.1;
        if (customer.yearsSinceJoined > 5) discount += 0.05;
        if (customer.totalOrders > 100) discount += 0.05;
        return price * (1 - discount);
    }
}

// 重构后：方法移到 Customer，因为它依赖 Customer 的数据
public class Customer
{
    public double GetDiscount()
    {
        double discount = 0;
        if (isPremium) discount = 0.1;
        if (yearsSinceJoined > 5) discount += 0.05;
        if (totalOrders > 100) discount += 0.05;
        return discount;
    }
}

public class Order
{
    public double GetDiscountedPrice(Customer customer)
        => price * (1 - customer.GetDiscount());
}
```

### 手法 8：引入接口（Extract Interface）

**适用场景**：准备解耦两个类，需要先提取接口

```csharp
// 重构前：PlayerController 直接依赖 MusicPlayer 具体类
public class PlayerController
{
    private MusicPlayer musicPlayer;
    void OnEnterCombat() => musicPlayer.PlayCombatTheme();
}

// 第一步：提取接口
public interface IMusicSystem
{
    void PlayCombatTheme();
    void PlayExplorationTheme();
    void StopMusic();
}

// 第二步：让 MusicPlayer 实现接口
public class MusicPlayer : MonoBehaviour, IMusicSystem { ... }

// 第三步：PlayerController 依赖接口，不依赖具体类
public class PlayerController
{
    [SerializeField] private IMusicSystem musicSystem; // 现在可以换实现了
    void OnEnterCombat() => musicSystem.PlayCombatTheme();
}
```

---

## 在 Unity 项目里安全重构的实践路径

### 原则一：先有测试，再重构

在改动任何代码之前，先写测试覆盖当前行为。哪怕测试写起来很麻烦（因为代码耦合严重），这个麻烦程度本身就告诉你耦合有多严重。

### 原则二：小步提交

每完成一个小的重构手法（比如"提取一个函数"），就提交一次。

```bash
git commit -m "refactor: 提取 HandleMovement 方法，从 Update 中分离移动逻辑"
git commit -m "refactor: 提取 HandleJump 方法，从 Update 中分离跳跃逻辑"
git commit -m "refactor: 提取 PlayerMovement 组件，职责与 PlayerCombat 分离"
```

小步提交意味着出了问题可以精确地回滚到问题出现之前的状态。

### 原则三：重构和功能分开提交

不要在同一个 commit 里既重构又加功能。这使得 code review 难以分辨哪些是行为变化，哪些是纯结构调整。

```
不好的 commit：
"修复了暴击 Bug，顺便重构了战斗系统"
→ 无法知道 Bug 修复在哪里，重构改了什么

好的 commit 序列：
1. "refactor: 提取 DamageCalculator 类"
2. "fix: 修复暴击率超过 100% 时的计算错误"
```

### 原则四：重构热点而不是全面重构

不需要把整个项目重构一遍。优先重构"热点"——那些经常被修改的代码，因为每次修改都在承担高耦合的代价。

识别热点的方法：`git log --stat` 看哪些文件修改频率最高，那就是热点。

---

## 重构的时机：童子军规则

Robert C. Martin 引用了童子军的规则：**离开营地时，营地要比你来时更干净。**

应用到代码：**每次打开一个文件，离开时它应该比你来时略微更整洁。**

不需要每次都做大规模重构。每次：
- 改一个坏的变量名
- 提取一个过长的方法
- 删掉一段注释掉的代码
- 消灭一个明显的 Code Smell

积少成多，代码质量会在日常工作中持续改善。

---

## 小结

| 重构手法 | 适用 Smell | 核心动作 |
|---|---|---|
| 提取函数 | 长方法 | 把一段代码变成有名字的函数 |
| 提取类 | 上帝类、SRP 违反 | 把一组相关字段和方法移到新类 |
| 用多态替换条件 | Switch 语句 | 用继承/接口替代 type-switch |
| 引入参数对象 | 过长参数列表 | 把相关参数打包成一个对象 |
| 封装字段 | 数据裸露 | 把公开字段变成带验证/通知的属性 |
| 以查询替换临时变量 | 可读性 | 提取方法，让计算有名字 |
| 移动方法 | 特性依恋 | 把方法移到它依赖数据更多的类 |
| 引入接口 | DIP 违反 | 提取接口，解除直接依赖 |

重构是一种**持续的、渐进式的**代码改善活动，而不是一次性的大手术。最有效的重构策略是：在测试保护下，随着功能开发持续进行小步重构——童子军规则。
