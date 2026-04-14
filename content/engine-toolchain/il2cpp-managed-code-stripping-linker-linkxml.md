---
title: "IL2CPP 实现分析｜Managed Code Stripping：裁剪策略与 link.xml"
date: "2026-04-14"
description: "系统拆解 IL2CPP 的 Managed Code Stripping 机制：UnityLinker 基于可达性分析的裁剪原理、四级裁剪策略（Minimal/Low/Medium/High）的行为差异、link.xml 的保护语法与 HybridCLR 的自动生成、裁剪与反射的冲突（运行时 TypeLoadException 的根因）、裁剪与热更新的冲突（AOT 类型被裁导致热更 DLL 找不到依赖）。对比 CoreCLR PublishTrimmed、Mono linker、LeanCLR 的裁剪策略差异。"
weight: 57
featured: false
tags:
  - IL2CPP
  - Unity
  - Stripping
  - Linker
  - Optimization
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> IL2CPP 把所有可见的 C# 代码编译成 native code——如果不裁剪，一个空项目的包体也会包含整个 BCL 的 native 编译产物。Managed Code Stripping 就是解决这个问题的机制，但它的每一刀裁剪都可能切断反射和热更新的依赖链。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 8 篇，也是 IL2CPP 模块的完结篇。

D7 分析了 IL2CPP 的 ECMA-335 覆盖度——哪些特性支持、哪些不支持。裁剪（stripping）在另一个维度上收窄了运行时可用的能力集合：即使某个类型在覆盖度范围内被支持，如果它被 stripping 裁掉了，运行时同样找不到它。

裁剪问题贯穿了 IL2CPP 工程实践的几乎所有环节。反射调用报 `TypeLoadException`——通常是被裁了。HybridCLR 热更 DLL 加载后报 `MissingMethodException`——通常是 AOT 侧的依赖类型被裁了。理解裁剪机制是诊断这类问题的前提。

## 为什么需要裁剪

IL2CPP 的 AOT 管线会把所有"可见"的 C# 代码编译成 C++ 再编译成 native binary。这里的"可见"包括：

- 项目中的所有 C# 脚本
- 引用的所有 NuGet 包和 Unity 包
- Unity 引擎内部的 C# 层（UnityEngine.dll、UnityEditor.dll 的运行时子集）
- .NET BCL（Base Class Library）——System.dll、System.Collections.dll、System.Linq.dll 等

BCL 是最大的体积来源。一个空 Unity 项目不使用 `System.Xml`、`System.Net.Http`、`System.Security.Cryptography` 中的绝大多数类型，但如果不裁剪，这些类型的 native 代码都会出现在最终的 `GameAssembly` 中。

一个实际的数据感知：BCL 的全量 native 编译可以轻松增加 20-30MB 的包体。对于移动端游戏，包体每增加 10MB 就意味着下载转化率的下降。裁剪的首要目的就是把这些不使用的代码从最终包体中移除。

## UnityLinker 的工作方式

Unity 使用 UnityLinker（基于 Mono.Linker / ILLink）执行裁剪。UnityLinker 在 il2cpp.exe 转换之前工作——先裁剪不需要的 IL 代码，再把裁剪后的精简 IL 交给 il2cpp.exe 转换。

### 可达性分析

UnityLinker 的核心算法是可达性分析（reachability analysis），也称为 tree shaking 或 dead code elimination：

```
入口点（Roots）
    │
    ├── MonoBehaviour 子类的公开方法（Unity 会反射调用）
    ├── [RuntimeInitializeOnLoadMethod] 标记的方法
    ├── 序列化字段引用的类型
    ├── link.xml 中显式保留的类型和方法
    │
    ↓
从入口点出发，递归追踪所有被引用的类型和方法
    │
    ↓
未被任何入口点直接或间接引用的类型/方法 → 标记为不可达 → 裁剪
```

**第一步：确定入口点。** UnityLinker 收集所有已知的代码入口——MonoBehaviour 的消息方法（`Awake`、`Start`、`Update`）、`[Preserve]` 标记的成员、序列化引用、link.xml 声明。

**第二步：传播可达性。** 从入口点出发，分析每个方法体的 IL 指令。如果一个方法调用了 `List<int>.Add`，那么 `List<int>` 类型、`Add` 方法、以及它们依赖的所有底层类型都被标记为可达。

**第三步：裁剪不可达代码。** 所有未被标记为可达的类型和方法从 IL 程序集中移除。裁剪后的程序集体积更小，il2cpp.exe 转换产生的 C++ 代码也更少，最终的 native binary 更小。

### 裁剪粒度

UnityLinker 支持不同粒度的裁剪：

**类型级别。** 整个类型未被引用 → 移除整个类型定义和它的所有成员。

**方法级别。** 类型被引用但某些方法未被调用 → 在高裁剪级别下移除未调用的方法。类型定义保留，但方法体被替换为 `throw new NotSupportedException()`。

**字段级别。** 在最激进的裁剪级别下，未被访问的字段也可能被移除（这会改变内存布局，风险较高）。

## 裁剪级别

Unity 提供四个裁剪级别，通过 Player Settings → Managed Stripping Level 配置：

### Minimal

只裁剪 BCL 中明确不需要的部分。项目自身的代码和 Unity 引擎代码几乎不裁。

这是最安全的级别——几乎不会裁掉有用的类型。代价是包体裁剪效果最弱，BCL 中很多不使用的类型仍然保留在包体中。

适用场景：开发阶段、裁剪问题排查。

### Low

在 Minimal 基础上，对 BCL 进行更积极的裁剪。项目代码仍然大部分保留。

裁剪掉的是 BCL 中完全没有引用链的命名空间——比如项目没有使用任何 XML 相关 API，那么 `System.Xml` 的大部分类型会被移除。

### Medium

开始对项目代码进行方法级别的裁剪。未被调用的 public 方法可能被移除。

这个级别开始出现"误裁"风险——如果一个 public 方法只通过反射调用（`MethodInfo.Invoke`），UnityLinker 的静态分析看不到这个调用关系，可能会把它裁掉。

### High

最激进的裁剪。尽可能移除所有未直接引用的代码，包括未使用的公开类型、方法和字段。

这个级别的包体优化效果最强，但误裁风险最高。几乎所有通过反射、字符串查找、动态加载引用的代码都需要通过 link.xml 或 `[Preserve]` 显式保护。

Unity 2021+ 使用 IL2CPP 时推荐的默认级别是 **Minimal** 或 **Low**。大多数正式发布的项目使用 **Low** 或 **Medium**，根据实际裁剪问题逐步添加 link.xml 保护。

## link.xml — 保护类型不被裁剪

link.xml 是告诉 UnityLinker "不要裁剪这些东西" 的配置文件。放在项目的 Assets 目录下（任意子目录），Unity 构建时会自动收集所有 link.xml。

### 基本语法

```xml
<linker>
  <!-- 保留整个程序集 -->
  <assembly fullname="System.Runtime.Serialization" preserve="all"/>
  
  <!-- 保留程序集中的特定类型 -->
  <assembly fullname="UnityEngine">
    <type fullname="UnityEngine.Networking.UnityWebRequest" preserve="all"/>
  </assembly>
  
  <!-- 保留类型的特定成员 -->
  <assembly fullname="MyGameAssembly">
    <type fullname="MyGame.Config.GameSettings" preserve="all"/>
    <type fullname="MyGame.Network.MessageHandler" preserve="methods"/>
  </assembly>
  
  <!-- 通配符 -->
  <assembly fullname="MyPluginAssembly">
    <type fullname="MyPlugin.Serialization.*" preserve="all"/>
  </assembly>
</linker>
```

`preserve` 属性的值：
- `all` — 保留类型的所有成员（方法、字段、属性）
- `methods` — 只保留方法
- `fields` — 只保留字段
- 不写 `preserve` — 只保留类型定义本身，但成员可能被裁

### [Preserve] 特性

除了 link.xml，Unity 还提供 `[Preserve]` 特性作为代码级的保护标记：

```csharp
[Preserve]
public class MyReflectionTarget
{
    [Preserve]
    public void MethodCalledByReflection() { }
}
```

`[Preserve]` 的作用范围是单个类型或成员。对于需要保护整个命名空间或整个程序集的场景，link.xml 更合适。

### HybridCLR 的 link.xml 自动生成

HybridCLR 的构建工具链包含 `LinkGeneratorCommand`，它分析热更 DLL 引用的所有 AOT 类型和方法，自动生成对应的 link.xml。

这个自动生成解决了热更新场景下最棘手的 link.xml 维护问题。手动维护意味着每次热更代码引用了新的 AOT 类型，都需要记得更新 link.xml——遗漏就会导致运行时找不到类型。自动生成把这个人工步骤变成了构建流程的一部分。

## 裁剪与反射的冲突

反射是裁剪机制的天然敌人。

UnityLinker 的可达性分析基于静态的 IL 指令分析——它扫描方法体中的 `call`、`newobj`、`ldfld` 等指令，追踪从入口点到目标类型的引用链。反射调用打破了这个分析的前提：

```csharp
// UnityLinker 可以看到这个引用
var list = new List<int>();

// UnityLinker 看不到这个引用
var type = Type.GetType("System.Collections.Generic.List`1");
var instance = Activator.CreateInstance(type.MakeGenericType(typeof(int)));
```

第一种写法在 IL 层面有明确的 `newobj` 指令指向 `List<int>`。UnityLinker 可以追踪到这个引用，保留 `List<int>` 类型。

第二种写法在 IL 层面只有对 `Type.GetType` 的调用和一个字符串参数。UnityLinker 不会（也不可能可靠地）解析字符串参数来推断被引用的类型。在高裁剪级别下，如果没有其他代码直接引用 `List<int>`，这个类型可能被裁掉。

**运行时症状：**

```
TypeLoadException: Could not load type 'System.Collections.Generic.List`1' 
from assembly 'mscorlib, Version=4.0.0.0'
```

或者更隐蔽的：

```
MissingMethodException: Method not found: 
'Void MyNamespace.MyClass.MyMethod()'
```

类型还在，但某个方法的方法体被裁剪替换为 `throw NotSupportedException()`。

### 诊断路径

遇到裁剪相关的运行时异常，诊断步骤：

1. 将 Managed Stripping Level 设为 Minimal，确认问题消失——确认是裁剪问题
2. 检查报错的类型/方法是否通过反射或字符串引用——确认 UnityLinker 为什么看不到
3. 在 link.xml 中添加保护——或者用 `[Preserve]` 标记
4. 逐步提高裁剪级别，每次验证——找到裁剪与体积的平衡点

## 裁剪与热更新的冲突

热更新场景把裁剪冲突推到了更高的维度。

常规的裁剪冲突发生在单个项目内部：项目中的代码通过反射引用了被裁剪的类型。问题和解决方案都在同一个构建产物中。

热更新的裁剪冲突发生在两个独立的代码集合之间：AOT 构建产物（包含裁剪后的 native code）和热更 DLL（运行时加载的新代码）。热更 DLL 引用的 AOT 类型如果在构建时被裁掉了，运行时就找不到。

```
构建时：
  AOT 代码 → UnityLinker 裁剪 → il2cpp.exe → native binary
  热更代码 → 不参与 AOT 构建（构建后才加载）

运行时：
  热更 DLL 加载 → 引用 System.Text.Json.JsonSerializer
                     ↓
  AOT 侧查找 JsonSerializer → 被裁了 → MissingMethodException
```

问题的核心：**UnityLinker 在构建时不知道热更 DLL 会引用哪些类型**。热更 DLL 是构建之后才编写和编译的——这是热更新的全部意义所在。

### HybridCLR 的解决方案

HybridCLR 通过两条路径解决裁剪与热更新的冲突：

**路径一：LinkGeneratorCommand 生成 link.xml。** 在构建时扫描当前版本的热更 DLL，分析它引用的所有 AOT 类型和方法，生成 link.xml 保护这些依赖。这保证了当前版本的热更代码在运行时能找到所有依赖。

局限性：只能保护当前版本引用的类型。如果未来的热更版本引用了新的 AOT 类型（当前版本未引用），这些类型不在 link.xml 中，仍然会被裁掉。解决方案是在 link.xml 中预留可能用到的类型，或者降低裁剪级别。

**路径二：supplementary metadata。** D5 和 HybridCLR 系列分析过，HybridCLR 的补充 metadata 机制不仅保留泛型实例的 native 代码，也间接保留了相关类型不被裁剪——因为 `AOTGenericReference` 的代码引用了这些类型，UnityLinker 可以追踪到。

两条路径组合覆盖了热更新场景下的大多数裁剪问题。但开发者仍然需要理解裁剪机制，才能在遇到 link.xml 自动生成覆盖不到的边界情况时知道如何手动补充。

## 与其他 runtime 的裁剪对比

### CoreCLR — PublishTrimmed + ILLink

.NET SDK 提供 `PublishTrimmed` 选项，使用 ILLink（和 UnityLinker 同源）执行裁剪：

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishTrimmed=true
```

CoreCLR 的裁剪和 IL2CPP 的裁剪在工具层面同源（都是 Mono.Linker），但使用场景不同：

**CoreCLR 裁剪是可选的。** 大多数 .NET 应用不需要裁剪——它们运行在有完整 .NET Runtime 的环境中。只有 self-contained 部署（把 runtime 打包进应用）或 Native AOT 部署时才需要裁剪。

**CoreCLR 有 Trim 兼容性标注。** .NET 8+ 引入了 `[DynamicallyAccessedMembers]`、`[RequiresUnreferencedCode]` 等 attribute，让库作者声明"这个方法通过反射访问了哪些成员"。ILLink 可以利用这些标注做更精确的裁剪——知道反射会访问什么，就不会误裁。

IL2CPP 的 UnityLinker 也在逐步支持这些标注，但 Unity 生态中的大量第三方库还没有添加 trim 兼容性标注。

### Mono — linker

Mono 的裁剪使用 mono-linker（Mono.Linker 的前身），算法原理和 UnityLinker / ILLink 相同。Mono 在 Unity Editor 中作为脚本 runtime，通常不做裁剪——裁剪主要在构建时针对 IL2CPP 路径执行。

### LeanCLR — 无裁剪

LeanCLR 不需要裁剪机制。原因是 LeanCLR 的设计哲学和 IL2CPP 相反：

**LeanCLR 不预编译 BCL。** LeanCLR 是纯解释器，BCL 以 DLL 形式存在，运行时按需加载和解释执行。不使用的类型不会被加载到内存中，也不会产生 native code 体积。

**LeanCLR 的 runtime 本身只有 ~600KB。** 这个体积已经足够小，不需要通过裁剪来进一步缩减。BCL DLL 的体积由用户决定——只打包需要的 DLL。

**无裁剪意味着无裁剪冲突。** LeanCLR 不存在"反射引用的类型被裁"或"热更依赖被裁"的问题——因为根本没有裁剪步骤。

这是 LeanCLR 设计选择的一个附带优势：通过放弃 AOT 编译的性能，换来了更简单的构建链和更少的工程障碍。

## 收束 — IL2CPP 模块完结篇

Managed Code Stripping 是 IL2CPP 管线中最容易引发工程问题的环节。它的核心矛盾在于：裁剪基于静态的可达性分析，而实际的代码引用关系可以是动态的（反射、热更新、字符串类名引用）。任何静态分析无法追踪的引用链都是潜在的裁剪风险。

link.xml 和 `[Preserve]` 是应对这个矛盾的工程手段——用人工声明补充静态分析的盲区。HybridCLR 的 `LinkGeneratorCommand` 把这个人工步骤自动化了一部分，但开发者仍然需要理解机制本身，才能处理自动生成覆盖不到的边缘情况。

IL2CPP 模块到此八篇完成。从 D1 的管线架构到 D8 的裁剪机制，覆盖了 IL2CPP 的完整技术栈：

| 编号 | 主题 | 核心问题 |
|------|------|----------|
| D1 | 架构总览 | C# → C++ → native 的完整管线是什么 |
| D2 | il2cpp.exe 转换器 | IL → C++ 代码生成的策略和规则 |
| D3 | libil2cpp runtime | 运行时的三层结构：MetadataCache、Class、Runtime |
| D4 | global-metadata.dat | metadata 的格式、加载和 runtime 绑定 |
| D5 | 泛型代码生成 | 共享、特化与 Full Generic Sharing |
| D6 | GC 集成 | BoehmGC 的接入层 |
| D7 | ECMA-335 覆盖度 | 支持什么、不支持什么、为什么 |
| D8 | Managed Code Stripping | 裁剪策略与 link.xml |

这八篇构成了理解 IL2CPP 工程行为的基础层。HybridCLR 系列的 25+ 篇文章在这个基础上展开——HybridCLR 的每一个设计决策都是对 IL2CPP 某个限制或特性的回应。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 — IL2CPP 模块
- 位置：IL2CPP-D8（模块完结篇）
- 上一篇：[IL2CPP-D7 ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}})

**相关阅读**

- [IL2CPP-D1 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})
- [IL2CPP-D4 global-metadata.dat：格式、加载与 runtime 的绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}})
- [IL2CPP-D5 泛型代码生成：共享、特化与 Full Generic Sharing]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}})
- [IL2CPP-D7 ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}})
- [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})
- [横切对比｜程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载]({{< relref "engine-toolchain/runtime-cross-assembly-loading-hot-update-comparison.md" >}})
