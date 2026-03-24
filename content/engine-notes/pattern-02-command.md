---
title: "游戏编程设计模式 02｜Command 模式：操作对象化"
description: "Command 模式把"做一件事"变成了一个可以传递、存储、撤销、重放的对象。这篇通过撤销/重做、技能队列、录像回放三个完整案例，讲清楚 Command 模式在游戏中的实际价值。"
slug: "pattern-02-command"
weight: 729
tags:
  - 软件工程
  - 设计模式
  - Command
  - 游戏架构
series: "游戏编程设计模式"
---

> **Command 模式的核心思想：把一个"操作"封装成一个对象。**
>
> 操作变成对象之后，就可以像对象一样被对待：存进队列、序列化、传递给另一个系统、在未来某个时刻执行、或者执行之后被撤销。

---

## 先看问题：没有 Command 模式时的困境

一个关卡编辑器里，用户可以放置、移动、删除物体。需求是：**支持撤销和重做（Ctrl+Z / Ctrl+Y）**。

没有 Command 模式时，怎么实现撤销？

第一反应可能是"保存每次操作前的完整状态，撤销时恢复"：

```csharp
// 方案一：每次操作前保存整个场景状态
void PlaceObject(GameObject prefab, Vector3 position)
{
    history.Push(SaveFullSceneState()); // 保存整个场景的快照
    Instantiate(prefab, position, Quaternion.identity);
}

void Undo()
{
    if (history.Count > 0)
        RestoreSceneState(history.Pop()); // 恢复整个快照
}
```

这个方案的问题：
1. 每次操作都要序列化整个场景，内存和时间开销极大
2. 场景越复杂（几千个物体），快照越大
3. 网络同步时，传输整个场景状态代价极高

---

## Command 模式的解法：记录"如何撤销"而不是"撤销前的状态"

Command 模式的思路完全不同：不保存场景状态，而是**把每个操作本身记录下来**，让每个操作知道自己怎么被撤销。

```csharp
// 所有操作都实现这个接口
public interface ICommand
{
    void Execute();
    void Undo();
}
```

把"放置物体"封装成一个 Command：

```csharp
public class PlaceObjectCommand : ICommand
{
    private readonly GameObject prefab;
    private readonly Vector3 position;
    private readonly Quaternion rotation;
    private GameObject placedInstance; // 记录实际放置的对象，撤销时销毁它

    public PlaceObjectCommand(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        this.prefab = prefab;
        this.position = position;
        this.rotation = rotation;
    }

    public void Execute()
    {
        placedInstance = Object.Instantiate(prefab, position, rotation);
    }

    public void Undo()
    {
        if (placedInstance != null)
            Object.Destroy(placedInstance);
    }
}

public class MoveObjectCommand : ICommand
{
    private readonly GameObject target;
    private readonly Vector3 from; // 移动前的位置
    private readonly Vector3 to;   // 移动后的位置

    public MoveObjectCommand(GameObject target, Vector3 from, Vector3 to)
    {
        this.target = target;
        this.from = from;
        this.to = to;
    }

    public void Execute() => target.transform.position = to;
    public void Undo()    => target.transform.position = from;
}

public class DeleteObjectCommand : ICommand
{
    private readonly GameObject target;
    private readonly Vector3 position;
    private readonly Quaternion rotation;

    public DeleteObjectCommand(GameObject target)
    {
        this.target = target;
        this.position = target.transform.position;
        this.rotation = target.transform.rotation;
    }

    public void Execute() => target.SetActive(false); // 先隐藏，不立即销毁（撤销时需要恢复）
    public void Undo()    => target.SetActive(true);
}
```

历史记录系统（撤销/重做管理器）：

```csharp
public class CommandHistory
{
    private readonly Stack<ICommand> undoStack = new();
    private readonly Stack<ICommand> redoStack = new();

    public void Execute(ICommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear(); // 执行新操作后，清空重做栈
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;
        var command = undoStack.Pop();
        command.Undo();
        redoStack.Push(command);
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;
        var command = redoStack.Pop();
        command.Execute();
        undoStack.Push(command);
    }

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;
}
```

使用时：

```csharp
public class LevelEditor : MonoBehaviour
{
    private CommandHistory history = new();

    // 用户放置物体
    public void PlaceObject(GameObject prefab, Vector3 pos)
    {
        history.Execute(new PlaceObjectCommand(prefab, pos, Quaternion.identity));
    }

    // 用户移动物体
    public void MoveObject(GameObject obj, Vector3 newPos)
    {
        Vector3 oldPos = obj.transform.position;
        history.Execute(new MoveObjectCommand(obj, oldPos, newPos));
        // 注意：移动完成后再创建 Command，oldPos 已经被记录了
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) history.Undo();
            if (Input.GetKeyDown(KeyCode.Y)) history.Redo();
        }
    }
}
```

---

## 应用二：技能队列与延迟执行

Command 模式的另一个典型游戏应用：把技能操作放入队列，按顺序执行。

这在格斗游戏（连招系统）和回合制 RPG（行动队列）里都很常见。

```csharp
// 技能 Command
public class CastSpellCommand : ICommand
{
    private readonly Character caster;
    private readonly SkillBase skill;
    private readonly Character target;

    public CastSpellCommand(Character caster, SkillBase skill, Character target)
    {
        this.caster = caster;
        this.skill = skill;
        this.target = target;
    }

    public void Execute()
    {
        if (!caster.CanCastSkill(skill)) return;
        caster.ConsumeMana(skill.ManaCost);
        skill.Execute(caster, target);
    }

    public void Undo()
    {
        // 技能通常不可撤销（已造成的伤害无法取消）
        // 但某些情况下可以部分撤销（退还法力消耗）
        caster.RestoreMana(skill.ManaCost);
    }
}

// 回合制战斗的行动队列
public class TurnBasedBattleQueue
{
    private readonly Queue<ICommand> actionQueue = new();

    // 玩家在选择阶段把操作放入队列（不立即执行）
    public void QueueAction(ICommand command)
    {
        actionQueue.Enqueue(command);
    }

    // 执行阶段：按顺序执行所有行动
    public IEnumerator ExecuteAllActions()
    {
        while (actionQueue.Count > 0)
        {
            var command = actionQueue.Dequeue();
            command.Execute();
            yield return new WaitForSeconds(0.5f); // 每个行动之间有演出间隔
        }
    }
}
```

格斗游戏的连招系统：

```csharp
// 连招检测：把玩家输入的按键序列和技能 Command 做映射
public class ComboSystem
{
    private readonly List<KeyCode> inputBuffer = new();
    private const float INPUT_WINDOW = 0.3f; // 0.3 秒内的输入视为连招
    private float lastInputTime;

    // 连招指令表（技能触发条件 → 技能 Command 工厂）
    private readonly List<(KeyCode[] sequence, Func<ICommand> commandFactory)> combos = new()
    {
        (new[] { KeyCode.J, KeyCode.J, KeyCode.K }, () => new CastSpellCommand(player, heavyStrikeSkill, target)),
        (new[] { KeyCode.J, KeyCode.K, KeyCode.J }, () => new CastSpellCommand(player, launchSkill, target)),
    };

    void Update()
    {
        if (Time.time - lastInputTime > INPUT_WINDOW)
            inputBuffer.Clear(); // 超时清空缓冲

        foreach (KeyCode key in monitoredKeys)
        {
            if (Input.GetKeyDown(key))
            {
                inputBuffer.Add(key);
                lastInputTime = Time.time;
                CheckCombos();
            }
        }
    }

    void CheckCombos()
    {
        foreach (var (sequence, factory) in combos)
        {
            if (MatchesSuffix(inputBuffer, sequence))
            {
                var command = factory();
                command.Execute(); // 触发技能
                inputBuffer.Clear();
                return;
            }
        }
    }
}
```

---

## 应用三：录像回放系统

Command 模式是实现录像回放的最优雅方案：**记录每帧的所有 Command，回放时按同样顺序重新执行**。

```csharp
[System.Serializable]
public class InputFrame
{
    public int frameIndex;
    public List<SerializableCommand> commands = new();
}

// 录制模式
public class GameRecorder
{
    private readonly List<InputFrame> recording = new();
    private int currentFrame = 0;

    public void OnFrameBegin()
    {
        recording.Add(new InputFrame { frameIndex = currentFrame });
    }

    public void RecordCommand(ISerializableCommand command)
    {
        recording[^1].commands.Add(command.Serialize());
    }

    public void OnFrameEnd() => currentFrame++;

    public List<InputFrame> GetRecording() => recording;
}

// 回放模式
public class GameReplayer
{
    private readonly List<InputFrame> frames;
    private int replayFrame = 0;

    public GameReplayer(List<InputFrame> frames)
    {
        this.frames = frames;
    }

    public void ReplayFrame()
    {
        if (replayFrame >= frames.Count) return;

        var frame = frames[replayFrame++];
        foreach (var serializedCmd in frame.commands)
        {
            ICommand command = CommandFactory.Deserialize(serializedCmd);
            command.Execute();
        }
    }
}
```

这种架构也是**帧同步网络对战**的基础：客户端互相发送"我这帧做了什么"（Command 序列），而不是发送整个游戏状态，大幅降低网络带宽需求。

---

## Command 模式的变体：宏命令（Macro Command）

把多个 Command 组合成一个：

```csharp
// 宏命令：一次执行多个操作（可以整体撤销）
public class MacroCommand : ICommand
{
    private readonly List<ICommand> commands;

    public MacroCommand(IEnumerable<ICommand> commands)
    {
        this.commands = new List<ICommand>(commands);
    }

    public void Execute()
    {
        foreach (var cmd in commands)
            cmd.Execute();
    }

    public void Undo()
    {
        // 逆序撤销（后执行的先撤销）
        for (int i = commands.Count - 1; i >= 0; i--)
            commands[i].Undo();
    }
}

// 应用：一键布置一组物体（可以整体撤销）
var groupPlace = new MacroCommand(new ICommand[]
{
    new PlaceObjectCommand(wallPrefab, pos1, rot),
    new PlaceObjectCommand(wallPrefab, pos2, rot),
    new PlaceObjectCommand(doorPrefab, pos3, rot),
});
history.Execute(groupPlace);
```

---

## 什么时候用 Command 模式

**适合用**：
- 需要撤销/重做（关卡编辑器、策略游戏）
- 操作需要延迟执行（技能队列、回合制行动顺序）
- 需要操作历史记录（录像、网络帧同步）
- 操作需要序列化传输（网络同步、存档回放）

**不适合用**：
- 简单的一次性操作，不需要历史记录
- 操作本身很难"逆转"（比如随机过程、涉及物理模拟的操作）
- 性能极其敏感的代码路径（每个 Command 是一个对象分配）

---

## 小结

Command 模式的三个关键价值：

1. **操作对象化**：把"做一件事"变成一个可以存储、传递、撤销的对象
2. **解耦调用方和执行方**：调用方只知道"有一个 Command"，不知道 Command 内部做了什么
3. **时间维度上的弹性**：操作可以被记录、延迟、重排、重放——这在实时循环模型的游戏里特别有价值

Command 模式和 Observer 模式（下一篇）是游戏里使用频率最高的两个模式，很多游戏系统的骨架都是这两个模式的组合。
