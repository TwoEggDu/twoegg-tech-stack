# 游戏引擎架构地图08：同任务比较证据需求

2026-10-25重定范围；本文是待写文章的证据需求，不是已完成的实验记录。正文状态见[系列规划](game-engine-architecture-series-plan.md)，写作范围见[08提纲](game-engine-architecture-08-outline.md)。

## 可复用与仍须查证

| 任务 | 已有材料入口 | 开写前仍需补齐 |
| --- | --- | --- |
| 可编辑属性从模板进入实例 | [01证据卡](game-engine-architecture-01-evidence-card.md)、[02证据卡](game-engine-architecture-02-evidence-card.md) | 所选版本的实际字段、撤销、持久化、实例覆盖和重载路径；区别公开行为与源码判断 |
| 内容进入目标产物 | [05证据卡](game-engine-architecture-05-evidence-card.md)、[06证据卡](game-engine-architecture-06-evidence-card.md) | 固定同类资产与目标，记录生成产物、依赖和消费方；包格式或加载API细节重新查证 |
| 可选的专业能力接入 | [00A](../content/system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md)、[07证据卡](game-engine-architecture-07-evidence-card.md) | 同一扩展需求的注册、生命周期、退出解绑和资源释放，不以包/模块名称代替证据 |

已有证据可支持局部概念，不能直接拼成已验证的Unity/Unreal全链对照。下一轮应先冻结两个引擎和有关包版本，再建立逐项事实表；本轮未做这些实际运行。

## 撤回的推论

- 不从UI Toolkit、Slate、Package、Module等命名推断整台引擎的稳定“气质”。
- 不默认Unity以工作流组织而UE以框架组织；两者都可能存在工作流、框架、模块和管线。
- 不把Prefab/Blueprint、Package/Plugin、DOTS/Mass当作完整语义等价物。
- 不以官方资料目录证明完整依赖图或源码执行顺序。

结论必须落在明确任务和版本的观察上。资料无法覆盖的寿命、线程或构建行为保留为待查问题，不写成产品差异。
