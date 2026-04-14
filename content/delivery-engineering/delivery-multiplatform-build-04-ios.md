---
title: "多端构建 04｜iOS 构建专项——Xcode 工程、签名与 Framework"
slug: "delivery-multiplatform-build-04-ios"
date: "2026-04-14"
description: "Unity 导出的 iOS 构建不是一个可执行文件，而是一个 Xcode 项目。从 Xcode 项目生成到签名到 Framework 管理，iOS 构建的每一步都有特有的坑。"
tags:
  - "Delivery Engineering"
  - "Build System"
  - "iOS"
  - "Xcode"
  - "Signing"
series: "多端构建"
primary_series: "delivery-multiplatform-build"
series_role: "article"
series_order: 40
weight: 640
delivery_layer: "platform"
delivery_volume: "V07"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

Unity 构建 iOS 平台时，产出的不是最终的 .ipa 文件，而是一个 Xcode 项目。从 Xcode 项目到可安装的 .ipa，中间还有编译、签名、归档等步骤。每一步都有 iOS 平台特有的工程问题。

## Unity 导出 → Xcode 项目

Unity `BuildPipeline.BuildPlayer()` 对 iOS 平台的产出是一个 Xcode 项目目录：

```
iOSBuild/
├── Unity-iPhone.xcodeproj/
├── Classes/                 (Unity 生成的 C++ 代码)
├── Data/                    (资源数据)
├── Libraries/               (Unity 引擎库)
├── MainApp/                 (App 入口)
└── LaunchScreen.storyboard
```

这个 Xcode 项目需要进一步用 `xcodebuild` 编译和归档才能产出 .ipa。

### CI 中的 Xcode 构建流程

```bash
# 1. 编译和归档
xcodebuild -project Unity-iPhone.xcodeproj \
           -scheme Unity-iPhone \
           -configuration Release \
           -archivePath build/game.xcarchive \
           archive

# 2. 导出 IPA
xcodebuild -exportArchive \
           -archivePath build/game.xcarchive \
           -exportOptionsPlist ExportOptions.plist \
           -exportPath build/ipa/
```

`ExportOptions.plist` 控制导出方式（development / ad-hoc / app-store）和签名配置。

## 签名体系

iOS 签名是 iOS 构建中最复杂也最容易出事故的环节。

### 签名三件套

| 要素 | 作用 | 有效期 | 存储位置 |
|------|------|--------|---------|
| **开发者证书**（.p12） | 标识开发者身份 | 1 年 | CI Agent 的 Keychain |
| **Provisioning Profile**（.mobileprovision） | 关联证书 + App ID + 设备列表 | 1 年 | CI Agent 或 Xcode |
| **App ID** | 应用的唯一标识 | 永久 | Apple Developer Portal |

### 签名类型

| 类型 | 用途 | 设备限制 |
|------|------|---------|
| Development | 开发调试 | 最多 100 台注册设备 |
| Ad Hoc | 内部测试分发 | 最多 100 台注册设备 |
| App Store | 正式发布 | 无限制 |
| Enterprise | 企业内部分发 | 企业内无限制 |

### CI 中的签名管理

**自动签名**：Xcode 的 Automatic Signing 功能可以自动匹配证书和 Profile。但在 CI 环境中不稳定——CI Agent 没有登录 Apple ID 时自动签名可能失败。

**手动签名**（推荐用于 CI）：在构建脚本中明确指定证书和 Profile：

```bash
xcodebuild ... \
  CODE_SIGN_IDENTITY="iPhone Distribution: Company Name" \
  PROVISIONING_PROFILE_SPECIFIER="AppStore_Profile"
```

**证书安装**：CI 启动时通过脚本安装证书到 Keychain：

```bash
# 创建临时 Keychain
security create-keychain -p "" build.keychain
# 导入证书
security import certificate.p12 -k build.keychain -P "$CERT_PASSWORD" -T /usr/bin/codesign
# 设置 Keychain 搜索路径
security list-keychains -s build.keychain
```

### 签名过期预警

iOS 证书和 Profile 均为 1 年有效期。到期后构建直接失败。

CI 应该有自动化的过期预警：
- 每天检查证书和 Profile 的过期日期
- 提前 30 天开始发送告警
- 提前 7 天升级为紧急告警

## Framework 管理

第三方 SDK 通常以 Framework 形式提供。Framework 的管理需要注意：

**静态 vs 动态 Framework**：

| 类型 | 链接方式 | 包体影响 | 启动影响 |
|------|---------|---------|---------|
| 静态（.a / .framework） | 编译时链接进可执行文件 | 增大可执行文件 | 无额外影响 |
| 动态（.framework / .xcframework） | 运行时动态加载 | 单独存在于包内 | 增加启动时间 |

Apple 限制动态 Framework 的数量——过多的动态 Framework 会显著增加启动时间。建议：核心 SDK 用静态链接，可选模块用动态链接。

**XCFramework**：Apple 推荐的新格式，支持多架构（ARM64 + 模拟器）打包在一个文件中。替代了旧的 Fat Framework。

### Unity 后处理中添加 Framework

```csharp
// PostProcessBuild 中
public static void OnPostprocessBuild(BuildTarget target, string path)
{
    var projPath = PBXProject.GetPBXProjectPath(path);
    var proj = new PBXProject();
    proj.ReadFromFile(projPath);

    var mainTarget = proj.GetUnityMainTargetGuid();
    
    // 添加系统 Framework
    proj.AddFrameworkToProject(mainTarget, "AdSupport.framework", false);
    
    // 添加第三方 Framework
    proj.AddFileToBuild(mainTarget, proj.AddFile(
        "Frameworks/ThirdParty.framework",
        "Frameworks/ThirdParty.framework",
        PBXSourceTree.Source));

    proj.WriteToFile(projPath);
}
```

## 常见事故与排障

**事故：CI 构建成功但安装到设备失败**。Provisioning Profile 中没有包含该设备的 UDID。Ad Hoc 和 Development Profile 有设备限制，新设备需要先注册。

**事故：App Store 提审失败——包含了模拟器架构**。某个第三方 Framework 是 Fat Binary，包含了 x86_64（模拟器）架构。App Store 拒绝包含模拟器架构的提交。解法：构建时用 `lipo` 或 XCFramework 去除模拟器架构。

**事故：证书过期导致连续 3 天无法发版**。证书过期后需要重新申请，走企业审批流程耗时。应该提前 30 天续期。

## 小结与检查清单

- [ ] CI 是否使用手动签名（不依赖 Automatic Signing）
- [ ] 证书和 Profile 是否有过期预警（提前 30 天）
- [ ] 签名材料是否通过 CI 密钥管理注入（不入版本库）
- [ ] 后处理脚本是否正确添加了所有必需的 Framework 和 Capability
- [ ] 第三方 Framework 是否已去除模拟器架构
- [ ] ExportOptions.plist 是否在版本库中管理

---

**下一步应读**：[Android 构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-05-android.md" >}}) — Gradle、签名和 SDK 版本要求

**扩展阅读**：[平台认证流程]({{< relref "code-quality/platform-certification-trc-xr-overview.md" >}}) — iOS App Store 审核要求和常见不通过原因
