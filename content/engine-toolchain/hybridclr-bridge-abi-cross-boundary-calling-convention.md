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
hybridclr_version: "runtime v9.0.1 (83929787) / package v9.0.3"
lastmod: "2026-10-25"
---

热更方法调用 AOT 方法时，参数已经在解释器的槽位里，目标 native 函数却按平台 ABI 接收参数。要让一次调用成立，需要同时知道方法身份、参数类型、存储位置和实际入口。MethodBridge 负责把这些信息接起来。

> **阅读位置**：在[工具链篇]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})之后、[调用链篇]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})之前阅读。指针、布局和类型访问的语言基础见 [CXX-06]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})。

> **版本**：社区源码固定为 runtime v9.0.1 `83929787581bce6c7219418f62348a10c33582ee`、package v9.0.3。ARM64 段用于解释 ABI，既有 Player 实验实际运行于 Windows x64；两者不能互作平台实测。

## 先区分三种约定

| 层 | 约定什么 | 不能替代什么 |
|---|---|---|
| 托管方法签名 | 声明类型、参数和返回类型、泛型上下文 | 平台寄存器和栈上的传参位置 |
| 解释器表示 | 参数所在槽位、结构体占用空间、返回值存储 | native 函数所需的 C++ 函数类型 |
| 平台 ABI | 参数分类、寄存器与栈分配、返回值、对齐 | 方法是否存在、是否已经保留或补充方法体 |

桥接器从解释器表示中取值，通过与目标匹配的函数类型发起调用。C++ 编译器据此生成满足目标 ABI 的调用序列。它不需要在每个 stub 中手写寄存器搬运汇编。

## ARM64：参数与返回值分开看

以下是基本 AAPCS64 下普通、非可变参数调用的教学例子。规则来自 [Arm AAPCS64 的参数传递与返回值章节](https://github.com/ARM-software/abi-aa/blob/main/aapcs64/aapcs64.rst#parameter-passing)；具体平台还有自己的约定扩展。

对于 `float Calculate(int id, float factor, int count)`，32 位整数使用 `w0`、`w1`，单精度参数使用 `s0`，返回的 float 也在 `s0`。这些分别是通用寄存器和 SIMD/浮点寄存器的对应宽度视图；double 使用 `d` 视图。普通标量参数的两组寄存器各自分配，寄存器不足时还会使用栈。

结构体不能只按“是否超过 16 字节”一句话概括：

- 满足条件的 HFA/HVA（同质浮点／向量聚合）有专门的寄存器规则，优先于普通聚合的大小判断。
- 普通大聚合**作为参数**时，可先复制到调用方内存，再把指向副本的指针作为参数分配；这个指针遵循参数规则。
- 无法按返回规则放进寄存器的聚合**作为返回值**时，由调用方提供结果缓冲，其地址通过 `x8` 传入。

`x8` 在这里承担间接结果地址，不能写成所有大结构体参数都通过 `x8` 传递。实际 IL2CPP 生成签名还可能带 `this`、`MethodInfo*` 等参数，应对照本次产物确认，不能直接用托管签名猜全部 native 参数位置。

## 解释器的参数是槽位，不是硬件寄存器

本版 [InterpreterDefs.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterDefs.h) 定义 `StackObject`。它是 8 字节的存储单元；`localVarBase` 是 `StackObject*`。

因此 `localVarBase + index` 按槽寻址，字节距离为 `index * sizeof(StackObject)`。一个标量可能只使用槽内的一部分字节；较大的值类型需要多个槽，不能认为每个参数固定占一个槽。实例方法的 `this`、结构体复制和各调用路径的参数重排也要分别处理。

解释器调用点用参数索引找到当前帧中的实参。只有在特定的小例子里，三个标量才恰好可画成连续的 0、1、2；实际桥接读取应遵循参数索引和生成的类型信息。

```text
当前帧的若干槽
  ├─ argIndex[0] → int 参数
  ├─ argIndex[1] → float 参数
  └─ argIndex[2] → int 参数
       ↓ 按签名取值
具有匹配参数类型的 native 调用
       ↓ 按返回类型保存
解释器返回值存储
```

这里的虚拟槽位仍位于内存中。寄存器式 IR 不意味着每个槽都对应一个 CPU 寄存器。

## stub 怎样把类型信息交给编译器

下面只描述动作，不是可直接替换生成物的 C++：

```text
读取目标调用约定和方法信息
按 argIndex[0] 读取 int32
按 argIndex[1] 读取 float32
按 argIndex[2] 读取 int32
用与目标生成签名匹配的函数类型调用 native 入口
按 float32 返回类型写回结果
```

如果用不匹配的函数指针类型调用，行为不受保证；不能预言它一定把第二个值放进某个错误寄存器。正确的强类型 stub 才使编译器能够完成参数分类和 ABI 适配。

桥接按生成器归一化后的签名组织；托管类型名不同不一定要求独立 stub，ABI 等价的签名可能复用。应关注目标平台的签名分类及生成范围，而不是按业务方法数量估算桥接数量。

固定版本入口是 package 的 [Editor/CodeGen/MethodBridge/Generator.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/CodeGen/MethodBridge/Generator.cs)，以及 runtime 的 [InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp) 中 `InitMethodBridge` 和桥接查找逻辑。生成物、运行时与目标平台必须配套。

## 两个方向，以及原生插件回调

| 方向 | 需要接起的表示 | 常见触发 |
|---|---|---|
| interpreter → AOT | 解释器槽位 → native 调用参数 → 返回值 | 热更方法调用主包函数 |
| AOT → interpreter | native 调用参数 → 解释帧 → native 返回值 | 委托、虚方法、接口分派 |
| 外部 native → 托管入口 | 外部 ABI → 受支持的回调 wrapper → 后续分派 | 原生插件、SDK 回调 |

第三种不能只用“反向 bridge”一词带过。Reverse P/Invoke wrapper 还涉及回调声明、线程登记、异常边界和委托寿命；随后可能进入 N2M 桥。已验证的正常路径见[线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

## 报错时按失败位置查

找不到 bridge、找不到 AOT 泛型实现、调用入口不匹配，是不同问题，但不能只按异常类型机械分类。版本与路径不同，异常消息也可能变化。

1. 保存完整异常、目标签名和实际调用栈，确认失败在桥接查找、泛型实现选择还是进入目标后。
2. 桥接缺失时，核对生成时与最终构建的目标、Development 配置和实际编译的生成文件。
3. AOT 泛型缺少可用实现时，检查补充方法体、可用共享代码和保留需求；它可以表现为 `MissingMethodException`，不能默认推成 SIGSEGV 或栈溢出。
4. native crash 另查 ABI、对象寿命、越界及符号可靠性，不能仅凭崩溃类型证明缺 bridge 或缺实例。

[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})已经给出同一调用在失败、native 与补充方法体解释路径之间的对照。桥接负责表示转换，方法实现的可用性仍须独立确认。

**理解一次跨边界调用，要同时核对方法身份、解释器槽位、native 签名和实际进入的路径。**

## 系列位置

- 前置：[工具链与生成物]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})、[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})
- 继续：[调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})
- 返回：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
