---
title: "CoreCLR 实现分析｜程序集加载：AssemblyLoadContext、Binder 与卸载支持"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解程序集加载的完整链路：AssemblyLoadContext 的双模设计（Default 不可卸载 + Custom 可卸载）、AssemblyBinder 的名字解析与文件定位、Assembly.Load 到 TypeLoading 的六步链路、Collectible ALC 的协作式卸载机制与限制，以及与 IL2CPP / HybridCLR / LeanCLR / Mono 的加载能力对比。"
weight: 41
featured: false
tags:
  - "CoreCLR"
  - "CLR"
  - "Assembly"
  - "Loading"
  - "AssemblyLoadContext"
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> CoreCLR 的 AssemblyLoadContext 是所有主流 CLR 实现中唯一原生支持程序集卸载的加载机制——这件事 IL2CPP 做不到，Mono 做不到，HybridCLR 的热重载版本正在追的也是这个能力。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 2 篇。

上一篇 B1 讲的是 CoreCLR 从 `dotnet run` 到 JIT 执行的启动链路。那条链路走到 `coreclr_execute_assembly` 之后，第一件实际要做的事情就是加载程序集。这篇接上那个节点，拆解 CoreCLR 的 assembly 加载机制——从 `AssemblyLoadContext` 的设计，到 `AssemblyBinder` 的名字解析，到卸载的协作式回收模型，再到与其他四种 runtime 的对比。

## Assembly 加载在 CoreCLR 中的位置

CoreCLR 的启动链路可以简化为四步：

```
Host (dotnet/apphost)
  → coreclr_initialize  → 创建 AppDomain + Default ALC
  → coreclr_execute_assembly → 加载入口 assembly
  → JIT 编译 Main 方法 → 开始执行
```

`coreclr_initialize` 完成 runtime 的基础初始化——GC、线程池、JIT 编译器。但初始化完成后，runtime 内部还没有任何用户代码。用户代码进入 runtime 的唯一入口就是 assembly 加载。

在 CoreCLR 的实现中，assembly 加载不是一个简单的"读文件"操作。它涉及三个子系统的协作：

- **AssemblyLoadContext** — 加载的管理容器，决定 assembly 的隔离边界和生命周期
- **AssemblyBinder** — 名字解析和文件定位引擎，决定 assembly name 映射到哪个物理文件
- **PEAssembly / Module / TypeLoading** — 加载后的 metadata 解析和类型注册

这三个子系统的源码主要分布在 `src/coreclr/vm/` 和 `src/coreclr/binder/` 目录下。

## AssemblyLoadContext 的设计

AssemblyLoadContext（ALC）是 .NET Core 引入的程序集加载和隔离模型。它替代了 .NET Framework 的 AppDomain 加载隔离功能，但设计目标完全不同——ALC 关注的是加载隔离和卸载能力，不关注安全隔离。

### Default ALC 与 Custom ALC

CoreCLR 维护两类 ALC：

**Default ALC。** runtime 启动时自动创建，全局唯一，不可卸载。所有通过 `Assembly.Load(AssemblyName)` 默认加载的 assembly 都进入 Default ALC。framework 自身的 BCL assembly（`System.Runtime`、`System.Collections` 等）也在这里。Default ALC 的生命周期等于进程的生命周期。

**Custom ALC。** 用户代码通过继承 `AssemblyLoadContext` 或直接构造 `new AssemblyLoadContext(name, isCollectible)` 创建。Custom ALC 有两个关键参数：

- `name` — 调试标识，不影响加载逻辑
- `isCollectible` — 是否支持卸载。`true` 时该 ALC 中的所有 assembly 可以被整体卸载

```csharp
// 创建一个可卸载的 Custom ALC
var pluginContext = new AssemblyLoadContext("PluginLoader", isCollectible: true);
Assembly pluginAsm = pluginContext.LoadFromAssemblyPath("/plugins/MyPlugin.dll");

// 使用插件类型...
Type entryType = pluginAsm.GetType("MyPlugin.Entry");
object instance = Activator.CreateInstance(entryType);

// 卸载整个 ALC
pluginContext.Unload();
```

在 CoreCLR 源码中，ALC 的 native 实现对应 `src/coreclr/vm/assemblyloadcontext.cpp` 中的 `AssemblyLoadContext` 类。Default ALC 对应 `DefaultAssemblyLoadContext`，Custom ALC 对应 `CustomAssemblyLoadContext`，后者多出了 collectible 支持的相关逻辑。

### 隔离边界：同一类型在不同 ALC 中是不同 Type

ALC 的隔离不只是"不同 ALC 可以加载同名 assembly"这么简单。更关键的语义是：**同一个 DLL 文件被两个不同的 ALC 加载后，其中定义的同名类型在 runtime 看来是两个完全不同的 Type**。

```csharp
var alc1 = new AssemblyLoadContext("ctx1");
var alc2 = new AssemblyLoadContext("ctx2");

Assembly asm1 = alc1.LoadFromAssemblyPath("/path/to/Plugin.dll");
Assembly asm2 = alc2.LoadFromAssemblyPath("/path/to/Plugin.dll");

Type t1 = asm1.GetType("Plugin.DataModel");
Type t2 = asm2.GetType("Plugin.DataModel");

// t1 != t2 —— 即使它们来自同一个 DLL 文件
// t1 的实例不能被 cast 成 t2 类型
```

这个行为的根源在 CoreCLR 的类型系统里。每个 `MethodTable`（CoreCLR 中描述类型的核心结构）都绑定了它所属的 `Module`，而 `Module` 又绑定了它所属的 `AssemblyLoadContext`。类型相等性的判断走 `MethodTable` 指针比较——不同 ALC 中加载的同名类型有不同的 `MethodTable`，所以它们不相等。

这个设计是有意为之。隔离边界的存在让 runtime 可以安全地加载多个版本的同名 assembly，每个版本在自己的 ALC 中独立运行。插件系统是最典型的使用场景——不同插件可能依赖同一个库的不同版本，ALC 隔离保证它们互不干扰。

## Assembly Binder

AssemblyBinder 是 ALC 内部的核心引擎，负责把一个 assembly name 解析到一个具体的文件路径，然后触发加载。CoreCLR 的 Binder 实现在 `src/coreclr/binder/` 目录下。

### Binder 的三步职责

Binder 在每次 assembly 加载请求中执行三步操作：

**名字解析。** 接收一个 `AssemblyName`（包含 Name、Version、Culture、PublicKeyToken），确定需要加载哪个 assembly。这一步不涉及文件系统。

**文件定位。** 在已知的探测路径中查找匹配的文件。探测路径的来源有两个：
- TPA 列表（Trusted Platform Assemblies）
- App 目录和额外的探测路径

**加载。** 找到文件后，读取 PE/COFF 头，验证 assembly identity，触发后续的 metadata 解析。

### TPA（Trusted Platform Assemblies）列表

TPA 是 CoreCLR 特有的概念，定义在 `src/coreclr/binder/assemblybindercommon.cpp` 中。

当 `dotnet` host 启动 CoreCLR 时，会通过 `coreclr_initialize` 传入一组属性，其中 `TRUSTED_PLATFORM_ASSEMBLIES` 属性是一个分号分隔的文件路径列表，包含了所有 framework assembly 的完整路径：

```
/usr/share/dotnet/shared/Microsoft.NETCore.App/9.0.0/System.Runtime.dll;
/usr/share/dotnet/shared/Microsoft.NETCore.App/9.0.0/System.Collections.dll;
/usr/share/dotnet/shared/Microsoft.NETCore.App/9.0.0/System.Linq.dll;
...
```

TPA 列表是 Default ALC 的 Binder 首先搜索的位置。当代码中出现 `Assembly.Load("System.Collections")` 时，Binder 先在 TPA 列表中按名字匹配，找到对应的完整路径，然后从该路径加载。

TPA 的设计让 CoreCLR 摆脱了 .NET Framework 时代的 GAC（Global Assembly Cache）。GAC 是一个机器级别的 assembly 注册中心，管理复杂、容易出错。TPA 把"哪些 assembly 可用"变成了一个启动时传入的扁平列表，运维更简单，行为更可预测。

### Custom ALC 的 Binder 逻辑

Custom ALC 有自己的加载逻辑。当 Custom ALC 中的代码触发 assembly 加载时，搜索顺序是：

```
1. ALC.Load(AssemblyName) — 用户重写的加载方法
2. Default ALC（回退查找）
3. ALC.Resolving 事件（最后的自定义机会）
```

第一步是关键的扩展点。用户通过继承 `AssemblyLoadContext` 并重写 `Load` 方法，可以完全控制 assembly 的来源：

```csharp
class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDir;

    public PluginLoadContext(string pluginDir)
        : base("PluginLoader", isCollectible: true)
    {
        _pluginDir = pluginDir;
    }

    protected override Assembly Load(AssemblyName name)
    {
        string path = Path.Combine(_pluginDir, $"{name.Name}.dll");
        if (File.Exists(path))
            return LoadFromAssemblyPath(path);
        return null; // 返回 null 表示让 Default ALC 处理
    }
}
```

返回 `null` 时，runtime 自动回退到 Default ALC 查找。这保证了 BCL assembly 不需要在每个 Custom ALC 中重复加载——它们始终从 Default ALC 获取。

## 加载链路

从 C# 的 `Assembly.Load(name)` 到 runtime 内部完成类型注册，中间经过六个关键阶段。以下是 CoreCLR 源码中的实际调用链路：

```
Assembly.Load(AssemblyName name)                          [managed]
  → AssemblyLoadContext.Load(name)                        [managed, 用户可重写]
    → AssemblyBinder::BindAssembly(name)                  [native, binder/]
      → AssemblyBinderCommon::BindByName(name)            [native, 名字解析 + 文件定位]
        → PEAssembly::Open(path)                          [native, vm/peassembly.cpp]
          → Module::Create(peAssembly)                    [native, vm/ceeload.cpp]
            → ClassLoader::LoadTypeHandleForTypeKey(...)  [native, vm/clsload.cpp, 按需]
```

逐步拆解：

**第一步：Assembly.Load 进入 ALC。** 托管层的 `Assembly.Load` 调用转发给当前 ALC 的 `Load` 方法。如果当前代码在 Default ALC 中，走 Default ALC 的逻辑；如果在 Custom ALC 中，先走用户重写的 `Load`。

**第二步：Binder 执行名字解析。** ALC 内部调用 AssemblyBinder，传入 AssemblyName。Binder 在 TPA 列表和探测路径中查找匹配的文件。源码位于 `src/coreclr/binder/assemblybindercommon.cpp` 的 `BindByName` 方法。

**第三步：PEAssembly 加载。** 找到物理文件后，runtime 创建 `PEAssembly` 对象。`PEAssembly` 负责解析 PE/COFF 文件头、定位 metadata 根、验证 assembly identity（确认文件中的 Assembly 表记录与请求的 AssemblyName 匹配）。源码在 `src/coreclr/vm/peassembly.cpp`。

**第四步：Module 创建。** 在 `PEAssembly` 基础上创建 `Module` 对象。`Module` 是 CoreCLR 中管理 metadata 的核心容器——它持有所有 metadata 表的解析结果，提供 TypeDef / MethodDef / FieldDef 等 token 的查找能力。源码在 `src/coreclr/vm/ceeload.cpp`。

**第五步：Assembly 注册。** `Module` 被注册到所属的 ALC 中。此时 assembly 的 metadata 已可查询，但其中的类型尚未被完整加载。

**第六步：TypeLoading（按需）。** 当代码首次访问 assembly 中的某个类型时，`ClassLoader` 被触发，从 `Module` 的 TypeDef 表中读取类型信息，构建 `MethodTable` 和 `EEClass`。这个过程是惰性的——不访问的类型不会被加载。

这里有一个实际工程中常见的误解需要澄清：`Assembly.Load` 完成后，assembly 中的所有类型并没有被立即加载。Assembly.Load 只完成了 metadata 层面的准备。真正的类型加载（构建 MethodTable、解析字段布局、准备虚方法表）发生在首次使用时。这就是为什么 Assembly.Load 本身的性能通常不是瓶颈——类型加载的成本被分摊到了后续的首次访问中。

## 卸载机制

CoreCLR 的卸载能力是它在所有主流 CLR 实现中最独特的设计之一。这个能力通过 Collectible ALC 实现。

### Collectible ALC 的卸载流程

当调用 `pluginContext.Unload()` 时，并不会立即释放所有资源。实际的卸载流程是协作式的：

```
ALC.Unload()
  → 标记 ALC 为 unloading 状态
  → 等待所有强引用被释放
  → GC 回收所有托管对象
  → 释放 metadata 结构（Module、MethodTable、EEClass）
  → 释放 JIT 编译产出的 native code（CodeHeap）
  → 释放 ALC 本身
```

"协作式"意味着 `Unload()` 是一个触发操作，不是一个同步完成操作。runtime 不会强制终止正在执行的代码或强制回收仍被引用的对象。实际的资源释放发生在后续的 GC 周期中，条件是所有指向该 ALC 中类型和对象的强引用都已经被释放。

### 为什么是协作式卸载

强制卸载在技术上是不可行的。考虑以下场景：

```csharp
var alc = new AssemblyLoadContext("plugin", isCollectible: true);
Assembly asm = alc.LoadFromAssemblyPath("/plugins/Plugin.dll");
Type t = asm.GetType("Plugin.Worker");
object worker = Activator.CreateInstance(t);

// worker 被传递到其他代码中持有
SomeService.Register(worker);

alc.Unload(); // 调用卸载
// 但 SomeService 仍然持有 worker 的引用
// 如果强制释放 worker 的 MethodTable，SomeService 下次调用 worker 就会崩溃
```

强制释放正在被引用的类型的 metadata 结构会导致 runtime 崩溃——调用方尝试通过已释放的 `MethodTable` 分派方法，访问的是无效内存。所以 CoreCLR 选择了协作式模型：`Unload()` 只是发出卸载请求，真正的释放等到所有引用都断开后由 GC 完成。

在 CoreCLR 源码中，collectible assembly 的资源管理通过 `LoaderAllocator`（`src/coreclr/vm/loaderallocator.cpp`）实现。每个 collectible ALC 有自己的 `LoaderAllocator`，管理该 ALC 中所有 metadata 结构和 JIT code 的内存分配。当 GC 发现 `LoaderAllocator` 没有外部强引用时，整个分配器及其管理的内存被一次性回收。

### JIT code 的释放

卸载需要释放的不只是 metadata 结构。JIT 为每个方法编译产出的 native code 也需要释放。

在 Default ALC 中，JIT code 分配在全局的 `CodeHeap` 上，不支持单独释放（因为 Default ALC 不可卸载）。在 collectible ALC 中，JIT code 分配在该 ALC 专属的 `LoaderAllocator` 管理的内存区域上。卸载时，整个区域被整体释放——这避免了在 CodeHeap 中做碎片化的逐方法释放。

## 卸载的限制

Collectible ALC 的卸载能力有明确的边界条件。不了解这些限制，就容易在生产环境中遇到 ALC 卸载不完整导致的内存泄漏。

### Default ALC 不可卸载

Default ALC 的 `isCollectible` 永远为 `false`。所有通过默认加载路径进入的 assembly——包括入口 assembly、BCL assembly、NuGet 包——都在 Default ALC 中，无法在运行时释放。

这个限制是有意为之。Default ALC 承载了 runtime 的基础设施，如果允许卸载，任何 BCL 类型的释放都可能导致连锁崩溃。

### 跨 ALC 引用导致无法释放

这是实际项目中最常见的卸载失败原因。

```csharp
var alc = new AssemblyLoadContext("plugin", isCollectible: true);
Assembly asm = alc.LoadFromAssemblyPath("/plugins/Plugin.dll");

// 跨 ALC 引用：Default ALC 中的代码持有 Custom ALC 中的对象
Type t = asm.GetType("Plugin.Worker");
_workers.Add(Activator.CreateInstance(t));  // _workers 在 Default ALC 中

alc.Unload();
// 卸载请求已发出，但 _workers 持有的引用阻止 GC 回收
// ALC 实际上无法完成卸载，metadata 和 JIT code 持续占用内存
```

解决方案是在卸载前清除所有跨 ALC 的引用。常见模式是通过接口解耦——让 Custom ALC 中的类型实现 Default ALC 中定义的接口，卸载前把接口引用置为 null。

### static 变量持有对象导致泄漏

如果 Custom ALC 中加载的 assembly 里有 static 字段持有对象引用，这些 static 字段的生命周期与包含它们的类型绑定。而类型的 `MethodTable` 在 ALC 卸载前不会被释放——形成循环依赖。

```csharp
// Plugin.dll（在 collectible ALC 中加载）
public class PluginCache
{
    static readonly List<object> _cache = new();  // static 字段

    public static void Add(object item) => _cache.Add(item);
}
```

`_cache` 是 static 字段，它的生命周期绑定在 `PluginCache` 的 `MethodTable` 上。而 `_cache` 中持有的对象又可能引用 ALC 中的其他类型。GC 在扫描根时发现 `_cache` 仍然可达，所以不会回收 ALC 的 `LoaderAllocator`。

实际项目中处理这个问题的标准做法是：在卸载前显式调用清理逻辑，把 static 字段置为 null。

### 线程仍在执行 ALC 中的代码

如果有线程正在执行 Custom ALC 中的方法（方法的 JIT code 正在被 CPU 执行），卸载也无法完成——强制释放正在执行的代码会导致立即崩溃。

runtime 在 `Unload()` 之后会等待所有线程离开该 ALC 的代码。如果某个线程被阻塞（比如在 `Thread.Sleep` 或等待锁），卸载就会一直挂起。

## 与其他 runtime 的对比

程序集加载是区分不同 runtime 能力边界最清晰的维度之一。以下是五种 runtime 在加载机制上的关键差异。

### IL2CPP：MetadataCache::LoadAssemblyFromBytes

IL2CPP 的 assembly 加载发生在构建时。`il2cpp.exe` 把所有 assembly 的 metadata 合并写入 `global-metadata.dat`。运行时 `MetadataCache::Initialize` 读取这个文件，为每个 assembly 创建 `Il2CppAssembly` 结构并注册到全局表中。

运行时调用 `Assembly.Load(name)` 时，`MetadataCache` 在已注册的 assembly 中按名字查找——这是纯查表操作，不涉及文件 I/O。

IL2CPP 不支持卸载。所有 assembly 在 runtime 初始化时全量注册，生命周期等于进程生命周期。metadata 结构和 AOT native code 都嵌入在二进制文件中，运行时没有"移除"的概念。这是 AOT 模型的固有约束，不是实现上的遗漏。

### HybridCLR：Assembly::LoadFromBytes -> InterpreterImage

HybridCLR 在 IL2CPP 基础上扩展了运行时加载能力。`Assembly::LoadFromBytes` 接收 DLL 的字节数组，创建 `InterpreterImage` 对象解析 PE 头和 metadata stream，将新 assembly 注册到全局 assembly 列表中。

HybridCLR 不支持卸载。一旦加载，assembly 的 `InterpreterImage` 和 metadata 结构在整个进程生命周期存在。HybridCLR 的商业热重载版本正在追的核心能力之一就是 assembly 卸载——但在 IL2CPP 的 append-only metadata 架构上实现卸载，比 CoreCLR 的 collectible ALC 模型要困难得多，因为 IL2CPP 的 metadata 结构没有按加载单元分组管理的概念。

### LeanCLR：Assembly::load_by_name -> CliImage -> RtModuleDef

LeanCLR 的加载链路是：`Assembly::load_by_name` 按名称查找 assembly 文件，读取 PE 文件解析 metadata 构建 `CliImage` 结构，再在 `CliImage` 基础上构建 `RtModuleDef`，包含运行时的类型和方法描述。依赖通过 AssemblyRef 表递归加载。

LeanCLR 当前不支持卸载。作为纯解释器 runtime，它在技术上比 IL2CPP 更容易实现卸载——解释器的 IR code 和 metadata 结构都在运行时动态分配，理论上可以按加载单元释放。但当前设计中没有引入类似 ALC 的生命周期管理机制。

### Mono：不支持卸载

Mono 在 .NET Framework 时代通过 AppDomain 提供隔离能力，但 AppDomain 的卸载不是真正的 assembly 卸载——它卸载的是整个 AppDomain，包括其中所有 assembly 和对象。单个 assembly 的卸载在 Mono 中从未被实现。

在合并到 dotnet/runtime 之后，Mono 的 AppDomain 支持被弱化到与 CoreCLR 对齐——只有一个 Default AppDomain，不支持创建和卸载额外的 AppDomain。Mono 也没有引入自己的 AssemblyLoadContext 卸载支持。

### 加载能力对比

| 维度 | CoreCLR | IL2CPP | HybridCLR | LeanCLR | Mono |
|------|---------|--------|-----------|---------|------|
| **加载时机** | 运行时按需 | 构建时全量注册 | 运行时按需 | 运行时按需 | 运行时按需 |
| **隔离机制** | ALC | 无 | 无 | 无 | 无（AppDomain 已废弃） |
| **卸载支持** | Collectible ALC | 不支持 | 不支持 | 不支持 | 不支持 |
| **Binder 机制** | TPA + 探测路径 | MetadataCache 查表 | 已注册 assembly 查表 | 文件系统查找 | GAC + 探测路径 |
| **加载后的结构** | PEAssembly → Module | Il2CppAssembly | InterpreterImage | CliImage → RtModuleDef | MonoAssembly → MonoImage |

最关键的差异在卸载行。CoreCLR 之所以能实现卸载，根源在于它的内存管理架构：每个 collectible ALC 有独立的 `LoaderAllocator` 管理所有 metadata 和 JIT code 的分配。卸载时整体回收这个分配器即可。其他四个 runtime 的 metadata 结构都分配在全局内存空间中，没有按加载单元分组——这使得按 assembly 粒度释放在架构上很难实现。

## AppDomain 的历史与淘汰

理解 ALC 的设计需要了解它替代了什么。.NET Framework 的 AppDomain 是 ALC 的前身，但两者的设计哲学差异很大。

### AppDomain 做了什么

.NET Framework 用 AppDomain 提供进程内隔离。每个 AppDomain 有自己的一组已加载 assembly，跨 AppDomain 的对象传递需要序列化（`MarshalByRefObject`）或值复制。AppDomain 可以被卸载，卸载时其中所有 assembly 和对象被释放。

```csharp
// .NET Framework 时代的 AppDomain 用法
AppDomain pluginDomain = AppDomain.CreateDomain("PluginDomain");
pluginDomain.ExecuteAssembly("/plugins/Plugin.exe");
AppDomain.Unload(pluginDomain);
```

### 为什么 AppDomain 被淘汰

AppDomain 被淘汰有三个核心原因：

**性能成本过高。** 跨 AppDomain 调用需要序列化/反序列化，即使传递一个简单的字符串也有显著开销。在高频调用场景下，这个成本不可接受。

**隔离不完整。** AppDomain 在规范上声称提供隔离，但实际上 native 代码（P/Invoke）、非托管资源、进程级全局状态都不在隔离范围内。一个 AppDomain 中的 native 代码崩溃仍然会导致整个进程崩溃。这种"看起来隔离但实际不完整"的状态比没有隔离更危险——它给了开发者虚假的安全感。

**实现复杂度过高。** AppDomain 的存在迫使 runtime 中几乎所有子系统都要感知 AppDomain 边界——类型加载、GC、异常处理、线程管理都需要额外的 AppDomain 感知逻辑。这极大地增加了 runtime 的复杂度和 bug 数量。

.NET Core 从 1.0 开始就放弃了 AppDomain 的隔离和卸载功能。`AppDomain.CurrentDomain` 仍然存在（为了兼容性），但不能创建新的 AppDomain，`AppDomain.Unload` 直接抛出 `PlatformNotSupportedException`。

ALC 保留了 AppDomain 中真正有用的两个能力——加载隔离和卸载——但去掉了跨域序列化的开销和安全隔离的虚假承诺。在 ALC 模型中，不同 ALC 中的对象可以直接互相引用（同一进程内的同一托管堆），不需要序列化。代价是失去了隔离的"安全"语义——但既然 AppDomain 的安全隔离本身就是不完整的，这个代价实际上很小。

## 收束

CoreCLR 的程序集加载机制围绕三个核心概念构建：

**AssemblyLoadContext 定义加载的边界和生命周期。** Default ALC 承载 runtime 基础设施，不可卸载。Custom ALC 提供隔离边界，collectible 模式支持协作式卸载。同一类型在不同 ALC 中是不同的 Type，这是隔离的运行时语义。

**AssemblyBinder 完成名字到文件的映射。** TPA 列表取代了 .NET Framework 的 GAC，让 assembly 探测变得扁平化和可预测。Custom ALC 可以重写 Load 方法完全控制加载来源，找不到时回退 Default ALC。

**Collectible ALC 实现了协作式卸载。** 调用 `Unload()` 标记卸载意图，实际释放由 GC 在所有强引用断开后完成。`LoaderAllocator` 按 ALC 粒度管理 metadata 和 JIT code 的内存，卸载时整体回收。跨 ALC 引用、static 变量、线程在执行中的代码都会阻止卸载完成。

这套设计在五种 .NET runtime 中是独一份的。IL2CPP、HybridCLR、LeanCLR、Mono 都不支持 assembly 级别的卸载。根源在于 CoreCLR 从架构上就用 `LoaderAllocator` 做了按加载单元的内存分组，其他 runtime 的 metadata 和代码分配在全局空间中，缺乏按加载单元回收的基础设施。

## 系列位置

- 上一篇：CLR-B1 CoreCLR 架构总览：从 dotnet run 到 JIT 执行
- 下一篇：CLR-B3 类型系统：MethodTable、EEClass、TypeHandle
