---
title: "06｜场景资产：怎样制作可以组合的环境构件"
slug: "game-art-production-06-environment-assets"
date: "2026-10-25"
lastmod: "2026-10-25"
description: "用直墙、门框与地板，走过 Maya / 3ds Max 建模、枢轴、UV、Photoshop 平铺贴图、FBX 导出，再到 Unity 材质、Prefab 和拼装检查。"
draft: false
featured: false
content_stage: "article"
weight: 86
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 6
type: "reading-series"
reading_title: "场景资产：怎样制作可以组合的环境构件"
reading_summary: "制作墙、门框、地板的最小套件，说明 Maya / Max 的建模和 UV、Photoshop 贴图、FBX，以及 Unity 材质配置和坐标拼装测试。"
---

回到研究者进入的遗迹庭院。设计图上有连续石墙、一道入口和铺石地面，制作时却不一定把它们建成一个大模型：墙可能要延长，入口可能要移动，地面也可能沿另一条路线继续铺开。

如果每块构件单独看都漂亮，放到一起却高度不齐、石块大小不同、门洞被碰撞堵住，后续搭建仍会反复退回制作端。场景资产既要形成可信的物体，也要提供可以组合的边界。

**一组可复用的场景资产，需要让形状、拼接接口与表面规则一起成立。** 本篇用直墙、门框和地板走完制作与接入，再拼一个小场地检查这些接口。

> **读这篇之前**：先看[第 02 篇原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})的庭院拆分；不熟悉网格、UV、纹理与材质时，可补[第 03 篇角色资产]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})。本篇负责构件制作与试拼，路线、镜头、空间节奏和整体氛围归第 07 篇地编。

> **版本与证据范围**：操作方案参考 Maya 2024、3ds Max 2024、Photoshop 桌面版帮助，以及 Unity 6（6000.0）/ URP 17 文档。尺寸、目录、材质参数与拼装坐标为原创教学选择；配图为原理示意。尚未制作实际模型、UV、PSD、FBX 或 Unity 场景，也未执行软件、通行、光照烘焙或设备实验。下面的步骤与观察条件均待实操。

## 从一幅场景设计，拆出哪些值得复用的东西

场景设计中有建筑、地面、道具、植被和视觉重点，它们不需要采用同一种制作方式。

连续墙、重复柱段和铺地可以先找规则单元；破损雕像、独特入口或叙事道具，可以围绕视觉目标单独制作。植被又有自己的轮廓、透明边缘和风动需求，本例不把它塞进石墙工作流。

模块拆分还要结合使用方式：玩家能否绕到背面，墙顶会不会入镜，门是否要开合，破损是否影响碰撞？本例做的是**静态门框**，不含可开合门扇。以后加入门扇，需要另做转轴、碰撞变化与交互接入。

复用也有代价。只用一个重复墙段，制作和修改较集中，但大面积排列后容易显出规律；把每段都做成独特资产，又会增加制作、引用和维护成本。可以先用少量规则构件验证尺度，再按视觉需求加入转角、端头和装饰变化。

## 先把形状、拼接接口与表面分开

构件可以从三层理解：

- **形状**：墙的厚度、门洞轮廓、地板边缘，以及实际需要的可见面。
- **拼接接口**：尺寸、枢轴、朝向、端点和碰撞边界，让重复摆放有明确依据。
- **表面**：UV、纹理与材质，决定相邻构件的石材大小、方向、粗糙程度和变化方式。

接口正确，不代表画面已经完成；贴图精细，也不能补救拼接尺寸错误。先用简单灰色材质检查组合，再加入表面细节，更容易知道问题应该回到哪里修。

### 为最小套件固定一组尺寸

本例在 Unity 中按 1 单位对应 1 米组织。墙沿本地 +X 延伸，Y 是高度，Z 是厚度；地板沿 +X、+Z 铺开。墙和门框的厚度中心位于 Z=0，地板可行走表面位于 Y=0。

<div role="region" aria-label="场景构件尺寸与局部边界，可横向查看" tabindex="0" style="overflow-x:auto">

| 构件 | 名称 | 局部几何范围，单位米 | 枢轴约定 |
| --- | --- | --- | --- |
| 直墙 | `wall_straight_2m` | X：0—2；Y：0—3；Z：−0.1—0.1 | 墙起点、底面、厚度中心，即 (0,0,0) |
| 门框 | `gate_frame_4m` | 外边界 X：0—4；Y：0—3；厚度同墙 | 整个门框左侧起点、底面、厚度中心 |
| 地板 | `floor_2m` | X：0—2；Y：−0.2—0；Z：0—2 | 顶面的起始角，即 (0,0,0) |

</div>

门框由两个宽 1 米、高 3 米的立柱，加中间宽 2 米、高 0.5 米的横梁组成。横梁底面在 Y=2.5，因此净门洞是 **宽 2 米、高 2.5 米**。这是练习尺寸，不是角色通行的通用标准；摄像机、装备、动作和碰撞体仍会改变实际需求。

本例先用 1 米定位步长，90° 旋转，全体实例保持 Scale=(1,1,1)。0.2 米厚度不要求平移也按 0.2 米走：**构件厚度与摆放步长是两种约定**。

{{< art-environment-kit >}}

图中枢轴是摆放接口。墙的枢轴如果总在几何中心，按 2 米步长平移仍可形成直线，但替换门框、转向和端点对齐的规则会不同。本例选起点枢轴，是为了让下面的坐标表直接对应构件边界，不表示所有模块库都必须采用这一套。

## 在 Maya 中建模：先做能拼的体块，再处理细节

### 以厘米制作三种构件

新建场景，确认 Working Units 的 Linear 为厘米、上方向为 Y。将原稿保存为 `courtyard_modules_maya_v001.ma`。下面按厘米填写基本体尺寸，换算到前面的米制范围。

先用 **Create → Polygon Primitives → Cube** 建立直墙，在构造节点中设置 Width=200、Height=300、Depth=20，再将几何中心放到 (100,150,0)。这样墙的左端在 X=0，底部在 Y=0。

地板同样用 Cube：Width=200、Height=20、Depth=200，几何中心放到 (100,−10,100)。地板的厚度向下延伸，顶面才会与墙的底部共同位于 Y=0；若把板的中心放在 Y=0，走面会高出 10 厘米。

门框分别建立三个方盒：

<div role="region" aria-label="Maya 门框方盒尺寸和几何中心，可横向查看" tabindex="0" style="overflow-x:auto">

| 部件 | Width / Height / Depth，厘米 | 几何中心，Maya 世界坐标 |
| --- | --- | --- |
| 左立柱 | 100 / 300 / 20 | (50,150,0) |
| 右立柱 | 100 / 300 / 20 | (350,150,0) |
| 横梁 | 200 / 50 / 20 | (200,275,0) |

</div>

选中三个部件，用 **Mesh → Combine** 合成门框网格，命名为 `gate_frame_4m`。这里允许网格由几个不相连的壳组成；Combine 不会自动把相接的面焊接为连续拓扑。第一轮先检查尺寸、法线及重复面，再决定是否清理内部接触面或焊接。不要为了得到门洞，先做完整方盒后忘记删掉堵住入口的面。

三种构件是独立资产。若为了编辑方便在原稿中把它们排开，导出时仍要让各自输出版本回到约定原点；不能把原稿展台位置带到每个模型的局部坐标中。

### 将制作枢轴变成明确的输出原点

Maya 的对象变换、几何位置与编辑枢轴不是同一件事。对这个**没有绑定和动画的静态练习**，先保存原稿，再在导出副本中按需使用 **Edit → Delete by Type → History** 清理构造历史，以及 **Modify → Freeze Transformations** 固化当前静态变换。检查形状保持之后，在 Attribute Editor 的 transform 节点 Pivots 中，将 World Space 的 Rotate Pivot、Scale Pivot 设到 (0,0,0)。

Custom Pivot 模式也能移动和吸附枢轴；如果使用的是工具自定义枢轴，需按情况用 **Modify → Bake Pivot** 固化到对象，再复查。数值设置和 Bake Pivot 的职责见 [Maya 枢轴说明（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/Maya-Basics/files/GUID-150B390E-840B-4FE3-B8E9-8DEBCE7CEC97.htm)。

最终要检查两件事：对象的实际输出原点是否在接口上，以及几何相对该原点是否仍符合尺寸表。不要只因为移动工具的轴心显示在原点，就认定 FBX 一定会使用预期的局部坐标。导出后重开检查，Unity 中再以单位缩放和已知尺寸复核。

### 细节应当保留接口

先保留方正体块完成试拼。之后可以给外露边做少量倒角，让边缘更容易接住光；但要区分外露边与拼接边。把每个端头都削成圆角，重复排列后可能留下可见凹缝。

石墙的起伏、破损和装饰也要分清功能：影响轮廓或通行的部分需要几何；很浅的凹凸可以后续由法线表达；只改变颜色的痕迹不必都变成面。倒角与拓扑改动完成后，重新检查尺寸及 UV，不把“清理模型”当作不会改变下游的步骤。

## UV 与贴图密度：一张石材图覆盖多大的表面

第 03 篇法杖示例把各部分放进一张独特的 0—1 UV 图。本篇选择另一条路径：**同一张石材纹理在构件上平铺**，不同表面可以取样同一片纹理，UV 也可以超出 0—1。

本例约定一张 1024×1024 的纹理代表 2 米×2 米表面，因此名义密度为：

```text
1024 像素 ÷ 2 米 = 512 像素/米
2 米 × 3 米墙面 → U 跨度 1，V 跨度 1.5
2 米 × 2 米地板 → U、V 跨度均为 1
1 米 × 3 米立柱正面 → U 跨度 0.5，V 跨度 1.5
```

512 像素/米只是本例的设计密度。最终可见清晰度还受纹理导入尺寸、压缩、Mip、距离、视角与设备影响，不能把这个数直接当成画质验收结论。

如果把高 3 米的墙面强行压成 V=0—1，石块会被拉长；如果为了放进一张图而把所有 UV 岛单独归一化，不同构件上的石块大小也会变化。**要统一的是每米对应的取样尺度，不是每个对象都占满同一个方框。**

### Maya：按主要面投影，再检查比例和方向

对直墙正面选面，用 **UV → Planar** 的选项沿相应法线方向投影；墙的主要正背面位于 XY 平面，可以分别沿 Z 方向处理。地板顶面位于 XZ 平面，沿 Y 方向投影。侧面、端面和门框内侧也分别安排，避免一个投影把互相垂直的面压成线。

在 UV Editor 中查看壳的方向、比例和边界。通过投影尺寸或 UV 编辑，将墙正面调成 U 跨 1、V 跨 1.5；地板顶面跨 1×1。其余面按实际米制尺寸使用相同比例，例如墙顶 2×0.2 米对应 1×0.1 的 UV 跨度。

投影后使用 Cut / Sew 等工具组织边界；本例规则平面不一定需要复杂 Unfold。不要在最后无条件执行把所有壳缩放进 0—1 的 Layout。共享石材可以有重叠取样，但若要给一个角落画独特裂纹，重叠的面也会读到它，需要改为独特 UV、贴花或其他变化方式。

Maya 的[平面投影说明（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/Maya-Modeling/files/GUID-B6519472-C0ED-4C07-99C6-12107A3509D9.htm)说明了选面、投影轴与后续编辑；投影本身不保证最终 UV 合格。用带方向标记的棋盘格检查拉伸，再用重复石材图检查实际尺度。

本例的一个纹理周期是 2 米。若相邻墙段还要求石缝连续，除了密度，还要匹配朝向、UV 起点与边界相位；无缝纹理只能解决图像自身的周期边界，不能保证任意两套 UV 都接得自然。

## 在 3ds Max 中制作：对应尺寸、枢轴与 UV 的工作位置

### 用 Box 和 Editable Poly 做同一套构件

在**新场景**中，先确认 System Unit Setup 为 1 Unit=1 Centimeter，Display Units 也用厘米。显示单位与系统单位各有职责，不能只把界面从英寸切成厘米就当作交换单位已经一致；也不要在做完场景后随意改系统单位。FBX 交换中的这一区别见 [Autodesk 单位排查说明（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/3DSMax-Interoperability/files/GUID-C3389EE9-18A9-47EC-8222-EA94BD665FF8.htm)。

保存 `courtyard_modules_max_v001.max`。用 **Create → Geometry → Standard Primitives → Box** 创建体块，在参数中设置尺寸，再按需转为 **Editable Poly** 编辑点、边、面。

Max 默认 Z-up，本例把 X 当宽度、Y 当厚度或地面纵深、Z 当高度。不能直接照抄 Maya 的 XYZ 中心。对应几何中心如下：

<div role="region" aria-label="3ds Max 构件尺寸和中心，可横向查看" tabindex="0" style="overflow-x:auto">

| 部件 | 本例本地 X / Y / Z 尺寸，厘米 | Max 世界几何中心 |
| --- | --- | --- |
| 直墙 | 200 / 20 / 300 | (100,0,150) |
| 地板 | 200 / 200 / 20 | (100,100,−10) |
| 门框左立柱 | 100 / 20 / 300 | (50,0,150) |
| 门框右立柱 | 100 / 20 / 300 | (350,0,150) |
| 门框横梁 | 200 / 20 / 50 | (200,0,275) |

</div>

Box 的尺寸参数与创建方向要一起核对，以上表格给的是最终几何范围，不是无条件照填任意旋转对象的三个输入框。若需要按几何中心定位，可先用 **Hierarchy → Pivot → Affect Pivot Only → Center to Object** 把枢轴移到中心，退出 Affect Pivot Only，再用绝对变换放到表中位置。

将三个门框部件转为 Editable Poly，用 **Attach** 合成一个门框对象；检查原有材质 ID 和面是否仍正确。Attach 同样不代表已经焊接。直墙与地板保持各自独立对象。

### 清理静态变换，再设置输出枢轴

若制作中用了非均匀缩放或旋转，在保存版本后的静态输出副本上使用 **Utilities → Reset XForm → Reset Selected**，按需要塌陷，并检查几何与法线。它主要处理旋转和缩放，不是自动把对象位置和接口都改正确的命令。

随后在 Hierarchy 的 Pivot 面板开启 **Affect Pivot Only**，用世界坐标将各对象枢轴放到 (0,0,0)，再关闭该模式。移动枢轴时几何应保持在尺寸表对应位置；误在普通对象模式移动，会把墙本身挪走。相关区别见 [Max 枢轴说明（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/3DSMax-Animation/files/GUID-D2A59192-0A81-49D5-AAA1-E6898D0B3957.htm)。

Maya 的 Freeze Transformations 与 Max 的 Reset XForm 不应仅因名称近似就互相替代。这里处理的是静态方盒输出副本，不能把同样操作直接套到前面章节的骨架和蒙皮上。

### 用 UVW Map 建尺度，用 Unwrap UVW 检查结果

对规则盒状构件，先添加 **UVW Map** 修改器，Mapping 选 Box，Map Channel=1。关闭 Real-World Map Size，将 gizmo 的 Length / Width / Height 均设为 200 厘米，U / V / W Tile 均为 1。本例的方向保持与世界轴一致，再按表面朝向检查图案。

这些参数使一个投影周期对应 2 米，而不是让每个对象各占一整张图。不要之后再按 Fit 将 gizmo 改回每个对象的包围盒，否则会改变刚确定的尺度。使用普通显式 UV 的预览贴图也关闭 Use Real-World Scale，避免叠加另一套缩放规则。

在其上添加 **Unwrap UVW**，使用 Channel 1，打开 UV 编辑窗口检查各面：墙的 2×3 米正面是否对应 1×1.5 的跨度，地板是否对应 1×1；方向是否倒置，门框内侧是否拉伸。如果另做 Flatten 或打包，重新核对比例，不让自动归一化破坏密度。

[UVW Map 文档（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/3DSMax-Modifiers/files/GUID-78327298-4741-470C-848D-4C3618B18FCA.htm)解释了映射类型、通道、gizmo 与 Real-World Map Size。修改器预览是制作入口，交付时仍需确认 FBX 中保存的是目标 UV；Max 的程序纹理和显示规则不会自动变成 Unity Shader。

<!-- DATA-TODO: 分别制作 Maya / Max 三种构件，补单位、几何边界、枢轴和 UV 截图；检查 Reset/Freeze 前后与 FBX 重导入结果。记录实际局部轴及贴图密度，不用示意图代替制作文件。 -->

## Photoshop：制作一张可以重复的石材颜色图

### 先明确这张图负责什么

本例先做一张 **Base Color**，表达石材自身的颜色与纹理变化。它不负责固定方向的太阳阴影，也不直接提供表面凹凸；把强烈高光、投影和大块黑色缝隙都画进去，换光照后可能仍留着不该存在的明暗。

为了先检查平铺和密度，可以从简单灰褐石材、轻微颗粒与克制的色差开始。若设计需要清晰的砌块结构，再讨论石块尺寸、横纵接缝和相邻构件的方向，不能只套一张看起来像石头的图片。

### 分层绘制、检查重复，再导出 PNG

1. 新建 **1024×1024、RGB、8 位**文档，使用本例约定的 sRGB 颜色工作流程，保存为 `courtyard_stone_surface_v001.psd`。
2. 分层组织 `BASE_COLOR`、`STONE_VARIATION`、`GRAIN`；如有砌缝，单独放 `JOINTS`。保留可编辑图层，先避免过强的大尺度独特污迹。
3. 开启 **View → Pattern Preview**，缩小观察连续重复。检查左右、上下边界是否有直线接缝，以及某块深色斑点是否形成明显阵列。
4. 若用 Offset 检查边界，可在检查副本中使用 **Filter → Other → Offset**，水平/垂直各偏移 512 像素，选择 Wrap Around，把原边界移到图像中部修补；修补后继续检查四向重复。Offset 只是改变接缝位置，不会自动把内容修成无缝。
5. 保存 PSD，再通过 Export As 等入口导出 `courtyard_stone_basecolor.png`。不带 UV 线框、标注或评审文字，导出后重新打开检查尺寸、颜色与边界。

Adobe 的 [Pattern Preview 操作说明](https://helpx.adobe.com/photoshop/desktop/apply-painting-techniques/create-fill-with-patterns/create-a-new-pattern.html)给出 View 菜单入口；[滤镜边界处理说明](https://helpx.adobe.com/photoshop/desktop/effects-filters/artistic-stylize-filters/apply-distortion-and-stylize-filters.html)解释了 Wrap Around 的用途。本文的石材图层与 512 像素偏移是教学方案，不是已制作好的材质。

与法杖不同，这张图按周期覆盖 2×2 米，并不对应一份独特的整件 UV 展开模板。可以导出 UV 供检查，但不应把它作为底图，再把墙的专属污迹直接画进所有构件共享的石材周期中。

### 加入 PBR 细节时，要对齐目标 Shader

初次接入先使用颜色图和材质常量，方便检查密度与拼接。之后可以用材质工具、雕刻/高低模烘焙等方法增加法线和表面变化，但它们仍要保持同样的平铺尺度与边界。

法线贴图表达方向，不能把任意灰度图片改个文件名就当作切线空间法线。粗糙度描述粗糙程度，URP/Lit 的 Smoothness 则采用光滑度语义；按常见归一化定义转换时使用 `smoothness = 1 - roughness`，并继续核对输出格式与材质取样通道。

后面的 URP 基线用 Metallic=0、Smoothness=0.2，不依赖额外通道贴图。若以后增加 Metallic Map，默认 Smoothness Source 为 Metallic Alpha，读取其 Alpha；不要直接把外部工具的独立 Roughness 图塞进去，期待引擎自动完成反相和打包。

<!-- DATA-TODO: 制作实际 PSD/PNG，补 Pattern Preview 与 Offset 修补前后对照；法线、粗糙度及通道打包属于后续可选素材，当前未制作、烘焙或验证。 -->

## 导出模型与纹理：交付静态数据，不交付软件预览效果

本例按构件分别导出三份 FBX。每次只选本构件的输出网格，排除参照角色、摄像机、灯光、展台与其他模块；保证其相对输出原点的范围符合约定。

**Maya** 使用 File → Export Selection，格式 FBX；**Max** 使用 File → Export → Export Selected。两者本轮都关闭动画输出，不需要骨架和蒙皮；保留 UV、材质槽与目标法线/硬边数据，按项目需要固定三角化方案。Maya 的[导出选项（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/Maya-Basics/files/GUID-FE8DBEAA-C2DD-43B3-9933-4BA4CDDEAA89.htm)与 Max 的 [FBX 选项资料（2024）](https://help.autodesk.com/cloudhelp/2024/ENU/3DSMax-Interoperability/files/GUID-31EA2A4F-A126-498B-96E8-31811522738A.htm)提供相应设置背景。

单位与轴转换采用各工具的 FBX 路径并记录设置。先导出一个带有非对称方向标记的构件，确认源场景的哪一侧在 Unity 中成为 +X、+Z，再固定整套输出约定。前面的 Max 表格描述源场景坐标，不承诺其 +Y 数值未经转换就成为 Unity 的 +Z。

若导入后地板落到目标原点的负方向，应统一修订源几何朝向、输出层级或导入轴转换配置，重新检查整套边界；不要在每个场景实例上各加一套补偿。Max 的 Z-up 与 Maya 的 Y-up 需要分别核对，目标是 Unity 中的尺寸、朝向和原点成立，不是两个源文件的 XYZ 数字完全相同。

本例不依赖 Embed Media 搬运贴图。PNG 独立交付，在 Unity 创建目标材质；DCC 中的程序石材、物理材质或渲染器节点，不因为文件是 FBX 就能原样成为 URP/Lit。

```text
ArtSource/Courtyard/
  Maya/courtyard_modules_maya_v001.ma
  Max/courtyard_modules_max_v001.max
  Textures/courtyard_stone_surface_v001.psd
  module-handoff.md

ArtExport/Courtyard/
  Maya/v001/wall_straight_2m.fbx
  Maya/v001/gate_frame_4m.fbx
  Maya/v001/floor_2m.fbx
  Max/v001/wall_straight_2m.fbx
  Max/v001/gate_frame_4m.fbx
  Max/v001/floor_2m.fbx
  Textures/v001/courtyard_stone_basecolor.png
```

原稿可以同时包含套件；导出物按用途拆分。交接说明记录尺寸、枢轴、正方向、2 米纹理周期、源版本、导出设置、材质槽，以及碰撞和光照 UV 的准备状态。

在独立场景重新导入 FBX，检查三个几何范围、UV 与法线。原稿里的棋盘格正确，只说明源软件的当前求值正确；交换文件中还可能漏掉数据或改变层级。

## 进入 Unity：材质、模型和 Prefab 怎样接起来

### 先验证尺度与原点

沿用本系列 Unity 6000.0 / URP 17 项目，将一套来源的 FBX 放到 `Assets/Art/Courtyard/Models/`，PNG 放到 `Textures/`。比较 Maya 与 Max 时使用独立来源目录，不互相覆盖同名模型。

在模型导入面板保留与文件单位匹配的转换，静态资产不需要动画。检查 Normals 等设置，本轮可先导入制作端法线，观察方盒边缘是否保持硬朗。单位与导入属性见 [Unity Model 导入参考](https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Model.html)。

把直墙拖入场景，与一个 1 米参照方盒比较：单位实例应宽 2 米、高 3 米，地板顶面为局部 Y=0。不要只看根 Transform 的 Scale 是否为 1；FBX 还可能有单位转换和子节点变换。也不要不看原文件单位，就把 Scale Factor 固定改成 0.01。

如果单位或轴向有误，优先回到导出与导入设置修正。为单个实例补一个 100 倍缩放，可能让这一处看似正常，却破坏后续碰撞坐标、替换和贴图尺度。

### 创建 URP/Lit 石材

选择 PNG，在纹理导入中使用普通二维纹理，本例开启 **sRGB（颜色纹理）**、Wrap Mode=Repeat、Generate Mip Maps，初次 Max Size 保留 1024。过滤与目标平台压缩按后续观察选择；用 Clamp 则不会按本例需要重复边界。纹理类型及设置入口见 [Unity Texture Importer](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TextureImporter.html)。

在 `Materials/` 新建 `M_Courtyard_Stone.mat`，Shader 选 **Universal Render Pipeline/Lit**：

- Workflow Mode：Metallic；Surface Type：Opaque。
- Base Map：`courtyard_stone_basecolor.png`；颜色乘数先用白色。
- Metallic：0；Smoothness：0.2。
- Tiling：(1,1)；Offset：(0,0)，让制作端 UV 控制本例周期。

将同一份材质分配给三个构件的目标材质槽。可以在 FBX 的 Materials 面板做 Remapped Materials，也可以在受控 Prefab 中分配；检查 MeshRenderer 的每个槽，避免门框 Attach/Combine 后残留未映射的额外槽。

材质属性及 Smoothness 通道见 [URP/Lit Inspector 参考](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)。将来加入法线时，纹理按 Normal Map 类型导入并赋给 Normal Map 槽；数值遮罩按数据语义处理，不能全部沿用颜色图的 sRGB 设置。

此时先看石块大小是否一致、方向是否符合设计，再评价精细程度。若一米宽门柱上的纹理比两米墙上的纹理大一倍，先查 UV 和材质 Tiling，不急着重画整张图。

### 做成可放置的 Prefab，并单独配置碰撞

建立三个根对象 `WallStraight2m`、`GateFrame4m`、`Floor2m`，在各自原点下放入对应模型，确保网格相对根节点的范围仍符合前面的表，再保存为同名 Prefab。根节点保持单位缩放，以后摆放这些 Prefab 实例。

如果导入模型本身有包装根节点，先检查网格相对 Prefab 根的实际结果；不要为了让所有子 Transform 看起来都是零，就随意移动网格并改变拼接边界。

本例用简化 **BoxCollider** 表达静态阻挡，不开启模型的自动碰撞生成，避免同时留下重复的 MeshCollider。以下 Center / Size 都相对**已符合几何约定的 Prefab 根**，单位米：

<div role="region" aria-label="场景构件 BoxCollider 配置，可横向查看" tabindex="0" style="overflow-x:auto">

| Prefab / 碰撞部分 | Center | Size |
| --- | --- | --- |
| 直墙 | (1,1.5,0) | (2,3,0.2) |
| 地板 | (1,−0.1,1) | (2,0.2,2) |
| 门框左立柱 | (0.5,1.5,0) | (1,3,0.2) |
| 门框右立柱 | (3.5,1.5,0) | (1,3,0.2) |
| 门框横梁 | (2,2.75,0) | (2,0.5,0.2) |

</div>

门框使用三个 BoxCollider，Is Trigger 关闭；不要给整个 4×3 米外包围盒只加一个实心碰撞，否则可见门洞仍会被阻挡。静态墙、地板和门框本身不需要 Rigidbody；与它们交互的移动对象再采用项目相应的物理或角色移动方案。[BoxCollider 文档](https://docs.unity3d.com/6000.0/Documentation/Manual/class-BoxCollider.html)说明了局部 Center 与 Size 的含义。

这套方盒只服务于当前体块练习。破损墙和斜面需要重新选择代理形状，装饰轮廓与通行边界不一定完全一致；应让地编与客户端知道这一区别。

## 拼装测试：用具体坐标检查接口

### 先拼一个 8×6 米测试围合

新建 `CourtyardModuleTest.unity`，建立单位变换的 `ModuleTestRoot`，在其下放置三个 Prefab 的实例。以下坐标为相对根节点的位置，所有实例 Scale=(1,1,1)，只有表中侧墙旋转。

<div role="region" aria-label="Unity 模块测试场景摆放坐标，可横向查看" tabindex="0" style="overflow-x:auto">

| 实例组 | Position / Y 旋转 | 数量与覆盖 |
| --- | --- | --- |
| 地板 | (2i,0,2j)，i=0…3、j=0…2；旋转 0° | 12 块，覆盖 X=0…8、Z=0…6 |
| 后墙 | (0,0,6)、(2,0,6)、(4,0,6)、(6,0,6)；0° | 4 段，沿 X 连续 |
| 前墙两侧 | (0,0,0)、(6,0,0)；0° | 2 段，中间留给 4 米门框 |
| 门框 | (2,0,0)；0° | 净开口位于 X=3…5、Y=0…2.5 |
| 左侧墙 | (0,0,0)、(0,0,2)、(0,0,4)；−90° | 3 段，本地 +X 转向世界 +Z |
| 右侧墙 | (8,0,0)、(8,0,2)、(8,0,4)；−90° | 3 段，与左侧对应 |

</div>

这里使用墙厚中心线作为围合边界，墙脚会与边缘地板相交；角落体块也有小范围体积相交。本例允许这些结构交叠，但不应留下两层共面的可见表面闪烁。它不是经过细化的建筑转角方案；若需要外轮廓、压顶和内角都干净，应制作相应转角或端头构件。

先用 Inspector 数值定位，再设置网格吸附步长 1 米、旋转吸附 90°体验连续摆放。吸附只帮助执行规则，不能修正原本错误的枢轴和尺寸。

### 分三轮观察，不把外观检查当成全部验证

**第一轮看几何。** 用灰色材质和正交视图检查：2 米墙段是否首尾接上，地板顶面是否同高，转向是否围成预期范围，门框净开口有没有被网格或碰撞封住。检查顶面、背面与近处边缘，不只看庭院中心的一张截图。

**第二轮看表面。** 换回石材，靠近看拉伸与硬边，拉远看重复斑点；改变方向光角度，对照墙面、门柱和地板的纹理尺度。几何裂缝、UV 接缝与光照接缝要分开判断：一条暗线不一定是模型没有贴齐，也可能来自法线或后续烘焙。

**第三轮看运行使用。** 在场景中加入 Camera、方向光与尺度参照，在 Game 视图和 Play 模式确认显示。为入口外的起步留出地面，可临时在 (2,0,−2)、(4,0,−2) 各放一块地板，避免参照角色从无支撑区域下落。再使用项目已有角色控制器，沿门洞中心 X=4，从入口外向 +Z 移动，观察穿过入口和靠近墙角的表现；没有移动控制器时，本轮只能完成几何与碰撞配置检查，不能记录“已通过角色通行”。

参照体可以用高 1.8 米、直径 0.6 米的胶囊说明空间余量，但实际角色的控制器半径、皮肤宽度、台阶能力、装备和镜头还要另外检查。通过 Transform 手动把它拖过墙，不会证明正常移动路径下碰撞可用。

记录检查所用模型、Prefab、材质和控制器版本，以及出现问题的位置。模型试拼场景要随套件保留；之后哪怕只改一个枢轴，也应重新检查直线、转角与门洞。

<!-- DATA-TODO: 制作三份 FBX、URP 材质、Prefab 和 CourtyardModuleTest.unity；补坐标拼装、接缝、碰撞显示、近远距离及换光照截图。实际角色通行需明确控制器和镜头设置，本轮未执行。 -->

## 材质复用、光照 UV 与 LOD，需要按方案增加

### 平铺、条带、独特贴图与贴花怎样选择

本例平铺石材适合大面积规则表面，但不能自然覆盖全部需求。可以按问题增加手段：

<div role="region" aria-label="场景表面复用方案与代价，可横向查看" tabindex="0" style="overflow-x:auto">

| 方式 | 适合解决的问题 | 需要付出的代价 |
| --- | --- | --- |
| 平铺纹理 | 大面积表面的统一尺度 | 容易重复，独特污迹难以直接定位 |
| 条带贴图（Trim Sheet） | 多处共用檐口、边框、装饰条 | 几何与 UV 要对齐条带比例和方向 |
| 独特 UV / 贴图 | 独特道具、雕刻或固定损坏 | 增加制作量与纹理数据 |
| 贴花（Decal） | 局部污渍、标志、裂纹等覆盖 | 需要目标渲染方案支持，并检查投影、排序与成本 |

</div>

这些方式可以组合；增加顶点色或材质变化也需要 Shader 和数据支持。共享一份材质有助于集中维护，但不直接保证场景合成一次绘制。实际批处理、实例化、光照、材质变体和可见性仍需在项目与设备上测量。

### 用于颜色的 UV，不等于用于烘焙光照的 UV

本例第一轮采用实时方向光检查材质与几何，没有完成光照烘焙。若目标方案需要 baked lightmap，应另外准备光照用 UV：在 0—1 范围内、不重叠，并按目标分辨率留出足够岛间距。

Unity 对烘焙光照使用第二套网格 UV，即 `Mesh.uv2`，也称 UV1。它与本例颜色取样用的 UV0 不同。可以在 DCC 创建第二套 UV，也可以在模型导入 Model 面板开启 **Generate Lightmap UVs**，Apply 后检查生成结果。要求与入口见 [Unity 光照 UV 说明](https://docs.unity3d.com/6000.0/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html)。

Max 的 Map Channel 2、Maya 第二个 UV set 与 Unity 第二套 UV 的映射，需要实际导入确认；别只凭界面编号就宣布对应正确。用于平铺的重叠 UV0 可以合理存在，但不能原样当作合格光照 UV。自动生成也仍需检查重叠、边距、拉伸和烘焙后的接缝。

### LOD 保留接口，简化也要有收益

本例方盒面数已经很低，不额外做几乎相同的多档 LOD。复杂檐口、雕像或植被是否需要 LOD，应根据观察距离、轮廓、设备预算和实际成本选择。

若制作 LOD，各档保持相同的尺寸基准、枢轴和放置关系，检查切换时轮廓、UV 与材质是否跳变。简化模型不意味着碰撞必须跟着每档替换；物理代理和可见细节可以按使用需求分开。

## 修改一个模块，怎样让整组资产继续可用

假设门洞太窄，需要从 2 米改成 2.5 米。先确认外宽是否仍为 4 米：若保持外宽，两侧立柱尺寸就要改变；若连外宽一起扩大，原来的前墙摆放坐标也会改变。这是两种不同的修改，不能只在场景里把门框 X 缩放为 1.25 就当作全部完成。

回到原稿调整几何，保持选定枢轴规则，再重新检查 UV 密度、门框碰撞与拼装场景。非均匀拉伸除了改变洞口，还会改变立柱、纹理比例和代理尺寸；应将这些影响一起记录。

新版本导出后更新稳定的引擎输入路径，保留 `.meta` 与需要延续的引用，检查三个 Prefab 的材质和碰撞仍匹配。交接说明要区分“只是表面颜色变化”与“拼接边界已经变化”；后一类修改需要通知所有使用该构件的位置重新检查。

最终交给地编的是一组可放置资产和使用说明：原稿与导出来源、几何/枢轴、材质、碰撞状态、测试场景及已知限制。它们为下一篇的搭建提供条件；路线是否好走、入口是否醒目、镜头是否被挡，仍要放在整体空间里判断。

## 本篇边界与延伸阅读

这一章用最小套件解释了制作路径，尚未提供完成的美术素材与运行验收。复杂地形、植被、程序化建筑和完整烘焙流程需要独立素材与证据，不能由三个方盒推导出已经完成的环境生产线。

- [02｜原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})：空间设计信息与模块拆分的起点。
- [03｜角色资产]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})：UV、纹理、材质与文件传递的基础。
- [07｜地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})：后续用资产组织路线、视线和整体场景，当前为提纲。
- [资源规范与准入]({{< relref "delivery-engineering/delivery-content-pipeline-03-asset-standards.md" >}})：按需延伸资源规则、检查与反馈。

**场景构件能被反复使用，依靠的是形状、拼接接口与表面规则的共同稳定。**
