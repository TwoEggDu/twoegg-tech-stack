---
date: "2026-03-26"
title: "软件工程基础 02｜耦合与内聚：衡量代码质量的两把尺子"
description: "耦合和内聚是评判任何软件设计的两个最基础维度。这篇解释它们的定义、度量方式，以及在游戏代码里如何用这两个概念快速诊断设计问题。"
slug: "solid-sw-02-coupling-cohesion"
weight: 703
tags:
  - 软件工程
  - 耦合
  - 内聚
  - 代码质量
  - SOLID
series: "软件工程基础与 SOLID 原则"
---

> 如果你只能用两个词来评价一段代码的设计质量，这两个词应该是：**内聚**和**耦合**。
> 内聚衡量一个模块内部有多"专注"，耦合衡量两个模块之间有多"纠缠"。
> 好的设计方向永远是：**高内聚，低耦合**。

SOLID 的五条原则，从不同角度在做同一件事：提高内聚，降低耦合。在讲具体原则之前，先把这两个概念说清楚。

---

## 耦合（Coupling）

耦合描述的是两个模块之间的依赖程度。

如果模块 A 的工作依赖于模块 B 的内部实现细节，那么 A 和 B 之间就存在耦合。耦合越高，两个模块就越"绑定"在一起——修改 B 的时候，必须同时考虑对 A 的影响；移植 A 到另一个项目时，必须把 B 也带过去。

### 耦合的直觉理解

想象两个齿轮。

低耦合的齿轮：通过标准接口啮合，可以单独替换其中一个，只要替换后的齿轮符合同样的接口规格。

高耦合的齿轮：被焊在一起，或者一个齿轮的轮齿形状依赖于另一个齿轮的内部结构。要改一个，必须两个一起改。

### 游戏中的高耦合案例

```csharp
// 高耦合的例子：PlayerController 直接依赖 EnemyAI 的内部实现
public class PlayerController : MonoBehaviour
{
    private EnemyAI enemyAI; // 直接持有敌人的引用

    void Update()
    {
        // 玩家逻辑里直接访问敌人的内部状态
        if (enemyAI.currentState == EnemyAI.State.Attacking &&
            enemyAI.attackTarget == this.gameObject &&
            enemyAI.attackTimer > 0.5f)
        {
            // 玩家的某种响应逻辑
            TriggerParry();
        }
    }
}
```

这段代码的问题：

1. `PlayerController` 依赖了 `EnemyAI.State` 这个枚举——如果敌人的状态机被重构，`PlayerController` 也要改
2. `PlayerController` 依赖了 `EnemyAI.attackTimer` 这个内部字段——如果敌人攻击系统改成事件驱动，`PlayerController` 也要改
3. 玩家和敌人的代码现在在逻辑上绑定在一起——你不能把 `PlayerController` 移植到没有这种敌人的游戏里

修改之后：

```csharp
// 低耦合：通过事件/接口通信，互不知道对方的内部实现
public class EnemyAI : MonoBehaviour
{
    public event Action<GameObject> OnAttackInitiated; // 对外发布事件，不关心谁在监听

    void BeginAttack(GameObject target)
    {
        OnAttackInitiated?.Invoke(target);
    }
}

public class PlayerController : MonoBehaviour
{
    void OnEnable()
    {
        // 玩家订阅事件，不需要知道 EnemyAI 内部有什么
        FindObjectOfType<EnemyAI>().OnAttackInitiated += HandleIncomingAttack;
    }

    void HandleIncomingAttack(GameObject attacker)
    {
        TriggerParry();
    }
}
```

现在两个类只通过一个公开的事件接口联系，彼此不知道对方的内部实现。

### 耦合的几个层次

从轻到重，耦合有以下几种形式：

**数据耦合（最轻）**：A 传给 B 一些简单数据（int、string），B 处理后返回结果。两者只通过数据交流，互不知道内部实现。这是可以接受的耦合。

```csharp
// 数据耦合：传入数值，返回结果，互不知道对方是怎么实现的
float finalDamage = DamageCalculator.Calculate(baseDamage, defense, critMultiplier);
```

**接口耦合（轻）**：A 通过一个定义好的接口与 B 通信，不依赖 B 的具体类型。这是推荐的耦合方式。

```csharp
// 接口耦合：只知道"它能受伤"，不知道它是 Player、Enemy 还是 Boss
IDamageable target = GetTarget();
target.TakeDamage(damage);
```

**实现耦合（重）**：A 依赖 B 的具体实现细节，如访问 B 的私有字段或依赖 B 的特定行为方式。这是前面例子里的问题。

**全局状态耦合（最重）**：A 和 B 都依赖同一个全局状态（静态变量、单例），修改全局状态会影响所有依赖它的模块，耦合关系变得难以追踪。

```csharp
// 全局状态耦合：GameManager.Instance 被所有地方使用
// 当 GameManager 发生变化，你不知道会影响哪些地方
void Update()
{
    if (GameManager.Instance.isInCombat &&
        GameManager.Instance.currentPlayer.stats.level > 10)
    {
        // ...
    }
}
```

---

## 内聚（Cohesion）

内聚描述的是一个模块内部的元素有多"相关"。

高内聚的模块：内部所有代码都在围绕一个明确的职责工作，每个方法都服务于同一个目的。

低内聚的模块：把不相关的功能堆在一起，"杂货铺"式的设计。

### 内聚的直觉理解

想象一把瑞士军刀和一把专业手术刀。

瑞士军刀（低内聚）：可以做很多事，但做任何一件事的时候都不是最好用的。你想换刀片，得把整把刀都换掉。

手术刀（高内聚）：只做一件事，做到极致。可以单独换，不影响其他工具。

### 游戏中的低内聚案例

```csharp
// 低内聚的 GameManager：什么都管，什么都不精
public class GameManager : MonoBehaviour
{
    // 游戏状态
    public GameState currentState;

    // 玩家数据
    public int playerHP;
    public int playerMP;
    public int playerGold;

    // 存档相关
    public void SaveGame() { ... }
    public void LoadGame() { ... }

    // UI 更新
    public void UpdateHPBar() { ... }
    public void ShowDamageNumber(int damage) { ... }

    // 音效播放
    public void PlayAttackSound() { ... }
    public void PlayDeathSound() { ... }

    // 关卡管理
    public void LoadNextLevel() { ... }
    public void RestartLevel() { ... }

    // 网络同步
    public void SyncPlayerState() { ... }

    // 成就系统
    public void CheckAchievements() { ... }
}
```

这个类的问题：

1. 它把完全不相关的功能（存档、UI、音效、关卡、网络、成就）塞在一起
2. 改声音播放逻辑，需要打开这个文件，风险影响到存档逻辑
3. 给存档系统写单元测试，不可避免地要初始化 UI 和音频相关的依赖
4. 任何一个功能变复杂，整个类都会膨胀

高内聚的版本应该把职责拆开：

```csharp
// 高内聚：每个类只管自己的一件事
public class PlayerStats : MonoBehaviour
{
    public int HP { get; private set; }
    public int MP { get; private set; }
    public int Gold { get; private set; }
    // 只管玩家属性数据
}

public class SaveSystem : MonoBehaviour
{
    public void Save(PlayerStats stats) { ... }
    public PlayerStats Load() { ... }
    // 只管存档/读档
}

public class UIManager : MonoBehaviour
{
    public void UpdateHPBar(int hp, int maxHP) { ... }
    public void ShowDamageNumber(int damage, Vector3 position) { ... }
    // 只管 UI 显示
}

public class AudioManager : MonoBehaviour
{
    public void Play(AudioClip clip, Vector3 position) { ... }
    // 只管音效播放
}
```

每个类内聚性更高，职责清晰，修改其中一个不会影响其他的。

### 内聚的度量

判断一个类是否高内聚，可以问以下问题：

1. **能否用一句话描述这个类的职责？** 如果需要用"以及"连接多个职责，内聚性可能不够。
2. **这个类的所有方法都用到了大部分成员变量吗？** 如果有些方法只用了几个成员变量，而其他方法用了另外几个，这个类可能应该被拆成两个。
3. **如果这个类的一部分职责改变，另一部分是否也必须跟着改？** 如果不是，两部分是独立的，不应该在同一个类里。

---

## 高耦合与低内聚是同一个问题的两面

有意思的是，高耦合和低内聚往往同时出现，互为因果。

当一个类的职责很多（低内聚），它就需要依赖很多其他类来完成这些职责（高耦合）。当两个类高度耦合，它们共享的职责很难被清晰划分（低内聚）。

改善任意一个，通常也会改善另一个。这也是为什么 SOLID 的五条原则实际上都在从不同角度解决同一个问题。

---

## 一个游戏场景的完整诊断

以一个典型的游戏伤害系统为例，用耦合和内聚来诊断它的问题：

```csharp
// 被诊断的代码
public class Sword : MonoBehaviour
{
    public int damage = 50;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            // 问题1：直接 GetComponent，和 Enemy 的具体实现耦合
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();

            // 问题2：在武器类里计算暴击，职责不属于这里（低内聚）
            int finalDamage = damage;
            if (Random.value < 0.2f) finalDamage *= 2;

            // 问题3：直接访问 enemy.hp，依赖内部字段（高耦合）
            enemy.hp -= finalDamage;

            // 问题4：在武器类里播放音效（低内聚）
            GetComponent<AudioSource>().Play();

            // 问题5：在武器类里更新 UI（低内聚，且和 UI 高耦合）
            GameObject.Find("EnemyHPBar").GetComponent<HPBar>().Refresh(enemy.hp);
        }
    }
}
```

**耦合问题**：
- `Sword` → `Enemy`（具体类，而不是接口）
- `Sword` → `HPBar`（直接查找 UI 对象）

**内聚问题**：
- `Sword` 同时负责：碰撞检测、伤害计算、暴击判断、音效播放、UI 更新

理想的改写方向：

```csharp
// 武器只负责碰撞检测和触发伤害事件
public class Sword : MonoBehaviour
{
    public int damage = 50;
    public event Action<IDamageable, int> OnHit; // 高层逻辑通过事件解耦

    void OnCollisionEnter(Collision collision)
    {
        IDamageable target = collision.gameObject.GetComponent<IDamageable>();
        if (target != null)
        {
            OnHit?.Invoke(target, damage);
        }
    }
}

// 伤害计算器专门负责计算最终伤害（高内聚）
public class DamageCalculator
{
    public int Calculate(int baseDamage, float critChance)
    {
        return Random.value < critChance ? baseDamage * 2 : baseDamage;
    }
}

// 可受伤接口（低耦合）
public interface IDamageable
{
    void TakeDamage(int amount);
}

// Enemy 实现接口，内部处理自己的血量逻辑（高内聚）
public class Enemy : MonoBehaviour, IDamageable
{
    private int hp = 100;
    public event Action<int> OnHPChanged;

    public void TakeDamage(int amount)
    {
        hp = Mathf.Max(0, hp - amount);
        OnHPChanged?.Invoke(hp);
    }
}

// UI 监听事件更新自己（低耦合，高内聚）
public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private Enemy enemy;

    void OnEnable() => enemy.OnHPChanged += UpdateBar;
    void OnDisable() => enemy.OnHPChanged -= UpdateBar;

    void UpdateBar(int hp) { /* 更新 UI */ }
}
```

重构后，每个类都只做一件事（高内聚），类之间通过接口和事件通信而不依赖实现细节（低耦合）。

---

## 小结

| | 高内聚 | 低内聚 |
|---|---|---|
| 特征 | 一个类专注于一个职责 | 一个类管多件不相关的事 |
| 修改代价 | 低：改动只影响这一个职责 | 高：改动一件事可能影响其他职责 |
| 可测试性 | 高：可以单独测试一个职责 | 低：测试一件事要初始化很多无关依赖 |
| 可复用性 | 高：职责清晰的类很容易在其他地方复用 | 低："杂货铺"很难被完整搬走 |

| | 低耦合 | 高耦合 |
|---|---|---|
| 特征 | 通过接口/事件通信 | 直接依赖内部实现 |
| 修改代价 | 低：修改一个模块不影响其他模块 | 高：修改一个地方需要同步修改所有依赖它的地方 |
| 可测试性 | 高：可以用 Mock 替换依赖，单独测试 | 低：测试一个类需要构建整个依赖链 |
| 可替换性 | 高：只要接口不变，实现可以随时换 | 低：实现绑定死了，换掉成本极高 |

**高内聚、低耦合**是衡量代码质量最重要的两个维度。后面五篇讲 SOLID，每一条原则都是在从不同角度逼近这个目标。
