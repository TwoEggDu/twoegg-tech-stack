---
slug: "leanclr-metadata-parsing-cli-image-module-def"
date: "2026-04-14"
title: "LeanCLR 源码分析｜Metadata 解析：CliImage、RtModuleDef 与 ECMA-335 表的对应关系"
description: "从 PE 文件读取到 metadata stream 解析，再到 RtModuleDef 的运行时结构构建：拆解 LeanCLR 如何用 8,603 行代码实现 ECMA-335 metadata 的完整加载链路。"
weight: 71
featured: false
tags:
  - LeanCLR
  - CLR
  - Metadata
  - ECMA-335
  - SourceCode
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> Metadata 解析是所有 CLR 实现的第一步——不管是 CoreCLR 的 PEDecoder、IL2CPP 的 RawImageBase，还是 LeanCLR 的 CliImage，它们面对的都是同一份 ECMA-335 二进制格式。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 2 篇。

LEAN-F1 建立了全景地图，给出了 9 个模块的分布和核心架构亮点。这篇下到 metadata 模块（`metadata/`，25 个文件，8,603 LOC）的实现细节，拆解 LeanCLR 如何从一个 .NET DLL 文件的二进制字节里，解析出运行时需要的所有类型、方法、字段信息。

## 从 ECMA-335 到源码

一个 .NET DLL 本质上是一个 PE/COFF 格式的可执行文件，里面嵌入了 ECMA-335 标准定义的 metadata 结构。这个结构描述了程序集中的所有类型定义、方法签名、字段布局、字符串常量等信息。

从磁盘上的 DLL 字节流到运行时可用的类型系统，需要经过四个阶段：

```
DLL 二进制
  → PE/COFF header 解析（PeImageReader）
    → metadata stream 加载（CliImage）
      → metadata table 解码（cli_metadata）
        → 运行时结构构建（RtModuleDef）
```

LeanCLR 的 `metadata/` 目录下，每个阶段都有对应的实现文件。下面按这个链路逐层拆解。

## PE 文件读取：PeImageReader

**源码位置：** `src/runtime/metadata/pe_image_reader.h`

任何 .NET 程序集（DLL 或 EXE）都是一个合法的 PE 文件。PeImageReader 的职责是从原始字节流中定位 CLI header 的位置。

PE 文件的结构在 LEAN-F1 提到的 ECMA-A1（Pre-A）中有详细描述。这里只关注 LeanCLR 的实现选择。

### 解析链路

PeImageReader 读取的关键路径是：

```
DOS header (0x3C 偏移 → PE 签名位置)
  → PE signature ("PE\0\0")
    → COFF header (机器类型、section 数量)
      → Optional header (PE32 / PE32+)
        → Data Directory[14] (CLI header RVA)
          → CLI header (metadata RVA + size)
            → metadata root
```

Data Directory 的第 15 项（index 14）固定指向 CLI header，这是 ECMA-335 Partition II 25.3.3 定义的规则。LeanCLR 直接按偏移读取这个值，不做额外的格式探测。

### 设计选择

PeImageReader 的实现有一个明确的取舍：它只读取定位 metadata 所需的最小信息。

PE 文件中还有很多 LeanCLR 不需要的内容：import table、export table、resource directory、debug directory 等。PeImageReader 全部跳过，只关心一件事——metadata root 在文件的哪个偏移位置，有多大。

这和 CoreCLR 的 PEDecoder 形成对比。PEDecoder 需要处理完整的 PE 结构，因为 CoreCLR 要支持 native interop、debug 信息、strong name 验证等功能。LeanCLR 作为轻量级运行时，不需要这些功能，所以 PE 解析层极其精简。

拿到 metadata root 的偏移和大小之后，PeImageReader 的工作就结束了。接下来由 CliImage 接手。

## CliImage：5 个 stream 的解析

**源码位置：** `src/runtime/metadata/cli_image.h`、`src/runtime/metadata/cli_image.cpp`

CliImage 是 LeanCLR metadata 解析的核心类。它的职责是：接收 PeImageReader 定位到的 metadata root，解析出 5 个 stream，并提供按 index 或 offset 访问数据的接口。

### metadata root 结构

metadata root（也叫 metadata header）的布局是 ECMA-335 Partition II 24.2.1 定义的：

```
Signature (0x424A5342, "BSJB")
MajorVersion (2 bytes)
MinorVersion (2 bytes)
Reserved (4 bytes)
VersionLength (4 bytes)
VersionString (UTF-8, padded to 4-byte boundary)
Flags (2 bytes)
NumberOfStreams (2 bytes)
StreamHeaders[NumberOfStreams]
```

CliImage 首先验证签名（BSJB magic number），然后读取 stream header 数组。每个 stream header 包含 offset、size 和 name。

### 5 个 stream

ECMA-335 定义了 5 个标准 stream：

| Stream | 存储内容 | CliImage 中的表示 |
|--------|----------|-------------------|
| `#Strings` | 类型名、方法名、字段名等 UTF-8 字符串 | 原始字节指针 + 大小 |
| `#US` | 用户字符串字面值（UTF-16 编码） | 原始字节指针 + 大小 |
| `#Blob` | 方法签名、类型签名、常量值等二进制块 | 原始字节指针 + 大小 |
| `#GUID` | 16 字节 GUID 数组 | 原始字节指针 + 大小 |
| `#~` | metadata 表（TypeDef、MethodDef 等 44 种表） | 解码后的表结构 |

前 4 个 stream 的加载是直接的——记录起始地址和大小就够了。所有访问都通过偏移量完成，不需要额外的解码。

`#~` stream（也叫 metadata table stream）是真正复杂的部分，需要专门的解码逻辑。

### stream 加载过程

CliImage 的 `load_streams` 方法是整个加载链路的入口。LEAN-F1 中给出的调用链是：

```
leanclr_load_assembly
  → Assembly::load_by_name
    → CliImage::load_streams
      → RtModuleDef::create
```

`load_streams` 做的事情按顺序是：

1. 验证 BSJB 签名
2. 跳过版本字符串（按 4 字节对齐）
3. 遍历 stream header，按名字匹配 5 个 stream
4. 记录每个 stream 的 offset 和 size
5. 对 `#~` stream 调用 metadata 表解码逻辑

这个过程是一次性完成的，不存在懒加载——因为 stream header 本身很小（通常不到 100 字节），而且后续的所有操作都需要知道 stream 的位置。

### 字符串和 Blob 的访问

`#Strings` stream 的访问模式是：给定一个偏移量（string index），返回一个以 null 结尾的 UTF-8 字符串。metadata 表里的名字字段（type name、method name、field name）存储的都是 `#Strings` stream 中的偏移量。

`#Blob` stream 类似，但内容是带长度前缀的二进制数据。blob 的第一个或前几个字节编码了数据长度（compressed unsigned integer），后面是实际数据。方法签名、类型签名、自定义属性数据都存储在这里。

`#US` stream 存储 C# 代码中的字符串字面值（`ldstr` 指令引用的值）。编码是 UTF-16LE，每个条目前面有一个长度前缀。

这些访问模式和 IL2CPP 的实现几乎一样——因为它们面对的是同一个二进制格式。区别在于封装方式：IL2CPP 把 stream 访问分散在多个类里，LeanCLR 集中在 CliImage 一个类中。

## Metadata 表解码

**源码位置：** `src/runtime/metadata/cli_metadata.h`

`#~` stream 是 metadata 最复杂的部分。它包含 44 种不同的表（ECMA-335 Partition II 22 定义），每种表有不同的列定义。

### 表头解析

`#~` stream 的头部包含：

```
Reserved (4 bytes)
MajorVersion (1 byte)
MinorVersion (1 byte)
HeapSizes (1 byte)
Reserved (1 byte)
Valid (8 bytes, bitmask)
Sorted (8 bytes, bitmask)
Rows[count of set bits in Valid]
```

HeapSizes 字段决定了三个 heap（`#Strings`、`#Blob`、`#GUID`）的索引宽度——如果 heap 大小超过 65536 字节，对应的索引用 4 字节表示，否则用 2 字节。

Valid bitmask 标记了哪些表存在。44 种表不一定都出现在每个程序集中。一个简单的 Hello World 程序可能只有 5-6 种表，而一个复杂的框架库可能用到 30 种以上。

cli_metadata.h 定义了所有 44 种表的枚举和列布局。核心的表包括：

| 表 ID | 表名 | 用途 |
|--------|------|------|
| 0x00 | Module | 模块定义（每个 DLL 一行） |
| 0x01 | TypeRef | 引用的外部类型 |
| 0x02 | TypeDef | 当前模块定义的类型 |
| 0x04 | Field | 字段定义 |
| 0x06 | MethodDef | 方法定义 |
| 0x08 | Param | 参数定义 |
| 0x09 | InterfaceImpl | 接口实现关系 |
| 0x0A | MemberRef | 引用的外部成员 |
| 0x0B | Constant | 常量值 |
| 0x0C | CustomAttribute | 自定义属性 |
| 0x17 | TypeSpec | 泛型实例化类型规范 |
| 0x1B | TypeRef（外部）| 程序集引用的类型 |
| 0x2B | GenericParam | 泛型参数定义 |
| 0x2C | MethodSpec | 泛型方法实例化 |

### coded index 的变宽编码

metadata 表之间的引用不是简单的行号，而是 coded index。这是 ECMA-335 中最精巧的设计之一。

一个 coded index 把"哪张表"和"第几行"编码到同一个整数里。低位是表标记（tag），高位是行号。不同类型的 coded index 有不同数量的候选表，因此 tag 的位宽也不同。

例如，TypeDefOrRef coded index 可以指向 TypeDef、TypeRef 或 TypeSpec 三张表之一，tag 占 2 位：

```
Tag bits: 2
  00 → TypeDef
  01 → TypeRef
  10 → TypeSpec
Row = value >> 2
```

coded index 本身的宽度（2 字节还是 4 字节）取决于候选表中最大的行数：如果最大行数右移 tag 位数后超过 16 位整数范围，就用 4 字节，否则用 2 字节。

cli_metadata.h 中定义了所有 coded index 类型的 tag 位宽和候选表映射。LeanCLR 在解码 `#~` stream 时，先根据每张表的行数和 HeapSizes 计算出每一列的宽度，然后按计算出的 row size 逐行读取。

### Row 的读取逻辑

确定了每列宽度之后，读取 metadata row 就是简单的偏移计算：

```
row_offset = table_offset + (row_index - 1) * row_size
column_value = read(row_offset + column_offset, column_width)
```

注意 metadata 的行号从 1 开始（token 的低 24 位），而不是从 0 开始。这是 ECMA-335 的规定。

LeanCLR 把这个计算封装在 CliImage 的读取方法里。调用方传入 table ID 和 row index，得到一个可以按列索引访问的 row 结构。这种按需读取的方式避免了一次性把所有表解码到内存中。

## RtModuleDef：从 raw 表到运行时结构

**源码位置：** `src/runtime/metadata/module_def.h`

CliImage 提供的是 metadata 表的原始访问接口——给一个 token，返回一行二进制数据。但运行时需要的不是 raw bytes，而是可以直接使用的类型描述符、方法描述符、字段描述符。

RtModuleDef 就是这个转换层。它从 CliImage 读取 raw metadata，构建出 LeanCLR 运行时实际使用的数据结构。

### 核心运行时结构

RtModuleDef 构建的主要结构体有：

**RtClass** —— 类型描述符，描述一个 .NET 类型的完整信息：

- 类型名称（namespace + name，从 `#Strings` stream 读取）
- 父类引用
- 接口列表
- 字段数组（`RtFieldInfo[]`）
- 方法数组（`RtMethodInfo[]`）
- 泛型参数
- 类型标志（abstract、sealed、interface 等）
- 实例大小和静态字段大小
- VTable 布局

**RtMethodInfo** —— 方法描述符：

- 方法名称
- 签名（参数类型列表 + 返回类型）
- 方法体 IL 偏移和大小
- 方法标志（static、virtual、abstract 等）
- 所属 RtClass
- 异常处理子句（`RtInterpExceptionClause`）

**RtFieldInfo** —— 字段描述符：

- 字段名称
- 字段类型
- 偏移量（实例字段在对象内存中的偏移）
- 字段标志（static、readonly 等）

这三个结构体是 LeanCLR 类型系统的基础。解释器执行方法时查 RtMethodInfo，访问字段时查 RtFieldInfo，类型检查时查 RtClass。

### lazy 初始化策略

RtModuleDef 不会在加载程序集时一次性构建所有 RtClass。它采用 lazy 初始化：

第一阶段（加载时）：只建立 token → RtClass 指针的映射表，RtClass 本身只填入最基本的信息（名字和标志）。

第二阶段（首次使用时）：当某个 RtClass 第一次被实际访问（比如创建实例、调用方法），才完成完整的初始化——解析父类、构建 VTable、计算字段偏移、解析泛型参数。

这个策略的好处很直接：一个程序集可能定义了几百个类型，但运行时实际用到的可能只有几十个。lazy 初始化避免了加载无用类型的开销。

HybridCLR 的 InterpreterImage 也采用类似的策略——它的 `InitRuntimeMetadatas` 在加载时只建索引，实际的类型初始化推迟到 `il2cpp::vm::Class::Init` 调用时。这几乎是所有 CLR 实现的共识做法。

### TypeDef → RtClass 的映射

从 metadata 到 RtClass 的构建过程涉及多张表的交叉查询：

```
TypeDef 表（第 0x02 号表）
  → 读取 TypeName、TypeNamespace 列 → 拼出完整类型名
  → 读取 Extends 列（TypeDefOrRef coded index）→ 定位父类
  → 读取 FieldList 列 → 确定字段范围（到下一个 TypeDef 的 FieldList）
  → 读取 MethodList 列 → 确定方法范围

对每个 Method（第 0x06 号表）
  → 读取 Name、Signature 列
  → 解析 Blob 中的方法签名
  → 构建 RtMethodInfo

对每个 Field（第 0x04 号表）
  → 读取 Name、Signature 列
  → 解析 Blob 中的字段签名
  → 构建 RtFieldInfo
```

字段和方法的范围确定用了一个常见的 ECMA-335 模式：TypeDef 表的 FieldList 和 MethodList 列存的是起始行号，范围的结束由下一个 TypeDef 的对应列隐式确定。这要求 TypeDef 表是按顺序排列的——ECMA-335 确实要求如此。

## MetadataCache：类型/方法查找

**源码位置：** `src/runtime/metadata/metadata_cache.h`

RtModuleDef 构建的运行时结构需要一个全局的查找机制。MetadataCache 提供这个能力。

### 按 token 查找

最基本的查找方式。metadata token 是一个 32 位整数，高 8 位是表 ID，低 24 位是行号。

```
token = 0x02000005
  → 表 ID = 0x02 (TypeDef)
  → 行号 = 5
  → 查找 module 中第 5 个 TypeDef 对应的 RtClass
```

MetadataCache 维护了 token → RtClass/RtMethodInfo/RtFieldInfo 的映射。这个映射在 RtModuleDef 构建阶段就初始化完成。

### 按名字查找

某些场景需要按类型全名查找——比如 `Type.GetType("System.String")` 的实现。MetadataCache 为此维护了一个 namespace + name → RtClass 的哈希表。

跨程序集的类型引用（TypeRef 表）也走名字查找路径：先通过 ResolutionScope 确定目标程序集，再在目标程序集的 MetadataCache 中按名字查找。

### 多 assembly 管理

一个 LeanCLR 运行时实例通常会加载多个程序集：至少有 mscorlib（或 System.Private.CoreLib），加上用户的业务程序集。MetadataCache 在全局层面维护了已加载程序集的列表，提供跨程序集的类型解析能力。

当解析一个 TypeRef 时，流程是：

```
TypeRef.ResolutionScope → 定位目标 Assembly
  → 在目标 Assembly 的 RtModuleDef 中按 namespace + name 查找
    → 返回目标 Assembly 中的 RtClass
```

这个跨 assembly 解析对于解释器来说是必要的——业务代码里的 `new List<int>()` 需要先解析到 mscorlib 中的 `System.Collections.Generic.List<T>`。

## GenericMetadata：泛型膨胀

**源码位置：** `src/runtime/metadata/generic_metadata.h`

泛型是 metadata 解析中最复杂的部分。ECMA-335 定义了开放类型（`List<T>`）和封闭类型（`List<int>`）的区分，但 metadata 表中只存储开放类型的定义。封闭类型需要在运行时动态构建。

### 从 `List<T>` 到 `List<int>`

当运行时第一次遇到 `List<int>` 这个类型时，GenericMetadata 的工作是：

1. 找到 `List<T>` 的开放类型定义（TypeDef 表中的一行）
2. 确认类型参数 `T` 的个数和约束（GenericParam 表）
3. 用 `int`（System.Int32）替换 `T`
4. 构建一个新的 RtClass，代表 `List<int>` 这个封闭类型

新构建的 RtClass（在 LeanCLR 中表示为 RtGenericClass）包含：

- 指向开放类型 RtClass 的引用
- 实际的类型参数数组（`[System.Int32]`）
- 膨胀后的方法签名（所有出现 `T` 的地方替换为 `int`）
- 膨胀后的字段类型
- 重新计算的实例大小（因为字段类型变了，大小可能变化）

### RtGenericClass 结构

RtGenericClass 是 RtClass 的一个特化形式。它不是独立于开放类型的完整副本，而是一个"差异层"——大部分信息直接引用开放类型的 RtClass，只有类型参数相关的部分做了替换。

这个设计在内存使用上很友好。一个程序如果用了 `List<int>`、`List<string>`、`List<float>` 三个封闭类型，它们共享同一个 `List<T>` 的方法体和大部分元数据，只有类型参数不同。

### 泛型缓存

同一个封闭类型可能在程序中出现多次。GenericMetadata 通过缓存确保每个封闭类型只构建一次。缓存的 key 是开放类型 + 类型参数列表的组合。

```
cache key: (List<T>, [int]) → RtClass for List<int>
cache key: (List<T>, [string]) → RtClass for List<string>
cache key: (Dictionary<TKey, TValue>, [string, int]) → RtClass for Dictionary<string, int>
```

泛型方法（MethodSpec 表）的处理逻辑类似：开放方法定义 + 类型参数 → 封闭方法实例。

## 与 IL2CPP / HybridCLR 的对比

LeanCLR 的 metadata 解析和 IL2CPP、HybridCLR 面对的是同一个问题，但实现策略有显著差异。

| 维度 | LeanCLR | IL2CPP | HybridCLR |
|------|---------|--------|-----------|
| **PE 解析类** | PeImageReader | 不需要（AOT 已转换） | 复用 IL2CPP 的 PE 解析 |
| **Image 核心类** | CliImage | RawImageBase / RawImage | InterpreterImage |
| **Module 表示** | RtModuleDef | Il2CppImage | 复用 Il2CppImage |
| **类型描述符** | RtClass | Il2CppClass | 复用 Il2CppClass |
| **方法描述符** | RtMethodInfo | MethodInfo / Il2CppMethodDefinition | 复用 + MethodInfo 适配 |
| **metadata 来源** | 直接读 DLL（ECMA-335 二进制） | global-metadata.dat（自定义格式） | 直接读 DLL（ECMA-335 二进制） |
| **表解码方式** | 运行时按需解码 #~ stream | 构建时已转换为 C++ 数据结构 | 运行时按需解码 #~ stream |
| **泛型膨胀** | GenericMetadata 运行时膨胀 | il2cpp.exe 构建时膨胀 + 运行时补充 | 运行时膨胀，注入 IL2CPP 类型系统 |
| **缓存层** | MetadataCache | MetadataCache（全局唯一） | 复用 IL2CPP MetadataCache |
| **lazy 初始化** | 有（RtModuleDef 分阶段） | 有（Class::Init） | 有（InitRuntimeMetadatas） |

这张表的核心区别在 **metadata 来源** 这一行。

IL2CPP 走的是 AOT 路线：`il2cpp.exe` 在构建时已经把 ECMA-335 metadata 转换成了自定义的 `global-metadata.dat` 格式和编译期的 C++ 数据结构。运行时的 Il2CppImage 读取的不是原始的 DLL，而是这个预处理后的格式。所以 IL2CPP 根本不需要 PE 解析和 `#~` stream 解码。

HybridCLR 作为 IL2CPP 的热更新补丁，需要在运行时加载新的 DLL。这时候 IL2CPP 原有的 metadata 加载通道不支持动态加载，所以 HybridCLR 的 InterpreterImage 自己实现了一套 ECMA-335 解析——这部分和 LeanCLR 的 CliImage 在逻辑上几乎等价。区别在于：HybridCLR 解析完之后要把结果注入到 IL2CPP 的类型系统（Il2CppClass、MethodInfo 等）中，而 LeanCLR 用的是自己的类型系统（RtClass、RtMethodInfo 等）。

LeanCLR 和 HybridCLR 在 metadata 解析层面的相似性不是巧合——它们来自同一团队，面对的是同一份规范。但 LeanCLR 不需要适配 IL2CPP 的类型系统，所以实现上更直接、更干净。

## 收束

LeanCLR 的 metadata 解析链路可以用四句话概括：

PeImageReader 从 PE 文件中定位 metadata root。CliImage 解析 5 个 stream，把 `#~` stream 中的 44 种表解码为可按列访问的 row 结构。RtModuleDef 把 raw 表数据转换为运行时结构（RtClass、RtMethodInfo、RtFieldInfo），采用 lazy 初始化避免加载无用类型。GenericMetadata 处理泛型膨胀，从开放类型生成封闭类型的 RtClass。

整个模块 25 个文件、8,603 行代码。和 CoreCLR 动辄几万行的 metadata 子系统相比，LeanCLR 的实现是真正的最小可用集。它跳过了 strong naming、assembly 版本策略、Reflection.Emit 支持等 CoreCLR 必须处理但轻量级运行时不需要的能力，把复杂度控制在了可通读的范围内。

但复杂度省不掉的地方——coded index 的变宽编码、泛型膨胀的递归构建、跨 assembly 的类型解析——LeanCLR 一样不少。这些是 ECMA-335 二进制格式本身带来的必然复杂度，任何直接读 DLL 的运行时（包括 HybridCLR 的 InterpreterImage）都要面对。

## 系列位置

- 上一篇：[LEAN-F1 调研报告：架构总览与源码地图]({{< ref "leanclr-survey-architecture-source-map" >}})
- 下一篇：[LEAN-F3 双解释器架构]({{< ref "leanclr-dual-interpreter-hl-ll-transform-pipeline" >}})
