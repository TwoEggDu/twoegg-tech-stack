---
title: "工程基建 03｜Unity 实践：Assembly Definition、Package 化与编译时间优化"
slug: "delivery-engineering-foundation-03-unity-asmdef"
date: "2026-04-14"
description: "上一篇的编译域原则在 Unity 中怎么落地：asmdef 怎么配、Package 化怎么做、Domain Reload 和 Enter Play Mode 怎么优化编译体验。"
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Unity"
  - "asmdef"
  - "Compilation"
series: "工程基建"
primary_series: "delivery-engineering-foundation"
series_role: "article"
series_order: 30
weight: 230
delivery_layer: "practice"
delivery_volume: "V03"
delivery_reading_lines:
  - "L2"
---

## 这篇解决什么问题

上一篇讲了编译域设计的通用原则。这一篇落地到 Unity：Assembly Definition（asmdef）怎么用、UPM Package 怎么组织、编译时间怎么压缩。

这是一篇 Unity 实践层的文章。如果你使用其他引擎，可以跳过本篇直接读 V03-04。

## Assembly Definition：Unity 的编译域机制

Unity 默认把 Assets/ 下所有 C# 代码编译进一个 Assembly（Assembly-CSharp.dll）。改一行代码，整个项目重新编译。

Assembly Definition（.asmdef）文件把代码分成多个独立的 Assembly，实现增量编译。

### 基本用法

在模块根目录创建 .asmdef 文件：

```
Assets/
├── Core/
│   └── Core.asmdef              → 编译为 Core.dll
├── Modules/
│   ├── Combat/
│   │   └── Combat.asmdef        → 编译为 Combat.dll
│   ├── UI/
│   │   └── UI.asmdef            → 编译为 UI.dll
│   └── Character/
│       └── Character.asmdef     → 编译为 Character.dll
└── ThirdParty/
    └── SomePlugin/
        └── SomePlugin.asmdef    → 编译为 SomePlugin.dll
```

### 引用关系配置

asmdef 的 `Assembly Definition References` 字段决定了域间依赖：

```json
// Combat.asmdef
{
    "name": "Combat",
    "references": [
        "Core",         // Combat 依赖 Core
        "Character"     // Combat 依赖 Character
    ]
}
```

**编译器会强制依赖方向**。如果 Combat 引用了 UI 但 asmdef 里没有声明，编译直接报错。这比靠人工 review 检查依赖方向可靠得多。

### 常见的 asmdef 划分方案

一个中型 Unity 项目的典型 asmdef 结构：

| asmdef | 内容 | 引用 | 变更频率 |
|--------|------|------|---------|
| Core | 框架、事件、工具类 | 无 | 低 |
| Character | 角色系统 | Core | 中 |
| Combat | 战斗系统 | Core, Character | 中 |
| UI | UI 系统 | Core | 高 |
| Network | 网络层 | Core | 低 |
| HotUpdate | 热更新入口 | Core, 各模块接口 | 高 |
| Editor | 编辑器扩展 | Core（仅 Editor 平台） | 低 |
| Tests | 测试代码 | Core, 各模块（仅 Editor 平台） | 中 |

### asmdef 配置要点

**Auto Referenced**：设为 false 的 asmdef 不会被默认 Assembly（Assembly-CSharp）引用。第三方插件的 asmdef 通常应该设为 false，需要时显式引用。

**Define Constraints**：可以在 asmdef 级别定义编译宏条件。例如 `ENABLE_COMBAT_DEBUG` 只在 Combat.asmdef 里定义，不污染其他域。

**Platform 过滤**：Editor-only 的 asmdef 应该限制为 Editor 平台，不编入运行时构建。测试代码同理。

## UPM Package 化

当模块足够独立和稳定时，可以从 asmdef 进一步升级为 UPM Package（Unity Package Manager 包）。

### 什么时候该 Package 化

| 场景 | 方式 |
|------|------|
| 只在本项目使用的模块 | asmdef 就够了 |
| 两个项目共用的工具库 | 考虑 Package 化 |
| 三个以上项目共用 | 必须 Package 化 |
| 第三方集成的封装层 | 建议 Package 化（版本锁定） |

### Package 的目录结构

```
com.studio.core/
├── package.json
├── Runtime/
│   ├── Core.asmdef
│   └── *.cs
├── Editor/
│   ├── Core.Editor.asmdef
│   └── *.cs
├── Tests/
│   ├── Runtime/
│   └── Editor/
├── Documentation~/
└── CHANGELOG.md
```

### Package 的分发方式

| 方式 | 适用场景 | 版本管理 |
|------|---------|---------|
| Embedded（嵌入项目 Packages/ 目录） | 开发阶段 | 跟随项目版本 |
| Git URL | 小团队跨项目共享 | Git tag 版本锁定 |
| 私有 Registry（Verdaccio / GitLab） | 大团队多项目 | 语义版本号 |

Package 化的核心收益不是"代码共享"——复制代码也能共享。核心收益是**版本锁定**：项目 A 使用 v1.2.0，项目 B 使用 v1.3.0，互不影响。升级是主动的决策，不是被动的联动。

## 编译时间优化

即使 asmdef 划分合理，编译时间仍然可能偏长。以下是 Unity 特有的优化手段：

### Enter Play Mode Settings

Unity 每次进入 Play Mode 默认会做 Domain Reload（重新加载所有 Assembly）和 Scene Reload（重新加载场景）。

关闭这两个选项可以把进入 Play Mode 的时间从 5-15 秒压缩到 1 秒以内：

- **Disable Domain Reload**：跳过 Assembly 重新加载。代价是静态变量不会重置——代码必须手动处理静态状态的初始化。
- **Disable Scene Reload**：跳过场景重新加载。代价是场景状态不会重置。

这两个优化对开发体验的改善是巨大的，但需要代码配合——所有依赖"进入 Play Mode 时自动重置"的逻辑都需要改造。

### 增量编译与缓存

Unity 的编译器（Roslyn）支持增量编译。确保增量编译生效的条件：

- asmdef 划分合理（改一个域不触发其他域的重新编译）
- 不频繁修改框架层和基础层的代码（这些域的改动会触发所有上层域重新编译）
- CI 中使用 Library 缓存（避免每次都从头编译）

### 构建时编译优化

CI 构建时的编译优化：

- **Code Stripping**：IL2CPP 的 Managed Code Stripping 可以裁掉未使用的代码，减少编译和链接时间
- **增量 IL2CPP 编译**：Unity 2021+ 支持增量 IL2CPP 编译，只重新编译变更的 Assembly
- **分布式构建**：大型项目可以把不同平台的构建分配到不同的 CI Agent 并行执行

## 常见错误做法

**所有代码放在默认 Assembly 里，只拆了第三方插件**。这是最常见的情况——项目组知道 asmdef 的存在，但只给第三方加了 asmdef，自己的代码全在 Assembly-CSharp 里。改一行业务代码仍然触发全量编译。

**asmdef 之间用 InternalsVisibleTo 互相暴露**。InternalsVisibleTo 让两个 Assembly 可以访问彼此的 internal 成员。这在逻辑上等于把两个域合并了，失去了编译隔离的意义。域间通信必须通过 public 接口。

**关闭 Domain Reload 后不处理静态状态**。关闭 Domain Reload 是好的优化，但如果代码里有大量静态缓存、静态事件注册、静态单例，关闭后行为会异常。必须先排查并处理所有静态状态。

## 小结与检查清单

- [ ] 项目是否有 asmdef 划分（不是只有默认 Assembly）
- [ ] asmdef 的引用关系是否和模块依赖方向一致
- [ ] 编辑器代码和测试代码是否在独立的 asmdef 中且限制了平台
- [ ] 跨项目共用的模块是否已经 Package 化
- [ ] Enter Play Mode Settings 是否启用了 Disable Domain Reload
- [ ] CI 构建是否使用了 Library 缓存
- [ ] 改一行业务代码的编译时间是否在 5 秒以内

---

**下一步应读**：[脚本编译管线]({{< relref "delivery-engineering/delivery-engineering-foundation-04-script-compilation.md" >}}) — 从 C# 到运行时：Mono / IL2CPP 的编译链路和选型

**扩展阅读**：脚本编译管线系列（engine-toolchain 栏）— Unity 的 Roslyn、ILPP、Domain Reload 的完整技术深挖
