---
date: "2026-03-24"
title: "Unity Shader Variant 实操：怎么知道项目用了哪些、运行时缺了哪些、以及怎么剔除不需要的"
description: "一篇偏实操的 Shader Variant 治理稿：从构建记录、运行时定位到 stripping 和 SVC 清理，给出一套可以直接在项目里落地的排查顺序。"
slug: "unity-shader-variants-how-to-find-missing-and-strip"
weight: 140
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Build"
  - "Rendering"
series: "Unity Shader Variant 治理"
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
上一篇我主要在讲：

`为什么 Unity 会有 Shader Variants，以及为什么它最后很容易变成工程治理问题。`

但真到项目里，大家更常卡住的不是原理，而是实操：

- 我怎么知道项目到底用了哪些 `Shader Variant`
- 我怎么知道运行时丢了哪些 variant，或者丢了哪些 `SVC`
- 我怎么把不需要的 shader variant 剔掉，又不把线上内容删坏

所以这篇我不打算再讲大框架，而是直接写成一篇偏执行的稿子。

目标不是“让你理解这个概念”，而是让你最后真的能在项目里做出下面这些东西：

- 一份构建期 `variant` 报告
- 一份项目 `SVC` 清单
- 一份运行时缺口清单
- 一套可重复执行的 stripping 规则
- 一套关键场景回归表

只要这几样东西做出来，Shader Variant 问题就不再是“玄学”，而会重新变回工程问题。

## 一、先把三个问题分开

### 1. “项目用了哪些 Shader Variant”是在问什么

这句话真正想问的，通常不是“项目目录里有多少可能的 variant”，而是：

`在真实构建和真实内容下，哪些 shader keyword 组合、pass、平台路径真的会被项目用到。`

它更接近“真实使用面”。

### 2. “运行时丢了哪些”是在问什么

这句话真正想查的是：

- 某些运行时会走到的 variant 根本没被编进包
- 或者 variant 编进去了，但没有被提前预热
- 或者某份 `Shader Variant Collection` 没被带上、没被加载、没被 `WarmUp`

它更接近“线上缺口”。

### 3. “怎么剔除不需要的 Shader Variant”是在问什么

这句话问的是：

`在不影响真实运行路径的前提下，怎样把构建时那些根本不会用到的 variant 排除掉。`

它更接近“构建治理”。

这三件事的关系可以粗略理解成：

- 先知道项目真的用了什么
- 再知道现在到底缺了什么
- 最后才有资格去删不需要的东西

顺序反了，通常就会出事故。

## 二、先准备最小工具，不要直接靠体感查

如果你真要落地治理，建议先准备下面四样东西。

### 1. 一份构建期 variant 记录器

核心入口就是 `IPreprocessShaders`。

原因很简单：

- 你想知道这次构建到底保留了哪些 variant
- Unity 真正处理 variant 的地方就在这条链上

如果不接这个口，你后面很多判断都会停留在“我感觉它应该会编进去”。

下面是一个够用的示意版本：

```csharp
public class ShaderVariantAudit : IPreprocessShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(
        Shader shader,
        ShaderSnippetData snippet,
        IList<ShaderCompilerData> compilerDataList)
    {
        foreach (var compilerData in compilerDataList)
        {
            var keywords = ExtractKeywords(compilerData); // 按项目 Unity 版本实现

            WriteOneLine(
                shaderName: shader.name,
                passName: snippet.passName,
                passType: snippet.passType.ToString(),
                stage: snippet.shaderType.ToString(),
                keywords: keywords);
        }
    }
}
```

这里最重要的不是代码写得多复杂，而是你最终能把下面这些字段落下来：

- `shader name`
- `pass name / pass type`
- `shader stage`
- `keyword list`
- `build target`
- `quality / pipeline / build flavor`

如果这几个字段没有，你后面很难回答“到底是没编进去，还是编进去了但没被预热”。

### 2. 一份 SVC 清单

这份清单至少要包含：

- `Graphics Settings -> Preloaded Shaders` 里挂了哪些 SVC
- 代码里显式加载了哪些 `ShaderVariantCollection`
- 哪些 Addressables / AssetBundle / ScriptableObject 间接引用了 `.shadervariants`
- 哪些地方调用了 `WarmUp`

这份表不用一开始做得很美，但字段要有：

- SVC 名字
- 来源
- 加载时机
- 由谁负责
- 是否真在目标平台使用

### 3. 一份关键场景回归表

不要等删坏了再临时想测什么。

先把最关键的路径写下来：

- 首次进入主场景
- 首次进入战斗
- 首次释放高频技能
- 首次出现大特效
- 首次切换画质档位
- 首次切平台或切图形 API
- AssetBundle / Addressables 首次独立加载的场景

这张表后面会决定你每次裁剪完到底怎么验。

### 4. 一份运行时问题记录模板

每次发现“是不是缺 variant”时，都按同一格式记：

- 平台
- 图形 API
- 质量档
- 出问题的场景
- 出问题的材质 / 特效 / pass
- 现象是粉、错、丢失，还是首次卡顿

别小看这张表，它决定你后面能不能把问题分对类。

## 三、怎么知道项目到底用了哪些 Shader Variant

这个问题最容易掉进一个坑：

`不要从“可能性”开始，要从“真实运行证据”开始。`

因为 shader variant 的理论组合永远比项目真实使用面大得多。

### 第一步：先把统计边界定死

在开始拉数据之前，先明确你到底在统计哪一组构建。

最起码先固定这几件事：

- 目标平台
- 图形 API
- Render Pipeline 资产
- 质量档
- 是 Player Build 还是 AssetBundle Build

因为这些东西一变，variant 集合就会变。

很多团队第一天就错在这里：

- Editor 看的是一套
- Player Build 出的是一套
- AssetBundle Build 又是另一套

最后拉出来的数字根本不能互相对比。

### 第二步：先从真实资源和真实配置缩小范围

一个 variant 会不会真的被项目用到，通常取决于几层东西一起作用：

- Shader 本身有哪些 keyword
- 材质到底启用了哪些 keyword 组合
- 场景和资源里到底引用了哪些 shader / material
- 当前 Render Pipeline 开了哪些 feature
- 平台、图形 API、质量档位会走哪些分支

所以这一步不要先看 SVC，而要先认清：

`真实使用的 variant，首先来自真实资源和真实配置。`

也就是说，如果一个 shader 有十个 keyword，不代表项目真的用了所有组合。很多组合只是理论上能编，但线上内容根本不会走到。

### 第三步：跑一次正式构建，把“实际保留面”拉出来

这里不要只看 Editor。

直接跑一次目标平台的正式构建，然后拿下面这些信息：

- Build 过程里的 shader variant 统计
- stripping 前后的数量变化
- 哪些 shader / pass / keyword 组合保留了
- 哪些被 strip 掉了

这一步的价值在于，它开始把“理论可能”缩成“本次构建实际保留”。

但要注意：

`构建里保留了，不代表运行时一定会用到；构建里没保留，则运行时一定不可能用到。`

这是后面排查缺失问题的重要前提。

### 第四步：把 build 记录器真正落成一张表

建议最终至少落成一份 `csv` 或 `json`，每一行就是一个 variant 记录。

如果你不想一上来就记太细，最低限度也要能按这几个维度聚合：

- 按 `shader` 聚合
- 按 `shader + pass` 聚合
- 按 `shader + pass + keywords` 聚合

你至少要能回答下面这些问题：

- 哪几个 shader 是 variant 大户
- 哪几个 pass 在放大量
- 哪些 keyword 组合最常见
- 同一个 shader 在不同平台上差多少

如果这几个问题答不出来，后面谈 stripping 基本就是盲改。

### 第五步：补运行时真实命中的证据

仅靠构建报告，你知道的是“编进去了哪些”。

如果你还想知道“项目真正用了哪些”，还要补运行时证据。

这里最实用的做法不是追求 100% 完整枚举，而是先抓关键路径：

- 问题最频繁的场景
- 最贵的玩法入口
- 首次命中最容易卡的特效
- 平台差异最大的场景

常见手段包括：

- Frame Debugger 看 draw call 实际走了哪个 shader / pass
- 对出问题材质把当前 keyword 状态打出来
- 对关键特效第一次出现前后打 marker
- 对 `SVC.WarmUp()` 打耗时日志

例如，SVC 预热本身就应该被计时：

```csharp
var sw = Stopwatch.StartNew();
svc.WarmUp();
Debug.Log($"Warmup {svc.name}: {sw.ElapsedMilliseconds} ms");
```

这一步的目标不是“完美统计全部 runtime variant”，而是把注意力拉回真实高风险路径。

### 第六步：把 SVC 放回正确位置

`Shader Variant Collection` 当然也要看，但它只能回答其中一部分问题：

- 你预期哪些 variant 值得提前准备
- 你为哪些路径做了显式预热

它回答不了：

- 项目真实会不会命中全部这些 variant
- 还有没有别的 variant 没进 SVC 但运行时照样会用到
- 当前构建是不是已经把某些关键 variant strip 掉了

所以如果你的问题是“项目用了哪些 shader variant”，`SVC` 只是证据之一，绝对不是全部答案。

## 四、怎么知道运行时丢了哪些 variant，或者丢了哪些 SVC

这个问题本质上是在找“构建结果”和“运行时需求”之间的缺口。

我一般会先把“丢了哪些”拆成两类。

### 第一步：先根据现象分流，不要一上来全怀疑

最实用的第一刀通常是按现象判断：

- 粉材质、pass 丢失、光照或阴影直接不对
- 编辑器正常，Player 异常
- 某个平台、某个质量档位才坏

这类优先怀疑：

`variant 根本没编进去，或者构建时被 strip 掉了。`

另一类现象通常是：

- 第一次出现某个特效卡一下
- 第一次进某个场景卡一下
- 第一次切画质或特定渲染路径卡一下
- 后续再次出现就好多了

这类优先怀疑：

`variant 在包里，但没有被预热；或者本来依赖某份 SVC 来预热，但 SVC 没带上、没加载、没 WarmUp。`

### 第二步：先查”缺 variant”这一支

发现 variant 没编进包，需要进一步区分两种根本不同的原因，因为它们的修法完全不同。

#### 情况 A：这条 variant 压根没有进入生成阶段（从未被枚举）

Unity 在构建期收集 `usedKeywords` 时，遍历的是当前构建的 `allObjects` 集合——也就是 Player 场景、Resources、以及本次参与构建的 AssetBundle 里的所有对象。

如果某个材质不在这个集合里（比如它在一个独立热更新包，或一个只在运行时下载的 AssetBundle，而这个 bundle 没有参与本次 Player 构建），那么这个材质上启用的 keyword 组合就永远不会出现在 `usedKeywords` 里。

`PrepareEnumeration` 只枚举出现在 `usedKeywords` 里的组合，所以这条 variant 从一开始就没有机会生成，更不会进入后面的 stripping 阶段。

**修法**：让材质参与构建（放入 Player 或参与构建的 AB），或者用 `SVC` 显式登记这条 keyword 组合（SVC 的 variant 会并入 `usedKeywords`），或者把 shader 加进 `Always Included`（`kShaderStripGlobalOnly` 不依赖 `usedKeywords`）。

#### 情况 B：这条 variant 进入了生成阶段，但被后续 stripping 删掉

这是”进去了再出来”。原因包括：

- Unity 内置 stripping（`ShouldShaderKeywordVariantBeStripped`）：基于全局使用状态（雾效关闭、光照贴图模式等）
- 自定义 `IPreprocessShaders` 的删除逻辑

这类问题会在构建日志的 stripping 统计里体现：你会看到 `After filtering` 和 `After builtin stripping` 或 `After IPreprocessShaders` 之间的数量下降。

**修法**：审查 stripping 规则，或检查全局 Graphics 配置（如 Fog Mode、Lightmap Mode）是否过于收窄。

---

要查这种问题，最有效的不是盯运行时，而是回去对比下面两样东西：

1. 出问题时真实走到的 shader / pass / keyword 组合
2. 构建报告里它到底有没有出现

如果构建里根本没有它，先判断是 A 还是 B——看 `IPreprocessShaders` 回调是否曾经收到过这条 variant。如果连回调都没收到，说明是情况 A（枚举阶段就没生成）；如果收到了但被 `RemoveAt` 掉，说明是情况 B（stripping 删掉了）。

这里真正要做的是把“故障现场”记清楚。

最少记下这些字段：

- 材质名
- shader 名
- pass
- 平台
- 图形 API
- 质量档
- 当前 keyword 状态

旧版本项目可以直接从材质上打 `shaderKeywords`；新版本就按当前 Unity 的 local/global keyword API 把活动 keyword 打出来。重点不是 API 名字，而是：

`你必须把故障现场的 keyword 状态拿到。`

### 第三步：再查“缺 SVC / 缺预热”这一支

如果怀疑丢的是 SVC，而不是 variant 本身，那么先查：

- `Graphics Settings -> Preloaded Shaders`
- 代码里有没有 `ShaderVariantCollection`
- 有没有 `WarmUp`
- 有没有 `Resources.Load` / Addressables / AssetBundle 加载 SVC
- 哪些配置、表、ScriptableObject 间接引用了 `.shadervariants`

你真正要确认的是：

- 这份 SVC 有没有进包
- 有没有在目标平台那条链路里被带上
- 有没有在正确时机被加载
- 加载后有没有真的执行预热

很多项目的问题不是“没有 SVC”，而是：

- SVC 资产在
- 但没进目标包
- 或者进了包，但没在正确时机加载
- 或者加载了，但没 `WarmUp`
- 或者 SVC 里收录的已经不是现在真实会走到的 variant

### 第四步：把 Player Build 和 AssetBundle Build 分开查

这一点特别关键。

很多团队只在 Player Build 上查 variant，然后忽略 AssetBundle。

但真实项目里很常见的情况是：

- 主包里一切正常
- AssetBundle 独立构建后问题才出现

那你要怀疑的就不是同一类问题，而是：

- AB 构建链没有正确收集依赖
- AB 构建链和 Player 构建链使用了不同的 stripping 条件
- AB 路径下本来依赖的 SVC 没被带进去

所以实际执行时，Player 和 AB 至少要各跑一轮。

### 第五步：把故障回推到三张表上

查到最后，你应该能把每个问题归到下面三类之一：

- 构建没保留
- 构建保留了，但没预热
- 预热名单还在，但名单已经过时

只要归类完成，后面的处理方式就清楚了。

## 五、怎么剔除不需要的 Shader Variant

这一步最容易做错，因为很多人一上来就想“我把不用的 `.shadervariants` 文件删了”。

但如果目标是减少真正的 shader variant 负担，那重点通常不在删 SVC，而在更前面的几层。

### 第一步：先减源头，不要先减名单

真正放大 variant 数量的，通常是这些东西：

- `multi_compile` 用得太宽
- 本来应该是运行时参数的东西，被做成了 keyword
- 一个 shader 里堆了太多彼此并不独立的开关
- Render Pipeline feature 开得太多
- 平台、质量档、图形 API 组合太散

所以最值钱的减法往往不是“后面 strip 掉”，而是：

`前面就不要生成那么多没意义的组合。`

### 第二步：先审 keyword 设计，再审 stripping

这一步是很多项目最容易回避，但收益最大的地方。

你至少要逐类看下面这些问题：

- 这个开关真的是“改代码路径”吗，还是只是改参数
- 这个开关真的需要运行时动态切吗
- 这个开关是不是只会出现在少数材质上
- 这几个布尔开关是不是其实互斥，应该合成一个枚举

很粗地讲：

- 会被大量材质静态使用，但不会运行时乱切的，更适合 `shader_feature`
- 无论是否用到都必须保留组合的，才更接近 `multi_compile`

如果这一步不做，后面 stripping 很多时候只是在帮前面的设计失误擦屁股。

### 第三步：把“项目永远不会发生的组合”写成规则

真正适合 strip 的，不是“我猜没人会用”，而是：

`我能明确证明，这个组合在项目里永远不会发生。`

这类规则通常来自：

- 平台边界
- 画质档边界
- Render Pipeline feature 边界
- 业务边界

例如：

- 某移动端包永远不走 Deferred
- 某低端档永远不开额外光阴影
- 发行包永远不带 debug display
- 某玩法包根本不会用雾效

这类东西才适合写进 stripping 规则里。

`IPreprocessShaders` 里真正做删除的逻辑，通常长这样：

```csharp
for (var i = compilerDataList.Count - 1; i >= 0; --i)
{
    var keywords = ExtractKeywords(compilerDataList[i]); // 按项目 Unity 版本实现

    if (ProjectNeverUsesThisCombination(shader, snippet, keywords))
    {
        compilerDataList.RemoveAt(i);
    }
}
```

关键点不在 `RemoveAt`，而在 `ProjectNeverUsesThisCombination` 背后的规则来源必须可靠。

### 第四步：最后才整理 SVC

删 `SVC` 当然也有价值，但它主要减少的是：

- 启动或切场景时的预热成本
- 历史上重复收录的冗余
- 团队对预热名单的管理负担

它不天然等于：

- variant 总量大降
- 编译压力大降

所以如果你的目标是“把不需要的 shader variant 剔掉”，主战场不是删 SVC，而是：

- keyword 设计
- shader 声明方式
- pipeline 配置
- stripping 规则

SVC 更像是最后那层“预热名单治理”。

### 第五步：每次只删一类，并且用真实构建回归

最稳的节奏通常是：

1. 先改一类 keyword
2. 或先加一条 stripping 规则
3. 或先撤一组 SVC 引用
4. 跑正式构建
5. 跑关键场景回归
6. 再看报告变化

不要一次同时改 shader、pipeline、stripping、SVC。

不然一旦出回归，你根本不知道是谁干的。

## 六、一个两天能落地的最小方案

如果你们项目现在完全没有这套治理，我更建议先做一个两天版本，而不是直接追求完美体系。

### 第一天上午

- 接一个最小版 `IPreprocessShaders` 记录器
- 列出所有 SVC 来源
- 列关键场景回归表

### 第一天下午

- 跑一次 Player Build
- 跑一次 AssetBundle Build
- 导出 variant 报告
- 导出 SVC 清单

### 第二天上午

- 找出 variant 数量最多的几个 shader
- 找出线上最典型的两个问题路径
- 判断它们分别属于“缺 variant”还是“缺预热”

### 第二天下午

- 先动一条最确定的 stripping 规则
- 或先清一组最确定的历史 SVC
- 跑一轮真机构建和关键场景回归
- 记录变化

只要这轮做完，团队就已经从“完全靠猜”进化到“有数据、有归类、有节奏”了。

## 七、最容易踩的几个坑

### 1. 只在 Editor 里验证

这几乎一定不够。

Shader Variant 的很多问题只会在：

- 真机构建
- 特定图形 API
- 特定质量档
- AssetBundle 独立加载路径

下才暴露。

### 2. 只看 Player，不看 AssetBundle

如果项目有 AB / Addressables，这个坑很常见。

Player 正常，不代表 AB 正常。

### 3. 把 SVC 当成 variant 总量的主要治理手段

SVC 主要解决的是预热名单，不是 variant 根因。

### 4. 把“我猜不会发生”当成 stripping 依据

没有证据的 strip，迟早会变成线上事故。

### 5. 不给规则写责任人和适用边界

任何 stripping 规则和 SVC 清理，如果没有：

- 适用平台
- 适用包型
- 责任人
- 回归场景

几个月后就会重新变成遗留问题。

## 八、我会怎么压缩成一句工程判断

如果只能留一句最有用的话，我会这样说：

`Shader Variant 治理不是先删，而是先做三张表：构建保留表、运行时缺口表、SVC 清单。`

这三张表一旦有了，后面的动作就都能落到明确对象上：

- 哪些 shader 先治理
- 哪些问题是缺 variant
- 哪些问题是缺预热
- 哪些规则可以安全 strip
- 哪些 SVC 已经过时

## 结论

最后把这篇压成几句执行上的结论：

1. 想知道项目用了哪些 shader variant，先接 `IPreprocessShaders`，把构建实际保留面记录下来，再用关键场景的运行时证据补真实需求面。
2. 想知道运行时丢了哪些，先分清是“根本没编进去”还是“编进去了但没预热”；前者优先查构建和 stripping，后者优先查 SVC 和 `WarmUp`。
3. 想剔除不需要的 shader variant，不要先删 SVC，而要先审 keyword 设计，再写有证据的 stripping 规则，最后再整理预热名单。
4. 任何 variant 治理动作都必须在真实构建、真实平台、真实关键场景里回归，不要只看 Editor。

所以真正该问的不是：

`项目里总共有多少个 variant。`

而是：

`这次构建到底保留了什么，运行时到底缺了什么，哪些组合我能证明永远不会发生。`

把这三件事做实，Unity 的 Shader Variant 问题才会从“玄学”重新变回工程问题。
