---
title: "多端构建 02｜构建配置管理——Debug / Development / Release 的差异与管控"
slug: "delivery-multiplatform-build-02-build-config"
date: "2026-04-14"
description: "三种构建配置不只是'优化开不开'。它们在代码路径、资源处理、调试能力和产物行为上有系统性差异，管控不当就是事故源。"
tags:
  - "Delivery Engineering"
  - "Build System"
  - "Build Configuration"
series: "多端构建"
primary_series: "delivery-multiplatform-build"
series_role: "article"
series_order: 20
weight: 620
delivery_layer: "principle"
delivery_volume: "V07"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

"开发包没问题，正式包崩溃了"——这是构建配置管理失控的典型症状。Debug、Development Build 和 Release Build 之间的差异远不只是优化级别，它们在代码路径、资源处理和运行时行为上有系统性差异。

## 本质是什么

构建配置本质上是一组**编译开关 + 优化选项 + 调试能力**的组合：

| 维度 | Debug（编辑器） | Development Build | Release |
|------|----------------|-------------------|---------|
| **代码优化** | 无 | 部分 | 完整 |
| **调试信息** | 完整 | 保留符号表 | 裁剪 |
| **Profiler 连接** | 是 | 是 | 否 |
| **编译宏** | UNITY_EDITOR | DEVELOPMENT_BUILD | 无特殊宏 |
| **Stripping** | 不执行 | 可选 | 执行 |
| **调试代码** | 编译进去 | 编译进去 | 不编译（如正确使用条件编译） |
| **资源处理** | 不打包 | 打包 | 打包 + 压缩 |
| **IL2CPP** | 不使用（编辑器用 Mono） | 可选 | 通常使用 |

### 差异导致的典型事故

**Stripping 差异**。Development Build 不执行 Managed Stripping（或级别低），Release Build 执行高级 Stripping。某个通过反射调用的类在 Release Build 中被裁剪，运行时 `TypeLoadException`。

**宏差异**。`#if DEVELOPMENT_BUILD` 块内的调试代码在 Development Build 中执行，Release 中不执行。如果调试代码中有初始化逻辑（如注册了某个服务），Release 中该服务不存在，依赖它的功能崩溃。

**优化差异**。IL2CPP 在 Release 模式下的优化可能改变代码执行顺序或内联行为。依赖特定执行顺序的代码（如多线程竞争、浮点精度敏感的计算）可能在 Release 中表现不同。

**Profiler 差异**。Development Build 保留了 Profiler 连接能力，这会引入额外的内存开销和性能开销。Development Build 的性能数据不能作为 Release 的基线。

## 构建配置的版本化管理

构建配置不应该依赖手动设置。所有配置应该在版本库中管理，通过 CI 参数选择。

### Unity 的构建配置管理

Unity 的构建配置分散在多个位置：

| 配置位置 | 内容 | 版本化方式 |
|---------|------|-----------|
| PlayerSettings | 平台设置、签名、图标 | ProjectSettings/ 目录入版本库 |
| Build Settings | 目标平台、场景列表 | EditorBuildSettings.asset |
| Quality Settings | 渲染质量级别 | QualitySettings.asset |
| 自定义 BuildScript | 构建参数、后处理 | 代码文件入版本库 |
| CI 参数 | 平台、配置、版本号 | CI 配置文件入版本库 |

**关键原则**：构建脚本应该接受 CI 传入的参数来决定构建配置，不应该读取 ProjectSettings 中手动修改的值。

```
CI 触发构建时传参：
  --platform ios
  --config release
  --version 1.2.3
  --build-number 456

构建脚本根据参数：
  设置 BuildTarget = iOS
  设置 Development = false
  设置 IL2CPP = true
  设置 Stripping = High
  设置版本号
```

这样同一个代码库可以通过不同参数产出 Development 和 Release 两种产物，不需要修改任何文件。

## 三种配置的使用场景

| 配置 | 使用场景 | CI 频率 |
|------|---------|---------|
| Debug（编辑器） | 日常开发和调试 | 不需要 CI |
| Development Build | 真机调试、性能分析、QA 内测 | 每次提交触发 |
| Release | 提审、灰度、正式发版 | Release 分支触发 |

### 验证矩阵

**核心原则：CI 的质量门必须在 Release 配置下执行。**

| 验证项 | Development | Release |
|--------|------------|---------|
| 编译通过 | 必须 | 必须 |
| 冒烟测试 | 必须 | 必须 |
| 性能基线 | 参考值 | **基准值** |
| Stripping 兼容 | 不检测 | **必须** |
| 包体大小 | 参考值 | **基准值** |

如果只在 Development Build 上跑验证，Stripping 导致的崩溃和 Release 特有的性能问题都不会被发现。

## 构建参数的工程化

除了 Debug/Dev/Release 三级之外，还有一些构建参数需要工程化管理：

### 平台宏定义

自定义的编译宏应该通过构建脚本注入，不应该手动在 Player Settings 中修改：

```csharp
// 构建脚本中
PlayerSettings.SetScriptingDefineSymbolsForGroup(
    BuildTargetGroup.iOS,
    "RELEASE;ANALYTICS_ENABLED;NO_CHEATS"
);
```

每种构建配置的宏集合应该在构建脚本中明确列出，可审查、可追溯。

### 签名配置

签名是构建配置中最容易出事故的部分：

| 平台 | 签名要素 | 管理方式 |
|------|---------|---------|
| iOS | 证书 + Provisioning Profile | CI Agent 的 Keychain |
| Android | Keystore + Key Alias + Password | CI 密钥管理服务 |
| 微信小游戏 | AppID + AppSecret | CI 环境变量 |

**签名材料绝不能入版本库**。通过 CI 的密钥管理服务或环境变量注入。

**签名过期是高频事故**。iOS 证书 1 年过期，Provisioning Profile 1 年过期。CI 应该有证书过期预警——提前 30 天开始提醒。

## 常见错误做法

**只在 Development Build 上测试**。"Development 包没问题就发 Release"——但 Stripping 差异、优化差异、宏差异都可能导致 Release 包行为不同。

**构建配置靠手动切换**。发 Release 包前有人手动到 Player Settings 里改配置。手动操作容易遗漏，且不可追溯。必须通过构建脚本 + CI 参数自动化。

**签名配置写死在项目里**。Keystore 路径和密码硬编码在构建脚本中。换台 CI Agent 就构建失败。签名通过环境变量注入。

**不检查构建产物的签名有效性**。构建成功了但签名无效（证书过期、Profile 不匹配），提交平台审核时才发现。构建后应该自动验证签名。

## 小结与检查清单

- [ ] Development 和 Release 的差异是否已知并有文档
- [ ] CI 质量门是否在 Release 配置下执行
- [ ] 构建配置是否通过 CI 参数控制（不靠手动修改 Player Settings）
- [ ] 自定义编译宏是否在构建脚本中明确列出
- [ ] 签名材料是否通过密钥管理服务注入（不入版本库）
- [ ] 签名证书是否有过期预警
- [ ] 构建后是否自动验证签名有效性

---

**下一步应读**：[Unity 构建管线]({{< relref "delivery-engineering/delivery-multiplatform-build-03-unity-pipeline.md" >}}) — Unity 的 BuildPipeline / SBP / 构建后处理

**扩展阅读**：Build Debug 系列（engine-toolchain 栏）— Debug / Release / Development Build 的完整技术差异
