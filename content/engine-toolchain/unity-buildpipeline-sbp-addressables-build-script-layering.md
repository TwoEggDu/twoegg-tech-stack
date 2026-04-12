---
date: "2026-04-08"
title: "Unity 的资源构建管线到底分几层：BuildPipeline、SBP、Addressables Build Script 各自站在哪"
description: "把 Unity 资源构建里最容易混掉的三层边界拆开，讲清 BuildPipeline、Scriptable Build Pipeline 和 Addressables Build Script 分别在解决什么问题，以及项目里该在哪一层做改动。"
slug: "unity-buildpipeline-sbp-addressables-build-script-layering"
weight: 65
featured: false
tags:
  - "Unity"
  - "BuildPipeline"
  - "SBP"
  - "Addressables"
  - "AssetBundle"
series: "Unity 资产系统与序列化"
primary_series: "unity-assets"
series_role: "article"
series_order: 50
---
上一篇把 `Addressables` 和 `YooAsset` 的优势层次先压清了：一个更强在官方抽象一致性，一个更强在交付工程控制力。可只要你继续往 Unity 官方这条线往下追，很快就会撞上三个名字：`BuildPipeline`、`SBP`、`Addressables Build Script`。

项目里最常见的混乱也正是从这里开始的：

- “我们这里是 `BuildPipeline` 打的”
- “其实底下已经走 `SBP` 了”
- “我们改的是 `Addressables Data Builder`”
- “那这三个到底谁才是真正的构建管线”

这些说法之所以容易越说越乱，不是因为名字难记，而是因为它们根本不站在同一层。这篇就只做一件事：把这三层边界钉住，让后面讨论 bundle、Catalog、内容更新、构建缓存和压缩代价时，不再把问题说串。

这篇先只追公开 API、公开 package 和公开文档能落住的那一层证据，不假装已经把完整 Unity 引擎源码底座一起读穿。

## 先给一句总判断

如果要把这三个词先压成一句话，我会这样说：`BuildPipeline` 更像 Unity 编辑器原生的构建入口层，`SBP` 更像 AssetBundle 的构建执行层，`Addressables Build Script` 更像建立在这上面的项目内容组织和运行时数据生成层。

所以它们最稳的关系不是：

`三选一`

而更像是：

`三层经常套在一起，但回答的是不同问题。`

## 为什么打包问题总会讨论到三四套名字

项目里大家最容易混在一起的，其实不是类名，而是三件不同的事：

1. 谁负责发起一次构建。
2. 谁负责把输入内容真正执行成一套 bundle 构建流程。
3. 谁负责定义组、地址、Catalog、本地/远端边界和内容更新语义。

这三件事如果不拆开，后面就很容易出现几种典型误判：

- 把 `Addressables` 当成“新版 BuildPipeline”
- 把 `SBP` 当成“Addressables 的别名”
- 把“换了一个 Data Builder”理解成“重写了整套 Unity 构建系统”

所以这一篇最重要的任务不是列 API，而是先把职责边界钉住。只有层次先站稳，后面的调用链才不会被说反。

## 第一层：BuildPipeline 站在编辑器原生构建入口

`BuildPipeline` 是 Unity 编辑器原生暴露出来的构建入口类。官方脚本 API 对它的定义非常直接：它允许你以代码方式构建 player 或 AssetBundle。

这句话已经把它的角色说清楚了。`BuildPipeline` 解决的是“向 Unity 编辑器发起一次构建请求”，而不是“替你定义资源交付模型”。

放到资源系统语境里，它最常见地站在两个入口：

- `BuildPipeline.BuildPlayer`
- `BuildPipeline.BuildAssetBundles`

如果你看的是 `BuildPipeline.BuildAssetBundles`，它更像一个编辑器侧的构建调用边界：给它输出目录、平台、构建选项，或者一份 `AssetBundleBuild[]` build map，它就去产出 bundle 以及相应的 `AssetBundleManifest`。

所以更准确的说法应该是：`BuildPipeline` 更像 Unity 编辑器原生提供的“构建发起和编排边界”，而不是项目资源系统自己的内容组织层。

它默认并不关心这些项目语义：

- 这个资源组为什么要独立更新
- 这个 bundle 是本地还是远端
- 地址如何映射到资源
- Catalog 怎么生成
- 内容更新怎样对齐旧版本快照

这些事都不是 `BuildPipeline` 的原生职责。你如果在项目里看到一层 Editor 菜单、批处理入口或 CI 脚本，最后去调 `BuildPipeline`，通常说明你看到的是：

`构建发起层`

而不是全部资源构建逻辑本身。

## 第二层：SBP 站在 AssetBundle 构建执行层

`SBP` 也就是 `Scriptable Build Pipeline`。Unity 手册对它的定位也很直接：它把 AssetBundle 构建管线移到 C#，让构建流程更可组合、更易扩展，并且改善增量构建与构建时间。

这段描述里的关键词其实就几个：

- `AssetBundle 构建管线`
- `可脚本化`
- `更灵活 / 更增量 / 更可组合`

也就是说，`SBP` 解决的不是“资源在项目里该怎么分组、怎么起地址”，而是：

`在你已经定义了内容边界之后，Unity 怎样更可控地执行 bundle 构建流程。`

所以我更愿意把 `SBP` 看成：

`AssetBundle 构建执行层`

这层最关心的往往是：

- 依赖分析怎么组织
- 构建任务怎么拆开
- 构建缓存怎么复用
- 增量构建为什么命中或没命中
- bundle 写出过程怎么被组合成一条可控流程

如果想在 SBP 层做定制，最值得先知道的是它的四步核心 task chain：

`GenerateBundlePacking → GenerateBundleCommands → WriteSerializedFiles → ArchiveAndCompressBundles`

分别对应：打包分组计算、生成写包指令、写出序列化文件、归档压缩成最终 bundle。这四个 `IBuildTask` 实现都在 SBP package 的 `Tasks/` 目录下，是自定义构建流程时最常见的切入点。

这也是为什么你一旦往更底层改 bundle 构建行为，很容易就会碰到 `SBP` 语境，而不只是 `BuildPipeline.BuildAssetBundles` 那层简单入口。

但反过来说，`SBP` 也不是项目资源系统的最终管理层。它默认也不替你定义：

- 地址系统
- Group 语义
- Profile
- Remote / Local 路径策略
- Catalog
- 内容更新规则

这些更靠上的项目语义，不是 `SBP` 自己的职责。

## 第三层：Addressables Build Script 站在项目内容组织和运行时数据层

如果前两层已经站住，`Addressables Build Script` 的位置就会清楚很多。

Addressables 的 `AddressableAssetSettings.BuildPlayerContent()` 官方描述很明确：运行当前激活的 player data build script，去生成运行时数据。它同时还显式建模了：

- `DataBuilders`
- `ActivePlayerDataBuilder`
- `IDataBuilder`
- `addressables_content_state.bin`

默认的 `BuildScriptPackedMode` 本身就是一类 `IDataBuilder`。这已经说明 `Addressables Build Script` 讨论的重点，不只是“把 bundle 写出来”，而是“用什么项目语义去组织输入内容，并且生成运行时需要的那套数据世界”。

再往下看 Addressables API，你会看到 `AddressableAssetsBuildContext` 这种类型。官方文档对它的说明也很关键：它是一个在 Addressables 不同部分之间、并通过 `SBP` 传递数据的上下文对象。

这句话基本已经把关系说透了：`Addressables Build Script` 不是在取代 bundle 构建执行层，而是在上面再加一层“项目内容组织、构建配置和运行时数据生成”的语义层。

所以如果把 `Addressables Build Script` 的职责压一下，它更像在回答这些问题：

- 哪些资源被纳入 Addressables 世界
- 它们分在哪些 Group
- 每个 Group 用什么打包策略
- 哪些走本地，哪些走远端
- 地址和 locator 怎么生成
- Catalog 怎么生成
- `content_state.bin` 怎样作为后续内容更新的快照证据

也就是说：

`Addressables Build Script` 关心的不是“能不能把 bundle 写出来”这么简单，而是“这一整套可定位、可更新、可发布的资源世界怎样成立”。`

## 这三层之间到底怎么串起来

这里最容易写错的一点，就是强行把三者画成一条严格串行的单链：

`BuildPipeline -> SBP -> Addressables`

这样会造成一个误解，好像它们只是简单的上下级函数调用。更稳的理解应该是：

- `BuildPipeline`：编辑器原生构建入口和编排边界
- `SBP`：AssetBundle 构建执行层
- `Addressables Build Script`：项目内容组织和运行时数据生成层

在真实项目里，它们经常会以这样的关系套在一起：

```text
CI / Editor 菜单 / 批处理入口
        ↓
Addressables Settings / Profile / Group / ActivePlayerDataBuilder
        ↓
Addressables Build Script 组织内容并生成运行时数据定义
        ↓
SBP 承接 bundle 构建执行、依赖分析、缓存与写包流程
        ↓
bundle / catalog / content_state / build layout 等产物
        ↓
需要整包时，再进入 BuildPipeline.BuildPlayer
```

注意这里的关键不是“谁一定先调用谁”，而是“谁在回答哪一层问题”。从职责分层看，它们不是三选一；从工程流程看，它们又经常会在一次完整构建里前后接起来。这两件事要同时成立，文章才不会把层次写歪。

## 项目判断：讨论打包问题时，应该先怀疑哪一层

只讲结构不讲落点，这篇还是会漂。下面把最常见的几类问题先压到对应层。

| 你碰到的问题 | 先怀疑哪一层 | 为什么 |
|---|---|---|
| batchmode 下怎么发起构建、平台怎么切、先打内容还是先打 player | `BuildPipeline` | 这是编辑器构建入口和编排问题 |
| `BuildAssetBundles` 输出目录、平台选项、构建命令怎么组织 | `BuildPipeline` | 仍然是调用边界和构建发起层 |
| 增量构建为什么没命中、缓存为什么没复用、bundle 执行为什么特别慢 | `SBP` | 这些已经进入 bundle 构建执行与缓存层 |
| 重复依赖、构建任务拆分、写包流程和底层构建产物异常 | `SBP` | 这不是 Group 语义，而是执行层行为 |
| Group 怎么切、地址怎么生成、哪些资源本地哪些远端 | `Addressables Build Script` | 这是项目内容组织与发布语义层 |
| Catalog、content state、内容更新为什么会把某些资源重新打包 | `Addressables Build Script` | 这是运行时数据和更新语义层，不只是写包层 |
| 整包能出，但 Addressables 内容世界不对 | 先看 `Addressables Build Script`，再下钻 `SBP` | 先判断是项目语义错了，还是底下执行层出了问题 |

更短的判断法其实就一句：先问你在改“构建怎么被发起”，还是“bundle 怎么被执行出来”，还是“项目资源世界怎样被定义”。

## 最常见的三个误判

### 1. 误判一：用了 Addressables，就等于不用理解底下的 bundle 构建层

更准确的说法应该是：`Addressables` 把很多项目语义显式化了，但 bundle 构建、依赖、缓存、Shader、平台和交付边界这些问题并没有消失。它只是把复杂度收编了，不是把复杂度删除了。

### 2. 误判二：SBP 就是 Addressables

更准确的说法应该是：`SBP` 更像可脚本化的 AssetBundle 构建执行层；`Addressables` 只是经常在它之上建立内容组织和运行时数据层。两者关系很紧，但不是同一个概念。

### 3. 误判三：我们写了一个自定义构建脚本，就等于重写了整套 Unity 构建管线

更准确的说法应该是：先问清你改的是入口编排、bundle 执行流程，还是项目内容组织层。很多团队其实只是包了一层 Editor 菜单、CI 调度或替换了一个 `Data Builder`，这和“重写完整资源构建管线”不是一个量级。

## 最后只压一句

`BuildPipeline` 负责发起和编排构建，`SBP` 负责执行 AssetBundle 构建流程，`Addressables Build Script` 负责把项目资源组织、运行时数据和发布语义挂到这条构建链上。把这三层拆开以后，后面再讨论 bundle、Catalog、内容更新、构建缓存和压缩代价，才不会把问题说成同一种。`

如果你下一步想继续追“真正把资源编成 AssetBundle 时，中间到底发生了什么”，接着读 [Unity 怎么把资源编成 AssetBundle：依赖、序列化、Manifest、压缩到底发生了什么]({{< relref "engine-toolchain/unity-how-assets-become-assetbundles-dependencies-manifest-compression.md" >}})。

如果你想把这篇放回前一篇总论的判断里看，回去读 [Addressables 和 YooAsset 到底谁强：你选的不是框架，是资源交付问题的主战场]({{< relref "engine-toolchain/unity-addressables-yooasset-main-battlefield-of-resource-delivery.md" >}})。