---
title: "IL2CPP 实现分析｜ECMA-335 覆盖度：哪些支持、哪些不支持、为什么"
slug: "il2cpp-ecma335-coverage-supported-unsupported"
date: "2026-04-14"
description: "系统梳理 IL2CPP 对 ECMA-335 规范的覆盖情况：完全支持的特性（泛型、异常处理、值类型、接口、委托、async/await、Span）、部分支持的特性（Reflection、P/Invoke）、不支持的特性（Reflection.Emit、Assembly.Load、AppDomain、动态代码生成）。从 AOT 模型的根因出发，解释每一项限制背后的技术约束，并对比 HybridCLR、CoreCLR、Mono、LeanCLR 的覆盖度差异。"
weight: 56
featured: false
tags:
  - IL2CPP
  - Unity
  - ECMA-335
  - Compatibility
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> IL2CPP 不是完整的 CLR 实现——它是一个面向 AOT 场景的 ECMA-335 子集实现。理解它的覆盖边界，才能理解 HybridCLR 在补什么、LeanCLR 在做什么、CoreCLR 为什么不需要补。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 7 篇，从规范合规性角度审视 IL2CPP 的实现边界。

D1 到 D5 覆盖了 IL2CPP 的管线架构、转换器、runtime 基础设施、metadata 格式和泛型代码生成。D6 分析了 GC 集成层。这些篇幅解释了 IL2CPP 如何工作，但没有系统回答一个更基础的问题：ECMA-335 规范定义了哪些能力，IL2CPP 实现了其中的多少？

这个问题不是学术兴趣。Unity 工程师在开发中遇到的很多运行时异常——`TypeLoadException`、`MissingMethodException`、`PlatformNotSupportedException`——本质上都是撞到了 IL2CPP 的覆盖度边界。理解这个边界，才能在架构设计阶段就避开陷阱，而不是在运行时才发现某个 API 不可用。

## 为什么覆盖度分析重要

ECMA-335 规范定义了 CLI（Common Language Infrastructure）的完整语义：类型系统、元数据格式、CIL 指令集、执行模型、异常处理、泛型、反射。一个"完整的 CLR 实现"意味着实现规范中定义的所有语义。

CoreCLR 是目前最接近"完整实现"的 runtime——它支持规范中的几乎所有特性，包括运行时代码生成（Reflection.Emit）、动态程序集加载（Assembly.Load）、多种执行模式（JIT + AOT + Interpreter）。

IL2CPP 从设计之初就不追求完整覆盖。它的目标是在移动端和主机平台上提供高性能的 AOT 执行，而不是成为一个通用的 CLR。这个定位决定了它会主动放弃规范中的某些能力——特别是所有依赖运行时代码生成的特性。

覆盖度分析的价值在于三个层面：

**工程决策。** 知道什么不能用，才能在架构设计时选择正确的方案。比如知道 Reflection.Emit 不可用，就不会选择依赖动态代理的 AOP 框架。

**故障诊断。** 运行时报 `PlatformNotSupportedException` 时，知道这是覆盖度边界问题而不是 bug，才能快速定位解决方案（link.xml、HybridCLR、或者换一种实现方式）。

**理解 HybridCLR 的价值。** HybridCLR 不是在修复 IL2CPP 的 bug——它是在补充 IL2CPP 主动放弃的覆盖度。理解 IL2CPP 缺什么，才能理解 HybridCLR 补了什么、为什么那样补。

## 完全支持的特性

IL2CPP 对 ECMA-335 核心类型系统和执行语义的支持是完整的。以下特性在 IL2CPP 上的行为与 CoreCLR/Mono 一致。

### 类型系统基础

**值类型与引用类型。** struct 和 class 的完整语义——值类型的栈分配和复制语义、引用类型的堆分配和 GC 管理、boxing/unboxing、`Nullable<T>`。D3 分析过的 `Il2CppObject` 对象头设计完整实现了引用类型的运行时表示。

**接口。** 接口声明、多接口实现、显式接口实现（EIIM）、接口的默认方法（.NET 8+）。D3 分析过的 `Il2CppClass::interfaceOffsets` 实现了接口方法的 O(1) 分派。

**委托。** 单播委托、多播委托（`MulticastDelegate`）、委托的 `BeginInvoke`/`EndInvoke`。il2cpp.exe 为每个委托类型生成对应的 C++ 包装类。

**枚举。** 枚举类型的完整支持，包括 `[Flags]` 属性、枚举的底层类型（`byte`、`int`、`long` 等）。

### 泛型

D5 详细分析过 IL2CPP 的泛型代码生成策略。核心结论：IL2CPP 完整支持 ECMA-335 定义的泛型语义——泛型类型、泛型方法、泛型约束、协变与逆变。

限制在于 AOT 模型带来的"构建时可见性"要求：只有构建时可见的泛型实例才会生成代码。D5 分析的 Full Generic Sharing（Unity 2021+）大幅缓解了这个限制，但在性能敏感路径上仍然推荐显式保留热路径的泛型实例。

### 异常处理

`try`/`catch`/`finally`/`throw` 的完整语义。il2cpp.exe 将 CIL 的异常处理块翻译为 C++ 的 `try`/`catch` 语句，依赖 C++ 编译器的异常处理机制。CROSS-G6 对比过，IL2CPP 的异常处理在语义正确性上与 CoreCLR 一致，差异主要在性能特性和实现细节上。

### async/await

C# 的 `async`/`await` 在编译阶段被 Roslyn 降级为状态机。IL2CPP 看到的是状态机的 CIL 代码，不需要特殊处理。`Task`、`ValueTask`、`IAsyncStateMachine` 的完整支持。

### Span\<T\> 与 Memory\<T\>

`Span<T>` 在 CIL 层面是一个 `ref struct`，IL2CPP 通过值类型的标准处理路径支持它。`stackalloc`、`Span<T>` 的切片操作、`Memory<T>` 的堆上版本——这些都正常工作。

### LINQ

LINQ 在编译后是标准的方法调用链（扩展方法 + 委托/lambda），IL2CPP 完整支持。泛型 LINQ 方法的 AOT 可见性问题通过 Full Generic Sharing 或显式保留解决。

## 部分支持的特性

以下特性 IL2CPP 支持了核心功能，但某些子能力不可用。

### Reflection（反射）

**支持的部分：**

- `Type.GetType()`、`typeof()`、`Assembly.GetTypes()` — 类型信息查询
- `GetFields()`、`GetMethods()`、`GetProperties()` — 成员信息查询
- `GetCustomAttributes()` — 自定义特性读取
- `Activator.CreateInstance()` — 通过反射创建实例
- `MethodInfo.Invoke()` — 通过反射调用方法

这些能力依赖 global-metadata.dat 中存储的类型信息。D4 分析过，metadata 文件保留了完整的类型描述信息——类名、方法签名、字段定义、特性数据。只要类型没有被 stripping 裁剪掉，反射查询就能正常工作。

**不支持的部分：**

- `Reflection.Emit` — 运行时动态生成 IL 代码（下一节详述）
- `TypeBuilder`、`MethodBuilder` — 运行时动态创建类型
- `DynamicMethod` — 运行时创建轻量级动态方法

反射的"只读"部分完全可用，"可写"部分（动态生成代码）完全不可用。这个边界的根因是 AOT 模型——反射查询只需要读取 metadata，而动态代码生成需要运行时编译 IL 到 native code。

### P/Invoke

**支持的部分：**

- `[DllImport]` 声明的静态 native 方法调用
- 基本的 marshalling（`string`、`int`、`float`、指针类型）
- `Marshal.PtrToStructure()`、`Marshal.StructureToPtr()` — 结构体的手动 marshalling
- `[StructLayout]`、`[FieldOffset]` — 显式内存布局控制

**受限的部分：**

- 所有 P/Invoke 声明必须是静态的、编译时确定的。不能在运行时构造新的 P/Invoke 调用
- 复杂的 marshalling 场景（嵌套结构体数组、callback delegate 的复杂签名）可能需要手动处理
- `Marshal.GetDelegateForFunctionPointer()` 在某些平台上的行为可能与 CoreCLR 不完全一致

P/Invoke 的核心限制是"静态声明"——il2cpp.exe 在构建时需要看到 `[DllImport]` 声明，才能生成对应的 C++ 调用包装。不能在运行时动态绑定新的 native 函数。

## 不支持的特性

以下特性在 IL2CPP 上完全不可用。

### Reflection.Emit

`System.Reflection.Emit` 命名空间的全部功能——`AssemblyBuilder`、`ModuleBuilder`、`TypeBuilder`、`MethodBuilder`、`ILGenerator`。

这是 IL2CPP 最核心的限制。Reflection.Emit 允许在运行时构造 IL 字节码并编译执行。CoreCLR 的 JIT 编译器可以随时将新的 IL 编译为 native code；IL2CPP 在运行时没有 JIT，无法将运行时生成的 IL 编译为 native code。

**影响范围：**

- 依赖 `Emit` 的序列化库（如某些版本的 JSON 序列化器用 Emit 生成序列化代码）
- 依赖动态代理的 AOP 框架（Castle DynamicProxy）
- 依赖 `Expression.Compile()` 生成委托的库（LINQ Expression Tree 的编译）
- 依赖 `DynamicMethod` 的高性能反射调用方案

### Assembly.Load（原生不支持）

`Assembly.Load(byte[])`、`Assembly.LoadFrom(string)` 在 IL2CPP 上不可用。IL2CPP 在构建时将所有程序集静态编译为 native code，运行时不接受新的程序集。

CROSS-G7 详细分析过这个限制：IL2CPP 的执行模型是纯 AOT，没有 JIT 或解释器来执行新加载的程序集中的代码。即使能加载 DLL 并解析其 metadata，也没有执行引擎来运行其中的方法。

### AppDomain

.NET Framework 时代的 `AppDomain` 在 .NET Core 中已经被标记为过时（`AppDomain.CreateDomain` 抛出 `PlatformNotSupportedException`）。IL2CPP 遵循 .NET Core 的策略，不支持创建新的 AppDomain。

`AppDomain.CurrentDomain` 仍然可以访问——用于获取当前应用域的程序集列表、处理未捕获异常等只读操作。但创建新的隔离域、跨域通信（`MarshalByRefObject`）等功能完全不可用。

### 动态代码生成

所有依赖运行时生成可执行代码的 API：

- `System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod()` — 某些重载不可用
- `System.Linq.Expressions.Expression.Compile()` — 在 IL2CPP 上退回解释执行模式（性能显著降低），而非生成 native code
- `System.CodeDom.Compiler` — 运行时编译 C# 源码

### COM Interop（部分平台）

COM Interop 在 Windows Standalone 平台上有限支持，在 iOS、Android 等移动平台上完全不可用。移动平台不存在 COM 基础设施，IL2CPP 不生成 COM 相关的 marshalling 代码。

## 为什么这些不支持 — 根因分析

上述所有不支持的特性可以追溯到同一个根因：**AOT 模型的固有约束**。

```
AOT 编译模型
    │
    ├── 所有代码必须在构建时确定
    │     ├── 不能运行时加载新程序集 → Assembly.Load 不可用
    │     └── 不能运行时生成新代码 → Emit/DynamicMethod 不可用
    │
    ├── 没有 JIT 编译器
    │     ├── 不能将 IL 编译为 native code → Emit 生成的 IL 无法执行
    │     └── Expression.Compile() 退回解释模式
    │
    └── 代码是 native binary
          ├── 不能动态修改已编译的代码
          └── 不能在运行时注入新的方法实现
```

这条因果链清晰且不可回避：

**第一层：AOT 模型。** IL2CPP 将 C# 编译为 C++，再编译为 native binary。构建完成后，所有可执行代码已经固化在 `GameAssembly` 中。

**第二层：无 JIT。** 运行时没有将 IL 编译为 native code 的能力。即使有新的 IL 字节码（通过 Emit 生成或从 DLL 加载），也没有执行引擎来运行它。

**第三层：无法运行时生成代码。** 没有 JIT 意味着 Reflection.Emit 生成的 `ILGenerator` 代码无法被编译执行。`Assembly.Load` 加载的新程序集中的方法无法被执行。所有依赖运行时代码生成的功能链全部断裂。

这不是 IL2CPP 的实现缺陷，而是 AOT 模型的设计选择。AOT 的收益——确定性的性能表现、没有 JIT 预热开销、更小的运行时体积、满足 iOS 等平台的代码签名要求——与这些限制是同一枚硬币的两面。

## HybridCLR 补了什么

HybridCLR 在 IL2CPP 的覆盖度缺口中补了两个核心能力：

### Assembly.Load — InterpreterImage

HybridCLR 通过 `InterpreterImage` 实现了运行时加载新程序集的能力。加载的 DLL 被解析为 `InterpreterImage`，其中的类型信息被注入到 IL2CPP 的类型系统中（`Il2CppClass` 的扩展），方法体保留为 CIL 字节码。

```
IL2CPP 原生加载路径（构建时确定）：
  DLL → il2cpp.exe → C++ → native code → GameAssembly

HybridCLR 热更加载路径（运行时）：
  DLL → InterpreterImage → metadata 注入 → CIL 字节码保留
                                              ↓
                                         解释器执行
```

### 运行时方法执行 — Interpreter

HybridCLR 内置的 CIL 解释器可以执行热更程序集中的方法。解释器读取 CIL 字节码，维护自己的求值栈和局部变量，逐条指令执行。

这两个能力的组合恢复了 IL2CPP 缺失的"运行时代码引入"能力：新程序集可以在运行时加载，其中的方法可以被解释执行。AOT 编译的主体代码保持 native 性能，热更代码走解释器路径。

### 没有补的：Reflection.Emit

HybridCLR 没有实现 Reflection.Emit。原因是 Emit 的使用场景（动态代理、运行时代码生成优化）在 Unity 游戏开发中不是刚需，而实现成本极高——需要构建一个完整的 IL 代码生成 API 和对应的解释器支持。

HybridCLR 选择的方案是"程序集级别的热更新"而非"方法级别的动态生成"。开发者在 Unity 外部编译好完整的 DLL，通过 `Assembly.Load` 加载。这种方案覆盖了绝大多数热更新场景，而不需要在运行时动态构造 IL 代码。

## 覆盖度对比表

| 特性 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 值类型 / 引用类型 | 完整 | 完整 | 完整 | 完整 | 完整 |
| 泛型 | 完整 | 完整 | 完整（AOT 可见性约束） | 完整（FGS 兜底） | 完整 |
| 异常处理 | 完整 | 完整 | 完整 | 完整 | 完整 |
| 接口 / 委托 | 完整 | 完整 | 完整 | 完整 | 完整 |
| async/await | 完整 | 完整 | 完整 | 完整 | 部分 |
| Span\<T\> | 完整 | 完整 | 完整 | 完整 | 不支持 |
| Reflection（只读） | 完整 | 完整 | 完整（受 stripping 影响） | 完整 | 基础 |
| Reflection.Emit | 完整 | 完整 | 不支持 | 不支持 | 不支持 |
| Assembly.Load | 完整（ALC） | 完整 | 不支持 | 支持（InterpreterImage） | 支持 |
| Assembly 卸载 | 支持（ALC） | 不支持 | 不支持 | 商业版支持 | 不支持 |
| AppDomain 创建 | 不支持 | 支持（旧版） | 不支持 | 不支持 | 不支持 |
| 动态代码生成 | 完整 | 完整 | 不支持 | 不支持 | 不支持 |
| P/Invoke | 完整 | 完整 | 静态声明 | 静态声明 | 基础 |
| COM Interop | 完整 | 部分 | 部分（仅 Windows） | 部分 | 不支持 |
| 执行模式 | JIT + AOT + Interp | JIT + AOT + Interp | 纯 AOT | AOT + Interpreter | 纯 Interpreter |

几个值得注意的模式：

**CoreCLR 覆盖度最高**，因为它同时拥有 JIT 编译器和完整的运行时基础设施。JIT 的存在让所有依赖运行时代码生成的特性都成为可能。

**IL2CPP 和 LeanCLR 分别缺失不同的东西。** IL2CPP 缺失的是动态能力（Emit、Assembly.Load），优势是 native 性能。LeanCLR 天然支持动态加载（纯解释器不需要编译），但缺失的是完整的 BCL 覆盖和高级特性（Span、完整反射）。

**HybridCLR 填的是 IL2CPP 最关键的缺口**——Assembly.Load 和运行时方法执行。这两个能力组合起来恢复了热更新的可行性，覆盖了 Unity 游戏开发中最高频的需求。

## 收束

IL2CPP 对 ECMA-335 的覆盖度可以用一句话概括：类型系统和执行语义完整，动态能力全部缺失。

这个覆盖度边界不是 IL2CPP 的缺陷，而是 AOT 模型的固有特征。AOT 编译把所有代码在构建时固化为 native binary——获得了确定性的性能和平台兼容性（iOS 要求代码签名，禁止 JIT），代价是放弃了运行时的代码可变性。

理解这个边界有两个层面的意义。第一个层面是工程实践：知道 Reflection.Emit 不可用，就不会选择依赖动态代理的框架；知道 Assembly.Load 不可用，就知道热更新需要 HybridCLR；知道反射受 stripping 影响，就知道需要 link.xml 保护关键类型。第二个层面是架构理解：IL2CPP 的覆盖度缺口定义了 HybridCLR 的存在价值——它不是在修复 bug，而是在 AOT 的约束条件下，用解释器补回被 AOT 模型排除的动态能力。

下一篇 D8 将分析 IL2CPP 的 Managed Code Stripping——裁剪策略如何进一步收窄运行时可用的类型集合，以及 link.xml 和 HybridCLR 如何应对裁剪带来的问题。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 — IL2CPP 模块
- 位置：IL2CPP-D7
- 上一篇：[IL2CPP-D6 GC 集成：BoehmGC 的接入层]
- 下一篇：[IL2CPP-D8 Managed Code Stripping：裁剪策略与 link.xml]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}})

**相关阅读**

- [IL2CPP-D1 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})
- [IL2CPP-D3 libil2cpp runtime：MetadataCache、Class、Runtime 三层结构]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}})
- [IL2CPP-D5 泛型代码生成：共享、特化与 Full Generic Sharing]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}})
- [横切对比｜程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载]({{< relref "engine-toolchain/runtime-cross-assembly-loading-hot-update-comparison.md" >}})
- [ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
