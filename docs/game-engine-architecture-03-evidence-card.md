# 游戏引擎架构地图 03｜证据卡：脚本、反射、GC、任务系统，到底站在引擎的哪一层

## 2026-10-25 修订依据与覆盖边界

本轮正文依据 Unity 2022.3、Unreal Engine 5.6 官方资料。以下来源已打开核对；只有文档级映射，不声称引擎源码、运行时序、线程分布或性能验证。

“运行时底座”表示共享职责群，不是一只编译模块。帧策略、对象回调接入、通用任务机制和平台执行设施分别讨论；动画任务图是作者教学设计，不是 Unity 或 Unreal 的真实动画流水线。

| 来源 | 本轮直接支持的事实 | 不应推出的保证 |
| --- | --- | --- |
| [Unity 2022.3 IL2CPP](https://docs.unity3d.com/2022.3/Documentation/Manual/IL2CPP.html) | IL 转 C++ 再编译到目标机器码；Mono 在运行时编译，IL2CPP 在构建期编译 | JIT 的定义就是动态生成 IL，或 AOT 取消全部运行时支持 |
| [Unity 2022.3 .NET 概览](https://docs.unity3d.com/2022.3/Documentation/Manual/overview-of-dot-net-in-unity.html) | 动态代码生成受后端约束；UnityEngine.Object 连接原生对应对象；原生销毁与托管 GC 有区别 | GC 能完成所有世界退出、外部句柄和设备资源清理 |
| [Unity 2022.3 Job 数据类型](https://docs.unity3d.com/2022.3/Documentation/Manual/JobSystemNativeContainer.html) | NativeContainer 可访问与主线程共享的原生数据；只读/写入访问受安全与依赖约束 | 所有 Job 数据完整深拷贝，或任务化之后任意共享写入都安全 |
| [UE 5.6 Objects](https://dev.epicgames.com/documentation/en-us/unreal-engine/objects-in-unreal-engine?application_version=5.6) | UObject 类型接入对象系统与 UHT 元信息 | 任意 C++ 对象、外部资源与引用全部被同一种 GC 规则管理 |
| [UE 5.6 Structs](https://dev.epicgames.com/documentation/en-us/unreal-engine/structs-in-unreal-engine?application_version=5.6) | USTRUCT 宏使结构体接入反射；结构体实例数据本身不作为 UObject 交给 GC 独立回收 | 把结构体值与类型元信息对象混为一谈，或认为支持反射就等于采用 UObject 寿命 |
| [UE 5.6 Tasks System](https://dev.epicgames.com/documentation/en-us/unreal-engine/tasks-systems-in-unreal-engine?application_version=5.6) | 依赖任务图、等待和结果获取；Tasks System 与 TaskGraph 共用调度器和工作线程后端 | 动画或渲染实际路径必然使用同一套 API；任务完成自动等于业务发布 |

补充核对过 [Unity 2022.3 Scripting backends](https://docs.unity3d.com/2022.3/Documentation/Manual/scripting-backends.html)、[Job System overview](https://docs.unity3d.com/2022.3/Documentation/Manual/JobSystemOverview.html)、[Execution Order](https://docs.unity3d.com/2022.3/Documentation/Manual/ExecutionOrder.html)。前者用于编译时机定义，后两者用于核对旧稿的任务与回调表述；正文主证据集中在上表。

## 教学模型与状态交接

- 帧策略决定哪些系统以及哪些消费阶段需要结果。
- 对象回调接入决定哪些有效对象在约定阶段被调用。
- 任务机制提供依赖与完成设施，平台提供具体执行能力。
- 动画示例固定输入身份、版本与参数，经过采样/混合，在发布阶段核对目标和版本。
- 输出缓冲寿命、任务完成对象和世界对象有效性分别解释。
- 目标失效时丢弃结果，是示例选择；不声称引擎自带相同取消或发布策略。
- 启动、固定步和退出链接现有 GEF-02，本文不重复展开。

## 相对旧稿的纠正

1. 旧句把“Mono/JIT 可动态生成 IL，IL2CPP/AOT 不支持动态代码生成”用作 JIT/AOT 区分。该能力描述确见官方 .NET 文档；修订纠正的是**以能力后果替代编译时机定义**的歧义，而非声称官方没有这一描述。
2. 将对象回调、帧策略与任务/平台设施分开，避免“都是运行时底座”被读为一层依赖。
3. 删除任务数据复制的无条件概括，补 NativeContainer 共享访问及协议约束。
4. 将 UObject 管理范围与普通 C++/USTRUCT 结构体值区分，不把反射等同于合法业务修改。
5. 删除反复宣称“正式入口/正式运行时”但没有交接内容的段落；保留一次证据范围和就近官方链接。
6. 移除正文内部 READY 状态，不继承旧卡对整个本地源码可用性的动态声明。

## 验证与未验证项

- 已做：上述版本官方文档核对、职责与教学状态交接的静态审读。
- 未做：引擎执行、Job/Task 调度实验、数据竞争测试、GC 行为实验、反射成本测试或性能比较。
- 教学图未作为可执行程序测试；它只约束示例中的先后与完成条件。
- 站点构建与实际 Mermaid 渲染由主任务统一记录，本卡不预先宣称通过。
