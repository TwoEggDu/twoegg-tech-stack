---
slug: "hybridclr-程序集热重载il2cpp-为什么不能卸载程序集热重载版怎么做到的"
title: "HybridCLR 程序集热重载｜卸载条件、metadata 复用与状态恢复"
date: "2026-04-13"
description: "区分托管对象回收、native metadata 寿命与程序集卸载，依据官方公开约束解释热重载的退出条件、保留类型信息及重新加载后的状态管理。"
weight: 59
featured: false
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "HotReload"
  - "Memory"
  - "Architecture"
series: "HybridCLR"
hybridclr_version: "社区 runtime v9.0.1；商业公开文档 9.x（2026-10-25）"
lastmod: "2026-10-25"
---

> 卸载程序集需要让对象、正在执行的代码和运行时缓存共同退出；做完一次 GC 只覆盖其中一部分。

这是 HybridCLR 系列第 28 篇。项目想把一个小游戏或玩法模块换成新版本时，最直接的问题是：旧 DLL 已经不用了，为什么不能像释放资源一样把它卸掉？

问题在于，加载不仅带来文件字节，还建立了类型、方法、反射、静态存储与执行之间的关系。只有明确谁仍然使用这些关系，才能讨论安全卸载。

> 源码基线：社区 runtime v9.0.1，[`83929787581bce6c7219418f62348a10c33582ee`](https://github.com/focus-creative-games/hybridclr/tree/83929787581bce6c7219418f62348a10c33582ee)。商业约束依据 2026-10-25 查阅的官方 9.x 热重载手册。本文没有商业版卸载实验，也不推断闭源释放算法。

## 三种寿命不能混在一起

| 对象 | 谁还可能使用它 | 为什么单次 GC 不够 |
|---|---|---|
| 托管对象 | 静态字段、委托、GCHandle、活跃栈等 | 仍可达的对象不能按“模块已退出”自动判死 |
| native metadata 与转换缓存 | 类型查询、方法入口、泛型、反射包装及引擎缓存 | 它们不都属于托管 GC 堆 |
| 活跃执行 | 托管线程、协程、异步续体、native 回调 | 删除对象引用不代表代码已返回或回调已停止 |

普通社区版的加载路径会建立并注册 `InterpreterImage`，转换结果又会依赖相关类型和方法描述符。可以从固定源码的 [Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/Assembly.cpp)、[InterpreterImage.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/InterpreterImage.h)跟读这些关系。

局部对象有析构函数、线程退出能清理状态，都不等于存在可卸载任意程序集的完整协议。业务也不能靠删除某个 image 或把某个注册槽置空来补出这个协议。

## .NET 的可卸载上下文提供了什么参照

CoreCLR 的 collectible `AssemblyLoadContext` 把一组程序集放进可协作卸载的上下文。`Unload()` 发起卸载，但完成还要求没有相关活跃调用栈，也没有阻止回收的强引用或 GCHandle。[Microsoft 的卸载说明](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability)专门区分了发起卸载和卸载完成。

这个参照说明“上下文、引用和执行寿命”都需要参与。它不能证明 HybridCLR 热重载版移植了同样的上下文模型，也不能推出其 metadata 必然使用 per-image arena、generation 标记或某套释放顺序。

## 商业热重载版公开了哪些检查入口

[官方热重载手册](https://www.hybridclr.cn/docs/business/reload/manual)提供 `TryUnloadAssembly` 与 `ForceUnloadAssembly`。前者尝试卸载并返回报告；后者即使仍有引用也会移除目标，因此调用方必须理解失败报告和悬挂使用风险，不能把它作为常规重试手段。

`printObjectReferenceLink` 用于输出非法引用线索。检查结果不是业务已经停止所有 native 入口的替代证明；业务仍须履行产品要求的退出条件。

一个可审阅的业务退出顺序是：

1. 阻止新任务、事件和 native 回调进入旧模块。
2. 取消任务并等待旧模块的在途执行结束，完成回调停止／Join 等握手。
3. 解除委托、事件和 UI 注册，按所有权释放上下文与句柄，清理反射缓存。
4. 按依赖逆序调用产品的检查与卸载接口，处理失败报告。
5. 验证旧模块不可再调用、引用与内存变化，再进入新版本。

这是业务协议，不是对商业运行时内部释放顺序的还原。具体线程退出问题见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

## 热重载并不保证每个类型描述符都是新地址

同名类型重新加载后，业务必须重新取得并验证可用的类型和反射对象；不能继续使用旧缓存。

但不能进一步说“所有 `Il2CppClass*` 一定重新分配”。官方说明，一部分被 Unity 持有的脚本类型和可序列化类型 metadata 会保留，并在后续加载时复用；还有不涉及卸载模块类型的泛型 metadata，例如 `List<int>`，不会随该模块释放。

因此需要分开看：

- **模块语义和业务状态**：新版本应按约定重新建立，旧对象和回调不能混入。
- **runtime 内部存储**：部分可释放，部分可能保留或复用，由产品协议管理。
- **业务缓存有效性**：不能用某个 native 地址恰好相同或不同来判定。

官方“多数程序集可回收99%以上 metadata”的表述是其产品资料，不是本项目循环加载、卸载后测得的回收率。它也不是“托管堆、native metadata 与引擎缓存全部归零”的承诺。

## 资源与类型变化有哪些限制

Unity 的序列化和脚本消息机制会缓存类型相关信息，因此热重载的可变范围小于“任意替换所有资源结构”。官方当前列出的约束包括：

- MonoBehaviour 消息／事件函数不能增删；不能简化为“数量相同即可”，改名替换也可能改变集合。
- 原来同名脚本类的序列化字段名不能随意改变，字段类型须遵循产品约束。
- 相关字段若使用可卸载程序集中的自定义类型，需要检查 `Serializable` 等要求；`List<A>` 与 `A[]` 也有明确差异。
- 不支持相关泛型继承、终结器和 DOTS 等场景，具体适用范围按所用版本核对。

这些约束不应靠“类型名一样就可以复用”来规避。官方提供 `HotReloadCompatibilityValidator`，适合作为构建前检查，但运行时的引用与退出验证仍然需要做。

## 重新加载后的状态由谁恢复

新的类型使用仍须遵守类型初始化语义。不能把程序集装载完成等同于所有 `.cctor` 已执行，也不能通过业务层重置某个内部标志来重跑初始化。

对于需要延续的状态，应在卸载前导出为宿主可持有的、版本明确的数据，再由新模块导入。单例、事件订阅、对象池、序列化器和异步状态都要分别制定重建规则。状态协议可演进，旧运行时对象和旧方法入口不能直接成为跨卸载边界的存档。

类型初始化的规范边界见 [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})。

## 什么模块更适合做这件事

小游戏合集或生命周期清晰的独立玩法，容易定义“进入、停止、导出状态、卸载、重建”的流程。依赖很深、使用大量 native 回调、全局缓存或复杂序列化类型的模块，则需要更完整的退出设计。

模块通过接口通信只能减少耦合，不能单独证明可卸载。接口委托、事件订阅和后台任务仍然可能把旧代码留在系统里。

安全热重载依赖完整的退出与重建协议，不能由一次 GC 或一个卸载 API 单独完成。

## 系列位置

- 上一篇：[解释器指令优化与证据边界]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}})
- 下一篇：[代码加固与访问控制]({{< relref "engine-toolchain/hybridclr-code-encryption-access-control-policy.md" >}})
- 回到入口：[HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
