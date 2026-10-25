---
title: "ECMA-335 附录｜IL 验证：合法性、类型安全与工具检查边界"
slug: "ecma335-cli-verification-il-type-safety"
date: "2026-04-15"
lastmod: "2026-10-23"
description: "供 IL 重写、unsafe 和验证工具排障按需查阅；最小栈与类型规则已并入 CIL 主线。"
weight: 16
tags: ["ECMA-335", "CIL", "Verification"]
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
primary_series: "ecma335"
series_order: 43
---

> **阅读位置**：原 A8 已退出必读顺序。求值栈、控制流合并和返回类型的基础检查，已合并到 [CIL 主线]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}})。本文用于 IL weaving、混淆、unsafe 或验证工具报错时查阅。

IL 能被某个 runtime 执行、IL 满足正确性要求、IL 可由标准规则验证为类型安全，是不同的判断。工程里若把它们混成一个“合法”，就很难解释为什么某些 unsafe 程序能运行，而某个修改后的方法被工具拒绝。

## 正确性与可验证性

| 判断 | 关注什么 |
|---|---|
| 格式与正确性 | metadata、操作数、指令和控制流是否满足相应要求 |
| 可验证性 | 能否按规定的静态规则证明类型安全 |
| 实现支持 | 当前运行时是否实现相应特性，配置是否允许执行 |

不正确的 IL 不能因为用了 unsafe 就变正确。反过来，无法通过类型安全验证也不自动表示文件损坏或一定会崩溃。工具对目标框架扩展的支持程度，还可能影响报告。

## 沿控制流检查，而不是只看每条指令

Add 这样的直线方法可以逐条推导栈类型。加入分支后，所有可达路径汇合时需要兼容的栈状态；异常处理入口也有规定的栈形状。

另外要检查：

- 参数、局部变量和 token 的引用是否合法。
- 调用参数和返回值是否与签名相容。
- 托管指针的来源、生命周期和用法是否满足要求。
- 跳转是否错误跨越异常保护区域。
- 初始化与构造过程是否满足对应规则。

单看“每个 opcode 都认识”不足以完成这些检查。

## 不要按一个关键词判定结果

### calli

calli 使用调用点签名和函数指针。不能说“只要出现 calli 就必然 unverifiable”：ECMA-335 Partition III 的 calli 条目列出了验证条件。验证器必须能够掌握目标指针的相关类型信息，并证明签名与参数相容。

C# unsafe 函数指针、任意 native 地址与可跟踪的托管方法指针不是同一种输入。尤其不要把 Marshal.GetFunctionPointerForDelegate 得到的 native 入口随意转换成托管调用约定的函数指针。

### ExplicitLayout

ExplicitLayout 本身不等于不可验证。需要看字段是否重叠、是否包含对象引用、偏移与类型规则是否满足要求。不能把 float 的 NaN 等有效位模式当成布局不合法的证明。

### 原始内存操作

原生指针运算、cpblk、initblk 等操作会引入静态验证难以保证的内存访问边界。即使程序员掌握额外前提，也不能自动说这些前提已经由 verifier 证明。

## 编译检查与完整验证的边界

C# 编译器、IL2CPP 转换器和 C++ 编译器并不执行同一套检查。C++ 能编译，只能说明生成代码通过了那一阶段的检查，不能据此证明原始 IL 已通过完整 CLI verification。

现代运行时是否执行完整验证，也不能从 CLI 规范中直接推断。更不能把“会解析 IL 和分析栈”说成“完全不检查”，或反过来把它说成“等同于安全沙箱”。

## 最小排障流程

1. 保存改写前后 DLL、目标引用程序集、工具版本与编译配置。
2. 确认错误出在格式、引用解析、验证规则还是运行时支持范围。
3. 将失败方法缩小到可复现输入；保留其签名、局部变量签名和 EH 信息。
4. 使用适配目标框架的验证工具分析，再执行目标 Player 或热更路径的实际测试。

工具检查、IL2CPP 构建成功和设备调用成功是三种证据，不要互相替代。CLI 验证也不负责检查业务授权、热更包来源或所有 native 互操作风险。

参考：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I 的类型安全与验证、Partition III 每条指令的 Correctness／Verifiability 条目。

**先区分正确性、可验证性和实现支持，再决定用什么工具验证哪一层。**

## 返回主线

- [CIL 与最小验证规则]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}})
- [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
