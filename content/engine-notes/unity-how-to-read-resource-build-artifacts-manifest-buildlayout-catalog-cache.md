+++
title = "怎么看 Unity 资源构建产物：Manifest、BuildLayout、Catalog 和缓存目录到底在告诉你什么"
description = "把 Unity 资源构建后的 Manifest、BuildLayout、Catalog 和缓存目录放回同一张观测地图，讲清它们分别在记录什么，以及遇到重复资源、依赖爆炸、缓存失效时该先看哪一份证据。"
slug = "unity-how-to-read-resource-build-artifacts-manifest-buildlayout-catalog-cache"
weight = 60
featured = false
tags = ["Unity", "AssetBundle", "Addressables", "Manifest", "BuildLayout"]
series = "Unity 资产系统与序列化"
+++

写到这里，这条系列已经把资源系统、序列化、`AssetBundle`、`Addressables`、`Shader`、工程治理和案例都铺得差不多了。

但项目里真正开始排障时，很多人卡住的并不是：

`我完全不懂原理。`

而是：

`构建完以后手上有一堆 manifest、catalog、bundle 文件和缓存目录，我到底先看哪个？`

这也是 Unity 资源系统特别容易让人“知道很多概念，但下手还是慢”的地方。

因为你面对的不是一个单一日志，而是四类完全不同的产物：

- `Manifest`
- `BuildLayout`
- `Catalog`
- `缓存目录`

它们都和资源交付有关，但回答的问题并不一样。

如果这四类产物不先分层，很容易出现两种低效排障：

- 明明在查重复资源，却一直盯着缓存目录
- 明明在查远端内容更新，却一直翻 `.manifest`

所以这篇我就只做一件事：

`把这四类构建产物放回同一张观测地图里。`

## 先给一句总判断

如果把整件事压成一句话，我会这样说：

`Manifest、BuildLayout、Catalog 和缓存目录，分别站在四个不同观察层：Manifest 更像 AssetBundle 构建和依赖记录，BuildLayout 更像面向人类和分析工具的构建剖面，Catalog 更像运行时定位和内容映射表，而缓存目录则只是当前设备实际持有的本地副本。`

这句话里最关键的是：

- `构建记录`
- `构建剖面`
- `运行时映射`
- `本地副本`

只要先把这四层分开，很多问题第一刀就会快很多。

## 一、先把四类产物分层：它们不是同一种“清单”

项目里大家很容易把它们统称成“资源清单”。

这个叫法不算完全错，但太粗。

更稳的分法是：

## 1. Manifest：AssetBundle 构建记录

它更接近在记录：

- 这个 bundle 里明确包含了什么
- 它依赖哪些其他 bundle
- 某些与 CRC、hash、type tree 相关的信息

它是 `BuildPipeline.BuildAssetBundles` 这条原生构建链的重要副产物。

## 2. BuildLayout：构建剖面和解释材料

它更接近在回答：

- 每个 bundle 到底有多大
- 明示资产和隐式依赖分别占了多少
- 某个 group 是怎么被打成 bundle 的
- 哪些 built-in 资源被单独打进了 built-in bundles

它比 Manifest 更适合给人类和分析器读。

## 3. Catalog：运行时定位和内容映射

它更接近在回答：

- 某个 address 或 key 最终要定位到哪里
- 远端和本地内容如何被统一映射
- 当前运行时应该使用哪一份内容索引

它不是在讲 bundle 物理结构，而是在讲：

`运行时怎样找到内容。`

## 4. 缓存目录：设备此刻真的持有什么

它更接近在回答：

- 当前设备本地有哪些副本
- 哪些远端内容已经被拉下来了
- 哪些缓存副本可能还在被复用

它是现场结果，不是上游设计本身。

这四层的关系可以粗略压成：

`Manifest / BuildLayout` 更偏构建世界，`Catalog` 更偏运行时映射世界，缓存目录更偏设备现场世界。`

## 二、Manifest：适合看“依赖关系和构建输入”，不适合拿来替代运行时真相

如果你走的是原生 `BuildPipeline.BuildAssetBundles` 路线，Unity 会在输出目录里生成：

- 每个 AssetBundle 对应一个 `.manifest`
- 一个根 `.manifest`
- 以及一个 manifest bundle

Unity 官方文档写得很清楚：

- 每个 bundle 的 `.manifest` 可以直接用文本编辑器打开
- 里面会有 CRC、哈希、类类型、明确包含的资源、依赖 bundle 等信息
- 根 `.manifest` 会记录生成出来的所有 bundle 及其依赖关系
- manifest bundle 里则包含 `AssetBundleManifest` 对象，运行时可以用它来解析依赖

## 1. Manifest 最适合回答什么

它最适合回答的是：

- 这个 bundle 明确包含了哪些资产
- 它显式依赖了哪些其他 bundle
- 这次构建出来的 bundle 图长什么样
- 某个 bundle 的 CRC / Hash 有没有变化

也就是说，Manifest 更像：

`AssetBundle 构建侧的依赖记录。`

## 2. 它最不适合回答什么

它不太适合直接回答这些问题：

- 运行时最终按 address 找到了什么
- Addressables 当前到底用了哪份远端映射
- 当前设备本地到底缓存了哪份副本
- 为什么玩家第二次进入还是像第一次

因为这些问题已经跨出原生 bundle 构建层，进入运行时映射和本地设备现场了。

## 3. Addressables 场景下要特别注意一件事

Unity 官方也明确写了：

如果用的是 Addressables 去构建 AssetBundles，那么生成的是同样格式的 AssetBundle 文件，但：

- 不会生成 `.manifest` 文件
- 也不会生成 manifest bundle

这点非常重要。

因为很多人习惯了原生 bundle 路线，会下意识去找 `.manifest`，然后误以为：

`是不是没构建出来。`

其实更准确的说法是：

`Addressables 把运行时依赖与定位信息挪到 catalog 这条线去了。`

## 三、BuildLayout：最适合查“为什么会大、为什么会重复、为什么依赖会爆”

如果前面说 Manifest 更像构建记录，那 BuildLayout 更像：

`构建剖面报告。`

这也是我最建议团队认真打开来看的一份产物。

Unity 官方的 Addressables Build Layout Report 文档列得很清楚，它可以告诉你：

- AssetBundle 描述
- 各个资产和 AssetBundle 的大小
- 作为隐式依赖被打进 bundle 的非 Addressable 资产
- bundle 依赖关系
- AssetBundle 内各序列化文件的信息
- built-in bundles 的信息

而且它会生成在：

`Library/com.unity.addressables/buildlayout`

启用它会增加构建时间，但换来的价值通常很高。

## 1. BuildLayout 最适合回答什么

它特别适合回答下面这类问题：

- 为什么这个 group 最后打成了这么多个 bundle
- 为什么某个 bundle 这么大
- 哪些资源是显式放进去的，哪些是隐式依赖带进去的
- 重复资源是不是在多个 bundle 里各带了一份
- built-in 资源有没有被单独打包出来

这正是重复资源、依赖爆炸、包体异常最需要的证据。

## 2. 你应该重点看哪几段

如果是第一次看 BuildLayout，我更建议先盯这几块：

### 1. Summary

先看总 bundle 数、总大小、总 MonoScript 大小这些全局指标。

### 2. Group

看某个 group 到底被打成了多少 bundle、用了什么 schema。

### 3. AssetBundle

这是最值钱的一层：

- 文件名
- 大小
- 压缩形式
- bundle dependencies
- explicit assets
- files

### 4. Built-in Bundles

这一块特别容易被忽略，但很值钱。

官方文档明确说，这里会列出像默认 shader 这种 built-in 资产形成的单独 bundle。  
如果你在排查 built-in 资源重复、默认 shader、隐式依赖，这一段很重要。

## 3. BuildLayout 不适合干什么

它不适合直接告诉你：

- 当前远端 catalog 选中了哪份内容
- 当前设备究竟用了哪份缓存副本

所以它最适合的定位是：

`构建解释报告。`

不是运行时真相本身。

## 四、Catalog：最适合看“运行时怎么找内容”，不适合拿它代替包体分析

到了 Addressables，这条线就必须单独看。

Unity 官方文档对 content catalog 的定义很直接：

- 它是 Addressables 用来根据系统提供的 key 查找资产实际位置的数据存储
- Addressables 会为所有 Addressable 资产构建一个单独 catalog
- Player build 时，本地 catalog 会放进 `StreamingAssets`
- 如果启用了 remote catalog，则运行时会检查 hash，决定是否下载并缓存远端 catalog 来替代本地 catalog

这就把它和 Manifest、BuildLayout 的位置彻底分开了。

## 1. Catalog 最适合回答什么

它最适合回答的是：

- 一个 address / key 最终会被定位到哪里
- 当前运行时到底在用本地 catalog，还是远端 catalog
- 内容更新以后，运行时为什么会切到另一份映射
- 多个项目或多个 catalog 是怎么被 additively 加载进来的

也就是说，Catalog 更像：

`运行时内容世界的索引和入口表。`

## 2. 它最不适合回答什么

它不适合单独回答：

- 某个 bundle 为什么这么大
- 哪些隐式依赖被打重复了
- 为什么 group 切法会导致包爆炸

因为这些都更属于 BuildLayout 的职责。

## 3. 看 Catalog 时最值得盯住的不是“有这个文件”，而是“有没有 hash 配套”

官方文档和 API 都强调了同一件事：

- remote catalog 会有对应的 hash 文件
- Addressables 会用这个 hash 判断缓存的 catalog 要不要更新
- 如果 hash 没变，运行时就会继续用缓存的 catalog，而不是重新下载 JSON

这意味着很多“内容明明更新了，客户端还是旧的”问题，第一刀不该先看 bundle 文件，而更该先问：

`当前 catalog 和它的 hash 到底是不是那份目标映射。`

## 五、缓存目录：它最像“现场遗留”，不是“设计意图”

缓存目录是很多人最爱翻、也最容易翻偏的一层。

因为它最直观：

- 本地是不是有文件
- 文件名像不像新版本
- 目录里有没有多一份副本

但这层其实最容易让人误解。

## 1. 缓存目录最适合回答什么

它最适合回答的是：

- 当前设备到底已经持有哪些副本
- 某个内容是不是曾经被下载过
- 某个缓存副本是否可能还在被复用

它是设备现场证据，不是逻辑设计文件。

## 2. 缓存目录最不适合回答什么

它不适合单独回答：

- 这份副本是不是当前目标内容
- 当前运行时为什么选择了它
- 这份副本为什么还没被清掉

因为这些问题都还要回到：

- catalog 映射
- 版本 / hash 语义
- 缓存策略

也就是说，缓存目录只能回答：

`现在本地有什么。`

但它不能单独回答：

`为什么应该用它。`

## 3. 你在缓存目录里看到“有文件”，并不等于运行时这次就一定能快

这一点特别重要。

因为即便本地已经有缓存副本：

- 当前映射也可能已经切到另一份内容
- 当前副本也可能不是那份目标 hash
- 就算副本复用了，第一次进入内容世界的准备链也仍然可能要重新结账

这就是为什么“缓存命中了，但玩家还是觉得像第一次进”是完全可能的。

## 六、遇到问题时，到底该先看哪一份

如果把这四类产物重新收成一个实战入口，我更建议这样切。

## 1. 你在查“为什么这个包这么大 / 为什么重复资源这么多”

先看：

- `BuildLayout`

再辅以：

- 原生 `.manifest` 或根 manifest 看依赖关系

不要先翻缓存目录。

## 2. 你在查“这个 bundle 到底依赖谁 / 这次构建依赖图有没有变”

先看：

- 原生 `Manifest`

如果是 Addressables 路线，再回头看：

- `BuildLayout` 里的 bundle dependencies

## 3. 你在查“为什么远端内容更新了，客户端还是旧的”

先看：

- `Catalog`
- `Catalog hash`

再对照：

- 当前缓存副本

不要先拿 BuildLayout 猜运行时为什么还在用旧内容。

## 4. 你在查“为什么第二次进还是像第一次”

先分两刀：

### 1. 先看 Catalog 和缓存副本

确认这次到底是不是复用了正确内容。

### 2. 再看准备链

因为哪怕内容复用成功了，首次命中、解压、依赖展开、对象恢复和实例化仍然可能继续结账。

## 5. 你在查“Addressables 到底给我打了什么”

先看：

- `BuildLayout`
- `Catalog`

不要先去找 `.manifest`，因为官方明确说 Addressables 构建不会给你这些文件。

## 七、把四者重新压成一张观测地图

如果只允许我用一张最粗的图去记它们，我会这样压：

- `Manifest`：构建出来的 bundle 关系和依赖记录
- `BuildLayout`：构建剖面、大小、隐式依赖、重复和 built-in 证据
- `Catalog`：运行时内容定位和更新映射
- `缓存目录`：设备现场此刻真的持有的副本

它们连起来，才是一条完整观测链：

`构建时打成了什么 -> 运行时该怎么找 -> 设备本地现在拿着什么`

这三段缺哪一段，排障都会慢很多。

## 最后收成一句话

如果把这篇最后再压回一句话，我会这样说：

`Manifest、BuildLayout、Catalog 和缓存目录，不是四份等价“清单”，而是四个不同观察层的证据：Manifest 讲 bundle 构建关系，BuildLayout 讲构建剖面和隐式成本，Catalog 讲运行时内容映射，缓存目录讲设备现场副本；真正高效的排障，不是翻得越多越好，而是先翻对那一层。`
