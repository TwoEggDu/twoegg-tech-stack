---
title: "横切对比｜P/Invoke 与 native interop：5 个 runtime 的原生互操作策略"
slug: "runtime-cross-pinvoke-native-interop-comparison"
date: "2026-04-14"
description: "同一个 DllImport 声明，在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出了五种完全不同的绑定路径：运行时动态加载、dllmap 映射、构建时静态绑定、ReversePInvokeWrapper 桥接、C 函数指针注册。从声明方式、绑定时机、动态加载支持、WASM 适配、callback 机制到 COM Interop，逐维度横切对比五种 native interop 实现的设计 trade-off。"
weight: 88
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "LeanCLR"
  - "Mono"
  - "PInvoke"
  - "Interop"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> 托管代码和 native 代码之间的边界不是一条线——它是一个需要处理调用约定、参数编组、生命周期管理和异常传播的完整过渡层。五个 runtime 对这一层的实现策略，直接决定了谁能在运行时加载新的 native 库、谁只能在构建时静态绑定。

这是 .NET Runtime 生态全景系列的横切对比篇第 9 篇（CROSS-G9）。

CROSS-G8 对比了体积与嵌入性——五个 runtime 的裁剪策略和嵌入成本。体积面对的是 runtime 自身的"瘦身"问题。Native interop 面对的是另一个维度的问题：托管代码怎么越过边界、调用 C/C++ 原生函数。这个能力对游戏引擎集成、系统 API 访问、第三方 native 库复用都不可或缺。

## 为什么 native interop 值得横切对比

所有 CLR 实现都需要和 native 世界交互。原因很直接：CLI 类型系统和 CIL 指令集覆盖不了所有操作——文件 I/O 要调用 OS API，图形渲染要调用 GPU driver 接口，音频处理要调用平台专用库，物理引擎、网络库、加密库大量以 C/C++ 形态存在。

ECMA-335 Partition II 15.5 定义了 P/Invoke（Platform Invocation Services）的声明语义：用 `DllImport` 属性标注一个 `extern` 方法，指定 native 库名和入口点名。规范定义了声明的语法和参数编组的规则，但不规定 runtime 怎么定位 native 库、怎么绑定函数地址、能不能在运行时动态加载。

这些"怎么做"的决策，构成了五个 runtime native interop 策略的核心分化点。

> **本文明确不展开的内容：**
> - 参数编组（marshalling）的完整规则（编组是各 runtime 共享的 ECMA-335 规范内容，不是实现差异的主要来源）
> - Unsafe 代码和指针操作（属于 IL 层面的能力，不涉及跨边界调用）
> - 各 runtime 的 internal call 实现细节（LEAN-F8 和各模块纵深篇已覆盖）

先给一个直觉层面的分类：

```
                       [DllImport("nativelib")]
                              │
         ┌──────────┬─────────┼──────────┬──────────┐
         │          │         │          │          │
      CoreCLR    Mono      IL2CPP    HybridCLR   LeanCLR
         │          │         │          │          │
   运行时动态   运行时动态   构建时静态   复用 IL2CPP  函数指针
   dlopen/     dllmap +     C++ 函数    + Reverse   注册表
   LoadLibrary  dlopen      直接调用    PInvoke     + EM_JS
```

CoreCLR 和 Mono 走的是传统路线——运行时根据库名动态定位和加载 native 库。IL2CPP 走了一条完全不同的路——构建时已经把 P/Invoke 声明翻译成 C++ 函数调用，运行时不存在"加载 native 库"这个步骤。HybridCLR 在 IL2CPP 的机制上需要额外处理热更 DLL 中的 P/Invoke。LeanCLR 作为嵌入式运行时，用自建的函数注册机制替代了系统级的动态加载。

下面逐个拆。

## CoreCLR — P/Invoke + COM Interop + LibraryImport

CoreCLR 的 native interop 是五个 runtime 中功能最完整的，支持三种主要机制。

### 经典 P/Invoke（DllImport）

声明方式：

```csharp
[DllImport("nativelib", CallingConvention = CallingConvention.Cdecl)]
static extern int NativeFunction(int arg1, [MarshalAs(UnmanagedType.LPStr)] string arg2);
```

当 JIT 编译到对这个方法的调用时，CoreCLR 的 P/Invoke 基础设施启动以下链路：

```
JIT 编译遇到 P/Invoke 调用
  → NDirect::NDirectLink（P/Invoke 准备）
    → LoadLibrary / dlopen 加载 "nativelib"
      → GetProcAddress / dlsym 定位 "NativeFunction"
  → 生成 P/Invoke stub（IL stub 或 inlined）
    → 参数编组：managed → native
    → GC 转换：Cooperative → Preemptive
    → 调用 native 函数
    → GC 转换：Preemptive → Cooperative
    → 返回值编组：native → managed
```

几个关键设计点：

**运行时动态加载。** native 库在首次调用时才加载。CoreCLR 调用操作系统的动态链接器接口（Windows 上 `LoadLibraryEx`，Linux 上 `dlopen`，macOS 上 `dlopen`）。这意味着 native 库可以在部署时替换，不需要重新编译托管代码。

**GC 转换。** P/Invoke 调用进入 native 代码前，当前线程从 Cooperative 模式切换到 Preemptive 模式。Cooperative 模式下线程必须在 GC 安全点暂停，Preemptive 模式下线程告诉 GC "你可以随时回收，我不会碰托管堆"。这个模式切换是 P/Invoke 的固定开销之一。

**IL stub 生成。** 对于需要参数编组的 P/Invoke（比如 `string` → `char*`），JIT 会生成一段 IL stub 来完成编组逻辑。编组包括内存分配（为 native 字符串分配 buffer）、数据复制（managed string 内容复制到 native buffer）、调用后清理（释放 buffer）。对于 blittable 类型（`int`、`double`、结构体中全是 blittable 字段），JIT 可以省略编组直接传递。

### LibraryImport（.NET 7+）

.NET 7 引入了 `LibraryImport` 作为 `DllImport` 的编译时替代：

```csharp
[LibraryImport("nativelib")]
static partial int NativeFunction(int arg1, [MarshalAs(UnmanagedType.Utf8)] string arg2);
```

关键区别：`LibraryImport` 使用源码生成器在编译时生成编组代码，而 `DllImport` 在运行时由 JIT 生成。编译时生成的好处是可以被 Native AOT 使用（Native AOT 没有运行时 JIT 能力来生成 IL stub），同时减少了运行时的反射和 metadata 查询开销。

### COM Interop

CoreCLR 在 Windows 上支持完整的 COM Interop——可以调用 COM 组件，也可以把 .NET 对象暴露为 COM 组件。COM Interop 依赖 Windows 的 COM 基础设施（注册表、CoCreateInstance、IUnknown/IDispatch 接口），在非 Windows 平台上不可用。

在五个 runtime 中，只有 CoreCLR 支持 COM Interop。这是因为 COM 是 Windows 特有的技术，其他 runtime 面向的场景（游戏、嵌入式、WASM）不需要 COM 支持。

### callback：托管 → native → 托管

当 native 代码需要回调托管方法时，CoreCLR 通过 delegate 的 `Marshal.GetFunctionPointerForDelegate` 生成一个 native 函数指针：

```csharp
delegate void Callback(int result);

[DllImport("nativelib")]
static extern void RegisterCallback(IntPtr callback);

Callback cb = OnResult;
IntPtr ptr = Marshal.GetFunctionPointerForDelegate(cb);
RegisterCallback(ptr);
```

JIT 为这个 delegate 生成一段 thunk 代码：native 调用方通过函数指针进入 thunk，thunk 完成 Preemptive → Cooperative 的 GC 转换，然后调用托管方法。delegate 对象必须在 native 回调期间保持存活（不被 GC 回收），否则函数指针失效。

## Mono — P/Invoke + dllmap

Mono 的 P/Invoke 在声明语法上和 CoreCLR 一致——同样使用 `DllImport`——但加载和绑定机制有几个关键差异。

### dllmap：跨平台库名映射

Mono 面对的核心问题是：同一个 native 库在不同平台上的文件名不同。Windows 上是 `user32.dll`，Linux 上可能是 `libX11.so`，macOS 上可能是 `libSystem.dylib`。如果 DllImport 硬编码了 `"user32.dll"`，在 Linux 上就找不到库。

Mono 的解决方案是 dllmap——在配置文件中定义库名映射：

```xml
<configuration>
  <dllmap dll="nativelib" target="libnativelib.so" os="linux" />
  <dllmap dll="nativelib" target="libnativelib.dylib" os="osx" />
  <dllmap dll="user32.dll" target="libgdiplus.so" os="linux" />
</configuration>
```

运行时在加载 native 库前先查 dllmap 配置，将 DllImport 中的库名替换为当前平台的实际文件名，然后调用 `mono_dl_open`（Mono 对 `dlopen` 的封装）加载。

CoreCLR 在 .NET Core 3.0+ 引入了 `NativeLibrary.SetDllImportResolver` API 来解决类似问题，但 Mono 的 dllmap 出现更早，是 Mono 作为跨平台 runtime 先驱的产物。

### mono_dl_open 与搜索路径

Mono 不使用操作系统的 `LoadLibrary`（Windows）或直接调用 `dlopen`（Linux），而是通过 `mono_dl_open` 封装了一层统一的加载逻辑。这层封装负责：

- 应用 dllmap 映射
- 按优先级搜索多个目录（应用目录 → 配置的搜索路径 → 系统目录）
- 处理平台相关的文件扩展名
- 在搜索失败时提供有意义的错误信息

### Unity 中的 Mono P/Invoke

Unity 在 Editor 模式下使用 Mono（或 CoreCLR，取决于版本），native plugin 通过 P/Invoke 调用。Unity 的 plugin 机制要求 native 库放在 `Assets/Plugins` 目录下对应平台的子目录中，Editor 在加载时按平台规则搜索。

这一层搜索逻辑叠加在 Mono 的 `mono_dl_open` 之上，实际的加载链路是：

```
DllImport("nativelib")
  → Unity Plugin Manager 路径解析
    → Mono dllmap 查询
      → mono_dl_open → dlopen / LoadLibrary
```

### callback 机制

Mono 的 managed → native → managed 回调机制与 CoreCLR 在概念上一致——通过 delegate 生成 native 函数指针，native 代码通过函数指针回调。Mono 使用 `mono_delegate_to_ftnptr` 生成 thunk。

## IL2CPP — 构建时静态绑定

IL2CPP 的 native interop 和前两者走了完全不同的路线。P/Invoke 声明在构建时已经被 il2cpp.exe 转换器处理完毕，运行时不存在"动态加载 native 库"这个步骤。

### 构建时处理

il2cpp.exe 在将 IL 转换为 C++ 代码时，遇到 `DllImport` 声明会生成对应的 C++ 外部函数声明：

```csharp
// C# 侧声明
[DllImport("nativelib")]
static extern int NativeFunction(int arg);
```

```cpp
// il2cpp.exe 生成的 C++ 代码
extern "C" int32_t NativeFunction(int32_t arg);

// 调用点
int32_t result = NativeFunction(p0);
```

转换器直接把 P/Invoke 调用翻译为 C++ 层面的外部函数调用。native 库的链接在 C++ 编译和链接阶段完成——native 函数要么静态链接进最终 binary，要么由平台的 native 链接器在加载时解析。

### 不支持运行时动态加载

IL2CPP 不支持在运行时动态加载新的 native 库。原因是 P/Invoke 的绑定已经在构建时完成，运行时没有"解析 DllImport 属性 → 搜索库文件 → 获取函数地址"的基础设施。

这个限制在多数移动游戏场景下不是问题——native plugin 在构建时已经打入包体。但对于需要运行时加载 native 扩展的场景（插件系统、mod 支持），这是一个硬约束。

### __Internal 约定

IL2CPP 有一个特殊约定：当 `DllImport` 的库名为 `"__Internal"` 时，表示 native 函数定义在当前二进制内部（静态链接），不需要外部库：

```csharp
[DllImport("__Internal")]
static extern void InternalNativeFunction();
```

这个约定在 iOS 平台上尤为重要——iOS 不允许应用动态加载代码（App Store 审核限制），所有 native 代码必须静态链接。`__Internal` 是 IL2CPP 在 iOS 上使用 native plugin 的标准方式。

### 编组处理

IL2CPP 的参数编组在 C++ 层面完成。转换器生成的 C++ 代码中包含编组逻辑——managed string 到 native string 的转换、结构体布局的调整、指针的提取。这些编组代码是静态生成的（由 il2cpp.exe 在构建时根据 MarshalAs 属性生成），不像 CoreCLR 在运行时动态生成 IL stub。

### callback

IL2CPP 的 managed → native → managed 回调通过生成的 C++ 包装函数实现。il2cpp.exe 为每个可能被 native 回调的 delegate 类型生成一个 reverse P/Invoke wrapper——一个 C++ 函数，接受 native 调用约定的参数，内部调用对应的托管方法。

## HybridCLR — 复用 IL2CPP + ReversePInvokeWrapper

HybridCLR 在 IL2CPP 之上叠加了解释器。AOT 部分的 P/Invoke 完全走 IL2CPP 的现有机制——构建时静态绑定，不需要额外处理。真正需要额外工作的是热更 DLL 中的 P/Invoke。

### 热更 DLL 中的 P/Invoke

热更 DLL 是运行时加载的——这些 DLL 中的代码由 HybridCLR 解释器执行。当热更代码中有 P/Invoke 声明时，HybridCLR 面对一个问题：IL2CPP 的 P/Invoke 机制是构建时静态绑定的，而热更代码在构建时不存在。

HybridCLR 的处理策略：热更 DLL 中的 `DllImport("__Internal")` 调用，由 HybridCLR 在解释器内部匹配到已经静态链接在 binary 中的 native 函数。这要求 native 函数在构建时已经被包含进最终二进制——热更代码不能调用构建时不存在的 native 库。

### ReversePInvokeWrapper

更复杂的场景是 native 代码需要回调热更 DLL 中的托管方法。

IL2CPP 的 reverse P/Invoke wrapper 是构建时生成的 C++ 函数。但热更 DLL 中的 delegate 在构建时不存在，il2cpp.exe 无法为它们生成 wrapper。

HybridCLR 的解决方案是 `ReversePInvokeWrapper` 机制：

```
AOT 构建时：
  HybridCLR Generate 阶段为热更 delegate 预生成一批通用 wrapper
    → 这些 wrapper 在 C++ 侧占位，运行时被绑定到具体的热更方法

运行时：
  热更代码注册 delegate 给 native 回调
    → HybridCLR 从预生成的 wrapper 池中分配一个
    → 将 wrapper 的函数指针传给 native 代码
    → native 回调时，wrapper 转入解释器执行热更方法
```

这个机制有一个约束：预生成的 wrapper 数量有上限。如果热更代码中注册了超出预生成数量的 native 回调，运行时会抛出 `GetReversePInvokeWrapper fail. exceed max wrapper num` 错误。实际工程中需要在 `HybridCLR → Generate` 阶段配置足够的 wrapper 数量。

### 限制

HybridCLR 的 native interop 受到 IL2CPP 的基础约束：不支持运行时动态加载新的 native 库。热更代码能调用的 native 函数，必须在 AOT 构建时已经存在于最终二进制中。热更代码能注册的 native 回调数量，受限于预生成 wrapper 池的大小。

这些限制在实际工程中通常可接受——游戏的 native plugin 在构建时确定，热更主要更新 C# 游戏逻辑，很少需要调用构建时不存在的 native 函数。

## LeanCLR — 函数指针注册 + EM_JS 桥接

LeanCLR 作为轻量级嵌入式 runtime，没有实现 ECMA-335 P/Invoke 规范的完整语义。它用一套更直接的机制来处理 managed 与 native 的交互。

### Custom P/Invoke 注册

LeanCLR 不通过操作系统的动态链接器加载 native 库。当 C# 代码声明 `[DllImport("__Internal")]` 时，LeanCLR 从自己维护的函数注册表中查找匹配的 C 函数实现：

```
C# 调用 DllImport("__Internal", "NativeFunc")
  → 解释器遇到 P/Invoke 调用
    → 查找 InternalCallRegistry（与 icall 共用注册机制）
      → 找到匹配的 C 函数指针
        → 通过 invoker 适配参数格式
          → 调用 C 函数
```

这个机制和 LEAN-F8 分析过的 internal call 注册共用同一套基础设施。嵌入方（游戏引擎、宿主程序）在初始化 LeanCLR 时注册需要暴露给 C# 的 native 函数，运行时按名称匹配。

### 为什么不用 dlopen

LeanCLR 的设计目标是零平台依赖。`dlopen`（Linux）和 `LoadLibrary`（Windows）是操作系统 API，依赖它们会破坏 Universal 版的跨平台承诺。而且 LeanCLR 面向的嵌入场景中，native 函数通常由宿主程序直接提供，不需要运行时去文件系统中搜索 `.so` / `.dll`。

函数注册机制让 native interop 变成了纯粹的编译时绑定 + 启动时注册——不涉及文件系统操作、不涉及 OS 动态链接器。

### WASM 场景：EM_JS 桥接 JavaScript

在 WebAssembly 环境下，"native 函数"的含义发生了变化——没有 `.so` 或 `.dll`，native 层是 JavaScript。LeanCLR 通过 Emscripten 的 `EM_JS` 宏将 C 函数桥接到 JavaScript：

```cpp
// C++ 侧：用 EM_JS 定义一个桥接函数
EM_JS(int, platform_create_canvas, (int width, int height), {
    const canvas = wx.createCanvas();
    canvas.width = width;
    canvas.height = height;
    return registerObject(canvas);
});
```

```csharp
// C# 侧：标准 DllImport 声明
[DllImport("__Internal")]
static extern int platform_create_canvas(int width, int height);
```

调用链路：C# 代码 → LeanCLR 解释器 → C 函数注册表 → `platform_create_canvas`（C 函数，编译为 WASM 导出）→ `EM_JS` 内联 JavaScript → 小游戏平台 SDK。

这种设计的优势是运行时核心代码不包含任何平台特定逻辑。从微信小游戏切换到抖音小游戏或 H5 网页，只需替换 JS 胶水层中的 `EM_JS` 实现，LeanCLR 运行时二进制不变。

### 非 WASM 场景

在原生平台（Windows、Linux、macOS、Android、iOS）上，LeanCLR 的 native interop 同样走函数注册机制。嵌入方在 C++ 层注册函数：

```cpp
// 宿主程序注册 native 函数
leanclr_register_internal_call("GameEngine.Native", "RenderFrame", &render_frame_impl);
```

这和 IL2CPP 的 `__Internal` 约定在效果上相似——native 函数在编译时已经确定，运行时按名称绑定。区别是 IL2CPP 在构建时由转换器生成绑定代码，LeanCLR 在启动时通过注册表动态绑定。

### callback 机制

LeanCLR 的 managed → native → managed 回调目前通过 internal call 层间接完成——native 回调不直接调用托管方法的函数指针，而是通过解释器的 invoke 接口触发执行。这比 CoreCLR 的 delegate 函数指针方式受限（native 代码需要知道 LeanCLR 的 invoke API），但实现简单，符合嵌入式运行时的定位。

## 5 方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| **声明方式** | DllImport / LibraryImport | DllImport | DllImport（构建时转 C++） | AOT: DllImport / 热更: DllImport("__Internal") | DllImport("__Internal") |
| **绑定时机** | 运行时首次调用 | 运行时首次调用 | 构建时（il2cpp.exe 转换） | AOT: 构建时 / 热更: 运行时匹配 | 启动时注册 |
| **动态加载支持** | 完整（LoadLibrary/dlopen） | 完整（mono_dl_open + dllmap） | 不支持 | 不支持（受 IL2CPP 约束） | 不支持（设计决策） |
| **WASM 适配** | 不适用（CoreCLR 不面向 WASM） | Blazor WASM 有限支持 | Unity WebGL 构建 | 受 IL2CPP WebGL 约束 | EM_JS 桥接 JavaScript |
| **callback 机制** | delegate → 函数指针（thunk） | delegate → ftnptr（thunk） | 构建时生成 reverse wrapper | 预生成 ReversePInvokeWrapper 池 | 解释器 invoke API |
| **COM Interop** | Windows 完整支持 | 不支持 | 不支持 | 不支持 | 不支持 |
| **GC 模式切换** | Cooperative → Preemptive | 类似切换 | C++ 层无 GC 模式区分 | AOT: C++ 层 / 热更: 解释器管理 | 解释器管理 |
| **编组方式** | 运行时 IL stub / 编译时源码生成 | 运行时 wrapper | 构建时 C++ 编组代码 | AOT: 同 IL2CPP / 热更: 解释器编组 | 注册时提供 invoker |
| **源码锚点** | `src/coreclr/vm/dllimport.cpp` | `mono/metadata/loader.c` | `il2cpp/vm/PInvoke.cpp` | `hybridclr/interpreter/` | `src/runtime/icalls/` |

## 收束

同一个 `DllImport` 声明，五个 runtime 给出了五种绑定策略。差异的根源是各自的执行模型和部署目标决定了 native 库的"可见时机"：

- CoreCLR 有运行时动态加载能力，P/Invoke 可以在首次调用时解析，native 库可以在部署时替换。功能最完整，包括 COM Interop 和 LibraryImport 源码生成。代价是依赖操作系统的动态链接器
- Mono 的 P/Invoke 能力和 CoreCLR 基本对等，加上 dllmap 机制解决了跨平台库名映射这个 CoreCLR 较晚才处理的问题
- IL2CPP 在构建时完成所有 P/Invoke 绑定，运行时不存在动态加载。这是 AOT 路线的自然推论——如果所有 IL 都在构建时转成了 C++，P/Invoke 也应该在构建时解析
- HybridCLR 复用 IL2CPP 的 P/Invoke 基础设施，通过 ReversePInvokeWrapper 预生成池解决热更代码的 native 回调问题。受限于 IL2CPP 的静态绑定模型
- LeanCLR 用函数注册表替代系统级动态加载，在 WASM 场景通过 EM_JS 桥接 JavaScript。最简单的实现，最少的平台依赖

Native interop 不是一个孤立的功能模块。它是 runtime 执行策略（JIT / AOT / Interpreter）和部署模型（桌面应用 / 移动游戏 / WASM）在 managed-native 边界上的直接投影。理解了这个约束结构，就理解了为什么 IL2CPP 不能运行时加载 native 库，也理解了为什么 LeanCLR 选择了函数注册而非 dlopen。

## 系列位置

这是横切对比篇第 9 篇（CROSS-G9），是对 CROSS-G8（体积与嵌入性）的延伸——体积决定了 runtime 能不能嵌入目标环境，native interop 决定了嵌入后 runtime 怎么和宿主环境交互。

前 8 篇横切对比覆盖了 runtime 的核心维度：metadata 解析、类型系统、方法执行、GC、泛型、异常处理、程序集加载与热更新、体积与嵌入性。这篇补上了 managed-native 边界交互这个在游戏引擎集成中不可绕过的维度。
