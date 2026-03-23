# 游戏引擎架构地图 03｜证据卡：脚本、反射、GC、任务系统，到底站在引擎的哪一层

## 本卡用途

- 对应文章：`03`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`什么叫引擎的运行时底座，为什么脚本后端、反射、GC、任务系统不该被当成功能模块平铺。`
- 这篇不展开：`00 总论里的六层总地图`
- 这篇不展开：`02 里 Scene / World、GameObject / Actor 的默认对象世界差异`
- 这篇不展开：`07 里 DOTS / Mass 对世界模型和执行模型的重构`
- 这篇不展开：`04/05/06 里渲染、资源发布链、平台抽象的内部实现`
- 本篇允许做的事：`只锁定 Unity 的 scripting backend / PlayerLoop / GC / reflection / Job System，与 Unreal 的 C++ / UObject / Reflection / Blueprint / Task Graph 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与 API，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把 scripting backend 写成运行时执行边界

- Unity 入口：
  - [Overview of .NET in Unity](https://docs.unity3d.com/es/2021.1/Manual/overview-of-dot-net-in-unity.html)
  - [IL2CPP Overview](https://docs.unity3d.com/cn/2023.2/Manual/IL2CPP.html)
- 可直接证明的事实：
  - Unity 官方明确不同平台可能使用不同的 scripting backends。
  - Unity 官方明确区分 `JIT` 与 `AOT`：JIT backend 允许在运行时生成动态 IL，AOT backend 不支持这一点。
  - Unity 官方把 `IL2CPP` 定义为 scripting backend，并说明构建时会把脚本与程序集里的 IL 转成 C++，再生成平台原生二进制。
- 暂定判断：
  - Unity 的 `脚本后端` 不是外围工具或单点功能，而是直接决定代码如何进入运行时、能否动态生成代码、最后以什么形态执行的底座能力。

### 2. Unity 把每帧执行顺序挂在 PlayerLoop 与 MonoBehaviour 生命周期上

- Unity 入口：
  - [LowLevel.PlayerLoop](https://docs.unity3d.com/es/2020.1/ScriptReference/LowLevel.PlayerLoop.html)
  - [Order of Execution for Event Functions](https://docs.unity3d.com/ru/2018.4/Manual/ExecutionOrder.html)
- 可直接证明的事实：
  - Unity 官方把 `PlayerLoop` 描述为代表 Unity player loop 的类，并明确它用于获取所有原生系统的 update order，以及设置插入新脚本入口后的自定义顺序。
  - Unity 官方说明场景初始对象里，所有脚本的 `Awake` / `OnEnable` 会先于任何 `Start` / `Update`。
  - Unity 官方把 `Update`、`LateUpdate`、`FixedUpdate` 等生命周期放在统一执行顺序文档中说明，而不是把它们写成某个业务模块的私有规则。
- 暂定判断：
  - `PlayerLoop + 生命周期` 更像 Unity 默认执行骨架，而不是挂在渲染、物理、UI 旁边的又一个平铺功能点。

### 3. Unity 官方把 GC、反射、Job System 放在运行时语境里说明

- Unity 入口：
  - [Overview of .NET in Unity](https://docs.unity3d.com/es/2021.1/Manual/overview-of-dot-net-in-unity.html)
  - [C# reflection overhead](https://docs.unity3d.com/kr/6000.0/Manual/dotnet-reflection-overhead.html)
  - [Job system overview](https://docs.unity3d.com/cn/2023.1/Manual/JobSystemOverview.html)
- 可直接证明的事实：
  - Unity 官方说明 `Mono` 与 `IL2CPP` 都使用 `Boehm garbage collector`，并且默认启用 incremental GC。
  - Unity 官方说明 `Mono` 与 `IL2CPP` 会缓存 C# reflection (`System.Reflection`) 对象，而这些缓存对象不会被 Unity 自动回收，因此 GC 会持续扫描它们。
  - Unity 官方把 Job System 描述为允许用户代码与 Unity 共享 worker threads 的多线程系统，并强调 worker thread 数量会与可用 CPU core 匹配。
  - Unity 官方说明 Job System 的安全系统会把 blittable 类型数据复制到 native memory，并使用 `memcpy` 在 managed / native 之间传输数据。
- 暂定判断：
  - 在 Unity 里，`GC / reflection / Job System` 都更接近执行模型、内存模型与调度模型，而不是能和渲染、音频、动画并列的一排业务模块。

### 4. Unreal 不是“只有 C++”，而是 `C++ + UObject + Reflection` 的对象运行时

- Unreal 入口：
  - [Programming with C++ in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/programming-with-cplusplus-in-unreal-engine)
  - [Objects in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/objects-in-unreal-engine?application_version=5.6)
  - [Reflection System in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/reflection-system-in-unreal-engine)
- 可直接证明的事实：
  - Unreal 官方把 `Programming with C++` 写成引擎正式编程入口，并把 `Gameplay Architecture`、`Objects`、`Reflection System` 等内容放在同一组基础编程文档里。
  - Unreal 官方明确 `UObject` 是 Unreal 对象的基类，反射系统通过 `UCLASS`、`USTRUCT` 等宏把类接入引擎与编辑器功能。
  - Unreal 官方 `Objects` 文档明确列出 `UObject` 提供的能力：`garbage collection`、`reflection`、`serialization`、`automatic editor integration`、`type information available at runtime`、`network replication`。
- 暂定判断：
  - Unreal 的运行时底座不是“纯 C++ 代码自己跑”，而是 `C++ 执行 + UObject handling + reflection / metadata / serialization` 共同组成的对象运行时基础。

### 5. Unreal 官方把 Blueprint 与 Task Graph 都挂进运行时执行系统

- Unreal 入口：
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
  - [FTaskGraphInterface](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/Async/FTaskGraphInterface)
  - [FTaskGraphInterface::AttachToThread](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Core/FTaskGraphInterface/AttachToThread)
- 可直接证明的事实：
  - Unreal 官方把 `Blueprint Visual Scripting system` 定义为完整的 gameplay scripting system。
  - Unreal 官方 `FTaskGraphInterface` API 直接把 task graph 写成正式运行时接口，并暴露 `GetNumWorkerThreads`、`IsMultithread`、`IsThreadProcessingTasks` 等线程与调度相关能力。
  - Unreal 官方 `AttachToThread` 文档说明外部线程需要被显式引入 task graph system，才能被该系统识别和管理。
- 暂定判断：
  - 在 Unreal 里，`Blueprint` 与 `Task Graph` 更像脚本执行与任务调度的运行时设施，而不是和渲染、物理、音频并列的一组表面功能名词。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把 scripting backend、PlayerLoop、GC、reflection、Job System 都写在正式运行时与脚本执行语境里，而不是写成一组平铺业务功能。
- `事实`：Unreal 官方把 C++ 编程入口、UObject、Reflection System、Blueprint、Task Graph 都放在正式编程 / API 文档体系里，而不是仅作为某个功能子系统的附录。
- `事实`：Unreal 官方明确 `UObject` 提供 garbage collection、reflection、serialization、runtime type information 等跨系统基础能力。
- `事实`：Unity 官方明确 `IL2CPP` 是 scripting backend，`PlayerLoop` 管所有原生系统的 update order，`Mono` / `IL2CPP` 都共享同一套 GC 约束。
- `事实`：`docs/engine-source-roots.md` 目前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `03` 可以把 `脚本后端 / 生命周期与主循环 / 反射与对象元信息 / GC / 任务调度` 收拢成 `运行时底座层`，因为这些能力都在支撑整台引擎的执行方式。
- `判断`：文章 `03` 最稳的写法不是平铺名词，而是先回答 `什么机制决定代码怎么运行、对象如何被识别、内存何时被回收、任务如何被调度`。
- `判断`：Unity 与 Unreal 在这一层的差异，不是简单的 `C# vs C++`，而是 `脚本后端、对象系统、反射接入、GC 模型、任务调度接口` 的整体组织方式不同。
- `判断`：当前最安全的结论是把 `GC` 当成运行时机制，把 `Job System / Task Graph` 当成执行地基；至于更细的调度细节、锁粒度、调用链与 VM 内部实现，要留给后续证据或源码可用时再压实。

## 本卡暂不支持的强结论

- 不支持：`Mono、IL2CPP、Blueprint VM、Task Graph 的内部调用链已经完成源码级对照`
- 不支持：`Unity 与 Unreal 的 GC 机制已经可以做精确实现级比较`
- 不支持：`Blueprint VM 与 C# / IL2CPP 可以直接做一一等价映射`
- 不支持：`Burst、UE::Tasks、Task Graph、DOTS、Mass 的准确层边界已经一次性压实`
- 不支持：`脚本后端、反射、GC、任务系统的性能结论已经可以脱离具体场景下强判断`

## 下一次最合适的增量

- 基于本卡给 `03` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是表面 API 差异
  7. 常见误解
  8. 我的结论
