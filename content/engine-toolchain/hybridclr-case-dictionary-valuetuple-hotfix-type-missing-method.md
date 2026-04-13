---
title: "HybridCLR 案例｜Dictionary<ValueTuple, 热更类型> 的 MissingMethodException 与 object 替代法"
date: "2026-03-27"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
tags:
  - "IL2CPP"
  - "AOT"
  - "Generics"
  - "Dictionary"
  - "MissingMethodException"
  - "DisStripCode"
weight: 47
---

这是 HybridCLR 系列第 18 篇。

上一篇案例续篇（HCLR-17）讲的是 async 崩溃的真正根因——GenerateAll 没进 CI 流程导致补充元数据失效。这一篇记录另一个独立的 AOT 泛型缺口：

`MissingMethodException: AOT generic method not instantiated: Dictionary<ValueTuple<int,int>, T_HotfixType>::set_Item`

它的成因、卡点和修法都和 async 那条链不同，单独拆开。

---

## 一、出错现象

进入某个游戏功能时，日志出现：

```
MissingMethodException: AOT generic method not instantiated in aot.
assembly:mscorlib.dll, method:System.Collections.Generic.Dictionary`2<System.ValueTuple`2<System.Int32,System.Int32>,GameLogic.T_HotfixType>::set_Item(...)
```

崩溃栈落在热更代码里某个用 `(int, int)` 做键的字典写入操作。

---

## 二、为什么会出这个错

### 2.1 问题的两层

这条错误完整形态是 `Dictionary<ValueTuple<int,int>, T_HotfixType>`。里面有两个泛型参数，各自带一个独立问题：

**第一层：ValueTuple 是值类型**

IL2CPP Full Generic Sharing 的共享逻辑对值类型和引用类型是分开的：

- 引用类型参数（class）：所有引用类型共享一套 AOT native 实现（用 `object` 实例化即可覆盖全部）
- 值类型参数（struct / enum）：每种值类型需要各自独立实例化

`ValueTuple<int,int>` 是 struct，`Dictionary<ValueTuple<int,int>, V>` 的 AOT 实例化不能靠引用类型的共享路径覆盖，必须在 AOT 代码里有一处真实引用 `Dictionary<(int,int), something>`，IL2CPP 才会为它生成对应的 native 实现。

**第二层：T_HotfixType 是热更类型**

如果 `T_HotfixType` 是定义在热更 DLL 里的类，它不存在于 AOT 程序集。IL2CPP 在构建 AOT 时根本看不到这个类型，自然不会为包含它的泛型组合生成实现。

这两层叠在一起，`Dictionary<(int,int), T_HotfixType>` 的 `set_Item`、`get_Item`、`TryGetValue` 等方法在 AOT 世界里完全不存在——跑起来就是 `MissingMethodException`。

---

## 三、第一反应为什么是错的

最直觉的修法是：在 `DisStripCode.cs` 里加上：

```csharp
// 直觉修法——但这是错的
var d = new Dictionary<(int, int), T_HotfixType>();
```

**这无法编译**。

`DisStripCode.cs` 在 AOT 程序集（Launcher）里，`T_HotfixType` 在热更 DLL（GameLogic）里。AOT 程序集不能引用热更程序集——热更 DLL 是运行时动态加载的，编译 AOT 时它根本还不存在。

这是 HybridCLR AOT/热更边界的基本约束，不是 `DisStripCode.cs` 自身的限制：

```
AOT 程序集  →  不能引用  →  热更程序集
热更程序集  →  可以引用  →  AOT 程序集（单向）
```

---

## 四、正确修法：用 object 替代热更类型

正确做法是在 `DisStripCode.cs` 里写：

```csharp
[Preserve]
static void ForceDreamSpiritAOTInstantiation()
{
    var d = new Dictionary<(int, int), object>();
    d[(0, 0)] = default;
}
```

用 `object` 代替热更类型 `T_HotfixType`。

---

## 五、为什么 object 能覆盖热更引用类型

这是这个修法成立的核心，需要理解 IL2CPP Full Generic Sharing 对引用类型的处理方式。

IL2CPP 对引用类型的泛型参数做了一个重要优化：**所有引用类型参数共享同一套 AOT 实现**。

背后的原因是引用类型在 IL2CPP 里的表示是统一的：所有引用类型实例在 native 层都是指针（`Il2CppObject*`），大小和 ABI 一致。因此 `Dictionary<K, string>`、`Dictionary<K, MyClass>`、`Dictionary<K, HotfixType>` 在值类型 `K` 确定的前提下，只需要生成一套 native 代码——用 `object` 实例化时生成的那套代码就能覆盖所有引用类型值。

更具体地说：IL2CPP 在 ABI 层面把所有引用类型都当作指针大小的值处理。AOT 泛型实例 `Dictionary<ValueTuple<int,int>, object>` 产生的 native 方法签名，与假设能编译 `Dictionary<ValueTuple<int,int>, HotfixType>` 时产生的签名完全一致，因此 MethodBridge 桩函数和 AOT 泛型实现是兼容的。

所以：

- AOT 里写 `new Dictionary<(int,int), object>()`
- IL2CPP 为 `Dictionary<ValueTuple<int,int>, object>` 生成完整 native 实现
- 运行时热更代码使用 `Dictionary<(int,int), T_HotfixType>` 时
- IL2CPP 的 Full Generic Sharing 识别到值参数相同（`ValueTuple<int,int>`）、值类型参数是引用类型
- 复用已生成的 `object` 版本实现
- `set_Item` 等方法有了对应 AOT 实现，`MissingMethodException` 消失

**这不是规避技巧，而是 IL2CPP 泛型共享机制的设计意图。**

---

## 六、这条规则的推广

在 `DisStripCode.cs` 里为带有热更类型的泛型组合写 AOT 保留代码时，通用规则是：

| 泛型参数性质 | 在 DisStripCode 里该写什么 |
|---|---|
| 值类型（struct / enum）| 直接写具体类型（`int`、`ValueTuple<int,int>` 等）|
| 引用类型（AOT class）| 写具体 AOT 类型，或 `object` |
| 引用类型（热更 class）| **只能写 `object`**，因为 AOT 不可见热更类型 |
| 接口（热更 interface）| 同上，写 `object` |

注意：对值类型的泛型参数，`object` **不能**替代——值类型的 AOT 实现不共享，必须写出具体类型。原因在于值类型的内存布局与 `object` 不同：`ValueTuple<int,string>` 至少占 12 字节（一个 int 加一个引用），而 `object` 只占指针大小（4/8 字节）。用 `object` 替代会让 AOT 生成的 native 代码采用错误的 struct 布局，字段偏移全部错位。上面这个案例之所以只需要 `object`，是因为需要 AOT 实例化的是**值类型键**（`ValueTuple<int,int>`），而热更类型只是**引用类型值**，值参数可以用 `object` 覆盖。

---

## 七、修完还需要什么

`DisStripCode.cs` 是 AOT 程序集的一部分，修改它需要**重新打 APK**（全量或至少重跑 IL2CPP 编译）。

仅更新 MPQ（热更包）**不够**——因为缺少的 native AOT 实现在 `GameAssembly.so` 里，MPQ 只包含热更 DLL，无法补 AOT 代码。

换句话说：遇到 `AOT generic method not instantiated` 类型的错误，修法在 AOT 层，发布的是 APK，不是 MPQ。

---

## 收束

> `Dictionary<ValueTuple, 热更类型>` 的 `MissingMethodException`，根因是值类型键强制要求独立 AOT 实例化，而热更引用类型值无法出现在 AOT 源码里。修法是在 `DisStripCode.cs` 里用 `object` 替代热更类型：IL2CPP Full Generic Sharing 保证引用类型参数共享一套 AOT 实现，`object` 的实例化覆盖所有引用类型值。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}}">HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法</a>
