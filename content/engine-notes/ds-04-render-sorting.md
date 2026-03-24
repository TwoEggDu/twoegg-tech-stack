---
title: "数据结构与算法 04｜渲染排序与 Z-order：DrawCall 合批、透明物体、2D 层级"
description: "渲染排序是游戏里最高频的排序场景，直接影响 DrawCall 数量、透明效果正确性和 2D 层级显示。这篇讲清楚不透明/透明物体的排序策略、Unity 的渲染队列设计，以及 2D 游戏的 Z-order 管理。"
slug: "ds-04-render-sorting"
weight: 747
tags:
  - 软件工程
  - 数据结构
  - 渲染
  - 排序
  - 性能优化
  - 游戏架构
series: "数据结构与算法"
---

> 渲染管线每帧都在排序。不透明物体按材质排（减少状态切换），透明物体按深度排（保证混合正确），2D 对象按层级排（保证遮挡关系）。排错了，要么性能差，要么画面花。

---

## 为什么渲染需要排序

### 不透明物体：减少状态切换

GPU 渲染时，切换不同的 Shader / Material 是昂贵的操作（"SetPass Call"）。如果把使用相同材质的 DrawCall 放在一起提交，可以大幅减少状态切换次数：

```
不排序提交顺序：
[材质A] [材质B] [材质A] [材质C] [材质B] [材质A]
→ 6 个 DrawCall，5 次材质切换

按材质排序后：
[材质A] [材质A] [材质A] [材质B] [材质B] [材质C]
→ 6 个 DrawCall，2 次材质切换（+合批机会）
```

### 透明物体：从后往前（Painter's Algorithm）

透明物体（使用 Alpha Blend）需要正确的混合顺序。GPU 的深度测试对透明物体不适用——透明像素不写深度，导致"被遮挡的透明物体"也能显示出来。

解决方案：**从后往前绘制**，用"画家算法"让前面的覆盖后面的：

```
错误（随机顺序）：
先画近处玻璃窗 → 再画远处火焰
结果：远处火焰画在玻璃窗之上，穿透玻璃出现（视觉错误）

正确（从后往前）：
先画远处火焰 → 再画近处玻璃窗
结果：玻璃窗混合叠加在火焰之上，透出火焰的颜色（正确）
```

### 不透明物体的另一种排序：从前往后

不透明物体也可以反过来——**从前往后**排序，让近处物体先写入深度缓冲，远处物体在 Early-Z 阶段被剔除，减少 Overdraw：

```
从前往后渲染不透明物体：
先画近处地板 → 写入深度
再画远处山脉 → 深度测试失败（被地板遮挡）→ 整个像素被丢弃
→ 减少了 Fragment Shader 的执行次数
```

Unity 的 URP/HDRP 对不透明物体默认就是从前往后排序。

---

## Unity 的渲染队列（Render Queue）

Unity 用一个整数 Render Queue 值决定渲染顺序：

```
Background    = 1000   ← 天空盒、背景
Geometry      = 2000   ← 普通不透明物体（默认）
AlphaTest     = 2450   ← 用 Alpha Clip 的物体（草、栅栏）
Transparent   = 3000   ← 半透明物体（玻璃、粒子）
Overlay       = 4000   ← UI、后期效果
```

在 Shader 里设置：

```hlsl
SubShader
{
    Tags { "Queue" = "Transparent" }   // 放入透明队列
    // 或者精确指定
    Tags { "Queue" = "Transparent+10" } // 比其他透明物体晚渲染
}
```

在代码里动态修改：

```csharp
// 让某个物体比其他透明物体晚渲染（比如特效覆盖在所有透明物体之上）
renderer.material.renderQueue = 3010;
```

---

## 同一队列内的排序逻辑

同一渲染队列内，Unity 还会进行二次排序：

**Geometry 队列（不透明）**：
```
排序键 = 材质 ID + Shader ID + 渲染器类型
→ 相同材质的物体尽量连续提交
→ 触发 Dynamic Batching / GPU Instancing（进一步减少 DrawCall）
```

**Transparent 队列（透明）**：
```
排序键 = 到摄像机的距离（从远到近）
→ 保证画家算法的正确性
→ 相同距离的物体：按提交顺序（稳定排序）
```

---

## DrawCall 合批与排序的关系

Unity 的 Static Batching（静态合批）和 Dynamic Batching（动态合批）都依赖排序：

```csharp
// Dynamic Batching 条件（简化版）：
// 1. 相同材质
// 2. 顶点数 < 900
// 3. 没有使用 MaterialPropertyBlock（不同属性块会打断合批）

// 排序确保相同材质的物体连续，为合批创造条件
// 如果排序结果是 [A_mat1][B_mat2][C_mat1]，A 和 C 虽然同材质但不连续，无法合批
```

```csharp
// GPU Instancing：同一个 Mesh + Material 的多个实例一次 DrawCall 绘制
// 前提：它们在渲染队列里是连续的

// 使用 MaterialPropertyBlock 给每个实例设置不同属性（颜色、HP 条等）
// 同时保持合批
MaterialPropertyBlock mpb = new MaterialPropertyBlock();
mpb.SetFloat("_Health", enemy.health / enemy.maxHealth);
renderer.SetPropertyBlock(mpb);
```

---

## 2D 游戏的 Z-order：Sorting Layer 与 Order in Layer

2D 游戏里所有物体都在同一个深度（或接近同一深度），不能靠深度缓冲决定遮挡，必须显式管理绘制顺序。

Unity 的解决方案：**Sorting Layer + Order in Layer**

```
Sorting Layer（层级，从低到高依次绘制）：
  Background   ← 背景图、天空
  Terrain      ← 地面、地形装饰
  Character    ← 玩家、NPC、怪物
  Effect       ← 技能特效、粒子
  UI_World     ← 血条、名字（世界空间 UI）

Order in Layer（同一 Layer 内的顺序）：
  数值越大越后绘制（越显示在前面）
```

```csharp
// 代码里设置
spriteRenderer.sortingLayerName = "Character";
spriteRenderer.sortingOrder = 10;

// 让角色根据 Y 轴位置自动决定遮挡关系（上方的角色被下方的遮挡）
void Update()
{
    // Y 越小（越靠下），Order 越大（越显示在前面）
    spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
}
```

这个技巧在俯视角 RPG 里很常见：角色的 Y 坐标越靠下（越靠近玩家视角），排序值越高，显示在其他角色前面，实现正确的遮挡感。

---

## 动态 Y-Sort 的性能问题

如果场景里有 500 个角色都在每帧更新 `sortingOrder`，会产生 500 次 SetProperty 调用，破坏合批。

优化方案：

```csharp
// 方案一：只在位置变化时更新
private float lastY;
void Update()
{
    if (Mathf.Abs(transform.position.y - lastY) > 0.01f)
    {
        lastY = transform.position.y;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-lastY * 100);
    }
}

// 方案二：用自定义排序器，在统一的管理器里批量排序，避免每帧 SetProperty
// SortingManager 收集所有角色的 Y 坐标，统一排序，只更新变化的条目
```

---

## 透明物体排序的陷阱

### 陷阱一：粒子系统与其他透明物体的层叠

```
场景：玩家穿过火焰粒子效果
错误：粒子系统的 Render Queue = 3000，角色的透明部分（衣服）也是 3000
     深度相近，排序不稳定 → 闪烁

解决：手动设置 renderQueue 拉开距离
     角色透明部分：3001
     覆盖型粒子效果：3002
```

### 陷阱二：透明物体之间的穿插（Intersecting Transparency）

画家算法只适合不穿插的透明物体。两个相互穿插的半透明物体，不管哪个先画，都有部分是错的：

```
正确处理方式：
1. 避免设计上让透明物体穿插（美术规范）
2. Order Independent Transparency（OIT）：Depth Peeling、Weighted Blended OIT
   （高端渲染管线才会实现，移动端通常不做）
3. Dithered Alpha（用 Alpha Clip 模拟半透明，用噪点抖动）：完全规避透明排序问题
```

---

## 小结

| 类型 | 排序方向 | 排序键 | 目的 |
|---|---|---|---|
| 不透明物体 | 从前往后 | 深度（到相机距离） | 减少 Overdraw（Early-Z） |
| 不透明物体（合批） | 按材质分组 | 材质 ID | 减少 SetPass Call，触发合批 |
| 透明物体 | 从后往前 | 深度（到相机距离） | 保证 Alpha Blend 正确性 |
| 2D Sprite | Sorting Layer + Order | 自定义 | 控制 2D 遮挡关系 |

- **Render Queue**：控制大类（不透明 / 透明 / UI），同类内部再按深度或材质排
- **Y-Sort**：俯视角游戏的标准解法，按 Y 坐标动态更新 `sortingOrder`
- **透明物体穿插**：画家算法无法处理，用 Dithered Alpha 或 OIT 绕过
- **合批与排序**：相同材质的物体必须连续才能合批，排序是合批的前提
