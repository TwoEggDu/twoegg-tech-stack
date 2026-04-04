---
title: "Mali 现代架构深度｜Valhall / 5th Gen 的 Execution Engine、带宽模型与移动优化含义"
slug: "zero-b-deep-01-mali-modern-architecture"
date: "2026-03-28"
description: "从 Bifrost 到 Valhall 到 5th Gen，Mali 的 Shader 执行模型经历了根本性变化：Quad 变成 Warp、超标量执行替代向量流水线。理解这些变化，才能解释为什么相同的 Shader 在新旧 Mali 设备上的性能表现如此不同，以及为什么带宽优化在 Mali 上比 Adreno 更加关键。"
tags:
  - "Mali"
  - "GPU"
  - "架构"
  - "移动端"
  - "性能优化"
series: "零·B 深度补充"
series_id: "zero-b-deep"
series_order: 1
weight: 2300
---

移动端 GPU 性能分析中有一个反复出现的现象：同一个 Shader，在骁龙设备上跑得很顺，换到联发科天玑或者老款三星 Exynos 之后帧率明显下滑。排查之后往往发现根源在 GPU 架构——更具体地说，在于 Mali 的执行模型和带宽特性与 Adreno 存在结构性差异。

本文的目标是把这个差异讲清楚：Mali 的架构在过去十年经历了什么根本性变化，Valhall 的 Warp 执行模型意味着什么，带宽为什么在 Mali 上是更硬的瓶颈，以及这些因素如何落地到 Shader 编写和优化决策上。

---

## 一、Mali 架构演进线

理解当前 Mali 的行为，需要先知道它从哪里来。

```
Midgard（2012–2017）：
  执行模型：向量流水线，128-bit SIMD
  线程分组：Quad-based（4 个线程为一组）
  代表型号：Mali-T880（2015）、Mali-G71（2016）
  主要问题：向量 ALU 利用率低，标量操作浪费 3/4 的 ALU 宽度

Bifrost（2016–2020）：
  执行模型：引入 Claused Shader 执行模式（指令分组为 clause）
  改进：废弃向量流水线，改为标量执行
  线程分组：Quad-based（仍然 4 线程一组）
  代表型号：Mali-G72（2017）、Mali-G76（2018）、Mali-G77（2019）
  主要问题：Clause 边界导致流水线气泡，占用率管理粒度粗

Valhall（2019–2022）：
  执行模型：根本性重设计，Warp-based 执行
  线程分组：每个 Warp = 16 个线程（Quad 的 4 倍）
  ALU：超标量（每个 Execution Engine 有多个独立 FMA 单元）
  调度：动态指令调度，打破 Clause 边界限制
  代表型号：Mali-G77（2019）、Mali-G78（2020）、Mali-G610/G715（2022）
  注意：G77 跨越了 Bifrost 和 Valhall 的命名混乱期，G78 才是完整 Valhall

5th Gen / Immortalis（2022–2024）：
  执行模型：继承 Valhall，进一步扩展 EE 数量
  新特性：硬件光线追踪（Immortalis 子品牌专有）
  缓存：旗舰型号 L2 扩展至 8MB
  代表型号：
    Immortalis-G715（2022，有 RT 单元）
    Mali-G615（2022，无 RT，为中端）
    Immortalis-G720（2023）
    Immortalis-G925（2024，骁龙竞品级旗舰）
    Mali-G720（无 RT 版本）
```

这条演进线中最重要的节点是 Valhall 的引入。Midgard 到 Bifrost 是优化，Bifrost 到 Valhall 是架构重写。

---

## 二、Valhall Execution Engine：Warp 执行模型的含义

### Bifrost 的 Clause 执行：问题在哪里

Bifrost 引入了 Claused Shader 执行模式。编译器将 Shader 指令分割成若干 clause，每个 clause 是一段指令序列。执行规则是：当前 clause 的所有指令必须全部完成，才能发射下一个 clause。

这个设计的初衷是降低调度硬件的复杂性，但代价明显：

- clause 内部的指令如果存在长延迟操作（比如纹理采样），整个 clause 必须等待
- clause 边界会产生流水线气泡，尤其是跨 clause 的数据依赖
- 线程占用率（occupancy）管理粒度粗，4 线程一组的 Quad 太小，无法充分隐藏内存延迟

举一个具体数字：假设纹理采样延迟是 200 cycles，Bifrost 的 4 线程 Quad 在等待采样时，EE 闲置。Valhall 的 16 线程 Warp 可以切换到另一个 Warp 继续工作，把这 200 cycles 用来推进其他 Warp 的计算。

### Valhall 的 Warp-based 执行

Valhall 抛弃了 Clause 机制，改为接近桌面 GPU 的 Warp-based 动态调度：

```
Valhall Execution Engine（EE）结构：

  一个 EE 管理若干个 Warp（每个 Warp = 16 个线程）
  EE 内有多个独立 FMA 单元（超标量）
  调度器在每个周期选择就绪的指令发射

  占用率示意：
  EE 同时持有 N 个 Warp（通常 4-8 个，取决于寄存器用量）
  当 Warp-0 的所有线程都在等待纹理采样（~200 cycles）
    → 调度器切换到 Warp-1
    → Warp-1 计算期间，Warp-0 的纹理请求在后台处理
    → 纹理返回后，Warp-0 重新变为就绪状态

  对比参考：
    NVIDIA SM：32 线程 / Warp
    AMD CU：64 线程 / Warp（实际是 2x32）
    Valhall EE：16 线程 / Warp

  Mali Warp 更小的含义：
    单个 Warp 消耗的寄存器更少 → 每个 EE 可以容纳更多 Warp
    → 在高寄存器压力的 Shader 下，占用率下降更慢
    → 适合移动端的复杂 PBR Shader（寄存器用量通常较高）
```

超标量 ALU 意味着如果一个 Warp 的指令流中存在多条无数据依赖的独立指令，调度器可以在同一周期发射多条。这是 Bifrost 单发射设计无法做到的。

### 对 Shader 编写的影响

Warp 调度和超标量特性改变了 Shader 优化的思路：

```glsl
// 案例一：长依赖链（Bifrost 和 Valhall 都受损，但 Valhall 更能缓解）
//
// 每个计算都依赖上一个结果，形成串行依赖链
// ALU 永远在等待上一条指令的结果
float a = texture(texAlbedo, uv).r;       // 等待 texAlbedo fetch
float b = a * roughnessFactor;             // 依赖 a，等 a 完成
float c = b + ambientOcclusion;            // 依赖 b
float d = c * c;                           // 依赖 c
// 指令流：fetch → mul → add → mul（完全串行）
// Bifrost：clause 等待整体延迟，利用率极低
// Valhall：超标量无法并行（依赖存在），但 Warp 切换可以隐藏 fetch 延迟

// 案例二：独立操作（Valhall 超标量可以并发发射）
//
// 两次纹理采样互相独立
float albedo  = texture(texAlbedo,  uv).rgb;   // fetch 1
float normal  = texture(texNormal,  uv).rgb;   // fetch 2，与 fetch 1 独立
float roughness = texture(texRoughness, uv).r; // fetch 3，独立
// Valhall 的调度器可以在这段等待窗口内切换 Warp
// 同时，独立的 ALU 操作（后续的 dot、normalize 等）可以超标量并发

// 案例三：显式利用 fp16 独立通道（Valhall 的 FMA 对 fp16 有 2x 吞吐）
mediump float ka = dot(lightDir, normal);      // fp16 FMA
mediump float kb = dot(viewDir,  normal);      // 独立的 fp16 FMA，可同周期发射
// 编译器能识别这两条指令独立，超标量调度同时执行
```

核心原则：减少依赖链深度，增加指令级并行（ILP）。这在 Bifrost 上帮助有限，在 Valhall 上会被超标量调度器充分利用。

---

## 三、Mali 的带宽模型：为什么带宽比 Adreno 更关键

### TBDR 的带宽优势和结构性代价

Mali 使用 TBDR（Tile-Based Deferred Rendering）。TBDR 的带宽优势是真实的：Fragment Shading 在片上 Tile Buffer 内完成，同一 Pass 内的 Color Attachment 读写不经过 DRAM。这是移动端 GPU 的核心设计。

但 TBDR 存在结构性代价，Mali 的实现方式使这个代价更明显：

```
TBDR 带宽代价分析：

1. Tiler 阶段（Binning Pass）：
   所有顶点必须先经过一次变换（Vertex Shader）
   每个图元被分配到对应的 Tile（Bin 阶段）
   分配结果（图元列表）写入 DRAM：Tiler Stream
   
   → 每帧写一次 Tiler Stream 到 DRAM
   → 复杂场景（高多边形数）的 Tiler Stream 可以达到几十 MB

2. Tile Shading 阶段：
   每个 Tile 从 DRAM 读取该 Tile 的图元列表
   执行 Fragment Shader
   结果写回 Tile Buffer（片上），不写 DRAM（这是省带宽的地方）

3. Resolve 阶段：
   Tile Buffer 内容写入 DRAM（Framebuffer）
   → 每帧写一次，这是必须的

关键结论：Mali TBDR 省掉的是 inter-pass 的 Color Attachment 读写
但 Tiler Stream + Framebuffer Resolve 仍然是固定的 DRAM 带宽消耗
而且纹理采样（Fragment Shader 内）每一次 L2 miss 都走 DRAM
```

### Mali 的内存层次与 L2 命中率

```
Mali GPU 典型内存层次（以 Valhall 旗舰为例）：

L1 Cache（每个 EE 私有）：
  大小：约 16-64KB（具体型号不同）
  延迟：~4-8 cycles
  作用：Shader 变量缓存、指令缓存

L2 Cache（所有 EE 共享）：
  大小：
    Mali-G78（2020）：~1-2MB
    Mali-G715（2022）：~4MB
    Immortalis-G925（2024）：~8MB
  延迟：~20-40 cycles（命中时）
  作用：纹理缓存、Tiler Stream 缓冲

External DRAM（LPDDR5）：
  带宽：~50-80 GB/s（LPDDR5-6400，双通道）
  延迟：~200-400 cycles（L2 miss 后）
  这是性能瓶颈所在

L2 命中率的重要性：
  假设一个 Fragment Shader 每 fragment 采样 5 张纹理
  每张纹理 1024x1024，ASTC 4x4 → ~1MB per texture
  5 张纹理 = ~5MB 工作集

  Mali-G78 的 L2 只有 2MB
  → 5MB 工作集完全无法缓存 → L2 命中率极低 → 大量 DRAM 访问

  Mali-G715 的 L2 有 4MB
  → 部分场景能缓存主要纹理 → 命中率提升 → DRAM 访问减少
  
  Immortalis-G925 的 L2 有 8MB
  → 中等复杂度场景的纹理工作集可以大部分命中
```

### Mali 带宽需求高于 Adreno 的结构性原因

这是开发者经常忽视的对比点：

```
Adreno 的片上内存优势（以 Adreno 740 为例）：
  GMEM（Render Target 专用片上内存）：约 512KB–1MB per Render Target
  作用：在同一 Renderpass 内，Color/Depth Attachment 的读写完全在片上
  
  Adreno GMEM 的特殊性：
  它不是通用 L2，而是专门服务 Render Target 的高速内存
  尺寸大到可以容纳一个完整的 1080p Depth Buffer（~8MB 未压缩）
  → 深度测试、Stencil、MRT 写入全部在 GMEM 内完成

  对 DRAM 带宽的影响：
  Adreno 在 Render Pass 内的 Depth Prepass → GBuffer 写入 → Lighting 读取
  这整个流程的 Attachment 访问全在 GMEM，不走 DRAM
  → DRAM 带宽消耗主要来自纹理采样和最终 Resolve

Mali 的 Tile Buffer 对比：
  Mali 的 Tile Buffer 专门用于当前 Tile 的 Fragment 输出
  Tile 大小通常 16x16 或 32x32 像素
  → Tile Buffer 容量极小（几 KB）
  → 跨 Pass 的数据读取（如 Depth Prepass 的结果用于后续 Pass）必须通过 L2 或 DRAM

  实际含义：
  在 Deferred Rendering 管线中：
    GBuffer 写入 → Tile Buffer（片上，Mali 的 TBDR 优势）
    GBuffer 读取（Lighting Pass）→ 必须从 L2/DRAM 读（因为 Lighting Pass 是新 Pass）
    → 每帧 GBuffer 读取带宽 = 全屏像素 × GBuffer 字节数
    → 在 1080p，GBuffer（4 张 RGBA16F）= ~33MB / 帧

    对比 Forward+ on Adreno：
    深度测试在 GMEM，不走 DRAM
    → 带宽消耗明显低于 Deferred on Mali
```

这解释了一个实践现象：**Deferred Rendering 管线在 Mali 上的带宽消耗远高于 Adreno**，因为 GBuffer 读取是 Mali 的 L2 无法完全覆盖的大工作集访问。

带宽优化的实操建议（纹理格式选择、RT 格式、MSAA Resolve 时机）见 [GPU 渲染优化 02｜带宽优化]({{< relref "engine-notes/gpu-opt-02-bandwidth.md" >}})。

### 量化参考

在相似规格设备上（2022-2023 年旗舰），相同场景的 DRAM 读取量测量（来自 ARM Performance Studio 和 Snapdragon Profiler 数据对比，典型中等复杂度游戏场景）：

| 场景类型 | Mali-G715 DRAM 读取 | Adreno 730 DRAM 读取 | 比值 |
|---------|---------------------|----------------------|------|
| Forward，4 张纹理 | ~1.8 GB/s | ~1.2 GB/s | 1.5x |
| Deferred，GBuffer | ~3.5 GB/s | ~1.8 GB/s | 1.9x |
| 粒子系统（大量 Alpha） | ~2.2 GB/s | ~1.6 GB/s | 1.4x |

这些数字会随具体 Shader 复杂度和纹理规格变化，但 1.4–2x 的差距是典型范围。

---

## 四、mediump 在 Mali 上的精度行为

### 两种实现的根本差异

这是移动端跨厂商兼容性问题中最容易被忽视的一个：

```glsl
// Adreno（高通）的行为：
// mediump 通常被驱动提升为 highp（32-bit float）
// 原因：Adreno 的 ALU 本身就是 32-bit，mediump 的 "降精度" 没有硬件收益
// → 开发者写了 mediump，但实际执行是 32-bit
// → 精度问题被驱动掩盖，开发者通常不会发现问题
// → 但也没有性能收益（因为没有真正用 fp16 路径）

// Mali（ARM）Valhall 及以后的行为：
// mediump 严格按 16-bit float（fp16）处理
// fp16 规格：
//   精度：约 3 位有效十进制数
//   范围：±65504（最大绝对值）
//   最小正规数：约 6.1e-5
//   精度（相对误差）：约 1/1024 ≈ 0.1%

// 为什么 Mali 坚持严格 fp16 执行？
// Valhall EE 的 FMA 单元对 fp16 有 2x 吞吐：
//   fp32 FMA：1 cycle per instruction
//   fp16 FMA：2 instructions per cycle（packed pair）
// → 对 Mali 来说，mediump 是真实的性能优化，不是摆设
// → 如果驱动把 mediump 提升为 fp32，这个 2x 收益就丢失了
```

ARM 的工程立场是：严格执行 mediump 才能给开发者提供真实的性能激励。代价是：在 Adreno 上隐藏良好的精度 bug，在 Mali 上会暴露出来。

### fp16 精度问题的触发条件和修复

```glsl
// 安全用法：值域在 fp16 范围内，且不需要高精度
mediump float normalizedAlpha;   // 0.0 ~ 1.0，fp16 完全够用
mediump vec2  texcoord;          // 0.0 ~ 1.0 范围的 UV，安全
mediump vec3  normalWS;          // 归一化法线，分量在 -1 ~ 1，安全
mediump float roughness;         // PBR 参数，0 ~ 1，安全
mediump vec4  color;             // HDR 颜色通常在 0 ~ 64 范围内，多数情况安全

// 危险用法：值域超出 fp16 精度或范围

// 危险 1：大数值坐标
mediump float worldPosX;
// 世界坐标可能是 5000.0（5km 场景）
// fp16 在 4096 以上精度：每个整数步长 = 0.5 精度
// → 4096.3 和 4096.7 无法区分（都表示为 4096.5）
// → 产生顶点抖动（jitter）

// 修复：
highp float worldPosX;    // 世界坐标必须 highp

// 危险 2：大分辨率像素坐标
mediump vec2 screenUV;
// 1440p 宽度 = 2560 像素
// gl_FragCoord.x 范围 = 0 ~ 2560
// fp16 在 2048 以上精度：步长 = 0.25，但像素精度需要 < 0.5
// → 后缓冲区采样可能出现半像素偏移，导致屏幕空间效果错位（SSAO、TAA）

// 修复：
highp vec2 screenUV = gl_FragCoord.xy / vec2(screenWidth, screenHeight);
mediump vec2 tileUV = screenUV;  // 归一化后再降精度，0~1 范围安全

// 危险 3：深度值精度
mediump float depth;
// Depth Buffer 通常是 24-bit，但 fp16 的精度在远处物体会不足
// 导致 Z-Fighting

// 修复：
highp float linearDepth = ...;  // 深度计算用 highp
mediump float depthForEffect;   // 用于屏幕效果的粗略深度可以 mediump

// 危险 4：累积误差
mediump float sum = 0.0;
for (int i = 0; i < 64; i++) {
    sum += values[i];   // 每次加法有 fp16 舍入，64 次后累积误差可观
}
// → 使用 fp32 累积，最后转换
highp float sum32 = 0.0;
for (int i = 0; i < 64; i++) {
    sum32 += float(values[i]);
}
mediump float result = sum32;
```

### 检测 mediump 精度问题的方法

实践中最有效的检测方式：

1. 在 Android 设备上用 `ARM Performance Studio`（旧称 DS-5 Streamline）的 Shader Editor，开启 "Strict mediump" 模式强制模拟 fp16 行为。
2. 在 Unity 中，`PlayerSettings → Android → Graphics API` 选择 Vulkan，启用 `Shader Precision Model: Platform Default` vs `Strict`，对比两种模式的渲染结果。
3. 用 `malioc`（Mali Offline Compiler）分析 Shader，关注 fp16 指令比例。

---

## 五、Mali 的 Shader 编译特性

### 编译器的质量和代价

Mali 的 Shader 编译器（GLSL/SPIR-V → Mali ISA）在移动端 GPU 中属于优化激进、编译时间长的类型：

```
编译时间对比（典型复杂 PBR Fragment Shader，~200 指令）：

设备类型              | 首次编译时间（估计范围）
---------------------|----------------------
Adreno 740（骁龙 8 Gen 3）| 30–100ms
Adreno 650（骁龙 888）    | 50–150ms
Mali-G715（天玑 9200）    | 100–300ms
Mali-G78（天玑 9000）     | 150–400ms
Mali-G76（中端，2018）    | 300–800ms

场景：10 个新 Pipeline 同时触发编译（首次进入关卡）
Mali-G715：总编译时间可能达到 1–3 秒
→ 如果在游戏主循环中触发，会产生明显卡顿

Mali 编译时间长的原因：
  1. 寄存器分配优化更激进（减少 Spill，提高 Occupancy）
  2. 指令调度优化更深（为超标量 EE 最大化 ILP）
  3. fp16 降精度分析（识别哪些值可以安全转换为 fp16）
```

### 应对编译延迟的实践方案

```csharp
// Unity 方案一：ShaderVariantCollection + WarmUp
// 在 Loading 界面触发编译，避免游戏中卡顿

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void PrewarmShaders()
{
    // 在进入第一个游戏场景之前触发
    ShaderVariantCollection svc = Resources.Load<ShaderVariantCollection>(
        "Shaders/GameplayVariants");
    
    if (svc != null)
    {
        // WarmUp 会触发所有 Variant 的 Pipeline 编译
        // 在 Mali 上，这个调用可能阻塞 0.5–3 秒
        // 应在 Loading 界面的异步协程中调用，不要阻塞主线程
        svc.WarmUp();
    }
}

// Unity 方案二：异步 Shader Compilation（Unity 2021.2+）
// PlayerSettings → Player → Other Settings → Asynchronous Shader Compilation
// 开启后，首次使用时用 pink placeholder，后台编译
// 注意：这会导致首帧渲染错误（粉色），需要评估是否可接受

// Unity 方案三：Pipeline Cache（Vulkan）
// Vulkan 允许缓存 Pipeline 编译结果到磁盘
// 二次启动时直接加载 Cache，跳过 SPIR-V → ISA 编译
// Mali 驱动对 Pipeline Cache 的支持良好，命中节省 70-90% 编译时间
// Unity 的 Vulkan 后端默认开启 Pipeline Cache
// 确认：PlayerSettings → Android → Vulkan = 已选，Cache 目录在应用私有存储
```

### 用 Mali Offline Compiler 分析 Shader

Mali Offline Compiler（malioc）是 ARM 提供的免费工具，可以在不需要真机的情况下分析 Shader 的编译输出：

```bash
# 下载地址：
# https://developer.arm.com/tools-and-software/graphics-and-gaming/mali-offline-compiler

# 基本用法：分析一个 GLSL Fragment Shader
malioc --core Mali-G715 --fragment your_shader.frag

# 典型输出（截取关键字段）：
# 
# Shader Properties:
#   Threads per core:      16
#   Work registers:        32      ← 寄存器用量（影响 Occupancy）
#
# Performance Metrics (estimated):
#   Shortest path cycles:  8.3     ← 最优情况执行周期数
#   Longest  path cycles:  14.2    ← 最差分支路径
#   Total instruction cycles: 12.6
#
# Instruction Mix:
#   FMA instructions:      42%
#   CVT instructions:      8%
#   SFU instructions:      12%     ← sin/cos/log 等，开销大
#   Texture instructions:  18%
#   Load/Store:            20%
#
# fp16 Utilization:
#   % fp16 instructions:   67%     ← 越高越好
#   % fp32 instructions:   33%
#
# Bandwidth Estimate:
#   Texture bandwidth:     3.8 bytes/fragment
#   Total DRAM bandwidth:  4.2 bytes/fragment

# 关注点：
# 1. fp16 比例低（< 50%）：说明 mediump 用量不足，或编译器无法降精度
# 2. 寄存器用量 > 32：Occupancy 受限，Warp 切换延迟隐藏能力下降
# 3. SFU 比例高：复杂数学函数，在移动端昂贵
# 4. DRAM bandwidth 高：纹理采样过多或 Cache miss 率高
```

---

## 六、5th Gen 的新特性对开发者的含义

### Immortalis 的硬件光线追踪

5th Gen 中带有 "Immortalis" 名称的型号（G710、G715、G720、G925）集成了专用的 Ray Tracing 硬件单元：

```
Mali 硬件 RT 单元的架构：
  BVH 遍历单元：专用硬件加速 BVH（Bounding Volume Hierarchy）遍历
  射线-三角相交单元：硬件执行 Möller-Trumbore 相交测试
  API 支持：Vulkan Ray Tracing Extensions
    VK_KHR_acceleration_structure
    VK_KHR_ray_tracing_pipeline
    VK_KHR_ray_query（更轻量，可在普通 Compute/Fragment Shader 中使用）

性能参考（Immortalis-G715，峰值理论值）：
  RT 光线数：约 300–500M rays/second（相比 PC GPU 的 5–30G rays/second）
  → 移动端 RT 的绝对性能仍然受限

适合移动端 RT 的场景：
  低分辨率光照探针烘焙（动态 GI 替代方案）：
    每帧投射少量探针射线（如 8x8 grid，每探针 16 rays = 1024 rays/frame）
    在 Immortalis-G715 上：< 1ms
    可用于动态场景的低频 GI 更新

  半分辨率环境遮蔽（RT-AO）：
    以 1/4 分辨率渲染 AO（540p for 1080p 目标）
    每像素 4–8 条射线
    时间积累（TAA-AO 方式）
    在 Immortalis-G715 上：约 2–4ms，可接受

  静态反射（RT 烘焙成 Reflection Probe 替代）：
    场景加载时用 RT 实时计算 Reflection Probe 内容
    不在每帧执行，只在场景切换时执行一次
    → 比预烘焙有更好的动态性，比实时 RT 反射省功耗

不适合的场景：
  全屏路径追踪（Path Tracing）：
    移动设备的散热限制在 3–6W（GPU 部分）
    全屏 PT 需要 50–200W → 不可能
    即使降质量，持续 RT 会触发热降频（Thermal Throttling）
    → RT 适合作为场景内的特效增强，不适合作为主渲染路径

功耗建议：
  将 RT 功能设为画质档位选项
  在检测到 Immortalis 系列 GPU 时启用
  设置帧时间预算上限（如 RT 相关渲染时间 < 3ms/帧）
  监控 GPU 温度，必要时动态降低 RT 质量
```

### 更大的 L2 Cache：实际影响

```
L2 Cache 演进：
  Mali-G78（2020）：约 1–2MB
  Mali-G715（2022）：约 4MB
  Immortalis-G925（2024）：约 8MB

对纹理密集场景的影响：
  假设场景使用 8 张 512x512 ASTC 4x4 纹理（每张约 170KB）
  总工作集：~1.4MB

  在 G78 上：L2 = 2MB，刚好能容纳，命中率较高
  在 G715 上：L2 = 4MB，有余量，命中率稳定
  在 G925 上：L2 = 8MB，可以同时缓存更多纹理

对 4K 纹理密集游戏的影响：
  8 张 1024x1024 ASTC 4x4 纹理：每张约 680KB，总 ~5.4MB
  
  在 G78 上：超出 L2 容量，大量 DRAM 访问
  在 G715 上：勉强，命中率不稳定（取决于访问模式）
  在 G925 上：可以缓存，带宽压力降低 40–60%

实践建议：
  不能依赖 G925 的大 L2 来覆盖纹理预算溢出
  正确做法：合理使用 ASTC，控制纹理采样数
  L2 扩大是缓解因子，不是免费午餐
```

---

## 七、Mali 优化实践总结

以下是针对 Valhall 和 5th Gen 的优化优先级参考：

| 优化方向 | 适用场景 | 预期收益 | Mali 特殊注意事项 |
|---------|---------|----------|----------------|
| mediump 正确用量 | 所有 Shader | fp16 FMA 可达 2x 吞吐 | 严格避免用于世界坐标、大范围数值、累积计算 |
| ASTC 纹理格式 | 所有材质 | 降低 Texel Bandwidth 50–75% | 4x4 用于质量敏感纹理，6x6 / 8x8 用于细节纹理 |
| 减少纹理采样数 | GPU Fragment 密集场景 | 直接降低 L2 压力和 DRAM 读取 | Mali 对 L2 miss 更敏感，每次节省都有意义 |
| Shader Warmup | 首次进入场景 | 消除主循环中的编译卡顿 | Mali 编译比 Adreno 慢 2–4x，Warmup 窗口需更长 |
| 减少 Deferred GBuffer 读取 | Deferred 管线 | 降低最大 DRAM 带宽峰值 | GBuffer 读取在 Mali 上比 Adreno 贵，评估 Forward+ |
| Pipeline Cache（Vulkan） | 所有使用 Vulkan 的项目 | 二次启动跳过重新编译 | Mali 的 Cache 命中节省明显（70–90%） |
| 避免长依赖链 Shader | 复杂 PBR Shader | 利用 Valhall 超标量 ILP | Bifrost 无效，G77+ 开始受益 |
| RT 功能分档 | 5th Gen 设备 | 仅在 Immortalis 上启用 | 设置帧时间预算，监控热降频 |

### 设备分档建议

```
在 Android 性能分档中，Mali 设备的分类参考：

高端（旗舰，2022+）：
  Immortalis-G715 / G720 / G925
  天玑 9200 / 9300 系列
  建议档位：中高画质，可选 RT 特效

中高端（2020–2022 旗舰）：
  Mali-G78 / G710
  天玑 9000 / 1200 系列
  建议档位：中高画质，无 RT

中端（2019–2021）：
  Mali-G77 / G76 / G68
  天玑 800 / 900 系列
  建议档位：中画质，严格控制 Shader 复杂度

低端（2018 及更早 Valhall 前）：
  Mali-G71 / G72 / G76 Midgard / 早期 Bifrost
  建议档位：低画质，停用高复杂度后处理

检测方法：
  Android API：android.os.Build.HARDWARE（返回 "mali" 等标识符）
  更精确：使用 Vulkan Device Properties
    VkPhysicalDeviceProperties.deviceName 包含型号字符串
    解析型号后查询预设分档表
```

---

## 小结

Mali 从 Bifrost 到 Valhall 的核心转变是：从 Clause 驱动的串行 Quad 执行，到 Warp-based 动态调度的超标量执行。这个变化使得在 Valhall 上 fp16 优化和指令级并行（ILP）都有了真实的硬件收益，而不仅仅是理论上的。

带宽始终是 Mali 的主要约束。Tile Buffer 的设计使 Mali 在单 Pass 内节省了 Attachment 带宽，但纹理采样密集的场景仍然需要面对比 Adreno 更高的 L2 Miss 压力。5th Gen 的 L2 扩展缓解了这个问题，但根本的应对方式仍然是控制纹理工作集大小和采样次数。

mediump 在 Mali 上是真实的精度削减，不是驱动的摆设。这是跨厂商兼容性问题中最容易被 Adreno 优先的开发流程掩盖的陷阱。在 Mali 真机或 Strict mediump 模式下测试，是交付高质量移动端内容的必要步骤。
