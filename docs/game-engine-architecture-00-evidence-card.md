# 游戏引擎架构地图00：职责地图修订证据卡

核对日期：2026-10-25。对应[正文](../content/system-design/game-engine-architecture-00-overview.md)；[本轮工作单](game-engine-architecture-revision-outline.md)记录执行与站点验证。本文未核对私有源码、未运行引擎。下面资料支持具体概念，不支持统一六层或固定依赖顺序。

| 官方材料 | 可支持的事实 | 正文使用与边界 |
| --- | --- | --- |
| [Unity2022.3 Prefabs](https://docs.unity3d.com/2022.3/Documentation/Manual/Prefabs.html) | 组件/属性/子对象可保存为模板资产；模板产生实例，实例可以有覆盖项 | 解释工具改模板与改实例的区别，不推广为通用对象模型 |
| [Unity2022.3 GameObjects](https://docs.unity3d.com/2022.3/Documentation/Manual/GameObjects.html) | GameObject容纳组件，由组件提供功能 | 说明身份与组合；不推断渲染器内部存储 |
| [Godot architecture](https://docs.godotengine.org/en/stable/engine_details/architecture/godot_architecture_diagram.html) | 区分Scene、Servers、Drivers/Platform、Core、Main；Core含Object/ClassDB等 | 作为职责区分案例，明确与本篇Core范围不同；stable不是冻结源码版本 |
| [UE5.6 Packaging](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project?application_version=5.6) | 构建、Cook、Stage、Package用于目标产物与发布 | 交付纳入工程视野，不据此称它为运行时底层 |

## 作者模型和本轮修正

- 内容生产、世界框架、运行机制、专业系统、资产交付、平台适配是问题覆盖面，不是严格六层架构。
- Core共享能力与数据导向横向组织分别讨论；07不再是主图最后一层。
- 角色外观修改是设计推演，箭头为数据交接。具体实现可以并行、合并或跳过。
- 撤下“先生产内容，再组织世界，再提供运行底座”的时间线暗示；00与00A按职责/依赖两种视角对齐。
- 不再以官方文档目录或产品名称证明架构全貌；删除内部READY及重复范围宣告。

## 导读的补充依据

[导读](../content/rendering/player-input-and-game-content-to-screen.md)同轮修订，除Prefab外核对：

- [Unity2022.3 Compute Shader](https://docs.unity3d.com/2022.3/Documentation/Manual/class-ComputeShader.html)：GPU可执行正常渲染管线外计算，撤下“GPU只负责画图”的绝对表述。
- [Unity2022.3 Execution Order](https://docs.unity3d.com/2022.3/Documentation/Manual/ExecutionOrder.html)：固定更新与帧更新节奏不同，不能把施法因果链当作同一显示帧的完整执行。

火球示例、输入不是唯一变化来源、状态归属和清理问题是公共模型推演，不登记为Unity实测。链接与构建检查只证明站点可构建、引用存在。
