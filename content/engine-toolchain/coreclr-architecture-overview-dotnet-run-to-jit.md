---
title: "CoreCLR 实现分析｜架构总览：从 dotnet run 到 JIT 执行的完整链路"
date: "2026-04-14"
description: "从 dotnet run 命令出发，拆解 CoreCLR 的完整执行链路：host → runtime init → assembly loading → type system → JIT compilation → native execution。定位 MethodTable、EEClass、RyuJIT、GC 在这条链里的职责。"
weight: 40
featured: false
tags:
  - CoreCLR
  - CLR
  - JIT
  - Runtime
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> CoreCLR 是 .NET 世界的参考实现——它的 JIT、GC、type system 设计是所有其他 CLR 实现（包括 IL2CPP 和 LeanCLR）的对标基准。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 1 篇。

前面的 ECMA-335 基础层解决的是规范本身——CLI 的类型系统、元数据格式、CIL 指令集、执行模型。这篇开始进入第一个具体实现：CoreCLR。目标不是把每个子系统讲透（后续 B2~B10 各有专题），而是先把从 `dotnet run` 到 native code 执行的完整链路走一遍，建立一张够用的全景地图。

## 为什么要分析 CoreCLR

CoreCLR 是 .NET 的参考实现，开源在 github.com/dotnet/runtime 仓库的 `src/coreclr/` 目录下。

"参考实现"这四个字意味着：当 ECMA-335 规范留有歧义时，CoreCLR 的做法就是事实标准。其他 CLR 实现在设计同样的子系统时，要么对齐 CoreCLR 的行为，要么有意识地偏离并承担不兼容的后果。

理解 CoreCLR 的设计决策，才能理解其他 runtime 为什么在同样的问题上做了不同的选择：

- IL2CPP 为什么把 MethodTable 和 EEClass 的信息合到一个 `Il2CppClass` 里？因为 AOT 场景下不需要热/冷分离。
- LeanCLR 为什么不用 JIT 而走双层解释器？因为目标平台（H5/小游戏）不允许运行时生成 native code。
- Mono 为什么同时保留 JIT 和解释器？因为嵌入式场景需要在两种模式之间灵活切换。

这些选择都不是凭空出现的。它们是在 CoreCLR 建立的基线上，针对不同约束做出的取舍。CoreCLR 就是那条基线。

## 从 dotnet run 到 Main 方法

在终端输入 `dotnet run`，到 C# 的 `Main` 方法开始执行，中间经过了一条分工明确的启动链路。这条链路是理解 CoreCLR 整体架构的入口。

### 启动链路全景

```
dotnet.exe (managed host)
  │
  ├── hostfxr.dll — 框架解析：找到目标框架版本和 runtime 路径
  │     │
  │     └── hostpolicy.dll — 加载策略：确定程序集搜索路径、deps.json 解析
  │           │
  │           └── coreclr.dll — CoreCLR 运行时本体
  │                 │
  │                 ├── coreclr_initialize() — 初始化运行时
  │                 │     ├── EEStartup — 执行引擎启动
  │                 │     ├── GC 初始化
  │                 │     ├── JIT 初始化 (RyuJIT)
  │                 │     └── AppDomain 创建
  │                 │
  │                 └── coreclr_execute_assembly() — 执行入口程序集
  │                       ├── Assembly::Load — 加载程序集
  │                       ├── EntryPoint 解析 — 找到 Main 方法
  │                       ├── JIT 编译 Main — IL → native code
  │                       └── 调用 native Main — 程序开始执行
```

### 三级 host 分层

启动链路的前三步是 host 层，不属于 runtime 本体。这个分层是有意设计的：

**dotnet.exe** 是最外层的入口。它的职责只有一个：找到并加载 `hostfxr.dll`。

**hostfxr.dll** 负责框架解析。当项目的 `.runtimeconfig.json` 指定了目标框架（比如 `net9.0`），hostfxr 要找到机器上对应版本的 runtime 安装目录。如果安装了多个版本，它还要处理 roll-forward 策略。

**hostpolicy.dll** 负责加载策略。它解析 `deps.json` 文件，确定程序集的搜索路径和依赖关系，然后调用 `coreclr_initialize` 把 runtime 真正启动起来。

这三级分层让 host 和 runtime 解耦。同一个 CoreCLR 可以被不同的 host 加载——`dotnet.exe` 是一种，自托管（self-contained）应用是另一种，ASP.NET 的 `w3wp.exe` 又是另一种。host 层只负责"找到并启动 runtime"，不参与后续的执行逻辑。

### Runtime 初始化

`coreclr_initialize` 是 runtime 真正的入口。它触发 `EEStartup`（Execution Engine Startup），按顺序完成以下初始化：

1. **线程子系统初始化** — 创建 finalizer 线程、线程池初始化
2. **GC 初始化** — 根据配置选择 Workstation 或 Server GC，分配初始堆
3. **JIT 初始化** — 创建 RyuJIT 编译器实例，注册编译接口
4. **类型系统基础结构** — 预加载基础类型（`System.Object`、`System.String`、`System.ValueType` 等）
5. **AppDomain 创建** — 初始化默认应用程序域

初始化完成后，runtime 处于就绪状态。此时还没有加载任何用户程序集，也没有 JIT 编译任何用户方法。

### 从 Assembly 到 Main

`coreclr_execute_assembly` 接过控制权后，开始加载用户程序集并执行入口方法：

1. **Assembly 加载** — 通过 `AssemblyLoadContext` 加载入口程序集（.dll），解析 PE 头和 metadata
2. **EntryPoint 解析** — 从 metadata 的 CLI header 中读取入口方法的 token（通常是 `Main` 方法的 MethodDef token）
3. **方法准备** — 为 `Main` 方法创建 `MethodDesc` 对象，此时方法指针指向 JIT 编译触发器（prestub）
4. **JIT 触发** — 第一次调用 `Main` 时，通过 prestub 触发 RyuJIT 编译，IL → native code
5. **执行** — JIT 产生的 native code 被写入代码堆，方法指针替换为 native 地址，`Main` 开始执行

从这条链路可以看到，CoreCLR 的设计是**延迟的**——不到真正调用方法的那一刻，不会触发 JIT 编译。这和 IL2CPP 的"构建时全量 AOT"是完全相反的策略。

## CoreCLR 的模块结构

dotnet/runtime 仓库中，CoreCLR 的源码主要在 `src/coreclr/` 下。按职责可以划分为 6 个核心子系统：

| 子系统 | 路径 | 职责 |
|--------|------|------|
| **VM** | `src/coreclr/vm/` | 类型系统、方法调用、AppDomain、执行引擎核心 |
| **JIT** | `src/coreclr/jit/` | RyuJIT 编译器：IL → native code |
| **GC** | `src/coreclr/gc/` | 分代精确 GC |
| **PAL** | `src/coreclr/pal/` | Platform Abstraction Layer：跨平台适配 |
| **Class Libraries** | `src/libraries/` | BCL（System.* 命名空间的基础类库） |
| **Interop** | `src/coreclr/vm/interop*` | P/Invoke、COM 互操作 |

### VM：最重的模块

`vm/` 是 CoreCLR 代码量最大的目录，承担了执行引擎的核心职责。类型加载、方法分派、字段访问、异常处理、线程管理、反射、安全检查——这些都在 VM 模块里。

如果做一个不精确但有用的类比：VM 之于 CoreCLR，相当于 `libil2cpp` 之于 IL2CPP。它是把 metadata、JIT 产物、GC 和 host 粘合在一起的那层运行时胶水。

### PAL：让 CoreCLR 跨平台

PAL 把操作系统的差异封装成统一的 API。线程创建、内存分配、文件 I/O、同步原语——在 Windows 上直接调用 Win32 API，在 Linux/macOS 上通过 PAL 适配成 POSIX 调用。

这和 LeanCLR 的策略不同。LeanCLR 用纯 C++17 标准库实现跨平台，不需要单独的 PAL 层。CoreCLR 的 PAL 存在是因为它的历史：最初 CoreCLR 深度依赖 Windows API（SEH、COM、注册表），跨平台迁移时不可能全部重写，只能用适配层包一层。

## 类型系统核心：MethodTable + EEClass

CoreCLR 类型系统中最关键的两个数据结构是 `MethodTable` 和 `EEClass`。每个被加载的类型都有这一对结构，它们通过互相引用配合工作。

### MethodTable：热数据

`MethodTable` 存放的是运行时高频访问的数据：

- **vtable 指针数组** — 虚方法分派表，每个 slot 对应一个虚方法的函数指针
- **接口映射（Interface Map）** — 记录该类型实现了哪些接口，以及接口方法在 vtable 中的偏移
- **GC 描述信息** — 告诉 GC 该类型实例中哪些字段是引用类型（需要扫描）
- **基本类型信息** — 实例大小、父类指针、模块引用
- **组件大小（Component Size）** — 数组类型特有，记录单个元素的大小

每个类型在 runtime 中只有一份 `MethodTable`。对象头中的类型指针（`MethodTable*`）就是指向这个结构。当 runtime 需要做类型检查、虚方法分派、接口调用、GC 扫描时，第一步都是通过对象头拿到 `MethodTable`。

### EEClass：冷数据

`EEClass` 存放的是低频访问的数据：

- **字段描述列表（FieldDesc）** — 每个字段的名字、偏移、类型
- **方法描述列表（MethodDesc）** — 每个方法的 metadata token、JIT 状态、函数指针
- **反射信息** — `System.Type` 需要的详细类型描述
- **泛型参数信息** — 如果是泛型类型，记录类型参数的约束和具体绑定

EEClass 不是每次方法调用都需要访问的——只有在反射、类型加载、调试时才会频繁查询。

### Hot/Cold 分离的设计理由

把一个类型的运行时信息拆成 MethodTable 和 EEClass，核心理由是**缓存友好性**。

在一个运行中的程序里，方法调用和 GC 扫描是最高频的操作。这两个操作需要的数据（vtable、GC 描述）全部集中在 MethodTable 里。而字段描述、反射信息这些只在特定场景下使用的数据被隔离在 EEClass 里。

这意味着：在正常执行期间，CPU 缓存里加载的主要是 MethodTable。EEClass 的数据不会污染缓存行。当程序的类型数量达到数千甚至数万时，这个分离对缓存命中率的影响是可测量的。

### 与其他 runtime 的对比

| 结构 | CoreCLR | IL2CPP | LeanCLR |
|------|---------|--------|---------|
| 类型描述 | MethodTable（热）+ EEClass（冷） | `Il2CppClass`（全混一起） | `RtClass`（精简版，不分离） |
| 设计理由 | 缓存友好，分离高频/低频访问 | AOT 场景无需分离，类型信息一次性加载 | 体量小，分离的收益不明显 |
| vtable 位置 | MethodTable 末尾的变长数组 | `Il2CppClass.vtable` 数组 | `RtClass` 中的 vtable 指针 |
| GC 信息 | MethodTable 中的 GCDesc | 独立的 GC 描述结构 | GC 当前为 stub |

IL2CPP 把 MethodTable 和 EEClass 的信息全部合到 `Il2CppClass` 里。这不是"设计不好"——在 AOT 场景下，所有类型信息在构建时就已确定，运行时不需要按需加载 EEClass，分离也就失去了意义。LeanCLR 的 `RtClass` 则是另一个极端：73K 行代码的运行时不需要为几百个类型做缓存优化，简单直接比精巧分离更合理。

## JIT 编译（RyuJIT）

RyuJIT 是 CoreCLR 的即时编译器，负责把 IL 字节码转换成目标平台的 native code。它是 CoreCLR 执行模型的核心。

### 编译管线

```
IL bytecode
  │
  ├── Importer — IL → RyuJIT HIR (High-level IR)
  │     把 IL 的栈操作转换为树形中间表示
  │
  ├── Optimizer — HIR 优化
  │     内联、常量折叠、死代码消除、循环优化
  │
  ├── Rationalize — HIR → LIR (Low-level IR)
  │     把树形 IR 线性化为指令序列
  │
  ├── Register Allocator — 寄存器分配
  │     LSRA (Linear Scan Register Allocation)
  │
  └── Code Generator — LIR → native code
        生成目标平台的机器码（x64/ARM64/...）
```

整个管线在方法首次调用时触发，编译产物被写入 CodeHeap（代码堆），后续调用直接执行 native code，不再经过 JIT。

### Stub 机制：延迟编译的关键

CoreCLR 用 stub 实现方法的延迟编译。当一个方法被加载但还没被调用时，它的函数指针不是指向最终的 native code，而是指向一个 **prestub**（预桩）。

```
方法首次调用前：
  MethodDesc.m_pCode → prestub（JIT 编译触发器）

方法首次调用时：
  prestub 被触发 → 调用 RyuJIT 编译 → 生成 native code → 写入 CodeHeap

方法编译后：
  MethodDesc.m_pCode → native code 地址（直接执行）
```

prestub 的本质是一小段汇编代码，它做的事情很简单：保存调用上下文，调用 JIT 编译当前方法，用编译产出的 native 地址替换自身，然后跳转到 native code 开始执行。从调用者的角度，这个过程是透明的——第一次调用会慢一点（因为触发了编译），之后的调用和直接调用 native 函数没有区别。

### 与 IL2CPP 和 LeanCLR 的对比

| 维度 | CoreCLR (RyuJIT) | IL2CPP (AOT) | LeanCLR (Interpreter) |
|------|-------------------|--------------|----------------------|
| 编译时机 | 运行时首次调用 | 构建时全量 | 不编译，逐条解释 |
| 产物 | native code（CodeHeap） | native code（GameAssembly） | 无（LL-IL 在解释器中执行） |
| 新泛型实例化 | 运行时随时可以 JIT | 构建时必须预备 | 运行时动态膨胀 |
| 启动速度 | 首次较慢（JIT 开销） | 快（代码已编译） | 取决于解释器效率 |
| 峰值性能 | 高（native code） | 高（native code） | 低（解释执行） |
| 平台限制 | 需要可写+可执行内存 | 无（已编译） | 无（纯解释） |

最后一行是理解三种策略选择的关键。iOS 和部分游戏主机禁止在运行时生成可执行代码（W^X 策略），这直接排除了 JIT。IL2CPP 的 AOT 和 LeanCLR 的解释器都是对这个限制的不同回应。

## GC 概览

CoreCLR 的 GC 是分代精确 GC，与 IL2CPP 使用的 BoehmGC 在设计哲学上有本质差异。

### 堆结构

CoreCLR GC 管理 5 个区域：

| 区域 | 说明 |
|------|------|
| **Gen 0** | 新对象分配区。大多数对象在这里分配，也在这里被回收 |
| **Gen 1** | 从 Gen 0 存活下来的对象。作为 Gen 0 和 Gen 2 之间的缓冲区 |
| **Gen 2** | 长期存活对象。Full GC 时才扫描 |
| **LOH** | Large Object Heap。大于 85,000 字节的对象直接分配在这里，不经过分代晋升 |
| **POH** | Pinned Object Heap（.NET 5+）。需要被 pin 的对象分配在这里，避免 pin 导致堆碎片化 |

分代的核心假设是**代际假说**：大多数对象生命周期很短。通过把短命对象集中在 Gen 0 做高频回收，避免每次都扫描整个堆。

### Precise GC

"精确"意味着 GC 能准确区分一个内存位置存的是对象引用还是普通数值。

这个能力依赖于 JIT 在编译方法时生成的 **GC 信息**：对于每个 GC 安全点，JIT 记录了当前栈帧和寄存器中哪些位置包含对象引用。GC 扫描时只跟踪这些位置，不会误把一个碰巧看起来像指针的整数当成对象引用。

### GC 安全点与挂起

GC 回收时需要暂停所有托管线程（Stop-the-World）。但不能在任意指令处暂停——必须在 **GC 安全点（safepoint）** 停下来，此时栈帧的布局是 JIT 记录过的，GC 能正确枚举所有引用。

安全点通常插入在方法调用处、循环回边、方法返回处。JIT 在编译时会在这些位置插入检查代码，如果 GC 请求挂起，线程会在最近的安全点停下来。

### 与 IL2CPP BoehmGC 的核心差异

| 维度 | CoreCLR GC | IL2CPP + BoehmGC |
|------|-----------|-----------------|
| 精确性 | 精确式——JIT 提供完整的引用位置信息 | 保守式——假设任何看起来像指针的值都可能是引用 |
| 分代 | 3 代 + LOH + POH | 无分代 |
| 压缩 | Gen 0/1 做压缩（移动对象消除碎片） | 不移动对象，靠 free list 管理碎片 |
| 误报 | 无——只跟踪真正的引用 | 可能有——把整数误认为引用，导致对象无法回收 |
| 暂停模式 | 支持 Background GC（Gen 2 并发回收） | 全量 Stop-the-World |

保守式 GC 的"误报"在实际项目中很少造成严重问题，但它确实意味着 BoehmGC 不能做堆压缩（移动对象），因为它不确定一个值是不是真的指针——如果把一个碰巧等于地址值的整数"更新"了，程序就会崩溃。

## 异常处理

CoreCLR 实现了 ECMA-335 定义的两遍扫描异常处理模型（对应 ECMA-A4 讲的 CLI Execution Model）。

### 两遍扫描

当异常被抛出时，runtime 执行两遍栈扫描：

**第一遍（First Pass）**：从抛出点开始向上遍历调用栈，查找匹配的 catch 子句。只查找，不执行任何 handler。如果找到匹配的 catch，记录它的位置。如果整个栈都没有匹配的 catch，触发未处理异常流程。

**第二遍（Second Pass）**：从抛出点再次向上遍历，这次会执行路径上的所有 finally 和 fault 子句，直到到达第一遍找到的 catch 子句位置，然后执行 catch handler。

两遍扫描的设计保证了 finally 块一定在 catch 之前执行——这是 ECMA-335 规定的语义。

### 与操作系统的集成

在 Windows 上，CoreCLR 的异常处理构建在 OS 的结构化异常处理（SEH）之上。托管异常被包装成 SEH 异常，利用 OS 的栈展开机制来遍历调用栈。

在 Linux/macOS 上，CoreCLR 使用 libunwind 实现栈展开。PAL 层负责把平台差异屏蔽掉，让 VM 层的异常处理逻辑保持统一。

### 与 IL2CPP 的差异

IL2CPP 的异常处理使用 `setjmp`/`longjmp` 机制（在支持 C++ 异常的平台上也可使用 C++ exceptions）。这是一种更简单的实现方式：在 try 块入口调用 `setjmp` 保存上下文，异常抛出时调用 `longjmp` 跳回保存的上下文。

这种方式的优点是实现简单、跨平台统一。缺点是 `setjmp` 在每次进入 try 块时都有开销（即使没有异常发生），而 CoreCLR 的基于 SEH / libunwind 的方案在无异常路径上几乎零开销。

## Assembly 加载

CoreCLR 的 Assembly 加载通过 `AssemblyLoadContext`（ALC）实现。ALC 是 .NET Core 引入的隔离机制，替代了 .NET Framework 时代的 AppDomain 加载隔离。

### Default ALC 与 Custom ALC

**Default ALC** 是应用启动时自动创建的加载上下文。所有通过正常引用关系解析的程序集都加载到 Default ALC 中。它的生命周期和应用一致。

**Custom ALC** 是用户创建的加载上下文。它支持程序集隔离和**卸载**——当一个 Custom ALC 被卸载时，其中加载的所有程序集和相关的类型、JIT 代码都会被回收。

```csharp
// 创建可卸载的加载上下文
var alc = new AssemblyLoadContext("plugin", isCollectible: true);

// 在隔离环境中加载插件
Assembly pluginAssembly = alc.LoadFromAssemblyPath(pluginPath);

// 使用完毕后卸载
alc.Unload();  // 触发卸载流程，GC 最终回收所有相关资源
```

ALC 的卸载不是即时的——它依赖 GC 来回收关联的对象。只有当所有对该 ALC 中类型的引用都被释放后，GC 才能真正回收这些资源。

### 与 IL2CPP MetadataCache 的对比

IL2CPP 的程序集加载通过 `MetadataCache` 完成，所有 metadata 在初始化时一次性加载。不支持运行时动态加载新程序集，也不支持卸载。

这个差异直接决定了两个 runtime 在热更新能力上的根本不同。CoreCLR 可以在运行时加载新的程序集并执行其中的代码（因为有 JIT），也可以通过 ALC 卸载不再需要的程序集。IL2CPP 在构建时就冻结了所有程序集的内容——这正是 HybridCLR 要解决的问题。

## CoreCLR vs 其他 Runtime 定位

| 维度 | CoreCLR | Mono | IL2CPP | LeanCLR |
|------|---------|------|--------|---------|
| **执行模式** | JIT + Tiered Compilation | JIT + Interpreter + Full AOT | AOT only | Interpreter only |
| **GC** | 分代精确 GC | SGen（精确分代） | BoehmGC（保守式） | Stub（设计目标：精确协作式） |
| **类型系统** | MethodTable + EEClass | MonoClass | Il2CppClass | RtClass |
| **热更新** | 支持（ALC + JIT） | 部分支持 | 不支持（HybridCLR 补丁） | 支持（解释执行） |
| **体积** | ~50MB+ | ~10-20MB | 随 libil2cpp | ~600KB |
| **目标场景** | 服务端、桌面、云 | Unity Editor、嵌入式 | Unity Player（移动/主机） | H5/小游戏/嵌入式 |
| **源码** | 开源 (MIT) | 开源 (MIT) | 闭源 | 开源 (MIT) |
| **ECMA-335 合规度** | 最完整 | 高 | 选择性实现 | 核心子集 |

这张表不是在排优劣。每个 runtime 的设计选择都是目标场景约束下的合理结果。CoreCLR 能做到最完整的 ECMA-335 覆盖和最高的执行性能，但它的体积和对 JIT 的依赖使它不适合移动端游戏和资源受限平台。IL2CPP 放弃了运行时灵活性换来了 native 性能和平台兼容性。LeanCLR 放弃了 native 性能换来了极小的体积和零依赖的嵌入能力。

## 后续路线图

这篇建立的是全景地图。后续每篇文章深入一个子系统：

| 编号 | 主题 | 目标 |
|------|------|------|
| CLR-B2 | 程序集加载 | 拆解 AssemblyLoadContext、Fusion、Binder 的完整加载链路 |
| CLR-B3 | 类型系统 | 深入 MethodTable、EEClass、TypeHandle 的内存布局和交互 |
| CLR-B4 | JIT 编译器（RyuJIT） | IL → HIR → LIR → native code 的完整编译管线 |
| CLR-B5 | GC | 分代策略、Workstation vs Server、Background GC、POH |
| CLR-B6 | 异常处理 | 两遍扫描的实现细节、SEH 集成、性能特征 |
| CLR-B7 | 泛型实现 | 引用类型代码共享 vs 值类型特化的具体机制 |
| CLR-B8 | 线程与同步 | Thread、Monitor、ThreadPool 的实现 |
| CLR-B9 | Reflection 与 Emit | 运行时代码生成能力，以及 IL2CPP 为什么没有 Emit |
| CLR-B10 | Tiered Compilation | 多级 JIT 与 PGO 的性能优化策略 |

## 收束

CoreCLR 的架构可以压到一条主线：

`dotnet.exe → hostfxr → hostpolicy → coreclr_initialize → Assembly Load → JIT → native execution`

这条主线串起了 6 个子系统。VM 负责把所有东西粘在一起，JIT 把 IL 变成 native code，GC 管理堆内存，类型系统用 MethodTable + EEClass 描述每个类型，AssemblyLoadContext 管理程序集的加载和卸载，PAL 屏蔽平台差异。

这些子系统的设计决策——JIT 延迟编译、hot/cold 数据分离、精确 GC、两遍扫描异常处理——不是孤立的技术细节，而是在"服务端/桌面场景、允许运行时代码生成、追求峰值性能"这个约束集合下的系统性选择。

把这些选择记住，后面看 IL2CPP 为什么全量 AOT、LeanCLR 为什么走双层解释器、Mono 为什么保留 JIT 和解释器两条路，就能看到每个 runtime 在基线上偏离的方向和理由。

## 系列位置

- 上一篇：无（CoreCLR 模块首篇）
- 下一篇：CLR-B2 程序集加载：AssemblyLoadContext、Fusion、Binder
