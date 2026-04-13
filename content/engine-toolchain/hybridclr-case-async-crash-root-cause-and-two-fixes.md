---
title: "HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法"
date: "2026-03-26"
series:
  - "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
tags:
  - "HybridCLR"
  - "async"
  - "UniTask"
  - "IL2CPP"
  - "crash"
  - "AOT"
weight: 46
---
上一篇案例（HCLR-14）定位到崩溃堆栈是 `AsyncUniTaskMethodBuilder.Start<IlCppFullySharedGenericAny>` 和 `Interpreter::Execute` 的无限递归，并指出 `AOTGenericReferences.RefMethods()` 是空的。但"RefMethods 是空的"只是症状，不是根因。

这篇补完整个因果链，并给出两种修法。

---

## 一、完整的因果链

```
GenerateAll 未进 CI 流程
    ↓
StripAOTDlls 用的 Development 标志来自面板，不可控
    ↓
StripAOTDlls 内部构建与最终 APK 构建的 Development 不一致
    ↓
生成的 AOT 裁剪后 DLL 与最终包不匹配
    ↓
LoadMetadataForAOTAssembly 加载进来的补充元数据失效
    ↓
解释器找不到 AsyncUniTaskMethodBuilder.Start<TStateMachine> 的方法体
    ↓
退化到 IlCppFullySharedGenericAny 路径
    ↓
FullySharedGeneric Start → 解释器执行 → 内部再调 Start → 死循环 → SIGSEGV
```

每一步都是确定性的——只要 Development 标志不一致，这条链就会走完。

![async 崩溃因果链](../../images/hybridclr/async-crash-causal-chain.svg)

*图：从 GenerateAll 未进 CI 到 SIGSEGV，每一步都是确定性的。*

---

## 二、为什么 RefMethods 是空的不是根因

`AOTGenericReferences.cs` 的 `RefMethods()` 方法里全是注释：

```csharp
public void RefMethods()
{
    // System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<object>(object&)
    // System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<int>.Start<object>(object&)
    // ...（几十行注释，没有一行真实代码）
}
```

这个文件是 `Generate/AOTGenericReference` 自动生成的，注释列出了"需要 AOT 实例化的泛型方法清单"，**但工具不会自动写出真实代码**。`RefMethods()` 本身也没有任何地方调用它，IL2CPP 不会因为它是空的就发出任何警告。

如果补充元数据是正确的，解释器能通过 `LoadMetadataForAOTAssembly` 载入的元数据找到这些方法的 IL 方法体并解释执行——`RefMethods` 空不空对这条路径没有影响。

**真正触发死循环的条件是：补充元数据失效，解释器找不到方法体，只能退化到 FullySharedGenericAny。**

---

## 三、补充元数据为什么会失效

`LoadMetadataForAOTAssembly` 需要的 DLL 是 IL2CPP **裁剪后**的 AOT DLL（位于 `HybridCLRData/AssembliesPostIl2CppStrip/{target}/`），由 `StripAOTDlls` 步骤生成。

`StripAOTDlls` 内部跑一次 `BuildScriptsOnly`，这次构建的 IL2CPP 裁剪结果取决于构建选项——尤其是 `BuildOptions.Development`。

- **Development 模式**：关闭部分优化，保留更多调试信息，IL2CPP 裁剪行为不同
- **Release 模式**：开启优化，IL2CPP 裁剪更激进

两种模式下生成的裁剪 DLL，方法签名相同，但 token 映射、泛型实例化集合可能不同。用 Development 模式生成的 DLL 去给 Release 包补充元数据，`HomologousImageMode.SuperSet` 模式下多数情况能工作，但不保证——关键泛型路径失配时，解释器就找不到方法体。

---

## 四、修法一：流程修法（治本）

让 `GenerateAll` 进入 CI 打包流程，并在调用前强制写入与最终构建一致的 Development 标志：

```csharp
// 打包流程里，BuildAndroidAPK 之前
EditorUserBuildSettings.development = req.Development;
EditorUserBuildSettings.allowDebugging = req.Development;

CompileDllCommand.CompileDll(target, req.Development);
Il2CppDefGeneratorCommand.GenerateIl2CppDef();
LinkGeneratorCommand.GenerateLinkXml(target);
StripAOTDllCommand.GenerateStripedAOTDlls(target);  // 内部构建此时与最终包同模式
MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
```

这样 StripAOTDlls 生成的裁剪 DLL 与最终 APK 是同模式构建的产物，补充元数据不会失配，解释器能正常找到方法体，FullySharedGenericAny 死循环根本不会触发。

这是**根治**，推荐优先选这条路。流程修法之所以优先于代码修法（修法二），是因为它解决的是根因——Development 标志不一致导致补充元数据失效。而代码修法（在 AOT 程序集里添加 `AOTGenericReferences`）只是一个 workaround，它只能覆盖你已经遇到并手动添加的那些具体泛型实例，无法防御未来新增的泛型用法。详细实现见 HCLR-16。

---

## 五、修法二：主动引用法（治标，适用于无法改构建流程的情况）

如果改不了 CI 流程（比如使用第三方 CI 平台或不方便改构建脚本），可以在 AOT 程序集里写一个"占位实例化"：

**原理**：在 AOT 程序集里定义一个实现了 `IAsyncStateMachine` 的 dummy struct，然后用它去实例化 `AsyncUniTaskMethodBuilder.Start<DummyStateMachine>`。IL2CPP 在编译时看到这个调用，会为 `Start<DummyStateMachine>` 生成真实的 AOT 代码。运行时，FullySharedGenericAny 共享路径优先寻找已编译的 AOT 实现，找到了就直接执行，不再走解释器，死循环消失。

```csharp
// 放在 AOT 程序集（非热更）里，比如 DisStripCode.cs 或独立文件
using Cysharp.Threading.Tasks.CompilerServices;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

/// <summary>
/// 强制 IL2CPP 为 UniTask async builder 编译 AOT 泛型实例化。
/// 此方法永远不会被调用，仅用于让 IL2CPP 看见这些泛型调用。
/// </summary>
[Preserve]
static class UniTaskAOTHelper
{
    struct DummyStateMachine : IAsyncStateMachine
    {
        public void MoveNext() { }
        public void SetStateMachine(IAsyncStateMachine s) { }
    }

    [Preserve]
    static void ForceAOTInstantiation()
    {
        var sm = default(DummyStateMachine);

        var b0 = new AsyncUniTaskMethodBuilder();
        b0.Start(ref sm);

        var b1 = new AsyncUniTaskMethodBuilder<int>();
        b1.Start(ref sm);

        var b2 = new AsyncUniTaskMethodBuilder<byte>();
        b2.Start(ref sm);

        var b3 = new AsyncUniTaskMethodBuilder<object>();
        b3.Start(ref sm);

        var bv = new AsyncUniTaskVoidMethodBuilder();
        bv.Start(ref sm);
    }
}
```

**注意事项**：
- `DummyStateMachine` 是 struct，`Start<TStateMachine>` 对 struct 参数不共享——这次实例化只覆盖 `DummyStateMachine` 这个具体类型。但 FullySharedGenericAny 共享机制在引用类型（class）和特殊路径上会复用，实际效果视 IL2CPP 版本和项目泛型用法而定。**不保证 100% 覆盖所有状态机类型**，建议配合 `HomologousImageMode.SuperSet` 的补充元数据一起使用。
- 这个方法用 `[Preserve]` 防止被裁剪，但本身不需要被调用。
- 如果热更里有返回 `UniTask<CustomStruct>` 的 async 方法，还需要额外加对应的 builder 实例化。

**修法二适合作为紧急临时方案**，根本上还是建议走修法一。

---

## 六、两种修法的对比

| | 修法一：流程修法 | 修法二：主动引用法 |
|---|---|---|
| 根治程度 | 根治 | 治标，覆盖范围有限 |
| 改动位置 | CI 构建脚本 | AOT 程序集源码 |
| 维护成本 | 低（一次性改造） | 中（新增泛型用法时需补充） |
| 适用场景 | 能控制构建流程 | 无法改构建流程的紧急情况 |
| 依赖补充元数据 | 是（但会正确匹配） | 是（叠加使用更稳） |

---

## 收束

async 崩溃的根因不是 `RefMethods()` 是空的，而是 GenerateAll 没进流程导致补充元数据失效。解释器找不到方法体才退化到 FullySharedGenericAny 死循环。修法是让 GenerateAll 进流程并对齐 Development 标志；主动引用法只是应急。
