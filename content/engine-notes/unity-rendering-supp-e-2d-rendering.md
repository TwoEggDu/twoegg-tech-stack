+++
title = "Unity 渲染系统补E｜2D 渲染：Sprite、Atlas、九宫格、2D 光照"
slug = "unity-rendering-supp-e-2d-rendering"
date = 2026-03-26
description = "Unity 的 2D 渲染建立在 3D 管线之上，但有一套专用系统：Sprite 的存储与切割、Sprite Atlas 的合批逻辑、九宫格（9-Slicing）的实现、2D 光照（URP 2D Renderer）的 Normal Map 与 Light 类型。"
weight = 1540
[taxonomies]
tags = ["Unity", "Rendering", "2D", "Sprite", "Atlas", "九宫格", "2D光照"]
[extra]
series = "Unity 渲染系统"
+++

Unity 的 2D 渲染从来不是一个独立的管线，它是建在 3D 管线之上的一层抽象。每个 Sprite 最终都是一个带纹理的三角形网格，每盏 2D 灯光最终都是一个着色器 Pass。理解这一点，才能解释 2D 开发中很多"为什么这样做"的问题——为什么要用 Sprite Atlas、为什么九宫格能节省纹理内存、为什么 2D 法线贴图的 Z 轴方向和 3D 里不一样。

---

## Sprite 本质：纹理上的矩形区域

### 存储结构

一个 `Sprite` 资产不是一张独立的纹理，而是对某张 `Texture2D` 的一个**区域描述**：

- **纹理引用**：指向底层 `Texture2D`
- **Rect**：在纹理上的像素矩形区域（x, y, width, height）
- **UV 坐标**：从像素坐标换算得来的归一化 UV
- **轴心（Pivot）**：旋转和缩放的基准点，默认中心（0.5, 0.5）
- **Pixels Per Unit（PPU）**：1 个 Unity 单位对应多少像素，影响 Sprite 在场景中的物理尺寸

运行时，`SpriteRenderer` 根据 Sprite 的 UV 信息生成一个四边形（两个三角形），提交渲染。

### Sprite Mode

| 模式 | 说明 | 适用场景 |
|-----|-----|---------|
| **Single** | 整张纹理就是一个 Sprite | 单图资产（背景、大型角色） |
| **Multiple** | 一张纹理包含多个 Sprite，用 Sprite Editor 切割 | 角色动画帧、UI 图集 |
| **Polygon** | 自定义多边形轮廓，减少透明像素的渲染面积 | 不规则形状 Sprite |

**Multiple 模式**是 2D 角色动画的标准做法：把所有动画帧排在一张 Sprite Sheet 上，通过 Sprite Editor 的 `Slice` 功能自动切割，然后在 Animator 中按帧序列播放。

### Mesh Type 对渲染的影响

Sprite Inspector 的 `Mesh Type` 控制底层三角形的生成方式：

- **Full Rect**：始终用矩形四边形（2 个三角形），包含所有透明区域
- **Tight**：沿 Sprite 的不透明轮廓生成紧包多边形，减少透明像素的 Overdraw

`Tight` 模式会增加顶点数，但对于有大面积透明区域的 Sprite（如不规则角色轮廓），可以显著减少 GPU 的透明像素填充量。

---

## Sprite Atlas：合批的物质基础

### 为什么 Sprite Atlas 能减少 Draw Call

GPU 每次切换纹理都需要刷新纹理缓存，`SpriteRenderer` 的合批要求使用**相同纹理**。如果 100 个 Sprite 来自 100 张不同纹理，就有 100 个 Draw Call。把它们打包进一张 Atlas，所有 Sprite 共享同一纹理，合批成 1 个 Draw Call。

### Atlas 打包规则

在 Project 窗口创建 `Sprite Atlas`（右键 → Create → 2D → Sprite Atlas），将 Sprite 或包含 Sprite 的文件夹拖入 `Objects for Packing` 列表。打包时 Unity 会：

1. 把所有 Sprite 的像素区域复制到同一张大纹理
2. 重新计算每个 Sprite 的 UV 映射
3. 运行时 `SpriteRenderer` 自动使用 Atlas 纹理而不是原始纹理

Atlas 的最大尺寸通常为 2048×2048 或 4096×4096。超出尺寸时需要拆分成多个 Atlas 页面。

### Late Binding：与 AssetBundle 的配合

默认情况下，Atlas 在被引用时立刻加载。启用 `Include in Build = false` 并使用 Late Binding 后，Atlas 不会自动打包进游戏，需要手动加载：

```csharp
// 注册 Atlas 请求回调（在 AssetBundle 工作流中）
SpriteAtlasManager.atlasRequested += (tag, callback) =>
{
    // 从 AssetBundle 异步加载 Atlas
    StartCoroutine(LoadAtlasFromBundle(tag, callback));
};
```

这样可以实现按关卡分包：第一关的 Sprite Atlas 只在第一关加载，不占用其他关卡的内存。

---

## 九宫格（9-Slicing）：可拉伸而不失真的 UI 元素

### 原理

九宫格把一张 Sprite 划分为 3×3 共 9 个区域：

```
┌──────┬────────────┬──────┐
│ 左上角 │   上边       │ 右上角 │  ← 角落区域：不拉伸，保持原始比例
├──────┼────────────┼──────┤
│ 左边  │    中心      │  右边  │  ← 边缘区域：单向拉伸
├──────┼────────────┼──────┤
│ 左下角 │   下边       │ 右下角 │  ← 角落区域：不拉伸
└──────┴────────────┴──────┘
                              ↑ 中心区域：双向拉伸
```

当 `Image` 组件调整尺寸时：
- **四个角落**：保持原始像素尺寸，不变形
- **四条边**：沿单轴方向拉伸
- **中心区域**：沿两个轴方向拉伸

这样无论按钮/面板拉多宽，角落的圆角/装饰纹理都不会变形。

### 配置方式

1. 在 Sprite Editor 中拖动 Border 手柄，设置四条边的边界（Left/Right/Top/Bottom，单位：像素）
2. 在 `Image` 组件中设置 `Image Type = Sliced`
3. 调整 `Image` 的 RectTransform 尺寸，九宫格自动生效

`Image.pixelsPerUnitMultiplier` 可以调整中心区域的平铺密度（当 `Tiled` 模式时）。

### Pixels Per Unit 的影响

`Sprite.pixelsPerUnit` 决定了九宫格边框在世界空间中的物理宽度。如果 Sprite PPU = 100，边框 = 10px，则边框在 Unity 世界坐标中宽度 = 10/100 = 0.1 单位。PPU 设置不一致时，九宫格比例会在不同尺寸的 Image 上看起来不一样。

---

## Sorting Layer 与 Order in Layer：2D 深度排序

### 不是 Z 值，是渲染顺序

3D 场景通过 Z 深度（距摄像机远近）确定遮挡关系，2D 场景通常所有物体在同一 Z 平面，无法用 Z 深度排序。Unity 的 2D 排序机制是：

1. **Sorting Layer**：手动定义的渲染层级列表（Project Settings → Tags and Layers → Sorting Layers），层级靠后的覆盖前面的
2. **Order in Layer**：同一 Sorting Layer 内的整数排序值，数值大的覆盖数值小的

这两个值只影响渲染顺序，不影响物理碰撞。

### Sorting Group 组件

当一个复杂对象由多个 `SpriteRenderer` 子对象组成（如带装备的角色），希望整体作为一个排序单元时，使用 `Sorting Group`：

```
角色根对象（SortingGroup: Layer=Character, Order=5）
├── 身体 SpriteRenderer（无需设置 Sorting Layer）
├── 手臂 SpriteRenderer
└── 武器 SpriteRenderer
```

`Sorting Group` 把所有子 `SpriteRenderer` 视为一个整体参与排序，内部子对象的相对顺序在组内处理，不影响与其他对象的排序比较。

---

## URP 2D Renderer：动态 2D 光照

### Light2D 类型

URP 的 2D Renderer 提供专用的 `Light2D` 组件，支持四种光照类型：

| 类型 | 说明 | 适用场景 |
|-----|-----|---------|
| **Global** | 全局环境光，影响所有 2D 物体 | 场景基础亮度 |
| **Freeform** | 自定义多边形光照范围 | 窗户透光、火焰范围光 |
| **Spot** | 圆形/椭圆形锥形光 | 手电筒、路灯 |
| **Point** | 点光源，360° 衰减 | 蜡烛、爆炸光效 |

每个 `Light2D` 有 **Target Sorting Layers** 设置，只影响指定 Sorting Layer 上的物体，可以做到前景和背景用不同光照。

### Normal Map 在 2D 里的工作方式

3D 里 Normal Map 的 Z 轴（蓝色通道）朝向表面外侧，代表表面法线的深度方向。2D 场景中，Sprite 是平面的，**法线的 Z 轴固定朝向摄像机（屏幕外）**，XY 轴在纹理平面内偏转：

```
2D Normal Map 约定：
- R 通道（X）：法线左右偏转
- G 通道（Y）：法线上下偏转
- B 通道（Z）：固定接近 1.0（朝向摄像机），值越大越"平"
```

当 Light2D 从左侧照射时，Normal Map 的 X 轴分量让左侧部分更亮（受光面），右侧更暗（背光面），从而产生 3D 立体感。这是 2D 角色做出"真实光照"感的核心技术。

### Shadow Caster 2D

`Shadow Caster 2D` 组件让 2D 物体可以投射阴影（实际上是遮挡光照，而不是传统 3D 阴影贴图）：

- 通过自定义多边形路径定义阴影轮廓
- `Self Shadows`：控制是否对自身产生阴影
- `Casts Shadows`：控制是否向其他物体投射

2D 阴影本质上是在 2D 光照的 Mask Pass 中用 Stencil 实现的遮蔽，性能代价远低于 3D Shadow Map。

### 2D 光照的性能代价

每盏 `Light2D` 会增加一个 Lighting Pass（或多个，取决于 Blend Style）。移动端建议：

- Global Light 只用 1~2 盏
- 动态 Spot/Point Light 尽量少于 4 盏
- 关闭不需要 Normal Map 的 Sprite 上的法线采样（`Use Normal Map` 关闭）

---

## Tilemap 渲染

### TilemapRenderer 合批

`Tilemap` + `TilemapRenderer` 是 Unity 2D 关卡的标准工具。渲染方面，同一 Tilemap 内的所有 Tile 可以合批（共享同一 Tile Palette 的纹理），一整块地图通常只有 1~2 个 Draw Call。

### Chunk Mode vs Individual Mode

`TilemapRenderer` 的 `Mode` 属性：

| 模式 | 说明 | 适用场景 |
|-----|-----|---------|
| **Chunk** | 把 Tilemap 按区块（Chunk）合并成大 Mesh | 静态背景地图（默认，性能最好） |
| **Individual** | 每个 Tile 独立渲染，参与全局深度排序 | 需要与其他 SpriteRenderer 精确排序时 |

**Individual Mode 会破坏合批**，每个 Tile 变成独立的排序单元。只在必须与角色精确排序（Tile 既在角色前又在角色后）的场景中使用。大多数情况下，用 Z 轴偏移或分层 Tilemap（背景层/前景层分开）来解决排序问题，保持 Chunk Mode。

---

## Pixel Perfect Camera：像素完美渲染

像素游戏（Pixel Art）的核心问题是**亚像素偏移**：当游戏分辨率与屏幕分辨率不匹配时，Sprite 的渲染位置会落在像素边缘，产生模糊。

`Pixel Perfect Camera` 组件（URP 2D Renderer 内置）解决这个问题：

- **Reference Resolution**：游戏设计的基准分辨率（如 320×180）
- **Assets Pixels Per Unit**：Sprite 的 PPU 设置
- `Crop Frame`：超出基准分辨率的部分如何处理（剪裁/黑边/拉伸）
- **运行时**：自动将摄像机的正交尺寸对齐到整数倍像素，并将物体位置 snap 到像素网格

```csharp
// 运行时读取实际缩放倍数
var ppc = Camera.main.GetComponent<PixelPerfectCamera>();
Debug.Log($"Zoom: {ppc.pixelRatio}x");
```

注意：`Pixel Perfect Camera` 会限制摄像机的正交尺寸，可能与某些后处理效果冲突。

---

## 小结

Unity 的 2D 渲染系统是 3D 管线的专用封装，理解以下核心点：

- **Sprite** 是纹理上的 UV 区域描述，`SpriteRenderer` 生成四边形 Mesh 渲染
- **Sprite Atlas** 合并纹理是减少 Draw Call 的首要手段，Late Binding 支持按需加载
- **九宫格** 通过固定角落、拉伸边缘中心的方式，让 UI 元素自由缩放而不失真
- **Sorting Layer + Order in Layer** 是 2D 深度排序的本质，不是 Z 值
- **URP 2D Renderer** 提供完整的 Light2D 系统，Normal Map 在 2D 里以平面法线偏转的方式工作
- **Tilemap** 的 Chunk Mode 是高效 2D 地图的关键，Individual Mode 代价高，谨慎使用
- **Pixel Perfect Camera** 解决像素艺术的亚像素模糊问题
