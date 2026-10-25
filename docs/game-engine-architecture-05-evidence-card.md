# 游戏引擎架构地图 05｜证据卡：资源导入、Cook、Build、Package，为什么也是引擎本体

## 本轮覆盖与证据边界

2026-10-25 修订：把资源身份、加工产物、交付组织和运行实例拆开，以两条交付路线替换统一必经链的暗示。本文为原理篇附实现对照，保留原编号、标题与 URL；本卡对应修订后的正文，替换旧首稿证据清单。

- 正文：[05 资产与发布](../content/system-design/game-engine-architecture-05-asset-and-delivery.md)。
- 结构：发布内容缺失 → 五类对象 → 随应用/独立内容两条路线 → 外观设计示例 → 职责及实现对照。
- 基线：Unity 6.0（6000.0）与 Unreal Engine 5.6 官方公开资料，本轮直接读取下列页面。
- 工程例子：基础外观随应用、追加外观独立交付；不声称所有项目采用同一包布局与执行顺序。
- 验证范围：未执行内容构建、Cook、打包或引擎运行；整站 Hugo 构建由主任务统一执行，本卡不提前记为通过。

## 当前正文直接来源

| 官方资料 | 直接支持的事实 | 不支持的推断 |
| --- | --- | --- |
| [Unity 6.0 Asset Database](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabase.html) | 导入转换源资产，并维护源文件与导入结果的关系 | 导入已决定所有最终分包与运行策略 |
| [Unity 6.0 Resources](https://docs.unity3d.com/6000.0/Documentation/Manual/LoadingResourcesatRuntime.html) | Resources 内容进入 Player build，可在运行时加载 | Resources 适合所有项目或独立 Bundle 是必需前置 |
| [Unity 6.0 AssetBundles](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetBundlesIntro.html) | 面向平台的非代码内容包，可用低层 API 或 Addressables 组织构建 | 每个 Player 都必须有独立 AssetBundle 阶段 |
| [UE 5.6 Asset Management](https://dev.epicgames.com/documentation/en-us/unreal-engine/asset-management-in-unreal-engine?application_version=5.6) | Asset Manager 贯穿编辑器/打包游戏，支持 Primary Asset、Chunking 与审计 | 获取资产自动在某个世界实例化角色 |
| [UE 5.6 Packaging](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project?application_version=5.6) | Build、Cook、Stage、Package 职责及目标平台产物 | Cook 与 Unity 导入/构建缓存、包格式严格一一对应 |

## 设计判断与最小模型

- 五类对象分别是源资产/设置、身份/描述、加工产物、交付单元/索引、运行数据/实例。
- 双路线图是产物数据流，不是编译依赖图或唯一串行构建时序；独立内容包也可以本地随首包交付。
- 身份、内容版本和目标应可区分；具体键值、索引格式和增量算法不作实现断言。
- 外观示例讨论依赖可达、共享包取舍、索引与内容相容发布，不声称证明某个项目的真实更新链。
- 通用资源服务不自行决定往哪个世界生成角色；类型解释、设备准备与世界实例化分别有负责方。

## 对旧稿的修正

- 移除“Unity 还要经过 artifact、bundle 与 Player build”的统一必经暗示；Bundle 为按交付策略选用的路线。
- 不把序列化机制、资产策略和世界生成视作同一职责。
- 不把 Asset Registry 作为与 Unity 导入器相同的转换步骤；当前对照聚焦已直接核实的 Asset Manager 与 Packaging。
- 删除重复枚举、内部 READY 状态及旧“下一步建提纲”提示；正文声明已完成与未验证范围。

## 校验记录

- 已检查标题、日期、slug、series、weight 与修订前一致；正文 3 个 relref 目标存在。
- 正文 5 个官方来源与本卡对应，版本限定为 Unity 6000.0 / UE 5.6；Markdown 代码围栏成对。
- 本轮六个文件的 git diff --check 通过；尚未执行整站构建与浏览器图形验证，由主任务统一完成。
- 网页资料核对与站点构建不代表引擎运行验证。
