---
date: "2026-04-14"
title: "LeanCLR 调研报告｜架构总览与源码地图：从零实现一个 600KB 的 CLR"
description: "从 clone 编译到跑通测试，再到 73K LOC 源码逐模块摸底：metadata 解析、双解释器、对象模型、类型系统、内存管理、internal calls。给后续 LeanCLR 深度分析建立全景地图。"
weight: 70
featured: false
tags:
  - LeanCLR
  - CLR
  - Runtime
  - Architecture
  - ECMA-335
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> LeanCLR 是 HybridCLR 同一团队的另一条技术路线：不是在 IL2CPP 里面补解释器，而是从零实现一个只有 600KB 的完整 CLR。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 1 篇。

本文不深入单个模块，而是先做一次完整的调研：能不能编译、能不能跑、源码结构长什么样、核心设计决策是什么。目标是给后续拆解每个子系统建立一张够用的全景地图。

## LeanCLR 是什么

LeanCLR 来自 Code Philosophy（代码哲学），和 HybridCLR 是同一团队的产品。

但两者的技术路线完全不同。

HybridCLR 的思路是：IL2CPP 已经把大部分基础设施搭好了，在它内部补一个解释器就能实现热更新。所以 HybridCLR 始终是 IL2CPP 世界的一部分，不能脱离 IL2CPP 独立存在。

LeanCLR 走的是另一条路：从零开始实现一个完整的 CLR。不依赖 IL2CPP，不依赖 Mono，不依赖任何现有运行时。

最终产物是一个纯 C++17 编写的运行时，编译后体积约 600KB（单线程版），可裁剪到约 300KB。整个项目 MIT 开源。

目标场景也不一样。HybridCLR 解决的是 Unity 热更新问题，而 LeanCLR 瞄准的是资源受限平台：H5、微信小游戏、嵌入式设备，以及任何需要一个足够小的 CLR 运行时的场景。

用一句话概括两者的分工：

`HybridCLR 是 IL2CPP 的补丁，LeanCLR 是 IL2CPP 的替代。`

## 为什么值得研究

CLR 是一个被广泛使用但很少有人真正拆解过实现细节的运行时。

CoreCLR 有几百万行代码，工程复杂度极高，不适合作为学习对象。Mono 虽然体量小一些，但代码年代久远、历史包袱重。对于想理解"一个 CLR 到底是怎么实现的"这个问题，中文社区始终缺少一个合适的分析目标。

LeanCLR 提供了一个几乎理想的学习窗口：

- **体量可控**。73K 行 C++ 代码，295 个运行时文件。一个人可以通读。
- **结构清晰**。模块边界明确，没有过度抽象，也没有历史遗留的妥协。
- **设计独特**。双解释器、三级 IL 变换、精确协作式 GC 设计，这些决策在其他 CLR 实现中很少见。
- **对比价值高**。和 HybridCLR 放在一起看，能非常清楚地理解"在已有运行时里补能力"和"从零构建运行时"这两条路线各自的取舍。

此外，LeanCLR 的存在本身就回答了一个很多人没意识到的问题：

`实现一个最小可用的 CLR，到底需要哪些模块？`

这个问题的答案，对理解 CoreCLR 和 Mono 的架构同样有用。

## 编译与运行

调研的第一步是确认项目能不能跑起来。

以下是实际的编译和测试过程。

### 环境

- Visual Studio 18 (2026 Community)
- CMake 4.3.1
- Windows 10 x64

### 编译 runtime

LeanCLR 使用标准的 CMake 构建流程：

```
cmake -B build -G "Visual Studio 18 2026" -A x64
cmake --build build --config Release
```

编译完成后生成 `leanclr.lib`，这是运行时的静态链接库。

整个过程没有额外依赖需要安装，纯 C++17 标准库即可编译通过。

### 编译 startup demo

仓库附带了一个 startup 示例程序，用于验证最基础的加载能力：

```
> startup.exe
Loading corlib...
Corlib loaded successfully.
```

这一步验证的是：assembly 加载链路能走通，metadata 能正确解析。

### 编译 lean CLI 并运行 CoreTests

lean 是 LeanCLR 自带的命令行宿主程序。CoreTests 是一组基础功能测试。

```
> lean.exe CoreTests.dll
========================================
  LeanCLR CoreTests
========================================

[PASS] Hello, World!
[PASS] 你好，世界！
[PASS] Exception: System.InvalidOperationException: Test exception
         at CoreTests.ExceptionTest.Run()
[PASS] Stack trace verified
[PASS] String concatenation
[PASS] Array operations
[PASS] Basic arithmetic

All tests passed.
```

输出包含了几个关键验证点：

- 基础字符串输出（包括中文 UTF-8）
- 异常抛出与捕获（try/catch 链路）
- 栈追踪信息生成
- 字符串操作和数组操作
- 基本算术运算

### BCL 兼容性

LeanCLR 对两套 BCL 做了兼容性验证：

- Unity 2019.4 ~ 6000.3 所有 LTS 版本的 il2cpp BCL：全部测试通过
- Mono 4.8 BCL：99.95% 兼容

这意味着 LeanCLR 可以直接加载 Unity 项目构建产出的 DLL，不需要额外的适配层。

### 小结

项目可编译、可运行、基础功能稳定。这个结论是后续所有源码分析的前提。

## 源码结构地图

LeanCLR 的运行时源码总计约 73,000 行 C++，分布在 295 个文件中。

下面这张表是按目录统计的模块分布。如果你准备跟源码，建议先把这张表的结构记住，后面每个模块的分析都会回到这里。

| 目录 | 文件数 | LOC | 职责 |
|------|--------|-----|------|
| `metadata/` | 25 | 8,603 | ECMA-335 metadata 解析：stream、table、token 解析、程序集加载 |
| `interp/` | 19 | 31,829 | 双解释器核心：HL-IL 与 LL-IL 的 transform 和执行 |
| `vm/` | 65 | 13,319 | 类型系统、方法调用、运行时状态管理 |
| `icalls/` | 61 | 14,300 | 61 个 internal call 实现（System.Type、System.Array 等） |
| `intrinsics/` | 18 | 1,240 | 性能关键方法的原生实现（Math、String、Memory 等） |
| `gc/` | 2 | 76 | GC 接口定义（当前为 stub，委托给宿主 malloc） |
| `alloc/` | 5 | 402 | 内存分配器：对象分配、数组分配、字符串分配 |
| `core/` | 2 | 281 | 基础设施：错误码定义和 Result 类型 |
| `public/` | 2 | 252 | C API：嵌入式宿主的公共接口 |

几个值得注意的比例：

- `interp/` 独占 43% 的代码量。双解释器是 LeanCLR 最重的模块。
- `icalls/` 的 61 个文件覆盖了 BCL 中最常用的 internal call，这是让 C# 代码真正能跑起来的关键。
- `gc/` 只有 76 行。GC 当前是 stub 实现，意味着内存管理完全委托给宿主。这是一个明确的设计选择，不是遗漏。

### 两个版本

LeanCLR 提供两个编译目标：

| 版本 | 线程模型 | GC 策略 | 体积 | 适用场景 |
|------|----------|---------|------|----------|
| Universal | 单线程 | 精确 GC（设计目标） | ~600KB | H5、小游戏、嵌入式 |
| Standard | 多线程 | 保守 GC | 更大 | 通用桌面/移动端 |

Universal 版是当前主推版本。单线程模型简化了大量并发问题，使得精确 GC 的实现成为可能。

## 核心架构亮点

以下是 LeanCLR 在架构层面最值得关注的 5 个设计决策。每个亮点后续都会有专题文章展开，这里只给出足够建立全景理解的描述。

### 三级 IL 变换

LeanCLR 不直接解释 MSIL。

它把 IL 的执行拆成了三级变换：

```
MSIL (256 opcodes) → HL-IL (182 opcodes) → LL-IL (298 opcodes)
```

第一级：MSIL 是 ECMA-335 标准定义的原始 IL，256 个操作码。这是 DLL 文件里存储的格式。

第二级：HL-IL（High-Level IL）压缩到 182 个操作码。这一步做的是语义归一化——把 MSIL 中语义等价但编码不同的指令合并。比如 `ldarg.0`、`ldarg.1`、`ldarg.s`、`ldarg` 在 HL-IL 里可以统一成一个带索引的 `HL_LDARG`。

第三级：LL-IL（Low-Level IL）展开到 298 个操作码。这一步做的是类型特化——把 HL-IL 中的泛型操作按实际类型展开成具体指令。比如 `HL_ADD` 会根据操作数类型展开成 `LL_ADD_I4`、`LL_ADD_I8`、`LL_ADD_R4` 等。

这个设计和 HybridCLR 形成了鲜明对比。HybridCLR 只做一级变换（CIL → HiOpcode），但生成了超过 1000 个特化指令。LeanCLR 选择分两步走，每一步的 opcode 空间都更小、更可控。

两种策略各有取舍：HybridCLR 的单步特化减少了运行时分派层数，但增加了 transform 的复杂度；LeanCLR 的分层变换让每一层的逻辑更简单，但多了一次中间转换的开销。

### 对象模型

LeanCLR 的托管对象在内存中的布局是：

```
[klass*][sync_block*][fields...]
```

- `klass*`：指向 `RtClass`，描述对象的类型信息
- `sync_block*`：同步块指针，用于 lock/Monitor
- `fields...`：实例字段的实际数据

这个布局和 IL2CPP 的对象布局高度一致（IL2CPP 也是 `Il2CppClass*` + 字段），但 LeanCLR 的实现更简洁。

核心类型结构包括：

- `RtObject`：所有托管对象的基础结构
- `RtClass`：类型描述符，相当于 CoreCLR 的 `MethodTable`
- `RtMethodInfo`：方法描述符
- `RtFieldInfo`：字段描述符
- `RtArray`：数组对象（在 `RtObject` 基础上加了长度和元素类型）
- `RtString`：字符串对象（UTF-16 内部表示）

### 方法调用三级 fallback

当 LeanCLR 需要执行一个方法时，它按固定优先级尝试三个执行路径：

```
Intrinsics → Internal Calls → Interpreter
```

第一级：**Intrinsics**。18 个文件定义了性能关键方法的原生实现。如果一个方法有对应的 intrinsic，直接执行 C++ 实现，不进解释器。

第二级：**Internal Calls**。61 个文件覆盖了 BCL 中标记为 `[MethodImpl(MethodImplOptions.InternalCall)]` 的方法。这些是 C# 标准库中必须由运行时提供的底层实现。

第三级：**Interpreter**。如果前两级都不匹配，方法体会被 transform 成 LL-IL，交给解释器逐条执行。

这个三级 fallback 和 CoreCLR 的 JIT/Tiered Compilation 在精神上有相似之处——都是"能走快路就走快路，否则回退到通用路径"。区别在于 LeanCLR 没有 JIT，所有代码最终要么走 intrinsic/icall，要么走解释器。

### 零依赖嵌入

LeanCLR 的整个运行时是纯 C++17，没有任何外部依赖。

这意味着它可以被编译到任何支持 C++17 编译器的平台：Windows、Linux、macOS、iOS、Android、WebAssembly。

嵌入接口通过 `public/` 目录下的 C API 暴露。宿主程序只需要：

1. 调用 `leanclr_init()` 初始化运行时
2. 调用 `leanclr_load_assembly()` 加载程序集
3. 调用入口方法

assembly 加载的内部链路是：

```
leanclr_load_assembly
  → Assembly::load_by_name
    → CliImage::load_streams
      → RtModuleDef::create
```

`CliImage::load_streams` 负责解析 PE/COFF 文件头和 metadata stream（`#~`、`#Strings`、`#Blob`、`#GUID`、`#US`），这正是 ECMA-335 Part II 定义的 metadata 物理布局。

### 精确协作式 GC（设计目标）

LeanCLR Universal 版的设计目标是精确协作式 GC，这和大多数现有实现形成了对比：

- CoreCLR：精确式 GC（标记-压缩），但实现极其复杂
- Mono：支持多种 GC 后端，默认 SGen（精确分代）
- IL2CPP + BoehmGC：保守式 GC，不需要精确的根扫描信息

"精确"意味着 GC 能准确知道哪些位置是对象引用、哪些是普通数值。"协作式"意味着 GC 不需要在任意时刻暂停线程，只在安全点（safepoint）收集。

当前 LeanCLR 的 GC 还是 stub 实现——`gc/` 目录只有 76 行代码，实际的分配直接委托给宿主的 `malloc`。但从对象模型和解释器的设计中，可以看到为精确 GC 预留的接口。

这也是 Universal 版选择单线程的一个重要原因：单线程环境下，精确 GC 的实现难度大幅降低——不需要处理线程挂起、不需要 write barrier 的并发安全、不需要跨线程的根枚举。

### 异常处理

LeanCLR 实现了完整的结构化异常处理：try/catch/finally/fault。

异常子句通过 `RtInterpExceptionClause` 描述，在 metadata 加载阶段从 method body 的 EH table 中解析。解释器在执行时维护一个异常处理栈，匹配最近的 catch 或 finally 子句。

从 CoreTests 的输出可以看到，异常类型、消息和栈追踪都能正确生成。

## 与 HybridCLR 的关键对比

因为两者来自同一团队，把它们放在一起对比能帮助理解两条技术路线的根本差异：

| 维度 | HybridCLR | LeanCLR |
|------|-----------|---------|
| 定位 | IL2CPP 内部补丁 | 独立 CLR 实现 |
| 依赖 | 必须嵌入 IL2CPP | 零依赖 |
| 体积 | 随 libil2cpp 整体 | ~600KB（可裁剪至 ~300KB） |
| IL transform 层级 | CIL → HiOpcode（1 级） | MSIL → HL-IL → LL-IL（2 级） |
| IR opcode 数量 | ~1000+（HiOpcodeEnum） | 182（HL）+ 298（LL）= 480 |
| 设计思路 | 尽量特化，一步到位 | 分层渐进，逐步降低 |
| GC | 复用 IL2CPP 的 BoehmGC | 自实现接口（当前 stub） |
| 对象模型 | 复用 IL2CPP 的 Il2CppObject | 自实现 RtObject |
| 类型系统 | 复用 IL2CPP 的 Il2CppClass | 自实现 RtClass |
| metadata 解析 | 复用 IL2CPP 的 MetadataCache | 自实现 CliImage |
| 目标场景 | Unity 热更新 | H5/小游戏/嵌入式/通用 |

对比中最核心的一行是 **依赖**。

HybridCLR 复用了 IL2CPP 的类型系统、对象模型、GC、metadata 基础设施，所以它能用相对少的代码量实现热更新。但代价是它永远被绑定在 IL2CPP 上——IL2CPP 的版本变化、内部结构调整、Unity 的裁剪策略，都会直接影响 HybridCLR 的适配工作。

LeanCLR 什么都自己实现，工程量大得多，但换来的是完全的独立性。它不关心宿主是 Unity 还是 Unreal 还是一个嵌入式设备的固件。

这两条路线不是竞争关系，而是互补。在 Unity 热更新场景里，HybridCLR 仍然是更成熟、更省事的选择。LeanCLR 解决的是 HybridCLR 覆盖不到的场景。

## 后续系列路线图

这篇调研报告建立的是全景地图。后续每篇文章会深入一个子系统，按以下顺序推进：

| 编号 | 主题 | 目标 |
|------|------|------|
| LEAN-F2 | Metadata 解析 | 拆解 CliImage 如何解析 PE/COFF 和 ECMA-335 metadata stream |
| LEAN-F3 | 双解释器 | 分析 HL-IL 和 LL-IL 的 transform 流程与执行循环 |
| LEAN-F4 | 对象模型 | 拆解 RtObject/RtClass/RtArray/RtString 的内存布局与分配 |
| LEAN-F5 | 类型系统 | 分析泛型实例化、接口、继承、类型解析的实现 |
| LEAN-F6 | 方法调用链 | 从 Intrinsics 到 Internal Calls 到 Interpreter 的完整分派逻辑 |

每篇文章都会以源码为主要依据，不做猜测。

## 收束

LeanCLR 用 73K 行 C++ 回答了一个问题：实现一个最小可用的 CLR 到底需要什么。

答案是 9 个模块：metadata 解析、双解释器、类型系统、对象模型、内存分配、internal calls、intrinsics、异常处理、GC 接口。其中解释器占了 43% 的代码量，internal calls 占了 20%。

从工程角度看，LeanCLR 的价值不在于它现在能替代谁——GC 还是 stub，多线程还在 Standard 版里——而在于它提供了一个完整但可读的 CLR 实现。对于想理解"ECMA-335 标准到底是怎么被实现成一个运行时的"这个问题，这可能是目前最合适的学习入口。

## 系列位置

- 上一篇：无（LeanCLR 模块首篇）
- 下一篇：LEAN-F2 Metadata 解析（计划中）
