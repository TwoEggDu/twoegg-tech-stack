---
title: "ECMA-335 基础｜术语约定：runtime、toolchain、metadata、execution engine 的边界"
slug: "ecma335-terminology-conventions"
date: "2026-04-14"
lastmod: "2026-10-25"
description: "同一个词在不同语境下含义不同。这篇术语约定页按易混淆词对组织，定义 runtime vs toolchain、metadata vs stream vs table、execution engine vs interpreter vs JIT、hot update vs hot reload vs hot fix 等核心术语的精确边界，作为全系列的参照锚点。"
weight: 9
featured: false
tags:
  - "ECMA-335"
  - "Terminology"
  - "CLR"
  - "Convention"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
primary_series: "ecma335"
series_order: 10
---

> 如果一个系列的不同文章里，同一个词指代了不同的东西，读者建立的心智模型就会在某一篇突然崩塌。术语约定不是学术洁癖，而是文章能被串联阅读的前提。

这是 ECMA-335 前置主线的导读，主要服务于 IL2CPP 与 HybridCLR；其他 runtime 的文章也可复用这些术语。

## 为什么需要术语约定

本前置系列主要服务于 IL2CPP 与 HybridCLR；生态总图另有 CoreCLR、Mono、LeanCLR 等实现专题。同一个词在规范层、实现层和工程层可能指代不同的东西，进入具体实现前要先确定它所在的层次。

最典型的例子是"IL2CPP"。它可以指：

- **转换器**——`il2cpp.exe`，构建时把 IL 代码翻译成 C++ 源码的工具
- **运行时**——`libil2cpp`，游戏运行时提供类型系统、GC、线程管理的 runtime 库
- **整套方案**——Unity 的 IL2CPP scripting backend，涵盖从 C# 编译到 native binary 的完整管线

一篇文章说"IL2CPP 不支持运行时加载新程序集"，指的是 runtime 层。另一篇说"IL2CPP 会把泛型方法展开成多份 C++ 代码"，指的是转换器层。如果不区分，两句话放在一起读就会产生因果混乱。

类似的歧义在 metadata、execution engine、hot update 等词上也存在。这篇页面把这些易混淆词对逐组拆清，后续所有文章遵循这里的约定。

## Runtime vs Toolchain

**Runtime**（运行时）——程序执行时驻留在进程中的组件。负责类型加载、方法执行、内存管理、异常处理、线程调度。

- CoreCLR：`coreclr.dll` / `libcoreclr.so` + `clrjit.dll`
- Mono：`libmonosgen-2.0.so` / `mono-2.0-sgen.dll`
- IL2CPP：`libil2cpp`（编译进 `GameAssembly.dll` / `libil2cpp.so`）
- HybridCLR：对 IL2CPP runtime 的扩展，包含动态元数据注册、指令转换与解释执行等模块
- LeanCLR：`leanclr` 单库（编译为 `.a` / `.so` / `.wasm`）

**Toolchain**（工具链）——构建时运行的工具。把源码变成目标格式后就退出，不参与运行时执行。

- Roslyn（`csc.exe`）：C# → IL
- `il2cpp.exe`：IL → C++ 源码
- UnityLinker：程序集裁剪
- `mono --aot`：IL → native（Mono AOT 编译器）

**歧义高发区：IL2CPP 这个名字。** Unity 文档和社区讨论中，"IL2CPP"经常不加区分地指代转换器和 runtime。本系列的约定：

- 说"IL2CPP 转换器"或 `il2cpp.exe` 时，指构建时工具
- 说"IL2CPP runtime"或 `libil2cpp` 时，指运行时组件
- 说"IL2CPP 方案"或"IL2CPP scripting backend"时，指整套管线

同一段落中如果两者都涉及，会明确标注。

## Metadata vs Metadata Stream vs Metadata Table

这三个词在 ECMA-335 语境下是嵌套关系，不是同义词。

**Metadata**——CLI 元数据体系的总称。包含程序集中所有类型定义、方法签名、字段布局、引用关系的结构化描述。ECMA-335 Partition II 定义了它的完整规范。当文章说"runtime 加载 metadata"时，指的是这个整体。

**Metadata Stream**——物理存储层的概念。metadata 在 PE 文件中以多个 stream 的形式存放：

| Stream 名 | 内容 |
|-----------|------|
| `#~` 或 `#-` | 压缩/非压缩的 metadata 表数据 |
| `#Strings` | 字符串堆（类名、命名空间、方法名） |
| `#US` | 用户字符串堆（代码中的字符串字面量） |
| `#Blob` | 二进制数据（方法签名、字段签名、自定义属性） |
| `#GUID` | GUID 堆 |

**Metadata Table**——逻辑结构层的概念。`#~` stream 内部组织为多张表，每张表的行结构和列定义由 ECMA-335 Partition II 第 22-24 节规定：

- TypeDef 表：每行描述一个类型定义
- MethodDef 表：每行描述一个方法定义
- Field 表：每行描述一个字段定义，一些工具称为 FieldDef
- MemberRef 表：成员引用；跨程序集引用是常见用途，也用于构造泛型类型上的成员等情况

**约定：** 本系列中"metadata"无修饰时指整体概念。需要区分物理层和逻辑层时，会明确使用"stream"或"table"。[DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})用一个加法方法连接文件位置、表项、签名和方法体。

## Execution Engine vs Interpreter vs JIT

**Execution Engine**（执行引擎）——本系列中用于指 runtime 内执行方法体的子系统，不指定具体执行策略。ECMA-335 的 VES（Virtual Execution System）还涵盖类型、对象和存储等运行机制，不与这个狭义用法作严格同义替换。

**Interpreter**（解释器）——逐条读取 IL 指令（或转换后的中间指令），在软件层面模拟执行的方式。不生成 native code。

- LeanCLR 的双解释器：HL-IL 指令先 transform 为 LL-IL 指令，由低层解释器执行
- HybridCLR 的解释器：先将 CIL 转成内部寄存器指令，再解释执行转换结果
- Mono 的 mint/interp：Mono 内置的解释器模式

**JIT**（Just-In-Time Compiler）——运行时按需将方法编译为 native code。首次调用常是触发点，分层编译还可能重新编译热点方法。

- CoreCLR 的 RyuJIT
- Mono 的 Mini JIT

**约定：** "execution engine"在本系列中只作为泛称使用，不暗示任何具体策略。讨论具体实现时会直接说"解释器"或"JIT"。

HybridCLR 的 [官方模块说明](https://www.hybridclr.cn/docs/basic/sourceinspect)区分了 transform 与 interpreter。转换会准备后续执行需要的内部表示，因此首次调用成本和重复执行成本应分开观察；不能把它描述成每次都直接调度原始 CIL。

## Hot Update vs Hot Reload vs Hot Fix

这三个术语的使用场景有交叉，不能只靠名字推断内部机制。本系列分别说明交付目的、运行中应用修改的能力，以及具体实现方式。

**Hot Update**（热更新）——运行时加载新的代码逻辑，不需要重新发布客户端。这是一个通用概念，不绑定特定技术方案。实现热更新的路径有多种：

- **C# 热更**：HybridCLR 通过 `Assembly.Load` 加载热更 DLL，将其类型和方法接入宿主运行时。需要解释执行的方法经过 CIL → 内部指令转换；调用也可能进入已有 AOT 实现。执行路径应按具体方法和调用方式判断，不能按“类型属于哪个 DLL”或只凭 `invoker_method` 一个字段概括。详见 [调用主链]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})。
- **Lua 热更**：xLua / ToLua 通过 Lua VM 执行新逻辑，C# 侧提供绑定层。
- **ILRuntime**：独立的 IL 解释器，不依赖 IL2CPP runtime。
- **LeanCLR**：独立 CLR，运行时加载 .NET DLL 直接解释执行。

**Hot Reload**（热重载）——在程序运行期间应用代码或资源修改，尽量保留当前运行状态的能力。这个名称本身不保证旧程序集被卸载，也不保证旧对象或 metadata 内存被回收。需要继续区分具体机制：

- **程序集卸载与重新加载**：需要处理旧引用、对象和静态状态，再装入新版本。HybridCLR 相关产品能力见 [程序集卸载专题]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}})；可卸载范围和内存回收量须绑定版本与测试条件。
- **.NET Hot Reload**：对受支持的修改应用增量更新，不以卸载整个程序集为前提。`dotnet watch` 遇到无法热应用的修改时还可能要求重启，见 [Microsoft 官方说明](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch#hot-reload)。

**Hot Fix**（热修复）——描述修复问题的场景，不是独立的执行机制。项目可以利用热更新、增量更新等能力修复线上 bug；具体产品名中的 Hotfix 还需查对应功能定义。

**约定：** 讲部署新逻辑时用“热更新”，讲修复目的时用“热修复”；涉及状态保留和回收时，明确写“增量更新”或“程序集卸载重载”，不从 Hot Reload 的名字推导回收保证。

## Assembly vs Module vs Image

**Assembly**（程序集）——常见的部署和版本管理单元，manifest 描述其名称、版本、文化信息等身份信息；强命名程序集还带有公钥相关信息。ECMA-335 Partition II 6.1 定义了 assembly 的身份模型，具体绑定与信任检查由宿主策略决定。

**Module**（模块）——持有 metadata 表、token 和方法体的单元。ECMA-335 允许多模块程序集；Unity 常见的托管 DLL 是单模块程序集，但不能把这个工程习惯写成所有 `.dll` 文件都满足的等式。

**Image**——runtime 加载后的内存表示。不同 runtime 用不同的数据结构：

- CoreCLR：`PEImage` / `PEFile`
- Mono：`MonoImage`
- IL2CPP：`Il2CppImage`（运行时从 `global-metadata.dat` 构建）
- LeanCLR：`CliImage`（从 PE 文件最小化解析构建）

**约定：** 说“程序集”时指逻辑身份与部署单元，说“image”时指对应 runtime 的内部表示。解析 token 时必须说明所属模块；即使两个程序集都是单模块，也不能跨模块比较 token 数字来判断成员相同。

## AOT vs JIT vs Interpreter

三种方法执行策略的精确边界。

**AOT**（Ahead-Of-Time Compilation）——在构建时将 IL 编译为 native code。运行时不再需要编译步骤，直接执行预编译的 native code。

- IL2CPP：IL → C++ → native（通过 C++ 编译器）
- Mono Full AOT：IL → native，具体代码生成后端取决于配置
- .NET Native AOT：IL → native（通过 ILC + RyuJIT AOT 模式）

AOT 路径依赖预先生成的 native 实现；对于运行时才出现的调用，需要确认已有实现或共享代码是否适用。AOT 不意味着无法构造新的泛型类型描述，也不排除同一产品再集成解释器。动态生成方法体与构造泛型类型身份是两件事，见 [泛型语义]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})。

前面已经区分 JIT 与解释器。放到 AOT 旁边比较，关键是某次调用的实现何时、以什么形式准备好：JIT 可以在运行时生成机器码；解释器执行已有指令或转换后的内部表示，无须为每个新方法生成机器码。两者仍受目标平台、runtime 支持范围与输入信息是否完整的约束。

**混合策略：** 同一运行时可以组合执行策略。HybridCLR 可以复用主包已有的 AOT 实现，也可以解释执行热更方法，以及需要补充原始方法体的 AOT 泛型路径。因此要按具体调用判断，不能把程序集归属直接等同于执行方式；详见 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。Mono 也支持 JIT、AOT 与解释器等不同模式和组合，具体取决于配置。

CoreCLR 的 Tiered Compilation 则是 **JIT 内部的编译层级**：先较快生成代码，再对适合的方法重新优化。仅有分层 JIT，不能据此称为 AOT 与解释器的混合执行。

比较成本时，需要给出版本、平台和工作负载，并区分首次准备与稳定执行。解释执行的分派、数据访问和跨边界成本，也会随预解码、转换与优化而变化，不能给所有解释器套一个统一倍数。

**约定：** 本系列讨论执行策略时，会明确标注当前语境是哪种模式。不会笼统地说"Mono 的执行方式"——要指明是 JIT 模式、Full AOT 模式还是 Interpreter 模式。

## 示例怎样贯穿主线

各篇用最小例子突出当前机制，不要求把所有示例类合并成一个项目。跨篇比较时，先核对完整定义，不只比较类名：

| 示例 | 用途 | 接续位置 |
|---|---|---|
| MathHelper.Add | 连接方法签名、方法体与求值栈 | DLL／metadata 篇 → CIL 篇 |
| `Box<T>.Echo<U>` | 区分类型实参与方法实参 | 类型篇 → 签名解析篇 |
| Counter.Reset | 对比装箱副本与原变量位置 | 调用与异常篇 |
| `Cache<T>` | 区分共享代码与独立静态状态 | 类型初始化篇 → 泛型共享桥接 |

旧文中可能出现 Unit、Player 等其他类型；除非正文明确复用同一份代码，不把同名示例当成同一个类型。实验记录也应区分桌面 .NET、IL2CPP Player 和 HybridCLR 热更路径。

## 系列位置

- 下一篇：[DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
