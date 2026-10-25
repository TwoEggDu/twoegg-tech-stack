---
title: "ECMA-335 附录｜PE、CLI Header 与 metadata 物理布局"
slug: "ecma335-cli-file-format-pe-cli-header"
date: "2026-04-15"
lastmod: "2026-10-23"
description: "供 DLL 分析和解析器排障查阅，补充 metadata stream、表宽度、coded index 和方法体布局；基础定位路径已并入主线。"
weight: 21
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "Metadata", "FileFormat", "PE"]
primary_series: "ecma335"
series_order: 42
---

> **阅读位置**：原 A13 改为查阅附录，不再作为系列完结篇。PE → CLI → metadata → 方法体的入门路径已经并入 [DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})。本文用于手工核对字节与解析器边界。

把一个 RVA 当成文件偏移，或把表内的 coded index 当作完整 token，会让后续每一步都偏离正确位置。解析失败时应先确认所在层次，再讨论是否缺少类型或方法。

## 从 PE 定位 CLI Header

常规 PE 文件中的 DOS header 给出 PE header 位置。读取 PE signature、COFF header、Optional Header 和节表后，可以找到 Optional Header 数据目录中索引 14 的 CLI 入口。

PE32 和 PE32+ 的 Optional Header 布局不同。解析器应先检查 Magic、声明大小和目录数量，再访问目录，不能把某个样本中的固定文件偏移当成格式常量。

CLI Header 的常见结构长度为 0x48，包含：

| 字段 | 作用 |
|---|---|
| cb | 头部大小 |
| MajorRuntimeVersion、MinorRuntimeVersion | CLI header 的版本信息 |
| MetaData | metadata 的 RVA 与长度 |
| Flags | IL_ONLY、32BIT_REQUIRED、STRONGNAMESIGNED 等标志 |
| EntryPointToken／EntryPointRVA | 入口信息，解释方式取决于标志 |
| Resources、StrongNameSignature | 相应数据目录 |
| CodeManagerTable、VTableFixups、ExportAddressTableJumps、ManagedNativeHeader | 保留或扩展相关字段，按对应格式解释 |

不能由版本字段直接推断“与当前运行时版本字符串不同就拒绝”，也不能把 ManagedNativeHeader 等扩展字段统一说成现代运行时必定忽略。

RVA 需要经节表转换。对于位于节原始数据范围中的位置：

```text
offset = section.PointerToRawData + RVA - section.VirtualAddress
```

先确认目标落在可读取的原始数据中，再校验偏移加长度没有溢出且没有超出文件。

## Metadata Root 与 stream header

Metadata Root 以字节 `42 53 4A 42`（ASCII BSJB）开头，随后是版本、版本字符串区域、标志与 stream 数量等信息。版本字符串及 stream 名称区域有对齐要求。

每个 stream header 提供相对 Metadata Root 的 offset、长度和名字。若文件中的 metadata root 位于 0x300，某 stream 的 offset 为 0x80，那么该 stream 的文件位置为 0x380；这里的 0x80 不是 RVA。

常见名称为 #~／#-、#Strings、#US、#GUID、#Blob。应读取实际 stream 列表，不能假定顺序、数量或偏移固定。

## 表流怎样决定每行有多宽

#~ 表流头包括：

```text
Reserved
MajorVersion、MinorVersion
HeapSizes
Reserved
Valid bitmap（64 位）
Sorted bitmap（64 位）
Rows[]（每张存在的表一个行数）
Tables[]（按表号排列）
```

HeapSizes 指示相关 heap index 使用 2 字节还是 4 字节。普通表索引的宽度取决于目标表行数；coded index 的宽度还与候选表和 tag 位数有关。

因此，不能只知道“这是 MethodDef 表”就按一个固定长度切行。需要先读 header 与行数，按列定义计算行宽，再定位每张表的数据范围。

规范各表有固定的列模式，但文件没有把每一列的名字和模式再写一遍。工具需要内置匹配的格式知识，同时校验行数与总长度。

## Coded Index 与 token 的区别

普通表 token 高 8 位是表号，低 24 位是行号。coded index 则按该列允许的候选表定义低位 tag。

例如 TypeDefOrRef 使用两个 tag 位，0、1、2 分别表示 TypeDef、TypeRef、TypeSpec。若指向 TypeRef 第 3 行，编码值为：

```text
(3 << 2) | 1 = 13
```

这是编码关系示例，不是完整 32 位 TypeRef token。表列中使用 2 字节还是 4 字节，取决于所有候选表的最大行数是否小于 `2^(16 - tagBits)`。

#Blob 签名中也会使用相关类型引用的压缩形式，读取规则与表列固定 2／4 字节表示不同。具体调用实例参见 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

## Method Body 不在表流内部

MethodDef 行的 RVA 指向方法体，而不是说明方法体嵌套存放在 #~ 内。

| 格式 | 主要信息 |
|---|---|
| Tiny | 一个头字节，高位给出代码长度；要求短代码、无局部变量签名和额外数据节等 |
| Fat | 标志与头大小、MaxStack、CodeSize、LocalVarSigTok，常见头长 12 字节 |

Tiny 的代码长度小于 64，隐含 maxstack 为 8。Fat 应按头大小字段定位 IL；IL 长度由 CodeSize 决定，不要求长度本身是 4 的倍数。若存在 EH 等额外数据节，其起始位置按要求对齐。

LocalVarSigTok 指向 StandAloneSig；局部变量类型签名最终在 #Blob 中。MoreSects 等标志决定是否继续解析额外数据节。抽象或外部实现的方法不应被强行解析成普通 IL 方法体。

## 最小排障清单

1. 验证文件完整性、PE 类别、CLI 目录和节映射。
2. 验证 metadata root、stream 范围与长度。
3. 根据行数和 heap 标志计算列宽，检查表边界。
4. 分别解码 token、表索引、coded index 与压缩签名。
5. 对方法体核对头大小、代码长度、局部变量签名和 EH 节。

在这些检查完成前，不能仅凭 BadImageFormatException 等宽泛错误就断言根因是 CPU 架构、编译器版本或某个混淆器。应保留失败文件与能定位到具体字段的证据。

对普通程序集，优先用成熟的 PEReader／MetadataReader 等工具交叉核对；本附录不是自行实现完整安全解析器的充分说明。加密容器、ReadyToRun、Portable PDB 和 IL2CPP global-metadata.dat 也不应硬套成同一种文件。

参考：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II §24、§25，以及 Microsoft [PE Format](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)。

**先把位置和编码层次读对，类型、方法与执行语义才有可靠的字节依据。**

## 返回主线

- [DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})
- [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
