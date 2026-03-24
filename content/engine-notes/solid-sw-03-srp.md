---
title: "软件工程基础 03｜单一职责原则（SRP）：一个类只做一件事"
description: "SRP 的真正含义不是『一个类只有一个方法』，而是『一个类只有一个变化的理由』。这篇通过游戏中的典型案例，解释什么是职责、如何识别违反、如何正确拆分。"
slug: "solid-sw-03-srp"
weight: 705
tags:
  - 软件工程
  - SOLID
  - SRP
  - 单一职责
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> **SRP（Single Responsibility Principle）：一个类应该只有一个变化的理由。**
>
> 注意，原文说的是"变化的理由"，不是"一个方法"或"一个功能"。理解这个区别，是理解 SRP 的关键。

这可能是 SOLID 中被误解最多的原则。很多人把它理解成"一个类里只能有一个方法"或者"类要尽量小"，于是写出了几十个只有一个方法的微型类，拆得四分五裂，反而更难维护。

SRP 真正说的是：**一个类应该只有一个让它需要改变的理由**。

---

## 理解"变化的理由"

"变化的理由"是一个更精确的说法。它把问题的关注点从"这个类有多少代码"转移到"谁会要求这个类改变"。

Robert C. Martin（SOLID 原则的提出者）有一个表述很清晰：

> "一个类的职责，是指它的变化来源。如果你能想到两个不同的原因让这个类需要改变，那么这个类就有两个职责，违反了 SRP。"

在游戏开发里，"谁会要求这个类改变"通常对应着**不同的需求来源**：

- 策划改了战斗数值平衡 → 伤害计算逻辑需要改变
- 美术要求更新 UI 视觉风格 → UI 渲染逻辑需要改变
- 后端接入新的存档服务 → 存档序列化逻辑需要改变

如果这三件事都会导致同一个类改变，那这个类违反了 SRP。

---

## 违反 SRP 的典型案例：PlayerCharacter

下面这个类在游戏项目中非常常见：

```csharp
public class PlayerCharacter : MonoBehaviour
{
    // --- 属性数据 ---
    public int maxHP = 100;
    public int currentHP;
    public int attackPower = 20;
    public int defense = 10;
    public int level = 1;
    public int experience = 0;

    // --- 移动逻辑 ---
    private CharacterController characterController;
    private Vector3 velocity;
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;

    void UpdateMovement()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        characterController.Move(move * moveSpeed * Time.deltaTime);
        if (Input.GetButtonDown("Jump"))
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        velocity.y += Physics.gravity.y * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // --- 战斗逻辑 ---
    public void TakeDamage(int incomingDamage)
    {
        int finalDamage = Mathf.Max(0, incomingDamage - defense);
        currentHP -= finalDamage;
        UpdateHPBar();      // ← 顺手更新了 UI
        PlayHurtSound();    // ← 顺手播了音效
        if (currentHP <= 0) Die();
    }

    public void Attack(IDamageable target)
    {
        int damage = attackPower;
        if (Random.value < 0.15f) damage = (int)(damage * 1.5f); // 暴击
        target.TakeDamage(damage);
        PlayAttackSound();  // ← 顺手播了音效
    }

    // --- 升级/经验逻辑 ---
    public void GainExperience(int amount)
    {
        experience += amount;
        int expRequired = level * 100;
        if (experience >= expRequired)
        {
            experience -= expRequired;
            level++;
            attackPower += 5;
            defense += 2;
            maxHP += 20;
            currentHP = maxHP;
            ShowLevelUpEffect();    // ← 顺手播了特效
            SaveProgress();         // ← 顺手存档了
        }
    }

    // --- 存档逻辑 ---
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("level", level);
        PlayerPrefs.SetInt("exp", experience);
        PlayerPrefs.SetInt("hp", currentHP);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        level = PlayerPrefs.GetInt("level", 1);
        experience = PlayerPrefs.GetInt("exp", 0);
        currentHP = PlayerPrefs.GetInt("hp", 100);
    }

    // --- UI 更新 ---
    [SerializeField] private UnityEngine.UI.Slider hpBarSlider;
    void UpdateHPBar()
    {
        hpBarSlider.value = (float)currentHP / maxHP;
    }

    // --- 音效 ---
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip hurtClip;
    private AudioSource audioSource;

    void PlayAttackSound() => audioSource.PlayOneShot(attackClip);
    void PlayHurtSound() => audioSource.PlayOneShot(hurtClip);

    // --- 死亡逻辑 ---
    void Die()
    {
        // 播放死亡动画
        GetComponent<Animator>().SetTrigger("Die");
        // 触发游戏结束
        GameManager.Instance.GameOver();
        // 存档（清除存档？保存死亡次数？）
        PlayerPrefs.SetInt("deathCount", PlayerPrefs.GetInt("deathCount", 0) + 1);
    }

    // --- 视觉特效 ---
    void ShowLevelUpEffect()
    {
        // 实例化升级粒子特效
        Instantiate(levelUpEffectPrefab, transform.position, Quaternion.identity);
    }
    [SerializeField] private GameObject levelUpEffectPrefab;
}
```

这个类有多少个"变化的理由"？

1. 策划调整移动手感 → `UpdateMovement` 需要改
2. 策划调整战斗数值 → `TakeDamage`、`Attack` 需要改
3. 策划调整升级曲线 → `GainExperience` 需要改
4. 改为云存档 → `SaveProgress`、`LoadProgress` 需要改
5. UI 设计师改 HP 条样式 → `UpdateHPBar` 需要改
6. 音频工程师换音效播放方式 → 所有 Play 方法需要改
7. TA 改特效系统 → `ShowLevelUpEffect` 需要改
8. 策划改死亡机制 → `Die` 需要改

**这个类有 8 个以上的变化理由**，任何一个方向的需求变更都可能导致它被修改。

---

## 识别 SRP 违反的方法

### 方法一：数"变化的理由"

直接数：这个类有多少种不同的需求来源会导致它改变？超过一个就有问题。

### 方法二：问"这个类是谁用的"

在游戏项目里，不同的职能人员对应不同的需求：

- 策划（战斗设计师）关心伤害计算
- 策划（关卡设计师）关心移动手感
- 美术关心视觉效果
- 音频关心声音
- 程序关心系统架构

如果一个类被多个职能角色"关心"，它很可能混合了多个职责。

### 方法三：尝试写单元测试

如果为这个类写单元测试时，需要 Mock 或初始化很多不相关的依赖（比如测试伤害计算，但必须先初始化 AudioSource 和 UI），这个类的职责很可能不够单一。

---

## 正确的拆分：按变化维度划分

把上面的 `PlayerCharacter` 按照"变化的理由"拆开：

```csharp
// 1. 只管属性数据（数值策划关心）
[System.Serializable]
public class PlayerStats
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int AttackPower { get; private set; }
    public int Defense { get; private set; }
    public int Level { get; private set; }
    public int Experience { get; private set; }

    public event Action<int, int> OnHPChanged; // (current, max)
    public event Action<int> OnLevelUp;

    public void TakeDamage(int incomingDamage)
    {
        int finalDamage = Mathf.Max(0, incomingDamage - Defense);
        CurrentHP = Mathf.Max(0, CurrentHP - finalDamage);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void GainExperience(int amount)
    {
        Experience += amount;
        int required = Level * 100;
        if (Experience >= required)
        {
            Experience -= required;
            Level++;
            AttackPower += 5;
            Defense += 2;
            MaxHP += 20;
            CurrentHP = MaxHP;
            OnLevelUp?.Invoke(Level);
        }
    }
}

// 2. 只管移动（关卡策划/手感程序关心）
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    private CharacterController controller;
    private Vector3 velocity;

    void Update()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        controller.Move(move * moveSpeed * Time.deltaTime);
        if (Input.GetButtonDown("Jump") && IsGrounded())
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    bool IsGrounded() => controller.isGrounded;
}

// 3. 只管战斗行为（战斗策划关心）
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    public event Action<IDamageable, int> OnAttack;

    public void PerformAttack(IDamageable target)
    {
        int damage = stats.AttackPower;
        bool isCrit = Random.value < 0.15f;
        if (isCrit) damage = (int)(damage * 1.5f);
        target.TakeDamage(damage);
        OnAttack?.Invoke(target, damage);
    }
}

// 4. 只管存档（后端/程序关心）
public class PlayerSaveSystem
{
    public void Save(PlayerStats stats)
    {
        PlayerPrefs.SetInt("level", stats.Level);
        PlayerPrefs.SetInt("exp", stats.Experience);
        PlayerPrefs.SetInt("hp", stats.CurrentHP);
        PlayerPrefs.Save();
    }

    public SaveData Load()
    {
        return new SaveData
        {
            level = PlayerPrefs.GetInt("level", 1),
            experience = PlayerPrefs.GetInt("exp", 0),
            hp = PlayerPrefs.GetInt("hp", 100)
        };
    }
}

// 5. 只管 HP 条 UI（UI/美术关心）
public class HPBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private PlayerStats stats;

    void OnEnable() => stats.OnHPChanged += UpdateBar;
    void OnDisable() => stats.OnHPChanged -= UpdateBar;

    void UpdateBar(int current, int max)
    {
        slider.value = (float)current / max;
    }
}
```

现在每个类只有一个变化的理由：

- 策划改数值 → 只改 `PlayerStats`
- 策划改移动手感 → 只改 `PlayerMovement`
- 后端接入云存档 → 只改 `PlayerSaveSystem`
- UI 改血条样式 → 只改 `HPBarUI`

任何一个方向的需求变化，都只会触碰一个文件。

---

## SRP 的常见误区

### 误区一：类越小越好

SRP 不是"类越小越好"。把一个方法拆成一个类，同样是过度拆分。

判断标准始终是"变化的理由"：如果两段代码总是因为同一件事一起改变，把它们放在一个类里是对的。

```csharp
// 过度拆分：没有意义
public class DamageAdder
{
    public int Add(int a, int b) => a + b;
}
public class DamageMultiplier
{
    public float Multiply(int damage, float multiplier) => damage * multiplier;
}
public class CriticalDamageCalculator
{
    public int Calculate(int base, float critMult, bool isCrit)
    {
        return isCrit ? (int)(base * critMult) : base;
    }
}
// 这三个类应该放在一个 DamageCalculator 里
```

### 误区二：把 SRP 当成禁止方法数量多

一个类有 20 个方法，但所有方法都围绕同一个职责，仍然符合 SRP。

```csharp
// 有很多方法，但所有方法都在管"伤害计算"——符合 SRP
public class DamageCalculator
{
    public int CalculatePhysicalDamage(int attack, int defense) { ... }
    public int CalculateMagicalDamage(int power, int resistance) { ... }
    public int ApplyCritical(int damage, float critMultiplier) { ... }
    public int ApplyElementalBonus(int damage, ElementType element, ElementType targetWeakness) { ... }
    public int ClampDamage(int damage, int minDamage) { ... }
    // 全都是伤害计算逻辑，一个职责
}
```

### 误区三：提前过度拆分

当一个系统还很简单，不需要为了 SRP 预先拆分成很多层。等到"有另一个变化的理由出现"时再拆。

**YAGNI（You Aren't Gonna Need It）**——不需要的东西不要提前加。

---

## SRP 在游戏中的一个经典案例：存档系统

这是一个 SRP 违反最容易出现的地方。

很多游戏的存档是直接在 `GameManager` 里写的，这意味着：

- 换存档格式（从 PlayerPrefs 换到 JSON 文件）需要改 `GameManager`
- 存档加密 → 改 `GameManager`
- 云存档接入 → 改 `GameManager`
- 增加存档槽位 → 改 `GameManager`

正确的做法是把存档的"格式"和"触发时机"分开：

```csharp
// 负责"存什么"和"怎么序列化"（可以换实现）
public interface ISaveSystem
{
    void Save(GameSaveData data);
    GameSaveData Load();
}

// 具体实现1：本地 JSON 存档
public class LocalJsonSaveSystem : ISaveSystem
{
    private string savePath = Application.persistentDataPath + "/save.json";

    public void Save(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(savePath, json);
    }

    public GameSaveData Load()
    {
        if (!File.Exists(savePath)) return new GameSaveData();
        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<GameSaveData>(json);
    }
}

// 具体实现2：云存档（日后接入时，GameManager 代码不需要改）
public class CloudSaveSystem : ISaveSystem
{
    public async void Save(GameSaveData data) { /* 调云端 API */ }
    public GameSaveData Load() { /* 从云端拉数据 */ }
}

// GameManager 只管"什么时候存"，不管"怎么存"
public class GameManager : MonoBehaviour
{
    [SerializeField] private ISaveSystem saveSystem; // 通过 Inspector 或注入

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            saveSystem.Save(CollectSaveData());
    }
}
```

---

## 小结

- **SRP 的本质**：一个类只有一个让它改变的理由
- **识别违反**：数"变化的理由"，问"谁会要求这个类改变"
- **拆分的依据**：不是代码行数，而是需求来源是否不同
- **常见误区**：类越小越好（错误），方法数量多就违反 SRP（错误），提前过度拆分（错误）

SRP 是 SOLID 中最直观、影响最广的一条。它是所有后续原则的基础——一个职责单一的类，更容易符合开闭原则（因为不需要因为一个职责变化而影响其他职责），更容易写接口（接口的边界更清晰），更容易测试（依赖更少）。
