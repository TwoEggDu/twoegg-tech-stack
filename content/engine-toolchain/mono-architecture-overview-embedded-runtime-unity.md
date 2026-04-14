---
title: "Mono 实现分析｜架构总览：从嵌入式 runtime 到 Unity 集成"
slug: "mono-architecture-overview-embedded-runtime-unity"
date: "2026-04-14"
description: "Mono 是开源跨平台 .NET 先驱，也是 Unity 的第一代 runtime。拆解 Mono 的六大架构模块（metadata、mini、interpreter、sgen、io-layer、icall），四种执行模式（JIT、Full AOT、Interpreter、Mixed），以及 Unity 为什么没有直接升级 Mono 而是走向 IL2CPP。"
weight: 60
featured: false
tags:
  - Mono
  - CLR
  - Runtime
  - Unity
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "mono"
---

> Mono 不是一个过时的 runtime，而是理解 Unity 技术演进的必经坐标——只有先看清 Mono 的架构边界，才能理解 IL2CPP 和 HybridCLR 各自在补什么。

这是 .NET Runtime 生态全景系列的 Mono 模块第 1 篇。

## 为什么要分析 Mono

在整个 .NET Runtime 生态系列里，CoreCLR 是当前的主线实现，IL2CPP 是 Unity 的 AOT 方案，HybridCLR 是热更新补丁，LeanCLR 是从零构建的轻量替代。Mono 看起来像是"老一代"——Unity 都已经转向 IL2CPP 了，为什么还要回头分析它？

原因有三。

第一，Unity 编辑器至今仍然运行在 Mono 上。即使 Player 端早已默认 IL2CPP，编辑器侧的脚本编译、域加载、热重载全部依赖 Mono。理解 Mono 的模块结构，直接影响对 Unity 编辑器行为的判断。

第二，Mono 是理解"为什么 Unity 转向 IL2CPP"这个问题的前提。如果不先看清 Mono 在 iOS、主机平台上遇到的限制，IL2CPP 的设计动机就只是一句空话。

第三，Mono 的架构在跨平台 CLR 实现中具有独特的参考价值。它同时提供了 JIT、AOT、解释器三条执行路径，这种灵活性在 CoreCLR 和 IL2CPP 中都不存在。对于想理解"一个 CLR 可以怎样组合执行策略"的读者，Mono 是最好的案例。

## Mono 的历史定位

Mono 的历史线可以分成四个阶段。

### Ximian / Novell 时期（2001 — 2011）

2001 年，Miguel de Icaza 在 Ximian 公司启动了 Mono 项目，目标是在 Linux 上实现一个兼容 .NET 的开源运行时。这在当时是一个大胆的决定——微软的 .NET Framework 是 Windows 专属技术，没有任何官方的跨平台支持。

Mono 从第一天起就在做一件事：把微软定义的 ECMA-335 规范，用开源代码实现在非 Windows 平台上。

2003 年 Ximian 被 Novell 收购后，Mono 继续发展，逐步支持了 Linux、macOS、FreeBSD 等平台。这个阶段的 Mono 主要服务于桌面和服务端场景。

### Xamarin 时期（2011 — 2016）

2011 年 Novell 被 Attachmate 收购后，Mono 团队的核心成员创立了 Xamarin，把 Mono 带向了移动平台——iOS 和 Android。

这个阶段 Mono 面临了一个关键技术挑战：iOS 不允许运行时生成可执行代码（即 JIT 被禁止）。Mono 为此开发了 Full AOT 模式，在编译期把所有 IL 预编译为 native code。

Full AOT 是 Mono 架构演进中最重要的分水岭。它证明了一个 CLR 不一定非要用 JIT——提前编译同样可以满足 ECMA-335 的执行模型。这个思路后来直接影响了 Unity 开发 IL2CPP 的决策。

### 微软收购与合并（2016 — 至今）

2016 年微软收购 Xamarin，Mono 正式进入微软体系。此后 Mono 被逐步合并到 `dotnet/runtime` 仓库中，成为 .NET 统一运行时的一部分。

在合并后的架构中，Mono 不再是一个独立竞争的 runtime，而是承担特定场景的执行后端：

- **Blazor WebAssembly** 使用 Mono 解释器在浏览器中运行 .NET 代码
- **iOS / tvOS / Catalyst** 使用 Mono AOT（通过 LLVM 后端）
- **Android** 可选 Mono JIT 或 CoreCLR

从工程角度看，Mono 的代码经过 20 多年的演化，历史包袱比 CoreCLR 重很多。但它在跨平台嵌入能力上的积累，至今仍然是 .NET 生态中不可替代的一块。

## 架构模块

Mono 的运行时可以拆成六个核心模块。这不是按目录结构硬划的分类，而是按职责边界划分的逻辑模块。

### metadata — 元数据加载与解析

对应源码目录：`mono/metadata/`

这个模块负责把 PE/COFF 格式的 .NET 程序集加载到内存，解析其中的 metadata stream（`#Strings`、`#US`、`#Blob`、`#GUID`、`#~`），构建运行时可用的类型和方法描述。

核心数据结构是 `MonoImage`，它代表一个已加载的程序集镜像。从 `MonoImage` 出发，可以查询到所有的类型定义（`MonoClass`）、方法定义（`MonoMethod`）、字段定义、接口实现等。

这一层的职责和 LeanCLR 的 `metadata/` 目录功能一致：都是在做 ECMA-335 Partition II 定义的 metadata table 解析。但 Mono 的 metadata 模块还额外承担了程序集搜索、版本匹配、GAC（Global Assembly Cache）查找等职责——这些在 LeanCLR 中被刻意省略了。

### mini — JIT 编译器

对应源码目录：`mono/mini/`

mini 是 Mono 的 JIT 编译引擎。名字叫"mini"是因为它替换了更早期的"old JIT"——相对于前代，它更轻量。

mini 的编译管线大致是：

```
IL → 基本块分割 → SSA 构建 → 优化 pass → 寄存器分配 → native code 生成
```

mini 内部使用一种自定义的 IR（中间表示），不是直接操作 IL。优化 pass 包括常量传播、死代码消除、内联等标准编译器优化。native code 生成阶段有多个后端：x86、x64、ARM、ARM64、MIPS、S390x、PowerPC 等。

这是 Mono 和 CoreCLR（RyuJIT）在同一层面上的对应物。两者的输入都是 IL，输出都是 native code，但内部的 IR 设计和优化策略完全不同。

### interpreter（mint/interp）— 解释器

对应源码目录：`mono/mini/interp/`

Mono 的解释器有两代。第一代叫 mint（Mono Interpreter），是 Mono 项目最早期的执行引擎，在 JIT 还没实现之前用来跑 IL。后来 JIT 成熟后，mint 被搁置。

第二代在 .NET Core 时代被重新启用，内部代号 interp。重新启用的原因有两个：一是 WebAssembly 平台不支持 JIT（和 iOS 类似的限制），需要一个解释器来执行动态加载的 IL；二是 Full AOT 模式下有些场景（泛型的延迟实例化、反射调用）仍然需要解释器作为 fallback。

这个模块是本系列 MONO-C2 的重点，这里只标注位置。

### sgen — 垃圾回收器

对应源码目录：`mono/sgen/`

SGen（Simple Generational GC）是 Mono 的现代 GC 实现。它是一个精确式分代垃圾回收器：

- **Nursery**（新生代）：使用复制算法（copying collector），默认 4MB
- **Major Heap**（老年代）：可选 mark-sweep 或 mark-compact
- **精确式**：依赖 GC descriptor 来精确知道对象中哪些字段是引用

SGen 替代了 Mono 早期使用的 Boehm GC（保守式 GC）。Boehm GC 的问题是无法区分"看起来像指针的整数"和"真正的指针"，导致内存无法被正确回收。SGen 通过在类型系统中嵌入 GC 描述符来解决这个问题。

这和 CoreCLR 的 GC 在设计目标上一致（都是精确式分代 GC），但 CoreCLR 的 GC 规模和复杂度远超 SGen——CoreCLR 支持 Workstation/Server 两种模式、后台 GC、Pinned Object Heap 等高级特性。

### io-layer — 操作系统抽象层

对应源码目录：`mono/utils/`、`mono/io-layer/`（历史版本）

这一层封装了操作系统的差异：线程创建、互斥锁、信号量、文件 I/O、socket、进程管理等。Mono 的跨平台能力很大程度上依赖这一层的实现。

在早期版本中，io-layer 试图在 Unix 上模拟 Win32 API 的行为。后来的版本逐步移除了这种设计，改为直接使用 POSIX API 并在上层做抽象。

### icall — 内部调用

对应源码目录：`mono/metadata/icall-def.h`、`mono/metadata/icall.c`

Internal call 是 BCL（Base Class Library）中标记为 `[MethodImpl(MethodImplOptions.InternalCall)]` 的方法。这些方法没有 IL 方法体，它们的实现直接由 runtime 的 C 代码提供。

典型的 icall 包括：
- `System.Type.GetType()` — 从字符串查找类型
- `System.Array.Copy()` — 数组复制
- `System.String.InternalAllocateStr()` — 字符串分配
- `System.Threading.Thread.Sleep_internal()` — 线程休眠

LeanCLR 的 `icalls/` 目录用 61 个文件实现了 BCL 最常用的 internal call。Mono 的 icall 数量远超 LeanCLR——因为 Mono 需要支持完整的 BCL，而 LeanCLR 只需要支持最小可用集合。

## 四种执行模式

Mono 最独特的架构特征是它支持四种执行模式。没有任何其他主流 CLR 实现同时提供这么多选择。

### JIT（默认模式）

IL 在方法首次被调用时，由 mini 编译为 native code，缓存后执行。后续调用直接执行缓存的 native code。

这是桌面和服务端场景的默认模式。JIT 的优势是可以利用运行时信息做优化（比如 inline caching、profile-guided 分支预测），代价是首次调用有编译延迟。

### Full AOT

所有 IL 在构建期预编译为 native code，运行时不生成任何新代码。

这个模式的存在是因为 iOS（和一些游戏主机）的安全策略：操作系统内核禁止应用在运行时分配可执行内存页（W^X policy）。JIT 的本质就是在运行时生成可执行代码，所以在这些平台上 JIT 不可用。

Full AOT 的限制：
- 泛型的延迟实例化受限——编译期必须能推断出所有用到的泛型实例
- 反射调用的 `Emit` 系列 API 不可用——因为 Emit 的本质就是运行时生成 IL
- 部分动态特性（如 `DynamicMethod`）不可用

这些限制和 IL2CPP 面临的 AOT 限制是完全一样的。IL2CPP 的 AOT 泛型补充元数据、HybridCLR 的 `supplementary metadata` 机制，追根溯源都是在解决 Full AOT 遗留下来的泛型实例化问题。

### Interpreter

直接解释执行 IL 字节码，不生成 native code。

Mono 解释器的主要用途有两个：作为 Full AOT 的 fallback（处理 AOT 编译期无法覆盖的泛型实例化），以及在 WebAssembly 平台上作为主执行引擎。

### Mixed（AOT + Interpreter）

预编译尽可能多的方法为 native code，对无法 AOT 的方法（典型的是泛型的延迟实例化）fallback 到解释器执行。

这是 Mono 在 .NET Core 时代引入的实用模式。它避免了 Full AOT 的"必须在编译期覆盖所有泛型实例"限制，同时保留了 AOT 的性能优势。

从设计哲学上看，Mixed 模式和 HybridCLR 的思路有共通之处——都是"AOT 为主、解释器兜底"。区别在于 HybridCLR 的 AOT 层是 IL2CPP（C# → C++ → native），而 Mono 的 AOT 层是 Mono 自己的 mini + LLVM 后端。

## Mono 在 Unity 中的角色

### Unity 2017 之前：唯一的 runtime

Unity 从最早期就选择 Mono 作为脚本运行时。这个选择在当时几乎是唯一合理的——CoreCLR 还不存在（CoreCLR 2016 年才开源），.NET Framework 是 Windows 专属，Mono 是唯一能跨平台运行 C# 的开源 CLR。

在这个阶段，Unity 使用的是自己 fork 的 Mono 分支。Unity 的 fork 和上游 Mono 有不少差异：固定在较老的 C# 语言版本（长期停留在 .NET 3.5 / C# 4）、移除了一些 Unity 不需要的模块、添加了 Unity 特有的嵌入 API。

### 编辑器仍然使用 Mono

即使在 Player 端全面转向 IL2CPP 之后，Unity 编辑器仍然运行在 Mono 上。原因很直接：编辑器需要 JIT。

编辑器的脚本编译—域加载—执行循环需要能快速加载新编译的 DLL 并立即执行。AOT 方案无法满足这个需求——每次修改脚本都要走一遍完整的 AOT 编译流程，延迟不可接受。

Mono 的 JIT 模式让编辑器可以在几百毫秒内完成从"脚本修改"到"编辑器里看到效果"的循环。这个开发体验是 IL2CPP 无法提供的。

### Player 端为什么转向 IL2CPP

Unity 从 2015 年开始引入 IL2CPP，逐步替代 Mono 成为 Player 端的默认 runtime。驱动这个转变的不是单一原因，而是多个因素叠加。

**iOS 平台的 JIT 限制。** iOS 的 W^X 策略禁止 JIT。Mono 的 Full AOT 可以解决这个问题，但它的 AOT 管线不够成熟——泛型覆盖率问题频繁出现，调试体验差。

**性能。** Mono 的 JIT（mini）在代码质量上和 CoreCLR 的 RyuJIT 有明显差距。mini 的优化 pass 较少，生成的 native code 质量一般。IL2CPP 把 IL 转成 C++ 后交给平台 C++ 编译器（Clang、MSVC、GCC），可以利用这些编译器多年积累的优化能力。

**代码保护。** Mono JIT 模式下，DLL 文件包含完整的 IL 字节码，反编译几乎是零成本。IL2CPP 把 IL 转成 native code 后，逆向工程的难度大幅提升。

**C# 版本升级困难。** Unity 长期 fork 自己的 Mono 分支，和上游 Mono 的版本差距越来越大。升级 C# 语言版本意味着同步大量上游改动，工程成本极高。IL2CPP 方案下，前端（Roslyn 编译器）和后端（运行时）解耦——升级 C# 版本只需要升级 Roslyn，不需要改动运行时。

**平台覆盖。** Mono 对某些平台的支持不够完善（尤其是游戏主机）。IL2CPP 的输出是标准 C++ 代码，理论上可以在任何有 C++ 编译器的平台上运行。

这五个因素叠加在一起，使得 Unity 选择了一条看似激进但工程上合理的路：不升级 Mono，而是用一个全新的 AOT 方案替代它。

## MonoClass / MonoMethod / MonoImage

Mono 的类型系统围绕三个核心结构展开。理解它们，是后续分析 JIT、GC、解释器的基础。

### MonoImage

`MonoImage` 代表一个已加载的程序集镜像。它包含：

- PE/COFF header 信息
- 五个 metadata stream 的解析结果
- 所有 metadata table 的访问入口
- assembly reference（引用的其他程序集）
- string heap 的缓存

`MonoImage` 的定位等价于 LeanCLR 的 `CliImage`，也等价于 IL2CPP 的 `Il2CppImage`。三者都是 ECMA-335 metadata 的运行时表示，只是内部字段和缓存策略不同。

### MonoClass

`MonoClass` 代表一个运行时类型。它是 Mono 类型系统的核心结构，包含：

- 类型名称、命名空间、所属 assembly
- 父类指针、接口列表
- 字段列表（`MonoClassField`）、方法列表（`MonoMethod`）
- vtable（虚方法表）
- 实例大小、对齐要求
- GC descriptor（用于精确 GC 的字段引用标记）

| 概念 | Mono | CoreCLR | IL2CPP | LeanCLR |
|------|------|---------|--------|---------|
| 类型描述 | `MonoClass` | `MethodTable` + `EEClass` | `Il2CppClass` | `RtClassInfo` |
| 方法描述 | `MonoMethod` | `MethodDesc` | `MethodInfo` | `RtMethodInfo` |
| 程序集镜像 | `MonoImage` | `Module` | `Il2CppImage` | `CliImage` |

CoreCLR 把类型描述拆成了两个结构：`MethodTable` 放运行时高频访问的数据（vtable、接口映射、GC descriptor），`EEClass` 放低频的静态信息（字段列表、方法列表）。Mono 和 IL2CPP 都选择了单结构设计——一个 `MonoClass` / `Il2CppClass` 包含所有信息。

### MonoMethod

`MonoMethod` 代表一个方法的运行时描述。它包含：

- 方法名称、签名
- 所属类型（`MonoClass*`）
- 方法的 IL 字节码位置（RVA）
- 如果已经 JIT 编译，指向 native code 的指针
- 如果是 internal call，指向 C 实现函数的指针

这个结构的生命周期贯穿了 Mono 的整个执行管线：metadata 模块负责创建它，JIT 模块负责编译它指向的 IL 并填入 native code 指针，解释器模块直接读取它的 IL 字节码来执行。

## 与其他 runtime 的定位对比

把 Mono 和系列中涉及的其他四个 runtime 放在一起：

| 维度 | Mono | CoreCLR | IL2CPP | HybridCLR | LeanCLR |
|------|------|---------|--------|-----------|---------|
| 代码规模 | ~200 万行 C | ~500 万行 C/C++ | 闭源（推测 50 万行+） | ~10 万行 C++ | ~7.3 万行 C++ |
| 执行策略 | JIT + AOT + Interpreter + Mixed | JIT（Tiered） | 全量 AOT | IL2CPP AOT + Interpreter | 纯解释器 |
| GC | SGen（精确式分代） | 精确式分代（Server/Workstation） | BoehmGC（保守式） | 复用 IL2CPP 的 BoehmGC | Stub（委托宿主） |
| 泛型 | 运行时实例化 | 运行时实例化 + 共享 | 编译期实例化 | 补充 metadata + 运行时实例化 | 运行时实例化 |
| 目标平台 | 几乎所有主流平台 | 桌面 + 服务端 + 移动 | Unity Player | Unity Player（热更新层） | H5 / 小游戏 / 嵌入式 |
| 开源 | MIT | MIT | 闭源 | MIT | MIT |
| 当前状态 | 合并到 dotnet/runtime | .NET 主线 runtime | Unity 维护 | 社区维护 | 开发中 |

几个值得注意的差异：

Mono 是唯一同时提供四种执行模式的 CLR。CoreCLR 以 JIT 为核心（Tiered Compilation 只是 JIT 的分级策略），IL2CPP 只有 AOT，LeanCLR 只有解释器。

Mono 的 GC（SGen）和 CoreCLR 的 GC 都是精确式的，但 IL2CPP 使用的 BoehmGC 是保守式的。这个差异在 ECMA-A6 中已经讨论过：保守式 GC 的缺点是无法移动对象（因为不确定"看起来像指针的值"是否真的是指针），因此无法做压缩（compaction），长期运行会产生内存碎片。

Mono 的代码规模（约 200 万行）介于 CoreCLR 和 LeanCLR 之间。这个体量对个人阅读来说偏大，但远比 CoreCLR 可控。

## 后续路线图

Mono 模块计划 6 篇文章，覆盖从解释器到 GC 的完整实现链路：

| 编号 | 主题 | 核心问题 |
|------|------|----------|
| C1 | 架构总览（本篇） | Mono 的模块边界和执行模式 |
| C2 | 解释器（mint/interp） | Mono 直接解释 IL 的设计，与 LeanCLR 双解释器的对比 |
| C3 | JIT（Mini） | IL → SSA → native 的编译管线 |
| C4 | AOT | Full AOT 与 LLVM 后端，与 IL2CPP AOT 的对比 |
| C5 | SGen GC | 精确式分代 GC，与 CoreCLR GC 和 BoehmGC 的对比 |
| C6 | Mono 在 Unity 中的角色 | 历史演进与技术决策的深度分析 |

C2 紧接本篇，拆解 Mono 解释器的执行模型，并和 LeanCLR 的双解释器做正面对比。

## 收束

Mono 在 CLR 实现的谱系中占据一个独特的位置：它不是最强的（性能不如 CoreCLR），不是最轻的（体积不如 LeanCLR），不是 Unity 的未来（已被 IL2CPP 替代）。但它是覆盖面最广的——JIT、AOT、解释器、Mixed 四条路径全部实现，几乎所有主流平台都能运行。

理解 Mono 架构的意义不在于"用 Mono"，而在于用它作为坐标原点。当看到 IL2CPP 把 C# 转成 C++ 时，参照系是 Mono 的 Full AOT。当看到 HybridCLR 在 IL2CPP 里补解释器时，参照系是 Mono 的 Mixed 模式。当看到 LeanCLR 用 600KB 实现一个 CLR 时，参照系是 Mono 的 200 万行代码。

这些对比不是为了判断谁更好，而是为了让每个 runtime 的设计决策在同一张地图上各归其位。

## 系列位置

- 上一篇（跨模块）：ECMA-A7 泛型实例化模型（待发布）
- 下一篇：[MONO-C2 解释器]({{< ref "mono-interpreter-mint-interp-vs-leanclr" >}})
