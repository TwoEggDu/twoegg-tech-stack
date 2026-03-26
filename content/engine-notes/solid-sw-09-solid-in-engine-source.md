---
date: "2026-03-26"
title: "软件工程基础 09｜SOLID 在引擎源码中的体现：从 Unity 和 Unreal 的架构理解这五个原则"
description: "从真实引擎的设计决策里，看 SOLID 原则是如何在工业级代码中被应用的。理解引擎为什么这样设计，是读懂引擎源码的前提。"
slug: "solid-sw-09-solid-in-engine-source"
weight: 717
tags:
  - 软件工程
  - SOLID
  - Unity
  - Unreal
  - 引擎架构
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> 引擎源码是 SOLID 原则的最佳教材。那些经历了十年以上迭代、被数万个项目使用的架构决策，背后几乎都能找到 SOLID 的影子。
>
> 这篇不涉及引擎的任何内部 C++ 文件名、类名或方法名，只从公开的 API 和设计文档出发，解释这些设计决策为什么是对的。

---

## Unity 的 Component 系统：ISP 和 SRP 的完美教材

Unity 早期版本曾经尝试过另一种架构：一个庞大的 `Entity` 基类，把渲染、物理、音效、动画等功能全部继承下来。

这个方向很快被放弃了，因为它违反了 SRP 和 ISP：

- 每个实体都带着它不需要的功能（一颗触发器碰撞体不需要渲染器）
- 任何一个系统的改变都会影响所有实体
- 不同需求之间的耦合无法解开

最终选择的方向是**Component 系统**：

```
GameObject（容器，只负责持有和组织）
    ├── Transform（只管位置/旋转/缩放）
    ├── MeshRenderer（只管如何渲染）
    ├── Rigidbody（只管物理模拟）
    ├── AudioSource（只管音效）
    └── 自定义 MonoBehaviour（只管游戏逻辑）
```

**SRP 的体现**：每个 Component 只有一个职责，只有一个变化的理由。改物理模拟不影响渲染，改渲染不影响音效。

**ISP 的体现**：GameObject 不强迫每个实体都带所有 Component。一个纯触发区域只挂 `Collider`，不需要 `Renderer` 和 `Rigidbody`。

这个架构使得 Unity 能够在不改变已有 Component API 的情况下，持续向引擎添加新的 Component 类型——这也是 **OCP** 的体现。

---

## Unity 的 ScriptableObject：DIP 的实践工具

`ScriptableObject` 看起来只是"存数据的资产文件"，但它的设计意图远不止于此——它是 DIP 在 Unity 里最直接的实现工具。

**没有 ScriptableObject 时的问题**：

```csharp
// 游戏逻辑直接依赖具体数值（低层实现细节）
public class PlayerStats : MonoBehaviour
{
    private int maxHP = 100;         // 硬编码
    private float moveSpeed = 5f;   // 硬编码
    private int attackPower = 20;   // 硬编码
}
```

修改数值需要修改代码，重新编译，重新测试。数值是"低层细节"，游戏逻辑是"高层策略"，高层直接依赖低层。

**用 ScriptableObject 之后**：

```csharp
// 抽象：定义数据规格（不依赖具体数值）
[CreateAssetMenu]
public class CharacterConfig : ScriptableObject
{
    public int maxHP;
    public float moveSpeed;
    public int attackPower;
}

// 高层游戏逻辑依赖抽象（配置资产）
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterConfig config; // 依赖抽象，不依赖具体数值

    public int MaxHP => config.maxHP;
}
```

游戏逻辑（`PlayerStats`）依赖配置接口（`CharacterConfig`），具体数值（100、5.0f、20）是"低层细节"，存在 `.asset` 文件里。改数值不需要改代码，不需要重编译。

更进一步，ScriptableObject 可以实现 **Runtime Set**（在运行时持有一组对象的引用）和 **Event Channel**（通过 ScriptableObject 传递事件，解除系统间的直接依赖）。这是 DIP + 事件驱动架构的组合，在 Unity 官方 Open Projects 里大量使用。

---

## Unity 的 Physics 系统：封装低层细节的 DIP 实践

Unity 的物理系统对 Nvidia PhysX 做了一层抽象包装：

```
用户代码（高层）
    ↓ 依赖
Unity Physics API：Rigidbody、Collider、Physics.Raycast（抽象层）
    ↓ 实现
PhysX / Unity Physics（低层，可替换）
```

用户的游戏代码只调用 `Physics.Raycast`，不需要知道底层是 PhysX 还是 Unity Physics（DOTS 版本）。Unity 曾在不改变公开 API 的前提下，把底层物理引擎从 PhysX 切换到了 Unity Physics，使用该 API 的游戏代码几乎不需要任何修改。

这正是 DIP 的最大价值：**高层代码不受底层技术切换的影响**。

---

## Unity 的 Input System（新版）：OCP 的教科书

旧版 Input System（`Input.GetKeyDown`）是典型的违反 OCP 的设计：

```csharp
// 旧版：所有输入判断硬编码在游戏逻辑里
void Update()
{
    if (Input.GetKeyDown(KeyCode.Space)) Jump();
    if (Input.GetKeyDown(KeyCode.LeftControl)) Crouch();
}
```

这意味着：换输入设备（手柄→键盘）需要修改游戏逻辑代码。

新版 Input System 的设计：

```csharp
// 新版：游戏逻辑只依赖"动作"抽象，不依赖具体按键
public class PlayerController : MonoBehaviour
{
    private PlayerInputActions inputActions;

    void OnEnable()
    {
        // 只订阅"跳跃动作"，不关心是哪个按键触发的
        inputActions.Player.Jump.performed += ctx => Jump();
        inputActions.Player.Crouch.performed += ctx => Crouch();
    }
}
```

游戏逻辑对扩展开放（新增输入方式），对修改封闭（游戏逻辑代码不需要改）。从键盘换到手柄、再到触屏，只需要在 Input Action Asset 里重新映射，代码不动。

---

## Unreal 的 UObject 与反射系统：SRP 分层的极致

Unreal 的核心类层次很能体现 SRP：

```
UObject（基类：只提供反射、序列化、垃圾回收）
    └── UActorComponent（只提供 Component 生命周期）
        └── USceneComponent（只增加：有 Transform、能附着）
            └── UPrimitiveComponent（只增加：有碰撞和渲染代理）
                └── UStaticMeshComponent（只增加：使用静态网格体渲染）
```

每一层继承都只增加**一个**新的职责，不破坏上层的约定（LSP）。你可以把任何 `USceneComponent` 的子类用在需要 `USceneComponent` 的地方，行为完全符合约定。

这与游戏里常见的"上帝类继承"完全不同：

```csharp
// 错误的继承：每一层都加很多职责
public class Character : UObject { } // 加了移动、战斗、动画、AI、对话...
public class PlayerCharacter : Character { } // 再加更多
public class MainPlayerCharacter : PlayerCharacter { } // 继续加
// 最终：所有职责都堆在继承链上，每层都是 SRP 的违反
```

Unreal 的做法是把这些职责分散到不同的 Component 上，而不是叠在继承链上。

---

## Unreal 的 GAS（Gameplay Ability System）：SOLID 设计的完整范本

GAS 是 Unreal 里最复杂的系统之一，也是 SOLID 原则应用最完整的系统之一。

**SRP**：每类概念有独立的类处理。
- `UGameplayAbility`：只管"一个技能是什么"
- `UAbilitySystemComponent`：只管"技能系统的容器和调度"
- `UGameplayEffect`：只管"效果是什么"（伤害、Buff 等）
- `UGameplayAttribute`：只管"属性数据"

**OCP**：新增技能不需要修改 GAS 框架代码，只需要继承 `UGameplayAbility` 创建新子类。

**DIP**：技能系统不直接依赖游戏里的具体角色类型，通过 `UAbilitySystemInterface` 接口来查询目标是否有技能系统组件。

```cpp
// GAS 通过接口查询，不依赖具体类
IAbilitySystemInterface* ASI = Cast<IAbilitySystemInterface>(Actor);
if (ASI)
{
    UAbilitySystemComponent* ASC = ASI->GetAbilitySystemComponent();
    // 通过接口操作，不关心 Actor 是玩家还是敌人
}
```

这使得任何 `Actor`（玩家、敌人、Boss、甚至环境物体）都可以接入 GAS，只需要实现一个接口，不需要从特定基类继承。

---

## Unity URP 的 RenderPass 系统：OCP 的工程实践

URP 的渲染管线设计是 OCP 的工程级实践：

```
UniversalRenderPipeline（稳定的框架，不会因为添加新效果而修改）
    ↓ 执行
渲染器（ScriptableRenderer）
    ↓ 依次执行
渲染通道列表（List<ScriptableRenderPass>）
    ├── ShadowCasterPass
    ├── DepthPrepass
    ├── DrawObjectsPass（Opaque）
    ├── SkyboxPass
    ├── DrawObjectsPass（Transparent）
    ├── PostProcessPass
    └── 自定义 Pass（用户扩展）← 加新效果只需要加新 Pass
```

用户添加自定义渲染效果（轮廓描边、屏幕空间雨滴、自定义后处理），只需要继承 `ScriptableRenderPass` 并实现 `Execute` 方法，注册进渲染器即可。

URP 框架代码（`UniversalRenderPipeline`）从不修改——新效果通过新的 Pass 来实现，OCP 得到了严格保证。

这使得 URP 能够支持无数种不同风格的渲染效果，而核心渲染管线代码保持稳定，不随每个项目的需求变化而改变。

---

## 从读引擎源码中学习 SOLID

读引擎源码时，可以用这几个问题来主动识别 SOLID：

**SRP**：这个类有多少个职责？它为什么被这样划分？如果某个功能单独在一个类里，是因为它的变化原因和其他功能不同吗？

**OCP**：这里有多少个虚函数/抽象方法？这些是扩展点——设计者预判了哪些地方会需要变化？

**LSP**：子类是否只在父类约定的范围内扩展行为，而不改变已有约定？父类的接口是否稳定？

**ISP**：接口/抽象类有多小？是否有多个只用了部分接口方法的实现类？（这提示接口可能还可以再拆）

**DIP**：这个模块依赖的是接口还是具体类？在哪里完成依赖注入（组装具体实现）？

---

## 小结

- **Unity Component 系统**：SRP + ISP，每个 Component 专注一个职责，按需组合
- **ScriptableObject**：DIP，把数据（低层细节）抽象成资产，让游戏逻辑（高层策略）依赖资产规格而非具体数值
- **新版 Input System**：OCP，输入映射变化不影响游戏逻辑
- **Unreal GAS**：SOLID 全面应用，职责分离、接口驱动、对扩展开放
- **URP RenderPass**：OCP，新增渲染效果不修改框架

读引擎源码最大的价值不是知道它用了什么 API，而是理解它**为什么这样设计**。SOLID 给了你理解这个"为什么"的语言。
