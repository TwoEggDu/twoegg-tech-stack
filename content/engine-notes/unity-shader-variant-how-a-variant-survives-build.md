---
date: "2026-03-30"
title: "Unity Shader Variant 到底怎样才会被保留下来：一个 variant 要过哪几关"
description: "把 Shader Variant 从“理论上存在”到“最终真的留在目标构建里”的链路拆开：可能空间、使用面、URP Prefiltering、内置 stripping、自定义 stripping、交付边界和运行时预热分别站在哪。"
slug: "unity-shader-variant-how-a-variant-survives-build"
weight: 35
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Build"
  - "URP"
  - "SVC"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
很多人真到项目里会卡在一句话上：

`为什么这个 keyword 明明有材质在用，SVC 里也记了，最后构建里还是没有这条 variant？`

如果把 Unity 实际构建链路按顺序摊开，这个问题会简单很多。

> 如果只留一句话，我会这样压：  
> 一个 `Shader Variant` 能不能留下来，不取决于“有没有这个 keyword”，而取决于它有没有先进入这次构建的候选集，再依次通过渲染管线配置、内置 stripping、脚本 stripping 和交付边界。

也就是说，真正的链路更像这样：

`Shader 声明可能空间 -> 本次构建的使用面 -> 渲染管线配置过滤 -> Unity 内置 stripping -> SRP / 项目 stripping -> 写进正确交付物 -> 运行时加载与预热`

这篇就只做一件事：

`把一个 Shader Variant 想留下来，到底要过哪几关，按构建顺序讲清楚。`

---

## 一、第一关：这条 variant 得先在“理论上存在”

最前面这关看的是：

`Shader 源码里到底有没有这条编译路径。`

如果一条路径根本没有被声明成 variant，那后面所有“保留”讨论都无从谈起。

这里至少要先分清三件事：

- `multi_compile` / `shader_feature` / `shader_feature_local` 这类声明，定义的是“可能的编译分叉”
- `Pass` 不同，variant 空间也不同
- 纯运行时参数改值，不等于新增了一条编译期 variant

所以一条 variant 最早不是从 `SVC` 开始的，而是从：

`Shader 自己有没有把这条路径声明出来`

开始的。

---

## 二、第二关：它要进入“本次构建的真实使用面”

这一步是很多项目最容易误判的地方。

Unity 并不是看“理论上有多少 keyword 组合”，而是更接近看：

`这次构建里，哪些 shader / pass / keyword 组合真的被当前参与构建的内容用到了。`

典型来源有三类：

- 参与本次构建的 `Scene`、`Resources`、`AssetBundle` 里的材质
- 显式登记的 `ShaderVariantCollection`
- 少数全局策略带进来的 shader

这里最关键的判断是：

`不在本次构建输入里的内容，不会自动贡献它的 keyword 使用面。`

所以你会遇到这类情况：

- 某个材质在项目里确实存在
- 但它在这次 `Player` 构建里并没有参与
- 或者它只在另一个独立 `AssetBundle` 里
- 那这条 keyword 组合就未必会进入这次构建的 variant 候选集

`SVC` 在这一层真正做的事，是把一部分“项目显式关心的 keyword 组合”并进这次构建的使用面。

但它要解决的是：

`让这条路径有资格被纳入候选`

而不是：

`从此以后无论前后发生什么，这条 variant 都一定存在。`

这也是为什么：

`SVC` 很重要，但它不是一张“全流程保留通行证”。`

### 一个少见但要知道的例外

Unity 编辑器里还有一条很少在日常项目里直接用到的特殊构建模式：按某个 `SVC` 直接驱动 variant 枚举。

这条链路和普通项目默认构建不一样，但大多数项目现场讨论的，都不是它。

所以如果你没有明确走这种特殊模式，默认都应该按普通路径理解：

`SVC 只是并入候选使用面，不是替代整条构建链。`

---

## 三、第三关：当前渲染管线得承认这条路径“有可能发生”

到了这里，问题就不再只是“项目有没有用到”，而变成：

`当前渲染管线配置，认不认这条路径在这次构建里是可能发生的。`

这一步在 URP 项目里尤其重要。

因为 URP 不只是等你把 variant 全列出来再慢慢裁，它还会根据当前 `Pipeline Asset`、`Renderer Feature`、图形 API、质量档和部分全局功能配置，提前把“不可能发生”的路径剪掉。

所以有些 variant 的死亡顺序其实是：

1. 材质或 `SVC` 的确让它进入了候选讨论范围
2. 但当前 URP 配置判断这条路径在这次构建里不可能发生
3. 于是它在更早的设置过滤阶段就被拿掉了

这也是很多人会误以为：

`是不是 OnProcessShader 把它删了`

但实际上，问题发生得更早。

### 这正是 `Decal Layers` 这类问题容易出事的原因

`Decal Layers` 不是一个“我把 keyword 写进 SVC 就稳了”的问题。

它还依赖：

- 当前 `Renderer Feature` 是否真的启用了 `Decal`
- 是否启用了和 `Rendering Layers` 相关的那条路径
- 当前图形 API 是否支持这组路径
- 当前构建用到的 `URP Asset / Renderer` 是否把这组能力算进来了

如果这些前提里有一个不成立，那么你即使把相关 keyword 记进 `SVC`，它也仍然可能被当成：

`这次构建里不可能发生的路径`

然后在更早的设置过滤阶段消失。

所以遇到“`Decal Layers` 的 keyword 明明在 `SVC` 里，为什么还是没了”这类问题时，第一反应不该是：

`是不是 SVC 没生效`

而更应该先问：

`这次构建实际生效的 URP Asset / Renderer / Graphics API，到底承不承认这条路径存在。`

---

## 四、第四关：它还要过 Unity 自己的内置 stripping

哪怕一条 variant 已经进入候选集，也不代表它一定会留下。

后面还有一层更通用的内置剔除，它看的不是“这个材质在不在”，而更接近：

`按这次构建的全局渲染配置，这些内置路径是不是根本没必要保留。`

最典型的是：

- 雾效模式
- 光照贴图模式
- 阴影相关全局路径
- 编辑器专用路径
- instancing 的全局保留 / 强制剔除判断

这一层很关键，因为它解释了另一个常见误会：

`Always Included 不等于完全不剔除。`

更准确地说，`Always Included` 更像是：

- 不再按项目局部使用面做那种细粒度的保留判断
- 但仍然会按全局渲染配置做一轮更粗的剔除

所以它比普通路径更稳，但它并不是：

`把这个 shader 的所有理论 variant 全部无脑塞进包里。`

---

## 五、第五关：它还要过 SRP 和项目自己的 stripping

到了这一步，留下来的候选 variant，才会进入很多团队熟悉的那层：

`IPreprocessShaders`

这里需要特别钉住一个顺序：

`你在 OnProcessShader 里看到的，不是理论全集，而是前面几关已经活下来的那一批候选。`

也就是说，如果一条 variant：

- 前面就没有进入候选集
- 或者已经在 URP 的设置过滤里被判掉
- 或者已经在 Unity 内置 stripping 里被拿掉

那你在自定义 `IPreprocessShaders` 里根本看不到它。

这层常见来源有两类：

- SRP 自己的 scriptable stripping
- 项目写的自定义 `IPreprocessShaders`

它们做的通常是：

- 去掉业务上永远不会发生的组合
- 去掉某平台不需要的路径
- 去掉 debug / 开发态才需要的变体

所以这层回答的是：

`前面都还活着的 variant，项目还想不想继续留。`

而不是：

`把前面已经判死的东西救回来。`

---

## 六、第六关：它得被写进正确的交付边界

就算一条 variant 通过了前面的构建判断，它还要落到正确的交付物里，问题才算真正结束。

这一步最容易在 `Player` 和 `AssetBundle` 之间出错。

因为“留下来”其实至少有两种不同含义：

- 留在 `Player` 全局里
- 留在某个独立 `AssetBundle` 自己负责的那部分里

这两种不是一回事。

例如：

- 某个 shader 在 `Always Included` 里
- 那它更接近是 `Player` 全局负责提供
- bundle 侧更像只持有引用

反过来，如果它不在 `Always Included` 里，那么就更依赖：

`这次负责交付它的 Player / Bundle，自己把相关 shader 代码和 variant 带齐。`

所以一条路径“在 Player 构建里留住了”，不等于“在某个独立 bundle 构建里也留住了”。

这也是为什么 shader variant 问题一到 `AssetBundle`、热更新或多包型场景，就会陡然变复杂。

---

## 七、最后才是运行时加载与预热，但这已经不是“保留”问题了

很多讨论会把下面几件事混在一起：

- variant 根本没编进目标构建
- variant 编进去了，但第一次命中才加载
- SVC 在，但没正确加载或没 `WarmUp`

这三件事不是一层问题。

如果一条 variant 在前面的构建链路里就没留下来，那么后面的 `WarmUp` 再正确也没法把它凭空变出来。

`WarmUp` 回答的是：  
`已经存在的 variant，要不要在更早、更可控的时机准备好。`

它不回答：

`这条 variant 到底有没有被编进来。`

所以真正稳定的判断顺序一定是：

1. 先问它有没有留下来
2. 再问它有没有被正确交付
3. 最后才问它有没有被正确预热

---

## 八、把整条链压成六个判断问题

如果你以后再遇到“这条 variant 为什么没了”，我建议直接按这六问去压：

1. 这条路径在 shader 里到底有没有被声明成 variant？
2. 它有没有进入这次构建的真实使用面？
3. 当前 `URP / SRP` 配置有没有把它提前判成“不可能发生”？
4. Unity 内置 stripping 有没有按全局配置把它裁掉？
5. SRP 或项目自定义 stripping 有没有把它删掉？
6. 它最后是不是被写进了正确的 `Player / AssetBundle` 边界？

只要这六问按顺序问，很多现场讨论会立刻从：

`为什么它又玄学地没了`

变成：

`它到底死在哪一关。`

---

## 九、用一句话解释你最开始那个 `Decal Layers` 现场

如果一个 `Decal Layers` 相关 keyword 明明在 `SVC` 里，最后还是没留下来，那么最值得优先怀疑的通常不是：

`SVC 失效了`

而是：

`这条路径虽然被你显式登记了，但当前 URP Asset / Renderer Feature / Graphics API / 包型边界并没有把它当成这次构建里真正可能发生的路径。`

它死的位置，更可能在：

- 更早的渲染管线设置过滤
- 或后面的 URP stripping

而不是单纯死在你自己看到的那份 `SVC` 之外。

---

## 十、官方排查抓手，分别对应哪一关

Unity 官方文档和那篇 variant 排查文章里，有三组工具特别值得固定下来。

因为它们刚好能把前面那条“保留链”落成三种证据。

### 1. 想看它有没有留到构建里，用 `Editor.log`

最直接的官方办法是：

- 构建后看 `Editor.log`
- 搜 `Compiling shader`
- 如果是 `URP / HDRP`，再打开 `Shader Variant Log Level`

这组日志最适合回答：

`这条路径到底有没有活到构建产物生成阶段。`

也就是前面几关里：

- 候选空间
- 配置过滤
- 内置 stripping
- scriptable stripping

之后，最终还剩多少。

### 2. 想看运行时是不是第一次才编 GPU 程序，用 `Log Shader Compilation`

如果你怀疑问题不是“没保留”，而是：

`保留了，但第一次命中时才真正触发驱动侧编译`

那官方更推荐的抓手是：

- 打开 `Log Shader Compilation`
- 用 Development Build 跑目标内容

这组信息更适合回答：

`它是不是已经进了包，但运行时第一次命中才真正走到 GPU 侧准备。`

这不是“保留没保留”的同一层问题，但它能帮你避免把：

- 变体缺失
- 首次编译卡顿
- 预热时机不对

这三件事继续混在一起。

### 3. 想把“近似匹配”变成显式报错，用 `Strict Shader Variant Matching`

官方这几年最值得纳入日常排查习惯的一个设置，就是：

`Strict Shader Variant Matching`

默认情况下，Unity 在运行时如果找不到精确 variant，会尽量找一个“最接近”的版本顶上。

这对玩家来说有时比较平滑，但对排查来说反而会遮住真正的问题。

打开严格匹配后，Unity 会：

- 不再默默挑一个近似 variant
- 找不到精确组合时直接报错
- 并给出对应 shader、pass 和 keywords

这组信息特别适合回答：

`这条运行时真实请求的 variant，到底是不是根本没留住。`

所以如果你们后面真要做 variant stripping 回归，我会很建议把这一项放进最小验证流程。

---

## 结论

最后把这篇压成几句最有用的工程结论：

1. `Shader Variant` 能不能留下来，先看“有没有这条路径”，再看“这次构建有没有用到”，最后才看“删不删”。
2. `SVC` 很重要，但它主要解决的是“显式登记和预热入口”，不是整条构建链的万能保留开关。
3. `URP` 项目里一定要把 `Pipeline Asset`、`Renderer Feature`、图形 API 和质量档放进同一张判断图里，因为很多 variant 在到达 `IPreprocessShaders` 之前就已经没了。
4. `Always Included` 也不是完全不剔除；它更像改变了“谁负责带这份 shader 与 variant”的交付边界。
5. 官方给你的三种证据要分开用：`Editor.log` 看构建期保留结果，`Log Shader Compilation` 看运行时首次编译，`Strict Shader Variant Matching` 看精确 variant 是否缺失。
6. 真正稳的问法不是“这个 keyword 在不在 SVC 里”，而是：

`这条 variant 到底死在哪一关。`

---

延伸读这几篇会更顺：

- [ShaderVariantCollection 到底是干什么的：记录、预热、保留与它不负责的事]({{< relref "engine-notes/unity-what-shadervariantcollection-is-for.md" >}})
- [URP 的 Shader Variant 管理：Prefiltering、Strip 设置和多 Pipeline Asset 对变体集合的影响]({{< relref "engine-notes/unity-urp-shader-variant-prefiltering-strip-settings.md" >}})
- [SVC、Always Included、Stripping 到底各自该在什么场景下用]({{< relref "engine-notes/unity-svc-always-included-stripping-when-to-use-which.md" >}})
- [为什么 Shader 加到 Always Included 就好了：它和放进 AssetBundle 到底差在哪]({{< relref "engine-notes/unity-why-always-included-shaders-fixes-assetbundle-problems.md" >}})
- [Unity Shader Variant 运行时命中机制：从 SetPass 到变体匹配的完整链路]({{< relref "engine-notes/unity-shader-variant-runtime-hit-mechanism.md" >}})
