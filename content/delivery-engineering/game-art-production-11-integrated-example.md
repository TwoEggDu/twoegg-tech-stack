---
title: "11｜综合示例：角色进入关卡并释放一次技能"
slug: "game-art-production-11-integrated-example"
date: "2026-10-25"
lastmod: "2026-10-26"
description: "把原画、模型、绑定、动作、庭院和特效汇合成 Unity 场景，沿占位替换、角色与镜头接线、一次交互和动作时长修改追踪完整依赖。"
draft: false
featured: false
content_stage: "article"
weight: 91
tags: ["美术生产", "Unity", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 11
type: "reading-series"
reading_title: "综合示例：角色进入关卡并释放一次技能"
reading_summary: "汇合各工种产物，用庭院中的移动、距离与遮挡、施法反馈和重新开始串起交互，再追踪动作时长修改。"
---

一根法杖能正确导入，一段手臂动作能播放，一个圆环特效能单独预览，仍然不足以回答玩家进入庭院之后会发生什么。

玩家可能走不过门洞，镜头还盯着旧测试角色；也可能隔着屏风就能施法，杖尖闪了一下，目标却没有任何反馈。把这些问题分别交回某个文件的制作者，往往得不到完整答案，因为问题发生在文件相互使用的地方。

这一篇沿同一个小目标汇合前文：从庭院外出生，走过门洞并绕过屏风，接近目标，释放一次短距离技能，看到反馈，再重新开始。我们要讲清的，是**每份制作产物怎样进入这个过程，以及更换其中一份内容后，哪些连接需要重新确认。**

> **读这篇之前**：[03 角色资产]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})到[08 特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})解释局部制作；[09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})解释技术模板和工具；[10 客户端接入]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})提供本篇复用的 Profile、角色表现组件和移动许可。

> **版本与证据范围**：下面是面向 Unity 6（6000.0）、URP 17、Input System 和 Cinemachine 3.1 的组装方案与待执行练习。当前没有随文提供实际 PSD、DCC、FBX、Prefab 或完整 Unity 工程，也未执行 C# 编译、Play、Player 构建和设备实验。正文中的坐标、参数、代码、依赖图及修改时间线都是教学设计，不是已有运行结果。角色仍由占位体与可选测试手臂组成，没有补齐完整人物和走跑动作。

## 先定义玩家能完成的事情

如果把目标写成“所有资源都进入 Unity”，任何一个文件导入成功都能被算作进展，却没有办法判断组合是否成立。

本例把目标固定成一段可以反复操作的过程：

1. 角色在庭院入口外出现，玩家可以用 WASD 移动。
2. 门柱和屏风阻挡身体；镜头能够观察通行和目标。
3. 站得太远或被环境遮挡时，施法请求被拒绝，并给出原因。
4. 走到可交互位置后，按 Space 朝向目标，播放 Cast、蓄光和释放闪光。
5. 到达本例释放时点后，再次检查距离和遮挡，向当前动作提交一次目标反馈；接受后显示完成提示。
6. 受击可以取消当前动作；角色离场和重新开始会清理其效果并重新绑定镜头。

这里的“可玩”指上述最小交互能够由玩家输入驱动。它没有敌人 AI、血量、伤害、消耗、联网或正式任务系统。目标反馈是本地教学规则批准的显示请求，不代表做出了真实投射物或战斗结算。

完整美术也分阶段加入。胶囊能够移动，可以检查路线和请求；加入测试手臂之后，才能进一步检查蒙皮与动作；拥有全身素材之后，才能继续检查脚步、躯干发力和完整持杖姿态。记录时要保留这些区别。

## 把产物看成依赖，而不是一个素材文件夹

这一段交互依赖三类联系：

- **制作联系**：导出物从哪个原稿来，修改应该回到哪里。
- **组装联系**：模型、纹理、材质、动作、挂点与配置怎样相互引用。
- **运行联系**：谁创建角色，谁批准表现请求，谁在离场时清理实例。

原画 PSD 在制作联系中很重要，但通常不会成为角色运行时的直接引用。世界坐标中的特效虽然没有挂在角色下面，仍然属于运行联系中某个明确的拥有者。只看 Unity 层级，无法代替完整依赖记录。

{{< art-integrated-example >}}

### 各工种在这一轮交什么

<div role="region" aria-label="综合示例的产物与消费关系，可横向查看" tabindex="0" style="overflow-x:auto">

| 工作环节 | 保留的原稿或说明 | 进入 Unity 的内容 | 本次组合中消费它的地方 |
| --- | --- | --- | --- |
| 原画 | researcher_design_v001.psd、结构与色彩说明 | 评审 PNG 可作参考，通常不作为角色运行纹理 | 建模、材质、场景和特效制作时对照 |
| 角色与道具 | staff_model_v001.ma 或相应 .max、staff_surface_v001.psd | staff.fbx、staff_basecolor.png | Staff 材质与 Prefab、持握位置 |
| 绑定与蒙皮 | arm_rig_maya_v001.ma 或 arm_rig_max_v001.max | 匹配的 arm_rig.fbx、骨架和蒙皮 | ArmVisual、Generic Avatar、weapon_socket_r |
| 动画 | arm_anim_maya_v001.ma 或相应 .max，帧率与片段表 | 独立动作副本 arm_actions_client.fbx | ClientPresentation.controller 的 Idle / Cast / Hit |
| 场景资产 | courtyard_modules 原稿、石材 PSD | 墙、门框、地板 FBX、颜色图和材质 | 模块 Prefab、环境碰撞 |
| 地编与光照 | 场景布局、路线、观察点和光照选择 | 庭院 Scene、TerrainData、使用时的烘焙数据 | 通行空间、镜头环境、目标位置 |
| 特效 | cast_soft / cast_ring 的 PSD 与效果约定 | RGBA PNG、材质、Charge / Release / Impact Prefab | Profile 与角色表现组件 |
| TA | Shader Graph、参数说明、检查器源码及规则版本 | 经本轮选择的材质模板与 Editor 工具 | 制作检查、可选效果替换与问题定位 |
| 客户端 | 交互约定、代码、配置与集成记录 | 角色 Prefab、Profile、场景入口 | 创建、输入、动作身份、反馈、清理与重开 |

</div>

Maya 与 3ds Max 是两条可选制作路径。不要把 Maya 输出的基础手臂与另一套 Max 骨架动作随意混用，也不要求同一份资产依次经过两套软件。先在交接表里写清本轮采用哪一条。

第 09 篇的 Shader Graph 版本也不是强制前置。第一轮可以保留第 08 篇的 URP 粒子材质，等基本组合能观察后，再单独替换 TA 版本。这样出现差异时，才知道这一轮具体改了什么。

### 固定一份集成清单

为本例建议一份 `courtyard-playable-handoff.md`。其中的 v001 是人工约定，不能代替实际文件版本或提交记录：

```text
集成编号：courtyard-playable-v001
证据状态：组装设计，尚未制作与运行
制作路径：Maya 或 Max，实际执行时二选一并记录版本
骨架：arm_rig.fbx 的实际来源与版本
动作：arm_actions_client.fbx；24 fps；Idle 1—25 / Cast 49—73 / Hit 97—109
模板：ResearcherPresentation.prefab
表现：ResearcherArt_v001.asset；release=0.5 / finish=1.1 / hit=0.5 秒
场景：CourtyardPlayable.unity
规则：距离 4 米；环境遮挡检查；释放时再判定；一次反馈后完成
包与设置：执行时记录 Unity、URP、Input System、Cinemachine 实际版本
检查：逐项填写未执行 / 已观察结果与证据位置
```

实际制作后，再补资产提交号、导出设置、导入设置以及引用关系。清单中的某个文件名存在，不表示对应软件操作已经完成。

## 先做一个可替换的占位版本

先把视觉复杂度降到能分辨关系：地板和墙用简单构件，角色用胶囊，目标用立方体，杖尖用空挂点。效果仍需有最小可用的 Particle System 方案，不能把一个不存在的 Prefab 引用留空后继续记录“特效通过”。

这个版本先回答：角色在哪里出现，能往哪里走，什么条件允许施法，反馈属于哪次动作，退出是否清理。它不负责证明模型、蒙皮、完整动作或最终画面正确。

### 保留前文的局部测试场景

从第 07 篇的庭院或第 10 篇的接入场景另存为：

```text
Assets/Scenes/ArtProduction/Courtyard/CourtyardPlayable.unity
```

保留 `CourtyardModuleTest`、`CourtyardWalkthrough`、`CourtyardVfxTest` 和 `CourtyardClientIntegration`，不要为了综合篇覆盖掉它们。组合出现问题时，这些局部入口能帮助判断输入资产本来就有问题，还是组装之后才出现差异。

综合场景只启用一套输入与表现入口：停用旧 WalkProbe、NavProbe、CourtyardVfxPreview 和 CourtyardClientDemo 对象。角色 Prefab 上的移动组件会保留；停用的是场景中的旧测试角色，避免两个身体、两个时钟同时响应按键。

### 固定场景坐标，先检查能走到哪里

沿用第 07 篇尺度：1 单位对应 1 米，Y 向上，入口朝 +Z。庭院范围约为 X=0—8、Z=0—6，地面顶面 Y=0；门洞净开口 X=3—5。

<div role="region" aria-label="综合场景的位置与观察关系，可横向查看" tabindex="0" style="overflow-x:auto">

| 对象 | 位置或约定 | 用途 |
| --- | --- | --- |
| SpawnPoint | (4, 0.05, -1)，朝 +Z | 入口外出生，不直接跳过庭院路线 |
| 屏风模块枢轴 | (3, 0, 2)，按前文模块尺寸摆放 | 遮住入口对目标的部分视线 |
| 路线 B / C / D | (6,0,1) / (6,0,3.5) / (4,0,4) | 穿门后向右绕行，再接近目标 |
| Target | 中心 (4,0.75,5)，尺寸 (0.6,1.5,0.6) | 教学目标，使用 Target Layer |
| ImpactAnchor | 世界位置 (4,1.1,4.65)，设为 Target 子物体 | 在目标前表面外侧显示反馈 |
| IdleCameraTarget | (4,1,-1)，旋转从 (15,0,0) 起调 | 角色离场时供跟随镜头观察 |

</div>

A→B→C→D 是建议走查路线，不会强制玩家只走右边。先尝试直穿屏风、贴门柱和反向绕行，确认模型的可见表面与实际 Collider 对齐。入口外要有落脚面，否则角色会在进入庭院之前下落。

本例的玩家移动仍由 CharacterController 驱动。NavMesh 可以作为第 07 篇的独立检查保留，但这条玩家路径不需要再加一个 NavMeshAgent，也不让两套组件同时改变同一个根节点。

### 地形与光照先选择一个稳定条件

沿用庭院内部的模块地板；外围 Terrain 只承担外部地形，避免与通行地面大面积重叠。先用第 07 篇的实时光照基线、固定环境光和质量设置，不在接入动作的同一轮同时改曝光、材质和烘焙方案。

需要比较烘焙光照时，另存观察条件并带齐 Lighting Settings、生成数据与使用的探针。移动墙体后，光照与导航数据分别判断是否需要重建；二者不是同一份烘焙结果。

## 组装角色：身体、视觉、挂点和镜头分开

复用第 10 篇的 `ResearcherPresentation.prefab`、`CourtyardArtProfile` 与 `CourtyardActorView`。这一篇只补场景交互，不重写一套粒子生命周期。

```text
ResearcherPresentation                   // 根缩放为 1，Layer=Player
  CharacterController                    // Center=(0,0.9,0)，Height=1.8，Radius=0.3
  CourtyardWalkProbe                      // 保留第 10 篇 CanMove 补丁
  CourtyardActorView                      // Animator 与杖尖字段明确绑定
  CourtyardPlayableAnchors                // 本篇新增，保存实例内引用
  AimOrigin                              // 局部位置 (0,0.9,0)，教学遮挡查询起点
  CameraTarget                           // 局部位置 (0,1.4,0)，俯仰从 15° 起调
  VisualRoot
    CapsulePlaceholder                   // 占位时使用，移除自带 Collider
    ArmVisual                            // 有实际手臂时加入，根位置/朝向实测对齐
      Animator
      skeleton / ... / weapon_socket_r
        Staff
          vfx_socket_staff_tip
```

这棵树表达引用和职责，不要求把 weapon_socket_r 从实际骨骼父节点中拖出来凑层级。占位版本可在 VisualRoot 下直接提供杖尖；替换手臂之后，再绑定真实挂点。

### 只让一个组件驱动身体

CharacterController 采用第 07 篇的接触参数，视觉胶囊不重复添加 Collider，根节点不叠加 Rigidbody。Animator 关闭 Apply Root Motion，身体位置由移动探针改变。

第 10 篇已经要求在方向计算之后、Move 之前，根据 `view.CanMove` 将水平输入清零。这里必须保留该补丁；施法和受击期间仍处理重力，不能直接退出整个移动 Update。`CharacterController.Move` 本身不应用重力，见 [Unity Move 文档](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html)。

WASD 仍沿世界 X/Z 移动，没有镜头相对输入、跳跃或移动 Blend Tree。操作时先松开移动键，再按 Space；同帧输入顺序、转向手感和移动锁定还需实际走查，不能当作正式角色控制器交付。

### 视觉替换不改变身体与请求接口

将法杖 Prefab 放到 weapon_socket_r 下，先检查握柄位置、朝向和尺度，再在杖尖建立或复核 `vfx_socket_staff_tip`。不要为了让法杖接上手，缩放整个角色移动根；那会同时改变碰撞、镜头与特效空间。

第 04 篇的基础手臂与第 05 篇的动作必须匹配。独立动作副本沿用 Generic / Copy From Other Avatar，Controller 仍为 Idle / Cast / Hit、Speed=1，不建立自动过渡。

保留第 10 篇的事件隔离：`arm_actions_client.fbx` 中移除旧预览事件，不能同时用第 08 篇事件和本篇时钟触发释放。占位阶段 Animator 可以留空，但此时只检查通行与粒子请求，不记录动作通过。

### 把场景引用与 Prefab 内引用分开

Prefab 不能预先绑定将来场景中的 Target 或 SpawnPoint；这些引用交给场景入口。Prefab 内的 CameraTarget 和 AimOrigin 则可以显式保存，实例化后得到本实例对应的引用。

新建 `Assets/Scripts/ArtProduction/CourtyardPlayableAnchors.cs`：

```csharp
using UnityEngine;

public sealed class CourtyardPlayableAnchors : MonoBehaviour
{
    public Transform cameraTarget;
    public Transform aimOrigin;
}
```

挂在角色根节点，分别绑定它自己的两个子节点。不要在每次施法时递归搜索一个碰巧同名的目标。

## 只用一个释放时钟，把显示请求接回交互

第 10 篇用 I 键手工请求目标反馈，适合检查动作身份。综合篇将这一步接到释放通知：角色表现组件仍负责何时进入 Recover，场景入口收到通知后按本地教学规则决定是否显示反馈。

### 给原表现组件增加一个通知出口

在 `CourtyardActorView` 类内增加：

```csharp
public event System.Action<long> Released;
```

在原 Update 中，**进入 Recover、清理 Charge、创建 Release 并写出 release 日志之后**，加入一行：

```csharp
Released?.Invoke(activeId);
```

保留原有 ShowImpact、Cancel、Shutdown 与粒子记录。不要在 TryCast 中提前发通知，也不要为场景入口再加一个等待 0.5 秒的协程。否则改配置后就有两个时间来源。

通知只表达“该动作的表现时钟到达释放点”。正式战斗应由玩法规则提供自己的结果；本例为了组合展示，在通知中执行简化的本地交互检查。

### 距离、环境遮挡和画面可见性分别检查

本例约定：AimOrigin 到 ImpactAnchor 的三维距离不超过 4 米，且两点间没有 Environment Layer 的非 Trigger Collider。请求开始与释放时各检查一次。

这是一条用于教学的局部规则。它没有判断目标是否在摄像机视锥中，也没有模拟法杖射出的弹道；一条身体高度的查询线也不代表整个模型都能看见目标。实际技能可能允许穿墙、抛物线或范围选择，应使用自己的规则。

Environment 包含庭院墙、门柱、屏风与相应阻挡；Player、Target 和特效不要混进这个查询 Mask。Target 可以保留 Collider，但反馈点应位于其表面外侧。Mask 为空时，本例直接拒绝创建，避免把漏配误当作无遮挡。

查询的过滤能力参考 [Physics.Linecast](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.Linecast.html)。这些 Layer 是本文约定，需要实际配置，Unity 不会根据节点名字自动创建。

## 场景入口：创建、检查、反馈与重新开始

新建 `Assets/Scripts/ArtProduction/CourtyardPlayableDemo.cs`。复用前文代码后再加入以下脚本，**尚未执行 Unity 编译和运行**：

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public sealed class CourtyardPlayableDemo : MonoBehaviour
{
    public CourtyardArtProfile profile;
    public Transform spawnPoint, target, impactAnchor, idleCameraTarget;
    public CinemachineCamera followCamera;
    public LayerMask blockers;
    public float maxRange = 4f;

    CourtyardActorView view;
    CourtyardPlayableAnchors anchors;
    long lastRequest;
    bool completed;
    string note = "等待开始";

    void Start() => SpawnActor();

    void Update()
    {
        var key = Keyboard.current;
        if (key == null) return;
        if (key.rKey.wasPressedThisFrame) SpawnActor();
        if (key.backspaceKey.wasPressedThisFrame) DespawnActor();
        if (target != null && key.tKey.wasPressedThisFrame)
            target.gameObject.SetActive(!target.gameObject.activeSelf);
        if (view == null) return;
        if (key.escapeKey.wasPressedThisFrame)
        {
            if (view.CurrentActionId != 0)
            {
                view.Cancel(lastRequest, true);
                note = completed ? "已请求受击取消；本轮反馈已接受，按 R 重开"
                                 : "已请求受击取消；等待恢复后可重试";
            }
            else note = "当前没有可取消动作";
        }
        if (key.spaceKey.wasPressedThisFrame) TryBegin();
    }

    void TryBegin()
    {
        if (completed) { note = "本轮已完成，按 R 重新开始"; return; }
        if (!view.CanMove) { note = "当前动作尚未恢复"; return; }
        if (!CanReach(out string reason)) { note = reason; return; }
        Vector3 facing = impactAnchor.position - view.transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
            view.transform.rotation = Quaternion.LookRotation(facing);
        long id = view.TryCast(target);
        if (id == 0) { note = "表现组件未接受施法"; return; }
        lastRequest = id;
        note = "施法中；释放时复查距离和遮挡";
    }

    bool CanReach(out string reason)
    {
        reason = "";
        if (view == null || anchors == null || anchors.aimOrigin == null ||
            target == null || !target.gameObject.activeInHierarchy ||
            impactAnchor == null || !impactAnchor.gameObject.activeInHierarchy)
        { reason = "角色、目标或反馈挂点不可用"; return false; }
        Vector3 from = anchors.aimOrigin.position;
        Vector3 to = impactAnchor.position;
        if (Vector3.Distance(from, to) > maxRange)
        { reason = "距离太远，请接近目标"; return false; }
        if (Physics.Linecast(from, to, blockers, QueryTriggerInteraction.Ignore))
        { reason = "环境遮挡，请绕过屏风"; return false; }
        return true;
    }

    void OnReleased(long id)
    {
        if (view == null || id == 0 || id != lastRequest ||
            id != view.CurrentActionId) return;
        if (!CanReach(out string reason))
        {
            note = "释放时未显示目标反馈：" + reason;
            return;
        }
        if (view.ShowImpact(id, impactAnchor.position))
        {
            completed = true;
            note = "已接受一次目标反馈；等待动作结束，按 R 可重开";
        }
    }

    void SpawnActor()
    {
        DespawnActor();
        completed = false;
        lastRequest = 0;
        if (profile == null || profile.actorPrefab == null || spawnPoint == null ||
            target == null || impactAnchor == null ||
            !impactAnchor.IsChildOf(target) || blockers.value == 0 ||
            float.IsNaN(maxRange) || float.IsInfinity(maxRange) || maxRange <= 0f ||
            (followCamera != null && idleCameraTarget == null))
        { note = "场景配置不完整，请检查引用、Mask 和距离"; return; }
        target.gameObject.SetActive(true);
        var actor = Instantiate(profile.actorPrefab,
            spawnPoint.position, spawnPoint.rotation);
        var nextView = actor.GetComponent<CourtyardActorView>();
        var nextAnchors = actor.GetComponent<CourtyardPlayableAnchors>();
        if (nextView == null || nextAnchors == null ||
            nextAnchors.aimOrigin == null || nextAnchors.cameraTarget == null ||
            !nextAnchors.aimOrigin.IsChildOf(actor.transform) ||
            !nextAnchors.cameraTarget.IsChildOf(actor.transform) ||
            !nextView.Initialize(profile))
        {
            actor.SetActive(false);
            Destroy(actor);
            note = "角色配置不完整，已清理本次实例";
            return;
        }
        view = nextView;
        anchors = nextAnchors;
        view.Released += OnReleased;
        BindCamera(anchors.cameraTarget);
        note = "从入口穿门并绕过屏风，接近目标后按 Space";
    }

    void BindCamera(Transform cameraTarget)
    {
        if (followCamera != null)
            followCamera.Target = new CameraTarget { TrackingTarget = cameraTarget };
    }

    void DespawnActor()
    {
        if (view != null) view.Released -= OnReleased;
        BindCamera(idleCameraTarget);
        if (view != null)
        {
            var actor = view.gameObject;
            actor.SetActive(false);
            Destroy(actor);
        }
        view = null;
        anchors = null;
        lastRequest = 0;
        note = "角色已离场，按 R 重新开始";
    }

    void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 640, 112), "庭院综合练习");
        GUI.Label(new Rect(24, 38, 610, 80),
            "WASD 移动 / Space 施法 / Escape 取消 / R 重开\n" +
            "T 切换目标有效状态 / Backspace 离场\n" + note);
    }

    void OnDisable() => DespawnActor();
}
```

这里用简单 IMGUI 标签显示规则和原因，便于本地桌面练习；没有加入正式 UI 生产、手柄、触屏和本地化方案。输入沿用前文新 Input System；若项目使用程序集定义，还需给脚本所在程序集配置实际包引用，先处理编译错误再运行。

### 为什么通知发生在 Recover 之后

原 ActorView 的 ShowImpact 只接受当前编号、Recover 阶段、有效目标和首次反馈。如果把通知放在 phase 仍为 Cast 时，场景入口即使传对编号，也会被拒绝。

这里的调用顺序是：进入 Recover → 创建 Release → 通知场景入口 → 重新判定 → ShowImpact → 原 Update 继续检查是否完成。它没有依赖不同对象的 Update 顺序来猜测释放时刻。

不过两个时钟仍需区分：这里唯一的**释放决策时钟**在 ActorView，Animator 自身还在采样动画。通知没有证明动画恰好采样到释放姿势；低帧率下也可能同一帧走过多个判断。动画、表现和画面的对齐仍需实际观察。

### 目标失效和离场如何处理

目标在 Cast / Recover 中失活时，原 ActorView 会取消当前动作。若先运行到释放通知，CanReach 也会发现目标不可用。T 只是方便制造这个条件，不是正式目标管理系统。

释放时变成超距或遮挡，本例只拒绝目标反馈，释放闪光可以继续，动作按原时钟结束。是否改为取消、落空表现或重选目标，是另一条玩法决策，不能默默藏在特效 Prefab 里。

受击取消仍由原组件使编号失效并清理当前动作效果。Backspace / R 则先解除场景订阅、切换镜头目标，再停用角色，使其 Shutdown 清理全部所属粒子，最后销毁。重建后绑定的是新实例的 CameraTarget，不再使用旧引用。

这条代码路径没有异步加载或跨实例消息。以后引入异步结果时，仍需带上角色身份、生成代次和动作编号，不能只凭一个 long 在所有角色间通用。

## 在 Inspector 完成最后的接线

### 先用固定镜头检查输入与空间

场景中只保留一个实际输出的 Main Camera。固定观察起值可用第 07 篇的 `(4,8,-6)`、旋转 `(40,0,0)`、Vertical FOV=60°，再按实际画面调整。此时停用 CinemachineBrain，PlayableDemo 的 followCamera 留空。

新建场景空物体 `PlayableRoot`，挂 CourtyardPlayableDemo：

1. profile 绑定 `ResearcherArt_v001.asset`；该资产引用本轮角色模板和三段效果根。
2. spawnPoint、target、impactAnchor 绑定本场景对象；ImpactAnchor 必须位于 Target 子层级。
3. blockers 只选实际的 Environment Layer，检查屏风和门柱 Collider 所在层。
4. maxRange 设为本轮教学起值 4；根、视觉父节点和挂点保持约定的单位缩放。
5. 确认角色 Anchors 字段、View 的 socket / Animator、Controller 与 Avatar 都对应本轮输入。

点击 Game 窗口获得焦点。先确认角色从 A 出现、能够移动、在远处拒绝施法，再做镜头切换。如果连固定视角下都找不到角色，先检查实例、位置、Layer 和材质。

### 再接第三人称观察

复用第 07 篇 Cinemachine 3.1 的 CinemachineCamera 和 Third Person Follow 设置，Main Camera 上启用 CinemachineBrain。将 CourtyardCamera 绑定到 followCamera，并绑定场景中的 IdleCameraTarget。

脚本通过 `CinemachineCamera.Target` 设置 TrackingTarget，保留默认的非独立 LookAt 方式；这与 [CameraTarget](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CameraTarget.html) 和 [CinemachineCamera.Target](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineCamera.html#Unity_Cinemachine_CinemachineCamera_Target) 的接口对应。这里代码针对 3.1，不与 Cinemachine 2 的旧类型混用。

Third Person Follow 的环境碰撞过滤继续沿用前文，排除 Player；这与施法 blockers 是两份用途不同的设置。前者控制镜头调整，后者决定这次教学交互是否被环境阻挡，修改一份不会自动修正另一份。

跟随镜头会受 CameraTarget 的位置、俯仰与角色转向影响。本例没有鼠标环绕；施法转向目标时的镜头变化、穿门收近和屏风边缘行为必须再走查。自动绑定引用只补上连接，不能证明镜头体验合格。

## 用一条完整路线观察组合

先记录场景、Profile、资产和脚本版本，以及固定还是跟随镜头，再开始下面的练习。下列均为待观察项目。

### 第一轮：从入口走到完成

1. 按 R，从 A 开始。检查只有一个当前角色，镜头指向其目标或约定观察点。
2. 在入口外按 Space。按本例参数应因距离过远拒绝，不创建 Charge。
3. 穿过门洞，在屏风前找一个距目标小于 4 米的位置再按 Space。若查询线穿过有效环境 Collider，应显示遮挡原因。
4. 沿 B→C→D 绕行，松开移动键，按 Space。观察转向、移动锁定、Cast 与杖尖蓄光。
5. 在名义 t=0.5 秒观察 Release 与目标反馈请求；确认日志属于同一动作编号，画面与挂点对应。
6. 在名义 t=1.1 秒观察恢复；目标圆环等尾部允许按剩余寿命自然结束，不要求最后一粒与动作同时消失。
7. 再按 Space 应提示本轮已经完成；按 R 回到入口，旧角色与世界效果不应继续存在。

HUD 的完成提示表示 ShowImpact 接受了一次请求。它不能证明粒子在镜头中可见，更不能证明伤害已经结算；仍需同时看画面、日志和实例记录。

### 第二轮：在组合处制造失败

<div role="region" aria-label="综合交互的失败分支练习，可横向查看" tabindex="0" style="overflow-x:auto">

| 条件 | 操作 | 要确认的连接 |
| --- | --- | --- |
| 释放前受击 | Space 后立刻 Escape | 当前编号失效，Charge 清理，不再显示该次目标反馈 |
| 目标失效 | Cast 中按 T | 目标不可用，动作取消或请求被拒绝；重新启用后旧请求不复活 |
| 释放时遮挡改变 | Play 中暂停，移动屏风，恢复后等待释放 | 规则复查是否拒绝反馈；记录物理更新后的真实结果 |
| 动作或尾部存在时重开 | 按 R | 解除旧订阅、切镜头、清理旧实例，再绑定新实例 |
| 场景入口停用 | 停用 PlayableRoot | 当前角色与世界效果清理；重新启用后按 R 初始化 |
| 缺少绑定 | 故意移除 Anchors 引用或效果引用 | 不保留半初始化角色，提示能定位到配置 |
| 更换视觉 | 胶囊换为对应手臂和法杖 | 身体路线保持，蒙皮/动作/握柄/杖尖另外复查 |

</div>

想检查重复反馈和旧编号，继续用第 10 篇的专用测试入口；不要把两个场景 Driver 同时打开。综合入口每次只提交一次反馈，不能拿它证明去重的所有异常分支都已覆盖。

## 逐份替换，让制作产物真正进入场景

### 从 Photoshop 参考到模型和材质

原画修改首先影响制作判断。例如法杖握柄位置改变，需要回到 Maya / Max 检查模型握持、UV 和材质；PNG 参考图本身不会替客户端自动移动 Staff。

替换 staff.fbx 或颜色图时，按第 03 篇复核单位、坐标、纹理语义与材质引用。保留原稿和导出对应关系，检查 Unity `.meta`；新建一份文件与更新原文件可能带来不同的资产身份，不能只看名字一样。

### 从绑定和动作到角色视觉

使用匹配的 arm_rig 与动作副本，先在局部场景确认 Avatar、Clip 和蒙皮，再接回综合 Prefab。将 Staff 与 View.socket 重新绑定到实际骨架挂点，检查 CameraTarget、AimOrigin 仍属于本实例。

检查 Cast 释放姿势时，既看手臂，也看握柄和杖尖的轨迹。只要骨骼路径或法杖枢轴改变，过去正确的挂点引用和偏移就需要重新确认。

### 从场景构件到实际通行

替换模块保持约定枢轴、尺度、门洞和地板高度，再看接缝、颜色图大小与材质。可见门洞换好了，Collider 仍可能保留占位盒；模型、身体阻挡、交互遮挡与镜头碰撞需要分别看。

地编调整屏风时，检查路线、目标显露、CanReach 的查询线和镜头收近。若保留导航和烘焙光照对照，也要确认它们是否仍对应当前布局。

### 从特效与 TA 模板到一次反馈

保留 Charge / Release / Impact 的根系统与空间约定，先在第 08 篇局部入口确认素材、材质和单次播放，再替换 Profile 引用。检查根与子系统的 Looping、Play On Awake、Stop Action 和材质。

第 09 篇的只读检查器可以发现其规则范围内的静态问题，不能证明 Shader 在目标平台可用，也不能证明运行清理或画面可读性通过。TA 版本单独对照同一场景、镜头和交互，不在同一轮顺便改尺寸、颜色、触发与质量档。

每次替换之后都走一次 A→D→施法→R。这样制作人员看到的是自己的产物进入组合后发生了什么，而不是收到一句“已经导入”。

## 追踪一次修改：施法从 1 秒改为 1.5 秒

假设希望准备动作更从容，把第 05 篇 Cast 的名义时长由 1 秒改为 1.5 秒，释放仍位于片段中点。下面是修改设计，不是已发生的修改结果。

### 在制作端修改节奏，保留旧版对照

将选定制作路径的动画原稿另存 v002。保持 24 fps、骨架和 Idle / Hit 不变，把 Cast 原先 49、55、61、67、73 的关键时点调整为 49、58、67、76、85。

49→85 有 36 个帧间隔，在 24 fps 下名义时长为 1.5 秒；释放姿势在 67，相对起点为 18/24=0.75 秒。不能把 37 个帧编号直接算成 37/24 秒，也不要通过修改整个场景帧率意外改变其他片段。

这只是整体拉长的教学安排。实际动作还要在 Maya Graph Editor 或 Max Curve Editor 中复看节奏、轨迹、切线和持握，不能假定拉伸关键帧就获得了更好的表演。

按原 FBX 烘焙、骨架与单位约定导出。创建用于对照的 `arm_actions_client_v002.fbx`，重新确认 Generic 与 Source Avatar、Idle / Cast / Hit 的范围；Cast 改为 49—85，Hit 仍为 97—109。核对导入后的真实 Clip 时长，再决定是否采用下面参数。

### 改动作文件，还必须改哪些引用

<div role="region" aria-label="动作时长变更的依赖清单，可横向查看" tabindex="0" style="overflow-x:auto">

| 对象 | v001 教学值 | v002 设计值与复查 |
| --- | --- | --- |
| Cast 源范围 | 49—73，名义 1 秒 | 49—85，名义 1.5 秒；检查实际导出/导入长度 |
| Cast 释放姿势 | 61，相对 0.5 秒 | 67，相对 0.75 秒 |
| ClientPresentation Controller | 引用 v001 动作副本 | 复制成 v002 Controller，Cast 引用新副本，Speed 仍为 1 |
| ResearcherPresentation Prefab | Animator 使用 v001 Controller | 复制成 v002 模板并改 Controller；其他引用保持可对照 |
| ResearcherArt Profile | release=0.5，finish=1.1 | 复制 v002，actorPrefab 与 charge 分别指向 v002 模板和蓄光；release=0.75，finish=1.6，hit=0.5 |
| Charge | 单粒子 Lifetime=0.45 秒 | 独立 v002 效果中考虑延长至 0.70 秒；重看渐变与提前淡出 |
| Release / Impact | 在收到释放/反馈请求时创建 | 仍由通知链触发，不另留 0.5 秒计时器；尾部寿命独立评估 |
| 旧预览动画事件 | 旧副本可能有第 08 篇事件 | 本路径仍移除不用的事件；其他事件路径需另行更新与检查 |
| PlayableRoot | 引用 v001 Profile | 停止 Play 后选择 v002 Profile，再创建实例比较 |

</div>

finish=1.6 表示本教学时钟留出名义 0.1 秒余量，不是通用后摇规定。Cast 在 1.5 秒结束后如何保持末帧、回到 Idle 时是否突变，都需要看实际 Controller 与画面。

Charge 的 Duration 和单粒子 Lifetime 也不同。第 08 篇采用起始一次 Burst；只把系统 Duration 改成 0.75，并不会让出生粒子的 0.45 秒寿命自动变长。若改 Lifetime，要重新观察归一化颜色/大小曲线，以及释放时清理蓄光是否出现跳变；持续发射方案则另行约定停止方式。

### 两个容易漏掉的修改位置

第一处是 Controller。导入新 Clip 后，如果 Animator 还引用旧 Controller，或者新 Controller 的 Cast 仍引用旧 Clip，Profile 上写了 v002 也不会自动切换动作。

第二处是事件。Unity 允许在导入片段中保存 Animation Events，见 [导入动画事件](https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationEventsOnImportedClips.html)。本路径已主动停用它们；若另一条预览路径仍依赖事件，需要独立更新标记并观察，不能假定这次导出会自动迁移正确时点。

新 Profile 是为了保存一组可比较的组合，不是在 Play 中改共享配置。ActorView 保存着同一配置资产的引用；运行中修改可能使一个动作前半段用旧值、后半段用新值，失去比较条件。

### 修改后重新走哪些分支

在相同镜头、路线和质量设置下，分别运行 v001 与 v002，记录输入起点、进入 Cast、释放、接受反馈、恢复和最后效果消失的位置与时间。日志和录像都从实际运行取得，不预先填入“与设计一致”。

取消也要按新阶段重新试：例如名义 0.6 秒在 v001 已释放、在 v002 仍准备，不能拿同一个绝对时刻的两张截图宣称取消行为相同。按准备段、释放后和恢复段分别比较。

再检查 R 重开、目标失效、当前动作尾部与角色离场。需要回退时，选择保存的 v001 Profile，同时确认它指向 v001 Prefab、Controller 和效果；不要只把 revision 文本改回去。

## 从编辑器场景走到独立 Player

Play 模式可观察组合，仍然不能替代一次实际 Player 运行。输入焦点、图形 API、Shader 支持、质量设置和资源包含方式都可能改变结果。

Unity 6 使用 Build Profiles 管理构建配置；窗口名称和能力见 [从编辑器创建构建](https://docs.unity3d.com/6000.0/Documentation/Manual/BuildSettings.html)。本例建议先选择可用的桌面平台，记录实际版本，再执行：

1. 保存 Scene、Prefab、材质、Profile 和相关修改，确认 Console 无编译错误。
2. 在目标 Build Profile 的场景列表中加入并启用 CourtyardPlayable，确认实际使用的是该列表；第一轮只从这个教学场景启动。
3. 核对该平台的 URP、图形质量和新输入配置，构建到项目 Assets 外的独立目录。
4. 启动 Player，重复 A→D、失败请求、施法、取消和 R 重开，保存日志和画面。
5. 更改为 v002 后重新构建并记录版本，不以编辑器中的配置改变推断旧 Player 已更新。

这些步骤尚未执行。本例使用场景、Profile 和 Prefab 的直接引用，没有实现 AssetBundle、Addressables 或 YooAsset 构建分发；资源系统的交付链可继续读[资源加载生命周期]({{< relref "delivery-engineering/delivery-resource-pipeline-05-loading-lifecycle.md" >}})。销毁角色也不等于释放全部共享资源。

## 留下别人能够接着复现的材料

综合示例的交付不能只剩一张画面。至少需要制作来源、Unity 工程依赖、具体操作和结果记录彼此对应。

建议组织为下面几类，路径是本篇设计，并非仓库已有的完整工程：

```text
ArtSource/                            # Unity Assets 外，保留可编辑原稿
  Researcher/                         # 设计、模型、绑定、动画、纹理
  Courtyard/                          # 模块与表面原稿
  VFX/                                # PSD 与效果说明
UnityProject/
  Assets/Art/...                      # FBX、PNG、材质、Controller、Prefab、Profile
  Assets/Scripts/ArtProduction/        # 前文组件、本篇 Anchors 与 PlayableDemo
  Assets/Scenes/ArtProduction/Courtyard/CourtyardPlayable.unity
  Packages/manifest.json
  Packages/packages-lock.json
  ProjectSettings/                    # 包括版本、输入、Layer、图形等设置
Evidence/courtyard-playable-v001/      # 实操后保存清单、日志、录像与观察表
```

Unity 资产连同 `.meta` 保存；可编辑原稿与可公开使用的素材授权一并说明。运行生成的临时缓存不能代替可复现工程，也不需要为了“文件齐全”把整个 Library 当作交接内容。

观察表至少写明：谁在什么版本和平台，使用哪套素材与配置，从哪里开始，按了什么键，看到什么，证据放在哪里，哪些部分未执行。遇到异常再回到具体依赖，例如“杖尖跟随错误”需区分挂点、装备偏移、动画与模拟空间，不直接给某个岗位下结论。

<!-- DATA-TODO: 制作并公开可授权的原稿、导出资产及 UnityProject，记录 Unity/URP/Input System/Cinemachine 实际版本与完整引用；编译前文脚本、Released 补丁、Anchors 和 PlayableDemo，补真实 Inspector 接线及占位/局部手臂对照。 -->
<!-- DATA-TODO: 执行固定/跟随镜头的 A→D、距离/遮挡拒绝、正常施法、取消、目标失效、重开/离场；补日志、实例清理与画面录像。当前只有流程推演，不预填通过结果。 -->
<!-- DATA-TODO: 实作 24 fps 的 Cast 49—73→49—85，保存 v001/v002 原稿、FBX、Controller、Prefab、Profile 和 Charge 对照；记录导入长度、实际释放/恢复与 Player 构建及设备走查。 -->

第 12 篇会继续讨论怎样在同一份内容上共同判断画面、功能、体验和性能，以及如何依据证据确定修复与复查范围。这一篇先把可操作路径和依赖接起来。

**综合示例的价值，在于让每份制作产物都能沿明确的引用、请求和修改路径回到玩家的一次操作。**

## 前置与延伸阅读

- [02｜原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})：设计参考怎样交给下游。
- [03｜角色资产]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})、[04｜绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}})、[05｜动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})：法杖、手臂与动作的制作来源。
- [06｜场景资产]({{< relref "delivery-engineering/game-art-production-06-environment-assets.md" >}})、[07｜地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})：庭院构件、路线、光照及独立走查。
- [08｜特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})、[09｜TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})、[10｜客户端接入]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})：效果制作、技术模板与本篇复用的表现生命周期。
- [技能表现的交付标准]({{< relref "system-design/skill-system-28-artist-vfx-delivery-standard.md" >}})：进一步理解技能表现接口与协作。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
