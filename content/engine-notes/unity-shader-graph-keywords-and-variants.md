+++
title = "Shader Graph 的 Keyword 节点与变体：Boolean、Enum 和 _local 在 Shader Graph 里怎么用"
description = "把 Shader Graph 的 Boolean Keyword、Enum Keyword 节点与 multi_compile、shader_feature 的对应关系讲清楚，说明 Shader Graph 项目里 keyword 设计错误是怎么导致变体爆炸的。"
slug = "unity-shader-graph-keywords-and-variants"
weight = 30
featured = false
tags = ["Unity", "Shader", "ShaderGraph", "Keyword", "Variant", "URP"]
series = ["Unity 资产系统与序列化", "Unity Shader Variant 治理"]
+++

上一篇把手写 shader 的 keyword 设计讲清楚了：

`multi_compile` 总是生成变体，`shader_feature` 只在材质使用时生成变体，`_local` 解决全局 keyword 空间污染。

但很多 URP 项目不直接写 `.shader` 文件，而是用 Shader Graph。

Shader Graph 里没有 `#pragma multi_compile` 这些语句，取而代之的是 **Keyword 节点**。

如果不清楚这些节点背后对应什么，就很容易在不知情的情况下把变体数量做大——因为节点本身看起来只是一个可视化的开关，完全看不出它会翻倍变体数量。

所以这篇只讲一件事：

`Shader Graph 里的 Keyword 节点和变体的关系。`

## 先给一句总判断

`Shader Graph 的 Keyword 节点会生成和手写 shader 完全等价的变体——Boolean Keyword 对应一条 multi_compile 或 shader_feature 声明，Enum Keyword 对应多值声明；节点背后的变体逻辑和手写 shader 一样，只是换了一种可视化的输入方式。`

## 一、Shader Graph Keyword 节点的两种类型

在 Shader Graph 的 Blackboard 里可以创建 Keyword，目前有两种：

### 1. Boolean Keyword

对应手写 shader 里的单个布尔开关：

```glsl
// 等价的手写 shader 声明
#pragma shader_feature __ _FEATURE_ON
// 或
#pragma multi_compile __ _FEATURE_ON
```

在 Shader Graph 里，Boolean Keyword 会生成两条变体路径：keyword 关闭时走一条，keyword 开启时走另一条。

### 2. Enum Keyword

对应手写 shader 里的多值枚举：

```glsl
// 等价的手写 shader 声明（以三个值为例）
#pragma shader_feature _QUALITY_LOW _QUALITY_MEDIUM _QUALITY_HIGH
// 或
#pragma multi_compile _QUALITY_LOW _QUALITY_MEDIUM _QUALITY_HIGH
```

Enum Keyword 有多少个 Entry，就生成多少条变体路径，是线性增长，不是乘积增长。

## 二、Definition 属性决定变体生成行为

每个 Keyword 节点都有一个 **Definition** 属性，这才是决定变体生成行为的关键：

| Definition | 等价手写声明 | 变体生成行为 |
|-----------|-------------|------------|
| `Shader Feature` | `shader_feature` | 只在材质实际启用时生成对应变体 |
| `Multi Compile` | `multi_compile` | 无论材质是否使用，所有组合都生成 |

除此之外还有一个 **Scope** 属性：

| Scope | 等价手写声明 | 作用 |
|-------|------------|------|
| `Global` | 不带 `_local` | 占用全局 keyword 槽位 |
| `Local` | `_local` 后缀 | 只占当前 shader 的本地槽位 |

**默认值的陷阱**：Shader Graph 创建 Keyword 时，默认 Definition 是 `Shader Feature`，Scope 是 `Local`。这个默认值是合理的，但如果开发者不理解差异、随手改成 `Multi Compile`，变体数量就会立刻翻倍。

## 三、Shader Graph Keyword 的变体计算方式和手写 shader 完全一样

这里没有任何特殊逻辑：

- 每增加一个 **Boolean + Multi Compile**：变体数 × 2
- 每增加一个 **Boolean + Shader Feature**：变体数最多 × 2（取决于材质使用面）
- 每增加一个 **Enum（N 个 Entry）+ Multi Compile**：变体数 × N
- 每增加一个 **Enum（N 个 Entry）+ Shader Feature**：变体数最多 × N

所以 Shader Graph 项目完全可能踩和手写 shader 一样的变体爆炸陷阱，只是触发方式变成了"随手往 Blackboard 里加几个 Keyword 节点"。

## 四、Shader Graph 特有的几个场景

### 1. URP 内置的全局 Keyword 已经计入总量

Shader Graph 生成的 shader 会自动包含 URP 管线要求的全局 keyword（主光源阴影、额外光源、SSAO 等）。这些 keyword 对应的变体数量不在你的 Blackboard 里体现，但确实存在于最终构建产物里。

这意味着：自定义 Keyword 带来的变体增长，是叠加在 URP 基础变体数量之上的。

### 2. Sub Graph 里的 Keyword 会向上传播

如果在 Sub Graph 里定义了 Keyword，使用这个 Sub Graph 的所有父 Graph 都会继承这个 Keyword，并产生对应变体。

一个被大量 Graph 共用的 Sub Graph 里加了一个 `Multi Compile` Keyword，影响范围可能非常大。

### 3. 从 Shader Graph 切换到手写 shader 的迁移场景

有些项目会把 Shader Graph 生成的 shader 导出成手写 shader，再做进一步优化。导出后 Keyword 节点会变成对应的 `#pragma` 声明，此时可以按手写 shader 的规则继续治理。

## 五、实际决策建议

在 Shader Graph 里创建 Keyword 时，先问三个问题：

**1. 这个开关是材质静态配置，还是运行时动态切换？**

- 材质静态配置（Inspector 里设一次就不变）→ `Shader Feature`
- 运行时脚本会动态 Enable/Disable → `Multi Compile`

**2. 这个 Keyword 会被多少个 Shader Graph 使用？**

如果只有一个 Graph 用，`Global` 和 `Local` 差别不大。如果会被多个 Graph 共用（尤其通过 Sub Graph），优先用 `Local`，避免全局 keyword 槽位耗尽。

**3. 多个互斥选项是否该合并成 Enum？**

三个表示"质量档"的 Boolean Keyword，换成一个三值 Enum Keyword，变体数从 2³=8 降到 3。同样的道理适用于 Shader Graph 和手写 shader。

## 六、怎么审计 Shader Graph 项目的 Keyword 使用情况

`IPreprocessShaders` 对 Shader Graph 生成的 shader 同样有效——Shader Graph 最终还是会生成标准的 shader，所以构建期的 variant 记录、统计和对比方法和手写 shader 完全一致。

发现某个 Shader Graph shader 变体数量异常多时，排查路径：

1. 打开 Shader Graph，查看 Blackboard 里的 Keyword 列表
2. 检查每个 Keyword 的 Definition（`Multi Compile` 还是 `Shader Feature`）
3. 检查是否有互斥的布尔 Keyword 可以合并成 Enum
4. 检查 Sub Graph 依赖链，确认没有从上游继承了不必要的 Keyword

## 最后收成一句话

`Shader Graph 的 Keyword 节点和手写 shader 的 #pragma 声明是等价的——Definition 决定是 multi_compile 还是 shader_feature，Scope 决定是否占用全局 keyword 槽位；Shader Graph 项目的变体治理逻辑与手写 shader 完全一致，只是触发变体爆炸的入口变成了 Blackboard 里的节点操作。`
