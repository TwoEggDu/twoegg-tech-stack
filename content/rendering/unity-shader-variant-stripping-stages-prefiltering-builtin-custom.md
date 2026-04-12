---
date: "2026-03-30"
title: "Unity Shader Variant 是在哪几层被剔除的：Prefiltering、内置 Stripping 和自定义 Stripping 的顺序"
description: "把 Shader Variant 常见的过滤与剔除层拆成设置过滤、URP Prefiltering、Unity 内置 stripping、SRP stripping 与项目自定义 stripping，讲清它们的顺序、证据和最短排查法。"
slug: "unity-shader-variant-stripping-stages-prefiltering-builtin-custom"
weight: 40
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Build"
  - "URP"
  - "Stripping"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
`Shader Variant` 丢失现场里，最常见的一句误判是：

`我在 OnProcessShader 里根本没删它，为什么它还是没了？`

这句话的问题不在于它完全错，而在于它默认把“剔除”理解成只有一层。

但真实项目里，variant 可能死在好几层不同的地方：

- 还没进入普通枚举前，就被设置过滤掉
- 进入候选后，被 Unity 内置 stripping 拿掉
- 被 SRP 自己的 stripping 拿掉
- 最后才被项目自己的 `IPreprocessShaders` 删掉

所以如果不先把这些层拆开，排查就会一直卡在：

`到底是谁删的。`

> 如果只留一句话，我会这样压：  
> `Shader Variant` 缺失不等于“某个地方统一做了 strip”，更常见的现实是它在不同层级被提前判成“这次构建不需要”。

这篇只回答一个问题：

`Unity Shader Variant 到底会在哪几层被剔除，以及我们怎么快速判断它死在哪一层。`

---

## 一、先把“没进入候选”与“进入候选后被删”分开

这是整篇最重要的分界线。

很多团队一说“被剔除了”，其实混着两种完全不同的情况：

### 1. 它根本没有进入这次构建的普通候选集

这类更接近：

- 当前配置觉得这条路径不可能发生
- 渲染管线提前把它过滤掉
- 它连后面的普通 stripping 都还没走到

这时候你在很多后续脚本回调里根本看不到它。

### 2. 它进入候选集了，但后面又被删掉

这类才更像通常口头说的“strip”：

- Unity 内置 stripping
- SRP stripping
- 项目自定义 `IPreprocessShaders`

这两种一定要先分开。

因为它们的排查方法完全不同。

---

## 二、第一层：设置过滤和 URP Prefiltering，很多 variant 死得比你想的更早

这一层最适合用一句话概括：

`不是所有理论上存在的 variant，都会进入普通构建枚举。`

在 URP 项目里，这个现象尤其明显。

它会根据当前这次构建真正生效的内容去过滤路径，例如：

- `Pipeline Asset` 的功能开关
- `Renderer Feature`
- 图形 API
- 质量档
- 某些平台能力约束

所以有些路径的真实死亡顺序是：

1. `Shader` 的确声明了这条 variant
2. 材质或 `SVC` 也确实让它具备了“值得讨论”的理由
3. 但当前 URP 配置判断它在这次构建里根本不可能发生
4. 它就在普通 stripping 之前被过滤掉了

这就是为什么：

`你明明没在 OnProcessShader 里删它，它还是可能已经没了。`

### `Decal Layers` 就是最典型的例子

如果 `Decal Layers` 相关路径依赖：

- 特定 `Renderer Feature`
- 特定 API
- 特定渲染设置

那么即使你把对应 keyword 补进 `SVC`，也不等于它一定能活到后面。

因为如果当前构建生效的 `Renderer / Asset / API` 本身不承认这条路径可能发生，它会先死在这里。

---

## 三、第二层：Unity 自己的内置 stripping

通过前一层以后，variant 还会继续经过更通用的一轮内置剔除。

这一层看的重点不再是“当前场景具体有没有某个材质”，而更像：

`按这次构建的全局渲染配置，这些公共路径是不是根本不需要保留。`

常见来源包括：

- 雾效模式
- 光照贴图模式
- 阴影相关全局路径
- instancing 的保留 / 强制剔除判断
- 编辑器专用路径

所以内置 stripping 更接近：

`全局能力边界上的粗剔除`

而不是：

`按你项目真实内容做细粒度治理。`

### 这层也解释了一个经常被误会的点

`Always Included` 不等于完全没有 stripping。`

它更准确的理解通常是：

- 不再那么依赖局部内容证明“我到底用了哪几组 keyword”
- 但仍然会受全局配置级的粗剔除影响

所以它更稳，但不是“完全不删”。

---

## 四、第三层：SRP 自己的 stripping

到了这一步，很多 SRP 项目又会再叠一层自己的逻辑。

最典型的就是 URP / HDRP 自己那套针对管线功能的 stripping。

这一层的判断通常会围绕：

- 当前管线到底开了哪些功能
- 哪些 pass / keyword 在当前 feature 组合下根本不成立
- 哪些路径在所有支持 feature 集里都没必要保留

这里要注意一点：

`SRP stripping` 和前面的 `Prefiltering` 不是一回事。`

两者都跟 SRP 配置有关，但职责不同：

- 前面的过滤更像“先别把不可能的路径放进普通候选”
- 这一层更像“候选已经形成后，再按 SRP 规则继续删一轮”

如果把两者混成一层，你就很容易在排查时误判。

---

## 五、第四层：项目自己的 `IPreprocessShaders`

最后才是大多数团队最熟悉的那层：

`我自己写的 shader stripping 规则。`

这层通常最适合做的是：

- 平台边界明确的删除
- debug / development-only 路径删除
- 已经被业务证明永远不会发生的组合删除
- 对你们项目自定义 keyword 体系的精细治理

它最不适合做的是：

`替前面所有边界不清的配置错误擦屁股。`

因为如果你们还没搞清楚：

- 当前构建到底收集了什么
- 当前 URP 配置到底承认什么
- 哪些路径本来就不该靠局部材质证明

那你在 `IPreprocessShaders` 里写出来的规则，往往只是更晚、更危险地删。

### 这里最重要的工程事实是

`你在 OnProcessShader 里看到的，不是理论全集，而是前面活下来的那批候选。`

所以它只能删：

`已经活到这里的 variant`

不能解释：

`为什么一条路径你从头到尾都没见过。`

---

## 六、还有一类问题看起来像 stripping，其实是交付边界问题

有些现场会被描述成：

`variant 被剔除了`

但更准确地说，其实是：

`它没有出现在你以为该由它负责的那个交付物里。`

最典型的就是：

- `Player` 正常
- 独立 `AssetBundle` 异常

或者：

- 构建时 shader 在 `Always Included` 里
- 加载时列表不一致
- bundle 侧实际只有引用，没有 shader 实体可用

这类问题对现象来说很像“被删了”，但根因其实不是 stripping 顺序，而是：

`谁来负责带这份 shader / variant`

的边界搞错了。

所以排查时一定要把这层单独分出来，不然会一直在 stripping 规则里空转。

---

## 七、怎么快速判断它死在哪一层

如果真要在项目现场快速定位，我建议顺序固定成下面这样。

### 1. 先看构建日志，而不是先看运行时体感

核心看两件事：

- 这条 shader / pass 的 variant 数量在构建里有没有明显下降
- `After settings filtering`、`After built-in stripping`、`After scriptable stripping` 这些阶段性数字是怎么变的

它们对应的意思通常可以粗理解成：

- `After settings filtering`：前面的设置过滤 / Prefiltering 之后
- `After built-in stripping`：Unity 内置剔除之后
- `After scriptable stripping`：项目或 SRP 脚本剔除之后

### 2. 如果 `OnProcessShader` 从来没见过它，优先怀疑更早层

也就是优先查：

- 当前 `URP Asset`
- 当前 `Renderer Feature`
- 当前图形 API
- 当前质量档
- 当前 `SVC` 是否真的参与了这次 build

这类通常不是“你后面删了”，而是“它更早就没进来”。

### 3. 如果 `OnProcessShader` 见过它，但后面没了，再查脚本层

这时候才重点看：

- SRP 自带 stripping
- 项目自定义 stripping 逻辑
- 规则是不是误删了本来会发生的组合

### 4. 如果构建里有，运行时还出错，再切到另一条线

这时优先怀疑的就不再是 stripping，而是：

- 命中退化路径
- `SVC` 没加载
- `WarmUp` 时机不对
- 首次 GPU 编译卡顿

这已经是运行时层问题了。

---

## 八、如果只给一套最短排查顺序，我会这样排

1. 先确认这条路径在当前构建里到底有没有出现
2. 如果没有，先分“没进入候选”还是“进入后被删”
3. 如果连 `OnProcessShader` 都没见到，先查 `URP Asset / Renderer / API / 质量档 / SVC 输入`
4. 如果 `OnProcessShader` 见到了，再查 SRP 和项目 stripping
5. 如果构建里有，运行时还不对，就转去查命中、预热和交付边界

这套顺序的价值不在于“绝对完整”，而在于它能帮你快速避免最常见的误判：

`把所有问题都当成 IPreprocessShaders 的锅。`

---

## 九、最容易搞混的三句话

### 1. “它被 strip 了”

这句话至少可能指三件事：

- 前面就没进入普通候选
- 内置或 SRP stripping 删了
- 其实不是 strip，而是没进正确交付边界

### 2. “SVC 明明有”

这通常只能说明：

`你显式关心这条路径`

不能自动说明：

`它已经通过了当前配置过滤和后续所有剔除。`

### 3. “我没在 OnProcessShader 删”

这通常只能说明：

`你最后一层自定义 stripping 没删它`

不能说明：

`它前面几层就一定没死。`

---

## 结论

最后把这篇压成几句最有用的话：

1. `Shader Variant` 的“剔除”不是一层，而是多层按顺序发生的过滤和删除。
2. 最早一层往往不是你自己的 `IPreprocessShaders`，而是更前面的配置过滤和 `URP Prefiltering`。
3. `OnProcessShader` 只能看到已经活到那里的候选，解释不了“为什么它从头到尾都没出现”。
4. 真正稳的排查，不是先问“谁删了它”，而是先问：

`它到底死在候选前、内置层、SRP 层、自定义层，还是其实死在交付边界。`

---

延伸读这几篇会更顺：

- [URP 的 Shader Variant 管理：Prefiltering、Strip 设置和多 Pipeline Asset 对变体集合的影响]({{< relref "rendering/unity-urp-shader-variant-prefiltering-strip-settings.md" >}})
- [Unity Shader Variant 到底怎样才会被保留下来：一个 variant 要过哪几关]({{< relref "rendering/unity-shader-variant-retention-and-survival.md" >}})
- [Unity Shader Variant 缺失事故排查流程：从现象到根因的三层定位法]({{< relref "rendering/unity-shader-variant-missing-diagnosis-flow.md" >}})
- [Unity Shader Variant 实操：怎么知道项目用了哪些、运行时缺了哪些、以及怎么剔除不需要的]({{< relref "rendering/unity-shader-variants-how-to-find-missing-and-strip.md" >}})
