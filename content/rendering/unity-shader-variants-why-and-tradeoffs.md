---
date: "2026-03-24"
title: "Unity Shader Variants 为什么会存在，以及它为什么总让项目变复杂"
description: "解释 Unity 为什么引入 Shader Variants、它带来的工程问题，以及 Unreal 在 Shader Permutations 上的对应做法。"
slug: "unity-shader-variants"
weight: 10
featured: false
tags:
  - "Unity"
  - "Unreal"
  - "Rendering"
  - "Shader"
series: "Unity Shader Variant 治理"
---
做 Unity 图形开发的人，迟早都会碰到一个词：`Shader Variants`。

它通常不是单独出现的，而是和这些现象绑在一起：

- 打包时间越来越长
- 首次进场景偶发卡顿
- 某些材质在特定平台上效果不对
- 明明只写了一个 Shader，最后却编出来一大堆版本

很多人第一次接触它时，都会有一个直觉上的疑问：

`不就是一个 Shader 吗，为什么最后会变成那么多“变体”？`

如果只从“语法”去理解 Shader，这件事确实很奇怪。但如果从引擎、GPU 和跨平台渲染链路去看，Shader Variants 其实是一个很典型的工程折中。

## 一、Shader Variants 到底是什么

可以先把一个 Shader 理解成一份“总模板”。

这个模板里可能有很多功能开关，比如：

- 是否启用法线贴图
- 是否接收阴影
- 是否参与雾效
- 是否走 Lightmap 路径
- 是否启用某种额外的后处理或材质效果

从代码角度看，这些事情似乎都可以用 `if` 在运行时判断。

但对 GPU 来说，很多分支如果能在编译期就提前确定，最终生成的程序通常会更高效、更稳定，也更容易被不同平台的图形驱动优化。

所以 Unity 的做法不是把所有情况都留到运行时再判断，而是提前把不同条件组合编译成多个版本。每一个具体版本，就是一个 `Shader Variant`。

换句话说：

`Shader Variants 本质上是同一份 Shader 源码，在不同 keyword、不同 pass、不同平台条件下生成的多个编译结果。`

这也是为什么一个 Shader 看起来只有一份代码，最后却可能对应成百上千个变体。

## 二、Unity 为什么要引入 Shader Variants

Unity 引入这套机制，不是为了把事情搞复杂，而是因为它要解决几个非常现实的问题。

## 1. 为了把运行时分支尽量前移到编译期

如果一个 Shader 把所有功能都留到运行时动态判断，代价通常会是：

- 分支变多
- 指令膨胀
- 寄存器压力变大
- 某些平台上的驱动优化效果变差

所以从运行时性能角度看，提前针对不同功能组合编译出专门版本，是合理的。

这也是 Variants 最核心的价值：

`用编译期复杂度，换运行时效率。`

## 2. 为了适配真实项目里的渲染状态组合

项目里的渲染条件，远不只是“贴一张图画出来”。

同一个材质在不同情况下，可能要面对：

- Forward / Deferred 路径差异
- ShadowCaster / DepthOnly / Meta 等不同 Pass
- 主光、附加光、Lightmap、SH、Reflection Probe
- Fog、Instancing、LOD Fade、Probe 或其他管线特性
- Built-in、URP、HDRP 各自不同的编译条件

这些差异里，有很多不是简单改几个数值，而是会直接改变 Shader 代码结构。

既然代码结构不同，Unity 就更倾向于把它们拆成不同变体，而不是在运行时临时做所有判断。

## 3. 为了跨平台

Unity 要面对的不是单一图形 API，而是一整个跨平台矩阵：

- DirectX
- Vulkan
- Metal
- OpenGL / OpenGL ES
- PC、主机、移动端等不同硬件环境

不同平台对 Shader 编译、优化和支持能力并不一样。对 Unity 这种通用引擎来说，提前生成不同条件下的编译结果，是更现实的做法。

所以 Unity 引入 Shader Variants，本质上不是“多此一举”，而是在兼顾：

- 运行时性能
- 渲染灵活性
- 跨平台兼容

## 三、为什么这套机制后来会变成问题

问题不在于 Variants 存在，而在于它非常容易失控。

## 1. 最大的问题是组合爆炸

只要 Shader 里有多个关键字开关，变体数量就会指数增长。

比如一个 Shader 有 5 个布尔开关，理论上就可能有 `2^5 = 32` 个组合。再叠加：

- Pass 数量
- 光照模式
- 平台差异
- 图形 API 差异
- 渲染管线额外 keyword

最后数量会膨胀得非常快。

这也是为什么很多项目明明只有几十个核心 Shader，最后构建日志里却能看到成千上万甚至更夸张的 variant 数量。

## 2. 编译时间、导入时间和构建时间都会变长

Variants 一多，Unity 在多个阶段都要付成本：

- 导入 Shader 时要处理更多编译结果
- 切平台时要重新准备对应结果
- Build 时要做收集、过滤和打包
- 某些情况下运行时还会补加载或补编译

于是项目里最直观的体感就是：

`Shader 改一点点，等很久。`

当项目规模上来之后，这已经不再是“图形同学自己的问题”，而是会直接影响整个研发生产效率。

## 3. 包体、内存和加载成本会上升

即使 Unity 做了 stripping，项目里仍然可能保留大量真正会被打进包里的 variant。

这些内容会继续带来几个问题：

- 包体更大
- 加载时需要准备更多 Shader 数据
- 运行时更容易出现首次使用卡顿
- 某些平台上内存压力更明显

特别是在移动端，这些问题通常会比 PC 端更敏感。

## 4. 很多问题都不容易被第一时间发现

Shader Variants 麻烦的一点，是它的很多问题都不是“代码当场报错”，而是会以很绕的形式出现：

- 某个 variant 根本没生成
- 某个 variant 被错误 strip 掉了
- 编辑器正常，真机异常
- 第一次看到某个特效时突然卡一下
- 某个平台、某个质量档位才出问题

这意味着 Variants 的问题很少是单纯 API 级别的问题，它更像是：

`渲染系统、构建系统和项目配置共同作用下的工程问题。`

## 5. Unity 把这部分复杂度暴露给了项目方

Unity 提供了 `shader_feature`、`multi_compile`、stripping、collection、warmup 等一整套机制，但也意味着项目要自己承担很多管理责任：

- 哪些开关真的值得做成 keyword
- 哪些组合其实永远不会在项目里出现
- 哪些 variant 必须保留
- 哪些 variant 应该被裁掉
- 哪些高频路径需要提前预热

所以项目越大，Shader Variants 越容易从“引擎机制”升级成“治理问题”。

## 四、那 Unreal 是什么情况

很多人讲到这里会追问一句：

`既然 Unity 的 Shader Variants 这么麻烦，那 Unreal 是不是就没有这个问题？`

答案是：没有这么简单。

Unreal 也在解决同一类问题，只是它的术语、工作流和工程配套不同。

## 1. Unreal 也有自己的 permutations

在 Unreal 里，对应的概念更常见的名字是：

- Shader Permutations
- Material compile permutations
- Static Switch Parameters 带来的编译分支

也就是说，Unreal 并不是“没有变体”，而是也会因为：

- 材质开关
- Pass 差异
- 平台差异
- 渲染路径差异

生成大量不同编译结果。

所以本质上，Unity 的 Variants 和 Unreal 的 Permutations，解决的是同一个问题：

`如何把高性能需要的编译期裁剪，和项目里复杂的渲染功能组合，放进一套可工作的工程体系里。`

## 2. Unreal 更强调把高频变化留给参数，而不是留给静态开关

Unreal 的 Material Instance 工作流相对更成熟，很多经常变化的内容都会尽量做成实例参数，而不是做成会触发重新编译的静态开关。

这背后的思路很明确：

- 颜色、强度、贴图权重之类的变化，尽量留给运行时参数
- 真正会改变代码路径的内容，再使用 Static Switch 或 permutation

这么做的好处是，很多美术调整不会直接放大 permutation 压力。

但这不代表 Unreal 没有编译成本。一旦 Static Switch、Landscape、Layer 或平台差异组合变多，Unreal 同样会出现非常重的 Shader 编译压力。

## 3. Unreal 更像是在工具链层面对这件事做了重配套

和 Unity 相比，Unreal 在这类问题上的体感差异，更多来自工程配套，而不是“没有 permutations”。

常见的配套手段包括：

- Shader Compile Workers
- Derived Data Cache（DDC）
- 更重的材质实例体系
- PSO Cache / PSO 预热

所以很多人会觉得 Unreal 也慢，也复杂，但它更像是默认接受“这件事本来就重”，然后围绕它建立了一整套工具链和缓存体系。

## 4. 两边的核心差别，不在有没有，而在复杂度暴露给谁

可以很粗地概括成这样：

- Unity 更轻，很多变体管理责任直接落到项目和开发者身上
- Unreal 更重，更多通过材质系统、编译缓存和运行时缓存把复杂度包进工具链里

这不是说谁绝对更好，而是谁把成本放在了不同位置。

Unity 更像是：

`我给你机制，你自己治理。`

Unreal 更像是：

`我承认这件事本来就重，所以我给你一整套更重的工程配套。`

## 五、怎么理解这个问题才比较准确

如果只把 Shader Variants 理解成“Unity 的坑”，很容易看偏。

更准确的理解应该是：

- 现代实时渲染需要大量编译期裁剪
- 编译期裁剪天然会带来排列组合
- 排列组合一多，就一定会变成工程复杂度问题

所以 Shader Variants 不是一个孤立的图形概念，它其实是一个典型的引擎工程折中。

Unity 选择的是：

`用更多变体管理成本，去换跨平台和运行时性能。`

而 Unreal 选择的是：

`接受 permutation 不可避免，再用更重的材质体系和工具链去承载。`

两边面对的是同一个矛盾，只是工程组织方式不同。

## 官方文档参考

- [Shader variants and keywords](https://docs.unity3d.com/Manual/shader-variants-and-keywords.html)

## 六、总结

最后可以把这件事压缩成三句话：

1. Unity 引入 Shader Variants，是因为它需要把很多渲染分支提前到编译期，以换取运行时性能、灵活性和跨平台兼容。
2. 它带来的最大问题，不是“有变体”，而是变体数量太容易组合爆炸，最后拖慢构建、放大包体、增加卡顿和管理复杂度。
3. Unreal 也有同类问题，只是更多通过 Material Instance、Shader Permutation 配套、DDC 和 PSO Cache 去系统化承受它。

所以真正值得问的，不是“为什么 Unity 要搞 Shader Variants”，而是：

`当一个引擎既要高性能、又要跨平台、还要支持复杂渲染功能时，它准备把复杂度放在哪一层。`

至于 Unity 项目里到底该怎么治理 Shader Variants，这件事更适合单独写成一篇工作流文章。