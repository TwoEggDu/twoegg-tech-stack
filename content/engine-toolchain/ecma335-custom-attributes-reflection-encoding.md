---
title: "ECMA-335 基础｜反射与特性：从元数据可见到方法可调用"
slug: "ecma335-custom-attributes-reflection-encoding"
date: "2026-04-15"
lastmod: "2026-10-23"
description: "区分反射描述、CustomAttribute 编码与实际执行能力，解释 IL2CPP 裁剪和 HybridCLR 泛型场景中能找到成员却不能调用的原因。"
weight: 17
featured: false
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "CustomAttribute", "Reflection"]
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
primary_series: "ecma335"
series_order: 19
---

序列化框架、注册器或消息分发系统常先扫描类型上的特性，再据此构造对象或调用方法。在编辑器中能工作，换到 IL2CPP Player 或热更代码后失败，不能只用“反射不支持”概括。

这条链至少有三个需要分别确认的事实：**描述还在、引用能解析、所需代码能够执行。** 特性参数的 blob 只是其中一部分。

## 先区分三种反射动作

| 动作 | 主要需要什么 | 还不能证明什么 |
|---|---|---|
| 找到类型和成员 | 保留下来的 metadata 与正确的解析上下文 | 不证明所有泛型实例化都有实现 |
| 读取特性描述 | 构造函数引用、参数编码及其依赖可解析 | 不证明特性构造函数已经执行 |
| 构造特性或调用目标 | 可执行实现、合法参数和受支持的调用路径 | 不证明业务初始化已经完成 |

例如，GetCustomAttributes 通常涉及创建特性实例并执行其构造逻辑。CustomAttributeData 提供不执行特性构造函数的描述读取方式，但它仍有类型与成员解析依赖。不能把后者当作“特性类型和构造函数被裁剪后仍能可靠读取一切”的兜底保证。

## 普通特性如何进入 metadata

```csharp
using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RouteAttribute : Attribute
{
    public RouteAttribute(string path) { Path = path; }
    public string Path { get; }
    public int Priority { get; set; }
}

[Route("login", Priority = 2)]
public sealed class LoginHandler { }
```

普通自定义特性通过 CustomAttribute 表连接三个部分：

| 列 | 内容 |
|---|---|
| Parent | 被标注的类型、方法、字段等目标，使用 HasCustomAttribute coded index |
| Type | 所选构造函数的 MethodDef 或 MemberRef，使用 CustomAttributeType coded index |
| Value | #Blob 中的特性参数编码 |

Type 列指向构造函数，不是直接指向特性类。它让解析器知道固定参数的数量与类型。Parent 和 Type 是 coded index，不是原封不动的普通 token；Value 的索引宽度也不是永远 4 字节。

## 一个足够用的 blob 例子

普通特性 blob 的基本组织为：

```text
Prolog + 固定参数值 + 命名参数数量 + 命名字段／属性及其值
```

Prolog 是字节 `01 00`。固定参数按构造函数签名解释；命名参数还要携带字段或属性标志、类型和名字。

若把示例写成 `[Route("login")]`，没有命名参数时，payload 为：

```text
01 00                  Prolog
05 6C 6F 67 69 6E      SerString："login" 的 UTF-8 字节
00 00                  命名参数数量为 0
```

SerString 使用压缩的字节长度，null 与空字符串不同。这里没有包含 #Blob heap 包住整个 blob 的长度前缀。

加上 `Priority = 2` 后，命名参数区还需说明这是属性、类型为 int32、名字为 Priority、值为 2。工具必须结合构造函数签名解析数据，不能只在字节中搜索一个字符串就推断特性完整语义。

## 方括号语法不保证生成 CustomAttribute 行

这里有一个容易误导 IL2CPP／HybridCLR 读者的例外：

- 普通自定义 RouteAttribute 通常以 CustomAttribute 行和参数 blob 表示。
- SerializableAttribute 的语义可编码到 TypeDef 的 Serializable 标志。
- DllImportAttribute 等伪自定义特性映射到专门的 metadata 标志或记录，如 P/Invoke 相关记录。

因此不能用 `[Serializable]` 演示“必定有一个四字节 CustomAttribute blob”。某些反射 API 会从专门的 metadata 信息合成相应特性视图，反射表面看到的对象不等于底层必有同名表行。

## 动手验证：把读取描述和执行构造分开

在 RouteAttribute 构造函数中暂时加入输出：

```csharp
public RouteAttribute(string path)
{
    System.Console.WriteLine("Route constructor");
    Path = path;
}
```

然后依次执行：

```csharp
var type = typeof(LoginHandler);
var descriptions = type.GetCustomAttributesData();
System.Console.WriteLine(descriptions.Count);

var instances = type.GetCustomAttributes(typeof(RouteAttribute), false);
System.Console.WriteLine(instances.Length);
```

第一个操作用于读取描述，第二个请求特性实例。预期在第二条路径看到构造函数输出。再用 metadata 工具查看 LoginHandler 的 CustomAttribute 行，将 Type 和 Value 与本文模型对应。

还可给 LoginHandler 加上 Serializable，检查 TypeDef 标志和真实的 CustomAttribute 行。记录工具与编译器版本，避免把 API 合成视图当成物理表内容。

这是验证方法，不是“所有 runtime 每次都重新解析 blob”的结论。缓存、预处理和内部表示由具体实现决定；特性构造函数应尽量保持简单，避免用反射扫描触发隐含业务副作用。

## IL2CPP：保留描述与保留执行路径

IL2CPP 项目中，裁剪可能改变可供反射使用的成员集合。字符串驱动的反射用途也未必能被静态分析完整识别。需要按具体工具链规则声明保留，再在目标 Player 中验证。

另一方面，即使拿到了某个泛型类型或方法描述，对应的具体执行实现仍要满足 AOT／共享策略的条件。修复裁剪问题和修复泛型实现缺口不是同一操作，不能默认“补一个 link.xml 就全部解决”。

保留规则的实际用法见 [IL2CPP Managed Code Stripping]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}})，也可查 Unity 官方的 [Managed code stripping](https://docs.unity3d.com/Manual/managed-code-stripping.html)。具体支持范围以项目 Unity 版本为准。

## HybridCLR：沿着失败阶段排查

| 失败阶段 | 优先核对 |
|---|---|
| 找不到类型／成员 | 程序集身份、引用依赖、签名、保留情况 |
| 读取特性失败 | 特性类型与构造函数引用能否解析，参数中的类型依赖是否可用 |
| 构造特性失败 | 构造函数内的异常、静态初始化异常、执行路径 |
| 泛型反射调用失败 | 实际类型／方法实参、AOT 实现与必要的补充信息 |

补充元数据不是恢复所有被裁剪 API 的通用办法，也不会自动执行注册器。具体选择见 [DisStripCode、link.xml 与补充元数据的修复分流]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}})。

## 这条前置主线走到哪里

到这里，DLL 的物理入口、类型、签名、CIL、调用、程序集加载、类型初始化和反射已经连成一条路径。下一步进入 [IL2CPP 实现分析]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) 与 [HybridCLR 系列]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})，再讨论具体版本怎样实现这些契约。

规范查阅：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II §22.10、§23.3 与伪自定义特性相关规则；API 行为另见 Microsoft [CustomAttributeData](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.customattributedata)。

**反射拿到的是可用于描述和定位程序的信息；从描述到实际执行，仍有解析、保留与实现可用性的边界。**

## 系列位置

- 上一篇：[对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})
- 后续：[IL2CPP 实现分析]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) → [HybridCLR 系列]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
