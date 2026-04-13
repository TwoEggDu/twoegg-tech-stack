---
date: "2026-03-26"
title: "HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出，一次完整的 native crash 符号化分析"
description: "记录一次真实的 HybridCLR 接入问题：先是 TypeLoadException 卡在加载顺序，修完后进入 native crash，通过 adb logcat + llvm-addr2line 符号化 libil2cpp.so 回溯栈帧，最终定位到 AOTGenericReferences 空导致 async UniTask 在解释器和 AOT 之间无限递归。"
weight: 43
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Troubleshooting"
  - "UniTask"
  - "NativeCrash"
  - "CaseStudy"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> 很多 HybridCLR 问题不是在第一行报错里，而是第一行报错修完之后，后面那层才真正暴露出来。

这是 HybridCLR 系列第 14 篇。
前面的系列已经把原理、工具链、AOT 泛型、排障手册都拆开了；这一篇不讲新概念，只记录一次真实的接入调试过程。

两个问题串成一条线：

1. 托管层的 `TypeLoadException`——热更 DLL 加载顺序搞错了
2. Native crash——`AOTGenericReferences.cs` 留空，导致 `async UniTask` 在 HybridCLR 解释器和 UniTask AOT 代码之间无限递归，栈溢出

## 问题 1：TypeLoadException

### 现象

打完 APK 安装上机，一启动就闪退。抓 `adb logcat`，找 Unity tag：

```
I Unity: Assembly [GameProto] loaded from MPQ
E Unity: Exception: System.TypeLoadException: Could not load type 'DP.Client.EventFactory' from assembly 'DP.Client'.
  at Procedure.ProcedureLoadAssembly.LoadAssembly ()
```

`GameProto` 加载成功，紧接着就抛了 `TypeLoadException`。报错是 `DP.Client.EventFactory`，但下一行加载的应该是 `GameLogic`——说明是 `GameLogic` 在加载或解析时，引用了 `DP.Client.EventFactory`，而此时 `DP.Client` 还没被 `Assembly.Load` 进 runtime。

### 根因

查 `UpdateSetting.asset` 里的 `HotUpdateAssemblies` 顺序：

```
GameProto.dll
GameLogic.dll      ← 第 2 个
DP.Client.dll      ← 第 3 个，但 GameLogic 依赖它
DP.Data.dll
DP.GenClient.dll
```

`GameLogic` 引用了 `DP.Client.EventFactory`，但 `DP.Client` 被排在 `GameLogic` 后面加载。`Assembly.Load` 按顺序执行，当 `GameLogic` 被加载进 runtime、CLR 解析其类型引用时，`DP.Client` 还不在 runtime 里，只有 IL2CPP 编译进二进制的 AOT 版本——而 AOT 版本经过了 strip，`EventFactory` 被裁掉了，`TypeLoadException` 就在这里出来。

### 分析方法

这类问题的诊断不需要符号化，日志就够了：

- `TypeLoadException` 中报的类属于哪个 DLL
- 那个 DLL 在加载顺序里排在第几位
- 报错是在加载哪个 DLL 时触发的

本质上是：**CLR 解析依赖的时机早于依赖本身的加载时机**。

### 修法

静态分析各 DLL 的依赖图，按从叶到根的顺序排列：

```
GameProto     ← 无依赖（热更 DLL 范围内）
DP.Data       ← 无依赖（热更 DLL 范围内）
DP.GenClient  ← 依赖 DP.Data
DP.Client     ← 依赖 DP.Data
GameLogic     ← 依赖 DP.Client、DP.Data、DP.GenClient（全部）
```

改后的 `HotUpdateAssemblies` 顺序：

```
GameProto.dll
DP.Data.dll
DP.GenClient.dll
DP.Client.dll
GameLogic.dll
```

重新打包，`TypeLoadException` 消失，五个 DLL 全部 `loaded from MPQ`。

---

## 问题 2：Native crash in libil2cpp.so

### 现象

修完加载顺序之后再装机，logcat 显示热更 DLL 全部加载成功，`GameApp.Entrance` 和 `StartGameLogic` 都打印出来了——然后 app 闪退回桌面。

这次没有 `Exception` 级别的日志，是静默崩溃。

### 第一步：看 CRASH tag

Unity 的 native crash 不会走 `Unity` tag，而会走 `CRASH` tag。过滤 `adb logcat` 找 `CRASH`：

```
E CRASH: *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***
E CRASH: Version '2022.3.60f1', Build type 'Release', Scripting Backend 'il2cpp', CPU 'arm64-v8a'
E CRASH: pid: 22923, tid: 22966, name: UnityMain  >>> com.spiritgo.android <<<
E CRASH: signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr --------
E CRASH: Cause: null pointer dereference
E CRASH: backtrace:
E CRASH:   #00 pc 00000000046951fc  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #01 pc 0000000003939604  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #02 pc 0000000003946358  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #03 pc 00000000046978d8  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #04 pc 00000000039394f0  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #05 pc 0000000003946358  /data/app/.../lib/arm64/libil2cpp.so  ← 地址开始重复
E CRASH:   #06 pc 00000000046978d8  /data/app/.../lib/arm64/libil2cpp.so
E CRASH:   #07 pc 00000000039394f0  /data/app/.../lib/arm64/libil2cpp.so
...
```

几个关键信息：

- `signal 11 (SIGSEGV)`，`Cause: null pointer dereference`——native crash，不是托管异常
- 所有帧都在 `libil2cpp.so`，没有 `libunity.so` 或业务 `.so`
- 地址从 `#02` 开始出现**重复**：`0x03946358 → 0x046978d8 → 0x039394f0 → 0x03946358 → ...`，这是栈溢出的特征

没有符号，看不出来这几个地址对应什么函数。

### 第二步：提取 symbols，符号化地址

Unity 打包时会产出一个 `*-IL2CPP.symbols.zip`，里面包含带 debug 符号的 `libil2cpp.so`。解压出 `arm64-v8a/libil2cpp.so`，用 Unity 内置 NDK 的 `llvm-addr2line` 做符号化。

> **注意**：对于 ARM64 Android 构建，必须使用 Android NDK 提供的 `aarch64-linux-android-addr2line`（或 Unity 内置 NDK 的 `llvm-addr2line`），不能用系统自带的 `addr2line`——系统版本通常是 x86 host 工具，无法正确解析 ARM64 ELF。需要符号化的二进制是构建产出 `unstripped` 目录下的 `libil2cpp.so`（即 symbols.zip 里的那份），不是安装到设备上被 strip 过的版本。

```bash
ADDR2LINE="<Unity_NDK>/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-addr2line.exe"
LIB="<symbols_dir>/arm64-v8a/libil2cpp.so"

llvm-addr2line -f -C -e "$LIB" 00000000046951fc
llvm-addr2line -f -C -e "$LIB" 0000000003939604
llvm-addr2line -f -C -e "$LIB" 0000000003946358
llvm-addr2line -f -C -e "$LIB" 00000000046978d8
llvm-addr2line -f -C -e "$LIB" 00000000039394f0
```

结果：

| 地址 | 函数名 |
|------|--------|
| `046951fc` | `AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted_TisIl2CppFullySharedGenericAny_TisIl2CppFullySharedGenericAny` |
| `03939604` | `hybridclr::interpreter::Interpreter::Execute` |
| `03946358` | `hybridclr::interpreter::InterpreterInvoke` |
| `046978d8` | `AsyncUniTaskMethodBuilder_Start_TisIl2CppFullySharedGenericAny` |
| `039394f0` | `hybridclr::interpreter::Interpreter::Execute` |

### 第三步：读懂调用链

把符号化结果按帧序重新排，从外层往内读：

```
ModuleSystem_Update
  → FsmModule_Update
    → RuntimeMethodInfo::InternalInvoke          ← 反射调用 GameApp.Entrance
      → il2cpp::vm::Runtime::Invoke
        → hybridclr::interpreter::Interpreter::Execute
          → hybridclr::interpreter::InterpreterInvoke
            → AsyncUniTaskMethodBuilder_Start_TisIl2CppFullySharedGenericAny   ← 第 1 次
              → hybridclr::interpreter::Interpreter::Execute
                → hybridclr::interpreter::InterpreterInvoke
                  → AsyncUniTaskMethodBuilder_Start_TisIl2CppFullySharedGenericAny  ← 第 2 次
                    → hybridclr::interpreter::Interpreter::Execute
                      → ...（重复到栈溢出）
                        → AsyncUniTaskMethodBuilder_AwaitUnsafeOnCompleted   ← CRASH
```

`AsyncUniTaskMethodBuilder_Start` 和 `HybridCLR 解释器` 在互相调用，没有出口。

### 第四步：定位根因

函数名里的 `TisIl2CppFullySharedGenericAny` 是关键线索。

IL2CPP 的泛型有两条路：
- **具体实例化路径**：AOT 阶段为某个具体类型（如 `AsyncUniTaskMethodBuilder<int>`）生成了具体 native 代码，直接调用
- **全共享泛型路径（Fully Shared Generic）**：没有具体实例，IL2CPP 用一个通用的 `IlCppFullySharedGenericAny` 路径走，通过反射/解释执行

热更代码里的 `async UniTask` 方法会产生一个状态机类型（`<SomeMethod>d__N`），这个状态机类型在热更 DLL 里，不在 AOT 里。当 `AsyncUniTaskMethodBuilder.Start<TStateMachine>(ref TStateMachine stateMachine)` 被调用时，`TStateMachine` 是热更类型，AOT 世界里没有它的具体实例，IL2CPP 就走全共享泛型路径，`TisIl2CppFullySharedGenericAny` 由此而来。

走进全共享泛型路径后，调用会进入 HybridCLR 解释器。而解释器里执行的代码又再次调用 `AsyncUniTaskMethodBuilder.Start`（因为 `async` 方法的 `MoveNext` 内部再次触发了异步基础设施），再次走全共享泛型路径，再次进入解释器——**死循环，直到栈溢出**。

查项目里的 `AOTGenericReferences.cs`，发现 `RefMethods()` 完全为空：

```csharp
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{
    public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
    {
        "UniTask.dll",
        // ...
    };

    // 文件里有大量注释，列出了需要实例化的泛型类型：
    // Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>
    // Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<int>
    // ...

    public void RefMethods()
    {
        // 空的——所有注释列出的泛型类型都没有被真正实例化
    }
}
```

这个文件是 HybridCLR 用 `Generate > AOTGenericReferences` 生成的**清单**，注释里列出了热更代码里用到的、需要 AOT 实例化的泛型类型。但 `RefMethods()` 里没有任何代码——IL2CPP 不会从注释里生成代码，所以这些泛型实例实际上从未被 AOT 编译出来过。

### 结论

这次 native crash 的完整因果链：

1. `AOTGenericReferences.cs` 的 `RefMethods()` 是空的
2. `AsyncUniTaskMethodBuilder<T>.Start<TStateMachine>` 没有热更状态机类型的具体 AOT 实例
3. 热更代码里 `async UniTask` 方法触发时，走了 `IlCppFullySharedGenericAny` 路径
4. 该路径调用 HybridCLR 解释器执行
5. 解释器执行的热更代码又触发了 `AsyncUniTaskMethodBuilder.Start`，再次走同一路径
6. 死循环 → 栈溢出 → `SIGSEGV`

---

## 两个问题的层次对比

| | 问题 1 | 问题 2 |
|-|--------|--------|
| 错误层 | 托管层（CLR 类型解析） | Native 层（IL2CPP + HybridCLR 解释器） |
| 信号 | `TypeLoadException`（有日志） | `SIGSEGV signal 11`（静默崩溃） |
| 诊断工具 | `adb logcat -s Unity` | `adb logcat` CRASH tag + `llvm-addr2line` |
| 根因 | DLL 加载顺序错，依赖早于依赖项进 runtime | `AOTGenericReferences.RefMethods()` 为空，async 泛型无 AOT 实例 |
| 修法方向 | 按依赖图排序 `HotUpdateAssemblies` | 补全 `RefMethods()` 里的 AOT 泛型实例化代码 |

## 三个值得单独记住的判断

**1. TypeLoadException 的第一反应不是"类不存在"，而是"类的 DLL 还没进 runtime"**

如果报错的类确实在热更 DLL 里，先查它的 DLL 在 `HotUpdateAssemblies` 里排第几，再查报错是从哪个 DLL 的加载里触发的。顺序对了，报错自然消失。

**2. SIGSEGV in libil2cpp.so，帧地址重复 = 栈溢出 = 死循环，不是真的空指针**

`signal 11 (SIGSEGV), Cause: null pointer dereference` 这个描述是栈溢出的常见表现形式——栈指针超出了 guard page，访问到了未映射内存，内核报 null pointer dereference。看到帧地址重复就应该先往递归/死循环方向想，而不是找哪个对象是 null。

Android 上原生线程的默认栈大小通常是 1 MB（可通过 `pthread_attr_setstacksize` 配置）。HybridCLR 解释器的每一层 `Interpreter::Execute` 调用都消耗原生栈帧，因此深度解释器递归（包括本案例中 FullySharedGeneric 引发的无限递归）会很快撞到这个上限。

**3. `AOTGenericReferences.cs` 里有注释没有代码 = 什么都没做**

`Generate > AOTGenericReferences` 生成的注释列表是分析结果，不是代码。`RefMethods()` 里必须有实际的引用代码，IL2CPP 才会把那些泛型实例编译进 AOT。空的 `RefMethods()` 和没有这个文件效果相同。

## 符号化工具快速参考

Unity 打包产出的 `*-IL2CPP.symbols.zip` 包含带符号的 `libil2cpp.so`，路径在 `arm64-v8a/libil2cpp.so`。

Unity 内置 NDK 里有 `llvm-addr2line`，路径一般是：

```
<Unity安装目录>/Editor/Data/PlaybackEngines/AndroidPlayer/NDK/
  toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-addr2line.exe
```

基本用法：

```bash
llvm-addr2line.exe -f -C -e libil2cpp.so <崩溃地址>
# -f：输出函数名
# -C：C++ 符号 demangle
# -e：指定带符号的 .so 文件
```

如果有多个地址要批量查：

```bash
for addr in 046951fc 03939604 03946358 046978d8 039394f0; do
  echo -n "$addr: "
  llvm-addr2line.exe -f -C -e libil2cpp.so $addr
done
```

## 系列位置

- 上一篇：[HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断]({{< relref "engine-toolchain/hybridclr-faq-10-most-confused-judgments.md" >}})
- 下一篇：待定
