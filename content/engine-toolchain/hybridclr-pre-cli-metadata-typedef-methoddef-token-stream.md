---
slug: "HybridCLR 前置篇｜CLI Metadata 基础：TypeDef、MethodDef、Token、Stream 到底是什么"
title: "ECMA-335 基础｜DLL 与 CLI Metadata：文件入口、表、堆、Token 和方法体"
date: "2026-04-13"
lastmod: "2026-10-23"
description: "从 PE 到 CLI Header、metadata 表与堆，再到 token 和方法体，建立 IL2CPP 与 HybridCLR 共同需要的 DLL 阅读基础。"
weight: 10
featured: false
tags: ["Unity", "IL2CPP", "HybridCLR", "ECMA-335", "Metadata"]
series: "HybridCLR"
series_order: -2
---

热更 DLL、用来补充 AOT 元数据的 DLL、IL2CPP 构建输入，都可能是托管程序集。但文件同为 DLL，不意味着它们在流水线中承担同一职责。想判断缺了什么，首先要知道文件里实际存了什么。

本篇是 ECMA-335 主线的第一篇，同时保留 HybridCLR Pre-A 的旧地址。原文件格式篇中的基础定位路径已合并到这里，完整结构留在 [PE／CLI 附录]({{< relref "engine-toolchain/ecma335-cli-file-format-pe-cli-header.md" >}})。

## 先区分描述和执行步骤

一个常规托管 DLL 至少要描述两类信息：

- **Metadata**：声明了哪些类型、方法和字段，它们叫什么、是什么类型，又引用了谁。
- **方法体**：用 CIL 表达操作顺序，并附带局部变量和异常处理所需的信息。

没有签名和引用，`call` 的操作数只是一个数字；没有方法体或其他实现，方法描述本身也不能完成计算。抽象方法、P/Invoke 方法等不要求在当前 DLL 中拥有普通的 IL 方法体。

IL2CPP 会在构建期消费这些信息，再生成自己的 runtime 数据和 native 代码。**global-metadata.dat 不是原 DLL 的 #~ stream，也不能当作普通 CLI 文件解析。** 这层区别后续看 [IL2CPP 产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。

## 从文件入口找到 metadata

常规 CLI 映像使用 PE 容器。跨平台 runtime 可以自行解析这些字节，使用 PE 格式不意味着必须经 Windows 系统加载器运行。

```text
PE Header 与节表
  → Optional Header 的 CLI 数据目录（索引 14）
  → CLI Header
      → Metadata 目录的 RVA、Size
      → Metadata Root
          → Stream headers
          → 表流（常见为 #~）和各个 heap
```

这里有三种位置，不能混用：

| 位置 | 相对什么 |
|---|---|
| 文件偏移 | 文件起始字节 |
| RVA | 映像的虚拟基址 |
| stream offset | Metadata Root 的起点 |

对于位于某节原始数据范围内的 RVA，文件偏移按节表换算：

```text
fileOffset = PointerToRawData + (RVA - Section.VirtualAddress)
```

例如节的 VirtualAddress 为 0x2000，PointerToRawData 为 0x200，RVA 为 0x2050，则文件偏移为 0x250。这个例子只演示换算；实际解析还需校验节边界和文件长度，不能假设任何 RVA 都有对应文件字节。

这一步既用于定位 metadata，也用于定位 MethodDef 指向的方法体。**RVA 不是可以直接拿去 Seek 的文件偏移。**

## 表负责关系，堆负责变长数据

下表是常见的流名，不是要求每个文件恰好有五个 stream：

| Stream | 内容 | 索引方式 |
|---|---|---|
| #Strings | 类型名、方法名、命名空间等 UTF-8 标识符 | 字节偏移 |
| #US | ldstr 使用的用户字符串 | 用户字符串 token 携带堆偏移 |
| #Blob | 签名、常量、特性参数等二进制数据 | 字节偏移，blob 带长度编码 |
| #GUID | 模块标识等 GUID | 从 1 开始的条目索引 |
| #~ | 常见的 metadata 表流 | 表号与行号 |

还可能遇到 `#-` 等表流形式。表行中的 heap index 宽度取决于格式标志，不能一律按 4 字节处理。

TypeDef 的 Name 列并不存整个名字，而是存 #Strings 中的位置。MethodDef 的 Signature 列也不是 C# 文本，而是 #Blob 中的签名数据。表把这些信息连接成结构。

## 先记住七张表

| 表 | 含义 |
|---|---|
| TypeDef（0x02） | 当前模块定义的类型及其成员范围 |
| MethodDef（0x06） | 方法名、签名、标志和方法体 RVA |
| Field（0x04） | 字段名、签名和标志 |
| TypeRef（0x01） | 带解析作用域的类型引用 |
| MemberRef（0x0A） | 成员的声明者、名字与签名 |
| TypeSpec（0x1B） | 用签名表示的类型形状，如泛型实例和数组 |
| MethodSpec（0x2B） | 泛型方法引用及其方法实参 |

Field 是规范表名，工具或实现中也可能使用 FieldDef 这样的名称。TypeRef／MemberRef 常用于外部引用，但不能把它们定义成“只能跨程序集”。定义、引用与泛型绑定的完整链路在 [签名解析篇]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}}) 展开。

## Token 只能在所属模块中解释

表 token 是 32 位值：高 8 位为表号，低 24 位为从 1 开始的行号。例如 `0x06000042` 表示当前模块 MethodDef 表的第 66 行。

两个模块都可以包含这个数字；重新编译也可能改变行号。因此日志中只留下 token，往往无法复现解析问题。应同时保留构建产物身份、模块和需要的泛型上下文。

这里讨论的是表 token。用户字符串 token 等有其他编码用途；metadata 表中的 coded index 也不是直接存完整 token。不要把所有“索引”都套进同一个位运算。

## 从 MethodDef 找到方法体

对普通 IL 方法，位置关系是：

```text
MethodDef.RVA
  → 按 PE 节表换算文件偏移
  → Tiny／Fat method header
  → IL bytes
  → 如存在，按对齐规则继续读取 EH 等额外数据节
```

Tiny header 为 1 字节，适用于代码小于 64 字节、无需局部变量签名和额外数据节等满足限制的方法，隐含 maxstack 为 8。常见 Fat header 为 12 字节，解析时应读取其大小字段，不能把所有头都硬编码为 12 字节。

局部变量签名本身存放在 metadata 中，由 Fat header 的 LocalVarSigTok 经 StandAloneSig 引用；它不是直接跟在 IL 后面的一块局部变量数组。异常处理子句则可以出现在方法体额外数据节中。

## 动手验证：一个加法方法的三种表示

```csharp
public static class MathHelper
{
    public static int Add(int a, int b) => a + b;
}
```

用 Release 配置编译并通过 IL 工具观察。其计算核心可写成：

```il
ldarg.0
ldarg.1
add
ret
```

这几条指令没有声明参数的类型。类型来自方法签名；Add 这个名字来自 MethodDef 对 #Strings 的引用；方法体位置来自 RVA。它们共同描述了这次计算。

检查步骤：

1. 找到 MathHelper 的 TypeDef，以及 Add 的 MethodDef。
2. 读取 Signature，确认两个 int32 参数和 int32 返回值。
3. 读取 RVA，通过节表换算文件偏移，检查实际 method header。
4. 按 header 中的代码长度读取 IL；不要把调试符号文件当成 metadata 的来源。

指令偏移、token、RVA 和是否使用 Tiny header 都应以本次产物为准。Debug 构建可能增加局部变量、nop 或跳转，不能靠示意图猜测真实字节。

## 对 HybridCLR 排障有什么帮助

热更 DLL 提供自身类型和方法的描述及 IL。补充 AOT 元数据的 DLL 为某些 AOT 泛型路径提供所需的原始信息，但必须与对应主包的产物关系一致。具体版本匹配规则见 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。

当运行时报告“找不到方法”时，先问它发生在哪一层：文件不能解析、引用不能绑定、方法体缺失，还是执行路径不可用。知道数据分别在哪里，才能避免把所有错误都归结为“少了一个 DLL”。

规范查阅：[ECMA-335 第六版](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II 的 metadata、签名和物理布局章节。

**Metadata 给操作命名和定型，IL 给出操作步骤；必须把模块、表、堆和方法体连起来，才能读懂一个 DLL。**

## 系列位置

- 上一篇：[术语导读]({{< relref "engine-toolchain/ecma335-terminology-conventions.md" >}})
- 下一篇：[类型系统与泛型语义]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
