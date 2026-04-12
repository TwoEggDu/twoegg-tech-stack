---
date: "2026-03-30"
title: "Unity Shader Variant 全流程总览：从生产、保留、剔除到运行时使用"
description: "把 Unity Shader Variant 的完整生命周期串成一条连续链路：它从哪里生产出来，谁为本次构建提供保留依据，哪些层会提前过滤或剔除它，最后怎样被写进交付物并在运行时命中到 GPU 程序。"
slug: "unity-shader-variant-full-lifecycle-overview"
weight: 18
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Build"
  - "Rendering"
  - "URP"
series:
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
项目里一旦开始系统排查 `Shader Variant`，团队通常会同时撞上几类问题：

- 变体到底是从哪里来的
- 为什么材质、场景、`SVC`、`Always Included` 都在提，但职责又不一样
- 为什么有些路径明明写进了 `SVC`，最后构建里还是没留下来
- 为什么有时是粉材质，有时只是效果不对，有时只是首帧卡一下

这些问题之所以容易缠在一起，不是因为它们本身很玄，而是因为它们分属同一条生命周期的不同阶段。

如果只留一句话，我会这样压：

`Shader Variant` 问题本质上不是一个按钮有没有勾上的问题，而是一条从 `Shader 声明可能空间 -> 本次构建的真实使用面 -> 各层过滤与剔除 -> 写入正确交付物 -> 运行时命中 -> GPU 消费` 的连续链路问题。

这篇文章只做一件事：

`把一个 Shader Variant 从"为什么会出现"到"为什么会消失"再到"运行时怎么被 GPU 真正使用"，按顺序串成一条完整流程。`

如果你在项目里已经遇到过下面这些说法，这篇就是把它们放回同一条链里：

- `这个 keyword 明明有材质在用`
- `这个 variant 我已经放进 SVC 了`
- `不行就先放 Always Included`
- `OnProcessShader 里我没删它`
- `包里看起来有 shader，为什么运行时还是错`

---

## 一、先把三层坐标钉住，不然后面一定会混

理解 `Shader Variant`，最怕一上来就把"资产""编译结果""运行时命中"混成一层。更稳的拆法是先分成三层：

1. `资产定义层`：项目里到底声明了什么、引用了什么、显式登记了什么
2. `构建产物层`：哪些路径真的被保留下来，并写进了目标交付物
3. `运行时消费层`：当前 draw call 到底命中了哪条现成路径，GPU 最终拿到的是哪份程序

只要这三层一混，后面的判断就会连续出错：

- 把 `Material` 当成"已经编译好的 GPU 程序"
- 把 `SVC` 当成"保证一切都一定存在的保险箱"
- 把"包里有 shader"误解成"运行时一定会命中正确 variant"
- 把"首帧卡顿"误判成"变体被剔除了"

`资产存在`、`引用存在`、`正确平台结果存在`、`运行时命中正确路径`，是四件不同的事。它们之间任何一层断掉，最后都可能表现为"画面不对"。

后面每一节，都按这三层往前推。

---

## 二、再把整条生命周期压成一张图

理解 `Shader Variant`，最稳的方式不是先记工具名，而是先记阶段。

一条 variant 从定义到被 GPU 使用，通常会经过下面这几站：

<div class="lifecycle-flow">
  <div class="lifecycle-step">
    <span class="lifecycle-step-index">1</span>
    <div class="lifecycle-step-copy">
      <strong>理论空间</strong>
      <p><code>Shader / Pass / keywords / 平台维度</code> 先定义"理论上可能有哪些 variant"。</p>
    </div>
  </div>
  <div class="lifecycle-flow-arrow" aria-hidden="true">↓</div>
  <div class="lifecycle-step">
    <span class="lifecycle-step-index">2</span>
    <div class="lifecycle-step-copy">
      <strong>构建输入与保留依据</strong>
      <p><code>Scene / Material / Bundle 输入 / SVC / Always Included / 活跃 URP Asset</code> 决定这次 build 真正要讨论哪些路径。</p>
    </div>
  </div>
  <div class="lifecycle-flow-arrow" aria-hidden="true">↓</div>
  <div class="lifecycle-step">
    <span class="lifecycle-step-index">3</span>
    <div class="lifecycle-step-copy">
      <strong>本次构建候选集</strong>
      <p><code>Build Usage</code> 把"项目里存在的路径"收缩成"这次构建真的要处理的路径"。</p>
    </div>
  </div>
  <div class="lifecycle-flow-arrow" aria-hidden="true">↓</div>
  <div class="lifecycle-step lifecycle-step-group">
    <span class="lifecycle-step-index">4</span>
    <div class="lifecycle-step-copy">
      <strong>过滤与剔除</strong>
      <ul>
        <li><code>URP Prefiltering</code></li>
        <li><code>Unity Builtin Stripping</code></li>
        <li><code>SRP Stripping</code></li>
        <li><code>Project Stripping / OnProcessShader</code></li>
      </ul>
    </div>
  </div>
  <div class="lifecycle-flow-arrow" aria-hidden="true">↓</div>
  <div class="lifecycle-step">
    <span class="lifecycle-step-index">5</span>
    <div class="lifecycle-step-copy">
      <strong>交付与平台编译</strong>
      <p>活下来的路径会变成 <code>subprogram / program blob</code>，再写入 <code>Player</code> 或 <code>AssetBundle</code>。</p>
    </div>
  </div>
  <div class="lifecycle-flow-arrow" aria-hidden="true">↓</div>
  <div class="lifecycle-step">
    <span class="lifecycle-step-index">6</span>
    <div class="lifecycle-step-copy">
      <strong>运行时命中与 GPU 消费</strong>
      <p><code>SetPass / keyword 匹配</code> 决定最终命中的那份平台程序，再交给 GPU 真正执行。</p>
    </div>
  </div>
</div>

这里先澄清一个很容易混的点：

- `Material / Scene / Bundle 输入 / SVC / Always Included` 不是构建输出，它们是 `输入`、`保留依据` 和 `交付边界条件`
- 真正逐阶段产出的，分别是 `理论空间`、`本次候选集`、`逐层裁剪后的保留集合`、`平台编译结果` 和 `运行时命中结果`
- 所以现场一说"variant 怎么被剔除了"，最好先问的是 `它死在输入阶段、过滤阶段、stripping 阶段，还是其实已经留下来了，只是运行时没命中对`

如果你现在关心的不是总览，而是源码口径的构建细账，可以继续读这篇：

- [Unity Shader Variant 构建账单：Player Build 与 AssetBundle Build 的差异]({{< relref "engine-notes/unity-shader-variant-build-receipts-player-vs-ab.md" >}})

---

## 三、生产阶段：变体的来源和资产定义层

### 1. Shader 决定的是理论可能空间

很多团队第一反应会把 `Shader Variant` 理解成"材质 keyword 的不同组合"。这只说对了一部分。

从资产层看，`Shader` 最核心的职责，不是"直接交给 GPU 跑"，而是定义一套可被后续构建系统继续处理的渲染模板。它提供的是：

- 有哪些 `SubShader` 和 `Pass`
- 有哪些编译期分支（`multi_compile`、`shader_feature`、`shader_feature_local`）
- 不同 `Pass` 各自拥有独立的 variant 空间
- Unity 内置渲染路径和平台特性带来的分支
- SRP 自己附加的全局或局部 keyword
- `Renderer Feature`、光照、雾、阴影、Lightmap、Instancing、XR、图形 API、Quality 档等功能开关

所以 `Shader` 更像是：`后续构建系统可以继续展开、筛选和编译的源头定义`，而不是最终设备上那份稳定可执行的结果。

一条 variant 最早不是从 `Material` 开始的，而是从：`Shader 源码和渲染管线共同定义的"理论可能空间"` 开始的。

如果某条编译路径根本没在 `Shader` 或管线侧被声明出来，后面所有"保留""预热""剔除"的讨论都无从谈起。

这也是为什么项目里常见的第一类误会是：`我把运行时参数设成这样了，为什么构建里没有自动出现对应 variant？`——运行时参数值变化，并不等于新增了一个编译期 variant。

### 2. Material 决定的是"怎样使用某个 Shader"

`Material` 保存的重点不是 GPU 程序本体，而是：

- 指向哪个 `Shader`
- 当前这份材质有哪些属性值
- 当前启用了哪些功能状态
- 某些 keyword 或等价功能路径当前处于什么状态

所以 `Material` 更接近：`某个 Shader 的一份具体使用配置`。这也是为什么同一个 `Shader` 可以被许多 `Material` 以完全不同的方式使用，而这些使用方式最后又会反过来影响构建期对 variant 的保留判断。

### 3. SVC 决定的是"哪些路径值得被显式记住"

`ShaderVariantCollection` 很容易被误解成"把 shader 编译结果装进去"。但它更稳的定位是：`把项目显式关心的 shader / pass / keyword 组合登记出来`，价值在于：

- 给构建期提供额外的保留依据
- 给运行时 WarmUp 提供可操作的清单
- 给团队提供一份可回归、可审计的关键路径名单

所以 `SVC` 更像"清单"，不是"平台程序容器"。

### 4. Graphics Settings 和 URP Asset 也是资产输入的一部分

很多团队会把 `Shader`、`Material`、`SVC` 当成资产输入的全部，但这不够。下面这些配置同样会改变一条 variant 的命运：

- `Graphics Settings` 和 `Always Included Shaders`
- 当前生效的 `URP Asset` 和 `Renderer`
- 某些全局图形开关和质量档

因为它们会直接影响：哪些路径被视为"本次构建有可能发生"、哪些路径会被提早过滤、某份 shader 最后由谁负责交付。

---

## 四、保留依据阶段：谁在告诉构建系统"这条路径这次值得保留"

理论上存在，不等于这次构建里一定会留下来。

接下来构建系统要回答的问题是：`这次 build 到底为什么要关心这条 variant？`

这一步最容易混在一起的，是下面几类东西：

### 1. Material 和场景：默认使用面的最前线

大多数 variant 能留下来，最普通的原因不是你做了什么治理，而是：`参与本次构建的真实内容，确实在使用这条路径。`

参与构建的材质、场景、资源引用链，构成了最自然的保留依据。但这里有一个关键限制：

`项目里存在` 不等于 `这次构建里参与了输入`

一个材质即使就在工程里，只要它没有进入这次 `Player` 或目标 `AssetBundle` 的构建输入，它就不一定会为这次构建贡献使用面。

### 2. SVC：显式补充"场景里没直连，但项目仍然关心"的路径

`SVC` 的职责是：`把项目显式关心的某些路径补充进这次构建与运行时预热的讨论范围。`

它解决的不是"无论前后发生什么，这条 variant 必须永远存活"，而是：`有些路径不容易仅靠场景直连被自然覆盖，但我们明确知道运行时会用到。`

所以 `SVC` 是保留依据的一部分，不是对后续所有剔除层的绝对免死金牌。

### 3. Always Included：不是补充使用面，而是改变交付责任

`Always Included Shaders` 容易被误解成"更强力的 SVC"。它更像是在改一件事：`这份 shader 到底由谁来负责被带进最终交付物。`

也就是说：

- `Material / 场景 / SVC` 更像在提供"这次构建为什么要保留它"的依据
- `Always Included` 更像在改变"这份 shader 以怎样的全局边界被交付"

所以现场那句常见的"实在不行就先放 Always Included"，本质上是在粗粒度地改交付边界，而不是在精确治理某个具体 variant。

### 4. 这一阶段最容易犯的判断错误

团队在这里最容易混淆三件事：

- `项目里有这个材质`
- `这次构建里有这个材质`
- `这次构建里真的因此要保留这条 variant`

如果这三层不分开，后面一旦遇到 `SVC` 明明加了、最终还是缺变体，就很容易把问题误判成"Unity 又失灵了"，而不是继续往后看过滤与剔除层。

---

## 五、过滤与剔除阶段：不是所有候选都会活着走到正式编译

到了这一步，构建系统已经知道"有哪些路径值得讨论"，但这仍然不等于它们都会留下来。

从工程排查的角度，最关键的区分是两种完全不同的死亡方式：

1. `它根本没进入普通候选集`
2. `它进入候选后又被后面的 stripping 删掉`

这两种情况看起来都像"最后没有这个 variant"，但排查手段不同。

### 1. 更早的配置过滤和 URP Prefiltering

这是很多项目最容易漏掉的一层。

在 URP 项目里，当前生效的管线配置、`Renderer Feature`、平台能力、图形 API、质量档等，可能会先把一整段路径判成：`这次构建里根本不可能发生`——这意味着某些 variant 甚至不会走到后面常说的普通 stripping 逻辑里。

URP 的 `Decal Layers` 就是一个典型案例：

- `Shader` 侧确实声明了相关路径
- 项目里也可能有材质或 `SVC` 记录了对应 keyword
- 但如果本次构建真正生效的 `URP Asset / Renderer Feature` 没把相关功能打开
- URP 可能在更早的预过滤阶段就把这条路径视为"不可能发生"

这时现场看起来就会像：`明明在 SVC 里，怎么还是没留下来`——其实不是 `SVC` 无效，而是它补进来的路径没能越过更前面的配置真实性判断。

### 2. Unity 内置 stripping

如果路径已经进入普通候选，Unity 还会继续做内置的 stripping：

- 删掉当前配置下明确不需要的变体
- 压缩理论空间和真实目标之间的差距
- 避免把大量永远不会命中的组合直接带进交付物

### 3. SRP stripping

SRP 自己也会在 Unity 内置逻辑之外继续裁剪。它更了解自己管线的功能开关、平台限制和组合约束，所以它删掉的往往是：`当前这条渲染管线在这次构建配置下明确不会走到的路径`。

### 4. 项目自定义 IPreprocessShaders

最后才轮到团队自己的自定义 stripping 规则。

这一层是项目治理能力最强、也最危险的一层：好处是你能把项目内永远不会发生的组合明确写成规则，风险是很容易用局部经验删掉未来才会需要的路径。

工程上最稳的思路不是一上来在这一层猛砍，而是先确认前面几层已经把"客观不可能"的东西自然滤掉。

### 5. 这一阶段的核心结论

`SVC 决定的是"有没有资格进候选面"，剔除层决定的是"进来之后还能不能活着出去"。`

这两者不是同一件事，也不能互相替代。

---

## 六、交付阶段：活下来的 variant 要被写进正确的交付边界

很多现场说"包里明明有 shader"时，问题其实已经不在"有没有留下来"，而在：`它最后到底被谁带进了目标交付物。`

### 1. Player 视角下的"留下来"

如果一条路径最终由 `Player` 全局负责，那它的含义更接近：`目标包体的全局图形环境里存在这份 shader 能力`——运行时很多对象只是在引用它，而不是自己再单独携带一整份可用结果。

### 2. AssetBundle 视角下的"留下来"

一旦进入 `AssetBundle` 或多包协作场景，"留下来"的含义就会变得更具体：`这次由哪个交付物实际带着它，谁在承担它的存在责任`。

你必须区分：

- bundle 只是引用某份全局 shader 能力
- 还是 bundle 自己负责带齐对应路径

这也是为什么同一条 shader 路径，在纯 `Player` 场景里正常，换到 bundle 场景里问题就会突然爆出来。

### 3. Always Included 为什么经常看起来像一键修复

当 `Always Included` 介入时，很多"bundle 加载后显示不对"的问题会暂时消失，原因通常不是它神奇地创造了新 variant，而是：`它把交付责任粗暴地改成了由 Player 全局兜底。`

所以它经常能"修好现场"，但代价往往是：包体更大、全局责任更重、后面更难判断到底哪些内容真的需要被精确保留。

### 4. 写进交付物的并不是抽象 keyword，而是平台相关程序数据

从资产视角看，大家口头上都在说 `keyword`、`variant`、`shader`。但到了真正交付和运行时消费这一层，系统处理的已经不是抽象概念，而是：

`某个 shader 的某个 pass，在某组 keyword 条件下，对应到目标平台的一份可用程序数据。`

这也是为什么"看到 shader 资产在包里"本身不能证明运行时一定能命中到你预期的那条路径。

---

## 七、运行时使用阶段：CPU 不是直接拿 shader 资产去喂 GPU

运行时链路的关键点在于：`GPU 消费的不是"一个 shader 文件"，而是"当前 draw call 最终命中的那份平台程序"。`

### 1. 运行时先要决定这次 draw call 用哪个 pass、哪组 keyword 状态

当 CPU 提交一个 draw call，它需要基于当前材质状态、pass、全局与局部 keyword，去找一条最合适的 variant。真正被命中的对象是：某个 shader、某个 pass、某组当前生效的 keyword 状态、在目标平台上的具体程序。

运行时不是在重新发明一条 variant，而是在已经留下来的结果里做命中——所以如果构建期根本没留下来，运行时再聪明也变不出来。

### 2. 运行时不一定要求精确命中，可能会退化到最近似路径

这一步非常重要，因为它直接解释了很多"效果不对但材质没粉"的现场。

如果精确 variant 不存在，Unity 并不总是立刻报错或变粉，它可能会退化到一条"还能跑"的最近似路径。于是你会看到：材质没有粉、Draw call 正常提交了、但光照、阴影、开关效果、贴花、发光、额外 pass 等结果和预期不同。

这不是"运行时随机出错"，而是：`命中到了 fallback variant，而不是你真正想要的那条。`

### 3. 命中了，也不等于第一次就没有代价

即使正确 variant 已经在包里，第一次真正使用它时，也仍然可能出现一次准备成本。这就是为什么首次进场景卡一下、第一次出现某个特效卡一帧、后面重复出现就恢复正常——这种现象往往不是"变体不存在"，而是：`变体存在，但对应的平台程序还没有提前准备好。`

### 4. WarmUp 站在这条链的最后端

`WarmUp` 特别容易被误放到前面，但它回答的不是 `这条路径有没有留下来`，而是：`这条已经存在的路径，要不要在真正首用之前先准备好`。

所以 `WarmUp` 的前提一定是：

- 这条路径已经被正确保留
- 它已经被正确写进目标交付边界
- 运行时也确实会走到它

如果这三个前提里任何一个不成立，`WarmUp` 都不是解法。`SVC` 和 `WarmUp` 在这里解决的是"提前准备"，不是"凭空补出不存在的 variant"。

---

## 八、为什么丢了 Shader Variant，显示结果有时会错、有时会粉、有时只是首帧卡

这一节单独拎出来，是因为它直接对应项目现场最常见的误判。

很多人把下面三种现象都笼统叫成"丢 variant 了"，但它们对应的链路位置并不一样。

### 1. 粉材质或关键 pass 彻底失效：通常是没有任何可接受路径

当运行时找不到任何可接受的 variant，或者关键 pass 根本没有可用程序时，才更容易出现粉材质、Error Shader、关键 pass 彻底不工作。

这种情况通常优先怀疑：这条路径根本没有被保留进交付物，或者交付责任配错了，目标运行环境里根本拿不到它。

### 2. 画面不对但不粉：通常是退化命中了 fallback variant

你明明看到物体还能画出来，于是第一反应会觉得 `那应该不是 variant 的问题吧`——其实恰恰相反。

如果精确 variant 缺失，但系统还能找到一条最近似的 fallback 路径，表面结果就会变成：物体还在、shader 也没报错、但功能开关不对、额外效果没生效、阴影或贴花结果异常。

这类问题最危险，因为它不够"炸裂"，所以经常被误归类成美术资源问题、参数没配对、某个平台精度差异——实际上完全可能是：`精确 variant 缺了，运行时只好退到一条能跑但不正确的近似路径。`

### 3. 首帧卡顿但之后正常：通常是 variant 在包里，但未提前准备

这一类不是"缺失"，而是"准备时机太晚"。如果变体已经保留下来，运行时也能正确命中，只是第一次真正触发这条路径时才去加载或准备平台程序，就会出现首帧卡顿。

把三种现象放在一起：

- `粉` 更像是"没有任何可接受路径"
- `显示不对` 更像是"命中到退化路径"
- `第一次卡` 更像是"路径在，但准备发生得太晚"

把这三种现象分开，排查才不会一上来就把所有责任都压到 stripping 上。

---

## 九、项目里怎么确认问题到底死在哪一关

真正排查时，不要从自己最熟悉的工具开始，而要按生命周期倒查。

最短的问题顺序通常是：

1. `这条路径在 Shader 理论空间里到底有没有被声明出来`
2. `这次构建有没有真实输入在为它提供保留依据`
3. `它是不是在 URP/SRP 的更早配置过滤里就被判成不可能`
4. `它是不是进入候选后又被 builtin / SRP / custom stripping 删掉`
5. `它最后到底写进了 Player 还是 Bundle，交付责任配对了吗`
6. `运行时是完全没找到、退化命中，还是只是第一次准备太晚`
7. `如果路径存在，是否只是首用时机没准备好`

如果需要证据链，最常用的抓手通常是：

- `Editor.log`：看构建侧到底留下了多少 variant，以及某些关键 shader 的构建统计
- `Strict Shader Variant Matching`：把"模糊退化"变成更明确的缺失信号
- `Log Shader Compilation`：看运行时是否第一次才开始准备平台程序
- `Frame Debugger`：确认当前 draw call 实际走了哪个 pass 和哪条路径
- `OnProcessShader` 日志：只证明"它有没有走到这层"，不能证明它更早没被预过滤

这里最重要的一条经验是：

`如果某条路径根本没进入你的自定义 stripping 回调，不要立刻得出"不是剔除问题"的结论，更可能是它死在更早的 Prefiltering 或构建输入阶段。`

---

## 十、团队应该怎样使用这条全流程视角

这篇总览文的目标不是替代细分文章，而是给整个系列建立统一坐标。

后面你们再看任何局部话题，都可以先问它在生命周期里属于哪一站：

- 如果讨论的是 `Material / 场景 / SVC / Always Included`，本质上是在讨论 `资产定义层的保留依据与交付责任`
- 如果讨论的是 `URP Prefiltering`、内置 stripping、自定义 stripping，本质上是在讨论 `构建产物层的过滤与剔除`
- 如果讨论的是 `SetPass`、fallback、`WarmUp`、首次卡顿，本质上是在讨论 `运行时消费层的命中与准备`
- 如果讨论的是 `AssetBundle` 明明有 shader 却显示不对，本质上通常是在讨论 `交付边界和运行时命中` 的组合问题

从治理角度，这条线最后应该沉淀的不是一个"万能开关"，而是几类长期资产：

- 哪些变体来源维度在持续制造规模
- 哪些路径是项目显式关心的保留面
- 哪些规则在稳定地剔除"不可能发生"的组合
- 哪些关键场景和关键效果必须被持续回归
- 哪些现象属于缺失，哪些属于退化命中，哪些属于未预热

---

## 十一、把全文压成一句结论

最后把整篇压成几句最值得记住的话：

1. `Shader / Material / SVC` 是资产定义，不是 GPU 最终消费对象。
2. 真正决定设备侧能不能用到某条路径的，是构建期保留下来的平台相关程序结果。
3. `Always Included` 解决的是交付责任边界，`SVC` 解决的是显式保留与预热清单，它们不在同一层。
4. 运行时只能命中"已经留下来的东西"，不能凭空救回构建期已经消失的 variant。
5. GPU 消费链的正确心智模型，不是"shader 在不在包里"，而是：

`这条路径有没有被资产层表达出来，有没有被构建层留下来，有没有被交付层带到目标环境里，最后有没有被运行时正确命中。`

只要链路视角是对的，下面这些现场问题就不会再混成一团：

- 为什么项目里有材质，这次构建却没留下对应路径
- 为什么 `SVC` 里有记录，URP 还是能提前把它判掉
- 为什么 `Always Included` 看起来像修好了问题，但代价很粗
- 为什么有时会粉，有时只是画面不对，有时只是第一次卡

后面每一篇细分文，其实都只是这条链上的某一站。
