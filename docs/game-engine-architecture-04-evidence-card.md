# 游戏引擎架构地图 04｜证据卡：渲染、物理、动画、音频、UI，为什么都像半台小引擎

## 本轮覆盖与证据边界

2026-10-25 修订：以公共职责模型和角色落地设计例子替换重复的产品能力枚举。本文为原理篇附实现对照，保留原编号、标题与 URL；本卡对应修订后的正文，替换旧首稿证据清单。

- 正文：[04 专业子系统](../content/system-design/game-engine-architecture-04-professional-subsystems.md)。
- 结构：角色表现不一致 → 五类专业表示 → 落地交接示例 → 状态与生命周期边界 → Unity/Unreal 对照。
- 基线：Unity 6.0（6000.0）与 Unreal Engine 5.6 官方公开资料，本轮直接读取下列页面。
- 工程例子：单机、由运动模块确认运动结果；不推导两台引擎的默认帧序、线程或网络行为。
- 验证范围：未做引擎源码审计或运行验证；整站 Hugo 构建由主任务统一执行，本卡不提前记为通过。

## 当前正文直接来源

| 官方资料 | 直接支持的事实 | 不支持的推断 |
| --- | --- | --- |
| [Unity 6.0 Render pipelines](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-overview.html) | 剔除、绘制、后处理及预制/自定义管线 | 所有管线内部线程或执行细节相同 |
| [Unity 6.0 Animation](https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationSection.html) | 导入、编辑、状态机与 Animator 等运行入口 | 动画状态机天然拥有全部玩法规则 |
| [Unity 6.0 UI Toolkit](https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html) | UXML/USS、UI Builder、事件系统、UI Renderer | 作者工具与运行模块必须同库、同时发行 |
| [UE 5.6 Animation Blueprints](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprints-in-unreal-engine?application_version=5.6) | 动画图控制 Skeletal Mesh 动画并形成姿态 | 本文落地示例就是 UE 默认更新链 |
| [UE 5.6 MetaSounds](https://dev.epicgames.com/documentation/en-us/unreal-engine/metasounds-the-next-generation-sound-sources-in-unreal-engine?application_version=5.6) | DSP 图、音频缓冲控制及玩法参数连接 | 游戏事件触发与音频完成处于同一帧时刻 |

## 设计判断与最小模型

- 输入/状态/输出表是作者分析模型，不宣称所有引擎按此布局。
- 落地由持续接触提升为一次状态转换，再向动画、音频和渲染交接，是示例设计；接触分类和运动权威需由项目定义。
- 数据因果图不能推导执行时序；正文要求核对快照、相容批次和失效身份。
- 工具与运行实现可属于同一专业领域，但在依赖图中分别定位。
- 共享资产、专业实例和世界身份分别管理；事件、查询或快照的选择由接口时效和所有权决定。

## 对旧稿的修正

- 用真正的系统职责边界替代“文章不写哪些章节”。
- 删除十组相同结论的产品枚举与多次总结，保留五个可解释模型的官方落点。
- 旧稿关于 Scriptable Audio Pipeline、广泛 Chaos/Nanite 能力与默认版本的列表不再作为当前正文事实；未将其缺少本轮核验认定为技术错误。
- 不再把本地源码路径的 READY 状态写成公开正文证据，未运行的限制只说明一次。

## 校验记录

- 已检查标题、日期、slug、series、weight 与修订前一致；正文 3 个 relref 目标存在。
- 正文 5 个官方来源与本卡对应，版本限定为 Unity 6000.0 / UE 5.6；Markdown 代码围栏成对。
- 本轮六个文件的 git diff --check 通过；尚未执行整站构建与浏览器图形验证，由主任务统一完成。
- 网页资料核对与站点构建不代表引擎运行验证。

## 2026-10-25增量：跨系统故障追踪的证据边界

- 新增章节以“角色落地后仍播放下落动画”为构造问题，日志中的W1、对象42、动画实例17/29和各计数均为教学数据，不是采集结果。
- 示例提供的因果证据：同一对象的有效支撑与状态修订、当前绑定记录、旧实例实际应用记录、当前实例求值输入、输出姿态与绘制消费关联。单独发送成功、日志未出现或提交成功均不构成端到端正确性证明。
- 定位到旧绑定依赖明确假设：最新落地状态持续有效，当前实例持续求值，允许的过渡和消费延迟已经过去。修复讨论属于设计建议，不声称Unity、Unreal或Piccolo存在该故障。
- 修复验证表是待执行实验：同步发布、快照恢复、失效消息拒绝及退役资源收尾仍需落在具体工程中核验。本轮没有新增引擎特定API或实现事实，保留原有官方来源及版本范围。
- 本轮站点与图示验证结果统一记录在[04增量记录](game-engine-architecture-04-outline.md)，与上述历史校验记录分开。
