---
title: "ECMA-335 基础｜CLI File Format：PE 头、CLI 头与 metadata 在文件中的布局"
slug: "ecma335-cli-file-format-pe-cli-header"
date: "2026-04-15"
weight: 21
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: [ECMA-335, CLR, FileFormat, PE, Binary]
---

> 一个 .NET DLL 本质上是一个 PE 文件里塞了 CLI metadata。理解 PE header → CLI header → metadata streams 的物理布局，才能理解为什么 Assembly.Load、LoadMetadataForAOTAssembly、il2cpp.exe 读的都是同一份二进制。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 14 篇，也是基础层完结篇。

前面 A0~A12 讨论的都是抽象规范：CIL 指令语义、类型系统分类、程序集身份、执行模型、内存模型、threading 模型。这些讨论都在"规范层"展开——不涉及具体字节如何排列在文件里。这一篇把视角压到最低，落到字节层面：一个 `.dll` 或 `.exe` 在磁盘上是怎么组织的，runtime 打开它时从哪里开始读。

> **本文明确不展开的内容：**
> - 完整的 PE 格式规范（Microsoft PE/COFF Specification）
> - Portable PDB 格式（独立于 CLI 规范）
> - ReadyToRun (R2R) 格式（CoreCLR 扩展，不在 ECMA-335）

## PE 文件的顶层结构

ECMA-335 Partition II §25.1 定义了 CLI 文件的物理格式——它必须是一个合法的 PE（Portable Executable）文件，并在某个特定的数据目录项里放一个指向 CLI header 的指针。

PE 文件的顶层嵌套结构如下：

```
DOS Header + DOS Stub
PE Signature ("PE\0\0")
COFF File Header
Optional Header (PE32 或 PE32+)
  └─ Data Directories[16]
     └─ CLI Header RVA + Size (第 15 项，index 14)
Section Headers
Sections (.text / .rsrc / .reloc 等)
```

每一层的作用：

**DOS Header + DOS Stub**——历史遗留。所有 PE 文件开头都有一段 DOS 兼容头，如果在 DOS 下运行会打印"This program cannot be run in DOS mode"后退出。DOS Header 的最后 4 字节（偏移 `0x3C`）存放 PE Signature 的文件偏移。

**PE Signature**——固定 4 字节 `"PE\0\0"`，用于确认这是一个合法的 PE 文件。紧接其后是 COFF File Header。

**COFF File Header**——20 字节，包含目标机器类型（x86 / x64 / ARM64）、section 数量、时间戳、符号表信息、可选头大小、Characteristics 标志位。

**Optional Header**——名字叫 optional 但对 PE 可执行文件必需。分 PE32（32 位，224 字节）和 PE32+（64 位，240 字节）两种。尾部是一个 16 项的 Data Directories 数组。

**Data Directories**——16 个 `{RVA, Size}` 对，每项指向一个特殊的表：导入表、导出表、资源表、重定位表等。**第 15 项（数组下标 14）是 CLI Header Directory**——这是 .NET DLL 区别于 native DLL 的唯一标志。native DLL 这一项全为 0；.NET DLL 必须指向一个合法的 CLI Header。

**Section Headers + Sections**——PE 把实际内容分散到多个 section 里。典型的 .NET DLL 至少有 `.text`（代码+metadata）、`.rsrc`（Win32 资源）、`.reloc`（基址重定位）三个 section。CLI Header 和 metadata 都位于 `.text` section 内。

## CLI Header 的结构

ECMA-335 Partition II §25.3.3 定义了 CLI Header 的 72 字节（`0x48`）定长结构：

```
cb (header 长度, 固定 0x48, 4 字节)
MajorRuntimeVersion + MinorRuntimeVersion (各 2 字节)
MetaData (RVA + Size, 8 字节, 指向 metadata root)
Flags (4 字节)
EntryPointToken (4 字节, 可选)
Resources (RVA + Size, 8 字节)
StrongNameSignature (RVA + Size, 8 字节)
CodeManagerTable (RVA + Size, 已弃用)
VTableFixups (RVA + Size)
ExportAddressTableJumps (RVA + Size, 已弃用)
ManagedNativeHeader (RVA + Size, 已弃用)
```

关键字段：

**MajorRuntimeVersion / MinorRuntimeVersion**——CLR 运行时版本要求。规范定义 `2.5` 为最低值。runtime 加载前会检查这个字段，版本不匹配直接拒绝。

**MetaData RVA + Size**——最核心的字段。指向 metadata root 在 PE 文件中的位置。runtime 读取这个 RVA 后，接下来所有的 metadata 解析都基于 metadata root 展开。

**Flags**——bitmap 标志位：
- `IL_ONLY` (0x01)：只包含 IL 代码，没有 native stub
- `32BIT_REQUIRED` (0x02)：必须以 32 位进程加载
- `STRONGNAMESIGNED` (0x08)：含强名称签名
- `TRACKDEBUGDATA` (0x10000)：含调试信息

**EntryPointToken**——对可执行文件（`.exe`），这是 `Main` 方法的 metadata token（4 字节编码：高 1 字节表选择器，低 3 字节行号）。DLL 这一字段为 0。

**VTableFixups**——native interop 使用。托管代码被 native 代码通过函数指针调用时，需要 vtable fixup 来处理调用约定的转换。

**CodeManagerTable / ExportAddressTableJumps / ManagedNativeHeader**——历史遗留字段，现代 .NET 已弃用。新 runtime 读到非零值会忽略或报错。

## Metadata Root 的结构

ECMA-335 Partition II §24.2.1 定义了 metadata root——所有 metadata 访问的入口：

```
Signature (4 字节, 固定 0x424A5342 = "BSJB")
MajorVersion + MinorVersion (各 2 字节)
Reserved (4 字节, 必须为 0)
Length (4 字节, Version 字符串长度)
Version (变长字符串, 如 "v4.0.30319", 4 字节对齐)
Flags (2 字节)
Streams (2 字节, stream 数量)
StreamHeader[] (变长数组)
```

**Signature `0x424A5342`**——四字节魔数，ASCII 为 `"BSJB"`。这是 metadata root 的识别标志。runtime 打开 DLL 后，先跳到 CLI Header 指定的 metadata RVA，检查前 4 字节是否是 `BSJB`，不是则判定为损坏文件。

**Version 字符串**——CLR 版本的字符串描述，如 `"v4.0.30319"`、`"Standard CLI 2005"`。runtime 记录这个字段用于调试和兼容性判断。

**StreamHeader[]**——每项包含 stream 在文件中的 offset（相对 metadata root）、size 和 name（以 null 结尾的字符串，4 字节对齐）。

CLI metadata 定义了 5 个标准 stream（A1 metadata 文章已展开）：

- `#~`（或 `#-`）：metadata tables，所有类型/方法/字段定义的结构化数据
- `#Strings`：标识符字符串堆（类名、命名空间、方法名等）
- `#US`：user string 堆（代码里的 `"hello"` 字面量）
- `#GUID`：GUID 堆（module version id、interface id 等）
- `#Blob`：签名和常量的二进制数据堆

一个 DLL 可能只出现其中一部分——没有 user string 字面量的 DLL 可以不包含 `#US` stream。`#~` 和 `#-` 二选一，前者是优化过的压缩格式，后者是未压缩格式（编辑场景下使用）。

## `#~` stream 的内部结构

ECMA-335 Partition II §24.2.6 定义了 `#~` stream 的二进制布局：

```
Reserved (4 字节, 必须为 0)
MajorVersion + MinorVersion (各 1 字节)
HeapSizes (1 字节, bitmap)
Reserved (1 字节, 必须为 1)
Valid (8 字节, 64-bit bitmap)
Sorted (8 字节, 64-bit bitmap)
Rows[] (4 字节 × 存在的表数量)
Tables[] (按表编号顺序存放行数据)
```

**HeapSizes**——一个 bitmap，三个 bit 分别指示 `#Strings`、`#GUID`、`#Blob` 堆的索引宽度：0 表示 2 字节索引，1 表示 4 字节索引。小程序集用 2 字节索引省空间，大程序集（堆大小超过 64K）必须用 4 字节索引。

**Valid bitmap**——64 位，每一位对应一张 metadata 表。bit `n` 为 1 表示第 `n` 张表存在。ECMA-335 定义了 45+ 张表，常用的包括：

| 表编号 | 表名 | 作用 |
|--------|------|------|
| 0x00 | Module | 模块信息 |
| 0x01 | TypeRef | 跨程序集类型引用 |
| 0x02 | TypeDef | 本程序集类型定义 |
| 0x04 | Field | 字段定义 |
| 0x06 | MethodDef | 方法定义 |
| 0x08 | Param | 参数定义 |
| 0x09 | InterfaceImpl | 接口实现关系 |
| 0x0A | MemberRef | 跨程序集成员引用 |
| 0x0B | Constant | 常量字面量 |
| 0x0C | CustomAttribute | 自定义属性 |
| 0x14 | Event | 事件定义 |
| 0x17 | Property | 属性定义 |
| 0x20 | Assembly | 程序集自身描述 |
| 0x23 | AssemblyRef | 依赖程序集引用 |
| 0x2A | GenericParam | 泛型参数 |

**Sorted bitmap**——指示哪些表已按主键排序。排序的表可用二分查找快速定位。

**Rows[]**——每个存在的表一个 4 字节的行数。

**Tables[]**——所有行的实际数据，按表编号升序排列。每张表的行格式在 Partition II §22 逐表定义。

## Coded Index 编码

ECMA-335 Partition II §24.2.6 定义了 coded index——metadata 表之间外键引用的省空间编码方式。

某些列的取值可以指向多张表中的一张。例如 `CustomAttribute` 表有个 `Parent` 列，可以指向 22 种不同的表之一（TypeDef、MethodDef、Field 等）。如果每个 parent 都用 `{表编号, 行号}` 两个字段存，会占 8 字节。coded index 把它压缩到 2 或 4 字节：

- **低若干位**：表选择器（tag）。例如 `TypeDefOrRef` 有 3 个候选表，需要 2 个 tag bit（0=TypeDef, 1=TypeRef, 2=TypeSpec）。
- **高位**：对应表的行号。

**编码宽度决定**：根据候选表中行数最大的那张决定。假设 `TypeDefOrRef` 的三张表中最大行数是 `N`，tag 需要 2 bit，那么行号需要 `ceil(log2(N))` bit。如果 `(N << 2)` 能放进 2 字节（即 `N <= 16383`），coded index 就用 2 字节；否则用 4 字节。

这就是为什么 `#~` 的 header 必须同时包含 `HeapSizes`（堆索引宽度）和 `Rows[]`（各表行数）——parser 需要这两部分信息才能推算出每列的字节宽度，才能正确按字段边界解析每一行。

换言之：**metadata 表没有自描述性。必须先读 header，算出列宽，才能读行**。这也是为什么 metadata 解析器比 JSON 解析器复杂得多。

## Method Body 的存储位置

ECMA-335 Partition II §25.4 定义了 method body（IL 代码体）的物理布局。

MethodDef 表的每一行有一个 RVA 列，指向该方法的 method body 在 PE 文件中的起始位置。method body 有两种格式：

**Tiny format**——给短方法用。

```
第 1 字节：flags + size (高 6 位是 IL 字节数, 低 2 位是 0x02)
随后若干字节：IL 指令
```

适用条件：IL 代码不超过 63 字节、没有局部变量、没有异常处理、栈深不超过 8。满足条件的方法用 tiny format 只花 1 字节 header。

**Fat format**——给其他方法用。

```
12 字节 header:
  Flags + HeaderSize (2 字节, 高 4 位 HeaderSize=3 表示 12 字节, 低 12 位 flags)
  MaxStack (2 字节, 求值栈最大深度)
  CodeSize (4 字节, IL 字节数)
  LocalVarSigTok (4 字节, 局部变量签名 token, 指向 StandAloneSig 表)

IL 指令 (CodeSize 字节, 4 字节对齐)

Exception Clauses (可选):
  每条 clause 描述 try-catch-finally 的覆盖范围、handler 类型、过滤器等
```

Flags 中的 `CorILMethod_InitLocals` 标志位决定局部变量是否初始化为零——这直接对应 C# 编译器生成的 `.locals init` 指令。

A1 的 metadata 文章讲过 `(image, token) → MethodBody` 的查找逻辑：通过 token 定位到 MethodDef 表的某一行，读取 RVA，跳到 method body 起始位置，解析 header，得到 IL 指令的起始位置和长度。这一节给出的是这套查找机制背后的物理层定义——每一字节在文件里的具体位置。

## 各 runtime 的 PE 解析实现

同一份 PE 格式，五个 runtime 的解析路径各有侧重：

**CoreCLR**——`PEDecoder` 类（`src/coreclr/inc/pedecoder.h`）负责读取整个 PE 结构。CoreCLR 把 PE 文件内存映射到进程，通过 `PEDecoder::GetCorHeader()` 访问 CLI Header，`GetMetadata()` 访问 metadata root。支持 NGen 和 ReadyToRun 等扩展格式。

**Mono**——`MonoImage` 结构（`mono/metadata/image.c`）持有完整的 PE 映射。`mono_image_load_pe_data()` 解析 PE 头，`mono_image_load_cli_data()` 解析 CLI header 和 metadata streams。Mono 把每个加载的程序集都包装成一个 `MonoImage`，作为 metadata 访问的基础句柄。

**IL2CPP**——构建时 `il2cpp.exe`（`il2cpp/libil2cpp/vm/MetadataCache.cpp`）读取 PE 文件、提取所有 metadata，转换成 C++ 源码 + `global-metadata.dat`。运行时不再有 PE 解析——所有 metadata 已经被烘焙成静态的 C++ 数据结构和一份二进制 metadata 文件。这是 AOT 路线放弃动态加载换取的代价。

**HybridCLR**——热更 DLL 走完整 PE 解析路径。`Assembly::LoadFromBytes`（`hybridclr/metadata/Assembly.cpp`）接收一段字节流 → 验证 PE 头 → 读取 CLI header → 提取 metadata streams → 建立本地的 metadata 索引结构。HybridCLR 需要和 IL2CPP 的静态 metadata 桥接，这也是 HybridCLR 代码复杂度集中的地方。

**LeanCLR**——`PeImageReader` 只提取 CLI header 和五个 stream，不做完整 PE 解析。不支持 Win32 资源、不支持重定位表、不支持 PE 导入表。目标是把最小化二进制体积的 loader 塞进嵌入式场景。

## 工程影响

**DLL 体积分析**——用 dnSpy、ildasm 或 CLI Sharp 查看一个 .NET DLL 的 stream 大小分布，可以判断哪些部分可被 stripping 减小：
- `#Strings` 过大：方法名、类名没做混淆；可以通过混淆器裁掉调试信息
- `#Blob` 过大：签名和自定义属性占比高；可以用 ILLink（Linker）裁掉未使用的特性
- method body 占比过大：IL 本身多，考虑裁减未使用方法

**HybridCLR 热更**——热更 DLL 必须是完全标准的 PE + 标准 CLI header。任何一层缺失或损坏都会报 `LoadImageErrorCode: BAD_IMAGE`。常见原因：
- DLL 被第三方混淆器处理过（某些混淆器破坏了 PE 签名）
- Roslyn 编译选项设置错误（生成了非标准格式）
- DLL 传输过程中字节丢失（解压错误、编码错误）

**IL2CPP 构建调试**——`il2cpp.exe` 报 PE 解析错误时，优先排查：
- .NET 编译器版本（旧版本可能生成略有差异的 metadata 格式）
- DLL 是否被第三方工具修改（protobuf-net、Fody 等）
- 程序集是否混用了不同版本的编译器

**逆向保护**——所有 CLI metadata 在 PE 文件里明文可读，dnSpy 之类的反编译器可以无障碍读出所有类型和方法定义。想做保护有两条路：
- 混淆：重命名标识符、重排方法体、插入死代码
- 加密：对 metadata streams 做 XOR 或 AES 处理，runtime 加载时解密

HybridCLR 商业版的加密方案属于后者——对 `#~`、`#Strings`、`#Blob` 等 stream 做对称加密，runtime 用 key 解密后再走标准解析路径。

## 收束

PE 格式看似枯燥，但它是所有 .NET 工具链——编译器、runtime、反编译器、profiler、分析器——的共同输入。理解这一层的五层嵌套：

```
PE 文件
  └─ CLI Header (Data Directory[14])
     └─ Metadata Root ("BSJB")
        └─ #~ Stream (+ #Strings / #US / #GUID / #Blob)
           └─ Metadata Tables (45+ 张)
              └─ Method Body RVA → Tiny / Fat format
```

就能把 A0~A12 的所有规范讨论落回到具体字节层面：CIL 指令不是抽象符号，是 method body 的字节序列；类型系统不是抽象分类，是 TypeDef 表的行结构；程序集身份不是抽象概念，是 Assembly 表的列值加 `#Strings` 中的字符串。

这是 ECMA-335 基础层的自然终点——从抽象规范回到物理存储。

基础层（A0~A13）至此完结。A0 约定术语、A1 讲 metadata 结构、A2 讲 CIL 指令、A3 讲类型系统、A4 讲执行模型、A5 讲程序集、A6 讲内存模型、A7 讲 verification、A8 讲 custom attributes、A9 讲 calling convention、A10 讲 P/Invoke、A11 讲 security、A12 讲 threading，再到这一篇的 file format——每一篇都定义了后续 runtime 实现分析需要反复回引的规范边界。

从下一篇开始，进入 runtime 实现模块。同一份 ECMA-335 规范在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五种实现里各走了不同的工程路径，而所有的路径分叉都起点于对这份规范的不同权衡。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}}">A12 CLI Threading 内存模型</a>
- 下一篇：进入 runtime 实现模块。推荐入口：
  - <a href="{{< relref "engine-toolchain/coreclr-architecture-overview-dotnet-run-to-jit.md" >}}">CoreCLR 架构总览</a>
  - <a href="{{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}">IL2CPP 架构总览</a>
  - <a href="{{< relref "engine-toolchain/leanclr-survey-architecture-source-map.md" >}}">LeanCLR 调研报告</a>
