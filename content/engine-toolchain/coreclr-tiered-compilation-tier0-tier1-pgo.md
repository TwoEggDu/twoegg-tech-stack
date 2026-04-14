---
title: "CoreCLR 实现分析｜Tiered Compilation：多级 JIT、动态降级与 PGO"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解分层编译的完整实现：Tier0 最小优化快速启动、CallCountingStub 的调用计数与热方法标记、Tier1 完整优化后台编译与代码替换、On-Stack Replacement 在回边处的运行中升级、动态 PGO 的 profile 收集与 Guarded Devirtualization、ReadyToRun 预编译作为 Tier0 替代及与 IL2CPP AOT 的根本区别、CoreCLR / IL2CPP / HybridCLR / LeanCLR 四种执行策略的对比。CoreCLR 模块完结篇。"
weight: 49
featured: false
tags:
  - CoreCLR
  - CLR
  - TieredCompilation
  - JIT
  - PGO
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> Tiered Compilation 让 CoreCLR 在启动速度和稳态性能之间不再做非此即彼的选择——Tier0 用最小优化快速启动，Tier1 在后台用完整优化替换热方法，PGO 用运行时 profile 指导优化决策。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 10 篇，也是 CoreCLR 模块的完结篇。

B9 分析了 Reflection 与 Emit——运行时类型查询和动态代码生成。那篇关注的是"能不能在运行时造新方法"。这篇关注的是另一个问题：已经 JIT 编译过的方法，能不能再编译一次，编译得更好？Tiered Compilation 的回答是可以——而且 CoreCLR 为此构建了一套从调用计数、代码替换到 profile 引导优化的完整机制。

## Tiered Compilation 在 CoreCLR 中的位置

B4 分析过 RyuJIT 的编译管线和 PreStub 机制：方法首次调用时触发 JIT 编译，编译产物写入 CodeHeap，方法指针替换为 native code 地址，后续调用直接跳转。

这个模型有一个隐含的 trade-off：JIT 编译时间和代码质量成反比。优化做得越多（内联、CSE、循环优化、寄存器分配），生成的 native code 越快，但编译本身耗时越长。方法首次调用时用户在等待——编译延迟直接体现为启动卡顿。

.NET Core 2.1 引入的 Tiered Compilation 打破了这个非此即彼的困境。核心思路：方法不只编译一次。第一次用最少的优化快速编译（Tier0），让方法尽快可执行；运行一段时间后，对被频繁调用的热方法用完整优化重新编译（Tier1），替换 Tier0 的代码。

```
方法的编译生命周期：

首次调用 → PreStub → Tier0 JIT（最小优化）
                        ↓
              方法可执行，用户看到响应
                        ↓
         调用计数累积到阈值 → 标记为热方法
                        ↓
         后台线程 → Tier1 JIT（完整优化）
                        ↓
         替换方法指针 → 后续调用走 Tier1 代码
```

Tiered Compilation 的实现跨越几个模块：

**PreStub 与 CallCountingStub（`src/coreclr/vm/prestub.cpp`、`callcounting.cpp`）** — 触发 Tier0 编译和调用计数。

**TieredCompilation（`src/coreclr/vm/tieredcompilation.cpp`）** — 管理 Tier1 编译的后台队列和策略。

**RyuJIT（`src/coreclr/jit/`）** — 同一个编译器，根据 tier 级别调整优化策略。Tier0 跳过大部分优化阶段，Tier1 全开。

**PGO（`src/coreclr/vm/pgo.cpp`）** — Profile-Guided Optimization 的数据收集和查询。

## 为什么需要多级编译

单级 JIT 面临一个结构性矛盾。

### 启动场景

应用启动时需要编译大量方法——Main 方法、依赖注入容器初始化、配置读取、日志框架初始化等。一个典型的 ASP.NET Core 应用启动时可能触发数百个方法的 JIT 编译。如果每个方法都用完整优化编译（内联展开、循环优化、全局寄存器分配），编译时间累加导致启动延迟显著。

用户感知到的是：`dotnet run` 之后等了 2 秒才看到第一个 HTTP 响应。其中相当一部分时间花在 JIT 编译上。

### 稳态场景

应用运行稳定后，性能热点集中在少量方法上——HTTP 请求处理管线、数据库查询路径、序列化/反序列化路径。这些方法被调用百万次以上。每个方法节省 1 纳秒，累积节省毫秒级延迟。

这些热方法需要最激进的优化：方法内联消除调用开销，CSE 消除重复计算，循环优化减少迭代开销，寄存器分配减少内存访问。

### 矛盾

启动场景需要编译快（少优化），稳态场景需要代码快（多优化）。单级 JIT 只能选一个策略，要么牺牲启动速度换稳态性能，要么牺牲稳态性能换启动速度。

Tiered Compilation 的解决方案是两次编译：Tier0 满足启动场景，Tier1 满足稳态场景。代价是热方法编译了两次（Tier0 + Tier1），但 Tier0 编译很快（代价小），且 Tier1 编译在后台线程进行（不阻塞用户请求）。

## Tier0：最小优化

Tier0 的目标是最快速度产出可执行的 native code。RyuJIT 在 Tier0 模式下跳过几乎所有优化阶段：

- **不做方法内联。** 遇到方法调用直接生成 `call` 指令，不尝试把被调用方法的代码展开到调用方中
- **不做 CSE（公共子表达式消除）。** 重复的表达式求值每次都执行
- **不做循环优化。** 循环体不做不变量外提、循环展开等变换
- **不做 SSA 构建和基于 SSA 的分析。** 跳过静态单赋值形式的构建和依赖它的数据流分析
- **简化寄存器分配。** 不使用完整的 LSRA，用更简单的分配策略

Tier0 保留的阶段是 Importer（IL → GenTree，这一步不能跳过，否则没有 IR 可以 codegen）和 CodeGen（GenTree → native code）。中间的优化管线基本被旁路。

结果是 Tier0 的编译速度约为 Tier1 的 3-5 倍，但生成的代码质量显著较低——没有内联意味着更多的函数调用开销，没有 CSE 意味着重复计算，没有循环优化意味着循环体执行效率低。

对于只调用几次的方法（初始化代码、配置解析），Tier0 的代码质量完全够用——这些方法的总执行时间本身就很短，优化带来的收益可以忽略。对于被频繁调用的热方法，Tier0 的代码质量不够，需要升级到 Tier1。

## 调用计数与升级触发

CoreCLR 怎么知道哪些方法是热方法？通过 CallCountingStub 机制。

### CallCountingStub

当 Tier0 编译完成后，方法的入口不是直接指向 Tier0 的 native code，而是指向一个 CallCountingStub。这个 stub 是一小段机器码，做两件事：

1. 递减该方法的调用计数器（一个整数变量）
2. 如果计数器降到零，触发 Tier1 编译请求；否则跳转到 Tier0 的 native code 继续执行

```
CallCountingStub（伪汇编）：
  dec [method_counter]        ; 计数器减 1
  jz  trigger_tier1_promotion ; 到零了，触发升级
  jmp tier0_native_code       ; 没到零，执行 Tier0 代码

trigger_tier1_promotion:
  push method_desc            ; 方法标识
  call TieredCompilation::AsyncPromote  ; 加入 Tier1 编译队列
  jmp tier0_native_code       ; 本次调用仍然执行 Tier0 代码
```

### 计数阈值

默认的调用计数阈值是 30。也就是说，一个方法被调用 30 次后被标记为热方法，加入 Tier1 编译队列。

这个数字是经验值——太小会导致过多方法进入 Tier1 编译（编译开销大，且很多方法调用 30 次后就不再被调用了），太大会延迟热方法的优化。30 次在大多数场景下是合理的平衡点。

计数阈值可以通过环境变量 `DOTNET_TC_CallCountThreshold` 调整。在性能调优场景中，降低阈值可以更快地触发 Tier1 编译，但会增加后台编译线程的负载。

### Stub 的替换

升级触发后，CallCountingStub 不会立即被移除——Tier1 编译需要时间。在 Tier1 编译完成之前，方法每次调用仍然经过 CallCountingStub 跳转到 Tier0 代码执行。计数器已经到零后，stub 不再做递减操作（用一个标志位标记为已触发），直接跳转到 Tier0 代码。

Tier1 编译完成后，runtime 替换方法的入口指针——从 CallCountingStub 变为 Tier1 的 native code 地址。后续调用直接跳转到 Tier1 代码，不再经过 stub。CallCountingStub 的内存在适当时机被回收。

## Tier1：完整优化

Tier1 使用 RyuJIT 的完整编译管线——B4 分析的所有阶段全开。

### 优化全开

- **方法内联（Inlining）。** 把小方法的代码展开到调用方中，消除 `call` / `ret` 指令的开销和栈帧建立成本。RyuJIT 根据被调用方法的 IL 大小、调用频率、嵌套深度等因素决定是否内联
- **CSE（公共子表达式消除）。** 识别重复计算的子表达式，只计算一次，后续引用结果
- **循环优化。** 循环不变量外提、循环展开、强度削减
- **SSA 构建和数据流分析。** 构建静态单赋值形式，基于 SSA 做常量传播、死代码消除、值域分析
- **完整 LSRA 寄存器分配。** 线性扫描寄存器分配，最大化寄存器利用率，最小化 spill/reload

### 后台编译

Tier1 编译在专用的后台线程上执行，不阻塞应用的工作线程。`TieredCompilationManager` 维护一个待编译方法的队列，后台线程从队列取出方法逐个编译。

后台线程的优先级设置为低于正常工作线程。在 CPU 繁忙时，Tier1 编译自动让步，避免抢占应用的 CPU 资源。在 CPU 空闲时，后台编译快速推进。

### 代码替换

Tier1 编译完成后需要替换 Tier0 的代码。这里有一个并发安全的问题：替换发生时，其他线程可能正在执行 Tier0 的代码。

CoreCLR 的替换策略是原子地更新方法入口指针。新的调用会走 Tier1 代码，正在 Tier0 代码中执行的线程不受影响——它们会执行完当前的 Tier0 调用后，下一次进入该方法时才走 Tier1 路径。Tier0 的 native code 不会立即释放，而是延迟到确认没有线程在执行它之后才回收。

## On-Stack Replacement（OSR）

Tiered Compilation 的标准流程有一个盲点：长循环方法。

### 问题

考虑这样的方法：

```csharp
void ProcessAll(List<Item> items)
{
    foreach (var item in items)
    {
        ProcessItem(item);  // 被调用百万次
    }
}
```

`ProcessAll` 只被调用一次——调用计数永远不会到 30。但这一次调用内部执行了百万次循环迭代，循环体的执行时间可能长达数秒。Tier0 编译的循环体没有经过任何优化（没有内联 `ProcessItem`、没有循环不变量外提），性能显著低于 Tier1。

标准的 Tiered Compilation 无法帮助这种情况——方法调用计数不够，不会触发 Tier1 升级；即使人为降低阈值，等待 Tier1 编译完成也需要时间，而 Tier0 的循环可能已经执行完了大部分迭代。

### .NET 7 的 OSR

.NET 7 引入了 On-Stack Replacement（OSR）来解决这个问题。OSR 允许在方法执行过程中——不需要等方法返回——就用优化后的版本替换当前正在执行的代码。

实现机制：

**回边计数。** JIT 在 Tier0 编译的循环回边（backedge，即循环体末尾跳回循环头部的跳转指令）处插入计数逻辑。每次循环迭代，回边计数器递增。

**OSR 入口。** 当回边计数达到阈值时，Tier0 代码暂停循环执行，触发 OSR 编译请求。RyuJIT 为这个方法生成一个特殊的 Tier1 版本——这个版本不是从方法入口开始执行，而是从当前循环所在的位置开始，继承 Tier0 的局部变量状态。

**状态迁移。** OSR 编译产物需要接收 Tier0 的执行状态——局部变量、循环计数器、栈帧中的临时值。JIT 在 Tier0 编译时为每个潜在的 OSR 入口点记录局部变量的布局信息，OSR 编译产物按照这个布局从 Tier0 的栈帧中读取状态。

**替换执行。** 状态迁移完成后，执行从 Tier1 版本的 OSR 入口点继续。后续的循环迭代全部在 Tier1 代码上执行——内联、CSE、循环优化全部生效。

```
长循环方法的 OSR 流程：

Tier0 执行：
  iteration 1 ... iteration 99  → 回边计数累积
  iteration 100 → 回边计数到阈值 → 暂停
                                  ↓
                   触发 OSR 编译（Tier1，从循环处开始）
                                  ↓
                   迁移局部变量状态
                                  ↓
Tier1 执行：
  iteration 101 ... iteration 1000000 → 全速优化代码
```

OSR 的代价是实现复杂度高——JIT 需要在 Tier0 和 Tier1 之间协调局部变量布局，状态迁移需要精确匹配。但收益是显著的：长循环方法不再被 Tier0 的低质量代码拖慢。

## Profile-Guided Optimization（PGO）

Tiered Compilation 的自然延伸是：既然方法要编译两次，能不能在 Tier0 执行期间收集运行时信息，用这些信息指导 Tier1 的优化决策？这就是动态 PGO（Profile-Guided Optimization）。

### 动态 PGO 的数据收集

.NET 6 引入了动态 PGO（.NET 8 默认启用）。Tier0 编译的代码中被插入 profiling 逻辑，收集以下数据：

**基本块执行次数。** 每个基本块（没有分支的直线代码段）被执行了多少次。这告诉 JIT 哪些代码路径是热路径、哪些是冷路径。

**分支概率。** if/else 分支的两个方向各被走了多少次。JIT 在 Tier1 中根据分支概率调整代码布局——把热路径放在 fall-through 方向（不需要跳转），冷路径放在跳转方向。

**类型 profile。** 对于虚方法调用和接口方法调用，记录实际接收者的类型分布。这是 Guarded Devirtualization 的基础。

### Guarded Devirtualization

虚方法调用是 .NET 中常见的性能瓶颈。虚调用通过 vtable 间接跳转，阻止了内联和其他跨方法优化。

Guarded Devirtualization 利用 PGO 的类型 profile 数据：如果 Tier0 期间 90% 的调用目标都是 `ConcreteType`，Tier1 生成的代码结构如下：

```
if (obj.GetType() == typeof(ConcreteType))
    // 直接调用 ConcreteType.Method()（可以内联）
    ConcreteType.Method(obj);
else
    // 回退到虚调用（vtable 间接跳转）
    obj.VirtualMethod();
```

类型检查（`obj.GetType() == typeof(ConcreteType)`）是一次内存读取 + 比较——远比 vtable 间接跳转便宜。而且如果直接调用路径的方法体足够小，JIT 会把它内联到调用方中，完全消除调用开销。

这种"猜测 + 守卫 + 回退"的模式在 JVM（HotSpot）中已经使用多年，CoreCLR 通过动态 PGO 获得了同样的能力。

### PGO 数据的传递

Tier0 收集的 profile 数据存储在 `PgoManager`（`src/coreclr/vm/pgo.cpp`）管理的缓冲区中。当 Tier1 编译启动时，JIT 从 `PgoManager` 查询目标方法的 profile 数据。如果数据存在，JIT 在优化阶段使用这些数据做决策：

- 基本块执行次数 → 代码布局优化（热路径 fall-through，冷路径 jump）
- 分支概率 → 条件分支方向选择
- 类型 profile → Guarded Devirtualization 和推测性内联

如果没有 profile 数据（方法在 Tier0 没有被执行过、或数据尚未收集完毕），Tier1 回退到静态启发式（与不使用 PGO 时相同的优化策略）。

## ReadyToRun（R2R）

ReadyToRun 是一种预编译格式，可以看作 Tier0 的 AOT 替代。

### R2R 的定位

`dotnet publish` 时加上 `-p:PublishReadyToRun=true`，Roslyn 编译器 + crossgen2 工具在构建阶段把 IL 预编译为目标平台的 native code，嵌入在程序集 DLL 中。

运行时加载 R2R 程序集时，方法的初始 native code 直接来自预编译产物——不需要 Tier0 的 JIT 编译。方法从第一次调用起就以 native 速度执行，启动延迟进一步降低。

```
无 R2R：
  方法首次调用 → Tier0 JIT（编译延迟）→ native code

有 R2R：
  方法首次调用 → 直接执行 R2R 预编译代码（零编译延迟）
```

### R2R + Tiered Compilation

R2R 代码的优化级别介于 Tier0 和 Tier1 之间。crossgen2 在构建阶段做了一些优化（比 Tier0 的最小优化多），但受限于 AOT 的约束——没有运行时 profile 数据，无法做 Guarded Devirtualization 和 profile-guided 的代码布局优化。

关键设计：R2R 代码可以被 Tier1 替换。运行时仍然对 R2R 方法做调用计数，热方法仍然会触发 Tier1 编译。Tier1 用完整优化 + PGO 数据重新编译，替换 R2R 的预编译代码。

```
R2R + Tiered Compilation 的完整路径：

构建阶段：IL → crossgen2 → R2R native code（中等优化）
运行时：
  方法首次调用 → R2R 代码（零延迟）
  调用计数累积 → 标记为热方法
  PGO 数据收集 → 记录类型 profile、分支概率
  后台 Tier1 编译 → 完整优化 + PGO → 高质量 native code
  替换 R2R 代码 → 后续调用走 Tier1
```

### 与 IL2CPP AOT 的根本区别

R2R 和 IL2CPP 都是 AOT 编译，但设计目标完全不同。

**R2R 是可升级的 AOT。** R2R 代码只是起步——运行时可以用 JIT 重新编译任何方法，生成更优的代码。R2R 解决的是启动延迟问题，不是最终代码质量问题。

**IL2CPP 是终态的 AOT。** IL2CPP 把所有 IL 翻译为 C++ 并编译为 native binary。运行时没有 JIT，不能重新编译。代码质量完全取决于 C++ 编译器（Clang/MSVC）在构建阶段的优化。

这个区别的直接后果：

- R2R 应用可以使用 Emit 和动态代码生成——JIT 仍然存在
- IL2CPP 应用不能使用 Emit——没有 JIT
- R2R 应用可以从动态 PGO 中获益——运行时收集 profile 指导 Tier1 优化
- IL2CPP 应用只能使用静态 PGO（构建阶段提供 profile 数据给 C++ 编译器）

R2R 的定位更接近"JIT 的启动加速器"而非"JIT 的替代品"。

## 与其他执行策略的对比

CoreCLR 的 Tiered Compilation 是四种执行策略之一。不同的 runtime 面对同一个问题（IL 怎么变成可执行代码）做了不同的选择。

| 维度 | CoreCLR Tiered | IL2CPP | HybridCLR | LeanCLR |
|------|---------------|--------|-----------|---------|
| **执行模型** | 渐进式优化 JIT | 一步到位 AOT | interpreter + AOT 补丁 | interpreter（计划中 AOT） |
| **启动路径** | Tier0 JIT 或 R2R | AOT native 直接执行 | AOT native + interpreter fallback | 解释执行 |
| **热方法优化** | Tier1 完整优化 + PGO | C++ 编译器优化（构建时固定） | 无升级路径 | 无升级路径 |
| **运行时代码生成** | 完整支持（JIT + Emit） | 不支持 | interpreter 执行新代码 | 不支持 |
| **PGO** | 动态 PGO（运行时 profile → Tier1） | 仅静态 PGO | 无 | 无 |
| **代码替换** | 支持（Tier0 → Tier1，原子指针替换） | 不支持 | 不支持 | 不支持 |
| **OSR** | 支持（.NET 7+，循环中升级） | N/A | N/A | N/A |
| **目标场景** | 服务端、桌面、云 | Unity 游戏（iOS/Android/Console） | Unity 热更新 | H5/小游戏 |

几个差异展开说明：

**CoreCLR vs IL2CPP：渐进式 vs 一步到位。** CoreCLR 的哲学是"先跑起来再优化"——Tier0 提供快速启动，Tier1 提供稳态性能，PGO 提供基于真实数据的优化。IL2CPP 的哲学是"一次编译到最终形态"——所有优化在构建阶段完成，运行时不再有编译活动。IL2CPP 的优势是运行时没有 JIT 的内存开销和编译延迟（对移动设备重要），劣势是无法根据实际运行情况动态调整优化策略。

**HybridCLR 的定位。** HybridCLR 在 IL2CPP 的 AOT 之上叠加了一个 IL 解释器。AOT 编译的方法以 native 速度执行，热更新的新方法由解释器执行。解释器没有升级路径——热更新方法永远以解释速度运行，不会被编译为 native code。这与 CoreCLR 的 Tiered Compilation 形成鲜明对比：CoreCLR 的所有方法都有升级到 Tier1 的可能，HybridCLR 的热更新方法被锁定在解释层。这个限制是架构性的——HybridCLR 运行在 IL2CPP 的 runtime 上，没有 JIT 基础设施可以利用。

**LeanCLR 的纯解释策略。** LeanCLR 是纯解释器，所有方法都以解释速度执行。没有 Tier0/Tier1 的分层，也没有 AOT 预编译。执行性能是四种策略中最低的，但优势是部署简单（不需要 native 编译工具链）和平台适应性强（H5/小游戏环境限制 native code 生成）。LeanCLR 的技术路线图中包含 AOT 编译的计划，如果实现，其模型会更接近 HybridCLR（AOT + interpreter），但目标平台不同。

**动态 PGO 是 CoreCLR 独有的能力。** 四种执行策略中，只有 CoreCLR 能做到运行时收集 profile 并用它指导优化。IL2CPP 可以使用静态 PGO（从 profiling run 中收集数据，在构建阶段提供给 C++ 编译器），但静态 PGO 的数据来自测试运行，不一定反映生产环境的真实负载。CoreCLR 的动态 PGO 收集的是当前运行实例的真实 profile，优化决策直接基于实际负载——这在服务端场景中有显著价值。

## 收束

CoreCLR 的 Tiered Compilation 是这样一套机制：

**启动层。** Tier0 用最小优化快速编译方法，把启动延迟降到最低。R2R 进一步消除首次调用的 JIT 开销——预编译的 native code 在加载时直接可用。两者的目标相同：让应用尽快可用。

**升级层。** CallCountingStub 追踪每个方法的调用次数，热方法被加入 Tier1 编译队列。后台线程用完整优化重新编译，编译完成后原子替换方法入口指针。OSR 补全了长循环方法的盲点——不等方法返回，在循环回边处直接升级到优化代码。

**反馈层。** 动态 PGO 在 Tier0 执行期间收集 profile 数据——分支概率、类型分布、基本块执行频率。Tier1 用这些数据做更精准的优化决策：热路径 fall-through、Guarded Devirtualization、推测性内联。优化基于真实负载，不是静态假设。

三层的关系是时序上的递进：启动层让方法可执行，升级层让热方法变快，反馈层让升级后的代码更加精准。每一层的代价（Tier0 代码质量低、Tier1 编译消耗 CPU、PGO 收集增加 Tier0 的 overhead）都被后一层的收益覆盖。

这是 CoreCLR 模块的最后一篇。从 B1 的架构总览到 B10 的 Tiered Compilation，十篇文章覆盖了 CoreCLR 的核心子系统：启动链路、程序集加载、类型系统、JIT 编译、垃圾回收、异常处理、泛型、线程同步、反射与 Emit、分层编译。后续系列将进入 Mono、IL2CPP 和 HybridCLR 的实现分析，CoreCLR 建立的概念框架是理解这些 runtime 设计选择的基线。

## 系列位置

- 上一篇：CLR-B9 Reflection 与 Emit：运行时代码生成
- CoreCLR 模块完结，下一模块：Mono
