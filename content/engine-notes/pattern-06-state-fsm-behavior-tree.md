---
title: "游戏编程设计模式 06｜State 模式 vs FSM vs 行为树：三种状态管理方式的适用边界"
description: "从最简单的枚举状态机，到有层次结构的 FSM，到支持复杂 AI 逻辑的行为树——这三种状态管理方式解决的问题规模不同。这篇用具体案例讲清楚它们各自的实现和适用场景。"
slug: "pattern-06-state-fsm-behavior-tree"
weight: 737
tags:
  - 软件工程
  - 设计模式
  - State
  - FSM
  - 行为树
  - AI
  - 游戏架构
series: "游戏编程设计模式"
---

> AI 的状态管理是游戏里"状态爆炸"问题最集中的地方。
>
> 一个简单的敌人：待机 → 发现玩家 → 追击 → 攻击 → 受击 → 死亡。这 6 个状态之间有多少种转换？随着逻辑越来越复杂，状态数和转换数成平方增长——这就是为什么需要专门的工具来管理它。

---

## 第零层：枚举 + if/else（能跑，但上限低）

最直觉的实现：用枚举表示状态，在 Update 里 switch。

```csharp
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Hurt, Dead }
    private State currentState = State.Idle;

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                if (CanSeePlayer()) currentState = State.Chase;
                break;

            case State.Chase:
                MoveTowardsPlayer();
                if (IsInAttackRange()) currentState = State.Attack;
                else if (!CanSeePlayer()) currentState = State.Idle;
                break;

            case State.Attack:
                PerformAttack();
                if (!IsInAttackRange()) currentState = State.Chase;
                break;

            case State.Hurt:
                // 播放受击动画，结束后恢复
                if (hurtAnimationDone) currentState = State.Chase;
                break;

            case State.Dead:
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0) currentState = State.Dead;
        else currentState = State.Hurt;
    }
}
```

**适用**：状态少（3~5 个）、逻辑简单、不需要重用、不需要跨对象共享。

**上限**：状态增加时，switch 里的代码急速膨胀。10 个状态、每个状态有 3~5 个转换条件，就是 30~50 个 if/else 分支。开始出现 Code Smell 12 里的"Switch 语句"问题。

---

## 第一层：State 模式（GoF 设计模式）

把每个状态的逻辑封装成独立的类，状态切换就是换一个状态对象。

```csharp
// 状态接口
public interface IEnemyState
{
    void Enter(EnemyController enemy);   // 进入状态时执行一次
    void Update(EnemyController enemy);  // 每帧执行
    void Exit(EnemyController enemy);    // 离开状态时执行一次
}

// 宿主（状态机持有者）
public class EnemyController : MonoBehaviour
{
    private IEnemyState currentState;

    public void TransitionTo(IEnemyState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    void Start() => TransitionTo(new IdleState());

    void Update() => currentState?.Update(this);

    // 暴露给状态类使用的数据和方法
    public Transform PlayerTransform => GameManager.Instance.Player.transform;
    public float DistanceToPlayer => Vector3.Distance(transform.position, PlayerTransform.position);
    public int HP { get; private set; } = 100;
    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0) TransitionTo(new DeadState());
        else TransitionTo(new HurtState());
    }
}
```

各个状态类：

```csharp
public class IdleState : IEnemyState
{
    public void Enter(EnemyController enemy)
    {
        enemy.GetComponent<Animator>().SetTrigger("Idle");
    }

    public void Update(EnemyController enemy)
    {
        if (enemy.DistanceToPlayer < 15f)
            enemy.TransitionTo(new ChaseState());
    }

    public void Exit(EnemyController enemy) { }
}

public class ChaseState : IEnemyState
{
    private UnityEngine.AI.NavMeshAgent agent;

    public void Enter(EnemyController enemy)
    {
        agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.isStopped = false;
    }

    public void Update(EnemyController enemy)
    {
        agent.SetDestination(enemy.PlayerTransform.position);

        if (enemy.DistanceToPlayer < 2f)
            enemy.TransitionTo(new AttackState());
        else if (enemy.DistanceToPlayer > 20f)
            enemy.TransitionTo(new IdleState());
    }

    public void Exit(EnemyController enemy)
    {
        agent.isStopped = true;
    }
}

public class AttackState : IEnemyState
{
    private float attackTimer;
    private const float ATTACK_INTERVAL = 1.5f;

    public void Enter(EnemyController enemy)
    {
        attackTimer = 0f;
    }

    public void Update(EnemyController enemy)
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= ATTACK_INTERVAL)
        {
            PerformAttack(enemy);
            attackTimer = 0f;
        }

        if (enemy.DistanceToPlayer > 3f)
            enemy.TransitionTo(new ChaseState());
    }

    public void Exit(EnemyController enemy) { }

    void PerformAttack(EnemyController enemy)
    {
        // 攻击逻辑
    }
}

public class HurtState : IEnemyState
{
    private float hurtDuration = 0.5f;
    private float timer;

    public void Enter(EnemyController enemy)
    {
        timer = 0f;
        enemy.GetComponent<Animator>().SetTrigger("Hurt");
    }

    public void Update(EnemyController enemy)
    {
        timer += Time.deltaTime;
        if (timer >= hurtDuration)
            enemy.TransitionTo(new ChaseState()); // 受击结束后恢复追击
    }

    public void Exit(EnemyController enemy) { }
}

public class DeadState : IEnemyState
{
    public void Enter(EnemyController enemy)
    {
        enemy.GetComponent<Animator>().SetTrigger("Die");
        enemy.GetComponent<Collider>().enabled = false;
        Object.Destroy(enemy.gameObject, 2f);
    }

    public void Update(EnemyController enemy) { }
    public void Exit(EnemyController enemy) { }
}
```

**优点**：
- 每个状态的逻辑独立封装，互不干扰
- Enter/Exit 钩子清晰，状态切换时的副作用（播动画、停止移动）有明确的位置
- 容易添加新状态，不影响已有状态

**上限**：状态之间的转换逻辑分散在各个状态类里——`ChaseState` 知道自己会转换到 `AttackState` 和 `IdleState`，这两个类之间有耦合。随着状态增多，转换关系变得难以全局把握。

---

## 第二层：有层次的 FSM（Hierarchical FSM）

很多游戏 AI 的状态是有层次的：

```
顶层状态：战斗模式 / 巡逻模式
  战斗模式下的子状态：追击 / 攻击 / 撤退 / 受击
  巡逻模式下的子状态：待机 / 巡逻 / 警觉
```

受击状态不管在战斗还是巡逻，行为都一样——如果用平坦的 FSM，必须重复实现受击状态两次。

分层 FSM 允许：
- 父状态处理通用行为（比如受到任何伤害都进入 HurtState）
- 子状态处理具体行为
- 子状态可以继承父状态的转换规则

在游戏项目中，分层 FSM 通常用 AnimatorController（Unity 的动画状态机）实现，程序侧用参数控制状态转换，而不是手写分层 FSM。

---

## 第三层：行为树（Behavior Tree）

当敌人的逻辑复杂到"需要在多个条件下组合多种行为"时，FSM 开始力不从心。比如：

```
如果 HP < 30%：
  如果附近有队友：请求支援，然后撤退
  否则：发狂攻击
否则，如果玩家在攻击范围内：
  如果上次攻击冷却结束：执行攻击
  否则：绕行保持距离
否则，如果能看到玩家：
  导航接近玩家
否则：
  巡逻直到再次发现玩家
```

这种嵌套的条件+行为组合，用 FSM 实现会非常混乱（状态数和转换数爆炸），但用行为树表达却非常自然。

### 行为树的核心概念

行为树由**节点**组成，每个节点执行后返回三种结果之一：
- `Success`：成功
- `Failure`：失败
- `Running`：正在执行中（需要持续更新）

节点类型：

**叶节点（Leaf）**：实际的行为或条件检查
```
Action：执行一个行为（"移动到目标位置"、"播放攻击动画"）
Condition：检查一个条件（"玩家在视野范围内？"、"HP > 50%？"）
```

**组合节点（Composite）**：组织子节点的执行方式
```
Sequence（顺序）：从左到右依次执行，任意一个失败则整体失败（逻辑与）
Selector（选择）：从左到右依次尝试，任意一个成功则整体成功（逻辑或）
```

**装饰节点（Decorator）**：修改子节点的行为
```
Inverter：翻转子节点的结果（成功变失败，失败变成功）
Repeater：重复执行子节点 N 次
UntilSuccess：持续执行直到成功
```

### 行为树的最小实现

```csharp
public enum BehaviorStatus { Success, Failure, Running }

// 所有节点的基类
public abstract class BehaviorNode
{
    public abstract BehaviorStatus Update(EnemyBlackboard bb);
}

// Sequence：全部成功才成功
public class SequenceNode : BehaviorNode
{
    private readonly List<BehaviorNode> children;
    private int currentIndex = 0;

    public SequenceNode(params BehaviorNode[] children)
    {
        this.children = new List<BehaviorNode>(children);
    }

    public override BehaviorStatus Update(EnemyBlackboard bb)
    {
        while (currentIndex < children.Count)
        {
            var status = children[currentIndex].Update(bb);
            if (status == BehaviorStatus.Running) return BehaviorStatus.Running;
            if (status == BehaviorStatus.Failure) { currentIndex = 0; return BehaviorStatus.Failure; }
            currentIndex++;
        }
        currentIndex = 0;
        return BehaviorStatus.Success;
    }
}

// Selector：任意成功即成功
public class SelectorNode : BehaviorNode
{
    private readonly List<BehaviorNode> children;

    public SelectorNode(params BehaviorNode[] children)
    {
        this.children = new List<BehaviorNode>(children);
    }

    public override BehaviorStatus Update(EnemyBlackboard bb)
    {
        foreach (var child in children)
        {
            var status = child.Update(bb);
            if (status == BehaviorStatus.Running) return BehaviorStatus.Running;
            if (status == BehaviorStatus.Success) return BehaviorStatus.Success;
        }
        return BehaviorStatus.Failure;
    }
}

// 黑板：存储 AI 需要共享的数据
public class EnemyBlackboard
{
    public Transform self;
    public Transform player;
    public float hp;
    public float maxHP;
    public NavMeshAgent agent;
}

// 叶节点示例
public class IsPlayerInRangeCondition : BehaviorNode
{
    private readonly float range;
    public IsPlayerInRangeCondition(float range) => this.range = range;

    public override BehaviorStatus Update(EnemyBlackboard bb)
    {
        float dist = Vector3.Distance(bb.self.position, bb.player.position);
        return dist <= range ? BehaviorStatus.Success : BehaviorStatus.Failure;
    }
}

public class MoveToPlayerAction : BehaviorNode
{
    public override BehaviorStatus Update(EnemyBlackboard bb)
    {
        bb.agent.SetDestination(bb.player.position);
        float dist = Vector3.Distance(bb.self.position, bb.player.position);
        if (dist > 2f) return BehaviorStatus.Running;
        return BehaviorStatus.Success;
    }
}

public class AttackAction : BehaviorNode
{
    private float cooldown = 1.5f;
    private float lastAttackTime = float.MinValue;

    public override BehaviorStatus Update(EnemyBlackboard bb)
    {
        if (Time.time - lastAttackTime < cooldown)
            return BehaviorStatus.Failure;

        // 执行攻击
        lastAttackTime = Time.time;
        return BehaviorStatus.Success;
    }
}
```

组装完整的行为树：

```csharp
// 构建 AI 的行为树
public class EnemyAI : MonoBehaviour
{
    private BehaviorNode behaviorTree;
    private EnemyBlackboard blackboard;

    void Start()
    {
        blackboard = new EnemyBlackboard
        {
            self = transform,
            player = GameManager.Instance.Player.transform,
            hp = 100,
            maxHP = 100,
            agent = GetComponent<NavMeshAgent>()
        };

        // 用代码描述 AI 逻辑
        behaviorTree = new SelectorNode(
            // 选项1：如果在攻击范围内，就攻击
            new SequenceNode(
                new IsPlayerInRangeCondition(2f),
                new AttackAction()
            ),
            // 选项2：如果能看到玩家，就追击
            new SequenceNode(
                new IsPlayerInRangeCondition(15f),
                new MoveToPlayerAction()
            )
            // 选项3：如果上面都失败（玩家不在视野内），默认巡逻
            // new PatrolAction()
        );
    }

    void Update()
    {
        behaviorTree.Update(blackboard);
    }
}
```

---

## 三种方式的选择标准

| 方式 | 适用场景 | 状态数 | 逻辑复杂度 |
|---|---|---|---|
| 枚举 + switch | 简单 UI 状态机、轻量级状态 | ≤5 | 低 |
| State 模式 | 游戏角色 AI、清晰的状态切换逻辑 | 5~15 | 中 |
| 行为树 | 复杂敌人 AI、Boss AI、需要策划可视化编辑 | 不限 | 高 |

实际项目里，通常会**混用**：玩家角色用 State 模式（状态清晰，程序控制），复杂 Boss 用行为树（策划可以在编辑器里调整逻辑），简单的 UI 流程用枚举。

---

## 小结

- **枚举 + switch**：快速原型，状态少时够用，不要在状态超过 5 个后继续用
- **State 模式**：状态逻辑清晰封装，Enter/Exit 钩子优雅，适合中等复杂度的角色 AI
- **行为树**：条件嵌套、模块复用、策划可编辑，适合复杂 AI；Unity 里可以用 NodeCanvas、Behavior Designer 等成熟插件，不需要从零实现
- **黑板（Blackboard）**：行为树节点之间共享数据的标准方式，把 AI 状态集中管理
