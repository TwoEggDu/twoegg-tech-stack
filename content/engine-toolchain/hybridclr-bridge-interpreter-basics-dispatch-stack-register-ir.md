---
title: "HybridCLR 桥接篇｜解释器基础：dispatch loop、stack vs register、IR 是什么"
date: "2026-04-13"
description: "从 CIL 求值栈到虚拟槽位，理解转换、分派、方法体缓存和解释执行之间的边界。"
weight: 61
featured: false
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "Interpreter"
  - "Architecture"
  - "Bridge"
series: "HybridCLR"
hybridclr_version: "runtime v9.0.1 (83929787)"
lastmod: "2026-10-25"
---

CIL 用求值栈描述计算，打开 HybridCLR 执行器却看到 `localVarBase`、槽索引和专门处理整数的指令。两者之间经过了什么转换？理解这一步，才能读懂解释器首次调用、缓存和跨边界参数搬运。

本篇接在 [CIL 与栈机模型]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}})之后，在[原理总览]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})与[调用链]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})之前按需阅读，不必等到商业优化专题才补这些概念。

> **版本与示例**：实现入口固定到 runtime v9.0.1 `83929787581bce6c7219418f62348a10c33582ee`。下面的短指令序列用于比较模型，不冒充特定构建逐字输出；实际转换结果应结合目标方法和固定版本观察。

## 一次执行需要哪些状态

解释器至少需要知道当前方法的指令、下一条指令的位置、操作数存储，以及如何调用和返回。字节码解释器可用下面的简化循环理解：

```text
取出当前位置的指令
根据指令种类选择处理逻辑
读取操作数，完成计算或控制转移
更新下一条指令位置
直到返回、抛异常或进入其他执行路径
```

真实执行器还处理栈帧切换、异常展开和 AOT 调用。执行完一条普通算术指令时按其编码长度推进；分支指令使用目标位置。不能假定所有指令都占一个字节，或每一步都只执行 `ip++`。

固定版本的 [Interpreter_Execute.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/Interpreter_Execute.cpp) 使用 switch 分派内部指令。这里执行的是转换后的指令流，CIL 是上游输入。

## 栈机和寄存器式表示，差在操作数怎样指定

CIL 的求值栈是语义模型。以下示例表示把两个局部值相加后保存：

```text
ldloc.0       将 a 的值放上求值栈
ldloc.1       将 b 的值放上求值栈
add           消耗两个操作数，产生一个结果
stloc.2       将结果写入 c
```

转换器可以静态模拟栈的类型和位置，把隐式的数据流改成显式的槽访问。对应的概念表示可以写成：

```text
add_i4 dst=c, left=a, right=b
```

它展示了消除部分栈搬运和分派的机会，并不保证所有 CIL 四指令序列最终都压成一条。控制流合流、值复制、别名以及具体优化阶段都可能影响最终结果。

“寄存器式”中的寄存器是**虚拟位置**，通常仍由内存保存；它不等于 CPU 寄存器分配。栈式字节码的实现也可以缓存栈顶或优化栈操作，因此不能只按指令集形式给出固定性能倍数。

## StackObject 槽索引与字节偏移

[InterpreterDefs.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterDefs.h) 定义 8 字节的 `StackObject`，执行器中的 `localVarBase` 是 `StackObject*`。对使用槽索引的指令：

```text
地址 = localVarBase 的起始地址 + 槽索引 × sizeof(StackObject)
```

索引 3 表示从基址前进 3 个槽，在该表示中即 24 字节。读取 i4 或 f4 时只解释对应位置上的 4 字节；大结构体可能跨多个槽。具体参数和局部值的槽数由类型与布局决定。

```text
方法帧：参数区域 | 局部值区域 | 求值栈对应的临时区域
```

转换后，许多计算直接读取已确定的槽，省去逐条维护原始 CIL 求值栈的动作；执行时仍有方法帧、调用与临时存储。“消除了原始 push/pop”不能理解成运行时没有栈或不再需要 GC 根。

还要区分三种数字：

| 数据 | 单位或含义 |
|---|---|
| 指令中的某个槽操作数 | `StackObject` 索引 |
| `ip` 推进和指令编码位置 | 指令流中的字节位置 |
| 立即数、分支目标、类型或方法引用 | 各自指令格式规定的含义 |

并非所有操作数都是 `uint16_t` 槽索引。应先找到 [Instruction.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/Instruction.h) 中的指令结构，再对照执行分支；不要看到 `offset` 就默认单位是字节。

## 转换发生在运行时，结果可以复用

IR 是中间表示：它承接上游语义，也可以成为解释器直接消费的表示。这里可按三步阅读：

```text
方法体中的 CIL
  → 模拟求值栈、解析类型与引用、转换和优化
  → 内部寄存器式指令
  → Interpreter::Execute 执行
```

本版 [Transform.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/Transform.cpp) 与 [TransformContext.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/TransformContext.cpp)承担转换。普通按需路径在首次需要该方法时准备；预热也可以提前触发。它属于运行时的方法准备，不应写成“transform-time 与 runtime 无关”。

另有两种缓存需要分开：

- 原始 IL `MethodBody` 的缓存，帮助读取与转换，见 [MethodBodyCache.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/MethodBodyCache.cpp)。
- 方法的已转换解释信息，由执行路径复用，涉及 `MethodInfo` 的 `interpData`。

[首次／再次调用实验]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})观察到同一 Add 转换一次、执行两次。缓存命中减少的是准备工作，后续仍须执行指令，也不会自动变成 native JIT。

## 优化术语怎样落回代码

| 概念 | 一般含义 | 需要核对什么 |
|---|---|---|
| 类型特化 | 已知操作数类型后选择专用指令 | 该版本有哪些指令，何时选中 |
| 搬运消除与指令合并 | 保持语义时减少临时复制或分派 | 别名、控制流与目标类型是否允许 |
| 窥孔优化 | 在局部序列中识别可替换模式 | 实际模式与前置条件 |
| 常量折叠 | 对已知常量提前计算 | 是否保持溢出、异常等语义 |
| 分支简化 | 删除无效跳转或不可达路径 | 目标及异常处理区域是否合法 |
| 内联 | 把被调方法的表示并入调用者 | 可内联条件、体积与递归限制 |

这张表定义通用术语，不声称每种优化都在所有 HybridCLR 版本中实现。具体支持范围与源码入口见[指令优化专题]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}})。[泛型实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})还展示了补充方法体被内联的实际路径：有解释执行，不一定有被调方法的独立解释帧。

## switch、computed goto 与 JIT 放在哪一层比较

switch 和 computed goto 是两种指令分派组织方式。后者可把分派点放在各处理分支尾部，使用 GCC/Clang 的标签地址扩展；是否改善分支预测、代码大小和吞吐，要看编译器、CPU 与工作负载。扩展说明见 [GCC Labels as Values](https://gcc.gnu.org/onlinedocs/gcc/Labels-as-Values.html)。本文不保留无样本来源的“快 15%～30%”结论。

JIT 则是在运行时生成 native 实现的执行策略，不是和 switch 等价的第三种 dispatch 写法。它还需要代码生成、平台执行权限和缓存管理等条件。HybridCLR 社区版的这条路径仍是解释执行。

商业文档只公开部分优化能力时，只能据此讨论能力和测量条件；不能由“分派优化”推断其已经采用 computed goto，更不能把这种推断写进事实对照表。

**先确认指令表示、操作数单位和准备时机，再讨论执行开销与优化收益。**

## 系列位置

- 前置：[CIL 与栈机模型]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}})
- 继续：[原理总览]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})、[ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})
- 深入：[指令优化专题]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}})
- 返回：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
