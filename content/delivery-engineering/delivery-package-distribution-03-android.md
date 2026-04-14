---
title: "包体管理与分发 03｜Android 分发：AAB、Play Asset Delivery 与多渠道包"
slug: "delivery-package-distribution-03-android"
date: "2026-04-14"
description: "Google Play 要求 AAB 格式，基础模块有 150MB 上限。Play Asset Delivery 提供了三种分发模式。国内市场还要处理多渠道包。"
tags:
  - "Delivery Engineering"
  - "Package Management"
  - "Android"
  - "AAB"
  - "Play Asset Delivery"
series: "包体管理与分发"
primary_series: "delivery-package-distribution"
series_role: "article"
series_order: 30
weight: 530
delivery_layer: "platform"
delivery_volume: "V06"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

Android 平台的包体分发机制在过去几年发生了重大变化：Google Play 要求上传 AAB 而非 APK，引入了 Play Asset Delivery（PAD）作为大资源分发方案。同时国内市场不使用 Google Play，需要处理多渠道包分发。

## AAB 与 APK 的区别

**APK**（Android Package）是传统的 Android 应用包格式。开发者上传一个完整的 APK，所有用户下载同一个文件。包含了所有架构（ARM64 + ARMv7）、所有语言、所有屏幕密度的资源。

**AAB**（Android App Bundle）是 Google 推的新格式。开发者上传 AAB，Google Play 根据用户的设备信息动态生成该设备需要的 APK 子集：

- 只包含设备的 CPU 架构（ARM64 或 ARMv7，不是两个都包含）
- 只包含设备的屏幕密度资源
- 只包含设备的语言资源

**体积影响**：相比完整 APK，AAB 的设备专属 APK 通常小 15-30%。对用户来说，下载量显著减少。

**工程影响**：
- Google Play 要求新应用必须使用 AAB 格式
- AAB 的签名由 Google Play 管理（Google Play App Signing）
- 调试和内部分发仍然可以使用 APK

### AAB 基础模块的 150MB 限制

AAB 的基础模块（base module）有 150MB 的上限。超出后 Google Play 会拒绝上传。

基础模块包含：
- 应用代码（DEX + native libraries）
- 基础资源（res/ 和 assets/ 中的内容）
- AndroidManifest.xml

如果游戏的基础资源超过 150MB（大多数中型以上游戏都会超），就需要使用 Play Asset Delivery 把资源拆出基础模块。

## Play Asset Delivery（PAD）

PAD 是 Google 提供的大资源分发方案，将资源拆分为 Asset Pack，有三种分发模式：

| 模式 | 行为 | 大小限制 | 适用场景 |
|------|------|---------|---------|
| **install-time** | 和 APK 一起安装 | 1GB/pack, 总计无限制 | 必须在首次启动前就有的资源 |
| **fast-follow** | 安装后立即开始后台下载 | 512MB/pack | 启动后很快需要但不紧急的资源 |
| **on-demand** | 用户触发时才下载 | 512MB/pack | 按需内容（新关卡、新角色） |

### Unity 中的 PAD 集成

Unity 提供了 Google Play Asset Delivery 插件（`com.google.play.assetdelivery`）。在 Unity 中使用 PAD 需要：

1. 在 Build Settings 中启用 "Split Application Binary"
2. 将资源标记为不同的 Asset Pack
3. 用 PAD API 在运行时查询下载状态和请求下载

**工程注意点**：
- install-time 的 Asset Pack 在运行时和本地文件系统中的路径不同于 assets/ 目录——需要用 PAD API 获取正确路径
- fast-follow 和 on-demand 的 Asset Pack 下载可能失败（网络问题、用户取消），代码必须处理这些错误状态
- PAD 的 Asset Pack 和 Unity 的 AssetBundle 是不同的概念——PAD 是 Android 层面的分发机制，AssetBundle 是 Unity 层面的打包机制。两者可以组合使用

### PAD 与自建 CDN 的选择

| 维度 | PAD | 自建 CDN |
|------|-----|---------|
| 分发成本 | 免费（Google Play 提供） | 需要 CDN 费用 |
| 下载体验 | Google Play 优化的下载器 | 需要自建下载逻辑 |
| 更新灵活性 | 必须通过 Google Play 发版 | 随时更新 CDN 内容 |
| 国内适用 | 不适用（国内无 Google Play） | 适用 |
| 版本绑定 | 和 App 版本绑定 | 独立于 App 版本 |

**推荐策略**：海外用 PAD（install-time 放核心资源，on-demand 放按需内容），国内用自建 CDN。

## 国内多渠道包

国内 Android 市场没有统一的应用商店，需要向华为、小米、OPPO、vivo、应用宝等多个渠道分发。

### 渠道包的差异

多渠道包之间的差异通常包括：

- **渠道标识**：写入 APK 的渠道 ID，用于数据统计和支付路由
- **SDK 差异**：不同渠道要求集成不同的支付 SDK、登录 SDK、推送 SDK
- **签名差异**：部分渠道要求使用渠道自己的签名（但大多数接受开发者签名）
- **审核要求差异**：部分渠道有额外的审核要求（华为要求适配鸿蒙、部分渠道要求接入防沉迷）

### 多渠道包的工程化管理

**渠道标识注入**：不要为每个渠道重新打包。使用 V2 Signing Scheme 的 APK 可以在签名区块中写入渠道信息，不影响签名有效性。工具如 VasDolly、Walle 可以在不重新打包的情况下注入渠道标识。

**SDK 差异管理**：如果不同渠道需要不同的 SDK，通过编译开关或运行时配置切换，不要维护多套代码。

**构建管线**：CI 中只构建一次基础 APK，然后用工具批量注入渠道标识，产出多个渠道包。不要为每个渠道从头构建。

## 常见错误做法

**不测试 AAB 的设备专属 APK**。开发时用完整 APK 测试正常，但 AAB 在 Google Play 上生成的设备专属 APK 缺少了某些架构的 native library，特定设备崩溃。应该用 `bundletool` 生成设备专属 APK 进行测试。

**PAD 的 Asset Pack 没有处理下载失败**。on-demand 的 Asset Pack 下载失败后没有重试或降级逻辑，用户看到空白界面。

**每个渠道从头构建**。20 个渠道 × 30 分钟构建 = 10 小时。应该构建一次，批量注入渠道标识。

## 小结与检查清单

- [ ] 是否使用 AAB 格式提交 Google Play
- [ ] 基础模块是否控制在 150MB 以内
- [ ] 超出 150MB 的资源是否使用 PAD 拆分
- [ ] PAD 的 on-demand/fast-follow 是否处理了下载失败
- [ ] 国内渠道包是否使用渠道标识注入（而非从头构建）
- [ ] 是否用 bundletool 测试过设备专属 APK

---

**下一步应读**：[iOS 分发]({{< relref "delivery-engineering/delivery-package-distribution-04-ios.md" >}}) — iOS 平台的 App Thinning 和 On-Demand Resources

**扩展阅读**：[Android AAB/PAD 系列]({{< relref "engine-toolchain/unity-android-aab-pad-series-index.md" >}}) — Unity 中 AAB 和 PAD 集成的完整技术深挖
