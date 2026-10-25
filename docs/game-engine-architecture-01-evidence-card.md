# 游戏引擎架构地图 01｜证据卡：为什么游戏引擎首先是一套内容生产工具

## 2026-10-25 修订依据与覆盖边界

本卡对应已修订正文，证据等级为官方文档。Unity 映射固定到 2022.3，Unreal 映射固定到 5.6；两组版本只提供实现例子，不构成产品能力或运行时结构的一一映射。以下来源已在本轮打开并核对相关段落。

角色模板改值、覆盖优先的教学协议、撤销记录结构和预览分支是作者自有例子。除明确标注的映射外，不声称两台引擎按这套步骤实现。内容生产是一组职责，不据此推断编译层级、程序边界或线程。

| 来源 | 本轮直接支持的事实 | 不由该来源推出什么 |
| --- | --- | --- |
| [Unity 2022.3 Instance overrides](https://docs.unity3d.com/2022.3/Documentation/Manual/PrefabInstanceOverrides.html) | 实例覆盖优先，模板修改不会覆盖实例已覆盖的对应属性 | 所有字段、所有实例都无条件随模板变化 |
| [Unity 2022.3 SerializedObject](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SerializedObject.html) | 序列化字段编辑接入 Undo、脏标记与 Prefab override 表现；直接赋值需另行处理编辑协议 | 任意业务校验、专业系统重建及磁盘保存都自动完成 |
| [Unity 2022.3 Package Manager](https://docs.unity3d.com/2022.3/Documentation/Manual/Packages.html) | 包可包含工具、运行时库、资产与模板；存在窗口、API、manifest 管理入口 | 一个包就是一个纯工具层或全部内容都进入最终发行 |
| [UE 5.6 Blueprints Technical Guide](https://dev.epicgames.com/documentation/en-us/unreal-engine/technical-guide-for-blueprints-visual-scripting-in-unreal-engine?application_version=5.6) | Blueprint 扩展类并组织组件、变量和逻辑，Construction Script 可构造 Actor 实例 | Blueprint 与 Prefab 的覆盖、撤销和实例语义完全相同 |
| [UE 5.6 Working with Plugins](https://dev.epicgames.com/documentation/en-us/unreal-engine/working-with-plugins-in-unreal-engine?application_version=5.6) | 插件可扩展工具界面、按项目启停 | 插件启用即可证明所有模块应随游戏发行 |

## 正文的公共模型与例子

- 输入是用户的编辑意图，输出是可编辑描述、操作记录、持久化内容和预览变更。
- 示例将模板默认值、实例覆盖、操作历史、运行预览状态分开。
- 撤销“新增覆盖”需要恢复覆盖关系，不能只把数字写回相同值。这是示例设计要求，不是对引擎撤销实现的源码结论。
- 保存和预览是不同消费方向；文件内容与专业系统派生状态分别由负责方处理。
- 发行目标不应依赖属性面板、选中状态或撤销栈，是本文采用的设计原则。

## 相对旧稿的纠正

1. 删除“官方文档入口先介绍编辑器，所以现代引擎在架构上首先是编辑器”的推导。标题保留，“首先”明确为观察问题时的关注点。
2. 将“Prefab 修改传播给所有实例”收窄为遵守实例覆盖与具体字段规则的传播。
3. 将 Package / Plugin 从简单层级标签改为跨职责的分发和依赖管理单位。
4. 将重复的功能罗列替换为编辑、撤销、保存、预览之间的责任交接。
5. 删除正文中的内部源码根路径和 READY 状态；不把某次工作区状态当作读者知识。

## 验证与未验证项

- 已做：正文与官方资料对照；教学例子、事实与设计建议的范围检查。
- 未做：Unity / Unreal 启动、模板编辑实验、Undo/Redo 回放、保存恢复、源码调用链和性能验证。
- 站点构建与生成页校验由本轮主任务统一记录，本卡不预先宣称通过。
- 本卡未改变系列目录状态；篇级路由由 canonical series plan 维护。

## 2026-10-25阅读修订增量

正文将Package Manager与Plugin的分发/模块介绍压缩为08对应小节的链接，保持编辑、覆盖、撤销、保存与预览主线。上表中的Package Manager和Working with Plugins作为此前修订的资料记录保留，已不再是当前正文直接引用；当前正文保留实例覆盖、SerializedObject与Blueprint三处官方来源。本轮未增加引擎事实或重新进行外部资料核验，文档验证见[阅读修订记录](game-engine-architecture-reading-refinement-outline.md)。
