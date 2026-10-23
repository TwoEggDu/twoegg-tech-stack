---
title: "ECMA-335 分支｜P/Invoke 与 Native Interop：描述、转换和调用方向"
slug: "ecma335-pinvoke-native-interop-marshaling-spec"
date: "2026-04-15"
lastmod: "2026-10-23"
weight: 18
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "PInvoke", "Interop"]
---

> **阅读位置**：原 A10 保留为原生互操作分支。先完成 [调用与异常主线]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}})，在接入 SDK、原生插件或回调时阅读本文。

“有一个 native 函数地址”不等于“托管代码已经知道怎样安全地调用它”。调用还需要匹配的参数表示、调用约定和生命周期；如果 native 反过来调用托管方法，方向又发生了变化。

## 三个层次

| 层次 | 回答的问题 |
|---|---|
| metadata 声明 | 原生库与符号叫什么，托管方法签名是什么 |
| marshaling | 托管参数如何变成 native 表示，返回数据怎样处理 |
| 平台 ABI | 参数实际放入哪些寄存器／栈位置，结构体怎样传递 |

CLI 描述前两层中的信息和规则，但不会替所有平台规定相同寄存器分配。下面的规范表述以 ECMA-335 第六版为基线；HybridCLR 使用方式单独标注版本。

## ImplMap 与字段／参数描述

常规 DllImport 声明会对应 MethodDef 的 P/Invoke 标志、ModuleRef 和 ImplMap 等记录。ImplMap 在 Partition II §22.22 定义：

| 列 | 含义 |
|---|---|
| MappingFlags | P/Invoke 属性，如字符集和调用约定 |
| MemberForwarded | MemberForwarded coded index，指向相应成员 |
| ImportName | #Strings 中的原生符号名 |
| ImportScope | ModuleRef 中的原生库名 |

MemberForwarded 不是 HasFieldMarshal。调用约定的位域掩码为 0x0700，也不是 MappingFlags 的低 8 位。

FieldMarshal 表及相关 descriptor 可补充字段或参数的原生表示信息。没有显式 MarshalAs 不等于没有转换：默认 marshaling 规则同样参与。具体类型组合是否受支持，还要看目标 runtime 与平台。

## 字符串、数组与结构体必须明确表示

例如，同样写作 C# string，native 侧可能要求不同编码、是否以零结尾，以及由谁分配和释放缓冲区。数组还需要长度与输入／输出方向；结构体则要匹配字段、packing 和对齐。

不能看到“都是指针”就认为 ABI 一定兼容。也不能把 blittable 简化为“所有 struct 都可以直接传”。bool、字符、引用字段等都需要结合实际声明检查。

原生库最好通过稳定的 C ABI 暴露接口，用返回码等明确表达错误。不要依赖 C++ 异常、SEH 或 signal 自动被所有 runtime 翻译为同一种托管异常。

## 正向调用与反向回调

```text
P/Invoke：        managed → native
Reverse P/Invoke：native → managed
```

两条路径都可能需要转换，但生成的入口和准备要求不同。**ReversePInvokeWrapper 服务于反向回调，不能拿来解释普通热更代码调用 AOT extern 的路径。**

原生侧长期保存回调指针时，托管侧还需要保持相关 delegate 存活，例如由长期持有者保存引用或使用适当句柄。解除注册后再释放；一个短暂的 GC.KeepAlive 不能覆盖任意异步回调周期。

## HybridCLR 的版本边界

截至本文核对的 HybridCLR **9.x 官方文档**，推荐方式是在 AOT 程序集中定义 extern，热更代码调用这些 AOT 声明。热更程序集中直接定义 extern 的功能曾在 8.0.0～8.15.0 提供，9.0.0 起已移除。参见 [PInvoke 支持](https://www.hybridclr.cn/docs/basic/pinvoke)。

```csharp
// 放在 AOT 程序集中；原生库需在相应目标平台提供此接口。
public static class NativeApi
{
    [System.Runtime.InteropServices.DllImport("game_native",
        CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    public static extern int CalculateDamage(int amount);
}
```

热更代码调用 `NativeApi.CalculateDamage(10)`。这不是热更中随意增加主包从未准备过的原生接口；原生库交付、符号存在性、裁剪与平台支持仍需确认。

对于 native → 热更方法回调，官方另有 [MonoPInvokeCallback 与 ReversePInvokeWrapper 预留规则](https://www.hybridclr.cn/docs/basic/monopinvokecallback)。应按签名与预计新增回调数量准备，不要把这个要求套到所有正向 P/Invoke 调用上。

## 最小验收

1. 明确调用方向、完整签名、目标平台与库版本。
2. 先用整数等简单参数证明符号和基本调用成立。
3. 分别验证字符串、数组和结构体等转换；核对边界值与释放责任。
4. 回调路径增加存活期、解除注册、线程归属与异常处理验证。
5. 在实际 IL2CPP Player 和需要的热更路径运行；编辑器通过不代表目标 ABI 已验证。

完整寄存器与桥接背景继续看 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})。规范入口：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II 的 ImplMap、FieldMarshal 和 marshaling descriptor。

**先确定调用方向和数据契约，再检查转换、ABI 与生命周期，才算把托管／原生边界接起来。**

## 返回主线

- [方法调用与异常处理]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}})
- [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
