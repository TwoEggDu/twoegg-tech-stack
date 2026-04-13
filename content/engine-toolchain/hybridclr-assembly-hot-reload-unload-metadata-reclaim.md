---
title: "HybridCLR 程序集热重载｜IL2CPP 为什么不能卸载程序集，热重载版怎么做到的"
date: "2026-04-13"
description: "从 IL2CPP 的全局元数据注册表、静态字段存储、GC 根这几个层面讲清楚为什么标准 IL2CPP 不能卸载程序集，然后对比 .NET CoreCLR 的 AssemblyLoadContext 机制，再沿着热重载版的公开接口和约束条件推导出商业版需要在哪些层面做改造，最后把 GC 配合、类型身份、重加载后的状态恢复和工程边界收进来。"
weight: 59
featured: false
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "HotReload"
  - "Memory"
  - "Architecture"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> 程序集卸载是 IL2CPP 从来没有设计过的事。每一张全局注册表、每一个静态字段、每一条缓存的 MethodInfo 都在向内指引并持有引用——这些结构只增长、不收缩，也不提供任何移除路径。

这是 HybridCLR 系列第 28 篇。

前一篇（HCLR-27）拆了解释器在指令层面的优化，下一篇（HCLR-29）讲代码加密。这篇要回答的问题完全在另一个方向：

`如果一个热更程序集加载后发现有 bug，能不能把它从内存中完全卸掉、换上修好的版本、继续运行？`

在标准 IL2CPP 里，这件事做不到。在 HybridCLR 商业版的"热重载版"（Hotreload Edition）里，它声称做到了——99.9% 的 metadata 内存可以回收。

这篇文章先从社区版源码讲清楚"为什么做不到"，再对比 .NET CoreCLR 的 `AssemblyLoadContext`，然后从热重载版的公开接口和约束条件推导出商业版需要改什么，最后把 GC 配合、类型身份和工程边界收进来。

> 本文 IL2CPP 层面的分析基于 HybridCLR 社区版 v6.x（main 分支，2024-2025）的开源代码。热重载版的实现是商业闭源的，本文不做源码级猜测，只从官方公开文档和 API 约束反推架构需求。

## IL2CPP 为什么原本不能卸载程序集

要理解"不能卸载"，得先看清楚一个程序集在加载后到底把数据写进了哪些全局结构。从社区版源码看，至少有以下几层。

### 全局注册表只增长、不收缩

IL2CPP 的 `MetadataCache` 在初始化阶段分配了一组全局静态数组：

```
static Il2CppClass** s_TypeInfoTable = NULL;
static Il2CppClass** s_TypeInfoDefinitionTable = NULL;
static const MethodInfo** s_MethodInfoDefinitionTable = NULL;
static Il2CppString** s_StringLiteralTable = NULL;
static const Il2CppGenericMethod** s_GenericMethodTable = NULL;
static Il2CppImage* s_ImagesTable = NULL;
static Il2CppAssembly* s_AssembliesTable = NULL;
```

这些表在 `MetadataCache::Initialize()` 里通过 `IL2CPP_CALLOC` 一次性分配，大小由 metadata header 决定。整个 runtime 生命周期内，这些表只写入、不删除、不缩容。

HybridCLR 社区版在这之上叠了自己的注册层。`InterpreterImage` 在 `InitBasic()` 阶段调用 `RegisterImage(this)`，将自身写入一个静态数组：

```
static InterpreterImage* s_images[kMaxMetadataImageCount];
```

同样没有对应的 `UnregisterImage` 路径。`Assembly::LoadFromBytes` 最终调用 `il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(ass)` 把程序集注册到 IL2CPP 的全局 assembly 列表里，这条注册也是单向的。

### InterpreterImage 的 metadata 存储没有释放路径

打开 `InterpreterImage.h`，可以看到这个类持有大量 metadata 容器：

```
std::vector<Il2CppTypeDefinition> _typesDefines;
std::vector<Il2CppMethodDefinition> _methodDefines;
std::vector<FieldDetail> _fieldDetails;
std::vector<ParamDetail> _params;
std::vector<Il2CppGenericParameter> _genericParams;
std::vector<Il2CppGenericContainer> _genericContainers;
std::vector<const Il2CppType*> _types;
std::vector<Il2CppClass*> _classList;
std::unordered_map<uint32_t, CustomAttributesInfo> _tokenCustomAttributes;
```

这些容器在 `InitRuntimeMetadatas()` 的多个阶段（`InitTypeDefs_0()`、`InitMethodDefs()`、`InitFieldDefs()` 等）被填充。填充过程是单向的：往 vector 里 push，往 map 里 insert，没有对应的 clear 或 erase 路径。

更关键的是，`InterpreterImage` 本身没有析构函数做清理。基类 `Image` 有虚析构函数，会释放 `_rawImage` 和 `_pdbImage`，但 `InterpreterImage` 的那些 vector 和 map 虽然会随 C++ 对象析构自动释放内存，前提是有人 `delete` 这个对象——而在社区版的代码路径里，没有任何地方 `delete` 一个已加载的 `InterpreterImage`。

### Il2CppClass 和 Il2CppMethodInfo 只分配不回收

metadata 解析过程中产生的运行时结构——`Il2CppClass*`、`Il2CppMethodInfo*`——都通过 `MetadataMallocT<>()` 或 `HYBRIDCLR_METADATA_MALLOC` 分配。这些宏最终调用的是 IL2CPP 的 metadata 内存池，设计目标是进程级生命周期，没有 per-object 的 free。

`MetadataPool` 提供了 `GetPooledIl2CppType()` 这样的池化接口，用于 metadata 去重，但同样没有 `Release` 或 `Free` 方法。整个池只有 `Initialize()`，没有 `Cleanup()`。

### 静态字段存储在类初始化时分配

当一个类型第一次被访问时，IL2CPP 调用 `Runtime::ClassInit()` 触发类型初始化。这个过程会为类的静态字段分配存储空间。在标准 IL2CPP 里，静态字段存储一旦分配就不会释放，因为类型本身不会被卸载。

静态构造函数（`.cctor`）只执行一次。IL2CPP 用 `Il2CppClass::initialized` 标记来保证这一点。没有"反初始化"机制。

### GC 根持有执行栈引用

`MachineState` 通过 `GarbageCollector::AllocateFixed()` 分配执行栈，并通过 `RegisterDynamicRoot(this, GetGCRootData)` 将栈注册为 GC 根。这意味着执行栈上任何指向 metadata 结构的引用——`InterpFrame` 里的 `const MethodInfo* method`、栈上的对象引用——都被 GC 根持有。

同时 `MachineState` 维护了一个 `std::stack<const Il2CppImage*>`，记录当前正在执行的程序集镜像栈。这些引用在执行期间持续存在。

### transform 缓存绑定到方法

当解释器第一次调用某个热更方法时，transform 阶段会把 CIL 方法体转换成 `InterpMethodInfo`，包含：

```
byte* codes;           // 转换后的指令字节码
uint64_t* resolveDatas; // 解析好的元数据引用数组
```

`resolveDatas` 持有对 `Il2CppClass*`、`MethodInfo*`、字段偏移等 metadata 的强引用。这些数据挂在 `MethodInfo` 的 `interpData` 指针上，只要 `MethodInfo` 存在就不会被释放。

把上面这些加起来：全局注册表 + InterpreterImage 的 metadata 容器 + 分配后不回收的 Il2CppClass/MethodInfo + 静态字段存储 + GC 根 + transform 缓存。每一层都在单向持有引用，没有任何一层提供反向清理路径。这就是标准 IL2CPP 不能卸载程序集的结构性原因。

## .NET 的 AssemblyLoadContext 做了什么

.NET CoreCLR（.NET Core 3.0+）通过 `AssemblyLoadContext` 提供了程序集卸载能力。它的做法和 IL2CPP 形成鲜明对比，值得简要对比。

CoreCLR 的设计核心是"可收集上下文"（collectible context）：

- 程序集不是加载到全局作用域，而是加载到一个独立的 `AssemblyLoadContext` 实例里
- 当调用 `AssemblyLoadContext.Unload()` 时，runtime 标记这个上下文为待卸载状态
- 实际卸载是协作式的：runtime 触发 `Unloading` 事件让业务代码清理资源，然后等待所有对该上下文内类型和对象的强引用被释放
- 最终由 GC 在下一轮收集时真正回收所有关联内存

这个机制成立的前提是几个关键设计：

**类型的弱引用管理。** CoreCLR 的 JIT 编译器和类型系统知道哪些类型属于哪个 `AssemblyLoadContext`。全局缓存里对可收集类型的引用是弱引用或可清理的条件引用，不会阻止卸载。

**独立的元数据域。** 每个 `AssemblyLoadContext` 有自己的程序集解析逻辑。同名程序集可以存在于不同上下文中，互不干扰。

**GC 集成。** GC 能感知 `AssemblyLoadContext` 的生命周期。当一个上下文被标记为待卸载且所有强引用释放后，GC 可以回收该上下文内所有对象的内存，包括 JIT 编译的 native code。

IL2CPP 没有任何这些基础设施：

- 类型注册是全局的（`s_TypeInfoTable`），没有上下文隔离
- metadata 引用都是强引用，没有弱引用机制
- GC（BoehmGC 或 IL2CPP 的增量 GC）不知道"程序集"这个概念，只知道对象
- 没有 `Unloading` 事件，没有协作式卸载协议

所以 HybridCLR 热重载版要实现程序集卸载，不能简单移植 CoreCLR 的方案，而是要在 IL2CPP 的全局注册模型上做侵入式改造。

## 热重载版需要改什么

以下分析基于热重载版的公开 API（`RuntimeApi.TryUnloadAssembly`、`RuntimeApi.ForceUnloadAssembly`）和官方文档描述的约束条件。商业版源码不公开，这里只从"需要满足的需求"反推"必须做的改造"。

### MetadataCache 注册必须可逆

社区版的 `MetadataCache::RegisterInterpreterAssembly(ass)` 是单向操作。热重载版必须支持反向操作——从全局 assembly 列表中移除指定的程序集，从 image 列表中移除对应的 image。

这意味着 `s_images[]` 数组中对应索引的槽位需要能被置空或标记为已释放，而不是简单的 `s_images[index] = nullptr`（因为其他地方可能还持有这个 index 做查找）。更可能的做法是维护一个版本号或 generation 标记，让持有旧引用的代码能检测到 image 已失效。

### InterpreterImage 必须有完整的析构路径

社区版的 `InterpreterImage` 虽然内部用了 `std::vector`（析构时自动释放内存），但它持有的 `Il2CppClass*`、`Il2CppMethodInfo*` 等通过 metadata 池分配的对象不会被自动回收。

热重载版需要：

- 为 `InterpreterImage` 增加显式的 `Dispose`/`Cleanup` 路径
- 遍历 `_classList`，释放每个 `Il2CppClass` 及其关联的虚表、接口表、字段信息
- 遍历 `_methodDefines`，释放每个 `Il2CppMethodInfo` 及其 `interpData`
- 清理 `_tokenCustomAttributes` 中通过 `GarbageCollector::AllocateFixed()` 分配的 custom attribute 缓存
- 释放 `_rawImage`（解析后的原始 metadata）

### transform 缓存必须按程序集可清理

每个通过 transform 产生的 `InterpMethodInfo` 持有 `codes` 和 `resolveDatas`。`resolveDatas` 中的每个条目可能指向 `Il2CppClass*`、`MethodInfo*`、字段偏移量等。

卸载一个程序集时，属于该程序集的所有方法的 `InterpMethodInfo` 必须被释放。同时，`MethodInfo` 上的 `interpData` 指针必须被置空，防止悬挂指针。

### MetadataPool 的分配必须支持按域释放

社区版的 `MetadataPool` 是进程级的池分配器，没有 per-image 的释放能力。热重载版可能需要把 metadata 分配改为 per-image arena（区域分配器），这样卸载一个 image 时可以整体释放该 arena，避免逐个对象 free 的复杂度和碎片化问题。

### 官方文档确认了 99% 以上的回收率

热重载版文档明确表示：除了被 Unity 引擎内部持有的 Script 类（`MonoBehaviour`、`ScriptableObject`）以及 `[Serializable]` 类型的 metadata 之外，几乎所有（99.9%）的 metadata 都可以被卸载。并且"多次加载和卸载同一个程序集只会产生一次不释放行为，不会导致泄漏或持续增长"。

这说明热重载版确实实现了上述大部分改造，并且对不可释放的部分做了复用策略——同一个类型名的 Script 类在首次加载时创建的 metadata 会被缓存，后续重载复用同一份缓存，不会重复分配。

## GC 层面的配合

程序集卸载不只是 metadata 层的事。当一个程序集被卸载时，GC 堆上可能还存在该程序集中定义的类型的活跃对象。这些对象怎么处理，是卸载机制最复杂的部分。

### 活跃对象的引用检查

热重载版提供了两个 API，对应两种策略：

`RuntimeApi.TryUnloadAssembly(assembly, printObjectReferenceLink)` 是安全卸载——如果 GC 堆上还存在该程序集中类型的活跃对象，卸载会失败并返回错误报告。设置 `printObjectReferenceLink=true` 会打印引用链，帮助开发者定位哪些对象还在持有引用（但会显著增加卸载时间）。

`RuntimeApi.ForceUnloadAssembly(assembly, ignoreObjectReferenceValidation, printObjectReferenceLink)` 是强制卸载——无论是否还有活跃引用都会执行卸载。官方文档警告这可能导致崩溃，建议只在确认安全或联系技术支持后使用。

### 卸载前必须清理的引用类型

官方文档列出了卸载前必须由业务代码清理的引用：

- **协程和异步操作：** 正在执行的协程或 `async Task` 如果引用了即将卸载的程序集中的方法，必须先停止
- **委托和事件：** 所有注册到全局事件的委托，如果目标方法在卸载程序集中，必须先反注册
- **UI 回调：** 按钮点击等 UI 事件绑定到卸载程序集中的方法时，必须先解绑
- **反射缓存：** `Assembly`、`Type`、`MethodInfo` 等反射对象的引用必须释放
- **泛型实例：** 泛型类如果类型参数包含卸载程序集中的类型，整个泛型实例也被视为非法引用

### 静态字段的处理

类型的静态字段存储必须在卸载时释放。对于值类型静态字段，直接释放存储即可。对于引用类型静态字段，需要先将其置空（断开对 GC 堆对象的引用），然后释放存储本身。

静态构造函数的执行记录（`Il2CppClass::initialized` 标记）必须重置，这样重新加载同名程序集时，静态构造函数才能再次执行。

### 析构器的限制

热重载版文档明确禁止在可卸载程序集中使用析构器（`~XXX()`）。这是一个合理的工程约束——析构器在 GC 的 finalization 线程上异步执行，如果析构器引用了已卸载程序集的 metadata，会导致悬挂指针。禁止析构器比处理析构器的生命周期复杂度低得多。

### 操作顺序

综合上述约束，卸载一个程序集的操作顺序应该是：

1. 检查引用：扫描 GC 堆，确认没有活跃对象的类型定义在目标程序集中
2. 清理静态字段：置空所有引用类型静态字段，释放静态存储
3. 清理 transform 缓存：释放该程序集所有方法的 `InterpMethodInfo`
4. 反注册类型：从全局 `Il2CppClass` 表和类型缓存中移除该程序集的类型
5. 反注册 metadata：从 `MetadataCache` 和 `InterpreterImage` 注册表中移除
6. 释放 metadata 内存：释放 `InterpreterImage` 及其持有的所有 metadata 结构

## 重新加载时的状态恢复

卸载只是前半段。热重载的完整流程是：卸载旧版本 → 加载新版本 → 恢复运行。加载新版本时有几个关键问题。

### 类型身份

卸载再重新加载同名程序集后，`typeof(MyType)` 返回的 `Type` 对象是不是同一个？

从热重载版的约束来看，答案是：逻辑上是同一个类型（相同的程序集名 + 类型名），但 runtime 层面的 `Il2CppClass*` 指针一定是新的。这意味着：

- 在卸载前持有的 `Type` 对象在卸载后失效
- `typeof(MyType)` 在重新加载后返回的是新的 `Type` 实例
- 如果业务代码用 `Type` 对象做 key 缓存了什么（比如序列化库的类型映射），缓存会失效

这也是为什么官方文档特别提到：使用了反射 metadata 缓存的库（如 LitJson 等序列化工具）必须在重载后手动清理缓存。

### 序列化资源的重新绑定

Prefab 和 ScriptableObject 上挂载的 `MonoBehaviour` 脚本引用的是类型名（通过 `fileID` 和 `guid` 以及 `m_Script` 引用）。重载后，Unity 引擎需要将这些引用重新解析到新的类型定义上。

热重载版文档对此有严格约束：

- 重载前后，`MonoBehaviour` 和 `ScriptableObject` 的序列化字段名不能改变
- 事件函数（`Awake`、`OnEnable` 等）的数量不能改变
- 不能使用泛型继承（`class MyScript : CommonScript<int>`）

这些约束的本质是：Unity 引擎内部对 Script 类的 metadata 做了缓存（比如序列化字段列表、事件函数指针表），热重载版在重载时需要更新这些缓存使其指向新的类型定义。如果字段名或事件函数数量变了，缓存更新的逻辑就无法安全执行。

### 静态构造函数重新执行

重载后，静态构造函数必须重新执行。这意味着所有通过静态构造函数初始化的状态——单例实例、配置缓存、运行时注册表——都会被重置。

对于使用单例模式的代码，这可能是预期行为（新版本的单例用新逻辑），也可能是意外（丢失了运行时累积的状态）。这需要业务层自己做状态持久化和恢复。

### 序列化字段类型的特殊限制

如果一个 `[Serializable]` 类型或 `MonoBehaviour` 的序列化字段类型来自可卸载程序集，官方文档要求使用数组（`A[]`）而不是 `List<A>`。原因是 Unity 引擎内部对 `List<T>` 的序列化 metadata 缓存处理更复杂——它需要缓存 `List<A>` 这个泛型实例的 metadata，如果 `A` 来自一个可卸载的程序集，这个缓存的生命周期管理就会出问题。数组的序列化 metadata 相对简单，可以安全地更新。

## 工程边界

热重载技术在工程层面有明确的适用域和风险域。

### 适合热重载的场景

**小游戏合集。** 这是热重载版的主打场景。每个小游戏是一个独立的程序集，切换游戏时卸载前一个、加载后一个。小游戏程序集通常是自包含的，跨程序集依赖少，卸载后状态清理相对简单。

**UGC/Mod 系统。** 用户生成的 mod 程序集设计时就应该是可插拔的，有明确的加载/卸载生命周期。mod 系统本身提供的 API 层可以做引用隔离，确保 mod 代码不会把引用泄漏到宿主程序的全局状态里。

**独立的玩法模块。** 如果游戏的某个玩法模块（比如活动系统、战斗模式）被封装在独立程序集里，且模块之间通过接口而非具体类型耦合，这样的模块可以安全地热重载。

### 风险较高的场景

**有大量跨程序集类型引用的程序集。** 如果程序集 A 中的类型被程序集 B、C、D 大量引用（作为字段类型、方法参数、泛型参数），卸载 A 前必须先确保 B、C、D 中没有任何活跃对象持有 A 中类型的引用。依赖越深，清理越难。

**在序列化资产中大量使用的类型。** 如果一个类型被用作 Prefab 上的 `MonoBehaviour` 或 `ScriptableObject`，它的 metadata 会被 Unity 引擎内部缓存。虽然热重载版对这类类型做了特殊处理（首次缓存、后续复用），但序列化字段的约束（字段名不能改、事件函数数量不能变）意味着这类类型的热重载灵活性有限。

**有 native 插件绑定的程序集。** 如果程序集中的代码通过 `DllImport` 调用 native 插件，或者通过 `AndroidJavaProxy` 等机制与平台层交互，这些外部系统持有的回调引用在程序集卸载时不一定能被正确清理。官方文档专门提到了 `AndroidJavaRunnableProxy` 的案例——Java GC 和 C# 卸载之间的时序不一致会导致悬挂引用。

**使用了 DOTS 的程序集。** 官方文档明确表示热重载版不兼容 DOTS。DOTS 的 ECS 架构在 native 层缓存了大量类型信息（ComponentType 注册表、Archetype 元数据），这些缓存远超 HybridCLR 的控制范围。

### 依赖卸载顺序

文档要求按照依赖关系的逆序卸载：先卸载依赖方（downstream），再卸载被依赖方（upstream）。如果 Assembly B 依赖 Assembly A，必须先卸载 B，再卸载 A。不能跳过 B 直接卸载 A。

这个约束的本质是：程序集的 metadata 之间存在引用关系（B 中的类型可能继承 A 中的基类，B 中的方法签名可能引用 A 中的类型）。如果先卸载 A，B 中的 metadata 引用就会变成悬挂指针。

## 收束

IL2CPP 不能卸载程序集，不是一个功能缺失，而是一个架构决策的自然结果：全局静态注册表（`s_TypeInfoTable`、`s_AssembliesTable`）、只分配不释放的 metadata 池、单次执行的静态构造函数、没有上下文隔离的类型系统——这些设计选择让 metadata 的生命周期绑定到了进程级别。

.NET CoreCLR 通过 `AssemblyLoadContext` 实现了程序集卸载，但它的实现前提是整个运行时从类型加载、JIT 编译到 GC 都内建了上下文感知能力。IL2CPP 没有这些基础设施。

HybridCLR 热重载版在商业闭源层面解决了这个问题，从公开的 API 和约束条件可以推断出它至少需要做到：MetadataCache 注册可逆、InterpreterImage 可析构、transform 缓存按程序集可清理、静态字段存储可释放。官方文档声称的 99.9% metadata 回收率和"多次加载卸载不会泄漏"的保证，说明这些改造已经落地。

但这不是魔法。热重载版的约束条件清楚地划出了边界：不能用析构器、`MonoBehaviour` 序列化字段名不能改、事件函数数量不能变、`List<T>` 要换成数组、DOTS 不兼容、依赖必须逆序卸载。这些约束背后的原因，在社区版源码里都能找到对应的结构性根源。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}}">HybridCLR 解释器指令优化｜标准优化与高级优化在 HiOpcode 层到底做了什么</a>（HCLR-27）
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-code-encryption-access-control-policy.md" >}}">HybridCLR 代码加密与访问控制｜DLL 加密怎么与解释器配合，访问控制策略拦在哪一层</a>（HCLR-29）
- 回到入口：<a href="{{< relref "engine-toolchain/hybridclr-series-index.md" >}}">HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}}">HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}}">HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事</a>
