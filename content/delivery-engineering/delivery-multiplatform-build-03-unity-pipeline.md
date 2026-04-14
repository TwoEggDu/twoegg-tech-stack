---
title: "多端构建 03｜Unity 构建管线——BuildPipeline、SBP 与构建后处理"
slug: "delivery-multiplatform-build-03-unity-pipeline"
date: "2026-04-14"
description: "Unity 的构建管线从 BuildPipeline API 到 Scriptable Build Pipeline，再到平台专属的构建后处理。怎么用、怎么扩展、CI 里怎么集成。"
tags:
  - "Delivery Engineering"
  - "Build System"
  - "Unity"
  - "BuildPipeline"
  - "SBP"
series: "多端构建"
primary_series: "delivery-multiplatform-build"
series_role: "article"
series_order: 30
weight: 630
delivery_layer: "practice"
delivery_volume: "V07"
delivery_reading_lines:
  - "L2"
---

## 这篇解决什么问题

前两篇讲了构建系统的通用原理和配置管理。这一篇落地到 Unity：构建管线怎么用、怎么自动化、怎么在 CI 中集成。

## Unity 构建管线的两层结构

Unity 的构建分两层：

**Player 构建**：把整个项目编译打包成平台可执行产物（.ipa / .apk / WebGL 目录）。

**Asset 构建**：把资源打包成 AssetBundle。这一层有两个管线选项：
- **Legacy Build Pipeline**：旧版，通过 `BuildPipeline.BuildAssetBundles()` 调用
- **Scriptable Build Pipeline（SBP）**：新版，Addressables 底层使用的管线

### Player 构建 API

Player 构建通过 `BuildPipeline.BuildPlayer()` 触发：

```csharp
var options = new BuildPlayerOptions
{
    scenes = new[] { "Assets/Scenes/Main.unity" },
    locationPathName = "Build/iOS",
    target = BuildTarget.iOS,
    options = BuildOptions.None  // 或 BuildOptions.Development
};

BuildPipeline.BuildPlayer(options);
```

在 CI 中，通过命令行参数调用构建脚本：

```bash
Unity -batchmode -quit -projectPath /path/to/project \
      -executeMethod BuildScript.Build \
      -platform ios -config release -buildNumber 456
```

### 构建后处理

Unity 构建完成后，通过 `IPostprocessBuildWithReport` 接口执行后处理：

**iOS 后处理**（在 Xcode 项目生成后修改）：
- 添加 Capability（Push Notifications、In-App Purchase）
- 修改 Info.plist（隐私权限声明、URL Scheme）
- 添加 Framework 依赖
- 修改 Xcode Build Settings

**Android 后处理**（在 Gradle 项目生成后修改）：
- 修改 AndroidManifest.xml
- 添加 Gradle 依赖
- 修改 ProGuard 规则

后处理脚本应该在版本库中管理，且有明确的执行顺序（通过 `callbackOrder` 控制）。

### Scriptable Build Pipeline（SBP）

SBP 是 AssetBundle 构建的新选择，Addressables 在底层使用它。相比 Legacy Build Pipeline：

| 维度 | Legacy | SBP |
|------|--------|-----|
| 增量构建 | 有限支持 | 完整支持 |
| 构建缓存 | 基于文件时间戳 | 基于内容哈希 |
| 自定义步骤 | 困难 | 通过 IBuildTask 扩展 |
| TypeTree | 默认包含 | 可选 |
| 确定性构建 | 不保证 | 更好的确定性 |

SBP 的增量构建基于内容哈希——只有资源内容真正变化时才重新打包，而不是靠文件时间戳判断。这对 CI 环境很重要（CI 每次 checkout 的文件时间戳都是新的）。

## 构建管线在 CI 中的集成

### CI 构建的标准流程

```
1. Checkout 代码（指定 commit）
2. 恢复 Library 缓存（加速资源导入）
3. 导入项目（Unity -batchmode -importAssets）
4. 执行构建脚本（按 CI 参数构建指定平台和配置）
5. 构建后处理（平台专属修改）
6. 归档产物（标记 Build Number，上传到存储）
7. 触发验证（包体检查、签名验证、冒烟测试）
```

### Library 缓存

Unity 的 Library/ 目录包含所有资源的导入缓存。在 CI 上首次构建需要从头导入所有资源（可能耗时 30-60 分钟），后续构建可以复用 Library 缓存。

**缓存策略**：
- 把 Library/ 目录按 Unity 版本和平台缓存
- 每次构建前检查缓存的版本是否匹配
- 定期清理过期缓存（Unity 版本升级后旧缓存不可用）

**注意**：Library 缓存不入版本库。它是机器本地的，跨机器不可复用（路径依赖）。CI 的缓存应该在同一台 Agent 或共享存储上管理。

### 多平台并行构建

三端构建可以在 CI 中并行执行：

```
                ┌→ iOS Agent → 构建 iOS → 归档
同一个 commit ──┼→ Android Agent → 构建 Android → 归档
                └→ WebGL Agent → 构建 WebGL → 归档
```

并行构建要求：
- 三台 Agent 的 Unity 版本和环境必须一致
- 构建脚本从 CI 参数读取平台，不硬编码
- 三端产物归档到同一个构建号下

## 常见事故与排障

**事故：CI 构建成功但产物和本地不同**。原因通常是 Library 缓存中有旧的导入缓存——某个资源在本地重新导入了但 CI 上还是旧的导入结果。解法：CI 上定期全量清理 Library 缓存。

**事故：构建后处理脚本顺序不对**。两个后处理脚本都修改 Info.plist，但执行顺序不确定，导致后一个覆盖了前一个的修改。解法：通过 `callbackOrder` 明确指定顺序。

**事故：Addressables 构建和 Player 构建的资源不一致**。先构建了 Addressables 的 Bundle，然后修改了资源，再构建 Player。Player 包含了新资源但 Bundle 还是旧的。解法：CI 流程中先构建 Addressables Bundle，再构建 Player，两步之间不允许资源变更。

## 小结与检查清单

- [ ] 构建是否通过脚本 + CI 参数触发（不是手动点 Build）
- [ ] 构建后处理脚本是否有明确的执行顺序
- [ ] AssetBundle 构建和 Player 构建的顺序是否正确
- [ ] Library 缓存是否有管理策略（恢复、校验、定期清理）
- [ ] 多平台构建是否可以并行
- [ ] 构建产物是否归档并标记 Build Number

---

**下一步应读**：[iOS 构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-04-ios.md" >}}) — Xcode 工程、签名和 Framework 的处理

**扩展阅读**：[构建流水线]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}}) — Jenkins 和 GitHub Actions 的构建流水线实践
