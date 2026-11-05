---
title: "Unity 渲染系统补D｜UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas"
slug: "unity-rendering-supp-d-ui-rendering"
date: "2026-03-26"
description: "从 Canvas 合批、布局与图形更新、透明叠层理解 UI 成本；以版本明确的 uGUI 公开实现，区分属性变化、脏标记、队列处理与实际耗时。"
weight: 1530
tags:
  - "Unity"
  - "Rendering"
  - "UI"
  - "uGUI"
  - "Canvas"
  - "性能优化"
  - "Overdraw"
series: "Unity 渲染系统"
---
UI 渲染是很多项目里性能问题最密集的地方，却也是最容易被忽视的地方。3D 场景用了 GPU Instancing、SRP Batcher，但 UI 这边每帧 Rebuild 几十个 Canvas，帧率依然上不去。uGUI 的合批逻辑和 3D 渲染完全不同——它有自己的几何重建机制、自己的深度排序规则、自己的 Overdraw 来源。搞清楚这套逻辑，才能真正解决 UI 性能问题。

---

## Canvas 合批原理

### 合批的基本条件

uGUI 的合批发生在 **Canvas 内部**。同一个 Canvas 下，相邻的 UI 元素如果满足以下条件，会合并为一个 Draw Call（一个 Mesh）：

1. **相同材质**（Material）
2. **相同纹理**（Texture）
3. **几何深度顺序连续**（Depth 排序中没有其他不同材质的元素插在中间）

uGUI 内部按层级顺序给每个元素分配 Depth 值，然后对相邻相同材质/纹理的元素合批。

### 打断合批的条件

以下情况会打断合批，每个打断点产生一个新的 Draw Call：

| 打断原因 | 说明 |
|---------|-----|
| 不同材质 | 默认材质 vs 自定义材质，哪怕只差一个参数 |
| 不同纹理 | Image A 用纹理 A，Image B 用纹理 B，中间无法合批 |
| Mask 组件 | `Mask` 用 Stencil Buffer 实现，强制产生额外 Draw Call |
| `RectMask2D` 插入 | 虽比 Mask 轻量，但仍会在切割边界打断合批 |
| Depth 排序交叉 | 不同材质的元素在 Z 序上交替出现（ABAB 排列） |

**Depth 交叉是最隐蔽的合批杀手。** 假设 Canvas 里有：

```
深度 0: Image（纹理A）
深度 1: Image（纹理B）
深度 2: Image（纹理A）
深度 3: Image（纹理B）
```

纹理 A 和纹理 B 的元素交替出现，无法合批，产生 4 个 Draw Call。正确做法是把同纹理的元素调整为相邻顺序（00BB），只产生 2 个 Draw Call。

---

## Sprite Atlas：合并纹理避免切换

### 为什么需要 Sprite Atlas

每张不同的纹理都会打断合批。如果 UI 上有 20 个 Icon，每个 Icon 是一张独立的 512×512 纹理，就会产生至少 20 个 Draw Call（每张纹理一个）。Sprite Atlas 把多张小图打包进一张大纹理，让这 20 个 Icon 共享同一张纹理，合批成 1 个 Draw Call。

### Packed Atlas vs Variant Atlas

| 类型 | 说明 | 适用场景 |
|-----|-----|---------|
| **Packed Atlas** | 直接包含所有 Sprite 的像素数据 | 大多数情况 |
| **Variant Atlas** | 引用另一个 Master Atlas，可指定缩放比例（如 0.5x） | 低画质变体、移动端低分辨率版本 |

### Late Binding

默认情况下，引用了 Atlas 中 Sprite 的 UI 会在 Atlas 加载时自动关联。`Late Binding` 模式下，Atlas 不会在引用时立刻加载，而是在运行时通过 `SpriteAtlasManager.atlasRequested` 回调按需加载：

```csharp
void OnEnable()
{
    SpriteAtlasManager.atlasRequested += OnAtlasRequested;
}

void OnAtlasRequested(string tag, System.Action<SpriteAtlas> callback)
{
    // 从 AssetBundle 或 Addressables 加载后回调
    var atlas = Resources.Load<SpriteAtlas>(tag);
    callback(atlas);
}
```

Late Binding 与 AssetBundle 配合使用时，可以做到分包加载，避免启动时把所有图集加载进内存。

---

<a id="ui-dirty-queue-teaching"></a>

## Rebuild 机制：从数据变化到布局、图形与批次

血量在一次结算中扣了三次，界面就完整重建三次吗？要回答这个问题，先区分业务写入、更新请求和实际处理。减少其中一种次数，不自动代表另两种也按相同比例下降。

> 本节实现核对限定为 Unity 官方 uGUI 仓库的 `e51e5200261043c95f5b761c4740abf238e22f38` 快照，[包声明为 uGUI 2.5.0 / Unity 6000.5](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/package.json)。下面使用旧版组件 `UnityEngine.UI.Text`，不外推到 TextMeshPro 或 UI Toolkit。来源是公开 C# 包；没有运行 Unity、Profiler 或设备实验，也没有由此核验原生 Canvas 合批实现。其他小节的合批、图集和工具结论仍需按使用版本逐项核对。

### 先分清三个处理职责

| 职责 | 在回答什么问题 | 不能直接换算成什么 |
| --- | --- | --- |
| Layout（布局） | 输入的布局需求怎样决定受影响层级的尺寸和位置 | 每个元素都重算、整个 Canvas 的 Mesh 都重建 |
| Graphic（图形） | 某个 Graphic 的顶点或材质数据是否需要更新 | 每次入队都生成 Mesh、每个 Graphic 都是一条 DrawCall |
| Canvas 批处理 | UI 渲染数据怎样组织成可提交的批次 | 等同于前两类队列项数，或等同于 GPU 耗时 |

在这个包中，`Text.text` 从一个非空字符串改为另一个不同的非空字符串，会调用 `SetVerticesDirty()` 和 `SetLayoutDirty()`；相同的非空值会跳过这两次请求。空串/空值有另一条分支，不能套用这个规则。`Graphic.color` 也先比较 RGBA，四分量相同就不会由这个 setter 再次标脏；但构造字符串、业务判断和其他回调仍可能有成本。

Layout 请求通过 `LayoutRebuilder` 查找有效布局根，沿活跃的布局组祖先关系决定候选范围；没有有效控制器时可能不注册。它不是无条件重算全场景所有布局组。布局处理还分水平/垂直的输入计算和控制，树的依赖与参与组件会改变遍历量。Graphic 请求则登记相应 Graphic，并记录顶点或材质是否脏。非活跃、被剔除、销毁等状态也会影响后续处理。

```text
业务写属性 → setter 判断值 → 直接发出布局/顶点等脏请求
                            ├─ 找布局根 R → 布局队列去重 → 布局阶段处理
                            └─ Graphic G → 图形队列去重 → 图形阶段处理

后续还需分别观察：Canvas 批处理 → 渲染提交 → 像素工作与最终反馈
```

这里的队列由 `CanvasUpdateRegistry` 管理，不能把它理解为每个 Canvas 各有一份完全隔离的布局队列。它订阅 `Canvas.willRenderCanvases`，在一次 `PerformUpdate` 中先处理布局，再剔除和处理图形。布局根与 Graphic 的注册会去重；队列处理完会清空，后续再次改变可以进入下一轮。布局阶段也可能引出新的尺寸或图形变化，因此请求、处理与后续影响应分别观察。

### 教学记录：三次变化，何时才是三轮处理

以下是**纸面调度题设，不是引擎采样**。一个活跃的旧版 `Text` 记为 G，初值为非空的 `"100"`，其有效布局根固定为 R。各组从同一初态独立推演，开始时两个队列均无待处理项。题设只登记这个 setter 直接发出的请求；不发生字体/图集变化、尺寸改变、额外脏回调、剔除或重建期间再入队。实际布局若产生额外请求，应另行记录，不能继续照抄本表。

“处理窗口”指题设中安排的一次完整队列处理，不等同于一个显示帧；强制更新等行为可能改变实际调用边界。“队列项·轮”指某对象参加一轮完整队列处理，**不是 `Rebuild` 函数的调用次数**，因为一轮内还有多个阶段。

| 组 | 按顺序写入 | 完整处理窗口数 | setter 调用 / 实际值变化 | 直接布局请求 / 顶点请求 | 窗口内 R / G 唯一待处理项 | 累计布局 / 图形队列项·轮 |
| --- | --- | --- | --- | --- | --- | --- |
| A | `99 → 98 → 97 → 97`，均在处理前 | 1 | 4 / 3 | 3 / 3 | 1 / 1 | 1 / 1 |
| B | 只提交最终的 `97` | 1 | 1 / 1 | 1 / 1 | 1 / 1 | 1 / 1 |
| C | `99`、`98`、`97`，每次写后都完整处理 | 3 | 3 / 3 | 3 / 3 | 每窗 1 / 1 | 3 / 3 |

A 的前三次写入改变了值，但 R 和 G 已在队列里时不会重复追加；最后一次 `97` 连直接脏请求也没有。B 减少了业务写入和直接请求，表内的队列项·轮与 A 相同。因此，不能宣称 A→B 把“三次完整重建优化为一次”，更不能宣称节省了三分之二耗时；找布局根和请求回调等工作也不能因去重而算作免费。

A 与 C 的差别来自处理边界。C 在三个窗口分别处理更新，有机会支持分阶段反馈；是否真的呈现仍须显示证据。若 `99`、`98` 是必须及时传达的战斗信息，直接合并为 `97` 就改变了任务。只有允许在同一提交点展示最终值，才比较 A/B 这种合并策略。

同样，表中 `1 / 1` 不证明只遍历一次层级、只生成一个 Mesh、只提交一个批次，也不能推算毫秒。`Graphic.Rebuild` 还会依据剔除和脏状态决定是否更新几何/材质；这正是需要把队列工作与后续执行分开的原因。

### 下一步该观察什么

| 观察层 | 候选记录及边界 | 用于区分的解释 |
| --- | --- | --- |
| 业务更新 | 同一次界面/任务的写入次数、实际值变化、允许反馈的时点 | 重复赋值，还是必须显示多个中间状态 |
| 布局与图形 | 布局根/Graphic 身份、直接与派生脏请求、处理窗口及覆盖的阶段 | 请求多但队列去重，还是跨窗反复处理、依赖范围扩大 |
| 批次与提交 | 对应 Canvas、目标/Pass、批次及材质/纹理/裁剪条件 | CPU 布局变化，还是渲染状态/排序引起提交变化 |
| 时间与体验 | 同条件 CPU 事件区间、GPU 支持范围、请求到必要反馈；缺测保留未知 | 哪段执行/等待变长，必要反馈是否迟到 |

这是一份观测设计。未来采集时须固定项目/包版本、实际层级与配置、首开或稳定更新阶段，并确认来源能否提供这些字段。若使用 Profiler 中的 `Canvas.SendWillRenderCanvases`、`Canvas.BuildBatch` 等标记，先在所用版本确认含义及父子区间；不能仅凭名称就把前者当成某个 Text 的独占时间，或把后者等同于 C# Graphic 的 Mesh 生成时间。避免把父区间与子区间重复相加。队列数、DrawCall 和 GC Alloc 都只能回答各自的问题，不能互相替代。

在这些边界明确后，再考虑改法：允许合并时只提交最终值；布局依赖造成热点时缩小依赖范围；批处理范围受频繁更新影响时尝试拆分 Canvas，同时复查新增批次、布局依赖和维护成本。换成 TextMeshPro 需要对齐显示效果、字体资源和更新路径后比较，不能消除“变化怎样传播”的问题。仅隐藏渲染、停止脚本更新、移出布局与释放资源也不是同一动作，生命周期策略另验。

**理解检查**：为什么 A/B 的请求数不同而队列项·轮相同？若在每次赋值后插入完整更新，哪项数字会改变？若两个 G 共享 R，布局项与图形项怎样分别数？若 Text 被剔除或换成 TextMeshPro，哪些前提与观测需要重新核对？

本节直接来源均来自上述同一快照：

- [Text 的非空赋值分支](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Text.cs#L201-L223)；[Graphic 的颜色判断](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs#L156)及 [RGBA 比较](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/SetPropertyUtility.cs#L9-L16)。
- [脏请求入口](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs#L276-L309)、[布局根查找与注册](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs#L170-L258)，后者含 Editor 非播放分支，本题不覆盖该模式。
- [队列分阶段处理](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs#L162-L230)、[注册去重](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs#L279-L325)与 [AddUnique](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/SpecializedCollections/IndexedSet.cs#L41-L54)。
- [布局阶段的四步计算/控制](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs#L76-L90)与 [Graphic 的实际更新条件](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs#L644-L663)。

---

## Overdraw：GPU 性能的主要杀手

### 什么是 Overdraw

UI 是透明层叠渲染的——每个 UI 元素从下到上依次绘制，同一屏幕像素被绘制多次，叫 **Overdraw**。3D 场景有深度测试可以跳过遮挡像素，但 UI 是透明的，没有办法用深度测试剔除，每层都必须写入。

### 查看 Overdraw

在 Scene View 工具栏，切换到 **Overdraw 模式**（Shading Mode → Overdraw），越亮的区域 Overdraw 越严重：

- 灰色：绘制 1 次
- 较亮：绘制 3~5 次
- 白色/极亮：绘制 10 次以上

### 常见 Overdraw 来源

| 来源 | 说明 | 解法 |
|-----|-----|-----|
| 全屏半透明背景 | 一张覆盖全屏的半透明 Image | 改用不透明背景，或直接清屏颜色 |
| 多层面板叠加 | 打开新面板时旧面板仍在渲染 | 关闭被遮挡的面板的 Canvas 或 GameObject |
| 文字底部阴影 | Shadow/Outline 组件生成额外 Mesh | 改用 TextMeshPro 的内置描边（单次渲染） |
| 不规则 Sprite 填充 | 矩形碰撞区内大量透明像素 | 使用 Tight Mesh（Sprite 的 Mesh Type = Tight） |

**全屏半透明背景是最大的杀手**，一张 `alpha = 0.5` 的全屏黑色背景意味着所有屏幕像素都被额外绘制一次。

---

## Canvas 分层策略

### Static Canvas vs Dynamic Canvas

最重要的 UI 性能实践是**把不变的内容和频繁变化的内容分到不同 Canvas**：

```
RootCanvas（不动的大背景）
├── StaticCanvas（技能图标、地图框架）—— 极少 Rebuild
└── DynamicCanvas（血量、计时器、弹幕）—— 频繁 Rebuild
```

这样分层是在尝试缩小频繁变化所影响的批处理范围。布局依赖仍需单独核对：`LayoutRebuilder` 按布局组祖先关系找根，不能仅凭多了一个 Canvas 就认定布局传播也被隔离。

Canvas 拆分须同时考虑更新频率、布局依赖、排序和新增批次；不能给每个功能都套一个 Canvas 后就视为优化完成。实际隔离范围和时间收益须在目标层级与版本中验证。

### World Space Canvas 的注意事项

World Space Canvas（`Render Mode = World Space`）挂在 3D 场景中的血条、名字牌等：

- **不参与 Screen Space Canvas 的合批**，每个 World Space Canvas 至少 1 个 Draw Call
- 每帧都会触发摄像机视锥体测试（每个 Canvas 是一个独立的 Renderer）
- 尽量减少 World Space Canvas 的数量，考虑用 `Canvas.enabled` 在不可见时关闭

---

## UI Toolkit vs uGUI

Unity 6 起，**UI Toolkit** 已成为编辑器 UI 的标准，运行时 UI 也在逐渐推广。与 uGUI 的本质差异：

| 维度 | uGUI | UI Toolkit |
|-----|------|-----------|
| 结构模型 | Hierarchy（GameObject 树） | Visual Tree（VisualElement 树，不是 GameObject） |
| 样式系统 | Inspector 属性 | USS（类 CSS）样式表 |
| 合批机制 | Canvas 内 Mesh 合并 | 内部 Render Chain，自动管理批次 |
| 更新路径 | 布局/Graphic 更新与 Canvas 批处理分别观察 | UIR（UI Renderer）路径需按版本核对 |
| 动态 UI 性能 | 取决于变化范围、布局依赖与渲染工作 | 须用相同任务和反馈要求作对照 |
| 运行时成熟度 | 成熟，生态完整 | Unity 2022~2023 逐步完善 |

不能由“增量更新”推导 UI Toolkit 一定更快，也不能把 uGUI 简化成每次重建整个 Canvas。本节的 uGUI 队列模型不适用于 UI Toolkit；迁移时需另核版本、布局/样式传播、文本与渲染路径，再在同任务条件下比较。

---

## 实践优化清单

| 检查项 | 工具 | 目标 |
|-------|-----|-----|
| Draw Call 数量 | Frame Debugger | 先解释批次变化；接受线绑定任务、设备和必要画面 |
| Canvas.BuildBatch 等 CPU 区间 | Profiler | 核对版本、对象范围及父子区间，再按候选预算复验 |
| 全屏半透明遮罩 | Scene View Overdraw | 消除或改为不透明 |
| 频繁更新的 Text | 更新记录、Profiler | 分开写入/脏请求/处理与分配，不能用 GC Alloc 代替重建工作 |
| Sprite Atlas 使用 | Sprite Atlas Inspector、实际引用 | 对齐图集身份与驻留/切换条件，另行核验收益 |
| Dynamic Canvas 隔离 | 层级/布局依赖检查、Profiler | 检查频繁变化的影响范围与拆分代价 |
| Mask 数量 | Frame Debugger | 尽量用 RectMask2D 替代 Mask |
| World Space Canvas 数量 | Profiler | 按需 Enable/Disable |

---

## 小结

uGUI 的性能问题高度集中：

- **合批被打断** 的根本原因是纹理不一致（用 Sprite Atlas 解决）和 Depth 顺序交叉（用调整层级解决）
- **更新成本** 要分清写入、脏请求、布局/Graphic 队列处理和 Canvas 批处理，按实际依赖选择合并更新或缩小影响范围
- **Overdraw** 的根本原因是不必要的透明层叠（全屏半透明背景是首要目标）

这些是排查与候选改法，收益须由目标任务的可比记录和必要反馈验证。本文没有设备对照数据，不能给出统一提升比例或据此选择 UI 框架。
