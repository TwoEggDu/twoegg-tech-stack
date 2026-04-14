---
date: "2026-04-14"
title: "LeanCLR 源码分析｜方法调用链：从 Assembly.Load 到 Interpreter::execute"
description: "跟着一个方法从 leanclr_load_assembly 到 Interpreter::execute 的完整路径：Assembly 加载、类初始化、三级 fallback 分派、首次 transform 触发、LL-IL 执行循环，以及与 HybridCLR 8 步链的逐步对比。"
weight: 75
featured: false
tags:
  - LeanCLR
  - CLR
  - MethodInvocation
  - Runtime
  - SourceCode
series: "dotnet-runtime-ecosystem"
series_id: "leanclr"
---

> 跟着一个方法从加载到执行的完整路径，是理解任何 CLR 实现的最好方式。LeanCLR 的这条链和 HybridCLR 的 8 步链有明显的结构差异。

这是 .NET Runtime 生态全景系列的 LeanCLR 模块第 6 篇。

## 从 HCLR-5 接续——对比视角

HCLR-5 做了一件事：沿着 HybridCLR 的一条真实调用链，从 `Assembly.Load(byte[])` 一路跟到 `Interpreter::Execute`，最终压成 8 步。

那 8 步是：

1. `Assembly.Load(byte[])` 进入 `AppDomain::LoadAssemblyRaw`
2. `MetadataCache::LoadAssemblyFromBytes` 转交 `hybridclr::metadata::Assembly::LoadFromBytes`
3. `Assembly::Create` 构造 `InterpreterImage`，注册为解释型程序集
4. `Class::SetupMethods` 为方法绑定 `invoker_method`，指向 `InterpreterInvoke`
5. `MethodInfo.Invoke` → `vm::Runtime::InvokeWithThrow` → `method->invoker_method(...)`
6. `InterpreterInvoke` 参数转换 + 首次调用触发 `GetInterpMethodInfo`
7. `HiTransform::Transform` 取 method body，产出 `InterpMethodInfo`
8. `Interpreter::Execute` 进入 `HiOpcode` 分派循环

这 8 步有一个核心特征：HybridCLR 始终运行在 IL2CPP 内部，每一步都需要和 IL2CPP 的已有基础设施对接——`Il2CppAssembly`、`Il2CppImage`、`Il2CppClass`、`invoker_method` 函数指针。它不是在搭自己的房子，而是在别人的房子里加建一层。

LeanCLR 的调用链没有这个约束。它是一个独立的运行时，从加载到执行的整条链路都在自己的结构里完成。这让它的链路更短、更直接，但也意味着每一步都要自己实现，没有现成的基础设施可以复用。

这篇就沿着 LeanCLR 的调用链完整走一遍，最后和 HybridCLR 的 8 步链做逐步对比。

## Assembly 加载入口

**源码位置：** `src/runtime/metadata/assembly.h`、`src/runtime/public/leanclr.h`

LeanCLR 的加载入口是 C API 函数 `leanclr_load_assembly`。这是宿主程序和运行时交互的第一个接触点。

整个加载链路是：

```
leanclr_load_assembly("CoreTests")
  → Assembly::load_by_name("CoreTests")
    → Settings::file_loader("CoreTests.dll")  // 回调宿主，获取 DLL 字节流
      → CliImage::load_streams(bytes)          // PE/COFF + metadata 解析
        → RtModuleDef::create(image)           // 构建运行时结构
```

每一步的职责边界清晰。

**`Assembly::load_by_name`** 是加载的调度中心。它先检查程序集是否已经加载过（避免重复加载），然后通过 `Settings::file_loader` 回调获取 DLL 文件的字节流。这个回调是宿主程序在初始化时注册的——不同平台的文件系统各不相同，LeanCLR 不假设文件怎么读取，由宿主决定。

**`CliImage::load_streams`** 负责 ECMA-335 定义的 PE/COFF 和 metadata 解析。LEAN-F2 已经详细拆解了这一步：从 DOS header 到 PE signature，到 COFF header，到 CLI header，最终定位并解析 5 个 metadata stream（`#~`、`#Strings`、`#Blob`、`#GUID`、`#US`）。

**`RtModuleDef::create`** 把 CliImage 解析出的 metadata 表转化为运行时结构。它遍历 `TypeDef` 表，为每个类型创建 `RtClass`；遍历 `MethodDef` 表，为每个方法创建 `RtMethodInfo`；遍历 `FieldDef` 表，为每个字段创建 `RtFieldInfo`。

和 HybridCLR 对比，最大的差异在第一步。HybridCLR 的加载入口是 `Assembly.Load(byte[])`——这是 C# 托管代码通过 `AppDomain` 触发的，需要经过 IL2CPP 的 icall 层（`AppDomain::LoadAssemblyRaw`），再转交给 `hybridclr::metadata::Assembly::LoadFromBytes`，最后把热更程序集包装成 `Il2CppAssembly` 注册回 `MetadataCache`。整个过程需要和 IL2CPP 的数据结构对齐。

LeanCLR 没有这层适配。`leanclr_load_assembly` 是一个纯 C 函数，直接调用自己的 `Assembly::load_by_name`，不需要经过任何中间层。加载完成后，程序集以 `RtModuleDef` 的形式存在于运行时中，不需要包装成别人的结构。

## 类初始化

**源码位置：** `src/runtime/vm/class.h`

程序集加载完成后，`RtModuleDef::create` 会触发 `Class::initialize_all`，为模块中的每个 TypeDef 执行类初始化。

类初始化的链路：

```
Class::initialize_all(module)
  → 遍历所有 TypeDef
    → init_corlib_classes()           // 如果是 corlib，建立核心类型缓存
    → 计算 field layout               // 确定每个字段的偏移量和对象大小
    → 构建 vtable                      // 虚方法分派表
    → 标记 .cctor 状态                 // 记录是否有类型构造器待执行
```

**核心类型缓存。** 如果当前加载的是 corlib（`mscorlib.dll` 或 `System.Private.CoreLib.dll`），`init_corlib_classes` 会把 `System.Object`、`System.String`、`System.Array`、`System.Int32` 等 70+ 核心类型缓存到 `CorLibTypes` 中。这个缓存在 LEAN-F4 中已经分析过——运行时中大量操作需要访问核心类型信息，如果每次都通过 metadata token 去查找，开销不可接受。

**字段布局计算。** 对每个类型，运行时需要确定实例字段的偏移量和总大小。这个过程遵循 ECMA-335 的布局规则：Sequential 布局按声明顺序排列，Explicit 布局使用 `FieldOffset` 属性指定偏移，Auto 布局由运行时自行决定对齐方式。LeanCLR 在 Auto 布局模式下按字段大小降序排列，以减少对齐填充——这和 CoreCLR 的策略类似。

**VTable 构建。** LEAN-F4 拆解了 VTable 的结构——每个槽位是一个 `RtVirtualInvokeData`，同时保存方法指针和方法元数据。在类初始化阶段，运行时需要把继承链上的虚方法和接口实现映射到正确的 VTable 槽位。`Method::get_vtable_method_invoke_data` 在后续的虚方法调用中通过索引定位具体槽位。

**.cctor 状态标记。** 如果一个类型有类型构造器（`.cctor`，即 `static` 构造函数），运行时在初始化阶段只标记它的存在，不立即执行。`.cctor` 的执行被推迟到类型第一次被使用时。这和 ECMA-335 的规定一致——CLR 规范要求类型构造器在类型首次被访问前执行，但不要求在加载时执行。

## 方法调用入口

**源码位置：** `src/runtime/vm/runtime.h`

当宿主程序或解释器需要调用一个方法时，入口是 `Runtime` 类提供的 invoke 函数族：

```cpp
class Runtime {
    static RtResult<RtObject*> invoke_with_run_cctor(
        const RtMethodInfo* method, RtObject* obj, const void* const* params);
    static RtResult<RtObject*> invoke_array_arguments_with_run_cctor(
        const RtMethodInfo* method, RtObject* obj, RtArray* params);
    static RtResultVoid invoke_stackobject_arguments_with_run_cctor(
        const RtMethodInfo* method, const RtStackObject* params, RtStackObject* ret);
};
```

三个变体对应不同的参数传递方式：

- `invoke_with_run_cctor`：参数以 `void*` 数组传入，适用于从 C API 或反射路径调用
- `invoke_array_arguments_with_run_cctor`：参数以 `RtArray` 传入，适用于托管代码中的 `MethodInfo.Invoke(object[])`
- `invoke_stackobject_arguments_with_run_cctor`：参数以 `RtStackObject` 传入，这是解释器内部方法间互调的路径

三个变体的名字都包含 `_with_run_cctor`，因为它们在分派方法之前都会做同一件事：检查方法所属类型的 `.cctor` 是否已经执行，如果没有，先执行 `.cctor`。

```
Runtime::invoke_with_run_cctor(method, obj, params)
  ├─ run_class_static_constructor(method->klass)  // 如果 .cctor 尚未执行
  └─ dispatch(method, obj, params)                // 进入三级 fallback
```

`run_class_static_constructor` 是一个幂等操作。RtClass 内部维护一个状态标记，记录 `.cctor` 是否已经执行过。首次调用时执行 `.cctor` 并更新标记，后续调用直接跳过。这避免了每次方法调用都去检查和执行类型构造器的开销。

和 HybridCLR 的对比：在 HybridCLR 中，`.cctor` 的执行由 IL2CPP 的 `Class::Init` 触发。热更方法在 `InterpreterInvoke` 进入 `Execute` 之前，会先调用 `Class::Init(methodInfo->klass)` 确保类已初始化。两者的语义相同，但 LeanCLR 把 `.cctor` 检查直接嵌入了 invoke 函数，不需要单独的 `Class::Init` 路径。

## 三级 fallback

**源码位置：** `src/runtime/vm/method.h`、`src/runtime/intrinsics/`、`src/runtime/icalls/`、`src/runtime/interp/interpreter.h`

`.cctor` 检查完成后，进入方法分派的核心逻辑。LeanCLR 按固定优先级尝试三个执行路径：

```
dispatch(method, obj, params)
  ├─ 第一级：Is intrinsic?  → Intrinsics::invoke()     [fast path]
  ├─ 第二级：Is internal call? → InternalCalls::invoke()
  ├─ 第三级：Has interp_data? → Interpreter::execute()
  └─ Otherwise: error (JIT not implemented)
```

### 第一级：Intrinsics

`intrinsics/` 目录下 18 个文件，共 1,240 行代码。这是性能关键方法的原生实现——运行时直接用 C++ 代码替代 IL 执行。

哪些方法会成为 intrinsic？以 `Math` 类为例：`Math.Abs(int)`、`Math.Max(int, int)` 这类方法的 IL 实现需要经过完整的解释器路径（加载参数 → 执行指令 → 返回值），但方法体本身只有一两条有效指令。把它们实现为 intrinsic，可以省掉解释器的帧构建、指令分派等全部开销，直接执行一个 C++ 函数。

intrinsic 的匹配在 transform 之前就已经发生。运行时在加载方法时检查方法的类型和签名是否匹配 intrinsic 注册表。如果匹配，方法的执行路径直接短路到 C++ 实现，不会进入后续的 transform 和解释器。

### 第二级：Internal Calls

`icalls/` 目录下 61 个文件，共 14,300 行代码。这是 BCL 中标记为 `[MethodImpl(MethodImplOptions.InternalCall)]` 的方法的运行时实现。

和 intrinsic 的区别在于：intrinsic 是对"有 IL 实现但性能关键"的方法的优化替代，internal call 是"根本没有 IL 实现"的方法的唯一实现。

典型的 internal call 包括：

- `System.Type.GetTypeFromHandle`：从运行时类型句柄获取 Type 对象
- `System.Array.Copy`：数组复制的底层实现
- `System.String.InternalAllocateStr`：字符串内存分配
- `System.GC.Collect`：垃圾收集触发
- `System.Threading.Monitor.Enter`：同步锁获取

这些方法在 C# 源码中只有声明没有实现体，因为它们的语义必须由运行时提供。每个 CLR 实现都需要自己提供这 61 组 internal call——CoreCLR 有自己的 ECall 表，Mono 有自己的 icall 注册，IL2CPP 在 `icalls/mscorlib/` 目录下提供实现，LeanCLR 同样如此。

`Method::is_internal_call` 检查方法的 flags 中是否设置了 `InternalCall` 标记。如果是，dispatch 直接走 internal call 路径，不进解释器。

### 第三级：Interpreter

如果前两级都不匹配，方法必须通过解释器执行。`Method::has_method_body` 检查方法是否有 IL 方法体——如果有，dispatch 进入 `Interpreter::execute`。

```cpp
if (Method::is_internal_call(method)) {
    // internal call path
} else if (Method::has_method_body(method)) {
    // interpreter path
} else {
    // error: no implementation available
}
```

最后一个分支是错误处理。如果一个方法既不是 intrinsic，也不是 internal call，也没有 IL 方法体，那就是一个无法执行的方法。在有 JIT 的运行时（CoreCLR）中，这个分支可以走 JIT 编译路径。但 LeanCLR 没有 JIT，所以这里直接报错。

### 虚方法分派

如果调用的是虚方法（`callvirt`），dispatch 还需要一个前置步骤：通过对象的实际类型找到正确的方法实现。

```cpp
const RtMethodInfo* resolved = Method::get_virtual_method_impl(obj, virtual_method);
```

`get_virtual_method_impl` 的逻辑是：从对象的 `klass` 指针找到 RtClass，再从 RtClass 的 VTable 中按方法索引定位 `RtVirtualInvokeData`，取出实际的方法指针。这和 LEAN-F4 分析的 VTable 结构直接对应。

对于接口方法调用，`get_vtable_method_invoke_data` 需要先在 RtClass 的接口映射表中查找接口方法到 VTable 槽位的映射，然后再做一次索引。这比普通虚方法调用多一次查找，但仍然是 O(1) 的操作。

## 首次调用：transform 触发

**源码位置：** `src/runtime/interp/interpreter.h`、`src/runtime/interp/hl_transformer.h`、`src/runtime/interp/ll_transformer.h`

当一个方法第一次通过解释器路径被调用时，还不能直接执行。LeanCLR 的解释器不执行原始 MSIL——它执行的是 LL-IL，这是 LEAN-F3 详细分析的三级 transform 管线的最终产物。

首次调用会触发 `init_interpreter_method`：

```
Interpreter::execute(method, ...)
  └─ 检查 method->interp_data
     ├─ 非空：直接使用缓存的 RtInterpMethodInfo
     └─ 空：init_interpreter_method(method)
            ├─ BasicBlockSplitter: 切分 MSIL 为基本块
            ├─ HLTransformer: MSIL(256) → HL-IL(182)
            │   ├─ 栈模拟（eval stack tracking）
            │   ├─ 语义归一化（编码合并）
            │   └─ 冗余 load/store 消除
            ├─ LLTransformer: HL-IL(182) → LL-IL(298)
            │   ├─ 类型特化（指令选择）
            │   └─ 参数/局部变量索引烘焙
            └─ 缓存 RtInterpMethodInfo 到 method->interp_data
```

`RtInterpMethodInfo` 是 transform 的产物，包含了执行所需的全部信息：

- LL-IL 指令流（`uint16_t` 数组）
- eval stack 深度和局部变量总大小
- 异常处理子句列表
- 参数和返回值的类型描述

transform 只在方法首次被调用时执行一次。完成后，`RtInterpMethodInfo` 被缓存到 `method->interp_data`，后续调用直接复用。这和 HybridCLR 的策略完全一致——HCLR-5 中的第 6 步和第 7 步（`GetInterpMethodInfo` → `HiTransform::Transform`）做的是同样的事情：懒触发 transform，缓存结果。

但 transform 的内部结构不同。HybridCLR 做一次 transform（CIL → HiOpcode），产出 1000+ 条特化指令。LeanCLR 做两次 transform（MSIL → HL-IL → LL-IL），总共 480 条指令。LEAN-F3 已经详细对比过两种策略的取舍——单层 transform 减少运行时 transform 次数但增加 opcode 维护成本，双层 transform 降低单层复杂度但多一次中间转换。

## 执行循环

**源码位置：** `src/runtime/interp/interpreter.cpp`、`src/runtime/interp/interp_defs.h`

transform 完成后（或从缓存中取回 `RtInterpMethodInfo` 后），进入执行循环。

```
Interpreter::execute(method, frame)
  ├─ 从 MachineState pool 分配 InterpFrame
  │   ├─ 设置 ip = LL-IL 指令流起始位置
  │   ├─ 设置 locals 基地址（偏移已在 LL Transform 中烘焙）
  │   ├─ 设置 args 基地址
  │   └─ 设置 stack 基地址
  ├─ 进入 dispatch loop
  │   └─ while(true):
  │       opcode = *ip++
  │       switch(opcode):
  │         case LL_ADD_I4: stack[dst].i32 = stack[a].i32 + stack[b].i32
  │         case LL_CALL:   递归调用 invoke_with_run_cctor
  │         case LL_CALLVIRT: 虚方法解析 → 递归调用
  │         case LL_RET:    return
  │         ... (298 cases total)
  └─ 弹出 InterpFrame，回退 pool 指针
```

LEAN-F3 已经分析了 dispatch loop 的设计选择：LeanCLR 使用 switch dispatch，原因是可移植性优先——目标平台包括 WebAssembly/Emscripten，对 computed goto 的支持有限。

执行循环中值得关注的几个细节：

**方法间互调。** 当 LL-IL 中遇到 `LL_CALL` 或 `LL_CALLVIRT` 指令时，执行循环不是简单地跳转，而是递归调用 `invoke_with_run_cctor`。这意味着被调用的方法会重新走一遍完整的分派流程——`.cctor` 检查、三级 fallback、首次 transform（如果需要）。调用完成后，返回值写入调用者的 eval stack，继续执行下一条 LL-IL 指令。

**异常处理。** 当执行过程中抛出异常时，解释器从当前 InterpFrame 开始沿 `parent` 链向上遍历，寻找匹配的 catch 子句。这是 ECMA-335 定义的两遍扫描机制的实现：第一遍找到匹配的 catch handler，第二遍执行路径上所有的 finally 块，最后跳转到 catch handler 继续执行。

**帧回收。** 方法执行完成后（遇到 `LL_RET` 或异常传播完成），InterpFrame 被弹出，MachineState 的 pool 指针回退。这是 LEAN-F3 中分析的 bump pointer 分配的回收路径——因为帧的生命周期严格 LIFO，所以回收就是移动指针，不需要 free。

## 与 HybridCLR 调用链的完整对比

把两条链并排放在一起：

| 步骤 | HybridCLR（HCLR-5 的 8 步） | LeanCLR |
|------|-----|---------|
| 加载入口 | `Assembly.Load(byte[])` → `AppDomain::LoadAssemblyRaw` → IL2CPP icall | `leanclr_load_assembly` → `Assembly::load_by_name`（C API 直入） |
| 程序集构建 | `Assembly::Create` → 构造 `InterpreterImage` → 包装为 `Il2CppAssembly` → 注册 `MetadataCache` | `CliImage::load_streams` → `RtModuleDef::create`（自有结构，无需包装） |
| 类初始化 | `Class::Init` / `Class::SetupMethods`（IL2CPP 已有路径） | `Class::initialize_all`（自行实现 field layout、vtable、.cctor 标记） |
| invoker 绑定 | `SetupMethods` 绑定 `invoker_method` → `InterpreterInvoke` | 无独立 invoker 机制；dispatch 在 `Runtime::invoke_with_run_cctor` 内部完成 |
| 调用触发 | `MethodInfo.Invoke` → `method->invoker_method(...)` | 宿主或解释器直接调用 `Runtime::invoke_with_run_cctor(method, ...)` |
| .cctor 执行 | `Class::Init` 中触发 | `invoke_with_run_cctor` 内部触发 `run_class_static_constructor` |
| 分派机制 | 函数指针（`invoker_method` 已绑定为 `InterpreterInvoke`） | 三级 fallback（Intrinsics → InternalCalls → Interpreter） |
| 参数整理 | `InterpreterInvoke` 将 `void**` 转为 `StackObject[]`（`alloca`） | `invoke_with_run_cctor` 内部参数适配 |
| transform 触发 | `GetInterpMethodInfo` → `HiTransform::Transform` | `init_interpreter_method` → HL Transform → LL Transform |
| transform 层数 | 1 级（CIL → HiOpcode，1000+ 条） | 2 级（MSIL → HL-IL → LL-IL，480 条） |
| 执行 | `Interpreter::Execute`（switch on `HiOpcodeEnum`） | `Interpreter::execute`（switch on LL-IL opcode） |

从这张表中可以看出几个结构性差异。

**适配层的存在与否。** HybridCLR 的链路中有大量适配工作：把热更程序集包装成 `Il2CppAssembly`，把方法绑定到 `invoker_method` 函数指针，在 `InterpreterInvoke` 中做参数格式转换。这些步骤的存在是因为 HybridCLR 必须让热更代码看起来和 AOT 代码一样——对 IL2CPP 的其他部分来说，热更方法和 AOT 方法的调用方式是统一的。

LeanCLR 不需要这些适配。它的所有方法都走同一条 dispatch 路径，不区分"热更"和"非热更"，因为它根本没有 AOT 的概念——所有代码都是解释执行的（除了 intrinsics 和 internal calls）。

**分派粒度。** HybridCLR 的分派是二元的：要么走 AOT invoker，要么走 `InterpreterInvoke`。这个分派在方法初始化阶段就已经确定了（`SetupMethods` 绑定 `invoker_method`），调用时不需要再判断。

LeanCLR 的分派是三级 fallback，每次调用都需要按顺序检查 intrinsic → internal call → interpreter。但因为 intrinsic 和 internal call 的检查是简单的标志位判断（O(1)），实际开销可以忽略。

**transform 策略。** 两者都采用懒 transform（首次调用时触发），但 transform 的内部结构不同。HybridCLR 做一次 transform 产出 `InterpMethodInfo`，LeanCLR 做两次 transform 产出 `RtInterpMethodInfo`。LEAN-F3 已经详细分析了两种策略的取舍。

**链路长度。** HybridCLR 的 8 步中，有 3 步是纯适配工作（步骤 3、4、8 的 `PREPARE_NEW_FRAME_FROM_NATIVE`），不承载核心逻辑。LeanCLR 的链路可以压成 5 步：加载 → 类初始化 → .cctor + dispatch → transform（首次）→ 执行。更短的链路反映了独立运行时的结构优势——不需要和已有框架对接，就不需要适配层。

## 完整调用链：从头到尾

把上面所有步骤串起来，LeanCLR 中一个方法从加载到执行的完整路径：

```
1. 加载
   leanclr_load_assembly("CoreTests")
   → Assembly::load_by_name
   → Settings::file_loader (宿主回调获取 DLL 字节流)
   → CliImage::load_streams (PE/COFF + metadata 解析)
   → RtModuleDef::create (构建 RtClass / RtMethodInfo / RtFieldInfo)

2. 类初始化
   Class::initialize_all
   → init_corlib_classes (核心类型缓存)
   → field layout 计算
   → vtable 构建
   → .cctor 状态标记

3. 方法调用触发
   Runtime::invoke_with_run_cctor(method, obj, params)
   → run_class_static_constructor(method->klass)  [幂等]

4. 三级 fallback 分派
   ├─ Intrinsics::invoke()      → 直接执行 C++ 实现，返回
   ├─ InternalCalls::invoke()   → 执行 icall 实现，返回
   └─ Interpreter::execute()    → 继续到步骤 5

5. 首次 transform（缓存命中则跳过）
   init_interpreter_method(method)
   → BasicBlockSplitter: MSIL → 基本块
   → HLTransformer: MSIL → HL-IL (182 opcodes)
   → LLTransformer: HL-IL → LL-IL (298 opcodes)
   → 缓存 RtInterpMethodInfo

6. 执行
   分配 InterpFrame (从 MachineState pool)
   → dispatch loop (switch on LL-IL opcode)
   → 遇到 LL_CALL/LL_CALLVIRT: 递归到步骤 3
   → 遇到 LL_RET: 弹出帧，返回值写入调用者 eval stack

7. 帧回收
   回退 MachineState pool 指针
```

步骤 1 和 2 是加载期，只执行一次。步骤 3-6 是调用期，每次方法调用都会经过。步骤 5 的 transform 只在首次调用时发生，后续走缓存。

这条链里最容易被忽略的边界是步骤 3 和步骤 6 之间的递归关系。解释器执行 LL-IL 指令时，遇到方法调用指令会递归回到步骤 3——这意味着被调用的方法会重新走一遍完整的分派流程，包括 .cctor 检查和三级 fallback。调用深度受 MachineState pool 的大小限制，超出时栈溢出。

## 收束

LeanCLR 的方法调用链用一条直线连接了 5 个子系统：metadata 解析（F2）、对象模型（F4）、双解释器（F3）、类型系统（F5 待写）、以及本篇分析的 dispatch 逻辑。

和 HybridCLR 的 8 步链对比，最根本的差异不在于具体的技术选择（懒 transform、switch dispatch 这些两者都一样），而在于架构前提：HybridCLR 在 IL2CPP 内部工作，需要大量适配代码让热更代码融入已有框架；LeanCLR 是独立运行时，所有结构自己定义，链路更短更直接，但每一步都要自己实现。

这也是理解两条技术路线的核心：HybridCLR 的工程挑战在于适配——怎么在不改变 IL2CPP 外部行为的前提下加入解释执行能力。LeanCLR 的工程挑战在于完整性——怎么用最少的代码实现一个能跑起来的 CLR。

从 LEAN-F1 的全景地图到 F6 的调用链，LeanCLR 核心模块的分析到这里完结。6 篇文章覆盖了架构总览、metadata 解析、双解释器、对象模型、类型系统、方法调用链。后续如果继续深入，可选方向包括 GC 接口设计、异常处理的完整实现、以及 WebAssembly 目标平台的特殊适配。

## 系列位置

- 上一篇：LEAN-F5 类型系统（计划中）
- 下一篇：LeanCLR 核心模块完结
