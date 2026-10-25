# 游戏引擎架构地图08：同任务比较证据卡

2026-10-25核对。正文见[08同任务比较](../content/system-design/game-engine-architecture-08-unity-unreal-same-tasks.md)，篇级状态见[canonical plan](game-engine-architecture-series-plan.md)，站点检查见[执行记录](game-engine-architecture-08-outline.md)。

## 固定范围

Unity 2022.3、Unreal Engine 5.6；不引入Addressables、Entities或其他附加包。案例采用传统GameObject/MonoBehaviour与Actor/ActorComponent。公开文档用于确认行为契约，不据此声称完整源码调用顺序、线程或运行实验通过。

同一任务的约束：
- 警戒参数以秒为单位，配置与当前剩余时间分开。
- 一套盔甲由网格、材质和纹理组成，先讨论随初始运行版本交付。
- 编辑器检查按钮只说明工具/运行代码隔离，不扩写07的数据导向组织。

## Unity已核对事实

| 官方入口（均2022.3） | 正文采用的事实及限制 |
| --- | --- |
| [Script serialization](https://docs.unity3d.com/2022.3/Documentation/Manual/script-Serialization.html) | 序列化满足规则的字段；Inspector不通过C#属性访问器。业务状态刷新不能寄托在setter必然执行上 |
| [Prefabs](https://docs.unity3d.com/2022.3/Documentation/Manual/Prefabs.html)、[Instance overrides](https://docs.unity3d.com/2022.3/Documentation/Manual/PrefabInstanceOverrides.html) | Prefab保存对象组合，实例覆盖优先于对应资产值；编辑实例、保存场景和应用覆盖回资产分开 |
| [SerializedObject](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SerializedObject.html) | Update/SerializedProperty/ApplyModifiedProperties负责编辑数据与写回，接入Undo和脏状态；写回对象不等于保存文件 |
| [SavePrefabAsset](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PrefabUtility.SavePrefabAsset.html)、[SaveScene](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveScene.html) | 前者接受属于现有Prefab Asset的GameObject，后者保存指定场景；不能把场景实例随手传给前者 |
| [Asset workflow](https://docs.unity3d.com/2022.3/Documentation/Manual/AssetWorkflow.html)、[Loading Resources at Runtime](https://docs.unity3d.com/2022.3/Documentation/Manual/LoadingResourcesatRuntime.html) | 导入、构建、分发、加载承担不同职责；Resources资产及依赖进入Player，外部依赖不因此得到可直接Resources.Load的路径；独立内容包另外构建交付 |
| [Assembly definitions](https://docs.unity3d.com/2022.3/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)、[Custom packages](https://docs.unity3d.com/2022.3/Documentation/Manual/CustomPackages.html) | Package可以包含代码、工具、资产；asmdef定义程序集及引用和目标。父目录有asmdef时，子Editor目录不会自动获得预定义编辑器程序集隔离 |

## Unreal已核对事实

| 官方入口（均5.6） | 正文采用的事实及限制 |
| --- | --- |
| [Properties](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-uproperties?application_version=5.6) | EditAnywhere控制属性窗口编辑；BlueprintReadOnly控制图中直接写入，二者不同；不能把SaveGame标志理解为自动完成游戏存档 |
| [Object Handling](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-object-handling-in-unreal-engine?application_version=5.6)、[Class Defaults](https://dev.epicgames.com/documentation/en-us/unreal-engine/blueprint-editor-defaults-tab?application_version=5.6) | CDO、序列化与默认值处理；Get Class Defaults不能直接提供组件模板内部默认属性。正文未把CDO默认值传播规则推成任意组件覆盖的实时更新承诺 |
| [Blueprint Class](https://dev.epicgames.com/documentation/en-us/unreal-engine/blueprint-class-assets-in-unreal-engine?application_version=5.6) | Blueprint Class基于父类定义类，作为内容资产保存；与Prefab并非完整类型语义等价物 |
| [Editor Python](https://dev.epicgames.com/documentation/en-us/unreal-engine/scripting-the-unreal-editor-using-python?application_version=5.6)、[Working with Assets](https://dev.epicgames.com/documentation/en-us/unreal-engine/working-with-assets-in-unreal-engine?application_version=5.6) | set_editor_property触发编辑器修改处理，ScopedEditorTransaction组织撤销；通知与事务不等于保存。没有给出未经核验的完整C++/Python保存脚本 |
| [Saving and Loading Your Game](https://dev.epicgames.com/documentation/en-us/unreal-engine/saving-and-loading-your-game-in-unreal-engine?application_version=5.6) | USaveGame存档对象、状态收集与保存/加载操作有独立流程，编辑器资产保存不等于玩家进度恢复 |
| [Importing Assets Directly](https://dev.epicgames.com/documentation/en-us/unreal-engine/importing-assets-directly-into-unreal-engine?application_version=5.6)、[Packaging Projects](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project?application_version=5.6) | 导入/保存与目标交付分开；Build/Cook/Stage/Package分别承担代码、目标内容、汇集与交付组织，不视为不可拆分的每次执行顺序 |
| [Content Cooking](https://dev.epicgames.com/documentation/en-us/unreal-engine/cooking-content-in-unreal-engine?application_version=5.6)、[Plugins](https://dev.epicgames.com/documentation/en-us/unreal-engine/plugins-in-unreal-engine?application_version=5.6) | Cook收集需看地图、引用与配置，软引用不等于不Cook；Plugin可含多个模块和内容，Runtime/Editor类型不能证明内容已经交付 |

## 设计推演与证据边界

- GuardAlert/AlertDelaySeconds及2→3、实例覆盖5秒为自拟教学例子；发现目标并开始等待时读取配置、等待结束后进入警戒，改值仅影响下次等待，是本例的玩法选择，不是引擎统一行为。
- 盔甲路径、程序集与模块名为示意；没有创建Unity/UE工程、设置资产或编译这些声明。
- 两图分别表示配置数据关系与代码依赖，不表示统一运行时间线、线程或实际完整依赖图。
- 文末三组实验是待执行设计；尚无Undo、保存重载、运行倒计时、Player/UE打包或设备结果。
- 资源释放仅说明应追清使用寿命，没有承诺某个通用卸载API能立即回收两台引擎的全部资源。
- 未复用受限Unity C++源码，不根据品牌、名称或文档目录推出“workflow/framework气质”。
