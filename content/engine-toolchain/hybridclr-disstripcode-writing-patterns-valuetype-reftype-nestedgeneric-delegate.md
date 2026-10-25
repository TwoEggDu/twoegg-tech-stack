---
title: "HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写"
date: "2026-03-31"
description: "给出显式 AOT 泛型调用的候选模板，并用裁剪后 IL、生成物和 Player 三层证据验证它是否真正覆盖目标成员。"
weight: 50
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "DisStripCode"
  - "Generics"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

显式 AOT 引用的目标，是让主包构建看见需要的泛型方法调用。文件取名 `DisStripCode.cs` 并不会赋予它特殊效果；最终保留下来的 IL 和生成物，才决定这份输入是否有效。

这篇只给候选模板和验证方式。先用 [修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) 确认确实需要 native 实例，再用 [共享规则]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 选择参数。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

## 一份带有真实调用的候选模板

下面代码应放进参与主包构建的 AOT 程序集。它使用 `Preserve` 表达保留意图，不需要为了保留而在业务启动时执行这些分配。示例已通过桌面 Roslyn 编译，引用本机 Unity 的 .NET Standard 2.1 与 CoreModule；这只验证语法和 API 引用，**尚未为这些组合完成 Unity 裁剪、IL2CPP 与 Player 验收。**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

internal static class AotGenericSeeds
{
    [Preserve]
    private static void KeepReferenceList()
    {
        var list = new List<object>();
        list.Add(new object());
        list.Contains(new object());
    }

    [Preserve]
    private static void KeepValueList()
    {
        var list = new List<int>();
        list.Add(1);
        list.Contains(1);
    }

    [Preserve]
    private static void KeepTupleDictionary()
    {
        var map = new Dictionary<ValueTuple<int, object>, object>();
        var key = new ValueTuple<int, object>(1, new object());
        map[key] = new object();
        map.TryGetValue(key, out object value);
    }

    [Preserve]
    private static void KeepLinqToList()
    {
        IEnumerable<object> source = new object[] { new object() };
        List<object> result = Enumerable.ToList<object>(source);
        GC.KeepAlive(result);
    }
}
```

`KeepReferenceList` 与 `KeepValueList` 分别保留引用、值类型参数的调用形态。`KeepTupleDictionary` 对应 key 第二项为引用类型的共享形态；如果 key 是 `(int,int)`，不能照抄这一组。代码明确调用 `set_Item` 和 `TryGetValue`，避免把“构造过字典”误当成“使用过所有成员”。

LINQ 示例传入明确的非空数组。不要写 `IEnumerable<object> source = null; source?.ToList();` 再假定一定产生目标调用：这类常量空路径可能被编译器消除。更一般地，不能用一段永远不可达的分支替代对最终 IL 的核对。

## 委托与嵌套泛型要分开看

`Action<T>` 或 `Func<T,...>` 有两个独立对象：委托调用所需的签名路径，以及委托指向的方法。创建一个 `Action<object>` 不会自动保留某个完全不同的闭包结构或 async builder 方法。

嵌套泛型也需要落实到成员。`Outer<List<HotClass>>` 的外层引用共享，并不意味着 `List<HotClass>.Sort`、比较器或业务 lambda 全部被覆盖。把异常中的完整方法签名逐层拆开，才能知道该模板应添加哪个调用。

模板只列高频容器调用，不尝试生成“一份覆盖所有委托与嵌套容器”的保留文件。这种文件既难审核，也容易让包体和构建成本失去边界。

## async builder 不能靠默认值保住

下面两种写法都不能证明保留了 `Start<TStateMachine>` 或 `AwaitUnsafeOnCompleted<TAwaiter,TStateMachine>`：

- 声明 `AsyncUniTaskMethodBuilder<T>` 局部变量。
- 对 builder 写 `default` 或构造实例，但不调用目标泛型方法。

这些方法自身还有状态机、awaiter 参数以及接口约束。热更中新生成的 struct 状态机也不能被任意同尺寸的 AOT struct 替代。实际处理应结合编译后的 IL、所用 UniTask 版本和 [async 案例证据边界]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})；补充定义所在 AOT 程序集的元数据是正常可选路径。

## 从源码到 Player 的四次核对

| 阶段 | 核对内容 | 单独通过仍不能证明什么 |
|---|---|---|
| C# 编译 | helper 位于 AOT 程序集，参数满足约束 | 主包裁剪后仍存在 |
| 托管裁剪后 | helper 和所需调用仍在最终 DLL，目标成员身份正确 | 已有可用 native 实现 |
| IL2CPP 生成后 | 相应实例或共享方法出现在实际生成物与注册信息中 | 本次业务一定调用了它 |
| Player | 执行目标成员，核对结果及实际方法指针／调用路径 | 所有平台和其他成员都覆盖 |

如果要区分 native 与补充元数据解释路径，可以使用隔离 Player 对照、原生断点或匹配符号。不要从正式启动中随手删除 metadata，再把可能影响其他路径的结果当作单变量实验。

已有 [泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 展示了这类证据如何关联。新增模板需要各自补证，不能继承另一个样本的“通过”。

## 保留文件也要有退出条件

每组调用旁记录它解决的目标签名、引入它的回归用例，以及对应主包版本。后续发现它从未进入热路径，或已经由其他稳定调用覆盖时，可以通过独立构建对照评估删除。

下一篇 [AOT 泛型工作流]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) 说明这份人工维护输入如何与扫描清单、补充元数据和发布包协作。
