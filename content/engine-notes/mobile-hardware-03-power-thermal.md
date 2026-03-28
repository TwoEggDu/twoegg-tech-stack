---
title: "移动端硬件 03｜功耗与发热：降频模型、帧率稳定性与热管控策略"
slug: "mobile-hardware-03-power-thermal"
date: "2026-03-28"
description: "移动端游戏的核心约束不是峰值性能，而是持续性能。SoC 在散热极限下会触发降频，导致帧率从 60fps 跌到 40fps。理解热管控模型，才能设计出在长时间游戏中保持帧率稳定的策略。"
tags:
  - "Mobile"
  - "Hardware"
  - "Thermal"
  - "功耗"
  - "性能优化"
series: "移动端硬件与优化"
weight: 2025
---

移动端游戏的性能不是一个固定值，而是一条随时间下降的曲线。手机没有风扇，散热完全依靠金属中框和导热材料。当 SoC 持续高负载时，温度上升 → 触发降频 → 性能骤降。理解这个机制，才能设计出"跑 30 分钟不掉帧"的游戏。

---

## 功耗与散热的基本模型

```
功耗 = CPU功耗 + GPU功耗 + 内存功耗 + 显示功耗 + 其他

骁龙 8 Gen 3 典型场景：
  峰值功耗（极限压测）：~12W
  重度游戏（高画质）：~8-10W
  中度游戏（中画质）：~5-6W
  手机散热极限（无风扇）：~4-6W（取决于外壳材质和环境温度）
```

**结论**：旗舰手机只能在峰值功耗下运行约 3-5 分钟，之后散热跟不上，必须降频。

---

## 降频（Throttling）模型

以骁龙 8 Gen 2 为例（实测数据，不同设备有差异）：

```
CPU 温度  →  CPU 频率上限
< 65°C   →  100%（3.2 GHz Prime Core）
65-70°C  →  90%（~2.8 GHz）
70-75°C  →  75%（~2.4 GHz）
75-80°C  →  60%（~1.9 GHz）
> 80°C   →  50% 或强制迁移到 Gold Core（~1.5 GHz）

GPU 温度  →  GPU 频率上限
< 65°C   →  100%（680 MHz）
70-75°C  →  70%（476 MHz）
> 80°C   →  50%（340 MHz）
```

**实际游戏表现**：
- 启动后 5 分钟内：60fps 稳定
- 5-10 分钟：偶发帧率抖动（温度接近阈值）
- 10-20 分钟：帧率可能跌到 40-50fps（CPU/GPU 轮番降频）
- 20 分钟后：维持在降频后的稳定帧率（通常 30-45fps）

---

## 如何观察降频

**Android（Perfetto / adb）**：
```bash
# 实时查看 CPU 频率
adb shell cat /sys/devices/system/cpu/cpu7/cpufreq/scaling_cur_freq

# 查看 CPU 温度
adb shell cat /sys/class/thermal/thermal_zone*/temp | head -20

# Perfetto trace（推荐）：
# 在 Perfetto UI 中录制 cpu_freq + thermal 数据源
# 可以看到频率随时间的变化曲线
```

**Snapdragon Profiler**：
- Realtime Capture → 添加 `CPU Frequency` 和 `GPU Frequency` Counter
- 可以实时看到频率曲线，温度升高时频率下降的拐点清晰可见

**iOS**：
- Apple 没有公开频率 API，但 Xcode Instruments 的 Energy 模块可以看到功耗变化
- 实测 iPhone 15 Pro：长时间游戏约 15 分钟后 GPU 性能降约 20%（A17 Pro 比骁龙更抗降频）

---

## 帧率稳定性 vs 峰值帧率

**错误直觉**：帧率越高越好
**正确直觉**：帧率越稳定越好

```
场景 A：帧率在 60-30fps 之间波动（平均 50fps）
场景 B：帧率稳定在 35fps

玩家体验：场景 B >> 场景 A
原因：帧率波动（帧时间抖动）比绝对帧率低更影响体验
     20ms → 50ms 的帧时间跳变 会让玩家感到明显卡顿
     30ms → 30ms 的稳定帧时间 虽然只有 33fps，但流畅感更好
```

**量化指标**：P95 帧时间（95% 的帧都在这个时间内完成）
- 60fps 目标：P95 帧时间 ≤ 18ms（允许 5% 的帧超时）
- 30fps 目标：P95 帧时间 ≤ 36ms

```csharp
// Unity 帧时间监控（用于自动降质决策）
public class FrameTimeMonitor : MonoBehaviour
{
    private Queue<float> _frameTimes = new Queue<float>();
    private const int SampleCount = 60; // 采样 60 帧

    void Update()
    {
        _frameTimes.Enqueue(Time.unscaledDeltaTime * 1000f); // ms
        if (_frameTimes.Count > SampleCount)
            _frameTimes.Dequeue();
    }

    public float GetP95FrameTime()
    {
        var sorted = _frameTimes.OrderBy(x => x).ToArray();
        int idx = Mathf.FloorToInt(sorted.Length * 0.95f);
        return sorted[idx];
    }
}
```

---

## Android Thermal API

Android 10+ 提供了官方 Thermal API，可以在游戏运行时获取热状态：

```java
// Android Java 层
PowerManager powerManager = (PowerManager) getSystemService(Context.POWER_SERVICE);

// getThermalHeadroom(30) = 预测 30 秒后的热余量（0-1）
// 1.0 = 完全安全；0.0 = 即将触发降频
float headroom = powerManager.getThermalHeadroom(30);

if (headroom < 0.5f) {
    // 主动降低画质
    sendToUnity("THERMAL_WARN");
}
```

```csharp
// Unity C# 侧接收 Android 的热状态
public class ThermalManager : MonoBehaviour
{
    public enum ThermalLevel { Normal, Warm, Hot, Critical }
    public ThermalLevel CurrentLevel { get; private set; }

    // Android 回调
    public void OnThermalStatusChanged(string status)
    {
        switch (status)
        {
            case "NORMAL":   CurrentLevel = ThermalLevel.Normal;   ApplyQuality(3); break;
            case "THERMAL_WARN": CurrentLevel = ThermalLevel.Warm; ApplyQuality(2); break;
            case "HOT":      CurrentLevel = ThermalLevel.Hot;      ApplyQuality(1); break;
            case "CRITICAL": CurrentLevel = ThermalLevel.Critical; ApplyQuality(0); break;
        }
    }

    void ApplyQuality(int level)
    {
        switch (level)
        {
            case 3: // 正常：高画质
                QualitySettings.SetQualityLevel(3);
                Screen.SetResolution(1920, 1080, true);
                break;
            case 2: // 温热：降分辨率
                Screen.SetResolution(1440, 810, true);
                break;
            case 1: // 热：关阴影 + 降分辨率
                QualitySettings.shadows = ShadowQuality.Disable;
                Screen.SetResolution(1280, 720, true);
                break;
            case 0: // 危险：极简模式
                QualitySettings.SetQualityLevel(0);
                Application.targetFrameRate = 30;
                break;
        }
    }
}
```

---

## Unity Adaptive Performance 插件

Unity 官方提供 Adaptive Performance 包（支持三星 Samsung 和 Adreno 设备）：

```csharp
using UnityEngine.AdaptivePerformance;

public class AdaptiveQualityController : MonoBehaviour
{
    IAdaptivePerformance ap;

    void Start()
    {
        ap = Holder.Instance;
        if (ap == null || !ap.Active) return;

        // 订阅热状态变化
        ap.ThermalStatus.ThermalEvent += OnThermalEvent;
    }

    void OnThermalEvent(ThermalMetrics metrics)
    {
        switch (metrics.WarningLevel)
        {
            case WarningLevel.NoWarning:
                // 可以提升画质
                break;
            case WarningLevel.ThrottlingImminent:
                // 即将降频，主动降低负载
                ReduceQuality();
                break;
            case WarningLevel.Throttling:
                // 正在降频
                ReduceQualityMore();
                break;
        }
    }
}
```

---

## iOS 的热管控差异

Apple 的热管控比 Android 更激进但更透明：

- **没有公开的温度/频率 API**：iOS 不允许 App 读取 CPU 温度
- **系统自主降频**：iOS 会在用户无感知的情况下降频（A 系列芯片有更大的降频空间）
- **实测表现**：iPhone 15 Pro 在重度游戏 20 分钟后，GPU 性能约降 15-20%（比骁龙 8 Gen 2 更抗降频，但仍然会降）
- **可用信号**：`Application.lowMemory`（内存压力）是间接信号；帧时间突增也是降频信号

```csharp
// iOS 降频检测（间接方法）
public class PerformanceWatcher : MonoBehaviour
{
    float _baselineFrameTime = -1f;

    void Update()
    {
        float ft = Time.unscaledDeltaTime;

        // 建立基准（前 5 秒）
        if (Time.time < 5f)
        {
            _baselineFrameTime = Mathf.Lerp(_baselineFrameTime < 0 ? ft : _baselineFrameTime, ft, 0.1f);
            return;
        }

        // 帧时间超过基准 50% 持续 3 秒 → 怀疑降频
        if (ft > _baselineFrameTime * 1.5f)
        {
            // 触发降质
        }
    }
}
```

---

## 实践建议

| 目标 | 做法 |
|------|------|
| 30 分钟不掉帧 | 用 30fps 而不是 60fps 作为稳定目标 |
| 减少热量产生 | 降低 GPU 负载（优先优化 GPU） |
| 主动适应降频 | 接入 Thermal API / Adaptive Performance |
| 测试热稳定性 | 在环境温度 25°C 的房间，连续游戏 30 分钟，记录帧率曲线 |
| 拒绝"手机抓着热"的优化 | 目标是设备表面温度 ≤ 42°C（用户舒适阈值） |
