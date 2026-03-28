# Unity DOTS 后续专题共享术语表

> Round 0 共享底稿。作用不是补正文，而是给 `Physics / 项目落地与迁移 / NetCode` 三条线统一术语、边界和版本口径，避免多 agent 并行时各写各的。

---

## 使用规则

- 这份表只冻结**概念口径**，不冻结所有 API 细节。
- 每篇正文开写前，先对照本文确认术语和边界；如果必须落到版本敏感 API，正文里单独补 `验证环境 / 包版本 / 本文 API 锚点` 小节。
- 后续文章可以引用 `DOTS-E01~E18`，但不能重新发明一套命名。

---

## 系列边界冻结

| 系列 | 负责回答什么 | 不要越界到哪里 |
|------|--------------|----------------|
| `Unity DOTS Physics` | 物理世界怎样接进 ECS 世界，查询、事件、Baking、角色控制和调试怎么看 | 不展开多人同步、服务端 Headless、渲染深水区 |
| `Unity DOTS 项目落地与迁移` | 项目里该不该上 DOTS、边界怎么切、怎样验证收益、怎么接进工程链 | 不写成 API 教程，不代替 Physics / NetCode 正文 |
| `Unity DOTS NetCode` | Client / Server World、Ghost、Snapshot、Prediction、Rollback、排障路径 | 不承担服务端 ECS 架构选型，不展开 Headless Server 工程细节 |

**明确不并入这 20 篇的主题**

- `DOTS Headless Server`：挂到服务端 ECS 线。
- 更深的 `Entities.Graphics / RenderGraph / SRP`：挂到渲染线。
- 泛化的 Unity 构建系统、CI 常识：只有和 DOTS 的 `Burst / AOT / Profiler` 工程链直接相关时才进入迁移线。

---

## 共享术语

| 术语 | 统一含义 | 不要写成 | 主要使用系列 |
|------|----------|----------|--------------|
| `World` | 一组 Entity、System 和调度边界组成的运行时世界 | “一个场景”或“一个线程” | 全部 |
| `Simulation Layer` | 负责权威状态更新的仿真层 | 表现层、渲染层 | 迁移 / NetCode |
| `Representation Layer` | 把仿真结果变成 MonoBehaviour、动画、VFX、UI 的表示层 | 逻辑主层 | 迁移 / Physics |
| `Fixed Step` | 以固定时间步推进仿真和物理的调度方式 | “每帧都一样” | Physics / NetCode |
| `Structural Change` | Entity 的 Archetype 发生变化，需要迁移 Chunk 布局的结构变更 | 普通字段写入 | Physics / 迁移 |
| `Physics World` | 由物理构建、步进和导出链维护的物理仿真世界 | MonoBehaviour 物理的换皮 | Physics |
| `Physics Query` | 以 Raycast / ColliderCast / DistanceQuery 等方式向物理世界做查询 | 普通 ECS Query | Physics |
| `Authority` | 哪一侧持有最终状态裁决权 | “谁先算出来就算谁的” | NetCode |
| `Ghost` | NetCode 中负责状态复制的同步对象模型 | 任意 NetworkObject 的统称 | NetCode |
| `Snapshot` | 在某个 Tick 上发给远端的同步状态切片 | 完整存档或完整 World 拷贝 | NetCode |
| `Prediction` | 客户端基于本地输入先行模拟的链路 | 单纯多跑一遍逻辑 | NetCode |
| `Rollback` | 收到权威状态后回退并重放预测链的修正过程 | 服务器强行覆盖一切 | NetCode |
| `Interpolation` | 远端对象基于历史状态做平滑显示的链路 | 预测 | NetCode |
| `Relevancy` | 哪些实体值得同步给当前客户端的筛选规则 | 单纯的距离判断 | NetCode |
| `Pilot System` | 项目里用于低风险试点 DOTS 的第一批候选系统 | 首次全量迁移 | 迁移 |
| `A/B Baseline` | 用旧实现和新实现做对照验证的基线 | “感觉更快了” | 迁移 |
| `Exit Condition` | 明确说明什么时候不该继续上 DOTS 的退出条件 | 失败后再看 | 迁移 |

---

## 最容易漂的口径

| 高风险口径 | 统一要求 |
|------------|----------|
| `Hybrid 边界` | 由 `DOTS-M03` 主负责；Physics / NetCode 只在本篇上下文里引用，不再重新下定义 |
| `Prediction / Rollback / Snapshot` | 先用概念层解释，再落到具体包实现；不要把某一版 API 名写成“永远如此” |
| `Query` | `EntityQuery` 只指 ECS 数据查询；`Physics Query` 只指向物理世界查询 |
| `性能验证` | 一律写成“最小可重复验证 + A/B 基线 + 通过阈值”，不写“优化建议清单” |
| `调试篇` | 工具名和窗口名最容易变，必须附验证环境；正文优先讲诊断顺序，不背 UI 名字 |

---

## 版本敏感区提醒

- `DOTS Physics`：`Unity Physics / Havok Physics` 的包能力和调试入口在不同版本上可能有差异，单篇必须写明验证环境。
- `DOTS NetCode`：`Ghost Authoring`、`Prediction / Rollback`、`系统组命名`、`诊断窗口` 都可能随版本变化，概念和 API 要分开写。
- `DOTS 项目落地与迁移`：`M04` 不能写成 changelog 摘抄，必须总结“升级最容易炸的层”。

---

## 跨篇引用规则

- `DOTS-P01 / M01 / N01` 是三条线的锚点篇。后文可以直接引用它们，不再重复定义整个问题空间。
- `DOTS-M03` 定义长期 Hybrid 边界；`DOTS-P05` 和 `DOTS-N06` 只在各自上下文里讲具体落点。
- `DOTS-P07` 和 `DOTS-N07` 是诊断收束篇；前文积累故障现象，最后统一回收到排障表。

---

## Batch 1 冻结输出要求

首批三篇 `DOTS-P01 / DOTS-M01 / DOTS-N01` 必须同时满足：

- 先立世界地图，再谈 API 或案例。
- 每篇都要写清“这篇不解决什么”。
- 文末必须给出下一篇导读，分别接到 `P02 / M03 / N03`。
- 只写概念稳定层，不把版本敏感实现硬写死。
