---
date: "2026-03-28"
title: "主流芯片档位参考表：四档判断依据与代码"
description: "覆盖 Apple、Qualcomm、MediaTek、Huawei 四家芯片的高/中/低/极低档划分，每档给出具体判断依据（GPU 核心数、带宽、API 支持），并提供更新后的 Unity 检测代码。支持底线到 iPhone 6（A8）/ Adreno 505 级别设备。"
slug: "device-tier-chip-reference-four-tiers"
weight: 192
featured: false
tags:
  - Platform
  - Mobile
  - Performance
  - Compatibility
  - Client
series: "机型分档"
series_order: 5
last_reviewed: "2026-04-17"
---

三档结构在设备池两端收窄的市场里是够用的。但如果游戏要覆盖全球市场、底线低至 2014 年 iPhone 6 / Adreno 505 级别设备、同时又要追求旗舰机 60fps，三档装不下这个性能跨度。

<!-- DATA-TODO: "iPhone 6 到 iPhone 16 Pro 差距 15-20 倍"需要标注来源。建议：用 GFXBench Metal Manhattan 3.1 Off-Screen 的分数对比（Kishonti 官方数据库有完整历代数据），或引用 Apple 发布会公开的"较前代提升 X 倍"连乘结果，并在括号里加来源"（数据来源：GFXBench Manhattan 3.1 离屏分数对比）"。 -->
iPhone 6（A8）到 iPhone 16 Pro（A18 Pro）的 GPU 性能差距大约是 15-20 倍。用三档描述这个范围，要么低档太宽（A8 和 A11 行为差距巨大），要么高档太宽（A15 和 A18 视觉潜力完全不同）。

本篇给出的**四档结构**：

| 档位 | 体验目标 | 代表 iOS | 代表 Android |
|------|---------|----------|-------------|
| **极低档** | 能跑不崩，30fps 锁帧 | iPhone 6 / A8 | Adreno 505–506 / Helio G35 |
| **低档** | 稳定 30fps，基础效果 | iPhone 6s–8 / A9–A11 | Adreno 508–616 / Helio G85 |
| **中档** | 稳定 30fps 或低负载 60fps | iPhone XS–12 / A12–A14 | Adreno 618–660 / 天玑 700–800 |
| **高档** | 目标 60fps，全效果开 | iPhone 13+ / A15+ | Adreno 730+ / 天玑 9000+ |

---

## Apple 芯片档位

Apple 的判据是 **GPU 核心数 × 架构代际**。`graphicsMemorySize` 在 iOS 上不可信，驱动返回估算值；`Device.generation` 枚举直接映射硬件代际，是最可靠的判断入口。

<!-- DATA-TODO: Apple 芯片档位表的"档位"列需要标注划档依据。建议：每档给一个 GFXBench 分数范围（如"高档 = Manhattan 3.1 Off-Screen ≥ 200 fps"），或在表前一段加"划档依据：Metal GPU Family 代际 + GFXBench Manhattan 3.1 分数区间 + 实际上线游戏的 60fps 可达性"。否则读者不知道"A14 中档"的 boundary 是怎么定的。"A11 比 A12 慢约 35%"的数字也需要出处，可加"来源：GFXBench Manhattan 3.1 Off-Screen 分数对比"。 -->
### 档位表

| 芯片 | GPU 核心数 | 代表机型 | 档位 | 关键判据 |
|------|-----------|---------|------|---------|
| A18 Pro | 6 核（新架构）| iPhone 16 Pro/Max | 高档 | 硬件光追，带宽最高 |
| A18 | 5 核 | iPhone 16 / 16 Plus | 高档 | |
| A17 Pro | 6 核 | iPhone 15 Pro/Max | 高档 | |
| A16 | 5 核 | iPhone 15 / 15 Plus，iPhone 14 Pro | 高档 | |
| A15 | 5 核（Pro 版）/ 4 核（标准版）| iPhone 13–14，SE 3rd | 高档 | SE 3rd 用 4 核版，低于 iPhone 13 |
| A14 | 4 核 | iPhone 12 系列，iPad Air 4 | 中档 | |
| A13 | 4 核 | iPhone 11 系列，SE 2nd | 中档 | SE 2nd 落在中档，但接近低档边界 |
| A12 | 4 核 | iPhone XS / XR，SE 2nd 早期 | 中档底线 | 中低档分界点 |
| A11 | 3 核（高性能架构）| iPhone 8 / X | 低档 | 架构代际跨度大，比 A12 慢约 35% |
| A10 Fusion | 6 核（老架构）| iPhone 7 | 低档 | 老架构核心多但效率低 |
| A9 | 6 核 | iPhone 6s，SE 1st | 低档 | |
| A8 | 4 核 | iPhone 6 / 6 Plus | **极低档** | PowerVR GX6450，Metal 支持有限 |

**SE 系列特别注意**：iPhone SE 各代芯片跨度极大，不能按"SE"这个名字统一分档——

- SE 1st（2016）：A9 → 低档
- SE 2nd（2020）：A13 → 中档
- SE 3rd（2022）：A15（4 核版）→ 高档
- SE 4th（2025）：A16 → 高档

**iPad 注意**：同芯片的 iPad 因屏幕分辨率更高，实际渲染压力更大，建议比手机版降一档配置，或单独检测并降低 Render Scale。

### iOS 检测代码（四档版）

```csharp
#if UNITY_IOS
using UnityEngine.iOS;

public static QualityTier DetectIOSTier()
{
    DeviceGeneration gen = Device.generation;

    // 高档：A15 及以上（iPhone 13 / SE3 / iPhone 14+ / iPhone 15+ / iPhone 16+）
    // 依据：5–6 GPU 核心，带宽 ≥ 68 GB/s，Metal GPU Family 8+
    if (gen >= DeviceGeneration.iPhone13)
        return QualityTier.High;

    // 中档：A12–A14（iPhone XS / 11 / 12 系列，SE 2nd）
    // 依据：4 核新架构，Metal GPU Family 5–7，比 A11 快约 35–80%
    if (gen >= DeviceGeneration.iPhoneXS)
        return QualityTier.Mid;

    // 低档：A9–A11（iPhone 6s / 7 / 8 / SE 1st / X）
    // 依据：仍能稳定 30fps，但后处理代价高，内存 2GB
    if (gen >= DeviceGeneration.iPhone6S)
        return QualityTier.Low;

    // 极低档：A8 及以下（iPhone 6 / 6 Plus）
    // 依据：PowerVR GX6450，Metal GPU Family 2，1GB RAM，Manhattan ~18fps
    // 目标：不崩溃，30fps 锁帧，关闭所有后处理
    return QualityTier.UltraLow;
}
#endif
```

---

## Qualcomm Adreno 档位

Adreno 是 Android 市场覆盖最广的 GPU 家族，判断依据是**型号数字 + 代际系列**。Adreno 型号前两位代表代际，后两位代表档次（数字越大性能越高）。

<!-- DATA-TODO: 下表里"Adreno 730 比 Adreno 660 快 30%"、"Manhattan ~15fps / ~12fps"等具体数字需要标注来源。建议：引用 GFXBench / Geekerwan 公开评测，或在表下加一行"划档依据：GFXBench Manhattan 3.1 Off-Screen 分数 + Vulkan 1.x 支持级别 + 实际上线游戏帧率表现"。"骁龙 888 热功耗大、实际不如 8 Gen 1 稳定"这个结论也需要数据支撑——引用持续性能评测（如 Geekerwan 的 3DMark Wild Life Extreme Stress Test）。 -->
### 档位表

| GPU | 对应 SoC | 代表机型 | 档位 | 关键判据 |
|-----|---------|---------|------|---------|
| Adreno 750 | 骁龙 8 Gen 3 | 小米 14, 一加 12 | 高档 | 光追支持，Vulkan 1.3 |
| Adreno 740 | 骁龙 8 Gen 2 | 小米 13, OPPO Find X6 | 高档 | |
| Adreno 730 | 骁龙 8 Gen 1 / 8+ Gen 1 | 小米 12, 三星 S22 | 高档 | 比 Adreno 660 快 30%，Vulkan 1.1 完整支持 |
| Adreno 660 | 骁龙 888 / 888+ | 小米 11, OPPO Find X3 | **中高档** | 性能接近高档，但热功耗大，旗舰机热降频明显 |
| Adreno 650 | 骁龙 865 / 870 | 小米 10, 一加 8T | 中档 | |
| Adreno 643 / 644 | 骁龙 778G / 7 Gen 1 | 小米 11 Lite 5G NE | 中档 | |
| Adreno 642 | 骁龙 870 部分版本 | Redmi K50 部分版本 | 中档 | |
| Adreno 619 | 骁龙 695 / 750G | Redmi Note 系列主力 | 中档 | 中国大陆最常见中档芯片之一 |
| Adreno 618 | 骁龙 765G | Redmi K30 5G | 中档入门 | |
| Adreno 616 | 骁龙 730G | Redmi K20 | 低中档边界 | |
| Adreno 612 | 骁龙 720G | Redmi Note 9 Pro | 低档 | |
| Adreno 610 | 骁龙 662 / 460 | Redmi 9T / 10C | 低档 | |
| Adreno 508 | 骁龙 450 | Redmi 6 | 低档 | |
| Adreno 506 | 骁龙 625 / 635 | Redmi Note 4 | **极低档上线** | Manhattan ~15fps |
| Adreno 505 | 骁龙 430 | Redmi 4A | 极低档 | Manhattan ~12fps，Android 最低可支持基线之一 |
| Adreno 512 | 骁龙 439 | Redmi 7A | 极低档 | 比 505 更弱 |

**Adreno 660 单独说明**：骁龙 888 是 2021 年旗舰，GPU 性能接近高档，但该芯片热功耗极大，长时运行时频繁触发降频，实际游戏中表现往往不如骁龙 8 Gen 1（Adreno 730）稳定。建议配置策略上归高档但加热机降档保护，而不是默认高档一直保持最高配置。

### Android Qualcomm 检测代码

```csharp
private static QualityTier DetectAdreno(string gpu)
{
    int model = ParseAdrenoModel(gpu); // 解析出三位数字，如 "Adreno (TM) 730" → 730

    // 高档：Adreno 730+（骁龙 8 Gen 1，2022 旗舰）
    // 依据：完整 Vulkan 1.1，GPU 性能比 Adreno 660 快约 30%
    if (model >= 730) return QualityTier.High;

    // 中档：Adreno 618–729
    // 依据：骁龙 7 系列和 2019–2021 旗舰降价机型，能稳定 30fps，部分场景 60fps
    if (model >= 618) return QualityTier.Mid;

    // 低档：Adreno 505–617
    // 依据：骁龙 6 系列和 4 系列，稳定 30fps 但预算有限
    if (model >= 505) return QualityTier.Low;

    // 极低档：Adreno 504 及以下
    return QualityTier.UltraLow;
}

private static int ParseAdrenoModel(string gpuName)
{
    // "Adreno (TM) 730" / "Adreno 730" 等格式
    var match = System.Text.RegularExpressions.Regex.Match(gpuName, @"(\d{3})");
    return match.Success ? int.Parse(match.Groups[1].Value) : 0;
}
```

---

## MediaTek 天玑 / Helio 档位

<!-- DATA-TODO: MediaTek 档位表里"Mali-G710 MP10 比 G78 快约 35%"等具体数字需要标注来源。建议引用 GFXBench Aztec Ruins High Tier Off-Screen 分数或 Geekerwan 评测，在表下加"数据来源：GFXBench 公开数据库 + 公开评测整理"。 -->
MediaTek 在中国大陆中端机和东南亚入门机市场占有率极高。天玑系列（Dimensity）面向 5G 中高端，Helio G 系列面向游戏入门机，Helio P/A 系列面向超低端。

GPU 家族两条线：
- **Immortalis-G**：天玑 9000 系以上，支持硬件光追
- **Mali-G**：主力中高端，710 / 715 / 720 是高档，610 / 57 / 68 是中低档
- **Mali-T / IMG PowerVR**：极低端设备

### 档位表

| GPU | 对应 SoC | 档位 | 关键判据 |
|-----|---------|------|---------|
| Immortalis-G720 | 天玑 9300 | 高档 | 硬件光追，12 核 |
| Immortalis-G715 | 天玑 9200 / 9200+ | 高档 | 硬件光追，10 核 |
| Mali-G710 MP10 | 天玑 9000 / 9000+ | 高档 | 10 核，比 G78 快约 35% |
| Mali-G715 | 天玑 8300 | 高档低线 | |
| Mali-G610 MP6 | 天玑 8200 / 7200 | 中档 | 6 核，Vulkan 1.1 稳定 |
| Mali-G610 MP4 | 天玑 1200 / 8050 | 中档 | |
| Mali-G77 MC9 | 天玑 1000+ | 中档 | |
| Mali-G57 MC5 | 天玑 700 / 900 | 中档入门 | 5 核，预算比 G610 紧 |
| Mali-G57 MC4 | Helio G99 / G96 | 中档入门 | 主要在中低端 4G 机型 |
| Mali-G52 MC2 | Helio G85 / G80 | 低档 | 东南亚主流低端机 |
| Mali-G52 MC1 | Helio G35 / G37 | **极低档** | 单核 G52，Manhattan ~18fps |
| IMG PowerVR GE8320 | Helio A22 / P22 | 极低档 | |
| Mali-T830 | Helio P10 / P20 | 极低档 | 2016–2017 老设备 |

### Android MediaTek 检测代码

```csharp
private static QualityTier DetectMali(string gpu)
{
    // 高档：Immortalis-G7xx / Mali-G710 及以上
    if (gpu.Contains("immortalis") ||
        ContainsMaliModel(gpu, new[] { "g720", "g715", "g710" }))
        return QualityTier.High;

    // 中档：Mali-G610 / G77 / G57（天玑系列主力）
    if (ContainsMaliModel(gpu, new[] { "g610", "g77", "g78", "g68", "g57" }))
        return QualityTier.Mid;

    // 低档：Mali-G52（Helio G85/G80）
    if (ContainsMaliModel(gpu, new[] { "g52" }))
        return QualityTier.Low;

    // 极低档：Mali-T / G51 / IMG PowerVR
    return QualityTier.UltraLow;
}

private static bool ContainsMaliModel(string gpu, string[] models)
{
    foreach (var m in models)
        if (gpu.Contains(m)) return true;
    return false;
}
```

**注意**：`SystemInfo.graphicsDeviceName` 在 MediaTek 设备上返回格式一般是 `"Mali-G57 MC5"` 或 `"Immortalis-G715"`，直接字符串匹配即可，不需要正则解析型号数字。

---

## Huawei Kirin 档位

Kirin 是华为手机的自研 SoC，GPU 部分主要来自 ARM Mali（早期）和自研 Turbo T 架构（麒麟 9000S 之后）。华为设备在中国大陆是一个不可忽视的市场，但 Kirin 检测有几个坑需要单独处理。

### 最重要的一个坑：Kirin 9000S ≠ Kirin 9000

<!-- DATA-TODO: "Kirin 9000S GPU 性能实测大幅低于 9000"这句话需要有实测支撑，否则是本文最容易被挑战的断言（涉及华为，读者敏感）。建议：引用 Geekerwan 2023-09 发布的麒麟 9000S 深度评测（B 站公开可引用）或极客湾 GFXBench 对比数据，给出具体分数对比（例如 Kirin 9000 Manhattan 3.1 Off-Screen = X fps vs Kirin 9000S = Y fps）。在"实测"二字后加 `[^geekerwan-kirin9000s]` 脚注链接到来源。 -->
| 芯片 | GPU | 档位 | 说明 |
|------|-----|------|------|
| Kirin 9000 | Mali-G78 MP24 | 高档 | 2020 年，Mate 40 Pro，24 核 G78，性能极强 |
| **Kirin 9000S** | 自研 Turbo T（4 核）| **中档** | 2023 年，Mate 60 Pro，GPU 性能实测大幅低于 9000，约等于中高档 Android |
| Kirin 9010 | 自研 Turbo T 改进版 | 中高档 | 2024 年，比 9000S 提升有限 |

Kirin 9000S 在宣传上是"新一代旗舰"，但 GPU 性能实际上比 Kirin 9000 退步明显。如果按型号名判断，会错误地将 Mate 60 Pro 判成高档，而它的 GPU 实际只有中档水平。

### 完整档位表

| 芯片 | GPU | 档位 | 代表机型 |
|------|-----|------|---------|
| Kirin 9000 | Mali-G78 MP24 | 高档 | Mate 40 Pro |
| Kirin 990 5G | Mali-G76 MP16 | 中高档 | Mate 30 Pro |
| Kirin 9000S / 9010 | Turbo T（4核）| 中档 | Mate 60 Pro / P70 |
| Kirin 985 | Mali-G77 MP8 | 中档 | Nova 8 Pro |
| Kirin 820 | Mali-G57 MP6 | 中档 | Nova 7 SE |
| Kirin 810 | Mali-G52 MP6 | 低中档 | Nova 5 Pro |
| Kirin 710 | Mali-G51 MP4 | 低档 | Nova 3i |
| Kirin 659 / 655 | Mali-T830 MP2 | 极低档 | 荣耀系列老机型 |

### Kirin / 华为设备检测代码

Kirin 设备的识别比较特殊。`SystemInfo.graphicsDeviceName` 会返回 GPU 名称（Mali-GXX 或 Turbo T），可以复用 Mali 检测逻辑，但需要特别处理 Turbo T：

```csharp
private static QualityTier DetectKirinOrHuawei(string gpu)
{
    // Kirin 9000S / 9010 的自研 GPU 标识
    // 注意：这类设备 graphicsDeviceName 可能返回 "Turbo T" 或厂商自定义字符串
    if (gpu.Contains("turbo t"))
        return QualityTier.Mid; // 不要判成高档，实际 GPU 性能只有中档

    // Kirin 9000 使用 Mali-G78 MP24，走 Mali 检测分支
    // Kirin 990 5G 使用 Mali-G76，中高档边界，这里保守归中档
    if (ContainsMaliModel(gpu, new[] { "g78" }))
        return QualityTier.High;

    if (ContainsMaliModel(gpu, new[] { "g76", "g77" }))
        return QualityTier.Mid;

    if (ContainsMaliModel(gpu, new[] { "g57", "g52" }))
        return QualityTier.Low;

    return QualityTier.UltraLow;
}
```

实际工程中建议在 Kirin 9000S 上做专项测试：用 GFXBench 或内部 benchmark 跑一次，把实测帧率作为分档的校准依据，不要只凭 GPU 名称字符串。

---

## 完整检测入口

```csharp
public enum QualityTier { UltraLow, Low, Mid, High }

public static QualityTier DetectDeviceTier()
{
#if UNITY_IOS
    return DetectIOSTier();
#elif UNITY_ANDROID
    return DetectAndroidTier();
#else
    return QualityTier.High; // PC 默认高档，编辑器内也用高档
#endif
}

#if UNITY_ANDROID
private static QualityTier DetectAndroidTier()
{
    string gpu = SystemInfo.graphicsDeviceName.ToLower();
    int vram = SystemInfo.graphicsMemorySize;

    if (gpu.Contains("adreno"))
        return DetectAdreno(gpu);

    if (gpu.Contains("immortalis") || gpu.Contains("mali"))
        return DetectMali(gpu);

    if (gpu.Contains("turbo t"))
        return QualityTier.Mid; // Kirin 9000S / 9010

    // 兜底：无法识别 GPU 时用显存估算
    // Android 显存值部分设备不准，仅作最后手段
    if (vram >= 3000) return QualityTier.Mid;
    if (vram >= 1500) return QualityTier.Low;
    return QualityTier.UltraLow;
}
#endif
```

---

## 三档还是四档，怎么决定

极低档不是必须的，但在以下情况下强烈建议单独设立：

- **底线设备性能跨度大**：iOS 底线低于 A11（iPhone 6/6s/7），或 Android 底线低于 Adreno 610
- **目标市场包含东南亚 / 印度 / 中东**：这些地区二手低端机比例高，Helio G35 / Adreno 505 存量不小
- **游戏类型对内存敏感**：极低档设备普遍只有 1–2GB RAM，贴图加载逻辑必须单独控制

如果底线只是"能跑不崩"而不是"保证体验"，极低档的配置策略非常简单：关闭所有非必要渲染特性、锁定 30fps、只保留核心 UI 和角色。这类极低档几乎不需要维护独立的美术资产，只需要一套更激进的配置开关。

---

## 延伸阅读

如果你需要把这套档位判断接进资产预算，下一步是看：
[每档资产规格清单：贴图压缩、LOD 与包体分层]({{< relref "performance/device-tier-asset-spec-texture-and-package.md" >}})

如果你需要把档位判断接进线上治理（遥测、灰度、回滚），可以看：
[机型分档怎样接线上：遥测回写、Remote Config、灰度与回滚]({{< relref "rendering/urp-platform-03-online-governance.md" >}})
