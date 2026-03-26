---
title: "URP 深度平台 02｜多平台质量分级：三档配置的工程实现"
slug: "urp-platform-02-quality"
date: "2026-03-25"
description: "URP 多平台质量分级的完整工程实现：Quality Settings 与 URP Asset 的配对机制、iOS / Android 三档设备检测代码（含分档依据）、每档的具体配置差异表、Runtime 动态切换策略、以及什么时候应该从三档扩展到四档。"
tags:
  - "Unity"
  - "URP"
  - "质量分级"
  - "移动端"
  - "iOS"
  - "Android"
  - "性能优化"
series: "URP 深度"
weight: 1660
---
一套配置无法适配所有设备，质量分级是解决这个问题的工程方案。这篇讲三档分级的完整实现：为什么这样分、代码怎么写、配置差异在哪里、切换时机怎么选。

---

## Quality Settings 与 URP Asset 的配对机制

Unity 的质量分级系统由两层组成：

**Quality Level**（Edit → Project Settings → Quality）：每个 Level 对应一组 Unity 全局质量参数（Shadow Resolution、VSync、Particle Raycast Budget 等）。

**URP Pipeline Asset**：每个 Quality Level 可以绑定一个独立的 URP Asset，控制 URP 特有的渲染参数（MSAA、HDR、附加光上限、Renderer Settings 等）。

```
Quality Level "High"   → URP_High.asset   → MSAA 2x, Additional Lights 4, SSAO On
Quality Level "Medium" → URP_Medium.asset → MSAA Off, Additional Lights 2, SSAO Off
Quality Level "Low"    → URP_Low.asset    → MSAA Off, Additional Lights 0, SSAO Off
```

**为什么要用多个 URP Asset 而不是运行时动态改参数**：

URP Asset 里的部分参数会影响 Shader 变体编译——比如开启 SSAO 会编译包含 `_SCREEN_SPACE_OCCLUSION` 关键字的变体，关闭则不编译。如果在运行时直接改 `PipelineAsset.msaaSampleCount`，已编译的 Shader 变体不变，效果可能不符合预期。用独立 Asset 在切换时整体替换，Shader 变体和配置始终一致。

---

## 三档配置差异表

根据上一篇的配置分析，三档的核心差异如下：

| 配置项 | 高档 | 中档 | 低档 |
|--------|------|------|------|
| Render Scale | 1.0 | 0.85 | 0.75 |
| HDR | 开（R11G11B10）| 开（R11G11B10）| 关 |
| MSAA | 2x | Off | Off |
| Anti-Aliasing | MSAA + FXAA | FXAA | FXAA |
| Additional Lights | 逐像素 4盏 | 逐像素 2盏 | 逐顶点 |
| Soft Shadow | 开 | 关 | 关 |
| Shadow Distance | 80m | 50m | 30m |
| Shadow Cascade | 4 | 2 | 1 |
| Shadow Map 分辨率 | 2048 | 1024 | 512 |
| SSAO | Low 质量 | 关 | 关 |
| Bloom | 开（高质量）| 开（低质量）| 关 |
| Color Grading | 开 | 开 | 开（最低代价 LUT）|
| Native RenderPass | 开 | 开 | 开 |
| Depth Priming | Auto | Disabled | Disabled |

**Color Grading 在低档也保留**：低档用一张预烘焙的 LUT 贴图做色调，代价接近零，但能保持视觉风格一致性——这是视觉质量性价比最高的选项。

---

## 设备检测与分档

### iOS 检测

```csharp
#if UNITY_IOS
using UnityEngine.iOS;

public static QualityTier DetectIOSTier()
{
    DeviceGeneration gen = Device.generation;

    // 高档：A15 及以上（iPhone 13 / SE3 及以上）
    // 依据：A15 GPU 核心数从 4 升到 5，渲染性能比 A12 快约 2.5 倍
    if (gen >= DeviceGeneration.iPhone13)
        return QualityTier.High;

    // 中档：A13 / A14（iPhone 11 / 12 系列）
    // 依据：2019-2020 旗舰，仍广泛在用，能跑大多数效果但有代价
    if (gen >= DeviceGeneration.iPhone11)
        return QualityTier.Mid;

    // 低档：A12 及以下（iPhone XS / XR）
    // 依据：2018 年芯片，东南亚、印度等市场二手机比例较高
    return QualityTier.Low;
}
#endif
```

**为什么用 `Device.generation` 而不是 `graphicsMemorySize`**：iOS 的 `graphicsMemorySize` 返回值不可信，驱动会返回估算值或固定值，不反映实际 GPU 能力。`Device.generation` 枚举直接对应硬件代际，准确可靠。

---

### Android 检测

```csharp
#if UNITY_ANDROID

public static QualityTier DetectAndroidTier()
{
    string gpu = SystemInfo.graphicsDeviceName.ToLower();
    int vram = SystemInfo.graphicsMemorySize;

    // --- Qualcomm Adreno ---
    if (gpu.Contains("adreno"))
    {
        int model = ParseAdrenoModel(gpu);
        // 高档：Adreno 730+（Snapdragon 8 Gen 1，2022 旗舰）
        // 依据：GPU 性能比 Adreno 660 快 30%，完整 Vulkan 1.1 支持
        if (model >= 730) return QualityTier.High;
        // 中档：Adreno 610~720
        // 依据：Snapdragon 7 系列和 2020-2021 旗舰降级市场
        if (model >= 610) return QualityTier.Mid;
        return QualityTier.Low;
    }

    // --- ARM Mali ---
    if (gpu.Contains("mali"))
    {
        // 高档：Mali-G710 及以上（Dimensity 9000 系列）
        if (ContainsMaliModel(gpu, new[] { "g710", "g715", "g720" }))
            return QualityTier.High;
        // 中档：Mali-G57 / G68 / G77 / G78
        if (ContainsMaliModel(gpu, new[] { "g77", "g78", "g68", "g57" }))
            return QualityTier.Mid;
        return QualityTier.Low;
    }

    // --- 无法识别 GPU 时，用显存兜底 ---
    // 注意：Android 显存值部分设备不准，仅作兜底而非主判据
    if (vram >= 4096) return QualityTier.Mid;
    return QualityTier.Low;
}

private static int ParseAdrenoModel(string gpuName)
{
    // "adreno (tm) 730" → 730
    var match = System.Text.RegularExpressions.Regex.Match(gpuName, @"\d{3}");
    return match.Success ? int.Parse(match.Value) : 0;
}

private static bool ContainsMaliModel(string gpuName, string[] models)
{
    foreach (var m in models)
        if (gpuName.Contains(m)) return true;
    return false;
}

#endif
```

---

### 首次启动 Benchmark 兜底

GPU 型号识别覆盖不了所有情况——部分驱动返回"Unknown GPU"、旗舰机因散热降频表现如中端。加一个首次启动 Benchmark 作为兜底：

```csharp
public static IEnumerator RunBenchmark(System.Action<QualityTier> onComplete)
{
    // 预热：等两帧稳定
    yield return null;
    yield return null;

    float totalTime = 0f;
    int sampleCount = 90; // 约 3 秒（30fps）

    for (int i = 0; i < sampleCount; i++)
    {
        totalTime += Time.unscaledDeltaTime;
        yield return null;
    }

    float avgFrameTime = totalTime / sampleCount * 1000f; // ms

    QualityTier tier;
    if (avgFrameTime < 20f)       tier = QualityTier.High;    // > 50fps
    else if (avgFrameTime < 35f)  tier = QualityTier.Mid;     // 28~50fps
    else                          tier = QualityTier.Low;     // < 28fps

    onComplete(tier);
}
```

**Benchmark 在什么时候用**：
- 设备识别返回"未知 GPU"
- 或者作为对识别结果的验证（识别为 High 但 Benchmark 跑出 Mid 结果，以 Benchmark 为准）

Benchmark 结果存入 `PlayerPrefs`，后续启动直接读取，不重复跑。

---

## Runtime 切换质量档位

```csharp
public enum QualityTier { Low = 0, Mid = 1, High = 2 }

public class QualityManager : MonoBehaviour
{
    // Quality Level 名字需要和 Project Settings → Quality 里的名字一致
    private static readonly string[] QualityLevelNames = { "Low", "Medium", "High" };

    private const string TierPrefKey = "UserQualityTier";

    public static QualityTier CurrentTier { get; private set; }

    public static void Initialize()
    {
        QualityTier tier;

        if (PlayerPrefs.HasKey(TierPrefKey))
        {
            // 玩家手动选过档位，优先使用
            tier = (QualityTier)PlayerPrefs.GetInt(TierPrefKey);
        }
        else
        {
            // 首次启动：自动检测
            tier = DetectTier();
            PlayerPrefs.SetInt(TierPrefKey, (int)tier);
            PlayerPrefs.Save();
        }

        ApplyTier(tier);
    }

    public static void ApplyTier(QualityTier tier)
    {
        CurrentTier = tier;
        // 切换 Unity Quality Level（同时切换绑定的 URP Asset）
        QualitySettings.SetQualityLevel((int)tier, applyExpensiveChanges: true);
    }

    // 玩家手动选择（设置页调用）
    public static void SetTierManually(QualityTier tier)
    {
        PlayerPrefs.SetInt(TierPrefKey, (int)tier);
        PlayerPrefs.Save();
        ApplyTier(tier);
    }

    private static QualityTier DetectTier()
    {
#if UNITY_IOS
        return DetectIOSTier();
#elif UNITY_ANDROID
        return DetectAndroidTier();
#else
        return QualityTier.High;
#endif
    }
}
```

---

## 切换时机：不要在游戏中途切

`QualitySettings.SetQualityLevel()` 会触发 URP Asset 重新加载，部分 Shader 变体需要重新编译，**在游戏运行中切换会导致明显的卡顿（0.5~2 秒）**。

**推荐的切换时机**：

```
首次启动检测 → 进入加载界面 → ApplyTier → 开始游戏   ✅ 玩家感知不到
玩家在设置页改档位 → 提示"重启生效"或"切换场景后生效" ✅ 有明确预期
场景切换的 Loading 界面期间切换                        ✅ 加载时间掩盖卡顿
游戏战斗中动态切换                                    ❌ 明显卡顿
```

---

## Renderer Feature 的按档位开关

有些 Renderer Feature 只在高档开启。在 Feature 的 `AddRenderPasses` 里检查当前档位：

```csharp
public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
    // SSAO Feature 只在高档加入队列
    if (QualityManager.CurrentTier < QualityTier.High) return;

    renderer.EnqueuePass(_pass);
}
```

或者直接在 Inspector 里对不同 URP Asset 的 Renderer 启用/禁用对应 Feature：每个 URP Asset 可以引用不同的 Universal Renderer Data，不同 Renderer Data 启用不同的 Feature 组合。

---

## 给玩家的手动档位选项

无论自动检测多精准，都应该在设置页暴露手动选项：

```
画质设置
  ● 自动（推荐）
  ○ 低
  ○ 中
  ○ 高
```

原因：
- 同一机型散热差异大（新机 vs 老化机器），自动检测无法感知
- 部分玩家宁可帧率换画质，或画质换帧率
- 减少"我的手机跑不动"的客服投诉——玩家能自己降档

检测结果作为"自动"选项的默认值，玩家选了手动档位后存 `PlayerPrefs`，下次启动直接读取。

---

## 什么时候需要扩展到四档

三档框架在大多数纯移动端项目里够用。以下情况值得考虑加第四档：

**加 Ultra 档**：你的旗舰用户（A17 Pro、Snapdragon 8 Gen 3）在高档下 GPU 利用率只有 40%，有明显的视觉提升空间（实时软阴影、高密度粒子、Screen Space Reflection），且这部分用户占比 > 20%。

**加极低档**：你的低档在 Helio G85 / Adreno 506 等设备上帧率仍不稳定，但这批设备代表了你不能放弃的市场（东南亚、印度用户）。极低档做大幅降质（关所有后处理、动态阴影改 Blob Shadow、粒子数量砍半），专门保证这批设备流畅运行。

**加档的代价**：每多一档 = 多一轮完整 QA、多一套美术效果确认。评估时考虑团队能否维护这个成本。

---

## 小结

- Quality Level + URP Asset 配对：每档独立 Asset，Shader 变体和配置始终一致
- iOS 用 `Device.generation` 枚举分档（可靠），Android 用 GPU 名称匹配 + 显存兜底
- 首次启动 Benchmark 作为设备识别失败时的兜底，结果缓存到 `PlayerPrefs`
- 切换时机：加载界面或首次启动，游戏中途切换会卡顿
- 给玩家暴露手动档位选项，自动检测结果作为默认值
- 三档适合大多数纯移动端项目；旗舰用户占比大或需要覆盖极低端市场时考虑加第四档

URP 深度系列（16 篇）到这里全部完成。
