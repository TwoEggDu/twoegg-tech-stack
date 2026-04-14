---
title: "工程基建 02｜编译域设计——为什么要把代码分成多个编译单元"
slug: "delivery-engineering-foundation-02-compilation-domains"
date: "2026-04-14"
description: "编译域划分决定了编译速度、依赖方向和代码的可替换性。划分得当，改一行只编译一个域；划分不当，改一行触发全量编译。"
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Compilation"
  - "Architecture"
series: "工程基建"
primary_series: "delivery-engineering-foundation"
series_role: "article"
series_order: 20
weight: 220
delivery_layer: "principle"
delivery_volume: "V03"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

上一篇讲了项目结构的目录划分。这一篇更进一步——把目录划分的逻辑对齐到编译系统，讲清楚为什么需要编译域、怎么划分、划分后怎么管理域间依赖。

## 为什么这个问题重要

没有编译域划分的项目，所有代码在同一个编译单元里。改一行代码，整个项目重新编译。

在小项目里这不是问题——几秒就编完了。但当代码量到了 10 万行以上：

- 编译时间从 3 秒变成 30 秒，再变成 3 分钟
- 每次改代码后等编译变成日常最大的效率损耗
- 程序开始攒着改动一次性编译，而不是小步验证——这直接降低了代码质量
- CI 的构建时间变长，反馈周期变慢

编译域划分的直接收益是**增量编译**——只重新编译受影响的域，不动其他域。

## 本质是什么

编译域是编译器的最小独立编译单元。在不同的技术栈里它有不同的名字：

| 技术栈 | 编译域名称 | 产出 |
|--------|-----------|------|
| C# / .NET | Assembly | .dll 文件 |
| Unity | Assembly Definition (asmdef) | .dll 文件 |
| C++ | Translation Unit / Library | .o / .lib / .dll |
| Java / Kotlin | Module / Package | .class / .jar |
| TypeScript | Project Reference | .js 文件集 |

无论技术栈如何，编译域设计的原则是通用的：

### 原则一：域的划分对齐模块边界

编译域应该和项目结构的模块划分一一对应。每个功能模块一个编译域。

```
Core/          → Core.asmdef        → Core.dll
Modules/Combat → Combat.asmdef      → Combat.dll
Modules/UI     → UI.asmdef          → UI.dll
ThirdParty/X   → ThirdPartyX.asmdef → ThirdPartyX.dll
```

这样做的好处：
- 修改 Combat 模块只重新编译 Combat.dll，不触发 Core.dll 和 UI.dll 的重新编译
- 编译域的依赖关系和模块的逻辑依赖关系一致，不会出现"编译过了但运行时依赖缺失"

### 原则二：依赖方向和项目结构一致

编译域之间的引用关系必须和项目结构的分层模型一致：

```
应用域 → 模块域 → 框架域 → 基础域
```

上层域可以引用下层域，反过来不行。同层域之间原则上不互相引用。

违反方向的依赖会被编译器直接报错（循环引用），这正是编译域的工程价值——**把架构规则从"口头约定"变成"编译器强制"。**

### 原则三：域不能太大也不能太细

**太大**：所有代码一个域。失去增量编译的收益，回到全量编译。

**太细**：每个类一个域。域间引用关系变成蜘蛛网，管理成本远大于编译收益。编译器启动和链接的固定开销也会让总编译时间更长。

经验法则：
- 一个中型项目（10-30 万行代码）通常 10-30 个编译域
- 每个域 3000-30000 行代码
- 框架层和基础层的域数量少、体积稳定；模块层的域数量多、按功能拆分

## 编译域设计与热更新的关系

在支持热更新的项目中（如使用 HybridCLR 的 Unity 项目），编译域划分还有一个额外的工程意义：**哪些域可以热更新，哪些域必须跟随首包。**

```
不可热更（AOT 编译，跟首包）   ← 框架层、基础层、引擎原生接口
    ↑ 被引用
可热更（解释执行或混合执行）    ← 游戏逻辑、UI、配置加载
```

如果编译域没有按这个边界划分，可能出现：
- 想热更一个功能，发现它依赖了一个不可热更的域里的内部类型
- 热更后调用了 AOT 域没有泛型实例化的方法，运行时报错

编译域的划分必须提前考虑热更新的边界。本专栏 V08（脚本热更新）会详细展开。

## 常见错误做法

**只拆了编辑器代码和运行时代码两个域**。这确实避免了编辑器代码被打进运行时包，但没有获得增量编译的收益——运行时代码还是一个巨大的域。

**频繁调整域划分**。编译域一旦确定，修改的成本很高（大量文件的 Assembly Reference 需要重配）。域划分应该在项目早期完成，后续只增加新域，尽量不调整已有域。

**域之间通过 internal 和 InternalsVisibleTo 互相访问**。这等于把两个域在逻辑上又合并了。域间只通过 public 接口通信，private/internal 是域内的实现细节。

## 小结与检查清单

- [ ] 是否每个功能模块有独立的编译域
- [ ] 编译域的依赖方向是否单向（上层 → 下层）
- [ ] 是否存在循环依赖（编译器会报错但应该提前排查）
- [ ] 改一个模块的代码，是否只触发该域的重新编译
- [ ] 编译域划分是否考虑了热更新边界（可热更 vs 不可热更）
- [ ] 编译域数量是否合理（不是一个巨大域，也不是几十个微小域）

---

**下一步应读**：[Unity 实践：asmdef / Package / 编译优化]({{< relref "delivery-engineering/delivery-engineering-foundation-03-unity-asmdef.md" >}}) — 编译域原则在 Unity 中的具体落地方式

**扩展阅读**：[脚本编译管线系列]({{< relref "engine-toolchain/unity-script-compilation-pipeline-series-index.md" >}}) — Unity 脚本从 C# 到 IL 到运行时的完整编译链路
