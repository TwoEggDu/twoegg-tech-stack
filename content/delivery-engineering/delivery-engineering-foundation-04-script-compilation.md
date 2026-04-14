---
title: "工程基建 04｜脚本编译管线——从源码到运行时的完整链路"
slug: "delivery-engineering-foundation-04-script-compilation"
date: "2026-04-14"
description: "C# 源码到运行时可执行代码，经过编译、后处理、AOT/JIT 三个阶段。Mono 和 IL2CPP 选哪个、构建配置怎么影响产物——讲清楚编译管线对交付的影响。"
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Compilation"
  - "IL2CPP"
  - "Mono"
series: "工程基建"
primary_series: "delivery-engineering-foundation"
series_role: "article"
series_order: 40
weight: 240
delivery_layer: "principle"
delivery_volume: "V03"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

前三篇从项目结构讲到编译域再到 Unity asmdef。这一篇拉远一步，讲整条脚本编译管线：源码怎么变成运行时可执行的代码，中间经过哪些步骤，不同的编译后端对交付有什么影响。

## 为什么这个问题重要

编译管线的选型和配置直接影响交付的三个方面：

**构建时间**。IL2CPP 比 Mono 慢得多——一个中型项目的 IL2CPP 编译可能需要 15-40 分钟，而 Mono 只需要几十秒。这直接影响 CI 的反馈速度。

**运行时性能**。IL2CPP 产出的是原生代码，运行时性能通常优于 Mono 的 JIT 编译。这影响性能预算的分配。

**热更新可行性**。Mono 的 JIT 模式理论上可以加载新的 IL 代码，IL2CPP 的 AOT 模式不可以。这决定了热更新架构的选择（HybridCLR / Lua / 纯资源热更）。

## 本质是什么

脚本编译管线的本质是一个三阶段转换：

```
阶段 1        阶段 2           阶段 3
源码 ──→ 中间表示（IL）──→ 可执行代码
C#   Roslyn   .dll (CIL)  JIT/AOT   机器码 / 解释执行
```

### 阶段 1：源码到 IL

C# 源码通过 Roslyn 编译器编译成 CIL（Common Intermediate Language），打包为 .dll 文件。

这个阶段是所有 .NET 生态共享的——无论你用 Unity、.NET Core 还是 ASP.NET，C# 到 IL 的编译过程相同。

这个阶段的产物（.dll）就是编译域的输出。上一篇讲的 asmdef 划分，影响的就是这个阶段会产出多少个 .dll、每个 .dll 包含什么代码。

### 阶段 2：IL 后处理（ILPP）

在 .dll 产出后、进入最终编译前，Unity 有一个 IL Post Processing（ILPP）步骤。

ILPP 允许在 IL 层面修改编译产物。常见的用途：

- 自动为 Serializable 字段生成序列化代码
- 为网络同步框架生成 RPC 代理代码
- 为 ECS 框架生成 System 的调度代码
- 注入性能采集探针

ILPP 对交付的影响：
- 增加编译时间（每个 ILPP 步骤都需要处理目标 Assembly）
- 可能引入编译时错误（ILPP 逻辑有 Bug 时，错误信息难以理解）
- 热更新时需要确保 ILPP 的产物和首包一致（否则运行时行为不匹配）

### 阶段 3：IL 到可执行代码

这是 Mono 和 IL2CPP 两条路径分叉的地方。

**Mono（JIT 模式）**：
- .dll 原样打进包里
- 运行时由 Mono VM 即时编译（JIT）为机器码
- 首次调用方法时编译，后续调用直接执行已编译的机器码
- 优点：构建快、可以加载新 IL（理论上支持热更新）
- 缺点：运行时性能较低、JIT 编译导致首次调用延迟、iOS 禁止 JIT

**IL2CPP（AOT 模式）**：
- .dll 被转换为 C++ 源码
- C++ 源码由平台原生编译器编译为机器码
- 运行时直接执行原生代码，没有 VM 开销
- 优点：运行时性能高、安全性好（代码不可逆向为 IL）、iOS 必须使用
- 缺点：构建时间长（C++ 编译耗时）、不支持动态加载新 IL

**选型原则**：

| 场景 | 推荐 | 理由 |
|------|------|------|
| iOS 发布 | IL2CPP | Apple 要求，没有选择 |
| Android 发布 | IL2CPP | 性能和安全性 |
| 微信小游戏 | IL2CPP → WebAssembly | 微信环境要求 |
| 开发期（编辑器） | Mono | 编译快、迭代快 |
| 需要脚本热更新 | IL2CPP + HybridCLR | HybridCLR 在 IL2CPP 基础上补充了解释器 |

## 构建配置对交付的影响

编译管线不只受编译后端影响，还受构建配置影响：

### Debug vs Development vs Release

| 配置 | 用途 | 特征 |
|------|------|------|
| Debug | 编辑器内调试 | 完整调试信息、无优化 |
| Development Build | 真机调试 | 保留 Profiler 连接、Development 标记、部分优化 |
| Release | 发布 | 完整优化、Strip、无调试信息 |

**常见事故**：Development Build 和 Release Build 的行为不同，导致"开发机上没问题、正式包有问题"。差异来源：

- Development Build 保留了完整的类型信息，Release Build 经过 Stripping 裁剪掉了"未使用"的类型
- Development Build 的宏定义（`DEVELOPMENT_BUILD`）开启了调试代码路径
- Development Build 的优化级别较低，某些时序敏感的 Bug 不会暴露

**工程原则**：CI 的质量门检查必须在 Release 配置下运行，不能只测 Development Build。

### Managed Code Stripping

IL2CPP 的 Managed Code Stripping 会裁剪掉未被直接引用的代码。这可以显著减少包体大小，但也可能裁剪掉通过反射调用的代码。

Stripping 级别：

| 级别 | 裁剪范围 | 风险 |
|------|---------|------|
| Disabled | 不裁剪 | 包体最大，无风险 |
| Minimal | 只裁剪 BCL 中明确未使用的 | 低风险 |
| Low | 裁剪未引用的类型 | 中风险（反射调用可能被裁） |
| Medium | 裁剪未引用的成员 | 高风险 |
| High | 激进裁剪 | 最高风险 |

**工程原则**：Stripping 级别的选择是包体大小和稳定性的权衡。如果用了反射、序列化或热更新，必须通过 link.xml 或 `[Preserve]` 标注保护相关类型。

## 编译管线与交付链路的关系

```
编译管线
├── 阶段 1（Roslyn）→ 影响：增量编译时间、CI 反馈速度
├── 阶段 2（ILPP）  → 影响：框架兼容性、热更新一致性
└── 阶段 3（Mono/IL2CPP）→ 影响：构建时间、运行时性能、热更新可行性

构建配置
├── Debug/Dev/Release → 影响：测试与发布的一致性
└── Stripping        → 影响：包体大小、反射/热更新的稳定性
```

这些选型和配置不是"一次设定就不管了"。每次 Unity 版本升级、每次引入新的第三方框架、每次调整热更新架构，都需要重新评估编译管线的配置。

## 小结与检查清单

- [ ] 是否清楚项目使用的编译后端（Mono / IL2CPP）及选型理由
- [ ] Development Build 和 Release Build 的行为差异是否已知并管控
- [ ] Managed Code Stripping 的级别是否明确，反射和热更新相关类型是否有 Preserve 保护
- [ ] CI 的验证是否在 Release 配置下执行（不只是 Development）
- [ ] ILPP 步骤是否有文档记录（哪些框架注入了什么、对编译时间的影响多大）
- [ ] 编译管线的配置是否在版本库中管理（不是某台机器上的本地设置）

---

**下一步应读**：[依赖管理与第三方集成]({{< relref "delivery-engineering/delivery-engineering-foundation-05-dependency-management.md" >}}) — SDK、插件和平台专属库怎么管

**扩展阅读**：脚本编译管线系列（engine-toolchain 栏）— Roslyn、ILPP、IL2CPP 的完整技术深挖，以及 Unity 裁剪系列的 link.xml 和 Preserve 策略
