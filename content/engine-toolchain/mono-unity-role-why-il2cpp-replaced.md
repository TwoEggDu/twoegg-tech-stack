---
title: "Mono 实现分析｜Mono 在 Unity 中的角色：为什么最终转向了 IL2CPP"
slug: "mono-unity-role-why-il2cpp-replaced"
date: "2026-04-14"
description: "回溯 Unity 与 Mono 的完整技术关系线：2005 年选择 Mono 的三个理由（跨平台、可嵌入、MIT 许可），Unity 对 Mono 的深度 fork 定制（GC 调优、iOS Full AOT 适配、Scripting Backend 抽象层），转向 IL2CPP 的五个驱动力，编辑器仍然使用 Mono 的结构性原因，以及 Unity 6 评估 CoreCLR 替代编辑器 Mono 的动机。Mono 模块完结篇。"
weight: 65
featured: false
tags:
  - Mono
  - Unity
  - IL2CPP
  - History
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "mono"
---

> Unity 选择 Mono 是因为 2005 年没有别的选择，离开 Mono 是因为 2015 年之后 Mono 的架构边界无法满足移动端和主机平台的要求。

这是 .NET Runtime 生态全景系列的 Mono 模块第 6 篇，也是 Mono 模块的完结篇。

C1 到 C5 分别拆解了 Mono 的架构总览、解释器、Mini JIT、SGen GC 和 AOT 模式。这篇从 Unity 的工程决策视角出发，把前五篇分析的技术特性放回它们实际产生影响的场景中：Unity 为什么选了 Mono、怎样改造了 Mono、又为什么转向了 IL2CPP、为什么编辑器至今仍在用 Mono、以及 CoreCLR 是否会成为下一个替代方案。

> 本文聚焦技术决策层面的分析，不展开 Mono 源码细节。源码层面的分析见 C1~C5。

## Unity + Mono 的历史

### 2005 年的技术环境

Unity 1.0 在 2005 年发布时，需要一个跨平台的脚本运行时。当时的选项：

**微软 .NET Framework。** Windows 专属，没有跨平台能力。ECMA-335 规范是公开的，但微软的实现只运行在 Windows 上。Unity 需要同时支持 macOS（当时叫 Mac OS X）和 Windows，.NET Framework 不满足需求。

**Mono。** 2001 年由 Miguel de Icaza 启动的开源 .NET 运行时，支持 Linux、macOS、Windows。MIT 许可证允许嵌入和修改。2005 年已经相对成熟，有生产环境的使用案例。

**CoreCLR。** 不存在。CoreCLR 在 2016 年才开源。

**自研脚本 VM。** 工程成本过高。要做到 C# 的语言表达力和库生态支持，自研一个 CLR 级别的 VM 不现实。

在这个技术环境下，Mono 几乎是唯一合理的选择。

### 为什么选 Mono

三个技术理由决定了这个选择。

**跨平台。** Unity 从第一天起就定位为跨平台引擎。Mono 在 Linux、macOS、Windows 上都能运行，后来逐步扩展到 iOS、Android、WebGL 等移动和 Web 平台。跨平台能力是 Mono 最核心的价值。

**可嵌入。** Mono 提供了完整的嵌入 API（`mono_jit_init`、`mono_runtime_invoke`、`mono_class_from_name` 等），允许 C/C++ 宿主程序创建 CLR 域、加载程序集、调用托管方法。Unity 的引擎核心是 C++，需要一个能嵌入到 C++ 进程中的 .NET runtime。Mono 的嵌入 API 设计成熟，有文档和社区支持。

**MIT 许可。** Mono 的 MIT 许可证允许 Unity 对源码做深度修改，并将修改后的版本闭源分发。这对于游戏引擎厂商至关重要——商业引擎不可能使用 GPL 许可的核心组件。

### 早期的技术栈

Unity 早期的 Mono 集成结构：

```
Unity Editor / Player
  └── C++ 引擎核心
        └── Mono Runtime（嵌入式部署）
              ├── JIT 编译 C# 脚本
              ├── metadata 加载 DLL
              ├── GC 管理托管对象
              └── icall 桥接 C++ ↔ C#
```

所有 C# 脚本（MonoBehaviour、EditorWindow、自定义组件）的编译和执行都由嵌入的 Mono runtime 完成。C++ 引擎通过 Mono 的嵌入 API 调用 C# 方法（如 `Awake()`、`Update()`），C# 脚本通过 icall 调用引擎的 C++ 功能（如 `Transform.position` 的 getter/setter）。

## Mono 在 Unity 里的定制

Unity 没有直接使用上游 Mono，而是维护了一个深度 fork。这个 fork 与上游 Mono 在多个维度上产生了分化。

### GC 调优

Unity 的 Mono fork 长期使用 BoehmGC 而非 SGen。C4 分析过，SGen 是精确式分代 GC，理论上优于保守式的 BoehmGC。但 Unity 的 Mono fork 迁移到 SGen 的工作量极大——SGen 需要 JIT 生成 GC Map、需要 write barrier 支持、需要整个 runtime 的安全点机制配合。Unity 在 fork 中对 BoehmGC 做了游戏场景的调优，包括增量收集支持、收集阈值调整等。

这个选择的后果是 Unity Mono 的 GC 性能始终落后于上游 Mono（SGen）和 CoreCLR（精确式分代 GC）。GC 暂停导致的帧率抖动是 Unity 开发者长期面对的问题。

### iOS Full AOT 适配

C5 分析过，Full AOT 是 iOS 平台的硬需求。Unity 的 Mono fork 对 Full AOT 做了大量适配工作：

- 泛型实例化的静态分析增强——扫描 Unity 项目中的脚本和插件，尽可能发现所有泛型实例
- AOT 编译器的 bug 修复——Mono 上游的 AOT 在某些 IL 模式下会崩溃或产出错误代码，Unity 需要在 fork 中修复
- 与 Xcode 构建系统的集成——AOT 产出的 native code 需要正确链接到 iOS 应用中

iOS Full AOT 的不稳定性是 Unity 转向 IL2CPP 的直接催化剂之一。开发者频繁遇到 AOT 编译错误、泛型覆盖遗漏导致的运行时崩溃，这些问题的根源在于 Mono AOT 管线的成熟度不足。

### 调试器集成

Unity 集成了 Mono 的软件调试器（Mono Soft Debugger），允许开发者在 Visual Studio 或 Rider 中对 C# 脚本设置断点、查看变量、单步执行。这个集成需要在 Unity 的 Mono fork 中添加调试通信协议的支持——编辑器进程和调试器 IDE 之间通过 TCP 连接交换调试命令。

### Scripting Backend 抽象层

Unity 在引擎内部引入了 Scripting Backend 抽象层，把"脚本运行时"的接口与具体实现解耦：

```
Unity Scripting API
  └── Scripting Backend 抽象层
        ├── Mono Backend — 调用 Mono 嵌入 API
        └── IL2CPP Backend — 调用 libil2cpp API
```

这个抽象层是 Unity 能在 Mono 和 IL2CPP 之间切换的工程基础。对上层的 C# 脚本来说，两个 backend 的行为应该完全一致——同样的 API、同样的语义、同样的生命周期回调。差异被限制在抽象层以下。

### .NET 版本长期固定

Unity 的 Mono fork 长期固定在较老的 .NET 版本上。Unity 4.x 使用 .NET 2.0 / C# 4，直到 Unity 2017 才升级到 .NET 4.6 / C# 6。这个滞后不是 Unity 不想升级，而是升级的工程代价太高——每次升级 C# 版本意味着同步上游 Mono 的大量变更、回归测试整个引擎的 C# 脚本兼容性、更新 BCL（Base Class Library）并确保所有平台上行为一致。

fork 越深，与上游的同步成本越高。这是一个正反馈循环：因为同步成本高所以不敢频繁同步，不频繁同步导致 fork 分化加剧，分化加剧进一步推高同步成本。

## 转向 IL2CPP 的 5 个驱动力

Unity 从 2015 年开始引入 IL2CPP，逐步替代 Mono 成为 Player 端的默认 runtime。这个决策不是某一个因素驱动的，而是五个因素叠加的结果。

### 1. 性能

Mono 的 Mini JIT 在代码质量上与 C++ 编译器有明显差距。C3 分析过，Mini 的优化集有限——没有自动向量化、没有去虚化、内联策略保守。即使启用 LLVM 后端，也只在 AOT 场景下实用。

IL2CPP 把 IL 转成 C++ 后交给平台 C++ 编译器（Clang、MSVC），可以利用这些编译器几十年积累的优化能力——自动向量化、链接时优化、过程间分析。在 CPU 密集型场景（物理模拟、AI 寻路、大规模 ECS 计算）下，IL2CPP 的执行性能显著优于 Mono JIT。

### 2. 包体大小

IL2CPP 的构建管线包含 UnityLinker（基于 Mono.Linker）裁剪步骤，可以在 IL 层面移除未使用的类型和方法。裁剪后再转换为 C++ 并编译，只有实际用到的代码才会出现在最终的 native binary 中。

Mono JIT 模式下，完整的 DLL 必须打包到应用中——因为运行时需要读取 IL 来 JIT 编译。即使某些类型和方法从未被调用，它们的 IL 和 metadata 仍然占据包体空间。

对于移动端游戏，包体大小直接影响下载转化率。每减少 1MB 包体，在某些市场可以提升可衡量的安装率。

### 3. 代码安全

Mono JIT 模式下，应用包中包含完整的 .NET DLL 文件。DLL 中的 IL 字节码可以被 dnSpy、ILSpy 等工具几乎零成本地反编译为可读的 C# 源码——类名、方法名、字段名、控制流逻辑全部暴露。

IL2CPP 把 IL 转成 native code 后，逆向工程的难度大幅提升。分析者面对的是经过 C++ 编译器优化的机器码，没有符号名、没有 IL 结构、控制流被编译器变换过。配合 global-metadata.dat 的二进制格式，完整还原原始 C# 逻辑的成本远高于反编译 DLL。

需要注意的是 IL2CPP 并非不可逆向——il2cppdumper 等工具可以从 global-metadata.dat 中提取类型和方法的字符串信息。但从"反编译出可读源码"的角度看，IL2CPP 的保护力度远超 Mono JIT。

### 4. 平台限制

C5 分析过，iOS 的 W^X policy 禁止 JIT。Mono Full AOT 可以绕过这个限制，但在实际工程中问题频出——泛型覆盖遗漏、AOT 编译器 bug、构建稳定性不足。

IL2CPP 天然就是 AOT 方案——il2cpp.exe 在构建时把所有 IL 转成 C++，C++ 编译器编译成 native code，运行时不需要任何代码生成。在 iOS 上的稳定性远优于 Mono Full AOT。

游戏主机平台也有类似的 JIT 限制。IL2CPP 的 C++ 输出可以直接交给主机平台的工具链编译，比适配 Mono AOT 到每个主机平台更可行。

### 5. .NET 版本升级困难

C# 语言版本升级带来了新的语法特性和 BCL 扩展。开发者需要 async/await、LINQ 增强、Span、Pattern Matching 等现代 C# 能力。

在 Mono 方案下，升级 C# 版本意味着同步上游 Mono 的 JIT、metadata 解析、BCL 等多个模块的变更。fork 越深，同步代价越高。

IL2CPP 方案下，前端（C# 编译器）和后端（运行时）解耦。C# 源码由 Roslyn 编译为 IL——升级 C# 版本只需要升级 Roslyn，不需要改动运行时。il2cpp.exe 处理的是标准 IL，不关心 IL 是从 C# 7 还是 C# 12 编译出来的。libil2cpp 运行时也不受 C# 语言版本影响——它只关心 native code 和 metadata。

这种解耦使得 Unity 能相对独立地跟进 C# 语言演进，不再被 Mono fork 的同步成本拖住。

## 编辑器为什么仍然用 Mono

Player 端已经全面转向 IL2CPP，但 Unity 编辑器至今仍然运行在 Mono 上。这不是遗留问题，而是技术上的结构性依赖。

### 需要 JIT

编辑器的核心工作循环是：修改 C# 脚本 → 编译 → 域重载 → 立即看到效果。这个循环要求 runtime 能在几百毫秒内加载新编译的 DLL 并执行其中的代码。

JIT 模式下，加载新 DLL 后 Mono 可以立即 JIT 编译被调用的方法。整个"修改→生效"的循环可以在 1~2 秒内完成。

如果编辑器也使用 IL2CPP（AOT），每次脚本修改后都需要走一遍完整的 AOT 编译流程——il2cpp.exe 转换 + C++ 编译 + 链接。这个流程在中型项目上可能需要数十秒到几分钟，无法满足编辑器的快速迭代需求。

### 需要 Reflection.Emit

Unity 编辑器和编辑器扩展工具大量使用 Reflection.Emit 系列 API。Reflection.Emit 允许在运行时动态生成 IL 代码，用于：

- 序列化系统动态生成属性访问器
- Editor GUI 框架动态构建绘制代码
- 某些编辑器插件使用动态代码生成优化性能

Reflection.Emit 的本质是运行时代码生成，AOT 方案天然不支持。IL2CPP 不支持 Reflection.Emit——D1 在 ECMA-335 覆盖度一节中已经说明。Mono JIT 模式完整支持 Reflection.Emit。

### Domain Reload 依赖 AppDomain

Unity 编辑器在脚本编译后执行 Domain Reload——卸载当前的 AppDomain，创建新的 AppDomain，重新加载编译后的 DLL。这个机制确保编辑器中运行的永远是最新版本的脚本代码。

AppDomain 是 Mono 的运行时概念。IL2CPP 不支持多 AppDomain——D1 已经说明 `AppDomain.CreateDomain()` 在 IL2CPP 上抛出 `NotSupportedException`。

虽然 Unity 2019.3+ 引入了可选的"Enter Play Mode Settings"来跳过 Domain Reload（提升进入 Play Mode 的速度），但 Domain Reload 仍然是编辑器正常工作流的默认行为。

## Unity 6 的 CoreCLR 计划

Unity 正在评估用 CoreCLR 替代 Mono 作为编辑器的运行时。这个评估的动机来自 Mono 在编辑器场景下暴露出的多个短板。

### 上游维护

微软在 2016 年收购 Xamarin 后，Mono 被合并到 `dotnet/runtime` 仓库中。但在 .NET 统一运行时的战略下，CoreCLR 是主线实现，Mono 承担的是特定场景（WebAssembly、移动端 AOT）的补充角色。这意味着 Mono 在服务端和桌面场景下的优化投入远不如 CoreCLR。

Unity 的编辑器运行在桌面平台（Windows、macOS、Linux）。在这个场景下，CoreCLR 是微软重点投入的 runtime，有更活跃的开发和更快的 bug 修复周期。继续维护 Mono 作为编辑器 runtime，意味着 Unity 需要自己承担越来越多的 Mono 桌面场景维护工作。

### Tiered Compilation

CoreCLR 的分层编译（Tiered Compilation）是一个 Mono 没有的重要特性。分层编译允许方法在首次调用时快速编译一个低优化版本（Tier0），在方法被频繁调用后再编译一个高优化版本（Tier1）。Tier1 可以利用运行时收集的 PGO（Profile-Guided Optimization）数据做针对性优化。

对于编辑器场景，分层编译的价值在于：

- **启动更快。** Tier0 的编译速度极快，编辑器启动时大量方法可以用低优化版本快速执行
- **热路径更快。** 编辑器中的高频操作（场景渲染、Inspector 刷新、Asset 导入）经过 Tier1 优化后性能更好
- **不需要手动调优。** 运行时自动识别热路径并定向优化，不需要开发者干预

Mono 的 Mini JIT 每个方法只编译一次，没有运行时反馈优化的机制。C3 分析过，Mini 选图着色寄存器分配的一个原因就是"每个方法只编译一次，必须一次产出足够好的代码"。CoreCLR 的分层机制让 JIT 可以在编译速度和代码质量之间动态平衡。

### 更好的 GC

CoreCLR 的 GC 在多个维度上优于 Unity Mono fork 中使用的 GC：

| 维度 | Unity Mono（BoehmGC） | CoreCLR GC |
|------|---------------------|------------|
| **精确性** | 保守式 | 精确式 |
| **分代** | 无分代 | 三代（Gen0/1/2） |
| **压缩** | 不支持 | 支持（Gen0/1/2 压缩，消除碎片） |
| **并发** | 增量式 | Background GC（并发标记+清扫） |
| **多堆** | 不支持 | Server GC 多堆并行 |
| **固定对象** | 无影响（不搬移） | POH 集中管理 |

对于编辑器——一个长时间运行、内存分配密集、GC 暂停敏感的应用——CoreCLR 的精确式分代 GC 可以带来显著的体验改善。精确式 GC 消除了保守式 GC 的误保留问题，分代收集减少了每次 GC 的扫描范围，Background GC 减少了 STW 暂停时间。

### 工程挑战

CoreCLR 替代 Mono 作为编辑器 runtime 并非简单的"换一个 DLL"。核心挑战包括：

**嵌入 API 不同。** Unity 二十年来围绕 Mono 的嵌入 API 构建了大量基础设施。CoreCLR 有自己的 hosting API（`coreclr_initialize`、`coreclr_execute_assembly` 等），接口设计和语义与 Mono 的嵌入 API 不同。Scripting Backend 抽象层需要适配新的 hosting 接口。

**Domain Reload 机制不同。** CoreCLR 不支持传统的 AppDomain 卸载。.NET Core 之后的等效机制是 `AssemblyLoadContext`，允许加载和卸载程序集。Unity 需要把基于 AppDomain 的 Domain Reload 逻辑迁移到 AssemblyLoadContext 上。

**BCL 差异。** CoreCLR 使用的 BCL（System.Private.CoreLib）与 Mono 的 BCL 在内部实现上有差异。某些 Unity 依赖的内部 API 或行为可能需要适配。

**编辑器插件兼容性。** 社区中大量编辑器插件可能依赖了 Mono 特有的行为或 API。Runtime 切换可能导致部分插件不兼容，需要评估影响范围并提供迁移路径。

这是一个多年期的工程项目，不是一次性的替换。Unity 的逐步推进策略——先评估、再实验、最终分阶段切换——反映了这个工程规模。

## Mono 模块完结回顾

六篇文章覆盖了 Mono 的完整技术栈：

| 编号 | 主题 | 核心结论 |
|------|------|----------|
| C1 | 架构总览 | Mono 的六大模块和四种执行模式。唯一同时提供 JIT + AOT + Interpreter + Mixed 的 CLR |
| C2 | 解释器 | mint/interp 的实现。WebAssembly 和 AOT fallback 的执行引擎 |
| C3 | Mini JIT | IL → SSA → native 的编译管线。图着色寄存器分配、与 RyuJIT 的定位差异 |
| C4 | SGen GC | 精确式分代 GC。nursery copying + major mark-sweep 的两代模型 |
| C5 | AOT | Normal AOT 与 Full AOT。LLVM 后端的优化收益。与 IL2CPP AOT 的结构性差异 |
| C6 | Unity 中的角色（本篇） | Unity 选择 Mono 的历史、离开 Mono 的原因、编辑器仍用 Mono 的结构性依赖 |

从这六篇的分析中可以提炼出一条主线：Mono 的架构灵活性（四种执行模式、跨平台嵌入能力）使它成为 Unity 早期唯一可行的选择，但这种灵活性的代价是每条路径都不够深——JIT 不如 CoreCLR、AOT 不如 IL2CPP、GC 从 Boehm 升级到 SGen 的工程阻力过大。Unity 最终的技术路线是把 Mono 的四条路径拆开，每条路径用更合适的技术替代：Player 端的 AOT 用 IL2CPP，编辑器的 JIT 未来可能换成 CoreCLR，WebAssembly 场景 Mono 仍然活跃（.NET 的 Blazor WebAssembly）。

## 收束

Unity 与 Mono 的技术关系线跨越了二十年。

2005 年 Unity 选择 Mono，是因为在跨平台 .NET 运行时这个需求上，Mono 是唯一的答案。2015 年 Unity 引入 IL2CPP，不是因为 Mono 变差了，而是移动端和主机平台的技术要求超出了 Mono 的架构边界——iOS 需要稳定的 AOT、移动端需要更好的性能和更小的包体、商业发行需要代码保护、C# 版本升级需要前后端解耦。2026 年 Unity 评估 CoreCLR 替代编辑器 Mono，驱动力同样是技术能力边界——分层编译、精确式 GC、上游维护活跃度。

每一次技术选型的变迁，背后都是"当前方案的架构边界"与"新场景的技术要求"之间的张力。Mono 不是被淘汰了，而是每个场景都找到了更合适的实现。理解这条演进线，比评判"Mono 好不好"更有价值。

## 系列位置

- 上一篇：[MONO-C5 AOT：Full AOT 与 LLVM 后端]({{< ref "mono-aot-full-aot-llvm-backend" >}})
- 下一篇（跨模块）：[IL2CPP-D6 GC 集成：BoehmGC 的接入层、write barrier 与 finalization]({{< ref "il2cpp-gc-integration-boehm-wbarrier-finalization" >}})
