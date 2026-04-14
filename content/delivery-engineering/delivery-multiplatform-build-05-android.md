---
title: "多端构建 05｜Android 构建专项——Gradle、签名与 SDK 版本策略"
slug: "delivery-multiplatform-build-05-android"
date: "2026-04-14"
description: "Unity 导出的 Android 构建经过 Gradle 处理。minSdk / targetSdk 怎么选、Keystore 怎么管、64 位要求怎么满足、ProGuard 怎么配。"
tags:
  - "Delivery Engineering"
  - "Build System"
  - "Android"
  - "Gradle"
  - "Signing"
series: "多端构建"
primary_series: "delivery-multiplatform-build"
series_role: "article"
series_order: 50
weight: 650
delivery_layer: "platform"
delivery_volume: "V07"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

Android 构建的核心工具是 Gradle。Unity 导出 Android 项目后，Gradle 负责编译 Java/Kotlin 代码、合并资源、签名、产出 APK 或 AAB。相比 iOS 的 Xcode，Android 构建的配置项更多、依赖管理更复杂。

## Unity 导出 → Gradle 项目

Unity 构建 Android 时有两种模式：

**直接导出 APK/AAB**：Unity 内部调用 Gradle 完成全部流程，产出最终产物。简单但定制能力有限。

**导出 Gradle 项目**：Unity 产出 Gradle 项目目录，由外部 Gradle 编译。可以在 Gradle 项目上做任意定制，是大型项目的推荐方式。

```
AndroidProject/
├── launcher/               (App 入口模块)
│   ├── build.gradle
│   └── src/main/
│       ├── AndroidManifest.xml
│       └── java/
├── unityLibrary/           (Unity 引擎和游戏代码)
│   ├── build.gradle
│   └── src/main/
│       ├── assets/         (StreamingAssets)
│       ├── jniLibs/        (native libraries: libil2cpp.so, libunity.so)
│       └── java/
├── build.gradle            (根构建文件)
├── settings.gradle
└── gradle.properties
```

## SDK 版本策略

Android 的 SDK 版本配置影响兼容性和功能可用性：

| 配置 | 含义 | 建议值 | 理由 |
|------|------|--------|------|
| **minSdkVersion** | 支持的最低 Android 版本 | 23（Android 6.0） | 覆盖 99%+ 的活跃设备 |
| **targetSdkVersion** | App 声明适配的 Android 版本 | 最新稳定版（34/35） | Google Play 要求 |
| **compileSdkVersion** | 编译时使用的 SDK 版本 | >= targetSdkVersion | 编译器需要知道新 API |

**targetSdkVersion 的坑**：Google Play 要求 targetSdkVersion 不低于最近两年内的版本。提高 targetSdkVersion 可能触发新的行为变更（权限模型、后台限制、存储访问范围）。每次提高 targetSdk 前需要做兼容性测试。

## 签名管理

Android 签名使用 Keystore 文件：

### 签名要素

| 要素 | 作用 | 管理方式 |
|------|------|---------|
| Keystore 文件（.jks / .keystore） | 包含签名密钥 | CI 密钥管理服务 |
| Key Alias | Keystore 中的密钥别名 | CI 环境变量 |
| Keystore Password | Keystore 密码 | CI 密钥管理服务 |
| Key Password | 密钥密码 | CI 密钥管理服务 |

**Keystore 丢失 = 应用身份丢失**。如果 Keystore 丢失，已发布的应用无法更新——只能发新应用。Keystore 必须有备份。

### Google Play App Signing

Google Play 提供 App Signing 服务：开发者上传 AAB 时使用上传密钥签名，Google Play 用自己管理的 App Signing Key 重新签名。

优势：
- 即使开发者的上传密钥泄露，可以重新生成（App Signing Key 不受影响）
- Google Play 负责保管 App Signing Key（不会丢失）

限制：
- 只适用于 Google Play 分发
- 国内渠道仍然使用开发者自己的签名

### CI 中的签名配置

```groovy
// launcher/build.gradle
android {
    signingConfigs {
        release {
            storeFile file(System.getenv("KEYSTORE_PATH"))
            storePassword System.getenv("KEYSTORE_PASSWORD")
            keyAlias System.getenv("KEY_ALIAS")
            keyPassword System.getenv("KEY_PASSWORD")
        }
    }
    buildTypes {
        release {
            signingConfig signingConfigs.release
        }
    }
}
```

签名信息通过 CI 环境变量注入，不出现在代码中。

## 64 位要求与 ABI 管理

Google Play 要求所有应用必须包含 64 位（ARM64）版本。Unity 的 IL2CPP 后端默认产出 ARM64。

ABI 配置：

| ABI | 目标 | 是否必须 |
|-----|------|---------|
| arm64-v8a | 64 位 ARM 设备 | 必须（Google Play 要求） |
| armeabi-v7a | 32 位 ARM 设备 | 可选（覆盖旧设备） |
| x86_64 | x86 模拟器 | 不推荐（增大包体） |

**只包含 arm64-v8a**是当前推荐做法——几乎所有现代 Android 设备都是 64 位。去掉 armeabi-v7a 可以减少 30-40% 的 native library 体积。

如果项目需要覆盖极低端设备（2018 年以前的 32 位设备），可以保留 armeabi-v7a。但通过 AAB 格式分发时，每个设备只下载自己需要的 ABI——两个都保留不会增加用户下载量。

## Gradle 依赖管理

Android 构建的依赖通过 Gradle 管理。Unity 项目中常见的依赖冲突来源：

**Google 服务依赖**。Firebase、Google Play Services 等 SDK 之间有版本依赖关系。不兼容的版本组合会导致编译失败或运行时崩溃。

**AndroidX 迁移**。旧 SDK 使用 Android Support Library，新 SDK 使用 AndroidX。两者不能共存。如果项目中有旧 SDK 未迁移到 AndroidX，需要通过 Jetifier 自动转换。

**依赖锁定**。在 `gradle.properties` 或 `build.gradle` 中锁定关键依赖版本：

```groovy
configurations.all {
    resolutionStrategy {
        force 'com.google.firebase:firebase-analytics:21.5.0'
        force 'com.google.android.gms:play-services-base:18.3.0'
    }
}
```

## 常见事故与排障

**事故：Google Play 拒绝上传——targetSdkVersion 过低**。Google Play 每年更新 targetSdk 要求。如果不及时提升 targetSdkVersion，现有版本可以保留但无法上传新版本。

**事故：ProGuard / R8 裁剪了 SDK 需要的类**。Release 构建启用了代码缩减，某个 SDK 通过反射调用的类被裁剪。解法：在 ProGuard 规则中添加 keep 规则。

**事故：Gradle 依赖冲突导致构建失败**。两个 SDK 依赖了同一个库的不同版本。`./gradlew dependencies` 可以打印完整依赖树，定位冲突来源。

## 小结与检查清单

- [ ] minSdkVersion 是否合理（覆盖目标设备范围）
- [ ] targetSdkVersion 是否满足 Google Play 最新要求
- [ ] Keystore 是否有安全备份
- [ ] 签名信息是否通过环境变量注入
- [ ] 是否包含 arm64-v8a（64 位必须）
- [ ] Gradle 依赖是否有版本锁定
- [ ] ProGuard 规则是否覆盖了所有 SDK 的 keep 需求
- [ ] AndroidX 迁移是否完成

---

**下一步应读**：[微信小游戏构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-06-wechat.md" >}}) — WebGL 到微信小游戏的转换链路

**扩展阅读**：[Android AAB/PAD 系列]({{< relref "engine-toolchain/" >}}) — Unity 中 Android 构建的完整技术深挖
