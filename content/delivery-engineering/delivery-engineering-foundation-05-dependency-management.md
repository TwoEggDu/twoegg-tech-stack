---
title: "工程基建 05｜依赖管理与第三方集成——SDK、插件和平台库怎么管"
slug: "delivery-engineering-foundation-05-dependency-management"
date: "2026-04-14"
description: "第三方 SDK 是交付链路的外部依赖。版本怎么锁、冲突怎么解、平台专属库怎么隔离——管不住依赖，发版时就是在赌运气。"
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Dependency Management"
  - "SDK"
series: "工程基建"
primary_series: "delivery-engineering-foundation"
series_role: "article"
series_order: 50
weight: 250
delivery_layer: "principle"
delivery_volume: "V03"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L5"
---

## 这篇解决什么问题

一个商业游戏项目通常集成 10-30 个第三方 SDK 和插件：广告、支付、分析、Crash 上报、推送、社交登录、反作弊、音频中间件等。

每个 SDK 都是交付链路的外部依赖。管不住依赖，构建就可能因为版本冲突失败，发布就可能因为某个 SDK 的合规问题被审核驳回。

## 为什么这个问题重要

第三方依赖出问题的典型场景：

- 两个 SDK 都依赖了 OkHttp，但版本不同，Android 构建时 Gradle 报冲突
- 某个 SDK 升级后修改了 iOS Framework 的接口，Xcode 构建直接失败
- 广告 SDK 新版本收集了设备标识符，提交 App Store 审核被拒（隐私合规）
- 某个分析 SDK 内部使用了反射加载类，IL2CPP Stripping 后该类被裁掉，运行时崩溃
- 微信小游戏环境不支持某个 SDK 的原生插件，构建通过但运行时报错

这些问题的共同特征：**不是自己的代码出了问题，是依赖的外部代码出了问题，但责任在自己的交付链路上。**

## 本质是什么

依赖管理要解决四个核心问题：

### 问题一：版本锁定

"这次能构建成功，下次也能构建成功。"

如果依赖的版本不锁定，两次构建可能拉取到不同版本的 SDK，产出不同的构建产物。这直接违反了交付链路对"可重复构建"的要求。

**版本锁定的实施**：

| 包管理器 | 锁定方式 |
|---------|---------|
| UPM (Unity) | package-lock.json / Git tag / 嵌入到 Packages/ |
| NuGet (.NET) | packages.lock.json / 精确版本号 |
| Gradle (Android) | 精确版本号 + dependency locking |
| CocoaPods (iOS) | Podfile.lock |
| npm (Web) | package-lock.json |

**原则**：lock 文件必须入版本库。任何人拿到代码后，不加额外操作就能恢复完全一致的依赖树。

### 问题二：冲突解决

"两个依赖需要同一个库的不同版本。"

依赖冲突在 Android 平台最常见——因为 Java/Kotlin 生态的依赖链很长，传递依赖很容易冲突。

**冲突类型**：
- **版本冲突**：A 需要 OkHttp 3.x，B 需要 OkHttp 4.x
- **类冲突**：两个 AAR 包都包含了同一个类的不同实现
- **资源冲突**：两个 AAR 包都包含了同名的 Android 资源文件

**解决策略**：
- 优先升级冲突双方到兼容的版本
- 如果无法兼容，用 Gradle 的 `exclude` 或 `force` 明确选择一个版本
- 用 `resolutionStrategy` 统一管理传递依赖的版本
- 记录每一个强制版本选择的原因（否则下次升级时不知道为什么要锁这个版本）

### 问题三：平台隔离

"iOS 的 SDK、Android 的 SDK 和微信小游戏的 SDK 不应该互相干扰。"

很多 SDK 有平台专属的原生部分：

| SDK | iOS | Android | 微信小游戏 |
|-----|-----|---------|-----------|
| 广告 | iOS Framework | AAR | JS Bridge |
| 支付 | StoreKit | Google Billing | 微信支付 API |
| Crash 上报 | PLCrashReporter | Breakpad | wx.reportEvent |
| 推送 | APNs | FCM | 微信订阅消息 |

平台隔离的实施：

- 每个 SDK 的平台专属代码放在独立的目录，用条件编译或平台过滤控制
- 对外暴露统一的抽象接口（如 `IAdsService`），不同平台提供不同实现
- 构建时通过平台宏（`UNITY_IOS` / `UNITY_ANDROID` / `UNITY_WEBGL`）选择正确的实现

### 问题四：封装隔离

"业务代码不应该直接调用第三方 API。"

第三方 SDK 的 API 会随版本变化。如果业务代码到处直接调用 `Firebase.Analytics.LogEvent()`，那每次 Firebase 升级时都需要修改散布在整个项目的调用点。

**封装原则**：
- 每个第三方 SDK 用一个封装层包裹
- 封装层对外暴露稳定的接口（`IAnalytics.LogEvent()`）
- 业务代码只依赖封装层的接口，不依赖第三方 API
- 替换 SDK 时只修改封装层的实现，业务代码不改

封装的编译域边界：
```
业务代码（asmdef）→ 引用 → 接口层（asmdef）← 实现 ← SDK 封装层（asmdef）→ 引用 → 第三方 SDK
```

## SDK 集成的工程化检查

在交付链路中，第三方 SDK 应该经过以下检查：

**引入时**：
- [ ] 是否有明确的版本号和 changelog
- [ ] 是否通过平台合规审查（隐私政策、数据收集声明）
- [ ] 是否与现有 SDK 有依赖冲突
- [ ] 是否在三端都有可用版本（或明确哪个端不支持）

**升级时**：
- [ ] changelog 中是否有 breaking change
- [ ] 升级后三端是否都能构建成功
- [ ] 升级后冒烟测试是否通过
- [ ] 升级后 Stripping 是否裁掉了新版本需要的类型

**发布时**：
- [ ] SDK 版本是否锁定（不是 `latest`）
- [ ] 隐私合规声明是否与当前 SDK 版本匹配
- [ ] 平台审核要求是否满足（iOS ATT、Android 数据安全表、微信隐私协议）

## 常见错误做法

**SDK 直接复制到项目里不记录版本号**。半年后想升级时，不知道当前用的是哪个版本。每个 SDK 必须有版本记录文件。

**所有 SDK 一起升级**。多个 SDK 同时升级时，如果出了问题无法判断是哪个 SDK 造成的。SDK 升级应该逐个进行，每次升级后验证。

**不测试 SDK 在 Release 配置下的行为**。很多 SDK 在 Debug 和 Release 配置下的行为不同（日志级别、数据上报频率、崩溃处理方式）。验证必须在 Release 配置下进行。

## 小结与检查清单

- [ ] 所有第三方 SDK 是否有版本锁定（lock 文件入版本库）
- [ ] 依赖冲突是否有明确的解决方案和记录
- [ ] 平台专属 SDK 代码是否通过条件编译隔离
- [ ] 业务代码是否通过封装层间接使用第三方 API
- [ ] SDK 升级是否逐个进行并验证
- [ ] 隐私合规声明是否与当前 SDK 版本一致

---

**下一步应读**：[条件编译与多端共用]({{< relref "delivery-engineering/delivery-engineering-foundation-06-conditional-compilation.md" >}}) — 一套代码怎么产出三端：平台宏、Feature Flag、编译开关

**扩展阅读**：[游戏质量保障体系：兼容性质量]({{< relref "code-quality/game-quality-06-compatibility-quality.md" >}}) — 第三方 SDK 兼容性在整体质量保障体系中的位置
