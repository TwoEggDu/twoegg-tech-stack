---
title: "平台发布 04｜Android 发布专项——Google Play、测试轨道与国内多渠道"
slug: "delivery-platform-publishing-04-android"
date: "2026-04-14"
description: "Google Play 提供了多轨道测试和分阶段发布的完整机制。国内市场没有统一商店，多渠道发布需要额外的工程管理。"
tags:
  - "Delivery Engineering"
  - "Platform Publishing"
  - "Android"
  - "Google Play"
series: "平台发布"
primary_series: "delivery-platform-publishing"
series_role: "article"
series_order: 40
weight: 840
delivery_layer: "platform"
delivery_volume: "V09"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## Google Play 发布流程

```
1. 上传 AAB（Google Play Console / CI 自动化）
2. 选择轨道（内部测试 / 封闭测试 / 开放测试 / 正式版）
3. 填写版本信息（Release Notes、数据安全表）
4. 提交审核（自动审核 + 人工审核）
5. 审核通过 → 发布到选定轨道
```

### 四轨道测试体系

Google Play 的轨道机制比 iOS 的 TestFlight 更完整：

| 轨道 | 用途 | 人数限制 | 审核 | 对外可见 |
|------|------|---------|------|---------|
| **内部测试** | 开发团队日常测试 | 100 人 | 无 | 否 |
| **封闭测试** | 邀请制外部测试 | 无限制（邮件邀请） | Beta 审核 | 否 |
| **开放测试** | 公开 Beta 测试 | 无限制 | Beta 审核 | 是（商店可见） |
| **正式版** | 生产发布 | 全部用户 | 完整审核 | 是 |

**工程建议**：CI 每次构建自动上传到内部测试轨道。QA 验证通过后提升到封闭测试。最终验证通过后发布到正式版。

### Staged Rollout（分阶段发布）

Google Play 正式版支持分阶段发布——先推送给一定比例的用户：

```
5% → 观察 24h → 20% → 观察 24h → 50% → 观察 24h → 100%
```

分阶段发布期间可以随时暂停或回滚。这是 Android 平台原生支持的灰度能力——比 iOS 的"审核通过后全量"灵活得多。

**暂停 vs 回滚**：
- **暂停**：停止向新用户推送，已更新用户不受影响
- **回滚**：回到上一个版本。但和 iOS 一样，已更新的用户不会自动回退

### 国内多渠道发布

国内 Android 不走 Google Play，需要向多个渠道商店提交：

| 渠道 | 特点 |
|------|------|
| 华为 AppGallery | 需要 AGC 控制台，有自己的审核标准，鸿蒙适配要求 |
| 小米应用商店 | 审核快，但对权限使用要求严格 |
| OPPO / vivo | 各自的开发者平台，部分要求集成渠道 SDK |
| 应用宝 | 腾讯系，有自己的分发逻辑 |
| TapTap | 游戏垂类商店，审核侧重游戏内容 |

**多渠道发布的工程化**：

1. **渠道包生成**：V06-03 已覆盖——一次构建 + 批量注入渠道 ID
2. **各渠道的审核文档和截图**：不同渠道要求可能不同，需要统一管理
3. **发布协调**：20 个渠道不可能同时提审同时通过，需要管理发布进度
4. **版本追踪**：每个渠道的当前版本、审核状态、用户量需要集中看板

**自动化工具**：部分第三方平台（如 fastlane 的 supply 模块）支持 Google Play 的自动上传。国内渠道通常需要自研或使用 CI 脚本调用各渠道的 API。

### Android 审核要点

| 要点 | 说明 |
|------|------|
| 数据安全表 | 必须声明收集了什么数据、怎么使用 |
| 权限声明 | 每个权限必须有正当理由 |
| targetSdkVersion | 必须满足 Google Play 的最低要求 |
| 64 位要求 | 必须包含 ARM64 |
| 广告 SDK 合规 | 广告 SDK 的数据收集必须符合 Google 政策 |

## 小结

- [ ] CI 是否自动上传到 Google Play 内部测试轨道
- [ ] 正式发布是否使用 Staged Rollout（分阶段）
- [ ] 国内渠道包是否通过批量注入生成（不逐个构建）
- [ ] 多渠道发布进度是否有集中追踪
- [ ] targetSdkVersion 是否满足最新要求

---

**下一步应读**：[微信小游戏发布专项]({{< relref "delivery-engineering/delivery-platform-publishing-05-wechat.md" >}})
