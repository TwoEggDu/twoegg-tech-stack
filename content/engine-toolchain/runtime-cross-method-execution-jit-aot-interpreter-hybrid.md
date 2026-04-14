---
title: "横切对比｜方法执行：JIT vs AOT vs Interpreter vs 混合执行"
slug: "runtime-cross-method-execution-jit-aot-interpreter-hybrid"
date: "2026-04-14"
description: "同一个 CIL 方法体，在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出五条完全不同的执行路线：JIT 编译、AOT 预编译、解释执行、混合执行。从 IR 层级、首次调用延迟、稳态性能到热更新能力，逐维度横切对比五种执行策略的设计 trade-off。"
weight: 82
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Execution"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> JIT 能让代码跑得像 native 一样快，但 iOS 不让你用；AOT 没有首次编译延迟，但无法热更新；解释器什么平台都能跑，但稳态性能差 10-100 倍。没有"最好的"执行策略——只有在不同约束下各自最优的策略。

这是 .NET Runtime 生态全景系列的横切对比篇第 3 篇。

CROSS-G2 对比了类型系统——五个 runtime 怎么把同一条 TypeDef 变成运行时结构。但类型系统只解决了"类型长什么样"，没有回答"方法怎么跑起来"。

一个 CIL 方法体在 DLL 里就是一段字节码。五个 runtime 拿到同一段字节码后，走出了五条截然不同的路线，最终让 CPU 执行的东西完全不同。这篇就是要把这五条路线并排摊开。

## 同一个 CIL 方法体，五种执行路线

考虑一个简单的 C# 方法：

```csharp
public int Add(int a, int b)
{
    return a + b;
}
```

编译后在 DLL 里的 CIL 字节码是：

```
IL_0000: ldarg.1      // 把参数 a 压栈
IL_0001: ldarg.2      // 把参数 b 压栈
IL_0002: add          // 弹出两个值，相加，结果压栈
IL_0003: ret          // 弹出栈顶作为返回值
```

4 条指令，4 个字节。所有 runtime 看到的都是同一段字节码。但从这里开始，路线分叉。

## CoreCLR JIT 路线：RyuJIT

CoreCLR 的默认执行策略是 JIT（Just-In-Time）编译。方法在第一次被调用时，由 RyuJIT 编译器把 CIL 编译成目标平台的 native code。

### 编译管线

```
CIL 字节码
  → Importer（CIL → RyuJIT 高层 IR）
    → 优化 passes（内联、常量折叠、死代码消除、循环优化...）
      → 寄存器分配
        → 代码生成（IR → x64 / ARM64 native code）
          → 写入代码堆
```

RyuJIT 的 IR 是基于 SSA（Static Single Assignment）的树形结构。`add` 这条 CIL 指令在 RyuJIT IR 中变成一个 `GT_ADD` 节点，两个子节点分别引用参数 a 和 b。

对于 `Add(int, int)` 这个简单方法，RyuJIT 最终生成的 x64 native code 大致是：

```asm
; RyuJIT 生成的 x64 代码（简化）
mov   eax, ecx      ; a 已经在 ecx 寄存器里
add   eax, edx      ; b 在 edx 里，相加
ret                  ; 返回 eax
```

3 条 native 指令，直接由 CPU 执行。没有间接层，没有 dispatch loop，性能和手写 C++ 一致。

### Tiered Compilation

CoreCLR 从 .NET Core 3.0 开始引入了分层编译（Tiered Compilation），把 JIT 编译进一步细分：

**Tier 0（快速编译）。** 方法首次被调用时，RyuJIT 用最少的优化快速生成 native code。目标是减少首次调用延迟。生成的代码质量不高，但能跑起来。

**Tier 1（优化编译）。** 当某个方法被调用足够多次后（达到调用计数阈值），后台线程用完整优化重新编译该方法，然后替换 Tier 0 的代码。

**PGO（Profile-Guided Optimization）。** 可选能力。Tier 0 代码运行时收集 profile 数据（哪些分支更热、哪些类型更常见），Tier 1 编译时利用这些数据做更精准的优化决策。

分层编译的效果是：应用启动更快（Tier 0 编译快），稳态性能更好（Tier 1 充分优化），代价是后台编译线程的 CPU 开销和代码替换的内存开销。

### 热路径上的执行流程

```
首次调用 Add(1, 2)
  → 触发 JIT 编译（Tier 0）
    → RyuJIT 编译 CIL → native code
      → 写入 code heap
        → 更新 MethodTable vtable 槽位为 native code 地址
          → 执行 native code

后续调用 Add(3, 4)
  → 直接跳到 native code 地址
    → 执行
```

首次调用有 JIT 编译延迟（微秒到毫秒级，取决于方法复杂度）。后续调用零开销——和直接调用 native 函数完全一致。

## Mono 双路线：Mini JIT + Interpreter

Mono 提供两种执行路线，根据运行环境选择其中一种或两种组合。

### Mini JIT

Mono 的 JIT 编译器叫 Mini。编译管线和 CoreCLR 的 RyuJIT 类似，但中间表示和优化策略不同：

```
CIL 字节码
  → 基本块划分 + 栈模拟
    → Mono IR（SSA 形式）
      → 优化 passes（内联、常量传播、死代码消除、局部优化）
        → 指令选择（IR → 目标平台指令）
          → 寄存器分配（线性扫描）
            → native code 发射
```

Mini JIT 的设计目标是编译速度优先。它的优化深度不如 RyuJIT——没有 PGO，循环优化较弱，但编译速度通常更快。生成代码质量在大多数场景下足够好，热点方法可能比 RyuJIT Tier 1 慢 10-30%。

### Interpreter（mint）

Mono 的解释器代号 mint。它直接解释 CIL 指令，不做 JIT 编译。

```
CIL 字节码
  → 解释器 transform（CIL → 解释器内部 IR）
    → Interpreter::interp_exec_method
      → switch dispatch loop 逐条执行
```

mint 在 CIL 到内部 IR 的 transform 阶段做了一些优化：栈式操作转为寄存器式引用（减少栈操作）、短指令和长指令统一编码等。但不做类型特化——`add` 在运行时仍然需要判断操作数类型。

解释器的存在主要是为了：

- **iOS / 游戏主机等禁止 JIT 的平台**。这些平台的操作系统不允许在运行时生成可执行代码（W^X 策略），JIT 无法使用
- **AOT 的 fallback**。Mono AOT 不能覆盖所有情况（比如动态泛型实例化），未被 AOT 的方法回退到解释器

### Mono AOT

Mono 也支持 AOT 编译。构建时用 `mono --aot` 把 CIL 预编译成 native code，运行时直接加载执行。这条路线和 IL2CPP 的 AOT 类似，但实现机制不同：

- Mono AOT 输出的是 native 共享库（.so / .dylib），运行时由 Mono runtime 加载
- IL2CPP 输出的是 C++ 源代码，再由平台 C++ 编译器编译
- Mono AOT 可以和解释器组合使用（AOT + Interpreter），IL2CPP 原生不支持解释执行

## IL2CPP AOT 路线：构建时全量编译

IL2CPP 的执行策略是纯 AOT——所有方法在构建时就被编译成 native code，运行时不存在任何形式的 JIT 或解释执行。

### 构建管线

```
C# 源代码
  → Roslyn 编译器 → CIL DLL
    → il2cpp.exe → C++ 源代码（每个方法一个 C++ 函数）
      → 平台 C++ 编译器（clang / MSVC / GCC）
        → native 代码（GameAssembly.dll / libil2cpp.so）
```

对于 `Add(int, int)` 方法，il2cpp.exe 生成的 C++ 代码类似：

```cpp
int32_t Player_Add_m12345(Player_o* __this, int32_t a, int32_t b,
                           const MethodInfo* method)
{
    return a + b;
}
```

每个 C# 方法变成一个 C++ 函数。函数签名包含 `this` 指针和一个 `MethodInfo*` 参数（用于泛型上下文传递和调试）。

C++ 编译器再把这个函数编译成 native code。最终的 native 代码质量取决于 C++ 编译器的优化能力——clang/MSVC 的优化器在大多数场景下和 RyuJIT Tier 1 不相上下。

### 运行时无 JIT

运行时调用方法时，直接跳到预编译好的 native 函数。IL2CPP 类型描述符 `Il2CppClass` 的 vtable 中存的就是这些 native 函数的指针：

```
调用 player.Add(1, 2)
  → 从对象头读 Il2CppClass*
    → 从 vtable（或直接调用）拿到 Player_Add_m12345 的函数指针
      → 直接调用 native 函数
```

没有编译延迟，没有解释开销。首次调用和后续调用的性能完全一致。

### 代价

全量 AOT 的核心代价是：

**无法执行构建时不存在的代码。** 没有 JIT、没有解释器，就无法执行运行时加载的新 DLL。这直接导致了 IL2CPP 不支持热更新，也是 HybridCLR 存在的根本原因。

**泛型膨胀。** 每个用到的泛型实例化（比如 `List<int>`、`List<string>`、`List<Player>`）都要生成独立的 C++ 代码。泛型使用越多，生成的代码量越大，包体也越大。没有使用过的泛型组合不会生成，运行时如果遇到了就会报错（AOT 泛型缺失问题）。

**构建时间长。** C++ 编译器编译大量生成的 C++ 代码需要相当长的时间。大型项目的 IL2CPP 构建可以超过 30 分钟。

## HybridCLR 解释路线：CIL → HiOpcode → switch dispatch

HybridCLR 在 IL2CPP 的纯 AOT 世界里补了一个解释器。AOT 方法继续以 native code 执行，热更新加载的方法由解释器执行。

### Transform 管线

```
CIL 字节码（热更 DLL 中的方法体）
  → HiTransform::Transform
    → 栈机 → 寄存器转换
      → 类型特化展开
        → 常量折叠、冗余消除等优化
          → HiOpcode 指令序列（1000+ opcodes）
            → InterpMethodInfo 缓存
              → switch dispatch 执行
```

HybridCLR 的 transform 是单级的：CIL 直接变成 HiOpcode。HiOpcode 的设计策略是**极度特化**——把 CIL 的类型多态指令按操作数类型全部展开。

一条 CIL `add` 在 HiOpcode 中变成了四条独立的 opcode：

- `BinOpVarVarVar_Add_i4`（int32 加法）
- `BinOpVarVarVar_Add_i8`（int64 加法）
- `BinOpVarVarVar_Add_f4`（float32 加法）
- `BinOpVarVarVar_Add_f8`（float64 加法）

这样做的好处是执行时不需要再判断操作数类型，switch dispatch 直接跳到对应的执行代码。

### 嵌入 IL2CPP 内部的执行模型

HybridCLR 的解释器不是一个独立的运行时。它运行在 IL2CPP 内部，共享 IL2CPP 的类型系统、GC、线程模型。

从 IL2CPP 的视角看，热更方法的 vtable 槽位指向解释器入口函数（`Interpreter::Execute`）。当 IL2CPP 按正常的 vtable 分派调用一个热更方法时，实际上是在调用解释器。解释器内部再根据 `InterpMethodInfo` 找到 transform 后的 HiOpcode 序列，开始逐条执行。

这种设计使得 AOT 方法和解释器方法可以无缝互调。AOT 代码调用热更方法时走 vtable 分派，自然地进入解释器；热更代码调用 AOT 方法时，解释器通过 IL2CPP 的 `MethodInfo->methodPointer` 直接调用 native 函数。

### 性能特征

- **首次调用**：需要完成 transform，即把 CIL 方法体转换为 HiOpcode 序列。transform 时间取决于方法复杂度，通常在亚毫秒级
- **稳态执行**：switch dispatch 解释执行。相对 native code 有 10-100 倍的性能差距。简单算术差距小，复杂控制流差距大
- **transform 缓存**：每个方法只 transform 一次，后续调用直接复用 HiOpcode 序列

## LeanCLR 双解释器路线：MSIL → HL-IL → LL-IL → switch dispatch

LeanCLR 和 HybridCLR 一样是解释执行，但管线设计完全不同。最大的区别是**两级 transform**。

### Transform 管线

```
MSIL (256 opcodes)
  → BasicBlockSplitter（CFG 构建）
    → HLTransformer
      → 语义归一化：ldarg.0/ldarg.1/ldarg.s/ldarg → HL_LDARG
      → 栈机 → 寄存器转换
      → 基本块边界 phi 节点处理
        → HL-IL (182 opcodes)
          → LLTransformer
            → 类型特化：HL_ADD → LL_ADD_I4 / LL_ADD_I8 / LL_ADD_R4 ...
            → 参数索引烘焙
            → 局部变量偏移烘焙
              → LL-IL (298 opcodes)
                → Interpreter::execute
                  → switch dispatch 逐条执行
```

### 两级 transform 的设计意图

HybridCLR 的单级 transform 一步到位，从 CIL 直接生成 1000+ 个高度特化的 HiOpcode。这样做的好处是执行路径最短——每条 HiOpcode 已经是完全确定的操作，dispatch loop 做的工作最少。

LeanCLR 分两步走：

**第一步（HL-IL）做语义归一化。** MSIL 有很多语义等价但编码不同的指令（比如 `ldarg.0` 和 `ldarg 0` 做的是同一件事，只是编码长度不同）。HL Transformer 把这些变体合并成一套统一的 182 条指令。同时完成栈式操作到寄存器式引用的转换。

**第二步（LL-IL）做类型特化。** 在 HL-IL 的基础上，按实际操作数类型展开成具体指令。`HL_ADD` 变成 `LL_ADD_I4`、`LL_ADD_I8`、`LL_ADD_R4` 等。同时把参数索引和局部变量偏移烘焙成指令操作数中的具体数值。

分两步的代价是多了一次中间转换。收益是每一层的逻辑更简单：HL Transformer 不需要关心类型特化，LL Transformer 不需要关心指令归一化。对于一个从零实现的 CLR 来说，降低每一层的复杂度比减少一次中间转换更重要。

### 与 HybridCLR 的 opcode 空间对比

| 维度 | HybridCLR | LeanCLR |
|------|-----------|---------|
| transform 级数 | 1 级 | 2 级 |
| 最终 opcode 数量 | 1000+ | 298 |
| 特化策略 | 极度特化（含调用参数组合） | 适度特化（按基本类型） |
| 单条指令复杂度 | 低（每条做一件事） | 中（可能包含一次类型分支） |
| transform 实现复杂度 | 高（一步完成所有工作） | 低（每步做一部分） |

HybridCLR 的 1000+ opcode 中有大约 500 条是调用指令的变体（按参数数量和类型组合预生成）。LeanCLR 不做这种调用特化，统一走通用的方法调用路径。

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 执行方式 | JIT | JIT + Interp | AOT | Interpreter | Interpreter |
| 首次调用延迟 | JIT 编译时间 | JIT 编译时间 | 无（已编译） | transform 时间 | 双 transform |
| 稳态性能 | native 级 | native 级 | native 级 | 10-100x 慢 | 待测 |
| 跨平台一致性 | 依赖 JIT 后端 | 依赖 JIT 后端 | 一致（AOT） | 一致 | 一致 |
| 热更新能力 | AssemblyLoadContext | 无 | 无 | 核心能力 | 支持 |
| 体积影响 | JIT 编译器大 | JIT + runtime | native 代码大 | 增加解释器 | ~600KB |
| IR 层级 | MSIL → RyuJIT IR → native | MSIL → SSA → native | MSIL → C++ | CIL → HiOpcode | MSIL → HL-IL → LL-IL |

逐行展开几个关键维度的具体含义。

### 执行方式

CoreCLR 和 Mono Mini 是 JIT——运行时把 CIL 编译成 native code。IL2CPP 是 AOT——构建时把 CIL 编译成 native code。HybridCLR 和 LeanCLR 是解释器——运行时逐条执行 transform 后的中间表示。

Mono 比较特殊，同时有 JIT 和解释器两条路线。在桌面/服务端环境下默认用 JIT；在 iOS 等禁止 JIT 的平台上用 AOT + Interpreter 组合。

### 首次调用延迟

JIT 路线（CoreCLR、Mono Mini）的首次调用需要等编译完成。简单方法编译只需几十微秒，复杂方法（大量分支、深层嵌套）可能需要几毫秒。

AOT 路线（IL2CPP）没有首次调用延迟。方法在构建时已经编译好，运行时直接执行。

解释器路线的首次调用延迟来自 transform。HybridCLR 做一级 transform，LeanCLR 做两级。但 transform 比 JIT 编译快得多——它不需要做寄存器分配和 native code 生成，只是指令集转换。通常在亚毫秒级完成。

实际项目中，首次调用延迟的影响集中在应用启动期和首次进入新场景时。大量方法集中在短时间内被首次调用，延迟会累积。CoreCLR 的 Tiered Compilation 和 HybridCLR 的 PreJit 都是针对这个问题的缓解策略。

### 稳态性能

JIT 和 AOT 路线的稳态性能是 native 级的——方法执行的是 CPU 直接运行的 native code，性能取决于编译器的优化能力。CoreCLR RyuJIT Tier 1 和 IL2CPP 经过 clang 优化的代码在大多数场景下性能相当。

解释器路线的稳态性能显著低于 native 执行。HybridCLR 的 switch dispatch 解释执行，根据操作类型的不同，比 native code 慢 10-100 倍。简单算术和内存访问差距在 10-30 倍，复杂控制流（深层嵌套调用、频繁分支）差距可以达到 50-100 倍。

LeanCLR 的稳态性能目前缺少系统性的 benchmark 数据（项目仍在早期阶段）。理论上，298 条 LL-IL opcode 比 HybridCLR 的 1000+ HiOpcode 特化程度低，单条指令的执行可能需要更多的类型判断，但两级 transform 减少了运行时的类型检查负担。实际差距需要待后续测试。

### 热更新能力

JIT 路线天然支持运行时加载新代码——把新 DLL 加载进来，JIT 编译，就能执行。CoreCLR 通过 `AssemblyLoadContext` 支持程序集加载和（受限的）卸载。

AOT 路线天然不支持热更新——构建时确定了所有代码，运行时无法引入新代码。IL2CPP 就是这种情况。

解释器路线天然支持热更新——解释器的输入是 CIL 字节码，运行时加载新 DLL 就能解释执行。HybridCLR 和 LeanCLR 都以此为核心能力。

Mono 在热更新方面能力有限。虽然有 JIT 可以编译新代码，但缺少完善的程序集卸载机制，实际项目中很少用于生产环境的热更新。

### 体积影响

CoreCLR 自带 RyuJIT 编译器，运行时体积较大（桌面环境约 50MB+）。Mono 的 runtime 较小但仍包含 JIT 编译器（约 10-20MB）。

IL2CPP 的运行时（libil2cpp）本身不大，但生成的 native code（GameAssembly）可以很大。每个 C# 方法对应一个 C++ 函数，泛型膨胀进一步放大代码量。大型 Unity 项目的 GameAssembly 超过 100MB 并不罕见。

HybridCLR 在 IL2CPP 的基础上增加了解释器代码，增量体积约 1-2MB。LeanCLR 的整个运行时（含解释器、metadata 解析、类型系统）约 600KB（Universal 版），是五个 runtime 中最小的。

## 混合执行模式分析

纯 JIT、纯 AOT、纯解释器各有局限。实际产品中，多种执行策略组合使用是常态。

### Mono：AOT + Interpreter

在 iOS 和游戏主机等平台上，Mono 的默认模式是 AOT + Interpreter：

- 构建时用 AOT 把所有能编译的方法预编译成 native code
- 运行时遇到 AOT 无法覆盖的情况（如某些动态泛型实例化），回退到解释器

这种组合解决了"AOT 不能覆盖所有情况"的问题。代价是运行时需要同时包含 AOT runtime 和解释器，体积增加。

### HybridCLR：AOT + Interpreter（在 IL2CPP 里）

HybridCLR 的混合执行和 Mono 的 AOT + Interpreter 在概念上类似，但实现层次不同：

- AOT 部分是 IL2CPP 提供的——构建时确定的 C# 代码走 IL2CPP 的全量 AOT
- Interpreter 部分是 HybridCLR 补的——热更新加载的 DLL 走解释器

关键区别：Mono 的 AOT + Interpreter 是同一份代码的两种执行路线（AOT 优先，解释器 fallback）。HybridCLR 的 AOT + Interpreter 是两份不同代码的两种执行路线（AOT 代码在构建时确定，热更代码在运行时加载）。

这意味着 HybridCLR 的性能分布是双峰的：AOT 方法跑 native 速度，热更方法跑解释器速度。工程上需要把性能敏感的逻辑放在 AOT 侧，把需要灵活迭代的逻辑放在热更侧。

DHE（Differential Hybrid Execution）进一步模糊了这个边界：它允许对 AOT 程序集做函数级的差分更新，未修改的函数继续跑 native code，修改过的函数走解释器。这使得热更新的性能代价只出现在被修改的函数上。

### LeanCLR：计划中的 AOT + Interpreter

LeanCLR 当前是纯解释器执行。但其架构预留了 AOT 的接口——`RtMethodInfo` 中有 `native_method_pointer` 字段，如果不为空就直接调用 native 函数，绕过解释器。

计划中的 AOT 模式是：把高频方法预编译成 native code（通过外部编译器），运行时加载后注册到对应的 `RtMethodInfo`。低频方法继续走解释器。

这种选择性 AOT 和 Mono 的全量 AOT + 解释器 fallback 路线不同，更接近 CoreCLR 的 Tiered Compilation 思路——识别热点方法，只对热点编译。但 LeanCLR 的热点识别是离线的（开发者标记），CoreCLR 的是运行时自动的（调用计数）。

### CoreCLR：Tiered Compilation 的混合本质

CoreCLR 的 Tiered Compilation 其实也是一种混合执行——同一个方法在不同时间以不同质量的 native code 执行：

- 首次调用：Tier 0 快速编译的 native code（低优化）
- 达到阈值后：后台重编译为 Tier 1 的 native code（高优化）
- 可选 PGO：基于运行时 profile 做进一步特化

这不是 JIT + Interpreter 的混合，而是 JIT(fast) + JIT(optimized) 的混合。但本质上解决的问题是一样的：在首次调用延迟和稳态性能之间找平衡。

## 为什么没有"最好的"执行策略

五种执行路线各自在不同的约束空间里是最优解。

**如果约束是稳态性能最大化**——CoreCLR Tiered Compilation 是当前最优。JIT 编译 + PGO 可以产出比静态 AOT 更好的代码（因为有运行时 profile 信息），同时 tiered 策略兼顾了启动速度。

**如果约束是不允许 JIT**——iOS 和部分游戏主机平台的安全策略禁止在运行时生成可执行代码。这个约束直接排除了所有 JIT 路线。IL2CPP AOT 是这些平台的标配。Mono AOT + Interpreter 也适用。

**如果约束是支持热更新**——纯 AOT（IL2CPP）天然不支持。需要解释器来执行运行时加载的代码。HybridCLR 在 IL2CPP 上补解释器是当前 Unity 生态的主流方案。LeanCLR 提供了另一条路线：直接替换 IL2CPP。

**如果约束是最小体积**——JIT 编译器本身就有几十 MB，AOT 生成的 native code 也占体积。纯解释器路线的体积最小。LeanCLR 的 600KB 包含了完整的 metadata 解析、类型系统、双解释器，适合 H5 和小游戏等体积敏感场景。

**如果约束是跨平台一致性**——JIT 生成的代码取决于 JIT 编译器的后端实现（x64 和 ARM64 的代码可能行为不完全一致，特别是浮点精度）。AOT 和解释器的行为一致性更容易保证。游戏项目通常对跨平台一致性有严格要求，这是解释器和 AOT 的一个优势。

每种策略都不是在真空中选择的。真正的决策框架是：先明确约束（目标平台、性能要求、热更新需求、体积预算），再从约束出发选择策略。任何脱离约束的"哪个更好"都是伪命题。

## 收束

同一段 CIL 字节码，五个 runtime 给出了五条完全不同的执行路线。这些差异的根源不在于技术偏好，而在于各自面对的约束集合不同。

CoreCLR 面对的是服务端 .NET 生态，追求极致稳态性能，所以做 Tiered JIT + PGO。Mono 面对的是多平台嵌入式场景，需要在 JIT 和非 JIT 平台之间灵活切换，所以保留了 JIT + Interpreter 双路线。IL2CPP 面对的是 Unity 移动端的 AOT 需求，构建时一步到位。HybridCLR 在 IL2CPP 的 AOT 世界里补回了运行时执行能力。LeanCLR 从零开始，用最小的实现覆盖最核心的能力。

五条路线并排来看，最清晰的结论是：执行策略不是一个独立的技术选择，而是 runtime 整体设计约束的直接推论。类型系统、内存模型、目标平台、体积预算——这些约束一旦确定，执行策略的选择空间就大幅收窄了。

## 系列位置

这是横切对比篇第 3 篇（CROSS-G3），也是 Phase 1 横切对比的收尾。

上一篇 CROSS-G2 对比了类型系统实现——五个 runtime 怎么把 TypeDef 变成运行时结构。这篇在类型系统之上走了一步：有了类型、有了方法，怎么让代码跑起来。

Phase 1 的三篇横切对比（CROSS-G1 metadata 解析、G2 类型系统、G3 方法执行）覆盖了 .NET runtime 最核心的三个环节。后续 Phase 2 的 CROSS-G4 和 G5 将继续对比 GC 实现和泛型策略——它们同样是每个 runtime 必须面对、但实现路径各异的核心问题。
