---
title: "横切对比｜Metadata 解析：5 个 runtime 怎么读同一份 .NET DLL"
date: "2026-04-14"
description: "同一份 ECMA-335 二进制格式，CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 的解析策略、缓存策略和惰性加载程度完全不同。这篇横切对比拆开这些差异，分析背后的设计决策。"
weight: 80
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Mono"
  - "Metadata"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> 5 个 runtime 面对的都是同一份 ECMA-335 二进制格式，但它们的解析策略、缓存策略和惰性加载程度完全不同——这些差异直接决定了启动速度、内存占用和热更新能力。

这是 .NET Runtime 生态全景系列的横切对比首篇。

前面的文章按纵向拆解了各个 runtime 的内部结构。这篇换一个视角：选一个所有 runtime 都必须完成的任务——读取 metadata——然后看 5 个实现之间的差异。

## 同一份规范，5 种解析策略

ECMA-335 Partition II 定义了 .NET 程序集的二进制格式。任何一个合规的 .NET DLL 都是这个格式：PE/COFF 外壳 + CLI header + metadata streams + IL code。Pre-A 已经拆过这个物理结构（5 个 stream、metadata 表、token 编码），这里不重复。

这篇要回答的问题是：面对同一份格式，5 个 runtime 各自走了什么路？

先给出一个直觉层面的分类：

```
                      .NET DLL (ECMA-335)
                             │
        ┌──────────┬─────────┼─────────┬──────────┐
        │          │         │         │          │
     CoreCLR    Mono     IL2CPP   HybridCLR   LeanCLR
        │          │         │         │          │
   完整 PE 解析  完整 PE 解析  构建时     运行时      最小 PE
   内存映射      逐字段解码   已完成     完整解析     只取 CLI
   延迟绑定      立即缓存    只读预处理  双路径并行   按需构建
```

CoreCLR 和 Mono 是传统路线——运行时加载 DLL、解析 PE、读取所有 metadata 表。IL2CPP 是最特殊的一个——metadata 在构建时已经被预处理成 `global-metadata.dat`，运行时不再接触原始 DLL 格式。HybridCLR 在 IL2CPP 之上又加了一层：AOT 部分走 IL2CPP 原有通道，热更 DLL 走自己的运行时解析。LeanCLR 走极简路线——PE 文件头只提取必要的 CLI 信息，能省的全省。

下面逐个拆。

## CoreCLR：PEDecoder + MDInternalRO

CoreCLR 的 metadata 解析从 `PEDecoder` 开始，经过 `MDInternalRO`，最终把类型信息填充到 `MethodTable` 和 `EEClass` 中。

### 加载链路

一个程序集被加载时，大致的调用链路是：

```
AssemblyLoadContext::LoadFromPath
  → PEImage::OpenFile
    → PEDecoder::Init          // 解析 PE/COFF 头
      → PEDecoder::CheckCorHeader  // 定位 CLI header
  → PEFile::SetupMetadataAccess
    → MDInternalRO::OpenScope   // 打开 metadata scope
      → CMiniMdRW::CommonOpenRO  // 建立 metadata 表的只读视图
```

`PEDecoder` 做的是完整的 PE 解析。它会验证 DOS header、PE signature、Optional header、Section table，然后定位 CLI header（IMAGE_COR20_HEADER）和 metadata 根。这一步和操作系统的 PE 加载器做的事情一样完整——CoreCLR 需要支持 PE 文件的所有合法变体，包括 mixed-mode 程序集和 R2R（Ready to Run）镜像。

### MDInternalRO 与 CMiniMdRW

metadata 的真正入口是 `MDInternalRO`。这个类提供了对 metadata 表的只读访问接口——给一个 TypeDef token，返回类名、命名空间、字段列表、方法列表；给一个 MethodDef token，返回签名、RVA、flags。

底层存储由 `CMiniMdRW` 管理。它持有 metadata 表的内存映射视图，按需解析行数据。关键的设计是它同时支持只读模式（RO）和读写模式（RW）——RW 模式支持 Edit and Continue（EnC），允许 debugger 在运行时修改 metadata 表、添加新的方法定义，而不需要重新加载整个程序集。

这个 RW 能力在 5 个 runtime 中是独此一家的。Mono、IL2CPP、HybridCLR、LeanCLR 都不支持 EnC。

### 内存映射与延迟绑定

CoreCLR 使用内存映射文件（memory-mapped file）来访问 PE 文件内容。metadata 不是一次性复制到内存中的，而是通过内存映射按需访问。操作系统的虚拟内存管理器负责决定哪些页面实际驻留物理内存。

类型解析采用延迟绑定策略：加载一个程序集时，CoreCLR 不会立即解析所有 TypeDef 记录并构建 `MethodTable`。只有当代码第一次引用一个类型时（比如 JIT 编译过程中遇到 `newobj` 指令），`ClassLoader::LoadTypeHandleForTypeKey` 才会触发该类型的完整解析——读取 TypeDef 记录、解析父类、解析接口列表、计算字段布局、构建 `MethodTable`。

这种按需策略的好处是启动时间短——大型程序集可能包含数千个类型，但实际执行路径只触及其中一部分。

## Mono：MonoImage + mono_metadata_decode

Mono 的 metadata 解析路径在精神上和 CoreCLR 相似——都是运行时加载 DLL、做完整 PE 解析——但实现细节有几个关键差异。

### 嵌入式历史的痕迹

Mono 最初的目标是做一个跨平台的 .NET runtime，特别是在 Linux 和嵌入式设备上运行。这个定位影响了它的 metadata 解析设计：

- 不依赖操作系统的 PE 加载器。Mono 自己解析 PE 头，不调用 Windows 的 `LoadLibrary`
- 不使用内存映射文件。metadata 通过 `g_malloc` 分配内存后逐字段复制

后一点和 CoreCLR 形成了明显对比。CoreCLR 用 mmap 可以利用操作系统的页面缓存和延迟加载机制，Mono 选择自己管理内存是因为它需要运行在不保证 mmap 可用的平台上。

### MonoImage 结构

Mono 把一个加载完成的程序集封装在 `MonoImage` 结构中。加载链路大致是：

```
mono_image_open_from_data_internal
  → do_mono_image_load
    → mono_image_load_pe_data     // 解析 PE header
    → mono_image_load_cli_data    // 解析 CLI header + metadata root
    → mono_image_load_cli_header  // 填充 MonoImage 的 metadata 字段
    → mono_image_load_tables      // 加载所有 metadata 表
```

`MonoImage` 中的核心字段包括指向各 metadata 表的指针、5 个 stream 的基地址和大小。这些在 `mono_image_load_tables` 阶段就完成了初始化。

### 缓存策略

Mono 和 CoreCLR 的一个重要差异在于类型信息的缓存粒度。

当 Mono 第一次解析一个类型时，它会构建一个 `MonoClass` 结构并缓存在 `MonoImage` 的 hash table 中。后续再引用这个类型时直接查表，不再回 metadata 去解析。方法信息同理——`MonoMethod` 在首次解析后缓存。

```
mono_class_from_name_case
  → 查 MonoImage::class_cache
    → miss → mono_class_create_from_typedef
      → mono_metadata_decode_table_row  // 从表中读 TypeDef 行
      → 设置字段布局、方法列表、接口列表
      → 存入 class_cache
    → hit → 直接返回
```

这个"一次解析、全量缓存"的策略和 CoreCLR 的延迟绑定殊途同归——都是按需触发。但 Mono 的 `MonoClass` 一旦构建就是完整的，包含了所有字段、方法、接口的描述；CoreCLR 的 `MethodTable` 构建则更加细粒度——vtable slots 可以指向 stub，直到方法真正被调用时才填充最终实现。

## IL2CPP：global-metadata.dat + 构建时预处理

IL2CPP 走的是一条完全不同的路。它在运行时根本不解析 .NET DLL。

### 构建时全量预处理

IL2CPP 的构建管线（il2cpp.exe）在打包阶段就完成了所有 metadata 解析工作。它读取所有程序集的 DLL，提取 metadata，执行类型解析、方法签名解析、泛型实例化枚举，然后把结果序列化成两份产物：

- **C++ 代码**：每个 C# 方法对应一个 C++ 函数，通过 native 编译器（MSVC / Clang / GCC）编译成机器码
- **global-metadata.dat**：运行时需要的元数据——类型名称、方法名称、字符串字面量、程序集引用关系

运行时的 metadata 访问路径是：

```
MetadataCache::Initialize
  → s_GlobalMetadata = mmap("global-metadata.dat")
  → 验证 magic number 和版本号
  → 建立各 metadata section 的指针偏移表
```

之后所有 metadata 查询都走预处理后的结构。`MetadataCache::GetTypeInfoFromTypeIndex` 不再需要解析 ECMA-335 格式的表行，而是直接从 `global-metadata.dat` 的预计算偏移处读取 `Il2CppTypeDefinition`。

### 运行时不再有 PE 解析

这是 IL2CPP 和其他四个 runtime 最根本的差异：运行时完全不接触 PE/COFF 格式。

`global-metadata.dat` 是 IL2CPP 自定义的二进制格式，不是 ECMA-335 格式。它的内部组织按照运行时的查询模式优化——按索引直接访问，没有变长编码、没有堆间引用、不需要 token 解析。

这个设计的优势是明确的：

- **启动时没有解析开销**。metadata 已经是运行时就绪的格式，mmap 之后直接用
- **内存占用低**。不需要在内存中同时保持原始 DLL 和解析后的类型结构
- **安全性**。原始 DLL 不随包体发布，IL 代码不暴露给用户

代价同样明确：

- **构建时间长**。每次打包都要全量处理所有程序集
- **不支持运行时加载新程序集**。`global-metadata.dat` 在构建时冻结，运行时无法扩展

第二个限制正是 HybridCLR 存在的原因。

## HybridCLR：InterpreterImage + AOTHomologousImage

HybridCLR 在 IL2CPP 的 metadata 体系上叠加了自己的解析层，形成了两条并行的 metadata 路径。

### 两条路径，两个入口

**路径一：热更 DLL → InterpreterImage**

热更新的 DLL 是标准的 .NET 程序集。这些 DLL 在构建时不存在于 IL2CPP 的处理管线中，所以 `global-metadata.dat` 里没有它们的任何信息。运行时需要从头解析这些 DLL 的 metadata。

这条路径的核心类是 `RawImage`（负责 PE/COFF 解析和 metadata stream 定位）和 `InterpreterImage`（在 `RawImage` 之上构建完整的运行时类型描述）。

```
Assembly::Load(byte[] dllBytes)
  → InterpreterImage::Load
    → RawImage::LoadFromBytes
      → 解析 PE header
      → 解析 CLI header
      → 定位 5 个 metadata stream
      → 解析所有 metadata 表行
    → InterpreterImage::InitTypeMapping
      → 为每个 TypeDef 建立 Il2CppClass 描述
      → 构建方法、字段的运行时索引
```

`RawImage` 做的事情本质上和 CoreCLR 的 `PEDecoder` + `MDInternalRO` 一样——完整的 ECMA-335 格式解析。区别在于 HybridCLR 的上层需要把解析结果填充到 IL2CPP 的数据结构中（`Il2CppClass`、`Il2CppMethodDefinition` 等），因为解释器的执行引擎复用了 IL2CPP 的类型系统。

**路径二：AOT 补充 → AOTHomologousImage**

AOT 补充元数据解决的是另一个问题：热更 DLL 引用了 AOT 程序集中的泛型实例化，但 IL2CPP 在构建时没有为这些实例化生成代码。

`AOTHomologousImage` 不是用来加载新代码的，而是用来补充 IL2CPP 已有程序集的 metadata。它的解析流程同样走 `RawImage`，但匹配策略不同：

```
AOTHomologousImage::Load
  → RawImage::LoadFromBytes      // 同样解析 PE + metadata
  → AOTHomologousImage::InitTypes
    → 按 token 对齐（同版本）或按名字匹配（不同版本）
    → 把补充的泛型实例化信息注入 MetadataCache
```

按 token 对齐是优先路径：如果补充 DLL 和 AOT 构建使用的是同一份源码，TypeDef token 值应该一一对应，直接用 token 做映射最快。如果版本不匹配（比如 AOT 构建后又修改了 AOT 程序集），则回退到按名字匹配——逐个比较 TypeDef 的 namespace + name。

### 两条路径的内存代价

HybridCLR 的双路径设计有一个不可回避的开销：热更 DLL 的原始字节必须保留在内存中。

`RawImage` 解析 metadata 后，IL 字节码仍然在原始 DLL 的字节数组中。解释器执行时需要从这些字节中读取 method body，所以 DLL 数据不能在解析完 metadata 后释放。如果热更包包含多个 DLL，每个 DLL 的原始字节都需要驻留内存。

相比之下，CoreCLR 使用 mmap 可以让操作系统在内存压力下换出页面，Mono 在解析后可以释放原始数据只保留 `MonoClass` 缓存。HybridCLR 的这个额外内存驻留是它作为"IL2CPP 补丁"方案的结构性代价。

## LeanCLR：CliImage + RtModuleDef

LeanCLR 走的是 5 个 runtime 中最轻量的路线。

### 最小 PE 解析

LeanCLR 的 PE 解析器 `PeImageReader` 只关心一件事：找到 CLI header，从中获取 metadata 的位置和大小。

```
PeImageReader::read
  → 跳过 DOS header（只验证 magic "MZ"）
  → 跳到 PE signature
  → 读取 Optional header 的 DataDirectory[14]  // CLI header RVA
  → 从 CLI header 中提取 metadata RVA 和 size
  → 结束
```

它不解析 Section table 的完整布局，不处理 Import table，不关心 Resource directory。它只需要 CLI header 中的一个 DataDirectory 条目——metadata 的 RVA（Relative Virtual Address）。

这个"只取 CLI header"的策略和 CoreCLR 的完整 PE 解析形成了最极端的对比。CoreCLR 需要处理所有合法的 PE 变体（包括 mixed-mode、R2R），LeanCLR 只面对纯 managed 程序集，所以可以安全地跳过 PE 层的大部分信息。

### CliImage：5 个 stream 的入口

从 metadata RVA 开始，`CliImage` 接管后续的解析。它的职责是定位和解析 metadata root 和 5 个 stream：

```
CliImage::load_streams
  → 读取 metadata root signature (0x424A5342 = "BSJB")
  → 解析 stream headers
    → #~ (metadata tables)
    → #Strings
    → #Blob
    → #GUID
    → #US
  → 对 #~ stream：解析 table sizes、row counts、column widths
```

`CliImage` 持有指向每个 stream 的指针和大小。它提供的核心 API 是按表和行号读取 metadata——`get_typedef_row(index)`、`get_methoddef_row(index)`——这些方法直接计算偏移，从 `#~` stream 的原始字节中提取行数据。

和 CoreCLR 的 `CMiniMdRW` 相比，`CliImage` 的实现极度精简：没有 RW 模式、没有 EnC 支持、没有 coded index 的复杂解码缓存。它只做最直接的事——给定表号和行号，返回该行各列的值。

### RtModuleDef：按需构建运行时类型

`CliImage` 只负责原始 metadata 的读取。运行时类型的构建由 `RtModuleDef` 完成。

```
RtModuleDef::create(CliImage*)
  → 为每个 TypeDef 创建 RtClass 占位符
  → 标记为 unresolved

// 后续按需触发：
RtModuleDef::resolve_class(type_def_index)
  → 从 CliImage 读取 TypeDef 行
  → 解析父类引用
  → 解析字段列表（FieldDef 表）
  → 解析方法列表（MethodDef 表）
  → 计算字段布局和 instance_size
  → 构建 vtable
  → 标记为 resolved
```

`RtModuleDef` 在创建时只为每个 TypeDef 分配一个空的 `RtClass` 壳——记录 token 和名字，但不解析字段、方法、接口。只有当代码运行中实际引用到一个类型时，`resolve_class` 才会触发完整的类型构建。

这和 CoreCLR 的延迟绑定策略在精神上一致，但实现更简单。CoreCLR 的延迟绑定涉及 `MethodTable` stub、class loader 的多阶段状态机、跨程序集的循环依赖检测。LeanCLR 的按需解析就是一个 resolved/unresolved 的二态标记。

## 5 方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| PE 解析深度 | 完整（所有 PE 结构） | 完整（自实现 PE 解析器） | 构建时完成 | 完整（热更 DLL） | 最小（只取 CLI header） |
| metadata 存储结构 | MDInternalRO / CMiniMdRW | MonoImage | global-metadata.dat | RawImage / InterpreterImage | CliImage |
| 惰性加载 | 按需解析 type（延迟绑定） | 按需解析 class（MonoClass 缓存） | 构建时全解析 | 按需 | 按需（resolved/unresolved 二态） |
| 内存映射 | mmap（利用 OS 页面管理） | 不使用（自管理内存） | mmap（global-metadata.dat） | 不使用（DLL 字节驻留内存） | 不使用（逐字段读取） |
| 内存占用 | 中等（mmap 按需驻留） | 中等（MonoClass 缓存累积） | 低（预处理后的紧凑格式） | 较高（DLL 原始字节 + Il2CppClass） | 低（精简解析结构） |
| 热更支持 | AssemblyLoadContext（加载/卸载） | 不支持运行时新增程序集 | 不支持（需 HybridCLR） | 核心能力 | 支持（运行时加载新 DLL） |
| EnC 支持 | 支持（CMiniMdRW 读写模式） | 不支持 | 不支持 | 不支持 | 不支持 |
| 格式合规性 | 完整 ECMA-335 | 完整 ECMA-335 | 自定义格式 | ECMA-335（热更）+ 自定义（AOT） | ECMA-335（精简子集） |

几个值得注意的模式。

第一，**PE 解析深度和 runtime 的目标场景直接相关**。CoreCLR 要兼容所有合法的 .NET 程序集（包括 mixed-mode 和 R2R），所以必须做完整 PE 解析。LeanCLR 只面对纯 managed DLL，所以可以跳过整个 PE 层只取 CLI header。

第二，**惰性加载是共识，但粒度不同**。CoreCLR、Mono、LeanCLR 都采用了按需解析策略，但 CoreCLR 的延迟绑定粒度最细（vtable slot 级别可以惰性），LeanCLR 最粗（整个类型要么解析要么不解析）。IL2CPP 是唯一不需要惰性加载的——因为它在构建时已经全部预处理完了。

第三，**内存映射的选择反映了平台假设**。CoreCLR 运行在桌面/服务器环境，mmap 是标准设施。Mono 和 LeanCLR 需要在嵌入式和 WebAssembly 环境运行，不能依赖 mmap。HybridCLR 寄生在 IL2CPP 中，而移动平台的 DLL 是从内存字节加载的，同样不适合 mmap。

## 设计决策分析

看完 5 个实现，可以从"为什么这样做"的角度拉出三条设计逻辑。

### 为什么 IL2CPP 选择构建时预处理

IL2CPP 的核心设计目标是消除运行时的解释和解析成本。它把所有能离线做的事情都移到了构建阶段：metadata 解析、类型布局计算、泛型实例化枚举、IL 到 C++ 的翻译。

运行时只剩下两件事需要做：加载预处理后的 `global-metadata.dat`，以及执行已经编译好的 native code。

这个决策在移动平台上有明确的收益。iOS 不允许 JIT，Android 的内存环境受限，启动速度对游戏体验有直接影响。把解析成本全部转移到构建时，换来的是运行时接近零的 metadata 开销。

但这个决策也创造了一个结构性的限制：运行时无法接受新的程序集。`global-metadata.dat` 在构建时冻结了所有 metadata 信息，运行时没有 ECMA-335 解析器来处理新的 DLL。这个空缺就是 HybridCLR 填补的。

### 为什么 LeanCLR 选择最小 PE 解析

LeanCLR 的目标场景是 H5、微信小游戏、嵌入式设备——体积和内存是硬约束。

一个完整的 PE 解析器需要处理 DOS stub、Rich header、Section table 映射、Import/Export table、Resource directory、Relocation table 等结构。这些对于纯 managed DLL 来说全是冗余信息——managed 程序集的 PE 外壳只是一个历史遗留的容器格式，真正有用的数据全在 CLI header 指向的 metadata 区域里。

LeanCLR 的 `PeImageReader` 跳过了整个 PE 层的绝大部分，只提取 CLI header 中 metadata 的 RVA。这让 PE 解析器本身的代码量降到了最低，同时避免了为无用结构分配内存。

这个选择对 LeanCLR 的适用范围有一个隐含约束：它不能加载 mixed-mode 程序集（同时包含 managed 和 native 代码的 DLL），因为 mixed-mode 程序集的 PE 结构中包含了 native 入口点、Import table 等信息，这些在 LeanCLR 的解析路径中被跳过了。但在它的目标场景中，这个约束不构成问题——H5 和小游戏环境下不会出现 mixed-mode 程序集。

### 为什么 HybridCLR 需要两条路径

HybridCLR 的双路径设计不是过度工程，而是它的定位决定的必然结构。

**路径一（InterpreterImage）存在的原因：** IL2CPP 的构建管线在打包时不知道热更 DLL 的存在，所以 `global-metadata.dat` 里没有热更类型的任何信息。运行时必须从头解析这些 DLL，走完整的 ECMA-335 解析流程，然后把结果桥接到 IL2CPP 的类型系统中。

**路径二（AOTHomologousImage）存在的原因：** 热更代码可能引用 AOT 程序集中不存在的泛型实例化。比如热更代码调用了 `Dictionary<MyHotfixType, int>.Add()`，但 IL2CPP 构建时没有为这个特定的泛型组合生成代码。AOTHomologousImage 通过加载补充 metadata，把缺失的泛型实例化信息注入 `MetadataCache`，让 IL2CPP 的泛型查找机制能找到它们。

两条路径的划分逻辑清晰：InterpreterImage 处理"完全新增"的代码，AOTHomologousImage 处理"对已有代码的补充"。前者需要完整的类型构建，后者只需要 metadata 级别的注入。

## 收束

5 个 runtime 面对同一份 ECMA-335 metadata 格式，给出了 5 套不同的解析方案。差异的根源不在技术偏好，而在各自的设计目标和运行环境：

**CoreCLR** 做完整 PE 解析 + 内存映射 + 延迟绑定，因为它要支持最广泛的程序集格式，运行在资源充裕的桌面/服务器环境。

**Mono** 做完整 PE 解析 + 自管理内存 + MonoClass 缓存，因为它要在没有 mmap 保证的嵌入式环境中运行。

**IL2CPP** 把 metadata 解析整体移到构建时，因为它的目标是消除运行时的一切解析成本，代价是冻结了运行时扩展的可能性。

**HybridCLR** 在 IL2CPP 之上叠加两条解析路径，因为它必须同时兼容已有的 AOT 体系和新增的热更代码。

**LeanCLR** 做最小 PE 解析 + 按需类型构建，因为它的约束是体积和内存，而不是兼容性。

这些差异不是对错之分，而是 trade-off 的具体展开。同一个问题的 5 种解法，每一种都精确地对应了它所服务的场景。

## 系列位置

- 所属模块：横切对比篇（模块 G）
- 上一篇：无（横切对比首篇）
- 下一篇：CROSS-G2 类型系统实现：MethodTable vs Il2CppClass vs RtClass
