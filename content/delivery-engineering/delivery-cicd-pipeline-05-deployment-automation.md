---
title: "CI/CD 管线 05｜部署自动化——fastlane、Gradle、微信 CLI 与 CDN 发布"
slug: "delivery-cicd-pipeline-05-deployment-automation"
date: "2026-04-14"
description: "部署不是构建的最后一步，而是独立的自动化流程。三端部署各有工具链，CDN 发布有自己的验证逻辑。核心原则：staging 和 production 用同一套管线，只改配置。"
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Deployment"
  - "fastlane"
series: "CI/CD 管线"
primary_series: "delivery-cicd-pipeline"
series_role: "article"
series_order: 50
weight: 1550
delivery_layer: "platform"
delivery_volume: "V16"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

V16-04 讲了质量门——构建产物通过检查后，下一步是部署到目标环境。手动部署最大的问题不是慢，而是不一致——staging 环境手动部署成功，production 环境换个人操作就漏了一步。这一篇讲怎么让部署流程自动化，并保证 staging 和 production 使用同一套流程。

## 部署管线的核心原则

**staging 和 production 用同一套部署脚本，区别只在配置参数。**

| 参数 | staging | production |
|------|---------|------------|
| 签名证书 | 开发证书 | 发布证书 |
| 服务器地址 | staging.api.example.com | api.example.com |
| CDN 地址 | cdn-staging.example.com | cdn.example.com |
| 推送目标 | TestFlight / 内部测试 | App Store / Google Play |
| 灰度比例 | 100%（全量推送给测试人员） | 1%→10%→50%→100% |

如果 staging 和 production 的部署脚本是两套独立代码，那在 staging 验证通过的部署流程并不能保证 production 也能成功。

## iOS 部署自动化

### 工具链：fastlane

fastlane 是 iOS/Android 自动化部署的事实标准，核心组件：

| 组件 | 功能 |
|------|------|
| fastlane match | 证书和 Provisioning Profile 管理 |
| fastlane gym | 构建 IPA |
| fastlane pilot | 上传到 TestFlight |
| fastlane deliver | 上传到 App Store Connect |

### 部署流程

```
构建产物（IPA）
  → fastlane pilot upload     # 上传到 TestFlight（内部测试）
  → 内测通过
  → fastlane deliver          # 提交到 App Store 审核
  → 审核通过
  → App Store Connect 手动发布 / 定时发布
```

### CI 集成要点

| 要点 | 说明 |
|------|------|
| 证书管理 | 用 fastlane match 从 Git 仓库同步证书，不手动在 Keychain 安装 |
| Apple ID 认证 | 使用 App Store Connect API Key（JSON），不用账号密码 |
| Provisioning Profile | match 自动管理，不手动在 Apple Developer Portal 操作 |
| 构建号自增 | 从 CI Build Number 获取，不从本地自增 |
| 超时处理 | 上传大 IPA 可能超时，设置合理的超时时间和重试机制 |

### 常见问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| Code Signing 失败 | 证书过期或 Profile 不匹配 | fastlane match 自动续期 |
| 上传超时 | IPA 过大 + 网络不稳定 | 重试机制 + 日志记录 |
| TestFlight 处理中 | Apple 后台处理需要时间 | 上传后等待处理完成再通知 QA |

## Android 部署自动化

### 工具链：Gradle + Google Play API

| 工具 | 功能 |
|------|------|
| Gradle | 构建 APK/AAB |
| bundletool | AAB 本地测试（生成 APKS） |
| Google Play Developer API | 上传到 Google Play Console |
| fastlane supply | Google Play 上传（fastlane 封装） |

### 部署流程

```
构建产物（AAB）
  → 签名（Release Keystore）
  → Google Play API / fastlane supply 上传
  → 内部测试轨道（Internal Testing）
  → 内测通过 → 封闭测试轨道（Closed Testing）
  → 灰度发布 → 正式轨道（Production，按比例推送）
```

### CI 集成要点

| 要点 | 说明 |
|------|------|
| Keystore 管理 | Keystore 文件加密存储在 CI 密钥管理中，不提交到 Git |
| 服务账号 | 用 Google Cloud Service Account JSON 认证，不用个人账号 |
| 多渠道包 | 如果有国内渠道包，需要单独的签名和上传流程 |
| AAB vs APK | Google Play 要求 AAB，国内渠道可能需要 APK |

## 微信小游戏部署自动化

### 工具链：miniprogram-ci

微信官方提供的 CI 工具，支持命令行上传和预览：

| 命令 | 功能 |
|------|------|
| miniprogram-ci upload | 上传代码到微信后台 |
| miniprogram-ci preview | 生成预览二维码 |
| miniprogram-ci buildNpm | 构建 npm 依赖 |

### 部署流程

```
构建产物（小游戏包）
  → miniprogram-ci upload     # 上传到微信后台
  → 微信管理后台提交审核
  → 审核通过
  → 微信管理后台手动发布 / 灰度发布
```

### CI 集成要点

| 要点 | 说明 |
|------|------|
| 密钥管理 | 上传密钥从微信后台下载，加密存储在 CI 中 |
| 包体限制 | 主包 ≤ 限定值，超限需要拆分子包 |
| 版本描述 | upload 时附带版本描述，方便审核 |
| 机器人通知 | 上传成功后通知到企业微信/钉钉群 |

## CDN 部署自动化

热更新资源（AssetBundle、Lua 脚本等）不走应用商店，直接部署到 CDN：

### 部署流程

```
构建资源包
  → 计算资源 Hash / 生成 Manifest
  → 上传到 CDN（staging 目录）
  → 验证 CDN 同步完成（检查多个边缘节点）
  → 更新版本 Manifest（staging 指向新资源）
  → staging 验证通过
  → 发布 Manifest（production 指向新资源）
```

### CDN 部署的关键步骤

| 步骤 | 说明 | 失败处理 |
|------|------|---------|
| 上传资源 | 上传新增和变更的资源文件 | 重试，记录失败文件 |
| 同步验证 | 检查 CDN 边缘节点是否已同步 | 等待 + 重试 |
| Manifest 发布 | 更新版本 Manifest，客户端才能发现新资源 | Manifest 是最后一步，失败则回滚 |
| 回滚准备 | 保留上一版本的 Manifest，随时可切回 | 回滚 = 切换 Manifest 指向 |

**V06-06 已详细覆盖 CDN 资源发布的完整流程，这里强调 CI 集成层面的自动化。**

## 部署审计追踪

每次部署都应记录完整的审计信息：

| 审计项 | 内容 |
|--------|------|
| 触发者 | 谁触发了这次部署（人 / CI 自动） |
| 触发时间 | 部署开始和完成的时间 |
| 部署版本 | 版本号 + Build Number + Commit Hash |
| 目标环境 | staging / production / 具体渠道 |
| 部署结果 | 成功 / 失败 / 部分成功 |
| 回滚信息 | 如果回滚了，记录回滚原因和回滚到的版本 |

审计记录不只是为了"出事后追责"——更重要的是为部署频率、部署成功率、部署耗时等度量提供数据，推动部署流程的持续改进。

## 小结与检查清单

- [ ] staging 和 production 是否使用同一套部署脚本（只改配置）
- [ ] iOS 部署是否使用 fastlane + API Key 认证（不依赖个人账号）
- [ ] Android 部署是否使用 Service Account 认证
- [ ] 微信小游戏是否集成 miniprogram-ci 自动上传
- [ ] CDN 资源部署是否有同步验证和 Manifest 发布机制
- [ ] 签名证书和密钥是否加密存储在 CI 密钥管理中
- [ ] 每次部署是否有审计记录（触发者、版本、环境、结果）
- [ ] 部署失败是否有回滚机制和通知

---

**下一步应读**：[CI 工具选型]({{< relref "delivery-engineering/delivery-cicd-pipeline-06-tool-selection.md" >}}) — Jenkins、GitHub Actions、GitLab CI 对比

**扩展阅读**：[包体管理与分发系列]({{< relref "delivery-engineering/delivery-package-distribution-series-index.md" >}}) — V06 已覆盖 CDN 发布的完整流程
