---
title: "ECMA-335 基础｜术语约定：runtime、toolchain、metadata、execution engine 的边界"
slug: "ecma335-terminology-conventions"
date: "2026-04-14"
description: "86 篇文章中同一个词在不同语境下含义不同。这篇术语约定页按易混淆词对组织，定义 runtime vs toolchain、metadata vs stream vs table、execution engine vs interpreter vs JIT、hot update vs hot reload vs hot fix 等核心术语的精确边界，作为全系列的参照锚点。"
weight: 9
featured: false
tags:
  - "ECMA-335"
  - "Terminology"
  - "CLR"
  - "Convention"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> 如果一个系列的不同文章里，同一个词指代了不同的东西，读者建立的心智模型就会在某一篇突然崩塌。术语约定不是学术洁癖，而是 86 篇文章能被串联阅读的前提。

这是 .NET Runtime 生态全景系列的术语约定页，位于所有正文之前。

## 为什么需要术语约定

这个系列覆盖 5 个 CLR 实现（CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR），横跨规范层、实现层、工程层。同一个词在不同层次里可能指代完全不同的东西。

最典型的例子是"IL2CPP"。它可以指：

- **转换器**——`il2cpp.exe`，构建时把 IL 代码翻译成 C++ 源码的工具
- **运行时**——`libil2cpp`，游戏运行时提供类型系统、GC、线程管理的 runtime 库
- **整套方案**——Unity 的 IL2CPP scripting backend，涵盖从 C# 编译到 native binary 的完整管线

一篇文章说"IL2CPP 不支持运行时加载新程序集"，指的是 runtime 层。另一篇说"IL2CPP 会把泛型方法展开成多份 C++ 代码"，指的是转换器层。如果不区分，两句话放在一起读就会产生因果混乱。

类似的歧义在 metadata、execution engine、hot update 等词上也存在。这篇页面把这些易混淆词对逐组拆清，后续所有文章遵循这里的约定。

## Runtime vs Toolchain

**Runtime**（运行时）——程序执行时驻留在进程中的组件。负责类型加载、方法执行、内存管理、异常处理、线程调度。

- CoreCLR：`coreclr.dll` / `libcoreclr.so` + `clrjit.dll`
- Mono：`libmonosgen-2.0.so` / `mono-2.0-sgen.dll`
- IL2CPP：`libil2cpp`（编译进 `GameAssembly.dll` / `libil2cpp.so`）
- HybridCLR：在 IL2CPP runtime 上叠加的解释器模块
- LeanCLR：`leanclr` 单库（编译为 `.a` / `.so` / `.wasm`）

**Toolchain**（工具链）——构建时运行的工具。把源码变成目标格式后就退出，不参与运行时执行。

- Roslyn（`csc.exe`）：C# → IL
- `il2cpp.exe`：IL → C++ 源码
- UnityLinker：程序集裁剪
- `mono --aot`：IL → native（Mono AOT 编译器）

**歧义高发区：IL2CPP 这个名字。** Unity 文档和社区讨论中，"IL2CPP"经常不加区分地指代转换器和 runtime。本系列的约定：

- 说"IL2CPP 转换器"或 `il2cpp.exe` 时，指构建时工具
- 说"IL2CPP runtime"或 `libil2cpp` 时，指运行时组件
- 说"IL2CPP 方案"或"IL2CPP scripting backend"时，指整套管线

同一段落中如果两者都涉及，会明确标注。

## Metadata vs Metadata Stream vs Metadata Table

这三个词在 ECMA-335 语境下是嵌套关系，不是同义词。

**Metadata**——CLI 元数据体系的总称。包含程序集中所有类型定义、方法签名、字段布局、引用关系的结构化描述。ECMA-335 Partition II 定义了它的完整规范。当文章说"runtime 加载 metadata"时，指的是这个整体。

**Metadata Stream**——物理存储层的概念。metadata 在 PE 文件中以多个 stream 的形式存放：

| Stream 名 | 内容 |
|-----------|------|
| `#~` 或 `#-` | 压缩/非压缩的 metadata 表数据 |
| `#Strings` | 字符串堆（类名、命名空间、方法名） |
| `#US` | 用户字符串堆（代码中的字符串字面量） |
| `#Blob` | 二进制数据（方法签名、字段签名、自定义属性） |
| `#GUID` | GUID 堆 |

**Metadata Table**——逻辑结构层的概念。`#~` stream 内部组织为多张表，每张表的行结构和列定义由 ECMA-335 Partition II 第 22-24 节规定：

- TypeDef 表：每行描述一个类型定义
- MethodDef 表：每行描述一个方法定义
- FieldDef 表：每行描述一个字段定义
- MemberRef 表：跨程序集的成员引用

**约定：** 本系列中"metadata"无修饰时指整体概念。需要区分物理层和逻辑层时，会明确使用"stream"或"table"。Pre-A（ECMA-A1）已经用 ildasm 示例展示了这三层的对应关系。

## Execution Engine vs Interpreter vs JIT

**Execution Engine**（执行引擎）——runtime 中负责执行方法体的子系统的泛称。不指定具体执行策略。ECMA-335 Partition I 12.1 使用 VES（Virtual Execution System）这个术语，execution engine 是它的通俗叫法。

**Interpreter**（解释器）——逐条读取 IL 指令（或转换后的中间指令），在软件层面模拟执行的方式。不生成 native code。

- LeanCLR 的双解释器：HL-IL 指令先 transform 为 LL-IL 指令，由低层解释器执行
- HybridCLR 的解释器：直接解释 IL 指令
- Mono 的 mint/interp：Mono 内置的解释器模式

**JIT**（Just-In-Time Compiler）——在方法首次被调用时，将 IL 编译为 native code，后续调用直接执行 native code。

- CoreCLR 的 RyuJIT
- Mono 的 Mini JIT

**约定：** "execution engine"在本系列中只作为泛称使用，不暗示任何具体策略。讨论具体实现时会直接说"解释器"或"JIT"。

## Hot Update vs Hot Reload vs Hot Fix

这三个术语在中文社区中混用严重，但它们在技术层面指向不同的机制。

**Hot Update**（热更新）——运行时替换或新增代码逻辑，不需要重新发布客户端。HybridCLR 的核心能力。技术实现：运行时加载新的 DLL，解释执行其中的 IL 代码。已加载的旧代码不卸载（社区版），新代码通过 metadata 注册覆盖旧的方法分派。

**Hot Reload**（热重载）——卸载旧程序集并重新加载新版本。HybridCLR 商业版提供此能力（需要 `AssemblyLoadContext` 语义支持）。和 hot update 的关键区别：旧代码会被卸载，不只是被覆盖。.NET 6+ 的 Hot Reload（`dotnet watch`）也属于这个范畴，但实现机制是 EnC（Edit and Continue），通过 delta metadata 修改已加载的程序集。

**Hot Fix**（热修复）——不是一个独立的技术机制，而是一个使用场景：用 hot update 或 hot reload 的能力修复线上 bug。"热修复"描述的是目的，不是手段。

**约定：** 本系列中"热更新"专指技术机制层面的 hot update，不用它来指 hot fix 场景。讨论 HybridCLR 商业版的程序集卸载能力时用"热重载"。

## Assembly vs Module vs Image

**Assembly**（程序集）——部署、版本管理和安全性的基本单元。一个 assembly 有自己的名称、版本号、文化信息和公钥标记（strong name）。ECMA-335 Partition II 6.1 定义了 assembly 的身份模型。

**Module**（模块）——assembly 的物理组成部分。ECMA-335 允许一个 assembly 包含多个 module（multi-module assembly），但实际工程中几乎所有 assembly 都是单 module 的——一个 `.dll` 文件 = 一个 assembly = 一个 module。

**Image**——runtime 加载后的内存表示。不同 runtime 用不同的数据结构：

- CoreCLR：`PEImage` / `PEFile`
- Mono：`MonoImage`
- IL2CPP：`Il2CppImage`（运行时从 `global-metadata.dat` 构建）
- LeanCLR：`CliImage`（从 PE 文件最小化解析构建）

**约定：** 说"程序集"时指 ECMA-335 规范层面的逻辑概念。说"image"时指 runtime 内部的加载后表示。不会把两者混用。"模块"在本系列中很少单独出现，因为 multi-module assembly 在实际工程中几乎绝迹。

## AOT vs JIT vs Interpreter

三种方法执行策略的精确边界。

**AOT**（Ahead-Of-Time Compilation）——在构建时将 IL 编译为 native code。运行时不再需要编译步骤，直接执行预编译的 native code。

- IL2CPP：IL → C++ → native（通过 C++ 编译器）
- Mono Full AOT：IL → native（通过 LLVM 后端）
- .NET Native AOT：IL → native（通过 ILC + RyuJIT AOT 模式）

AOT 的核心约束：构建时必须能看到所有需要执行的代码。运行时不能生成新代码。这导致反射 `Emit`、动态泛型实例化等能力受限。

**JIT**（Just-In-Time Compilation）——运行时按需将 IL 编译为 native code。首次调用有编译开销，后续调用直接执行 native code。

- CoreCLR 的 RyuJIT
- Mono Mini JIT

JIT 的核心优势：能处理 AOT 无法预见的代码（运行时首次遇到的泛型组合、反射调用的方法）。代价是需要随 runtime 携带编译器组件。

**Interpreter**（解释执行）——逐指令模拟执行 IL（或转换后的中间表示），不生成 native code。

- LeanCLR：双层解释器（HL-IL → LL-IL → 执行）
- HybridCLR：IL 直接解释执行
- Mono interp：IL 解释执行

解释器的核心优势：零 native code 生成，完全灵活。代价是执行速度比 native code 慢 10-100 倍。

**混合策略：** 现实中多数 runtime 不是纯粹使用一种策略。CoreCLR 有 Tiered Compilation（先快速 JIT，后优化 JIT）。HybridCLR 是 AOT + Interpreter 混合（AOT 部分走 IL2CPP，热更部分走解释器）。Mono 同时支持 JIT、AOT、Interpreter 三种模式。

**约定：** 本系列讨论执行策略时，会明确标注当前语境是哪种模式。不会笼统地说"Mono 的执行方式"——要指明是 JIT 模式、Full AOT 模式还是 Interpreter 模式。

## 贯穿全系列的参考类约定

Pre-A（ECMA-A1）定义了一组参考类，后续所有 runtime 模块的文章都用这组类型来展示实现差异：

```csharp
public interface IHittable
{
    void TakeDamage(int amount);
}

public class Unit : IHittable
{
    public int hp;
    public virtual void TakeDamage(int amount) { hp -= amount; }
}

public class Player : Unit
{
    public List<Item> inventory = new();
    public override void TakeDamage(int amount) { /* 带护甲计算 */ }
    public T GetItem<T>(int index) where T : Item { return (T)inventory[index]; }
}

public class Item
{
    public string name;
    public int weight;
}
```

这组类型覆盖的 CLI 概念：

| 概念 | 对应元素 | 涉及的 runtime 实现维度 |
|------|---------|------------------------|
| 接口 | `IHittable` | 接口分派（vtable / itable） |
| 继承 | `Unit → Player` | 虚方法覆盖、方法解析 |
| 泛型方法 | `GetItem<T>` | 泛型实例化、共享与特化 |
| 值类型字段 | `hp: int`, `weight: int` | 内存布局、字段偏移 |
| 引用类型字段 | `inventory`, `name` | GC 引用追踪、对象头 |
| 约束 | `where T : Item` | 约束检查、泛型代码生成 |

**约定：** 当文章需要一个具体的类型示例来说明某个 runtime 机制时，优先使用这组参考类。如果讨论的机制不适合用这组类型（比如值类型的特定布局问题），会在文中说明为什么另选示例。

## 系列位置

这是 .NET Runtime 生态全景系列的术语约定页（A0），位于所有正文之前。

ECMA-335 基础层的正文从 ECMA-A1（Pre-A：CLI Metadata 基础）开始。本页不包含技术分析，只定义术语边界。后续文章中遇到术语使用歧义时，可以回到这里对照。
