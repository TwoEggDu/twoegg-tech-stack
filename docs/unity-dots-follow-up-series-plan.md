# Unity DOTS 后续专题规划

> 本文件承接 `dots-mass-hardware-deep-series-plan.md` 中已经闭环的 `Unity DOTS 工程实践（DOTS-E01~E18）`。
> 那 18 篇解决的是 `Entities 1.x` 的核心工程路径；这里解决的是不适合继续硬塞进 `E19 / E20` 的三块后续主题：`Physics`、`项目落地与迁移`、`NetCode`。

---

## 系列定位

`DOTS-E01~E18` 已经回答了三件事：

1. `E01~E08`：ECS 核心模型怎样建立。
2. `E09~E15`：Baking、Jobs、Burst、ECB 这些工程能力怎样落地。
3. `E16~E18`：渲染边界、OOP 边界、调试工具怎样收束。

但 DOTS 读者真正继续往下走，最容易卡住的不是再多几个 API 题目，而是三类更大的专题：

1. **DOTS Physics**：物理世界怎样接进 ECS 世界。
2. **DOTS 项目落地与迁移**：项目里到底该不该上、怎么上、怎么长期维护。
3. **DOTS NetCode**：多人同步、预测、回滚、Ghost 边界到底怎么理解。

**一句话定位**：不把 DOTS 主线继续拉长，而是把最值得继续写的三块主题拆成三个独立后续系列，让 `DOTS-E01~E18` 保持闭环。

---

## 与其他系列的关系

| 关联系列 | 关系 |
|---------|------|
| `dots-mass-hardware-deep-series-plan.md` | 上游：`DOTS-E01~E18` 已完成，这里是后续延伸，不继续追加 `E19+` |
| 系列十八·C（Unreal Mass 深度） | 对照：Physics / NetCode 的很多判断点，最终都能回到 DOTS 和 Mass 的边界差异 |
| 系列十三·G（高性能游戏服务端 ECS） | 分流：`DOTS Headless Server` 更适合挂到服务端 ECS 线，不并进这里 |
| 渲染 / URP / SRP 相关系列 | 分流：更深的 `Entities.Graphics`、渲染管线与 RenderGraph 细节，不并进这里 |

---

## 目标读者

- 已经读完 `DOTS-E01~E18`，想继续往 `Physics / NetCode / 项目落地` 深挖的人
- 在项目里评估是否引入 DOTS，需要做长期技术选型判断的技术负责人
- 已经会写一些 ECS 代码，但在 `物理 / 网络 / 迁移边界` 上没有稳定工程判断的人

---

## 范围说明

**本规划涵盖的内容**

- `Unity Physics / Havok Physics` 的世界模型、查询、事件、调试与性能
- `DOTS` 在真实项目中的迁移路线、Hybrid 边界、版本升级、验证链与工程化
- `NetCode for Entities` 的 Ghost、Prediction、Rollback、Snapshot、调试与排障

**本规划不涵盖的内容**

- `DOTS Headless Server`
- 更深的 `Entities.Graphics / RenderGraph / SRP` 主题
- 纯 API 手册式的包说明文档

---

## 系列结构（20 篇）

### Part 1：Unity DOTS Physics（7 篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-P01 | Unity Physics / Havok Physics 全景：DOTS 里的物理世界到底怎样运转 | Unity Physics 和 Havok Physics 各自站在哪一层，为什么 DOTS 物理不是 MonoBehaviour Physics 的平移版 |
| DOTS-P02 | Collider、PhysicsBody、PhysicsMass：DOTS 物理数据模型怎么拆 | 刚体、碰撞体、质量、运动状态分别放在哪个 Component，为什么要这样分 |
| DOTS-P03 | Physics Query：Raycast、ColliderCast、DistanceQuery 什么时候该用哪种 | DOTS 里最常见的物理查询怎么选，代价差在哪 |
| DOTS-P04 | CollisionEvents / TriggerEvents：命中事件怎样安全地回写 ECS 世界 | 事件为什么不能直接在回调里乱改 Entity，正确的数据回流路径是什么 |
| DOTS-P05 | Character Controller 与 Kinematic 移动：为什么这块最容易写散 | 角色控制器在 DOTS 里为什么总是边界问题，而不是纯 Physics 问题 |
| DOTS-P06 | Physics Baking：Collider Authoring 到运行时物理数据的转换链 | 物理资源怎样在 Baking 期变成 ECS 可消费的数据 |
| DOTS-P07 | DOTS Physics 调试与性能分析：Broadphase、接触对、固定步长抖动怎么看 | 物理慢时到底看什么，不靠猜 |

### Part 2：Unity DOTS 项目落地与迁移（6 篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-M01 | 什么项目该上 DOTS：不要把“性能焦虑”误当成技术选型依据 | 适合 DOTS 的问题空间是什么，什么时候 `Burst / Jobs` 就够了 |
| DOTS-M02 | 第一阶段怎么迁移：别从 UI 开始，先切高密度仿真层 | 哪一层最适合做低风险试点，最小迁移路线怎么走 |
| DOTS-M03 | Hybrid 架构长期怎么活：输入、UI、动画、资源、调试分别站哪边 | MonoBehaviour 和 ECS 的长期边界到底该怎么切 |
| DOTS-M04 | 版本升级与包依赖：Entities / Burst / Collections 升级最容易炸哪 | DOTS 工程最大的风险为什么很多来自维护期而不是运行期 |
| DOTS-M05 | 测试与验证：怎样证明这次引入 DOTS 真的值 | 怎样做最小可重复性能验证，不靠主观感觉 |
| DOTS-M06 | 构建、CI 与发布：Burst、AOT、Headless、Profiler 数据怎么进工程链 | 当 DOTS 真正进项目后，构建与验证链该怎么补齐 |

### Part 3：Unity DOTS NetCode（7 篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-N01 | NetCode 世界观：Client World / Server World / Ghost 各自在解决什么 | 先分清不同 World 的职责，才能理解 NetCode 不是网络 API 套皮 |
| DOTS-N02 | CommandData 与输入链：客户端输入怎样进入预测系统 | 预测系统的入口为什么是输入而不是状态 |
| DOTS-N03 | Snapshot 与 Ghost 同步：什么应该同步，什么根本不该发 | 快照里到底该放什么，不该同步的对象有哪些 |
| DOTS-N04 | Prediction / Rollback：为什么“多跑一遍逻辑”远远不够 | 预测最难的不是概念，而是边界稳定性与确定性 |
| DOTS-N05 | Relevancy / Prioritization / Interpolation：不同实体为什么不该被同等对待 | 网络性能问题本质上也是预算分配问题 |
| DOTS-N06 | Character、Projectile、技能系统：三类高频对象在 NetCode 下怎么拆 | 真正的多人同步难点，为什么都会集中在这三类对象上 |
| DOTS-N07 | NetCode 调试与排障：延迟、抖动、错位、回滚尖峰怎么定位 | NetCode 最后拼的是证据链，不是概念背诵 |

---

## 总篇数

| Part | 内容 | 篇数 |
|------|------|------|
| Part 1 | Unity DOTS Physics | 7 |
| Part 2 | Unity DOTS 项目落地与迁移 | 6 |
| Part 3 | Unity DOTS NetCode | 7 |
| **合计** | | **20 篇** |

这 20 篇是 `DOTS-E01~E18` 的后续延伸，**不计入系列十八现有 45 篇闭环**。

---

## 写作约束

- **不继续写成 `DOTS-E19+`**：编号和定位都要明确成独立后续专题
- **Physics 与 NetCode 必须标清包版本与验证环境**：这两块版本敏感，不能写成模糊经验帖
- **迁移篇必须回答“什么时候不要上 DOTS”**：不能只写引入理由，不写退出条件
- **每篇都要显式回连 `DOTS-E01~E18` 的前置概念**：避免后续专题重新重复主线内容

---

## 推荐推进顺序

1. **先写 `DOTS Physics`**：最自然衔接现有 18 篇，也最容易接住现在的读者需求。
2. **再写 `DOTS 项目落地与迁移`**：这组最能体现工程判断价值，也能补上选型闭环。
3. **最后写 `DOTS NetCode`**：价值最高，但版本敏感、校对成本最高，适合最后开。

---

## 当前状态

| Part | 状态 |
|------|------|
| Part 1 Unity DOTS Physics（7 篇） | ❌ 待写 |
| Part 2 Unity DOTS 项目落地与迁移（6 篇） | ❌ 待写 |
| Part 3 Unity DOTS NetCode（7 篇） | ❌ 待写 |

---

## 最后压成一句话

读完这 20 篇，读者应该能回答：`DOTS 主线之外，物理、项目迁移和多人同步这三块最容易翻车的主题，分别该怎样判断、怎样落地、怎样避坑。`
