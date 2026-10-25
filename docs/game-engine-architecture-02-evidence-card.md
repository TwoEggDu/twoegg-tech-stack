# 游戏引擎架构地图 02｜证据卡：Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪

## 2026-10-25 修订依据与覆盖边界

本轮基于 Unity 2022.3、Unreal Engine 5.6 官方文档修订。来源已实际打开并核对相关段落；没有读取两台引擎受限实现，也没有启动引擎验证。角色关系图是自有教学模型，箭头表达包含、组合、控制和资产引用，不是线程、调用或继承图。

| 来源 | 本轮直接支持的事实 | 使用边界 |
| --- | --- | --- |
| [Unity 2022.3 GameObject](https://docs.unity3d.com/2022.3/Documentation/Manual/class-GameObject.html) | GameObject 的组件组织与 Transform 关系 | 不推断项目的控制器、规则或身份策略 |
| [Unity 2022.3 Hierarchy](https://docs.unity3d.com/2022.3/Documentation/Manual/Hierarchy.html) | 窗口显示和组织 GameObject，可显示多 Scene；父子关系可由 Transform 建立 | 窗口不是运行时世界容器或 Scene 与对象之间的一层 |
| [Unity 2022.3 Execution Order](https://docs.unity3d.com/2022.3/Documentation/Manual/ExecutionOrder.html) | OnEnable 的启用/活动条件；单对象 Awake/OnEnable 顺序；跨对象和动态创建的限制 | 不承诺任意对象间的同名回调总顺序 |
| [UE 5.6 Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology?application_version=5.6) | World、Level、Actor 及 Pawn、Controller、GameMode、GameState 的职责 | 不把这些角色写成所有 Actor 都必须采用的结构 |
| [UE 5.6 Actor Lifecycle](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle?application_version=5.6) | 加载、PIE 复制、生成路径不同，初始化、EndPlay、后续回收有阶段区别 | 不把不同路径压成统一构造时序，不保证跨对象任意顺序 |
| [UE 5.6 Components](https://dev.epicgames.com/documentation/en-us/unreal-engine/components-in-unreal-engine?application_version=5.6) | Scene Component 表达 Transform 与挂接树；普通 ActorComponent 无 Transform | 仅用于正文比较表的空间关系落点，不推广为所有关系都是组件父子关系 |

## 教学模型与建议

- 分开世界成员、能力组合、空间父子、控制来源与共享资产五类关系。
- 两个角色共用网格但保持独立实例状态；示意图只画乙的一部分组件。
- 删除角色、切换控制来源与修改空间挂接是不同操作；示例清理过程是模型要求，不是实际引擎销毁调用链。
- “控制逻辑与角色身体分开”“共享资产不能因为一个实例退出就直接销毁”作为可检查的设计原则。
- Unity 与 Unreal 的映射比较默认契约，不作优劣排序或逐类等价翻译。

## 相对旧稿的纠正

1. 移除 `Scene -> Hierarchy -> GameObject` 运行时链；分别表达 Scene 成员、组件组合、Transform 父子和编辑器视图。
2. 将“事件顺序明确”补成有限保证，注明 OnEnable 条件及跨对象/动态实例限制。
3. 去掉“Unreal 生命周期更强，所以 Actor 更完整”的泛化；以加载/生成/结束使用的具体协议说明差异。
4. 补入角色、组件、控制来源与共享资产的图及三个阅读动作：生成、转移控制、删除。
5. 不再保留内部 READY 状态作为正文证据；没有将当前工作区状态推广成永久结论。

## 验证与未验证项

- 已做：官方文档相关段落核对、图的关系语义与正文一致性检查。
- 未做：Unity 生命周期顺序实验、UE Spawn/PIE/EndPlay 跟踪、资产引用计数或设备释放验证、引擎源码审计。
- Mermaid 的实际页面渲染及站点构建由主任务统一验证；本卡不提前声称通过。
- 世界启动和退出的公共模型复用 GEF-02，不新增重复正文。
