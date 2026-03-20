# Unity 裁剪系列规划

## 定位

这套系列不是 `IL2CPP` 教程，也不是 Player Settings 选项说明书。

它的目标是：

`把 Unity 构建期“裁掉什么、凭什么裁、为什么会误删、怎样写代码才能更适合被裁剪”讲成一套有源码依据的工程理解。`

读者看完之后，至少应该得到四样东西：

- 知道 Unity 里的 `managed stripping`、`Strip Engine Code` 和 `native symbol strip` 不是一回事
- 知道 `Managed Stripping Level` 到底在改变什么
- 知道为什么反射、自动注册、运行时泛型会经常成为误删高发区
- 知道怎样把代码写得更“strip-friendly”，而不是每次出问题才补 `link.xml`

## 系列边界

这套系列明确不做下面几件事：

- 不系统讲 `IL2CPP` 编译链全貌
- 不写成逆向工程教程
- 不讲一般性的 C# 反射教程
- 不把 `Strip Engine Code` 和二进制层面的 `strip --strip-debug` 混成一个概念

只有在下面这些地方，才会最小程度提到 `IL2CPP`：

- `Strip Engine Code` 是 `IL2CPP-only`
- `IL2CPP` 下 `Managed Stripping Level` 不能真正设成 `Disabled`

## 当前完成状态

- [x] 01｜Unity 的裁剪到底分几层
- [x] 02｜Managed Stripping Level 到底做了什么
- [x] 03｜Unity 为什么有时看不懂你的反射
- [x] 04｜哪些 Unity 代码最怕 Strip，以及怎样写得更适合裁剪
- [ ] 05｜Strip Engine Code 到底在裁什么

## 核心判断

这套系列统一围绕两句判断展开：

1. `越依赖运行时动态解释的代码，越危险；越能在构建期被 Unity 看见的依赖，越适合被正确 strip。`
2. `Strip Engine Code 不是“最后对二进制跑一次 strip”，而是 linker 参与决策之后，重新生成更小的引擎模块/类注册结果。`

## 源码依据

本系列默认以本地 Unity 源码为主证据，必要时直接写明“Unity 当前就是这么实现的”。

当前已经确认的关键依据：

- `E:\HT\Projects\UnitySrcCode\Documentation\ApiDocs\UnityEditor\ManagedStrippingLevel.mem.xml`
  - 官方对 `Disabled / Minimal / Low / Medium / High` 的定义
- `E:\HT\Projects\UnitySrcCode\Editor\Mono\BuildPipeline\UnityLinker\UnityLinkerArgumentValueProvider.cs`
  - `Minimal -> Minimal`、`Low -> Conservative`、`Medium -> Aggressive`、`High -> Experimental`
- `E:\HT\Projects\UnitySrcCode\Editor\Mono\BuildPipeline\Il2Cpp\IL2CPPUtils.cs`
  - `IL2CPP` 下如果拿到 `Disabled`，会被强制改成 `Minimal`
- `E:\HT\Projects\UnitySrcCode\Documentation\ApiDocs\UnityEditor\PlayerSettings.mem.xml`
  - `Strip Engine Code` 的官方定义：`IL2CPP-only`
- `E:\HT\Projects\UnitySrcCode\Editor\Mono\BuildPipeline\AssemblyStripper.cs`
  - linker 参数拼装
  - `link.xml` 搜集
  - `TypesInScenes.xml`
  - `SerializedTypes.xml`
  - `MethodsToPreserve.xml`
- `E:\HT\Projects\UnitySrcCode\Editor\Src\BuildPipeline\SerializedInfoCollection.cpp`
  - `UnityEvent.PersistentCall` 的特殊保留逻辑
- `E:\HT\Projects\UnitySrcCode\Editor\Mono\BuildPipeline\RuntimeClassMetadata.cs`
  - `AddMethodToPreserve` 和 `RuntimeClassRegistry`
- `E:\HT\Projects\UnitySrcCode\Editor\IncrementalBuildPipeline\PlayerBuildProgramLibrary\ClassRegistrationGenerator.cs`
  - 从 linker 输出和 editor 数据生成 `UnityClassRegistration.cpp`
- `E:\HT\Projects\UnitySrcCode\Editor\IncrementalBuildPipeline\PlayerBuildProgramLibrary\PlayerBuildProgramBase.cs`
  - 裁剪后的注册源码如何重新并入 native build
- `E:\HT\Projects\UnitySrcCode\Runtime\Export\Scripting\PreserveAttribute.cs`
  - `[Preserve]`
- `E:\HT\Projects\UnitySrcCode\Runtime\Export\Scripting\RuntimeInitializeOnLoadAttribute.cs`
  - `[RuntimeInitializeOnLoadMethod]` 继承自 `PreserveAttribute`

## 系列总主线

推荐按下面这条顺序写：

`先建地图 -> 再讲 managed stripping -> 再讲 Unity 能识别和识别不了的动态依赖 -> 再讲危险代码模式与可执行写法 -> 最后讲 Strip Engine Code`

这个顺序的好处是：

- 先把概念边界拆开
- 再把误删问题讲清楚
- 再把“我应该怎么写代码”落到工程动作
- 最后再进引擎模块裁剪，不会把 managed / native 两层混掉

## 系列结构

建议做成 `5 篇正式长文`。

### 01｜Unity 的裁剪到底分几层

核心问题：
`Unity 里常说的 strip，到底在说哪一层？`

必须讲清楚的点：

- `managed stripping`
- `Strip Engine Code`
- `native symbol strip`
- 为什么它们经常被混成一个词

结论方向：

- `managed stripping` 处理的是托管程序集和托管依赖
- `Strip Engine Code` 处理的是原生引擎模块与类注册
- `native symbol strip` 处理的是最终二进制里的符号信息

适合配的图：

- 一张三层裁剪地图

### 02｜Managed Stripping Level 到底做了什么

核心问题：
`Minimal / Low / Medium / High 的差别，到底是文案区别，还是规则区别？`

必须讲清楚的点：

- `Disabled`、`Minimal`、`Low`、`Medium`、`High` 的官方定义
- 它们和 `UnityLinker` rule set 的映射关系
- 为什么 `Minimal` 和 `Low` 不是简单“力度大小”差异
- 为什么 `IL2CPP` 下没有真正的 `Disabled`

结论方向：

- `Minimal` 更像“只动类库和引擎程序集，其他程序集直接复制”
- `Low / Medium / High` 才是强度不断提高的 reachability stripping
- `Medium / High` 官方就已经承认会更容易碰到反射和 `link.xml` 问题

适合配的图：

- 一张 `Managed Stripping Level -> RuleSet` 映射图

### 03｜Unity 为什么有时看不懂你的反射

核心问题：
`Unity 到底能自动识别哪些动态依赖，哪些识别不了？`

必须讲清楚的点：

- 场景里的 managed type 会进入 `TypesInScenes.xml`
- 序列化类型会进入 `SerializedTypes.xml`
- `UnityEvent.PersistentCall` 会进入 `MethodsToPreserve.xml`
- `Assets` 下的 `link.xml` 会自动被搜集
- `[Preserve]` 是显式保留通道
- 这不等于 Unity 能理解任意反射

结论方向：

- Unity 的策略不是“万能反射分析”
- Unity 更像是在构建期收集一组“已知保留通道”
- 已知模式能补救，未知动态行为就需要开发者自己声明

适合配的图：

- 一张 “场景 / 序列化 / UnityEvent / link.xml / Preserve -> UnityLinker” 的依赖入口图

### 04｜哪些 Unity 代码最怕 Strip，以及怎样写得更适合裁剪

核心问题：
`什么代码写法最容易在运行时缺东西？怎样让 strip 更稳、更狠？`

这篇是强工程导向篇，必须单独写。

必须讲清楚的危险模式：

- 字符串驱动的反射
  - `Type.GetType(string)`
  - `GetMethod(string)`
  - `MethodInfo.Invoke`
  - `Activator.CreateInstance`
- 全程序集扫描和自动注册
  - `GetAssemblies().SelectMany(a => a.GetTypes())`
  - 容器自动注册
  - 消息总线自动发现
  - 运行时 serializer / binder 扫描
- 运行时拼泛型
  - `MakeGenericType`
  - `MakeGenericMethod`
- 配置驱动入口
  - JSON / 表驱动类名和方法名
- 只靠名字字符串的协议调用
  - 脚本桥接
  - 插件回调表
  - 约定式入口

必须讲清楚的 strip-friendly 写法：

- 直接引用胜过字符串查找
- 真引用胜过程序集扫描
- 编辑器期生成注册表胜过运行时发现
- 最小粒度 `[Preserve]` / `link.xml` 胜过整程序集粗暴保留
- 把例外集中管理，不要让保留规则散落
- 提前在目标 stripping level 上做 smoke test

这一篇的中心句：

`让 UnityLinker 尽量靠静态信息完成工作，少靠运行时猜。`

适合配的图：

- 左边“高危写法”，右边“更适合裁剪的替代写法”

### 05｜Strip Engine Code 到底在裁什么

核心问题：
`Strip Engine Code 真的是“对 libunity 做一次普通 strip”吗？`

必须讲清楚的点：

- `Strip Engine Code` 的官方定义是 `IL2CPP-only`
- 它开启后，linker 会拿到 `--enable-engine-module-stripping`
- editor 会把场景类型、native type、模块 include/exclude 等信息写给 linker
- linker 输出会被 `ClassRegistrationGenerator` 再转成 `UnityClassRegistration.cpp`
- 也就是说它会影响最终注册进 player 的模块和类型

结论方向：

- 这不是“单纯删符号”
- 这是“linker 决策 + 重新生成更小的原生注册代码 + native build 重新编译/链接”

适合配的图：

- `EditorToUnityLinkerData.json -> UnityLinker -> UnityLinkerToEditorData.json -> UnityClassRegistration.cpp -> native build`

## 写法约束

整套系列统一遵守下面这些约束：

- 不把文章写成 Player Settings 按钮说明书
- 不用“Higher / Lower”这种空泛形容词糊弄，尽量落回具体实现
- 明确区分“源码直接证明的结论”和“从源码推出来的工程判断”
- 先建地图，再进文件细节
- 每篇只回答一个主问题，不把 managed / engine / symbol 三层混写
- 所有“危险代码模式”都要配对应的“更安全写法”
- 尽量把结论落成工程动作，而不是停在原理

## 优先顺序

建议按下面的顺序产出：

1. `Unity 的裁剪到底分几层`
2. `Managed Stripping Level 到底做了什么`
3. `Unity 为什么有时看不懂你的反射`
4. `哪些 Unity 代码最怕 Strip，以及怎样写得更适合裁剪`
5. `Strip Engine Code 到底在裁什么`

这个顺序能让第 4 篇的工程建议建立在第 2、3 篇的证据之上，不会显得像纯经验贴。

## 最后压缩成一句话

如果整套系列最后只能让读者记住一句话，那应该是：

`Unity 的 strip 不是“神秘黑盒删代码”，而是一套依赖构建期可见性做裁剪的机制；你写法越动态，越需要自己告诉它别删什么。`
