---
title: "ECMA-335 基础｜CLI Execution Model：方法调用约定、虚分派、异常处理模型"
date: "2026-04-14"
description: "从 ECMA-335 规范出发，拆解 CLI 执行模型的三大核心机制：方法调用指令的语义差异、虚方法与接口分派的 vtable 结构、两遍扫描的异常处理模型，以及这些机制在 CoreCLR、IL2CPP、HybridCLR、LeanCLR 中的不同实现。"
weight: 13
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Execution"
  - "VirtualDispatch"
  - "ExceptionHandling"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> 一个方法被调用时，runtime 到底做了几件事——查 vtable、准备参数、建栈帧、处理异常——每个 runtime 实现不同，但规范层的契约是同一份。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 4 篇。

上一篇讲的是类型系统——值类型 vs 引用类型、泛型、接口、约束。那些是静态结构：runtime 在加载程序集时解析 metadata，构建类型描述。这篇讲的是动态行为：当一个方法被调用时，runtime 怎么找到目标方法、怎么传参、怎么建栈帧、出了异常怎么处理。

## 为什么要讲执行模型

Metadata 和 CIL 是静态结构。TypeDef 表告诉 runtime 一个类型有哪些字段和方法，MethodDef 表告诉 runtime 一个方法的签名和 IL body。但光有这些结构不够——它们只描述了"有什么"，没有定义"怎么运行"。

执行模型定义的就是这些静态结构在运行时被"激活"的方式：

- 遇到 `call` 指令时，参数怎么从 eval stack 传递给目标方法
- 遇到 `callvirt` 指令时，runtime 怎么根据对象的实际类型查找 vtable
- 进入一个 try block 后抛了异常，runtime 怎么找到匹配的 catch handler
- 一个方法调用另一个方法时，栈帧怎么创建和销毁

所有 runtime 的方法调用、异常处理都遵循——或者有意识地偏离——这份规范。CoreCLR 用 JIT 生成 native 调用，IL2CPP 在构建时把 IL 翻译成 C++ 函数调用，LeanCLR 在解释器内部模拟栈帧，但它们要保证的外部可观测行为是同一套。

理解执行模型，就是理解这些 runtime 的公共契约。

## CLI 的方法调用模型

ECMA-335 Partition I 12.4 定义了 CLI 的方法调用机制。方法调用通过 4 种 CIL 指令完成，每种指令的语义不同。

### 四种调用指令

**`call`** — 静态绑定调用。编译器在编译时就确定了目标方法的 token，runtime 直接用这个 token 找到方法的入口点。不查 vtable，不做 null check（对实例方法，this 指针可能为 null 而不会被 runtime 捕获）。典型用途：调用静态方法、调用非虚实例方法、在子类中用 `base.Method()` 调用父类方法。

**`callvirt`** — 虚分派调用。runtime 先对 this 指针做 null check（如果是 null 就抛 `NullReferenceException`），然后根据对象的实际类型查 vtable 找到目标方法。即使目标方法不是 virtual 的，C# 编译器也经常生成 callvirt 而不是 call，纯粹是为了获得 null check 的保护。

**`calli`** — 间接调用。从 eval stack 顶部弹出一个函数指针，用这个指针调用方法。函数指针可以来自 `ldftn`（加载方法指针）或 `ldvirtftn`（加载虚方法指针）。delegate 的底层机制依赖 calli。

**`newobj`** — 构造器调用。语义上等价于：分配内存 + 调用 `.ctor` 方法。对于引用类型，先在堆上分配对象（包括对象头），然后调用构造函数初始化。对于值类型，在栈上分配空间后调用构造函数。

一个常见的误解是：`call` 用于静态方法，`callvirt` 用于虚方法。实际上 C# 编译器对几乎所有实例方法调用都生成 `callvirt`：

```csharp
class Player
{
    public void TakeDamage(int amount) { ... }  // 非虚方法
    public virtual void Die() { ... }            // 虚方法
}

Player p = GetPlayer();
p.TakeDamage(10);  // 编译器生成 callvirt（为了 null check），虽然方法不是 virtual
p.Die();           // 编译器生成 callvirt（真正的虚分派）
```

两者在 CIL 层面用的都是 `callvirt`，但 JIT 可以对非虚方法的 callvirt 优化——保留 null check 但跳过 vtable 查找，因为编译器知道目标方法不会被 override。

### 参数传递

ECMA-335 Partition I 12.4.1 定义了两种参数传递方式：

**按值传递** — 默认方式。值类型参数在 eval stack 上复制一份传给被调方法。被调方法拿到的是副本，修改不影响调用方的原始值。引用类型参数传的是引用（指针）的副本——指针本身被复制，但两个指针指向同一个堆对象。

**按引用传递** — 通过 managed pointer（`&` 类型）传递。C# 中的 `ref` 和 `out` 参数在 CIL 层面都是 managed pointer。被调方法拿到的是指向原始位置的指针，可以直接修改调用方的变量。

```csharp
void Swap(ref int a, ref int b)  // a 和 b 是 managed pointer
{
    int temp = a;
    a = b;
    b = temp;
}
```

对应的 CIL 中，`Swap` 方法的参数类型是 `int32&`（int32 的 managed pointer）。调用方通过 `ldloca`（加载局部变量地址）把变量的地址压栈，然后 `call` 传给 Swap。

### 返回值

方法的返回值通过 eval stack 返回。`ret` 指令从当前方法返回时，把 eval stack 顶部的值（如果有的话）作为返回值传递给调用方。调用方在 call/callvirt 指令完成后，返回值已经在自己的 eval stack 顶部。

## 虚方法分派

ECMA-335 Partition II 10.3 定义了虚方法的分派机制。

### VTable 概念

每个类型有一张虚方法表（vtable），表中的每个条目（slot）对应一个虚方法。slot 编号按方法的声明顺序排列。

```csharp
class Animal
{
    public virtual void Speak() { }    // slot 0
    public virtual void Move() { }     // slot 1
}

class Dog : Animal
{
    public override void Speak() { }   // 覆盖 slot 0
    // Move() 继承自 Animal，slot 1 不变
    public virtual void Fetch() { }    // slot 2（新增）
}
```

Dog 的 vtable：

```
slot 0: Dog.Speak()     ← 覆盖了 Animal.Speak()
slot 1: Animal.Move()   ← 继承，没有覆盖
slot 2: Dog.Fetch()     ← 新增的虚方法
```

当 runtime 执行 `callvirt Animal::Speak` 时，它从对象头拿到实际类型的 vtable，然后取 slot 0 的函数指针。如果对象实际类型是 Dog，取到的就是 `Dog.Speak`，这就是多态。

### Override 语义

子类通过 `.override` 指令或 `newslot`/`virtual` 标记来声明覆盖关系。覆盖的本质是替换父类 vtable slot 中的函数指针：

- `virtual` + 无 `newslot` → 覆盖父类的同名同签名 slot
- `virtual` + `newslot` → 创建新的 slot（C# 中的 `new virtual`）
- `.override` 指令 → 显式覆盖（用于接口的显式实现）

### 接口分派 vs 虚方法分派

虚方法分派只需要一次 vtable 查找：对象类型 → vtable → slot[n]。

接口分派需要额外一步：先找到接口方法在当前类型 vtable 中的映射位置。因为接口不在继承链上，同一个类型可能实现多个接口，接口方法的 slot 位置在不同类型中可能不同。

```csharp
interface IAttackable { void OnHit(); }     // 接口的 OnHit 是 slot 0

class Wall : IAttackable
{
    public void OnHit() { }  // 在 Wall 的 vtable 里可能是 slot 3
}

class Enemy : IAttackable
{
    public void OnHit() { }  // 在 Enemy 的 vtable 里可能是 slot 5
}
```

runtime 不能用固定的 slot 编号来分派接口方法——必须查一张 interface map，找到 `IAttackable` 在当前类型中对应的 vtable 起始偏移。

### sealed/final 方法与去虚化

当一个虚方法被标记为 `sealed`（CIL 中的 `final` 标记），编译器知道这个方法不会再被子类覆盖。JIT 或 AOT 编译器可以把对 sealed 方法的 `callvirt` 降级为直接调用（devirtualization），跳过 vtable 查找。

```csharp
class Base
{
    public virtual void Update() { }
}

class Derived : Base
{
    public sealed override void Update() { }  // 不会再被覆盖
}

Derived d = new Derived();
d.Update();  // JIT 可以直接调用 Derived.Update，不查 vtable
```

去虚化是一个重要的优化。CoreCLR 的 JIT 不仅对 sealed 方法做去虚化，还会做推测性去虚化（guarded devirtualization）——当 JIT 观察到某个 callvirt 调用点在历史上总是调用同一个类型的方法时，生成一个类型检查 + 直接调用的快速路径。

### 不同 runtime 的 vtable 实现

各 runtime 的 vtable 设计差异体现在内存布局上：

**CoreCLR** — vtable 嵌入在 MethodTable 结构的尾部。MethodTable 是 CoreCLR 中最核心的类型描述结构，每个类型一份。vtable slot 存的是方法的 native code pointer（JIT 编译后的入口地址）。

**IL2CPP** — `Il2CppClass` 结构中有一个 `vtable` 数组（`VirtualInvokeData[]`），每个条目包含方法指针和方法信息。因为 IL2CPP 是全量 AOT，所有 vtable slot 在构建时就填好了。

**LeanCLR** — `RtClass` 中有一个 `vtable` 数组，slot 存的是 `RtMethod*`。因为 LeanCLR 是解释器，调用虚方法时从 vtable 取到 `RtMethod`，再进入解释器执行 IR bytecode。

## 异常处理模型

ECMA-335 Partition I 12.4.2 定义了 CLI 的异常处理模型。这个模型的核心是两遍扫描（two-pass）机制。

### 四种异常处理子句

每个 try block 可以关联四种异常处理子句：

**catch** — 按类型匹配异常。当抛出的异常对象是 catch 子句声明的类型（或其子类型）时匹配成功。

**filter** — 动态过滤。运行一段用户代码来决定是否匹配，返回 true 则匹配。C# 6.0 的 `when` 子句就编译为 filter。

**finally** — 无论异常是否发生都执行。用于资源清理。

**fault** — 只在发生异常时执行（不像 finally 总是执行）。C# 没有直接的 fault 语法，但 `using` 语句在某些情况下会生成 fault 子句。

### 两遍扫描模型

runtime 抛出异常后，执行两遍扫描：

**第一遍（查找阶段）** — 从抛出点开始，沿调用栈向上遍历每一帧。在每一帧中，检查当前 IP（instruction pointer）是否在某个 try block 的 protected region 内。如果是，检查该 try 关联的 catch/filter 子句是否匹配。找到第一个匹配的 handler 后停止。

**第二遍（展开阶段）** — 从抛出点重新开始，沿调用栈向上展开（unwind）到匹配的 handler 所在帧。展开过程中，逐帧执行途经的所有 finally 和 fault 子句。到达目标帧后，把控制权交给匹配的 catch handler。

```csharp
void A()
{
    try
    {
        B();
    }
    catch (InvalidOperationException ex)  // ← 第一遍找到匹配
    {
        Console.WriteLine(ex.Message);    // ← 第二遍最终到达这里
    }
}

void B()
{
    try
    {
        C();
    }
    finally
    {
        Cleanup();  // ← 第二遍展开时执行
    }
}

void C()
{
    throw new InvalidOperationException("error");  // 抛出点
}
```

执行顺序：
1. 第一遍：C（无 handler）→ B（finally 不是 catch，跳过）→ A（catch InvalidOperationException 匹配）
2. 第二遍：从 C 展开到 B，执行 B 的 finally（调用 Cleanup），然后展开到 A，进入 A 的 catch handler

两遍扫描的设计意图是：在实际展开栈帧之前，先确认有 handler 能处理这个异常。如果整个调用栈上没有匹配的 handler，runtime 可以在第一遍结束时直接报告未处理异常，而不需要执行任何 finally（某些 runtime 的策略）。

### Protected Region 在 metadata 中的表示

每个方法的异常处理信息存储在方法体的 exception handling clause 表中（不是 metadata 表，而是 method body 的一部分，跟在 CIL 指令之后）。每个子句记录：

- `Flags` — 子句类型（catch = 0、filter = 1、finally = 2、fault = 4）
- `TryOffset` + `TryLength` — try block 的 IL 偏移范围
- `HandlerOffset` + `HandlerLength` — handler 的 IL 偏移范围
- `ClassToken`（catch 子句）— 捕获的异常类型 token
- `FilterOffset`（filter 子句）— filter 代码的起始 IL 偏移

runtime 在方法加载时解析这些子句，建立"IL 偏移 → 异常处理区域"的映射。抛异常时根据当前 IP 查找所在的 protected region。

### 栈展开

栈展开（stack unwinding）是第二遍扫描的核心操作：逐帧回退调用栈，执行 finally/fault 子句，销毁栈帧，直到到达匹配的 handler。

栈展开的实现高度依赖 runtime 的执行模型：

**CoreCLR** — 在 Windows 上与操作系统的 SEH（Structured Exception Handling）集成。CLR 异常被包装为 SEH 异常，利用 OS 的栈展开基础设施遍历 native 栈帧。在 Linux 上使用 libunwind 库。JIT 生成的 native code 需要提供 unwind info（寄存器保存位置、栈帧大小等），OS 才能正确展开。

**IL2CPP** — 把 CIL 的 try/catch 翻译成 C++ 的 try/catch 或 setjmp/longjmp。在支持 C++ exception 的平台上（大多数桌面和移动平台），直接利用 C++ 的异常机制。在不支持 C++ exception 的平台上（某些嵌入式平台、WebAssembly），使用 setjmp/longjmp 模拟。

**HybridCLR** — 解释器内部用 `ExceptionFlowInfo` 结构记录异常状态。当解释器执行到 throw 指令时，不做真正的栈展开，而是在解释器循环内部切换执行状态——从正常执行模式切换到异常处理模式，在当前方法的 exception clause 中查找匹配项。如果当前方法没有匹配的 handler，解释器退出当前方法回到调用方，继续在异常处理模式下查找。

**LeanCLR** — 解释器内部用 `RtInterpExceptionClause` 结构管理异常处理子句。展开逻辑在解释器循环中实现：记录当前嵌套的 try 深度，抛异常时从内层向外层逐层查找匹配的 handler，执行途经的 finally 子句。

## 方法的运行时表示

一个方法从 metadata 中的静态记录变成可执行状态，经历的路径因 runtime 而异。

### 从 MethodDef 到可执行状态

metadata 中的 MethodDef 表（表编号 0x06）记录了方法的名称、签名、属性（static/virtual/abstract 等）和 RVA（指向 IL body 的偏移）。runtime 加载方法时，解析 MethodDef 构建运行时的方法描述结构——通常包含 vtable slot 编号、函数指针、调用约定等信息。

三种执行路线把方法变成可执行状态的方式不同：

**JIT 路线（CoreCLR）** — 方法首次被调用时触发 JIT 编译。MethodDesc（CoreCLR 的方法描述结构）初始时函数指针指向一段 prestub 代码。prestub 触发 JIT，JIT 读取 IL body 生成 native code，然后把 MethodDesc 的函数指针更新为 native code 入口地址。后续调用直接跳转到 native code，不再经过 JIT。

**AOT 路线（IL2CPP）** — 构建时 il2cpp.exe 把每个方法的 IL body 翻译成 C++ 函数。运行时方法结构（`MethodInfo`）在初始化时就填入了对应 C++ 函数的指针。不存在"首次调用"的编译开销，但无法执行构建时未见过的方法。

**Interpreter 路线（LeanCLR / HybridCLR）** — 方法加载时，解释器对 IL body 做一次 transform，转换成解释器专用的 IR bytecode。LeanCLR 的 transform 经过三级变换：raw IL → stack IR → register IR。调用方法时解释器逐条执行 IR 指令，不生成 native code。执行速度慢于 JIT/AOT 产物，但支持运行时加载新方法。

### 三种路线的工程 trade-off

| 维度 | JIT | AOT | Interpreter |
|------|-----|-----|-------------|
| 首次调用延迟 | 有（JIT 编译耗时） | 无 | 有（transform 耗时，但远小于 JIT） |
| 稳态执行速度 | 最快（native code + 运行时 profile 优化） | 快（native code，但缺少运行时优化信息） | 最慢（逐条解释） |
| 包体大小 | 小（只需分发 IL） | 大（每个方法的 native code 都打包） | 小（只需分发 IL） |
| 运行时加载新代码 | 支持（JIT 可以编译新 IL） | 不支持 | 支持 |
| 平台限制 | 需要 W^X 权限（可写可执行内存） | 无特殊要求 | 无特殊要求 |

CoreCLR 选 JIT 是因为服务器和桌面场景对启动延迟不敏感但对稳态性能要求高。IL2CPP 选 AOT 是因为 iOS 禁止 JIT（App Store 政策不允许动态生成可执行代码）。LeanCLR 和 HybridCLR 选 interpreter 是为了在 AOT 环境中支持热更新——解释执行不需要生成 native code，绕过了 W^X 限制。

## 栈帧

ECMA-335 Partition I 12.3 定义了方法调用的栈帧模型。

### 栈帧结构

每次方法调用创建一个栈帧（stack frame），包含：

- **参数区（argument area）** — 传入的参数值（按值传递的副本或 managed pointer）
- **局部变量区（local variable area）** — 方法声明的局部变量，初始化为零
- **求值栈（evaluation stack）** — CIL 指令的操作数栈，运行时大小动态变化但有编译时可知的最大深度
- **返回信息** — 调用方的返回地址和需要恢复的寄存器状态

```
高地址
┌──────────────────────┐
│  调用方的栈帧          │
├──────────────────────┤
│  返回地址             │
│  参数 N              │
│  ...                 │
│  参数 0 (this)       │
├──────────────────────┤
│  局部变量 0           │
│  局部变量 1           │
│  ...                 │
├──────────────────────┤
│  eval stack 空间      │
│  (最大深度已知)        │
└──────────────────────┘
低地址
```

### JIT 栈帧 vs Interpreter 栈帧

JIT 编译后的方法使用硬件栈（CPU 的 RSP/RBP 寄存器管理）。栈帧布局由 JIT 决定，和 C/C++ 函数的栈帧没有本质区别——参数可能在寄存器里（遵循平台 calling convention），局部变量分配在栈上，eval stack 被 JIT 消除（转换为寄存器操作或栈上临时变量）。

解释器的栈帧是软件模拟的。LeanCLR 用 `InterpFrame` 结构在堆内存上分配栈帧，每个 InterpFrame 包含参数数组、局部变量数组和求值栈数组。方法调用时分配新的 InterpFrame，方法返回时释放。HybridCLR 类似，用 `MachineState` 维护解释器的执行状态。

### 混合执行的栈帧切换

当 AOT 编译的代码调用解释器执行的方法（或反过来）时，需要在两种栈帧模型之间切换。

HybridCLR 的做法：AOT 代码通过 IL2CPP 的 `Runtime::Invoke` 进入解释器。解释器在自己的 MachineState 上建立栈帧执行 IL。当解释器需要回调 AOT 方法时，通过 IL2CPP 的函数指针直接调用 native 代码。这意味着调用栈上 native 帧和解释器帧交替出现。

LeanCLR 的做法类似但更统一——因为 LeanCLR 本身就是纯解释器，大多数调用都在解释器内部完成。但调用 internal call（runtime 内部实现的方法，用 C++ 编写）时，也需要从解释器帧切换到 native 帧。

混合执行的一个工程难点是异常处理：异常在 native 帧和解释器帧之间传播时，两套栈展开机制需要正确衔接。CoreCLR 不存在这个问题（全是 native 帧），但 HybridCLR 和 LeanCLR 都需要在解释器的异常处理逻辑中正确处理"异常穿越 native 帧"的场景。

## 这些概念在不同 runtime 里的对比

| 概念 | CoreCLR | IL2CPP | HybridCLR | LeanCLR |
|------|---------|--------|-----------|---------|
| **方法调用** | JIT 生成 native call 指令 | 编译为 C++ 函数调用 | 解释器内部分派 + 可回调 AOT 方法 | 解释器内部分派 |
| **vtable 存储** | MethodTable 尾部，slot 存 native code pointer | `Il2CppClass::vtable[]`，slot 存 `VirtualInvokeData` | 复用 IL2CPP 的 vtable 结构 | `RtClass::vtable[]`，slot 存 `RtMethod*` |
| **接口分派** | Interface dispatch map + Virtual Stub Dispatch 缓存 | `interface_offsets` 数组线性查找 | 复用 IL2CPP 的接口分派 | `interface_vtable_offsets` 映射表 |
| **异常处理机制** | Windows SEH / Linux libunwind | C++ try/catch 或 setjmp/longjmp | 解释器内部 ExceptionFlowInfo | 解释器内部 RtInterpExceptionClause |
| **栈帧模型** | 硬件栈帧（JIT 管理 RSP/RBP） | C++ 编译器管理的 native 栈帧 | MachineState 软件栈帧 + native 帧混合 | InterpFrame 软件栈帧 |
| **方法编译** | 首次调用时 JIT 编译 | 构建时 AOT 编译为 C++ | transform IL → 解释器 IR | 三级 transform → register IR |
| **去虚化** | JIT 做 sealed 去虚化 + 推测性去虚化 | il2cpp.exe 做静态去虚化 | 不做（解释器逐条执行） | 不做（解释器逐条执行） |

几个值得注意的对比点：

**接口分派的优化程度。** CoreCLR 在接口分派上投入了大量工程：Virtual Stub Dispatch 会把第一次查找的结果缓存在一个 dispatch stub 里，后续调用走 fast path 直接命中。IL2CPP 和 LeanCLR 每次都做完整查找——对于不在热路径上的接口调用影响不大，但在高频场景下差异可测量。

**异常处理的性能特征。** CoreCLR 的异常处理依赖 OS 机制，throw 的开销很高（涉及 SEH/signal 处理），但 try block 本身几乎零开销（JIT 生成的代码在没有异常时不会执行任何额外指令）。解释器的异常处理在 throw 时开销更低（不涉及 OS 机制），但 try block 本身有一定的管理开销（需要在解释器循环中追踪当前所在的 protected region）。

**混合执行的复杂度。** HybridCLR 面临的工程复杂度最高：它需要在 IL2CPP 的 AOT 世界和解释器世界之间建立桥梁，异常处理、栈帧管理、参数传递都需要两套机制的对接。纯解释器（LeanCLR）和纯 JIT（CoreCLR）在这方面反而简单——只有一种栈帧模型。

## 收束

CLI 执行模型的核心可以归纳为三层契约：

**调用契约。** 四种调用指令定义了方法调用的所有方式——静态绑定、虚分派、间接调用、构造器。参数按值或按引用传递，返回值通过 eval stack。这一层是所有 runtime 必须遵守的语义。

**分派契约。** 虚方法通过 vtable slot 分派，接口方法需要额外的 interface map 查找。sealed 方法可以去虚化。不同 runtime 的 vtable 内存布局和接口分派优化策略各异，但外部可观测行为一致。

**异常契约。** 两遍扫描——先找 handler 再展开栈帧。四种子句类型覆盖了所有异常处理场景。runtime 的实现差异最大的部分在栈展开机制，从 OS 级别的 SEH 到解释器内部的状态切换，跨度极大。

这三层契约是理解后续每个 runtime 具体实现的前提。CoreCLR 的 MethodTable 和 JIT 编译管线、IL2CPP 的代码生成和异常翻译、HybridCLR 的桥接层设计、LeanCLR 的双解释器架构——都是在这同一套契约之上做出的工程选择。

## 系列位置

- 上一篇：ECMA-A3 CLI Type System：值类型 vs 引用类型、泛型、接口、约束
- 下一篇：ECMA-A5 Assembly Model：程序集加载、版本绑定、元数据导出
