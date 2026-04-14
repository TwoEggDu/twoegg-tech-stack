---
title: "横切对比｜体积与嵌入性：从 50MB CoreCLR 到 300KB LeanCLR"
slug: "runtime-cross-size-embedding-50mb-to-300kb"
date: "2026-04-14"
description: "五个 .NET runtime 在体积和嵌入性维度的完整对比：CoreCLR 的 50MB 全量运行时与 PublishTrimmed 裁剪策略、Mono 的 10-15MB 中间地带、IL2CPP 的运行时 2-3MB 但 GameAssembly 可达 50-100MB、HybridCLR 在 IL2CPP 基础上增加 1-2MB、LeanCLR 的 600KB 极致轻量与 300KB 裁剪下限。从嵌入 API 设计、平台适配成本、构建链复杂度、WASM 体积四个维度横切对比五种嵌入策略，并覆盖 LTO、dead code elimination、wasm-opt、Brotli 等体积优化手段。"
weight: 87
featured: false
tags:
  - ECMA-335
  - CoreCLR
  - IL2CPP
  - LeanCLR
  - Mono
  - Size
  - Embedding
  - Comparison
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> 一个 .NET runtime 的体积从 300KB 到 50MB，差距超过 150 倍。这个差距不是实现质量的差异，而是设计目标的差异——全功能桌面运行时和极致嵌入式解释器面对的约束完全不同。

这是 .NET Runtime 生态全景系列横切对比篇第 8 篇（CROSS-G8），也是整个横切对比模块的完结篇，同时标志着 Phase 4 的收尾。

CROSS-G7 对比了程序集加载与热更新——五个 runtime 怎么引入新代码、能不能卸载旧代码。程序集加载解决的是代码的"进入与退出"问题。体积与嵌入性面对的是另一个维度的问题：runtime 本身能不能足够小、足够简单地嵌入到宿主环境中。

这个问题在 H5 小游戏、WebAssembly、嵌入式设备、游戏引擎内嵌脚本系统等场景中尤为关键——这些场景对 runtime 的体积有硬约束，超出限制就无法部署。

## Runtime 体积为什么重要

体积约束来自三类场景：

**H5 / 小游戏。** 微信小游戏的主包限制是 20MB（分包可扩展），首包加载时间直接影响用户留存。WebAssembly 模块的下载和编译时间与体积成正比。一个 50MB 的 runtime 在 3G 网络下需要 30 秒以上才能加载完成。

**嵌入式设备。** IoT 设备、游戏主机的内存和存储空间有限。某些嵌入式 Linux 平台的总存储只有 64MB——runtime 占掉一半存储就失去了嵌入的意义。

**游戏引擎内嵌。** 游戏引擎需要一个脚本运行时来执行游戏逻辑。引擎的核心二进制本身可能只有 5-10MB，如果脚本运行时比引擎还大，体积比例就失衡了。

体积不仅是存储占用的问题。更大的 binary 意味着更多的内存映射页、更多的 instruction cache 压力、更长的启动时间。在性能敏感的游戏场景中，这些间接影响不可忽视。

## CoreCLR — ~50MB 全功能运行时

CoreCLR 是五个 runtime 中体积最大的。一个 self-contained 部署（把 runtime 打包进应用）的最小体积约 50-60MB。

### 体积构成

```
CoreCLR self-contained 部署 (~50-60MB)
├── coreclr.dll / libcoreclr.so      ~5-8MB    (runtime 核心：JIT、GC、类型系统)
├── clrjit.dll / libclrjit.so        ~3-5MB    (RyuJIT 编译器)
├── System.Private.CoreLib.dll       ~10-12MB  (核心 BCL)
├── 其他 BCL 程序集                   ~20-30MB  (System.*, Microsoft.*)
└── native 依赖                       ~2-5MB    (ICU、OpenSSL 等)
```

runtime 核心（coreclr + clrjit）约 8-13MB。真正占大头的是 BCL——.NET 的标准库包含了从基础类型到网络、加密、序列化、XML 处理的完整功能集。

### 裁剪后的体积

.NET SDK 提供 `PublishTrimmed` 和 Native AOT 两种方式缩减体积：

**PublishTrimmed：** 使用 ILLink 裁剪未使用的 BCL 类型。裁剪效果取决于应用实际引用了多少 BCL 功能。一个简单的控制台应用裁剪后可以从 ~60MB 降到 ~10-15MB。

**Native AOT（.NET 8+）：** 将应用和 runtime 一起 AOT 编译为单个 native binary。没有 JIT 编译器、没有 IL 加载器、BCL 也被 AOT 编译。一个最小的 Native AOT 应用可以低至 ~1.5-3MB。

Native AOT 的体积下限接近 IL2CPP 的 runtime 体积，但 Native AOT 是面向服务端和桌面应用的，不面向游戏引擎嵌入场景。

### 嵌入场景的可行性

CoreCLR 提供了 hosting API（`coreclr_initialize`、`coreclr_execute_assembly`），允许 native 应用嵌入 CoreCLR 并执行 .NET 代码。但嵌入成本较高：

- 需要部署完整的 runtime 目录结构（runtime binary + BCL DLLs）
- JIT 编译器的存在增加了初始化时间和内存占用
- CoreCLR 假设自己是进程的主 runtime——多个 CoreCLR 实例共存不在设计考虑中
- 构建链复杂：需要 .NET SDK、NuGet 包管理、deps.json 解析

CoreCLR 的定位是"应用级运行时"，不是"组件级嵌入运行时"。

## Mono — ~10-15MB 中间地带

Mono 比 CoreCLR 轻量，但仍然不算小。

### 体积构成

```
Mono 嵌入部署 (~10-15MB)
├── libmonosgen-2.0.so / mono-2.0-sgen.dll   ~3-5MB    (runtime + SGen GC)
├── JIT 编译器（内置）                         包含在上述 binary 中
├── BCL 程序集                                 ~5-10MB   (mscorlib + 必要的 System.*)
└── native 依赖                                ~1-2MB
```

### Unity 中的 Mono

Unity Editor 内嵌的 Mono runtime 约 15-20MB（包含调试支持和完整 BCL）。Unity Player 中的 Mono 可以通过 stripping 裁剪到约 5-8MB。

Unity 从 Mono 转向 IL2CPP 的原因不只是性能——Mono 的 JIT 在 iOS 上不可用（Apple 禁止运行时代码生成），Full AOT 模式的兼容性问题多，Mono 的维护成本也在上升。

### 嵌入 API

Mono 历史上一直以"可嵌入"为卖点。它提供了一套 C API：

```c
MonoDomain* domain = mono_jit_init("MyApp");
MonoAssembly* assembly = mono_domain_assembly_open(domain, "MyApp.dll");
MonoImage* image = mono_assembly_get_image(assembly);
MonoClass* klass = mono_class_from_name(image, "MyNamespace", "MyClass");
MonoMethod* method = mono_class_get_method_from_name(klass, "Main", 0);
mono_runtime_invoke(method, NULL, NULL, NULL);
```

这套 API 比 CoreCLR 的 hosting API 更适合游戏引擎嵌入——可以精确控制类型查找、方法调用、对象创建。Unity 引擎内部就是通过这套 API 与 C# 脚本交互的。

Mono 的嵌入性比 CoreCLR 好，但 10-15MB 的基础体积在小游戏和 WebAssembly 场景下仍然偏大。

## IL2CPP — runtime 小，但产物大

IL2CPP 的体积特征和前两者完全不同：runtime 本身很小，但最终包体取决于 C# 代码量。

### 体积构成

```
IL2CPP 构建产物
├── libil2cpp.a / .so              ~2-3MB     (runtime 核心：GC、类型系统、icall)
├── global-metadata.dat            ~2-10MB    (metadata，取决于类型数量)
├── GameAssembly.dll / .so         ~10-100MB  (AOT 编译后的 native code)
└── il2cpp_data/                   ~1-5MB     (资源数据)
```

**runtime 本身（libil2cpp）只有 2-3MB。** 这个体积和 LeanCLR 在同一个量级。libil2cpp 不包含 JIT 编译器、不包含 IL 加载器——它只是一个运行时服务层：GC 管理（BoehmGC）、类型系统操作、icall 分发、线程管理。

**真正的体积大户是 GameAssembly。** il2cpp.exe 将所有 C# 代码转换为 C++ 再编译为 native code。一个中等规模的 Unity 项目（10 万行 C#）的 GameAssembly 可以轻松达到 50-100MB。泛型实例化是膨胀的主要来源——D5 分析过，每个值类型泛型实例都需要独立的 native 代码。

**global-metadata.dat 的体积可控。** metadata 文件的大小取决于项目中的类型、方法、字符串数量。多数项目在 2-10MB 范围内。

### Managed Code Stripping 的效果

D8 详细分析了裁剪机制。裁剪对体积的影响主要体现在两个方面：

**BCL 裁剪。** 不使用的 BCL 类型不会被 il2cpp.exe 转换，不产生 native code。High stripping 可以把 BCL 的 native code 贡献从 20-30MB 减少到 5-10MB。

**项目代码裁剪。** Medium 和 High stripping 可以移除项目代码中未调用的方法，进一步减少 GameAssembly 体积。但裁剪过度会引发运行时异常。

### 嵌入场景的可行性

IL2CPP 不是一个可以独立嵌入的 runtime。它是 Unity 构建管线的一部分，深度绑定 Unity 的构建系统、项目结构和引擎 API。没有独立的嵌入 API——不能把 libil2cpp 单独拿出来用于非 Unity 项目。

这是 IL2CPP 和其他 runtime 的根本区别：它不是一个通用的 .NET runtime，而是 Unity 引擎的内部组件。

## HybridCLR — IL2CPP + ~1-2MB

HybridCLR 在 IL2CPP 基础上增加了约 1-2MB 的体积。

### 增量体积构成

```
HybridCLR 增量
├── 解释器核心 (Interpreter)        ~500KB-1MB   (CIL 解释器 + 指令分发)
├── InterpreterImage               ~200-400KB   (热更 DLL 解析和 metadata 注入)
├── Bridge / Transform             ~200-400KB   (AOT-Interpreter 桥接层)
└── 补充 metadata                   ~100-500KB   (supplementary metadata)
```

总增量控制在 1-2MB，对于一个已经有 50-100MB GameAssembly 的项目来说微不足道。这也是 HybridCLR 的设计优势之一——极低的体积开销换来了热更新能力。

### 热更 DLL 的体积

热更新的 DLL 文件本身是 CIL 字节码，体积通常远小于等量的 native code。一个 10 万行 C# 的热更 DLL 可能只有 1-3MB，而相同代码 AOT 编译后的 native code 可能有 30-50MB。

这个体积差异在热更新下载场景中是显著的优势——用户只需要下载 1-3MB 的更新包，而不是重新下载整个 native binary。

## LeanCLR — ~600KB 极致轻量

LeanCLR 是五个 runtime 中体积最小的，也是唯一一个设计目标就是"极致轻量嵌入"的方案。

### 体积构成

```
LeanCLR 完整构建 (~600KB)
├── 解释器核心                     ~300KB   (双解释器 + 指令 transform)
├── Metadata 解析                  ~100KB   (CliImage + RtModuleDef)
├── 类型系统                       ~100KB   (RtClass + VTable + 泛型膨胀)
├── 内存管理                       ~50KB    (MemPool arena + GC 接口)
└── Internal Calls                ~50KB    (61 个 icall 实现)
```

**无 JIT、无 AOT 编译器。** LeanCLR 是纯解释器——没有代码生成组件，省掉了 JIT 编译器通常占据的 3-5MB。

**无 BCL 绑定。** LeanCLR 不内嵌 BCL。标准库以 DLL 文件形式存在，按需加载。runtime binary 中不包含任何 BCL 代码。

**零外部依赖。** 纯 C++17 实现，不依赖 Boost、ICU、OpenSSL 或任何第三方库。编译出来的 binary 只依赖 C++ 标准库。

### 裁剪到 ~300KB

LeanCLR 的 ~600KB 是全功能构建（包含所有 icall、完整的 metadata 解析、双解释器路径）。通过编译选项裁剪：

- 移除调试支持 → 约 -100KB
- 移除不需要的 icall → 约 -50KB
- 启用 `-Os` 优化和 LTO → 约 -100KB
- 单线程模式（移除线程同步原语） → 约 -50KB

裁剪后的 LeanCLR 可以低至约 300KB。这个体积在 WebAssembly 场景下极有竞争力——300KB 的 wasm 模块加上 Brotli 压缩后可以低于 100KB，在 3G 网络下 1 秒内就能加载完成。

### 嵌入 API

LeanCLR 提供纯 C API 用于嵌入：

```c
// 初始化 runtime
LeanClrContext* ctx = leanclr_create();

// 加载程序集
leanclr_load_assembly(ctx, "GameLogic.dll");

// 查找并调用方法
LeanClrMethod* method = leanclr_find_method(ctx, "GameLogic", "Main", "Entry");
leanclr_invoke(method, NULL, 0);

// 释放
leanclr_destroy(ctx);
```

API 设计遵循"最小表面积"原则——初始化、加载、调用、释放，四个操作覆盖基本嵌入需求。没有 domain、ALC、AppDomain 等概念——一个 context 就是一个独立的运行环境。

这种设计让 LeanCLR 的嵌入成本极低：

- 不需要部署额外的 runtime 目录
- 不需要 .NET SDK 或 NuGet
- 不需要理解 .NET 的 hosting 模型
- 一个 `.h` 文件 + 一个 `.a/.so` 文件就能完成集成

## 嵌入性对比

体积只是嵌入性的一个维度。完整的嵌入性评估还需要考虑 API 设计、平台适配成本和构建链复杂度。

### API 设计

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| API 类型 | C hosting API | C embedding API | 无独立 API | 继承 IL2CPP | C API |
| 初始化复杂度 | 高（需要 TPA 列表、配置） | 中（domain + assembly） | N/A | N/A | 低（单 context） |
| 类型交互 | 通过委托 / function pointer | 直接操作 MonoObject | C++ 直接调用 | C++ 直接调用 | 通过 invoke API |
| 多实例 | 不推荐 | 支持（多 domain） | 不支持 | 不支持 | 支持（多 context） |
| 脚本热加载 | 支持（ALC 卸载） | 部分 | 不支持 | 支持 | 支持 |

### 平台适配成本

| 平台 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| Windows / Linux / macOS | 原生支持 | 原生支持 | Unity 构建 | Unity 构建 | 编译即用 |
| iOS | 不支持 | Full AOT | Unity 构建 | Unity 构建 | 编译即用 |
| Android | 支持（.NET MAUI） | 支持 | Unity 构建 | Unity 构建 | 编译即用 |
| WebAssembly | Blazor（~10MB） | 支持（~5-8MB） | 不直接支持 | 不直接支持 | 支持（~300KB） |
| 嵌入式 Linux | 需要移植 | 需要移植 | 不支持 | 不支持 | 编译即用 |
| 游戏主机 | 不支持 | Unity 定制版 | Unity 构建 | Unity 构建 | 需适配 |

LeanCLR 在平台适配上的优势来自两点：纯 C++17 无外部依赖意味着任何有 C++17 编译器的平台都能编译；没有 JIT 意味着不需要处理平台的可执行代码限制（iOS、游戏主机）。

### 构建链复杂度

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| SDK 依赖 | .NET SDK | Mono SDK / .NET SDK | Unity Editor | Unity Editor + HybridCLR | CMake |
| 包管理 | NuGet | NuGet | Unity Package Manager | UPM + NuGet | 无 |
| 构建步骤 | dotnet publish | xbuild / dotnet | Unity Build | Unity Build + 额外步骤 | cmake --build |
| 交叉编译 | 需要 RID 配置 | 需要 cross 工具链 | Unity 内置 | Unity 内置 | CMake toolchain file |
| CI/CD 复杂度 | 中 | 中 | 高 | 高 | 低 |

## 五方对比总表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| runtime 体积 | ~50MB (self-contained) | ~10-15MB | ~2-3MB (libil2cpp) | ~3-5MB (libil2cpp + interp) | ~600KB (~300KB min) |
| 裁剪后 | ~10-15MB (trimmed) ~1.5-3MB (Native AOT) | ~5-8MB | N/A（取决于代码量） | N/A | ~300KB |
| 最终包体 | ~10-60MB | ~5-20MB | ~20-120MB (GameAssembly 主导) | ~20-120MB + 热更 DLL | ~300KB + DLLs |
| WASM 体积 | ~10MB (Blazor) | ~5-8MB | 不直接支持 | 不直接支持 | ~300KB (Brotli 后 <100KB) |
| 嵌入 API | C hosting API | C embedding API | 无 | 无 | C API |
| 平台适配 | 需要 .NET SDK | 需要 Mono SDK | Unity 绑定 | Unity 绑定 | CMake |
| 构建链 | NuGet + dotnet | NuGet + xbuild | Unity Build Pipeline | Unity + HybridCLR 工具链 | CMake |
| 多实例 | 不推荐 | 支持 | 不支持 | 不支持 | 支持 |
| **源码锚点** | `dotnet publish --self-contained` | `mono/mini/` (~10MB runtime) | `libil2cpp.a` (~2-3MB) | `hybridclr/` (~1-2MB additional) | `src/runtime/` (73K LOC → ~600KB) |

## 体积优化策略

不同 runtime 可以使用的体积优化手段：

### 通用手段

**LTO（Link-Time Optimization）。** 在链接阶段跨编译单元优化，消除未使用的函数、内联跨文件调用、合并相同代码段。对所有 native binary 有效——CoreCLR 的 Native AOT、IL2CPP 的 GameAssembly、LeanCLR 的 runtime binary 都可以受益。

LTO 的典型效果是 10-20% 的体积减少，但编译时间可能增加 2-5 倍。

**Dead Code Elimination。** 编译器级别的未使用代码消除。与 LTO 配合效果更好——LTO 提供了跨编译单元的全局视图，让编译器能发现单文件编译时无法确定的死代码。

### IL2CPP 特有

**Managed Code Stripping。** D8 分析的 UnityLinker 裁剪。这是 IL2CPP 最重要的体积优化手段——裁剪级别从 Minimal 到 High，体积差异可达 30-50%。

**IL2CPP Code Generation 选项。** Unity 提供 `Faster runtime` 和 `Faster (smaller) builds` 两个代码生成选项。后者通过减少内联和代码特化来缩小 GameAssembly 体积，代价是运行时性能略降。

### WebAssembly 特有

**wasm-opt。** Binaryen 工具链提供的 WebAssembly 优化器。对 wasm 模块执行多轮优化——死代码消除、常量折叠、控制流简化。典型效果是 10-30% 的体积减少。

**Brotli 压缩。** WebAssembly 模块的分发通常使用 Brotli 压缩。Brotli 在 wasm 二进制上的压缩率通常在 60-70%——一个 300KB 的 wasm 模块压缩后约 90-100KB，一个 10MB 的 wasm 模块压缩后约 3-4MB。

**Asyncify / Stack Switching。** 某些 wasm 场景需要协程支持，Asyncify 转换会增加 wasm 体积（通常 30-50%）。WebAssembly Stack Switching 提案（实验阶段）可以在不膨胀代码的情况下实现类似功能。

### LeanCLR 特有

**编译时功能裁剪。** LeanCLR 的 CMake 构建系统支持通过编译选项移除不需要的功能模块：

```cmake
# 移除调试支持
set(LEANCLR_DEBUG OFF)
# 单线程模式
set(LEANCLR_SINGLE_THREAD ON)
# 裁剪 icall
set(LEANCLR_MINIMAL_ICALLS ON)
```

这种编译时裁剪比运行时裁剪更彻底——被移除的代码不会出现在最终 binary 中，不存在"裁剪后运行时找不到"的问题。

## 收束 — Phase 4 完结篇

五个 .NET runtime 的体积跨度从 50MB 到 300KB，背后是五种完全不同的设计权衡：

**CoreCLR 选择了完整性。** JIT 编译器、完整的 BCL、丰富的运行时服务——这些组件加起来构成了 50MB 的基线体积。Native AOT 可以把这个数字压到 1.5-3MB，但放弃了 JIT 的灵活性。

**Mono 选择了平衡。** 比 CoreCLR 小一半以上，保留了 JIT 和 AOT 两种执行模式，提供了可用的嵌入 API。作为 Unity 的老 runtime，它证明了 10-15MB 级别的嵌入是可行的，但也暴露了这个体积在 WebAssembly 场景下的局限。

**IL2CPP 选择了性能。** runtime 核心极小（2-3MB），但 AOT 编译的 native code 可以很大。它的体积问题不是 runtime 本身，而是 C# 代码到 native code 的膨胀——特别是泛型实例化导致的代码膨胀。

**HybridCLR 选择了增量最小化。** 在 IL2CPP 已有体积上只增加 1-2MB，获得了热更新能力。热更 DLL 本身是 CIL 字节码，体积远小于等量 native code，在更新分发场景中有显著的带宽优势。

**LeanCLR 选择了极致轻量。** 放弃 JIT、放弃 AOT、放弃完整 BCL、放弃多线程（可选），用纯解释器架构换来了 300-600KB 的极小体积。这个体积让 WebAssembly 场景下的 .NET runtime 嵌入从"勉强可行"变成了"毫无压力"。

体积与嵌入性的对比揭示了一个底层规律：runtime 的体积与其承诺的能力成正比。CoreCLR 承诺了"在任何情况下都能高效运行任何 .NET 代码"——这个承诺需要 JIT、完整 GC、完整 BCL 来兑现，体积是代价。LeanCLR 承诺了"在极小体积下能正确运行简单的 .NET 代码"——这个承诺只需要解释器和最小类型系统，体积自然极小。

没有"最好的"体积——只有最适合场景的体积。服务端应用不在乎 50MB，桌面应用也不太在乎 15MB，但 WebAssembly 模块的每一个 KB 都直接影响首屏加载时间。理解每个 runtime 的体积特征和优化手段，才能在具体场景中做出正确的技术选型。

## 系列位置

这是横切对比篇第 8 篇（CROSS-G8），也是横切对比模块的完结篇和 Phase 4 的收尾篇。

八篇横切对比覆盖了"同一份 ECMA-335 规范在五个 runtime 中的实现分化"这条主线的八个核心横截面：

| 编号 | 主题 | 对比维度 |
|------|------|----------|
| CROSS-G1 | Metadata 解析 | 解析策略、缓存策略、惰性加载 |
| CROSS-G2 | 类型系统实现 | MethodTable vs Il2CppClass vs RtClass |
| CROSS-G3 | 方法执行 | JIT vs AOT vs Interpreter vs 混合 |
| CROSS-G4 | GC 实现 | 分代精确 vs 保守式 vs 协作式 vs stub |
| CROSS-G5 | 泛型实现 | 共享 vs 特化 vs 全泛型共享 |
| CROSS-G6 | 异常处理 | 两遍扫描 vs setjmp/longjmp vs 解释器展开 |
| CROSS-G7 | 程序集加载与热更新 | 静态绑定 vs 动态加载 vs 卸载 |
| CROSS-G8 | 体积与嵌入性 | 50MB 全功能 vs 300KB 极致轻量 |

从 metadata 解析、类型系统、方法执行这三个基础层环节，到 GC、泛型、异常处理这三个实现差异最大的维度，再到程序集加载和体积嵌入性这两个直接关系到工程选型的维度——八个切面构成了一张完整的 runtime 能力地图。每个 runtime 在这张地图上的位置，就是它的技术定位和适用边界。
