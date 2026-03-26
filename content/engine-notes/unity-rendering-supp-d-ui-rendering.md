+++
title = "Unity 渲染系统补D｜UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas"
slug = "unity-rendering-supp-d-ui-rendering"
date = 2026-03-26
description = "Unity 的 UI 渲染（uGUI/UI Toolkit）有自己独立的合批逻辑。Canvas 合批依赖几何排序和材质一致性；Rebuild 是 CPU 性能的主要杀手；Overdraw 是 GPU 性能的主要杀手。这篇系统讲清楚这三个问题的原因和解法。"
weight = 1530
[taxonomies]
tags = ["Unity", "Rendering", "UI", "uGUI", "Canvas", "性能优化", "Overdraw"]
[extra]
series = "Unity 渲染系统"
+++

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

## Rebuild 机制：CPU 性能的主要杀手

### 什么是 Rebuild

每当 Canvas 内某个 UI 元素发生变化，Canvas 需要**重新生成 Mesh**，这个过程叫 **Rebuild**。Rebuild 分两种：

#### Layout Rebuild（布局重建）

触发条件：元素的层级结构、尺寸、位置、RectTransform 变化。

Layout Rebuild 会重新计算所有 `LayoutGroup`（`HorizontalLayoutGroup`、`VerticalLayoutGroup`、`GridLayoutGroup`）和 `ContentSizeFitter`。如果层级很深，一个子元素改变尺寸会触发整条链路的重算。

#### Graphic Rebuild（图形重建）

触发条件：颜色、文字内容、UV 坐标变化。

`Text` 组件改变文字内容时触发 Graphic Rebuild，需要重新生成文字 Mesh（逐字符生成四边形，计算字形 UV）。**频繁更新的 Text（如帧率显示、血量数字）每帧都在触发 Rebuild**，是最常见的 CPU 热点。

### WillRenderCanvases 回调

Rebuild 发生在 Unity 的 `WillRenderCanvases` 事件中（渲染前的最后一步）。可以通过 Profiler 的 `Canvas.BuildBatch` 和 `Canvas.SendWillRenderCanvases` 条目来定位 Rebuild 开销：

```
Canvas.BuildBatch        → Mesh 重建耗时
Canvas.SendWillRenderCanvases → Layout/Graphic Rebuild 耗时
```

### 减少 Rebuild 的核心策略

1. **分离 Dynamic Canvas**：把频繁变化的元素（血量条、计时器）放在独立的小 Canvas 上，避免污染整个大 Canvas 的 Rebuild
2. **避免频繁改 Text.text**：改用 `TextMeshPro`，其 Mesh 生成更高效；或者用数字图片替换纯文字
3. **禁用不可见元素的 Canvas**：用 `canvas.enabled = false` 而不是 `gameObject.SetActive(false)`（后者会触发完整的 OnDisable/OnEnable）
4. **避免在 Update 里每帧改 color**：即使颜色相同也会标脏触发 Rebuild

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

这样 DynamicCanvas 频繁 Rebuild 时，StaticCanvas 的 Mesh 不会受影响。

每个 Canvas 是一个独立的 Rebuild 单元，也是一个独立的合批单元。拆分过细（几十个 Canvas）会增加 Draw Call 和 Rebuild 管理开销，拆分原则是**按更新频率分层，而不是按功能分层**。

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
| Rebuild | Canvas.BuildBatch | UIR（UI Renderer）增量更新 |
| 动态 UI 性能 | 频繁 Rebuild 开销大 | 增量更新，理论上更高效 |
| 运行时成熟度 | 成熟，生态完整 | Unity 2022~2023 逐步完善 |

UI Toolkit 的 `UIR（UI Renderer）` 采用增量更新模型：只有发生变化的 VisualElement 子树才重新生成几何，理论上比 uGUI 的整 Canvas Rebuild 更高效。但当前版本（Unity 6）在某些复杂布局场景下仍存在性能问题，迁移决策需要实测对比。

---

## 实践优化清单

| 检查项 | 工具 | 目标 |
|-------|-----|-----|
| Draw Call 数量 | Frame Debugger | UI Draw Call < 10（简单 HUD） |
| Canvas.BuildBatch 耗时 | Profiler | < 0.5ms/帧 |
| 全屏半透明遮罩 | Scene View Overdraw | 消除或改为不透明 |
| 频繁更新的 Text | Profiler → GC Alloc | 换用 TextMeshPro |
| Sprite Atlas 覆盖率 | Sprite Atlas Inspector | 主 UI 图集 >90% 覆盖 |
| Dynamic Canvas 隔离 | 人工审查 | 血量/计时器独立 Canvas |
| Mask 数量 | Frame Debugger | 尽量用 RectMask2D 替代 Mask |
| World Space Canvas 数量 | Profiler | 按需 Enable/Disable |

---

## 小结

uGUI 的性能问题高度集中：

- **合批被打断** 的根本原因是纹理不一致（用 Sprite Atlas 解决）和 Depth 顺序交叉（用调整层级解决）
- **Rebuild** 的根本原因是频繁更新内容与静态内容共享同一 Canvas（用 Canvas 分层解决）
- **Overdraw** 的根本原因是不必要的透明层叠（全屏半透明背景是首要目标）

解决这三个问题，UI 渲染性能通常可以提升 50% 以上。UI Toolkit 代表未来方向，但 uGUI 在运行时场景仍然是当前主流。
