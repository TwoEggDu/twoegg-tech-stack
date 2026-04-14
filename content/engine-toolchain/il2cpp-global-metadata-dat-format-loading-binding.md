---
title: "IL2CPP 实现分析｜global-metadata.dat：格式、加载与 runtime 的绑定"
date: "2026-04-14"
description: "拆解 global-metadata.dat 的完整生命周期：il2cpp.exe 如何生成它、二进制文件头和各 section 的结构、MetadataLoader 的加载路径、Il2CppClass 如何通过 index 延迟绑定 metadata、加密与逆向工具、与 CoreCLR/Mono 的 metadata 存储对比、版本兼容性的 fast-fail 设计。"
weight: 53
featured: false
tags:
  - IL2CPP
  - Unity
  - Metadata
  - Runtime
  - Architecture
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> global-metadata.dat 是 IL2CPP 管线中唯一一个"构建时写入、运行时只读"的二进制数据文件——它不包含可执行代码，但没有它，运行时连一个类型名都查不到。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 4 篇，承接 D3 libil2cpp runtime 的展开。

D3 拆解了 libil2cpp 的三层内部结构，其中 MetadataCache 被反复提到：它在 `Runtime::Init` 时初始化、按 token 索引类型和方法、惰性构建 `Il2CppClass`。但那篇有意留了一个口子——MetadataCache 从哪里读数据、数据长什么样、版本不匹配时发生什么。这篇把这个口子补上。

## global-metadata.dat 在管线中的位置

回顾 D1 的五阶段管线：

```
Stage 1        Stage 2          Stage 3           Stage 4         Stage 5
C# 源码  →  IL 程序集  →  Stripped IL  →  C++ 源码  →  native binary  →  最终包体
(Roslyn)    (DLL)       (UnityLinker)   (il2cpp.exe)  (Clang/MSVC)
```

global-metadata.dat 由 il2cpp.exe 在 Stage 3 生成。il2cpp.exe 的核心工作是把 IL 翻译成 C++ 代码，但它在翻译过程中还有一项附带产出：把所有需要在运行时查询的元数据信息——类型名、方法名、字段定义、泛型参数、字符串字面量等——序列化成一份独立的二进制文件。

这份文件随 native binary 一起打入最终包体，在不同平台上的路径有所不同：

| 平台 | 典型路径 |
|------|---------|
| Android | `assets/bin/Data/Managed/Metadata/global-metadata.dat` |
| Windows | `<GameName>_Data/il2cpp_data/Metadata/global-metadata.dat` |
| iOS | `Data/Managed/Metadata/global-metadata.dat` |

运行时，libil2cpp 在启动阶段读取这份文件，解析其内容并建立 MetadataCache。之后所有类型查询、反射调用、异常信息输出都依赖这份数据。文件本身在整个进程生命周期中只读，不会被修改。

## 为什么需要这个文件

一个直觉上的问题：il2cpp.exe 已经把所有 C# 代码翻译成了 C++ 函数，类型信息不是已经编码到了 C++ 结构体里了吗？为什么还需要一份额外的数据文件？

原因在于 C++ 代码中丢失了 .NET metadata 的运行时表示。

在标准 .NET 运行时（CoreCLR、Mono）中，IL 程序集本身就是 metadata 的载体——DLL 文件中的 metadata tables 包含了类型名、方法签名、泛型参数、自定义 Attribute 等所有描述信息。运行时直接解析 DLL 就能获取这些信息。

IL2CPP 的转换管线把 IL 代码变成了 C++ 函数，DLL 文件在构建完成后就不再存在于包体中。C++ 编译器编译出的 native binary 里只有函数指针和内存布局——没有字符串形式的类型名，没有方法签名的结构化描述，没有泛型参数的约束信息。

但运行时仍然需要这些信息。反射查询（`GetType("Player")`）需要按名字查找类型。异常堆栈需要输出方法名和类名。泛型实例化需要知道类型参数。序列化需要字段名。这些需求无法通过 native code 中的函数指针满足。

global-metadata.dat 就是为了填这个缺口：它把 .NET metadata 从 DLL 格式中提取出来，重新组织成一份 IL2CPP 专用的二进制格式，供运行时按需查询。

## 文件格式概览

global-metadata.dat 是一个紧凑的二进制文件，由一个固定大小的文件头和多个数据 section 组成。

### 文件头

文件头是一个 `Il2CppGlobalMetadataHeader` 结构体，定义了文件的整体布局：

```cpp
struct Il2CppGlobalMetadataHeader
{
    int32_t sanity;          // magic number，固定值
    int32_t version;         // metadata 格式版本号

    // 每个 section 用一对 offset + size 描述
    int32_t stringLiteralOffset;
    int32_t stringLiteralSize;
    int32_t stringLiteralDataOffset;
    int32_t stringLiteralDataSize;
    int32_t stringOffset;
    int32_t stringSize;
    int32_t eventsOffset;
    int32_t eventsSize;
    int32_t propertiesOffset;
    int32_t propertiesSize;
    int32_t methodsOffset;
    int32_t methodsSize;
    int32_t parameterDefaultValuesOffset;
    int32_t parameterDefaultValuesSize;
    int32_t fieldDefaultValuesOffset;
    int32_t fieldDefaultValuesSize;
    int32_t fieldAndParameterDefaultValueDataOffset;
    int32_t fieldAndParameterDefaultValueDataSize;
    int32_t fieldsOffset;
    int32_t fieldsSize;
    int32_t genericContainersOffset;
    int32_t genericContainersSize;
    int32_t genericParametersOffset;
    int32_t genericParametersSize;
    int32_t typeDefinitionsOffset;
    int32_t typeDefinitionsSize;
    // ... 更多 section 的 offset/size 对
};
```

`sanity` 是一个固定的 magic number（IL2CPP 源码中定义为 `0xFAB11BAF`），用于快速校验文件合法性。`version` 是 metadata 格式版本号，每当 IL2CPP 的 metadata 格式发生变化时递增。

文件头之后，各 section 通过 offset 定位、通过 size 限定范围，紧密排列在同一个二进制流中。这种设计意味着不需要文件系统级别的多文件管理——一次读取或内存映射就能获取全部数据。

### 主要 section

| Section | 内容 | 运行时用途 |
|---------|------|-----------|
| strings | 类型名、方法名、字段名、命名空间名等 UTF-8 字符串 | 反射查询、异常堆栈、日志输出 |
| stringLiteral / stringLiteralData | C# 代码中的字符串字面量 | `ldstr` 指令加载字符串常量 |
| typeDefinitions | 类型定义：所属程序集、基类索引、接口列表、字段范围、方法范围 | 构建 Il2CppClass |
| methods | 方法定义：名字索引、参数信息、返回类型、所属类型、token | 方法查找、反射 |
| fields | 字段定义：名字索引、类型、偏移 | 字段布局、反射 |
| parameters | 参数定义：名字、类型、默认值 | 方法签名查询 |
| properties | 属性定义：名字、getter/setter 方法索引 | 反射 |
| events | 事件定义：名字、add/remove 方法索引 | 反射 |
| genericContainers | 泛型容器：哪些类型/方法有泛型参数 | 泛型实例化 |
| genericParameters | 泛型参数：名字、约束 | 泛型实例化、反射 |
| images | 程序集镜像信息 | Assembly 查找 |
| assemblies | 程序集名称和版本 | Assembly.Load 路径匹配 |

每个 section 内部是定长记录的数组。例如 typeDefinitions section 中的每条记录都是一个固定大小的 `Il2CppTypeDefinition` 结构体，其中的字符串引用用 strings section 中的 offset 表示，类型引用用 typeDefinitions section 中的 index 表示。这种"index 指向 index"的链式引用构成了整份 metadata 的内部拓扑。

## MetadataLoader：加载路径

global-metadata.dat 的加载发生在 IL2CPP 运行时初始化的早期阶段。

### 加载时机

回顾 D3 中 `Runtime::Init` 的初始化链路：

```
il2cpp_init()
  └─ Runtime::Init()
       ├─ os::Initialize()
       ├─ MetadataCache::Initialize()    ← 在这里加载
       │    └─ MetadataLoader::LoadMetadataFile("global-metadata.dat")
       ├─ gc::GarbageCollector::Initialize()
       ├─ Thread::Initialize()
       └─ ...
```

`MetadataCache::Initialize()` 是整个初始化链路中第二个被调用的子系统（在 os 层之后），因为后续的 GC 初始化和类型注册都依赖 metadata 信息。

### 加载方式

`MetadataLoader::LoadMetadataFile` 的实现因平台而异，但核心逻辑可以归纳为两步：

```cpp
// 概念示意
void* MetadataLoader::LoadMetadataFile(const char* fileName)
{
    // 1. 构造文件路径
    std::string path = GetMetadataDirectory() + "/" + fileName;

    // 2. 读取文件内容到内存
    //    - 某些平台使用 mmap（内存映射文件）
    //    - 某些平台整体读入内存（malloc + fread）
    void* data = os::File::ReadAllBytes(path, &size);
    return data;
}
```

在支持内存映射的平台上（桌面、Android），metadata 文件可以通过 `mmap` 映射到进程地址空间，避免一次性分配等量内存。在不支持 `mmap` 的平台上，整个文件被读入一块 `malloc` 分配的内存中。

无论哪种方式，加载完成后 `s_GlobalMetadata` 指针指向文件内容的起始地址，后续所有 section 的访问都是基于这个基地址加上 header 中记录的 offset 做指针运算。

### 解析初始化

加载完成后，`MetadataCache::Initialize()` 解析文件头，验证 magic number 和 version（详见版本兼容性一节），然后根据 header 中各 section 的 offset 和 size 建立指向各 section 的指针：

```cpp
// 概念示意
void MetadataCache::Initialize()
{
    const Il2CppGlobalMetadataHeader* header =
        (const Il2CppGlobalMetadataHeader*)s_GlobalMetadata;

    // 校验
    IL2CPP_ASSERT(header->sanity == kSanity);
    IL2CPP_ASSERT(header->version == kVersion);

    // 建立各 section 的基地址指针
    s_StringData = (const char*)s_GlobalMetadata + header->stringOffset;
    s_TypeDefinitions = (const Il2CppTypeDefinition*)
        ((const char*)s_GlobalMetadata + header->typeDefinitionsOffset);
    s_MethodDefinitions = (const Il2CppMethodDefinition*)
        ((const char*)s_GlobalMetadata + header->methodsOffset);
    s_FieldDefinitions = (const Il2CppFieldDefinition*)
        ((const char*)s_GlobalMetadata + header->fieldsOffset);
    // ... 其他 section 同理

    // 分配类型缓存数组
    int typeCount = header->typeDefinitionsSize
        / sizeof(Il2CppTypeDefinition);
    s_TypeInfoTable = (Il2CppClass**)calloc(typeCount, sizeof(Il2CppClass*));
}
```

这一步完成后，MetadataCache 就拥有了对 global-metadata.dat 中所有 section 的随机访问能力。后续的类型查找、方法查找都是 O(1) 的数组索引操作。

## 与 runtime 的绑定

global-metadata.dat 中的数据如何和运行时的 `Il2CppClass`、`MethodInfo` 等结构体关联？答案是 index 绑定。

### index 引用模型

il2cpp.exe 在转换代码时，为每个类型、方法、字段分配了一个全局唯一的整数索引（TypeDefinitionIndex、MethodIndex、FieldIndex 等）。这些索引同时出现在两个地方：

1. global-metadata.dat 的各 section 中，索引就是记录在数组中的位置
2. 转换后的 C++ 代码中，类型引用和方法引用以索引常量的形式出现

运行时通过索引做桥接：C++ 代码持有索引 → MetadataCache 用索引访问 global-metadata.dat 中的对应记录 → 构建出 `Il2CppClass` 或 `MethodInfo`。

```
转换后的 C++ 代码                    global-metadata.dat
┌─────────────────────┐              ┌──────────────────────┐
│ TypeDefinitionIndex  │──── 索引 ───→│ typeDefinitions[i]   │
│       = 42           │              │  .nameIndex = 1087   │
│                      │              │  .namespaceIndex = 56│
│                      │              │  .methodStart = 200  │
│                      │              │  .methodCount = 15   │
└─────────────────────┘              └──────────────────────┘
                                              │
                                     strings[1087] → "Player"
                                     strings[56]   → "Game"
```

### 延迟绑定

D3 中已经分析过 `Il2CppClass` 的惰性初始化机制。global-metadata.dat 的绑定同样是延迟的。

运行时启动时，`s_TypeInfoTable` 被分配为全零数组。只有在代码第一次访问某个类型时，MetadataCache 才会从 global-metadata.dat 中读取该类型的定义，构建完整的 `Il2CppClass`：

```cpp
Il2CppClass* MetadataCache::GetTypeInfoFromTypeDefinitionIndex(
    TypeDefinitionIndex index)
{
    if (s_TypeInfoTable[index])
        return s_TypeInfoTable[index];

    // 首次访问：从 metadata 构建
    const Il2CppTypeDefinition* def = s_TypeDefinitions + index;

    Il2CppClass* klass = (Il2CppClass*)calloc(1, sizeof(Il2CppClass));
    klass->name = s_StringData + def->nameIndex;
    klass->namespaze = s_StringData + def->namespaceIndex;
    klass->method_count = def->methodCount;
    // ... 其余字段

    s_TypeInfoTable[index] = klass;
    return klass;
}
```

这种设计意味着 global-metadata.dat 虽然在启动时被整体加载到内存中，但其中的数据并不会被一次性全部解析。一个包含上万个类型定义的项目，如果一次运行只实际使用了其中两千个类型，那么只有这两千个类型对应的 metadata 记录会被真正读取和解析。

`Il2CppClass` 上的字段——`name`、`methods`、`fields` 等——最终都指向 global-metadata.dat 在内存中的对应位置。字符串类字段（如 `name`）直接指向 strings section 中的 UTF-8 字节，不做额外拷贝。这也是为什么 global-metadata.dat 的内存需要在整个进程生命周期中保持有效——释放它等于释放了所有 `Il2CppClass` 的名字和描述信息。

## 加密与保护

### 默认状态：不加密

Unity 原生构建产出的 global-metadata.dat 不做任何加密或混淆。文件头的 magic number 和 section 布局都是公开的，任何了解其格式的工具都可以直接解析。

这意味着两件事：

一是逆向工具可以从 global-metadata.dat 中提取出完整的类型信息。Il2CppDumper 和 Cpp2IL 是两个主流的开源工具，它们通过解析 global-metadata.dat 和 native binary 的符号，能够还原出接近 C# 源码级别的类型和方法声明。对于发布到终端设备上的游戏，这意味着代码逻辑的结构信息（类名、方法名、字段名）对逆向者是透明的。

二是 global-metadata.dat 的替换或篡改可以被检测到，但默认不做检测。如果有人替换了包体中的 global-metadata.dat（例如用修改过的版本），只要 magic number 和 version 匹配，runtime 会正常加载——这可能导致运行时行为异常但不会在校验阶段被拦截。

### HybridCLR 商业版的加密方案

HybridCLR 商业版提供了 global-metadata.dat 加密作为其多层保护策略的第一层。其 7 层加密体系中，global-metadata.dat 加密负责防止逆向工具直接解析 AOT 层的类型信息。

加密的基本思路是在构建时对 global-metadata.dat 的内容做变换（加密或结构重排），同时修改 libil2cpp 中的 `MetadataLoader::LoadMetadataFile` 实现，使其在读取文件后先做逆变换（解密）再交给 `MetadataCache::Initialize` 解析。对于运行时代码而言，解密后的数据和未加密版本完全一致，不影响正常运行。

具体的加密策略和实现属于 HybridCLR 商业版的技术细节，将在 HCLR-29 中展开。

## 与其他 runtime 的 metadata 存储对比

global-metadata.dat 是 IL2CPP 独有的设计。其他 .NET runtime 的 metadata 存储方式和它有本质区别。

### CoreCLR

CoreCLR 直接读取 DLL 文件中的 metadata stream。ECMA-335 规范定义了 PE/COFF 格式中 CLI metadata 的存储结构——`#Strings`、`#Blob`、`#GUID`、`#~`（压缩 metadata tables）等 heap 和 table 直接存在于 DLL 二进制中。CoreCLR 的 metadata 加载器解析这些结构，不需要任何预处理或格式转换。

### Mono

Mono 的策略和 CoreCLR 一致——直接解析 DLL 中的标准 ECMA-335 metadata。Mono 在 Unity 编辑器模式下作为运行时使用时，加载的就是原始的 C# 编译产出 DLL。

### LeanCLR

LeanCLR 同样直接解析 DLL 格式的 metadata。作为一个面向 H5/小游戏的精简运行时，它的 metadata 加载路径更简单——不需要处理 AOT 编译产物，直接从 DLL 的 metadata tables 中按需读取类型和方法信息。

### IL2CPP 的独特性

IL2CPP 是这四个 runtime 中唯一一个把 metadata 从标准 DLL 格式中提取出来、预处理成独立二进制文件的方案。这个设计选择是 AOT 管线的必然结果：

DLL 中的 metadata 和 IL 字节码是绑定在一起的。IL2CPP 丢弃了 IL 字节码（用 C++ 代码替代），DLL 格式中的 metadata tables 也随之失去了载体。与其在运行时携带一份"只有 metadata 没有 IL"的残缺 DLL，不如把 metadata 提取成一份专用格式的独立文件——这就是 global-metadata.dat 的由来。

```
CoreCLR / Mono / LeanCLR：
  DLL = IL 字节码 + metadata tables
  runtime 直接读 DLL

IL2CPP：
  DLL → il2cpp.exe → C++ 代码 + global-metadata.dat
  runtime 读 global-metadata.dat（DLL 不再存在于包体中）
```

这个对比也解释了为什么 CoreCLR 和 Mono 天然支持 `Assembly.Load`（加载新 DLL 就等于加载新代码 + 新 metadata），而 IL2CPP 原生不支持——它的运行时只有 metadata 加载能力，没有 IL 执行能力。

## 版本兼容性

global-metadata.dat 的文件头中有一个 `version` 字段，libil2cpp 的代码中有一个编译期常量 `kIl2CppMetadataVersion`。两者必须完全相等。

### 校验逻辑

```cpp
// MetadataCache::Initialize 中的校验（概念示意）
IL2CPP_ASSERT(header->sanity == kIl2CppGlobalMetadataSanity);
IL2CPP_ASSERT(header->version == kIl2CppMetadataVersion);
```

`kIl2CppGlobalMetadataSanity` 和 `kIl2CppMetadataVersion` 都是编译期常量，在构建 libil2cpp 时就被烘焙进了 native binary。global-metadata.dat 在构建时写入同一版本的值。两者来自同一次 Unity 构建，所以天然匹配。

### 不匹配的后果

如果 `version` 不匹配，`IL2CPP_ASSERT` 失败，进程直接 `abort()`。在 Android 上表现为 SIGABRT，在 Windows 上表现为进程终止。

这是一个刻意的 fast-fail 设计，而不是设计缺陷。原因有两条：

第一，version 不匹配意味着 metadata section 的内部结构已经变了。不同版本的 IL2CPP 可能增删 section、改变记录字段的布局。如果强行继续解析，指针运算会指向错误的内存位置，产生比崩溃更难追查的数据损坏。

第二，IL2CPP 没有实现跨版本兼容逻辑。不存在"旧版本 runtime 兼容读取新版本 metadata"的降级路径。提前终止是唯一安全的选择。

### 版本不匹配的常见场景

版本不匹配通常出现在以下情况：

- 热更新框架错误替换了 global-metadata.dat（这个文件属于 AOT 层，不能热更）
- CI/CD 流程中 libil2cpp.so 和 global-metadata.dat 来自不同的构建版本
- 手动替换包体文件时搞混了版本

崩溃发生在应用启动阶段，在任何业务代码执行之前。崩溃栈通常指向 `MetadataCache::Initialize` 附近。这是和其他启动崩溃（如 Assembly 加载失败、AOT 泛型缺失）区分的关键特征。

## 收束

global-metadata.dat 的角色可以用一句话概括：它是 IL2CPP 管线中 .NET metadata 的运行时载体。

il2cpp.exe 在转换 IL 代码的过程中，把 DLL 中的 metadata tables 提取出来，重新组织成一份以 offset/size 对定位的二进制格式。运行时通过 MetadataLoader 将其加载到内存，MetadataCache 根据文件头建立各 section 的指针索引。`Il2CppClass` 的构建、方法信息的查询、字符串字面量的加载——这些运行时操作最终都落到 global-metadata.dat 在内存中的对应位置上。

延迟绑定是这套设计的核心策略。metadata 文件在启动时整体加载，但 section 中的记录按需解析。首次访问某个类型时，MetadataCache 才从 typeDefinitions section 中读取定义、从 strings section 中获取名字，构建出完整的 `Il2CppClass` 并缓存。这让启动速度和内存占用保持在合理范围，不会因为项目中有上万个类型定义而在启动阶段全部解析。

与 CoreCLR、Mono、LeanCLR 直接读取 DLL 中 metadata stream 的方式不同，IL2CPP 是唯一一个把 metadata 预处理成独立二进制文件的方案。这是 AOT 管线丢弃 IL 字节码之后的必然选择——代码的载体从 DLL 变成了 native binary，metadata 的载体也必须从 DLL 中独立出来。

版本兼容性的 fast-fail 设计确保了 metadata 文件和 runtime 的一致性。version 不匹配时直接终止进程，不尝试兼容解析，避免了更难定位的数据损坏问题。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 -- IL2CPP 模块
- 位置：IL2CPP-D4
- 上一篇：[IL2CPP-D3 libil2cpp runtime：MetadataCache、Class、Runtime 三层结构]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}})
- 下一篇：IL2CPP-D5 泛型代码生成

**相关阅读**

- [IL2CPP-D1 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})
- [IL2CPP-D2 il2cpp.exe 转换器：IL → C++ 代码生成策略]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}})
- [IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})
- [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
- [ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
