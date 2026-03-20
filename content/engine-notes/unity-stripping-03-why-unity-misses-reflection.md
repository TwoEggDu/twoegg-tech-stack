---
title: "Unity 裁剪 03｜Unity 为什么有时看不懂你的反射"
description: "从 TypesInScenes、SerializedTypes、MethodsToPreserve、link.xml 和 [Preserve] 五条入口拆开 Unity 能自动保留什么，以及它为什么不可能理解任意运行时反射。"
slug: "unity-stripping-03-why-unity-misses-reflection"
weight: 52
featured: false
tags:
  - Unity
  - Build
  - Stripping
  - Reflection
series: "Unity 裁剪"
---

> 这一篇最重要的结论只有一句：`Unity 不是“看懂了你的反射”，而是在构建期收集几条它明确认识的保留通道；超出这些通道的动态依赖，本来就不在它的视野里。`

上一篇我们把 `Managed Stripping Level` 的几档差别拆开了，重点讲清楚了：

- `Minimal` 不是更弱的 `Low`
- `Low / Medium / High` 是同一条逐步变激进的 linker 主线
- `IL2CPP` 下没有真正的 `Disabled`

但光知道“linker 会删不可达代码”还不够。

真实项目里更常见的疑问通常是：

`为什么有些反射路径明明没人手写 Preserve，也能活下来；而另一些看起来差不多的写法，一上 build 就被删了？`

如果这个问题不拆开，后面谈 `link.xml`、`[Preserve]`、自动注册、运行时泛型时，结论会很容易飘。

所以这一篇只回答一件事：

`Unity 到底能自动识别哪些动态依赖，哪些识别不了？`

## 这篇要回答什么

这篇文章主要回答四个问题：

1. Unity 当前源码里，哪些依赖入口会被显式喂给 `UnityLinker`。
2. 为什么这些入口不等于“Unity 会分析任意反射”。
3. 哪些运行时动态行为最容易落到 Unity 视野外。
4. 工程上该怎么判断问题是“Unity 本来就看不见”，还是“你忘了显式保留”。

如果先给一个压缩版答案，可以写成这样：

- Unity 会自动收集一部分“已知保留入口”。
- 这些入口主要是场景类型、序列化类型、`UnityEvent` 持久化调用、`Assets` 下的 `link.xml`、以及显式保留属性。
- Unity 不会替你理解任意字符串驱动反射、全程序集扫描、运行时拼泛型或配置驱动入口。

也就是说，它更像是在做：

`build-time known roots collection`

而不是：

`万能运行时语义推理`

## 先给一张入口图

如果把 Unity 当前会主动喂给 linker 的几条主要保留入口压成一张表，大概是这样：

| 入口 | 主要来源 | 最终交给 linker 的形态 | 它能解决什么 | 它解决不了什么 |
| --- | --- | --- | --- | --- |
| 场景里的 managed type | `RuntimeClassRegistry.GetAllManagedTypesInScenes()` | `TypesInScenes.xml` | 场景和已知引用类型不被整体删掉 | 运行时临时拼出来的类型 |
| 序列化类型 | `GetAllSerializedClassesAsString()` | `SerializedTypes.xml` | 序列化系统明确记录到的类型 | 没进入序列化图的动态类型 |
| `UnityEvent.PersistentCall` | `AddMethodToPreserve()` | `MethodsToPreserve.xml` | Inspector 挂好的持久化回调目标方法 | 运行时 `AddListener`、字符串反射找方法 |
| 用户声明 | `Assets/**/link.xml` | 直接传给 linker | 你明确知道必须保留的类型和成员 | 你没声明出来的动态依赖 |
| 显式属性 | `[Preserve]`、`[RuntimeInitializeOnLoadMethod]` | 属性本身参与保留语义 | 某个类型、方法、字段、入口显式不删 | 任意未标记的动态调用链 |

这张表最重要的不是记文件名，而是记边界：

`Unity 自动保留的不是“所有动态行为”，而是“几条源码里写死的已知通道”。`

## 一、第一条通道：场景里的 managed type

先看 `RuntimeClassRegistry.cs`。

这里有一个非常关键的方法：

`GetAllManagedTypesInScenes()`

它会把当前构建里已经被认出来的 user assembly type 和 UnityEngine 相关 managed wrapper type 汇总起来，然后交给 `AssemblyStripper` 去写文件。

`AssemblyStripper.cs` 里对应的实现也很直接：

- `WriteTypesInScenesBlacklist(...)`
- 取 `runInformation.rcr.GetAllManagedTypesInScenes()`
- 生成 `TypesInScenes.xml`

而且写出来的内容是明确的 linker XML：

```xml
<assembly fullname="SomeAssembly">
    <type fullname="Some.Namespace.SomeType" preserve="nothing"/>
</assembly>
```

这说明 Unity 当前并不是“理解了你后面会怎么反射这个类型”，而是：

`它先把场景和构建图里已经看见的类型记下来，再把这些类型当成已知根喂给 linker。`

这条通道能解释很多看似“有点反射味道，但为什么没被删”的情况：

- 某个 `MonoBehaviour` 明明后面也会被反射拿到，但它本来就挂在场景里
- 某个 `ScriptableObject` 类型被资源直接引用
- 某些 UnityEngine wrapper type 因为场景 native class 已经进入注册链

这些东西活下来，不代表 Unity 分析懂了你后面的运行时逻辑，只代表：

`它们在更早的构建期就已经被看见了。`

## 二、第二条通道：序列化图里出现过的类型

第二条通道和第一条很像，但它关注的是：

`哪些类型被 Unity 的序列化系统明确记录到了。`

`RuntimeClassRegistry` 里有：

- `SetSerializedTypesInUserAssembly(...)`
- `GetAllSerializedClassesAsString()`

`AssemblyStripper.cs` 里则会把这些信息写成 `SerializedTypes.xml`。

对应生成代码里最值得注意的一行是：

```xml
<type fullname="Some.Namespace.SomeType" preserve="nothing" serialized="true"/>
```

也就是说，这不是随便“猜测你可能会反射它”，而是：

`Unity 在 build 时已经知道这个类型出现在序列化图里，所以显式告诉 linker：这个类型和序列化语义相关，别直接删没。`

这一点特别容易被误讲成：

“Unity 会自动处理序列化相关反射。”

更准确的说法其实应该是：

`Unity 会处理它自己的序列化系统已经看见的那部分类型依赖。`

边界仍然很清楚：

- 进了序列化图，Unity 更容易保住它
- 没进序列化图，只在运行时字符串里提过一次名字，Unity 还是看不见

## 三、第三条通道：`UnityEvent.PersistentCall` 的特殊保留

这一条是很多人最容易误判的地方。

因为它会给人一种错觉：

`为什么有些方法名明明也是字符串，Unity 却能保住？`

答案不是 Unity 突然学会了“通用字符串反射分析”，而是它专门为 `UnityEvent.PersistentCall` 开了一条特殊通道。

关键证据在 `SerializedInfoCollection.cpp`。

这个文件会在序列化对象遍历时检查：

- 当前对象是不是 `UnityEngine.Events.PersistentCall`
- `targetAssemblyTypeName` 和 `methodName` 是否有效
- 目标对象类型上是否真的存在这个方法

如果都成立，它不会停在“哦，这里有个字符串方法名”这个层面，而是直接调用：

`AddMethodToPreserve(assembly, namespace, class, methodName)`

然后 `RuntimeClassRegistry.cs` 会把这些方法累积进 `m_MethodsToPreserve`，最后 `AssemblyStripper.cs` 再生成 `MethodsToPreserve.xml`：

```xml
<assembly fullname="SomeAssembly" ignoreIfMissing="1">
    <type fullname="Some.Namespace.SomeType">
        <method name="SomeMethod"/>
    </type>
</assembly>
```

所以这里的真实结论是：

`Unity 不是“理解了任意方法名字符串”，而是对 PersistentCall 这种自己可控、可验证、可在构建期收集的模式做了定向保留。`

这个边界一旦看清，很多误解就会消失。

比如：

- Inspector 里挂好的 `UnityEvent` 持久化回调经常能活下来
- 但运行时 `button.onClick.AddListener(SomeMethod)` 并不会因此自动获得同等级别的保留
- 更不用说你自己写的 `GetMethod("SomeMethod")`、`Invoke(...)`、配置表里写类名方法名这些路径

前者是 Unity 自己认识的构建期数据。

后者只是你运行时才解释的动态行为。

这两类事，本来就不在一条保留链上。

## 四、第四条通道：`Assets` 下的 `link.xml`

再往下看 `AssemblyStripper.GetLinkXmlFiles(...)`，你会看到一个非常直接的入口：

`GetUserBlacklistFiles()`

实现更直接：

```csharp
Directory.GetFiles("Assets", "link.xml", SearchOption.AllDirectories)
```

也就是说，Unity 当前会主动扫描 `Assets` 目录下的 `link.xml`，然后把这些文件一起传给 linker。

这件事本身已经说明一个事实：

`Unity 从来没假设自己一定能推断出所有动态依赖。`

不然它根本没必要给你一条显式声明通道。

`link.xml` 的角色，本质上就是：

- 给构建期不可见的依赖补一份声明
- 把“你自己知道会用，但 Unity 看不见”的类型和成员告诉 linker

所以只要项目里有这些东西，`link.xml` 就经常会变成必须品：

- 配置驱动的类型名 / 方法名
- 第三方容器自动注册
- 运行时 serializer / binder 扫描
- 热更桥接或脚本桥接入口
- 某些插件靠反射或约定式入口回调

这里最需要纠正的一种想法是：

`link.xml` 不是给 Unity 补 bug 的。

更准确地说，它是在补：

`构建期本来就不可见的动态依赖。`

## 五、第五条通道：`[Preserve]` 和显式保留属性

除了 `link.xml`，Unity 还给了一条更细粒度的通道：属性。

`PreserveAttribute.cs` 很简单，核心就是：

```csharp
public class PreserveAttribute : System.Attribute
{
}
```

但它的意义不在代码量，而在语义：

`它是告诉 UnityLinker“这个目标别删”的显式标记。`

更有意思的是另一个文件：

`RuntimeInitializeOnLoadAttribute.cs`

这里直接写着：

```csharp
public class RuntimeInitializeOnLoadMethodAttribute : Scripting.PreserveAttribute
```

这行继承关系非常值得写进文章里，因为它能纠正一个常见误解：

很多人会以为 `RuntimeInitializeOnLoadMethod` 之所以能活下来，是因为 Unity 理解了“启动流程一定会调它”。

但从当前源码看，更直接的事实是：

`这个 attribute 自己就是 PreserveAttribute 的子类。`

也就是说，它首先是一条显式保留通道，然后才是一条运行时初始化语义。

这也提示了一个很实用的工程结论：

- 如果你已经明确知道某个类型或方法必须保留，最可靠的方式之一就是显式声明
- 不要把希望寄托在“Unity 也许能顺着某条动态链路推断到它”

## 到这里，边界其实已经很清楚了

把前面几条通道合起来，Unity 当前真正做的事情更接近：

1. 收集场景里已经出现的 managed type。
2. 收集序列化系统已经记录到的类型。
3. 对 `UnityEvent.PersistentCall` 这种已知模式额外抽出方法保留。
4. 合并用户在 `Assets` 下写的 `link.xml`。
5. 识别显式保留属性。

这套策略的风格非常统一：

`只处理构建期可收集、可验证、可落到具体类型或方法名上的依赖。`

所以它的反面也就很明确：

`凡是必须等到运行时、靠字符串、靠扫描、靠约定、靠数据解释才能知道的依赖，Unity 都没有义务“自动懂”。`

## 六、Unity 典型看不见的几类动态依赖

基于前面的源码证据，可以把最危险的盲区大致压成下面几类。

这里我要先标清楚：

`下面这些是从源码里现有保留通道反推出来的工程判断。`

它们不是某个注释直接写死的句子，但逻辑边界是很清楚的。

### 1. 字符串驱动的类型和方法查找

例如：

- `Type.GetType("Some.Namespace.SomeType")`
- `assembly.GetType(typeName)`
- `type.GetMethod(methodName)`
- `method.Invoke(...)`
- `Activator.CreateInstance(typeNameBasedResult)`

这类路径的问题不在于 API 名字，而在于：

`真正依赖的是运行时字符串值，而不是构建期可见的静态引用。`

如果这些字符串既没有进入序列化图，也没有对应的 `link.xml` / `[Preserve]`，那 Unity 不看见它们才是正常结果。

### 2. 全程序集扫描和自动注册

例如：

- `AppDomain.CurrentDomain.GetAssemblies()`
- `SelectMany(a => a.GetTypes())`
- 约定式扫描实现接口再自动注册
- 容器启动时扫程序集建映射

这类写法的风险在于：

`你需要的不是某一个具体类型，而是一整批“运行时扫描后才知道要不要用”的类型。`

这和 Unity 当前几条“先落成明确 XML / attribute 保留项”的策略天然不匹配。

### 3. 运行时拼泛型

例如：

- `MakeGenericType(...)`
- `MakeGenericMethod(...)`

如果闭包类型参数只在运行时才决定，那构建期就很难有稳定证据告诉 linker：

`请保住这一组具体泛型实例。`

### 4. 配置驱动入口

例如：

- JSON 里写类型名
- 表格里写类名和方法名
- Lua / JS / 热更侧按字符串桥接 C# 入口

这类东西的共同点是：

`依赖图藏在数据里，不在编译期引用里。`

只要没有额外保留通道，Unity 就看不见。

### 5. 运行时 AddListener / 约定式回调表

这一类最容易被 `UnityEvent.PersistentCall` 误导。

因为很多人会想：

“既然 Unity 能保 `UnityEvent`，那我运行时注册监听应该也差不多吧？”

差很多。

源码里特殊处理的是：

`PersistentCall`

也就是 Inspector 里序列化下来的持久化调用。

运行时 `AddListener`、插件回调表、按命名约定找方法这些路径，都不是同一条保留通道。

## 七、工程上怎么判断“Unity 看不见”还是“你漏了声明”

如果项目里已经出了 build 后缺方法、缺类型、反射失败的问题，我建议先按下面这套顺序问：

### 1. 这个依赖有没有进入 Unity 已知通道

先问它属于不属于下面五类：

- 场景里直接出现
- 序列化图里出现
- `UnityEvent.PersistentCall`
- `Assets` 下 `link.xml`
- `[Preserve]` / 派生保留属性

如果一条都不占，那大概率不是 Unity“识别失败”，而是：

`它本来就没有识别入口。`

### 2. 如果有入口，入口是不是只保住了类型，没有保住成员

这一步很重要。

因为“类型没丢”和“某个方法没丢”不是同一回事。

比如：

- 某个类型挂在场景里，类型可能活着
- 但你运行时想反射它的某个很偏的私有方法，这件事未必天然就跟着安全

所以问题不能停在“这个类不是已经在场景里了吗”。

要继续问：

`你真正依赖的粒度，到底是 type 级，还是 method / field / ctor 级？`

### 3. 如果本来就不在已知通道里，别先怪 stripping level

很多团队一出问题第一反应就是：

- 把 `Managed Stripping Level` 调低
- 或者直接回退到 `Minimal`

这有时能止血，但经常治标不治本。

因为如果依赖关系本来就对构建期不可见，那你只是：

`通过“少删一点”掩盖“本来就看不见”的事实。`

## 这一篇最该带走的三句话

如果把这篇文章最后压成三句话，我建议记住这三句：

- Unity 会自动保留的，不是所有反射，而是场景类型、序列化类型、`PersistentCall`、`link.xml`、`[Preserve]` 这些已知通道。
- `UnityEvent.PersistentCall` 能活下来，不代表运行时字符串反射也会自动安全；前者是专门实现过的构建期保留入口，后者不是。
- 真正危险的，不是“Unity 没学会反射”，而是你的依赖图只存在于运行时字符串、扫描或配置里，构建期根本看不见。

下一篇，我们就顺着这个边界继续往下讲最工程化的一步：

`哪些 Unity 代码最怕 Strip，以及怎样把它写得更适合被裁剪。`
