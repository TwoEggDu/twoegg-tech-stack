---
date: "2026-03-23"
title: "AssetBundle 文件内部结构：Header、Block、Directory 和 SerializedFile 是怎么组织的"
description: "从 Unity 官方的 Archive 与 AssetBundle file format reference 出发，按 Header、Block、Directory 和 SerializedFile 四层解释 AssetBundle 内部结构，以及它为什么会影响加载、压缩和随机访问。"
slug: "unity-assetbundle-file-internal-structure-header-block-directory-serializedfile"
weight: 58
featured: false
tags:
  - "Unity"
  - "AssetBundle"
  - "Serialization"
  - "Archive"
  - "Compression"
series: "Unity 资产系统与序列化"
---
写到这里，`AssetBundle` 这条线已经把构建、运行时加载、复杂度、性能、治理和 `Shader` 边界都铺得差不多了。

但如果还想再往下一层看，就会碰到一个更底层的问题：

`一个 AssetBundle 文件自己内部，到底是怎么组织的？`

这个问题一旦不拆开，很多现象就会一直像黑盒：

- 为什么它不能简单理解成一个压缩包
- 为什么 `LZMA` 和 `LZ4` 会直接影响随机访问和首载成本
- 为什么 bundle 里除了主文件，还会有 `.resS`、`.resource`、`sharedasset`
- 为什么真正恢复对象的关键，最后又会落到 `SerializedFile`

不过这篇要先划清边界。

我不会把它写成“Unity 各版本二进制字段大全”，也不会假装每个版本、每个细节都完全稳定。  
这篇更想做的是：

`站在 Unity 官方当前对 Archive 和 AssetBundle file format 的描述上，给出一份足够稳的结构地图。`

其中 `Header / Block / Directory / SerializedFile` 这四个词里：

- `Header`
- `Block`
- `SerializedFile`

都能直接落在 Unity 官方公开描述上。

而这里说的 `Directory`，更准确地说，是：

`Archive 内部那层“虚拟文件表 / 文件目录”语义。`

Unity 官方近年的文档更常说的是：

- 这是一个 `Archive`
- 里面有一组 `virtual files`
- 它会被挂进 Unity 的 `VFS`

所以这篇里用 `Directory`，主要是为了帮助理解那层“这些内部文件分别是谁、在什么位置、怎么被找到”的结构职责，而不是在宣称某个不可变字段名。

## 先给一句总判断

如果把整件事压成一句话，我会这样描述：

`AssetBundle 更像一个被挂进 Unity 虚拟文件系统的 Archive：最外层先用 Header 描述这份归档和压缩方式，中间用 Block 组织内容区的压缩与读取，里面再靠一层虚拟文件目录把 CAB、sharedasset、.resS、.resource 这些内部文件组织起来，而真正承载 Unity 对象图的核心文件则是 SerializedFile。`

这句话里最关键的是：

- `AssetBundle 首先是 Archive`
- `Archive 里还有内部文件`
- `真正恢复对象靠的是 SerializedFile`

只要这三点站住，后面很多表面现象就会突然变得顺很多。

## 一、先从外到内看：AssetBundle 不是一个文件，而是一层套一层

很多人第一次看 bundle，会下意识把它想成：

`一批资源被压成了一个包。`

这个理解不算错，但还太粗。

更稳的理解是：

1. 最外层是一份 `Archive`
2. Archive 里有自己的 `Header`
3. Header 后面是内容区，内容区可能被分块压缩
4. 内容区里不是直接摆“资源对象”，而是一组内部文件
5. 其中最关键的内部文件是 `SerializedFile`

所以它更像：

`一个能被 Unity 挂进虚拟文件系统的内容容器。`

这也是为什么 Unity 官方会直接把 AssetBundle 归到 `Archive file format` 上来讲，而不是只把它当“压缩过的资源包”。

## 二、Header：先说明“这是什么包，以及后面怎么读”

最外层先看到的，就是 `Header`。

Unity 官方当前对 AssetBundle 压缩格式的描述比较稳：  
一个 AssetBundle 文件包含：

- 一个很小的 `header data structure`
- 一个 `content section`

其中 header 永远不压缩，content section 可以压缩。

这层意味着什么，其实很好理解。

## 1. Header 的职责不是存资源对象，而是告诉加载器“后面那坨内容该怎么理解”

它更接近在解决这些问题：

- 这是不是 Unity 的 archive / AssetBundle
- 后面的内容区有没有压缩
- 用的是什么压缩方式
- 某些与版本、兼容性有关的元信息是什么

Unity 也明确提到，默认情况下，构建 bundle 的 Unity Editor 版本信息会写进 AssetBundle header；如果不想让这个信息影响增量构建和下载，可以用 `BuildAssetBundleOptions.AssetBundleStripUnityVersion` 把它去掉。

这说明 Header 站的位置很像：

`加载入口和兼容入口。`

## 2. Header 小而稳定，因为它必须先于内容区被读取

这一点很重要。

因为如果加载器连“后面内容区是什么压缩形式、怎么读”都还不知道，它根本就没法往里走。

所以从结构职责上说，Header 必须：

- 足够靠前
- 足够小
- 足够稳定

它不是“资源主体”，但它决定了你能不能进入资源主体。

## 三、Block：内容区为什么不是一整块原样数据

Header 之后，就是 content section。

而 content section 之所以会直接影响加载和性能，是因为它不是简单的“后半截字节流”，而是会受到压缩组织方式影响。

## 1. Unity 官方的核心描述是：内容区可以被整体压缩，也可以被分块压缩

官方文档当前给出的三种形式是：

- `LZMA`：整段内容区作为单一流压缩
- `LZ4`：内容区按块压缩
- `Uncompressed`

其中 `LZ4` 的关键点，官方写得很具体：

- 写 bundle 时，Unity 会把内容区按 `128 KB` 一块分别压缩
- 这样加载时可以只解压访问到目标对象所需的那几个块

这就是这篇里说 `Block` 的来源。

更准确地说：

`Block` 不是“资源逻辑块”，而是内容区在归档层、压缩层上的读取块。`

## 2. Block 的职责，是在“压缩率”和“随机访问”之间折中

这也是为什么 `LZMA` 和 `LZ4` 的体验差异这么大。

### 1. LZMA 更像“整段内容一起压”

它的优点是：

- 文件更小

但代价是：

- 想读一个对象，也得先把整段内容区解开

所以它更适合：

- 下载分发
- 一次性整体消费

### 2. LZ4 更像“内容区切成很多独立块”

它的优点是：

- 想读某个对象时，只需要解压命中的那些块
- 随机访问和缓存体验更好

代价则是：

- 文件通常比 LZMA 更大

所以从工程上看，Block 这层不是文件格式小细节，而是：

`为什么这个 bundle 是“首载卡一下”还是“随机读起来更顺”的关键原因之一。`

## 3. Block 还解释了“为什么 AssetBundle 不能简单类比 zip”

因为 Unity 真正在乎的不是“把文件塞进去压起来”，而是：

- 能不能快速挂进 VFS
- 能不能按需读内部文件
- 能不能在运行时少解压不需要的内容

所以它做的是一套更偏运行时友好的 archive 结构，而不只是传统离线压缩包。

## 四、Directory：内容区里到底有哪些内部文件，靠什么找到

到这里，就该讲这篇里最容易被误解的那个词了：

`Directory`

这里说的不是操作系统文件夹，而是：

`Archive 内部那层“有哪些虚拟文件、它们各自在哪里”的目录语义。`

Unity 官方现在的说法是：

- AssetBundle 是一个 archive file
- 它 contains multiple files
- 这些文件会被 mount 到 Unity 的 VFS 里

从结构职责上看，这就意味着 archive 里面一定要有一层机制，能回答：

- 里面有哪些文件
- 每个文件叫什么
- 每个文件从哪开始、占多大

这篇把这层统一叫做 `Directory`，就是在指这件事。

## 1. 没有这层目录语义，Unity 就没法把 bundle 当成 VFS 里的文件系统来挂

官方不仅把它叫 `ArchiveFileSystem`，还提供了 `ArchiveFileInterface` 让你低层挂载 archive。

这已经足够说明一件事：

`Unity 不是把 bundle 当作一串匿名字节看，而是把它当作一个内部还包含文件的归档文件系统看。`

只要是文件系统语义，就一定离不开“目录 / 文件表”。

## 2. 这层目录语义负责把内部文件组织起来

一份典型 AssetBundle 内部，官方目前明确会看到这些虚拟文件：

- 主 `SerializedFile`
- `.resource`
- `.resS`
- 场景 bundle 里的 `sharedasset`

例如：

- 资产 bundle 里的主序列化文件，通常叫 `CAB-` 加上 AssetBundle 名称的 MD4 哈希
- 场景 bundle 里，可能会出现 `PlayerBuild-SceneName`
- 它还可能有配套的 `.sharedasset`、`.resS`、`.resource`

这些文件显然不是操作系统上独立摆着的，而是：

`作为 archive 内部文件，被这层目录语义组织起来。`

## 3. 这也是为什么“bundle 里有什么”不等于“里面只有一个主文件”

很多时候我们会说：

`这个 bundle 里有一个 CAB 文件。`

但更完整的说法其实是：

`这个 bundle archive 里至少有一个主 SerializedFile，还可能有若干大块二进制数据文件。`

如果这层不拆开，后面就很难解释：

- 为什么纹理和网格的大数据常常在 `.resS`
- 为什么音频和视频常在 `.resource`
- 为什么 scene bundle 会出现 `sharedasset`

## 五、SerializedFile：真正恢复对象图的关键，不在 Header，也不在 Block

前面几层更多是在讲：

- 这是个什么容器
- 它怎么压缩
- 它里面有哪些文件

但真正让 Unity 把内容恢复成对象的关键，最后还是会落到：

`SerializedFile`

## 1. SerializedFile 才是 Unity 对象世界真正站住的地方

Unity 官方对 AssetBundle 内部文件的描述很明确：

- 主文件是 Unity 的 `serialized file format`
- 它里面包含 `AssetBundle` 对象，以及所有被打进来的资产对象

也就是说，真正承载：

- `GameObject`
- `Material`
- `Mesh`
- `Texture` 的对象记录
- 引用关系
- Type Tree 信息

这些东西的核心，不是 Header，不是 Block，而是 `SerializedFile`。

所以如果把 AssetBundle 比作一层层容器，那么 `SerializedFile` 更接近：

`对象图真正落地的那一层。`

## 2. 对资产 bundle 和场景 bundle 来说，SerializedFile 的布局还不完全一样

官方文档当前给出的区分是：

### 1. 资产 AssetBundle

通常会把资产对象写进一个单独的 `SerializedFile` 里。

### 2. Scene AssetBundle

更像 Player build 的布局：

- 每个 scene 有自己的 serialized file
- 引用的其他对象可能进入对应的 `sharedasset`

这也是为什么 Scene bundle 的重复资源和边界问题，常常比普通资产 bundle 更复杂。

## 3. 大块二进制数据为什么会被拆去 `.resS` / `.resource`

这件事如果回到 `SerializedFile` 层就很好理解了。

如果所有大纹理、网格、音频都硬塞进主序列化文件：

- 主文件会非常胖
- 读取对象记录和读取大块二进制数据会彼此干扰
- 多线程从磁盘读取大二进制也不方便

所以 Unity 会把这些大块数据拆到：

- `.resS`
- `.resource`

而主 `SerializedFile` 里保留的是：

`对象记录和对这些外部大数据的连接关系。`

这又一次说明，AssetBundle 的关键不是“一个大文件”，而是：

`Archive 里的一组内部文件共同组成内容世界。`

## 六、把四层重新接回运行时：AssetBundle.LoadFromFile 到底在跨什么

如果把前面四层再接回运行时链，事情会变得非常清楚。

当你做一次 `AssetBundle.LoadFromFile`，从结构上更接近在发生这些事：

1. 先读 `Header`，确认这是个怎样的 archive
2. 再根据压缩组织方式读取或解压命中的 `Block`
3. 再靠 archive 内部的目录语义找到目标虚拟文件
4. 然后打开主 `SerializedFile`
5. 再按 `SerializedFile` 的对象记录、引用和外部数据连接，把对象逐步恢复出来

也就是说，真正的运行时链并不是：

`打开 bundle -> 直接拿到对象`

而更接近：

`打开 archive -> 找内部文件 -> 读 serialized data -> 恢复对象`

这也是为什么我前面一直强调：

`AssetBundle 是交付容器，不是最终对象本体。`

## 七、为什么这层结构值得写：它直接决定你后面会怎么理解性能、重复和排障

如果只把这篇当格式兴趣贴，它价值会比较小。  
但如果把它接回工程判断，这层结构其实很值钱。

## 1. 它解释了为什么压缩格式会直接影响首载成本

因为真正被压缩的不是“一个对象”，而是 archive 的 content section。  
`LZMA` 和 `LZ4` 的差异，会直接决定你读一个对象时要不要先处理整段内容区，还是只处理命中的那几个块。

## 2. 它解释了为什么 Scene bundle 更容易带出重复资源

因为 Scene bundle 的内部布局更接近 Player build，会有 scene file 和 `sharedasset` 的关系；  
而 `sharedasset` 的计算边界只覆盖同一个 bundle 内部的 scene。

这意味着：

`scene 怎么分 bundle，直接影响重复对象怎么被切开。`

## 3. 它解释了为什么很多问题不是“文件下到了没”

因为哪怕 bundle 文件已经下到本地，后面还要继续跨：

- Archive 挂载
- Block 解压
- 内部文件定位
- SerializedFile 读取
- 对象恢复

所以“下载完成”距离“对象可用”，中间其实还隔着好几层。

## 最后收成一句话

如果把这篇最后再压回一句话，我会这样说：

`AssetBundle 文件内部最稳的理解，不是“一个压缩包里塞着资源”，而是“一个会被挂进 Unity VFS 的 Archive”：Header 先定义怎么读这份归档，Block 决定内容区怎么被压缩和随机访问，Directory 语义负责把内部虚拟文件组织起来，而 SerializedFile 才是真正承载 Unity 对象图的那层。`
