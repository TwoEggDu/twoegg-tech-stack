---
date: "2026-04-13"
title: "HybridCLR 桥接篇｜ABI 与跨边界调用：为什么 interpreter 调 AOT 需要一层 bridge"
description: "从平台 ABI 约定到解释器 StackObject 布局，解释 MethodBridge 存在的根本原因：两个世界的参数传递规则不兼容，必须有一层显式搬运。"
weight: 33
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "ABI"
  - "MethodBridge"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> MethodBridge 不是一种优化策略，而是两套参数传递规则之间的物理翻译层：平台 ABI 把参数放在寄存器里，解释器把参数放在连续内存里，这两件事不可能自动兼容。

这是 HybridCLR 系列的桥接专题篇，插在工具链（HCLR-3）和 MonoBehaviour（HCLR-4）之间。

在前面几篇文章里，MethodBridge 已经出现过很多次——HCLR-1 的 runtime 主链里提过它，HCLR-3 的工具链里讲了它的生成流程，后面的排障篇和 AOT 泛型篇还会反复用到它。但到目前为止，有一个最基础的问题一直没有正式展开：

`ABI 到底是什么，为什么它会导致 interpreter 和 AOT 之间必须有一层桥接。`

这篇文章专门回答这个问题。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. ABI 到底是什么，它和 API 的边界在哪。
2. 平台 ABI（以 ARM64 为例）怎么规定参数传递。
3. HybridCLR 解释器的参数布局长什么样。
4. 这两种布局为什么不可能自动兼容。
5. MethodBridge 的 stub 函数到底在做什么搬运工作。

读完这篇，再回看系列里所有出现 MethodBridge 的地方，应该都能立刻知道它为什么存在、缺了会怎样。

## 为什么要单独讲 ABI

因为在这个系列里，MethodBridge 几乎每篇都会出现，但"ABI"这个词始终只被当成已知概念带过。

HCLR-1 说它是"interpreter / AOT / native 三个世界之间的 ABI 桥"。HCLR-3 说它是"build-time 生成、runtime 真消费的桥接代码"。后面的排障篇会说"缺了 bridge 报 NotSupportedException"。再后面的性能篇和 DHE 篇还会在不同语境下提到它。

但如果读者不清楚"ABI"到底约定了什么，这些句子就只能当结论记。

所以这篇的定位很简单：把 ABI 这个概念补到位，让后面所有涉及 MethodBridge 的讨论都能回到同一个基础上。

## 什么是 ABI

ABI 的全称是 Application Binary Interface。

很多人容易把它和 API 搞混。区分只需要记住一件事：

- **API** 是源码层面的约定。函数叫什么名字、参数类型是什么、返回什么类型——这些信息在编译前就确定了，编译器靠它做类型检查。

- **ABI** 是二进制层面的约定。编译完成之后，参数到底放在哪些寄存器、放在栈的哪个位置、返回值从哪里取、栈帧怎么对齐——这些规则在源码里看不到，但 CPU 执行时必须遵守。

换句话说，API 管的是"函数签名长什么样"，ABI 管的是"这个签名编译成机器码之后，参数和返回值的物理位置在哪"。

两个函数的 API 可以完全一样，但如果调用方和被调用方各自按不同的 ABI 规则放参数，调用就会崩溃——不是逻辑错误，而是读到了错误的内存位置或寄存器。

## ARM64 AAPCS 举例

ARM64 平台上通用的调用约定叫 AAPCS64（Procedure Call Standard for the Arm 64-bit Architecture）。这里只列和 MethodBridge 直接相关的几条核心规则：

**整型参数：** 前 8 个整型（或指针）参数，按顺序放在通用寄存器 `x0`、`x1`、`x2` ... `x7` 里。超出 8 个的部分放栈上。

**浮点参数：** 前 8 个浮点参数，按顺序放在浮点寄存器 `d0`、`d1`、`d2` ... `d7` 里。超出 8 个的同样放栈上。

**整型和浮点各自计数。** 一个函数如果签名是 `void Foo(int a, float b, int c, float d)`，那么 `a` 放 `x0`，`b` 放 `d0`，`c` 放 `x1`，`d` 放 `d1`。整型和浮点各走各的寄存器序列，互不干扰。

**返回值：** 整型返回值放 `x0`，浮点返回值放 `d0`。

**struct 传递：** 小 struct（16 字节以内）可能直接放寄存器；大 struct 由调用方分配内存，把指针通过 `x8` 传给被调用方，被调用方往那块内存里写。

这些规则不是建议，而是硬约定。编译器生成的每一条 `bl`（branch with link）指令，都默认对方按这个规则放好了参数。如果参数不在预期的物理位置，CPU 读到的就是垃圾数据。

## 解释器的参数布局

HybridCLR 的解释器不走寄存器约定。

它的参数传递用的是一块连续内存——具体来说，是一个 `StackObject` 数组。每个 `StackObject` 是一个 8 字节的 union，可以存 `int32_t`、`int64_t`、`float`、`double`、指针等各种基本类型。

当解释器准备调用一个方法时，参数按声明顺序依次排在 `localVarBase` 数组的开头位置：

```
localVarBase[0]  →  第 1 个参数（不管是 int 还是 float）
localVarBase[1]  →  第 2 个参数
localVarBase[2]  →  第 3 个参数
...
```

这是一个完全扁平的内存布局。不区分整型和浮点，不区分寄存器和栈，所有参数按顺序排在连续的 8 字节槽位里。返回值也写回同一种 `StackObject` 结构。

这种设计对解释器本身来说是合理的：解释器需要统一的方式访问任意类型的参数，而且它的"栈帧"就是一块 C++ 分配的内存，根本不涉及硬件寄存器分配。

## 两个世界的冲突

到这里，冲突就很清楚了。

假设热更代码要调用一个 AOT 编译好的函数：

```csharp
// AOT 侧的函数，已经被 IL2CPP 编译成 native code
static float Calculate(int id, float factor, int count)
```

IL2CPP 编译出来的 native 代码，严格遵守 ARM64 AAPCS：

- `id` → `x0`（整型寄存器）
- `factor` → `d0`（浮点寄存器）
- `count` → `x1`（整型寄存器）
- 返回值 → `d0`（浮点寄存器）

但解释器这边，三个参数是这样排的：

- `localVarBase[0]` → `id`（int，8 字节槽）
- `localVarBase[1]` → `factor`（float，8 字节槽）
- `localVarBase[2]` → `count`（int，8 字节槽）

如果解释器直接用函数指针调用 native 函数，会发生什么？

C++ 编译器会按照 ABI 规则把 `localVarBase[0]` 的值放进 `x0`，但它根本不知道 `localVarBase[1]` 应该放 `d0` 而不是 `x1`。因为从 C++ 的视角看，解释器只是在操作一块 `void*` 大小的内存数组，编译器无法从中推断出"第二个参数其实是 float 类型、应该走浮点寄存器"这种信息。

结果就是：`factor` 的值被放进了 `x1`（整型寄存器），而 `d0`（浮点寄存器）里是未初始化的垃圾值。native 函数从 `d0` 读到的不是 `factor`，而是上一次残留在浮点寄存器里的随机数据。

这不是"精度丢失"或"值不太对"，而是彻底的内存语义错乱。

## MethodBridge 到底在做什么

MethodBridge 的每一个 stub 函数，做的就是这层"物理搬运"。

一个典型的 bridge stub 长这样（伪代码，简化自 `MethodBridge.cpp` 中的真实生成物）：

```cpp
// 签名：返回 float，参数是 (int, float, int)
static void __M2N_r4_i4_r4_i4(
    const MethodInfo* method,
    uint16_t* argVarIndexs,
    StackObject* localVarBase,
    void* ret)
{
    // 1. 从解释器的 StackObject 数组里，按正确的类型读出每个参数
    int32_t arg0 = *(int32_t*)(localVarBase + argVarIndexs[0]);
    float   arg1 = *(float*)(localVarBase + argVarIndexs[1]);
    int32_t arg2 = *(int32_t*)(localVarBase + argVarIndexs[2]);

    // 2. 用强类型的函数指针调用 native 函数
    //    C++ 编译器看到 arg0 是 int、arg1 是 float、arg2 是 int，
    //    就会自动按 ABI 把它们放进 x0、d0、x1
    typedef float (*NativeMethod)(int32_t, float, int32_t, const MethodInfo*);
    float result = ((NativeMethod)method->methodPointer)(arg0, arg1, arg2, method);

    // 3. 把返回值写回解释器的返回值位置
    *(float*)ret = result;
}
```

这个 stub 函数就是一个翻译器。它知道三件事：

1. 每个参数在 `StackObject` 数组里的偏移和类型
2. native 函数期望的 C 签名是什么样的
3. 返回值应该用什么类型写回去

当 C++ 编译器编译这个 stub 时，它看到的是一个正常的强类型函数调用——参数类型明确，编译器自然会按 ABI 把 `int` 放 `x0`/`x1`，`float` 放 `d0`。这一步不需要手写汇编，编译器自己就能做对。

MethodBridge 的核心价值就在这里：**它把解释器的"类型无关的连续内存"翻译成编译器能理解的"强类型参数列表"，让编译器替它完成 ABI 适配。**

## 为什么签名不同就需要不同的 stub

因为不同签名的参数走的物理位置完全不同。

以两个只差一个参数类型的函数为例：

```csharp
void Foo(int value)    // value → x0（整型寄存器）
void Bar(float value)  // value → d0（浮点寄存器）
```

从 API 角度看，这两个函数长得差不多——都只有一个参数，都没有返回值。

但从 ABI 角度看，`Foo` 的参数在 `x0`，`Bar` 的参数在 `d0`，搬运逻辑完全不同。所以它们需要两个不同的 stub：

```cpp
// Foo 的 stub：从 StackObject 读 int，放进整型参数位置
static void __M2N_v_i4(...) {
    int32_t arg0 = *(int32_t*)(localVarBase + argVarIndexs[0]);
    ((void(*)(int32_t, const MethodInfo*))method->methodPointer)(arg0, method);
}

// Bar 的 stub：从 StackObject 读 float，放进浮点参数位置
static void __M2N_v_r4(...) {
    float arg0 = *(float*)(localVarBase + argVarIndexs[0]);
    ((void(*)(float, const MethodInfo*))method->methodPointer)(arg0, method);
}
```

参数越多、类型组合越丰富，需要的 stub 数量就越多。这就是为什么 `MethodBridge.cpp` 通常有几千行甚至上万行——它要覆盖当前项目里出现过的所有参数签名组合。

HybridCLR 的生成器按签名 hash 给每个 stub 命名和索引。runtime 启动时，`InitMethodBridge()` 把这些 stub 注册进一张哈希表。解释器每次需要跨边界调用时，用目标方法的签名查表，找到对应的 stub，再通过 stub 完成参数搬运和调用。

## 缺了 bridge 会怎样

如果某个方法签名在桥接表里找不到对应的 stub，runtime 会走到一个兜底逻辑，直接抛出异常：

```
NotSupportedException: method call bridge missing: ...SignatureString...
```

注意，这里的表现是 **NotSupportedException**，不是崩溃，也不是静默返回错误值。

这和 AOT 泛型缺失的表现完全不同。AOT 泛型缺失时，常见的表现是 `SIGSEGV`（段错误）或栈溢出——因为 IL2CPP 在找不到泛型实例时，可能走到一个未初始化的代码路径。

两者都会导致"HybridCLR 跑不起来"，但它们坏的层次完全不同：

| 问题 | 表现 | 根因 |
|------|------|------|
| MethodBridge 缺失 | `NotSupportedException` | 跨边界调用找不到签名匹配的搬运 stub |
| AOT 泛型实例缺失 | `SIGSEGV` / 栈溢出 | IL2CPP 在 AOT 阶段没生成该泛型实例的代码 |

所以在排障时，看到 `NotSupportedException` 且消息里带 bridge missing，第一反应应该是检查 `MethodBridge.cpp` 是否重新生成过，而不是去查 AOT 泛型或补充 metadata。

## Reverse bridge：AOT 调 interpreter 的反向问题

前面讲的都是 interpreter → AOT 方向的调用。但在实际项目里，反向调用同样存在，而且触发场景更多。

什么时候 AOT 代码会调用 interpreter 方法？至少有这几种常见场景：

- **delegate / event**：AOT 代码持有一个 delegate，delegate 的目标方法在热更程序集里。AOT 代码调用 delegate 时，控制流从 native 进入 interpreter。
- **virtual override**：AOT 基类定义了 virtual 方法，热更子类 override 了它。AOT 代码通过基类引用调用 virtual 方法时，实际执行的是 interpreter 里的 override 实现。
- **interface 实现**：AOT 代码通过接口引用调用方法，而接口的实现类在热更程序集里。

这些场景的共同点是：AOT 侧的调用者不知道自己要调用的是 interpreter 方法。它按正常的 ABI 传参，把参数放在寄存器里，期望被调用方也是一个 native 函数。

但被调用方实际上是解释器。解释器需要从寄存器里把参数"搬"回 `StackObject` 数组，执行完之后再把返回值"搬"回寄存器。

这就是 reverse bridge 做的事。它的方向正好和 M2N（Managed-to-Native）bridge 相反：

- **M2N bridge**：StackObject → 寄存器 → 调用 native 函数
- **N2M bridge**（reverse bridge）：寄存器 → StackObject → 调用 interpreter → 返回值写回寄存器

两个方向需要的 stub 各自独立生成，各自按签名索引。生成器在分析时会同时扫描两个方向的需求，最终统一输出到 `MethodBridge.cpp` 里。

## 收束

MethodBridge 存在的根本原因，不是性能优化，不是框架设计偏好，而是一个物理事实：平台 ABI 把参数按类型分散到不同的寄存器序列里，解释器把参数按顺序排在连续的内存槽位里，这两种布局在二进制层面不兼容。

每一个 bridge stub 做的事情都很简单：从一种布局读参数，按另一种布局写参数，然后调用目标函数。但因为每种参数签名需要一个独立的 stub，所以必须在构建期静态生成，而且必须覆盖项目里所有出现过的签名组合。

理解了这一点，系列里后续出现 MethodBridge 的地方——生成流程、缺失报错、性能影响、DHE 的处理方式——就都能回到同一个基础上。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}">HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}">HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化</a>
