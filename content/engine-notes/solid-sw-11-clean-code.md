---
title: "软件工程基础 11｜Clean Code 基础：命名、函数、注释——让代码自解释"
description: "好的代码应该像好的散文：清晰、直接、无需注解就能被理解。这篇从命名、函数设计、注释三个维度，讲游戏代码里最实用的 Clean Code 规范。"
slug: "solid-sw-11-clean-code"
weight: 721
tags:
  - 软件工程
  - Clean Code
  - 命名
  - 代码质量
  - 可读性
series: "软件工程基础与 SOLID 原则"
---

> Robert C. Martin 在《代码整洁之道》里说：**代码是写给人读的，顺便让机器执行。**
>
> 这话不是在说"写注释"。它说的是：代码本身的结构和命名，应该清晰到不需要注释就能被理解。注释是无法避免时的最后手段，而不是解释烂代码的工具。

---

## 第一部分：命名

命名是程序员每天做最多次的决策，也是影响代码可读性最大的单一因素。

### 命名要揭示意图

```csharp
// 差：名字没有揭示任何信息
int d; // 这是什么？
List<int> list1;
bool flag;
void Process();
void DoStuff(int x);

// 好：名字就是文档
int daysSinceLastSave;
List<int> activeEnemyIds;
bool isPlayerInvincible;
void ApplyDamageToTarget();
void CalculateFinalDamage(int baseDamage);
```

衡量标准：**看到名字，不需要注释就能知道它是什么、做什么**。

### 命名要表达意图，不表达实现

```csharp
// 差：名字描述的是"怎么做"（实现细节）
List<Enemy> enemyArray;       // 以后如果从 Array 换成 List，名字就错了
void LoopThroughEnemies();    // 实现可能改成 LINQ，但意图是"处理所有敌人"
int hpIntValue;               // int 是显而易见的类型，不需要放名字里

// 好：名字描述的是"是什么"（意图）
List<Enemy> activeEnemies;
void ProcessAllEnemies();
int currentHP;
```

### 命名要有区分度

```csharp
// 差：名字太相似，读者容易混淆
void GetPlayerData() { }
void FetchPlayerData() { }
void RetrievePlayerData() { }
// 这三个名字的区别是什么？做的事情有什么不同？

// 好：如果确实有区别，名字要能体现区别
void GetCachedPlayerData() { }     // 从内存缓存取
void FetchPlayerDataFromDisk() { } // 从磁盘读
void FetchPlayerDataFromCloud() { } // 从网络拉
```

### 游戏代码中的命名规范

```csharp
// 布尔值：用 is/has/can/should 前缀，读起来像句子
bool isGrounded;
bool hasJumped;
bool canPickUpItem;
bool shouldUpdateUI;

// 集合：用复数名词
List<Enemy> enemies;
Dictionary<string, Skill> skills;
int[] enemyIds;

// 方法：动词 + 名词（描述动作）
void SpawnEnemy();
void CalculateDamage();
bool TryPickUpItem();   // Try 前缀表示可能失败，不抛异常
void OnPlayerDied();    // On 前缀表示事件响应

// 常量：全大写，下划线分隔
const int MAX_INVENTORY_SIZE = 20;
const float GRAVITY = -9.81f;

// 私有字段（Unity 风格）
[SerializeField] private int maxHP;  // 或 _maxHP，团队统一即可

// 事件：名词描述事件内容，或 OnXxx 命名
public event Action PlayerDied;
public event Action<int> HPChanged; // 参数代表新的 HP 值
```

### 避免魔法数字

```csharp
// 差：数字没有名字，读代码的人不知道这些数字代表什么
void Update()
{
    if (Input.GetKeyDown(KeyCode.E) && Vector3.Distance(transform.position, target.position) < 2.5f)
        Interact();

    if (health < 30)
        ShowLowHealthWarning();
}

// 好：数字有了名字，代码自解释
private const float INTERACTION_RANGE = 2.5f;
private const int LOW_HEALTH_THRESHOLD = 30;

void Update()
{
    if (Input.GetKeyDown(KeyCode.E) && IsWithinInteractionRange())
        Interact();

    if (health < LOW_HEALTH_THRESHOLD)
        ShowLowHealthWarning();
}

bool IsWithinInteractionRange()
    => Vector3.Distance(transform.position, target.position) < INTERACTION_RANGE;
```

---

## 第二部分：函数

### 函数要小，只做一件事

```csharp
// 差：这个函数做了太多事
void HandleEnemyDeath(Enemy enemy)
{
    // 1. 播放死亡动画
    enemy.animator.SetTrigger("Die");

    // 2. 给玩家经验
    player.stats.experience += enemy.expReward;

    // 3. 掉落物品
    foreach (var loot in enemy.lootTable)
        if (Random.value < loot.dropChance)
            SpawnLootItem(loot.item, enemy.transform.position);

    // 4. 检查是否完成击杀任务
    questManager.CheckKillObjectives(enemy.type);

    // 5. 播放击杀音效
    audioManager.Play(killSound, enemy.transform.position);

    // 6. 更新成就进度
    achievementTracker.OnEnemyKilled(enemy.type);

    // 7. 延迟销毁
    Destroy(enemy.gameObject, 2f);
}
```

这个函数做了 7 件不同的事，如果任何一件事的逻辑需要改变，都要改这个函数。

```csharp
// 好：每个函数只做一件事，命名说明做什么
void HandleEnemyDeath(Enemy enemy)
{
    // 函数主体只是"协调"，不包含具体逻辑
    enemy.PlayDeathAnimation();
    AwardExperience(enemy);
    SpawnLoot(enemy);
    NotifyQuestSystem(enemy);
    PlayKillFeedback(enemy);
    ScheduleDestruction(enemy);
}

void AwardExperience(Enemy enemy)
{
    player.stats.GainExperience(enemy.expReward);
}

void SpawnLoot(Enemy enemy)
{
    foreach (var loot in enemy.lootTable)
        if (Random.value < loot.dropChance)
            LootSpawner.Spawn(loot.item, enemy.transform.position);
}

// ... 每个函数都很短，名字说明了全部
```

### 函数参数：越少越好

函数参数超过 3 个时，应该考虑把参数打包成一个对象，或者重新考虑函数的职责。

```csharp
// 差：参数太多，调用时难以理解每个参数的含义
SpawnEnemy("Goblin", 100, 20, 5, true, false, Vector3.zero, Quaternion.identity);

// 好：参数少，或者打包成有意义的对象
var spawnConfig = new EnemySpawnConfig
{
    enemyType = "Goblin",
    hp = 100,
    attackPower = 20,
    defense = 5,
    isElite = true
};
SpawnEnemy(spawnConfig, Vector3.zero);
```

### 函数不应该有副作用（尽量）

```csharp
// 差：函数名是"检查"，但实际上修改了状态（有副作用）
bool CheckAndUpdateHealth(int damage)
{
    hp -= damage; // 这个副作用从名字里看不出来！
    return hp <= 0;
}

// 好：功能分离，名字和行为一致
bool IsDead() => hp <= 0;
void TakeDamage(int damage) { hp -= damage; }

// 调用时意图清晰
entity.TakeDamage(damage);
if (entity.IsDead()) HandleDeath(entity);
```

---

## 第三部分：注释

### 好注释的类型

**解释"为什么"，而不是"是什么"**：

```csharp
// 差：注释重复了代码的内容（代码本身已经很清楚）
// 把 hp 减去 damage
hp -= damage;

// 好：解释了代码背后的原因（代码本身看不出来）
// 使用 Mathf.Max 是因为某些 Buff 可以让伤害变成负数（治疗）
// 如果不限制，负伤害会让 HP 超过上限
hp = Mathf.Min(maxHP, hp - Mathf.Min(damage, 0) + Mathf.Max(damage, 0));
```

实际上，如果逻辑真的这么复杂，更好的方案是把它提取成方法：

```csharp
// 更好：函数名就是文档
hp = ApplyDamageWithHealingSupport(hp, maxHP, damage);
```

**警告**：记录危险或反直觉的地方：

```csharp
// 警告：Unity 的 Destroy() 不是立即生效的，gameObject 在同一帧内仍然有效
// 如果需要立即失效，使用 DestroyImmediate()（只在编辑器模式下使用）
// 或者先设置一个 isDestroyed 标志位
Destroy(gameObject);
```

**TODO/FIXME**：记录已知的技术债（需要配合 Issue Tracker 使用）：

```csharp
// TODO: 当前是 O(n²) 的暴力检测，敌人超过 100 个时需要换成空间哈希
// Ref: Issue #234
foreach (Enemy a in enemies)
    foreach (Enemy b in enemies)
        if (a != b) CheckProximity(a, b);
```

### 坏注释的类型

**过时的注释**（比没有注释更危险）：

```csharp
// 玩家只能跳一次
// （代码实际上已经支持二段跳，这个注释从来没更新）
void Jump()
{
    if (jumpCount < maxJumps) // maxJumps 现在可以是 2
    {
        velocity.y = jumpForce;
        jumpCount++;
    }
}
```

**注释掉的代码**（用版本控制来追踪历史，不要留注释代码）：

```csharp
// 差：大量注释掉的代码让文件难以阅读
// void OldAttackSystem()
// {
//     // ... 50 行旧代码
// }

// if (useOldSystem)
//     OldAttackSystem();
// else
    NewAttackSystem();
```

**显而易见的注释**（噪音，消耗读者注意力）：

```csharp
// 获取玩家位置
Vector3 playerPos = player.transform.position;

// 增加经验值
experience += amount;
```

---

## 代码格式与一致性

最后一条常被忽视的 Clean Code 原则：**格式要一致**。

格式不统一的代码会增加读者的认知负担——读者需要不停地"解码"不同的格式风格，而不是专注于代码逻辑。

在团队项目里，建议：

1. **用 EditorConfig 或 .editorconfig 文件**统一缩进、换行规则
2. **用 Roslyn Analyzer 或 StyleCop**自动检测违反规范的代码
3. **最重要的原则**：团队内要一致，具体用哪种风格（tabs vs spaces、大括号换不换行）是次要的

---

## 小结

Clean Code 的核心是**让代码成为自己的文档**：

| 方面 | 核心原则 |
|---|---|
| 命名 | 揭示意图，不表达实现；避免魔法数字；布尔值用 is/has/can 前缀 |
| 函数 | 短小、只做一件事；参数越少越好；避免隐藏的副作用 |
| 注释 | 解释"为什么"，不解释"是什么"；警告危险；不留注释掉的代码 |
| 格式 | 团队内一致；用工具自动检查和格式化 |

Clean Code 不是绝对的规则，而是在"每次修改都需要理解代码"这个成本下，持续降低这个成本的努力。
