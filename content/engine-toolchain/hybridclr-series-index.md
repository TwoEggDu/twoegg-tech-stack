---
date: "2026-03-26"
title: "HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇"
description: "给 HybridCLR 系列先补一个总入口：推荐阅读顺序、按问题回看路径、公共前置文章，以及已经开始往下延伸的高级主题。"
slug: "hybridclr-series-index"
weight: 29
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Index"
  - "Architecture"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
series_id: "hybridclr"
series_role: "index"
series_order: 0
series_nav_order: 30
series_title: "HybridCLR"
series_audience:
  - "Unity 客户端"
  - "热更新 / 工具链"
series_level: "进阶"
series_best_for: "当你想把 HybridCLR 从 build-time、runtime 到排障链路一起看清"
series_summary: "把 HybridCLR 的 build-time、runtime、AOT 泛型和排障链路收进一张入口图"
series_intro: "这组文章不是只讲某个按钮或单点 API，而是把 HybridCLR 从运行时主链、元数据、AOT 泛型到工具链、排障和项目落地拆成可顺读的一条线。先看入口页再沿着主线前进，能更快知道当前问题属于哪一层。"
series_reading_hint: "第一次进入系列时，建议按目录顺序顺读主线；遇到具体报错时，再按问题入口跳回对应文章。"
---
> 这组文章如果一篇篇单看，其实都能成立；但 HybridCLR 真正难的地方，不在某一篇单文，而在于你能不能先知道自己现在碰到的问题到底属于哪一层。

这是 HybridCLR 系列第 0 篇。  
它不讲新的源码细节，只做一件事：

`给这组文章补一个稳定入口，让读者知道先读哪篇、遇到什么问题该回看哪篇。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 这组文章现在已经覆盖了哪些主题。
2. 如果按阅读顺序看，最稳的路径是什么。
3. 如果不是系统读，而是项目里遇到具体问题，该先回看哪篇。
4. 这组文章接下来还准备补什么。

## 先给一句总判断

如果先把整个系列压成一句话，我会这样描述：

`HybridCLR 不是一个“热更新功能点”，而是一条从 build-time 到 runtime 的系统链路；这组文章的任务，就是把这条链按层拆开，再在项目层重新收回来。`

所以这组文章故意不是按源码目录平铺，而是按问题拆：

- 主链怎么跑起来
- AOT 泛型为什么会出问题
- 菜单和生成物到底在干什么
- 资源挂载 MonoBehaviour 为什么能成立
- 一条真实调用链怎么进解释器
- 边界和 trade-off 到底在哪
- 项目里该怎么落地

## 建议先补一个公共前置

这组文章虽然会在必要处补最小 `IL2CPP` 背景，但它本身不是一组 `IL2CPP` 入门文章。  
如果你现在对下面这些问题还没有稳定直觉：

- `global-metadata.dat` 到底是什么
- `GameAssembly` 和 `libil2cpp` 分别负责什么
- 为什么“看得见 metadata”不等于“调得到实现”

那我建议先补这篇公共前置：

- [IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})

如果你还不清楚 Unity 编辑器里脚本是怎么编译的（asmdef、Roslyn、bee_backend、ILPP 各做什么），建议再补这个系列：

- [Unity 脚本编译管线系列索引｜从改一行代码到编辑器可用，中间发生了什么]({{< relref "engine-toolchain/unity-script-compilation-pipeline-series-index.md" >}})

它不属于 HybridCLR 系列编号本身，但会把后面 `补 metadata`、`AOT 泛型`、`MethodBody`、`MethodBridge` 这些话题需要的底座先立住。

## 推荐阅读顺序

如果你准备第一次系统读，我建议就按下面这个顺序：

0. [HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
   先建地图，不然很容易把后面的概念看散。

1. [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
   先把总地图和 runtime 主链立住。

2. [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})
   把最容易踩坑、也最容易和“补 metadata”混掉的部分拆开。

3. [HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})
   把 Editor 菜单从“按钮列表”拉回 build-time 因果链。

4. [HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}})
   这是最容易被低估的一篇，因为它讲的是资源反序列化和程序集身份链。

5. [HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})
   到这里再顺着真实调用链跑一遍，前面的概念会更扎实。

6. [HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}})
   把几条主线重新收成一张边界图。

7. [HybridCLR 最佳实践｜程序集拆分、加载顺序、裁剪与回归防线]({{< relref "engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md" >}})
   最后再回到项目落地，回答“怎么长期不出事”。

8. [HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}})
   这一篇不再加新原理，而是把前面几篇重新变成可用的排障工具。

9. [HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}})
   这是第一轮主线的收束篇，把性能问题收回工程判断，不写空泛 benchmark，而是讲首调、常驻热点和跨边界成本。

10. [HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版]({{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}})
   这是第二轮高级篇的第一篇，专门解释为什么它改的是 generic 调用模型，不是 metadata 可见性。

11. [HybridCLR DHE｜为什么它不是普通解释执行更快一点]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}})
   这是第二轮高级篇的第二篇，专门解释为什么它改的是 AOT 与 interpreter 之间的函数级分流，不是单纯把解释器调快。

12. [HybridCLR 高级能力选型｜社区版主线、补 metadata、Full Generic Sharing、DHE 分别该在什么时候上]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}})
   这是当前阶段最适合收口的一篇，把前面几条线重新收回项目选型判断。

13. [HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断]({{< relref "engine-toolchain/hybridclr-faq-10-most-confused-judgments.md" >}})
   这是一篇 FAQ 入口，不补新原理，专门把最容易说错的 10 句话重新拉直。

14. [HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出，一次完整的 native crash 符号化分析]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}})
   这一篇开始进入真实项目案例，把 `TypeLoadException`、真机崩溃和符号化分析串成一条完整排查链。

15. [HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}})
   这一篇把调用栈阅读方法单独展开，说明 `hybridclr::`、AOT 泛型缺失、MethodBridge 缺失和 metadata 不匹配各自会留下什么特征。

16. [HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})
   这一篇把案例里暴露出来的工程问题单独收束，解释为什么 `GenerateAll`、`Development` 一致性和 `Launcher-only` 场景必须进入构建流程。

17. [HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})
   这一篇沿着 HCLR-14 的崩溃链继续下钻，把“`RefMethods()` 是空的”和“真正根因是什么”区分开，并给出根治与应急两条修法。

18. [HybridCLR 案例｜Dictionary<ValueTuple, 热更类型> 的 MissingMethodException 与 object 替代法]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}})
   这是另一类 AOT 泛型缺口案例，专门讲清为什么值类型键和热更引用类型值组合在一起时，`DisStripCode` 里只能用 `object` 兜底。

### AOT 泛型工程化子系列（HCLR-19 ~ HCLR-24）

> 下面 6 篇围绕同一个主题展开：AOT 泛型问题从"怎么修"到"怎么不再出"。如果你只是想快速查修法，直接看 HCLR-19；如果你想把防线建进 CI，直接跳到 HCLR-24。

19. [HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})
   把三种修法从"都试一遍"拉回决策树：哪个解决构建期问题，哪个解决运行时问题，哪个补性能。

20. [HybridCLR 共享类型判断｜为什么报错是 ValueTuple<int,string>，你写的却可能是 ValueTuple<int,object>]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})
   拆清 IL2CPP 泛型共享规则：引用类型共享 `object`，值类型独立实例，DisStripCode 该写哪个。

21. [HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})
   按 4 种模式给出 DisStripCode 的具体写法和背后的 IL2CPP 共享规则。

22. [HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})
   把三者的因果关系收成一条链：AOTGenericReferences 发现缺口 → DisStripCode 保实例 → 补 metadata 加可见性。

23. [HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}})
   按 5 类高频场景归档坑型，每类给出根因和修法。

24. [HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}})
   把判断前移到工程流程，附 CI 检查脚本示例。

## 如果你不是系统读，而是带着问题来查

如果你已经在项目里遇到问题，那比起从头读，更稳的是按问题回看。

### 1. 你想先知道 HybridCLR 到底把什么补进了 IL2CPP

先看：

- [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})

### 2. 你遇到的是 AOT 泛型、补 metadata、`AOT generic method not instantiated` 一类问题

先看：

- [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})

### 3. 你想知道菜单按钮、`Generate/All`、`MethodBridge`、`AOTGenericReference` 到底在生成什么

先看：

- [HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})

### 4. 你遇到的是资源上挂热更脚本、Prefab/Scene 反序列化、程序集身份链问题

先看：

- [HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}})

### 5. 你想跟一条真实调用链，确认 `MethodInfo.Invoke` 最终为什么会掉进解释器

先看：

- [HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})

### 6. 你发现自己把几种能力和代价混成一件事了

先看：

- [HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}})

### 7. 你已经懂原理，但想知道项目里怎么组织才稳

先看：

- [HybridCLR 最佳实践｜程序集拆分、加载顺序、裁剪与回归防线]({{< relref "engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md" >}})

### 8. 你已经在线上或测试环境报错，想先快速判断属于哪一层

先看：

- [HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}})

### 9. 你已经把主链和排障都看完了，想知道性能到底该怎么治理

先看：

- [HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}})

### 10. 你发现补 metadata 还是不够，想知道 Full Generic Sharing 到底补的是哪一层

先看：

- [HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版]({{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}})

### 11. 你想直接改现有 AOT 模块，又不想让没改的函数全退回解释执行

先看：

- [HybridCLR DHE｜为什么它不是普通解释执行更快一点]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}})

### 12. 你已经把几条线都看过了，想知道项目里到底该选哪条路

先看：

- [HybridCLR 高级能力选型｜社区版主线、补 metadata、Full Generic Sharing、DHE 分别该在什么时候上]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}})

### 13. 你不是缺新知识点，而是总把几种判断混成一件事

先看：

- [HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断]({{< relref "engine-toolchain/hybridclr-faq-10-most-confused-judgments.md" >}})

### 14. 你已经拿到一条 `TypeLoadException`、真机 `SIGSEGV` 或 native crash，想看一次完整的 HybridCLR 案例诊断是怎么推进的

先看：

- [HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出，一次完整的 native crash 符号化分析]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}})

### 15. 你已经拿到 native crash 调用栈，想读懂里面的 `hybridclr::`、AOT 泛型缺口、MethodBridge 缺失和 metadata 不匹配

先看：

- [HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}})

### 16. 你想把 HybridCLR 的打包流程做成可重复的 CI 标准流程

先看：

- [HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})

### 17. 你在线上或 CI 遇到 async 崩溃，想知道真正的根因和修法

先看：

- [HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})

### 18. 你遇到 `Dictionary<ValueTuple, 热更类型>` 的 `MissingMethodException`，或者想知道 DisStripCode 里为什么只能用 object 替代热更类型

先看：

- [HybridCLR 案例｜Dictionary<ValueTuple, 热更类型> 的 MissingMethodException 与 object 替代法]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}})

### 19. 你拿到报错后不确定该用 DisStripCode、link.xml 还是补 metadata

先看：

- [HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})

### 20. 你看到 AOTGenericReferences 里的类型和你源码里写的不一样，想知道 IL2CPP 共享规则

先看：

- [HybridCLR 共享类型判断｜为什么报错是 ValueTuple<int,string>，你写的却可能是 ValueTuple<int,object>]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})

### 21. 你需要写 DisStripCode 但不知道值类型、嵌套泛型、委托各自怎么写

先看：

- [HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})

### 22. 你不确定 AOTGenericReferences、DisStripCode、补 metadata 三者到底怎么配合

先看：

- [HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})

### 23. 你遇到 UniTask、LINQ、Dictionary、委托相关的 AOT 泛型坑，想快速定位属于哪种类型

先看：

- [HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}})

### 24. 你想把 AOT 泛型问题前移到 CI 和构建检查里，不想每次都线上排

先看：

- [HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}})

## 这组文章刻意不做什么

为了让这组文章保持收敛，我刻意没把它写成下面几种形态：

- 不是接入教程
- 不是完整 API 手册
- 不是逐文件平铺的源码索引
- 不是最小可运行 demo 教程

它更像一套：

`先把系统地图立起来，再把高频问题拆开，再把工程判断收回来的源码导读系列。`

## 目前已经覆盖到哪一层

如果只按能力层来分，这组文章现在已经覆盖了：

- build-time 工具链
- 动态程序集装载
- 补充 metadata
- 方法执行主链
- MonoBehaviour 资源挂载身份链
- 边界和 trade-off
- 工程落地 best practice
- runtime generic 调用模型往下的一条高级扩展轴：`Full Generic Sharing`
- 已进包 AOT 程序集的函数级差分执行：`DHE`
- 面向真实项目的高级能力选型判断
- 一篇面向高频混淆点的 FAQ 入口
- 真实案例诊断：从 `TypeLoadException` 到 async 栈溢出的完整排查链
- native crash 调用栈定位：`hybridclr::`、AOT 泛型缺失、MethodBridge 缺失、metadata 不匹配
- CI 打包工程化：GenerateAll、Development 一致性、Launcher-only 场景
- 真实崩溃案例续篇：async 崩溃根因与两种修法
- 真实案例：`Dictionary<ValueTuple, 热更类型>` 的 `MissingMethodException` 与 `object` 替代法
- AOT 泛型修法决策：DisStripCode / link.xml / 补 metadata 的选择依据
- IL2CPP 泛型共享规则：引用类型共享 `object`，值类型独立实例
- DisStripCode 写法手册：值类型、引用类型、嵌套泛型、委托 4 种模式
- AOTGenericReferences / DisStripCode / 补 metadata 三者协作因果链
- AOT 泛型高频坑型录：UniTask、LINQ、Dictionary、委托、自定义容器
- AOT 泛型回归防线：Generate 触发条件、CI 检查脚本、线上止血与下版本根治分工

到这一步，这组文章从基础原理、高级能力、真实案例到 AOT 泛型的完整工程化闭环都已经收齐。

## 收束

HybridCLR 这组文章最重要的价值，不是把知识点列齐，而是帮你在真正打开源码或排查项目问题之前，先知道自己应该走哪条阅读路径。

## 系列位置

- 上一篇：无。这是系列入口。
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}}">HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute</a>


