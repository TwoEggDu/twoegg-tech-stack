+++
title = "HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次"
description = "HybridCLR 系列第 15 篇。专注于 HybridCLR 项目的 native crash 分析：hybridclr:: 函数在调用栈里意味着什么，AOT 泛型缺失的崩溃特征，MethodBridge 缺失，metadata 不匹配，以及如何系统性地填写 AOTGenericReferences.cs 来预防这类崩溃。"
weight = 44
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "Troubleshooting", "NativeCrash", "Symbols", "AOTGeneric", "MethodBridge"]
series = "HybridCLR"
+++

> HybridCLR 的 native crash 和普通 IL2CPP 项目的 native crash 看起来很像，但调用栈里多了几类 HybridCLR 特有的函数——学会识别这些函数，就能快速判断这次崩溃属于哪一层。

这是 HybridCLR 系列第 15 篇。

前置阅读：
- 崩溃分析基础在 [崩溃分析系列第 0 篇]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}})（信号/调用栈/符号化概念）
- Android 符号化操作在 [崩溃分析系列第 1 篇]({{< relref "engine-notes/crash-analysis-01-android.md" >}})
- 真实案例在 [HybridCLR 案例诊断篇]({{< relref "engine-notes/hybridclr-case-typeload-and-async-native-crash.md" >}})

---

## HybridCLR 给调用栈带来的变化

没有 HybridCLR 的 IL2CPP 项目，调用栈里全是 IL2CPP 生成的 C++ 函数，每帧都直接对应一个 C# 方法的编译结果：

```
# 纯 AOT 调用栈（无 HybridCLR）
#00  SomeGameClass_SomeMethod (SomeGameClass.cpp:123)
#01  AnotherClass_Update (AnotherClass.cpp:456)
#02  UnityEngine_MonoBehaviour_Update (...)
```

加入 HybridCLR 之后，热更代码不再被 AOT 编译——它在解释器里运行。调用栈里因此会出现 HybridCLR 的解释器帧：

```
# 含 HybridCLR 解释器的调用栈
#00  hybridclr::interpreter::Interpreter::Execute (Interpreter.cpp:2134)
#01  hybridclr::interpreter::InterpreterInvoke (Interpreter.cpp:2981)
#02  il2cpp::vm::Runtime::Invoke (...)
#03  SomeAOTClass_Update (SomeAOTClass.cpp:456)
```

`Interpreter::Execute` 和 `InterpreterInvoke` 是"热更代码正在被解释执行"的标志——你看不到具体的热更函数名，只能看到解释器。

---

## HybridCLR 项目调用栈的三层结构

符号化后的调用栈从上到下（`#N` 到 `#0`）通常呈现这样的层次：

```
（业务调用层）
AOT 代码 → il2cpp::vm::Runtime::Invoke
                ↓
（HybridCLR 解释器层）
hybridclr::interpreter::Interpreter::Execute
hybridclr::interpreter::InterpreterInvoke
                ↓
（热更代码或 AOT/热更 交界层）
AsyncXxxMethodBuilder_Start_TisIl2CppFullySharedGenericAny  ← AOT 泛型
hybridclr::interpreter::Interpreter::Execute
...
```

识别这三层，是判断崩溃属于哪一层问题的第一步。

---

## 常见崩溃模式

### 模式 1：AOT 泛型缺失 → 死循环 → 栈溢出

**特征**：

- `signal 11 (SIGSEGV), Cause: null pointer dereference`（这是栈溢出的常见表现）
- 帧地址重复
- 符号化后看到 `TisIl2CppFullySharedGenericAny` 函数名
- 在 `Interpreter::Execute` 和某个 AOT 泛型函数之间循环

**典型调用栈**：

```
#00  AsyncXxxMethodBuilder_AwaitUnsafeOnCompleted_TisIl2CppFullySharedGenericAny_TisIl2CppFullySharedGenericAny
#01  hybridclr::interpreter::Interpreter::Execute
#02  hybridclr::interpreter::InterpreterInvoke
#03  AsyncXxxMethodBuilder_Start_TisIl2CppFullySharedGenericAny
#04  hybridclr::interpreter::Interpreter::Execute
#05  hybridclr::interpreter::InterpreterInvoke
#06  AsyncXxxMethodBuilder_Start_TisIl2CppFullySharedGenericAny    ← 重复
...
```

**根因**：热更代码里用了某个泛型方法（常见于 `async`/`UniTask`/`Task`），该泛型的具体实例没有被 AOT 编译出来。IL2CPP 走全共享泛型（`FullySharedGeneric`）路径，该路径调用解释器，解释器执行的热更代码又触发同一个泛型方法，形成死循环。

**定位方法**：

1. 看 `TisIl2CppFullySharedGenericAny` 前面的函数名，找出是哪个泛型类型/方法
2. 在 `AOTGenericReferences.cs` 里搜索该类型的注释
3. 检查 `RefMethods()` 里是否有对应的实例化代码

---

### 模式 2：MethodBridge 缺失 → SIGABRT 或 SIGILL

**特征**：

- `signal 6 (SIGABRT)` 或 `signal 4 (SIGILL)`
- 调用栈里出现 `NOT_SUPPORT_BRIDGE` 或 `MethodBridge_NotSupport`
- 通常在第一次调用某个 delegate 或接口方法时崩溃

**典型调用栈（符号化前）**：

```
#00 pc ...  libil2cpp.so
#01 pc ...  libil2cpp.so
```

**符号化后**：

```
#00  hybridclr::interpreter::MethodBridge_NotSupport (...)
#01  hybridclr::interpreter::Interpreter::Execute (...)
```

或者直接在 logcat 里看到：

```
E Unity: NotSupportedException: method call bridge missing: ...MethodSignature...
```

**根因**：热更代码里有某个方法签名，HybridCLR 没有为它生成 MethodBridge（调用约定适配层）。常见于：
- 有参数或返回值的 `delegate` 调用
- 某些泛型委托签名
- 通过反射调用带特定参数类型的方法

**定位方法**：

1. 看 logcat 里的 `NotSupportedException` 日志（这个错误通常有详细的方法签名）
2. 在 Unity 里执行 `HybridCLR → Generate → All`，重新生成 `MethodBridgeGenerics` 等文件
3. 重新打包

---

### 模式 3：Assembly 加载顺序错 → TypeLoadException → 提前退出

**特征**：

- 不是 native crash，是托管异常
- logcat 里有 `E Unity: Exception: System.TypeLoadException`
- 进程可能继续运行但逻辑已失败，也可能因为异常没被捕获而退出

**典型日志**：

```
E Unity: Exception: System.TypeLoadException: Could not load type 'Namespace.ClassName' from assembly 'SomeDll'.
  at LoadAssembly ()
```

**根因**：`UpdateSetting.asset` 的 `HotUpdateAssemblies` 列表中，某个 DLL 在它依赖的 DLL 之前被加载了。CLR 解析类型引用时，被引用的 DLL 还没进 runtime，只有 AOT 版本（经过 strip，类型可能不存在）。

**定位方法**：

1. 看报错里的类名属于哪个 DLL（`from assembly 'X'`）
2. 看报错是从哪个 DLL 的加载过程里触发的（stack trace 里的 `LoadAssembly`）
3. 检查 `HotUpdateAssemblies` 里这两个 DLL 的顺序
4. 分析依赖图，按从叶到根的顺序排列

---

### 模式 4：global-metadata.dat 版本不匹配 → 启动时崩溃

**特征**：

- app 安装后一启动就崩溃，连热更 DLL 加载都没开始
- logcat 里可能看到：`CRASH: signal 6 (SIGABRT)` 或 IL2CPP metadata 相关日志
- Unity log 里出现：`Failed to initialize Unity Metadata` 或 IL2CPP 初始化错误

**根因**：`global-metadata.dat` 是随包体编译的，和 `libil2cpp.so` 的版本强绑定。如果热更流程错误地替换了 `global-metadata.dat`（例如把下一个版本的 metadata 推给了旧版本的包），IL2CPP 在启动时校验失败，直接 abort。

**预防**：`global-metadata.dat` 属于 AOT 层产物，不应该通过热更下发。只有 DLL 可以热更，metadata 不行。

---

### 模式 5：解释器内部崩溃（HybridCLR bug）

**特征**：

- 崩溃点在 `hybridclr::interpreter::Interpreter::Execute` 的深层
- 没有明显的死循环（帧地址不重复）
- 崩溃和特定的热更代码逻辑相关（例如特定的语言特性：yield return、unsafe、fixed buffer 等）

**定位方法**：

1. 用二分法缩小触发崩溃的热更代码范围
2. 查 HybridCLR GitHub Issues 是否有已知 bug
3. 尝试升级 HybridCLR 版本
4. 把触发崩溃的代码移到 AOT 层（作为临时规避）

---

## AOTGenericReferences.cs 的正确使用流程

这是 AOT 泛型缺失类崩溃的核心预防点。

### 完整流程

```
1. 写热更代码
2. HybridCLR → Generate → AOTGenericReferences
   → 生成 Assets/HybridCLRData/Generated/AOTGenericReferences.cs
   → 文件里的注释列出了分析到的"需要 AOT 实例化的泛型"
3. 把注释里的类型引用实现为真实代码（RefMethods 里）
4. 打 AOT 包
5. 运行时才有对应的具体泛型实例
```

### 错误做法 vs 正确做法

**错误**（注释列出了，但 RefMethods 是空的）：

```csharp
public void RefMethods()
{
    // 什么都没写——注释不生成代码
}
```

**正确**（注释里列的类型都要有对应的引用代码）：

```csharp
public void RefMethods()
{
    // AsyncUniTaskMethodBuilder<object>
    Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object> _unused1 = default;
    _ = _unused1;

    // AsyncUniTaskMethodBuilder<int>
    Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<int> _unused2 = default;
    _ = _unused2;

    // IUniTaskSource<object>
    Cysharp.Threading.Tasks.IUniTaskSource<object> _unused3 = default;
    _ = _unused3;

    // 对于方法级泛型：
    // System.Linq.Enumerable.ToList<SomeType>
    System.Linq.Enumerable.ToList<SomeType>(null);
}
```

### 常见遗漏的泛型来源

| 来源 | 典型类型 |
|------|---------|
| `async UniTask` | `AsyncUniTaskMethodBuilder<T>`, `IUniTaskSource<T>` |
| `async Task` | `AsyncTaskMethodBuilder<T>`, `Task<T>` |
| LINQ | `IEnumerable<T>`, `List<T>`, `Dictionary<K,V>` |
| 事件系统 | 各种 `Action<T>`, `Func<T,R>` delegate 类型 |
| 自定义泛型容器 | 项目里自己写的泛型类 |

### 什么时候需要重新 Generate

每次满足以下条件之一，都需要重新执行 `Generate → AOTGenericReferences`：

- 新增了热更代码里使用的泛型类型
- 修改了泛型方法的参数类型
- 升级了热更依赖的第三方库（新版本可能用了不同的泛型）

---

## 快速判断崩溃属于哪一层

```
看 logcat 有没有 E Unity 级别的日志
  ├── 有 TypeLoadException → Assembly 加载顺序问题
  ├── 有 NotSupportedException → MethodBridge 缺失
  └── 没有托管层日志，有 CRASH tag → native crash

看 native crash 的 backtrace
  ├── 帧地址重复 + TisIl2CppFullySharedGenericAny → AOT 泛型缺失 → 死循环
  ├── MethodBridge_NotSupport → MethodBridge 缺失
  ├── IL2CPP metadata 初始化相关 → global-metadata.dat 版本不对
  └── 解释器深层帧，无重复 → 可能是 HybridCLR bug 或热更代码逻辑问题
```

---

## 预防性检查清单

每次打包前：

```
□ AOTGenericReferences.cs 的 RefMethods() 里有实际代码（不是空函数或只有注释）
□ HotUpdateAssemblies 的顺序按依赖图从叶到根排列
□ Generate → All 已执行（MethodBridge、AOTGenericReferences、LinkXml 全部更新）
□ global-metadata.dat 不在热更下发列表里
□ 热更 DLL 和 AOT metadata DLL 来自同一次 HybridCLR 编译
```

---

## 系列位置

- 上一篇：[HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出]({{< relref "engine-notes/hybridclr-case-typeload-and-async-native-crash.md" >}})
- 通用崩溃分析基础：[崩溃分析系列入口]({{< relref "engine-notes/crash-analysis-00-what-is-a-crash.md" >}})
