---
title: "CI/CD 管线 03｜构建自动化——脚本设计、环境管理、缓存与产物归档"
slug: "delivery-cicd-pipeline-03-build-automation"
date: "2026-04-14"
description: "构建自动化不是写个打包脚本那么简单。要解决幂等性、环境一致性、缓存加速和产物可追溯四个问题。"
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Build Automation"
series: "CI/CD 管线"
primary_series: "delivery-cicd-pipeline"
series_role: "article"
series_order: 30
weight: 1530
delivery_layer: "principle"
delivery_volume: "V16"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

V16-02 讲了管线架构，构建是管线的核心阶段。很多团队的构建脚本是"能跑就行"——在某台机器上、某个人操作时能成功，换一台机器或换一个人就可能失败。这一篇讲构建脚本怎么设计才能在任何环境下、任何人触发时都产出一致的结果。

## 构建脚本设计原则

### 幂等性

**同样的输入，无论跑多少次，都产出同样的输出。**

| 违反幂等性的做法 | 正确做法 |
|----------------|---------|
| 脚本依赖上一次构建残留的中间文件 | 每次构建前清理工作目录 |
| 版本号从本地文件读取并自增 | 版本号从 CI 环境变量或 Git Tag 获取 |
| 脚本内硬编码绝对路径 | 使用相对路径或环境变量 |
| 构建结果依赖构建机的时区设置 | 时间戳统一使用 UTC |

### 参数化

**所有可变项都通过参数传入，不在脚本内硬编码。**

核心参数：

| 参数 | 示例 | 说明 |
|------|------|------|
| 目标平台 | iOS / Android / WeChat | 决定构建管线分支 |
| 构建类型 | Debug / Release / Profile | 决定编译优化和调试信息 |
| 版本号 | 1.2.3 | 写入构建产物 |
| Build Number | 456 | 唯一标识本次构建 |
| 资源服务器地址 | cdn.example.com | 热更新资源的 CDN 地址 |
| 签名配置 | dev / dist | 决定签名证书 |

### 无副作用

**构建脚本不应修改构建机的全局状态。**

- 不修改系统环境变量（只在脚本作用域内设置）
- 不安装全局工具（依赖预配置的构建环境）
- 不修改 Git 仓库内容（不在构建过程中 commit）

## 环境管理

### Unity 版本锁定

| 做法 | 说明 |
|------|------|
| ProjectSettings/ProjectVersion.txt | 记录项目使用的 Unity 版本 |
| CI Agent 预装多版本 Unity | 通过 Unity Hub CLI 管理 |
| 构建脚本自动选择对应版本 | 读取 ProjectVersion.txt，调用对应 Unity 可执行文件 |

**绝对不要使用"最新版 Unity"构建。** 版本不一致是构建失败的首要原因。

### SDK 版本锁定

| 工具/SDK | 版本锁定方式 |
|----------|------------|
| Xcode | 指定 Xcode 版本路径（xcode-select） |
| Android SDK | 在 CI 配置中指定 compileSdkVersion 和 buildToolsVersion |
| NDK | 在项目配置中指定 NDK 版本 |
| CocoaPods | Podfile.lock 锁定依赖版本 |
| Gradle | gradle-wrapper.properties 锁定 Gradle 版本 |

### CI Agent 标准化

CI Agent（构建机）的环境应该是标准化的、可重建的：

```
Agent 环境清单：
├── OS 版本（macOS 14.x / Ubuntu 22.04 / Windows Server 2022）
├── Unity 版本列表（2022.3.x LTS, 2023.2.x）
├── Xcode 版本（仅 macOS）
├── Android SDK + NDK 版本
├── Node.js 版本（微信小游戏构建需要）
├── Python 版本（构建脚本可能需要）
└── 签名证书和 Provisioning Profile（仅 macOS）
```

环境清单应该文档化或脚本化，新增构建机时可以一键配置。

## 缓存策略

游戏项目构建慢的主要原因是资源处理。合理的缓存策略可以把增量构建时间从 60 分钟缩短到 10-15 分钟。

### 各层缓存

| 缓存项 | 缓存位置 | 节省时间 | 失效条件 |
|--------|---------|---------|---------|
| Unity Library 缓存 | CI Agent 本地 / 共享存储 | 10-20 分钟（资源导入） | Unity 版本升级、平台切换 |
| IL2CPP 编译缓存 | CI Agent 本地 | 5-15 分钟 | C# 代码变更 |
| Gradle 缓存 | CI Agent 本地 | 2-5 分钟 | 依赖版本变更 |
| CocoaPods 缓存 | CI Agent 本地 | 1-3 分钟 | Podfile.lock 变更 |
| npm 缓存 | CI Agent 本地 | 1-2 分钟 | package-lock.json 变更 |
| AssetBundle 增量构建缓存 | CI Agent 本地 / 共享存储 | 10-30 分钟 | 资源变更范围决定 |

### Library 缓存的特殊处理

Unity 的 Library 文件夹是最大的缓存项（通常 5-20 GB），也是失效最频繁的：

| 场景 | 缓存可用？ | 说明 |
|------|-----------|------|
| 同平台增量构建 | 可用 | 只重新导入变更的资源 |
| 切换目标平台 | 不可用 | 不同平台的导入结果不同 |
| Unity 版本升级 | 不可用 | 需要全量重新导入 |
| 新增 CI Agent | 需要预热 | 第一次构建是全量导入 |

**实践建议**：每个 CI Agent 固定负责一个平台，避免平台切换导致缓存失效。

### 缓存一致性验证

缓存加速构建的同时可能引入一致性问题：

- 定期做一次无缓存的全量构建，对比产物是否一致
- 缓存命中率低于 70% 时检查缓存策略是否合理
- 主版本发布时强制使用全量构建，不依赖缓存

## 产物归档

### 归档内容

每次发布构建（不是每次开发构建）都应该归档：

| 归档项 | 内容 | 用途 |
|--------|------|------|
| 构建产物 | IPA / APK / AAB / 小游戏包 | 版本回溯、问题复现 |
| 符号表 | dSYM / mapping.txt / IL2CPP symbols | Crash 符号化 |
| Build Report | Unity Build Report | 包体分析 |
| 构建日志 | 完整构建输出 | 问题排查 |
| 环境快照 | Unity 版本、SDK 版本、CI Agent 信息 | 环境复现 |
| Git 信息 | Commit Hash、Branch、Tag | 代码定位 |

### 归档命名规范

```
{项目名}_{平台}_{版本号}_{BuildNumber}_{CommitShort}/
├── build/          # 构建产物
├── symbols/        # 符号表
├── reports/        # 构建报告
├── logs/           # 构建日志
└── metadata.json   # 元数据（版本、环境、时间）
```

### 保留策略

| 构建类型 | 保留策略 | 说明 |
|---------|---------|------|
| 发布构建 | 永久保留 | 已上线版本的回溯需要 |
| 预发布构建 | 保留 3 个月 | QA 验证期间可能需要对比 |
| 日常构建 | 保留最近 N 次（如 30 次） | 磁盘空间有限 |
| 特性分支构建 | 分支删除时自动清理 | 不占用长期存储 |

自动清理脚本应该是管线的一部分——不要依赖人工清理构建存储。

## 构建可复现性验证

**最终检验标准：给定同一个 Commit Hash，在不同机器上构建出的产物是否功能一致？**

完全的二进制一致在游戏项目中很难做到（时间戳、签名等会导致差异），但功能一致是必须保证的：

- 定期在两台不同的 CI Agent 上构建同一个版本，对比包体大小、文件列表、资源 Hash
- 差异超过阈值时告警排查
- 版本发布前至少在两个环境上验证构建结果一致

## 小结与检查清单

- [ ] 构建脚本是否满足幂等性（同输入同输出）
- [ ] 所有可变项是否参数化（不硬编码）
- [ ] Unity 版本和 SDK 版本是否锁定
- [ ] CI Agent 环境是否标准化、可重建
- [ ] Library 缓存策略是否合理（固定 Agent 固定平台）
- [ ] 发布构建是否有完整归档（产物+符号表+日志+元数据）
- [ ] 归档保留策略是否有自动清理机制
- [ ] 是否定期验证构建可复现性

---

**下一步应读**：[质量门自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-04-quality-gates.md" >}}) — 编译、资源、Shader 变体、性能基线怎么接入 CI

**扩展阅读**：[多端构建系列]({{< relref "delivery-engineering/delivery-multiplatform-build-series-index.md" >}}) — V07 已覆盖各平台构建的具体配置
