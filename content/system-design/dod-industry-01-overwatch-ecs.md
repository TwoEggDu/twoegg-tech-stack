---
title: "DOD 行业案例 01｜Overwatch ECS（GDC 2017）：ECS 的架构价值和性能价值可以分开"
slug: "dod-industry-01-overwatch-ecs"
date: "2026-03-28"
description: "Blizzard 在 GDC 2017 分享的 Overwatch ECS 是 Managed 的——没有 cache-friendly 布局，没有 SIMD 优化——但它依然成功地用 ECS 解决了逻辑隔离和可维护性问题。这个案例证明 ECS 的架构价值和性能价值是可以分开的。"
tags:
  - "ECS"
  - "Overwatch"
  - "GDC"
  - "Blizzard"
  - "架构"
  - "数据导向"
series: "数据导向行业横向对比"
primary_series: "dod-industry"
series_role: "article"
series_order: 1
weight: 2110
---

## 背景：一个不以性能为目标的 ECS

2017 年 GDC，Blizzard 工程师 Timothy Ford 在演讲「Overwatch Gameplay Architecture and Netcode」中公开了 Overwatch 的架构决策。这场演讲的核心结论出乎很多人意料：Overwatch 使用了 ECS，但这个 ECS **不是为了性能而生的**。

那一年，数据导向设计（DOD）的讨论正在游戏工业中升温。Unity DOTS 的早期预览已经出现，Rust 的所有权模型让更多人开始关注内存布局。大多数人对 ECS 的期待是：Archetype 布局、SoA 存储、SIMD 向量化、多线程并行。

Overwatch 的 ECS 一条都没有。

它是一个 Managed ECS——Component 是堆上的引用类型对象，没有连续内存保证，没有 Burst 编译，没有 Job Safety System。但它依然被 Blizzard 视为成功的架构决策，并且在生产环境中支撑了 Overwatch 从发布到运营多年的版本迭代。

理解这件事，需要先把 ECS 的两种价值拆开来看。

---

## 问题根源：传统组件模型在复杂技能交互下的边界崩塌

Overwatch 的设计目标之一是英雄技能的极度多样化。每个英雄都有独特的主动技能、被动机制、终极技能，而且这些机制之间存在大量交互——某英雄的技能可以被另一英雄的能力阻断、增幅、或改变行为。

在传统的 GameObject + MonoBehaviour（或类似的面向对象组件）模型下，这类交互会产生几个经典问题：

**上帝类蔓延。** 每加一个英雄，就需要在基类或通用管理器里加入对新行为的判断。随着英雄数量增长，处理通用逻辑的类会膨胀成知道一切的「上帝类」。

**逻辑边界模糊。** 一个技能的效果可能散落在多个组件里，修改一个英雄的行为时，很难判断会不会意外影响到另一个英雄。

**可测试性差。** 技能逻辑依赖运行时对象图，单元测试必须构造完整的游戏对象树，测试成本极高。

**网络同步困难。** 状态同步需要知道「什么变了」，而面向对象的对象封装恰恰隐藏了状态边界，难以精确 diff。

Ford 在演讲中明确指出，Overwatch 引入 ECS 的首要动机是**让逻辑边界变得清晰**，而不是让 CPU 跑得更快。

---

## Overwatch ECS 的技术实现

Overwatch 的 ECS 是 Blizzard 内部自研的，大致遵循标准 ECS 语义：

- **Entity**：一个轻量 ID，没有行为，没有数据。
- **Component**：附加在 Entity 上的数据载体。但在 Overwatch 的实现中，Component 是 Managed 类（引用类型），分配在托管堆上，不保证内存连续。
- **System**：处理特定 Component 组合的逻辑单元。每个 System 声明它关心哪些 Component，只处理拥有这些 Component 的 Entity。

**没有 Archetype/Chunk 布局。** Unity DOTS 的 Archetype 把拥有相同 Component 集合的 Entity 打包进连续内存块，实现 SoA（Structure of Arrays）访问模式。Overwatch 的 ECS 没有这一层，Entity 的 Component 数据散落在各自的堆对象中。

**没有 SIMD 优化路径。** SoA + 连续内存是 SIMD 向量化的前提。Managed 对象没有这个前提，也就无法被 Burst 这类编译器优化。Overwatch 的 ECS 在这一维度上和普通面向对象代码没有本质区别。

**没有结构化并发保护。** DOTS 的 Job Safety System 会在编译期和运行时检测 Component 访问冲突，Overwatch 的 ECS 没有等价机制。

这是一个**软件工程意义上的 ECS**，不是**数据导向意义上的 ECS**。

---

## 它实际解决了什么

即便没有任何性能层面的优化，Overwatch ECS 依然带来了明确的工程收益。

**英雄能力的可组合性。** 每个英雄的能力被建模为一组 Component 的组合：移动组件、护盾组件、瞬移组件、弹跳组件……新英雄的开发变成了「选择和组合已有 Component，加上新的专属 Component」。这个思路和 Unity 的 Component 模式相似，但 ECS 的关键差别在于逻辑处于 System 而不是 Component 本身，Component 纯粹是数据。

**逻辑隔离。** 每个 System 只能看到它声明的 Component，不会有任何一个 System 知道整个游戏世界的状态。这个强制边界让代码审查、调试和修改的范围变得可预测。修改护盾逻辑的工程师只需要关注 ShieldSystem，不需要担心是否会意外触碰到移动或射击逻辑。

**网络同步的自然契合。** Overwatch 是一款高节奏多人游戏，网络同步是核心难题。Entity + Component 的数据模型天然契合状态同步的需求：服务端只需要发送「哪个 Entity 的哪个 Component 发生了变化」，客户端接收后定点更新。相比整个对象序列化，这种粒度更细、带宽更节省，而且不需要客户端理解整个游戏对象的结构。Ford 在演讲中特别强调了这一点，ECS 是 Overwatch 网络架构的重要基础。

**可测试性提升。** System 只依赖传入的 Component 数据，测试一个 System 只需要构造几个带有特定 Component 的 Entity，不需要完整的游戏运行时。这让 Blizzard 能够对技能逻辑编写有意义的单元测试，显著降低了回归风险。

---

## 它没有解决什么

理解一项技术的边界，和理解它的能力同样重要。

**内存局部性。** Overwatch 的战斗中同时存在几十个英雄、几百个技能效果、大量子弹。遍历这些对象时，Managed Component 散落在堆上，CPU 缓存命中率低，每次访问可能触发 cache miss。在数量级更大的场景（比如 RTS 的几千单位），这个问题会显著放大。但对于 Overwatch 的对象数量，这不是性能瓶颈。

**SIMD 向量化。** 物理模拟、粒子系统、大规模 AI 计算这类任务，需要对大量相同类型的数据执行相同操作，SIMD 可以几倍甚至十几倍地提升吞吐量。Managed ECS 无法利用这一点。

**结构化多线程。** 现代引擎大量使用多线程提升 CPU 利用率。DOTS 的 Job System 让多个 System 可以安全并行，Overwatch 的 ECS 在这方面没有内建支持。

---

## 两种价值的分解

ECS 通常被作为一个整体概念讨论，但它实际上承载了两类性质不同的价值，可以独立获取：

| 价值类型 | 来源 | Overwatch ECS | DOTS ECS |
|---------|------|:------------:|:--------:|
| 逻辑隔离 | ECS 架构模式本身 | ✅ | ✅ |
| 可组合性 | Component 数据模型 | ✅ | ✅ |
| 网络同步友好 | Entity/Component 状态粒度 | ✅ | ✅ |
| Cache-friendly 遍历 | Archetype/Chunk SoA 布局 | ❌ | ✅ |
| SIMD 向量化 | Burst + 连续内存 | ❌ | ✅ |
| 结构化多线程安全 | Job Safety System | ❌ | ✅ |

Overwatch 只需要上半部分。它的瓶颈从来不是「CPU 处理几十个英雄太慢」，而是「几十个英雄的交互逻辑太复杂、太难维护」。

DOTS ECS 是在 Managed ECS 的架构价值之上，叠加了数据导向的性能价值。这是一个更大的工程投入，需要开发者理解 Archetype、Chunk、NativeArray、Burst 限制。它的适用场景是需要大规模仿真（几万个单位、实时物理、密集粒子）的游戏，在这类场景下，性能价值是必须的，不可妥协。

但如果一个项目的复杂度来源于**逻辑交互**而非**数量规模**，Managed ECS 是完全合理的选择。

---

## 工程启示

这个案例给出了一个清晰的设计决策框架。

**先诊断瓶颈类型。** 引入 ECS 之前，问自己：我要解决的是「代码太乱、边界不清、测试困难」，还是「CPU 帧时间超预算、需要 SIMD 或大规模并行」？前者用 Managed ECS 就够，后者才需要 DOTS/Mass 级别的底层布局。

**不要为了 DOD 而 DOD。** 数据导向是一种优化手段，不是信仰。Overwatch 的选择证明：即使不追求 cache-friendly 布局，ECS 的架构约束本身就能带来巨大的工程收益。过早引入 DOTS 的复杂性，可能在团队还没面临性能瓶颈时，就先被工具链的学习成本和限制拖慢迭代速度。

**架构决策要匹配对象数量级。** Overwatch 战场上的对象是几十到几百量级，远达不到需要 SoA + SIMD 的门槛。当对象数量进入几千、几万量级，性能价值才开始显现出来，这时候 Archetype 布局和 Burst 编译的投入才是合理的。

---

## 下一篇预告

Overwatch 的案例是 ECS 在「逻辑复杂度」维度的典型实践——用架构约束管理复杂交互，而不追求底层性能。

系列的下一篇将转向另一个极端：**id Software 在 DOOM Eternal（id Tech 7）中的数据导向实践**。这是一个真正追求极致 CPU 性能的案例——稠密的几何数据、高并发的可见性计算、精心设计的内存布局。两个案例放在一起，能更清晰地看到「什么情况下值得付出数据导向的全部代价」。
