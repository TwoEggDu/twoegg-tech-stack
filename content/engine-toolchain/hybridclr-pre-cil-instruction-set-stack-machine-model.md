---
slug: "HybridCLR 前置篇｜CIL 指令集与栈机模型：ldloc、add、call 到底在做什么"
title: "ECMA-335 基础｜CIL 与栈机模型：指令语义和最小验证规则"
date: "2026-04-13"
description: "从求值栈、参数和局部变量读懂 CIL，合并最小 IL 验证规则，再理解 IL2CPP 编译与 HybridCLR 转换的边界。"
weight: 11
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "CIL"
  - "ECMA-335"
series: "HybridCLR"
lastmod: "2026-10-23"
series_order: -1
---
一个方法只有几行 C#，IL 中却包含多次加载、存储和跳转。这些指令在描述什么，为什么 IL2CPP 能把它们编译成 native，HybridCLR 又能把它们转换后解释执行？

本篇保留 HybridCLR Pre-B 的旧地址，作为 ECMA 主线第 4 篇。先读懂求值栈，再把原验证篇的基础规则放到同一个例子中。具体解释器优化留给后续专题。

## CIL 是栈机

CIL（Common Intermediate Language）是 .NET 运行时的中间语言。C# 编译器（Roslyn）把源代码编译成 CIL，存储在 DLL 的方法体中。运行时（CLR 或 IL2CPP）再把 CIL 转成目标平台的机器码或解释执行。

CIL 的执行模型是栈机（stack machine），而不是寄存器机。算术指令通常消费隐式求值栈；加载、存储等指令仍可通过索引或 token 指定参数、局部变量和字段。这个抽象栈不要求实现真的逐条进行内存 push/pop。

在 CIL 抽象模型中，可以区分三个数据区域；实现不必为它们分配三块独立物理内存：

```
┌──────────────────────────────────────────┐
│  Evaluation Stack（求值栈）              │
│  所有中间计算结果在这里暂存              │
│  每条指令隐式操作栈顶                    │
├──────────────────────────────────────────┤
│  Local Variable Table（局部变量表）       │
│  .locals init 声明的局部变量             │
│  通过 ldloc/stloc 按索引读写            │
├──────────────────────────────────────────┤
│  Argument Table（参数表）                │
│  方法签名定义的参数                      │
│  通过 ldarg/starg 按索引读写            │
│  实例方法中 arg.0 是 this               │
└──────────────────────────────────────────┘
```

关键特征：CIL 指令不指定操作数的来源位置。`add` 这条指令没有"从第 3 号寄存器取值"的概念——它固定从求值栈顶弹出两个值、相加、把结果压回栈顶。"谁把值放到栈顶的"由前面的 load 指令决定。

## 核心指令逐类介绍

按功能先看以下几类，每类只列最核心的几条。

### 加载 / 存储

把值在求值栈和变量表之间搬运。

| 指令 | 动作 |
|------|------|
| `ldloc.N` / `ldloc.s N` | 把第 N 个局部变量的值 push 到求值栈 |
| `stloc.N` / `stloc.s N` | 从求值栈 pop 一个值，存入第 N 个局部变量 |
| `ldarg.N` / `ldarg.s N` | 把第 N 个参数的值 push 到求值栈 |
| `starg.s N` | 从求值栈 pop 一个值，存入第 N 个参数槽 |
| `ldc.i4 V` | 把 32 位整数常量 V push 到求值栈 |
| `ldc.i4.s V` | 短格式，V 为 int8 范围 |
| `ldc.i4.0` ~ `ldc.i4.8` | 把 0~8 push 到求值栈（专用短指令） |

`ldloc.0` ~ `ldloc.3` 是索引 0~3 的短指令，占 1 字节；`ldloc.s` 带一个 uint8 操作数，覆盖索引 0~255；`ldloc` 带 uint16 操作数，覆盖更大范围。这种长/短格式在 CIL 中很常见，目的是压缩方法体体积。

### 算术

从求值栈弹出操作数，计算后压回结果。

| 指令 | 动作 |
|------|------|
| `add` | pop 两个值，相加，push 结果 |
| `sub` | pop 两个值，相减，push 结果 |
| `mul` | pop 两个值，相乘，push 结果 |
| `div` | pop 两个值，相除，push 结果 |

同一条 `add` 可以处理规范允许的多种数值表示。参数签名与前序指令决定求值栈中的类型；编译器或解释器转换阶段可以分析这些信息。它并不要求每次执行 `add` 都做动态类型判断。

### 字段访问

| 指令 | 动作 |
|------|------|
| `ldfld <token>` | pop 对象引用，push 该对象的指定实例字段值 |
| `stfld <token>` | pop 值和对象引用，把值写入该对象的指定实例字段 |
| `ldsfld <token>` | push 指定静态字段的值（不需要对象引用） |
| `stsfld <token>` | pop 值，写入指定静态字段 |

`<token>` 可引用字段定义或成员引用，需要结合所属模块与声明类型解析。上表以引用类型的实例字段为例，值类型的地址访问还会涉及托管指针。

### 方法调用

| 指令 | 动作 |
|------|------|
| `call <token>` | 按指定目标调用，不按接收者做虚分派；也可直接调用某个虚方法实现 |
| `callvirt <token>` | 消费接收者和参数；保留空引用检查语义，需要时进行虚分派 |
| `newobj <token>` | 构造实例；引用类型产生对象引用，值类型产生相应值 |

`call` 的目标仍需按 metadata 引用解析。`callvirt` 也可以用于非虚实例方法，此时不需要虚分派。调用指令还要按签名消费参数，不能只看接收者。

### 类型操作

| 指令 | 动作 |
|------|------|
| `box <token>` | 把值类型装箱成引用类型 |
| `unbox.any <token>` | 对普通值类型取出装箱的值；引用类型目标具有相应的类型转换语义 |
| `castclass <token>` | 强制类型转换，失败抛 InvalidCastException |
| `isinst <token>` | 类型检查，成功 push 对象引用，失败 push null |

### 分支

| 指令 | 动作 |
|------|------|
| `br <offset>` | 无条件跳转 |
| `brtrue <offset>` | pop 一个值，非零则跳转 |
| `brfalse <offset>` | pop 一个值，为零则跳转 |
| `beq <offset>` | pop 两个值，相等则跳转 |
| `blt <offset>` | pop 两个值，小于则跳转 |

`<offset>` 是相对于下一条指令起始地址的字节偏移。

## 逐行拆解一个最小方法

```csharp
public static int Add(int a, int b)
{
    return a + b;
}
```

下面是保留计算核心的 IL 示意；实际 Debug／Release 产物的 nop、局部变量与 header 可能不同：

```
.method public hidebysig static int32 Add(int32 a, int32 b) cil managed
{
    .maxstack 2

    ldarg.0      // 把参数 a push 到求值栈
    ldarg.1      // 把参数 b push 到求值栈
    add          // pop 两个值，相加，push 结果
    ret          // pop 栈顶值作为返回值
}
```

逐步画出求值栈的状态变化：

```
指令执行前   →  ldarg.0    →  ldarg.1    →  add        →  ret
                push a         push b        pop b,a       pop sum
                               ┌───┐         push sum      返回
           ┌───┐  ┌───┐       │ b │         ┌───────┐
           │   │  │ a │       ├───┤         │ a + b │
           └───┘  └───┘       │ a │         └───────┘
            空栈               └───┘
```

示例需要的最大求值栈深度为 2。方法头中的 maxstack 是上界，允许更大；Tiny header 的隐含值为 8。不要把示意中的 `.maxstack 2` 当成每次编译后必定出现的头部数值。

注意：这是静态方法，所以 `ldarg.0` 是第一个参数 `a`，`ldarg.1` 是第二个参数 `b`。如果是实例方法，`ldarg.0` 就是 `this`，实际参数从 `ldarg.1` 开始。

## 稍复杂的示例

```csharp
public class Character
{
    public int hp;
    public int baseHp;

    public int GetHp()
    {
        return this.hp + this.baseHp;
    }
}
```

计算核心的 IL 示意：

```
.method public hidebysig instance int32 GetHp() cil managed
{
    .maxstack 2

    ldarg.0          // push this（实例方法，arg.0 = this）
    ldfld int32 Character::hp
                     // pop this，push this.hp 的值

    ldarg.0          // 再次 push this
    ldfld int32 Character::baseHp
                     // pop this，push this.baseHp 的值

    add              // pop 两个 int32，相加，push 结果
    ret              // 返回
}
```

逐步状态变化：

```
 ldarg.0     ldfld hp     ldarg.0     ldfld baseHp    add          ret
 push this   pop this     push this   pop this        pop 两个值   pop 并返回
             push hp值               push baseHp值   push 结果

 ┌──────┐   ┌──────┐     ┌──────┐    ┌──────────┐    ┌─────────┐
 │ this │   │  hp  │     │ this │    │ baseHp   │    │hp+baseHp│
 └──────┘   └──────┘     ├──────┤    ├──────────┤    └─────────┘
                         │  hp  │    │    hp    │
                         └──────┘    └──────────┘
```

两个 ldfld 都消费一个接收者，因此示例两次加载 this。CIL 也有 dup 等栈操作；后端还可以消除重复加载，不能由 IL 文本中的次数推导最终访存次数。

## 最小验证规则：从同一段 IL 检查类型和栈

原 A8 的入门部分并入这里。先区分两个问题：IL 是否满足格式与执行语义要求，以及它能否通过规范的类型安全验证。满足前者的程序仍可能无法通过后者，不能把所有 unverifiable IL 都叫作损坏文件。

以 Add 为例，可以在执行前沿控制流追踪栈：

| 位置 | 栈形状 | 要检查什么 |
|---|---|---|
| 方法入口 | 空 | 参数类型来自方法签名 |
| ldarg.0、ldarg.1 后 | int32、int32 | 索引合法，栈深度未超界 |
| add 后 | int32 | 操作数类型组合合法 |
| ret | 返回一个 int32 | 与返回签名相容，返回时没有多余栈项 |

加入分支后，到达同一个汇合点的栈深度和类型状态还需要兼容。不能一条路径留下对象引用、另一条留下整数，再假定后面的 add 能自行解决问题。

局部变量的初始化、托管指针的使用、受保护区域的跳转也各有规则。`leave` 与普通 `br` 不可随意互换：离开异常保护区域涉及清栈和 finally 语义，下一篇会继续说明。

### 编译通过不是完整的 IL 验证证明

C# 编译器、IL 转换器、C++ 编译器和完整 CLI verifier 检查的是不同问题。尤其不能因为 IL2CPP 生成的 C++ 能编译，就宣布输入 IL 已完成全部类型安全验证。

对于 IL weaving、混淆或手工修改后的程序集，应保留修改前后的产物，用适合目标框架的验证工具检查，再做目标 Player 测试。工具拒绝了某项扩展与程序必然无法运行也不能直接划等号。

更完整的规则和边界放在 [IL 验证附录]({{< relref "engine-toolchain/ecma335-cli-verification-il-type-safety.md" >}})，不要求主线读者在这里掌握所有指令的验证条件。

## 从求值栈到执行实现

IL2CPP 可以在构建期分析栈状态并生成 C++；HybridCLR 的转换阶段可以把隐式栈上的中间值映射为内部 IR 的显式位置，再选择类型特化的操作。

```text
CIL：加载参数 → 加载参数 → add → 返回
  → 分析栈与类型
  → 内部表示或生成代码
  → native 执行／解释执行
```

对直接逐条调度的朴素解释器，指令数、栈维护和分派可能构成成本；预解码、类型分析和优化能改变这些成本。它们是实现选择，不是“栈机必然每次动态判断类型”的规范要求，也不能从图中的指令数量直接推导加速倍数。

后续看 [解释器基础桥接]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}}) 与 [HiOpcode 转换优化]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}})。

规范依据：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I 的执行与验证模型，以及 Partition III 的指令语义。

**CIL 规定如何计算以及必须满足的栈与类型条件；怎样高效实现这套语义，是 AOT 编译器和解释器的工作。**

## 系列位置

- 上一篇：[签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})
- 下一篇：[方法调用与异常处理]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
