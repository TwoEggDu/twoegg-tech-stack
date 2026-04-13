---
title: "HybridCLR 前置篇｜CIL 指令集与栈机模型：ldloc、add、call 到底在做什么"
date: "2026-04-13"
description: "在进入 HybridCLR 解释器优化之前，先把 CIL 指令集和栈机执行模型讲清楚：eval stack 怎么运转、核心指令分几类、一个最小方法的 IL 逐行拆解、栈机为什么慢，以及这些概念在 HybridCLR 源码里对应什么。"
weight: 11
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "CIL"
  - "ECMA-335"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
series_order: -1
---
> HybridCLR 的 transform 阶段把 CIL 方法体转成 HiOpcode IR。如果不知道 CIL 长什么样，后面看 transform 优化就会一直卡在"它到底在优化什么"这个问题上。

这是 HybridCLR 系列的前置篇 B，位于 Pre-A（CLI Metadata）之后、HCLR-1（原理拆解）之前。

Pre-A 解决的是"metadata 里存了什么"——类型表、方法表、字段表这些静态结构。这篇解决的是"方法体里存了什么"——CIL 指令怎么编码、求值栈怎么运转、一条 `add` 在运行时到底做了几件事。

把这两篇前置补完之后，再进 HCLR-1 看 RuntimeApi 到 Interpreter::Execute 的调用链，每一层都能对上。

> 本文的 CIL 语义遵循 ECMA-335 规范。所有 IL 示例均可通过 `ildasm`、`dnSpy` 或 `ILSpy` 在 Unity 编译产物上验证。

## 为什么要先讲 CIL

直接原因：HCLR-27（解释器指令优化）的核心主题是 CIL 栈机到寄存器 IR 的转换。那篇文章的第一句话就提到"CIL 是类型多态的，一个 `add` 处理 i4、i8、f4、f8"。如果读者不知道 CIL 的 `add` 长什么样、为什么是类型多态的，后面的 8 种 transform 优化就全变成了抽象描述。

根本原因：HybridCLR 解释器的输入就是 CIL 方法体。`TransformContext.cpp` 里的 `TransformBodyImpl` 逐条读 CIL 指令，生成 HiOpcode 指令流。如果把 CIL 当黑盒，那 transform 的输入侧就是不可读的。

所以这篇不讲 HybridCLR 源码，只讲 CIL 本身。目标是让读者看完之后，能直接读懂任意一段 IL dump，然后顺畅地进入 HCLR-1 和 HCLR-27。

## CIL 是栈机

CIL（Common Intermediate Language）是 .NET 运行时的中间语言。C# 编译器（Roslyn）把源代码编译成 CIL，存储在 DLL 的方法体中。运行时（CLR 或 IL2CPP）再把 CIL 转成目标平台的机器码或解释执行。

CIL 的执行模型是栈机（stack machine），而不是寄存器机。这意味着所有计算都通过一个隐式的栈来完成，指令不会直接指定"从哪个寄存器读、往哪个寄存器写"。

每个方法在执行时维护三个数据区域：

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

CIL 一共大约 220 条指令。按功能分成以下几类，每类只列最核心的几条。

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

`ldloc.0` ~ `ldloc.3` 是索引 0~3 的短指令，占 1 字节；`ldloc.s` 带一个 uint8 操作数，覆盖索引 4~255；`ldloc` 带 uint16 操作数，覆盖更大范围。这种长/短格式在 CIL 中很常见，目的是压缩方法体体积。

### 算术

从求值栈弹出操作数，计算后压回结果。

| 指令 | 动作 |
|------|------|
| `add` | pop 两个值，相加，push 结果 |
| `sub` | pop 两个值，相减，push 结果 |
| `mul` | pop 两个值，相乘，push 结果 |
| `div` | pop 两个值，相除，push 结果 |

重点：**同一条 `add` 指令处理所有数值类型**。运行时根据栈上操作数的实际类型决定是做 int32 加法、int64 加法、float32 加法还是 float64 加法。CIL 规范用一张类型合并表（ECMA-335 Partition III, Table 2）定义了所有合法的操作数类型组合和结果类型。

这就是所谓的"类型多态"——一条指令，多种行为，运行时判断。

### 字段访问

| 指令 | 动作 |
|------|------|
| `ldfld <token>` | pop 对象引用，push 该对象的指定实例字段值 |
| `stfld <token>` | pop 值和对象引用，把值写入该对象的指定实例字段 |
| `ldsfld <token>` | push 指定静态字段的值（不需要对象引用） |
| `stsfld <token>` | pop 值，写入指定静态字段 |

`<token>` 是 metadata token，指向字段定义表中的一行。运行时通过 token 解析到字段的内存偏移量。

### 方法调用

| 指令 | 动作 |
|------|------|
| `call <token>` | 调用指定方法（静态方法或非虚实例方法） |
| `callvirt <token>` | 虚调用：pop 对象引用，查 vtable，调用实际方法 |
| `newobj <token>` | 分配对象 + 调用构造函数，push 新对象引用 |

`call` 在编译时就确定目标方法，`callvirt` 需要在运行时查虚方法表。C# 编译器对实例方法默认生成 `callvirt`（即使方法不是 virtual），因为 `callvirt` 会先做 null check。

### 类型操作

| 指令 | 动作 |
|------|------|
| `box <token>` | 把值类型装箱成引用类型 |
| `unbox.any <token>` | 把引用类型拆箱成值类型 |
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

C# 编译器为这个方法生成的 IL（用 `ildasm` 或 `ILSpy` 可以直接看到）：

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

`.maxstack 2` 告诉运行时：这个方法执行过程中求值栈最多同时存在 2 个值（`ldarg.1` 之后、`add` 之前的瞬间）。这个值由编译器计算，运行时用它来分配栈空间。

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

生成的 IL：

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

这里可以看到一个特征：为了读两个字段，`this` 被 load 了两次。CIL 栈机没有"把 this 留在某个寄存器里复用"的能力——每次 `ldfld` 都会消耗栈顶的对象引用，下次用就得重新 push。

## 为什么栈机直接解释慢

把上面两个例子的指令数量对比一下：

**C# 表达式 `a + b`（静态方法）：**

CIL 需要 4 步才能完成一次加法并存储结果：

```
ldarg.0     // 1. push a
ldarg.1     // 2. push b
add         // 3. pop + pop + push
stloc.0     // 4. pop → 存到局部变量
```

如果换成寄存器式 IR，同样的语义只需要 1 条：

```
add r0, r1, r2    // r0 = r1 + r2，直接寻址，无栈操作
```

**开销差异来自三个方面：**

**第一，指令条数。** 4 条 vs 1 条，意味着 4 次 dispatch vs 1 次 dispatch。每次 dispatch 都要读 opcode、查跳转表、跳到 handler——在解释器里这是主要的固定开销。

**第二，隐式栈操作。** 每条 CIL 指令背后都有 push/pop 操作。`add` 不是简单的"读两个数相加"，而是"从栈顶弹出两个值、相加、把结果压回栈顶"。如果解释器老老实实模拟这个栈，每条指令都要做栈指针的加减和内存读写。

**第三，类型判断。** CIL 的 `add` 在运行时需要判断操作数类型。如果解释器按 CIL 语义逐条执行，每遇到一个 `add` 都要走一次类型分派：这是 int32 加法？int64 加法？float 加法？

这就是 HybridCLR 为什么不直接解释 CIL，而是先做 transform 的根本原因。transform 把栈式操作转成寄存器式操作，同时把类型多态指令展开成类型特化指令，一次性消除上面三层开销。

## CIL 的类型多态 vs HiOpcode 的类型特化

CIL 的设计哲学是"一条指令打天下"：

```
add     →  同时处理 int32、int64、float32、float64、IntPtr
ceq     →  同时处理所有可比较类型
ldelem  →  同时处理所有数组元素类型
```

这对编译器生成 IL 来说很方便——不需要为每种类型选不同的指令。但对解释器来说，每次执行都要做一次运行时类型判断，这是纯开销。

HybridCLR 的 HiOpcode 走了相反的路：把一条 CIL 指令展开成多条类型特化指令。

```
CIL:     add
           ↓ transform 阶段，根据 EvalStackVarInfo 确定操作数类型
HiOpcode:  BinOpVarVarVar_Add_i4    （两个 int32 相加）
           BinOpVarVarVar_Add_i8    （两个 int64 相加）
           BinOpVarVarVar_Add_f4    （两个 float32 相加）
           BinOpVarVarVar_Add_f8    （两个 float64 相加）
```

transform 阶段通过 `GetEvalStackReduceDataType()` 在编译时就确定了操作数的归约类型（i4、i8、f4、f8、obj 等），然后直接选择对应的 HiOpcode 变体。执行时不再需要任何类型判断——每条 HiOpcode 只做一种类型的计算。

这也是为什么 `HiOpcodeEnum` 有 1000 多个条目，而 CIL 只有 220 条指令。不是 HybridCLR 发明了更多语义，而是它把类型维度展开了。

## 这些概念在 HybridCLR 里对应什么

读完 CIL 的栈机模型之后，可以直接把每个概念映射到 HybridCLR 的源码结构上。

| CIL 概念 | HybridCLR 源码对应 | 位置 |
|----------|-------------------|------|
| Evaluation Stack（求值栈） | `EvalStackVarInfo` 数组 | `TransformContext.h` |
| Local Variable（局部变量） | `localVarBase` 偏移 | `Interpreter_Execute.cpp` |
| Argument（参数） | `localVarBase` 起始区域 | 参数和局部变量共享同一段连续内存 |
| CIL 方法体 | `MethodBodyCache` 的输入 | transform 阶段的原始输入 |
| CIL opcode | `HiTransform` 的输入 | `TransformContext.cpp` 逐条读取 |
| HiOpcode | `Interpreter::Execute` 的输入 | `Interpreter_Execute.cpp` 的 switch 循环 |
| `.maxstack` | `InterpMethodInfo.evalStackSize` | transform 产出物的一部分 |

几个值得注意的对应关系：

**eval stack 在 transform 之后就消失了。** CIL 的求值栈在 transform 阶段被 `EvalStackVarInfo` 模拟，目的是追踪每个栈位置的类型和偏移。transform 完成后，所有栈操作都被转成了对 `localVarBase` 的直接偏移寻址。执行阶段没有求值栈——只有一段连续内存和一组偏移量。

**参数、局部变量、临时变量共享同一段内存。** CIL 把它们分成三个逻辑区域，但 HybridCLR 在 transform 之后把它们拍平成一段 `StackObject` 数组，统一用 `uint16_t` 偏移寻址：

```
localVarBase
├── [0 .. argEnd)           参数区
├── [argEnd .. localEnd)    局部变量区
└── [localEnd .. evalEnd)   求值栈临时区（transform 后变成临时变量区）
```

**CIL opcode 和 HiOpcode 是一对多。** 一条 CIL `add` 可能变成 `BinOpVarVarVar_Add_i4`、`_Add_i8`、`_Add_f4` 或 `_Add_f8` 中的一条。一条 CIL `ldfld` 可能变成 `LdfldVarVar_1`（1 字节字段）到 `LdfldVarVar_32`（32 字节字段）中的一条。这种一对多展开是 transform 消除运行时判断的基本手段。

## 收束

CIL 是一套基于求值栈的指令集。每条指令隐式操作栈顶，不直接寻址寄存器或内存位置。这个设计让 IL 格式紧凑、编译器实现简单，但给解释器带来了三层额外开销：指令条数多、隐式栈操作多、运行时类型判断多。

HybridCLR 的 transform 阶段做的核心工作，就是把这三层开销一次性消除：栈式变寄存器式、多态变特化、多条变一条。理解了 CIL 这一侧的输入长什么样，再去看 HCLR-27 的 transform 优化，每一种优化的动机和效果就能直接对上。

---

## 系列位置

- 上一篇：Pre-A CLI Metadata
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}}">HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute</a>
- 回到入口：<a href="{{< relref "engine-toolchain/hybridclr-series-index.md" >}}">HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇</a>
- 相关后文：<a href="{{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}}">HybridCLR 解释器指令优化｜标准优化与高级优化在 HiOpcode 层到底做了什么</a>
