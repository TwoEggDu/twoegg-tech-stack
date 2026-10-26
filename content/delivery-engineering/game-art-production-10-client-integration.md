---
title: "10｜客户端：怎样把美术资产接成运行中的表现"
slug: "game-art-production-10-client-integration"
date: "2026-10-25"
lastmod: "2026-10-26"
description: "从模型、材质、动作和特效的引用组装，到 Unity 配置、实例创建、施法编号、释放与反馈，再到取消、退出清理和版本复查。"
draft: false
featured: false
content_stage: "article"
weight: 90
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 10
type: "reading-series"
reading_title: "客户端：怎样把美术资产接成运行中的表现"
reading_summary: "把前文资产连接成 Unity 表现模板与运行实例，用配置、动作编号和明确的拥有者管理播放、目标反馈、取消、尾部与退出。"
---

即使模型已导入、动作可以在预览窗口中查看、特效也有自己的 Prefab，仍有一组问题：谁创建角色，谁让它施法，收到受击时清掉什么，角色离场后那些已经飞到世界中的粒子由谁结束？

如果这些连接只靠名字相似和临时脚本，单件资源各自可用，组合起来仍可能错位、重复播放或残留。客户端接入需要把引用、运行状态和生命周期明确组织起来。

**美术资产成为运行表现，需要稳定的组装关系、明确的驱动入口，以及有人负责的创建、取消和退出。** 本篇沿用庭院、测试手臂、法杖与三段特效，把这条连接具体展开。

> **读这篇之前**：模型、绑定与动作分别见[03 角色资产]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})、[04 绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}})、[05 动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})；庭院、效果和技术模板见[07 地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})、[08 特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})、[09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})。本篇补接入关系，不重写各工具制作教程。

> **版本与证据范围**：操作方案参考 Unity 6（6000.0），沿用前文 URP、Input System 与粒子设置。目录、参数和 C# 为教学方案，尚无实际模型/动画/效果工程，也未执行 Unity 编译、运行、资源加载框架接入或设备实验。局部手臂与占位体不代表完整角色；本篇不宣称已有完整可玩样例，综合制作与验收仍由 11—12 收束。

## 先把资产、模板、实例和请求分开

同一段施法，制作端与客户端看到的对象并不相同：

<div role="region" aria-label="制作产物与运行对象的区别，可横向查看" tabindex="0" style="overflow-x:auto">

| 层次 | 本例对象 | 负责什么 |
| --- | --- | --- |
| 制作源文件 | Maya / Max 原稿、PSD | 保留可编辑内容与修改依据 |
| 引擎导入对象 | FBX 的网格、Avatar、AnimationClip，PNG 纹理 | 按导入设置解释交换文件 |
| 组装模板 | 角色 Prefab、材质、Controller、效果 Prefab | 保存稳定结构与资源引用 |
| 表现配置 | CourtyardArtProfile | 指定一套模板、效果和时间参数 |
| 运行实例 | 某个角色及其效果实例 | 持有位置、当前动作、目标与清理责任 |
| 表现请求 | 本次施法、受击、目标反馈、离场 | 表达运行中发生的变化 |

</div>

一个 Prefab 可以创建多个角色。它们共享模板，不应共享“当前正在施法”这样的实例状态。配置资产也不应被当成全局变量，在运行时反复写入每个角色的当前目标。

模型文件存在，不等于导入对象正确；模板正确，也不等于所有运行分支都已检查。Importer 的更完整职责可读[Unity Importer]({{< relref "engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md" >}})。

### 逻辑结果与表现请求分别由谁提供

正式游戏中，输入先经过玩法规则判断，允许施法后才创建动作上下文；表现层消费这个结果，播放动作和效果。命中、伤害与消耗不应由“一个粒子恰好播到这里”自动决定。

本篇用键盘模拟这些请求，让接入过程容易观察：

- Space：请求开始一次施法表现。
- Escape：模拟允许打断的受击，取消当前动作并播放 Hit。
- I：模拟携带当前动作编号的目标反馈；不检测碰撞，也不扣血。
- O：重放上一动作编号的反馈，检查过期请求是否被拒绝。
- Backspace：让角色离场；R：重新创建角色。

这些按键是调试入口，不是战斗规则或网络协议。

### 每层拥有者都要明确退出责任

场景入口拥有角色实例，角色表现组件拥有自己创建的效果。一个效果即使放在世界根下，仍登记在创建者的记录里，不能因为没有父子关系就丢掉清理责任。

动作正常结束可以让粒子尾部自然消失；动作取消只处理该次动作所属效果；角色离场则处理它拥有的全部效果。资源句柄与对象实例又是另一层所有权，后面再说明。

{{< art-client-integration >}}

## 收到文件后，先核对导入和交接接口

这里不是把所有原稿拖进 Unity 就开始写播放代码。先列出本次实际收到的文件、版本、用途和预期结果。

### 用前文局部产物组成接入清单

<div role="region" aria-label="本次集成的资产清单，可横向查看" tabindex="0" style="overflow-x:auto">

| 输入 | 引擎侧复核 | 接到哪里 |
| --- | --- | --- |
| staff.fbx / staff_basecolor.png | 尺度、前向、网格、颜色纹理与材质 | 法杖子对象与效果尖端挂点 |
| arm_rig.fbx | Generic、基础 Avatar、蒙皮和骨骼层级 | 测试视觉对象 |
| arm_actions.fbx | 对应 Avatar，Idle / Cast / Hit 三个有效片段 | 本篇独立的 Animator Controller |
| 庭院模块与 Scene | 材质、碰撞、目标与观察位置 | 场景测试环境 |
| VFX_Charge / Release / Impact | 根 Particle System、空间、引用、关闭自动播放 | 本篇表现配置 |
| 第 09 篇可选模板 | Shader / 材质支持范围和公开属性 | 通过对照后再替换目标反馈 |

</div>

Maya 与 Max 使用各自匹配的基础模型和动作来源，不要只凭文件同名混用。第 05 篇的动作模型采用 Generic → Copy From Other Avatar；这依赖一致的输出骨架，不会自动修正骨架变更，参考 [Unity Generic animations](https://docs.unity3d.com/6000.0/Documentation/Manual/GenericAnimations.html)。

纹理先核对颜色/数据语义、Alpha、采样和平台设置，再确定材质；动作先单独预览，再检查 Controller；效果先检查引用与单次播放，再进入组合。保留 `.meta` 与明确引用，避免重新建文件导致身份变化。

本系列目前尚未附带这些实际资产。读者可以先用胶囊、空挂点与前文基础粒子方案搭建接入框架，Animator 暂时留空；有对应测试手臂后再补动画路径，不能把占位体检查当作蒙皮和动作已通过。

### 引用要保存在可检查的位置

材质、Controller、挂点和效果应通过明确字段绑定，不在每次施法时递归搜索一个碰巧叫 Staff 的节点。节点名字仍便于交接，但不能代替稳定引用和缺失检查。

通过 Inspector 绑定的 Prefab 引用，可以保存对应内部层级关系；创建实例后应确认字段指向该实例内的对象。Prefab 的复用与实例关系见 [Unity Prefabs](https://docs.unity3d.com/6000.0/Documentation/Manual/Prefabs.html)。

## 在 Unity 组装角色模板，分开移动根与视觉对象

从庭院场景另存为 `CourtyardClientIntegration.unity`，保留前文的单资源测试场景。建议的角色结构为：

```text
ResearcherPresentation             // 单位缩放；角色实例根
  CharacterController
  CourtyardWalkProbe               // 第 07 篇移动探针，按本篇补移动许可
  CourtyardActorView               // 本篇表现拥有者
  VisualRoot
    CapsulePlaceholder            // 占位时使用；移除重复 Collider
    ArmVisual                     // 有测试手臂时替换/补入
      Animator
      root / skeleton / ...
      weapon_socket_r
        Staff
          vfx_socket_staff_tip
  CameraTarget
```

这里是结构意图，不要求在没有骨骼工程时凭空创建假的变形层级。占位路径可直接在 VisualRoot 下创建 staff_tip 空物体，局部位置参考第 08 篇；完整手臂路径则使用实际骨骼、装备与法杖尖端位置。

CharacterController 仍负责本例身体通行；视觉对象在子层级，不额外叠加一个 Rigidbody 来同时移动同一根节点。Apply Root Motion 先关闭，角色位置由第 07 篇的移动探针改变。

### 移动许可与移动动画是两件事

本例在施法和受击阶段禁止移动，Idle 时允许 WASD。将第 07 篇 CourtyardWalkProbe 的方向计算之后、Move 之前补入：

```csharp
var view = GetComponent<CourtyardActorView>();
if (view == null || !view.CanMove)
    direction = Vector3.zero;
```

这只阻止水平输入，仍保留原脚本的重力处理。真正工程通常会缓存组件或通过初始化注入，避免每帧查询。

本系列只有局部手臂的 Idle / Cast / Hit，没有完整角色走跑动画，因此 **本篇不伪装已经做成了移动 Blend Tree 和脚步同步**。占位体可以移动，测试手臂也可以保持 Idle；有全身素材后再把真实速度、落地与转向等状态映射到动作系统。

### 本篇采用一个单独的动作控制方案

新建 `ClientPresentation.controller`，使用第 05 篇同一套动作数据的三个 Clip，建立 Base Layer.Idle / Cast / Hit，Idle 为默认状态。第一轮不建立自动过渡，不使用旧 Trigger，也不接第 08 篇的 OnPreviewRelease 事件。

如果前一轮已在共享 FBX 的导入片段中添加 Events，仅换 Controller 不会移除那些事件。先在 Project 窗口复制动作 FBX 为独立的 `arm_actions_client.fbx`，在副本中复核 Generic、Source Avatar 和三个片段范围，再移除本路径不用的旧事件；新 Controller 只引用这些副本片段。保留第 08 篇原有动作资产，避免为了本篇试验而破坏另一条预览路径。导入片段的事件位置见 [Animation Events on imported clips](https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationEventsOnImportedClips.html)。

本篇使用一个表现时钟推进释放与动作结束，并用 Animator.Play 明确选择状态。这样先检查编号、所有权与清理，不让多个时间来源同时驱动释放。Idle 循环，Cast / Hit 不循环；状态 Speed 与 Animator.speed 先为 1，Animator 使用 Normal 更新与 Always Animate 剔除模式，按实际版本核对设置。

Animator.Play 的状态名包含层名，第三个参数可以指定归一化起点，见 [Unity Animator.Play](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator.Play.html)。本路径不展示过渡混合；需要第 05 篇的平滑过渡时，应重新对齐进入时刻、偏移和释放标记，而不是照搬固定秒数。

独立时钟也不是逐帧同步保证。Animator 求值顺序、帧间隔、暂停和速度变化都会影响观察；后续可采用统一动作时间轴或带动作身份的事件映射。不能将当前方案中的 0.5 秒称为任意 Controller 的准确释放时点。

保存为 `ResearcherPresentation.prefab`，在 CourtyardActorView 中绑定该实例的 Animator（可空）与 staff_tip；停用旧 CourtyardVfxPreview，避免两个脚本同时响应 Space 或创建效果。

## 用配置选择资产，用实例保存运行状态

在 `Assets/Scripts/ArtProduction/CourtyardArtProfile.cs` 定义一份只读使用的配置：

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Art Production/Courtyard Art Profile")]
public sealed class CourtyardArtProfile : ScriptableObject
{
    public string revision = "teaching-v001";
    public GameObject actorPrefab;
    public ParticleSystem charge, release, impact;
    public float releaseSeconds = 0.5f;
    public float finishSeconds = 1.1f;
    public float hitSeconds = 0.5f;
}
```

通过 Create 菜单创建 `ResearcherArt_v001.asset`，引用角色 Prefab 和三段粒子的根组件。第 09 篇 TA 版本属于可选替换，未实际验证前先使用第 08 篇方案作参照。

ScriptableObject 用于保存共享配置，不自动成为资源加载框架或每个角色独有的状态，见 [Unity ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScriptableObject.html)。

releaseSeconds / finishSeconds 是本篇演示时钟的参数。它们基于一秒 Cast 的教学安排，hitSeconds 对应半秒 Hit；粒子尾部仍按自身 Lifetime 检查结束，不强行等同于 finishSeconds。

## 为每次动作分配编号，再管理释放和反馈

下面的状态只属于这个角色表现实例：

```text
Idle → Cast → Recover → Idle
          ↘ 取消/受击 → Hit → Idle

动作正常结束：编号失效，已存在的粒子尾部继续自然结束
动作取消：编号失效，清理该动作的粒子
角色离场：停止接收，清理该角色全部粒子
```

### 编号必须跟随请求，而不是回调时再读取当前值

开始动作时返回编号，例如 41。后续目标反馈应该继续携带 41。若它被取消、结束，或者角色已开始 42，再收到 41 应拒绝。

不要在旧异步回调里读取 CurrentActionId，重新包装成“当前动作反馈”。那会丢失请求原本属于谁的信息。实际项目还需关联角色身份、生成代次和相应业务事件；本例编号只在当前组件实例内有效。

### 最小表现拥有者

创建 `Assets/Scripts/ArtProduction/CourtyardActorView.cs`。下面代码管理本例三段粒子、动作时钟与当前目标，**尚未在 Unity 编译或运行**：

```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class CourtyardActorView : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Transform socket;
    enum Phase { Idle, Cast, Recover, Hit }
    [SerializeField] Phase phase;

    struct Fx
    {
        public long owner;
        public ParticleSystem system;
        public bool charge;
    }
    readonly List<Fx> effects = new();
    CourtyardArtProfile profile;
    Transform target;
    long sequence, activeId;
    float elapsed;
    bool ready, impactShown;

    public bool CanMove => ready && phase == Phase.Idle;
    public long CurrentActionId => activeId;
    bool TargetValid => target != null && target.gameObject.activeInHierarchy;

    public bool Initialize(CourtyardArtProfile data)
    {
        if (data == null || socket == null ||
            data.charge == null || data.release == null || data.impact == null)
            return false;
        if (!ValidTime(data.releaseSeconds) ||
            !ValidTime(data.finishSeconds) || !ValidTime(data.hitSeconds) ||
            data.finishSeconds <= data.releaseSeconds)
            return false;
        Shutdown();
        profile = data;
        ready = true;
        phase = Phase.Idle;
        Play("Idle");
        return true;
    }

    static bool ValidTime(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

    public long TryCast(Transform goal)
    {
        if (!ready || !isActiveAndEnabled || phase != Phase.Idle ||
            goal == null || !goal.gameObject.activeInHierarchy || socket == null)
            return 0;
        target = goal;
        activeId = ++sequence;
        elapsed = 0f;
        impactShown = false;
        phase = Phase.Cast;
        Spawn(profile.charge, socket.position, socket.rotation, activeId, true);
        Play("Cast");
        Trace(activeId, "begin");
        return activeId;
    }

    void Update()
    {
        if (!ready) return;
        if (phase == Phase.Hit)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= profile.hitSeconds)
            {
                phase = Phase.Idle;
                Play("Idle");
            }
            return;
        }
        if (activeId == 0) return;
        if (!TargetValid || socket == null)
        {
            Cancel(activeId, false);
            return;
        }
        elapsed += Time.deltaTime;
        if (phase == Phase.Cast && elapsed >= profile.releaseSeconds)
        {
            phase = Phase.Recover;
            ClearEffects(activeId, true);
            Spawn(profile.release, socket.position, socket.rotation, activeId);
            Trace(activeId, "release");
        }
        if (elapsed >= profile.finishSeconds)
        {
            long completed = activeId;
            activeId = 0;
            target = null;
            phase = Phase.Idle;
            Play("Idle");
            Trace(completed, "complete; particle tails remain owned");
        }
    }

    public bool ShowImpact(long requestId, Vector3 worldPoint)
    {
        if (!ready || requestId == 0 || requestId != activeId ||
            phase != Phase.Recover || impactShown || !TargetValid)
        {
            Trace(requestId, "impact rejected");
            return false;
        }
        impactShown = true;
        Spawn(profile.impact, worldPoint, Quaternion.identity, requestId);
        Trace(requestId, "impact");
        return true;
    }

    public void Cancel(long requestId, bool hit)
    {
        if (!ready || requestId == 0 || requestId != activeId) return;
        activeId = 0;
        target = null;
        ClearEffects(requestId);
        elapsed = 0f;
        phase = hit ? Phase.Hit : Phase.Idle;
        Play(hit ? "Hit" : "Idle");
        Trace(requestId, "cancel");
    }

    void Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation,
               long owner, bool isCharge = false)
    {
        var fx = Instantiate(prefab, position, rotation);
        if (isCharge)
        {
            fx.transform.SetParent(socket, false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
        }
        fx.transform.localScale = Vector3.one;
        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fx.Play(true);
        effects.Add(new Fx { owner = owner, system = fx, charge = isCharge });
    }

    void ClearEffects(long wanted = 0, bool onlyCharge = false)
    {
        for (int i = effects.Count - 1; i >= 0; --i)
        {
            var entry = effects[i];
            if (wanted != 0 && entry.owner != wanted) continue;
            if (onlyCharge && !entry.charge) continue;
            if (entry.system != null)
            {
                entry.system.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(entry.system.gameObject);
            }
            effects.RemoveAt(i);
        }
    }

    void LateUpdate()
    {
        for (int i = effects.Count - 1; i >= 0; --i)
            if (effects[i].system == null || !effects[i].system.IsAlive(true))
            {
                if (effects[i].system != null)
                    Destroy(effects[i].system.gameObject);
                effects.RemoveAt(i);
            }
    }

    void Play(string name)
    {
        if (animator == null) return;
        int hash = Animator.StringToHash("Base Layer." + name);
        if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
        else Debug.LogWarning("Missing presentation state: " + name, this);
    }

    void Trace(long id, string message) =>
        Debug.Log($"view={GetInstanceID()} action={id} active={activeId} " +
                  $"phase={phase} revision={profile?.revision}: {message}", this);

    public void Shutdown()
    {
        ready = false;
        activeId = 0;
        target = null;
        phase = Phase.Idle;
        ClearEffects();
    }

    void OnDisable() => Shutdown();
    void OnDestroy() => Shutdown();
}
```

这里 HasState 用于发现状态缺失，不代表 Avatar、Clip 和骨骼正确，见 [Unity Animator.HasState](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator.HasState.html)。Animator 留空时，只有占位体和粒子路径，不能记录动画检查通过。

### 释放和目标反馈不会自动互相推导

时钟到 releaseSeconds 后，代码只创建杖尖的 Release。ShowImpact 需要另一个明确请求，并检查编号、阶段、重复状态与目标。

本例只在 Recover 阶段接受一次反馈，动作结束后不接受迟到反馈。这是一种演示协议；长飞行投射物若可能在动作结束后命中，应有自己的存活上下文，不能继续套用当前短动作窗口。

目标在本例中失活或销毁，会取消当前动作；目标反馈使用请求携带的世界坐标。正式请求还应明确目标身份、作用位置与朝向，不能仅凭一个场景 Transform 推断服务器命中结果。

### 动作结束与最后一粒消失分别处理

效果记录同时保存 owner 编号和粒子根。正常完成使 activeId 失效，但未直接销毁所有尾部；LateUpdate 用 IsAlive(true) 等待根与子系统自然结束。

下一次动作可以在前一次尾部仍存在时开始。取消 42 只清理 42 的效果，不误删已经正常完成的 41 的尾部；Shutdown 则清理这个角色拥有的全部效果，包括独立世界实例。

粒子存活检测的范围见 [ParticleSystem.IsAlive](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.IsAlive.html)。它不代表灯光、音效、Tween、拖尾或外部异步任务也已结束；添加这些内容时需要扩展组合对象的清理协议。

## 场景入口负责创建角色、模拟请求与离场

创建 `Assets/Scripts/ArtProduction/CourtyardClientDemo.cs`，放在场景独立空物体上，绑定 Profile、spawnPoint、target 与 impactAnchor。target 指向实际目标物体，impactAnchor 指向其前方的反馈位置，不能把独立锚点的存在等同于目标仍有效。输入沿用第 07 篇的配置，键盘接口参考 [Input System Keyboard](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Keyboard.html)。

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CourtyardClientDemo : MonoBehaviour
{
    public CourtyardArtProfile profile;
    public Transform spawnPoint, target, impactAnchor;
    CourtyardActorView view;
    long lastId, previousId;

    void Start() => SpawnActor();

    void Update()
    {
        var key = Keyboard.current;
        if (key == null) return;
        if (key.rKey.wasPressedThisFrame) SpawnActor();
        if (key.backspaceKey.wasPressedThisFrame) DespawnActor();
        if (view == null) return;
        if (key.spaceKey.wasPressedThisFrame)
        {
            long accepted = view.TryCast(target);
            if (accepted != 0)
            {
                previousId = lastId;
                lastId = accepted;
            }
        }
        if (key.escapeKey.wasPressedThisFrame) view.Cancel(lastId, true);
        if (impactAnchor != null && key.iKey.wasPressedThisFrame)
            view.ShowImpact(lastId, impactAnchor.position);
        if (impactAnchor != null && key.oKey.wasPressedThisFrame)
            view.ShowImpact(previousId, impactAnchor.position);
    }

    void SpawnActor()
    {
        DespawnActor();
        if (profile == null || profile.actorPrefab == null || spawnPoint == null)
            return;
        var instance = Instantiate(profile.actorPrefab,
            spawnPoint.position, spawnPoint.rotation);
        var candidate = instance.GetComponent<CourtyardActorView>();
        if (candidate == null || !candidate.Initialize(profile))
        {
            Debug.LogWarning("Invalid actor references or profile.", this);
            instance.SetActive(false);
            Destroy(instance);
            return;
        }
        view = candidate;
        lastId = previousId = 0;
    }

    void DespawnActor()
    {
        if (view == null) return;
        view.gameObject.SetActive(false);
        Destroy(view.gameObject);
        view = null;
    }

    void OnDisable() => DespawnActor();
}
```

这三个脚本是一条直接引用的教学路径，尚未实测。代码没有实现加载句柄、对象池、网络、伤害、完整移动动画或复杂技能优先级，也没有覆盖配置在运行中被外部修改等情况；第一轮不在 Play 中修改共享 Profile。

### 创建之后再初始化，不依赖 OnEnable 已拿到配置

活动对象 Instantiate 时可能已触发 Awake / OnEnable，再执行后面的 Initialize。不要在 OnEnable 中假定调用方还没注入的数据已经存在；本例以 ready 阻止未初始化动作。创建行为见 [Unity Instantiate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.Instantiate.html)。

检查失败时停用并销毁实例，避免留下半初始化对象。该规则同样适用于后续把素材、材质或控制器动态注入实例的方案。

### 先失效，再清理，再销毁

离场先 SetActive(false)，使表现组件 Shutdown、动作失效并清掉世界效果，再请求 Destroy。实际销毁通常延后发生，因此不能只把“已调用 Destroy”当作所有后续请求已经不可能到达，见 [Unity Destroy](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.Destroy.html)。

OnDisable / OnDestroy 的清理设计为可重复调用；第一次清掉记录后，下一次没有内容可再清。OnDisable 的触发范围见 [Unity OnDisable](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnDisable.html)。

本例对象重新启用前要重新 Initialize；正式池化方案需明确重置动作、编号代次、目标、效果、事件订阅和外部引用。不能把 Destroy 换成 SetActive(false)，就认为复用已经完成。

## 从编辑器输入走到实际可观察行为

将 spawnPoint 放在庭院 D 附近，例如 (4,0.05,4)，为 Capsule / CharacterController 设置第 07 篇的尺寸和接触参数。使用原目标与前方 impactAnchor，保持所有关键父节点缩放为 1。

旧测试角色、旧效果 Driver 和导航探针先停用，避免重叠输入、多个身体或多个效果实例干扰结果。检查新角色被创建的位置和字段后，再打开移动与第三人称观察。

### 镜头要跟随新实例的目标

原场景摄像机若仍指向被停用或旧的角色，实例创建成功也可能没有正确画面。在 Play 中明确把 Cinemachine Tracking Target 指向本次实例的 CameraTarget；这是手动调试路径。

若要自动绑定，应由角色创建/销毁入口维护当前镜头目标，先检查引用再切换，离场时清空或转向观察镜头。本文代码没有自动接管镜头，不能把第 07 篇的静态引用当作对所有动态实例都有效。

### 通过日志和字段共同定位

查看 CourtyardActorView 的 phase、动作日志和场景层级，区分：

- 没有创建：入口配置、Prefab 引用、spawnPoint 或组件缺失。
- 创建但不能施法：初始化、当前阶段、目标、挂点或输入条件。
- 动作错误：Controller、状态名、Avatar、Clip、根运动和时钟条件。
- 效果错位：挂点、父节点、模拟空间、缩放或反馈坐标。
- 效果残留：拥有者记录、取消分支、循环子内容或退出清理。
- 看不到角色：实例位置、相机目标、Layer、剔除或材质。

日志中的 GetInstanceID 只用于本次运行定位，不是跨会话或网络业务身份。revision 是人工标记，也不能替代真实资源/配置版本快照。

### 把边界分支列成检查表

<div role="region" aria-label="客户端表现接入走查，可横向查看" tabindex="0" style="overflow-x:auto">

| 场景 | 操作 | 待记录的结果 |
| --- | --- | --- |
| 初始化 | 分别缺少 Profile、挂点、效果或 View | 是否拒绝使用并清理半成品 |
| 正常施法 | Space，释放后 Recover 阶段按 I | begin/release/impact/complete 与粒子尾部 |
| 重复请求 | Cast / Hit 时再次 Space | 是否拒绝新动作，未重复创建 |
| 重复反馈 | 同一编号连续 I | 是否只接受一次 |
| 旧反馈 | 新动作 Recover 阶段按 O | 旧编号是否被拒绝，当前动作不受影响 |
| 释放前取消 | Space 后、释放前 Escape | 不再释放，当前 Charge 被清理 |
| 释放后取消 | 已 Release / Impact 后 Escape | 当前动作效果按本例策略清理 |
| 目标失效 | 在 Cast / Recover 中关闭或销毁目标 | 当前动作取消，后续反馈拒绝 |
| 正常尾部叠加 | 在 Recover 后段按 I，完成后马上开始下一次，再取消新动作 | 上次尾部自然结束，不被误删 |
| 角色退出 | 施法或尾部存在时 Backspace / R | 动作先失效，世界效果清理；新实例独立 |
| 场景退出 | 切换/卸载测试场景 | 入口、角色、世界效果和引用各自处理 |
| Player | 固定版本、路径、镜头与设备重走 | 与编辑器差异、日志、画面及实际成本 |

</div>

这些是待执行项目，不是已有实验结论。帧间隔很大时，释放与完成可能在同一帧处理；本例不会凭空补出中间多帧的动画过程。动作时长、时间缩放、Animator 设置和设备行为都需要实际记录。

## 直接引用路径站稳后，再接资源加载与卸载

本例没有异步加载：Profile 的直接引用构成资产依赖，运行时对已获得的 Prefab 创建实例。不能把这叫作已完成 AssetBundle、Addressables 或 YooAsset 接入。

### 对象清理不等于资产与句柄释放

销毁角色实例，解决的是实例生命周期；资源系统可能仍持有模型、纹理、材质、动画、配置或句柄引用。共享资产也可能仍被其他实例使用，不能跟着一个角色离场就随意卸载。

场景卸载与资源内存释放也不能简单画等号，见 [SceneManager.UnloadSceneAsync](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html)。完整加载与生命周期可深入[资源加载生命周期]({{< relref "delivery-engineering/delivery-resource-pipeline-05-loading-lifecycle.md" >}})。

### 异步到达时，检查的应是发起时的上下文

如果后续改为异步创建，应有以下顺序约定，而不是直接在完成回调中 Instantiate：

```text
发起加载：保存实体身份、创建代次与请求上下文
离场/替换：先让该上下文失效，再取消或结束等待
加载完成：
  上下文仍有效 → 创建、初始化、显示，并登记句柄归属
  上下文已失效 → 不显示旧角色，按资源框架协议处理已获得的结果
销毁/归还：停止表现和回调 → 处理实例 → 释放所属句柄
```

取消等待不一定等于底层加载停止；迟到结果仍可能需要释放。具体 API、引用计数、取消语义和错误处理由实际资源框架决定，本篇没有实现这段伪流程。

对跨场景常驻对象，应另行确定谁保存实体、镜头和资源句柄，何时转移或结束所有权。不要只加 DontDestroyOnLoad，就把清理问题藏到下一张场景。

## 固定一次集成版本，再判断替换影响

同一目录中的资源也可能来自不同制作轮次。交接记录至少包含：源文件/导出版本、导入设置、材质/Shader、Avatar、Clip/Controller、效果 Prefab、Profile、场景、脚本和包版本，以及已执行与未执行的检查。

<div role="region" aria-label="替换资产后的复查范围，可横向查看" tabindex="0" style="overflow-x:auto">

| 变更 | 首先看哪里 | 需要重新走查什么 |
| --- | --- | --- |
| 改骨架或蒙皮 | Avatar、骨骼路径、SkinnedMeshRenderer 与挂点 | 动作、装备、变形、特效位置 |
| 改动作时长/速度 | 表现时钟、状态控制和释放标记 | 释放、取消、恢复与同步 |
| 换特效或材质 | 根结构、引用、参数与支持范围 | 位置、Alpha、尾部、取消与设备结果 |
| 改模板层级 | 序列化字段和实例内引用 | 初始化、相机、装备与清理 |
| 换加载方案 | 请求代次、句柄与共享引用 | 迟到结果、取消、退出与重建 |
| 修改庭院布局 | 出生点、碰撞、目标和观察条件 | 通行、导航、镜头与画面可读性 |

</div>

本例建议的文件组织为：

```text
Assets/Art/Researcher/Prefabs/ResearcherPresentation.prefab
Assets/Art/Researcher/Animations/ClientPresentation.controller
Assets/Art/Researcher/Profiles/ResearcherArt_v001.asset
Assets/Scripts/ArtProduction/
  CourtyardArtProfile.cs
  CourtyardActorView.cs
  CourtyardClientDemo.cs
Assets/Scenes/ArtProduction/Courtyard/
  CourtyardClientIntegration.unity
  courtyard-client-handoff.md
```

保存所有引用资产与 `.meta`；必要的包配置、ProjectSettings 和图形质量条件一起记录。这里文件名说明制作组织，不代表这些 Unity 文件已经实际产出。

## 本篇交付到哪里

这条接入方案连接了导入复核、角色模板、共享配置、实例创建、动作编号、释放/反馈、取消、尾部与退出，并给出共同观察和替换资源后的复查入口。

它没有补齐完整角色素材，没有实现加载框架、对象池、战斗规则或网络。下一篇的综合示例需要在真实工程里汇合这些产物，并保留制作、修改和运行证据。

<!-- DATA-TODO: 制作 ResearcherPresentation、ClientPresentation.controller、Profile 与 CourtyardClientIntegration，记录实际模型/Avatar/Clip/效果来源、引用字段、包版本与导入配置；占位体和测试手臂分别说明。 -->
<!-- DATA-TODO: 在 Unity 编译三个教学脚本与移动许可补丁，执行正常/重复/旧编号/取消/目标失效/尾部/角色与场景退出分支，补日志和录像；核对 Animator 时钟、镜头绑定及真实取消入口。 -->
<!-- DATA-TODO: 实际接入资源框架时补加载代次、取消、句柄与迟到结果验证；在目标设备固定版本、路径、质量与相机记录画面/性能，未执行前不填通过或优化收益。 -->

**客户端接入要让每个表现请求有明确身份，让每个运行对象有清楚的引用、拥有者和退出路径。**

下一篇进入[第 11 篇综合示例]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}})，把制作与接入放进同一份可追溯的工程过程。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
