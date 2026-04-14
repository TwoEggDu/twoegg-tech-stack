---
title: "版本与分支管理 05｜变更追踪与影响分析——一次提交影响哪些端、哪些包"
slug: "delivery-version-management-05-change-tracking"
date: "2026-04-14"
description: "在合入前就知道一次变更的影响范围：影响哪些平台、哪些 AssetBundle、哪些功能模块——而不是上线后才发现漏测了某一端。"
tags:
  - "Delivery Engineering"
  - "Version Management"
  - "Change Tracking"
  - "Impact Analysis"
series: "版本与分支管理"
primary_series: "delivery-version-management"
series_role: "article"
series_order: 50
weight: 350
delivery_layer: "principle"
delivery_volume: "V04"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 这篇解决什么问题

代码和资源的每一次变更，都可能影响三端中某一端或全部端的构建产物。如果在合入前不知道影响范围，验证就变成了猜测——可能漏测了某一端、某一个 Bundle、某一个平台专属功能。

## 为什么这个问题重要

- 修改了一个 Shader 的 fallback 逻辑，只在 Android 低端设备上测了，没发现 WebGL 不支持该 fallback——微信小游戏端上线后画面全黑
- 调整了某个 Prefab 引用的贴图，不知道这个 Prefab 被三个不同的 AssetBundle 引用——热更新时只更新了一个 Bundle，另外两个 Bundle 加载到旧贴图
- 改了网络层的序列化逻辑，影响了客户端和服务端的协议兼容，但没有通知服务端团队

变更影响分析的目标是：**在变更合入之前，自动化地给出"这次变更影响了什么"的报告。**

## 本质是什么

变更影响分析本质上是一个依赖图查询问题：

```
变更的文件集合 → 查询依赖图 → 受影响的模块/平台/Bundle 集合
```

需要建立三层依赖图：

### 第一层：代码依赖图

代码的依赖关系由编译域（asmdef）的引用关系和命名空间的 using 关系决定。

当一个编译域的公共接口发生变化时，所有引用它的编译域都受影响。

```
变更了 Core.dll 的公共接口
  → Combat.dll 引用了 Core → 受影响
  → UI.dll 引用了 Core → 受影响
  → Network.dll 未引用 Core → 不受影响
```

代码依赖图可以从 asmdef 的引用关系自动提取，不需要手动维护。

### 第二层：资源依赖图

资源的依赖关系由 AssetBundle 的打包分组和资源间的引用关系决定。

当一个贴图被修改时，引用该贴图的所有 Material、Prefab 和它们所在的 AssetBundle 都受影响。

```
修改了 tex_hero_warrior_d.tga
  → mat_hero_warrior.mat 引用了该贴图 → 受影响
  → prefab_hero_warrior.prefab 使用了该材质 → 受影响
  → bundle_characters.bundle 包含该 Prefab → 需要重新构建
```

资源依赖图可以通过引擎的依赖分析 API 提取。Unity 的 `AssetDatabase.GetDependencies()` 和 Addressables 的依赖分析工具都能做到。

### 第三层：平台影响图

某些变更只影响特定平台：

| 变更类型 | 影响的平台 |
|---------|-----------|
| 修改了 `#if UNITY_IOS` 块内的代码 | 只影响 iOS |
| 修改了 Android Gradle 配置 | 只影响 Android |
| 修改了 WebGL 专用的 Shader 变体 | 只影响微信小游戏 |
| 修改了 Core 模块的公共接口 | 影响所有平台 |
| 修改了通用的贴图资源 | 影响所有平台（但压缩格式不同） |

平台影响图的构建比较简单——根据变更文件的路径和条件编译宏判断。

## 变更影响报告

将三层依赖图的查询结果合成一份变更影响报告，在 Code Review 时自动展示：

```
变更影响报告
──────────
提交：abc1234 "修复角色 Shader 的 fallback 逻辑"
变更文件：3 个

代码影响：
  ✦ Rendering 模块（直接变更）
  ○ Combat 模块（间接依赖，需要回归）

资源影响：
  ✦ mat_hero_warrior.mat → bundle_characters
  ✦ mat_npc_guard.mat → bundle_npcs
  共影响 2 个 AssetBundle

平台影响：
  ✦ iOS — Shader 变体受影响
  ✦ Android — Shader 变体受影响
  ✦ 微信小游戏 — WebGL fallback 路径受影响，建议重点测试
  
建议验证范围：
  - 三端 Shader 渲染正确性
  - bundle_characters 和 bundle_npcs 的加载测试
  - 低端设备 fallback 路径验证
```

这份报告的价值在于：**Review 的人不需要靠经验判断"这次改动要测哪些东西"，报告自动给出。**

## 怎么实施

### 最小可行方案

如果还没有完整的依赖图系统，可以从最简单的规则开始：

**基于路径的规则**：
```
如果变更了 Assets/Modules/Combat/ 下的文件 → 标记为"影响战斗模块"
如果变更了 Assets/Shaders/ 下的文件 → 标记为"影响所有平台渲染"
如果变更了 Plugins/iOS/ 下的文件 → 标记为"仅影响 iOS"
如果变更了 ProjectSettings/ → 标记为"影响所有平台构建配置"
```

这些规则用 CI 脚本实现，不需要复杂的依赖分析——已经能拦住大部分"漏测某一端"的问题。

### 进阶方案

在 CI 中集成依赖分析工具：

1. **代码依赖**：解析 asmdef 引用关系，变更一个 Assembly 时自动标记所有下游 Assembly
2. **资源依赖**：用 Unity 的依赖分析 API 查询变更资源的上游引用链
3. **Bundle 影响**：从资源依赖推导出受影响的 AssetBundle 列表
4. **平台影响**：根据文件路径和条件编译宏判断影响的平台

产出结果作为 CI 的一个检查步骤，附加到 Pull Request 的评论中。

## 变更追踪与版本复盘的关系

变更影响报告不只在 Review 时有用。当版本上线后出了问题，它也是追溯根因的关键工具：

```
线上问题：Android 低端设备上某个角色模型渲染异常
  → 查看版本的变更列表
  → 过滤"影响 Android + 影响渲染"的变更
  → 定位到 abc1234 "修复角色 Shader 的 fallback 逻辑"
  → 确认该变更的影响报告中标注了"低端设备 fallback 路径"
  → 该路径的验证在发版前被跳过了
  → 根因定位完成
```

如果没有变更影响记录，上面这个追溯过程可能需要几小时甚至几天。

## 常见错误做法

**依赖影响分析靠人脑判断**。Review 时靠 Reviewer 的经验判断"这次改动需要测什么"。经验丰富的人能判断对，新人或不熟悉该模块的人就会漏掉。自动化分析不替代人的判断，但能保证基线覆盖。

**只追踪代码变更，不追踪配置和资源变更**。配置表改了一个数值不会出现在代码 diff 里，但它可能影响游戏行为。资源变更了一个贴图不会出现在代码 Review 里，但它可能影响多个 Bundle。变更追踪必须覆盖代码、配置和资源三条线。

**变更记录不关联到版本号**。知道"这个版本有哪些变更"，但不知道"这个变更从哪个版本开始生效"。变更记录必须关联到版本号（或 commit hash），支持双向查询：从版本查变更、从变更查版本。

## 小结与检查清单

- [ ] 是否有变更影响分析的机制（至少基于路径规则）
- [ ] 代码变更是否能自动推导出受影响的编译域
- [ ] 资源变更是否能自动推导出受影响的 AssetBundle
- [ ] 平台影响是否能自动判断（基于文件路径或条件编译宏）
- [ ] 变更影响报告是否在 Code Review 时展示
- [ ] 变更记录是否关联到版本号
- [ ] 配置和资源变更是否纳入变更追踪

---

V04 版本与分支管理到这里结束。

五篇文章覆盖了：版本号设计（01）、分支策略（02）、内容冻结与发布列车（03）、环境配置与 Feature Flag（04）和变更追踪与影响分析（05）。

**推荐下一步**：V05 资源管线 — 从版本管理进入资源管线：资源怎样从引擎内部格式到用户设备的运行时文件

**扩展阅读**：[案例：一次热更新上线事故的复盘]({{< relref "projects/case-hotupdate-production-incident.md" >}}) — 变更追踪缺失导致的线上事故实例
