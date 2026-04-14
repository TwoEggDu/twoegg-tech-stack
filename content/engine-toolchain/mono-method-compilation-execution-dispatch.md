---
title: "Mono 实现分析｜方法编译与执行：从 MonoMethod 到 native code 或解释器"
slug: "mono-method-compilation-execution-dispatch"
date: "2026-04-14"
description: "拆解 Mono 中一个方法从 MonoMethod 描述符到实际执行的完整路径：mono_runtime_invoke 入口的三路分派（JIT / Interpreter / Trampoline），trampoline 机制驱动的懒编译、mono_jit_compile_method 的编译触发与缓存，以及 Full AOT 模式下解释器 fallback 的触发条件。对比 CoreCLR PreStub、IL2CPP 静态方法表、HybridCLR 混合分派的方法执行策略差异。"
weight: 64
featured: false
tags:
  - Mono
  - CLR
  - MethodCompilation
  - JIT
  - Interpreter
series: "dotnet-runtime-ecosystem"
series_id: "mono"
---

> 一个方法在 Mono 中的第一次调用不会直接执行——它会先撞上一个 trampoline，由 trampoline 决定这个方法应该走 JIT 编译还是解释器执行。这个"先占位、后编译"的机制是 Mono 方法执行链路的核心设计。

这是 .NET Runtime 生态全景系列的 Mono 模块补充篇，插在 C4（SGen GC）和 C5（AOT）之间。

C3 拆了 Mini JIT 的编译管线——IL 怎么经过 SSA、优化、寄存器分配最终变成 native code。但 C3 没有回答一个前置问题：Mini JIT 是怎么被触发的？一个方法从 `MonoMethod` 描述符到实际执行的 native code（或解释器），中间经过了哪些分派和决策步骤？这篇回答这个问题。

> **本文明确不展开的内容：**
> - Mini JIT 编译管线的内部阶段（IL → SSA → 优化 → native code 的详细流程在 C3 已覆盖）
> - 解释器的执行模型细节（interp 的主循环、InterpFrame、eval stack 在 C2 已覆盖）
> - AOT 编译的构建时流程（AOT 模式的编译管线和产物格式在 C5 展开）

## 方法执行在 Mono 中的位置

Mono 的方法执行链路横跨三个子系统：

```
metadata 层           执行层                     编译层
┌──────────┐    ┌────────────────┐    ┌──────────────────────┐
│MonoMethod│───→│ 分派器         │───→│ Mini JIT / LLVM 后端 │
│(方法描述符)│    │ (trampoline /  │    │ (编译 IL → native)   │
│          │    │  runtime_invoke)│    │                      │
└──────────┘    │                │───→│ Interpreter (interp) │
                │                │    │ (直接解释 IL)         │
                └────────────────┘    └──────────────────────┘
```

`MonoMethod` 是 metadata 层的方法描述符（C1 已介绍）。分派器决定一个方法调用最终走哪条执行路径。编译层负责把 IL 变成可执行的形式——JIT 编译为 native code，或解释器直接执行 IL 字节码。

这条链路的核心源码分布在 `mono/metadata/object.c`（`mono_runtime_invoke`）、`mono/mini/mini-runtime.c`（`mono_jit_compile_method`）、`mono/mini/mini-trampolines.c`（trampoline 机制）和 `mono/mini/interp/interp.c`（解释器入口）中。

## MonoMethod 结构

C1 已经介绍了 `MonoMethod` 的核心字段。从方法执行的角度看，有几个字段特别关键：

```
MonoMethod
  ├─ name          方法名称
  ├─ klass         所属类型（MonoClass*）
  ├─ signature     参数和返回值的类型签名
  ├─ flags         方法属性标志（virtual / static / abstract 等）
  ├─ token         metadata token（用于在 MonoImage 中定位 IL）
  ├─ slot          vtable 中的 slot 索引（虚方法）
  └─ info          指向编译结果的指针容器
       ├─ compiled_code   JIT/AOT 编译产出的 native code 入口
       └─ interp_method   解释器的 InterpMethod 缓存
```

`compiled_code` 是方法执行链路中最重要的指针。当一个方法尚未被 JIT 编译时，这个指针指向一个 trampoline（占位跳板）。当 JIT 编译完成后，这个指针被替换为 native code 的入口地址。后续对同一方法的调用直接跳到 native code，不再经过 trampoline。

`MonoMethod` 本身不包含 IL 字节码的副本。IL 字节码存储在 `MonoImage` 中，`MonoMethod` 通过 `token` 字段间接引用——需要时通过 `mono_method_get_header` 从 `MonoImage` 中读取方法的 IL 头和字节码。

## 方法调用分派

### mono_runtime_invoke

`mono_runtime_invoke` 是 Mono 中从 runtime 内部调用托管方法的主要入口。当 runtime 需要调用一个 C# 方法（比如静态构造函数、finalizer、反射调用目标）时，都通过这个函数。

```c
MonoObject* mono_runtime_invoke(MonoMethod *method,
                                 void *obj,
                                 void **params,
                                 MonoObject **exc);
```

它接收方法描述符、this 指针（实例方法）、参数数组，返回方法的返回值。内部的分派逻辑是：

1. **检查方法是否已有编译结果。** 如果 `method->info->compiled_code` 指向有效的 native code 入口（非 trampoline），直接调用
2. **尝试 JIT 编译。** 如果 JIT 可用（非 Full AOT 模式），调用 `mono_jit_compile_method` 编译方法，获取 native code 入口，再调用
3. **fallback 到解释器。** 如果 JIT 不可用（Full AOT 模式下的某些方法，或显式配置了 interpreter-only），通过 interp 模块直接解释执行方法的 IL

`mono_runtime_invoke` 是一个"保证方法能被执行"的封装——它不关心方法最终走 JIT 还是解释器，只保证调用能完成。

### 正常方法调用的分派

对于 JIT 编译后的正常方法调用（非通过 `mono_runtime_invoke`），分派路径更短。

**直接调用（call 指令）。** JIT 编译 `call` 指令时，如果目标方法已经被编译过，JIT 直接把 native code 地址嵌入调用指令。如果目标方法尚未编译，JIT 把一个 trampoline 地址嵌入调用指令——方法首次被调用时走 trampoline 触发编译。

**虚调用（callvirt 指令）。** 虚调用通过 vtable 间接分派。vtable 中每个 slot 存储的是方法的 native code 入口——如果方法已编译就是 native code 地址，未编译就是 trampoline 地址。虚调用的过程是：从对象的类型指针找到 vtable → 用方法的 slot 索引取出入口地址 → 跳转。

**接口调用。** 在虚调用的基础上多一层接口映射表查找（C3.5 类型加载篇已详细分析），最终同样落到 vtable slot 中的入口地址。

三种调用方式最终都归结为同一个问题：入口地址是 native code 还是 trampoline？如果是 native code，直接执行；如果是 trampoline，先编译再执行。

## Trampoline 机制

### 什么是 trampoline

Trampoline（跳板）是 Mono 方法执行链路中最巧妙的设计之一。

当一个方法尚未被 JIT 编译时，它的 vtable slot 或调用点中存储的不是 native code 地址，而是一段很短的"跳板代码"。这段跳板代码的作用是：保存调用现场 → 调用 JIT 编译器编译目标方法 → 获取编译结果的 native code 地址 → 把原来指向 trampoline 的位置替换为 native code 地址 → 跳转到 native code 执行。

```
方法首次调用：

调用者 ──call──→ trampoline
                   │
                   ├─ 保存调用现场（寄存器、栈状态）
                   ├─ 调用 mono_jit_compile_method
                   │    └─ Mini JIT 编译管线 → native code
                   ├─ 回填：把 trampoline 替换为 native code 地址
                   └─ 跳转到 native code
                        │
                        └─ 方法实际执行

方法后续调用：

调用者 ──call──→ native code（直接执行，不经过 trampoline）
```

trampoline 的回填操作（patching）是原子性的——一旦替换完成，后续所有对该方法的调用都直接到达 native code，trampoline 不再被触发。这保证了 JIT 编译的"一次编译、永久生效"语义。

### Trampoline 的种类

Mono 有多种 trampoline，用于不同的分派场景。

**方法 trampoline（Method Trampoline）。** 最基本的跳板——方法首次调用时触发 JIT 编译。上文描述的就是这种。

**类初始化 trampoline（Class Init Trampoline）。** 方法调用前检查目标类型的静态构造函数（`.cctor`）是否已执行。如果未执行，先运行 `.cctor`，然后替换为方法 trampoline 或直接跳到 native code。

**泛型 trampoline（Generic Trampoline）。** 处理泛型方法的调用。泛型方法可能需要根据具体类型参数选择不同的编译结果，泛型 trampoline 在运行时解析类型参数并路由到正确的 native code。

**委托 trampoline（Delegate Trampoline）。** 处理 delegate 的调用。delegate 在 Mono 中通过一个特殊的 trampoline 来延迟绑定目标方法。

所有 trampoline 的核心逻辑都在 `mono/mini/mini-trampolines.c` 中实现。平台相关的 trampoline 代码生成（trampoline 本身也是 native code 片段）在各平台的 `mini-<arch>-trampoline.c` 中实现。

### 与 CoreCLR PreStub 的对比

CoreCLR 有一个功能等价的机制叫 PreStub。

| 维度 | Mono Trampoline | CoreCLR PreStub |
|------|----------------|-----------------|
| **触发时机** | 方法首次调用 | 方法首次调用 |
| **核心动作** | 保存现场 → JIT 编译 → 回填 → 跳转 | 保存现场 → JIT 编译 → 回填 → 跳转 |
| **回填目标** | vtable slot / 调用点 | MethodDesc 中的 stub slot |
| **分层支持** | 无——一次编译 | 有——Tier0 编译后可能再被 Tier1 替换 |
| **trampoline 类型** | 方法、类初始化、泛型、委托 | PreStub、ClassInit、VSD Stub、Interface Stub |
| **源码位置** | mini-trampolines.c | prestub.cpp |

两者在机制上高度相似——都是"先占位、后编译、最后回填"的模式。核心差异在于分层编译的支持。CoreCLR 的 PreStub 在 Tier0 编译后，方法入口被替换为 Tier0 代码；随后如果方法被认定为热路径，Tier1 编译器会重新编译并再次替换入口。Mono 没有分层编译——trampoline 被替换后就是最终版本。

这个差异的工程影响是：CoreCLR 方法的入口地址在运行时可能被多次替换（Tier0 → Tier1），而 Mono 方法的入口地址只被替换一次（trampoline → native code）。

## JIT 编译触发 — mono_jit_compile_method

### 编译入口

`mono_jit_compile_method` 是从 trampoline 到 Mini JIT 管线的桥梁。它的职责是：

1. 检查方法是否已有编译结果（double-check，防止并发重复编译）
2. 读取方法的 IL 字节码（通过 `mono_method_get_header`）
3. 调用 Mini JIT 编译管线（C3 中分析的完整管线）
4. 把编译产出的 native code 存储到 JIT 代码缓存中
5. 更新 `MonoMethod` 上的 `compiled_code` 指针

```c
gpointer mono_jit_compile_method(MonoMethod *method, MonoError *error)
{
    // 1. 检查是否已编译
    code = mono_jit_info_get_code_start(
               mono_jit_info_table_find(domain, method));
    if (code)
        return code;

    // 2. 获取 IL
    header = mono_method_get_header(method);

    // 3. 调用 Mini 编译
    cfg = mini_method_compile(method, ...);

    // 4. 缓存编译结果
    mono_jit_info_table_add(domain, cfg->jit_info);

    // 5. 更新 compiled_code
    return cfg->native_code;
}
```

上面是简化的伪码，实际实现包含大量的错误处理、锁保护和特殊情况分支。

### 编译结果缓存

编译产出的 native code 被存储在一个 per-domain 的 JIT info table 中。`MonoJitInfo` 结构记录了编译结果的元信息：

- native code 的起始地址和大小
- 异常处理信息（try/catch/finally 的 native code 区域映射）
- GC Map（每个安全点的根集信息，供 SGen 使用）
- 调用方法对应的 `MonoMethod`

JIT info table 按 native code 地址排序，支持二分查找——给定一个 native code 中的指令地址，可以快速找到它属于哪个方法的编译结果。这个反向查找能力对异常处理（需要从异常发生的指令地址找到对应的异常处理子句）和 GC（需要从栈帧的返回地址找到对应的 GC Map）至关重要。

### 并发编译

多个线程可能同时调用同一个未编译的方法。`mono_jit_compile_method` 通过以下策略处理并发：

1. 进入编译逻辑前获取全局的 JIT 域锁
2. 在锁内再次检查方法是否已被其他线程编译（double-check pattern）
3. 如果已有编译结果，释放锁，直接返回已有结果
4. 如果没有，在锁的保护下执行编译，完成后释放锁

这种策略保证了同一个方法只被编译一次，但代价是全局锁的序列化——同一时刻只有一个线程在做 JIT 编译。CoreCLR 的并发 JIT 策略更精细：允许多个线程同时编译不同的方法，只在 JIT info table 的插入操作上加锁。Mono 的全局 JIT 锁是一个简化设计，在方法数量少或编译频率低的场景下影响不大，但在启动阶段大量方法首次调用时可能成为瓶颈。

## 解释器 fallback

### 什么情况走解释器

C2 详细分析了 Mono 解释器的执行模型和定位。从方法执行分派的角度，解释器在以下情况被触发：

**Full AOT 模式下的 AOT 遗漏。** Full AOT 要求所有方法在构建时预编译。但某些方法——特别是泛型方法的延迟实例化——可能在构建时未被 AOT 编译器发现。如果启用了 Mixed 模式（AOT + Interpreter），这些遗漏的方法 fallback 到解释器执行。如果没有启用解释器 fallback，运行时会抛出异常。

**反射调用的动态目标。** 通过 `MethodInfo.Invoke` 进行的反射调用，目标方法可能是运行时动态确定的。在 Full AOT 模式下，这些动态目标无法保证有 AOT 编译结果。解释器可以直接读取方法的 IL 字节码并执行。

**Blazor WebAssembly 场景。** 在 WASM 平台上，解释器是主要的执行引擎（C2 中已详细分析）。所有方法默认走解释器，除非通过 AOT 预编译了特定的程序集。

**显式配置。** 通过命令行参数 `--interpreter` 可以强制所有方法走解释器执行，跳过 JIT。这主要用于调试和测试。

### 解释器执行入口

当分派器决定一个方法走解释器执行时，调用链大致是：

```
方法调用
  │
  ├─ mono_runtime_invoke（或 trampoline 判断走解释器）
  │
  ├─ interp_exec_method（解释器主循环入口）
  │     ├─ 构建 InterpFrame（栈帧）
  │     ├─ 获取 InterpMethod（如果没有缓存则创建）
  │     └─ 进入 switch dispatch 主循环
  │           逐条解释 CIL 指令
  │
  └─ 返回结果
```

`InterpMethod` 在首次解释执行时创建并缓存到 `MonoMethod` 上。后续对同一方法的解释执行直接使用缓存的 `InterpMethod`，不需要重复创建。

### JIT 与解释器的边界调用

在 Mixed 模式下，JIT 编译的方法和解释器执行的方法会互相调用。这个跨执行引擎的调用需要特殊处理：

**JIT → Interpreter。** JIT 编译的方法调用一个只有解释器执行结果的方法时，调用链经过一个 interp trampoline，把参数从 native 栈帧格式转换为解释器的 eval stack 格式，进入解释器主循环。

**Interpreter → JIT。** 解释器执行到一个 call 指令，目标方法有 JIT 编译结果时，解释器从 eval stack 取出参数，按 native 调用约定排列，直接跳到 native code 入口。

这种跨引擎调用有额外的开销——参数格式转换和栈帧重建。在 Mixed 模式下，如果 JIT 和解释器之间的调用频繁发生，这个开销可能变得显著。

## 与 CoreCLR / IL2CPP / HybridCLR / LeanCLR 的方法执行对比

| 维度 | Mono | CoreCLR | IL2CPP | HybridCLR | LeanCLR |
|------|------|---------|--------|-----------|---------|
| **方法描述** | MonoMethod | MethodDesc | MethodInfo (Il2Cpp) | MethodInfo + InterpMethodInfo | RtMethodInfo |
| **首次调用** | trampoline → JIT 编译 | PreStub → JIT 编译 | 直接执行（构建时已编译） | AOT 方法直接执行 / 热更方法 transform + 解释 | transform + 解释 |
| **编译触发** | mono_jit_compile_method | ThePreStub → jit_compile | 无（构建时完成） | 热更方法首次调用时 transform | 首次调用时三级 transform |
| **编译缓存** | per-domain JIT info table | MethodDesc.stub slot | 无需（静态链接） | InterpMethodInfo 缓存 | LL-IL 指令流缓存 |
| **分层编译** | 无 | Tier0 → Tier1 + PGO | 无 | 无 | 无 |
| **解释器 fallback** | 有（Mixed 模式） | 无 | 无 | 解释器是热更方法的唯一路径 | 解释器是唯一路径 |
| **跨引擎调用** | JIT ↔ Interpreter | 无（纯 JIT） | 无（纯 AOT） | AOT ↔ Interpreter | 无（纯解释器） |
| **native code 回填** | trampoline patching | PreStub patching | 无需 | 无（解释器方法不产出 native code） | 无（解释器不产出 native code） |

几个差异的深层原因：

**Mono 的 trampoline vs CoreCLR 的 PreStub。** 两者在"懒编译"这一核心机制上完全一致——都是先放占位跳板，首次调用时触发编译并回填。差异在于后续演进：CoreCLR 的 Tiered Compilation 让方法入口可以被多次替换（Tier0 → Tier1），而 Mono 只有一次替换机会。这意味着 Mono 的 JIT 必须在唯一的一次编译中产出足够好的代码——这也是 C3 中分析的 Mini 选择图着色寄存器分配而非 LSRA 的工程背景。

**IL2CPP 没有运行时编译的概念。** IL2CPP 的所有方法在构建时已经变成了 C++ 函数，经过 C++ 编译器编译为 native code，静态链接到最终产物中。运行时的方法调用就是普通的 C/C++ 函数调用——没有 trampoline，没有 JIT 触发，没有编译缓存。这是 AOT 方案在方法执行路径上的极致简化。

**HybridCLR 的双路分派。** HybridCLR 的方法执行分两条路：AOT 代码中的方法直接执行（和 IL2CPP 完全一致），热更新代码中的方法走解释器。跨路径的调用通过 IL2CPP 的 `Runtime::Invoke` 桥接。这种双路分派的结构和 Mono 的 Mixed 模式（JIT + Interpreter）在架构上是同构的——区别在于 AOT 层不同（Mono JIT vs IL2CPP AOT）和解释器设计不同（直接 IL 解释 vs HiOpcode transform 解释）。

**LeanCLR 没有分派决策。** LeanCLR 是纯解释器 runtime，所有方法走同一条路径：首次调用时经过三级 transform（MSIL → HL-IL → LL-IL），缓存 LL-IL 指令流，后续调用直接解释执行 LL-IL。没有 JIT、没有 trampoline、没有跨引擎调用——最简单的执行模型。

## 收束

Mono 的方法执行链路可以从三个层次理解。

**MonoMethod 是描述，不是执行。** `MonoMethod` 记录了方法的名称、签名、所属类型、IL 位置，但它本身不包含可执行的代码。它是 metadata 和执行引擎之间的桥接结构——metadata 层创建它，执行层消费它。

**Trampoline 是懒编译的关键。** Mono 不在程序启动时编译所有方法，而是用 trampoline 占位。方法首次被调用时，trampoline 触发 JIT 编译，编译完成后回填 native code 入口，后续调用不再经过 trampoline。这种"先占位、后编译、最后回填"的模式和 CoreCLR 的 PreStub 机制同源，但 Mono 没有分层编译——一次编译就是最终版本。

**三路 fallback 是 Mono 的执行弹性。** Mono 的方法执行不是只有 JIT 一条路。在 JIT 可用的平台上，trampoline 触发 JIT 编译是默认路径。在 JIT 不可用的平台上（Full AOT），AOT 预编译结果是主路径。在 AOT 也覆盖不到的方法上（泛型延迟实例化等），解释器是最后的 fallback。这种三路 fallback 让 Mono 在"所有方法都能执行"这一目标上有了最大的覆盖弹性——代价是 Mixed 模式下跨引擎调用的额外开销。

## 系列位置

- 上一篇：MONO-C4 SGen GC：精确式分代 GC 与 nursery 设计
- 下一篇：MONO-C5 Mono AOT：Full AOT 与 LLVM 后端
