# 游戏引擎架构地图进度账本

## 当前阶段

- 阶段：`首稿推进`
- 目标：`先完成整套系列的可读首稿，再进入逐篇细化`

## 文章状态
| 编号 | 文章 | 状态 | 最近进展 | 下一步 |
| --- | --- | --- | --- | --- |
| 00 | 总论：现代游戏引擎到底该怎么分层 | 首稿完成 | 已按详细提纲写出可读首稿，覆盖固定 8 段骨架与六层总地图，明确区分官方资料事实与工程判断，不展开 `02 / 03 / 07 / 05 / 06` 的细节。 | 第一阶段继续向下一篇推进；等整套首稿完成后再回到本篇细化。 |
| 01 | 为什么游戏引擎首先是一套内容生产工具 | 首稿完成 | 已基于证据卡与详细提纲写出可读首稿，沿固定 8 段骨架把 Unity 的 `Scene View / Inspector / Prefab / Package Manager` 与 Unreal 的 `Level Editor / Content Browser / Details / Blueprints / Plugins` 收回内容生产层，并持续声明当前不是源码级验证。 | 本篇后续再补源码证据、短观点与收紧表达。 |
| 02 | Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪 | 首稿完成 | 已基于证据卡与详细提纲写出可读首稿，沿固定 8 段骨架压清 `Scene / World`、`GameObject / Actor`、`Component`、`MonoBehaviour lifecycle` 与 `Gameplay Framework` 的默认对象世界差异，并持续声明当前不是源码级验证。 | 本篇后续再补源码证据与收紧表达。 |
| 03 | 脚本、反射、GC、任务系统，到底站在引擎的哪一层 | 首稿完成 | 已基于证据卡与详细提纲写出可读首稿，沿固定 8 段骨架把 Unity 的 `scripting backend / PlayerLoop / GC / reflection / Job System` 与 Unreal 的 `C++ / UObject / Reflection / Blueprint / Task Graph` 收回运行时底座层，并持续声明当前不是源码级验证。 | 本篇后续再补源码证据与收紧表达。 |
| 04 | 渲染、物理、动画、音频、UI，为什么都像半台小引擎 | 首稿完成 | 已基于详细提纲写出可读首稿，沿固定 8 段骨架把 Unity 的 `render pipeline / physics integrations / animation system / audio stack / UI Toolkit` 与 Unreal 的 `Lumen / Nanite / Chaos / Animation Blueprint / MetaSounds / UMG / Slate` 收回专业子系统层，保留一张对照表与事实 / 判断分界，不写成功能百科、教程或产品比较。 | 本篇后续再补源码证据、短观点与收紧表达。 |
| 05 | 资源导入、Cook、Build、Package，为什么也是引擎本体 | 首稿完成 | 已基于详细提纲写出可读首稿，沿固定 8 段骨架把 Unity 的 `Asset Database / serialization / Addressables / AssetBundles / BuildPipeline` 与 Unreal 的 `Asset Registry / Asset Manager / cook / package / Unreal Build Tool` 收回资产与发布层，保留一张对照表与事实 / 判断分界，不写成教程、参数百科或产品比较。 | 本篇后续再补源码证据、短观点与收紧表达。 |
| 06 | 跨平台引擎到底在抽象什么？ | 首稿完成 | 已基于详细提纲写出可读首稿，沿固定 8 段骨架把 Unity 的 `graphics APIs / conditional compilation / Player settings / build profiles / platform-specific rendering differences` 与 Unreal 的 `RHI / target platform / target platform settings / build configurations / target-platform requirements` 收回平台抽象层，保留一张对照表与事实 / 判断分界，明确声明当前仍不是源码级验证，不写成平台接入教程或产品比较。 | 本篇后续再补源码证据、短观点与收紧表达。 |
| 07 | 为什么 DOTS 和 Mass 不能只算“一个模块” | 首稿完成 | 已基于官方文档证据卡与详细提纲写出可读首稿，沿固定 8 段骨架把 DOTS / Mass 收回数据导向扩展层，说明它们如何分别改写默认对象世界与批处理执行组织，并持续声明当前不是源码级验证。 | 本篇后续再补源码证据、短观点与收紧表达。 |
| 08 | Unity 和 Unreal，到底是什么气质的引擎 | 提纲中 | 已基于证据卡建立详细提纲，锁定固定 8 段骨架、总对照表、节级证据锚点与事实 / 判断分界，把 Unity 的 `Scene View / Prefab / Package / GameObject / IL2CPP / BuildPipeline / build profiles / Entities` 和 Unreal 的 `Unreal Editor / World / Actor / Gameplay Framework / UObject / Packaging / target platform / Mass` 都收回“复杂度默认被放在哪种组织方式里”这一问，持续声明当前不是源码级验证。 | 基于详细提纲起草可读首稿，继续只回答“复杂度默认被放在哪种组织方式里”；在 `08` 达到首稿完成前不要转入细化。 |

## 状态枚举
- `未开始`
- `提纲中`
- `首稿中`
- `首稿完成`
- `细化中`
- `完成`
- `阻塞`

## 当前顺序

默认推进顺序：
1. `00`
2. `02`
3. `03`
4. `07`
5. `01`
6. `04`
7. `05`
8. `06`
9. `08`

## 最近一次运行摘要
- 时间：`2026-03-23 00:00:03 +08:00`
- 推进文章：`08`
- 动作类型：`建立详细提纲`
- 更新文件：`docs/game-engine-architecture-08-outline.md`、`docs/game-engine-architecture-progress.md`
- 备注：本轮继续只使用官方文档证据；`docs/engine-source-roots.md` 中 Unity / Unreal 仍未标记为 `READY`，因此新提纲只在 `08` 证据卡和 `00-07` 已建立的官方证据边界上做总收束，不声称源码级验证；提纲按固定 8 段骨架锁定“复杂度默认被放在哪种组织方式里”这一唯一问题，安排总对照表、节级证据锚点与事实 / 判断分界，不重讲 `01 / 02 / 03 / 04 / 05 / 06 / 07` 的机制细节，也不写成产品优劣比较、选型建议或术语对译表。

## 下一次运行默认目标
- 文章：`08`
- 动作：基于 `08` 详细提纲起草可读首稿；它仍是第一阶段最后一篇总收束文章，在 `08` 达到首稿完成前不要转入逐篇细化。

## 说明

自动化应优先读取本文档来判断：
- 当前应该推进哪一篇。
- 这一篇现在处于什么阶段。
- 应该继续写首稿，还是转入细化。

如果本文档和执行计划冲突，以“先完成整套首稿，再逐篇细化”为准。
