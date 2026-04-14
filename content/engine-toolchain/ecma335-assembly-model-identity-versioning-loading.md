---
title: "ECMA-335 基础｜CLI Assembly Model：程序集身份、版本策略与加载模型"
date: "2026-04-14"
description: "从 ECMA-335 规范出发，拆解 CLI 程序集模型的核心机制：程序集的四元组身份标识、AssemblyRef 跨程序集引用、版本绑定策略、五种 runtime 的加载模型差异，以及这些概念在热更新场景中的实际意义。"
weight: 14
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Assembly"
  - "Versioning"
  - "Loading"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> 程序集不是"DLL 文件"的别名——它是 .NET 中类型可见性、版本管理和部署的基本单元。HybridCLR 的 Assembly.Load、IL2CPP 的 MetadataCache、LeanCLR 的 Assembly::load_by_name 都在实现同一份规范定义的加载契约。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 5 篇。

上一篇讲的是执行模型——方法调用约定、虚分派、异常处理。那些是"代码怎么跑"的问题。这篇讲的是"代码怎么组织"——程序集是 CLI 的部署单元，它定义了类型的归属边界、版本管理的粒度、运行时加载的目标。不理解 assembly identity，就无法理解热更新中"加载一个新 DLL"到底意味着什么。

## 为什么要讲 Assembly Model

前面四篇建立了四个层次：metadata 的物理结构、CIL 指令的执行语义、类型系统的分类规则、执行模型的运行时行为。但这四个层次都回避了一个问题：这些 metadata、IL code、类型定义存放在哪里，以什么为单位进行管理？

答案是 assembly。

ECMA-335 把 assembly 定义为最小的版本管理和部署单元。一个 assembly 包含 metadata、IL code 和资源，对外暴露一组公开类型，对内隐藏实现细节。runtime 加载代码的粒度不是"文件"也不是"类型"，而是 assembly。

这个概念在不同 runtime 中有不同的实现方式，但规范层的契约是同一份：

- CoreCLR 用 AssemblyLoadContext 管理 assembly 的加载和卸载
- IL2CPP 用 MetadataCache 在构建时注册所有 assembly，运行时按名字查找
- HybridCLR 通过 Assembly::LoadFromBytes 在运行时加载热更 assembly
- LeanCLR 通过 Assembly::load_by_name 加载 assembly 并构建 CliImage

它们都在回答同一个问题：给定一个 assembly identity，怎么找到对应的 metadata 和 IL code，怎么解析它的依赖，怎么把它的类型注册到 runtime 的类型系统中。

## 程序集是什么

ECMA-335 Partition II §6 定义了 assembly 的基本结构：

**assembly = metadata + IL code + resources**

一个 assembly 是一个自描述的代码单元。metadata 描述了它包含的所有类型、方法、字段的结构信息；IL code 是这些方法的可执行体；resources 是嵌入的非代码数据（比如本地化字符串、嵌入的配置文件）。

### Assembly 与 Module 的关系

一个 assembly 可以由多个 module 组成。每个 module 对应一个 PE 文件（.dll 或 .exe），其中一个是主模块（manifest module），包含 assembly 的清单信息——名称、版本、依赖列表。

```
Assembly: MyGame.Logic
├── MyGame.Logic.dll     ← 主模块（manifest module）
│   ├── Assembly 表      ← 程序集身份信息
│   ├── AssemblyRef 表   ← 依赖的其他程序集
│   ├── TypeDef 表       ← 本模块定义的类型
│   └── ExportedType 表  ← 转发到其他模块的类型
└── MyGame.Logic.AI.netmodule  ← 附属模块（实际极少使用）
    └── TypeDef 表       ← 本模块定义的类型
```

实际项目中几乎所有 assembly 都是单 module 结构。多 module assembly 在 .NET Framework 时代就已经极为罕见，.NET Core / .NET 5+ 和 Unity 的工具链完全不支持多 module assembly。理解"assembly 可以多 module"主要是为了读懂规范——在实际工程中可以把 assembly 和 module 当作同一个东西。

### Assembly 是类型可见性的边界

C# 中 `internal` 访问修饰符的作用域不是"同一个项目"或"同一个命名空间"，而是"同一个 assembly"。这是 ECMA-335 Partition I §8.5.3 定义的可见性规则。

```csharp
// MyGame.Logic.dll
internal class PathFinder { ... }      // 只有 MyGame.Logic 内部可见
public class NavigationSystem { ... }  // 跨 assembly 可见

// MyGame.Rendering.dll
// 无法访问 PathFinder —— 不在同一个 assembly
```

在 Unity 热更新场景中，这意味着热更 DLL 中的 `internal` 类型对主工程的 AOT assembly 不可见。如果热更代码需要访问主工程的 internal 类型，要么改为 public，要么用 `[InternalsVisibleTo]` 属性打开跨 assembly 的 internal 访问——这个属性本身也存储在 assembly 的 metadata 中（CustomAttribute 表）。

## Assembly Identity：程序集身份

ECMA-335 Partition II §6.1 定义了 assembly 的身份标识——四元组：

**Name + Version + Culture + PublicKeyToken**

这四个字段唯一标识一个 assembly。它们存储在 metadata 的 Assembly 表（表编号 0x20）中。

| 字段 | 说明 | 示例 |
|------|------|------|
| **Name** | 程序集名称（不含扩展名） | `System.Collections` |
| **Version** | Major.Minor.Build.Revision | `6.0.0.0` |
| **Culture** | 区域设置（`neutral` 表示不限区域） | `neutral` |
| **PublicKeyToken** | 公钥的 8 字节哈希 | `b03f5f7f11d50a3a` |

完整的程序集身份字符串形如：

```
System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
```

runtime 在解析 AssemblyRef（跨程序集引用）时，就是用这个四元组来匹配目标 assembly 的。

### Strong Naming

PublicKeyToken 来自 strong naming 机制。strong name 的作用：

**防篡改** — assembly 在签名时用私钥对内容哈希签名。runtime 加载时用公钥验证签名，如果内容被修改过，签名验证失败，加载被拒绝。

**版本隔离** — 不同 PublicKeyToken 的 assembly 即使名称和版本相同也是不同的 assembly。这允许不同厂商发布同名但互不冲突的程序集。

在 Unity 热更新场景中，大多数热更 DLL 不使用 strong name。原因很直接：热更 DLL 需要频繁重新生成和替换，每次都用私钥签名增加了构建流程的复杂度；而且 IL2CPP 和 HybridCLR 的加载路径并不强制验证 strong name 签名。

但 .NET BCL（Base Class Library）的程序集全部使用 strong name。当热更代码引用 `System.Collections.Generic.List<T>` 时，AssemblyRef 中记录的 PublicKeyToken 必须与 runtime 中实际加载的 mscorlib / System.Runtime 的 PublicKeyToken 匹配。

## AssemblyRef：跨程序集引用

metadata 的 AssemblyRef 表（表编号 0x23）记录了当前 assembly 的所有外部依赖。每条 AssemblyRef 记录包含目标 assembly 的四元组信息。

```
// HotUpdate.dll 的 AssemblyRef 表
AssemblyRef #0: mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
AssemblyRef #1: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
AssemblyRef #2: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
```

当 runtime 在执行 IL 时遇到一个跨 assembly 的类型引用（TypeRef 表，表编号 0x01），它通过 TypeRef 中的 ResolutionScope 字段找到对应的 AssemblyRef，再用 AssemblyRef 中的四元组查找已加载的 assembly。

查找失败的后果：

- 如果目标 assembly 未加载且找不到 → `FileNotFoundException`
- 如果目标 assembly 已加载但其中没有被引用的类型 → `TypeLoadException`
- 如果目标 assembly 的版本不匹配 → `FileLoadException`（严格版本匹配模式下）

### HybridCLR 中的加载顺序问题

HybridCLR 的热更场景中，AssemblyRef 解析是一个实际的工程问题。

假设热更 DLL A 依赖热更 DLL B（A 的 AssemblyRef 表中有 B 的记录）。如果先加载 A 再加载 B，A 在加载过程中解析 AssemblyRef 时会发现 B 还没有被加载。根据 runtime 的策略不同，可能立即报错，也可能延迟到首次使用 B 中的类型时才报错。

正确的做法是按依赖图的拓扑排序加载：先加载没有未满足依赖的 assembly，再加载依赖它们的 assembly。这就是 AssemblyRef 表的实际工程意义——它定义了 assembly 之间的依赖图。

```
加载顺序（拓扑排序）：
1. mscorlib / System.Runtime     ← 没有外部依赖
2. UnityEngine.CoreModule        ← 依赖 mscorlib
3. Assembly-CSharp               ← 依赖 mscorlib + UnityEngine
4. HotUpdate-Base.dll            ← 依赖 mscorlib + Assembly-CSharp
5. HotUpdate-Logic.dll           ← 依赖 HotUpdate-Base + Assembly-CSharp
```

## 版本策略

ECMA-335 定义的版本号格式是 **Major.Minor.Build.Revision**，四个 16 位无符号整数。

### 版本绑定的三层机制

在完整的 .NET Framework 中，版本绑定有三层机制叠加：

**应用程序配置（App.config）** — 通过 `<bindingRedirect>` 指定版本重定向。当 AssemblyRef 请求的版本和实际可用的版本不一致时，重定向告诉 runtime 接受不同的版本。

```xml
<dependentAssembly>
  <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed"/>
  <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0"/>
</dependentAssembly>
```

**发布者策略（Publisher Policy）** — assembly 的发布者可以提供一个策略文件，声明新版本兼容旧版本。runtime 在绑定时自动应用这个策略。

**机器策略（Machine.config）** — 管理员在机器级别设置版本绑定规则，优先级最高。

这三层策略从应用级 → 发布者级 → 机器级逐层覆盖，给了 .NET Framework 极大的版本管理灵活性——同时也带来了"DLL Hell"的现代变种：当多层重定向叠加时，最终加载哪个版本变得难以预测。

### Unity / IL2CPP 场景下的版本策略

在 Unity 的构建管线中，所有 DLL 都是同一次构建的产出。IL2CPP 在构建时把所有 assembly 的 metadata 打包进 global-metadata.dat，运行时从这个文件加载——不存在"运行时发现版本不匹配"的情况，因为所有 assembly 在构建时就已经解析完毕。

这意味着 .NET Framework 的整套版本绑定机制在 Unity 场景下几乎不适用。Unity 的"版本问题"发生在构建时（PackageManager 的版本冲突、多个插件引用不同版本的同一个 DLL），不是运行时。

但理解版本策略仍然有价值：

- .NET 服务器项目仍然大量使用 bindingRedirect
- NuGet 包管理的版本解析逻辑依赖这些机制
- 排查 Unity 构建错误时，错误信息中的版本号来自 AssemblyRef 表

## Assembly 加载模型

不同 runtime 对 assembly 加载的实现差异是整个 Assembly Model 中工程意义最大的部分。

### CoreCLR：AssemblyLoadContext

.NET Core / .NET 5+ 引入了 `AssemblyLoadContext`（ALC）作为 assembly 加载和隔离的基本单元。每个 ALC 维护一组已加载的 assembly，同名 assembly 可以在不同的 ALC 中各加载一份。

```csharp
var alc = new AssemblyLoadContext("plugin", isCollectible: true);
Assembly asm = alc.LoadFromAssemblyPath("/path/to/Plugin.dll");
// 使用 assembly 中的类型...
alc.Unload();  // 卸载整个 ALC 及其中的所有 assembly
```

关键特性：

- **隔离**：不同 ALC 中可以加载同名但不同版本的 assembly
- **卸载**：`isCollectible: true` 的 ALC 支持卸载，GC 回收所有相关对象后释放 native 资源
- **依赖解析**：ALC 有 `Resolving` 事件，当 runtime 无法找到 AssemblyRef 引用的 assembly 时触发，允许自定义加载逻辑

### .NET Framework：AppDomain

.NET Framework 使用 AppDomain 作为隔离单元。每个 AppDomain 有自己的一组已加载 assembly，跨 AppDomain 的对象传递需要序列化或 MarshalByRefObject。

AppDomain 在 .NET Core 中已被废弃。取代它的 AssemblyLoadContext 提供了更轻量的隔离机制——不需要跨域序列化的开销，但也不提供 AppDomain 级别的安全隔离。

### IL2CPP：MetadataCache

IL2CPP 的 assembly 加载发生在两个阶段：

**构建时** — il2cpp.exe 把所有 assembly 的 metadata 合并写入 global-metadata.dat。每个 assembly 在文件中有一个 `Il2CppAssemblyDefinition` 记录，包含名称、版本等身份信息。

**运行时** — `MetadataCache::Initialize` 读取 global-metadata.dat，为每个 assembly 创建 `Il2CppAssembly` 结构并注册到全局表中。当代码请求加载 assembly 时（比如通过 `Assembly.Load`），`MetadataCache` 在已注册的 assembly 中按名字查找。

```
global-metadata.dat
├── Il2CppAssemblyDefinition[0]: mscorlib
├── Il2CppAssemblyDefinition[1]: System
├── Il2CppAssemblyDefinition[2]: UnityEngine.CoreModule
├── Il2CppAssemblyDefinition[3]: Assembly-CSharp
└── ...
```

IL2CPP 不支持卸载已加载的 assembly。所有 assembly 在 runtime 初始化时就全部加载，生命周期与进程相同。这是 AOT 模型的固有限制——所有代码在构建时已经编译为 native code，运行时没有"按需加载新代码"的能力。

### HybridCLR：Assembly::LoadFromBytes

HybridCLR 在 IL2CPP 的基础上扩展了运行时加载能力：

```csharp
byte[] dllBytes = File.ReadAllBytes("HotUpdate.dll");
Assembly asm = Assembly.Load(dllBytes);
```

内部流程：

1. `Assembly::LoadFromBytes` 接收 DLL 的字节数组
2. 创建 `InterpreterImage` 对象，解析 PE 头和 metadata stream
3. 解析 AssemblyRef 表，在已加载的 assembly（包括 AOT assembly 和之前加载的热更 assembly）中查找依赖
4. 注册新 assembly 到全局 assembly 列表中
5. 新 assembly 中的方法用解释器执行，AOT assembly 中的方法仍然走 native code

HybridCLR 同样不支持卸载。一旦加载，assembly 的 metadata 和 InterpreterImage 结构在整个进程生命周期内存在。

### LeanCLR：Assembly::load_by_name

LeanCLR 作为独立的 CLI 解释器，有自己的 assembly 加载管线：

1. `Assembly::load_by_name` 按名称查找 assembly 文件
2. 读取 PE 文件，解析 metadata 构建 `CliImage` 结构
3. 在 `CliImage` 基础上构建 `RtModuleDef`，包含运行时的类型和方法描述
4. 解析 AssemblyRef，递归加载依赖的 assembly

LeanCLR 的加载模型是纯解释器模型——所有代码都通过解释器执行，不存在 AOT/JIT 编译的 native code。这意味着它的加载逻辑比 HybridCLR 更简单（不需要处理 AOT 和解释器的混合执行），但执行性能更低。

### 五种加载模型对比

| 维度 | CoreCLR (ALC) | .NET Framework (AppDomain) | IL2CPP | HybridCLR | LeanCLR |
|------|--------------|---------------------------|--------|-----------|---------|
| **加载时机** | 运行时按需 | 运行时按需 | 构建时全量注册 | 运行时按需（热更部分） | 运行时按需 |
| **加载单元** | AssemblyLoadContext | AppDomain | MetadataCache 全局 | InterpreterImage | CliImage → RtModuleDef |
| **卸载支持** | 支持（collectible ALC） | 支持（卸载整个 AppDomain） | 不支持 | 不支持 | 不支持 |
| **隔离边界** | ALC 级别 | AppDomain 级别 | 无隔离 | 无隔离 | 无隔离 |
| **依赖解析** | ALC.Resolving 事件 | AppDomain.AssemblyResolve | 构建时静态解析 | 加载时查已注册 assembly | 加载时递归查找 |
| **执行方式** | JIT | JIT | AOT native code | AOT + 解释器混合 | 纯解释器 |

一个关键观察：卸载能力与执行方式强相关。JIT runtime 可以支持卸载——释放 JIT 产出的 native code 和相关的类型描述即可。AOT runtime 的代码在构建时就编译进了可执行文件，无法在运行时移除。解释器理论上可以支持卸载（释放解释器持有的 metadata 和 IR code），但 HybridCLR 和 LeanCLR 目前都没有实现这个能力。

## Type Forwarding

当一个类型从 assembly A 移到 assembly B 时，所有引用 A 中这个类型的代码都会因为 AssemblyRef + TypeRef 不再匹配而失败。Type forwarding 机制解决这个问题。

```csharp
// 原来 List<T> 在 mscorlib 中
// .NET Core 把它移到了 System.Collections.Generic

// mscorlib 中添加 type forwarder：
[assembly: TypeForwardedTo(typeof(System.Collections.Generic.List<>))]
```

Type forwarder 记录在 assembly 的 ExportedType 表（表编号 0x27）中，标记为 `IsTypeForwarder`。当 runtime 在 assembly A 中查找某个类型时，如果发现 ExportedType 表中有对应的 forwarder，就自动跳转到目标 assembly B 继续查找。

.NET BCL 在从 .NET Framework 迁移到 .NET Core 的过程中大量使用了 type forwarding。很多类型的实际位置从 mscorlib 移到了 System.Runtime、System.Collections 等更细粒度的 assembly 中，但通过 type forwarder 保持了对旧 AssemblyRef 的兼容。

在 Unity 的脚本后端切换（Mono → IL2CPP）过程中，type forwarding 也发挥作用——某些 BCL 类型在不同后端中位于不同的 assembly，type forwarder 保证了用户代码不需要修改 AssemblyRef。

## 这些概念在热更新场景中的意义

前面的内容是规范层的定义。把这些概念投射到 Unity 热更新场景中，可以回答几个实际问题。

### Assembly identity 是 placeholder assembly 的基础

HybridCLR 的工作模式是：构建时为热更 DLL 生成 placeholder assembly（桩程序集），这些 placeholder 参与 IL2CPP 的 AOT 编译，让 AOT 代码可以正确引用热更 DLL 中的类型。运行时再用真正的热更 DLL 替换 placeholder。

替换的匹配依据就是 assembly identity 中的 Name 字段。placeholder assembly 和热更 assembly 必须同名，runtime 才能把 AOT 代码中的 AssemblyRef 正确解析到热更 assembly。

### Assembly.Load 的顺序 = AssemblyRef 依赖图的拓扑排序

当项目有多个热更 DLL 时，加载顺序不是随意的。每个热更 DLL 的 AssemblyRef 表定义了它的依赖。如果 DLL A 依赖 DLL B，那么 B 必须在 A 之前加载——否则 A 在解析 AssemblyRef 时找不到 B，导致类型解析失败。

实际项目中，热更 DLL 的加载代码通常需要显式维护这个顺序：

```csharp
// 按依赖关系排序
string[] loadOrder = { "HotUpdate.Base", "HotUpdate.Logic", "HotUpdate.UI" };
foreach (var name in loadOrder)
{
    byte[] dll = LoadDllBytes(name);
    Assembly.Load(dll);
}
```

如果依赖关系复杂，可以解析每个 DLL 的 AssemblyRef 表自动计算拓扑排序，而不是手动维护加载列表。

### 加载模型的差异 = 热更新能力的上限

Assembly 加载模型直接决定了热更新的能力边界：

- IL2CPP 不支持运行时加载新 assembly → 没有热更新能力（所有代码必须在构建时确定）
- HybridCLR 扩展了运行时加载能力，但不支持卸载 → 可以热更新，但更新后的旧代码无法释放，长时间运行可能有内存问题
- CoreCLR 的 collectible ALC 支持加载和卸载 → 理论上支持完整的热替换（加载新版本 + 卸载旧版本），但 Unity 不使用 CoreCLR

这些限制不是某个 runtime 的"bug"或"功能缺失"——它们是加载模型设计决策的直接后果。AOT 模型的代码在构建时就编译进了二进制文件，运行时没有"移除"的概念。解释器模型理论上可以支持卸载，但需要处理"已创建的对象引用了被卸载的类型"这类复杂的生命周期问题。

## 收束

CLI Assembly Model 的核心可以归纳为三个层次：

**身份层。** 四元组（Name + Version + Culture + PublicKeyToken）唯一标识一个 assembly。AssemblyRef 表记录跨 assembly 的依赖关系。runtime 按四元组匹配目标 assembly，匹配失败就是 FileNotFoundException 或 TypeLoadException。

**版本层。** Major.Minor.Build.Revision 四段式版本号。.NET Framework 有三层绑定重定向机制（应用配置 / 发布者策略 / 机器配置），Unity/IL2CPP 场景下版本策略几乎不适用——所有 assembly 都是同一次构建的产出。

**加载层。** 不同 runtime 的加载模型决定了它们的能力边界。CoreCLR 的 ALC 支持加载和卸载，IL2CPP 只支持构建时注册，HybridCLR 扩展了运行时加载但不支持卸载，LeanCLR 支持运行时按需加载。加载模型的差异直接决定了热更新能力的上限。

这三个层次是理解后续每个 runtime 的 assembly 加载实现的前提。CoreCLR 的 AssemblyLoadContext 设计、IL2CPP 的 MetadataCache 初始化流程、HybridCLR 的 InterpreterImage 构建——都是在这同一套规范之上做出的不同工程选择。

## 系列位置

- 上一篇：ECMA-A4 CLI Execution Model：方法调用约定、虚分派、异常处理模型
- 下一篇：ECMA-A6 CLI Memory Model：对象布局、GC 契约、finalization 语义
