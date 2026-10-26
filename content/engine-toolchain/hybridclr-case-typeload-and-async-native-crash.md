---
slug: "hybridclr-真实案例诊断从-typeloadexception-到-async-栈溢出一次完整的-native-crash-符号化分析"
date: "2026-03-26"
title: "HybridCLR 真实案例诊断｜从 TypeLoadException 到 async native crash 的历史取证"
description: "保留一次 TypeLoadException 与 async native crash 的历史日志，区分观察、符号化结果和仍待复现的根因假设。"
weight: 43
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Troubleshooting"
  - "UniTask"
  - "NativeCrash"
  - "CaseStudy"
series: "HybridCLR"
hybridclr_version: "历史案例版本未完整固定；当前机制另按 v9 配套源码核对"
lastmod: "2026-10-26"
---

一次接入记录先遇到 TypeLoadException，调整 DLL 加载顺序后，又出现带有重复解释器与 async builder 帧的 native crash。这个案例最值得保留的是分层取证过程；旧文把后半段直接归因于空 RefMethods，证据不足。

> **历史记录边界**：以下日志摘自原案例记录，Player 标识为 Unity 2022.3.60f1、Android arm64、Release。原记录没有保存可供本轮复查的完整 runtime/package 提交、全部 DLL hash 与修复前后对照，因此这里不把它归为当前 v9 实验，也不宣布 async 根因已闭合。

这是系列第 14 篇。当前机制以 [泛型篇]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})为准，未证实环节的验证方案见 [案例续篇]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})。

## 第一阶段：类型解析失败

原记录包含：

```text
I Unity: Assembly [GameProto] loaded from MPQ
E Unity: Exception: System.TypeLoadException:
Could not load type 'DP.Client.EventFactory' from assembly 'DP.Client'.
  at Procedure.ProcedureLoadAssembly.LoadAssembly ()
```

当时配置把 GameLogic 放在 DP.Client 之前，而 GameLogic 引用了后者。调整后记录的顺序为：

```text
GameProto → DP.Data → DP.GenClient → DP.Client → GameLogic
```

原记录称重新构建后五个 DLL 均加载成功，TypeLoadException 消失。这支持“装载依赖与触发时机是该阶段的有效排查方向”，但还不能单凭异常断言 runtime 曾找到一份同名、被裁剪的 AOT DP.Client。热更程序集是否被错误打入 AOT，需要额外检查实际构建输入。

## 这个阶段如何复查

需要同时保存：DLL 静态引用图、加载前后日志、首次解析 EventFactory 的实际调用点、各文件 hash。

若仅改变加载顺序后问题消失，这是一组有用对照；若同时重新编译了主包、换了 DLL 或保留规则，则需要分开变量。TypeLoadException 还可能来自版本不匹配或定义被裁剪，不能统一当成顺序错误。

## 第二阶段：原生崩溃

原记录中的 crash 信息包括：

```text
Version '2022.3.60f1', Build type 'Release',
Scripting Backend 'il2cpp', CPU 'arm64-v8a'
signal 11 (SIGSEGV), code 1 (SEGV_MAPERR)
Cause: null pointer dereference
```

部分帧地址重复。按当时的符号化记录，相关函数包括：

| 地址摘录 | 记录中的符号 |
|---|---|
| 046951fc | AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted_TisIl2CppFullySharedGenericAny_TisIl2CppFullySharedGenericAny |
| 03939604 | hybridclr::interpreter::Interpreter::Execute |
| 03946358 | hybridclr::interpreter::InterpreterInvoke |
| 046978d8 | AsyncUniTaskMethodBuilder_Start_TisIl2CppFullySharedGenericAny |
| 039394f0 | hybridclr::interpreter::Interpreter::Execute |

这些地址只属于该历史二进制，不能用于当前 Player 断点。当前正文也没有完整符号包来重新确认地址映射。

## 符号化怎样形成可靠证据

使用匹配该 APK 构建与 ABI 的未裁剪符号文件，并核对模块 Build ID。可使用 Unity 配套 Android NDK 的 LLVM 工具：

```bash
llvm-addr2line -f -C -e <matching-libil2cpp.so> <module-relative-pc>
```

输入应按日志格式确认是模块相对地址，还是需要换算的运行地址。不要在已经相对化的地址上重复减 load bias。工具是否支持目标格式与架构，要实际核对，不能简单按 host 是 x86 就判断无法读取 ARM64 ELF。

符号化成功只恢复调用位置，不自动证明根因。保留原始栈、寄存器、fault address、线程与栈界，才能进一步判断递归、栈耗尽、内存访问错误或展开异常。

## 哪些结论应撤回

旧文把下面几步直接连成确定因果：

`RefMethods 为空 → AOT 泛型缺失 → FullySharedGenericAny → 无限递归 → SIGSEGV`

这条链不能由现有记录推出：

- 自动生成的 RefMethods 本来就可能全是注释；正常使用补充 metadata 不要求把它填满。
- 没有适用 native 实例时，补充原始泛型 IL 可以提供解释实现。
- FullySharedGenericAny 符号不是“无实现占位”或死循环的证明。
- 重复 native 帧提示递归等方向，但还需确认是否同一方法、状态是否推进，以及实际 fault。

因此保留“异常发生在解释器与 async builder 交界附近”的观察，撤回“空 RefMethods 已确认为根因”和“重复地址一定不是空指针”的结论。

## 接下来怎样把案例闭合

复现应固定同一 Player、热更 DLL 和 UniTask 版本，记录补充 DLL 的模式、hash、返回码，以及目标状态机和方法实例。先建立能完成的最小 async 样本，再分别改变加载与构建变量。

若怀疑补充 metadata，验证方法体来源与转换；若怀疑递归，记录同一状态机每次进入和 continuation 注册/恢复；若怀疑生成物兼容，比较实际调用入口。方案详见 [续篇]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})。

## 当前固定环境的新证据

[async／UniTask Player 实测]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md#async-unitask-player" >}})已完成 60 个普通 Player 与 12 个匹配 PDB 进程，覆盖 class／struct 热更状态机的同步完成、实际跨帧以及 await 前后异常。本样本 metadata on/off 都有可用 native builder；带 FullySharedGenericAny 名称的入口也正常完成，因此不能仅凭旧栈中的该名称或重复帧认定泛型缺失与递归。

本轮固定提交为 [b34ac10](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b34ac104fa72504d1d2a480ca936ea57a7a0fb22)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/results.json)。

新实验使用自定义 Unity 2022.3.62f3／HybridCLR v9／UniTask 2.5.10／Windows x64，与原 Android Release 现场分别记录；历史根因仍未知。

## 系列位置

通用取证流程见 [崩溃定位专题]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}})，构建一致性见 [打包工程化]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})。

**保留现场能支持的观察，让根因随对照证据收敛。**
