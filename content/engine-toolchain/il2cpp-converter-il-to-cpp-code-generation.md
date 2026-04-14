---
title: "IL2CPP 实现分析｜il2cpp.exe 转换器：IL → C++ 代码生成策略"
date: "2026-04-14"
description: "深入 il2cpp.exe 的内部转换策略：方法签名映射、类型布局生成、泛型实例化决策、虚方法表构建、异常处理翻译、字符串字面量处理。从具体的 C# → C++ 翻译示例出发，理解 source-to-source 转换器的工作边界。"
weight: 51
featured: false
tags:
  - IL2CPP
  - Unity
  - CodeGeneration
  - AOT
  - Compiler
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> il2cpp.exe 不是编译器——它是一个 source-to-source 转换器，把 .NET IL 翻译成等价的 C++ 代码，然后交给平台编译器做真正的编译。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 2 篇，承接 D1 架构总览中 Stage 3 的展开。

D1 建立了 IL2CPP 的五阶段管线全景，其中 Stage 3（Stripped IL → C++）被标记为"整条管线的核心"。那篇只用了一段概述了转换的核心映射关系，这篇把那段展开——拆解 il2cpp.exe 内部到底做了哪些转换决策。

## il2cpp.exe 在管线中的位置

回顾 D1 的管线图，il2cpp.exe 处于第三阶段：

```
Stage 1        Stage 2          Stage 3           Stage 4         Stage 5
C# 源码  →  IL 程序集  →  Stripped IL  →  C++ 源码  →  native binary  →  最终包体
(Roslyn)    (DLL)       (UnityLinker)   (il2cpp.exe)  (Clang/MSVC)
```

il2cpp.exe 的输入是 UnityLinker 裁剪后的 IL 程序集，输出是一组 C++ 源码文件和 `global-metadata.dat`。它本身是一个用 C# 编写的命令行工具，Unity 编辑器在构建时通过命令行调用它，传入裁剪后的程序集路径、输出目录和一系列配置参数。

关键认知：il2cpp.exe 不做优化、不做寄存器分配、不生成机器码。它只负责语义等价的 source-to-source 翻译。真正的优化和机器码生成是下游 C++ 编译器（Clang / MSVC / GCC）的工作。

## 输入与输出

### 输入

il2cpp.exe 读取的是 Stage 2 产出的 stripped IL assemblies——经过 UnityLinker 裁剪后的标准 .NET 程序集。这些 DLL 仍然是 ECMA-335 格式，包含 CIL 字节码和 metadata，只是不可达的类型和方法已经被移除。

典型输入文件：

```
Temp/StagingArea/Data/Managed/
  Assembly-CSharp.dll       ← 项目代码
  UnityEngine.CoreModule.dll ← 引擎模块
  mscorlib.dll               ← BCL 核心库
  System.dll                 ← 标准库
  ...
```

### 输出

il2cpp.exe 的输出是一个目录，包含大量 C++ 源码文件和一份二进制数据文件。核心产物包括：

| 文件 | 内容 |
|------|------|
| `Assembly-CSharp.cpp` | 项目 C# 代码转换后的 C++ 实现 |
| `Assembly-CSharp0.cpp` ~ `N.cpp` | 大文件拆分（避免单个 .cpp 过大导致编译器内存溢出） |
| `GenericMethods.cpp` | 泛型方法的具体实例化代码 |
| `Il2CppInvokerTable.cpp` | 方法调用桥接表（参数封装/拆包） |
| `Il2CppTypeDefinitions.c` | 类型定义的静态数据表 |
| `Il2CppMethodPointerTable.cpp` | 方法指针注册表 |
| `Il2CppGenericMethodPointerTable.cpp` | 泛型方法指针注册表 |
| `Il2CppAttributes.cpp` | 自定义 Attribute 的构造代码 |
| `global-metadata.dat` | 运行时元数据（字符串、类型信息、反射数据等） |
| 各种 `.h` 头文件 | 类型声明、前向声明、公共头 |

一个中等规模的 Unity 项目，il2cpp.exe 可能输出数百个 .cpp 文件、总计数十万行 C++ 代码。这就是为什么 Stage 4 的 C++ 编译通常是整个构建流程中最耗时的环节。

## 方法转换

方法转换是 il2cpp.exe 最核心的工作：把每个 C# 方法翻译成一个 C++ 函数。

### 函数签名

一个 C# 方法：

```csharp
public class Player
{
    public int TakeDamage(int amount)
    {
        hp -= amount;
        return hp;
    }
}
```

转换后的 C++ 函数签名：

```cpp
int32_t Player_TakeDamage_m1A2B3C4D(
    Player_t* __this,
    int32_t ___amount,
    const RuntimeMethod* method)
{
    // ...
}
```

几个关键点：

**第一个参数 `__this`**。实例方法的 `this` 指针被显式传递。静态方法没有这个参数（或传 `NULL`）。

**最后一个参数 `method`**。指向 `RuntimeMethod`（也即 `MethodInfo`）结构体的指针，携带方法的运行时元数据。虚方法调度、反射调用、泛型方法共享都依赖这个参数。

**函数名编码规则**。函数名包含类名、方法名和一个 hash 后缀（如 `m1A2B3C4D`）。hash 用于区分重载方法和避免命名冲突。

### IL 指令逐条翻译

il2cpp.exe 的翻译策略是逐条 IL 指令映射为 C++ 语句。以 `TakeDamage` 的方法体为例：

C# 源码中的 `hp -= amount; return hp;`，在 IL 层面是一组栈操作指令。il2cpp.exe 把这些栈操作翻译成等价的 C++ 局部变量赋值：

```cpp
int32_t Player_TakeDamage_m1A2B3C4D(
    Player_t* __this,
    int32_t ___amount,
    const RuntimeMethod* method)
{
    // ldarg.0 → __this（已在参数中）
    // ldfld int32 Player::hp → 读取字段
    int32_t L_0 = __this->___hp;
    // ldarg.1 → ___amount（已在参数中）
    // sub → 减法
    int32_t L_1 = L_0 - ___amount;
    // stfld int32 Player::hp → 写回字段
    __this->___hp = L_1;
    // ldfld int32 Player::hp → 再次读取
    // ret → 返回
    return __this->___hp;
}
```

这就是"source-to-source 转换"的含义——il2cpp.exe 不做控制流分析、不做寄存器分配，只是把 IL 的栈语义机械地翻译成 C++ 的变量语义。优化的责任完全留给下游 C++ 编译器。

### 更多 IL 指令的翻译示例

| IL 指令 | C++ 翻译 | 说明 |
|---------|---------|------|
| `ldloc.0` + `ldloc.1` + `add` | `int32_t V_2 = V_0 + V_1;` | 算术运算 |
| `newobj` | `Player_t* L_0 = (Player_t*)il2cpp_codegen_object_new(Player_il2cpp_TypeInfo);` | 对象分配 |
| `callvirt` | `VirtFuncInvoker1<int32_t, int32_t>::Invoke(slot, __this, amount);` | 虚方法调用 |
| `box` | `RuntimeObject* L_0 = Box(Int32_il2cpp_TypeInfo, &value);` | 装箱 |
| `castclass` | `RuntimeObject* L_0 = Castclass(obj, TargetType_il2cpp_TypeInfo);` | 类型转换 |
| `ldstr` | `String_t* L_0 = _stringLiteral_XXX;` | 字符串字面量加载 |

每条 IL 指令都有确定的 C++ 翻译模板。il2cpp.exe 本质上是一个基于模板的代码生成器。

## 类型转换

### 基本映射

每个 C# 类型被转换为一个 C++ 结构体。以一个简单的类为例：

```csharp
public class Player
{
    public int hp;
    public float speed;
    public string name;
}
```

转换后的 C++ 结构体：

```cpp
struct Player_t : public RuntimeObject
{
    int32_t ___hp;
    float ___speed;
    String_t* ___name;
};
```

几个关键设计：

**继承通过结构体嵌套实现**。`Player_t` 继承自 `RuntimeObject`，后者包含对象头——`Il2CppClass*` 指针（用于运行时类型标识和 vtable 查找）以及 GC 同步信息。

**字段按 CLI 布局规则排列**。字段的偏移量和对齐方式遵循 ECMA-335 规范中的布局规则。值类型字段内联存储，引用类型字段存储为指针。

**字段名前缀**。转换后的字段名带有 `___` 前缀，避免和 C++ 关键字或宏冲突。

### 值类型

值类型（struct）的转换不继承 `RuntimeObject`：

```csharp
public struct Vector2
{
    public float x;
    public float y;
}
```

```cpp
struct Vector2_t
{
    float ___x;
    float ___y;
};
```

值类型没有对象头，直接按值存储，不经过 GC 分配。这和 C# 语义一致。

### 枚举

枚举转换为带有底层类型的结构体：

```csharp
public enum DamageType : byte
{
    Physical = 0,
    Magical = 1,
    True = 2
}
```

```cpp
struct DamageType_t
{
    uint8_t value__;
};
```

枚举值作为常量定义在代码中，运行时访问直接使用整数值。

## 泛型处理

泛型是 il2cpp.exe 转换过程中最复杂的部分，也是代码膨胀的主要来源。

### 值类型泛型：独立实例化

对于值类型参数的泛型实例，每个不同的类型参数组合必须生成独立的 C++ 代码。原因很直接：不同值类型的内存布局不同。

```csharp
public class Container<T>
{
    public T value;
    public T GetValue() => value;
}

// 使用
var intContainer = new Container<int>();
var floatContainer = new Container<float>();
```

il2cpp.exe 会为 `Container<int>` 和 `Container<float>` 各生成一份完整的 C++ 代码：

```cpp
// Container<int> 的结构体
struct Container_1_tXXXX /* Container<int> */ : public RuntimeObject
{
    int32_t ___value;
};

// Container<int>.GetValue()
int32_t Container_1_GetValue_mXXXX(
    Container_1_tXXXX* __this,
    const RuntimeMethod* method)
{
    return __this->___value;
}

// Container<float> 的结构体
struct Container_1_tYYYY /* Container<float> */ : public RuntimeObject
{
    float ___value;
};

// Container<float>.GetValue()
float Container_1_GetValue_mYYYY(
    Container_1_tYYYY* __this,
    const RuntimeMethod* method)
{
    return __this->___value;
}
```

如果项目中使用了 `Container<int>`、`Container<float>`、`Container<double>`、`Container<Vector3>`、`Container<long>`，就会生成 5 份几乎相同的代码。这就是泛型代码膨胀的根本原因。

### 引用类型泛型：共享实例化

对于引用类型参数的泛型实例，il2cpp.exe 可以共享同一份代码——因为所有引用类型在 native 层都是指针，大小相同。

```csharp
var stringContainer = new Container<string>();
var objectContainer = new Container<object>();
var playerContainer = new Container<Player>();
```

这三个实例共享同一份 C++ 实现：

```cpp
// 共享版本：Container<RuntimeObject>
struct Container_1_tSHARED : public RuntimeObject
{
    RuntimeObject* ___value;  // 所有引用类型都是指针
};

RuntimeObject* Container_1_GetValue_mSHARED(
    Container_1_tSHARED* __this,
    const RuntimeMethod* method)
{
    return __this->___value;
}
```

`Container<string>`、`Container<object>`、`Container<Player>` 在运行时都使用这同一个函数，只是通过 `method` 参数中的 `RuntimeMethod` 区分具体的类型参数。这就是 IL2CPP 的 generic sharing 策略——用一份代码服务所有引用类型实例，大幅减少代码膨胀。

### 膨胀的规模

在实际项目中，泛型膨胀的影响非常显著。一个典型的例子是 `Dictionary<TKey, TValue>`：如果项目中使用了 `Dictionary<int, string>`、`Dictionary<int, float>`、`Dictionary<string, int>`、`Dictionary<long, object>` 四种组合，其中涉及值类型的组合各自需要独立生成 `Dictionary`、`Enumerator`、`Entry` 等多个内部类型的完整 C++ 代码。一个 `Dictionary` 实例化可以展开出上千行 C++ 代码。

这也是 IL2CPP D5 篇将要详细讨论的主题——泛型代码生成的三种策略（shared / specialized / full generic sharing）以及它们的工程影响。

## 虚方法表生成

### vtable 结构

C# 的虚方法调用在 IL2CPP 中通过 vtable（虚方法表）实现。每个类型的 `Il2CppClass` 结构体中包含一个方法指针数组，按固定的 slot 顺序排列。

```csharp
public class Unit
{
    public virtual int GetHP() => 100;
    public virtual void TakeDamage(int amount) { }
}

public class Player : Unit
{
    public override int GetHP() => hp;
    public override void TakeDamage(int amount) { hp -= amount; }
}
```

il2cpp.exe 在生成代码时，会为每个类型静态初始化 vtable 数组。`Unit` 和 `Player` 各有自己的 vtable，其中 `Player` 的 vtable 在对应的 slot 上指向覆写后的方法实现：

```
Unit 的 vtable:
  slot[0] → Unit_GetHP_mAAAA
  slot[1] → Unit_TakeDamage_mBBBB

Player 的 vtable:
  slot[0] → Player_GetHP_mCCCC      ← override
  slot[1] → Player_TakeDamage_mDDDD  ← override
```

### callvirt 翻译

C# 中的虚方法调用（`callvirt`）被翻译为通过 vtable 的间接调用：

```csharp
Unit unit = GetUnit();
int hp = unit.GetHP();  // callvirt
```

翻译后：

```cpp
// 从对象头获取 Il2CppClass*，再从 vtable 中按 slot 索引取函数指针
int32_t L_1 = VirtFuncInvoker0<int32_t>::Invoke(
    /* slot */ 0,
    /* this */ L_0);
```

`VirtFuncInvoker0` 是 il2cpp.exe 生成的模板辅助类，内部实现就是 `obj->klass->vtable[slot].methodPtr(obj, method)`——从对象的类型信息中取出 vtable，按 slot 索引找到目标函数指针，然后调用。

非虚方法调用（`call`）则直接翻译为静态函数调用，不经过 vtable。

## 异常处理翻译

### 两种实现策略

C# 的 `try/catch/finally` 在 IL2CPP 中有两种翻译策略，取决于目标平台：

**C++ 异常（`-fexceptions`）。** 在支持 C++ 异常的平台上（大多数桌面和移动平台），il2cpp.exe 直接翻译为 C++ 的 `try/catch`：

```csharp
try
{
    DoSomething();
}
catch (InvalidOperationException ex)
{
    Debug.Log(ex.Message);
}
finally
{
    Cleanup();
}
```

```cpp
try
{
    DoSomething_mXXXX(NULL);
}
catch (Il2CppExceptionWrapper& e)
{
    __exception_local = e.ex;
    if (il2cpp_codegen_class_is_assignable_from(
            InvalidOperationException_il2cpp_TypeInfo,
            il2cpp_codegen_object_class(__exception_local)))
    {
        // catch 块
        goto CATCH_001a;
    }
    throw;  // 不匹配的异常继续传播
}

CATCH_001a:
{
    InvalidOperationException_t* L_1 =
        (InvalidOperationException_t*)__exception_local;
    String_t* L_2 = Exception_get_Message_mXXXX(L_1, NULL);
    Debug_Log_mYYYY(L_2, NULL);
    goto IL_002e;
}

IL_002e:  // finally
{
    Cleanup_mZZZZ(NULL);
}
```

**setjmp/longjmp。** 在不支持 C++ 异常的平台（如某些嵌入式环境或 WebGL 早期版本）上，异常处理通过 `setjmp/longjmp` 模拟。这种方式性能更差，但兼容性更广。

注意 `Il2CppExceptionWrapper`——IL2CPP 用这个 C++ 包装类型承载 CLI 异常对象。`catch` 块内部通过 `il2cpp_codegen_class_is_assignable_from` 做类型匹配，模拟 CLI 的异常过滤语义。

## 字符串字面量

C# 中的字符串字面量（`ldstr` 指令）不会被内联到 C++ 代码中。il2cpp.exe 把所有字符串字面量收集起来写入 `global-metadata.dat`，运行时通过索引查找。

```csharp
string greeting = "Hello World";
```

翻译后：

```cpp
// il2cpp.exe 生成一个全局变量，在运行时初始化时从 metadata 加载
String_t* _stringLiteralXXXX;  // "Hello World"

// 方法体内直接引用
String_t* L_0 = _stringLiteralXXXX;
```

为什么不直接用 C++ 字符串字面量？因为 .NET 字符串是 UTF-16 编码的托管对象，不是 C 风格的 `char*`。它需要在运行时被分配为 GC 管理的 `Il2CppString` 对象，并且支持字符串驻留（interning）。把字符串数据集中存储在 metadata 文件中，既减少了 C++ 代码体积，也让运行时能统一管理字符串的生命周期。

## 优化

严格来说，il2cpp.exe 本身几乎不做优化。但它会生成一些有利于下游 C++ 编译器优化的代码模式。

### 去虚化（Devirtualization）

如果一个方法被标记为 `sealed` 或所在类被标记为 `sealed`，il2cpp.exe 知道该方法不会被覆写，可以把 `callvirt` 直接翻译为静态调用，跳过 vtable 查找：

```csharp
public sealed class FinalPlayer : Unit
{
    public override int GetHP() => hp;
}

// 调用侧
FinalPlayer player = GetFinalPlayer();
int hp = player.GetHP();  // 类型已知且 sealed
```

il2cpp.exe 可以把这个调用直接翻译为 `FinalPlayer_GetHP_mXXXX(__this, method)` 而不经过 vtable。这不仅避免了间接调用的开销，还为 C++ 编译器开启了内联优化的可能。

### 内联与常量折叠

il2cpp.exe 不做内联。但它生成的 C++ 函数通常是小函数（尤其是 getter/setter），C++ 编译器的内联优化器会自动处理。

同样，常量折叠、死代码消除、循环展开等优化也完全交给 C++ 编译器。这就是 IL2CPP 性能策略的核心思路：自己只做忠实翻译，把优化留给几十年工程积累的 C++ 编译器基础设施。

### null 检查

il2cpp.exe 默认在每次对象访问前插入 null 检查：

```cpp
// 在访问 __this->___hp 之前
NullCheck(__this);
int32_t L_0 = __this->___hp;
```

`NullCheck` 在 Development Build 中是一个显式的 if 判断加异常抛出。在 Release Build 中，某些平台可以通过硬件异常（SIGSEGV 捕获）来实现零开销的 null 检查——只有真正为 null 时才触发信号处理，正常路径没有额外开销。

## 转换器的局限

il2cpp.exe 作为 AOT 转换器，有一些无法绕过的结构性局限。

### 无法处理的特性

**Reflection.Emit 和动态 IL 生成。** `System.Reflection.Emit` 允许在运行时动态构造 IL 代码。这和 AOT 的基本假设（"所有代码在构建时已知"）直接矛盾。il2cpp.exe 在遇到 `Emit` 相关 API 时不会报错——这些 API 的 stub 存在于 BCL 中——但运行时调用会抛出 `NotSupportedException`。

**Assembly.Load（原生不支持）。** 原生 IL2CPP 无法在运行时加载新的托管程序集。il2cpp.exe 在构建时已经把所有已知的程序集转换完毕，运行时没有 IL 执行能力来处理新加载的 DLL。HybridCLR 补的核心能力之一就是这个。

**Expression.Compile()。** `System.Linq.Expressions.Expression.Compile()` 需要在运行时把表达式树编译成可执行的委托。在 JIT 环境下，这通过动态代码生成实现。IL2CPP 环境下，这个 API 会回退到解释执行模式（性能较差）或直接不可用。

### 构建时不可见的泛型实例

il2cpp.exe 只能为它在静态分析中发现的泛型实例生成代码。如果某个泛型实例只在运行时通过反射或热更代码创建，il2cpp.exe 无法预知它的存在，自然也不会生成对应的 C++ 代码。

这就是为什么 Unity 提供了 `[Preserve]` 和 `link.xml` 来手动标记需要保留的类型，以及为什么 HybridCLR 需要 supplementary metadata 来补充 AOT 泛型的缺口。

### 构建时间

il2cpp.exe 的输出是大量 C++ 代码，下游 C++ 编译器需要编译这些代码。一个大型项目可能输出 50 万行以上的 C++ 代码，C++ 编译耗时可能占整个构建的 60%~80%。il2cpp.exe 自身的转换速度反而不是瓶颈——瓶颈在它产出的 C++ 代码太多。

## 收束

回到 D1 中对 il2cpp.exe 的一句定位：它是转换器，不是编译器。

这篇拆开了这个转换器内部的具体策略。总结下来，il2cpp.exe 的工作可以归纳为六个核心转换：

1. **方法 → 函数**。每个 C# 方法变成一个 C++ 函数，带有 `__this` 和 `RuntimeMethod*` 参数，IL 指令逐条翻译为 C++ 语句。
2. **类型 → 结构体**。每个 C# 类型变成一个 C++ struct，引用类型继承 `RuntimeObject`（包含对象头），值类型直接按布局排列。
3. **泛型 → 实例化 / 共享**。值类型参数的泛型实例独立生成代码，引用类型参数的泛型实例共享代码。这是代码膨胀与运行效率的核心权衡点。
4. **虚调用 → vtable**。虚方法调用翻译为通过 vtable 的间接函数指针调用，vtable 在 C++ 代码中静态初始化。
5. **异常 → C++ try/catch 或 setjmp/longjmp**。根据平台配置选择实现策略。
6. **字符串 → metadata 查找**。字符串字面量不内联到 C++ 代码中，而是存储在 `global-metadata.dat` 中，运行时按索引加载。

il2cpp.exe 自身不做优化，它的策略是生成尽可能直白的 C++ 代码，让下游成熟的 C++ 编译器做真正的优化工作。这个设计决策既降低了 il2cpp.exe 自身的复杂度，也让 IL2CPP 能够"借力"不同平台上最好的 C++ 编译器基础设施。

但这个策略也带来了代价：C++ 代码量膨胀导致编译时间长，泛型实例化导致包体变大。后续 D5 篇会专门讨论泛型代码生成的优化策略。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 — IL2CPP 模块
- 位置：IL2CPP-D2
- 上一篇：[IL2CPP-D1 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})
- 下一篇：IL2CPP-D3 libil2cpp runtime

**相关阅读**

- [IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})
- [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
- [ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
