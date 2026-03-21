---
title: "Unity 裁剪 02｜Managed Stripping Level 到底做了什么"
description: "拆开 Disabled、Minimal、Low、Medium、High 这几个级别真正的差别，重点说明 Minimal 不是更弱的 Low，以及为什么 IL2CPP 下没有真正的 Disabled。"
slug: "unity-stripping-02-managed-stripping-level"
weight: 51
featured: false
tags:
  - Unity
  - Build
  - Stripping
  - Linker
series: "Unity 裁剪"
---

> 如果只先记一个结论，我建议记这个：`Managed Stripping Level` 不是一根从弱到强的简单滑杆，`Minimal` 和 `Low` 之间的区别，首先是“处理范围不同”，其次才是“激进程度不同”。

上一篇我们先把 Unity 里的“裁剪”拆成了三层：`managed stripping`、`Strip Engine Code` 和 `native symbol strip`。

这一篇开始往下拆第一层，也就是最常见、也最容易踩坑的 `Managed Stripping Level`。

很多人对这个参数的理解，通常只有一句话：

`级别越高，删得越狠。`

这句直觉不能说完全错，但它会掩盖一个非常关键的事实：

`Unity 这几个级别，并不只是“强度不同”，有的连处理范围都不一样。`

如果这一点没立住，后面谈反射、`link.xml`、`[Preserve]`、误删和修复策略时，很容易全都讲歪。

## 这篇要回答什么

这篇文章主要回答四个问题：

1. `Disabled`、`Minimal`、`Low`、`Medium`、`High` 到底分别意味着什么。
2. 为什么 `Minimal` 不是“弱一点的 `Low`”。
3. 为什么 `IL2CPP` 下没有真正的 `Disabled`。
4. 工程上到底该怎么选。

如果先给一个压缩版结论，可以写成这样：

- `Disabled`：不做 managed strip。
- `Minimal`：只对一部分程序集做裁剪，其他程序集直接复制。
- `Low`：开始按“不可达代码”思路删 managed code。
- `Medium`：比 `Low` 更激进。
- `High`：比 `Medium` 更激进，而且 Unity 自己都把它映射成了 `Experimental` 规则集。

这里最值得展开的，是中间这句：

`Minimal` 和 `Low` 不是同一把刀的不同力度，它们在“刀砍到哪里”这件事上就已经不一样了。`

## 先看 Unity 文档对这几个级别怎么定义

Unity 的公开说明和当前版本行为里，对这几个枚举其实已经给了很多关键信号。

如果把它们压成中文，大意是：

- `Disabled`：不要裁剪任何 managed code。
- `Minimal`：裁剪 class libraries、`UnityEngine` 和 Windows Runtime 程序集，其他程序集直接复制。
- `Low`：删除不可达 managed code，用来减小构建体积和 Mono/IL2CPP 构建时间。
- `Medium`：用比 `Low` 更不保守的方式运行 `UnityLinker`，体积会继续下降，但可能需要维护自定义 `link.xml`，有些反射路径可能表现不同。
- `High`：尽可能多地 strip，比 `Medium` 更进一步，但代价更大，某些方法的 managed debugging 可能受影响，反射路径也更容易出问题。

光看这几句，其实已经能得出两个很重要的判断：

1. `Low / Medium / High` 是同一条主线上的不同激进程度。
2. `Minimal` 不是简单地位于这条主线最左边，因为它的描述压根不是“删不可达代码更少”，而是“只处理部分程序集，其他程序集直接复制”。

## 最容易误解的一点：Minimal 不是“更弱的 Low”

很多文章喜欢把这几个级别画成这样：

`Disabled < Minimal < Low < Medium < High`

这个画法勉强能表达风险递增，但如果拿它去理解行为，会产生一个误导：

`你会以为 Minimal 只是 Low 的一个更保守版本。`

从 Unity 文档和当前实现看，这个理解并不准确。

`Minimal` 的关键不在于“删得少一点”，而在于：

`它只 strip 某些特定程序集，其他程序集直接复制。`

而 `Low` 的关键在于：

`它已经进入了“删除不可达 managed code”的模式。`

这两者差别很大。

你可以把它理解成：

- `Minimal` 更像是“先只动 Unity 认为相对标准、相对可控的那部分程序集”。
- `Low` 才真正开始全面按依赖可达性去做 managed code 裁剪。

也正因为这样，`Minimal` 往往会给人一种“几乎没干什么”的感觉。它不是没干活，而是它的边界比很多人想象得更收。

所以如果你在项目里观察到：

- 从 `Disabled` 切到 `Minimal`，问题不大
- 从 `Minimal` 切到 `Low`，突然开始冒出反射或动态注册相关问题

这不是巧合，而是因为你跨过的不是一个普通强度档位，而是从“有限范围裁剪”进入了“按不可达代码删”的另一种工作模式。

## UnityLinker 是怎么把这些级别真正落地的

如果只看文档，很多人会觉得这些词还是有点虚。

但 Unity 当前源码里，这几个级别最终会被映射成明确的 `UnityLinker` 规则集。

从当前实现看，这几个级别最终会被映射成明确的 `UnityLinker` 规则集：

- `Minimal -> Minimal`
- `Low -> Conservative`
- `Medium -> Aggressive`
- `High -> Experimental`

这组映射非常有信息量。

它说明：

- `Low` 其实对应的是一个“保守删除”的真实规则集，而不是“默认随便裁一点”。
- `Medium` 已经进入 `Aggressive`。
- `High` 连 Unity 自己都把它映射成 `Experimental`，这基本就是在告诉你：这个级别的收益可能更大，但边界更难预测。

所以如果你要写得更准确一点，可以把这几个级别分成两组来看：

- `Minimal`：单独一类，它强调的是处理范围和基本裁剪。
- `Low / Medium / High`：同一主线，分别对应 `Conservative / Aggressive / Experimental` 三种 linker 规则集。

## IL2CPP 下为什么没有真正的 Disabled

这是第二个特别容易被讲错的点。

很多人会下意识认为：既然枚举里有 `Disabled`，那所有后端都应该支持它。

但 Unity 当前实现不是这么处理的。

先看编辑器可选项，Unity 实际上把可选级别分成了两组：

- Mono：`Disabled`、`Minimal`、`Low`、`Medium`、`High`
- IL2CPP：`Minimal`、`Low`、`Medium`、`High`

也就是说，从界面层开始，`IL2CPP` 就不打算给你真正的 `Disabled`。

再往后看构建行为，逻辑更直接：

如果某种情况下 `IL2CPP` 真的遇到了 `ManagedStrippingLevel.Disabled`，它也会被按更保守但仍然启用裁剪的模式继续处理。

也就是说，`IL2CPP` 这条链路上并不存在“完全不做 managed strip”的正常工作模式。

这也解释了 Unity 编辑器里那句很容易被忽略的提示：

`IL2CPP scripting backend 下，managed bytecode stripping 始终是开启的。`

所以这部分最准确的说法应该是：

`ManagedStrippingLevel` 这个枚举有 `Disabled`，但 IL2CPP 后端并不真正支持它；在 IL2CPP 语境里，最保守的有效选项其实是 `Minimal`。`

## 默认值还有一个版本迁移细节

如果你只看当前 getter，可能会以为 Unity 一直都是这样：

- 非 IL2CPP 默认 `Disabled`
- IL2CPP 默认 `Minimal`

当前版本的默认逻辑，的确就是这个方向。

但 Unity 还处理了兼容旧项目的迁移逻辑。

这意味着默认值并不是所有版本都完全一样，旧项目升级时，Unity 也会尽量保留原有行为，避免你在没显式改配置的情况下突然换了一套 strip 策略。

这个细节很值得放进文章里，因为它解释了一个现实里经常让人困惑的现象：

`为什么同样是“没改过这个参数”的项目，不同 Unity 版本里默认行为却不一样。`

答案不是你记错了，而是 Unity 的默认策略后来确实改过，而且还专门写了迁移代码去保旧行为。

## 那这几个级别该怎么理解

如果把文档和源码合在一起，我会建议用下面这套方式去记：

### 1. Disabled

只适合放在 Mono 语境里理解：

`不做 managed strip。`

它的价值主要是：

- 排查“是不是 linker 误删”的问题时，能提供一个最干净的对照组
- 某些强依赖动态行为的旧项目，短期内可以先用它保命

但它不该被当成长期最优解，因为它放弃的是整层 managed 裁剪收益。

### 2. Minimal

这是 `IL2CPP` 下最保守、也最容易被误读的级别。

它的核心不是“保守版可达性裁剪”，而是：

`只对 class libraries、UnityEngine 和 Windows Runtime 程序集做 strip，其他程序集直接复制。`

所以它更像一个“先在比较确定的范围内动刀”的选项。

### 3. Low

这是很多项目真正进入 managed stripping 主线的起点。

它开始明确按“不可达 managed code”来删东西，而且 Unity 把它映射到了 `Conservative` 规则集。

如果你想兼顾收益和稳定性，`Low` 往往是第一个值得认真评估的档位。

### 4. Medium

它对应 `Aggressive`。

Unity 文档已经明确提醒了两件事：

- 你更可能需要自己维护 `link.xml`
- 某些反射路径可能不再和以前表现一致

这其实已经很接近工程上的警告语了：

`如果你的项目大量依赖运行时动态发现，Medium 开始就不能只靠“感觉上应该没事”。`

### 5. High

它对应 `Experimental`。

这里最重要的，不是“它删得最狠”，而是你要看到 Unity 对它的态度：

`收益更大，但不再承诺像低档位那样容易预测。`

文档里连“某些方法的 managed debugging 可能不再工作”都写出来了，这说明它已经不是单纯的包体优化开关，而是会开始触碰可调试性和行为稳定性的边界。

## 工程上到底怎么选

如果不谈极端情况，我会给一个很实用的选择顺序：

### 1. Mono 项目

如果你只是想先判断“是不是 strip 误删”，可以先用 `Disabled` 做对照。

一旦确认项目本身没有大量高危动态路径，就可以从 `Minimal` 或 `Low` 开始试。

### 2. IL2CPP 项目

先接受一个现实：

`你没有真正的 Disabled。`

所以起点通常应该是 `Minimal`，然后再决定要不要往上提。

### 3. 动态行为很多的项目

如果项目里有这些特征：

- 大量反射
- 程序集扫描自动注册
- 字符串驱动 `Type.GetType`
- 运行时泛型构造
- 配置驱动类型名或方法名

那我会建议从 `Minimal` 或 `Low` 起步，不要一开始就上 `Medium` 或 `High`。

### 4. 想要更小体积的项目

那就别只盯着“切到 `High`”。

更实际的顺序通常是：

1. 先把高危动态代码点找出来。
2. 把该显式引用、该生成注册表、该补 `link.xml` 的地方补上。
3. 然后从 `Low` 往 `Medium` 试。
4. `High` 只在你已经能稳定回归验证时再考虑。

因为真正决定你能不能把级别拉高的，往往不是“Unity 敢不敢删”，而是：

`你的项目依赖关系，是否已经足够让构建期看见。`

## 这一篇最该带走的三句话

如果把这篇文章最后再压缩一次，我建议记住这三句：

- `Minimal` 不是“更弱的 Low”，它首先是一个处理范围更收的级别。
- `Low / Medium / High` 才是同一条按可达性裁剪、逐步变激进的主线，对应 `Conservative / Aggressive / Experimental`。
- `IL2CPP` 下没有真正的 `Disabled`，最保守的有效选项其实是 `Minimal`。

下一篇，我们就顺着这条线往下讲一个最关键的问题：

`为什么 Unity 有时看不懂你的反射。`
