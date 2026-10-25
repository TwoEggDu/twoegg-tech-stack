---
title: "08｜特效：怎样把动作与状态做成可读的反馈"
slug: "game-art-production-08-vfx"
date: "2026-10-25"
lastmod: "2026-10-25"
description: "从 Photoshop 柔光与圆环素材，到 Unity 粒子、材质和 Prefab，再把蓄光、释放与目标反馈接到挂点、动画时点、取消和结束。"
draft: false
featured: false
content_stage: "article"
weight: 88
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 8
type: "reading-series"
reading_title: "特效：怎样把动作与状态做成可读的反馈"
reading_summary: "制作两张 Photoshop RGBA 纹理，在 Unity 组装三段施法效果，讲清挂点、空间、动画同步、取消、自然结束与运行走查。"
---

前文已经说明施法动作怎样制作，以及庭院中在哪里停留和观察。接下来要回答：玩家怎样知道角色正在蓄力、已经释放，以及目标发生了什么变化？

如果三个阶段都用同样亮、同样大的光团，画面可能很热闹，信息却没有层次。如果杖尖的光跟不上动作，或者角色受击后还在继续蓄光，问题又会从单个效果扩展到挂点、时间和生命周期。

**一段可用的特效，要同时交代它传达什么、在什么位置出现，以及何时开始和结束。** 本篇沿用研究者在庭院施法的例子，把素材制作、引擎组装和运行接入串起来。

> **读这篇之前**：先看[第 05 篇动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})的施法时点与受击取消，以及[第 07 篇地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})的庭院和镜头条件。挂点与角色层级可回看[第 04 篇绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}})。

> **版本与证据范围**：操作方案参考 Photoshop 桌面版帮助、Unity 6（6000.0）与 URP 17 文档；主路径使用 Unity 内置 Particle System。素材尺寸、参数、时间线与代码为教学选择。尚未制作实际 PSD、PNG、Prefab 或 Unity 工程，也未执行脚本编译、动画事件、效果播放和设备测试；图为原创原理示意。下面的步骤与观察条件均待实操。

## 先确定效果在说什么，再决定用什么形状

同一团光，放在不同阶段会产生不同理解。角色手中逐渐聚起的亮点，可以表示准备；短促向外的闪光，可以强调释放；目标上的圆环和碎光，可以表示反馈出现。

这些关系要与玩法约定一致：是施法提示、范围预警、命中确认、持续状态，还是只负责氛围？玩家会依据画面行动，因此不能把装饰范围、视觉持续时间和实际伤害规则混在一起。

### 把一次施法拆成三个职责

本例先做没有飞行弹道的短距离表现预览：

<div role="region" aria-label="三段效果的信息与位置，可横向查看" tabindex="0" style="overflow-x:auto">

| 段落 | 想传达的信息 | 放在哪里 | 本例的表现 |
| --- | --- | --- | --- |
| 蓄光 Charge | 正在准备释放 | 杖尖挂点，随角色移动 | 小柔光从弱到强，再消退 |
| 释放 Release | 释放时点已经到达 | 释放瞬间的杖尖位置 | 短促向外的碎光 |
| 目标反馈 Impact | 目标处出现反馈 | 预览目标前的指定位置 | 扩散圆环与少量碎光 |

</div>

本例在预览释放时同时触发目标反馈，用来检查素材、时序和空间，**不代表代码已经检测到命中或结算伤害**。正式接入时，施法释放和命中反馈可以是两个事件：投射物到达、碰撞或服务器确认后，才按玩法规则决定目标反馈。

若需要飞行过程，就要增加弹道对象、轨迹与抵达规则，不能把这三段预览称为完整投射物系统。

### 主体、辅助和尾部要有强弱关系

先选出最重要的瞬间，再配置亮度、大小与持续时间。本例让杖尖释放短而集中，目标圆环负责位置识别，碎光只补动势。若碎光遮住圆环和目标轮廓，应该先减弱辅助层。

颜色也承担区分作用，但不能只依赖颜色：形状、方向和节奏可以帮助识别。效果还要在庭院的亮处、暗处、不同背景和镜头距离下观察，不以黑色预览背景上的“够亮”作为唯一条件。

## 一段效果至少包含素材、组装和实例三层

Photoshop 画出的柔光图片，只定义二维形状和透明度。Unity 材质决定怎样取样、着色和混合；粒子系统决定怎样生成、运动和消失；Prefab 保存这些引用与配置；运行实例再决定放在哪里、何时播放和由谁清理。

同一张圆点 PNG 可以用于蓄光、碎光或拖尾，它本身并没有“施法”的语义。

### 时间、空间和结束条件要单独约定

- **时间**：什么时候发射，单粒子活多久，整组何时不再产生内容？
- **空间**：出生点在哪里，已经产生的粒子是否继续跟随挂点？
- **结束**：正常播完、停止产生、立即清空，还是对象被卸载？
- **所有权**：谁创建实例，谁负责取消与销毁，谁防止旧请求影响新动作？

动画切换不会自动回答后面三项。一个特效即使已经脱离角色放在世界中，也仍然需要明确的拥有者处理结束。

{{< art-vfx-flow >}}

### 粒子、网格、拖尾和序列帧各自适合什么

粒子适合分散的小块运动与大量重复元素；网格可表达稳定的环、扇面或有厚度的形体；拖尾沿运动留下连续形状；序列帧用多张连续图像表达单张贴图难以描述的变化。

它们可以组合，但不需要为了“像特效”全部使用。本例用两张纹理和四个 Particle System 先讲清主链路。地面范围环若要求贴合平面，应考虑平放网格、对应投影方案或合适的粒子朝向，不能直接把始终面向摄像机的圆环称为地面范围指示器。

## 在 Photoshop 制作柔光圆点与圆环

先做两张能看清引用关系的简单素材，不依赖复杂笔刷或购买素材。它们是教学构成单元，不是已经完成风格设计的商业效果。

### 建立透明画布并保留原稿

各建一份 512×512 像素、RGB、8 bit/channel 的文档，背景为透明，采用 sRGB 工作色彩条件。保存为：

```text
ArtSource/Effects/CourtyardCast/
  cast_soft_v001.psd
  cast_ring_v001.psd
ArtExport/Effects/CourtyardCast/v001/
  cast_soft_rgba.png
  cast_ring_rgba.png
```

PSD 中保留 `source_shape`、`export_shape` 和可选的 `preview_background`。背景层只用于检查不同底色，导出前隐藏。不要把 Photoshop 的棋盘格截图当成透明图：棋盘格只是编辑器的透明区域显示。

### 柔光圆点：先画轮廓，再柔化边缘

在 soft 文档中：

1. 使用 Ellipse Tool，Mode 选 Shape，Fill 设白色，Stroke 设无；建立直径约 280 像素、居中的圆。
2. 保留原形状层，复制一份作为 export_shape，将副本栅格化后处理；也可以使用 Smart Object 保留可调滤镜。
3. 在副本上执行 `Filter > Blur > Gaussian Blur`，半径先试 32 像素，观察中心与边缘。参数只是起点，边缘在画布边界前应已足够透明，避免裁出方框。
4. 隐藏未模糊的 source_shape，分别在深、浅背景上看轮廓与半透明边缘；导出时只显示需要的柔光层。

Ellipse 的填充与尺寸入口见 [Adobe Ellipse Tool](https://helpx.adobe.com/photoshop/using/tool-techniques/ellipse-tool.html)，模糊工具与保留可编辑副本的方法见 [Adobe Blur effects](https://helpx.adobe.com/photoshop/desktop/effects-filters/blur-sharpen-filters/overview-of-adding-blur-to-images.html)。

这张图用白色 RGB 和逐渐变小的 Alpha 表达柔光，颜色留给引擎调节。**在不透明黑底上画一个白圆，并不会自动得到正确的透明圆点**；黑色与 Alpha 为零是两回事。

### 圆环：空心区域也必须透明

在 ring 文档中，仍用 Ellipse Tool 建一个居中的圆，形状宽高约 320 像素，Fill 设无，Stroke 设白色、宽约 24 像素。保留形状层，必要时在副本上试少量模糊，先保持环的边缘容易辨认。

检查环中心和外部均透明，不能把中心填黑再期待所有混合模式都将它“挖掉”。软圆点和圆环分成两份原稿，便于分别修改边缘与环宽；以后打图集时再决定排布。

### 导出 PNG，重新打开检查透明度

用 Export As 或当前桌面版对应的 PNG 导出入口，保留 Transparency，输出 512×512 的 RGBA PNG，不带预览背景。保存 PSD，再重新打开导出的 PNG，核对尺寸、Alpha 与边缘。PNG 透明能力和保存入口见 [Adobe PNG 保存说明](https://helpx.adobe.com/photoshop/desktop/save-and-export/save-files/save-in-png-and-gif-formats.html)。

若在浅底出现黑边，先检查源图、透明区域邻近 RGB、导出与混合方式；若只在 Unity 中出现，再比较导入压缩、采样和材质设置。不要用一味增加亮度掩盖边缘问题。

独立导出的灰度遮罩是另一条用法：它可能把数据放在 R 通道，由 Shader 明确读取。本文两张 RGBA 图直接使用自身 Alpha，没有额外打包一张遮罩；纯数据纹理是否关闭 sRGB，应按取样语义决定，不能把所有特效纹理都当作颜色图。

## 导入 Unity，先让材质正确显示一张图

将两张 PNG 放到：

```text
Assets/Art/Effects/CourtyardCast/
  Textures/
  Materials/
  Prefabs/
```

PSD 留在制作源目录；Unity 本轮使用明确导出的 PNG。不要只复制 Prefab 而漏掉它引用的纹理和材质。

### 先固定纹理导入条件

两张图的 Texture Type 选 Default，Texture Shape 为 2D，Max Size 先取 512；Alpha Source 使用输入 Alpha，Wrap Mode 取 Clamp，Filter Mode 先用 Bilinear，首轮关闭压缩以排除压缩边缘的干扰，Apply 后分别看 RGB 与 Alpha。

它们作为带颜色的 RGBA 图使用，本例保持 sRGB 开启；白色素材经粒子颜色调色。Alpha Is Transparency 可用于处理透明边缘邻近颜色，但不能替代缺失的 Alpha。导入属性含义见 [Unity Default texture](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-type-default.html)。

Mipmap 按实际距离与缩小表现选择；先保留工程中明确记录的设置，再比较远近闪烁和边缘，不将“关闭 Mipmap”写成通用性能建议。平台压缩也在显示正确后另做对照。

### 建立两份粒子材质，分清两个混合设置

创建 `M_CastSoft.mat` 与 `M_CastRing.mat`，Shader 选 `Universal Render Pipeline/Particles/Unlit`：

- Surface Type=Transparent。
- Blending Mode=Alpha。
- Color Mode=Multiply。
- Base Map 分别引用 soft/ring PNG，材质颜色先为白色，Alpha 为 1。
- Alpha Clipping 先关闭；Soft Particles、Camera Fading、Distortion 等附加功能先关闭。

**Blending Mode** 处理与背景的合成，**Color Mode** 处理材质颜色与粒子颜色的组合，它们不是同一个选项。本文用 Alpha 混合保留透明轮廓，再用白色素材乘粒子颜色调色。字段与模式见 [URP Particles Unlit](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/particles-unlit-shader.html)。

以后想得到更亮的叠加感，可以复制材质比较 Additive，但要在浅背景和多层叠加时检查。加法不自动等于“更高级”，也不能用它的视觉结果反推透明图一定正确。

Unlit 让本轮颜色不依赖普通受光计算，不代表它会照亮墙面，也不保证越过场景深度显示。若需要柔化与几何相交的硬边，再按 URP 配置确认 Soft Particles 的深度依赖；首轮先把基本显示与位置关系弄清楚。

## 用 Particle System 组装三段效果

从 `GameObject > Effects > Particle System` 开始，在 Prefab 编辑模式中制作。每个 Prefab 的根节点就是一个 Particle System，变换位置/旋转为 0、缩放为 1；目标反馈另有一个碎光子系统。

统一关闭 Looping、Prewarm、Play On Awake，Start Delay=0，Gravity Modifier=0，Stop Action=None，Delta Time 使用 Scaled。首轮 Culling Mode 取 Always Simulate，避免离屏暂停干扰结束观察；这不是所有正式效果都应采用的性能配置。

### 先理解 Duration、Lifetime 和 Burst

Duration 描述一轮系统时长，Start Lifetime 描述每个粒子从出生到死亡的时间。一个 Duration 为 0.1 秒的系统，在 0 秒发射寿命为 0.6 秒的粒子后，画面仍可能继续存在；不能用 Duration 单独推算回收时点。

Main 的时间、空间与剔除属性见 [Unity Main module](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysMainModule.html)。本例统一将 Emission 的 Rate over Time / Rate over Distance 设为 0，只保留一条 Time=0 的 Burst，Cycles=1、Probability=1，Count 见下表；发射配置见 [Emission module](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysEmissionModule.html)。

<div role="region" aria-label="三段效果的粒子起始参数，可横向查看" tabindex="0" style="overflow-x:auto">

| Prefab / 系统 | Duration / Lifetime（秒） | Burst Count | Speed（米/秒） / Size（米） | Simulation Space | 材质 / Max Particles |
| --- | --- | --- | --- | --- | --- |
| VFX_Charge 根 | 0.45 / 0.45 | 1 | 0 / 0.3 | Local | Soft / 4 |
| VFX_Release 根 | 0.1 / 0.35 | 12 | 1.5 / 0.06 | World | Soft / 32 |
| VFX_Impact 根圆环 | 0.1 / 0.6 | 1 | 0 / 0.8 | World | Ring / 4 |
| VFX_Impact 子碎光 | 0.1 / 0.35 | 8 | 1.2 / 0.05 | World | Soft / 16 |

</div>

表中 Speed 是 Start Speed，Size 是 Start Size，在本例单位缩放下作为米制参照。所有 Renderer 先用 Billboard、Render Alignment=View，指定对应材质；不需要的模块先关闭。Billboard 的朝向与材质入口见 [Renderer module](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysRendererModule.html)。

### 蓄光：一个粒子也能表达准备阶段

Charge 关闭 Shape，Start Color 先取淡青色、Alpha=1。开启 Size over Lifetime，使用单一曲线，在归一化生命 0、0.7、1 处取约 0.2、1、0.6。

开启 Color over Lifetime，RGB 保持白色，Alpha 在生命 0、0.15、0.8、1 处取 0、0.7、0.5、0，使它淡入再淡出。曲线横轴对应单粒子生命进度，不能直接当成整段施法的秒数。

这里 Start Size=0.3 再乘大小曲线，粒子并不是从 0.2 米长到 1 米。颜色变化也应结合起始颜色与材质看最终结果。曲线入口见 [Size over Lifetime](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysSizeOverLifeModule.html)，颜色与 Alpha 渐变见 [Color over Lifetime](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysColorOverLifeModule.html)。

保存为 `VFX_Charge.prefab`。它的 Local 模拟配合挂接，让已经出生的蓄光继续随杖尖走；这项选择要在角色转身与移动时观察。

### 释放：短促向外的碎光

Release 开启 Shape，选 Sphere，Radius 先取 0.03 米，以球形分布产生向外运动。保持表中的速度、寿命和数量，Start Color 用较亮的青白色。

Size over Lifetime 从 1 降到 0；Color over Lifetime 的 RGB 白色，Alpha 从 0.8 降到 0。若曲线插值带来意外回弹，先检查曲线形状再调参数。Shape 决定出生区域和方向，相关选项见 [Shape module](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysShapeModule.html)。

保存为 `VFX_Release.prefab`。它只是释放点附近的碎光，不会自动从法杖飞到目标。World 模拟让已出生的粒子不随发射器后续平移；运行端还会将整个实例独立放在释放瞬间的位置。

### 目标反馈：圆环负责主体，碎光负责辅助

Impact 根系统关闭 Shape，使用 ring 材质，Start Color 先取暖色；Size over Lifetime 从 0.3 增到 1.5，即在 Start Size=0.8 的基础上扩散。Alpha 从 0.8 衰减到 0。

添加名为 `ImpactSparks` 的 Particle System 子物体，局部位置/旋转为 0、缩放为 1，使用表中的子碎光参数；Shape 选 Sphere、Radius=0.05，颜色与根圆环协调，大小与 Alpha 随生命降到 0。

保存为 `VFX_Impact.prefab`，确保子系统也关闭 Looping / Play On Awake，Stop Action=None。本例整个圆环面向摄像机，作为目标反馈符号；它没有贴在模型曲面上，也不是贴地 Decal。

### 单独预览后，再检查组合

在 Prefab 或场景中逐个使用粒子预览控制，观察形状、发射次数、颜色、大小变化和最后一粒消失的时间。再检查 Impact 根与子系统是否一起播放、是否有默认连续发射残留。

保存后重新打开 Prefab 核对引用。编辑器粒子预览能检查局部配置，不能替代后续游戏输入、动画取消、角色失活和资源卸载的检查。

## 把效果放进庭院，明确挂点与世界位置

将第 07 篇场景另存为 `CourtyardVfxTest.unity`。先保留原来的空间走查场景，避免把布局变更与效果接入混在一起。

### 没有完整角色时，使用明确的预览锚点

本系列尚无完整角色和法杖工程，可以先创建：

```text
VfxPreviewActor                // 世界位置 (4,0,4)，缩放 1
  vfx_socket_staff_tip         // 局部位置 (0,1.4,0.25)
PreviewImpactAnchor            // 世界位置 (4,1.1,4.65)
VfxPreviewDriver               // 初始独立预览；动画接入时见后文
```

目标仍用第 07 篇的占位立方体，中心 `(4,0.75,5)`、尺寸 `(0.6,1.5,0.6)`。ImpactAnchor 在前表面外侧留约 0.05 米，避免把圆环中心直接埋进不透明模型；实际反馈位置和朝向仍按目标表面与镜头需求调整。

这是静止锚点预览，不代表角色持杖效果已经完成。之后将 `vfx_socket_staff_tip` 放到法杖模型尖端，随装备和骨架走。第 04 篇的 `weapon_socket_r` 负责武器挂接，和法杖尖端的效果锚点用途不同。

### 挂点跟随与粒子空间是两个维度

Charge 实例成为 staff_tip 子物体，局部位置/旋转归零，使用 Local 模拟。角色移动时，发射器和已出生的粒子共同移动。

Release 实例在释放时读取挂点位置/旋转，放到世界根下，使用 World 模拟；Impact 实例读取目标反馈锚点，独立留在该世界位置。目标随后移动，本例的反馈不继续跟随；需要持续附着时应使用另一套明确的挂接与结束约定。

即使粒子设为 World，如果发射器仍跟着角色移动，它新发出的粒子还是会从移动后的发射点出生。反过来，独立的世界对象也可以使用 Local 模拟，只要发射器不动，视觉未必立刻显出区别。要分别记录“实例父节点”与“粒子模拟空间”。

本例所有相关父节点保持单位缩放，先避免骨骼或装备缩放改变效果尺寸。正式工程要核对 Scaling Mode、装备层级与非均匀缩放；不能仅靠把 Prefab 根缩放设为 1 就保证世界尺寸不变。

## 用一个小预览驱动，检查播放与结束

这一段只管理本例三个粒子 Prefab，不承担伤害、网络、正式对象池或完整技能状态机。先用计时预览把链路跑通，再换到动作释放标记。

### 预览时间线要与动画时间分开标记

独立预览按以下安排：

- t=0：创建 Charge 并跟随挂点。
- t=0.5 秒：停止并清理剩余 Charge，创建 Release 与 Impact。
- 后续：停止继续产生内容的各组粒子自然消失，拥有者确认整组结束后销毁实例。

Charge 的名义粒子寿命为 0.45 秒，因此默认会在释放前稍早淡出；这段间隔可以在观察后调整。Impact 从释放时刻开始，主圆环寿命 0.6 秒，所以整套表现不能按“系统 Duration=0.1”立即清掉。

这个 t 是**预览计时器**。第 05 篇 Cast 中的名义 0.5 秒释放标记属于动作片段，实际输入后何时到达它还受状态进入、过渡、速度与中断影响。

### 创建预览脚本，先确认能编译

在 `Assets/Scripts/ArtProduction/CourtyardVfxPreview.cs` 创建以下教学代码，挂到 VfxPreviewDriver。通过 Inspector 引用三个 Prefab 的根 Particle System、socket 与 impactAnchor；保持 usePreviewClock 开启，Animator 可空。

输入沿用第 07 篇的 Input System，Game 窗口中按 Space 播放、Escape 清空取消、F 停止发射后等待自然结束。**代码尚未在 Unity 编译或运行，实际工程应先确认 Console 无编译错误，再执行下面的检查。**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CourtyardVfxPreview : MonoBehaviour
{
    public ParticleSystem chargePrefab, releasePrefab, impactPrefab;
    public Transform socket, impactAnchor;
    public bool usePreviewClock = true;
    public Animator animator;

    readonly List<ParticleSystem> live = new();
    ParticleSystem charge;
    Coroutine clock;
    bool pendingRelease;

    void Update()
    {
        var key = Keyboard.current;
        if (key == null) return;
        if (key.spaceKey.wasPressedThisFrame) BeginPreview();
        if (key.escapeKey.wasPressedThisFrame)
        {
            CancelPreview();
            if (!usePreviewClock && animator != null)
            {
                animator.ResetTrigger("Cast");
                animator.SetTrigger("Hit");
            }
        }
        if (key.fKey.wasPressedThisFrame) FinishPreview();
    }

    void BeginPreview()
    {
        if (socket == null || impactAnchor == null ||
            chargePrefab == null || releasePrefab == null ||
            impactPrefab == null) return;

        if (!usePreviewClock &&
            (animator == null || animator.IsInTransition(0) ||
             !animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")))
            return;

        CancelPreview();
        pendingRelease = true;
        charge = Spawn(chargePrefab, socket, true);
        if (usePreviewClock)
            clock = StartCoroutine(PreviewClock());
        else
        {
            animator.ResetTrigger("Cast");
            animator.SetTrigger("Cast");
        }
    }

    IEnumerator PreviewClock()
    {
        yield return new WaitForSeconds(0.5f);
        clock = null;
        OnPreviewRelease();
    }

    public void OnPreviewRelease()
    {
        if (!pendingRelease) return;
        if (socket == null || impactAnchor == null)
        {
            CancelPreview();
            return;
        }
        pendingRelease = false;
        if (charge != null)
        {
            charge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            live.Remove(charge);
            Destroy(charge.gameObject);
            charge = null;
        }
        Spawn(releasePrefab, socket, false);
        Spawn(impactPrefab, impactAnchor, false);
    }

    ParticleSystem Spawn(ParticleSystem prefab, Transform anchor, bool follow)
    {
        var fx = Instantiate(prefab, anchor.position, anchor.rotation);
        if (follow)
        {
            fx.transform.SetParent(anchor, false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
        }
        fx.transform.localScale = Vector3.one;
        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fx.Play(true);
        live.Add(fx);
        return fx;
    }

    void StopClock()
    {
        if (clock != null) StopCoroutine(clock);
        clock = null;
        pendingRelease = false;
    }

    void FinishPreview()
    {
        StopClock();
        foreach (var fx in live)
            if (fx != null)
                fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void CancelPreview()
    {
        StopClock();
        foreach (var fx in live)
            if (fx != null)
            {
                fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(fx.gameObject);
            }
        live.Clear();
        charge = null;
    }

    void LateUpdate()
    {
        for (int i = live.Count - 1; i >= 0; --i)
            if (live[i] == null || !live[i].IsAlive(true))
            {
                if (live[i] != null) Destroy(live[i].gameObject);
                live.RemoveAt(i);
            }
    }

    void OnDisable() => CancelPreview();
}
```

每次独立预览按 Space，会先取消上一轮拥有的实例，再重新开始，防止上一轮计时器稍后补发效果。结束检查用 `IsAlive(true)` 包含子粒子系统，避免 Impact 的主系统停了、子碎光还活着时提前销毁。

`Play(true)` 会带上子系统；停止发射和立即清空则用不同 Stop 行为。能力范围见 [ParticleSystem.Play](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.Play.html)、[Stop](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.Stop.html)与 [IsAlive](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.IsAlive.html)。

这份清理只覆盖本例层级内的 Particle System。如果以后加上灯光、音效、Tween、独立拖尾或其他异步任务，IsAlive 不能代表那些内容也已结束，要由组合效果的拥有者共同管理。

### 区分自然播完、停止发射和立即取消

自然播完时，Burst 产生的粒子随 Lifetime 和颜色曲线消失，再销毁实例。按 F 时，停止未来发射，并取消尚未发生的预览释放，但已经产生的粒子仍按剩余生命继续存在。

本例主要使用一次 Burst，所以发射后按 F 不会把那些粒子“收回去”，也不会自动给任意效果补一个淡出过程。柔和结束需要素材和曲线本来就有相应尾部，或者专门设计退出段。

按 Escape 则停止计时、清空所有本轮粒子并销毁实例，包括已经在世界中的 Release / Impact。这是**本预览选择的取消策略**。正式游戏中，已释放的投射物或命中反馈是否继续存在，应由玩法与表现约定决定，不能照搬全部清空。

## 再接到动画释放标记，检查受击与同步

有了第 05 篇的 Animator 和测试动作后，再打开第二条路径。局部手臂仍只适合检查挂点和动作时点，不代表全身角色、脚步与持杖表演已经完成。

### 关闭预览计时，只留一个释放来源

1. 将 CourtyardVfxPreview 移到实际使用动画的 Animator 所在 GameObject；停用旧的独立 Driver，避免两个输入接收者。
2. 引用该 Animator，以及法杖尖端的真实 socket；关闭 usePreviewClock。Controller 沿用第 05 篇的第 0 层 Idle / Cast / Hit 与同名 Trigger。
3. 在导入动作的 Cast 片段 Events 中，于约定释放标记添加事件，Function 填 `OnPreviewRelease`，Apply。
4. 进入 Play，先等到 Idle。Space 请求 Cast，事件到达时再创建释放和目标反馈；代码在过渡或非 Idle 时拒绝新请求，便于单次检查。
5. 释放前按 Escape 请求 Hit 并清理效果，再检查是否出现迟到的释放；释放后再取消，按本预览规则检查世界效果也被清理。

事件函数需要由使用该动画的 Animator 对象上的脚本接收，操作见 [Unity imported animation events](https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationEventsOnImportedClips.html)。只在子挂点或另一个空物体上放同名脚本，不会自动得到这个接收关系。

不要同时保留 0.5 秒计时器和动画事件来触发同一次释放。这里 pendingRelease 让重复回调在本轮内只触发一次；正式工程仍需要动作实例标识，检查旧事件、重入和复用对象，不能把这个布尔值当成完整的时序隔离。

### 同步以后，也要处理时长与取消

在动画模式下，Charge 的粒子寿命仍是固定的 0.45 秒，不会因为 Animator 放慢而自动延长。第一轮先固定动作 Speed=1，观察这套起值；改变动作速度后，应重新选择蓄光素材时长、粒子模拟速度，或按动作阶段控制发射与退出。

暂停、过渡、跳帧、循环、角色失活和没有到达释放标记，都要分别检查。如果事件没有到达，pendingRelease 不会自动判断原因；本预览可以 Escape 复位，正式接入则应在动作取消/退出等生命周期中确保清理。

本脚本只有按 Escape 的演示入口会联动 Hit。项目中的真实受击、死亡、切场景或装备移除，必须由相应系统通知效果拥有者，不能期待 Animator 状态变化自动调用 CancelPreview。

目标反馈位置也不应长期固定为一个测试 Transform。正式命中事件需要携带目标、接触点、法线或相应表现位置，再决定实例是否附着、是否跟随以及怎样结束；完整接入在[第 10 篇客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})汇合。

## 回到庭院镜头中，观察效果有没有把信息盖住

保持第 07 篇的观察点与相机条件，先检查静止效果，再移动角色和目标。对每轮测试记录素材/Prefab 版本、时间来源、镜头、背景、质量档与运行设备。

<div role="region" aria-label="特效运行走查记录，可横向查看" tabindex="0" style="overflow-x:auto">

| 项目 | 怎样操作 | 要记录什么 |
| --- | --- | --- |
| 素材与材质 | 深浅背景、近远镜头分别播放 | 方形边缘、黑边、环中心透明度和颜色 |
| 跟随 | 蓄光期间移动/旋转挂点 | 是否贴住杖尖，尺寸是否受父级缩放影响 |
| 世界位置 | 释放后继续移动角色与目标 | 碎光和圆环是否按约定留在出生处 |
| 时点 | 独立计时与动画事件分别测试 | 是否重复、延迟或在取消后补发 |
| 取消 | 释放前后按 Escape，停用 Driver | 蓄光、世界反馈和旧计时是否残留 |
| 自然结束 | 不取消等待结束，再按 F 对照 | 末粒子消失、子系统结束与实例清理 |
| 重复请求 | 计时模式连续 Space；动画模式忙碌时 Space | 重启与拒绝策略是否符合本文约定 |
| 可读性 | 从入口、侧面和目标前观察 | 角色、目标及路线是否被强光或圆环遮住 |
| 构建与叠加 | 在 Player 中固定路径、增加同时播放数量 | 编辑器差异与设备上的实际成本 |

</div>

这张表目前是待执行清单。页面上的参数表和原理图只能解释方案，不能填写“播放正常”“无残留”之类运行结论。

Particle System 的预览背景、庭院固定相机和第三人称镜头，分别会暴露不同问题。尤其要检查圆环在门柱前后、角色与目标相互遮挡时的效果，不要靠强制盖在所有不透明物体上来解决看不清的问题。

### 性能先看屏幕覆盖与叠加，再看具体瓶颈

粒子数量是一个输入，但不能单独代表效果成本。一张覆盖大部分屏幕的透明面片，可能在多个透明层之间反复绘制；细小碎光的数量、材质切换、模拟模块、灯光、贴图与屏幕分辨率也会改变结果。

透明排序还可能在不同摄像机角度下产生不同观感。先减掉不传达信息的层，再判断是否需要更复杂的排序、材质或形状方案。共享材质不意味着整组效果自动变成一次绘制。

使用固定场景、镜头路径和叠加数量，在目标设备的 Development Build 中配合 Profiler 与 Frame Debugger 分辨模拟、绘制和覆盖问题，再调整分辨率、材质、数量或质量档。本文没有实测设备、帧率或优化收益，不能从这几个小参数推出性能保证。

## 网格、拖尾、序列帧与 VFX Graph 放在哪一步扩展

当三段基础效果表达清楚后，再按缺少的信息选择工具：

- **网格**：需要稳定扇形、体积或地面环时，用 Maya / Max 制作简单网格和 UV，再按前面资产章节导入；也可以先用 Unity 基本体做形状验证。
- **拖尾**：需要表现杖端划动或物体轨迹时，再选择 TrailRenderer 或粒子 Trails。确认采样、朝向、宽度和尾部寿命，瞬移及对象复用前按方案清除旧轨迹。
- **序列帧**：需要烟雾、火焰等连续形态变化时，制作或导出多帧图集，再配置 Texture Sheet Animation 的行列和播放曲线。帧间边距、透明边缘与过滤应一起检查，入口见 [Texture Sheet Animation](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysTexSheetAnimModule.html)。
- **VFX Graph / Shader Graph**：当需求涉及更复杂的粒子组织、材质行为与工具参数时，再核对目标平台、渲染管线和实际包版本，决定是否采用。它们不会自动替代效果设计、挂点协议和清理责任。

本篇主路径没有制作网格特效、拖尾或序列帧，不将扩展入口算作已经完成的素材。TA 如何把复杂方案变成团队可用能力，放到下一篇。

## 交付与修改回流，不能只给一份效果 Prefab

一次可复现交付至少包含：

```text
ArtSource/Effects/CourtyardCast/       // PSD 与制作说明
ArtExport/Effects/CourtyardCast/v001/ // 两张 RGBA PNG
Assets/Art/Effects/CourtyardCast/
  Textures/                          // PNG 与导入设置
  Materials/                         // M_CastSoft、M_CastRing
  Prefabs/                           // VFX_Charge、Release、Impact
Assets/Scenes/ArtProduction/Courtyard/
  CourtyardVfxTest.unity
  courtyard-vfx-handoff.md
Assets/Scripts/ArtProduction/
  CourtyardVfxPreview.cs
```

另记录 Unity/URP/Input System 版本、必要的 ProjectSettings、纹理/材质/Prefab 的 `.meta`，以及采用动画路径时的动作导入事件与 Controller。场景和 Prefab 只是引用链的一部分。

交接说明要能回答：哪个挂点、前向和单位是什么，哪个系统发起释放，目标位置从哪里来，是否随目标移动，缩放允许怎样变化，取消前后保留哪些内容，谁判断结束并回收。更完整的接口约定可读[技能表现交付标准]({{< relref "system-design/skill-system-28-artist-vfx-delivery-standard.md" >}})。

### 做一次修改回流：目标反馈太抢眼

先确定问题是圆环太大、Alpha 太高、持续太长，还是碎光遮住目标：

1. 圆环宽度与透明形状有问题，回 PSD 修改，重新导出同一用途 PNG，再检查 Unity 导入与材质引用。
2. 大小、数量和生命节奏有问题，调整 Prefab 曲线与发射参数，不必修改 Photoshop 原稿。
3. 释放时点不对，回动画标记或表现时间轴；目标位置不对，检查接入数据和坐标转换。
4. 正常结束或取消后残留，检查拥有者、子系统、旧计时和退出入口，不靠把所有 Lifetime 改短来掩盖。
5. 在相同庭院、镜头与背景条件下重做播放、取消和叠加检查，保存参数及版本。

本文用 Instantiate / Destroy 让所有权容易看清，还没有实现对象池。改成池化以后，应在取出和归还时重置粒子、颜色/尺寸覆写、随机状态、父节点、延迟任务与外部引用，并保证每个实例只归还一次；不能把 Destroy 机械替换成 SetActive(false) 就视为完成复用。

## 本篇交付到哪里

现在方案已经从两张可编辑素材走到 Unity 材质、三段粒子 Prefab 和两条预览接入路径，并把空间、释放时点、自然结束与取消分别说明。

待补的是实际 PSD/PNG、Prefab/场景、脚本编译、动画与输入实操、正常/取消录像和目标设备记录。Hugo 或浏览器检查只验证文章与页面，不替代这些工程证据。

<!-- DATA-TODO: 制作 cast_soft_v001.psd / cast_ring_v001.psd 与对应 RGBA PNG，记录 Photoshop 版本，补透明度、导出与 Unity 导入/材质面板截图。 -->
<!-- DATA-TODO: 制作 VFX_Charge/Release/Impact 与 CourtyardVfxTest，编译 CourtyardVfxPreview；记录实际 Unity/URP/Input System 版本、粒子模块、挂点、动画事件和两条预览路径的播放/取消/结束录像。 -->
<!-- DATA-TODO: 固定设备、镜头、分辨率、质量档和同时播放数量，记录 Player 差异、屏幕覆盖与 CPU/GPU/内存观察；没有实测前不填性能收益。 -->

**特效的可用性，来自清楚的信息、正确的空间和有人负责的开始与结束。**

下一篇进入[第 09 篇 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})，讨论怎样把视觉方案、工具和技术规则变成团队可持续使用的能力。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
