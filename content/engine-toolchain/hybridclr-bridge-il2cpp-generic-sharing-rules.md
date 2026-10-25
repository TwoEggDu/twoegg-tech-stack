---
slug: "HybridCLR 桥接篇｜IL2CPP 泛型共享规则：引用类型共享 object，值类型为什么不能"
title: "HybridCLR 桥接篇｜从泛型语义到 IL2CPP 代码共享"
date: "2026-04-13"
lastmod: "2026-10-23"
description: "区分封闭泛型身份与可执行实现，理解常规共享的适用范围、值类型的布局与 ABI 限制，以及补充元数据和 FGS 的不同职责。"
weight: 39
featured: false
tags: ["Unity", "IL2CPP", "HybridCLR", "Generics", "AOT"]
series: "HybridCLR"
primary_series: "ecma335"
series_order: 39
---

> **阅读位置**：本文保留原 A7／Bridge-D 地址，现作为 ECMA 主线通往 IL2CPP／HybridCLR 的实现桥接。先读 [泛型类型语义]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) 与 [签名解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

热更代码调用 `List<HotType>.Add` 时，类型能够被描述出来，不意味着主包已生成这次调用需要的方法实现。问题可以进一步拆开：是否有可复用的 native 代码，若没有，是否有另一条受支持的执行路径？

CLI 要求保持类型语义，但不规定每个封闭泛型都必须生成一份机器码。**泛型共享讨论的是代码复用，不是把不同封闭类型变成同一个类型。**

## 为什么引用类型常能共享

以 `List<string>` 和 `List<object>` 为例，元素存储的是对象引用。常规共享策略有机会复用处理引用的代码，同时通过运行时泛型上下文获得具体类型信息。

这不意味着泛型代码只要“把参数替换成 object”就完成了。类型检查、反射、泛型静态状态以及依赖 T 的操作，仍须保留正确语义。共享的是实现中的某些代码，不是所有运行时数据。

## 值类型为什么需要更多信息

不同值类型可能有不同大小、对齐、引用字段分布，以及平台 ABI 下的参数和返回值传递方式。

假设两个结构体大小相同，一个由整数构成，另一个由浮点字段构成。仅凭总字节数，不能证明它们在所有平台都按相同寄存器类别传参；仅凭布局相同，也不能证明包含引用时的 GC 处理相同。

因此，传统共享策略对值类型往往有更多限制。但标题式结论“所有值类型永远不能共享”不准确：枚举、泛型值类型的递归共享规则与完全泛型共享，都需要单独看。

## 共享类型与共享方法，不能混为一谈

HybridCLR 官方 [AOT 泛型说明](https://www.hybridclr.cn/docs/basic/aotgeneric)给出了当前文档中的 sharing type 计算规则。阅读时要注意它出现的位置：

| 情形 | 关键区别 |
|---|---|
| 普通引用类型作为泛型实参 | 常规规则下归约为 object |
| 普通非枚举值类型 | 不因“大小一样”就自动视为可共享 |
| 枚举 | 有与 underlying type 相关的共享规则 |
| 泛型值类型 | 可能递归计算其泛型实参的共享类型 |
| 泛型类型上的泛型方法 | 类型实参与方法实参都参与目标计算 |

例如 `ValueTuple<int, string>` 的相关共享计算不能粗略写成“它是 struct，必须保留每个引用类型实参”。现有案例 [共享类型判断]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 专门讨论这类问题；若项目版本的规则不同，应以对应版本为准。

上表是实现文档的入口，不是 ECMA-335 的规范条款。也不能把“某个类型作为参数时的归约”误读成“这个类型的所有成员方法都变成 object 的方法”。

### 推导一次 `List<ValueTuple<int, string>>.Add`

以下按本文核对的 HybridCLR 9.x 官方文档中的常规共享规则推导，未引入 FGS。假设热更代码有：

```csharp
var items = new System.Collections.Generic.List<System.ValueTuple<int, string>>();
items.Add((7, "ok"));
```

先只追 Add，不把“使用了 List”当作一个不可拆的动作：

| 步骤 | 本例结果 |
|---|---|
| 确认方法定义 | `List<T>.Add(T)`，方法自身没有泛型参数 |
| 绑定声明类型实参 | `T = ValueTuple<int, string>` |
| 计算 T 的共享类型 | ValueTuple 是泛型值类型，递归得到 `ValueTuple<int, object>` |
| 还原候选共享方法 | `List<ValueTuple<int, object>>.Add(ValueTuple<int, object>)` |
| 核对执行条件 | 对应主包中是否存在且保留了可用实现；缺失时再检查其他执行路径 |

这里最容易错的是直接得到 `List<object>.Add(object)`：外层 List 虽是 class，但我们在计算的是 **List 的成员方法使用的类型实参**。实参是一个内嵌的 ValueTuple 值，不能因为声明者 List 是 class 就把这个实参整体丢掉。

代码中还存在 List 的构造调用；找到构造函数的共享实现，不自动证明 Add 及其所需调用链已经具备。实际 AOT 生成和保留应按成员与依赖检查，不能只确认一个类型名。

上述归约也不是把业务源码改成另一种容器：`List<ValueTuple<int, string>>` 与 `List<ValueTuple<int, object>>` 仍是不同类型，原调用仍使用前者的类型身份。推导结果用于判断 native 代码是否可复用，不能用强制转换代替 runtime 的泛型处理。

如果 Add 缺少可用实现而选择补充元数据，应寻找 **`List<T>.Add` 定义所在的 AOT 程序集**，而不是只寻找 string、ValueTuple 或热更类型所在的 DLL。程序集名字以目标 Unity 产物为准。这是依据公开规则的推导；对应主包是否实际生成了该实现，仍需在指定版本的构建产物和 Player 中确认。

## 三条实现路径

| 路径 | 解决什么 |
|---|---|
| 已有的具体 AOT 实现或适用的共享实现 | 复用主包已有 native 执行路径 |
| 补充元数据支持的解释执行 | 提供所需原始泛型方法体等信息，以支持相应执行 |
| Full Generic Sharing | 用更通用的实现与额外机制扩大代码共享范围 |

不同 Unity 版本的 IL2CPP FGS 与 HybridCLR 产品中的 FGS，需要分别核对适用版本、模式和功能范围。Unity 对其方案的公开说明见 [IL2CPP Full Generic Sharing](https://unity.com/blog/engine-platform/il2cpp-full-generic-sharing-in-unity-2022-1-beta)。不能把普通共享的限制直接外推到所有 FGS 配置。

## 对 AOTGenericReferences 和保留代码的影响

工具扫描得到的泛型引用清单，需要结合具体版本理解。它列出引用，不代表“写进注释就生成了 native 实现”，也不代表文件中任何一个共享形式都适合盲目复制到所有位置。

要判断一个缺口，应先还原：

```text
声明类型 + 类型实参 + 方法定义 + 方法实参 + 参数／返回值
  → 当前版本的共享规则
  → 主包已有实现
  → 是否需要其他受支持的执行路径
```

具体写法继续看 [AOTGenericReferences、DisStripCode 与补充元数据]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})。泛型定义所在程序集与泛型实参所在程序集也可能不同，不能只按 HotType 所在 DLL 猜测需要补充什么。

## 最小验证

选定一组真实调用，在同一个 Unity／HybridCLR 版本和目标平台上，分别记录具体实参、生成或保留的 AOT 实现、补充信息与实际执行结果。把 native 路径的可用性和解释路径的正确性分开确认。

这里不提供跨版本通用的性能倍数或“一份保留模板覆盖所有泛型”的承诺。对高频调用，正确性通过之后再测量不同路径的成本。

**先确定泛型调用的完整身份，再讨论哪份代码可以共享、哪条执行路径可以补上缺口。**

## 系列位置

- 前置：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
- 深入：[IL2CPP 泛型实现]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}})
- 接续：[AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})
- 返回：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
