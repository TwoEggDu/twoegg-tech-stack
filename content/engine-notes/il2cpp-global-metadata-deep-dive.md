---
date: "2026-03-26"
title: "IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么"
description: "独立专题。拆清楚 IL2CPP 构建产物的分工：libil2cpp 是什么、GameAssembly 是什么、global-metadata.dat 存什么、IL2CPP 启动时如何校验 metadata 版本、版本不匹配为什么直接 abort 而不是报错，以及这三个文件和 HybridCLR 的关系。"
weight: 60
featured: false
tags:
  - "IL2CPP"
  - "Unity"
  - "NativeCrash"
  - "global-metadata"
  - "HybridCLR"
  - "Symbols"
  - "Debug"
---
> 很多人用了 IL2CPP 好几年，但从没想过那个 `global-metadata.dat` 里装的是什么，或者为什么它不能热更。这篇把这个问题彻底拆开。

---

## 三个文件的分工

IL2CPP 打包产物里，三类文件承担了不同的职责：

```
Android APK 解压后：
  lib/arm64-v8a/
    libil2cpp.so          ← IL2CPP 运行时 + 所有 C# 代码编译结果
  assets/bin/Data/Managed/Metadata/
    global-metadata.dat   ← 运行时元数据（名字、类型信息、引用表）
```

Windows Standalone：

```
  GameAssembly.dll        ← 同 libil2cpp.so，IL2CPP 运行时 + C# 编译结果
  <GameName>_Data/il2cpp_data/Metadata/
    global-metadata.dat
```

iOS（Xcode 工程）：

```
  GameAssembly.dylib（或静态库）← 同上
  Data/Managed/Metadata/
    global-metadata.dat
```

| 文件 | 内容 | 大小特征 |
|------|------|---------|
| `libil2cpp.so` / `GameAssembly` | IL2CPP C++ 运行时代码 + C# 代码编译后的机器码 | 通常几十 MB 到上百 MB |
| `global-metadata.dat` | 符号名称、类型结构、方法表、引用信息（见下文详解） | 几 MB 到十几 MB |
| `.so` / `.dll` 的 strip 版（包体） | 去掉调试符号的机器码 | 比带符号版小，但仍包含运行时需要的所有代码 |

---

## libil2cpp.so / GameAssembly 里有什么

IL2CPP 把两件事合并进了同一个二进制：

**1. IL2CPP C++ 运行时**

这是 Unity 自己实现的托管运行时，负责：
- 内存管理（GC）
- 类型系统（类型加载、转换、instanceof）
- 线程管理
- 异常处理（`il2cpp::vm::Exception::*`）
- 反射 API（`il2cpp::vm::Reflection::*`）
- P/Invoke 桥接

这部分代码在所有 Unity 项目里几乎相同（同一 Unity 版本），是 Unity 发布的闭源 C++ 库。

**2. C# 代码编译结果**

IL2CPP 把项目里所有 C# 代码转译成 C++，再由平台 C++ 编译器（Clang / MSVC）编译进同一个二进制。

转译后的函数名形如：
```
GameManager_Update_m1234567890       // void GameManager.Update()
Player_TakeDamage_m9876543210_gshared // 泛型 shared 版本
```

这就是为什么符号化后能看到 C# 类名和方法名——它们被编码进了 C++ 函数名里。

**重点**：`libil2cpp.so` 里的代码不依赖 `global-metadata.dat` 里的字符串信息就能运行——代码里直接使用数字索引（token）引用类型和方法，字符串名字只在需要反射、日志、异常消息时才查。

---

## global-metadata.dat 里有什么

`global-metadata.dat` 是一个紧凑的二进制格式文件，它存放的是 IL2CPP 运行时需要的**名字和结构信息**。

### 文件头（Header）

文件开头是一个固定结构的 Header：

```cpp
// 来自 IL2CPP 源码（UPM 包 com.unity.il2cpp 或 Unity 源码），简化版
struct Il2CppGlobalMetadataHeader
{
    int32_t  sanity;          // 魔术数，固定值，用于快速验证文件合法性
    int32_t  version;         // metadata 格式版本号

    // 以下是各数据段的 offset 和 size（每个段都有一对）
    int32_t  stringLiteralOffset;
    int32_t  stringLiteralCount;
    int32_t  stringLiteralDataOffset;
    int32_t  stringLiteralDataCount;
    int32_t  eventsOffset;
    int32_t  eventsCount;
    int32_t  propertiesOffset;
    int32_t  propertiesCount;
    int32_t  methodsOffset;
    int32_t  methodsCount;
    // ... 以及 fields, typeDefinitions, images, assemblies 等
    // 完整字段随 Unity 版本而略有变化
};
```

`sanity` 是一个固定魔术数（IL2CPP 源码里定义为常量），用来快速排除"这根本不是 metadata 文件"的情况。

`version` 是 **metadata 格式版本号**，这是运行时校验的核心字段。

### 各数据段

Header 之后是多个数据段，每个段都通过 Header 里的 offset 和 size 定位。主要段：

| 段名 | 内容 |
|------|------|
| `stringLiteral` | 代码里的字符串字面量（`"Hello World"` 这类） |
| `stringLiteralData` | 字符串字面量的实际字节 |
| `events` | 事件定义（名字、类型） |
| `properties` | 属性定义 |
| `methods` | 方法定义（名字、参数信息、返回类型） |
| `fields` | 字段定义（名字、类型、偏移） |
| `typeDefinitions` | 类型定义（类名、基类、接口、方法列表起始位置等） |
| `images` | 程序集（Assembly）的镜像信息 |
| `assemblies` | 程序集的名字和版本 |
| `genericContainers` | 泛型容器（泛型类型、泛型方法的参数信息） |
| `genericParameters` | 泛型参数名（`T`、`TResult` 等） |
| `customAttributeData` | 自定义 Attribute 数据 |
| `metadataUsageLists` | 运行时元数据使用清单（反射、泛型实例等） |

一句话概括：**`global-metadata.dat` 存的是 IL2CPP 运行时要用到的所有"名字"和"结构描述"**——机器码里用数字引用的东西，在这里可以查到对应的名字和完整定义。

---

## IL2CPP 启动时的 metadata 加载与校验

IL2CPP 的初始化入口（简化版流程）：

```
Unity 引擎启动
  → il2cpp_init()                          // IL2CPP 对外入口
    → il2cpp::vm::Runtime::Init()          // 运行时初始化
      → il2cpp::vm::MetadataCache::Initialize()  // 元数据初始化
        → MetadataLoader::LoadMetadataFile()     // 读取文件
          → 验证 Header（sanity + version）
          → 建立各段的内存映射
        → 建立类型缓存（TypeInfoArray 等）
      → RegisterAllStrings()               // 注册字符串字面量
      → RegisterAllTypes()                 // 注册所有类型
      → il2cpp::vm::Assembly::Register()  // 注册所有 Assembly
```

### 版本校验的代码逻辑

`MetadataCache::Initialize()` 里的校验（概念示意，基于 IL2CPP 开源部分）：

```cpp
static void Initialize()
{
    const Il2CppGlobalMetadataHeader* header =
        (const Il2CppGlobalMetadataHeader*)s_GlobalMetadata;

    // 第一关：magic 校验
    IL2CPP_ASSERT(header->sanity == kIl2CppGlobalMetadataSanity);

    // 第二关：版本校验
    IL2CPP_ASSERT(header->version == kIl2CppMetadataVersion);

    // 两者都是编译期常量，编译进 libil2cpp.so 里
    // metadata 文件里的值必须和这两个常量完全一致
    // 不一致 → IL2CPP_ASSERT 失败 → abort()
}
```

`kIl2CppMetadataVersion` 是一个整数常量（如 Unity 2022.3 里大约是 29），在每次 IL2CPP 格式有变化时递增。`kIl2CppGlobalMetadataSanity` 是一个固定魔术数。

这两个常量在**编译 libil2cpp.so 时**就被烘焙进二进制里了。`global-metadata.dat` 在**打包时**写入同样的值。两者来自同一次 Unity 版本，所以匹配。

### 为什么版本不匹配就直接 abort，而不是报错继续

`IL2CPP_ASSERT` 在 Release 构建里通常实现为 `abort()`（或平台相关的强制终止），而不是抛异常。

这是有意为之的设计，理由有两条：

**1. metadata 版本不匹配意味着"内存布局已经错了"**

`global-metadata.dat` 里的 `typeDefinitions` 段存的是所有类型的字段偏移、方法索引等结构信息。`libil2cpp.so` 里的机器码在访问对象字段时，直接用这些偏移值做指针运算。如果 metadata 格式变了，偏移表的排列方式可能变了，继续运行只会产生更难追查的内存损坏。

**2. 没有降级路径**

不同版本的 metadata 格式可能增删了字段、改变了段结构，IL2CPP 运行时没有实现跨版本兼容逻辑。在这种情况下，提前终止比带着损坏状态继续跑更安全——至少能给出明确的崩溃位置，而不是几秒后在完全无关的地方崩溃。

---

## 版本不匹配的崩溃特征

在 Android 上，`global-metadata.dat` 版本不匹配的崩溃发生在**应用启动时**，在 Unity 任何业务代码执行之前：

```
E CRASH: signal 6 (SIGABRT), code -1 (SI_QUEUE)
E CRASH: pid: 1234, tid: 1234
E CRASH: backtrace:
E CRASH:   #00 pc ...  libc.so (abort+...)
E CRASH:   #01 pc ...  libil2cpp.so   ← abort 在 il2cpp 里被调用
E CRASH:   #02 pc ...  libil2cpp.so
E CRASH:   #03 pc ...  libunity.so
```

符号化后，`#01` 通常指向 `il2cpp::vm::MetadataCache::Initialize()` 或附近的函数。

**区分于其他启动崩溃**：

| 崩溃类型 | 信号 | 发生时机 | 特征 |
|---------|------|---------|------|
| metadata 版本不匹配 | SIGABRT | Unity 初始化阶段，业务代码之前 | abort 在 MetadataCache 附近 |
| Assembly 加载顺序错 | 无（托管异常） | 热更 DLL 加载阶段 | logcat 有 TypeLoadException |
| AOT 泛型死循环 | SIGSEGV | 业务逻辑执行中 | 帧地址重复 + FullySharedGeneric |

---

## global-metadata.dat 和 HybridCLR 的关系

HybridCLR 的热更代码（DLL）在运行时加载，它有自己的元数据（DLL 里的 PE/CLI 元数据）。但 HybridCLR **不替换也不修改** `global-metadata.dat`。

两者的职责边界：

```
global-metadata.dat  ─── 描述 AOT 代码里的所有类型和方法
                               ↑
                    libil2cpp.so 启动时加载，之后只读

热更 DLL              ─── 描述热更代码里的类型和方法
                               ↑
                    HybridCLR 在 Assembly.Load() 时动态解析
```

所以：
- 热更新 DLL 的元数据变了 → 只换 DLL 文件，正确
- `global-metadata.dat` 的元数据变了 → 必须重新打包 APK，不能热更

---

## 符号化时 global-metadata.dat 的作用

符号化 crash 时，`llvm-addr2line` 查的是 **libil2cpp.so 里的 DWARF 调试信息**，里面存的是机器码地址到 C++ 函数名的映射——这些函数名已经包含了 C# 类名（因为 IL2CPP 把类名编进了 C++ 函数名里）。

`global-metadata.dat` **不参与**符号化过程。

但有两个工具会用到它：

| 工具 | 用途 |
|------|------|
| IL2CPP 逆向工具（如 Il2CppDumper） | 从 `global-metadata.dat` 提取所有类/方法名，恢复被 strip 的符号 |
| HybridCLR 运行时 | 解析热更 DLL 里对 AOT 类型的引用时，通过 metadata 查 token |

---

## 小结

```
libil2cpp.so / GameAssembly
  = IL2CPP 运行时 + C# 代码机器码
  = 能独立执行，但只认 token（数字）

global-metadata.dat
  = 所有名字 + 类型结构 + 方法信息
  = 机器码里的 token 在这里查到对应的名字和定义
  = Header 里有版本号，必须和 libil2cpp.so 里的编译期常量匹配

版本校验
  = IL2CPP 启动时第一件事
  = 不匹配 → IL2CPP_ASSERT → abort()
  = 这是有意设计的 fast-fail，防止带错误状态继续跑

HybridCLR 边界
  = 热更 DLL 有自己的元数据
  = global-metadata.dat 属于 AOT 层，不参与热更
```

---

## 延伸阅读

- [HybridCLR 崩溃定位专题]({{< relref "engine-notes/hybridclr-crash-analysis.md" >}}) — 包含 metadata 版本不匹配崩溃的识别方法
- [崩溃分析 Android 篇]({{< relref "engine-notes/crash-analysis-01-android.md" >}}) — 如何获取和符号化 Android native crash
