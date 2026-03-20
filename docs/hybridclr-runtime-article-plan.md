# HybridCLR 文章规划

## 定位

这篇文章不是接入教程，也不是源码文件索引。

它的目标是：

`用一条真实的调用链，把 HybridCLR 从 Editor 生成物、metadata 装载、方法体解析，一路讲到 transform 和解释执行。`

读者看完之后，应该至少得到三样东西：

- 知道 HybridCLR 到底往 IL2CPP 里补了什么能力
- 知道菜单按钮为什么存在，它们分别在为 runtime 准备什么
- 能顺着文章自己进源码，看懂主链上关键函数在干什么

同时，这篇文章会在必要处补一段最小 IL2CPP 背景，帮助读者先搞清楚：

- HybridCLR 到底是补在谁身上的
- IL2CPP 原本天然擅长什么
- IL2CPP 原本天然不擅长什么

## 风格约束

按站点现有 `engine-notes` 和 `problem-solving` 的写法，这篇要保持下面这些特征：

- 开头先用一句话压缩全文判断
- 先建地图，再进细节
- 以问题驱动，而不是以目录驱动
- 源码解释必须服务于主线，不做逐文件平铺
- 每节都要回答“这一层到底解决了什么问题”
- 结尾要有“把整条链压成一句话”和“常见误解”

这篇会比现有 `engine-notes` 更硬，但不改成论文风格。

## 文章定位

建议放到：

- `content/engine-notes/`

建议标题方向：

- `HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute`
- `HybridCLR 源码导读｜IL2CPP 热更新到底是怎么跑起来的`

更推荐第一种，兼顾搜索和阅读兴趣。

## 核心问题

全文围绕 4 个问题展开：

1. `IL2CPP` 明明是 AOT，为什么还能跑热更代码
2. `HybridCLR` 到底往 `libil2cpp` 里补了哪些能力
3. Editor 菜单里那几个生成步骤，到底在为 runtime 准备什么
4. 一个热更方法，最终是怎么走到 `Interpreter::Execute` 的

## 前置背景：IL2CPP 要讲到什么程度

这篇文章里必须介绍 IL2CPP，但只讲和 HybridCLR 直接相关的部分。

建议控制在一节短背景里，目标不是“讲懂 IL2CPP 全貌”，而是让读者先建立一个足够用的坐标系。

这一节只讲 4 件事：

1. Unity 托管代码大致经过什么路径
   - `C# -> IL -> il2cpp 转换 -> C++ -> native binary`
2. IL2CPP 的运行时本质上是什么
   - 它是 AOT runtime，不是默认可以在运行时再接新 IL 并现场执行的 JIT CLR
3. IL2CPP 运行时里至少有哪些和本文直接相关的东西
   - 类型
   - 方法
   - metadata
   - native method pointer
4. 为什么 HybridCLR 的问题空间天然会落在这里
   - 因为它要解决的正是“在一个纯 AOT 世界里，重新引入动态 metadata 和解释执行能力”

这节不展开的内容：

- 不系统讲 il2cpp 代码生成细节
- 不展开 Unity 整体构建流水线
- 不讲 GC、线程、反射实现的全貌
- 不把文章改写成 il2cpp 原理教程

一句话说，这部分 IL2CPP 背景只服务于一个目的：

`让读者先知道 HybridCLR 不是凭空工作，而是在一个原本偏静态、偏 AOT 的 runtime 上补动态能力。`

## 主线选择

正文不按目录讲，只追这一条主线：

`C# RuntimeApi -> native RuntimeApi -> metadata::Assembly -> MetadataModule / MethodBodyCache -> HiTransform -> Interpreter::Execute`

这条主线是全文骨架。

支线只在需要时插入：

- `Installer / Generate/*` 工具链
- `AOT 泛型`
- `MethodBridge / ReversePInvokeWrapper`
- `DHE / Full Generic Sharing` 只讲边界，不做正文主线

## 源码范围

正文主要用下面几组源码。

### C# 入口

- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Runtime\RuntimeApi.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Runtime\HomologousImageMode.cs`

### Editor 工具链

- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\PrebuildCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\CompileDllCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\LinkGeneratorCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\StripAOTDllCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\MethodBridgeGeneratorCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\AOTReferenceGeneratorCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Commands\Il2CppDefGeneratorCommand.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor\Installer\InstallerController.cs`

### Native runtime 主线

- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\Runtime.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\RuntimeApi.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\metadata\Assembly.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\metadata\MetadataModule.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\metadata\MethodBodyCache.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\metadata\AOTHomologousImage.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\transform\Transform.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\transform\TransformContext.h`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\interpreter\InterpreterModule.cpp`
- `E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr\interpreter\Interpreter_Execute.cpp`

## ECMA-335 使用策略

会引用，但只在必要处引用，不写成标准导读。

主要插入点：

- metadata / 表结构
  - `Partition II`
  - `MethodDef` / `MemberRef` / `TypeSpec` / `MethodSpec`
- method body
  - `II.25.4 Common Intermediate Language physical layout`
- 泛型实例化
  - `II.9 Generics`
  - `II.22.29 MethodSpec`
  - `II.22.39 TypeSpec`
- 调用和桥接
  - `I.12.4.1` 的 `call / callvirt / calli`

策略是：

- 先讲源码行为
- 再用 ECMA-335 做“规范锚点”
- 不反过来

## 正文结构

### 1. 开篇一句话

用 blockquote 定调。

目标是让读者在一开始就知道：

- HybridCLR 不是“加载 dll 就能跑”
- 它是在 `IL2CPP` 的 AOT runtime 里，补了一整条从 metadata 到解释执行的链路

### 2. 这篇要回答什么

沿用站点现有文章常见写法。

会明确这篇回答的就是那 4 个核心问题。

### 3. 先给一段最小 IL2CPP 背景

这一节会在正式进入 HybridCLR 之前，先把 IL2CPP 的基本坐标立住。

重点是：

- IL2CPP 不是什么
- 它为什么天然是 AOT
- 它为什么不会自动支持“运行时加载一份新的 IL 程序集并执行”

这里会明确告诉读者：

HybridCLR 不是绕开 IL2CPP，而是补进 IL2CPP。

### 4. 为什么这个问题值得先搞清楚

这一节的作用是防止读者一进源码就迷路。

核心会讲：

- 如果不先建地图，`metadata / transform / interpreter / MethodBridge / AOTGenericReference` 会看成五摊散点
- 这篇文章的目标不是列文件，而是建立因果关系

### 5. 先给源码地图

这一节先把项目里的三层边界立住：

- `Packages/HybridCLR/Editor`：工具链
- `Packages/HybridCLR/Runtime`：C# 入口
- `HybridCLRData/.../libil2cpp/hybridclr`：真正 runtime

这一节是文章进入源码前的坐标系。

### 6. 先看 Editor 工具链：运行时到底提前需要哪些输入

从 `PrebuildCommand.cs` 讲起。

顺序固定：

- `CompileDll`
- `Il2CppDef`
- `LinkXml`
- `AOTDlls`
- `MethodBridge`
- `AOTGenericReference`

这一节的重点不是菜单介绍，而是：

- 每一步生成了什么
- 这些产物最终被谁消费
- 缺哪一步 runtime 会出什么问题

### 7. HybridCLR 是怎么挂进 libil2cpp 的

从 `Runtime.cpp` 开始。

核心只抓两句：

- `RuntimeApi::RegisterInternalCalls();`
- `metadata::MetadataModule::Initialize();`

这节回答：

- HybridCLR 为什么不是“外部热更框架”
- 它是怎么成为 runtime 一部分的

### 8. C# RuntimeApi 调到 native 后，语义到底是什么

从 `RuntimeApi.cs` 到 `RuntimeApi.cpp`。

重点 API：

- `LoadMetadataForAOTAssembly`
- `PreJitClass`
- `PreJitMethod`

这一节会特别强调：

- `LoadMetadataForAOTAssembly` 不是“执行热更 dll”
- 它是在给 AOT 程序集补充同源 metadata 支撑

### 9. metadata 层到底干了什么

这是全篇最关键的一节。

会主要讲：

- `Assembly.cpp`
- `MetadataModule.cpp`
- `AOTHomologousImage.cpp`

核心对象：

- `InterpreterImage`
- `AOTHomologousImage`
- `MetadataModule`

这一节回答：

- 热更程序集如何进入 runtime
- AOT 补充元数据如何与已加载程序集关联
- runtime 如何区分哪些方法该交给 interpreter

### 10. 一个 MethodInfo 怎么找到 MethodBody

从 `MethodBodyCache.cpp` 切进去。

这里会专门结合 ECMA-335 讲：

- `MethodDef`
- method body header
- EH table
- `token -> MethodBody` 为什么是合理的组织方式

这一节回答：

- 解释器执行前，方法体是如何被定位和缓存的

### 11. 为什么 HybridCLR 不直接解释 IL，而要先 Transform

从 `Transform.cpp` 和 `TransformContext.h` 讲起。

不会平铺 `TransformContext.cpp`，只抓：

- 输入是什么
- 输出是什么
- 为什么原始 CIL 的栈机模型不适合直接执行
- 为什么要改写成 HybridCLR 自己的内部表示

这一节回答：

- transform 是执行性能和执行正确性中间那一层

### 12. Interpreter::Execute 到底怎么跑起来

从 `InterpreterModule.cpp` 和 `Interpreter_Execute.cpp` 讲。

会重点讲：

- `HiTransform::Transform(methodInfo)`
- `Interpreter::Execute(method, args, ret)`
- dispatch loop
- 参数、返回值、分支、调用

这一节回答：

- 一个热更方法最终为什么真的能跑起来

### 13. AOT 泛型和 MethodBridge 为什么是必要能力，不只是附加优化

这一节是支线，但必须写。

会讲两件事：

- `AOT 泛型` 为什么会天然和 `IL2CPP AOT` 冲突
- `MethodBridge / ReversePInvokeWrapper` 为什么跨 interpreter / AOT / native 边界必须存在

这一节回答：

- 为什么光有解释器仍然不够支撑真实项目

### 14. 把整条链压成一句话

保持站点现有文章的收束方式。

会把整篇压成一句主线结论。

### 15. 常见误解

计划写 4 个：

- 误解一：HybridCLR 本质上只是一个解释器
- 误解二：补充元数据等于解决了所有泛型问题
- 误解三：Editor 菜单只是工具细节，不属于原理
- 误解四：MethodBridge 只是性能优化，没有它也能正常工作

## 写法细节

正文里每一节都按这个模板组织：

1. 这一节要回答什么
2. 先看哪几个函数
3. 调用链怎么走
4. 关键数据结构是什么
5. 这一层到底解决了什么问题

## 代码展示原则

- 每次只贴关键片段，不贴整函数
- 每段代码后立刻解释：
  - 它在主线里的位置
  - 它的输入
  - 它的输出
  - 它的 runtime 语义
- 不做“逐行翻译式注释”

## 不写的内容

为了不跑偏，这些内容不会展开成正文主线：

- HybridCLR 接入步骤
- 商业版能力的详细原理
- DHE 细节实现
- Full Generic Sharing 深入细节
- 完整枚举所有 opcode
- 完整梳理 `TransformContext.cpp` 全部逻辑

这些最多作为边界说明。

## 成稿标准

第一版写完后，要满足这几个标准：

- 读者可以顺着文章自己打开源码
- 读者能说清楚 `RuntimeApi -> Assembly -> Transform -> Execute` 这条主线
- 读者能理解菜单按钮为什么存在
- 读者能知道 HybridCLR 不是单点魔法，而是 build-time 和 runtime 共同完成的系统链路
