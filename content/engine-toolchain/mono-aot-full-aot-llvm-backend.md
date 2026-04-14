---
title: "Mono 实现分析｜AOT：Full AOT 与 LLVM 后端"
slug: "mono-aot-full-aot-llvm-backend"
date: "2026-04-14"
description: "拆解 Mono AOT 的两种模式（Normal AOT 与 Full AOT）、编译流程（IL → Mini JIT 管线 → native .o → 链接）、Full AOT 的泛型实例化限制、LLVM 后端的优化收益与代价，以及 Mono AOT 与 IL2CPP AOT 在 metadata 保留策略和产物形态上的结构性差异。"
weight: 64
featured: false
tags:
  - Mono
  - CLR
  - AOT
  - LLVM
  - iOS
series: "dotnet-runtime-ecosystem"
series_id: "mono"
---

> Mono 的 AOT 不是一条独立的编译管线，而是把 Mini JIT 的编译管线从运行时搬到了构建时——同一套 IL → SSA → 优化 → native code 的流程，只是触发时机不同。

这是 .NET Runtime 生态全景系列的 Mono 模块第 5 篇。

C3 拆了 Mini JIT 的编译管线，C4 拆了 SGen GC 的分代模型。C3 最后提到 Mini 的管线可以通过 `--aot` 在构建时运行，也可以用 `--llvm` 切换到 LLVM 后端。这篇把这两个扩展维度作为独立主题展开：AOT 模式解决了什么平台限制，LLVM 后端提供了什么优化收益，它们与 IL2CPP AOT 在架构上有什么根本差异。

## Mono AOT 的两种模式

Mono 的 AOT 有两种运行方式，区别在于运行时是否还允许 JIT。

### Normal AOT

`mono --aot assembly.dll` 在构建时预编译程序集中的方法，输出一个包含 native code 的共享库文件（`.so` / `.dylib`）。运行时 Mono 加载这个共享库，方法调用优先使用预编译的 native code。

关键特征：**仍然允许 JIT fallback。** 如果某个方法在 AOT 编译时没有被覆盖到——可能因为泛型实例化在构建时无法推断、也可能因为 AOT 编译器选择性跳过了某些复杂方法——运行时可以 fallback 到 Mini JIT 现场编译。

Normal AOT 的定位是加速，不是替代。它把高频方法的首次调用延迟消除了（不需要运行时触发 JIT），但保留了 JIT 作为兜底手段。这种模式适用于 Android 等允许 JIT 的平台——启动阶段用 AOT 预编译的代码，运行时遇到 AOT 未覆盖的方法再 JIT。

### Full AOT

`mono --aot=full assembly.dll` 在构建时预编译程序集中的所有方法，运行时完全禁止 JIT。

关键特征：**不允许运行时生成任何新的可执行代码。** 这意味着程序中所有可能被调用的方法——包括泛型实例化、委托回调、反射调用目标——都必须在构建时被 AOT 编译器发现并编译。

Full AOT 的存在理由只有一个：平台安全策略。iOS 的 W^X policy 禁止应用在运行时分配同时可写和可执行的内存页。JIT 的本质是在运行时生成可执行代码，因此在 iOS 上不可用。Full AOT 通过在构建时完成所有编译来绕过这个限制。

早期的游戏主机（PlayStation、Xbox）也有类似的安全策略限制 JIT。Full AOT 在这些平台上同样是必需的。

### 两种模式的对比

| 维度 | Normal AOT | Full AOT |
|------|-----------|----------|
| **JIT fallback** | 允许 | 禁止 |
| **泛型覆盖要求** | 不严格（JIT 兜底） | 严格（所有实例必须预编译） |
| **动态代码生成** | 可用（Reflection.Emit 等） | 不可用 |
| **典型平台** | Android、桌面 Linux、macOS | iOS、tvOS、游戏主机 |
| **目的** | 加速启动 | 满足平台安全要求 |
| **风险** | 低（JIT 兜底） | 高（遗漏方法导致运行时崩溃） |

从工程角度看，Normal AOT 几乎没有风险——最坏情况是 AOT 没覆盖到的方法由 JIT 编译，性能与纯 JIT 持平。Full AOT 则是一个"全覆盖或崩溃"的方案——任何遗漏都意味着运行时找不到 native 代码，抛出异常。

## AOT 编译流程

Mono AOT 编译的完整流程可以拆成四步。

### Step 1：IL 读取

AOT 编译器加载目标程序集的 DLL 文件，解析 PE/COFF 格式和 ECMA-335 metadata。这一步与运行时加载程序集的逻辑相同——都是通过 `MonoImage` 加载并构建 `MonoClass` / `MonoMethod` 等描述结构。

### Step 2：Mini JIT 管线编译

对每个需要编译的方法，AOT 编译器调用 Mini JIT 的完整编译管线：

```
IL → Basic Blocks → SSA → Optimization → Register Allocation → Native Code
```

这条管线与 C3 中分析的 JIT 编译管线完全相同——同样的 IL 到 IR 转换、同样的 SSA 构建、同样的优化 pass、同样的图着色寄存器分配。区别在于输出目标：JIT 模式下输出到内存中的可执行缓冲区，AOT 模式下输出到磁盘上的目标文件（`.o`）。

如果启用了 LLVM 后端（`--aot --llvm`），Mini 前端完成 IL → SSA → 优化后，不走自身的寄存器分配和代码生成，而是把 IR 转换为 LLVM IR，交给 LLVM 完成后端处理。

### Step 3：生成目标文件

每个方法编译产出的 native code 被收集到一个目标文件（`.o`）中。目标文件遵循平台的 ELF / Mach-O / COFF 格式，包含：

- 每个方法的 native code section
- 方法查找表——从 method token 到 native code 地址的映射
- GC 信息——每个方法的 GC Map，运行时 SGen 需要它来做精确的栈根枚举
- 异常处理信息——try/catch/finally 的 native code 区域映射
- 调试信息——如果启用了调试符号

### Step 4：链接

目标文件与 Mono runtime 链接，生成最终的可执行文件或共享库。链接可以是静态的（所有代码编入一个可执行文件）或动态的（AOT 产物作为共享库在运行时加载）。

在 iOS 上，Step 4 的输出通常是一个静态库，最终链接到 Xcode 项目中。在 Android 上是一个共享库（`.so`），打包到 APK 中。

### 与 JIT 编译的对比

```
JIT 模式：
  方法首次调用 → Mini JIT → native code（内存中） → 直接执行

AOT 模式：
  构建时：所有方法 → Mini JIT → native code → .o 文件 → 链接 → 共享库
  运行时：加载共享库 → 方法调用直接跳转到预编译代码
```

本质上是同一条编译管线，不同的触发时机和输出目标。AOT 不引入新的编译策略——它只是把 JIT 的"按需编译"变成了"批量预编译"。

## Full AOT 的泛型问题

Full AOT 最棘手的工程问题是泛型。

### 问题根源

JIT 模式下，泛型实例化在运行时按需发生。代码执行到 `new List<MyStruct>()` 时，JIT 编译器现场为 `List<MyStruct>` 编译一份 native code。不需要预判项目中会使用哪些泛型组合。

Full AOT 模式下，运行时不能生成新代码。这意味着 AOT 编译器必须在构建时发现所有可能被使用的泛型实例化组合，并为每个组合预编译 native code。

### 静态分析的局限

AOT 编译器通过静态分析 IL 来发现泛型实例化。它扫描所有方法体中的 `newobj`、`call`、`callvirt` 等指令，找到引用了泛型实例的位置。但静态分析有固有的局限：

**反射构造的泛型实例。** 如果代码通过 `typeof(List<>).MakeGenericType(someType)` 在运行时构造泛型实例，静态分析无法知道 `someType` 的值。

**跨程序集的泛型传播。** 程序集 A 定义了一个泛型方法 `void Process<T>(T item)`，程序集 B 在某个条件分支里调用 `Process<MyStruct>()`。AOT 编译器分析程序集 A 时不知道程序集 B 会传入什么类型参数，分析程序集 B 时才能发现这个实例化。

**高阶泛型组合。** 泛型嵌套（`Dictionary<int, List<float>>`）和泛型方法嵌套（`foo.Select<int>().Where<int>()`）使得可能的组合空间呈指数增长。静态分析可以发现代码中显式出现的组合，但无法穷举所有理论上可能的组合。

### 与 IL2CPP 的 AOT 泛型问题同源

IL2CPP 的泛型代码生成面临完全相同的问题——构建时必须确定所有泛型实例化。D5 详细分析了 IL2CPP 的应对策略（引用类型共享、值类型独立、Full Generic Sharing）。

两者的根本约束是一样的：AOT 编译的前提是"所有代码在构建时已知"，泛型的延迟实例化与这个前提直接冲突。Mono Full AOT 遇到这个问题在先，IL2CPP 继承了同样的挑战。HybridCLR 的 supplementary metadata 和 AOTGenericReference 机制，归根结底也是在解决这个 AOT 泛型覆盖问题。

### 应对手段

Mono Full AOT 提供了几种泛型覆盖手段：

**完整的 IL 静态分析。** AOT 编译器遍历所有可达方法体，尽可能发现泛型实例化。这是默认行为。

**额外的泛型实例化声明。** 通过配置文件或代码中的显式引用，手动告知 AOT 编译器需要预编译哪些泛型实例。这和 HybridCLR 的 AOTGenericReference 概念上完全一致——都是手动补充静态分析遗漏的实例。

**Interpreter fallback（Mixed 模式）。** .NET 6+ 的 Mono 支持 Mixed 模式——AOT 预编译主体代码，无法 AOT 的方法交给解释器执行。这条路径绕过了"必须在构建时覆盖所有泛型实例"的限制，代价是 fallback 到解释器的方法性能显著下降。Mixed 模式的思路与 HybridCLR 的"IL2CPP AOT + 解释器"架构异曲同工。

## LLVM 后端

### Mini 后端的瓶颈

C3 分析过，Mini 的优化集覆盖了 JIT 编译器的主要场景——内联、常量传播、死代码消除、循环不变量外提——但在深度上有明确的边界。Mini 没有自动向量化、没有过程间分析、没有链接时优化、循环变换策略有限。对于计算密集型代码（矩阵运算、物理模拟、信号处理），Mini 产出的 native code 与 GCC/Clang 的 `-O2` 产出有明显差距。

LLVM 后端的引入就是为了突破这个瓶颈。

### 集成方式

启用 LLVM 后端（`mono --llvm` 或 `mono --aot --llvm`）后，编译管线在 Mini 前端完成 IL → SSA → 基础优化后分叉：

```
IL → Basic Blocks → SSA → Mini 基础优化
  │
  ├─ 默认路径：→ 图着色 → Mini CodeGen → native code
  │
  └─ LLVM 路径：→ Mini IR → LLVM IR → LLVM 优化 pass → LLVM CodeGen → native code
```

Mini IR 到 LLVM IR 的转换在 `mono/mini/mini-llvm.c` 中实现。这个转换层需要处理几个关键映射：

- Mini 的操作码映射到 LLVM 的指令集
- Mono 的异常处理模型映射到 LLVM 的 landing pad / invoke 机制
- GC 安全点和根集信息通过 LLVM 的 GC 插件接口传递
- 调用约定从 Mono 的内部表示转换为 LLVM 的 calling convention

### LLVM 提供的优化

LLVM 拥有 Mini 不具备的多项高级优化：

**自动向量化。** LLVM 的 Loop Vectorizer 和 SLP Vectorizer 可以自动把标量循环转换为 SIMD 指令（SSE、AVX、NEON）。对于数组遍历、数学计算等场景，向量化可以带来 2x~4x 的吞吐量提升。Mini 没有向量化能力。

**激进的循环变换。** Loop Unroll、Loop Rotation、Loop Fusion 等变换让 LLVM 在循环密集型代码上的表现远超 Mini。

**过程间分析。** LLVM 的 IPO（Inter-Procedural Optimization）pass 可以跨函数边界做优化——跨函数常量传播、死参数消除、全局值编号。Mini 的优化范围限制在单个方法内。

**高质量的寄存器分配。** LLVM 使用 Greedy Register Allocator，在大函数和寄存器紧张的平台上表现优于 Mini 的图着色实现。

### 性能代价

LLVM 后端在生成的代码质量上显著优于 Mini 自身的后端，但编译时间也显著增加。

LLVM 的编译时间包含：Mini IR 到 LLVM IR 的转换、LLVM 的多轮优化 pass（每个 pass 遍历整个函数的 IR）、LLVM 自身的寄存器分配和代码生成。对于一个中等大小的方法，Mini 自身的编译可能在微秒级完成，加入 LLVM 后端可能增长到毫秒级。

在 JIT 模式下，这个编译时间增长直接转化为方法首次调用的延迟。对于启动速度敏感的场景，LLVM JIT 通常不可接受。

在 AOT 模式下，编译时间发生在构建阶段，不影响运行时性能。因此 LLVM 后端的典型使用场景是 AOT——构建时花更多时间，换取运行时更高质量的 native code。`mono --aot --llvm` 是 Mono 在 AOT 场景下的最高性能配置。

### 不适合 LLVM 的方法

并非所有方法都适合通过 LLVM 后端编译。Mono 的 LLVM 集成层会跳过某些复杂场景：

- 使用了 Mono 特有异常处理语义（某些 IL 异常处理模式难以映射到 LLVM 的异常机制）
- 包含内联汇编或平台特定 intrinsic 的方法
- LLVM IR 转换层尚未覆盖的 Mini IR 操作码

被跳过的方法 fallback 到 Mini 自身的后端编译。这意味着即使启用了 LLVM，最终的 native code 中可能混合了 LLVM 和 Mini 两个后端的产出。

## 与 IL2CPP AOT 的对比

Mono AOT 和 IL2CPP 都是"构建时编译、运行时直接执行 native code"的方案，但在架构上有根本性的差异。

### 转换路径

```
Mono AOT：
  IL → Mini JIT 管线（或 LLVM） → native .o / .so

IL2CPP：
  IL → il2cpp.exe → C++ 源码 → C++ 编译器（Clang/MSVC） → native .so / .dll
```

Mono AOT 是直接编译——从 IL 到 native code 在一条管线内完成。IL2CPP 是两步转换——先从 IL 到 C++，再从 C++ 到 native code。

### Metadata 保留策略

这是两者最重要的结构性差异。

**Mono AOT 保留完整的 runtime metadata。** AOT 编译后，原始的 `MonoImage`、`MonoClass`、`MonoMethod` 等 metadata 结构仍然在运行时可用。反射查询、类型信息、方法签名——所有 metadata 操作和 JIT 模式下完全一致。AOT 只替换了执行层（native code 代替了 JIT），没有改变 metadata 层。

**IL2CPP 把 metadata 提取到 global-metadata.dat。** il2cpp.exe 在转换过程中，把原始 DLL 中的 metadata 提取出来，重新编码为 IL2CPP 私有的二进制格式（global-metadata.dat）。运行时通过 `MetadataCache` 按需读取这个文件来获得类型信息。原始的 DLL 不存在于最终包体中。

这个差异导致了连锁后果：

| 维度 | Mono AOT | IL2CPP |
|------|---------|--------|
| **原始 DLL 是否保留** | 保留（运行时需要读取 metadata） | 不保留（metadata 已提取到 .dat） |
| **反射能力** | 完整（与 JIT 模式一致） | 受限（受 stripping 和 metadata 覆盖影响） |
| **逆向难度** | 较低（DLL 包含完整 IL 和 metadata） | 较高（只有 native code 和二进制 metadata） |
| **包体组成** | native .so + 原始 DLL + Mono runtime | native GameAssembly + global-metadata.dat |

### 产物形态

Mono AOT 的产物是标准的 native 共享库（`.so` / `.dylib`），其中每个方法对应一段 native code，通过 method token 索引。Mono runtime 在加载时查找这些预编译的方法，找到就用预编译版本，找不到就 fallback（Normal AOT）或报错（Full AOT）。

IL2CPP 的产物也是 native 共享库（`GameAssembly.dll` / `libil2cpp.so`），但其中不只是方法的 native code——还包含了 il2cpp.exe 生成的类型布局结构（C++ struct）、静态字段存储、vtable 初始化代码等。IL2CPP 的产物是一个完整的 C++ 程序编译结果，不只是方法的代码集合。

### 优化能力

| 维度 | Mono AOT（Mini 后端） | Mono AOT（LLVM 后端） | IL2CPP |
|------|---------------------|----------------------|--------|
| **编译器后端** | Mini（自研） | LLVM | Clang / MSVC / GCC |
| **优化深度** | 中（Mini 优化集） | 高（LLVM 完整优化） | 高（C++ 编译器完整优化） |
| **向量化** | 无 | 有（LLVM 向量化 pass） | 有（C++ 编译器向量化） |
| **LTO** | 无 | 有限 | 有（C++ 链接时优化） |
| **构建速度** | 较快 | 中 | 慢（两步转换 + C++ 编译） |

Mono AOT 加 LLVM 后端在优化深度上接近 IL2CPP。两者都利用了工业级编译器后端（LLVM vs Clang/MSVC），能做到自动向量化、循环变换、过程间分析等高级优化。

IL2CPP 的额外优势在于 C++ 编译器的链接时优化（LTO）。il2cpp.exe 把所有 C# 方法转换为 C++ 后，C++ 编译器可以在链接阶段做跨编译单元的优化——这是 Mono AOT 无法匹配的，因为 Mono AOT 以方法为单位独立编译，没有跨方法的全局视野。

Unity 最终选择 IL2CPP 替代 Mono AOT 作为 Player 端方案，优化能力是关键因素之一——但不是唯一因素。代码保护（C++ 比 DLL 难逆向）、平台覆盖（C++ 编译器几乎无处不在）、.NET 版本升级解耦（前端 Roslyn 和后端 runtime 独立演进）同样是重要的驱动力。这些因素将在下一篇（MONO-C6）中展开。

## 收束

Mono AOT 的设计可以从三个层次理解。

**Normal AOT 是 JIT 的加速器。** 把高频方法的编译从运行时移到构建时，消除首次调用延迟。JIT 仍然在运行时可用，作为 AOT 未覆盖方法的 fallback。这种模式风险低、收益明确，适用于允许 JIT 的平台。

**Full AOT 是平台限制的妥协产物。** iOS 的 W^X policy 禁止 JIT，Full AOT 是在这个约束下让 .NET 代码运行的唯一方案。但"所有方法必须在构建时编译"的要求引入了泛型覆盖的难题——这个难题后来在 IL2CPP 身上原样重现，并催生了 HybridCLR 的 supplementary metadata 和 AOTGenericReference 机制。

**LLVM 后端是优化深度的突破口。** Mini 的优化集足够应对一般场景，但在计算密集型代码上与工业级编译器有差距。LLVM 后端把后端工作外包给 LLVM，用编译时间换代码质量。在 AOT 场景下这个 trade-off 是合理的——构建时间增加不影响最终用户体验。

Mono AOT 与 IL2CPP AOT 的核心差异不在于"谁编译得更快"或"谁的优化更好"，而在于 metadata 的处理策略。Mono AOT 保留完整的 runtime metadata，IL2CPP 把 metadata 提取为私有格式。这个选择决定了两者在反射能力、代码保护、包体结构上的根本不同。

## 系列位置

- 上一篇：[MONO-C4 SGen GC：精确式分代 GC 与 nursery 设计]({{< ref "mono-sgen-gc-precise-generational-nursery" >}})
- 下一篇：[MONO-C6 Mono 在 Unity 中的角色：为什么最终转向了 IL2CPP]({{< ref "mono-unity-role-why-il2cpp-replaced" >}})
