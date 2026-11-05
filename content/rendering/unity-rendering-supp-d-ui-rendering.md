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

<a id="ui-coverage-diagnostics"></a>

## Overdraw：覆盖、执行、写回与诊断图分开看

前面的丙只统一纹理，仍保留四层覆盖。它提示我们：批次减少后，像素相关工作未必减少。反过来，画面看起来透明，也不能据此判断没有工作，或每一层必定写回。

### 先说清所数的是哪一层

| 口径 | 在回答什么 | 为什么不能换成另一项 |
| --- | --- | --- |
| 几何覆盖与重叠 | 在指定目标、采样点与 Pass 下，哪些图形覆盖该位置 | 可能随后被剔除、裁剪、测试或丢弃，不等于全部执行/写回 |
| 片元着色工作 | 哪些片元着色器工作实际发生 | 测试、丢弃、硬件执行方式和工具定义影响计数，不能仅数 UI 节点推定 |
| 颜色/深度写回 | 哪些结果进入对应附件，是否经过混合 | 不写深度不代表不做深度测试；不改变最终颜色也不证明前面没有工作 |
| GPU 时间 | 在确定支持范围、窗口与条件下执行花了多久 | 材质、分辨率、带宽等会改变代价，覆盖层数不能直接换毫秒 |

Unity 6.0 的 [ZTest](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/shaders/reference/sl-reference/sl-commands/sl-ztest)、[ZWrite](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/shaders/reference/sl-reference/sl-commands/sl-zwrite) 与 [Blend](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/shaders/reference/sl-reference/sl-commands/sl-blend) 分别控制深度测试、深度写入与混合。透明混合不能直接推出“没有深度测试”；是否有效剔除要看实际 Pass 状态、目标和已有深度，混合也可能影响 GPU 的优化机会。本节没有核验项目的实际 Shader，不指定一套适用于所有 Canvas 的深度状态。

同样，在普通 Alpha 合成的题设下，输出 Alpha 为零可以不改变目标颜色，但它不等于命令、几何或着色工作已被跳过。[HLSL `clip`](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-clip) 是按条件丢弃当前像素的另一种语义；用了它也不能推出整个着色过程没有代价。实际执行与写回需匹配状态和工具证据，不能凭最终透明像素反推。这里只说明边界，没有运行 Shader 或硬件计数器。

### 为什么白色诊断图不能固定换算层数

先区分观察的工具。上述固定 uGUI 快照的 [UI Profiler 文档](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Documentation~/ProfilerUI.md)列出 Standard、Overdraw、Composite Overdraw 预览模式，但没有提供旧文所称“灰色一次、白色十次”的通用标尺。Scene View、这个 Editor 预览以及自制计数 Pass 也不能只因名称相似就共用数值含义；该 UI Profiler 的 Editor-only 限制仍沿用前节。

用一个**纯教学映射，非 Unity 模式实现**说明问题。假定单通道诊断值从零开始，每覆盖一层累加固定值 d，最后在 1 饱和：`D(L) = min(1, d × L)`。L 是给定的纸面层数，不是本轮实测；题设不引入色彩转换和其他处理。

| 给定层数 L | d = 0.25 时的 D | 改用 d = 0.125 时的 D |
| --- | --- | --- |
| 0 | 0 | 0 |
| 1 | 0.25 | 0.125 |
| 2 | 0.5 | 0.25 |
| 4 | 1 | 0.5 |
| 8 | 1 | 1 |

第一种映射中，4 层和 8 层都是 1，已经不能区分；换 d 后，同为 4 层的数值也改变了。这个反例不规定实际工具的色阶，更不建议直接修改工具参数。它说明读图前应确认：所数对象、计层条件、映射和饱和范围。未知映射或已经饱和时，保留定量层数未知；即使计层已校准，也不能把诊断 Pass 的时间当作原 UI 渲染时间。

### 改几何和叠层时，先守住哪些条件

以下 uGUI 实现继续限定为 2.5.0 的同一公开快照。[Image.useSpriteMesh 的说明](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs#L646-L659)要求同时检查导入的 Sprite 几何；[OnPopulateMesh](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs#L883-L908)中，Simple 根据该开关选择简单四边形或 Sprite 几何，Sliced、Tiled、Filled 则进入各自生成路径。因此，单改导入设置为 Tight 不能证明当前 Image 已采用紧贴轮廓的网格；这个开关也不能直接套到所有 Image Type。

`GenerateSimpleSprite` 构造四边形，而 `GenerateSprite` 读取 Sprite 的顶点和三角形，具体见[两条生成路径](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs#L1071-L1121)。紧贴轮廓可能减少无用覆盖，同时改变顶点量和生成工作；轮廓、缩放、边缘与显示要求必须保留，不能把几何更复杂就一律判为更快。

| 候选来源 | 先查什么 | 改选条件与待验证项 |
| --- | --- | --- |
| 全屏半透明底图 | 必须透露哪些下层信息，实际覆盖和混合状态是什么 | 需要透景就保留合成语义；仅改 Alpha 为 1 不证明下层提交已停止或材质路径已改变 |
| 多层面板 | 下层是否仍提交，是否完全被遮挡且允许暂停呈现 | 可考虑停止不需要的提交；输入、更新和驻留另核，不能把隐藏当释放 |
| 文字阴影/描边 | 顶点扩增、覆盖、材质与必要可读性 | 该包 Shadow/Outline 修改顶点流，不等同于额外独立 Mesh 或固定新增 DrawCall；换 TMP 另作同效果比较 |
| 不规则 Sprite 的透明区域 | 当前 Image Type、useSpriteMesh、实际导入几何及 Shader 条件 | 同画面比较几何/覆盖和处理代价，不能由矩形碰撞区或 Tight 选项直接推渲染结果 |

顶点修改的直接依据见 [Shadow](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/VertexModifiers/Shadow.cs#L91-L142) 与 [Outline](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/VertexModifiers/Outline.cs#L15-L45)。这里没有取得 TMP、实际提交或性能收益证据，也不能预先认定全屏底图一定是最大热点。

未来观察保留同一任务/帧、Canvas/目标/Pass、分辨率、实际几何/材质/状态、诊断模式及其计数定义。诊断图帮助提出“大覆盖、必要/多余叠层、几何路径”等候选解释；原渲染的时间、正确性与采集支持另查，不支持或映射未知时不填零。

**理解检查**：教学图值为 1 时为什么分不清 4 层和 8 层？Alpha 为零能否证明不做着色？Simple 与 Sliced 设置相同 useSpriteMesh 后为何不能直接认定几何相同？减少叠层或修改轮廓时，应保留什么画面证据？

---

<a id="ui-lifecycle-observation"></a>

## 从首开到再开：关闭界面之后，还剩下什么

同一背包第一次打开慢，关掉后再次打开快，可能只是实例、字体或图集仍在；画面消失后，数据订阅也可能继续写入。要判断成本转移到哪里，不能只记录一个“已关闭”状态。先把五件事分开：是否绘制、谁还更新、是否参与布局、是否接收输入，以及谁持有实例与资源。

### 隐藏、停更、移出布局和回池不是同一步

以下实现说明仍围绕屏幕空间 uGUI；公开包资料沿前节 2.5.0 快照，GameObject 契约另按 Unity 6.0。它不替 World Space 或 UI Toolkit 定义启停规则。

| 动作或状态 | 能说明什么 | 还不能据此说明什么 |
| --- | --- | --- |
| CanvasGroup 的 Alpha 为 0 | 该组透明度变化 | 不证明布局/业务更新停止、输入禁用或资源释放；Interactable、Block Raycasts 是另外的设置 |
| 关闭目标 Canvas 组件 | 官方优化指南说明该 Canvas 停止发出绘制调用 | 不等于子组件停更，不保证子 Canvas、射线输入或外部订阅一起关闭；需逐个核对实际范围 |
| 停止数据订阅、计时器或动效 | 指定生产者不再驱动更新，须有对应确认 | 不等于已有画面隐藏、所有在途结果失效或布局对象移除 |
| 从某布局组的候选子项中排除 | 该布局组计算时不再把它作为子项 | 不等于停止渲染、业务更新或所有其他布局控制；ignoreLayout 的实际组合条件须核对 |
| GameObject 在层级中变为非激活 | Unity 停用其组件，不再调用其脚本的 Update，并停止附着协程 | 不销毁对象或自动卸载资源；其他活跃对象、服务持有的回调仍须收尾 |
| 回池，或撤销本任务的资源持有 | 前者把实例交回复用管理；后者解除指定持有责任 | 回池可保留实例和资源；引用解除、实际卸载和系统占用下降仍是不同事件 |

[CanvasGroup 文档](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Documentation~/class-CanvasGroup.md)分别定义 Alpha、交互和射线设置。[Unity UI 优化指南](https://unity.com/how-to/unity-ui-optimization-tips)的“How to hide a Canvas”也提醒关闭 Canvas 后另查子组件的逐帧代码；该网页没有绑定本包版本，不能把其中的保留几何建议扩成“重开必定零重建、零准备”。停用对象则按 [SetActive 契约](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/gameobject/setactive)检查 activeInHierarchy 的实际变化，不能只看 activeSelf。

布局边界还可以直接核对该快照的 [LayoutGroup.CalculateLayoutInputHorizontal](https://github.com/Unity-Technologies/uGUI/blob/e51e5200261043c95f5b761c4740abf238e22f38/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs#L50-L82)：它先排除层级非激活子项，再检查 ILayoutIgnorer；存在报告“不忽略”的实现时仍可纳入。不要把给某组件勾选 ignoreLayout 视为所有布局路径都已退出。资源端复用[内存记账]({{< relref "performance/game-performance-memory-accounting.md" >}})；[Addressables 1.21 内存说明](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/MemoryManagement.html)明确引用释放不等于即时卸载，这不是建议在每次关闭时调用一次全局回收。

### 给各阶段一个起点、反馈和收尾记录

沿[资源就绪链]({{< relref "performance/game-performance-read-does-not-mean-ready.md" >}})定义用途；以下是后续测量的设计，尚未执行。任务/请求代次、页面与实例身份连接事件，再把帧、更新队列、渲染记录和持有记录关联回来，不能把同帧所有工作都归为该页面独占成本。

```text
请求 → 必要内容准备 → 必要画面与约定输入反馈共同满足 → 展示/更新
  └─ 失败、超时或用途过期 → 不再交接旧结果 → 按契约完成/取消与清理
展示/更新 → 关闭请求 → 画面/输入退出确认 → 停更、回池或退役分别确认
保留实例或资源 → 再开新代次；仍重新确认数据、必要画面与输入反馈
```

| 阶段 | 先固定的条件与起点 | 必要观测与异常记录 |
| --- | --- | --- |
| 首次打开 | 指明是进程内首次、首次创建还是首次使用某资源；记录实例/图集/字体持有和已知缓存状态 | 从请求到必要画面及约定操作反馈；准备、绑定、布局/图形处理是候选解释，资源回调不代反馈，磁盘缓存未知就保留未知 |
| 重开 | 新请求代次；注明复用同一实例还是重建、数据是否失效、资源是否保留 | 再验必要反馈、重绑定与准备工作；不能把它与首次混成一个平均值，也不能把同一运行里的多次打开算成独立运行 |
| 切页 | 固定源页/目标页、交叠规则和输入路由；记录旧页退出与新页请求 | 分别记录新页反馈、旧页停更/持有；交接峰值按同一时刻记共享资源，旧页未退不等于新页独有成本 |
| 滚动 | 固定数据集、可见项、路径/速度、缓存及复用条件，以滚动请求为起点 | 保留连续窗口及项目约定的卡顿指标、绑定/布局/文本事件；复用项身份与数据代次分开，漏样和空白/错图不能被均值抹去 |
| 关闭与再开 | 记录关闭请求、输入/画面退出条件、保留策略和下一次请求 | 分别确认绘制、生产者、布局、池和资源持有；看关闭后及再开窗口，保留重置失败、迟到回调与未完成清理 |

### 四条教学记录：画面出现不等于任务完成

下表是**虚构教学记录，不是设备采样或建议阈值**。同一背包任务的每次请求均以 R=0 重新计时，单位 ms，使用同一时钟。V 为必要画面首次满足，I 为约定测试操作首次产生正确反馈；题设规定画面从 V 起持续满足，且没有其他必要条件，故已完成行的任务耗时为 `T = max(V, I) - R`。若画面随后失效或任务还有别的必要条件，不能继续套这个式子。

| 请求 | 本次条件 | V | I | 记录状态 | 能得到什么 |
| --- | --- | --- | --- | --- | --- |
| P1 | 本运行首次创建，页面资源未由本任务持有；磁盘缓存未知 | 90 | 120 | 完成 | T = 120；不能归因为纯读盘 |
| P2 | 同一运行重开，同实例及资源保留，数据仍有效 | 8 | 15 | 完成 | T = 15；条件与 P1 不同，不能据差值宣称某优化收益 |
| P3 | 重开，输入反馈异常；连续观察至题设截止 50 | 12 | 截止仍未发生 | 超时 | 该次任务未在 50 内完成，最终耗时未知；不能写成 12 或精确的 50 |
| P4 | 重开，V 后输入反馈记录中断 | 10 | 缺失 | 证据不足 | T 未知；既不能记 0，也不能仅因没有记录就判超时 |

P1/P2 的差值为 105 ms，只是两条不同条件记录的差，不能当重复/对照或因果证据。P3/P4 都保留在请求清单中，状态原因分开，不从“成功请求平均耗时”里静默消失。

如果 P7 已关闭，P8 复用同一实例后才收到 P7 的旧图片结果，只检查实例 ID 就不够。应把结果与当前数据/请求代次匹配，在受控交接边界确认用途仍有效；过期结果不再覆盖 P8，按实际加载契约清理自己的持有。这里复用资源就绪链的设计，不把“业务不接收”写成底层操作已取消。

选择保留实例/资源，是用驻留和失效管理换后续准备机会；选择退役，是减少持有但承担下次准备、重绑及反馈延迟的候选代价。先按高频入口、必要反馈、持有预算和再开窗口选方案，再作可比验证；本节没有给出哪种策略在目标设备上更快。

**理解检查**：只关 Canvas 后，哪几种工作仍须分别查？为什么 P2 不证明首开已经改善，P3 不能写成 12 ms，而 P4 不能直接算超时？关闭 P7 后复用同一实例，怎样避免迟到结果覆盖 P8，谁负责释放它的持有？

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
| 全屏半透明底图 | 定义明确的覆盖诊断、原渲染记录 | 保留必要透景，核对下层实际提交及同效果代价 |
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
- **像素相关成本** 要分开覆盖、着色、写回与时间；诊断图先查计数和映射，叠层改动保留必要画面

这些是排查与候选改法，收益须由目标任务的可比记录和必要反馈验证。本文没有设备对照数据，不能给出统一提升比例或据此选择 UI 框架。
