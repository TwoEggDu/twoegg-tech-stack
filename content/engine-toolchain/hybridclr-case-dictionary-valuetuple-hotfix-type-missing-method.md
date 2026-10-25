---
title: "HybridCLR 案例｜Dictionary<ValueTuple, 热更类型> 的 MissingMethodException 与 object 替代法"
date: "2026-03-27"
series: "HybridCLR"
hybridclr_version: "历史案例版本未完整固定；当前机制另按 v9 配套源码核对"
tags:
  - "IL2CPP"
  - "AOT"
  - "Generics"
  - "Dictionary"
  - "MissingMethodException"
  - "DisStripCode"
weight: 47
lastmod: "2026-10-25"
---

历史项目在写入以 (int, int) 为键、热更类为值的 Dictionary 时，记录了 AOT generic method not instantiated。这个案例适合解释一种具体修法：当需要为该调用保留 native 共享实现时，怎样在 AOT 代码里表达热更引用类型。

> **范围**：这是系列第 18 篇的历史案例整理。原报错保留，修法按当前共享规则解释；原案例未保存完整 Player 对照，因此不把下面 helper 写成已在当前实验仓库验证的结果。常规恢复执行也可以考虑匹配的补充 metadata。

## 原报错表达了什么

```text
MissingMethodException: AOT generic method not instantiated in aot.
assembly:mscorlib.dll
method: Dictionary<ValueTuple<Int32,Int32>,GameLogic.T_HotfixType>::set_Item(...)
```

它给出了方法定义所在程序集、封闭类型与目标成员。排查时先确认 Dictionary 定义和 set_Item 未被主包裁剪，再检查 native 共享实现与补充 IL 是否可用。

异常本身不证明只能重发主包；[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})已给出其他固定样本通过补充 metadata 恢复执行的对照。

## 如何计算这次共享目标

这里讨论普通泛型共享，不把它称作完全泛型共享：

- 键 `ValueTuple<int,int>` 是值类型，不能整体替换为 object。
- 值 `T_HotfixType` 若确实是 class，作为泛型实参可按引用类型共享规则映射到 object。
- 目标是 `set_Item` 这个方法实例，构造一次 Dictionary 不自动证明所有成员都保留。

因此可研究 `Dictionary<(int,int),object>.set_Item` 这条 AOT 共享路径。引用类型用 object 表达的是共享形态，实际运行时类型身份并没有被改成 object。[官方规则](https://www.hybridclr.cn/docs/basic/aotgeneric#共享类型计算规则)还包含 enum 与泛型 struct 的处理，不能概括成所有值类型永远互不共享。

## helper 要包含真实方法调用

下面是待放入 AOT 程序集、再检查生成物和 Player 的候选代码。已用桌面 Roslyn 与本机 Unity 的 .NET Standard 2.1/CoreModule 引用完成编译检查，这不等于裁剪或 native 实例验收：

```csharp
using System.Collections.Generic;
using UnityEngine.Scripting;

static class DictionaryAotSeeds
{
    [Preserve]
    static void PreserveSetItem()
    {
        var dictionary = new Dictionary<(int, int), object>();
        dictionary[(0, 0)] = null;
    }
}
```

此 helper 的目的，是让构建器看到具体调用；业务不需要为了保留而执行它。仍要检查 helper 是否进入目标 AOT 程序集、托管裁剪后的方法体是否保留，以及 native 与运行时路径是否符合预期。

如果缺的是 TryGetValue，就显式覆盖那个调用，而不是把“写过同一个类型”当作全部方法都已生成。

## 为什么不直接写热更类型

主包 AOT 代码不能直接依赖被排除出 AOT 编译输入的热更程序集。Editor 的 C# 编译阶段可能看得到热更 DLL，但不能因此认为最终 Player 的静态依赖合法。

这也不表示所有热更类型都能替换为 object：如果实际参数是热更 struct，必须重新评估共享条件；不能通过强行换成 object 获得同一实例，也不应声称 runtime 会因此直接采用错误 struct 布局。

布局大小与对齐依赖平台和具体类型，本文不使用“ValueTuple<int,string> 至少 12 字节”这种跨平台数字作论据。

## 两种交付动作

若补充所需 AOT 方法体能够满足功能与性能需求，可以作为常规方案，文件按当前主包版本匹配。若决定新增 native seeds，修改会影响主包 AOT 产物，需要重新构建并发布对应 Player；仅替换热更 DLL 不能改变旧包 native 代码。

两种方案不是“临时”与“正确”的固定等级，选择取决于缺失层次、性能要求和发布约束。

## 系列位置

完整选择见 [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})，递归共享规则见 [共享类型判断]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})，模板验收见 [DisStripCode 手册]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})。

**先定位具体方法，再计算共享形态，最后验证实际生成与执行路径。**
