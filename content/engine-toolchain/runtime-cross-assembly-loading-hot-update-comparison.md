---
title: "横切对比｜程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载"
slug: "runtime-cross-assembly-loading-hot-update-comparison"
date: "2026-04-14"
description: "同一份 ECMA-335 程序集模型（identity + loading + versioning），在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出五种完全不同的加载策略：AssemblyLoadContext 动态加载与卸载、AppDomain 加载但不卸载、构建时静态绑定、运行时注入 InterpreterImage、运行时解析 CliImage。从加载方式、加载时机、卸载支持、隔离边界到热更新能力，逐维度横切对比五种程序集加载实现的设计 trade-off。"
weight: 86
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Assembly"
  - "HotUpdate"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> 热更新不是一个功能开关——能不能在运行时接入新代码，取决于 runtime 能不能加载新程序集、能不能解释或编译新代码、能不能卸载旧代码。这三个能力在五个 runtime 中的组合完全不同，构成了热更新可行性的根本边界。

这是 .NET Runtime 生态全景系列的横切对比篇第 7 篇（CROSS-G7），也是 Phase 3 横切对比的收尾篇。

CROSS-G6 对比了异常处理——五个 runtime 怎么在 throw 发生时找到 handler、展开栈帧、执行 finally。异常处理解决的是执行流的"中断与恢复"问题。程序集加载面对的是代码的"进入与退出"问题——新的代码怎么进入 runtime、旧的代码能不能离开 runtime。这个问题的答案直接决定了每个 runtime 的热更新能力边界。

## ECMA-335 的程序集模型

ECMA-335 Partition II 6 定义了 CLI 的程序集（Assembly）模型。程序集是 .NET 的部署、版本管理和类型可见性的基本单元。

### 程序集身份

每个程序集有一个四元组身份（Assembly Identity）：

- **Name：** 程序集的简单名称（如 `System.Collections`）
- **Version：** 四段版本号（Major.Minor.Build.Revision）
- **Culture：** 文化/区域设置（通常为 neutral）
- **PublicKeyToken：** 强命名密钥的公钥摘要（可选）

当代码引用另一个程序集中的类型时，引用方的 metadata 中记录了目标程序集的身份信息（AssemblyRef 表）。runtime 在类型解析时需要根据这个身份找到并加载对应的程序集。

### 加载语义

ECMA-335 定义了程序集加载的基本语义：

**按需加载。** 规范不要求 runtime 在启动时加载所有程序集。runtime 可以在第一次引用某个程序集中的类型时才加载它——惰性加载。

**唯一性。** 同一个程序集身份在一个加载域内只应加载一次。两次加载同一个身份应该得到同一个 Assembly 实例。

**可见性。** 程序集是类型可见性的边界。`internal` 类型只在定义它的程序集内部可见（除非通过 InternalsVisibleTo 显式放开）。

规范不规定具体的加载机制——不规定从哪里找到 DLL、不规定是否支持运行时加载新程序集、不规定是否支持卸载。每个 runtime 自行决定这些策略。

### 版本管理

规范定义了 AssemblyRef 中的版本信息和公钥标记，但"版本冲突怎么处理"留给 runtime 自行实现。CoreCLR 有复杂的版本绑定策略（配置文件、运行时回退），IL2CPP 在构建时就确定了所有版本——两种方案差异巨大。

## CoreCLR — AssemblyLoadContext + 动态加载与卸载

CoreCLR 的程序集加载是五个 runtime 中最完整的实现，提供了从加载到卸载的完整生命周期管理。

### AssemblyLoadContext

.NET Core 引入了 AssemblyLoadContext（ALC）取代了 .NET Framework 时代的 AppDomain 作为程序集隔离和加载的核心机制。

每个 ALC 是一个独立的加载上下文，维护自己的程序集集合。同一个程序集身份可以在不同的 ALC 中各加载一份——这意味着同一个类名在不同 ALC 中代表不同的类型（Type Identity 由程序集 + 类型名共同决定）。

```
Default ALC (不可卸载)
  ├─ System.Runtime.dll
  ├─ System.Collections.dll
  └─ MyApp.dll

Plugin ALC (可卸载)
  ├─ PluginA.dll
  └─ PluginA.Dependencies.dll

Another Plugin ALC (可卸载)
  └─ PluginB.dll
```

### Default ALC

每个 .NET 进程有一个默认的 Default ALC。应用启动时加载的程序集（主程序集和它的直接依赖）进入 Default ALC。Default ALC 不可卸载——加载进去的程序集在进程生命周期内永远存在。

Default ALC 使用一套标准的程序集探测逻辑（probing）来找到 DLL 文件：

1. 从应用目录（app base）查找
2. 从 NuGet 包缓存查找（deps.json 文件中描述的路径）
3. 从框架目录（shared framework）查找
4. 调用 AssemblyLoadContext.Resolving 事件（自定义解析）

### 动态加载

CoreCLR 支持在运行时动态加载新程序集：

```csharp
// 方式一：在默认 ALC 中加载
Assembly.LoadFrom("path/to/NewPlugin.dll");

// 方式二：在自定义 ALC 中加载（推荐）
var alc = new AssemblyLoadContext("PluginContext", isCollectible: true);
Assembly assembly = alc.LoadFromAssemblyPath("path/to/NewPlugin.dll");
```

动态加载后，新程序集中的类型立即可用——可以通过反射创建实例、调用方法。如果新程序集包含新的泛型组合，JIT 按需编译——不存在"构建时没见过"的问题。

### 动态卸载

CoreCLR 是五个 runtime 中唯一支持程序集卸载的。标记为 `isCollectible: true` 的 ALC 可以被卸载：

```csharp
var alc = new AssemblyLoadContext("PluginContext", isCollectible: true);
var weakRef = new WeakReference(alc);
// ... 使用 ALC 中的类型 ...
alc.Unload();  // 触发卸载
alc = null;

// 等待 GC 回收
GC.Collect();
GC.WaitForPendingFinalizers();
// weakRef.IsAlive == false 时，卸载完成
```

卸载不是立即完成的。`Unload()` 标记 ALC 为"待卸载"，但实际的内存回收需要等到所有对该 ALC 中类型的引用都被释放后，由 GC 在后续回收周期中完成。如果应用代码还持有该 ALC 中类型的实例或 Type 引用，卸载就不会完成——这是卸载泄露的常见原因。

卸载时，该 ALC 中所有程序集的 metadata、JIT 生成的 native code、类型系统结构（MethodTable 等）都会被回收。

### 版本管理

CoreCLR 通过 deps.json 和运行时配置处理版本绑定。多个 ALC 可以加载同一个程序集的不同版本——PluginA 依赖 Newtonsoft.Json 12.0，PluginB 依赖 Newtonsoft.Json 13.0，两者可以在各自的 ALC 中共存。

## Mono — 运行时加载但不支持卸载

Mono 的程序集加载能力介于 CoreCLR 和 IL2CPP 之间。

### 历史遗留：AppDomain

Mono 最初的程序集隔离机制是 AppDomain——.NET Framework 时代的概念。AppDomain 提供了一定程度的隔离，不同 AppDomain 中的类型互不可见，跨 AppDomain 的对象访问需要通过 remoting。

AppDomain 在理论上支持卸载（`AppDomain.Unload`），但实际实现中有诸多限制——无法保证干净卸载（线程可能还在执行 AppDomain 中的代码）、卸载后内存不一定真正释放、跨域引用导致泄露。

.NET Core 废弃了 AppDomain 的隔离和卸载能力（只保留默认 AppDomain 作为兼容性概念），改用 ALC。Mono 合并到 dotnet/runtime 后，也逐步走向 ALC 模型。但在 Unity 使用的 Mono 分支中，仍然沿用旧的加载模型。

### 运行时加载

Mono 支持运行时加载程序集：

```csharp
// Mono 的动态加载
Assembly.Load("MyPlugin");
Assembly.LoadFrom("path/to/plugin.dll");
```

加载后，Mono 的类型系统（MonoClass 等结构）被更新，新类型可以通过反射或直接代码引用使用。在 JIT 模式下，新类型的方法由 Mini JIT 按需编译。在解释器模式下，新方法由解释器执行。

### 不支持卸载

Unity 的 Mono 分支不支持程序集卸载。一旦程序集被加载，它的 metadata、类型信息、编译后的代码在进程生命周期内永远存在。

这意味着如果反复加载新版本的程序集（比如在开发迭代中），内存会持续增长——旧版本的程序集无法被回收。Unity 的"Domain Reload"机制是通过重新初始化整个脚本域来变相实现"卸载"的，但这不是真正的程序集卸载，而是整个运行环境的重启。

### Unity 中的实际状态

在 Unity 编辑器中，Mono 在 Domain Reload 时重新加载所有脚本程序集。这个过程涉及销毁所有 MonoBehaviour 实例、卸载旧的脚本域、创建新的脚本域、重新加载所有 DLL。这个机制保证了代码修改后编辑器能看到最新版本，但代价是 Domain Reload 可能花费数秒到数十秒。

在 Player（正式构建的游戏）中，Mono 不做 Domain Reload。程序集在启动时加载一次，之后不再变化。

## IL2CPP — 构建时静态绑定

IL2CPP 的程序集"加载"完全在构建时完成。运行时不存在加载新程序集的能力。

### 构建时绑定

il2cpp.exe 在构建时做的事情：

1. 读取所有 DLL 的 metadata
2. 解析所有 AssemblyRef，把跨程序集的类型引用解析为直接引用
3. 为每个程序集中的类型和方法生成 C++ 代码
4. 把所有程序集的 metadata 合并到 global-metadata.dat

构建完成后，"程序集"这个概念在运行时层面已经被消解了——所有类型和方法都是全局可见的 C++ 函数和结构体。程序集只作为 metadata 中的信息保留，用于反射查询。

### MetadataCache 全局表

IL2CPP 运行时通过 MetadataCache 管理所有 metadata。MetadataCache 在启动时从 global-metadata.dat 加载数据，构建类型和方法的全局索引表：

```
MetadataCache
  ├─ s_TypeInfoTable[]      ← 所有 Il2CppClass* 的全局数组
  ├─ s_MethodInfoTable[]    ← 所有 MethodInfo* 的全局数组
  ├─ s_AssemblyTable[]      ← 所有 Il2CppAssembly 的全局数组
  └─ s_ImageTable[]         ← 所有 Il2CppImage 的全局数组
```

"加载程序集"在 IL2CPP 中变成了"在全局表中找到对应的索引"——不涉及读取新的 DLL、解析新的 metadata、生成新的类型结构。所有这些工作在构建时已经完成。

### 不支持运行时加载

IL2CPP 不支持 `Assembly.LoadFrom` 或任何形式的运行时加载新程序集。调用这些 API 会抛出 `NotSupportedException`。

原因很明确：IL2CPP 是纯 AOT runtime，所有 native code 在构建时生成。运行时加载一个新 DLL 意味着需要把它的 CIL 编译成 native code——但 IL2CPP 没有 JIT，无法在运行时编译。

这个限制是 IL2CPP 架构的根本约束，不是实现上的疏漏。要让 IL2CPP 支持运行时加载，需要在运行时执行 il2cpp.exe 的转译管线 + C++ 编译器——这在移动端和主机端是不现实的。

### 版本管理

IL2CPP 不需要运行时版本管理。所有程序集版本在构建时已确定，运行时不存在版本冲突的可能性——因为根本不会加载新版本。

## HybridCLR — Assembly::LoadFromBytes + InterpreterImage

HybridCLR 在 IL2CPP 的静态绑定基础上，重新引入了运行时加载程序集的能力。这是 HybridCLR 实现热更新的基础。

### 加载路径

HybridCLR 的程序集加载通过 `Assembly.Load(byte[])` 触发：

```csharp
// 从文件读取热更 DLL 字节数据
byte[] dllBytes = File.ReadAllBytes("HotUpdate.dll");
// 加载到运行时
Assembly hotUpdateAssembly = Assembly.Load(dllBytes);
```

这个调用在 HybridCLR 内部的处理流程：

1. HybridCLR 拦截 IL2CPP 的 Assembly::Load 调用
2. 从字节数组解析 PE/CLI metadata——创建 InterpreterImage
3. InterpreterImage 注册到 IL2CPP 的 MetadataCache 中
4. 新程序集中的类型信息注入到 IL2CPP 的类型系统——创建 Il2CppClass 结构
5. 新方法标记为"由解释器执行"——MethodInfo 的 methodPointer 指向解释器入口

### InterpreterImage

InterpreterImage 是 HybridCLR 的核心数据结构之一。它在 IL2CPP 的 Image 体系中扮演热更程序集的 metadata 容器：

- 解析并缓存 TypeDef、MethodDef、FieldDef 等 metadata 表
- 为热更类型创建 Il2CppClass，插入 IL2CPP 的全局类型表
- 管理热更方法的 CIL 字节码，供解释器 transform 和执行

InterpreterImage 加载后，热更程序集中的类型在 IL2CPP 的类型系统中和 AOT 类型"看起来一样"——都有 Il2CppClass，都可以通过反射查询，都支持 GetType/typeof。差别在于方法执行路径：AOT 类型的方法走 native code，热更类型的方法走解释器。

### 不支持卸载（社区版）

HybridCLR 社区版不支持程序集卸载。加载的热更程序集在进程生命周期内永远存在。

这意味着如果在游戏运行过程中多次加载热更 DLL（比如每次热更推送一个新版本），旧版本的 metadata 和类型信息不会被回收。在实际游戏中，热更通常在启动阶段加载一次，不存在反复加载的场景。

### 热重载版（商业版）

HybridCLR 的商业版（热重载版）追加了程序集卸载能力，在理念上对标 CoreCLR 的 AssemblyLoadContext。热重载版支持在运行时卸载旧的热更程序集、加载新版本——让"不停服热更新"成为可能。

热重载版的卸载机制需要处理的核心问题：
- 旧版本类型的实例如何处置（需要迁移或销毁）
- 旧版本的 Il2CppClass 和 MethodInfo 如何从全局表中移除
- 仍在执行的旧版本方法如何安全中止

这些问题的解决方案是 HybridCLR 商业版的核心价值所在。

### 与 AOT 程序集的关系

HybridCLR 中，AOT 程序集和热更程序集共存。AOT 程序集的加载仍然走 IL2CPP 的原始路径（构建时静态绑定），热更程序集通过 InterpreterImage 动态加载。两类程序集共享同一个 MetadataCache 和类型系统——热更类型可以继承 AOT 类型、实现 AOT 接口、使用 AOT 泛型。

这种共存模型的优势是 AOT 代码的性能不受影响——只有热更代码走解释器。但也带来了复杂度：两套代码执行路径、两套异常处理机制、两套方法调用约定需要无缝衔接。

## LeanCLR — Assembly::load_by_name + CliImage + RtModuleDef

LeanCLR 的程序集加载是纯运行时操作，和 CoreCLR 在概念上最接近。

### 加载路径

LeanCLR 通过 `Assembly::load_by_name` 加载程序集：

1. 根据程序集名称在配置的搜索路径中找到 DLL 文件
2. 读取 DLL 文件，解析 PE 头和 CLI metadata
3. 创建 CliImage——LeanCLR 的 metadata 容器
4. 从 CliImage 构建 RtModuleDef——模块级别的运行时结构
5. 解析 TypeDef 表，按需创建 RtClass（惰性加载）

### CliImage 与 RtModuleDef

CliImage 是 LeanCLR 对 .NET DLL 的解析表示。它直接映射 ECMA-335 定义的 metadata 表结构——TypeDef、MethodDef、FieldDef、MemberRef 等。CliImage 的设计目标是忠实反映规范中的 metadata 模型，不做过多抽象。

RtModuleDef 是模块的运行时表示，管理该模块中所有类型的 RtClass。RtModuleDef 和 CliImage 之间的关系是：CliImage 提供静态的 metadata 查询接口，RtModuleDef 管理动态的运行时类型实例。

### 动态加载

LeanCLR 支持在运行时加载新程序集。加载后的程序集中的类型立即可用——可以通过反射或直接调用使用。新类型的方法由 LeanCLR 的双层解释器（HL-IL → LL-IL）执行。

和 CoreCLR 一样，LeanCLR 的动态加载不存在"构建时没见过"的问题。解释器的输入是 CIL 字节码，CIL 携带了完整的类型和方法描述信息。新加载的程序集中的任何类型——包括新的泛型组合——都可以在运行时被解释器处理。

### 不支持卸载

LeanCLR 当前不支持程序集卸载。加载的程序集在 runtime 生命周期内永远存在。

程序集卸载需要的基础设施（加载域隔离、类型引用追踪、代码生命周期管理）在 LeanCLR 的当前阶段不是优先事项。LeanCLR 的定位是轻量级嵌入式 runtime——嵌入场景下通常在启动时加载所有需要的程序集，不需要在运行过程中卸载。

### 与 HybridCLR 的对比

LeanCLR 和 HybridCLR 都支持运行时加载程序集并用解释器执行新代码。核心差异在于宿主环境：

**HybridCLR** 运行在 IL2CPP 之上，热更程序集和 AOT 程序集共存。加载路径需要把新程序集的 metadata 注入到 IL2CPP 的 MetadataCache 中，类型系统需要和 IL2CPP 的 Il2CppClass 体系兼容。

**LeanCLR** 是独立 runtime，所有程序集走相同的加载路径。不存在 AOT/解释器 的二元性——所有代码都是解释执行。这让程序集加载的实现更简单，但也意味着没有 native code 的性能。

## 热更新能力的本质

热更新不是 runtime 提供的一个开关。它是三个能力的组合：

### 第一个能力：能不能加载新程序集

运行时能否接受新的代码进入。

- **CoreCLR：** 能。ALC 支持动态加载
- **Mono：** 能。Assembly.Load 支持动态加载
- **IL2CPP：** 不能。构建时静态绑定，运行时不接受新代码
- **HybridCLR：** 能。InterpreterImage 支持动态加载
- **LeanCLR：** 能。CliImage + RtModuleDef 支持动态加载

### 第二个能力：能不能执行新代码

加载了新程序集后，runtime 能否运行其中的方法。

- **CoreCLR：** 能。JIT 按需编译新方法
- **Mono：** 能。JIT 编译或解释器执行
- **IL2CPP：** 不能。没有 JIT，没有解释器，无法执行未预编译的代码
- **HybridCLR：** 能。解释器执行热更方法
- **LeanCLR：** 能。双层解释器执行所有方法

### 第三个能力：能不能卸载旧代码

旧版本的程序集能否被移除，为新版本腾出空间。

- **CoreCLR：** 能。Collectible ALC 支持卸载
- **Mono：** 不能。Unity 分支不支持程序集卸载
- **IL2CPP：** 不能。不存在加载，自然不存在卸载
- **HybridCLR 社区版：** 不能。加载后永驻
- **HybridCLR 商业版：** 能。支持热重载
- **LeanCLR：** 不能。加载后永驻

### 组合矩阵

| runtime | 加载新代码 | 执行新代码 | 卸载旧代码 | 热更新能力 |
|---------|-----------|-----------|-----------|-----------|
| CoreCLR | 能 | 能（JIT） | 能（ALC） | 完整 |
| Mono | 能 | 能（JIT/interp） | 不能 | 可加载不可卸载 |
| IL2CPP | 不能 | 不能 | 不能 | 无 |
| HybridCLR 社区版 | 能 | 能（interp） | 不能 | 可加载不可卸载 |
| HybridCLR 商业版 | 能 | 能（interp） | 能 | 完整 |
| LeanCLR | 能 | 能（interp） | 不能 | 可加载不可卸载 |

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 加载方式 | ALC + Assembly.Load | Assembly.Load | 构建时静态绑定 | Assembly.Load → InterpreterImage | load_by_name → CliImage |
| 加载时机 | 运行时按需 | 运行时按需 | 构建时全量 | 运行时显式调用 | 运行时按需 |
| 卸载支持 | Collectible ALC | 不支持 | 不适用 | 社区版不支持 / 商业版支持 | 不支持 |
| 隔离边界 | ALC（类型隔离） | AppDomain（已弱化） | 无（全局） | 无（共享 MetadataCache） | 无（全局 RtModuleDef） |
| 热更新能力 | 完整 | 可加载 | 无 | 可加载（社区）/ 完整（商业） | 可加载 |
| 版本管理 | deps.json + ALC 隔离 | GAC + config | 构建时确定 | 无版本管理 | 无版本管理 |
| 新代码执行 | JIT 按需编译 | JIT / 解释器 | 不支持 | 解释器 | 双层解释器 |
| metadata 存储 | 内存映射 DLL | 内存映射 DLL | global-metadata.dat | InterpreterImage | CliImage |

## 为什么热更新是 AOT runtime 的天然难题

五个 runtime 的程序集加载能力差异，本质上是 JIT、AOT、解释器三种执行策略在"运行时代码引入能力"上的直接分野。

### JIT 的天然优势

JIT runtime 天生支持热更新——新代码进来，JIT 编译一下就能执行。CoreCLR 的 ALC 机制在此基础上还提供了隔离和卸载能力。这是 .NET 在服务端场景下做插件系统、动态加载模块的基础。

但 JIT 在移动端和主机端受限（iOS 禁止 JIT，PlayStation/Switch/Xbox 禁止运行时代码生成）。这个平台限制把大量游戏项目推向了 AOT 方案。

### AOT 的天然困境

AOT runtime 在构建时生成所有 native code，运行时没有代码生成能力。新的代码进来后，runtime 看得懂 metadata（如果有加载能力的话），但没法执行方法体——因为方法体需要被编译成 native code 才能执行，而 AOT 模式下运行时不能编译。

这就是 IL2CPP 无法热更新的根本原因。不是 Unity 不想做——而是 AOT 架构本身排斥了运行时引入新代码的可能性。

### 解释器的桥梁作用

解释器打破了 AOT 的代码引入困境。解释器的输入是 CIL 字节码，不需要编译成 native code。新的程序集加载后，其中的方法可以直接被解释器执行。

这就是 HybridCLR 的核心价值——在 IL2CPP 这个纯 AOT runtime 上叠加一个 CIL 解释器，让运行时代码引入重新成为可能。AOT 负责主体代码的高性能执行，解释器负责热更代码的动态引入。

LeanCLR 走了另一条路——干脆全部用解释器，不依赖 AOT。代价是所有代码都走解释器速度，收益是架构极度简化、跨平台无障碍。

### 卸载的额外复杂度

加载新代码已经不容易，卸载旧代码更难。卸载需要解决的问题包括：

**引用追踪。** runtime 需要知道是否还有任何代码或数据引用了待卸载程序集中的类型。如果一个 AOT 方法的参数类型来自热更程序集，卸载该程序集会导致类型系统崩溃。

**实例迁移。** 待卸载程序集中的类型可能有存活的实例。这些实例的 Il2CppClass 指针指向待卸载的类型信息——直接回收会导致悬空指针。

**代码中止。** 如果某个线程正在执行待卸载程序集中的方法，直接回收方法体的字节码会导致解释器崩溃。

CoreCLR 通过 ALC 的引用计数和 GC 协作解决这些问题——ALC 只有在所有引用都释放后才会被 GC 真正回收。HybridCLR 商业版需要在 IL2CPP 的类型系统上实现类似的引用追踪和安全卸载机制，工程复杂度很高。

## 收束 — Phase 3 完结篇

同一份 ECMA-335 程序集模型，五个 runtime 给出了五种加载策略。差异的根源是各自的执行策略决定了运行时能否引入和执行新代码：

- CoreCLR 有 JIT + ALC，能加载、能执行、能卸载——热更新能力最完整
- Mono 能加载、能执行，但不能卸载——功能位于中间地带
- IL2CPP 在构建时静态绑定一切，运行时不接受新代码——热更新能力为零
- HybridCLR 在 IL2CPP 上叠加解释器，重新打开了运行时代码引入的通道，商业版进一步追加了卸载能力
- LeanCLR 用纯解释器路线天然支持动态加载，但卸载不在当前优先级

热更新不是一个可以独立讨论的功能。它是程序集加载能力、代码执行能力、代码卸载能力三者的交集。理解了每个 runtime 在这三个维度上的位置，就理解了为什么 Unity 生态需要 HybridCLR——IL2CPP 提供了 AOT 性能，但天然排斥热更新；解释器补上了动态代码引入的缺口；两者结合才构成了一个在移动端可行的热更新方案。

## 系列位置

这是横切对比篇第 7 篇（CROSS-G7），也是 Phase 3 横切对比的收尾篇。

Phase 1 的三篇横切对比（CROSS-G1 metadata 解析、G2 类型系统、G3 方法执行）覆盖了 runtime 的基础层三大环节。Phase 2 的两篇（CROSS-G4 GC 实现、G5 泛型实现）覆盖了 GC 和泛型两个跨 runtime 差异最显著的维度。Phase 3 的两篇（CROSS-G6 异常处理、G7 程序集加载与热更新）覆盖了异常处理和程序集加载——这两个维度直接关系到热更新工程实践中最常遇到的跨 runtime 行为差异。

七篇横切对比从 metadata 解析、类型系统、方法执行、GC、泛型、异常处理到程序集加载与热更新，七个维度覆盖了"同一份 ECMA-335 规范在五个 runtime 中的实现分化"这条主线的核心横截面。最后一篇 CROSS-G8 将对比体积与嵌入性——从 50MB 的 CoreCLR 到 300KB 的 LeanCLR，五个 runtime 的裁剪策略和嵌入成本。
