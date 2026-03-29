---
date: "2026-03-28"
title: "每档资产规格清单：贴图压缩、LOD 与包体分层"
description: "高/中/低/极低四档下的具体资产预算：ASTC 格式选法（4x4 / 6x6 / 8x8 / 12x12）、角色与场景贴图各档分辨率、LOD Bias 与植被密度、包体分层策略（base 包 + 高档差异包），以及按游戏类型的差异调整。"
slug: "device-tier-asset-spec-texture-and-package"
weight: 40
featured: false
tags:
  - Platform
  - Mobile
  - Performance
  - Assets
  - Client
series: "游戏性能判断"
primary_series: "device-tiering"
series_order: 5.5
---

分档系统在代码层落地之后，真正影响玩家体验的是资产层——贴图用什么压缩格式、分辨率压到多少、LOD 怎么分层、不同档位的设备是否下载不同的包。

这篇给出具体数字，按游戏类型给出差异调整，同时解释包体分层的工程实现。

---

## ASTC 格式选法

ASTC（Adaptive Scalable Texture Compression）是 iOS 和现代 Android 的主流压缩格式，从 iPhone 6（A8）开始全面支持。不同 block size 的代价差异如下：

| 格式 | 压缩率（bpp）| 画质损失 | 适用场景 |
|------|------------|---------|---------|
| ASTC 4x4 | 8 bpp | 极小 | 角色主贴图、近景高细节贴图 |
| ASTC 6x6 | 3.56 bpp | 较小 | 角色次级贴图、中景场景贴图 |
| ASTC 8x8 | 2 bpp | 中等 | 远景场景、地面、天空盒 |
| ASTC 12x12 | 0.89 bpp | 明显 | 极低档或超远景兜底 |

**ETC2 / PVRTC 的使用场景**：
- ETC2：老 Android 设备（不支持 ASTC 的极少数设备，现在市占极低），建议只作为 fallback
- PVRTC：iPhone 5s 及以前（A7 之前），A8（iPhone 6）开始可以用 ASTC，不再需要 PVRTC

Unity 在 Texture Importer 里可以对不同平台分别设置格式；iOS 全线走 ASTC，Android 按 GPU 能力在运行时选择对应的 AssetBundle 变体（或使用 Addressables Content Catalog 分平台配置）。

---

## 各档贴图规格

### 通用规则

1. **角色主贴图（Albedo / Base Color）不轻易降分辨率**：角色是玩家视线焦点，降质感知明显，宁可在其他地方省。
2. **法线贴图对分辨率敏感**：降一半分辨率的法线贴图，在高光下走形很明显，比 Albedo 更应该优先保护。
3. **场景远处贴图可以激进压缩**：超过 30m 的场景基本看不出 ASTC 8x8 和 ASTC 6x6 的差异。
4. **UI 贴图不参与档位压缩**：UI 元素清晰度要求高，且不随设备档位变化，保持 ASTC 4x4 或直接用 PNG/无损格式。

### 规格表

| 贴图类型 | 高档 | 中档 | 低档 | 极低档 |
|---------|------|------|------|--------|
| 角色主贴图（Albedo）| 2048，ASTC 4x4 | 2048，ASTC 6x6 | 1024，ASTC 6x6 | 512，ASTC 8x8 |
| 角色次级贴图（细节/遮罩）| 1024，ASTC 4x4 | 1024，ASTC 6x6 | 512，ASTC 8x8 | 256，ASTC 8x8 |
| 角色法线贴图 | 2048，ASTC 4x4 | 1024，ASTC 6x6 | 512，ASTC 6x6 | 256，ASTC 8x8 |
| 场景近景贴图 | 1024，ASTC 4x4 | 1024，ASTC 6x6 | 512，ASTC 8x8 | 256，ASTC 8x8 |
| 场景中远景贴图 | 1024，ASTC 6x6 | 512，ASTC 8x8 | 256，ASTC 8x8 | 256，ASTC 12x12 |
| 地面 / Terrain | 1024，ASTC 6x6 | 512，ASTC 8x8 | 256，ASTC 8x8 | 128，ASTC 12x12 |
| 天空盒 | 2048，ASTC 6x6 | 1024，ASTC 8x8 | 1024，ASTC 8x8 | 512，ASTC 8x8 |
| 特效贴图 | 512，ASTC 4x4 | 256，ASTC 6x6 | 256，ASTC 8x8 | 128，ASTC 8x8 |
| UI 贴图 | 全档保持，不参与分级 | | | |

---

## LOD、密度与渲染预算

| 配置项 | 高档 | 中档 | 低档 | 极低档 |
|--------|------|------|------|--------|
| Render Scale | 1.0 | 0.85 | 0.75 | 0.65 |
| LOD Bias | 1.0 | 0.8 | 0.6 | 0.5 |
| 植被 / Crowd 密度 | 100% | 70% | 40% | 0%（关闭） |
| VFX 粒子发射量 | 100% | 70% | 40% | 20% |
| Shadow Distance | 80m | 50m | 30m | 10m（或关闭） |
| Shadow Map 分辨率 | 2048 | 1024 | 512 | 关闭 |
| Shadow Cascade | 4 | 2 | 1 | 0 |
| SSAO | 开 | 关 | 关 | 关 |
| Bloom | 开 | 开（降分辨率）| 关 | 关 |
| Color Grading | LUT 完整 | LUT 完整 | 预烘焙 LUT | 预烘焙 LUT |
| 目标帧率 | 60fps | 30–60fps | 30fps | 30fps 锁帧 |

**Color Grading 在低档也要保留**：用预烘焙的一张 LUT 贴图做色调，运行时代价接近零，但能保住视觉风格一致性。这是性价比最高的配置项，不应该在低档直接关掉。

---

## 按游戏类型的差异调整

上面的表格是通用基线。不同游戏类型的核心压力不同，调整侧重点也不一样。

### 动作 / ARPG

**核心压力**：角色特效密度、实时阴影、屏幕空间效果。

| 调整项 | 说明 |
|--------|------|
| 角色主贴图分辨率**不降** | 玩家盯着角色看，2048 在低档也要保住 |
| 低档/极低档关闭实时阴影 | 改用 Blob Shadow，代价极低，视觉上可接受 |
| VFX 粒子降档要激进 | 低档砍到 30%，高密度特效是 ARPG 最大的 GPU 杀手 |
| 中高档单独维护 60fps 配置 | ARPG 对帧率感知强，高档玩家对 30fps 容忍度低 |

### 开放世界 / MMO

**核心压力**：场景复杂度、Draw Call、角色同屏数量、Terrain。

| 调整项 | 说明 |
|--------|------|
| 场景贴图降档比角色更激进 | 大地图的场景贴图预算优先压缩，不值得为远景留高规格 |
| Crowd 密度分档控制 | 低档 NPC 同屏数量从 30+ 砍到 8–10，Draw Call 影响极大 |
| Terrain 低档关闭 Detail Layer | Detail Mesh（草地）是低端机的 GPU 杀手，低档全关 |
| 中低档强制 LOD 更积极切换 | 调低 LOD Bias（0.5–0.6），远处物体更早切到低 LOD |

### 卡牌 / 回合制 RPG

**核心压力**：UI 层复杂度、全屏角色立绘、特效叠加。

| 调整项 | 说明 |
|--------|------|
| 角色立绘可以不参与分档 | 卡牌游戏的立绘是核心卖点，全档保持 2048 ASTC 4x4 |
| 场景预算可以大幅收缩 | 背景往往静态或简单循环动画，低档 512 ASTC 8x8 完全够用 |
| 极低档可以关闭所有实时特效 | 改用预烘焙帧动画（Flipbook）模拟特效，代价低效果好 |
| 帧率压力小，极低档体验可以做得比较完整 | 卡牌游戏就算 30fps 也基本无感知，极低档配置相对宽松 |

### 休闲 / 超休闲

**核心压力**：覆盖尽量多的设备，极低档也要有完整体验。

| 调整项 | 说明 |
|--------|------|
| 贴图规格整体下压一级 | 休闲游戏视觉风格一般偏扁平，512 ASTC 6x6 完全够用 |
| 极低档和低档合并也可以 | 如果底线设备不需要太多区分，极低档配置直接复用低档 |
| 重点保证极低档设备的流畅 | 休闲游戏的目标用户里低端设备比例高，极低档体验比高档体验更重要 |

---

## 包体分层：高中低档下载不同的东西

**是的，不同档位的设备可以下载不同的资源包**。这不是 trick，而是移动端资源交付的基本能力。

### 分层逻辑

```
Base 包（所有设备必下）
├── 极低档 + 低档贴图（512/256 分辨率）
├── 低 LOD Mesh
├── 核心逻辑和 UI
└── 极低档配置所需的全部资源

高档差异包（仅高档设备下载）
├── 2048 分辨率贴图
├── 高 LOD Mesh（可选）
├── 高分辨率天空盒
└── 高质量特效贴图

中档差异包（可选，按需设计）
├── 1024 分辨率贴图升级版
└── 中 LOD Mesh（如果 base 包只包含低 LOD 的话）
```

### 实现方式：Addressables + Content Catalog

Unity Addressables 天然支持按平台 / 标签分组，配合 CDN 可以实现分层下载：

```csharp
// 标签示例：给高档专用资源打上 "high-tier" 标签
// 启动时根据档位决定是否下载该标签的内容

public static async Task DownloadTierContent(QualityTier tier)
{
    var labels = new List<string> { "base" };

    if (tier >= QualityTier.Mid)
        labels.Add("mid-tier");

    if (tier >= QualityTier.High)
        labels.Add("high-tier");

    foreach (var label in labels)
    {
        var handle = Addressables.DownloadDependenciesAsync(label);
        await handle.Task;
        Addressables.Release(handle);
    }
}
```

### 常见踩坑

**坑 1：低档设备下载到了高档资源**

原因：Addressables 分组时没有按档位 label 分开，把高档贴图和低档贴图打进了同一个 bundle。
修复：严格按档位分组，高档差异资源单独 bundle，base 包不包含任何高档专用内容。

**坑 2：包体降了，但运行时仍然引用高档资源**

原因：代码里用 AssetReference 直接引用了高档贴图，低档设备下载时没有下这个资源，运行时报 missing reference。
修复：通过 label 加载资源，而不是直接引用 GUID；低档路径有单独的低分辨率资源对应。

**坑 3：档位切换后包体版本不一致**

原因：玩家游戏过程中触发降档，但高档资源已经下载到本地缓存，低档配置引用了不同版本的资源文件。
修复：档位只影响配置和渲染参数，不在运行时动态替换贴图；贴图资源在启动时按档位一次性决定，不在游戏过程中切换。

**坑 4：中档设备下载了 base 包之后，发现分辨率不够，想升档但没有触发中档包下载**

原因：分层下载逻辑只在首次安装时跑，后续档位调整没有触发补包。
修复：在每次档位判断结束后、正式进游戏前，检查当前档位需要的 bundle 是否已完整下载，缺失则补下。

---

## 极低档的资产策略

极低档（iPhone 6 / Adreno 505 级别）的目标只有一个：**不崩溃，30fps 锁帧**。

这类设备的 1GB RAM 是最硬的墙。贴图内存是第一杀手：

- 一张 2048 ASTC 4x4 贴图解压后约 16MB
- 极低档设备能分配给贴图的预算往往不超过 150–200MB
- 按角色 + 场景合计，极低档下贴图总数量必须控制，不只是分辨率

极低档的典型配置：
- 所有贴图走 base 包，512 以下 ASTC 8x8
- 关闭动态阴影，改用静态 Lightmap + Blob Shadow
- 关闭所有后处理（Bloom、SSAO、Depth of Field 全部关）
- 特效粒子减少到 20% 以下，或改用 Flipbook 帧动画
- Render Scale 降到 0.65，减少像素填充压力

极低档的配置不需要独立维护太多美术资产，只需要激进的开关控制。美术资产的实际制作成本集中在低档和中档之间，极低档只是把已有资源压缩到最小。

---

## 延伸阅读

芯片档位判断的详细代码见：
[主流芯片档位参考表：四档判断依据与代码]({{< relref "engine-notes/device-tier-chip-reference-four-tiers.md" >}})

热机状态如何影响运行时降档见：
[URP 深度平台 04｜热机后的质量分档]({{< relref "engine-notes/urp-platform-04-thermal-and-dynamic-tiering.md" >}})

如何验证分档配置是否真的命中了正确设备见：
[机型分档怎样验证：设备矩阵、Baseline、截图回归与错配排查]({{< relref "code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md" >}})
