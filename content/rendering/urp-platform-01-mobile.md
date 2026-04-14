---
title: "URP 深度平台 01｜移动端专项配置：为什么这么设、怎么验证"
slug: "urp-platform-01-mobile"
date: "2026-03-25"
description: "URP 移动端配置不是参数抄答案，而是每个选项背后有具体的 TBR 带宽代价模型。本篇从 TBR 架构出发，逐一讲清楚 Pipeline Asset、Universal Renderer Settings、Pass 裁剪、Shader 写法四个层面的配置决策依据，并给出用 Frame Debugger / Xcode / Snapdragon Profiler 验证效果的具体方法。"
tags:
  - "Unity"
  - "URP"
  - "移动端"
  - "性能优化"
  - "TBR"
  - "渲染管线"
series: "URP 深度"
weight: 1650
---
> **读这篇之前**：本篇会大量引用 TBR/TBDR 架构和 Pipeline Asset 参数。如果不熟悉，建议先看：
> - [游戏图形系统 08｜移动 GPU 与桌面 GPU 的区别]({{< relref "rendering/game-graphics-stack-08-mobile-vs-desktop-gpu.md" >}})
> - [移动端硬件 02｜TBDR 架构详解]({{< relref "rendering/hardware-02-tbdr.md" >}})
> - [URP 深度配置 01｜Pipeline Asset 解读]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})

移动端 URP 配置的常见误区是"照着最佳实践清单全关一遍"。问题在于：没有理解原因的配置，换个项目就不会用了，遇到效果和性能的取舍也无从判断。

这篇从移动端 GPU 的架构特点出发，讲清楚每个配置项背后的代价是什么，以及怎么用工具验证你的配置确实起了作用。

---

## 前提：TBR 的带宽代价模型

移动端 GPU 普遍使用 TBR（Tile-Based Rendering）或 TBDR（Tile-Based Deferred Rendering）架构。和 PC 的 IMR（Immediate Mode Rendering）最大的区别在于：

**TBR 把屏幕分成小块（Tile，通常 16×16 或 32×32 像素），每个 Tile 的中间计算结果保存在片上的 Tile Memory 里，不需要写回系统内存。只有最终结果才写到 Framebuffer。**

这意味着：
- **减少 RT 切换 = 减少 Store/Load 带宽**：每次切换 RT，当前 Tile 的内容要写回系统内存（Store），新 RT 的内容要读进来（Load）。系统内存带宽远慢于片上 Tile Memory，代价很高。
- **Native RenderPass 合并 Pass = 让更多计算留在 Tile Memory 里**：如果两个相邻 Pass 能合并成一个 RenderPass，中间结果不需要写回系统内存。
- **打断 Early-Z = 增加 OverDraw**：TBR 的 Hidden Surface Removal（HSR）依赖 Early-Z，`discard` / `clip` 会打断这个流程，导致本来不需要执行的 Fragment Shader 被执行。

**这三条是移动端所有渲染配置决策的底层逻辑。** 下面每个配置项都可以对应回这里。

---

## Pipeline Asset 配置

### HDR

```
移动端推荐：按需开启，不是默认必须开
```

HDR 开启后，相机颜色 RT 格式从 R8G8B8A8（32bit）升级到 R16G16B16A16 或 R11G11B10（32~64bit），带宽翻倍或更多。

**什么时候值得开**：项目有 Bloom、Tone Mapping、颜色分级，HDR RT 保留高动态范围避免 Bloom 出现 Banding。这些效果在 LDR RT 上质量会明显下降。

**什么时候可以关**：无后处理或只用简单 2D 风格渲染，关掉 HDR 直接省一倍带宽。

**格式选择**：如果必须开 HDR，优先选 `R11G11B10`（无 Alpha 通道，32bit），而不是 `R16G16B16A16`（64bit）。大多数移动端场景不需要 Alpha 通道的 HDR 精度，R11G11B10 在 Metal 和 Vulkan 上都支持。

---

### MSAA

```
移动端推荐：2x 或关闭（用 FXAA/TAA 代替）
```

TBR 架构对 MSAA 有天然优势——MSAA 的多采样数据可以存在 Tile Memory 里，Resolve 在 Tile 内完成，不需要写回系统内存。因此移动端 MSAA 的带宽代价远低于 PC。

**但 4x MSAA 依然有代价**：4x 意味着每个像素存 4 份采样数据，Tile Memory 占用翻 4 倍，大型 Tile Buffer 的 Tile Memory 可能不够，溢出到系统内存，代价反而上升。

**推荐策略**：
- 低端档：关闭 MSAA，开 FXAA（后处理，几乎无代价）
- 中端档：2x MSAA
- 高端档：2x MSAA + FXAA，或 TAA

---

### Render Scale

```
移动端推荐：中档 0.85，低档 0.7
```

Render Scale 直接线性缩放 GPU 填充率负担。0.85 意味着实际渲染分辨率是目标分辨率的 85%，GPU 工作量约降低 28%（面积比例）。

**注意点**：Render Scale < 1.0 会触发 `Intermediate Texture`（见下文），有额外的 Blit 代价。0.85 以上通常值得，0.7 以下收益递减（画质损失开始明显）。

---

## Universal Renderer Settings

这三个配置是移动端性能影响最大、也最容易被忽视的选项。

### Native RenderPass

```
移动端推荐：开启
```

开启后，URP 尝试把相邻的 Pass 合并成一个 Vulkan/Metal Native RenderPass，减少 Load/Store 次数。

**为什么这对 TBR 很重要**：每次 RT 切换，TBR 需要把 Tile 内容写回系统内存（Store），下次再用时重新读回（Load）。如果两个 Pass 能合并，中间的 Store/Load 直接省掉，带宽节省可以很显著。

<!-- DATA-TODO: 补充 Xcode GPU Frame Capture 或 Mali Graphics Debugger 截图，展示 Native RenderPass 开/关时 Load/Store 次数的对比。截图存放 static/images/urp/native-renderpass-bandwidth.png -->

**什么情况下合并会失败**：
- 中间有 `cmd.Blit` 或自定义 Pass 插在两个 URP Pass 之间
- Renderer Feature 的 `Configure()` 里显式设置了 RT 切换
- Depth Priming 模式设置冲突

如果开启 Native RenderPass 后出现渲染错误，先用 Frame Debugger 确认哪个 Pass 导致合并失败。

---

### Depth Priming Mode

```
移动端推荐：Disabled（默认）或 Auto
```

Depth Priming 是在不透明 Pass 之前先跑一个 Depth Prepass，把深度写好，正式 Pass 用 `ZTest Equal` 减少 OverDraw。

**PC 上 Depth Priming 有价值**：PC GPU 的 Fragment Shader 代价高，提前裁掉 OverDraw 合算。

**移动端通常不推荐**：
- TBDR（Apple GPU、Mali Valhall）有硬件 HSR，自动做 Hidden Surface Removal，不需要软件的 Depth Prepass
- 多一个 Depth Prepass = 多一次 RT 写入 = 多一次 Store，TBR 上代价可能比省掉的 OverDraw 更高
- 如果场景 OverDraw 特别严重（复杂植被、大量透明叠加），`Auto` 模式让 URP 自动判断是否需要

---

### Intermediate Texture

```
移动端推荐：尽量避免触发
```

Intermediate Texture 是 URP 在相机颜色 RT 和最终输出之间插入的一张中间 RT。它会触发额外的 Blit，增加一次 RT 切换。

**什么情况会强制触发**：
- `Render Scale != 1.0`（缩放分辨率后需要 Upscale Blit）
- Renderer Feature 里的 Pass 需要同时读写相机颜色 RT（无法同一 Pass 内读写同一张 RT）
- Camera Stack 有 Overlay Camera

**验证方法**：Frame Debugger 里搜索 `FinalBlit`——如果出现了，说明 Intermediate Texture 被触发了。如果这个 Blit 不是预期的，检查上面三个触发条件。

---

## Pass 裁剪：哪些值得关

不是"全关"，而是按代价和项目需求判断。

### Additional Lights（附加光）

```
移动端推荐：逐像素上限 2~4 盏，或改用 Forward+
```

URP 默认每个物体最多 4 盏附加光逐像素计算。每盏额外的逐像素光 = 额外一次完整的光照计算。

中低端设备上，把逐像素附加光数量从 4 降到 2，在场景附加光密集时帧时间可以节省 15~30%。

**Forward+**（URP 14+）：用屏幕空间 Tile 分配光源，比传统 Forward 的附加光代价更可控，在光源密集场景有优势。但 Forward+ 需要额外的 Tile Culling Pass，低端设备不一定合算，需要实测。

---

### Soft Shadows

```
移动端低档：关闭
```

Soft Shadow 在 URP 里通过多次采样 Shadow Map 实现（PCF / Poisson）。Hard Shadow 是单次采样，代价约是 Soft Shadow 的 1/4~1/6。

低端设备关闭 Soft Shadow 改用 Hard Shadow，视觉差异在移动小屏上通常不明显，帧时间收益明显。

---

### SSAO

```
移动端中低档：关闭；高档：开启 Low 质量
```

SSAO 在低端设备上的代价不成比例。URP 的 SSAO 即使调到 Low 质量，在 1080P 下也需要每帧对几十万像素做深度采样和模糊。

关闭 SSAO 后可以用烘焙 AO（Lightmap 里的 AO 通道）弥补静态物体的接触暗化，动态物体用 Blob Shadow 方案替代。

---

### Shadow Distance 和 Cascade

```
移动端推荐：Distance 50~80m，低档 Cascade 1~2
```

Shadow Distance 控制阴影绘制范围，距离翻倍 = Shadow Map 覆盖面积翻 4 倍 = 阴影精度下降或 Shadow Map 分辨率需要翻倍。

Cascade 数量影响的是 Shadow Map 的绘制次数：4 Cascade = 场景几何体画 4 遍深度图。低端设备 1 个 Cascade 就够，加一个 Stable Cascade 设置减少阴影抖动。

---

## Shader 侧的 Tile 友好写法

配置层之外，Shader 写法也会影响 TBR 效率。

### 避免 discard / clip 打断 Early-Z

```hlsl
// ❌ 透明度测试不推荐直接 clip
clip(alpha - 0.5);

// ✅ 改用 Alpha to Coverage（MSAA 场景）
// 或接受全透明物体用透明排序而不是 clip
```

TBDR 的 HSR 依赖 Early-Z 提前剔除被遮挡的 Fragment。`discard` / `clip` 让 GPU 无法在 Fragment Shader 执行前确定该像素是否可见，HSR 对这些像素失效。

植被、栅栏等大量使用 Alpha Test 的物体是最常见的 OverDraw 来源。如果无法避免，至少把这些物体的 RenderQueue 放在不透明物体的最后，让它们在深度已经填好的情况下执行。

---

### Framebuffer Fetch（iOS Metal）

Metal 支持 Framebuffer Fetch，允许 Fragment Shader 直接读取当前 Tile 里已经写好的颜色值，**不需要把 Tile 内容写回系统内存再重新采样**。

URP 的 Deferred 路径在 Metal 上会自动利用 Framebuffer Fetch 合并 G-Buffer 读取。自定义 Shader 可以通过 `COLOR_TARGET_BLEND` 或 `LOAD_FRAMEBUFFER_INPUT` 宏使用（需要 URP 14+）：

```hlsl
// Metal Framebuffer Fetch 示例（混合效果）
LOAD_FRAMEBUFFER_INPUT(0) → 直接读当前 Tile 的颜色，零带宽代价
```

---

## 测试验证方法

配置改完之后，需要工具确认实际效果。

### Unity Frame Debugger（快速定位）

**Window → Analysis → Frame Debugger**

用途：
- 确认 Native RenderPass 是否合并成功（合并后相邻 Pass 会在同一个 `RenderPass` 括号下）
- 搜索 `FinalBlit` 确认 Intermediate Texture 是否触发
- 查看每个 Pass 的 RT 尺寸和格式

---

### Xcode GPU Frame Capture（iOS 专用，最权威）

用 Xcode 连接 iOS 真机，项目打 Development Build 后直接捕获。

**关键指标**：
- **Bandwidth**（带宽）：看 Tile Memory Load/Store 次数，这是 TBR 配置是否有效的直接证据
- **GPU Time**：按 Pass 分解的 GPU 耗时
- **Fragment Utilization**：Fragment Shader 利用率，高 OverDraw 会让这个数字虚高

**A/B 对比方法**：改一个配置，同一场景、同一视角，各捕获 3 帧取平均，排除第一帧热身代价。

---

### Snapdragon Profiler（Android Qualcomm 设备）

连接 Adreno 设备，实时查看：
- **Vertices / Fragments**：几何和像素的处理量
- **Memory Read/Write**：系统内存带宽，配合 Native RenderPass 调优
- **SP Active**：Shader Processor 利用率

**Mali 设备用 Mali Graphics Debugger**，指标类似，界面不同。

---

### 验证顺序建议

不要一次改所有配置，按影响大小逐步验证：

```
① 开启 Native RenderPass → Frame Debugger 确认 Pass 合并
② 检查 Intermediate Texture → 搜索 FinalBlit 是否出现
③ 调整 Depth Priming Mode → 对比开关前后帧时间
④ 降低 Additional Lights 上限 → 场景灯光密集处实测
⑤ 关闭 Soft Shadow / SSAO → 低端设备实测帧时间
⑥ Render Scale 调整 → 实机视觉质量 vs 帧时间权衡
```

每步确认收益后再往下走，这样能明确知道是哪个改动带来了多少收益，后续有需要回退也清楚。

---

## 导读

- 上一篇：[URP Shader 手写｜从骨架到完整光照：接入主光、附加光与阴影]({{< relref "rendering/urp-shader-custom-lit.md" >}})
- 下一篇：[URP 深度平台 02｜多平台质量分级：三档配置的工程实现]({{< relref "rendering/urp-platform-02-quality.md" >}})

---

## 小结

- TBR 的核心代价模型：减少 RT 切换（Store/Load）、不要打断 Early-Z
- **Native RenderPass**：移动端必开，合并 Pass 减少带宽
- **Depth Priming**：TBDR 设备有硬件 HSR，通常不需要，设为 Disabled 或 Auto
- **Intermediate Texture**：尽量避免触发，Render Scale 非 1.0 会强制触发
- 逐项验证而不是批量改：用 Frame Debugger + 真机 Profiler，每步确认实际收益
- Shader 侧：避免 `discard/clip`，iOS 上利用 Framebuffer Fetch

下一篇：URP平台-02，多平台质量分级的工程实现——三档配置表、iOS / Android 设备检测代码、Runtime 切换策略。
