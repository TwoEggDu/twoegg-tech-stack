---
date: "2026-03-24"
title: "热更新场景下的 Shader 交付架构：bundle 边界、版本对齐与变体保护策略"
description: "把热更新项目里 shader 交付的核心问题拆成 bundle 边界设计、shader bundle 独立性、版本对齐和变体保护四层，讲清楚如何避免热更内容里的 shader 变体在运行时缺失。"
slug: "unity-shader-delivery-in-hotupdate"
weight: 100
featured: false
tags:
  - "Unity"
  - "Shader"
  - "AssetBundle"
  - "HotUpdate"
  - "Variant"
  - "Addressables"
series: "Unity Shader Variant 治理"
---
前面的文章已经把这个核心问题讲清楚了：

热更新包里的材质如果没有参与 Player 构建，它们的 keyword 组合就不会进入 `usedKeywords`，对应的变体就不会被生成——这是热更项目里 shader 变体缺失问题最常见的根因。

也讲了修法：用 SVC 显式登记 keyword 组合，或者用 Always Included 全局兜底。

但没有讲的是：

`项目层面，shader 的交付架构应该怎么设计，才能从结构上避免这类问题，而不是每次出问题了靠 SVC 或 Always Included 补洞？`

这篇专门讲这个架构层面的问题。

## 先给一句总判断

`热更新场景下 shader 交付的核心原则是：shader 的生命周期应该和使用它的内容生命周期对齐；如果内容会热更，shader 要么提前在 Player 里兜底，要么和内容一起交付，要么通过 SVC 让构建期提前感知到——三条路都行，但不能让 shader 处于"内容在但 shader 变体不在"的状态。`

## 一、先把热更新场景下的问题结构说清楚

热更新项目和普通 AssetBundle 项目最大的区别是：

`内容可以在 Player 发布之后才决定最终状态。`

这带来了一个根本性的张力：

- Player 构建时，hot patch 的内容还不确定（或者根本不存在）
- 但变体生成需要在 Player 构建期就完成
- 所以 hot patch 里材质的 keyword 组合，在 Player 构建期根本无法被自动收集

这就是为什么热更新包里总容易出现 shader 变体缺失——不是 stripping 太激进，而是构建期根本没机会看到这些材质。

## 二、三条可行路径

### 路径一：shader 在 Player 层全局兜底

**适合场景**：shader 数量少，变体组合相对固定，项目愿意用 Player 体积换交付简单性。

**做法**：

把热更内容会用到的 shader 加入 `Always Included Shaders`，或者把 shader 资产放进 Resources 或随 Player 一起打包的 bundle。

**工程含义**：

- Player 构建时，这些 shader 的变体按 `kShaderStripGlobalOnly` 策略生成——不依赖材质的 keyword 使用面，只按全局渲染配置做粗剔
- 热更内容加载后，材质引用的 shader 已经在 Player 内存里，不需要从 bundle 里加载
- bundle 里只存 PPtr 引用，不存 shader 实体

**风险**：

- 随着热更内容增加，被"全局兜底"的 shader 越来越多，Player 体积上升
- 如果热更内容引入了新的 shader（不只是新材质），这条路就不够用了

### 路径二：shader bundle 和内容 bundle 分开管理，显式版本对齐

**适合场景**：项目有完善的内容管线，shader 可以独立成包，热更内容能和 shader 包一起版本化。

**做法**：

把 shader 资产单独打成 shader bundle（或少数几个 shader bundle），内容 bundle 依赖 shader bundle。热更时，如果内容引入了新 shader 或新 keyword 组合，连同对应的 shader bundle 一起更新。

**工程含义**：

- shader bundle 作为共享依赖，多个内容 bundle 可以引用同一个 shader bundle
- shader bundle 单独版本化，内容包和 shader 包的版本关系显式管理
- 运行时加载内容 bundle 之前，先确保对应版本的 shader bundle 已经加载

**关键要求**：

内容 bundle 引入新 keyword 组合时，必须同时更新 shader bundle——这要求内容发布流程里有一步"重新构建 shader bundle"。如果漏掉这一步，就会出现内容已更新但 shader bundle 里的变体还是旧版本的情况。

**风险**：

- 需要维护 shader bundle 和内容 bundle 之间的显式版本依赖关系
- 发布流程更复杂，容易出现"内容更新了，shader bundle 忘记更新"的事故

### 路径三：SVC 作为构建期的 keyword 代理

**适合场景**：无法提前知道热更内容的具体 keyword 组合，但可以维护一份"已知高风险 keyword 组合"的显式清单。

**做法**：

维护一个或多个 SVC，里面登记热更内容可能用到的 keyword 组合。这些 SVC 参与 Player 构建，让相关 keyword 组合进入 `usedKeywords`，从而被枚举生成。

**工程含义**：

- SVC 作为"构建期代理"，代替热更内容材质向构建系统声明 keyword 需求
- Player 构建时，这些变体就已经在包里
- 热更内容加载后，需要的变体已经存在，不会缺失

**关键要求**：

SVC 里的 keyword 组合必须和热更内容实际使用的保持同步。如果热更内容引入了新的 keyword 组合，但 SVC 没有更新，这条路就失效了。

**风险**：

- SVC 和热更内容的同步需要纪律性——SVC 是滞后的，热更内容随时可能引入新组合
- SVC 如果覆盖面不足，线上仍然会出问题；如果覆盖面太宽，变体数量失控

## 三、三条路径的对比

| 维度 | Player 兜底 | Shader Bundle 分离 | SVC 代理 |
|------|------------|------------------|---------|
| 实现复杂度 | 低 | 高 | 中 |
| Player 体积影响 | 大 | 小 | 中 |
| 热更灵活性 | 低（新 shader 需要 Player 更新） | 高 | 中（SVC 需要同步更新） |
| 出问题的风险点 | Player 体积失控 | 版本依赖管理疏漏 | SVC 和内容脱节 |
| 适合规模 | 小项目 / shader 种类少 | 大项目 / 有独立内容管线 | 中等项目 / shader 种类可枚举 |

## 四、版本对齐：最容易被忽视的问题

不管选哪条路径，**版本对齐**都是热更新场景下最容易出事故的环节。

典型事故模式：

1. 旧版本 Player 用 shader bundle A（包含变体集 V1）
2. 热更内容引入了新的 keyword 组合
3. shader bundle 更新为 B（包含变体集 V2）
4. 部分用户还在用旧版本 Player + 旧版本 shader bundle A
5. 这批用户加载新热更内容时，缺少 V2 里的变体 → 粉材质

**处理方式**：

- 如果 shader bundle 更新意味着兼容性断裂，这次热更需要强制 Player 更新
- 如果希望新旧 Player 都能兼容，shader bundle 更新必须保持向后兼容（保留旧变体，只增加新变体）
- 用版本号显式标记 shader bundle 和内容 bundle 的兼容关系，加载时做版本检查

## 五、SVC 在热更场景里的正确使用方式

路径三里用 SVC 做 keyword 代理，有几个细节需要注意：

### 1. SVC 应该按内容域分组，而不是一个全项目大 SVC

如果热更内容按活动、DLC、场景分包，SVC 也应该对应分组：

- 活动 A 的内容 bundle → 附带活动 A 的 SVC
- 活动 B 的内容 bundle → 附带活动 B 的 SVC

这样才能：

- 清楚知道哪个 SVC 保护哪批内容的变体
- 内容下线时，对应 SVC 也可以一起清理
- 新活动上线时，只需要构建新的 SVC，不影响其他

### 2. SVC 要参与 Player 构建，才能发挥 keyword 代理作用

SVC 如果只是被热更内容 bundle 引用，但没有参与 Player 构建，它对变体生成没有任何影响。

SVC 作为 keyword 代理，必须在 Player 构建期就被 `ComputeBuildUsageTagOnObjects` 收集到——也就是必须在 Player 构建的 `allObjects` 集合里。

最直接的方式：把 SVC 放进 Resources 目录，或者在 Player 的场景 / 预制体里引用它。

### 3. SVC 也要跟内容一起更新

如果热更内容引入了新的 keyword 组合，对应的 SVC 需要更新，然后重新触发一次 Player 构建（或者至少重新触发 shader bundle 构建，让新变体被生成）。

这意味着：**SVC 的更新不能绕过 Player 构建**。如果希望新变体出现在包里，必须重新构建。

## 六、最小安全实践

如果项目刚开始做热更，还没有完整的 shader 交付架构，可以先用这套最小安全实践：

1. **把最核心的基础 shader 加进 Always Included**：数量控制在 10 个以内，只放真正全局基础的 shader（UI、角色主材质等）
2. **按业务模块维护 SVC**：每个会热更的业务模块（活动、副本、新角色）有自己的 SVC，登记该模块已知的 keyword 组合
3. **SVC 参与每次 Player 构建**：确保它们在 `allObjects` 范围内
4. **新模块上线前，更新 SVC 并重新构建 Player**：不要在 Player 没重新构建的情况下上线引入新 keyword 组合的热更内容
5. **关键场景上线前跑真机验证**：新活动或新模块的首次加载路径必须在目标平台真机上验证，不能只看编辑器

## 最后收成一句话

`热更新场景下 shader 变体问题的根本原因是构建期看不到热更内容的材质；解法有三条：Player 兜底（简单但有体积代价）、shader bundle 独立版本化（灵活但管理复杂）、SVC 显式代理（折中但需要同步纪律）；无论选哪条，版本对齐都是最容易被忽视、最容易导致线上事故的环节。`
