+++
title = "Unity 的资源构建管线到底分几层：BuildPipeline、SBP、Addressables Build Script 各自站在哪"
description = "把 Unity 资源构建里最容易混掉的三层边界拆开，讲清 BuildPipeline、Scriptable Build Pipeline 和 Addressables Build Script 分别在解决什么问题，以及项目里该在哪一层做改动。"
slug = "unity-buildpipeline-sbp-addressables-build-script-layering"
weight = 65
featured = false
tags = ["Unity", "BuildPipeline", "SBP", "Addressables", "AssetBundle"]
series = "Unity 资产系统与序列化"
+++

写到这里，这条系列已经把：

- 资产系统本体
- `AssetBundle / Addressables`
- 构建产物、缓存和治理
- Shader、案例和工程实践

都铺得比较完整了。

但项目里一旦真的开始做资源构建，很快又会冒出另一层混乱：

- “我们这里是 `BuildPipeline` 打的”
- “其实底下已经是 `SBP` 了”
- “我们改的是 `Addressables Build Script`”
- “那这三个到底谁才是真正的构建管线”

这些说法之所以容易越说越乱，不是因为名字难记，而是因为它们根本不站在同一层。

## 先给一句总判断

如果要把这三个词先压成一句话，我会这样说：

`BuildPipeline` 更像 Unity 编辑器原生的构建入口层，`SBP` 更像 AssetBundle 的构建执行层，`Addressables Build Script` 更像建立在这上面的项目资源组织和运行时数据生成层。

所以它们最稳的关系不是：

`三选一`

而更像是：

`三层经常套在一起，但回答的是不同问题。`

## 一、为什么这个问题总会被说乱

先把最常见的混淆源压一下。

项目里大家经常会把“谁发起构建”、“谁执行 bundle 构建”、“谁决定内容组织和运行时数据”混成同一个问题。

但这其实是三件不同的事：

1. `谁负责启动一次构建`
2. `谁负责把输入内容真正编成 bundle`
3. `谁负责定义组、地址、Catalog、远端/本地边界和内容更新模型`

如果这三件事不拆开，后面就很容易出现这些误判：

- 把 `Addressables` 当成“新版 BuildPipeline”
- 把 `SBP` 当成“Addressables 的别名”
- 把“换了一个 Addressables Data Builder”理解成“我们重写了整套 Unity 构建系统”

所以这一篇最重要的任务，不是列 API，而是先把职责边界钉住。

## 二、第一层：BuildPipeline 站在编辑器原生构建入口

`BuildPipeline` 是 Unity 编辑器原生暴露出来的构建入口类。

Unity 的脚本 API里直接把它定义成：

`Lets you programmatically build players or AssetBundles.`

这句话其实已经把它的角色说得很清楚了：

`BuildPipeline` 解决的是“向 Unity 编辑器发起一次构建请求”，而不是“替你定义资源交付模型”。

放到资源系统语境里，它常见地站在两个入口：

- `BuildPipeline.BuildPlayer`
- `BuildPipeline.BuildAssetBundles`

其中 `BuildPipeline.BuildAssetBundles` 的职责也很直接：

- 你给它输出目录、目标平台、构建选项
- 或者给它一份 `AssetBundleBuild[]` build map
- 它返回一个 `AssetBundleManifest`

所以更准确的说法应该是：

`BuildPipeline` 更像 Unity 编辑器原生提供的“构建入口和编排边界”，而不是项目资源系统自己的内容组织层。`

它很重要，但它默认并不关心这些项目语义：

- 这个资源组为什么要独立更新
- 这个 bundle 是本地还是远端
- 地址如何映射到资源
- Catalog 怎么生成
- Content Update 应该怎么接旧版本快照

这些事都不是 `BuildPipeline` 的原生职责。

所以你如果看到项目里有一层自定义 Editor 菜单、批处理入口、CI 脚本，最后去调：

- `BuildPipeline.BuildPlayer`
- `BuildPipeline.BuildAssetBundles`

那通常说明你看到的是：

`构建发起层`

而不是全部资源构建逻辑本身。

## 三、第二层：SBP 站在 AssetBundle 构建执行层

`SBP` 也就是 `Scriptable Build Pipeline`，Unity 官方对它的描述非常直接：

`Scriptable Build Pipeline 将资源包构建管线移至 C#。您可以使用预定义的构建流程，或使用分散的 API 来创建自己的构建流程。此系统可以缩短构建时间，修复增量构建，并提供更大的灵活性。`

这句话里的关键词其实就三个：

- `AssetBundle 构建管线`
- `移至 C#`
- `更灵活 / 更增量 / 更可组合`

也就是说，`SBP` 解决的不是“资源在项目里该怎么分组、怎么起地址”，而是：

`在你已经定义了内容边界之后，Unity 怎样把这些输入更可控地执行成一套 bundle 构建流程。`

所以我更愿意把 `SBP` 看成：

`AssetBundle 构建执行层`

这层最关心的往往是：

- 依赖分析怎么组织
- bundle 输入怎么展开
- 哪些任务可以拆分
- 缓存怎么复用
- 增量构建为什么能快一些
- bundle 写出过程怎么更可组合

这也是为什么你一旦往更底层改 bundle 构建行为，很容易就会碰到 `SBP` 语境，而不只是 `BuildPipeline.BuildAssetBundles` 那层简单入口。

但反过来说，`SBP` 也不是项目资源系统的最终管理层。

它默认也不替你定义：

- 地址系统
- Group 语义
- Profile
- Remote / Local 路径策略
- Catalog
- Content Update 规则

这些更靠上的项目语义，不是 `SBP` 自己的职责。

## 四、第三层：Addressables Build Script 站在项目内容组织和运行时数据层

如果前两层已经站住，`Addressables Build Script` 的位置就会更清楚。

Addressables 文档里，`AddressableAssetSettings.BuildPlayerContent` 的描述是：

`Runs the active player data build script to create runtime data.`

同时它明确说，构建会考虑：

- `AddressableAssetSettingsDefaultObject`
- `ActivePlayerDataBuilder`
- `addressables_content_state.bin`

而且 Addressables 还把“激活哪个构建脚本”这件事显式建模成了：

- `DataBuilders`
- `ActivePlayerDataBuilder`
- `IDataBuilder`

默认的 `BuildScriptPackedMode` 甚至直接就是一个 `BuildScriptBase, IDataBuilder`，并且它的 API 里会处理：

- group
- schema
- bundle packing
- runtime data

Addressables API 里还有一个更关键的信号：

`AddressableAssetsBuildContext` 被描述为“在 Addressables 代码不同部分之间，通过 SBP 传递数据的上下文对象”。`

这其实已经把关系说得很明白了：

`Addressables Build Script` 不是在取代底下的 bundle 构建执行层，而是在上面加了一层“项目内容组织、构建配置和运行时数据生成”的语义层。

所以如果把 `Addressables Build Script` 的职责压一下，它更像在回答这些问题：

- 哪些资源被纳入 Addressables 世界
- 它们分在哪些 group
- 每个 group 采用什么打包策略
- 哪些走本地，哪些走远端
- 地址和 locator 怎么生成
- Catalog 怎么生成
- `content_state.bin` 怎么留给后续内容更新

也就是说：

`Addressables Build Script` 关心的不是“能不能把 bundle 写出来”这么简单，而是“这一整套可定位、可更新、可发布的资源世界怎样成立”。`

## 五、这三层不是一条假单链，而是三个不同边界

这里最容易写错的一点，就是强行把三者画成：

`BuildPipeline -> SBP -> Addressables`

这样会造成一个误解，好像它们只是严格串行的上下级函数调用。

更稳的理解应该是：

- `BuildPipeline`：编辑器原生构建入口和调度边界
- `SBP`：AssetBundle 构建执行层
- `Addressables Build Script`：项目内容组织和运行时数据生成层

在项目里，它们经常这样套在一起：

1. 你自己的 CI / 菜单命令 / Editor 脚本决定何时发起构建
2. Addressables 根据 `Settings / Profile / Group / ActivePlayerDataBuilder` 组织内容
3. 默认打包脚本把组和打包策略翻译成 bundle 构建输入
4. 底下的 bundle 构建执行过程由 `SBP` 这类层来承接
5. 产出 bundle、Catalog、运行时数据、内容状态文件
6. 如果要出整包玩家端，再走 `BuildPipeline.BuildPlayer`

所以从“职责分层”看，它们不是三选一。

从“工程调用”看，它们又确实会在一次完整构建里前后接起来。

这两件事要同时成立，文章才不会把层次写歪。

## 六、项目里该在哪一层改东西

如果只讲结构不讲落点，这篇还是不够实用。下面我把最常见的改动需求压到对应层。

## 1. 你要控制构建入口、批处理流程、Player 构建顺序

先看：

`BuildPipeline` 这一层

典型问题是：

- 先打 Addressables 内容还是先打 Player
- CI 命令怎么串
- batchmode 下怎么切目标平台
- 构建成功与失败怎么汇总

这些更像：

`构建编排层`

## 2. 你要改 AssetBundle 构建执行方式、缓存复用或更细的执行流程

先看：

`SBP` 这一层

典型问题是：

- 想做更细的 bundle 构建任务切分
- 想研究增量构建为什么没命中
- 想看更底层的构建缓存和写包流程
- 想在 bundle 执行层插入更细颗粒的处理

这些已经不是单纯改 group 配置能解决的事了。

## 3. 你要改组、地址、Catalog、远端/本地、内容更新边界

先看：

`Addressables Build Script / Data Builder` 这一层

典型问题是：

- group 怎么切
- bundle naming 和 packing policy 怎么从项目语义出发定义
- catalog 怎样跟发布快照对齐
- 内容更新为什么会把某些资源打回新 bundle
- 本地首包和远端热更边界怎么落

这些问题，本质上已经站在：

`项目资源交付模型`

这一层了。

## 七、最常见的三个误判

最后把最容易误导项目决策的三种说法也压一下。

## 1. 误判一：用了 Addressables，就等于不用理解底下的 bundle 构建层

更准确的说法应该是：

`Addressables 帮你把很多项目语义显式化了，但 bundle 构建、依赖、缓存、Shader、平台和交付边界这些问题并没有消失。`

它只是把复杂度收编了，不是把复杂度删除了。

## 2. 误判二：SBP 就是 Addressables

更准确的说法应该是：

`SBP 更像可脚本化的 AssetBundle 构建执行层；Addressables 只是经常在它之上建立内容组织和运行时数据层。`

两者关系很紧，但不是同一个概念。

## 3. 误判三：我们写了一个自定义构建脚本，就等于重写了整套 Unity 构建管线

更准确的说法应该是：

`先问清楚你改的是入口编排、bundle 执行流程，还是项目内容组织层。`

很多团队其实只是：

- 在 `BuildPipeline` 外面包了一层菜单或 batchmode 入口
- 或者换了一个 `Addressables Data Builder`

这和“重写完整资源构建管线”不是一个量级。

## 最后收成一句话

如果把这篇最后再压回一句话，我会这样说：

`Unity 资源构建里，BuildPipeline、SBP 和 Addressables Build Script 最容易被混成一层；但更稳的理解是：BuildPipeline 负责发起和编排构建，SBP 负责执行 AssetBundle 构建流程，Addressables Build Script 负责把项目资源组织、运行时数据和发布语义挂到这条构建链上。`
