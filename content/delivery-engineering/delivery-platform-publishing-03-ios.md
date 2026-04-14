---
title: "平台发布 03｜iOS 发布专项——App Store Connect、审核与 TestFlight"
slug: "delivery-platform-publishing-03-ios"
date: "2026-04-14"
description: "iOS 发布的核心流程：App Store Connect 提审、审核规则和常见拒审原因、TestFlight 内测分发、加急审核和版本回撤。"
tags:
  - "Delivery Engineering"
  - "Platform Publishing"
  - "iOS"
  - "App Store"
series: "平台发布"
primary_series: "delivery-platform-publishing"
series_role: "article"
series_order: 30
weight: 830
delivery_layer: "platform"
delivery_volume: "V09"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## App Store Connect 发布流程

```
1. 上传 IPA（Xcode / Transporter / CI 自动化）
2. App Store Connect 自动处理（Processing，通常 10-30 分钟）
3. 填写版本信息（Release Notes、截图、隐私标签）
4. 提交审核（Submit for Review）
5. 审核（Review，1-3 天）
6. 审核通过 → 手动发布 或 自动发布
```

### 上传自动化

推荐使用 **fastlane** 或 **Xcode 命令行工具** 在 CI 中自动上传：

```bash
# fastlane 上传
fastlane deliver --ipa build/game.ipa --skip_metadata --skip_screenshots

# 或 altool（Apple 原生工具）
xcrun altool --upload-app -f build/game.ipa -t ios \
  --apiKey $API_KEY_ID --apiIssuer $API_ISSUER_ID
```

### 常见审核拒审原因

| 拒审原因 | 频率 | 预防 |
|---------|------|------|
| **隐私声明不完整** | 高 | 每个权限（相机/位置/IDFA）必须有使用说明 |
| **崩溃或严重 Bug** | 高 | 提审前在 TestFlight 上做完整冒烟 |
| **内容不当** | 中 | 审核年龄分级是否匹配实际内容 |
| **元数据不一致** | 中 | 截图和实际功能不一致 |
| **热更新违规** | 低 | 不在审核说明中提及"动态下载代码" |
| **缺少登录选项** | 中 | 使用第三方登录时必须同时提供 Sign in with Apple |
| **IPv6 不兼容** | 低 | 确保网络层支持 IPv6-only 环境 |

**拒审知识库**：每次拒审的原因、解决方案和修复时间应该记录。团队常见的拒审类型通常集中在 3-5 种——针对性预防可以显著降低拒审率。

### TestFlight 内测

TestFlight 是 iOS 的内测分发渠道：

| 类型 | 人数限制 | 审核 | 有效期 |
|------|---------|------|--------|
| 内部测试 | 100 人 | 不需要审核 | 90 天 |
| 外部测试 | 10000 人 | 需要 Beta 审核（通常 <24h） | 90 天 |

**工程建议**：每次 CI 构建的 Development 包自动上传到 TestFlight 内部测试组。QA 从 TestFlight 获取最新包测试，不从个人电脑拷贝。

### 加急审核

Apple 提供了加急审核（Expedited Review）申请入口。适用于：
- 修复线上关键 Bug（崩溃、数据丢失）
- 安全漏洞修复
- 限时活动上线

加急审核通常 24 小时内完成，但不保证。每个开发者账号有使用额度限制——不要滥用。

### 版本回撤

iOS 的版本回撤有限制：

**可以做的**：
- 停止新版本的分发（"Remove from Sale"）
- 回退到上一个已发布的版本

**不能做的**：
- 强制已更新的用户回到旧版本
- 已更新的用户只能等下一个修复版本或通过热更新修复

**工程意义**：iOS 的回撤只能阻止新用户下载问题版本，不能修复已更新用户的问题。所以灰度发布极其重要——先让少量用户更新，确认无问题后再全量。

## 小结

- [ ] IPA 上传是否通过 CI 自动化
- [ ] 拒审知识库是否维护
- [ ] TestFlight 是否作为 QA 的标准获取渠道
- [ ] 加急审核的申请条件是否团队知晓
- [ ] 版本回撤后的已更新用户是否有热更修复方案

---

**下一步应读**：[Android 发布专项]({{< relref "delivery-engineering/delivery-platform-publishing-04-android.md" >}})
