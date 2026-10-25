---
title: "07｜地编：怎样把资产组织成可玩的空间"
slug: "game-art-production-07-level-art"
date: "2026-10-25"
lastmod: "2026-10-25"
description: "沿用墙、门框和地板，在 Unity 搭建庭院、组织路线与视线，配置外围地形和光照，再分别走查角色碰撞、导航与第三人称镜头。"
draft: false
featured: false
content_stage: "article"
weight: 87
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 7
type: "reading-series"
reading_title: "地编：怎样把资产组织成可玩的空间"
reading_summary: "从灰盒和玩家视角出发，在 Unity 组织庭院、Terrain 与光照，并用角色移动、NavMesh 和 Cinemachine 分别检查空间。"
---

上一章的墙、门框和地板有了统一尺寸，也能拼成一个庭院。但模块接缝正确，还不能回答：玩家进门后往哪走，什么时候发现目标，转弯时镜头会不会被墙挤到角色身上？

地编接手的是这些空间关系。墙既是建筑构件，也会限制通行、截断视线、投下阴影；一块石头既能补画面层次，也可能堵住路线或推近镜头。单件资产的效果，要放回玩家持续移动的过程中判断。

**地编的核心，是把视觉、通行、观察和运行数据组织成同一个可体验的空间。** 本篇沿用前一章的小庭院，从 Unity 编辑器中的搭建走到 Play 模式走查。

> **读这篇之前**：先看[第 06 篇场景资产]({{< relref "delivery-engineering/game-art-production-06-environment-assets.md" >}})，了解三种模块的尺寸、枢轴、Prefab 和碰撞约定；设计依据来自[第 02 篇原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})。完整关卡设计理论归 GamePlay，本篇重点是把空间意图落实到引擎。

> **版本与证据范围**：操作方案参考 Unity 6（6000.0）/ URP 17、AI Navigation 2.0、Cinemachine 3.1 与 Input System 1.11 官方资料。坐标、路线、参数与示意图为本文教学选择；尚未制作实际 Unity 工程，也未执行脚本编译、角色通行、寻路、镜头、烘焙或设备测试。下面的操作、观察条件和预期问题均待实操，原理图不代表已生成的 NavMesh 或运行画面。包的小版本可能调整面板名称，实操时应记录实际安装版本。

## 地编要协调的，是四个相互关联的空间

“这里放一面墙”至少会改变四件事：

<div role="region" aria-label="四种空间及其检查方式，可横向查看" tabindex="0" style="overflow-x:auto">

| 空间 | 要回答的问题 | Unity 中的主要表达 |
| --- | --- | --- |
| 视觉空间 | 看到了什么，重点是否清楚，前后景怎样分层？ | 模型、材质、地形、灯光、雾和实际摄像机 |
| 角色通行空间 | 身体能否通过，哪里会阻挡、滑动或落下？ | Collider、CharacterController 或项目实际移动系统 |
| 导航空间 | 这种体型的寻路对象怎样到达目的地？ | NavMeshSurface、Agent Type、NavMeshAgent、区域与连接 |
| 镜头活动空间 | 观察点能否跟随，墙边和门洞会怎样挤压镜头？ | 镜头规则、碰撞检测、距离、视角与遮挡处理 |

</div>

这四种表达不要求使用相同几何精度。石墙可以有凹凸表面，行走碰撞用简化盒体；导航又根据代理半径，从可走表面边缘留出距离；第三人称镜头还要为角色背后的观察位置寻找空间。

它们需要相互对照：看起来是洞的地方却走不过，视觉暗示与碰撞就冲突；导航显示有路，实际角色却卡在台阶，代理设置和移动能力就没有对齐。

### 分工由团队决定，问题需要有人负责闭环

关卡设计通常先说明入口、目标、路线、交互和节奏；场景资产制作提供构件；地编把它们摆成空间，并在玩家视角中反复调整。灯光工作可能独立，也可能由地编或场景美术兼任。TA 和客户端共同处理工具、渲染、导航、加载及运行约束。

不能只凭岗位名称判定谁必须修改。比如“入口太暗”可能需要改灯光，也可能是门洞过深、材质过暗或曝光条件不稳定。先固定观察条件、找出原因，再确认修改负责人。

本篇设一个小目标：**角色从门洞进入，受到右侧路线提示，绕过屏风墙，看到后方的施法目标，并走到目标前停下。** 这是空间练习，还没有加入敌人、任务、技能判定或正式交互。

## 先用灰盒确定路线和视线，再决定细节密度

灰盒是用简单几何表达空间的工作状态。它保留尺度、阻挡和关键轮廓，暂时省去精细表面。这样移动一道墙时，可以先判断路线变化，不必同时处理装饰和贴花。

### 给庭院设一套可以复查的坐标

沿用上一章：Unity 的 1 单位对应 1 米，Y 向上，入口朝向 +Z。庭院墙线围成 X=0—8、Z=0—6 的区域；地面顶面为 Y=0。门框位于前墙中段，净开口在 X=3—5，宽 2 米、高 2.5 米。

新增一道 2 米屏风墙，放在入口内侧：

- 使用 `WallStraight2m.prefab`，位置 `(3, 0, 2)`，旋转 `(0, 0, 0)`，缩放为 1。
- 它的实体覆盖 X=3—5、Y=0—3、Z=1.9—2.1。
- 后方目标先用立方体占位：中心 `(4, 0.75, 5)`，尺寸 `(0.6, 1.5, 0.6)`，底部接地。保留 BoxCollider，正式施法反馈后续再接。

设置四个空物体作为走查点，它们只记录位置，不带 Collider：

<div role="region" aria-label="庭院四个走查点，可横向查看" tabindex="0" style="overflow-x:auto">

| 点 | 地面坐标 | 在这里检查什么 |
| --- | --- | --- |
| A：入口外 | (4, 0, -1) | 门洞、进入方向、目标是否被屏风挡住 |
| B：右侧入口内 | (6, 0, 1) | 是否容易理解右侧绕行，身体转弯是否顺畅 |
| C：屏风右后侧 | (6, 0, 3.5) | 目标开始显露，镜头绕墙是否突变 |
| D：目标前 | (4, 0, 4) | 停留空间、目标可读性、回头看入口的关系 |

</div>

D 是目标前的站位，目标物体在更后方。不要把寻路终点设进目标的实体碰撞中。

{{< art-level-layout >}}

### 路线、视线和玩家选择要分别画

A→B→C→D 表示本例准备检查的路线。它不等于唯一通道：屏风两侧都可能通行，玩家也可能向左探索。若希望右侧更容易被发现，可以在右侧保留连续的地面方向、较清楚的明暗对比和一个低矮提示物，再用实际游玩观察它是否有效。

不要为了引导，把所有地方都塞满障碍，或者让最亮的地方同时也是死角。先说清希望引导什么，再判断形状、材质和光照是否共同支持这个意图。

视线则是另一组关系：从 A 朝 +Z 看，屏风应遮住目标主体；从 C 看向目标，应逐渐显露。这是按当前尺寸提出的观察条件，仍需用实际镜头高度、朝向、FOV 和宽高比确认。高俯角镜头可能直接越过屏风，路线图上的遮挡便不成立。

### 留出身体和镜头的余量

本例先用高 1.8 米、直径 0.6 米的胶囊代表角色，不能因此断言 0.61 米的通道就够用。转向、台阶、控制器的接触余量、镜头偏肩、武器轮廓都可能提出额外需求。

同样，身体通过 2 米门洞，不代表身后 3 米的摄像机也能维持原位穿门。镜头需要提前收近、受约束移动，或使用另一套观察规则。门洞、屏风与墙角应从一开始就进入镜头走查。

## 在 Unity 中把模块组织成庭院

地编阶段消费的是上一章已经导入、配材质并带简化碰撞的 Prefab，不需要再把整个场景从 Maya 或 Max 导出一次。单件构件回源修改仍回到相应 DCC 原稿；摆放关系主要保存在 Unity 场景或组装 Prefab 中。

### 建立独立场景和清楚的层级

将上一章的 `CourtyardModuleTest.unity` 另存为：

```text
Assets/Scenes/ArtProduction/Courtyard/CourtyardWalkthrough.unity
```

保留原来的接口测试场景，便于区分“构件坏了”还是“这次摆放变了”。新场景可按以下层级整理：

```text
CourtyardRoot                 // 位置/旋转为 0，缩放为 1
  CourtyardGeometry           // 静态通行几何；后续加 NavMeshSurface
    Floors
    BoundaryWalls
    Entrance
    ScreenWall
    Target
  TerrainExterior             // 外围地形，与主通行几何分开
  Decoration                  // 非阻挡装饰；按需要补对应碰撞
  Lighting
  RouteMarkers                // A / B / C / D，无碰撞
WalkProbe                     // 手动移动探针，单独启用
NavProbe                      // 导航探针，单独启用
Main Camera
CourtyardCamera               // Cinemachine Camera
```

CourtyardGeometry 及 Floors 等分组节点也保持位置/旋转为 0、缩放为 1，避免层级变换改变下表的坐标含义。

创建 `Environment`、`Player`、`Decoration` 等 Layer。把墙、地板、目标和 Terrain 的实际碰撞所在对象归入 Environment；玩家归 Player。Layer 可以用于导航收集和镜头碰撞筛选，**Layer 名称本身不会自动决定物理阻挡**，还要核对 Physics 的 Layer Collision Matrix。

不阻挡的装饰可以没有 Collider；需要阻挡的装饰应另行明确其物理、导航和镜头规则。父物体改 Layer 时，检查是否将更改应用到相关子对象。

### 按接口摆放，先做一轮无装饰检查

三种 Prefab 的局部坐标约定与碰撞尺寸见上一章。这里把庭院主体的摆放再列一次；所有坐标均为单位根节点下的局部坐标，与本例世界坐标一致：

<div role="region" aria-label="庭院模块摆放坐标，可横向查看" tabindex="0" style="overflow-x:auto">

| 对象 | 位置 | Y 旋转 |
| --- | --- | --- |
| 12 块 Floor2m | (2i, 0, 2j)，i=0—3，j=0—2 | 0° |
| 后墙 4 块 | (0/2/4/6, 0, 6) | 0° |
| 前墙两端 | (0, 0, 0)、(6, 0, 0) | 0° |
| GateFrame4m | (2, 0, 0) | 0° |
| 左右侧墙各 3 块 | X=0 或 8，Z=0/2/4，Y=0 | -90° |
| 入口外两块地板 | (2, 0, -2)、(4, 0, -2) | 0° |
| 屏风墙 | (3, 0, 2) | 0° |

</div>

侧墙绕 Y 旋转 -90°，使模块的局部 +X 沿世界 +Z 延伸。入口外地板提供 A 点的落脚面；缺少它时，角色可能在进门前就下落。边界墙转角仍沿用上一章允许小范围相交的教学接法，正式建筑角件可另做。

使用移动吸附检查 1 米位置、90° 旋转和统一高度，再在 Inspector 中输入关键坐标。若父节点带缩放，面板上的“2 米”可能已经不是世界中的 2 米，应先恢复或明确转换关系。保持 Prefab 引用，不为了移动实例就随意 Unpack。

检查地板顶面、墙脚、门洞和屏风接地，再看共享石材的大小、方向和接缝。对同一个重复构件，优先用组合、转角、适量装饰与光影打破重复；拉伸一整块墙会连带改变形状与表面比例。

### 没有上游模型时，先用占位体保留空间条件

本系列尚未附带实际 FBX 和 Prefab。读者可以先按第 06 篇的几何范围，用 Unity Cube 在空根节点下组成墙、三块门框和地板：例如直墙子 Cube 中心 `(1, 1.5, 0)`、缩放 `(2, 3, 0.2)`；根节点仍为单位缩放。

占位体可直接使用 Cube 的 BoxCollider；换成正式 Prefab 后移除重复碰撞。不要只把整个门框做成一个实心盒子，否则视觉开口会被封住。

灰盒替换为正式资产时，保留原根节点的接口，再检查碰撞、包围盒和材质。占位体可帮助检查路线，但不代表正式资产的轮廓、阴影、性能与镜头结果已经通过。

## 地形放在哪里，怎样接住建筑外围

小庭院的铺装地面已经由模块提供，不必再把 Terrain 雕成每一块石板。这里给 Terrain 一个明确任务：承接墙外的土地与远处起伏，避免建筑悬在空中。

### 创建外围 Terrain，并把尺寸控制在练习范围内

在 `GameObject > 3D Object > Terrain` 创建地形，移动到 TerrainExterior 下，保留单位缩放。本例起始参数为：

- 世界位置 `(-8, -0.25, -8)`。
- Terrain Settings 中 Width=24、Length=24、Height=4，单位为米。
- 初始高度保持平坦，平面位于 Y=-0.25；先核对实际生成状态。

这块 Terrain 覆盖 X/Z=-8—16，包围庭院。高度为 4 表示高度数据的纵向范围，并不表示初始地面自动抬到 4 米。铺装板底面在 Y=-0.2，外围平地稍低，先避免与地板表面重合。

创建 Terrain 会产生对应资产，不能只保存 Hierarchy 里的物体。将其 TerrainData 放到场景旁的 `Terrain/` 目录，确认 TerrainCollider 引用同一份数据。菜单、雕刻入口与尺寸含义参考 Unity [Terrain 创建与编辑](https://docs.unity3d.com/6000.0/Documentation/Manual/terrain-UsingTerrains.html)和 [Terrain Settings](https://docs.unity3d.com/6000.0/Documentation/Manual/terrain-OtherSettings.html)。

### 先保护通行区，再雕外围起伏

选中 Terrain，在雕刻工具中用 Raise or Lower Terrain 增加墙外起伏，再用 Smooth Height 缓和过渡；需要固定高度时用 Set Height。先低强度、大范围，再局部修整，避免每次笔刷都制造尖峰。

本例将 X=0—8、Z=-2—6 及其紧邻边缘视为铺装保护区，不在这里随意刷高。它包含庭院与入口落脚面。外围坡地靠近墙脚时，也要检查 Terrain 是否穿进地板、堵住门洞或在墙边形成能爬出的坡道。

地形可以穿到不可见的建筑底部以遮住缝隙，但要区分视觉接合与可走表面。若未来需要角色从地板走到地形，必须给出坡度和高度连续的连接，再让实际移动系统验证；本篇主路线先留在铺装区内。

### 用 Terrain Layer 配表面，别把网格材质直接当成地形层

在地形的 Paint Texture 工具中创建 Terrain Layer，保存为例如 `TL_ExteriorStone.terrainlayer`，将上一章的石材颜色图作为临时 Diffuse Texture。先使用一个层确认引用和比例；再增加真实制作的土、草等表面，不能把尚未制作的素材写成已完成。

若重复周期仍取 2 米，在 Terrain Layer 的 Tiling Settings 中将 Size X/Y 设为 2，对照铺装检查纹理大小。这只是比例参照，并不意味着两种表面必须无缝连接。Terrain Layer 的纹理与平铺设置见 [Unity Terrain Layers](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TerrainLayer.html)。

URP 工程应使用兼容地形的材质，核对 Terrain 的 Material 采用 `Universal Render Pipeline/Terrain/Lit` 等对应方案；上一章给网格使用的 `URP/Lit` 材质不能直接代替 Terrain Layer。正常显示后，再决定是否需要植被、细节和额外法线，而不是先把密度拉满。

植被要检查高度、重复方向和对通道的遮盖。刷出的细节不天然具有与视觉一致的碰撞和导航阻挡；有相关需求时需要单独核对。

## 用光照组织空间，也要保留稳定的观察条件

灰盒可以先在简单实时光下判断空间。进入首轮美术搭建后，再用主光方向、环境亮度、材质对比和少量点缀组织视觉层次。

### 先建立能复现的实时光照基线

本例先保留一盏 Directional Light，Mode 设为 Realtime，旋转从 `(45, -30, 0)` 开始。这个角度只是起点：在 Game 视角观察门洞、右侧路线和目标，再决定要不要调整。

- 使用当前 URP 工程可用的阴影设置，检查灯光、Renderer 和 URP Asset 是否共同启用了阴影。
- 在 `Window > Rendering > Lighting` 的环境设置中，固定 Skybox 或环境颜色；记录光强、颜色与使用的单位。
- 暂时保持一致的后处理与曝光条件，先确认墙面、地面与目标的明暗关系。
- 用同一画面比较“调整前/后”，不要一边改灯光一边换相机，导致原因无法分辨。

目标要容易辨认，可以先用明度或色相差，而不是直接提高到过曝。右侧提示也不必是一盏强灯：地面方向、留白与轮廓可能已经足够。发光材质看起来亮，不等于它必然向周围投光。

不要用点光源逐一填补每块暗面。先判断是环境过暗、主光方向不合适，还是建筑厚度和遮挡本来就让它处于阴影。光源和阴影带来的开销，要到目标设备中结合实际配置测量。

### 雾和色彩在路线可读之后再加

若需要弱化远景，可从 Lighting 的环境 Fog 开始试验。比如 Linear、Start=15、End=40 米，是这处小场景的温和起点，主要影响更远的地形；材质是否响应雾仍需确认。

加入后重走 A→D：门洞、绕行空间与目标边缘是否还能分辨？不同天气或氛围版本，也应按相同观察点比较。若雾和颜色处理让目标与背景融合，应调整层次，不能只以静态截图的气氛作为完成标准。

本篇不展开体积雾、天空系统与昼夜循环。选择这些系统会增加灯光、材质、质量档和运行变化，需要另行确认预算与验证条件。

### 可选：布局稳定后再做烘焙光照

如果项目采用烘焙间接光，可以在实时基线可读后增加一轮方案：

1. 在 Lighting 窗口创建并关联独立 Lighting Settings Asset，保存到 `Lighting/`；启用 Baked Global Illumination。
2. 在当前 URP 配置支持的前提下，将主灯设为 Mixed，Lighting Mode 选择 Baked Indirect。它保留实时直接光与阴影，将间接光预计算到相应数据。
3. 对不移动的墙、地板和屏风设置 Contribute GI；需要接收光照贴图的 Renderer 检查 Receive GI 与光照 UV。上一章的平铺 UV 不能直接充当无重叠的光照 UV。
4. 采用传统 Light Probe Group 时，在角色活动区布置有上下高度的探针，例如四角附近 X=1/7、Z=1/5，分别在 Y=0.3 与 2.5 形成八个初始点；在门洞和屏风两侧补点，避免把探针埋进墙里。
5. 动态角色不标为静态烘焙对象，Renderer 按方案使用 Blend Probes。若工程已经采用 Adaptive Probe Volumes，则沿用对应工作流，不把两套方案当作都必须配置。
6. Generate Lighting 后检查日志、贴图接缝、墙脚漏光和角色经过明暗边界的变化，保存生成数据，再进入 Play 模式观察。

这里八个探针只是布点起点，不能保证屏风两侧的照明插值准确；应在亮暗变化和角色真正经过的位置加密、复查。Mixed 模式与烘焙设置参考 [Lighting Settings Asset](https://docs.unity3d.com/6000.0/Documentation/Manual/class-LightingSettings.html)，动态对象与探针的关系参考 [Lighting configuration workflow](https://docs.unity3d.com/6000.0/Documentation/Manual/lighting-configuration-workflow.html)。

移动屏风、改墙高、换材质或调整灯光后，需要判断哪些烘焙数据已经失效。NavMesh 烘焙与光照烘焙是两种独立过程，点击其中一个 Bake 不会自动更新另一个。

## 先让身体走一遍：碰撞检查需要实际移动系统

编辑器中拖动物体穿过门洞，可以观察尺寸，不能检验碰撞响应。真正的检查要让游戏的移动系统驱动角色；已经有角色控制器时，优先使用项目实际方案。

下面提供没有控制器时的最小替代步骤，只检查平地行走、转弯和阻挡，不承担正式角色系统。

### 配一个手动移动探针

创建空物体 `WalkProbe`，位置 `(4, 0.05, -1)`，缩放为 1，Layer=Player。添加 CharacterController：

- Center=`(0, 0.9, 0)`，Height=1.8，Radius=0.3。
- Step Offset=0.25，Slope Limit=45，Skin Width=0.03，Min Move Distance=0。
- 这些是练习起值，需要结合真实体型和移动方案调整。

添加 Capsule 子物体作视觉标记，局部位置 `(0, 0.9, 0)`、缩放 `(0.6, 0.9, 0.6)`，使默认高 2 米的胶囊变为高 1.8 米。移除子物体自动附带的 CapsuleCollider，由父节点 CharacterController 负责本轮碰撞；不要再叠加 Rigidbody 或 NavMeshAgent。

通过 Package Manager 安装项目兼容的 Input System，确认 Active Input Handling 启用新输入系统，按编辑器要求重启。本例按 [Input System 键盘接口](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Keyboard.html)直接读取按键，省略正式 Input Actions 与设备适配。

在 `Assets/Scripts/ArtProduction/CourtyardWalkProbe.cs` 创建以下教学脚本并挂到根节点。**该脚本尚未在 Unity 编译或运行；实操第一步应先确认 Console 无编译错误。**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class CourtyardWalkProbe : MonoBehaviour
{
    CharacterController controller;
    float verticalSpeed;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        var keyboard = Keyboard.current;
        Vector3 direction = Vector3.zero;
        if (keyboard != null)
        {
            direction.x = (keyboard.dKey.isPressed ? 1 : 0)
                        - (keyboard.aKey.isPressed ? 1 : 0);
            direction.z = (keyboard.wKey.isPressed ? 1 : 0)
                        - (keyboard.sKey.isPressed ? 1 : 0);
        }
        direction = Vector3.ClampMagnitude(direction, 1f);

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += -9.81f * Time.deltaTime;
        controller.Move((direction * 2f + Vector3.up * verticalSpeed)
                        * Time.deltaTime);

        if (direction.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
```

这里 WASD 沿世界 X/Z 移动，速度为每秒 2 米；它没有跳跃、镜头相对输入、加减速或台阶专门逻辑。`CharacterController.Move` 接收本次位移，碰撞会约束移动，重力需要调用方处理，见 [Unity Move 文档](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html)。高度、半径和接触余量的意义见 [Character Controller 组件](https://docs.unity3d.com/6000.0/Documentation/Manual/class-CharacterController.html)。

### 从直线、贴边和倒退检查碰撞

先用能看清场地的固定相机观察，再切到后面的第三人称镜头。没有现成观察镜头时，可以把 Main Camera 放在 `(4, 8, -6)`、旋转设为 `(40, 0, 0)`，透视 Vertical FOV 取 60°；检查画面覆盖入口和庭院后再按需要调整。此阶段先不让 CinemachineBrain 接管它。

进入 Play，点击 Game 窗口取得键盘焦点：

1. 从 A 正向通过门洞，检查落脚面和横梁附近是否存在意外阻挡。
2. 靠近门柱及屏风两端，分别正向、斜向贴边移动，观察是否被不可见盒体卡住。
3. 从 B 绕到 C，再到 D，检查地板拼缝是否抖动、被抬起或下落。
4. 沿原路倒退、反向绕行、贴外墙移动，确认背面与墙角的阻挡。
5. 暂停运行，选中实际 Collider 查看位置与尺寸，区分模型穿插、碰撞间隙和控制器参数。

如果门洞整体不可通过，先看是否仍有封住门口的大盒体；若只在个别接缝抖动，再核对地板高度、重复 Collider 和控制器设置。不要用关闭全部碰撞掩盖问题。

探针通过只说明这种体型和这段移动逻辑在这组条件下可通行。正式角色换成另一套控制器、速度或根运动后，仍要复查。

## 再让寻路对象走一遍：NavMesh 需要独立配置

手动行走依赖输入和物理阻挡，导航还需要一份表达可达关系的数据。画面里的地板不会自动变成可用的导航面。

### 收集明确的几何，按同一种体型烘焙

通过 Package Manager 安装项目兼容的 AI Navigation 2.0 包。选中 CourtyardGeometry，添加 NavMeshSurface。本例采用：

- Agent Type：创建或选择与练习对应的类型，Radius=0.3、Height=1.8、Step Height=0.25、Max Slope=45。
- Collect Objects：选择当前对象及子层级；本文核对的 2.0 文档称为 `Current Object Hierarchy`，部分小版本显示为 `Children`。
- Include Layers：Environment。
- Use Geometry：Physics Colliders；默认区域先用 Walkable，其他高级精度设置先保持默认。

这里 CourtyardGeometry 包含入口外地板、墙、门框、屏风与目标，但不包含角色和路线标记；TerrainExterior 位于其外，本轮不参与寻路。若要检查外围通行，需要将 Terrain 纳入收集并重新确认代理能力与边界。

点 Bake，检查生成导航面是否覆盖 A—D 相关地面、门洞是否连通、屏风是否把导航面分开。导航面从障碍边缘留出体型余量是正常现象。收集范围、Layer 和几何选项见 [NavMeshSurface 官方说明](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html)。

用物理几何烘焙可使这一轮导航更接近简化碰撞，但仍不能保证与实际角色移动逐帧一致。若门洞断开，依次检查净宽、Collider、Agent Type、收集范围和烘焙精度，不要先把代理半径缩到与角色无关的数值。

### 添加单独的导航探针，确认路径与抵达

创建 `NavProbe`，放在 A 点的已烘焙地面上，添加 NavMeshAgent；Agent Type 与 Surface 相同，Radius=0.3、Height=1.8，Speed=2，Stopping Distance=0.15。可添加相同的可视胶囊，但不要添加 CharacterController、Rigidbody 或多余 Collider。

关闭 WalkProbe，再启用 NavProbe，避免两个探针重叠干扰观察。在 `CourtyardNavProbe.cs` 中加入最小目标请求：

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class CourtyardNavProbe : MonoBehaviour
{
    public Transform goal;

    IEnumerator Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        if (goal == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("Assign a goal and place the probe on its NavMesh.");
            yield break;
        }
        if (!agent.SetDestination(goal.position))
        {
            Debug.LogWarning("Destination request was rejected.");
            yield break;
        }
        yield return null;
        while (agent.pathPending)
            yield return null;

        Debug.Log($"Path status: {agent.pathStatus}");
    }
}
```

将 C 的 Transform 拖到 goal，先检查它是否能绕过屏风抵达右后侧；停止后将起点改到 C、goal 改为 D，再检查目标前的停留区。这两段检查对应我们希望观察的右侧路径。

另做一次 A→D 的直接请求，记录实际选择哪一侧。**路线图不会约束寻路算法按 A→B→C→D 经过每个点。** 直接请求可能走左侧；想强制经过检查点，需要额外的路径点驱动或玩法约束，本篇不将视觉提示写成导航规则。

`SetDestination` 返回成功只表示接受请求，路径计算可能延后；还要等 pathPending 结束，再检查 pathStatus，见 [Unity SetDestination](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent.SetDestination.html)。日志中路径完整也不等于运行中已经抵达：继续观察移动、停滞、终点位置和 remainingDistance，确认没有一直被挡住。

这个脚本同样待 Unity 编译和运行。它不实现连续点击、自动巡逻、失败重试或动态障碍处理。

### 改了摆放，导航数据不会凭空变新

编辑模式移动屏风或堵住门洞后，重新烘焙，再重复相同请求。如果运行中要移动箱子或打开门，需要项目采用的 NavMeshObstacle、Carving、Link 或运行时更新方案；仅给模型加 Collider，不足以说明导航会及时绕行。

也不能因为 NavProbe 走过去，就省略 WalkProbe 的检查。两者的位移驱动和约束不同，前者验证导航路径，后者验证这一套角色身体移动。

## 最后把观察位置带进去：第三人称镜头走查

固定相机适合检查路线全貌，第三人称镜头更容易暴露门洞、屏风和墙角的观察问题。本例使用 Cinemachine 3.1 的 Third Person Follow，不混用旧版 Virtual Camera 的字段名称。

### 建立跟随目标与镜头

通过 Package Manager 安装项目兼容的 Cinemachine 3.1。给 Main Camera 添加 CinemachineBrain，创建一个 Cinemachine Camera 作为 CourtyardCamera，确认它是当前生效的镜头，关闭其他竞争镜头。

在 WalkProbe 下建 `CameraTarget`，局部位置 `(0, 1.5, 0)`，起始局部旋转 `(10, 0, 0)`。CourtyardCamera 的 Tracking Target 指向它，Position Control 选择 Third Person Follow，Lens 的 Vertical FOV 先取 60°。

Third Person Follow 按目标位置和朝向建立偏肩观察。本文先用它自身的朝向规则，不额外添加指向庭院目标的 Rotation Composer，避免让镜头始终盯着施法目标而掩盖路线观察问题。Tracking Target 和位置控制的关系见 [Cinemachine Camera](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html)。

起始参数可取：

<div role="region" aria-label="第三人称镜头练习参数，可横向查看" tabindex="0" style="overflow-x:auto">

| 参数 | 本例起值 | 观察重点 |
| --- | --- | --- |
| Shoulder Offset | (0.4, 0.2, 0) 米 | 右肩偏移是否让门柱过早挡住画面 |
| Vertical Arm Length | 0.3 米 | 转俯仰时镜头高度与目标构图 |
| Camera Side | 1 | 右肩起步，再比较左肩 |
| Camera Distance | 3 米 | 狭窄处收近是否过于频繁 |
| Damping | 各轴先取 0.2 | 起步、转弯与停止时的滞后 |
| Camera Collision Filter | Environment | 墙、地面、横梁和地形能否参与避障 |
| Camera Radius | 0.2 米 | 贴墙时镜头中心是否留有余量 |

</div>

Third Person Follow 自带碰撞调整，相关 Layer、Ignore Tag 与半径定义见 [官方组件说明](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html)。本例先通过 Layer 排除 Player，保留环境碰撞；若使用 Ignore Tag，只忽略明确的玩家标签，不要把它设成大量环境物体共用的 Untagged。

参数是观察起点，不能据此保证任何门洞都不会穿帮。Near Clip Plane、镜头半径、装饰物碰撞和画面构图还需要一起核对。

### 这个探针怎样转镜头，怎样检查

重新启用 WalkProbe、关闭 NavProbe。在本例脚本中，移动方向会转动玩家根节点，CameraTarget 随之转动；WASD 仍按世界轴移动，**这不是正式的镜头相对第三人称操作**。

需要原地检查朝向时，在 Play 模式暂停，调整 CameraTarget 的局部 Y 旋转和 X 俯仰，再继续观察。这个步骤用于覆盖观察角度，不代表已经实现鼠标或手柄环绕；正式输入交给项目已有镜头控制器。

沿 A→B→C→D 检查：

- 进入门洞时，门柱、横梁是否让镜头突然挤近或遮住角色？
- 在 B 转向绕行时，屏风边缘是否导致明显跳变，目标是否提早暴露？
- 从 C 看目标时，摄像机是否被背后的墙阻挡，目标是否落在画面边缘？
- 在 D 背靠目标、贴墙或回头看入口时，镜头收近后还能理解空间吗？
- 改成另一侧肩位和不同俯仰，再做同一轮；必要时检查不同宽高比。

碰撞调整可以改变镜头位置，但不保证画面始终舒适。若它不断在墙角来回变化，先确认碰撞配置，再评估走廊宽度、装饰位置、跟随距离和镜头规则，不能只反复增大阻尼。

**运行镜头避障与渲染的 Occlusion Culling 解决不同问题。** 后者决定是否绘制被遮挡的物体，不能替代镜头与墙的碰撞，也不能代替玩家是否看得懂路线的检查。

## 用同一条路径，把美术调整和运行问题接起来

地编走查要能复现。记录场景版本、玩家体型、移动方式、镜头设置、分辨率、宽高比、灯光与质量档，再按固定顺序检查，比“随便飞一圈感觉还行”更容易发现修改影响。

### 把检查写成观察记录，不提前填通过

<div role="region" aria-label="庭院运行走查记录模板，可横向查看" tabindex="0" style="overflow-x:auto">

| 项目 | 操作与观察条件 | 需要记录的结果 |
| --- | --- | --- |
| 入口 | WalkProbe 从 A 穿门，正向及斜向贴门柱 | 是否卡住、下落、碰横梁；发生坐标 |
| 绕行 | A→B→C，比较左右路线 | 提示是否清楚，转弯和屏风接缝表现 |
| 视线 | 固定镜头参数，在 A、C、D 观察目标 | 遮挡、显露时点、背景对比与屏幕位置 |
| 导航 | A→C、C→D，再直接 A→D | 请求、路径状态、实际路径、停滞与抵达 |
| 镜头 | 门洞、屏风端点、贴墙、倒退及原地转向 | 收近、穿插、突变、角色和目标是否可读 |
| 地形 | 检查入口与墙脚，向外围观察 | 穿地、浮空、坡度、地形碰撞与表面比例 |
| 光照 | 同一组点比较实时/烘焙版本 | 暗部、接缝、角色明暗变化、数据是否更新 |
| Player | 同一输入与场景在构建中重走 | 与编辑器差异、日志及设备环境 |

</div>

这些格子现在都是待执行项目。实际记录可以写“B 点斜向贴墙时卡住，截图/录像编号，关闭某个重复 Collider 后恢复，再重走 A—D”；不能只写一个“已验收”，也不要在没有执行时填入示例结果。

首次 Play 前先清编译与资源错误；发生问题时暂停并定位模型、Collider、NavMesh 或相机状态。一次只改一类原因，改完重走相关路径，再补全程回归。

### 增加细节时，让空间关系继续成立

当灰盒可走，再逐步替换正式模块、补地面变化、装饰和植被。每一轮增加后都重新检查：

- 一块摆在路边的石头，是否缩小了身体和镜头共用的空间？
- 前景植物是否遮住了目标提示，且碰撞形状与画面不一致？
- 重复墙面靠贴花打破后，有没有明显漂浮或共面闪烁？
- 新增高光、雾或灯光是否把视线重点从目标拉走？
- 更新屏风后，导航、光照和路线标记是否还对应当前布局？

远景和近景可以采用不同细节密度，但不能仅凭离摄像机远就断言“没有成本”。是否增加 LOD、遮挡剔除、合批或拆分加载，应在实际场景与设备测量后决定。

编辑器中一次流畅走查也不能替代目标设备性能结论。进入 Development Build 后，再用同一路径配合 Profiler、Frame Debugger 等工具记录 CPU/GPU、内存与加载；本篇不预填帧率或性能提升数字。

## 保存与交付时，场景文件只占其中一部分

本例的变化主要发生在 Unity 工程内。交付要让另一位协作者打开工程后重现空间，而不仅是收到一张庭院截图。

### 保存布局和它依赖的数据

建议将以下内容纳入本例的交接记录，路径为教学组织方案：

```text
Assets/
  Art/Courtyard/
    Models/ Textures/ Materials/ Prefabs/
  Scenes/ArtProduction/Courtyard/
    CourtyardWalkthrough.unity
    Terrain/              // TerrainData、Terrain Layer 与材质引用
    Lighting/             // Lighting Settings；采用烘焙时的生成数据
    Navigation/           // 核对 Surface 实际生成/引用的数据路径
    courtyard-walkthrough.md
  Scripts/ArtProduction/
    CourtyardWalkProbe.cs
    CourtyardNavProbe.cs
Packages/
  manifest.json
  packages-lock.json
```

保存 Scene、修改过的 Prefab 和 Material，检查 TerrainData、Terrain Layer、NavMeshData、Lighting Data Asset、光照贴图及探针相关数据的实际引用。生成数据的位置由当前工作流决定，上表不要求随意移动已生成文件。

使用 Unity 的 Project 窗口整理资产，并保留相应 `.meta`；只复制 `.unity` 而漏掉它引用的 GUID 对应资产，无法完整恢复场景。包版本和必要 ProjectSettings 也应随工程约定一起交接。

Play 模式里的临时调整通常不会在退出后自动成为编辑状态的改动。记录有效参数，停止运行后按项目方式回写 Scene 或 Prefab，再重开场景检查，不要靠一段只存在于当前运行实例中的状态交付。

### 做一次变更回流：把屏风向入口移动

假设希望更早遮挡目标，将屏风从 Z=2 改为 Z=1.5：

1. 在布局原稿中修改实例位置，不需要为同一块直墙重新导出 FBX。
2. 重新检查门内转弯空间和 A/B 的实际视线；更早挡住目标，也可能更早挤压身体和摄像机。
3. 更新 NavMesh，再做相同寻路请求，确认路线变化。
4. 若采用烘焙光照，重算受影响数据；比较门洞和墙脚照明。
5. 用正式角色与实际镜头重走，更新记录和版本。

如果问题来自墙本身太厚，就回第 06 篇的 Maya / Max 原稿修改，重新导出、检查 Unity 导入和 Prefab，再复查所有使用该构件的实例。**摆放变更与资产源文件变更的回流范围不同。**

### 场景划分和加载边界要有实际理由

本篇先用单一 Scene 保存练习，光照、地形与导航仍分别说明依赖。将来改成 Additive Scene、分区加载或资源包，不能只按 Hierarchy 分组就认为已经完成拆分。

要明确角色跨区时哪些碰撞、导航、照明、镜头和目标仍需存在，卸载后谁持有引用，重进时怎样恢复。正式接入与生命周期归[第 10 篇客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})，综合示例在第 11 篇汇合。

## 本篇交付到哪里

到这里，方案已经从模块摆放扩展到一套可执行的空间检查顺序：先以灰盒确认庭院尺度和路线，再组织表面、外围地形与光照，最后分别让身体、寻路对象和镜头进入同一个空间。

尚缺的证据是实际 Unity 场景、素材与参数截图、脚本编译、走查录像、导航/光照生成数据和设备记录。原理与操作说明已经展开，这些验证不能由 Hugo 构建或网页截图代替。

<!-- DATA-TODO: 制作 CourtyardWalkthrough.unity 与引用资产，记录 Unity/URP/Input System/AI Navigation/Cinemachine 实际版本；补灰盒与正式资产的同视角对照、地形/光照面板和 A/B/C/D 截图。 -->
<!-- DATA-TODO: 编译并运行两段教学探针；记录控制器、Agent Type、NavMeshSurface 几何与 Layer 设置，补手动通行、A→C/C→D/A→D 导航、镜头贴墙/穿门/俯仰录像，以及屏风变更后的复查。 -->
<!-- DATA-TODO: 若采用烘焙，补光照 UV、探针、Lighting/NavMesh 生成数据与依赖记录；Player 和目标设备实验另记环境、观察及性能结果，不能以编辑器运行代替。 -->

**地编的完成，要由玩家沿路径走过、看懂并能稳定运行的空间来回答。**

下一篇进入[第 08 篇特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})，把角色动作与状态转成可读的动态反馈。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
