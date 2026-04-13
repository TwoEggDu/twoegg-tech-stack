---
title: "HybridCLR 桥接篇｜解释器基础：dispatch loop、stack vs register、IR 是什么"
date: "2026-04-13"
description: "在进入 HCLR-27 的 1000+ HiOpcode 和 transform 优化之前，先把解释器的核心循环、三种 dispatch 方式、stack machine 与 register machine 的区别、IR 的定义和常见优化术语一次讲清。"
weight: 61
featured: false
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "Interpreter"
  - "Architecture"
  - "Bridge"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> HCLR-27 会讲 1000+ HiOpcode、peephole optimization、dead branch elimination。如果不先知道"解释器一般怎么工作"，这些术语就是噪声。

这是 HybridCLR 系列的一篇桥接文章，插在 HCLR-24（回归防线）和 HCLR-25（DHE 内部机制）之间。

前面 24 篇把 HybridCLR 从 build-time 到 runtime、从 AOT 泛型到工程落地拆了一遍，但整条线都停在"架构层"——知道 transform 会把 CIL 转成内部 IR，知道解释器会执行这套 IR，但从来没有回答过一个更基础的问题：

`解释器到底是怎么工作的？CIL 栈机和寄存器 IR 到底差在哪？transform 说的"优化"到底在优化什么？`

这篇就专门回答这几个问题。不需要编译原理背景，不涉及 HybridCLR 源码细节，只把后面 HCLR-25 ~ HCLR-30 需要的概念底座先立住。

## 解释器的核心循环

所有解释器——不论是 Python、Lua、JVM 还是 HybridCLR——都遵循同一个基本循环：

```
取指令 → 解码 → 执行 → 移动到下一条 → 重复
```

用最简单的伪代码表示，一个只支持 4 条指令的解释器长这样：

```
ip = &instructions[0]          // ip = instruction pointer

loop:
    opcode = *ip               // 取指令
    switch (opcode):           // 解码 + 执行
        case LOAD_CONST:
            reg[A] = constants[B]
            ip += 1
        case ADD:
            reg[A] = reg[B] + reg[C]
            ip += 1
        case JUMP:
            ip = &instructions[target]
            goto loop
        case HALT:
            return reg[0]
    goto loop
```

这个循环有 4 个组成部分：

1. **指令指针（ip）**：指向当前要执行的指令。
2. **取指（fetch）**：从 ip 位置读出 opcode。
3. **解码+执行（decode+execute）**：根据 opcode 跳到对应的处理逻辑。
4. **推进（advance）**：把 ip 移到下一条指令，回到步骤 2。

这个循环通常叫 dispatch loop 或 eval loop。解释器的性能差异，很大程度上取决于步骤 3 的实现方式——也就是 dispatch 方式。

## 三种 dispatch 方式

### switch dispatch

最直接的实现：用一个 `switch` 语句分发所有 opcode。

```cpp
while (true) {
    switch (*ip) {
        case OP_ADD: /* ... */ break;
        case OP_SUB: /* ... */ break;
        case OP_LOAD: /* ... */ break;
        // ...数百个 case
    }
}
```

优点是结构简单、可移植性好、任何 C/C++ 编译器都能编译。

缺点是每次 dispatch 都要经过同一个 switch 跳转点。CPU 的分支预测器面对的是一个有数百个目标的间接跳转，预测准确率会下降。对于指令数量很多的解释器（HybridCLR 有 1000+ 条），这个开销不可忽略。

**HybridCLR 社区版用的就是 switch dispatch。**

### computed goto（threaded dispatch）

GCC/Clang 支持一个非标准扩展：可以把标签的地址存进数组，然后用 `goto *` 跳到任意标签。

```cpp
// 预先构建跳转表
static void* handlers[] = {
    &&handler_ADD,
    &&handler_SUB,
    &&handler_LOAD,
    // ...
};

// 初始 dispatch
goto *handlers[*ip];

handler_ADD:
    /* 执行 ADD 逻辑 */
    ip++;
    goto *handlers[*ip];    // 直接跳到下一个 handler

handler_SUB:
    /* 执行 SUB 逻辑 */
    ip++;
    goto *handlers[*ip];    // 不回 switch，直接跳
```

和 switch dispatch 的关键区别：每个 handler 末尾直接跳到下一个 handler，不需要回到一个中央 switch 点。CPU 看到的是分散在各处的间接跳转，每个跳转点的目标分布更窄，分支预测器的命中率更高。

实测通常比 switch dispatch 快 15%~30%，但代价是代码只能在支持 `&&label` 扩展的编译器上编译（GCC、Clang 支持，MSVC 不支持）。

### JIT 编译

JIT（Just-In-Time）编译器不再逐条解释执行，而是在运行时把一段指令编译成目标平台的原生机器码，然后直接执行编译出来的代码。

```
解释执行:  取指 → 解码 → 执行 → 取指 → 解码 → 执行 → ...
JIT:       编译整段 → 直接调用编译出来的函数
```

JIT 的性能上限最高，因为编译出来的就是和 AOT 编译一样的原生指令，没有 dispatch 开销。但 JIT 的实现复杂度也最高：需要为每个目标架构编写代码生成器，需要处理内存权限（可写 → 可执行），在 iOS 等平台上还被系统策略禁止。

HybridCLR 不使用 JIT。这不是技术限制（IL2CPP 本身就是 AOT 方案），而是一个有意的设计选择：HybridCLR 的目标是在 IL2CPP 已有的 AOT 框架上补一个解释器，而不是重建一个完整的 JIT。

### 三种方式的对比

```
方式             dispatch 开销    实现复杂度    可移植性
─────────────────────────────────────────────────────
switch           中等             低            高
computed goto    较低             低-中         中（GCC/Clang）
JIT              无               高            低（按平台）
```

HybridCLR 社区版选择 switch dispatch，是在可移植性和实现复杂度之间取的平衡点。商业版有可能在特定平台上使用 computed goto 来提升 dispatch 性能。

## stack machine 和 register machine

dispatch 方式决定了"怎么找到下一条指令"，而 stack machine vs register machine 决定了"操作数从哪里来、结果存到哪里去"。

### stack machine

栈机的操作数隐式地在栈顶。所有运算都是从栈顶取操作数、把结果压回栈顶。

计算 `a + b` 的过程：

```
push a      栈: [a]
push b      栈: [a, b]
add         弹出 a 和 b，计算 a+b，压入结果
            栈: [a+b]
pop result  弹出结果到 result
            栈: []
```

CIL（Common Intermediate Language）就是一种栈机指令集。C# 编译器把源码编译成 CIL 时，所有中间值都通过求值栈传递。

栈机的优点是指令非常紧凑：指令本身不需要编码操作数位置，因为操作数位置永远是"栈顶"。这让指令流很短，对内存和带宽友好。

栈机的缺点是在解释执行时，每个操作都伴随着 push 和 pop。这些栈操作本身就是开销——修改栈指针、读写栈内存——而且它们传达的信息（"从栈顶取值"）在源码层面早就知道了。

### register machine

寄存器机的操作数显式地指定位置。每条指令都说明从哪个寄存器读、往哪个寄存器写。

计算 `a + b` 的过程：

```
add r2, r0, r1    // r2 = r0 + r1，一条指令完成
```

不需要 push，不需要 pop，不需要修改栈指针。操作数位置直接编码在指令里。

寄存器机的优点是执行效率更高：没有隐式的栈操作开销，操作数直接按偏移读写。

寄存器机的缺点是每条指令更宽：需要额外的空间编码操作数位置（比如 3 个 `uint16_t` 的偏移量），指令流总体更长。

### 对比

```
                stack machine          register machine
────────────────────────────────────────────────────────
操作数位置      隐式（栈顶）           显式（偏移/寄存器号）
指令宽度        窄                     宽
指令数量        多（push/pop 多）      少（直接读写）
执行开销        栈操作                 偏移计算
典型代表        CIL, JVM bytecode      Lua 5.0+, Dalvik
```

用一个具体例子对比 `c = a + b`：

```
CIL 栈机（4 条指令）：     寄存器 IR（1 条指令）：
  ldloc.0    // push a       add [offset_c], [offset_a], [offset_b]
  ldloc.1    // push b
  add        // pop+pop+push
  stloc.2    // pop to c
```

4 条指令变 1 条。栈操作（4 次 push/pop）全部消除。这就是 HybridCLR 在 transform 阶段做栈机到寄存器转换的核心收益来源。

### HybridCLR 的选择

HybridCLR 选择了 register-style IR。所有操作数用 `uint16_t` 表示，含义是相对于 `localVarBase` 的字节偏移。方法的栈帧被压平为一段连续内存：

```
localVarBase
  |
  v
  [参数区 | 局部变量区 | 求值栈临时区]
  ^         ^            ^
  offset_0  offset_n     offset_m
```

每条 HiOpcode 指令直接按偏移读写这段内存，不做任何 push/pop。CIL 的求值栈在 transform 阶段就被彻底消除了。

## 什么是 IR

IR 是 Intermediate Representation 的缩写，翻译为"中间表示"。它是源语言和最终执行形式之间的一个过渡层。

```
源语言 ──→ IR ──→ 最终执行
```

在不同的系统中，IR 有不同的形态：

```
系统             源语言        IR                最终执行
──────────────────────────────────────────────────────────
GCC             C/C++         GIMPLE/RTL        x86/ARM 机器码
LLVM            多种          LLVM IR           x86/ARM 机器码
JVM             Java          Java bytecode     解释执行 / JIT
HybridCLR       CIL           HiOpcode 指令流   解释执行
```

IR 存在的意义是把问题分成两步：前端把各种源语言翻译成统一的 IR，后端只需要处理一种 IR。优化也集中在 IR 层做，不需要对每种源语言重复一遍。

在 HybridCLR 的语境下：

- **源语言**是 CIL（.NET 编译器产出的字节码）。
- **IR** 是 HiOpcode 指令流（HybridCLR 定义的寄存器式指令集）。
- **最终执行**是 switch dispatch 解释执行。
- **transform** 的工作就是 CIL → HiOpcode IR 的转换。

transform 不是逐条翻译。它在转换过程中会做优化：消除栈操作、特化类型、折叠指令、识别模式并替换。这些优化让最终执行的 IR 比原始 CIL 更高效。

## 常见的 IR 优化术语

HCLR-27 会详细展开 HybridCLR 社区版和商业版各自做了哪些优化。在进入那些细节之前，先把几个反复出现的术语定义清楚。

### 类型特化（type specialization）

一条多态指令展开成多条单态指令，每条只处理一种类型。

```
优化前:  add          // 运行时判断: i32? i64? f32? f64?
优化后:  add_i32      // 编译期已确定类型，运行时无需判断
         add_i64
         add_f32
         add_f64
```

收益：消除运行时类型分派。CIL 的一个 `add` 在 HybridCLR 中会变成 `BinOpVarVarVar_Add_i4`、`BinOpVarVarVar_Add_i8`、`BinOpVarVarVar_Add_f4`、`BinOpVarVarVar_Add_f8` 四条不同的指令。

### 指令折叠（instruction folding）

多条指令合成一条，前提是语义等价。

```
优化前:  ldloc.0      // 4 条 CIL 指令
         ldloc.1
         add
         stloc.2
优化后:  add [c], [a], [b]   // 1 条寄存器指令
```

栈机到寄存器的转换本身就是一种指令折叠：load/store 被折进了算术指令的操作数里。

### 窥孔优化（peephole optimization）

在一个小窗口（通常 2~4 条指令）内，用模式匹配的方式识别可优化的指令序列，并替换成更高效的等价序列。

```
窗口: [box T; brtrue target]
识别: 如果 T 是非 Nullable 值类型，box 结果必然不为 null
替换: 无条件跳转 / 直接消除
```

"窥孔"的名字来源就是这个小窗口——像通过一个小孔看指令流，每次只看几条。

### 死代码/死分支消除（dead code/branch elimination）

移除不可达的代码，或者移除跳转目标就是下一条指令的无效分支。

```
优化前:  br.s +0      // 跳转偏移为 0，目标就是紧接着的下一条
         next_instr
优化后:  next_instr   // 直接删掉无效跳转
```

### 常量折叠（constant folding）

如果一个表达式的所有操作数在编译期就已知，直接算出结果，不生成运算指令。

```
优化前:  load 3; load 4; mul    // 运行时算 3 * 4
优化后:  load 12                // 编译期已算好
```

这几个术语不是 HybridCLR 独有的概念，它们是编译器和解释器领域的通用工具箱。但在 HybridCLR 的 transform 阶段，它们被具体化为一组可审计的 pass，每个 pass 都有对应的源码入口。HCLR-27 会逐个展开。

## HybridCLR 的设计选择

把上面的概念收回 HybridCLR 的工程语境，它的技术栈可以总结为三个选择：

```
dispatch:   switch dispatch（非 computed goto，非 JIT）
IR 风格:    register-style（非 stack machine）
优化时机:   transform-time（非 runtime，非 AOT）
```

这三个选择背后是同一个工程 trade-off：**在实现复杂度和运行时性能之间取平衡**。

**选 switch dispatch 而非 computed goto**：保持 MSVC 兼容性，降低跨平台维护成本。代价是 dispatch 性能比 computed goto 低约 15%~30%。

**选 register-style IR 而非保持栈机**：CIL 本身是栈机，直接解释执行 CIL 最简单，但每条指令的 push/pop 开销在高频循环中不可接受。转成寄存器 IR 后，指令数量大幅减少，执行效率显著提升。这个转换的实现成本（transform 阶段的栈模拟和偏移计算）是一次性的，收益是永久的。

**选 transform-time 优化而非运行时优化**：所有优化都在方法第一次被调用时的 transform 阶段完成，之后每次调用直接执行优化后的 IR。这意味着首次调用有额外开销（transform），但后续调用不再为优化付费。

商业版在这三个维度上都有进一步推进的空间：

- dispatch 层面可以在支持的平台上切到 computed goto
- IR 层面可以增加更多指令特化和折叠规则
- 优化层面可以增加更多 transform pass

但社区版已经用这三个选择跑出了一个工程上可维护、性能上够用的解释器。HCLR-27 会展开社区版已有的 8 种 transform 优化和商业版追加的 4 种优化各自在做什么。

## 收束

这篇没有讲任何 HybridCLR 源码细节，只做了一件事：

把"解释器"这个黑盒拆成了可以说清楚的几个组件——dispatch loop、dispatch 方式、栈机与寄存器机、IR、常见优化术语。

读完这篇，HCLR-27 里出现的每个术语都已经有了着陆点：

- 看到 `switch (*ip)` → 知道这是 switch dispatch
- 看到 `BinOpVarVarVar_Add_i4` → 知道这是类型特化后的寄存器指令
- 看到 `localVarBase + offset` → 知道这是寄存器式寻址
- 看到 `transform` → 知道这是 CIL 到 HiOpcode IR 的转换
- 看到 `peephole` → 知道这是在小窗口内做模式匹配替换

这些概念本身不复杂。把它们先放稳，后面的 1000+ HiOpcode、差分算法和函数级分流才读得进去。

---

**系列位置**

上一篇：[HCLR-24 AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}})

下一篇：[HCLR-25 DHE 内部机制｜dhao 文件格式、差分算法与函数级分流实现]({{< relref "engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md" >}})
