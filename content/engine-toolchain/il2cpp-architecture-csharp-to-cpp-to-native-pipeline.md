---
title: "IL2CPP 实现分析｜架构总览：从 C# → C++ → native 的完整管线"
slug: "il2cpp-architecture-csharp-to-cpp-to-native-pipeline"
date: "2026-04-14"
description: "从 Unity 编辑器到最终包体，拆解 IL2CPP 的完整管线：C# 编译 → IL 产出 → il2cpp.exe 转换 → C++ 代码生成 → native 编译。定位 libil2cpp runtime 和 global-metadata.dat 在这条链里各自的职责。"
weight: 50
featured: false
tags:
  - IL2CPP
  - Unity
  - CLR
  - AOT
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> IL2CPP 不是一个 runtime，而是一条从 C# 到 native binary 的完整转换管线——il2cpp.exe 做转换，libil2cpp 做运行时，global-metadata.dat 做数据。

这是 .NET Runtime 生态全景系列的 IL2CPP 模块第 1 篇。

## 为什么要独立分析 IL2CPP

HybridCLR 系列已经从"补丁"视角间接分析了 IL2CPP 的部分模块：metadata 怎么加载、方法怎么注册、泛型共享怎么运作。但那条分析线始终是为了回答"HybridCLR 补了什么"，而不是为了回答"IL2CPP 本身是什么"。

两个视角的侧重点不一样。

从 HybridCLR 出发，关心的是接缝——IL2CPP 在哪些地方留了口子，HybridCLR 把解释器插进去之后改变了什么。这种视角天然会跳过 IL2CPP 自身那些"没被补丁触碰"的部分。

但 IL2CPP 作为 Unity 当前的主力 runtime 方案，几乎所有上线项目都在用它。很多工程问题——包体膨胀、泛型代码爆炸、stripping 误裁、构建耗时过长——不需要经过 HybridCLR 那层视角，而是需要直接理解 IL2CPP 自身的管线设计。

所以这条独立分析线的目标很明确：把 IL2CPP 从头到尾拆一遍，不依赖"它哪里被 HybridCLR 改了"这个切入点。

这篇先建全景地图。

## IL2CPP 的整体管线

从 Unity 编辑器点下 Build 到最终包体生成，IL2CPP 的完整管线可以拆成 5 个阶段。

### Stage 1：C# → IL（Roslyn 编译）

这一步和 IL2CPP 其实没有直接关系。

Unity 使用 Roslyn（csc.dll）把项目里的 C# 源码编译成标准的 .NET 程序集（DLL），产物是包含 CIL 字节码和 metadata 的托管 DLL。这一步产出的是标准 ECMA-335 格式的程序集，和用 `dotnet build` 编译出来的 DLL 在格式上没有本质区别。

输出产物举例：`Assembly-CSharp.dll`、`UnityEngine.dll`、`mscorlib.dll` 等。

此时代码还是 IL 形态，和 IL2CPP 无关。

### Stage 2：IL → Stripped IL（UnityLinker 裁剪）

Unity 在把 IL 交给 il2cpp.exe 之前，会先用 UnityLinker（基于 Mono.Linker）做一轮静态分析裁剪。

UnityLinker 的核心工作是：从入口程序集出发做可达性分析，标记所有被直接或间接引用到的类型、方法、字段，把没被标记到的成员从 IL 中移除。

这一步的意义在于减少后续 C++ 代码生成的体量。如果一个类型从来没有被引用过，没有必要为它生成 C++ 代码、占用编译时间和最终包体空间。

但裁剪也会引入问题——如果某个类型只通过反射使用，静态分析无法发现这条引用路径，它就会被裁掉。这就是为什么需要 `link.xml` 或 `[Preserve]` 来手动保护。

输出产物：裁剪后的程序集，体积通常比原始 DLL 小很多。

### Stage 3：Stripped IL → C++（il2cpp.exe 转换）

这一步是整条管线的核心。

il2cpp.exe 读取裁剪后的 IL 程序集，输出等价的 C++ 源码文件。这里有一个关键认知需要先建立：il2cpp.exe 不是编译器，而是转换器。它不做优化、不做寄存器分配、不生成机器码——它只是把每一条 IL 指令翻译成对应的 C++ 语句。

转换的核心映射关系：

- 每个 C# 类 → 一个 C++ 结构体（字段布局）+ 一组 C++ 函数（方法实现）
- 每个 C# 方法 → 一个 C++ 函数，函数名包含类名、方法名和 hash
- IL 指令 → C++ 语句，例如 `ldloc.0` + `ldloc.1` + `add` → `int32_t V_2 = V_0 + V_1;`
- 泛型实例化在这一步完成：`List<int>` 和 `List<float>` 各自生成独立的 C++ 代码（值类型），`List<string>` 和 `List<object>` 共享同一份代码（引用类型）

除了转换后的 C++ 源码，il2cpp.exe 还会生成 `global-metadata.dat`——后面单独讲。

输出产物：大量 `.cpp` 和 `.h` 文件，以及 `global-metadata.dat`。

### Stage 4：C++ → native binary（平台编译器）

il2cpp.exe 输出的 C++ 代码，由平台原生编译器编译成机器码：

| 目标平台 | 编译器 |
|---------|--------|
| Windows | MSVC（cl.exe） |
| Android | Clang（NDK） |
| iOS | Clang（Xcode） |
| macOS | Clang（Xcode） |
| Linux | GCC / Clang |
| WebGL | Emscripten（Clang → Wasm） |

这一步是真正的编译——C++ 编译器负责优化（内联、循环展开、寄存器分配等）和生成目标平台的机器码。IL2CPP 能做到"接近 C++ 性能"的原因就在这里：最终的优化不是 Unity 做的，而是由成熟的 C++ 编译器完成的。

这一步通常是整个构建流程中最耗时的环节，尤其在大项目上，C++ 编译可能占据构建时间的 60%~80%。

输出产物：`GameAssembly.dll`（Windows）、`libil2cpp.so`（Android）、静态库（iOS）。

### Stage 5：最终包体组装

最终交付的包体由三个核心部分组成：

```
最终包体
  ├── GameAssembly（转换后的 C# 逻辑 + libil2cpp runtime）
  ├── global-metadata.dat（运行时元数据）
  └── Unity 引擎本体 + 资源
```

到这里，原始的 C# 代码已经完全不存在了。没有 IL，没有 DLL，只有 native 机器码、一份二进制元数据文件和引擎本体。

## il2cpp.exe：转换器

il2cpp.exe 值得单独拉出来讲，因为它是整条管线里最容易被误解的环节。

最常见的误解是把它当成"编译器"。但编译器的核心工作是优化和代码生成（IR → 机器码），il2cpp.exe 不做这些。它的工作是源到源的翻译（source-to-source translation）：读入 IL，输出语义等价的 C++。真正的优化和机器码生成交给下游的 C++ 编译器。

il2cpp.exe 本身是一个用 C# 编写的命令行工具。Unity 编辑器在构建时调用它，传入裁剪后的程序集路径和一系列配置参数。

它在转换过程中要处理的核心问题包括：

**方法转换。** 每个 C# 方法被转换为一个 C++ 函数。函数签名包含一个额外的 `MethodInfo*` 参数，用于运行时方法调度。虚方法调用被转换为通过 vtable 的函数指针调用。

**类型布局。** 每个 C# 类型被转换为一个 C++ 结构体，字段按照 CLI 布局规则排列。引用类型的对象头包含 `Il2CppClass*` 指针（用于类型标识和 vtable 查找）。

**泛型实例化。** 这是转换过程中最复杂的部分。对于值类型参数的泛型实例，必须为每个实例生成独立的 C++ 代码（因为内存布局不同）。对于引用类型参数的泛型实例，可以共享同一份代码（因为所有引用类型的指针大小相同）。这就是 IL2CPP 的泛型共享（generic sharing）策略。

**metadata 生成。** il2cpp.exe 在转换代码的同时，还会生成 `global-metadata.dat` 文件。这份文件包含了运行时需要的所有元数据信息。

后续 D2 篇会专门拆解 il2cpp.exe 的内部转换策略。

## libil2cpp：运行时

如果 il2cpp.exe 负责构建时的转换，那 libil2cpp 就负责运行时的基础设施。

libil2cpp 是 IL2CPP 的运行时库，用 C++ 编写，最终和转换后的 C# 代码链接在一起。它提供了一个 AOT 环境下的 CLR 运行时所需的核心服务。

### MetadataCache：类型与方法的注册中心

MetadataCache 是 libil2cpp 里最核心的模块之一。它负责在运行时启动时读取 `global-metadata.dat`，把其中的类型定义、方法定义、字段信息等解析出来，建立起运行时可查询的缓存结构。

当代码在运行时需要做类型查找、反射查询或泛型实例化时，最终都会落到 MetadataCache 上。

### GC：BoehmGC 集成

IL2CPP 使用 BoehmGC 作为垃圾收集器。这是一个保守式（conservative）GC——它不精确区分栈上的值到底是指针还是普通整数，而是把所有看起来像指针的值都当成潜在的对象引用来处理。

保守式 GC 的优势是实现简单、不需要运行时生成精确的 GC 栈映射；劣势是可能产生虚假引用（false positive），导致某些应该被回收的对象因为栈上恰好有一个看起来像它地址的整数值而无法被回收。

和 CoreCLR 的分代精确式 GC 相比，BoehmGC 在吞吐量和内存精确性上都有差距。但对于 IL2CPP 的使用场景（游戏，对象生命周期相对可控），这个 trade-off 是可以接受的。

### Thread：线程管理

libil2cpp 封装了平台线程原语（pthread / Win32 Thread），提供托管线程的创建、同步和 TLS（Thread-Local Storage）管理。`System.Threading.Thread`、`Monitor`、`Mutex` 等 .NET 线程 API 最终都落到这一层。

### Runtime：初始化与生命周期

`il2cpp::vm::Runtime` 负责整个运行时的初始化流程——加载 metadata、注册类型、初始化 GC、设置线程环境。引擎启动时调用 `il2cpp_init()`，最终走到 `Runtime::Init()`，把所有子系统拉起来。

### icalls：内部调用

.NET BCL（Base Class Library）中很多方法的实现最终需要调用底层 C/C++ 代码。在 IL2CPP 里，这些通过 internal call（icall）机制实现。libil2cpp 内部维护了一张 icall 注册表，把托管方法签名映射到 native 函数指针上。

### 与 CoreCLR / Mono 的定位对比

libil2cpp 和 CoreCLR 的 coreclr.dll、Mono 的 libmonosgen 在定位上是平级的——它们都是 CLR 运行时。区别在于：

- CoreCLR 的运行时包含 JIT 编译器（RyuJIT），可以在运行时把 IL 编译成机器码
- Mono 的运行时包含 JIT 和解释器两条执行路径
- libil2cpp 的运行时不包含任何 IL 执行能力——所有代码在构建时已经转换成了 native

这就是为什么原生 IL2CPP 不支持 `Assembly.Load()` 加载新的 DLL：运行时根本没有能力执行 IL。HybridCLR 补的正是这个能力。

## global-metadata.dat：数据

`global-metadata.dat` 是 il2cpp.exe 在构建时生成的一份二进制数据文件。它不包含可执行代码，但运行时离不开它。

这份文件存储的信息包括：

**字符串表。** 所有类型名、方法名、字段名、命名空间名、程序集名等字符串。运行时做反射查询、输出异常堆栈、打印类型名时，都需要从这里查。

**类型定义索引。** 每个类型的定义信息——它属于哪个程序集、基类是谁、实现了哪些接口、有哪些字段和方法、字段偏移是多少。

**方法定义索引。** 每个方法的签名、参数信息、返回类型、所属类型、method token。

**泛型信息。** 泛型容器（哪些类型/方法有泛型参数）、泛型参数约束、泛型实例化记录。

**自定义属性。** `[Serializable]`、`[Preserve]` 等 attribute 的数据。

运行时通过 MetadataCache 按需读取这些信息。并不是启动时就把整个文件全部解析一遍，而是在第一次需要某个类型或方法信息时才去查表——惰性加载。

一个值得注意的设计：native code 里的方法调用不依赖 global-metadata.dat 里的字符串。代码在编译时已经通过函数指针直接绑定了调用目标。metadata 文件主要服务于运行时的"描述层"需求——反射、类型查询、异常信息等。

后续 D4 篇会专门拆解 global-metadata.dat 的二进制格式和加载机制。

## GameAssembly 与 libil2cpp：最终产物

最终包体里，native 代码的存在形态因平台而异：

| 平台 | 转换后的 C# 代码 | libil2cpp runtime | 形态 |
|------|-----------------|-------------------|------|
| Windows | GameAssembly.dll | 链接在 GameAssembly.dll 内 | 动态库 |
| Android | libil2cpp.so | 链接在 libil2cpp.so 内 | 共享库 |
| iOS | 静态库 | 链接在最终可执行文件内 | 静态链接 |

有一个容易混淆的点需要澄清：在 Windows 上，转换后的代码和 runtime 分别在 GameAssembly.dll 里（看起来是两个部分），但在 Android 上，两者合并在同一个 `libil2cpp.so` 里。名字不同，但本质是一样的——转换后的用户代码和 runtime 基础设施最终链接在一起。

GameAssembly 包含的是所有转换后的 C# 方法的 native 实现。如果项目有 1000 个 C# 方法，经过泛型实例化和共享之后，GameAssembly 里就会有对应数量的 C++ 函数编译出的机器码。

libil2cpp 包含的是运行时基础设施——GC、类型系统、线程管理、metadata 解析等。这部分代码在同一 Unity 版本的所有项目中几乎相同。

两者必须在一起才能工作。GameAssembly 里的代码会调用 libil2cpp 提供的 runtime API（分配对象、查找类型、抛出异常），libil2cpp 又依赖 GameAssembly 里注册的类型和方法信息来构建运行时世界。

## IL2CPP 的 ECMA-335 覆盖度

IL2CPP 实现了 ECMA-335 规范的大部分内容，但并非全部。理解它的覆盖边界，对于判断"这个特性在 IL2CPP 上能不能用"非常重要。

### 支持的核心特性

**泛型。** 包括泛型类型、泛型方法、泛型约束、协变/逆变。泛型是 IL2CPP 投入最重的模块之一，因为 AOT 环境下的泛型实例化策略直接影响包体大小和运行时正确性。

**异常处理。** try/catch/finally/fault，包括异常过滤器（exception filter）。IL2CPP 把 CLI 的异常处理模型转换为 C++ 的 setjmp/longjmp 或平台特定的异常机制。

**委托与事件。** 包括多播委托、匿名方法、lambda 表达式。这些在 IL 层面最终都是委托对象，IL2CPP 能正常处理。

**LINQ。** LINQ 查询最终编译成方法链调用和 lambda，IL2CPP 完全支持。需要注意的是泛型 LINQ 扩展方法可能触发额外的泛型实例化。

**反射（部分）。** `typeof`、`GetType()`、`GetMethod()` 等基础反射 API 可用。但反射的可用范围受 stripping 和 code generation 的约束——如果一个类型被裁掉了，反射自然找不到它。

**值类型与装箱。** struct、enum、boxing/unboxing 正常支持。

### 不支持或受限的特性

**Reflection.Emit。** 完全不支持。`Reflection.Emit` 允许在运行时动态生成 IL 代码，这和 AOT 的设计前提直接冲突——所有代码必须在构建时确定。

**动态代码生成。** `System.Linq.Expressions.Expression.Compile()`、`DynamicMethod` 等依赖运行时代码生成的 API 不可用。

**AppDomain。** IL2CPP 不支持多 AppDomain。所有代码运行在同一个域中。`AppDomain.CreateDomain()` 会抛出 `NotSupportedException`。

**Assembly.Load（原生不支持）。** 这是 IL2CPP 最显著的能力缺口。原生 IL2CPP 无法在运行时加载新的托管程序集，因为 runtime 没有 IL 执行能力。HybridCLR 补的核心能力之一就是这个——它在 IL2CPP runtime 里注入了一个解释器，使得运行时加载和执行新 DLL 成为可能。

**Thread.Abort。** 不支持。`Thread.Abort()` 在现代 .NET 中也已被弃用。

这些限制并非偶然遗漏，而是 AOT 设计的必然结果。AOT 的核心假设是"所有代码在构建时已知"，任何需要在运行时生成或加载新代码的特性都和这个假设矛盾。

## IL2CPP vs 其他 runtime 的定位

同一份 ECMA-335 规范，5 个 runtime 做了不同的实现决策。用一张表快速定位它们的差异：

| 维度 | CoreCLR | Mono | IL2CPP | LeanCLR |
|------|---------|------|--------|---------|
| 执行模式 | JIT | JIT + Interpreter | AOT（全量） | Interpreter |
| 代码生成时机 | 运行时 | 运行时 | 构建时 | 无代码生成 |
| IL 执行能力 | 有（JIT） | 有（JIT / Interp） | 无（原生） | 有（解释器） |
| 典型产物体积 | 大（含 JIT 编译器） | 中 | 大（native code 膨胀） | 小（~600KB） |
| GC 类型 | 分代精确式 | SGen 精确式 | BoehmGC 保守式 | 协作式（stub） |
| 热更新能力 | AssemblyLoadContext | 无原生支持 | 需 HybridCLR | 原生支持 |
| Assembly.Load | 支持 | 支持 | 不支持（原生） | 支持 |
| 主要使用场景 | 服务端 / 桌面 | Unity 编辑器 / 旧 Player | Unity Player（主力） | H5 / 小游戏 / 嵌入式 |

几个值得注意的对比点：

**代码生成时机**是最根本的分歧。CoreCLR 和 Mono 在运行时做代码生成（JIT），IL2CPP 在构建时做代码生成（AOT），LeanCLR 不做代码生成（纯解释）。这个决策决定了后续几乎所有架构差异。

**AOT 带来的体积问题。** IL2CPP 的 native code 体积通常比等价的 IL 字节码大很多倍。一个 1MB 的 DLL 经过 IL2CPP 转换和编译后，可能变成 10~20MB 的机器码。这是 AOT 的固有代价——IL 是紧凑的栈机指令，native code 是展开后的寄存器机指令。

**热更新能力的差异**直接影响了移动游戏的技术选型。IL2CPP 原生不支持热更新，HybridCLR 的存在价值正在于此。

## 后续路线图

IL2CPP 模块总共规划 8 篇，这是第 1 篇。后续 4 篇的预告：

**D2：il2cpp.exe 转换器。** 深入 il2cpp.exe 的内部，拆解 IL → C++ 的具体转换策略：方法转换、类型布局生成、泛型实例化决策、代码生成模板。

**D3：libil2cpp runtime。** 拆解 MetadataCache、Class、Runtime 三层的内部结构，理解运行时启动流程和类型系统的初始化链路。

**D4：global-metadata.dat。** 二进制格式详解、版本校验机制、与 runtime 的绑定关系。

**D5：IL2CPP 泛型代码生成。** 共享（shared）、特化（specialized）、全泛型共享（full generic sharing）三种策略的触发条件和工程影响。

## 收束

回到开头那句话：IL2CPP 不是一个 runtime，而是一条完整的转换管线。

更准确地说，"IL2CPP"这个名字覆盖了三个不同层次的东西：

1. **il2cpp.exe**——构建时的转换器，把 IL 翻译成 C++
2. **libil2cpp**——运行时的基础设施，提供 GC、类型系统、线程管理等 CLR 服务
3. **global-metadata.dat**——构建时生成的数据文件，运行时按需查询

当有人说"IL2CPP 不支持热更新"时，准确地说是 libil2cpp 这个运行时不具备执行 IL 的能力。当有人说"IL2CPP 包体太大"时，问题出在 il2cpp.exe 转换 + C++ 编译器编译后的 native code 膨胀。当有人说"IL2CPP 反射有限制"时，限制来自 UnityLinker 裁剪和 global-metadata.dat 的覆盖范围。

把问题定位到管线的具体环节上，才能找到正确的解决方向。

这是后续所有 IL2CPP 深度分析的基础坐标系。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 — IL2CPP 模块
- 位置：IL2CPP 模块首篇（D1）
- 下一篇：IL2CPP-D2 il2cpp.exe 转换器

**相关阅读**

- [IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})
- [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
- [ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
