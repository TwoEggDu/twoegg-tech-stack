---
title: "Android 厂商定制｜调度器、内存回收与省电策略如何影响游戏"
slug: "android-oem-customization"
date: "2026-03-28"
description: "同一颗骁龙 8 Gen 3，在小米、三星、OPPO 上的游戏表现可以差很多。原因不在硬件，在于各厂商对 CPU 调度器、内存回收策略和省电机制的深度修改。本篇拆解各厂商定制层的核心差异，以及这些修改如何造成帧率不稳、后台重启和推送失效。"
tags:
  - "Android"
  - "厂商定制"
  - "ROM"
  - "性能优化"
  - "移动端"
series: "移动端硬件与优化"
weight: 2250
---

Android 是开源的，但各厂商拿到 AOSP（Android 开源项目）源码后都会深度修改。这些修改覆盖内核调度器、内存管理、后台进程策略，最终导致同一个游戏在不同品牌手机上行为差异显著。

---

## 修改发生在哪一层

```
Android 软件栈（从上到下）：

App 层（游戏）
  ↓
Android Framework（系统 API）
  ↓
HAL（硬件抽象层）← 厂商修改重灾区
  ↓
Linux Kernel   ← 厂商修改重灾区（调度器、内存管理）
  ↓
硬件（SoC）

厂商主要修改：
  内核层：EAS 调度器参数、LMK（Low Memory Killer）阈值、ZRAM 配置
  Framework 层：后台进程管控、省电白名单、性能模式触发逻辑
  系统 App 层：Game Booster、电池管家、后台清理工具
```

---

## CPU 调度器：帧率不稳的根源

### 原生 Android 的调度机制

Android 使用 **EAS（Energy-Aware Scheduler）**，根据负载和热状态动态决定线程跑在哪个核心：

```
EAS 的基本逻辑：
  低负载线程 → 调度到小核（省电）
  高负载线程 → 调度到大核或超大核（高性能）

  参数：
  schedutil governor：根据 CPU 使用率动态调节频率
  uclamp（utilization clamping）：可以为特定线程设置频率下限
```

游戏的 GameThread 通常会被调度到超大核（Prime Core），但这依赖调度器能"识别"出这是高优先级线程。

### 厂商如何修改调度器

**小米（HyperOS / MIUI）**

小米的 **Frame Scheduling** 机制会识别白名单内的游戏，提前预测下一帧的计算量，提前拉高 CPU 频率：

```
白名单内游戏：
  系统检测到游戏线程的周期性唤醒模式
  → 在帧开始前提频（减少频率爬坡延迟）
  → GameThread 稳定跑在 Prime Core

白名单外游戏：
  系统按普通 App 处理
  → 频率响应慢（schedutil 有延迟）
  → 偶发帧时间尖峰（频率来不及拉高）
  → 表现：帧率整体达标，但 P99 帧时间差
```

小米的白名单由系统预置，也支持厂商联调申请加入。

**三星（One UI）**

三星的 **Game Booster** 有更激进的干预：

```
Game Booster 开启时：
  ✅ 提高 GameThread 的 CPU 调度优先级
  ✅ 减少后台 App 的 CPU 竞争
  ⚠ 但 Game Booster 会拦截 setFrameRate() API 的部分调用
    → 游戏设置 120fps 目标，Game Booster 可能强制限制在 60fps
    → 原因：三星在 One UI 5.0 之前把 setFrameRate 和显示刷新率的协商做了额外封装

  One UI 6.0+ 改善了这个问题，但低版本 ROM 仍然存在
```

**OPPO / OnePlus（ColorOS / OxygenOS）**

OPPO 的 **HyperBoost** 以"帧率稳定引擎"为核心：

```
HyperBoost 的工作方式：
  分析游戏的历史帧时间分布
  → 预测下一帧是否会超时
  → 提前提高 CPU/GPU 频率（而不是等超时后再提频）

白名单问题：
  HyperBoost 只对特定游戏（合作游戏）启用预测模式
  非白名单游戏：使用保守的频率策略，帧率波动较大

检测方法（adb）：
  adb shell dumpsys game_mode_service
  → 可以看到当前设备的 Game Mode 状态和白名单列表（部分厂商会暴露）
```

**华为（EMUI / HarmonyOS）**

华为的调度修改较为激进，且在鸿蒙化后与标准 Android 行为差异更大：

```
主要影响：
  GPU Turbo 技术：在帧率即将下降时提前干预，效果因设备而异
  多任务管理：后台任务调度与 AOSP 行为差异大

  开发者注意：
  HarmonyOS 4.0+ 对部分 Android API 的支持有差异
  建议在华为设备上单独做兼容性测试
```

---

## 内存回收：后台重启的根本原因

### 原生 Android 的 LMK 机制

Android 用 **LMK（Low Memory Killer）** 管理内存压力，按进程优先级从低到高依次终止：

```
原生 LMK 触发阈值（参考值，各设备不同）：

可用内存 > 512MB：不杀进程
可用内存 200-512MB：杀缓存进程（已关闭的 App）
可用内存 100-200MB：杀后台服务进程
可用内存 < 100MB：开始杀后台可见进程
可用内存 < 50MB：最后手段，杀前台进程（极少发生）
```

### 厂商如何修改 LMK

**小米（MIUI）的"二次清理"**

MIUI 在原生 LMK 之上，增加了一个额外的"安全中心"主动清理层：

```
MIUI 的后台清理逻辑：
  定时扫描（约每 5-10 分钟）：清理"不活跃"后台进程
  内存占用超过阈值：主动回收内存，不等 LMK 触发
  用户锁屏后 N 分钟：清理非白名单的后台进程

  对游戏的影响：
  游戏切后台 → 用户去刷了几分钟微博 → 切回游戏
  → MIUI 已经把游戏进程清理了 → 游戏重新加载（冷启动）
  → 表现：进度丢失，或重新进入 Loading 界面
```

**Vivo（OriginOS）**

Vivo 的后台限制策略在国内厂商中偏激进：

```
后台冻结机制：
  App 切后台后，一定时间内被"冻结"（进程存活但不执行代码）
  → 对于有后台逻辑的游戏（定时存档、后台下载）影响明显
  → 重新进入游戏时有短暂卡顿（进程从冻结态恢复）
```

### 应对策略：请求加入省电白名单

各厂商都有"省电白名单"，加入后可以豁免部分后台限制：

```java
// 引导用户手动将游戏加入省电白名单
// （合规做法：弹出说明 + 跳转到系统设置）

Intent intent = new Intent();
String packageName = getPackageName();
PowerManager pm = (PowerManager) getSystemService(POWER_SERVICE);

if (!pm.isIgnoringBatteryOptimizations(packageName)) {
    // 用户尚未豁免，弹出引导
    intent.setAction(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS);
    intent.setData(Uri.parse("package:" + packageName));
    startActivity(intent);
}
```

**注意**：Google Play 政策要求 `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` 权限只能在合理场景下使用（如实时导航、VOIP），游戏直接申请可能被审核拒绝。更稳妥的做法是引导用户手动进入系统设置操作。

---

## 省电管控：推送失效和网络受限

### 后台网络限制

各厂商对后台网络访问的限制程度不同：

```
厂商后台网络限制对比（严格程度）：

最严格：
  华为 EMUI / HarmonyOS
  Vivo OriginOS
  → 非白名单 App 在后台几乎无法发起网络请求
  → 表现：游戏切后台后，心跳包无法发送，服务端判定下线

中等：
  小米 MIUI / HyperOS
  OPPO ColorOS
  → 有配额限制，部分网络请求可以通过

最宽松（接近原生）：
  三星 One UI（国际版）
  Google Pixel（原生 Android）
  OnePlus OxygenOS（国际版）
```

**对多人游戏的影响**：

```
场景：玩家游戏时接了个电话（游戏切后台约 3 分钟）

华为设备（严格限制）：
  → 后台网络被切断 → 心跳包超时 → 服务端踢下线
  → 玩家挂电话切回游戏 → 提示「连接已断开，请重新登录」

三星设备（宽松）：
  → 后台网络保持 → 心跳包正常 → 重新进入游戏继续

应对方案：
  接入 FCM（Firebase Cloud Messaging）或厂商推送 SDK
  → 依靠推送通道唤醒游戏，而不是保持长连接
  → 厂商对自家推送 SDK 的后台网络访问不做限制
```

### 厂商推送 SDK 接入

国内发行的游戏，必须接入各厂商的推送 SDK 才能保证通知到达率：

```
主要厂商推送 SDK：
  小米：MiPush
  华为：HMS Push（鸿蒙设备没有 GMS，必须用 HMS）
  OPPO：OPush
  Vivo：VPush
  三星：Samsung Push（国内可以用 FCM）

  典型做法：
  国内版游戏：接入以上全部（或通过第三方推送聚合 SDK，如个推、极光）
  海外版游戏：只接入 FCM（Google Firebase）
```

---

## 厂商识别与差异化处理

在代码里检测当前设备的厂商和 ROM 版本：

```java
// 基础厂商信息
String manufacturer = Build.MANUFACTURER; // 如 "Xiaomi", "samsung", "OPPO"
String brand = Build.BRAND;               // 如 "Redmi", "Galaxy", "realme"
String model = Build.MODEL;               // 具体型号

// ROM 版本（厂商自定义字段，不标准）
String miuiVersion = getSystemProperty("ro.miui.ui.version.name");     // MIUI
String emuiVersion = getSystemProperty("ro.build.version.emui");       // EMUI
String colorOsVersion = getSystemProperty("ro.build.version.opporom"); // ColorOS

private static String getSystemProperty(String key) {
    try {
        Class<?> systemProperties = Class.forName("android.os.SystemProperties");
        return (String) systemProperties.getMethod("get", String.class)
            .invoke(null, key);
    } catch (Exception e) {
        return "";
    }
}
```

**实际使用建议**：不要对厂商做硬编码的特殊处理，而是检测行为并适配：

```java
// 不推荐：
if (Build.MANUFACTURER.equals("Xiaomi")) {
    doMiuiSpecificThing();
}

// 推荐：检测行为，而不是检测厂商
// 比如检测是否支持精确的帧率控制
if (Build.VERSION.SDK_INT >= 31) {
    // 尝试使用 setFrameRate，再验证实际帧率是否符合预期
    surface.setFrameRate(120f, Surface.FRAME_RATE_COMPATIBILITY_FIXED_SOURCE);
}
```

---

## 测试矩阵建议

只有旗舰机型的测试是不够的，覆盖不同厂商和定制程度：

| 优先级 | 设备类型 | 原因 |
|--------|---------|------|
| 必测 | 小米旗舰（HyperOS） | 国内市场份额第一，调度器定制深 |
| 必测 | 华为中高端（HarmonyOS） | 没有 GMS，网络和推送行为完全不同 |
| 必测 | OPPO/一加旗舰（ColorOS） | HyperBoost 白名单影响帧率 |
| 必测 | 三星 Galaxy（One UI） | Game Booster 的 setFrameRate 干预 |
| 建议 | 原生 Android（Pixel） | 作为基准对照，排查是厂商问题还是自身问题 |
| 建议 | 低端机（骁龙 6xx + MIUI） | 验证 OOM 和后台重启 |
