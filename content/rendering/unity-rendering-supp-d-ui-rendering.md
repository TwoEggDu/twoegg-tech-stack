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

<a id="ui-state-order-atlas-teaching"></a>

## Canvas 合批：先对齐状态，再检查顺序约束

把二十个图标放进同一图集，为什么批次仍然不止一个？图集解决的是纹理组织的一部分问题，不能消除材质、裁剪、Canvas 和画面顺序的约束。应先判断哪些元素允许一起提交，再核对实际引擎产生了什么。

> 本节 uGUI 映射沿用公开包 2.5.0、固定快照 `e51e5200261043c95f5b761c4740abf238e22f38`；包声明与源码范围见下文 Rebuild 节。批处理字段取该快照的 [UI Profiler 文档](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Documentation~/ProfilerUI.md)。Sprite Atlas 配置另按 Unity 6.0 手册核对，不视为同一运行环境已验证；以下没有运行原生合批器、帧捕获或设备计时。

### 材质名和图集名为什么不够

| 候选检查 | 它在区分什么 | 不能直接下的结论 |
| --- | --- | --- |
| Canvas、渲染目标/Pass 与共平面条件 | 是否属于同一待比较的提交范围 | 视觉上挨着就能合批，或跨 Canvas 自动合并 |
| 实际材质、纹理及 Alpha 使用 | 提交给渲染器的状态是否相容 | Inspector 材质名相同、同一图集资源名就必然一批 |
| 裁剪矩形、Stencil 与层级约束 | 可见区域及绘制顺序能否保持 | 每加一个 Mask 或 RectMask2D 就固定增加一批 |

公开实现给了两个可核对的入口：`Graphic.materialForRendering` 会经过 `IMaterialModifier`，实际使用的材质未必就是 Inspector 中原始的那份；有 Sprite 的 `Image.mainTexture` 返回实际 Sprite 的纹理。应比较有效状态及对象身份，不能只比较资源名称。

包文档把 Different Material Instance、Rect clipping、Texture、A8TextureUsage 和非共平面等列为可能的 Batch Breaking Reason。这个列表帮助提出解释，不是“每个条件必多一次 DrawCall”的算式。`Mask` 使用 Stencil；`RectMask2D` 在其适用条件下不使用 Stencil，也不自行增加遮罩绘制调用，但它的裁剪状态仍需与其他元素对齐。后者限定 2D/共平面矩形裁剪，不能为了批次替换任意形状遮罩。

### 教学反例：ABAB 不是实际批次数公式

以下是**纸面模型，不是 Unity 的批处理算法或采样结果**。四个 UI 四边形依次记为 A1、B1、A2、B2；A 采样纯红纹理，B 采样纯蓝纹理，输出 Alpha 均为 0.5，背景为不透明黑色。颜色按抽象线性数值作普通 Alpha 合成，不引入 sRGB 转换或其他混合方式。固定一个 Canvas、一个目标和一个 Pass，共平面、同一材质，其他状态一致，无裁剪和额外绘制。目标只有 2×2 共四个格点，每个四边形在覆盖的格点上恰好计一层。

模型把**连续使用同一纹理的序列段**记为一段。例如 ABAB 为 4 段，AABB 为 2 段；这不是 Batch Count 或 Mesh 数。只在保持画面所需顺序的排列中，寻找最少的序列段，检验“按纹理分组”是否可行。

| 题设 | 覆盖与必须保持的关系 | 纸面纹理序列及最少段数 | 覆盖格点·层 |
| --- | --- | --- | --- |
| 甲：四个四边形都覆盖全目标 | 每格都需保留 A1→B1→A2→B2 的顺序 | ABAB；4 段 | 16 |
| 乙：四个四边形各占不同一格 | 四者没有共同覆盖格点，题设无其他顺序依赖 | ABAB 可排为 AABB；2 段 | 4 |
| 丙：甲的两个纹理区域放入同一纹理 T | 仍按原顺序绘制，UV 保留原色与 Alpha，其他状态不变 | TTTT；1 段 | 16 |

甲为何不能直接排成 AABB？按 `新色 × 0.5 + 旧色 × 0.5` 逐层合成，任一格的红/蓝分量为：

```text
ABAB： (R,B) = (0,0) → (0.5,0) → (0.25,0.5)
                    → (0.625,0.25) → (0.3125,0.625)
AABB： (R,B) = (0,0) → (0.5,0) → (0.75,0)
                    → (0.375,0.5) → (0.1875,0.75)
```

两种结果不同。减少纸面纹理段数却改变叠层画面，不是通过优化。乙说明没有重叠时，层级列表的 ABAB 本身不能证明四段都不可合并；**本模型不保证 Unity 会选择这个重排或得到两批**。甲/乙覆盖和画面任务不同，也不能当作同条件性能 A/B。

丙保留甲的显示顺序，同时减少模型中的纹理切换；覆盖仍为 16 格点·层。因而“纹理段更少”不证明透明像素工作减少或 GPU 时间下降。这些格点·层只是教学覆盖计数，实际光栅化、Shader、缓存、带宽、额外 Pass 和提交结果仍需观察。同理，统一纹理不能据此推导业务更新、布局请求或下文定义的队列项·轮减少。

### 从纸面模型回到实际观测

未来对比时，记录同一帧/任务、Canvas、实际纹理和材质身份、裁剪状态、参与对象、必要画面顺序及对应提交。先定位批次变化由哪项状态或约束造成，再分别检查 CPU 处理、GPU 工作与必要反馈；只缩短层级列表或降低一个总计数不能完成归因。

一个重要支持边界是：**上述快照文档明确说明 UI (Canvas)/UI Details (Canvas) Profiler 模块只收集 Editor 数据，不适用于 Development Build。** 其中 Self Batch Count 与包含嵌套 Canvas 的 Cumulative Batch Count 口径不同，不能把父累计数再加子数。可以把该模块用作 Editor 内的解释线索，不能把这些字段预设成已可采集的真机数据。Player 平台的证据来源和支持范围在测量阶段另定，缺失保留未知。

**理解检查**：甲减少到 AABB 为什么不被接受？乙能否据此承诺两批？丙为何保留 16 而不是变成 4？若保持图集名却改变裁剪状态，原先的推断哪部分失效？拿不到 Player 字段时怎样表达，而不是填零？

本节直接实现依据：[最终材质入口](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs#L506-L534)、[Image 实际纹理](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs#L687-L702)、[Mask](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Documentation~/script-Mask.md) 与 [RectMask2D](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Documentation~/script-RectMask2D.md) 文档；它们不提供本教学表的原生批次数或收益证明。

---

## Sprite Atlas 与文本：状态统一和资源准备是两件事

### 图集能统一纹理，不能保证整页一批

把多个 Sprite 图像区域组织进同一实际纹理，可以减少纹理状态差异，丙就是这种候选变化。要获得可比较结果，还需确认材质、裁剪、Canvas/Pass、顺序、实际绑定纹理及最终画面。不能由“二十个图标在同一图集”直接宣布二十批变一批。

Unity 6.0 的 [Sprite Atlas 说明](https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/sprite/atlas/atlas-introduction)建议按同时使用、压缩要求以及使用频率组织资源，避免加载不用的 Sprite，或因数量过大产生多个组合纹理。因此，不能为了统一图集名就把不同时使用的所有界面塞进一张大图；首开准备、驻留和使用期间的提交应一起比较。

### Master、Variant 与加载边界

按 Unity 6.0 的 [Inspector 参考](https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/sprite/atlas/sprite-atlas-reference)，Type 字段是 Master 或 Variant，不能把 Packed 当作该字段的选项。Variant 引用 Master 并设置缩放比例；它是资源候选，不证明低档设备的必要文字/图标可读性已经接受。

同一参考的 `Include in Build` 与官方[运行时手动加载说明](https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/sprite/atlas/load-sprite-atlas-spriteatlasmanageratlasrequested)需要一起读：关闭自动包含后，必须另有实际交付与加载路径；`SpriteAtlasManager.atlasRequested` 是请求未加载图集时的绑定入口，不负责下载、缓存、失败恢复或释放。Resources 和 AssetBundle 的准备方式也不能混写成同一种路径。完整生命周期与缺失处理另接首开/重开/关闭协议，这里不提供未经运行的通用加载器。

### 字体纹理不等于 Sprite 图集

在同一 uGUI 快照中，旧版 [Text.mainTexture](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Text.cs#L56-L97)优先取字体材质纹理，字体纹理改变又可能使几何更新或标脏。把相邻图标放进 Sprite Atlas，不能据此声称文字也绑定了同一纹理，或字体准备不再影响首开/后续更新。应另记文本组件类型、字体/材质/实际纹理和需要的字符集合；字体缺字、纹理变化或配置切换后重查。TMP 的字体资产、回退与多材质路径没有在本节核对，不继承旧版 Text 的具体分支。

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
| Mask 与裁剪状态 | 对象/状态记录、渲染事件检查 | 先保证形状语义；矩形共平面需求再比较 RectMask2D |
| World Space Canvas 数量 | Profiler | 按需 Enable/Disable |

---

## 小结

uGUI 的性能问题高度集中：

- **合批** 要对齐有效材质/纹理、裁剪和提交范围，并保留必要顺序；图集只解决其中一部分条件
- **更新成本** 要分清写入、脏请求、布局/Graphic 队列处理和 Canvas 批处理，按实际依赖选择合并更新或缩小影响范围
- **Overdraw** 的根本原因是不必要的透明层叠（全屏半透明背景是首要目标）

这些是排查与候选改法，收益须由目标任务的可比记录和必要反馈验证。本文没有设备对照数据，不能给出统一提升比例或据此选择 UI 框架。
