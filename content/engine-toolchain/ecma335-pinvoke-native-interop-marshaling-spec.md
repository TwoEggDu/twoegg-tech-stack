---
title: "ECMA-335 基础｜P/Invoke 与 Native Interop：marshaling 的规范层定义"
slug: "ecma335-pinvoke-native-interop-marshaling-spec"
date: "2026-04-15"
weight: 18
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: [ECMA-335, CLR, PInvoke, Interop, Marshaling]
---

> P/Invoke 不是某个 runtime 的私有功能，而是 ECMA-335 在规范层就定义好的托管/原生互操作机制——`DllImport` 标记、参数 marshaling 规则、calling convention 都在 Partition II §22.27 和 Partition I §12.4.5 里有精确定义。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 11 篇。

前几篇覆盖了 metadata、指令集、类型系统、执行模型、内存模型、custom attributes 等纯托管侧的规范。这一篇跨过托管/原生边界，讲 ECMA-335 怎么在规范层把 P/Invoke 这件事定义清楚——从 metadata 表的存储、参数 marshaling 描述符的编码，到 calling convention 的语义约束。

> **本文明确不展开的内容：**
> - 各 runtime 的 P/Invoke 实现细节（在 G9 横切对比展开）
> - C++/CLI 的 IJW（It Just Works）特殊情况
> - WinRT / COM Interop 的完整规范

## 为什么 P/Invoke 在规范层就要定义

托管代码不可能完全脱离原生世界存在。文件 IO 要走 OS 系统调用，网络要走 socket API，图形渲染要走 Direct3D / Vulkan / Metal，硬件交互要走平台 SDK——任何真实的应用都会在某个层次上调用原生库。

如果 P/Invoke 只是某个 runtime 的私有特性，会立刻产生两个问题：

第一，跨 runtime 的代码无法兼容。同一个 DLL 在 CoreCLR 上能跑，搬到 Mono 或 IL2CPP 上就需要重写互操作层——因为 marshaling 规则不一致，`DllImport` 的解释方式不同，calling convention 的处理也不同。这违反了 ECMA-335 作为标准的初衷：让同一份 IL 在不同 runtime 上有可预测的行为。

第二，工具链无法理解 P/Invoke 调用点。AOT 编译器（IL2CPP、NativeAOT、CoreRT）需要在编译期就识别所有 P/Invoke 调用，为每个调用点生成 marshaling stub；静态分析器需要识别哪些方法跨过托管边界；安全审计需要列出程序集对哪些 native API 有依赖。如果 P/Invoke 信息散落在 CustomAttribute 里靠字符串匹配，这些工具的行为都会变得脆弱。

ECMA-335 的解法是把 P/Invoke 提升到 metadata 表的一等公民——专门设计 ImplMap 表存储 P/Invoke 元数据，专门定义 MarshalingDescriptor 编码参数转换规则，专门在 Partition I §12.4.5 中规定 calling convention 的语义。这样所有 runtime 看到的是同一份规范，所有工具读取的是同一份结构化数据。

## ImplMap 表：P/Invoke 元数据存储

ECMA-335 Partition II §22.27 定义了 ImplMap 表，专门用于存储 P/Invoke 的元数据。一个常见的误解是 `[DllImport]` 是普通的 CustomAttribute——实际上 C# 编译器在生成 IL 时，会把 `DllImportAttribute` 翻译成 ImplMap 表中的一行记录，CustomAttribute 表里并不会留下这个 attribute 的痕迹。

ImplMap 表的每一行有四个字段：

**MappingFlags（2 字节）** — 一个位字段，编码三类信息。低 8 位表示 calling convention（cdecl / stdcall / thiscall / fastcall / winapi），中间几位表示字符集（Ansi / Unicode / Auto），剩余位表示名称匹配规则（是否 ExactSpelling、是否 BestFitMapping、SetLastError 是否启用）。所有这些在 C# 里都对应 `DllImport` 的 named parameters。

**MemberForwarded（HasFieldMarshal coded index）** — 指向被 P/Invoke 标记的方法或字段。绝大多数情况下是 MethodDef 表中的一行（即一个外部方法）。

**ImportName（#Strings 索引）** — 原生符号的名称。如果 `EntryPoint` 在 `DllImport` 里被显式指定，这里存的是 `EntryPoint` 的值；否则存的是托管方法的名称。

**ImportScope（ModuleRef 索引）** — 指向 ModuleRef 表中的一行，对应原生 DLL 的名称。ModuleRef 本身只存名称字符串，不存路径——具体怎么解析这个名称、在哪些目录里搜索对应的 DLL，是各 runtime 的实现细节。

举例：

```csharp
[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
public static extern IntPtr GetModuleHandleW(string lpModuleName);
```

在 metadata 里展开成：

- MethodDef 表：一行 `GetModuleHandleW`，标记 `pinvokeimpl`
- ModuleRef 表：一行 `kernel32.dll`
- ImplMap 表：一行，MappingFlags = StdCall | CharSetUnicode，MemberForwarded → MethodDef 行，ImportName → "GetModuleHandleW"，ImportScope → ModuleRef 行

这意味着任何能解析 metadata 的工具——ILDasm、ILSpy、Mono.Cecil、System.Reflection.Metadata——都能直接列出程序集的所有 P/Invoke 入口，不需要解析 CustomAttribute。

## MarshalingDescriptor：参数转换规则

光有 ImplMap 不够。一个 P/Invoke 方法的参数和返回值还需要一份"翻译规则"，告诉 runtime 怎么把托管对象转换成 native 表示。这份规则叫 marshaling descriptor，定义在 Partition II §22.16（FieldMarshal 表）和 §23.4（marshaling descriptor 编码）。

每个需要特殊 marshal 的参数或字段都关联一个 marshaling descriptor。如果参数是一个直接对应 native 类型的简单值（比如 `int` 对应 C 的 `int32_t`），可以省略 descriptor，runtime 用默认规则处理；只有需要非默认转换时（比如 `string` 转 `char*`），才必须显式指定。

ECMA-335 §23.4 定义了一组 NATIVE_TYPE_* 常量，描述 native 侧的目标类型：

**简单类型** — `NATIVE_TYPE_BOOLEAN`（注意 native bool 在不同 ABI 下大小不同：Win32 BOOL 是 4 字节，C99 _Bool 是 1 字节，VARIANT_BOOL 是 2 字节，descriptor 必须明确指定）、`NATIVE_TYPE_LPSTR`（ANSI 字符串指针）、`NATIVE_TYPE_LPWSTR`（UTF-16 字符串指针）、`NATIVE_TYPE_LPUTF8STR`（UTF-8 字符串指针）等。

**数组** — `NATIVE_TYPE_ARRAY` 后面跟元素类型和长度信息。长度可以是常量，也可以是另一个参数的索引（"长度由第 N 个参数指定"）。

**结构体** — `NATIVE_TYPE_STRUCT`，靠 `StructLayoutAttribute` 控制内存布局（sequential 还是 explicit），靠每个字段的 marshaling descriptor 控制字段级转换。

**委托** — `NATIVE_TYPE_FUNC`，把托管 delegate 转换为 C 函数指针。runtime 必须保证 delegate 对象在 native 持有期间不被 GC，常见做法是要求调用方手动 `GCHandle.Alloc`。

**自定义** — `NATIVE_TYPE_CUSTOMMARSHALER`，指向一个实现了 `ICustomMarshaler` 接口的托管类型，由这个类型负责双向转换。

C# 中 `[MarshalAs(UnmanagedType.LPWStr)]`、`[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]` 这些 attribute，在 metadata 里都被编码成 marshaling descriptor 写入 FieldMarshal 表，并不留在 CustomAttribute 表里。

## Calling Convention 的规范定义

参数布局和数据转换是一回事，参数怎么传递给 native 函数是另一回事。Partition I §12.4.5 "Method calls" 定义了 CLI 自己的 calling convention（用于 IL `call` 指令）和 native calling convention 之间的映射关系。

CLI 自己的 callconv 有 default、varargs、generic 几种，全部由 IL 指令配合 method signature 描述。这是托管侧的 ABI，所有 .NET 方法调用都走它，与平台无关。

native callconv 是另一套，由目标平台的 ABI 规范定义。ECMA-335 在规范层只列出几种主流 convention 并定义其语义，具体的寄存器分配、栈对齐规则要看平台 ABI（System V AMD64、Win64、AArch64 PCS 等）：

**cdecl** — C 标准。调用方负责清理栈，参数从右到左压栈。支持变参（变参函数的参数数量编译期不可知，只能由调用方清栈）。

**stdcall** — Win32 默认。被调方清理栈，不支持变参。Win32 API 几乎全部使用 stdcall（在 64 位 Windows 上 stdcall 退化到与 cdecl 相同，因为 Win64 ABI 统一了寄存器传参规则）。

**thiscall** — C++ 成员函数。this 指针在 ECX（x86）或第一个寄存器参数（x64）传递，其他参数按 cdecl/stdcall 规则。

**fastcall** — 前若干个参数通过寄存器传递（x86 上是 ECX、EDX，x64 上更多）。现代 64 位 ABI 普遍以寄存器传参为默认行为，fastcall 在 64 位上意义弱化。

`calli` 指令（间接调用）在调用 native 函数时必须显式指定 callconv——signature 中带一个 callconv tag，告诉 JIT 或 AOT 编译器按哪种 ABI 生成调用代码。这也是为什么 verifier 不能验证 `calli`：函数指针在编译期不可知目标，verifier 无法证明被调函数的类型契约与 signature 匹配，只能假设调用方传入的指针有效。

## 管理边界：marshaling stub

光有 metadata 描述不够，调用真正发生时需要一段桥接代码完成转换。所有 runtime 的实现思路是相同的：在 P/Invoke 方法的入口生成一段 marshaling stub，由 stub 完成入参转换、callconv 适配、native 调用、出参转换、异常翻译这一系列工作。

stub 的工作步骤大致如下：

**入参转换** — 遍历每个参数，按 marshaling descriptor 把托管对象转成 native 表示。string → 分配 native 缓冲区并复制字符（按 charset 决定 ANSI/UTF-16/UTF-8）、array → 计算元素数量并准备连续内存（如果元素类型不需要逐个转换，可以直接 pin 托管数组拿地址；否则要复制）、struct → 按 layout 计算每个字段的偏移，递归 marshal 每个字段、delegate → 取出（或生成）C 函数指针。

**callconv 适配** — 把准备好的参数按目标 callconv 放到正确的寄存器和栈位置。这一步通常由 JIT/AOT 编译器直接生成机器码，stub 只准备数据。

**native 调用** — 跳转到目标函数。在调用前后通常要切换 GC 模式（从 cooperative mode 切到 preemptive mode），告诉 GC "现在运行的是 native 代码，不要等待这个线程到达 safe point 再触发 GC"。

**出参转换** — native 返回值按 descriptor 转回托管对象。如果有 `out` 或 `ref` 参数，把 native 写入的内存内容复制回托管对象（或 unpin 之前 pin 住的托管内存）。

**异常翻译** — native 端抛出的 SEH 异常（Windows）或 signal（Unix）需要翻译成 .NET Exception。这一步各 runtime 处理方式差异很大，规范只要求"native 异常不能裸露穿过托管边界"。

举一个延续 Player 类设定的例子：

```csharp
public class Player : Unit {
    [DllImport("game_native.dll")]
    public static extern int CalculateDamage(int baseAmount, ref Vector3 hitPoint);
}
```

stub 的工作很简单：

- `baseAmount` 是 `int`，已经是 native 表示，直接放入第一个寄存器
- `hitPoint` 是 `ref Vector3`，托管侧拿到的是 managed pointer。Vector3 是 `[StructLayout(LayoutKind.Sequential)]` 的值类型，runtime 直接把这个 pointer 转成 native pointer（在 GC 不会移动该位置的前提下，比如它在栈上或被 pin 住）放入第二个寄存器
- 调用 native 函数 `CalculateDamage`
- native 返回 `int32_t`，直接作为返回值传回托管侧

如果 `hitPoint` 是引用类型字段而不是栈上的局部变量，stub 还要先 pin 这个对象（防止 GC 在 native 调用期间移动它），native 返回后再 unpin。这是 P/Invoke 中最容易出错的地方之一——忘了 pin 会导致 native 拿到一个可能已经失效的指针。

## 各 runtime 的 P/Invoke 实现差异（最小桥接）

规范只定义"应该是什么"，各 runtime 在"具体怎么实现"上选择不同：

**CoreCLR** — 经典路线是 JIT 在第一次调用时按 metadata 动态生成 marshaling stub。.NET 7+ 引入 LibraryImport（`[LibraryImport]`），通过 source generator 在编译期生成 stub 的托管代码——避免运行时动态生成、对 trimming 和 NativeAOT 友好。

**Mono** — 同样是运行时生成 stub。额外提供 dllmap 配置文件机制，允许在配置里把 `kernel32.dll` 重定向到 `libSystem.dylib` 或其他名字，用于跨平台兼容（同一份 IL 在 Windows / Linux / macOS 上调用不同的 native lib）。

**IL2CPP** — 完全静态。构建时扫描所有 P/Invoke 调用点，为每个调用点生成 C++ stub 函数，编译进最终二进制。运行时没有"动态生成 stub"这条路径，自然也不支持运行时新增的 P/Invoke。

**HybridCLR** — 直接复用宿主 IL2CPP 的 P/Invoke 机制。AOT DLL 中的 P/Invoke 由 IL2CPP 在构建时生成 stub；热更 DLL 中的 P/Invoke 通过 ReversePInvokeWrapper 桥接到 AOT 侧已生成的 stub——前提是热更代码引用的 native 方法已经被某个 AOT DLL 调用过（即 stub 已被生成进二进制）。

**LeanCLR** — 自定义 P/Invoke 注册机制。WASM 场景下 native 调用最终走 EM_JS 桥接到 JavaScript 函数，整套 marshaling 在 LeanCLR 自己的 stub 层完成。

## 工程影响

规范层的设计选择最终落到实际工程中：

**AOT 限制。** `[DllImport]` 声明必须在编译期可见，AOT runtime 才能在构建时生成对应的 stub。IL2CPP 不支持运行时动态 P/Invoke——不能像 CoreCLR 那样写 `LoadLibrary + GetProcAddress + Marshal.GetDelegateForFunctionPointer` 的代码动态构造 native 调用。如果业务确实需要运行时加载 native 插件，要么用平台原生方式（Android JNI、iOS dlopen），要么把所有可能调用的 native 方法都预先用 `[DllImport]` 声明出来让 IL2CPP 提前生成 stub。

**HybridCLR 热更场景。** 热更 DLL 中新增的 P/Invoke 容易出问题。如果热更代码声明了一个 AOT 侧从未调用过的 native 方法，IL2CPP 的构建产物里没有对应的 stub，ReversePInvokeWrapper 就找不到桥接目标，运行时调用会崩溃。常规做法是在 AOT 侧预留一份"P/Invoke 引用表"，把所有热更代码可能用到的 native 方法都列一遍，强制 IL2CPP 生成 stub。

**callback 方向。** native → managed 的 callback 通过 delegate marshaling 完成。托管侧 `Marshal.GetFunctionPointerForDelegate` 拿到一个 native 可调用的函数指针，传给 native 注册。这里有一个隐藏风险：拿到函数指针后，托管 delegate 对象不再有任何 GC 根引用它，下次 GC 就被回收，native 持有的函数指针变成悬挂指针。正确做法是在调用方持有一个 `GCHandle.Alloc(delegate, GCHandleType.Normal)`，直到 native 不再需要这个 callback 时再 Free。这一点在所有 runtime 上都一致，是 P/Invoke 工程实践中最常见的踩坑之一。

## 收束

P/Invoke 不是各 runtime 的"附加功能"，而是 ECMA-335 在 metadata 表层就定义好的标准机制。三层规范层层递进：

**ImplMap 表层。** Partition II §22.27 把 P/Invoke 提升到 metadata 一等公民，每个 `[DllImport]` 都对应一行结构化记录，存储 calling convention、charset、native 符号名、目标 DLL。任何工具读取这一表就能列出程序集的所有 native 依赖。

**MarshalingDescriptor 层。** Partition II §22.16 + §23.4 用 NATIVE_TYPE_* 常量定义参数转换规则，覆盖简单类型、字符串、数组、结构体、委托等所有需要双向翻译的场景。`[MarshalAs]` attribute 是这一层的源代码语法糖。

**Calling Convention 层。** Partition I §12.4.5 定义托管 callconv 与 native callconv 之间的映射，规定 `calli` 指令在调用 native 时必须显式指定 callconv，并明确 verifier 不能验证 `calli` 的类型安全。

理解这三层规范，就抓住了任何 .NET runtime native interop 行为的共同骨架。各 runtime 在"什么时候生成 stub"（运行时 vs 编译时）、"怎么解析 native 符号"（动态 vs 静态）、"GC 模式怎么切换"（cooperative vs preemptive）上的差异，都是在这同一份规范之上做出的工程选择。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/ecma335-custom-attributes-reflection-encoding.md" >}}">A9 Custom Attributes</a>
- 下一篇：<a href="{{< relref "engine-toolchain/ecma335-cli-security-strong-name-cas.md" >}}">A11 CLI Security 模型</a>
