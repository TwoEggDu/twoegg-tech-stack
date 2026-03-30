---
date: "2026-03-30"
title: "Material、场景、SVC、Always Included 是怎么一起决定 Shader Variant 保留面的"
description: "把 Material、场景、ShaderVariantCollection 和 Always Included 放回同一条构建链里，讲清它们谁在提供保留依据、谁在改变交付边界，以及它们为什么不是同一种按钮。"
slug: "unity-shader-variant-retention-sources-material-scene-svc-always-included"
weight: 30
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Build"
  - "SVC"
  - "Always Included"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
很多团队讨论 `Shader Variant` 时，最容易把四样东西揉成一坨：

- `Material`
- `Scene`
- `ShaderVariantCollection`
- `Always Included Shaders`

然后现场对话就会快速变成：

`这个 shader 我不是已经加到 SVC 了吗？`

`这个材质明明在场景里啊。`

`不行就放 Always Included。`

这些话都在试图回答同一个问题：

`这条 variant 到底为什么应该被留下来。`

但它们其实不是同一种东西，也不在同一层。

> 如果只留一句话，我会这样压：  
> `Material / 场景 / SVC` 更像在提供“这次构建为什么要保留这条路径”的依据，`Always Included` 更像在改变“谁来负责把这份 shader 带进交付物”的边界。

这篇就只回答这一个问题：

`Material、场景、SVC、Always Included 到底是怎么协作决定 Shader Variant 保留面的。`

---

## 一、先把四者分成两类，不要一上来就把它们当成同一类按钮

最稳的拆法其实很简单。

第一类是：

`谁在告诉构建系统：这条路径真的会被项目用到。`

这里主要是：

- `Material`
- 参与本次构建的 `Scene / Resources / Bundle 输入`
- `SVC`

第二类是：

`谁在改变 shader 的归属边界，让它不再只由本次局部内容自己负责。`

这里主要是：

- `Always Included`

所以这四者不是并列关系。

更准确地说：

- `Material / 场景` 是默认使用面
- `SVC` 是显式补充的使用面
- `Always Included` 是更粗的一层全局交付策略

这也是为什么：

`SVC` 和 `Always Included` 不是谁更高级的问题，而是它们根本不在同一层。`

---

## 二、Material 和场景：默认的保留依据，站位最靠前

大多数 variant 能留下来，最普通的原因其实不是你做了什么高级治理，而是：

`参与本次构建的材质和内容，本来就在真实使用这条路径。`

这层最容易被忽略的一个事实是：

`不是项目里存在的所有材质，都会自动成为这次构建的保留依据。`

真正有资格参与这次 build 判断的，通常是：

- 当前要构建进 `Player` 的场景对象
- `Resources` 里会被一并带入的对象
- 本次参与构建的 `AssetBundle / Addressables` 输入
- 构建链显式收集到的依赖对象

所以“场景里有材质”这句话，真正有效时，隐含条件其实是：

`这个场景和这个材质真的属于当前这次构建输入。`

如果不满足这个条件，常见误会就来了：

- 材质明明在项目里
- 编辑器里也能看到
- 但它没有参与这次 `Player` 构建
- 那它就未必会给这次构建贡献真实使用面

换句话说：

`Material / Scene` 解决的是“当前构建真实内容在用什么”，不是“项目理论上未来可能会用什么”。`

---

## 三、SVC：补“场景和材质看不到”的关键路径，不是替代整个世界

`ShaderVariantCollection` 最有价值的地方，不是“神奇地生成更多 variant”，而是：

`把项目显式关心的那批路径补进构建使用面。`

这类路径通常有几种典型来源：

- 首次进入关键场景时一定会命中的特效
- 不在默认场景里，但运行时一定会下载的活动内容
- 需要提前 `WarmUp` 的首屏或战斗入口
- 靠编辑器静态扫描很难自然覆盖到的组合

所以 `SVC` 的位置，应该理解成：

`对默认材质 / 场景使用面的显式补充。`

它在工程上做的是两件事：

1. 让一些本来不容易被当前构建输入自然带出来的 keyword 组合，有机会进入保留判断
2. 让这些显式关心的路径，后续可以被分组、加载、预热和回归

这也是为什么 `SVC` 特别适合处理下面这种项目现实：

- 真实运行时会走到一条路径
- 但这条路径不稳定出现在主场景里
- 或者它只在活动包 / 热更包 / 某个入口里才会命中

这时候，如果只靠默认场景和材质输入，使用面经常是不完整的。

`SVC` 就是用来补这块缺口的。

### 但它不等于“写进去就一定完整”

这里要特别钉住一句话：

`SVC` 提供的是显式保留依据，不是最终存在保证。`

也就是说，哪怕你把一条路径登记进 `SVC`，它后面仍然可能出问题：

- 当前渲染管线配置觉得这条路径不可能发生
- URP 预过滤提前把它剪掉
- 后续 stripping 又把它删了
- 或者它最后没有进入正确的交付边界

所以 `SVC` 更像：

`我明确告诉构建系统，这条路径值得保留，请把它纳入正式讨论。`

而不是：

`从此以后它一定已经安全地存在于所有目标构建里。`

---

## 四、Always Included：它不是“更大的 SVC”，而是在改交付责任

`Always Included Shaders` 最容易被误解成：

`全局版 SVC`

但这其实不准确。

`SVC` 仍然是在说：

`项目显式关心哪些具体 variant 路径。`

而 `Always Included` 更像是在说：

`这份 shader 不再只由局部内容是否引用来决定，而是由 Player 全局负责把它带进去。`

它改变的核心不是“多了一份关键词清单”，而是：

`shader 的归属边界。`

所以当一个 shader 被放进 `Always Included` 时，工程含义更接近：

- 它不再完全依赖当前场景 / 当前 bundle 自己证明“我用到了哪些路径”
- 它更像被当成全局基础能力来处理
- bundle 侧很多时候只是在引用它，而不是自己再承载一整份 shader 本体

这也是为什么它经常表现得像一个强力止血按钮。

因为它回答的问题其实已经不是：

`这条 variant 是不是被当前场景自然带到了`

而变成：

`这份 shader 本身就由 Player 全局兜底了。`

### 所以它解决的问题和 `SVC` 根本不一样

如果一定要压成一句区分：

- `SVC` 更像精细保留关键路径
- `Always Included` 更像全局兜底 shader 归属

这两个不是替代关系，而是职责不同。

---

## 五、把四者放回同一条构建链里，顺序会清楚很多

如果把整个协作顺序按构建思路摆开，通常更接近下面这样：

1. `Shader` 先定义理论上可能有哪些 variant
2. 本次构建输入里的 `Material / Scene / Bundle 对象` 提供默认使用面
3. `SVC` 把显式关心但默认输入看不到的关键路径补进来
4. 如果 shader 在 `Always Included` 里，交付责任被提升到 `Player` 全局层
5. 后续再由 URP / Unity / 项目自己的 stripping 决定哪些还能继续活下来
6. 最后把活下来的结果写进正确的 `Player / AssetBundle` 边界

这里最值得记住的是第三步和第四步的区别：

- `SVC` 是在补“哪些路径值得参与保留”
- `Always Included` 是在改“谁来承担 shader 存在责任”

这就是它们为什么能同时出现，而且并不矛盾。

---

## 六、在 Player 和 AssetBundle 场景里，这四者的协作关系会不一样

这一步是很多团队真正踩坑的地方。

### 1. 纯 Player 场景里

更常见的链路是：

- 场景和材质先贡献默认使用面
- `SVC` 补关键路径
- `Always Included` 兜全局基础 shader

这时问题相对容易理解，因为很多依赖都在同一条 `Player` 构建链里。

### 2. AssetBundle / 热更新场景里

问题会立刻复杂一层。

因为这时你要回答的不只是：

`这条路径该不该保留`

还要回答：

`它最后到底由 Player 负责，还是由这个 Bundle 自己负责。`

于是四者的职责会变成：

- `Material / 场景`：本次参与 build 的 bundle 内容真实在用什么
- `SVC`：补 bundle 静态输入看不到但运行时会走到的关键路径
- `Always Included`：把某些 shader 的责任重新推回 Player 全局
- stripping：继续根据配置和规则删掉“不需要”的部分

所以在 bundle 场景里，最容易出的问题不是：

`这些工具谁更强`

而是：

`现在到底是谁在负责带齐这份 shader 和它的关键路径。`

---

## 七、最常见的四种误会

### 1. “材质在项目里”就等于“材质参与了这次构建”

不是。

只有真的进入当前 `Player / Bundle` 构建输入的内容，才会自然提供保留依据。

### 2. “SVC 里有”就等于“目标包里一定有”

不是。

`SVC` 只是让这条路径进入正式讨论，后面还要过渲染管线配置、stripping 和交付边界。

### 3. “Always Included”就是“更大号的 SVC”

不是。

`Always Included` 主要是在改 shader 的交付归属边界，不是在替你列一份更精细的 variant 清单。

### 4. “既然有 Always Included，就不需要 SVC 了”

也不是。

`Always Included` 解决的是“这份 shader 要不要全局兜底”，而 `SVC` 解决的是“哪些关键路径需要显式保留和预热”。

在认真做首载稳定性和内容分包治理的项目里，两者经常会同时存在。

---

## 八、如果只给一套最小判断法，我会这样问

以后再遇到现场争论“到底该靠材质、SVC 还是 Always Included”时，我会先问四句：

1. 这条路径是不是已经稳定地出现在本次构建输入的材质和场景里？
2. 如果没有，项目能不能用 `SVC` 把它显式补进来？
3. 这个问题本质上是在缺“关键路径清单”，还是在缺“全局兜底 shader”？
4. 这份 shader 最后到底应该由 `Player` 全局负责，还是由局部 bundle 自己负责？

这四句一问，很多看似混乱的讨论会马上清楚。

因为你会发现，大家争的其实通常不是一个按钮，而是两个完全不同的问题：

- 保留依据够不够
- 交付责任归谁

---

## 结论

最后把这篇压成几句最有用的话：

1. `Material / 场景` 是默认保留依据，它们定义“当前构建真实在用什么”。
2. `SVC` 是显式补充的保留依据，解决“默认输入看不到，但运行时确实重要”的关键路径。
3. `Always Included` 不是更大的 `SVC`，它更像把 shader 的责任提升到 `Player` 全局兜底层。
4. 真正稳的理解不是“这几个按钮谁更强”，而是：

`谁在提供保留依据，谁在承担交付责任。`

---

延伸读这几篇会更顺：

- [ShaderVariantCollection 到底是干什么的：记录、预热、保留与它不负责的事]({{< relref "engine-notes/unity-what-shadervariantcollection-is-for.md" >}})
- [为什么 Shader 加到 Always Included 就好了：它和放进 AssetBundle 到底差在哪]({{< relref "engine-notes/unity-why-always-included-shaders-fixes-assetbundle-problems.md" >}})
- [Unity Shader Variant 到底怎样才会被保留下来：一个 variant 要过哪几关]({{< relref "engine-notes/unity-shader-variant-how-a-variant-survives-build.md" >}})
- [SVC、Always Included、Stripping 到底各自该在什么场景下用]({{< relref "engine-notes/unity-svc-always-included-stripping-when-to-use-which.md" >}})
