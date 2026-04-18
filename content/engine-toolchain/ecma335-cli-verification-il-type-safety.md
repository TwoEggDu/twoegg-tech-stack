---
title: "ECMA-335 基础｜CLI Verification：IL 验证规则与 type safety 的运行时边界"
slug: "ecma335-cli-verification-il-type-safety"
date: "2026-04-15"
description: "从 ECMA-335 规范出发，拆解 CLI verification 的三层划分（Invalid / Verifiable / Unverifiable）、栈机层面的验证规则、Unverifiable IL 的典型来源，以及各 runtime（CoreCLR、Mono、IL2CPP、HybridCLR）verifier 实现现状的桥接说明。"
weight: 16
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Verification"
  - "TypeSafety"
  - "Security"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> Type safety 不是编译器单独保证的——它是 ECMA-335 verifier 在 IL 层面强制的契约。理解 verification 才能理解 unsafe 代码、calli 和 IL2CPP 为什么不需要运行时验证。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 9 篇。

前 8 篇展开了 metadata、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享。这些层次描述了"代码怎么组织、怎么跑"。这一篇补一块容易被忽略但直接影响安全模型与工程边界的拼图：verification——runtime 在执行 IL 之前如何证明这段代码"不会做坏事"。

> **本文明确不展开的内容：**
> - 完整的 IL 指令集（在 A2 已展开）
> - 各 runtime 的具体 verifier 实现细节（这篇只讲规范层桥接，CoreCLR/Mono/IL2CPP/HybridCLR 的实现细节在 B/D/E 模块展开）
> - CAS（Code Access Security）权限模型（已在 .NET 5+ 中废弃，不展开）

## 为什么需要 verification

C# 编译器生成的 IL 不一定是"安全"的。"安全"在这里有两层含义：

**Type safety** — 任何对值的操作都符合该值声明的类型。不会把一个 `int` 当成对象指针解引用，不会把一个长度为 4 的数组当成长度为 8 的数组访问。

**Memory safety** — 不会读写对象边界外的内存，不会通过悬垂指针访问已被回收的对象，不会跳过 GC 屏障直接修改 managed reference。

Type safety 是 memory safety 的充分条件。一个 type-safe 的程序不可能越界访问、不可能产生悬垂指针、不可能绕过类型系统强制的访问控制——因为这些行为本身就违反了类型系统的语义。

ECMA-335 Partition III §1.8 定义了 verification 的角色：在执行一段 IL 之前，runtime 应当能够证明这段 IL 满足 type safety。这个"证明"不是 runtime 自己想出来的——它由 verifier 静态分析 IL 完成，输入是 method body + metadata，输出是"verifiable / unverifiable"。

verification 的工程价值在 .NET 早期非常关键。当时 .NET Framework 想要支持 partial trust 场景——比如浏览器里跑的 Silverlight 控件、IIS 上跑的 medium trust web 应用——这些 assembly 来自非可信源，runtime 必须在执行前确认它们不会做出越权行为。verifier 就是这个机制的核心：通过 verification 的 IL 被允许执行，未通过的被拒绝。

到了现代 .NET 生态，partial trust 已经退场，但 verification 定义的边界仍然影响今天的工程实践——什么样的 IL 是 type safe 的、什么样的代码需要 unsafe 标记、为什么 IL2CPP 转换流程中不需要 verifier，这些问题的答案都指向 ECMA-335 §1.8。

## Verifiable / Unverifiable / Invalid 三个层级

ECMA-335 Partition III §1.8.1 把所有可能的 IL 划分成三个层级。这个划分是理解 verification 的起点。

### Invalid

违反 ECMA-335 基本规则的 IL。比如：

- 栈下溢（stack underflow）：一条 `add` 指令要求 eval stack 上至少有两个值，但执行到这条指令时栈是空的
- 引用了不存在的 metadata token（比如 `call` 指令引用了一个不在 MethodDef/MemberRef 表里的 method token）
- 控制流跳转到方法体之外（branch target 越界）
- 异常处理子句的 try/handler 区域定义错误（比如 try 区间和 handler 区间重叠但不嵌套）

任何 runtime 加载这样的 IL 时都应当拒绝执行——CoreCLR 在 JIT 阶段抛出 `InvalidProgramException`，IL2CPP 在 il2cpp.exe 转换阶段直接报错。Invalid IL 不存在"运行时容忍"的余地，它本质上是一段格式错误的代码。

### Verifiable

可证明 type safe 的 IL。verifier 通过静态分析能够确认：

- 每条指令对 eval stack 顶部的类型要求都被满足
- 每个局部变量在使用前都被赋值
- 每个对象引用在解引用前都被检查（runtime 自动插入 null check）
- 每个数组访问都在边界内（runtime 自动插入边界检查）
- 每个类型转换都符合继承关系或显式 cast 规则

C# 编译器在不使用 `unsafe` 关键字的情况下生成的 IL 默认应当是 verifiable 的。这是 C# 类型系统给 IL 层面的承诺。

### Unverifiable

可能 type safe 但 verifier 无法静态证明的 IL。典型来源是指针操作、union 布局、函数指针调用。这类 IL 不一定不安全——一个写得正确的 unsafe 块可能完全 type safe——但 verifier 没有足够的信息证明它。

举个例子，下面这段 C# 用了 `unsafe`：

```csharp
unsafe void CopyBytes(byte* src, byte* dst, int count) {
    for (int i = 0; i < count; i++) {
        dst[i] = src[i];
    }
}
```

这段代码生成的 IL 包含 `ldind.u1`（从指针位置加载一个字节）和 `stind.i1`（向指针位置存储一个字节）。verifier 无法知道 `src` 和 `dst` 指向的内存区域是否真的有 `count` 个字节——这取决于调用方传入的参数。从 verifier 的视角看，这段 IL 可能写到任意内存位置，所以它是 unverifiable 的。

### 三者的依存关系

三个层级之间的关系是：

```
合法 IL = Verifiable ∪ Unverifiable
Invalid = ¬合法 IL
```

也就是说：

- **Verifiable ⊂ 合法（不 Invalid）** — 通过 verification 的 IL 一定是合法的
- **Unverifiable ⊂ 合法（不 Invalid）** — 没通过 verification 的 IL 不一定 invalid，它可能只是 verifier 没法证明
- **Invalid 与 Verifiable/Unverifiable 不相交** — Invalid 的 IL 连基本格式都不对，谈不上是否 type safe

这个三层划分的意义在于：runtime 对三类 IL 的处理策略可以不同。Invalid 必须拒绝；Verifiable 可以无条件执行；Unverifiable 在不同的安全模型下可以选择拒绝（partial trust 时代）或允许（现代 .NET 的 full trust 默认行为）。

## 栈机层面的验证规则

ECMA-335 Partition III §1.8.1.3 定义了每条 IL 指令的 verification 规则。这些规则的统一形式是：

> 每条指令对 eval stack 顶部的若干项类型有要求，执行后 push 一个或多个新类型项。verifier 通过模拟整个方法体的栈类型变化，确认每条指令的输入类型都符合要求。

用 Unit/Player 参考类做示例：

```csharp
public class Unit : IHittable {
    public int hp;
    public virtual void TakeDamage(int amount) {
        hp -= amount;
    }
}
```

`Unit.TakeDamage` 编译后的 IL 大致如下（简化版）：

```
.method public hidebysig newslot virtual instance void TakeDamage(int32 amount) {
    ldarg.0           // push Unit& (this)
    ldarg.0           // push Unit&
    ldfld int32 Unit::hp     // pop Unit&, push int32
    ldarg.1           // push int32 (amount)
    sub               // pop int32, int32, push int32
    stfld int32 Unit::hp     // pop Unit&, int32
    ret
}
```

verifier 的工作是逐条指令模拟栈类型变化：

| 指令 | 执行前栈状态 | 指令要求 | 执行后栈状态 |
|------|--------------|----------|--------------|
| `ldarg.0` | `[]` | 无 | `[Unit&]` |
| `ldarg.0` | `[Unit&]` | 无 | `[Unit&, Unit&]` |
| `ldfld hp` | `[Unit&, Unit&]` | 顶部为 `Unit&`（或 `Unit`） | `[Unit&, int32]` |
| `ldarg.1` | `[Unit&, int32]` | 无 | `[Unit&, int32, int32]` |
| `sub` | `[Unit&, int32, int32]` | 顶部两项均为 numeric type | `[Unit&, int32]` |
| `stfld hp` | `[Unit&, int32]` | 顶部为 `int32`，下一项为 `Unit&` | `[]` |
| `ret` | `[]` | 栈为空（void 方法） | — |

每一步的"指令要求"如果与当前栈顶类型不匹配，verification 失败。比如如果 `stfld hp` 时栈上是 `[Unit&, string]`，verifier 会拒绝这段 IL，因为 `hp` 的类型是 `int32`，不能存储 string。

### 控制流合并点

verification 真正复杂的地方在控制流的合并点（比如 if/else 的汇合处、loop 的入口）。这些位置可能从多条路径到达，每条路径上 eval stack 的类型可能不同。verifier 需要确认：从所有路径到达同一位置时，栈上的类型在某个公共基类型上一致。

```csharp
public IHittable PickTarget(bool useUnit) {
    if (useUnit) return new Unit();
    else return new Wall();
}
```

这段代码的 IL 在 return 点合并两条路径：一条 push `Unit`，一条 push `Wall`。verifier 要求合并后的类型是两者的公共基类型——`IHittable`（如果两者都实现了这个接口）或 `object`。这个合并规则保证了控制流走任意路径，最终返回的对象都符合方法签名声明的返回类型。

### 局部变量的"可读"状态

ECMA-335 §1.8.1.5 规定：局部变量必须在赋值后才能读取。verifier 通过定值分析（definite assignment analysis）跟踪每个变量的"已赋值"状态。如果某条路径上变量未被赋值就被读取，verification 失败。

C# 编译器自己也做这个检查——但 verifier 在 IL 层面再做一次，是为了防止其他语言（F#、IL 直接编辑）生成绕过编译器检查的 IL。

## Unverifiable 的典型来源

不是所有 IL 都能通过 verification。ECMA-335 §1.8.1.2 列出了一组"verifier 无法证明 type safety"的指令和构造，这些就是 unverifiable IL 的来源。

### unsafe 代码：指针运算

C# 的 `unsafe` 块允许使用原生指针（`int*`、`byte*` 等）。指针操作对应的 IL 指令：

- `ldind.*` 系列（ldind.i1, ldind.i4, ldind.r8 等）— 从任意地址加载值
- `stind.*` 系列 — 向任意地址存储值
- `localloc` — 在栈上分配指定字节数的内存块
- `conv.u` / `conv.i` — 在整数和指针之间转换

verifier 无法证明这些操作是安全的——指针指向的内存是否在合法范围、是否对齐、是否属于当前进程都无法静态分析。

```csharp
unsafe int ReadInt(byte* ptr) {
    return *(int*)ptr;     // ldind.i4，verifier 无法证明 ptr 指向至少 4 字节
}
```

### calli 指令

`calli` 通过函数指针调用方法，函数指针是 eval stack 上的一个 native int。verifier 无法静态确定 `calli` 实际调用的是哪个方法——也就无法验证传入的参数类型是否匹配目标方法的签名。

```csharp
delegate int BinaryOp(int a, int b);

unsafe int Apply(BinaryOp op, int x, int y) {
    IntPtr fp = Marshal.GetFunctionPointerForDelegate(op);
    return ((delegate*<int, int, int>)fp)(x, y);  // 编译为 calli
}
```

C# 9.0 引入的 function pointer 语法（`delegate*<...>`）就编译为 calli。这类调用必然 unverifiable——这是 C# 把 unverifiable IL 引入主流语法的一次重要扩展。

### Explicit struct layout 暴露内存布局

`[StructLayout(LayoutKind.Explicit)]` + `[FieldOffset(...)]` 允许多个字段共享同一段内存（union 语义）。verifier 无法判断这种共享是否安全——读取一个字段时，实际读到的可能是另一个字段写入的位模式，类型完全不匹配。

```csharp
[StructLayout(LayoutKind.Explicit)]
struct IntFloatUnion {
    [FieldOffset(0)] public int intValue;
    [FieldOffset(0)] public float floatValue;
}
```

verifier 看到 `floatValue` 字段的 offset 与 `intValue` 重叠，无法保证读 `floatValue` 时拿到的是合法的 float 位模式（intValue 写入的整数可能对应非法的 float，比如 NaN payload 或非规格化数）。这类型 overlap 是 unverifiable 的。

### cpblk / initblk 内存块操作

`cpblk` 复制一段任意大小的内存块（类似 `memcpy`），`initblk` 用一个字节值初始化一段内存（类似 `memset`）。两条指令都接受三个 native int 参数：起始地址、大小或值。verifier 无法确认起始地址是否在合法的内存范围内、大小是否会越界、目标内存是否包含 GC 引用（错误地复制一段 GC 引用可能破坏 GC 的可达性追踪）。

这两条指令几乎只在 `unsafe` 上下文中出现，但它们的存在让 IL 层面具备了直接操作内存块的能力——这也是 unverifiable IL 的典型代表。

## 各 runtime 的 verifier 实现现状

ECMA-335 §1.8 是规范层的契约，各 runtime 的 verifier 实现现状差异极大。下面只做最简短的桥接说明，具体实现细节在后续模块展开。

### CoreCLR

`peverify` 工具（.NET Framework 时代的独立 verifier）已不再随 .NET Core / .NET 5+ 发布。运行时不主动 verify 任何加载的 IL——除非通过特定路径（如 `LoaderAllocator` 的某些显式调用）触发。

.NET 5+ 完全移除了 partial trust 场景：所有 assembly 默认运行在 full trust 下，没有"不可信代码"的概念。verifier 失去了主要用例，从 runtime 主流程中淡出。CoreCLR 的 JIT 仍然会做一些与 type safety 相关的检查（比如对非法的 metadata 抛 `InvalidProgramException`），但这是为了防止 JIT 自身崩溃，不是完整的 verification。

### Mono

`mono_verifier_verify_class` 在 Mono 主线代码中仍然存在，但默认只对来自非可信源的 assembly 触发。Unity 使用的 Mono runtime（Mono Scripting Backend）默认信任所有加载的 DLL，verifier 不会主动运行。

### IL2CPP

构建时通过 `il2cpp.exe` 把 IL 转换成 C++ 代码，C++ 编译器接管类型检查。运行时（libil2cpp）不需要独立 verifier——因为所有可执行代码已经过 C++ 编译期检查，IL 层面的 type safety 问题（如果存在）会在 il2cpp.exe 转换阶段或 C++ 编译阶段暴露。

这是 IL2CPP 一个常被忽略的工程优势：把 verification 工作从运行时前移到构建时，运行时少了一个开销来源。代价是构建时间变长、不能动态加载新 IL。

### HybridCLR

解释器执行不做完整 verification。热更 DLL 由开发者自己保证 type safety——HybridCLR 不会拒绝执行 unverifiable 的 IL，也不会在执行前做静态分析。

这是性能 / 灵活性的 trade-off：如果开发者在热更代码中误写了 unverifiable IL（比如手动编辑过的 IL，或某些 IL rewriter 工具产出的代码），运行时可能直接崩溃（segfault、内存破坏），而不是抛出 `VerificationException`。这要求热更代码的构建管线必须足够可靠——任何引入 unverifiable IL 的环节都需要在 CI 阶段提前发现。

## 工程影响

verification 的边界至今仍然影响几个具体的工程实践。

### Unity 项目里 unsafe 代码块的真实作用

C# 的 `unsafe` 关键字开启了对指针操作的语法支持，编译器允许生成 `ldind.*`、`stind.*` 等 unverifiable 指令。但在 Unity（Mono Scripting Backend 或 IL2CPP）默认配置下，runtime 并不强制 verify——`unsafe` 块在运行时和普通代码没有区别，区别只在编译期是否允许指针语法。

这意味着 Unity 项目里写 `unsafe` 不会带来运行时性能损失（因为没有 verifier 在跑），但仍然带来工程风险——指针错误不会被 runtime 捕获，只会以 segfault 或随机数据损坏的形式暴露。Burst 编译器对 `unsafe` 代码做了额外的静态检查（边界检查、aliasing 检查），这是 Unity 生态中少数把 verifier 思路重新引入的地方。

### HybridCLR 热更 DLL 的可靠性

如果热更 DLL 中出现 unverifiable IL（比如某个 obfuscator 工具产出的不规范 IL），IL2CPP 的 AOT 桥接层不会拒绝它，HybridCLR 解释器也不会拒绝它。运行时的表现不可预测——可能正常工作、可能轻微的内存损坏（看起来一切正常但 GC 偶尔崩溃）、可能直接 segfault。

工程上的应对策略是把 verification 的责任前移到构建管线：热更 DLL 在打包前用第三方 verifier（如 ILSpy 提供的检查、或 Mono 的 `mono-cil-strip` + verifier 组合）过一遍，确认所有方法都是 verifiable 的（除非项目明确允许 unsafe 热更代码）。

### 现代 .NET 实践

verification 在现代 .NET 生态中已淡出，type safety 主要靠两层保证：

- **C# 编译器** — 在 IL 生成阶段就拒绝绝大多数 type-unsafe 代码，需要显式 `unsafe` 关键字才能生成 unverifiable IL
- **BCL（基础类库）设计** — 把所有需要指针操作的 API 封装在 `Span<T>`、`Memory<T>`、`MemoryMarshal` 等抽象后，使用方不需要直接接触指针就能完成大多数高性能操作

这两层共同把 unverifiable IL 的产生面积压缩到了一个非常小的范围——典型应用代码不会主动产生 unverifiable IL，verification 也就不再是 runtime 的活跃职责。

## 收束

CLI verification 是 ECMA-335 设计的核心安全机制——它定义了"什么样的 IL 是 type safe 的"这个边界，并把这个边界从规范文字落实到可机械检查的算法。三层划分（Invalid / Verifiable / Unverifiable）、栈类型模拟、控制流合并的类型推导、unverifiable 指令的明确列举，构成了一套完整的形式系统。

但在现代 .NET 生态中，verification 已经从"运行时强制"退化为"工具层提示"。CoreCLR 不主动跑 verifier，IL2CPP 把检查前移到 C++ 编译期，HybridCLR 把责任交给开发者。Partial trust 场景的退场让 verifier 失去了最关键的用例。

理解 verification 仍然有价值，因为它定义的边界至今影响多个工程决策：

- `unsafe` 代码块在不同 runtime 上的实际行为
- HybridCLR 热更代码的可靠性保证应当放在哪里
- IL2CPP 的 AOT 转换为什么不需要 runtime verifier
- Native AOT 方案为什么对 unverifiable IL 更敏感

下一篇会展开另一个 ECMA-335 的关键机制：custom attributes 与 reflection 的 metadata 编码。verification 关心"IL 是否合法"，custom attributes 关心"metadata 还能携带哪些规范之外的信息"——两者一起构成了 .NET runtime 的元数据表达力上限。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}">A7 IL2CPP 泛型共享规则</a>
- 下一篇：<a href="{{< relref "engine-toolchain/ecma335-custom-attributes-reflection-encoding.md" >}}">A9 Custom Attributes 与 Reflection 元数据编码</a>
