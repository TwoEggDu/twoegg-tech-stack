---
title: "ECMA-335 基础层索引｜规范 + 最小桥接的 14 个核心主题"
slug: "ecma335-series-index"
date: "2026-04-15"
weight: 8
series: "dotnet-runtime-ecosystem"
series_id: "ecma335-index"
tags: [ECMA-335, CLR, Index]
---

这 14 篇文章定位为"规范 + 最小桥接"：以 ECMA-335 规范为骨架，必要时引入最小的 runtime 实现示例（IL2CPP / HybridCLR）来锚定抽象概念，避免读者只读规范而无法将概念映射到代码。

A0~A6 以规范层为主；A1/A2/A7 引入 IL2CPP/HybridCLR 视角的桥接段；A8~A13 补齐 verification、custom attributes、P/Invoke、security、threading 内存模型、PE 文件格式 6 个原本缺失的规范层主题。纯规范读者可跳过桥接段。

> **本文明确不展开的内容：**
> - 各 runtime 的具体实现细节（在 B/C/D/F 模块展开）
> - 横向对比同一概念在 5 个 runtime 里的差异（在 G 模块展开）

## 推荐阅读顺序

| # | 文章 | 一句话定位 |
|---|------|-----------|
| A0 | [术语约定：runtime、toolchain、metadata、execution engine 的边界]({{< relref "engine-toolchain/ecma335-terminology-conventions.md" >}}) | 统一全系列的核心术语边界，消除歧义 |
| A1 | [CLI Metadata 基础：TypeDef、MethodDef、Token、Stream]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) | metadata 表和 token 的编码规则，所有 runtime 解析 DLL 的起点 |
| A2 | [CIL 指令集与栈机模型：ldloc、add、call]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) | 规范定义的指令集和栈机语义，JIT/AOT/解释器的共同输入 |
| A3 | [CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) | 类型系统的规范定义，理解各 runtime 类型加载差异的基准 |
| A4 | [CLI Execution Model：方法调用约定、虚分派、异常处理模型]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}}) | 调用约定和异常模型的规范层定义 |
| A5 | [CLI Assembly Model：程序集身份、版本策略与加载模型]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}}) | 程序集的身份标识与版本绑定规则 |
| A6 | [CLI Memory Model：对象布局、GC 契约与 finalization 语义]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) | 对象布局和 GC 契约的规范约束，各 runtime GC 实现的共同约束 |
| A7 | [IL2CPP 泛型共享规则：引用类型共享 object，值类型为什么不能]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) | 泛型共享的规范基础，对比各 runtime 的共享策略差异 |
| A8 | [CLI Verification：IL 验证规则与 type safety 的运行时边界]({{< relref "engine-toolchain/ecma335-cli-verification-il-type-safety.md" >}}) | Verifiable / Unverifiable / Invalid 三层级，unsafe 与 calli 为什么走 unverifiable |
| A9 | [Custom Attributes 与 Reflection 元数据编码]({{< relref "engine-toolchain/ecma335-custom-attributes-reflection-encoding.md" >}}) | CustomAttribute 表 + blob 二进制编码 + 反射读取路径 |
| A10 | [P/Invoke 与 Native Interop：marshaling 的规范层定义]({{< relref "engine-toolchain/ecma335-pinvoke-native-interop-marshaling-spec.md" >}}) | ImplMap 表 + MarshalingDescriptor + Calling Convention 三层规范 |
| A11 | [CLI Security：Strong Name、CAS 与现代演进]({{< relref "engine-toolchain/ecma335-cli-security-strong-name-cas.md" >}}) | Strong Name 密码学标识、CAS 规范与废弃、现代 .NET 安全替代 |
| A12 | [Threading 内存模型：volatile、原子操作与内存屏障]({{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}}) | CLI 内存模型核心承诺、3 层 barrier、与 C++/Java 内存模型对比 |
| A13 | [CLI File Format：PE 头、CLI 头与 metadata 物理布局]({{< relref "engine-toolchain/ecma335-cli-file-format-pe-cli-header.md" >}}) | PE → CLI Header → Metadata Root → Streams → Tables → Method Body 五层嵌套（基础层完结篇） |

## 读完后可以进入

- [CoreCLR 实现分析索引]({{< relref "engine-toolchain/coreclr-series-index.md" >}}) — .NET 主线 runtime，JIT 路线的标杆实现
- [Mono 实现分析索引]({{< relref "engine-toolchain/mono-series-index.md" >}}) — Unity 第一代 runtime，理解 IL2CPP 转型的必经坐标
- [IL2CPP 实现分析]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) — Unity 当前 AOT runtime，从 D1 架构总览开始
- [LeanCLR 调研报告]({{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}) — 轻量级独立 CLR，从 F1 调研报告开始
- [横切对比 G1~G9]({{< relref "engine-toolchain/runtime-cross-series-index.md" >}}) — 同一概念在 5 个 runtime 中的不同实现决策
