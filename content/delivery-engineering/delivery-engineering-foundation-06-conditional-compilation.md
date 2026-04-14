---
title: "工程基建 06｜条件编译与多端共用——一套代码怎么产出三端"
slug: "delivery-engineering-foundation-06-conditional-compilation"
date: "2026-04-14"
description: "平台宏、Feature Flag、编译开关——三种机制各自解决什么问题，怎样组织才能让一套代码稳定产出 iOS、Android 和微信小游戏三端产物。"
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Conditional Compilation"
  - "Multi-platform"
series: "工程基建"
primary_series: "delivery-engineering-foundation"
series_role: "article"
series_order: 60
weight: 260
delivery_layer: "principle"
delivery_volume: "V03"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L5"
---

## 这篇解决什么问题

一个项目同时出 iOS、Android 和微信小游戏三端。三端的 API 不同、能力不同、限制不同。怎样让一套代码库产出三端构建产物，而不是维护三套代码？

## 为什么这个问题重要

多端共用处理不好的典型后果：

- 到处是 `#if UNITY_IOS ... #elif UNITY_ANDROID ... #endif`，代码可读性极差
- 某个平台的分支里有 Bug，但只在构建该平台时才能发现——平时开发在另一个平台，Bug 长期潜伏
- Feature Flag 和平台宏混在一起，关不掉某个功能时不确定是平台限制还是功能开关
- 微信小游戏端的代码路径和原生端差异太大，每次改功能都要改两遍

## 本质是什么

多端共用需要三种机制配合，各自解决不同层次的问题：

| 机制 | 解决什么问题 | 生效时机 | 谁控制 |
|------|------------|---------|--------|
| **平台宏** | 平台 API 不同 | 编译时 | 引擎 / 构建系统 |
| **Feature Flag** | 功能是否启用 | 运行时 | 配置 / 远程下发 |
| **编译开关** | 模块是否编入 | 编译时 | 构建参数 |

### 平台宏：处理 API 差异

平台宏由引擎或构建系统自动定义，用于处理不同平台的 API 差异：

```csharp
public void ShowNativeDialog(string message)
{
#if UNITY_IOS
    IOSBridge.ShowAlert(message);
#elif UNITY_ANDROID
    AndroidBridge.ShowToast(message);
#elif UNITY_WEBGL
    JSBridge.Alert(message);
#endif
}
```

**使用原则**：

**平台宏只用在平台抽象层，不用在业务代码里。** 如果业务代码里出现了 `#if UNITY_IOS`，说明平台差异没有被正确封装。

正确的做法：

```
业务代码 → 调用 → INativeDialog.Show(message)   ← 接口（无平台宏）
                       ↓
           IosPlatform : INativeDialog            ← iOS 实现（有平台宏）
           AndroidPlatform : INativeDialog        ← Android 实现
           WebGLPlatform : INativeDialog           ← WebGL 实现
```

业务代码里一个 `#if` 都没有。平台差异被封装在平台实现层里。

**平台宏的管理**：

| 宏 | 定义者 | 含义 |
|---|--------|------|
| `UNITY_IOS` | Unity | 目标平台是 iOS |
| `UNITY_ANDROID` | Unity | 目标平台是 Android |
| `UNITY_WEBGL` | Unity | 目标平台是 WebGL |
| `UNITY_EDITOR` | Unity | 在编辑器中运行 |
| `DEVELOPMENT_BUILD` | Unity | Development Build 模式 |
| `ENABLE_IL2CPP` | Unity | 使用 IL2CPP 编译后端 |

这些宏由 Unity 自动定义，不需要手动管理。自定义的平台相关宏应该尽量少——越多的宏意味着越多的代码路径分支，越多的潜在 Bug。

### Feature Flag：控制功能开关

Feature Flag 和平台宏看起来类似（都是条件分支），但本质不同：

- 平台宏是**编译时**确定的，决定代码是否存在于构建产物中
- Feature Flag 是**运行时**读取的，决定功能是否对用户可见

```csharp
// Feature Flag（运行时）
if (FeatureFlags.IsEnabled("new_shop_ui"))
{
    ShowNewShopUI();
}
else
{
    ShowOldShopUI();
}
```

Feature Flag 的典型用途：

**灰度发布**。新功能先对 5% 的用户开放，观察数据后逐步扩量。

**A/B 测试**。同一个功能的两种实现，随机分配给不同用户，比较效果。

**紧急关闭**。上线后发现某个功能有问题，通过远程配置关闭该功能，不需要发新版本。

**开发期隔离**。一个未完成的功能先合入主干，但通过 Flag 关闭，不影响当前版本发布。

**Feature Flag 的管理**：

- Flag 存储在配置文件或远程配置服务中，不硬编码在代码里
- 每个 Flag 有明确的生命周期：创建 → 灰度 → 全量 → 清理
- 全量后的 Flag 必须清理——否则代码里会积累大量永远为 true 的死分支
- Flag 的当前状态应该在版本健康看板中可查

### 编译开关：控制模块是否编入

编译开关是介于平台宏和 Feature Flag 之间的机制：它在编译时生效，但由构建参数控制（而非平台自动定义）。

典型用途：

```csharp
// 自定义编译开关
#if ENABLE_CHEAT_CONSOLE
    CheatConsole.Initialize();
#endif
```

**通过构建参数控制**——开发版开启 `ENABLE_CHEAT_CONSOLE`，发布版不开启。这样作弊控制台的代码不会出现在发布包里。

编译开关通过 Unity 的 `Scripting Define Symbols` 或 asmdef 的 `Define Constraints` 配置。

**管理原则**：

- 编译开关的数量应该控制在 5-10 个以内——每多一个开关，代码路径的组合就翻倍
- 每个开关必须有文档说明：名称、用途、谁控制、在哪些构建配置下开启
- CI 应该至少测试两种开关组合：开发配置和发布配置

## 三种机制的协作

```
            编译时                          运行时
    ┌──────────────────┐           ┌──────────────────┐
    │ 平台宏            │           │ Feature Flag      │
    │ → 处理平台 API 差异│           │ → 控制功能可见性   │
    │                  │           │                   │
    │ 编译开关          │           │                   │
    │ → 控制模块是否编入 │           │                   │
    └──────────────────┘           └──────────────────┘
```

一个典型的多端功能实现流程：

1. **平台抽象层**用平台宏处理 API 差异（编译时，隐藏在封装层）
2. **构建配置**用编译开关决定是否编入调试模块（编译时，CI 参数控制）
3. **功能模块**用 Feature Flag 决定是否对用户可见（运行时，远程配置控制）

三种机制各就各位，不混用。

## 常见错误做法

**业务代码里到处写 `#if UNITY_IOS`**。平台差异应该被封装在平台抽象层，业务代码不应该感知平台。如果业务代码里需要写平台宏，说明抽象层没有覆盖到。

**用平台宏代替 Feature Flag**。`#if NEW_SHOP` 写在代码里，想关闭时需要重新编译。应该用运行时 Feature Flag，不需要重新编译就能控制。

**Feature Flag 只增不删**。代码里积累了 50 个 Flag，其中 40 个永远为 true。每个已全量的 Flag 都应该在下一个版本中清理掉。

**不同平台的构建从不同分支出**。iOS 从 `ios-release` 分支构建，Android 从 `android-release` 分支构建。两个分支逐渐分叉，三端一致性无法保证。必须从同一个分支、同一个 commit 构建三端。

## 小结与检查清单

- [ ] 平台差异是否被封装在抽象层（业务代码无 `#if UNITY_IOS`）
- [ ] Feature Flag 是否有生命周期管理（创建 → 灰度 → 全量 → 清理）
- [ ] 编译开关是否通过构建参数控制（不是手动修改 Player Settings）
- [ ] 三端是否从同一个分支、同一个 commit 构建
- [ ] CI 是否测试了至少两种编译开关组合（开发 + 发布）
- [ ] 已全量的 Feature Flag 是否定期清理

---

V03 工程基建到这里结束。

六篇文章覆盖了：项目结构（01）、编译域（02）、Unity asmdef（03）、脚本编译管线（04）、依赖管理（05）和多端条件编译（06）。

**推荐下一步**：V04 版本与分支管理 — 从工程基建进入版本管理：分支策略、版本号、内容冻结和 Feature Flag 的流程设计

**扩展阅读**：[案例：多团队工程治理]({{< relref "projects/case-multi-team-governance.md" >}}) — 多团队场景下分支策略和 CI 门禁的实战经验
