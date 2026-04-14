---
title: "包体管理与分发 04｜iOS 分发：App Thinning、On-Demand Resources 与蜂窝限制"
slug: "delivery-package-distribution-04-ios"
date: "2026-04-14"
description: "iOS 的包体优化机制和 Android 不同：App Slicing 自动裁剪设备不需要的资源，On-Demand Resources 支持按需下载，蜂窝网络下载有硬性大小限制。"
tags:
  - "Delivery Engineering"
  - "Package Management"
  - "iOS"
  - "App Thinning"
  - "On-Demand Resources"
series: "包体管理与分发"
primary_series: "delivery-package-distribution"
series_role: "article"
series_order: 40
weight: 540
delivery_layer: "platform"
delivery_volume: "V06"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

iOS 平台有自己的包体优化和分发机制，与 Android 的 AAB/PAD 在思路上相似但实现完全不同。本篇覆盖 App Thinning 三件套和蜂窝网络下载限制的工程处理。

## App Thinning 三件套

App Thinning 是 Apple 的包体优化伞概念，包含三个机制：

### App Slicing

App Store 根据用户设备自动生成该设备需要的 IPA 子集：
- 只包含设备的 CPU 架构（ARM64）
- 只包含设备屏幕分辨率对应的图片资源（@2x 或 @3x）
- 只包含设备支持的 GPU 能力对应的资源

**工程影响**：
- 开发者不需要额外操作，Slicing 由 App Store 自动完成
- 但需要确保 Asset Catalog 中的资源正确标记了设备特征（否则 Slicing 无法生效）
- Xcode 的 App Thinning Size Report 可以预估各设备变体的大小

### Bitcode（已废弃）

Bitcode 允许 Apple 在服务端针对特定设备重新编译优化代码。Xcode 14 开始 Apple 已经废弃了 Bitcode——不需要再关注这个机制。

### On-Demand Resources（ODR）

ODR 是 iOS 版的"按需下载"机制。资源被标记为不同的 Tag，App Store 负责托管和分发：

```
Tag: "level_01"  → 关卡 1 的资源
Tag: "level_02"  → 关卡 2 的资源
Tag: "character_pack_a" → 角色包 A 的资源
```

ODR 的三种获取优先级：

| 优先级 | 行为 | 适用场景 |
|--------|------|---------|
| **Initial Install** | 随安装包一起下载 | 首次体验必需的资源 |
| **Prefetch** | 安装后立即后台下载 | 很快就需要的资源 |
| **On-Demand** | 代码请求时才下载 | 按需内容 |

**ODR 与自建 CDN 的区别**：

| 维度 | ODR | 自建 CDN |
|------|-----|---------|
| 托管 | Apple CDN，免费 | 自建，需要费用 |
| 更新 | 必须通过 App Store 审核 | 随时更新 |
| 缓存管理 | 系统自动管理，空间不足时可能被清除 | 应用自己管理 |
| 下载 API | NSBundleResourceRequest | 自建下载逻辑 |

**ODR 的限制**：
- 资源更新和 App 版本绑定——改了一个 ODR 资源就需要提交新版本审核
- 系统可能在设备空间不足时自动清除已下载的 ODR 资源——代码必须处理"资源曾经下载过但现在不在了"的情况
- 没有增量下载——Tag 中任何一个文件变化，整个 Tag 重新下载

**工程建议**：ODR 适合静态内容（关卡、角色包）的分发。对于频繁更新的内容（配置、活动资源），自建 CDN + 热更新更灵活。

## 蜂窝网络下载限制

Apple 对蜂窝网络（移动数据）下载有大小限制。超出限制的应用只能在 Wi-Fi 下下载：

| 时间 | 蜂窝下载限制 |
|------|------------|
| iOS 13 之前 | 150MB |
| iOS 13+ | 200MB |
| iOS 17+ | 用户可在设置中自行调整或取消限制 |

**工程影响**：
- 首包（安装包 + Initial Install 的 ODR）大小应控制在 200MB 以内
- 超出 200MB 的用户在蜂窝网络下载时会看到"需要 Wi-Fi 才能下载"的提示
- 这不是审核拒绝——应用仍然可以上架，但蜂窝用户的下载转化率会显著下降

**应对策略**：
- 首包严格控制在 200MB 以内
- 核心体验之外的资源通过 ODR 的 Prefetch 或 On-Demand 下载
- 在应用内提供 Wi-Fi 下载提醒（"建议在 Wi-Fi 环境下下载额外内容"）

## Unity 中的 iOS 包体优化

### iOS 构建产物结构

Unity 导出的 iOS 构建产物是一个 Xcode 项目，最终产出 .ipa 文件。IPA 内部结构：

```
Payload/
└── GameName.app/
    ├── GameName          (可执行文件，IL2CPP 产出)
    ├── Data/
    │   ├── Managed/      (元数据)
    │   ├── Resources/    (StreamingAssets 内容)
    │   └── Raw/          (资源数据)
    ├── Frameworks/       (动态库)
    └── Assets.car        (Asset Catalog 产物)
```

### 大小优化要点

**可执行文件（IL2CPP 产出）**：
- Managed Stripping Level 设为 High（配合 link.xml）
- 开启 LTO（Link Time Optimization）减少代码体积
- Strip Engine Code 裁剪未使用的引擎模块

**资源数据**：
- 纹理使用 ASTC 压缩
- 音频使用 AAC 编码
- 关闭不必要的 Mipmap

**动态库**：
- 检查 Frameworks/ 下是否有不必要的第三方 Framework
- 每个 Framework 的体积贡献应该在构建报告中可查

## 常见错误做法

**不查看 App Thinning Size Report**。Xcode 的 App Thinning Size Report 能告诉你每种设备变体的实际下载大小。很多团队只看 IPA 的总大小，不知道特定设备的实际下载量。

**ODR Tag 划分太粗**。一个 Tag 包含了太多资源，更新其中一个文件就要重新下载整个 Tag。Tag 应该按独立使用场景划分。

**不处理 ODR 资源被系统清除的情况**。代码假设"ODR 资源一旦下载就永远在"，但系统在设备空间不足时会清除。访问 ODR 资源前必须检查是否存在，不存在时重新请求下载。

## 小结与检查清单

- [ ] Asset Catalog 中的资源是否正确标记了设备特征
- [ ] 首包是否控制在 200MB 以内（蜂窝网络下载阈值）
- [ ] 非首次体验资源是否使用 ODR 或自建 CDN 分发
- [ ] ODR 资源被系统清除后是否有重新下载逻辑
- [ ] 是否查看了 App Thinning Size Report
- [ ] IL2CPP 的 Stripping Level 和 LTO 是否已优化

---

**下一步应读**：[微信小游戏分发]({{< relref "delivery-engineering/delivery-package-distribution-05-wechat.md" >}}) — 约束最严格的平台：4MB 主包 + 内存限制 + WebGL 限制

**扩展阅读**：[包体大小优化]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}}) — IL2CPP Stripping 和 iOS On-Demand Resources 的具体操作
