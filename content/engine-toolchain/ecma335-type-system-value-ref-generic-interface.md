---
title: "ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束"
date: "2026-04-14"
description: "从 ECMA-335 规范出发，讲清 CLI 类型系统的核心模型：值类型与引用类型的本质区别、泛型的开放/封闭类型、接口与约束的运行时语义，以及这些概念在 IL2CPP、CoreCLR、LeanCLR 中的不同实现。"
weight: 12
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "TypeSystem"
  - "Generics"
  - "Unity"
  - "IL2CPP"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---
> 值类型和引用类型的区别不是"struct vs class"这么简单——它决定了内存布局、参数传递、泛型共享策略，以及最终每个 runtime 会为你的代码生成什么样的机器码。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 3 篇。

Pre-A 解决的是 metadata 的物理结构——TypeDef、MethodDef、token 这些静态记录。Pre-B 解决的是 CIL 指令的执行模型——栈机怎么运转、一条 `add` 做了什么。这篇解决的是第三个前提：当 runtime 看到一个类型时，它怎么判断这个类型的内存布局、怎么决定赋值语义、怎么处理泛型参数。

## 为什么类型系统是地基

CLI 类型系统不是一个"语言特性"层面的概念。它是所有 runtime 实现决策的起点。

CoreCLR 用 JIT，Mono 有解释器和 AOT 两条路，IL2CPP 做全量 AOT，LeanCLR 走双层解释器——这些看起来完全不同的执行策略，最终都要回答同一组问题：

- 一个 `int` 字段在对象里占多大、放在哪
- 一个 `string` 字段在对象里存的是值还是指针
- `List<int>` 和 `List<string>` 能不能共享同一份代码
- 调用接口方法时怎么查到具体实现
- `where T : struct` 这个约束对代码生成有什么影响

这些问题的答案不在"用了什么编译策略"里，而在类型系统的规范定义里。不同 runtime 只是对同一份规范做了不同的实现。

所以类型系统是地基：GC 策略依赖它（要知道哪些字段是引用、哪些是值），JIT/AOT 代码生成依赖它（要知道字段偏移和对象大小），泛型处理依赖它（要知道类型参数是值类型还是引用类型），接口分派依赖它（要知道 vtable 怎么排列）。

把类型系统讲清楚，后面每个 runtime 的实现分析才有共同的参照坐标。

## 值类型与引用类型的本质区别

C# 层面的直觉是：struct 是值类型，class 是引用类型。这个直觉在语法上没错，但它掩盖了真正的区分标准。

ECMA-335 Partition I 8.2.4 定义的区分不在语法层，而在语义层：

**值类型**——类型的实例直接包含数据。变量里存的就是数据本身。赋值 = 复制全部内容。没有对象头，没有独立的堆分配（除非 boxing）。

**引用类型**——类型的实例是一个堆上分配的对象。变量里存的是指向对象的指针（managed reference）。赋值 = 复制指针，两个变量指向同一个对象。对象在堆上有对象头（用于 GC、锁、类型标识）。

用一个具体的例子来说明这个区别对内存布局的影响：

```csharp
struct Vector3 { public float x, y, z; }   // 值类型
class Transform { public Vector3 pos; public string name; }  // 引用类型
```

当 runtime 为 `Transform` 对象分配内存时：

```
Transform 对象的内存布局（引用类型）：
┌──────────────────────┐
│  对象头 (16 bytes)    │  ← SyncBlock index + MethodTable*
├──────────────────────┤
│  pos.x  (4 bytes)    │  ← float，值类型字段直接嵌入
│  pos.y  (4 bytes)    │
│  pos.z  (4 bytes)    │
├──────────────────────┤
│  name   (8 bytes)    │  ← string 是引用类型，这里存的是指针
└──────────────────────┘
```

`Vector3` 是值类型，所以 `pos` 字段的三个 float 直接嵌入 `Transform` 对象的内存块里，总共 12 字节。`string` 是引用类型，所以 `name` 字段存的是一个 8 字节（64 位平台）的指针，指向堆上另一个位置的 string 对象。

这个区别直接影响缓存友好度。值类型字段和宿主对象在同一块连续内存里，访问时不需要额外的指针解引用。引用类型字段意味着一次间接寻址，如果对象分布在堆的不同位置，就可能产生 cache miss。

### 赋值语义

值类型赋值是内容复制，引用类型赋值是指针复制：

```csharp
Vector3 a = new Vector3 { x = 1, y = 2, z = 3 };
Vector3 b = a;    // 复制了 12 字节内容。修改 b 不影响 a。

Transform t1 = new Transform();
Transform t2 = t1; // 复制了 8 字节指针。t1 和 t2 指向同一个对象。
```

这不是"语法糖"或"编译器行为"，而是 CLI 规范定义的类型语义。任何符合 ECMA-335 的 runtime 都必须保证这个语义。

### Boxing 和 Unboxing

当值类型需要被当作引用类型使用时（比如赋值给 `object` 变量，或者作为接口调用的目标），runtime 执行 boxing：

1. 在堆上分配一个对象，大小 = 对象头 + 值类型数据
2. 把值类型的内容复制到这个堆对象里
3. 返回指向这个对象的引用

```csharp
int x = 42;
object boxed = x;   // box: 堆分配 + 复制 4 字节 + 设置对象头
int y = (int)boxed;  // unbox: 从堆对象里复制 4 字节回来
```

Boxing 的成本是真实的：一次堆分配 + 一次内存复制 + 后续 GC 压力。在 Unity 的热路径上，隐式 boxing（比如值类型实现接口后通过接口调用）是常见的性能陷阱。

## ECMA-335 的类型分类体系

ECMA-335 Partition I 8.2.3 把 CLI 的所有类型分成三大类：

### 引用类型（Reference Types）

变量里存的是指向堆对象的 managed pointer。包括：

- **class** — 用户定义的类
- **interface** — 接口类型（可以作为变量类型，存的是实现了该接口的对象的引用）
- **delegate** — 委托，本质是继承自 `System.MulticastDelegate` 的类
- **array** — 数组，包括一维和多维
- **string** — `System.String`，虽然内容不可变，但它是引用类型

### 值类型（Value Types）

变量里直接存数据内容。包括：

- **primitive types** — `int`（System.Int32）、`float`（System.Single）、`bool`（System.Boolean）等
- **user-defined struct** — 用户定义的 struct
- **enum** — 枚举，底层是整数类型，继承自 `System.Enum`

### 指针类型（Pointer Types）

这一类在日常 C# 里不常用，但在 runtime 内部和 unsafe 代码里是核心概念：

- **managed pointer**（`ref`）— 指向托管堆或栈上某个位置的引用，GC 能跟踪。CIL 中用 `&` 表示。C# 中的 `ref` 参数和 `ref` 局部变量就是 managed pointer。
- **unmanaged pointer**（`*`）— 原始指针，GC 不跟踪。只能在 unsafe 上下文中使用。

### 类型继承关系

所有类型最终都继承自 `System.Object`，但值类型和引用类型的继承链路不同：

```
System.Object                        ← 所有类型的根
├── System.ValueType                 ← 所有值类型的基类
│   ├── System.Enum                  ← 所有枚举的基类
│   │   └── MyEnum                   ← 用户定义的枚举
│   ├── System.Int32                 ← int
│   ├── System.Single                ← float
│   └── MyStruct                     ← 用户定义的 struct
├── System.String                    ← string
├── System.Array                     ← 所有数组的基类
│   └── int[]                        ← 具体数组类型
├── System.Delegate
│   └── System.MulticastDelegate     ← 所有委托的基类
│       └── Action<T>                ← 具体委托类型
└── MyClass                          ← 用户定义的 class
```

这里有一个容易混淆的点：`System.ValueType` 本身是一个 class（引用类型），但它的子类（int、float、用户 struct）是值类型。这不是矛盾——ECMA-335 规范明确规定：直接继承自 `System.ValueType`（且不是 `System.ValueType` 本身）的类型，按值类型语义处理。类型的"值/引用"属性不是由基类的属性传递的，而是由规范在继承链上的特殊规则定义的。

## 泛型：开放类型 vs 封闭类型

ECMA-335 Partition II 9 定义了泛型的基本模型。泛型的核心概念是类型参数化——一个类型定义可以带有类型参数，在使用时绑定为具体类型。

### 开放类型与封闭类型

```csharp
// 泛型定义
class List<T> { ... }
class Dictionary<TKey, TValue> { ... }
```

当类型参数未绑定时，这个类型就是**开放类型（open type）**。当所有类型参数都绑定为具体类型时，就是**封闭类型（closed type）**。

| 类型表达式 | 分类 | 说明 |
|-----------|------|------|
| `List<T>` | 开放类型 | T 未绑定 |
| `List<int>` | 封闭类型 | T = int，完全绑定 |
| `List<string>` | 封闭类型 | T = string，完全绑定 |
| `Dictionary<string, T>` | 部分封闭 | TKey 绑定，TValue 未绑定 |
| `Dictionary<string, int>` | 封闭类型 | 两个参数都绑定 |

一个关键规则：**runtime 只能实例化封闭类型**。你不能创建 `List<T>` 的实例——必须告诉 runtime T 是什么，才能确定对象的内存布局、字段偏移、方法实现。

```csharp
// 合法：封闭类型，runtime 知道 T = int
var list = new List<int>();

// 非法：开放类型，runtime 不知道 T 的大小和布局
// var list = new List<T>();  // 编译错误（除非在泛型方法/类型内部）
```

### Metadata 中的泛型表示

泛型实例化信息存储在 metadata 的 TypeSpec 表（表编号 0x1B）和 MethodSpec 表（表编号 0x2B）中。

当 C# 代码写下 `new List<int>()` 时，编译器在 metadata 中生成一条 TypeSpec 记录，编码了 `List<int>` 的签名——基础类型 `List<T>` 的 TypeDef token + 类型参数 `int`。

runtime 在遇到这条 TypeSpec 时，需要做**泛型实例化**：基于泛型定义和具体的类型参数，构造出一个完整的类型描述——包括字段布局、方法列表、vtable。这个过程在不同 runtime 里的实现差异巨大：

- **CoreCLR**：运行时按需 JIT，遇到新的泛型实例化就现场生成
- **IL2CPP**：构建时预生成所有可能的实例化，运行时只查表
- **LeanCLR**：运行时动态膨胀，用 `RtGenericClass` 记录实例化结果

这个差异是后续很多 runtime 行为差异的根源。

## 泛型约束

泛型参数可以附加约束（constraints），限制调用者能传入什么类型。约束定义在 ECMA-335 Partition II 10.1.7。

```csharp
// struct 约束：T 必须是值类型
void Process<T>(T value) where T : struct { ... }

// class 约束：T 必须是引用类型
void Process<T>(T value) where T : class { ... }

// new() 约束：T 必须有无参构造函数
void Create<T>() where T : new() { return new T(); }

// 接口约束：T 必须实现 IComparable<T>
void Sort<T>(T[] items) where T : IComparable<T> { ... }

// 基类约束：T 必须继承自 Component
void FindAll<T>() where T : Component { ... }
```

约束不只是编译器的静态检查工具。它们对 runtime 的代码生成有直接影响。

### 约束影响代码生成

当 runtime 知道 `T : struct` 时，它可以确定：

- T 的实例不需要堆分配
- T 的赋值是值复制
- 对 T 的操作不需要 null check
- 不需要为 T 做 boxing（除非通过接口调用）

当 runtime 知道 `T : class` 时，它可以确定：

- T 的变量存的是指针
- 所有 T 的泛型实例化可以共享同一份代码（因为指针大小固定）

这直接影响了 CoreCLR 的 JIT 和 IL2CPP 的 AOT 代码生成策略。CoreCLR 的 shared generic code 机制依赖于：所有引用类型的指针大小相同，所以 `List<string>` 和 `List<MyClass>` 可以共享一份 JIT 产物，只有值类型实例化才需要独立生成。IL2CPP 的泛型共享规则也遵循同样的逻辑。

### 约束影响类型检查

runtime 在泛型实例化时会验证约束。如果用户通过反射传入一个不满足约束的类型参数，runtime 会抛出 `TypeLoadException`。这个检查在 CoreCLR 中由 `MethodTable::SatisfiesClassConstraints` 完成，在 IL2CPP 中由 `il2cpp::vm::Class::Init` 中的约束验证逻辑完成。

## 接口的运行时语义

接口在 C# 层面的理解常常是"只有方法签名没有实现的类型"。但从 runtime 的角度，接口是一种**类型契约**——它定义了一组方法签名，实现了该接口的类型保证提供这些方法的具体实现。

### 接口方法调用 = 间接分派

普通虚方法调用的分派路径：

```
对象引用 → 对象头中的类型指针 → MethodTable/Il2CppClass → vtable[slot_index] → 目标方法
```

接口方法调用多一层查找：

```
对象引用 → 类型指针 → MethodTable → 接口分派表 → 找到接口对应的 vtable 区域 → vtable[slot_index] → 目标方法
```

为什么多一层？因为虚方法的 slot index 在继承链上是固定的——基类的第 3 个虚方法，子类重写后还是第 3 个 slot。但接口不在继承链上，同一个类型可能实现多个接口，每个接口的方法 slot 需要一个额外的映射来定位。

```csharp
interface IMovable { void Move(); }
interface IDamageable { void TakeDamage(int amount); }

class Enemy : IMovable, IDamageable
{
    public void Move() { ... }
    public void TakeDamage(int amount) { ... }
}
```

当通过 `IMovable` 接口调用 `Move()` 时，runtime 需要先在 `Enemy` 的接口分派表里找到 `IMovable` 接口对应的 vtable 偏移，然后再从那个偏移处取 `Move` 方法的函数指针。

这个额外的查表开销在大多数场景下可以忽略（现代 CPU 的分支预测和缓存通常能覆盖），但在极高频率的调用场景下（比如 ECS 系统中每帧处理上万个实体），接口分派的间接性会成为可测量的成本。

### 默认接口方法

C# 8.0 / .NET Core 3.0 引入了默认接口方法（Default Interface Methods, DIM），允许在接口中提供方法的默认实现：

```csharp
interface ILogger
{
    void Log(string message);
    void LogError(string message) => Log($"ERROR: {message}"); // 默认实现
}
```

从 ECMA-335 的角度，这意味着接口的 MethodDef 可以有 method body（RVA 非零）。runtime 在做接口分派时，如果实现类没有提供覆盖，就回退到接口自身的默认实现。

这个特性在 IL2CPP 中需要特别注意：IL2CPP 的接口分派表需要额外处理默认实现的回退逻辑。Unity 对 DIM 的支持受限于 IL2CPP 版本和 .NET 目标框架的配置。

## 这些概念在不同 runtime 里怎么实现

同一份 ECMA-335 类型系统规范，不同 runtime 的实现决策差异很大。下面用一张表把核心概念的实现对齐：

| 概念 | CoreCLR | IL2CPP | LeanCLR |
|------|---------|--------|---------|
| **值类型字段嵌入** | MethodTable 记录字段偏移和实例大小，EEClass 持有详细布局 | `Il2CppClass` 的 `field_offsets` 数组记录每个字段的偏移 | `RtClass` 的 `instance_size` 记录实例大小 |
| **引用类型对象头** | SyncBlock index + MethodTable* (16 bytes, 64-bit) | `Il2CppObject`: `klass*` + `monitor*` (16 bytes) | `RtObject`: `klass*` + `sync*` (16 bytes) |
| **泛型实例化** | 运行时按需 JIT + shared generic code（引用类型共享，值类型特化） | 构建时生成 + supplementary metadata（引用类型共享 `__Il2CppFullySharedGenericType`，值类型独立生成） | 运行时类型膨胀，`RtGenericClass` 记录实例化参数 |
| **接口分派** | Interface dispatch map + virtual stub dispatch (VSD) | vtable + `interface_offsets` 数组 | vtable + `interface_vtable_offsets` 映射表 |
| **Boxing** | 堆分配 + 复制值 + 设置 MethodTable | 堆分配 + 复制值 + 设置 `klass` | 堆分配 + 复制值 + 设置 `klass` |

几个值得注意的差异：

**对象头大小。** 三个 runtime 的对象头都是 16 字节（64 位平台），但内部字段的用途不同。CoreCLR 的 SyncBlock index 用于锁和哈希码缓存，是一个指向全局 SyncBlock 表的索引。IL2CPP 的 `monitor` 字段更直接地用于 `Monitor.Enter/Exit`。LeanCLR 的 `sync` 字段是类似的设计，但目前实现较为简化。

**泛型实例化的时机。** 这是三个 runtime 之间最大的架构差异之一。CoreCLR 可以在运行时现场 JIT 一个从未见过的泛型实例化——这意味着它不需要提前枚举所有可能的类型参数组合。IL2CPP 必须在构建时确定所有实例化，漏掉的就无法在运行时补充（这正是 HybridCLR 要解决的问题之一）。LeanCLR 作为解释器，在运行时动态膨胀类型信息，不需要预生成 native code，但需要在内存中构建完整的类型描述。

**接口分派优化。** CoreCLR 使用了 Virtual Stub Dispatch (VSD) 技术——第一次接口调用走慢路径做完整查找，然后把结果缓存在一个 dispatch stub 里，后续调用直接命中缓存。IL2CPP 和 LeanCLR 则使用更直接的 vtable + offset 方案，没有运行时缓存优化，但也没有 stub 管理的开销。

## 收束

CLI 类型系统的核心模型可以压到四条规则：

1. **值类型 vs 引用类型的区别在语义层**：值类型的内容直接嵌入容器，赋值 = 复制内容，无对象头；引用类型在堆上分配对象，变量存指针，赋值 = 复制指针。这个区别决定了内存布局、参数传递、GC 扫描策略。

2. **泛型有开放和封闭之分**：runtime 只能实例化封闭类型。泛型实例化信息存在 TypeSpec / MethodSpec 表中。不同 runtime 在"何时实例化"这个问题上做了根本不同的决策——JIT 可以运行时按需生成，AOT 必须构建时预备，解释器走运行时膨胀。

3. **约束不只是编译期检查**：`where T : struct` 让 runtime 知道可以避免 boxing 和 null check，`where T : class` 让 runtime 知道可以共享泛型代码。约束直接影响 JIT/AOT 的代码生成策略。

4. **接口分派比虚方法多一层间接查找**：虚方法通过 vtable slot 直接分派，接口方法需要先查接口分派表找到对应的 vtable 区域，再从那里取方法指针。

这四条规则是后面所有 runtime 实现分析的公共坐标。CoreCLR 的 MethodTable 怎么编码类型信息、IL2CPP 的 `Il2CppClass` 怎么记录字段偏移、LeanCLR 的 `RtGenericClass` 怎么做运行时膨胀——都是对这同一套规范的不同实现。

## 系列位置

- 上一篇：Pre-B CIL 指令集与栈机模型
- 下一篇：ECMA-A4 CLI Execution Model：方法调用约定、虚分派、异常处理模型
