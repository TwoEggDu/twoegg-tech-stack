+++
title = "Unity 内置资源到底是什么：Builtin Resources、Default Resources、Always Included 和 Built-in Bundles 分别站在哪"
description = "把 Unity 里最容易混掉的几类内置资源边界拆开，讲清 unity default resources、unity_builtin_extra、Always Included Shaders、Built-in Bundles 和项目资源到底是什么关系。"
slug = "unity-builtin-resources-default-resources-always-included-built-in-bundles"
weight = 64
featured = false
tags = ["Unity", "Builtin Resources", "Shader", "AssetBundle", "BuildLayout"]
series = "Unity 资产系统与序列化"
+++

写到这里，这条系列里关于“项目自己的资源”其实已经讲得很完整了：

- 资产对象是什么
- `Scene / Prefab` 怎么恢复
- `AssetBundle / Addressables` 站在哪
- `Shader / Variant / SVC / Always Included` 分别解决什么

但项目里还有一块特别容易把人绕进去的灰区：

`Unity 内置资源到底算什么？`

很多时候，现场会冒出这些混在一起的说法：

- “这是 built-in 资源”
- “这是 default resources”
- “这是 Always Included 的 shader”
- “BuildLayout 里怎么又多了 built-in bundles”

然后后面很快就会开始歪：

- 把引擎内置 shader 当成项目资源
- 把 `Always Included` 误以为就是 built-in shader 本体
- 把 `unity default resources` 跟项目里的 `Resources` 文件夹混在一起
- 看到 built-in bundles 就以为 Addressables 又偷偷重复打了一份包

所以这篇我只做一件事：

`把 Builtin Resources、Default Resources、Always Included 和 Built-in Bundles 这几层边界拆开。`

## 先给一句总判断

如果把整件事压成一句话，我会这样说：

`Unity 的“内置资源”不是一个单一文件或单一概念，而是一组站在不同层的引擎侧内容：有些是 Player 构建里始终存在的默认资源，有些是只有被引用时才进入输出的 built-in shader 文件，有些是 Graphics Settings 明确要求强制带入的 shader 变体集合，而 Built-in Bundles 则是 Addressables 为避免这类引擎侧隐式依赖在多个 bundle 中重复出现而单独切出的交付物。`

这句话里最关键的是：

- `引擎侧内容`
- `Player 输出文件`
- `强制带入策略`
- `单独切出的交付物`

只要这四层不混，很多 built-in 相关问题都会突然顺很多。

## 一、先把“Builtin Resources”理解成一把总伞，不要把它想成一个固定文件名

这是第一层最该先钉住的点。

项目里大家经常会说：

`这是内置资源。`

但这个词本身太容易被说粗。

更稳的理解是：

`Builtin Resources` 更像一个总称，指 Unity 引擎或编辑器随产品一起提供、并在构建或运行时以某种形式进入内容世界的那批资源。`

这批东西里，至少可以再分成几类：

- 引擎预制的默认资源
- 引擎内置 shader
- 某些内置包自带的代码与资源
- 构建时因为 built-in 依赖而额外产出的交付物

所以第一层最重要的不是记住名词，而是先接受一个事实：

`“内置资源”本来就不是单层概念。`

## 二、`unity default resources`：这是 Player 里始终存在的默认资源文件，不是你项目里的 `Resources`

这一层是最容易被名字带偏的。

Unity 官方在 `Content output of a build` 文档里明确写了：

- Player 的 `Data` 目录中会有 `Resources/unity default resources`
- 这个文件包含始终进入构建的 built-in 资源，例如默认材质和字体
- 它是针对每个平台预先构建好的，并作为 Unity Editor 安装的一部分分发；构建时 Unity 会把它拷进输出

这意味着几件事。

## 1. 它不是你项目里的 `Resources` 文件夹

这是最重要的一刀。

项目里的：

- `Assets/.../Resources/...`

最后会进：

- `resources.assets`

而官方文档里说的：

- `Resources/unity default resources`

是另外一回事。

前者是你项目里显式放进 `Resources` 文件夹的内容。  
后者是 Unity 自己带着走的默认资源文件。

它们名字里都带 `Resources`，但身份完全不同。

## 2. 它更像“程序默认资源底座”

这一层里的东西，站位更接近：

- 默认材质
- 默认字体
- 某些默认对象创建时依赖的资源

也就是说，这层不是为了表达“项目想怎么分发内容”，而更像：

`Unity 为了让 Player 世界本身成立，默认就会带着走的一批底座资源。`

## 3. 这也是为什么它经常不会出现在你的项目资产目录里

很多人第一次查到这里会困惑：

`项目里又没有这个资源文件，为什么构建输出里会有？`

答案很简单：

因为它本来就不是从你的项目资产目录直接构建出来的，而是 Unity 复制进 Player 输出的一部分。

## 三、`unity_builtin_extra`：这是“被构建引用到的内置 shader 文件”，不是 Default Resources

这是第二层最容易和前一层混掉的东西。

同一份 `Content output of a build` 文档里还明确写了：

- `Resources/unity_builtin_extra` 包含被构建引用到的 built-in shaders

这句话非常关键。

它说明几件事。

## 1. 它主要在 shader 这条线上出现

跟 `unity default resources` 那种“总会存在的默认资源底座”不同，`unity_builtin_extra` 更偏：

`你这次构建真的引用到了哪些 built-in shader 相关内容。`

所以它和图形、材质、shader variant、built-in shader 路径的关系会更紧。

## 2. 它不是“所有内置资源”的总容器

这也是最容易讲歪的地方。

有些人会把：

- `unity_builtin_extra`
- `unity default resources`

都统称成“Unity 自带资源文件”，然后默认把它们当成一回事。

但更稳的理解应该是：

- `unity default resources` 更像始终存在的默认资源底座
- `unity_builtin_extra` 更像这次构建真正引用到的 built-in shader 相关内容

这两者都属于引擎侧资源，但职责不一样。

## 3. 它为什么经常在 shader 排障里变得重要

因为只要你开始排查：

- built-in shader 到底进没进构建
- 为什么某些默认 shader 路径在 Player 里存在
- 为什么某些 material 看起来没显式打包却仍然能工作

最后就很容易碰到这一层。

所以你可以把它理解成：

`built-in shader 相关内容在 Player 输出里的一个关键落点。`

## 四、`Always Included Shaders`：这不是 built-in 资源本体，而是 Graphics Settings 里的强制纳入策略

这一层又是完全不同的东西。

Unity Graphics Settings 文档对 `Always Included Shaders` 的定义很明确：

- 这是一个 shader 列表
- 对列表里的 shader，Unity 会把所有可能的 variants 都带进每次构建
- 这对运行时会使用到、但否则不会被构建带上的 shader 或 variant 有帮助，比如 `AssetBundles`、`Addressables` 或运行时 keyword 切换场景

这已经足够说明：

`Always Included` 不是一份资源文件，也不是“Unity 自带 shader 的别名”，而是一条构建策略。`

## 1. 它改变的是“包含规则”，不是 shader 的资源身份

也就是说，`Always Included` 真正回答的是：

`哪些 shader 不管场景静态分析结果如何，都必须把所有 variant 带进构建。`

它并没有把 shader 从“项目资源”变成“引擎资源”，也没有把 built-in shader 和自定义 shader 变成不同类别。

它只是说：

`对这份 shader，构建时请强制兜底。`

## 2. 所以它和 built-in shader 并不是同一个问题

项目里很容易把这两件事混起来：

- “这是 built-in shader”
- “把它加到 Always Included 就好了”

但更准确的说法是：

- built-in shader 是 shader 来源和归属的一部分
- `Always Included` 是构建时的包含策略

一个 shader 可以是 built-in，也可以被加进 `Always Included`；  
一个 shader 也可以不是 built-in，但同样被加进 `Always Included`。

它们不是互相替代的概念。

## 3. 这也是为什么 `Always Included` 解决问题时，常常像“换了边界”

前面那篇已经讲过：

- 把 shader 放进 `Always Included`，本质上是让 Player 全局内置它及其所有 variant
- 这和“让 bundle 自己负责带 shader 结果”不是一回事

所以它更像一条：

`强制把某些 shader 提升到 Player 全局兜底边界`

的策略。

## 五、Built-in Bundles：这不是引擎默认文件，而是 Addressables 构建时为了避免重复打包而切出来的交付物

这层是最容易第一次看 `BuildLayout` 时产生误会的地方。

Unity Addressables 的 Build Layout Report 文档明确写了：

- 报告里会有 `Built-in bundles`
- 它们是为 Unity 内置资产，例如默认 shaders，单独创建出来的 bundles
- 这样做是为了避免这些资产作为隐式依赖，在多个 bundle 中重复包含

这句话非常关键，因为它把 Built-in Bundles 的身份说得很清楚了。

## 1. 它不是引擎原本就自带的固定输出文件

也就是说，Built-in Bundles 不是像：

- `unity default resources`
- `unity_builtin_extra`

那样的 Player 默认输出文件。

它们更像是：

`Addressables 构建在分析到 built-in 资产作为隐式依赖时，主动切出来的单独交付物。`

## 2. 它解决的是“不要把 built-in 依赖重复烘进多个 bundle”

这件事跟上一篇讲的重复资源和依赖爆炸就直接接上了。

因为如果一个 built-in shader 或默认资产被多个 Addressables bundle 当作隐式依赖各带一份，结果通常会变成：

- 冗余
- 包体膨胀
- 更难解释的 bundle 关系

Built-in Bundles 的出现，本质上是在说：

`这些引擎侧隐式依赖，也需要被显式收口成交付单元。`

## 3. 所以看到 Built-in Bundles，不要第一反应就是“又重复打包了”

有时恰恰相反。

在 BuildLayout 里看到它们，很多时候说明的是：

`Addressables 正在试图避免 built-in 资产在多个 bundle 中隐式重复。`

真正该进一步看的，是：

- 这些 built-in bundles 里到底装了哪些资产
- 它们是不是过大
- 它们是否反映出你项目里对 built-in shader 或默认资源依赖过重

## 六、还有一个特别容易混的词：`Editor Default Resources`

虽然这篇重点不在编辑器资源，但这里必须顺手澄清一下。

Unity 的特殊文件夹里还有一个：

- `Editor Default Resources`

它的用途是：

- 让编辑器脚本通过 `EditorGUIUtility.Load` 去加载编辑器用资源

这和前面几层又完全不是一回事。

它是：

- 编辑器侧 special folder 语义

不是：

- Player 输出里的 `unity default resources`

这两个名字太像，所以非常容易让团队误以为是同一层。

但工程上它们几乎没有可互换性。

## 七、把这几层重新压成一张最稳的地图

如果只允许我用一张很粗的图去记这几层，我会这样压：

## 1. 引擎默认输出底座

- `unity default resources`

它更像 Player 默认资源底座。

## 2. 构建引用到的 built-in shader 文件

- `unity_builtin_extra`

它更像 built-in shader 相关内容在 Player 输出里的落点。

## 3. 构建策略层

- `Always Included Shaders`

它更像“哪些 shader 要被强制全量带入构建”的规则。

## 4. Addressables 交付收口层

- `Built-in Bundles`

它更像 Addressables 为避免 built-in 隐式依赖重复而切出的独立交付单元。

## 5. 编辑器特殊文件夹层

- `Editor Default Resources`

它是编辑器资源路径语义，不是 Player 输出文件。

一旦这样记，很多现象就不再会混成一句模糊的“这是不是 built-in 资源”。

## 八、项目现场最常见的四种误判

最后把最常见的误判也压一下。

## 1. 误判一：`unity default resources` 就是项目里的 `Resources`

更准确的说法应该是：

`项目里的 Resources 会进 resources.assets；unity default resources 是 Unity 默认带进 Player 的另一份内置资源文件。`

## 2. 误判二：`Always Included` 就是 built-in shader

更准确的说法应该是：

`Always Included 是包含策略，不是 shader 来源。`

## 3. 误判三：BuildLayout 里的 Built-in Bundles 说明引擎又偷偷重复打包了

更准确的说法应该是：

`Built-in Bundles 往往正是在避免 built-in 资产作为隐式依赖被重复打进多个 bundle。`

## 4. 误判四：只要是 built-in 资源，就和项目自己的资源系统没关系

更准确的说法应该是：

`一旦这些内置资源进入构建输出、参与 shader 路径、或作为隐式依赖进入 Addressables 交付，它们就会直接影响你项目自己的包体、依赖、shader 和回归体系。`

## 最后收成一句话

如果把这篇最后再压回一句话，我会这样说：

`Unity 的内置资源不是一个统一名词，而是一组站在不同层的引擎侧内容：unity default resources 是默认底座，unity_builtin_extra 是构建引用到的 built-in shader 文件，Always Included 是强制带入策略，Built-in Bundles 是 Addressables 为避免 built-in 隐式依赖重复而切出的交付物；真正稳的理解，不是把它们都叫 built-in，而是先分清它们各自站在哪一层。`
