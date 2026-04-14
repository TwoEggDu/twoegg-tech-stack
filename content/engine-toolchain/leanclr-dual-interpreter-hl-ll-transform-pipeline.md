---
slug: "leanclr-dual-interpreter-hl-ll-transform-pipeline"
date: "2026-04-14"
title: "LeanCLR 源码分析｜双解释器架构：HL-IL → LL-IL 的三级 transform 管线"
description: "拆解 LeanCLR 解释器的核心设计：MSIL(256) → HL-IL(182) → LL-IL(298) 三级变换管线，从 BasicBlockSplitter 到 HL Transformer 到 LL Transformer 的完整链路，以及与 HybridCLR 单级 HiOpcode 的架构差异。"
weight: 72
featured: false
tags:
  - LeanCLR
  - CLR
  - Interpreter
  - IR
  - Transform
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> LeanCLR 的解释器不是单层的。它先把 MSIL 转成 182 条 HL-IL，再转成 298 条 LL-IL，最后才执行。这个两级 transform 设计是它与 HybridCLR 单级 HiOpcode 最大的架构差异。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 3 篇。

## 从 LEAN-F1 接续

LEAN-F1 建立了全景地图，给出了 9 个模块的分布。LEAN-F2 拆解了 metadata 解析链路——从 PE 文件到 CliImage 的 5 个 stream，再到 RtModuleDef 构建出运行时结构。

这篇进入 LeanCLR 最大的模块：`interp/`。

回顾 F1 给出的数据：`interp/` 目录包含 19 个文件、31,829 行代码，独占整个运行时 43% 的代码量。这个比例本身就说明了一件事——对于一个纯解释执行的 CLR 来说，解释器就是运行时的核心。

metadata 解析回答的是"程序集里有什么"，解释器回答的是"怎么执行"。F2 完成了前半部分，这篇进入后半部分。

> **本文明确不展开的内容：**
> - 每条 opcode 的执行语义（298 条 LL-IL 指令不逐条分析，聚焦管线结构）
> - 异常处理展开逻辑（try/catch/finally 的解释执行在后续篇目展开）
> - AOT 编译器设计（LeanCLR 目前是纯解释执行，不涉及 AOT 编译器）

## 三级 transform 管线总览

LeanCLR 的解释器不直接执行 MSIL。从 DLL 中的 IL 字节码到解释器真正逐条执行的指令，中间经过了三级变换：

```
MSIL (256 opcodes)
  → HL-IL (182 opcodes)
    → LL-IL (298 opcodes)
      → Interpreter::execute
```

每一级的职责边界非常清晰：

**第一级输入：MSIL。** ECMA-335 Partition III 定义的原始 IL 指令集，256 个操作码。这是 .NET DLL 文件中存储的格式，也是 LEAN-F2 中 RtMethodInfo 从方法体中读出来的 raw bytes。

**第一级变换：MSIL → HL-IL。** 由 `HLTransformer` 完成。HL-IL（High-Level IL）压缩到 182 个操作码。这一步做的是**语义归一化**——把 MSIL 中语义等价但编码不同的指令合并。同时做栈模拟，将 MSIL 的栈式操作转为显式的寄存器/槽位引用。

**第二级变换：HL-IL → LL-IL。** 由 `LLTransformer` 完成。LL-IL（Low-Level IL）展开到 298 个操作码。这一步做的是**类型特化与指令选择**——把 HL-IL 中的泛型操作按实际类型展开成具体指令，同时烘焙参数索引和局部变量偏移。

**执行：** `Interpreter::execute` 逐条执行 LL-IL 指令。每条 LL-IL 指令的操作数已经完全确定，dispatch loop 只需要做最少的工作。

为什么要分两步而不是一步到位？后面的架构对比会详细讨论。先把每一步的实现拆清楚。

## BasicBlockSplitter：CFG 构建

**源码位置：** `src/runtime/interp/basic_block_splitter.h`

在做任何 transform 之前，LeanCLR 需要先把方法体的线性字节码切分成基本块（basic block）。这是所有编译器和高级解释器的标准做法。

### 基本块的定义

一个基本块是一段连续执行的指令序列：只能从头部进入，只能从尾部离开。基本块内部没有分支，没有跳转目标。

BasicBlockSplitter 扫描方法体的 MSIL 字节码，在以下位置切分基本块边界：

1. **分支指令之后**。`br`、`brfalse`、`brtrue`、`beq`、`bge`、`bgt`、`ble`、`blt`、`bne.un`、`switch` 等指令的下一条就是新基本块的起点。
2. **分支目标处**。任何被分支指令引用的目标偏移量，是新基本块的起点。
3. **异常处理子句边界**。`try` 块的起点和终点、`catch` 块的起点、`finally` 块的起点——这些都由 LEAN-F2 中 RtMethodInfo 解析出的 `RtInterpExceptionClause` 定义。
4. **方法入口**。偏移量 0 是第一个基本块的起点。

### CFG 构建

切分完成后，BasicBlockSplitter 建立基本块之间的控制流图（CFG, Control Flow Graph）。每个基本块记录：

- 起始 IL 偏移量
- 结束 IL 偏移量
- 后继基本块列表（fall-through + 跳转目标）
- 前驱基本块列表

这个 CFG 在后续的 HL Transform 中有两个用途：一是确保栈模拟在基本块边界正确传播（进入一个基本块时，eval stack 的状态必须与所有前驱基本块的出口一致）；二是为 LL Transform 的指令选择提供控制流信息。

### 和 HybridCLR 的差异

HybridCLR 也有基本块切分的逻辑，但它嵌入在 transform 过程中，不是一个独立的 pass。HybridCLR 的 `HiTransform::Transform` 在遍历 IL 的过程中同时识别基本块边界和做指令转换。

LeanCLR 把基本块切分独立出来作为第一步，是分层设计的体现——每一步只做一件事。这让 BasicBlockSplitter 可以独立测试和调试，不受 transform 逻辑的干扰。

## HL Transformer：MSIL → HL-IL

**源码位置：** `src/runtime/interp/hl_transformer.h`

HLTransformer 是三级管线的第一级变换。它接收 BasicBlockSplitter 产出的基本块列表和原始 MSIL 字节码，输出 HL-IL 指令序列。

这一步做了四件核心的事。

### 栈模拟（eval stack tracking）

MSIL 是一个纯栈式指令集。所有操作数都通过 eval stack 传递：

```
// C#: int c = a + b;
// MSIL:
ldloc.0    // push a
ldloc.1    // push b
add        // pop 2, push result
stloc.2    // pop result, store to c
```

这种栈式编码对编译器友好（生成简单），但对解释器不友好——每条指令执行前都需要做栈顶类型检查，每次 push/pop 都有栈指针操作的开销。

HLTransformer 做的第一件事就是**模拟 eval stack**。它在 transform 时跟踪每条指令执行后栈的深度和每个槽位的类型，把隐式的栈操作转换为显式的槽位引用。

具体来说，HLTransformer 为方法分配一组 eval stack 槽位（数量由方法 header 中的 `maxstack` 决定），每条 HL-IL 指令直接引用槽位编号，而不是做 push/pop。

### 冗余 load/store 消除

MSIL 中存在大量冗余的 load/store 模式。最常见的是：

```
ldloc.0
stloc.1
ldloc.1
```

这三条指令的净效果等价于把 `loc.0` 的值复制到 `loc.1` 并留一份在栈上。HLTransformer 在栈模拟的过程中识别这类模式，消除不必要的中间 load/store。

这不是一个通用的优化 pass，而是利用栈模拟信息顺手做的局部优化——因为 HLTransformer 已经知道每个栈槽里存的是哪个局部变量或参数，所以消除冗余的成本很低。

### 基本块边界对齐

栈模拟在基本块边界有一个约束：进入同一个基本块的所有路径，栈深度必须一致。ECMA-335 规范本身就要求这一点（Partition III, 1.7.5），但 HLTransformer 需要显式验证并处理。

对于有多个前驱的基本块（比如循环头部、条件分支的汇合点），HLTransformer 在第一次遇到时记录栈状态，后续遇到时验证一致性。如果不一致，说明 IL 不合法。

### HL-IL 的 182 条 opcode

HL-IL 的 opcode 定义在 `src/runtime/interp/hl_opcodes.h` 中。182 条 opcode 可以按功能分成以下几类：

| 类别 | 示例 | 说明 |
|------|------|------|
| 局部变量/参数 | `HL_LDLOC`, `HL_STLOC`, `HL_LDARG`, `HL_STARG` | MSIL 的 `ldloc.0/1/2/3/s`、`ldarg.0/1/2/3/s` 统一为带索引的单条指令 |
| 常量加载 | `HL_LDC_I4`, `HL_LDC_I8`, `HL_LDC_R4`, `HL_LDC_R8`, `HL_LDNULL`, `HL_LDSTR` | 各类型常量加载 |
| 算术运算 | `HL_ADD`, `HL_SUB`, `HL_MUL`, `HL_DIV`, `HL_REM`, `HL_NEG` | 类型无关的算术指令 |
| 比较运算 | `HL_CEQ`, `HL_CGT`, `HL_CLT` | 类型无关的比较 |
| 分支控制 | `HL_BR`, `HL_BRTRUE`, `HL_BRFALSE`, `HL_BEQ`, `HL_BGE`, `HL_BGT`, `HL_BLE`, `HL_BLT` | 条件和无条件跳转 |
| 类型转换 | `HL_CONV_I1`, `HL_CONV_I2`, `HL_CONV_I4`, `HL_CONV_I8`, `HL_CONV_R4`, `HL_CONV_R8` | 显式类型转换 |
| 方法调用 | `HL_CALL`, `HL_CALLVIRT`, `HL_NEWOBJ`, `HL_CALL_INTRINSIC` | 方法调用类 |
| 字段访问 | `HL_LDFLD`, `HL_STFLD`, `HL_LDSFLD`, `HL_STSFLD` | 实例字段和静态字段 |
| 数组操作 | `HL_LDELEM`, `HL_STELEM`, `HL_LDLEN`, `HL_NEWARR` | 数组访问 |
| 对象操作 | `HL_CASTCLASS`, `HL_ISINST`, `HL_BOX`, `HL_UNBOX`, `HL_LDOBJ`, `HL_STOBJ` | 类型检查、装箱拆箱 |
| 异常处理 | `HL_THROW`, `HL_RETHROW`, `HL_LEAVE`, `HL_ENDFINALLY` | 异常相关 |
| 其他 | `HL_NOP`, `HL_RET`, `HL_DUP`, `HL_POP`, `HL_INITOBJ` | 控制流与杂项 |

对比 MSIL 的 256 条，HL-IL 减少到 182 条。减少的主要来源是**编码归一化**：MSIL 为了压缩字节码大小，同一语义有多种编码（`ldloc.0` 是 1 字节，`ldloc.s` 是 2 字节，`ldloc` 是 3 字节），HL-IL 统一为一种。

但 HL-IL 仍然保持**类型无关**——`HL_ADD` 不区分操作数是 int32 还是 int64 还是 float。类型特化推迟到下一级。

## LL Transformer：HL-IL → LL-IL

**源码位置：** `src/runtime/interp/ll_transformer.h`

LLTransformer 是三级管线的第二级变换。它接收 HLTransformer 产出的 HL-IL 指令序列，输出 LL-IL 指令序列——这是解释器真正执行的指令格式。

从 182 条扩展到 298 条，增加的 116 条全部来自**类型特化**。

### 指令选择（instruction selection）

LL Transformer 的核心工作是指令选择。对于每条 HL-IL 指令，根据操作数的实际类型选择对应的 LL-IL 指令。

以 `HL_ADD` 为例。在 HL-IL 中只有一条 `HL_ADD`，但在 LL-IL 中展开为：

```
HL_ADD  →  LL_ADD_I4    (int32 + int32)
           LL_ADD_I8    (int64 + int64)
           LL_ADD_R4    (float32 + float32)
           LL_ADD_R8    (float64 + float64)
           LL_ADD_I_I4  (native int + int32)
           LL_ADD_I_I8  (native int + int64)
```

LLTransformer 怎么知道操作数类型？因为 HLTransformer 已经做了栈模拟，每个 eval stack 槽位的类型在 transform 时就确定了。LLTransformer 继承了这些类型信息，直接用于指令选择。

### 参数/局部变量索引烘焙（arg index baking）

HL-IL 中的 `HL_LDLOC` 和 `HL_LDARG` 使用的是逻辑索引（第几个局部变量、第几个参数）。到了 LL-IL 层，这些逻辑索引被烘焙成 InterpFrame 中的实际字节偏移量。

```
HL_LDLOC index=2
  → LL_LDLOC_I4 offset=16   // 假设 loc.0 是 int32(4字节), loc.1 是 int64(8字节)
                              // 那么 loc.2 的偏移就是 4+8=12...对齐到 16
```

烘焙后的偏移量让执行循环可以直接用指针算术访问数据，避免了每次执行时做"逻辑索引 → 偏移量"的查找。

### 类型特化分支

分支指令是类型特化最显著的区域。HL-IL 中的 `HL_BRTRUE` 只有一条，但不同类型的真值判断逻辑不同：

```
HL_BRTRUE  →  LL_BRTRUE_I4   (int32 != 0)
              LL_BRTRUE_I8   (int64 != 0)
              LL_BRTRUE_R4   (float32 != 0.0 且非 NaN)
              LL_BRTRUE_R8   (float64 != 0.0 且非 NaN)
              LL_BRTRUE_REF  (object ref != null)
```

比较分支同理，`HL_BEQ` 展开为 `LL_BEQ_I4`、`LL_BEQ_I8`、`LL_BEQ_R4`、`LL_BEQ_R8` 等。每条特化指令的执行体可以省略类型检查，直接做对应类型的比较。

### LL-IL 的 298 条 opcode

LL-IL 的 opcode 定义在 `src/runtime/interp/ll_opcodes.h` 中。298 条 opcode 按功能分类：

| 类别 | 数量（约） | 说明 |
|------|-----------|------|
| 算术运算（类型特化） | ~40 | ADD/SUB/MUL/DIV/REM 各 6-8 个类型变体 |
| 比较运算（类型特化） | ~25 | CEQ/CGT/CLT 各 5-6 个类型变体 |
| 分支（类型特化） | ~35 | BRTRUE/BRFALSE/BEQ/BGE/BGT/BLE/BLT 各 5+ 个类型变体 |
| 类型转换 | ~20 | CONV_xx 各类型对之间的转换 |
| 局部变量/参数（类型特化） | ~30 | LDLOC/STLOC/LDARG/STARG 的 I4/I8/R4/R8/REF 变体 |
| 常量加载 | ~10 | LDC 各类型 |
| 方法调用 | ~15 | CALL/CALLVIRT/NEWOBJ 的各种变体（含 intrinsic 快速路径） |
| 字段访问（类型特化） | ~25 | LDFLD/STFLD 的类型变体 + 静态字段 |
| 数组操作（类型特化） | ~30 | LDELEM/STELEM 的元素类型变体 |
| 对象操作 | ~20 | CASTCLASS/ISINST/BOX/UNBOX 等 |
| 异常处理 | ~8 | THROW/RETHROW/LEAVE/ENDFINALLY 等 |
| 控制流与杂项 | ~40 | NOP/RET/BR/SWITCH + 内部操作 |

298 条看起来比 HL-IL 的 182 条多了很多，但和 HybridCLR 的 1000+ 条 HiOpcode 相比，仍然精简得多。原因在于 LeanCLR 把"语义归一化"和"类型特化"分到了两层——HL 层只做归一化，LL 层只做特化。HybridCLR 在一层里同时做了两件事，导致 opcode 空间爆炸。

## 执行循环：Interpreter::execute

**源码位置：** `src/runtime/interp/interpreter.h`、`src/runtime/interp/interpreter.cpp`

三级 transform 完成后，产出的 LL-IL 指令序列交给 `Interpreter::execute` 执行。

### interp_defs.h：基础数据定义

**源码位置：** `src/runtime/interp/interp_defs.h`

执行循环依赖两个核心数据结构，定义在 `interp_defs.h` 中。

**RtStackObject** 是 eval stack 上每个槽位的运行时表示。它是一个 8 字节的 union：

```cpp
union RtStackObject {
    int32_t   i32;
    int64_t   i64;
    float     f32;
    double    f64;
    void*     ptr;
    RtObject* obj;
};
```

8 字节大小是一个经过权衡的选择：足够容纳 CLR 规范中所有基元类型（最大的是 int64 和 double，各占 8 字节），同时保持统一大小让栈指针操作变得简单——每次 push/pop 固定移动 8 字节。

HybridCLR 的 `StackObject` 也是类似的 union 设计，大小同样是 8 字节（在 64 位平台上）。这不是巧合，而是 CLR eval stack 语义的自然约束。

**InterpFrame** 是解释器的栈帧结构，代表一次方法调用的完整执行上下文：

```cpp
struct InterpFrame {
    InterpFrame*      parent;       // 调用者的栈帧
    RtMethodInfo*     method;       // 当前执行的方法
    const uint16_t*   ip;           // 指令指针（指向 LL-IL 指令流）
    RtStackObject*    stack;        // eval stack 基地址
    RtStackObject*    locals;       // 局部变量区基地址
    RtStackObject*    args;         // 参数区基地址
    RtObject*         exception;    // 当前异常对象（如果有）
};
```

几个设计要点：

- `ip` 指向的是 LL-IL 指令流，不是原始 MSIL。到了执行层面，MSIL 已经彻底不存在了。
- `locals` 和 `args` 的偏移在 LL Transform 阶段已经烘焙完成，执行时直接用指针算术访问。
- `parent` 形成一个链表，代表调用栈。这比操作系统的 native 调用栈更可控——解释器可以随时遍历整个调用链，用于栈追踪和异常处理。
- `exception` 字段让异常对象在栈帧之间传播变得简单。

### dispatch loop

`Interpreter::execute` 的核心是一个 dispatch loop。每次迭代读取当前 `ip` 指向的 opcode，执行对应的处理逻辑，然后推进 `ip`。

LeanCLR 使用 **switch dispatch**——一个巨大的 `switch` 语句覆盖所有 298 条 LL-IL opcode：

```cpp
void Interpreter::execute(InterpFrame* frame) {
    while (true) {
        uint16_t opcode = *frame->ip++;
        switch (opcode) {
            case LL_ADD_I4: {
                auto& a = frame->stack[op1];
                auto& b = frame->stack[op2];
                frame->stack[dst].i32 = a.i32 + b.i32;
                break;
            }
            case LL_ADD_I8: {
                auto& a = frame->stack[op1];
                auto& b = frame->stack[op2];
                frame->stack[dst].i64 = a.i64 + b.i64;
                break;
            }
            // ... 296 more cases
            case LL_RET: {
                return;
            }
        }
    }
}
```

上面是简化后的伪码，实际实现要复杂得多——包括操作数的解码、异常检查、方法调用的帧管理等。但核心结构就是这个 switch loop。

### 为什么是 switch dispatch

解释器的 dispatch 策略有几种主流选择：

| 策略 | 描述 | 优势 | 劣势 |
|------|------|------|------|
| switch dispatch | 一个 switch 覆盖所有 opcode | 简单、可移植 | 每次迭代一次间接跳转，分支预测压力大 |
| direct threading | 每个 opcode 处理末尾直接跳到下一个 opcode 的处理代码 | 减少一层间接跳转 | 依赖 GCC computed goto 扩展，可移植性差 |
| tail call threading | 每个 opcode handler 是独立函数，通过尾调用链接 | 缓存友好 | 依赖编译器优化尾调用 |

LeanCLR 选择 switch dispatch，原因和它的整体设计哲学一致：**可移植性优先**。LeanCLR 的目标平台包括 WebAssembly（Emscripten），而 Emscripten 对 computed goto 的支持有限。switch dispatch 在所有 C++ 编译器上都能正确工作。

HybridCLR 同样使用 switch dispatch（`HiInterpreter::Execute` 中的巨型 switch），原因类似——它需要在 IL2CPP 的编译环境中工作，而 IL2CPP 输出的 C++ 代码需要兼容多种平台编译器。

CoreCLR 的解释器（用于 Tiered Compilation 的 Tier 0 回退场景）也使用 switch dispatch。Mono 的解释器（mint/interp）则使用 computed goto（在支持的平台上）回退到 switch（在不支持的平台上）。

### LL-IL 指令的编码格式

LL-IL 指令流使用 `uint16_t` 数组编码。每条指令由 1 个 opcode 加若干个操作数组成：

```
[opcode:16] [operand1:16] [operand2:16] ...
```

操作数的数量和含义由 opcode 决定。比如：

```
LL_ADD_I4:  [opcode] [dst_slot] [src1_slot] [src2_slot]     → 4 个 uint16_t
LL_LDC_I4:  [opcode] [dst_slot] [imm_lo] [imm_hi]           → 4 个 uint16_t
LL_BR:      [opcode] [target_offset_lo] [target_offset_hi]   → 3 个 uint16_t
LL_RET:     [opcode]                                         → 1 个 uint16_t
```

使用 16 位而不是 8 位编码 opcode，是因为 298 条 opcode 已经超过了 8 位的范围（255）。使用 32 位又浪费空间。16 位是一个合适的平衡点——足够编码所有 opcode，同时保持指令流紧凑。

操作数中的 slot 索引也是 16 位，意味着一个方法最多可以有 65535 个 eval stack 槽位和局部变量。对于绝大多数方法来说这远远够用。

## MachineState：全局解释器状态

**源码位置：** `src/runtime/interp/machine_state.h`

MachineState 管理解释器的全局执行状态。它不是单个方法调用的状态（那是 InterpFrame 的事），而是整个解释器实例的共享资源。

### eval stack pool

解释器需要为每个方法调用分配 eval stack 和局部变量空间。如果每次调用都 malloc/free，性能开销不可接受。

MachineState 维护一个 eval stack pool——预分配一块大内存，按栈式分配（bump pointer）给每个 InterpFrame 使用。方法返回时，对应的空间自动回收（指针回退）。

```
Stack Pool:
┌─────────────────────────────────────────┐
│ Frame 0 locals │ Frame 0 stack │ Frame 1 locals │ Frame 1 stack │ ...
│ (main)         │ (main)        │ (Foo)          │ (Foo)         │
└─────────────────────────────────────────┘
                                          ↑ pool_top
```

这个设计和操作系统的线程栈在精神上是一样的——一块连续内存，LIFO 分配。区别在于这是解释器自己管理的"虚拟栈"，和操作系统的 native 栈完全独立。

### frame stack

InterpFrame 通过 `parent` 指针形成调用链。MachineState 维护当前线程的栈顶帧指针（top frame），新的方法调用在栈顶推入一帧，返回时弹出。

frame stack 也参与异常处理——当异常抛出时，解释器从当前帧开始向 parent 方向遍历，寻找匹配的 catch 子句。这就是 CLR 的"第一遍扫描"（first pass）。找到匹配的 catch 后，执行 finally 子句（第二遍），然后跳转到 catch handler。

### 线程亲和性

LeanCLR Universal 版是单线程设计，所以 MachineState 是一个全局单例，不需要处理线程同步。

Standard 版（多线程）需要每个线程一个 MachineState 实例——eval stack pool 和 frame stack 都是线程私有的。这和 CoreCLR 的 Thread 结构（每个线程有自己的 alloc context 和 frame chain）在设计上一致。

LeanCLR 选择 Universal 版作为主推版本，很大程度上是因为单线程大幅简化了 MachineState 的实现——不需要 TLS（Thread Local Storage），不需要线程安全的 pool 分配，不需要跨线程的栈遍历。这些在 CoreCLR 中都是极其复杂的工程问题。

## 与 HybridCLR HiOpcode 的架构对比

把 LeanCLR 的双解释器和 HybridCLR 的单级 transform 放在一起，是理解两种设计哲学的最佳方式。

| 维度 | HybridCLR | LeanCLR |
|------|-----------|---------|
| transform 级数 | 1 级（CIL → HiOpcode） | 2 级（MSIL → HL-IL → LL-IL） |
| IR opcode 总量 | ~1000+（单层但高度特化） | 182 + 298 = 480（分层但更精简） |
| 设计哲学 | 尽量在一步里做完所有优化 | 分层渐进，每层只做一类变换 |
| 优点 | 单次 transform 成本低 | 中间层可独立优化、调试 |
| 缺点 | opcode 爆炸（1000+）、维护成本高 | 两次 transform 有额外开销 |

### 为什么 HybridCLR 有 1000+ 条 opcode

HybridCLR 的 `HiOpcodeEnum` 定义了超过 1000 个指令。这不是设计失误，而是"单层 transform + 极致特化"策略的必然结果。

以 `ldfld`（加载实例字段）为例。MSIL 中只有一条 `ldfld`，但 HybridCLR 需要同时做语义归一化和类型特化，结果是：

```
ldfld → LdFldVarSize_i4       // 字段偏移 < 256, 值类型 int32
        LdFldVarSize_i8       // 字段偏移 < 256, 值类型 int64
        LdFldVarSize_f4       // 字段偏移 < 256, 值类型 float
        LdFldVarSize_f8       // 字段偏移 < 256, 值类型 double
        LdFldVarSize_obj      // 字段偏移 < 256, 引用类型
        LdFldFixedSize_i4     // 字段偏移已知, 固定大小 int32
        // ... 还有更多变体
```

每种字段类型、偏移编码、值类型/引用类型的组合都对应一条 HiOpcode。同一语义乘以类型变体数量，1000+ 就是这么来的。

### 为什么 LeanCLR 用 480 条就够了

LeanCLR 把工作分两步：

第一步（HL）：`ldfld` 归一化为 `HL_LDFLD`，不管字段类型。HL-IL 层不关心类型。

第二步（LL）：`HL_LDFLD` 按字段类型展开为 `LL_LDFLD_I4`、`LL_LDFLD_I8`、`LL_LDFLD_R4`、`LL_LDFLD_R8`、`LL_LDFLD_REF`。只做类型特化，不做编码优化。

因为每一层只做一类变换，所以 opcode 数量是**加法关系**（182 + 298 = 480），而不是 HybridCLR 的**乘法关系**（语义数 x 类型变体数 x 编码变体数 = 1000+）。

### 哪种策略更好

没有绝对的答案，取决于约束条件。

HybridCLR 的约束是：它运行在 IL2CPP 内部，transform 性能很重要（影响首次调用延迟），而 transform 只做一次后缓存结果。所以"一步到位"的策略虽然增加了 opcode 维护成本，但减少了运行时开销。

LeanCLR 的约束是：它是一个独立运行时，可维护性和可移植性比单次 transform 性能更重要。分层设计让每一层的实现更简单，更容易在新平台上移植和调试。如果将来需要加一个优化 pass（比如常量折叠或死代码消除），只需要在 HL 和 LL 之间插入一层，不需要改动现有的两层。

从编译原理的角度看，LeanCLR 的分层策略更接近传统编译器的设计——前端（HL Transform）做语义分析和归一化，后端（LL Transform）做目标特化和指令选择。HybridCLR 的单层策略更像一个"macro expansion"——直接把 CIL 指令展开为特化实现。

## 完整执行链路：从方法调用到指令执行

把三级 transform 和执行循环串起来，一个方法从被调用到实际执行的完整链路是：

```
1. 调用触发
   Interpreter 收到一个 RtMethodInfo（来自 LEAN-F2 的 metadata 解析）

2. 检查 transform 缓存
   如果这个方法已经被 transform 过，直接用缓存的 LL-IL 指令流

3. 首次 transform（如果缓存未命中）
   3a. BasicBlockSplitter 切分方法体 MSIL 为基本块
   3b. HLTransformer 做栈模拟 + 语义归一化 → 生成 HL-IL
   3c. LLTransformer 做类型特化 + 索引烘焙 → 生成 LL-IL
   3d. 缓存 LL-IL 指令流到 RtMethodInfo

4. 帧构建
   从 MachineState 的 pool 分配 InterpFrame
   设置 ip = LL-IL 指令流起始位置
   设置 locals、args、stack 的基地址

5. 执行
   进入 Interpreter::execute 的 switch dispatch loop
   逐条执行 LL-IL 指令

6. 返回
   弹出 InterpFrame，回退 pool 指针
   将返回值写入调用者的 eval stack
```

第 3 步的 transform 只在方法首次被调用时执行。后续调用直接走第 2 步的缓存路径。这和 HybridCLR 的策略一样——transform 一次，执行多次。

第 3d 步的缓存机制意味着 transform 的两次开销在长期运行中会被摊销。对于热路径方法（被频繁调用的方法），transform 成本可以忽略不计。

## 收束

LeanCLR 的 `interp/` 目录用 31,829 行 C++ 实现了一个完整的三级 transform 解释器。

从架构角度看，这个设计最值得关注的不是某个单独的技术点，而是**分层策略本身**。BasicBlockSplitter 只管切块，HLTransformer 只管归一化，LLTransformer 只管特化，Interpreter 只管执行。每一层的输入输出格式明确，每一层可以独立测试和优化。

这和 HybridCLR 的"一步到位"形成了鲜明对比。两种策略没有绝对优劣，但它们体现了两种不同的工程判断：HybridCLR 优化的是运行时效率（减少 transform 次数），LeanCLR 优化的是工程效率（降低单层复杂度）。

从 opcode 数量看也能印证这一点：HybridCLR 用 1000+ 条 opcode 换来单层 transform，LeanCLR 用 182 + 298 = 480 条 opcode 实现了两层 transform。总 opcode 数不到 HybridCLR 的一半，但 transform 多了一遍。

对于想理解"解释器的 IR 设计有哪些选择"这个问题，LeanCLR 和 HybridCLR 放在一起看是目前最好的学习材料——它们来自同一团队，面对同一份规范，做了两种截然不同的设计决策。

## 系列位置

- 上一篇：[LEAN-F2 Metadata 解析]({{< ref "leanclr-metadata-parsing-cli-image-module-def" >}})
- 下一篇：[LEAN-F4 对象模型]({{< ref "leanclr-object-model-rtobject-rtclass-vtable" >}})
