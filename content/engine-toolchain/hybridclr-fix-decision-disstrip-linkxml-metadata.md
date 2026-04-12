---
title: "HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用"
date: "2026-03-31"
series:
  - "HybridCLR"
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "DisStripCode"
  - "link.xml"
  - "LoadMetadataForAOTAssembly"
  - "MissingMethodException"
weight: 48
---

这是 HybridCLR 系列第 19 篇。

前两篇案例（HCLR-17、HCLR-18）记录了 async 崩溃和 `Dictionary<ValueTuple, 热更类型>` 各自的根因和修法。这一篇不再拆案例，而是把这两次排查过程里反复要做的判断收成一张决策图：

**看到同一条 `MissingMethodException`，我该改 `DisStripCode`，还是补 metadata，还是加 `link.xml`？**

三者经常同时出现在讨论里，但修的不是同一件事。混用不会报编译错误，只会让问题若隐若现或带来新的性能坑。

---

## 一、同一个报错，三种可能根因

触发本篇写作的现场报错：

```
MissingMethodException: AOT generic method not instantiated in aot.
assembly:mscorlib.dll
method: System.Boolean System.Collections.Generic.Dictionary`2
  [System.ValueTuple`2[System.Int32,System.String],
   System.Collections.Generic.List`1[Game.Data.T_AreaUnlockTips]]
  ::TryGetValue(...)
```

看到这条报错，直觉反应通常是三条路之一：去 `DisStripCode.cs` 加一行，或者去补 metadata，或者去 `link.xml` 加保留规则。

这三条路解决的不是同一个问题。要判断走哪条，先要分清报错的根因层：

**根因 A：native 代码从未生成过**
IL2CPP 构建时没见过某个泛型参数组合，没为它生成 native 实现。这个组合在 `GameAssembly.so` 里根本不存在。

**根因 B：native 代码曾经存在但被裁掉了**
IL2CPP Managed Stripper 在树摇阶段认为某个类型/方法没有引用，把它从 AOT 产物里删除了。代码写了，但最终没进包。

**根因 C：解释器缺乏 AOT 类型的元信息**
HybridCLR 解释器在执行热更代码时需要查询 AOT 程序集的类型布局，但运行时拿不到对应的 IL 元数据，无法继续。

三条根因对应三种修法，一一对应，不互相替代。

---

## 二、三种机制各自在修什么

**DisStripCode（AOT 泛型实例化保留）**

在 AOT 程序集里写出对某个泛型类型组合的显式引用，迫使 IL2CPP 在构建时为该组合生成 native 实现。

修的是根因 A：native 代码不存在。

它不负责防止代码被裁剪，也不给 HybridCLR 解释器提供任何信息。

**link.xml / [Preserve]**

指示 IL2CPP Managed Stripper 在树摇阶段保留指定类型或成员，使其进入最终 AOT 产物。

修的是根因 B：native 代码被删掉了。

它不生成新的 native 代码，只阻止现有代码被删。如果某个泛型组合从来没有被写进 AOT 代码，`link.xml` 对它无效——保留的前提是"有东西可以保留"。

**LoadMetadataForAOTAssembly（补充元数据）**

运行时向 HybridCLR 注册一份 AOT 程序集的裁剪后 IL 字节，使解释器在执行热更代码时能查询该程序集的类型布局和泛型定义。

修的是根因 C：解释器看不懂。

它不让 AOT native 代码凭空出现。补了 metadata 之后，如果 AOT native 实例仍不存在，解释器走的是 fallback 解释路径——报错可能消失，但执行路径已经从 native 切换成了解释器。

---

## 三、报错信号与修法的对应关系

| 报错信号 | 根因 | 修法 | 能否热更（不发 APK）|
|---|---|---|---|
| `MissingMethodException: AOT generic method not instantiated in aot` | AOT 泛型 native 实例缺失 | LoadMetadataForAOTAssembly（先恢复运行） / DisStripCode（要 native 性能时） | metadata ✅ / native 方案 ❌ |
| `TypeLoadException` / `MissingMemberException` / `MissingFieldException` | 类型或成员被 Stripper 裁掉 | link.xml / `[Preserve]` | ❌ 必须重打 APK |
| `ExecutionEngineException: metadata not found` / 解释器找不到 AOT 类型布局 | HybridCLR 解释器缺 AOT 元信息 | LoadMetadataForAOTAssembly | ✅ 可通过热更 MPQ 下发 |
| `AOT generic method not instantiated`（已上线，无法立即发 APK）| 同上，但要先恢复运行 | LoadMetadataForAOTAssembly（先恢复运行）→ 性能敏感或要回 native 时再补 DisStripCode | 先恢复运行 ✅ / native 方案 ❌ |

**DisStripCode 的具体写法规则：**

泛型参数里出现值类型时，不能简单拿 `object` 覆盖整个实例，至少要把值类型部分按实际共享类型写出来；泛型参数里的热更引用类型，在 AOT 代码里看不见，用 `object` 替代。

本篇触发场景的修法：

```csharp
[Preserve]
static void ForceDreamSpiritAOTInstantiation()
{
    // (int, int) 键的组合——原有
    var d = new Dictionary<(int, int), object>();
    d[(0, 0)] = default;

    // (int, string) 键的新变体——本次新增
    // string 是引用类型，用 object 替代；List<热更类型> 同理
    var d2 = new Dictionary<(int, string), List<object>>();
    d2.TryGetValue((0, ""), out _);
}
```

为什么 `object` 能覆盖 `List<T_AreaUnlockTips>`：IL2CPP Full Generic Sharing 对引用类型参数共享一套 native 实现，`object` 实例化时生成的 native 代码在运行时被所有引用类型值复用。细节见 HCLR-18。

---

## 四、容易混淆的两个边界

### 边界一：`MissingMethodException` 有两种根因，报错名字相同

`MissingMethodException` 本身不区分"从未生成"和"生成后被裁掉"。鉴别方式：

- **查 AOTGenericReferences.cs**：如果该泛型组合在自动生成的清单里有记录，说明热更侧已经依赖到了它；接下来再继续判断 AOT 侧到底是没实例化，还是实例化后又被裁剪。这个文件更像需求清单，不是"native 代码已经生成过"的直接证据。
- **查报错是否带 "AOT generic method not instantiated in aot"**：这条固定措辞来自 IL2CPP 的 `RaiseAOTGenericMethodNotInstantiatedException`，至少能说明当前缺的是 AOT 泛型 native 实例。后续是先补 metadata 恢复运行，还是补 DisStripCode 回到 native，要看你是在抢修功能，还是在修性能路径。
- **查 Managed Stripping Level**：如果项目最近刚从 Low 调到 Medium 或 High，出现的新报错优先怀疑裁剪，走 link.xml 方向。

### 边界二：补 metadata 后报错消失，不代表问题解决了

这是最常见的误判。

补了 `LoadMetadataForAOTAssembly` 之后，`AOT generic method not instantiated` 的报错有时会消失。原因不是 native 代码出现了，而是 HybridCLR 解释器拿到了 AOT 类型的 IL 元信息后，切换到了 interpreter-only 路径兜底执行。

两条路径的区别：
- native 路径：IL2CPP 为该泛型组合生成了 C++ native 实现，直接 native 调用
- interpreter fallback 路径：HybridCLR 解释器用 IL 字节码逐条解释执行，有性能代价

如果是热路径（每帧调用、大量数据处理），fallback 路径的性能代价会在帧率上直接显现。报错消失但帧率下降，是这个误判最典型的后果。

正确做法：如果当前目标只是先恢复运行，补 metadata 已经是有效修法；如果这条路径是热路径，或者你希望回到 native 执行，再在下一个 APK 版本里把对应泛型实例补进 DisStripCode，让 native 代码真正进包。

---

## 五、修完还要做什么

三种修法的交付动作不同，不能混用：

**DisStripCode 改动 → 必须重打 APK**

修改进入 AOT 程序集，最终编译进 `GameAssembly.so` / `libil2cpp.so`。只更新热更包（MPQ）无效，设备上的 native 代码不会变。

**link.xml / [Preserve] 改动 → 必须重打 APK**

Managed Stripper 在构建阶段运行，结果影响最终 AOT 产物。同样无法通过热更修复。

**LoadMetadataForAOTAssembly 改动 → 可以热更**

常见做法是把 AOT DLL 文件（通常是 `AssembliesPostIl2CppStrip` 目录下的裁剪后产物）打进热更包（MPQ）下发。运行时调用 `LoadMetadataForAOTAssembly` 加载即可生效。

两个使用约束：
1. 如果使用 `HomologousImageMode.Consistent`，必须使用 `AssembliesPostIl2CppStrip` 里的 DLL；如果使用 `SuperSet`，裁剪后 DLL 和原始 AOT DLL 都可以，但最好和本次打包产物同源
2. 补充元数据没有硬性的加载顺序要求；只要在相关 AOT 泛型第一次被使用前完成加载即可。若项目里把它固化成"热更 DLL 之前统一加载"，那是工程约定，不是机制强制要求

**AOTGenericReferences.cs 需要定期重新生成**

DisStripCode 里手动加的泛型引用和 `AOTGenericReferences.cs` 是两件事：前者是让 IL2CPP 生成 native 代码的可执行代码，后者是 HybridCLR 工具链扫描热更 DLL 后输出的"当前需要哪些 AOT 实例"的注释清单。

手动加了 DisStripCode 并不代表 AOTGenericReferences.cs 已经覆盖了所有缺口。每次热更 DLL 有变更，都应该重新运行 `HybridCLR > Generate > AOT Generic Reference`，用新清单检查是否有遗漏，再补进 DisStripCode。

---

## 六、把决策压成一句话

> 看到 `AOT generic method not instantiated`，先问"现在要解决的是能不能跑，还是要不要回到 native 路径"——先恢复运行可以补 metadata；如果这条路径性能敏感，或者你就是要回到 native，再补 DisStripCode。写 DisStripCode 时，值类型部分要按实际共享类型写，热更引用类型通常用 `object` 替代。`link.xml` 不解决这类"实例没进 AOT"问题，它解决的是"类型存在但被裁掉了"。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}">HybridCLR 案例｜Dictionary\<ValueTuple, 热更类型\> 的 MissingMethodException 与 object 替代法</a>
