---
title: "09｜TA：怎样把视觉方案变成团队可用的技术能力"
slug: "game-art-production-09-technical-art"
date: "2026-10-25"
lastmod: "2026-10-26"
description: "沿庭院目标反馈的复用需求，走过方案选择、Shader Graph 材质原型、参数开放、Unity Prefab 检查器、质量分档与协作维护。"
draft: false
featured: false
content_stage: "article"
weight: 89
tags: ["美术生产", "客户端", "协作", "TA"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 9
type: "reading-series"
reading_title: "TA：怎样把视觉方案变成团队可用的技术能力"
reading_summary: "用目标反馈复用的需求，说明 TA 如何选择方案、制作 Shader Graph 参数模板和只读 Prefab 检查器，再连接创作者使用、运行验证与版本维护。"
---

上一章给出了蓄光、释放和目标反馈的制作方案。接下来，如果其他美术也要做一组同类效果：颜色怎样调整，哪些参数可以改，怎样避免误开循环，出了问题回到哪个文件修改？

只交付一个调好的 Prefab，接收者仍可能依赖原作者解释。把所有参数都开放，也可能让同一套效果出现不受控的变体。这里需要继续解决的是：怎样让一个方案被团队理解、使用、验证并维护。

**TA 的一类核心工作，是把视觉目标与制作、运行之间的缺口，转化为可复用的技术方案和清楚的使用边界。** 本篇用庭院目标反馈的复用需求，说明这项工作怎样发生。

> **读这篇之前**：先看[第 08 篇特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})的 PNG、材质、粒子 Prefab、播放与结束约定；庭院观察条件来自[第 07 篇地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})。[TA 职责与协作接口]({{< relref "rendering/ta-role-boundaries-and-deliverables.md" >}})提供更广的角色背景，本篇重点落到工具和产物。

> **版本与证据范围**：引擎操作方案参考 Unity 6（6000.0）、URP 17 和 Shader Graph 17 官方资料；DCC 工具入口参考 Autodesk 2024 帮助。参数、规则、代码与配图为教学设计。尚未制作实际 Shader Graph、材质或检查器工程，也未执行 C# / Shader 编译、DCC / Unity 实操或设备实验。正文给出可实施的步骤和待验证条件，不把静态说明写成工具已投产或运行已通过。

## 从一个反复出现的问题，判断需要什么技术工作

TA 不是所有视觉问题的固定接收者，也不等于“最后把画面调好的人”。先确认缺口在哪里，才能决定是补素材、改制作方法、研发效果、提供工具，还是调整运行方案。

### 视觉目标、制作输入和运行约束要放在一起看

本例提出一个教学需求：

> 庭院的目标反馈需要提供冷色与暖色两种配色；美术可以调节颜色、透明度与亮度，同时保留粒子原有淡出。交接前能发现误开循环、自动播放或漏材质的问题，并在同一场景复查。

这段需求包含不同性质的问题：

<div role="region" aria-label="需求缺口与对应工作，可横向查看" tabindex="0" style="overflow-x:auto">

| 缺口 | 需要先确认什么 | 可能的产物 |
| --- | --- | --- |
| 不知道怎样调出目标观感 | 形状、配色、背景、观察距离 | 对照图、效果原型、素材调整 |
| 不知道哪些参数可安全修改 | 参数语义、范围、联动与默认值 | 材质模板、参数说明、示例预设 |
| 反复出现相同配置错误 | 错误是否有明确判定条件 | 检查器、规则编号与修复位置 |
| 在设备或实际技能中失效 | 管线、生命周期、渲染与平台条件 | 运行验证、定位记录、降级方案 |
| 更改后影响很多内容 | 依赖范围、兼容性与责任 | 版本约定、迁移步骤和回归清单 |

</div>

一个问题可以涉及多个岗位。美术提出观感与创作需要，TA 可以帮助建立实现方案和制作约定；图形、工具与客户端程序根据项目分工处理渲染能力、编辑器集成和运行生命周期。质量、预算与上线取舍由相应负责人共同决策，不能默认全部交给 TA 一人拍板。

### 不同 TA 方向，处理的是不同缺口

可以按工作对象理解几个方向：

- **渲染与效果**：材质、Shader、灯光与表现方法，连接视觉目标和图形能力。
- **资产与制作管线**：导出、导入、命名、数据检查与批处理，连接源文件和引擎资产。
- **程序化与内容工具**：把重复构成、布局或变体生成做成可控过程，并保留人工修改入口。
- **角色与动画技术**：绑定、变形、动作工具和角色制作规则，连接 DCC 操作与运行需求。

这是工作分类，不是所有公司的统一岗位表。小团队可能兼任，较细分的团队则由多人协作。本文只展开“材质参数模板 + 引擎检查工具”这一条路径，不用它代表全部 TA 工作。

## 从原型到团队使用，中间还有一层产品化工作

原型回答“这种效果能不能表达目标”；团队产物还要回答“别人怎样使用，错误怎样定位，版本变化怎样处理”。

至少应一起交付四部分：

1. **实现**：材质图、脚本、预设或其他可以运行的技术产物。
2. **接口**：输入是什么，参数是什么，哪些约束必须保持。
3. **验证方法**：在哪个场景、镜头、平台和版本中观察，结果如何记录。
4. **维护约定**：谁修复，哪些变更兼容，旧内容怎样迁移。

{{< art-ta-workflow >}}

### 先比较现成能力，再决定是否研发

第 08 篇的 URP Particles/Unlit 已经支持纹理、粒子颜色与透明混合。如果只要冷暖配色，现有材质与粒子 Start Color 可能就够用：整理两份材质或 Prefab 预设，再写清说明，通常比维护新 Shader 更直接。

若多个效果确实需要统一的属性名、独立透明度或统一亮度控制，可以再评估 Shader Graph；若需要扭曲、深度交互、特殊几何或管线扩展，才进一步确认额外能力和成本。

本篇仍做一个小 Shader Graph 原型，**用于解释参数接口怎样形成**，并不宣称它比上一章现成 Shader 更快、更完整，或必须替换全部效果。能由现成配置解决的问题，也可以是一次完整的 TA 交付。

### 把验收条件写在制作之前

本例准备观察：

- 换成不同配色后，圆环形状、粒子淡出与目标轮廓是否仍清楚。
- Opacity=0 时该圆环是否不可见，Opacity=1 时是否保留纹理和粒子 Alpha。
- 调 Intensity 时是否主要改变颜色输出，而没有意外改变透明形状。
- 两份材质是否相互独立；修改一份是否误影响其他引用者。
- Prefab 的配置检查能否定位到具体层级；静态无告警后，运行取消与结束是否仍正确。

这些都是待执行条件。它们让原型比较有依据，但不能代替真正的工具试用和设备记录。

## 工具分别在哪一段工作，文件怎样流转

TA 不一定长期停在同一种软件里。关键是工具运行在哪个阶段、接收哪份数据、输出什么，以及谁继续消费。

<div role="region" aria-label="TA 工具与文件位置，可横向查看" tabindex="0" style="overflow-x:auto">

| 工作位置 | 工具举例 | 输入与输出 | 本篇使用范围 |
| --- | --- | --- | --- |
| 原稿与素材 | Photoshop | PSD → 明确导出的 PNG / 遮罩 | 复用上一章圆环，不重做绘画教程 |
| 模型与动作制作端 | Maya Python / MEL、3ds Max MAXScript、FBX 预设 | 场景与选中对象 → 检查报告 / 导出资源 | 说明导出辅助的工作位置 |
| 引擎视觉方案 | Unity Shader Graph、材质与粒子编辑 | 纹理 / 顶点数据 → Shader、材质、效果模板 | 展开圆环参数原型 |
| 引擎制作工具 | Unity Editor C# | 所选 Prefab → 规则编号、路径与提示 | 给出只读检查器方案 |
| 游戏运行与测量 | 项目表现系统、Profiler、Frame Debugger | 实例与实际场景 → 行为 / 画面 / 成本记录 | 设计检查条件，尚无实测 |

</div>

### DCC 脚本怎样帮助导出，而不是再造一套资产制作

假设第 06 篇的直墙反复出现命名、单位或导出选择错误，可以把检查放回制作端：

- Maya 在 `Windows > General Editors > Script Editor` 的 Python 标签运行检查脚本；读取选中对象、层级和项目约定，输出需要修改的节点。入口见 [Maya Script Editor](https://help.autodesk.com/cloudhelp/2024/ENU/Maya-Scripting/files/GUID-7C861047-C7E0-4780-ACB5-752CD22AB02E.htm)。
- 3ds Max 可从 Scripting 菜单创建或运行 MAXScript，在 Listener 查看输出；以相同交付接口检查 Max 场景，不要求先转进 Maya。入口见 [3ds Max Scripting Menu](https://help.autodesk.com/cloudhelp/2024/ENU/MAXScript-Help/files/MAXScript-Introduction/Accessing-MAXScript/GUID-9CF9AE1B-CDB5-4120-96C0-BCE410572CC9.html)。
- 检查通过后，显式选定导出对象、输出位置和版本化 FBX 预设，再按第 06 篇的流程导出；导入 Unity 后仍复核尺度、轴向、材质与碰撞。

“检查单位”不等于自动修好模型，也不应在原角色绑定上批量 Freeze / Reset 变换。若工具需要修改数据，应该先让用户看见修改对象与差异，提供可撤销方式和副本，再明确执行。本文没有给出 DCC 自动修改或导出脚本。

这种辅助替代的是重复读取和配置操作，不替代拓扑、变形、构图与最终画面判断。源文件中检查通过，仍不保证交换格式和引擎导入没有改变结果。

## 在 Shader Graph 制作一个可解释的圆环模板

这里复用第 08 篇的 `cast_ring_rgba.png`。它以白色和 Alpha 表达空心圆环，粒子系统再提供起始颜色与随生命变化的颜色/透明度。

### 建立图资产，先固定渲染方式

在 Unity 的 Project 窗口中，通过 `Create > Shader Graph > URP > Unlit Shader Graph` 建立：

```text
Assets/Art/Effects/CourtyardCast/
  Shaders/SG_CourtyardRing.shadergraph
```

菜单与面板以实际安装版本为准。打开图，在 Graph Inspector 确认目标为 Universal / Unlit，Surface Type=Transparent，Blending Mode=Alpha，关闭 Alpha Clipping；Depth Write 使用 Auto，不额外强制透明物体写深度。透明输出与深度属性见 [URP Unlit shader graph](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/prebuilt-shader-graphs-urp-unlit.html)。

先不增加光照、扭曲、软粒子或特殊深度规则。透明材质仍会参与与场景的遮挡关系，不是为了提示就自动画在所有物体上方。

### 在 Blackboard 开放四个语义清楚的属性

在 Blackboard 用加号建立属性，确认 Exposed 开启，并在 Node Settings 明确填写 Reference：

<div role="region" aria-label="圆环模板公开属性，可横向查看" tabindex="0" style="overflow-x:auto">

| 显示名 / Reference | 类型与默认值 | 允许怎样使用 | 不代表什么 |
| --- | --- | --- | --- |
| Shape Texture / _ShapeTex | Texture2D，材质中指定圆环 PNG | 形状与透明输入；本例采 UV0 | 不自动支持任意通道打包 |
| Tint / _Tint | Color，白色，Alpha=1 | 材质整体调色，与粒子颜色相乘 | 不替代粒子的生命渐变 |
| Opacity / _Opacity | Float Slider，0—1，默认 1 | 整体透明倍率，图中再限幅 | 不改变粒子数量或 Lifetime |
| Intensity / _Intensity | Float Slider，0—4，默认 1 | RGB 输出倍率，图中限制到 0—4 | 不等于光源强度、曝光或性能预算 |

</div>

这些范围是教学选择，不是所有项目的美术规格。属性添加与 Exposed 入口见 [Shader Graph Blackboard](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Blackboard.html)，显示名与 Reference 的区别见 [Property Types](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Property-Types.html)。

显示名面向使用者，Reference 是脚本与材质数据需要稳定识别的名字。以后改掉 `_Opacity`，不能只确认面板文字变了，还要检查旧材质值和运行代码。滑条范围也不等于所有外部写入都已受约束，所以这里在图中另做限制。

### 把颜色和 Alpha 分成两条可检查的路径

在图中建立：

1. 将 Shape Texture 拖入图，接到 Sample Texture 2D 的 Texture，按普通颜色纹理采样；UV 使用 UV 节点的 UV0，保持第 08 篇纹理导入约定。
2. 加入 Vertex Color。它读取顶点颜色，粒子端需要提供对应的颜色数据。
3. 用 Multiply 将采样 RGBA、Vertex Color RGBA 和 Tint RGBA 逐通道相乘，记为 Q。
4. RGB 路径取 Q 的 R/G/B，用 Combine 组成三通道，再乘经 Clamp 限制到 0—4 的 Intensity，接 Fragment 的 Base Color。
5. Alpha 路径取 Q.a，乘经 Saturate 限制到 0—1 的 Opacity，再接一个 Saturate，连到 Fragment 的 Alpha。
6. Save Asset，检查图错误、材质预览和实际粒子显示。

以公式表示这套教学连接：

```text
Q = textureRGBA × vertexRGBA × tintRGBA
BaseColor = Q.rgb × clamp(Intensity, 0, 4)
Alpha = saturate(Q.a × saturate(Opacity))
```

普通 Alpha 混合下，这里不再手动将 BaseColor 乘一次最终 Alpha，以免把本例改成另一套预乘输入约定。不同混合模式要配套检查，不能只切换面板选项而忽略输入。

各节点的能力见 [Sample Texture 2D](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Sample-Texture-2D-Node.html)、[Vertex Color](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Vertex-Color-Node.html)、[Multiply](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Multiply-Node.html)、[Clamp](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Clamp-Node.html)与 [Saturate](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Saturate-Node.html)。

Vertex Color 这一步很容易漏：如果只读纹理和材质颜色，第 08 篇的 Color over Lifetime 可能无法按原意影响图的输出。检查 Particle System Renderer 的顶点流，确认包含 Color 与图所用 UV，字段说明见 [Particle System vertex streams](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysVertexStreams.html)。

### 创建材质和 Prefab 副本，再比较变化

基于图建立 `M_CourtyardRing_Cyan.mat` 与 `M_CourtyardRing_Warm.mat`，两者都引用圆环 PNG，Opacity=1、Intensity=1，Tint 先分别取冷暖色、Alpha=1。

复制上一章的 `VFX_Impact.prefab` 为 `VFX_Impact_TA.prefab`。只把根圆环 Renderer 的材质换成新材质，子碎光仍用上一章的 Soft 材质，保留其他粒子参数和生命周期约定。

注意，根粒子的 Start Color 原本也参与调色。第一轮将它设为白色、Alpha=1，Color over Lifetime 的 RGB 保持白色，让材质 Tint 单独决定冷暖，Alpha 渐变继续保留；再有意改变粒子颜色，检查乘色关系。否则两处颜色混合可能让“暖色材质”仍偏向原来的颜色。

分别观察 Opacity=0/0.5/1，以及 Intensity=0/1/2 等值。Opacity 归零不会停止模拟、销毁实例或省掉所有成本；Intensity 增大也不保证出现 Bloom，更不等于照亮周围场景。

修改共享材质会影响它的其他使用者。美术需要独立配色时，应明确编辑哪份材质或预设；运行侧还需要按实例的使用范围，另外约定临时参数覆写怎样隔离与恢复，不能随意修改共享资产。

### 写清这个图没有保留哪些能力

这个原型只消费普通纹理、UV0 和顶点色，没有实现上一章现成粒子 Shader 的所有能力，例如翻页混合、软粒子、扭曲及其他特定功能。

如果团队需要这些能力，要继续实现并验证，或保留现成 Shader。图节点少也不意味着一定更快；编译、变体、渲染路径和平台结果需要另行观察。模板的适用范围是交付的一部分。

## 把重复错误变成可定位的检查结果

材质原型解决了怎样调整，但仍不能阻止制作时误开 Looping，或者漏掉材质引用。规则足够明确时，可以做一个制作端检查入口。

### 先说明本例检查的对象和规则

本篇只检查第 08—09 篇这类一次性粒子 Prefab。它们由预览拥有者显式播放，根节点提供 Particle System，结束后由拥有者销毁。

<div role="region" aria-label="一次性粒子示例的静态检查规则，可横向查看" tabindex="0" style="overflow-x:auto">

| 编号 | 本例约定 | 为什么检查 | 修复位置 |
| --- | --- | --- | --- |
| TA001 | 根节点有 Particle System | 驱动引用根系统并检查子系统 | Prefab 根结构 |
| TA002 | 根节点缩放为 1 | 减少资产自身缩放的干扰 | Prefab 根 Transform |
| TA003 | 所有粒子 Looping 关闭 | 本轮采用一次性效果 | 对应系统 Main |
| TA004 | Play On Awake 关闭 | 拥有者明确发起播放 | 对应系统 Main |
| TA005 | Stop Action=None | 避免和拥有者的清理策略冲突 | 对应系统 Main |
| TA006 | 粒子 Renderer 有主材质 | 本例都使用实际绘制的粒子 | 对应 Renderer 引用 |

</div>

这些是**本教学方案的接口检查**。持续光环可以合理循环，其他系统也可以采用 Callback 或自管理清理，不能把表中规则直接提升为全项目通用禁令。

根缩放为 1 也不保证实例的祖先没有缩放；有主材质不保证 Shader、纹理和平台支持正确。每条检查都要说明它证明了什么。

### 在 Unity Editor 提供一个只读入口

默认程序集工程可将脚本放在：

```text
Assets/Editor/ArtProduction/CourtyardVfxInspector.cs
```

使用 asmdef 的工程，应把它归入明确的 Editor 程序集并配置引用，避免 UnityEditor 依赖进入运行程序集。以下代码只读取所选 Prefab 的结构与字段，不主动保存或修改资产；**尚未实际编译和运行**。

```csharp
using System;
using UnityEditor;
using UnityEngine;

public static class CourtyardVfxInspector
{
    [MenuItem("Tools/Art Production/Inspect Selected VFX Prefab")]
    static void Inspect()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(assetPath) ||
            !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("Select one VFX Prefab asset in the Project window.");
            return;
        }

        var root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (root == null) return;
        int issues = 0;

        void Report(string rule, Transform node, string message)
        {
            issues++;
            string relative = AnimationUtility.CalculateTransformPath(
                node, root.transform);
            if (string.IsNullOrEmpty(relative)) relative = "<root>";
            Debug.LogWarning(
                $"[{rule}] {assetPath} :: {relative} :: {message}",
                node.gameObject);
        }

        if (root.GetComponent<ParticleSystem>() == null)
            Report("TA001", root.transform,
                   "Expected ParticleSystem on the Prefab root.");

        if ((root.transform.localScale - Vector3.one).sqrMagnitude > 0.000001f)
            Report("TA002", root.transform, "Expected unit root scale.");

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            if (main.loop)
                Report("TA003", ps.transform, "Disable Looping for this contract.");
            if (main.playOnAwake)
                Report("TA004", ps.transform, "Disable Play On Awake.");
            if (main.stopAction != ParticleSystemStopAction.None)
                Report("TA005", ps.transform,
                       "Owner-managed cleanup expects Stop Action=None.");

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                Report("TA006", ps.transform, "Assign the main particle material.");
        }

        Debug.Log(
            $"Static VFX contract: {assetPath}; systems={systems.Length}; " +
            $"issues={issues}. Runtime, visual and device checks still required.",
            root);
    }
}
```

菜单注册、资产读取与相对路径能力分别见 [MenuItem](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MenuItem.html)、[LoadAssetAtPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.LoadAssetAtPath.html)和 [CalculateTransformPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationUtility.CalculateTransformPath.html)。

检查器使用 includeInactive=true，包含暂时关闭的子物体，便于发现以后启用时仍可能违反本例约定的字段；接口见 [GetComponentsInChildren](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GameObject.GetComponentsInChildren.html)。

这里读取 sharedMaterial，不通过修改材质来“验证”；更不能自动调整共享材质让警告消失。共享引用的影响范围见 [Renderer.sharedMaterial](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer-sharedMaterial.html)。

### 让告警指向修复输入，而不是只输出不合格

在 Project 窗口选中 `VFX_Impact_TA.prefab`，执行菜单，查看 Console。需要设计一轮正反输入检查：

1. 在测试副本中打开 ImpactSparks 的 Looping，检查是否报告 TA003、资产路径和子物体路径。
2. 清空根圆环的材质，检查是否报告 TA006；恢复后再检查。
3. 改根缩放、Play On Awake 或 Stop Action，逐项验证对应规则，不用一次改完再猜哪条有效。
4. 将子系统关闭，确认它仍被扫描；这不代表运行时一定启用了这个系统。
5. 选择 Scene 实例或 PNG，确认入口提示选择 Prefab，而不是扫描无关范围。
6. 对符合本例约定的 Prefab，确认摘要中 issues=0，再继续人工和运行检查。

这些是工具待执行的验证步骤，不是已获得的测试结果。假设输出可能是：

```text
[TA003] Assets/Art/Effects/CourtyardCast/Prefabs/VFX_Impact_TA.prefab
:: ImpactSparks :: Disable Looping for this contract.
```

示例只展示格式。真正报告还应关联规则版本、运行时间和工具版本，必要时导出结构化记录；本轮脚本没有实现批量扫描、CI 门禁或自动修复。

它也没有验证纹理 Alpha、Shader 属性语义、完整材质依赖、拖尾材质、动画标记、运行挂点和设备成本。对这些未覆盖项，应写“未检查”，而不是让零告警替它们背书。

## 把原型交给创作者使用，再回到实际运行

TA 自己能操作，不代表别人可以独立复现。至少让使用者按交接说明完成一次修改，并指出哪里需要原作者口头补充。

### 用同一个庭院场景做对照

将第 08 篇场景另存为 `CourtyardTaReview.unity`：

1. 保留第 08 篇的默认效果作为参照，在测试副本中把 Driver 的 impactPrefab 指向 `VFX_Impact_TA.prefab` 的根 Particle System。
2. 固定相机、背景、粒子 Start Color 与时间来源，分别比较两份新材质，避免一次改动太多变量。
3. 用新的公开参数调整透明度和亮度，保存材质，再重新进入场景确认引用。
4. 重复正常播放、释放前后取消、F 停止发射、Driver 失活和连续请求；材质变化不应让生命周期约定悄悄失效。
5. 用动画事件路径再检查粒子 Alpha 淡出、颜色和释放时点；有完整角色后再补移动、装备和镜头变化。
6. 在 Player 与目标设备中观察支持情况、画面和成本，记录实际条件。

Shader Graph 预览球可以检查局部颜色，却不会自动提供真实粒子顶点色、空间、排序和生命周期。静态检查器也没有运行这些过程。三种验证要分别保留结果。

### 质量分档先保留信息，再删减辅助内容

作为待验证的分档练习，可以复制一个 `VFX_Impact_TA_Low.prefab`，保留根圆环，关闭辅助 ImpactSparks，再与完整副本比较。

这样做尝试保留目标反馈的主要位置和节奏，减少辅助层。但它不自动证明低档设备够用，也不保证减少了所有瓶颈。若成本主要来自大面积圆环覆盖，删掉小碎光未必解决关键问题。

不要为了降档删掉根系统或改掉对外挂接方式，让客户端必须猜测不同结构。分档应尽量维持相同的调用与结束接口；需要改变时，明确版本和接入差异。

实际采用哪种档位、阈值和平台范围，要由视觉、程序、产品与测试等相关负责人按项目职责共同确认。本篇没有设备预算数据，不预填通用粒子数量、材质数量或帧率指标。

## 规则、例外和维护责任怎样一起交付

工具把规则执行得更一致，也会把错误规则执行得更一致。因此规则应有适用范围和例外机制，不能只保留一份永远不变的数字表。

### 将静态事实与运行目标分开

<div role="region" aria-label="静态检查与运行判断的边界，可横向查看" tabindex="0" style="overflow-x:auto">

| 问题 | 本篇工具能判断什么 | 还需要什么 |
| --- | --- | --- |
| 粒子是否循环 | 读取当前 Prefab 的 Looping 字段 | 运行代码是否改写、是否有其他循环内容 |
| 根缩放是否为 1 | 读取资产根 Transform | 实例祖先、运行缩放和实际尺寸 |
| 是否引用主材质 | 当前引用是否为空 | Shader、纹理、图形 API 与画面验证 |
| 角色受击后是否残留 | 本脚本不能判断 | 实际动作取消与效果拥有者走查 |
| 目标反馈是否清楚 | 本脚本不能判断 | 固定镜头、背景和视觉比较 |
| 性能是否达标 | 本脚本不能判断 | 指定设备、场景、质量档与测量 |

</div>

若要在构建中设门禁，先定义规则严重级别、失败条件、复现输入和批准例外的人，再让工具程序或构建维护者接入。一次菜单扫描不会自动成为全项目准入系统。资源规范的更广范围见[资源规格与准入]({{< relref "delivery-engineering/delivery-content-pipeline-03-asset-standards.md" >}})。

### 例外要有理由和复查条件

持续光环可以需要循环，但不能直接绕过本例一次性 Prefab 的约定。应为它采用另一种契约和检查范围，说明谁停止它，以及怎样验证退出。

例外至少记录：资产与规则编号、偏离原因、补充验证、影响范围、批准者和失效/复查条件。Shader 或驱动升级后，旧例外是否仍成立，需要重新检查。

这不是要求每次调色都走复杂审批，而是避免一种含糊状态：工具报错，所有人都说可以忽略，却没人知道为什么。

### 参数接口变化，要考虑旧内容怎样继续使用

把 Opacity 默认值从 1 改为 0.5，新增一个可选属性，或将输入从 RGBA Alpha 改成 R 通道，影响并不相同。尤其是 Reference、采样通道、混合方式与输出语义变化，应明确为接口变更，检查全部引用内容。

迁移时先保留旧模板或制作可比较副本，记录新旧属性映射，再更新材质和 Prefab；不要用批量替换 Shader 的一次点击，宣称所有画面已经保持一致。

检查器规则同样需要版本。某一版本的 issues=0，只说明那一版规则没有发现相应问题，不代表以后增加规则也无需重验。

## 让协作责任落到具体产物，而不是一个岗位标签

本例可以这样分配讨论和交付对象，具体归属仍由团队决定：

- 美术说明目标观感、参数使用习惯、冷暖版本与对照效果。
- TA 组织方案比较、图原型、参数语义、示例预设和静态规则。
- 图形程序协助确认渲染接口、平台能力、特殊材质功能与底层问题。
- 工具程序协助将小检查器整理成可维护工具，处理工程集成与批量工作流。
- 客户端负责按约定消费 Prefab，关联动作实例、生命周期、运行参数和加载释放。
- 测试与相应负责人共同确认运行、设备和例外范围，推动问题回到可修复的输入。

这不意味着每项都必须由一个独立岗位完成。重点是视觉、工具、运行和验收各环节都有明确负责人，出了问题能追到数据与决策。

### 交付包要包括实现、说明和验证入口

本例建议保留：

```text
Assets/Art/Effects/CourtyardCast/
  Textures/cast_ring_rgba.png
  Shaders/SG_CourtyardRing.shadergraph
  Materials/M_CourtyardRing_Cyan.mat
  Materials/M_CourtyardRing_Warm.mat
  Prefabs/VFX_Impact_TA.prefab
  Prefabs/VFX_Impact_TA_Low.prefab
Assets/Editor/ArtProduction/
  CourtyardVfxInspector.cs
Assets/Scenes/ArtProduction/Courtyard/
  CourtyardTaReview.unity
  courtyard-ta-handoff.md
```

连同源 PSD、原始效果依赖、`.meta`、Packages 版本和必要 ProjectSettings 一起按工程约定保存；本例 Shader Graph 不经过 FBX 导出，它直接成为 Unity 工程中的图资产，再被材质和 Prefab 引用。

handoff 中写清：适用效果、支持与未支持能力、四个属性的语义、规则 TA001—TA006、工具入口、预览/运行检查步骤、维护者和已知限制。帮助另一位使用者找到修复位置，比只交一个工具按钮更重要。

### 用一次反馈判断问题应该回到哪里

假设美术说“低档版本仍太亮，循环检查通过但受击后会残留”：

1. 亮度先在固定画面中区分 Tint、Intensity、粒子颜色、曝光与辅助层，不立即认定是 Shader 算错。
2. 如果公开参数不足以控制目标观感，回到参数语义与模板设计；如果只是配色不合适，修改对应材质或预设。
3. Looping 静态通过没有覆盖动作取消。残留应回查效果拥有者、旧计时、子内容和退出通知，再修改接入代码。
4. 若确有可静态发现的重复错误，再扩展检查规则，并为正反输入增加验证；不要让检查器承担无法从资产字段推断的运行目标。
5. 更新说明与版本，重新比较相关画面、取消和设备结果。

这条回流也体现 TA 的维护责任：产物投入使用后，问题不是永远被转交给另一个人，而是按实际原因修复实现、规则、说明或运行接口。

## 本篇交付到哪里

本文从目标反馈复用出发，说明了方案选择、Shader Graph 参数原型、只读 Prefab 检查器、创作者试用、质量分档和版本维护。它给出的是一条可以复查的工作方案，不是已经投产的 TA 工具平台。

实际图、材质、工具工程、编译、正反输入、团队试用与设备结果仍待补齐。图和网页检查不能代替这些证据，静态零告警也不能替代运行验收。

<!-- DATA-TODO: 制作 SG_CourtyardRing、两份材质与 VFX_Impact_TA/Low，记录 Unity/URP/Shader Graph 实际版本，补节点连接、属性面板、顶点流和同视角材质对照。 -->
<!-- DATA-TODO: 在 Editor 程序集中编译 CourtyardVfxInspector，按 TA001—TA006 制作正反输入，记录层级定位、非 Prefab 输入及 includeInactive 行为；不以文档构建代表工具运行通过。 -->
<!-- DATA-TODO: 由其他使用者按 handoff 调整参数并记录卡点；在 CourtyardTaReview/Player/目标设备执行播放、取消、结束及分档比较，补实际成本、限制和维护记录。 -->

**TA 的产物要让创作者能够使用，让运行侧能够接入，也让后续问题能够定位和修复。**

下一篇进入[第 10 篇客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})，把模型、材质、动画和特效接成可以驱动与清理的运行表现。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
