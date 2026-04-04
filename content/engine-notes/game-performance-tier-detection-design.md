---
date: "2026-04-05"
title: "从型号表到能力指纹：Android 与 PC 的分档判断怎么设计"
description: "设备分档不是查一张型号表，而是用硬件能力信号构建可靠的运行时判断。本篇讲清楚型号表的失效原因、能力指纹的设计逻辑，以及在 Android 碎片化和 PC GPU 长尾环境下，如何设计一套可维护的分档检测系统。"
slug: "game-performance-tier-detection-design"
weight: 183
featured: false
tags:
  - Platform
  - Mobile
  - PC
  - Performance
  - Android
  - Client
series: "游戏性能判断"
primary_series: "device-tiering"
series_order: 4
---

> 如果只用一句话概括这篇文章，我会这样说：型号表是把每台设备当作独立案例处理，能力指纹是把每台设备的硬件能力先抽象成几个维度，再映射到档位——后者写一次可以运行多年，前者两个月就会过时。

上一篇 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}}) 讲的是直觉层的东西：为什么同样的问题，在手机和 PC 上长相完全不同。

这篇要往下走一步，讲一个更具体的工程问题：

`你要让游戏在运行时知道自己跑在哪一档设备上，并据此做出渲染和资产配置的决策。这件事应该怎么设计？`

---

## 为什么型号表会出问题

大多数团队最开始都会建一张型号表：

```
if (device == "Samsung Galaxy S23") → 高档
if (device == "Redmi Note 11")      → 低档
if (device == "iPhone 13")          → 高档
...
```

这条路刚开始走得很顺。型号已知，行为已知，维护成本看起来很低。

但这个方案有三个根本性的问题，而且随着时间推移会越来越严重。

### 问题一：型号数量爆炸

Android 市场的设备型号数量，每年新增约 1000~1500 款（这还只是国内主流渠道）。
全球范围内，Unity 遥测数据里出现过的 Android 设备型号超过 10 万个。

型号表能维护多少行？实际上，团队通常维护 300~500 行之后就开始放弃——因为维护成本太高。
那么剩下没被覆盖的设备怎么办？通常落到"默认中档"或"默认低档"，造成大量误档。

### 问题二：同型号、不同表现

这是型号表最难解决的问题。

`同一个设备型号，不一定意味着同样的性能。`

具体来说，有几种常见情况：

**同款 SoC，不同 OEM 配置**：骁龙 8 Gen 2 同样出现在三星 Galaxy S23、小米 13、OPPO Find X6 上，但三星习惯用更激进的功率曲线换性能，小米习惯做散热优化，OPPO 某些机型为了薄度牺牲了持续性能。同样叫"骁龙 8 Gen 2"，实际的持续帧率可能相差 15~20%。

**同型号、不同批次（同频降频）**：部分机型的不同批次使用了不同频率或不同散热方案，外部型号字符串完全一致，内部硬件略有差异。

**同型号、不同区域版本**：部分机型有国行版和海外版，GPU 驱动版本不同，甚至帧率锁定规则不同。

**型号字符串本身不一致**：`SM-G991B`、`SM-G991B/DS`、`SM-G991N`、`samsung/SM-G991B`，这些都可能是同一款手机，但如果用字符串精确匹配，四个规则要写四次。

### 问题三：PC 的 GPU 长尾

PC 比 Android 还复杂一层。

显卡型号本身就是一个 SKU 矩阵：

- 同系列有 Ti / Super / Max-Q / mobile 版本差异
- 同型号有不同 TDP 的笔记本版（RTX 4070 Laptop 85W vs 115W，性能差距接近 30%）
- OEM 笔记本可能关了 MUX Switch，导致独显实际带宽受限
- 核显和独显命名规则完全不同（Intel Arc / Iris Xe / UHD 系列、AMD Radeon Graphics / RDNA2 等）

型号表对这种长尾几乎没有抵抗力。

---

## 能力指纹：换一个抽象层

能力指纹（Capability Fingerprint）的核心思路是：

`不问"这是什么设备"，而是问"这台设备能做什么"。`

把判断的基础从设备名字，转移到一组可量化的硬件能力信号。

这不是新概念——Android 的 `PerformanceClass` API 和 iOS 的设备代际判断本质上都是这套思路。但如果你的游戏需要自己设计分档逻辑，你需要先想清楚：

**哪些信号是可靠的，哪些信号需要校正，哪些信号完全不能信。**

---

## 可用的能力信号

### GPU 家族与档位

GPU 品牌 + 代际是最核心的信号。

在 Unity 里，`SystemInfo.graphicsDeviceName` 会返回 GPU 名字字符串，格式大概是：

```
"Adreno (TM) 740"
"Mali-G715 MC11"
"Apple A17 Pro GPU"
"NVIDIA GeForce RTX 4070"
"Intel(R) Iris(R) Xe Graphics"
```

这个字符串比设备型号稳定得多——同款 GPU 出现在不同 OEM 机型上，字符串基本一致。

实际处理时，通常做法是：

1. 提取 GPU 品牌（Adreno / Mali / Apple / NVIDIA / AMD / Intel）
2. 提取代际编号（740 / G715 / A17 / 4070 等）
3. 用代际编号映射到档位区间

具体的 GPU → 档位对应表，见下一篇 [主流芯片档位参考表]({{< relref "engine-notes/device-tier-chip-reference-four-tiers.md" >}})。

### 系统内存

`SystemInfo.systemMemorySize` 是判断档位的辅助信号，不能作为主判据。

| 内存 | 对应设备范围 |
|------|------------|
| ≤ 2GB | 极低档（Android 端已接近退市） |
| 3~4GB | 低档为主，部分中档 |
| 6~8GB | 中档为主 |
| 12GB+ | 高档 |

内存的限制：它只能向下限制档位，不能向上提升档位。一台 12GB 内存的机器不一定是高档——GPU 才是决定渲染性能的核心，内存只能排除掉不可能做到的事情（比如 2GB 机器跑高品质 4K 纹理）。

### 图形 API 版本

`SystemInfo.graphicsDeviceType` 和 `SystemInfo.graphicsShaderLevel` 可以判断设备支持的图形 API：

| 条件 | 含义 |
|------|------|
| Vulkan 支持 | Android 8.0+，中端以上硬件，较新驱动 |
| OpenGL ES 3.2 | 比 3.1 稍好，支持 Compute Shader |
| OpenGL ES 3.0 | 基础现代移动端 |
| OpenGL ES 2.0 | 极低档，2013 年以前的设备 |

API 版本是硬性门槛，不是性能指标——支持 Vulkan 不等于高档，但不支持 Vulkan 通常意味着中档以下。

### iOS：设备代际（最可靠）

iOS 上最可靠的判据是 `UnityEngine.iOS.Device.generation`，它直接返回 `DeviceGeneration` 枚举值，对应具体的 iPhone / iPad 代际，与 GPU 架构强绑定。

不建议在 iOS 上用 GPU 名字字符串做主判据，Apple 的 GPU 命名规则在不同版本里并不稳定。用 `Device.generation` → 映射到 A 系列芯片 → 映射到档位，这条链路最清晰。

### PC：独显 vs 核显

PC 上首先要区分独显和核显（集成显卡）。

`SystemInfo.graphicsMemorySize` 在 PC 上是有意义的：
- 独显一般 ≥ 4GB VRAM
- 核显（Intel Iris Xe）一般 ≤ 2GB（实际是共享内存，报告值不稳定）
- AMD APU 的核显报告值同样不稳定

更可靠的做法是用 GPU 名字字符串判断 GPU 系列：

```csharp
string gpuName = SystemInfo.graphicsDeviceName.ToLower();
bool isIntegrated = gpuName.Contains("intel") || 
                    gpuName.Contains("iris") ||
                    gpuName.Contains("uhd graphics") ||
                    (gpuName.Contains("amd") && gpuName.Contains("radeon graphics")); // APU核显
```

核显通常归入低档或中低档，独显再按显存和型号进一步判断。

---

## 设计一套可维护的分档系统

理解了可用信号之后，设计分档系统的原则可以总结成几条：

### 原则一：GPU 家族主判，其他信号辅判

分档决策树的主干应该是 GPU 家族 + 代际，其他信号（内存、API 版本、温度类别）只做向下修正，不做向上提升。

```
GPU 家族识别
  ↓
GPU 代际 → 初始档位
  ↓
系统内存检查 → 若内存不足，降档（例：初始高档 + 4GB内存 → 降至中档）
  ↓
API 支持检查 → 若不支持 Vulkan 且需要计算着色器功能 → 限制部分高端效果
  ↓
最终档位
```

### 原则二：默认偏保守

对没有被明确识别的设备，默认给低档，而不是中档或高档。

理由很简单：高档误给了低档机，游戏会崩溃或严重卡顿，玩家直接删游戏；低档误给了高档机，游戏跑得好但画面差，玩家可以手动在设置里调高，不会直接流失。

保守策略造成的体验损失，比激进策略造成的稳定性问题小得多。

### 原则三：允许玩家手动覆盖 + 云端热更正

纯本地的分档逻辑有两个弱点：
1. 新设备上市时，本地逻辑无法识别
2. 某些设备被错误归档，需要快速修正

实际工程里通常设计两层：

- **本地逻辑（保底）**：基于 GPU 字符串 + 内存 + API 版本的运行时判断，无需网络，100% 覆盖
- **云端档位表（覆盖）**：通过 Remote Config 下发一张轻量级的"特例表"，只记录和本地逻辑结论不一致的机型，优先级高于本地

```json
// 云端特例表（只记录例外，不替换全量逻辑）
{
  "overrides": [
    { "gpuFamily": "Adreno", "gpuTier": "630", "forceTier": "low",  "reason": "thermal_issue_oem_X" },
    { "deviceModel": "SM-G991B/DS", "forceTier": "high", "reason": "confirmed_stable" }
  ]
}
```

这样维护成本最低——本地逻辑处理 90% 的情况，云端只维护 10% 的特例。

### 原则四：提前在 QA 阶段建立档位验证流程

分档错误很难在开发阶段发现，因为开发机通常是高档机。

建议在 QA 流程里明确要求：

- 每次版本上线前，在覆盖四个档位的真实设备上跑冒烟测试
- 记录每台测试设备的 `SystemInfo.graphicsDeviceName` 和实际分配档位，存入 QA 文档
- 对档位边界设备（比如 A12 / Adreno 618 这类中低档临界点）重点测试

---

## Android 碎片化的额外处理

Android 需要额外处理两类问题：

### 驱动 Bug 的白名单

同款 GPU，不同驱动版本可能触发不同的渲染 Bug。例如 Mali 的某个驱动版本在特定条件下会产生 GPU Hang，Adreno 的某些老驱动不支持某些 Vulkan Extension。

这类问题不能靠档位判断解决，需要一张独立的"功能禁用白名单"：

```json
{
  "featureBlacklist": [
    { "gpuFamily": "Mali", "driverVersionBelow": "32.0", "disableFeature": "compute_skinning" },
    { "gpuFamily": "Adreno", "gpuTierBelow": "640", "disableFeature": "vulkan_rayquery" }
  ]
}
```

档位决定"用多少资源"，功能禁用表决定"这个功能开不开"，两套逻辑要分开维护。

### OEM 热管理差异

同款 SoC 在不同 OEM 机型上的持续性能可能相差 15~20%（原因见 [移动端硬件 02b｜持续性能]({{< relref "engine-notes/mobile-hardware-02b-sustained-performance.md" >}})）。

如果需要处理这个问题，最直接的手段是在游戏启动时做一次短暂的 GPU 性能基准测试（跑 2~3 帧简单 DrawCall，测量实际帧时间），而不是仅依赖静态的芯片档位。

这个方案的代价是启动时间增加 100~300ms，通常可以藏在 Loading 阶段。

---

## PC 的长尾处理

PC 最大的问题是"已知高端名字 + 未知配置"：RTX 4070 Laptop 有 85W / 100W / 115W 三种版本，性能差距可达 25%，但 `SystemInfo.graphicsDeviceName` 都只显示 `"NVIDIA GeForce RTX 4070 Laptop GPU"`。

处理策略：

1. **先按 GPU 名字识别到"家族+代际"**，给出初始档位
2. **用 VRAM 做向下限制**：同款 Laptop GPU 里，VRAM 少的版本通常是低 TDP 版
3. **PC 不做过细的档位划分**：PC 上通常三档（高/中/低）就够，不必像移动端一样细分到四档，因为 PC 玩家可以自己调设置
4. **提供画质设置界面**：PC 上最可靠的"分档"，往往是让玩家自选画质预设，检测逻辑给出一个初始推荐值，而不是强制锁定

---

## 小结

- **型号表失效的根本原因**：Android 型号爆炸（10 万+ 种）、同型号不同性能、型号字符串不一致；PC 同款 GPU 有多个 TDP 版本
- **能力指纹的核心**：用 GPU 家族 + 代际作为主判据，内存 / API 版本 / 核显标记作为辅助修正
- **iOS 最可靠的判据**：`Device.generation` 枚举，直接对应硬件代际
- **Android 主判据**：`graphicsDeviceName` 字符串中的 GPU 系列 + 代际编号
- **默认偏保守**：未识别设备给低档，不给中高档
- **本地逻辑 + 云端热更**：本地处理 90% 的情况，云端只维护特例覆盖表
- **档位≠功能开关**：驱动 Bug 的功能禁用表和档位表要分开维护

下一篇 [主流芯片档位参考表]({{< relref "engine-notes/device-tier-chip-reference-four-tiers.md" >}}) 给出 Apple / Qualcomm / MediaTek / Kirin 四家的具体档位对照表和 Unity 检测代码。
