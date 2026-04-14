---
title: "CoreCLR 实现分析｜Reflection 与 Emit：运行时类型查询与动态代码生成"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解反射与动态代码生成的完整实现：RuntimeType 对 MethodTable 的包装、RuntimeMethodInfo 对 MethodDesc 的包装、RuntimeFieldInfo 对 FieldDesc 的包装、MethodInfo.Invoke 的参数打包与安全检查开销、System.Reflection.Emit 的 DynamicMethod / ILGenerator 运行时 IL 生成与 JIT 编译路径、Emit 在 ORM / 序列化 / AOP 中的应用、IL2CPP 缺失 Emit 的根因与替代方案、LeanCLR 的 61 个 icall 反射实现、以及 Source Generator 作为编译期替代的趋势。"
weight: 48
featured: false
tags:
  - CoreCLR
  - CLR
  - Reflection
  - Emit
  - DynamicCode
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> Reflection 把 CoreCLR 的类型系统从编译期的静态结构暴露为运行时可查询的动态对象——RuntimeType 包装 MethodTable，RuntimeMethodInfo 包装 MethodDesc，RuntimeFieldInfo 包装 FieldDesc。Emit 更进一步，允许在运行时生成 IL 并通过 JIT 编译为 native code。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 9 篇。

B8 分析了 CoreCLR 的线程与同步子系统——Thread 结构、Monitor 的 thin lock 升级、ThreadPool 的 hill-climbing 调度。那些是 runtime 管理执行资源的机制。这篇转向另一个维度：运行时对类型系统自身的查询和动态生成能力。Reflection 回答"这个类型有哪些方法"，Emit 回答"能不能在运行时造一个新方法"。

## Reflection 在 CoreCLR 中的位置

Reflection 的实现跨越两层：

**BCL 层（`System.Reflection`）** — 面向 C# 开发者的 API。`Type`、`MethodInfo`、`FieldInfo`、`PropertyInfo` 等抽象类定义了查询接口。CoreCLR 提供的具体实现是 `RuntimeType`、`RuntimeMethodInfo`、`RuntimeFieldInfo` 等内部类型。

**VM 层（`src/coreclr/vm/`）** — 存储实际类型信息的数据结构。B3 分析过的 MethodTable、EEClass、MethodDesc、FieldDesc 是 VM 内部的 native 数据结构。Reflection 的 BCL 类型是这些 native 结构的托管包装。

两层的关系：BCL 的 `RuntimeType` 持有一个指向 VM 层 `MethodTable` 的指针（通过 `RuntimeTypeHandle`）。当 C# 代码调用 `typeof(string).GetMethods()` 时，`RuntimeType` 通过这个指针访问 `MethodTable` → `EEClass` → `MethodDescChunk`，遍历所有 MethodDesc，为每个 MethodDesc 创建一个 `RuntimeMethodInfo` 包装对象并返回。

```
C# 代码层面：
  typeof(string)  →  RuntimeType 对象
  .GetMethods()   →  RuntimeMethodInfo[] 数组

VM 内部：
  RuntimeType.m_handle → MethodTable*
    → EEClass → MethodDescChunk
      → MethodDesc[0] → 包装为 RuntimeMethodInfo
      → MethodDesc[1] → 包装为 RuntimeMethodInfo
      → ...
```

## System.Reflection 的三层包装

B3 深入分析了 CoreCLR 的类型系统数据结构——MethodTable 存储运行时类型信息，EEClass 存储加载时元数据，MethodDesc 描述方法，FieldDesc 描述字段。Reflection 的核心工作是把这些 native 结构包装为托管对象，让 C# 代码能够查询。

### RuntimeType 包装 MethodTable

`RuntimeType` 是 `System.Type` 的 CoreCLR 实现。它是反射体系的入口——几乎所有的反射操作都从获取一个 `Type` 对象开始。

`RuntimeType` 内部持有一个 `RuntimeTypeHandle`，这个 handle 本质上是一个指向 MethodTable 的指针。当调用 `typeof(MyClass)` 或 `obj.GetType()` 时，runtime 从对象头部的 MethodTable 指针出发，找到或创建对应的 `RuntimeType` 实例。

每个已加载类型的 `RuntimeType` 实例在 runtime 中是唯一的——`typeof(string)` 在任何地方返回的都是同一个 `RuntimeType` 对象。CoreCLR 通过 `RuntimeTypeHandle` 到 `RuntimeType` 的映射表保证这种唯一性。这个设计的一个后果是 `Type` 对象的引用相等可以直接用 `==` 比较，不需要调用 `Equals`。

### RuntimeMethodInfo 包装 MethodDesc

`RuntimeMethodInfo` 是 `System.Reflection.MethodInfo` 的 CoreCLR 实现。它内部持有一个 `RuntimeMethodHandle`，指向 VM 层的 MethodDesc。

从 MethodDesc 中可以读取方法的所有元信息：方法名（通过 metadata token 查 metadata 表）、参数列表（metadata 中的 Param 表）、返回类型、方法属性（`public` / `static` / `virtual` 等标志位）、方法体的 IL 偏移。

`RuntimeMethodInfo` 还提供动态调用能力——`MethodInfo.Invoke` 方法可以在运行时调用任意方法，这是序列化框架、依赖注入容器等基础设施的根基。

### RuntimeFieldInfo 包装 FieldDesc

`RuntimeFieldInfo` 包装 VM 层的 FieldDesc。FieldDesc 记录了字段在对象布局中的偏移量、字段类型、访问修饰符。

`FieldInfo.GetValue(obj)` 的实现路径：从 FieldDesc 读取字段偏移量 → 在对象实例的内存地址上加上偏移量 → 读取该位置的值 → 如果是值类型，boxing 后返回。`SetValue` 做相反的操作。

这意味着反射读写字段本质上是知道了偏移量之后的直接内存操作——但中间包裹了安全检查、类型兼容验证和可能的 boxing，这些是性能开销的主要来源。

### 性能：metadata 查表的代价

每次调用 `GetType()`、`GetMethods()`、`GetFields()` 都涉及 metadata 查表。以 `Type.GetMethod("DoSomething")` 为例：

1. 从 RuntimeType 获取 MethodTable 指针
2. 从 MethodTable 找到 EEClass
3. 遍历 EEClass 中的 MethodDescChunk 链表
4. 对每个 MethodDesc，通过 metadata token 从 metadata 表中读取方法名
5. 用字符串比较匹配目标方法名
6. 匹配成功后创建 RuntimeMethodInfo 包装对象

步骤 4 和 5 是主要的性能瓶颈——字符串比较的代价随方法名长度线性增长，而遍历所有 MethodDesc 的代价随类型的方法数量线性增长。一个有 200 个方法的类型，`GetMethod` 最坏情况需要比较 200 次字符串。

CoreCLR 对反射结果做了缓存。`RuntimeType` 内部维护一个 `RuntimeTypeCache`，第一次 `GetMethods()` 的结果被缓存，后续调用直接返回缓存的数组。但首次查询的开销无法避免，且缓存本身占用内存。

## MethodInfo.Invoke 的执行路径

`MethodInfo.Invoke` 是反射中最常用也最昂贵的操作之一。它允许在运行时动态调用任意方法，代价是绕过了编译器的直接调用优化。

调用路径拆解：

**参数打包。** `Invoke` 接受 `object[]` 参数数组。所有值类型参数必须 boxing——`int` 变成 `System.Int32` 堆对象，`double` 变成 `System.Double` 堆对象。每个值类型参数产生一次堆分配。对于一个接受 3 个 `int` 参数的方法，仅参数打包就产生 3 次堆分配 + 1 次数组分配。

**安全检查。** Runtime 验证调用者是否有权限访问目标方法。这包括：方法可见性检查（`public` / `private` / `internal`）、`ReflectionPermission` 安全检查、`SecurityCritical` / `SecuritySafeCritical` 属性验证。这些检查在每次 `Invoke` 调用时都执行——即使连续调用同一个方法 100 次，安全检查也执行 100 次。

**参数类型验证。** Runtime 检查传入的参数类型是否与方法签名匹配。如果参数类型不完全匹配但可以隐式转换（如 `int` 传给 `long` 参数），runtime 执行转换。

**目标方法调用。** 安全检查通过后，runtime 通过 MethodDesc 获取方法的 native code 入口地址（如果方法尚未 JIT 编译，此时触发 JIT），构建调用帧，执行间接调用。

**返回值处理。** 如果返回类型是值类型，返回值需要 boxing 后作为 `object` 返回。

```
MethodInfo.Invoke 的开销分解：

直接调用:  call target_method          → ~1 ns
反射调用:
  1. object[] 参数数组分配              → ~50 ns
  2. 值类型参数 boxing（每个）           → ~20 ns
  3. 安全检查                          → ~100 ns
  4. 参数类型验证                       → ~50 ns
  5. 间接调用                          → ~10 ns
  6. 返回值 boxing                     → ~20 ns
                                      ─────────
  总计：约 250+ ns vs 直接调用 ~1 ns
```

这就是为什么高性能框架尽量避免 `MethodInfo.Invoke`，转而使用 Delegate、Expression Tree 编译或 Emit 生成直接调用的代码。

## System.Reflection.Emit

Reflection 允许查询类型系统。Emit 允许扩展类型系统——在运行时生成新的 IL 代码，通过 JIT 编译为 native code 并执行。

### 核心 API

`System.Reflection.Emit` 提供两种粒度的代码生成：

**AssemblyBuilder / TypeBuilder / MethodBuilder** — 完整的程序集/类型/方法构建。可以在运行时创建一个新的程序集，在其中定义新的类型和方法，编译后像普通类型一样使用。Entity Framework 的代理类型生成、Castle Windsor 的动态代理都基于这条路径。

**DynamicMethod** — 轻量级路径。创建一个独立的方法（不归属于任何类型和程序集），通过 ILGenerator 生成 IL 字节码，调用 `CreateDelegate` 触发 JIT 编译，得到一个可以直接调用的委托。DynamicMethod 是性能敏感场景的首选——它避免了创建完整程序集的开销，且生成的 native code 可以被 GC 回收。

### ILGenerator 的工作机制

`ILGenerator` 是 Emit 的核心。它提供一组方法（`Emit`、`DefineLabel`、`MarkLabel`、`DeclareLocal`），让开发者以编程方式构造 IL 字节码流。

```csharp
// 运行时生成一个 (int, int) → int 的加法方法
var method = new DynamicMethod("Add", typeof(int),
    new[] { typeof(int), typeof(int) });

var il = method.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);      // 加载第一个参数
il.Emit(OpCodes.Ldarg_1);      // 加载第二个参数
il.Emit(OpCodes.Add);          // 相加
il.Emit(OpCodes.Ret);          // 返回

var add = (Func<int, int, int>)method.CreateDelegate(
    typeof(Func<int, int, int>));
int result = add(3, 4);        // result = 7，全速 native 执行
```

`CreateDelegate` 调用时发生的事情：ILGenerator 输出的 IL 字节码被提交给 RyuJIT。JIT 按照 B4 分析的标准编译管线处理这段 IL——Importer 构建 GenTree、Morph / SSA / CSE 优化、Lowering / LSRA / CodeGen 生成 native code。编译产物存储在 CodeHeap 中。返回的委托内部直接持有 native code 的入口地址。

后续通过这个委托调用 `add(3, 4)` 时，执行路径与编译器静态编译的方法完全相同——没有反射开销，没有安全检查，没有 boxing。这就是 Emit 的核心价值：用一次运行时编译的代价，换取后续所有调用的 native 性能。

### Emit 依赖 JIT 基础设施

Emit 的能力完全建立在 JIT 之上。ILGenerator 生成 IL，JIT 把 IL 编译为 native code——没有 JIT，Emit 生成的 IL 就只是一堆无法执行的字节。

这个依赖关系是理解 Emit 在不同 runtime 中可用性的关键。CoreCLR 有完整的 RyuJIT，所以 Emit 可以工作。任何没有 JIT 的 runtime——IL2CPP、LeanCLR、NativeAOT——都无法支持完整的 Emit。

## Emit 的应用场景

Emit 不是学术玩具，它是 .NET 生态中大量核心框架的基础能力。

### ORM：Entity Framework

Entity Framework 为每个实体类型在运行时生成代理类（proxy class）。代理类继承用户定义的实体类，重写虚属性（如 `virtual ICollection<Order> Orders { get; set; }`），在 setter 中注入变更追踪逻辑。这些代理类通过 `TypeBuilder` + `MethodBuilder` + `ILGenerator` 在运行时构建。

为什么不在编译期生成？因为 Entity Framework 是一个通用框架，不可能在自身的编译期知道用户会定义哪些实体类型。代理类必须在用户应用启动后、DbContext 初始化时才能生成。

### 序列化：System.Text.Json

`System.Text.Json` 在 .NET 7+ 中使用 Emit 为每种类型生成定制的序列化/反序列化代码。

考虑序列化 `Person { Name, Age }`：反射路径需要调用 `GetProperties()` 获取属性列表，对每个属性调用 `GetValue()` 读取值——每次序列化都有反射开销。Emit 路径在首次遇到 `Person` 类型时生成一个 DynamicMethod，方法体直接读取 `Name` 和 `Age` 字段的内存偏移位置，写入 JSON 输出——后续每次序列化都是 native 速度。

这就是 `System.Text.Json` 在 benchmark 中大幅领先反射序列化器的原因——它把 O(n) 次反射调用替换为一次 Emit 生成。

### AOP 框架

面向切面编程（AOP）框架如 Castle DynamicProxy 使用 Emit 在运行时为接口和虚方法生成拦截代理。代理类在方法调用前后插入横切逻辑（日志、事务、缓存），实现对原始代码的无侵入增强。

### 表达式树编译

`System.Linq.Expressions` 提供了一种更高级的动态代码生成方式。开发者构建表达式树（`Expression<Func<T, bool>>`），调用 `Compile()` 方法将其编译为可执行的委托。`Compile()` 的底层实现就是 Emit——表达式树被翻译为 IL 字节码，通过 DynamicMethod + JIT 编译为 native code。

LINQ to Objects 的 `Where`、`Select` 等操作符在特定路径上使用编译后的表达式树来加速谓词求值。

## 为什么 IL2CPP 没有 Emit

IL2CPP 是 AOT（Ahead-of-Time）编译方案。构建阶段把所有 IL 翻译成 C++ 源码，编译为 native 二进制。运行时不包含 JIT 编译器，不能生成新的 native code。

Emit 的核心是"运行时生成 IL → JIT 编译 → 得到 native code"。这条路径在 IL2CPP 环境中走不通：

1. `ILGenerator.Emit` 可以生成 IL 字节码——这一步不依赖 JIT，纯内存操作
2. `DynamicMethod.CreateDelegate` 需要把 IL 编译为 native code——这一步需要 JIT
3. IL2CPP 没有 JIT → 步骤 2 失败 → Emit 不可用

IL2CPP 对使用了 Emit 的代码会抛出 `System.NotSupportedException`。

### IL2CPP 的替代方案

**System.Linq.Expressions 的解释执行。** Expression Tree 在 IL2CPP 上不经过 `Compile()`（那需要 Emit），而是走解释执行路径。解释器遍历表达式树节点，逐节点求值。性能远低于编译执行，但功能正确。LINQ 查询在 IL2CPP 上可以工作，只是慢。

**代码剪裁与预生成。** 使用 Emit 的框架（如 Newtonsoft.Json 的反射路径）在 IL2CPP 上需要替代方案。Unity 生态中的 JSON 序列化库（如 `JsonUtility`）在编译期就确定了序列化逻辑，不依赖运行时代码生成。

**link.xml 保留元数据。** IL2CPP 的代码剪裁（stripping）会移除未被静态引用的类型和方法。反射使用的类型必须通过 `link.xml` 显式标记保留，否则运行时 `Type.GetType("MyClass")` 会返回 `null`。这不是 Emit 的替代方案，但与反射的可用性直接相关。

## LeanCLR 的 Reflection

LeanCLR 是纯解释器 runtime，没有 JIT，因此同样不支持 Emit。但它提供了基础的反射能力。

### 61 个 icall 实现

LeanCLR 通过 61 个 internal call（icall）实现了 `System.Reflection` 的核心 API。这些 icall 是解释器内部的 C++ 函数，当托管代码调用反射 API 时，解释器拦截调用并转发到对应的 icall 实现。

覆盖的能力包括：

- `Type.GetType` / `Assembly.GetType` — 按名称查找类型
- `Type.GetMethods` / `Type.GetFields` / `Type.GetProperties` — 枚举成员
- `MethodInfo.Invoke` — 动态方法调用
- `FieldInfo.GetValue` / `FieldInfo.SetValue` — 动态字段读写
- `Activator.CreateInstance` — 动态对象创建
- `Attribute` 相关 API — 自定义特性查询

这 61 个 icall 覆盖了反射 API 的核心子集。足以支撑依赖注入容器的基本功能（按类型查找和创建实例）和简单的序列化场景（枚举属性并读取值），但不支持运行时代码生成。

### 无 Emit 的代价

没有 Emit 意味着 LeanCLR 上的序列化、ORM、AOP 框架都只能走反射路径。每次方法调用都经过 `MethodInfo.Invoke` 的完整安全检查和参数打包流程。在 H5/小游戏的目标场景中，这些框架通常不是性能热点，代价可以接受。但如果是服务端高吞吐场景，缺失 Emit 会成为显著的瓶颈。

## Source Generator 作为 Emit 的替代

.NET 5 引入了 Source Generator，提供了一种在编译期生成 C# 源码的能力。Source Generator 不替代所有 Emit 的使用场景，但覆盖了其中最重要的一类：为已知类型生成定制代码。

### 编译期 vs 运行时

Emit 的代码生成发生在运行时：应用启动后，框架检查用户定义的类型，生成对应的 IL 代码。Source Generator 的代码生成发生在编译期：在 Roslyn 编译管线中，Generator 分析源码中的类型定义，生成额外的 C# 源文件，一起参与编译。

```
Emit 路径：
  编译期 → 应用代码.dll
  运行时 → 框架发现 Person 类型 → Emit 生成序列化代码 → JIT → native

Source Generator 路径：
  编译期 → Roslyn 分析 Person 类型 → Generator 生成 PersonSerializer.g.cs
        → 编译器编译 PersonSerializer.g.cs → 应用代码.dll 包含序列化代码
  运行时 → 直接调用 PersonSerializer → 已是 native code
```

### System.Text.Json 的双路径

`System.Text.Json` 在 .NET 6+ 同时支持 Emit 路径和 Source Generator 路径。开发者可以用 `[JsonSerializable]` attribute 标记上下文类型，Roslyn Source Generator 在编译期为标记的类型生成序列化代码。

Source Generator 路径的优势：

- **AOT 兼容。** 生成的代码是普通的 C# 方法，不依赖 JIT。在 IL2CPP、NativeAOT 上都可以工作
- **启动更快。** 不需要运行时的 Emit 编译，序列化代码在应用加载时就已经是 native code
- **可审计。** 生成的 `.g.cs` 文件可以在 IDE 中查看和调试，Emit 生成的 IL 只能通过反编译工具查看

### Source Generator 的局限

Source Generator 无法覆盖所有 Emit 的使用场景。关键差异在于：Source Generator 只能处理编译期已知的类型。

动态代理（Castle DynamicProxy）的场景——在运行时为任意接口生成代理类——Source Generator 无法完成，因为被代理的接口可能来自动态加载的程序集，编译期不可见。

插件系统中动态加载的程序集中的类型，Source Generator 同样无法预处理。这些类型在宿主程序编译时尚不存在。

因此 Source Generator 与 Emit 是互补关系：编译期能确定的类型用 Source Generator，运行时才知道的类型仍然需要 Emit（或退化为反射路径）。

## 反射性能优化的演进

CoreCLR 团队在 .NET 7 和 .NET 8 中对反射路径做了显著的性能优化，值得记录。

### .NET 7：MethodBase.Invoke 优化

.NET 7 重写了 `MethodBase.Invoke` 的热路径。关键改进：

- 缓存安全检查结果——首次调用后，如果调用者和目标方法的信任级别不变，后续调用跳过完整的安全检查
- 减少参数数组的分配——对于 4 个及以下参数的方法，使用栈分配的 `Span<object?>` 替代堆分配的 `object[]`
- 内联常见的参数类型转换——`int` → `long` 等宽化转换直接在调用桩中完成

### .NET 8：UnsafeAccessor

.NET 8 引入了 `UnsafeAccessorAttribute`——一种绕过访问检查直接访问私有成员的方式。JIT 在编译时识别这个 attribute，生成直接的字段读写或方法调用指令，没有反射开销。

```csharp
[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_name")]
extern static ref string GetName(Person p);

// JIT 编译为直接的字段偏移读取，零反射开销
string name = GetName(person);
```

这个 API 的目标用户是序列化框架和测试框架——它们经常需要访问私有成员，传统路径是 `FieldInfo.GetValue`（反射开销）或 Emit（运行时代码生成），`UnsafeAccessor` 提供了第三条路径：编译期标记，JIT 优化，native 性能。

## 收束

CoreCLR 的反射与 Emit 子系统可以压缩为三个层次：

**查询层。** RuntimeType / RuntimeMethodInfo / RuntimeFieldInfo 把 VM 内部的 MethodTable / MethodDesc / FieldDesc 包装为托管对象。每次查询都涉及 metadata 查表和对象分配的开销，CoreCLR 通过 RuntimeTypeCache 做缓存来摊销首次查询的代价。这一层所有 CLR 实现都提供——CoreCLR 完整实现，IL2CPP 通过保留 metadata 实现，LeanCLR 通过 61 个 icall 实现。

**动态调用层。** `MethodInfo.Invoke` 提供运行时动态调用任意方法的能力。代价是参数 boxing、安全检查、间接调用的组合开销。.NET 7/8 的优化显著降低了这个层的开销，但与直接调用仍然有数量级的差距。

**代码生成层。** Emit 通过 ILGenerator 生成 IL，JIT 编译为 native code，后续调用达到直接调用的性能。这一层是 CoreCLR 独有的能力——IL2CPP 和 LeanCLR 因缺少 JIT 而无法提供。Source Generator 在编译期覆盖了部分 Emit 的使用场景，且天然兼容 AOT 环境，但无法处理运行时才确定的类型。

三个层次的关系是递进的：查询层提供信息，动态调用层提供低速执行能力，代码生成层提供高速执行能力。框架开发者在三个层次之间选择，取决于性能需求和目标 runtime 的能力边界。

## 系列位置

- 上一篇：CLR-B8 线程与同步：Thread、Monitor、ThreadPool
- 下一篇：CLR-B10 Tiered Compilation：多级 JIT 与 PGO
