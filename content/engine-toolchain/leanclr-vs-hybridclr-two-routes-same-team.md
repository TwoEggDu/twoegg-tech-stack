---
date: "2026-04-14"
title: "LeanCLR vs HybridCLR｜同一团队的两条技术路线：独立 CLR vs IL2CPP 补丁"
description: "从战略定位到架构实现的完整对比：15+ 维度架构总表、metadata 解析路径差异、解释器设计哲学分歧、类型系统复用与自建的取舍、方法调用链结构对比，以及两条路线在不同场景下的互补关系。"
weight: 79
featured: false
tags:
  - LeanCLR
  - HybridCLR
  - CLR
  - IL2CPP
  - Architecture
  - Comparison
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> Code Philosophy 的两个产品走了完全相反的路：HybridCLR 把解释器补进了 IL2CPP，LeanCLR 把整个 CLR 从零写了一遍。两条路都通向同一个目标——让 C# 代码能在任何平台上热更新。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 10 篇，也是 LeanCLR 模块的完结篇。

前 9 篇分别拆解了 LeanCLR 的每个子系统：metadata 解析、双解释器、对象模型、类型系统、方法调用链、内存管理、internal calls、WebAssembly 构建。每篇都会顺带和 HybridCLR 做局部对比，但始终没有一篇文章把两个产品放在一起，做一次完整的、系统性的架构对比。

这篇补上这个位置。

## 两条路线的战略定位

Code Philosophy（代码哲学）同时维护两个 C# 运行时产品，这在技术团队里并不常见。理解这个决策，需要先看清两个产品各自瞄准的问题域。

### HybridCLR：Unity 生态内的最优解

HybridCLR 的核心思路是：IL2CPP 已经构建了一套完整的运行时基础设施——类型系统、对象模型、GC、metadata 缓存、线程管理——但它缺少一个解释器。在 IL2CPP 内部补一个解释器，就能让热更新 DLL 的 IL 代码在 IL2CPP 运行时里执行，而不需要修改 Unity 引擎本身。

这条路线的前提是 IL2CPP 的存在。HybridCLR 不能脱离 IL2CPP 独立运行——它的类型系统是 `Il2CppClass`，它的对象头是 `Il2CppObject`，它的 GC 是 BoehmGC，它的 metadata 缓存是 `MetadataCache`。这些基础设施不是 HybridCLR 实现的，而是 IL2CPP 提供的。HybridCLR 在这些基础设施之上，补了 `InterpreterImage`（metadata 加载）、`HiTransform`（IL → HiOpcode 变换）、`Interpreter::Execute`（执行循环）三个核心模块。

这意味着 HybridCLR 天然绑定 Unity 生态。它的安装方式是作为 Unity Package 接入项目，它的构建产物嵌入 libil2cpp，它的适配工作跟随 Unity 版本变化。

但这个绑定换来的是极高的工程效率。HybridCLR 不需要自己实现 GC、不需要自己实现线程模型、不需要自己实现 reflection 基础设施——这些 IL2CPP 都有。所以 HybridCLR 能用相对少的代码量（解释器核心约 3 万行）实现完整的热更新能力，并且和 AOT 代码无缝互操作。

### LeanCLR：Unity 生态外的通用解

LeanCLR 走的是另一条路：从零实现一个完整的 CLR。不依赖 IL2CPP，不依赖 Mono，不依赖任何现有运行时。

LEAN-F1 给出了数据：73K 行 C++，295 个文件，编译后约 600KB。这个体量能覆盖 metadata 解析、双解释器、对象模型、类型系统、方法调用、internal calls、intrinsics、内存管理等所有 CLR 核心模块。

LeanCLR 瞄准的是 HybridCLR 覆盖不到的场景：H5 页面、微信小游戏、嵌入式设备、非 Unity 的 C++ 项目。这些场景没有 IL2CPP 可以复用，需要一个完全独立的 C# 运行时。

两条路线的分界线非常清晰：

`有 IL2CPP 的地方用 HybridCLR，没有 IL2CPP 的地方用 LeanCLR。`

## 架构对比总表

以下是 15 个维度的完整对比。每个维度在后续章节中都会展开分析。

| 维度 | HybridCLR | LeanCLR |
|------|-----------|---------|
| 技术路线 | IL2CPP 内部补丁 | 独立 CLR 实现 |
| 依赖 | Unity + IL2CPP | 零依赖（纯 C++17） |
| 体积 | 随 libil2cpp 整体 | ~600KB（可裁剪至 ~300KB） |
| IL transform | 1 级（CIL → HiOpcode，1000+） | 2 级（MSIL → HL-IL → LL-IL，480） |
| GC | 复用 BoehmGC | 自实现接口（当前 stub） |
| 对象头 | 复用 Il2CppObject（16B） | RtObject（16B / 8B 单线程） |
| ECMA-335 覆盖 | 受限于 IL2CPP 已实现的范围 | 声称高于 IL2CPP + HybridCLR |
| 热更新 | 核心能力 | 支持 |
| AOT 支持 | 复用 IL2CPP AOT | 计划中（IL → C++） |
| 商业功能 | DHE / FGS / 加密 / 注入 | 无（MIT 全开源） |
| 目标平台 | Unity 项目 | 任何 C++ 项目 |
| 多线程 | 复用 IL2CPP 线程模型 | Standard 版支持 |
| Reflection | 复用 IL2CPP reflection | 完整自实现（61 icalls） |
| 开源协议 | 社区版 MIT | MIT |
| 成熟度 | 生产级（大量项目验证） | 开发中 |

这张表的核心信息可以压缩成一句话：HybridCLR 在 IL2CPP 的基础设施上做增量，LeanCLR 从零搭建全部基础设施。

## Metadata 解析对比

Metadata 解析是 CLR 运行时的入口——从磁盘上的 DLL 二进制到运行时可用的类型结构，这是必须先走通的第一步。两个产品在这一步的实现策略完全不同。

### HybridCLR：InterpreterImage

HybridCLR 的 metadata 加载类是 `InterpreterImage`。但它不是从头解析 PE 文件和 metadata stream——IL2CPP 的 `MetadataCache` 已经提供了解析能力，HybridCLR 在这套基础设施之上做扩展。

HCLR-5 分析过的 8 步调用链中，metadata 加载发生在第 2-3 步：

```
Assembly.Load(byte[])
  → AppDomain::LoadAssemblyRaw
    → MetadataCache::LoadAssemblyFromBytes
      → hybridclr::metadata::Assembly::LoadFromBytes
        → InterpreterImage::Create
```

`InterpreterImage` 需要做的是：解析热更 DLL 的 metadata 表，然后把解析结果包装成 IL2CPP 能理解的结构（`Il2CppAssembly`、`Il2CppImage`、`Il2CppClass`），注册回 `MetadataCache`。

这个过程的关键约束是**格式对齐**。热更程序集在 IL2CPP 运行时中必须看起来和 AOT 程序集一样——使用相同的 `Il2CppClass` 结构、相同的 token 索引方式、相同的 metadata 访问接口。否则 AOT 代码调用热更类型时会因为数据结构不匹配而崩溃。

### LeanCLR：CliImage

LeanCLR 的 metadata 加载类是 `CliImage`，LEAN-F2 做了完整拆解。

CliImage 从 PE 文件的第一个字节开始解析：DOS header → PE signature → COFF header → CLI header → metadata root → 5 个 stream（`#~`、`#Strings`、`#Blob`、`#GUID`、`#US`）。每一步都是 LeanCLR 自己的代码，不依赖任何外部解析器。

```
leanclr_load_assembly
  → Assembly::load_by_name
    → CliImage::load_streams    // 完整的 PE + metadata 解析
      → RtModuleDef::create     // 构建运行时结构
```

解析完成后，结果存储在 LeanCLR 自己的结构中：`RtModuleDef`、`RtClass`、`RtMethodInfo`、`RtFieldInfo`。不需要和任何外部格式对齐。

### 差异的根源

两者的差异不在于 ECMA-335 解析的技术难度——metadata 二进制格式是标准定义的，任何实现都要走同样的步骤。差异在于解析完成后的数据归宿。

HybridCLR 必须把解析结果转成 IL2CPP 的 `Il2CppClass` 体系，因为后续的类型检查、虚方法分派、GC 扫描都走 IL2CPP 的代码路径。这层适配是 HybridCLR 工程复杂度的重要来源——每当 Unity 发布新版本调整了 IL2CPP 的内部结构，HybridCLR 就需要同步适配。

LeanCLR 没有这个适配负担。解析结果存在自己的结构里，后续的所有操作都在自己的代码路径中完成。代价是所有后续操作也必须自己实现。

## 解释器架构对比

解释器是两个产品的核心模块。前面各自的分析文章已经深入过实现细节，这里聚焦架构层面的设计哲学差异。

### HybridCLR：HiOpcode——一步到位的高度特化

HybridCLR 的 transform 是单级的：CIL → HiOpcode。

HCLR-27 分析过，`HiOpcodeEnum` 包含 1000+ 条指令。这个数量远超 CIL 的 200 多条，原因是 HybridCLR 做了极致的类型特化：

- 一个 CIL `add` 展开成 `BinOpVarVarVar_Add_i4`、`_Add_i8`、`_Add_f4`、`_Add_f8`
- 字段访问按大小特化成 1/2/4/8/12/16/20/24/28/32 字节的变体
- 方法调用按参数数量和类型组合预生成约 500 条变体

这个设计的目标是：在 dispatch loop 中，每条 HiOpcode 的执行路径尽可能确定，减少运行时的类型判断和分支。

代价是 transform 阶段的复杂度高。每种 CIL 指令需要根据操作数类型选择正确的 HiOpcode 变体，这需要在 transform 时做完整的类型推断。同时，1000+ 的指令空间使得 dispatch table 的大小和 switch 分支数量都很大。

### LeanCLR：HL-IL + LL-IL——分层渐进

LeanCLR 的 transform 是两级的：MSIL → HL-IL → LL-IL。

LEAN-F3 的分析数据：HL-IL 182 条指令，LL-IL 298 条指令，总计 480 条。

两级各自的职责边界清晰：

**HL-IL（High-Level IL）** 做语义归一化。把 MSIL 中编码不同但语义等价的指令合并——`ldarg.0`、`ldarg.1`、`ldarg.s`、`ldarg` 统一成 `HL_LDARG`。同时做栈模拟，把 CIL 的栈式操作转成显式的槽位引用。

**LL-IL（Low-Level IL）** 做类型特化。把 HL-IL 中的泛型操作按实际类型展开——`HL_ADD` 根据操作数类型展开成 `LL_ADD_I4`、`LL_ADD_I8` 等。同时烘焙参数索引和局部变量偏移。

这个设计的目标是：每一层 transform 的逻辑足够简单，可独立测试和调试。

代价是多了一次中间转换。方法首次执行时需要走两遍 transform，理论上比单级 transform 多一次遍历的开销。

### 设计哲学差异

两种策略反映了不同的工程优先级。

HybridCLR 优先运行时性能。单级 transform 虽然复杂，但一步到位——transform 完成后 dispatch loop 可以直接执行高度特化的指令，不需要额外的类型判断层。在 IL2CPP 的 AOT/解释器混合环境中，解释器执行的性能越接近 AOT，整体体验越好。

LeanCLR 优先可维护性和可调试性。分层 transform 把复杂度分散到两步中，每一步的输入输出都有明确的语义定义。这对一个还在开发中的运行时来说很重要——能更快定位 transform bug 是在语义归一化阶段还是在类型特化阶段。

两种策略都是合理的。它们不是对错之分，而是在不同约束条件下的最优选择。

## 类型系统对比

### HybridCLR：复用 Il2CppClass

HybridCLR 的类型系统建立在 IL2CPP 的 `Il2CppClass` 之上。

当 `InterpreterImage` 加载一个热更类型时，它会为这个类型创建一个 `Il2CppClass` 实例，填充字段偏移、VTable 槽位、接口映射等信息，然后注册到 `MetadataCache`。

复用 `Il2CppClass` 的最大优势是互操作的透明性。AOT 代码看到热更类型时，通过 `Il2CppClass` 访问类型信息——和访问 AOT 类型走的是同一条路径。虚方法分派、接口调用、类型检查（`is`/`as`）都可以直接走 IL2CPP 已有的逻辑，不需要 HybridCLR 自己实现分派机制。

限制也同样来自 `Il2CppClass`。IL2CPP 的泛型共享策略（Generic Sharing）对哪些类型可以共享实例有自己的规则——引用类型共享、值类型不共享。HybridCLR 需要遵守这些规则，并在 Full Generic Sharing（FGS）等扩展能力中处理 IL2CPP 泛型共享的边界情况。

### LeanCLR：自建 RtClass

LeanCLR 的类型描述符是 `RtClass`。LEAN-F4 和 LEAN-F5 做了详细分析。

`RtClass` 是 LeanCLR 自己定义的结构，包含类型名称、父类指针、接口列表、字段信息、方法表、VTable 等所有类型系统需要的数据。它不需要和任何外部格式对齐。

自建类型系统的自由度体现在几个方面：

**对象头裁剪。** LEAN-F4 分析过，LeanCLR Universal 版可以把对象头从 16 字节裁剪到 8 字节（去掉单线程环境下无用的 `__sync_block`）。这个优化在 HybridCLR 中不可能——`Il2CppObject` 的布局由 IL2CPP 定义，HybridCLR 无法修改。

**泛型膨胀策略。** LEAN-F5 分析了 `Method::inflate` 的泛型膨胀机制——LeanCLR 可以自行决定泛型实例的缓存策略和膨胀时机，不受 IL2CPP 泛型共享规则的约束。

**ECMA-335 覆盖边界。** LeanCLR 可以选择实现 IL2CPP 未覆盖的 ECMA-335 特性，而不需要担心和 IL2CPP 已有实现冲突。

代价是所有类型操作——虚方法分派、接口调用、类型检查、Boxing/Unboxing——都必须自己实现。HybridCLR 只需要确保热更类型的 `Il2CppClass` 填充正确，分派逻辑走 IL2CPP 的代码。LeanCLR 的分派逻辑从 VTable 查找到接口映射全部是自己的代码。

## 方法调用链对比

方法调用链是两个产品在运行时行为上差异最直观的体现。

### HybridCLR：8 步链

HCLR-5 跟完了 HybridCLR 的完整调用链，8 步从 C# 层到解释器：

```
1. Assembly.Load(byte[])           // C# 托管代码入口
2. AppDomain::LoadAssemblyRaw      // IL2CPP icall 层
3. MetadataCache::LoadAssemblyFromBytes → InterpreterImage::Create
4. Class::SetupMethods → 绑定 invoker_method 为 InterpreterInvoke
5. MethodInfo.Invoke → vm::Runtime::InvokeWithThrow
6. InterpreterInvoke → 参数转换 + GetInterpMethodInfo
7. HiTransform::Transform → 产出 InterpMethodInfo
8. Interpreter::Execute → HiOpcode 分派循环
```

这 8 步中，第 1-5 步走的是 IL2CPP 的标准路径。HybridCLR 介入的点是第 4 步（把方法的 invoker 函数指针替换成 `InterpreterInvoke`）和第 6-8 步（参数转换、transform、执行）。

链路长的原因是 HybridCLR 必须在 IL2CPP 的方法分派机制中找到合适的注入点。`invoker_method` 函数指针是 IL2CPP 为每个方法预留的调用入口——AOT 方法的 invoker 指向编译好的原生代码，HybridCLR 把热更方法的 invoker 替换成 `InterpreterInvoke`，让 IL2CPP 的调用机制自动把流量导向解释器。

### LeanCLR：3 级 fallback

LEAN-F6 分析了 LeanCLR 的方法分派，核心是三级 fallback：

```
方法调用
  → Intrinsics 查找 (18 个文件，性能关键方法)
    → miss → Internal Calls 查找 (61 个文件，BCL native 实现)
      → miss → Interpreter (HL-IL → LL-IL → execute)
```

没有 IL2CPP 的调用机制需要对接，所以链路更短更直接。宿主程序通过 C API 调用 `leanclr_load_assembly` 加载程序集，然后直接触发方法执行。方法执行时按优先级检查三个执行路径：intrinsic → icall → interpreter。

### 差异的工程含义

HybridCLR 的 8 步链路虽然长，但每一步都有明确的存在理由——它需要在 IL2CPP 已有的方法分派机制中透明地插入解释器。这种透明性使得 AOT 代码调用热更代码时不需要任何特殊处理。

LeanCLR 的 3 级 fallback 更简单，但简单的前提是它不需要和任何外部调用机制协调。所有方法——无论是 BCL 内部方法还是用户代码——都走同一条分派路径。

## 各自的优势场景

### HybridCLR 的场景：Unity 项目热更新

HybridCLR 在 Unity 热更新领域已经是事实标准。它的优势场景很明确：

**成熟度。** 经过大量生产项目验证，crash 率、兼容性、性能都有可靠的数据支撑。

**AOT 互操作。** 热更代码和 AOT 代码共享 IL2CPP 的对象模型和类型系统，跨边界调用零开销（不需要序列化、不需要代理）。

**商业功能。** DHE（Differential Hybrid Execution）提供函数级 AOT/解释混合执行，FGS（Full Generic Sharing）解决泛型共享的完整性问题，加密和代码注入提供安全和扩展能力。

**生态集成。** 作为 Unity Package 安装，和 Unity 的构建流程、CI/CD 管线无缝集成。

### LeanCLR 的场景：非 Unity 项目

LeanCLR 的优势场景是 HybridCLR 覆盖不到的地方：

**H5 / 微信小游戏。** LEAN-F9 分析过，LeanCLR 编译到 WebAssembly 后约 600KB（压缩后约 100KB），可以在浏览器环境中运行 C# 代码。IL2CPP WebGL 构建的产物体积随项目膨胀，而 LeanCLR 的运行时体积近乎恒定。

**嵌入式设备。** 零依赖、纯 C++17、可裁剪至 300KB。对于资源受限的设备，一个够小的 CLR 运行时比一个完整的 IL2CPP 管线更实际。

**非 Unity 的 C++ 项目。** 通过 C API 嵌入，不需要 Unity 引擎。游戏引擎之外的场景——工业控制、自动化脚本、模组系统——如果需要 C# 作为脚本语言，LeanCLR 提供了一个轻量级选择。

**教学和研究。** 73K 行可通读的 CLR 实现，模块边界清晰，没有历史包袱。对于想理解 CLR 内部实现的开发者，LeanCLR 可能是当前最合适的学习入口。

## 两条路线的互补性

HybridCLR 和 LeanCLR 不是竞争关系。

从产品定位看：HybridCLR 解决的是"Unity 项目怎么热更新"，LeanCLR 解决的是"没有 Unity 的地方怎么运行 C#"。两个问题没有重叠。

从技术路线看：HybridCLR 选择在 IL2CPP 内部做增量，最大化复用已有基础设施，用最小的工程量覆盖 Unity 热更新场景。LeanCLR 选择从零构建，牺牲短期工程效率，换取完全的平台独立性。

从商业策略看：HybridCLR 已经进入生产级，有社区版（MIT）和商业版（DHE/FGS/加密）的清晰分层。LeanCLR 全量开源（MIT），当前处于开发阶段，尚未形成商业模式。两个产品覆盖的客户群不同——Unity 开发团队会选择 HybridCLR，非 Unity 的 C++ 项目会评估 LeanCLR。

同一团队维护两个产品的战略合理性在于：C# 热更新的需求不局限于 Unity 生态。Unity 是当前最大的 C# 游戏引擎，但 H5 游戏、微信小游戏、自研引擎、非游戏领域的 C# 应用都有让 C# 代码动态加载执行的需求。一个团队如果只做 HybridCLR，就把自己锁定在了 Unity 生态内。LeanCLR 是对 Unity 生态外需求的覆盖。

两条路线之间也存在技术复用的可能。HybridCLR 在 IL → HiOpcode 变换、指令优化、AOT 互操作方面的经验，可以反哺 LeanCLR 的解释器优化和未来的 AOT 编译。LeanCLR 在完整 CLR 实现方面的积累——metadata 全量解析、独立类型系统、independent reflection——可以帮助 HybridCLR 理解 IL2CPP 约束的本质，在适配新版 Unity 时做出更好的工程决策。

## 收束

同一个目标，两条路，各自最优。

HybridCLR 选择了效率最高的路——在 IL2CPP 内部补解释器，复用已有的类型系统、对象模型、GC、metadata 缓存，用最小的代码量实现 Unity 生态内的完整热更新。它的约束是永远绑定 IL2CPP，它的优势是 AOT 互操作的透明性和经过生产验证的成熟度。

LeanCLR 选择了自由度最高的路——从零实现 CLR 的每个模块，73K 行 C++ 覆盖 metadata 解析到解释器到类型系统到 internal calls。它的约束是所有基础设施都要自己搭建、GC 还在 stub 阶段、成熟度有限，它的优势是零依赖、600KB 体积、可嵌入任何 C++ 项目。

两条路不需要合并。Unity 项目用 HybridCLR，非 Unity 项目用 LeanCLR。Code Philosophy 用两个产品覆盖了 C# 热更新需求的完整频谱。

## 系列位置

- 上一篇：[LEAN-F9 WebAssembly 构建与 H5 小游戏嵌入]({{< ref "leanclr-webassembly-build-h5-minigame-embedding" >}})
- LeanCLR 模块完结。回到 [.NET Runtime 生态全景系列总目录]({{< ref "ecma335-type-system-value-ref-generic-interface" >}})。
